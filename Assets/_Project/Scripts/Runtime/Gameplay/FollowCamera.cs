using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>Grounded chase camera with optional bounded yaw instead of lateral translation.</summary>
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 3.4f, -6.5f);
        [Tooltip("Metres ahead of the rider the camera looks at.")]
        public float lookAhead = 16f;
        [Tooltip("Height of the aim point above the road ahead.")]
        public float lookHeight = 1f;
        [Tooltip("How much of the rider's sideways movement the camera follows (0..1).")]
        [Range(0f, 1f)] public float followX = 0.6f;
        [Tooltip("Keep the camera on the street axis; track sideways movement with bounded yaw instead of translation.")]
        public bool directionalFollow;
        [Range(0,45)] public float maxYaw = 22f;
        [Min(.1f)] public float rotationResponse = 6f;
        public float smoothing = 8f;
        public float baseFov = 60f;
        public float fovPerMetrePerSecond = 0.5f;
        public float maxFov = 74f;
        [Header("Ramps and air (v0.3)")]
        [Tooltip("On ramps and ramp flights the camera rises by this share of the rider's height above the road (normal jumps: not at all).")]
        [Range(0f, 1f)] public float airFollow = 0.55f;
        [Tooltip("...and aims this share of that rise higher, so the flying rider stays in the picture.")]
        [Range(0f, 1f)] public float airLookFollow = 0.5f;
        [Tooltip("How quickly the camera rises with a flight, and settles after landing (per second).")]
        public float airRiseResponse = 2.5f;
        public float airSettleResponse = 3.5f;

        Camera _cam;
        RiderController _rider;
        Quaternion _base = Quaternion.identity; // rotation before the fake-turn bank, so the bank never feeds back
        float _shake;
        float _lift; // v0.3: smoothed rise with ramp flights
        float _pull, _dip, _dipVel; // Ramp feel (11/9): pull back during a ramp flight, dip at the landing
        int _landings = -1;

        /// <summary>Crash jolt: 0..1, fades out in about half a second.</summary>
        public void Shake(float strength) => _shake = Mathf.Max(_shake, Mathf.Clamp01(strength));

        void Awake() { _cam = GetComponent<Camera>(); _base = transform.rotation; }

        public void SnapToTarget()
        {
            if (target == null) return;
            _lift = LiftTarget();
            _pull = _dip = _dipVel = 0f;
            _landings = _rider != null ? _rider.Landings : -1;
            transform.position = Desired();
            _base = DesiredRotation(transform.position);
            transform.rotation = WithTurn(_base);
        }

        // Fake turns: look a little into the turn and bank the horizon (FakeTurnDirector).
        Quaternion WithTurn(Quaternion rotation)
        {
            FakeTurnDirector turn = FakeTurnDirector.Instance;
            if (turn == null || turn.Amount == 0f) return rotation;
            GameTuning t = turn.tuning != null ? turn.tuning : GameTuning.Default;
            return Quaternion.AngleAxis(turn.Amount * t.turnCameraLook, Vector3.up) * rotation * Quaternion.Euler(0f, 0f, -turn.Amount * t.turnCameraRoll);
        }

        Vector3 Desired()
        {
            Vector3 p = target.position;
            return new Vector3((directionalFollow?0:p.x*followX)+offset.x, Ground(p.z) + offset.y + _lift + _dip + _pull * 0.25f, p.z + offset.z - _pull);
        }

        float Ground(float z)
        {
            if (target!=null&&(_rider==null||_rider.transform!=target)) _rider = target.GetComponent<RiderController>();
            return _rider != null && _rider.track != null ? _rider.track.SurfaceHeightAt(z) : 0f;
        }

        Vector3 LookPoint()
        {
            Vector3 p = target.position;
            return new Vector3(p.x * followX, Ground(p.z + lookAhead) + lookHeight + _lift * airLookFollow + _dip * 0.6f, p.z + lookAhead);
        }

        Quaternion DesiredRotation(Vector3 position)
        {
            if(!directionalFollow)return Quaternion.LookRotation(LookPoint()-position,Vector3.up);
            Vector3 p=target.position;
            Vector3 centre=new Vector3(offset.x,Ground(p.z+lookAhead)+lookHeight+_lift*airLookFollow+_dip*0.6f,p.z+lookAhead);
            Quaternion pitch=Quaternion.LookRotation(centre-position,Vector3.up);
            float yaw=Mathf.Clamp(Mathf.Atan2(p.x-offset.x,Mathf.Max(1,lookAhead-offset.z))*Mathf.Rad2Deg,-maxYaw,maxYaw);
            return Quaternion.AngleAxis(yaw,Vector3.up)*pitch;
        }

        float LiftTarget() => _rider != null ? _rider.CameraLift * airFollow : 0f;

        /// <summary>How much the camera is raised for a ramp flight right now (tests).</summary>
        public float AirLift => _lift;
        /// <summary>Ramp feel: metres the camera is pulled back for a ramp flight, and dipped (negative) after a landing (tests).</summary>
        public float PullBack => _pull;
        public float LandingDip => _dip;

        // Ramp feel (11/9): pull back a little during a ramp flight (more of the arc in the picture), and dip at the landing,
        // springing back, deeper for a harder landing (GameTuning "Ramp feel").
        void UpdateRampFeel(float dt)
        {
            if (_rider == null) return;
            GameTuning t = _rider.tuning != null ? _rider.tuning : GameTuning.Default;
            float pull = _rider.BigAir && !_rider.Falling ? t.rampCameraPullBack : 0f;
            _pull = Mathf.Lerp(_pull, pull, 1f - Mathf.Exp(-(pull > _pull ? 3f : 1.6f) * dt));
            if (_rider.Landings != _landings)
            {
                if (_landings >= 0 && _rider.LastLandingBig)
                    _dipVel -= t.rampCameraLandDip * (0.3f + 0.7f * _rider.LastLandingHardness) * DipOmega * 1.4f;
                _landings = _rider.Landings;
            }
            const float damping = 0.45f;
            int n = Mathf.Clamp(Mathf.CeilToInt(dt / 0.008f), 1, 25);
            float h = dt / n;
            for (int i = 0; i < n; i++)
            {
                _dipVel += (-DipOmega * DipOmega * _dip - 2f * damping * DipOmega * _dipVel) * h;
                _dip += _dipVel * h;
            }
        }

        const float DipOmega = 2f * Mathf.PI * 2.2f;

        void LateUpdate()
        {
            if (target == null) return;
            if (_rider == null) _rider = target.GetComponent<RiderController>();
            float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            // Ramps and air (v0.3): rise part of the way with a flight, smoothly, and settle after landing.
            float lift = LiftTarget();
            _lift = Mathf.Lerp(_lift, lift, 1f - Mathf.Exp(-(lift > _lift ? airRiseResponse : airSettleResponse) * Time.deltaTime));
            UpdateRampFeel(Time.deltaTime);
            Vector3 desired = Desired();
            // Forward follow is exact (no lag at high speed); sideways and height are smoothed.
            Vector3 pos = Vector3.Lerp(transform.position, desired, k);
            pos.z = desired.z;
            transform.position = pos;
            float turn=directionalFollow?1-Mathf.Exp(-rotationResponse*Time.deltaTime):k;
            _base = Quaternion.Slerp(_base, DesiredRotation(pos), turn);
            transform.rotation = WithTurn(_base);
            if (_shake > 0.001f)
            {
                float t = Time.time * 40f;
                transform.position += transform.rotation * new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * (_shake * 0.6f);
                transform.rotation *= Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(t, 3.1f) - 0.5f) * _shake * 6f);
                _shake = Mathf.MoveTowards(_shake, 0f, Time.deltaTime * 2f);
            }
            if (_cam != null && _rider != null)
            {
                float fov = Mathf.Min(maxFov, baseFov + _rider.Speed * fovPerMetrePerSecond);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, fov, k);
            }
        }
    }
}
