using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DataChampions
{
    /// <summary>One numbered tile on the board. Front: number, points, diagram type. Back (answered): tick/cross and coins won.</summary>
    public class TileView : MonoBehaviour
    {
        public Button button;
        public RectTransform front, back, body;
        public ProceduralImage backFace, backLip;
        public Image backIcon;
        public TextMeshProUGUI backText;
        public int index;
        public bool answered;

        Tween bob;

        public void StartBob(float delay)
        {
            bob?.Kill();
            body.anchoredPosition = Vector2.zero;
            bob = body.DOAnchorPosY(7f, 1.3f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(delay).SetUpdate(true).SetLink(gameObject);
        }

        public void StopBob()
        {
            bob?.Kill();
            body.anchoredPosition = Vector2.zero;
        }

        public void SetResult(bool correct, int delta, bool instant)
        {
            answered = true;
            button.interactable = false;
            StopBob();
            var c = correct ? Palette.Correct : Palette.Locked;
            backFace.color = c;
            backLip.color = correct ? Palette.CorrectLip : Palette.LockedLip;
            backIcon.sprite = GameConfig.Get(correct ? "check" : "cross");
            backText.text = delta > 0 ? "+" + delta : delta < 0 ? delta.ToString() : "0";
            if (instant)
            {
                front.gameObject.SetActive(false);
                back.gameObject.SetActive(true);
                return;
            }
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.Append(body.DOScaleX(0f, 0.16f).SetEase(Ease.InQuad));
            s.AppendCallback(() =>
            {
                front.gameObject.SetActive(false);
                back.gameObject.SetActive(true);
                GameAudio.I?.Play(Sfx.TileFlip);
            });
            s.Append(body.DOScaleX(1f, 0.22f).SetEase(Ease.OutBack));
            s.Append(body.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.6f));
            s.AppendCallback(() => UIFx.Burst(transform.position, correct ? Palette.Correct : Palette.Gold, 10, 140f));
        }

        public void ResetTile()
        {
            answered = false;
            button.interactable = true;
            body.localScale = Vector3.one;
            front.gameObject.SetActive(true);
            back.gameObject.SetActive(false);
        }

        public static TileView Build(Transform parent, int index, int points, int skill, Vector2 pos, Vector2 size)
        {
            var root = UIFactory.Rect(parent, "Tile" + (index + 1), UIFactory.Center, pos, size);
            var tv = root.gameObject.AddComponent<TileView>();
            tv.index = index;
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            tv.button = root.gameObject.AddComponent<Button>();
            tv.button.transition = Selectable.Transition.None;
            tv.button.targetGraphic = hit;
            tv.body = UIFactory.Stretch(root, "Body");
            var fx = root.gameObject.AddComponent<UIButtonFX>();
            fx.face = tv.body;

            // front
            tv.front = UIFactory.Stretch(tv.body, "Front");
            var sh = UIFactory.Stretch(tv.front, "Shadow");
            sh.offsetMin = new Vector2(6, -26); sh.offsetMax = new Vector2(6, -26);
            UIFactory.Rounded(sh, Palette.UiShadow, 36);
            var lip = UIFactory.Stretch(tv.front, "Lip");
            lip.offsetMin = new Vector2(0, -16); lip.offsetMax = new Vector2(0, -16);
            UIFactory.Rounded(lip, Palette.TileLip(points), 36);
            UIFactory.Rounded(UIFactory.Stretch(tv.front, "Face"), Palette.TileFace(points), 36);
            UIFactory.Rounded(UIFactory.Stretch(tv.front, "Rim", 10), Palette.WithA(Color.white, 0.35f), 28, 4f);
            var shine = UIFactory.Rect(tv.front, "Shine", new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(size.x - 50, 34), new Vector2(0.5f, 1f));
            UIFactory.Rounded(shine, Palette.WithA(Color.white, 0.25f), 17);
            var num = UIFactory.Text(tv.front, "Number", (index + 1).ToString(), UIFactory.Center, new Vector2(0, 22), new Vector2(size.x, 140), 128, Color.white, TextAlignmentOptions.Center, true);
            var pill = UIFactory.Rect(tv.front, "Points", new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(170, 52));
            UIFactory.Rounded(pill, Palette.WithA(Palette.StudioTop, 0.45f), 26);
            UIFactory.Icon(pill, "Coin", GameConfig.Get("fl_coin"), new Vector2(0, 0.5f), new Vector2(28, 0), 46);
            UIFactory.Text(pill, "Value", points.ToString(), UIFactory.Center, new Vector2(20, 2), new Vector2(110, 50), 38, Palette.GoldLight, TextAlignmentOptions.Center, true);
            var badge = UIFactory.Rect(tv.front, "Type", new Vector2(1f, 1f), new Vector2(-30, -30), new Vector2(56, 56));
            UIFactory.Rounded(badge, Palette.WithA(Color.white, 0.92f), -1);
            UIFactory.Icon(badge, "Icon", GameConfig.Get(Skills.Icons[skill]), UIFactory.Center, Vector2.zero, 40, Palette.TileLip(points));

            // back (answered)
            tv.back = UIFactory.Stretch(tv.body, "Back");
            var bl = UIFactory.Stretch(tv.back, "Lip");
            bl.offsetMin = new Vector2(0, -16); bl.offsetMax = new Vector2(0, -16);
            tv.backLip = UIFactory.Rounded(bl, Palette.CorrectLip, 36);
            tv.backFace = UIFactory.Rounded(UIFactory.Stretch(tv.back, "Face"), Palette.Correct, 36);
            tv.backIcon = UIFactory.Icon(tv.back, "Icon", GameConfig.Get("check"), UIFactory.Center, new Vector2(0, 26), 110, Color.white);
            tv.backText = UIFactory.Text(tv.back, "Delta", "+200", new Vector2(0.5f, 0f), new Vector2(0, 46), new Vector2(size.x, 60), 48, Color.white, TextAlignmentOptions.Center, true);
            tv.back.gameObject.SetActive(false);
            return tv;
        }
    }
}
