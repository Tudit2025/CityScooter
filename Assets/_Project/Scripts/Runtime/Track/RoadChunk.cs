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

        [Header("Optional width change along the chunk (boulevard transitions)")]
        [Tooltip("Road half width at the END of the chunk. Zero (default) keeps halfWidth along the whole chunk. The kerb, " +
                 "the sidewalk and the rideable edge all follow HalfWidthAt(z); the art must too.")]
        [Min(0)] public float endHalfWidth;
        [Tooltip("Local z where the width starts and stops changing (smoothstep in between).")]
        public Vector2 widthChangeZ = new Vector2(8f, 32f);

        /// <summary>True for transition chunks whose road gets wider or narrower along z.</summary>
        public bool ChangesWidth => endHalfWidth > 0f && Mathf.Abs(endHalfWidth - halfWidth) > 0.001f;

        /// <summary>Road half width at local z (constant unless <see cref="endHalfWidth"/> is set).</summary>
        public float HalfWidthAt(float localZ)
        {
            if (!ChangesWidth) return halfWidth;
            float t = Mathf.InverseLerp(widthChangeZ.x, widthChangeZ.y, localZ);
            return Mathf.Lerp(halfWidth, endHalfWidth, t * t * (3f - 2f * t));
        }

        /// <summary>Road plus sidewalk half width at the START of the chunk (legacy; see <see cref="RideableHalfWidthAt"/>).</summary>
        public float RideableHalfWidth => halfWidth+Mathf.Max(0,sidewalkWidth);
        public float RideableHalfWidthAt(float localZ) => HalfWidthAt(localZ)+Mathf.Max(0,sidewalkWidth);
        /// <summary>Kerb height offset at local x, for the start width (legacy; see the overload with z).</summary>
        public float SidewalkOffsetAt(float localX) => SidewalkOffset(localX, halfWidth);
        public float SidewalkOffsetAt(float localX, float localZ) => SidewalkOffset(localX, HalfWidthAt(localZ));

        float SidewalkOffset(float localX, float kerb)
        {
            if(sidewalkWidth<=0)return 0;
            float blend=Mathf.InverseLerp(kerb-curbTransitionWidth*.5f,kerb+curbTransitionWidth*.5f,Mathf.Abs(localX));
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
            Vector3 Edge(float side, float z) => new Vector3(side * HalfWidthAt(z), HeightAt(z), z);
            Gizmos.DrawLine(Edge(-1f, 0f), Edge(1f, 0f));
            const int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float z0 = length * i / steps, z1 = length * (i + 1) / steps;
                Gizmos.DrawLine(Edge(-1f, z0), Edge(-1f, z1));
                Gizmos.DrawLine(Edge(1f, z0), Edge(1f, z1));
            }
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
            Gizmos.DrawLine(Edge(-1f, length), Edge(1f, length));
        }
    }
}
