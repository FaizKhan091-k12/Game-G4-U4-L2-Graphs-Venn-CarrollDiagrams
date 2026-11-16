using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DataChampions
{
    /// <summary>
    /// The question stage. Left: the diagram card (skill tag and tile chip in its own header row, caption strip at the
    /// bottom). Right: the question card (replay, timer, wager chip and hint all inside it), then SAFE / RISK IT or the
    /// six answers, then Professor Hoot with his speech bubble and NEXT. Every element keeps a clear gap from the others.
    /// GameFlow drives the sequence.
    /// </summary>
    public class QuestionScreen : UIScreen
    {
        public const int AnswerCount = 6;

        public RectTransform card, diagramHolder, caption, tab, pointsChip, questionPanel, wagerGroup, answersGroup, wagerChip, timer;
        public TextMeshProUGUI captionText, tabText, pointsText, questionText, wagerChipText, riskSub, safeSub, riskTitle, timerText;
        public Image tabIcon, wagerChipIcon, riskIcon, timerFill;
        public ProceduralImage pointsFace, wagerChipFace;
        public Button safe, risk, hint, replay, next;
        public Button[] answers;
        public TextMeshProUGUI[] answerTexts;
        public Image[] answerMarks;
        public Owl owl;
        public SpeechBubble bubble;
        // cover hiding the diagram until SAFE / RISK IT is chosen
        public RectTransform cover, coverIconBg;
        public Image coverIcon;
        public TextMeshProUGUI coverTag, coverPoints;

        [System.NonSerialized] public DiagramView diagram;
        Tween typeTween, timerPulse, coverBob;
        Vector2 cardHome, panelHome;
        string questionLine;

        void Awake()
        {
            cardHome = card.anchoredPosition;
            panelHome = questionPanel.anchoredPosition;
        }

        /// <summary>options are already in display order.</summary>
        public void Setup(QuestionDef q, string[] options, int tileNumber, int points)
        {
            if (diagram != null) Destroy(diagram.gameObject);
            diagram = DiagramFactory.Build(q.diagram, diagramHolder);
            diagram.setCaption = SetCaption;
            caption.gameObject.SetActive(false);

            tabText.text = Skills.Tags[q.skill];
            tabIcon.sprite = GameConfig.Get(Skills.Icons[q.skill]);
            pointsText.text = "TILE " + tileNumber + "  •  " + points;
            pointsFace.color = Palette.TileFace(points);

            // the question stays hidden until the wager is made
            questionLine = q.text;
            questionText.text = "?   ?   ?";
            questionText.color = Palette.Line;
            questionText.maxVisibleCharacters = 99999;
            replay.gameObject.SetActive(false);

            cover.gameObject.SetActive(true);
            cover.localScale = Vector3.one;
            coverIcon.sprite = GameConfig.Get(Skills.Icons[q.skill]);
            coverTag.text = Skills.Tags[q.skill];
            coverPoints.text = "TILE " + tileNumber + "  •  " + points + " COINS";

            bool numeric = int.TryParse(options[0], out _);
            for (int i = 0; i < AnswerCount; i++)
            {
                var t = answerTexts[i];
                t.text = options[i];
                t.fontSizeMax = numeric ? 84f : 38f;
                t.fontSize = t.fontSizeMax;
                // worded options stay on one line and shrink to fit, leaving room for the tick/cross on the right
                t.textWrappingMode = numeric ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                var box = (RectTransform)t.transform.parent;
                box.anchoredPosition = numeric ? new Vector2(-14, 2) : new Vector2(-20, 2);
                box.sizeDelta = numeric ? new Vector2(170, 104) : new Vector2(150, 104);
                UIFactory.SetButtonColors(answers[i], Color.white, Palette.Line);
                answerTexts[i].color = Palette.Ink;
                answerMarks[i].gameObject.SetActive(false);
                answers[i].interactable = true;
                answers[i].transform.localScale = Vector3.one;
            }
            wagerGroup.gameObject.SetActive(false);
            answersGroup.gameObject.SetActive(false);
            wagerChip.gameObject.SetActive(false);
            hint.gameObject.SetActive(false);
            next.gameObject.SetActive(false);
            bubble.gameObject.SetActive(false);
            StopTimer(true);
        }

        /// <summary>Card (still covered) slides in, the empty question card drops in, Professor Hoot arrives.</summary>
        public void PlayIntro()
        {
            card.DOKill();
            questionPanel.DOKill();
            card.anchoredPosition = cardHome;
            questionPanel.anchoredPosition = panelHome;
            SlideIn(card, new Vector2(-900, 0), 0f, 0.55f, false);
            PopIn(tab, 0.35f, 0.35f, false);
            PopIn(pointsChip, 0.45f, 0.35f, false);
            PopIn(coverIconBg, 0.45f, 0.45f, false);
            coverBob?.Kill();
            coverIconBg.anchoredPosition = new Vector2(0, 150);
            coverBob = coverIconBg.DOAnchorPosY(166f, 0.9f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetDelay(0.9f).SetLink(gameObject);
            SlideIn(questionPanel, new Vector2(0, 420), 0.2f, 0.5f, false);
            owl.Enter(0.1f);
        }

        /// <summary>
        /// After the wager: the cover rolls up, the diagram builds and the question types out.
        /// Returns how long the diagram build takes.
        /// </summary>
        public float Reveal()
        {
            coverBob?.Kill();
            cover.DOKill();
            var s = DOTween.Sequence().SetLink(gameObject);
            s.Append(cover.DOScaleY(0f, 0.4f).SetEase(Ease.InBack));
            s.AppendCallback(() => cover.gameObject.SetActive(false));
            DOVirtual.DelayedCall(0.3f, () => { if (diagram != null) diagram.PlayIntro(); }).SetLink(gameObject);

            questionText.text = questionLine;
            questionText.color = Palette.Ink;
            questionText.maxVisibleCharacters = 0;
            typeTween?.Kill();
            int n = questionLine.Length;
            typeTween = DOTween.To(() => questionText.maxVisibleCharacters, x => questionText.maxVisibleCharacters = x, n, Mathf.Clamp(n * 0.022f, 0.5f, 1.4f))
                .SetDelay(0.2f).SetEase(Ease.Linear).SetLink(gameObject);
            replay.gameObject.SetActive(true);
            PopIn(replay.transform, 0.4f, 0.3f, false);
            return 0.3f + (diagram != null ? diagram.IntroDuration : 1f);
        }

        /// <summary>Caption strip under the diagram: pops in once, then just updates (running count, final sum).</summary>
        public void SetCaption(string text)
        {
            captionText.text = text;
            caption.DOKill(true);
            if (!caption.gameObject.activeSelf)
            {
                caption.gameObject.SetActive(true);
                caption.localScale = new Vector3(0.6f, 0f, 1f);
                caption.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
            }
            else
            {
                caption.localScale = Vector3.one;
                caption.DOPunchScale(new Vector3(0.02f, 0.08f, 0f), 0.25f, 6, 0.5f);
            }
        }

        public void ShowWager(bool riskUnlocked, int points)
        {
            wagerGroup.gameObject.SetActive(true);
            safeSub.text = "WIN " + points;
            riskTitle.text = "RISK IT ×2";
            if (riskUnlocked)
            {
                UIFactory.SetButtonColors(risk, Palette.Risk, Palette.RiskLip);
                riskIcon.sprite = GameConfig.Get("fl_fire");
                riskSub.text = "WIN " + points * 2 + " / LOSE " + points;
            }
            else
            {
                UIFactory.SetButtonColors(risk, Palette.Locked, Palette.LockedLip);
                riskIcon.sprite = GameConfig.Get("fl_locked");
                riskSub.text = "Need " + points + " coins";
            }
            PopIn(safe.transform, 0.05f);
            PopIn(risk.transform, 0.15f);
            var label = wagerGroup.Find("Label");
            if (label != null) PopIn(label, 0f);
        }

        public void ShowHintButton(bool available)
        {
            hint.gameObject.SetActive(available);
            hint.interactable = available;
            if (available) PopIn(hint.transform, 0.3f);
        }

        public void ChooseWager(bool isRisk)
        {
            var chosen = isRisk ? risk.transform : safe.transform;
            var other = isRisk ? safe.transform : risk.transform;
            other.DOScale(0f, 0.2f).SetEase(Ease.InBack);
            chosen.DOPunchScale(Vector3.one * 0.15f, 0.25f, 6, 0.6f);
            DOVirtual.DelayedCall(0.3f, () =>
            {
                wagerGroup.gameObject.SetActive(false);
                safe.transform.localScale = risk.transform.localScale = Vector3.one;
            }).SetLink(gameObject);

            wagerChip.gameObject.SetActive(true);
            wagerChipFace.color = isRisk ? Palette.Risk : Palette.Safe;
            wagerChipIcon.sprite = GameConfig.Get(isRisk ? "fl_fire" : "fl_shield");
            wagerChipText.text = isRisk ? "RISK ×2" : "SAFE";
            PopIn(wagerChip, 0.15f);
        }

        public void ShowAnswers()
        {
            answersGroup.gameObject.SetActive(true);
            for (int i = 0; i < AnswerCount; i++) PopIn(answers[i].transform, 0.25f + i * 0.06f);
        }

        public void LockAnswers()
        {
            foreach (var a in answers) a.interactable = false;
            hint.interactable = false;
        }

        public void MarkAnswer(int i, bool correct)
        {
            UIFactory.SetButtonColors(answers[i], correct ? Palette.Correct : Palette.Wrong, correct ? Palette.CorrectLip : Palette.WrongLip);
            answerTexts[i].color = Color.white;
            answerMarks[i].sprite = GameConfig.Get(correct ? "check" : "cross");
            answerMarks[i].gameObject.SetActive(true);
            PopIn(answerMarks[i].transform, 0f);
            var t = answers[i].transform;
            t.DOKill(true);
            if (correct) t.DOPunchScale(Vector3.one * 0.12f, 0.45f, 7, 0.6f);
            else ((RectTransform)t).DOShakeAnchorPos(0.45f, new Vector2(16, 0), 18, 0f);
        }

        public void DimOthers(int keepA, int keepB)
        {
            for (int i = 0; i < AnswerCount; i++)
                if (i != keepA && i != keepB) answers[i].transform.DOScale(0.9f, 0.25f);
        }

        public void ShowNext()
        {
            next.gameObject.SetActive(true);
            next.interactable = true;
            PopIn(next.transform, 0f);
        }

        // ------------------------------------------------------------------ timer

        public void ShowTimer(float total)
        {
            timer.gameObject.SetActive(true);
            PopIn(timer, 0f, 0.3f, false);
            SetTimer(total, total);
        }

        public void SetTimer(float left, float total)
        {
            timerFill.fillAmount = Mathf.Clamp01(left / total);
            timerFill.color = left > 20f ? Palette.Green : left > 10f ? Palette.Orange : Palette.Wrong;
            timerText.text = Mathf.CeilToInt(Mathf.Max(0f, left)).ToString();
            timerText.color = left > 10f ? Palette.Ink : Palette.Wrong;
            if (left <= 10f && left > 0f && timerPulse == null)
                timerPulse = timerText.transform.DOScale(1.2f, 0.25f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(timer.gameObject);
        }

        public void StopTimer(bool hide)
        {
            timerPulse?.Kill();
            timerPulse = null;
            timerText.transform.localScale = Vector3.one;
            if (hide) timer.gameObject.SetActive(false);
        }

        public void Clear()
        {
            if (diagram != null) Destroy(diagram.gameObject);
            diagram = null;
            StopTimer(true);
        }

        // ------------------------------------------------------------------ build

        static Button BigChoice(Transform parent, string name, Vector2 pos, Color face, Color lip, string icon, string title,
            out TextMeshProUGUI titleText, out TextMeshProUGUI sub, out Image iconImg)
        {
            var b = UIFactory.Button(parent, name, null, UIFactory.Center, pos, new Vector2(340, 196), face, lip, 40, null, 14f);
            var f = b.transform.Find("Face");
            iconImg = UIFactory.Icon(f, "Icon", GameConfig.Get(icon), UIFactory.Center, new Vector2(0, 46), 86);
            titleText = UIFactory.Text(f, "Title", title, UIFactory.Center, new Vector2(0, -20), new Vector2(310, 54), 44, Color.white, TextAlignmentOptions.Center, true);
            sub = UIFactory.Text(f, "Sub", "", UIFactory.Center, new Vector2(0, -64), new Vector2(310, 38), 28, Palette.WithA(Color.white, 0.95f), TextAlignmentOptions.Center, true);
            return b;
        }

        static RectTransform Pill(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, out ProceduralImage face)
        {
            var rt = UIFactory.Rect(parent, name, anchor, pos, size);
            face = UIFactory.Rounded(rt, color, size.y * 0.5f);
            return rt;
        }

        // Layout at the 1920 x 1080 reference (origin in the middle), with clear gaps between all blocks:
        //   HUD                    y  540 ..  430 (corner anchored)
        //   diagram card           x -920 ..  120, y  405 .. -505   (header row, diagram, caption all inside)
        //   question card          x  180 ..  900, y  410 ..  130   (replay, timer, wager chip, hint inside)
        //   wager label / buttons  y  102 .. -174
        //   answers (3 x 2)        y   85 .. -181
        //   owl / bubble / NEXT    bottom-anchored, below y -264
        public static QuestionScreen Build(Transform parent, GameConfig cfg)
        {
            var q = Make<QuestionScreen>(parent, "Question");
            var root = q.transform;

            // ---- diagram card
            q.card = UIFactory.Rect(root, "Card", UIFactory.Center, new Vector2(-400, -50), new Vector2(1040, 910));
            var sh = UIFactory.Stretch(q.card, "Shadow");
            sh.offsetMin = new Vector2(10, -28); sh.offsetMax = new Vector2(10, -28);
            UIFactory.Rounded(sh, Palette.UiShadow, 52);
            var lip = UIFactory.Stretch(q.card, "Lip");
            lip.offsetMin = new Vector2(0, -14); lip.offsetMax = new Vector2(0, -14);
            UIFactory.Rounded(lip, Palette.GoldLip, 52);
            UIFactory.Rounded(UIFactory.Stretch(q.card, "Frame"), Palette.Gold, 52);
            UIFactory.Rounded(UIFactory.Stretch(q.card, "Inner", 16), Palette.Cream, 40);

            // header row inside the card: skill tag (left), tile chip (right)
            q.tab = Pill(q.card, "Tab", new Vector2(0f, 1f), new Vector2(262, -64), new Vector2(440, 68), Palette.Purple, out _);
            var tabIconBg = UIFactory.Rect(q.tab, "IconBg", new Vector2(0, 0.5f), new Vector2(38, 0), new Vector2(54, 54));
            UIFactory.Rounded(tabIconBg, Color.white, -1);
            q.tabIcon = UIFactory.Icon(tabIconBg, "Icon", cfg.S("mini_venn"), UIFactory.Center, Vector2.zero, 38, Palette.Purple);
            q.tabText = UIFactory.Text(q.tab, "T", "VENN DIAGRAM", UIFactory.Center, new Vector2(28, 2), new Vector2(350, 60), 38, Color.white, TextAlignmentOptions.Center, true);
            q.tabText.enableAutoSizing = true; q.tabText.fontSizeMax = 38; q.tabText.fontSizeMin = 24;

            q.pointsChip = Pill(q.card, "Points", new Vector2(1f, 1f), new Vector2(-202, -64), new Vector2(340, 68), Palette.Orange, out q.pointsFace);
            UIFactory.Icon(q.pointsChip, "Coin", cfg.S("fl_coin"), new Vector2(1, 0.5f), new Vector2(-38, 0), 54);
            q.pointsText = UIFactory.Text(q.pointsChip, "T", "TILE 1  •  200", UIFactory.Center, new Vector2(-26, 2), new Vector2(270, 60), 38, Color.white, TextAlignmentOptions.Center, true);

            // diagram between the header row and the caption strip (scaled to 930 x 688)
            q.diagramHolder = UIFactory.Rect(q.card, "DiagramHolder", UIFactory.Center, new Vector2(0, -6), new Vector2(1000, 740));
            q.diagramHolder.localScale = Vector3.one * 0.93f;
            q.caption = UIFactory.Rect(q.card, "Caption", new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(960, 64));
            UIFactory.Rounded(q.caption, Palette.Purple, 32);
            q.captionText = UIFactory.Label(q.caption, "T", "", 38, Color.white, TextAlignmentOptions.Center, true);
            q.captionText.rectTransform.offsetMin = new Vector2(24, 4);
            q.captionText.rectTransform.offsetMax = new Vector2(-24, 0);
            q.captionText.enableAutoSizing = true; q.captionText.fontSizeMax = 38; q.captionText.fontSizeMin = 22;
            q.caption.gameObject.SetActive(false);

            // cover over the diagram area (below the header row) until SAFE / RISK IT is chosen; rolls up on reveal
            q.cover = UIFactory.Rect(q.card, "Cover", UIFactory.Center, new Vector2(0, 345), new Vector2(984, 772), new Vector2(0.5f, 1f));
            UIFactory.Rounded(UIFactory.Stretch(q.cover, "Face"), Palette.Hex(0x3A1678), 30);
            UIFactory.Rounded(UIFactory.Stretch(q.cover, "Frame"), Palette.Gold, 30, 6f);
            UIFactory.Text(q.cover, "Q1", "?", UIFactory.Center, new Vector2(-330, 200), new Vector2(260, 300), 260, Palette.WithA(Color.white, 0.07f));
            UIFactory.Text(q.cover, "Q2", "?", UIFactory.Center, new Vector2(340, -220), new Vector2(240, 280), 220, Palette.WithA(Color.white, 0.07f));
            q.coverIconBg = UIFactory.Rect(q.cover, "IconBg", UIFactory.Center, new Vector2(0, 150), new Vector2(190, 190));
            UIFactory.Rounded(q.coverIconBg, Color.white, -1);
            q.coverIcon = UIFactory.Icon(q.coverIconBg, "Icon", cfg.S("mini_venn"), UIFactory.Center, Vector2.zero, 120, Palette.Purple);
            q.coverTag = UIFactory.Text(q.cover, "Tag", "VENN DIAGRAM", UIFactory.Center, new Vector2(0, -6), new Vector2(900, 90), 70, Color.white, TextAlignmentOptions.Center, true);
            q.coverPoints = UIFactory.Text(q.cover, "Points", "TILE 1  •  200 COINS", UIFactory.Center, new Vector2(0, -86), new Vector2(900, 60), 44, Palette.GoldLight, TextAlignmentOptions.Center, true);
            UIFactory.Paragraph(q.cover, "Hint", "Choose SAFE or RISK IT to see the question!", UIFactory.Center, new Vector2(0, -200), new Vector2(820, 110), 42, 28, Palette.WithA(Color.white, 0.92f));

            // ---- question card
            var qp = UIFactory.Panel(root, "QuestionPanel", UIFactory.Center, new Vector2(540, 270), new Vector2(720, 280), Color.white, Palette.Line, 40, 10f);
            q.questionPanel = (RectTransform)qp.transform.parent;
            q.questionText = UIFactory.Paragraph(qp.transform, "Text", "Question", UIFactory.Center, new Vector2(0, 22), new Vector2(520, 170), 44, 26, Palette.Ink);

            q.replay = UIFactory.RoundButton(qp.transform, "Replay", cfg.S("speaker"), new Vector2(0, 1), new Vector2(50, -48), 64, Palette.Blue, Palette.BlueLip);
            q.replay.GetComponent<UIButtonFX>().burst = false;

            // countdown ring (top-right, inside the card)
            q.timer = UIFactory.Rect(qp.transform, "Timer", new Vector2(1, 1), new Vector2(-50, -48), new Vector2(72, 72));
            UIFactory.Rounded(UIFactory.Stretch(q.timer, "Back"), Palette.Hex(0xEEE8F8), -1);
            var fillRt = UIFactory.Stretch(q.timer, "Fill");
            q.timerFill = fillRt.gameObject.AddComponent<Image>();
            q.timerFill.sprite = cfg.S("ring_thick");
            q.timerFill.type = Image.Type.Filled;
            q.timerFill.fillMethod = Image.FillMethod.Radial360;
            q.timerFill.fillOrigin = (int)Image.Origin360.Top;
            q.timerFill.fillClockwise = false;
            q.timerFill.color = Palette.Green;
            q.timerFill.raycastTarget = false;
            q.timerText = UIFactory.Label(q.timer, "T", "45", 30, Palette.Ink);
            q.timerText.rectTransform.offsetMin = new Vector2(0, 2);

            // bottom row inside the card: wager chip (left), hint (right)
            q.wagerChip = Pill(qp.transform, "WagerChip", new Vector2(0, 0), new Vector2(130, 40), new Vector2(210, 52), Palette.Safe, out q.wagerChipFace);
            q.wagerChipIcon = UIFactory.Icon(q.wagerChip, "Icon", cfg.S("fl_shield"), new Vector2(0, 0.5f), new Vector2(32, 1), 44);
            q.wagerChipText = UIFactory.Text(q.wagerChip, "T", "SAFE", UIFactory.Center, new Vector2(22, 2), new Vector2(150, 48), 32, Color.white, TextAlignmentOptions.Center, true);

            q.hint = UIFactory.Button(qp.transform, "Hint", "HINT -50", new Vector2(1, 0), new Vector2(-130, 44), new Vector2(210, 56), Palette.Yellow, Palette.GoldLip, 30, cfg.S("fl_light_bulb"), 7f);
            var hl = UIFactory.ButtonLabel(q.hint);
            hl.color = Palette.Ink;
            hl.fontSharedMaterial = cfg.font.material;

            // ---- SAFE / RISK IT
            q.wagerGroup = UIFactory.Rect(root, "Wager", UIFactory.Center, new Vector2(540, -30), new Vector2(720, 280));
            UIFactory.Text(q.wagerGroup, "Label", "How sure are you?", UIFactory.Center, new Vector2(0, 104), new Vector2(700, 56), 44, Color.white, TextAlignmentOptions.Center, true);
            q.safe = BigChoice(q.wagerGroup, "Safe", new Vector2(-182, -32), Palette.Safe, Palette.SafeLip, "fl_shield", "PLAY SAFE", out _, out q.safeSub, out _);
            q.risk = BigChoice(q.wagerGroup, "Risk", new Vector2(182, -32), Palette.Risk, Palette.RiskLip, "fl_fire", "RISK IT ×2", out q.riskTitle, out q.riskSub, out q.riskIcon);

            // ---- six answers (3 x 2)
            q.answersGroup = UIFactory.Rect(root, "Answers", UIFactory.Center, new Vector2(540, -40), new Vector2(720, 264));
            q.answers = new Button[AnswerCount];
            q.answerTexts = new TextMeshProUGUI[AnswerCount];
            q.answerMarks = new Image[AnswerCount];
            var size = new Vector2(224, 118);
            for (int i = 0; i < AnswerCount; i++)
            {
                var pos = new Vector2((i % 3 - 1) * 248f, i < 3 ? 66f : -72f);
                var b = UIFactory.Button(q.answersGroup, "Answer" + i, null, UIFactory.Center, pos, size, Color.white, Palette.Line, 40, null, 10f);
                var f = b.transform.Find("Face");
                var t = UIFactory.Text(f, "Value", "0", UIFactory.Center, new Vector2(-14, 2), new Vector2(170, 104), 84, Palette.Ink);
                t.enableAutoSizing = true; t.fontSizeMax = 84; t.fontSizeMin = 26;
                t.textWrappingMode = TextWrappingModes.Normal;
                q.answerTexts[i] = t;
                q.answerMarks[i] = UIFactory.Icon(f, "Mark", cfg.S("check"), new Vector2(1, 0.5f), new Vector2(-30, 0), 40, Color.white);
                q.answers[i] = b;
            }

            // ---- host row (bottom-anchored so it rests on the floor on any aspect ratio)
            q.owl = Owl.Build(root, "Owl", new Vector2(0.5f, 0f), new Vector2(232, 2), 0.5f);
            q.bubble = SpeechBubble.Build(root, "Bubble", new Vector2(0.5f, 0f), new Vector2(350, 226), new Vector2(0f, 0.5f), 410, 30, true);
            q.next = UIFactory.Button(root, "Next", "NEXT", new Vector2(0.5f, 0f), new Vector2(570, 80), new Vector2(330, 108), Palette.Green, Palette.GreenLip, 58, cfg.S("next"));
            q.next.GetComponent<UIButtonFX>().idlePulse = true;
            return q;
        }
    }
}
