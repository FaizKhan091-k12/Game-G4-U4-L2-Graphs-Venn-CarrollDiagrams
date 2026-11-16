using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataChampions
{
    /// <summary>
    /// Pooled UI effects on their own top canvas: sparkle bursts, confetti, floating score text, flying coins, rings.
    /// Positions are world positions (screen-space overlay canvases share the same space), so any screen can call it.
    /// Nothing is instantiated per event after warm-up.
    /// </summary>
    public class UIFx : MonoBehaviour
    {
        public static UIFx I { get; private set; }

        RectTransform root;
        GameConfig cfg;
        readonly List<Image> images = new List<Image>();
        readonly List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();
        int nextImage, nextText;
        float canvasScale = 1f;

        public void Init(RectTransform fxRoot, GameConfig config)
        {
            I = this;
            root = fxRoot;
            cfg = config;
            for (int i = 0; i < 140; i++) images.Add(MakeImage());
            for (int i = 0; i < 8; i++) texts.Add(MakeText());
        }

        void LateUpdate()
        {
            canvasScale = root.lossyScale.x > 0 ? root.lossyScale.x : 1f;
        }

        Image MakeImage()
        {
            var rt = UIFactory.Rect(root, "Fx", UIFactory.Center, Vector2.zero, new Vector2(24, 24));
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            rt.gameObject.SetActive(false);
            return img;
        }

        TextMeshProUGUI MakeText()
        {
            var rt = UIFactory.Rect(root, "FxText", UIFactory.Center, Vector2.zero, new Vector2(600, 140));
            var t = UIFactory.Label(rt, "T", "", 72, Color.white, TextAlignmentOptions.Center, true);
            rt.gameObject.SetActive(false);
            return t;
        }

        Image TakeImage(Sprite s, Color c, float size)
        {
            Image img = null;
            for (int i = 0; i < images.Count; i++)
            {
                var cand = images[(nextImage + i) % images.Count];
                if (!cand.gameObject.activeSelf) { img = cand; nextImage = (nextImage + i + 1) % images.Count; break; }
            }
            if (img == null) { img = images[nextImage]; nextImage = (nextImage + 1) % images.Count; img.rectTransform.DOKill(); img.DOKill(); }
            img.sprite = s;
            img.color = c;
            img.preserveAspect = true;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(size, size);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.SetAsLastSibling();
            img.gameObject.SetActive(true);
            return img;
        }

        static void Release(Image img)
        {
            if (img != null) img.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ effects

        /// <summary>Sparkle burst around a point (button clicks, stars, correct answers).</summary>
        public static void Burst(Vector3 worldPos, Color color, int count = 10, float distance = 120f)
        {
            if (I == null) return;
            I.DoBurst(worldPos, color, count, distance);
        }

        void DoBurst(Vector3 worldPos, Color color, int count, float distance)
        {
            for (int i = 0; i < count; i++)
            {
                var sprite = i % 3 == 0 ? cfg.S("sparkle") : i % 3 == 1 ? cfg.S("star") : cfg.S("soft");
                var img = TakeImage(sprite, i % 4 == 0 ? Color.white : color, Random.Range(18f, 34f));
                var rt = img.rectTransform;
                rt.position = worldPos;
                float a = (360f / count * i + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                float d = distance * Random.Range(0.7f, 1.2f) * canvasScale;
                var s = DOTween.Sequence().SetUpdate(true).SetLink(img.gameObject);
                s.Append(rt.DOMove(worldPos + dir * d, 0.5f).SetEase(Ease.OutCubic));
                s.Join(rt.DOScale(0.15f, 0.5f).SetEase(Ease.InQuad));
                s.Join(rt.DOLocalRotate(new Vector3(0, 0, Random.Range(-180f, 180f)), 0.5f, RotateMode.FastBeyond360));
                s.Join(img.DOFade(0f, 0.5f).SetEase(Ease.InQuad));
                s.OnComplete(() => Release(img));
            }
        }

        /// <summary>Full-screen confetti rain (milestones only, so it stays special).</summary>
        public static void Confetti(int count = 60, float duration = 2.6f)
        {
            if (I == null) return;
            I.DoConfetti(count, duration);
        }

        void DoConfetti(int count, float duration)
        {
            var size = root.rect.size;
            for (int i = 0; i < count; i++)
            {
                var c = Palette.Confetti[i % Palette.Confetti.Length];
                var img = TakeImage(i % 5 == 0 ? cfg.S("star") : null, c, Random.Range(18f, 30f));
                var rt = img.rectTransform;
                if (img.sprite == null) rt.sizeDelta = new Vector2(Random.Range(14f, 22f), Random.Range(24f, 36f));
                var start = new Vector2(Random.Range(-size.x * 0.5f, size.x * 0.5f), size.y * 0.5f + Random.Range(20f, 220f));
                rt.anchoredPosition = start;
                rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                float t = duration * Random.Range(0.75f, 1.15f);
                float drift = Random.Range(-160f, 160f);
                var s = DOTween.Sequence().SetUpdate(true).SetLink(img.gameObject);
                s.AppendInterval(Random.Range(0f, 0.6f));
                s.Append(rt.DOAnchorPos(new Vector2(start.x + drift, -size.y * 0.5f - 60f), t).SetEase(Ease.InSine));
                s.Join(rt.DOLocalRotate(new Vector3(0, 0, Random.Range(360f, 900f) * (Random.value < 0.5f ? -1 : 1)), t, RotateMode.FastBeyond360).SetEase(Ease.Linear));
                s.Join(rt.DOScaleX(Random.Range(-1f, 1f), t * 0.25f).SetLoops(4, LoopType.Yoyo).SetEase(Ease.InOutSine));
                s.OnComplete(() => Release(img));
            }
        }

        /// <summary>Text that pops, rises and fades ("+400!", "-200").</summary>
        public static void FloatText(Vector3 worldPos, string text, Color color, float size = 80f, float rise = 150f, float hold = 0.5f)
        {
            if (I == null) return;
            I.DoFloat(worldPos, text, color, size, rise, hold);
        }

        void DoFloat(Vector3 worldPos, string text, Color color, float size, float rise, float hold)
        {
            var t = texts[nextText];
            nextText = (nextText + 1) % texts.Count;
            var rt = (RectTransform)t.transform.parent;
            rt.DOKill();
            t.DOKill();
            rt.gameObject.SetActive(true);
            rt.SetAsLastSibling();
            rt.position = worldPos;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            rt.localScale = Vector3.zero;
            var target = rt.anchoredPosition + new Vector2(0, rise);
            var s = DOTween.Sequence().SetUpdate(true).SetLink(rt.gameObject);
            s.Append(rt.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
            s.Join(rt.DOAnchorPos(target, 0.9f + hold).SetEase(Ease.OutCubic));
            s.Insert(0.55f + hold, t.DOFade(0f, 0.4f));
            s.OnComplete(() => rt.gameObject.SetActive(false));
        }

        /// <summary>Coins arc from one point to another (score counter). onEach fires as each coin lands.</summary>
        public static void CoinFly(Vector3 from, Vector3 to, int count, System.Action onEach, System.Action onDone = null)
        {
            if (I == null) { onDone?.Invoke(); return; }
            I.DoCoins(from, to, count, onEach, onDone);
        }

        void DoCoins(Vector3 from, Vector3 to, int count, System.Action onEach, System.Action onDone)
        {
            var coin = cfg.S("fl_coin");
            int landed = 0;
            for (int i = 0; i < count; i++)
            {
                var img = TakeImage(coin, Color.white, 64f);
                var rt = img.rectTransform;
                rt.position = from;
                rt.localScale = Vector3.zero;
                var spread = new Vector3(Random.Range(-110f, 110f), Random.Range(-40f, 120f), 0f) * canvasScale;
                var mid = from + spread;
                var s = DOTween.Sequence().SetUpdate(true).SetLink(img.gameObject);
                s.AppendInterval(i * 0.05f);
                s.Append(rt.DOScale(1f, 0.18f).SetEase(Ease.OutBack));
                s.Join(rt.DOMove(mid, 0.25f).SetEase(Ease.OutCubic));
                s.Append(rt.DOMove(to, 0.45f).SetEase(Ease.InCubic));
                s.Join(rt.DOScale(0.55f, 0.45f).SetEase(Ease.InQuad));
                s.OnComplete(() =>
                {
                    Release(img);
                    onEach?.Invoke();
                    landed++;
                    if (landed == count) onDone?.Invoke();
                });
            }
        }

        /// <summary>Coins fall away from a point (risk lost).</summary>
        public static void CoinDrop(Vector3 from, int count)
        {
            if (I == null) return;
            I.DoCoinDrop(from, count);
        }

        void DoCoinDrop(Vector3 from, int count)
        {
            var coin = cfg.S("fl_coin");
            for (int i = 0; i < count; i++)
            {
                var img = TakeImage(coin, Color.white, 56f);
                var rt = img.rectTransform;
                rt.position = from;
                var target = from + new Vector3(Random.Range(-260f, 260f), Random.Range(-520f, -380f), 0f) * canvasScale;
                var s = DOTween.Sequence().SetUpdate(true).SetLink(img.gameObject);
                s.AppendInterval(i * 0.04f);
                s.Append(rt.DOJump(target, 120f * canvasScale, 1, 0.8f).SetEase(Ease.Linear));
                s.Join(rt.DOLocalRotate(new Vector3(0, 0, Random.Range(-540f, 540f)), 0.8f, RotateMode.FastBeyond360));
                s.Join(img.DOFade(0f, 0.8f).SetEase(Ease.InQuad));
                s.OnComplete(() => Release(img));
            }
        }

        /// <summary>An expanding ring (answer feedback, tile pick).</summary>
        public static void Ring(Vector3 worldPos, Color color, float size = 260f)
        {
            if (I == null) return;
            var img = I.TakeImage(I.cfg.S("ring"), color, size * 0.4f);
            var rt = img.rectTransform;
            rt.position = worldPos;
            var s = DOTween.Sequence().SetUpdate(true).SetLink(img.gameObject);
            s.Append(rt.DOSizeDelta(new Vector2(size, size), 0.5f).SetEase(Ease.OutCubic));
            s.Join(img.DOFade(0f, 0.5f).SetEase(Ease.InQuad));
            s.OnComplete(() => Release(img));
        }
    }
}
