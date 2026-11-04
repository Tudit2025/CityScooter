using System;
using UnityEngine;

namespace CityScooter.Rendering
{
    /// <summary>
    /// One place + mood for a scene: sky material, gradient ambient light, linear fog, the sun and (optionally) the
    /// distant skyline textures. <see cref="ApplySceneLook"/> pushes it into RenderSettings, the sun light, shader
    /// globals and any <see cref="SkylineParallax"/> rig, in edit mode and at runtime.
    /// Colours are authored as shown in the colour picker (sRGB); Unity converts them for linear lighting.
    /// </summary>
    [CreateAssetMenu(menuName = "CityScooter/Scene Look", fileName = "Look_New")]
    public sealed class SceneLook : ScriptableObject
    {
        /// <summary>Raised when the asset is edited in the inspector, so open scenes update live.</summary>
        public static event Action<SceneLook> Changed;

        [Header("Sky")]
        [Tooltip("Skybox material using the CityScooter/Sky Gradient shader.")]
        public Material skyboxMaterial;

        [Header("Ambient light (gradient)")]
        [Tooltip("Light from above (blue sky fill on shadowed faces).")]
        public Color ambientSky = new Color(0.66f, 0.80f, 0.92f);
        [Tooltip("Light from the horizon (sideways fill).")]
        public Color ambientEquator = new Color(0.86f, 0.83f, 0.75f);
        [Tooltip("Light bounced up from the ground (underside of things).")]
        public Color ambientGround = new Color(0.61f, 0.55f, 0.45f);
        [Range(0f, 1f), Tooltip("Strength of sky reflections on shiny materials.")]
        public float reflectionIntensity = 0.5f;

        [Header("Fog (linear, metres from the camera)")]
        public bool fog = true;
        [Tooltip("Use the horizon colour of the skyline art so the far road melts into the skyline base " +
                 "(Perth #F4E3C5, Rockingham #EBECDC). The procedural sky uses it as its horizon colour too.")]
        public Color fogColor = new Color(0.957f, 0.890f, 0.773f);
        [Min(0f), Tooltip("Fog starts here. Keep obstacles within ~60 m crisp.")]
        public float fogStart = 40f;
        [Min(1f), Tooltip("Fully fogged here. Spawn new road chunks a bit before this so they fade in.")]
        public float fogEnd = 190f;

        [Header("Skyline")]
        [Tooltip("Skyline textures for this place (RGBA, tile horizontally). Empty = the SkylineRig keeps its own textures.")]
        public Texture2D skylineFar;
        public Texture2D skylineMid;
        public Texture2D skylineNear;
        [Range(0.3f, 1f), Tooltip("Horizon / waterline inside the skyline textures, as a fraction from the top " +
                                  "(Perth 0.845, Rockingham 0.705). The rig puts this line on the in-game horizon.")]
        public float skylineHorizonFromTop = 0.845f;
        [Tooltip("Colour the skyline layers fade towards when a layer's Haze is above 0 " +
                 "(the art's own atmospheric-perspective colour: Perth #D0E0E8, Rockingham #D4E4EC).")]
        public Color skylineHazeColor = new Color(0.816f, 0.878f, 0.910f);

        [Header("Sun (main directional light)")]
        public Color sunColor = new Color(1f, 0.95f, 0.86f);
        [Range(0f, 3f), Tooltip("No HDR on the Web: keep lit white surfaces below clipping (about 1.2-1.5).")]
        public float sunIntensity = 1.35f;
        [Tooltip("x: how steeply the light points down (degrees; 90 = straight down). " +
                 "y: heading (degrees; 0 = light travels along +Z, the riding direction; 180 = towards the camera).")]
        public Vector2 sunRotation = new Vector2(45f, 35f);
        [Range(0f, 1f)] public float shadowStrength = 0.7f;

        public Quaternion SunRotation => Quaternion.Euler(sunRotation.x, sunRotation.y, 0f);

        public bool HasSkyline => skylineFar != null || skylineMid != null || skylineNear != null;

        /// <summary>Skyline texture for a slot (0 far, 1 mid, 2 near); null when not set.</summary>
        public Texture2D GetSkyline(int slot)
        {
            switch (slot)
            {
                case 0: return skylineFar;
                case 1: return skylineMid;
                case 2: return skylineNear;
                default: return null;
            }
        }

        void OnValidate()
        {
            if (fogEnd < fogStart + 1f) fogEnd = fogStart + 1f;
            Changed?.Invoke(this);
        }
    }
}
