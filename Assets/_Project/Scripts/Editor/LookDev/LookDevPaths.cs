namespace CityScooter.EditorTools.LookDev
{
    /// <summary>Every asset path the look-dev tools read or write.</summary>
    static class LookDevPaths
    {
        public const string Art = "Assets/_Project/Art";
        public const string Shaders = Art + "/Shaders";
        public const string Materials = Art + "/Materials";
        public const string VfxMaterials = Materials + "/VFX";
        public const string Looks = Art + "/Looks";
        public const string GeneratedTextures = Art + "/Textures/Generated";
        /// <summary>Drop the real skyline PNGs here (import settings are applied automatically).</summary>
        public const string SkylineTextures = Art + "/Textures/Skyline";
        public const string LookDevArt = Art + "/LookDev";
        public const string LookDevMaterials = LookDevArt + "/Materials";
        public const string LookDevMeshes = LookDevArt + "/Meshes";

        public const string EnvironmentPrefabs = "Assets/_Project/Prefabs/Environment";
        public const string VfxPrefabs = "Assets/_Project/Prefabs/VFX";
        public const string Scene = "Assets/_Project/Scenes/LookDev.unity";

        public const string SkyShader = Shaders + "/SkyGradient.shader";
        public const string SkylineShader = Shaders + "/SkylineLayer.shader";

        public const string SkyPerth = Materials + "/Sky_PerthAfternoon.mat";
        public const string SkyRockingham = Materials + "/Sky_RockinghamBeach.mat";
        public const string SkylineMaterial = Materials + "/Skyline_Layer.mat";

        public const string LookPerth = Looks + "/Look_PerthAfternoon.asset";
        public const string LookRockingham = Looks + "/Look_RockinghamBeach.asset";

        // Procedural placeholders (kept for testing; the delivered art below is the default).
        public const string SkylineFarTexture = GeneratedTextures + "/T_Skyline_Far_Placeholder.png";
        public const string SkylineMidTexture = GeneratedTextures + "/T_Skyline_Mid_Placeholder.png";
        public const string SkylineNearTexture = GeneratedTextures + "/T_Skyline_Near_Placeholder.png";

        // Delivered skyline art (art-src/skyline/out, Web-size "-2k" files), copied by SkylineArtImporter.
        public const string ArtSourceFolder = "art-src/skyline/out";
        public static readonly string[] ArtSets = { "perth", "rockingham" };
        public static readonly string[] ArtLayers = { "far", "mid", "near", "sky" };
        public static string ArtTexture(string set, string layer) => SkylineTextures + "/" + set + "_" + layer + "-2k.png";
        public const float PerthHorizonFromTop = 0.845f;
        public const float RockinghamHorizonFromTop = 0.705f;
        public const string SoftDotTexture = GeneratedTextures + "/T_VFX_SoftDot.png";
        public const string StarTexture = GeneratedTextures + "/T_VFX_Star.png";
        public const string SparkleTexture = GeneratedTextures + "/T_VFX_Sparkle.png";
        public const string BlobShadowTexture = GeneratedTextures + "/T_BlobShadow.png";

        public const string DustMaterial = VfxMaterials + "/M_VFX_Dust.mat";
        public const string StarMaterial = VfxMaterials + "/M_VFX_Star.mat";
        public const string SparkleMaterial = VfxMaterials + "/M_VFX_Sparkle_Add.mat";
        public const string GlowMaterial = VfxMaterials + "/M_VFX_Glow_Add.mat";
        public const string SpeedLineMaterial = VfxMaterials + "/M_VFX_SpeedLine_Add.mat";
        public const string BlobShadowMaterial = VfxMaterials + "/M_BlobShadow.mat";

        public const string LightingPrefab = EnvironmentPrefabs + "/Lighting_PerthAfternoon.prefab";
        public const string SkylinePrefab = EnvironmentPrefabs + "/SkylineRig.prefab";
        public const string CrashPuffPrefab = VfxPrefabs + "/VFX_CrashPuff.prefab";
        public const string GiftSparklePrefab = VfxPrefabs + "/VFX_GiftSparkle.prefab";
        public const string SpeedLinesPrefab = VfxPrefabs + "/VFX_SpeedLines.prefab";
        public const string BlobShadowPrefab = VfxPrefabs + "/BlobShadow.prefab";

        public const string ConeMesh = LookDevMeshes + "/LD_TrafficCone.asset";
    }
}
