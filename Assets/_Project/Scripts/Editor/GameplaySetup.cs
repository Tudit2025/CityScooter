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
