using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataChampions
{
    /// <summary>Full-screen colour wipe for screen changes (0.3 s, unscaled).</summary>
    public class Fader : MonoBehaviour
    {
        public Image image;

        public Tween FadeOut(float t = 0.28f)
        {
            image.DOKill();
            image.raycastTarget = true;
            return image.DOFade(1f, t).SetUpdate(true);
        }

        public Tween FadeIn(float t = 0.32f)
        {
            image.DOKill();
            return image.DOFade(0f, t).SetUpdate(true).OnComplete(() => image.raycastTarget = false);
        }

        public static Fader Build(Transform parent)
        {
            var rt = UIFactory.Stretch(parent, "Fader");
            var f = rt.gameObject.AddComponent<Fader>();
            f.image = rt.gameObject.AddComponent<Image>();
            f.image.color = Palette.WithA(Palette.StudioTop, 0f);
            f.image.raycastTarget = false;
            return f;
        }
    }
}
