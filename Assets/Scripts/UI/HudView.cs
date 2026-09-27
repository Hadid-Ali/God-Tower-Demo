using GodTower.Level;
using GodTower.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>
    /// Gameplay HUD modelled on the reference: a thin cyan tube meter on the left with the goal at the
    /// top and a climber marker carrying the current height; plus level name, boost bar and grab prompt.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [Header("Progress meter")]
        [SerializeField] Image meterFill;
        [SerializeField] RectTransform meterMarker;
        [SerializeField] TMP_Text heightLabel;
        [SerializeField] TMP_Text goalLabel;
        [SerializeField] float fillSmoothing = 10f;

        [Header("Level")]
        [SerializeField] TMP_Text levelLabel;

        [Header("Boost")]
        [SerializeField] CanvasGroup boostGroup;
        [SerializeField] Image boostFill;
        [SerializeField] TMP_Text boostLabel;

        [Header("Recovery")]
        [SerializeField] CanvasGroup grabPrompt;

        ClimberController _climber;
        float _displayedProgress;
        int _displayedHeight = -1;
        string _displayedBoost;

        public void Bind(LevelConfig config, ClimberController climber)
        {
            _climber = climber;
            goalLabel.text = Mathf.RoundToInt(config.goalHeight).ToString();
            levelLabel.text = $"LEVEL {config.number}  <size=70%>{config.displayName.ToUpperInvariant()}</size>";
            _displayedProgress = 0f;
            UITween.SetVisible(boostGroup, false);
            grabPrompt.alpha = 0f;
        }

        void LateUpdate()
        {
            if (_climber == null) return;
            float dt = Time.unscaledDeltaTime;

            _displayedProgress = Mathf.Lerp(_displayedProgress, _climber.Progress01, 1f - Mathf.Exp(-fillSmoothing * dt));
            meterFill.fillAmount = _displayedProgress;
            meterMarker.anchorMin = meterMarker.anchorMax = new Vector2(0.5f, _displayedProgress);

            int height = Mathf.FloorToInt(_climber.HeightUnits);
            if (height != _displayedHeight)
            {
                _displayedHeight = height;
                heightLabel.text = height.ToString();
            }

            UpdateBoost();

            float promptTarget = _climber.CanRegrab ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 14f) : 0f;
            grabPrompt.alpha = Mathf.MoveTowards(grabPrompt.alpha, promptTarget, dt * 8f);
        }

        void UpdateBoost()
        {
            bool boosting = _climber.IsBoosting;
            if (boosting != boostGroup.gameObject.activeSelf) UITween.SetVisible(boostGroup, boosting);
            if (!boosting) return;

            boostFill.fillAmount = _climber.BoostRemaining01;
            if (_displayedBoost == _climber.BoostLabel) return;
            _displayedBoost = _climber.BoostLabel;
            boostLabel.text = _displayedBoost;
        }
    }
}
