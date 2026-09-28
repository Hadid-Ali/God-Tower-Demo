using GodTower.Audio;
using GodTower.Player;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>
    /// Tower attack delivered by a dragon (or any carrier, e.g. a glove particle system): it appears above the climber,
    /// drops onto their head and drags them straight down at a constant speed, then flies away.
    /// The push distance comes from the trigger (a level encounter's Push %) or Push Depth. It is a hit, so
    /// invulnerability, the start floor and the safe zone apply.
    /// </summary>
    public sealed class DragonEffect : CarrierEffect
    {
        [SerializeField, Tooltip("Which event triggers this push-down (e.g. Dragon, or GloveBurst for the webhook gloves).")] EffectType type = EffectType.Dragon;

        [Header("Push")]
        [SerializeField, Tooltip("How far it pushes the climber down, in meters, when the trigger doesn't set a distance (e.g. the webhook). " +
                                 "Level encounters set it from their Push %.")] float pushDepth = 5f;
        [SerializeField, Tooltip("Constant speed the climber slides down while pushed, in m/s. The climber has no control meanwhile.")] float pushSpeed = 4f;
        [SerializeField, Tooltip("Off: the push stops at the start point. On: past the safe zone it can knock the climber off the tower.")] bool lethal;
        [SerializeField] float slamShake = 0.6f;
        [SerializeField] ParticleBurst impactBurst;

        bool _pinning;
        float _requestedPush;

        public override EffectType Type => type;

        protected override bool IsHolding => _pinning && Context.Climber.State == ClimberState.Hit;

        // Defaults for a freshly added component: above the climber's head, leaving up and back toward the camera.
        void Reset()
        {
            holdOffset = new Vector3(0f, 2.8f, -0.4f);
            departOffset = new Vector3(0f, 10f, -6f);
            approachTime = 1.2f;
        }

        public override void Play()
        {
            _requestedPush = 0f;
            base.Play();
        }

        public override void Play(float pushMeters)
        {
            _requestedPush = pushMeters;
            base.Play();
        }

        protected override bool PushesUp => false;

        protected override void OnApproachStarted() => AudioHandler.TryPlay(SfxId.Warning, 0.9f);

        protected override bool OnArrived()
        {
            float meters = _requestedPush > 0f ? _requestedPush : pushDepth;
            float duration = Mathf.Max(0.2f, meters / Mathf.Max(0.1f, pushSpeed));
            _pinning = Context.Climber.ApplyHit(HitData.Push(meters, duration, lethal, Vector3.down));

            Context.Rig.Shake(slamShake, 0.35f, 22);
            AudioHandler.TryPlay(SfxId.Impact, 1f, 0.05f);
            if (impactBurst != null) impactBurst.Emit(Context.Climber.ChestPosition, 16);
            // Recovering from an earlier hit: the slam lands visually but does no damage.
            if (!_pinning && Context.View != null) Context.View.Jolt(Vector3.down);
            return _pinning;
        }

        public override void Clear()
        {
            _pinning = false;
            base.Clear();
        }
    }
}
