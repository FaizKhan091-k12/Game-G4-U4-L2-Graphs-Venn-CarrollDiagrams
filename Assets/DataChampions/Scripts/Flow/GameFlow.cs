using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DataChampions
{
    /// <summary>
    /// The whole game in one scene: Splash -> Title -> (How to play) -> Board -> Question -> ... -> Results.
    /// Scoring: right answer wins the tile's coins (RISK IT doubles them), a wrong RISK loses them, 3+ in a row adds a
    /// streak bonus, a hint costs 50. Progress is saved and reported to the hosting page.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public const int HintCost = 50;
        public const int StreakBonus = 50;
        public const int SpeedBonus = 50;
        public const float QuestionSeconds = 45f;
        public const float FastSeconds = 15f;
        const int TimeOut = -2;

        public GameConfig config;
        public GameAudio audioManager;
        public UIFx fx;
        public RectTransform fxRoot;
        public Studio studio;
        public Hud hud;
        public SplashScreen splash;
        public TitleScreen title;
        public HowToScreen howTo;
        public BoardScreen board;
        public QuestionScreen question;
        public ResultsScreen results;
        public ProgressScreen progress;
        public PauseScreen pause;
        public RotatePrompt rotate;
        public Fader fader;
        public RectTransform edovuCorner;

        public enum State { Splash, Title, Board, Question, Results }
        public State Current { get; private set; }

        class Run
        {
            public int[] tileQuestion = new int[10];
            public int[] tileState = new int[10];   // 0 open, 1 right, 2 wrong
            public int[] tileDelta = new int[10];
            public bool[] questionRight = new bool[11];
            public int score, streak, bestStreak, correct, answered, risksTaken, risksWon, hints, fastAnswers;
            public float startTime;
            public List<WebBridge.QuestionResult> results = new List<WebBridge.QuestionResult>();
        }

        Run run;
        Coroutine flow;
        SpeechBubble bubble;
        bool busy, paused, waitingNext, hintUsed, startAfterHowTo;
        int wagerChoice = -1, answerChoice = -1, currentTile = -1;
        QuestionDef currentQ;
        float timeLeft;

        // test hooks (read by the editor autoplayer)
        public int Score => run != null ? run.score : 0;
        public int Answered => run != null ? run.answered : 0;
        public bool WaitingForWager => Current == State.Question && wagerChoice < 0 && question.wagerGroup.gameObject.activeSelf;
        public bool WaitingForAnswer => Current == State.Question && wagerChoice >= 0 && answerChoice < 0 && question.answersGroup.gameObject.activeSelf;
        public bool WaitingForNext => waitingNext && question.next.gameObject.activeSelf;
        public QuestionDef CurrentQuestion => currentQ;
        public bool Busy => busy;

        // ------------------------------------------------------------------ boot

        void Start()
        {
            Time.timeScale = 1f;
            Application.runInBackground = true;
            DOTween.SetTweensCapacity(800, 200);
            SaveSystem.Load();
            audioManager.Init(config);
            fx.Init(fxRoot, config);
            Wire();

            foreach (var s in new UIScreen[] { hud, title, howTo, board, question, results, progress, pause })
                s.Hide(true);
            edovuCorner.gameObject.SetActive(false);
            UpdateSoundIcons();
            Current = State.Splash;
            fader.image.color = Palette.WithA(Palette.StudioTop, 0f);
            splash.Play(() =>
            {
                edovuCorner.gameObject.SetActive(true);
                UIScreen.PopIn(edovuCorner, 0.1f);
                GoTitle(false);
            });
        }

        void Wire()
        {
            title.play.onClick.AddListener(StartGame);
            title.progress.onClick.AddListener(() => { title.bubble.Hide(); progress.Show(); });
            title.sound.onClick.AddListener(ToggleSound);
            howTo.go.onClick.AddListener(CloseHowTo);
            board.help.onClick.AddListener(() => { startAfterHowTo = false; board.bubble.Hide(); howTo.Show(); howTo.PlayIntro(); Speak("howto"); });
            hud.pause.onClick.AddListener(Pause);
            pause.resume.onClick.AddListener(Resume);
            pause.restart.onClick.AddListener(() => { Resume(); StopFlow(); BeginRun(); });
            pause.home.onClick.AddListener(() => { Resume(); StopFlow(); GoTitle(true); });
            pause.sound.onClick.AddListener(ToggleSound);
            question.safe.onClick.AddListener(() => OnWager(0));
            question.risk.onClick.AddListener(() => OnWager(1));
            question.hint.onClick.AddListener(OnHint);
            question.replay.onClick.AddListener(() => { if (currentQ != null) Speak(currentQ.QuestionVoice, false); });
            question.next.onClick.AddListener(() => { if (waitingNext) { waitingNext = false; question.next.interactable = false; } });
            for (int i = 0; i < QuestionScreen.AnswerCount; i++)
            {
                int idx = i;
                question.answers[i].onClick.AddListener(() => OnAnswer(idx));
            }
            results.again.onClick.AddListener(StartGame);
            results.progress.onClick.AddListener(() => { results.bubble.Hide(); progress.Show(); });
            results.home.onClick.AddListener(() => GoTitle(true));
            progress.close.onClick.AddListener(() => progress.Hide());
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (paused) Resume();
                else if (Current == State.Board || Current == State.Question) Pause();
            }
        }

        // ------------------------------------------------------------------ speech

        float Speak(string key, bool showBubble = true)
        {
            float len = GameAudio.I != null ? GameAudio.I.Speak(key) : 0f;
            if (showBubble && bubble != null)
            {
                string text = VoiceText.Get(key);
                if (!string.IsNullOrEmpty(text)) bubble.Say(text, Mathf.Max(len, 1.5f));
            }
            return len;
        }

        IEnumerator WaitVoice(float max)
        {
            float t = 0f;
            while (GameAudio.I != null && GameAudio.I.IsVoicePlaying && t < max)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        // ------------------------------------------------------------------ title

        void GoTitle(bool fade)
        {
            StartCoroutine(GoTitleRoutine(fade));
        }

        IEnumerator GoTitleRoutine(bool fade)
        {
            busy = true;
            if (fade) yield return fader.FadeOut().WaitForCompletion();
            GameAudio.I?.StopVoice();
            foreach (var s in new UIScreen[] { hud, howTo, board, question, results, progress, pause }) s.Hide(true);
            question.Clear();
            Current = State.Title;
            bubble = title.bubble;
            GameAudio.I?.PlayMusic(Music.Main);
            studio.SetIntensity(1f);
            title.best.text = "Best: " + SaveSystem.Data.bestScore;
            title.Show(true);
            title.PlayIntro();
            if (fade) fader.FadeIn();
            yield return new WaitForSecondsRealtime(1.1f);
            title.owl.Wave();
            GameAudio.I?.Play(Sfx.Hoot, 1f, 0.7f);
            Speak(SaveSystem.Data.gamesPlayed > 0 ? "welcome_back" : "welcome");
            busy = false;
        }

        void ToggleSound()
        {
            GameAudio.I.SoundOn = !GameAudio.I.SoundOn;
            UpdateSoundIcons();
        }

        void UpdateSoundIcons()
        {
            bool on = SaveSystem.Data == null || SaveSystem.Data.soundOn;
            var s = config.S(on ? "sound_on" : "sound_off");
            title.soundIcon.sprite = s;
            pause.soundIcon.sprite = s;
        }

        // ------------------------------------------------------------------ game start

        public void StartGame()
        {
            if (busy) return;
            if (!SaveSystem.Data.seenHowTo && Current == State.Title)
            {
                startAfterHowTo = true;
                title.bubble.Hide();
                howTo.Show();
                howTo.PlayIntro();
                GameAudio.I?.StopVoice();
                Speak("howto");
                return;
            }
            BeginRun();
        }

        void CloseHowTo()
        {
            howTo.Hide();
            SaveSystem.Data.seenHowTo = true;
            SaveSystem.Save();
            if (startAfterHowTo)
            {
                startAfterHowTo = false;
                BeginRun();
            }
            else GameAudio.I?.StopVoice();
        }

        void BeginRun()
        {
            StopFlow();
            run = new Run { startTime = Time.realtimeSinceStartup };
            var order = new List<int>();
            for (int i = 0; i < QuestionBank.All.Count; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            var points = new int[10];
            var skills = new int[10];
            for (int i = 0; i < 10; i++)
            {
                run.tileQuestion[i] = order[i];
                points[i] = QuestionBank.All[order[i]].points;
                skills[i] = QuestionBank.All[order[i]].skill;
            }
            flow = StartCoroutine(BeginRunRoutine(points, skills));
        }

        IEnumerator BeginRunRoutine(int[] points, int[] skills)
        {
            busy = true;
            yield return fader.FadeOut().WaitForCompletion();
            GameAudio.I?.StopVoice();
            foreach (var s in new UIScreen[] { title, howTo, question, results, progress, pause }) s.Hide(true);
            title.StopDecor();
            question.Clear();
            Current = State.Board;
            bubble = board.bubble;
            board.BuildTiles(points, skills, OnPickTile);
            hud.Show(true);
            hud.coins.Set(0);
            hud.SetStreak(0, false);
            hud.SetDots(run.tileState, -1);
            board.Show(true);
            GameAudio.I?.PlayMusic(Music.Main);
            studio.SetIntensity(1f);
            fader.FadeIn();
            board.PlayIntro(true);
            board.owl.Enter(0.1f);
            board.SetInteractable(true);
            yield return new WaitForSeconds(0.8f);
            board.owl.Point(1);
            Speak("pick_tile");
            busy = false;
        }

        void StopFlow()
        {
            if (flow != null) StopCoroutine(flow);
            flow = null;
            waitingNext = false;
            busy = false;
            GameAudio.I?.StopVoice();
        }

        // ------------------------------------------------------------------ board -> question

        public void OnPickTile(int tile)
        {
            if (busy || paused || Current != State.Board || run == null || run.tileState[tile] != 0) return;
            busy = true;
            currentTile = tile;
            board.SetInteractable(false);
            var t = board.tiles[tile];
            t.StopBob();
            GameAudio.I?.Play(Sfx.TileFlip);
            GameAudio.I?.Play(Sfx.Whoosh, 1f, 0.6f);
            UIFx.Ring(t.transform.position, Palette.Gold, 420f);
            UIFx.Burst(t.transform.position, Palette.Gold, 12, 180f);
            t.transform.DOScale(1.25f, 0.3f).SetEase(Ease.OutBack);
            flow = StartCoroutine(QuestionFlow(tile));
        }

        IEnumerator QuestionFlow(int tile)
        {
            var q = QuestionBank.All[run.tileQuestion[tile]];
            currentQ = q;
            int points = q.points;

            // numbers stay in order (easier to scan); worded options are shuffled so the answer has no fixed spot
            int n = q.options.Length;
            var order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            if (!int.TryParse(q.options[0], out _))
                for (int i = n - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (order[i], order[j]) = (order[j], order[i]); }
            var shown = new string[n];
            int correctIdx = 0;
            for (int i = 0; i < n; i++)
            {
                shown[i] = q.options[order[i]];
                if (order[i] == q.answer) correctIdx = i;
            }

            yield return new WaitForSeconds(0.3f);
            yield return fader.FadeOut(0.22f).WaitForCompletion();
            board.tiles[tile].transform.localScale = Vector3.one;
            board.Hide(true);
            Current = State.Question;
            bubble = question.bubble;
            question.Setup(q, shown, tile + 1, points);
            question.Show(true);
            hud.SetDots(run.tileState, tile);
            fader.FadeIn(0.25f);
            GameAudio.I?.PlayMusic(Music.Think);
            studio.SetIntensity(0.55f);
            float introT = question.PlayIntro();
            GameAudio.I?.Play(Sfx.Slide);
            busy = false;
            yield return new WaitForSeconds(0.55f);
            Speak(q.QuestionVoice, false);
            yield return new WaitForSeconds(Mathf.Max(introT - 0.55f, 0.7f));

            // --- SAFE or RISK IT
            wagerChoice = -1;
            hintUsed = false;
            question.ShowWager(run.score >= points, points, run.score >= HintCost);
            question.owl.Think();
            float waitStart = Time.time;
            bool prompted = false;
            while (wagerChoice < 0)
            {
                if (!prompted && run.answered < 2 && Time.time - waitStart > 0.4f && !GameAudio.I.IsVoicePlaying)
                {
                    prompted = true;
                    Speak("choose_wager");
                }
                yield return null;
            }
            bool risk = wagerChoice == 1;
            question.ChooseWager(risk);
            if (risk)
            {
                run.risksTaken++;
                GameAudio.I?.Play(Sfx.Risk);
                studio.SetIntensity(1.4f);
                question.owl.Hop(50f);
                UIFx.Burst(question.risk.transform.position, Palette.Risk, 14, 200f);
            }
            else
            {
                GameAudio.I?.Play(Sfx.Safe);
                UIFx.Burst(question.safe.transform.position, Palette.Safe, 10, 150f);
            }
            if (!GameAudio.I.IsVoicePlaying || GameAudio.I.CurrentLine == "choose_wager") Speak(risk ? "risk" : "safe");
            yield return new WaitForSeconds(0.35f);

            // --- answer against the clock
            answerChoice = -1;
            question.ShowAnswers();
            question.ShowTimer(QuestionSeconds);
            timeLeft = QuestionSeconds;
            int lastTick = -1;
            while (answerChoice < 0)
            {
                timeLeft -= Time.deltaTime;
                question.SetTimer(timeLeft, QuestionSeconds);
                int secs = Mathf.CeilToInt(timeLeft);
                if (secs <= 5 && secs >= 1 && secs != lastTick)
                {
                    lastTick = secs;
                    GameAudio.I?.Play(Sfx.Tick, 1.3f, 0.8f);
                }
                if (timeLeft <= 0f)
                {
                    answerChoice = TimeOut;
                    break;
                }
                yield return null;
            }
            question.StopTimer(false);
            question.LockAnswers();
            if (question.hint.gameObject.activeSelf) question.hint.transform.DOScale(0f, 0.2f).OnComplete(() => question.hint.gameObject.SetActive(false));
            bool timedOut = answerChoice == TimeOut;
            int chosen = timedOut ? -1 : answerChoice;
            bool correct = !timedOut && chosen == correctIdx;
            bool fast = correct && QuestionSeconds - timeLeft <= FastSeconds;
            var chosenBtn = timedOut ? question.answers[correctIdx].transform : question.answers[chosen].transform;
            if (risk && !timedOut)
            {
                GameAudio.I?.StopVoice();
                GameAudio.I?.Play(Sfx.Drumroll, 1f, 0.8f);
                chosenBtn.DOScale(1.08f, 0.16f).SetLoops(6, LoopType.Yoyo);
                yield return new WaitForSeconds(1.05f);
                chosenBtn.localScale = Vector3.one;
            }
            else yield return new WaitForSeconds(0.2f);

            int delta;
            run.answered++;
            if (correct)
            {
                delta = risk ? points * 2 : points;
                run.correct++;
                run.streak++;
                run.bestStreak = Mathf.Max(run.bestStreak, run.streak);
                if (risk) run.risksWon++;
                question.MarkAnswer(chosen, true);
                GameAudio.I?.Play(Sfx.Correct);
                GameAudio.I?.Play(Sfx.Cheer, 1f, 0.35f);
                UIFx.Ring(chosenBtn.position, Palette.Correct, 340f);
                UIFx.Burst(chosenBtn.position, Palette.Correct, 14, 200f);
                UIFx.FloatText(chosenBtn.position, "+" + delta, Palette.Gold, 84f, 90f);
                int coinCount = Mathf.Clamp(delta / 50, 4, 14);
                run.score += delta;
                UIFx.CoinFly(chosenBtn.position, hud.CoinTarget, coinCount,
                    () => GameAudio.I?.Play(Sfx.Coin, Random.Range(1.0f, 1.25f), 0.35f),
                    () => { if (run != null) hud.coins.AnimateTo(run.score, 0.5f); });
                question.owl.Happy();
                studio.SetIntensity(1.5f);
                Speak(risk ? "risk_win" : fast ? "fast" : "correct_" + Random.Range(1, 6));
                yield return new WaitForSeconds(0.9f);
                if (fast)
                {
                    run.score += SpeedBonus;
                    delta += SpeedBonus;
                    run.fastAnswers++;
                    GameAudio.I?.Play(Sfx.Sparkle, 1.2f);
                    UIFx.FloatText(question.timer.position, "FAST +" + SpeedBonus, Palette.Blue, 56f, -110f);
                    DOVirtual.DelayedCall(0.4f, () => { if (run != null) hud.coins.AnimateTo(run.score, 0.4f); }).SetLink(gameObject);
                }
                hud.SetStreak(run.streak, true);
                if (run.streak >= 3)
                {
                    run.score += StreakBonus;
                    delta += StreakBonus;
                    GameAudio.I?.Play(Sfx.Streak);
                    UIFx.FloatText(hud.streakChip.position, "STREAK +" + StreakBonus, Palette.Risk, 56f, -130f);
                    UIFx.Burst(hud.streakChip.position, Palette.Risk, 12, 140f);
                    DOVirtual.DelayedCall(0.5f, () => { if (run != null) hud.coins.AnimateTo(run.score, 0.4f); }).SetLink(gameObject);
                    if (run.streak == 3 || run.streak == 5)
                    {
                        yield return WaitVoice(3f);
                        Speak("streak_" + run.streak);
                    }
                }
            }
            else
            {
                delta = risk ? -Mathf.Min(points, run.score) : 0;
                run.streak = 0;
                hud.SetStreak(0, false);
                if (!timedOut) question.MarkAnswer(chosen, false);
                GameAudio.I?.Play(Sfx.Wrong);
                GameAudio.I?.Play(Sfx.Aww, 1f, 0.3f);
                question.owl.Sad();
                studio.SetIntensity(0.45f);
                if (risk && delta < 0)
                {
                    run.score += delta;
                    hud.coins.AnimateTo(run.score, 0.7f);
                    UIFx.CoinDrop(hud.CoinTarget, Mathf.Clamp(-delta / 50, 3, 10));
                    UIFx.FloatText(timedOut ? question.timer.position : chosenBtn.position, delta.ToString(), Palette.Wrong, 80f, -120f);
                    GameAudio.I?.Play(Sfx.CoinLose);
                }
                Speak(timedOut ? "times_up" : risk && delta < 0 ? "risk_lose" : "wrong_" + Random.Range(1, 4));
                yield return new WaitForSeconds(0.6f);
                question.MarkAnswer(correctIdx, true);
                GameAudio.I?.Play(Sfx.Sparkle);
            }
            question.DimOthers(chosen, correctIdx);

            run.tileState[tile] = correct ? 1 : 2;
            run.tileDelta[tile] = delta;
            run.questionRight[q.id] = correct;
            SaveSystem.RecordAnswer(q.skill, correct);
            var res = new WebBridge.QuestionResult
            {
                question = q.id, tile = tile + 1, chosen = timedOut ? "(time up)" : shown[chosen], answer = q.AnswerText, delta = delta,
                correct = correct, risk = risk, hint = hintUsed, seconds = Mathf.RoundToInt(QuestionSeconds - Mathf.Max(0f, timeLeft)), fast = fast
            };
            run.results.Add(res);
            WebBridge.ReportAnswer(res, run.score);
            hud.SetDots(run.tileState, tile);
            hud.PunchDot(tile);

            // --- explanation on the diagram
            yield return WaitVoice(3.5f);
            question.diagram.ClearMarks();
            waitingNext = true;
            if (correct) DOVirtual.DelayedCall(0.5f, () => { if (waitingNext) question.ShowNext(); }).SetLink(gameObject);
            else
            {
                Speak(q.ExplainVoice, false);
                question.owl.Point(-1);
            }
            // the explanation runs on its own so NEXT can cut it short at any moment
            explainDone = false;
            var explain = StartCoroutine(Explain(q));
            while (waitingNext)
            {
                if (explainDone && !question.next.gameObject.activeSelf) question.ShowNext();
                yield return null;
            }
            StopCoroutine(explain);

            // --- back to the board
            busy = true;
            GameAudio.I?.StopVoice();
            question.bubble.Hide();
            yield return fader.FadeOut(0.22f).WaitForCompletion();
            question.Clear();
            question.Hide(true);
            Current = State.Board;
            bubble = board.bubble;
            board.Show(true);
            hud.SetDots(run.tileState, -1);
            GameAudio.I?.PlayMusic(Music.Main);
            studio.SetIntensity(1f);
            fader.FadeIn(0.25f);
            board.PlayIntro(false);
            yield return new WaitForSeconds(0.35f);
            board.tiles[tile].SetResult(correct, delta, false);
            yield return new WaitForSeconds(0.5f);

            if (run.answered >= 10)
            {
                yield return new WaitForSeconds(0.6f);
                yield return FinishGame();
                yield break;
            }
            board.SetInteractable(true);
            busy = false;
            int left = 10 - run.answered;
            if (left == 1) { board.owl.Hop(); Speak("last_tile"); }
            else if (Random.value < 0.5f) { board.owl.Point(1); Speak("pick_tile2"); }
        }

        bool explainDone;

        IEnumerator Explain(QuestionDef q)
        {
            foreach (var step in q.explain)
                yield return question.diagram.Play(step);
            explainDone = true;
        }

        // ------------------------------------------------------------------ question input

        public void OnWager(int choice)
        {
            if (paused || Current != State.Question || wagerChoice >= 0 || currentQ == null) return;
            if (choice == 1 && run.score < currentQ.points)
            {
                GameAudio.I?.Play(Sfx.Wrong, 1.3f, 0.5f);
                ((RectTransform)question.risk.transform).DOShakeAnchorPos(0.4f, new Vector2(16, 0), 16, 0f);
                Speak("risk_locked");
                return;
            }
            wagerChoice = choice;
        }

        public void OnAnswer(int i)
        {
            if (paused || Current != State.Question || wagerChoice < 0 || answerChoice >= 0) return;
            answerChoice = i;
        }

        public void OnHint()
        {
            if (paused || hintUsed || Current != State.Question || answerChoice >= 0 || run.score < HintCost) return;
            hintUsed = true;
            run.hints++;
            run.score -= HintCost;
            hud.coins.AnimateTo(run.score, 0.5f);
            UIFx.CoinDrop(hud.CoinTarget, 3);
            UIFx.FloatText(question.hint.transform.position, "-" + HintCost, Palette.Wrong, 64f, 90f);
            GameAudio.I?.Play(Sfx.Hint);
            question.hint.transform.DOScale(0f, 0.2f).OnComplete(() => question.hint.gameObject.SetActive(false));
            question.diagram.ShowHint(currentQ.hintRegions);
            question.owl.Think();
            Speak(currentQ.HintVoice);
        }

        // ------------------------------------------------------------------ results

        IEnumerator FinishGame()
        {
            busy = true;
            int stars = run.correct >= 9 ? 3 : run.correct >= 6 ? 2 : 1;
            var rec = new GameRecord
            {
                score = run.score, correct = run.correct, stars = stars, seconds = Mathf.RoundToInt(Time.realtimeSinceStartup - run.startTime),
                risksTaken = run.risksTaken, risksWon = run.risksWon, hints = run.hints, bestStreak = run.bestStreak
            };
            var earned = new List<string>();
            var r = run.questionRight;
            if (r[1] && r[5] && r[6]) earned.Add("pictogram_pro");
            if (r[3] && r[7]) earned.Add("venn_wizard");
            if (r[2] && r[8] && r[9] && r[10]) earned.Add("carroll_champ");
            if (run.risksWon >= 3) earned.Add("daredevil");
            if (run.bestStreak >= 5) earned.Add("hot_streak");
            if (run.correct == 10) earned.Add("perfect_ten");
            var fresh = SaveSystem.RecordGame(rec, earned, out bool newBest);
            WebBridge.ReportGame(rec, newBest, fresh, run.results);

            yield return fader.FadeOut().WaitForCompletion();
            board.Hide(true);
            hud.Hide(true);
            Current = State.Results;
            bubble = results.bubble;
            results.Show(true);
            studio.SetIntensity(1.5f);
            GameAudio.I?.PlayMusic(Music.Main);
            fader.FadeIn();
            busy = false;
            yield return results.Play(rec, newBest, fresh, k => Speak(k));
        }

        // ------------------------------------------------------------------ pause

        public void Pause()
        {
            if (paused || busy && Current != State.Question) return;
            if (Current != State.Board && Current != State.Question) return;
            paused = true;
            Time.timeScale = 0f;
            if (bubble != null) bubble.Hide();
            AudioListener.pause = true;
            pause.Show();
            UIScreen.PopIn(pause.panel, 0f, 0.35f);
        }

        public void Resume()
        {
            if (!paused) return;
            paused = false;
            pause.Hide();
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        void OnApplicationPause(bool p)
        {
            if (p) SaveSystem.Save();
        }
    }
}
