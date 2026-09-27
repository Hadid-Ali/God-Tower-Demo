using System;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] Button resumeButton;
        [SerializeField] Button retryButton;
        [SerializeField] Button levelSelectButton;

        public event Action ResumeRequested;
        public event Action RetryRequested;
        public event Action LevelSelectRequested;

        void Awake()
        {
            resumeButton.onClick.AddListener(() => ResumeRequested?.Invoke());
            retryButton.onClick.AddListener(() => RetryRequested?.Invoke());
            levelSelectButton.onClick.AddListener(() => LevelSelectRequested?.Invoke());
            UITween.SetVisible(group, false);
        }

        public void Show() => UITween.ShowPanel(group);
        public void Hide() => UITween.HidePanel(group);
    }
}
