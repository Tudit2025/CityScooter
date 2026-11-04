using System.Collections.Generic;
using System.Linq;
using CityScooter.Gameplay;
using CityScooter.Track;
using UnityEditor;
using UnityEngine;

namespace CityScooter.EditorTools
{
    /// <summary>
    /// Fills a TrackTheme from the standard prefab folders, so new art is picked up without hand-wiring:
    /// Prefabs/Road (RoadChunk), Prefabs/Obstacles (Obstacle; spansRoad ones become jump obstacles),
    /// Prefabs/Pickups (Gift). Re-run after adding or removing prefabs.
    /// Batchmode: -executeMethod CityScooter.EditorTools.ThemeTools.FillPerthStreetBatch
    /// </summary>
    public static class ThemeTools
    {
        const string PerthStreetTheme = "Assets/_Project/Data/Theme_PerthStreet.asset";

        [MenuItem("CityScooter/Gameplay/Fill Theme_PerthStreet from prefab folders")]
        public static void FillPerthStreet() =>
            Fill(PerthStreetTheme, "Assets/_Project/Prefabs/Road", "Assets/_Project/Prefabs/Obstacles", "Assets/_Project/Prefabs/Pickups");

        public static void FillPerthStreetBatch()
        {
            bool ok = FillPerthStreet_Checked();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool FillPerthStreet_Checked()
        {
            FillPerthStreet();
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(PerthStreetTheme);
            return theme != null && theme.HasChunks;
        }

        public static void Fill(string themePath, string roadDir, string obstacleDir, string pickupDir)
        {
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(themePath);
            if (theme == null)
            {
                Debug.LogError("[CityScooter] Theme not found: " + themePath);
                return;
            }
            List<Obstacle> obstacles = Load<Obstacle>(obstacleDir);
            theme.chunks = Load<RoadChunk>(roadDir).ToArray();
            theme.obstacles = obstacles.Where(o => !o.spansRoad).ToArray();
            theme.jumpObstacles = obstacles.Where(o => o.spansRoad).ToArray();
            theme.gifts = Load<Gift>(pickupDir).ToArray();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("[CityScooter] " + themePath + ": " + theme.chunks.Length + " chunks, " + theme.obstacles.Length
                + " obstacles, " + theme.jumpObstacles.Length + " jump obstacles, " + theme.gifts.Length + " gifts");
        }

        static List<T> Load<T>(string dir) where T : Component
        {
            var list = new List<T>();
            if (!AssetDatabase.IsValidFolder(dir)) return list;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { dir }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go != null && go.TryGetComponent(out T component)) list.Add(component);
            }
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list;
        }
    }
}
