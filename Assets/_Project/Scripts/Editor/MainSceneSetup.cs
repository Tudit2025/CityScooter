using CityScooter.Gameplay;
using CityScooter.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CityScooter.EditorTools
{
    /// <summary>
    /// Wires the look-dev output into Main.unity and the GameSystems prefab. Idempotent: only adds what is missing.
    /// Lighting rig (replaces the template's plain directional light), skyline rig, speed lines, quality auto-tuner,
    /// camera far plane / post-processing, crash + gift effects and the rider's blob shadow.
    /// Batchmode: -executeMethod CityScooter.EditorTools.MainSceneSetup.ApplyBatch
    /// </summary>
    public static class MainSceneSetup
    {
        const string MainScene = "Assets/_Project/Scenes/Main.unity";
        const string GameSystemsPrefab = "Assets/_Project/Prefabs/Game/GameSystems.prefab";
        const string LightingPrefab = "Assets/_Project/Prefabs/Environment/Lighting_PerthAfternoon.prefab";
        const string SkylinePrefab = "Assets/_Project/Prefabs/Environment/SkylineRig.prefab";
        const string SpeedLinesPrefab = "Assets/_Project/Prefabs/VFX/VFX_SpeedLines.prefab";
        const string CrashEffect = "Assets/_Project/Prefabs/VFX/VFX_CrashPuff.prefab";
        const string GiftEffect = "Assets/_Project/Prefabs/VFX/VFX_GiftSparkle.prefab";
        const string BlobShadowPrefab = "Assets/_Project/Prefabs/VFX/BlobShadow.prefab";
        const float SkylineFarClip = 700f;

        [MenuItem("CityScooter/Gameplay/Apply look to Main (lighting, skyline, VFX)")]
        public static void Apply()
        {
            WireGameSystemsPrefab();

            Scene scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<ApplySceneLook>() == null)
            {
                foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light.type == LightType.Directional && !PrefabUtility.IsPartOfPrefabInstance(light)) Object.DestroyImmediate(light.gameObject);
                }
                AddPrefab(LightingPrefab, scene);
            }
            if (Object.FindAnyObjectByType<SkylineParallax>() == null) AddPrefab(SkylinePrefab, scene);
            if (Object.FindAnyObjectByType<SpeedLines>() == null) AddPrefab(SpeedLinesPrefab, scene);
            if (Object.FindAnyObjectByType<QualityAutoTuner>() == null)
            {
                var go = new GameObject("QualityAutoTuner");
                go.AddComponent<QualityAutoTuner>().waitForRunStarted = true; // measure gameplay, not the title screen
            }

            Camera cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.farClipPlane = Mathf.Max(cam.farClipPlane, SkylineFarClip);
                EditorUtility.SetDirty(cam);
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data != null)
                {
                    data.renderPostProcessing = false; // MSAA lives on the URP asset; no post AA passes on the Web
                    data.antialiasing = AntialiasingMode.None;
                    EditorUtility.SetDirty(data);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[CityScooter] Main wired: lighting, skyline, speed lines, quality tuner, camera far " + (cam != null ? cam.farClipPlane : 0f));
        }

        public static void ApplyBatch()
        {
            Apply();
            EditorApplication.Exit(0);
        }

        static void AddPrefab(string path, Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[CityScooter] Missing " + path);
                return;
            }
            PrefabUtility.InstantiatePrefab(prefab, scene);
        }

        static void WireGameSystemsPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GameSystemsPrefab);
            try
            {
                var game = root.GetComponent<GameController>();
                if (game.crashEffect == null) game.crashEffect = AssetDatabase.LoadAssetAtPath<GameObject>(CrashEffect);
                if (game.giftEffect == null) game.giftEffect = AssetDatabase.LoadAssetAtPath<GameObject>(GiftEffect);
                RiderController rider = root.GetComponentInChildren<RiderController>(true);
                if (rider != null && rider.GetComponentInChildren<BlobShadow>(true) == null)
                {
                    var blob = AssetDatabase.LoadAssetAtPath<GameObject>(BlobShadowPrefab);
                    if (blob != null) PrefabUtility.InstantiatePrefab(blob, rider.transform);
                }
                PrefabUtility.SaveAsPrefabAsset(root, GameSystemsPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
