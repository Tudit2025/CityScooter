using CityScooter.Rendering;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

namespace CityScooter.EditorTools.LookDev
{
    /// <summary>Sky / skyline / VFX / look-dev materials and the SceneLook presets (colours from art-src/skyline).</summary>
    static class LookDevMaterials
    {
        sealed class Place
        {
            public string Set;            // art file prefix
            public float HorizonFromTop;  // horizon / waterline inside the art
            public string Fog;            // fog = horizon colour of the art
            public string SkyHaze;        // art's atmospheric-perspective colour
            public string Zenith;         // above the sky strip (a touch deeper than the strip's top colour)
            public string Below;          // procedural fallback below the horizon
            public string Glow;
            public float GlowAmount;
        }

        // Values from art-src/skyline/out/skyline-meta.js (fog, haze, sky stops) and the coordinator's brief.
        static readonly Place Perth = new Place
        {
            Set = "perth", HorizonFromTop = LookDevPaths.PerthHorizonFromTop, Fog = "#F4E3C5", SkyHaze = "#D0E0E8",
            Zenith = "#68A7D9", Below = "#F4D4A7", Glow = "#FFE2B8", GlowAmount = 0.18f,
        };

        static readonly Place Rockingham = new Place
        {
            Set = "rockingham", HorizonFromTop = LookDevPaths.RockinghamHorizonFromTop, Fog = "#EBECDC", SkyHaze = "#D4E4EC",
            Zenith = "#5DA2DA", Below = "#F3DBB4", Glow = "#FFE9CC", GlowAmount = 0.15f,
        };

        public static void BuildAll(bool overwrite)
        {
            Shader sky = LookDevUtil.LoadShader(LookDevPaths.SkyShader);
            Sky(LookDevPaths.SkyPerth, sky, Perth, overwrite);
            Sky(LookDevPaths.SkyRockingham, sky, Rockingham, overwrite);

            Shader skyline = LookDevUtil.LoadShader(LookDevPaths.SkylineShader);
            LookDevUtil.GetOrCreate(LookDevPaths.SkylineMaterial, overwrite, () => new Material(skyline), m =>
            {
                m.shader = skyline;
                // Only seen if SkylineParallax is disabled (it sets every layer's texture through a property block).
                m.SetTexture("_BaseMap", Load<Texture2D>(LookDevPaths.ArtTexture(Perth.Set, "mid")));
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Haze", 0f);
                m.SetFloat("_BottomHaze", 0f);
                m.SetColor("_HazeColor", LookDevUtil.Hex(Perth.SkyHaze));
                m.enableInstancing = false;
            }, out _);

            BuildVfxMaterials(overwrite);
            BuildLooks(overwrite);
        }

        static void Sky(string path, Shader shader, Place p, bool overwrite)
        {
            LookDevUtil.GetOrCreate(path, overwrite, () => new Material(shader), m =>
            {
                m.shader = shader;
                var strip = Load<Texture2D>(LookDevPaths.ArtTexture(p.Set, "sky"));
                m.SetFloat("_UseGradientTex", strip != null ? 1f : 0f);
                m.SetTexture("_GradientTex", strip);
                m.SetFloat("_GradientHorizon", p.HorizonFromTop);
                // Fallback mapping when no SkylineRig publishes one: same as the rig defaults
                // (horizon -0.4 deg, one 4:1 texture per 90 deg -> the strip's top row sits at this angle).
                const float horizonAngle = -0.4f;
                float tanTop = Mathf.Tan(horizonAngle * Mathf.Deg2Rad) + p.HorizonFromTop * (90f * Mathf.Deg2Rad) * 0.25f;
                m.SetFloat("_GradientHorizonAngle", horizonAngle);
                m.SetFloat("_GradientTopAngle", Mathf.Atan(tanTop) * Mathf.Rad2Deg);
                m.SetColor("_ZenithColor", LookDevUtil.Hex(p.Zenith));
                m.SetColor("_HorizonColor", LookDevUtil.Hex(p.Fog));
                m.SetFloat("_HorizonFromFog", 1f);
                m.SetColor("_BelowHorizonColor", LookDevUtil.Hex(p.Below));
                m.SetFloat("_GradientPower", 0.55f);
                m.SetFloat("_HorizonBlend", 0.15f);
                m.SetColor("_SunGlowColor", LookDevUtil.Hex(p.Glow));
                m.SetFloat("_SunGlowAmount", p.GlowAmount);
                m.SetFloat("_SunGlowPower", 3f);
                m.SetFloat("_SunGlowHeight", 0.35f);
                m.SetColor("_SunDiscColor", LookDevUtil.Hex("#FFF6E2"));
                m.SetFloat("_SunDiscIntensity", 1.8f);
                m.SetFloat("_SunSize", 0.035f);
                m.SetFloat("_SunSoftness", 0.012f);
                m.SetColor("_SunHaloColor", LookDevUtil.Hex("#FFF0D2"));
                m.SetFloat("_SunHaloAmount", 0.28f);
                m.SetFloat("_SunHaloPower", 20f);
                m.SetFloat("_UseMainLight", 1f);
            }, out _);
        }

