using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// The rider throws a punch with a real arm. The riding model is one mesh, so for the length of the punch it is
    /// swapped for the same rider without that arm (armlessRight / armlessLeft, generated with the hazard kit), and
    /// this two-part arm swings from the shoulder: the elbow straightens, the fist flies at the target, grows a
    /// little on impact (comic), and comes back to the handlebar. Lives under the rider model (model space).
    /// </summary>
    public sealed class PunchArm : MonoBehaviour
    {
        public Mesh armlessRight, armlessLeft;
        public Transform upperArm, forearm, fist;
        public float outSeconds = 0.07f, holdSeconds = 0.08f, backSeconds = 0.15f;

        // Rest pose of the right arm in model space (mirrored for the left), from the comic rider: shoulder, elbow, hand.
        static readonly Vector3 Shoulder = new Vector3(.28f, 1.52f, .13f), Elbow = new Vector3(.38f, 1.2f, .33f), Hand = new Vector3(.35f, 1.23f, .65f);

        MeshFilter[] _filters;
        Mesh _original;
        float _t = -1f, _side = 1f, _reach, _recoil;
        Vector3 _dir = Vector3.forward;

        public bool Busy => _t >= 0f;
        /// <summary>World position of the fist (for effects).</summary>
        public Vector3 Fist => fist != null ? fist.position : transform.position;

        void Awake() => Show(false);

        /// <summary>Punch with the arm on this side (+1 right, -1 left) towards a point in model space.</summary>
        public void Fire(float side, Vector3 modelTarget)
        {
            if (Busy) Restore();
            _side = side >= 0f ? 1f : -1f;
            Vector3 shoulder = Mirror(Shoulder);
            Vector3 d = modelTarget - shoulder;
            d.y = Mathf.Clamp(d.y, -0.7f, 0.3f);
            d.z = Mathf.Max(d.z, 0.3f);
            // Always a hook out to the side, so the swing reads from the chase camera behind the rider.
            d.x = _side * Mathf.Max(Mathf.Abs(d.x), 0.8f * d.z);
            _dir = d.normalized;
            _reach = Mathf.Clamp(d.magnitude, 1.25f, 1.75f); // comic: the arm stretches
            _recoil = 0f;
            SwapBody(true);
            Show(true);
            _t = 0f;
            Pose(0f);
        }

        /// <summary>Contact: the fist snaps onto the target (model space) at full stretch, holds, then comes back.</summary>
        public void Strike(Vector3 modelTarget)
        {
            if (!Busy) { Fire(_side, modelTarget); }
            Vector3 d = modelTarget - Mirror(Shoulder);
            d.y = Mathf.Clamp(d.y, -0.7f, 0.3f);
            d.z = Mathf.Max(d.z, 0.3f);
            d.x = _side * Mathf.Max(Mathf.Abs(d.x) * Mathf.Sign(d.x * _side), 0.5f * d.z);
            _dir = d.normalized;
            _reach = Mathf.Clamp(d.magnitude, 1.25f, 1.9f);
            _recoil = 0f;
            _t = outSeconds; // fully out now
            Pose(1f);
        }

        /// <summary>Hit something solid: pull back early.</summary>
        public void Recoil() { if (Busy && _recoil <= 0f) _recoil = 1f; }

        Vector3 Mirror(Vector3 v) => new Vector3(v.x * _side, v.y, v.z);

        void Update()
        {
            if (_t < 0f) return;
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            float total = outSeconds + holdSeconds + backSeconds, k;
            if (_recoil > 0f) { _recoil -= dt / backSeconds; k = Mathf.Max(0f, _recoil); if (k <= 0f) _t = total; }
            else if (_t < outSeconds) { float u = _t / outSeconds; k = 1f - (1f - u) * (1f - u); }
            else if (_t < outSeconds + holdSeconds) k = 1f;
            else k = 1f - Mathf.SmoothStep(0f, 1f, (_t - outSeconds - holdSeconds) / backSeconds);
            Pose(k);
            if (_t >= total) Restore();
        }

        void Pose(float k)
        {
            Vector3 s = Mirror(Shoulder);
            Vector3 elbow = Vector3.Lerp(Mirror(Elbow), s + _dir * (_reach * 0.48f) + Vector3.up * 0.05f, k);
            Vector3 hand = Vector3.Lerp(Mirror(Hand), s + _dir * _reach, k);
            Segment(upperArm, s, elbow);
            Segment(forearm, elbow, hand);
            if (fist != null)
            {
                fist.localPosition = hand;
                fist.localRotation = Quaternion.LookRotation(hand - elbow, Vector3.up);
                fist.localScale = Vector3.one * Mathf.Lerp(1f, 1.8f, k * k);
            }
        }

        static void Segment(Transform t, Vector3 from, Vector3 to)
        {
            if (t == null) return;
            Vector3 d = to - from;
            t.localPosition = from;
            t.localRotation = Quaternion.LookRotation(d.sqrMagnitude > 1e-6f ? d : Vector3.forward, Vector3.up);
            t.localScale = new Vector3(1f, 1f, Mathf.Max(0.01f, d.magnitude));
        }

        // The riding model (and its ink outline) without the punching arm, for the length of the punch.
        void SwapBody(bool armless)
        {
            if (_filters == null)
            {
                Transform body = transform.parent != null ? transform.parent.Find("RiderAndScooter") : null;
                _filters = body != null ? body.GetComponentsInChildren<MeshFilter>(true) : new MeshFilter[0];
                if (_filters.Length > 0) _original = _filters[0].sharedMesh;
            }
            Mesh mesh = armless ? (_side > 0f ? armlessRight : armlessLeft) : _original;
            if (mesh == null || _original == null) return;
            foreach (MeshFilter f in _filters) if (f != null && (f.sharedMesh == _original || f.sharedMesh == armlessRight || f.sharedMesh == armlessLeft)) f.sharedMesh = mesh;
        }

        void Restore()
        {
            _t = -1f;
            SwapBody(false);
            Show(false);
        }

        void OnDisable() { if (_t >= 0f) Restore(); }

        void Show(bool on)
        {
            foreach (Transform t in new[] { upperArm, forearm, fist })
                if (t != null) t.gameObject.SetActive(on);
        }
    }
}
