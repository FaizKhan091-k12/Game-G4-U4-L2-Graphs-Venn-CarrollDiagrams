using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DataChampions
{
    /// <summary>Pause menu (time stops; music keeps playing softly).</summary>
    public class PauseScreen : UIScreen
    {
        public RectTransform panel;
        public Button resume, restart, home, sound;
        public Image soundIcon;

        public static PauseScreen Build(Transform parent, GameConfig cfg)
        {
            var p = Make<PauseScreen>(parent, "Pause");
            UIFactory.Dim(p.transform, 0.72f).raycastTarget = true;
            var face = UIFactory.Panel(p.transform, "Panel", UIFactory.Center, new Vector2(0, 0), new Vector2(720, 760), Palette.Cream, Palette.GoldLip, 56, 18f);
            p.panel = (RectTransform)face.transform.parent;
            UIFactory.Rounded(UIFactory.Stretch(face.transform, "Frame"), Palette.Gold, 56, 10f);
            HowToScreen.Ribbon(p.panel, "PAUSED", new Vector2(0, 302), Palette.Purple, Palette.PurpleLip, 460);
            p.resume = UIFactory.Button(p.panel, "Resume", "RESUME", UIFactory.Center, new Vector2(0, 150), new Vector2(480, 130), Palette.Green, Palette.GreenLip, 60, cfg.S("play"));
            p.restart = UIFactory.Button(p.panel, "Restart", "RESTART", UIFactory.Center, new Vector2(0, -5), new Vector2(480, 130), Palette.Orange, Palette.OrangeLip, 60, cfg.S("replay"));
            p.home = UIFactory.Button(p.panel, "Home", "HOME", UIFactory.Center, new Vector2(0, -160), new Vector2(480, 130), Palette.Blue, Palette.BlueLip, 60, cfg.S("home"));
            p.sound = UIFactory.RoundButton(p.panel, "Sound", cfg.S("sound_on"), UIFactory.Center, new Vector2(0, -297), 100, Palette.Purple, Palette.PurpleLip);
            p.soundIcon = p.sound.transform.Find("Face/Icon").GetComponent<Image>();
            return p;
        }
    }
}
