using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DataChampions
{
    /// <summary>How to play: three quick steps and the coin rules.</summary>
    public class HowToScreen : UIScreen
    {
        public RectTransform panel;
        public RectTransform[] steps;
        public Button go;

        public void PlayIntro()
        {
            PopIn(panel, 0f, 0.45f);
            for (int i = 0; i < steps.Length; i++) PopIn(steps[i], 0.25f + i * 0.18f);
            PopIn(go.transform, 0.9f);
        }

        public static HowToScreen Build(Transform parent, GameConfig cfg)
        {
            var h = Make<HowToScreen>(parent, "HowTo");
            UIFactory.Dim(h.transform, 0.7f).raycastTarget = true;
            var p = UIFactory.Panel(h.transform, "Panel", UIFactory.Center, new Vector2(-30, -5), new Vector2(1560, 900), Palette.Cream, Palette.GoldLip, 56, 18f);
            h.panel = (RectTransform)p.transform.parent;
            var face = p.transform;
            UIFactory.Rounded(UIFactory.Stretch(face, "Frame"), Palette.Gold, 56, 10f);
            Ribbon(h.panel, "HOW TO PLAY", new Vector2(0, 372), Palette.Purple, Palette.PurpleLip, 640);

            string[] titles = { "Pick a tile", "SAFE or RISK IT?", "Read and answer" };
            string[] lines = { "Every tile hides a question. Bigger tiles win more coins!", "Choose BEFORE you see the question. RISK IT wins double, or loses the coins!", "The clock starts when the question appears: 45 seconds!" };
            h.steps = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                var card = UIFactory.Panel(h.panel, "Step" + i, UIFactory.Center, new Vector2(-490 + i * 490, 50), new Vector2(440, 450), Color.white, Palette.Line, 40, 10f, false);
                var c = (RectTransform)card.transform.parent;
                h.steps[i] = c;
                var badge = UIFactory.Rect(c, "Num", new Vector2(0, 1), new Vector2(40, -36), new Vector2(64, 64));
                UIFactory.Rounded(badge, Palette.Orange, -1);
                UIFactory.Label(badge, "T", (i + 1).ToString(), 50, Color.white, TextAlignmentOptions.Center, true);
                var art = UIFactory.Rect(c, "Art", UIFactory.Center, new Vector2(0, 72), new Vector2(300, 190));
                if (i == 0)
                {
                    var tile = UIFactory.Panel(art, "Tile", UIFactory.Center, Vector2.zero, new Vector2(170, 160), Palette.Orange, Palette.OrangeLip, 30, 12f, false);
                    UIFactory.Label(tile.transform, "N", "7", 104, Color.white, TextAlignmentOptions.Center, true).rectTransform.offsetMin = new Vector2(0, 30);
                    var pts = UIFactory.Text(tile.transform, "P", "200", new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(160, 40), 34, Palette.GoldLight, TextAlignmentOptions.Center, true);
                    UIFactory.Icon(art, "Sparkles", cfg.S("fl_sparkles"), UIFactory.Center, new Vector2(100, -60), 90);
                }
                else if (i == 2) UIFactory.Icon(art, "Chart", cfg.S("fl_bar_chart"), UIFactory.Center, Vector2.zero, 190);
                else
                {
                    UIFactory.Icon(art, "Shield", cfg.S("fl_shield"), UIFactory.Center, new Vector2(-70, 0), 136);
                    UIFactory.Icon(art, "Fire", cfg.S("fl_fire"), UIFactory.Center, new Vector2(74, 0), 136);
                }
                UIFactory.Paragraph(c, "Title", titles[i], UIFactory.Center, new Vector2(0, -70), new Vector2(400, 64), 46, 30, Palette.Ink);
                UIFactory.Paragraph(c, "Line", lines[i], UIFactory.Center, new Vector2(0, -160), new Vector2(390, 110), 32, 22, Palette.InkSoft);
            }
            var rule = UIFactory.Rect(h.panel, "Rule", UIFactory.Center, new Vector2(0, -232), new Vector2(1240, 70));
            UIFactory.Rounded(rule, Palette.Hex(0xFFF1CF), 35);
            UIFactory.Icon(rule, "Bulb", cfg.S("fl_light_bulb"), new Vector2(0, 0.5f), new Vector2(50, 0), 58);
            UIFactory.Label(rule, "T", "Right within 20 seconds: +50.  3 in a row: +50.  A hint costs 50.", 34, Palette.Ink).rectTransform.offsetMin = new Vector2(70, 0);
            h.go = UIFactory.Button(h.panel, "Go", "LET'S GO!", UIFactory.Center, new Vector2(0, -352), new Vector2(460, 130), Palette.Green, Palette.GreenLip, 64, cfg.S("play"));
            return h;
        }

        public static RectTransform Ribbon(Transform parent, string text, Vector2 pos, Color face, Color lip, float width)
        {
            var r = UIFactory.Rect(parent, "Ribbon", UIFactory.Center, pos, new Vector2(width, 100));
            var l = UIFactory.Stretch(r, "Lip");
            l.offsetMin = new Vector2(0, -10); l.offsetMax = new Vector2(0, -10);
            UIFactory.Rounded(l, lip, 50);
            UIFactory.Rounded(UIFactory.Stretch(r, "Face"), face, 50);
            UIFactory.Label(r, "T", text, 60, Color.white, TextAlignmentOptions.Center, true).rectTransform.offsetMin = new Vector2(0, 5);
            return r;
        }
    }
}
