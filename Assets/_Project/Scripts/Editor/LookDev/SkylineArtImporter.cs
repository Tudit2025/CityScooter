using System.IO;
using UnityEditor;

namespace CityScooter.EditorTools.LookDev
{
    /// <summary>
    /// Copies the Web-size skyline art (perth_/rockingham_ far/mid/near/sky "-2k" PNGs) from art-src/skyline/out into
    /// Assets/_Project/Art/Textures/Skyline/. Overwriting keeps the .meta (GUID and import settings), so re-running
    /// after the artist regenerates the art updates every look and the rig in place.
    /// Batchmode can pass another source folder: -skylineSrc "D:/path/to/art-src/skyline/out".
    /// </summary>
    static class SkylineArtImporter
    {
        [MenuItem("CityScooter/Look Dev/Import Skyline Art (art-src -> Art/Textures/Skyline)")]
        static void ImportMenu() => Import(null);

        /// <summary>Returns the number of files copied. Missing files are reported, not fatal.</summary>
        public static int Import(string sourceFolder)
        {
            string source = sourceFolder ?? LookDevUtil.GetArg("-skylineSrc") ?? Path.Combine(LookDevUtil.ProjectRoot, LookDevPaths.ArtSourceFolder);
            if (!Directory.Exists(source))
            {
                LookDevUtil.Warn("skyline art folder not found: " + source + " (looks keep their current textures)");
                return 0;
            }

            LookDevUtil.EnsureFolder(LookDevPaths.SkylineTextures);
            int copied = 0;
            foreach (string set in LookDevPaths.ArtSets)
            {
                foreach (string layer in LookDevPaths.ArtLayers)
                {
                    string assetPath = LookDevPaths.ArtTexture(set, layer);
                    string from = Path.Combine(source, Path.GetFileName(assetPath));
                    if (!File.Exists(from))
                    {
                        LookDevUtil.Warn("missing " + from);
                        continue;
                    }
                    File.Copy(from, LookDevUtil.ToFullPath(assetPath), true);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    copied++;
                }
            }
            LookDevUtil.Log("imported " + copied + " skyline textures from " + source);
            return copied;
        }
    }
}
