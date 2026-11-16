using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DataChampions
{
    /// <summary>Title: marquee plate, Professor Hoot, Play / My Progress, best score, sound toggle, floating doodles.</summary>
    public class TitleScreen : UIScreen
    {
        public RectTransform plate, ribbon, decorRoot;
        public Button play, progress, sound;
        public Image soundIcon;
        public Owl owl;
        public SpeechBubble bubble;
        public TextMeshProUGUI best;
        public MarqueeLights marquee;
        public RectTransform[] decor;
        Vector2[] decorHome;

        public void PlayIntro()
        {
            plate.DOKill(true);
            if (decorHome == null)
            {
                decorHome = new Vector2[decor.Length];
                for (int i = 0; i < decor.Length; i++) decorHome[i] = decor[i].anchoredPosition;
            }
            for (int i = 0; i < decor.Length; i++)
            {
                decor[i].DOKill();
                decor[i].anchoredPosition = decorHome[i];
                decor[i].localRotation = Quaternion.identity;
            }
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            var platePos = plate.anchoredPosition;
            plate.anchoredPosition = platePos + new Vector2(0, 700);
            s.Append(plate.DOAnchorPos(platePos, 0.8f).SetEase(Ease.OutBounce));
            s.Join(PopIn(ribbon, 0.55f));
            s.Join(PopIn(play.transform, 0.75f));
            s.Join(PopIn(progress.transform, 0.85f));
            s.Join(PopIn(best.transform.parent, 0.95f));
            owl.Enter(0.3f);
            marquee.party = true;
            DOVirtual.DelayedCall(1.6f, () => marquee.party = false, true).SetLink(gameObject);
            for (int i = 0; i < decor.Length; i++)
            {
                var d = decor[i];
                PopIn(d, 0.4f + i * 0.07f, 0.5f);
                d.DOAnchorPosY(d.anchoredPosition.y + 18f, 1.6f + (i % 3) * 0.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(1f).SetUpdate(true);
                d.DOLocalRotate(new Vector3(0, 0, (i % 2 == 0 ? 10f : -10f)), 2.2f + (i % 4) * 0.3f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }
        }

        public void StopDecor()
        {
            foreach (var d in decor) d.DOKill();
        }

        public static TitleScreen Build(Transform parent, GameConfig cfg)
        {
            var t = Make<TitleScreen>(parent, "Title");
            var root = t.transform;

            t.decorRoot = UIFactory.Stretch(root, "Decor");
            string[] dec = { "fl_cookie", "fl_evergreen_tree", "fl_shark", "fl_octopus", "fl_basketball", "fl_carrot", "fl_crab", "fl_bar_chart", "starfish", "fl_trophy" };
            Vector2[] pos = { new Vector2(-890, -260), new Vector2(-420, 460), new Vector2(690, 474), new Vector2(880, 140), new Vector2(900, -170),
                              new Vector2(-880, -40), new Vector2(560, -440), new Vector2(-310, -445), new Vector2(470, 474), new Vector2(-40, 474) };
            float[] size = { 96, 100, 76, 100, 100, 100, 100, 110, 78, 74 };
            t.decor = new RectTransform[dec.Length];
            for (int i = 0; i < dec.Length; i++)
                t.decor[i] = UIFactory.Icon(t.decorRoot, "Decor" + i, cfg.S(dec[i]), UIFactory.Center, pos[i], size[i], Palette.WithA(Color.white, 0.9f)).rectTransform;

            // marquee title plate
            t.plate = UIFactory.Rect(root, "Plate", UIFactory.Center, new Vector2(200, 225), new Vector2(1080, 400));
            var sh = UIFactory.Stretch(t.plate, "Shadow");
            sh.offsetMin = new Vector2(10, -34); sh.offsetMax = new Vector2(10, -34);
            UIFactory.Rounded(sh, Palette.UiShadow, 70);
            var lip = UIFactory.Stretch(t.plate, "Lip");
            lip.offsetMin = new Vector2(0, -18); lip.offsetMax = new Vector2(0, -18);
            UIFactory.Rounded(lip, Palette.GoldLip, 70);
            UIFactory.Rounded(UIFactory.Stretch(t.plate, "Frame"), Palette.Gold, 70);
            UIFactory.Rounded(UIFactory.Stretch(t.plate, "Inner", 30), Palette.Hex(0x3A1678), 48);
            t.marquee = MarqueeLights.Build(t.plate, 62f, 26f, 15f);
            var data = UIFactory.Text(t.plate, "Data", "DATA", UIFactory.Center, new Vector2(0, 78), new Vector2(900, 170), 168, Palette.Gold, TextAlignmentOptions.Center, true);
            var champs = UIFactory.Text(t.plate, "Champions", "CHAMPIONS", UIFactory.Center, new Vector2(0, -78), new Vector2(1000, 170), 132, Color.white, TextAlignmentOptions.Center, true);
            data.characterSpacing = 8f;
            champs.characterSpacing = 4f;

            t.ribbon = UIFactory.Rect(root, "Ribbon", UIFactory.Center, new Vector2(200, -52), new Vector2(820, 86));
            var rl = UIFactory.Stretch(t.ribbon, "Lip");
            rl.offsetMin = new Vector2(0, -9); rl.offsetMax = new Vector2(0, -9);
            UIFactory.Rounded(rl, Palette.PinkLip, 43);
            UIFactory.Rounded(UIFactory.Stretch(t.ribbon, "Face"), Palette.Pink, 43);
            UIFactory.Label(t.ribbon, "T", "Graphs  •  Venn  •  Carroll diagrams", 46, Color.white, TextAlignmentOptions.Center, true).rectTransform.offsetMin = new Vector2(0, 4);

            t.play = UIFactory.Button(root, "Play", "PLAY", UIFactory.Center, new Vector2(200, -215), new Vector2(520, 170), Palette.Orange, Palette.OrangeLip, 96, cfg.S("play"), 16f);
            t.play.GetComponent<UIButtonFX>().idlePulse = true;
            t.progress = UIFactory.Button(root, "Progress", "MY PROGRESS", UIFactory.Center, new Vector2(200, -400), new Vector2(520, 116), Palette.Blue, Palette.BlueLip, 52, cfg.S("fl_trophy"));

            var bestChip = UIFactory.Rect(root, "Best", new Vector2(0, 1), new Vector2(40, -36), new Vector2(380, 84), new Vector2(0, 1));
            UIFactory.Rounded(bestChip, Palette.WithA(Palette.StudioTop, 0.6f), 42);
            UIFactory.Icon(bestChip, "Trophy", cfg.S("fl_trophy"), new Vector2(0, 0.5f), new Vector2(46, 2), 66);
            t.best = UIFactory.Text(bestChip, "Value", "Best: 0", UIFactory.Center, new Vector2(40, 2), new Vector2(280, 70), 42, Palette.GoldLight, TextAlignmentOptions.Center, true);

            t.sound = UIFactory.RoundButton(root, "Sound", cfg.S("sound_on"), new Vector2(1, 1), new Vector2(-78, -70), 104, Palette.Purple, Palette.PurpleLip);
            t.soundIcon = t.sound.transform.Find("Face/Icon").GetComponent<Image>();

            t.owl = Owl.Build(root, "Owl", new Vector2(0.5f, 0f), new Vector2(-640, 10), 0.95f);
            t.bubble = SpeechBubble.Build(root, "Bubble", new Vector2(0.5f, 0f), new Vector2(-660, 640), new Vector2(0.5f, 0f), 440, 36, true);
            return t;
        }
    }
}
