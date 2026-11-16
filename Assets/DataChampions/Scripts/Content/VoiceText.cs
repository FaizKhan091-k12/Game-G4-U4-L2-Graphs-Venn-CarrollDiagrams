using System.Collections.Generic;

namespace DataChampions
{
    /// <summary>
    /// What Professor Hoot says, as shown in his speech bubble (subtitles). Keys match Audio/Voice/vo_&lt;key&gt;.mp3
    /// (ElevenLabs "George", British English). Question, hint and explanation lines come from QuestionBank.
    /// </summary>
    public static class VoiceText
    {
        public static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
        {
            { "welcome", "Welcome to Data Champions! I'm Professor Hoot. Let's read some graphs and diagrams!" },
            { "welcome_back", "Welcome back, champion! Ready for another round?" },
            { "howto", "Pick a tile. Read the diagram carefully. Then play it SAFE, or RISK IT for double points!" },
            { "pick_tile", "Pick a tile!" },
            { "pick_tile2", "Which tile next?" },
            { "last_tile", "Last tile! Make it count!" },
            { "choose_wager", "Play it safe, or risk it?" },
            { "risk_locked", "Earn more coins first, then you can risk it!" },
            { "safe", "Safe and steady!" },
            { "risk", "Ooh, risky! Win double, or lose your points!" },
            { "correct_1", "Brilliant!" },
            { "correct_2", "Spot on!" },
            { "correct_3", "Superb! You read that perfectly." },
            { "correct_4", "Fantastic work!" },
            { "correct_5", "Yes! That's right!" },
            { "risk_win", "Your risk paid off! Double points!" },
            { "wrong_1", "Not quite! Let's look together." },
            { "wrong_2", "Oops! Let's check the diagram." },
            { "wrong_3", "Good try! Here's how it works." },
            { "risk_lose", "Oh no! The risk didn't pay off this time." },
            { "streak_3", "Three in a row! You're on fire!" },
            { "streak_5", "Five in a row! Unstoppable!" },
            { "result_3", "Incredible! Three stars! You're a true Data Champion!" },
            { "result_2", "Great work! Two stars! Can you get three next time?" },
            { "result_1", "Well done for finishing! Play again to win more stars!" },
            { "new_best", "That's a new best score!" },
            { "badge", "You've unlocked a new badge!" },
            { "turn", "Please turn your device sideways." },
        };

        public static string Get(string key)
        {
            if (Lines.TryGetValue(key, out var t)) return t;
            if (key.Length > 1 && (key[0] == 'h') && int.TryParse(key.Substring(1), out int id))
            {
                var q = QuestionBank.ById(id);
                if (q != null) return q.hint;
            }
            return null;
        }
    }
}
