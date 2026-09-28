using System;
using GodTower.Level;
using GodTower.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>
    /// Gameplay HUD: level name, boost bar, grab prompt and the level complete menu shown at the summit.
    /// Level progress lives in <see cref="LevelProgressController"/>. Every reference is optional.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField] TMP_Text levelLabel;

        [Header("Boost")]
        [SerializeField] CanvasGroup boostGroup;
        [SerializeField] Image boostFill;
        [SerializeField] TMP_Text boostLabel;

        [Header("Recovery")]
        [SerializeField] CanvasGroup grabPrompt;

        [Header("Level complete")]
        [SerializeField, Tooltip("Shown when the climber reaches the summit.")] LevelCompleteMenu levelCompleteMenu;

        /// <summary>Continue pressed on the level complete menu.</summary>
        public event Action ContinueRequested;
        /// <summary>Exit pressed on the level complete menu.</summary>
        public event Action ExitRequested;

        ClimberController _climber;
        string _displayedBoost;

        void Awake()
        {
            if (levelCompleteMenu == null) return;
            levelCompleteMenu.ContinueRequested += RaiseContinue;
            levelCompleteMenu.ExitRequested += RaiseExit;
        }

        public void Bind(LevelConfig config, ClimberController climber)
        {
            _climber = climber;
            if (levelLabel != null) levelLabel.text = $"LEVEL {config.number}  <size=70%>{config.displayName.ToUpperInvariant()}</size>";
            if (boostGroup != null) UITween.SetVisible(boostGroup, false);
            if (grabPrompt != null) grabPrompt.alpha = 0f;
        }

        /// <summary>Shows the level complete menu. Returns false when none is assigned.</summary>
        public bool TryShowLevelComplete()
        {
            if (levelCompleteMenu == null) return false;
            levelCompleteMenu.Show();
            return true;
        }

        void LateUpdate()
        {
            if (_climber == null) return;
            float dt = Time.unscaledDeltaTime;

            UpdateBoost();

            if (grabPrompt != null)
            {
                float promptTarget = _climber.CanRegrab ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 14f) : 0f;
                grabPrompt.alpha = Mathf.MoveTowards(grabPrompt.alpha, promptTarget, dt * 8f);
            }
        }

        void UpdateBoost()
        {
            if (boostGroup == null) return;
            bool boosting = _climber.IsBoosting;
            if (boosting != boostGroup.gameObject.activeSelf) UITween.SetVisible(boostGroup, boosting);
            if (!boosting) return;

            if (boostFill != null) boostFill.fillAmount = _climber.BoostRemaining01;
            if (_displayedBoost == _climber.BoostLabel) return;
            _displayedBoost = _climber.BoostLabel;
            if (boostLabel != null) boostLabel.text = _displayedBoost;
        }

        void RaiseContinue() => ContinueRequested?.Invoke();
        void RaiseExit() => ExitRequested?.Invoke();

        void OnDestroy()
        {
            if (levelCompleteMenu == null) return;
            levelCompleteMenu.ContinueRequested -= RaiseContinue;
            levelCompleteMenu.ExitRequested -= RaiseExit;
        }
    }
}
