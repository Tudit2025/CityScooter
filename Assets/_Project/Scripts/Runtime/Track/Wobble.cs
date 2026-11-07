using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>A solid thing that was punched shakes for a moment (its "Art" child only; the collider stays put).</summary>
    public sealed class Wobble : MonoBehaviour
    {
        Transform _art;
        float _t = -1f, _side = 1f;
        const float Seconds = 0.55f;

        public void Hit(float side)
        {
            if (_art == null) _art = transform.Find("Art");
            if (_art == null) return;
            _side = side >= 0f ? 1f : -1f;
            _t = 0f;
        }

        void Update()
        {
            if (_t < 0f || _art == null) return;
            _t += Time.deltaTime;
            float k = Mathf.Max(0f, 1f - _t / Seconds);
            float a = Mathf.Sin(_t * 38f) * 7f * k * k;
            _art.localRotation = Quaternion.Euler(a * 0.3f, 0f, -a * _side);
            if (_t >= Seconds) { _art.localRotation = Quaternion.identity; _t = -1f; }
        }

        void OnDisable() { if (_art != null) _art.localRotation = Quaternion.identity; _t = -1f; }
    }
}
