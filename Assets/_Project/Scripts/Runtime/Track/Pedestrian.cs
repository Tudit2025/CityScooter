using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>
    /// A person on the street. Walkers stroll along the sidewalk; crossers wait at the kerb and walk over the road
    /// when the rider is a few seconds away (so they are mid-road on arrival, with a free lane on one side).
    /// The bell ("bl") makes people within reach step out of the rider's way. Walking is a cheap comic bob of the
    /// whole body (no skeleton). Hitting a person is a crash, never a knockdown.
    /// </summary>
    [DefaultExecutionOrder(-45)]
    [RequireComponent(typeof(Obstacle))]
    public sealed class Pedestrian : MonoBehaviour
    {
        public static readonly List<Pedestrian> Active = new List<Pedestrian>();
        /// <summary>Raised when the bell startles someone (for the "!" pop-up).</summary>
        public static event Action<Pedestrian> Startled;
        /// <summary>Set by the game: where the rider is and how fast it goes.</summary>
        public static Func<Vector3> RiderPosition;
        public static Func<float> RiderSpeed;

        [Tooltip("Metres per second along the road while strolling (+ = same way as the rider).")]
        public float walkSpeed = 1.2f;
        public bool crosses;
        public float crossSpeed = 1.7f;
        [Tooltip("Start crossing when the rider is this many seconds away.")]
        public float crossLeadSeconds = 2.4f;
        public float dodgeSpeed = 4.5f;
        public Transform art;

        enum State { Waiting, Crossing, Walking, Dodging }
        State _state;
        float _dir, _dodgeTargetX, _phase, _walk;

        public bool IsStartled => _state == State.Dodging;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Active.Clear(); Startled = null; RiderPosition = null; RiderSpeed = null; }

        void OnEnable()
        {
            Active.Add(this);
            _state = crosses ? State.Waiting : State.Walking;
            _walk = walkSpeed;
            _phase = UnityEngine.Random.value * 6f;
            if (art != null) { art.localPosition = Vector3.zero; art.localRotation = Quaternion.identity; }
        }

        void OnDisable() => Active.Remove(this);

        /// <summary>The bell: step away from the rider's line, to the nearer free side.</summary>
        public void Startle(float riderX)
        {
            if (_state == State.Dodging) return;
            float x = transform.localPosition.x;
            float side = Mathf.Abs(x - riderX) < 0.01f ? (x >= 0f ? 1f : -1f) : Mathf.Sign(x - riderX);
            // Off the road if possible: to the kerb on that side, or further out on the sidewalk.
            if (Mathf.Abs(x) > 4.2f) _dodgeTargetX = side * x > 0f ? side * 7.3f : side * 4.4f;
            else _dodgeTargetX = side * 4.9f;
            if (Mathf.Abs(_dodgeTargetX - x) < 1.2f) _dodgeTargetX = x + side * 1.2f;
            _state = State.Dodging;
            Startled?.Invoke(this);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 p = transform.localPosition;
            bool moving = true;
            switch (_state)
            {
                case State.Waiting:
                    moving = false;
                    if (RiderPosition != null)
                    {
                        float dz = transform.position.z - RiderPosition().z;
                        float speed = RiderSpeed != null ? Mathf.Max(5f, RiderSpeed()) : 10f;
                        if (dz > 0f && dz < speed * crossLeadSeconds + 6f) { _state = State.Crossing; _dir = p.x < 0f ? 1f : -1f; }
                    }
                    break;
                case State.Crossing:
                    p.x += _dir * crossSpeed * dt;
                    if (p.x * _dir > 5.6f) { _state = State.Walking; _walk = walkSpeed; }
                    break;
                case State.Walking:
                    p.z += _walk * dt;
                    moving = Mathf.Abs(_walk) > 0.01f;
                    break;
                case State.Dodging:
                    p.x = Mathf.MoveTowards(p.x, _dodgeTargetX, dodgeSpeed * dt);
                    if (Mathf.Abs(p.x - _dodgeTargetX) < 0.01f) { _state = State.Walking; _walk = 0f; }
                    break;
            }
            if (p != transform.localPosition) { transform.localPosition = p; Mover.MovedThisFrame = true; }
            if (art == null) return;
            _phase += dt * (moving ? (_state == State.Dodging ? 16f : 9f) : 0f);
            float facing = _state == State.Crossing || _state == State.Dodging ? (_state == State.Crossing ? _dir : Mathf.Sign(_dodgeTargetX - p.x)) * -90f : (_walk > 0f ? 180f : 0f);
            art.localPosition = new Vector3(0f, moving ? Mathf.Abs(Mathf.Sin(_phase)) * 0.09f : 0f, 0f);
            art.localRotation = Quaternion.Euler(0f, facing, moving ? Mathf.Sin(_phase) * 5f : 0f);
        }
    }
}
