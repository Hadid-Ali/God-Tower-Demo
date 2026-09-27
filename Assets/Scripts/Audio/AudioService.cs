using System;
using System.Collections.Generic;
using GodTower.Core;
using UnityEngine;

namespace GodTower.Audio
{
    /// <summary>Plays one-shot sound effects from a small pool of sources; applies saved volume settings.</summary>
    public sealed class AudioService : MonoBehaviour
    {
        public const string LibraryResourcePath = "SfxLibrary";
        const int VoiceCount = 12;

        readonly Dictionary<SfxId, AudioClip> _clips = new Dictionary<SfxId, AudioClip>();
        readonly Dictionary<SfxId, float> _baseVolumes = new Dictionary<SfxId, float>();
        readonly List<UnityEngine.Object> _generated = new List<UnityEngine.Object>();
        AudioSource[] _voices;
        int _nextVoice;

        public static AudioService Instance => GameSession.Instance != null ? GameSession.Instance.Audio : null;

        void Awake()
        {
            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _voices[i] = source;
            }

            var library = Resources.Load<SfxLibrary>(LibraryResourcePath);
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                if (library != null && library.TryGet(id, out SfxLibrary.Entry entry))
                {
                    _clips[id] = entry.clip;
                    _baseVolumes[id] = entry.volume > 0f ? entry.volume : 1f;
                }
                else
                {
                    AudioClip clip = SfxSynth.Create(id);
                    _generated.Add(clip);
                    _clips[id] = clip;
                    _baseVolumes[id] = 1f;
                }
            }

            ApplyMasterVolume();
        }

        public void ApplyMasterVolume() => AudioListener.volume = SaveData.MasterVolume;

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

        /// <summary>Convenience for callers that may run before the session exists (e.g. in tests).</summary>
        public static void TryPlay(SfxId id, float volume = 1f, float pitchVariance = 0f)
        {
            AudioService service = Instance;
            if (service == null) return;
            if (pitchVariance > 0f) service.PlayVaried(id, volume, pitchVariance);
            else service.Play(id, volume);
        }

        void OnDestroy()
        {
            foreach (UnityEngine.Object clip in _generated)
            {
                if (clip != null) Destroy(clip);
            }
        }
    }
}
