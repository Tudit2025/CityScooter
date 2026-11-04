using UnityEngine;

namespace CityScooter.Track
{
    public enum SpawnKind
    {
        /// <summary>Gameplay decides: obstacle, gift, or nothing.</summary>
        Any = 0,
        Obstacle = 1,
        Gift = 2,
        /// <summary>Something the rider must jump over (railing across the road).</summary>
        JumpObstacle = 3,
    }

    /// <summary>
    /// Empty child of a <see cref="RoadChunk"/> marking where gameplay may place an obstacle or a gift.
    /// Only the local position matters (y = 0 on the road). Rotation is ignored.
    /// </summary>
    public sealed class SpawnSlot : MonoBehaviour
    {
        public SpawnKind kind = SpawnKind.Any;

        void OnDrawGizmos()
        {
            switch (kind)
            {
                case SpawnKind.Gift: Gizmos.color = new Color(1f, 0.85f, 0.1f); break;
                case SpawnKind.Obstacle: Gizmos.color = new Color(1f, 0.3f, 0.3f); break;
                case SpawnKind.JumpObstacle: Gizmos.color = new Color(1f, 0.3f, 1f); break;
                default: Gizmos.color = Color.white; break;
            }
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.5f);
        }
    }
}
