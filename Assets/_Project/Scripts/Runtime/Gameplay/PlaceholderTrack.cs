using System.Collections.Generic;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Grey-box stand-ins built from primitives: road chunks, obstacles, a road-wide railing and gifts.
    /// Used when the TrackTheme has no art yet, and by tests. Follows the RoadChunk conventions.
    /// </summary>
    public static class PlaceholderTrack
    {
        public const float ChunkLength = 40f;
        public const float HalfWidth = 4f;

        static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
        static Material _base;

        public static readonly Color Road = new Color(0.33f, 0.34f, 0.37f);
        public static readonly Color Line = new Color(0.95f, 0.95f, 0.9f);
        public static readonly Color Footpath = new Color(0.72f, 0.7f, 0.66f);
        public static readonly Color Cone = new Color(1f, 0.5f, 0.1f);
        public static readonly Color Barrier = new Color(0.2f, 0.45f, 0.95f);
        public static readonly Color Rail = new Color(0.85f, 0.15f, 0.15f);
        public static readonly Color GiftBox = new Color(1f, 0.83f, 0.15f);
        public static readonly Color Ribbon = new Color(0.9f, 0.2f, 0.35f);

        public const string PlaceholderMaterialName = "CityScooterPlaceholder";

        public static Material MaterialFor(Color c)
        {
            if (Materials.TryGetValue(c, out Material m) && m != null) return m;
            if (_base == null)
            {
                // A URP Lit material in Resources/ so its shader is always in the build. A primitive's default
                // material is fine in the Editor but renders magenta in Web builds (URP 17 keeps its default
                // materials editor-only). Created by CityScooter/Gameplay setup (GameplaySetup.EnsurePlaceholderMaterial).
                _base = Resources.Load<Material>(PlaceholderMaterialName);
                if (_base == null)
                {
                    Debug.LogWarning("[CityScooter] Resources/" + PlaceholderMaterialName + " missing; placeholders may render magenta in builds.");
                    GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _base = probe.GetComponent<Renderer>().sharedMaterial;
                    SafeDestroy(probe);
                }
            }
            m = new Material(_base) { color = c, name = "Placeholder " + ColorUtility.ToHtmlStringRGB(c) };
            Materials[c] = m;
            return m;
        }

        static GameObject Box(string name, Transform parent, Vector3 centre, Vector3 size, Color color, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
            if (!collider) SafeDestroy(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>variant 0/1: plain street; variant 2: has a road-wide jump slot in the middle.</summary>
        public static RoadChunk CreateChunk(int variant, int seed)
        {
            var root = new GameObject("Chunk_Placeholder_" + variant);
            var chunk = root.AddComponent<RoadChunk>();
            chunk.length = ChunkLength;
            chunk.halfWidth = HalfWidth;
            Transform t = root.transform;

            Box("Road", t, new Vector3(0f, -0.05f, ChunkLength / 2f), new Vector3(HalfWidth * 2f + 0.4f, 0.1f, ChunkLength), Road, false);
            for (int i = 0; i < 4; i++) // dashed centre line
                Box("Line", t, new Vector3(0f, 0.005f, 5f + i * 10f), new Vector3(0.15f, 0.01f, 4f), Line, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Footpath", t, new Vector3(side * (HalfWidth + 1.4f), 0.1f, ChunkLength / 2f), new Vector3(2.4f, 0.2f, ChunkLength), Footpath, false);
                var rng = new System.Random(seed * 31 + side);
                for (float z = 2f; z < ChunkLength - 2f; z += 6f + (float)rng.NextDouble() * 4f)
                {
                    float h = 4f + (float)rng.NextDouble() * 14f;
                    float w = 4f + (float)rng.NextDouble() * 3f;
                    float g = 0.55f + (float)rng.NextDouble() * 0.35f;
                    Box("Building", t, new Vector3(side * (HalfWidth + 3f + w / 2f), h / 2f, z + 2.5f), new Vector3(w, h, 5f), new Color(g, g * 0.97f, g * 0.92f), false);
                }
            }

            float[] lanes = { -3f, -1.5f, 0f, 1.5f, 3f };
            float[] rows = variant == 2 ? new[] { 8f, 32f } : new[] { 8f, 16f, 24f, 32f };
            foreach (float z in rows)
                foreach (float x in lanes)
                    AddSlot(t, new Vector3(x, 0f, z), SpawnKind.Any);
            if (variant == 2) AddSlot(t, new Vector3(0f, 0f, 20f), SpawnKind.JumpObstacle);
            return chunk;
        }

        static void AddSlot(Transform parent, Vector3 pos, SpawnKind kind)
        {
            var go = new GameObject("Slot_" + kind);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<SpawnSlot>().kind = kind;
        }

        /// <summary>kind 0: low cone block you can jump; 1: tall barrier you must steer around.</summary>
        public static Obstacle CreateObstacle(int kind)
        {
            var root = new GameObject(kind == 0 ? "Obstacle_Cone" : "Obstacle_Barrier");
            var o = root.AddComponent<Obstacle>();
            if (kind == 0)
            {
                Box("Body", root.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.8f), Cone, true);
                o.canJumpOver = true;
            }
            else
            {
                Box("Body", root.transform, new Vector3(0f, 0.8f, 0f), new Vector3(1.3f, 1.6f, 0.5f), Barrier, true);
                o.canJumpOver = false;
            }
            return o;
        }

        public static Obstacle CreateRailing()
        {
            var root = new GameObject("Obstacle_Railing");
            var o = root.AddComponent<Obstacle>();
            o.canJumpOver = true;
            o.spansRoad = true;
            Box("Bar", root.transform, new Vector3(0f, 0.85f, 0f), new Vector3(HalfWidth * 2f + 0.4f, 0.12f, 0.12f), Rail, false);
            for (int i = -2; i <= 2; i++)
                Box("Post", root.transform, new Vector3(i * 2f, 0.45f, 0f), new Vector3(0.1f, 0.9f, 0.1f), Rail, false);
            var col = root.AddComponent<BoxCollider>(); // deep enough that fast riders cannot skip it
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(HalfWidth * 2f + 0.4f, 1f, 0.6f);
            return o;
        }

        public static Gift CreateGift()
        {
            var root = new GameObject("Gift_Placeholder");
            var gift = root.AddComponent<Gift>();
            Transform box = Box("Box", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 0.6f, 0.6f), GiftBox, false).transform;
            Box("RibbonA", box, Vector3.zero, new Vector3(1.05f, 1.05f, 0.2f), Ribbon, false);
            Box("RibbonB", box, Vector3.zero, new Vector3(0.2f, 1.05f, 1.05f), Ribbon, false);
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.9f, 0f);
            trigger.radius = 0.6f;
            root.AddComponent<Spin>();
            return gift;
        }

        /// <summary>Blue capsule rider on a scooter deck.</summary>
        public static GameObject CreateRiderModel(Transform parent)
        {
            var model = new GameObject("Model");
            model.transform.SetParent(parent, false);
            Box("Deck", model.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.28f, 0.06f, 0.9f), new Color(0.15f, 0.15f, 0.18f), false);
            Box("Stem", model.transform, new Vector3(0f, 0.6f, 0.4f), new Vector3(0.06f, 0.95f, 0.06f), new Color(0.75f, 0.75f, 0.8f), false);
            Box("Bars", model.transform, new Vector3(0f, 1.05f, 0.4f), new Vector3(0.55f, 0.05f, 0.05f), new Color(0.75f, 0.75f, 0.8f), false);
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            SafeDestroy(body.GetComponent<Collider>());
            body.transform.SetParent(model.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.95f, -0.05f);
            body.transform.localScale = new Vector3(0.5f, 0.65f, 0.4f);
            body.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(0.15f, 0.5f, 0.95f));
            return model;
        }

        static void SafeDestroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }
    }
}
