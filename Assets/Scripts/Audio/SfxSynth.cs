using System;
using UnityEngine;

namespace GodTower.Audio
{
    /// <summary>
    /// Generates small placeholder sound effects at startup so the prototype has complete audio
    /// feedback without imported assets. Any clip can be overridden on the <see cref="AudioHandler"/>.
    /// </summary>
    public static class SfxSynth
    {
        const int SampleRate = 22050;

        public static AudioClip Create(SfxId id)
        {
            switch (id)
            {
                case SfxId.Punch: return Build("Punch", 0.22f, Punch);
                case SfxId.Impact: return Build("Impact", 0.35f, Impact);
                case SfxId.Whoosh: return Build("Whoosh", 0.4f, Whoosh());
                case SfxId.Boost: return Build("Boost", 0.9f, Boost);
                case SfxId.Explosion: return Build("Explosion", 1.3f, Explosion());
                case SfxId.Win: return Build("Win", 1.1f, Win);
                case SfxId.Lose: return Build("Lose", 1.2f, Lose);
                case SfxId.Click: return Build("Click", 0.06f, (t, r) => Mathf.Sin(2f * Mathf.PI * 1400f * t) * Decay(t, 0.06f, 6f) * 0.5f);
                case SfxId.Grab: return Build("Grab", 0.12f, (t, r) => (Mathf.Sin(2f * Mathf.PI * 140f * t) * 0.8f + Noise(r) * 0.3f) * Decay(t, 0.12f, 5f));
                case SfxId.Warning: return Build("Warning", 0.34f, Warning);
                case SfxId.ClimbStep: return Build("ClimbStep", 0.07f, (t, r) => Noise(r) * Decay(t, 0.07f, 5f) * 0.35f);
                default: throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        static AudioClip Build(string name, float seconds, Func<float, System.Random, float> sample)
        {
            int count = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[count];
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < count; i++)
            {
                data[i] = Mathf.Clamp(sample(i / (float)SampleRate, rng), -1f, 1f);
            }

            // 5 ms fade-out avoids clicks at the end of every clip.
            int fade = Mathf.Min(count, SampleRate / 200);
            for (int i = 0; i < fade; i++) data[count - 1 - i] *= i / (float)fade;

            AudioClip clip = AudioClip.Create($"sfx_{name}", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Noise(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);

        static float Decay(float t, float length, float sharpness) => Mathf.Exp(-sharpness * t / length);

        static float Punch(float t, System.Random rng)
        {
            float thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(160f, 60f, t / 0.22f) * t) * Decay(t, 0.22f, 4f);
            float slap = Noise(rng) * Decay(t, 0.05f, 4f);
            return thump * 0.9f + slap * 0.6f;
        }

        static float Impact(float t, System.Random rng)
        {
            float ring = Mathf.Sin(2f * Mathf.PI * 520f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 1340f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * 2210f * t);
            return ring * 0.3f * Decay(t, 0.35f, 5f) + Noise(rng) * 0.5f * Decay(t, 0.04f, 4f);
        }

        // Filtered-noise generators keep their filter state in the closure.
        static Func<float, System.Random, float> Whoosh()
        {
            float low = 0f;
            return (t, rng) =>
            {
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.4f));
                low += (Noise(rng) - low) * Mathf.Lerp(0.05f, 0.35f, envelope);
                return low * envelope * 1.4f;
            };
        }

        static float Boost(float t, System.Random rng)
        {
            float frequency = Mathf.Lerp(180f, 900f, t / 0.9f);
            float tone = Mathf.Sin(2f * Mathf.PI * frequency * t) + 0.35f * Mathf.Sin(4f * Mathf.PI * frequency * t);
            float envelope = Mathf.Clamp01(t / 0.08f) * Decay(t, 0.9f, 2f);
            return (tone * 0.4f + Noise(rng) * 0.15f) * envelope;
        }

        static Func<float, System.Random, float> Explosion()
        {
            float low = 0f;
            return (t, rng) =>
            {
                low += (Noise(rng) - low) * 0.08f;
                float rumble = Mathf.Sin(2f * Mathf.PI * 48f * t) * 0.5f;
                return (low * 2.5f + rumble) * Decay(t, 1.3f, 4f);
            };
        }

        static float Win(float t, System.Random rng)
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            int index = Mathf.Min(notes.Length - 1, (int)(t / 0.16f));
            float local = t - index * 0.16f;
            float length = index == notes.Length - 1 ? 0.6f : 0.16f;
            return Square(notes[index], t) * 0.25f * Decay(local, length, 3f);
        }

        static float Lose(float t, System.Random rng)
        {
            float[] notes = { 392f, 329.63f, 261.63f, 196f };
            int index = Mathf.Min(notes.Length - 1, (int)(t / 0.22f));
            float local = t - index * 0.22f;
            float vibrato = 1f + 0.01f * Mathf.Sin(2f * Mathf.PI * 6f * t);
            float length = index == notes.Length - 1 ? 0.55f : 0.22f;
            return Square(notes[index] * vibrato, t) * 0.22f * Decay(local, length, 2.5f);
        }

        static float Warning(float t, System.Random rng)
        {
            bool on = t < 0.12f || (t > 0.18f && t < 0.3f);
            return on ? Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.35f : 0f;
        }

        static float Square(float frequency, float t)
        {
            float s = Mathf.Sin(2f * Mathf.PI * frequency * t);
            return Mathf.Clamp(s * 3f, -1f, 1f) * 0.6f + s * 0.4f;
        }
    }
}
