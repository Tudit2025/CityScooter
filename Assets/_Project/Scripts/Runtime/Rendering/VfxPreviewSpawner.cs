using UnityEngine;

namespace CityScooter.Rendering
{
    /// <summary>
    /// Look-dev helper: in play mode, spawns a one-shot effect here every few seconds so it can be judged in context.
    /// Not meant for gameplay scenes.
    /// </summary>
    [AddComponentMenu("CityScooter/Rendering/VFX Preview Spawner")]
    public sealed class VfxPreviewSpawner : MonoBehaviour
    {
        public GameObject prefab;
        [Min(0.2f)] public float interval = 2.5f;
        [Min(0.2f), Tooltip("Spawned copies are destroyed after this many seconds (if the effect did not already).")]
        public float lifetime = 4f;
        [Min(0f)] public float firstDelay = 0.5f;

        float _next;

        void OnEnable() => _next = Time.time + firstDelay;

        void Update()
        {
            if (prefab == null || Time.time < _next) return;
            _next = Time.time + interval;
            GameObject go = Instantiate(prefab, transform.position, transform.rotation);
            Destroy(go, lifetime);
        }
    }
}
