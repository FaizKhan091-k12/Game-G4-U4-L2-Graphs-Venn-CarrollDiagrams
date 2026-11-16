using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DataChampions.EditorTools
{
    /// <summary>
    /// Editor-only test helpers used from MCP commands: exact Game view sizes, stepping a paused play session
    /// forward by real seconds (works while the Editor window is in the background), and screenshots.
    /// </summary>
    public static class DataChampionsDevTools
    {
        /// <summary>Selects (adding if needed) a fixed Game view resolution.</summary>
        public static bool SetGameViewSize(int width, int height)
        {
            try
            {
                var asm = typeof(UnityEditor.Editor).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes = singleton.GetProperty("instance").GetValue(null);
                var groupType = (GameViewSizeGroupType)sizesType.GetProperty("currentGroupType").GetValue(sizes);
                var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { (int)groupType });
                var gType = group.GetType();
                string label = "DC " + width + "x" + height;

                int index = -1;
                int total = (int)gType.GetMethod("GetTotalCount").Invoke(group, null);
                var getSize = gType.GetMethod("GetGameViewSize");
                for (int i = 0; i < total; i++)
                {
                    var s = getSize.Invoke(group, new object[] { i });
                    var st = s.GetType();
                    if ((int)st.GetProperty("width").GetValue(s) == width && (int)st.GetProperty("height").GetValue(s) == height &&
                        st.GetProperty("sizeType").GetValue(s).ToString() == "FixedResolution") { index = i; break; }
                }
                if (index < 0)
                {
                    var sizeType = asm.GetType("UnityEditor.GameViewSize");
                    var sizeEnum = asm.GetType("UnityEditor.GameViewSizeType");
                    var ctor = sizeType.GetConstructor(new[] { sizeEnum, typeof(int), typeof(int), typeof(string) });
                    var newSize = ctor.Invoke(new object[] { Enum.Parse(sizeEnum, "FixedResolution"), width, height, label });
                    gType.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                    index = (int)gType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
                }

                var gameViewType = asm.GetType("UnityEditor.GameView");
                var views = Resources.FindObjectsOfTypeAll(gameViewType);
                if (views.Length == 0) EditorWindow.GetWindow(gameViewType);
                foreach (var w in Resources.FindObjectsOfTypeAll(gameViewType))
                {
                    var prop = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null) prop.SetValue(w, index);
                    else gameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?.Invoke(w, new object[] { index, null });
                    ((EditorWindow)w).Repaint();
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[DataChampions] Could not set Game view size: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Pauses play mode and steps frames until `seconds` of game time have passed (each step = captureDeltaTime).
        /// Runs synchronously inside one command, so coroutines and DOTween advance even when the Editor is unfocused.
        /// </summary>
        public static int Advance(float seconds, float frameDt = 1f / 30f)
        {
            if (!EditorApplication.isPlaying) return 0;
            Time.captureDeltaTime = frameDt;
            EditorApplication.isPaused = true;
            float start = Time.unscaledTime;
            int steps = 0;
            double guard = EditorApplication.timeSinceStartup + 120.0;
            while (Time.unscaledTime - start < seconds && EditorApplication.timeSinceStartup < guard)
            {
                EditorApplication.Step();
                steps++;
            }
            return steps;
        }

        /// <summary>Game view screenshot (overlay UI included). The file is written on the next frames, so this steps twice.</summary>
        public static string Shot(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path)) File.WriteAllBytes(path, new byte[0]);
            ScreenCapture.CaptureScreenshot(path);
            EditorApplication.Step();
            EditorApplication.Step();
            EditorApplication.Step();
            return path;
        }

        /// <summary>Faster play-mode entry for tests (no domain reload, so the MCP connection survives).</summary>
        public static void FastPlayMode(bool on)
        {
            EditorSettings.enterPlayModeOptionsEnabled = on;
            EditorSettings.enterPlayModeOptions = on ? EnterPlayModeOptions.DisableDomainReload : EnterPlayModeOptions.None;
        }
    }
}
