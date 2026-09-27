using System;
using GodTower.Effects;
using GodTower.Level;
using GodTower.Player;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>
    /// Owns every in-level UI piece (HUD, level progress, flash, notifications, banner, pause and result screens) so the
    /// level flow only talks to one object. Every reference is optional: a missing piece is simply skipped,
    /// and <see cref="TryShowResult"/> reports when there is no result screen to show.
    /// </summary>
    public sealed class GameHudController : MonoBehaviour
    {
        [SerializeField] HudView hud;
        [SerializeField] LevelProgressController levelProgress;
        [SerializeField] ScreenFlash screenFlash;
        [SerializeField] NotificationFeed notifications;
        [SerializeField] LevelBanner banner;
        [SerializeField] PauseMenu pauseMenu;
        [SerializeField] ResultScreen resultScreen;
        [SerializeField] Button pauseButton;

        public event Action PauseRequested;
        public event Action ResumeRequested;
        public event Action RetryRequested;
        public event Action NextLevelRequested;
        public event Action LevelSelectRequested;

        EffectManager _effects;

        /// <summary>Screen flash for effects, or a real null when unassigned (never Unity's "fake null").</summary>
        public IScreenFeedback ScreenFeedback => screenFlash != null ? screenFlash : null;

        void Awake()
        {
            if (pauseButton != null) pauseButton.onClick.AddListener(RaisePause);
            if (pauseMenu != null)
            {
                pauseMenu.ResumeRequested += RaiseResume;
                pauseMenu.RetryRequested += RaiseRetry;
                pauseMenu.LevelSelectRequested += RaiseLevelSelect;
            }
            if (resultScreen != null)
            {
                resultScreen.NextLevelRequested += RaiseNextLevel;
                resultScreen.RetryRequested += RaiseRetry;
                resultScreen.LevelSelectRequested += RaiseLevelSelect;
            }
        }

        /// <summary>Connects the HUD to the level being played. <paramref name="effects"/> may be null.</summary>
        public void Bind(LevelConfig config, ClimberController climber, EffectManager effects)
        {
            if (hud != null) hud.Bind(config, climber);
            if (levelProgress != null) levelProgress.Bind(climber);

            UnbindEffects();
            _effects = effects;
            if (_effects != null) _effects.EffectTriggered += OnEffectTriggered;
        }

        public void ShowBanner(string title, string subtitle, float hold)
        {
            if (banner != null) banner.Show(title, subtitle, hold);
        }

        public void ShowPause()
        {
            if (pauseMenu != null) pauseMenu.Show();
        }

        public void HidePause()
        {
            if (pauseMenu != null) pauseMenu.Hide();
        }

        /// <summary>Shows the win/lose screen. Returns false when no result screen is assigned.</summary>
        public bool TryShowResult(bool won, string levelName, bool hasNextLevel, int heightReached)
        {
            if (resultScreen == null) return false;
            if (won) resultScreen.ShowWin(levelName, hasNextLevel);
            else resultScreen.ShowLose(heightReached);
            return true;
        }

        /// <summary>Removes transient UI (toasts) when leaving or restarting the level.</summary>
        public void ClearTransient()
        {
            if (notifications != null) notifications.ClearAll();
        }

        void OnEffectTriggered(EffectType type, string source)
        {
            if (notifications != null) notifications.Post(type, source);
        }

        void UnbindEffects()
        {
            if (_effects != null) _effects.EffectTriggered -= OnEffectTriggered;
            _effects = null;
        }

        void RaisePause() => PauseRequested?.Invoke();
        void RaiseResume() => ResumeRequested?.Invoke();
        void RaiseRetry() => RetryRequested?.Invoke();
        void RaiseNextLevel() => NextLevelRequested?.Invoke();
        void RaiseLevelSelect() => LevelSelectRequested?.Invoke();

        void OnDestroy()
        {
            UnbindEffects();
            if (pauseButton != null) pauseButton.onClick.RemoveListener(RaisePause);
            if (pauseMenu != null)
            {
                pauseMenu.ResumeRequested -= RaiseResume;
                pauseMenu.RetryRequested -= RaiseRetry;
                pauseMenu.LevelSelectRequested -= RaiseLevelSelect;
            }
            if (resultScreen != null)
            {
                resultScreen.NextLevelRequested -= RaiseNextLevel;
                resultScreen.RetryRequested -= RaiseRetry;
                resultScreen.LevelSelectRequested -= RaiseLevelSelect;
            }
        }
    }
}
