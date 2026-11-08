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

        static ComicCityRecipe _r;
        static Material _material, _ink;
        static string _tag;
        static readonly List<string> Report = new List<string>();

        [MenuItem("CityScooter/Districts/Build UWA + Matilda Bay")]
        public static void Build()
        {
            _r = AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(ComicCityBuilder.RecipePath);
            _material = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicPalette.mat");
            _ink = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicInk.mat");
            if (_r == null || _material == null || _ink == null) throw new InvalidOperationException("Build the comic city first (recipe, ComicPalette.mat, ComicInk.mat).");
            ComicSurfaceAssets.EnsureLoaded();
            PrefabBuildUtility.EnsureFolder(DataFolder);
            Report.Clear();
            BuildUwa();
            BuildMatilda();
            AssetDatabase.SaveAssets();
            Debug.Log("[Crawley] built " + string.Join("; ", Report));
        }

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
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
            UwaBlue = C(0x27407A);

        static void BuildUwa()
        {
            Begin("Uwa");
            float g = _r.downhillGrade;
            var chunks = new List<RoadChunk>();
            RoadChunk entry = SaveChunk(UwaEntry(g, false));
            chunks.Add(SaveChunk(UwaArcades(g)));
            chunks.Add(SaveChunk(UwaGrove(g)));
            chunks.Add(SaveChunk(UwaWinthrop(g)));
            chunks.Add(SaveChunk(UwaPondJump(g)));
            chunks.Add(SaveChunk(UwaCloisters(g)));
            RoadChunk exit = SaveChunk(UwaEntry(g, true));

            Ramp steps = SaveRamp("RMP_Uwa_SandstoneSteps", StepsRampMesh(3f, 5f, 1.3f, 0f, g), 3f, 5f, 1.3f, 0f, 1f, .5f);
            var aFrame = SaveObstacle("OBS_Uwa_AFrameSign", AFrameSign(), new Vector3(0, .5f, 0), new Vector3(.95f, 1f, .7f), ObstacleKind.Thing, true, true);
            var sprinkler = SaveObstacle("OBS_Uwa_Sprinkler", Sprinkler(), new Vector3(0, .35f, 0), new Vector3(.9f, .7f, .9f), ObstacleKind.Thing, true, true);
            GameObject peacock = SaveAnimal("OBS_Uwa_Peacock", PeacockMesh(false), new Vector3(0, .6f, .4f), new Vector3(.55f, 1.2f, 1.7f), true, .5f);
            GameObject peacockFan = SaveAnimal("OBS_Uwa_PeacockFan", PeacockMesh(true), new Vector3(0, .8f, .2f), new Vector3(1.9f, 1.6f, .7f), false, .3f);
            GameObject far = SaveFar("PRP_Uwa_FarCampus", UwaFar());

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
            d.farScenery = far; d.gateway = null; d.ambience = null;
            EditorUtility.SetDirty(d);
        }

        static Chunk UwaBase(string id, float g)
        {
            var c = new Chunk(id, g) { WalkLeft = UwaPaving, WalkRight = UwaPaving, BorderColor = Brick };
            return c;
        }

        // Entry (calm at z = 0: low walls, hedges, lawn and jacarandas; the campus starts towards z = 40).
        // Exit is the same chunk mirrored along z (calm at z = 40).
        static Chunk UwaEntry(float g, bool exit)
        {
            Chunk c = UwaBase(exit ? "Exit" : "Entry", g);
            float Z(float z) => exit ? 40f - z : z; // z measured from the calm end
            for (int s = -1; s <= 1; s += 2)
            {
                c.Lawn(s, 8.4f, 75f, 0f, 40f);
                c.LowWall(s, 0f, 40f, .55f);
                c.HedgeRow(s, 9.6f, Mathf.Min(Z(0), Z(16)), Mathf.Max(Z(0), Z(16)));
                c.Tree(Jacaranda(11 + s), s * 12.5f, Z(7), true);
                c.Tree(Jacaranda(17 + s), s * 13.5f, Z(25), true);
                c.Tree(CypressMesh(1f), s * 17f, Z(2f), false);
                c.Tree(CypressMesh(.9f), s * 17.5f, Z(13f), false);
                c.Lamp(s * 8.7f, Z(12));
                c.Lamp(s * 8.7f, Z(32));
            }
            // The campus begins: a single-storey cloister on the left, a two-storey block on the right.
            c.CampusRow(-1, 16f, Z(40) < Z(22) ? Z(40) : Z(22), Z(40) < Z(22) ? Z(22) : Z(40), 9f, 6f, 4.6f, 1, true);
            c.CampusRow(1, 18f, Z(40) < Z(24) ? Z(40) : Z(24), Z(40) < Z(24) ? Z(24) : Z(40), 8f, 10f, 8.5f, 2, true);
            c.Far(-1, 3); c.Far(1, 5);
            c.Bench(-7.4f, Z(20)); c.BikeRack(7.3f, Z(18));
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
            c.Tree(Jacaranda(33), 10.5f, 21f, true);
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
            for (int i = 0; i < 4; i++) c.Tree(Jacaranda(40 + i), 11.5f, 4f + i * 10.5f, true);
            for (int i = 0; i < 3; i++) c.Tree(Jacaranda(50 + i), 22f, 9f + i * 11f, true);
            c.Tree(Jacaranda(60), -13f, 30f, true);
            c.Tree(Jacaranda(61), -12f, 8f, true);
            float[][] palms = { new[] { -16f, 14f }, new[] { -19.5f, 18f }, new[] { -15.5f, 21.5f }, new[] { -21f, 24f }, new[] { -18f, 27f } };
            for (int i = 0; i < palms.Length; i++) c.Tree(Palm(70 + i, 8f + i % 3), palms[i][0], palms[i][1], false);
            c.Tree(CypressMesh(1.1f), -26f, 6f, false); c.Tree(CypressMesh(1f), -28f, 33f, false);
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
            c.Tree(Jacaranda(80), -11f, 19f, true);
            c.Lamp(-8.7f, 8f); c.Lamp(-8.7f, 28f);
            // Right: lawn, jacarandas, a cypress row and a cloister.
            c.Lawn(1, 8.4f, 75f, 0f, 40f);
            c.LowWall(1, 0f, 40f, .45f);
            c.Tree(Jacaranda(81), 12f, 6f, true); c.Tree(Jacaranda(82), 13f, 21f, true); c.Tree(Jacaranda(83), 12f, 35f, true);
            for (int i = 0; i < 4; i++) c.Tree(CypressMesh(1f), 21f, 4f + i * 10f, false);
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
            c.Far(-1, 10); c.Far(1, 11);
            c.Slots(10f, 25f);
            return c;
        }

        // ---- UWA parts (local: pivot bottom centre, front faces +Z, width along X)

        // Sandstone block: plinth, string courses, terracotta hip roof, ground-floor arcade, arched upper windows.
        static ComicMesh CampusBlock(float w, float d, float h, int floors, bool arcade, int seed)
        {
            var m = new ComicMesh();
            float front = d * .5f, floor = h / Mathf.Max(1, floors);
            Box(m, ComicSurface.Plaster, V(0, h * .5f, 0), V(w, h, d), Lime);
            Box(m, ComicSurface.Plaster, V(0, .3f, 0), V(w + .12f, .6f, d + .12f), Plinth);
            Box(m, ComicSurface.Plaster, V(0, h - .12f, 0), V(w + .3f, .24f, d + .3f), LimeTrim);
            for (int level = 1; level < floors; level++) Box(m, ComicSurface.Plaster, V(0, level * floor, front + .04f), V(w, .16f, .14f), LimeTrim);
            HipRoof(m, V(0, h, 0), w + .9f, d + .9f, Mathf.Min(w, d) * .28f, Terracotta, TerracottaDark);
            int bays = Mathf.Max(2, Mathf.RoundToInt(w / 3.1f));
            float bay = w / bays;
            for (int i = 0; i < bays; i++)
            {
                float x = -w * .5f + (i + .5f) * bay;
                if (arcade)
                {
                    Arch(m, V(x, .6f, front + .03f), Vector3.right, Vector3.forward, bay * .66f, Mathf.Min(floor - .9f, 3.4f), ArchDark);
                    // pier capitals between the arches
                    Box(m, ComicSurface.Plaster, V(-w * .5f + i * bay, floor - .9f - bay * .33f, front + .08f), V(.34f, .14f, .14f), LimeTrim);
                }
                else Arch(m, V(x, .9f, front + .03f), Vector3.right, Vector3.forward, bay * .4f, Mathf.Min(floor - 1.4f, 2.2f), WindowDark);
                for (int level = 1; level < floors; level++)
                {
                    float y = level * floor + .55f;
                    if ((i + seed) % 3 == 1) Arch(m, V(x, y, front + .03f), Vector3.right, Vector3.forward, bay * .5f, floor * .62f, WindowDark);
                    else
                    {
                        Arch(m, V(x - bay * .17f, y, front + .03f), Vector3.right, Vector3.forward, bay * .24f, floor * .55f, WindowDark);
                        Arch(m, V(x + bay * .17f, y, front + .03f), Vector3.right, Vector3.forward, bay * .24f, floor * .55f, WindowDark);
                    }
                }
            }
            // a window on each end wall
            for (int s = -1; s <= 1; s += 2)
                Arch(m, V(s * (w * .5f + .03f), floors > 1 ? floor + .6f : 1f, 0), V(0, 0, -s), V(s, 0, 0), 1f, floor * .55f, WindowDark);
            return m;
        }

        // Winthrop Hall: the great hall with tall arched windows, the two-storey arcade (loggia) in front and the
        // clock tower at the -X end (which faces the oncoming rider once the hall is turned to face the road).
        static ComicMesh WinthropHall()
        {
            var m = new ComicMesh();
            const float W = 34f, D = 15f, H = 15f, hallZ = -3f;
            float front = hallZ + D * .5f;
            Box(m, ComicSurface.Plaster, V(0, H * .5f, hallZ), V(W, H, D), Lime);
            Box(m, ComicSurface.Plaster, V(0, .4f, hallZ), V(W + .2f, .8f, D + .2f), Plinth);
            Box(m, ComicSurface.Plaster, V(0, H - .2f, hallZ), V(W + .4f, .4f, D + .4f), LimeTrim);
            // decorative band of small blind arches under the eaves
            for (int i = 0; i < 22; i++) Arch(m, V(-W * .5f + (i + .5f) * W / 22f, H - 1.3f, front + .03f), Vector3.right, Vector3.forward, .7f, .9f, LimeShade);
            HipRoof(m, V(0, H, hallZ), W + 1f, D + 1f, 4.2f, Terracotta, TerracottaDark);
            for (int i = 0; i < 9; i++) Arch(m, V(-W * .5f + 2.2f + i * (W - 4.4f) / 8f, 8.4f, front + .03f), Vector3.right, Vector3.forward, 1.25f, 4.8f, WindowDark);
            // Loggia: two storeys of round arches across the front.
            const float LW = W - 5f, LD = 4f, LH = 8f;
            float lz = front + LD * .5f, lf = front + LD;
            Box(m, ComicSurface.Plaster, V(1.5f, LH * .5f, lz), V(LW, LH, LD), Lime);
            Box(m, ComicSurface.Plaster, V(1.5f, LH - .15f, lz), V(LW + .3f, .3f, LD + .3f), LimeTrim);
            Box(m, ComicSurface.Plaster, V(1.5f, 4.55f, lf + .08f), V(LW, .45f, .2f), LimeTrim);
            for (int i = 0; i < 10; i++) Arch(m, V(1.5f - LW * .5f + (i + .5f) * LW / 10f, .8f, lf + .03f), Vector3.right, Vector3.forward, LW / 10f * .7f, 3.4f, ArchDark);
            for (int i = 0; i < 20; i++) Arch(m, V(1.5f - LW * .5f + (i + .5f) * LW / 20f, 5f, lf + .03f), Vector3.right, Vector3.forward, LW / 20f * .62f, 2.1f, ArchDark);
            // Steps and a terrace in front.
            Box(m, ComicSurface.Paving, V(1.5f, .25f, lf + 1.5f), V(LW + 2f, .5f, 3f), LimeTrim);
            // Clock tower.
            const float T = 44f, TW = 7f;
            Vector3 tc = V(-W * .5f - TW * .5f + 1f, 0, front - 1f);
            Box(m, ComicSurface.Plaster, tc + V(0, T * .5f, 0), V(TW, T, TW), Lime);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Box(m, ComicSurface.Plaster, tc + V(sx * (TW * .5f - .25f), (T - 6f) * .5f, sz * (TW * .5f - .25f)), V(.62f, T - 6f, .62f), LimeShade);
            foreach (float y in new[] { 14f, 23f, 31.5f })
                Box(m, ComicSurface.Plaster, tc + V(0, y, 0), V(TW + .3f, .4f, TW + .3f), LimeTrim);
            Box(m, ComicSurface.Plaster, tc + V(0, T - .3f, 0), V(TW + .6f, .6f, TW + .6f), LimeTrim);
            HipRoof(m, tc + V(0, T, 0), TW + .7f, TW + .7f, 2.4f, Terracotta, TerracottaDark);
            Vector3[] normals = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            foreach (Vector3 n in normals)
            {
                Vector3 right = Vector3.Cross(Vector3.up, n), face = tc + n * (TW * .5f + .03f);
                // belfry: two tall arches, a row of four small ones above
                for (int i = -1; i <= 1; i += 2) Arch(m, face + right * (i * 1.3f) + V(0, 32.5f, 0), right, n, 1.5f, 4.2f, ArchDark);
                for (int i = 0; i < 4; i++) Arch(m, face + right * (-2.1f + i * 1.4f) + V(0, 38.6f, 0), right, n, .85f, 2.3f, ArchDark);
                // long slit windows lower down
                Arch(m, face + V(0, 16f, 0), right, n, .7f, 3.6f, WindowDark);
                if (n == Vector3.back) continue; // no clock on the back
                // clock: cream ring, blue face, gold hands
                Disc(m, face + V(0, 26.5f, 0) + n * .02f, n, 1.75f, LimeTrim, 12);
                Disc(m, face + V(0, 26.5f, 0) + n * .06f, n, 1.4f, ClockBlue, 12);
                Vector3 c0 = face + V(0, 26.5f, 0) + n * .1f;
                Slab(m, c0 + V(0, .45f, 0), right, n, .12f, .9f, PeacockGold);
                Slab(m, c0 + right * .3f + V(0, -.12f, 0), right, n, .7f, .1f, PeacockGold);
            }
            return m;
        }

        static ComicMesh Jacaranda(int seed, float size = 1f)
        {
            var rng = new System.Random(seed * 7919);
            float R() => (float)rng.NextDouble();
            var m = new ComicMesh();
            m.Surface = ComicSurface.Wood;
            float lean = (R() - .5f) * .6f;
            m.Cylinder(V(0, 1.4f, 0) * size, .22f * size, 2.8f * size, Trunk, 6, .75f);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f + R();
                m.Beam(V(0, 2.5f, 0) * size, V(Mathf.Cos(a) * 1.9f + lean, 4.6f, Mathf.Sin(a) * 1.9f) * size, .17f * size, Trunk);
            }
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(V(lean, 5.1f, 0) * size, V(7f, 2.4f, 7f) * size, J1);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 1.571f + R() * .6f, rr = 2.1f + R() * .7f;
                m.Ellipsoid(V(Mathf.Cos(a) * rr + lean, 5.6f + R() * .6f, Mathf.Sin(a) * rr) * size, V(3.4f, 2f, 3.4f) * size, i % 2 == 0 ? J2 : J3);
            }
            m.Ellipsoid(V(lean + .3f, 6.4f, -.2f) * size, V(3.4f, 1.7f, 3.4f) * size, J3);
            return m;
        }

        static ComicMesh CypressMesh(float size)
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.Wood;
            m.Cylinder(V(0, .4f, 0), .18f, .8f, Trunk, 6);
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(V(0, 4.6f * size, 0), V(2f, 8.8f, 2f) * size, Cypress);
            m.Ellipsoid(V(.25f, 5.6f * size, -.2f), V(1.2f, 5.5f, 1.2f) * size, Color.Lerp(Cypress, Grass, .3f));
            return m;
        }

        static ComicMesh Palm(int seed, float height)
        {
            var rng = new System.Random(seed * 104729);
            var m = new ComicMesh();
            m.Surface = ComicSurface.Wood;
            m.Cylinder(V(0, height * .5f, 0), .3f, height, PalmTrunk, 6, .75f);
            m.Cylinder(V(0, height - .3f, 0), .48f, 1.4f, C(0xB79A6A), 6, 1.2f);
            m.Surface = ComicSurface.Foliage;
            Vector3 top = V(0, height + .4f, 0);
            float spin = (float)rng.NextDouble() * 6f;
            for (int i = 0; i < 9; i++)
            {
                float a = spin + i * Mathf.PI * 2f / 9f, droop = i % 2 == 0 ? -.35f : .05f;
                var dir = new Vector3(Mathf.Cos(a), droop, Mathf.Sin(a)).normalized;
                m.Box(top + dir * 1.25f, V(.75f, .05f, 2.6f), i % 3 == 0 ? Color.Lerp(PalmLeaf, _r.yellow, .2f) : PalmLeaf, Quaternion.LookRotation(dir));
            }
            m.Ellipsoid(top, V(.9f, .7f, .9f), Color.Lerp(PalmLeaf, _r.ink, .2f));
            return m;
        }

        static ComicMesh CampusLamp()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .1f, 0), .16f, .2f, _r.ink, 6);
            m.Cylinder(V(0, 2.4f, 0), .07f, 4.6f, _r.ink, 6);
            m.Cylinder(V(0, 4.75f, 0), .48f, .14f, _r.ink, 8);
            m.Cylinder(V(0, 4.66f, 0), .32f, .05f, _r.cream, 8);
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

        static ComicMesh AFrameSign()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            m.Box(V(0, .5f, .17f), V(.9f, 1.02f, .06f), _r.cream, Quaternion.Euler(-16, 0, 0));
            m.Box(V(0, .5f, -.17f), V(.9f, 1.02f, .06f), _r.cream, Quaternion.Euler(16, 0, 0));
            m.Surface = ComicSurface.Plain;
            // A blue panel with a gold arrow on the side facing the rider (-Z).
            Quaternion tilt = Quaternion.Euler(16, 0, 0);
            Vector3 n = tilt * Vector3.back, right = tilt * Vector3.left, up = tilt * Vector3.up, c = V(0, .58f, -.21f);
            Panel(m, c, right, up, n, .74f, .66f, UwaBlue);
            Panel(m, c + up * .05f + n * .01f, right, up, n, .36f, .1f, PeacockGold);
            Tri(m, c + n * .01f + right * .28f + up * .05f, c + n * .01f + right * .14f + up * .19f, c + n * .01f + right * .14f - up * .09f, PeacockGold, n);
            Panel(m, c - up * .2f + n * .01f, right, up, n, .5f, .06f, _r.cream);
            return m;
        }

        static ComicMesh Sprinkler()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            m.Cylinder(V(0, .04f, 0), .3f, .08f, C(0x3C8A4A), 8);
            m.Cylinder(V(0, .17f, 0), .07f, .22f, C(0xF2C230), 6);
            m.Box(V(0, .3f, 0), V(.5f, .06f, .08f), C(0xF2C230), Quaternion.identity);
            Color spray = C(0xBFE4F8);
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                m.Beam(V(0, .32f, 0) + dir * .22f, V(0, .68f, 0) + dir * .38f, .035f, spray);
                m.Beam(V(0, .68f, 0) + dir * .38f, V(0, .25f, 0) + dir * .5f, .03f, spray);
                m.Ellipsoid(V(0, .2f, 0) + dir * .5f, V(.07f, .07f, .07f), Color.white);
            }
            return m;
        }

        // Peacock facing -Z, feet at the pivot. fan: tail raised in a display fan behind the body.
        static ComicMesh PeacockMesh(bool fan)
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color legs = C(0x8A8070);
            for (int s = -1; s <= 1; s += 2) m.Box(V(s * .08f, .19f, .04f), V(.05f, .38f, .05f), legs);
            m.Ellipsoid(V(0, .56f, .06f), V(.44f, .42f, .66f), Peacock);
            m.Ellipsoid(V(0, .64f, .26f), V(.38f, .26f, .4f), C(0x8C7650)); // brown wing coverts
            m.Beam(V(0, .66f, -.14f), V(0, .98f, -.2f), .17f, PeacockNeck);
            m.Ellipsoid(V(0, 1.04f, -.22f), V(.19f, .19f, .23f), PeacockNeck);
            m.Box(V(0, 1.02f, -.36f), V(.05f, .05f, .1f), C(0xD9C9A0));
            for (int s = -1; s <= 1; s += 2) { m.Box(V(s * .095f, 1.06f, -.27f), V(.012f, .05f, .1f), Color.white); m.Box(V(s * .1f, 1.06f, -.28f), V(.012f, .025f, .025f), _r.ink); }
            for (int i = -1; i <= 1; i++)
            {
                Vector3 tip = V(i * .07f, 1.32f, -.2f + i * .015f);
                m.Beam(V(0, 1.12f, -.22f), tip, .02f, PeacockNeck);
                m.Ellipsoid(tip, V(.07f, .06f, .07f), PeacockNeck);
            }
            if (!fan)
            {
                // the long train trailing behind, with eye spots
                m.Surface = ComicSurface.Foliage;
                Vector3 a = V(-.2f, .55f, .3f), b = V(.2f, .55f, .3f), c = V(.36f, .1f, 1.6f), d = V(-.36f, .1f, 1.6f);
                Face(m, a, b, c, d, PeacockTail, Vector3.up);
                Face(m, a, b, c, d, Color.Lerp(PeacockTail, _r.ink, .3f), Vector3.down);
                Face(m, a, d, d + V(0, -.08f, 0), a + V(0, -.08f, 0), PeacockTail, Vector3.left);
                Face(m, b, c, c + V(0, -.08f, 0), b + V(0, -.08f, 0), PeacockTail, Vector3.right);
                Vector3 slope = Vector3.Cross(b - a, d - a).normalized;
                if (slope.y < 0f) slope = -slope;
                for (int i = 0; i < 9; i++)
                {
                    float t = .25f + (i / 3) * .25f, x = ((i % 3) - 1) * (.08f + t * .14f);
                    Vector3 p = Vector3.Lerp((a + b) * .5f, (c + d) * .5f, t) + V(x, 0, 0) + slope * .015f;
                    Disc(m, p, slope, .08f, PeacockGold, 6);
                    Disc(m, p + slope * .01f, slope, .045f, UwaBlue, 6);
                }
            }
            else
            {
                // display fan: a half-disc of green feathers with a ring of eye spots, facing the rider (-Z)
                m.Surface = ComicSurface.Foliage;
                Vector3 centre = V(0, .45f, .32f);
                Quaternion tilt = Quaternion.Euler(-12, 0, 0);
                const int n = 12; const float r = 1.05f;
                for (int i = 0; i < n; i++)
                {
                    float a0 = Mathf.PI * (-.05f + 1.1f * i / n), a1 = Mathf.PI * (-.05f + 1.1f * (i + 1) / n);
                    Vector3 p0 = centre + tilt * V(Mathf.Cos(a0) * r, Mathf.Sin(a0) * r, 0), p1 = centre + tilt * V(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, 0);
                    Color col = i % 2 == 0 ? PeacockTail : Color.Lerp(PeacockTail, PeacockGold, .25f);
                    Tri(m, centre, p0, p1, col, tilt * Vector3.back);
                    Tri(m, centre + tilt * V(0, 0, .03f), p0 + tilt * V(0, 0, .03f), p1 + tilt * V(0, 0, .03f), Color.Lerp(PeacockTail, _r.ink, .35f), tilt * Vector3.forward);
                }
                for (int i = 0; i < 11; i++)
                {
                    float a = Mathf.PI * (.0f + i / 10f);
                    foreach (float rr in new[] { .85f, .55f })
                    {
                        if (rr < .6f && i % 2 == 1) continue;
                        Vector3 p = centre + tilt * V(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, -.02f);
                        Disc(m, p, tilt * Vector3.back, .085f, PeacockGold, 6);
                        Disc(m, p + tilt * V(0, 0, -.01f), tilt * Vector3.back, .05f, UwaBlue, 6);
                    }
                }
            }
            return m;
        }

        static ComicMesh UwaFar()
        {
            var m = new ComicMesh();
            var rng = new System.Random(4242);
            float R() => (float)rng.NextDouble();
            float g = _r.downhillGrade;
            // Ground to the horizon on both sides, then tree lines and terracotta roofs.
            var ground = new ComicMesh();
            for (int s = -1; s <= 1; s += 2)
            {
                ground.Surface = ComicSurface.Foliage;
                StripSlab(ground, s * 70f, s * 420f, -80f, 520f, -.1f, LawnDark, 40f);
                for (float z = -60f; z < 500f; z += 26f + R() * 14f)
                {
                    float x = s * (80f + R() * 40f);
                    if (R() < .45f)
                    {
                        float w = 14f + R() * 10f, d = 10f + R() * 6f, h = 6f + R() * 5f;
                        Box(ground, ComicSurface.Plaster, V(x, h * .5f - 2f, z), V(d, h + 4f, w), Color.Lerp(Lime, _r.cream, .3f));
                        HipRoof(ground, V(x, h - 2f, z), d + .8f, w + .8f, 2.6f, Terracotta, TerracottaDark);
                    }
                    else
                    {
                        ground.Surface = ComicSurface.Foliage;
                        ground.Ellipsoid(V(x, 5f, z), V(16f + R() * 8f, 12f + R() * 6f, 14f), Color.Lerp(Cypress, LawnDark, R() * .5f));
                    }
                }
                // a dark bush ridge further out (Kings Park on the left)
                for (float z = -60f; z < 500f; z += 40f)
                {
                    ground.Surface = ComicSurface.Foliage;
                    ground.Ellipsoid(V(s * (150f + R() * 30f), s < 0 ? 12f : 6f, z), V(60f, s < 0 ? 34f : 20f, 50f), Color.Lerp(Cypress, C(0x6C7D44), .5f));
                }
            }
            ground.Shear(g);
            m.Add(ground, Vector3.zero, Quaternion.identity, Vector3.one);
            return m;
        }

        // =====================================================================================================
        // MATILDA BAY
        // =====================================================================================================

        static readonly Color RedPath = C(0xA8483A), RiverDeep = C(0x2F7EC0), RiverMid = C(0x4A98D2), RiverLight = C(0x9ACDEC),
            Sand = C(0xE8D6A6), BayLawn = C(0x7DB24E), Peppermint = C(0x5F8B3A), PeppermintLight = C(0x7FA44C), PepTrunk = C(0x6B5745),
            Bush = C(0x6E7E45), BushDark = C(0x4F6334), LimeRock = C(0xD9CDA8), ShedBlue = C(0x78A6D8), White = C(0xF4F4EE),
            ShedRoof = C(0xE3E8EA), Timber = C(0xA39684), TimberDark = C(0x6E5E4E), PicnicWood = C(0x9C6B43), ShelterRoof = C(0x3F7055),
            Swan = C(0x1B1D24), SwanBeak = C(0xD8322B), EskyBlue = C(0x2D6DB5), Wicker = C(0xC49A55);

        static void BuildMatilda()
        {
            Begin("Matilda");
            float g = MatildaGrade;
            var chunks = new List<RoadChunk>();
            RoadChunk entry = SaveChunk(MatildaEntry(g, false));
            chunks.Add(SaveChunk(MatildaForeshore(g)));
            chunks.Add(SaveChunk(MatildaBoatshed(g)));
            chunks.Add(SaveChunk(MatildaCafe(g)));
            chunks.Add(SaveChunk(MatildaInlet(g)));
            chunks.Add(SaveChunk(MatildaPicnic(g)));
            RoadChunk exit = SaveChunk(MatildaEntry(g, true));

            Ramp jetty = SaveRamp("RMP_Matilda_Jetty", JettyRampMesh(3f, 5f, 1.3f, 0f, g, 0f), 3f, 5f, 1.3f, 0f, 1f, .5f);
            var esky = SaveObstacle("OBS_Matilda_Esky", Esky(), new Vector3(0, .25f, 0), new Vector3(.75f, .5f, .5f), ObstacleKind.Thing, true, true);
            var basket = SaveObstacle("OBS_Matilda_PicnicBasket", PicnicBasket(), new Vector3(0, .28f, 0), new Vector3(.7f, .56f, .5f), ObstacleKind.Thing, true, true);
            GameObject swan = SaveAnimal("OBS_Matilda_BlackSwan", SwanMesh(), new Vector3(0, .45f, .05f), new Vector3(.5f, .9f, .9f), true, .4f);
            GameObject far = SaveFar("PRP_Matilda_FarRiver", MatildaFar(g));

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
            d.farScenery = far; d.gateway = null; d.ambience = null;
            EditorUtility.SetDirty(d);
        }

        static Chunk MatildaBase(string id, float g, bool river = true, bool bank = true)
        {
            var c = new Chunk(id, g) { WalkLeft = _r.pavement, WalkRight = RedPath, RedRight = true };
            if (river)
            {
                c.Lawn(1, 8f, 15f, 0f, 40f, BayLawn);
                c.Beach(15f, 17.5f, 0f, 40f);
                c.Water(17.5f, 170f, 0f, 40f, -.45f, RiverMid);
                c.Ripples(19f, 120f, 0f, 40f, -.43f);
            }
            if (bank) c.Escarpment(32f, 0f, 40f);
            return c;
        }

        // Entry (calm at z = 0) / exit (calm at z = 40): lawns and peppermint trees, low hedges.
        static Chunk MatildaEntry(float g, bool exit)
        {
            Chunk c = MatildaBase(exit ? "Exit" : "Entry", g);
            float Z(float z) => exit ? 40f - z : z;
            c.Lawn(-1, 8f, 32f, 0f, 40f, BayLawn);
            c.HedgeRow(-1, 8.9f, Mathf.Min(Z(0), Z(14)), Mathf.Max(Z(0), Z(14)));
            c.Tree(PeppermintMesh(201), -12f, Z(5), false);
            c.Tree(PeppermintMesh(202), -15f, Z(22), false);
            c.Tree(PeppermintMesh(203), -11.5f, Z(35), false);
            c.Tree(PeppermintMesh(204), 11.5f, Z(9), false);
            c.Tree(PeppermintMesh(205), 12f, Z(31), false);
            c.Prop(PicnicTable(), -16f, Z(30), 10f);
            c.Lamp(-8.7f, Z(12), true); c.Lamp(8.4f, Z(26), true);
            c.Boat(48f, Z(14), 3); c.Boat(80f, Z(34), 4);
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
            c.Boat(30f, 8f, 1); c.Boat(42f, 28f, 2); c.Boat(66f, 18f, 5);
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
            c.Boat(36f, 22f, 7); c.Boat(58f, 34f, 8);
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

        static ComicMesh PeppermintMesh(int seed)
        {
            var rng = new System.Random(seed * 31337);
            float R() => (float)rng.NextDouble();
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            float lean = (R() - .5f) * 1.4f;
            m.Beam(V(0, 0, 0), V(lean * .5f, 2.2f, .2f), .32f, PepTrunk);
            m.Beam(V(lean * .5f, 2.2f, .2f), V(lean, 4f, 0), .24f, PepTrunk);
            m.Beam(V(lean * .5f, 2.2f, .2f), V(lean - 1.4f, 3.6f, .8f), .16f, PepTrunk);
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(V(lean, 4.6f, 0), V(5.6f, 2.6f, 5.2f), Peppermint);
            m.Ellipsoid(V(lean + .5f, 5.4f, -.4f), V(3.4f, 1.8f, 3.2f), PeppermintLight);
            // weeping curtains of leaves around the crown
            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.0472f + R() * .4f;
                m.Ellipsoid(V(lean + Mathf.Cos(a) * 2.4f, 3.1f, Mathf.Sin(a) * 2.3f), V(1.7f, 3.6f, 1.7f), i % 2 == 0 ? Peppermint : PeppermintLight);
            }
            return m;
        }

        static ComicMesh PicnicTable()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            m.Box(V(0, .74f, 0), V(1.9f, .07f, .85f), PicnicWood);
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(V(0, .44f, s * .62f), V(1.9f, .06f, .3f), PicnicWood);
                m.Beam(V(s * .7f, 0, -.7f), V(s * .7f, .72f, 0), .08f, Color.Lerp(PicnicWood, _r.ink, .3f));
                m.Beam(V(s * .7f, 0, .7f), V(s * .7f, .72f, 0), .08f, Color.Lerp(PicnicWood, _r.ink, .3f));
            }
            return m;
        }

        static ComicMesh BbqShelter()
        {
            var m = new ComicMesh { Surface = ComicSurface.Concrete };
            m.Box(V(0, .05f, 0), V(5f, .1f, 3.6f), LimeRock);
            m.Surface = ComicSurface.PaintedMetal;
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2) m.Box(V(sx * 2.1f, 1.35f, sz * 1.5f), V(.14f, 2.7f, .14f), White);
            GableRoof(m, V(0, 2.7f, 0), 5.4f, 4.2f, 1.1f, ShelterRoof, ShelterRoof, true);
            m.Surface = ComicSurface.Metal;
            m.Box(V(0, .45f, 0), V(1.6f, .9f, .8f), C(0x8A8F96));
            m.Box(V(0, .92f, 0), V(1.4f, .05f, .6f), _r.ink);
            m.Add(PicnicTable(), V(0, 0, -2.6f), Quaternion.identity, Vector3.one);
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
            GableRoof(m, V(0, H, 0), W + .5f, D + .5f, 1.6f, ShedRoof, ShedBlue, false);
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
            m.Cylinder(V(-W * .3f, H + 2.4f, 0), .04f, 2.4f, White, 6);
            Panel(m, V(-W * .3f + .45f, H + 3.25f, 0), Vector3.right, Vector3.up, Vector3.forward, .8f, .5f, C(0x1F2E78));
            Panel(m, V(-W * .3f + .45f, H + 3.25f, 0), Vector3.left, Vector3.up, Vector3.back, .8f, .5f, C(0x1F2E78));
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
            var m = new ComicMesh { Surface = ComicSurface.Metal };
            m.Cylinder(V(0, 1.2f, 0), .04f, 2.4f, White, 6);
            m.Surface = ComicSurface.Fabric;
            m.Cylinder(V(0, 2.3f, 0), 1.4f, .45f, i % 2 == 0 ? _r.cream : ShedBlue, 8, .05f);
            m.Surface = ComicSurface.Wood;
            m.Cylinder(V(0, .38f, 0), .45f, .05f, PicnicWood, 8);
            m.Cylinder(V(0, .2f, 0), .05f, .38f, _r.ink, 6);
            return m;
        }

        static ComicMesh SailBoat(int seed)
        {
            var rng = new System.Random(seed * 17);
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            float len = 4f + (float)rng.NextDouble() * 2f;
            Color hull = seed % 3 == 0 ? C(0x1F3F7A) : White;
            m.Box(V(0, .25f, 0), V(1.3f, .55f, len), hull);
            m.Box(V(0, .3f, len * .5f + .25f), V(.8f, .45f, .6f), hull, Quaternion.Euler(0, 45, 0));
            m.Box(V(0, .62f, -.2f), V(.9f, .35f, 1.4f), White);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(V(0, 3.6f, .4f), .05f, 6.4f, White, 6);
            m.Surface = ComicSurface.Fabric;
            Vector3 a = V(0, .9f, .45f), b = V(0, 6.6f, .45f), c = V(0, .95f, -len * .45f);
            Color sail = seed % 4 == 1 ? C(0xF1D9A4) : White;
            Tri(m, a, b, c, sail, Vector3.right);
            Tri(m, a, b, c, sail, Vector3.left);
            Vector3 j = V(0, .8f, len * .5f + .4f);
            Tri(m, V(0, .9f, .55f), V(0, 5.6f, .5f), j, Color.Lerp(sail, _r.cream, .3f), Vector3.right);
            Tri(m, V(0, .9f, .55f), V(0, 5.6f, .5f), j, Color.Lerp(sail, _r.cream, .3f), Vector3.left);
            return m;
        }

        static ComicMesh Esky()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            m.Box(V(0, .2f, 0), V(.72f, .4f, .46f), EskyBlue);
            m.Box(V(0, .44f, 0), V(.76f, .1f, .5f), White);
            m.Box(V(0, .52f, 0), V(.5f, .05f, .07f), White);
            for (int s = -1; s <= 1; s += 2) m.Box(V(s * .25f, .5f, 0), V(.05f, .1f, .07f), White);
            Panel(m, V(0, .22f, -.235f), Vector3.left, Vector3.up, Vector3.back, .5f, .12f, White);
            return m;
        }

        static ComicMesh PicnicBasket()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            m.Box(V(0, .18f, 0), V(.66f, .36f, .44f), Wicker);
            for (int i = 0; i < 3; i++) Panel(m, V(0, .08f + i * .11f, -.225f), Vector3.left, Vector3.up, Vector3.back, .66f, .025f, Color.Lerp(Wicker, _r.ink, .35f));
            for (int s = -1; s <= 1; s += 2) m.Box(V(s * .17f, .4f, 0), V(.31f, .06f, .46f), Color.Lerp(Wicker, _r.cream, .2f), Quaternion.Euler(0, 0, s * -6f));
            m.Beam(V(-.2f, .42f, 0), V(0, .58f, 0), .04f, Color.Lerp(Wicker, _r.ink, .3f));
            m.Beam(V(0, .58f, 0), V(.2f, .42f, 0), .04f, Color.Lerp(Wicker, _r.ink, .3f));
            // red and white gingham peeking out
            m.Surface = ComicSurface.Fabric;
            for (int i = 0; i < 4; i++) m.Box(V(-.24f + i * .16f, .37f, -.2f), V(.15f, .06f, .08f), i % 2 == 0 ? C(0xD23A3A) : White);
            return m;
        }

        // Black swan facing -Z, feet at the pivot.
        static ComicMesh SwanMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            for (int s = -1; s <= 1; s += 2) m.Box(V(s * .08f, .1f, .05f), V(.05f, .2f, .05f), C(0x2A2A2E));
            m.Ellipsoid(V(0, .4f, .1f), V(.44f, .36f, .78f), Swan);
            // ruffled wing feathers on the back and white flight-feather tips
            for (int i = 0; i < 3; i++) m.Box(V((i - 1) * .1f, .58f, .2f + i * .06f), V(.1f, .1f, .22f), C(0x2B2E36), Quaternion.Euler(-25, (i - 1) * 15, 0));
            for (int s = -1; s <= 1; s += 2) m.Box(V(s * .19f, .42f, .4f), V(.04f, .08f, .22f), White);
            m.Beam(V(0, .48f, -.2f), V(0, .68f, -.28f), .1f, Swan);
            m.Beam(V(0, .68f, -.28f), V(0, .86f, -.22f), .09f, Swan);
            m.Ellipsoid(V(0, .9f, -.26f), V(.12f, .12f, .16f), Swan);
            m.Box(V(0, .88f, -.38f), V(.06f, .05f, .14f), SwanBeak);
            m.Box(V(0, .885f, -.45f), V(.065f, .055f, .02f), White);
            for (int s = -1; s <= 1; s += 2) m.Box(V(s * .061f, .92f, -.28f), V(.01f, .025f, .025f), SwanBeak);
            return m;
        }

        static ComicMesh MatildaFar(float g)
        {
            var m = new ComicMesh();
            var rng = new System.Random(2020);
            float R() => (float)rng.NextDouble();
            var flat = new ComicMesh();
            // The river out to the far shore, under the chunks' own water (no gap, no z-fighting).
            flat.Surface = ComicSurface.Plain;
            StripSlab(flat, 40f, 600f, -80f, 560f, -.62f, RiverDeep, 40f);
            for (int i = 0; i < 70; i++)
            {
                float x = 50f + R() * 230f, z = -40f + R() * 520f, w = 2f + R() * 6f;
                FlatQuad(flat, x - w, x + w, z, z + .35f + R() * .3f, -.58f, i % 3 == 0 ? White : RiverLight);
            }
            // The far shore (South Perth): low land with trees and pale houses.
            flat.Surface = ComicSurface.Foliage;
            StripSlab(flat, 280f, 600f, -80f, 560f, 1.2f, C(0x86A27A), 60f);
            flat.Shear(g);
            m.Add(flat, Vector3.zero, Quaternion.identity, Vector3.one);
            var up = new ComicMesh();
            for (float z = -60f; z < 540f; z += 14f + R() * 10f)
            {
                float x = 286f + R() * 60f, y = -g * z;
                up.Surface = ComicSurface.Foliage;
                up.Ellipsoid(V(x, y + 4f, z), V(14f + R() * 10f, 8f + R() * 5f, 12f), Color.Lerp(C(0x5E7E58), C(0x7E9A80), R()));
                if (R() < .5f) Box(up, ComicSurface.Plaster, V(x - 10f, y + 2.5f, z + 6f), V(6f, 4f, 8f), Color.Lerp(White, C(0xC9B9A0), R()));
            }
            // (The CBD skyline itself is in the panorama sky, straight ahead across the water.)
            // sailboats out on the bay
            for (int i = 0; i < 9; i++)
            {
                float x = 60f + R() * 180f, z = 30f + R() * 260f;
                up.Add(SailBoat(300 + i), V(x, -g * z - .55f, z), Quaternion.Euler(0, R() * 360f, 0), Vector3.one * 1.3f);
            }
            m.Add(up, Vector3.zero, Quaternion.identity, Vector3.one);
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
                Sheared(Back, t => { t.Surface = ComicSurface.Plain; StripSlab(t, x0, x1, z0, z1, .27f, PondWater, 4f, false); });
                Sheared(Front, t =>
                {
                    t.Surface = ComicSurface.Plaster;
                    StripBox(t, x0 - .35f, x0, z0 - .35f, z1 + .35f, 0f, .42f, LimeTrim);
                    StripBox(t, x1, x1 + .35f, z0 - .35f, z1 + .35f, 0f, .42f, LimeTrim);
                    t.Box(V((x0 + x1) * .5f, .21f, z0 - .17f), V(x1 - x0, .42f, .35f), LimeTrim);
                    t.Box(V((x0 + x1) * .5f, .21f, z1 + .17f), V(x1 - x0, .42f, .35f), LimeTrim);
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

            public void Escarpment(float from, float z0, float z1)
            {
                Sheared(Back, t =>
                {
                    t.Surface = ComicSurface.Foliage;
                    for (float z = z0; z < z1 - .01f; z += 4f)
                    {
                        float e = Mathf.Min(z1, z + 4f);
                        Face(t, V(-from, Kerb, z), V(-from, Kerb, e), V(-from - 26f, 20f, e), V(-from - 26f, 20f, z), Bush, V(1, 1, 0), false);
                    }
                    StripSlab(t, -from - 120f, -from - 26f, z0, z1, 20f, Bush, 4f, false);
                });
                var rng = new System.Random(Id.GetHashCode() & 0xfff);
                for (int i = 0; i < 9; i++)
                {
                    float u = (float)rng.NextDouble(), z = z0 + (float)rng.NextDouble() * (z1 - z0), x = -from - u * 26f, y = Top(z) + u * 19.8f;
                    Back.Surface = ComicSurface.Foliage;
                    Back.Ellipsoid(V(x, y + 1f, z), V(4f + u * 3f, 2.6f, 4f), i % 3 == 0 ? BushDark : Color.Lerp(Bush, BushDark, .4f));
                    if (i % 4 == 1) { Back.Surface = ComicSurface.Concrete; Back.Box(V(x - 1f, y + .6f, z + 2f), V(3f, 1.6f, 2.4f), LimeRock); }
                }
                for (int i = 0; i < 4; i++)
                {
                    float z = z0 + 5f + i * 10f, x = -from - 30f - (float)rng.NextDouble() * 20f;
                    Back.Surface = ComicSurface.Foliage;
                    Back.Ellipsoid(V(x, Top(z) + 20f + 4f, z), V(9f, 7f, 8f), C(0x58703A));
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
                if (petals) Sheared(Back, t => Disc(t, V(x, Kerb + .035f, z), Vector3.up, 3.6f, Petals, 10));
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
                ComicMesh lamp = CampusLamp();
                if (white)
                {
                    lamp = new ComicMesh { Surface = ComicSurface.PaintedMetal };
                    lamp.Cylinder(V(0, 2.4f, 0), .07f, 4.8f, White, 6);
                    lamp.Beam(V(0, 4.7f, 0), V(x < 0 ? .9f : -.9f, 4.85f, 0), .08f, White);
                    lamp.Box(V(x < 0 ? .95f : -.95f, 4.78f, 0), V(.5f, .14f, .26f), White);
                    lamp.Box(V(x < 0 ? .95f : -.95f, 4.7f, 0), V(.4f, .03f, .2f), _r.cream);
                }
                Props.Add(lamp, V(x, Top(z), z), Quaternion.identity, Vector3.one);
                if (Mathf.Abs(x) < 8.05f) Blocks.Add(new Block { Id = "Lamp", Position = V(x, Top(z), z), Size = V(.35f, 4.8f, .35f), Jump = false });
            }

            public void Bench(float x, float z, Color? color = null)
            {
                float yaw = x < 0 ? 90f : -90f;
                var m = new ComicMesh { Surface = ComicSurface.Wood };
                Color col = color ?? Brick;
                for (int s = -1; s <= 1; s += 2) m.Box(V(s * .7f, .22f, 0), V(.08f, .44f, .5f), _r.ink);
                m.Box(V(0, .46f, 0), V(1.7f, .08f, .5f), col);
                m.Box(V(0, .75f, -.24f), V(1.7f, .32f, .07f), col);
                Props.Add(m, V(x, Top(z), z), Quaternion.Euler(0, yaw, 0), Vector3.one);
                Blocks.Add(new Block { Id = "Bench", Position = V(x, Top(z), z), Size = V(1.75f, .95f, .6f), Yaw = yaw, Jump = true });
            }

            public void StoneBench(float x, float z) => Props.Add(StoneBenchMesh(), V(x, Top(z), z), Quaternion.Euler(0, x < 0 ? 90f : -90f, 0), Vector3.one);

            public void BikeRack(float x, float z)
            {
                var m = new ComicMesh { Surface = ComicSurface.Metal };
                for (int i = -1; i <= 1; i++)
                {
                    m.Beam(V(i * .45f, 0, -.35f), V(i * .45f, .8f, -.35f), .05f, C(0x8D959E));
                    m.Beam(V(i * .45f, .8f, -.35f), V(i * .45f, .8f, .35f), .05f, C(0x8D959E));
                    m.Beam(V(i * .45f, .8f, .35f), V(i * .45f, 0, .35f), .05f, C(0x8D959E));
                }
                // a parked bike
                m.Surface = ComicSurface.Rubber;
                m.Cylinder(V(-.2f, .33f, -.5f), .33f, .05f, _r.ink, 8, 1, Quaternion.Euler(0, 0, 90));
                m.Cylinder(V(-.2f, .33f, .5f), .33f, .05f, _r.ink, 8, 1, Quaternion.Euler(0, 0, 90));
                m.Surface = ComicSurface.PaintedMetal;
                m.Beam(V(-.2f, .33f, -.5f), V(-.2f, .75f, .1f), .05f, _r.red);
                m.Beam(V(-.2f, .75f, .1f), V(-.2f, .33f, .5f), .05f, _r.red);
                float yaw = x < 0 ? 90f : -90f;
                Props.Add(m, V(x, Top(z), z), Quaternion.Euler(0, yaw, 0), Vector3.one);
                Blocks.Add(new Block { Id = "BikeRack", Position = V(x, Top(z), z), Size = V(1.3f, .95f, 1.2f), Yaw = yaw, Jump = true });
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
                        HipRoof(Back, V(x, Top(z) + h - 2f, z), 10.8f, w + .8f, 2.6f, Terracotta, TerracottaDark);
                    }
                    else
                    {
                        Back.Surface = ComicSurface.Foliage;
                        Back.Ellipsoid(V(x, Top(z) + 5f, z), V(13f, 11f, 12f), i % 2 == 0 ? Cypress : LawnDark);
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
                    bool red = s > 0 && c.RedRight;
                    Color walk = s < 0 ? c.WalkLeft : c.WalkRight;
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
                    if (c.BorderColor.a > 0f) Flat(m, s < 0 ? o0 : o0 - .45f, s < 0 ? o0 + .45f : o0, z0, z1, h + .015f, c.BorderColor);
                }
            }
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
