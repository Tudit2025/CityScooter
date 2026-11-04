using System;
using CityScooter.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CityScooter.EditorTools.LookDev
{
    /// <summary>
    /// Builds the environment and VFX prefabs. Everything is assembled in an editor preview scene, so building never
    /// changes the open scene (ApplySceneLook would otherwise push its look into the open scene's RenderSettings).
    /// </summary>
    static class LookDevPrefabs
    {
        public static void BuildEnvironment(bool overwrite)
        {
            Build(LookDevPaths.LightingPrefab, overwrite, BuildLighting);
            Build(LookDevPaths.SkylinePrefab, overwrite, BuildSkylineRig);
        }

        public static void BuildVfx(bool overwrite)
        {
            Build(LookDevPaths.CrashPuffPrefab, overwrite, BuildCrashPuff);
            Build(LookDevPaths.GiftSparklePrefab, overwrite, BuildGiftSparkle);
            Build(LookDevPaths.SpeedLinesPrefab, overwrite, BuildSpeedLines);
            Build(LookDevPaths.BlobShadowPrefab, overwrite, BuildBlobShadow);
        }

        static void Build(string path, bool overwrite, Func<Scene, GameObject> build)
        {
            if (!overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            LookDevUtil.EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = build(preview);
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
                if (!ok) throw new InvalidOperationException("Could not save prefab " + path);
                LookDevUtil.Log("saved " + path);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        static GameObject NewRoot(string name, Scene scene)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene); // before any component can react to OnEnable
            return go;
        }

        static GameObject NewChild(GameObject parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        // ------------------------------------------------------------------ environment

        static GameObject BuildLighting(Scene scene)
        {
            var look = LookDevMaterials.Load<SceneLook>(LookDevPaths.LookPerth);
            GameObject root = NewRoot("Lighting_PerthAfternoon", scene);
            GameObject sunObject = NewChild(root, "Sun", Vector3.zero);
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft; // the Web asset has soft shadows off, so this falls back to hard there
            sun.renderMode = LightRenderMode.ForcePixel;
            sun.lightmapBakeType = LightmapBakeType.Realtime;
            sunObject.AddComponent<UniversalAdditionalLightData>();
            ApplySceneLook.ApplySun(sun, look, true);

            var apply = root.AddComponent<ApplySceneLook>();
            apply.look = look;
            apply.sun = sun;
            apply.applySunRotation = true;
            return root;
        }

        static GameObject BuildSkylineRig(Scene scene)
        {
            var material = LookDevMaterials.Load<Material>(LookDevPaths.SkylineMaterial);
            GameObject root = NewRoot("SkylineRig", scene);
            // Textures come from the active Scene Look; the Perth art is the fallback. The art already has its own
            // atmospheric perspective, so no extra haze. Parallax factors from the art README (far 0.15, mid 0.4, near 1).
            // Radii only decide what can stand in front (all beyond fog end and road chunks); angles are shared.
            var layers = new[]
            {
                new SkylineParallax.Layer
                {
                    name = "Far", lookSlot = SkylineParallax.LookSlot.Far,
                    texture = LookDevMaterials.Load<Texture2D>(LookDevPaths.ArtTexture("perth", "far")),
                    radius = 600f, parallax = 0.15f,
                },
                new SkylineParallax.Layer
                {
                    name = "Mid", lookSlot = SkylineParallax.LookSlot.Mid,
                    texture = LookDevMaterials.Load<Texture2D>(LookDevPaths.ArtTexture("perth", "mid")),
                    radius = 500f, parallax = 0.4f,
                },
                new SkylineParallax.Layer
                {
                    name = "Near", lookSlot = SkylineParallax.LookSlot.Near,
                    texture = LookDevMaterials.Load<Texture2D>(LookDevPaths.ArtTexture("perth", "near")),
                    radius = 400f, parallax = 1f,
                },
            };
            string[] childNames = { "Layer_Far", "Layer_Mid", "Layer_Near" };
            for (int i = 0; i < layers.Length; i++)
            {
                GameObject child = NewChild(root, childNames[i], Vector3.zero);
                child.AddComponent<MeshFilter>();
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                ConfigureUnlitRenderer(renderer);
                layers[i].renderer = renderer;
            }

            var rig = root.AddComponent<SkylineParallax>();
            rig.layers = layers;
            rig.horizonAngle = -0.4f;
            rig.degreesPerRepeat = 90f;
            rig.horizonFromTop = LookDevPaths.PerthHorizonFromTop;
            rig.centerU = 0.47f;
            rig.arcDegrees = 150f;
            rig.scrollPerMetre = 0.0015f;
            rig.Refresh(); // lays the children out so the prefab has sensible transforms
            foreach (SkylineParallax.Layer layer in layers)
            {
                // The arc mesh is generated at runtime; never save a reference to the temporary one.
                layer.renderer.GetComponent<MeshFilter>().sharedMesh = null;
            }
            return root;
        }

        static void ConfigureUnlitRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = false;
        }

        // ------------------------------------------------------------------ VFX

        static GameObject BuildCrashPuff(Scene scene)
        {
            GameObject root = NewRoot("VFX_CrashPuff", scene);

            // Dust burst: soft beige puffs pushed out and up, slowed by drag, growing and fading.
            ParticleSystem dust = AddSystem(root);
            ParticleSystem.MainModule main = dust.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 0.95f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(LookDevUtil.Hex("#EFE6D2"), LookDevUtil.Hex("#CDBFA5"));
            main.gravityModifier = -0.05f;
            main.maxParticles = 24;
            main.stopAction = ParticleSystemStopAction.Destroy;
            Burst(dust, 0f, 14);
            ParticleSystem.ShapeModule shape = dust.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.35f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            Drag(dust, 3.5f);
            SizeOverLifetime(dust, new Keyframe(0f, 0.55f), new Keyframe(0.35f, 1.05f), new Keyframe(1f, 1.35f));
            AlphaOverLifetime(dust, new Vector2(0f, 0.9f), new Vector2(0.6f, 0.6f), new Vector2(1f, 0f));
            Spin(dust, 1.2f);
            SetupRenderer(dust, LookDevPaths.DustMaterial, ParticleSystemRenderMode.Billboard, 0f);

            // A few cartoon stars popping up over the rider's head and falling away.
            GameObject starsObject = NewChild(root, "Stars", new Vector3(0f, 0.9f, 0f));
            ParticleSystem stars = AddSystem(starsObject);
            main = stars.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 0.95f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 3.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.32f, 0.42f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(LookDevUtil.Hex("#FFD93B"), LookDevUtil.Hex("#FFE877"));
            main.gravityModifier = 0.75f;
            main.maxParticles = 6;
            Burst(stars, 0.02f, 5);
            shape = stars.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 38f;
            shape.radius = 0.15f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            SizeOverLifetime(stars, new Keyframe(0f, 0.6f), new Keyframe(0.12f, 1.1f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f));
            AlphaOverLifetime(stars, new Vector2(0f, 1f), new Vector2(0.85f, 1f), new Vector2(1f, 0f));
            Spin(stars, 4f);
            SetupRenderer(stars, LookDevPaths.StarMaterial, ParticleSystemRenderMode.Billboard, -5f);

            root.AddComponent<VfxDensityScaler>();
            return root;
        }

        static GameObject BuildGiftSparkle(Scene scene)
        {
            GameObject root = NewRoot("VFX_GiftSparkle", scene);

            // Gold twinkles bursting out of the gift.
            ParticleSystem sparkles = AddSystem(root);
            ParticleSystem.MainModule main = sparkles.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(LookDevUtil.Hex("#FFE07A"), LookDevUtil.Hex("#FFB52E"));
            main.gravityModifier = 0.35f;
            main.maxParticles = 24;
            main.stopAction = ParticleSystemStopAction.Destroy;
            Burst(sparkles, 0f, 16);
            ParticleSystem.ShapeModule shape = sparkles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
            Drag(sparkles, 4f);
            SizeOverLifetime(sparkles, new Keyframe(0f, 1f), new Keyframe(0.6f, 0.8f), new Keyframe(1f, 0f));
            AlphaOverLifetime(sparkles, new Vector2(0f, 1f), new Vector2(0.7f, 1f), new Vector2(1f, 0f));
            Spin(sparkles, 3f);
            SetupRenderer(sparkles, LookDevPaths.SparkleMaterial, ParticleSystemRenderMode.Billboard, 0f);

            // One quick warm glow behind the twinkles.
            GameObject flashObject = NewChild(root, "Flash", Vector3.zero);
            ParticleSystem flash = AddSystem(flashObject);
            main = flash.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = 0.2f;
            main.startSpeed = 0f;
            main.startSize = 1.4f;
            main.startColor = new Color(1f, 0.9f, 0.63f, 0.85f);
            main.maxParticles = 1;
            Burst(flash, 0f, 1);
            shape = flash.shape;
            shape.enabled = false;
            SizeOverLifetime(flash, new Keyframe(0f, 0.5f), new Keyframe(1f, 1.3f));
            AlphaOverLifetime(flash, new Vector2(0f, 1f), new Vector2(1f, 0f));
            SetupRenderer(flash, LookDevPaths.GlowMaterial, ParticleSystemRenderMode.Billboard, 2f);

            root.AddComponent<VfxDensityScaler>();
            return root;
        }

        static GameObject BuildSpeedLines(Scene scene)
        {
            GameObject root = NewRoot("VFX_SpeedLines", scene);

            // A ring of thin streaks ahead of the camera flying back past it (gap at the bottom keeps the rider clear).
            ParticleSystem lines = AddSystem(root);
            ParticleSystem.MainModule main = lines.main;
            main.duration = 1f;
            main.loop = true;
            main.prewarm = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.6f);
            main.startSpeed = 40f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.05f);
            main.startColor = new Color(1f, 1f, 1f, 0.42f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = 60;
            ParticleSystem.EmissionModule emission = lines.emission;
            emission.rateOverTime = 0f; // SpeedLines drives this
            ParticleSystem.ShapeModule shape = lines.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0f;
            shape.radius = 4.5f;
            shape.radiusThickness = 0f;
            shape.arc = 270f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
            shape.rotation = new Vector3(0f, 0f, -45f);
            AlphaOverLifetime(lines, new Vector2(0f, 0f), new Vector2(0.15f, 1f), new Vector2(0.75f, 1f), new Vector2(1f, 0f));
            ParticleSystemRenderer renderer = SetupRenderer(lines, LookDevPaths.SpeedLineMaterial, ParticleSystemRenderMode.Stretch, 0f);
            renderer.velocityScale = 0.045f;
            renderer.lengthScale = 2f;
            renderer.maxParticleSize = 1f;

            var controller = root.AddComponent<SpeedLines>();
            controller.maxRate = 45f;
            controller.startAt = 0.3f;
            controller.streakSpeed = new Vector2(30f, 55f);
            controller.cameraOffset = new Vector3(0f, 0.5f, 22f);
            return root;
        }

        static GameObject BuildBlobShadow(Scene scene)
        {
            GameObject root = NewRoot("BlobShadow", scene);
            GameObject quad = NewChild(root, "Blob", Vector3.zero);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var renderer = quad.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = LookDevMaterials.Load<Material>(LookDevPaths.BlobShadowMaterial);
            ConfigureUnlitRenderer(renderer);

            var blob = root.AddComponent<BlobShadow>();
            blob.blobRenderer = renderer;
            blob.visibility = BlobShadow.Visibility.WhenRealShadowsOff;
            blob.size = new Vector2(1.05f, 1.9f);
            blob.color = new Color(0.33f, 0.36f, 0.48f, 0.7f);
            return root;
        }

        // ------------------------------------------------------------------ particle helpers

        static ParticleSystem AddSystem(GameObject go)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.stopAction = ParticleSystemStopAction.None;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            return ps;
        }

        static void Burst(ParticleSystem ps, float time, short count)
        {
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(time, count) });
        }

        static void Drag(ParticleSystem ps, float drag)
        {
            ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 100f; // no hard cap, only drag
            limit.dampen = 0f;
            limit.drag = drag;
        }

        static void SizeOverLifetime(ParticleSystem ps, params Keyframe[] keys)
        {
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));
        }

        /// <summary>Keys are (time, alpha); colour stays white so the start colour shows through.</summary>
        static void AlphaOverLifetime(ParticleSystem ps, params Vector2[] keys)
        {
            var alphas = new GradientAlphaKey[keys.Length];
            for (int i = 0; i < keys.Length; i++) alphas[i] = new GradientAlphaKey(keys[i].y, keys[i].x);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, alphas);
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        static void Spin(ParticleSystem ps, float radiansPerSecond)
        {
            ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-radiansPerSecond, radiansPerSecond);
        }

        static ParticleSystemRenderer SetupRenderer(ParticleSystem ps, string materialPath, ParticleSystemRenderMode mode, float sortingFudge)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = mode;
            renderer.sharedMaterial = LookDevMaterials.Load<Material>(materialPath);
            renderer.sortingFudge = sortingFudge;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.maxParticleSize = 0.6f;
            return renderer;
        }

        /// <summary>Destroy helper used by scene building code.</summary>
        public static void DestroyCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
        }
    }
}
