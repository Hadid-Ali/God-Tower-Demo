using GodTower.Audio;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>
    /// Temporary upward boost (Jetpack, Phoenix). Carries the climber upward for a fixed duration with
    /// a visible start and end; re-triggering refreshes the timer up to a cap and never stacks speed.
    /// </summary>
    public sealed class BoostEffect : EffectBase
    {
        [SerializeField] EffectType type = EffectType.Jetpack;
        [SerializeField] string label = "JETPACK";
        [SerializeField, Tooltip("Seconds added per trigger.")] float duration = 2.5f;
        [SerializeField, Tooltip("Ascent speed while boosting, m/s.")] float speed = 6f;
        [SerializeField, Tooltip("Longest the boost can run after repeated triggers.")] float maxDuration = 5f;
        [SerializeField] float zoomDistance = 14f;
        [SerializeField] Color flashColor = new Color(1f, 0.75f, 0.3f);

        [Header("Visuals (parented to the climber's back at runtime)")]
        [SerializeField] ParticleSystem[] trails;

        public override EffectType Type => type;

        public override void Initialize(EffectContext context)
        {
            base.Initialize(context);
            foreach (ParticleSystem trail in trails)
            {
                if (trail == null) continue;
                trail.transform.SetParent(context.View != null ? context.View.BackAnchor : context.Climber.transform, false);
                trail.transform.localPosition = Vector3.zero;
                trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public override void Play()
        {
            if (Context == null) return;
            Context.Climber.ApplyBoost(label, duration, speed, maxDuration);
            if (!Context.Climber.IsBoosting) return;

            Context.Rig.RequestZoom(zoomDistance, duration);
            Context.Rig.Shake(0.15f, 0.4f, 10);
            Context.Screen?.Flash(flashColor, 0.3f, 0.3f);
            AudioService.TryPlay(SfxId.Boost);
            foreach (ParticleSystem trail in trails)
            {
                if (trail != null && !trail.isPlaying) trail.Play(true);
            }
        }

        void Update()
        {
            if (Context == null) return;
            bool active = Context.Climber.IsBoosting && Context.Climber.BoostLabel == label;
            if (active) return;

            foreach (ParticleSystem trail in trails)
            {
                if (trail != null && trail.isEmitting) trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        public override void Clear()
        {
            foreach (ParticleSystem trail in trails)
            {
                if (trail != null) trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
