using GodTower.Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace GodTower.Effects
{
    /// <summary>
    /// An effect delivered by a flying creature (the carrier). When triggered, the carrier appears in line with the
    /// climber at <see cref="spawnOffset"/> (10 m along +Z by default) and flies to a point straight above the climber for
    /// a push down (or straight below for a push up), then drops or rises onto its hold point while the camera follows,
    /// and only then acts on the climber (<see cref="OnArrived"/>). It moves together with the climber while
    /// <see cref="IsHolding"/> (dragging them down or carrying them up), then flies off and hides.
    /// Without a carrier the effect acts immediately.
    /// </summary>
    public abstract class CarrierEffect : EffectBase
    {
        [Header("Carrier (optional)")]
        [SerializeField, Tooltip("Creature that delivers the effect, e.g. the Phoenix or Dragon. Hidden when idle.")]
        protected Transform carrier;
        [SerializeField, FormerlySerializedAs("carryOffset"), Tooltip("Carrier position relative to the climber while it acts on them, in world meters.")]
        protected Vector3 holdOffset = new Vector3(0f, -0.65f, 0f);
        [SerializeField, Tooltip("Where the carrier appears relative to the climber, in world meters (in line with the climber, 10 m along +Z).")]
        protected Vector3 spawnOffset = new Vector3(0f, 0f, 10f);
        [SerializeField, Tooltip("Seconds to fly from where it appears to the point straight above (or below) the climber.")]
        protected float approachTime = 1.6f;
        [SerializeField, Tooltip("How far above the hold point (push down) or below it (push up) it lines up before settling, in meters.")]
        protected float approachDistance = 3f;
        [SerializeField, Tooltip("Seconds to drop (or rise) straight onto the hold point from there.")]
        protected float settleTime = 0.35f;
        [SerializeField, Tooltip("Direction and distance it flies off to when done, in meters.")]
        protected Vector3 departOffset = new Vector3(4f, 8f, 0f);
        [SerializeField] protected float departTime = 0.8f;

        [Header("Camera during the approach")]
        [SerializeField, Tooltip("Zoom out and follow the carrier while it flies in, then return to the climber.")]
        protected bool followCarrierWithCamera = true;
        [SerializeField, Tooltip("Camera distance while following the approach.")]
        protected float approachZoomDistance = 18f;

        CarrierPhase _phase;
        float _phaseTime;
        Vector3 _phaseStart;

        /// <summary>True if this effect pushes the climber up (carrier comes from below), false if it pushes down (from above).</summary>
        protected abstract bool PushesUp { get; }

        /// <summary>World position where the carrier appears, in line with the climber.</summary>
        protected virtual Vector3 ApproachStart() => Context.Climber.transform.position + spawnOffset;

        /// <summary>The point straight above (push down) or below (push up) the hold point it settles from.</summary>
        Vector3 LineUpPoint(Vector3 hold) => hold + (PushesUp ? Vector3.down : Vector3.up) * approachDistance;

        /// <summary>The carrier has reached the climber (or there is no carrier): act now. Return true to stay and hold.</summary>
        protected abstract bool OnArrived();

        /// <summary>While true after arriving, the carrier stays glued to the climber; once false it flies off.</summary>
        protected abstract bool IsHolding { get; }

        /// <summary>Triggered again while already holding the climber.</summary>
        protected virtual void OnTriggeredWhileHolding() { }

        /// <summary>The carrier has just started flying in.</summary>
        protected virtual void OnApproachStarted() { }

        public override void Initialize(EffectContext context)
        {
            base.Initialize(context);
            HideCarrier();
        }

        public override void Play()
        {
            if (Context == null) return;
            if (carrier == null)
            {
                OnArrived();
                return;
            }

            switch (_phase)
            {
                case CarrierPhase.Hidden:
                case CarrierPhase.Departing:
                    BeginApproach();
                    break;
                case CarrierPhase.Carrying:
                    OnTriggeredWhileHolding();
                    break;
                case CarrierPhase.Arriving:
                case CarrierPhase.Settling:
                    break; // It acts when it arrives.
            }
        }

        // After the climber has moved this frame, so the carrier stays glued to it.
        protected virtual void LateUpdate()
        {
            if (carrier == null || Context == null || _phase == CarrierPhase.Hidden) return;

            Vector3 hold = Context.Climber.transform.position + holdOffset;
            _phaseTime += Time.deltaTime;
            switch (_phase)
            {
                case CarrierPhase.Arriving:
                case CarrierPhase.Settling:
                    TickApproach(hold);
                    break;
                case CarrierPhase.Carrying:
                    if (IsHolding) carrier.position = hold;
                    else SetPhase(CarrierPhase.Departing);
                    break;
                case CarrierPhase.Departing:
                {
                    float t = departTime > 0f ? Mathf.Clamp01(_phaseTime / departTime) : 1f;
                    carrier.position = _phaseStart + departOffset * (t * t);
                    if (t >= 1f) HideCarrier();
                    break;
                }
            }
        }

        void TickApproach(Vector3 hold)
        {
            ClimberState state = Context.Climber.State;
            if (state == ClimberState.Win || state == ClimberState.Lose)
            {
                // Nothing left to act on: turn away.
                ReleaseCamera();
                SetPhase(CarrierPhase.Departing);
                return;
            }

            // Both legs ease in and out and aim at the climber's live position, so it always lines up and lands exactly.
            if (_phase == CarrierPhase.Arriving)
            {
                // Leg 1: from the spawn point to straight above (or below) the climber.
                float t = approachTime > 0f ? Mathf.Clamp01(_phaseTime / approachTime) : 1f;
                carrier.position = Vector3.LerpUnclamped(_phaseStart, LineUpPoint(hold), Mathf.SmoothStep(0f, 1f, t));
                if (t >= 1f) SetPhase(CarrierPhase.Settling);
                return;
            }

            // Leg 2: straight down (or up) onto the hold point.
            float s = settleTime > 0f ? Mathf.Clamp01(_phaseTime / settleTime) : 1f;
            carrier.position = Vector3.LerpUnclamped(_phaseStart, hold, Mathf.SmoothStep(0f, 1f, s));
            if (s < 1f) return;

            // The effect starts now, so the camera is back on the climber from this frame.
            ReleaseCamera(immediate: true);
            SetPhase(CarrierPhase.Carrying);
            if (!OnArrived()) SetPhase(CarrierPhase.Departing);
        }

        void BeginApproach()
        {
            // A departing carrier swoops back from where it is; otherwise it appears at the approach point.
            if (_phase == CarrierPhase.Hidden) carrier.position = ApproachStart();
            carrier.gameObject.SetActive(true);
            SetPhase(CarrierPhase.Arriving);
            // Frame the spot the climber will occupy (not the creature itself), so arrival hands back without a jump.
            if (followCarrierWithCamera) Context.Rig.FocusOn(carrier, approachZoomDistance, -holdOffset);
            OnApproachStarted();
        }

        void SetPhase(CarrierPhase phase)
        {
            _phase = phase;
            _phaseTime = 0f;
            if (carrier != null) _phaseStart = carrier.position;
        }

        void ReleaseCamera(bool immediate = false)
        {
            if (Context != null && Context.Rig != null) Context.Rig.ReleaseFocus(carrier, immediate);
        }

        void HideCarrier()
        {
            ReleaseCamera();
            _phase = CarrierPhase.Hidden;
            if (carrier != null) carrier.gameObject.SetActive(false);
        }

        public override void Clear() => HideCarrier();

        [ContextMenu("Trigger Now (Play Mode)")]
        protected void TriggerNow()
        {
            if (Application.isPlaying) Play();
            else Debug.LogWarning($"[{GetType().Name}] Enter Play Mode to trigger the effect.", this);
        }

        [ContextMenu("Use Carrier's Scene Position As Hold Offset")]
        protected void CaptureHoldOffset()
        {
            var climber = FindAnyObjectByType<ClimberController>();
            if (carrier != null && climber != null) holdOffset = carrier.position - climber.transform.position;
        }
    }
}
