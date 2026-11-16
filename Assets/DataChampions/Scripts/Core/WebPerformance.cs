using System.Runtime.InteropServices;
using UnityEngine;

namespace DataChampions
{
    /// <summary>
    /// Web player frame pacing (created automatically in web builds): ticks on requestAnimationFrame (vsync),
    /// capped at 60 fps by DataChampionsWeb.jslib, so 120/144 Hz screens don't burn CPU and battery on extra frames.
    /// Add ?fps=1 to the page URL for a small stats overlay (hidden by default).
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    public class WebPerformance : MonoBehaviour
    {
        const int FrameCap = 60;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void DC_SetFrameCap(int fps);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var go = new GameObject("WebPerformance");
            DontDestroyOnLoad(go);
            go.AddComponent<WebPerformance>();
#endif
        }

        bool overlay;
        float timer, worst;
        int frames;
        string text = "";
        GUIStyle style;

        void Awake()
        {
            // -1 keeps Unity on requestAnimationFrame; a targetFrameRate would switch it to setTimeout.
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 1;
#if UNITY_WEBGL && !UNITY_EDITOR
            DC_SetFrameCap(FrameCap);
#endif
            overlay = Application.absoluteURL.Contains("fps=1");
        }

        void Update()
        {
            if (!overlay) return;
            float dt = Time.unscaledDeltaTime;
            timer += dt;
            frames++;
            worst = Mathf.Max(worst, dt);
            if (timer < 0.5f) return;
            text = $"{frames / timer:0} FPS (cap {FrameCap})   worst {worst * 1000f:0} ms";
            timer = 0f;
            frames = 0;
            worst = 0f;
        }

        void OnGUI()
        {
            if (!overlay) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
                style.normal.textColor = Color.white;
            }
            GUI.Box(new Rect((Screen.width - 420) * 0.5f, Screen.height - 36, 420, 30), text, style);
        }
    }
}
