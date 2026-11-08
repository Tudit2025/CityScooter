using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>
    /// A ramp or platform the rider rides up and flies off (v0.3). Data contract between art and gameplay: art builds
    /// the visuals to match these numbers; gameplay reads only this component.
    /// Local space: pivot on the road surface at the START of the run-up, centred on the ramp, +Z forward.
    /// Profile along z: a straight slope from height 0 at z = 0 up to <see cref="height"/> at z = <see cref="rise"/>, then
    /// flat for <see cref="top"/> metres, then the lip (the ground drops away; the rider flies from there).
    /// Across x: rideable for |x| &lt;= width / 2. Off the sides the rider is not lifted (art should show that clearly).
    /// Flying from a ramp uses gravity x <see cref="hangGravity"/>, so the rider hangs in the air for a while.
    /// No colliders are needed on the ramp itself (riding into its side is not a crash), but obstacles may sit on top.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Ramp : MonoBehaviour
    {
        [Tooltip("Rideable width across the road (m).")]
        [Min(0.5f)] public float width = 3f;
        [Tooltip("Length of the slope up (m).")]
        [Min(0.5f)] public float rise = 5f;
        [Tooltip("Height of the lip above the road (m).")]
        [Min(0.1f)] public float height = 1.4f;
        [Tooltip("Flat top after the slope, before the lip (m). 0 = a kicker; longer = a platform.")]
        [Min(0f)] public float top;
        [Tooltip("Multiplies the take-off speed upwards (speed x slope). 1 = what the slope gives.")]
        [Min(0f)] public float launch = 1f;
        [Tooltip("Gravity multiplier while flying from this ramp (lower = longer in the air).")]
        [Range(0.2f, 1f)] public float hangGravity = 0.5f;
        [Tooltip("Lane slots across the road this ramp may be placed in by the generator (-1, 0, 1). Empty = any.")]
        public int[] lanes;

        /// <summary>Total length along z from the start of the run-up to the lip.</summary>
        public float Length => rise + top;

        /// <summary>Surface height above the road at a point in the ramp's local space; 0 outside it.</summary>
        public float HeightAt(float localX, float localZ)
        {
            if (Mathf.Abs(localX) > width * 0.5f || localZ < 0f || localZ > Length) return 0f;
            return localZ >= rise ? height : height * localZ / rise;
        }

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            float w = width * 0.5f;
            foreach (float x in new[] { -w, w })
            {
                Gizmos.DrawLine(new Vector3(x, 0f, 0f), new Vector3(x, height, rise));
                Gizmos.DrawLine(new Vector3(x, height, rise), new Vector3(x, height, Length));
                Gizmos.DrawLine(new Vector3(x, height, Length), new Vector3(x, 0f, Length));
            }
            Gizmos.DrawLine(new Vector3(-w, height, Length), new Vector3(w, height, Length));
        }
    }
}
