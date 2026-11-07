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

        Camera _cam;
        RiderController _rider;
        Quaternion _base = Quaternion.identity; // rotation before the fake-turn bank, so the bank never feeds back

        void Awake() { _cam = GetComponent<Camera>(); _base = transform.rotation; }

        public void SnapToTarget()
        {
            if (target == null) return;
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
            return new Vector3((directionalFollow?0:p.x*followX)+offset.x, Ground(p.z) + offset.y, p.z + offset.z);
        }

        float Ground(float z)
        {
            if (target!=null&&(_rider==null||_rider.transform!=target)) _rider = target.GetComponent<RiderController>();
            return _rider != null && _rider.track != null ? _rider.track.SurfaceHeightAt(z) : 0f;
        }

        Vector3 LookPoint()
        {
            Vector3 p = target.position;
            return new Vector3(p.x * followX, Ground(p.z + lookAhead) + lookHeight, p.z + lookAhead);
        }

        Quaternion DesiredRotation(Vector3 position)
        {
            if(!directionalFollow)return Quaternion.LookRotation(LookPoint()-position,Vector3.up);
            Vector3 p=target.position;
            Vector3 centre=new Vector3(offset.x,Ground(p.z+lookAhead)+lookHeight,p.z+lookAhead);
            Quaternion pitch=Quaternion.LookRotation(centre-position,Vector3.up);
            float yaw=Mathf.Clamp(Mathf.Atan2(p.x-offset.x,Mathf.Max(1,lookAhead-offset.z))*Mathf.Rad2Deg,-maxYaw,maxYaw);
            return Quaternion.AngleAxis(yaw,Vector3.up)*pitch;
        }

        void LateUpdate()
        {
            if (target == null) return;
            if (_rider == null) _rider = target.GetComponent<RiderController>();
            float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            Vector3 desired = Desired();
            // Forward follow is exact (no lag at high speed); sideways and height are smoothed.
            Vector3 pos = Vector3.Lerp(transform.position, desired, k);
            pos.z = desired.z;
            transform.position = pos;
            float turn=directionalFollow?1-Mathf.Exp(-rotationResponse*Time.deltaTime):k;
            _base = Quaternion.Slerp(_base, DesiredRotation(pos), turn);
            transform.rotation = WithTurn(_base);
            if (_cam != null && _rider != null)
            {
                float fov = Mathf.Min(maxFov, baseFov + _rider.Speed * fovPerMetrePerSecond);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, fov, k);
            }
        }
    }
}
