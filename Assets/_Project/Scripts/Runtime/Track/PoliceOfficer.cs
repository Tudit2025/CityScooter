using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>
    /// A WA police officer (Obstacle with <see cref="Obstacle.police"/>, kind Person, not punchable). A punch without
    /// cartoon mode only knocks the cap off (the gameplay spawns a flying cap); the cap is back when the pool reuses
    /// the officer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PoliceOfficer : MonoBehaviour
    {
        [Tooltip("The cap piece under Art (hidden while it is knocked off).")]
        public GameObject cap;

        public bool CapOff => cap != null && !cap.activeSelf;

        void OnEnable() { if (cap != null) cap.SetActive(true); }

        /// <summary>Hides the cap and says where it was, for the flying copy. False if it is already off.</summary>
        public bool TakeCapOff(out Vector3 position, out Quaternion rotation)
        {
            position = transform.position + Vector3.up * 1.75f;
            rotation = transform.rotation;
            if (cap == null || !cap.activeSelf) return false;
            Renderer r = cap.GetComponent<Renderer>();
            if (r != null) position = r.bounds.center;
            rotation = cap.transform.rotation;
            cap.SetActive(false);
            return true;
        }
    }
}
