using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CityScooter.EditorTools.Art;
using CityScooter.Gameplay;
using CityScooter.Rendering;
using CityScooter.Track;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using K = CityScooter.EditorTools.Districts.CbdQuayKit;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// PERTH CBD district (v0.3, route order 50): St Georges Terrace glass towers with heritage facades between them
    /// (a red-brick town hall clock tower, sandstone colonnades, a mock-Tudor arcade entrance), Hay Street Mall
    /// pedestrian chunks, a car-park chunk, city bus and delivery van traffic, car-park and loading-dock ramps.
    /// Everything is generated with the tech artist's comic kit (ComicMesh, ComicPalette.mat + ComicInk.mat, vertex
    /// colours, ink outline renderers) and keeps the standard street cross-section at both ends of every chunk.
    ///   Menu: CityScooter/Districts/Build Perth CBD
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.CbdBuilder.BuildBatch
    /// </summary>
    public static class CbdBuilder
    {
        public const string Id = "cbd";
        public const string PrefabFolder = ComicCityBuilder.Prefabs + "/Districts/Cbd";
        public const string MeshFolder = ComicCityBuilder.Root + "/Districts/Cbd";
        public const string DataFolder = "Assets/_Project/Data/Districts";
        public const string PatternFolder = DataFolder + "/cbd";
        public const string DistrictPath = DataFolder + "/District_CBD.asset";
        /// <summary>The ordered cycle: it starts and ends at the street width (4), the Terrace part between Widen and
        /// Narrow is 4 lanes (halfWidth 8), Hay Street Mall stays narrow.</summary>
        public static readonly string[] ChunkIds = { "Widen", "Terrace", "TownHall", "Plaza", "CarPark", "Narrow", "MallArcade", "MallShops" };
        public const float WideHalf = 8f;
        public static string ChunkPath(string variant) => PrefabFolder + "/CHN_Cbd_" + variant + ".prefab";
        static string Pf(string name) => PrefabFolder + "/" + name + ".prefab";

        // Perth CBD palette (sRGB; sources in art-src/districts/cbd/SOURCES.txt).
        static readonly Color GlassBlue = K.Hex("2E5E9E"), GlassTeal = K.Hex("2F6B66"), GlassPale = K.Hex("7FA8D8"),
            GlassNavy = K.Hex("23384F"), GlassGreen = K.Hex("3E8C7A"), GlassSilver = K.Hex("9AAFC4");
        static readonly Color FrameLight = K.Hex("DDE3EA"), FrameDark = K.Hex("1F2833"), Concrete = K.Hex("E6E1D3"), RibbonDark = K.Hex("34404C");
        static readonly Color Brick = K.Hex("B5523B"), Trim = K.Hex("F1E6CE"), Slate = K.Hex("4A4F5A"), Sandstone = K.Hex("D9B98A"),
            SandDark = K.Hex("C29A68"), Render = K.Hex("F2EEE4"), Timber = K.Hex("4A3326");
        static readonly Color MallPaver = K.Hex("6F7378"), MallPaverLight = K.Hex("9A9C9E"), Granite = K.Hex("B8BABD"), Awning = K.Hex("3A3F46");
        static readonly Color[] MallWalls = { K.Hex("EFE6D2"), K.Hex("E9D3B8"), K.Hex("D8E0E4"), K.Hex("F3EBDD"), K.Hex("E4C9A8"), K.Hex("D7CFC2") };

        [MenuItem("CityScooter/Districts/Build Perth CBD")]
        public static void Build()
        {
            K.Begin(MeshFolder, PrefabFolder, DataFolder, PatternFolder);
            var drop = K.StandardDrop;

            // ---- obstacles, ramps, traffic
            Obstacle planter = K.SaveObstacle(Pf("OBS_Cbd_Planter"), "OBS_Cbd_Planter", PlanterMesh(), new Vector3(1.5f, .95f, 1.5f), ObstacleKind.Thing, true, false, 1f);
            Obstacle kiosk = K.SaveObstacle(Pf("OBS_Cbd_Kiosk"), "OBS_Cbd_Kiosk", KioskMesh(), new Vector3(1.9f, 2.6f, 1.9f), ObstacleKind.Thing, false, false, .7f);
            Obstacle cafe = K.SaveObstacle(Pf("OBS_Cbd_CafeSet"), "OBS_Cbd_CafeSet", CafeSetMesh(), new Vector3(1.7f, .95f, 1.1f), ObstacleKind.Thing, true, true, .8f);
            Ramp carParkRamp = K.SaveRamp(Pf("RMP_Cbd_CarPark"), "RMP_Cbd_CarPark", 3f, 6f, 1.5f, 1.5f, 1f, .5f, K.Hex("9EA3A8"), K.Hex("7D8288"), true);
            Ramp dockRamp = K.SaveRamp(Pf("RMP_Cbd_LoadingDock"), "RMP_Cbd_LoadingDock", 2.4f, 3.2f, .9f, 0f, 1.15f, .55f, K.Hex("6B7078"), K.Hex("50555C"), true);
            GameObject busOncoming = K.SaveVehicle(Pf("VEH_Cbd_BusOncoming"), "VEH_Cbd_Bus", BusMesh(), new Vector3(2.55f, 3.3f, 12.2f), -11f, 0f);
            GameObject busAhead = K.SaveVehicle(Pf("VEH_Cbd_BusAhead"), "VEH_Cbd_Bus", null, new Vector3(2.55f, 3.3f, 12.2f), 0f, .3f);
            GameObject vanAhead = K.SaveVehicle(Pf("VEH_Cbd_VanAhead"), "VEH_Cbd_Van", VanMesh(), new Vector3(2.05f, 2.6f, 5.3f), 0f, .35f);
            GameObject vanOncoming = K.SaveVehicle(Pf("VEH_Cbd_VanOncoming"), "VEH_Cbd_Van", null, new Vector3(2.05f, 2.6f, 5.3f), -14f, 0f);

            // ---- chunks
            K.WriteChunk(Entry(drop), ChunkPath("Entry"));
            K.WriteChunk(Widen(drop, true), ChunkPath("Widen"));
            K.WriteChunk(Widen(drop, false), ChunkPath("Narrow"));
            K.WriteChunk(Terrace(drop), ChunkPath("Terrace"));
            K.WriteChunk(TownHall(drop), ChunkPath("TownHall"));
            K.WriteChunk(Mall(drop, true), ChunkPath("MallArcade"));
            K.WriteChunk(Mall(drop, false), ChunkPath("MallShops"));
            K.WriteChunk(Plaza(drop), ChunkPath("Plaza"));
            K.WriteChunk(CarPark(drop), ChunkPath("CarPark"));
            K.WriteChunk(Exit(drop), ChunkPath("Exit"));
            GameObject far = K.SaveFarScenery(Pf("FAR_Cbd_Towers"), "FAR_Cbd_Towers", FarTowers());

            // ---- traffic patterns for the 4-lane Terrace (roadHalfWidth 8, never on the mall or the narrow chunks).
            // Australia drives on the left: same-way traffic x = -2 / -6, oncoming x = +2 / +6; buses in the kerb lanes.
            var patterns = new List<HazardPattern>
            {
                K.Pattern(PatternFolder, "HZ_Cbd_BusOncoming", 100, 1, 1f, false, 55, WideHalf, (busOncoming, 6f, 55f, 180f)),
                K.Pattern(PatternFolder, "HZ_Cbd_BusAhead", 100, 1, .9f, false, 50, WideHalf, (busAhead, -6f, 50f, 0f)),
                K.Pattern(PatternFolder, "HZ_Cbd_DeliveryVans", 115, 2, .9f, false, 50, WideHalf, (vanAhead, -2f, 45f, 0f), (vanOncoming, 2f, 85f, 180f)),
                K.Pattern(PatternFolder, "HZ_Cbd_BusAndVan", 120, 3, .7f, false, 55, WideHalf, (busAhead, -6f, 40f, 0f), (vanOncoming, 2f, 60f, 180f), (busOncoming, 6f, 100f, 180f)),
            };

            // ---- the district
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme == null) throw new InvalidOperationException("No comic theme at " + ComicCityBuilder.ThemePath);
            District d = K.LoadOrCreate<District>(DistrictPath);
            d.id = Id; d.displayName = "PERTH CBD"; d.order = 50; d.length = new Vector2(480f, 640f);
            d.chunks = ChunkIds.Select(c => AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(c)).GetComponent<RoadChunk>()).ToArray();
            d.orderedChunks = true;
            d.entry = AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath("Entry")).GetComponent<RoadChunk>();
            d.exit = AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath("Exit")).GetComponent<RoadChunk>();
            d.allowBoulevard = false; // the Terrace is 4 lanes by itself
            var obstacles = new List<Obstacle> { planter, kiosk, cafe };
            if (theme.obstacles != null) obstacles.AddRange(theme.obstacles.Where(o => o != null));
            d.obstacles = obstacles.ToArray();
            d.jumpObstacles = Array.Empty<Obstacle>();
            d.gifts = Array.Empty<Gift>();
            // The street's own patterns (roadworks, police, crossings, cars) still happen here, the 4-lane boulevard
            // patterns for the Terrace, plus buses and vans.
            var hazards = new List<HazardPattern>();
            if (theme.hazards != null) hazards.AddRange(theme.hazards.Where(h => h != null && !h.name.StartsWith("HZ_Cbd_") && !h.name.StartsWith("HZ_Quay_")));
            if (theme.boulevardHazards != null) hazards.AddRange(theme.boulevardHazards.Where(h => h != null && h.roadHalfWidth >= WideHalf - .01f && !hazards.Contains(h)));
            hazards.AddRange(patterns);
            d.hazards = hazards.ToArray();
            d.pedestrians = K.People(theme, "Office", "Office", "Office", "Courier", "Shopper", "Shopper", "Barista", "Student", "Tourist", "Elder", "Skater", "Jogger", "DogWalker");
            d.ramps = new[] { carParkRamp, dockRamp };
            d.rampFrequency = 1f;
            d.blendMeters = 150f;
            // Crisp and a little cooler in the shade of the towers.
            d.fogColor = new Color(.66f, .76f, .86f);
            d.skyTint = new Color(.93f, .97f, 1f);
            d.ambientSky = new Color(.62f, .74f, .90f);
            d.ambientEquator = new Color(.78f, .80f, .83f);
            d.ambientGround = new Color(.50f, .51f, .55f);
            d.sunColor = new Color(1f, .96f, .90f);
            d.sunIntensity = 1.25f;
            d.farScenery = far;
            d.gateway = null;
            d.ambience = null;
            d.ambienceVolume = .35f;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            K.WriteReport("art-src/districts/cbd/catalog.txt");
            K.RenderAtlas("art-src/districts/cbd/props-atlas.png", new List<(string, GameObject, ComicMesh)>
            {
                ("PlaneTree", null, Atlas(m => K.PlaneTree(m, null, Vector3.zero, 1f))),
                ("PlaneTreeFar", null, Atlas(m => K.PlaneTree(m, null, Vector3.zero, 1.25f, true))),
                ("TrafficLight", null, Atlas(m => K.TrafficLight(m, null, 1, 0f, 0f, 0f))),
                ("CafeUmbrella", null, Atlas(m => K.CafeUmbrella(m, null, Vector3.zero, K.Hex("F4F2EA")))),
                ("Planter", planter.gameObject, null),
                ("Kiosk", kiosk.gameObject, null),
                ("CafeSet", cafe.gameObject, null),
                ("Bus", busOncoming, null),
                ("Van", vanAhead, null),
                ("CarParkRamp", carParkRamp.gameObject, null),
                ("LoadingDock", dockRamp.gameObject, null),
                ("BusShelter", AssetDatabase.LoadAssetAtPath<GameObject>(ComicCityBuilder.Prefabs + "/Props/PRP_BusShelter.prefab"), null),
            });
            Debug.Log("[Districts/CBD] built " + (ChunkIds.Length + 2) + " chunks: " + string.Join("; ", K.Report));
        }

        static ComicMesh Atlas(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        // ================================================================ chunks

        static CqChunkDraft Entry(float drop)
        {
            // Calm at z = 0 (any district can come before): Stirling Gardens-style trees, hedges and low sandstone walls,
            // then the first heritage front and towers from z = 16.
            var d = new CqChunkDraft("Cbd_Entry", drop);
            StreetRoad(d, false);
            for (int s = -1; s <= 1; s += 2)
            {
                GardenEdge(d, s, 0, 16);
                CqLot heritage = new CqLot(s, 16, 28, K.Edge, 14, d.Base(22));
                if (s < 0) SandstoneColonnade(d, heritage, 3); else BrickHeritage(d, heritage, 3, true);
                GlassTower(d, new CqLot(s, 28, 40, K.Edge, 16, d.Base(34)), s < 0 ? 64 : 82, s < 0 ? GlassTeal : GlassBlue, FrameLight, s < 0 ? 0 : 3);
                for (float z = 3; z < 16; z += 6.5f) K.PlaneTree(d.Props, d.Collisions, new Vector3(s * 5.6f, d.Ground(z), z), .95f);
                K.Lamp(d.Props, d.Collisions, s, d.Ground(22f), 22f);
                DeepTowers(d, s, 16, 40, 1);
                DeepParkTrees(d, s, 0, 16);
            }
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft Exit(float drop)
        {
            // Towers first, then calm towards z = 40: the Esplanade end with trees and low limestone walls.
            var d = new CqChunkDraft("Cbd_Exit", drop);
            StreetRoad(d, true);
            for (int s = -1; s <= 1; s += 2)
            {
                GlassTower(d, new CqLot(s, 0, 14, K.Edge, 16, d.Base(7)), s < 0 ? 96 : 70, s < 0 ? GlassBlue : GlassSilver, FrameLight, s < 0 ? 1 : 0);
                CqLot heritage = new CqLot(s, 14, 24, K.Edge, 14, d.Base(19));
                if (s < 0) BrickHeritage(d, heritage, 2, false); else SandstoneColonnade(d, heritage, 2);
                GardenEdge(d, s, 24, 40);
                for (float z = 27; z < 40; z += 6.5f) K.PlaneTree(d.Props, d.Collisions, new Vector3(s * 5.6f, d.Ground(z), z), .95f);
                K.Lamp(d.Props, d.Collisions, s, d.Ground(12f), 12f);
                DeepTowers(d, s, 0, 24, 2);
                DeepParkTrees(d, s, 24, 40);
            }
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft Wide(string id, float drop)
        {
            var d = new CqChunkDraft(id, drop) { Half = WideHalf };
            return d;
        }

        static CqChunkDraft Terrace(float drop)
        {
            var d = Wide("Cbd_Terrace", drop);
            K.WideStreetSurface(d.Road, d);
            float E = d.Edge, h = d.Half;
            // Left: teal tower, sandstone bank, blue tower. Right: brick, tall tower with a crown, concrete ribbon tower.
            GlassTower(d, new CqLot(-1, 0, 16, E, 16, d.Base(8)), 92, GlassTeal, FrameLight, 1);
            SandstoneColonnade(d, new CqLot(-1, 16, 24, E, 14, d.Base(20)), 3);
            GlassTower(d, new CqLot(-1, 24, 40, E, 16, d.Base(32)), 70, GlassBlue, FrameDark, 2);
            BrickHeritage(d, new CqLot(1, 0, 8, E, 14, d.Base(4)), 3, true);
            GlassTower(d, new CqLot(1, 8, 22, E, 16, d.Base(15)), 118, GlassNavy, FrameLight, 3);
            ConcreteTower(d, new CqLot(1, 22, 34, E, 16, d.Base(28)), 54);
            BrickHeritage(d, new CqLot(1, 34, 40, E, 14, d.Base(37)), 2, false);
            for (int s = -1; s <= 1; s += 2)
            {
                for (float z = s < 0 ? 6 : 1; z < 40; z += 14) K.PlaneTree(d.Props, d.Collisions, new Vector3(s * (h + .95f), d.Ground(z), z), .9f);
                K.Lamp(d.Props, d.Collisions, s, d.Ground(s < 0 ? 13f : 33f), s < 0 ? 13f : 33f, h);
                K.TrafficLight(d.Props, d.Collisions, s, d.Ground(37.5f), 37.5f, h);
                DeepTowers(d, s, 0, 40, 3);
            }
            K.PropFromKit(d.Props, d.Collisions, "BusShelter", new Vector3(h + 1.6f, d.Ground(24f), 24f), -90f);
            K.PropFromKit(d.Props, d.Collisions, "Bench", new Vector3(-(h + 3.2f), d.Ground(29f), 29f), 90f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft TownHall(float drop)
        {
            var d = Wide("Cbd_TownHall", drop);
            K.WideStreetSurface(d.Road, d);
            float E = d.Edge, h = d.Half;
            for (int x = -7; x <= 7; x++) K.Flat(d.Road, x - .27f, x + .27f, x - .27f, x + .27f, 1.2f, 4.6f, .036f, K.R.cream);
            // Left: a small paved forecourt (4 m) before the red-brick town hall with its clock tower, then sandstone.
            const float set = 4f;
            Forecourt(d, -1, 0, 26, set);
            TownHallBuilding(d, new CqLot(-1, 2, 26, E + set, 16, d.Base(14)));
            BrickHeritage(d, new CqLot(-1, 0, 2, E + set, 14, d.Base(1)), 2, false);
            SandstoneColonnade(d, new CqLot(-1, 26, 40, E, 14, d.Base(33)), 3);
            // Right: green glass tower, blue tower, heritage corner.
            GlassTower(d, new CqLot(1, 0, 16, E, 16, d.Base(8)), 104, GlassGreen, FrameLight, 0);
            GlassTower(d, new CqLot(1, 16, 30, E, 16, d.Base(23)), 78, GlassPale, FrameDark, 2);
            SandstoneColonnade(d, new CqLot(1, 30, 40, E, 14, d.Base(35)), 2);
            K.PlaneTree(d.Props, d.Collisions, new Vector3(-(E + 2.2f), d.Ground(24.5f), 24.5f), 1f);
            K.PropFromKit(d.Props, d.Collisions, "Bench", new Vector3(-(h + 3.3f), d.Ground(14f), 14f), 90f);
            for (float z = 4; z < 40; z += 12) K.PlaneTree(d.Props, d.Collisions, new Vector3(h + .95f, d.Ground(z), z), 1f);
            K.Lamp(d.Props, d.Collisions, 1, d.Ground(22f), 22f, h);
            K.Lamp(d.Props, d.Collisions, -1, d.Ground(32f), 32f, h);
            K.TrafficLight(d.Props, d.Collisions, -1, d.Ground(1f), 1f, h);
            K.TrafficLight(d.Props, d.Collisions, 1, d.Ground(1f), 1f, h);
            DeepTowers(d, -1, 0, 40, 4);
            DeepTowers(d, 1, 0, 40, 5);
            d.StandardSlots();
            return d;
        }

        // The Terrace opens from the 2-lane street to 4 lanes (Widen) and closes again before the mall (Narrow), like the
        // boulevard In / Out chunks: the kerbs angle out, a raised garden bed fills the corner up to the building line.
        static CqChunkDraft Widen(float drop, bool widen)
        {
            var d = new CqChunkDraft(widen ? "Cbd_Widen" : "Cbd_Narrow", drop)
            {
                Half = widen ? K.Half : WideHalf, EndHalf = widen ? WideHalf : K.Half, WidthChangeZ = new Vector2(8f, 32f),
            };
            K.WideStreetSurface(d.Road, d);
            float E = d.Edge;
            float cornerFrom = widen ? 0f : 32f, cornerTo = cornerFrom + 8f;
            for (int s = -1; s <= 1; s += 2)
            {
                // corner building at the street building line where the road is still narrow
                var corner = new CqLot(s, cornerFrom, cornerTo, K.Edge, 14, d.Base(cornerFrom + 4f));
                if (s < 0) BrickHeritage(d, corner, 3, true); else SandstoneColonnade(d, corner, 3);
                float from = widen ? 8f : 0f, to = widen ? 40f : 32f;
                if (s < 0)
                {
                    SandstoneColonnade(d, new CqLot(-1, from, from + 12f, E, 14, d.Base(from + 6f)), 3);
                    GlassTower(d, new CqLot(-1, from + 12f, to, E, 16, d.Base((from + 12f + to) * .5f)), widen ? 84 : 100, widen ? GlassTeal : GlassBlue, FrameLight, widen ? 0 : 1);
                }
                else
                {
                    GlassTower(d, new CqLot(1, from, from + 18f, E, 16, d.Base(from + 9f)), widen ? 108 : 76, widen ? GlassNavy : GlassSilver, FrameLight, widen ? 3 : 2);
                    ConcreteTower(d, new CqLot(1, from + 18f, to, E, 16, d.Base((from + 18f + to) * .5f)), widen ? 48 : 60);
                }
                GardenBed(d, s);
                DeepTowers(d, s, 0, 40, widen ? 12 : 13);
                float lampZ = widen ? 36f : 4f;
                K.Lamp(d.Props, d.Collisions, s, d.Ground(lampZ), lampZ, d.HalfAt(lampZ));
            }
            // like the boulevard transitions: rows where the road is wide enough for sure
            if (widen) { d.Slot(-2.6f, 5f); d.Slot(0f, 5f); d.Slot(2.6f, 5f); d.Slot(-2f, 25f); d.Slot(2f, 25f); }
            else { d.Slot(-2f, 15f); d.Slot(2f, 15f); d.Slot(-2.6f, 35f); d.Slot(0f, 35f); d.Slot(2.6f, 35f); }
            return d;
        }

        // Raised garden bed between the angled sidewalk edge and the 4-lane building line (sheared with the road).
        static void GardenBed(CqChunkDraft d, int s)
        {
            ComicMesh m = d.SlopedFrontage;
            float line = d.Edge, top = .9f, soil = .72f, wall = .3f;
            Color wallColour = Color.Lerp(Sandstone, K.R.ink, .15f), green = K.Hex("4E7F3A");
            for (float z = d.WidthChangeZ.x; z < d.WidthChangeZ.y - .01f; z += 1f)
            {
                float e0 = d.EdgeAt(z), e1 = d.EdgeAt(z + 1f);
                if (line - e0 < .05f && line - e1 < .05f) continue;
                float k0 = Mathf.Min(e0 + wall, line), k1 = Mathf.Min(e1 + wall, line);
                m.Surface = ComicSurface.Concrete;
                K.Face(m, K.V(s * e0, -.4f, z), K.V(s * e1, -.4f, z + 1f), K.V(s * e1, top, z + 1f), K.V(s * e0, top, z), wallColour, new Vector3(-s, 0, 0), false);
                K.Face(m, K.V(s * e0, top, z), K.V(s * e1, top, z + 1f), K.V(s * k1, top, z + 1f), K.V(s * k0, top, z), Trim, Vector3.up, false);
                m.Surface = ComicSurface.Foliage;
                K.Face(m, K.V(s * k0, soil, z), K.V(s * k1, soil, z + 1f), K.V(s * line, soil, z + 1f), K.V(s * line, soil, z), green, Vector3.up, false);
            }
            foreach (float at in new[] { 12f, 18f, 24f, 28f })
            {
                float z = d.EndHalf > d.Half ? at : d.Length - at;
                float inner = d.EdgeAt(z) + wall, room = line - inner;
                if (room >= 2.2f) K.PlaneTree(d.Props, null, new Vector3(s * (inner + line) * .5f, d.Ground(z) + soil - K.Kerb, z), .85f);
                else if (room >= .9f) K.PropFromKit(d.Props, null, "FlowerPot", new Vector3(s * (inner + line) * .5f, d.Ground(z) + soil - K.Kerb, z), 0f);
            }
        }

        static CqChunkDraft Mall(float drop, bool arcade)
        {
            // Hay Street Mall: no cars, grey pavers wall to wall, low granite kerbs, shopfronts under dark awnings.
            var d = new CqChunkDraft(arcade ? "Cbd_MallArcade" : "Cbd_MallShops", drop) { Type = RoadType.PedestrianMall };
            MallRoad(d);
            int k = arcade ? 0 : 3;
            for (int s = -1; s <= 1; s += 2)
            {
                float[] cuts = s < 0 ? new[] { 0f, 8f, 16f, 24f, 32f, 40f } : new[] { 0f, 10f, 18f, 28f, 40f };
                for (int i = 0; i + 1 < cuts.Length; i++)
                {
                    var lot = new CqLot(s, cuts[i], cuts[i + 1], K.Edge, 14, d.Base((cuts[i] + cuts[i + 1]) * .5f));
                    if (arcade && s < 0 && i == 2) LondonCourt(d, lot);
                    else MallShop(d, lot, MallWalls[(k + i * 2 + (s > 0 ? 1 : 0)) % MallWalls.Length], 2 + (k + i + (s > 0 ? 1 : 0)) % 3, (k + i) % 2 == 0);
                    k++;
                }
                // Plane trees and benches along the low kerbs; a planter or two against the shops.
                for (float z = arcade ? 6 : 2; z < 40; z += 12) K.PlaneTree(d.Props, d.Collisions, new Vector3(s * 4.9f, d.Ground(z), z), .9f);
                K.PropFromKit(d.Props, d.Collisions, "Bench", new Vector3(s * 5.1f, d.Ground(arcade ? 13f : 9f), arcade ? 13f : 9f), s < 0 ? 90f : -90f);
                K.PropFromKit(d.Props, d.Collisions, "Planter", new Vector3(s * 7.4f, d.Ground(arcade ? 33f : 22f), arcade ? 33f : 22f), s < 0 ? 90f : -90f);
                K.PropFromKit(d.Props, d.Collisions, "Bin", new Vector3(s * 5.0f, d.Ground(arcade ? 31f : 27f), arcade ? 31f : 27f), 0f);
                DeepTowers(d, s, 0, 40, arcade ? 6 : 7);
            }
            // Bunting-free: the mall reads from paving, awnings, trees and people. Lamps on tall posts in the centre strip
            // would block the lanes, so the mall's light poles stand at the kerbs.
            K.Lamp(d.Props, d.Collisions, -1, d.Ground(20f), 20f);
            K.Lamp(d.Props, d.Collisions, 1, d.Ground(36f), 36f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft Plaza(float drop)
        {
            // Open: the tallest towers stand back behind forecourts with cafe tables, so their whole height shows.
            var d = Wide("Cbd_Plaza", drop);
            K.WideStreetSurface(d.Road, d);
            float E = d.Edge, h = d.Half;
            const float set = 6f;
            for (int s = -1; s <= 1; s += 2)
            {
                Forecourt(d, s, 0, 40, set);
                if (s < 0)
                {
                    GlassTower(d, new CqLot(-1, 0, 8, E + set, 16, d.Base(4)), 40, GlassSilver, FrameLight, 0);
                    CentralTower(d, new CqLot(-1, 8, 30, E + set, 22, d.Base(19)));
                    GlassTower(d, new CqLot(-1, 30, 40, E + set, 16, d.Base(35)), 58, GlassBlue, FrameDark, 0);
                }
                else
                {
                    BracedTower(d, new CqLot(1, 0, 18, E + set, 18, d.Base(9)), 128);
                    GlassTower(d, new CqLot(1, 18, 30, E + set, 16, d.Base(24)), 86, GlassTeal, FrameLight, 1);
                    ConcreteTower(d, new CqLot(1, 30, 40, E + set, 16, d.Base(35)), 44);
                }
                for (float z = 6; z < 40; z += 13) K.CafeUmbrella(d.Props, d.Collisions, new Vector3(s * (E + 2.6f), d.Ground(z), z), s < 0 ? K.Hex("F4F2EA") : K.Hex("2F6B66"));
                for (float z = 0; z < 40; z += 14f) K.PlaneTree(d.Props, d.Collisions, new Vector3(s * (h + .95f), d.Ground(z + 3), z + 3), 1f);
                DeepTowers(d, s, 0, 40, 8 + s);
            }
            K.Lamp(d.Props, d.Collisions, -1, d.Ground(27f), 27f, h);
            K.Lamp(d.Props, d.Collisions, 1, d.Ground(8f), 8f, h);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CarPark(float drop)
        {
            // A multi-storey car park with its exit ramp; a loading-dock kicker in the left kerb lane of the Terrace.
            var d = Wide("Cbd_CarPark", drop);
            K.WideStreetSurface(d.Road, d);
            float E = d.Edge, h = d.Half;
            CarParkBuilding(d, new CqLot(-1, 0, 26, E, 18, d.Base(13)));
            BrickHeritage(d, new CqLot(-1, 26, 40, E, 14, d.Base(33)), 3, true);
            GlassTower(d, new CqLot(1, 0, 14, E, 16, d.Base(7)), 88, GlassBlue, FrameLight, 3);
            SandstoneColonnade(d, new CqLot(1, 14, 24, E, 14, d.Base(19)), 2);
            GlassTower(d, new CqLot(1, 24, 40, E, 16, d.Base(32)), 66, GlassSilver, FrameDark, 0);
            // Kicker: 3.2 m run-up to 0.9 m, 3.2 m wide in the left kerb lane (x = -6, a 4 m lane) at z = 12.
            var ramp = new CqRampSpec { Name = "LoadingDockKicker", X = -6f, Z = 12f, Width = 3.2f, Rise = 3.2f, Height = .9f, Top = 0f, Launch = 1.15f, Hang = .55f };
            d.Ramps.Add(ramp);
            var deck = new ComicMesh();
            K.RampMesh(deck, ramp, K.Hex("6B7078"), K.Hex("50555C"), true, 0f);
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            for (int s = -1; s <= 1; s += 2)
            {
                for (float z = s < 0 ? 31 : 3; z < 40; z += 10) K.PlaneTree(d.Props, d.Collisions, new Vector3(s * (h + .95f), d.Ground(z), z), 1f);
                DeepTowers(d, s, 0, 40, 10 + s);
            }
            K.Lamp(d.Props, d.Collisions, 1, d.Ground(18f), 18f, h);
            K.PropFromKit(d.Props, d.Collisions, "ParkingMeter", new Vector3(-(h + .5f), d.Ground(22f), 22f), 90f);
            // The four lane centres like the other Terrace chunks, but nothing in the kicker's lane (before it or where
            // riders come down).
            foreach (float z in new[] { 10f, 25f })
                foreach (float x in new[] { -2f, 2f, 6f }) d.Slot(x, z);
            return d;
        }

        // ================================================================ road surfaces

        static void StreetRoad(CqChunkDraft d, bool crossing)
        {
            ComicMesh m = d.Road;
            K.StreetSurface(m, d.Length, K.R.asphalt, K.R.pavement, K.R.cream, true);
            if (crossing)
                for (int x = -3; x <= 3; x++) K.Flat(m, x - .27f, x + .27f, x - .27f, x + .27f, 1.2f, 4.6f, .036f, K.R.cream);
        }

        static void MallRoad(CqChunkDraft d)
        {
            ComicMesh m = d.Road;
            K.StreetSurface(m, d.Length, MallPaver, MallPaverLight, Granite, false);
            Color joint = Color.Lerp(MallPaver, K.R.ink, .35f), band = Color.Lerp(MallPaver, K.Hex("B04A3A"), .55f);
            // Brick-bond joints across the centre paving and darker red-brown bands every 4 m.
            for (int z = 0; z < 40; z++)
                K.Flat(m, -3.9f, 3.9f, -3.9f, 3.9f, z - .0125f, z + .0125f, .021f, joint);
            for (int z = 0; z < 40; z++)
                for (float x = -3.5f + (z % 2) * .5f; x < 3.9f; x += 1f)
                    K.Flat(m, x - .0125f, x + .0125f, x - .0125f, x + .0125f, z, z + 1f, .022f, joint);
            for (int z = 2; z < 40; z += 4) K.Flat(m, -3.8f, 3.8f, -3.8f, 3.8f, z - .25f, z + .25f, .024f, band);
        }

        // ================================================================ buildings

        static void Foundation(CqChunkDraft d, CqLot l)
        {
            d.Frontage.Surface = ComicSurface.Concrete;
            K.LBox(d.Frontage, l, 0, l.W, -2.6f, 0f, .02f, -l.Depth, K.R.ink);
            d.Wall(l, 30f);
        }

        // Glass tower: lobby podium, up to three setback tiers with floor bands and fins, a crown.
        // crown 0 flat parapet + plant room, 1 mast, 2 stepped crown, 3 open crown frame.
        internal static void GlassTower(CqChunkDraft d, CqLot l, float h, Color glass, Color frame, int crown)
        {
            ComicMesh m = d.Frontage;
            Foundation(d, l);
            float W = l.W, D = l.Depth;
            m.Surface = ComicSurface.Glass;
            K.LBox(m, l, 0, W, 0, 6.5f, -.6f, -D, K.Hex("1E2A36"));                        // dark lobby glass, set in
            m.Surface = ComicSurface.Concrete;
            for (float a = 0; a <= W + .01f; a += Mathf.Max(2.5f, W / Mathf.Max(1, Mathf.Round(W / 4f))))
                K.LBox(m, l, Mathf.Clamp(a - .3f, 0, W - .6f), Mathf.Clamp(a + .3f, .6f, W), 0, 6.5f, 0, -.8f, frame); // podium columns
            K.LBox(m, l, 0, W, 6.5f, 7.3f, .2f, -D, frame);                                      // podium cornice
            K.LBox(m, l, W * .3f, W * .7f, 3.4f, 3.7f, 2.2f, 0, frame);                          // entrance canopy
            int tiers = h > 95 ? 3 : h > 55 ? 2 : 1;
            float y0 = 7.3f;
            for (int t = 0; t < tiers; t++)
            {
                float y1 = t == tiers - 1 ? h : Mathf.Round((h * (t == 0 ? .58f : .82f)) / 3.6f) * 3.6f;
                float inset = .8f + t * 1.6f;
                float a0 = inset, a1 = W - inset, o0 = -inset + .6f, o1 = -D + inset;
                if (a1 - a0 < 3f) break;
                m.Surface = ComicSurface.Glass;
                K.LBox(m, l, a0, a1, y0, y1, o0, o1, glass);
                m.Surface = ComicSurface.PaintedMetal;
                for (float y = y0 + 3.6f; y < y1 - .5f; y += 3.6f)
                    K.LBox(m, l, a0 - .06f, a1 + .06f, y - .16f, y + .16f, o0 + .06f, o1 - .06f, frame);
                float span = a1 - a0; int fins = Mathf.Max(2, Mathf.RoundToInt(span / 3f));
                for (int i = 0; i <= fins; i++)
                {
                    float a = a0 + span * i / fins;
                    K.LBox(m, l, a - .09f, a + .09f, y0, y1, o0 + .14f, o0 - .02f, frame);
                }
                // the face towards the rider (-z) gets fins too: it is what you see coming down the terrace
                float depth = o0 - o1; int backFins = Mathf.Max(2, Mathf.RoundToInt(depth / 3f));
                for (int i = 1; i < backFins; i++)
                {
                    float o = o0 - depth * i / backFins;
                    K.LBox(m, l, a0 - .14f, a0 + .02f, y0, y1, o + .09f, o - .09f, frame);
                }
                K.LBox(m, l, a0 - .15f, a1 + .15f, y1 - .1f, y1 + .5f, o0 + .15f, o1 - .15f, frame);   // tier parapet
                y0 = y1 + .5f;
            }
            float topInset = .8f + (tiers - 1) * 1.6f, ta0 = topInset + 1.5f, ta1 = W - topInset - 1.5f, to0 = -topInset - .9f, to1 = -D + topInset + 1.5f;
            float top = y0;
            switch (crown)
            {
                case 1:
                    K.LBox(m, l, ta0, ta1, top, top + 3f, to0, to1, frame);
                    m.Cylinder(l.P((ta0 + ta1) * .5f, top + 3f + 9f, (to0 + to1) * .5f), .22f, 18f, K.R.ink, 6, .4f);
                    K.LBox(m, l, (ta0 + ta1) * .5f - .5f, (ta0 + ta1) * .5f + .5f, top + 8f, top + 8.6f, (to0 + to1) * .5f + .5f, (to0 + to1) * .5f - .5f, K.R.red);
                    break;
                case 2:
                    for (int i = 0; i < 3; i++)
                    {
                        float shrink = i * 1.4f;
                        m.Surface = ComicSurface.Glass;
                        K.LBox(m, l, ta0 - 1f + shrink, ta1 + 1f - shrink, top + i * 3.2f, top + (i + 1) * 3.2f, to0 + 1f - shrink, to1 - 1f + shrink, Color.Lerp(glass, frame, .25f + i * .2f));
                    }
                    break;
                case 3:
                    m.Surface = ComicSurface.PaintedMetal;
                    float fy0 = top, fy1 = top + 7f;
                    foreach (float a in new[] { ta0 - 1f, ta1 + 1f })
                        K.LBox(m, l, a - .25f, a + .25f, fy0, fy1, to0 + 1f, to1 - 1f, frame);
                    foreach (float o in new[] { to0 + 1f, to1 - 1f })
                        K.LBox(m, l, ta0 - 1f, ta1 + 1f, fy0, fy1, o + .25f, o - .25f, frame);
                    K.LBox(m, l, ta0 - 1.2f, ta1 + 1.2f, fy1 - .5f, fy1, to0 + 1.2f, to1 - 1.2f, frame);
                    break;
                default:
                    K.LBox(m, l, ta0, ta0 + (ta1 - ta0) * .45f, top, top + 2.4f, to0, to1 * .6f, K.R.ink);
                    break;
            }
        }

        // Cream concrete tower with dark ribbon windows wrapping round (the 1970s/80s Terrace offices).
        internal static void ConcreteTower(CqChunkDraft d, CqLot l, float h)
        {
            ComicMesh m = d.Frontage;
            Foundation(d, l);
            float W = l.W, D = l.Depth;
            m.Surface = ComicSurface.Glass;
            K.LBox(m, l, 0, W, 0, 5.5f, -1f, -D, K.Hex("1E2A36"));
            m.Surface = ComicSurface.Concrete;
            for (float a = .4f; a < W; a += 3f) K.LBox(m, l, a - .35f, a + .35f, 0, 5.5f, 0, -1f, Concrete);
            K.LBox(m, l, .3f, W - .3f, 5.5f, h, -.2f, -D + .3f, Concrete);
            m.Surface = ComicSurface.Glass;
            for (float y = 7f; y < h - 1.5f; y += 3.4f) K.LBox(m, l, .22f, W - .22f, y, y + 1.6f, -.12f, -D + .22f, RibbonDark);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, .1f, W - .1f, h, h + .6f, 0, -D + .1f, Concrete);
            K.LBox(m, l, W * .25f, W * .75f, h + .6f, h + 3f, -2f, -D + 3f, K.Hex("C9C3B4"));
        }

        // The tallest: a Central Park-style tower, pale silver glass with dark frames, stepped top and an antenna.
        static void CentralTower(CqChunkDraft d, CqLot l)
        {
            GlassTower(d, l, 150f, GlassSilver, FrameDark, 2);
            ComicMesh m = d.Frontage;
            float W = l.W;
            m.Cylinder(l.P(W * .5f, 150f + 9.6f + 11f, -l.Depth * .5f), .3f, 22f, K.R.ink, 6, .3f);
            // a red aircraft light near the top
            m.Cylinder(l.P(W * .5f, 150f + 9.6f + 20f, -l.Depth * .5f), .45f, .6f, K.R.red, 6);
        }

        // Brookfield-style: dark glass with big white diagonal braces on the road face.
        static void BracedTower(CqChunkDraft d, CqLot l, float h)
        {
            GlassTower(d, l, h, GlassNavy, FrameDark, 0);
            ComicMesh m = d.Frontage;
            float W = l.W, a0 = .8f, a1 = W - .8f, o = .65f;
            m.Surface = ComicSurface.PaintedMetal;
            float storey = 22f;
            for (float y = 8f; y + storey <= h * .58f + .1f; y += storey)
            {
                m.Beam(l.P(a0, y, o), l.P(a1, y + storey, o), .55f, FrameLight);
                m.Beam(l.P(a1, y, o), l.P(a0, y + storey, o), .55f, FrameLight);
                K.LBox(m, l, a0, a1, y - .3f, y + .3f, o + .3f, o - .3f, FrameLight);
            }
        }

        // Two to four storeys of red brick with cream string courses, arched windows and a parapet.
        static void BrickHeritage(CqChunkDraft d, CqLot l, int floors, bool gable)
        {
            ComicMesh m = d.Frontage;
            Foundation(d, l);
            float W = l.W, D = l.Depth, fh = 4f, h = floors * fh;
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, 0, W, 0, h, 0, -D, Brick);
            m.Surface = ComicSurface.Concrete;
            for (int f = 1; f <= floors; f++) K.LBox(m, l, -.05f, W + .05f, f * fh - .2f, f * fh + .1f, .15f, -D - .05f, Trim);
            K.LBox(m, l, -.2f, W + .2f, h, h + .45f, .45f, -D - .1f, Trim);                      // cornice
            for (int f = 0; f < floors; f++)
                for (float a = 1.2f; a < W - .6f; a += 2.2f)
                {
                    bool door = f == 0 && Mathf.Abs(a - W * .5f) < 1.2f;
                    float wy0 = f * fh + (f == 0 ? (door ? 0 : .9f) : 1f), wy1 = f * fh + (f == 0 ? 3.1f : 3.2f);
                    K.LQuad(m, l, a - .55f, a + .55f, wy0, wy1, .02f, K.R.ink);
                    K.LQuad(m, l, a - .43f, a + .43f, wy0 + .1f, wy1 - .25f, .04f, door ? Timber : K.R.glass);
                    K.LBox(m, l, a - .62f, a + .62f, wy1 - .02f, wy1 + .32f, .12f, 0, Trim);     // arch / lintel
                    K.LBox(m, l, a - .2f, a + .2f, wy1 + .2f, wy1 + .45f, .16f, 0, Trim);        // keystone
                }
            if (gable)
            {
                // a Dutch gable over the middle
                float c = W * .5f, gw = Mathf.Min(4.4f, W * .6f);
                m.Surface = ComicSurface.Brick;
                K.LBox(m, l, c - gw * .5f, c + gw * .5f, h + .45f, h + 2.2f, .05f, -1.2f, Brick);
                K.LBox(m, l, c - gw * .3f, c + gw * .3f, h + 2.2f, h + 3.3f, .05f, -1.2f, Brick);
                m.Surface = ComicSurface.Concrete;
                K.LBox(m, l, c - gw * .35f, c + gw * .35f, h + 3.3f, h + 3.6f, .15f, -1.3f, Trim);
                K.LBox(m, l, c - gw * .55f, c + gw * .55f, h + 2.1f, h + 2.3f, .15f, -1.3f, Trim);
            }
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, 1f, W - 1f, h + .45f, h + 1.6f, -2f, -D + 1f, Slate);                    // roof behind the parapet
        }

        // Sandstone with a ground-floor colonnade, a heavy cornice and a slate mansard (Old Treasury / GPO family).
        static void SandstoneColonnade(CqChunkDraft d, CqLot l, int floors)
        {
            ComicMesh m = d.Frontage;
            Foundation(d, l);
            float W = l.W, D = l.Depth, fh = 4.2f, h = floors * fh;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, 0, h, -1.6f, -D, Sandstone);                                      // walls behind the colonnade
            K.LBox(m, l, 0, W, fh, h, 0, -1.6f, Sandstone);                                      // upper floors over it
            K.LQuad(m, l, .2f, W - .2f, 0f, fh - .4f, -1.55f, Color.Lerp(SandDark, K.R.ink, .55f)); // shadowed arcade wall
            int cols = Mathf.Max(2, Mathf.RoundToInt(W / 2.6f));
            for (int i = 0; i <= cols; i++)
            {
                float a = Mathf.Lerp(.35f, W - .35f, (float)i / cols);
                m.Cylinder(l.P(a, fh * .5f - .1f, -.35f), .32f, fh - .2f, Trim, 8);
                K.LBox(m, l, a - .42f, a + .42f, fh - .45f, fh - .15f, .1f, -.8f, Trim);            // capital
            }
            K.LBox(m, l, -.05f, W + .05f, fh - .2f, fh + .35f, .2f, -D, SandDark);                 // entablature
            for (int f = 1; f < floors; f++)
                for (float a = 1.1f; a < W - .5f; a += 2.2f)
                {
                    float wy0 = f * fh + .9f, wy1 = f * fh + 3.2f;
                    K.LQuad(m, l, a - .5f, a + .5f, wy0, wy1, .02f, K.R.ink);
                    K.LQuad(m, l, a - .4f, a + .4f, wy0 + .1f, wy1 - .1f, .04f, K.R.glass);
                    K.LBox(m, l, a - .65f, a + .65f, wy1 + .05f, wy1 + .4f, .18f, 0, Trim);         // pediment hood
                    K.LBox(m, l, a - .6f, a + .6f, wy0 - .2f, wy0, .14f, 0, Trim);                  // sill
                }
            K.LBox(m, l, -.3f, W + .3f, h, h + .6f, .55f, -D - .1f, SandDark);                    // cornice
            K.LBox(m, l, -.1f, W + .1f, h + .6f, h + 1.2f, .25f, -D, Sandstone);                   // balustrade band
            m.Surface = ComicSurface.PaintedMetal;
            K.LSlope(m, l, .4f, W - .4f, h + 1.2f, h + 4f, -.6f, -3.4f, -D + .8f, Slate);          // mansard
            for (float a = 2f; a < W - 1f; a += 3.5f)
                K.LBox(m, l, a - .5f, a + .5f, h + 1.6f, h + 3f, -.9f, -2.2f, Trim);                 // dormers
        }

        // Red-brick town hall with white trim and a clock tower (stylised Perth Town Hall).
        static void TownHallBuilding(CqChunkDraft d, CqLot l)
        {
            ComicMesh m = d.Frontage;
            Foundation(d, l);
            float W = l.W, D = l.Depth, fh = 4.4f;
            int floors = 3; float h = floors * fh;
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, 0, W, 0, h, 0, -D, Brick);
            m.Surface = ComicSurface.Concrete;
            for (int f = 1; f <= floors; f++) K.LBox(m, l, -.05f, W + .05f, f * fh - .25f, f * fh + .1f, .18f, -D - .05f, Trim);
            K.LBox(m, l, -.05f, W + .05f, 0, .9f, .1f, -D - .05f, Trim);                         // plinth
            for (int f = 0; f < floors; f++)
                for (float a = 7.2f; a < W - .8f; a += 2.4f)
                {
                    float wy0 = f * fh + (f == 0 ? 1.1f : 1f), wy1 = f * fh + 3.4f;
                    K.LQuad(m, l, a - .55f, a + .55f, wy0, wy1, .02f, K.R.ink);
                    K.LQuad(m, l, a - .42f, a + .42f, wy0 + .1f, wy1 - .3f, .04f, K.R.glass);
                    K.LBox(m, l, a - .7f, a + .7f, wy1 - .05f, wy1 + .35f, .14f, 0, Trim);
                    K.LBox(m, l, a - .12f, a + .12f, wy0, wy1 - .3f, .07f, 0, Trim);               // mullion
                }
            // steep slate roof with a cream ridge
            m.Surface = ComicSurface.PaintedMetal;
            K.LSlope(m, l, 6.5f, W - .3f, h + .1f, h + 4.5f, .1f, -D * .5f, -D + .1f, Slate);
            // corner clock tower (on the near end, stepping out into the forecourt, so the rider sees it coming)
            float t0 = .3f, t1 = 6.5f, to0 = 3.6f, to1 = -2.6f, th = 30f;
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, t0, t1, 0, th, to0, to1, Brick);
            m.Surface = ComicSurface.Concrete;
            foreach (float y in new[] { 4.2f, 8.6f, 13f, 18.5f, 24f }) K.LBox(m, l, t0 - .12f, t1 + .12f, y - .25f, y + .2f, to0 + .14f, to1 - .14f, Trim);
            for (int i = 0; i < 4; i++) // corner quoins
            {
                float a = i % 2 == 0 ? t0 - .05f : t1 - .5f, o = i < 2 ? to0 + .06f : to1 + .5f;
                K.LBox(m, l, a, a + .55f, 0, th, o, o - .56f, Trim);
            }
            // arched belfry openings on the road face and the face towards the rider
            float bc = (t0 + t1) * .5f, oc = (to0 + to1) * .5f;
            K.LQuad(m, l, bc - 1.6f, bc - .3f, 19.4f, 23.2f, to0 + .02f, K.R.ink);
            K.LQuad(m, l, bc + .3f, bc + 1.6f, 19.4f, 23.2f, to0 + .02f, K.R.ink);
            K.LBox(m, l, t0 - .02f, t0, 19.4f, 23.2f, oc + 1.6f, oc + .3f, K.R.ink);
            K.LBox(m, l, t0 - .02f, t0, 19.4f, 23.2f, oc - .3f, oc - 1.6f, K.R.ink);
            // clock faces: road side and rider side
            K.Clock(m, l.P(bc, 15.8f, to0 + .05f), Quaternion.Euler(0, l.S < 0 ? 90 : -90, 0), 1.35f, Trim, K.R.ink);
            K.Clock(m, l.P(t0 - .05f, 15.8f, oc), Quaternion.Euler(0, 180, 0), 1.35f, Trim, K.R.ink);
            // spire: steep slate pyramid with four cream pinnacles and a flag pole
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(l.P(bc, th + 4.5f, oc), 3.6f, 9f, Slate, 4, .04f, Quaternion.Euler(0, 45, 0));
            foreach (float a in new[] { t0 + .2f, t1 - .2f })
                foreach (float o in new[] { to0 - .2f, to1 + .2f })
                    m.Cylinder(l.P(a, th + 1.2f, o), .3f, 2.4f, Trim, 6, .2f);
            m.Cylinder(l.P(bc, th + 11f, oc), .07f, 4f, K.R.ink, 6);
            m.Box(l.P(bc, th + 12.2f, oc + l.S * .0f) + new Vector3(0, 0, .7f), new Vector3(.05f, .9f, 1.3f), K.R.red);
        }

        // London Court-style mock-Tudor arcade entrance: white render, dark timber framing, a gable and an arch.
        static void LondonCourt(CqChunkDraft d, CqLot l)
        {
            ComicMesh m = d.Frontage;
            Foundation(d, l);
            float W = l.W, D = l.Depth, h = 12.5f, c = W * .5f;
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, W, 0, 3.8f, 0, -D, Render);
            K.LBox(m, l, -.1f, W + .1f, 3.8f, h, .45f, -D, Render);                              // jettied upper floors
            m.Surface = ComicSurface.Wood;
            K.LBox(m, l, -.15f, W + .15f, 3.7f, 4.05f, .6f, -D, Timber);
            for (float a = 0f; a <= W + .01f; a += W / 6f) K.LBox(m, l, a - .14f, a + .14f, 4f, h, .55f, .4f, Timber);
            foreach (float y in new[] { 7.2f, 10.2f }) K.LBox(m, l, -.12f, W + .12f, y - .12f, y + .12f, .56f, .4f, Timber);
            for (int i = 0; i < 6; i++)
            {
                float a0 = i * W / 6f, a1 = (i + 1) * W / 6f;
                bool rising = i % 2 == 0;
                m.Beam(l.P(a0, rising ? 4.1f : 7.1f, .5f), l.P(a1, rising ? 7.1f : 4.1f, .5f), .16f, Timber);
            }
            // leaded windows on the top floor
            for (int i = 0; i < 6; i++)
            {
                float a = (i + .5f) * W / 6f;
                K.LQuad(m, l, a - .4f, a + .4f, 7.7f, 9.7f, .58f, K.R.ink);
                K.LQuad(m, l, a - .32f, a + .32f, 7.8f, 9.6f, .6f, K.Hex("C9B36A"));
            }
            // gable facing the street
            m.Surface = ComicSurface.Plaster;
            K.LGable(m, l, .2f, W - .2f, h, h + 4.2f, .5f, -3f, Render);
            m.Surface = ComicSurface.Wood;
            m.Beam(l.P(.2f, h, .62f), l.P(c, h + 4.2f, .62f), .28f, Timber);
            m.Beam(l.P(W - .2f, h, .62f), l.P(c, h + 4.2f, .62f), .28f, Timber);
            K.LBox(m, l, c - .12f, c + .12f, h, h + 4f, .62f, .5f, Timber);
            m.Surface = ComicSurface.PaintedMetal;
            K.LSlope(m, l, 0f, W, h, h + 4.2f, .4f, -3f, -D, Slate);
            // the arcade arch with a clock above it, shop windows either side
            K.LQuad(m, l, c - 1.7f, c + 1.7f, 0f, 3.5f, .03f, K.Hex("2A1E16"));
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, c - 2.1f, c - 1.7f, 0, 3.5f, .25f, 0, Trim);
            K.LBox(m, l, c + 1.7f, c + 2.1f, 0, 3.5f, .25f, 0, Trim);
            K.LBox(m, l, c - 2.1f, c + 2.1f, 3.3f, 3.7f, .3f, 0, Trim);
            K.Clock(m, l.P(c, 5.6f, .7f), Quaternion.Euler(0, l.S < 0 ? 90 : -90, 0), .85f, K.Hex("F5E7B0"), K.R.ink);
            foreach (float a in new[] { 1.2f, W - 1.2f })
            {
                K.LQuad(m, l, a - .9f, a + .9f, .5f, 3f, .03f, K.R.ink);
                K.LQuad(m, l, a - .8f, a + .8f, .6f, 2.9f, .05f, K.R.glass);
            }
        }

        // Hay Street Mall shopfront: pale heritage facade, dark awning over the sidewalk, plain coloured sign band.
        static void MallShop(CqChunkDraft d, CqLot l, Color wall, int floors, bool pediment)
        {
            ComicMesh m = d.Frontage;
            Foundation(d, l);
            float W = l.W, D = l.Depth, gh = 4.2f, fh = 3.6f, h = gh + (floors - 1) * fh;
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, W, 0, h, 0, -D, wall);
            // dark shopfront glass, display windows, door
            K.LQuad(m, l, .3f, W - .3f, .2f, 3.2f, .02f, K.R.ink);
            K.LQuad(m, l, .45f, W - .45f, .3f, 3.05f, .04f, K.Hex("33505E"));
            m.Surface = ComicSurface.PaintedMetal;
            for (float a = 2f; a < W - .5f; a += 2f) K.LBox(m, l, a - .05f, a + .05f, .3f, 3.05f, .08f, 0, K.R.ink);
            // awning: flat dark canopy out over the sidewalk (2.4 m), on hidden ties
            K.LBox(m, l, -.02f, W + .02f, 3.35f, 3.6f, 2.5f, 0, Awning);
            K.LBox(m, l, -.02f, W + .02f, 3.2f, 3.35f, 2.55f, 2.35f, Color.Lerp(wall, K.R.ink, .2f));
            // sign band on the awning fascia: plain colour, no letters
            Color sign = new[] { K.R.red, K.R.yellow, K.Hex("2F6B66"), K.Hex("2E5E9E"), K.R.cream }[Mathf.Abs(ComicKitGeometry.StableHash(d.Id + l.Z0 + l.S)) % 5];
            K.LBox(m, l, W * .2f, W * .8f, 3.86f, 4.2f, .14f, 0, sign);
            // upper floors: tall sash windows with cream surrounds
            m.Surface = ComicSurface.Concrete;
            for (int f = 1; f < floors; f++)
            {
                float y = gh + (f - 1) * fh;
                for (float a = 1.2f; a < W - .6f; a += 2f)
                {
                    K.LQuad(m, l, a - .48f, a + .48f, y + .6f, y + 2.9f, .02f, K.R.ink);
                    K.LQuad(m, l, a - .38f, a + .38f, y + .7f, y + 2.8f, .04f, K.R.glass);
                    K.LBox(m, l, a - .6f, a + .6f, y + 2.9f, y + 3.15f, .12f, 0, K.R.cream);
                }
                K.LBox(m, l, -.03f, W + .03f, y - .1f, y + .12f, .12f, -D, K.R.cream);
            }
            K.LBox(m, l, -.15f, W + .15f, h, h + .45f, .35f, -D, K.R.cream);                    // cornice
            K.LBox(m, l, 0, W, h + .45f, h + 1.3f, .1f, -.5f, wall);                              // parapet
            if (pediment) K.LGable(m, l, W * .25f, W * .75f, h + 1.3f, h + 2.8f, .1f, -.4f, K.R.cream);
            else K.LBox(m, l, W * .35f, W * .65f, h + 1.3f, h + 2f, .12f, -.4f, K.R.cream);
        }

        static void CarParkBuilding(CqChunkDraft d, CqLot l)
        {
            ComicMesh m = d.Frontage;
            Foundation(d, l);
            float W = l.W, D = l.Depth, fh = 3f;
            int floors = 6; float h = floors * fh;
            Color slab = K.Hex("A9ADB1"), dark = K.Hex("2C3238");
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, .2f, W - .2f, 0, h, -.9f, -D, dark);                                     // shadowed decks inside
            for (int f = 0; f <= floors; f++)
            {
                float y = f * fh;
                K.LBox(m, l, 0, W, y, y + .45f, 0, -D, slab);                                     // slab edges
                if (f > 0) K.LBox(m, l, 0, W, y + .45f, y + 1.15f, .05f, -.3f, f % 2 == 0 ? K.R.cream : slab); // parapet
            }
            for (float a = 0; a <= W + .01f; a += 6.5f) K.LBox(m, l, Mathf.Max(0, a - .35f), Mathf.Min(W, a + .35f), 0, h, .1f, -D, slab);
            // yellow and black chevrons on the exit portal and an "up" arrow sign shape
            float p0 = W - 7f, p1 = W - 1.5f;
            K.LQuad(m, l, p0, p1, 0f, 2.6f, .2f, K.R.ink);
            for (int i = 0; i < 8; i++)
            {
                float a = p0 + (p1 - p0) * i / 8f;
                K.LQuad(m, l, a, a + (p1 - p0) / 16f, 2.6f, 3f, .21f, i % 2 == 0 ? K.R.yellow : K.R.ink);
            }
            // the exit ramp coming down the side, cut into the sidewalk line
            m.Surface = ComicSurface.Concrete;
            m.Beam(l.P(1f, 6.2f, -1.5f), l.P(9f, .5f, -1.5f), 1.2f, slab);
            // a big round "P" sign: blue disc with a white bar shape (not a letter)
            m.Cylinder(l.P(W * .45f, 13.5f, .45f), 1.3f, .2f, K.Hex("2E5E9E"), 12, 1f, Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, 0, 0));
            K.LBox(m, l, W * .45f - .2f, W * .45f + .2f, 12.7f, 14.3f, .62f, .5f, K.R.cream);
            K.LBox(m, l, W * .45f - .2f, W * .45f + .55f, 13.9f, 14.3f, .62f, .5f, K.R.cream);
            K.LBox(m, l, W * .45f + .35f, W * .45f + .55f, 13.3f, 14.3f, .62f, .5f, K.R.cream);
            K.LBox(m, l, W * .45f - .2f, W * .45f + .55f, 13.2f, 13.5f, .62f, .5f, K.R.cream);
        }

        // Paved forecourt between the sidewalk and a set-back building line (sheared with the road).
        internal static void Forecourt(CqChunkDraft d, int s, float z0, float z1, float depth)
        {
            var m = new ComicMesh();
            float x0 = s * d.Edge, x1 = s * (d.Edge + depth);
            Color joint = Color.Lerp(K.R.ink, K.R.pavement, .5f);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                m.Surface = ComicSurface.Paving;
                K.Face(m, K.V(x0, K.Kerb, z), K.V(x0, K.Kerb, e), K.V(x1, K.Kerb, e), K.V(x1, K.Kerb, z), Color.Lerp(K.R.pavement, K.R.cream, .25f), Vector3.up, false);
                K.Face(m, K.V(x1, -2f, z), K.V(x1, -2f, e), K.V(x1, K.Kerb, e), K.V(x1, K.Kerb, z), K.R.pavement, new Vector3(s, 0, 0), false);
                K.Flat(m, x0, x1, x0, x1, z - .02f, z + .02f, K.Kerb + .02f, joint);
            }
            m.Surface = ComicSurface.Concrete;
            // a low step at the sidewalk edge, with gaps
            for (float z = z0 + 1f; z < z1 - 1f; z += 6f)
                m.Box(new Vector3(s * (d.Edge + .3f), K.Kerb + .2f, Mathf.Min(z + 2f, z1 - 1f)), new Vector3(.45f, .4f, 4f), K.R.cream);
            m.Shear(d.Grade);
            d.Road.Add(m, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        // Calm end of an entry/exit chunk: low sandstone wall, hedge and lawn (sheared with the sidewalk), park trees behind.
        internal static void GardenEdge(CqChunkDraft d, int s, float z0, float z1, float lawn = 50f)
        {
            ComicMesh m = d.SlopedFrontage;
            float k = K.Kerb;
            m.Surface = ComicSurface.Concrete;
            K.SideBox(m, s, z0, z1, -2.4f, k + .75f, .05f, .5f, Sandstone);
            K.SideBox(m, s, z0, z1, k + .75f, k + .9f, -.02f, .57f, Trim);
            m.Surface = ComicSurface.Foliage;
            K.SideBox(m, s, z0 + .4f, z1 - .4f, k + .4f, k + 1.5f, .7f, 1.7f, K.Hex("4E7F3A"));
            K.SideBox(m, s, z0, z1, -2.4f, k + .3f, .5f, lawn, K.Hex("6E9A45"));
            d.Walls.Add(new Bounds(new Vector3(s * (K.Edge + 7f), d.Y((z0 + z1) * .5f) + 1f, (z0 + z1) * .5f), new Vector3(13.5f, 4f, z1 - z0)));
        }

        static void DeepParkTrees(CqChunkDraft d, int s, float z0, float z1)
        {
            for (float z = z0 + 3f; z < z1; z += 6f)
                for (int row = 0; row < 3; row++)
                    K.PlaneTree(d.Deep, null, new Vector3(s * (K.Edge + 6f + row * 9f + (z + row * 2f) % 3f), d.Ground(z) + .3f, z + row * 2.5f - 2f), 1.1f + row * .15f, true);
        }

        // Second and third rows of towers (no outline, no shadows), like the street chunks' deep city.
        internal static void DeepTowers(CqChunkDraft d, int s, float z0, float z1, int seed)
        {
            ComicMesh m = d.Deep;
            m.Surface = ComicSurface.Glass;
            Color[] glass = { GlassBlue, GlassTeal, GlassPale, GlassNavy, GlassSilver, Concrete, GlassGreen };
            for (float z = z0; z < z1 - 1f; z += 10f)
            {
                int k = Mathf.Abs(seed * 7 + (int)z * 3 + s * 5);
                float zc = Mathf.Min(z + 5f, z1 - 2f), len = Mathf.Min(10f, z1 - z) - .4f;
                for (int layer = 0; layer < 2; layer++)
                {
                    float h = 30f + (k * 13 + layer * 29) % 70;
                    float x = s * (d.Edge + 22f + layer * 20f + (k % 3) * 2f), w = 14f;
                    Color c = Color.Lerp(glass[(k + layer) % glass.Length], new Color(.49f, .64f, .75f), .15f + layer * .2f);
                    float y = d.Ground(zc) - 2f;
                    m.Box(new Vector3(x, y + h * .5f, zc), new Vector3(w, h, len), c);
                    Color band = Color.Lerp(c, K.R.ink, .35f);
                    for (float by = 6f; by < h - 2f; by += 4.5f) m.Box(new Vector3(x - s * .02f, y + by, zc), new Vector3(w + .08f, .35f, len + .08f), band);
                    m.Box(new Vector3(x, y + h + .4f, zc), new Vector3(w * .6f, .8f, len * .6f), band);
                    k += 3;
                }
            }
        }

        // Far scenery: a cluster of towers either side that follows the rider (no colliders, no outline).
        static ComicMesh FarTowers()
        {
            var m = new ComicMesh { Surface = ComicSurface.Glass };
            Color[] glass = { GlassBlue, GlassTeal, GlassPale, GlassNavy, GlassSilver, Concrete };
            var rng = new System.Random(5050);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 9; i++)
                {
                    float z = 70f + i * 17f + (float)rng.NextDouble() * 6f, x = s * (64f + (float)rng.NextDouble() * 50f);
                    float h = 50f + (float)rng.NextDouble() * 90f, w = 14f + (float)rng.NextDouble() * 8f;
                    Color c = Color.Lerp(glass[rng.Next(glass.Length)], new Color(.55f, .68f, .80f), .35f);
                    m.Box(new Vector3(x, h * .5f - 20f, z), new Vector3(w, h, w), c);
                    for (float y = -14f; y < h - 22f; y += 6f) m.Box(new Vector3(x, y, z), new Vector3(w + .1f, .5f, w + .1f), Color.Lerp(c, K.R.ink, .3f));
                }
            return m;
        }

        // ================================================================ obstacle and vehicle meshes

        static ComicMesh PlanterMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Box(new Vector3(0, .05f, 0), new Vector3(1.5f, .1f, 1.5f), K.R.ink);
            m.Surface = ComicSurface.Concrete;
            m.Box(new Vector3(0, .38f, 0), new Vector3(1.38f, .56f, 1.38f), Granite);
            m.Box(new Vector3(0, .7f, 0), new Vector3(1.5f, .1f, 1.5f), K.R.cream);
            K.Plant(m, new Vector3(-.3f, .95f, .15f), .85f);
            K.Plant(m, new Vector3(.3f, .92f, -.2f), .75f, K.Hex("4F7F38"));
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(new Vector3(.25f, 1.1f, .3f), new Vector3(.4f, .3f, .4f), K.Hex("E05A8A"));
            return m;
        }

        // Mall kiosk: ink base, teal body with cream corner posts, framed glass, counter, striped awning, red roof.
        static ComicMesh KioskMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            Color body = K.Hex("2F6B66");
            m.Box(new Vector3(0, .08f, 0), new Vector3(1.9f, .16f, 1.9f), K.R.ink);
            m.Box(new Vector3(0, 1.1f, 0), new Vector3(1.6f, 1.9f, 1.6f), body);
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2) m.Box(new Vector3(x * .78f, 1.1f, z * .78f), new Vector3(.14f, 1.9f, .14f), K.R.cream);
            foreach (Quaternion q in new[] { Quaternion.identity, Quaternion.Euler(0, 90, 0), Quaternion.Euler(0, 180, 0) })
            {
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(q * new Vector3(0, 1.4f, .8f), new Vector3(1.2f, .86f, .06f), K.R.ink, q);
                m.Surface = ComicSurface.Glass;
                m.Box(q * new Vector3(0, 1.4f, .83f), new Vector3(1.06f, .72f, .04f), K.R.glass, q);
            }
            m.Surface = ComicSurface.Wood;
            m.Box(new Vector3(0, .95f, .95f), new Vector3(1.5f, .08f, .32f), K.R.cream);
            m.Surface = ComicSurface.Fabric;
            for (int i = 0; i < 6; i++) m.Box(new Vector3(-.75f + (i + .5f) * .25f, 1.98f, 1.02f), new Vector3(.25f, .08f, .5f), i % 2 == 0 ? K.R.red : K.R.cream, Quaternion.Euler(18, 0, 0));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(new Vector3(0, 2.11f, 0), new Vector3(2f, .14f, 2f), K.R.ink);
            m.Cylinder(new Vector3(0, 2.48f, 0), 1.3f, .6f, K.R.red, 4, .12f, Quaternion.Euler(0, 45, 0));
            m.Cylinder(new Vector3(0, 2.85f, 0), .1f, .2f, K.R.cream, 8);
            return m;
        }

        static ComicMesh CafeSetMesh()
        {
            var m = new ComicMesh();
            K.CafeSet(m, Vector3.zero, K.Hex("2F6B66"));
            return m;
        }

        // Wheels like the kit's cars: ink tyre, silver hub.
        static void Wheels(ComicMesh m, float x, float r, float y, params float[] zs)
        {
            m.Surface = ComicSurface.Rubber;
            foreach (float z in zs)
                for (int s = -1; s <= 1; s += 2)
                {
                    m.Surface = ComicSurface.Rubber;
                    m.Cylinder(new Vector3(s * x, y, z), r, .3f, K.R.ink, 8, 1, Quaternion.Euler(0, 0, 90));
                    m.Surface = ComicSurface.Metal;
                    m.Cylinder(new Vector3(s * (x + .03f), y, z), r * .45f, .3f, K.Hex("D1DBE5"), 8, 1, Quaternion.Euler(0, 0, 90));
                }
        }

        // A generic city bus (no brand, no text): white body, teal skirt, window band with ink pillars, doors, lights,
        // ink bumpers and trims, wheels with hubs. Front at +Z.
        static ComicMesh BusMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            Color body = K.Hex("F2F2EE"), skirt = K.Hex("2F8C7E"), dark = K.Hex("26333D");
            float L = 12f, hz = L / 2;
            m.Box(new Vector3(0, 1.8f, 0), new Vector3(2.5f, 2.6f, L), body);
            m.Box(new Vector3(0, .72f, 0), new Vector3(2.54f, .5f, L + .04f), skirt);
            m.Box(new Vector3(0, 3.12f, 0), new Vector3(2.4f, .1f, L - .2f), K.R.ink);                      // roof edge trim
            m.Box(new Vector3(0, 3.3f, -1.2f), new Vector3(1.8f, .3f, 3.2f), K.Hex("C9CDD2"));               // roof unit
            m.Box(new Vector3(0, 3.47f, -1.2f), new Vector3(1.86f, .06f, 3.26f), K.R.ink);
            for (int s = -1; s <= 1; s += 2)
            {
                m.Surface = ComicSurface.Glass;
                m.Box(new Vector3(s * 1.255f, 2.2f, -.6f), new Vector3(.06f, 1.05f, L - 3f), dark);
                m.Surface = ComicSurface.PaintedMetal;
                for (float z = -hz + 1f; z < hz - 2f; z += 1.6f) m.Box(new Vector3(s * 1.275f, 2.2f, z), new Vector3(.06f, 1.1f, .14f), K.R.ink);
                m.Box(new Vector3(s * 1.27f, 1.55f, 0), new Vector3(.06f, .1f, L - .3f), K.R.ink);         // belt line
            }
            // doors on the kerb side (left), front and middle
            foreach (float z in new[] { hz - 1.4f, -.8f })
            {
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(new Vector3(-1.27f, 1.65f, z), new Vector3(.07f, 2.3f, 1.2f), K.R.ink);
                m.Surface = ComicSurface.Glass;
                m.Box(new Vector3(-1.3f, 1.75f, z), new Vector3(.05f, 1.9f, 1f), dark);
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(new Vector3(0, 2f, hz + .02f), new Vector3(2.36f, 1.66f, .06f), K.R.ink);
            m.Surface = ComicSurface.Glass;
            m.Box(new Vector3(0, 1.98f, hz + .05f), new Vector3(2.2f, 1.45f, .04f), dark);
            m.Box(new Vector3(0, 2.4f, -hz - .04f), new Vector3(2f, .8f, .06f), dark);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(new Vector3(0, 3f, hz + .02f), new Vector3(1.7f, .3f, .1f), K.R.ink);                      // destination board, blank
            m.Box(new Vector3(0, 3f, hz + .08f), new Vector3(1.5f, .18f, .04f), K.Hex("F3B23A"));
            m.Box(new Vector3(0, .5f, hz + .08f), new Vector3(2.56f, .3f, .14f), K.R.ink);                   // bumpers
            m.Box(new Vector3(0, .5f, -hz - .08f), new Vector3(2.56f, .3f, .14f), K.R.ink);
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * .9f, .95f, hz + .04f), new Vector3(.38f, .2f, .06f), K.R.cream);
                m.Box(new Vector3(s * .95f, 1f, -hz - .04f), new Vector3(.3f, .28f, .06f), K.R.red);
            }
            Wheels(m, 1.12f, .5f, .5f, hz - 2.4f, -hz + 2.8f);
            return m;
        }

        // A plain delivery van (no brand): cab and box body, framed windscreen and side windows, ink bumpers and trims,
        // a red stripe, rear doors, lights, wheels with hubs. Front at +Z.
        static ComicMesh VanMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            Color body = K.Hex("E8E4D8"), dark = K.Hex("26333D");
            m.Box(new Vector3(0, 1.4f, -.55f), new Vector3(2f, 2.1f, 4f), body);
            m.Box(new Vector3(0, 1f, 1.95f), new Vector3(1.96f, 1.3f, 1.1f), body);
            m.Box(new Vector3(0, 1.9f, 1.55f), new Vector3(1.9f, .5f, .4f), body);
            m.Box(new Vector3(0, .62f, -.1f), new Vector3(2.04f, .26f, 5f), K.R.red);
            m.Box(new Vector3(0, 2.48f, -.55f), new Vector3(2.04f, .08f, 4.04f), K.R.ink);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(new Vector3(0, 1.72f, 1.78f), new Vector3(1.84f, .78f, .06f), K.R.ink);
            m.Surface = ComicSurface.Glass;
            m.Box(new Vector3(0, 1.72f, 1.82f), new Vector3(1.7f, .64f, .04f), dark);
            for (int s = -1; s <= 1; s += 2)
            {
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(new Vector3(s * .99f, 1.7f, 1.95f), new Vector3(.06f, .62f, .9f), K.R.ink);
                m.Surface = ComicSurface.Glass;
                m.Box(new Vector3(s * 1.01f, 1.7f, 1.95f), new Vector3(.04f, .5f, .78f), dark);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(new Vector3(s * 1.02f, 1.35f, -.55f), new Vector3(.06f, .08f, 3.9f), K.R.ink);       // side trim
                m.Box(new Vector3(s * .72f, 1f, 2.52f), new Vector3(.36f, .2f, .06f), K.R.cream);
                m.Box(new Vector3(s * .8f, 1.1f, -2.57f), new Vector3(.24f, .34f, .06f), K.R.red);
            }
            m.Box(new Vector3(0, 1.45f, -2.57f), new Vector3(.06f, 1.8f, .06f), K.R.ink);                  // rear doors split
            m.Box(new Vector3(0, .45f, 2.58f), new Vector3(2.04f, .26f, .14f), K.R.ink);
            m.Box(new Vector3(0, .45f, -2.6f), new Vector3(2.04f, .26f, .14f), K.R.ink);
            Wheels(m, .95f, .38f, .38f, 1.55f, -1.75f);
            return m;
        }
    }

    // ==================================================================== shared kit (CBD and Quay)

    internal sealed class CqRampSpec
    {
        public string Name; public float X, Z, Width, Rise, Height, Top, Launch = 1f, Hang = .5f;
        /// <summary>0 = a launch ramp (+Z); 180 = a "down ramp" describing a deck coming back down towards +Z: its pivot
        /// is at the bottom end and it rises backwards (slow riders land on it and ride down).</summary>
        public float Yaw;
        public float Lip => Z + Rise + Top;
    }

    internal sealed class CqGapSpec
    {
        public string Name; public float Z, Length, Width; public string Word = "SPLASH"; public bool Water = true;
    }

    /// <summary>A building lot along one side of a chunk: facade plane at |x| = Line facing the road, from Z0 to Z1.</summary>
    internal struct CqLot
    {
        public int S; public float Z0, Z1, Line, Depth, Base;
        public CqLot(int side, float z0, float z1, float line, float depth, float baseY) { S = side; Z0 = z0; Z1 = z1; Line = line; Depth = depth; Base = baseY; }
        public float W => Z1 - Z0;
        public float Zc => (Z0 + Z1) * .5f;
        /// <summary>along: metres from Z0; up: metres above the base; outward: metres from the facade towards the road.</summary>
        public Vector3 P(float along, float up, float outward) => new Vector3(S * (Line - outward), Base + up, Z0 + along);
    }

    /// <summary>One chunk being assembled: meshes, colliders, slots, ramps and gaps, written by CbdQuayKit.WriteChunk.</summary>
    internal sealed class CqChunkDraft
    {
        public readonly string Id;
        public readonly float Length, Drop;
        public RoadType Type = RoadType.Street;
        /// <summary>Road, sidewalks, water: built flat (relative to the road), sheared on write. No outline.</summary>
        public readonly ComicMesh Road = new ComicMesh();
        /// <summary>Ramp decks and bridges: built flat, sheared on write, drawn with the frontage (ink outline).</summary>
        public readonly ComicMesh SlopedFrontage = new ComicMesh();
        public readonly ComicMesh Frontage = new ComicMesh(), Props = new ComicMesh(), Deep = new ComicMesh(), Landmark = new ComicMesh();
        public readonly List<DressingObstacle> Collisions = new List<DressingObstacle>();
        public readonly List<Bounds> Walls = new List<Bounds>(), KeepOut = new List<Bounds>();
        public readonly List<Vector2> Slots = new List<Vector2>(), GiftSlots = new List<Vector2>();
        public readonly List<CqRampSpec> Ramps = new List<CqRampSpec>();
        public readonly List<CqGapSpec> Gaps = new List<CqGapSpec>();

        public CqChunkDraft(string id, float drop, float length = 40f) { Id = id; Drop = drop; Length = length; }
        /// <summary>Road half width at the start (4 street, 8 for the 4-lane Terrace) and at the end (0 = same).</summary>
        public float Half = CbdQuayKit.Half, EndHalf;
        public Vector2 WidthChangeZ = new Vector2(8f, 32f);
        public bool ChangesWidth => EndHalf > 0f && Mathf.Abs(EndHalf - Half) > .001f;
        /// <summary>Same as RoadChunk.HalfWidthAt (smoothstep between WidthChangeZ.x and .y).</summary>
        public float HalfAt(float z)
        {
            if (!ChangesWidth) return Half;
            float t = Mathf.InverseLerp(WidthChangeZ.x, WidthChangeZ.y, z);
            return Mathf.Lerp(Half, EndHalf, t * t * (3f - 2f * t));
        }
        /// <summary>Sidewalk outer edge at z.</summary>
        public float EdgeAt(float z) => HalfAt(z) + CbdQuayKit.Side;
        /// <summary>The building line: the outer sidewalk edge where the road is widest.</summary>
        public float Edge => Mathf.Max(Half, EndHalf) + CbdQuayKit.Side;
        public float Grade => -Drop / Length;
        /// <summary>Road surface height at z.</summary>
        public float Y(float z) => -Grade * z;
        /// <summary>Sidewalk surface height at z.</summary>
        public float Ground(float z) => Y(z) + CbdQuayKit.Kerb;
        /// <summary>Base height for an upright building centred at z (the sidewalk height there).</summary>
        public float Base(float z) => Ground(z);
        public void Slot(float x, float z) => Slots.Add(new Vector2(x, z));
        public void StandardSlots()
        {
            for (float z0 = 0; z0 < Length - 1f; z0 += 40f)
                foreach (float z in new[] { z0 + 10f, z0 + 25f })
                    foreach (float x in Half >= 7.9f ? new[] { -6f, -2f, 2f, 6f } : new[] { -2.6f, 0f, 2.6f }) Slot(x, z);
        }
        /// <summary>An invisible wall where the building is (keeps people from being placed inside it).</summary>
        public void Wall(CqLot l, float height)
        {
            Vector3 a = l.P(0, -1f, 0), b = l.P(l.W, height, -l.Depth);
            var bounds = new Bounds((a + b) * .5f, new Vector3(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z)));
            Walls.Add(bounds);
        }
    }

    internal static class CbdQuayKit
    {
        public const float Half = 4f, Side = 4f, Kerb = .2f, Edge = Half + Side;
        /// <summary>The street's standard drop per 40 m chunk (4.5 degrees).</summary>
        public const float StandardDrop = -3.1470692f;
        public const int TriangleBudget = 25000, RendererBudget = 8;
        public static ComicCityRecipe R;
        public static Material Palette, Ink;
        public static readonly List<string> Report = new List<string>();
        static string _meshFolder;
        static readonly Dictionary<string, ComicMesh> KitGeometry = new Dictionary<string, ComicMesh>();

        public static void Begin(string meshFolder, params string[] folders)
        {
            R = AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(ComicCityBuilder.RecipePath);
            Palette = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicPalette.mat");
            Ink = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicInk.mat");
            if (R == null || Palette == null || Ink == null) throw new InvalidOperationException("Build the comic city first (recipe, ComicPalette.mat, ComicInk.mat).");
            if (R.roadHalfWidth != Half || R.sidewalkWidth != Side || Mathf.Abs(R.curbHeight - Kerb) > 1e-4f) throw new InvalidOperationException("Districts assume the 8 m street with 4 m sidewalks and 0.2 m kerbs.");
            ComicSurfaceAssets.EnsureLoaded();
            _meshFolder = meshFolder;
            PrefabBuildUtility.EnsureFolder(meshFolder);
            foreach (string f in folders) PrefabBuildUtility.EnsureFolder(f);
            KitGeometry.Clear();
            Report.Clear();
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out Color c) ? c : Color.magenta;
        public static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // ---- primitive helpers

        public static void Face(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector3 normal, bool ink = true)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) m.Quad(d, c, b, a, color, ink);
            else m.Quad(a, b, c, d, color, ink);
        }

        public static void Flat(ComicMesh m, float l0, float r0, float l1, float r1, float z0, float z1, float y, Color color)
        {
            m.Surface = ComicSurface.Plain;
            Face(m, V(l0, y, z0), V(l1, y, z1), V(r1, y, z1), V(r0, y, z0), color, Vector3.up, false);
        }

        public static void MinMaxBox(ComicMesh m, Vector3 a, Vector3 b, Color color)
        {
            Vector3 min = Vector3.Min(a, b), max = Vector3.Max(a, b);
            m.Box((min + max) * .5f, max - min, color);
        }

        /// <summary>Box in flat road space beside the sidewalk (sheared with the road on write): d0..d1 metres beyond the
        /// sidewalk edge on side s, y relative to the road surface; split into 4 m pieces so the city curl stays smooth.</summary>
        public static void SideBox(ComicMesh m, int s, float z0, float z1, float y0, float y1, float d0, float d1, Color c)
        {
            for (float z = z0; z < z1 - .01f; z += 4f)
                MinMaxBox(m, V(s * (Edge + d0), y0, z), V(s * (Edge + d1), y1, Mathf.Min(z1, z + 4f)), c);
        }

        /// <summary>Box in lot space: along a0..a1, up u0..u1, outward o0..o1 (towards the road from the facade).</summary>
        public static void LBox(ComicMesh m, CqLot l, float a0, float a1, float u0, float u1, float o0, float o1, Color c) =>
            MinMaxBox(m, l.P(a0, u0, o0), l.P(a1, u1, o1), c);

        /// <summary>A quad on the facade plane (outward o), facing the road.</summary>
        public static void LQuad(ComicMesh m, CqLot l, float a0, float a1, float u0, float u1, float o, Color c, bool ink = true) =>
            Face(m, l.P(a0, u0, o), l.P(a1, u0, o), l.P(a1, u1, o), l.P(a0, u1, o), c, new Vector3(-l.S, 0, 0), ink);

        /// <summary>A roof slope rising from the front (outward oFront, height u0) to the ridge (oRidge, u1) and back down to oBack.</summary>
        public static void LSlope(ComicMesh m, CqLot l, float a0, float a1, float u0, float u1, float oFront, float oRidge, float oBack, Color c)
        {
            Vector3 f0 = l.P(a0, u0, oFront), f1 = l.P(a1, u0, oFront), r0 = l.P(a0, u1, oRidge), r1 = l.P(a1, u1, oRidge), b0 = l.P(a0, u0, oBack), b1 = l.P(a1, u0, oBack);
            Vector3 toRoad = new Vector3(-l.S, 0, 0);
            Face(m, f0, f1, r1, r0, c, (toRoad + Vector3.up).normalized);
            Face(m, r0, r1, b1, b0, c, (-toRoad + Vector3.up).normalized);
            TriFacing(m, f0, r0, b0, c, Vector3.back);
            TriFacing(m, f1, r1, b1, c, Vector3.forward);
        }

        /// <summary>A triangular gable on the facade plane, with depth: base a0..a1 at u0, apex at the middle, u1.</summary>
        public static void LGable(ComicMesh m, CqLot l, float a0, float a1, float u0, float u1, float oFront, float oBack, Color c)
        {
            float ac = (a0 + a1) * .5f;
            Vector3 f0 = l.P(a0, u0, oFront), f1 = l.P(a1, u0, oFront), fa = l.P(ac, u1, oFront);
            Vector3 b0 = l.P(a0, u0, oBack), b1 = l.P(a1, u0, oBack), ba = l.P(ac, u1, oBack);
            Vector3 toRoad = new Vector3(-l.S, 0, 0);
            TriFacing(m, f0, f1, fa, c, toRoad);
            TriFacing(m, b0, b1, ba, c, -toRoad);
            Face(m, f0, fa, ba, b0, c, (Vector3.back + Vector3.up).normalized);
            Face(m, f1, fa, ba, b1, c, (Vector3.forward + Vector3.up).normalized);
        }

        public static void TriFacing(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, Color color, Vector3 normal)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) m.Tri(a, c, b, color); else m.Tri(a, b, c, color);
        }

        public static void Clock(ComicMesh m, Vector3 centre, Quaternion facing, float radius, Color face, Color ink)
        {
            // facing: rotation that turns +Z towards the viewer side the clock faces
            m.Cylinder(centre, radius + .12f, .12f, ink, 12, 1f, facing * Quaternion.Euler(90, 0, 0));
            m.Cylinder(centre + facing * Vector3.forward * .07f, radius, .08f, face, 12, 1f, facing * Quaternion.Euler(90, 0, 0));
            Vector3 f = facing * Vector3.forward * .13f;
            m.Box(centre + f + facing * new Vector3(0, radius * .3f, 0), new Vector3(.09f, radius * .62f, .03f), ink, facing);
            m.Box(centre + f + facing * new Vector3(radius * .22f, 0, 0), new Vector3(radius * .5f, .08f, .03f), ink, facing);
        }

        // ---- road surface: the street cross-section (road, kerbs, sidewalks), no ink on slabs (like the street chunks)

        public static void StreetSurface(ComicMesh m, float length, Color road, Color pavement, Color kerbCap, bool centreLine, float skipFrom = -1f, float skipTo = -1f)
        {
            float h = Kerb, sw = Side, w = Half;
            Color joint = Color.Lerp(R.ink, pavement, .5f);
            var cuts = new List<float>();
            for (float z = 0; z < length - .01f; z += 4f) cuts.Add(z);
            cuts.Add(length);
            if (skipFrom >= 0f) { cuts.Add(skipFrom); cuts.Add(skipTo); }
            cuts = cuts.Distinct().OrderBy(z => z).ToList();
            for (int ci = 0; ci + 1 < cuts.Count; ci++)
            {
                float z0 = cuts[ci], z1 = cuts[ci + 1];
                if (z1 - z0 < .01f || skipFrom >= 0f && z0 >= skipFrom - .01f && z1 <= skipTo + .01f) continue;
                m.Surface = road == R.asphalt ? ComicSurface.Asphalt : ComicSurface.Paving;
                Face(m, V(-w, 0, z0), V(-w, 0, z1), V(w, 0, z1), V(w, 0, z0), road, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    float i0 = s * w, o0 = s * (w + sw);
                    Vector3 inward = new Vector3(-s, 0, 0), outward = new Vector3(s, 0, 0);
                    m.Surface = ComicSurface.Paving;
                    Face(m, V(i0, h, z0), V(i0, h, z1), V(o0, h, z1), V(o0, h, z0), pavement, Vector3.up, false);
                    Face(m, V(i0, -.05f, z0), V(i0, -.05f, z1), V(i0, h, z1), V(i0, h, z0), pavement, inward, false);
                    Face(m, V(o0, -2f, z0), V(o0, -2f, z1), V(o0, h, z1), V(o0, h, z0), pavement, outward, false);
                    m.Surface = ComicSurface.Plain;
                    float c0 = s * (w + .24f), top = h + .04f;
                    Face(m, V(i0, top, z0), V(i0, top, z1), V(c0, top, z1), V(c0, top, z0), kerbCap, Vector3.up, false);
                    Face(m, V(i0, h, z0), V(i0, h, z1), V(i0, top, z1), V(i0, top, z0), kerbCap, inward, false);
                    Face(m, V(c0, h, z0), V(c0, h, z1), V(c0, top, z1), V(c0, top, z0), kerbCap, outward, false);
                    if (centreLine) Flat(m, s * (w - .19f), s * (w - .11f), s * (w - .19f), s * (w - .11f), z0, z1, .0305f, R.cream);
                    for (int k = 1; k < 4; k++) Flat(m, s * (w + k) - .0125f, s * (w + k) + .0125f, s * (w + k) - .0125f, s * (w + k) + .0125f, z0, z1, h + .025f, joint);
                }
                for (float z = Mathf.Ceil(z0 / 2f) * 2f; z < z1 - .01f; z += 2f)
                    for (int s = -1; s <= 1; s += 2) Flat(m, s * w, s * (w + sw), s * w, s * (w + sw), z - .0125f, z + .0125f, h + .025f, joint);
                if (centreLine)
                    for (int s = -1; s <= 1; s += 2)
                        for (float z = Mathf.Ceil(z0 / 5f) * 5f; z < z1 - .01f; z += 5f) Flat(m, s * .12f - .05f, s * .12f + .05f, s * .12f - .05f, s * .12f + .05f, z, Mathf.Min(z + 3f, z1), .035f, R.yellow);
            }
            // closed slab ends (hidden by the neighbouring chunk)
            foreach (float ze in new[] { 0f, length })
            {
                Vector3 normal = ze <= 0f ? Vector3.back : Vector3.forward;
                m.Surface = ComicSurface.Asphalt;
                Face(m, V(-w, -.8f, ze), V(w, -.8f, ze), V(w, 0, ze), V(-w, 0, ze), road, normal, false);
                m.Surface = ComicSurface.Paving;
                for (int s = -1; s <= 1; s += 2) Face(m, V(s * w, -2f, ze), V(s * (w + sw), -2f, ze), V(s * (w + sw), h, ze), V(s * w, h, ze), pavement, normal, false);
            }
        }

        /// <summary>The 4-lane Terrace surface, following d.HalfAt(z) (width transitions too): asphalt, kerbs, sidewalks,
        /// white lane lines at x = +-4 where there are two lanes each way, and a painted median (two solid yellow lines with
        /// yellow hatching, no raised kerb: the rider can change lanes); the street's dashed double yellow where it is
        /// two lanes.</summary>
        public static void WideStreetSurface(ComicMesh m, CqChunkDraft d)
        {
            float L = d.Length, h = Kerb, sw = Side;
            Color joint = Color.Lerp(R.ink, R.pavement, .5f), white = new Color(.95f, .95f, .93f);
            float step = d.ChangesWidth ? 2f : 4f;
            int n = Mathf.RoundToInt(L / step);
            for (int i = 0; i < n; i++)
            {
                float z0 = i * step, z1 = z0 + step, w0 = d.HalfAt(z0), w1 = d.HalfAt(z1);
                m.Surface = ComicSurface.Asphalt;
                Face(m, V(-w0, 0, z0), V(-w1, 0, z1), V(w1, 0, z1), V(w0, 0, z0), R.asphalt, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    float i0 = s * w0, i1 = s * w1, o0 = s * (w0 + sw), o1 = s * (w1 + sw);
                    Vector3 inward = new Vector3(-s, 0, 0), outward = new Vector3(s, 0, 0);
                    m.Surface = ComicSurface.Paving;
                    Face(m, V(i0, h, z0), V(i1, h, z1), V(o1, h, z1), V(o0, h, z0), R.pavement, Vector3.up, false);
                    Face(m, V(i0, -.05f, z0), V(i1, -.05f, z1), V(i1, h, z1), V(i0, h, z0), R.pavement, inward, false);
                    Face(m, V(o0, -2f, z0), V(o1, -2f, z1), V(o1, h, z1), V(o0, h, z0), R.pavement, outward, false);
                    m.Surface = ComicSurface.Plain;
                    float c0 = s * (w0 + .24f), c1 = s * (w1 + .24f), top = h + .04f;
                    Face(m, V(i0, top, z0), V(i1, top, z1), V(c1, top, z1), V(c0, top, z0), R.cream, Vector3.up, false);
                    Face(m, V(i0, h, z0), V(i1, h, z1), V(i1, top, z1), V(i0, top, z0), R.cream, inward, false);
                    Face(m, V(c0, h, z0), V(c1, h, z1), V(c1, top, z1), V(c0, top, z0), R.cream, outward, false);
                    Flat(m, s * (w0 - .19f), s * (w0 - .11f), s * (w1 - .19f), s * (w1 - .11f), z0, z1, .0305f, R.cream);
                    for (int k = 1; k < 4; k++)
                        Flat(m, s * (w0 + k) - .0125f, s * (w0 + k) + .0125f, s * (w1 + k) - .0125f, s * (w1 + k) + .0125f, z0, z1, h + .025f, joint);
                }
            }
            for (int z = 0; z < 40; z += 2)
                for (int s = -1; s <= 1; s += 2)
                {
                    float w = d.HalfAt(z);
                    Flat(m, s * w, s * (w + sw), s * w, s * (w + sw), z - .0125f, z + .0125f, h + .025f, joint);
                }
            // centre: the double yellow opens into a 1.2 m painted median as the road widens
            float Median(float z) => .12f + .48f * Mathf.InverseLerp(Half, 8f, d.HalfAt(z));
            for (float z = 0; z < L - .01f; z += 1f)
            {
                float ma = Median(z), mb = Median(z + 1f);
                bool wide = ma > .13f || mb > .13f;
                for (int s = -1; s <= 1; s += 2)
                    if (wide || z % 5f < 3f)
                        Flat(m, s * ma - .05f, s * ma + .05f, s * mb - .05f, s * mb + .05f, z, z + 1f, .035f, R.yellow);
                if (ma > .55f && mb > .55f && Mathf.RoundToInt(z) % 2 == 0)
                    m.Box(V(0, .036f, z + .5f), V(.08f, .006f, 1.45f), R.yellow, Quaternion.Euler(0, 52f, 0));
            }
            // lane lines: 3 m white dashes every 8 m at x = +-4 wherever both lanes on that side are wide enough
            for (int s = -1; s <= 1; s += 2)
                for (int z = 0; z < 40; z += 8)
                    if (Mathf.Min(d.HalfAt(z), d.HalfAt(z + 3)) >= 6.9f)
                        Flat(m, s * 4f - .06f, s * 4f + .06f, s * 4f - .06f, s * 4f + .06f, z, z + 3f, .035f, white);
            foreach (float ze in new[] { 0f, L })
            {
                Vector3 normal = ze <= 0f ? Vector3.back : Vector3.forward;
                float w = d.HalfAt(ze);
                m.Surface = ComicSurface.Asphalt;
                Face(m, V(-w, -.8f, ze), V(w, -.8f, ze), V(w, 0, ze), V(-w, 0, ze), R.asphalt, normal, false);
                m.Surface = ComicSurface.Paving;
                for (int s = -1; s <= 1; s += 2) Face(m, V(s * w, -2f, ze), V(s * (w + sw), -2f, ze), V(s * (w + sw), h, ze), V(s * w, h, ze), R.pavement, normal, false);
            }
        }

        /// <summary>Flat water (no ink) with lighter ripple strokes; level relative to the road.</summary>
        public static void Water(ComicMesh m, float x0, float x1, float z0, float z1, float level, Color water, Color ripple, int seed)
        {
            m.Surface = ComicSurface.Plain;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                Face(m, V(x0, level, z), V(x0, level, e), V(x1, level, e), V(x1, level, z), water, Vector3.up, false);
            }
            var rng = new System.Random(seed);
            float area = Mathf.Abs((x1 - x0) * (z1 - z0));
            int strokes = Mathf.Clamp(Mathf.RoundToInt(area / 14f), 2, 90);
            for (int i = 0; i < strokes; i++)
            {
                float x = Mathf.Lerp(Mathf.Min(x0, x1) + .5f, Mathf.Max(x0, x1) - 1.5f, (float)rng.NextDouble());
                float z = Mathf.Lerp(z0 + .3f, z1 - .5f, (float)rng.NextDouble());
                float len = .8f + (float)rng.NextDouble() * 1.6f;
                Face(m, V(x, level + .015f, z), V(x, level + .015f, z + .14f), V(x + len, level + .015f, z + .14f), V(x + len, level + .015f, z), ripple, Vector3.up, false);
            }
        }

        /// <summary>The ramp deck, in flat road space: slope, top, sides and lip face down to lipBottom.</summary>
        public static void RampMesh(ComicMesh m, CqRampSpec r, Color top, Color side, bool stripes, float lipBottom)
        {
            float x0 = r.X - r.Width * .5f, x1 = r.X + r.Width * .5f, z0 = r.Z, z1 = r.Z + r.Rise, z2 = r.Lip, h = r.Height;
            m.Surface = ComicSurface.Concrete;
            Face(m, V(x0, 0, z0), V(x0, h, z1), V(x1, h, z1), V(x1, 0, z0), top, (Vector3.up + Vector3.back * (h / r.Rise)).normalized);
            if (r.Top > .01f) Face(m, V(x0, h, z1), V(x0, h, z2), V(x1, h, z2), V(x1, h, z1), top, Vector3.up);
            Face(m, V(x0, lipBottom, z2), V(x1, lipBottom, z2), V(x1, h, z2), V(x0, h, z2), side, Vector3.forward);
            foreach (float x in new[] { x0, x1 })
            {
                Vector3 n = new Vector3(x < r.X ? -1 : 1, 0, 0);
                TriFacing(m, V(x, 0, z0), V(x, h, z1), V(x, 0, z1), side, n);
                if (r.Top > .01f) Face(m, V(x, 0, z1), V(x, h, z1), V(x, h, z2), V(x, 0, z2), side, n);
                if (lipBottom < 0f) Face(m, V(x, lipBottom, z1), V(x, 0, z1), V(x, 0, z2), V(x, lipBottom, z2), side, n);
            }
            if (!stripes)
            {
                // timber deck: board joints, a cream nosing on the lip and ink skirting along the sides
                Color joint = Color.Lerp(top, R.ink, .35f);
                for (float t = .12f; t < 1f; t += .12f)
                {
                    float z = z0 + r.Rise * t, y = h * t + .012f;
                    Face(m, V(x0, y, z), V(x0, y, z + .05f), V(x1, y, z + .05f), V(x1, y, z), joint, Vector3.up, false);
                }
                m.Surface = ComicSurface.Wood;
                m.Box(V(r.X, h + .04f, z2 - .12f), V(r.Width + .04f, .1f, .24f), R.cream);
                m.Surface = ComicSurface.PaintedMetal;
                foreach (float x in new[] { x0, x1 }) m.Box(V(x, .05f, (z0 + z2) * .5f), V(.08f, .1f, z2 - z0), R.ink);
                return;
            }
            // yellow and black hazard stripes along the lip and the sides, so the rider reads it from far away
            int n2 = Mathf.Max(4, Mathf.RoundToInt(r.Width / .5f));
            for (int i = 0; i < n2; i++)
            {
                float a = Mathf.Lerp(x0, x1, (float)i / n2), b = Mathf.Lerp(x0, x1, (float)(i + 1) / n2);
                Face(m, V(a, h - .3f, z2 + .01f), V(b, h - .3f, z2 + .01f), V(b, h, z2 + .01f), V(a, h, z2 + .01f), i % 2 == 0 ? R.yellow : R.ink, Vector3.forward, false);
                Face(m, V(a, h + .01f, z2 - .45f), V(a, h + .01f, z2), V(b, h + .01f, z2), V(b, h + .01f, z2 - .45f), i % 2 == 0 ? R.yellow : R.ink, Vector3.up, false);
            }
        }

        // ---- props (street furniture), with simple colliders for StreetDressing

        static ComicAssetSpec Spec(string id) => R.props.FirstOrDefault(s => s.id == id) ?? throw new InvalidOperationException("No prop " + id + " in the comic city recipe.");

        public static ComicMesh KitProp(string id)
        {
            if (!KitGeometry.TryGetValue(id, out ComicMesh m)) KitGeometry[id] = m = ComicKitGeometry.Prop(Spec(id), R);
            return m;
        }

        public static void PropFromKit(ComicMesh props, List<DressingObstacle> collisions, string id, Vector3 p, float yaw)
        {
            props.Add(KitProp(id), p, Quaternion.Euler(0, yaw, 0), Vector3.one);
            ComicAssetSpec spec = Spec(id);
            bool thin = spec.style == "tree" || spec.style == "lamp" || spec.style == "sign";
            collisions?.Add(new DressingObstacle
            {
                id = id, position = p, rotation = V(0, yaw, 0), centre = V(0, spec.height * .5f, 0),
                size = thin ? V(.35f, spec.height, .35f) : V(spec.width, spec.height, spec.depth), canJumpOver = spec.height <= 1.1f,
            });
        }

        public static void Lamp(ComicMesh props, List<DressingObstacle> collisions, int side, float y, float z, float half = Half) =>
            PropFromKit(props, collisions, "StreetLamp", new Vector3(side * (half + .55f), y, z), side < 0 ? 90f : -90f);

        public static void Thin(List<DressingObstacle> collisions, string id, Vector3 p, float height, float size = .4f) =>
            collisions?.Add(new DressingObstacle { id = id, position = p, rotation = Vector3.zero, centre = V(0, height * .5f, 0), size = V(size, height, size), canJumpOver = false });

        static readonly Color Bark = Hex("8C7A62"), Silver = new Color(.82f, .86f, .9f);

        /// <summary>The comic kit's plant blob (ComicKitGeometry Prop "tree" / "planter" Plant helper): a faceted
        /// ellipsoid and a smaller, lighter one on top of it.</summary>
        public static void Plant(ComicMesh m, Vector3 c, float size, Color? green = null)
        {
            Color g = green ?? R.green;
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(c, new Vector3(size, size * .7f, size), g);
            m.Ellipsoid(c + new Vector3(size * .23f, size * .15f, 0), new Vector3(size * .65f, size * .6f, size * .65f), Color.Lerp(g, R.yellow, .25f));
        }

        /// <summary>Tree pit like the kit's TreePit: ink frame, pavement top, ink root ring.</summary>
        public static void TreePit(ComicMesh m, Vector3 p, float size)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, .05f, 0), V(size, .1f, size), R.ink);
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, .11f, 0), V(size * .84f, .06f, size * .84f), R.pavement);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .16f, 0), .3f, .08f, R.ink, 8);
        }

        /// <summary>London plane tree, the kit's street tree made bigger: tapered 8-sided trunk on a tree pit, two
        /// limbs, a canopy of clustered two-tone blobs (far = fewer blobs and no pit, for the deep city).</summary>
        public static void PlaneTree(ComicMesh m, List<DressingObstacle> collisions, Vector3 p, float scale, bool far = false)
        {
            if (!far) TreePit(m, p, 1.4f);
            float h = 3f * scale;
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, h * .5f + .1f, 0), .24f * scale, h, Bark, 8, .72f);
            m.Cylinder(p + V(.5f * scale, h + .2f * scale, .1f * scale), .12f * scale, 1.5f * scale, Bark, 8, .7f, Quaternion.Euler(0, 0, -40));
            // a broad, spreading crown: one big blob and two side blobs hanging a little lower
            Vector3 crown = p + V(0, h + 1.45f * scale, 0);
            Plant(m, crown, 3.6f * scale);
            Plant(m, crown + V(1.55f, -.55f, .5f) * scale, 2.5f * scale, Hex("4F7F38"));
            if (!far) Plant(m, crown + V(-1.5f, -.45f, -.55f) * scale, 2.6f * scale, Hex("6A9A40"));
            Thin(collisions, "PlaneTree", p, h);
        }

        /// <summary>A slab from a to b (thick, width tapering wa -> wb), top colour up, darker underside: palm fronds.</summary>
        static void Slab(ComicMesh m, Vector3 a, Vector3 b, Vector3 side, float wa, float wb, float thick, Color top, Color under)
        {
            Vector3 along = (b - a).normalized, n = Vector3.Cross(along, side).normalized;
            if (n.y < 0f) n = -n;
            Vector3 A(float w, float t) => a + side * w + n * t;
            Vector3 B(float w, float t) => b + side * w + n * t;
            float ha = wa * .5f, hb = wb * .5f, ht = thick * .5f;
            Color edge = Color.Lerp(top, under, .5f);
            Face(m, A(-ha, ht), B(-hb, ht), B(hb, ht), A(ha, ht), top, n);
            Face(m, A(-ha, -ht), B(-hb, -ht), B(hb, -ht), A(ha, -ht), under, -n);
            Face(m, A(-ha, -ht), B(-hb, -ht), B(-hb, ht), A(-ha, ht), edge, -side);
            Face(m, A(ha, -ht), B(hb, -ht), B(hb, ht), A(ha, ht), edge, side);
            Face(m, A(-ha, -ht), A(ha, -ht), A(ha, ht), A(-ha, ht), edge, -along);
            Face(m, B(-hb, -ht), B(hb, -ht), B(hb, ht), B(-hb, ht), edge, along);
        }

        /// <summary>Palm: ringed 8-sided trunk with a slight curve, ink root ring and crown boot, coconuts, 9 fronds of
        /// four thick bent slabs arching out and drooping, darker underneath.</summary>
        public static void Palm(ComicMesh m, List<DressingObstacle> collisions, Vector3 p, float height, int seed)
        {
            Color trunk = Hex("9A7652"), trunkDark = Hex("85653F");
            float lean = .35f + (Mathf.Abs(seed * 37) % 4) * .12f;
            Vector3 dir = Quaternion.Euler(0, seed * 71f, 0) * Vector3.right;
            Vector3 At(float y) => p + V(0, y, 0) + dir * (lean * y * y / (height * height));
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .06f, 0), .42f, .12f, R.ink, 8);
            int segs = Mathf.Max(5, Mathf.RoundToInt(height / .95f));
            for (int i = 0; i < segs; i++)
            {
                Vector3 a = At(height * i / segs), b = At(height * (i + 1) / segs);
                Quaternion q = Quaternion.FromToRotation(Vector3.up, b - a);
                float r = .26f - .08f * i / segs, len = (b - a).magnitude;
                m.Surface = ComicSurface.Wood;
                m.Cylinder((a + b) * .5f, r, len * .9f, i % 2 == 0 ? trunk : trunkDark, 8, .9f, q);
                if (i < segs - 1) { m.Surface = ComicSurface.PaintedMetal; m.Cylinder(b, r * .92f + .03f, len * .14f, R.ink, 8, 1f, q); }
            }
            Vector3 top = At(height);
            Quaternion tq = Quaternion.FromToRotation(Vector3.up, top - At(height - .6f));
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(top + tq * V(0, .15f, 0), .3f, .5f, R.ink, 8, .65f, tq);                   // the ink boot
            m.Surface = ComicSurface.Wood;
            for (int k = 0; k < 3; k++) m.Box(top + Quaternion.Euler(0, k * 120f + seed * 20f, 0) * V(.27f, -.12f, 0), V(.22f, .22f, .22f), Hex("6E5238"));
            Color f0 = Hex("4F8A3A"), f1 = Hex("6FA84A"), under = Hex("335E27");
            float[] len4 = { .85f, .8f, .7f, .55f }, w4 = { .5f, .62f, .5f, .2f }, ang = { 38f, 8f, -24f, -55f };
            for (int i = 0; i < 9; i++)
            {
                Vector3 outDir = Quaternion.Euler(0, i * 40f + seed * 17f, 0) * Vector3.forward, side = Vector3.Cross(Vector3.up, outDir);
                Vector3 a = top + tq * V(0, .35f, 0) + outDir * .12f;
                float tilt = (i % 3) * 7f;
                for (int k = 0; k < 4; k++)
                {
                    float th = (ang[k] - tilt) * Mathf.Deg2Rad;
                    Vector3 b = a + (outDir * Mathf.Cos(th) + Vector3.up * Mathf.Sin(th)) * len4[k];
                    Slab(m, a, b + (b - a).normalized * .06f, side, k == 0 ? .3f : w4[k - 1], w4[k], .08f, i % 2 == 0 ? f0 : f1, under);
                    a = b;
                }
            }
            Thin(collisions, "Palm", p, height);
        }

        static void CafeTable(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .05f, 0), .3f, .1f, R.ink, 8);
            m.Cylinder(p + V(0, .4f, 0), .06f, .62f, R.ink, 8);
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, .75f, 0), .46f, .08f, R.cream, 8);
        }

        /// <summary>A cafe chair facing +Z at yaw 0: two ink leg frames, a thick plastic seat and backrest, ink top trim.</summary>
        public static void CafeChair(ComicMesh m, Vector3 p, float yaw, Color seat)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            void B(Vector3 c, Vector3 size, Color col, ComicSurface surface) { m.Surface = surface; m.Box(p + q * c, size, col, q); }
            B(V(0, .22f, .17f), V(.42f, .44f, .06f), R.ink, ComicSurface.PaintedMetal);
            B(V(0, .22f, -.17f), V(.42f, .44f, .06f), R.ink, ComicSurface.PaintedMetal);
            B(V(0, .48f, 0), V(.46f, .08f, .46f), seat, ComicSurface.Plastic);
            B(V(0, .76f, -.21f), V(.46f, .5f, .08f), seat, ComicSurface.Plastic);
            B(V(0, 1.03f, -.21f), V(.48f, .07f, .1f), R.ink, ComicSurface.PaintedMetal);
        }

        /// <summary>Cafe table with two chairs (facing each other across it, along x).</summary>
        public static void CafeSet(ComicMesh m, Vector3 p, Color seat)
        {
            CafeTable(m, p);
            CafeChair(m, p + V(-.72f, 0, 0), 90f, seat);
            CafeChair(m, p + V(.72f, 0, 0), -90f, seat);
        }

        /// <summary>Cafe table, two chairs and a market umbrella: 8-sided pole, closed canopy cone, valance band, finial.</summary>
        public static void CafeUmbrella(ComicMesh m, List<DressingObstacle> collisions, Vector3 p, Color canopy)
        {
            CafeSet(m, p, Hex("2F6B66"));
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 1.55f, 0), .05f, 1.6f, R.ink, 8);
            m.Surface = ComicSurface.Fabric;
            m.Cylinder(p + V(0, 2.5f, 0), 1.45f, .45f, canopy, 8, .08f);
            m.Cylinder(p + V(0, 2.22f, 0), 1.47f, .12f, Color.Lerp(canopy, R.ink, .3f), 8);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 2.8f, 0), .09f, .18f, R.cream, 8);
            collisions?.Add(new DressingObstacle { id = "CafeUmbrella", position = p, rotation = Vector3.zero, centre = V(0, .6f, 0), size = V(1.9f, 1.2f, .9f), canJumpOver = false });
        }

        public static void TrafficLight(ComicMesh m, List<DressingObstacle> collisions, int side, float y, float z, float half = Half)
        {
            Vector3 p = V(side * (half + .45f), y, z);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .08f, 0), .2f, .16f, R.ink, 8);
            m.Cylinder(p + V(0, 1.85f, 0), .08f, 3.5f, R.ink, 8);
            m.Box(p + V(0, 3.2f, 0), V(.46f, 1.2f, .34f), Hex("2C3238"));
            m.Box(p + V(0, 3.82f, 0), V(.5f, .08f, .38f), R.cream);
            Color[] lamps = { Hex("E8322B"), Hex("F3B23A"), Hex("3DBE5A") };
            for (int i = 0; i < 3; i++)
            {
                float ly = 3.56f - i * .36f;
                m.Surface = ComicSurface.Glass;
                m.Box(p + V(0, ly, -.18f), V(.24f, .24f, .06f), i == 2 ? lamps[2] : Color.Lerp(lamps[i], R.ink, .55f));
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(p + V(0, ly + .15f, -.27f), V(.32f, .06f, .18f), R.ink);                        // visor
            }
            m.Surface = ComicSurface.Plastic;
            m.Box(p + V(0, 1.15f, -.1f), V(.26f, .38f, .16f), R.yellow);                               // push-button box
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 1.2f, -.19f), V(.12f, .12f, .04f), R.ink);
            Thin(collisions, "TrafficLight", p, 3.6f, .3f);
        }

        // ---- prefab writing (generated in place: stable GUIDs and child names, like the street builders)

        public static Mesh SaveMesh(string id, ComicMesh data, bool curl = true)
        {
            string path = _meshFolder + "/" + id + ".asset";
            Mesh mesh = data.Bake(id), existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (curl)
            {
                // Culling happens on the CPU before the vertex shader curls the far city up (ComicCityBuilder.SaveMesh).
                Bounds b = mesh.bounds;
                b.Encapsulate(b.max + Vector3.up * (ComicCityProjection.Rise(R, 360) + 1));
                mesh.bounds = b;
            }
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

        public static void Renderer(GameObject go, string id, ComicMesh data, bool outline, bool shadows, bool curl = true)
        {
            Mesh mesh = SaveMesh(id, data, curl);
            PrefabBuildUtility.GetOrAdd<MeshFilter>(go).sharedMesh = mesh;
            var mr = PrefabBuildUtility.GetOrAdd<MeshRenderer>(go);
            mr.sharedMaterial = Palette;
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = true;
            var children = new ChildSync(go.transform);
            if (outline)
            {
                GameObject ink = children.Plain("InkOutline");
                PrefabBuildUtility.GetOrAdd<MeshFilter>(ink).sharedMesh = mesh;
                var imr = PrefabBuildUtility.GetOrAdd<MeshRenderer>(ink);
                imr.sharedMaterial = Ink; imr.shadowCastingMode = ShadowCastingMode.Off; imr.receiveShadows = false;
            }
            children.Finish();
        }

        static void Colliders(GameObject group, string prefix, List<Bounds> boxes)
        {
            var sync = new ChildSync(group.transform);
            for (int i = 0; i < boxes.Count; i++)
            {
                GameObject go = sync.Plain(prefix + i);
                go.transform.localPosition = boxes[i].center;
                var box = PrefabBuildUtility.GetOrAdd<BoxCollider>(go);
                box.center = Vector3.zero; box.size = boxes[i].size; box.isTrigger = false;
            }
            sync.Finish();
        }

        public static void WriteChunk(CqChunkDraft d, string path)
        {
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            var chunk = PrefabBuildUtility.GetOrAdd<RoadChunk>(root);
            chunk.length = d.Length; chunk.endHeight = d.Drop;
            chunk.halfWidth = d.Half; chunk.sidewalkWidth = Side; chunk.sidewalkHeight = Kerb; chunk.curbTransitionWidth = .3f;
            chunk.endHalfWidth = d.ChangesWidth ? d.EndHalf : 0f; chunk.widthChangeZ = d.WidthChangeZ;
            chunk.roadType = d.Type; chunk.weight = 1f; chunk.minDifficulty = 0;

            var road = new ComicMesh();
            road.Add(d.Road, Vector3.zero, Quaternion.identity, Vector3.one);
            road.Shear(d.Grade);
            road.ClearCreaseInk();
            var frontage = new ComicMesh();
            frontage.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one);
            if (d.SlopedFrontage.TriangleCount > 0)
            {
                var sloped = new ComicMesh();
                sloped.Add(d.SlopedFrontage, Vector3.zero, Quaternion.identity, Vector3.one);
                sloped.Shear(d.Grade);
                frontage.Add(sloped, Vector3.zero, Quaternion.identity, Vector3.one);
            }
            string id = "CHN_" + d.Id;
            Renderer(sync.Plain("RoadAndSidewalk"), id + "_Road", road, false, false);
            Renderer(sync.Plain("Frontage"), id + "_Frontage", frontage, true, true);
            GameObject furniture = sync.Plain("StreetFurniture");
            Renderer(furniture, id + "_Props", d.Props, true, true);
            var dressing = PrefabBuildUtility.GetOrAdd<StreetDressing>(root);
            dressing.surface = furniture.GetComponent<MeshFilter>();
            dressing.outline = furniture.transform.Find("InkOutline").GetComponent<MeshFilter>();
            dressing.layouts = new[] { dressing.surface.sharedMesh };
            dressing.collisionLayouts = new[] { new DressingCollisionLayout { obstacles = d.Collisions.ToArray() } };
            if (d.Deep.TriangleCount > 0) Renderer(sync.Plain("DeepCity"), id + "_Background", d.Deep, false, false);
            if (d.Landmark.TriangleCount > 0) Renderer(sync.Plain("Landmark"), id + "_Landmark", d.Landmark, true, false);
            Colliders(sync.Plain("BoundaryColliders"), "Wall", d.Walls);
            // Plain colliders (no Obstacle: the rider ignores them) where nobody may be placed: over water, on ramps.
            if (d.KeepOut.Count > 0) Colliders(sync.Plain("PeopleKeepOut"), "KeepOut", d.KeepOut);
            if (d.Ramps.Count > 0)
            {
                var ramps = new ChildSync(sync.Plain("Ramps").transform);
                foreach (CqRampSpec r in d.Ramps)
                {
                    GameObject go = ramps.Plain(r.Name);
                    go.transform.localPosition = new Vector3(r.X, d.Y(r.Z), r.Z);
                    go.transform.localRotation = Quaternion.Euler(0f, r.Yaw, 0f);
                    var ramp = PrefabBuildUtility.GetOrAdd<Ramp>(go);
                    ramp.width = r.Width; ramp.rise = r.Rise; ramp.height = r.Height; ramp.top = r.Top; ramp.launch = r.Launch; ramp.hangGravity = r.Hang;
                    ramp.lanes = Array.Empty<int>();
                }
                ramps.Finish();
            }
            if (d.Gaps.Count > 0)
            {
                var gaps = new ChildSync(sync.Plain("Gaps").transform);
                foreach (CqGapSpec g in d.Gaps)
                {
                    GameObject go = gaps.Plain(g.Name);
                    go.transform.localPosition = new Vector3(0f, d.Y(g.Z), g.Z);
                    var gap = PrefabBuildUtility.GetOrAdd<RoadGap>(go);
                    gap.length = g.Length; gap.width = g.Width; gap.word = g.Word; gap.water = g.Water;
                }
                gaps.Finish();
            }
            var slots = new ChildSync(sync.Plain("SpawnSlots").transform);
            foreach (Vector2 s in d.Slots.Concat(d.GiftSlots))
            {
                GameObject slot = slots.Plain("Row" + s.y.ToString(CultureInfo.InvariantCulture) + "_Lane" + s.x.ToString(CultureInfo.InvariantCulture));
                slot.transform.localPosition = new Vector3(s.x, d.Y(s.y), s.y);
                PrefabBuildUtility.GetOrAdd<SpawnSlot>(slot).kind = d.GiftSlots.Contains(s) ? SpawnKind.Gift : SpawnKind.Any;
            }
            slots.Finish();
            sync.Finish();
            int tris = PrefabBuildUtility.CountTriangles(root), renderers = root.GetComponentsInChildren<MeshRenderer>(true).Length;
            PrefabBuildUtility.SaveAndClose(root, path);
            Report.Add(id + " " + tris + " tris, " + renderers + " renderers");
            if (tris > TriangleBudget) throw new InvalidOperationException(id + " exceeds the 25k triangle chunk budget: " + tris);
            if (renderers > RendererBudget) throw new InvalidOperationException(id + " has " + renderers + " renderers (budget 8)");
        }

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        static GameObject Art(GameObject root, string meshId, ComicMesh data, bool outline, bool shadows)
        {
            var sync = new ChildSync(root.transform);
            GameObject art = sync.Plain("Art");
            Renderer(art, meshId, data, outline, shadows, false);
            sync.Finish();
            return art;
        }

        static readonly Dictionary<string, ComicMesh> SharedArt = new Dictionary<string, ComicMesh>();

        /// <summary>An obstacle prefab: Art (ComicPalette + ink), a BoxCollider sized <paramref name="size"/> (base at y 0).</summary>
        public static Obstacle SaveObstacle(string path, string meshId, ComicMesh art, Vector3 size, ObstacleKind kind, bool jump, bool punch, float weight)
        {
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            Art(root, meshId, art, true, kind != ObstacleKind.Person);
            var box = PrefabBuildUtility.KeepSingle<BoxCollider>(root) ?? root.AddComponent<BoxCollider>();
            box.center = new Vector3(0, size.y * .5f, 0); box.size = size; box.isTrigger = false;
            var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
            o.kind = kind; o.canJumpOver = jump; o.spansRoad = false; o.punchable = punch; o.police = false; o.animal = false; o.spawnWeight = weight;
            PrefabBuildUtility.SaveAndClose(root, path);
            Report.Add(System.IO.Path.GetFileNameWithoutExtension(path) + " " + art.TriangleCount * 2 + " tris");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Obstacle>();
        }

        /// <summary>A moving vehicle (Mover) for hazard patterns; front at +Z, placed with yaw 180 when oncoming.
        /// Pass art null to reuse the mesh saved under the same meshId.</summary>
        public static GameObject SaveVehicle(string path, string meshId, ComicMesh art, Vector3 size, float speed, float fraction)
        {
            if (art != null) SharedArt[meshId] = art;
            art = SharedArt[meshId];
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            Art(root, meshId, art, true, true);
            var box = PrefabBuildUtility.KeepSingle<BoxCollider>(root) ?? root.AddComponent<BoxCollider>();
            box.center = new Vector3(0, size.y * .5f, 0); box.size = size; box.isTrigger = false;
            var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
            o.kind = ObstacleKind.Vehicle; o.canJumpOver = false; o.spansRoad = false; o.punchable = false; o.police = false; o.animal = false; o.spawnWeight = 1f;
            var mv = PrefabBuildUtility.GetOrAdd<Mover>(root);
            mv.speed = speed; mv.fractionOfRiderSpeed = fraction;
            PrefabBuildUtility.SaveAndClose(root, path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>A ramp prefab for District.ramps: Ramp + art matching its numbers (no colliders).</summary>
        public static Ramp SaveRamp(string path, string meshId, float width, float rise, float height, float top, float launch, float hang, Color deck, Color side, bool stripes)
        {
            var spec = new CqRampSpec { X = 0, Z = 0, Width = width, Rise = rise, Height = height, Top = top, Launch = launch, Hang = hang };
            var art = new ComicMesh();
            RampMesh(art, spec, deck, side, stripes, 0f);
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            Art(root, meshId, art, true, true);
            var ramp = PrefabBuildUtility.GetOrAdd<Ramp>(root);
            ramp.width = width; ramp.rise = rise; ramp.height = height; ramp.top = top; ramp.launch = launch; ramp.hangGravity = hang;
            ramp.lanes = Array.Empty<int>();
            PrefabBuildUtility.SaveAndClose(root, path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Ramp>();
        }

        /// <summary>District.farScenery: decoration only (no colliders, no shadows, no outline).</summary>
        public static GameObject SaveFarScenery(string path, string meshId, ComicMesh art)
        {
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            GameObject go = sync.Plain("Scenery");
            Renderer(go, meshId, art, false, false);
            sync.Finish();
            PrefabBuildUtility.SaveAndClose(root, path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static HazardPattern Pattern(string folder, string name, float length, int minLevel, float weight, bool mirror, float meet,
            params (GameObject prefab, float x, float z, float yaw)[] entries) => Pattern(folder, name, length, minLevel, weight, mirror, meet, Half, entries);

        public static HazardPattern Pattern(string folder, string name, float length, int minLevel, float weight, bool mirror, float meet, float roadHalf,
            params (GameObject prefab, float x, float z, float yaw)[] entries)
        {
            string path = folder + "/" + name + ".asset";
            var p = LoadOrCreate<HazardPattern>(path);
            p.entries = entries.OrderBy(e => e.z).Select(e => new HazardPattern.Entry { prefab = e.prefab, x = e.x, z = e.z, yaw = e.yaw }).ToArray();
            p.length = length; p.minLevel = minLevel; p.weight = weight; p.allowMirror = mirror;
            p.turnDirection = 0; p.turnAtZ = 0; p.turnStrength = .8f; p.turnSeconds = 4f; p.meetAtZ = meet; p.roadHalfWidth = roadHalf;
            EditorUtility.SetDirty(p);
            return p;
        }

        /// <summary>People from the hazard kit (HZ_Ped_*), by look id; repeats make a look more common.</summary>
        public static GameObject[] People(TrackTheme theme, params string[] looks)
        {
            var list = new List<GameObject>();
            foreach (string look in looks)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(ComicCityBuilder.Prefabs + "/Hazards/HZ_Ped_" + look + ".prefab");
                if (go != null) list.Add(go);
            }
            if (list.Count == 0 && theme.pedestrians != null) list.AddRange(theme.pedestrians.Where(p => p != null));
            return list.ToArray();
        }

        /// <summary>Renders a grid of props on the light-blue preview background, like the comic city's 03-props.png
        /// (same camera angle and lighting as ComicCityPreview.RenderSheet). Items are prefabs or loose meshes.</summary>
        public static void RenderAtlas(string file, IList<(string name, GameObject prefab, ComicMesh mesh)> items)
        {
            ArtPreviewUtility.NewPreviewScene();
            const int tile = 300, cols = 6;
            int rows = Mathf.CeilToInt(items.Count / (float)cols);
            var sheet = new Texture2D(cols * tile, rows * tile, TextureFormat.RGB24, false);
            sheet.SetPixels(Enumerable.Repeat(new Color(.82f, .87f, .9f), sheet.width * sheet.height).ToArray());
            var temp = new List<Object>();
            for (int i = 0; i < items.Count; i++)
            {
                GameObject go;
                if (items[i].prefab != null) go = (GameObject)PrefabUtility.InstantiatePrefab(items[i].prefab);
                else
                {
                    go = new GameObject(items[i].name);
                    Mesh mesh = items[i].mesh.Bake(items[i].name);
                    temp.Add(mesh);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = Palette;
                    var ink = new GameObject("InkOutline");
                    ink.transform.SetParent(go.transform, false);
                    ink.AddComponent<MeshFilter>().sharedMesh = mesh;
                    ink.AddComponent<MeshRenderer>().sharedMaterial = Ink;
                }
                Bounds b = ArtPreviewUtility.WorldBounds(go);
                float extent = Mathf.Max(b.size.x, b.size.y, b.size.z);
                Vector3 aim = b.center;
                Camera cam = ArtPreviewUtility.CreateCamera(aim + new Vector3(1, .65f, 1.4f).normalized * extent * 2, aim, 32, 600);
                cam.orthographic = true; cam.orthographicSize = Mathf.Max(.6f, extent * .7f);
                Object.DestroyImmediate(ArtPreviewUtility.Render(cam, tile, tile));
                Texture2D tex = ArtPreviewUtility.Render(cam, tile, tile);
                sheet.SetPixels((i % cols) * tile, (rows - 1 - i / cols) * tile, tile, tile, tex.GetPixels());
                Object.DestroyImmediate(tex); Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(go);
            }
            sheet.Apply();
            ArtPreviewUtility.SavePng(sheet, file);
            Object.DestroyImmediate(sheet);
            foreach (Object o in temp) Object.DestroyImmediate(o);
        }

        public static void WriteReport(string file)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
            System.IO.File.WriteAllLines(file, Report, new System.Text.UTF8Encoding(false));
        }
    }
}
