using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DataChampions
{
    /// <summary>
    /// The question stage: diagram board on the left; question card, SAFE / RISK IT choice, four answers and NEXT on the
    /// right; Professor Hoot with his speech bubble underneath. GameFlow drives the sequence.
    /// </summary>
    public class QuestionScreen : UIScreen
    {
        public RectTransform card, diagramHolder, caption, tab, pointsChip, questionPanel, wagerGroup, answersGroup, wagerChip;
        public TextMeshProUGUI captionText, tabText, pointsText, questionText, wagerChipText, riskSub, safeSub, riskTitle;
        public Image tabIcon, wagerChipIcon, riskIcon;
        public ProceduralImage pointsFace, wagerChipFace;
        public Button safe, risk, hint, replay, next;
        public Button[] answers;
        public TextMeshProUGUI[] answerTexts;
        public Image[] answerMarks;
        public Owl owl;
        public SpeechBubble bubble;

        [System.NonSerialized] public DiagramView diagram;
        Tween typeTween;
        Vector2 cardHome, panelHome;

        void Awake()
        {
            cardHome = card.anchoredPosition;
            panelHome = questionPanel.anchoredPosition;
        }

        public void Setup(QuestionDef q, int tileNumber, int points)
        {
            if (diagram != null) Destroy(diagram.gameObject);
            diagram = DiagramFactory.Build(q.diagram, diagramHolder);
            diagram.setCaption = SetCaption;
            caption.gameObject.SetActive(false);

            tabText.text = Skills.Tags[q.skill];
            tabIcon.sprite = GameConfig.Get(Skills.Icons[q.skill]);
            pointsText.text = "TILE " + tileNumber + "   " + points;
            pointsFace.color = Palette.TileFace(points);

            questionText.text = q.text;
            questionText.maxVisibleCharacters = 0;

            for (int i = 0; i < 4; i++)
            {
                answerTexts[i].text = q.options[i].ToString();
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
        }

        /// <summary>Card slides in, diagram builds, question card drops and types out.</summary>
        public float PlayIntro()
        {
            card.DOKill();
            questionPanel.DOKill();
            card.anchoredPosition = cardHome;
            questionPanel.anchoredPosition = panelHome;
            SlideIn(card, new Vector2(-900, 0), 0f, 0.55f, false);
            PopIn(tab, 0.35f, 0.35f, false);
            PopIn(pointsChip, 0.45f, 0.35f, false);
            SlideIn(questionPanel, new Vector2(0, 420), 0.2f, 0.5f, false);
            float introStart = 0.4f;
            DOVirtual.DelayedCall(introStart, () => { if (diagram != null) diagram.PlayIntro(); }).SetLink(gameObject);
            typeTween?.Kill();
            int n = questionText.text.Length;
            typeTween = DOTween.To(() => questionText.maxVisibleCharacters, x => questionText.maxVisibleCharacters = x, n, Mathf.Clamp(n * 0.025f, 0.6f, 1.6f))
                .SetDelay(0.55f).SetEase(Ease.Linear).SetLink(gameObject);
            owl.Enter(0.1f);
            return introStart + (diagram != null ? diagram.IntroDuration : 1f);
        }

        public void SetCaption(string text)
        {
            caption.gameObject.SetActive(true);
            captionText.text = text;
            caption.DOKill(true);
            caption.localScale = new Vector3(0.6f, 0f, 1f);
            caption.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
        }

        public void ShowWager(bool riskUnlocked, int points, bool hintAvailable)
        {
            wagerGroup.gameObject.SetActive(true);
            safeSub.text = "WIN " + points;
            if (riskUnlocked)
            {
                UIFactory.SetButtonColors(risk, Palette.Risk, Palette.RiskLip);
                riskIcon.sprite = GameConfig.Get("fl_fire");
                riskTitle.text = "RISK IT ×2";
                riskSub.text = "WIN " + points * 2 + " / LOSE " + points;
            }
            else
            {
                UIFactory.SetButtonColors(risk, Palette.Locked, Palette.LockedLip);
                riskIcon.sprite = GameConfig.Get("fl_locked");
                riskTitle.text = "RISK IT ×2";
                riskSub.text = "Need " + points + " coins";
            }
            PopIn(safe.transform, 0.05f);
            PopIn(risk.transform, 0.15f);
            var label = wagerGroup.Find("Label");
            if (label != null) PopIn(label, 0f);
            ShowHintButton(hintAvailable);
        }

        public void ShowHintButton(bool available)
        {
            hint.gameObject.SetActive(available);
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
            if (isRisk)
                wagerChip.DOScale(1.08f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetDelay(0.6f).SetLink(wagerChip.gameObject);
        }

        public void ShowAnswers()
        {
            answersGroup.gameObject.SetActive(true);
            for (int i = 0; i < 4; i++) PopIn(answers[i].transform, 0.3f + i * 0.08f);
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
            if (correct) t.DOPunchScale(Vector3.one * 0.18f, 0.45f, 7, 0.6f);
            else ((RectTransform)t).DOShakeAnchorPos(0.45f, new Vector2(22, 0), 18, 0f);
        }

        public void DimOthers(int keepA, int keepB)
        {
            for (int i = 0; i < 4; i++)
                if (i != keepA && i != keepB) answers[i].transform.DOScale(0.88f, 0.25f);
        }

        public void ShowNext()
        {
            next.gameObject.SetActive(true);
            next.interactable = true;
            PopIn(next.transform, 0f);
        }

        public void Clear()
        {
            if (diagram != null) Destroy(diagram.gameObject);
            diagram = null;
            wagerChip.DOKill();
            wagerChip.localScale = Vector3.one;
        }

        // ------------------------------------------------------------------ build

        static Button BigChoice(Transform parent, string name, Vector2 pos, Color face, Color lip, string icon, string title, out TextMeshProUGUI titleText, out TextMeshProUGUI sub, out Image iconImg)
        {
            var b = UIFactory.Button(parent, name, null, UIFactory.Center, pos, new Vector2(340, 210), face, lip, 40, null, 14f);
            var f = b.transform.Find("Face");
            iconImg = UIFactory.Icon(f, "Icon", GameConfig.Get(icon), UIFactory.Center, new Vector2(0, 46), 96);
            titleText = UIFactory.Text(f, "Title", title, UIFactory.Center, new Vector2(0, -24), new Vector2(320, 60), 48, Color.white, TextAlignmentOptions.Center, true);
            sub = UIFactory.Text(f, "Sub", "", UIFactory.Center, new Vector2(0, -70), new Vector2(320, 44), 30, Palette.WithA(Color.white, 0.95f), TextAlignmentOptions.Center, true);
            return b;
        }

        public static QuestionScreen Build(Transform parent, GameConfig cfg)
        {
            var q = Make<QuestionScreen>(parent, "Question");
            var root = q.transform;

            // diagram card
            q.card = UIFactory.Rect(root, "Card", UIFactory.Center, new Vector2(-400, -45), new Vector2(1040, 850));
            var sh = UIFactory.Stretch(q.card, "Shadow");
            sh.offsetMin = new Vector2(10, -36); sh.offsetMax = new Vector2(10, -36);
            UIFactory.Rounded(sh, Palette.UiShadow, 52);
            var lip = UIFactory.Stretch(q.card, "Lip");
            lip.offsetMin = new Vector2(0, -18); lip.offsetMax = new Vector2(0, -18);
            UIFactory.Rounded(lip, Palette.GoldLip, 52);
            UIFactory.Rounded(UIFactory.Stretch(q.card, "Frame"), Palette.Gold, 52);
            UIFactory.Rounded(UIFactory.Stretch(q.card, "Inner", 16), Palette.Cream, 40);
            q.diagramHolder = UIFactory.Rect(q.card, "DiagramHolder", UIFactory.Center, new Vector2(0, 38), new Vector2(1000, 740));
            q.caption = UIFactory.Rect(q.card, "Caption", UIFactory.Center, new Vector2(0, -372), new Vector2(940, 70));
            UIFactory.Rounded(q.caption, Palette.Purple, 35);
            q.captionText = UIFactory.Label(q.caption, "T", "", 40, Color.white, TextAlignmentOptions.Center, true);
            q.captionText.rectTransform.offsetMin = new Vector2(10, 4);
            q.caption.gameObject.SetActive(false);

            q.tab = UIFactory.Rect(q.card, "Tab", UIFactory.Center, new Vector2(-270, 440), new Vector2(460, 74));
            UIFactory.Rounded(q.tab, Palette.Purple, 37);
            var tabIconBg = UIFactory.Rect(q.tab, "IconBg", new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(58, 58));
            UIFactory.Rounded(tabIconBg, Color.white, -1);
            q.tabIcon = UIFactory.Icon(tabIconBg, "Icon", cfg.S("mini_venn"), UIFactory.Center, Vector2.zero, 42, Palette.Purple);
            q.tabText = UIFactory.Text(q.tab, "T", "VENN DIAGRAM", UIFactory.Center, new Vector2(30, 2), new Vector2(380, 64), 40, Color.white, TextAlignmentOptions.Center, true);

            q.pointsChip = UIFactory.Rect(q.card, "Points", UIFactory.Center, new Vector2(340, 440), new Vector2(330, 74));
            q.pointsFace = UIFactory.Rounded(q.pointsChip, Palette.Orange, 37);
            UIFactory.Icon(q.pointsChip, "Coin", cfg.S("fl_coin"), new Vector2(1, 0.5f), new Vector2(-40, 0), 60);
            q.pointsText = UIFactory.Text(q.pointsChip, "T", "TILE 1   200", UIFactory.Center, new Vector2(-24, 2), new Vector2(260, 64), 40, Color.white, TextAlignmentOptions.Center, true);

            // question card
            var qp = UIFactory.Panel(root, "QuestionPanel", UIFactory.Center, new Vector2(545, 290), new Vector2(730, 270), Color.white, Palette.Line, 44, 12f);
            q.questionPanel = (RectTransform)qp.transform.parent;
            q.questionText = UIFactory.Paragraph(qp.transform, "Text", "Question", UIFactory.Center, new Vector2(-10, 6), new Vector2(620, 220), 50, 30, Palette.Ink);
            q.replay = UIFactory.RoundButton(q.questionPanel, "Replay", cfg.S("speaker"), new Vector2(0, 1), new Vector2(8, -8), 74, Palette.Blue, Palette.BlueLip);
            q.replay.GetComponent<UIButtonFX>().burst = false;

            q.wagerChip = UIFactory.Rect(q.questionPanel, "WagerChip", new Vector2(0, 0), new Vector2(150, -2), new Vector2(240, 66));
            q.wagerChipFace = UIFactory.Rounded(q.wagerChip, Palette.Safe, 33);
            q.wagerChipIcon = UIFactory.Icon(q.wagerChip, "Icon", cfg.S("fl_shield"), new Vector2(0, 0.5f), new Vector2(38, 2), 58);
            q.wagerChipText = UIFactory.Text(q.wagerChip, "T", "SAFE", UIFactory.Center, new Vector2(26, 2), new Vector2(170, 60), 38, Color.white, TextAlignmentOptions.Center, true);

            q.hint = UIFactory.Button(q.questionPanel, "Hint", "HINT -50", new Vector2(1, 0), new Vector2(-150, -6), new Vector2(260, 76), Palette.Yellow, Palette.GoldLip, 34, cfg.S("fl_light_bulb"), 9f);
            UIFactory.ButtonLabel(q.hint).color = Palette.Ink;
            UIFactory.ButtonLabel(q.hint).fontSharedMaterial = cfg.font.material;

            // wager
            q.wagerGroup = UIFactory.Rect(root, "Wager", UIFactory.Center, new Vector2(545, -70), new Vector2(720, 330));
            UIFactory.Text(q.wagerGroup, "Label", "How sure are you?", UIFactory.Center, new Vector2(0, 148), new Vector2(700, 60), 46, Color.white, TextAlignmentOptions.Center, true);
            q.safe = BigChoice(q.wagerGroup, "Safe", new Vector2(-180, -26), Palette.Safe, Palette.SafeLip, "fl_shield", "PLAY SAFE", out _, out q.safeSub, out _);
            q.risk = BigChoice(q.wagerGroup, "Risk", new Vector2(180, -26), Palette.Risk, Palette.RiskLip, "fl_fire", "RISK IT ×2", out q.riskTitle, out q.riskSub, out q.riskIcon);

            // answers
            q.answersGroup = UIFactory.Rect(root, "Answers", UIFactory.Center, new Vector2(545, -75), new Vector2(720, 360));
            q.answers = new Button[4];
            q.answerTexts = new TextMeshProUGUI[4];
            q.answerMarks = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var pos = new Vector2(i % 2 == 0 ? -180 : 180, i < 2 ? 92 : -92);
                var b = UIFactory.Button(q.answersGroup, "Answer" + i, null, UIFactory.Center, pos, new Vector2(340, 160), Color.white, Palette.Line, 40, null, 12f);
                var f = b.transform.Find("Face");
                q.answerTexts[i] = UIFactory.Text(f, "Value", "0", UIFactory.Center, new Vector2(0, 4), new Vector2(300, 140), 96, Palette.Ink);
                q.answerMarks[i] = UIFactory.Icon(f, "Mark", cfg.S("check"), new Vector2(1, 0.5f), new Vector2(-46, 0), 64, Color.white);
                q.answers[i] = b;
            }

            q.owl = Owl.Build(root, "Owl", new Vector2(0.5f, 0f), new Vector2(232, -5), 0.5f);
            q.bubble = SpeechBubble.Build(root, "Bubble", new Vector2(0.5f, 0f), new Vector2(335, 210), new Vector2(0f, 0.5f), 420, 32, true);
            q.next = UIFactory.Button(root, "Next", "NEXT", new Vector2(0.5f, 0f), new Vector2(565, 78), new Vector2(340, 112), Palette.Green, Palette.GreenLip, 60, cfg.S("next"));
            q.next.GetComponent<UIButtonFX>().idlePulse = true;
            return q;
        }
    }
}
