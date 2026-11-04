using UnityEditor;
using UnityEngine;

namespace CityScooter.EditorTools.Art
{
    /// <summary>
    /// Normalises import settings for everything under Assets/_Project/Art/ThirdParty so dropped-in kits are
    /// Web-build friendly without anyone touching the importer inspector:
    /// models: real-world scale from <see cref="KitRules"/>, axis conversion baked (no -90 root rotations),
    /// no cameras/lights/animation, mesh compression, Read/Write off, no tangents/UV2;
    /// textures: palette-friendly (no mipmaps, clamp, compressed, max 1024).
    /// Settings are re-applied on every import: change <see cref="KitRules"/> instead of the inspector.
    /// The one shared URP material per kit is assigned by remapping (see <see cref="KitPrefabBuilder"/>),
    /// because creating assets inside an import callback is not allowed.
    /// </summary>
    public sealed class ThirdPartyArtPostprocessor : AssetPostprocessor
    {
        // Bump when the rules below change so Unity reimports the affected assets.
        const uint Version = 1;
        public const int MaxThirdPartyTextureSize = 1024;

        public override uint GetVersion() => Version;

        void OnPreprocessModel()
        {
            if (!ArtPaths.IsThirdParty(assetPath)) return;
            var mi = (ModelImporter)assetImporter;
            ApplyModelSettings(mi, KitRules.Resolve(assetPath));
        }

        /// <summary>Also used by the prefab builder to detect importers that drifted from the rules.</summary>
        public static void ApplyModelSettings(ModelImporter mi, ResolvedRule rule)
        {
            mi.globalScale = rule.Scale;
            mi.useFileScale = true;
            mi.bakeAxisConversion = true;

            mi.importCameras = false;
            mi.importLights = false;
            mi.importVisibility = false;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importBlendShapes = false;
            mi.importConstraints = false;

            mi.isReadable = false;
            // Road/ground tiles must butt together without cracks, so they get the lightest compression.
            mi.meshCompression = rule.Category == KitCategory.Road || rule.Category == KitCategory.Ground
                ? ModelImporterMeshCompression.Low
                : ModelImporterMeshCompression.Medium;
            mi.optimizeMeshPolygons = true;
            mi.optimizeMeshVertices = true;
            mi.weldVertices = true;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.None; // Simple Lit without normal maps does not need them
            mi.generateSecondaryUV = false;
            mi.addCollider = false; // prefabs get simple BoxColliders instead of mesh colliders

            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }

        public static bool ModelSettingsMatch(ModelImporter mi, ResolvedRule rule)
        {
            return Mathf.Approximately(mi.globalScale, rule.Scale)
                   && mi.useFileScale && mi.bakeAxisConversion
                   && !mi.importCameras && !mi.importLights && !mi.importAnimation
                   && !mi.isReadable && !mi.addCollider
                   && mi.meshCompression != ModelImporterMeshCompression.Off;
        }

        void OnPostprocessModel(GameObject root)
        {
            if (!ArtPaths.IsThirdParty(assetPath)) return;

            // Belt and braces: nothing from a third-party file may add cameras or lights to our scenes.
            foreach (Camera c in root.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(c);
            foreach (Light l in root.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l);

            ResolvedRule rule = KitRules.Resolve(assetPath);
            if (!rule.Known)
            {
                Debug.LogWarning("[CityScooter] " + assetPath + " is not covered by KitRules (scale 1 used). "
                                 + "Add the kit to Scripts/Editor/Art/KitRules.cs.");
            }
        }

        void OnPreprocessTexture()
        {
            if (!ArtPaths.IsThirdParty(assetPath)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.isReadable = false;
            ti.maxTextureSize = Mathf.Min(ti.maxTextureSize, MaxThirdPartyTextureSize);
            ti.textureCompression = TextureImporterCompression.Compressed;

            if (IsPaletteTexture(assetPath))
            {
                // Kenney kits colour everything through one small gradient palette: UVs sit inside flat cells,
                // so mipmaps only cost memory and bleed neighbouring colours at a distance.
                ti.mipmapEnabled = false;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.anisoLevel = 0;
                ti.alphaSource = TextureImporterAlphaSource.None;
            }
        }

        public static bool IsPaletteTexture(string assetPath)
        {
            string p = ArtPaths.Normalize(assetPath);
            return p.StartsWith(ArtPaths.KenneyRoot + "/", System.StringComparison.OrdinalIgnoreCase)
                   && p.EndsWith("/Textures/colormap.png", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
