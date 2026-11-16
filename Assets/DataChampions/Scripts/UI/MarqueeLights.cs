using UnityEngine;
using UnityEngine.UI;

namespace DataChampions
{
    /// <summary>Chasing marquee bulbs around a rounded rectangle (title plate, board header). Steps at 8 Hz.</summary>
    public class MarqueeLights : MonoBehaviour
    {
        public Image[] bulbs;
        public Color on = Palette.GoldLight;
        public Color off = Palette.Hex(0xB3741A);
        public float stepsPerSecond = 8f;

        int step = -1;
        public bool party;

        void Update()
        {
            if (bulbs == null || bulbs.Length == 0) return;
            int s = Mathf.FloorToInt(Time.unscaledTime * (party ? stepsPerSecond * 2f : stepsPerSecond));
            if (s == step) return;
            step = s;
            for (int i = 0; i < bulbs.Length; i++)
            {
                bool lit = party ? ((i + s) % 2 == 0) : ((i + s) % 3 != 0);
                bulbs[i].color = lit ? on : off;
            }
        }

        public static MarqueeLights Build(RectTransform target, float spacing, float bulbSize, float inset)
        {
            var host = UIFactory.SubCanvas(target, "Bulbs", false);
            var m = host.gameObject.AddComponent<MarqueeLights>();
            var size = target.sizeDelta;
            float w = size.x - inset * 2f, h = size.y - inset * 2f;
            int nx = Mathf.Max(2, Mathf.RoundToInt(w / spacing));
            int ny = Mathf.Max(1, Mathf.RoundToInt(h / spacing));
            var list = new System.Collections.Generic.List<Image>();
            var sprite = GameConfig.Get("bulb");
            void Add(float x, float y) => list.Add(UIFactory.Icon(host, "Bulb", sprite, UIFactory.Center, new Vector2(x, y), bulbSize, m.on));
            for (int i = 0; i <= nx; i++) Add(-w / 2 + w * i / nx, h / 2);
            for (int i = 1; i <= ny; i++) Add(w / 2, h / 2 - h * i / ny);
            for (int i = nx - 1; i >= 0; i--) Add(-w / 2 + w * i / nx, -h / 2);
            for (int i = ny - 1; i >= 1; i--) Add(-w / 2, h / 2 - h * i / ny);
            m.bulbs = list.ToArray();
            return m;
        }
    }
}
