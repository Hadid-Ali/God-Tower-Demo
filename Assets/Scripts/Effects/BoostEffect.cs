using GodTower.Audio;
using GodTower.Player;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>
    /// Temporary upward boost (Jetpack, Phoenix). Carries the climber upward for a fixed duration with
    /// a visible start and end; re-triggering refreshes the timer up to a cap and never stacks speed.
    /// With a carrier (e.g. the phoenix) the boost is staged: the carrier appears off to the side, the camera
    /// zooms out and follows it in to the tower, it settles beneath the climber, and only then does the boost
    /// start. It holds the climber while the boost lasts, then flies off and hides.
    /// </summary>
    public sealed class BoostEffect : EffectBase
    {
        [SerializeField] EffectType type = EffectType.Jetpack;
        [SerializeField] string label = "JETPACK";
        [SerializeField, Tooltip("Seconds added per trigger.")] float duration = 2.5f;
        [SerializeField, Tooltip("Ascent speed while boosting, m/s.")] float speed = 6f;
        [SerializeField, Tooltip("Longest the boost can run after repeated triggers.")] float maxDuration = 5f;
        [SerializeField] float zoomDistance = 14f;

        [Header("Visuals (parented to the climber's back at runtime)")]
        [SerializeField] ParticleSystem[] trails;

        [Header("Carrier (optional)")]
        [SerializeField, Tooltip("Object that carries the climber during the boost, e.g. the Phoenix. Hidden when idle.")] Transform carrier;
        [SerializeField, Tooltip("Carrier position relative to the climber while carrying (beneath it), in world meters.")] Vector3 carryOffset = new Vector3(0f, -0.65f, 0f);
        [SerializeField, Tooltip("Where the carrier appears relative to the climber before flying in, in world meters.")] Vector3 approachOffset = new Vector3(-12f, -5f, -3f);
        [SerializeField, Tooltip("Seconds to fly from the approach point to beneath the climber.")] float approachTime = 1.6f;
        [SerializeField, Tooltip("Direction and distance it flies off to when the boost ends, in meters.")] Vector3 departOffset = new Vector3(4f, 8f, 0f);
        [SerializeField] float departTime = 0.8f;

        [Header("Camera during the approach")]
        [SerializeField, Tooltip("Zoom out and follow the carrier while it flies in, then return to the climber.")] bool followCarrierWithCamera = true;
        [SerializeField, Tooltip("Camera distance while following the approach.")] float approachZoomDistance = 18f;

        CarrierPhase _phase;
        float _phaseTime;
        Vector3 _phaseStart;

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
            HideCarrier();
        }

        public override void Play()
        {
            if (Context == null) return;
            if (carrier == null)
            {
                StartBoost();
                return;
            }

            switch (_phase)
            {
                case CarrierPhase.Hidden:
                case CarrierPhase.Departing:
                    BeginApproach();
                    break;
                case CarrierPhase.Carrying:
                    StartBoost(); // Extends the running boost.
                    break;
                case CarrierPhase.Arriving:
                    break; // The boost starts when it arrives.
            }
        }

        void StartBoost()
        {
            Context.Climber.ApplyBoost(label, duration, speed, maxDuration);
            if (!Context.Climber.IsBoosting) return;

            Context.Rig.RequestZoom(zoomDistance, duration);
            Context.Rig.Shake(0.15f, 0.4f, 10);
            AudioHandler.TryPlay(SfxId.Boost);
            foreach (ParticleSystem trail in trails)
            {
                if (trail != null && !trail.isPlaying) trail.Play(true);
            }
        }

        void Update()
        {
            if (Context == null) return;
            bool active = Context.Climber.IsBoosting && Context.Climber.BoostLabel == label;
            if (!active && _phase == CarrierPhase.Carrying) SetPhase(CarrierPhase.Departing);
            if (active) return;

            foreach (ParticleSystem trail in trails)
            {
                if (trail != null && trail.isEmitting) trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // After the climber has moved this frame, so the carrier stays glued to it.
        void LateUpdate()
        {
            if (carrier == null || Context == null || _phase == CarrierPhase.Hidden) return;

            Vector3 hold = Context.Climber.transform.position + carryOffset;
            _phaseTime += Time.deltaTime;
            switch (_phase)
            {
                case CarrierPhase.Arriving:
                    TickApproach(hold);
                    break;
                case CarrierPhase.Carrying:
                    carrier.position = hold;
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
                // Nothing left to carry: turn away instead of boosting.
                ReleaseCamera();
                SetPhase(CarrierPhase.Departing);
                return;
            }

            // Eases in and out, aiming at the climber's live position so it always ends up beneath them.
            float t = approachTime > 0f ? Mathf.Clamp01(_phaseTime / approachTime) : 1f;
            carrier.position = Vector3.LerpUnclamped(_phaseStart, hold, Mathf.SmoothStep(0f, 1f, t));
            if (t < 1f) return;

            // The push starts now, so the camera is back on the climber from this frame.
            ReleaseCamera(immediate: true);
            SetPhase(CarrierPhase.Carrying);
            StartBoost();
            if (!Context.Climber.IsBoosting) SetPhase(CarrierPhase.Departing);
        }

        void BeginApproach()
        {
            // A departing carrier swoops back from where it is; otherwise it appears at the approach point.
            if (_phase == CarrierPhase.Hidden)
                carrier.position = Context.Climber.transform.position + approachOffset;
            carrier.gameObject.SetActive(true);
            SetPhase(CarrierPhase.Arriving);
            // Frame the spot the climber will occupy (not the bird itself), so arrival hands back without a jump.
            if (followCarrierWithCamera) Context.Rig.FocusOn(carrier, approachZoomDistance, -carryOffset);
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

        public override void Clear()
        {
            foreach (ParticleSystem trail in trails)
            {
                if (trail != null) trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            HideCarrier();
        }

        [ContextMenu("Trigger Now (Play Mode)")]
        void TriggerNow()
        {
            if (Application.isPlaying) Play();
            else Debug.LogWarning("[BoostEffect] Enter Play Mode to trigger the boost.", this);
        }

        [ContextMenu("Use Carrier's Scene Position As Carry Offset")]
        void CaptureCarryOffset()
        {
            var climber = FindAnyObjectByType<ClimberController>();
            if (carrier != null && climber != null) carryOffset = carrier.position - climber.transform.position;
        }

        [ContextMenu("Use Carrier's Scene Position As Approach Offset")]
        void CaptureApproachOffset()
        {
            var climber = FindAnyObjectByType<ClimberController>();
            if (carrier != null && climber != null) approachOffset = carrier.position - climber.transform.position;
        }
    }
}
