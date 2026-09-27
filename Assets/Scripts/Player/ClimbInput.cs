using UnityEngine;
using UnityEngine.InputSystem;

namespace GodTower.Player
{
    public interface IClimbInput
    {
        bool IsHeld { get; }
        bool PressedThisFrame { get; }
    }

    /// <summary>
    /// Merges the on-screen Climb button (touch / mouse) and the Space key (Editor, desktop) into
    /// one hold signal. Runs early so gameplay reads a consistent value for the frame.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class ClimbInput : MonoBehaviour, IClimbInput
    {
        int _buttonHolds;
        bool _wasHeld;

        public bool IsHeld { get; private set; }
        public bool PressedThisFrame { get; private set; }

        /// <summary>Called by <see cref="GodTower.UI.ClimbButton"/> on pointer down/up.</summary>
        public void SetButtonHeld(bool held) => _buttonHolds = Mathf.Max(0, _buttonHolds + (held ? 1 : -1));

        public void ReleaseAll()
        {
            _buttonHolds = 0;
            IsHeld = false;
            PressedThisFrame = false;
            _wasHeld = false;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            bool keyHeld = keyboard != null && (keyboard.spaceKey.isPressed || keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed);

            IsHeld = keyHeld || _buttonHolds > 0;
            PressedThisFrame = IsHeld && !_wasHeld;
            _wasHeld = IsHeld;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            // A finger lifted while the app was in the background never sends pointer-up.
            if (!hasFocus) ReleaseAll();
        }
    }
}
