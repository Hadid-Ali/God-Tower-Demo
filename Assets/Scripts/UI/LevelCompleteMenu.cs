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

        void Awake()
        {
            Wire();
            Hide();
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
            if (group != null) UITween.ShowPanel(group);
            else gameObject.SetActive(true);
        }

        public void Hide()
        {
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
