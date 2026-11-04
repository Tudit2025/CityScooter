using UnityEngine;
using UnityEngine.InputSystem;

namespace CityScooter.Controls
{
    /// <summary>Keyboard and gamepad fallback, so the game is fully playable without a micro:bit.</summary>
    public static class KeyboardControls
    {
        /// <summary>-1..1. Arrow keys / A,D; a gamepad stick overrides when pushed.</summary>
        public static float Steer()
        {
            float s = 0f;
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.leftArrowKey.isPressed || k.aKey.isPressed) s -= 1f;
                if (k.rightArrowKey.isPressed || k.dKey.isPressed) s += 1f;
            }
            Gamepad g = Gamepad.current;
            if (g != null)
            {
                float x = g.leftStick.x.ReadValue();
                if (Mathf.Abs(x) > 0.15f) s = x;
            }
            return Mathf.Clamp(s, -1f, 1f);
        }

        /// <summary>Space / ↑ / W, gamepad south button.</summary>
        public static bool JumpPressed()
        {
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            return (k != null && (k.spaceKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame))
                || (g != null && g.buttonSouth.wasPressedThisFrame);
        }

        /// <summary>↓ / S, gamepad east button or left trigger.</summary>
        public static bool BrakeHeld()
        {
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            return (k != null && (k.downArrowKey.isPressed || k.sKey.isPressed))
                || (g != null && (g.buttonEast.isPressed || g.leftTrigger.ReadValue() > 0.3f));
        }
    }
}
