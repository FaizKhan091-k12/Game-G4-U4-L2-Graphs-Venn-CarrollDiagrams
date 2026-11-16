using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace DataChampions.EditorTools
{
    /// <summary>
    /// One-click generator for "Data Champions": painted icons, font, audio import settings, the Edovu logo,
    /// the single game scene (all screens baked) and WebGL-friendly project settings.
    /// Re-running it rebuilds everything generated (hand edits to the generated scene are overwritten).
    /// </summary>
    public static partial class DataChampionsBuilder
    {
        public const string Root = "Assets/DataChampions";
        const string Gen = Root + "/Generated";
        const string TexDir = Gen + "/Textures";
        const string ResourcesDir = Root + "/Resources";
        const string AudioDir = Root + "/Audio";
        public const string ScenePath = Root + "/Scenes/DataChampions.unity";

        [MenuItem("Data Champions/Build Game (all)", priority = 0)]
        public static void BuildAll()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Data Champions", "Assets", 0.1f);
                var cfg = BuildAssets();
                EditorUtility.DisplayProgressBar("Data Champions", "Scene", 0.6f);
                BuildScene(cfg);
                ConfigureProject();
                ConfigureRendering();
                AssetDatabase.SaveAssets();
                Debug.Log("[DataChampions] Build complete.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Data Champions/Rebuild Scene Only", priority = 1)]
        public static void BuildSceneOnly()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ResourcesDir + "/" + GameConfig.ResourcePath + ".asset");
            if (cfg == null) { BuildAll(); return; }
            BuildScene(cfg);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ------------------------------------------------------------------ assets

        static GameConfig BuildAssets()
        {
            foreach (var f in new[] { TexDir, ResourcesDir, Root + "/Scenes", Root + "/Fonts" }) EnsureFolder(f);
            string cfgPath = ResourcesDir + "/" + GameConfig.ResourcePath + ".asset";
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(cfgPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(cfg, cfgPath);
            }
            BuildIcons(cfg);
            BuildFont(cfg);
            SetupAudio(cfg);
            cfg.edovuLogo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Edovu Logo .prefab");
            cfg.logoSprite = BuildLogo("Assets/Grade 6 L1/Logo High.png", 512);
            // the web page's own loading screen shows the same logo before Unity starts
            string logoPng = TexDir + "/EdovuLogo512.png";
            const string templateData = "Assets/WebGLTemplates/DataChampions/TemplateData";
            if (File.Exists(logoPng) && Directory.Exists(templateData))
            {
                File.Copy(logoPng, templateData + "/edovu-logo.png", true);
                File.Copy(logoPng, templateData + "/favicon.png", true);
                AssetDatabase.ImportAsset(templateData + "/edovu-logo.png");
                AssetDatabase.ImportAsset(templateData + "/favicon.png");
            }
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            return cfg;
        }

        /// <summary>
        /// The source logo is 4096 px but shows at 130-560 px; on WebGL Unity would cap and compress it, smearing the
        /// tagline. This bakes a clean copy (successive 2x GPU halvings act as a box filter), uncompressed, trilinear.
        /// </summary>
        static Sprite BuildLogo(string sourcePath, int size)
        {
            if (!File.Exists(sourcePath)) { Debug.LogWarning("[DataChampions] logo not found: " + sourcePath); return null; }
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(File.ReadAllBytes(sourcePath));
            Texture current = src;
            var temps = new List<RenderTexture>();
            int w = src.width, h = src.height;
            while (w > size || h > size)
            {
                w = Mathf.Max(size, w / 2);
                h = Mathf.Max(size, h / 2);
                var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                rt.filterMode = FilterMode.Bilinear;
                Graphics.Blit(current, rt);
                temps.Add(rt);
                current = rt;
            }
            var prev = RenderTexture.active;
            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            if (current is RenderTexture last)
            {
                RenderTexture.active = last;
                outTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                outTex.Apply();
            }
            else outTex.SetPixels32(src.GetPixels32());
            RenderTexture.active = prev;
            foreach (var t in temps) RenderTexture.ReleaseTemporary(t);

            string path = TexDir + "/EdovuLogo" + size + ".png";
            File.WriteAllBytes(path, outTex.EncodeToPNG());
            Object.DestroyImmediate(src);
            Object.DestroyImmediate(outTex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.sRGBTexture = true;
            ti.mipmapEnabled = true;
            ti.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            ti.filterMode = FilterMode.Trilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = size;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            foreach (var platform in new[] { "WebGL", "Standalone", "Android", "iPhone" })
            {
                var ps = ti.GetPlatformTextureSettings(platform);
                ps.overridden = true;
                ps.maxTextureSize = size;
                ps.format = TextureImporterFormat.RGBA32;
                ps.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SetPlatformTextureSettings(ps);
            }
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void BuildFont(GameConfig cfg)
        {
            string ttfPath = Root + "/Fonts/LilitaOne-Regular.ttf";
            string assetPath = Root + "/Fonts/LilitaOne SDF.asset";
            const string chars = " ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!?.,:;'\"+-%/()<>*=&#×•’";
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fa == null && font != null)
            {
                fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
                fa.name = "LilitaOne SDF";
                AssetDatabase.CreateAsset(fa, assetPath);
                fa.atlasTextures[0].name = "LilitaOne Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
                fa.material.name = "LilitaOne Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
                if (!fa.TryAddCharacters(chars, out string missing) && !string.IsNullOrEmpty(missing))
                    Debug.LogWarning("[DataChampions] font is missing characters: " + missing);
                EditorUtility.SetDirty(fa);
                AssetDatabase.SaveAssets();
            }
            if (fa == null) { Debug.LogWarning("[DataChampions] Lilita One font missing, using the TMP default."); return; }
            if (fa.atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                // every string is known ASCII + a few symbols: bake them and freeze the atlas (stable and cheap on WebGL)
                fa.TryAddCharacters(chars);
                fa.atlasPopulationMode = AtlasPopulationMode.Static;
                EditorUtility.SetDirty(fa);
            }
            cfg.font = fa;

            string outlinePath = Root + "/Fonts/LilitaOne Outline.mat";
            var outline = AssetDatabase.LoadAssetAtPath<Material>(outlinePath);
            if (outline == null) { outline = new Material(fa.material); AssetDatabase.CreateAsset(outline, outlinePath); }
            outline.CopyPropertiesFromMaterial(fa.material);
            outline.EnableKeyword("OUTLINE_ON");
            outline.SetFloat("_OutlineWidth", 0.22f);
            outline.SetColor("_OutlineColor", Palette.Hex(0x23124F));
            outline.SetFloat("_FaceDilate", 0.22f);
            outline.EnableKeyword("UNDERLAY_ON");
            outline.SetColor("_UnderlayColor", new Color(0.08f, 0.02f, 0.2f, 0.55f));
            outline.SetFloat("_UnderlayOffsetY", -0.9f);
            outline.SetFloat("_UnderlaySoftness", 0.1f);
            EditorUtility.SetDirty(outline);
            cfg.fontOutline = outline;

            var settings = TMP_Settings.instance;
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                var p = so.FindProperty("m_defaultFontAsset");
                if (p != null) { p.objectReferenceValue = fa; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
        }

        static void SetupAudio(GameConfig cfg)
        {
            var vol = new Dictionary<Sfx, float>
            {
                { Sfx.UiClick, 0.7f }, { Sfx.UiHover, 0.45f }, { Sfx.TileFlip, 0.8f }, { Sfx.Whoosh, 0.55f }, { Sfx.Pop, 0.6f },
                { Sfx.Slide, 0.5f }, { Sfx.BarGrow, 0.5f }, { Sfx.Correct, 0.85f }, { Sfx.Wrong, 0.6f }, { Sfx.Coin, 0.7f },
                { Sfx.CoinLose, 0.7f }, { Sfx.Risk, 0.75f }, { Sfx.Safe, 0.7f }, { Sfx.Drumroll, 0.6f }, { Sfx.Streak, 0.75f },
                { Sfx.Star, 0.85f }, { Sfx.Fanfare, 0.75f }, { Sfx.Badge, 0.8f }, { Sfx.Tick, 0.7f }, { Sfx.Sparkle, 0.55f },
                { Sfx.Hint, 0.75f }, { Sfx.Cheer, 0.6f }, { Sfx.Aww, 0.6f }, { Sfx.Hoot, 0.7f }, { Sfx.Logo, 0.8f },
            };
            cfg.sfx.Clear();
            foreach (Sfx id in System.Enum.GetValues(typeof(Sfx)))
            {
                string file = Regex.Replace(id.ToString(), "(?<=[a-z])([A-Z])", "_$1").ToLowerInvariant();
                string path = AudioDir + "/SFX/" + file + ".mp3";
                ConfigureClip(path, AudioClipLoadType.DecompressOnLoad, 0.5f, true);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) { Debug.LogWarning("[DataChampions] missing sfx " + path); continue; }
                cfg.sfx.Add(new GameConfig.SfxEntry { id = id, clip = clip, volume = vol.TryGetValue(id, out var v) ? v : 0.8f });
            }
            cfg.voiceLines.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir + "/Voice" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ConfigureClip(path, AudioClipLoadType.CompressedInMemory, 0.45f, true);
                cfg.voiceLines.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }
            foreach (var m in new[] { "music_main", "music_think" })
                ConfigureClip(AudioDir + "/Music/" + m + ".mp3", AudioClipLoadType.Streaming, 0.4f, false);
            cfg.musicMain = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/Music/music_main.mp3");
            cfg.musicThink = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/Music/music_think.mp3");
        }

        static void ConfigureClip(string path, AudioClipLoadType load, float quality, bool mono)
        {
            var ai = AssetImporter.GetAtPath(path) as AudioImporter;
            if (ai == null) return;
            ai.forceToMono = mono;
            ai.loadInBackground = load == AudioClipLoadType.Streaming;
            var s = ai.defaultSampleSettings;
            s.loadType = load;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = quality;
            s.preloadAudioData = load != AudioClipLoadType.Streaming;
            ai.defaultSampleSettings = s;
            ai.SaveAndReimport();
        }

        // ------------------------------------------------------------------ scene

        static void BuildScene(GameConfig cfg)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.StudioTop;
            cam.cullingMask = 0;          // the whole game is UI on overlay canvases
            cam.allowMSAA = false;
            cam.allowHDR = false;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = false;
            camData.antialiasing = AntialiasingMode.None;
            camGo.transform.position = new Vector3(0, 0, -10);

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();
            try { module.AssignDefaultActions(); } catch (System.Exception e) { Debug.LogWarning("[DataChampions] UI input defaults: " + e.Message); }

            var rootGo = new GameObject("DataChampions");
            var flow = rootGo.AddComponent<GameFlow>();
            flow.config = cfg;
            flow.audioManager = rootGo.AddComponent<GameAudio>();

            flow.studio = Studio.Build(rootGo.transform, cfg);

            var ui = UIFactory.Canvas(rootGo.transform, "UICanvas", 0);
            var safe = UIFactory.Stretch(ui.transform, "Safe");
            safe.gameObject.AddComponent<SafeArea>();
            flow.title = TitleScreen.Build(safe, cfg);
            flow.board = BoardScreen.Build(safe, cfg);
            flow.question = QuestionScreen.Build(safe, cfg);
            flow.results = ResultsScreen.Build(safe, cfg);
            flow.hud = Hud.Build(safe, cfg);
            flow.howTo = HowToScreen.Build(safe, cfg);
            flow.progress = ProgressScreen.Build(safe, cfg);
            flow.pause = PauseScreen.Build(safe, cfg);

            var fxCanvas = UIFactory.Canvas(rootGo.transform, "FxCanvas", 20);
            Object.DestroyImmediate(fxCanvas.GetComponent<GraphicRaycaster>());
            flow.fxRoot = UIFactory.Stretch(fxCanvas.transform, "FxRoot");
            flow.fx = fxCanvas.gameObject.AddComponent<UIFx>();

            var top = UIFactory.Canvas(rootGo.transform, "TopCanvas", 30);
            var topSafe = UIFactory.Stretch(top.transform, "Safe");
            topSafe.gameObject.AddComponent<SafeArea>();
            flow.edovuCorner = UIFactory.EdovuLogo(topSafe, cfg.edovuLogo, cfg.logoSprite);
            flow.splash = SplashScreen.Build(top.transform, cfg);
            flow.fader = Fader.Build(top.transform);
            flow.rotate = RotatePrompt.Build(top.transform, cfg);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[DataChampions] Scene saved: " + ScenePath);
        }

        // ------------------------------------------------------------------ project & rendering

        static void ConfigureProject()
        {
            PlayerSettings.productName = "Data Champions";
            PlayerSettings.companyName = "Edovu";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            try { PlayerSettings.SplashScreen.show = false; } catch (System.Exception e) { Debug.LogWarning("[DataChampions] splash: " + e.Message); }
            // full-window, landscape-first page with an Edovu loader (Assets/WebGLTemplates/DataChampions)
            PlayerSettings.WebGL.template = "PROJECT:DataChampions";
            // Uncompressed output: plain .data/.wasm/.framework.js files that the post-build "WebGL auto fix" tool
            // renames to build.*, and that run on any static server (Live Server, LMS) without Content-Encoding headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.Default;
            PlayerSettings.stripEngineCode = true;
            // Low stripping keeps DOTween's reflection-based bits safe.
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
            UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.RuntimeSpeed;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        /// <summary>UI-only game: no MSAA, no HDR, no post, no shadows; the GPU only fills the canvases.</summary>
        static void ConfigureRendering()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (rp == null) continue;
                rp.msaaSampleCount = 1;
                rp.supportsHDR = false;
                rp.renderScale = 1f;
                rp.shadowDistance = 0f;
                rp.supportsCameraDepthTexture = false;
                rp.supportsCameraOpaqueTexture = false;
                EditorUtility.SetDirty(rp);
            }
        }
    }
}
