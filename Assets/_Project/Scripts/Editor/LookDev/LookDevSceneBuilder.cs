using System.Collections.Generic;
using System.IO;
using CityScooter.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace CityScooter.EditorTools.LookDev
{
    /// <summary>
    /// Builds Assets/_Project/Scenes/LookDev.unity: a straight Perth street made of primitives (road, footpaths,
    /// buildings, trees), obstacles and gifts in the lanes, a capsule rider on a scooter, the game camera, the lighting
    /// prefab, the skyline rig and VFX previews. For judging the look only; it is not in the build settings.
    /// </summary>
    static class LookDevSceneBuilder
    {
        const float RoadHalfWidth = 4f;
        const float StreetLength = 330f;

        static readonly Dictionary<string, Material> s_Materials = new Dictionary<string, Material>();
        static bool s_Overwrite;

        public static void Build(bool overwrite)
        {
            if (!overwrite && File.Exists(LookDevUtil.ToFullPath(LookDevPaths.Scene)))
            {
                LookDevUtil.Log("scene exists, not rebuilt: " + LookDevPaths.Scene);
                return;
            }
            s_Overwrite = overwrite;
            s_Materials.Clear();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildStreet();
            BuildObstaclesAndGifts();
            GameObject rideRoot = BuildRiderAndCamera(out Camera camera);
            Instantiate(LookDevPaths.LightingPrefab, null);
            Instantiate(LookDevPaths.SkylinePrefab, null);
            BuildVfxPreviews(rideRoot, camera);

            var tuner = new GameObject("QualityAutoTuner (enable to test tiers)");
            tuner.AddComponent<QualityAutoTuner>();
            tuner.SetActive(false);

            LookDevUtil.EnsureFolder(Path.GetDirectoryName(LookDevPaths.Scene)?.Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, LookDevPaths.Scene);
            LookDevUtil.Log("saved " + LookDevPaths.Scene);
        }

        // ------------------------------------------------------------------ street

        static void BuildStreet()
        {
            Transform env = new GameObject("Environment").transform;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Setup(ground, env, "Ground", new Vector3(0f, -0.02f, 200f), new Vector3(80f, 1f, 80f), Mat("Grass", "#93AB66"));

            float zCentre = StreetLength * 0.5f - 15f;
            Box(env, "Road", new Vector3(0f, -0.05f, zCentre), new Vector3(RoadHalfWidth * 2f, 0.1f, StreetLength), Mat("Asphalt", "#55585E"));
            for (int side = -1; side <= 1; side += 2)
            {
                Box(env, "Footpath", new Vector3(side * 5.6f, 0.075f, zCentre), new Vector3(3f, 0.15f, StreetLength), Mat("Footpath", "#CFC5B4"));
                Box(env, "Kerb", new Vector3(side * 4.1f, 0.085f, zCentre), new Vector3(0.2f, 0.17f, StreetLength), Mat("Kerb", "#B4AEA3"));
                Box(env, "EdgeLine", new Vector3(side * 3.7f, 0.006f, zCentre), new Vector3(0.12f, 0.012f, StreetLength), Mat("LaneWhite", "#F2F1EA"));
            }
            for (float z = -10f; z < StreetLength - 20f; z += 9f)
            {
                Box(env, "Dash", new Vector3(0f, 0.006f, z), new Vector3(0.15f, 0.012f, 3f), Mat("LaneWhite", "#F2F1EA"));
            }

            // Low-rise Perth street (1-3 storeys, the odd 4-5) set back behind the footpath, with open lots and
            // parks so the skyline shows over the roofs. Warm palette: sandstone, white render, terracotta, pastels.
            string[] facades = { "#E2C79C", "#EFEAE0", "#C9724E", "#A8C5D8", "#B5D8C2", "#7FA2BF", "#BDB2A4", "#E8D6B5" };
            var rng = new Random(4404);
            for (int side = -1; side <= 1; side += 2)
            {
                float z = -12f;
                while (z < StreetLength - 30f)
                {
                    float depth = R(rng, 8f, 15f);
                    if (rng.NextDouble() < 0.3)
                    {
                        z += depth + R(rng, 4f, 12f); // open lot / park
                        continue;
                    }
                    float width = R(rng, 7f, 12f);
                    float height = rng.NextDouble() < 0.15 ? R(rng, 12f, 16f) : R(rng, 4f, 10f);
                    float x = side * (9.5f + width * 0.5f + R(rng, 0f, 3f));
                    string facade = facades[rng.Next(facades.Length)];
                    Box(env, "Building", new Vector3(x, height * 0.5f, z + depth * 0.5f), new Vector3(width, height, depth), Mat("Facade_" + facade.Substring(1), facade));
                    if (rng.NextDouble() < 0.5)
                    {
                        Box(env, "Parapet", new Vector3(x, height + 0.2f, z + depth * 0.5f), new Vector3(width + 0.3f, 0.4f, depth + 0.3f), Mat("Parapet", "#9C948A"));
                    }
                    z += depth + R(rng, 2f, 6f);
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                for (float z = 6f; z < StreetLength - 30f; z += 20f)
                {
                    Transform tree = new GameObject("StreetTree").transform;
                    tree.SetParent(env, false);
                    tree.localPosition = new Vector3(side * 6.6f, 0.15f, z + (side > 0 ? 10f : 0f));
                    var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Setup(trunk, tree, "Trunk", new Vector3(0f, 1f, 0f), new Vector3(0.28f, 1f, 0.28f), Mat("Trunk", "#7A5A3E"));
                    var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Setup(canopy, tree, "Canopy", new Vector3(0f, 2.9f, 0f), new Vector3(2.6f, 2.2f, 2.6f), Mat("Canopy", "#5F9A4C"));
                }
            }
        }

        // ------------------------------------------------------------------ obstacles and gifts

        static void BuildObstaclesAndGifts()
        {
            Transform obstacles = new GameObject("Obstacles").transform;
            Mesh cone = ConeMesh();
            Cone(obstacles, cone, new Vector3(2.6f, 0f, 14f));
            Cone(obstacles, cone, new Vector3(1.8f, 0f, 15.3f));
            Barrier(obstacles, new Vector3(-2.6f, 0f, 24f));
            Bin(obstacles, new Vector3(0.2f, 0f, 33f));
            Car(obstacles, new Vector3(-2.5f, 0f, 46f));
            Railing(obstacles, new Vector3(0f, 0f, 60f));
            Cone(obstacles, cone, new Vector3(2.6f, 0f, 80f));
            Bin(obstacles, new Vector3(-2.4f, 0f, 104f));
            Cone(obstacles, cone, new Vector3(0f, 0f, 130f));

            Transform gifts = new GameObject("Gifts").transform;
            Gift(gifts, new Vector3(-2.6f, 0.75f, 9f));
            Gift(gifts, new Vector3(0.4f, 0.75f, 40f));
            Gift(gifts, new Vector3(2.6f, 0.75f, 70f));
        }

        static void Cone(Transform parent, Mesh mesh, Vector3 position)
        {
            var go = new GameObject("TrafficCone");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { Mat("ConeOrange", "#FF6A13"), Mat("LaneWhite", "#F2F1EA") };
        }

        static void Barrier(Transform parent, Vector3 position)
        {
            Transform root = Group(parent, "RoadworksBarrier", position);
            for (int i = 0; i < 4; i++)
            {
                Box(root, "Board", new Vector3(-0.825f + i * 0.55f, 0.75f, 0f), new Vector3(0.55f, 0.36f, 0.1f),
                    i % 2 == 0 ? Mat("BarrierRed", "#E5352B") : Mat("LaneWhite", "#F2F1EA"));
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Box(root, "Post", new Vector3(side * 1.05f, 0.45f, 0f), new Vector3(0.08f, 0.9f, 0.08f), Mat("DarkMetal", "#3A3D42"));
                Box(root, "Foot", new Vector3(side * 1.05f, 0.03f, 0f), new Vector3(0.45f, 0.06f, 0.35f), Mat("DarkMetal", "#3A3D42"));
            }
        }

        static void Bin(Transform parent, Vector3 position)
        {
            // WA kerbside bin: dark green body, red general-waste lid.
            Transform root = Group(parent, "WheelieBin", position);
            Box(root, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 1f, 0.72f), Mat("BinGreen", "#2E6B3A"));
            Box(root, "Lid", new Vector3(0f, 1.03f, 0f), new Vector3(0.66f, 0.07f, 0.78f), Mat("BinLidRed", "#D93A2B"));
        }

        static void Car(Transform parent, Vector3 position)
        {
            Transform root = Group(parent, "ParkedCar", position);
            Box(root, "Body", new Vector3(0f, 0.58f, 0f), new Vector3(1.8f, 0.66f, 4.2f), Mat("CarBlue", "#2F80ED"));
            Box(root, "Cabin", new Vector3(0f, 1.18f, -0.25f), new Vector3(1.6f, 0.56f, 2.2f), Mat("CarGlass", "#2B3440"));
            Box(root, "Roof", new Vector3(0f, 1.48f, -0.25f), new Vector3(1.62f, 0.06f, 2.1f), Mat("CarBlue", "#2F80ED"));
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Setup(wheel, root, "Wheel", new Vector3(sx * 0.82f, 0.32f, sz * 1.35f), new Vector3(0.62f, 0.12f, 0.62f), Mat("Tyre", "#1E1F22"));
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }
        }

        static void Railing(Transform parent, Vector3 position)
        {
            // Jump obstacle spanning the road.
            Transform root = Group(parent, "Railing", position);
            float width = RoadHalfWidth * 2f - 0.4f;
            Box(root, "TopRail", new Vector3(0f, 0.75f, 0f), new Vector3(width, 0.1f, 0.1f), Mat("RailYellow", "#FFC531"));
            Box(root, "LowRail", new Vector3(0f, 0.38f, 0f), new Vector3(width, 0.08f, 0.08f), Mat("RailYellow", "#FFC531"));
            for (float x = -width * 0.5f; x <= width * 0.5f + 0.01f; x += width / 4f)
            {
                Box(root, "Post", new Vector3(x, 0.4f, 0f), new Vector3(0.08f, 0.8f, 0.08f), Mat("DarkMetal", "#3A3D42"));
            }
        }

        static void Gift(Transform parent, Vector3 position)
        {
            Transform root = Group(parent, "Gift", position);
            root.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Material gold = Mat("GiftGold", "#FFC93C", "#5A3C00");
            Material ribbon = Mat("GiftRibbon", "#E83F6F");
            Box(root, "Box", Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), gold);
            Box(root, "RibbonA", Vector3.zero, new Vector3(0.62f, 0.62f, 0.14f), ribbon);
            Box(root, "RibbonB", Vector3.zero, new Vector3(0.14f, 0.62f, 0.62f), ribbon);
        }

        // ------------------------------------------------------------------ rider, camera, VFX

        static GameObject BuildRiderAndCamera(out Camera camera)
        {
            var rideRoot = new GameObject("RideRoot (moves in play mode)");
            rideRoot.AddComponent<LookDevAutoRide>();

            Transform rider = Group(rideRoot.transform, "Rider", Vector3.zero);
            Box(rider, "Deck", new Vector3(0f, 0.14f, 0f), new Vector3(0.22f, 0.06f, 0.9f), Mat("ScooterDeck", "#2B2D42"));
            for (int sz = -1; sz <= 1; sz += 2)
            {
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Setup(wheel, rider, "Wheel", new Vector3(0f, 0.09f, sz * 0.38f), new Vector3(0.18f, 0.025f, 0.18f), Mat("Tyre", "#1E1F22"));
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Setup(stem, rider, "Stem", new Vector3(0f, 0.6f, 0.36f), new Vector3(0.04f, 0.45f, 0.04f), Mat("Chrome", "#C0C5CC"));
            Box(rider, "Handlebar", new Vector3(0f, 1.05f, 0.36f), new Vector3(0.5f, 0.04f, 0.04f), Mat("ScooterDeck", "#2B2D42"));
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Setup(body, rider, "Body", new Vector3(0f, 0.745f, 0f), new Vector3(0.42f, 0.575f, 0.42f), Mat("ShirtTeal", "#2EC4B6"));
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Setup(head, rider, "Helmet", new Vector3(0f, 1.47f, 0.02f), new Vector3(0.34f, 0.32f, 0.36f), Mat("HelmetYellow", "#FFD23F"));
            Instantiate(LookDevPaths.BlobShadowPrefab, rider);

            // Game camera: ~6 m behind, 3.5 m up, looking ~20 m ahead of the rider.
            var cameraObject = new GameObject("LookDevCamera") { tag = "MainCamera" };
            cameraObject.transform.SetParent(rideRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 3.5f, -6f);
            cameraObject.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, 0f, 20f) - new Vector3(0f, 3.5f, -6f));
            camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.AddComponent<AudioListener>();
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false; // recommended for the Web build (see docs/art/urp-web-settings.md)
            data.antialiasing = AntialiasingMode.None; // MSAA comes from the URP asset
            data.renderShadows = true;
            data.requiresDepthOption = CameraOverrideOption.Off;
            data.requiresColorOption = CameraOverrideOption.Off;
            return rideRoot;
        }

        static void BuildVfxPreviews(GameObject rideRoot, Camera camera)
        {
            GameObject lines = Instantiate(LookDevPaths.SpeedLinesPrefab, camera.transform);
            lines.transform.localPosition = new Vector3(0f, 0.5f, 22f);
            lines.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var speed = lines.GetComponent<SpeedLines>();
            speed.speed01 = 0.8f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(speed);

            Spawner(rideRoot.transform, "VFX Preview - CrashPuff", LookDevPaths.CrashPuffPrefab, new Vector3(1.2f, 0f, 3f), 2.5f);
            Spawner(rideRoot.transform, "VFX Preview - GiftSparkle", LookDevPaths.GiftSparklePrefab, new Vector3(-1.6f, 0.8f, 7f), 1.8f);
        }

        static void Spawner(Transform parent, string name, string prefabPath, Vector3 localPosition, float interval)
        {
            Transform t = Group(parent, name, localPosition);
            var spawner = t.gameObject.AddComponent<VfxPreviewSpawner>();
            spawner.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            spawner.interval = interval;
        }

        // ------------------------------------------------------------------ helpers

        static GameObject Instantiate(string prefabPath, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                LookDevUtil.Warn("missing prefab " + prefabPath);
                return new GameObject("MISSING " + Path.GetFileNameWithoutExtension(prefabPath));
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return go;
        }

        static Transform Group(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        static GameObject Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Setup(go, parent, name, localPosition, size, material);
            return go;
        }

        static void Setup(GameObject go, Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            LookDevPrefabs.DestroyCollider(go); // look-dev geometry is visual only
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        static Material Mat(string name, string color, string emission = null)
        {
            if (s_Materials.TryGetValue(name, out Material cached)) return cached;
            Color e = emission != null ? LookDevUtil.Hex(emission) : Color.black;
            Material m = LookDevMaterials.LookDevLit(name, LookDevUtil.Hex(color), e, s_Overwrite);
            s_Materials[name] = m;
            return m;
        }

        static float R(Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

        // ------------------------------------------------------------------ traffic cone mesh (flat shaded)

        static Mesh ConeMesh()
        {
            LookDevUtil.GetOrCreate(LookDevPaths.ConeMesh, s_Overwrite, () => new Mesh(), mesh =>
            {
                var vertices = new List<Vector3>();
                var normals = new List<Vector3>();
                var body = new List<int>();
                var band = new List<int>();
                AddBox(vertices, normals, body, new Vector3(0f, 0.025f, 0f), new Vector3(0.46f, 0.05f, 0.46f));
                const float y0 = 0.05f, y1 = 0.75f, r0 = 0.2f, r1 = 0.035f;
                AddFrustum(vertices, normals, body, y0, y1, r0, r1, 12, true);
                float RadiusAt(float y) => Mathf.Lerp(r0, r1, (y - y0) / (y1 - y0)) + 0.006f;
                AddFrustum(vertices, normals, band, 0.36f, 0.48f, RadiusAt(0.36f), RadiusAt(0.48f), 12, false);
                mesh.Clear();
                mesh.name = "LD_TrafficCone";
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.subMeshCount = 2;
                mesh.SetTriangles(body, 0);
                mesh.SetTriangles(band, 1);
                mesh.RecalculateBounds();
            }, out Mesh result);
            return result;
        }

        static void AddFrustum(List<Vector3> v, List<Vector3> n, List<int> t, float y0, float y1, float r0, float r1, int sides, bool capTop)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides;
                float a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = new Vector3(Mathf.Sin(a0) * r0, y0, Mathf.Cos(a0) * r0);
                Vector3 p1 = new Vector3(Mathf.Sin(a1) * r0, y0, Mathf.Cos(a1) * r0);
                Vector3 p2 = new Vector3(Mathf.Sin(a1) * r1, y1, Mathf.Cos(a1) * r1);
                Vector3 p3 = new Vector3(Mathf.Sin(a0) * r1, y1, Mathf.Cos(a0) * r1);
                float mid = (a0 + a1) * 0.5f;
                AddQuad(v, n, t, p0, p1, p2, p3, new Vector3(Mathf.Sin(mid), 0f, Mathf.Cos(mid)));
                if (capTop) AddTriangle(v, n, t, new Vector3(0f, y1, 0f), p3, p2, Vector3.up);
            }
        }

        static void AddBox(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 c, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            Vector3 P(float x, float y, float z) => c + new Vector3(x * h.x, y * h.y, z * h.z);
            AddQuad(v, n, t, P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), Vector3.up);
            AddQuad(v, n, t, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), Vector3.down);
            AddQuad(v, n, t, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), Vector3.forward);
            AddQuad(v, n, t, P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), Vector3.back);
            AddQuad(v, n, t, P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1), Vector3.right);
            AddQuad(v, n, t, P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), Vector3.left);
        }

        /// <summary>Adds a quad (corners in order around the edge) wound so its front faces <paramref name="facing"/>.</summary>
        static void AddQuad(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 facing)
        {
            AddTriangle(v, n, t, a, b, c, facing);
            AddTriangle(v, n, t, a, c, d, facing);
        }

        static void AddTriangle(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 facing)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a); // Unity: clockwise front faces -> this points to the front
            if (Vector3.Dot(normal, facing) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }
            normal.Normalize();
            int i = v.Count;
            v.Add(a);
            v.Add(b);
            v.Add(c);
            n.Add(normal);
            n.Add(normal);
            n.Add(normal);
            t.Add(i);
            t.Add(i + 1);
            t.Add(i + 2);
        }
    }
}
