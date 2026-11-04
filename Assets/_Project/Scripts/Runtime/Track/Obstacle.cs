using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>
    /// Put on the root of every obstacle prefab (cones, bins, barriers, railings, parked cars).
    /// Needs at least one non-trigger Collider (prefer BoxCollider) on itself or a child.
    /// The rider crashes on contact unless it is airborne and <see cref="canJumpOver"/> is true.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Obstacle : MonoBehaviour
    {
        [Tooltip("Low things (cones, bins, railings) can be cleared with a jump; cars and walls cannot.")]
        public bool canJumpOver = true;

        [Tooltip("Spans the whole road (e.g. a railing across it): steering around is impossible, you must jump.")]
        public bool spansRoad;

        [Tooltip("Relative chance of being picked for a matching spawn slot.")]
        public float spawnWeight = 1f;
    }
}
