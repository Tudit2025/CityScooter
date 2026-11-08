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
        public static string ChunkPath(string variant) => PrefabFolder + "/CHN_Quay_" + variant + ".prefab";
        [MenuItem("CityScooter/Districts/Build Perth CBD and Elizabeth Quay")]
        public static void BuildBoth() { CbdBuilder.Build(); Build(); }
        static string Pf(string name) => PrefabFolder + "/" + name + ".prefab";

        /// <summary>Riverside: flatter than the street (-1.6 m per 40 m instead of -3.15).</summary>
        public const float Drop = -1.6f;
        const float WaterLevel = -1.3f;

        // The bridge hump (Ramp platform) and the jetty jump over the inlet (Ramp + RoadGap). Numbers for gameplay:
        public const float BridgeStart = 6f, BridgeRise = 10f, BridgeHeight = 2.2f, BridgeTop = 8f, BridgeDown = 8f;
        public const float JettyStart = 6f, JettyRise = 6f, JettyHeight = 1.4f, JettyTop = 3f, GapLength = 6f;

        // Elizabeth Quay palette (sRGB; sources in art-src/districts/quay/SOURCES.txt).
        static readonly Color Beige = K.Hex("D8C7A6"), BeigeLight = K.Hex("E3D6BC"), Band = K.Hex("8C877E"), Limestone = K.Hex("ECE6D8");
        static readonly Color Water = K.Hex("2F6E9A"), Ripple = K.Hex("5D9AC6"), BridgeWhite = K.Hex("F4F4F0"), Deck = K.Hex("8A5A3C"),
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
            K.WriteChunk(BellTower(), ChunkPath("BellTower"));
            K.WriteChunk(Bridge(), ChunkPath("Bridge"));
            K.WriteChunk(Spanda(), ChunkPath("Spanda"));
            K.WriteChunk(Inlet(), ChunkPath("Inlet"));
            K.WriteChunk(Exit(), ChunkPath("Exit"));
            GameObject far = K.SaveFarScenery(Pf("FAR_Quay_Skyline"), "FAR_Quay_Skyline", FarSkyline());

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
            d.chunks = ChunkIds.Select(c => AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(c)).GetComponent<RoadChunk>()).ToArray();
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
            d.gateway = null;
            d.ambience = null;
            d.ambienceVolume = .35f;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            K.WriteReport("art-src/districts/quay/catalog.txt");
            Debug.Log("[Districts/Quay] built " + (ChunkIds.Length + 2) + " chunks: " + string.Join("; ", K.Report));
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

        static ChunkDraft Entry()
        {
            // Calm at z = 0 (low walls, hedges, trees), then the river opens up on the right and the plaza on the left.
            var d = new ChunkDraft("Quay_Entry", Drop);
            Promenade(d);
            for (int s = -1; s <= 1; s += 2)
            {
                CbdBuilder.GardenEdge(d, s, 0, 14);
                for (float z = 3; z < 14; z += 5.5f) K.PlaneTree(d.Props, d.Collisions, new Vector3(s * 5.6f, d.Ground(z), z), .9f);
            }
            River(d, 14, 40);
            K.Water(d.Road, K.Edge + 14f, 260f, 0, 14, WaterLevel, Water, Ripple, 11);
            d.SlopedFrontage.Surface = ComicSurface.Paving;
            K.SideBox(d.SlopedFrontage, -1, 0, 14, -2.4f, K.Kerb - .02f, 14f, 60f, BeigeLight);
            Plaza(d, 14, 40, true);
            Pavilion(d, new Lot(-1, 22, 40, K.Edge + 10, 12, d.Base(31)), K.Hex("F2EEE4"));
            CbdBuilder.DeepTowers(d, -1, 14, 40, 21);
            d.StandardSlots();
            return d;
        }

        static ChunkDraft Exit()
        {
            // The river and plaza first, then calm towards z = 40: low walls, hedges, trees.
            var d = new ChunkDraft("Quay_Exit", Drop);
            Promenade(d);
            River(d, 0, 26);
            K.Water(d.Road, K.Edge + 14f, 260f, 26, 40, WaterLevel, Water, Ripple, 12);
            d.SlopedFrontage.Surface = ComicSurface.Paving;
            K.SideBox(d.SlopedFrontage, -1, 26, 40, -2.4f, K.Kerb - .02f, 14f, 60f, BeigeLight);
            Plaza(d, 0, 26, true);
            Pavilion(d, new Lot(-1, 0, 16, K.Edge + 10, 12, d.Base(8)), K.Hex("E9E2D2"));
            CbdBuilder.DeepTowers(d, -1, 0, 26, 22);
            for (int s = -1; s <= 1; s += 2)
            {
                CbdBuilder.GardenEdge(d, s, 26, 40);
                for (float z = 28; z < 40; z += 5.5f) K.PlaneTree(d.Props, d.Collisions, new Vector3(s * 5.6f, d.Ground(z), z), .9f);
            }
            d.StandardSlots();
            return d;
        }

        static ChunkDraft Promenade()
        {
            var d = new ChunkDraft("Quay_Promenade", Drop);
            Promenade(d);
            River(d, 0, 40);
            Plaza(d, 0, 40, false);
            Pavilion(d, new Lot(-1, 4, 20, K.Edge + 10, 12, d.Base(12)), K.Hex("F2EEE4"));
            CbdBuilder.GlassTower(d, new Lot(-1, 22, 38, K.Edge + 12, 16, d.Base(30)), 96, GlassGrey, FrameLight, 0);
            CbdBuilder.DeepTowers(d, -1, 0, 40, 23);
            Ferry(d.Props, new Vector3(24f, d.Y(24f) + WaterLevel, 24f), 12f);
            d.StandardSlots();
            return d;
        }

        static ChunkDraft BellTower()
        {
            var d = new ChunkDraft("Quay_BellTower", Drop);
            Promenade(d);
            River(d, 0, 40);
            // the ferry jetty: a timber pier out from the quay with a ferry alongside
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Wood;
            K.SideBox(m, 1, 4, 8, K.Kerb - .25f, K.Kerb, .2f, 9f, Deck);
            for (float x = 1f; x < 9f; x += 2.6f)
                foreach (float z in new[] { 4.3f, 7.7f }) K.SideBox(m, 1, z - .15f, z + .15f, WaterLevel - .5f, K.Kerb - .25f, x - .15f, x + .15f, Color.Lerp(Deck, K.R.ink, .4f));
            Ferry(d.Props, new Vector3(K.Edge + 11.2f, d.Y(16f) + WaterLevel, 16f), 0f);
            BellTowerLandmark(d, new Vector3(34f, d.Y(26f), 26f));
            Plaza(d, 0, 40, false);
            Pavilion(d, new Lot(-1, 0, 14, K.Edge + 10, 12, d.Base(7)), K.Hex("E9E2D2"));
            CbdBuilder.GlassTower(d, new Lot(-1, 16, 34, K.Edge + 12, 18, d.Base(25)), 132, GlassBlue, FrameLight, 1);
            CbdBuilder.DeepTowers(d, -1, 0, 40, 24);
            d.StandardSlots();
            return d;
        }

        static ChunkDraft Spanda()
        {
            var d = new ChunkDraft("Quay_Spanda", Drop);
            Promenade(d);
            River(d, 0, 40);
            Island(d);
            Plaza(d, 0, 40, false);
            SpandaLandmark(d, new Vector3(-(K.Edge + 11f), d.Ground(22f), 22f));
            Pavilion(d, new Lot(-1, 30, 40, K.Edge + 10, 12, d.Base(35)), K.Hex("F2EEE4"));
            CbdBuilder.GlassTower(d, new Lot(-1, 2, 16, K.Edge + 14, 16, d.Base(9)), 84, GlassPale, FrameLight, 3);
            CbdBuilder.DeepTowers(d, -1, 0, 40, 25);
            d.StandardSlots();
            return d;
        }

        static ChunkDraft Bridge()
        {
            // The road goes up and over the white double-arched bridge: a Ramp platform (rise + crown); off the crown the
            // rider flies over the descending half. Water on both sides and under the deck; no ground beside the deck.
            var d = new ChunkDraft("Quay_Bridge", Drop);
            float z0 = BridgeStart, z1 = z0 + BridgeRise, z2 = z1 + BridgeTop, z3 = z2 + BridgeDown;
            Promenade(d, z0, z3);
            K.Water(d.Road, -60f, 260f, z0, z3, WaterLevel, Water, Ripple, 31);
            // the promenade ends at quay walls either side of the inlet under the bridge; the river is on the right
            QuayWall(d.Road, -60f, K.Edge, z0, true);
            QuayWall(d.Road, -60f, K.Edge, z3, false);
            River(d, 0, z0);
            River(d, z3, 40);
            LeftPromenadeLand(d, 0, z0);
            LeftPromenadeLand(d, z3, 40);
            d.Ramps.Add(new RampSpec { Name = "BridgeHump", X = 0f, Z = z0, Width = 2f * K.Edge, Rise = BridgeRise, Height = BridgeHeight, Top = BridgeTop, Launch = 1f, Hang = .5f });
            BridgeDeck(d, z0, z1, z2, z3);
            d.KeepOut.Add(KeepOutBox(d, -1, z0, z3)); d.KeepOut.Add(KeepOutBox(d, 1, z0, z3));
            // No obstacles on the hump or where riders land; a row of gifts after it, a gap in the middle for a lane.
            d.GiftSlots.Add(new Vector2(-2.6f, 37f)); d.GiftSlots.Add(new Vector2(2.6f, 37f));
            CbdBuilder.DeepTowers(d, -1, 0, z0, 26);
            CbdBuilder.DeepTowers(d, -1, z3 - 2f, 40, 26);
            return d;
        }

        static ChunkDraft Inlet()
        {
            // A timber jetty ramp right up to the quay edge, then 6 m of open water across the whole promenade (SPLASH!),
            // then the promenade again. The ramp covers the full rideable width, so the gap can always be cleared.
            var d = new ChunkDraft("Quay_Inlet", Drop);
            float z0 = JettyStart, lip = z0 + JettyRise + JettyTop, far = lip + GapLength;
            Promenade(d, lip, far);
            K.Water(d.Road, -22f, 260f, lip, far, WaterLevel, Water, Ripple, 41);
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
            var ramp = new RampSpec { Name = "JettyRamp", X = 0f, Z = z0, Width = 2f * K.Edge, Rise = JettyRise, Height = JettyHeight, Top = JettyTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(ramp);
            d.Gaps.Add(new GapSpec { Name = "InletWater", Z = lip, Length = GapLength, Width = 0f, Word = "SPLASH", Water = true });
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
            return d;
        }

        // ================================================================ ground, water, edges

        // The promenade: light paving wall to wall (road and sidewalks), white limestone kerbs, dark curving bands.
        static void Promenade(ChunkDraft d, float skipFrom = -1f, float skipTo = -1f)
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
        static void River(ChunkDraft d, float z0, float z1, bool rail = true)
        {
            K.Water(d.Road, K.Edge, 260f, z0, z1, WaterLevel, Water, Ripple, Mathf.RoundToInt(z0 * 7 + z1));
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, K.V(K.Edge - .02f, WaterLevel - 1f, z0), K.V(K.Edge + .25f, K.Kerb + .06f, z1), Limestone);
            if (!rail) return;
            Railing(d.SlopedFrontage, 1, z0 + .3f, z1 - .3f, K.Edge + .3f);
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
                K.MinMaxBox(m, K.V(s * x - .01f, y + .15f, z), K.V(s * x + .01f, y + 1f, e), Color.Lerp(RailGrey, K.R.cream, .35f));
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
        static void LeftPromenadeLand(ChunkDraft d, float z0, float z1)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Paving;
            K.SideBox(m, -1, z0, z1, WaterLevel - 1f, K.Kerb - .01f, 0f, 52f, BeigeLight);
            for (float z = z0 + 2f; z < z1 - 1f; z += 7f) K.Palm(d.Props, null, new Vector3(-(K.Edge + 3f), d.Ground(z), z), 6.5f, (int)z);
        }

        // The plaza beyond the left sidewalk: palms, white umbrellas, curved white seating walls.
        static void Plaza(ChunkDraft d, float z0, float z1, bool short_)
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
        static void Pavilion(ChunkDraft d, Lot l, Color wall)
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

        static Bounds KeepOutBox(ChunkDraft d, int s, float z0, float z1)
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

        static void BridgeDeck(ChunkDraft d, float z0, float z1, float z2, float z3)
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
                    K.Face(m, K.V(x + s * .01f, h + .12f, z), K.V(x + s * .01f, h2 + .12f, z + 2f), K.V(x + s * .01f, h2 + 1.05f, z + 2f), K.V(x + s * .01f, h + 1.05f, z), Color.Lerp(RailGrey, K.R.cream, .45f), new Vector3(s, 0, 0));
                }
            }
            // two white arches in sequence, leaning out on opposite sides (the bridge's S), with hanger cables
            Arch(m, 1, z0 - 4f, (z1 + z2) * .5f + 3f, z0, z1, z2, z3);
            Arch(m, -1, (z1 + z2) * .5f - 3f, z3 + 4f, z0, z1, z2, z3);
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
        static void BellTowerLandmark(ChunkDraft d, Vector3 p)
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
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 9; i++)
                    for (int k = -1; k <= 1; k++)
                    {
                        float y = 1.8f + i * 3.1f, ang = side * 90f + k * 26f + side * i * 4f, r = 3.7f - i * .14f;
                        Quaternion q = Quaternion.Euler(0, ang - 90f, 0);
                        Vector3 c = K.V(p.x, y0 + y, p.z) + Quaternion.Euler(0, ang, 0) * K.V(0, 0, r);
                        m.Box(c, K.V(1.9f, 3.15f, .45f), (i + k) % 2 == 0 ? Copper : CopperDark, Quaternion.Euler(0, ang, 0));
                    }
            m.Surface = ComicSurface.Glass;
            m.Cylinder(K.V(p.x, y0 + 8f, p.z), 3.2f, 12f, Color.Lerp(SpireGlass, K.R.ink, .2f), 8, .9f);
        }

        // Spanda: nested white ellipse rings sharing their lowest point, about 29 m tall.
        static void SpandaLandmark(ChunkDraft d, Vector3 p)
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
        static void Island(ChunkDraft d)
        {
            ComicMesh m = d.SlopedFrontage;
            float x0 = 14f, x1 = 34f, z0 = 4f, z1 = 36f;
            m.Surface = ComicSurface.Concrete;
            K.SideBox(m, 1, z0, z1, WaterLevel - 1f, K.Kerb - .01f, x0 - K.Edge, x1 - K.Edge, Limestone);
            m.Surface = ComicSurface.Foliage;
            K.SideBox(m, 1, z0 + 2f, z1 - 2f, K.Kerb, K.Kerb + .08f, x0 - K.Edge + 1.5f, x1 - K.Edge - 1.5f, Lawn);
            ComicMesh f = d.Frontage;
            float Y(float z) => d.Ground(z) + .08f;
            // kiosk: white walls, red hipped tile roof with a little lantern
            Vector3 k = K.V(24f, Y(26f), 26f);
            f.Surface = ComicSurface.Plaster;
            f.Box(k + K.V(0, 1.8f, 0), K.V(7f, 3.6f, 7f), BridgeWhite);
            f.Surface = ComicSurface.Glass;
            f.Box(k + K.V(-3.52f, 1.6f, 0), K.V(.05f, 2f, 5f), K.Hex("33505E"));
            f.Box(k + K.V(0, 1.6f, -3.52f), K.V(5f, 2f, .05f), K.Hex("33505E"));
            f.Surface = ComicSurface.PaintedMetal;
            f.Cylinder(k + K.V(0, 5f, 0), 6.2f, 2.8f, RoofRed, 4, .25f, Quaternion.Euler(0, 45, 0));
            f.Cylinder(k + K.V(0, 6.9f, 0), 1.3f, 1.2f, BridgeWhite, 4, 1f, Quaternion.Euler(0, 45, 0));
            f.Cylinder(k + K.V(0, 7.9f, 0), 1.6f, .9f, RoofRed, 4, .1f, Quaternion.Euler(0, 45, 0));
            // playground: yellow slide on a red tower, a blue climbing frame, a white shade sail
            Vector3 pg = K.V(22f, Y(11f), 11f);
            f.Box(pg + K.V(0, 1.2f, 0), K.V(1.6f, 2.4f, 1.6f), K.R.red);
            f.Box(pg + K.V(0, 2.55f, 0), K.V(1.9f, .3f, 1.9f), K.R.yellow);
            f.Beam(pg + K.V(0, 2.4f, -.9f), pg + K.V(0, .3f, -4.2f), .8f, K.R.yellow);
            for (int i = 0; i < 4; i++) f.Beam(pg + K.V(3f + (i % 2) * 2f, 0, -1f + (i / 2) * 2f), pg + K.V(4f, 2.4f, 0), .14f, K.Hex("2E5E9E"));
            f.Box(pg + K.V(4f, 2.45f, 0), K.V(.5f, .5f, .5f), K.Hex("2E5E9E"));
            f.Surface = ComicSurface.Fabric;
            K.TriFacing(f, pg + K.V(-2f, 4.2f, 3f), pg + K.V(6f, 3.6f, 2f), pg + K.V(2f, 4.6f, -4f), BridgeWhite, Vector3.up);
            K.TriFacing(f, pg + K.V(-2f, 4.2f, 3f), pg + K.V(6f, 3.6f, 2f), pg + K.V(2f, 4.6f, -4f), BridgeWhite, Vector3.down);
            foreach (Vector3 c in new[] { K.V(-2f, 0, 3f), K.V(6f, 0, 2f), K.V(2f, 0, -4f) }) f.Cylinder(pg + c + K.V(0, 2.2f, 0), .07f, 4.4f, K.R.ink, 6);
            // trees on the island
            for (float z = 7f; z < 34f; z += 9f) K.PlaneTree(d.Props, null, K.V(31f, Y(z), z), 1.1f);
        }

        static void Ferry(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            void B(Vector3 c, Vector3 s, Color col) => m.Box(p + q * c, s, col, q);
            m.Surface = ComicSurface.PaintedMetal;
            B(K.V(0, .6f, 0), K.V(4.2f, 1.4f, 15f), BridgeWhite);
            B(K.V(0, .2f, 0), K.V(4.25f, .5f, 15.05f), FerryGreen);
            B(K.V(0, 1.95f, -.5f), K.V(3.6f, 1.5f, 10f), BridgeWhite);
            m.Surface = ComicSurface.Glass;
            B(K.V(0, 2.05f, -.5f), K.V(3.65f, .7f, 9.4f), K.Hex("26333D"));
            m.Surface = ComicSurface.PaintedMetal;
            B(K.V(0, 2.85f, -1.5f), K.V(3.2f, .3f, 6f), FerryGreen);
            B(K.V(0, 3.2f, 2.5f), K.V(2f, .6f, 2f), BridgeWhite);
        }

        // ================================================================ far scenery, obstacles, animals

        // The CBD skyline on the city side and the far shore across the river (follows the rider, no colliders).
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
            // the far shore: a low green line with pale low-rise buildings
            m.Surface = ComicSurface.Foliage;
            for (float z = 0f; z < 300f; z += 30f)
                m.Box(new Vector3(270f, -16f, z + 15f), new Vector3(30f, 22f, 30.2f), Color.Lerp(Lawn, new Color(.62f, .76f, .88f), .45f));
            m.Surface = ComicSurface.Concrete;
            for (int i = 0; i < 16; i++)
            {
                float z = i * 18f + (float)rng.NextDouble() * 8f, h = 4f + (float)rng.NextDouble() * 14f;
                m.Box(new Vector3(262f + (float)rng.NextDouble() * 12f, -5f + h * .5f, z), new Vector3(8f, h, 10f), Color.Lerp(K.R.cream, new Color(.62f, .76f, .88f), .45f));
            }
            return m;
        }

        static ComicMesh BollardMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(new Vector3(0, .05f, 0), .26f, .1f, K.R.ink, 8);
            m.Cylinder(new Vector3(0, .48f, 0), .18f, .86f, K.Hex("2A2D33"), 8);
            m.Cylinder(new Vector3(0, .93f, 0), .2f, .1f, K.Hex("2A2D33"), 8, .6f);
            m.Cylinder(new Vector3(0, .75f, 0), .185f, .08f, BridgeWhite, 8);
            return m;
        }

        // A food truck (no brand): bright body, serving hatch with an awning, wheels. Front at +Z.
        static ComicMesh FoodTruckMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            Color body = K.Hex("F3B23A");
            m.Box(new Vector3(0, 1.6f, -.4f), new Vector3(2f, 2.4f, 3.6f), body);
            m.Box(new Vector3(0, 1.1f, 1.8f), new Vector3(1.95f, 1.5f, 1f), body);
            m.Box(new Vector3(0, .45f, 0), new Vector3(2.04f, .3f, 4.5f), K.R.red);
            m.Surface = ComicSurface.Glass;
            m.Box(new Vector3(0, 1.55f, 2.31f), new Vector3(1.7f, .6f, .03f), K.Hex("26333D"));
            m.Box(new Vector3(-1.01f, 1.9f, -.4f), new Vector3(.03f, 1f, 2.4f), K.Hex("26333D"));
            m.Surface = ComicSurface.Fabric;
            for (int i = 0; i < 6; i++) m.Box(new Vector3(-1.3f, 2.55f, -1.5f + i * .44f), new Vector3(.8f, .08f, .44f), i % 2 == 0 ? K.R.red : BridgeWhite, Quaternion.Euler(0, 0, -15));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(new Vector3(-1.08f, 1.32f, -.4f), new Vector3(.2f, .06f, 2.4f), BridgeWhite);           // counter
            m.Box(new Vector3(0, 2.95f, -.4f), new Vector3(1.2f, .3f, 1.2f), K.R.cream);                  // roof vent
            m.Surface = ComicSurface.Rubber;
            for (int s = -1; s <= 1; s += 2)
                foreach (float z in new[] { 1.5f, -1.6f })
                    m.Cylinder(new Vector3(s * .95f, .38f, z), .38f, .26f, K.R.ink, 8, 1, Quaternion.Euler(0, 0, 90));
            return m;
        }

        static ComicMesh UmbrellaTableMesh()
        {
            var m = new ComicMesh();
            K.CafeUmbrella(m, null, Vector3.zero, BridgeWhite);
            return m;
        }

        // A silver gull (animal: kind Person, not punchable; jumpable, it is small). Facing -Z, feet at the pivot.
        static GameObject SaveSeagull(string path, float walk)
        {
            var art = new ComicMesh { Surface = ComicSurface.Plain };
            Color white = BridgeWhite, grey = K.Hex("AEB4BA"), beak = K.Hex("E8402E");
            art.Box(new Vector3(0, .32f, 0), new Vector3(.26f, .24f, .5f), white);
            art.Box(new Vector3(0, .38f, .14f), new Vector3(.3f, .1f, .36f), grey);                 // folded wings
            art.Box(new Vector3(0, .36f, .3f), new Vector3(.16f, .08f, .2f), K.R.ink);               // tail tip
            art.Box(new Vector3(0, .5f, -.2f), new Vector3(.17f, .17f, .18f), white);                // head
            art.Box(new Vector3(0, .48f, -.33f), new Vector3(.05f, .05f, .12f), beak);
            foreach (int s in new[] { -1, 1 })
            {
                art.Box(new Vector3(s * .05f, .1f, 0), new Vector3(.03f, .2f, .03f), beak);
                art.Box(new Vector3(s * .06f, .53f, -.25f), new Vector3(.02f, .03f, .03f), K.R.ink);
            }
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