        static void BuildVfxMaterials(bool overwrite)
        {
            var softDot = Load<Texture2D>(LookDevPaths.SoftDotTexture);
            ParticleUnlit(LookDevPaths.DustMaterial, softDot, BaseShaderGUI.BlendMode.Alpha, overwrite);
            ParticleUnlit(LookDevPaths.StarMaterial, Load<Texture2D>(LookDevPaths.StarTexture), BaseShaderGUI.BlendMode.Alpha, overwrite);
            ParticleUnlit(LookDevPaths.SparkleMaterial, Load<Texture2D>(LookDevPaths.SparkleTexture), BaseShaderGUI.BlendMode.Additive, overwrite);
            ParticleUnlit(LookDevPaths.GlowMaterial, softDot, BaseShaderGUI.BlendMode.Additive, overwrite);
            ParticleUnlit(LookDevPaths.SpeedLineMaterial, softDot, BaseShaderGUI.BlendMode.Additive, overwrite);

            // Blob shadow: plain URP Unlit (not a particle), multiply blend so the road keeps its hue.
            Shader unlit = LookDevUtil.FindShader("Universal Render Pipeline/Unlit");
            LookDevUtil.GetOrCreate(LookDevPaths.BlobShadowMaterial, overwrite, () => new Material(unlit), m =>
            {
                m.shader = unlit;
                m.SetTexture("_BaseMap", Load<Texture2D>(LookDevPaths.BlobShadowTexture));
                m.SetColor("_BaseColor", new Color(0.36f, 0.39f, 0.5f, 0.6f));
                m.SetFloat("_Surface", (float)BaseShaderGUI.SurfaceType.Transparent);
                m.SetFloat("_Blend", (float)BaseShaderGUI.BlendMode.Multiply);
                m.SetFloat("_Cull", 2f);
                BaseShaderGUI.SetMaterialKeywords(m);
            }, out _);
        }

        static void ParticleUnlit(string path, Texture2D texture, BaseShaderGUI.BlendMode blend, bool overwrite)
        {
            Shader shader = LookDevUtil.FindShader("Universal Render Pipeline/Particles/Unlit");
            LookDevUtil.GetOrCreate(path, overwrite, () => new Material(shader), m =>
            {
                m.shader = shader;
                m.SetTexture("_BaseMap", texture);
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Surface", (float)BaseShaderGUI.SurfaceType.Transparent);
                m.SetFloat("_Blend", (float)blend);
                m.SetFloat("_Cull", 0f); // billboards: no culling surprises with flipped particles
                m.SetFloat("_ColorMode", 0f); // multiply by particle colour
                m.SetFloat("_SoftParticlesEnabled", 0f); // would need the depth texture
                m.SetFloat("_CameraFadingEnabled", 0f);
                m.SetFloat("_DistortionEnabled", 0f); // would need the opaque texture
                m.SetFloat("_FlipbookBlending", 0f);
                BaseShaderGUI.SetMaterialKeywords(m, null, ParticleGUI.SetMaterialKeywords);
            }, out _);
        }

