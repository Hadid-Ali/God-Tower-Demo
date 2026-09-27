using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    public sealed class LevelSelectButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] TMP_Text numberLabel;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text detailLabel;
        [SerializeField] GameObject completedMark;

        public void Setup(int number, string levelName, int goalHeight, bool completed, Action onClick)
        {
            numberLabel.text = number.ToString();
            nameLabel.text = levelName;
            detailLabel.text = $"Goal {goalHeight}";
            completedMark.SetActive(completed);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }
    }
}
