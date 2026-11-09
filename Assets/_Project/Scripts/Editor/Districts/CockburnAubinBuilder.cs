using System;
using System.Collections.Generic;
using System.Linq;
using CityScooter.EditorTools.Art;
using CityScooter.Gameplay;
using CityScooter.Rendering;
using CityScooter.Track;
using UnityEditor;
using UnityEngine;
using K = CityScooter.EditorTools.Districts.CbdQuayKit;
using P = CityScooter.EditorTools.Districts.ParkRiverKit;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// South line, art agent 2 (v0.4): COCKBURN CENTRAL (order 100) and AUBIN GROVE (order 110), on the real ride south
    /// along the Kwinana Freeway and the Mandurah line.
    /// - COCKBURN CENTRAL: a town-centre street with traffic. The station bus port and the enclosed footbridge over the
    ///   street, the multi-storey car park, a big shopping centre (no brand: blank sign panels and a pylon), mid-rise
    ///   apartments with lime and teal balcony panels round a town square with shade sails and cafes, the Cockburn ARC with
    ///   its big curved roof and coloured waterslides. Trolleys (jumpable), A-frame signs, delivery vans, buses.
    /// - AUBIN GROVE: the shared path beside the freeway and the train line (PedestrianMall chunks: no cars) and an estate
    ///   street: the white station building and its footbridge over the path and the freeway, the Russell Road overpass,
    ///   rendered-brick houses with grey Colorbond roofs on white limestone retaining walls, bin day, the Thomsons Lake
    ///   wetland with paperbarks and reeds, western grey kangaroos (animals, they hop), and a wetland drain jump
    ///   (limestone kicker + road-wide water gap, SPLASH).
    /// Same kit as the CBD and the Quay (CbdQuayKit) plus the park kit's plants (ParkRiverKit). The district system owns
    /// gateway, ambience and the sky fields: never written here (placeholders only when an asset is first created).
    ///   Menu: CityScooter/Districts/Build Cockburn Central and Aubin Grove
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.CockburnAubinBuilder.BuildBatch (no atlas, -nographics ok)
    ///          -executeMethod CityScooter.EditorTools.Districts.CockburnAubinBuilder.BuildAndRenderAtlasBatch (needs graphics)
    /// </summary>
    public static class CockburnAubinBuilder
    {
        public const string CockburnId = "cockburn", AubinId = "aubingrove";
        public const string CockburnPrefabFolder = ComicCityBuilder.Prefabs + "/Districts/Cockburn";
        public const string CockburnMeshFolder = ComicCityBuilder.Root + "/Districts/Cockburn";
        public const string AubinPrefabFolder = ComicCityBuilder.Prefabs + "/Districts/AubinGrove";
        public const string AubinMeshFolder = ComicCityBuilder.Root + "/Districts/AubinGrove";
        public const string CockburnPatternFolder = CbdBuilder.DataFolder + "/cockburn";
        public const string AubinPatternFolder = CbdBuilder.DataFolder + "/aubingrove";
        public const string CockburnPath = CbdBuilder.DataFolder + "/District_Cockburn.asset";
        public const string AubinPath = CbdBuilder.DataFolder + "/District_AubinGrove.asset";
        public static readonly string[] CockburnChunkIds = { "Station", "Mall", "Square", "Arc", "Apartments" };
        public static readonly string[] AubinChunkIds = { "Path", "Estate", "Station", "Wetland", "Drain" };
        public static string CockburnChunkPath(string variant) => CockburnPrefabFolder + "/CHN_Cockburn_" + variant + ".prefab";
        public static string AubinChunkPath(string variant) => AubinPrefabFolder + "/CHN_AubinGrove_" + variant + ".prefab";

        /// <summary>Gentle downhill (per 40 m): the town centre a little, the coastal plain by the wetlands hardly at all.</summary>
        public const float CockburnDrop = -2.2f, AubinDrop = -1.6f;
        /// <summary>The wetland drain: a full-width limestone kicker, then open water across the whole path.</summary>
        public const float DrainStart = 6f, DrainRise = 6f, DrainHeight = 1.4f, DrainTop = 2f, DrainGap = 5f;
        const float DrainWater = -1.05f;

        // ---- palette (sRGB; sources in art-src/districts/cockburn|aubingrove/SOURCES.txt)
        static readonly Color Render = K.Hex("F2F0EA"), RenderWarm = K.Hex("EDE3D2"), Lime = K.Hex("9CC93A"), Teal = K.Hex("2F8C8A"),
            Charcoal = K.Hex("3C4148"), Timber = K.Hex("B07A4A"), Orange = K.Hex("E98A3B"), ShopGlass = K.Hex("33505E"),
            WinGlass = K.Hex("4F6F86"), Slab = K.Hex("D9D7D0"), PlantGrey = K.Hex("A9ADB1"), White = K.Hex("F4F4F0");
        static readonly Color Silver = K.Hex("B9BEC4"), SilverDark = K.Hex("8E959D"), Concrete = K.Hex("C9CBC8"), DeckDark = K.Hex("2C3238"),
            PortRed = K.Hex("A8473A"), SignBlue = K.Hex("2E5E9E");
        static readonly Color MallRender = K.Hex("D9CBB0"), MallBand = K.Hex("8C8478"), AtriumGlass = K.Hex("3F6E8C");
        static readonly Color ArcRoof = K.Hex("E8EBEE"), ArcCream = K.Hex("F1E6CE"), ArcTimber = K.Hex("B68A5A");
        static readonly Color SlideYellow = K.Hex("F3B23A"), SlideRed = K.Hex("E8322B"), SlideBlue = K.Hex("2E7FD0"), SlideGreen = K.Hex("4DB848");
        static readonly Color Limestone = K.Hex("E3D3AC"), LimestoneCap = K.Hex("EFE4C8"), LimeJoint = K.Hex("BFA97C");
        static readonly Color RoofMonument = K.Hex("4B4F54"), RoofBasalt = K.Hex("6E7276"), RoofWindspray = K.Hex("8C9195"), RoofTile = K.Hex("9A5A44");
        static readonly Color[] HouseWalls = { K.Hex("F1ECE2"), K.Hex("D9D2C3"), K.Hex("C8C3B8"), K.Hex("E6D7BF"), K.Hex("B5734F"), K.Hex("E9E4DA") };
        static readonly Color[] GarageDoors = { K.Hex("4B4F54"), K.Hex("8C9195"), K.Hex("E4E2D5"), K.Hex("6E7276") };
        static readonly Color Lawn = K.Hex("7DA24A"), LawnLight = K.Hex("94B456"), LawnDry = K.Hex("A7A85A"), DryGrass = K.Hex("C9B77A"),
            Sand = K.Hex("E6D9B5"), HedgeDark = K.Hex("4E7F3A"), HedgeLight = K.Hex("6A9A44"), Leaf = K.Hex("5E8F3E"), Peppermint = K.Hex("6B8F4E");
        static readonly Color Water = K.Hex("3A6F8C"), Ripple = K.Hex("6C9DB8"), Reed = K.Hex("7C8F3E"), ReedLight = K.Hex("9AA650"), Cattail = K.Hex("6B4A2E");
        static readonly Color Paperbark = K.Hex("EDE6D6"), PaperbarkPatch = K.Hex("C9B49A"), PaperLeaf = K.Hex("6F8F5A"), PaperLeafLight = K.Hex("8FAA6C"), PaperLeafDark = K.Hex("55704A");
        static readonly Color Roo = K.Hex("8A7B6C"), RooDark = K.Hex("5C5045"), RooBelly = K.Hex("D8CBB5");
        static readonly Color Haze = K.Hex("A9C1D3"), Range = K.Hex("6F8199");
        static readonly Color Footpath = K.Hex("CFCAC0"), StationWhite = K.Hex("F3F3EF"), BridgeClad = K.Hex("3D4F66"), BridgeGlass = K.Hex("7FA8C8");
        static readonly Color Ballast = K.Hex("8E8578"), RailSteel = K.Hex("5C6066"), Sleeper = K.Hex("6A5A4A"), Barrier = K.Hex("DAD7CF"),
            NoiseWall = K.Hex("D7C3A0"), NoiseWallDark = K.Hex("B89F78"), TrainSilver = K.Hex("CDD2D7");
        static readonly Color[] CarColours = { K.Hex("F4F4F0"), K.Hex("2E5E9E"), K.Hex("C9302C"), K.Hex("8C9195"), K.Hex("2C3238"), K.Hex("E8B23A"), K.Hex("4F7F6A") };

        // ================================================================ build

        [MenuItem("CityScooter/Districts/Build Cockburn Central and Aubin Grove")]
        public static void BuildMenu() => Build(true);

        public static void Build(bool atlas)
        {
            P.Init();
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme == null) throw new InvalidOperationException("No comic theme at " + ComicCityBuilder.ThemePath);
            BuildCockburn(theme, atlas);
            BuildAubin(theme, atlas);
            AssetDatabase.SaveAssets();
        }

        /// <summary>-executeMethod CityScooter.EditorTools.Districts.CockburnAubinBuilder.BuildBatch (works with -nographics).</summary>
        public static void BuildBatch()
        {
            try { Build(false); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Builds and renders both props atlases (needs graphics: no -nographics).</summary>
        public static void BuildAndRenderAtlasBatch()
        {
            try { Build(true); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static District LoadOrCreateDistrict(string path)
        {
            var d = AssetDatabase.LoadAssetAtPath<District>(path);
            if (d != null) return d;
            d = ScriptableObject.CreateInstance<District>();
            // Placeholders for the system's sky fields, set only when the asset is first made: these districts are far south
            // of the city, so the CBD skyline is turned behind the rider and sunk. Never written afterwards.
            d.skyYaw = 180f; d.skySize = .6f; d.skyHeight = -12f; d.skyline = 0f;
            AssetDatabase.CreateAsset(d, path);
            return d;
        }

        /// <summary>The street's own patterns (Data/Hazards: roadworks, police, crossings, cars). Cars never go on the shared
        /// path: the generator keeps patterns with movers off PedestrianMall chunks.</summary>
        static IEnumerable<HazardPattern> StreetPatterns(TrackTheme theme) => theme.hazards == null ? Enumerable.Empty<HazardPattern>() :
            theme.hazards.Where(h => h != null && AssetDatabase.GetAssetPath(h).StartsWith("Assets/_Project/Data/Hazards/")).Distinct();

        static RoadChunk LoadChunk(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<RoadChunk>();
        static ComicMesh M(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }

        static void BuildCockburn(TrackTheme theme, bool atlas)
        {
            K.Begin(CockburnMeshFolder, CockburnPrefabFolder, CbdBuilder.DataFolder, CockburnPatternFolder);
            string Pf(string n) => CockburnPrefabFolder + "/" + n + ".prefab";

            // ---- obstacles, traffic, ramp
            Obstacle trolley = K.SaveObstacle(Pf("OBS_Cockburn_Trolley"), "OBS_Cockburn_Trolley", TrolleyMesh(1), new Vector3(.72f, 1.05f, 1.05f), ObstacleKind.Thing, true, true, 1.4f);
            Obstacle trolleys = K.SaveObstacle(Pf("OBS_Cockburn_TrolleyTrain"), "OBS_Cockburn_TrolleyTrain", TrolleyMesh(4), new Vector3(.72f, 1.05f, 2.75f), ObstacleKind.Thing, true, false, .9f);
            Obstacle aframe = K.SaveObstacle(Pf("OBS_Cockburn_AFrame"), "OBS_Cockburn_AFrame", AFrameMesh(), new Vector3(.75f, 1.05f, .75f), ObstacleKind.Thing, true, true, 1f);
            Obstacle van = K.SaveObstacle(Pf("OBS_Cockburn_DeliveryVan"), "OBS_Cockburn_DeliveryVan", VanMesh(), new Vector3(2.05f, 2.6f, 5.3f), ObstacleKind.Vehicle, false, false, .5f);
            GameObject vanAhead = K.SaveVehicle(Pf("VEH_Cockburn_VanAhead"), "VEH_Cockburn_Van", VanMesh(), new Vector3(2.05f, 2.6f, 5.3f), 0f, .35f);
            GameObject busOncoming = K.SaveVehicle(Pf("VEH_Cockburn_BusOncoming"), "VEH_Cockburn_Bus", BusMesh(), new Vector3(2.55f, 3.3f, 12.2f), -10f, 0f);
            Ramp carPark = K.SaveRamp(Pf("RMP_Cockburn_CarParkRamp"), "RMP_Cockburn_CarParkRamp", 3.2f, 6f, 1.5f, 1.5f, 1f, .5f, K.Hex("C3C6C9"), K.Hex("8E9398"), true);

            // ---- chunks and far scenery
            K.WriteChunk(CockburnEntry(), CockburnChunkPath("Entry"));
            K.WriteChunk(CockburnStation(), CockburnChunkPath("Station"));
            K.WriteChunk(CockburnMall(), CockburnChunkPath("Mall"));
            K.WriteChunk(CockburnSquare(), CockburnChunkPath("Square"));
            K.WriteChunk(CockburnArc(), CockburnChunkPath("Arc"));
            K.WriteChunk(CockburnApartments(), CockburnChunkPath("Apartments"));
            K.WriteChunk(CockburnExit(), CockburnChunkPath("Exit"));
            GameObject far = K.SaveFarScenery(Pf("FAR_Cockburn_Skyline"), "FAR_Cockburn_Skyline", CockburnFar());

            var patterns = new List<HazardPattern>
            {
                K.Pattern(CockburnPatternFolder, "HZ_Cockburn_Trolleys", 45, 0, 1f, true, 30, (trolleys.gameObject, -2f, 18f, 0f), (trolley.gameObject, 1.2f, 26f, 25f), (trolley.gameObject, 2.9f, 33f, -20f)),
                K.Pattern(CockburnPatternFolder, "HZ_Cockburn_VanAhead", 90, 1, .8f, false, 45, (vanAhead, -2f, 45f, 0f)),
                K.Pattern(CockburnPatternFolder, "HZ_Cockburn_BusOncoming", 90, 1, .8f, false, 50, (busOncoming, 2f, 50f, 180f)),
            };

            District d = LoadOrCreateDistrict(CockburnPath);
            d.id = CockburnId; d.displayName = "COCKBURN CENTRAL"; d.order = 100; d.length = new Vector2(360f, 440f);
            d.chunks = CockburnChunkIds.Select(c => LoadChunk(CockburnChunkPath(c))).ToArray();
            d.orderedChunks = true;
            d.entry = LoadChunk(CockburnChunkPath("Entry"));
            d.exit = LoadChunk(CockburnChunkPath("Exit"));
            d.allowBoulevard = false;
            var obstacles = new List<Obstacle> { trolley, trolleys, aframe, van };
            if (theme.obstacles != null) obstacles.AddRange(theme.obstacles.Where(o => o != null && o.name == "OBS_Bin"));
            d.obstacles = obstacles.ToArray();
            d.jumpObstacles = Array.Empty<Obstacle>();
            d.gifts = Array.Empty<Gift>();
            d.hazards = StreetPatterns(theme).Concat(patterns).ToArray();
            d.pedestrians = K.People(theme, "Shopper", "Shopper", "Shopper", "Shopper", "Office", "Student", "Student", "Kid", "Elder", "DogWalker", "Jogger", "Courier", "Barista");
            d.ramps = new[] { carPark };
            d.rampFrequency = 1f;
            d.blendMeters = 150f;
            // Bright, open suburban town centre: clear blue haze, warm sun.
            d.fogColor = new Color(.72f, .82f, .90f);
            d.skyTint = new Color(.98f, .99f, 1f);
            d.ambientSky = new Color(.66f, .80f, .94f);
            d.ambientEquator = new Color(.86f, .84f, .78f);
            d.ambientGround = new Color(.60f, .56f, .48f);
            d.sunColor = new Color(1f, .96f, .88f);
            d.sunIntensity = 1.4f;
            d.farScenery = far;
            // gateway, ambience, ambienceVolume and the sky fields belong to the district system: not written here.
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            K.WriteReport("art-src/districts/cockburn/catalog.txt");
            Debug.Log("[Districts/Cockburn] built: " + string.Join("; ", K.Report));
            if (!atlas) return;
            K.RenderAtlas("art-src/districts/cockburn/props-atlas.png", new List<(string, GameObject, ComicMesh)>
            {
                ("YoungTree", null, M(m => YoungTree(m, null, Vector3.zero, 6f, 3))),
                ("Trolley", trolley.gameObject, null),
                ("TrolleyTrain", trolleys.gameObject, null),
                ("AFrame", aframe.gameObject, null),
                ("DeliveryVan", van.gameObject, null),
                ("Bus", busOncoming, null),
                ("CarParkRamp", carPark.gameObject, null),
                ("ShadeSails", null, M(m => ShadeSails(m, Vector3.zero, 0))),
                ("Pylon", null, M(m => Pylon(m, Vector3.zero))),
                ("SlideTower", null, M(m => SlideTower(m, Vector3.zero))),
                ("Car", null, CarMesh(CarColours[1])),
                ("Apartment", null, AtlasLot(dr => Apartment(dr, new CqLot(-1, 0, 16, 0f, 14, 0f), 5, Render, Lime, 0))),
                ("CarPark", null, AtlasLot(dr => CarParkBuilding(dr, new CqLot(-1, 0, 20, 0f, 16, 0f)))),
                ("Footbridge", null, M(m => { Footbridge(m, 7f, 0f, -12f, 12f, 3.4f); LiftTower(m, new Vector3(12f, 0f, 1.7f), 10.5f); })),
                ("ArcRoof", null, AtlasLot(dr => ArcBuilding(dr, new CqLot(-1, 0, 30, 0f, 30, 0f)))),
                ("Sign", null, M(m => BusStopSign(m, Vector3.zero))),
            });
        }

        static ComicMesh AtlasLot(Action<CqChunkDraft> build)
        {
            var dr = new CqChunkDraft("Atlas", 0f);
            build(dr);
            var m = new ComicMesh();
            m.Add(dr.Frontage, Vector3.zero, Quaternion.identity, Vector3.one);
            m.Add(dr.Landmark, Vector3.zero, Quaternion.identity, Vector3.one);
            return m;
        }

        static void BuildAubin(TrackTheme theme, bool atlas)
        {
            K.Begin(AubinMeshFolder, AubinPrefabFolder, CbdBuilder.DataFolder, AubinPatternFolder);
            string Pf(string n) => AubinPrefabFolder + "/" + n + ".prefab";

            Obstacle bins = K.SaveObstacle(Pf("OBS_AubinGrove_WheelieBins"), "OBS_AubinGrove_WheelieBins", WheelieBinsMesh(), new Vector3(1.45f, 1.1f, .75f), ObstacleKind.Thing, true, true, 1.4f);
            Obstacle letterbox = K.SaveObstacle(Pf("OBS_AubinGrove_Letterbox"), "OBS_AubinGrove_Letterbox", LetterboxMesh(), new Vector3(.55f, 1.05f, .55f), ObstacleKind.Thing, true, false, .8f);
            Obstacle rooSign = K.SaveObstacle(Pf("OBS_AubinGrove_RooSign"), "OBS_AubinGrove_RooSign", RooSignMesh(), new Vector3(.35f, 2.7f, .35f), ObstacleKind.Thing, false, false, .5f);
            GameObject roo = SaveKangaroo(Pf("ANM_AubinGrove_Kangaroo"), 1.1f);
            GameObject rooGrazing = SaveKangaroo(Pf("ANM_AubinGrove_KangarooGrazing"), 0f);
            Ramp kicker = SaveRampArt(Pf("RMP_AubinGrove_LimestoneKicker"), "RMP_AubinGrove_LimestoneKicker",
                new CqRampSpec { Width = 3f, Rise = 4.5f, Height = 1.1f, Top = 0f, Launch = 1.1f, Hang = .55f }, 0f);

            K.WriteChunk(AubinEntry(), AubinChunkPath("Entry"));
            K.WriteChunk(AubinSharedPath(), AubinChunkPath("Path"));
            K.WriteChunk(AubinEstate(), AubinChunkPath("Estate"));
            K.WriteChunk(AubinStation(), AubinChunkPath("Station"));
            K.WriteChunk(AubinWetland(), AubinChunkPath("Wetland"));
            K.WriteChunk(AubinDrain(), AubinChunkPath("Drain"));
            K.WriteChunk(AubinExit(), AubinChunkPath("Exit"));
            GameObject far = K.SaveFarScenery(Pf("FAR_AubinGrove_Wetland"), "FAR_AubinGrove_Wetland", AubinFar());

            var patterns = new List<HazardPattern>
            {
                // a mob of western greys on the path: one lane always free (they are not jumpable)
                K.Pattern(AubinPatternFolder, "HZ_AubinGrove_Roos", 45, 0, 1f, true, 25, (roo, -2.6f, 18f, 0f), (rooGrazing, -.5f, 24f, 0f), (roo, -3.3f, 29f, 0f)),
                K.Pattern(AubinPatternFolder, "HZ_AubinGrove_BinDay", 40, 0, .9f, true, 25, (bins.gameObject, -3.1f, 14f, 0f), (bins.gameObject, 3.1f, 22f, 0f), (bins.gameObject, -3.1f, 31f, 0f)),
            };

            District d = LoadOrCreateDistrict(AubinPath);
            d.id = AubinId; d.displayName = "AUBIN GROVE"; d.order = 110; d.length = new Vector2(360f, 440f);
            d.chunks = AubinChunkIds.Select(c => LoadChunk(AubinChunkPath(c))).ToArray();
            d.orderedChunks = true;
            d.entry = LoadChunk(AubinChunkPath("Entry"));
            d.exit = LoadChunk(AubinChunkPath("Exit"));
            d.allowBoulevard = false;
            d.obstacles = new[] { bins, letterbox, rooSign };
            d.jumpObstacles = Array.Empty<Obstacle>();
            d.gifts = Array.Empty<Gift>();
            d.hazards = StreetPatterns(theme).Concat(patterns).ToArray();
            var people = new List<GameObject>(K.People(theme, "Jogger", "Jogger", "Jogger", "DogWalker", "DogWalker", "Kid", "Elder", "Student", "Skater"));
            people.Add(roo); people.Add(roo); people.Add(rooGrazing);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { kicker };
            d.rampFrequency = 1.2f;
            d.blendMeters = 150f;
            // Dry, open outer suburb: warm sun, slightly green-grey haze off the wetlands.
            d.fogColor = new Color(.76f, .84f, .87f);
            d.skyTint = new Color(1f, .99f, .97f);
            d.ambientSky = new Color(.66f, .80f, .90f);
            d.ambientEquator = new Color(.84f, .84f, .74f);
            d.ambientGround = new Color(.62f, .58f, .44f);
            d.sunColor = new Color(1f, .95f, .84f);
            d.sunIntensity = 1.45f;
            d.farScenery = far;
            // gateway, ambience, ambienceVolume and the sky fields belong to the district system: not written here.
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            K.WriteReport("art-src/districts/aubingrove/catalog.txt");
            Debug.Log("[Districts/AubinGrove] built: " + string.Join("; ", K.Report));
            if (!atlas) return;
            K.RenderAtlas("art-src/districts/aubingrove/props-atlas.png", new List<(string, GameObject, ComicMesh)>
            {
                ("Paperbark", null, M(m => PaperbarkTree(m, Vector3.zero, 8f, 4))),
                ("PaperbarkSmall", null, M(m => PaperbarkTree(m, Vector3.zero, 6f, 9, true))),
                ("Reeds", null, M(m => Reeds(m, Vector3.zero, 1.2f, 2))),
                ("Peppermint", null, M(m => YoungTree(m, null, Vector3.zero, 5.5f, 7, false, Peppermint))),
                ("Kangaroo", roo, null),
                ("WheelieBins", bins.gameObject, null),
                ("Letterbox", letterbox.gameObject, null),
                ("RooSign", null, M(m => m.Add(RooSignMesh(), Vector3.zero, Quaternion.Euler(0, 200f, 0), Vector3.one))),
                ("LimestoneKicker", kicker.gameObject, null),
                ("House", null, AtlasLot(dr => House(dr, new CqLot(-1, 0, 14, 0f, 12, 0f), HouseWalls[0], RoofMonument, GarageDoors[1], true, false))),
                ("HouseTwoStorey", null, AtlasLot(dr => House(dr, new CqLot(-1, 0, 13, 0f, 12, 0f), HouseWalls[4], RoofBasalt, GarageDoors[2], false, true))),
                ("WallAndFence", null, M(m => { LimestoneWall(m, 1, 0f, 0f, 8f, 0f, 1.6f); ColorbondFence(m, 1, .25f, 0f, 8f, 1.6f, RoofBasalt); })),
                ("Station", null, M(m => m.Add(M(st => StationBuilding(st, Vector3.zero)), Vector3.zero, Quaternion.Euler(0, 180f, 0), Vector3.one))),
                ("Train", null, M(m => TrainCar(m, Vector3.zero, 18f, true))),
                ("Car", null, CarMesh(CarColours[2])),
                ("Banksia", null, M(m => P.Banksia(m, Vector3.zero, 1f, 3))),
            });
        }

        // ================================================================ COCKBURN CENTRAL chunks

        static CqChunkDraft CockburnDraft(string variant) => new CqChunkDraft("Cockburn_" + variant, CockburnDrop);

        static void TownRoad(CqChunkDraft d, bool crossing)
        {
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, K.R.pavement, K.R.cream, true);
            if (crossing)
                for (int x = -3; x <= 3; x++) K.Flat(d.Road, x - .27f, x + .27f, x - .27f, x + .27f, 1.2f, 4.6f, .036f, K.R.cream);
        }

        static CqChunkDraft CockburnEntry()
        {
            // Calm at z = 0 (low limestone walls, hedges, young trees), then the first apartment blocks from z = 16.
            var d = CockburnDraft("Entry");
            TownRoad(d, false);
            for (int s = -1; s <= 1; s += 2)
            {
                CalmEdge(d, s, 0, 15, 11 + s);
                Apartment(d, new CqLot(s, 16, 40, K.Edge, 15, d.Base(28)), s < 0 ? 4 : 5, s < 0 ? RenderWarm : Render, s < 0 ? Teal : Lime, s < 0 ? 1 : 0);
                Paving(d, s, 15, 40);
                DeepBlocks(d, s, 16, 40, 3 + s);
                K.Lamp(d.Props, d.Collisions, s, d.Ground(20f), 20f);
            }
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CockburnExit()
        {
            // Apartments first, then calm towards z = 40.
            var d = CockburnDraft("Exit");
            TownRoad(d, true);
            for (int s = -1; s <= 1; s += 2)
            {
                Apartment(d, new CqLot(s, 0, 24, K.Edge, 15, d.Base(12)), s < 0 ? 5 : 4, s < 0 ? Render : RenderWarm, s < 0 ? Lime : Orange, s < 0 ? 0 : 1);
                Paving(d, s, 0, 25);
                CalmEdge(d, s, 25, 40, 21 + s);
                DeepBlocks(d, s, 0, 24, 7 + s);
                K.Lamp(d.Props, d.Collisions, s, d.Ground(12f), 12f);
            }
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CockburnStation()
        {
            // Left: the bus port (long white canopy over the bus bays, a bus waiting), the station concourse behind it.
            // Over the street: the enclosed station footbridge to the lift tower on the right. Right: the multi-storey car
            // park, then apartments.
            var d = CockburnDraft("Station");
            TownRoad(d, false);
            BusPort(d, -1, 0, 40);
            ComicMesh lm = d.Landmark;
            const float bz = 21f, bw = 3.4f, bridgeY = 7.2f;
            Footbridge(lm, d.Y(bz) + bridgeY, bz, -38f, 13.4f, bw);
            LiftTower(lm, new Vector3(15.2f, d.Base(bz + bw * .5f), bz + bw * .5f), bridgeY + 3.6f);
            Concourse(lm, d, -1, 6f, 36f);
            CarParkBuilding(d, new CqLot(1, 0, 19, K.Edge, 18, d.Base(9.5f)));
            Apartment(d, new CqLot(1, 26, 40, K.Edge, 15, d.Base(33)), 5, Render, Lime, 1);
            Paving(d, 1, 19, 26);
            DeepBlocks(d, 1, 0, 40, 31);
            K.Lamp(d.Props, d.Collisions, 1, d.Ground(23f), 23f);
            K.PropFromKit(d.Props, d.Collisions, "BikeRack", new Vector3(K.Half + 3.2f, d.Ground(30f), 30f), -90f);
            BusStopSign(d.Props, new Vector3(-(K.Half + .5f), d.Ground(12f), 12f));
            K.Thin(d.Collisions, "BusStopSign", new Vector3(-(K.Half + .5f), d.Ground(12f), 12f), 2.6f, .3f);
            YoungTree(d.Props, d.Collisions, new Vector3(K.Half + 1.6f, d.Ground(4f), 4f), 5.5f, 41);
            YoungTree(d.Props, d.Collisions, new Vector3(K.Half + 1.6f, d.Ground(36f), 36f), 5.5f, 42);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CockburnMall()
        {
            // Right: the shopping centre car park (bays, parked cars, a trolley bay, young trees), the big box behind with
            // its glass entrance and blank sign panels, and the pylon sign. Left: apartments over cafes.
            var d = CockburnDraft("Mall");
            TownRoad(d, true);
            const float set = 10f;
            CarParkLot(d, 1, 0, 40, set, 51);
            ShoppingCentre(d, new CqLot(1, 0, 40, K.Edge + set, 32, d.Base(20)));
            Pylon(d.Landmark, new Vector3(K.Edge + 1.6f, d.Ground(5f), 5f));
            Apartment(d, new CqLot(-1, 0, 18, K.Edge, 15, d.Base(9)), 6, Render, Teal, 0);
            Apartment(d, new CqLot(-1, 20, 40, K.Edge, 15, d.Base(30)), 5, RenderWarm, Lime, 1);
            Paving(d, -1, 18, 20);
            DeepBlocks(d, -1, 0, 40, 52);
            for (float z = 6; z < 40; z += 14) YoungTree(d.Props, d.Collisions, new Vector3(-(K.Half + 1.6f), d.Ground(z), z), 5.5f, 53 + (int)z);
            K.Lamp(d.Props, d.Collisions, 1, d.Ground(16f), 16f);
            K.Lamp(d.Props, d.Collisions, -1, d.Ground(30f), 30f);
            d.Props.Add(AFrameMesh(), new Vector3(-(K.Edge - .9f), d.Ground(12f), 12f), Quaternion.Euler(0, 75f, 0), Vector3.one);
            d.Collisions.Add(new DressingObstacle { id = "AFrame", position = new Vector3(-(K.Edge - .9f), d.Ground(12f), 12f), rotation = new Vector3(0, 75f, 0), centre = new Vector3(0, .5f, 0), size = new Vector3(.7f, 1f, .7f), canJumpOver = true });
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CockburnSquare()
        {
            // The town square on the right: paving, shade sails over cafe tables, young trees and a lawn, a seven-storey
            // block facing it; apartments with timber-look panels on the left.
            var d = CockburnDraft("Square");
            TownRoad(d, false);
            const float set = 14f;
            CbdBuilder.Forecourt(d, 1, 0, 40, set);
            Apartment(d, new CqLot(1, 0, 40, K.Edge + set, 16, d.Base(20)), 7, Render, Orange, 0);
            ShadeSails(d.Props, new Vector3(K.Edge + 6f, d.Ground(12f), 12f), 0);
            ShadeSails(d.Props, new Vector3(K.Edge + 6.5f, d.Ground(27f), 27f), 1);
            for (float z = 8f; z < 36f; z += 9f) K.CafeSet(d.Props, new Vector3(K.Edge + 5f + (z % 2f), d.Ground(z), z), z % 18f < 9f ? Teal : Orange);
            LawnBed(d, 1, 33f, 39f, K.Edge + 2f, K.Edge + 10f);
            YoungTree(d.Props, d.Collisions, new Vector3(K.Edge + 4f, d.Ground(36f), 36f), 6f, 61);
            YoungTree(d.Props, null, new Vector3(K.Edge + 9f, d.Ground(4f), 4f), 6.5f, 62);
            YoungTree(d.Props, null, new Vector3(K.Edge + 11f, d.Ground(20f), 20f), 6f, 63);
            K.PropFromKit(d.Props, d.Collisions, "Bench", new Vector3(K.Half + 3.4f, d.Ground(18f), 18f), -90f);
            Apartment(d, new CqLot(-1, 0, 22, K.Edge, 15, d.Base(11)), 6, RenderWarm, Timber, 1);
            Apartment(d, new CqLot(-1, 24, 40, K.Edge, 15, d.Base(32)), 5, Render, Lime, 0);
            Paving(d, -1, 22, 24);
            DeepBlocks(d, -1, 0, 40, 64);
            DeepBlocks(d, 1, 0, 40, 65, 14f);
            K.Lamp(d.Props, d.Collisions, -1, d.Ground(14f), 14f);
            K.Lamp(d.Props, d.Collisions, 1, d.Ground(32f), 32f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CockburnArc()
        {
            // The Cockburn ARC on the right behind a lawn: glass ground floor, cream and timber bands, the big curved roof,
            // the waterslide tower with yellow, blue and red tubes. Apartments on the left.
            var d = CockburnDraft("Arc");
            TownRoad(d, true);
            const float set = 12f;
            Ground(d, 1, K.Edge, K.Edge + set + 2f, K.Kerb - .02f, Lawn, 0, 40);
            P.Patches(d.Road, P.Rng(71), 1, K.Edge + .5f, K.Edge + set, K.Kerb - .02f, LawnLight, LawnDry, 0, 40, 6);
            PathStrip(d, 1, 16f, 19f, K.Edge, K.Edge + set);
            ArcBuilding(d, new CqLot(1, 2, 40, K.Edge + set, 34, d.Base(21)));
            SlideTower(d.Landmark, new Vector3(K.Edge + 5f, d.Ground(33f), 33f));
            YoungTree(d.Props, null, new Vector3(K.Edge + 3f, d.Ground(5f), 5f), 6f, 72);
            YoungTree(d.Props, null, new Vector3(K.Edge + 6f, d.Ground(11f), 11f), 5.5f, 73);
            YoungTree(d.Props, null, new Vector3(K.Edge + 3f, d.Ground(25f), 25f), 6f, 74);
            Apartment(d, new CqLot(-1, 0, 18, K.Edge, 15, d.Base(9)), 5, Render, Lime, 1);
            Apartment(d, new CqLot(-1, 20, 40, K.Edge, 15, d.Base(30)), 4, RenderWarm, Teal, 0);
            Paving(d, -1, 18, 20);
            DeepBlocks(d, -1, 0, 40, 75);
            K.Lamp(d.Props, d.Collisions, 1, d.Ground(9f), 9f);
            K.Lamp(d.Props, d.Collisions, -1, d.Ground(26f), 26f);
            YoungTree(d.Props, d.Collisions, new Vector3(-(K.Half + 1.6f), d.Ground(6f), 6f), 5.5f, 76);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CockburnApartments()
        {
            var d = CockburnDraft("Apartments");
            TownRoad(d, false);
            Apartment(d, new CqLot(-1, 0, 20, K.Edge, 15, d.Base(10)), 6, Render, Lime, 0);
            Apartment(d, new CqLot(-1, 20, 40, K.Edge, 15, d.Base(30)), 5, RenderWarm, Orange, 1);
            Apartment(d, new CqLot(1, 0, 14, K.Edge, 15, d.Base(7)), 4, RenderWarm, Teal, 1);
            Apartment(d, new CqLot(1, 16, 40, K.Edge, 15, d.Base(28)), 6, Render, Timber, 0);
            Paving(d, 1, 14, 16);
            for (int s = -1; s <= 1; s += 2)
            {
                DeepBlocks(d, s, 0, 40, 80 + s);
                for (float z = s < 0 ? 5 : 12; z < 40; z += 15) YoungTree(d.Props, d.Collisions, new Vector3(s * (K.Half + 1.6f), d.Ground(z), z), 5.5f, 82 + (int)z + s);
                K.Lamp(d.Props, d.Collisions, s, d.Ground(s < 0 ? 26f : 34f), s < 0 ? 26f : 34f);
            }
            d.Props.Add(AFrameMesh(), new Vector3(K.Edge - .9f, d.Ground(22f), 22f), Quaternion.Euler(0, -70f, 0), Vector3.one);
            d.Collisions.Add(new DressingObstacle { id = "AFrame", position = new Vector3(K.Edge - .9f, d.Ground(22f), 22f), rotation = new Vector3(0, -70f, 0), centre = new Vector3(0, .5f, 0), size = new Vector3(.7f, 1f, .7f), canJumpOver = true });
            K.CafeSet(d.Props, new Vector3(-(K.Edge - 1.2f), d.Ground(8f), 8f), Teal);
            d.Collisions.Add(new DressingObstacle { id = "CafeSet", position = new Vector3(-(K.Edge - 1.2f), d.Ground(8f), 8f), rotation = Vector3.zero, centre = new Vector3(0, .5f, 0), size = new Vector3(1.8f, 1f, .7f), canJumpOver = true });
            d.StandardSlots();
            return d;
        }

        // ================================================================ COCKBURN pieces

        /// <summary>Light pavers from the sidewalk to the building line (gaps between blocks), sheared with the road.</summary>
        static void Paving(CqChunkDraft d, int s, float z0, float z1, float depth = 16f) =>
            Ground(d, s, K.Edge, K.Edge + depth, K.Kerb - .01f, Color.Lerp(K.R.pavement, K.R.cream, .2f), z0, z1, ComicSurface.Paving);

        /// <summary>Flat ground beside the sidewalk on side s, |x| = a..b, y relative to the road (sheared with it).</summary>
        static void Ground(CqChunkDraft d, int s, float a, float b, float y, Color c, float z0, float z1, ComicSurface surface = ComicSurface.Foliage)
        {
            d.Road.Surface = surface;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.Face(d.Road, K.V(s * a, y, z), K.V(s * a, y, e), K.V(s * b, y, e), K.V(s * b, y, z), c, Vector3.up, false);
            }
        }

        static void PathStrip(CqChunkDraft d, int s, float z0, float z1, float a, float b)
        {
            d.Road.Surface = ComicSurface.Paving;
            K.Face(d.Road, K.V(s * a, K.Kerb + .005f, z0), K.V(s * a, K.Kerb + .005f, z1), K.V(s * b, K.Kerb + .005f, z1), K.V(s * b, K.Kerb + .005f, z0), Footpath, Vector3.up, false);
        }

        /// <summary>Calm end: a low limestone wall right behind the sidewalk, a clipped hedge, lawn, young street trees in
        /// pits, park trees behind.</summary>
        static void CalmEdge(CqChunkDraft d, int s, float z0, float z1, int seed)
        {
            ComicMesh m = d.SlopedFrontage;
            LimestoneWall(m, s, K.Edge + .05f, z0, z1, -1.2f, K.Kerb + .7f);
            m.Surface = ComicSurface.Foliage;
            K.SideBox(m, s, z0 + .3f, z1 - .3f, K.Kerb + .3f, K.Kerb + 1.45f, .75f, 1.8f, HedgeDark);
            K.SideBox(m, s, z0 + .3f, z1 - .3f, K.Kerb + 1.45f, K.Kerb + 1.6f, .8f, 1.75f, HedgeLight);
            Ground(d, s, K.Edge, K.Edge + 50f, K.Kerb - .02f, Lawn, z0, z1);
            P.Patches(d.Road, P.Rng(seed), s, K.Edge + 2f, K.Edge + 30f, K.Kerb - .02f, LawnLight, LawnDry, z0, z1, 5);
            int i = 0;
            for (float z = z0 + 3f; z < z1 - 1f; z += 6.5f, i++) YoungTree(d.Props, d.Collisions, new Vector3(s * 5.7f, d.Ground(z), z), 5.6f, seed * 13 + i);
            for (float z = z0 + 2f; z < z1; z += 6f)
                for (int row = 0; row < 2; row++)
                    DeepTree(d.Deep, new Vector3(s * (K.Edge + 6f + row * 9f + (z * 1.7f) % 3f), d.Ground(z), z + row * 2.5f), 7f + row * 1.5f, seed + (int)z + row);
            d.Walls.Add(new Bounds(new Vector3(s * (K.Edge + 7f), d.Y((z0 + z1) * .5f) + 1f, (z0 + z1) * .5f), new Vector3(13.5f, 4f, z1 - z0)));
        }

        /// <summary>A mid-rise apartment block: cafe / shop glass under an awning, white render, balconies with coloured
        /// balustrade panels, a charcoal stair core, parapet and roof plant.</summary>
        static void Apartment(CqChunkDraft d, CqLot l, int floors, Color body, Color accent, int style)
        {
            ComicMesh m = d.Frontage;
            float W = l.W, D = l.Depth, gh = 4f, fh = 3.1f, h = gh + floors * fh;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            d.Wall(l, h);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, W, 0, h, 0, -D, body);
            // ground floor: framed dark glass, mullions, a charcoal awning and a plain sign band
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, .4f, W - .4f, .15f, 3.3f, .02f, K.R.ink);
            K.LQuad(m, l, .55f, W - .55f, .25f, 3.15f, .04f, ShopGlass);
            m.Surface = ComicSurface.PaintedMetal;
            for (float a = 3f; a < W - 1f; a += 3f) K.LBox(m, l, a - .06f, a + .06f, .25f, 3.15f, .1f, 0, K.R.ink);
            K.LBox(m, l, -.05f, W + .05f, 3.4f, 3.65f, 2.2f, 0, Charcoal);
            K.LBox(m, l, W * .15f, W * .45f, 3.7f, 3.98f, .12f, 0, accent);
            // upper floors
            int bays = Mathf.Max(2, Mathf.RoundToInt(W / 4.2f));
            float bw = W / bays;
            for (int f = 0; f < floors; f++)
            {
                float y = gh + f * fh;
                m.Surface = ComicSurface.Concrete;
                K.LBox(m, l, -.04f, W + .04f, y - .12f, y + .1f, .12f, -D, Slab);
                for (int b = 0; b < bays; b++)
                {
                    float a0 = b * bw + .3f, a1 = (b + 1) * bw - .3f;
                    m.Surface = ComicSurface.Glass;
                    K.LQuad(m, l, a0 + .25f, a1 - .25f, y + .45f, y + 2.75f, .02f, K.R.ink);
                    K.LQuad(m, l, a0 + .35f, a1 - .35f, y + .55f, y + 2.65f, .04f, WinGlass);
                    if ((b + f + style) % 2 != 0) continue;
                    m.Surface = ComicSurface.Concrete;
                    K.LBox(m, l, a0, a1, y + .1f, y + .32f, 1.35f, 0, Slab);
                    m.Surface = ComicSurface.PaintedMetal;
                    K.LBox(m, l, a0, a1, y + .32f, y + 1.3f, 1.35f, 1.22f, accent);
                }
            }
            // stair core (charcoal) at one end, rising above the roof
            m.Surface = ComicSurface.PaintedMetal;
            float c0 = style % 2 == 0 ? .2f : W - 2.6f;
            K.LBox(m, l, c0, c0 + 2.4f, gh, h + 1.4f, .35f, -D * .4f, Charcoal);
            for (int f = 0; f < floors; f++) K.LQuad(m, l, c0 + .9f, c0 + 1.5f, gh + f * fh + .6f, gh + f * fh + 2.4f, .37f, WinGlass);
            // parapet and roof plant
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, -.08f, W + .08f, h, h + .6f, .1f, -D, Color.Lerp(body, K.R.ink, .1f));
            m.Surface = ComicSurface.Metal;
            K.LBox(m, l, W * .4f, W * .7f, h + .6f, h + 1.8f, -3f, -D * .6f, PlantGrey);
        }

        /// <summary>Second rows of blocks behind the street (no outline, no shadows): render with coloured balcony stripes.</summary>
        static void DeepBlocks(CqChunkDraft d, int s, float z0, float z1, int seed, float extra = 0f)
        {
            ComicMesh m = d.Deep;
            Color[] accents = { Lime, Teal, Orange, Timber, SignBlue };
            for (float z = z0; z < z1 - 1f; z += 14f)
            {
                int k = Mathf.Abs(seed * 7 + (int)z * 3);
                float zc = Mathf.Min(z + 7f, z1 - 4f), len = Mathf.Min(13f, z1 - z - .5f);
                for (int layer = 0; layer < 2; layer++)
                {
                    float h = 12f + (k * 7 + layer * 11) % 14, w = 14f;
                    float x = s * (K.Edge + extra + 26f + layer * 18f + (k % 3) * 2f);
                    Color c = Color.Lerp(k % 2 == 0 ? Render : RenderWarm, Haze, .1f + layer * .2f), a = Color.Lerp(accents[(k + layer) % accents.Length], Haze, .15f + layer * .2f);
                    float y = d.Ground(zc) - 2f;
                    m.Surface = ComicSurface.Plaster;
                    m.Box(new Vector3(x, y + h * .5f, zc), new Vector3(w, h, len), c);
                    for (float by = 5f; by < h - 1f; by += 3.1f)
                    {
                        m.Box(new Vector3(x - s * .1f, y + by, zc), new Vector3(w + .2f, .25f, len + .1f), Color.Lerp(c, K.R.ink, .2f));
                        m.Box(new Vector3(x - s * (w * .5f + .25f), y + by + .6f, zc - len * .2f), new Vector3(.5f, .9f, len * .35f), a);
                    }
                    m.Box(new Vector3(x, y + h + .6f, zc), new Vector3(w * .5f, 1.2f, len * .4f), PlantGrey);
                    k += 5;
                }
            }
        }

        static void DeepTree(ComicMesh m, Vector3 p, float h, int seed)
        {
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + K.V(0, h * .25f, 0), .2f, h * .5f, K.Hex("7A6650"), 6, .75f);
            P.Plant(m, p + K.V(0, h * .6f, 0), h * .62f, Color.Lerp(Leaf, K.R.ink, .08f * (seed % 3)), Color.Lerp(Leaf, K.R.yellow, .2f));
        }

        /// <summary>A young street tree: tree pit, slim trunk with two timber stakes, a layered two-tone crown.</summary>
        static void YoungTree(ComicMesh m, List<DressingObstacle> collisions, Vector3 p, float h, int seed, bool pit = true, Color? leaf = null)
        {
            var r = P.Rng(seed);
            Color g = leaf ?? Leaf;
            if (pit) K.TreePit(m, p, 1.2f);
            float trunk = h * .46f;
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + K.V(0, trunk * .5f + .1f, 0), .1f + h * .012f, trunk, K.Hex("7A6650"), 8, .75f);
            if (pit)
            {
                m.Box(p + K.V(.3f, .8f, 0), K.V(.06f, 1.5f, .06f), K.Hex("C9A878"));
                m.Box(p + K.V(-.3f, .8f, 0), K.V(.06f, 1.5f, .06f), K.Hex("C9A878"));
                m.Box(p + K.V(0, 1.3f, 0), K.V(.66f, .05f, .05f), K.Hex("C9A878"));
            }
            Vector3 c = p + K.V(0, trunk + h * .2f, 0);
            K.Plant(m, c, h * .5f, g);
            K.Plant(m, c + K.V(P.F(r, -.12f, .12f) * h, h * .18f, P.F(r, -.1f, .1f) * h), h * .32f, Color.Lerp(g, K.R.yellow, .1f));
            if (collisions != null) K.Thin(collisions, "YoungTree", p, h);
        }

        /// <summary>The station bus port on side s: bus bays at road level beyond the sidewalk, a long white canopy on silver
        /// posts, red paving islands, a bus waiting.</summary>
        static void BusPort(CqChunkDraft d, int s, float z0, float z1)
        {
            Ground(d, s, K.Edge, K.Edge + 9f, .01f, K.R.asphalt, z0, z1, ComicSurface.Asphalt);
            Ground(d, s, K.Edge + 9f, K.Edge + 12f, K.Kerb - .01f, PortRed, z0, z1, ComicSurface.Paving);
            Ground(d, s, K.Edge + 12f, K.Edge + 40f, K.Kerb - .02f, Color.Lerp(K.R.pavement, K.R.cream, .2f), z0, z1, ComicSurface.Paving);
            for (float z = z0 + 2f; z < z1 - 2f; z += 12f) K.Flat(d.Road, s * (K.Edge + 1f), s * (K.Edge + 8f), s * (K.Edge + 1f), s * (K.Edge + 8f), z, z + .12f, .03f, K.R.yellow);
            ComicMesh m = d.SlopedFrontage;
            // kerb face of the bus bays (the sidewalk edge already has one) and the canopy, sheared with the street
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = z0 + 2f; z < z1 - 1f; z += 8f)
                foreach (float x in new[] { K.Edge + 9.6f, K.Edge + 11.4f })
                    m.Cylinder(K.V(s * x, 2.4f, z), .14f, 4.8f, Silver, 8);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.MinMaxBox(m, K.V(s * (K.Edge + 3.5f), 4.8f, z), K.V(s * (K.Edge + 12.6f), 5.05f, e), White);
                K.MinMaxBox(m, K.V(s * (K.Edge + 3.4f), 4.65f, z), K.V(s * (K.Edge + 3.6f), 5.1f, e), Charcoal);
                K.MinMaxBox(m, K.V(s * (K.Edge + 12.5f), 4.65f, z), K.V(s * (K.Edge + 12.7f), 5.1f, e), Charcoal);
                for (float x = K.Edge + 4.2f; x < K.Edge + 12.4f; x += 1.4f)
                    K.MinMaxBox(m, K.V(s * x - .04f, 4.72f, z), K.V(s * x + .04f, 4.8f, e), SilverDark);
            }
            // benches on the island
            for (float z = z0 + 6f; z < z1 - 2f; z += 16f) K.PropFromKit(d.Props, null, "Bench", new Vector3(s * (K.Edge + 10.5f), d.Ground(z), z), s < 0 ? 90f : -90f);
            d.Props.Add(BusMesh(), new Vector3(s * (K.Edge + 4.6f), d.Y(18f) + .01f, 18f), Quaternion.identity, Vector3.one);
            d.Walls.Add(new Bounds(new Vector3(s * (K.Edge + 6f), d.Y((z0 + z1) * .5f) + 2f, (z0 + z1) * .5f), new Vector3(12f, 6f, z1 - z0)));
        }

        /// <summary>The station concourse behind the bus port: silver cladding, a glass band and a deep white roof.</summary>
        static void Concourse(ComicMesh m, CqChunkDraft d, int s, float z0, float z1)
        {
            float x0 = K.Edge + 21f, x1 = K.Edge + 36f, zc = (z0 + z1) * .5f, y = d.Base(zc);
            m.Surface = ComicSurface.Metal;
            K.MinMaxBox(m, K.V(s * x0, y - 2f, z0), K.V(s * x1, y + 6.5f, z1), Silver);
            m.Surface = ComicSurface.Glass;
            K.MinMaxBox(m, K.V(s * (x0 - .05f), y + .2f, z0 + 1f), K.V(s * (x0 + 1f), y + 3.4f, z1 - 1f), ShopGlass);
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = z0 + 1f; z < z1 - .5f; z += 4.5f) K.MinMaxBox(m, K.V(s * (x0 - .12f), y + .2f, z - .1f), K.V(s * (x0 + .2f), y + 6.5f, z + .1f), SilverDark);
            K.MinMaxBox(m, K.V(s * (x0 - 2f), y + 6.5f, z0 - 1f), K.V(s * (x1 + 1f), y + 7.1f, z1 + 1f), White);
            K.MinMaxBox(m, K.V(s * (x0 - 2.05f), y + 6.3f, z0 - 1.05f), K.V(s * (x1 + 1.05f), y + 6.55f, z1 + 1.05f), Charcoal);
            LiftTower(m, new Vector3(s * (x0 + 3f), y, 22.7f), 10.8f);
        }

        /// <summary>An enclosed footbridge across x0..x1 at deck height y0, z..z+width: concrete deck, charcoal lower walls,
        /// a glass band, a roof, and a white truss (posts and diagonals) on both faces.</summary>
        static void Footbridge(ComicMesh m, float y0, float z, float x0, float x1, float width)
        {
            float y1 = y0 + 3.2f;
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, K.V(x0, y0 - .5f, z - .1f), K.V(x1, y0, z + width + .1f), Concrete);
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, K.V(x0, y0, z), K.V(x1, y0 + 1.1f, z + width), BridgeClad);
            m.Surface = ComicSurface.Glass;
            K.MinMaxBox(m, K.V(x0, y0 + 1.1f, z + .06f), K.V(x1, y1 - .35f, z + width - .06f), BridgeGlass);
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, K.V(x0, y1 - .35f, z - .2f), K.V(x1, y1, z + width + .2f), BridgeClad);
            const float step = 4f;
            foreach (float fz in new[] { z - .1f, z + width + .1f })
                for (float x = x0; x < x1 - .1f; x += step)
                {
                    float xb = Mathf.Min(x1, x + step);
                    m.Box(K.V(x, (y0 + y1) * .5f, fz), K.V(.2f, y1 - y0, .14f), White);
                    m.Beam(K.V(x, y0 + .2f, fz), K.V(xb, y1 - .3f, fz), .14f, White);
                }
        }

        /// <summary>A lift and stair tower: silver box, a vertical glass strip, a white cap.</summary>
        static void LiftTower(ComicMesh m, Vector3 p, float h)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(p + K.V(0, -.9f, 0), K.V(4f, 2f, 4.4f), K.R.ink);
            m.Surface = ComicSurface.Metal;
            m.Box(p + K.V(0, h * .5f, 0), K.V(3.6f, h, 4f), Silver);
            m.Surface = ComicSurface.Glass;
            m.Box(p + K.V(0, h * .5f, 0), K.V(3.7f, h - 1.2f, 1.2f), ShopGlass);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + K.V(0, h + .25f, 0), K.V(4.2f, .5f, 4.6f), White);
        }

        /// <summary>Multi-storey car park: open decks with concrete slab edges and parapets, columns, a stair core, the
        /// entry portal with yellow-black chevrons, a ramp down the side and a round blue P sign (shape, not a letter).</summary>
        static void CarParkBuilding(CqChunkDraft d, CqLot l)
        {
            ComicMesh m = d.Frontage;
            float W = l.W, D = l.Depth, fh = 3f;
            const int floors = 5;
            float h = floors * fh;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            d.Wall(l, h);
            K.LBox(m, l, .2f, W - .2f, 0, h, -.9f, -D, DeckDark);
            for (int f = 0; f <= floors; f++)
            {
                float y = f * fh;
                K.LBox(m, l, 0, W, y, y + .45f, 0, -D, Concrete);
                if (f > 0) K.LBox(m, l, 0, W, y + .45f, y + 1.15f, .05f, -.3f, f % 2 == 0 ? White : Concrete);
            }
            for (float a = 0; a <= W + .01f; a += 6.3f) K.LBox(m, l, Mathf.Max(0, a - .35f), Mathf.Min(W, a + .35f), 0, h, .1f, -D, Concrete);
            // stair core with silver cladding and a teal stripe
            m.Surface = ComicSurface.Metal;
            K.LBox(m, l, W - 3.4f, W, 0, h + 2.4f, .4f, -4f, Silver);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, W - 3.45f, W + .05f, h + 1.2f, h + 1.7f, .45f, -4.05f, Teal);
            // entry portal with chevrons
            float p0 = 1.5f, p1 = 8f;
            K.LQuad(m, l, p0, p1, 0f, 2.6f, .2f, K.R.ink);
            for (int i = 0; i < 8; i++)
            {
                float a = p0 + (p1 - p0) * i / 8f;
                K.LQuad(m, l, a, a + (p1 - p0) / 8f, 2.6f, 3f, .21f, i % 2 == 0 ? K.R.yellow : K.R.ink);
            }
            // the round P sign: blue disc, white bar shapes
            m.Surface = ComicSurface.PaintedMetal;
            Vector3 c = l.P(W * .5f, 11f, .5f);
            m.Cylinder(c, 1.4f, .2f, SignBlue, 12, 1f, Quaternion.Euler(0, 0, 90));
            K.LBox(m, l, W * .5f - .25f, W * .5f + .2f, 10.1f, 11.9f, .62f, .52f, White);
            K.LBox(m, l, W * .5f - .25f, W * .5f + .6f, 11.5f, 11.9f, .62f, .52f, White);
            K.LBox(m, l, W * .5f + .4f, W * .5f + .6f, 11f, 11.9f, .62f, .52f, White);
            K.LBox(m, l, W * .5f - .25f, W * .5f + .6f, 10.9f, 11.2f, .62f, .52f, White);
        }

        /// <summary>Shopping centre car park between the sidewalk and the set-back mall: bays, parked cars, a trolley bay,
        /// young trees in islands, light poles.</summary>
        static void CarParkLot(CqChunkDraft d, int s, float z0, float z1, float depth, int seed)
        {
            Ground(d, s, K.Edge, K.Edge + depth, .02f, K.R.asphalt, z0, z1, ComicSurface.Asphalt);
            d.SlopedFrontage.Surface = ComicSurface.Concrete;
            K.SideBox(d.SlopedFrontage, s, z0, z1, -.6f, K.Kerb, -.05f, .2f, K.R.cream);
            for (float z = z0 + 1f; z < z1 - 1f; z += 2.6f)
                K.Flat(d.Road, s * (K.Edge + 4.2f), s * (K.Edge + 9.4f), s * (K.Edge + 4.2f), s * (K.Edge + 9.4f), z, z + .1f, .035f, White);
            var r = P.Rng(seed);
            for (float z = z0 + 2.3f; z < z1 - 2f; z += 5.2f)
            {
                if (P.F(r, 0f, 1f) < .35f) continue;
                d.Props.Add(CarMesh(CarColours[r.Next(CarColours.Length)]), new Vector3(s * (K.Edge + 6.8f), d.Y(z) + .02f, z), Quaternion.Euler(0, s * 90f, 0), Vector3.one);
            }
            // trolley bay: a roofed rail with trolleys inside
            Vector3 tb = new Vector3(s * (K.Edge + 2.2f), d.Y(20f), 20f);
            ComicMesh m = d.Props;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float dz in new[] { -2.2f, 2.2f })
                foreach (float dx in new[] { -.7f, .7f }) m.Cylinder(tb + K.V(dx, 1.2f, dz), .06f, 2.4f, Silver, 8);
            m.Box(tb + K.V(0, 2.45f, 0), K.V(1.8f, .12f, 4.8f), Teal);
            m.Box(tb + K.V(-.7f, .9f, 0), K.V(.06f, .06f, 4.4f), Silver);
            m.Box(tb + K.V(.7f, .9f, 0), K.V(.06f, .06f, 4.4f), Silver);
            m.Add(TrolleyMesh(3), tb + K.V(0, 0, -1.2f), Quaternion.identity, Vector3.one);
            d.Walls.Add(new Bounds(new Vector3(s * (K.Edge + depth * .5f), d.Y((z0 + z1) * .5f) + 1.5f, (z0 + z1) * .5f), new Vector3(depth, 3f, z1 - z0)));
            K.Lamp(d.Props, null, s, d.Y(30f), 30f, K.Edge + 5.6f);
        }

        /// <summary>The big box: warm render, a dark band, a tall glass entrance atrium in a cream portal with a blank sign
        /// panel, charcoal canopies with coloured blank tenancy panels, roof plant.</summary>
        static void ShoppingCentre(CqChunkDraft d, CqLot l)
        {
            ComicMesh m = d.Frontage;
            float W = l.W, D = l.Depth, h = 11f;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            d.Wall(l, h);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, W, 0, h, 0, -D, MallRender);
            K.LBox(m, l, -.1f, W + .1f, h - 2f, h - .7f, .15f, -D, MallBand);
            K.LBox(m, l, 0, W, 0, 1f, .08f, 0, Color.Lerp(MallRender, K.R.ink, .2f));
            float e0 = W * .36f, e1 = W * .64f;
            // atrium and portal
            m.Surface = ComicSurface.Glass;
            K.LBox(m, l, e0, e1, 0, h + 4f, 1.5f, -6f, AtriumGlass);
            m.Surface = ComicSurface.PaintedMetal;
            for (float a = e0 + 1.8f; a < e1 - .5f; a += 1.8f) K.LBox(m, l, a - .05f, a + .05f, 0, h + 4f, 1.56f, 1.45f, K.R.ink);
            for (float y = 3.8f; y < h + 3.5f; y += 3.6f) K.LBox(m, l, e0, e1, y - .05f, y + .05f, 1.56f, 1.45f, K.R.ink);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, e0 - 1f, e0, 0, h + 4.8f, 2.2f, -6.2f, K.R.cream);
            K.LBox(m, l, e1, e1 + 1f, 0, h + 4.8f, 2.2f, -6.2f, K.R.cream);
            K.LBox(m, l, e0 - 1f, e1 + 1f, h + 4f, h + 5f, 2.2f, -6.2f, K.R.cream);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, e0 + 1.2f, e1 - 1.2f, h + 1.4f, h + 3.4f, 1.7f, 1.55f, SlideRed);                  // blank sign panel
            K.LBox(m, l, e0 + 1.35f, e1 - 1.35f, h + 1.55f, h + 3.25f, 1.75f, 1.7f, White);
            K.LBox(m, l, e0 - 1.2f, e1 + 1.2f, 3.6f, 3.95f, 5f, 0, Charcoal);                               // entrance canopy
            K.LQuad(m, l, e0 + 1f, e1 - 1f, 0, 3f, 1.53f, ShopGlass);
            // side canopies and blank tenancy panels
            Color[] panels = { SlideYellow, Teal, SlideBlue, Orange, Lime };
            int k = 0;
            foreach ((float a0, float a1) in new[] { (.5f, e0 - 1.5f), (e1 + 1.5f, W - .5f) })
            {
                K.LBox(m, l, a0, a1, 3.2f, 3.45f, 2.4f, 0, Charcoal);
                for (float a = a0 + .5f; a < a1 - 2.5f; a += 4.5f, k++)
                {
                    K.LQuad(m, l, a, a + 3.8f, .2f, 3f, .02f, K.R.ink);
                    K.LQuad(m, l, a + .15f, a + 3.65f, .3f, 2.9f, .04f, ShopGlass);
                    K.LBox(m, l, a + .4f, a + 3.4f, 4.1f, 5.2f, .12f, 0, panels[k % panels.Length]);
                }
            }
            m.Surface = ComicSurface.Metal;
            K.LBox(m, l, W * .1f, W * .25f, h, h + 1.6f, -4f, -10f, PlantGrey);
            K.LBox(m, l, W * .72f, W * .9f, h, h + 2f, -6f, -14f, PlantGrey);
        }

        /// <summary>A shopping-centre pylon sign: tall charcoal frame with three blank coloured panels (no brand).</summary>
        static void Pylon(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(p + K.V(0, .3f, 0), K.V(2.4f, .6f, 1.4f), K.R.cream);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + K.V(0, 5f, 0), K.V(2f, 9.4f, .9f), Charcoal);
            Color[] c = { SlideRed, Teal, SlideYellow };
            for (int i = 0; i < 3; i++)
            {
                float y = 3.2f + i * 2.5f;
                m.Box(p + K.V(0, y, 0), K.V(1.8f, 2.1f, 1f), White);
                m.Box(p + K.V(0, y, 0), K.V(1.5f, 1.7f, 1.06f), c[i]);
            }
            m.Box(p + K.V(0, 10f, 0), K.V(2.4f, .6f, 1.2f), White);
        }

        /// <summary>Three triangular shade sails (white, yellow, teal or red) on tall ink posts, like the town square's.</summary>
        static void ShadeSails(ComicMesh m, Vector3 c, int variant)
        {
            Color[] cols = variant == 0 ? new[] { White, SlideYellow, Teal } : new[] { SlideRed, White, Lime };
            var sails = new[]
            {
                (K.V(-3.2f, 4.6f, -3.4f), K.V(3.4f, 3.9f, -2.6f), K.V(-.6f, 4.2f, 2.8f)),
                (K.V(-1.2f, 4.1f, 1.2f), K.V(3.8f, 4.8f, 3.6f), K.V(1.6f, 3.8f, -1.6f)),
                (K.V(-3.6f, 3.8f, 1.8f), K.V(-.2f, 4.4f, 5f), K.V(-3.8f, 4.9f, 5.6f)),
            };
            for (int i = 0; i < sails.Length; i++)
            {
                (Vector3 a, Vector3 b, Vector3 d) = sails[i];
                Vector3 p0 = c + a, p1 = c + b, p2 = c + d, t = K.V(0, .07f, 0);
                m.Surface = ComicSurface.Fabric;
                K.TriFacing(m, p0 + t, p1 + t, p2 + t, cols[i], Vector3.up);
                K.TriFacing(m, p0, p1, p2, Color.Lerp(cols[i], K.R.ink, .25f), Vector3.down);
                Vector3 mid = (p0 + p1 + p2) / 3f;
                foreach ((Vector3 u, Vector3 v) in new[] { (p0, p1), (p1, p2), (p2, p0) })
                {
                    Vector3 n = Vector3.Cross(v - u, Vector3.up).normalized;
                    if (Vector3.Dot(n, (u + v) * .5f - mid) < 0f) n = -n;
                    K.Face(m, u, v, v + t, u + t, cols[i], n);
                }
                m.Surface = ComicSurface.PaintedMetal;
                foreach (Vector3 q in new[] { p0, p1, p2 })
                {
                    m.Cylinder(new Vector3(q.x, (c.y + q.y) * .5f, q.z), .07f, q.y - c.y + .2f, K.R.ink, 8);
                    m.Cylinder(new Vector3(q.x, c.y + .08f, q.z), .2f, .16f, K.R.ink, 8);
                }
            }
        }

        /// <summary>A raised lawn bed with a limestone rim on the square.</summary>
        static void LawnBed(CqChunkDraft d, int s, float z0, float z1, float x0, float x1)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, K.V(s * x0, K.Kerb - .1f, z0), K.V(s * x1, K.Kerb + .45f, z1), Limestone);
            m.Surface = ComicSurface.Foliage;
            K.MinMaxBox(m, K.V(s * (x0 + .3f), K.Kerb + .45f, z0 + .3f), K.V(s * (x1 - .3f), K.Kerb + .52f, z1 - .3f), Lawn);
        }

        /// <summary>The Cockburn ARC: glass ground floor, cream and timber bands, a big curved (barrel) roof over the whole
        /// hall with ink seams and glazed gable ends, a cantilevered entry canopy and a teal feature wall.</summary>
        static void ArcBuilding(CqChunkDraft d, CqLot l)
        {
            ComicMesh m = d.Frontage, roof = d.Landmark;
            float W = l.W, D = l.Depth, h = 9f;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            d.Wall(l, h + 8f);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, W, 0, h, 0, -D, ArcCream);
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, .4f, W - .4f, .1f, 4.2f, .02f, K.R.ink);
            K.LQuad(m, l, .5f, W - .5f, .2f, 4.1f, .04f, ShopGlass);
            m.Surface = ComicSurface.PaintedMetal;
            for (float a = 2.5f; a < W - .5f; a += 2.5f) K.LBox(m, l, a - .06f, a + .06f, .2f, 4.1f, .1f, 0, K.R.ink);
            m.Surface = ComicSurface.Wood;
            for (float y = 4.6f; y < h - .2f; y += 1f) K.LBox(m, l, -.05f, W + .05f, y, y + .5f, .18f, 0, ArcTimber);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, 12f, 15f, 0, h + 1.2f, .4f, -1f, Teal);
            K.LBox(m, l, 9f, 24f, 4.3f, 4.9f, 6f, -.5f, White);
            K.LBox(m, l, 9.2f, 23.8f, 4.1f, 4.3f, 5.8f, -.3f, Charcoal);
            // the barrel roof: rises from the eave at the front (outward +1.5) to a crown two thirds back, down to the back
            const int n = 12;
            float Rise(float t) => h + .2f + 7.5f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, .8f));
            float O(float t) => Mathf.Lerp(1.8f, -D - .6f, t);
            roof.Surface = ComicSurface.Metal;
            Vector3 toRoad = new Vector3(-l.S, 0, 0);
            for (int i = 0; i < n; i++)
            {
                float t0 = (float)i / n, t1 = (float)(i + 1) / n;
                Vector3 a0 = l.P(-.8f, Rise(t0), O(t0)), a1 = l.P(-.8f, Rise(t1), O(t1)), b0 = l.P(W + .8f, Rise(t0), O(t0)), b1 = l.P(W + .8f, Rise(t1), O(t1));
                Vector3 normal = (toRoad * (Rise(t1) - Rise(t0)) + Vector3.up * Mathf.Abs(O(t0) - O(t1))).normalized;
                K.Face(roof, a0, a1, b1, b0, Color.Lerp(ArcRoof, K.R.ink, i % 2 == 0 ? 0f : .06f), normal);
                K.Face(roof, a0 + Vector3.down * .3f, a1 + Vector3.down * .3f, b1 + Vector3.down * .3f, b0 + Vector3.down * .3f, Color.Lerp(ArcRoof, K.R.ink, .45f), -normal);
                // glazed gable ends under the curve
                roof.Surface = ComicSurface.Glass;
                Vector3 g0 = l.P(0f, h, O(t0)), g1 = l.P(0f, h, O(t1)), c0 = l.P(0f, Rise(t0) - .3f, O(t0)), c1 = l.P(0f, Rise(t1) - .3f, O(t1));
                if (O(t0) <= 0f && O(t1) <= 0f) K.Face(roof, g0, g1, c1, c0, AtriumGlass, Vector3.back);
                Vector3 h0 = l.P(W, h, O(t0)), h1 = l.P(W, h, O(t1)), e0 = l.P(W, Rise(t0) - .3f, O(t0)), e1 = l.P(W, Rise(t1) - .3f, O(t1));
                if (O(t0) <= 0f && O(t1) <= 0f) K.Face(roof, h0, h1, e1, e0, AtriumGlass, Vector3.forward);
                roof.Surface = ComicSurface.Metal;
            }
            // seams across the curve every 3 m
            for (float a = 1f; a < W; a += 3f)
                for (int i = 0; i < n; i++)
                {
                    float t0 = (float)i / n, t1 = (float)(i + 1) / n;
                    roof.Beam(l.P(a, Rise(t0) + .04f, O(t0)), l.P(a, Rise(t1) + .04f, O(t1)), .1f, SilverDark);
                }
        }

        /// <summary>The ARC waterslide tower: four ink legs, a platform with a teal roof, stairs, three tubes (yellow, blue,
        /// red) spiralling down round it to a splash pool.</summary>
        static void SlideTower(ComicMesh m, Vector3 p)
        {
            const float top = 12f;
            m.Surface = ComicSurface.Concrete;
            m.Box(p + K.V(0, .15f, 0), K.V(9f, .3f, 9f), K.Hex("7FC6D8"));
            m.Surface = ComicSurface.PaintedMetal;
            foreach (int x in new[] { -1, 1 })
                foreach (int z in new[] { -1, 1 })
                    m.Box(p + K.V(x * 1.3f, top * .5f, z * 1.3f), K.V(.3f, top, .3f), K.R.ink);
            m.Box(p + K.V(0, top, 0), K.V(3.4f, .3f, 3.4f), SilverDark);
            m.Cylinder(p + K.V(0, top + 1.9f, 0), 2.4f, .8f, Teal, 4, .1f, Quaternion.Euler(0, 45, 0));
            foreach (int x in new[] { -1, 1 }) m.Cylinder(p + K.V(x * 1.3f, top + .9f, 1.3f), .06f, 1.6f, K.R.ink, 6);
            for (float y = 1.5f; y < top; y += 3f) m.Box(p + K.V(0, y, 0), K.V(2.8f, .15f, 2.8f), Silver);
            Color[] tubes = { SlideYellow, SlideBlue, SlideRed };
            m.Surface = ComicSurface.Plastic;
            for (int t = 0; t < 3; t++)
            {
                const int seg = 14;
                float start = t * 120f, turns = 1f + t * .2f, radius = 2.6f + t * .7f;
                Vector3 Q(int i)
                {
                    float u = (float)i / seg, ang = (start + u * turns * 360f) * Mathf.Deg2Rad;
                    return p + K.V(Mathf.Cos(ang) * radius, Mathf.Lerp(top - .2f - t * .6f, 1f, u), Mathf.Sin(ang) * radius);
                }
                for (int i = 0; i < seg; i++) m.Beam(Q(i), Q(i + 1), .9f, tubes[t]);
                m.Surface = ComicSurface.PaintedMetal;
                for (int i = 2; i < seg; i += 4) m.Box(new Vector3(Q(i).x, (p.y + Q(i).y) * .5f, Q(i).z), K.V(.12f, Q(i).y - p.y, .12f), K.R.ink);
                m.Surface = ComicSurface.Plastic;
            }
        }

        static void BusStopSign(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + K.V(0, .08f, 0), .16f, .16f, K.R.ink, 8);
            m.Cylinder(p + K.V(0, 1.4f, 0), .05f, 2.8f, Silver, 8);
            m.Box(p + K.V(0, 2.4f, 0), K.V(.06f, .7f, .55f), SignBlue);
            m.Box(p + K.V(0, 2.45f, 0), K.V(.08f, .3f, .32f), White);
            m.Box(p + K.V(0, 1.7f, 0), K.V(.06f, .45f, .4f), SlideYellow);
        }

        // ================================================================ AUBIN GROVE chunks

        static CqChunkDraft AubinDraft(string variant, bool path) =>
            new CqChunkDraft("AubinGrove_" + variant, AubinDrop) { Type = path ? RoadType.PedestrianMall : RoadType.Street };

        /// <summary>The Principal Shared Path: asphalt with a white dashed centre line, concrete footpath edges.</summary>
        static void SharedPath(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f)
        {
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, Footpath, K.R.cream, false, skipFrom, skipTo);
            for (float z = 1f; z < d.Length - .5f; z += 6f)
            {
                float e = Mathf.Min(z + 3f, d.Length - .5f);
                if (skipFrom >= 0f && e > skipFrom - .5f && z < skipTo + .5f) continue;
                K.Flat(d.Road, -.07f, .07f, -.07f, .07f, z, e, .03f, White);
            }
        }

        static void EstateRoad(CqChunkDraft d)
        {
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, Footpath, K.R.cream, false);
            for (int s = -1; s <= 1; s += 2) K.Flat(d.Road, s * (K.Half + .3f), s * (K.Half + 1.4f), s * (K.Half + .3f), s * (K.Half + 1.4f), 0f, d.Length, K.Kerb + .02f, Lawn);
        }

        static CqChunkDraft AubinEntry()
        {
            // A quiet estate street: calm at z = 0 (hedges, low limestone walls, young trees), then the freeway fence on
            // the left and limestone walls with Colorbond fences on the right.
            var d = AubinDraft("Entry", false);
            EstateRoad(d);
            for (int s = -1; s <= 1; s += 2) CalmEdge(d, s, 0, 14, 31 + s);
            FreewaySide(d, 14, 40, 33, false);
            EstateEdge(d, 14, 40, 34);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft AubinExit()
        {
            // The path with the freeway and the wetland, then calm towards z = 40.
            var d = AubinDraft("Exit", true);
            SharedPath(d);
            FreewaySide(d, 0, 26, 41, false);
            WetlandSide(d, 0, 26, 42, false);
            for (int s = -1; s <= 1; s += 2) CalmEdge(d, s, 26, 40, 43 + s);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft AubinSharedPath()
        {
            // The shared path beside the freeway: fence, freeway and the train line in the median on the left, limestone
            // retaining walls with Colorbond fences and roofs on the right, the Russell Road overpass over everything.
            var d = AubinDraft("Path", true);
            SharedPath(d);
            FreewaySide(d, 0, 40, 51, true);
            EstateEdge(d, 0, 40, 52);
            Overpass(d, 22f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft AubinEstate()
        {
            // An estate street (cars allowed): rendered-brick houses with grey Colorbond hip roofs on raised lots behind
            // white limestone retaining walls, letterbox pillars, bins out on the kerb, young street trees.
            var d = AubinDraft("Estate", false);
            EstateRoad(d);
            var r = P.Rng(61);
            for (int s = -1; s <= 1; s += 2)
            {
                const float set = 6f, pad = .9f;
                LimestoneWall(d.SlopedFrontage, s, K.Edge + .1f, 0, 40, -1.2f, K.Kerb + pad);
                Ground(d, s, K.Edge + .6f, K.Edge + 26f, K.Kerb + pad - .02f, Lawn, 0, 40);
                P.Patches(d.Road, r, s, K.Edge + 1f, K.Edge + set - .5f, K.Kerb + pad - .02f, LawnLight, LawnDry, 0, 40, 5);
                float[] cuts = s < 0 ? new[] { 0f, 13.5f, 27f, 40f } : new[] { 0f, 14f, 26.5f, 40f };
                for (int i = 0; i + 1 < cuts.Length; i++)
                {
                    float z0 = cuts[i] + .4f, z1 = cuts[i + 1] - .4f;
                    bool garageFirst = (i + (s > 0 ? 1 : 0)) % 2 == 0, two = s > 0 && i == 1;
                    Color wall = HouseWalls[(i * 2 + (s > 0 ? 3 : 0)) % HouseWalls.Length];
                    Color roof = new[] { RoofMonument, RoofBasalt, RoofWindspray, RoofTile }[(i + (s > 0 ? 2 : 0)) % 4];
                    House(d, new CqLot(s, z0, z1, K.Edge + set, 12, d.Base((z0 + z1) * .5f) + pad), wall, roof, GarageDoors[(i + s + 4) % GarageDoors.Length], garageFirst, two);
                    // driveway (concrete) from the wall to the garage, letterbox pillar beside it
                    float g0 = garageFirst ? z0 + .5f : z1 - 6.5f;
                    PathStripAt(d, s, g0, g0 + 5.6f, K.Edge + .6f, K.Edge + set, K.Kerb + pad);
                    float lz = garageFirst ? g0 + 6.3f : g0 - .8f;
                    d.Props.Add(LetterboxMesh(), new Vector3(s * (K.Edge - .45f), d.Ground(lz), lz), Quaternion.Euler(0, s < 0 ? 90f : -90f, 0), Vector3.one);
                    d.Collisions.Add(new DressingObstacle { id = "Letterbox", position = new Vector3(s * (K.Edge - .45f), d.Ground(lz), lz), rotation = Vector3.zero, centre = new Vector3(0, .5f, 0), size = new Vector3(.55f, 1.05f, .55f), canJumpOver = true });
                    if (i != 1 || s < 0)
                    {
                        float bz = garageFirst ? g0 + 7.4f : g0 - 2f;
                        d.Props.Add(WheelieBinsMesh(), new Vector3(s * (K.Half + .9f), d.Ground(bz), bz), Quaternion.Euler(0, s < 0 ? 90f : -90f, 0), Vector3.one);
                        d.Collisions.Add(new DressingObstacle { id = "WheelieBins", position = new Vector3(s * (K.Half + .9f), d.Ground(bz), bz), rotation = new Vector3(0, s < 0 ? 90f : -90f, 0), centre = new Vector3(0, .55f, 0), size = new Vector3(1.45f, 1.1f, .75f), canJumpOver = true });
                    }
                }
                YoungTree(d.Props, d.Collisions, new Vector3(s * (K.Half + .85f), d.Ground(s < 0 ? 20f : 6f), s < 0 ? 20f : 6f), 5f, 63 + s, false, Peppermint);
                BackRoofs(d, s, 0, 40, 64 + s, 30f);
            }
            d.Props.Add(RooSignMesh(), new Vector3(K.Half + .5f, d.Ground(36f), 36f), Quaternion.Euler(0, -90f, 0), Vector3.one);
            K.Thin(d.Collisions, "RooSign", new Vector3(K.Half + .5f, d.Ground(36f), 36f), 2.7f, .3f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft AubinStation()
        {
            // Aubin Grove station: the white wedge-roofed building on the right, its enclosed footbridge over the path and
            // the freeway to the island platform in the median (canopy, orange walls), bus shelters at the path.
            var d = AubinDraft("Station", true);
            SharedPath(d);
            FreewaySide(d, 0, 40, 71, false, true);
            Ground(d, 1, K.Edge, K.Edge + 50f, K.Kerb - .02f, Color.Lerp(K.R.pavement, K.R.cream, .25f), 0, 40, ComicSurface.Paving);
            Ground(d, 1, K.Edge + 22f, K.Edge + 50f, K.Kerb - .015f, K.R.asphalt, 0, 40, ComicSurface.Asphalt);
            StationBuilding(d.Landmark, new Vector3(K.Edge + 11f, d.Base(15f), 4f));
            d.Walls.Add(new Bounds(new Vector3(K.Edge + 11f, d.Y(15f) + 4f, 15f), new Vector3(14f, 8f, 22f)));
            const float bz = 22f, bw = 3.2f, by = 7.4f;
            Footbridge(d.Landmark, d.Y(bz) + by, bz, -36f, K.Edge + 11.5f, bw);
            LiftTower(d.Landmark, new Vector3(-32f, d.Y(bz) - .8f + 1f, bz + bw * .5f), by + 3.4f);
            for (float z = 4f; z < 40f; z += 26f)
                K.PropFromKit(d.Props, d.Collisions, "BusShelter", new Vector3(K.Edge - 1.2f, d.Ground(z + 3f), z + 3f), -90f);
            K.PropFromKit(d.Props, d.Collisions, "BikeRack", new Vector3(K.Edge - .8f, d.Ground(17f), 17f), -90f);
            BackParked(d, 1, 72);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft AubinWetland()
        {
            // Thomsons Lake country on the right: grass verge where kangaroos graze, reeds and paperbarks at the water.
            var d = AubinDraft("Wetland", true);
            SharedPath(d);
            FreewaySide(d, 0, 40, 81, true);
            WetlandSide(d, 0, 40, 82, true);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft AubinDrain()
        {
            // A wetland drain crosses the path: a full-width limestone kicker up to the channel wall, 5 m of water
            // (SPLASH), then the path again. The ramp covers the whole rideable width, so the gap can always be cleared.
            var d = AubinDraft("Drain", true);
            float z0 = DrainStart, lip = z0 + DrainRise + DrainTop, far = lip + DrainGap;
            SharedPath(d, lip, far);
            FreewaySide(d, 0, 40, 91, false, false, lip, far);
            WetlandSide(d, 0, lip, 92, false);
            WetlandSide(d, far, 40, 93, false);
            // the channel: water from the culvert under the freeway out into the wetland, limestone walls each side
            K.Water(d.Road, -(K.Edge + 4.4f), 80f, lip, far, DrainWater, Water, Ripple, 94);
            ComicMesh w = d.Road;
            w.Surface = ComicSurface.Concrete;
            K.MinMaxBox(w, K.V(-(K.Edge + 4.4f), DrainWater - 1f, lip - .35f), K.V(K.Edge + 6f, K.Kerb - .01f, lip), Limestone);
            // the far wall: flush with the path (under the asphalt) and with the footpaths beside it
            K.MinMaxBox(w, K.V(-K.Half, DrainWater - 1f, far), K.V(K.Half, -.03f, far + .35f), Limestone);
            K.MinMaxBox(w, K.V(-(K.Edge + 4.4f), DrainWater - 1f, far), K.V(-K.Half, K.Kerb - .01f, far + .35f), Limestone);
            K.MinMaxBox(w, K.V(K.Half, DrainWater - 1f, far), K.V(K.Edge + 6f, K.Kerb - .01f, far + .35f), Limestone);
            K.MinMaxBox(w, K.V(-(K.Edge + 4.9f), DrainWater - 1f, lip - .4f), K.V(-(K.Edge + 4.4f), K.Kerb + 1.2f, far + .4f), Limestone);
            w.Surface = ComicSurface.Plain;
            K.Face(w, K.V(-(K.Edge + 4.35f), DrainWater + .05f, lip + 1f), K.V(-(K.Edge + 4.35f), DrainWater + .05f, far - 1f), K.V(-(K.Edge + 4.35f), DrainWater + 1.2f, far - 1f), K.V(-(K.Edge + 4.35f), DrainWater + 1.2f, lip + 1f), K.R.ink, Vector3.right, false);
            var ramp = new CqRampSpec { Name = "LimestoneKicker", X = 0f, Z = z0, Width = 2f * K.Edge, Rise = DrainRise, Height = DrainHeight, Top = DrainTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(ramp);
            d.Gaps.Add(new CqGapSpec { Name = "DrainWater", Z = lip, Length = DrainGap, Width = 0f, Word = "SPLASH", Water = true });
            d.SlopedFrontage.Add(LimestoneRampArt(ramp, DrainWater - .3f), Vector3.zero, Quaternion.identity, Vector3.one);
            for (int i = 0; i < 4; i++)
            {
                Reeds(d.Props, new Vector3(K.Edge + 1.2f + i * 1.7f, d.Ground(lip) - .2f, lip - .8f), 1.1f, 95 + i);
                Reeds(d.Props, new Vector3(K.Edge + 1.6f + i * 1.9f, d.Ground(far) - .2f, far + .9f), 1f, 99 + i);
            }
            d.KeepOut.Add(KeepOutBox(d, -1, z0, far)); d.KeepOut.Add(KeepOutBox(d, 1, z0, far));
            d.GiftSlots.Add(new Vector2(0f, 27f));
            d.Slot(-2.6f, 34f); d.Slot(2.6f, 34f);
            return d;
        }

        static Bounds KeepOutBox(CqChunkDraft d, int s, float z0, float z1)
        {
            float yTop = d.Y(z0) + 5f, yBottom = d.Y(z1) - 2f;
            return new Bounds(new Vector3(s * (K.Half + K.Side * .5f + .3f), (yTop + yBottom) * .5f, (z0 + z1) * .5f), new Vector3(K.Side + .6f, yTop - yBottom, z1 - z0));
        }

        // ================================================================ AUBIN pieces

        static void PathStripAt(CqChunkDraft d, int s, float z0, float z1, float a, float b, float y)
        {
            d.Road.Surface = ComicSurface.Concrete;
            K.Face(d.Road, K.V(s * a, y + .01f, z0), K.V(s * a, y + .01f, z1), K.V(s * b, y + .01f, z1), K.V(s * b, y + .01f, z0), Footpath, Vector3.up, false);
        }

        /// <summary>Freeway side (left) from z0 to z1: dry verge with banksias, the black path fence, the bank down to the
        /// southbound lanes, a jersey barrier, the Mandurah line in the median (ballast, sleepers, rails, overhead masts,
        /// sometimes a train), the northbound lanes, the far noise wall, a few cars. platform: an island platform with a
        /// canopy and orange walls between the tracks. A drain (skipFrom..skipTo) cuts through the verge.</summary>
        static void FreewaySide(CqChunkDraft d, float z0, float z1, int seed, bool train, bool platform = false, float skipFrom = -1f, float skipTo = -1f)
        {
            const int s = -1;
            var r = P.Rng(seed);
            float y = -.9f;
            bool drain = skipFrom >= 0f;
            foreach ((float a, float b) in Spans(z0, z1, skipFrom, skipTo))
            {
                Ground(d, s, K.Edge, K.Edge + 4.4f, K.Kerb - .03f, DryGrass, a, b);
                P.Patches(d.Road, r, s, K.Edge + .3f, K.Edge + 4.2f, K.Kerb - .03f, Sand, LawnDry, a, b, Mathf.Max(1, Mathf.RoundToInt((b - a) / 8f)));
                Fence(d.SlopedFrontage, s, K.Edge + 4.5f, a, b);
            }
            P.Bank(d.Road, s, K.Edge + 4.4f, K.Kerb - .03f, K.Edge + 7f, y, DryGrass, z0, z1);
            // southbound lanes |x| 15..26.5, northbound 37.5..49
            foreach ((float a, float b) in new[] { (15f, 26.5f), (37.5f, 49f) })
            {
                Ground(d, s, a, b, y, K.R.asphalt, z0, z1, ComicSurface.Asphalt);
                Stripe(d, s, a + .4f, a + .55f, z0, z1, y + .02f, White);
                Stripe(d, s, b - .55f, b - .4f, z0, z1, y + .02f, K.R.yellow);
                for (float lane = a + 4f; lane < b - 2f; lane += 3.6f)
                    for (float z = Mathf.Ceil(z0 / 9f) * 9f; z < z1 - 3f; z += 9f) Stripe(d, s, lane - .07f, lane + .07f, z, z + 3f, y + .02f, White);
            }
            Ground(d, s, K.Edge + 7f, 15f, y, LawnDry, z0, z1);
            JerseyBarrier(d.SlopedFrontage, s, 26.9f, y, z0, z1);
            JerseyBarrier(d.SlopedFrontage, s, 37.1f, y, z0, z1);
            // the rail corridor in the median
            Ground(d, s, 27.3f, 36.7f, y - .05f, Ballast, z0, z1, ComicSurface.Concrete);
            float t1 = platform ? 28.6f : 29.8f, t2 = platform ? 35.4f : 34.2f;
            foreach (float tc in new[] { t1, t2 })
            {
                for (float z = Mathf.Ceil(z0 / .9f) * .9f; z < z1 - .3f; z += .9f) Stripe(d, s, tc - 1.25f, tc + 1.25f, z, z + .24f, y + .02f, Sleeper);
                foreach (float rx in new[] { tc - .72f, tc + .72f })
                {
                    // rails in the (sheared, outline-free) road mesh so they follow the slope
                    d.Road.Surface = ComicSurface.Metal;
                    for (float z = z0; z < z1 - .01f; z += 4f)
                        K.MinMaxBox(d.Road, K.V(-rx - .05f, y + .02f, z), K.V(-rx + .05f, y + .16f, Mathf.Min(z1, z + 4f)), RailSteel);
                }
            }
            // overhead line masts every 40 m, wires along each track
            for (float z = z0 + 6f; z < z1 - 2f; z += 40f)
            {
                ComicMesh m = d.Props;
                m.Surface = ComicSurface.PaintedMetal;
                float by = d.Y(z) + y;
                m.Box(K.V(-27.6f, by + 3.3f, z), K.V(.3f, 6.6f, .3f), SilverDark);
                m.Box(K.V(-36.4f, by + 3.3f, z), K.V(.3f, 6.6f, .3f), SilverDark);
                m.Box(K.V(-32f, by + 6.4f, z), K.V(9f, .3f, .3f), SilverDark);
            }
            for (float z = z0; z < z1 - .01f; z += 8f)
            {
                float e = Mathf.Min(z1, z + 8f);
                foreach (float tc in new[] { t1, t2 })
                    K.MinMaxBox(d.Road, K.V(-tc - .03f, y + 5.9f, z), K.V(-tc + .03f, y + 5.96f, e), K.R.ink);
            }
            if (platform) Platform(d, z0, z1, (t1 + t2) * .5f, y);
            if (train && !platform) TrainCar(d.Landmark, new Vector3(-t1, d.Y(20f) + y + .1f, 20f), 19f, true);
            // far verge and noise wall
            Ground(d, s, 49f, 62f, y, DryGrass, z0, z1);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float zc = z + 2f;
                d.Deep.Surface = ComicSurface.Concrete;
                d.Deep.Box(K.V(-52f, d.Y(zc) + y + 1.9f, zc), K.V(.4f, 5.6f, 3.96f), (int)(z / 4f) % 2 == 0 ? NoiseWall : Color.Lerp(NoiseWall, K.R.cream, .3f));
                d.Deep.Box(K.V(-51.75f, d.Y(zc) + y + 3.2f, zc), K.V(.1f, .9f, 2.2f), NoiseWallDark);
            }
            // a few cars on the freeway (southbound with us, northbound towards us)
            for (int i = 0; i < 3; i++)
            {
                float z = Mathf.Lerp(z0 + 3f, z1 - 3f, P.F(r, 0f, 1f));
                bool south = i != 1;
                float lane = south ? 15f + 2f + 3.6f * r.Next(3) : 37.5f + 2f + 3.6f * r.Next(3);
                d.Landmark.Add(CarMesh(CarColours[r.Next(CarColours.Length)]), new Vector3(-lane, d.Y(z) + y, z), Quaternion.Euler(0, south ? 0f : 180f, 0), Vector3.one);
            }
            // verge plants
            for (float z = z0 + 4f; z < z1 - 2f; z += 11f)
            {
                if (drain && z > skipFrom - 2f && z < skipTo + 2f) continue;
                Vector3 p = new Vector3(-(K.Edge + 2.2f + P.F(r, 0f, 1.5f)), d.Ground(z) - .03f, z);
                if (r.Next(3) == 0) P.Balga(d.Props, p, .9f, seed + (int)z); else P.Banksia(d.Props, p, .75f, seed + (int)z);
            }
            if (drain)
            {
                // the culvert under the freeway: limestone headwall with a dark pipe mouth
                ComicMesh m = d.SlopedFrontage;
                m.Surface = ComicSurface.Concrete;
                K.MinMaxBox(m, K.V(-(K.Edge + 4.9f), DrainWater - .5f, skipFrom - 1.5f), K.V(-(K.Edge + 5.6f), K.Kerb + .9f, skipTo + 1.5f), Limestone);
            }
        }

        static IEnumerable<(float, float)> Spans(float z0, float z1, float skipFrom, float skipTo)
        {
            if (skipFrom < 0f || skipTo <= z0 || skipFrom >= z1) { yield return (z0, z1); yield break; }
            if (skipFrom > z0) yield return (z0, skipFrom);
            if (skipTo < z1) yield return (skipTo, z1);
        }

        static void Stripe(CqChunkDraft d, int s, float a, float b, float z0, float z1, float y, Color c)
        {
            float x0 = s * a, x1 = s * b;
            K.Flat(d.Road, Mathf.Min(x0, x1), Mathf.Max(x0, x1), Mathf.Min(x0, x1), Mathf.Max(x0, x1), z0, z1, y, c);
        }

        /// <summary>The black path fence: posts every 2.5 m, top and bottom rails, two wires (flat space, sheared).</summary>
        static void Fence(ComicMesh m, int s, float x, float z0, float z1)
        {
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = z0 + .3f; z < z1; z += 2.5f) m.Box(K.V(s * x, K.Kerb + .8f, z), K.V(.09f, 1.7f, .09f), DeckDark);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                foreach (float y in new[] { K.Kerb + 1.55f, K.Kerb + .15f })
                    K.MinMaxBox(m, K.V(s * x - .04f, y - .04f, z), K.V(s * x + .04f, y + .04f, e), DeckDark);
                foreach (float y in new[] { K.Kerb + .65f, K.Kerb + 1.1f })
                    K.MinMaxBox(m, K.V(s * x - .015f, y - .015f, z), K.V(s * x + .015f, y + .015f, e), SilverDark);
            }
        }

        /// <summary>A concrete jersey barrier (flat space, sheared): wide foot, narrow top, cap.</summary>
        static void JerseyBarrier(ComicMesh m, int s, float x, float y, float z0, float z1)
        {
            m.Surface = ComicSurface.Concrete;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.MinMaxBox(m, K.V(s * x - .32f, y - .2f, z), K.V(s * x + .32f, y + .32f, e), Barrier);
                K.MinMaxBox(m, K.V(s * x - .14f, y + .32f, z), K.V(s * x + .14f, y + .82f, e), Barrier);
            }
        }

        /// <summary>The island platform: raised deck with yellow edges, a long canopy on columns, the orange-walled pod.</summary>
        static void Platform(CqChunkDraft d, float z0, float z1, float xc, float y)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, K.V(-xc - 2.2f, y - .3f, z0), K.V(-xc + 2.2f, y + 1f, z1), Concrete);
            K.MinMaxBox(m, K.V(-xc - 2.25f, y + 1f, z0), K.V(-xc - 1.9f, y + 1.02f, z1), K.R.yellow);
            K.MinMaxBox(m, K.V(-xc + 1.9f, y + 1f, z0), K.V(-xc + 2.25f, y + 1.02f, z1), K.R.yellow);
            K.MinMaxBox(m, K.V(-xc - 1.8f, y + 1f, z0), K.V(-xc + 1.8f, y + 1.015f, z1), PortRed);
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = z0 + 2f; z < z1 - 1f; z += 6f) m.Box(K.V(-xc, y + 2.9f, z), K.V(.25f, 3.8f, .25f), Silver);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.MinMaxBox(m, K.V(-xc - 2.6f, y + 4.8f, z), K.V(-xc + 2.6f, y + 5.05f, e), White);
                K.MinMaxBox(m, K.V(-xc - 2.65f, y + 4.65f, z), K.V(-xc + 2.65f, y + 4.82f, e), Charcoal);
            }
            m.Surface = ComicSurface.Plaster;
            K.MinMaxBox(m, K.V(-xc - 1.2f, y + 1f, 8f), K.V(-xc + 1.2f, y + 3.6f, 15f), Orange);
            K.MinMaxBox(m, K.V(-xc - 1.25f, y + 1.1f, 9f), K.V(-xc + 1.25f, y + 2.6f, 11f), ShopGlass);
        }

        /// <summary>Estate edge on the right from z0 to z1: grass verge with a peppermint tree and banksias, a white
        /// limestone retaining wall, a grey Colorbond fence on top, and the roofs of the houses behind it.</summary>
        static void EstateEdge(CqChunkDraft d, float z0, float z1, int seed)
        {
            const int s = 1;
            var r = P.Rng(seed);
            Ground(d, s, K.Edge, K.Edge + 3f, K.Kerb - .03f, Lawn, z0, z1);
            P.Patches(d.Road, r, s, K.Edge + .3f, K.Edge + 2.8f, K.Kerb - .03f, LawnLight, LawnDry, z0, z1, 3);
            LimestoneWall(d.SlopedFrontage, s, K.Edge + 3f, z0, z1, -1f, K.Kerb + 1.6f);
            ColorbondFence(d.SlopedFrontage, s, K.Edge + 3.25f, z0, z1, K.Kerb + 1.7f, RoofBasalt);
            Ground(d, s, K.Edge + 3.5f, K.Edge + 40f, K.Kerb + 1.68f, Lawn, z0, z1);
            for (float z = z0 + 6f; z < z1 - 3f; z += 14f)
                YoungTree(d.Props, null, new Vector3(s * (K.Edge + 1.6f), d.Ground(z), z), 5.2f, seed + (int)z, false, Peppermint);
            for (float z = z0 + 2f; z < z1 - 1f; z += 7f)
                if (r.Next(2) == 0) P.Banksia(d.Props, new Vector3(s * (K.Edge + 1f + P.F(r, 0f, 1.2f)), d.Ground(z), z), .6f, seed + (int)z);
            // the houses behind the fence: walls and roofs (the near row outlined, a second row in the background)
            int i = 0;
            for (float z = z0 + 1f; z < z1 - 5f; z += 13.5f, i++)
            {
                float zc = Mathf.Min(z + 6f, z1 - 6f);
                HipHouse(d.Frontage, new Vector3(s * (K.Edge + 11f), d.Base(zc) + 1.6f, zc), 11f, 10f, 3.1f, HouseWalls[(seed + i) % HouseWalls.Length], new[] { RoofMonument, RoofBasalt, RoofWindspray, RoofTile }[(seed + i) % 4]);
            }
            BackRoofs(d, s, z0, z1, seed + 5, 26f);
            d.Walls.Add(new Bounds(new Vector3(s * (K.Edge + 12f), d.Y((z0 + z1) * .5f) + 2f, (z0 + z1) * .5f), new Vector3(18f, 5f, z1 - z0)));
        }

        /// <summary>Rows of estate roofs in the background (no outline): walls barely showing, hip roofs in Colorbond greys
        /// and terracotta tiles, a gum tree here and there.</summary>
        static void BackRoofs(CqChunkDraft d, int s, float z0, float z1, int seed, float from)
        {
            var r = P.Rng(seed);
            for (int row = 0; row < 2; row++)
                for (float z = z0 + 2f + row * 5f; z < z1 - 2f; z += 13f)
                {
                    float zc = z + 5f, x = s * (from + row * 14f + P.F(r, 0f, 2f));
                    HipHouse(d.Deep, new Vector3(x, d.Base(Mathf.Min(zc, z1)) + 1.4f, Mathf.Min(zc, z1 - 1f)), 11f, 11f, 3f,
                        Color.Lerp(HouseWalls[r.Next(HouseWalls.Length)], Haze, .1f * row), Color.Lerp(new[] { RoofMonument, RoofBasalt, RoofWindspray, RoofTile }[r.Next(4)], Haze, .1f + .1f * row));
                    if (r.Next(3) == 0) DeepTree(d.Deep, new Vector3(x + s * 7f, d.Base(zc), zc + 4f), 8f, seed + (int)z);
                }
        }

        /// <summary>Cars parked behind the station (background, no outline).</summary>
        static void BackParked(CqChunkDraft d, int s, int seed)
        {
            var r = P.Rng(seed);
            for (float z = 2f; z < 38f; z += 3f)
                for (int row = 0; row < 2; row++)
                    if (r.Next(4) != 0)
                        d.Deep.Add(CarMesh(CarColours[r.Next(CarColours.Length)]), new Vector3(s * (K.Edge + 26f + row * 9f), d.Y(z) + K.Kerb, z), Quaternion.Euler(0, 90f, 0), Vector3.one);
        }

        /// <summary>The wetland on the right from z0 to z1: grass verge (grazing kangaroos), a bank down to the water, reeds
        /// along the edge, paperbarks standing at and in the water, more across the lake.</summary>
        static void WetlandSide(CqChunkDraft d, float z0, float z1, int seed, bool roos)
        {
            const int s = 1;
            var r = P.Rng(seed);
            const float level = -.85f;
            Ground(d, s, K.Edge, K.Edge + 4.5f, K.Kerb - .03f, Lawn, z0, z1);
            P.Patches(d.Road, r, s, K.Edge + .3f, K.Edge + 4.3f, K.Kerb - .03f, LawnLight, LawnDry, z0, z1, Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / 8f)));
            P.Bank(d.Road, s, K.Edge + 4.5f, K.Kerb - .03f, K.Edge + 9f, level - .15f, Color.Lerp(Lawn, Reed, .5f), z0, z1);
            K.Water(d.Road, K.Edge + 8f, 120f, z0, z1, level, Water, Ripple, seed);
            float len = z1 - z0;
            for (float z = z0 + 1.5f; z < z1 - 1f; z += 3.2f)
            {
                Vector3 p = new Vector3(K.Edge + 7.5f + P.F(r, 0f, 2.5f), d.Y(z) + level + .05f, z);
                Reeds(z % 2f < 1f ? d.Props : d.Deep, p, P.F(r, .9f, 1.3f), seed + (int)(z * 3f));
            }
            int full = len > 30f ? 3 : 1;
            for (int i = 0; i < full; i++)
            {
                float z = z0 + (i + .5f) * len / full + P.F(r, -2f, 2f);
                PaperbarkTree(d.Props, new Vector3(K.Edge + 9.5f + P.F(r, 0f, 5f), d.Y(z) + level, z), P.F(r, 7f, 9f), seed + i);
            }
            for (float z = z0 + 3f; z < z1 - 1f; z += 7f)
                PaperbarkTree(d.Deep, new Vector3(K.Edge + 24f + P.F(r, 0f, 30f), d.Y(z) + level, z), P.F(r, 6f, 9f), seed + (int)z, true);
            for (float z = z0 + 5f; z < z1 - 1f; z += 9f)
                Reeds(d.Deep, new Vector3(K.Edge + 18f + P.F(r, 0f, 25f), d.Y(z) + level + .05f, z), 1.4f, seed + (int)z + 7);
            if (roos)
            {
                d.Props.Add(KangarooMesh(), new Vector3(K.Edge + 2.4f, d.Ground(12f) - .03f, 12f), Quaternion.Euler(0, 140f, 0), Vector3.one);
                d.Props.Add(KangarooMesh(true), new Vector3(K.Edge + 3.6f, d.Ground(16f) - .03f, 16f), Quaternion.Euler(0, 200f, 0), Vector3.one * .85f);
            }
            d.Walls.Add(new Bounds(new Vector3(s * (K.Edge + 8f), d.Y((z0 + z1) * .5f), (z0 + z1) * .5f), new Vector3(14f, 4f, z1 - z0)));
        }

        /// <summary>The Russell Road overpass across the path, the freeway and the railway: deck on pier walls outside the
        /// rideable width, white fascia, blue-grey throw screens, light poles on top.</summary>
        static void Overpass(CqChunkDraft d, float z)
        {
            ComicMesh m = d.Landmark;
            const float w = 11f, under = 6.8f;
            float y = d.Y(z + w * .5f);
            float x0 = -62f, x1 = 34f;
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, K.V(x0, y + under, z), K.V(x1, y + under + 1.3f, z + w), Concrete);
            K.MinMaxBox(m, K.V(x0, y + under + .4f, z - .25f), K.V(x1, y + under + 1.5f, z), White);
            K.MinMaxBox(m, K.V(x0, y + under + .4f, z + w), K.V(x1, y + under + 1.5f, z + w + .25f), White);
            K.MinMaxBox(m, K.V(x0, y + under - .25f, z + .5f), K.V(x1, y + under, z + w - .5f), Color.Lerp(Concrete, K.R.ink, .35f));
            // throw screens
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float sz in new[] { z - .1f, z + w + .1f })
            {
                K.MinMaxBox(m, K.V(x0, y + under + 1.5f, sz - .04f), K.V(x1, y + under + 3.6f, sz + .04f), Color.Lerp(BridgeClad, BridgeGlass, .35f));
                for (float x = x0; x <= x1; x += 4f) m.Box(K.V(x, y + under + 2.55f, sz), K.V(.12f, 2.1f, .14f), DeckDark);
                K.MinMaxBox(m, K.V(x0, y + under + 3.55f, sz - .08f), K.V(x1, y + under + 3.7f, sz + .08f), DeckDark);
            }
            for (float x = x0 + 6f; x < x1; x += 18f)
            {
                m.Cylinder(K.V(x, y + under + 5f, z + 1f), .08f, 7f, Silver, 6);
                m.Box(K.V(x, y + under + 8.45f, z + 1.8f), K.V(.3f, .15f, 1.6f), Silver);
            }
            // piers: either side of the path (outside the rideable width), in the freeway median and beyond
            m.Surface = ComicSurface.Concrete;
            foreach (float px in new[] { -(K.Edge + 4f), K.Edge + 3.4f, -32f, -51f, 26f })
            {
                float py = d.Y(z + w * .5f) + (Mathf.Abs(px) > 14f && px < 0f ? -.9f : K.Kerb);
                K.MinMaxBox(m, K.V(px - .7f, py - 1f, z + 1.2f), K.V(px + .7f, y + under - .25f, z + w - 1.2f), Concrete);
                K.MinMaxBox(m, K.V(px - 1.1f, y + under - .9f, z + .8f), K.V(px + 1.1f, y + under - .25f, z + w - .8f), Color.Lerp(Concrete, K.R.ink, .15f));
            }
            // the abutment bank on the right (limestone-clad), with the estate wall stopping against it
            K.MinMaxBox(m, K.V(K.Edge + 6f, d.Y(z) - 1f, z - 1f), K.V(x1, y + under, z + w + 1f), Limestone);
        }

        /// <summary>A Perth retaining wall of big white limestone blocks (flat space, sheared): body in 4 m pieces, a cap,
        /// course and staggered vertical joints on the face towards the road.</summary>
        static void LimestoneWall(ComicMesh m, int s, float x, float z0, float z1, float y0, float y1)
        {
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                m.Surface = ComicSurface.Concrete;
                K.MinMaxBox(m, K.V(s * x, y0, z), K.V(s * (x + .5f), y1, e), Limestone);
                K.MinMaxBox(m, K.V(s * (x - .04f), y1, z), K.V(s * (x + .56f), y1 + .1f, e), LimestoneCap);
                m.Surface = ComicSurface.Plain;
                float fx = s * (x - .012f);
                Vector3 n = new Vector3(-s, 0, 0);
                int course = 0;
                for (float yy = y1 - .55f; yy > Mathf.Max(y0, K.Kerb - .3f); yy -= .55f, course++)
                {
                    K.Face(m, K.V(fx, yy - .02f, z), K.V(fx, yy - .02f, e), K.V(fx, yy + .02f, e), K.V(fx, yy + .02f, z), LimeJoint, n, false);
                    for (float jz = z + (course % 2 == 0 ? .6f : 1.2f); jz < e - .1f; jz += 1.2f)
                        K.Face(m, K.V(fx, yy + .02f, jz - .02f), K.V(fx, yy + .02f, jz + .02f), K.V(fx, Mathf.Min(y1, yy + .55f), jz + .02f), K.V(fx, Mathf.Min(y1, yy + .55f), jz - .02f), LimeJoint, n, false);
                }
            }
        }

        /// <summary>A Colorbond fence (flat space, sheared): 2.4 m panels with darker ribs, posts, a capping rail.</summary>
        static void ColorbondFence(ComicMesh m, int s, float x, float z0, float z1, float y0, Color c)
        {
            const float h = 1.8f;
            for (float z = z0; z < z1 - .01f; z += 2.4f)
            {
                float e = Mathf.Min(z1, z + 2.4f);
                m.Surface = ComicSurface.Metal;
                K.MinMaxBox(m, K.V(s * x - .03f, y0, z), K.V(s * x + .03f, y0 + h, e), c);
                m.Surface = ComicSurface.Plain;
                Vector3 n = new Vector3(-s, 0, 0);
                for (float rz = z + .3f; rz < e - .1f; rz += .45f)
                    K.Face(m, K.V(s * (x - .035f), y0 + .05f, rz), K.V(s * (x - .035f), y0 + .05f, rz + .1f), K.V(s * (x - .035f), y0 + h - .1f, rz + .1f), K.V(s * (x - .035f), y0 + h - .1f, rz), Color.Lerp(c, K.R.ink, .25f), n, false);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(K.V(s * x, y0 + h * .5f, z), K.V(.09f, h + .05f, .09f), Color.Lerp(c, K.R.ink, .3f));
                K.MinMaxBox(m, K.V(s * x - .06f, y0 + h, z), K.V(s * x + .06f, y0 + h + .06f, e), Color.Lerp(c, K.R.ink, .3f));
            }
        }

        /// <summary>An estate house on a raised lot: rendered walls, a double garage with a Colorbond roller door, a
        /// portico with a column, framed windows with white sills, a hip roof with a ridge cap and a dark gutter line.</summary>
        static void House(CqChunkDraft d, CqLot l, Color wall, Color roof, Color door, bool garageFirst, bool twoStorey)
        {
            ComicMesh m = d.Frontage;
            float W = l.W, D = l.Depth, h = twoStorey ? 5.9f : 3.1f;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.2f, 0f, .02f, -D, K.R.ink);
            d.Wall(l, h + 3f);
            m.Surface = wall == HouseWalls[4] ? ComicSurface.Brick : ComicSurface.Plaster;
            K.LBox(m, l, 0, W, 0, h, 0, -D, wall);
            float g0 = garageFirst ? .3f : W - 6.3f, g1 = g0 + 6f;
            // the garage, stepped forward, with its roller door and ribs
            K.LBox(m, l, g0, g1, 0, 2.9f, 1f, 0, wall);
            m.Surface = ComicSurface.Metal;
            K.LQuad(m, l, g0 + .5f, g1 - .5f, 0f, 2.3f, 1.02f, door);
            for (float y = .35f; y < 2.2f; y += .35f) K.LBox(m, l, g0 + .5f, g1 - .5f, y - .02f, y + .02f, 1.06f, 1.02f, Color.Lerp(door, K.R.ink, .3f));
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, g0 - .1f, g1 + .1f, 2.85f, 3.05f, 1.15f, -.1f, Color.Lerp(wall, K.R.ink, .2f));
            // the entry: recessed dark timber door under a portico with a column
            float e0 = garageFirst ? g1 + .6f : g0 - 2.4f;
            m.Surface = ComicSurface.Wood;
            K.LQuad(m, l, e0, e0 + 1.1f, 0f, 2.3f, .03f, K.Hex("4A3326"));
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, e0 + 1.4f, e0 + 1.8f, 0f, 2.7f, 1.6f, 1.2f, Color.Lerp(wall, White, .4f));
            K.LBox(m, l, e0 - .3f, e0 + 2f, 2.7f, 2.95f, 1.7f, 0f, Color.Lerp(wall, White, .3f));
            // windows
            float w0 = garageFirst ? e0 + 2.6f : .8f, w1 = garageFirst ? W - .6f : e0 - .6f;
            for (int storey = 0; storey < (twoStorey ? 2 : 1); storey++)
            {
                float yb = storey * 2.8f;
                float a0 = storey == 0 ? w0 : .8f, a1 = storey == 0 ? w1 : W - .8f;
                for (float a = a0; a + 1.4f <= a1; a += 2.4f)
                {
                    m.Surface = ComicSurface.PaintedMetal;
                    K.LQuad(m, l, a, a + 1.6f, yb + .8f, yb + 2.3f, .02f, White);
                    m.Surface = ComicSurface.Glass;
                    K.LQuad(m, l, a + .12f, a + 1.48f, yb + .9f, yb + 2.2f, .04f, WinGlass);
                    m.Surface = ComicSurface.Concrete;
                    K.LBox(m, l, a - .1f, a + 1.7f, yb + .7f, yb + .82f, .14f, 0, White);
                }
            }
            if (twoStorey)
            {
                m.Surface = ComicSurface.Plaster;
                K.LBox(m, l, -.05f, W + .05f, 2.9f, 3.1f, .12f, -D, Color.Lerp(wall, K.R.ink, .15f));
            }
            // gutter and hip roof
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.55f, W + .55f, h - .05f, h + .15f, .65f, -D - .55f, Color.Lerp(roof, K.R.ink, .35f));
            HipRoof(m, l, -.5f, W + .5f, h + .15f, h + .15f + (D + 1.1f) * .32f, .6f, -D - .5f, roof);
        }

        /// <summary>A hip roof in lot space: front and back slopes, hipped ends, a ridge cap, a dark underside.</summary>
        static void HipRoof(ComicMesh m, CqLot l, float a0, float a1, float u0, float u1, float oFront, float oBack, Color c)
        {
            float depth = oFront - oBack, inset = depth * .5f, oMid = (oFront + oBack) * .5f;
            float r0 = a0 + inset, r1 = a1 - inset;
            if (r1 < r0 + .2f) { r0 = (a0 + a1) * .5f - .1f; r1 = r0 + .2f; }
            Vector3 f0 = l.P(a0, u0, oFront), f1 = l.P(a1, u0, oFront), b0 = l.P(a0, u0, oBack), b1 = l.P(a1, u0, oBack);
            Vector3 t0 = l.P(r0, u1, oMid), t1 = l.P(r1, u1, oMid);
            Vector3 toRoad = new Vector3(-l.S, 0, 0);
            m.Surface = ComicSurface.Metal;
            K.Face(m, f0, f1, t1, t0, c, (toRoad + Vector3.up).normalized);
            K.Face(m, b0, b1, t1, t0, Color.Lerp(c, K.R.ink, .12f), (-toRoad + Vector3.up).normalized);
            K.TriFacing(m, f0, b0, t0, Color.Lerp(c, K.R.ink, .06f), (Vector3.back + Vector3.up).normalized);
            K.TriFacing(m, f1, b1, t1, Color.Lerp(c, K.R.ink, .06f), (Vector3.forward + Vector3.up).normalized);
            K.Face(m, f0, f1, b1, b0, Color.Lerp(c, K.R.ink, .45f), Vector3.down);
            m.Surface = ComicSurface.PaintedMetal;
            m.Beam(t0 + Vector3.up * .05f, t1 + Vector3.up * .05f, .16f, Color.Lerp(c, White, .25f));
        }

        /// <summary>A simple house in world space (ridge along z): walls and a hip roof, for back rows and the far view.</summary>
        static void HipHouse(ComicMesh m, Vector3 p, float lenZ, float wideX, float wallH, Color wall, Color roof)
        {
            m.Surface = ComicSurface.Plaster;
            m.Box(p + K.V(0, wallH * .5f - .8f, 0), K.V(wideX, wallH + 1.6f, lenZ), wall);
            float y0 = p.y + wallH, y1 = y0 + wideX * .36f, hx = wideX * .5f + .5f, hz = lenZ * .5f + .5f, ridge = Mathf.Max(.2f, hz - hx);
            Vector3 a = K.V(p.x - hx, y0, p.z - hz), b = K.V(p.x + hx, y0, p.z - hz), c = K.V(p.x + hx, y0, p.z + hz), e = K.V(p.x - hx, y0, p.z + hz);
            Vector3 t0 = K.V(p.x, y1, p.z - ridge), t1 = K.V(p.x, y1, p.z + ridge);
            m.Surface = ComicSurface.Metal;
            K.Face(m, a, e, t1, t0, roof, (Vector3.left + Vector3.up).normalized);
            K.Face(m, b, c, t1, t0, Color.Lerp(roof, K.R.ink, .1f), (Vector3.right + Vector3.up).normalized);
            K.TriFacing(m, a, b, t0, Color.Lerp(roof, K.R.ink, .05f), (Vector3.back + Vector3.up).normalized);
            K.TriFacing(m, e, c, t1, Color.Lerp(roof, K.R.ink, .05f), (Vector3.forward + Vector3.up).normalized);
        }

        /// <summary>Aubin Grove station building: white walls under a wedge roof that rises towards the footbridge (+z), a
        /// dark glass band, an orange entry wall, a long white awning.</summary>
        static void StationBuilding(ComicMesh m, Vector3 p)
        {
            // p: centre of the front (road side) edge at the low end; the building runs +z for 22 m and +x for 13 m
            const float L = 22f, Wd = 13f, low = 4.2f, high = 11.5f;
            float x0 = p.x, x1 = p.x + Wd, z0 = p.z, z1 = p.z + L, y = p.y;
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, K.V(x0, y - 2f, z0), K.V(x1, y, z1), K.R.ink);
            m.Surface = ComicSurface.Plaster;
            Vector3 A(float x, float z, bool top) => K.V(x, y + (top ? Mathf.Lerp(low, high, (z - z0) / L) : 0f), z);
            K.Face(m, A(x0, z0, false), A(x0, z1, false), A(x0, z1, true), A(x0, z0, true), StationWhite, Vector3.left);
            K.Face(m, A(x1, z0, false), A(x1, z1, false), A(x1, z1, true), A(x1, z0, true), StationWhite, Vector3.right);
            K.Face(m, A(x0, z0, false), A(x1, z0, false), A(x1, z0, true), A(x0, z0, true), StationWhite, Vector3.back);
            K.Face(m, A(x0, z1, false), A(x1, z1, false), A(x1, z1, true), A(x0, z1, true), StationWhite, Vector3.forward);
            m.Surface = ComicSurface.Metal;
            Vector3 lift = K.V(0, .25f, 0);
            K.Face(m, A(x0 - .5f, z0 - .5f, true) + lift, A(x0 - .5f, z1 + .3f, true) + lift, A(x1 + .5f, z1 + .3f, true) + lift, A(x1 + .5f, z0 - .5f, true) + lift, K.Hex("E4E6E8"), (Vector3.up + Vector3.back * (high - low) / L).normalized);
            K.Face(m, A(x0 - .5f, z0 - .5f, true), A(x0 - .5f, z1 + .3f, true), A(x1 + .5f, z1 + .3f, true), A(x1 + .5f, z0 - .5f, true), Charcoal, Vector3.down);
            K.Face(m, A(x0 - .5f, z0 - .5f, true), A(x0 - .5f, z1 + .3f, true), A(x0 - .5f, z1 + .3f, true) + lift, A(x0 - .5f, z0 - .5f, true) + lift, Charcoal, Vector3.left);
            // dark glass band along the road side and the low end, the orange entry wall, an awning
            m.Surface = ComicSurface.Glass;
            K.MinMaxBox(m, K.V(x0 - .06f, y + .2f, z0 + 1f), K.V(x0 + .2f, y + 2.6f, z1 - 1f), ShopGlass);
            K.MinMaxBox(m, K.V(x0 + 1f, y + .2f, z0 - .06f), K.V(x1 - 1f, y + 2.6f, z0 + .2f), ShopGlass);
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = z0 + 3f; z < z1 - 1f; z += 3f) K.MinMaxBox(m, K.V(x0 - .1f, y + .2f, z - .06f), K.V(x0 + .05f, y + 2.6f, z + .06f), K.R.ink);
            K.MinMaxBox(m, K.V(x0 - .2f, y, z0 + 8f), K.V(x0 + .4f, y + 3.6f, z0 + 11f), Orange);
            K.MinMaxBox(m, K.V(x0 - 2.4f, y + 2.9f, z0 + 1f), K.V(x0, y + 3.15f, z1 - 1f), White);
            K.MinMaxBox(m, K.V(x0 - 2.45f, y + 2.75f, z0 + 1f), K.V(x0 - 2.3f, y + 3.15f, z1 - 1f), Charcoal);
        }

        // ================================================================ trees, reeds, kangaroos

        /// <summary>Paperbark (melaleuca): leaning cream papery trunks with tan peeling patches, a dense layered grey-green
        /// crown. cheap = one trunk, one crown (background).</summary>
        static void PaperbarkTree(ComicMesh m, Vector3 p, float h, int seed, bool cheap = false)
        {
            var r = P.Rng(seed);
            int trunks = cheap ? 1 : 2;
            for (int i = 0; i < trunks; i++)
            {
                float a = P.F(r, 0f, 360f) * Mathf.Deg2Rad, lean = P.F(r, .08f, .22f);
                Vector3 top = p + K.V(Mathf.Cos(a) * lean * h, h * P.F(r, .5f, .6f), Mathf.Sin(a) * lean * h);
                m.Surface = ComicSurface.Wood;
                P.Rod(m, p + K.V(0, -.3f, 0), top, .13f + .016f * h, Paperbark, .7f, cheap ? 6 : 8);
                if (!cheap)
                {
                    Vector3 mid = Vector3.Lerp(p, top, P.F(r, .3f, .55f));
                    Quaternion q = Quaternion.FromToRotation(Vector3.up, top - p);
                    m.Box(mid + q * K.V(.12f + .01f * h, 0, 0), K.V(.06f, P.F(r, .5f, .9f), .2f), PaperbarkPatch, q);
                    m.Box(mid + q * K.V(-.08f, .6f, .1f), K.V(.06f, .4f, .16f), PaperbarkPatch, q * Quaternion.Euler(0, 120f, 0));
                }
                P.Plant(m, top + K.V(0, h * .1f, 0), h * (cheap ? .5f : .42f), PaperLeaf, PaperLeafLight);
                if (!cheap) P.Plant(m, top + K.V(P.F(r, -.2f, .2f) * h, h * .24f, P.F(r, -.15f, .15f) * h), h * .26f, PaperLeafDark, PaperLeafLight);
            }
        }

        /// <summary>A clump of reeds and rushes: thin leaning blades in two greens, a few brown bulrush heads.</summary>
        static void Reeds(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = P.Rng(seed);
            for (int i = 0; i < 9; i++)
            {
                float a = P.F(r, 0f, 360f), tilt = P.F(r, 3f, 16f), len = s * P.F(r, .9f, 1.6f);
                Quaternion yaw = Quaternion.Euler(0, a, 0), q = yaw * Quaternion.Euler(tilt, 0, 0);
                Vector3 b = p + yaw * K.V(0, 0, P.F(r, .05f, .35f) * s);
                m.Surface = ComicSurface.Foliage;
                m.Box(b + q * K.V(0, len * .5f, 0), K.V(.08f * s, len, .05f * s), i % 3 == 0 ? ReedLight : Reed, q);
                if (i % 4 != 1) continue;
                m.Surface = ComicSurface.Fabric;
                m.Box(b + q * K.V(0, len * .8f, 0), K.V(.11f * s, .28f * s, .11f * s), Cattail, q);
            }
        }

        /// <summary>A western grey kangaroo sitting up, facing -Z: big haunches, long feet, a thick tapering tail, a slim
        /// chest with a pale belly, short arms, a long head with a dark nose, tall ears. grazing = bent down to the grass.</summary>
        static ComicMesh KangarooMesh(bool grazing = false)
        {
            var a = new ComicMesh { Surface = ComicSurface.Fabric };
            a.Ellipsoid(K.V(0, .42f, .12f), K.V(.5f, .62f, .74f), Roo);                                      // haunches
            foreach (int s in new[] { -1, 1 })
            {
                a.Box(K.V(s * .13f, .05f, -.08f), K.V(.12f, .09f, .52f), RooDark);                           // long feet
                a.Ellipsoid(K.V(s * .17f, .34f, .1f), K.V(.18f, .44f, .5f), Color.Lerp(Roo, RooDark, .2f));  // thighs
            }
            a.Cylinder(K.V(0, .26f, .78f), .13f, 1.05f, Roo, 8, .45f, Quaternion.Euler(100f, 0, 0));         // tail, thin end on the ground
            float lean = grazing ? 38f : 8f;
            Quaternion body = Quaternion.Euler(-lean, 0, 0);
            Vector3 chest = K.V(0, .62f, -.05f);
            a.Add(M(t =>
            {
                t.Surface = ComicSurface.Fabric;
                t.Ellipsoid(K.V(0, .3f, 0), K.V(.4f, .7f, .42f), Roo);
                t.Ellipsoid(K.V(0, .26f, -.14f), K.V(.28f, .5f, .2f), RooBelly);
                foreach (int s in new[] { -1, 1 }) t.Box(K.V(s * .12f, .28f, -.22f), K.V(.06f, .26f, .07f), RooDark, Quaternion.Euler(-30f, 0, s * 8f));
                t.Ellipsoid(K.V(0, .72f, -.08f), K.V(.22f, .26f, .3f), Roo);                               // head
                t.Box(K.V(0, .68f, -.26f), K.V(.13f, .13f, .18f), Roo);                                      // snout
                t.Box(K.V(0, .7f, -.36f), K.V(.08f, .07f, .05f), K.R.ink);                                   // nose
                foreach (int s in new[] { -1, 1 })
                {
                    t.Box(K.V(s * .085f, .76f, -.17f), K.V(.03f, .04f, .03f), K.R.ink);                     // eyes
                    t.Box(K.V(s * .08f, .95f, -.02f), K.V(.07f, .22f, .04f), Roo, Quaternion.Euler(0, 0, s * -14f));
                    t.Box(K.V(s * .08f, .95f, -.045f), K.V(.04f, .16f, .02f), RooBelly, Quaternion.Euler(0, 0, s * -14f));
                }
            }), chest, body, Vector3.one);
            return a;
        }

        /// <summary>An animal prefab: Obstacle (kind Person, animal, not punchable) + Pedestrian; the body hops while it moves.</summary>
        static GameObject SaveKangaroo(string path, float walk)
        {
            ComicMesh art = KangarooMesh(walk == 0f);
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            GameObject pivot = sync.Plain("Art");
            var inner = new ChildSync(pivot.transform);
            GameObject body = inner.Plain("Body");
            K.Renderer(body, System.IO.Path.GetFileNameWithoutExtension(path), art, true, false, false);
            var hop = PrefabBuildUtility.GetOrAdd<KangarooHop>(body);
            hop.hopsPerSecond = 1.6f; hop.height = .35f; hop.lean = 14f;
            inner.Finish();
            sync.Finish();
            var box = PrefabBuildUtility.KeepSingle<BoxCollider>(root) ?? root.AddComponent<BoxCollider>();
            box.center = new Vector3(0, .6f, .2f); box.size = new Vector3(.6f, 1.2f, 1.3f); box.isTrigger = false;
            var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
            o.kind = ObstacleKind.Person; o.animal = true; o.punchable = false; o.police = false; o.canJumpOver = false; o.spansRoad = false; o.spawnWeight = 1f;
            var ped = PrefabBuildUtility.GetOrAdd<Pedestrian>(root);
            ped.walkSpeed = walk; ped.crosses = false; ped.art = pivot.transform; ped.dodgeSpeed = 5f;
            PrefabBuildUtility.SaveAndClose(root, path);
            K.Report.Add(System.IO.Path.GetFileNameWithoutExtension(path) + " " + art.TriangleCount * 2 + " tris");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // ================================================================ ramps

        /// <summary>A limestone kicker (deck in flat space): the kit's ramp body in limestone with block courses and a pale
        /// cap at the lip, plus vertical joints on the sides.</summary>
        static ComicMesh LimestoneRampArt(CqRampSpec r, float lipBottom)
        {
            var m = new ComicMesh();
            K.RampMesh(m, r, Limestone, Color.Lerp(Limestone, LimeJoint, .45f), false, lipBottom);
            m.Surface = ComicSurface.Plain;
            float x0 = r.X - r.Width * .5f, x1 = r.X + r.Width * .5f;
            foreach (float x in new[] { x0, x1 })
            {
                Vector3 n = new Vector3(x < r.X ? -1 : 1, 0, 0);
                float fx = x + n.x * .01f;
                for (float z = r.Z + 1.2f; z < r.Lip - .2f; z += 1.2f)
                {
                    float top = z < r.Z + r.Rise ? r.Height * (z - r.Z) / r.Rise : r.Height;
                    K.Face(m, K.V(fx, Mathf.Max(lipBottom, -.3f), z - .02f), K.V(fx, Mathf.Max(lipBottom, -.3f), z + .02f), K.V(fx, top - .02f, z + .02f), K.V(fx, top - .02f, z - .02f), LimeJoint, n, false);
                }
            }
            return m;
        }

        static Ramp SaveRampArt(string path, string meshId, CqRampSpec spec, float lipBottom)
        {
            ComicMesh art = LimestoneRampArt(spec, lipBottom);
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            GameObject go = sync.Plain("Art");
            K.Renderer(go, meshId, art, true, true, false);
            sync.Finish();
            var ramp = PrefabBuildUtility.GetOrAdd<Ramp>(root);
            ramp.width = spec.Width; ramp.rise = spec.Rise; ramp.height = spec.Height; ramp.top = spec.Top; ramp.launch = spec.Launch; ramp.hangGravity = spec.Hang;
            ramp.lanes = Array.Empty<int>();
            foreach (Collider c in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c, true);
            PrefabBuildUtility.SaveAndClose(root, path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Ramp>();
        }

        // ================================================================ obstacle and vehicle meshes

        /// <summary>Shopping trolleys (n nested in a line along z): chrome wire basket (grey panels with ink grid), red
        /// handle, child-seat flap, chassis rails, four small wheels. Front at +Z.</summary>
        static ComicMesh TrolleyMesh(int n)
        {
            var m = new ComicMesh();
            Color chrome = K.Hex("C9CFD6"), panel = K.Hex("AEB6BF"), grid = K.Hex("6B737C");
            for (int i = 0; i < n; i++)
            {
                Vector3 o = K.V(0, 0, (i - (n - 1) * .5f) * .55f);
                m.Surface = ComicSurface.Metal;
                // basket: tapered (narrow at the front), rim beams, side panels with grid lines
                Vector3 fl = o + K.V(-.26f, .5f, .45f), fr = o + K.V(.26f, .5f, .45f), bl = o + K.V(-.32f, .5f, -.4f), br = o + K.V(.32f, .5f, -.4f);
                Vector3 up = K.V(0, .48f, 0);
                K.Face(m, fl, bl, bl + up, fl + up, panel, Vector3.left);
                K.Face(m, fr, br, br + up, fr + up, panel, Vector3.right);
                K.Face(m, fl, fr, fr + up, fl + up, panel, Vector3.forward);
                K.Face(m, fl, fr, br, bl, Color.Lerp(panel, grid, .4f), Vector3.down);
                m.Surface = ComicSurface.Plain;
                for (int k = 1; k < 4; k++)
                {
                    float t = k / 4f;
                    Vector3 lA = Vector3.Lerp(fl, bl, t), rA = Vector3.Lerp(fr, br, t);
                    K.Face(m, lA - K.V(.005f, 0, .015f), lA + K.V(-.005f, 0, .015f), lA + up + K.V(-.005f, 0, .015f), lA + up - K.V(.005f, 0, .015f), grid, Vector3.left, false);
                    K.Face(m, rA - K.V(-.005f, 0, .015f), rA + K.V(.005f, 0, .015f), rA + up + K.V(.005f, 0, .015f), rA + up - K.V(-.005f, 0, .015f), grid, Vector3.right, false);
                }
                m.Surface = ComicSurface.Metal;
                foreach ((Vector3 a, Vector3 b) in new[] { (fl, fr), (bl, br), (fl, bl), (fr, br) })
                {
                    m.Beam(a + up, b + up, .035f, chrome);
                    m.Beam(a + K.V(0, .24f, 0), b + K.V(0, .24f, 0), .02f, chrome);
                }
                // handle and child seat
                m.Surface = ComicSurface.Plastic;
                m.Box(o + K.V(0, 1.02f, -.47f), K.V(.66f, .06f, .06f), SlideRed);
                m.Surface = ComicSurface.Metal;
                m.Beam(o + K.V(-.32f, .98f, -.4f), o + K.V(-.32f, 1.02f, -.47f), .03f, chrome);
                m.Beam(o + K.V(.32f, .98f, -.4f), o + K.V(.32f, 1.02f, -.47f), .03f, chrome);
                m.Surface = ComicSurface.Plastic;
                m.Box(o + K.V(0, .84f, -.36f), K.V(.5f, .26f, .04f), K.Hex("2E7FD0"));
                // chassis and wheels
                m.Surface = ComicSurface.Metal;
                foreach (int s in new[] { -1, 1 })
                {
                    m.Beam(o + K.V(s * .26f, .12f, .45f), o + K.V(s * .3f, .12f, -.42f), .04f, chrome);
                    m.Beam(o + K.V(s * .3f, .12f, -.42f), o + K.V(s * .32f, .5f, -.4f), .04f, chrome);
                    m.Beam(o + K.V(s * .24f, .12f, .4f), o + K.V(s * .26f, .5f, .45f), .03f, chrome);
                    m.Surface = ComicSurface.Rubber;
                    foreach (float z in new[] { .38f, -.36f }) m.Cylinder(o + K.V(s * .26f, .06f, z), .06f, .05f, K.R.ink, 8, 1f, Quaternion.Euler(0, 0, 90));
                    m.Surface = ComicSurface.Metal;
                }
                m.Box(o + K.V(0, .14f, 0), K.V(.5f, .03f, .8f), Color.Lerp(chrome, grid, .4f));
            }
            return m;
        }

        /// <summary>An A-frame footpath sign: two framed boards (blank chalkboard with cream scribble bars, a coloured header).</summary>
        static ComicMesh AFrameMesh()
        {
            var m = new ComicMesh();
            Color board = K.Hex("2E4A3C"), frame = K.Hex("8A5A3C");
            foreach (int s in new[] { -1, 1 })
            {
                Quaternion q = Quaternion.Euler(s * 14f, 0, 0);
                Vector3 c = K.V(0, .5f, s * .13f);
                m.Surface = ComicSurface.Wood;
                m.Box(c, K.V(.62f, 1f, .05f), frame, q);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(c + q * K.V(0, -.02f, s * .03f), K.V(.5f, .78f, .02f), board, q);
                m.Box(c + q * K.V(0, .3f, s * .04f), K.V(.5f, .14f, .02f), s < 0 ? SlideYellow : Orange, q);
                for (int i = 0; i < 3; i++) m.Box(c + q * K.V(-.05f + (i % 2) * .06f, .08f - i * .14f, s * .045f), K.V(.32f - (i % 2) * .1f, .035f, .01f), K.R.cream, q);
            }
            return m;
        }

        static void Wheels(ComicMesh m, float x, float r, float y, params float[] zs)
        {
            foreach (float z in zs)
                for (int s = -1; s <= 1; s += 2)
                {
                    m.Surface = ComicSurface.Rubber;
                    m.Cylinder(K.V(s * x, y, z), r, .3f, K.R.ink, 8, 1, Quaternion.Euler(0, 0, 90));
                    m.Surface = ComicSurface.Metal;
                    m.Cylinder(K.V(s * (x + .03f), y, z), r * .45f, .3f, K.Hex("D1DBE5"), 8, 1, Quaternion.Euler(0, 0, 90));
                }
        }

        /// <summary>A plain white delivery van (no brand): cab and box body, framed windscreen, a teal stripe, rear doors,
        /// lights, ink bumpers, wheels. Front at +Z.</summary>
        static ComicMesh VanMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            Color body = K.Hex("F2F0EA"), dark = K.Hex("26333D");
            m.Box(K.V(0, 1.4f, -.55f), K.V(2f, 2.1f, 4f), body);
            m.Box(K.V(0, 1f, 1.95f), K.V(1.96f, 1.3f, 1.1f), body);
            m.Box(K.V(0, 1.9f, 1.55f), K.V(1.9f, .5f, .4f), body);
            m.Box(K.V(0, .62f, -.1f), K.V(2.04f, .26f, 5f), Teal);
            m.Box(K.V(0, 1.6f, -.6f), K.V(2.03f, .3f, 3.6f), Lime);
            m.Box(K.V(0, 2.48f, -.55f), K.V(2.04f, .08f, 4.04f), K.R.ink);
            m.Box(K.V(0, 1.72f, 1.78f), K.V(1.84f, .78f, .06f), K.R.ink);
            m.Surface = ComicSurface.Glass;
            m.Box(K.V(0, 1.72f, 1.82f), K.V(1.7f, .64f, .04f), dark);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(K.V(s * .99f, 1.7f, 1.95f), K.V(.06f, .62f, .9f), K.R.ink);
                m.Surface = ComicSurface.Glass;
                m.Box(K.V(s * 1.01f, 1.7f, 1.95f), K.V(.04f, .5f, .78f), dark);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(K.V(s * .72f, 1f, 2.52f), K.V(.36f, .2f, .06f), K.R.cream);
                m.Box(K.V(s * .8f, 1.1f, -2.57f), K.V(.24f, .34f, .06f), K.R.red);
            }
            m.Box(K.V(0, 1.45f, -2.57f), K.V(.06f, 1.8f, .06f), K.R.ink);
            m.Box(K.V(0, .45f, 2.58f), K.V(2.04f, .26f, .14f), K.R.ink);
            m.Box(K.V(0, .45f, -2.6f), K.V(2.04f, .26f, .14f), K.R.ink);
            Wheels(m, .95f, .38f, .38f, 1.55f, -1.75f);
            return m;
        }

        /// <summary>A suburban bus (no brand): white body, grey-blue skirt, window band with ink pillars, doors on the kerb
        /// side, a blank yellow destination board, bumpers, wheels. Front at +Z.</summary>
        static ComicMesh BusMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            Color body = K.Hex("F2F2EE"), skirt = K.Hex("4F6F86"), dark = K.Hex("26333D");
            const float L = 12f, hz = 6f;
            m.Box(K.V(0, 1.8f, 0), K.V(2.5f, 2.6f, L), body);
            m.Box(K.V(0, .72f, 0), K.V(2.54f, .5f, L + .04f), skirt);
            m.Box(K.V(0, 3.12f, 0), K.V(2.4f, .1f, L - .2f), K.R.ink);
            m.Box(K.V(0, 3.3f, -1.2f), K.V(1.8f, .3f, 3.2f), K.Hex("C9CDD2"));
            foreach (int s in new[] { -1, 1 })
            {
                m.Surface = ComicSurface.Glass;
                m.Box(K.V(s * 1.255f, 2.2f, -.6f), K.V(.06f, 1.05f, L - 3f), dark);
                m.Surface = ComicSurface.PaintedMetal;
                for (float z = -hz + 1f; z < hz - 2f; z += 1.6f) m.Box(K.V(s * 1.275f, 2.2f, z), K.V(.06f, 1.1f, .14f), K.R.ink);
                m.Box(K.V(s * 1.27f, 1.55f, 0), K.V(.06f, .1f, L - .3f), K.R.ink);
            }
            foreach (float z in new[] { hz - 1.4f, -.8f })
            {
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(K.V(-1.27f, 1.65f, z), K.V(.07f, 2.3f, 1.2f), K.R.ink);
                m.Surface = ComicSurface.Glass;
                m.Box(K.V(-1.3f, 1.75f, z), K.V(.05f, 1.9f, 1f), dark);
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(K.V(0, 2f, hz + .02f), K.V(2.36f, 1.66f, .06f), K.R.ink);
            m.Surface = ComicSurface.Glass;
            m.Box(K.V(0, 1.98f, hz + .05f), K.V(2.2f, 1.45f, .04f), dark);
            m.Box(K.V(0, 2.4f, -hz - .04f), K.V(2f, .8f, .06f), dark);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(K.V(0, 3f, hz + .02f), K.V(1.7f, .3f, .1f), K.R.ink);
            m.Box(K.V(0, 3f, hz + .08f), K.V(1.5f, .18f, .04f), SlideYellow);
            m.Box(K.V(0, .5f, hz + .08f), K.V(2.56f, .3f, .14f), K.R.ink);
            m.Box(K.V(0, .5f, -hz - .08f), K.V(2.56f, .3f, .14f), K.R.ink);
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(K.V(s * .9f, .95f, hz + .04f), K.V(.38f, .2f, .06f), K.R.cream);
                m.Box(K.V(s * .95f, 1f, -hz - .04f), K.V(.3f, .28f, .06f), K.R.red);
            }
            Wheels(m, 1.12f, .5f, .5f, hz - 2.4f, -hz + 2.8f);
            return m;
        }

        /// <summary>A small hatch / sedan: body, cabin, window band, roof, bumpers, lights, wheels. Front at +Z.</summary>
        static ComicMesh CarMesh(Color body)
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            Color dark = K.Hex("26333D");
            m.Box(K.V(0, .62f, 0), K.V(1.8f, .62f, 4.2f), body);
            m.Box(K.V(0, 1.12f, -.3f), K.V(1.56f, .5f, 2.3f), body);
            m.Surface = ComicSurface.Glass;
            m.Box(K.V(0, 1.14f, -.3f), K.V(1.6f, .34f, 2.1f), dark);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(K.V(0, 1.39f, -.3f), K.V(1.5f, .06f, 2.1f), Color.Lerp(body, K.R.ink, .2f));
            m.Box(K.V(0, .42f, 2.12f), K.V(1.84f, .22f, .1f), K.R.ink);
            m.Box(K.V(0, .42f, -2.12f), K.V(1.84f, .22f, .1f), K.R.ink);
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(K.V(s * .62f, .78f, 2.11f), K.V(.32f, .14f, .04f), K.R.cream);
                m.Box(K.V(s * .66f, .8f, -2.11f), K.V(.26f, .16f, .04f), K.R.red);
            }
            Wheels(m, .82f, .33f, .33f, 1.35f, -1.35f);
            return m;
        }

        /// <summary>A suburban train car (no brand): silver body, dark window band with ink pillars, doors, a roof with a
        /// pantograph, a yellow nose at the front end (+Z) if leading.</summary>
        static void TrainCar(ComicMesh m, Vector3 p, float len, bool leading)
        {
            Color dark = K.Hex("26333D");
            m.Surface = ComicSurface.Metal;
            m.Box(p + K.V(0, 2.05f, 0), K.V(3f, 3f, len), TrainSilver);
            m.Box(p + K.V(0, .55f, 0), K.V(2.6f, .4f, len - 1f), DeckDark);
            m.Surface = ComicSurface.Glass;
            m.Box(p + K.V(0, 2.5f, 0), K.V(3.04f, .95f, len - 1.4f), dark);
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = -len * .5f + 1.6f; z < len * .5f - 1f; z += 2.2f) m.Box(p + K.V(0, 2.5f, z), K.V(3.08f, 1f, .16f), TrainSilver);
            foreach (float z in new[] { -len * .3f, len * .1f })
            {
                m.Box(p + K.V(0, 1.85f, z), K.V(3.06f, 2.4f, 1.4f), K.R.ink);
                m.Box(p + K.V(0, 1.85f, z), K.V(3.08f, 2.2f, 1.2f), Color.Lerp(TrainSilver, K.R.ink, .2f));
            }
            m.Box(p + K.V(0, 3.62f, 0), K.V(2.6f, .16f, len - .6f), Color.Lerp(TrainSilver, K.R.ink, .25f));
            m.Beam(p + K.V(-.5f, 3.75f, -1f), p + K.V(.3f, 4.6f, 0f), .08f, K.R.ink);
            m.Box(p + K.V(.3f, 4.62f, 0f), K.V(1.4f, .06f, .2f), K.R.ink);
            if (!leading) return;
            m.Box(p + K.V(0, 2f, len * .5f + .25f), K.V(2.9f, 2.9f, .5f), SlideYellow);
            m.Surface = ComicSurface.Glass;
            m.Box(p + K.V(0, 2.6f, len * .5f + .52f), K.V(2.3f, .9f, .06f), dark);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (int s in new[] { -1, 1 }) m.Box(p + K.V(s * .9f, 1.2f, len * .5f + .52f), K.V(.36f, .2f, .05f), K.R.cream);
        }

        /// <summary>Bin day: a red-lid general bin and a yellow-lid recycling bin (dark green bodies, wheels, lids).</summary>
        static ComicMesh WheelieBinsMesh()
        {
            var m = new ComicMesh();
            Color body = K.Hex("2F5D3A");
            foreach ((float x, Color lid) in new[] { (-.36f, K.Hex("C9302C")), (.36f, SlideYellow) })
            {
                m.Surface = ComicSurface.Plastic;
                m.Cylinder(K.V(x, .5f, 0), .36f, .94f, body, 4, 1.12f, Quaternion.Euler(0, 45, 0));
                m.Box(K.V(x, 1.0f, .02f), K.V(.62f, .08f, .68f), lid);
                m.Box(K.V(x, .96f, -.33f), K.V(.5f, .06f, .06f), Color.Lerp(body, K.R.ink, .3f));
                m.Box(K.V(x, .55f, .3f), K.V(.3f, .2f, .02f), K.R.cream);
                m.Surface = ComicSurface.Rubber;
                foreach (int s in new[] { -1, 1 }) m.Cylinder(K.V(x + s * .24f, .1f, -.26f), .1f, .07f, K.R.ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            }
            return m;
        }

        /// <summary>A rendered letterbox pillar with a limestone cap, a dark slot and a house-number plate (blank).</summary>
        static ComicMesh LetterboxMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Concrete };
            m.Box(K.V(0, .05f, 0), K.V(.56f, .1f, .56f), K.R.ink);
            m.Surface = ComicSurface.Plaster;
            m.Box(K.V(0, .5f, 0), K.V(.48f, .86f, .48f), HouseWalls[1]);
            m.Surface = ComicSurface.Concrete;
            m.Box(K.V(0, .98f, 0), K.V(.56f, .1f, .56f), Limestone);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(K.V(0, .78f, -.245f), K.V(.3f, .05f, .02f), K.R.ink);
            m.Box(K.V(0, .52f, -.245f), K.V(.2f, .14f, .02f), Charcoal);
            return m;
        }

        /// <summary>The yellow diamond kangaroo warning sign on a pole (silhouette made of ink blocks, no text).</summary>
        static ComicMesh RooSignMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(K.V(0, .08f, 0), .16f, .16f, K.R.ink, 8);
            m.Cylinder(K.V(0, 1.25f, 0), .05f, 2.5f, Silver, 8);
            Quaternion diamond = Quaternion.Euler(0, 0, 45);
            Vector3 c = K.V(0, 2.2f, -.05f);
            m.Box(c, K.V(.84f, .84f, .04f), K.R.ink, diamond);
            m.Box(c + K.V(0, 0, -.025f), K.V(.74f, .74f, .02f), SlideYellow, diamond);
            Vector3 f = c + K.V(0, 0, -.04f);
            m.Box(f + K.V(.02f, -.04f, 0), K.V(.26f, .2f, .02f), K.R.ink, Quaternion.Euler(0, 0, 20));             // body
            m.Box(f + K.V(-.1f, .1f, 0), K.V(.1f, .12f, .02f), K.R.ink, Quaternion.Euler(0, 0, -20));             // head
            m.Box(f + K.V(-.08f, .19f, 0), K.V(.03f, .08f, .02f), K.R.ink);                                        // ear
            m.Box(f + K.V(.2f, -.2f, 0), K.V(.3f, .04f, .02f), K.R.ink, Quaternion.Euler(0, 0, -25));             // tail
            m.Box(f + K.V(-.02f, -.2f, 0), K.V(.2f, .04f, .02f), K.R.ink);                                         // feet
            return m;
        }

        // ================================================================ far scenery (the whole horizon: no city here)

        static void FlatQ(ComicMesh m, float x0, float x1, float z0, float z1, float y, Color c)
        {
            m.Surface = ComicSurface.Plain;
            K.Face(m, K.V(Mathf.Min(x0, x1), y, z0), K.V(Mathf.Min(x0, x1), y, z1), K.V(Mathf.Max(x0, x1), y, z1), K.V(Mathf.Max(x0, x1), y, z0), c, Vector3.up, false);
        }

        /// <summary>Aerial perspective: colours fade towards the haze with distance (the fog adds more on top).</summary>
        static Color Air(Color c, float distance) => Color.Lerp(c, Haze, Mathf.Clamp01((distance - 80f) / 220f) * .7f);

        /// <summary>The Darling Scarp far to the east: a long, low, nearly flat-topped line of hills, blue with distance, from
        /// angle a0 to a1 (degrees from straight ahead, - = left) at radius r. A silhouette strip facing the rider with a
        /// lighter slope going back over the top (nothing below the ground).</summary>
        static void RangeBand(ComicMesh m, float a0, float a1, float r, float g, int seed)
        {
            var rng = P.Rng(seed);
            m.Surface = ComicSurface.Foliage;
            bool has = false;
            Vector3 pb = default, pt = default, pk = default;
            for (float a = a0; a <= a1 + .01f; a += 2.5f)
            {
                float rad = a * Mathf.Deg2Rad;
                float x = Mathf.Sin(rad) * r, z = Mathf.Cos(rad) * r;
                float h = 13f + 4f * Mathf.Sin(a * .19f + seed) + 2.5f * Mathf.Sin(a * .53f) + P.F(rng, -1f, 1f);
                Vector3 b = K.V(x, -g * z - 1f, z), t = K.V(x, -g * z + h, z), k = K.V(Mathf.Sin(rad) * (r + 70f), -g * z + h * .55f, Mathf.Cos(rad) * (r + 70f));
                if (has && Mathf.Abs(x) > 60f && Mathf.Abs(pb.x) > 60f)
                {
                    Vector3 toRider = -new Vector3(x + pb.x, 0f, z + pb.z).normalized;
                    Color c = Color.Lerp(Range, Haze, .25f + .08f * Mathf.Sin(a * .7f));
                    K.Face(m, pb, b, t, pt, c, toRider, false);
                    K.Face(m, pt, t, k, pk, Color.Lerp(c, Haze, .3f), (Vector3.up + toRider * .5f).normalized, false);
                }
                pb = b; pt = t; pk = k; has = true;
            }
        }

        /// <summary>A band of trees (blobs on short trunks) from angle a0 to a1 at radius r.. r + depth.</summary>
        static void TreeBand(ComicMesh m, float a0, float a1, float r, float depth, float g, int seed, Color leaf, float size = 12f)
        {
            var rng = P.Rng(seed);
            for (float a = a0; a <= a1; a += 2.6f)
            {
                float rad = (a + P.F(rng, -1f, 1f)) * Mathf.Deg2Rad, rr = r + P.F(rng, 0f, depth);
                float x = Mathf.Sin(rad) * rr, z = Mathf.Cos(rad) * rr;
                if (Mathf.Abs(x) < 58f) continue;
                float s = size * P.F(rng, .7f, 1.3f);
                Color c = Air(Color.Lerp(leaf, K.R.ink, P.F(rng, 0f, .15f)), rr);
                m.Surface = ComicSurface.Foliage;
                m.Ellipsoid(K.V(x, -g * z + s * .55f, z), K.V(s * 1.3f, s, s * 1.1f), c);
                m.Ellipsoid(K.V(x + s * .3f, -g * z + s * .8f, z - s * .2f), K.V(s * .8f, s * .7f, s * .8f), Air(Color.Lerp(leaf, K.R.yellow, .2f), rr));
            }
        }

        /// <summary>A far mid-rise block: render body, balcony bands, a coloured stripe, roof plant.</summary>
        static void FarBlock(ComicMesh m, Vector3 p, float w, float d, float h, Color accent, float dist)
        {
            Color body = Air(Render, dist), band = Air(Color.Lerp(Render, K.R.ink, .25f), dist), acc = Air(accent, dist);
            m.Surface = ComicSurface.Plaster;
            m.Box(p + K.V(0, h * .5f - 2f, 0), K.V(w, h + 4f, d), body);
            for (float y = 4f; y < h - 1f; y += 3.1f) m.Box(p + K.V(0, y, 0), K.V(w + .4f, .4f, d + .4f), band);
            m.Box(p + K.V(-w * .25f, h * .5f, -d * .5f - .15f), K.V(w * .2f, h - 4f, .3f), acc);
            m.Box(p + K.V(0, h + .7f, 0), K.V(w * .4f, 1.4f, d * .4f), Air(PlantGrey, dist));
        }

        static ComicMesh CockburnFar()
        {
            float g = -CockburnDrop / 40f;
            var flat = new ComicMesh();
            // the ground beyond the chunks: suburb and dry grass both sides; the freeway (two carriageways) and the railway
            // in its median on the left (east), out to the fog
            for (float z = -120f; z < 360f; z += 40f)
            {
                FlatQ(flat, 46f, 340f, z, z + 40f, -.3f, K.Hex("A9AE7E"));
                FlatQ(flat, -46f, -54f, z, z + 40f, -.3f, K.Hex("B9B08A"));
                FlatQ(flat, -54f, -66f, z, z + 40f, -.25f, K.R.asphalt);
                FlatQ(flat, -66f, -76f, z, z + 40f, -.25f, Ballast);
                FlatQ(flat, -76f, -88f, z, z + 40f, -.25f, K.R.asphalt);
                FlatQ(flat, -88f, -340f, z, z + 40f, -.3f, K.Hex("B3AD84"));
                for (float dz = 0f; dz < 40f; dz += 10f)
                    foreach (float lx in new[] { -58f, -62f, -80f, -84f }) FlatQ(flat, lx - .15f, lx + .15f, z + dz, z + dz + 4f, -.22f, White);
                foreach (float rx in new[] { -68.5f, -73.5f }) FlatQ(flat, rx - .5f, rx + .5f, z, z + 40f, -.2f, RailSteel);
            }
            flat.Shear(g);
            var m = new ComicMesh();
            m.Add(flat, Vector3.zero, Quaternion.identity, Vector3.one);
            float Y(float z) => -g * z;
            // ---- layer 1 (left): the multi-storey car park, the station (platform canopy, concourse, a train), the
            // Beeliar Drive overpass over the freeway
            for (float z = 10f; z < 80f; z += 20f)
            {
                Vector3 p = K.V(-112f, Y(z + 10f), z + 10f);
                float d = Vector3.Distance(p, Vector3.zero);
                m.Surface = ComicSurface.Concrete;
                m.Box(p + K.V(0, 6f, 0), K.V(26f, 16f, 20f), Air(DeckDark, d));
                for (float y = 0f; y <= 14f; y += 3f) m.Box(p + K.V(0, y + .3f, 0), K.V(26.4f, .7f, 20.2f), Air(y % 6f < 1f ? White : Concrete, d));
                m.Box(p + K.V(13.4f, 7.5f, 0), K.V(.6f, 17f, 1.2f), Air(Concrete, d));
            }
            m.Box(K.V(-99f, Y(40f) + 9f, 40f), K.V(3f, 20f, 5f), Air(Silver, 110f));
            for (float z = 70f; z < 150f; z += 10f)
            {
                Vector3 p = K.V(-71f, Y(z + 5f), z + 5f);
                float d = p.magnitude;
                m.Surface = ComicSurface.Metal;
                m.Box(p + K.V(0, 5.6f, 0), K.V(12f, .5f, 10.2f), Air(White, d));
                m.Box(p + K.V(0, 2.5f, 0), K.V(.4f, 5f, .4f), Air(SilverDark, d));
                m.Box(p + K.V(0, .3f, 0), K.V(5f, 1.1f, 10f), Air(Concrete, d));
            }
            m.Surface = ComicSurface.Metal;
            m.Box(K.V(-60f, Y(110f) + 4f, 110f), K.V(10f, 8f, 26f), Air(Silver, 125f));
            m.Box(K.V(-60f, Y(110f) + 8.4f, 110f), K.V(12f, .8f, 28f), Air(White, 125f));
            for (int i = 0; i < 3; i++) TrainCar(m, K.V(-68.5f, Y(85f + i * 21f) - .2f, 85f + i * 21f), 20f, i == 2);
            {
                float z = 160f, y = Y(z);
                m.Surface = ComicSurface.Concrete;
                m.Box(K.V(-110f, y + 7.6f, z), K.V(120f, 1.4f, 12f), Air(Concrete, 190f));
                m.Box(K.V(-110f, y + 8.8f, z - 6f), K.V(120f, 1f, .4f), Air(White, 190f));
                foreach (float px in new[] { -60f, -82f, -105f, -130f, -155f }) m.Box(K.V(px, y + 3.5f, z), K.V(2f, 8f, 8f), Air(Concrete, 190f));
                for (float x = -160f; x < -55f; x += 25f) d_FarCar(m, K.V(x, y + 8.3f, z - 2f), 190f);
            }
            // ---- layer 2: apartment clusters ahead-left and ahead-right, the ARC and the shopping centre on the right
            var rng = P.Rng(1001);
            Color[] acc = { Lime, Teal, Orange, Timber };
            foreach ((float x0, float x1, float z0, float z1, int n) in new[] { (-62f, -100f, 175f, 235f, 5), (60f, 98f, 170f, 230f, 5), (-120f, -160f, 90f, 140f, 3) })
                for (int i = 0; i < n; i++)
                {
                    float x = Mathf.Lerp(x0, x1, P.F(rng, 0f, 1f)), z = Mathf.Lerp(z0, z1, (i + P.F(rng, 0f, .8f)) / n);
                    Vector3 p = K.V(x, Y(z), z);
                    FarBlock(m, p, P.F(rng, 14f, 22f), P.F(rng, 12f, 18f), P.F(rng, 16f, 34f), acc[rng.Next(acc.Length)], p.magnitude);
                }
            {
                // the ARC: a long cream hall under a big curved silver roof, waterslides in colour
                Vector3 p = K.V(112f, Y(125f), 125f);
                float d = p.magnitude;
                m.Surface = ComicSurface.Plaster;
                m.Box(p + K.V(0, 4f, 0), K.V(34f, 9f, 46f), Air(ArcCream, d));
                m.Box(p + K.V(-17.2f, 2.2f, 0), K.V(.5f, 4f, 44f), Air(ShopGlass, d));
                m.Surface = ComicSurface.Metal;
                for (int i = 0; i < 10; i++)
                {
                    float t0 = i / 10f, t1 = (i + 1) / 10f;
                    float xa = Mathf.Lerp(-19f, 19f, t0), xb = Mathf.Lerp(-19f, 19f, t1), ya = 8.5f + 8f * Mathf.Sin(Mathf.PI * Mathf.Pow(t0, .8f)), yb = 8.5f + 8f * Mathf.Sin(Mathf.PI * Mathf.Pow(t1, .8f));
                    K.Face(m, p + K.V(xa, ya, -24f), p + K.V(xa, ya, 24f), p + K.V(xb, yb, 24f), p + K.V(xb, yb, -24f), Air(Color.Lerp(ArcRoof, K.R.ink, i % 2 == 0 ? 0f : .08f), d), Vector3.up);
                    K.Face(m, p + K.V(xa, 8.5f, -23.5f), p + K.V(xb, 8.5f, -23.5f), p + K.V(xb, yb, -23.5f), p + K.V(xa, ya, -23.5f), Air(AtriumGlass, d), Vector3.back);
                }
                var slide = new ComicMesh();
                SlideTower(slide, Vector3.zero);
                m.Add(slide, p + K.V(-22f, 0f, -20f), Quaternion.identity, Vector3.one * 1.3f);
            }
            {
                // the shopping centre: a big beige box with a dark band, a glass entrance and a pylon, car park lights
                Vector3 p = K.V(118f, Y(55f), 55f);
                float d = p.magnitude;
                m.Surface = ComicSurface.Plaster;
                m.Box(p + K.V(0, 4.5f, 0), K.V(40f, 13f, 60f), Air(MallRender, d));
                m.Box(p + K.V(-.2f, 9.6f, 0), K.V(40.4f, 1.6f, 60.4f), Air(MallBand, d));
                m.Surface = ComicSurface.Glass;
                m.Box(p + K.V(-20f, 6f, -4f), K.V(3f, 16f, 12f), Air(AtriumGlass, d));
                var pylon = new ComicMesh();
                Pylon(pylon, Vector3.zero);
                m.Add(pylon, p + K.V(-34f, -2f, -24f), Quaternion.Euler(0, -90f, 0), Vector3.one * 1.4f);
            }
            // ---- layer 2/3: tree bands everywhere between, the Darling Range low and blue on the left (east), the bush
            // of the Beeliar wetlands on the right (west)
            TreeBand(m, -78f, -8f, 150f, 30f, g, 1002, Leaf, 11f);
            TreeBand(m, 8f, 78f, 145f, 35f, g, 1003, Leaf, 11f);
            TreeBand(m, 15f, 82f, 205f, 25f, g, 1004, K.Hex("4F6E45"), 11f);
            RangeBand(m, -88f, -14f, 240f, g, 1005);
            return m;
        }

        static void d_FarCar(ComicMesh m, Vector3 p, float dist)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p, K.V(4.2f, 1.2f, 1.8f), Air(CarColours[Mathf.Abs((int)p.x) % CarColours.Length], dist));
        }

        static ComicMesh AubinFar()
        {
            float g = -AubinDrop / 40f;
            var flat = new ComicMesh();
            for (float z = -120f; z < 360f; z += 40f)
            {
                // right (west): the verge, then Thomsons Lake out to the far bank; left (east): verge, noise wall line, estates
                FlatQ(flat, 46f, 64f, z, z + 40f, -.4f, Color.Lerp(Lawn, DryGrass, .4f));
                FlatQ(flat, 64f, 250f, z, z + 40f, -1.2f, Water);
                FlatQ(flat, 250f, 340f, z, z + 40f, -.6f, K.Hex("8E9A62"));
                FlatQ(flat, -46f, -340f, z, z + 40f, -.4f, K.Hex("B9B08A"));
                var r = P.Rng((int)z + 500);
                for (int i = 0; i < 8; i++)
                {
                    float x = P.F(r, 70f, 240f), zz = z + P.F(r, 1f, 38f);
                    FlatQ(flat, x, x + P.F(r, 6f, 20f), zz, zz + .5f, -1.15f, i % 3 == 0 ? White : Ripple);
                }
                FlatQ(flat, 63f, 68f, z, z + 40f, -1.1f, Reed);
            }
            flat.Shear(g);
            var m = new ComicMesh();
            m.Add(flat, Vector3.zero, Quaternion.identity, Vector3.one);
            float Y(float z) => -g * z;
            var rng = P.Rng(2001);
            // ---- left: the far side of the freeway: noise wall, then the roofs of the estates over it, rising in rows
            for (float z = -60f; z < 300f; z += 8f)
            {
                float d = new Vector2(64f, z).magnitude;
                m.Surface = ComicSurface.Concrete;
                m.Box(K.V(-64f, Y(z + 4f) + 1.8f, z + 4f), K.V(.6f, 5f, 7.9f), Air((int)(z / 8f) % 2 == 0 ? NoiseWall : Color.Lerp(NoiseWall, K.R.cream, .3f), d));
            }
            Color[] roofs = { RoofMonument, RoofBasalt, RoofWindspray, RoofTile, K.Hex("B5734F") };
            for (int row = 0; row < 5; row++)
                for (float z = -20f + row * 6f; z < 230f - row * 25f; z += 15f + P.F(rng, 0f, 4f))
                {
                    float x = -(76f + row * 17f + P.F(rng, 0f, 4f));
                    Vector3 p = K.V(x, Y(z) + row * .6f, z);
                    float d = p.magnitude;
                    HipHouse(m, p, 12f, 12f, 3.2f, Air(HouseWalls[rng.Next(HouseWalls.Length)], d), Air(roofs[rng.Next(roofs.Length)], d));
                }
            // the Rowley Road overpass ahead-left over the freeway
            {
                float z = 190f, y = Y(z);
                m.Surface = ComicSurface.Concrete;
                m.Box(K.V(-105f, y + 7.4f, z), K.V(110f, 1.4f, 11f), Air(Concrete, 210f));
                m.Box(K.V(-105f, y + 8.9f, z - 5.5f), K.V(110f, 1.6f, .3f), Air(Color.Lerp(BridgeClad, BridgeGlass, .35f), 210f));
                foreach (float px in new[] { -62f, -84f, -110f, -140f }) m.Box(K.V(px, y + 3.4f, z), K.V(2f, 8f, 7f), Air(Concrete, 210f));
                for (float x = -150f; x < -60f; x += 22f) d_FarCar(m, K.V(x, y + 8.6f, z - 2f), 210f);
            }
            // ---- right: paperbarks along the lake edge and on islands of reeds, the far bank's bush
            for (float z = -40f; z < 260f; z += 9f)
            {
                var tree = new ComicMesh();
                PaperbarkTree(tree, Vector3.zero, P.F(rng, 7f, 11f), 2100 + (int)z, true);
                float x = P.F(rng, 64f, 74f);
                m.Add(tree, K.V(x, Y(z) - 1f, z), Quaternion.Euler(0, P.F(rng, 0f, 360f), 0), Vector3.one * 1.2f);
            }
            for (int i = 0; i < 10; i++)
            {
                float x = P.F(rng, 90f, 200f), z = P.F(rng, 20f, 200f);
                if (new Vector2(x, z).magnitude > 260f) continue;
                m.Surface = ComicSurface.Foliage;
                m.Ellipsoid(K.V(x, Y(z) - 1.2f, z), K.V(P.F(rng, 14f, 26f), 2.4f, P.F(rng, 10f, 18f)), Air(Reed, new Vector2(x, z).magnitude));
                var tree = new ComicMesh();
                PaperbarkTree(tree, Vector3.zero, P.F(rng, 7f, 10f), 2200 + i, true);
                m.Add(tree, K.V(x, Y(z) - 1.2f, z), Quaternion.identity, Vector3.one * 1.4f);
            }
            // a few black swans on the lake
            for (int i = 0; i < 5; i++)
            {
                float x = P.F(rng, 80f, 140f), z = P.F(rng, 30f, 130f);
                m.Surface = ComicSurface.Fabric;
                m.Box(K.V(x, Y(z) - .7f, z), K.V(1.4f, .8f, 2.4f), K.R.ink);
                m.Box(K.V(x, Y(z) + .3f, z - 1f), K.V(.3f, 1.6f, .3f), K.R.ink);
                m.Box(K.V(x, Y(z) + 1f, z - 1.3f), K.V(.25f, .25f, .6f), SlideRed);
            }
            // ---- tree bands and the Darling Range low and blue far to the east
            TreeBand(m, -80f, -12f, 175f, 30f, g, 2003, Leaf, 11f);
            TreeBand(m, 20f, 84f, 225f, 25f, g, 2004, K.Hex("4F6E45"), 11f);
            TreeBand(m, 8f, 24f, 200f, 30f, g, 2005, Leaf, 12f);
            RangeBand(m, -88f, -14f, 240f, g, 2006);
            // Aubin Grove's own roofs ahead-right, beyond the lake's south end
            for (int i = 0; i < 14; i++)
            {
                float x = P.F(rng, 60f, 110f), z = P.F(rng, 200f, 260f);
                Vector3 p = K.V(x, Y(z), z);
                HipHouse(m, p, 12f, 12f, 3.2f, Air(HouseWalls[rng.Next(HouseWalls.Length)], p.magnitude), Air(roofs[rng.Next(roofs.Length)], p.magnitude));
            }
            return m;
        }
    }
}
