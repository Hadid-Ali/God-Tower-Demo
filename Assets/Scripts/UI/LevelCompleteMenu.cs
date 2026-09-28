using System;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>
    /// Shown when the climber reaches the summit. Continue plays the next level, or replays this one when there
    /// is no next level (the level flow decides); Exit quits the game. Only raises events: the level flow acts on them.
    /// </summary>
    public sealed class LevelCompleteMenu : MonoBehaviour
    {
        [SerializeField, Tooltip("Panel to show/hide. Defaults to a CanvasGroup on this object.")] CanvasGroup group;
        [SerializeField] Button continueButton;
        [SerializeField] Button exitButton;

        public event Action ContinueRequested;
        public event Action ExitRequested;

        bool _wired;
        bool _showing;

        void Awake()
        {
            Wire();
            // If the menu starts inactive, Show() activating it is what runs Awake: don't hide it again then.
            if (!_showing) Hide();
        }

        /// <summary>Also called from <see cref="Show"/> in case the menu starts inactive and Awake has not run.</summary>
        void Wire()
        {
            if (_wired) return;
            _wired = true;
            if (group == null) group = GetComponent<CanvasGroup>();
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
            if (exitButton != null) exitButton.onClick.AddListener(OnExit);
        }

        public void Show()
        {
            Wire();
            _showing = true;
            gameObject.SetActive(true);
            if (group != null) UITween.ShowPanel(group);
        }

        public void Hide()
        {
            _showing = false;
            if (group != null) UITween.HidePanel(group);
            else gameObject.SetActive(false);
        }

        void OnContinue() => ContinueRequested?.Invoke();
        void OnExit() => ExitRequested?.Invoke();

        void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinue);
            if (exitButton != null) exitButton.onClick.RemoveListener(OnExit);
        }
    }
}
