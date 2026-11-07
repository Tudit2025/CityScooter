using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>Flashing lights without new shader keywords: shows group A, then group B (police lights, arrow boards).</summary>
    public sealed class Blinker : MonoBehaviour
    {
        public GameObject[] groupA;
        public GameObject[] groupB;
        [Min(0.1f)] public float flashesPerSecond = 3f;
        [Tooltip("B stays off for single-colour lights that just blink.")]
        public bool alternate = true;
        float _offset;
        int _shown = -1;

        void OnEnable() { _offset = Random.value; _shown = -1; }

        void Update()
        {
            int phase = Mathf.FloorToInt(Time.time * flashesPerSecond + _offset) & 1;
            if (phase == _shown) return;
            _shown = phase;
            Set(groupA, phase == 0);
            Set(groupB, alternate && phase == 1);
        }

        static void Set(GameObject[] group, bool on)
        {
            if (group == null) return;
            foreach (GameObject g in group) if (g != null && g.activeSelf != on) g.SetActive(on);
        }
    }
}
