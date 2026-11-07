using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>
    /// A moving obstacle (oncoming car, slow car ahead). Moves along the road at <see cref="speed"/> m/s
    /// (+ = same way as the rider, - = oncoming) and follows the road surface. The TrackGenerator moves every
    /// Mover before the rider sweeps for collisions, so fast riders cannot pass through a car.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Mover : MonoBehaviour
    {
        [Tooltip("Metres per second along +Z. Negative drives towards the rider (right-hand lanes: Australia drives on the left).")]
        public float speed = -12f;
        [Tooltip("Pattern speed is scaled by this fraction of the rider's speed instead, when above zero (slow cars ahead).")]
        [Range(0f, 1f)] public float fractionOfRiderSpeed;

        /// <summary>Set by anything that moves a collider during Update (pedestrians, knocked-out props); the
        /// TrackGenerator syncs physics once and clears it before the rider sweeps.</summary>
        public static bool MovedThisFrame;
    }
}
