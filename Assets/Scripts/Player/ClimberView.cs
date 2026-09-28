using DG.Tweening;
using UnityEngine;

namespace GodTower.Player
{
    /// <summary>
    /// Presentation for the climber; lives on the model root under the ClimberController. Drives a
    /// hand-over-hand cycle while climbing: the two hand IK targets alternate between a reach (up) and a
    /// pull (down) position, and the rig transform leans toward the hand that is up. Also feeds an optional
    /// <see cref="Animator"/> and plays small whole-body reactions to hits and regrabs.
    /// </summary>
    public sealed class ClimberView : MonoBehaviour
    {
        static readonly int StateId = Animator.StringToHash("State");
        static readonly int ClimbSpeedId = Animator.StringToHash("ClimbSpeed");

        [SerializeField] ClimberController climber;
        [SerializeField, Tooltip("Optional: receives the State / ClimbSpeed parameters. Ignored while it has no controller.")] Animator animator;

        [Header("Hand-over-hand")]
        [SerializeField, Tooltip("IK target the left hand follows.")] Transform leftHandTarget;
        [SerializeField, Tooltip("IK target the right hand follows.")] Transform rightHandTarget;
        [SerializeField, Tooltip("Transform that leans toward the raised hand (e.g. the Rig object).")] Transform rig;
        [SerializeField, Tooltip("How far above its start position a hand reaches, in world meters.")] float reachHeight = 0.35f;
        [SerializeField, Tooltip("How far below its start position a hand pulls down to, in world meters.")] float pullDepth = 0.35f;
        [SerializeField, Tooltip("Rig rotation (Euler, added to its start rotation) while the left hand is up.")] Vector3 leftUpEuler = new Vector3(0f, 0f, 15f);
        [SerializeField, Tooltip("Rig rotation (Euler, added to its start rotation) while the right hand is up.")] Vector3 rightUpEuler = new Vector3(0f, 0f, -15f);
        [SerializeField, Tooltip("Full cycles (left up, then right up) per meter climbed.")] float strideFrequency = 0.75f;

        [Header("Attach points for effects")]
        [SerializeField] Transform backAnchor;
        public Transform BackAnchor => backAnchor != null ? backAnchor : transform;

        Tween _reactionTween;
        float _phase;
        Vector3 _leftRest;
        Vector3 _rightRest;
        Quaternion _rigRest = Quaternion.identity;

        void Awake()
        {
            if (climber == null) climber = GetComponentInParent<ClimberController>();
            if (leftHandTarget != null) _leftRest = leftHandTarget.localPosition;
            if (rightHandTarget != null) _rightRest = rightHandTarget.localPosition;
            if (rig != null) _rigRest = rig.localRotation;
        }

        void OnEnable()
        {
            if (climber == null) return;
            climber.HitTaken += OnHit;
            climber.Regrabbed += OnRegrab;
        }

        void OnDisable()
        {
            if (climber == null) return;
            climber.HitTaken -= OnHit;
            climber.Regrabbed -= OnRegrab;
        }

        void LateUpdate()
        {
            if (climber == null) return;

            // Only upward movement advances the cycle; when the climber stops, the hands hold their grip.
            _phase = Mathf.Repeat(_phase + Mathf.Max(0f, climber.VerticalSpeed) * strideFrequency * Time.deltaTime, 1f);
            ApplyHands(Mathf.Sin(_phase * Mathf.PI * 2f));

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetInteger(StateId, (int)climber.State);
                animator.SetFloat(ClimbSpeedId, Mathf.Max(0f, climber.VerticalSpeed));
            }
        }

        /// <param name="stroke">+1: left hand up, right hand down. -1: the opposite. 0: both at their start pose.</param>
        void ApplyHands(float stroke)
        {
            MoveTarget(leftHandTarget, _leftRest, stroke);
            MoveTarget(rightHandTarget, _rightRest, -stroke);

            if (rig != null)
            {
                Vector3 lean = Vector3.Lerp(rightUpEuler, leftUpEuler, (stroke + 1f) * 0.5f);
                rig.localRotation = _rigRest * Quaternion.Euler(lean);
            }
        }

        void MoveTarget(Transform target, Vector3 restLocal, float stroke)
        {
            if (target == null) return;
            float meters = stroke >= 0f ? stroke * reachHeight : stroke * pullDepth;
            // Offsets are in world meters so the climber's scale does not shrink them.
            Vector3 offset = Vector3.up * meters;
            if (target.parent != null) offset = target.parent.InverseTransformVector(offset);
            target.localPosition = restLocal + offset;
        }

        void OnHit(HitData hit)
        {
            if (hit.Linear) return; // A steady push moves the climber itself; a body jolt would read as a jump.
            _reactionTween?.Kill(true);
            Vector3 push = hit.Direction.sqrMagnitude > 0.001f ? hit.Direction.normalized * 0.35f : Vector3.down * 0.3f;
            _reactionTween = transform.DOPunchPosition(push, 0.35f, 12, 0.6f).SetLink(gameObject);
        }

        void OnRegrab()
        {
            _reactionTween?.Kill(true);
            _reactionTween = transform.DOPunchScale(new Vector3(0.12f, -0.12f, 0.12f), 0.25f, 8, 0.5f).SetLink(gameObject);
        }

        /// <summary>Small visual flinch for repeated impacts that don't apply gameplay damage.</summary>
        public void Jolt(Vector3 direction)
        {
            _reactionTween?.Kill(true);
            _reactionTween = transform.DOPunchPosition(direction.normalized * 0.18f, 0.2f, 10, 0.5f).SetLink(gameObject);
        }
    }
}
