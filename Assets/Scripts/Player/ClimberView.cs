using DG.Tweening;
using UnityEngine;

namespace GodTower.Player
{
    /// <summary>
    /// Presentation for the climber; lives on the model root under the ClimberController. Feeds the climber
    /// state into an optional <see cref="Animator"/> (State / ClimbSpeed parameters) and plays small
    /// whole-body reactions to hits and regrabs. Bone animation is left to the character's own setup.
    /// </summary>
    public sealed class ClimberView : MonoBehaviour
    {
        static readonly int StateId = Animator.StringToHash("State");
        static readonly int ClimbSpeedId = Animator.StringToHash("ClimbSpeed");

        [SerializeField] ClimberController climber;
        [SerializeField, Tooltip("Optional: receives the State / ClimbSpeed parameters. Ignored while it has no controller.")] Animator animator;

        [Header("Attach points for effects")]
        [SerializeField] Transform backAnchor;
        public Transform BackAnchor => backAnchor != null ? backAnchor : transform;

        Tween _reactionTween;

        void Awake()
        {
            if (climber == null) climber = GetComponentInParent<ClimberController>();
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
            if (climber == null || animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetInteger(StateId, (int)climber.State);
            animator.SetFloat(ClimbSpeedId, Mathf.Max(0f, climber.VerticalSpeed));
        }

        void OnHit(HitData hit)
        {
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
