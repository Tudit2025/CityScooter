using System.Collections.Generic;
using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>
    /// A stretch with no ground under the rideable area (v0.3): a river inlet after a jetty, the gap at the end of a
    /// raised walkway. A rider who is on the ground inside it falls and crashes (comic word <see cref="word"/>, e.g.
    /// SPLASH!); a rider flying over it lands safely beyond it. Always put a <see cref="Ramp"/> right before a gap that
    /// covers the whole road, so it can be cleared.
    /// Local space: pivot at the START of the gap on the road centre line, +Z forward; the gap runs from z = 0 to
    /// <see cref="length"/>, across |x| &lt;= width / 2 (width 0 = the whole rideable width).
    /// Usually a child of a road chunk (authored with the chunk) or part of a hazard pattern.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoadGap : MonoBehaviour
    {
        [Min(1f)] public float length = 8f;
        [Tooltip("0 = the whole rideable width (road and sidewalks).")]
        [Min(0f)] public float width;
        [Tooltip("Comic word when someone falls in: SPLASH (water), WHOOPS (a drop).")]
        public string word = "SPLASH";
        [Tooltip("True for water: the fall shows a splash.")]
        public bool water = true;

        public bool Contains(float localX, float localZ) =>
            localZ >= 0f && localZ <= length && (width <= 0f || Mathf.Abs(localX) <= width * 0.5f);

        // ---- Ramps and air (v0.3, system B): runtime registry. Enabled gaps (chunk children, pattern entries) are in
        // Active; recycled chunks are deactivated, so their gaps leave the list by themselves.

        static readonly List<RoadGap> ActiveList = new List<RoadGap>();
        /// <summary>Gaps in the world right now (enabled ones).</summary>
        public static IReadOnlyList<RoadGap> Active => ActiveList;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ActiveList.Clear();

        void OnEnable() { if (!ActiveList.Contains(this)) ActiveList.Add(this); }
        void OnDisable() => ActiveList.Remove(this);

        /// <summary>World z where the gap ends (the far edge).</summary>
        public float EndZ => transform.TransformPoint(new Vector3(0f, 0f, length)).z;

        /// <summary>True when a world point (on the road surface at <paramref name="roadY"/>) is over this gap.</summary>
        public bool ContainsWorld(float worldX, float worldZ, float roadY)
        {
            Vector3 local = transform.InverseTransformPoint(new Vector3(worldX, roadY, worldZ));
            return Contains(local.x, local.z);
        }

        /// <summary>The gap under a world point, or null.</summary>
        public static RoadGap At(float worldX, float worldZ, float roadY)
        {
            for (int i = 0; i < ActiveList.Count; i++)
            {
                RoadGap g = ActiveList[i];
                if (g != null && g.ContainsWorld(worldX, worldZ, roadY)) return g;
            }
            return null;
        }

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.1f, 0.5f, 1f, 0.9f);
            float w = width > 0f ? width * 0.5f : 8f;
            Gizmos.DrawWireCube(new Vector3(0f, -0.5f, length * 0.5f), new Vector3(w * 2f, 1f, length));
        }
    }
}
