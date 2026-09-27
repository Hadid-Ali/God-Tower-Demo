using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>
    /// uGUI tweens built on DOTween core (the DOTween UI module is not in an assembly definition).
    /// Menu tweens run on unscaled time so they work while the game is paused.
    /// </summary>
    public static class UITween
    {
        public static Tween Fade(CanvasGroup group, float to, float duration) =>
            DOTween.To(() => group.alpha, a => group.alpha = a, to, duration)
                .SetUpdate(true)
                .SetLink(group.gameObject);

        public static Tween FadeGraphic(Graphic graphic, float to, float duration) =>
            DOTween.To(() => graphic.color.a, a =>
                {
                    Color c = graphic.color;
                    c.a = a;
                    graphic.color = c;
                }, to, duration)
                .SetUpdate(true)
                .SetLink(graphic.gameObject);

        public static void SetVisible(CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            group.gameObject.SetActive(visible);
        }

        /// <summary>Pop-in used by every panel: fade and a slight scale overshoot.</summary>
        public static void ShowPanel(CanvasGroup group)
        {
            DOTween.Kill(group);
            group.transform.DOKill();
            group.gameObject.SetActive(true);
            group.interactable = true;
            group.blocksRaycasts = true;
            group.alpha = 0f;
            Fade(group, 1f, 0.2f).SetId(group);
            group.transform.localScale = Vector3.one * 0.92f;
            group.transform.DOScale(1f, 0.28f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(group.gameObject);
        }

        public static void HidePanel(CanvasGroup group)
        {
            DOTween.Kill(group);
            group.transform.DOKill();
            SetVisible(group, false);
        }
    }
}
