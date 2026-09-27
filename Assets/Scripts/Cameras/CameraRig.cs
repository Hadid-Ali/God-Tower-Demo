using System.Collections.Generic;
using DG.Tweening;
using GodTower.Core;
using UnityEngine;

namespace GodTower.Cameras
{
    /// <summary>
    /// Perspective follow camera facing the tower. Follows the climber vertically with smoothing and
    /// look-ahead, zooms out for big events (the largest active request wins, then eases back) and
    /// shakes on a child pivot so shake never fights the follow.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Transform shakePivot;
        [SerializeField] Camera cam;

        [Header("Framing")]
        [SerializeField, Tooltip("Distance from the tower axis in normal play.")] float baseDistance = 10.5f;
        [SerializeField, Tooltip("Aim above the target pivot so the climber sits slightly below centre.")] float lookHeight = 2.4f;
        [SerializeField] float cameraHeightOffset = 0.8f;
        [SerializeField] float followSmoothTime = 0.22f;
        [SerializeField, Tooltip("Seconds of vertical velocity to lead by.")] float lookAhead = 0.35f;
        [SerializeField] float zoomSmoothTime = 0.45f;

        [Header("Shake")]
        [SerializeField] float reducedShakeScale = 0.25f;

        struct ZoomRequest
        {
            public float Distance;
            public float EndTime;
        }

        readonly List<ZoomRequest> _zoomRequests = new List<ZoomRequest>();
        float _followY;
        float _followVelocity;
        float _distance;
        float _distanceVelocity;
        float _lastTargetY;
        Tween _shakeTween;

        public Camera Camera => cam;

        void Awake()
        {
            if (cam == null) cam = GetComponentInChildren<Camera>();
            _distance = baseDistance;
        }

        public void SetTarget(Transform newTarget, bool snap = true)
        {
            target = newTarget;
            if (target == null || !snap) return;
            _lastTargetY = target.position.y;
            _followY = _lastTargetY;
            _distance = baseDistance;
            transform.position = DesiredPosition(target.position.y, _distance);
        }

        /// <summary>Zoom out to at least <paramref name="distance"/> for <paramref name="duration"/> seconds.</summary>
        public void RequestZoom(float distance, float duration)
        {
            _zoomRequests.Add(new ZoomRequest { Distance = distance, EndTime = Time.time + duration });
        }

        public void Shake(float strength, float duration, int vibrato = 18)
        {
            if (shakePivot == null) return;
            float scale = SaveData.ReducedShake ? reducedShakeScale : 1f;
            _shakeTween?.Kill(true);
            shakePivot.localPosition = Vector3.zero;
            _shakeTween = shakePivot.DOShakePosition(duration, strength * scale, vibrato, 90f, false, true)
                .SetLink(shakePivot.gameObject);
        }

        public void ClearEffects()
        {
            _zoomRequests.Clear();
            _shakeTween?.Kill();
            if (shakePivot != null) shakePivot.localPosition = Vector3.zero;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float targetY = target.position.y;
            float velocity = (targetY - _lastTargetY) / dt;
            _lastTargetY = targetY;

            float desiredDistance = baseDistance;
            for (int i = _zoomRequests.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _zoomRequests[i].EndTime) _zoomRequests.RemoveAt(i);
                else desiredDistance = Mathf.Max(desiredDistance, _zoomRequests[i].Distance);
            }
            _distance = Mathf.SmoothDamp(_distance, desiredDistance, ref _distanceVelocity, zoomSmoothTime, Mathf.Infinity, dt);

            // Clamp look-ahead so big knockbacks don't whip the camera.
            float lead = Mathf.Clamp(velocity * lookAhead, -2f, 3f);
            _followY = Mathf.SmoothDamp(_followY, targetY + lead, ref _followVelocity, followSmoothTime, Mathf.Infinity, dt);

            transform.position = DesiredPosition(_followY, _distance);
        }

        Vector3 DesiredPosition(float targetY, float distance)
        {
            // Rise slightly as we zoom so wide shots show more tower above than sea below.
            float zoomLift = (distance - baseDistance) * 0.12f;
            return new Vector3(0f, targetY + lookHeight + cameraHeightOffset + zoomLift, -distance);
        }
    }
}
