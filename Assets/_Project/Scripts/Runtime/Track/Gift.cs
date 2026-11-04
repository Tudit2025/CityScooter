using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>
    /// Put on the root of every pickup prefab. Needs a trigger Collider (SphereCollider, isTrigger = true).
    /// Gameplay awards points, plays the pickup effect and raises GameEvents.GiftCollected ("gift" to the micro:bit).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Gift : MonoBehaviour
    {
        public int points = 10;

        [Tooltip("Optional effect spawned on pickup (particles); gameplay instantiates it and destroys it after a few seconds.")]
        public GameObject pickupEffect;
    }
}
