using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataChampions
{
    /// <summary>Professor Hoot's speech bubble (subtitles for every spoken line). Grows to fit the text.</summary>
    public class SpeechBubble : MonoBehaviour
    {
        public RectTransform body;
        public TextMeshProUGUI text;
        public float maxWidth = 520f;

        Sequence seq;

        public void Say(string line, float seconds)
        {
            if (string.IsNullOrEmpty(line)) { Hide(); return; }
            gameObject.SetActive(true);
            text.text = line;
            text.ForceMeshUpdate();
            var pref = text.GetPreferredValues(line, maxWidth - 56f, 0f);
            float w = Mathf.Clamp(pref.x + 60f, 180f, maxWidth);
            pref = text.GetPreferredValues(line, w - 56f, 0f);
            body.sizeDelta = new Vector2(w, Mathf.Max(90f, pref.y + 44f));
            seq?.Kill();
            body.localScale = Vector3.zero;
            seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            seq.Append(body.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            seq.AppendInterval(Mathf.Max(1.6f, seconds + 0.6f));
            seq.Append(body.DOScale(0f, 0.2f).SetEase(Ease.InBack));
            seq.OnComplete(() => gameObject.SetActive(false));
        }

        public void Hide()
        {
            seq?.Kill();
            if (!gameObject.activeSelf) return;
            seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            seq.Append(body.DOScale(0f, 0.15f));
            seq.OnComplete(() => gameObject.SetActive(false));
        }

        /// <summary>pivot = where the bubble grows from (its tail corner).</summary>
        public static SpeechBubble Build(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 pivot, float maxWidth, float fontSize, bool tailLeft)
        {
            var root = UIFactory.Rect(parent, name, anchor, pos, new Vector2(10, 10));
            var sb = root.gameObject.AddComponent<SpeechBubble>();
            sb.maxWidth = maxWidth;
            sb.body = UIFactory.Rect(root, "Body", UIFactory.Center, Vector2.zero, new Vector2(maxWidth, 120), pivot);
            var sh = UIFactory.Stretch(sb.body, "Shadow");
            sh.offsetMin = new Vector2(4, -10); sh.offsetMax = new Vector2(4, -10);
            UIFactory.Rounded(sh, Palette.UiShadow, 34);
            var tail = UIFactory.Rect(sb.body, "Tail", new Vector2(tailLeft ? 0f : 1f, 0f), new Vector2(tailLeft ? 40f : -40f, -14f), new Vector2(46, 40));
            var ti = tail.gameObject.AddComponent<Image>();
            ti.sprite = GameConfig.Get("tri");
            ti.color = Color.white;
            ti.raycastTarget = false;
            tail.localRotation = Quaternion.Euler(0, 0, tailLeft ? 200f : 160f);
            UIFactory.Rounded(UIFactory.Stretch(sb.body, "Face"), Color.white, 34);
            sb.text = UIFactory.Label(sb.body, "Text", "", fontSize, Palette.Ink, TextAlignmentOptions.Center);
            sb.text.rectTransform.offsetMin = new Vector2(28, 18);
            sb.text.rectTransform.offsetMax = new Vector2(-28, -18);
            sb.text.textWrappingMode = TextWrappingModes.Normal;
            root.gameObject.SetActive(false);
            return sb;
        }
    }
}
