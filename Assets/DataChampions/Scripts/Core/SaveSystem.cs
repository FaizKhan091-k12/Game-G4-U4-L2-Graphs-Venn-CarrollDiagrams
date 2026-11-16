using System.Collections.Generic;
using UnityEngine;

namespace DataChampions
{
    [System.Serializable]
    public class GameRecord
    {
        public int score, correct, stars, seconds, risksTaken, risksWon, hints, bestStreak;
        public string date;
    }

    [System.Serializable]
    public class SaveData
    {
        public bool soundOn = true;
        public bool seenHowTo;
        public int gamesPlayed;
        public int bestScore;
        public int bestStars;
        public int bestCorrect;
        public int totalCorrect, totalAnswered;
        public int totalCoins;
        public int totalSeconds;
        public int risksTaken, risksWon;
        public int[] skillCorrect = new int[Skills.Count];
        public int[] skillAttempts = new int[Skills.Count];
        public List<string> badges = new List<string>();
        public List<GameRecord> history = new List<GameRecord>();

        public int AccuracyPercent => totalAnswered == 0 ? 0 : Mathf.RoundToInt(100f * totalCorrect / totalAnswered);

        public int SkillPercent(int skill) =>
            skillAttempts[skill] == 0 ? 0 : Mathf.RoundToInt(100f * skillCorrect[skill] / skillAttempts[skill]);
    }

    /// <summary>Progress tracking, stored as JSON in PlayerPrefs (IndexedDB on WebGL).</summary>
    public static class SaveSystem
    {
        const string Key = "DataChampions.Save.v1";
        const int HistoryLength = 8;

        public static SaveData Data { get; private set; }

        public static void Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            Data = null;
            if (!string.IsNullOrEmpty(json))
            {
                try { Data = JsonUtility.FromJson<SaveData>(json); }
                catch (System.Exception e) { Debug.LogWarning("[DataChampions] save unreadable, starting fresh: " + e.Message); }
            }
            if (Data == null) Data = new SaveData();
            if (Data.skillCorrect == null || Data.skillCorrect.Length != Skills.Count) Data.skillCorrect = new int[Skills.Count];
            if (Data.skillAttempts == null || Data.skillAttempts.Length != Skills.Count) Data.skillAttempts = new int[Skills.Count];
            if (Data.badges == null) Data.badges = new List<string>();
            if (Data.history == null) Data.history = new List<GameRecord>();
        }

        public static void Save()
        {
            if (Data == null) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static void RecordAnswer(int skill, bool correct)
        {
            Data.skillAttempts[skill]++;
            Data.totalAnswered++;
            if (correct)
            {
                Data.skillCorrect[skill]++;
                Data.totalCorrect++;
            }
            Save();
        }

        /// <summary>Stores a finished game. Returns the badges that are new for this player.</summary>
        public static List<string> RecordGame(GameRecord rec, IEnumerable<string> earnedBadges, out bool newBest)
        {
            Data.gamesPlayed++;
            newBest = rec.score > Data.bestScore;
            Data.bestScore = Mathf.Max(Data.bestScore, rec.score);
            Data.bestStars = Mathf.Max(Data.bestStars, rec.stars);
            Data.bestCorrect = Mathf.Max(Data.bestCorrect, rec.correct);
            Data.totalCoins += rec.score;
            Data.totalSeconds += rec.seconds;
            Data.risksTaken += rec.risksTaken;
            Data.risksWon += rec.risksWon;
            rec.date = System.DateTime.Now.ToString("d MMM, HH:mm");
            Data.history.Insert(0, rec);
            while (Data.history.Count > HistoryLength) Data.history.RemoveAt(Data.history.Count - 1);
            var fresh = new List<string>();
            foreach (var b in earnedBadges)
                if (!Data.badges.Contains(b)) { Data.badges.Add(b); fresh.Add(b); }
            Save();
            return fresh;
        }

        /// <summary>
        /// Clears all progress (games, scores, stars, skills, badges, history). The sound setting is kept;
        /// "How to play" will show again for the next player.
        /// </summary>
        public static void ResetProgress()
        {
            bool sound = Data == null || Data.soundOn;
            Data = new SaveData { soundOn = sound };
            Save();
        }

#if UNITY_EDITOR
        public static void ResetForTests()
        {
            PlayerPrefs.DeleteKey(Key);
            Load();
        }
#endif
    }
}
