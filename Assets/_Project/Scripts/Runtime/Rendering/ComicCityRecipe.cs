using System;
using UnityEngine;

namespace CityScooter.Rendering
{
    [Serializable]
    public sealed class ComicAssetSpec
    {
        public string id;
        public string label;
        public string style;
        public float width = 1f, height = 1f, depth = 1f;
        public int floors = 2;
        public Color color = Color.white;
    }

    [CreateAssetMenu(menuName = "CityScooter/Comic City Recipe")]
    public sealed class ComicCityRecipe : ScriptableObject
    {
        public int schemaVersion = 7;
        [Header("Metres; road +Z, building fronts +Z, bottom-centre pivots")]
        [Tooltip("Authoring snap guideline; small attachments can use finer dimensions.")]
        public float grid = 0.5f;
        public float chunkLength = 40f;
        public float roadHalfWidth = 4f;
        public float sidewalkWidth = 4f;
        public float curbHeight = 0.2f;
        [Range(0.02f, 0.35f)] public float downhillGrade = 0.07867673f;
        public float openSetback = 5f;
        [Tooltip("Initial recipe default. Existing entries use their own height and floors fields.")]
        public float floorHeight = 3f;
        public int seed = 7301;
        [Header("Roadside small props (baseline: 22 per 40 m chunk)")]
        [Range(0f,1f)] public float roadsidePropDensity = 0.1f;
        [Range(1,16)] public int roadsideLayoutVariants = 10;
        [Range(0f,30f)] public float roadsideYawJitter = 14f;
        [Header("Chase camera; vertical FOV in degrees")]
        public Vector3 cameraOffset = new Vector3(0f, 3.38f, -5.6f);
        public float cameraLookAhead = 12f;
        public float cameraLookHeight = 0f;
        [Range(40f, 100f)] public float cameraFov = 76f;
        [Range(40f, 110f)] public float cameraMaxFov = 82f;
        public float cameraFovPerSpeed = 0.3f;
        public bool cameraDirectionalFollow = true;
        [Range(0,45)] public float cameraYawLimit = 22f;
        [Min(.1f)] public float cameraRotationResponse = 6f;
        [Header("Visual city curl; gameplay stays on the original slope")]
        [Min(20f)] public float cityBendStart = 32f;
        [Range(0f, 0.003f)] public float cityBendStrength = 0.0011f;
        [Header("Distant perspective convergence; visual only")]
        public float horizonTaperStart = 180f;
        public float horizonTaperEnd = 460f;
        [Range(.001f,.1f)] public float horizonMinimumScale = .002f;
        public float curveTransitionDistance = 180f;
        [Range(.05f,.3f)] public float horizonRiseSlope = .18f;
        public float curveEaseLength = 70f;
        [Header("Image skybox; equirectangular panorama")]
        public Texture2D skyPanorama;
        [Range(-180f,180f)] public float skyYaw = -28f;
        [Range(-20f,40f)] public float skyElevation = 12f;
        [Range(.2f,1.5f)] public float skyVerticalScale = .45f;
        [Tooltip("Apparent size around the forward sky horizon. Smaller values show more of the panorama without repeating it.")]
        [Range(.4f,1f)] public float skyApparentScale = .7f;
        [Range(0f,.5f)] public float skyHaze = .12f;
        [Header("Comic ink; pixel widths at 900 px image height")]
        public Color outlineColor = new Color(0.012f, 0.014f, 0.022f);
        [Range(0f, 4f)] public float outlinePixels = 1.8f;
        [Range(0f, 2f)] public float creasePixels = 0.65f;
        [Range(0f, 0.3f)] public float halftoneStrength = 0.12f;
        public Color shadowTint = new Color(0.36f, 0.40f, 0.53f);
        [Header("Shared palette")]
        public Color ink = new Color(0.10f, 0.15f, 0.22f);
        public Color asphalt = new Color(0.34f, 0.39f, 0.47f);
        public Color pavement = new Color(0.78f, 0.76f, 0.67f);
        public Color cream = new Color(0.98f, 0.89f, 0.67f);
        public Color glass = new Color(0.19f, 0.45f, 0.55f);
        public Color red = new Color(0.91f, 0.12f, 0.15f);
        public Color yellow = new Color(1f, 0.69f, 0.16f);
        public Color green = new Color(0.36f, 0.58f, 0.29f);
        [Header("Stable IDs are filenames. Change dimensions/colors here, then rebuild.")]
        public ComicAssetSpec[] props;
        public ComicAssetSpec[] buildings;
    }
}
