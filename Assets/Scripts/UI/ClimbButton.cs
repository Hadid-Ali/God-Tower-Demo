using DG.Tweening;
using GodTower.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GodTower.UI
{
    /// <summary>On-screen hold-to-climb button. Sliding the finger off does not release it; lifting does.</summary>
    public sealed class ClimbButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] ClimbInput input;
        [SerializeField] RectTransform visual;
        [SerializeField] float pressedScale = 0.9f;

        int _pointers;

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointers++;
            input.SetButtonHeld(true);
            Animate(pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_pointers == 0) return;
            _pointers--;
            input.SetButtonHeld(false);
            if (_pointers == 0) Animate(1f);
        }

        void OnDisable()
        {
            while (_pointers > 0)
            {
                _pointers--;
                input.SetButtonHeld(false);
            }
            if (visual != null) visual.localScale = Vector3.one;
        }

        void Animate(float scale)
        {
            if (visual == null) return;
            visual.DOKill();
            visual.DOScale(scale, 0.08f).SetUpdate(true).SetLink(visual.gameObject);
        }
    }
}
