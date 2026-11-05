using System.IO;
using CityScooter.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CityScooter.EditorTools
{
    /// <summary>
    /// Creates the gameplay data assets and the GameSystems prefab (only if missing, so hand edits survive),
    /// and drops one GameSystems instance into Main.unity if the scene has none.
    /// Batchmode: -executeMethod CityScooter.EditorTools.GameplaySetup.SetupBatch
    /// </summary>
    public static class GameplaySetup
    {
        const string DataDir = "Assets/_Project/Data";
        const string TuningPath = DataDir + "/GameTuning.asset";
        const string ThemePath = DataDir + "/Theme_PerthStreet.asset";
        const string PrefabDir = "Assets/_Project/Prefabs/Game";
        const string PrefabPath = PrefabDir + "/GameSystems.prefab";
        const string MainScene = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("CityScooter/Gameplay/Create GameSystems prefab + add to Main")]
        public static void Setup()
        {
            EnsurePlaceholderMaterial();
            GameTuning tuning = LoadOrCreate<GameTuning>(TuningPath);
            TrackTheme theme = LoadOrCreate<TrackTheme>(ThemePath);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Directory.CreateDirectory(PrefabDir);
                GameObject temp = GameSystemsFactory.Create(tuning, theme);
                prefab = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
                Object.DestroyImmediate(temp);
                Debug.Log("[CityScooter] Created " + PrefabPath);
            }

            var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<GameController>() == null)
            {
                PrefabUtility.InstantiatePrefab(prefab, scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[CityScooter] Added GameSystems to " + MainScene);
            }
            AssetDatabase.SaveAssets();
        }

        public static void SetupBatch()
        {
            Setup();
            EditorApplication.Exit(0);
        }

        const string PlaceholderMaterialPath = "Assets/_Project/Resources/" + PlaceholderTrack.PlaceholderMaterialName + ".mat";

        /// <summary>
        /// The grey-box placeholders tint copies of this URP Lit material. It lives in Resources/ so the shader
        /// ships in Web builds; BuildScript calls this before every build. Only created if missing.
        /// </summary>
        public static void EnsurePlaceholderMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(PlaceholderMaterialPath) != null) return;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                Debug.LogError("[CityScooter] URP Lit shader not found; cannot create " + PlaceholderMaterialPath);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(PlaceholderMaterialPath));
            var mat = new Material(lit) { name = PlaceholderTrack.PlaceholderMaterialName };
            mat.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(mat, PlaceholderMaterialPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[CityScooter] Created " + PlaceholderMaterialPath);
        }

        public static void EnsurePlaceholderMaterialBatch()
        {
            EnsurePlaceholderMaterial();
            EditorApplication.Exit(0);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log("[CityScooter] Created " + path);
            return asset;
        }
    }
}
