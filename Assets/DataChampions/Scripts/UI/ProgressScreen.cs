using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DataChampions
{
    /// <summary>Tracking: totals, accuracy per skill, recent games and the badge shelf.</summary>
    public class ProgressScreen : UIScreen
    {
        public RectTransform panel;
        public TextMeshProUGUI games, best, stars, accuracy;
        public ProceduralImage[] skillFill;
        public TextMeshProUGUI[] skillPct;
        public RectTransform badgeShelf, recentList;
        public TextMeshProUGUI recentEmpty;
        public Button close;

        const float BarWidth = 280f;

        public void Refresh()
        {
            var d = SaveSystem.Data;
            games.text = d.gamesPlayed.ToString();
            best.text = d.bestScore.ToString();
            stars.text = d.bestStars + " / 3";
            accuracy.text = d.totalAnswered == 0 ? "-" : d.AccuracyPercent + "%";
            for (int i = 0; i < Skills.Count; i++)
            {
                int pct = d.SkillPercent(i);
                skillPct[i].text = d.skillAttempts[i] == 0 ? "-" : pct + "%";
                var rt = (RectTransform)skillFill[i].transform;
                rt.DOKill();
                rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);
                rt.DOSizeDelta(new Vector2(Mathf.Max(d.skillAttempts[i] == 0 ? 0f : 24f, BarWidth * pct / 100f), rt.sizeDelta.y), 0.7f)
                    .SetDelay(0.3f + i * 0.1f).SetEase(Ease.OutCubic).SetUpdate(true);
                skillFill[i].color = pct >= 80 ? Palette.Green : pct >= 50 ? Palette.Orange : Palette.Pink;
            }

            foreach (Transform c in badgeShelf) Destroy(c.gameObject);
            for (int i = 0; i < Badges.All.Count; i++)
            {
                var def = Badges.All[i];
                bool have = d.badges.Contains(def.id);
                var pos = new Vector2(-230 + (i % 3) * 230, i < 3 ? 80 : -100);
                var b = ResultsScreen.BadgeIcon(badgeShelf, def, pos, 118, have);
                var t = UIFactory.Paragraph(b, "Title", def.title, new Vector2(0.5f, 0f), new Vector2(0, -30), new Vector2(220, 44), 30, 20, have ? Palette.Ink : Palette.InkSoft);
                PopIn(b, 0.2f + i * 0.06f);
            }

            foreach (Transform c in recentList) Destroy(c.gameObject);
            recentEmpty.gameObject.SetActive(d.history.Count == 0);
            for (int i = 0; i < Mathf.Min(4, d.history.Count); i++)
            {
                var g = d.history[i];
                var row = UIFactory.Rect(recentList, "Game" + i, new Vector2(0.5f, 1f), new Vector2(0, -i * 66f - 30f), new Vector2(620, 58));
                UIFactory.Rounded(row, i % 2 == 0 ? Color.white : Palette.Hex(0xF7F0FF), 29);
                UIFactory.Text(row, "Date", g.date, new Vector2(0, 0.5f), new Vector2(120, 0), new Vector2(220, 50), 28, Palette.InkSoft);
                UIFactory.Icon(row, "Coin", GameConfig.Get("fl_coin"), UIFactory.Center, new Vector2(-10, 0), 44);
                UIFactory.Text(row, "Score", g.score.ToString(), UIFactory.Center, new Vector2(70, 0), new Vector2(130, 50), 34, Palette.Ink);
                for (int s = 0; s < 3; s++)
                    UIFactory.Icon(row, "Star" + s, GameConfig.Get("star"), new Vector2(1, 0.5f), new Vector2(-130 + s * 44, 0), 40, s < g.stars ? Palette.Gold : Palette.WithA(Palette.Ink, 0.15f));
                PopIn(row, 0.2f + i * 0.07f);
            }
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            PopIn(panel, 0f, 0.45f);
            Refresh();
        }

        static TextMeshProUGUI StatTile(Transform parent, string label, string icon, Vector2 pos, Color c)
        {
            var rt = UIFactory.Rect(parent, label, UIFactory.Center, pos, new Vector2(300, 140));
            UIFactory.Rounded(rt, Color.white, 30);
            UIFactory.Rounded(UIFactory.Stretch(rt, "Edge"), Palette.Line, 30, 4f);
            UIFactory.Icon(rt, "Icon", GameConfig.Get(icon), new Vector2(0, 0.5f), new Vector2(58, 0), 76);
            UIFactory.Text(rt, "Label", label, UIFactory.Center, new Vector2(46, 34), new Vector2(200, 40), 28, Palette.InkSoft);
            return UIFactory.Text(rt, "Value", "0", UIFactory.Center, new Vector2(46, -16), new Vector2(200, 64), 54, c);
        }

        public static ProgressScreen Build(Transform parent, GameConfig cfg)
        {
            var p = Make<ProgressScreen>(parent, "Progress");
            UIFactory.Dim(p.transform, 0.6f).raycastTarget = true;
            var face = UIFactory.Panel(p.transform, "Panel", UIFactory.Center, new Vector2(-40, -15), new Vector2(1600, 960), Palette.Cream, Palette.GoldLip, 60, 18f);
            p.panel = (RectTransform)face.transform.parent;
            UIFactory.Rounded(UIFactory.Stretch(face.transform, "Frame"), Palette.Gold, 60, 12f);
            HowToScreen.Ribbon(p.panel, "MY PROGRESS", new Vector2(0, 402), Palette.Blue, Palette.BlueLip, 620);
            p.close = UIFactory.RoundButton(p.panel, "Close", cfg.S("close"), UIFactory.Center, new Vector2(728, 404), 90, Palette.Pink, Palette.PinkLip);

            // left: totals + recent games
            p.games = StatTile(p.panel, "Games played", "fl_party_popper", new Vector2(-590, 235), Palette.Ink);
            p.best = StatTile(p.panel, "Best score", "fl_coin", new Vector2(-270, 235), Palette.GoldLip);
            p.stars = StatTile(p.panel, "Best stars", "fl_glowing_star", new Vector2(-590, 80), Palette.Ink);
            p.accuracy = StatTile(p.panel, "Accuracy", "fl_check_mark_button", new Vector2(-270, 80), Palette.GreenLip);
            UIFactory.Text(p.panel, "RecentTitle", "Recent games", UIFactory.Center, new Vector2(-430, -40), new Vector2(620, 60), 44, Palette.Ink);
            p.recentList = UIFactory.Rect(p.panel, "Recent", UIFactory.Center, new Vector2(-430, -215), new Vector2(620, 290));
            p.recentEmpty = UIFactory.Text(p.panel, "RecentEmpty", "Play a game to see it here!", UIFactory.Center, new Vector2(-430, -160), new Vector2(620, 60), 34, Palette.InkSoft);

            // right: skills + badges
            UIFactory.Text(p.panel, "SkillsTitle", "Skills", UIFactory.Center, new Vector2(380, 290), new Vector2(700, 60), 44, Palette.Ink);
            p.skillFill = new ProceduralImage[Skills.Count];
            p.skillPct = new TextMeshProUGUI[Skills.Count];
            for (int i = 0; i < Skills.Count; i++)
            {
                float y = 225 - i * 68;
                var iconBg = UIFactory.Rect(p.panel, "SkillIcon" + i, UIFactory.Center, new Vector2(70, y), new Vector2(58, 58));
                UIFactory.Rounded(iconBg, Palette.Purple, -1);
                UIFactory.Icon(iconBg, "Icon", cfg.S(Skills.Icons[i]), UIFactory.Center, Vector2.zero, 40, Color.white);
                UIFactory.Text(p.panel, "SkillName" + i, Skills.Names[i], UIFactory.Center, new Vector2(230, y), new Vector2(240, 50), 32, Palette.Ink, TextAlignmentOptions.Left);
                var track = UIFactory.Rect(p.panel, "Track" + i, UIFactory.Center, new Vector2(505, y), new Vector2(BarWidth + 8, 36));
                UIFactory.Rounded(track, Palette.Hex(0xE9E2F7), 18);
                var fill = UIFactory.Rect(track, "Fill", new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(0, 28), new Vector2(0, 0.5f));
                p.skillFill[i] = UIFactory.Rounded(fill, Palette.Green, 14);
                p.skillPct[i] = UIFactory.Text(p.panel, "Pct" + i, "-", UIFactory.Center, new Vector2(705, y), new Vector2(90, 50), 34, Palette.Ink);
            }
            UIFactory.Text(p.panel, "BadgesTitle", "Badges", UIFactory.Center, new Vector2(380, -58), new Vector2(700, 60), 44, Palette.Ink);
            p.badgeShelf = UIFactory.Rect(p.panel, "Shelf", UIFactory.Center, new Vector2(380, -245), new Vector2(700, 300));
            return p;
        }
    }
}
