using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// What one scene/skin is built from. Leave lists empty and the generator uses grey-box placeholders,
    /// so gameplay works before art exists.
    /// </summary>
    [CreateAssetMenu(menuName = "CityScooter/Track Theme", fileName = "Theme_")]
    public sealed class TrackTheme : ScriptableObject
    {
        [Tooltip("Road chunk prefabs (root has RoadChunk).")]
        public RoadChunk[] chunks;

        [Tooltip("Cycle chunks in array order to preserve authored near / open / near vista rhythm.")]
        public bool orderedChunks;

        [Tooltip("Single-lane obstacle prefabs (root has Obstacle).")]
        public Obstacle[] obstacles;

        [Tooltip("Road-wide obstacles you must jump (Obstacle.spansRoad), e.g. railings.")]
        public Obstacle[] jumpObstacles;

        [Tooltip("Pickup prefabs (root has Gift).")]
        public Gift[] gifts;

        public bool HasChunks => chunks != null && chunks.Length > 0;
    }
}
