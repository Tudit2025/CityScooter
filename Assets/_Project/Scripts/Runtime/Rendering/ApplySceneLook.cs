using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityScooter.Rendering
{
    /// <summary>
    /// Applies a <see cref="SceneLook"/>: RenderSettings (sky, gradient ambient, linear fog), the sun light, shader
    /// globals (fog/horizon colour for the sky shader, haze colour for the skyline shader) and the skyline textures of
    /// any <see cref="SkylineParallax"/> in the scene. Runs in edit mode too, so the scene view shows the look while
    /// you tune the asset. Put exactly one in a scene (the Lighting_* prefabs carry it) and delete any other
    /// directional light.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("CityScooter/Rendering/Apply Scene Look")]
    public sealed class ApplySceneLook : MonoBehaviour
    {
        /// <summary>Global fog colour (alpha 1 = set); the procedural sky can use it as its horizon colour.</summary>
        public static readonly int FogColorId = Shader.PropertyToID("_CS_FogColor");

        /// <summary>Global colour skyline layers fade towards (alpha 1 = set).</summary>
        public static readonly int HazeColorId = Shader.PropertyToID("_CS_HazeColor");

        static ApplySceneLook s_Active;

        /// <summary>The look of the most recently applied, still enabled instance (null when there is none).</summary>
        public static SceneLook ActiveLook => s_Active != null && s_Active.isActiveAndEnabled ? s_Active.look : null;

        /// <summary>Raised after a look has been applied or removed (skyline rigs listen to swap their textures).</summary>
        public static event Action<SceneLook> LookApplied;

        [Tooltip("The look to apply (sky, ambient, fog, sun, skyline). Swap the asset to change the whole place and mood.")]
        public SceneLook look;

        [Tooltip("The sun. Empty = first directional Light in children, else RenderSettings.sun.")]
        public Light sun;

        [Tooltip("Rotate the sun from the look. Turn off to aim the light by hand.")]
        public bool applySunRotation = true;

        void OnEnable()
        {
            SceneLook.Changed += OnLookAssetChanged;
            Apply();
            WarnAboutExtraDirectionalLights();
        }

        void OnDisable()
        {
            SceneLook.Changed -= OnLookAssetChanged;
            if (s_Active != this) return;
            s_Active = null;
            LookApplied?.Invoke(null);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // RenderSettings and DynamicGI must not be touched inside OnValidate: apply on the next editor tick.
            UnityEditor.EditorApplication.delayCall -= DelayedApply;
            UnityEditor.EditorApplication.delayCall += DelayedApply;
        }
#endif

        void DelayedApply()
        {
            if (this != null && isActiveAndEnabled) Apply();
        }

        void OnLookAssetChanged(SceneLook changed)
        {
            if (changed != look || !isActiveAndEnabled) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= DelayedApply;
            UnityEditor.EditorApplication.delayCall += DelayedApply;
#else
            Apply();
#endif
        }

        /// <summary>Push the look into RenderSettings, the sun, the shader globals and skyline rigs.</summary>
        public void Apply()
        {
            if (look == null) return;
#if UNITY_EDITOR
            // Prefab mode / editor tools: RenderSettings belong to the active scene, not to this preview scene.
            if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene)) return;
#endif

            if (look.skyboxMaterial != null) RenderSettings.skybox = look.skyboxMaterial;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = look.ambientSky;
            RenderSettings.ambientEquatorColor = look.ambientEquator;
            RenderSettings.ambientGroundColor = look.ambientGround;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = look.reflectionIntensity;

            RenderSettings.fog = look.fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = look.fogColor;
            RenderSettings.fogStartDistance = look.fogStart;
            RenderSettings.fogEndDistance = Mathf.Max(look.fogEnd, look.fogStart + 1f);

            // Alpha 1 tells the shaders the globals are valid.
            Color fog = look.fogColor;
            fog.a = 1f;
            Shader.SetGlobalColor(FogColorId, fog);
            Color haze = look.skylineHazeColor;
            haze.a = 1f;
            Shader.SetGlobalColor(HazeColorId, haze);

            Light s = ResolveSun();
            if (s != null)
            {
                ApplySun(s, look, applySunRotation);
                RenderSettings.sun = s;
            }

            // Refresh the ambient probe and the default reflection cubemap from the new sky.
            DynamicGI.UpdateEnvironment();

            s_Active = this;
            LookApplied?.Invoke(look);
        }

        /// <summary>Copy the look's sun settings onto a light (also used by the editor tools that build prefabs).</summary>
        public static void ApplySun(Light light, SceneLook look, bool rotate)
        {
            if (light == null || look == null) return;
            light.useColorTemperature = false; // the look's colour is the whole story
            light.color = look.sunColor;
            light.intensity = look.sunIntensity;
            light.shadowStrength = look.shadowStrength;
            if (rotate) light.transform.rotation = look.SunRotation;
        }

        Light ResolveSun()
        {
            if (sun != null) return sun;
            foreach (Light l in GetComponentsInChildren<Light>(true))
            {
                if (l.type == LightType.Directional) return l;
            }
            return RenderSettings.sun;
        }

        void WarnAboutExtraDirectionalLights()
        {
#if UNITY_EDITOR
            if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene)) return;
#endif
            int count = 0;
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional && l.isActiveAndEnabled) count++;
            }
            if (count > 1)
            {
                Debug.LogWarning("[CityScooter] " + count + " directional lights are enabled. Keep only the sun from the "
                    + "Lighting prefab: every extra one is lit as an additional light and shading no longer matches the look.", this);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Active = null;
            LookApplied = null;
        }
    }
}
