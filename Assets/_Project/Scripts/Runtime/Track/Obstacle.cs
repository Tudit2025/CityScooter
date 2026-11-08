using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>
    /// Put on the root of every obstacle prefab (cones, bins, barriers, railings, parked cars).
    /// Needs at least one non-trigger Collider (prefer BoxCollider) on itself or a child.
    /// The rider crashes on contact unless it is airborne and <see cref="canJumpOver"/> is true.
    /// </summary>
    public enum ObstacleKind
    {
        Thing = 0,
        /// <summary>Cars, trucks, police cars.</summary>
        Vehicle = 1,
        /// <summary>Workers, traffic controllers, pedestrians.</summary>
        Person = 2,
        /// <summary>Open manholes and other holes in the road: jump or steer around.</summary>
        Hole = 3,
    }

    [DisallowMultipleComponent]
    public sealed class Obstacle : MonoBehaviour
    {
        [Tooltip("What it is: decides the warning sent to the micro:bit and how a crash looks.")]
        public ObstacleKind kind = ObstacleKind.Thing;
        [Tooltip("Low things (cones, bins, railings) can be cleared with a jump; cars and walls cannot.")]
        public bool canJumpOver = true;

        [Tooltip("Spans the whole road (e.g. a railing across it): steering around is impossible, you must jump.")]
        public bool spansRoad;

        [Tooltip("A punch (\"pu\") knocks it out of the way and the game sends \"ko\". Off for people and vehicles.")]
        public bool punchable;
        [Tooltip("WA police (officers, police cars): punching one, or knocking an officer flying in cartoon mode, raises the wanted level (PoliceChase).")]
        public bool police;
        [Tooltip("Peacocks, swans, magpies (kind Person): warned about and crashed into like people, but a punch only " +
                 "shoos them and cartoon mode never throws them.")]
        public bool animal;
        [Tooltip("Relative chance of being picked for a matching spawn slot.")]
        public float spawnWeight = 1f;
    }
}
