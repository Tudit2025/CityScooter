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

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// ELIZABETH QUAY district (v0.3, route order 60): a light paved waterfront promenade (same cross-section as the
    /// street) with the river on the right, the white double-arched pedestrian bridge (the road goes up and over its hump:
    /// a Ramp platform), a timber jetty ramp in front of a road-wide water gap (RoadGap, SPLASH), the Bell Tower (glass
    /// spire, copper sails), Spanda (nested white rings), the island playground and kiosk, ferries, palms and white
    /// umbrellas, the CBD towers behind. Landmarks are decoration only (no colliders).
    ///   Menu: CityScooter/Districts/Build Elizabeth Quay
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.QuayBuilder.BuildBatch
    /// </summary>
    public static class QuayBuilder
    {
        public const string Id = "quay";
        public const string PrefabFolder = ComicCityBuilder.Prefabs + "/Districts/Quay";
        public const string MeshFolder = ComicCityBuilder.Root + "/Districts/Quay";
        public const string PatternFolder = CbdBuilder.DataFolder + "/quay";
        public const string DistrictPath = CbdBuilder.DataFolder + "/District_Quay.asset";
        public static readonly string[] ChunkIds = { "Promenade", "BellTower", "Bridge", "Spanda", "Inlet" };
        /// <summary>v0.6: the district (360-480 m) is longer than its five chunks, so the cycle used to start again and the
        /// rider passed a second Bell Tower, a second arched bridge and a second Spanda. The cycle is now ten chunks: the
        /// same gameplay order twice (plain, plain, the hump, plain, the jetty jump) with other real places the second
        /// time: the ferry jetty, the short bridge to the island's moorings, The Landing with the carousel and The Towers.
        /// The plain promenade and the inlet are the same prefabs both times.</summary>
        public static readonly string[] Cycle = { "Promenade", "BellTower", "Bridge", "Spanda", "Inlet", "Promenade", "Jetty", "IslandBridge", "Landing", "Inlet" };
        public static string ChunkPath(string variant) => PrefabFolder + "/CHN_Quay_" + variant + ".prefab";
        [MenuItem("CityScooter/Districts/Build Perth CBD and Elizabeth Quay")]
        public static void BuildBoth() { CbdBuilder.Build(); Build(); }
        static string Pf(string name) => PrefabFolder + "/" + name + ".prefab";

        /// <summary>Riverside: flatter than the street (-1.6 m per 40 m instead of -3.15).</summary>
        public const float Drop = -1.6f;
        // The same river as Mounts Bay Rd (v0.6: one riverfront): its level, its colours, its far shore.
        const float WaterLevel = MountsBayBuilder.WaterY;
        // How far out the river plane goes (the panorama sky shows the far shore beyond it).
        const float RiverFar = 260f;

        // The bridge hump (Ramp platform) and the jetty jump over the inlet (Ramp + RoadGap). Numbers for gameplay:
        public const float BridgeStart = 6f, BridgeRise = 10f, BridgeHeight = 2.2f, BridgeTop = 8f, BridgeDown = 8f;
        public const float JettyStart = 6f, JettyRise = 6f, JettyHeight = 1.4f, JettyTop = 3f, GapLength = 6f;

        // Elizabeth Quay palette (sRGB; sources in art-src/districts/quay/SOURCES.txt).
        static readonly Color Beige = K.Hex("D8C7A6"), BeigeLight = K.Hex("E3D6BC"), Band = K.Hex("8C877E"), Limestone = K.Hex("ECE6D8");
        static readonly Color Water = ParkRiverKit.Water, Ripple = ParkRiverKit.WaterLight, BridgeWhite = K.Hex("F4F4F0"), Deck = K.Hex("8A5A3C"),
            RailGrey = K.Hex("9AA0A6"), Copper = K.Hex("B8733D"), CopperDark = K.Hex("8E5229"), SpireGlass = K.Hex("8FB8C8"),
            FerryGreen = K.Hex("1F7A5C"), Lawn = K.Hex("6E9A45"), RoofRed = K.Hex("C4532F"), Timber = K.Hex("A8774E");
        static readonly Color GlassGrey = K.Hex("5F7384"), GlassBlue = K.Hex("2E5E9E"), GlassPale = K.Hex("7FA8D8"), FrameLight = K.Hex("DDE3EA");

        [MenuItem("CityScooter/Districts/Build Elizabeth Quay")]
        public static void Build()
        {
            K.Begin(MeshFolder, PrefabFolder, CbdBuilder.DataFolder, PatternFolder);

            // ---- obstacles, animals, ramps
            Obstacle bollard = K.SaveObstacle(Pf("OBS_Quay_Bollard"), "OBS_Quay_Bollard", BollardMesh(), new Vector3(.5f, 1f, .5f), ObstacleKind.Thing, true, false, 1.2f);
            Obstacle truck = K.SaveObstacle(Pf("OBS_Quay_FoodTruck"), "OBS_Quay_FoodTruck", FoodTruckMesh(), new Vector3(2.1f, 3f, 4.6f), ObstacleKind.Thing, false, false, .6f);
            Obstacle umbrella = K.SaveObstacle(Pf("OBS_Quay_UmbrellaTable"), "OBS_Quay_UmbrellaTable", UmbrellaTableMesh(), new Vector3(1.9f, 2.6f, 1.2f), ObstacleKind.Thing, false, false, .6f);
            GameObject gull = SaveSeagull(Pf("ANM_Quay_Seagull"), 0f);
            GameObject gullWalking = SaveSeagull(Pf("ANM_Quay_SeagullWalking"), .6f);
            Ramp jetty = K.SaveRamp(Pf("RMP_Quay_Jetty"), "RMP_Quay_Jetty", 3f, 5f, 1.3f, 2f, 1f, .5f, Deck, Color.Lerp(Deck, K.R.ink, .3f), false);

            // ---- chunks
            K.WriteChunk(Entry(), ChunkPath("Entry"));
            K.WriteChunk(Promenade(), ChunkPath("Promenade"));
            K.WriteChunk(BellTower(true), ChunkPath("BellTower"));
            K.WriteChunk(Bridge(true), ChunkPath("Bridge"));
            K.WriteChunk(Spanda(true), ChunkPath("Spanda"));
            K.WriteChunk(BellTower(false), ChunkPath("Jetty"));
            K.WriteChunk(Bridge(false), ChunkPath("IslandBridge"));
            K.WriteChunk(Spanda(false), ChunkPath("Landing"));
            K.WriteChunk(Inlet(), ChunkPath("Inlet"));
            K.WriteChunk(Exit(), ChunkPath("Exit"));
            // Far scenery: the river and the South Perth shore, the SAME prefab as Mounts Bay Rd (neighbours that share
            // it keep it standing across their boundary). The CBD towers behind the quay are in the chunks (SkylineRow).
            GameObject far = AssetDatabase.LoadAssetAtPath<GameObject>(MountsBayBuilder.FarShorePath);
            if (far == null) far = K.SaveFarScenery(Pf("FAR_Quay_Skyline"), "FAR_Quay_Skyline", FarSkyline()); // Mounts Bay not built yet
            else
            {
                AssetDatabase.DeleteAsset(Pf("FAR_Quay_Skyline"));
                AssetDatabase.DeleteAsset(MeshFolder + "/FAR_Quay_Skyline.asset");
            }

            // ---- promenade patterns (no cars here: seagulls, bollards, food trucks)
            var patterns = new List<HazardPattern>
            {
                K.Pattern(PatternFolder, "HZ_Quay_Seagulls", 45, 0, 1f, true, 30, (gull, -2.4f, 22f, 0f), (gull, -.6f, 23.5f, 30f), (gull, -1.6f, 26f, -40f), (gull, .4f, 27f, 150f)),
                K.Pattern(PatternFolder, "HZ_Quay_Bollards", 40, 1, .8f, false, 30, (bollard.gameObject, -3.2f, 20f, 0f), (bollard.gameObject, -1.6f, 20f, 0f), (bollard.gameObject, 0f, 20f, 0f), (bollard.gameObject, 1.6f, 20f, 0f), (bollard.gameObject, 3.2f, 20f, 0f)),
                K.Pattern(PatternFolder, "HZ_Quay_FoodTrucks", 75, 2, .7f, true, 40, (truck.gameObject, -2.5f, 20f, 0f), (gull, 1.8f, 34f, 0f), (truck.gameObject, 2.5f, 55f, 180f)),
            };

            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme == null) throw new InvalidOperationException("No comic theme at " + ComicCityBuilder.ThemePath);
            District d = K.LoadOrCreate<District>(DistrictPath);
            d.id = Id; d.displayName = "ELIZABETH QUAY"; d.order = 60; d.length = new Vector2(360f, 480f);
            d.chunks = Cycle.Select(c => AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(c)).GetComponent<RoadChunk>()).ToArray();
            d.orderedChunks = true;
            d.entry = AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath("Entry")).GetComponent<RoadChunk>();
            d.exit = AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath("Exit")).GetComponent<RoadChunk>();
            d.allowBoulevard = false;
            var obstacles = new List<Obstacle> { bollard, truck, umbrella };
            if (theme.obstacles != null) obstacles.AddRange(theme.obstacles.Where(o => o != null && (o.name == "OBS_Bin" || o.name == "OBS_Crates")));
            d.obstacles = obstacles.ToArray();
            d.jumpObstacles = Array.Empty<Obstacle>();
            d.gifts = Array.Empty<Gift>();
            // Police and sidewalk people from the street still turn up; no car traffic or manholes on the promenade.
            var hazards = new List<HazardPattern>();
            if (theme.hazards != null)
                hazards.AddRange(theme.hazards.Where(h => h != null && (h.name == "HZ_PoliceStop" || h.name == "HZ_SidewalkStroll" || h.name == "HZ_RoadworksShort")));
            hazards.AddRange(patterns);
            d.hazards = hazards.ToArray();
            var people = new List<GameObject>(K.People(theme, "Tourist", "Tourist", "Tourist", "Surfer", "Jogger", "Jogger", "DogWalker", "Kid", "Kid", "Elder", "Student", "Skater", "Shopper"));
            people.Add(gullWalking); people.Add(gullWalking); people.Add(gull);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { jetty };
            d.rampFrequency = 1.5f;
            d.blendMeters = 150f;
            // Bright, open and blue.
            d.fogColor = new Color(.70f, .84f, .94f);
            d.skyTint = new Color(.97f, 1f, 1f);
            d.ambientSky = new Color(.68f, .84f, .98f);
            d.ambientEquator = new Color(.86f, .88f, .86f);
            d.ambientGround = new Color(.62f, .60f, .54f);
            d.sunColor = new Color(1f, .97f, .88f);
            d.sunIntensity = 1.45f;
            d.farScenery = far;
            // gateway (DistrictSetup's GW_ sign), ambience and the sky fields (skyYaw, skySize, skyHeight) belong to the
            // district system: never written here, existing values are kept.
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            K.WriteReport("art-src/districts/quay/catalog.txt");
            RenderAtlas(bollard, truck, umbrella, gull, jetty);
            Debug.Log("[Districts/Quay] built " + (Cycle.Distinct().Count() + 2) + " chunks: " + string.Join("; ", K.Report));
        }

        static void RenderAtlas(Obstacle bollard, Obstacle truck, Obstacle umbrella, GameObject gull, Ramp jetty)
        {
            ComicMesh Make(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }
            var bell = new CqChunkDraft("Atlas", 0f);
            BellTowerLandmark(bell, Vector3.zero);
            var spanda = new CqChunkDraft("Atlas", 0f);
            SpandaLandmark(spanda, Vector3.zero);
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("Palm", null, Make(m => K.Palm(m, null, Vector3.zero, 7f, 1))),
                ("PalmShort", null, Make(m => K.Palm(m, null, Vector3.zero, 5.5f, 4))),
                ("PlaneTree", null, Make(m => K.PlaneTree(m, null, Vector3.zero, 1f))),
                ("UmbrellaTable", umbrella.gameObject, null),
                ("Bollard", bollard.gameObject, null),
                ("FoodTruck", truck.gameObject, null),
                ("Seagull", gull, null),
                ("Ferry", null, Make(m => Ferry(m, Vector3.zero, 0f))),
                ("IslandKiosk", null, Make(m => IslandKiosk(m, Vector3.zero))),
                ("Playground", null, Make(m => Playground(m, Vector3.zero))),
                ("Railing", null, Make(m => Railing(m, 1, 0f, 6f, 0f))),
                ("JettyRamp", jetty.gameObject, null),
                ("BellTower", null, bell.Landmark),
                ("Spanda", null, spanda.Landmark),
            };
            K.RenderAtlas("art-src/districts/quay/props-atlas.png", items);
        }

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Both art-agent-3 districts in one Unity run: -executeMethod CityScooter.EditorTools.Districts.QuayBuilder.BuildCbdAndQuayBatch</summary>
        public static void BuildCbdAndQuayBatch()
        {
            try { CbdBuilder.Build(); Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        // ================================================================ chunks

        // Where the street's asphalt and slab footpaths hand over to the promenade's paving (or back): a cream kerb-stone
        // band and a dark granite band across the whole width, and piano-key bars on the road like a raised crossing.
        static void PavingBand(CqChunkDraft d, float z, bool pavingAfter)
        {
            ComicMesh m = d.Road;
            float k0 = pavingAfter ? z - 2.6f : z + .9f;
            for (int x = -3; x <= 3; x++) K.Flat(m, x - .3f, x + .3f, x - .3f, x + .3f, k0, k0 + 1.7f, .036f, K.R.cream);
            K.Flat(m, -K.Half, K.Half, -K.Half, K.Half, z - .5f, z, .037f, Limestone);
            K.Flat(m, -K.Half, K.Half, -K.Half, K.Half, z, z + .5f, .037f, Band);
            for (int s = -1; s <= 1; s += 2)
            {
                K.Flat(m, s * (K.Half + .24f), s * K.Edge, s * (K.Half + .24f), s * K.Edge, z - .5f, z, K.Kerb + .03f, Limestone);
                K.Flat(m, s * (K.Half + .24f), s * K.Edge, s * (K.Half + .24f), s * K.Edge, z, z + .5f, K.Kerb + .03f, Band);
            }
        }

        // A tall white light mast of the promenade (decoration beyond the rideable edge).
        static void LightMast(ComicMesh m, Vector3 p, int s)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + K.V(0, .12f, 0), .26f, .24f, K.R.ink, 8);
            m.Cylinder(p + K.V(0, 4.6f, 0), .11f, 9f, BridgeWhite, 8, .6f);
            m.Beam(p + K.V(0, 8.9f, 0), p + K.V(-s * 1.5f, 9.3f, 0), .1f, BridgeWhite);
            m.Box(p + K.V(-s * 1.5f, 9.22f, 0), K.V(.7f, .14f, .34f), K.R.cream);
            m.Box(p + K.V(-s * 1.5f, 9.32f, 0), K.V(.76f, .08f, .4f), K.R.ink);
        }

        // The third row behind the quay: the tallest CBD towers (no outline, no shadows), so the city stands right behind
        // the plaza in every chunk (they used to be the district's far scenery).
        static void SkylineRow(CqChunkDraft d, float z0, float z1, int seed)
        {
            ComicMesh m = d.Deep;
            Color[] glass = { GlassBlue, GlassGrey, GlassPale, K.Hex("2F6B66"), K.Hex("9AAFC4"), K.Hex("E6E1D3"), K.Hex("B08A56") };
            var rng = new System.Random(6060 + seed * 31);
            for (float z = z0 + 2f; z < z1 - 6f; z += 19f)
            {
                float x = -(K.Edge + 68f + (float)rng.NextDouble() * 30f), h = 70f + (float)rng.NextDouble() * 80f, w = 15f + (float)rng.NextDouble() * 7f;
                Color c = Color.Lerp(glass[rng.Next(glass.Length)], new Color(.62f, .76f, .88f), .3f);
                float y = d.Ground(z) - 3f;
                m.Surface = ComicSurface.Glass;
                m.Box(new Vector3(x, y + h * .5f, z + 7f), new Vector3(w, h, w), c);
                for (float by = 8f; by < h - 3f; by += 8f) m.Box(new Vector3(x, y + by, z + 7f), new Vector3(w + .1f, .6f, w + .1f), Color.Lerp(c, K.R.ink, .3f));
                m.Box(new Vector3(x, y + h + 1.5f, z + 7f), new Vector3(w * .55f, 3f, w * .55f), Color.Lerp(c, K.R.ink, .35f));
            }
        }

        static CqChunkDraft Entry()
        {
            // After the CBD's exit (a city street): the street runs on for 12 m between two low buildings, then the
            // paving changes at a kerb-stone band, the plaza opens on the left and the river on the right.
            var d = new CqChunkDraft("Quay_Entry", Drop);
            const float street = 12f;
            Promenade(d, 0f, street);
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, K.R.pavement, K.R.cream, true, street, d.Length);
            PavingBand(d, street, true);
            // left: the esplanade pavilion on the street's building line, then the plaza
            d.SlopedFrontage.Surface = ComicSurface.Paving;
            K.SideBox(d.SlopedFrontage, -1, 0, street, -2.4f, K.Kerb - .02f, 0f, 60f, BeigeLight);
            Pavilion(d, new CqLot(-1, 0, street - .4f, K.Edge + .3f, 12, d.Base(6)), K.Hex("E9E2D2"));
            Plaza(d, street, 40, true);
            Pavilion(d, new CqLot(-1, 24, 40, K.Edge + 10, 12, d.Base(32)), K.Hex("F2EEE4"));
            // right: the ferry office on a paved apron, the quay wall, then the open river
            K.SideBox(d.SlopedFrontage, 1, 0, street, WaterLevel - 1f, K.Kerb - .02f, 0f, 11f, BeigeLight);
            Pavilion(d, new CqLot(1, 0, 8.5f, K.Edge + .3f, 9, d.Base(4)), K.Hex("F2EEE4"));
            K.Water(d.Road, K.Edge + 11f, RiverFar, 0, street, WaterLevel, Water, Ripple, 11);
            River(d, street, 40);
            for (int s = -1; s <= 1; s += 2)
                foreach (float z in new[] { 3f, 9.5f }) K.PlaneTree(d.Props, d.Collisions, new Vector3(s * 5.6f, d.Ground(z), z), .9f);
            LightMast(d.Props, new Vector3(-(K.Edge + 1.2f), d.Ground(18f), 18f), -1);
            CbdBuilder.DeepTowers(d, -1, 0, 40, 21);
            SkylineRow(d, 0, 40, 1);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft Exit()
        {
            // Before Mounts Bay Rd's entry (one riverfront): the plaza ends at a quay wall with First Contact on its
            // corner, and from there the path runs on with the river on BOTH sides (the mouth of the inlet on the right,
            // the open river on the left); the paving hands back to the street's asphalt at a kerb-stone band.
            var d = new CqChunkDraft("Quay_Exit", Drop);
            const float street = 26f, quayEnd = 20f;
            Promenade(d, street, d.Length);
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, K.R.pavement, K.R.cream, true, 0f, street);
            PavingBand(d, street, false);
            River(d, 0, 40);
            Plaza(d, 0, quayEnd, true);
            Pavilion(d, new CqLot(-1, 0, 12, K.Edge + 10, 12, d.Base(6)), K.Hex("E9E2D2"));
            CbdBuilder.DeepTowers(d, -1, 0, quayEnd, 22);
            SkylineRow(d, 0, quayEnd, 7);
            FirstContact(d, new Vector3(-(K.Edge + 4.5f), d.Ground(quayEnd - 4f), quayEnd - 4f));
            River(d, quayEnd, 40, true, -1);
            LightMast(d.Props, new Vector3(-(K.Edge + 1.2f), d.Ground(8f), 8f), -1);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft Promenade()
        {
            var d = new CqChunkDraft("Quay_Promenade", Drop);
            Promenade(d);
            River(d, 0, 40);
            Plaza(d, 0, 40, false);
            Pavilion(d, new CqLot(-1, 4, 20, K.Edge + 10, 12, d.Base(12)), K.Hex("F2EEE4"));
            CbdBuilder.GlassTower(d, new CqLot(-1, 22, 38, K.Edge + 12, 16, d.Base(30)), 96, GlassGrey, FrameLight, 0);
            CbdBuilder.DeepTowers(d, -1, 0, 40, 23);
            SkylineRow(d, 0, 40, 2);
            Ferry(d.Props, new Vector3(24f, d.Y(24f) + WaterLevel, 24f), 12f);
            d.StandardSlots();
            return d;
        }

        // tower: the Bell Tower chunk. Without it (second pass): Elizabeth Quay Jetty, the ferry terminal: a longer pier
        // under a white canopy, two ferries, the jetty's name on a board.
        static CqChunkDraft BellTower(bool tower)
        {
            var d = new CqChunkDraft(tower ? "Quay_BellTower" : "Quay_Jetty", Drop);
            Promenade(d);
            River(d, 0, 40);
            // the ferry jetty: a timber pier out from the quay with a ferry alongside
            ComicMesh m = d.SlopedFrontage;
            float pier = tower ? 9f : 16f, pz0 = tower ? 4f : 14f, pz1 = pz0 + 4f;
            m.Surface = ComicSurface.Wood;
            K.SideBox(m, 1, pz0, pz1, K.Kerb - .25f, K.Kerb, .2f, pier, Deck);
            for (float x = 1f; x < pier; x += 2.6f)
                foreach (float z in new[] { pz0 + .3f, pz1 - .3f }) K.SideBox(m, 1, z - .15f, z + .15f, WaterLevel - .5f, K.Kerb - .25f, x - .15f, x + .15f, Color.Lerp(Deck, K.R.ink, .4f));
            if (tower)
            {
                Ferry(d.Props, new Vector3(K.Edge + 11.2f, d.Y(16f) + WaterLevel, 16f), 0f);
                BellTowerLandmark(d, new Vector3(34f, d.Y(26f), 26f));
            }
            else
            {
                Ferry(d.Props, new Vector3(K.Edge + 6f, d.Y(26f) + WaterLevel, 26f), 0f);
                Ferry(d.Props, new Vector3(K.Edge + 13f, d.Y(5f) + WaterLevel, 5f), 0f);
                // the canopy over the pier and the name board at its head
                ComicMesh f = d.Frontage;
                f.Surface = ComicSurface.PaintedMetal;
                float y = d.Ground(pz0 + 2f);
                for (float x = 2f; x < pier; x += 4.4f)
                    foreach (float z in new[] { pz0 + .4f, pz1 - .4f }) f.Cylinder(K.V(K.Edge + x, y + 1.6f, z), .08f, 3.2f, BridgeWhite, 8);
                f.Box(K.V(K.Edge + pier * .5f + .4f, y + 3.3f, pz0 + 2f), K.V(pier, .16f, 4.8f), BridgeWhite, Quaternion.Euler(0, 0, -3f));
                f.Box(K.V(K.Edge + pier * .5f + .4f, y + 3.18f, pz0 + 2f), K.V(pier + .1f, .1f, 4.9f), FerryGreen, Quaternion.Euler(0, 0, -3f));
                foreach (float x in new[] { 1.2f, 6.6f }) f.Cylinder(K.V(K.Edge + x, d.Ground(pz0 - 1.5f) + .9f, pz0 - 1.5f), .07f, 5.8f, K.R.ink, 8);
                TrunkPixelText.Board(f, "QUAY JETTY", K.V(K.Edge + 3.9f, d.Ground(pz0 - 1.5f) + 3.5f, pz0 - 1.5f), Vector3.right, .11f, FerryGreen, Color.white, Color.white);
            }
            Plaza(d, 0, 40, false);
            if (tower)
            {
                Pavilion(d, new CqLot(-1, 0, 14, K.Edge + 10, 12, d.Base(7)), K.Hex("E9E2D2"));
                CbdBuilder.GlassTower(d, new CqLot(-1, 16, 34, K.Edge + 12, 18, d.Base(25)), 132, GlassBlue, FrameLight, 1);
            }
            else
            {
                CbdBuilder.GlassTower(d, new CqLot(-1, 2, 18, K.Edge + 12, 16, d.Base(10)), 74, K.Hex("2F6B66"), FrameLight, 2);
                Pavilion(d, new CqLot(-1, 22, 38, K.Edge + 10, 12, d.Base(30)), K.Hex("F2EEE4"));
            }
            CbdBuilder.DeepTowers(d, -1, 0, 40, tower ? 24 : 34);
            SkylineRow(d, 0, 40, tower ? 3 : 9);
            d.StandardSlots();
            return d;
        }

        // A carousel on the plaza (The Landing): striped tent roof, a ring of poles with horses.
        static void Carousel(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + K.V(0, .2f, 0), 4.2f, .4f, K.R.ink, 12);
            m.Cylinder(p + K.V(0, 2.2f, 0), .5f, 4f, K.R.cream, 8);
            m.Cylinder(p + K.V(0, 4.05f, 0), 4.5f, .5f, K.R.red, 12);
            m.Cylinder(p + K.V(0, 4.36f, 0), 4.56f, .14f, K.R.cream, 12);
            m.Surface = ComicSurface.Fabric;
            m.Cylinder(p + K.V(0, 5.45f, 0), 4.4f, 2.1f, K.R.cream, 12, .06f);
            for (int i = 0; i < 6; i++)
            {
                Quaternion q = Quaternion.Euler(0, i * 60f, 0);
                m.Surface = ComicSurface.Fabric;
                m.Box(p + K.V(0, 5.3f, 0) + q * K.V(0, 0, 2.2f), K.V(1.15f, 1.9f, .1f), K.R.red, q * Quaternion.Euler(-62f, 0, 0));
                m.Surface = ComicSurface.Metal;
                m.Cylinder(p + q * K.V(0, 2.2f, 3.2f), .05f, 3.6f, K.Hex("D9B45A"), 6);
                m.Surface = ComicSurface.Plastic;
                m.Box(p + q * K.V(0, 1.5f + (i % 2) * .4f, 3.2f), K.V(1.1f, .5f, .34f), i % 2 == 0 ? Color.white : K.Hex("8A5A3C"), q);
                m.Box(p + q * K.V(.5f, 1.95f + (i % 2) * .4f, 3.2f), K.V(.3f, .5f, .26f), i % 2 == 0 ? Color.white : K.Hex("8A5A3C"), q);
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + K.V(0, 6.7f, 0), .12f, .5f, K.R.yellow, 8);
        }

        // sculpture: the Spanda chunk (with the island across the water). Without it (second pass): The Landing with the
        // carousel, and The Towers behind it (two bronze-glass towers, the taller with a stepped crown).
        static CqChunkDraft Spanda(bool sculpture)
        {
            var d = new CqChunkDraft(sculpture ? "Quay_Spanda" : "Quay_Landing", Drop);
            Promenade(d);
            River(d, 0, 40);
            Plaza(d, 0, 40, false);
            if (sculpture)
            {
                Island(d);
                SpandaLandmark(d, new Vector3(-(K.Edge + 11f), d.Ground(22f), 22f));
                Pavilion(d, new CqLot(-1, 30, 40, K.Edge + 10, 12, d.Base(35)), K.Hex("F2EEE4"));
                CbdBuilder.GlassTower(d, new CqLot(-1, 2, 16, K.Edge + 14, 16, d.Base(9)), 84, GlassPale, FrameLight, 3);
            }
            else
            {
                Carousel(d.Landmark, new Vector3(-(K.Edge + 12f), d.Ground(9f), 9f));
                CbdBuilder.GlassTower(d, new CqLot(-1, 18, 30, K.Edge + 16, 14, d.Base(24)), 96, K.Hex("B08A56"), K.Hex("E9DCC2"), 2);
                CbdBuilder.GlassTower(d, new CqLot(-1, 31, 40, K.Edge + 16, 12, d.Base(35.5f)), 62, K.Hex("C29A68"), K.Hex("E9DCC2"), 0);
                Ferry(d.Props, new Vector3(30f, d.Y(22f) + WaterLevel, 22f), 168f);
                for (int i = 0; i < 3; i++) Dinghy(d.Props, new Vector3(K.Edge + 3.2f, d.Y(7f + i * 11f) + WaterLevel, 7f + i * 11f), i);
            }
            CbdBuilder.DeepTowers(d, -1, 0, 40, sculpture ? 25 : 35);
            SkylineRow(d, 0, 40, sculpture ? 4 : 10);
            d.StandardSlots();
            return d;
        }

        // A small moored boat (the quay's public moorings).
        static void Dinghy(ComicMesh m, Vector3 p, int i)
        {
            Color band = new[] { K.R.red, K.Hex("2E5E9E"), FerryGreen }[i % 3];
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + K.V(0, .35f, 0), K.V(1.9f, .7f, 5.2f), BridgeWhite);
            m.Box(p + K.V(0, .62f, 0), K.V(1.96f, .14f, 5.26f), band);
            m.Box(p + K.V(0, 1.1f, -.6f), K.V(1.4f, .8f, 2f), BridgeWhite);
            m.Surface = ComicSurface.Glass;
            m.Box(p + K.V(0, 1.15f, -.6f), K.V(1.44f, .4f, 1.7f), K.Hex("26333D"));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + K.V(0, 1.54f, -.6f), K.V(1.5f, .08f, 2.1f), band);
        }

        // arches: the Elizabeth Quay Bridge. Without them (second pass): the short bridge to the islet, the same hump with
        // plain white rails, boats on the moorings beside it.
        static CqChunkDraft Bridge(bool arches)
        {
            // The road goes up and over the white double-arched bridge: a Ramp platform (rise + crown); off the crown the
            // rider flies over the descending half. Water on both sides and under the deck; no ground beside the deck.
            var d = new CqChunkDraft(arches ? "Quay_Bridge" : "Quay_IslandBridge", Drop);
            float z0 = BridgeStart, z1 = z0 + BridgeRise, z2 = z1 + BridgeTop, z3 = z2 + BridgeDown;
            Promenade(d, z0, z3);
            K.Water(d.Road, -60f, RiverFar, z0, z3, WaterLevel, Water, Ripple, 31);
            // the promenade ends at quay walls either side of the inlet under the bridge; the river is on the right
            QuayWall(d.Road, -60f, K.Edge, z0, true);
            QuayWall(d.Road, -60f, K.Edge, z3, false);
            River(d, 0, z0);
            River(d, z3, 40);
            LeftPromenadeLand(d, 0, z0);
            LeftPromenadeLand(d, z3, 40);
            d.Ramps.Add(new CqRampSpec { Name = "BridgeHump", X = 0f, Z = z0, Width = 2f * K.Edge, Rise = BridgeRise, Height = BridgeHeight, Top = BridgeTop, Launch = 1f, Hang = .5f });
            // The deck coming back down (system B): a reversed ramp, pivot at the foot (z3), rising back towards the crown,
            // so slow riders land on it and ride down instead of sinking into the deck; fast ones fly over it.
            d.Ramps.Add(new CqRampSpec { Name = "BridgeDown", X = 0f, Z = z3, Width = 2f * K.Edge, Rise = BridgeDown, Height = BridgeHeight, Top = 0f, Launch = 1f, Hang = .5f, Yaw = 180f });
            BridgeDeck(d, z0, z1, z2, z3, arches);
            if (!arches)
                for (int i = 0; i < 4; i++) Dinghy(d.Props, new Vector3(K.Edge + 5f + (i % 2) * 6f, d.Y(9f + i * 6.5f) + WaterLevel, 9f + i * 6.5f), i + 1);
            d.KeepOut.Add(KeepOutBox(d, -1, z0, z3)); d.KeepOut.Add(KeepOutBox(d, 1, z0, z3));
            // No obstacles on the hump or where riders land; a row of gifts after it, a gap in the middle for a lane.
            d.GiftSlots.Add(new Vector2(-2.6f, 37f)); d.GiftSlots.Add(new Vector2(2.6f, 37f));
            CbdBuilder.DeepTowers(d, -1, 0, z0, arches ? 26 : 36);
            CbdBuilder.DeepTowers(d, -1, z3 - 2f, 40, arches ? 26 : 36);
            SkylineRow(d, 0, 40, arches ? 5 : 11);
            return d;
        }

        static CqChunkDraft Inlet()
        {
            // A timber jetty ramp right up to the quay edge, then 6 m of open water across the whole promenade (SPLASH!),
            // then the promenade again. The ramp covers the full rideable width, so the gap can always be cleared.
            var d = new CqChunkDraft("Quay_Inlet", Drop);
            float z0 = JettyStart, lip = z0 + JettyRise + JettyTop, far = lip + GapLength;
            Promenade(d, lip, far);
            K.Water(d.Road, -22f, RiverFar, lip, far, WaterLevel, Water, Ripple, 41);
            QuayWall(d.Road, -60f, K.Edge, lip, true);
            QuayWall(d.Road, -60f, K.Edge, far, false);
            d.Road.Surface = ComicSurface.Concrete;
            K.MinMaxBox(d.Road, K.V(-22.4f, WaterLevel - 1f, lip), K.V(-22f, K.Kerb, far), Limestone);
            d.SlopedFrontage.Surface = ComicSurface.Paving;
            K.SideBox(d.SlopedFrontage, -1, lip, far, WaterLevel - 1f, K.Kerb - .01f, 14.4f, 52f, BeigeLight);
            River(d, 0, lip);
            River(d, far, 40);
            LeftPromenadeLand(d, 0, lip);
            LeftPromenadeLand(d, far, 40);
            var ramp = new CqRampSpec { Name = "JettyRamp", X = 0f, Z = z0, Width = 2f * K.Edge, Rise = JettyRise, Height = JettyHeight, Top = JettyTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(ramp);
            d.Gaps.Add(new CqGapSpec { Name = "InletWater", Z = lip, Length = GapLength, Width = 0f, Word = "SPLASH", Water = true });
            var deck = new ComicMesh();
            K.RampMesh(deck, ramp, Deck, Color.Lerp(Deck, K.R.ink, .3f), true, WaterLevel - .3f);
            // boards and a white rail post at each side so the jetty reads as timber
            Color board = Color.Lerp(Deck, K.R.ink, .25f);
            for (float t = .1f; t < 1f; t += .1f)
            {
                float z = z0 + JettyRise * t, y = JettyHeight * t + .012f;
                K.Face(deck, K.V(-K.Edge, y, z), K.V(-K.Edge, y, z + .05f), K.V(K.Edge, y, z + .05f), K.V(K.Edge, y, z), board, Vector3.up, false);
            }
            for (float z = z0 + JettyRise + .5f; z < lip; z += .8f)
                K.Face(deck, K.V(-K.Edge, JettyHeight + .012f, z), K.V(-K.Edge, JettyHeight + .012f, z + .05f), K.V(K.Edge, JettyHeight + .012f, z + .05f), K.V(K.Edge, JettyHeight + .012f, z), board, Vector3.up, false);
            for (int s = -1; s <= 1; s += 2)
            {
                deck.Surface = ComicSurface.PaintedMetal;
                deck.Box(K.V(s * (K.Edge + .15f), JettyHeight + .6f, lip - .3f), K.V(.25f, 1.2f + JettyHeight, .25f), BridgeWhite);
                deck.Box(K.V(s * (K.Edge + .15f), .6f, z0 + .2f), K.V(.25f, 1.2f, .25f), BridgeWhite);
                deck.Beam(K.V(s * (K.Edge + .15f), 1.15f, z0 + .2f), K.V(s * (K.Edge + .15f), JettyHeight + 1.15f, z0 + JettyRise), .12f, BridgeWhite);
                deck.Beam(K.V(s * (K.Edge + .15f), JettyHeight + 1.15f, z0 + JettyRise), K.V(s * (K.Edge + .15f), JettyHeight + 1.15f, lip - .3f), .12f, BridgeWhite);
            }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            Ferry(d.Props, new Vector3(K.Edge + 6.5f, d.Y(18f) + WaterLevel, 18f), 0f);
            d.KeepOut.Add(KeepOutBox(d, -1, z0, far)); d.KeepOut.Add(KeepOutBox(d, 1, z0, far));
            // Before the ramp: one free lane each row; after the gap: gifts where riders come down, obstacles further on.
            d.GiftSlots.Add(new Vector2(0f, 28f));
            d.Slot(-2.6f, 36f); d.Slot(2.6f, 36f);
            CbdBuilder.DeepTowers(d, -1, 0, 40, 27);
            SkylineRow(d, 0, 40, 6);
            return d;
        }

        // ================================================================ ground, water, edges

        // The promenade: light paving wall to wall (road and sidewalks), white limestone kerbs, dark curving bands.
        static void Promenade(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f)
        {
            ComicMesh m = d.Road;
            K.StreetSurface(m, d.Length, Beige, BeigeLight, Limestone, false, skipFrom, skipTo);
            for (float zb = 3f; zb < d.Length; zb += 9f)
            {
                float phase = zb * .37f;
                for (float x = -K.Edge; x < K.Edge - .01f; x += .5f)
                {
                    float xa = x, xb = x + .5f;
                    if (xa < -K.Half && xb > -K.Half || xa < K.Half && xb > K.Half) continue;
                    float za = zb + 1.1f * Mathf.Sin(xa * .38f + phase), zbb = zb + 1.1f * Mathf.Sin(xb * .38f + phase);
                    if (skipFrom >= 0f && Mathf.Max(za, zbb) + .5f > skipFrom && Mathf.Min(za, zbb) - .5f < skipTo) continue;
                    if (Mathf.Max(za, zbb) + .5f > d.Length || Mathf.Min(za, zbb) - .5f < 0f) continue;
                    float y = Mathf.Abs(xa + .25f) < K.Half ? .022f : K.Kerb + .026f;
                    m.Surface = ComicSurface.Plain;
                    K.Face(m, K.V(xa, y, za - .18f), K.V(xa, y, za + .18f), K.V(xb, y, zbb + .18f), K.V(xb, y, zbb - .18f), Band, Vector3.up, false);
                }
            }
        }

        // The river on the right from the sidewalk edge, a white rail at the quay edge, the quay wall.
        // side -1 (v0.6, the exit): the same on the left, out to where the shared far shore's water takes over.
        static void River(CqChunkDraft d, float z0, float z1, bool rail = true, int side = 1)
        {
            K.Water(d.Road, side * K.Edge, side > 0 ? RiverFar : -100f, z0, z1, WaterLevel, Water, Ripple, Mathf.RoundToInt(z0 * 7 + z1) + (side > 0 ? 0 : 5));
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, K.V(side * (K.Edge - .02f), WaterLevel - 1f, z0), K.V(side * (K.Edge + .25f), K.Kerb + .06f, z1), Limestone);
            if (!rail) return;
            Railing(d.SlopedFrontage, side, z0 + .3f, z1 - .3f, K.Edge + .3f);
        }

        static void Railing(ComicMesh m, int s, float z0, float z1, float x)
        {
            m.Surface = ComicSurface.Metal;
            float y = K.Kerb;
            for (float z = z0; z <= z1 + .01f; z += 2f) m.Box(K.V(s * x, y + .55f, z), K.V(.08f, 1.1f, .08f), RailGrey);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.MinMaxBox(m, K.V(s * x - .06f, y + 1.06f, z), K.V(s * x + .06f, y + 1.16f, e), RailGrey);
                K.MinMaxBox(m, K.V(s * x - .03f, y + .15f, z), K.V(s * x + .03f, y + 1f, e), Color.Lerp(RailGrey, K.R.cream, .35f));
            }
        }

        // A quay wall across the promenade where it ends at the water (its face at z looks out over the water).
        static void QuayWall(ComicMesh m, float x0, float x1, float z, bool facingForward)
        {
            m.Surface = ComicSurface.Concrete;
            float t = facingForward ? -.3f : .3f;
            K.MinMaxBox(m, K.V(x0, WaterLevel - 1f, z), K.V(x1, -.03f, z + t), Limestone);
            // white coping flush with the sidewalks and the land beside them (not over the road)
            K.MinMaxBox(m, K.V(x0, -.03f, z), K.V(-K.Half, K.Kerb - .01f, z + t), K.R.cream);
            K.MinMaxBox(m, K.V(K.Half, -.03f, z), K.V(x1, K.Kerb - .01f, z + t), K.R.cream);
        }

        // Land on the city side where the bridge and the inlet chunks have promenade (beyond the left sidewalk).
        static void LeftPromenadeLand(CqChunkDraft d, float z0, float z1)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Paving;
            K.SideBox(m, -1, z0, z1, WaterLevel - 1f, K.Kerb - .01f, 0f, 52f, BeigeLight);
            for (float z = z0 + 2f; z < z1 - 1f; z += 7f) K.Palm(d.Props, null, new Vector3(-(K.Edge + 3f), d.Ground(z), z), 6.5f, (int)z);
        }

        // The plaza beyond the left sidewalk: palms, white umbrellas, curved white seating walls.
        static void Plaza(CqChunkDraft d, float z0, float z1, bool short_)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Paving;
            K.SideBox(m, -1, z0, z1, -2.4f, K.Kerb - .01f, 0f, 60f, BeigeLight);
            m.Surface = ComicSurface.Concrete;
            for (float z = z0 + 2f; z < z1 - 3f; z += 12f) K.SideBox(m, -1, z, Mathf.Min(z1 - 1f, z + 6f), K.Kerb, K.Kerb + .45f, 6.2f, 7f, Limestone);
            int i = 0;
            for (float z = z0 + 2.5f; z < z1 - 1f; z += 8f, i++)
            {
                K.Palm(d.Props, d.Collisions, new Vector3(-(K.Edge + 2.4f), d.Ground(z), z), 6f + (i % 3), i);
                if (!short_ && i % 2 == 1) K.CafeUmbrella(d.Props, null, new Vector3(-(K.Edge + 5f), d.Ground(z + 3f), z + 3f), K.Hex("F4F2EA"));
            }
        }

        // Low waterfront pavilion: white render, timber screens, glass ground floor, a thin overhanging roof.
        static void Pavilion(CqChunkDraft d, CqLot l, Color wall)
        {
            ComicMesh m = d.Frontage;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, l.W, -2.6f, 0f, .02f, -l.Depth, K.R.ink);
            m.Surface = ComicSurface.Glass;
            K.LBox(m, l, .4f, l.W - .4f, 0, 3.4f, -.8f, -l.Depth + .5f, K.Hex("33505E"));
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, l.W, 3.4f, 7f, -.3f, -l.Depth, wall);
            m.Surface = ComicSurface.Wood;
            for (float a = .6f; a < l.W - .3f; a += .7f) K.LBox(m, l, a - .12f, a + .12f, 3.6f, 6.6f, -.2f, -.35f, Timber);
            for (float a = .2f; a < l.W; a += 4f) K.LBox(m, l, a - .15f, a + .15f, 0, 3.4f, -.6f, -.9f, wall);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.8f, l.W + .8f, 7f, 7.35f, 1.6f, -l.Depth - .4f, BridgeWhite);
            K.LBox(m, l, l.W * .2f, l.W * .5f, 7.35f, 8.2f, -2f, -5f, K.R.ink);
            d.Wall(l, 8f);
        }

        static Bounds KeepOutBox(CqChunkDraft d, int s, float z0, float z1)
        {
            float yTop = d.Y(z0) + 5f, yBottom = d.Y(z1) - 2f;
            return new Bounds(new Vector3(s * (K.Half + K.Side * .5f + .3f), (yTop + yBottom) * .5f, (z0 + z1) * .5f), new Vector3(K.Side + .6f, yTop - yBottom, z1 - z0));
        }

        // ================================================================ the bridge

        static float Hump(float z, float z0, float z1, float z2, float z3)
        {
            if (z <= z0 || z >= z3) return 0f;
            if (z < z1) return BridgeHeight * (z - z0) / (z1 - z0);
            if (z <= z2) return BridgeHeight;
            return BridgeHeight * (1f - (z - z2) / (z3 - z2));
        }

        static void BridgeDeck(CqChunkDraft d, float z0, float z1, float z2, float z3, bool arches = true)
        {
            ComicMesh m = d.SlopedFrontage;
            float w = K.Edge;
            var cuts = new List<float>();
            for (float z = z0; z < z3 - .01f; z += 1f) cuts.Add(z);
            cuts.Add(z3);
            Color centre = Color.Lerp(Deck, K.R.cream, .18f), edge = Color.Lerp(Deck, K.R.ink, .15f);
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float a = cuts[i], b = cuts[i + 1], ha = Hump(a, z0, z1, z2, z3), hb = Hump(b, z0, z1, z2, z3);
                m.Surface = ComicSurface.Wood;
                K.Face(m, K.V(-w, ha, a), K.V(-w, hb, b), K.V(w, hb, b), K.V(w, ha, a), Deck, Vector3.up, false);
                K.Face(m, K.V(-1.6f, ha + .012f, a), K.V(-1.6f, hb + .012f, b), K.V(1.6f, hb + .012f, b), K.V(1.6f, ha + .012f, a), centre, Vector3.up, false);
                // tactile strips near the edges and board joints
                foreach (int s in new[] { -1, 1 })
                    K.Face(m, K.V(s * (w - .7f), ha + .014f, a), K.V(s * (w - .7f), hb + .014f, b), K.V(s * (w - .45f), hb + .014f, b), K.V(s * (w - .45f), ha + .014f, a), edge, Vector3.up, false);
                if (i % 2 == 0) K.Face(m, K.V(-w, ha + .016f, a), K.V(-w, ha + .016f, a + .05f), K.V(w, ha + .016f, a + .05f), K.V(w, ha + .016f, a), edge, Vector3.up, false);
                // white fascia under the deck edges, and the underside
                m.Surface = ComicSurface.PaintedMetal;
                foreach (int s in new[] { -1, 1 })
                    K.Face(m, K.V(s * w, ha - .75f, a), K.V(s * w, hb - .75f, b), K.V(s * w, hb, b), K.V(s * w, ha, a), BridgeWhite, new Vector3(s, 0, 0));
                K.Face(m, K.V(-w, ha - .75f, a), K.V(-w, hb - .75f, b), K.V(w, hb - .75f, b), K.V(w, ha - .75f, a), Color.Lerp(BridgeWhite, K.R.ink, .3f), Vector3.down, false);
            }
            // railings: posts every 2 m, a top rail and a mesh panel following the hump
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * (w + .12f);
                for (float z = z0; z <= z3 + .01f; z += 2f)
                {
                    float h = Hump(z, z0, z1, z2, z3);
                    m.Surface = ComicSurface.Metal;
                    m.Box(K.V(x, h + .55f, z), K.V(.09f, 1.1f, .09f), RailGrey);
                    if (z + 2f > z3 + .01f) continue;
                    float h2 = Hump(z + 2f, z0, z1, z2, z3);
                    m.Beam(K.V(x, h + 1.12f, z), K.V(x, h2 + 1.12f, z + 2f), .1f, RailGrey);
                    K.Face(m, K.V(x, h + .12f, z), K.V(x, h2 + .12f, z + 2f), K.V(x, h2 + 1.05f, z + 2f), K.V(x, h + 1.05f, z), Color.Lerp(RailGrey, K.R.cream, .45f), new Vector3(-s, 0, 0));
                    K.Face(m, K.V(x + s * .06f, h + .12f, z), K.V(x + s * .06f, h2 + .12f, z + 2f), K.V(x + s * .06f, h2 + 1.05f, z + 2f), K.V(x + s * .06f, h + 1.05f, z), Color.Lerp(RailGrey, K.R.cream, .45f), new Vector3(s, 0, 0));
                    K.Face(m, K.V(x, h + 1.05f, z), K.V(x, h2 + 1.05f, z + 2f), K.V(x + s * .06f, h2 + 1.05f, z + 2f), K.V(x + s * .06f, h + 1.05f, z), RailGrey, Vector3.up);
                }
            }
            // two white arches in sequence, leaning out on opposite sides (the bridge's S), with hanger cables
            if (arches)
            {
                Arch(m, 1, z0 - 4f, (z1 + z2) * .5f + 3f, z0, z1, z2, z3);
                Arch(m, -1, (z1 + z2) * .5f - 3f, z3 + 4f, z0, z1, z2, z3);
            }
            // white piers into the water
            m.Surface = ComicSurface.Concrete;
            foreach (float z in new[] { z1, z2 })
                foreach (int s in new[] { -1, 1 })
                    m.Cylinder(K.V(s * 5f, (WaterLevel + BridgeHeight - .7f) * .5f, z), .45f, BridgeHeight - .7f - WaterLevel, BridgeWhite, 8);
        }

        static void Arch(ComicMesh m, int s, float za, float zb, float z0, float z1, float z2, float z3)
        {
            const int n = 16;
            const float peak = 15f, lean = 2.5f, baseX = 9.6f;
            float Foot(float z) => z <= z0 || z >= z3 ? K.Kerb : WaterLevel;
            float ya = Foot(za), yb = Foot(zb);
            Vector3 P(float t)
            {
                float bump = 4f * t * (1f - t);
                return K.V(s * (baseX + lean * bump), Mathf.Lerp(ya, yb, t) + peak * bump, Mathf.Lerp(za, zb, t));
            }
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < n; i++)
            {
                float mid = (i + .5f) / n;
                m.Beam(P((float)i / n), P((float)(i + 1) / n), .8f - .3f * 4f * mid * (1f - mid), BridgeWhite);
            }
            // feet: a block on the quay, or a pier rising out of the water
            foreach (float t in new[] { 0f, 1f })
            {
                Vector3 f = P(t);
                m.Box(f + K.V(0, -.6f, 0), K.V(1.5f, 1.6f, 1.5f), BridgeWhite);
            }
            // hangers from the arch down to the deck edge
            for (int i = 2; i < n - 1; i += 2)
            {
                Vector3 a = P((float)i / n);
                if (a.z <= z0 + .5f || a.z >= z3 - .5f) continue;
                float deck = Hump(a.z, z0, z1, z2, z3);
                m.Beam(a, K.V(s * (K.Edge + .15f), deck + 1.1f, a.z), .05f, K.R.ink);
            }
        }

        // ================================================================ landmarks (decoration: no colliders)

        // The Bell Tower: a tall glass spire with two copper sails wrapped round its base, on a round lawn by the water.
        static void BellTowerLandmark(CqChunkDraft d, Vector3 p)
        {
            ComicMesh m = d.Landmark;
            float y0 = d.Y(p.z);
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(K.V(p.x, y0 + (WaterLevel - .8f + K.Kerb) * .5f, p.z), 11f, K.Kerb + .8f - WaterLevel, Limestone, 12);
            m.Surface = ComicSurface.Foliage;
            m.Cylinder(K.V(p.x, y0 + K.Kerb + .06f, p.z), 10.4f, .12f, Lawn, 12);
            // the glass spire
            m.Surface = ComicSurface.Glass;
            m.Cylinder(K.V(p.x, y0 + 34f, p.z), 3.4f, 64f, SpireGlass, 6, .06f);
            m.Surface = ComicSurface.PaintedMetal;
            for (float y = 6f; y < 60f; y += 6f) m.Cylinder(K.V(p.x, y0 + y, p.z), 3.4f * (1f - (y - 2f) / 64f * .94f) + .06f, .25f, Color.Lerp(SpireGlass, K.R.ink, .35f), 6);
            m.Cylinder(K.V(p.x, y0 + 68f, p.z), .12f, 6f, K.R.ink, 6);
            // the copper sails: two curved wings hugging the spire either side, twisting a little as they rise
            m.Surface = ComicSurface.Metal;
            // (v0.6) like sails, not like a tiled drum: each is three panels wide at the foot and narrows to a point,
            // its top edge slanting; the one towards the city is taller (about 30 m, the bell chamber shows between them).
            for (int side = -1; side <= 1; side += 2)
            {
                int rows = side < 0 ? 10 : 8;
                for (int i = 0; i < rows; i++)
                    for (int k = -1; k <= 1; k++)
                    {
                        float t = (float)i / (rows - 1);
                        if (t > .55f && k == side || t > .82f && k == 0) continue;           // the slanting top edge
                        float y = 1.8f + i * 3.1f, ang = side * 90f + k * 26f + side * i * 4f, r = 3.9f - i * .16f;
                        Vector3 c = K.V(p.x, y0 + y, p.z) + Quaternion.Euler(0, ang, 0) * K.V(0, 0, r);
                        m.Box(c, K.V(1.95f, 3.15f, .4f), (i + k) % 2 == 0 ? Copper : CopperDark, Quaternion.Euler(0, ang, 0));
                    }
            }
            m.Surface = ComicSurface.Glass;
            m.Cylinder(K.V(p.x, y0 + 8f, p.z), 3.2f, 12f, Color.Lerp(SpireGlass, K.R.ink, .2f), 8, .9f);
        }

        // First Contact (Laurel Nannup): a five-metre cast-aluminium bird standing in a boat, wings raised, on a granite
        // base at the south-western shore by the bridge. Stylised silhouette, decoration only.
        static void FirstContact(CqChunkDraft d, Vector3 p)
        {
            ComicMesh m = d.Landmark;
            Color metal = K.Hex("E9ECEE"), shade = K.Hex("C3CAD0");
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(p + K.V(0, .3f, 0), 1.7f, .6f, K.Hex("6F7378"), 10);
            m.Surface = ComicSurface.Metal;
            // the boat: a long hull with raised ends
            m.Box(p + K.V(0, 1.1f, 0), K.V(1.1f, .7f, 3.6f), metal);
            m.Box(p + K.V(0, 1.5f, 1.95f), K.V(.8f, 1.1f, .7f), metal, Quaternion.Euler(-28f, 0, 0));
            m.Box(p + K.V(0, 1.5f, -1.95f), K.V(.8f, 1.1f, .7f), metal, Quaternion.Euler(28f, 0, 0));
            m.Box(p + K.V(0, .86f, 0), K.V(1.16f, .16f, 3.7f), shade);
            // the bird: body, long neck, head and beak, two wings swept up and back
            m.Ellipsoid(p + K.V(0, 2.5f, 0), K.V(1f, 1.9f, 1.3f), metal);
            m.Beam(p + K.V(0, 3.2f, -.3f), p + K.V(0, 4.5f, -.75f), .34f, metal);
            m.Ellipsoid(p + K.V(0, 4.7f, -.85f), K.V(.5f, .46f, .66f), metal);
            m.Box(p + K.V(0, 4.62f, -1.35f), K.V(.14f, .14f, .6f), shade);
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(p + K.V(s * .75f, 3.4f, .35f), K.V(.16f, 1.9f, 1.1f), metal, Quaternion.Euler(-24f, 0, s * -30f));
                m.Box(p + K.V(s * 1.2f, 4.5f, .9f), K.V(.14f, 1.5f, .8f), shade, Quaternion.Euler(-36f, 0, s * -16f));
            }
        }

        // Spanda: nested white ellipse rings sharing their lowest point, about 29 m tall.
        static void SpandaLandmark(CqChunkDraft d, Vector3 p)
        {
            ComicMesh m = d.Landmark;
            Quaternion q = Quaternion.Euler(0, -28f, 0); // rings turned towards riders coming down the promenade
            float[] scale = { 1f, .8f, .62f, .45f, .3f, .18f };
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(p + K.V(0, .2f, 0), 2.4f, .4f, K.R.ink, 10);
            m.Surface = ComicSurface.PaintedMetal;
            for (int r = 0; r < scale.Length; r++)
            {
                float b = 14.5f * scale[r], a = 8.2f * scale[r], t = Mathf.Lerp(.55f, .3f, r / 5f);
                int n = r < 2 ? 22 : r < 4 ? 16 : 12;
                Vector3 E(int i)
                {
                    float ang = Mathf.PI * 2f * i / n;
                    return p + q * K.V(a * Mathf.Sin(ang), b - b * Mathf.Cos(ang), 0f);
                }
                for (int i = 0; i < n; i++) m.Beam(E(i), E(i + 1), t, BridgeWhite);
            }
        }

        // The island: a paved and grassed island across the water with the red-roofed kiosk and a playground.
        static void Island(CqChunkDraft d)
        {
            ComicMesh m = d.SlopedFrontage;
            float x0 = 14f, x1 = 34f, z0 = 4f, z1 = 36f;
            m.Surface = ComicSurface.Concrete;
            K.SideBox(m, 1, z0, z1, WaterLevel - 1f, K.Kerb - .01f, x0 - K.Edge, x1 - K.Edge, Limestone);
            m.Surface = ComicSurface.Foliage;
            K.SideBox(m, 1, z0 + 2f, z1 - 2f, K.Kerb, K.Kerb + .08f, x0 - K.Edge + 1.5f, x1 - K.Edge - 1.5f, Lawn);
            float Y(float z) => d.Ground(z) + .08f;
            IslandKiosk(d.Frontage, K.V(24f, Y(26f), 26f));
            Playground(d.Frontage, K.V(22f, Y(11f), 11f));
            for (float z = 7f; z < 34f; z += 9f) K.PlaneTree(d.Props, null, K.V(31f, Y(z), z), 1f);
        }

        // The island kiosk: white walls on an ink plinth, framed windows, red hipped tile roof with ink eaves, lantern.
        static void IslandKiosk(ComicMesh f, Vector3 k)
        {
            f.Surface = ComicSurface.PaintedMetal;
            f.Box(k + K.V(0, .15f, 0), K.V(7.3f, .3f, 7.3f), K.R.ink);
            f.Surface = ComicSurface.Plaster;
            f.Box(k + K.V(0, 1.95f, 0), K.V(7f, 3.3f, 7f), BridgeWhite);
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2) f.Box(k + K.V(x * 3.45f, 1.95f, z * 3.45f), K.V(.3f, 3.3f, .3f), K.R.cream);
            foreach (Quaternion q in new[] { Quaternion.identity, Quaternion.Euler(0, 90, 0), Quaternion.Euler(0, 180, 0), Quaternion.Euler(0, 270, 0) })
                for (int i = -1; i <= 1; i++)
                {
                    f.Surface = ComicSurface.PaintedMetal;
                    f.Box(k + q * K.V(i * 2f, 1.9f, -3.5f), K.V(1.5f, 1.9f, .08f), K.R.ink, q);
                    f.Surface = ComicSurface.Glass;
                    f.Box(k + q * K.V(i * 2f, 1.9f, -3.53f), K.V(1.3f, 1.7f, .06f), K.Hex("33505E"), q);
                }
            f.Surface = ComicSurface.PaintedMetal;
            f.Box(k + K.V(0, 3.68f, 0), K.V(8f, .16f, 8f), K.R.ink);
            f.Cylinder(k + K.V(0, 5.1f, 0), 6.2f, 2.7f, RoofRed, 4, .25f, Quaternion.Euler(0, 45, 0));
            f.Surface = ComicSurface.Plaster;
            f.Cylinder(k + K.V(0, 6.95f, 0), 1.3f, 1.1f, BridgeWhite, 4, 1f, Quaternion.Euler(0, 45, 0));
            f.Surface = ComicSurface.PaintedMetal;
            f.Cylinder(k + K.V(0, 7.95f, 0), 1.65f, .9f, RoofRed, 4, .1f, Quaternion.Euler(0, 45, 0));
            f.Cylinder(k + K.V(0, 8.55f, 0), .1f, .4f, K.R.ink, 8);
        }

        // Island playground: red tower with a blue roof, yellow slide with rails, blue climbing frame, a shade sail.
        static void Playground(ComicMesh f, Vector3 pg)
        {
            Color blue = K.Hex("2E5E9E");
            f.Surface = ComicSurface.Rubber;
            f.Box(pg + K.V(1f, .04f, -.5f), K.V(9f, .08f, 8f), K.Hex("C9573C"));                      // soft-fall pad
            f.Surface = ComicSurface.PaintedMetal;
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2) f.Box(pg + K.V(x * .75f, 1.6f, z * .75f), K.V(.16f, 3.2f, .16f), K.R.red);
            f.Box(pg + K.V(0, 1.5f, 0), K.V(1.8f, .14f, 1.8f), K.R.yellow);
            for (int x = -1; x <= 1; x += 2) f.Box(pg + K.V(x * .75f, 2.05f, 0), K.V(.1f, .5f, 1.5f), K.R.red);
            f.Box(pg + K.V(0, 3.25f, 0), K.V(2f, .12f, 2f), K.R.ink);
            f.Cylinder(pg + K.V(0, 3.75f, 0), 1.35f, .9f, blue, 4, .08f, Quaternion.Euler(0, 45, 0));
            f.Surface = ComicSurface.Plastic;
            Vector3 a = pg + K.V(0, 1.55f, -.9f), b = pg + K.V(0, .3f, -4f);
            f.Beam(a, b, .1f, K.R.yellow);
            f.Box((a + b) * .5f, K.V(.9f, .1f, (b - a).magnitude), K.R.yellow, Quaternion.LookRotation(b - a));
            for (int x = -1; x <= 1; x += 2) f.Beam(a + K.V(x * .45f, .15f, 0), b + K.V(x * .45f, .15f, 0), .1f, K.R.red);
            f.Box(b + K.V(0, -.1f, -.5f), K.V(.9f, .1f, 1f), K.R.yellow);
            f.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 2; i++)
            {
                float x = 3.2f + i * 1.8f;
                f.Beam(pg + K.V(x, 0, -1f), pg + K.V(x, 2.2f, 0), .14f, blue);
                f.Beam(pg + K.V(x, 0, 1f), pg + K.V(x, 2.2f, 0), .14f, blue);
            }
            f.Box(pg + K.V(4.1f, 2.2f, 0), K.V(2f, .14f, .14f), blue);
            for (int r = 1; r <= 3; r++) f.Box(pg + K.V(4.1f, r * .55f, -1f + r * .25f), K.V(1.95f, .1f, .1f), K.R.ink);
            // shade sail: a thick triangular slab on three ink posts
            Vector3 p0 = pg + K.V(-2.2f, 4.2f, 3f), p1 = pg + K.V(6.2f, 3.7f, 2f), p2 = pg + K.V(2.2f, 4.6f, -4.2f);
            f.Surface = ComicSurface.Fabric;
            Vector3 t = K.V(0, .07f, 0);
            K.TriFacing(f, p0 + t, p1 + t, p2 + t, BridgeWhite, Vector3.up);
            K.TriFacing(f, p0, p1, p2, Color.Lerp(BridgeWhite, K.R.ink, .2f), Vector3.down);
            foreach ((Vector3 u, Vector3 v) in new[] { (p0, p1), (p1, p2), (p2, p0) })
                K.Face(f, u, v, v + t, u + t, BridgeWhite, Vector3.Cross(v - u, Vector3.up).normalized * (Vector3.Dot(Vector3.Cross(v - u, Vector3.up), (u + v) * .5f - (p0 + p1 + p2) / 3f) > 0f ? 1f : -1f));
            f.Surface = ComicSurface.PaintedMetal;
            foreach (Vector3 c in new[] { p0, p1, p2 })
            {
                f.Cylinder(new Vector3(c.x, (pg.y + c.y) * .5f, c.z), .08f, c.y - pg.y, K.R.ink, 8);
                f.Cylinder(new Vector3(c.x, pg.y + .08f, c.z), .2f, .16f, K.R.ink, 8);
            }
        }

        // A river ferry (no brand): white hull with an ink rubbing strip and a green band, framed window band with ink
        // pillars, wheelhouse, ink roof and deck rails.
        static void Ferry(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            void B(Vector3 c, Vector3 size, Color col, ComicSurface surface) { m.Surface = surface; m.Box(p + q * c, size, col, q); }
            B(K.V(0, .55f, 0), K.V(4.2f, 1.3f, 15f), BridgeWhite, ComicSurface.PaintedMetal);
            B(K.V(0, .55f, 7.7f), K.V(3f, 1.1f, .6f), BridgeWhite, ComicSurface.PaintedMetal);              // bow
            B(K.V(0, .1f, 0), K.V(4.26f, .3f, 15.06f), FerryGreen, ComicSurface.PaintedMetal);
            B(K.V(0, 1.23f, 0), K.V(4.34f, .14f, 15.1f), K.R.ink, ComicSurface.Rubber);                      // rubbing strip
            B(K.V(0, 2f, -.6f), K.V(3.6f, 1.4f, 10f), BridgeWhite, ComicSurface.PaintedMetal);
            for (int sd = -1; sd <= 1; sd += 2)
            {
                B(K.V(sd * 1.81f, 2.05f, -.6f), K.V(.06f, .8f, 9.4f), K.R.ink, ComicSurface.PaintedMetal);
                B(K.V(sd * 1.84f, 2.05f, -.6f), K.V(.04f, .66f, 9.2f), K.Hex("26333D"), ComicSurface.Glass);
                for (float z = -5f; z <= 3.9f; z += 1.5f) B(K.V(sd * 1.86f, 2.05f, z), K.V(.05f, .7f, .12f), K.R.ink, ComicSurface.PaintedMetal);
                for (float z = -7f; z <= 7f; z += 1f) B(K.V(sd * 2f, 1.6f, z), K.V(.07f, .6f, .07f), K.R.ink, ComicSurface.Metal);
                B(K.V(sd * 2f, 1.92f, 0), K.V(.08f, .08f, 14.2f), K.R.ink, ComicSurface.Metal);
            }
            B(K.V(0, 2.78f, -.6f), K.V(3.8f, .16f, 10.3f), FerryGreen, ComicSurface.PaintedMetal);
            B(K.V(0, 3.25f, 2.6f), K.V(2.2f, .8f, 2.2f), BridgeWhite, ComicSurface.PaintedMetal);              // wheelhouse
            B(K.V(0, 3.3f, 3.72f), K.V(1.9f, .45f, .06f), K.Hex("26333D"), ComicSurface.Glass);
            B(K.V(0, 3.7f, 2.6f), K.V(2.4f, .12f, 2.4f), K.R.ink, ComicSurface.PaintedMetal);
        }

        // ================================================================ far scenery, obstacles, animals

        // The CBD skyline on the city side (follows the rider, no colliders).
        static ComicMesh FarSkyline()
        {
            var m = new ComicMesh { Surface = ComicSurface.Glass };
            Color[] glass = { GlassBlue, GlassGrey, GlassPale, K.Hex("2F6B66"), K.Hex("9AAFC4"), K.Hex("E6E1D3") };
            var rng = new System.Random(6060);
            for (int i = 0; i < 14; i++)
            {
                float z = 30f + i * 13f + (float)rng.NextDouble() * 6f, x = -(66f + (float)rng.NextDouble() * 60f);
                float h = 45f + (float)rng.NextDouble() * 100f, w = 14f + (float)rng.NextDouble() * 9f;
                Color c = Color.Lerp(glass[rng.Next(glass.Length)], new Color(.62f, .76f, .88f), .35f);
                m.Box(new Vector3(x, h * .5f - 25f, z), new Vector3(w, h, w), c);
                for (float y = -18f; y < h - 26f; y += 6f) m.Box(new Vector3(x, y, z), new Vector3(w + .1f, .5f, w + .1f), Color.Lerp(c, K.R.ink, .3f));
            }
            // (no far shore here: the panorama sky shows it; boxes over the water read as floating slabs)
            return m;
        }

        // Like the kit's bollard: ink base, dark 8-sided post, cream reflective band, ink cap.
        static ComicMesh BollardMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(new Vector3(0, .08f, 0), .25f, .16f, K.R.ink, 8);
            m.Cylinder(new Vector3(0, .52f, 0), .15f, .76f, K.Hex("3A3F46"), 8);
            m.Cylinder(new Vector3(0, .78f, 0), .16f, .12f, K.R.cream, 8);
            m.Cylinder(new Vector3(0, .95f, 0), .17f, .1f, K.R.ink, 8, .7f);
            return m;
        }

        // A food truck (no brand): cab with a framed windscreen, box body, serving hatch with counter and a striped
        // awning on ink struts, a plain menu board, ink bumpers and trims, wheels with hubs. Front at +Z.
        static ComicMesh FoodTruckMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            Color body = K.Hex("F3B23A"), dark = K.Hex("26333D");
            m.Box(new Vector3(0, 1.62f, -.45f), new Vector3(2f, 2.36f, 3.5f), body);
            m.Box(new Vector3(0, 1.12f, 1.75f), new Vector3(1.96f, 1.36f, .9f), body);
            m.Box(new Vector3(0, .6f, -.1f), new Vector3(2.04f, .28f, 4.5f), K.R.red);
            m.Box(new Vector3(0, 2.84f, -.45f), new Vector3(2.06f, .1f, 3.56f), K.R.ink);
            m.Box(new Vector3(0, 1.62f, 1.25f), new Vector3(2.04f, .1f, .1f), K.R.ink);
            m.Box(new Vector3(0, 1.48f, 2.21f), new Vector3(1.8f, .62f, .06f), K.R.ink);
            m.Surface = ComicSurface.Glass;
            m.Box(new Vector3(0, 1.48f, 2.25f), new Vector3(1.66f, .5f, .04f), dark);
            for (int s = -1; s <= 1; s += 2) m.Box(new Vector3(s * 1f, 1.5f, 1.75f), new Vector3(.04f, .45f, .6f), dark);
            // serving hatch on the kerb side (-X): ink frame, dark opening, cream counter, striped awning on struts
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(new Vector3(-1.02f, 1.85f, -.5f), new Vector3(.06f, 1.1f, 2.5f), K.R.ink);
            m.Box(new Vector3(-1.04f, 1.85f, -.5f), new Vector3(.04f, .9f, 2.3f), K.Hex("1A2028"));
            m.Surface = ComicSurface.Wood;
            m.Box(new Vector3(-1.2f, 1.3f, -.5f), new Vector3(.36f, .08f, 2.4f), K.R.cream);
            m.Surface = ComicSurface.Fabric;
            for (int i = 0; i < 6; i++) m.Box(new Vector3(-1.38f, 2.55f, -1.75f + (i + .5f) * .42f), new Vector3(.8f, .08f, .42f), i % 2 == 0 ? K.R.red : BridgeWhite, Quaternion.Euler(0, 0, -16));
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { -1.7f, .7f }) m.Beam(new Vector3(-1.02f, 2.15f, z), new Vector3(-1.72f, 2.42f, z), .06f, K.R.ink);
            m.Box(new Vector3(1.03f, 1.9f, -.6f), new Vector3(.06f, .9f, 1.4f), K.R.ink);                    // menu board (plain)
            m.Box(new Vector3(1.06f, 1.9f, -.6f), new Vector3(.04f, .78f, 1.28f), K.R.cream);
            for (int i = 0; i < 3; i++) m.Box(new Vector3(1.09f, 2.1f - i * .22f, -.6f), new Vector3(.03f, .06f, .9f), K.R.ink);
            m.Box(new Vector3(0, 3.05f, -.9f), new Vector3(.8f, .36f, .8f), K.R.cream);                      // roof vent
            m.Box(new Vector3(0, 3.26f, -.9f), new Vector3(.9f, .08f, .9f), K.R.ink);
            m.Box(new Vector3(0, .5f, 2.26f), new Vector3(2.04f, .26f, .14f), K.R.ink);
            m.Box(new Vector3(0, .5f, -2.26f), new Vector3(2.04f, .26f, .14f), K.R.ink);
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * .7f, .95f, 2.22f), new Vector3(.34f, .18f, .06f), K.R.cream);
                m.Box(new Vector3(s * .8f, 1f, -2.22f), new Vector3(.24f, .3f, .06f), K.R.red);
            }
            m.Surface = ComicSurface.Rubber;
            foreach (float z in new[] { 1.45f, -1.55f })
                for (int s = -1; s <= 1; s += 2)
                {
                    m.Surface = ComicSurface.Rubber;
                    m.Cylinder(new Vector3(s * .95f, .38f, z), .38f, .28f, K.R.ink, 8, 1, Quaternion.Euler(0, 0, 90));
                    m.Surface = ComicSurface.Metal;
                    m.Cylinder(new Vector3(s * .98f, .38f, z), .17f, .28f, K.Hex("D1DBE5"), 8, 1, Quaternion.Euler(0, 0, 90));
                }
            return m;
        }

        static ComicMesh UmbrellaTableMesh()
        {
            var m = new ComicMesh();
            K.CafeUmbrella(m, null, Vector3.zero, BridgeWhite);
            return m;
        }

        // Chunky silver gull: white body and head, thick grey wing slabs with ink tips, yellow beak, legs and feet.
        static ComicMesh SeagullMesh()
        {
            var a = new ComicMesh { Surface = ComicSurface.Plastic };
            Color white = BridgeWhite, grey = K.Hex("AEB4BA"), yellow = K.Hex("F3B23A");
            a.Box(new Vector3(0, .34f, .02f), new Vector3(.3f, .26f, .52f), white);
            a.Box(new Vector3(0, .3f, -.2f), new Vector3(.26f, .2f, .18f), white);                               // chest
            a.Box(new Vector3(0, .52f, -.24f), new Vector3(.2f, .2f, .22f), white);                              // head
            a.Box(new Vector3(0, .5f, -.4f), new Vector3(.07f, .07f, .14f), yellow);                              // beak
            a.Box(new Vector3(0, .47f, -.45f), new Vector3(.06f, .05f, .05f), K.R.red);
            foreach (int s in new[] { -1, 1 })
            {
                a.Box(new Vector3(s * .17f, .4f, .08f), new Vector3(.07f, .2f, .46f), grey, Quaternion.Euler(0, 0, s * -10f));   // folded wing
                a.Box(new Vector3(s * .17f, .42f, .34f), new Vector3(.08f, .12f, .16f), K.R.ink, Quaternion.Euler(0, 0, s * -10f));
                a.Box(new Vector3(s * .105f, .56f, -.3f), new Vector3(.03f, .05f, .05f), K.R.ink);                // eye
                a.Box(new Vector3(s * .06f, .12f, 0), new Vector3(.06f, .22f, .06f), yellow);                    // leg
                a.Box(new Vector3(s * .06f, .03f, -.04f), new Vector3(.1f, .06f, .14f), yellow);                 // foot
            }
            a.Box(new Vector3(0, .38f, .33f), new Vector3(.2f, .08f, .16f), white);                              // tail
            a.Box(new Vector3(0, .38f, .43f), new Vector3(.18f, .08f, .06f), K.R.ink);
            return a;
        }

        // A silver gull (animal: kind Person, not punchable; jumpable, it is small). Facing -Z, feet at the pivot.
        static GameObject SaveSeagull(string path, float walk)
        {
            var art = SeagullMesh();
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            GameObject go = sync.Plain("Art");
            K.Renderer(go, "ANM_Quay_Seagull", art, true, false, false);
            sync.Finish();
            var box = PrefabBuildUtility.KeepSingle<BoxCollider>(root) ?? root.AddComponent<BoxCollider>();
            box.center = new Vector3(0, .3f, 0); box.size = new Vector3(.5f, .6f, .7f); box.isTrigger = false;
            var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
            o.kind = ObstacleKind.Person; o.animal = true; o.punchable = false; o.police = false; o.canJumpOver = true; o.spansRoad = false; o.spawnWeight = 1f;
            var ped = PrefabBuildUtility.GetOrAdd<Pedestrian>(root);
            ped.walkSpeed = walk; ped.crosses = false; ped.art = go.transform; ped.dodgeSpeed = 6f;
            PrefabBuildUtility.SaveAndClose(root, path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
    }
}
