using GodTower.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>
    /// Level progress as a filler bar with a text label on top, plus a distance label that moves between a min and
    /// max anchored Y with the fill. Fills from the base of the tower to the summit using the climber's progress, smoothed so knockbacks
    /// drain it visibly. Works on its own when
    /// <see cref="climber"/> is assigned, or is bound by <see cref="GameHudController"/>.
    /// </summary>
    public sealed class LevelProgressController : MonoBehaviour
    {
        [SerializeField, Tooltip("Optional here: GameHudController binds it at level start. Assign to use without a HUD controller.")]
        ClimberController climber;

        [Header("Filler")]
        [SerializeField, Tooltip("Image whose fill shows progress. Set up as a Filled image automatically.")] Image fill;
        [SerializeField] Image.FillMethod fillMethod = Image.FillMethod.Vertical;
        [SerializeField, Tooltip("Higher is snappier; 0 disables smoothing.")] float fillSmoothing = 10f;

        [Header("Label")]
        [SerializeField, Tooltip("Text shown on top of the filler.")] TMP_Text label;
        [SerializeField, Tooltip("{0} = height, {1} = goal, {2} = percent.")] string labelFormat = "{0} / {1}";

        [Header("Distance label")]
        [SerializeField, Tooltip("Shows the height reached and moves between the min and max anchored Y with the fill.")] TMP_Text distanceLabel;
        [SerializeField, Tooltip("{0} = height reached.")] string distanceFormat = "{0}m";
        [SerializeField, Tooltip("Label anchoredPosition.y at 0% progress (empty fill).")] float minAnchoredY;
        [SerializeField, Tooltip("Label anchoredPosition.y at 100% progress (full fill).")] float maxAnchoredY = 500f;

        ClimberController _bound;
        float _displayed;
        int _shownHeight = -1;
        int _shownGoal = -1;
        int _shownPercent = -1;
        int _shownDistance = -1;

        void Awake()
        {
            if (fill == null) return;
            fill.type = Image.Type.Filled;
            fill.fillMethod = fillMethod;
            fill.fillOrigin = 0; // Bottom for vertical, left for horizontal.
        }

        void Start()
        {
            if (_bound == null && climber != null) Bind(climber);
        }

        /// <summary>Tracks <paramref name="target"/> and snaps the filler to its current progress.</summary>
        public void Bind(ClimberController target)
        {
            _bound = target;
            _shownHeight = _shownGoal = _shownPercent = _shownDistance = -1;
            _displayed = target != null ? target.Progress01 : 0f;
            Refresh(immediate: true);
        }

        void LateUpdate() => Refresh(immediate: false);

        void Refresh(bool immediate)
        {
            if (_bound == null) return;

            float target = _bound.Progress01;
            _displayed = immediate || fillSmoothing <= 0f
                ? target
                : Mathf.Lerp(_displayed, target, 1f - Mathf.Exp(-fillSmoothing * Time.unscaledDeltaTime));
            if (fill != null) fill.fillAmount = _displayed;

            UpdateLabel();
            UpdateDistanceLabel();
        }

        void UpdateDistanceLabel()
        {
            if (distanceLabel == null) return;

            int distance = Mathf.FloorToInt(_bound.HeightUnits);
            if (distance != _shownDistance)
            {
                _shownDistance = distance;
                distanceLabel.text = string.Format(distanceFormat, distance);
            }

            // Same smoothed value as the fill, so the label stays level with it.
            RectTransform rect = distanceLabel.rectTransform;
            Vector2 position = rect.anchoredPosition;
            position.y = Mathf.LerpUnclamped(minAnchoredY, maxAnchoredY, _displayed);
            rect.anchoredPosition = position;
        }

        [ContextMenu("Distance Label/Use Current Position As Min")]
        void CaptureMin()
        {
            if (distanceLabel != null) minAnchoredY = distanceLabel.rectTransform.anchoredPosition.y;
        }

        [ContextMenu("Distance Label/Use Current Position As Max")]
        void CaptureMax()
        {
            if (distanceLabel != null) maxAnchoredY = distanceLabel.rectTransform.anchoredPosition.y;
        }

        void UpdateLabel()
        {
            if (label == null) return;

            int height = Mathf.FloorToInt(_bound.HeightUnits);
            int goal = Mathf.RoundToInt(_bound.GoalUnits);
            int percent = Mathf.FloorToInt(_bound.Progress01 * 100f);
            // Only rebuild the string when a shown number changes.
            if (height == _shownHeight && goal == _shownGoal && percent == _shownPercent) return;

            _shownHeight = height;
            _shownGoal = goal;
            _shownPercent = percent;
            label.text = $"{string.Format(labelFormat, height, goal, percent)}m";
        }
    }
}
