using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GodTower.UI
{
    /// <summary>Big centred title card ("LEVEL 3 / WIND PASSAGE", "CLIMB!") that pops in and fades out.</summary>
    public sealed class LevelBanner : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;

        Sequence _sequence;

        void Awake() => UITween.SetVisible(group, false);

        public void Show(string titleText, string subtitleText, float hold = 1f)
        {
            _sequence?.Kill();
            title.text = titleText;
            subtitle.text = subtitleText;
            subtitle.gameObject.SetActive(!string.IsNullOrEmpty(subtitleText));

            group.gameObject.SetActive(true);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.transform.localScale = Vector3.one * 1.4f;

            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                .Append(UITween.Fade(group, 1f, 0.2f))
                .Join(group.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack))
                .AppendInterval(hold)
                .Append(UITween.Fade(group, 0f, 0.3f))
                .OnComplete(() => group.gameObject.SetActive(false));
        }
    }
}
