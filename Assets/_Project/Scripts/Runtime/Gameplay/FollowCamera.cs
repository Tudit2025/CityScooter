using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>Chase camera: behind and above the rider, looking ahead; widens the view with speed.</summary>
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
        public float smoothing = 8f;
        public float baseFov = 60f;
        public float fovPerMetrePerSecond = 0.5f;
        public float maxFov = 74f;

        Camera _cam;
        RiderController _rider;

        void Awake() => _cam = GetComponent<Camera>();

        public void SnapToTarget()
        {
            if (target == null) return;
            transform.position = Desired();
            transform.rotation = Quaternion.LookRotation(LookPoint() - transform.position, Vector3.up);
        }

        Vector3 Desired()
        {
            Vector3 p = target.position;
            return new Vector3(p.x * followX + offset.x, Ground(p.z) + offset.y, p.z + offset.z);
        }

        float Ground(float z)
        {
            if (_rider == null && target != null) _rider = target.GetComponent<RiderController>();
            return _rider != null && _rider.track != null ? _rider.track.SurfaceHeightAt(z) : 0f;
        }

        Vector3 LookPoint()
        {
            Vector3 p = target.position;
            return new Vector3(p.x * followX, Ground(p.z + lookAhead) + lookHeight, p.z + lookAhead);
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
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(LookPoint() - pos, Vector3.up), k);
            if (_cam != null && _rider != null)
            {
                float fov = Mathf.Min(maxFov, baseFov + _rider.Speed * fovPerMetrePerSecond);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, fov, k);
            }
        }
    }
}
