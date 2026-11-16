using UnityEngine;
using UnityEngine.UI;

namespace DataChampions
{
    /// <summary>
    /// The game-show studio behind every screen: violet gradient, coloured glows, four sweeping spotlight beams,
    /// a stage floor and drifting twinkles. Lives on its own canvas and animates in Update (no per-frame allocations).
    /// </summary>
    public class Studio : MonoBehaviour
    {
        public RectTransform[] beams;
        public float[] beamPhase;
        public Image[] twinkles;
        public Vector2[] twinkleHome;
        public Image glow;
        public float intensity = 1f;

        float targetIntensity = 1f;
        Color glowBase;

        void Start()
        {
            if (glow != null) glowBase = glow.color;
        }

        /// <summary>Brighten the studio (celebrations) or calm it down (reading a diagram).</summary>
        public void SetIntensity(float v) => targetIntensity = v;

        void Update()
        {
            float t = Time.unscaledTime;
            intensity = Mathf.MoveTowards(intensity, targetIntensity, Time.unscaledDeltaTime * 1.5f);
            if (beams != null)
                for (int i = 0; i < beams.Length; i++)
                {
                    if (beams[i] == null) continue;
                    float speed = 0.22f + i * 0.04f;
                    float a = Mathf.Sin(t * speed + beamPhase[i]) * (16f + 6f * intensity) + (i < 2 ? -8f : 8f);
                    beams[i].localRotation = Quaternion.Euler(0, 0, a);
                }
            if (twinkles != null)
                for (int i = 0; i < twinkles.Length; i++)
                {
                    var img = twinkles[i];
                    if (img == null) continue;
                    float p = t * (0.6f + (i % 5) * 0.13f) + i * 1.7f;
                    var c = img.color;
                    c.a = (0.25f + 0.55f * Mathf.Max(0f, Mathf.Sin(p))) * Mathf.Lerp(0.6f, 1f, intensity);
                    img.color = c;
                    float y = Mathf.Repeat(twinkleHome[i].y + t * (8f + (i % 4) * 4f) + 600f, 1200f) - 600f;
                    img.rectTransform.anchoredPosition = new Vector2(twinkleHome[i].x + Mathf.Sin(p * 0.5f) * 12f, y);
                    float s = 0.7f + 0.4f * Mathf.Sin(p * 1.3f);
                    img.rectTransform.localScale = new Vector3(s, s, 1f);
                }
            if (glow != null)
            {
                var c = glowBase;
                c.a = glowBase.a * (0.75f + 0.25f * Mathf.Sin(t * 0.8f)) * Mathf.Lerp(0.7f, 1.25f, intensity);
                glow.color = c;
            }
        }

        public static Studio Build(Transform parent, GameConfig cfg)
        {
            var canvas = UIFactory.Canvas(parent, "StudioCanvas", -10);
            Object.DestroyImmediate(canvas.GetComponent<GraphicRaycaster>());
            var root = (RectTransform)canvas.transform;
            var st = root.gameObject.AddComponent<Studio>();

            var bg = UIFactory.Stretch(root, "Gradient").gameObject.AddComponent<Image>();
            bg.sprite = cfg.S("studio_grad");
            bg.type = Image.Type.Simple;
            bg.raycastTarget = false;

            var glowTop = UIFactory.Icon(root, "GlowTop", cfg.S("soft"), UIFactory.Center, new Vector2(-700, 420), 1300, Palette.WithA(Palette.Blue, 0.16f));
            glowTop.preserveAspect = false;
            glowTop.rectTransform.sizeDelta = new Vector2(1500, 900);
            st.glow = UIFactory.Icon(root, "Glow", cfg.S("soft"), UIFactory.Center, new Vector2(0, -380), 1400, Palette.WithA(Palette.StudioGlow, 0.34f));
            st.glow.preserveAspect = false;
            st.glow.rectTransform.sizeDelta = new Vector2(2400, 1250);

            // spotlight beams hang from the top edge
            var beamColors = new[] { Palette.WithA(Palette.Pink, 0.13f), Palette.WithA(Palette.Blue, 0.12f), Palette.WithA(Palette.Gold, 0.12f), Palette.WithA(Palette.Purple, 0.16f) };
            float[] xs = { -760f, -280f, 280f, 760f };
            st.beams = new RectTransform[4];
            st.beamPhase = new float[4];
            for (int i = 0; i < 4; i++)
            {
                var b = UIFactory.Rect(root, "Beam" + i, new Vector2(0.5f, 1f), new Vector2(xs[i], 40f), new Vector2(560, 1500), new Vector2(0.5f, 1f));
                var img = b.gameObject.AddComponent<Image>();
                img.sprite = cfg.S("beam");
                img.color = beamColors[i];
                img.raycastTarget = false;
                st.beams[i] = b;
                st.beamPhase[i] = i * 1.9f;
            }

            // stage floor
            var floor = UIFactory.Rect(root, "Floor", new Vector2(0.5f, 0f), new Vector2(0, -170), new Vector2(3200, 520));
            UIFactory.Rounded(floor, Palette.WithA(Palette.StudioFloor, 0.85f), -1);
            var rim = UIFactory.Rect(root, "FloorRim", new Vector2(0.5f, 0f), new Vector2(0, -170), new Vector2(3200, 520));
            UIFactory.Rounded(rim, Palette.WithA(Palette.Pink, 0.35f), -1, 5f);

            // drifting twinkles
            const int N = 22;
            st.twinkles = new Image[N];
            st.twinkleHome = new Vector2[N];
            var rnd = new System.Random(7);
            for (int i = 0; i < N; i++)
            {
                var pos = new Vector2((float)(rnd.NextDouble() * 2200 - 1100), (float)(rnd.NextDouble() * 1200 - 600));
                var c = Palette.Confetti[i % 5];
                var img = UIFactory.Icon(root, "Twinkle" + i, cfg.S(i % 3 == 0 ? "star" : "sparkle"), UIFactory.Center, pos, 14 + (i % 4) * 7, Palette.WithA(Color.Lerp(c, Color.white, 0.5f), 0.5f));
                st.twinkles[i] = img;
                st.twinkleHome[i] = pos;
            }
            return st;
        }
    }
}
