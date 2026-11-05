using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CityScooter.EditorTools
{
    /// <summary>
    /// URP upgrades a pipeline asset saved by an older URP in memory only; until the asset is saved again, builds
    /// can use its old data. On 2026-11-05 (Unity 6.0 -> 6.3) an un-saved
    /// Mobile_RPAsset made shader stripping remove every URP/Lit forward variant: the Web build showed sky and
    /// skyline but no road, buildings or rider, while the build itself "succeeded".
    /// BuildScript refuses to build while any URP asset is outdated. Fix with the menu item, or in batchmode
    /// (without -quit): -executeMethod CityScooter.EditorTools.UrpAssetCheck.UpgradeBatch
    /// </summary>
    public static class UrpAssetCheck
    {
        // By name, so this editor assembly needs no URP reference.
        const string AssetType = "UniversalRenderPipelineAsset";
        const string VersionField = "k_AssetVersion";

        /// <summary>Paths (with versions) of URP assets under Assets/ that are older than the installed URP.</summary>
        public static List<string> Outdated()
        {
            var outdated = new List<string>();
            ScriptableObject fresh = ScriptableObject.CreateInstance(AssetType);
            if (fresh == null) return outdated; // URP not installed
            SerializedProperty latestProp = new SerializedObject(fresh).FindProperty(VersionField);
            int latest = latestProp != null ? latestProp.intValue : 0;
            Object.DestroyImmediate(fresh);

            // Read the version from the file on disk, not the loaded object: by the time an -executeMethod runs,
            // URP has already bumped the in-memory version, but the saved data (and the shader prefilter fields
            // stripping uses) stays old until the asset is saved by this URP.
            foreach (string guid in AssetDatabase.FindAssets("t:" + AssetType, new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Match m = Regex.Match(File.ReadAllText(path), @"^\s*" + VersionField + @":\s*(\d+)", RegexOptions.Multiline);
                int onDisk = m.Success ? int.Parse(m.Groups[1].Value) : 0;
                if (onDisk < latest) outdated.Add(path + " (v" + onDisk + " on disk, current v" + latest + ")");
            }
            return outdated;
        }

        [MenuItem("CityScooter/Tools/Upgrade URP assets")]
        public static void Upgrade() => UpgradeThen(null);

        /// <summary>Batchmode without -quit: upgrades, saves, exits 0 when everything is current.</summary>
        public static void UpgradeBatch() => UpgradeThen(ok => EditorApplication.Exit(ok ? 0 : 1));

        /// <summary>Batchmode: only reports; exits 1 if any URP asset is outdated.</summary>
        public static void CheckBatch()
        {
            List<string> outdated = Outdated();
            Debug.Log("[CityScooter] Outdated URP assets: " + (outdated.Count == 0 ? "none" : string.Join(", ", outdated)));
            EditorApplication.Exit(outdated.Count == 0 ? 0 : 1);
        }

        static void UpgradeThen(System.Action<bool> done)
        {
            var assets = new List<Object>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + AssetType, new[] { "Assets" }))
            {
                Object a = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
                if (a != null) assets.Add(a);
            }

            int frames = 0;
            void Tick()
            {
                if (++frames < 30) return; // URP's upgrader runs on a delayCall; give it a few editor frames
                EditorApplication.update -= Tick;
                foreach (Object a in assets) EditorUtility.SetDirty(a);
                AssetDatabase.SaveAssets();
                List<string> left = Outdated();
                if (left.Count == 0) Debug.Log("[CityScooter] URP assets are up to date (" + assets.Count + " checked).");
                else Debug.LogError("[CityScooter] URP assets still outdated: " + string.Join(", ", left));
                done?.Invoke(left.Count == 0);
            }
            EditorApplication.update += Tick;
        }
    }
}
