using System;
using CityScooter.Controls;
using CityScooter.Game;
using CityScooter.Protocol;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// The scooter: rolls forward on its own, steers sideways, jumps, brakes. Collision is a box swept over
    /// this frame's movement (no rigidbody), so fast riders cannot tunnel through thin railings.
    /// Airborne riders pass over obstacles with canJumpOver; anything else ends the run.
    /// </summary>
    public sealed class RiderController : MonoBehaviour
    {
        public GameTuning tuning;
        public TrackGenerator track;
        [Tooltip("Visual child that leans; built as a grey-box rider when empty.")]
        public Transform model;
        public float radius = 0.35f;
        public float height = 1.6f;

        public float Speed { get; private set; }
        public float Distance => transform.position.z - _startZ;
        public bool Airborne { get; private set; }
        public bool Braking { get; private set; }
        public bool Running => _running;
        public float RunTime => _runTime;
        public float CurrentMaxSpeed => T.MaxSpeedAt(_runTime);
        public float SpeedGrowth01 => T.SpeedGrowth01(_runTime);
        /// <summary>0..9, what "spd:N" carries.</summary>
        public int SpeedLevel { get; private set; } = -1;

        public event Action<Obstacle> HitObstacle;
        public event Action<Gift> GotGift;

        readonly Collider[] _hits = new Collider[32];
        bool _running;
        float _startZ;
        float _vx;
        float _vy;
        float _y;
        float _runTime;

        GameTuning T => tuning != null ? tuning : GameTuning.Default;

        void Awake()
        {
            if (model == null) model = PlaceholderTrack.CreateRiderModel(transform).transform;
        }

        public void ResetRider(Vector3 start)
        {
            if (track != null) start.y = track.SurfaceHeightAt(start.x,start.z);
            transform.position = start;
            _startZ = start.z;
            Speed = 0f;
            _runTime = 0f;
            _vx = _vy = _y = 0f;
            Airborne = false;
            Braking = false;
            _running = false;
            if (model != null) model.localRotation = Quaternion.Euler(track != null ? track.SurfacePitchAt(start.z) : 0f, 0f, 0f);
            SetSpeedLevel(0);
        }

        public void Begin()
        {
            _running = true;
            _runTime = 0f;
            Speed = T.startSpeed;
        }

        public void Stop()
        {
            _running = false;
            Braking = false;
        }

        void Update()
        {
            if (!_running) return;
            float dt = Time.deltaTime;
            if(dt<=0)return;
            _runTime += dt;
            RiderInput input = RiderInputSource.Read();

            // Forward speed: ramps with distance, brake pulls it down, then it recovers.
            float target = Mathf.Lerp(T.startSpeed, CurrentMaxSpeed, T.Difficulty01(Distance));
            Braking = input.Brake;
            if (Braking) Speed = Mathf.Max(T.brakeMinSpeed, Speed - T.brakeDeceleration * dt);
            else Speed = Mathf.MoveTowards(Speed, target, T.acceleration * dt);

            // Sideways: eased towards the steer input, clamped to the lane.
            Vector3 from=transform.position;
            float nextZ=from.z+Speed*dt;
            float limit = Mathf.Max(0.5f, (track != null ? track.RideableHalfWidthAt(nextZ) : PlaceholderTrack.HalfWidth) - radius);
            float targetVx = input.Steer * T.lateralMaxSpeed;
            _vx = Mathf.Lerp(_vx, targetVx, 1f - Mathf.Exp(-T.lateralResponse * dt));
            FakeTurnDirector turn = FakeTurnDirector.Instance;
            float x=from.x+(_vx+(turn != null ? turn.Drift : 0f))*dt;
            if(x>limit){x=limit;_vx=Mathf.Min(_vx,0);}
            if(x<-limit){x=-limit;_vx=Mathf.Max(_vx,0);}

            // Jump arc.
            if (input.Jump && !Airborne && _runTime >= T.jumpLockAtStart)
            {
                Airborne = true;
                _vy = T.jumpVelocity;
            }
            if (Airborne)
            {
                _vy -= T.gravity * dt;
                _y += _vy * dt;
                // Crossing a curb in mid-air must not lift the entire jump by the curb height.
                if(track!=null)_y-=track.SidewalkOffsetAt(x,nextZ)-track.SidewalkOffsetAt(from.x,from.z);
                if (_y <= 0f)
                {
                    _y = 0f;
                    _vy = 0f;
                    Airborne = false;
                }
            }

            Vector3 to = new Vector3(x, _y + (track != null ? track.SurfaceHeightAt(x,nextZ) : 0f), nextZ);
            transform.position = to;

            float lean = -_vx / Mathf.Max(0.01f, T.lateralMaxSpeed) * T.maxLean - (turn != null ? turn.Amount * T.turnRiderLean : 0f);
            if (model != null) model.localRotation = Quaternion.Euler(track != null ? track.SurfacePitchAt(nextZ) : 0f, 0f, Mathf.Clamp(lean, -T.maxLean - T.turnRiderLean, T.maxLean + T.turnRiderLean));
            SetSpeedLevel(Mathf.RoundToInt(Speed / Mathf.Max(0.01f, CurrentMaxSpeed) * Messages.MaxSpeedLevel));
            Sweep(from, to);
        }

        void Sweep(Vector3 from, Vector3 to)
        {
            Vector3 centre = (from + to) * 0.5f + Vector3.up * (height * 0.5f);
            var half = new Vector3(radius + Mathf.Abs(to.x - from.x) * 0.5f,
                height * 0.5f + Mathf.Abs(to.y - from.y) * 0.5f, Mathf.Abs(to.z - from.z) * 0.5f + radius);
            int n = Physics.OverlapBoxNonAlloc(centre, half, _hits, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                Collider c = _hits[i];
                if (c == null || c.transform.IsChildOf(transform)) continue;
                Gift gift = c.GetComponentInParent<Gift>();
                if (gift != null)
                {
                    if (gift.gameObject.activeInHierarchy) GotGift?.Invoke(gift);
                    continue;
                }
                Obstacle obstacle = c.GetComponentInParent<Obstacle>();
                if (obstacle == null) continue;
                if (Airborne && obstacle.canJumpOver) continue;
                HitObstacle?.Invoke(obstacle);
                return;
            }
        }

        void SetSpeedLevel(int level)
        {
            level = Mathf.Clamp(level, 0, Messages.MaxSpeedLevel);
            if (level == SpeedLevel) return;
            SpeedLevel = level;
            GameEvents.RaiseSpeedLevel(level);
        }
    }
}
