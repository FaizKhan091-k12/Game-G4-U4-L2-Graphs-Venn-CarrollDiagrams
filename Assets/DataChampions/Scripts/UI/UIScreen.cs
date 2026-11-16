using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataChampions
{
    /// <summary>Base for full screens: a CanvasGroup that fades/pops in and out.</summary>
    public class UIScreen : MonoBehaviour
    {
        public CanvasGroup group;

        public bool IsOpen => gameObject.activeSelf && group.alpha > 0.01f;

        public virtual void Show(bool instant = false)
        {
            gameObject.SetActive(true);
            group.DOKill();
            group.blocksRaycasts = true;
            group.interactable = true;
            if (instant) { group.alpha = 1f; return; }
            group.alpha = 0f;
            group.DOFade(1f, 0.3f).SetUpdate(true);
        }

        public virtual void Hide(bool instant = false)
        {
            group.DOKill();
            group.blocksRaycasts = false;
            group.interactable = false;
            if (instant || !gameObject.activeSelf) { group.alpha = 0f; gameObject.SetActive(false); return; }
            group.DOFade(0f, 0.25f).SetUpdate(true).OnComplete(() => gameObject.SetActive(false));
        }

        protected static T Make<T>(Transform parent, string name) where T : UIScreen
        {
            var rt = UIFactory.Stretch(parent, name);
            var s = rt.gameObject.AddComponent<T>();
            s.group = rt.gameObject.AddComponent<CanvasGroup>();
            return s;
        }

        /// <summary>Pops a rect in with an overshoot after a delay (used for staggered entrances).</summary>
        public static Tween PopIn(Transform t, float delay, float duration = 0.4f, bool unscaled = true)
        {
            t.DOKill();
            t.localScale = Vector3.zero;
            return t.DOScale(1f, duration).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(unscaled);
        }

        public static Tween SlideIn(RectTransform rt, Vector2 from, float delay, float duration = 0.5f, bool unscaled = true)
        {
            var to = rt.anchoredPosition;
            rt.DOKill();
            rt.anchoredPosition = to + from;
            return rt.DOAnchorPos(to, duration).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(unscaled);
        }
    }
}
