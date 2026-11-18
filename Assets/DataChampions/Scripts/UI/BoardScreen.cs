using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DataChampions
{
    /// <summary>The tile board: "Pick a tile!" marquee header, 5 x 2 tiles, Professor Hoot hosting on the left.</summary>
    public class BoardScreen : UIScreen
    {
        public RectTransform header, grid;
        public Owl owl;
        public SpeechBubble bubble;
        public Button help;
        public MarqueeLights marquee;
        [System.NonSerialized] public List<TileView> tiles = new List<TileView>();

        public static readonly Vector2 TileSize = new Vector2(250, 232);
        const float Gap = 28f;
        static readonly Vector2 HelpSize = new Vector2(290, 76);

        /// <summary>Creates the ten tiles for a new game. points/skills are per tile.</summary>
        public void BuildTiles(int[] points, int[] skills, System.Action<int> onPick)
        {
            foreach (var t in tiles) if (t != null) Destroy(t.gameObject);
            tiles.Clear();
            for (int i = 0; i < 10; i++)
            {
                int col = i % 5, row = i / 5;
                var pos = new Vector2((col - 2) * (TileSize.x + Gap), (0.5f - row) * (TileSize.y + Gap + 16f));
                var t = TileView.Build(grid, i, points[i], skills[i], pos, TileSize);
                int idx = i;
                t.button.onClick.AddListener(() => onPick(idx));
                tiles.Add(t);
            }
        }

        public void PlayIntro(bool firstTime)
        {
            header.DOKill(true);
            var hp = header.anchoredPosition;
            header.anchoredPosition = hp + new Vector2(0, 260);
            header.DOAnchorPos(hp, 0.6f).SetEase(Ease.OutBack).SetUpdate(true);
            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i];
                if (firstTime)
                {
                    PopIn(t.transform, 0.15f + (i % 5) * 0.06f + (i / 5) * 0.12f, 0.45f);
                    DOVirtual.DelayedCall(0.15f + i * 0.05f, () => GameAudio.I?.Play(Sfx.Pop, 0.9f + idxPitch(t.index), 0.4f), true).SetLink(gameObject);
                }
                if (!t.answered) t.StartBob(0.6f + i * 0.13f);
            }
        }

        static float idxPitch(int i) => i * 0.04f;

        public void SetInteractable(bool on)
        {
            foreach (var t in tiles) t.button.interactable = on && !t.answered;
        }

        public static BoardScreen Build(Transform parent, GameConfig cfg)
        {
            var b = Make<BoardScreen>(parent, "Board");
            var root = b.transform;
            b.header = UIFactory.Rect(root, "Header", UIFactory.Center, new Vector2(190, 352), new Vector2(760, 124));
            var lip = UIFactory.Stretch(b.header, "Lip");
            lip.offsetMin = new Vector2(0, -12); lip.offsetMax = new Vector2(0, -12);
            UIFactory.Rounded(lip, Palette.GoldLip, 50);
            UIFactory.Rounded(UIFactory.Stretch(b.header, "Frame"), Palette.Gold, 50);
            UIFactory.Rounded(UIFactory.Stretch(b.header, "Inner", 14), Palette.Hex(0x3A1678), 38);
            b.marquee = MarqueeLights.Build(b.header, 52f, 18f, 7f);
            UIFactory.Label(b.header, "T", "PICK A TILE!", 74, Color.white, TextAlignmentOptions.Center, true).rectTransform.offsetMin = new Vector2(0, 4);

            b.grid = UIFactory.Rect(root, "Grid", UIFactory.Center, new Vector2(190, -48), new Vector2(1400, 520));
            b.owl = Owl.Build(root, "Owl", new Vector2(0.5f, 0f), new Vector2(-770, 10), 0.82f);
            b.bubble = SpeechBubble.Build(root, "Bubble", new Vector2(0.5f, 0f), new Vector2(-770, 520), new Vector2(0.5f, 0f), 380, 36, true);
            // "How to Play" pill under the HUD pause button (left edges line up at x = 26)
            b.help = UIFactory.Button(root, "Help", "HOW TO PLAY", new Vector2(0, 1), new Vector2(26 + HelpSize.x * 0.5f, -200), HelpSize,
                Palette.Blue, Palette.BlueLip, 34, cfg.S("question"), 9f);
            var helpLabel = UIFactory.ButtonLabel(b.help);
            helpLabel.enableAutoSizing = true;
            helpLabel.fontSizeMax = 34;
            helpLabel.fontSizeMin = 22;
            return b;
        }
    }
}
