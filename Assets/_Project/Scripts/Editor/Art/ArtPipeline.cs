using UnityEditor;
using UnityEngine;

namespace CityScooter.EditorTools.Art
{
    /// <summary>
    /// CityScooter/Art/Rebuild All: Rebuild Kit Prefabs, then Rebuild Street Chunks, then Validate Budget.
    /// Batchmode: -executeMethod CityScooter.EditorTools.Art.ArtPipeline.RebuildAllBatch (exit code 1 on any error).
    /// </summary>
    public static class ArtPipeline
    {
        public const string MenuPath = "CityScooter/Art/Rebuild All (Kits + Chunks + Validate)";

        [MenuItem(MenuPath, priority = 99)]
        static void RebuildAllMenu() => RebuildAll();

        public static void RebuildAllBatch() => EditorApplication.Exit(RebuildAll() ? 0 : 1);

        public static bool RebuildAll()
        {
            bool kits = KitPrefabBuilder.Rebuild();
            bool chunks = kits && StreetChunkBuilder.Rebuild();
            ArtBudgetValidator.Report report = ArtBudgetValidator.Validate();
            bool ok = kits && chunks && report.Errors == 0;
            Debug.Log("[CityScooter] Rebuild All " + (ok ? "OK" : "FAILED") + " (kits " + kits + ", chunks " + chunks
                      + ", validator errors " + report.Errors + ", warnings " + report.Warnings + ").");
            return ok;
        }
    }
}
