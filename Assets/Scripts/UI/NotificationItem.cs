using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>One pill-shaped event toast: gift icon, sender line and effect label with a count.</summary>
    public sealed class NotificationItem : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] TMP_Text sourceLabel;
        [SerializeField] TMP_Text effectLabel;
        [SerializeField] CanvasGroup group;

        string _effect;

        public string Key { get; private set; }
        public int Count { get; private set; }
        public float Age { get; private set; }

        public void Show(string key, string source, string effect, Color color)
        {
            Key = key;
            _effect = effect;
            Count = 1;
            Age = 0f;
            background.color = color;
            sourceLabel.text = source;
            effectLabel.text = $"{effect}<size=80%>*1</size>";

            group.alpha = 0f;
            UITween.Fade(group, 1f, 0.15f);
            transform.localScale = Vector3.one * 0.6f;
            transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        /// <summary>Repeated events of the same kind collapse into one toast with a counter.</summary>
        public void Increment()
        {
            Count++;
            Age = 0f;
            effectLabel.text = $"{_effect}<size=80%>*{Count}</size>";
            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 6, 0.5f).SetUpdate(true).SetLink(gameObject);
        }

        public void Tick(float dt) => Age += dt;
    }
}
