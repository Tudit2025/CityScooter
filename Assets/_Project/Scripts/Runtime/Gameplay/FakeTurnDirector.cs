using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Fake turns: the road stays straight for physics, but the far city (beyond the bend start, 32 m) swings
    /// left or right in ComicWorldBend.hlsl, the panorama sky turns by an accumulated heading, the camera banks
    /// and the rider leans. Sets the global _ComicTurn (x: sideways metres per squared metre, + = right;
    /// y: sky heading radians). Nothing within 32 m of the rider moves, so what you see near you is what you hit.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class FakeTurnDirector : MonoBehaviour
    {
        public static FakeTurnDirector Instance { get; private set; }
        static readonly int TurnId = Shader.PropertyToID("_ComicTurn");

        public RiderController rider;
        public GameTuning tuning;

        /// <summary>-1 (full left) .. 1 (full right), eased.</summary>
        public float Amount { get; private set; }
        /// <summary>Accumulated heading in radians (+ = turned right), drives the sky.</summary>
        public float Heading { get; private set; }
        /// <summary>-1, 0 or 1 once a turn is clearly on (for the "turn:" message and HUD).</summary>
        public int Direction => Amount > 0.35f ? 1 : Amount < -0.35f ? -1 : 0;
        /// <summary>Sideways push on the rider in m/s (towards the outside of the turn).</summary>
        public float Drift { get; private set; }

        System.Random _rng = new System.Random(1);
        float _target;
        float _segmentLeft;
        bool _turning;
        float _velocity;
        float _forcedLeft;

        GameTuning T => tuning != null ? tuning : GameTuning.Default;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetGlobals() { Instance = null; Shader.SetGlobalVector(TurnId, Vector4.zero); }

        void OnEnable() { Instance = this; Restart(1); }
        void OnDisable()
        {
            if (Instance == this) Instance = null;
            Shader.SetGlobalVector(TurnId, Vector4.zero);
        }

        /// <summary>New run: straight start, then a fresh sequence of turns.</summary>
        public void Restart(int seed)
        {
            _rng = new System.Random(seed);
            _turning = false;
            _target = 0f;
            _forcedLeft = 0f;
            _segmentLeft = T.turnFirstStraight;
        }

        /// <summary>Scripted turn, e.g. after a "TAKE A DETOUR" sign: direction -1/1, strength 0..1.</summary>
        public void ForceTurn(int direction, float strength, float seconds)
        {
            _target = Mathf.Clamp(direction, -1, 1) * Mathf.Clamp01(strength);
            _turning = true;
            _forcedLeft = _segmentLeft = Mathf.Max(0.5f, seconds);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool running = rider != null && rider.Running;
            if (running && T.turnCurvature > 0f)
            {
                _segmentLeft -= dt;
                if (_forcedLeft > 0f) _forcedLeft -= dt;
                if (_segmentLeft <= 0f) NextSegment();
            }
            else _target = 0f;

            Amount = Mathf.SmoothDamp(Amount, _target, ref _velocity, Mathf.Max(0.05f, T.turnEaseSeconds), Mathf.Infinity, dt);
            if (Mathf.Abs(Amount) < 0.0005f && _target == 0f) Amount = 0f;
            float speed = rider != null ? rider.Speed : 0f;
            float speedFactor = Mathf.Clamp(speed / Mathf.Max(1f, T.turnReferenceSpeed), 0.5f, 1.6f);
            if (running) Heading = Mathf.Repeat(Heading + Amount * T.turnHeadingDegreesPerSecond * Mathf.Deg2Rad * speedFactor * dt + Mathf.PI, 2f * Mathf.PI) - Mathf.PI;
            int level = rider != null ? T.DifficultyLevel(rider.Distance) : 0;
            Drift = running && level >= T.turnDriftMinLevel ? -Amount * T.turnDrift * T.lateralMaxSpeed : 0f;
            Shader.SetGlobalVector(TurnId, new Vector4(Amount * T.turnCurvature, Heading, 0f, 0f));
        }

        void NextSegment()
        {
            if (_turning)
            {
                _turning = false;
                _target = 0f;
                _segmentLeft = Range(T.turnStraightSeconds);
                return;
            }
            _turning = true;
            float strength = Range(new Vector2(0.55f, 1f));
            _target = (_rng.NextDouble() < 0.5 ? -1f : 1f) * strength;
            _segmentLeft = Range(T.turnHoldSeconds);
        }

        float Range(Vector2 r) => Mathf.Lerp(r.x, Mathf.Max(r.x, r.y), (float)_rng.NextDouble());
    }
}
