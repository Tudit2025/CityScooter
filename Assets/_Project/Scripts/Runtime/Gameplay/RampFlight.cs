using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// The maths of a ramp flight (Ramp feel, 11/9), shared by RiderController (take-off), TrackGenerator (air gifts,
    /// landing zone) and the tests that check every road-wide gap can be cleared at the start speed.
    /// Riding up costs speed (speed squared loses 2 x gravity x rampClimbCost x height, never below rampClimbKeep of the
    /// speed at the foot); a flat top gives the motor time to win some back; take-off speed upwards = lip speed x slope x
    /// launch x rampLaunch (at most rampMaxLaunchVelocity), plus the pop when A was pressed just before the lip; in the air
    /// gravity x hangGravity pulls down and rampAirDrag slows the forward speed. Heights are above the road surface,
    /// which keeps going downhill under the flight (the flight follows it, as in RiderController).
    /// </summary>
    public static class RampFlight
    {
        /// <summary>Gravity while flying from this ramp (m/s2).</summary>
        public static float Gravity(Ramp r, GameTuning t) => t.gravity * HangScale(r, t);

        /// <summary>The ramp's hangGravity times the tuning's (what RiderController multiplies gravity by).</summary>
        public static float HangScale(Ramp r, GameTuning t) => Mathf.Clamp(r.hangGravity * t.rampHangGravity, 0.1f, 2f);

        /// <summary>Speed after climbing <paramref name="climb"/> metres (negative: going down a deck) from
        /// <paramref name="speed"/>; <paramref name="footSpeed"/> is the speed at the foot (the climb keeps rampClimbKeep of it),
        /// <paramref name="cruise"/> the normal speed (a descent gives at most rampDescentMaxBoost times it).</summary>
        public static float AfterClimb(float speed, float climb, float footSpeed, float cruise, GameTuning t)
        {
            if (climb > 0f)
            {
                float v = Mathf.Sqrt(Mathf.Max(0f, speed * speed - 2f * t.gravity * t.rampClimbCost * climb));
                return Mathf.Min(speed, Mathf.Max(v, footSpeed * t.rampClimbKeep));
            }
            if (climb < 0f)
            {
                float v = Mathf.Sqrt(speed * speed - 2f * t.gravity * t.rampDescentGain * climb);
                return Mathf.Max(speed, Mathf.Min(v, cruise * t.rampDescentMaxBoost));
            }
            return speed;
        }

        /// <summary>Speed at the lip for a rider reaching the foot at <paramref name="footSpeed"/> (no brake, no boost): the
        /// climb costs speed, the flat top lets the motor (acceleration) win it back towards the foot speed.</summary>
        public static float LipSpeed(Ramp r, float footSpeed, GameTuning t)
        {
            float v = AfterClimb(footSpeed, r.height, footSpeed, footSpeed, t);
            if (r.top > 0f) v = Mathf.Min(footSpeed, Mathf.Sqrt(v * v + 2f * t.acceleration * r.top));
            return v;
        }

        /// <summary>Take-off speed upwards at the lip (without the pop).</summary>
        public static float LaunchVelocity(Ramp r, float lipSpeed, GameTuning t) =>
            Mathf.Min(lipSpeed * r.Slope * r.launch * t.rampLaunch, t.rampMaxLaunchVelocity);

        /// <summary>Seconds from the lip (at the ramp's height) back down to the road.</summary>
        public static float Seconds(Ramp r, float launchVelocity, GameTuning t)
        {
            float g = Mathf.Max(0.1f, Gravity(r, t));
            return (launchVelocity + Mathf.Sqrt(launchVelocity * launchVelocity + 2f * g * r.height)) / g;
        }

        /// <summary>Metres flown forward in <paramref name="seconds"/> from the lip at <paramref name="lipSpeed"/> (air drag).</summary>
        public static float Forward(float lipSpeed, float seconds, GameTuning t)
        {
            float k = t.rampAirDrag;
            return k > 1e-4f ? lipSpeed * (1f - Mathf.Exp(-k * seconds)) / k : lipSpeed * seconds;
        }

        /// <summary>Forward speed after <paramref name="seconds"/> in the air.</summary>
        public static float AirSpeed(float lipSpeed, float seconds, GameTuning t) => lipSpeed * Mathf.Exp(-t.rampAirDrag * seconds);

        /// <summary>The whole flight for a rider reaching the foot of <paramref name="r"/> at <paramref name="footSpeed"/>
        /// without the pop: seconds in the air, take-off speed upwards, lip speed, and the metres from the lip to the landing.</summary>
        public static float Distance(Ramp r, float footSpeed, GameTuning t, out float seconds, out float launchVelocity, out float lipSpeed)
        {
            lipSpeed = LipSpeed(r, footSpeed, t);
            launchVelocity = LaunchVelocity(r, lipSpeed, t);
            seconds = Seconds(r, launchVelocity, t);
            return Forward(lipSpeed, seconds, t);
        }

        /// <summary>Metres from the lip to the landing for a rider reaching the foot at <paramref name="footSpeed"/> (no pop).</summary>
        public static float Distance(Ramp r, float footSpeed, GameTuning t) => Distance(r, footSpeed, t, out _, out _, out _);
    }
}
