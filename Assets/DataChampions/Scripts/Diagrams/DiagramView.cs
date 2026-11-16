using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataChampions
{
    /// <summary>
    /// One live diagram on the question board. Knows its items (grouped for counting), its regions (rows, columns,
    /// cells, circles) and its bars, and plays the build-in animation, hints and step-by-step explanations.
    /// Content space is 1000 x 740 with the origin in the middle. Built by DiagramFactory.
    /// </summary>
    public class DiagramView : MonoBehaviour
    {
        public enum IntroKind { Fade, Pop, Grow, Circle }

        struct Intro
        {
            public RectTransform rt;
            public IntroKind kind;
            public float delay;
            public float height;
            public bool sound;
        }

        public struct Region
        {
            public Rect rect;
            public bool round;
        }

        public RectTransform content;   // diagram drawing
        public RectTransform overlay;   // highlights, guide lines, value badges
        public RectTransform under;     // glows behind counted items
        public System.Action<string> setCaption;

        readonly List<Intro> intros = new List<Intro>();
        readonly Dictionary<string, List<RectTransform>> groups = new Dictionary<string, List<RectTransform>>();
        readonly Dictionary<string, Region> regions = new Dictionary<string, Region>();
        readonly List<GameObject> marks = new List<GameObject>();
        readonly List<Tween> markTweens = new List<Tween>();

        // bar chart data
        public readonly List<RectTransform> bars = new List<RectTransform>();
        public readonly List<int> barValues = new List<int>();
        public float axisX, zeroY, unit;

        int runningCount;

        /// <summary>NEXT can remove the diagram mid-animation: stop every tween that still targets its parts.</summary>
        void OnDestroy()
        {
            foreach (var t in markTweens) t?.Kill();
            foreach (var t in GetComponentsInChildren<Transform>(true)) t.DOKill();
            foreach (var cg in GetComponentsInChildren<CanvasGroup>(true)) cg.DOKill();
            foreach (var g in GetComponentsInChildren<Graphic>(true)) g.DOKill();
        }

        // ------------------------------------------------------------------ registration (factory)

        public void AddIntro(RectTransform rt, IntroKind kind, float delay, bool sound = false)
        {
            intros.Add(new Intro { rt = rt, kind = kind, delay = delay, height = rt.sizeDelta.y, sound = sound });
            if (kind == IntroKind.Grow) rt.sizeDelta = new Vector2(rt.sizeDelta.x, 0f);
            else if (kind == IntroKind.Fade)
            {
                if (!rt.TryGetComponent<CanvasGroup>(out var cg)) cg = rt.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
            else rt.localScale = Vector3.zero;
        }

        public void AddItem(string group, RectTransform rt)
        {
            if (!groups.TryGetValue(group, out var list)) groups[group] = list = new List<RectTransform>();
            list.Add(rt);
        }

        public void AddRegion(string key, Rect r, bool round = false) => regions[key] = new Region { rect = r, round = round };

        public bool HasRegion(string key) => regions.ContainsKey(key);

        // ------------------------------------------------------------------ intro

        public float IntroDuration
        {
            get
            {
                float end = 0f;
                foreach (var i in intros) end = Mathf.Max(end, i.delay + (i.kind == IntroKind.Grow ? 0.6f : 0.4f));
                return end;
            }
        }

        public void PlayIntro()
        {
            float lastSound = -1f;
            int soundIndex = 0;
            foreach (var i in intros)
            {
                var rt = i.rt;
                switch (i.kind)
                {
                    case IntroKind.Fade:
                        var cg = rt.GetComponent<CanvasGroup>();
                        rt.localScale = Vector3.one * 0.96f;
                        cg.DOFade(1f, 0.35f).SetDelay(i.delay);
                        rt.DOScale(1f, 0.45f).SetDelay(i.delay).SetEase(Ease.OutBack);
                        break;
                    case IntroKind.Pop:
                        rt.DOScale(1f, 0.38f).SetDelay(i.delay).SetEase(Ease.OutBack);
                        break;
                    case IntroKind.Circle:
                        rt.localScale = Vector3.zero;
                        rt.DOScale(1f, 0.55f).SetDelay(i.delay).SetEase(Ease.OutBack);
                        break;
                    case IntroKind.Grow:
                        rt.DOSizeDelta(new Vector2(rt.sizeDelta.x, i.height), 0.6f).SetDelay(i.delay).SetEase(Ease.OutBack);
                        break;
                }
                if (i.sound && i.delay - lastSound > 0.07f)
                {
                    lastSound = i.delay;
                    int idx = soundIndex++;
                    var kind = i.kind;
                    DOVirtual.DelayedCall(i.delay, () =>
                        GameAudio.I?.Play(kind == IntroKind.Grow ? Sfx.BarGrow : Sfx.Pop, 0.9f + Mathf.Min(idx, 14) * 0.035f, kind == IntroKind.Grow ? 0.6f : 0.45f));
                }
            }
        }

        // ------------------------------------------------------------------ marks (highlights, counters)

        readonly List<GameObject> highlights = new List<GameObject>();
        readonly List<string> tally = new List<string>();

        RectTransform MarkRect(string name, Rect r)
        {
            var rt = UIFactory.Rect(overlay, name, UIFactory.Center, r.center, r.size);
            marks.Add(rt.gameObject);
            return rt;
        }

        /// <summary>
        /// Glowing frame drawn just inside a region (never over the table edges). By default it replaces the previous
        /// frame, so the eye goes row -> column -> the one box that answers the question.
        /// </summary>
        public void Highlight(string key, Color? color = null, bool soft = false, bool replace = true)
        {
            if (!regions.TryGetValue(key, out var reg)) { Debug.LogWarning("[DataChampions] no region " + key); return; }
            if (replace)
            {
                foreach (var old in highlights)
                {
                    if (old == null) continue;
                    var g = old;
                    var ocg = g.GetComponent<CanvasGroup>();
                    ocg.DOKill();
                    ocg.DOFade(0f, 0.2f).SetLink(g).OnComplete(() => g.SetActive(false));
                }
                highlights.Clear();
            }
            var c = color ?? Palette.Highlight;
            var rt = MarkRect("Hl_" + key, reg.rect);
            highlights.Add(rt.gameObject);
            float radius = reg.round ? -1f : 20f;
            UIFactory.Rounded(UIFactory.Stretch(rt, "Fill"), Palette.WithA(c, soft ? 0.07f : 0.08f), radius);
            UIFactory.Rounded(UIFactory.Stretch(rt, "Frame"), c, radius, soft ? 6f : 8f);
            rt.localScale = Vector3.one * 1.04f;
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            var s = DOTween.Sequence().SetLink(rt.gameObject);
            s.Append(cg.DOFade(1f, 0.2f)).Join(rt.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            markTweens.Add(s);
            // hints keep breathing gently (a separate tween: infinite loops can't live inside a Sequence)
            if (soft) markTweens.Add(cg.DOFade(0.55f, 0.5f).SetDelay(0.3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(rt.gameObject));
            GameAudio.I?.Play(Sfx.Sparkle, 1.1f, 0.5f);
        }

        public void ClearMarks()
        {
            foreach (var t in markTweens) t?.Kill();
            markTweens.Clear();
            foreach (var g in marks) if (g != null) Destroy(g);
            marks.Clear();
            highlights.Clear();
            tally.Clear();
            runningCount = 0;
        }

        /// <summary>
        /// Counts a group item by item: a gold glow appears BEHIND each item (nothing is drawn over the diagram) and
        /// the running total grows in the caption strip ("2, 4, 6 ...").
        /// </summary>
        public IEnumerator Count(string group, int step, bool keep)
        {
            if (!groups.TryGetValue(group, out var items)) { Debug.LogWarning("[DataChampions] no group " + group); yield break; }
            if (!keep) { runningCount = 0; tally.Clear(); }
            float per = items.Count > 10 ? 0.34f : 0.45f;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                runningCount += step;
                tally.Add(runningCount.ToString());
                setCaption?.Invoke("Count: " + string.Join(", ", tally));

                var p = (Vector2)under.InverseTransformPoint(it.position);
                // soft gold glow sized to the item's own shape (round chips, square tiles, wide name pills)
                var size = it.rect.size + new Vector2(70f, 56f);
                var halo = UIFactory.Rect(under, "Glow", UIFactory.Center, p, size);
                marks.Add(halo.gameObject);
                var glow = halo.gameObject.AddComponent<Image>();
                glow.sprite = GameConfig.Get("soft");
                glow.color = Palette.Gold;
                glow.raycastTarget = false;
                halo.localScale = Vector3.zero;
                halo.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetLink(halo.gameObject);
                it.DOKill(true);
                it.DOPunchScale(Vector3.one * 0.22f, 0.35f, 6, 0.6f);
                GameAudio.I?.Play(Sfx.Tick, 0.85f + Mathf.Min(i, 16) * 0.05f, 0.9f);
                yield return new WaitForSeconds(per);
            }
            yield return new WaitForSeconds(0.25f);
        }

        void Badge(Vector2 pos, string text, float h = 54f, float font = 36f)
        {
            float w = text.Length > 1 ? h * 1.25f : h;
            var rt = UIFactory.Rect(overlay, "Count", UIFactory.Center, pos, new Vector2(w, h));
            marks.Add(rt.gameObject);
            var lip = UIFactory.Stretch(rt, "Lip");
            lip.offsetMin = new Vector2(0, -4); lip.offsetMax = new Vector2(0, -4);
            UIFactory.Rounded(lip, Palette.GoldLip, h * 0.5f);
            UIFactory.Rounded(UIFactory.Stretch(rt, "Face"), Palette.Gold, h * 0.5f);
            UIFactory.Label(rt, "T", text, font, Color.white, TextAlignmentOptions.Center, true).rectTransform.offsetMin = new Vector2(0, 3);
            rt.localScale = Vector3.zero;
            rt.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(rt.gameObject);
        }

        /// <summary>Reads a bar: outline it, run a guide line across to the axis, light up its value.</summary>
        public IEnumerator ReadBar(int index)
        {
            if (index < 0 || index >= bars.Count) yield break;
            var bar = bars[index];
            int v = barValues[index];
            float top = zeroY + v * unit;
            var p = (Vector2)overlay.InverseTransformPoint(bar.position); // bar pivot is its bottom centre
            float half = bar.rect.width * 0.5f;
            var r = new Rect(p.x - half - 8f, zeroY, half * 2f + 16f, v * unit + 8f);
            regions["bar" + index] = new Region { rect = r };
            Highlight("bar" + index, Palette.Highlight, false, false);
            bar.DOPunchScale(new Vector3(0.12f, 0.06f, 0), 0.4f, 6, 0.5f);
            yield return new WaitForSeconds(0.35f);

            // guide line from the bar top to the axis
            float len = (p.x - half) - axisX;
            var line = UIFactory.Rect(overlay, "Guide", UIFactory.Center, new Vector2(p.x - half, top), new Vector2(0f, 8f), new Vector2(1f, 0.5f));
            marks.Add(line.gameObject);
            UIFactory.Rounded(line, Palette.Ink, 4f);
            GameAudio.I?.Play(Sfx.Whoosh, 1.2f, 0.5f);
            line.DOSizeDelta(new Vector2(len, 8f), 0.45f).SetEase(Ease.OutCubic).SetLink(line.gameObject);
            yield return new WaitForSeconds(0.5f);
            Badge(new Vector2(axisX - 40f, top), v.ToString());
            Badge(new Vector2(p.x, top + 44f), v.ToString());
            GameAudio.I?.Play(Sfx.Tick, 1.1f + index * 0.1f);
            yield return new WaitForSeconds(0.6f);
        }

        public IEnumerator Play(ExplainStep step)
        {
            switch (step.kind)
            {
                case StepKind.Highlight:
                    Highlight(step.target);
                    yield return new WaitForSeconds(0.6f);
                    break;
                case StepKind.Count:
                    yield return Count(step.target, step.step <= 0 ? 1 : step.step, step.keepCount);
                    break;
                case StepKind.ReadBar:
                    yield return ReadBar(int.Parse(step.target));
                    break;
                case StepKind.Caption:
                    setCaption?.Invoke(step.text);
                    GameAudio.I?.Play(Sfx.Sparkle, 1f, 0.6f);
                    yield return new WaitForSeconds(0.4f);
                    break;
            }
        }

        public void ShowHint(string[] regionKeys)
        {
            foreach (var k in regionKeys) Highlight(k, Palette.Highlight, true, false);
        }
    }
}
