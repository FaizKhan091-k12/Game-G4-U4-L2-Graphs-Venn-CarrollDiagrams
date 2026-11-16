using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DataChampions
{
    /// <summary>Top bar during play: pause, ten progress dots, streak flame, coins.</summary>
    public class Hud : UIScreen
    {
        public Button pause;
        public RectTransform coinPill, coinIcon, streakChip;
        public CountingLabel coins;
        public TextMeshProUGUI streakText, progressText;
        public ProceduralImage[] dots;

        public Vector3 CoinTarget => coinIcon.position;

        public void SetDots(int[] states, int current)
        {
            // states: 0 open, 1 right, 2 wrong
            int answered = 0;
            for (int i = 0; i < dots.Length; i++)
            {
                var c = states[i] == 1 ? Palette.Correct : states[i] == 2 ? Palette.Wrong : Palette.WithA(Color.white, 0.25f);
                dots[i].color = c;
                dots[i].transform.localScale = Vector3.one * (i == current ? 1.35f : 1f);
                if (states[i] != 0) answered++;
            }
            progressText.text = answered + " / " + dots.Length;
        }

        public void PunchDot(int i)
        {
            if (i < 0 || i >= dots.Length) return;
            dots[i].transform.DOKill(true);
            dots[i].transform.DOPunchScale(Vector3.one * 0.8f, 0.45f, 6, 0.5f).SetUpdate(true);
        }

        public void SetStreak(int streak, bool animate)
        {
            bool show = streak >= 2;
            streakText.text = "×" + streak;
            if (show && !streakChip.gameObject.activeSelf)
            {
                streakChip.gameObject.SetActive(true);
                UIScreen.PopIn(streakChip, 0f);
            }
            else if (!show && streakChip.gameObject.activeSelf)
            {
                streakChip.DOKill();
                streakChip.DOScale(0f, 0.2f).SetUpdate(true).OnComplete(() => streakChip.gameObject.SetActive(false));
            }
            else if (show && animate)
            {
                streakChip.DOKill(true);
                streakChip.localScale = Vector3.one;
                streakChip.DOPunchScale(Vector3.one * 0.35f, 0.45f, 7, 0.6f).SetUpdate(true);
            }
        }

        public static Hud Build(Transform parent, GameConfig cfg)
        {
            var h = Make<Hud>(parent, "Hud");
            var root = h.transform;
            h.pause = UIFactory.RoundButton(root, "Pause", cfg.S("pause"), new Vector2(0, 1), new Vector2(72, -62), 92, Palette.Purple, Palette.PurpleLip);

            // progress dots
            var bar = UIFactory.Rect(root, "Progress", new Vector2(0.5f, 1f), new Vector2(0, -56), new Vector2(560, 70));
            UIFactory.Rounded(bar, Palette.WithA(Palette.StudioTop, 0.55f), 35);
            h.dots = new ProceduralImage[10];
            for (int i = 0; i < 10; i++)
            {
                var d = UIFactory.Rect(bar, "Dot" + i, UIFactory.Center, new Vector2(-240 + i * 38, 0), new Vector2(26, 26));
                h.dots[i] = UIFactory.Rounded(d, Palette.WithA(Color.white, 0.25f), -1);
            }
            h.progressText = UIFactory.Text(bar, "Count", "0 / 10", new Vector2(1f, 0.5f), new Vector2(-24, 0), new Vector2(110, 60), 34, Color.white, TextAlignmentOptions.Right, true);
            ((RectTransform)h.progressText.transform.parent).pivot = new Vector2(1f, 0.5f);
            bar.sizeDelta = new Vector2(560, 70);
            foreach (var d in h.dots) ((RectTransform)d.transform).anchoredPosition += new Vector2(-50, 0);

            // coins
            h.coinPill = UIFactory.Rect(root, "Coins", new Vector2(1, 1), new Vector2(-28, -18), new Vector2(290, 82), new Vector2(1, 1));
            var lip = UIFactory.Stretch(h.coinPill, "Lip");
            lip.offsetMin = new Vector2(0, -8); lip.offsetMax = new Vector2(0, -8);
            UIFactory.Rounded(lip, Palette.GoldLip, 45);
            UIFactory.Rounded(UIFactory.Stretch(h.coinPill, "Face"), Palette.Gold, 45);
            h.coinIcon = UIFactory.Icon(h.coinPill, "Coin", cfg.S("fl_coin"), new Vector2(0, 0.5f), new Vector2(46, 2), 76).rectTransform;
            var lbl = UIFactory.Text(h.coinPill, "Value", "0", UIFactory.Center, new Vector2(40, 2), new Vector2(200, 80), 58, Color.white, TextAlignmentOptions.Center, true);
            h.coins = h.coinPill.gameObject.AddComponent<CountingLabel>();
            h.coins.label = lbl;

            // streak
            h.streakChip = UIFactory.Rect(root, "Streak", new Vector2(1, 1), new Vector2(-336, -18), new Vector2(180, 82), new Vector2(1, 1));
            var sl = UIFactory.Stretch(h.streakChip, "Lip");
            sl.offsetMin = new Vector2(0, -8); sl.offsetMax = new Vector2(0, -8);
            UIFactory.Rounded(sl, Palette.RiskLip, 45);
            UIFactory.Rounded(UIFactory.Stretch(h.streakChip, "Face"), Palette.Risk, 45);
            UIFactory.Icon(h.streakChip, "Fire", cfg.S("fl_fire"), new Vector2(0, 0.5f), new Vector2(44, 5), 72);
            h.streakText = UIFactory.Text(h.streakChip, "Value", "×2", UIFactory.Center, new Vector2(38, 2), new Vector2(110, 80), 56, Color.white, TextAlignmentOptions.Center, true);
            h.streakChip.gameObject.SetActive(false);
            return h;
        }
    }
}
