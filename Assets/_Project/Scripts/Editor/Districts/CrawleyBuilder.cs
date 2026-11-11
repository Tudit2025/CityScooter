using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CityScooter.EditorTools.Art;
using CityScooter.Rendering;
using CityScooter.Track;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// v0.3 districts in Crawley, the start of the UWA -> Perth CBD route, all generated in the comic style
    /// (ComicMesh, ComicPalette + ComicInk, vertex colours):
    ///   UWA (order 10): sandstone arcades with terracotta roofs, Winthrop Hall with its clock tower and the reflection
    ///       pond, jacarandas in purple bloom, palms and cypresses, peacocks; a sandstone-steps kicker and a lily-pond
    ///       chunk (ramp + half-road SPLASH gap).
    ///   MATILDA BAY (order 20): the red shared path along the Swan River, lawns with WA peppermint trees, picnic
    ///       tables, BBQ shelters, the blue Crawley Edge boatshed on its jetty, black swans; a timber jetty kicker and a
    ///       river inlet chunk (ramp + road-wide SPLASH gap); far scenery with the river, the far shore and sailboats.
    /// Every chunk keeps the standard street cross-section (docs/v0.3-districts.md). Prefabs are edited in place by
    /// stable child names (GUIDs survive a rebuild).
    /// v0.6 polish (docs/v0.6-polish.md; notes in art-src/districts/uwa|matilda/previews/polish/NOTES.txt): real arcades,
    /// Winthrop Hall from photographs, a campus gate as UWA's entry, UWA's exit and Matilda Bay's entry as one riverside
    /// campus edge, Matilda Bay's exit climbing into Kings Park; sidewalk surfaces change inside the entry / exit chunks
    /// (Chunk.WalkAt / RedAt / BorderAt, Band, PathWords); one far-scenery prefab for both districts (SharedFarName).
    ///   Menu: CityScooter/Districts/Build UWA + Matilda Bay
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.CrawleyBuilder.BuildBatch
    /// </summary>
    public static class CrawleyBuilder
    {
        public const string DataFolder = "Assets/_Project/Data/Districts";
        public const string UwaAssetPath = DataFolder + "/District_UWA.asset";
        public const string MatildaAssetPath = DataFolder + "/District_Matilda.asset";
        public const int TriangleBudget = 25000, RendererBudget = 8;
        public const float MatildaGrade = .04f; // riverside: flatter than the street's 4.5 degrees
        const float Half = 4f, Walk = 4f, Kerb = .2f;

        public static string PrefabFolder(string tag) => ComicCityBuilder.Prefabs + "/Districts/" + tag;
        static string MeshFolder(string tag) => ComicCityBuilder.Root + "/Districts/" + tag;
        public static string ChunkPath(string tag, string variant) => PrefabFolder(tag) + "/CHN_" + tag + "_" + variant + ".prefab";
        static string PrefabPath(string tag, string name) => PrefabFolder(tag) + "/" + name + ".prefab";

        public const string SharedFarName = "PRP_Crawley_FarRiverside";
        static GameObject _sharedFar;
        static ComicCityRecipe _r;
        static Material _material, _ink;
        static string _tag;
        static readonly List<string> Report = new List<string>();

        [MenuItem("CityScooter/Districts/Build UWA + Matilda Bay")]
        public static void Build()
        {
            Load();
            PrefabBuildUtility.EnsureFolder(DataFolder);
            Report.Clear();
            BuildUwa();
            BuildMatilda();
            AssetDatabase.SaveAssets();
            Debug.Log("[Crawley] built " + string.Join("; ", Report));
        }

        static void Load()
        {
            _r = AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(ComicCityBuilder.RecipePath);
            _material = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicPalette.mat");
            _ink = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicInk.mat");
            if (_r == null || _material == null || _ink == null) throw new InvalidOperationException("Build the comic city first (recipe, ComicPalette.mat, ComicInk.mat).");
            ComicSurfaceAssets.EnsureLoaded();
            ParkRiverKit.Init(); // v0.6: Matilda Bay's exit borrows Kings Park's gums, grass trees and limestone (read only)
        }

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Builds both districts, then renders the props atlases (needs a graphics device: no -nographics).</summary>
        public static void BuildAndRenderAtlasBatch()
        {
            try { Build(); RenderAtlas(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        // ---- props atlas (same framing, background and light as ComicCityPreview's 03-props.png)

        [MenuItem("CityScooter/Districts/Render UWA + Matilda Bay props atlas")]
        public static void RenderAtlas()
        {
            Load();
            ArtPreviewUtility.NewPreviewScene();
            Sheet("art-src/districts/uwa/props-atlas.png", new List<(string, ComicMesh, float)>
            {
                ("Jacaranda", Jacaranda(11), 0f), ("JacarandaB", Jacaranda(42), 0f), ("Palm", Palm(70, 8f), 0f), ("Cypress", CypressMesh(1f), 0f),
                ("CampusLamp", CampusLamp(), 0f), ("Bench", BenchMesh(BenchRed), 0f), ("StoneBench", StoneBenchMesh(), 0f), ("BikeRack", BikeRackMesh(), 0f),
                ("AFrameSign", AFrameSign(), 180f), ("Sprinkler", Sprinkler(), 0f), ("Peacock", PeacockMesh(false), 150f), ("PeacockFan", PeacockMesh(true), 180f),
                ("SandstoneSteps", StepsRampMesh(3f, 5f, 1.3f, 0f, 0f), 0f), ("CampusBlock", CampusBlock(8.2f, 10f, 8.5f, 2, true, 1), 0f),
                ("Cloister", CampusBlock(9.7f, 6.5f, 4.8f, 1, true, 2), 0f), ("WinthropHall", WinthropHall(), 0f),
                ("MoretonBayFig", FigMesh(9, 17f), 0f), ("GatePylon", GatePylon(1), 180f), ("StreetSign", StreetSign("HACKETT DR"), 180f),
            });
            Sheet("art-src/districts/matilda/props-atlas.png", new List<(string, ComicMesh, float)>
            {
                ("Peppermint", PeppermintMesh(201), 0f), ("PeppermintB", PeppermintMesh(213), 0f), ("RiversideLamp", RiversideLamp(1), 0f), ("Bench", BenchMesh(PicnicWood), 0f),
                ("PicnicTable", PicnicTable(), 0f), ("BbqShelter", BbqShelter(), 0f), ("Umbrella", Umbrella(0), 0f), ("SailBoat", SailBoat(2), 60f),
                ("Esky", Esky(), 180f), ("PicnicBasket", PicnicBasket(), 180f), ("BlackSwan", SwanMesh(), 150f), ("Boatshed", Boatshed(), 0f),
                ("Jetty", JettyRampMesh(3f, 5f, 1.3f, 0f, 0f, 0f), 0f), ("CafePavilion", CafePavilion(), 0f),
                ("MooredYacht", MooredYacht(2), 60f), ("Eliza", Eliza(), 180f),
            });
        }

        static void Sheet(string path, List<(string name, ComicMesh mesh, float yaw)> items)
        {
            const int tile = 300, cols = 6;
            int rows = Mathf.CeilToInt(items.Count / (float)cols);
            var sheet = new Texture2D(cols * tile, rows * tile, TextureFormat.RGB24, false);
            sheet.SetPixels(Enumerable.Repeat(new Color(.82f, .87f, .9f), sheet.width * sheet.height).ToArray());
            for (int i = 0; i < items.Count; i++)
            {
                Mesh mesh = items[i].mesh.Bake(items[i].name);
                var go = new GameObject(items[i].name);
                go.transform.rotation = Quaternion.Euler(0, items[i].yaw, 0);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = _material;
                var ink = new GameObject("InkOutline");
                ink.transform.SetParent(go.transform, false);
                ink.AddComponent<MeshFilter>().sharedMesh = mesh;
                var imr = ink.AddComponent<MeshRenderer>();
                imr.sharedMaterial = _ink; imr.shadowCastingMode = ShadowCastingMode.Off;
                Bounds b = ArtPreviewUtility.WorldBounds(go);
                float extent = Mathf.Max(b.size.x, b.size.y, b.size.z);
                Vector3 aim = b.center;
                Camera cam = ArtPreviewUtility.CreateCamera(aim + new Vector3(1, .65f, 1.4f).normalized * extent * 2, aim, 32, 250);
                cam.orthographic = true; cam.orthographicSize = Mathf.Max(.6f, extent * .62f);
                Object.DestroyImmediate(ArtPreviewUtility.Render(cam, tile, tile));
                Texture2D tex = ArtPreviewUtility.Render(cam, tile, tile);
                sheet.SetPixels((i % cols) * tile, (rows - 1 - i / cols) * tile, tile, tile, tex.GetPixels());
                Object.DestroyImmediate(tex); Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(go); Object.DestroyImmediate(mesh);
            }
            sheet.Apply();
            ArtPreviewUtility.SavePng(sheet, path);
            Object.DestroyImmediate(sheet);
        }

        // =====================================================================================================
        // UWA
        // =====================================================================================================

        static readonly Color Lime = C(0xE6C893), LimeShade = C(0xD2AE76), LimeTrim = C(0xF3E0B4), Plinth = C(0xC39A63),
            Terracotta = C(0xCB5A30), TerracottaDark = C(0xA4442A), ArchDark = C(0x5E4733), WindowDark = C(0x3A4A5E),
            UwaPaving = C(0xDDCDAA), Brick = C(0xA8523E), Grass = C(0x6FAE48), LawnDark = C(0x5C9A3C),
            J1 = C(0x8E6CDF), J2 = C(0xA98AF0), J3 = C(0xC9B4FA), Petals = C(0xB59BEE), Trunk = C(0x5E4B3E),
            Cypress = C(0x2F5B3B), Hedge = C(0x4F8439), PalmLeaf = C(0x4E8B3A), PalmTrunk = C(0x8C7862), ClockBlue = C(0x2B4D8F),
            PondWater = C(0x7FB8DE), Peacock = C(0x1E4FB4), PeacockNeck = C(0x1C78B8), PeacockTail = C(0x2E7C48), PeacockGold = C(0xDDB64A),
            UwaBlue = C(0x27407A), BenchRed = C(0xB86333);

        static void BuildUwa()
        {
            Begin("Uwa");
            float g = _r.downhillGrade;
            var chunks = new List<RoadChunk>();
            RoadChunk entry = SaveChunk(UwaEntry(g));
            chunks.Add(SaveChunk(UwaArcades(g)));
            chunks.Add(SaveChunk(UwaGrove(g)));
            chunks.Add(SaveChunk(UwaWinthrop(g)));
            chunks.Add(SaveChunk(UwaPondJump(g)));
            chunks.Add(SaveChunk(UwaCloisters(g)));
            RoadChunk exit = SaveChunk(UwaExit(g));

            Ramp steps = SaveRamp("RMP_Uwa_SandstoneSteps", StepsRampMesh(3f, 5f, 1.3f, 0f, g), 3f, 5f, 1.3f, 0f, 1f, .5f);
            var aFrame = SaveObstacle("OBS_Uwa_AFrameSign", AFrameSign(), new Vector3(0, .52f, 0), new Vector3(.95f, 1.04f, .65f), ObstacleKind.Thing, true, true);
            var sprinkler = SaveObstacle("OBS_Uwa_Sprinkler", Sprinkler(), new Vector3(0, .4f, 0), new Vector3(1.5f, .8f, .6f), ObstacleKind.Thing, true, true);
            GameObject peacock = SaveAnimal("OBS_Uwa_Peacock", PeacockMesh(false), new Vector3(0, .6f, .4f), new Vector3(.55f, 1.2f, 1.7f), true, .5f);
            GameObject peacockFan = SaveAnimal("OBS_Uwa_PeacockFan", PeacockMesh(true), new Vector3(0, .8f, .2f), new Vector3(1.9f, 1.6f, .7f), false, .3f);
            // v0.6: UWA and Matilda Bay look over the same river to the same far shore: one far-scenery prefab for both,
            // so it stays up across their boundary (DistrictDirector keeps one instance per prefab).
            GameObject far = SaveFar(SharedFarName, CrawleyFar());
            _sharedFar = far;
            AssetDatabase.DeleteAsset(PrefabPath("Uwa", "PRP_Uwa_FarCampus"));
            AssetDatabase.DeleteAsset(MeshFolder("Uwa") + "/PRP_Uwa_FarCampus.asset");

            var d = LoadOrCreateDistrict(UwaAssetPath);
            d.id = "uwa"; d.displayName = "UWA"; d.order = 10; d.length = new Vector2(420f, 560f);
            d.chunks = chunks.ToArray(); d.orderedChunks = true; d.entry = entry; d.exit = exit; d.allowBoulevard = false;
            var bin = AssetDatabase.LoadAssetAtPath<GameObject>(ComicCityBuilder.Prefabs + "/Gameplay/OBS_Bin.prefab");
            d.obstacles = new[] { aFrame, sprinkler, bin != null ? bin.GetComponent<Obstacle>() : null }.Where(o => o != null).ToArray();
            d.jumpObstacles = Array.Empty<Obstacle>();
            d.gifts = Array.Empty<Gift>();
            d.hazards = Array.Empty<HazardPattern>();
            d.pedestrians = new[] { Person("Student"), Person("Student"), Person("Skater"), Person("Tourist"), Person("Barista"), peacock, peacock, peacockFan }
                .Where(p => p != null).ToArray();
            d.ramps = new[] { steps };
            d.rampFrequency = 1f;
            d.blendMeters = 150f;
            d.fogColor = new Color(.95f, .87f, .80f);          // warm cream with a hint of jacaranda
            d.skyTint = new Color(1f, .96f, 1f);
            d.ambientSky = new Color(.74f, .74f, .93f);
            d.ambientEquator = new Color(.93f, .84f, .74f);
            d.ambientGround = new Color(.62f, .52f, .43f);
            d.sunColor = new Color(1f, .91f, .78f);            // warm afternoon
            d.sunIntensity = 1.4f;
            d.farScenery = far; // gateway (DistrictSetup GW_ sign), ambience and the sky fields are left as they are
            EditorUtility.SetDirty(d);
        }

        static Chunk UwaBase(string id, float g)
        {
            var c = new Chunk(id, g) { WalkLeft = UwaPaving, WalkRight = UwaPaving, BorderColor = Brick };
            return c;
        }

        // Entry (v0.6): the arrival at the university for a rider coming back from the end of either line (Warnbro or
        // Midland). z = 0 is the boundary: the standard street (grey paving, the low limestone wall, lawn, street trees).
        // At z = 12 the campus gate: two limestone pylons with terracotta caps and wing walls, a pair of cypresses each
        // side; behind a brick band the paving turns to campus limestone with its brick border. A jacaranda avenue then
        // leads to the first arcades.
        static Chunk UwaEntry(float g)
        {
            Chunk c = UwaBase("Entry", g);
            const float gate = 12f;
            c.WalkAt = (s, z) => z < gate ? _r.pavement : UwaPaving;
            c.BorderAt = (s, z) => z >= gate;
            for (int s = -1; s <= 1; s += 2)
            {
                c.Band(s, gate, Brick, .6f);
                c.Lawn(s, 8.4f, 75f, 0f, 40f);
                c.LowWall(s, 0f, gate - 1.1f, .55f);
                c.LowWall(s, gate + 1.1f, 40f, .55f);
                c.Pylon(s, gate);
                c.KerbTree(s * 10.6f, 3f); c.KerbTree(s * 10.6f, 8f);
                c.Tree(CypressMesh(1.1f), s * 13.2f, 15f, false);
                c.Tree(CypressMesh(.95f), s * 16.6f, 13.2f, false);
                c.Tree(Jacaranda(11 + s), s * 12.3f, 22f, true);
                c.Tree(Jacaranda(17 + s), s * 12.8f, 33f, true);
                c.Lamp(s * 8.7f, 18f); c.Lamp(s * 8.7f, 36f);
            }
            // The campus begins: a single-storey cloister on the left, a two-storey block on the right.
            c.CampusRow(-1, 16f, 24f, 40f, 8f, 6f, 4.6f, 1, true);
            c.CampusRow(1, 18f, 22f, 40f, 9f, 10f, 8.5f, 2, true);
            c.Far(-1, 3); c.Far(1, 5);
            c.Bench(-7.4f, 20f); c.BikeRack(7.3f, 18f);
            c.Slots(10f, 25f);
            return c;
        }

        // Exit (v0.6): Hackett Drive leaves the buildings and meets Matilda Bay. Left: the campus edge goes on (a cloister
        // row behind its lawn, the low limestone wall, campus paving). Right: the last block ends at z = 12, the lawn
        // opens to peppermint trees, a strip of beach and the river; the red shared path starts at z = 26 behind a stone
        // band (START bar, SHARED PATH). Matilda Bay's entry starts with exactly this cross-section.
        static Chunk UwaExit(float g)
        {
            Chunk c = UwaBase("Exit", g);
            const float path = 26f, open = 14f;
            c.WalkAt = (s, z) => s > 0 && z >= path ? RedPath : UwaPaving;
            c.RedAt = (s, z) => s > 0 && z >= path;
            c.BorderAt = (s, z) => s < 0 || z < path;
            c.Band(1, path, LimeTrim, .6f); c.PathBar(path + 1f); c.PathWords(path + 2.6f, "SHARED", "PATH");
            // left: the campus edge
            c.Lawn(-1, 8.4f, 75f, 0f, 40f);
            c.LowWall(-1, 0f, 40f, .55f);
            c.CampusRow(-1, 16f, 0f, 40f, 10f, 6f, 4.6f, 1, true);
            c.Tree(Jacaranda(10), -12.5f, 8f, true); c.Tree(Jacaranda(16), -12.8f, 29f, true);
            c.Lamp(-8.7f, 12f); c.Lamp(-8.7f, 34f);
            // right: the last block, then the foreshore
            c.Lawn(1, 8.4f, 75f, 0f, open);
            c.LowWall(1, 0f, open, .55f);
            c.CampusRow(1, 18f, 0f, 12f, 12f, 10f, 8.5f, 2, true);
            c.Tree(Jacaranda(12), 12.2f, 5f, true);
            c.Lawn(1, 8f, 15f, open, 40f, BayLawn);
            c.Shore(open, 28f, z => Mathf.Lerp(75f, 15f, Mathf.SmoothStep(0f, 1f, (z - open) / 12f)), BayLawn);
            c.Beach(15f, 17.5f, 28f, 40f);
            c.Water(17.5f, RiverEdge, 28f, 40f, -.45f, RiverMid);
            c.Ripples(22f, 120f, 20f, 40f, -.43f);
            c.Sign(8.9f, open + 1f, "HACKETT DR");
            c.Tree(PeppermintMesh(206), 11.8f, 21f, false); c.Tree(PeppermintMesh(207), 12.2f, 35f, false);
            c.Lamp(8.4f, 31f, true);
            c.Boat(46f, 32f, 3); c.Boat(84f, 24f, 4);
            c.Moored(30f, 36f, 2); c.Moored(38f, 27f, 3);
            c.Far(-1, 3);
            c.Bench(-7.4f, 20f); c.BikeRack(7.3f, 22f);
            c.Slots(10f, 25f);
            return c;
        }

        // Two-storey sandstone buildings with ground-floor arcades on both sides (Hackett-style), jacarandas in the
        // courts and the lawn strip.
        static Chunk UwaArcades(float g)
        {
            Chunk c = UwaBase("Arcades", g);
            c.Paving(-1, 8f, 10f, 0f, 40f);
            c.Lawn(-1, 10f, 75f, 0f, 40f);
            c.CampusRow(-1, 10f, 0f, 17f, 8.5f, 10f, 8.5f, 2, true);
            c.CampusRow(-1, 10f, 23f, 40f, 8.5f, 10f, 8.5f, 2, true);
            c.Tree(Jacaranda(31), -12.5f, 20f, true);
            c.Lawn(1, 8f, 13f, 0f, 40f);
            c.Paving(1, 13f, 14f, 0f, 40f);
            c.Lawn(1, 25f, 75f, 0f, 40f);
            c.CampusRow(1, 14f, 0f, 40f, 10f, 11f, 9f, 2, true);
            c.Tree(Jacaranda(32), 10.8f, 6f, true);
            c.Tree(Jacaranda(33, .8f), 10.5f, 21f, true);
            c.Tree(Jacaranda(34), 11f, 35f, true);
            c.Lamp(-8.6f, 5f); c.Lamp(-8.6f, 30f); c.Lamp(8.5f, 14f); c.Lamp(8.5f, 28f);
            c.Bench(-7.4f, 12f); c.BikeRack(-7.3f, 33f); c.Bench(7.4f, 27f);
            c.Far(-1, 1); c.Far(1, 2);
            c.Slots(10f, 25f);
            return c;
        }

        // Open lawns: a jacaranda avenue with a purple carpet, the tropical grove's palms, stone benches.
        static Chunk UwaGrove(float g)
        {
            Chunk c = UwaBase("Grove", g);
            for (int s = -1; s <= 1; s += 2) { c.Lawn(s, 8.4f, 75f, 0f, 40f); c.LowWall(s, 0f, 40f, .45f); }
            for (int i = 0; i < 4; i++) c.Tree(Jacaranda(40 + i, i % 2 == 0 ? 1f : .8f), 11.5f + i % 2, 4f + i * 10.5f, true);
            for (int i = 0; i < 3; i++) c.Tree(CypressMesh(1f), 22f, 9f + i * 11f, false);
            c.Tree(Jacaranda(60), -13f, 30f, true);
            c.Tree(Jacaranda(61), -12f, 8f, true);
            float[][] palms = { new[] { -16f, 14f }, new[] { -19f, 21f } };
            for (int i = 0; i < palms.Length; i++) c.Tree(Palm(70 + i, 8f + i % 3), palms[i][0], palms[i][1], false);
            c.Tree(CypressMesh(1.1f), -26f, 4f, false);
            c.FigTree(-30f, 27f, 19f, 5); // a Moreton Bay fig behind the palms
            c.StoneBench(16f, 15f); c.StoneBench(16f, 26f); c.StoneBench(-11f, 19f);
            c.CampusRow(1, 30f, 6f, 36f, 10f, 10f, 8.5f, 2, true);
            c.Lamp(-8.7f, 15f); c.Lamp(8.7f, 34f);
            c.Bench(7.4f, 9f); c.BikeRack(-7.3f, 24f);
            c.Far(-1, 4); c.Far(1, 6);
            c.Slots(10f, 25f);
            return c;
        }

        // The landmark: Winthrop Hall with its clock tower, set back behind a lawn and the long reflection pond.
        static Chunk UwaWinthrop(float g)
        {
            Chunk c = UwaBase("WinthropHall", g);
            c.Lawn(-1, 8.4f, 75f, 0f, 40f);
            c.LowWall(-1, 0f, 40f, .45f);
            // Reflection pond parallel to the hall, paved surround.
            c.Paving(-1, 12.5f, 22.5f, 3f, 37f, .24f);
            c.Pond(-21.5f, -13.5f, 5f, 35f);
            ComicMesh hall = WinthropHall();
            // The hall faces the road (+X), its tower at the far (+Z) end; on a terrace at the uphill ground.
            c.Building(hall, -1, 24f, 11.5f, 22f, 17f, 0f, 40f);
            c.Tree(CypressMesh(1.1f), -14f, 2f, false); c.Tree(CypressMesh(1.2f), -22f, 1.5f, false);
            c.Tree(CypressMesh(1.1f), -11f, 38f, false);
            c.Lamp(-8.7f, 8f); c.Lamp(-8.7f, 28f); // (no tree in front of the hall: the pond and the front stay in view)
            // Right: lawn, jacarandas, a cypress row and a cloister.
            c.Lawn(1, 8.4f, 75f, 0f, 40f);
            c.LowWall(1, 0f, 40f, .45f);
            c.Tree(Jacaranda(81), 12f, 5f, true); c.Tree(Jacaranda(83, .85f), 12f, 36f, true);
            c.FigTree(17f, 20f, 17f, 9); // the great Moreton Bay fig between Winthrop Hall and the Arts building
            c.Tree(CypressMesh(1f), 21f, 4f, false); c.Tree(CypressMesh(1f), 21f, 35f, false);
            c.CampusRow(1, 26f, 2f, 38f, 9f, 6f, 4.6f, 1, true);
            c.Lamp(8.7f, 14f); c.Lamp(8.7f, 30f);
            c.BikeRack(7.3f, 24f);
            c.Far(1, 8);
            c.Slots(10f, 25f);
            return c;
        }

        // A lily pond across the right half of the road: sandstone steps up and over it (Ramp), SPLASH if you miss.
        static Chunk UwaPondJump(float g)
        {
            Chunk c = UwaBase("PondJump", g);
            const float rampZ = 12f, rise = 5f, height = 1.4f, top = 1f, gapZ = 18f, gapLen = 6f;
            c.Holes.Add(new Hole(0f, 8f, gapZ, gapZ + gapLen));
            c.Ramps.Add(new RampSpec { X = 4f, Z = rampZ, Width = 8f, Rise = rise, Height = height, Top = top, Launch = 1f, Hang = .5f,
                Mesh = StepsRampMesh(8f, rise, height, top, g, 1.6f) });
            c.Gaps.Add(new GapSpec { X = 4f, Z = gapZ, Length = gapLen, Width = 8f, Word = "SPLASH", Water = true });
            // The pond itself: water under the gap, running out to the right into a lily pond on the lawn.
            c.Water(0f, 26f, gapZ - .3f, gapZ + gapLen + .3f, -.55f, PondWater);
            c.Lilies(1f, 24f, gapZ, gapZ + gapLen, -.53f);
            c.PondRim(8.4f, 26f, gapZ, gapZ + gapLen);
            c.Lawn(1, 8.4f, 75f, 0f, gapZ);
            c.Lawn(1, 8.4f, 75f, gapZ + gapLen, 40f);
            c.Lawn(1, 26f, 75f, gapZ, gapZ + gapLen);
            c.LowWall(1, 0f, gapZ - .3f, .45f); c.LowWall(1, gapZ + gapLen + .3f, 40f, .45f);
            c.Tree(Jacaranda(90), 13f, 8f, true); c.Tree(Jacaranda(91), 12.5f, 33f, true);
            c.Tree(Palm(92, 9f), 29f, 21f, false);
            // Left: a cloister behind a lawn strip.
            c.Lawn(-1, 8.4f, 75f, 0f, 40f);
            c.LowWall(-1, 0f, 40f, .45f);
            c.CampusRow(-1, 13f, 0f, 40f, 10f, 7f, 5f, 1, true);
            c.Tree(Jacaranda(93), -10.8f, 20f, true);
            c.Lamp(-8.7f, 6f); c.Lamp(-8.7f, 32f); c.Lamp(8.7f, 34f);
            c.NoStreetLife(4f, 8f, rampZ - 1f, gapZ + gapLen + 1f);
            c.Far(-1, 7); c.Far(1, 9);
            c.SlotRow(6f, -2.6f, 0f);
            c.SlotRow(32f, -2.6f, 0f, 2.6f);
            return c;
        }

        // Arcades again, single-storey cloisters with a jacaranda-filled quad and palms.
        static Chunk UwaCloisters(float g)
        {
            Chunk c = UwaBase("Cloisters", g);
            for (int s = -1; s <= 1; s += 2) c.Paving(s, 8f, 9.5f, 0f, 40f);
            c.Lawn(-1, 9.5f, 75f, 0f, 40f); c.Lawn(1, 9.5f, 75f, 0f, 40f);
            c.CampusRow(-1, 9.5f, 0f, 40f, 10f, 6.5f, 4.8f, 1, true);
            c.CampusRow(-1, 22f, 0f, 40f, 10f, 10f, 9f, 2, false);
            c.Tree(Jacaranda(100), 11.8f, 5f, true); c.Tree(Jacaranda(101), 12.5f, 18f, true);
            c.Tree(Palm(102, 9f), 11.5f, 28f, false); c.Tree(Palm(103, 10f), 13.5f, 31f, false);
            c.Tree(Jacaranda(104), 12f, 37f, true);
            c.CampusRow(1, 18f, 0f, 40f, 8f, 10f, 8.5f, 2, true);
            c.Lamp(8.5f, 9f); c.Lamp(8.5f, 24f);
            c.Bench(7.4f, 14f); c.BikeRack(7.3f, 34f); c.Bench(-7.4f, 22f);
            // The Arts building's peacocks on the quad's lawn (scenery behind the sidewalk, out of reach).
            c.Prop(PeacockMesh(true), 10.3f, 22.5f, 90f); c.Prop(PeacockMesh(false), 10.6f, 9.5f, 40f);
            c.Far(-1, 10); c.Far(1, 11);
            c.Slots(10f, 25f);
            return c;
        }

        // ---- UWA parts (local: pivot bottom centre, front faces +Z, width along X)

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        // Sandstone block (v0.6): rock-faced limestone, a real ground-floor arcade (round arches on stout round columns,
        // the walk and the ground-floor wall in shade behind them), a string course, arched upper windows on sills, a
        // terracotta hip roof with tile courses and hip ridges.
        static ComicMesh CampusBlock(float w, float d, float h, int floors, bool arcade, int seed)
        {
            var m = new ComicMesh();
            var rng = new System.Random(seed * 7349 + floors * 31 + 3);
            float front = d * .5f, floor = h / Mathf.Max(1, floors);
            int bays = Mathf.Max(2, Mathf.RoundToInt(w / 3.1f));
            float bay = w / bays;
            const float walk = 1.6f; // depth of the arcade walk
            Color stoneA = Color.Lerp(Lime, LimeTrim, .5f), stoneB = Color.Lerp(Lime, LimeShade, .65f);
            if (arcade)
            {
                float open = bay * .74f, spring = Mathf.Min(floor - .5f - open * .5f, 2.9f), wallTop = floors > 1 ? floor : h;
                // ground floor set back behind the walk; the upper floor over it
                Box(m, ComicSurface.Plaster, V(0, floor * .5f, -walk * .5f), V(w, floor, d - walk), LimeShade);
                if (floors > 1) Box(m, ComicSurface.Plaster, V(0, floor + (h - floor) * .5f, 0), V(w, h - floor, d), Lime);
                Box(m, ComicSurface.Paving, V(0, .1f, front - walk * .5f), V(w, .2f, walk), UwaPaving);
                // doors and windows in the shade of the walk
                for (int i = 0; i < bays; i++)
                {
                    float x = -w * .5f + (i + .5f) * bay;
                    if ((i + seed) % 3 == 0) Panel(m, V(x, 1.35f, front - walk + .03f), Vector3.right, Vector3.up, Vector3.forward, 1.2f, 2.3f, C(0x4A3524));
                    else Arch(m, V(x, 1f, front - walk + .03f), Vector3.right, Vector3.forward, bay * .36f, 1.7f, WindowDark);
                }
                m.Surface = ComicSurface.Plaster;
                TrunkAKit.Arcade(m, V(-w * .5f, 0, front), Vector3.right, Vector3.forward, w, bays, open, 0f, spring, wallTop, .5f, Lime, LimeShade, LimeTrim, true, .27f);
                if (floors == 1) TrunkAKit.StoneBlocks(m, V(-w * .5f, spring + open * .5f + .1f, front), Vector3.right, Vector3.forward, w, wallTop - spring - open * .5f - .3f, rng, bays, stoneA, stoneB, .3f);
            }
            else
            {
                Box(m, ComicSurface.Plaster, V(0, h * .5f, 0), V(w, h, d), Lime);
                Box(m, ComicSurface.Plaster, V(0, .3f, 0), V(w + .12f, .6f, d + .12f), Plinth);
                for (int i = 0; i < bays; i++)
                    Arch(m, V(-w * .5f + (i + .5f) * bay, .9f, front + .03f), Vector3.right, Vector3.forward, bay * .4f, Mathf.Min(floor - 1.4f, 2.2f), WindowDark);
            }
            Box(m, ComicSurface.Plaster, V(0, h - .12f, 0), V(w + .3f, .24f, d + .3f), LimeTrim);
            for (int level = 1; level < floors; level++)
            {
                Box(m, ComicSurface.Plaster, V(0, level * floor, front + .05f), V(w + .06f, .2f, .16f), LimeTrim);
                m.Surface = ComicSurface.Plaster;
                TrunkAKit.StoneBlocks(m, V(-w * .5f, level * floor + .2f, front), Vector3.right, Vector3.forward, w, floor - .6f, rng, bays + 1, stoneA, stoneB);
                for (int i = 0; i < bays; i++)
                {
                    float x = -w * .5f + (i + .5f) * bay, y = level * floor + .8f;
                    bool pair = (i + seed) % 3 != 1;
                    float ww = pair ? bay * .23f : bay * .46f, wh = floor * (pair ? .52f : .6f);
                    foreach (float dx in pair ? new[] { -bay * .17f, bay * .17f } : new[] { 0f })
                        Arch(m, V(x + dx, y, front + .035f), Vector3.right, Vector3.forward, ww, wh, WindowDark);
                    Box(m, ComicSurface.Plaster, V(x, y - .08f, front + .07f), V(pair ? bay * .62f : bay * .56f, .14f, .16f), LimeTrim);
                }
            }
            TileRoof(m, V(0, h, 0), w + .9f, d + .9f, Mathf.Min(w, d) * .26f);
            // a window on each end wall
            for (int s = -1; s <= 1; s += 2)
                Arch(m, V(s * (w * .5f + .03f), floors > 1 ? floor + .6f : 1.6f, -walk * .5f), V(0, 0, -s), V(s, 0, 0), 1f, floor * .5f, WindowDark);
            return m;
        }

        // Terracotta hip roof with two darker tile courses on each long slope and hip ridges.
        static void TileRoof(ComicMesh m, Vector3 c, float w, float d, float rise)
        {
            HipRoof(m, c, w, d, rise, Terracotta, TerracottaDark);
            m.Surface = ComicSurface.Brick;
            bool alongX = w >= d;
            float run = (alongX ? d : w) * .5f, len = (alongX ? w : d) * .5f; // slope run, half length of the long side
            Color course = Color.Lerp(Terracotta, TerracottaDark, .6f);
            Vector3 P(float along, float across, float y) => c + (alongX ? V(along, y, across) : V(across, y, along));
            foreach (float t in new[] { .34f, .67f })
                for (int s = -1; s <= 1; s += 2)
                {
                    float t2 = t + .06f, a = len - t * run, a2 = len - t2 * run;
                    Face(m, P(-a, s * run * (1f - t), t * rise + .03f), P(a, s * run * (1f - t), t * rise + .03f),
                        P(a2, s * run * (1f - t2), t2 * rise + .03f), P(-a2, s * run * (1f - t2), t2 * rise + .03f), course, alongX ? V(0, 1, s) : V(s, 1, 0), false);
                }
            for (int sa = -1; sa <= 1; sa += 2)
                for (int sc = -1; sc <= 1; sc += 2)
                    m.Beam(P(sa * len, sc * run, 0f), P(sa * (len - run), 0f, rise), .16f, TerracottaDark);
        }

        // Winthrop Hall (v0.6, from photographs: art-src/districts/uwa/SOURCES.txt). The long north front: an undercroft
        // of 12 round arches on round columns, above it a loggia of 18 small arches in threes behind a balustrade, then
        // six tall narrow round-headed windows; a projecting pavilion at the east end (local +X); a low orange tile
        // roof. The square clock tower stands at the west end (local -X: on the right as you face the front, the far
        // end for the rider): a belfry of four arches a side under a cornice and a very low pyramid roof, a pair of
        // arched windows, then the blue-and-cream clock under its hood.
        static ComicMesh WinthropHall()
        {
            var m = new ComicMesh();
            var rng = new System.Random(1932);
            const float W = 34f, D = 15f, H = 15f, hallZ = -3f;
            float front = hallZ + D * .5f;
            Color stoneA = Color.Lerp(Lime, LimeTrim, .5f), stoneB = Color.Lerp(Lime, LimeShade, .65f), door = C(0x4A3524);
            Box(m, ComicSurface.Plaster, V(0, H * .5f, hallZ), V(W, H, D), Lime);
            Box(m, ComicSurface.Plaster, V(0, .4f, hallZ), V(W + .2f, .8f, D + .2f), Plinth);
            Box(m, ComicSurface.Plaster, V(0, H - .2f, hallZ), V(W + .4f, .4f, D + .4f), LimeTrim);
            // patterned frieze under the eaves
            for (int i = 0; i < 22; i++) Arch(m, V(-W * .5f + (i + .5f) * W / 22f, H - 1.3f, front + .03f), Vector3.right, Vector3.forward, .7f, .9f, LimeShade);
            TileRoof(m, V(0, H, hallZ), W + 1f, D + 1f, 3f);

            // the two-storey gallery across the front
            const float LW = 26.4f, LD = 4f, LH = 8.2f, lx = .2f, mid = 4.6f;
            float lf = front + LD;
            Box(m, ComicSurface.Paving, V(lx, .35f, front + LD * .5f + .8f), V(LW + 1.6f, .7f, LD + 1.6f), LimeTrim);   // terrace
            Box(m, ComicSurface.Paving, V(lx, .15f, lf + 2.1f), V(LW + 3f, .3f, 1.4f), UwaPaving);                       // step
            Box(m, ComicSurface.Plaster, V(lx, mid, front + LD * .5f), V(LW, .35f, LD), LimeTrim);                       // gallery floor
            Box(m, ComicSurface.Plaster, V(lx, LH + .15f, front + LD * .5f), V(LW + .4f, .3f, LD + .4f), LimeTrim);      // gallery top
            for (int i = 0; i < 6; i++)
            {
                float x = lx - LW * .5f + (i + .5f) * LW / 6f;
                Arch(m, V(x, .8f, front + .03f), Vector3.right, Vector3.forward, 1.5f, 2.9f, door);
                Arch(m, V(x, mid + .5f, front + .03f), Vector3.right, Vector3.forward, 1.1f, 2.4f, WindowDark);
            }
            m.Surface = ComicSurface.Plaster;
            TrunkAKit.Arcade(m, V(lx - LW * .5f, .7f, lf), Vector3.right, Vector3.forward, LW, 12, 1.7f, 0f, 2.55f, mid - .875f, .55f, Lime, LimeShade, LimeTrim, true, .24f);
            TrunkAKit.Arcade(m, V(lx - LW * .5f, mid + .175f, lf), Vector3.right, Vector3.forward, LW, 18, .98f, .75f, 2.2f, LH - mid - .175f, .4f, Lime, LimeShade, LimeTrim, false, 0f, false, 5);
            for (int i = 0; i <= 6; i++)
                Box(m, ComicSurface.Plaster, V(lx - LW * .5f + i * LW / 6f, (mid + LH) * .5f + .1f, lf + .06f), V(.5f, LH - mid - .2f, .14f), LimeShade);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(m, ComicSurface.Plaster, V(lx + s * (LW * .5f - .3f), (LH + .7f) * .5f, front + LD * .5f), V(.6f, LH - .7f, LD), Lime);
                Arch(m, V(lx + s * (LW * .5f + .01f), .9f, front + LD * .5f), V(0, 0, -s), V(s, 0, 0), 1.6f, 3f, ArchDark);
            }
            // six tall narrow round-headed windows above the gallery
            for (int i = 0; i < 6; i++)
            {
                float x = lx - LW * .5f + (i + .5f) * LW / 6f;
                Arch(m, V(x, LH + 1f, front + .035f), Vector3.right, Vector3.forward, 1.3f, 4.2f, WindowDark);
                Box(m, ComicSurface.Plaster, V(x, LH + .92f, front + .08f), V(1.7f, .16f, .18f), LimeTrim);
            }
            m.Surface = ComicSurface.Plaster;
            TrunkAKit.StoneBlocks(m, V(-W * .5f, LH + .6f, front), Vector3.right, Vector3.forward, W, 4.5f, rng, 16, stoneA, stoneB, .6f);

            // east pavilion: a projecting block with a paired window over a door
            const float PW = 5.2f;
            float px = W * .5f - PW * .5f + .4f, pf = front + 2.4f;
            Box(m, ComicSurface.Plaster, V(px, (H + .6f) * .5f, pf - 2.4f), V(PW, H + .6f, 4.8f), Lime);
            Box(m, ComicSurface.Plaster, V(px, H + .5f, pf - 2.4f), V(PW + .4f, .35f, 5.2f), LimeTrim);
            TileRoof(m, V(px, H + .65f, pf - 2.4f), PW + .9f, 5.7f, 1.6f);
            for (int s = -1; s <= 1; s += 2) Arch(m, V(px + s * .85f, 8.6f, pf + .03f), Vector3.right, Vector3.forward, 1.1f, 3.6f, WindowDark);
            Box(m, ComicSurface.Plaster, V(px, 8.5f, pf + .08f), V(3.4f, .18f, .18f), LimeTrim);
            Arch(m, V(px, .8f, pf + .03f), Vector3.right, Vector3.forward, 1.8f, 3.4f, door);

            // the clock tower
            const float T = 44f, TW = 7f, belfry = 36.2f;
            Vector3 tc = V(-W * .5f - TW * .5f + 1f, 0, front - 1f);
            Box(m, ComicSurface.Plaster, tc + V(0, belfry * .5f, 0), V(TW, belfry, TW), Lime);
            Box(m, ComicSurface.Plaster, tc + V(0, 1f, 0), V(TW + .3f, 2f, TW + .3f), Plinth);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Box(m, ComicSurface.Plaster, tc + V(sx * (TW * .5f - .25f), belfry * .5f + 1f, sz * (TW * .5f - .25f)), V(.62f, belfry - 2f, .62f), LimeShade);
            foreach (float y in new[] { 13f, 21.2f })
                Box(m, ComicSurface.Plaster, tc + V(0, y, 0), V(TW + .3f, .4f, TW + .3f), LimeTrim);
            float by = belfry + .4f, bh = T - by - .6f;
            Box(m, ComicSurface.Plaster, tc + V(0, belfry + .2f, 0), V(TW + .5f, .4f, TW + .5f), LimeTrim);
            Box(m, ComicSurface.Plain, tc + V(0, by + bh * .5f, 0), V(TW - 1.3f, bh, TW - 1.3f), ArchDark);              // the dark inside of the belfry
            Box(m, ComicSurface.Plaster, tc + V(0, T - .3f, 0), V(TW + .7f, .6f, TW + .7f), LimeTrim);
            TileRoof(m, tc + V(0, T, 0), TW + 1f, TW + 1f, 1.3f);
            Vector3[] normals = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            foreach (Vector3 n in normals)
            {
                Vector3 right = Vector3.Cross(Vector3.up, n), face = tc + n * (TW * .5f + .03f);
                m.Surface = ComicSurface.Plaster;
                TrunkAKit.Arcade(m, tc + n * (TW * .5f) - right * (TW * .5f) + V(0, by, 0), right, n, TW, 4, 1.05f, .9f, 4.3f, bh, .55f, Lime, LimeShade, LimeTrim, false, 0f, true, 5);
                // a pair of arched windows under the belfry, slits lower down
                for (int i = -1; i <= 1; i += 2) Arch(m, face + right * (i * 1.1f) + V(0, 29.4f, 0), right, n, 1.25f, 4.4f, WindowDark);
                Box(m, ComicSurface.Plaster, tc + n * (TW * .5f + .08f) + V(0, 29.3f, 0), Abs(right * 3.9f + Vector3.up * .18f + n * .18f), LimeTrim);
                Arch(m, face + V(0, 8.6f, 0), right, n, .8f, 3.2f, WindowDark);
                Arch(m, face + V(0, 15.6f, 0), right, n, .8f, 3.2f, WindowDark);
                m.Surface = ComicSurface.Plaster;
                TrunkAKit.StoneBlocks(m, tc + n * (TW * .5f) - right * (TW * .5f - .7f) + V(0, 3f, 0), right, n, TW - 1.4f, 17f, rng, 8, stoneA, stoneB, .6f);
                if (n == Vector3.back) continue; // no clock towards the back
                // clock: a small hood, cream ring, blue face, gold hands
                Vector3 c0 = face + V(0, 25f, 0);
                Box(m, ComicSurface.Plaster, tc + n * (TW * .5f + .25f) + V(0, 27.3f, 0), Abs(right * 4.6f + Vector3.up * .28f + n * .5f), LimeTrim);
                TrunkAKit.Disc(m, c0 + n * .02f, n, 1.95f, LimeTrim, 12);
                TrunkAKit.Disc(m, c0 + n * .06f, n, 1.55f, ClockBlue, 12);
                Slab(m, c0 + n * .1f + V(0, .5f, 0), right, n, .14f, 1f, PeacockGold);
                Slab(m, c0 + n * .1f + right * .34f + V(0, -.12f, 0), right, n, .78f, .12f, PeacockGold);
            }
            // the small hooded balcony low on the front of the tower
            Box(m, ComicSurface.Plaster, tc + V(0, 5.6f, TW * .5f + .5f), V(2.6f, .25f, 1f), LimeTrim);
            Box(m, ComicSurface.Plaster, tc + V(0, 6.2f, TW * .5f + .95f), V(2.6f, 1f, .12f), Lime);
            Arch(m, tc + V(0, 5.75f, TW * .5f + .035f), Vector3.right, Vector3.forward, 1.3f, 2.6f, ArchDark);
            return m;
        }

        // Moreton Bay fig (one great fig stands between Winthrop Hall and the Arts building).
        static ComicMesh FigMesh(int seed, float spread)
        {
            var m = new ComicMesh();
            TrunkAKit.Fig(m, Vector3.zero, spread, seed, C(0x6E5D4E), C(0x2F5B34), C(0x4F7F40), _r.ink);
            return m;
        }

        // The kit's street tree in its pit (the same tree West Perth's street uses): the street edge at a boundary.
        static ComicMesh StreetTreeMesh() =>
            ComicKitGeometry.Prop(new ComicAssetSpec { id = "TreePit", label = "TreePit", style = "tree", width = 2f, height = 5f, depth = 2f, color = _r.pavement }, _r);

        // Campus gate pylon: a limestone pier with a terracotta cap and a short wing wall curving away from the road.
        static ComicMesh GatePylon(int side)
        {
            var m = new ComicMesh();
            Box(m, ComicSurface.Plaster, V(0, .35f, 0), V(1.7f, .7f, 1.7f), Plinth);
            Box(m, ComicSurface.Plaster, V(0, 2.5f, 0), V(1.35f, 3.6f, 1.35f), Lime);
            Box(m, ComicSurface.Plaster, V(0, 4.4f, 0), V(1.7f, .24f, 1.7f), LimeTrim);
            Box(m, ComicSurface.Plaster, V(0, 1.5f, -.7f), V(.9f, .16f, .1f), LimeTrim);
            Arch(m, V(0, 1.7f, -.705f), Vector3.left, Vector3.back, .7f, 1.9f, LimeShade);
            HipRoof(m, V(0, 4.52f, 0), 2f, 2f, .9f, Terracotta, TerracottaDark);
            // wing wall: three straight pieces swinging out and back
            Vector3 at = V(side * .6f, 0, 0);
            float yaw = 0f;
            for (int i = 0; i < 3; i++)
            {
                yaw += side * 20f;
                Quaternion q = Quaternion.Euler(0, yaw, 0);
                Vector3 dir = q * V(side, 0, 0), mid = at + dir * 1.3f;
                float h = 2.3f - i * .35f;
                m.Surface = ComicSurface.Plaster;
                m.Box(mid + V(0, h * .5f, 0), V(2.6f, h, .5f), Lime, q);
                m.Box(mid + V(0, h + .07f, 0), V(2.7f, .14f, .62f), LimeTrim, q);
                at += dir * 2.6f;
            }
            Box(m, ComicSurface.Plaster, at + V(0, .8f, 0), V(.7f, 1.6f, .7f), Lime);
            Box(m, ComicSurface.Plaster, at + V(0, 1.67f, 0), V(.84f, .14f, .84f), LimeTrim);
            return m;
        }

        // A street name blade on a pole: dark green plate, white pixel letters towards the rider (-Z).
        static ComicMesh StreetSign(string name)
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            Color green = C(0x1F6B45);
            float w = TrunkAKit.TextWidth(name, .07f) + .3f;
            m.Cylinder(V(0, .12f, 0), .16f, .24f, _r.ink, 8);
            m.Cylinder(V(0, 1.7f, 0), .05f, 3.3f, C(0x8A8F96), 8);
            m.Box(V(w * .5f - .05f, 3.1f, 0), V(w, .5f, .06f), green);
            m.Box(V(w * .5f - .05f, 3.1f, 0), V(w + .06f, .56f, .03f), White);
            TrunkAKit.Text(m, name, V(w * .5f - .05f, 3.1f, -.035f), Vector3.right, Vector3.up, Vector3.back, .07f, White);
            return m;
        }

        // ---- trees, props and animals in the comic kit's standard (ComicKitGeometry: closed boxes, 8-sided
        // cylinders, faceted blobs, thick slabs; ink bases and caps, cream trim)

        // ComicKitGeometry.Prop's Plant(): a faceted low-poly blob and a lighter highlight blob (two tones).
        static void Plant(ComicMesh m, Vector3 p, float size, Color main, Color light, bool highlight = true, float squash = .7f)
        {
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(p, V(size, size * squash, size), main);
            if (highlight) m.Ellipsoid(p + V(size * .23f, size * squash * .2f, 0), V(size * .65f, size * squash * .85f, size * .65f), light);
        }

        // Tapered 8-sided cylinder from a to b (trunks, limbs, necks, water jets).
        static void Limb(ComicMesh m, Vector3 a, Vector3 b, float r, Color c, float taper = .75f, ComicSurface s = ComicSurface.Wood)
        {
            m.Surface = s;
            Vector3 d = b - a;
            m.Cylinder((a + b) * .5f, r, d.magnitude, c, 8, taper, Quaternion.FromToRotation(Vector3.up, d));
        }

        // Ink root ring at the foot of a tree (the street tree's ink pit).
        static void RootRing(ComicMesh m, float r)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(V(0, .06f, 0), r, .12f, _r.ink, 8);
        }

        // A thick slab from a to b (width w0 -> w1, thickness t), its top facing `up`, darker underneath (fronds, feathers).
        static void Blade(ComicMesh m, Vector3 a, Vector3 b, Vector3 up, float w0, float w1, float t, Color top, Color under, ComicSurface s)
        {
            m.Surface = s;
            Vector3 dir = (b - a).normalized, side = Vector3.Cross(up, dir).normalized, n = Vector3.Cross(dir, side).normalized;
            if (Vector3.Dot(n, up) < 0f) n = -n;
            Vector3 ha = side * (w0 * .5f), hb = side * (w1 * .5f), h = n * (t * .5f);
            Vector3 A0 = a - ha + h, A1 = a + ha + h, B0 = b - hb + h, B1 = b + hb + h, a0 = a - ha - h, a1 = a + ha - h, b0 = b - hb - h, b1 = b + hb - h;
            Color mid = Color.Lerp(top, under, .5f);
            Face(m, A0, A1, B1, B0, top, n);
            Face(m, a0, a1, b1, b0, under, -n);
            Face(m, A0, B0, b0, a0, mid, -side);
            Face(m, A1, B1, b1, a1, mid, side);
            Face(m, A0, A1, a1, a0, mid, -dir);
            Face(m, B0, B1, b1, b0, mid, dir);
        }

        // Jacaranda in November bloom: a tapered trunk that splits into three thick limbs under a broad umbrella crown
        // of purple and lilac blobs.
        static ComicMesh Jacaranda(int seed, float size = 1f)
        {
            var rng = new System.Random(seed * 7919);
            float R() => (float)rng.NextDouble();
            var m = new ComicMesh();
            RootRing(m, .44f);
            Vector3 fork = V((R() - .5f) * .3f, 2.2f, 0);
            Limb(m, V(0, .1f, 0), fork, .27f, Trunk, .8f);
            float spin = R() * 6f;
            for (int i = 0; i < 3; i++)
            {
                float a = spin + i * 2.09f;
                Limb(m, fork - V(0, .2f, 0), fork + V(Mathf.Cos(a) * 1.6f, 2.1f + R() * .4f, Mathf.Sin(a) * 1.6f), .2f, Trunk, .65f);
            }
            Plant(m, fork + V(0, 3.1f, 0), 3.8f, J1, J3, true, .5f);
            for (int i = 0; i < 5; i++)
            {
                float a = spin + .6f + i * 1.2566f, rr = 2.3f + R() * .5f;
                Plant(m, fork + V(Mathf.Cos(a) * rr, 2.5f + R() * .5f, Mathf.Sin(a) * rr), 2.5f + R() * .6f, i % 2 == 0 ? J2 : J1, J3, i % 2 == 0, .62f);
            }
            if (Mathf.Approximately(size, 1f)) return m;
            var scaled = new ComicMesh();
            scaled.Add(m, Vector3.zero, Quaternion.identity, Vector3.one * size);
            return scaled;
        }

        // Italian cypress: three stacked tall blobs on a short trunk.
        static ComicMesh CypressMesh(float size)
        {
            var m = new ComicMesh();
            RootRing(m, .34f);
            Limb(m, V(0, .1f, 0), V(0, 1.3f, 0), .2f, Trunk, .85f);
            Color light = Color.Lerp(Cypress, Grass, .35f);
            Plant(m, V(0, 2.6f, 0) * size, 2.2f * size, Cypress, light, false, 1.45f);
            Plant(m, V(0, 4.7f, 0) * size, 1.95f * size, Cypress, light, true, 1.65f);
            Plant(m, V(0, 6.8f, 0) * size, 1.4f * size, Cypress, light, false, 1.85f);
            return m;
        }

        // Washingtonia palm: a ringed trunk of stacked 8-sided segments with ink rings, a dry skirt and 8 fronds of
        // thick bent slab segments arching down, darker underneath.
        static ComicMesh Palm(int seed, float height)
        {
            var rng = new System.Random(seed * 104729);
            var m = new ComicMesh();
            RootRing(m, .44f);
            const int segments = 5;
            float seg = height / segments;
            for (int i = 0; i < segments; i++)
            {
                float r = Mathf.Lerp(.33f, .25f, i / (float)segments);
                Limb(m, V(0, i * seg, 0), V(0, (i + 1) * seg, 0), r, i % 2 == 0 ? PalmTrunk : Color.Lerp(PalmTrunk, _r.cream, .18f), .93f);
                if (i > 0) { m.Surface = ComicSurface.PaintedMetal; m.Cylinder(V(0, i * seg, 0), r + .035f, .09f, _r.ink, 8); }
            }
            m.Surface = ComicSurface.Wood;
            m.Cylinder(V(0, height + .05f, 0), .45f, 1.1f, C(0xB79A6A), 8, 1.3f);
            Vector3 top = V(0, height + .65f, 0);
            Plant(m, top, .95f, PalmLeaf, Color.Lerp(PalmLeaf, _r.yellow, .3f), false, .8f);
            float spin = (float)rng.NextDouble() * 6f;
            Color under = Color.Lerp(PalmLeaf, _r.ink, .45f), light = Color.Lerp(PalmLeaf, _r.yellow, .22f);
            for (int i = 0; i < 8; i++)
            {
                float a = spin + i * Mathf.PI * 2f / 8f, rise = i % 2 == 0 ? .5f : .2f;
                Vector3 h = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 p0 = top + h * .25f, p1 = p0 + h * 1f + Vector3.up * rise, p2 = p1 + h * .95f - Vector3.up * .2f, p3 = p2 + h * .7f - Vector3.up * .8f;
                Color c = i % 3 == 0 ? light : PalmLeaf;
                Blade(m, p0, p1, Vector3.up, .3f, .62f, .08f, c, under, ComicSurface.Foliage);
                Blade(m, p1, p2, Vector3.up, .62f, .5f, .08f, c, under, ComicSurface.Foliage);
                Blade(m, p2, p3, Vector3.up, .5f, .12f, .08f, c, under, ComicSurface.Foliage);
            }
            return m;
        }

        // Campus lamp: ink foot, slim 8-sided pole, a flat ink disc hat with a cream light underneath.
        static ComicMesh CampusLamp()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .15f, 0), .2f, .3f, _r.ink, 8);
            m.Cylinder(V(0, 2.45f, 0), .08f, 4.4f, _r.ink, 8, .85f);
            m.Cylinder(V(0, 4.72f, 0), .52f, .16f, _r.ink, 8);
            m.Cylinder(V(0, 4.86f, 0), .24f, .12f, _r.ink, 8, .5f);
            m.Surface = ComicSurface.Glass;
            m.Cylinder(V(0, 4.6f, 0), .38f, .08f, _r.cream, 8);
            return m;
        }

        // Riverside lamp: ink foot, white pole and arm, a white head with a cream light under an ink cap.
        static ComicMesh RiversideLamp(int armSide)
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .15f, 0), .2f, .3f, _r.ink, 8);
            m.Cylinder(V(0, 2.45f, 0), .08f, 4.4f, White, 8, .85f);
            m.Box(V(armSide * .45f, 4.68f, 0), V(1f, .1f, .1f), White);
            m.Box(V(armSide * .95f, 4.66f, 0), V(.58f, .16f, .3f), White);
            m.Box(V(armSide * .95f, 4.77f, 0), V(.62f, .06f, .34f), _r.ink);
            m.Surface = ComicSurface.Glass;
            m.Box(V(armSide * .95f, 4.56f, 0), V(.46f, .06f, .22f), _r.cream);
            return m;
        }

        // The kit's bench (recipe Bench: ink legs, wooden seat and back), in another colour.
        static ComicMesh BenchMesh(Color color) =>
            ComicKitGeometry.Prop(new ComicAssetSpec { id = "Bench", label = "Bench", style = "bench", width = 2f, height = 1f, depth = .8f, color = color }, _r);

        // The kit's bike rack (ink hoops) with a parked red bike.
        static ComicMesh BikeRackMesh()
        {
            var m = new ComicMesh();
            m.Add(ComicKitGeometry.Prop(new ComicAssetSpec { id = "BikeRack", label = "BikeRack", style = "bike", width = 1.2f, height = .9f, depth = .9f, color = _r.ink }, _r), Vector3.zero, Quaternion.identity, Vector3.one);
            m.Surface = ComicSurface.Rubber;
            for (int s = -1; s <= 1; s += 2) m.Cylinder(V(-.2f, .34f, s * .5f), .34f, .1f, _r.ink, 8, 1, Quaternion.Euler(0, 0, 90));
            m.Surface = ComicSurface.PaintedMetal;
            for (int s = -1; s <= 1; s += 2) m.Cylinder(V(-.2f, .34f, s * .5f), .12f, .12f, _r.cream, 8, 1, Quaternion.Euler(0, 0, 90));
            Limb(m, V(-.2f, .34f, -.5f), V(-.2f, .78f, .05f), .045f, _r.red, 1f, ComicSurface.PaintedMetal);
            Limb(m, V(-.2f, .78f, .05f), V(-.2f, .34f, .5f), .045f, _r.red, 1f, ComicSurface.PaintedMetal);
            Limb(m, V(-.2f, .34f, -.5f), V(-.2f, .8f, -.42f), .04f, _r.red, 1f, ComicSurface.PaintedMetal);
            m.Box(V(-.2f, .86f, -.4f), V(.1f, .07f, .26f), _r.ink);
            m.Box(V(-.2f, .84f, .02f), V(.5f, .06f, .07f), _r.ink);
            return m;
        }

        static ComicMesh StoneBenchMesh()
        {
            var m = new ComicMesh();
            Box(m, ComicSurface.Plaster, V(-.7f, .2f, 0), V(.35f, .4f, .5f), LimeShade);
            Box(m, ComicSurface.Plaster, V(.7f, .2f, 0), V(.35f, .4f, .5f), LimeShade);
            Box(m, ComicSurface.Plaster, V(0, .46f, 0), V(2f, .14f, .6f), LimeTrim);
            return m;
        }

        // A-frame sign: two thick gold boards on ink feet, a navy panel with a cream arrow towards the rider (-Z).
        static ComicMesh AFrameSign()
        {
            var m = new ComicMesh();
            Color board = C(0xE3B53E);
            for (int s = -1; s <= 1; s += 2)
            {
                m.Surface = ComicSurface.Wood;
                m.Box(V(0, .5f, s * .14f), V(.86f, 1f, .08f), board, Quaternion.Euler(-s * 15f, 0, 0));
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(0, .04f, s * .26f), V(.92f, .08f, .1f), _r.ink);
            }
            m.Box(V(0, .99f, 0), V(.9f, .08f, .12f), _r.ink);
            Quaternion q = Quaternion.Euler(15f, 0, 0);
            Vector3 n = q * Vector3.back, up = q * Vector3.up, c = V(0, .5f, -.14f) + up * .06f + n * .045f;
            Panel(m, c, Vector3.right, up, n, .66f, .56f, UwaBlue);
            Vector3 f = c + n * .006f;
            Panel(m, f + Vector3.right * .06f, Vector3.right, up, n, .3f, .1f, _r.cream);
            Tri(m, f + Vector3.right * -.24f, f + Vector3.right * -.08f + up * .14f, f + Vector3.right * -.08f - up * .14f, _r.cream, n);
            Panel(m, f - up * .2f, Vector3.right, up, n, .44f, .05f, PeacockGold);
            return m;
        }

        // Lawn sprinkler: ink base, green body, yellow spinning arm, chunky arcs of water.
        static ComicMesh Sprinkler()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .05f, 0), .32f, .1f, _r.ink, 8);
            m.Surface = ComicSurface.Plastic;
            m.Cylinder(V(0, .17f, 0), .25f, .14f, C(0x3C8A4A), 8, .8f);
            m.Cylinder(V(0, .32f, 0), .08f, .18f, C(0xF2C230), 8);
            m.Box(V(0, .44f, 0), V(.58f, .08f, .1f), C(0xF2C230));
            m.Box(V(0, .47f, 0), V(.12f, .08f, .12f), _r.ink);
            Color spray = C(0xBFE4F8);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 p0 = V(s * .3f, .46f, 0), p1 = V(s * .42f, .74f, 0), p2 = V(s * .58f, .72f, 0), p3 = V(s * .7f, .32f, 0);
                Limb(m, p0, p1, .05f, spray, 1f, ComicSurface.Glass);
                Limb(m, p1, p2, .05f, spray, 1f, ComicSurface.Glass);
                Limb(m, p2, p3, .045f, spray, .8f, ComicSurface.Glass);
                m.Surface = ComicSurface.Glass;
                m.Ellipsoid(p3 - V(0, .14f, 0), V(.13f, .13f, .13f), Color.white);
                m.Ellipsoid(p1 + V(s * .06f, .12f, .1f), V(.1f, .1f, .1f), Color.white);
            }
            return m;
        }

        // Peacock facing -Z, feet at the pivot: chunky blue body, brown wings, an 8-sided neck, a crest; the train of
        // thick slab feathers with eye spots trails behind, or stands up in a display fan.
        static ComicMesh PeacockMesh(bool fan)
        {
            var m = new ComicMesh();
            Color legs = C(0x8A8070), wing = C(0x8C7650), tailDark = Color.Lerp(PeacockTail, _r.ink, .4f);
            m.Surface = ComicSurface.PaintedMetal;
            for (int s = -1; s <= 1; s += 2) { m.Box(V(s * .09f, .2f, .05f), V(.07f, .4f, .07f), legs); m.Box(V(s * .09f, .03f, -.02f), V(.12f, .06f, .18f), _r.ink); }
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(V(0, .6f, .08f), V(.5f, .46f, .72f), Peacock);
            m.Ellipsoid(V(0, .7f, .3f), V(.44f, .28f, .46f), wing);
            Limb(m, V(0, .7f, -.14f), V(0, .93f, -.21f), .1f, PeacockNeck, .85f, ComicSurface.Fabric);
            Limb(m, V(0, .93f, -.21f), V(0, 1.07f, -.21f), .085f, PeacockNeck, .95f, ComicSurface.Fabric);
            m.Ellipsoid(V(0, 1.13f, -.23f), V(.21f, .2f, .25f), PeacockNeck);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, 1.11f, -.38f), V(.06f, .06f, .12f), C(0xD9C9A0));
            for (int s = -1; s <= 1; s += 2) { m.Box(V(s * .1f, 1.15f, -.28f), V(.02f, .06f, .1f), Color.white); m.Box(V(s * .107f, 1.15f, -.3f), V(.02f, .035f, .035f), _r.ink); }
            for (int i = -1; i <= 1; i++)
            {
                Vector3 tip = V(i * .07f, 1.4f, -.21f);
                Limb(m, V(0, 1.22f, -.22f), tip, .03f, PeacockNeck, 1f, ComicSurface.Fabric);
                m.Surface = ComicSurface.Fabric;
                m.Ellipsoid(tip, V(.08f, .07f, .08f), PeacockNeck);
            }
            if (!fan)
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = (i - 2) * .11f;
                    Vector3 a = V(x * .4f, .62f, .36f), b = V(x * 2.2f, .12f + (i % 2) * .05f, 1.55f);
                    Blade(m, a, b, Vector3.up, .16f, .2f, .06f, PeacockTail, tailDark, ComicSurface.Foliage);
                    Vector3 dir = (b - a).normalized, n = Vector3.Cross(dir, Vector3.Cross(Vector3.up, dir)).normalized;
                    if (n.y < 0f) n = -n;
                    Vector3 spot = Vector3.Lerp(a, b, .82f) + n * .036f;
                    Disc(m, spot, n, .085f, PeacockGold, 8);
                    Disc(m, spot + n * .006f, n, .048f, UwaBlue, 8);
                }
            }
            else
            {
                Quaternion tilt = Quaternion.Euler(-14f, 0, 0);
                Vector3 centre = V(0, .55f, .4f), front = tilt * Vector3.back;
                for (int row = 0; row < 2; row++)
                {
                    int count = row == 0 ? 11 : 8;
                    float len = row == 0 ? 1.1f : .75f;
                    for (int i = 0; i < count; i++)
                    {
                        float a = Mathf.Lerp(-82f, 82f, i / (count - 1f)) * Mathf.Deg2Rad;
                        Vector3 dir = tilt * V(Mathf.Sin(a), Mathf.Cos(a), 0);
                        Vector3 a0 = centre + dir * .12f + front * (row * .07f), a1 = centre + dir * len + front * (row * .07f);
                        Color col = row == 0 ? PeacockTail : Color.Lerp(PeacockTail, PeacockGold, .25f);
                        Blade(m, a0, a1, front, .1f, row == 0 ? .26f : .2f, .06f, col, tailDark, ComicSurface.Foliage);
                        Vector3 spot = centre + dir * (len - .14f) + front * (row * .07f + .036f);
                        Disc(m, spot, front, row == 0 ? .1f : .08f, PeacockGold, 8);
                        Disc(m, spot + front * .006f, front, row == 0 ? .056f : .045f, UwaBlue, 8);
                    }
                }
            }
            return m;
        }

        // The far scenery UWA and Matilda Bay share (v0.6): Matilda Bay and the Swan out to the far shore on the right,
        // campus roofs among trees and the bush ridge of Kings Park on the left. Built on UWA's slope; Matilda Bay's
        // flatter chunks carry their own water out to the far shore (RiverEdge), so no seam shows there.
        static ComicMesh CrawleyFar()
        {
            var m = new ComicMesh();
            var rng = new System.Random(4242);
            float R() => (float)rng.NextDouble();
            float g = _r.downhillGrade;
            var flat = new ComicMesh();
            flat.Surface = ComicSurface.Foliage;
            StripSlab(flat, -420f, -70f, -80f, 520f, -.1f, LawnDark, 40f);
            flat.Surface = ComicSurface.Plain;
            StripSlab(flat, 76f, 600f, -80f, 560f, -.62f, RiverDeep, 40f);
            for (int i = 0; i < 70; i++)
            {
                float x = 90f + R() * 180f, z = -40f + R() * 520f, w = 2f + R() * 6f;
                FlatQuad(flat, x - w, x + w, z, z + .35f + R() * .3f, -.58f, i % 3 == 0 ? White : RiverLight);
            }
            // the far shore: low land
            flat.Surface = ComicSurface.Foliage;
            StripSlab(flat, RiverEdge, 600f, -80f, 560f, 1.2f, C(0x86A27A), 60f);
            flat.Shear(g);
            m.Add(flat, Vector3.zero, Quaternion.identity, Vector3.one);

            var up = new ComicMesh();
            // left: campus roofs among trees
            for (float z = -60f; z < 500f; z += 24f + R() * 14f)
            {
                float x = -(84f + R() * 34f), y = -g * z;
                if (R() < .38f)
                {
                    float w = 16f + R() * 12f, d = 10f + R() * 5f, h = 7f + R() * 5f;
                    Box(up, ComicSurface.Plaster, V(x, y + h * .5f - 2f, z), V(d, h + 4f, w), Color.Lerp(Lime, _r.cream, .3f));
                    for (float wz = z - w * .5f + 1.6f; wz < z + w * .5f - 1f; wz += 3.2f)
                        Arch(up, V(x + d * .5f + .05f, y + h * .45f, wz), Vector3.forward, Vector3.right, 1.3f, 2.4f, WindowDark);
                    HipRoof(up, V(x, y + h, z), d + .8f, w + .8f, 2.6f, Terracotta, TerracottaDark);
                }
                else
                {
                    up.Surface = ComicSurface.Foliage;
                    Color leaf = Color.Lerp(Cypress, LawnDark, R() * .6f);
                    Vector3 size = V(15f + R() * 8f, 11f + R() * 6f, 13f + R() * 5f);
                    up.Ellipsoid(V(x, y + 5f, z), size, leaf);
                    up.Ellipsoid(V(x + size.x * .2f, y + 5f + size.y * .22f, z - 1f), size * .6f, Color.Lerp(leaf, Grass, .45f));
                }
            }
            // the bush ridge of Kings Park further out
            for (float z = -60f; z < 500f; z += 40f)
            {
                up.Surface = ComicSurface.Foliage;
                float x = -(150f + R() * 30f), y = -g * z;
                up.Ellipsoid(V(x, y + 12f, z), V(60f, 34f, 50f), Color.Lerp(Cypress, C(0x6C7D44), .5f));
                up.Ellipsoid(V(x + 8f, y + 20f, z + 6f), V(34f, 18f, 30f), C(0x71864A));
            }
            // right: the far shore's trees and pale houses, yachts out on the bay
            for (float z = -60f; z < 540f; z += 14f + R() * 10f)
            {
                float x = RiverEdge + 6f + R() * 60f, y = -g * z;
                up.Surface = ComicSurface.Foliage;
                up.Ellipsoid(V(x, y + 4f, z), V(14f + R() * 10f, 8f + R() * 5f, 12f), Color.Lerp(C(0x5E7E58), C(0x7E9A80), R()));
                if (R() < .5f) Box(up, ComicSurface.Plaster, V(x - 10f, y + 2.5f, z + 6f), V(6f, 4f, 8f), Color.Lerp(White, C(0xC9B9A0), R()));
            }
            // (The CBD skyline itself is in the panorama sky, across the water.)
            for (int i = 0; i < 12; i++)
            {
                float x = 100f + R() * 150f, z = 30f + R() * 320f;
                up.Add(SailBoat(300 + i), V(x, -g * z - .55f, z), Quaternion.Euler(0, R() * 360f, 0), Vector3.one * 1.3f);
            }
            m.Add(up, Vector3.zero, Quaternion.identity, Vector3.one);
            return m;
        }

        // =====================================================================================================
        // MATILDA BAY
        // =====================================================================================================

        static readonly Color RedPath = C(0xA8483A), RiverDeep = C(0x2F7EC0), RiverMid = C(0x4A98D2), RiverLight = C(0x9ACDEC),
            Sand = C(0xE8D6A6), BayLawn = C(0x7DB24E), Peppermint = C(0x5F8B3A), PeppermintLight = C(0x7FA44C), PepTrunk = C(0x4A3B30),
            Bush = C(0x6E7E45), BushDark = C(0x4F6334), LimeRock = C(0xD9CDA8), ShedBlue = C(0x78A6D8), White = C(0xF4F4EE),
            ShedRoof = C(0xE3E8EA), Timber = C(0xA39684), TimberDark = C(0x6E5E4E), PicnicWood = C(0x9C6B43), ShelterRoof = C(0x3F7055),
            Swan = C(0x1B1D24), SwanBeak = C(0xD8322B), EskyBlue = C(0x2D6DB5), Wicker = C(0xC49A55);

        static void BuildMatilda()
        {
            Begin("Matilda");
            float g = MatildaGrade;
            var chunks = new List<RoadChunk>();
            RoadChunk entry = SaveChunk(MatildaEntry(g));
            chunks.Add(SaveChunk(MatildaForeshore(g)));
            chunks.Add(SaveChunk(MatildaBoatshed(g)));
            chunks.Add(SaveChunk(MatildaCafe(g)));
            chunks.Add(SaveChunk(MatildaInlet(g)));
            chunks.Add(SaveChunk(MatildaPicnic(g)));
            RoadChunk exit = SaveChunk(MatildaExit(g));

            Ramp jetty = SaveRamp("RMP_Matilda_Jetty", JettyRampMesh(3f, 5f, 1.3f, 0f, g, 0f), 3f, 5f, 1.3f, 0f, 1f, .5f);
            var esky = SaveObstacle("OBS_Matilda_Esky", Esky(), new Vector3(0, .31f, 0), new Vector3(.85f, .62f, .55f), ObstacleKind.Thing, true, true);
            var basket = SaveObstacle("OBS_Matilda_PicnicBasket", PicnicBasket(), new Vector3(0, .33f, 0), new Vector3(.72f, .66f, .5f), ObstacleKind.Thing, true, true);
            GameObject swan = SaveAnimal("OBS_Matilda_BlackSwan", SwanMesh(), new Vector3(0, .6f, .05f), new Vector3(.52f, 1.2f, 1f), true, .4f);
            GameObject far = _sharedFar != null ? _sharedFar : AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("Uwa", SharedFarName)); // UWA's (shared)
            AssetDatabase.DeleteAsset(PrefabPath("Matilda", "PRP_Matilda_FarRiver"));
            AssetDatabase.DeleteAsset(MeshFolder("Matilda") + "/PRP_Matilda_FarRiver.asset");

            var d = LoadOrCreateDistrict(MatildaAssetPath);
            d.id = "matilda"; d.displayName = "MATILDA BAY"; d.order = 20; d.length = new Vector2(420f, 560f);
            d.chunks = chunks.ToArray(); d.orderedChunks = true; d.entry = entry; d.exit = exit; d.allowBoulevard = false;
            d.obstacles = new[] { esky, basket };
            d.jumpObstacles = Array.Empty<Obstacle>();
            d.gifts = Array.Empty<Gift>();
            d.hazards = Array.Empty<HazardPattern>();
            d.pedestrians = new[] { Person("Jogger"), Person("Jogger"), Person("DogWalker"), Person("Elder"), Person("Tourist"), Person("Surfer"), swan, swan }
                .Where(p => p != null).ToArray();
            d.ramps = new[] { jetty };
            d.rampFrequency = 1.2f;
            d.blendMeters = 150f;
            d.fogColor = new Color(.72f, .84f, .94f);          // bright, cool river haze
            d.skyTint = new Color(.94f, .98f, 1f);
            d.ambientSky = new Color(.62f, .80f, .98f);
            d.ambientEquator = new Color(.82f, .87f, .88f);
            d.ambientGround = new Color(.53f, .57f, .50f);
            d.sunColor = new Color(1f, .97f, .92f);
            d.sunIntensity = 1.5f;
            d.farScenery = far; // gateway (DistrictSetup GW_ sign), ambience and the sky fields are left as they are
            EditorUtility.SetDirty(d);
        }

        const float RiverEdge = 280f; // the chunks' own water reaches the far shore of the shared far scenery

        static Chunk MatildaBase(string id, float g, bool river = true, bool bank = true)
        {
            var c = new Chunk(id, g) { WalkLeft = _r.pavement, WalkRight = RedPath, RedRight = true };
            if (river)
            {
                c.Lawn(1, 8f, 15f, 0f, 40f, BayLawn);
                c.Beach(15f, 17.5f, 0f, 40f);
                c.Water(17.5f, RiverEdge, 0f, 40f, -.45f, RiverMid);
                c.Ripples(19f, 120f, 0f, 40f, -.43f);
                c.Bollards(0f, 40f);
            }
            if (bank) c.Escarpment(32f, 0f, 40f);
            return c;
        }

        // Entry (v0.6): the same Hackett Drive a little further north. Left: the campus edge carries on from UWA's exit
        // (campus paving with its brick border, the low limestone wall, a last cloister) and hands over to the reserve at
        // z = 22: grey paving behind a brick band, the wall ends at a pier; the Kings Park scarp rises behind the lawn.
        // Right: the red path, lawn, beach and river exactly as UWA's exit leaves them.
        static Chunk MatildaEntry(float g)
        {
            Chunk c = MatildaBase("Entry", g, true, false);
            const float campus = 22f;
            c.WalkAt = (s, z) => s > 0 ? RedPath : z < campus ? UwaPaving : _r.pavement;
            c.BorderAt = (s, z) => s < 0 && z < campus;
            c.Band(-1, campus, Brick, .6f);
            c.Lawn(-1, 8f, 75f, 0f, 40f, BayLawn);
            c.LowWall(-1, 0f, campus - .4f, .55f);
            c.WallPier(-8.2f, campus);
            c.CampusRow(-1, 16f, 0f, 20f, 10f, 6f, 4.6f, 1, true);
            c.Escarpment(32f, 12f, 40f, 0f, 20f);
            c.Tree(Jacaranda(19), -12.5f, 10f, true);
            c.Tree(PeppermintMesh(201), -12f, 27f, false);
            c.Tree(PeppermintMesh(203), -11.5f, 36f, false);
            c.Tree(PeppermintMesh(202), -20f, 31f, false);
            c.Tree(PeppermintMesh(204), 11.5f, 9f, false);
            c.Tree(PeppermintMesh(205), 12f, 31f, false);
            c.Prop(PicnicTable(), -16f, 34f, 10f);
            c.Lamp(-8.7f, 12f, true); c.Lamp(8.4f, 26f, true);
            c.Boat(48f, 14f, 3); c.Boat(80f, 34f, 4);
            c.Moored(28f, 6f, 5); c.Moored(34f, 20f, 6); c.Moored(26f, 33f, 7);
            c.Slots(10f, 25f);
            return c;
        }

        // Exit (v0.6): the road leaves the river and climbs into Kings Park. Left: the scarp comes down to the road behind
        // a limestone retaining wall (stepping up to 1.8 m by z = 20), lemon-scented gums and grass trees on the bank.
        // Right: the red path ends at z = 16 (PATH ENDS, END bar, stone band) and the shore swings away behind a bank of
        // bush and gums. At z = 40 both sides are what Kings Park's entry starts with: grey paving, limestone, bush, gums.
        static Chunk MatildaExit(float g)
        {
            Chunk c = MatildaBase("Exit", g, false, true);
            const float end = 16f;
            c.WalkAt = (s, z) => s > 0 && z < end ? RedPath : _r.pavement;
            c.RedAt = (s, z) => s > 0 && z < end;
            c.PathWords(8.6f, "PATH", "ENDS"); c.PathBar(end - 1.3f); c.Band(1, end, LimeTrim, .6f);
            // right: the shore swings away
            Func<float, float> shore = z => Mathf.Lerp(15f, 84f, Mathf.SmoothStep(0f, 1f, (z - 6f) / 30f));
            c.Lawn(1, 8f, 15f, 0f, 40f, BayLawn);
            c.Shore(0f, 40f, shore, ParkRiverKit.BushFloor);
            c.Ripples(60f, 150f, 0f, 30f, -.43f);
            c.Bollards(0f, end);
            c.ParkWall(1, end + 2f, 40f);
            c.Tree(PeppermintMesh(244), 11.8f, 7f, false);
            c.Gum(10.8f, 22f, 18f, 71); c.Gum(11.2f, 33f, 20f, 72); c.Gum(16.5f, 27f, 21f, 73); c.Gum(17f, 38f, 19f, 74); c.Gum(24f, 33f, 20f, 75);
            c.Scrub(14f, 21f, 0); c.Scrub(20f, 30f, 1); c.Scrub(13.5f, 36f, 2); c.Scrub(27f, 37f, 3); c.Scrub(22f, 22f, 1);
            c.Boat(120f, 20f, 3); c.Boat(62f, 4f, 4);
            // left: the scarp comes down to the road
            Func<float, float> wall = z => z < 16f ? .8f : z < 20f ? 1.3f : 1.8f;
            c.Lawn(-1, 8f, 32f, 0f, 40f, BayLawn);
            c.Retaining(12f, 40f, wall);
            c.Tree(PeppermintMesh(245), -12f, 5f, false);
            c.Gum(-11.5f, 18f, 17f, 81, c.BankY(11.5f, wall(18f))); c.Gum(-12.6f, 28f, 19f, 82, c.BankY(12.6f, 1.8f));
            c.Gum(-11.2f, 37f, 18f, 83, c.BankY(11.2f, 1.8f)); c.Gum(-18f, 23f, 21f, 84, c.BankY(18f, 1.8f)); c.Gum(-19f, 34f, 20f, 85, c.BankY(19f, 1.8f));
            c.Scrub(-10.6f, 24f, 0, c.BankY(10.6f, 1.8f)); c.Scrub(-14f, 32f, 1, c.BankY(14f, 1.8f)); c.Scrub(-10.8f, 31f, 3, c.BankY(10.8f, 1.8f)); c.Scrub(-15f, 20f, 2, c.BankY(15f, 1.8f));
            c.Lamp(-8.7f, 8f, true); c.Lamp(8.4f, 14f, true);
            c.Slots(10f, 25f);
            return c;
        }

        // Picnic lawn with a BBQ shelter, moored yachts on the river.
        static Chunk MatildaForeshore(float g)
        {
            Chunk c = MatildaBase("Foreshore", g);
            c.Lawn(-1, 8f, 32f, 0f, 40f, BayLawn);
            c.Prop(BbqShelter(), -15f, 18f, 90f);
            c.Prop(PicnicTable(), -12.5f, 8f, 5f); c.Prop(PicnicTable(), -13f, 28f, -8f);
            c.Tree(PeppermintMesh(210), -11f, 2f, false); c.Tree(PeppermintMesh(211), -21f, 33f, false); c.Tree(PeppermintMesh(212), -22f, 10f, false);
            c.Tree(PeppermintMesh(213), 12f, 20f, false);
            c.Boat(52f, 30f, 2); c.Boat(76f, 12f, 5);
            c.Moored(26f, 6f, 1); c.Moored(31f, 17f, 2); c.Moored(27f, 27f, 3); c.Moored(38f, 9f, 4); c.Moored(40f, 23f, 8); // the club moorings
            c.Gum(-27f, 22f, 19f, 61);
            c.Marker(24f, 34f);
            c.Lamp(-8.7f, 20f, true); c.Lamp(8.4f, 4f, true); c.Lamp(8.4f, 36f, true);
            c.Bench(-7.4f, 14f, PicnicWood);
            c.Slots(10f, 25f);
            return c;
        }

        // The landmark: the blue Crawley Edge boatshed at the end of its jetty.
        static Chunk MatildaBoatshed(float g)
        {
            Chunk c = MatildaBase("Boatshed", g);
            c.Lawn(-1, 8f, 32f, 0f, 40f, BayLawn);
            c.Jetty(15f, 38.6f, 24f, 1.7f);
            c.Prop(Boatshed(), 41f, 24f, -90f, -.45f + .9f);
            c.Prop(Eliza(), 27f, 11f, -90f, -.5f); // the diver statue on her post, a little way off the shore
            c.Moored(52f, 36f, 9);
            c.Tree(PeppermintMesh(220), 11.5f, 7f, false); c.Tree(PeppermintMesh(221), 12f, 36f, false);
            c.Tree(PeppermintMesh(222), -13f, 12f, false); c.Tree(PeppermintMesh(223), -12f, 30f, false); c.Tree(PeppermintMesh(224), -24f, 21f, false);
            c.Prop(PicnicTable(), -17f, 22f, 90f);
            c.Boat(70f, 10f, 6);
            c.Lamp(-8.7f, 4f, true); c.Lamp(8.4f, 16f, true);
            c.Bench(7.4f, 30f, PicnicWood);
            c.Slots(10f, 25f);
            return c;
        }

        // A low cafe pavilion with umbrellas on the land side.
        static Chunk MatildaCafe(float g)
        {
            Chunk c = MatildaBase("Cafe", g);
            c.Lawn(-1, 8f, 32f, 0f, 40f, BayLawn);
            c.Paving(-1, 8f, 13f, 6f, 34f, .22f);
            c.Building(CafePavilion(), -1, 13f, 4f, 8f, 20f, 8f, 32f);
            for (int i = 0; i < 3; i++) c.Prop(Umbrella(i), -10.6f, 11f + i * 8.5f, 0f);
            c.Tree(PeppermintMesh(230), -11.5f, 3f, false); c.Tree(PeppermintMesh(231), -11f, 37.5f, false);
            c.Tree(PeppermintMesh(232), 12f, 12f, false);
            c.Boat(58f, 34f, 8);
            c.Moored(27f, 12f, 10); c.Moored(34f, 24f, 11); c.Moored(29f, 33f, 12);
            c.Gum(-28f, 4f, 20f, 62);
            c.Marker(26f, 6f);
            c.Lamp(8.4f, 24f, true);
            c.Bench(7.4f, 4f, PicnicWood);
            c.Slots(10f, 25f);
            return c;
        }

        // Picnic lawn variant: two shelters, swans' favourite stretch of beach.
        static Chunk MatildaPicnic(float g)
        {
            Chunk c = MatildaBase("Picnic", g);
            c.Lawn(-1, 8f, 32f, 0f, 40f, BayLawn);
            c.Prop(BbqShelter(), -14f, 8f, 90f); c.Prop(BbqShelter(), -14f, 30f, 90f);
            c.Prop(PicnicTable(), -20f, 18f, 80f);
            c.Tree(PeppermintMesh(240), -11f, 19f, false); c.Tree(PeppermintMesh(241), -23f, 4f, false); c.Tree(PeppermintMesh(242), -24f, 36f, false);
            c.Tree(PeppermintMesh(243), 11.5f, 4f, false); c.Tree(PeppermintMesh(244), 12f, 27f, false);
            c.Boat(34f, 12f, 9); c.Boat(52f, 32f, 10);
            c.Lamp(-8.7f, 34f, true); c.Lamp(8.4f, 15f, true);
            c.Bench(7.4f, 36f, PicnicWood);
            c.Slots(10f, 25f);
            return c;
        }

        // The road crosses a short river inlet: a timber boardwalk kicker over the whole width, then 6 m of water.
        static Chunk MatildaInlet(float g)
        {
            Chunk c = MatildaBase("Inlet", g, false, true);
            const float rampZ = 14f, rise = 6f, height = 1.6f, top = 2f, gapZ = 22f, gapLen = 6f;
            c.Holes.Add(new Hole(-8f, 8f, gapZ, gapZ + gapLen));
            c.Ramps.Add(new RampSpec { X = 0f, Z = rampZ, Width = 16f, Rise = rise, Height = height, Top = top, Launch = 1f, Hang = .5f,
                Mesh = JettyRampMesh(16f, rise, height, top, g, 1.6f) });
            c.Gaps.Add(new GapSpec { X = 0f, Z = gapZ, Length = gapLen, Width = 0f, Word = "SPLASH", Water = true });
            // River side, cut by the inlet channel.
            c.Lawn(1, 8f, 15f, 0f, gapZ - .4f, BayLawn); c.Lawn(1, 8f, 15f, gapZ + gapLen + .4f, 40f, BayLawn);
            c.Beach(15f, 17.5f, 0f, gapZ - .4f); c.Beach(15f, 17.5f, gapZ + gapLen + .4f, 40f);
            c.Water(17.5f, 170f, 0f, 40f, -.45f, RiverMid);
            c.Ripples(19f, 120f, 0f, 40f, -.43f);
            // The channel under the road, into a small lagoon with reeds on the land side.
            c.Water(-26f, 17.6f, gapZ - .4f, gapZ + gapLen + .4f, -.6f, RiverMid);
            c.Water(-26f, -8.3f, gapZ - 6f, gapZ + gapLen + 5f, -.58f, RiverMid);
            c.Lawn(-1, 8f, 32f, 0f, gapZ - 6.2f, BayLawn);
            c.Lawn(-1, 8f, 32f, gapZ + gapLen + 5.2f, 40f, BayLawn);
            c.Lawn(-1, 26f, 32f, gapZ - 6.2f, gapZ + gapLen + 5.2f, BayLawn);
            c.Reeds(-25f, -9f, gapZ - 6f, gapZ + gapLen + 5f);
            c.ChannelWalls(gapZ, gapZ + gapLen);
            c.Tree(PeppermintMesh(250), -12f, 6f, false); c.Tree(PeppermintMesh(251), 12f, 36f, false); c.Tree(PeppermintMesh(252), -13f, 37f, false);
            c.Boat(40f, 20f, 11);
            c.NoStreetLife(-8f, 8f, rampZ - 1f, gapZ + gapLen + 1f);
            c.Lamp(8.4f, 8f, true);
            c.SlotRow(6f, -2.6f, 0f, 2.6f);
            c.SlotRow(34f, -2.6f, 0f, 2.6f);
            return c;
        }

        // ---- Matilda Bay parts

        // WA peppermint: a dark leaning trunk under a weeping dome, its blobs hanging lower towards the edge.
        static ComicMesh PeppermintMesh(int seed)
        {
            var rng = new System.Random(seed * 31337);
            float R() => (float)rng.NextDouble();
            var m = new ComicMesh();
            RootRing(m, .42f);
            float lean = (R() - .5f) * 1.2f;
            Vector3 bend = V(lean * .4f, 1.9f, .15f), top = V(lean, 3.6f, 0);
            Limb(m, V(0, .1f, 0), bend, .28f, PepTrunk, .85f);
            Limb(m, bend, top, .21f, PepTrunk, .75f);
            Limb(m, bend - V(0, .2f, 0), bend + V(-1.1f - lean * .3f, 1.3f, .7f), .14f, PepTrunk, .7f);
            Color dark = Color.Lerp(Peppermint, _r.ink, .2f);
            Plant(m, top + V(0, 1.2f, 0), 4.6f, Peppermint, PeppermintLight, true, .55f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.0472f + R() * .4f;
                Vector3 p = top + V(Mathf.Cos(a) * 2.15f, -.15f - (i % 2) * .35f, Mathf.Sin(a) * 2.05f);
                Plant(m, p, 1.75f, i % 2 == 0 ? Peppermint : dark, PeppermintLight, i % 3 == 0, 1.65f);
            }
            return m;
        }

        static ComicMesh PicnicTable()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            Color leg = Color.Lerp(PicnicWood, _r.ink, .35f);
            m.Box(V(0, .74f, 0), V(1.9f, .08f, .85f), PicnicWood);
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(V(0, .45f, s * .62f), V(1.9f, .07f, .3f), PicnicWood);
                m.Beam(V(s * .7f, .06f, -.72f), V(s * .7f, .72f, 0), .09f, leg);
                m.Beam(V(s * .7f, .06f, .72f), V(s * .7f, .72f, 0), .09f, leg);
                m.Box(V(s * .7f, .41f, 0), V(.09f, .07f, 1.5f), leg);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(s * .7f, .03f, -.72f), V(.16f, .06f, .2f), _r.ink);
                m.Box(V(s * .7f, .03f, .72f), V(.16f, .06f, .2f), _r.ink);
                m.Surface = ComicSurface.Wood;
            }
            return m;
        }

        // BBQ shelter: limestone slab, white posts on ink feet with cream caps, a green gable roof, a steel BBQ.
        static ComicMesh BbqShelter()
        {
            var m = new ComicMesh { Surface = ComicSurface.Concrete };
            m.Box(V(0, .08f, 0), V(5f, .16f, 3.8f), LimeRock);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Box(V(sx * 2.1f, .28f, sz * 1.5f), V(.3f, .24f, .3f), _r.ink);
                    m.Box(V(sx * 2.1f, 1.45f, sz * 1.5f), V(.16f, 2.5f, .16f), White);
                    m.Box(V(sx * 2.1f, 2.68f, sz * 1.5f), V(.24f, .1f, .24f), _r.cream);
                }
            m.Box(V(0, 2.62f, 1.5f), V(4.5f, .14f, .14f), White);
            m.Box(V(0, 2.62f, -1.5f), V(4.5f, .14f, .14f), White);
            GableRoof(m, V(0, 2.72f, 0), 5.4f, 4.2f, 1.1f, ShelterRoof, ShelterRoof, true);
            m.Surface = ComicSurface.Metal;
            m.Box(V(0, .55f, 0), V(1.6f, .8f, .8f), C(0x8A8F96));
            m.Box(V(0, .99f, 0), V(1.66f, .08f, .86f), _r.ink);
            m.Box(V(0, 1.04f, 0), V(1.3f, .03f, .55f), C(0x5A5F66));
            m.Add(PicnicTable(), V(0, .16f, -2.7f), Quaternion.identity, Vector3.one);
            return m;
        }

        // Crawley Edge boatshed: blue weatherboards, white trim, a front gable with door and two windows, on piles.
        // Local: front (door) faces +Z, pivot at the deck level centre.
        static ComicMesh Boatshed()
        {
            var m = new ComicMesh();
            const float W = 6f, D = 5f, H = 3f;
            Box(m, ComicSurface.Wood, V(0, -.12f, .4f), V(W + .6f, .24f, D + .8f), Timber);
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz++)
                    { m.Surface = ComicSurface.Wood; m.Cylinder(V(sx * (W * .5f - .1f), -1.3f, sz * 2f), .14f, 2.4f, TimberDark, 6); }
            Box(m, ComicSurface.Wood, V(0, H * .5f, 0), V(W, H, D), ShedBlue);
            // weatherboard lines
            for (int i = 1; i < 6; i++)
                foreach (Vector3 n in new[] { Vector3.forward, Vector3.right, Vector3.left, Vector3.back })
                {
                    float half = n.z != 0 ? W * .5f : D * .5f, depth = n.z != 0 ? D * .5f : W * .5f;
                    Vector3 right = Vector3.Cross(Vector3.up, n);
                    Slab(m, n * (depth + .015f) + V(0, i * H / 6f, 0), right, n, half * 2f, .035f, Color.Lerp(ShedBlue, _r.ink, .25f));
                }
            // corner boards
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
                    Box(m, ComicSurface.Wood, V(sx * W * .5f, H * .5f, sz * D * .5f), V(.16f, H, .16f), White);
            // front gable (wall colour), white bargeboards, light roof; ridge front to back
            GableRoof(m, V(0, H, 0), W + .5f, D + .5f, 1.6f, C(0x7F97AC), ShedBlue, false); // blue-grey roof (photographs)
            Vector3 f = V(0, 0, D * .5f + .03f);
            Panel(m, f + V(0, 1.05f, 0), Vector3.right, Vector3.up, Vector3.forward, 1f, 2.1f, White);
            Panel(m, f + V(0, 1.05f, .01f), Vector3.right, Vector3.up, Vector3.forward, .8f, 1.9f, C(0xE6E9EC));
            for (int s = -1; s <= 1; s += 2)
            {
                Panel(m, f + V(s * 1.9f, 1.75f, 0), Vector3.right, Vector3.up, Vector3.forward, .95f, .95f, White);
                Panel(m, f + V(s * 1.9f, 1.75f, .01f), Vector3.right, Vector3.up, Vector3.forward, .7f, .7f, C(0x2C3A4A));
            }
            // flag pole on the ridge
            m.Surface = ComicSurface.Metal;
            m.Cylinder(V(-W * .3f, H + 2.4f, 0), .05f, 2.4f, White, 8);
            m.Surface = ComicSurface.Fabric;
            m.Box(V(-W * .3f + .45f, H + 3.25f, 0), V(.8f, .5f, .06f), C(0x1F2E78));
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(V(-W * .3f, H + 3.66f, 0), .07f, .1f, _r.ink, 8);
            return m;
        }

        static ComicMesh CafePavilion()
        {
            var m = new ComicMesh();
            const float W = 24f, D = 8f, H = 3.6f;
            Box(m, ComicSurface.Plaster, V(0, H * .5f, 0), V(W, H, D), White);
            Box(m, ComicSurface.Plaster, V(0, .25f, 0), V(W + .2f, .5f, D + .2f), LimeRock);
            Box(m, ComicSurface.PaintedMetal, V(0, H + .15f, .6f), V(W + 1.2f, .3f, D + 2.4f), C(0x5B6F7C));
            for (int i = 0; i < 8; i++)
            {
                float x = -W * .5f + (i + .5f) * W / 8f;
                Panel(m, V(x, 1.75f, D * .5f + .03f), Vector3.right, Vector3.up, Vector3.forward, W / 8f * .8f, 2.3f, _r.glass);
                Box(m, ComicSurface.PaintedMetal, V(-W * .5f + i * W / 8f, 1.75f, D * .5f + .06f), V(.14f, 2.5f, .1f), C(0x5B6F7C));
            }
            // awning band in Matilda blue
            Box(m, ComicSurface.Fabric, V(0, H - .35f, D * .5f + .9f), V(W, .25f, 1.8f), ShedBlue);
            return m;
        }

        static ComicMesh Umbrella(int i)
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .05f, 0), .26f, .1f, _r.ink, 8);
            m.Cylinder(V(0, 1.2f, 0), .05f, 2.3f, White, 8);
            m.Surface = ComicSurface.Fabric;
            m.Cylinder(V(0, 2.3f, 0), 1.4f, .45f, i % 2 == 0 ? _r.cream : ShedBlue, 8, .05f);
            m.Cylinder(V(0, 2.05f, 0), 1.42f, .08f, i % 2 == 0 ? ShedBlue : _r.cream, 8);
            m.Surface = ComicSurface.Wood;
            m.Cylinder(V(0, .74f, 0), .45f, .08f, PicnicWood, 8);
            return m;
        }

        // A sail as a thin solid (not a card): triangle a-b-c extruded by t.
        static void Sail(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, float t, Color color)
        {
            m.Surface = ComicSurface.Fabric;
            Vector3 n = Vector3.Cross(b - a, c - a).normalized * (t * .5f);
            Tri(m, a + n, b + n, c + n, color, n);
            Tri(m, a - n, b - n, c - n, color, -n);
            Vector3[] p = { a, b, c };
            for (int i = 0; i < 3; i++)
            {
                Vector3 p0 = p[i], p1 = p[(i + 1) % 3], out_ = Vector3.Cross(p1 - p0, n).normalized;
                if (Vector3.Dot(out_, p0 - (a + b + c) / 3f) < 0f) out_ = -out_;
                Face(m, p0 - n, p1 - n, p1 + n, p0 + n, Color.Lerp(color, _r.ink, .15f), out_);
            }
        }

        static ComicMesh SailBoat(int seed)
        {
            var rng = new System.Random(seed * 17);
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            float len = 4f + (float)rng.NextDouble() * 2f;
            Color hull = seed % 3 == 0 ? C(0x1F3F7A) : White;
            m.Box(V(0, .25f, 0), V(1.3f, .55f, len), hull);
            m.Box(V(0, .3f, len * .5f + .25f), V(.8f, .45f, .6f), hull, Quaternion.Euler(0, 45, 0));
            m.Box(V(0, .04f, 0), V(1.34f, .1f, len + .04f), _r.ink);
            m.Box(V(0, .62f, -.2f), V(.9f, .35f, 1.4f), White);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(V(0, 3.6f, .4f), .06f, 6.4f, White, 8);
            Color sail = seed % 4 == 1 ? C(0xF1D9A4) : White;
            Sail(m, V(0, .95f, .3f), V(0, 6.6f, .3f), V(0, .95f, -len * .45f), .06f, sail);
            Sail(m, V(0, .95f, .55f), V(0, 5.6f, .5f), V(0, .8f, len * .5f + .4f), .06f, Color.Lerp(sail, _r.cream, .3f));
            return m;
        }

        static ComicMesh Esky()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            m.Box(V(0, .04f, 0), V(.74f, .08f, .48f), _r.ink);
            m.Box(V(0, .26f, 0), V(.72f, .38f, .46f), EskyBlue);
            m.Box(V(0, .49f, 0), V(.78f, .1f, .52f), White);
            m.Box(V(0, .58f, 0), V(.42f, .06f, .1f), White);
            for (int s = -1; s <= 1; s += 2) { m.Box(V(s * .19f, .55f, 0), V(.06f, .08f, .1f), White); m.Box(V(s * .385f, .3f, 0), V(.06f, .08f, .22f), White); }
            Panel(m, V(0, .27f, -.235f), Vector3.left, Vector3.up, Vector3.back, .46f, .1f, White);
            return m;
        }

        static ComicMesh PicnicBasket()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            Color band = Color.Lerp(Wicker, _r.ink, .35f), lid = Color.Lerp(Wicker, _r.cream, .25f);
            m.Box(V(0, .03f, 0), V(.68f, .06f, .46f), _r.ink);
            m.Box(V(0, .22f, 0), V(.66f, .32f, .44f), Wicker);
            for (int i = 0; i < 3; i++) Panel(m, V(0, .11f + i * .1f, -.225f), Vector3.left, Vector3.up, Vector3.back, .66f, .03f, band);
            for (int s = -1; s <= 1; s += 2) m.Box(V(s * .17f, .42f, 0), V(.32f, .08f, .48f), lid, Quaternion.Euler(0, 0, s * -6f));
            Limb(m, V(-.24f, .44f, 0), V(-.15f, .62f, 0), .035f, band, 1f);
            m.Box(V(0, .63f, 0), V(.32f, .07f, .07f), band);
            Limb(m, V(.15f, .62f, 0), V(.24f, .44f, 0), .035f, band, 1f);
            m.Surface = ComicSurface.Fabric;
            for (int i = 0; i < 4; i++) m.Box(V(-.24f + i * .16f, .38f, -.2f), V(.15f, .07f, .09f), i % 2 == 0 ? C(0xD23A3A) : White);
            return m;
        }

        // Black swan facing -Z, feet at the pivot: chunky black body, curly wing feathers with white tips, an S-curved
        // neck of 8-sided segments, red beak with a white band.
        static ComicMesh SwanMesh()
        {
            var m = new ComicMesh();
            Color feather = C(0x2B2E36);
            m.Surface = ComicSurface.PaintedMetal;
            for (int s = -1; s <= 1; s += 2) { m.Box(V(s * .09f, .11f, .06f), V(.07f, .2f, .07f), C(0x2A2A2E)); m.Box(V(s * .09f, .03f, -.02f), V(.14f, .06f, .2f), _r.ink); }
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(V(0, .43f, .12f), V(.5f, .42f, .84f), Swan);
            m.Ellipsoid(V(0, .56f, .3f), V(.42f, .26f, .52f), feather);
            for (int i = 0; i < 3; i++) Blade(m, V((i - 1) * .1f, .62f, .3f), V((i - 1) * .13f, .76f, .54f), Vector3.up, .1f, .15f, .06f, feather, Swan, ComicSurface.Fabric);
            for (int s = -1; s <= 1; s += 2) m.Box(V(s * .2f, .45f, .46f), V(.07f, .09f, .22f), White);
            Vector3 n0 = V(0, .5f, -.22f), n1 = V(0, .74f, -.38f), n2 = V(0, .98f, -.36f), n3 = V(0, 1.14f, -.26f);
            Limb(m, n0, n1, .09f, Swan, .9f, ComicSurface.Fabric);
            Limb(m, n1, n2, .08f, Swan, .9f, ComicSurface.Fabric);
            Limb(m, n2, n3, .075f, Swan, .95f, ComicSurface.Fabric);
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(V(0, 1.18f, -.3f), V(.15f, .15f, .21f), Swan);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, 1.16f, -.44f), V(.07f, .06f, .15f), SwanBeak);
            m.Box(V(0, 1.165f, -.515f), V(.075f, .065f, .03f), White);
            for (int s = -1; s <= 1; s += 2) m.Box(V(s * .072f, 1.2f, -.34f), V(.02f, .035f, .035f), SwanBeak);
            return m;
        }

        // A yacht on its mooring (v0.6): white hull with a coloured boot stripe, cabin, bare mast and boom with the sail
        // furled along it, an orange buoy off the bow. The bay's dozens of moored boats, next to the ones sailing.
        static ComicMesh MooredYacht(int seed)
        {
            var rng = new System.Random(seed * 53 + 7);
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            float len = 5f + (float)rng.NextDouble() * 2.5f;
            Color stripe = seed % 3 == 0 ? C(0xB23A32) : seed % 3 == 1 ? C(0x1F3F7A) : C(0x2F6F5A);
            m.Box(V(0, .3f, 0), V(1.5f, .6f, len), White);
            m.Box(V(0, .34f, len * .5f + .3f), V(.95f, .5f, .7f), White, Quaternion.Euler(0, 45, 0));
            m.Box(V(0, .1f, 0), V(1.54f, .16f, len + .04f), stripe);
            m.Box(V(0, .75f, -.3f), V(1.05f, .4f, len * .34f), White);
            m.Box(V(0, .78f, -.3f), V(1.08f, .16f, len * .26f), C(0x2C3A4A));
            m.Surface = ComicSurface.Metal;
            float mast = len * 1.25f;
            m.Cylinder(V(0, .6f + mast * .5f, len * .12f), .06f, mast, C(0xC9CDD2), 8);
            m.Cylinder(V(0, 1.55f, len * .12f - len * .22f), .05f, len * .44f, C(0xC9CDD2), 8, 1f, Quaternion.Euler(90, 0, 0));
            m.Surface = ComicSurface.Fabric;
            m.Box(V(0, 1.68f, len * .12f - len * .22f), V(.2f, .2f, len * .4f), stripe);
            m.Surface = ComicSurface.Plastic;
            m.Cylinder(V(.5f, .28f, len * .5f + 2.2f), .28f, .4f, C(0xF08A2C), 8, .7f);
            return m;
        }

        // "Eliza" (v0.6): the bronze diver on her post in the river by the Crawley Edge boatshed: a steel pylon, a
        // timber-clad box, the figure about to dive with her arms raised and spread. Pivot at the water.
        static ComicMesh Eliza()
        {
            var m = new ComicMesh { Surface = ComicSurface.Metal };
            Color bronze = C(0x6B4A2C), light = C(0x8A6238);
            m.Cylinder(V(0, 1.5f, 0), .16f, 3.6f, C(0x7C8086), 8);
            m.Surface = ComicSurface.Wood;
            m.Box(V(0, 3.5f, 0), V(.9f, .7f, .9f), Timber);
            m.Box(V(0, 3.88f, 0), V(1f, .08f, 1f), TimberDark);
            m.Surface = ComicSurface.Metal;
            for (int s = -1; s <= 1; s += 2) m.Box(V(s * .13f, 4.42f, 0), V(.17f, 1f, .2f), bronze);
            m.Box(V(0, 5.3f, .03f), V(.46f, .8f, .26f), light, Quaternion.Euler(8f, 0, 0));
            m.Ellipsoid(V(0, 5.92f, .1f), V(.3f, .34f, .3f), bronze);
            for (int s = -1; s <= 1; s += 2) TrunkAKit.Rod(m, V(s * .22f, 5.6f, .06f), V(s * .75f, 6.45f, .2f), .07f, light, .8f);
            return m;
        }

        // =====================================================================================================
        // Chunk drafting
        // =====================================================================================================

        struct Hole
        {
            public float X0, X1, Z0, Z1;
            public Hole(float x0, float x1, float z0, float z1) { X0 = x0; X1 = x1; Z0 = z0; Z1 = z1; }
        }

        sealed class RampSpec { public float X, Z, Width, Rise, Height, Top, Launch, Hang; public ComicMesh Mesh; }
        sealed class GapSpec { public float X, Z, Length, Width; public string Word; public bool Water; }
        sealed class Block { public string Id; public Vector3 Position, Size; public float Yaw; public bool Jump; public bool Obstacle = true; }

        sealed class Chunk
        {
            public readonly string Id;
            public readonly float L = 40f, G;
            public Color WalkLeft, WalkRight, BorderColor;
            public bool RedRight;
            // v0.6: surfaces change INSIDE entry / exit chunks. Sidewalk colour, red shared path and brick border per
            // (side, z); null = the chunk's WalkLeft / WalkRight, RedRight and BorderColor all the way.
            public Func<int, float, Color> WalkAt;
            public Func<int, float, bool> RedAt, BorderAt;
            // Extra paint on the flat road / sidewalks (bands, SHARED PATH letters), drawn before the slope shear.
            public readonly List<Action<ComicMesh>> Paint = new List<Action<ComicMesh>>();
            public readonly ComicMesh Front = new ComicMesh(), Props = new ComicMesh(), Back = new ComicMesh();
            public readonly List<Hole> Holes = new List<Hole>();
            public readonly List<RampSpec> Ramps = new List<RampSpec>();
            public readonly List<GapSpec> Gaps = new List<GapSpec>();
            public readonly List<Block> Blocks = new List<Block>();
            public readonly List<Vector2> SlotList = new List<Vector2>();

            public Chunk(string id, float g) { Id = id; G = g; }

            public float Y(float z) => -G * z;
            public float Top(float z) => -G * z + Kerb;

            public bool IsHole(float x0, float x1, float z0, float z1) =>
                Holes.Any(h => x0 >= h.X0 - .01f && x1 <= h.X1 + .01f && z0 >= h.Z0 - .01f && z1 <= h.Z1 + .01f);

            public void Slots(params float[] rows) { foreach (float z in rows) SlotRow(z, -2.6f, 0f, 2.6f); }
            public void SlotRow(float z, params float[] lanes) { foreach (float x in lanes) SlotList.Add(new Vector2(x, z)); }

            // ---- v0.6 surface transitions inside entry / exit chunks

            /// <summary>A stone band across one sidewalk, where its surface changes.</summary>
            public void Band(int side, float z, Color color, float width = .5f)
            {
                Paint.Add(m => Flat(m, side < 0 ? -Half - Walk : Half + .24f, side < 0 ? -Half - .24f : Half + Walk, z - width * .5f, z + width * .5f, Kerb + .03f, color));
            }

            /// <summary>Two words painted on the right-hand path for a rider going +Z (tall road-marking letters).</summary>
            public void PathWords(float z, string near, string far)
            {
                Paint.Add(m =>
                {
                    Vector3 tall = Vector3.forward * 2.2f;
                    TrunkAKit.Text(m, near, V(Half + 2.1f, Kerb + .03f, z), Vector3.right, tall, Vector3.up, .1f, White);
                    TrunkAKit.Text(m, far, V(Half + 2.1f, Kerb + .03f, z + 1.7f), Vector3.right, tall, Vector3.up, .1f, White);
                });
            }

            /// <summary>A thick white bar across the right-hand path (start / end of the shared path).</summary>
            public void PathBar(float z)
            {
                Paint.Add(m => Flat(m, Half + .5f, Half + Walk - .3f, z - .15f, z + .15f, Kerb + .03f, White));
            }

            // ---- ground (sheared with the slope; no shadows)

            void Sheared(ComicMesh target, Action<ComicMesh> build)
            {
                var t = new ComicMesh();
                build(t);
                t.Shear(G);
                target.Add(t, Vector3.zero, Quaternion.identity, Vector3.one);
            }

            static (float, float) Span(int side, float a, float b) => side < 0 ? (-b, -a) : (a, b);

            public void Lawn(int side, float a, float b, float z0, float z1, Color? color = null, float top = Kerb)
            {
                (float x0, float x1) = Span(side, a, b);
                Color col = color ?? Grass;
                Sheared(Back, t => { t.Surface = ComicSurface.Foliage; StripSlab(t, x0, x1, z0, z1, top, col, 4f); });
            }

            public void Paving(int side, float a, float b, float z0, float z1, float top = Kerb + .01f)
            {
                (float x0, float x1) = Span(side, a, b);
                Color col = RedRight ? _r.pavement : UwaPaving;
                Sheared(Back, t => { t.Surface = ComicSurface.Paving; StripSlab(t, x0, x1, z0, z1, top, col, 4f); });
            }

            public void Beach(float a, float b, float z0, float z1)
            {
                Sheared(Back, t =>
                {
                    t.Surface = ComicSurface.Concrete;
                    for (float z = z0; z < z1 - .01f; z += 4f)
                    {
                        float e = Mathf.Min(z1, z + 4f);
                        Face(t, V(a, Kerb, z), V(a, Kerb, e), V(b, -.55f, e), V(b, -.55f, z), Sand, Vector3.up, false);
                    }
                });
            }

            public void Water(float x0, float x1, float z0, float z1, float y, Color col)
            {
                Sheared(Back, t => { t.Surface = ComicSurface.Plain; StripSlab(t, x0, x1, z0, z1, y, col, 4f, false); });
            }

            public void Ripples(float x0, float x1, float z0, float z1, float y)
            {
                var rng = new System.Random(Id.GetHashCode() & 0xffff);
                Sheared(Back, t =>
                {
                    for (int i = 0; i < 26; i++)
                    {
                        float x = Mathf.Lerp(x0, x1, (float)rng.NextDouble()), z = Mathf.Lerp(z0 + 1f, z1 - 1f, (float)rng.NextDouble()), w = .8f + (float)rng.NextDouble() * 2.4f;
                        FlatQuad(t, x - w, x + w, z, z + .22f, y, i % 4 == 0 ? White : RiverLight);
                    }
                });
            }

            public void Lilies(float x0, float x1, float z0, float z1, float y)
            {
                var rng = new System.Random(77);
                Sheared(Back, t =>
                {
                    for (int i = 0; i < 16; i++)
                    {
                        Vector3 p = V(Mathf.Lerp(x0, x1, (float)rng.NextDouble()), y, Mathf.Lerp(z0 + .5f, z1 - .5f, (float)rng.NextDouble()));
                        Disc(t, p, Vector3.up, .35f + (float)rng.NextDouble() * .25f, C(0x4E9A44), 7);
                        if (i % 3 == 0) Disc(t, p + V(.1f, .03f, .05f), Vector3.up, .12f, C(0xF3B6D4), 5);
                    }
                });
            }

            // Reflection pond on a lawn: water just above the lawn, a limestone rim.
            public void Pond(float x0, float x1, float z0, float z1)
            {
                // v0.6: dark still water almost flush with the limestone paving (no lilies here); the hall's front and
                // the tower show in it as pale streaks (the hall is on the x0 side, its tower at the z1 end).
                Color water = C(0x2C5F7E), mirror = Color.Lerp(water, Lime, .5f), sky = C(0x7FB3D2);
                Sheared(Back, t =>
                {
                    t.Surface = ComicSurface.Plain;
                    StripSlab(t, x0, x1, z0, z1, .27f, water, 4f, false);
                    float w = x1 - x0;
                    for (float z = z0 + 2f; z < z1 - 6f; z += 4.4f) FlatQuad(t, x0 + .3f, x0 + w * .34f, z, z + 3.2f, .285f, mirror);
                    FlatQuad(t, x0 + .3f, x1 - .5f, z1 - 4.2f, z1 - 1.4f, .285f, mirror);                       // the tower
                    FlatQuad(t, x0 + w * .62f, x0 + w * .68f, z1 - 3.4f, z1 - 2.2f, .29f, ClockBlue);          // its clock
                    for (int i = 0; i < 5; i++) FlatQuad(t, x0 + w * .5f + i * .5f, x1 - .6f - i * .3f, z0 + 3f + i * 5.2f, z0 + 3.25f + i * 5.2f, .285f, sky);
                });
                Sheared(Front, t =>
                {
                    t.Surface = ComicSurface.Plaster;
                    StripBox(t, x0 - .5f, x0, z0 - .5f, z1 + .5f, 0f, .34f, LimeTrim);
                    StripBox(t, x1, x1 + .5f, z0 - .5f, z1 + .5f, 0f, .34f, LimeTrim);
                    t.Box(V((x0 + x1) * .5f, .17f, z0 - .25f), V(x1 - x0, .34f, .5f), LimeTrim);
                    t.Box(V((x0 + x1) * .5f, .17f, z1 + .25f), V(x1 - x0, .34f, .5f), LimeTrim);
                    // low stone bench walls at both ends
                    t.Box(V((x0 + x1) * .5f, .45f, z0 - 1.3f), V(x1 - x0 - 2f, .5f, .6f), Lime);
                    t.Box(V((x0 + x1) * .5f, .45f, z1 + 1.3f), V(x1 - x0 - 2f, .5f, .6f), Lime);
                });
            }

            public void PondRim(float x0, float x1, float z0, float z1)
            {
                Sheared(Front, t =>
                {
                    t.Surface = ComicSurface.Plaster;
                    StripBox(t, x0, x1, z0 - .3f, z0, -.6f, .45f, LimeTrim);
                    StripBox(t, x0, x1, z1, z1 + .3f, -.6f, .45f, LimeTrim);
                    t.Box(V(x1 + .15f, -.05f, (z0 + z1) * .5f), V(.3f, 1f, z1 - z0 + .6f), LimeTrim);
                });
            }

            public void LowWall(int side, float z0, float z1, float height)
            {
                (float x0, float x1) = Span(side, 8f, 8.4f);
                Sheared(Front, t =>
                {
                    t.Surface = ComicSurface.Plaster;
                    StripBox(t, x0, x1, z0, z1, -.3f, Kerb + height, Lime);
                    StripBox(t, x0 - .04f, x1 + .04f, z0, z1, Kerb + height, Kerb + height + .08f, LimeTrim);
                });
            }

            public void HedgeRow(int side, float x, float z0, float z1)
            {
                float xc = side * x;
                Sheared(Props, t => { t.Surface = ComicSurface.Foliage; StripBox(t, xc - .5f, xc + .5f, z0, z1, 0f, Kerb + 1f, Hedge); });
            }

            // The Kings Park scarp behind the reserve (Mount Eliza, v0.6): a gentle foot, then a steep face of dark scrub
            // with pale limestone outcrops and bare sand, tree crowns and gums along the top. h0 -> h1: its height at
            // z0 / z1 (it rises out of the lawn in the entry chunk).
            public void Escarpment(float from, float z0, float z1, float h0 = 20f, float h1 = 20f)
            {
                float H(float z) => Mathf.Lerp(h0, h1, Mathf.InverseLerp(z0, z1, z));
                Color foot = Color.Lerp(Bush, BayLawn, .35f), scrub = C(0x5C6E3C), top = C(0x66773F), sand = C(0xCDBF94);
                Sheared(Back, t =>
                {
                    t.Surface = ComicSurface.Foliage;
                    for (float z = z0; z < z1 - .01f; z += 4f)
                    {
                        float e = Mathf.Min(z1, z + 4f), a = H(z), b = H(e);
                        Face(t, V(-from, Kerb, z), V(-from, Kerb, e), V(-from - 8f, Kerb + b * .22f, e), V(-from - 8f, Kerb + a * .22f, z), foot, V(1, 1, 0), false);
                        Face(t, V(-from - 8f, Kerb + a * .22f, z), V(-from - 8f, Kerb + b * .22f, e), V(-from - 25f, Kerb + b, e), V(-from - 25f, Kerb + a, z), scrub, V(1, 1, 0), false);
                        Face(t, V(-from - 25f, Kerb + a, z), V(-from - 25f, Kerb + b, e), V(-from - 120f, Kerb + b, e), V(-from - 120f, Kerb + a, z), top, Vector3.up, false);
                    }
                });
                var rng = new System.Random(Id.GetHashCode() & 0xfff);
                float R() => (float)rng.NextDouble();
                float len = z1 - z0;
                // scrub, sand patches and outcrops on the face
                for (int i = 0; i < 14; i++)
                {
                    float u = .1f + R() * .85f, z = z0 + (i + R()) * len / 14f, h = H(z);
                    if (h < 3f) continue;
                    float x = -from - 8f - u * 17f, y = Top(z) + h * (.22f + u * .78f);
                    Back.Surface = ComicSurface.Foliage;
                    if (i % 5 == 2) Back.Ellipsoid(V(x + 1f, y + .1f, z), V(5f + R() * 3f, .9f, 3.5f), sand);
                    else Back.Ellipsoid(V(x, y + .7f, z), V(4.5f + R() * 3.5f, 2.2f + R(), 4f + R() * 2f), i % 3 == 0 ? BushDark : Color.Lerp(scrub, BushDark, R() * .6f));
                    if (i % 4 == 1)
                    {
                        Back.Surface = ComicSurface.Concrete;
                        Back.Box(V(x - 1.2f, y + .9f, z + 2f), V(3.4f, 2.2f, 2.6f), LimeRock, Quaternion.Euler(0, R() * 40f, 8f));
                        Back.Box(V(x - 2.2f, y + .3f, z + 3.6f), V(2.2f, 1.4f, 1.8f), Color.Lerp(LimeRock, _r.ink, .12f), Quaternion.Euler(0, R() * 40f, -6f));
                    }
                }
                // the tree line on top: crowns in two tones, a few pale gum trunks
                for (int i = 0; i < 5; i++)
                {
                    float z = z0 + (i + .2f + R() * .6f) * len / 5f, h = H(z), x = -from - 27f - R() * 10f;
                    if (h < 6f) continue;
                    Back.Surface = ComicSurface.Wood;
                    Back.Cylinder(V(x, Top(z) + h + 3.5f, z), .4f, 7f, ParkRiverKit.GumBark, 6, .7f);
                    Back.Surface = ComicSurface.Foliage;
                    Back.Ellipsoid(V(x, Top(z) + h + 8.5f, z), V(9f + R() * 3f, 6f, 8f), C(0x58703A));
                    Back.Ellipsoid(V(x + 1.6f, Top(z) + h + 9.6f, z - .8f), V(5.5f, 3.6f, 5f), C(0x748C48));
                    Back.Ellipsoid(V(x - 9f - R() * 8f, Top(z) + h + 5f, z + 3f), V(12f, 8f, 10f), C(0x4C6436));
                }
            }

            // ---- v0.6 pieces

            public void Pylon(int side, float z) => Front.Add(GatePylon(side), V(side * 9.5f, Top(z) - .2f, z), Quaternion.identity, Vector3.one);
            public void KerbTree(float x, float z) => Props.Add(StreetTreeMesh(), V(x, Top(z), z), Quaternion.identity, Vector3.one);
            public void FigTree(float x, float z, float spread, int seed) => Props.Add(FigMesh(seed, spread), V(x, Top(z), z), Quaternion.identity, Vector3.one);
            public void Sign(float x, float z, string name) => Props.Add(StreetSign(name), V(x, Top(z), z), Quaternion.identity, Vector3.one);
            public void Moored(float x, float z, int seed) => Props.Add(MooredYacht(seed), V(x, Y(z) - .6f, z), Quaternion.Euler(0, 150f + seed * 23f % 60f, 0), Vector3.one);

            /// <summary>The end pier of a low limestone wall.</summary>
            public void WallPier(float x, float z)
            {
                Front.Surface = ComicSurface.Plaster;
                Front.Box(V(x, Top(z) + .45f, z), V(.7f, 1.5f, .7f), Lime);
                Front.Box(V(x, Top(z) + 1.26f, z), V(.84f, .14f, .84f), LimeTrim);
            }

            /// <summary>Timber bollards along the lawn edge of the shared path, every 5 m.</summary>
            public void Bollards(float z0, float z1)
            {
                for (float z = z0 + 2.5f; z < z1; z += 5f)
                {
                    Props.Surface = ComicSurface.Wood;
                    Props.Cylinder(V(8.5f, Top(z) + .3f, z), .11f, .6f, Timber, 8);
                    Props.Cylinder(V(8.5f, Top(z) + .63f, z), .125f, .07f, TimberDark, 8);
                }
            }

            /// <summary>
            /// The river bank on the right where the shore line is not straight: ground from x = 15 out to shore(z), a strip
            /// of beach along it and the river beyond (in 2 m steps).
            /// </summary>
            public void Shore(float z0, float z1, Func<float, float> shore, Color ground)
            {
                Sheared(Back, t =>
                {
                    for (float z = z0; z < z1 - .01f; z += 2f)
                    {
                        float e = Mathf.Min(z1, z + 2f), a = shore(z), b = shore(e);
                        t.Surface = ComicSurface.Foliage;
                        Face(t, V(15f, Kerb, z), V(15f, Kerb, e), V(b, Kerb, e), V(a, Kerb, z), ground, Vector3.up, false);
                        t.Surface = ComicSurface.Concrete;
                        Face(t, V(a, Kerb, z), V(b, Kerb, e), V(b + 2.5f, -.55f, e), V(a + 2.5f, -.55f, z), Sand, Vector3.up, false);
                    }
                    t.Surface = ComicSurface.Plain;
                    StripSlab(t, 17.5f, RiverEdge, z0, z1, -.45f, RiverMid, 4f, false);
                });
            }

            /// <summary>Kings Park's low limestone edge wall (the same numbers as KingsParkBuilder.LimestoneEdge).</summary>
            public void ParkWall(int side, float z0, float z1, float height = .45f)
            {
                Sheared(Front, t => ParkRiverKit.Wall(t, side * 8.3f, .5f, height, z0, z1, ParkRiverKit.Limestone, ParkRiverKit.LimestoneCap));
            }

            /// <summary>Height above the road of the bush bank behind the retaining wall at |x|, for a wall `wall` high.</summary>
            public float BankY(float x, float wall) => Mathf.Lerp(wall, 5.4f - Kerb, Mathf.InverseLerp(8.9f, 40f, x));

            /// <summary>
            /// A limestone retaining wall along the left sidewalk (its height above the sidewalk from `height`) and the bush
            /// bank behind it, rising to the foot of the scarp.
            /// </summary>
            public void Retaining(float z0, float z1, Func<float, float> height)
            {
                Sheared(Front, t =>
                {
                    for (float z = z0; z < z1 - .01f; z += 4f)
                    {
                        float e = Mathf.Min(z1, z + 4f), h = Kerb + height((z + e) * .5f);
                        t.Surface = ComicSurface.Concrete;
                        t.Box(V(-8.35f, (h - .6f) * .5f, (z + e) * .5f), V(.6f, h + .6f, e - z), ParkRiverKit.Limestone);
                        t.Box(V(-8.35f, h + .06f, (z + e) * .5f), V(.72f, .12f, e - z), ParkRiverKit.LimestoneCap);
                    }
                });
                Sheared(Back, t =>
                {
                    t.Surface = ComicSurface.Foliage;
                    for (float z = z0; z < z1 - .01f; z += 4f)
                    {
                        float e = Mathf.Min(z1, z + 4f), h = Kerb + height((z + e) * .5f) - .05f;
                        Face(t, V(-8.6f, h, z), V(-8.6f, h, e), V(-40f, 5.4f, e), V(-40f, 5.4f, z), ParkRiverKit.BushFloor, Vector3.up, false);
                        Face(t, V(-8.6f, Kerb, z), V(-40f, Kerb, z), V(-40f, 5.4f, z), V(-8.6f, h, z), ParkRiverKit.BushFloorDark, Vector3.back, false);
                    }
                });
            }

            /// <summary>A lemon-scented gum (ParkRiverKit's), y above the sidewalk level.</summary>
            public void Gum(float x, float z, float h, int seed, float y = 0f) => ParkRiverKit.Gum(Props, V(x, Top(z) + y, z), h, seed);

            /// <summary>Kings Park undergrowth: 0 balga, 1 banksia, 2 shrub, 3 kangaroo paw.</summary>
            public void Scrub(float x, float z, int kind, float y = 0f)
            {
                Vector3 p = V(x, Top(z) + y, z);
                int seed = Mathf.RoundToInt(x * 13f + z * 7f);
                switch (kind)
                {
                    case 0: ParkRiverKit.Balga(Props, p, 1.1f, seed); break;
                    case 1: ParkRiverKit.Banksia(Props, p, 1f, seed); break;
                    case 2: ParkRiverKit.Shrub(Props, p, 1.3f, Color.Lerp(ParkRiverKit.BanksiaLeaf, ParkRiverKit.GumLeaf, .5f)); break;
                    default: ParkRiverKit.KangarooPaw(Props, p, 1.2f, seed); break;
                }
            }

            public void Reeds(float x0, float x1, float z0, float z1)
            {
                var rng = new System.Random(55);
                for (int i = 0; i < 14; i++)
                {
                    bool edge = i % 2 == 0;
                    float x = edge ? Mathf.Lerp(x0, x1, (float)rng.NextDouble()) : x0 + (float)rng.NextDouble() * 2f;
                    float z = edge ? (i % 4 == 0 ? z0 + .6f : z1 - .6f) : Mathf.Lerp(z0, z1, (float)rng.NextDouble());
                    Props.Surface = ComicSurface.Foliage;
                    Props.Cylinder(V(x, Y(z) - .55f + .7f, z), .35f, 1.4f, C(0x7C9A48), 6, .2f);
                }
            }

            public void ChannelWalls(float z0, float z1)
            {
                Sheared(Front, t =>
                {
                    t.Surface = ComicSurface.Concrete;
                    foreach (float z in new[] { z0, z1 })
                        for (int s = -1; s <= 1; s += 2)
                            t.Box(V(s * 12.5f, -.25f, z + (z == z0 ? -.2f : .2f)), V(9f, .9f, .4f), LimeRock);
                });
            }

            public void Jetty(float x0, float x1, float z, float width)
            {
                Sheared(Front, t =>
                {
                    t.Surface = ComicSurface.Wood;
                    float y = .36f;
                    t.Box(V((x0 + x1) * .5f, y, z), V(x1 - x0, .18f, width), Timber);
                    for (float x = x0 + .5f; x < x1; x += .6f) FlatQuad(t, x, x + .04f, z - width * .5f, z + width * .5f, y + .095f, Color.Lerp(Timber, _r.ink, .35f));
                    for (float x = x0 + 2f; x < x1; x += 3f)
                        for (int s = -1; s <= 1; s += 2) t.Box(V(x, -.4f, z + s * (width * .5f - .1f)), V(.18f, 1.4f, .18f), TimberDark);
                });
            }

            // ---- upright things (not sheared)

            public void Tree(ComicMesh tree, float x, float z, bool petals)
            {
                Props.Add(tree, V(x, Top(z), z), Quaternion.Euler(0, (x * 37f + z * 11f) % 360f, 0), Vector3.one);
                // the purple carpet of fallen flowers: a thin slab disc following the slope
                if (petals) Sheared(Back, t => { t.Surface = ComicSurface.Foliage; t.Cylinder(V(x, Kerb + .03f, z), 3.5f, .06f, Petals, 10); });
            }

            public void Prop(ComicMesh prop, float x, float z, float yaw, float? y = null)
            {
                Props.Add(prop, V(x, y.HasValue ? Y(z) + y.Value : Top(z), z), Quaternion.Euler(0, yaw, 0), Vector3.one);
            }

            public void Boat(float x, float z, int seed)
            {
                Props.Add(SailBoat(seed), V(x, Y(z) - .6f, z), Quaternion.Euler(0, 20f + seed * 47f % 140f, 0), Vector3.one);
            }

            public void Marker(float x, float z)
            {
                Props.Surface = ComicSurface.PaintedMetal;
                Props.Cylinder(V(x, Y(z) + .9f, z), .12f, 3f, C(0x2F8F4A), 6);
                Props.Box(V(x, Y(z) + 2.6f, z), V(.5f, .5f, .5f), C(0x2F8F4A));
            }

            public void Lamp(float x, float z, bool white = false)
            {
                ComicMesh lamp = white ? RiversideLamp(x < 0 ? 1 : -1) : CampusLamp();
                Props.Add(lamp, V(x, Top(z), z), Quaternion.identity, Vector3.one);
                if (Mathf.Abs(x) < 8.05f) Blocks.Add(new Block { Id = "Lamp", Position = V(x, Top(z), z), Size = V(.4f, 4.8f, .4f), Jump = false });
            }

            public void Bench(float x, float z, Color? color = null)
            {
                float yaw = x < 0 ? 90f : -90f;
                Props.Add(BenchMesh(color ?? BenchRed), V(x, Top(z), z), Quaternion.Euler(0, yaw, 0), Vector3.one);
                Blocks.Add(new Block { Id = "Bench", Position = V(x, Top(z), z), Size = V(2f, 1f, .8f), Yaw = yaw, Jump = true });
            }

            public void StoneBench(float x, float z) => Props.Add(StoneBenchMesh(), V(x, Top(z), z), Quaternion.Euler(0, x < 0 ? 90f : -90f, 0), Vector3.one);

            public void BikeRack(float x, float z)
            {
                float yaw = x < 0 ? 90f : -90f;
                Props.Add(BikeRackMesh(), V(x, Top(z), z), Quaternion.Euler(0, yaw, 0), Vector3.one);
                Blocks.Add(new Block { Id = "BikeRack", Position = V(x, Top(z), z), Size = V(1.3f, .95f, 1.3f), Yaw = yaw, Jump = true });
            }

            // Generator people must not stand on a ramp or above a gap: plain colliders (no Obstacle) on the
            // sidewalks there are skipped by the rider but block TrackGenerator.PlaceStreetLife.
            public void NoStreetLife(float x0, float x1, float z0, float z1)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    float a = Mathf.Max(x0, s < 0 ? -8f : 4f), b = Mathf.Min(x1, s < 0 ? -4f : 8f);
                    if (b - a < .5f) continue;
                    float zc = (z0 + z1) * .5f;
                    Blocks.Add(new Block { Id = "NoStreetLife", Position = V((a + b) * .5f, Top(zc) - .2f, zc), Size = V(b - a, 2f, z1 - z0), Obstacle = false });
                }
            }

            // A row of sandstone campus blocks along z, each segment on its own uphill ground with a plinth below.
            public void CampusRow(int side, float line, float z0, float z1, float segment, float depth, float height, int floors, bool arcade)
            {
                int n = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / segment));
                float len = (z1 - z0) / n;
                for (int i = 0; i < n; i++)
                {
                    float a = z0 + i * len, zc = a + len * .5f, x = side * (line + depth * .5f);
                    ComicMesh block = CampusBlock(len - .3f, depth, height, floors, arcade, i + (int)line);
                    float y = Top(a);
                    Front.Add(block, V(x, y, zc), Quaternion.Euler(0, side < 0 ? 90 : -90, 0), Vector3.one);
                    Front.Surface = ComicSurface.Plaster;
                    float drop = Top(a) - Top(a + len) + .8f;
                    Front.Box(V(x, y - drop * .5f, zc), V(depth + .1f, drop, len - .3f), Plinth);
                }
            }

            // A big building facing the road (its local +Z front at frontLocal) with the front `line` metres from the
            // road centre, centred at zc along the road, on the ground there; a plinth below fills the downhill side.
            public void Building(ComicMesh mesh, int side, float line, float frontLocal, float depth, float zc, float zFrom, float zTo)
            {
                float y = Top(zc);
                Front.Add(mesh, V(side * (line + frontLocal), y, zc), Quaternion.Euler(0, side < 0 ? 90 : -90, 0), Vector3.one);
                float drop = y - Top(zTo) + .8f;
                Front.Surface = ComicSurface.Plaster;
                Front.Box(V(side * (line + depth * .5f), y - drop * .5f, (zFrom + zTo) * .5f), V(depth, drop, zTo - zFrom), Plinth);
            }

            public void Far(int side, int seed)
            {
                var rng = new System.Random(seed * 991 + (side + 1));
                for (int i = 0; i < 3; i++)
                {
                    float z = 6f + i * 13f + (float)rng.NextDouble() * 4f, x = side * (42f + (float)rng.NextDouble() * 14f);
                    if ((i + seed) % 2 == 0)
                    {
                        float w = 10f + (float)rng.NextDouble() * 4f, h = 7f + (float)rng.NextDouble() * 5f;
                        Back.Surface = ComicSurface.Plaster;
                        Back.Box(V(x, Top(z) + h * .5f - 2f, z), V(10f, h + 4f, w), Color.Lerp(Lime, _r.cream, .25f));
                        // v0.6: arched windows towards the road, a string course, tile courses on the roof
                        Vector3 n = V(-side, 0, 0);
                        for (float wz = z - w * .5f + 1.5f; wz < z + w * .5f - 1f; wz += 2.9f)
                            Arch(Back, V(x - side * 5.04f, Top(z) + h * .42f - 2f, wz), Vector3.forward, n, 1.2f, 2.6f, WindowDark);
                        Back.Surface = ComicSurface.Plaster;
                        Back.Box(V(x, Top(z) + h * .36f - 2f, z), V(10.2f, .25f, w + .2f), LimeTrim);
                        TileRoof(Back, V(x, Top(z) + h - 2f, z), 10.8f, w + .8f, 2.6f);
                    }
                    else
                    {
                        Back.Surface = ComicSurface.Foliage;
                        Color leaf = i % 2 == 0 ? Cypress : LawnDark;
                        Back.Ellipsoid(V(x, Top(z) + 5f, z), V(13f, 11f, 12f), leaf);
                        Back.Ellipsoid(V(x - side * 2.5f, Top(z) + 7.6f, z - 1f), V(8f, 6.5f, 7.5f), Color.Lerp(leaf, Grass, .5f));
                    }
                }
            }
        }

        // ---- road: the street's cross-section (ComicCityBuilder / BoulevardBuilder), holes for gaps

        static ComicMesh Road(Chunk c)
        {
            float L = c.L, h = Kerb;
            var m = new ComicMesh();
            Color joint = Color.Lerp(_r.ink, _r.pavement, .5f);
            const float step = 2f;
            int n = Mathf.RoundToInt(L / step);
            for (int i = 0; i < n; i++)
            {
                float z0 = i * step, z1 = z0 + step;
                for (int lane = -1; lane <= 1; lane += 2)
                {
                    float xa = lane < 0 ? -Half : 0f, xb = lane < 0 ? 0f : Half;
                    if (c.IsHole(xa, xb, z0, z1)) continue;
                    m.Surface = ComicSurface.Asphalt;
                    Face(m, V(xa, 0, z0), V(xa, 0, z1), V(xb, 0, z1), V(xb, 0, z0), _r.asphalt, Vector3.up);
                    float edge = lane * (Half - .15f);
                    Flat(m, edge - .04f, edge + .04f, z0, z1, .0305f, _r.cream);
                }
                for (int s = -1; s <= 1; s += 2)
                {
                    float i0 = s * Half, o0 = s * (Half + Walk);
                    if (c.IsHole(Mathf.Min(i0, o0), Mathf.Max(i0, o0), z0, z1)) continue;
                    float zm = (z0 + z1) * .5f;
                    bool red = c.RedAt != null ? c.RedAt(s, zm) : s > 0 && c.RedRight;
                    Color walk = c.WalkAt != null ? c.WalkAt(s, zm) : s < 0 ? c.WalkLeft : c.WalkRight;
                    bool border = c.BorderAt != null ? c.BorderAt(s, zm) : c.BorderColor.a > 0f;
                    Vector3 inward = V(-s, 0, 0), outward = V(s, 0, 0);
                    m.Surface = red ? ComicSurface.Asphalt : ComicSurface.Paving;
                    Face(m, V(i0, h, z0), V(i0, h, z1), V(o0, h, z1), V(o0, h, z0), walk, Vector3.up);
                    m.Surface = ComicSurface.Paving;
                    Face(m, V(i0, -.05f, z0), V(i0, -.05f, z1), V(i0, h, z1), V(i0, h, z0), _r.pavement, inward);
                    Face(m, V(o0, -.6f, z0), V(o0, -.6f, z1), V(o0, h, z1), V(o0, h, z0), walk, outward);
                    m.Surface = ComicSurface.Plain;
                    float cap = s * (Half + .24f), top = h + .04f;
                    Face(m, V(i0, top, z0), V(i0, top, z1), V(cap, top, z1), V(cap, top, z0), _r.cream, Vector3.up);
                    Face(m, V(i0, h, z0), V(i0, h, z1), V(i0, top, z1), V(i0, top, z0), _r.cream, inward);
                    Face(m, V(cap, h, z0), V(cap, h, z1), V(cap, top, z1), V(cap, top, z0), _r.cream, outward);
                    if (red)
                    {
                        // the shared path's white dashed centre line
                        if (i % 2 == 0) Flat(m, s * (Half + 2f) - .06f, s * (Half + 2f) + .06f, z0 + .4f, z0 + 2f, h + .02f, White);
                    }
                    else
                    {
                        for (int k = 1; k < 4; k++) Flat(m, s * (Half + k) - .0125f, s * (Half + k) + .0125f, z0, z1, h + .025f, joint);
                        Flat(m, Mathf.Min(i0, o0) + (s < 0 ? 0f : .24f), Mathf.Max(i0, o0) - (s < 0 ? .24f : 0f), z0 - .0125f, z0 + .0125f, h + .025f, joint);
                    }
                    if (border) Flat(m, s < 0 ? o0 : o0 - .45f, s < 0 ? o0 + .45f : o0, z0, z1, h + .015f, c.BorderColor.a > 0f ? c.BorderColor : Brick);
                }
            }
            foreach (Action<ComicMesh> paint in c.Paint) paint(m);
            // Dashed double yellow centre line (same rhythm as the street chunks).
            for (int z = 0; z < 40; z += 5)
                for (int s = -1; s <= 1; s += 2)
                    if (!c.IsHole(-.3f, .3f, z, z + 4f) && !c.IsHole(-.3f, .3f, z + 2f, z + 4f) && !HoleTouches(c, z + .5f, z + 3.5f))
                        Flat(m, s * .12f - .05f, s * .12f + .05f, z + .5f, z + 3.5f, .035f, _r.yellow);
            // Closed slab ends at the chunk ends and around holes (the cut shows as a stone edge).
            foreach (float ze in new[] { 0f, c.L })
            {
                Vector3 normal = ze <= 0f ? Vector3.back : Vector3.forward;
                m.Surface = ComicSurface.Asphalt;
                Face(m, V(-Half, -.8f, ze), V(Half, -.8f, ze), V(Half, 0, ze), V(-Half, 0, ze), _r.asphalt, normal);
                m.Surface = ComicSurface.Paving;
                for (int s = -1; s <= 1; s += 2)
                    Face(m, V(s * Half, -.6f, ze), V(s * (Half + Walk), -.6f, ze), V(s * (Half + Walk), h, ze), V(s * Half, h, ze), _r.pavement, normal);
            }
            foreach (Hole hole in c.Holes)
            {
                Color edge = Color.Lerp(_r.pavement, _r.ink, .25f);
                m.Surface = ComicSurface.Concrete;
                foreach (float ze in new[] { hole.Z0, hole.Z1 })
                {
                    Vector3 normal = ze == hole.Z0 ? Vector3.forward : Vector3.back;
                    float a = Mathf.Max(hole.X0, -Half - Walk), b = Mathf.Min(hole.X1, Half + Walk);
                    for (float x = a; x < b - .01f; x += 4f)
                    {
                        float e = Mathf.Min(b, x + 4f), top = Mathf.Abs(x + 2f) > Half ? h : 0f;
                        Face(m, V(x, -1.4f, ze), V(e, -1.4f, ze), V(e, top, ze), V(x, top, ze), edge, normal);
                    }
                }
                foreach (float xe in new[] { hole.X0, hole.X1 })
                {
                    if (Mathf.Abs(xe) >= Half + Walk - .01f) continue;
                    Vector3 normal = xe == hole.X0 ? Vector3.right : Vector3.left;
                    float top = Mathf.Abs(xe) > Half + .01f ? h : 0f;
                    Face(m, V(xe, -1.4f, hole.Z0), V(xe, -1.4f, hole.Z1), V(xe, top, hole.Z1), V(xe, top, hole.Z0), edge, normal);
                }
            }
            m.Shear(c.G);
            m.ClearCreaseInk();
            return m;
        }

        static bool HoleTouches(Chunk c, float z0, float z1) => c.Holes.Any(h => h.X0 < .2f && h.X1 > -.2f && z1 > h.Z0 && z0 < h.Z1);

        // ---- ramps (local: pivot at the start of the run-up on the road, +Z forward; sheared with the slope)

        static ComicMesh WedgeShell(float w, float rise, float h, float top, float lipDown, Color topColor, Color side, ComicSurface surface)
        {
            var m = new ComicMesh { Surface = surface };
            float hw = w * .5f, L = rise + top;
            Face(m, V(-hw, 0, 0), V(hw, 0, 0), V(hw, h, rise), V(-hw, h, rise), topColor, V(0, 1, -h / rise));
            if (top > 0f) Face(m, V(-hw, h, rise), V(hw, h, rise), V(hw, h, L), V(-hw, h, L), topColor, Vector3.up);
            Face(m, V(-hw, -lipDown, L), V(hw, -lipDown, L), V(hw, h, L), V(-hw, h, L), side, Vector3.forward);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 n = V(s, 0, 0);
                Face(m, V(s * hw, 0, 0), V(s * hw, h, rise), V(s * hw, h, L), V(s * hw, 0, L), side, n);
                if (lipDown > 0f) Face(m, V(s * hw, -lipDown, rise), V(s * hw, 0, rise), V(s * hw, 0, L), V(s * hw, -lipDown, L), side, n);
            }
            return m;
        }

        static ComicMesh StepsRampMesh(float w, float rise, float h, float top, float grade, float lipDown = 0f)
        {
            ComicMesh m = WedgeShell(w, rise, h, top, lipDown, LimeTrim, Lime, ComicSurface.Plaster);
            float hw = w * .5f;
            // stair nosings: dark riser shadows across the slope
            int steps = Mathf.RoundToInt(rise / .55f);
            for (int i = 1; i <= steps; i++)
            {
                float z = rise * i / (steps + .5f), y = h * z / rise + .015f;
                Face(m, V(-hw + .1f, y, z), V(hw - .1f, y, z), V(hw - .1f, y + h * .12f / rise, z + .12f), V(-hw + .1f, y + h * .12f / rise, z + .12f), LimeShade, Vector3.up, false);
            }
            // balustrade posts and a coping on each side
            for (int s = -1; s <= 1; s += 2)
            {
                m.Surface = ComicSurface.Plaster;
                m.Beam(V(s * (hw + .12f), .35f, 0), V(s * (hw + .12f), h + .45f, rise), .24f, LimeShade);
                if (top > 0f) m.Box(V(s * (hw + .12f), h + .45f, rise + top * .5f), V(.24f, .24f, top), LimeShade);
                m.Box(V(s * (hw + .12f), (h + .5f) * .5f, rise + top), V(.4f, h + .5f, .4f), Lime);
            }
            m.Shear(grade);
            return m;
        }

        static ComicMesh JettyRampMesh(float w, float rise, float h, float top, float grade, float lipDown)
        {
            ComicMesh m = WedgeShell(w, rise, h, top, lipDown, Timber, TimberDark, ComicSurface.Wood);
            float hw = w * .5f, L = rise + top;
            Color line = Color.Lerp(Timber, _r.ink, .35f);
            for (float z = .4f; z < L - .1f; z += .45f)
            {
                float y = (z < rise ? h * z / rise : h) + .015f;
                Face(m, V(-hw + .05f, y, z), V(hw - .05f, y, z), V(hw - .05f, y, z + .05f), V(-hw + .05f, y, z + .05f), line, Vector3.up, false);
            }
            m.Surface = ComicSurface.Wood;
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(V(s * (hw + .08f), (h + .5f - lipDown) * .5f, L - .2f), V(.25f, h + .5f + lipDown, .25f), TimberDark);
                m.Beam(V(s * (hw + .1f), .25f, .2f), V(s * (hw + .1f), h + .25f, rise), .18f, TimberDark);
                if (top > 0f) m.Box(V(s * (hw + .1f), h + .25f, rise + top * .5f), V(.18f, .18f, top), TimberDark);
            }
            for (float x = -hw + 1f; x < hw; x += 2.5f) m.Box(V(x, -lipDown * .5f + h * .5f - .2f, L - .15f), V(.2f, h + lipDown, .2f), TimberDark);
            m.Shear(grade);
            return m;
        }

        // =====================================================================================================
        // Saving
        // =====================================================================================================

        static void Begin(string tag)
        {
            _tag = tag;
            PrefabBuildUtility.EnsureFolder(PrefabFolder(tag));
            PrefabBuildUtility.EnsureFolder(MeshFolder(tag));
        }

        static RoadChunk SaveChunk(Chunk c)
        {
            string id = "CHN_" + _tag + "_" + c.Id, path = ChunkPath(_tag, c.Id);
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            var chunk = PrefabBuildUtility.GetOrAdd<RoadChunk>(root);
            chunk.length = c.L; chunk.endHeight = -c.L * c.G; chunk.roadType = RoadType.Street; chunk.weight = 1f; chunk.minDifficulty = 0;
            chunk.halfWidth = Half; chunk.sidewalkWidth = Walk; chunk.sidewalkHeight = Kerb; chunk.curbTransitionWidth = .3f;
            chunk.endHalfWidth = 0f; chunk.widthChangeZ = new Vector2(8f, 32f);

            foreach (RampSpec r in c.Ramps) c.Props.Add(r.Mesh, V(r.X, c.Y(r.Z), r.Z), Quaternion.identity, Vector3.one);
            Renderer(sync.Plain("RoadAndSidewalk"), id + "_Road", Road(c), false);
            Renderer(sync.Plain("Frontage"), id + "_Frontage", c.Front, true);
            Renderer(sync.Plain("Props"), id + "_Props", c.Props, true);
            Renderer(sync.Plain("Grounds"), id + "_Grounds", c.Back, false);
            PrefabBuildUtility.SyncComponent<StreetDressing>(root, false);

            var blocksGo = sync.Plain("SidewalkObstacles");
            var blocks = new ChildSync(blocksGo.transform);
            for (int i = 0; i < c.Blocks.Count; i++)
            {
                Block b = c.Blocks[i];
                GameObject go = blocks.Plain(b.Id + "_" + i);
                go.transform.localPosition = b.Position; go.transform.localRotation = Quaternion.Euler(0, b.Yaw, 0);
                var box = PrefabBuildUtility.GetOrAdd<BoxCollider>(go);
                box.center = V(0, b.Size.y * .5f, 0); box.size = b.Size; box.isTrigger = false;
                PrefabBuildUtility.SyncComponent<Obstacle>(go, b.Obstacle, o => { o.kind = ObstacleKind.Thing; o.canJumpOver = b.Jump; o.punchable = false; o.spansRoad = false; });
            }
            blocks.Finish();

            var rampsGo = sync.Plain("Ramps");
            var ramps = new ChildSync(rampsGo.transform);
            for (int i = 0; i < c.Ramps.Count; i++)
            {
                RampSpec r = c.Ramps[i];
                GameObject go = ramps.Plain("Ramp_" + i);
                go.transform.localPosition = V(r.X, c.Y(r.Z), r.Z); go.transform.localRotation = Quaternion.identity;
                var ramp = PrefabBuildUtility.GetOrAdd<Ramp>(go);
                ramp.width = r.Width; ramp.rise = r.Rise; ramp.height = r.Height; ramp.top = r.Top; ramp.launch = r.Launch; ramp.hangGravity = r.Hang; ramp.lanes = Array.Empty<int>();
            }
            for (int i = 0; i < c.Gaps.Count; i++)
            {
                GapSpec gp = c.Gaps[i];
                GameObject go = ramps.Plain("Gap_" + i);
                go.transform.localPosition = V(gp.X, c.Y(gp.Z), gp.Z); go.transform.localRotation = Quaternion.identity;
                var gap = PrefabBuildUtility.GetOrAdd<RoadGap>(go);
                gap.length = gp.Length; gap.width = gp.Width; gap.word = gp.Word; gap.water = gp.Water;
            }
            ramps.Finish();

            var slotsGo = sync.Plain("SpawnSlots");
            var slots = new ChildSync(slotsGo.transform);
            foreach (Vector2 s in c.SlotList)
            {
                GameObject slot = slots.Plain("Row" + s.y.ToString(CultureInfo.InvariantCulture) + "_Lane" + s.x.ToString(CultureInfo.InvariantCulture));
                slot.transform.localPosition = V(s.x, c.Y(s.y), s.y);
                PrefabBuildUtility.GetOrAdd<SpawnSlot>(slot).kind = SpawnKind.Any;
            }
            slots.Finish();
            sync.Finish();

            int tris = PrefabBuildUtility.CountTriangles(root), renderers = root.GetComponentsInChildren<MeshRenderer>(true).Length;
            PrefabBuildUtility.SaveAndClose(root, path);
            Report.Add(id + " " + tris + " tris, " + renderers + " renderers");
            Debug.Log("[Crawley] " + id + " " + tris + " tris, " + renderers + " renderers");
            if (tris > TriangleBudget) throw new InvalidOperationException(id + " exceeds the 25k triangle chunk budget: " + tris);
            if (renderers > RendererBudget) throw new InvalidOperationException(id + " has more than 8 renderers: " + renderers);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<RoadChunk>();
        }

        static Ramp SaveRamp(string name, ComicMesh mesh, float width, float rise, float height, float top, float launch, float hang)
        {
            GameObject prefab = SaveArt(PrefabPath(_tag, name), name, mesh, true, root =>
            {
                var r = PrefabBuildUtility.GetOrAdd<Ramp>(root);
                r.width = width; r.rise = rise; r.height = height; r.top = top; r.launch = launch; r.hangGravity = hang; r.lanes = new[] { -1, 0, 1 };
            });
            return prefab.GetComponent<Ramp>();
        }

        static Obstacle SaveObstacle(string name, ComicMesh mesh, Vector3 centre, Vector3 size, ObstacleKind kind, bool jump, bool punchable)
        {
            GameObject prefab = SaveArt(PrefabPath(_tag, name), name, mesh, true, root =>
            {
                var box = PrefabBuildUtility.GetOrAdd<BoxCollider>(root);
                box.center = centre; box.size = size; box.isTrigger = false;
                var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
                o.kind = kind; o.canJumpOver = jump; o.punchable = punchable; o.spansRoad = false; o.animal = false; o.police = false; o.spawnWeight = 1f;
            });
            return prefab.GetComponent<Obstacle>();
        }

        // Animals: kind Person + animal (a punch only shoos them, cartoon mode never throws them), never punchable.
        static GameObject SaveAnimal(string name, ComicMesh mesh, Vector3 centre, Vector3 size, bool jump, float walk)
        {
            return SaveArt(PrefabPath(_tag, name), name, mesh, true, root =>
            {
                var box = PrefabBuildUtility.GetOrAdd<BoxCollider>(root);
                box.center = centre; box.size = size; box.isTrigger = false;
                var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
                o.kind = ObstacleKind.Person; o.animal = true; o.punchable = false; o.canJumpOver = jump; o.spansRoad = false; o.police = false; o.spawnWeight = 1f;
                var p = PrefabBuildUtility.GetOrAdd<Pedestrian>(root);
                p.walkSpeed = walk; p.crosses = false; p.art = root.transform.Find("Art");
                foreach (MeshRenderer mr in root.GetComponentsInChildren<MeshRenderer>(true)) mr.shadowCastingMode = ShadowCastingMode.Off;
            }, false);
        }

        static GameObject SaveFar(string name, ComicMesh mesh) =>
            SaveArt(PrefabPath(_tag, name), name, mesh, false, root => { }, false, true);

        static GameObject SaveArt(string path, string meshId, ComicMesh mesh, bool outline, Action<GameObject> configure, bool shadows = true, bool far = false)
        {
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            GameObject art = sync.Plain("Art");
            Renderer(art, meshId, mesh, outline, shadows && outline, far);
            sync.Finish();
            configure(root);
            PrefabBuildUtility.SaveAndClose(root, path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static GameObject Person(string look) => AssetDatabase.LoadAssetAtPath<GameObject>(ComicCityBuilder.Prefabs + "/Hazards/HZ_Ped_" + look + ".prefab");

        static District LoadOrCreateDistrict(string path)
        {
            var d = AssetDatabase.LoadAssetAtPath<District>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<District>(); AssetDatabase.CreateAsset(d, path); }
            return d;
        }

        static Mesh SaveMesh(string id, ComicMesh data, bool far = false)
        {
            string path = MeshFolder(_tag) + "/" + id + ".asset";
            Mesh mesh = data.Bake(id), existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            // Unity culls on the CPU, before the vertex shader curls the far city up (ComicCityBuilder.SaveMesh).
            Bounds b = mesh.bounds;
            b.Encapsulate(b.max + Vector3.up * (ComicCityProjection.Rise(_r, far ? 600 : 360) + 1));
            if (far) b.Encapsulate(new Bounds(Vector3.zero, Vector3.one * 1200f));
            mesh.bounds = b;
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); existing = mesh; }
            else
            {
                var writable = new SerializedObject(existing);
                writable.FindProperty("m_IsReadable").boolValue = true;
                writable.ApplyModifiedPropertiesWithoutUndo();
                existing.Clear(); existing.indexFormat = mesh.indexFormat;
                existing.SetVertices(mesh.vertices); existing.SetNormals(mesh.normals); existing.SetColors(mesh.colors);
                var uv = new List<Vector3>(); mesh.GetUVs(0, uv); existing.SetUVs(0, uv);
                mesh.GetUVs(1, uv); existing.SetUVs(1, uv);
                var surfaceUV = new List<Vector4>(); mesh.GetUVs(2, surfaceUV); existing.SetUVs(2, surfaceUV);
                existing.SetTriangles(mesh.triangles, 0); existing.bounds = mesh.bounds;
                existing.UploadMeshData(false);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
            }
            var so = new SerializedObject(existing);
            SerializedProperty readable = so.FindProperty("m_IsReadable");
            if (readable != null) { readable.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
            return existing;
        }

        static void Renderer(GameObject go, string id, ComicMesh data, bool outline, bool shadows = true, bool far = false)
        {
            Mesh mesh = SaveMesh(id, data, far);
            PrefabBuildUtility.GetOrAdd<MeshFilter>(go).sharedMesh = mesh;
            var mr = PrefabBuildUtility.GetOrAdd<MeshRenderer>(go);
            mr.sharedMaterial = _material;
            mr.shadowCastingMode = outline && shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = !far;
            var children = new ChildSync(go.transform);
            if (outline)
            {
                GameObject ink = children.Plain("InkOutline");
                PrefabBuildUtility.GetOrAdd<MeshFilter>(ink).sharedMesh = mesh;
                var imr = PrefabBuildUtility.GetOrAdd<MeshRenderer>(ink);
                imr.sharedMaterial = _ink; imr.shadowCastingMode = ShadowCastingMode.Off; imr.receiveShadows = false;
            }
            children.Finish();
        }

        // =====================================================================================================
        // Geometry helpers
        // =====================================================================================================

        static Color C(int hex) => new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f);
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // A quad facing `normal` whatever order the corners come in (BoulevardBuilder.Face).
        static void Face(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector3 normal, bool ink = true)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) m.Quad(d, c, b, a, color, ink);
            else m.Quad(a, b, c, d, color, ink);
        }

        static void Tri(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, Color color, Vector3 normal)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) m.Tri(a, c, b, color);
            else m.Tri(a, b, c, color);
        }

        static void Box(ComicMesh m, ComicSurface s, Vector3 centre, Vector3 size, Color color)
        {
            m.Surface = s;
            m.Box(centre, size, color);
        }

        static void Flat(ComicMesh m, float x0, float x1, float z0, float z1, float y, Color color)
        {
            m.Surface = ComicSurface.Plain;
            Face(m, V(x0, y, z0), V(x0, y, z1), V(x1, y, z1), V(x1, y, z0), color, Vector3.up);
        }

        static void FlatQuad(ComicMesh m, float x0, float x1, float z0, float z1, float y, Color color)
        {
            m.Surface = ComicSurface.Plain;
            Face(m, V(x0, y, z0), V(x0, y, z1), V(x1, y, z1), V(x1, y, z0), color, Vector3.up, false);
        }

        // A flat rectangle centred at c in the plane spanned by right/up, facing n (signs, windows, clock hands).
        static void Panel(ComicMesh m, Vector3 c, Vector3 right, Vector3 up, Vector3 n, float w, float h, Color color)
        {
            m.Surface = ComicSurface.Plain;
            Vector3 r = right * (w * .5f), u = up * (h * .5f);
            Face(m, c - r - u, c + r - u, c + r + u, c - r + u, color, n, false);
        }

        static void Slab(ComicMesh m, Vector3 c, Vector3 right, Vector3 n, float w, float h, Color color) => Panel(m, c, right, Vector3.up, n, w, h, color);

        // Flat polygon (petal carpets, clock faces, lily pads), no crease ink.
        static void Disc(ComicMesh m, Vector3 c, Vector3 n, float r, Color color, int sides)
        {
            m.Surface = ComicSurface.Plain;
            Vector3 a = Vector3.Cross(n, Mathf.Abs(n.y) > .9f ? Vector3.forward : Vector3.up).normalized, b = Vector3.Cross(n, a);
            for (int i = 0; i < sides; i++)
            {
                float t0 = Mathf.PI * 2f * i / sides, t1 = Mathf.PI * 2f * (i + 1) / sides;
                Tri(m, c, c + (a * Mathf.Cos(t0) + b * Mathf.Sin(t0)) * r, c + (a * Mathf.Cos(t1) + b * Mathf.Sin(t1)) * r, color, n);
            }
        }

        // Round arch opening (rectangle + half disc), flat on a wall: bottom centre p, `right` along the wall, facing n.
        static void Arch(ComicMesh m, Vector3 p, Vector3 right, Vector3 n, float w, float h, Color color)
        {
            m.Surface = ComicSurface.Plain;
            float r = w * .5f, spring = Mathf.Max(0f, h - r);
            Vector3 up = Vector3.up;
            if (spring > 0f) Face(m, p - right * r, p + right * r, p + right * r + up * spring, p - right * r + up * spring, color, n, false);
            Vector3 c = p + up * spring;
            const int seg = 6;
            for (int i = 0; i < seg; i++)
            {
                float a0 = Mathf.PI * i / seg, a1 = Mathf.PI * (i + 1) / seg;
                Tri(m, c, c + right * (Mathf.Cos(a0) * r) + up * (Mathf.Sin(a0) * r), c + right * (Mathf.Cos(a1) * r) + up * (Mathf.Sin(a1) * r), color, n);
            }
        }

        // A slab with its top at `top`, split into strips of at most `step` metres along z (the GPU curl bends them);
        // ink only along its outer edges.
        static void StripSlab(ComicMesh m, float x0, float x1, float z0, float z1, float top, Color color, float step, bool ink = true)
        {
            if (x0 > x1) (x0, x1) = (x1, x0);
            int n = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / step - .001f));
            float len = (z1 - z0) / n;
            for (int i = 0; i < n; i++)
            {
                float a = z0 + i * len, b = a + len, v0 = (float)i / n, v1 = (float)(i + 1) / n;
                if (ink)
                {
                    m.RibbonQuad(V(x0, top, a), V(x0, top, b), V(x1, top, b), V(x1, top, a), color, v0, v1,
                        Vector3.Dot(Vector3.Cross(V(0, 0, len), V(x1 - x0, 0, len)), Vector3.up) < 0f);
                }
                else Face(m, V(x0, top, a), V(x0, top, b), V(x1, top, b), V(x1, top, a), color, Vector3.up, false);
                Face(m, V(x0, top - 1.6f, a), V(x0, top - 1.6f, b), V(x0, top, b), V(x0, top, a), Color.Lerp(color, _r.ink, .2f), Vector3.left, false);
                Face(m, V(x1, top - 1.6f, a), V(x1, top - 1.6f, b), V(x1, top, b), V(x1, top, a), Color.Lerp(color, _r.ink, .2f), Vector3.right, false);
            }
            Face(m, V(x0, top - 1.6f, z0), V(x1, top - 1.6f, z0), V(x1, top, z0), V(x0, top, z0), Color.Lerp(color, _r.ink, .2f), Vector3.back, false);
            Face(m, V(x0, top - 1.6f, z1), V(x1, top - 1.6f, z1), V(x1, top, z1), V(x0, top, z1), Color.Lerp(color, _r.ink, .2f), Vector3.forward, false);
        }

        // Box along z between bottom and top, in spans of at most 4 m (walls, hedges, rims).
        static void StripBox(ComicMesh m, float x0, float x1, float z0, float z1, float bottom, float top, Color color)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / 4f - .001f));
            float len = (z1 - z0) / n;
            for (int i = 0; i < n; i++)
                m.Box(V((x0 + x1) * .5f, (bottom + top) * .5f, z0 + (i + .5f) * len), V(x1 - x0, top - bottom, len), color);
        }

        // Hip roof over an eave rectangle centred at c (eave height c.y): ridge along the longer side.
        static void HipRoof(ComicMesh m, Vector3 c, float w, float d, float rise, Color color, Color ridge)
        {
            m.Surface = ComicSurface.Brick;
            float hw = w * .5f, hd = d * .5f;
            Vector3 A = c + V(-hw, 0, hd), B = c + V(hw, 0, hd), Cc = c + V(hw, 0, -hd), D = c + V(-hw, 0, -hd);
            if (w >= d)
            {
                float rx = hw - hd;
                Vector3 R0 = c + V(-rx, rise, 0), R1 = c + V(rx, rise, 0);
                Face(m, A, B, R1, R0, color, V(0, 1, 1));
                Face(m, Cc, D, R0, R1, color, V(0, 1, -1));
                Tri(m, B, Cc, R1, Color.Lerp(color, ridge, .3f), V(1, 1, 0));
                Tri(m, D, A, R0, Color.Lerp(color, ridge, .3f), V(-1, 1, 0));
                if (rx > .05f) m.Beam(R0, R1, .18f, ridge);
            }
            else
            {
                float rz = hd - hw;
                Vector3 R0 = c + V(0, rise, -rz), R1 = c + V(0, rise, rz);
                Face(m, B, Cc, R0, R1, color, V(1, 1, 0));
                Face(m, D, A, R1, R0, color, V(-1, 1, 0));
                Tri(m, A, B, R1, Color.Lerp(color, ridge, .3f), V(0, 1, 1));
                Tri(m, Cc, D, R0, Color.Lerp(color, ridge, .3f), V(0, 1, -1));
                if (rz > .05f) m.Beam(R0, R1, .18f, ridge);
            }
            m.Surface = ComicSurface.Plain;
            Face(m, A, B, Cc, D, Color.Lerp(color, _r.ink, .45f), Vector3.down, false);
        }

        // Gable roof: ridge along x (alongX) or z; the gable triangles take the wall colour.
        static void GableRoof(ComicMesh m, Vector3 c, float w, float d, float rise, Color color, Color gable, bool alongX)
        {
            float hw = w * .5f, hd = d * .5f;
            m.Surface = ComicSurface.Metal;
            if (alongX)
            {
                Vector3 R0 = c + V(-hw, rise, 0), R1 = c + V(hw, rise, 0);
                Face(m, c + V(-hw, 0, hd), c + V(hw, 0, hd), R1, R0, color, V(0, 1, 1));
                Face(m, c + V(hw, 0, -hd), c + V(-hw, 0, -hd), R0, R1, color, V(0, 1, -1));
                m.Surface = ComicSurface.Wood;
                Tri(m, c + V(-hw + .25f, 0, hd - .25f), c + V(-hw + .25f, 0, -hd + .25f), R0 + V(.25f, -.15f, 0), gable, Vector3.left);
                Tri(m, c + V(hw - .25f, 0, -hd + .25f), c + V(hw - .25f, 0, hd - .25f), R1 + V(-.25f, -.15f, 0), gable, Vector3.right);
            }
            else
            {
                Vector3 R0 = c + V(0, rise, -hd), R1 = c + V(0, rise, hd);
                Face(m, c + V(hw, 0, -hd), c + V(hw, 0, hd), R1, R0, color, V(1, 1, 0));
                Face(m, c + V(-hw, 0, hd), c + V(-hw, 0, -hd), R0, R1, color, V(-1, 1, 0));
                m.Surface = ComicSurface.Wood;
                Tri(m, c + V(-hw + .25f, 0, hd - .25f), c + V(hw - .25f, 0, hd - .25f), R1 + V(0, -.15f, -.25f), gable, Vector3.forward);
                Tri(m, c + V(hw - .25f, 0, -hd + .25f), c + V(-hw + .25f, 0, -hd + .25f), R0 + V(0, -.15f, .25f), gable, Vector3.back);
                // white bargeboards along the front gable
                m.Beam(c + V(-hw, -.05f, hd + .02f), R1 + V(0, .02f, .02f), .16f, White);
                m.Beam(c + V(hw, -.05f, hd + .02f), R1 + V(0, .02f, .02f), .16f, White);
            }
            m.Surface = ComicSurface.Plain;
            Face(m, c + V(-hw, 0, hd), c + V(hw, 0, hd), c + V(hw, 0, -hd), c + V(-hw, 0, -hd), Color.Lerp(color, _r.ink, .45f), Vector3.down, false);
        }
    }
}
