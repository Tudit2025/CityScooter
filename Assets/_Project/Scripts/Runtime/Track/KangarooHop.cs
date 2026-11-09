using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>
    /// A cheap comic hop for a kangaroo (v0.4, Aubin Grove): put it on the body under the Pedestrian's art transform.
    /// While the kangaroo moves it bounds along in arcs (body tilts forward on take-off, tail down on landing, a short
    /// squash between hops); standing still it only twitches now and then. No physics, no animator.
    /// </summary>
    public sealed class KangarooHop : MonoBehaviour
    {
        [Tooltip("Hops per second while moving.")]
        [Min(0.1f)] public float hopsPerSecond = 1.6f;
        [Tooltip("Height of a hop (m).")]
        [Min(0f)] public float height = 0.35f;
        [Tooltip("Forward tilt at take-off (degrees).")]
        public float lean = 14f;

        Transform _mover;
        Vector3 _last;
        float _t, _still;

        void OnEnable()
        {
            Pedestrian p = GetComponentInParent<Pedestrian>();
            _mover = p != null ? p.transform : transform.parent;
            _last = _mover != null ? _mover.position : Vector3.zero;
            _t = Random.value;
            _still = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 now = _mover != null ? _mover.position : Vector3.zero;
            float speed = new Vector2(now.x - _last.x, now.z - _last.z).magnitude / dt;
            _last = now;
            _still = speed > 0.15f ? 0f : _still + dt;
            if (_still > 0.3f)
            {
                // Standing: settle on the haunches, a small ear-and-body twitch every few seconds.
                _t += dt * 0.35f;
                float twitch = Mathf.Max(0f, Mathf.Sin(_t * 9f) - 0.92f) * 6f;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.Euler(-twitch * 3f, 0f, 0f);
                transform.localScale = Vector3.one;
                return;
            }
            _t += dt * hopsPerSecond * Mathf.Clamp(speed / 1.5f, 0.8f, 2.2f);
            float p = _t - Mathf.Floor(_t);
            const float air = 0.72f;
            if (p < air)
            {
                float a = p / air;
                transform.localPosition = new Vector3(0f, 4f * a * (1f - a) * height, 0f);
                // The model faces -Z: a negative pitch tips the head forward.
                transform.localRotation = Quaternion.Euler(-lean * (1f - 2f * a), 0f, 0f);
                transform.localScale = Vector3.one;
            }
            else
            {
                float b = (p - air) / (1f - air);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.Euler(lean * 0.4f * Mathf.Sin(b * Mathf.PI), 0f, 0f);
                transform.localScale = new Vector3(1f, 1f - 0.12f * Mathf.Sin(b * Mathf.PI), 1f);
            }
        }
    }
}
