using System;
using CityScooter.Game;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityScooter.Rendering
{
    /// <summary>
    /// Web safety net for weak lab PCs. Measures the average frame rate over the first seconds of play and, if it
    /// is too low, steps quality down once: <see cref="Tier.Reduced"/> (main-light shadows off, render scale 0.85),
    /// then <see cref="Tier.Low"/> (also render scale 0.7, half VFX density, MSAA off).
    /// It never edits the URP asset on disk: it instantiates a copy of the active asset, changes the copy and assigns
    /// it to QualitySettings.renderPipeline for the rest of the session (the real asset is put back when the
    /// application quits / play mode stops). Drop one into the gameplay scene. VFX scripts read <see cref="VfxDensity"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("CityScooter/Rendering/Quality Auto Tuner")]
    public sealed class QualityAutoTuner : MonoBehaviour
    {
        public enum Tier
        {
            /// <summary>Everything as authored.</summary>
            Full = 0,
            /// <summary>Main-light shadows off, lower render scale.</summary>
            Reduced = 1,
            /// <summary>Lowest render scale, half VFX density, MSAA off.</summary>
            Low = 2,
        }

        /// <summary>0..1 multiplier VFX should apply to particle counts / emission (1 = full).</summary>
        public static float VfxDensity { get; private set; } = 1f;

        public static Tier CurrentTier { get; private set; } = Tier.Full;

        /// <summary>True once a tier has been chosen this session.</summary>
        public static bool HasDecided { get; private set; }

        /// <summary>Raised whenever a tier is applied.</summary>
        public static event Action<Tier> TierChanged;

        [Header("Measuring")]
        [Min(0f), Tooltip("Seconds ignored after start (scene load and shader warm-up hitches).")]
        public float warmupSeconds = 2f;

        [Min(1f), Tooltip("Seconds of play averaged before deciding.")]
        public float measureSeconds = 5f;

        [Min(0.05f), Tooltip("Frames longer than this (tab switch, GC spike) are skipped, not averaged.")]
        public float ignoreFramesLongerThan = 0.25f;

        [Tooltip("Start measuring only when a run starts (GameEvents.RunStarted). Menus are lighter than gameplay.")]
        public bool waitForRunStarted;

        [Tooltip("Keep the decision for the whole session: later scene loads keep it instead of measuring again.")]
        public bool rememberForSession = true;

        [Header("Tiers")]
        [Tooltip("Average fps below this -> Reduced.")]
        public float reducedBelowFps = 45f;

        [Tooltip("Average fps below this -> Low.")]
        public float lowBelowFps = 30f;

        [Range(0.5f, 1f)] public float reducedRenderScale = 0.85f;
        [Range(0.5f, 1f)] public float lowRenderScale = 0.7f;
        [Range(0f, 1f)] public float lowVfxDensity = 0.5f;

        [Tooltip("Low tier also turns MSAA off.")]
        public bool lowDisablesMsaa = true;

        [Header("Testing")]
        [Tooltip("Skip measuring and apply 'Forced Tier' right away (to preview tiers in the editor).")]
        public bool forceTier;
        public Tier forcedTier = Tier.Reduced;

        enum State { Waiting, Measuring, Done }

        // The session owns at most one runtime copy of the URP asset, whatever happens to scenes and instances.
        static UniversalRenderPipelineAsset s_RuntimeAsset;
        static RenderPipelineAsset s_OriginalAsset;
        static bool s_Swapped;

        State _state;
        float _elapsed;
        float _sampleTime;
        int _frames;
        int _skippedFrames;

        void OnEnable() => GameEvents.RunStarted += OnRunStarted;

        void OnDisable() => GameEvents.RunStarted -= OnRunStarted;

        void Start()
        {
            if (forceTier)
            {
                Decide(forcedTier, "forced in the inspector");
                return;
            }
            if (rememberForSession && HasDecided)
            {
                Debug.Log("[CityScooter] QualityAutoTuner keeps tier " + CurrentTier + " chosen earlier this session.", this);
                _state = State.Done;
                return;
            }
            RestoreOriginalAsset(); // measure at full quality
            _state = waitForRunStarted ? State.Waiting : State.Measuring;
        }

        void OnRunStarted()
        {
            if (_state == State.Waiting) _state = State.Measuring;
        }

        void Update()
        {
            if (_state != State.Measuring) return;

            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            if (_elapsed < warmupSeconds) return;
            if (dt > ignoreFramesLongerThan)
            {
                _skippedFrames++;
                return;
            }
            _sampleTime += dt;
            _frames++;
            if (_sampleTime < measureSeconds) return;

            float fps = _frames / Mathf.Max(0.001f, _sampleTime);
            Tier tier = fps < lowBelowFps ? Tier.Low : fps < reducedBelowFps ? Tier.Reduced : Tier.Full;
            Decide(tier, "average " + fps.ToString("F1") + " fps (" + (1000f / Mathf.Max(0.001f, fps)).ToString("F1")
                + " ms) over " + _sampleTime.ToString("F1") + " s, " + _frames + " frames, " + _skippedFrames + " long frames skipped");
        }

        /// <summary>Forget the decision, return to full quality and measure again (e.g. from a dev menu).</summary>
        public void Remeasure()
        {
            RestoreOriginalAsset();
            HasDecided = false;
            CurrentTier = Tier.Full;
            VfxDensity = 1f;
            _elapsed = 0f;
            _sampleTime = 0f;
            _frames = 0;
            _skippedFrames = 0;
            _state = State.Measuring;
            TierChanged?.Invoke(Tier.Full);
        }

        void Decide(Tier tier, string reason)
        {
            HasDecided = true;
            _state = State.Done;
            CurrentTier = tier;
            VfxDensity = tier == Tier.Low ? lowVfxDensity : 1f;

            string actions = "nothing changed";
            RestoreOriginalAsset();
            if (tier != Tier.Full)
            {
                var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (source == null)
                {
                    actions = "active render pipeline is not URP, only VFX density changed";
                }
                else
                {
                    float scale = tier == Tier.Low ? lowRenderScale : reducedRenderScale;
                    bool msaaOff = tier == Tier.Low && lowDisablesMsaa;

                    UniversalRenderPipelineAsset copy = Instantiate(source);
                    copy.name = source.name + " (auto-tuned " + tier + ")";
                    copy.hideFlags = HideFlags.DontSave;
                    copy.shadowDistance = 0f; // shadow distance 0 = URP skips the main-light shadow pass
                    copy.renderScale = scale;
                    if (scale < 1f) copy.upscalingFilter = UpscalingFilterSelection.Linear; // cheapest upscale
                    if (msaaOff) copy.msaaSampleCount = 1;

                    s_OriginalAsset = QualitySettings.renderPipeline; // may be null: the level uses the default asset
                    s_RuntimeAsset = copy;
                    s_Swapped = true;
                    QualitySettings.renderPipeline = copy;

                    actions = "main-light shadows off, render scale " + scale.ToString("0.##")
                        + (msaaOff ? ", MSAA off" : "") + (VfxDensity < 1f ? ", VFX density " + VfxDensity.ToString("0.##") : "");
                }
            }

            Debug.Log("[CityScooter] QualityAutoTuner tier " + tier + " (" + reason + "): " + actions + ".", this);
            TierChanged?.Invoke(tier);
        }

        static void RestoreOriginalAsset()
        {
            if (!s_Swapped) return;
            if (QualitySettings.renderPipeline == s_RuntimeAsset) QualitySettings.renderPipeline = s_OriginalAsset;
            if (s_RuntimeAsset != null) Destroy(s_RuntimeAsset);
            s_RuntimeAsset = null;
            s_OriginalAsset = null;
            s_Swapped = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Needed when "Enter Play Mode Options" skips the domain reload.
            VfxDensity = 1f;
            CurrentTier = Tier.Full;
            HasDecided = false;
            TierChanged = null;
            s_RuntimeAsset = null;
            s_OriginalAsset = null;
            s_Swapped = false;
            // Stopping play mode raises Application.quitting: put the real asset back so the editor's quality
            // settings never keep a reference to the runtime copy.
            Application.quitting -= RestoreOriginalAsset;
            Application.quitting += RestoreOriginalAsset;
        }
    }
}
