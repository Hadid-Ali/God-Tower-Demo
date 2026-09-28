using GodTower.Audio;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>
    /// Temporary upward boost (Jetpack, Phoenix). Carries the climber upward for a fixed duration with
    /// a visible start and end; re-triggering refreshes the timer up to a cap and never stacks speed.
    /// With a carrier (e.g. the phoenix) the boost is staged: the carrier appears below the climber, rises into place
    /// beneath them, and only then does the boost start; it carries the climber up at a constant speed while the
    /// boost lasts, then flies off and hides.
    /// </summary>
    public sealed class BoostEffect : CarrierEffect
    {
        [SerializeField] EffectType type = EffectType.Jetpack;
        [SerializeField] string label = "JETPACK";
        [SerializeField, Tooltip("Seconds added per trigger.")] float duration = 2.5f;
        [SerializeField, Tooltip("Ascent speed while boosting, m/s.")] float speed = 6f;
        [SerializeField, Tooltip("Longest the boost can run after repeated triggers.")] float maxDuration = 5f;
        [SerializeField] float zoomDistance = 14f;

        [Header("Visuals (parented to the climber's back at runtime)")]
        [SerializeField] ParticleSystem[] trails;

        public override EffectType Type => type;

        bool IsBoostActive => Context.Climber.IsBoosting && Context.Climber.BoostLabel == label;

        protected override bool IsHolding => IsBoostActive;

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

        protected override bool PushesUp => true;

        protected override bool OnArrived() => StartBoost();

        protected override void OnTriggeredWhileHolding() => StartBoost(); // Extends the running boost.

        bool StartBoost()
        {
            Context.Climber.ApplyBoost(label, duration, speed, maxDuration);
            if (!Context.Climber.IsBoosting) return false;

            Context.Rig.RequestZoom(zoomDistance, duration);
            Context.Rig.Shake(0.15f, 0.4f, 10);
            AudioHandler.TryPlay(SfxId.Boost);
            foreach (ParticleSystem trail in trails)
            {
                if (trail != null && !trail.isPlaying) trail.Play(true);
            }
            return true;
        }

        void Update()
        {
            if (Context == null || IsBoostActive) return;
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
            base.Clear();
        }
    }
}
