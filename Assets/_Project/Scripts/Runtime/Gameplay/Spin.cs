using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>Slow spin around the world up axis, for pickups.</summary>
    public sealed class Spin : MonoBehaviour
    {
        public float degreesPerSecond = 90f;

        void Update() => transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}
