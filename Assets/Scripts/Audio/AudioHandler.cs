using System;
using System.Collections.Generic;
using GodTower.Core;
using UnityEngine;

namespace GodTower.Audio
{
    /// <summary>
    /// Owns all game audio: the start music followed gaplessly by the background loop, and every sound
    /// effect (one-shots from a small voice pool). Place one in the first scene; it persists across scene
    /// loads so music is not restarted by retries. Effects with no clip assigned fall back to a synthesized
    /// placeholder. Gameplay code plays sounds through <see cref="TryPlay"/>, which is safe when no handler exists.
    /// </summary>
    public sealed class AudioHandler : MonoBehaviour
    {
        [Serializable]
        public struct SfxEntry
        {
            public SfxId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        const int VoiceCount = 12;
        const double ScheduleLead = 0.1;

        [Header("Music")]
        [SerializeField, Tooltip("Plays once, then hands over to the background loop.")] AudioClip startMusic;
        [SerializeField, Tooltip("Loops forever after the start music (or right away if there is none).")] AudioClip backgroundLoop;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.6f;
        [SerializeField] bool playMusicOnStart = true;

        [Header("Sound effects")]
        [SerializeField, Tooltip("Clip per effect id. Volume 0 is treated as 1.")] SfxEntry[] soundEffects = Array.Empty<SfxEntry>();
        [SerializeField, Tooltip("Generate placeholder sounds for ids with no clip assigned.")] bool synthesizeMissing = true;

        [Header("Lifetime")]
        [SerializeField, Tooltip("Keep playing across scene loads; a handler in the next scene is then discarded.")] bool persistAcrossScenes = true;

        public static AudioHandler Instance { get; private set; }

        readonly Dictionary<SfxId, AudioClip> _clips = new Dictionary<SfxId, AudioClip>();
        readonly Dictionary<SfxId, float> _baseVolumes = new Dictionary<SfxId, float>();
        readonly List<UnityEngine.Object> _generated = new List<UnityEngine.Object>();
        AudioSource[] _voices;
        int _nextVoice;
        AudioSource _intro;
        AudioSource _loop;

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                if (_intro != null) _intro.volume = musicVolume;
                if (_loop != null) _loop.volume = musicVolume;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (persistAcrossScenes)
            {
                if (transform.parent != null) transform.SetParent(null); // DontDestroyOnLoad only works on root objects.
                DontDestroyOnLoad(gameObject);
            }

            _intro = CreateSource(loop: false);
            _loop = CreateSource(loop: true);
            MusicVolume = musicVolume;

            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++) _voices[i] = CreateSource(loop: false);

            LoadSoundEffects();
            ApplyMasterVolume();
        }

        void Start()
        {
            if (playMusicOnStart) PlayMusic();
        }

        AudioSource CreateSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = loop;
            return source;
        }

        void LoadSoundEffects()
        {
            foreach (SfxEntry entry in soundEffects)
            {
                if (entry.clip == null) continue;
                _clips[entry.id] = entry.clip;
                _baseVolumes[entry.id] = entry.volume > 0f ? entry.volume : 1f;
            }

            if (!synthesizeMissing) return;
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                if (_clips.ContainsKey(id)) continue;
                AudioClip clip = SfxSynth.Create(id);
                _generated.Add(clip);
                _clips[id] = clip;
                _baseVolumes[id] = 1f;
            }
        }

        /// <summary>(Re)starts the start music, then the background loop exactly when it ends.</summary>
        public void PlayMusic()
        {
            StopMusic();
            double start = AudioSettings.dspTime + ScheduleLead;

            if (startMusic != null)
            {
                _intro.clip = startMusic;
                _intro.PlayScheduled(start);
                start += (double)startMusic.samples / startMusic.frequency;
            }

            if (backgroundLoop != null)
            {
                _loop.clip = backgroundLoop;
                _loop.PlayScheduled(start);
            }
        }

        public void StopMusic()
        {
            if (_intro != null) _intro.Stop();
            if (_loop != null) _loop.Stop();
        }

        public void Play(SfxId id, float volume = 1f, float pitch = 1f)
        {
            if (!_clips.TryGetValue(id, out AudioClip clip) || clip == null) return;

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            voice.pitch = pitch;
            voice.PlayOneShot(clip, volume * _baseVolumes[id] * SaveData.SfxVolume);
        }

        public void PlayVaried(SfxId id, float volume = 1f, float pitchVariance = 0.12f) =>
            Play(id, volume, 1f + UnityEngine.Random.Range(-pitchVariance, pitchVariance));

        /// <summary>Plays a sound effect if a handler exists; does nothing otherwise.</summary>
        public static void TryPlay(SfxId id, float volume = 1f, float pitchVariance = 0f)
        {
            AudioHandler handler = Instance;
            if (handler == null) return;
            if (pitchVariance > 0f) handler.PlayVaried(id, volume, pitchVariance);
            else handler.Play(id, volume);
        }

        /// <summary>Applies the saved master volume to everything the listener hears.</summary>
        public static void ApplyMasterVolume() => AudioListener.volume = SaveData.MasterVolume;

        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            foreach (UnityEngine.Object clip in _generated)
            {
                if (clip != null) Destroy(clip);
            }
        }
    }
}