        static void BuildLooks(bool overwrite)
        {
            LookDevUtil.GetOrCreate(LookDevPaths.LookPerth, overwrite, () => ScriptableObject.CreateInstance<SceneLook>(), look =>
            {
                SetPlace(look, Perth, LookDevPaths.SkyPerth);
                look.ambientSky = LookDevUtil.Hex("#A9CBEA");
                look.ambientEquator = LookDevUtil.Hex("#DDD5C3");
                look.ambientGround = LookDevUtil.Hex("#9C8B72");
                look.reflectionIntensity = 0.5f;
                look.fogStart = 40f;
                look.fogEnd = 190f;
                look.sunColor = LookDevUtil.Hex("#FFF1DC");
                look.sunIntensity = 1.35f;
                look.sunRotation = new Vector2(45f, 35f);
                look.shadowStrength = 0.7f;
            }, out _);

            LookDevUtil.GetOrCreate(LookDevPaths.LookRockingham, overwrite, () => ScriptableObject.CreateInstance<SceneLook>(), look =>
            {
                SetPlace(look, Rockingham, LookDevPaths.SkyRockingham);
                look.ambientSky = LookDevUtil.Hex("#B3DCEF");
                look.ambientEquator = LookDevUtil.Hex("#DCE2D6");
                look.ambientGround = LookDevUtil.Hex("#C4AF8C");
                look.reflectionIntensity = 0.55f;
                look.fogStart = 35f;
                look.fogEnd = 170f;
                look.sunColor = LookDevUtil.Hex("#FFF4E3");
                look.sunIntensity = 1.3f;
                look.sunRotation = new Vector2(38f, -30f);
                look.shadowStrength = 0.6f;
            }, out _);
        }

        static void SetPlace(SceneLook look, Place p, string skyMaterialPath)
        {
            look.skyboxMaterial = Load<Material>(skyMaterialPath);
            look.fog = true;
            look.fogColor = LookDevUtil.Hex(p.Fog);
            look.skylineFar = Load<Texture2D>(LookDevPaths.ArtTexture(p.Set, "far"));
            look.skylineMid = Load<Texture2D>(LookDevPaths.ArtTexture(p.Set, "mid"));
            look.skylineNear = Load<Texture2D>(LookDevPaths.ArtTexture(p.Set, "near"));
            look.skylineHorizonFromTop = p.HorizonFromTop;
            look.skylineHazeColor = LookDevUtil.Hex(p.SkyHaze);
        }

        /// <summary>Opaque Simple Lit material for look-dev stand-in geometry (no specular: cheapest lit path).</summary>
        public static Material LookDevLit(string name, Color color, Color emission, bool overwrite)
        {
            Shader shader = LookDevUtil.FindShader("Universal Render Pipeline/Simple Lit");
            string path = LookDevPaths.LookDevMaterials + "/LD_" + name + ".mat";
            LookDevUtil.GetOrCreate(path, overwrite, () => new Material(shader), m =>
            {
                m.shader = shader;
                m.SetColor("_BaseColor", color);
                m.SetFloat("_SpecularHighlights", 1f); // 1 = no specular
                m.SetFloat("_Smoothness", 0.2f);
                bool emissive = emission.maxColorComponent > 0.001f;
                m.SetColor("_EmissionColor", emission);
                m.globalIlluminationFlags = emissive ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                BaseShaderGUI.SetMaterialKeywords(m, SimpleLitGUI.SetMaterialKeywords);
            }, out Material material);
            return material;
        }

        public static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) LookDevUtil.Warn("missing asset " + path);
            return asset;
        }
    }
}
