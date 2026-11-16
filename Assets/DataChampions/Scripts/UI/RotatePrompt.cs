using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataChampions
{
    /// <summary>
    /// On a phone held upright the diagrams get tiny, so this asks the player to turn the device sideways
    /// (an animated phone rotates). It hides by itself once the screen is landscape; "Play anyway" dismisses it.
    /// </summary>
    public class RotatePrompt : MonoBehaviour
    {
        public CanvasGroup group;
        public RectTransform phone;
        public Button playAnyway;

        const float PortraitAspect = 0.95f;
        bool dismissed, visible, spoke;
        Tween spin;

        void Start()
        {
            playAnyway.onClick.AddListener(() => { dismissed = true; SetVisible(false); });
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        void Update()
        {
            bool portrait = Screen.height > 0 && (float)Screen.width / Screen.height < PortraitAspect;
            SetVisible(portrait && !dismissed);
        }

        void SetVisible(bool on)
        {
            if (on == visible) return;
            visible = on;
            group.DOKill();
            group.DOFade(on ? 1f : 0f, 0.25f).SetUpdate(true);
            group.blocksRaycasts = on;
            group.interactable = on;
            spin?.Kill();
            if (!on) return;
            if (!spoke && GameAudio.I != null) { spoke = true; GameAudio.I.Speak("turn"); }
            phone.localRotation = Quaternion.identity;
            spin = DOTween.Sequence().SetUpdate(true).SetLoops(-1)
                .AppendInterval(0.5f)
                .Append(phone.DOLocalRotate(new Vector3(0, 0, -90f), 0.6f).SetEase(Ease.InOutBack))
                .AppendInterval(0.9f)
                .Append(phone.DOLocalRotate(Vector3.zero, 0.4f).SetEase(Ease.InOutSine));
        }

        public static RotatePrompt Build(Transform canvas, GameConfig cfg)
        {
            var root = UIFactory.Stretch(canvas, "RotatePrompt");
            var p = root.gameObject.AddComponent<RotatePrompt>();
            p.group = root.gameObject.AddComponent<CanvasGroup>();
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = Palette.WithA(Palette.StudioBottom, 0.98f);

            // In portrait the canvas expands vertically, so content is centred and sized for the 1920-wide reference.
            var phoneRt = UIFactory.Rect(root, "Phone", UIFactory.Center, new Vector2(0, 400), new Vector2(380, 640));
            UIFactory.Rounded(phoneRt, Palette.Ink, 70);
            var screen = UIFactory.Stretch(phoneRt, "Screen", 26);
            UIFactory.Rounded(screen, Palette.Blue, 48);
            UIFactory.Icon(screen, "Chart", cfg.S("fl_bar_chart"), UIFactory.Center, Vector2.zero, 210);
            p.phone = phoneRt;

            var arrow = UIFactory.Icon(root, "Arrow", cfg.S("replay"), UIFactory.Center, new Vector2(330, 680), 170, Palette.Gold);
            arrow.rectTransform.localScale = new Vector3(-1, 1, 1);

            var t = UIFactory.Paragraph(root, "Title", "Turn your device sideways!", UIFactory.Center, new Vector2(0, -60), new Vector2(1700, 260), 120, 70, Color.white);
            t.fontSharedMaterial = cfg.fontOutline;
            UIFactory.Paragraph(root, "Sub", "Data Champions plays best in landscape.", UIFactory.Center, new Vector2(0, -250), new Vector2(1700, 120), 70, 44, Palette.WithA(Color.white, 0.92f));

            p.playAnyway = UIFactory.Button(root, "PlayAnyway", "Play anyway", UIFactory.Center, new Vector2(0, -470),
                new Vector2(640, 170), Palette.Orange, Palette.OrangeLip, 76);
            return p;
        }
    }
}
