using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DataChampions
{
    /// <summary>
    /// Reports progress to the hosting page (postMessage to the parent frame + a "datachampions-progress" DOM event),
    /// so an LMS wrapper can record it. Logs to the console outside WebGL.
    /// </summary>
    public static class WebBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void DC_Report(string json);
#endif

        [System.Serializable]
        public class QuestionResult
        {
            public int question, tile, delta, seconds;
            public string chosen, answer;
            public bool correct, risk, hint, fast;
        }

        [System.Serializable]
        class AnswerReport
        {
            public string game = "DataChampions";
            public string eventName = "questionAnswered";
            public QuestionResult result;
            public int score;
        }

        [System.Serializable]
        class GameReport
        {
            public string game = "DataChampions";
            public string eventName = "gameComplete";
            public int score, correct, outOf, stars, seconds, risksTaken, risksWon, hintsUsed, bestStreak;
            public int bestScore, gamesPlayed, accuracyPercent;
            public bool newBest;
            public List<string> badges;
            public List<string> newBadges;
            public List<QuestionResult> questions;
        }

        public static void ReportAnswer(QuestionResult r, int score) =>
            Send(JsonUtility.ToJson(new AnswerReport { result = r, score = score }));

        public static void ReportGame(GameRecord rec, bool newBest, List<string> newBadges, List<QuestionResult> questions)
        {
            var d = SaveSystem.Data;
            Send(JsonUtility.ToJson(new GameReport
            {
                score = rec.score, correct = rec.correct, outOf = QuestionBank.All.Count, stars = rec.stars, seconds = rec.seconds,
                risksTaken = rec.risksTaken, risksWon = rec.risksWon, hintsUsed = rec.hints, bestStreak = rec.bestStreak,
                bestScore = d.bestScore, gamesPlayed = d.gamesPlayed, accuracyPercent = d.AccuracyPercent, newBest = newBest,
                badges = new List<string>(d.badges), newBadges = newBadges, questions = questions
            }));
        }

        [System.Serializable]
        class ResetReport
        {
            public string game = "DataChampions";
            public string eventName = "progressReset";
        }

        /// <summary>Tells the host page the player wiped their saved progress.</summary>
        public static void ReportReset() => Send(JsonUtility.ToJson(new ResetReport()));

        static void Send(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { DC_Report(json); } catch (System.Exception e) { Debug.LogWarning("[DataChampions] report failed: " + e.Message); }
#else
            Debug.Log("[DataChampions] progress " + json);
#endif
        }
    }
}
