using UnityEngine;

namespace CityScooter.Track
{
    public enum RoadType
    {
        Street = 0,
        PedestrianMall = 1,
        Viaduct = 2,
        Tunnel = 3,
    }

    /// <summary>
    /// Data contract between art and gameplay: one authored stretch of road.
    /// Conventions: 1 unit = 1 m, +Z is forward, +Y up, road surface at y = 0.
    /// Pivot at the START of the chunk, centred on the road (x = 0). The track generator places
    /// the next chunk at local z = <see cref="length"/>. Art owns the visuals; gameplay reads only this
    /// component, its <see cref="SpawnSlot"/> children, and colliders on <see cref="Obstacle"/>s.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoadChunk : MonoBehaviour
    {
        [Tooltip("Length along +Z in metres; the next chunk starts here.")]
        public float length = 40f;

        [Tooltip("Half width of the area the rider can steer in (rider x stays within ±halfWidth).")]
        public float halfWidth = 4f;

        public RoadType roadType = RoadType.Street;

        [Tooltip("Relative chance of being picked among chunks of the same road type.")]
        public float weight = 1f;

        [Tooltip("Only used once the difficulty level (0..9) reaches this.")]
        [Range(0, 9)] public int minDifficulty;

        public SpawnSlot[] GetSlots() => GetComponentsInChildren<SpawnSlot>(true);

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(new Vector3(0f, 0.05f, length * 0.5f), new Vector3(halfWidth * 2f, 0.1f, length));
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
            Gizmos.DrawLine(new Vector3(-halfWidth, 0f, length), new Vector3(halfWidth, 0f, length));
        }
    }
}
