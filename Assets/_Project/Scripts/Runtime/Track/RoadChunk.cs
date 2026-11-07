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
    /// Conventions: 1 unit = 1 m, +Z is forward, +Y up; the surface starts at y = 0 and ends at endHeight.
    /// Pivot at the START of the chunk, centred on the road (x = 0). The track generator places
    /// the next chunk at local z = <see cref="length"/>. Art owns the visuals; gameplay reads only this
    /// component, its <see cref="SpawnSlot"/> children, and colliders on <see cref="Obstacle"/>s.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoadChunk : MonoBehaviour
    {
        [Tooltip("Length along +Z in metres; the next chunk starts here.")]
        public float length = 40f;

        [Tooltip("Height change from start to end. Negative = downhill; the start surface remains at local y = 0.")]
        public float endHeight;

        public float HeightAt(float localZ) => endHeight * localZ / Mathf.Max(1f, length);

        [Tooltip("Half width of the road surface; rideable sidewalks extend outside this width.")]
        public float halfWidth = 4f;
        [Tooltip("Additional rideable width on each side, outside the road. Zero keeps legacy chunks road-only.")]
        [Min(0)] public float sidewalkWidth;
        [Min(0)] public float sidewalkHeight;
        [Min(.01f)] public float curbTransitionWidth = .3f;
        public float RideableHalfWidth => halfWidth+Mathf.Max(0,sidewalkWidth);
        public float SidewalkOffsetAt(float localX)
        {
            if(sidewalkWidth<=0)return 0;
            float blend=Mathf.InverseLerp(halfWidth-curbTransitionWidth*.5f,halfWidth+curbTransitionWidth*.5f,Mathf.Abs(localX));
            return Mathf.SmoothStep(0,sidewalkHeight,blend);
        }

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
            Vector3 startLeft = new Vector3(-halfWidth, 0f, 0f), startRight = new Vector3(halfWidth, 0f, 0f);
            Vector3 endLeft = new Vector3(-halfWidth, endHeight, length), endRight = new Vector3(halfWidth, endHeight, length);
            Gizmos.DrawLine(startLeft, startRight);
            Gizmos.DrawLine(startLeft, endLeft);
            Gizmos.DrawLine(startRight, endRight);
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
            Gizmos.DrawLine(endLeft, endRight);
        }
    }
}
