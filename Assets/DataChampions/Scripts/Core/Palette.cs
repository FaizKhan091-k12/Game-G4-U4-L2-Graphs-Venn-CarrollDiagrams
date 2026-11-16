using UnityEngine;

namespace DataChampions
{
    /// <summary>
    /// Art bible (single colour source for the whole game).
    ///  Style:   flat-vector TV game-show studio - chunky rounded cards with bold lips, glowing marquee bulbs,
    ///           sweeping spotlights; the diagrams sit on clean cream boards so they stay easy to read.
    ///  Palette: studio indigo/violet, board cream + gold frame, ink for text, five bright "button" hues,
    ///           correct green / wrong coral, risk fire / safe blue, gold accent for everything that scores.
    ///  Mood:    spotlights sweep a deep violet studio; gold bulbs glow; everything that scores sparkles.
    /// </summary>
    public static class Palette
    {
        public static Color Hex(uint rgb, float a = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);

        public static Color WithA(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // studio
        public static readonly Color StudioTop = Hex(0x1C0F4A);
        public static readonly Color StudioBottom = Hex(0x4A1E8A);
        public static readonly Color StudioGlow = Hex(0xFF5FA2);
        public static readonly Color StudioFloor = Hex(0x2A1260);

        // boards & text
        public static readonly Color Cream = Hex(0xFFF7E8);
        public static readonly Color CreamDark = Hex(0xF3E3C3);
        public static readonly Color Paper = Hex(0xFFFFFF);
        public static readonly Color Gold = Hex(0xFFC23D);
        public static readonly Color GoldLip = Hex(0xD9861A);
        public static readonly Color GoldLight = Hex(0xFFE08A);
        public static readonly Color Ink = Hex(0x2B2257);
        public static readonly Color InkSoft = Hex(0x5B5288);
        public static readonly Color Line = Hex(0xCDBFE8);
        public static readonly Color UiShadow = Hex(0x120838, 0.35f);

        // button hues + lips
        public static readonly Color Blue = Hex(0x2FB8FF);
        public static readonly Color BlueLip = Hex(0x1279C9);
        public static readonly Color Orange = Hex(0xFF9B26);
        public static readonly Color OrangeLip = Hex(0xD2650A);
        public static readonly Color Pink = Hex(0xFF4F8B);
        public static readonly Color PinkLip = Hex(0xC21E5C);
        public static readonly Color Green = Hex(0x34D17C);
        public static readonly Color GreenLip = Hex(0x17985A);
        public static readonly Color Purple = Hex(0x8B5CF6);
        public static readonly Color PurpleLip = Hex(0x5B33C2);
        public static readonly Color Yellow = Hex(0xFFD43B);
        public static readonly Color Teal = Hex(0x22C7B8);

        // feedback
        public static readonly Color Correct = Hex(0x2EDB7F);
        public static readonly Color CorrectLip = Hex(0x14A35A);
        public static readonly Color Wrong = Hex(0xFF5468);
        public static readonly Color WrongLip = Hex(0xC4243A);
        public static readonly Color Risk = Hex(0xFF5A36);
        public static readonly Color RiskLip = Hex(0xC22D12);
        public static readonly Color Safe = Hex(0x2FB8FF);
        public static readonly Color SafeLip = Hex(0x1279C9);
        public static readonly Color Locked = Hex(0x8E86B5);
        public static readonly Color LockedLip = Hex(0x625A8C);
        public static readonly Color Highlight = Hex(0xFFE14D);

        // owl
        public static readonly Color OwlBody = Hex(0x8B5CF6);
        public static readonly Color OwlDark = Hex(0x6A3FE0);
        public static readonly Color OwlBelly = Hex(0xEDE3FF);
        public static readonly Color OwlFace = Hex(0xF8F2FF);
        public static readonly Color OwlBeak = Hex(0xFFB020);
        public static readonly Color OwlBeakDark = Hex(0xE58A00);
        public static readonly Color OwlPupil = Hex(0x1B1240);
        public static readonly Color OwlCap = Hex(0x23194F);
        public static readonly Color OwlBow = Hex(0xFF4F6D);

        // tile colours by points
        public static Color TileFace(int points) => points >= 300 ? Pink : points >= 200 ? Orange : Blue;
        public static Color TileLip(int points) => points >= 300 ? PinkLip : points >= 200 ? OrangeLip : BlueLip;

        // diagram accents
        public static readonly Color VennLeft = Hex(0x2FB8FF);
        public static readonly Color VennRight = Hex(0xFF4F8B);
        public static readonly Color HeaderA = Hex(0xFF9B26);
        public static readonly Color HeaderB = Hex(0x8B5CF6);
        public static readonly Color HeaderC = Hex(0x34D17C);
        public static readonly Color RowTint = Hex(0xF6EEDB);
        public static readonly Color[] Bars = { Hex(0x2FB8FF), Hex(0xFF9B26), Hex(0xFF4F8B), Hex(0x34D17C) };
        public static readonly Color[] Confetti = { Hex(0xFFC23D), Hex(0xFF4F8B), Hex(0x2FB8FF), Hex(0x34D17C), Hex(0x8B5CF6), Hex(0xFFFFFF) };
    }
}
