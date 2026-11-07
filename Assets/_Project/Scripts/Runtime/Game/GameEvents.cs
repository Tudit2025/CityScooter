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
        /// <summary>v1 cartoon mode: the rider bumped a person, who went flying ("boing").</summary>
        public static event Action PersonBounced;
        /// <summary>v1: a punch was ignored because the rider is out of punches ("tired").</summary>
        public static event Action Tired;
        /// <summary>v1: a board asked the game to say a word ("say:party"): (device id, word).</summary>
        public static event Action<string, string> BoardSaid;
        /// <summary>v1: something is in the rider's lane ahead; the argument is the whole message ("warn:car").</summary>
        public static event Action<string> Warning;
        /// <summary>v1: the fake road turns: -1 left, 0 straight, 1 right.</summary>
        public static event Action<int> TurnChanged;

        public static void RaiseRunStarted() => RunStarted?.Invoke();
        public static void RaiseCrashed() => Crashed?.Invoke();
        public static void RaiseGiftCollected() => GiftCollected?.Invoke();
        public static void RaiseSpeedLevel(int level) => SpeedLevelChanged?.Invoke(level);
        public static void RaisePunchLanded() => PunchLanded?.Invoke();
        public static void RaisePersonBounced() => PersonBounced?.Invoke();
        public static void RaiseTired() => Tired?.Invoke();
        public static void RaiseBoardSaid(string deviceId, string word) => BoardSaid?.Invoke(deviceId, word);
        public static void RaiseWarning(string message) => Warning?.Invoke(message);
        public static void RaiseTurnChanged(int direction) => TurnChanged?.Invoke(direction);

        // ---- v1 WA police (PoliceChase, PoliceHelicopter)
        /// <summary>0..5 stars ("wanted:N").</summary>
        public static event Action<int> WantedChanged;
        /// <summary>A police chase started / ended ("chase:1" / "chase:0").</summary>
        public static event Action<bool> ChaseChanged;
        /// <summary>0 (right behind) .. 9 (far or none): the chasing police car's distance ("cop:N").</summary>
        public static event Action<int> CopDistanceChanged;
        /// <summary>The police helicopter arrived / left ("heli:1" / "heli:0").</summary>
        public static event Action<bool> HeliChanged;
        /// <summary>A paint pellet's ring or the red dot locked: -1 on the rider's left, 0 on the rider, 1 on the right ("inc:L|C|R").</summary>
        public static event Action<int> Incoming;
        /// <summary>The ride ended: pulled over or boxed in ("arrested"; DeviceFeedback sends "crash" first).</summary>
        public static event Action Arrested;
        /// <summary>The ride ended: hit by the police car or a paint pellet ("busted", after "crash").</summary>
        public static event Action Busted;
        /// <summary>The wanted level wore off to 0: "LOST THEM!" ("lost").</summary>
        public static event Action LostPolice;

        public static void RaiseWantedChanged(int stars) => WantedChanged?.Invoke(stars);
        public static void RaiseChaseChanged(bool on) => ChaseChanged?.Invoke(on);
        public static void RaiseCopDistance(int level) => CopDistanceChanged?.Invoke(level);
        public static void RaiseHeliChanged(bool on) => HeliChanged?.Invoke(on);
        public static void RaiseIncoming(int side) => Incoming?.Invoke(side);
        public static void RaiseArrested() => Arrested?.Invoke();
        public static void RaiseBusted() => Busted?.Invoke();
        public static void RaiseLostPolice() => LostPolice?.Invoke();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Needed when "Enter Play Mode Options" skips the domain reload.
            RunStarted = null;
            Crashed = null;
            GiftCollected = null;
            SpeedLevelChanged = null;
            PunchLanded = null;
            PersonBounced = null;
            Tired = null;
            BoardSaid = null;
            Warning = null;
            TurnChanged = null;
            WantedChanged = null;
            ChaseChanged = null;
            CopDistanceChanged = null;
            HeliChanged = null;
            Incoming = null;
            Arrested = null;
            Busted = null;
            LostPolice = null;
        }
    }
}
