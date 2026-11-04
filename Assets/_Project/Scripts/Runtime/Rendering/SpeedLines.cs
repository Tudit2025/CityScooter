using CityScooter.Game;
using CityScooter.Protocol;
using UnityEngine;

namespace CityScooter.Rendering
{
    /// <summary>
    /// Drives the VFX_SpeedLines particle system: thin streaks rushing past the camera, more of them the faster you go.
    /// Gameplay calls <see cref="SetSpeed01"/>, or leaves <see cref="followSpeedLevelEvents"/> on and the streaks follow
    /// GameEvents.SpeedLevelChanged (0..9). The emitter sticks to the camera, so the prefab can live anywhere.
    /// Emission is multiplied by <see cref="QualityAutoTuner.VfxDensity"/>.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    [DisallowMultipleComponent]
    [AddComponentMenu("CityScooter/Rendering/Speed Lines")]
    public sealed class SpeedLines : MonoBehaviour
    {
        [Range(0f, 1f), Tooltip("Normalised speed 0..1. Set by gameplay or by speed level events.")]
        public float speed01;

        [Min(0f), Tooltip("Streaks per second at full speed (before VFX density).")]
        public float maxRate = 45f;

        [Range(0f, 0.95f), Tooltip("No streaks below this speed.")]
        public float startAt = 0.3f;

        [Tooltip("How fast the streaks fly past (m/s) at the start speed and at full speed.")]
        public Vector2 streakSpeed = new Vector2(30f, 55f);

        [Min(0f), Tooltip("Seconds to ease towards a new speed.")]
        public float smoothTime = 0.25f;

        [Tooltip("Follow GameEvents.SpeedLevelChanged (0..9) so no extra wiring is needed.")]
        public bool followSpeedLevelEvents = true;

        [Header("Placement")]
        [Tooltip("Camera to stick to. Empty = Camera.main.")]
        public Camera targetCamera;

        [Tooltip("Emitter position in camera space (metres). The ring of streaks starts here and flies towards the camera.")]
        public Vector3 cameraOffset = new Vector3(0f, 0.5f, 22f);

        ParticleSystem _ps;
        float _current;
        float _velocity;

        /// <summary>The eased speed actually used for emission.</summary>
        public float CurrentSpeed01 => _current;

        public void SetSpeed01(float value) => speed01 = Mathf.Clamp01(value);

        void Awake() => _ps = GetComponent<ParticleSystem>();

        void OnEnable()
        {
            if (followSpeedLevelEvents) GameEvents.SpeedLevelChanged += OnSpeedLevel;
        }

        void OnDisable() => GameEvents.SpeedLevelChanged -= OnSpeedLevel;

        void OnSpeedLevel(int level) => SetSpeed01(level / (float)Messages.MaxSpeedLevel);

        void LateUpdate()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam != null)
            {
                Transform c = cam.transform;
                transform.SetPositionAndRotation(c.TransformPoint(cameraOffset), c.rotation * Quaternion.Euler(0f, 180f, 0f));
            }

            _current = smoothTime > 0f
                ? Mathf.SmoothDamp(_current, speed01, ref _velocity, smoothTime, Mathf.Infinity, Time.deltaTime)
                : speed01;
            ApplyEmission(_current);
        }

        /// <summary>Set emission for a speed right away (also used by editor previews).</summary>
        public void ApplyEmission(float speed)
        {
            if (_ps == null) _ps = GetComponent<ParticleSystem>();
            float t = Mathf.InverseLerp(startAt, 1f, speed);
            ParticleSystem.EmissionModule emission = _ps.emission;
            emission.rateOverTimeMultiplier = maxRate * t * t * QualityAutoTuner.VfxDensity;
            ParticleSystem.MainModule main = _ps.main;
            main.startSpeedMultiplier = Mathf.Lerp(streakSpeed.x, streakSpeed.y, t);
        }
    }
}
