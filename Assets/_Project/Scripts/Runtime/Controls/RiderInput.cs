using CityScooter.Devices;
using UnityEngine;

namespace CityScooter.Controls
{
    /// <summary>What the rider should do this frame.</summary>
    public struct RiderInput
    {
        /// <summary>-1 (left) .. 1 (right).</summary>
        public float Steer;
        /// <summary>True on the frame a jump was requested (keyboard or any device).</summary>
        public bool Jump;
        /// <summary>Held.</summary>
        public bool Brake;
        /// <summary>v1: true on the frame a punch was requested.</summary>
        public bool Punch;
        /// <summary>v1: true on the frame the bell was rung.</summary>
        public bool Bell;
        /// <summary>v1: true on the frame a boost was requested.</summary>
        public bool Boost;
        /// <summary>v1 police: true on the frame a board sent "sur" (pull over now).</summary>
        public bool Surrender;
        /// <summary>v0.3 ramps: tricks requested this frame ("trick" lines from boards, Q on the keyboard).</summary>
        public int Trick;
        /// <summary>v0.5 item race: true on the frame "use" arrived from a board (or E was pressed while holding an item).</summary>
        public bool Use;
        /// <summary>A micro:bit / simulator is steering right now (for the HUD).</summary>
        public bool DeviceSteering;
    }

    /// <summary>
    /// Gameplay's only input API: merges keyboard/gamepad with every connected device.
    /// Call <see cref="Read"/> from Update (not FixedUpdate); repeated calls in one frame return the same value.
    /// </summary>
    public static class RiderInputSource
    {
        static int _frame = -1;
        static RiderInput _last;

        /// <summary>The value returned by the most recent Read (does not consume anything).</summary>
        public static RiderInput Last => _last;

        public static RiderInput Read()
        {
            if (Time.frameCount == _frame) return _last;
            _frame = Time.frameCount;

            DeviceHub hub = DeviceHub.Instance;
            double now = DeviceHub.Now;
            bool keyboard = hub == null || hub.KeyboardEnabled;

            var input = new RiderInput();
            if (hub != null && hub.Mixer.HasRecentSteer(now))
            {
                input.Steer = hub.Mixer.Steer(now);
                input.DeviceSteering = true;
            }
            float keySteer = keyboard ? KeyboardControls.Steer() : 0f;
            if (Mathf.Abs(keySteer) > 0.01f)
            {
                input.Steer = keySteer; // a pressed key wins while it is held
                input.DeviceSteering = false;
            }
            // Non-short-circuit '|' so pending device jumps are always consumed.
            input.Jump = (keyboard && KeyboardControls.JumpPressed()) | (hub != null && hub.Mixer.ConsumeJump());
            input.Brake = (keyboard && KeyboardControls.BrakeHeld()) || (hub != null && hub.Mixer.Braking(now));
            input.Punch = (keyboard && KeyboardControls.PunchPressed()) | (hub != null && hub.Mixer.ConsumePunch());
            input.Bell = (keyboard && KeyboardControls.BellPressed()) | (hub != null && hub.Mixer.ConsumeBell());
            // v0.5 item race: E uses the item in hand; with empty hands (and outside race mode) E is the boost, as before.
            bool eKey = keyboard && KeyboardControls.BoostPressed();
            bool useKey = eKey && ItemInHand != null && ItemInHand();
            input.Boost = (eKey && !useKey) | (hub != null && hub.Mixer.ConsumeBoost());
            input.Use = useKey | (hub != null && hub.Mixer.ConsumeUse());
            input.Surrender = hub != null && hub.Mixer.ConsumeSurrender(); // keyboard players hold S to pull over
            input.Trick = (keyboard && KeyboardControls.TrickPressed() ? 1 : 0) + (hub != null ? hub.Mixer.ConsumeTricks() : 0); // v0.3 ramps

            _last = input;
            return input;
        }

        /// <summary>v0.5 item race: set by RaceItems; true while the rider holds an item (then the E key means "use").</summary>
        public static System.Func<bool> ItemInHand;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            ItemInHand = null;
            _frame = -1;
            _last = default;
        }
    }
}
