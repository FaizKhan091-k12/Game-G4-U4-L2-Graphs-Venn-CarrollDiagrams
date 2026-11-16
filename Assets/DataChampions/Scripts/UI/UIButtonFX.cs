using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DataChampions
{
    /// <summary>DOTween feel for buttons: hover grow, press sink onto the lip, click bounce + pop sound + sparkle burst.</summary>
    public class UIButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public RectTransform face;
        public bool burst = true;
        public bool idlePulse;
        public bool clickSound = true;

        Vector3 baseScale = Vector3.one;
        Button button;
        bool hovered;
        Tween scaleTween, pulseTween;

        void Awake()
        {
            baseScale = transform.localScale;
            button = GetComponent<Button>();
        }

        void OnEnable()
        {
            if (idlePulse) StartPulse();
        }

        public void SetPulse(bool on)
        {
            idlePulse = on;
            pulseTween?.Kill();
            pulseTween = null;
            if (face != null) face.localScale = Vector3.one;
            if (on && isActiveAndEnabled) StartPulse();
        }

        void StartPulse()
        {
            if (face == null) return;
            pulseTween?.Kill();
            face.localScale = Vector3.one;
            pulseTween = face.DOScale(1.06f, 0.55f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
        }

        bool Active => button == null || button.interactable;

        public void OnPointerEnter(PointerEventData e)
        {
            if (!Active) return;
            hovered = true;
            scaleTween?.Kill();
            scaleTween = transform.DOScale(baseScale * 1.06f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
            if (e.pointerId < 0) GameAudio.I?.Play(Sfx.UiHover, Random.Range(0.95f, 1.08f), 0.3f);
        }

        public void OnPointerExit(PointerEventData e)
        {
            hovered = false;
            scaleTween?.Kill();
            scaleTween = transform.DOScale(baseScale, 0.18f).SetUpdate(true);
            if (face != null) face.DOAnchorPos(Vector2.zero, 0.1f).SetUpdate(true);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!Active || face == null) return;
            face.DOKill();
            pulseTween = null;
            face.localScale = Vector3.one;
            face.DOAnchorPos(new Vector2(0, -9f), 0.06f).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (face == null) return;
            face.DOKill();
            face.DOAnchorPos(Vector2.zero, 0.14f).SetEase(Ease.OutBack).SetUpdate(true);
            if (idlePulse && isActiveAndEnabled) StartPulse();
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!Active) return;
            if (clickSound) GameAudio.I?.Play(Sfx.UiClick, Random.Range(0.96f, 1.05f));
            scaleTween?.Kill();
            transform.localScale = baseScale * (hovered ? 1.06f : 1f);
            scaleTween = transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 8, 0.7f).SetUpdate(true);
            if (burst) UIFx.Burst(transform.position, Palette.Gold, 8, 100f);
        }

        void OnDisable()
        {
            scaleTween?.Kill();
            pulseTween?.Kill();
            transform.localScale = baseScale;
            hovered = false;
            if (face != null) { face.DOKill(); face.anchoredPosition = Vector2.zero; face.localScale = Vector3.one; }
        }
    }
}
