using System;
using UnityEngine;

namespace CityScooter.Game
{
    /// <summary>
    /// Gameplay raises these; <see cref="Devices.DeviceFeedback"/> turns them into micro:bit messages.
    /// Static so any script can raise them without references; reset on play-mode start.
    /// </summary>
    public static class GameEvents
    {
        public static event Action RunStarted;
        public static event Action Crashed;
        public static event Action GiftCollected;
        /// <summary>0 (stopped) .. 9 (top speed).</summary>
        public static event Action<int> SpeedLevelChanged;

        public static void RaiseRunStarted() => RunStarted?.Invoke();
        public static void RaiseCrashed() => Crashed?.Invoke();
        public static void RaiseGiftCollected() => GiftCollected?.Invoke();
        public static void RaiseSpeedLevel(int level) => SpeedLevelChanged?.Invoke(level);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Needed when "Enter Play Mode Options" skips the domain reload.
            RunStarted = null;
            Crashed = null;
            GiftCollected = null;
            SpeedLevelChanged = null;
        }
    }
}
