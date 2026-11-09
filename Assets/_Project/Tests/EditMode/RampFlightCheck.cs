using CityScooter.Gameplay;
using CityScooter.Track;
using UnityEditor;

namespace CityScooter.Tests
{
    /// <summary>
    /// The flight prediction the district tests share ("every road-wide gap has a ramp before it, and the flight at the
    /// start speed clears it"): RiderController's own maths (<see cref="RampFlight"/>: the climb costs speed, the flat top
    /// wins some back, take-off from the lip speed, hang gravity, air drag; no pop) with the game's tuning. The flight
    /// follows the road surface downhill (heights are above the road), so the chunk's grade does not change it.
    /// </summary>
    static class RampFlightCheck
    {
        // Main.unity's GameController uses this asset (ramp fields it does not store have the code defaults).
        const string TuningPath = "Assets/_Project/Data/Tuning_ComicDownhill.asset";
        static GameTuning _tuning;

        public static GameTuning Tuning
        {
            get
            {
                if (_tuning == null) _tuning = AssetDatabase.LoadAssetAtPath<GameTuning>(TuningPath);
                return _tuning != null ? _tuning : GameTuning.Default;
            }
        }

        /// <summary>Metres from the lip to the landing for a rider reaching the foot of <paramref name="r"/> at
        /// <paramref name="speed"/>.</summary>
        public static float Flight(Ramp r, float speed) => RampFlight.Distance(r, speed, Tuning);
    }
}
