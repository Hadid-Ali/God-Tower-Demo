using DG.Tweening;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>Pulsing billboard that telegraphs an incoming hazard before it lands.</summary>
    public sealed class WarningMarker : MonoBehaviour
    {
        [SerializeField] float pulseScale = 1.35f;
        [SerializeField] float pulsePeriod = 0.18f;

        Vector3 _baseScale;
        Tween _pulse;

        void Awake() => _baseScale = transform.localScale;

        void OnEnable()
        {
            transform.localScale = _baseScale;
            _pulse = transform.DOScale(_baseScale * pulseScale, pulsePeriod)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject);
        }

        void OnDisable()
        {
            _pulse?.Kill();
            _pulse = null;
        }
    }
}
