using DG.Tweening;
using GodTower.Effects;
using UnityEngine;
using UnityEngine.UI;

namespace GodTower.UI
{
    /// <summary>Full-screen colour flash for impacts and boosts. Never blocks input.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class ScreenFlash : MonoBehaviour, IScreenFeedback
    {
        [SerializeField, Range(0f, 1f), Tooltip("Upper bound so flashes never white out the meter.")] float maxAlpha = 0.6f;

        Image _image;
        Tween _tween;

        void Awake()
        {
            _image = GetComponent<Image>();
            _image.raycastTarget = false;
            SetAlpha(0f);
        }

        public void Flash(Color color, float alpha, float duration)
        {
            _tween?.Kill();
            float start = Mathf.Max(Mathf.Min(alpha, maxAlpha), _image.color.a);
            color.a = start;
            _image.color = color;
            _tween = UITween.FadeGraphic(_image, 0f, duration).SetEase(Ease.OutQuad);
        }

        void SetAlpha(float alpha)
        {
            Color c = _image.color;
            c.a = alpha;
            _image.color = c;
        }
    }
}
