using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DataChampions
{
    public enum Sfx
    {
        UiClick, UiHover, TileFlip, Whoosh, Pop, Slide, BarGrow, Correct, Wrong, Coin, CoinLose, Risk, Safe,
        Drumroll, Streak, Star, Fanfare, Badge, Tick, Sparkle, Hint, Cheer, Aww, Hoot, Logo
    }

    public enum Music { None, Main, Think }

    /// <summary>
    /// Everything the generated scene needs at runtime: sprites (by name), font, audio, logo.
    /// Filled in by the builder (Data Champions > Build Game); lives in Resources so UI code can reach it anywhere.
    /// </summary>
    public class GameConfig : ScriptableObject
    {
        public const string ResourcePath = "DataChampionsConfig";

        [System.Serializable]
        public class NamedSprite
        {
            public string name;
            public Sprite sprite;
        }

        [System.Serializable]
        public class SfxEntry
        {
            public Sfx id;
            public AudioClip clip;
            [Range(0, 1)] public float volume = 0.8f;
        }

        [Header("Look")]
        public TMP_FontAsset font;
        public Material fontOutline;
        public Material fontShadow;
        public List<NamedSprite> sprites = new List<NamedSprite>();
        public Sprite logoSprite;
        public GameObject edovuLogo;

        [Header("Audio")]
        public List<SfxEntry> sfx = new List<SfxEntry>();
        public List<AudioClip> voiceLines = new List<AudioClip>();
        public AudioClip musicMain;
        public AudioClip musicThink;
        [Range(0, 1)] public float musicVolume = 0.32f;
        [Range(0, 1)] public float voiceVolume = 1f;

        static GameConfig cached;
        Dictionary<string, Sprite> lookup;

        public static GameConfig Load()
        {
            if (cached == null) cached = Resources.Load<GameConfig>(ResourcePath);
            return cached;
        }

        /// <summary>Sprite by name (painted icons and Fluent emoji, e.g. "fl_cookie", "star", "venn").</summary>
        public Sprite S(string name)
        {
            if (lookup == null || lookup.Count != sprites.Count)
            {
                lookup = new Dictionary<string, Sprite>();
                foreach (var s in sprites)
                    if (s != null && !string.IsNullOrEmpty(s.name)) lookup[s.name] = s.sprite;
            }
            if (lookup.TryGetValue(name, out var sp)) return sp;
            Debug.LogWarning("[DataChampions] missing sprite " + name);
            return null;
        }

        public static Sprite Get(string name) => Load() != null ? Load().S(name) : null;
    }
}
