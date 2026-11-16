using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DataChampions
{
    /// <summary>Boot splash: the Edovu logo on white with a loading bar, then a fade into the studio.</summary>
    public class SplashScreen : UIScreen
    {
        public RectTransform logo, barFill;
        public Image shine;
        public TextMeshProUGUI status;

        public Sequence Play(System.Action done)
        {
            Show(true);
            logo.localScale = Vector3.one * 0.7f;
            var logoImg = logo.GetComponent<Image>();
            logoImg.color = Palette.WithA(Color.white, 0f);
            barFill.sizeDelta = new Vector2(0f, barFill.sizeDelta.y);
            float full = ((RectTransform)barFill.parent).sizeDelta.x - 8f;
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.AppendInterval(0.2f);
            s.AppendCallback(() => GameAudio.I?.Play(Sfx.Logo));
            s.Append(logo.DOScale(1f, 0.7f).SetEase(Ease.OutBack));
            s.Join(logoImg.DOFade(1f, 0.4f));
            shine.rectTransform.anchoredPosition = new Vector2(-420f, 0f);
            s.Append(shine.rectTransform.DOAnchorPosX(420f, 0.6f).SetEase(Ease.InOutSine));
            s.Join(barFill.DOSizeDelta(new Vector2(full * 0.6f, barFill.sizeDelta.y), 0.6f));
            s.Append(barFill.DOSizeDelta(new Vector2(full, barFill.sizeDelta.y), 0.7f).SetEase(Ease.InOutSine));
            s.AppendCallback(() => status.text = "Let's go!");
            s.Append(logo.DOPunchScale(Vector3.one * 0.06f, 0.35f, 6, 0.6f));
            s.AppendInterval(0.25f);
            s.Append(group.DOFade(0f, 0.45f));
            s.OnComplete(() => { gameObject.SetActive(false); done?.Invoke(); });
            return s;
        }

        public static SplashScreen Build(Transform parent, GameConfig cfg)
        {
            var s = Make<SplashScreen>(parent, "Splash");
            var bg = s.gameObject.AddComponent<Image>();
            bg.color = Color.white;
            var logoImg = UIFactory.Icon(s.transform, "Logo", cfg.logoSprite, UIFactory.Center, new Vector2(0, 50), 560);
            s.logo = logoImg.rectTransform;
            var shineHost = UIFactory.Rect(s.logo, "ShineMask", UIFactory.Center, Vector2.zero, new Vector2(560, 560));
            shineHost.gameObject.AddComponent<RectMask2D>();
            s.shine = UIFactory.Icon(shineHost, "Shine", cfg.S("beam"), UIFactory.Center, new Vector2(-420, 0), 300, Palette.WithA(Color.white, 0.55f));
            s.shine.preserveAspect = false;
            s.shine.rectTransform.sizeDelta = new Vector2(140, 800);
            s.shine.rectTransform.localRotation = Quaternion.Euler(0, 0, -20f);

            var track = UIFactory.Rect(s.transform, "Bar", UIFactory.Center, new Vector2(0, -300), new Vector2(620, 30));
            UIFactory.Rounded(track, Palette.Hex(0xEDE7F6), 15);
            s.barFill = UIFactory.Rect(track, "Fill", new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(0, 22), new Vector2(0, 0.5f));
            UIFactory.Rounded(s.barFill, Palette.Hex(0xFF6A00), 11);
            s.status = UIFactory.Text(s.transform, "Status", "Loading...", UIFactory.Center, new Vector2(0, -360), new Vector2(600, 60), 36, Palette.Hex(0x1F2A38));
            return s;
        }
    }
}
