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
        [Tooltip("Multiplier of the initial speed limit reached after speedRampSeconds. One disables the initial ramp.")]
        [Min(1f)] public float maxSpeedMultiplier = 1f;
        [Min(1f)] public float speedRampSeconds = 600f;
        [Tooltip("After the ramp, add this fraction of the initial limit each minute. Zero holds the reached limit.")]
        [Min(0)] public float postRampMultiplierPerMinute;
        [Tooltip("Distance in metres at which acceleration target reaches the current speed limit and obstacle density reaches its maximum.")]
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

        [Header("Punch and bell limits (sending pu / bl in a fast loop must not pay off)")]
        [Tooltip("Punches in a row before the rider is tired; one comes back every punchRechargeSeconds.")]
        [Min(1)] public int punchCharges = 3;
        public float punchRechargeSeconds = 1.2f;
        [Tooltip("Steering while a punch is out (one hand off the handlebar).")]
        [Range(0f, 1f)] public float punchSteerFactor = 0.3f;
        public int punchHitPoints = 15;
        [Tooltip("Points lost for a punch that hits nothing.")]
        public int punchMissPoints = 5;
        public float bellCooldown = 1f;
        [Tooltip("More than this many rings within bellSpamWindow seconds and people stop listening for bellIgnoreSeconds.")]
        public int bellSpamCount = 3;
        public float bellSpamWindow = 5f;
        public float bellIgnoreSeconds = 3f;
        [Tooltip("Cartoon mode: each person sent flying costs a gift; with no gifts, bumping into someone is a crash.")]
        public bool cartoonCostsGift = true;

        [Header("Boost (bo)")]
        public float boostSeconds = 2f;
        [Tooltip("Speed limit multiplier while boosting.")]
        public float boostFactor = 1.35f;
        [Tooltip("Seconds from one boost to the next; boosts sent earlier are ignored.")]
        public float boostCooldown = 6f;

        [Header("Street life")]
        [Tooltip("People on the sidewalks of every 40 m chunk (min, max). They are obstacles for riders on the sidewalk.")]
        public Vector2 sidewalkPeople = new Vector2(2f, 4f);
        [Range(0f, 1f)] public float sidewalkPeopleWalking = 0.65f;

        [Header("Hazard patterns (roadworks, police, traffic)")]
        [Tooltip("No hazard patterns in the first metres of a run.")]
        public float hazardStartDistance = 250f;
        [Range(0f, 1f)] public float hazardChanceStart = 0.35f;
        [Range(0f, 1f)] public float hazardChanceMax = 0.7f;
        [Tooltip("Free metres between two hazard patterns (random obstacles still appear there).")]
        public float hazardGap = 40f;
        [Tooltip("Moving cars appear when the rider is this many seconds (at least 160 m) from where they meet.")]
        public float moverLeadSeconds = 5f;

        [Header("Boulevard sections (4 lanes; the chunks are in TrackTheme)")]
        [Tooltip("Sometimes the street widens into a 4-lane boulevard: In chunk, some full-width chunks, Out chunk.")]
        public bool boulevardSections = true;
        [Tooltip("No boulevard in the first metres of a run.")]
        public float boulevardMinDistance = 600f;
        [Tooltip("Chance of a boulevard each time the street chunk cycle starts again (every 160 m with 4 ordered chunks).")]
        [Range(0f, 1f)] public float boulevardChance = 0.35f;
        [Tooltip("Full-width chunks between the In and Out chunks (min, max; 40 m each).")]
        public Vector2Int boulevardChunks = new Vector2Int(3, 6);
        [Tooltip("At least this many metres of normal street between two boulevards.")]
        public float boulevardGap = 480f;
        [Tooltip("Moving cars in the outer boulevard lanes keep this far from the kerb, so they merge in where the road narrows.")]
        public float moverKerbGap = 1.9f;

        [Header("Fake turns (visual; the road stays straight for physics)")]
        [Tooltip("Turns are stretches of road fixed in the world: they appear far ahead, come closer, and the sky turns while you ride through.")]
        public bool fakeTurns = true;
        [Tooltip("How far the road turns over a full-strength turn (degrees of heading).")]
        [Range(0f, 60f)] public float turnMaxDegrees = 30f;
        [Tooltip("The first turn starts this many metres into a run.")]
        public float turnFirstAt = 300f;
        [Tooltip("Straight road between turns: seconds of riding at the current speed (at least turnMinStraight metres).")]
        public Vector2 turnStraightSeconds = new Vector2(9f, 16f);
        public float turnMinStraight = 220f;
        [Tooltip("Length of one turn: seconds of riding at the current speed (at least turnMinLength metres).")]
        public Vector2 turnLengthSeconds = new Vector2(5f, 8f);
        public float turnMinLength = 160f;
        [Tooltip("Seconds for the camera bank and rider lean to follow into and out of a turn.")]
        public float turnEaseSeconds = 1.6f;
        [Tooltip("Camera bank and look into the turn, and rider lean, at a full turn (degrees).")]
        public float turnCameraRoll = 2.5f;
        public float turnCameraLook = 3f;
        public float turnRiderLean = 7f;
        [Tooltip("Push towards the outside of a turn as a fraction of lateralMaxSpeed (students must steer against it).")]
        [Range(0f, 0.5f)] public float turnDrift = 0.15f;
        [Range(0, 9)] public int turnDriftMinLevel = 3;

        [Header("WA police: wanted level and chase (attacking police adds a star)")]
        [Range(1, 5)] public int wantedMax = 5;
        [Tooltip("One punch or bump on the police counts once within this many seconds.")]
        public float policeAttackCooldown = 0.6f;
        [Tooltip("No new attack on the police for this long: lose a star.")]
        public float wantedDecaySeconds = 30f;
        [Tooltip("The police car gone, or at least escapeDistance metres behind, for escapeSeconds in total: lose a star.")]
        public float escapeDistance = 120f;
        public float escapeSeconds = 12f;
        [Tooltip("LOST THEM! bonus: this many points per star of the highest wanted level of the chase.")]
        public int lostBonusPerStar = 100;
        [Tooltip("Seconds from the first star until the police car turns up, and how far behind the rider it appears (m).")]
        public float copFirstDelay = 1.5f;
        public float copSpawnBehind = 80f;
        [Tooltip("How much faster than the rider the police car closes in at 1 star, plus this much for every extra star (m/s).")]
        public float copClosingSpeed = 3f;
        public float copClosingPerStar = 1.2f;
        [Tooltip("Sideways speed of the police car (m/s), plus this much per extra star; never above 75% of lateralMaxSpeed, so a sidestep always works.")]
        public float copLateralSpeed = 3f;
        public float copLateralPerStar = 0.4f;
        [Tooltip("Pulling alongside: metres to the side of the rider.")]
        public float copAlongsideOffset = 2.4f;
        [Tooltip("Ramming: the police car stops steering this many seconds before it would hit (the warning), then lunges this much faster (m/s).")]
        public float copLungeWarning = 1f;
        public float copLungeBoost = 4f;
        [Tooltip("After a spin-out the next police car comes this many seconds later, this much sooner per extra star (at least 4 s).")]
        public float copRespawnSeconds = 12.5f;
        public float copRespawnPerStar = 2f;
        [Tooltip("ARRESTED: slower than pullOverSpeed (m/s), or boxed in alongside, for this many seconds while chased.")]
        public float arrestSeconds = 1.5f;
        public float pullOverSpeed = 3f;
        [Tooltip("Boxed in: the police car within alongsideZ metres front/back and alongsideX (min, max) metres to the side.")]
        public float alongsideZ = 2.5f;
        public Vector2 alongsideX = new Vector2(1.2f, 3.6f);

        [Header("WA police helicopter: nets and the sniper's red dot")]
        [Range(1, 5)] public int heliMinStars = 3;
        [Range(1, 5)] public int sniperMinStars = 4;
        [Tooltip("Where it hovers: metres ahead of and above the rider (high enough to stay clear of the far road, where obstacles appear).")]
        public float heliAhead = 16f;
        public float heliHeight = 9f;
        [Tooltip("Seconds after the helicopter arrives before its first net (the sniper's red dot comes 1.5 s later).")]
        public float heliFirstShotSeconds = 2f;
        [Tooltip("Seconds from the target ring appearing to the net landing (never below 0.8 s), and the ring's radius (m). Caught under the net = ARRESTED.")]
        public float netWarnSeconds = 1.1f;
        public float netRadius = 1.3f;
        [Tooltip("Seconds between nets at heliMinStars, this much less per extra star (at least 1.6 s).")]
        public float netInterval = 3.6f;
        public float netIntervalPerStar = 0.6f;
        [Range(1, 2)] public int netsAtFiveStars = 2;
        [Tooltip("The red dot follows the rider this long, then locks (blinks) for sniperLockSeconds before the shot. A hit = BUSTED.")]
        public float sniperTrackSeconds = 2.4f;
        public float sniperLockSeconds = 0.7f;
        public float sniperLockAtFiveStars = 0.55f;
        public float sniperRadius = 0.55f;
        [Tooltip("How fast the red dot slides sideways after the rider (m/s), slower than the rider; seconds between two shots.")]
        public float sniperFollowSpeed = 4f;
        public float sniperInterval = 5f;

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
        public float SpeedGrowth01(float runSeconds) => maxSpeedMultiplier>1?Mathf.Clamp01(runSeconds/Mathf.Max(1,speedRampSeconds)):0;
        public float MaxSpeedAt(float runSeconds)
        {
            float multiplier=Mathf.Lerp(1,Mathf.Max(1,maxSpeedMultiplier),SpeedGrowth01(runSeconds));
            multiplier+=Mathf.Max(0,runSeconds-Mathf.Max(1,speedRampSeconds))/60*Mathf.Max(0,postRampMultiplierPerMinute);
            return Mathf.Max(startSpeed,maxSpeed)*multiplier;
        }

        public int DifficultyLevel(float distance) => Mathf.Clamp(Mathf.FloorToInt(Difficulty01(distance) * 9.999f), 0, 9);
    }
}
