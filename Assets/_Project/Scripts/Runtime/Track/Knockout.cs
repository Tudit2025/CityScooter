using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>A punched prop spins away and disappears; colliders come back when the pool reuses it.</summary>
    public sealed class Knockout : MonoBehaviour
    {
        Vector3 _velocity, _spin;
        float _left;
        Collider[] _colliders;

        public bool KnockedOut => _left > 0f;

        void Awake() { _colliders = GetComponentsInChildren<Collider>(true); }

        // The pool re-activates the object later: make it solid again.
        void OnDisable() { _left = 0f; SetColliders(true); }

        public void Launch(Vector3 velocity)
        {
            if (_colliders == null) _colliders = GetComponentsInChildren<Collider>(true);
            _velocity = velocity;
            _spin = new Vector3(Random.Range(-720f, 720f), Random.Range(-540f, 540f), Random.Range(-720f, 720f));
            _left = 0.9f;
            SetColliders(false);
            Mover.MovedThisFrame = true;
        }

        void Update()
        {
            if (_left <= 0f) return;
            float dt = Time.deltaTime;
            _left -= dt;
            _velocity += Vector3.down * 18f * dt;
            transform.position += _velocity * dt;
            transform.Rotate(_spin * dt, Space.World);
            if (_left <= 0f) gameObject.SetActive(false);
        }

        void SetColliders(bool on)
        {
            if (_colliders == null) return;
            foreach (Collider c in _colliders) if (c != null) c.enabled = on;
        }
    }
}
