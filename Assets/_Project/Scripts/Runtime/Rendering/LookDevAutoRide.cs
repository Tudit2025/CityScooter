using UnityEngine;

namespace CityScooter.Rendering
{
    /// <summary>
    /// Look-dev helper: in play mode, moves this object (rider + camera) along +Z and loops back, so skyline drift,
    /// fog pop-in and speed lines can be judged in motion. Not meant for gameplay scenes.
    /// </summary>
    [AddComponentMenu("CityScooter/Rendering/Look Dev Auto Ride")]
    public sealed class LookDevAutoRide : MonoBehaviour
    {
        [Min(0f), Tooltip("Metres per second.")]
        public float speed = 14f;

        [Min(10f), Tooltip("Jump back to the start after this many metres.")]
        public float loopLength = 220f;

        Vector3 _start;

        void OnEnable() => _start = transform.position;

        void Update()
        {
            Vector3 p = transform.position + Vector3.forward * (speed * Time.deltaTime);
            if (p.z - _start.z > loopLength) p = _start;
            transform.position = p;
        }
    }
}
