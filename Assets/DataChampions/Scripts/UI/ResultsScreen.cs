using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DataChampions
{
    /// <summary>End of the show: stars stamp in, coins count up, stats, new badges, Play again / Progress / Home.</summary>
    public class ResultsScreen : UIScreen
    {
        public RectTransform panel, ribbon, newBest, badgeRow;
        public Image[] stars;
        public CountingLabel score;
        public TextMeshProUGUI correctText, riskText, streakText, badgeHint;
        public Button again, progress, home;
        public Owl owl;
        public SpeechBubble bubble;

        public IEnumerator Play(GameRecord rec, bool isNewBest, List<string> newBadges, System.Action<string> speak)
        {
            score.Set(0);
            correctText.text = rec.correct + " / 10";
            riskText.text = rec.risksWon + " / " + rec.risksTaken;
            streakText.text = rec.bestStreak.ToString();
            newBest.gameObject.SetActive(false);
            foreach (Transform c in badgeRow) Destroy(c.gameObject);
            badgeHint.gameObject.SetActive(false);
            foreach (var s in stars) { s.color = Palette.WithA(Palette.StudioTop, 0.25f); s.transform.localScale = Vector3.one; }
            again.gameObject.SetActive(false);
            progress.gameObject.SetActive(false);
            home.gameObject.SetActive(false);

            PopIn(panel, 0f, 0.5f);
            PopIn(ribbon, 0.3f);
            owl.Enter(0.2f);
            yield return new WaitForSeconds(0.7f);

            GameAudio.I?.Play(Sfx.Drumroll);
            yield return new WaitForSeconds(0.9f);
            for (int i = 0; i < 3; i++)
            {
                if (i >= rec.stars) break;
                var st = stars[i];
                st.color = Palette.Gold;
                st.transform.localScale = Vector3.one * 2.2f;
                st.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
                st.transform.DOPunchRotation(new Vector3(0, 0, 25f), 0.5f, 8, 0.6f);
                GameAudio.I?.Play(Sfx.Star, 1f + i * 0.12f);
                UIFx.Burst(st.transform.position, Palette.Gold, 14, 180f);
                yield return new WaitForSeconds(0.38f);
            }
            GameAudio.I?.Play(Sfx.Fanfare);
            UIFx.Confetti(rec.stars >= 2 ? 80 : 40);
            if (rec.stars >= 2) owl.Cheer(); else owl.Happy();
            speak("result_" + Mathf.Clamp(rec.stars, 1, 3));

            GameAudio.I?.Play(Sfx.Coin);
            score.AnimateTo(rec.score, 1.2f);
            yield return new WaitForSeconds(1.3f);
            if (isNewBest)
            {
                newBest.gameObject.SetActive(true);
                newBest.localScale = Vector3.one * 2.5f;
                newBest.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                GameAudio.I?.Play(Sfx.Star, 1.3f);
                UIFx.Burst(newBest.position, Palette.Pink, 12, 150f);
            }

            if (newBadges.Count > 0)
            {
                for (int i = 0; i < newBadges.Count; i++)
                {
                    var def = Badges.ById(newBadges[i]);
                    if (def == null) continue;
                    var b = BadgeIcon(badgeRow, def, new Vector2((i - (newBadges.Count - 1) * 0.5f) * 150f, 0), 120, true);
                    PopIn(b, i * 0.2f, 0.45f);
                    int idx = i;
                    DOVirtual.DelayedCall(i * 0.2f, () =>
                    {
                        GameAudio.I?.Play(Sfx.Badge, 1f + idx * 0.08f);
                        UIFx.Burst(b.position, Palette.Gold, 10, 120f);
                    }).SetLink(b.gameObject);
                }
            }
            else
            {
                badgeHint.gameObject.SetActive(true);
                PopIn(badgeHint.transform, 0f);
            }

            again.gameObject.SetActive(true);
            progress.gameObject.SetActive(true);
            home.gameObject.SetActive(true);
            PopIn(again.transform, 0.3f);
            PopIn(progress.transform, 0.4f);
            PopIn(home.transform, 0.5f);

            // wait for the result line, then the follow-ups
            yield return new WaitForSeconds(0.5f);
            while (GameAudio.I != null && GameAudio.I.IsVoicePlaying) yield return null;
            if (isNewBest)
            {
                speak("new_best");
                yield return new WaitForSeconds(0.3f);
                while (GameAudio.I != null && GameAudio.I.IsVoicePlaying) yield return null;
            }
            if (newBadges.Count > 0) speak("badge");
        }

        public static RectTransform BadgeIcon(Transform parent, BadgeDef def, Vector2 pos, float size, bool unlocked)
        {
            var rt = UIFactory.Rect(parent, "Badge_" + def.id, UIFactory.Center, pos, new Vector2(size, size));
            var lip = UIFactory.Stretch(rt, "Lip");
            lip.offsetMin = new Vector2(0, -7); lip.offsetMax = new Vector2(0, -7);
            UIFactory.Rounded(lip, unlocked ? Palette.GoldLip : Palette.LockedLip, -1);
            UIFactory.Rounded(UIFactory.Stretch(rt, "Face"), unlocked ? Palette.Gold : Palette.Locked, -1);
            UIFactory.Rounded(UIFactory.Stretch(rt, "Inner", size * 0.09f), unlocked ? Palette.Hex(0xFFF3C4) : Palette.Hex(0xB9B3D6), -1);
            var ic = UIFactory.Icon(rt, "Icon", GameConfig.Get(def.icon), UIFactory.Center, Vector2.zero, size * 0.62f, unlocked ? Color.white : new Color(0.3f, 0.27f, 0.45f, 0.55f));
            if (!unlocked) UIFactory.Icon(rt, "Lock", GameConfig.Get("fl_locked"), new Vector2(1f, 0f), new Vector2(-size * 0.12f, size * 0.12f), size * 0.36f);
            return rt;
        }

        public static ResultsScreen Build(Transform parent, GameConfig cfg)
        {
            var r = Make<ResultsScreen>(parent, "Results");
            var root = r.transform;
            var face = UIFactory.Panel(root, "Panel", UIFactory.Center, new Vector2(150, -20), new Vector2(1180, 880), Palette.Cream, Palette.GoldLip, 60, 18f);
            r.panel = (RectTransform)face.transform.parent;
            UIFactory.Rounded(UIFactory.Stretch(face.transform, "Frame"), Palette.Gold, 60, 12f);
            r.ribbon = HowToScreen.Ribbon(r.panel, "SHOW COMPLETE!", new Vector2(0, 430), Palette.Pink, Palette.PinkLip, 700);

            r.stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 210f;
                float y = i == 1 ? 262f : 236f;
                float s = i == 1 ? 200f : 160f;
                UIFactory.Icon(r.panel, "StarBack" + i, cfg.S("star"), UIFactory.Center, new Vector2(x, y - 8), s + 14, Palette.WithA(Palette.GoldLip, 0.5f));
                r.stars[i] = UIFactory.Icon(r.panel, "Star" + i, cfg.S("star"), UIFactory.Center, new Vector2(x, y), s, Palette.Gold);
            }

            var scoreRt = UIFactory.Rect(r.panel, "Score", UIFactory.Center, new Vector2(0, 92), new Vector2(560, 130));
            UIFactory.Rounded(scoreRt, Palette.Hex(0x3A1678), 65);
            UIFactory.Icon(scoreRt, "Coin", cfg.S("fl_coin"), new Vector2(0, 0.5f), new Vector2(72, 0), 110);
            var sl = UIFactory.Text(scoreRt, "Value", "0", UIFactory.Center, new Vector2(50, 4), new Vector2(420, 120), 100, Palette.Gold, TextAlignmentOptions.Center, true);
            r.score = scoreRt.gameObject.AddComponent<CountingLabel>();
            r.score.label = sl;

            r.newBest = UIFactory.Rect(r.panel, "NewBest", UIFactory.Center, new Vector2(390, 120), new Vector2(250, 84));
            r.newBest.localRotation = Quaternion.Euler(0, 0, -12f);
            UIFactory.Rounded(r.newBest, Palette.Pink, 20);
            UIFactory.Rounded(UIFactory.Stretch(r.newBest, "Edge", 6), Color.white, 16, 4f);
            UIFactory.Label(r.newBest, "T", "NEW BEST!", 42, Color.white, TextAlignmentOptions.Center, true);

            string[] labels = { "Correct", "Risks won", "Best streak" };
            string[] icons = { "fl_check_mark_button", "fl_fire", "fl_high_voltage" };
            var vals = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                var chip = UIFactory.Rect(r.panel, "Stat" + i, UIFactory.Center, new Vector2((i - 1) * 350f, -70), new Vector2(320, 120));
                UIFactory.Rounded(chip, Color.white, 30);
                UIFactory.Rounded(UIFactory.Stretch(chip, "Edge"), Palette.Line, 30, 4f);
                UIFactory.Icon(chip, "Icon", cfg.S(icons[i]), new Vector2(0, 0.5f), new Vector2(56, 0), 74);
                UIFactory.Text(chip, "Label", labels[i], UIFactory.Center, new Vector2(42, 28), new Vector2(220, 44), 30, Palette.InkSoft);
                vals[i] = UIFactory.Text(chip, "Value", "0", UIFactory.Center, new Vector2(42, -18), new Vector2(220, 60), 50, Palette.Ink);
            }
            r.correctText = vals[0];
            r.riskText = vals[1];
            r.streakText = vals[2];

            r.badgeRow = UIFactory.Rect(r.panel, "Badges", UIFactory.Center, new Vector2(0, -215), new Vector2(1000, 130));
            r.badgeHint = UIFactory.Text(r.panel, "BadgeHint", "Win a badge next time: try a Perfect Ten!", UIFactory.Center, new Vector2(0, -215), new Vector2(1000, 60), 38, Palette.InkSoft);

            r.again = UIFactory.Button(r.panel, "Again", "PLAY AGAIN", UIFactory.Center, new Vector2(-215, -360), new Vector2(440, 130), Palette.Orange, Palette.OrangeLip, 56, cfg.S("replay"));
            r.again.GetComponent<UIButtonFX>().idlePulse = true;
            r.progress = UIFactory.Button(r.panel, "Progress", "PROGRESS", UIFactory.Center, new Vector2(200, -360), new Vector2(350, 130), Palette.Blue, Palette.BlueLip, 40, cfg.S("fl_trophy"));
            r.home = UIFactory.RoundButton(r.panel, "Home", cfg.S("home"), UIFactory.Center, new Vector2(458, -360), 124, Palette.Purple, Palette.PurpleLip);

            r.owl = Owl.Build(root, "Owl", new Vector2(0.5f, 0f), new Vector2(-790, 10), 0.82f);
            r.bubble = SpeechBubble.Build(root, "Bubble", new Vector2(0.5f, 0f), new Vector2(-800, 520), new Vector2(0.5f, 0f), 360, 34, true);
            return r;
        }
    }
}
