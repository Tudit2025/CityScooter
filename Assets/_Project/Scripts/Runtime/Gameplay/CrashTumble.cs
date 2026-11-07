using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Comic crash: something thrown along the street (the rider off the scooter, the scooter on its side). It flies,
    /// bounces on the road surface a couple of times, then settles into its resting pose. Visual only, no colliders.
    /// </summary>
    public sealed class CrashTumble : MonoBehaviour
    {
        public TrackGenerator track;
        Vector3 _velocity, _spin;
        Quaternion _rest;
        float _lift, _settle;
        bool _landed;

        public bool Settled { get; private set; }

        public void Throw(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 spinDegrees, Quaternion rest, float lift)
        {
            transform.SetPositionAndRotation(position, rotation);
            _velocity = velocity;
            _spin = spinDegrees;
            _rest = rest;
            _lift = lift;
            _settle = 0f;
            _landed = false;
            Settled = false;
            gameObject.SetActive(true);
        }

        /// <summary>Stuck in place (the scooter in a manhole).</summary>
        public void Stick(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _velocity = Vector3.zero;
            Settled = true;
            gameObject.SetActive(true);
        }

        float Ground(Vector3 p) => (track != null ? track.SurfaceHeightAt(p.x, p.z) : 0f) + _lift;

        void Update()
        {
            if (Settled) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            Vector3 p = transform.position;
            if (!_landed)
            {
                _velocity += Vector3.down * 20f * dt;
                p += _velocity * dt;
                transform.Rotate(_spin * dt, Space.World);
                float ground = Ground(p);
                if (p.y <= ground && _velocity.y < 0f)
                {
                    p.y = ground;
                    if (_velocity.y > -3f) _landed = true; // too slow to bounce again: slide and settle
                    _velocity = new Vector3(_velocity.x * 0.55f, -_velocity.y * 0.35f, _velocity.z * 0.55f);
                    _spin *= 0.45f;
                }
            }
            else
            {
                // slide to a stop and roll into the resting pose
                _velocity = Vector3.MoveTowards(new Vector3(_velocity.x, 0f, _velocity.z), Vector3.zero, 9f * dt);
                p += _velocity * dt;
                p.y = Ground(p);
                _settle += dt * 3f;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * _rest, Mathf.Clamp01(_settle) * 0.35f);
                if (_settle > 2.5f && _velocity.sqrMagnitude < 0.01f) Settled = true;
            }
            transform.position = p;
        }
    }
}
