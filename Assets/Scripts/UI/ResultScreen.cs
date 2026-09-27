using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>
    /// Win / lose card styled after the reference GAME OVER screen: dark overlay, big title and two
    /// stacked white buttons. Win offers Next Level or Level Select; loss offers Retry or Level Select.
    /// </summary>
    public sealed class ResultScreen : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] Button primaryButton;
        [SerializeField] TMP_Text primaryLabel;
        [SerializeField] Button levelSelectButton;

        public event Action NextLevelRequested;
        public event Action RetryRequested;
        public event Action LevelSelectRequested;

        bool _won;

        void Awake()
        {
            primaryButton.onClick.AddListener(OnPrimary);
            levelSelectButton.onClick.AddListener(() => LevelSelectRequested?.Invoke());
            UITween.SetVisible(group, false);
        }

        public void ShowWin(string levelName, bool hasNextLevel)
        {
            _won = true;
            title.text = "SUMMIT!";
            subtitle.text = $"{levelName} complete";
            primaryLabel.text = hasNextLevel ? "Next Level" : "Play Again";
            UITween.ShowPanel(group);
        }

        public void ShowLose(int heightReached)
        {
            _won = false;
            title.text = "GAME OVER";
            subtitle.text = $"Best height {heightReached}";
            primaryLabel.text = "Retry";
            UITween.ShowPanel(group);
        }

        void OnPrimary()
        {
            if (_won) NextLevelRequested?.Invoke();
            else RetryRequested?.Invoke();
        }
    }
}
