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
        /// <summary>v1: a punch hit something ("ko").</summary>
        public static event Action PunchLanded;
        /// <summary>v1: something is in the rider's lane ahead; the argument is the whole message ("warn:car").</summary>
        public static event Action<string> Warning;
        /// <summary>v1: the fake road turns: -1 left, 0 straight, 1 right.</summary>
        public static event Action<int> TurnChanged;

        public static void RaiseRunStarted() => RunStarted?.Invoke();
        public static void RaiseCrashed() => Crashed?.Invoke();
        public static void RaiseGiftCollected() => GiftCollected?.Invoke();
        public static void RaiseSpeedLevel(int level) => SpeedLevelChanged?.Invoke(level);
        public static void RaisePunchLanded() => PunchLanded?.Invoke();
        public static void RaiseWarning(string message) => Warning?.Invoke(message);
        public static void RaiseTurnChanged(int direction) => TurnChanged?.Invoke(direction);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Needed when "Enter Play Mode Options" skips the domain reload.
            RunStarted = null;
            Crashed = null;
            GiftCollected = null;
            SpeedLevelChanged = null;
            PunchLanded = null;
            Warning = null;
            TurnChanged = null;
        }
    }
}
