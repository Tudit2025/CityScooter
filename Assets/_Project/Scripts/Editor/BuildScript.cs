using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CityScooter.EditorTools
{
    /// <summary>
    /// One-click / batchmode Web build into Builds/Web.
    /// Batchmode: Unity.exe -batchmode -quit -projectPath . -executeMethod CityScooter.EditorTools.BuildScript.BuildWebBatch
    /// </summary>
    public static class BuildScript
    {
        public const string WebOutputPath = "Builds/Web";

        [MenuItem("CityScooter/Build Web (Builds/Web)")]
        public static void BuildWebMenu() => BuildWeb(development: false);

        [MenuItem("CityScooter/Build Web - development (F1 overlay)")]
        public static void BuildWebDevMenu() => BuildWeb(development: true);

        public static void BuildWebBatch()
        {
            bool dev = Environment.GetCommandLineArgs().Contains("-cityscooterDev");
            EditorApplication.Exit(BuildWeb(dev) ? 0 : 1);
        }

        public static bool BuildWeb(bool development)
        {
            GameplaySetup.EnsurePlaceholderMaterial(); // grey-box renders magenta in builds without it
            var outdatedUrp = UrpAssetCheck.Outdated();
            if (outdatedUrp.Count > 0)
            {
                Debug.LogError("[CityScooter] Build stopped: URP assets saved by an older URP: " + string.Join(", ", outdatedUrp)
                    + ". Building now would strip URP/Lit and the game would show no road or buildings. Run the menu"
                    + " CityScooter/Tools/Upgrade URP assets (or batchmode, without -quit: -executeMethod"
                    + " CityScooter.EditorTools.UrpAssetCheck.UpgradeBatch), commit the changed assets, then build.");
                return false;
            }
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[CityScooter] No enabled scenes in Build Settings.");
                return false;
            }

            // Kept in code so a stray Player Settings edit cannot break the classroom build.
            PlayerSettings.WebGL.template = "PROJECT:CityScooter";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true; // works on any static host; Vercel adds headers for speed
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = true;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = WebOutputPath,
                target = BuildTarget.WebGL,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log("[CityScooter] Web build " + summary.result + ": " + (summary.totalSize / 1048576f).ToString("F1")
                + " MB, " + summary.totalTime + ", errors " + summary.totalErrors);
            return summary.result == BuildResult.Succeeded;
        }
    }
}
