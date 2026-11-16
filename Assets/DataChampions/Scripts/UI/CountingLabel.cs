using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataChampions
{
    /// <summary>A number that counts up/down smoothly with a punch when it changes (coins, score).</summary>
    public class CountingLabel : MonoBehaviour
    {
        public TextMeshProUGUI label;
        public string prefix = "";
        int shown, target;
        Tween tween;

        public int Value => target;

        public void Set(int v)
        {
            tween?.Kill();
            shown = target = v;
            label.text = prefix + v;
        }

        public void AnimateTo(int v, float duration = 0.6f)
        {
            if (v == target) return;
            bool up = v > target;
            target = v;
            tween?.Kill();
            tween = DOTween.To(() => shown, x => { shown = x; label.text = prefix + x; }, v, duration).SetEase(Ease.OutCubic).SetUpdate(true);
            label.transform.DOKill(true);
            label.transform.DOPunchScale(Vector3.one * (up ? 0.25f : -0.15f), 0.35f, 6, 0.6f).SetUpdate(true);
        }
    }
}
