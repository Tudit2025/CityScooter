using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Every gameplay number in one asset, so designers (and later the "sandbox mode" that lets
    /// students change the rules) tweak data, not code. Units: metres, seconds.
    /// </summary>
    [CreateAssetMenu(menuName = "CityScooter/Game Tuning", fileName = "GameTuning")]
    public sealed class GameTuning : ScriptableObject
    {
        [Header("Speed (m/s)")]
        public float startSpeed = 9f;
        public float maxSpeed = 22f;
        [Tooltip("Distance in metres at which speed and obstacle density reach their maximum.")]
        public float difficultyDistance = 1500f;
        [Tooltip("How fast the scooter speeds back up after braking (m/s per second).")]
        public float acceleration = 3f;
        public float brakeDeceleration = 14f;
        public float brakeMinSpeed = 4f;

        [Header("Steering")]
        public float lateralMaxSpeed = 7f;
        [Tooltip("How quickly sideways speed follows the steer input (per second). Higher = twitchier.")]
        public float lateralResponse = 10f;
        [Tooltip("Visual lean of the rider model at full steer (degrees).")]
        public float maxLean = 18f;

        [Header("Jump")]
        public float jumpVelocity = 6.5f;
        public float gravity = 20f;
        [Tooltip("Jumps are ignored this long after a run starts (the start press must not also jump).")]
        public float jumpLockAtStart = 0.3f;

        [Header("Track content")]
        [Tooltip("No obstacles in the first metres of a run.")]
        public float safeStartDistance = 50f;
        [Range(0f, 1f)] public float obstacleChanceStart = 0.15f;
        [Range(0f, 1f)] public float obstacleChanceMax = 0.5f;
        [Range(0f, 1f)] public float giftChance = 0.25f;
        [Range(0f, 1f)] public float jumpObstacleChance = 0.5f;
        [Range(0, 9)] public int jumpObstacleMinLevel = 1;

        [Header("Score / flow")]
        public float pointsPerMetre = 1f;
        [Tooltip("Seconds after a crash before a press can start the next run.")]
        public float restartDelay = 1f;

        static GameTuning _default;

        /// <summary>Built-in defaults when no asset is assigned.</summary>
        public static GameTuning Default
        {
            get
            {
                if (_default == null) _default = CreateInstance<GameTuning>();
                return _default;
            }
        }

        public float Difficulty01(float distance) => Mathf.Clamp01(distance / Mathf.Max(1f, difficultyDistance));

        public int DifficultyLevel(float distance) => Mathf.Clamp(Mathf.FloorToInt(Difficulty01(distance) * 9.999f), 0, 9);
    }
}
