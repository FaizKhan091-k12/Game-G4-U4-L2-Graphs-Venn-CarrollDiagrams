using System.Collections.Generic;
using UnityEngine;

namespace DataChampions
{
    /// <summary>
    /// SFX pool, Professor Hoot's voice lines and two music beds (studio groove + thinking music) that crossfade.
    /// Music ducks under the voice. One switch mutes everything (stored in the save).
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        const int SfxVoices = 10;
        const float MusicDuck = 0.35f;
        const float CrossfadeSeconds = 0.8f;
        const string VoicePrefix = "vo_";

        public static GameAudio I { get; private set; }

        readonly Dictionary<Sfx, GameConfig.SfxEntry> sfx = new Dictionary<Sfx, GameConfig.SfxEntry>();
        readonly Dictionary<string, AudioClip> voice = new Dictionary<string, AudioClip>();
        readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
        AudioSource[] sfxSources;
        AudioSource voiceSource;
        AudioSource[] music = new AudioSource[2];
        float[] musicLevel = new float[2];
        int activeMusic = -1;
        int nextSfx;
        float duck = 1f;
        GameConfig config;

        public bool SoundOn
        {
            get => SaveSystem.Data == null || SaveSystem.Data.soundOn;
            set
            {
                SaveSystem.Data.soundOn = value;
                SaveSystem.Save();
                AudioListener.volume = value ? 1f : 0f;
            }
        }

        public bool IsVoicePlaying => voiceSource != null && voiceSource.clip != null && voiceSource.isPlaying;
        public string CurrentLine { get; private set; }

        public event System.Action<string, float> Spoke;

        public void Init(GameConfig cfg)
        {
            I = this;
            config = cfg;
            foreach (var e in cfg.sfx) if (e.clip != null) sfx[e.id] = e;
            foreach (var c in cfg.voiceLines)
                if (c != null) voice[c.name.StartsWith(VoicePrefix) ? c.name.Substring(VoicePrefix.Length) : c.name] = c;

            sfxSources = new AudioSource[SfxVoices];
            for (int i = 0; i < SfxVoices; i++) sfxSources[i] = MakeSource("Sfx" + i, false);
            voiceSource = MakeSource("Voice", false);
            music[0] = MakeSource("MusicMain", true);
            music[1] = MakeSource("MusicThink", true);
            music[0].clip = cfg.musicMain;
            music[1].clip = cfg.musicThink;
            // music keeps playing softly in the pause menu (AudioListener.pause stops voice + effects)
            music[0].ignoreListenerPause = music[1].ignoreListenerPause = true;
            AudioListener.volume = SoundOn ? 1f : 0f;
        }

        AudioSource MakeSource(string n, bool loop)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            return s;
        }

        public void PlayMusic(Music m)
        {
            int idx = m == Music.Main ? 0 : m == Music.Think ? 1 : -1;
            if (idx == activeMusic) return;
            activeMusic = idx;
            if (idx >= 0 && music[idx].clip != null && !music[idx].isPlaying)
            {
                if (music[idx].time > 0f) music[idx].UnPause();
                else music[idx].Play();
            }
        }

        void Update()
        {
            if (config == null) return;
            float target = IsVoicePlaying ? MusicDuck : 1f;
            duck = Mathf.MoveTowards(duck, target, Time.unscaledDeltaTime * 3f);
            for (int i = 0; i < 2; i++)
            {
                if (music[i] == null) continue;
                float goal = i == activeMusic ? 1f : 0f;
                musicLevel[i] = Mathf.MoveTowards(musicLevel[i], goal, Time.unscaledDeltaTime / CrossfadeSeconds);
                music[i].volume = config.musicVolume * duck * musicLevel[i] * (i == 1 ? 0.8f : 1f);
                if (musicLevel[i] <= 0f && music[i].isPlaying && i != activeMusic) music[i].Pause();
                else if (musicLevel[i] > 0f && !music[i].isPlaying && music[i].clip != null) music[i].UnPause();
            }
        }

        public void Play(Sfx id, float pitch = 1f, float volumeScale = 1f)
        {
            if (!sfx.TryGetValue(id, out var e)) return;
            if (lastPlayed.TryGetValue(id, out float t) && Time.unscaledTime - t < 0.035f) return;
            lastPlayed[id] = Time.unscaledTime;
            var s = sfxSources[nextSfx];
            nextSfx = (nextSfx + 1) % sfxSources.Length;
            s.pitch = pitch;
            s.PlayOneShot(e.clip, e.volume * volumeScale);
        }

        /// <summary>Speaks a voice line (file vo_key). Returns its length in seconds, 0 when missing.</summary>
        public float Speak(string key)
        {
            if (string.IsNullOrEmpty(key) || !voice.TryGetValue(key, out var clip)) return 0f;
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.volume = config.voiceVolume;
            voiceSource.Play();
            CurrentLine = key;
            Spoke?.Invoke(key, clip.length);
            return clip.length;
        }

        public void StopVoice()
        {
            if (voiceSource != null) voiceSource.Stop();
            CurrentLine = null;
        }

        public float VoiceLength(string key) => voice.TryGetValue(key, out var c) ? c.length : 0f;
        public bool HasVoice(string key) => voice.ContainsKey(key);

        /// <summary>Beak opening while a line plays (0..1). Procedural, as AudioClip.GetData is unavailable on WebGL.</summary>
        public float VoiceLevel()
        {
            if (!IsVoicePlaying) return 0f;
            float t = voiceSource.time;
            float syllable = Mathf.Abs(Mathf.Sin(t * 12f)) * (0.55f + 0.45f * Mathf.PerlinNoise(t * 3f, 0.3f));
            float fadeOut = Mathf.Clamp01((voiceSource.clip.length - t) * 6f);
            return syllable * fadeOut;
        }
    }
}
