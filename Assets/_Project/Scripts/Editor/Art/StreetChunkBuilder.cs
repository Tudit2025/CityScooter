using System;
using System.IO;
using CityScooter.Track;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityScooter.EditorTools.Art
{
    /// <summary>
    /// CityScooter/Art/Rebuild Street Chunks: generates Prefabs/Road/RoadChunk_Street_A/B/C from kit prefabs.
    ///
    /// Every Street chunk shares the same cross-section so any two chunks butt together at z = 0 / z = 40:
    ///   |x| 0..4   asphalt at y = 0 (RoadChunk.halfWidth = 4, the lane the rider steers in)
    ///   |x| 4..5   curb strip, part of the Kenney road tile, top at y = 0.1
    ///   |x| 5..8   footpath (tile-low stretched), top at y = 0.1
    ///   |x| 8..40  lot: paved (tile-low) or lawn (palette quad), y = 0.1; buildings start at |x| >= 8.5
    /// Nothing but road tiles is placed inside |x| &lt; 5. Edit the layout methods below and re-run; hand-made
    /// chunks should live in another file name because the generated ones are overwritten.
    /// Batchmode: -executeMethod CityScooter.EditorTools.Art.StreetChunkBuilder.RebuildBatch
    /// </summary>
    public static class StreetChunkBuilder
    {
        public const string MenuPath = "CityScooter/Art/Rebuild Street Chunks";
        public const float Length = 40f;
        public const float HalfWidth = 4f;
        public const float CurbOuter = 5f;
        public const float FootpathOuter = 8f;
        public const float LotOuter = 40f;
        public const float WalkHeight = KitRules.CurbHeight;
        public const float ShopSetback = 8.5f;
        public const float HouseSetback = 12f;
        public const float BackgroundSetback = 21f;

        const string Roads = "CityKitRoads";
        const string Shops = "CityKitCommercial";
        const string Suburb = "CityKitSuburban";

        public const string LawnMeshPath = "Assets/_Project/Art/Generated/Meshes/PaletteQuad_Lawn.asset";
        // Flat green cell of the Kenney City Kit (Suburban) colormap (Unity UVs, origin bottom-left).
        static readonly Vector2 LawnUv = new Vector2(0.03f, 0.63f);

        public static readonly string[] ChunkNames = { "RoadChunk_Street_A", "RoadChunk_Street_B", "RoadChunk_Street_C" };

        [MenuItem(MenuPath, priority = 101)]
        static void RebuildMenu() => Rebuild();

        public static void RebuildBatch() => EditorApplication.Exit(Rebuild() ? 0 : 1);

        public static string ChunkPath(string name) => ArtPaths.RoadPrefabRoot + "/" + name + ".prefab";

        public static bool Rebuild()
        {
            try
            {
                Build(ChunkNames[0], 0, LayoutA);
                Build(ChunkNames[1], 0, LayoutB);
                Build(ChunkNames[2], 1, LayoutC);
            }
            catch (Exception e)
            {
                Debug.LogError("[CityScooter] Rebuild Street Chunks failed: " + e);
                return false;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[CityScooter] Rebuild Street Chunks: built " + ChunkNames.Length + " chunks in " + ArtPaths.RoadPrefabRoot);
            return true;
        }

        static void Build(string name, int minDifficulty, Action<Layout> layout)
        {
            string path = ChunkPath(name);
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            try
            {
                RoadChunk chunk = PrefabBuildUtility.GetOrAdd<RoadChunk>(root);
                chunk.length = Length;
                chunk.halfWidth = HalfWidth;
                chunk.roadType = RoadType.Street;
                chunk.weight = 1f;
                chunk.minDifficulty = minDifficulty;

                var groups = new ChildSync(root.transform);
                var l = new Layout(
                    groups.Plain("Road").transform,
                    groups.Plain("Ground").transform,
                    groups.Plain("Buildings").transform,
                    groups.Plain("Props").transform,
                    groups.Plain("SpawnSlots").transform);
                groups.Finish();

                layout(l);
                l.Finish();
            }
            catch
            {
                PrefabUtility.UnloadPrefabContents(root);
                throw;
            }
            PrefabBuildUtility.SaveAndClose(root, path);
        }

        // ------------------------------------------------------------------ layouts

        /// <summary>A: city block. Shops both sides on a paved lot, towers behind, lamps, café parasols.</summary>
        static void LayoutA(Layout l)
        {
            l.StandardRoad(crossingTileAt: -1);
            l.PavedLot(-1);
            l.PavedLot(+1);

            l.BuildingRow(-1, ShopSetback, 0.4f, 0.4f, Shops, "building-k", "building-f", "building-a");
            l.BuildingRow(+1, ShopSetback, 0.8f, 0.6f, Shops, "building-e", "building-g", "building-h");
            l.BuildingRow(-1, BackgroundSetback, 2f, 2f, Shops, "building-skyscraper-e", "low-detail-building-wide-a", "low-detail-building-a");
            l.BuildingRow(+1, BackgroundSetback, 3f, 3f, Shops, "low-detail-building-f", "building-skyscraper-a", "low-detail-building-a");

            l.Lamp(-1, 12f, "light-square");
            l.Lamp(+1, 30f, "light-square");
            l.Prop(Shops, "detail-parasol-a", 6.8f, 5f, 0f);
            l.Prop(Shops, "detail-parasol-a", 6.8f, 10.5f, 30f);
            l.Prop(Suburb, "planter", -6.9f, 4.5f, 90f);
            l.Prop(Suburb, "tree-small", -7.1f, 26f, 0f);
            l.Prop(Suburb, "tree-small", -7.1f, 36f, 40f);
            l.Prop(Suburb, "tree-small", 7.1f, 22f, 0f);

            l.SlotRow(4f, (-2.5f, SpawnKind.Any), (0f, SpawnKind.Gift), (2.5f, SpawnKind.Any));
            l.SlotRow(12f, (-2.5f, SpawnKind.Obstacle), (0f, SpawnKind.Any), (2.5f, SpawnKind.Gift));
            l.SlotRow(20f, (-3f, SpawnKind.Any), (-1f, SpawnKind.Gift), (1f, SpawnKind.Gift), (3f, SpawnKind.Any));
            l.SlotRow(28f, (-2.5f, SpawnKind.Gift), (0f, SpawnKind.Obstacle), (2.5f, SpawnKind.Any));
            l.SlotRow(36f, (-2.5f, SpawnKind.Any), (0f, SpawnKind.Gift), (2.5f, SpawnKind.Obstacle));
        }

        /// <summary>B: Perth suburb. Lawns, houses set back behind front yards, verge trees, side fences.</summary>
        static void LayoutB(Layout l)
        {
            l.StandardRoad(crossingTileAt: -1);
            l.Lawn(-1);
            l.Lawn(+1);

            float[] left = l.BuildingRow(-1, HouseSetback, 1.5f, 4f, Suburb, "building-type-a", "building-type-k", "building-type-t");
            float[] right = l.BuildingRow(+1, HouseSetback, 1.2f, 3f, Suburb, "building-type-c", "building-type-h", "building-type-p");
            l.SideFences(-1, HouseSetback, left);
            l.SideFences(+1, HouseSetback, right);

            l.Prop(Suburb, "tree-large", -7.2f, 7f, 0f);
            l.Prop(Suburb, "tree-small", -7.2f, 20f, 30f);
            l.Prop(Suburb, "tree-large", -7.2f, 33f, 60f);
            l.Prop(Suburb, "tree-small", 7.2f, 13f, 0f);
            l.Prop(Suburb, "tree-large", 7.2f, 27f, 45f);
            l.Lamp(-1, 26f, "light-curved");
            l.Lamp(+1, 4f, "light-curved");
            l.Prop(Suburb, "planter", -10f, 6f, 90f);
            l.Prop(Suburb, "planter", 10f, 20f, -90f);

            l.SlotRow(4f, (-2.5f, SpawnKind.Gift), (0f, SpawnKind.Any), (2.5f, SpawnKind.Gift));
            l.SlotRow(12f, (-2.5f, SpawnKind.Any), (0f, SpawnKind.Obstacle), (2.5f, SpawnKind.Any));
            l.SlotRow(20f, (-3f, SpawnKind.Gift), (-1.5f, SpawnKind.Any), (0f, SpawnKind.Gift), (1.5f, SpawnKind.Any), (3f, SpawnKind.Gift));
            l.SlotRow(28f, (-2.5f, SpawnKind.Obstacle), (0f, SpawnKind.Gift), (2.5f, SpawnKind.Any));
            l.SlotRow(36f, (-2.5f, SpawnKind.Any), (0f, SpawnKind.Any), (2.5f, SpawnKind.Obstacle));
        }

        /// <summary>C: shops left, houses right, zebra crossing, roadworks with a road-wide railing to jump.</summary>
        static void LayoutC(Layout l)
        {
            l.StandardRoad(crossingTileAt: 1);
            l.PavedLot(-1);
            l.Lawn(+1);

            l.BuildingRow(-1, ShopSetback, 0.5f, 0.5f, Shops, "building-c", "building-g", "building-h", "building-a");
            l.BuildingRow(-1, BackgroundSetback, 4f, 3f, Shops, "low-detail-building-a", "low-detail-building-wide-a", "building-skyscraper-a");
            float[] right = l.BuildingRow(+1, HouseSetback, 1f, 7f, Suburb, "building-type-p", "building-type-t");
            l.BuildingRow(+1, HouseSetback, 31.5f, 0f, Suburb, "building-type-k");
            l.SideFences(+1, HouseSetback, right);

            // Pedestrian crossing (road tile 1 = z 10..20).
            l.Prop(Roads, "traffic-light", -5.6f, 9.4f, 0f);
            l.Prop(Roads, "traffic-light", 5.6f, 20.6f, 180f);
            l.Prop(Roads, "road-sign-warning", 5.6f, 6f, 180f);

            // Roadworks around the JumpObstacle slot at z = 28.
            l.Prop(Roads, "construction-light", -5.4f, 25.5f, 0f);
            l.Prop(Roads, "construction-light", -5.4f, 30.5f, 0f);
            l.Prop(Roads, "construction-light", 5.4f, 25.5f, 0f);
            l.Prop(Roads, "construction-light", 5.4f, 30.5f, 0f);

            l.Lamp(+1, 36f, "light-square");
            l.Prop(Suburb, "tree-small", 7.2f, 33f, 0f);
            l.Prop(Shops, "detail-parasol-a", -6.8f, 34f, 0f);

            l.SlotRow(4f, (-2.5f, SpawnKind.Any), (0f, SpawnKind.Gift), (2.5f, SpawnKind.Any));
            l.SlotRow(12f, (-2.5f, SpawnKind.Gift), (0f, SpawnKind.Any), (2.5f, SpawnKind.Obstacle));
            l.SlotRow(20f, (-2.5f, SpawnKind.Obstacle), (0f, SpawnKind.Gift), (2.5f, SpawnKind.Any));
            l.SlotRow(28f, (0f, SpawnKind.JumpObstacle));
            l.SlotRow(34f, (-2f, SpawnKind.Gift), (0f, SpawnKind.Gift), (2f, SpawnKind.Gift));
        }

        // ------------------------------------------------------------------ layout helpers

        sealed class Layout
        {
            readonly ChildSync _road, _ground, _buildings, _props, _slots;
            int _propIndex, _buildingIndex;

            public Layout(Transform road, Transform ground, Transform buildings, Transform props, Transform slots)
            {
                _road = new ChildSync(road);
                _ground = new ChildSync(ground);
                _buildings = new ChildSync(buildings);
                _props = new ChildSync(props);
                _slots = new ChildSync(slots);
            }

            public void Finish()
            {
                _road.Finish();
                _ground.Finish();
                _buildings.Finish();
                _props.Finish();
                _slots.Finish();
            }

            static GameObject KitPrefab(string kit, string model)
            {
                string path = ArtPaths.KitPrefabFolder(kit) + "/" + model + ".prefab";
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) throw new InvalidOperationException("Kit prefab missing (run " + KitPrefabBuilder.MenuPath + "): " + path);
                return go;
            }

            /// <summary>Four 10 m road tiles; <paramref name="crossingTileAt"/> (0..3) becomes a zebra crossing, -1 = none.</summary>
            public void StandardRoad(int crossingTileAt)
            {
                for (int i = 0; i < 4; i++)
                {
                    string model = i == crossingTileAt ? "road-crossing" : "road-straight";
                    GameObject t = _road.Instance("Road_" + i + "_" + model, KitPrefab(Roads, model));
                    PrefabBuildUtility.SetTransform(t.transform, new Vector3(0f, 0f, 5f + 10f * i), 0f, Vector3.one);
                }
                // Footpaths are part of the standard cross-section on both sides.
                Footpath(-1);
                Footpath(+1);
            }

            void Footpath(int side)
            {
                Slab(side < 0 ? "Footpath_L" : "Footpath_R", side, CurbOuter, FootpathOuter);
            }

            public void PavedLot(int side)
            {
                Slab(side < 0 ? "Lot_L" : "Lot_R", side, FootpathOuter, LotOuter);
            }

            /// <summary>tile-low (10 x 10 m, top at walk height) stretched to cover |x| inner..outer, z 0..40.</summary>
            void Slab(string name, int side, float inner, float outer)
            {
                GameObject t = _ground.Instance(name, KitPrefab(Roads, "tile-low"));
                float width = outer - inner;
                PrefabBuildUtility.SetTransform(t.transform, new Vector3(side * (inner + width * 0.5f), 0f, Length * 0.5f), 0f,
                    new Vector3(width / 10f, 1f, Length / 10f));
            }

            public void Lawn(int side)
            {
                GameObject go = _ground.Plain(side < 0 ? "Lawn_L" : "Lawn_R");
                float width = LotOuter - FootpathOuter;
                PrefabBuildUtility.SetTransform(go.transform, new Vector3(side * (FootpathOuter + width * 0.5f), WalkHeight, Length * 0.5f), 0f,
                    new Vector3(width, 1f, Length));
                PrefabBuildUtility.GetOrAdd<MeshFilter>(go).sharedMesh = PaletteMeshes.Quad(LawnMeshPath, LawnUv);
                var mr = PrefabBuildUtility.GetOrAdd<MeshRenderer>(go);
                mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ArtPaths.KitMaterialPath("Kenney", Suburb));
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = true;
            }

            /// <summary>
            /// Places buildings one after another along +Z, fronts facing the road, front faces at |x| = setback.
            /// Returns [zStart0, zEnd0, zStart1, zEnd1, ...] of the placed footprints.
            /// </summary>
            public float[] BuildingRow(int side, float setback, float zStart, float gap, string kit, params string[] models)
            {
                var extents = new float[models.Length * 2];
                float z = zStart;
                for (int i = 0; i < models.Length; i++)
                {
                    GameObject prefab = KitPrefab(kit, models[i]);
                    float yaw = side < 0 ? 90f : -90f; // kit prefabs face +Z; turn them to face the road
                    Bounds rb = RotatedFootprint(prefab, yaw);
                    float x = side < 0 ? -setback - rb.max.x : setback - rb.min.x;
                    float zEnd = z + rb.size.z;
                    if (zEnd > Length - 0.2f)
                        Debug.LogWarning("[CityScooter] " + models[i] + " ends at z = " + zEnd.ToString("F1") + " (past the chunk end).");
                    GameObject b = _buildings.Instance("Building_" + _buildingIndex++ + "_" + models[i], prefab);
                    PrefabBuildUtility.SetTransform(b.transform, new Vector3(x, WalkHeight, z - rb.min.z), yaw, Vector3.one);
                    extents[i * 2] = z;
                    extents[i * 2 + 1] = zEnd;
                    z = zEnd + gap;
                }
                return extents;
            }

            /// <summary>Two fence panels (6.7 m) running back from the front line in each gap between houses.</summary>
            public void SideFences(int side, float setback, float[] row)
            {
                for (int i = 1; i + 1 < row.Length; i += 2)
                {
                    float zGap = (row[i] + row[i + 1]) * 0.5f;
                    if (row[i + 1] - row[i] < 0.6f) continue;
                    for (int p = 0; p < 2; p++)
                        Prop(Suburb, "fence", side * (setback + 1.67f + 3.33f * p), zGap, 0f);
                }
            }

            /// <summary>Street lamp on the footpath with its arm reaching over the road (arm is +Z in the kit prefab).</summary>
            public void Lamp(int side, float z, string model)
            {
                Prop(Roads, model, side * 5.5f, z, side < 0 ? 90f : -90f);
            }

            public void Prop(string kit, string model, float x, float z, float yaw)
            {
                if (Mathf.Abs(x) < CurbOuter)
                    Debug.LogWarning("[CityScooter] Prop " + model + " at x = " + x + " is inside the road; keep dressing at |x| >= " + CurbOuter);
                GameObject p = _props.Instance("Prop_" + _propIndex++ + "_" + model, KitPrefab(kit, model));
                PrefabBuildUtility.SetTransform(p.transform, new Vector3(x, WalkHeight, z), yaw, Vector3.one);
            }

            public void SlotRow(float z, params (float x, SpawnKind kind)[] slots)
            {
                foreach ((float x, SpawnKind kind) in slots)
                {
                    GameObject s = _slots.Plain("Slot_z" + z.ToString("00") + "_x" + x.ToString("+0.0;-0.0;0.0") + "_" + kind);
                    PrefabBuildUtility.SetTransform(s.transform, new Vector3(x, 0f, z), 0f, Vector3.one);
                    PrefabBuildUtility.GetOrAdd<SpawnSlot>(s).kind = kind;
                }
            }

            /// <summary>Footprint (BoxCollider of the kit prefab root) after rotating by yaw, relative to the pivot.</summary>
            static Bounds RotatedFootprint(GameObject prefab, float yaw)
            {
                var box = prefab.GetComponent<BoxCollider>();
                Bounds local = box != null ? new Bounds(box.center, box.size) : new Bounds(Vector3.zero, Vector3.one);
                Quaternion q = Quaternion.Euler(0f, yaw, 0f);
                Bounds r = new Bounds(q * local.center, Vector3.zero);
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? local.min.x : local.max.x,
                        (i & 2) == 0 ? local.min.y : local.max.y,
                        (i & 4) == 0 ? local.min.z : local.max.z);
                    r.Encapsulate(q * corner);
                }
                return r;
            }
        }
    }

    /// <summary>
    /// Tiny generated meshes whose UVs all point at one texel of a kit colormap, so new flat colours
    /// (e.g. lawn) reuse an existing kit material instead of adding materials.
    /// </summary>
    public static class PaletteMeshes
    {
        /// <summary>1 x 1 m quad in the XZ plane facing +Y, centred on the origin.</summary>
        public static Mesh Quad(string assetPath, Vector2 uv)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (mesh != null && mesh.vertexCount == 4 && mesh.uv.Length == 4 && mesh.uv[0] == uv) return mesh;

            bool create = mesh == null;
            if (create) mesh = new Mesh { name = Path.GetFileNameWithoutExtension(assetPath) };
            mesh.Clear();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f),
            };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[] { uv, uv, uv, uv };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            if (create)
            {
                PrefabBuildUtility.EnsureFolder(Path.GetDirectoryName(assetPath));
                AssetDatabase.CreateAsset(mesh, assetPath);
            }
            // Read/Write off: the CPU copy is not needed at runtime (Validate Budget checks this).
            var so = new SerializedObject(mesh);
            SerializedProperty readable = so.FindProperty("m_IsReadable");
            if (readable != null && readable.boolValue)
            {
                readable.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(mesh);
            return mesh;
        }
    }
}
