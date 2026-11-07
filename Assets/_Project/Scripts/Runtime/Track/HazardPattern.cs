using System;
using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>
    /// A hand-made stretch of road (roadworks zone, police stop, oncoming car...). The TrackGenerator places it
    /// as a whole instead of random obstacles. Positions are metres: x across the road (0 = centre line,
    /// + = right; on the street the road is ±4 and sidewalks reach ±8, on a boulevard ±8 and ±12, see
    /// <see cref="roadHalfWidth"/>), z from the start of the pattern. Every pattern must keep a
    /// straight lane free of obstacles you cannot jump (HazardPatternTests checks this).
    /// </summary>
    [CreateAssetMenu(menuName = "CityScooter/Hazard Pattern", fileName = "HZ_")]
    public sealed class HazardPattern : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public GameObject prefab;
            [Tooltip("Used instead of prefab when the pattern is mirrored (signs with arrows or text).")]
            public GameObject mirrorPrefab;
            public float x;
            public float z;
            [Tooltip("Yaw in degrees (180 = facing the rider).")]
            public float yaw;
        }

        public Entry[] entries = Array.Empty<Entry>();
        [Tooltip("Metres of road this pattern owns: no random obstacles here.")]
        public float length = 60f;
        [Range(0, 9)] public int minLevel;
        public float weight = 1f;
        [Tooltip("Randomly mirror left/right. Off for traffic: oncoming cars must stay in the right-hand lanes.")]
        public bool allowMirror = true;
        [Header("Optional fake turn when the rider reaches turnAtZ")]
        [Range(-1, 1)] public int turnDirection;
        public float turnAtZ;
        [Range(0f, 1f)] public float turnStrength = 0.8f;
        public float turnSeconds = 4f;
        [Header("Moving entries meet the rider about here (z)")]
        public float meetAtZ = 40f;
        [Header("Road the positions were authored for")]
        [Tooltip("Half width of that road (4 = normal street, 8 = 4-lane boulevard). On a wider road, entries beyond it " +
                 "(signs, people on the sidewalk) move out with the kerb; patterns never go on a narrower road.")]
        public float roadHalfWidth = 4f;
    }
}
