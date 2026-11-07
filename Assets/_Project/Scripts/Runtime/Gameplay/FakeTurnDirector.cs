using System.Collections.Generic;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Fake turns. The road stays straight for physics; ComicWorldBend.hlsl only bends what is drawn beyond the
    /// unaffected distance (32 m). A turn is a stretch of road fixed in the world (start z, end z, curvature), so
    /// it appears far ahead, comes closer like a real bend, and while the rider rides through it the sky turns and
    /// the camera banks; the rest of the turn ahead shrinks until the road ahead is straight again.
    /// Globals: _ComicTurn (curvature, start z, end z, sky heading) and _ComicTurn2 (the next turn).
    /// Nothing within 32 m of the rider moves, so what you see near you is what you hit.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class FakeTurnDirector : MonoBehaviour
    {
        public static FakeTurnDirector Instance { get; private set; }
        static readonly int TurnId = Shader.PropertyToID("_ComicTurn");
        static readonly int NextTurnId = Shader.PropertyToID("_ComicTurn2");
        static readonly int BendId = Shader.PropertyToID("_ComicWorldBend");
        const float PlanAhead = 650f;

        public struct Turn
        {
            public float Start, End, Curvature, Strength;
            public int Direction => Curvature > 0f ? 1 : Curvature < 0f ? -1 : 0;
        }

        public RiderController rider;
        public GameTuning tuning;

        /// <summary>-1 (full left) .. 1 (full right) while riding through a turn, eased; 0 on the straight.</summary>
        public float Amount { get; private set; }
        /// <summary>Accumulated heading in radians (+ = turned right), drives the sky.</summary>
        public float Heading { get; private set; }
        /// <summary>-1, 0 or 1 once a turn is clearly on (for the "turn:" message).</summary>
        public int Direction => Amount > 0.35f ? 1 : Amount < -0.35f ? -1 : 0;
        /// <summary>Sideways push on the rider in m/s (towards the outside of the turn).</summary>
        public float Drift { get; private set; }
        /// <summary>Turns ahead (and the one being ridden), nearest first.</summary>
        public IReadOnlyList<Turn> Planned => _plan;

        readonly List<Turn> _plan = new List<Turn>();
        System.Random _rng = new System.Random(1);
        float _nextStart = float.NaN;
        float _lastBend = float.NaN;
        float _velocity;

        GameTuning T => tuning != null ? tuning : GameTuning.Default;
        float RiderZ => rider != null ? rider.transform.position.z : 0f;
        float Speed => rider != null ? Mathf.Max(T.startSpeed, rider.Speed) : T.startSpeed;
        static float BendStart { get { float y = Shader.GetGlobalVector(BendId).y; return y > 1f ? y : 32f; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetGlobals()
        {
            Instance = null;
            Shader.SetGlobalVector(TurnId, Vector4.zero);
            Shader.SetGlobalVector(NextTurnId, Vector4.zero);
        }

        void OnEnable() { Instance = this; Restart(1); }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            Shader.SetGlobalVector(TurnId, Vector4.zero);
            Shader.SetGlobalVector(NextTurnId, Vector4.zero);
        }

        /// <summary>New run: straight start, sky back to its authored direction, a fresh sequence of turns.</summary>
        public void Restart(int seed)
        {
            _rng = new System.Random(seed);
            _plan.Clear();
            _nextStart = float.NaN;
            _lastBend = float.NaN;
            Heading = 0f;
            Amount = 0f;
            _velocity = 0f;
        }

        /// <summary>
        /// A turn starting at world z (e.g. after a "TAKE A DETOUR" sign). Planned turns it overlaps are dropped;
        /// if the rider is already riding through one of them, the request is ignored.
        /// </summary>
        public bool RequestTurn(float startZ, int direction, float strength, float length)
        {
            float bend = RiderZ + BendStart;
            startZ = Mathf.Max(startZ, bend);
            float endZ = startZ + Mathf.Max(30f, length);
            float gap = T.turnMinStraight * 0.5f;
            for (int i = _plan.Count - 1; i >= 0; i--)
            {
                Turn t = _plan[i];
                if (t.End + gap <= startZ || t.Start >= endZ + gap) continue;
                if (t.Start <= bend) return false;
                _plan.RemoveAt(i);
            }
            _plan.Add(Make(startZ, endZ, direction, strength));
            _plan.Sort((a, b) => a.Start.CompareTo(b.Start));
            float after = endZ + StraightLength();
            _nextStart = float.IsNaN(_nextStart) ? after : Mathf.Max(_nextStart, after);
            return true;
        }

        /// <summary>Starts a turn right away, at the bend start (tests).</summary>
        public void ForceTurn(int direction, float strength, float seconds) =>
            RequestTurn(RiderZ + BendStart, direction, strength, Mathf.Max(60f, Speed * seconds));

        Turn Make(float start, float end, int direction, float strength)
        {
            strength = Mathf.Clamp01(strength);
            float radians = strength * T.turnMaxDegrees * Mathf.Deg2Rad;
            return new Turn
            {
                Start = start, End = end, Strength = strength,
                Curvature = Mathf.Sign(direction) * radians / (2f * Mathf.Max(1f, end - start)),
            };
        }

        float StraightLength() => Mathf.Max(T.turnMinStraight, Speed * Range(T.turnStraightSeconds));
        float TurnLength() => Mathf.Max(T.turnMinLength, Speed * Range(T.turnLengthSeconds));
        float Range(Vector2 r) => Mathf.Lerp(r.x, Mathf.Max(r.x, r.y), (float)_rng.NextDouble());

        void Update()
        {
            bool running = rider != null && rider.Running;
            float bend = RiderZ + BendStart;
            if (running && T.fakeTurns) PlanAheadOf(bend);

            // Sky: turn by exactly the part of each turn ridden since last frame (heading change = 2 x curvature x metres).
            if (running && !float.IsNaN(_lastBend) && bend > _lastBend)
                foreach (Turn t in _plan)
                {
                    float ridden = Mathf.Clamp(bend, t.Start, t.End) - Mathf.Clamp(_lastBend, t.Start, t.End);
                    if (ridden > 0f) Heading = Mathf.Repeat(Heading + 2f * t.Curvature * ridden + Mathf.PI, 2f * Mathf.PI) - Mathf.PI;
                }
            _lastBend = bend;
            while (_plan.Count > 0 && _plan[0].End <= bend) _plan.RemoveAt(0);

            float target = _plan.Count > 0 && _plan[0].Start <= bend ? _plan[0].Direction * _plan[0].Strength : 0f;
            Amount = Mathf.SmoothDamp(Amount, target, ref _velocity, Mathf.Max(0.05f, T.turnEaseSeconds), Mathf.Infinity, Time.deltaTime);
            if (Mathf.Abs(Amount) < 0.0005f && target == 0f) Amount = 0f;
            int level = rider != null ? T.DifficultyLevel(rider.Distance) : 0;
            Drift = running && level >= T.turnDriftMinLevel ? -Amount * T.turnDrift * T.lateralMaxSpeed : 0f;

            Shader.SetGlobalVector(TurnId, _plan.Count > 0 ? new Vector4(_plan[0].Curvature, _plan[0].Start, _plan[0].End, Heading) : new Vector4(0f, 0f, 0f, Heading));
            Shader.SetGlobalVector(NextTurnId, _plan.Count > 1 ? new Vector4(_plan[1].Curvature, _plan[1].Start, _plan[1].End, 0f) : Vector4.zero);
        }

        // Keep turns planned well beyond the far fade, so each one first shows up in the distance.
        void PlanAheadOf(float bend)
        {
            if (float.IsNaN(_nextStart)) _nextStart = RiderZ + T.turnFirstAt;
            while (_plan.Count < 3)
            {
                float start = _nextStart;
                if (_plan.Count > 0) start = Mathf.Max(start, _plan[_plan.Count - 1].End + T.turnMinStraight);
                if (start > bend + PlanAhead) break;
                float length = TurnLength();
                // Mostly turn back towards the authored view, so the city skyline stays roughly ahead.
                float headingAfter = Heading;
                foreach (Turn t in _plan) headingAfter += 2f * t.Curvature * (t.End - Mathf.Max(t.Start, bend));
                int direction = _rng.NextDouble() < 0.5 ? -1 : 1;
                if (Mathf.Abs(headingAfter) > 15f * Mathf.Deg2Rad && _rng.NextDouble() < 0.8) direction = headingAfter > 0f ? -1 : 1;
                _plan.Add(Make(start, start + length, direction, Range(new Vector2(0.5f, 1f))));
                _nextStart = start + length + StraightLength();
            }
        }
    }
}
