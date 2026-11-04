using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CityScooter.EditorTools.LookDev
{
    /// <summary>
    /// Entry points that (re)generate everything look-dev: skyline art import, VFX sprites, materials, Scene Looks,
    /// environment + VFX prefabs and the LookDev scene. "Missing" only creates what does not exist yet, so the tech
    /// artist's tuning survives; the overwrite variants rebuild from code.
    /// Batchmode: Unity.exe -batchmode -projectPath . -executeMethod CityScooter.EditorTools.LookDev.LookDevBuilder.BuildBatch
    ///   [-overwrite] [-skylineSrc "path/to/art-src/skyline/out"] [-placeholdersTo "folder"]
    /// </summary>
    public static class LookDevBuilder
    {
        [MenuItem("CityScooter/Look Dev/Build Missing Assets", priority = 0)]
        static void BuildMissingMenu() => Build(false, false);

        [MenuItem("CityScooter/Look Dev/Rebuild VFX Prefabs (overwrite)", priority = 1)]
        static void RebuildVfxMenu()
        {
            LookDevTextures.GenerateVfxSprites(true);
            LookDevPrefabs.BuildVfx(true);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("CityScooter/Look Dev/Rebuild Everything (overwrites tuning)", priority = 2)]
        static void RebuildAllMenu()
        {
            if (!EditorUtility.DisplayDialog("Rebuild look-dev assets",
                    "Overwrites materials, Scene Looks, prefabs and LookDev.unity with the values in code. Tuning done by hand is lost.",
                    "Rebuild", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(true, true);
        }

        public static void BuildBatch()
        {
            int code = 0;
            try
            {
                bool overwrite = LookDevUtil.HasArg("-overwrite");
                Build(overwrite, overwrite);
                string placeholders = LookDevUtil.GetArg("-placeholdersTo");
                if (placeholders != null) LookDevTextures.WritePlaceholderSkylinesTo(placeholders);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        public static void Build(bool overwriteAssets, bool rebuildScene)
        {
            LookDevUtil.Log("build start (overwrite " + overwriteAssets + ")");
            SkylineArtImporter.Import(null);
            LookDevTextures.GenerateVfxSprites(overwriteAssets);
            LookDevMaterials.BuildAll(overwriteAssets);
            AssetDatabase.SaveAssets();
            LookDevPrefabs.BuildEnvironment(overwriteAssets);
            LookDevPrefabs.BuildVfx(overwriteAssets);
            AssetDatabase.SaveAssets();
            LookDevSceneBuilder.Build(rebuildScene);
            AssetDatabase.SaveAssets();
            LookDevUtil.Log("build done");
        }
    }
}
