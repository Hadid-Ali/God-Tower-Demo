using System;
using GodTower.Effects;
using GodTower.Level;
using GodTower.Player;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>
    /// Owns every in-level UI piece (HUD with its level complete menu, level progress, notifications, pause button)
    /// so the level flow only talks to one object. Every reference is optional: a missing piece is simply skipped,
    /// and <see cref="TryShowLevelComplete"/> reports when there is no menu to show.
    /// </summary>
    public sealed class GameHudController : MonoBehaviour
    {
        [SerializeField] HudView hud;
        [SerializeField] LevelProgressController levelProgress;
        [SerializeField] NotificationFeed notifications;

        public event Action PauseToggleRequested;
        public event Action NextLevelRequested;
        public event Action QuitRequested;

        EffectManager _effects;

        void Awake()
        {
            if (hud != null)
            {
                hud.ContinueRequested += RaiseNextLevel;
                hud.ExitRequested += RaiseQuit;
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

        /// <summary>Shows the HUD's level complete menu. Returns false when there is none.</summary>
        public bool TryShowLevelComplete() => hud != null && hud.TryShowLevelComplete();

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

        void RaisePauseToggle() => PauseToggleRequested?.Invoke();
        void RaiseNextLevel() => NextLevelRequested?.Invoke();
        void RaiseQuit() => QuitRequested?.Invoke();

        void OnDestroy()
        {
            UnbindEffects();
            if (hud != null)
            {
                hud.ContinueRequested -= RaiseNextLevel;
                hud.ExitRequested -= RaiseQuit;
            }
        }
    }
}
