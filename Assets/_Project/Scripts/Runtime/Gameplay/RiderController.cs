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
    /// Airborne riders pass over obstacles with canJumpOver, and over anything whose collider top is below their feet;
    /// anything else ends the run.
    /// v0.3 ramps and air: the ground is the road plus the highest <see cref="Ramp"/> under the rider. Riding up a ramp
    /// lifts the rider; past the lip the rider flies (take-off speed upwards = speed x slope x launch, gravity x
    /// hangGravity) and lands on the road or on another ramp. On the ground inside a <see cref="RoadGap"/> the rider
    /// falls (<see cref="FellIntoGap"/>; RiderActions shows SPLASH! and ends the ride); flying over a gap is fine.
    /// Ramp feel (11/9, GameTuning "Ramp feel", maths in <see cref="RampFlight"/>): riding up costs speed and take-off uses
    /// the speed at the lip; A just before the lip pops (higher); the scooter pitches with the ramp and the flight path,
    /// squashes on bumps and landings and stretches at take-off; a hard landing costs speed, a deck coming down is soft.
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
        /// <summary>v1 cartoon mode: rode into a person, who goes flying instead of ending the run.</summary>
        public event Action<Obstacle> BouncedPerson;

        float _twist;
        /// <summary>1 normally; less while a punch is out (one hand off the handlebar). Set by RiderActions.</summary>
        public float SteerFactor { get; set; } = 1f;
        /// <summary>Punch: the body twists towards the side of the punch, then settles (visual only).</summary>
        public void PunchTwist(float side) => _twist = Mathf.Sign(side) * 22f;

        float _boostUntil = -1f, _boostReadyAt;
        public bool Boosting => _running && Time.time < _boostUntil;
        public bool BoostReady => Time.time >= _boostReadyAt;

        /// <summary>A short burst above the speed limit; false while it recharges.</summary>
        public bool Boost()
        {
            if (!_running || Time.time < _boostReadyAt) return false;
            _boostUntil = Time.time + T.boostSeconds;
            _boostReadyAt = Time.time + Mathf.Max(T.boostSeconds, T.boostCooldown);
            return true;
        }

        /// <summary>WA police: while chased, braking may slow right down to this (0 = stop, i.e. pull over); below 0 uses brakeMinSpeed.</summary>
        public float BrakeFloor { get; set; } = -1f;
        /// <summary>WA police: brake as if B were held ("sur", surrender).</summary>
        public bool HoldBrake { get; set; }

        /// <summary>Lose some speed (cartoon bumps).</summary>
        public void Bump(float keep) => Speed = Mathf.Max(T.brakeMinSpeed, Speed * Mathf.Clamp01(keep));

        // ---- Ramps and air (v0.3)

        /// <summary>A flight that counts (from a ramp, or any flight that lasts airMessageMinSeconds): "air:1" was sent,
        /// tricks work, the camera rises with the rider.</summary>
        public bool BigAir { get; private set; }
        /// <summary>Riding on a ramp right now (on the ground).</summary>
        public Ramp OnRamp => Airborne ? null : _onRamp;
        /// <summary>Height of the rider's feet above the road surface (on a ramp or in the air).</summary>
        public float HeightAboveRoad => _y;
        /// <summary>Seconds since the current flight took off (0 on the ground); the last flight's when it landed.</summary>
        public float AirTime { get; private set; }
        public float LastAirTime { get; private set; }
        /// <summary>Metres flown (forward) in the last flight.</summary>
        public float LastAirDistance { get; private set; }
        /// <summary>Fell into a road gap: dropping out of sight until the ride ends.</summary>
        public bool Falling => _gap != null;
        public RoadGap FallingInto => _gap;
        /// <summary>The camera may rise this much with the rider (ramp flights and ramp tops; not normal jumps or falls).</summary>
        public float CameraLift => Falling ? 0f : BigAir || (!Airborne && _onRamp != null) ? Mathf.Max(0f, _y) : 0f;
        /// <summary>Extra rotation of the rider model for tricks (spin, flip), around its middle. Set by RiderActions.</summary>
        public Quaternion TrickPose { get; set; } = Quaternion.identity;

        /// <summary>Took off (argument: a big-air flight, i.e. "air:1" was sent).</summary>
        public event Action<bool> TookOff;
        /// <summary>Landed safely (argument: it was a big-air flight; "air:0" was just sent).</summary>
        public event Action<bool> Landed;
        /// <summary>On the ground inside a road gap (or came down into one): the rider falls; the ride should end.</summary>
        public event Action<RoadGap> FellIntoGap;

        Ramp _onRamp;
        float _groundH, _gravityScale = 1f, _flightStartZ, _fallTime;
        RoadGap _gap;
        Vector3 _modelBasePos, _modelBaseScale = Vector3.one;
        static readonly Vector3 TrickPivot = new Vector3(0f, 0.85f, 0f);

        // ---- Ramp feel (11/9): slope physics, pose, suspension, pop, landing impact (numbers: GameTuning "Ramp feel")

        /// <summary>Pitch of the scooter relative to the road (degrees, nose up positive): the ramp under both wheels, the
        /// flight path in the air, smoothed. Tricks (TrickPose) turn the model on top of it.</summary>
        public float RampPitch => _pitch;
        /// <summary>Suspension: 0 at rest, up to 1 crouched (squashed), below 0 stretched (take-off).</summary>
        public float Crouch => _crouch;
        /// <summary>A pop is lined up: A was pressed just before the lip; the rider pops off it.</summary>
        public bool PopQueued => _popQueued;
        /// <summary>The last take-off was a pop (A pressed just before the lip).</summary>
        public bool LastTakeOffPopped { get; private set; }
        /// <summary>Take-off speed upwards of the last flight (m/s).</summary>
        public float LastLaunchVelocity { get; private set; }
        /// <summary>Last landing: speed into the surface (m/s), how hard it was (0 soft .. 1 hard), forward speed it cost
        /// (m/s; ramp flights only), and whether it ended a big-air flight.</summary>
        public float LastLandingImpact { get; private set; }
        public float LastLandingHardness { get; private set; }
        public float LastLandingSpeedLoss { get; private set; }
        public bool LastLandingBig { get; private set; }
        /// <summary>Landings so far (never reset; the camera watches it for the landing dip).</summary>
        public int Landings { get; private set; }

        float _pitch, _pitchVel, _crouch, _crouchVel, _lastSlope, _footSpeed, _speedBeforeLanding, _surgeUntil = -1f;
        bool _hadSlope, _onSlope, _popQueued;

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
            _modelBasePos = model.localPosition;
            _modelBaseScale = model.localScale;
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
            _twist = 0f;
            _boostUntil = -1f;
            _boostReadyAt = 0f;
            BrakeFloor = -1f;
            HoldBrake = false;
            ClearAir();
            if (model != null)
            {
                model.localRotation = Quaternion.Euler(track != null ? track.SurfacePitchAt(start.z) : 0f, 0f, 0f);
                model.localPosition = _modelBasePos;
                model.localScale = _modelBaseScale;
            }
            SetSpeedLevel(0);
        }

        // ---- Ramps and air (v0.3): back to plain riding on the road (new ride, tests).
        void ClearAir()
        {
            BigAir = false;
            _onRamp = null;
            _groundH = 0f;
            _gravityScale = 1f;
            _gap = null;
            _fallTime = 0f;
            AirTime = 0f;
            TrickPose = Quaternion.identity;
            // Ramp feel
            _pitch = _pitchVel = _crouch = _crouchVel = _lastSlope = _footSpeed = _speedBeforeLanding = 0f;
            _surgeUntil = -1f;
            _hadSlope = _onSlope = _popQueued = false;
            LastTakeOffPopped = false;
        }

        public void Begin()
        {
            _running = true;
            _runTime = 0f;
            Speed = T.startSpeed;
        }

        public void Stop()
        {
            // The ride ended in the air (crash, gap, police): close the flight for the boards ("air:0" before "crash").
            if (BigAir) { BigAir = false; GameEvents.RaiseAirChanged(false); }
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
            if (Falling) { UpdateFall(dt); return; }

            // Forward speed: ramps with distance, brake pulls it down, then it recovers.
            float target = Mathf.Lerp(T.startSpeed, CurrentMaxSpeed, T.Difficulty01(Distance));
            bool boosting = Time.time < _boostUntil;
            Braking = input.Brake || HoldBrake;
            if (Braking) Speed = Mathf.Max(BrakeFloor >= 0f ? BrakeFloor : T.brakeMinSpeed, Speed - T.brakeDeceleration * dt);
            else if (boosting) Speed = Mathf.MoveTowards(Speed, target * T.boostFactor, T.acceleration * 4f * dt);
            else if (_onSlope || (Airborne && BigAir)) { } // Ramp feel: on a ramp's slope and in a ramp flight, gravity and drag (below), not the motor
            else Speed = Mathf.MoveTowards(Speed, target, T.acceleration * (Speed > target ? (Time.time < _surgeUntil ? 0.35f : 2f) : 1f) * dt);

            // Sideways: eased towards the steer input, clamped to the lane.
            Vector3 from=transform.position;
            float nextZ=from.z+Speed*dt;
            float limit = Mathf.Max(0.5f, (track != null ? track.RideableHalfWidthAt(nextZ) : PlaceholderTrack.HalfWidth) - radius);
            float targetVx = input.Steer * T.lateralMaxSpeed * SteerFactor * (BigAir ? T.airSteerFactor : 1f);
            _vx = Mathf.Lerp(_vx, targetVx, 1f - Mathf.Exp(-T.lateralResponse * dt));
            FakeTurnDirector turn = FakeTurnDirector.Instance;
            float x=from.x+(_vx+(turn != null ? turn.Drift : 0f))*dt;
            if(x>limit){x=limit;_vx=Mathf.Min(_vx,0);}
            if(x<-limit){x=-limit;_vx=Mathf.Max(_vx,0);}

            float road = track != null ? track.SurfaceHeightAt(x, nextZ) : 0f;
            float ground = GroundAt(x, nextZ, road, dt, out Ramp ramp);

            // Jump arc (also from a ramp top: then it floats like a ramp flight).
            bool jump = input.Jump && !Airborne && _runTime >= T.jumpLockAtStart;
            // Ramp feel: A just before the lip lines up a pop (the rider pops off the lip, higher); earlier it is a normal jump.
            if (jump && _onRamp != null && InPopWindow(_onRamp, x, road, nextZ)) { _popQueued = true; jump = false; }
            if (jump)
                TakeOff(T.jumpVelocity, _onRamp != null ? HangOf(_onRamp) : 1f, false, from.z);
            else if (!Airborne && _onRamp != null && PastLip(_onRamp, x, road, nextZ))
            {
                // Rode off the lip: fly, whatever comes after it (the road, a gap, or a deck coming back down).
                // Take-off speed upwards comes from the speed at the lip (the climb has cost some: RampFlight).
                Ramp was = _onRamp;
                bool pop = _popQueued;
                TakeOff(RampFlight.LaunchVelocity(was, Speed, T) + (pop ? T.rampPopVelocity : 0f), HangOf(was), true, from.z, pop);
            }
            else if (!Airborne && _groundH > 0.02f && _groundH - ground > Speed * dt * 0.7f + 0.05f)
            {
                // The ground drops away faster than the rider can follow it (off a ramp's side): drop.
                // A gently falling surface (a deck coming back down: a Ramp turned round) is simply followed.
                TakeOff(0f, 1f, false, from.z);
            }

            if (Airborne)
            {
                AirTime += dt;
                // Ramp feel: a little air drag in a ramp flight (normal jumps keep their speed, as before).
                if (BigAir && !Braking && !boosting) Speed *= Mathf.Exp(-T.rampAirDrag * dt);
                _vy -= T.gravity * _gravityScale * dt;
                _y += _vy * dt;
                // Crossing a curb in mid-air must not lift the entire jump by the curb height.
                if(track!=null)_y-=track.SidewalkOffsetAt(x,nextZ)-track.SidewalkOffsetAt(from.x,from.z);
                if (_y <= ground && _vy <= 0f)
                {
                    RoadGap gap = ground <= 0.01f ? RoadGap.At(x, nextZ, road) : null;
                    if (gap != null) { StartFall(gap); }
                    else Land(ground, ramp, nextZ, x, road);
                }
            }
            else
            {
                // Ramp feel: riding up a ramp costs speed, riding down a deck gives some back (RampFlight.AfterClimb).
                if (ramp != null && _onRamp == null) _footSpeed = Speed;
                float climb = ground - _groundH;
                _onSlope = (ramp != null || _onRamp != null) && Mathf.Abs(climb) > 1e-4f;
                if (_onSlope) Speed = RampFlight.AfterClimb(Speed, climb, _footSpeed, target, T);
                _y = ground;
                _groundH = ground;
                _onRamp = ramp;
                if (ramp == null) _popQueued = false;
                RoadGap gap = ground <= 0.01f ? RoadGap.At(x, nextZ, road) : null;
                if (gap != null) StartFall(gap);
            }

            Vector3 to = new Vector3(x, _y + road, nextZ);
            transform.position = to;

            float lean = -_vx / Mathf.Max(0.01f, T.lateralMaxSpeed) * T.maxLean - (turn != null ? turn.Amount * T.turnRiderLean : 0f);
            _twist = Mathf.MoveTowards(_twist, 0f, 110f * dt);
            UpdatePose(dt, x, road, nextZ);
            float pitch = (track != null ? track.SurfacePitchAt(nextZ) : 0f) - _pitch;
            PoseModel(Quaternion.Euler(pitch, _twist, Mathf.Clamp(lean - _twist * 0.35f, -T.maxLean - T.turnRiderLean - 8f, T.maxLean + T.turnRiderLean + 8f)));
            SetSpeedLevel(Mathf.RoundToInt(Speed / Mathf.Max(0.01f, CurrentMaxSpeed) * Messages.MaxSpeedLevel));
            if (!Falling) Sweep(from, to);
        }

        // ---- Ramps and air (v0.3)

        float HangOf(Ramp r) => Mathf.Clamp(r.hangGravity * T.rampHangGravity, 0.1f, 2f);

        // Past the lip of a ramp that faces the way the rider goes (a ramp turned round is a deck going down: no lip).
        static bool PastLip(Ramp r, float x, float road, float z) =>
            r.transform.forward.z > 0.5f && r.transform.InverseTransformPoint(new Vector3(x, road, z)).z >= r.Length - 0.05f;

        // Height above the road of whatever the rider would stand on here: the highest ramp, or 0 (the road).
        // Riding into a ramp's side (or flying into it low) does not lift the rider up onto it.
        float GroundAt(float x, float z, float road, float dt, out Ramp ramp)
        {
            float h = Ramp.HighestAt(x, z, road, out ramp, out _);
            if (ramp == null || ramp == _onRamp) return h;
            float now = Airborne ? _y : _groundH;
            float step = Mathf.Max(0.35f, Speed * dt * 0.8f);
            if (h - now > step) { ramp = null; return 0f; }
            return h;
        }

        void TakeOff(float vy, float gravityScale, bool fromRampLip, float fromZ, bool pop = false)
        {
            Airborne = true;
            _vy = vy;
            _gravityScale = gravityScale;
            _y = _groundH;
            _flightStartZ = fromZ;
            AirTime = 0f;
            // Big air: every ramp flight, other flights only when they will last long enough (normal jumps do not).
            float g = Mathf.Max(0.1f, T.gravity * gravityScale);
            float predicted = (vy + Mathf.Sqrt(Mathf.Max(0f, vy * vy + 2f * g * Mathf.Max(0f, _y)))) / g;
            BigAir = fromRampLip || predicted >= T.airMessageMinSeconds;
            // Ramp feel: the legs push off (stretch), more for a pop; a normal hop stretches half as much.
            LastTakeOffPopped = pop;
            LastLaunchVelocity = vy;
            _popQueued = _onSlope = _hadSlope = false;
            Kick(-Mathf.Max(0f, vy) * T.rampStretchPerLaunch * (BigAir ? 1f : 0.5f));
            if (BigAir) GameEvents.RaiseAirChanged(true);
            TookOff?.Invoke(BigAir);
        }

        void Land(float ground, Ramp ramp, float z, float x, float road)
        {
            bool big = BigAir;
            // Ramp feel: impact = speed into the surface (a deck coming down meets the flight: soft). A hard landing from a
            // ramp flight costs speed (normal jumps: no), every landing makes the rider crouch.
            float slope = GroundSlope(x, z, road);
            float impact = Mathf.Max(0f, slope * Speed - _vy);
            LastLandingImpact = impact;
            LastLandingHardness = Mathf.InverseLerp(T.rampLandSoftImpact, Mathf.Max(T.rampLandSoftImpact + 0.1f, T.rampLandHardImpact), impact);
            LastLandingBig = big;
            _speedBeforeLanding = Speed;
            LastLandingSpeedLoss = big ? Speed * T.rampLandMaxLoss * LastLandingHardness : 0f;
            Speed -= LastLandingSpeedLoss;
            _footSpeed = Speed;
            Kick(impact * T.rampCrouchPerImpact);
            _lastSlope = slope;
            _hadSlope = true;
            _onSlope = false;
            Landings++;
            _y = ground;
            _vy = 0f;
            Airborne = false;
            _groundH = ground;
            _onRamp = ramp;
            _gravityScale = 1f;
            LastAirTime = AirTime;
            LastAirDistance = z - _flightStartZ;
            AirTime = 0f;
            BigAir = false;
            if (big) GameEvents.RaiseAirChanged(false);
            Landed?.Invoke(big);
        }

        void StartFall(RoadGap gap)
        {
            if (BigAir) { BigAir = false; GameEvents.RaiseAirChanged(false); }
            _gap = gap;
            _fallTime = 0f;
            // Airborne stays as it was: riding off the edge is not a jump (no jump sound), a flight that came down
            // here never "lands" (no landing sound).
            _onRamp = null;
            _groundH = 0f;
            _vy = Mathf.Min(_vy, 0f);
            _gravityScale = 1f;
            FellIntoGap?.Invoke(gap);
        }

        // Dropping into the gap: forward speed dies away, the rider sinks below the road (and stays inside the gap).
        void UpdateFall(float dt)
        {
            _fallTime += dt;
            Speed = Mathf.MoveTowards(Speed, 0f, Mathf.Max(6f, Speed * 2.5f) * dt);
            _vx = Mathf.Lerp(_vx, 0f, 1f - Mathf.Exp(-6f * dt));
            _vy -= T.gravity * dt;
            _y = Mathf.Max(-8f, _y + _vy * dt);
            Vector3 p = transform.position;
            float z = p.z + Speed * dt;
            if (_gap != null) z = Mathf.Min(z, Mathf.Max(p.z, _gap.EndZ - 0.8f));
            float x = p.x + _vx * dt;
            float road = track != null ? track.SurfaceHeightAt(x, z) : 0f;
            transform.position = new Vector3(x, road + _y, z);
            StepSuspension(dt);
            PoseModel(Quaternion.Euler(Mathf.Min(70f, 20f + _fallTime * 90f), _twist, 0f));
            SetSpeedLevel(Mathf.RoundToInt(Speed / Mathf.Max(0.01f, CurrentMaxSpeed) * Messages.MaxSpeedLevel));
        }

        // ---- Ramp feel (11/9): pop, pose and suspension

        /// <summary>Landed clean after a trick (RiderActions): the landing costs no speed, and a little extra for a moment.</summary>
        public void Stomp()
        {
            if (!_running || Airborne) return;
            Speed = Mathf.Max(Speed, _speedBeforeLanding) * (1f + T.rampStompBoost);
            LastLandingSpeedLoss = 0f;
            _surgeUntil = Time.time + 0.8f;
        }

        // On a ramp that faces the way the rider goes, at most rampPopWindow seconds before its lip.
        bool InPopWindow(Ramp r, float x, float road, float z)
        {
            if (r.transform.forward.z <= 0.5f) return false;
            float toLip = r.Length - r.transform.InverseTransformPoint(new Vector3(x, road, z)).z;
            return toLip >= -0.05f && toLip <= Mathf.Max(0.5f, Speed) * T.rampPopWindow;
        }

        // Slope of the ground (height above the road per metre forward) under the wheels, one wheelbase apart.
        float GroundSlope(float x, float z, float road)
        {
            float half = Mathf.Max(0.05f, T.rampWheelbase * 0.5f);
            return (SurfaceAt(x, z + half, road) - SurfaceAt(x, z - half, road)) / (2f * half);
        }

        // Height above the road under one wheel: the highest ramp there. Past the lip of the ramp being ridden the front
        // wheel is already over the edge, but the rider still stands on the ramp: the ramp's last stretch carries on (the
        // slope of a kicker, the flat top of a platform), so the nose does not dip at the lip.
        float SurfaceAt(float x, float z, float road)
        {
            float h = Ramp.HighestAt(x, z, road, out _, out _);
            Ramp r = _onRamp;
            if (!Airborne && r != null && r.transform.forward.z > 0.5f && z > r.LipZ)
            {
                float lip = r.WorldHeightAt(x, r.LipZ - 0.02f, road, out _);
                h = Mathf.Max(h, lip + (r.top > 0.01f ? 0f : r.Slope * (z - r.LipZ)));
            }
            return h;
        }

        // Pitch: with the ground under both wheels (tips up at a ramp's foot, stays up over the lip), with the flight path in
        // the air (nose up rising, level at the top, nose down falling), smoothed. The ground bending up under the wheels
        // (a ramp's foot) makes the rider crouch; bending down (a crest) lightens them.
        void UpdatePose(float dt, float x, float road, float z)
        {
            float target;
            if (Airborne)
            {
                float follow = T.rampAirPitchFollow * (BigAir ? 1f : 0.5f);
                target = Mathf.Clamp(Mathf.Atan2(_vy, Mathf.Max(4f, Speed)) * Mathf.Rad2Deg * follow, -T.rampAirPitchMax, T.rampAirPitchMax);
                _hadSlope = false;
            }
            else
            {
                float slope = GroundSlope(x, z, road);
                if (_hadSlope) Kick((slope - _lastSlope) * Speed * T.rampCrouchPerBump);
                _lastSlope = slope;
                _hadSlope = true;
                target = Mathf.Atan(slope) * Mathf.Rad2Deg;
            }
            _pitch = Mathf.SmoothDamp(_pitch, target, ref _pitchVel, Mathf.Max(0.01f, Airborne ? T.rampPitchSmoothAir : T.rampPitchSmoothGround), Mathf.Infinity, dt);
            StepSuspension(dt);
        }

        float SpringOmega => 2f * Mathf.PI * Mathf.Max(0.5f, T.rampSuspensionHz);

        // A push on the suspension: about this much crouch at its deepest (negative: stretch).
        void Kick(float crouch) => _crouchVel += crouch * SpringOmega * 1.4f;

        void StepSuspension(float dt)
        {
            float w = SpringOmega, d = T.rampSuspensionDamping;
            int n = Mathf.Clamp(Mathf.CeilToInt(dt / 0.008f), 1, 25);
            float h = dt / n;
            for (int i = 0; i < n; i++)
            {
                _crouchVel += (-w * w * _crouch - 2f * d * w * _crouchVel) * h;
                _crouch += _crouchVel * h;
            }
            if (_crouch > 1f) { _crouch = 1f; _crouchVel = Mathf.Min(_crouchVel, 0f); }
            if (_crouch < -0.6f) { _crouch = -0.6f; _crouchVel = Mathf.Max(_crouchVel, 0f); }
        }

        void PoseModel(Quaternion baseRotation)
        {
            if (model == null) return;
            // Squash and stretch about the feet (the riding model is one mesh): crouched = lower and a little wider.
            float s = _crouch * T.rampSquash;
            Vector3 squash = new Vector3(1f + s * 0.5f, 1f - s, 1f + s * 0.5f);
            model.localScale = Vector3.Scale(_modelBaseScale, squash);
            Quaternion trick = TrickPose;
            Vector3 pivot = Vector3.Scale(TrickPivot, squash);
            model.localRotation = baseRotation * trick;
            model.localPosition = _modelBasePos + baseRotation * (pivot - trick * pivot);
        }

        void Sweep(Vector3 from, Vector3 to)
        {
            Vector3 centre = (from + to) * 0.5f + Vector3.up * (height * 0.5f);
            var half = new Vector3(radius + Mathf.Abs(to.x - from.x) * 0.5f,
                height * 0.5f + Mathf.Abs(to.y - from.y) * 0.5f, Mathf.Abs(to.z - from.z) * 0.5f + radius);
            float feet = Mathf.Min(from.y, to.y);
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
                if (Time.time < PassThroughUntil) continue; // ---- Radio race (v0.4): just respawned, or past the finish
                if (Airborne && obstacle.canJumpOver) continue;
                // v0.3: flying (or riding a ramp) over something: clear when the feet are above its collider all frame.
                if (c.bounds.max.y <= feet + 0.02f) continue;
                if (obstacle.kind == ObstacleKind.Person && !obstacle.animal && RiderActions.CanBounce)
                {
                    BouncedPerson?.Invoke(obstacle);
                    continue;
                }
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

        // ---- Radio race (v0.4): in a race a crash is a 3 s stop, then the ride goes on from the same spot (RaceDirector).
        /// <summary>Obstacles are passed through (not hit) until this Time.time: just after a race respawn, past the finish.</summary>
        public float PassThroughUntil { get; set; } = -1f;

        /// <summary>
        /// Back on the scooter where the ride stopped (after falling into a road gap: just past the gap), in lane
        /// <paramref name="x"/>, at the start speed. The distance and the ride time carry on. Obstacles are passed through
        /// for <paramref name="passSeconds"/> so the thing that was hit cannot end it again.
        /// </summary>
        public void Respawn(float x, float passSeconds)
        {
            Vector3 p = transform.position;
            float z = _gap != null ? Mathf.Max(p.z, _gap.EndZ + 1f) : p.z;
            float limit = Mathf.Max(0.5f, (track != null ? track.RideableHalfWidthAt(z) : PlaceholderTrack.HalfWidth) - radius);
            x = Mathf.Clamp(x, -limit, limit);
            transform.position = new Vector3(x, track != null ? track.SurfaceHeightAt(x, z) : 0f, z);
            _vx = _vy = _y = 0f;
            Airborne = false;
            Braking = false;
            HoldBrake = false;
            BrakeFloor = -1f;
            _twist = 0f;
            ClearAir();
            if (model != null)
            {
                model.localRotation = Quaternion.Euler(track != null ? track.SurfacePitchAt(z) : 0f, 0f, 0f);
                model.localPosition = _modelBasePos;
            }
            Speed = T.startSpeed;
            _running = true;
            PassThroughUntil = Time.time + Mathf.Max(0f, passSeconds);
        }
        // ---- end Radio race (v0.4)
    }
}
