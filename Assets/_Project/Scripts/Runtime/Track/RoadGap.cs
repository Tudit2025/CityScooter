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

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.1f, 0.5f, 1f, 0.9f);
            float w = width > 0f ? width * 0.5f : 8f;
            Gizmos.DrawWireCube(new Vector3(0f, -0.5f, length * 0.5f), new Vector3(w * 2f, 1f, length));
        }
    }
}
