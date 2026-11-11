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
    /// KWINANA (route order 120) and WELLARD (order 130), v0.4 south line, south art agent 3.
    /// KWINANA: the industrial coast. A wide industrial road with trucks, chain-link fences with warning signs, red-earth
    /// verges, refinery columns, flare stacks, storage tanks and pipe racks, the coastal grain terminal (white silos and
    /// the blue-and-white striped head house, a blue conveyor out to the jetty), a grain train, Kwinana Beach with the
    /// half-buried rusty hull of the old ship, a loading-dock ramp before a pipe trench across the road (WHOOPS).
    /// WELLARD: a family suburb. The station (teal entrance canopy, car park, the Mandurah line), new estate streets
    /// (rendered brick houses, Colorbond roofs, limestone retaining walls, young street trees), the village square with
    /// cafes, a park with a playground under shade sails (shared path, no cars) and The Spectacles paperbark wetland on a
    /// boardwalk with a boardwalk ramp over open water (SPLASH). Black swans and ducks.
    /// v0.6 (docs/v0.6-polish.md, south line B): the boundaries are no longer a hedge-and-lawn gap. Kwinana's entry starts
    /// with Aubin Grove's freeway-and-railway corridor and lets it swing away (KwEntryFreewayCorridor: the one function
    /// to swap for SouthLineKit); the Mandurah line runs from Kwinana's exit to Wellard station (RailLine); Wellard's
    /// exit brings the coast in to meet Rockingham's foreshore on the boundary line (WlCoastArrival). Surfaces change
    /// at a kerb band inside the entry / exit chunks (SurfaceBand) and path chunks begin and end with a street stub
    /// (StreetStubs). Polish notes: art-src/districts/kwinana|wellard/previews/polish/NOTES.txt.
    /// Built with the CBD/Quay kit (CbdQuayKit, CqChunkDraft): the standard street cross-section at both ends of every
    /// chunk, 25k triangles and 8 renderers per chunk, only ComicPalette and ComicInk. Landmarks are decoration only.
    ///   Menu: CityScooter/Districts/Build Kwinana and Wellard
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.KwinanaWellardBuilder.BuildBatch
    ///   With the props atlases (needs graphics): ...KwinanaWellardBuilder.BuildAndRenderAtlasBatch
    /// </summary>
    public static class KwinanaWellardBuilder
    {
        public const string KwinanaId = "kwinana", WellardId = "wellard";
        public const string DataFolder = "Assets/_Project/Data/Districts";
        public const string KwinanaPath = DataFolder + "/District_Kwinana.asset", WellardPath = DataFolder + "/District_Wellard.asset";
        public static readonly string[] KwinanaChunks = { "Refinery", "Tanks", "GrainTerminal", "Beach", "Trench" };
        public static readonly string[] WellardChunks = { "Station", "Estate", "Village", "Park", "Wetland" };
        public static string PrefabFolder(string tag) => ComicCityBuilder.Prefabs + "/Districts/" + tag;
        static string MeshFolder(string tag) => ComicCityBuilder.Root + "/Districts/" + tag;
        public static string ChunkPath(string tag, string variant) => PrefabFolder(tag) + "/CHN_" + tag + "_" + variant + ".prefab";
        static string Pf(string tag, string name) => PrefabFolder(tag) + "/" + name + ".prefab";

        /// <summary>Both districts are fairly flat: -2 m per 40 m (the street is -3.15).</summary>
        public const float KwinanaDrop = -2f, WellardDrop = -2f;
        // Ramp before a road-wide gap, numbers for gameplay (lip = start + rise + top; the gap starts at the lip).
        public const float TrenchRampZ = 6f, TrenchRise = 6f, TrenchHeight = 1.4f, TrenchTop = 2f, TrenchGap = 5f;
        public const float BoardwalkRampZ = 8f, BoardwalkRise = 6f, BoardwalkHeight = 1.4f, BoardwalkTop = 2f, WetlandGap = 5f;
        const float SeaLevel = -.9f, PondLevel = -.85f;

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Color H(string hex) => K.Hex(hex);

        // ---- Kwinana palette (sRGB; sources in art-src/districts/kwinana/SOURCES.txt)
        static readonly Color RedEarth = H("B5603A"), RedEarthDark = H("96492B"), Gravel = H("C2AE8A"), Scrub = H("A9A472"),
            FenceGrey = H("9AA3AA"), FenceDark = H("5E676F"), Wire = H("7F8A93"), Concrete = H("C9C4B8"), KerbCap = H("E9E2CF");
        static readonly Color TankWhite = H("EEF0EB"), TankBand = H("8DB3A4"), TankGreen = H("B9CDB4"), TankCream = H("E9DDBF"), Steel = H("A9B2B9"), SteelDark = H("5D666E"),
            PipeSilver = H("CBD1D6"), PipeYellow = H("E8C24A"), PipeGreen = H("6E9E6A"), PipeRed = H("B54A3A");
        static readonly Color FlareRed = H("D9442E"), FlareWhite = H("F2F0EA"), Flame = H("FF8A2A"), FlameCore = H("FFE066"), Smoke = H("E4E1DA");
        static readonly Color SiloWhite = H("EDEFEA"), SiloShade = H("D3D8D6"), TerminalBlue = H("2F5FA8"), TerminalBlueDark = H("22477F");
        static readonly Color Rust = H("8E3B22"), RustLight = H("B35A30"), RustDark = H("5A281A");
        static readonly Color Sand = H("EFE5C8"), SandWet = H("D9CBA4"), Sea = H("3FB0B8"), SeaDeep = H("2D86A6"), Ripple = H("8FD7D7"), Foam = H("EAF7F2");
        static readonly Color Limestone = H("E3D3A8"), LimestoneDark = H("C9B583"), PineGreen = H("2F5E34"), PineLight = H("4C7E45");

        // ---- Wellard palette
        static readonly Color Lawn = H("6FA548"), LawnLight = H("86B85A"), Pave = H("D2CEC4"), Ret = H("E6D7AE"), RetCap = H("F2E9CC");
        static readonly Color[] HouseWalls = { H("EDE3CF"), H("D9C3A0"), H("C9CDD0"), H("B8694A"), H("E8D5BA"), H("A9B4B8") };
        static readonly Color[] Roofs = { H("4A4F55"), H("6E7378"), H("E6E5DE"), H("8C9190"), H("A0573E"), H("3E4C5A") };
        static readonly Color StationTeal = H("1F7F96"), StationTealLight = H("38A3BA"), StationOrange = H("F39A1E"), StationGrey = H("AEB4B8"),
            PlatformBrick = H("B5523B"), TrainSilver = H("D6DADD"), TrainBlue = H("2B5E9A");
        static readonly Color PaperTrunk = H("E6D9BE"), PaperPatch = H("B39C78"), PaperLeaf = H("7E8F5A"), PaperLeafLight = H("9BAA6A"), PaperLeafDark = H("63734A");
        static readonly Color Pond = H("3E86A6"), PondLight = H("6FAACB"), Duckweed = H("9CC23A"), Reed = H("8C9A4A"), ReedDark = H("5E6B34");
        static readonly Color Boards = H("8E7A64"), BoardsLight = H("A28E76"), Rail = H("A68A6A");
        static readonly Color SailYellow = H("F2C230"), SailTeal = H("2FA7B0"), SailRed = H("E0533A"), Softfall = H("3A7FC2"), SoftfallRed = H("C95B3A");
        static readonly Color Dirt = H("A8703F"), DirtDark = H("86552F");
        static readonly Color GlassDark = H("26333D");

        [MenuItem("CityScooter/Districts/Build Kwinana and Wellard")]
        public static void Build() { BuildKwinana(false); BuildWellard(false); }

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Builds both districts and renders their props atlases (run WITHOUT -nographics).</summary>
        public static void BuildAndRenderAtlasBatch()
        {
            try { BuildKwinana(true); BuildWellard(true); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Begin(string tag, string patternFolder)
        {
            K.Begin(MeshFolder(tag), PrefabFolder(tag), DataFolder, patternFolder);
            P.Init();
        }

        /// <summary>The system owns gateway, ambience and the sky fields: only a NEW asset gets the sky placeholders
        /// (the CBD skyline turned behind and sunk below the horizon, skyline 0: these districts are far south of the
        /// city, the horizon is the district's own far scenery).</summary>
        static District LoadDistrict(string path)
        {
            var d = AssetDatabase.LoadAssetAtPath<District>(path);
            if (d != null) return d;
            d = ScriptableObject.CreateInstance<District>();
            d.skyYaw = 180f; d.skySize = .6f; d.skyHeight = -12f; d.skyline = 0f;
            AssetDatabase.CreateAsset(d, path);
            return d;
        }

        static RoadChunk Chunk(string tag, string variant) => AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(tag, variant)).GetComponent<RoadChunk>();

        static IEnumerable<HazardPattern> StreetPatterns(TrackTheme theme, params string[] names) =>
            theme.hazards == null ? Enumerable.Empty<HazardPattern>() : theme.hazards.Where(h => h != null && names.Contains(h.name));

        // =====================================================================================================
        // KWINANA
        // =====================================================================================================

        static void BuildKwinana(bool atlas)
        {
            const string tag = "Kwinana";
            string patternFolder = DataFolder + "/kwinana";
            Begin(tag, patternFolder);

            // ---- obstacles, traffic, animals, ramp
            Obstacle truck = K.SaveObstacle(Pf(tag, "OBS_Kwinana_Truck"), "OBS_Kwinana_Truck", TipperTruckMesh(H("F2F0EA"), H("C8452F"), RedEarth), new Vector3(2.6f, 3.4f, 8.6f), ObstacleKind.Vehicle, false, false, .45f);
            Obstacle barrier = K.SaveObstacle(Pf(tag, "OBS_Kwinana_WaterBarrier"), "OBS_Kwinana_WaterBarrier", WaterBarrierMesh(), new Vector3(2.2f, 1f, .7f), ObstacleKind.Thing, true, true, 1f);
            Obstacle pallets = K.SaveObstacle(Pf(tag, "OBS_Kwinana_Pallets"), "OBS_Kwinana_Pallets", PalletsMesh(), new Vector3(1.4f, 1.05f, 1.2f), ObstacleKind.Thing, true, true, 1f);
            Obstacle drums = K.SaveObstacle(Pf(tag, "OBS_Kwinana_OilDrums"), "OBS_Kwinana_OilDrums", DrumsMesh(), new Vector3(1.5f, .95f, 1.1f), ObstacleKind.Thing, true, true, 1f);
            GameObject truckAhead = K.SaveVehicle(Pf(tag, "VEH_Kwinana_TruckAhead"), "VEH_Kwinana_Truck", TipperTruckMesh(H("F3B23A"), H("D8DDE0"), Limestone), new Vector3(2.6f, 3.4f, 8.6f), 0f, .35f);
            GameObject tanker = K.SaveVehicle(Pf(tag, "VEH_Kwinana_TankerOncoming"), "VEH_Kwinana_Tanker", TankerMesh(), new Vector3(2.6f, 3.7f, 11.6f), -12f, 0f);
            GameObject gull = SaveAnimal(Pf(tag, "ANM_Kwinana_Seagull"), "ANM_Kwinana_Seagull", SeagullMesh(), new Vector3(.5f, .6f, .7f), true, 0f, false);
            GameObject gullWalking = SaveAnimal(Pf(tag, "ANM_Kwinana_SeagullWalking"), "ANM_Kwinana_Seagull", null, new Vector3(.5f, .6f, .7f), true, .6f, false);
            Ramp dock = K.SaveRamp(Pf(tag, "RMP_Kwinana_LoadingDock"), "RMP_Kwinana_LoadingDock", 3f, 4.5f, 1.2f, 1.5f, 1f, .5f, H("A9AEB2"), H("7D8288"), true);

            // ---- chunks
            K.WriteChunk(KwEntry(), ChunkPath(tag, "Entry"));
            K.WriteChunk(KwRefinery(), ChunkPath(tag, "Refinery"));
            K.WriteChunk(KwTanks(), ChunkPath(tag, "Tanks"));
            K.WriteChunk(KwGrainTerminal(), ChunkPath(tag, "GrainTerminal"));
            K.WriteChunk(KwBeach(), ChunkPath(tag, "Beach"));
            K.WriteChunk(KwTrench(), ChunkPath(tag, "Trench"));
            K.WriteChunk(KwExit(), ChunkPath(tag, "Exit"));
            GameObject far = K.SaveFarScenery(Pf(tag, "FAR_Kwinana_Horizon"), "FAR_Kwinana_Horizon", KwinanaFar());

            // ---- patterns (drive on the left: same-way traffic x -2, oncoming x +2 facing the rider)
            var patterns = new List<HazardPattern>
            {
                K.Pattern(patternFolder, "HZ_Kwinana_TruckAhead", 80, 0, 1f, false, 40, (truckAhead, -2f, 40f, 0f)),
                K.Pattern(patternFolder, "HZ_Kwinana_TankerOncoming", 90, 1, 1f, false, 50, (tanker, 2f, 55f, 180f)),
                K.Pattern(patternFolder, "HZ_Kwinana_TruckAndTanker", 110, 2, .8f, false, 50, (truckAhead, -2f, 35f, 0f), (tanker, 2f, 80f, 180f)),
                K.Pattern(patternFolder, "HZ_Kwinana_Seagulls", 45, 0, .8f, true, 30, (gull, -2.2f, 22f, 0f), (gull, -.4f, 23.5f, 40f), (gull, 1.4f, 25.5f, -30f), (gull, 5.5f, 27f, 160f)),
                K.Pattern(patternFolder, "HZ_Kwinana_Yard", 50, 1, .8f, true, 30, (pallets.gameObject, -2.6f, 20f, 0f), (drums.gameObject, 0f, 26f, 0f), (barrier.gameObject, 2.6f, 32f, 0f)),
            };

            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme == null) throw new InvalidOperationException("No comic theme at " + ComicCityBuilder.ThemePath);
            District d = LoadDistrict(KwinanaPath);
            d.id = KwinanaId; d.displayName = "KWINANA"; d.order = 120; d.length = new Vector2(360f, 440f);
            d.chunks = KwinanaChunks.Select(c => Chunk(tag, c)).ToArray();
            d.orderedChunks = true;
            d.entry = Chunk(tag, "Entry");
            d.exit = Chunk(tag, "Exit");
            d.allowBoulevard = false;
            d.obstacles = new[] { truck, barrier, pallets, drums };
            d.jumpObstacles = new[] { barrier, pallets, drums };
            d.gifts = Array.Empty<Gift>();
            var hazards = new List<HazardPattern>(StreetPatterns(theme, "HZ_RoadworksShort", "HZ_RoadworksZone", "HZ_PoliceStop", "HZ_SlowCar", "HZ_OncomingCar", "HZ_SidewalkStroll"));
            hazards.AddRange(patterns);
            d.hazards = hazards.ToArray();
            var people = new List<GameObject>(K.People(theme, "Courier", "Courier", "Office", "Jogger", "DogWalker", "Surfer", "Elder", "Student"));
            people.Add(gullWalking); people.Add(gullWalking); people.Add(gull);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { dock };
            d.rampFrequency = 1f;
            d.blendMeters = 150f;
            // Hazy, warm industrial light with a little smoke in the air; the sea glints beyond it.
            d.fogColor = new Color(.86f, .80f, .70f);
            d.skyTint = new Color(1f, .95f, .86f);
            d.ambientSky = new Color(.72f, .77f, .82f);
            d.ambientEquator = new Color(.88f, .80f, .68f);
            d.ambientGround = new Color(.64f, .52f, .42f);
            d.sunColor = new Color(1f, .9f, .76f);
            d.sunIntensity = 1.3f;
            d.farScenery = far;
            // gateway, ambience, ambienceVolume, skyYaw, skySize and skyHeight belong to the district system: never
            // written here (LoadDistrict sets the sky placeholders once, when it creates the asset).
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            K.WriteReport("art-src/districts/kwinana/catalog.txt");
            Debug.Log("[Districts/Kwinana] built " + (KwinanaChunks.Length + 2) + " chunks: " + string.Join("; ", K.Report));
            if (!atlas) return;
            ComicMesh Make(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }
            K.RenderAtlas("art-src/districts/kwinana/props-atlas.png", new List<(string, GameObject, ComicMesh)>
            {
                ("Truck", truck.gameObject, null),
                ("Tanker", tanker, null),
                ("WaterBarrier", barrier.gameObject, null),
                ("Pallets", pallets.gameObject, null),
                ("OilDrums", drums.gameObject, null),
                ("Seagull", gull, null),
                ("StorageTank", null, Make(m => Tank(m, Vector3.zero, 5f, 7f, TankWhite, TankBand, true))),
                ("RefineryColumn", null, Make(m => Column(m, Vector3.zero, 1.6f, 18f, 2))),
                ("FlareStack", null, Make(m => FlareStack(m, Vector3.zero, 22f))),
                ("Chimney", null, Make(m => Chimney(m, Vector3.zero, 26f, 1.4f, true))),
                ("FenceAndSign", null, Make(m => FenceSample(m))),
                ("PipeRack", null, Make(m => PipeRackSample(m))),
                ("GrainTerminal", null, Make(m => GrainTerminalAtlas(m))),
                ("Wreck", null, Make(m => Wreck(m, Vector3.zero, 0f))),
                ("HopperWagon", null, Make(m => Hopper(m, Vector3.zero))),
                ("Container", null, Make(m => Container(m, V(0, 0, 0), H("C8452F")))),
                ("NorfolkPine", null, Make(m => NorfolkPine(m, Vector3.zero, 11f))),
                ("LoadingDockRamp", dock.gameObject, null),
            });
        }

        // ---------------------------------------------------------------- Kwinana street pieces

        // (z0, z1) pieces of at most `step` metres along a chunk, leaving out [skipFrom, skipTo].
        static IEnumerable<(float, float)> Pieces(float length, float step, float skipFrom = -1f, float skipTo = -1f)
        {
            var cuts = new List<float>();
            for (float z = 0; z < length - .01f; z += step) cuts.Add(z);
            cuts.Add(length);
            if (skipFrom >= 0f) { cuts.Add(skipFrom); cuts.Add(skipTo); }
            cuts = cuts.Distinct().OrderBy(z => z).ToList();
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float a = cuts[i], b = cuts[i + 1];
                if (b - a < .01f || skipFrom >= 0f && a >= skipFrom - .01f && b <= skipTo + .01f) continue;
                yield return (a, b);
            }
        }

        /// <summary>The industrial road: asphalt with the dashed double yellow, a concrete footpath by the kerb and a
        /// red-earth verge on the outer half of each sidewalk.</summary>
        static void KwinanaStreet(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f)
        {
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, Concrete, KerbCap, true, skipFrom, skipTo);
            foreach ((float a, float b) in Pieces(d.Length, 4f, skipFrom, skipTo))
                for (int s = -1; s <= 1; s += 2)
                {
                    K.Flat(d.Road, s * (K.Edge - 2.1f), s * K.Edge, s * (K.Edge - 2.1f), s * K.Edge, a, b, K.Kerb + .03f, RedEarth);
                    K.Flat(d.Road, s * (K.Edge - 2.15f), s * (K.Edge - 2.05f), s * (K.Edge - 2.15f), s * (K.Edge - 2.05f), a, b, K.Kerb + .032f, RedEarthDark);
                }
        }

        /// <summary>Ground beyond the sidewalk on side s, from `from` to `to` metres past the sidewalk edge.</summary>
        static void Ground(CqChunkDraft d, int s, float z0, float z1, float from, float to, Color c, float top = K.Kerb - .02f, ComicSurface surface = ComicSurface.Concrete)
        {
            d.Road.Surface = surface;
            K.SideBox(d.Road, s, z0, z1, -2.4f, top, from, to, c);
        }

        /// <summary>A few flat patches (red earth, tyre marks, scrub) on the ground x0..x1 metres beyond the sidewalk
        /// edge, flat road space (v0.6: measured from the sidewalk edge; from the centre line some lay over the road).</summary>
        static void Patches(CqChunkDraft d, int s, float z0, float z1, float x0, float x1, float y, Color a, Color b, int count, int seed)
        {
            var r = new System.Random(seed);
            float F() => (float)r.NextDouble();
            d.Road.Surface = ComicSurface.Plain;
            for (int i = 0; i < count; i++)
            {
                float x = K.Edge + Mathf.Lerp(x0, x1, F()), z = Mathf.Lerp(z0 + 1f, z1 - 3f, F()), w = 1.5f + F() * 3f, l = 1.5f + F() * 2.5f;
                K.Flat(d.Road, s * x, s * (x + w), s * (x + w * .2f), s * (x + w * .9f), z, Mathf.Min(z1, z + l), y + i * .001f, i % 2 == 0 ? a : b);
            }
        }

        /// <summary>Chain-link fence with barbed wire on the line x (flat road space, posts on the ground beside the
        /// sidewalk): posts and rails with the outline, the diamond wire mesh on the road mesh (thin, no outline), yellow
        /// warning diamonds every 12 m.</summary>
        static void ChainFence(CqChunkDraft d, int s, float z0, float z1, float x, bool signs = true, float h = 2.4f)
        {
            ComicMesh m = d.SlopedFrontage, lat = d.Road;
            float y = K.Kerb - .02f;
            int bays = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / 3f));
            float bw = (z1 - z0) / bays;
            m.Surface = ComicSurface.Metal;
            for (int i = 0; i <= bays; i++)
            {
                float z = z0 + i * bw;
                m.Box(V(s * x, y + h * .5f, z), V(.1f, h, .1f), FenceDark);
                m.Beam(V(s * x, y + h - .05f, z), V(s * (x + .42f), y + h + .38f, z), .05f, FenceDark);
            }
            for (int i = 0; i < bays; i++)
            {
                float a = z0 + i * bw, b = a + bw;
                K.MinMaxBox(m, V(s * x - .04f, y + h - .07f, a), V(s * x + .04f, y + h + .01f, b), FenceGrey);
                K.MinMaxBox(m, V(s * x - .02f, y + .06f, a), V(s * x + .02f, y + .12f, b), FenceGrey);
                for (int k = 1; k <= 3; k++)
                {
                    float wx = s * (x + .13f * k), wy = y + h + .12f * k;
                    K.MinMaxBox(m, V(wx - .012f, wy - .012f, a), V(wx + .012f, wy + .012f, b), FenceDark);
                }
                Wires(lat, s, s * (x - .03f), y + .12f, y + h - .07f, a, b);
            }
            if (!signs) return;
            for (float z = z0 + 5f; z < z1 - 2f; z += 12f) WarningSign(m, s, x - .07f, y + 1.45f, z);
        }

        // Diagonal wires both ways, clipped to the bay, facing the road.
        static void Wires(ComicMesh lat, int s, float xf, float y0, float y1, float a, float b)
        {
            lat.Surface = ComicSurface.Plain;
            float hgt = y1 - y0, w = .034f;   // v0.6: thicker and wider apart (0.044 m wires every 0.55 m shimmered far away)
            Vector3 n = new Vector3(-s, 0, 0);
            for (int dir = -1; dir <= 1; dir += 2)
                for (float zs = a - hgt; zs < b; zs += .92f)
                {
                    // the line z = zs + (y - y0) (dir +1) or z = zs + hgt - (y - y0) (dir -1), clipped to [a, b]
                    float ya = y0, yb = y1;
                    float ZAt(float yy) => dir > 0 ? zs + (yy - y0) : zs + hgt - (yy - y0);
                    float za = ZAt(ya), zb = ZAt(yb);
                    if (Mathf.Max(za, zb) < a || Mathf.Min(za, zb) > b) continue;
                    float Clip(float zz) => Mathf.Clamp(zz, a, b);
                    float YAt(float zz) => dir > 0 ? y0 + (zz - zs) : y0 + (zs + hgt - zz);
                    float ca = Clip(za), cb = Clip(zb);
                    ya = YAt(ca); yb = YAt(cb);
                    if (Mathf.Abs(cb - ca) < .05f) continue;
                    Vector3 p = V(xf, ya, ca), q = V(xf, yb, cb), off = V(0, w, -w * dir);
                    K.Face(lat, p - off, q - off, q + off, p + off, Wire, n, false);
                }
        }

        static void WarningSign(ComicMesh m, int s, float x, float y, float z)
        {
            m.Surface = ComicSurface.PaintedMetal;
            Quaternion diamond = Quaternion.Euler(45f, 0, 0);
            m.Box(V(s * x, y, z), V(.04f, .66f, .66f), K.R.ink, diamond);
            m.Box(V(s * (x - .025f), y, z), V(.03f, .56f, .56f), K.R.yellow, diamond);
            m.Box(V(s * (x - .045f), y + .07f, z), V(.02f, .26f, .07f), K.R.ink);   // "!"
            m.Box(V(s * (x - .045f), y - .14f, z), V(.02f, .07f, .07f), K.R.ink);
        }

        /// <summary>Low limestone block wall on line x (flat road space): blocks of two tones with a cream cap.</summary>
        static void LimestoneWall(ComicMesh m, int s, float z0, float z1, float x, float h)
        {
            m.Surface = ComicSurface.Concrete;
            int i = 0;
            for (float z = z0; z < z1 - .01f; z += 1.6f, i++)
            {
                float e = Mathf.Min(z1, z + 1.6f);
                K.MinMaxBox(m, V(s * (x - .25f), K.Kerb - .1f, z), V(s * (x + .25f), K.Kerb + h, e - .03f), i % 3 == 1 ? LimestoneDark : Limestone);
            }
            for (float z = z0; z < z1 - .01f; z += 4f)
                K.MinMaxBox(m, V(s * (x - .3f), K.Kerb + h, z), V(s * (x + .3f), K.Kerb + h + .1f, Mathf.Min(z1, z + 4f)), RetCap);
        }

        /// <summary>Coastal scrub beyond a low limestone wall: rounded shrubs, banksias, grass trees and a few tuarts.</summary>
        static void CoastalScrub(CqChunkDraft d, int s, float z0, float z1, int seed, bool wall = true, bool trees = true, float near = 1.5f)
        {
            var r = new System.Random(seed);
            float F() => (float)r.NextDouble();
            if (wall) LimestoneWall(d.SlopedFrontage, s, z0, z1, K.Edge + .4f, .5f);
            int i = 0;
            for (float z = z0 + 1.2f; z < z1 - 1f; z += 2.6f + F() * 1.6f, i++)
            {
                float x = K.Edge + near + F() * 7f;
                Vector3 p = V(s * x, d.Ground(z) - .05f, z);
                if (i % 4 == 1) P.Banksia(d.Frontage, p, .8f + F() * .3f, seed + i);
                else if (i % 4 == 3) P.Balga(d.Frontage, p, .8f + F() * .2f, seed + i);
                else P.Shrub(d.Frontage, p, 1f + F() * .5f, Color.Lerp(P.GumLeafDark, Scrub, F() * .5f));
            }
            if (!trees) return;
            for (float z = z0 + 4f; z < z1 - 2f; z += 11f + F() * 4f)
                P.Gum(d.Frontage, V(s * (K.Edge + 8f + F() * 5f), d.Ground(z) - .05f, z), 9f + F() * 3f, seed * 7 + (int)z);
        }

        // ---------------------------------------------------------------- industrial props (upright, absolute y)

        /// <summary>Storage tank: 12-sided white shell, shallow cone roof, ink rim, colour band, concrete ring footing and
        /// a stair winding up the side facing the road.</summary>
        static void Tank(ComicMesh m, Vector3 p, float r, float h, Color body, Color band, bool stairs, int roadSide = -1)
        {
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(p + V(0, .2f, 0), r + .5f, .4f, Concrete, 12);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, h * .5f + .4f, 0), r, h, body, 12);
            m.Cylinder(p + V(0, h + .4f + r * .07f, 0), r, r * .14f, Color.Lerp(body, SiloShade, .5f), 12, .3f);
            m.Cylinder(p + V(0, h + .25f, 0), r + .07f, .3f, K.R.ink, 12);
            m.Cylinder(p + V(0, h * .32f + .4f, 0), r + .04f, h * .13f, band, 12);
            if (!stairs) return;
            m.Surface = ComicSurface.Metal;
            float a0 = roadSide < 0 ? 200f : -20f;
            for (int i = 0; i < 5; i++)
            {
                float a = (a0 + i * 22f) * Mathf.Deg2Rad, b = (a0 + (i + 1) * 22f) * Mathf.Deg2Rad;
                Vector3 pa = p + V(Mathf.Cos(a) * (r + .35f), .5f + h * i / 5f, Mathf.Sin(a) * (r + .35f));
                Vector3 pb = p + V(Mathf.Cos(b) * (r + .35f), .5f + h * (i + 1) / 5f, Mathf.Sin(b) * (r + .35f));
                m.Beam(pa, pb, .22f, SteelDark);
                m.Beam(pa + V(0, 1f, 0), pb + V(0, 1f, 0), .07f, K.R.yellow);
            }
        }

        /// <summary>Refinery column: tall 10-sided silver tower, ink platforms, a caged ladder, dome cap and a vapour pipe.</summary>
        static void Column(ComicMesh m, Vector3 p, float r, float h, int platforms)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, .4f, 0), V(r * 2.6f, .8f, r * 2.6f), Concrete);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(p + V(0, h * .5f + .8f, 0), r, h, PipeSilver, 10);
            m.Cylinder(p + V(0, h + .8f + r * .3f, 0), r, r * .6f, PipeSilver, 10, .45f);
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 1; i <= platforms; i++)
            {
                float y = .8f + h * i / (platforms + 1f);
                m.Cylinder(p + V(0, y, 0), r + .8f, .16f, SteelDark, 10);
                m.Cylinder(p + V(0, y + .55f, 0), r + .82f, .06f, K.R.yellow, 10);
            }
            m.Box(p + V(-r - .45f, h * .5f + .8f, 0), V(.12f, h, .5f), SteelDark);                      // ladder cage
            m.Beam(p + V(.2f, h + 1.2f, 0), p + V(r + 1.6f, h + .4f, 0), .4f, PipeSilver);               // vapour line
            m.Beam(p + V(r + 1.6f, h + .4f, 0), p + V(r + 1.6f, .8f, 0), .4f, PipeSilver);
        }

        /// <summary>Flare stack: a three-leg lattice tower round a red-and-white banded pipe, with a flame at the top.</summary>
        static void FlareStack(ComicMesh m, Vector3 p, float h)
        {
            m.Surface = ComicSurface.PaintedMetal;
            float spread = h * .09f;
            Vector3 top = p + V(0, h, 0);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad, b = (i + 1) * 120f * Mathf.Deg2Rad;
                Vector3 foot = p + V(Mathf.Cos(a) * spread, 0, Mathf.Sin(a) * spread), next = p + V(Mathf.Cos(b) * spread, 0, Mathf.Sin(b) * spread);
                m.Beam(foot, top - V(0, 1.5f, 0), .3f, SteelDark);
                for (int k = 1; k < 4; k++)
                {
                    float t = k / 4f;
                    m.Beam(Vector3.Lerp(foot, top, t), Vector3.Lerp(next, top, t), .12f, SteelDark);
                }
            }
            int bands = 6;
            for (int i = 0; i < bands; i++)
            {
                float y0 = h * i / bands, y1 = h * (i + 1) / bands;
                m.Cylinder(p + V(0, (y0 + y1) * .5f, 0), .55f, y1 - y0, i % 2 == 0 ? FlareRed : FlareWhite, 8);
            }
            m.Cylinder(top + V(0, .3f, 0), .7f, .6f, K.R.ink, 8);
            m.Surface = ComicSurface.Plain;
            m.Ellipsoid(top + V(.2f, 2.2f, 0), V(2.2f, 4.2f, 2.2f), Flame);
            m.Ellipsoid(top + V(.1f, 1.7f, 0), V(1.2f, 2.6f, 1.2f), FlameCore);
            m.Ellipsoid(top + V(.9f, 4.4f, 0), V(1.1f, 1.8f, 1.1f), Flame);
        }

        /// <summary>Tall concrete chimney with red-and-white bands at the top and a drifting smoke puff.</summary>
        static void Chimney(ComicMesh m, Vector3 p, float h, float r, bool smoke)
        {
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(p + V(0, h * .4f, 0), r, h * .8f, Concrete, 10, .85f);
            for (int i = 0; i < 4; i++)
            {
                float y0 = h * .8f + h * .05f * i;
                m.Surface = ComicSurface.PaintedMetal;
                m.Cylinder(p + V(0, y0 + h * .025f, 0), r * .85f, h * .05f, i % 2 == 0 ? FlareRed : FlareWhite, 10);
            }
            m.Cylinder(p + V(0, h + .15f, 0), r * .9f, .3f, K.R.ink, 10);
            if (!smoke) return;
            m.Surface = ComicSurface.Plain;
            m.Ellipsoid(p + V(1.5f, h + 2.5f, 0), V(3.6f, 2.8f, 3.2f), Smoke);
            m.Ellipsoid(p + V(4.5f, h + 4.2f, .5f), V(4.6f, 3.2f, 3.8f), Color.Lerp(Smoke, FlareWhite, .5f));
            m.Ellipsoid(p + V(8.5f, h + 5.4f, 1f), V(5f, 3f, 4f), Color.Lerp(Smoke, H("CFCBC2"), .5f));
        }

        /// <summary>LPG sphere on legs.</summary>
        static void Sphere(ComicMesh m, Vector3 p, float r)
        {
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                m.Box(p + V(Mathf.Cos(a) * r * .8f, r * .55f, Mathf.Sin(a) * r * .8f), V(.35f, r * 1.1f, .35f), SteelDark);
            }
            m.Ellipsoid(p + V(0, r * 1.5f, 0), V(r * 2f, r * 2f, r * 2f), TankWhite);
            m.Cylinder(p + V(0, r * 1.5f, 0), r * 1.01f, .25f, K.R.ink, 10);
        }

        /// <summary>A pipe rack along the road (flat road space, follows the slope): portal frames every 6 m and four
        /// coloured pipes on top.</summary>
        static void PipeRack(ComicMesh m, int s, float z0, float z1, float xc, float h)
        {
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = z0; z <= z1 + .01f; z += 6f)
            {
                foreach (float dx in new[] { -1.5f, 1.5f }) m.Box(V(s * (xc + dx), K.Kerb + h * .5f, z), V(.3f, h, .3f), SteelDark);
                m.Box(V(s * xc, K.Kerb + h, z), V(3.4f, .3f, .3f), SteelDark);
                m.Box(V(s * xc, K.Kerb + h - 1.6f, z), V(3.4f, .25f, .25f), SteelDark);
            }
            (float dx, float r, Color c, float lift)[] pipes = { (-1.1f, .32f, PipeSilver, 0f), (-.35f, .24f, PipeYellow, 0f), (.35f, .4f, PipeSilver, 0f), (1.1f, .22f, PipeGreen, 0f), (-.7f, .2f, PipeRed, -1.6f), (.6f, .28f, PipeSilver, -1.6f) };
            m.Surface = ComicSurface.Metal;
            foreach (var pipe in pipes)
                for (float z = z0; z < z1 - .01f; z += 8f)
                {
                    float e = Mathf.Min(z1, z + 8f);
                    m.Cylinder(V(s * (xc + pipe.dx), K.Kerb + h + pipe.lift + .15f + pipe.r, (z + e) * .5f), pipe.r, e - z, pipe.c, 6, 1f, Quaternion.Euler(90f, 0, 0));
                }
        }

        static void FenceSample(ComicMesh m)
        {
            var d = new CqChunkDraft("Atlas", 0f, 9f);
            ChainFence(d, 1, 0, 9, 0f, true);
            m.Add(d.SlopedFrontage, Vector3.zero, Quaternion.identity, Vector3.one);
            m.Add(d.Road, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        static void PipeRackSample(ComicMesh m) => PipeRack(m, 1, 0, 12, 0f, 4.5f);

        /// <summary>A low industrial building (control room, workshop): render walls, dark window band, a roof with AC units.</summary>
        static void Workshop(CqChunkDraft d, CqLot l, float h, Color wall, Color trim)
        {
            ComicMesh m = d.Frontage;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, l.W, -1.5f, .2f, 0, -l.Depth, K.R.ink);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, l.W, .2f, h, -.1f, -l.Depth, wall);
            m.Surface = ComicSurface.Glass;
            K.LBox(m, l, .8f, l.W - .8f, h * .42f, h * .72f, 0f, -.4f, GlassDark);
            m.Surface = ComicSurface.PaintedMetal;
            for (float a = .8f; a < l.W - .5f; a += 2.4f) K.LBox(m, l, a - .06f, a + .06f, h * .42f, h * .72f, .02f, -.3f, trim);
            K.LBox(m, l, -.2f, l.W + .2f, h, h + .35f, .3f, -l.Depth - .2f, trim);
            K.LBox(m, l, l.W * .25f, l.W * .25f + 2f, h + .35f, h + 1.2f, -3f, -4.6f, K.R.cream);
            K.LBox(m, l, l.W * .6f, l.W * .6f + 1.4f, h + .35f, h + 1f, -5f, -6.2f, K.R.cream);
            K.LBox(m, l, l.W - 3f, l.W - 1.6f, .2f, 2.4f, .02f, -.2f, trim);                              // door
            d.Wall(l, h);
        }

        // ---------------------------------------------------------------- Kwinana chunks

        static void IndustrialLamps(CqChunkDraft d, float z0 = 6f, float z1 = 40f)
        {
            for (float z = z0; z < z1; z += 20f) K.Lamp(d.Props, d.Collisions, z < 20f ? -1 : 1, d.Ground(z), z);
        }

        // =====================================================================================================
        // v0.6 boundaries (docs/v0.6-polish.md, rules 3, 4 and 7): what runs alongside carries across the boundary,
        // and the surface changes inside the district's own entry / exit chunk, never on the boundary line.
        // =====================================================================================================

        /// <summary>Where the surface changes in the boundary chunks (metres into the chunk).</summary>
        const float KwPathEnd = 14f, KwExitPlainFrom = 26f, WlExitVergeEnd = 30f;
        /// <summary>The Mandurah line beside the road from Kwinana's exit to Wellard station: track centre and mast line,
        /// metres beyond the sidewalk edge (the same numbers as the station chunk).</summary>
        const float RailOff = 25f, MastOff = 28f;

        // Aubin Grove's shared path and freeway side (CockburnAubinBuilder.SharedPath / FreewaySide), copied so the
        // corridor meets it at z = 0. SouthLineKit will own these.
        static readonly Color PspFootpath = H("CFCAC0"), PspDryGrass = H("C9B77A"), PspLawnDry = H("A7A85A"), PspBallast = H("8E8578"), PspRail = H("5C6066"),
            PspSleeper = H("6A5A4A"), PspBarrier = H("DAD7CF"), PspNoiseWall = H("D7C3A0"), PspNoiseWallDark = H("B89F78"), PspFence = H("2C3238"), PspMast = H("8E959D");

        /// <summary>How far the freeway has swung away from the path at z (0 at the boundary, 26 m at the chunk's end).</summary>
        static float KwFreewayOffset(float z) => 26f * Mathf.Pow(Mathf.Clamp01((z - 4f) / 36f), 1.5f);

        /// <summary>
        /// v0.6: THE PIECE TO SWAP FOR SouthLineKit (the shared freeway corridor). The left (-X) side of Kwinana's entry
        /// chunk, z = 0..40, and the shared-path surface of its first 14 m.
        /// At z = 0 it is Aubin Grove's freeway side as built today (CockburnAubinBuilder.FreewaySide): dry verge with the
        /// black path fence 4.5 m beyond the sidewalk, the bank down to the southbound lanes (|x| 15..26.5, 0.9 m below
        /// the path), jersey barriers, the Mandurah line in the median (tracks at |x| 29.8 and 34.2, masts, wires), the
        /// northbound lanes (37.5..49) and the far noise wall at 52. From z = 4 the freeway swings away to the left
        /// (KwFreewayOffset) and a bush-covered bank rises in the widening wedge between it and the path, so the freeway
        /// leaves the picture inside this chunk and the industrial road carries on alone.
        /// </summary>
        static void KwEntryFreewayCorridor(CqChunkDraft d)
        {
            const float step = 2f, L = 40f;
            ComicMesh road = d.Road;
            float Off(float z) => KwFreewayOffset(z);
            // the freeway's level below the path: 0.9 m at the boundary (Aubin Grove's), dipping to 5 m where it goes
            // under the yard at the end of the chunk
            float Yf(float z) => -.9f - 4.1f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6f, L, z));
            // a strip of the freeway cross-section between a and b metres from the centre line (before the offset),
            // dy above the freeway's level
            void Strip(float a, float b, float dy, Color c, ComicSurface surface, float z0 = 0f, float z1 = L)
            {
                road.Surface = surface;
                for (float z = z0; z < z1 - .01f; z += step)
                {
                    float e = Mathf.Min(z1, z + step);
                    K.Face(road, V(-(a + Off(z)), Yf(z) + dy, z), V(-(a + Off(e)), Yf(e) + dy, e), V(-(b + Off(e)), Yf(e) + dy, e), V(-(b + Off(z)), Yf(z) + dy, z), c, Vector3.up, false);
                }
            }
            // ---- the shared path's surface (Aubin Grove's), up to the give-way line where it joins the industrial road
            K.StreetSurface(road, L, K.R.asphalt, PspFootpath, K.R.cream, false, KwPathEnd, L);
            foreach (float z in new[] { 1f, 7f }) K.Flat(road, -.07f, .07f, -.07f, .07f, z, z + 3f, .03f, FlareWhite);
            // ---- the path's own verge and black fence (straight, with the path), to where the yard fence takes over
            road.Surface = ComicSurface.Foliage;
            K.SideBox(road, -1, 0f, L, -2.4f, K.Kerb - .03f, 0f, 4.4f, PspDryGrass);
            road.Surface = ComicSurface.Plain;
            for (int i = 0; i < 4; i++) K.Flat(road, -(K.Edge + .6f + i * .9f), -(K.Edge + 2.4f + i * .5f), -(K.Edge + .9f + i * .9f), -(K.Edge + 2f + i * .5f), 2f + i * 4.6f, 4.4f + i * 4.6f, K.Kerb - .025f, i % 2 == 0 ? PspLawnDry : Sand);
            PathFence(d.SlopedFrontage, -1, K.Edge + 4.5f, 0f, 20f);
            P.Banksia(d.Props, V(-(K.Edge + 2.4f), d.Ground(5f) - .03f, 5f), .75f, 61);
            P.Balga(d.Props, V(-(K.Edge + 3f), d.Ground(15f) - .03f, 15f), .9f, 62);
            // ---- the wedge between the path and the freeway: the dry verge, from z = 14 the first yard's gravel next
            // to the path, and beyond it a bank rising under bush that hides the freeway as it leaves
            float inner = K.Edge + 4.4f, top = K.Kerb - .03f;
            float Yard(float z) => Mathf.Min(Off(z) * .6f, 13f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(12f, 22f, z)));
            float Rise(float z) => 3f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8f, 26f, z)) * Mathf.Clamp01((Off(z) - Yard(z) - 1.5f) / 5f);
            float WedgeY(float v, float z)
            {
                float yard = Yard(z), w = Off(z) - yard;
                return v <= yard || w < .01f ? top : top + Rise(z) * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Clamp01((v - yard) / w))), .8f);   // Max: sin(pi) is a hair below 0, and Pow of that is NaN
            }
            float Across(int i, float z) => i <= 2 ? Yard(z) * i / 2f : Yard(z) + (Off(z) - Yard(z)) * (i - 2) / 5f;
            for (float z = 4f; z < L - .01f; z += step)
            {
                float e = z + step;
                for (int i = 0; i < 7; i++)
                {
                    float a0 = Across(i, z), a1 = Across(i + 1, z), b0 = Across(i, e), b1 = Across(i + 1, e);
                    Vector3 a = V(-(inner + a0), WedgeY(a0, z), z), b = V(-(inner + b0), WedgeY(b0, e), e);
                    Vector3 c = V(-(inner + b1), WedgeY(b1, e), e), dd = V(-(inner + a1), WedgeY(a1, z), z);
                    Color col = i < 2 ? Color.Lerp(PspDryGrass, Gravel, Mathf.InverseLerp(14f, 22f, z))
                        : Color.Lerp(Color.Lerp(PspDryGrass, Scrub, Mathf.Clamp01(z / 16f)), P.GumLeafDark, Rise(z) > .4f ? .2f + .12f * ((i + (int)(z / step)) % 2) : 0f);
                    road.Surface = i < 2 && z >= 18f ? ComicSurface.Concrete : ComicSurface.Foliage;
                    K.Face(road, a, b, c, dd, col, Vector3.up, false);
                }
            }
            for (int i = 0; i < 8; i++)
            {
                float z = 14f + i * 3.4f, w = Off(z) - Yard(z), v = Yard(z) + w * (.4f + .25f * ((i * 7) % 5) / 4f);
                if (w < 2.5f) continue;
                var p = V(-(inner + v), d.Y(z) + WedgeY(v, z) - .1f, z);
                if (i % 3 == 0) P.Gum(d.Frontage, p, 9f + (i % 4), 300 + i);
                else if (i % 3 == 1) P.Banksia(d.Frontage, p, .9f + .1f * (i % 3), 310 + i);
                else P.Shrub(d.Frontage, p, 1.5f + .3f * (i % 2), Color.Lerp(P.GumLeafDark, Scrub, .3f));
            }
            // ---- the freeway, swinging away and dipping: batter, southbound lanes, median railway, northbound lanes,
            // the far verge with its noise wall and the far batter back up to the bush
            road.Surface = ComicSurface.Foliage;
            for (float z = 0f; z < L - .01f; z += step)
            {
                float e = z + step;
                K.Face(road, V(-(inner + Off(z)), top, z), V(-(inner + Off(e)), top, e), V(-(K.Edge + 7f + Off(e)), Yf(e), e), V(-(K.Edge + 7f + Off(z)), Yf(z), z), PspDryGrass, Vector3.up, false);
                K.Face(road, V(-(62f + Off(z)), Yf(z), z), V(-(62f + Off(e)), Yf(e), e), V(-(69f + Off(e)), top, e), V(-(69f + Off(z)), top, z), PspDryGrass, Vector3.up, false);
                K.Face(road, V(-(69f + Off(z)), top, z), V(-(69f + Off(e)), top, e), V(-102f, top, e), V(-102f, top, z), Color.Lerp(PspDryGrass, Scrub, .6f), Vector3.up, false);
            }
            Strip(K.Edge + 7f, 15f, 0f, PspLawnDry, ComicSurface.Foliage);
            foreach ((float a, float b) in new[] { (15f, 26.5f), (37.5f, 49f) })
            {
                Strip(a, b, 0f, K.R.asphalt, ComicSurface.Asphalt);
                Strip(a + .4f, a + .55f, .02f, FlareWhite, ComicSurface.Plain);
                Strip(b - .55f, b - .4f, .02f, K.R.yellow, ComicSurface.Plain);
                for (float lane = a + 4f; lane < b - 2f; lane += 3.6f)
                    for (float z = 0f; z < L - 3f; z += 9f) Strip(lane - .07f, lane + .07f, .02f, FlareWhite, ComicSurface.Plain, z, z + 3f);
            }
            Strip(27.3f, 36.7f, -.05f, PspBallast, ComicSurface.Concrete);
            Strip(49f, 62f, 0f, PspDryGrass, ComicSurface.Foliage);
            ComicMesh f = d.SlopedFrontage;
            for (float z = 0f; z < L - .01f; z += 4f)
            {
                float e = z + 4f;
                f.Surface = ComicSurface.Concrete;
                foreach (float bx in new[] { 26.9f, 37.1f })
                {
                    f.Beam(V(-(bx + Off(z)), Yf(z) + .06f, z), V(-(bx + Off(e)), Yf(e) + .06f, e), .6f, PspBarrier);
                    f.Beam(V(-(bx + Off(z)), Yf(z) + .58f, z), V(-(bx + Off(e)), Yf(e) + .58f, e), .3f, PspBarrier);
                }
                foreach (float tc in new[] { 29.8f, 34.2f })
                {
                    road.Surface = ComicSurface.Metal;
                    foreach (float rx in new[] { tc - .72f, tc + .72f }) road.Beam(V(-(rx + Off(z)), Yf(z) + .09f, z), V(-(rx + Off(e)), Yf(e) + .09f, e), .12f, PspRail);
                    road.Beam(V(-(tc + Off(z)), Yf(z) + 5.93f, z), V(-(tc + Off(e)), Yf(e) + 5.93f, e), .06f, K.R.ink);
                }
            }
            foreach (float tc in new[] { 29.8f, 34.2f })
                for (float z = .45f; z < L - .3f; z += .9f) Strip(tc - 1.25f, tc + 1.25f, .02f, PspSleeper, ComicSurface.Plain, z, z + .24f);
            foreach (float z in new[] { 6f, 26f })
            {
                ComicMesh m = d.Props;
                m.Surface = ComicSurface.PaintedMetal;
                float by = d.Y(z) + Yf(z), o = Off(z);
                m.Box(V(-(27.6f + o), by + 3.3f, z), V(.3f, 6.6f, .3f), PspMast);
                m.Box(V(-(36.4f + o), by + 3.3f, z), V(.3f, 6.6f, .3f), PspMast);
                m.Box(V(-(32f + o), by + 6.4f, z), V(9f, .3f, .3f), PspMast);
            }
            // far noise wall (upright panels turned along the curve) and a few cars
            for (float z = 0f; z < L - .01f; z += 4f)
            {
                float zc = z + 2f, yaw = -Mathf.Atan2(Off(z + 4f) - Off(z), 4f) * Mathf.Rad2Deg, len = Mathf.Sqrt(16f + Mathf.Pow(Off(z + 4f) - Off(z), 2f));
                d.Deep.Surface = ComicSurface.Concrete;
                d.Deep.Box(V(-(52f + Off(zc)), d.Y(zc) + Yf(zc) + 1.9f, zc), V(.4f, 5.6f, len - .04f), (int)(z / 4f) % 2 == 0 ? PspNoiseWall : Color.Lerp(PspNoiseWall, K.R.cream, .3f), Quaternion.Euler(0, yaw, 0));
                d.Deep.Box(V(-(51.75f + Off(zc)), d.Y(zc) + Yf(zc) + 3.2f, zc), V(.1f, .9f, 2.2f), PspNoiseWallDark, Quaternion.Euler(0, yaw, 0));
            }
            (float lane, float z, bool south, Color c)[] cars = { (18.6f, 9f, true, H("C8452F")), (22.2f, 24f, true, H("D8DDE0")), (41.3f, 15f, false, H("2F5FA8")) };
            foreach (var car in cars)
            {
                float yaw = -Mathf.Atan2(Off(car.z + 2f) - Off(car.z - 2f), 4f) * Mathf.Rad2Deg + (car.south ? 0f : 180f);
                d.Landmark.Add(CarMesh(car.c), V(-(car.lane + Off(car.z)), d.Y(car.z) + Yf(car.z), car.z), Quaternion.Euler(0, yaw, 0), Vector3.one);
            }
            // ---- where the freeway leaves the chunk it goes under the yard: a concrete portal with three dark mouths
            float xe0 = inner + Off(L), xe1 = 69f + Off(L), ye = Yf(L);
            f.Surface = ComicSurface.Concrete;
            K.MinMaxBox(f, V(-xe0, ye - .3f, L - .35f), V(-xe1, K.Kerb + 1.2f, L), Concrete);
            K.MinMaxBox(f, V(-xe0 + .1f, K.Kerb + 1.2f, L - .45f), V(-xe1 - .1f, K.Kerb + 1.4f, L), SteelDark);
            f.Surface = ComicSurface.Plain;
            foreach ((float a, float b) in new[] { (15.2f, 26.4f), (27.5f, 36.5f), (37.6f, 48.8f) })
                K.Face(f, V(-(a + Off(L)), ye, L - .36f), V(-(b + Off(L)), ye, L - .36f), V(-(b + Off(L)), ye + 4.3f, L - .36f), V(-(a + Off(L)), ye + 4.3f, L - .36f), H("23282E"), Vector3.back, false);
        }

        /// <summary>The black path fence of the shared path (as CockburnAubinBuilder.Fence): posts every 2.5 m, top and
        /// bottom rails, two wires (flat road space).</summary>
        static void PathFence(ComicMesh m, int s, float x, float z0, float z1)
        {
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = z0 + .3f; z < z1; z += 2.5f) m.Box(V(s * x, K.Kerb + .8f, z), V(.09f, 1.7f, .09f), PspFence);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                foreach (float y in new[] { K.Kerb + 1.55f, K.Kerb + .15f }) K.MinMaxBox(m, V(s * x - .04f, y - .04f, z), V(s * x + .04f, y + .04f, e), PspFence);
                foreach (float y in new[] { K.Kerb + .65f, K.Kerb + 1.1f }) K.MinMaxBox(m, V(s * x - .015f, y - .015f, z), V(s * x + .015f, y + .015f, e), PspMast);
            }
        }

        /// <summary>A band across the whole cross-section where the surface changes (a flush limestone kerb strip on the
        /// road and over the sidewalks), so the change reads as made on purpose.</summary>
        internal static void SurfaceBand(ComicMesh road, float z, float width, Color c)
        {
            road.Surface = ComicSurface.Plain;
            K.Flat(road, -K.Half, K.Half, -K.Half, K.Half, z, z + width, .036f, c);
            for (int s = -1; s <= 1; s += 2) K.Flat(road, s * K.Half, s * K.Edge, s * K.Half, s * K.Edge, z, z + width, K.Kerb + .045f, c);
        }

        /// <summary>A row of white give-way triangles across the road just before z (points towards the rider).</summary>
        internal static void GiveWayTeeth(ComicMesh road, float z)
        {
            road.Surface = ComicSurface.Plain;
            for (float x = -K.Half + .5f; x < K.Half - .9f; x += 1f)
                K.TriFacing(road, V(x, .034f, z), V(x + .7f, .034f, z), V(x + .35f, .034f, z - .9f), FlareWhite, Vector3.up);
        }

        /// <summary>A small round sign on a post beyond the sidewalk edge (decoration, no collider): blue shared-path
        /// disc; ends: with a red slash.</summary>
        static void PathSign(ComicMesh m, Vector3 p, bool ends)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 1.2f, 0), .05f, 2.4f, Steel, 6);
            m.Cylinder(p + V(0, 2.5f, -.04f), .42f, .06f, K.R.ink, 12, 1f, Quaternion.Euler(90, 0, 0));
            m.Cylinder(p + V(0, 2.5f, -.08f), .36f, .06f, TerminalBlue, 12, 1f, Quaternion.Euler(90, 0, 0));
            m.Box(p + V(0, 2.5f, -.12f), V(.34f, .08f, .03f), FlareWhite);
            m.Box(p + V(-.1f, 2.62f, -.12f), V(.08f, .2f, .03f), FlareWhite);
            m.Box(p + V(.12f, 2.38f, -.12f), V(.08f, .2f, .03f), FlareWhite);
            if (ends) m.Box(p + V(0, 2.5f, -.14f), V(.86f, .1f, .03f), FlareRed, Quaternion.Euler(0, 0, 45f));
        }

        static CqChunkDraft KwEntry()
        {
            // From Aubin Grove: the shared path beside the Kwinana Freeway and the Mandurah line carries on across the
            // boundary (left); the freeway swings away behind a bush bank and the path joins the industrial road at a
            // give-way line. Right: coastal bush behind a limestone wall, then the first yard fence and tank.
            var d = new CqChunkDraft("Kwinana_Entry", KwinanaDrop);
            KwEntryFreewayCorridor(d);
            KwinanaStreet(d, 0f, KwPathEnd);
            GiveWayTeeth(d.Road, KwPathEnd - .3f);
            SurfaceBand(d.Road, KwPathEnd, .5f, KerbCap);
            foreach (int s in new[] { -1, 1 }) PathSign(d.Props, V(s * (K.Edge + .45f), d.Ground(KwPathEnd - 1f), KwPathEnd - 1f), true);
            Ground(d, 1, 0, 20, 0, 72, Scrub, K.Kerb - .02f, ComicSurface.Foliage);
            Ground(d, 1, 20, 40, 0, 72, Gravel);
            Patches(d, 1, 20, 40, 3f, 40f, K.Kerb, RedEarth, RedEarthDark, 5, 12);
            Patches(d, -1, 22, 40, .5f, 3f, K.Kerb - .02f, RedEarth, Gravel, 3, 10);
            CoastalScrub(d, 1, 0, 19, 121, true, true);
            ChainFence(d, 1, 21, 40, K.Edge + .6f, true);
            ChainFence(d, -1, 21, 40, K.Edge + .6f, false);
            Tank(d.Frontage, V(-19f, d.Base(34f), 34f), 4.5f, 8f, TankWhite, TankBand, true, 1);
            Tank(d.Frontage, V(21f, d.Base(34f), 34f), 5f, 7f, TankCream, H("B5603A"), true, -1);
            Chimney(d.Deep, V(46f, d.Base(36f), 36f), 44f, 1.8f, true);
            K.Lamp(d.Props, d.Collisions, -1, d.Ground(30f), 30f);
            d.StandardSlots();
            return d;
        }

        /// <summary>Railway beside the road on side s, track centre x(z) metres from the centre line (flat road space):
        /// the same ballast, timber sleepers and rails as Track (the station), following a curve; masts with the contact
        /// wire at the given z; the black line fence 3.5 m inside the track.</summary>
        static void RailLine(CqChunkDraft d, int s, Func<float, float> x, float z0, float z1, float fenceFrom, params float[] masts)
        {
            ComicMesh road = d.Road, m = d.SlopedFrontage;
            const float step = 2f;
            for (float z = z0; z < z1 - .01f; z += step)
            {
                float e = Mathf.Min(z1, z + step), xa = x(z), xb = x(e);
                road.Surface = ComicSurface.Concrete;
                K.Face(road, V(s * (xa - 1.7f), K.Kerb + .12f, z), V(s * (xb - 1.7f), K.Kerb + .12f, e), V(s * (xb + 1.7f), K.Kerb + .12f, e), V(s * (xa + 1.7f), K.Kerb + .12f, z), H("8F887C"), Vector3.up, false);
                K.Face(road, V(s * (xa - 1.7f), K.Kerb - .1f, z), V(s * (xb - 1.7f), K.Kerb - .1f, e), V(s * (xb - 1.7f), K.Kerb + .12f, e), V(s * (xa - 1.7f), K.Kerb + .12f, z), H("8F887C"), new Vector3(-s, 0, 0), false);
                m.Surface = ComicSurface.Metal;
                foreach (float dx in new[] { -.72f, .72f }) m.Beam(V(s * (xa + dx), K.Kerb + .3f, z), V(s * (xb + dx), K.Kerb + .3f, e), .11f, H("8A9198"));
                m.Beam(V(s * xa, K.Kerb + 5.62f, z), V(s * xb, K.Kerb + 5.62f, e), .05f, K.R.ink);
                if (z >= fenceFrom)
                {
                    m.Surface = ComicSurface.PaintedMetal;
                    foreach (float fy in new[] { K.Kerb + 1.55f, K.Kerb + .15f }) m.Beam(V(s * (xa - 3.5f), fy, z), V(s * (xb - 3.5f), fy, e), .08f, PspFence);
                    m.Box(V(s * (xa - 3.5f), K.Kerb + .8f, z), V(.09f, 1.7f, .09f), PspFence);
                }
            }
            m.Surface = ComicSurface.Wood;
            for (float z = z0 + .4f; z < z1; z += .8f)
            {
                float yaw = Mathf.Atan2(s * (x(z + .4f) - x(z - .4f)), .8f) * Mathf.Rad2Deg;
                m.Box(V(s * x(z), K.Kerb + .18f, z), V(2.4f, .12f, .24f), H("6B5A48"), Quaternion.Euler(0, yaw, 0));
            }
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in masts)
            {
                float xm = x(z) + (MastOff - RailOff);
                m.Box(V(s * xm, K.Kerb + 3.4f, z), V(.3f, 6.8f, .3f), H("8A9198"));
                m.Beam(V(s * xm, K.Kerb + 6.2f, z), V(s * (xm - 4.6f), K.Kerb + 6.2f, z), .15f, H("8A9198"));
                m.Beam(V(s * xm, K.Kerb + 5.2f, z), V(s * (xm - 4f), K.Kerb + 6.1f, z), .1f, H("8A9198"));
            }
        }

        /// <summary>Kwinana's exit: where the Mandurah line swings in beside the road (it has left the freeway).</summary>
        static float KwExitRailX(float z) => K.Edge + RailOff + 12f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 20f, z)));

        static CqChunkDraft KwExit()
        {
            // The last yards, then bush behind limestone walls. Left: the Mandurah line, having left the freeway, swings
            // in beside the road and runs on across the boundary to Wellard station (same track, masts and fence there).
            // The industrial road's red verge and centre line end at a band 14 m before the boundary.
            var d = new CqChunkDraft("Kwinana_Exit", KwinanaDrop);
            KwinanaStreet(d, KwExitPlainFrom, 40f);
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, Pave, KerbCap, false, 0f, KwExitPlainFrom);
            SurfaceBand(d.Road, KwExitPlainFrom - .5f, .5f, KerbCap);
            for (int s = -1; s <= 1; s += 2)
            {
                Ground(d, s, 0, 20, 0, 72, Gravel);
                Ground(d, s, 20, 40, 0, 72, Scrub, K.Kerb - .02f, ComicSurface.Foliage);
                ChainFence(d, s, 0, 19, K.Edge + .6f, s < 0);
                CoastalScrub(d, s, 21, 40, 140 + s, true, s > 0);
                YoungTree(d.Props, d.Collisions, V(s * (K.Edge - .9f), d.Ground(31f), 31f), 31 + s);
            }
            RailLine(d, -1, KwExitRailX, 0f, 40f, 8f, 2f, 22f);
            // a railcar on its way to Wellard station (the track is straight from z = 20): what makes the line read from the road
            TrainCar(d.Landmark, V(-(K.Edge + RailOff), d.Ground(30f) + .3f, 30f), true);
            Tank(d.Frontage, V(-21f, d.Base(8f), 8f), 5.5f, 8f, TankWhite, TankBand, true, 1);
            Column(d.Frontage, V(23f, d.Base(7f), 7f), 1.6f, 20f, 2);
            Chimney(d.Deep, V(-64f, d.Base(4f), 4f), 40f, 1.6f, true);
            K.Lamp(d.Props, d.Collisions, 1, d.Ground(10f), 10f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft KwRefinery()
        {
            var d = new CqChunkDraft("Kwinana_Refinery", KwinanaDrop);
            KwinanaStreet(d);
            for (int s = -1; s <= 1; s += 2)
            {
                Ground(d, s, 0, 40, 0, 72, Gravel);
                Patches(d, s, 0, 40, 2f, 50f, K.Kerb, RedEarth, H("B9AD90"), 7, 31 + s);
                ChainFence(d, s, 0, 40, K.Edge + .6f, true);
            }
            // left: the pipe rack along the fence, a control room, columns, a process unit frame, the flare
            PipeRack(d.SlopedFrontage, -1, 0, 40, K.Edge + 4.2f, 5f);
            Workshop(d, new CqLot(-1, 26, 39, K.Edge + 7.5f, 9, d.Base(32f)), 4.2f, K.R.cream, H("3E6E8E"));
            Column(d.Landmark, V(-21f, d.Base(8f), 8f), 2.2f, 30f, 3);
            Column(d.Landmark, V(-27f, d.Base(15f), 15f), 1.5f, 22f, 2);
            Column(d.Landmark, V(-23f, d.Base(22f), 22f), 2.8f, 18f, 2);
            ProcessFrame(d.Landmark, V(-34f, d.Base(8f), 8f), 8f, 10f, 16f);
            FlareStack(d.Landmark, V(-40f, d.Base(32f), 32f), 40f);
            // right: tanks, an LPG sphere, a low pipe manifold
            Tank(d.Frontage, V(20f, d.Base(10f), 10f), 7f, 10f, TankWhite, TankBand, true, -1);
            Tank(d.Frontage, V(22f, d.Base(31f), 31f), 6f, 12f, TankGreen, H("F2F0EA"), true, -1);
            Sphere(d.Frontage, V(35f, d.Base(20f), 20f), 4.5f);
            PipeRack(d.SlopedFrontage, 1, 2, 38, K.Edge + 3.2f, 1.6f);
            Chimney(d.Deep, V(-58f, d.Base(12f), 12f), 62f, 2.2f, true);
            Tank(d.Deep, V(48f, d.Base(6f), 6f), 9f, 11f, TankWhite, TankBand, false);
            Tank(d.Deep, V(52f, d.Base(30f), 30f), 8f, 10f, TankGreen, TankWhite, false);
            IndustrialLamps(d);
            d.StandardSlots();
            return d;
        }

        /// <summary>A steel process-unit frame: four columns, three decks, braces, and pipes and vessels inside.</summary>
        static void ProcessFrame(ComicMesh m, Vector3 p, float w, float dep, float h)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -w * .5f, w * .5f })
                foreach (float z in new[] { 0f, dep })
                    m.Box(p + V(x, h * .5f, z), V(.35f, h, .35f), SteelDark);
            for (int i = 1; i <= 3; i++)
            {
                float y = h * i / 3f;
                m.Box(p + V(0, y, dep * .5f), V(w + .3f, .25f, dep + .3f), i == 3 ? SteelDark : Color.Lerp(SteelDark, Steel, .4f));
                m.Box(p + V(0, y + .55f, -.05f), V(w, .06f, .06f), K.R.yellow);
            }
            m.Beam(p + V(-w * .5f, 0, 0), p + V(w * .5f, h / 3f, 0), .18f, SteelDark);
            m.Beam(p + V(w * .5f, h / 3f, 0), p + V(-w * .5f, h * 2f / 3f, 0), .18f, SteelDark);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(p + V(-1.5f, h * .5f + 1f, dep * .4f), 1.2f, 6f, PipeSilver, 8);
            m.Cylinder(p + V(1.8f, h / 3f + 1.4f, dep * .6f), .9f, 3.6f, PipeYellow, 8, 1f, Quaternion.Euler(0, 0, 90));
            m.Cylinder(p + V(0, h * 2f / 3f + .6f, dep * .5f), .35f, dep, PipeSilver, 6, 1f, Quaternion.Euler(90, 0, 0));
        }

        static CqChunkDraft KwTanks()
        {
            var d = new CqChunkDraft("Kwinana_Tanks", KwinanaDrop);
            KwinanaStreet(d);
            for (int s = -1; s <= 1; s += 2)
            {
                Ground(d, s, 0, 40, 0, 72, Gravel);
                ChainFence(d, s, 0, 40, K.Edge + .6f, true);
                // red-earth bund walls round the tank farm
                d.SlopedFrontage.Surface = ComicSurface.Concrete;
                K.SideBox(d.SlopedFrontage, s, 2, 38, K.Kerb - .05f, K.Kerb + .8f, 3.2f, 4.2f, RedEarth);
                Patches(d, s, 0, 40, 5f, 60f, K.Kerb, RedEarth, RedEarthDark, 6, 51 + s);
            }
            Tank(d.Frontage, V(-21f, d.Base(9f), 9f), 7f, 9f, TankWhite, TankBand, true, 1);
            Tank(d.Frontage, V(-35f, d.Base(24f), 24f), 9f, 12f, TankGreen, H("F2F0EA"), false);
            Tank(d.Frontage, V(-19f, d.Base(31f), 31f), 5.5f, 8f, TankCream, TankBand, true, 1);
            Tank(d.Frontage, V(22f, d.Base(26f), 26f), 8f, 11f, TankWhite, TankBand, true, -1);
            Tank(d.Frontage, V(39f, d.Base(10f), 10f), 7f, 9f, TankCream, H("B5603A"), false);
            // tanker loading bay on the right: a canopy on posts with a parked tanker trailer under it
            ComicMesh m = d.Frontage;
            float b = d.Base(9f);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { 13.5f, 19.5f })
                foreach (float z in new[] { 3f, 15f }) m.Box(V(x, b + 2.9f, z), V(.35f, 5.8f, .35f), SteelDark);
            m.Box(V(16.5f, b + 6f, 9f), V(7.4f, .4f, 13.4f), K.R.yellow);
            m.Box(V(16.5f, b + 6.3f, 9f), V(7.6f, .2f, 13.6f), K.R.ink);
            m.Box(V(16.5f, b + .08f, 9f), V(6f, .16f, 12f), Concrete);
            m.Add(TankerMesh(), V(16.5f, b + .16f, 9f), Quaternion.identity, Vector3.one);
            Chimney(d.Deep, V(-56f, d.Base(34f), 34f), 56f, 2f, true);
            FlareStack(d.Deep, V(52f, d.Base(36f), 36f), 34f);
            IndustrialLamps(d);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft KwGrainTerminal()
        {
            var d = new CqChunkDraft("Kwinana_GrainTerminal", KwinanaDrop);
            KwinanaStreet(d);
            // left: the grain loop railway with a train of hopper wagons, coastal scrub behind
            Ground(d, -1, 0, 40, 0, 72, Scrub, K.Kerb - .02f, ComicSurface.Foliage);
            ChainFence(d, -1, 0, 40, K.Edge + .6f, true);
            Track(d, -1, K.Edge + 4.5f);
            Locomotive(d.Frontage, V(-(K.Edge + 4.5f), d.Ground(4f) + .25f, 6f));
            for (int i = 0; i < 2; i++) Hopper(d.Frontage, V(-(K.Edge + 4.5f), d.Ground(17f + i * 13f) + .25f, 17f + i * 13f));
            CoastalScrub(d, -1, 0, 40, 77, false, true, 8f);
            // right: the terminal behind its fence, ground out to the coast
            Ground(d, 1, 0, 40, 0, 74, Gravel);
            Patches(d, 1, 0, 40, 2f, 12f, K.Kerb, H("B9AD90"), RedEarth, 4, 61);
            ChainFence(d, 1, 0, 40, K.Edge + .6f, true);
            GrainTerminal(d);
            IndustrialLamps(d);
            d.StandardSlots();
            return d;
        }

        /// <summary>Railway track along the road (flat road space): ballast, sleepers, two rails.</summary>
        static void Track(CqChunkDraft d, int s, float x)
        {
            d.Road.Surface = ComicSurface.Concrete;
            K.SideBox(d.Road, s, 0, d.Length, -1f, K.Kerb + .12f, x - K.Edge - 1.7f, x - K.Edge + 1.7f, H("8F887C"));
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Wood;
            for (float z = .4f; z < d.Length; z += .8f) m.Box(V(s * x, K.Kerb + .18f, z), V(2.4f, .12f, .24f), H("6B5A48"));
            m.Surface = ComicSurface.Metal;
            for (float z = 0; z < d.Length - .01f; z += 4f)
                foreach (float dx in new[] { -.72f, .72f }) K.MinMaxBox(m, V(s * x + dx - .05f, K.Kerb + .24f, z), V(s * x + dx + .05f, K.Kerb + .36f, z + 4f), H("8A9198"));
        }

        /// <summary>Grain hopper wagon (no brand): ochre body with sloped ends, ink frame and ribs, bogies.</summary>
        static void Hopper(ComicMesh m, Vector3 p)
        {
            Color body = H("D8B65A"), dark = Color.Lerp(body, K.R.ink, .35f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 2.4f, 0), V(2.9f, 2.6f, 9.4f), body);
            m.Box(p + V(0, 3.75f, 0), V(3f, .14f, 9.5f), K.R.ink);
            m.Box(p + V(0, 3.9f, 0), V(.8f, .2f, 9f), dark);                              // roof hatch strip
            for (float z = -4f; z <= 4.01f; z += 1.6f) m.Box(p + V(0, 2.4f, z), V(3f, 2.4f, .12f), dark);
            m.Box(p + V(0, 1.05f, -2.6f), V(2.2f, .9f, 2.2f), dark, Quaternion.Euler(45f, 0, 0));
            m.Box(p + V(0, 1.05f, 2.6f), V(2.2f, .9f, 2.2f), dark, Quaternion.Euler(45f, 0, 0));
            m.Box(p + V(0, .95f, 0), V(2.6f, .3f, 10.2f), K.R.ink);                       // frame
            Bogies(m, p, 3.6f);
        }

        static void Bogies(ComicMesh m, Vector3 p, float z)
        {
            m.Surface = ComicSurface.Metal;
            foreach (float bz in new[] { -z, z })
            {
                m.Box(p + V(0, .55f, bz), V(2.3f, .45f, 2.4f), H("33393F"));
                foreach (float wz in new[] { -.75f, .75f })
                    foreach (int s in new[] { -1, 1 }) m.Cylinder(p + V(s * .9f, .45f, bz + wz), .45f, .2f, K.R.ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            }
        }

        /// <summary>Diesel freight locomotive (no brand): yellow cab and long hood with a blue band and ink trim.</summary>
        static void Locomotive(ComicMesh m, Vector3 p)
        {
            Color body = H("F3B23A"), band = TerminalBlue;
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 1.05f, 0), V(2.8f, .45f, 11f), K.R.ink);
            m.Box(p + V(0, 2.55f, -1f), V(2.3f, 2.6f, 8f), body);                         // long hood
            m.Box(p + V(0, 2.15f, -1f), V(2.34f, .45f, 8.02f), band);
            m.Box(p + V(0, 2.9f, 3.95f), V(2.9f, 3.3f, 2.4f), body);                      // cab
            m.Box(p + V(0, 2.3f, 3.95f), V(2.94f, .45f, 2.44f), band);
            m.Box(p + V(0, 4.62f, 3.95f), V(3f, .16f, 2.5f), K.R.ink);
            m.Surface = ComicSurface.Glass;
            m.Box(p + V(0, 3.75f, 5.16f), V(2.3f, .8f, .06f), GlassDark);
            foreach (int s in new[] { -1, 1 }) m.Box(p + V(s * 1.46f, 3.75f, 3.95f), V(.05f, .7f, 1.4f), GlassDark);
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = -4.5f; z < 2.5f; z += 1.2f) m.Box(p + V(0, 3.2f, z), V(2.36f, .9f, .1f), Color.Lerp(body, K.R.ink, .25f));
            m.Box(p + V(0, 1.6f, 5.4f), V(2.8f, .6f, .3f), K.R.yellow);
            m.Box(p + V(0, 4.1f, -3.5f), V(1f, .5f, 1.6f), K.R.ink);                      // exhaust hatch
            Bogies(m, p, 3.8f);
        }

        // The terminal: two rows of tall white silos either side of the blue-and-white striped head house, blue
        // galleries on top, a penthouse, and the blue conveyor gallery running out over the coast to the jetty.
        static void GrainTerminal(CqChunkDraft d)
        {
            ComicMesh m = d.Landmark;
            float b = d.Base(20f) - .2f;
            GrainTerminalAt(m, V(13f, b, 0), true);   // set back from the fence so its silhouette reads from the road (v0.6: 6 m further)
        }

        static void GrainTerminalAtlas(ComicMesh m) => GrainTerminalAt(m, V(-27f, 0, -20f), false);

        static void GrainTerminalAt(ComicMesh m, Vector3 o, bool conveyor)
        {
            const float siloR = 3.2f, siloH = 25f, hhH = 52f;
            m.Surface = ComicSurface.Concrete;
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 6; i++)
                {
                    float z = 3.3f + i * 6.4f;
                    if (z > 12f && z < 28f) continue;                         // the head house stands there
                    m.Cylinder(o + V(22.6f + row * 6.4f, siloH * .5f, z), siloR, siloH, row == 0 ? SiloWhite : SiloShade, 10);
                }
            for (int i = 0; i < 2; i++)
            {
                float z0 = i == 0 ? 0f : 27.6f, z1 = i == 0 ? 12.4f : 40f;
                m.Surface = ComicSurface.PaintedMetal;
                K.MinMaxBox(m, o + V(19.6f, siloH, z0), o + V(32.6f, siloH + 3f, z1), TerminalBlue);
                K.MinMaxBox(m, o + V(19.5f, siloH + 3f, z0 - .1f), o + V(32.7f, siloH + 3.3f, z1 + .1f), K.R.ink);
                K.MinMaxBox(m, o + V(19.55f, siloH - .25f, z0), o + V(32.65f, siloH, z1), TerminalBlueDark);
            }
            // head house: white box, blue vertical stripes on the faces the rider sees, blue crown, penthouse
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, o + V(19f, 0, 13f), o + V(34f, hhH, 27f), SiloWhite);
            // blue ribs standing proud of the white walls (real relief, not paint), floor bands, a stair tower
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = 13.5f; z < 26.6f; z += 1.8f) K.MinMaxBox(m, o + V(18.6f, 1f, z), o + V(19.02f, hhH - 1.5f, z + .9f), TerminalBlue);
            for (float x = 19.5f; x < 33.6f; x += 1.8f) K.MinMaxBox(m, o + V(x, 1f, 12.6f), o + V(x + .9f, hhH - 1.5f, 13.02f), TerminalBlue);
            for (float y = 9f; y < hhH - 3f; y += 9f)
            {
                K.MinMaxBox(m, o + V(18.45f, y, 12.45f), o + V(19.1f, y + .35f, 27.2f), TerminalBlueDark);
                K.MinMaxBox(m, o + V(18.45f, y, 12.45f), o + V(34.2f, y + .35f, 13.1f), TerminalBlueDark);
            }
            K.MinMaxBox(m, o + V(18.4f, 0, 12.4f), o + V(34.4f, 1f, 27.6f), TerminalBlueDark);                 // plinth
            K.MinMaxBox(m, o + V(18.5f, hhH - 1.5f, 12.5f), o + V(34.5f, hhH, 27.5f), TerminalBlue);           // crown
            K.MinMaxBox(m, o + V(18.3f, hhH, 12.3f), o + V(34.7f, hhH + .3f, 27.7f), K.R.ink);
            K.MinMaxBox(m, o + V(30.5f, 0, 9.4f), o + V(34f, hhH - 6f, 12.4f), SiloShade);                      // stair tower
            for (float y = 3f; y < hhH - 8f; y += 6f)
            {
                m.Surface = ComicSurface.Glass;
                K.MinMaxBox(m, o + V(31.4f, y, 9.3f), o + V(33.1f, y + 1.2f, 9.45f), GlassDark);
            }
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, o + V(30.3f, hhH - 6f, 9.2f), o + V(34.2f, hhH - 5.6f, 12.5f), TerminalBlue);
            // ink plinth rings round the silos
            m.Surface = ComicSurface.Concrete;
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 6; i++)
                {
                    float z = 3.3f + i * 6.4f;
                    if (z > 12f && z < 28f) continue;
                    m.Cylinder(o + V(22.6f + row * 6.4f, .6f, z), siloR + .15f, 1.2f, Concrete, 10);
                }
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, o + V(23f, hhH + .3f, 16f), o + V(30f, hhH + 4f, 24f), SiloWhite);
            m.Surface = ComicSurface.Glass;
            K.Face(m, o + V(22.98f, hhH + 1.6f, 16.5f), o + V(22.98f, hhH + 1.6f, 23.5f), o + V(22.98f, hhH + 3.2f, 23.5f), o + V(22.98f, hhH + 3.2f, 16.5f), GlassDark, Vector3.left, false);
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, o + V(22.6f, hhH + 4f, 15.6f), o + V(30.4f, hhH + 4.5f, 24.4f), TerminalBlue);
            m.Box(o + V(28f, hhH + 6f, 22f), V(.15f, 3f, .15f), K.R.ink);
            if (!conveyor) return;
            // conveyor gallery out to the jetty over the coast (beyond the far shore line it stands in the sound)
            Vector3 start = o + V(34f, hhH - 8f, 20f), end = o + V(190f, 17f, 20f);
            int n = 8;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = Vector3.Lerp(start, end, (float)i / n), c = Vector3.Lerp(start, end, (float)(i + 1) / n);
                m.Beam(a, c, 2.8f, TerminalBlue);
                m.Beam(a - V(0, 1.8f, 0), c - V(0, 1.8f, 0), .3f, K.R.ink);
                if (i == 0) continue;
                float footY = a.x > 82f ? o.y + SeaLevel - 1f : o.y;
                m.Beam(V(a.x, footY, a.z - 2.2f), a - V(0, 1.6f, 1f), .7f, Steel);
                m.Beam(V(a.x, footY, a.z + 2.2f), a - V(0, 1.6f, -1f), .7f, Steel);
            }
            // ship loader at the end of the jetty
            m.Box(end + V(4f, -2f, 0), V(6f, 4f, 10f), TerminalBlue);
            m.Beam(end + V(6f, 0, 0), end + V(22f, -6f, 0), 1.2f, TerminalBlue);
            m.Box(end + V(4f, -9f, 0), V(1f, 12f, 1f), Steel);
        }

        static CqChunkDraft KwBeach()
        {
            // The set-back chunk: Kwinana Beach. Right: a low limestone sea wall, white sand and the turquoise sound, the
            // rusty half-buried hull of the old ship on the beach. Left: lawn with Norfolk pines and a picnic shelter, dunes.
            var d = new CqChunkDraft("Kwinana_Beach", KwinanaDrop);
            KwinanaStreet(d);
            // right: beach and sea (flat road space)
            LimestoneWall(d.SlopedFrontage, 1, 0, 40, K.Edge + .35f, .45f);
            d.Road.Surface = ComicSurface.Concrete;
            K.SideBox(d.Road, 1, 0, 40, -2.4f, K.Kerb - .3f, .6f, 14f, Sand);
            K.SideBox(d.Road, 1, 0, 40, -2.4f, -.45f, 14f, 22f, Sand);
            K.SideBox(d.Road, 1, 0, 40, -2.4f, -.75f, 22f, 27f, SandWet);
            K.Water(d.Road, K.Edge + 25f, 100f, 0, 40, SeaLevel, Sea, Ripple, 71);
            d.Road.Surface = ComicSurface.Plain;
            for (float z = 0; z < 40f - .01f; z += 4f)
                K.Flat(d.Road, K.Edge + 24.6f, K.Edge + 25.4f, K.Edge + 24.8f, K.Edge + 25.6f, z, z + 4f, SeaLevel + .02f, Foam);
            Wreck(d.Landmark, V(K.Edge + 22f, d.Y(20f) - .55f, 20f), 12f);
            // beach umbrella and a boogie board on the sand
            ComicMesh p = d.Props;
            p.Surface = ComicSurface.PaintedMetal;
            p.Cylinder(V(K.Edge + 8f, d.Y(32f) - .2f + 1.1f, 32f), .05f, 2.2f, K.R.ink, 6);
            p.Surface = ComicSurface.Fabric;
            p.Cylinder(V(K.Edge + 8f, d.Y(32f) - .2f + 2.2f, 32f), 1.4f, .4f, K.R.red, 8, .08f);
            p.Cylinder(V(K.Edge + 8f, d.Y(32f) - .2f + 1.98f, 32f), 1.42f, .1f, FlareWhite, 8);
            p.Box(V(K.Edge + 6.6f, d.Y(30f) - .22f, 30f), V(.6f, .06f, 1.1f), K.R.yellow, Quaternion.Euler(0, 20, 0));
            // left: lawn reserve with Norfolk pines and a shelter, then the dunes
            Ground(d, -1, 0, 40, 0, 16, Lawn, K.Kerb - .02f, ComicSurface.Foliage);
            Ground(d, -1, 0, 40, 16, 72, Scrub, K.Kerb - .02f, ComicSurface.Foliage);
            NorfolkPine(d.Frontage, V(-(K.Edge + 3.5f), d.Ground(6f), 6f), 13f);
            NorfolkPine(d.Frontage, V(-(K.Edge + 4f), d.Ground(33f), 33f), 15f);
            Shelter(d.Frontage, V(-(K.Edge + 8f), d.Ground(19f), 19f));
            CoastalScrub(d, -1, 0, 40, 91, false, true, 9f);
            Dunes(d.Deep, d, -1, 0, 40, 30f, 60f, 93);
            K.Lamp(d.Props, d.Collisions, -1, d.Ground(26f), 26f);
            d.StandardSlots();
            return d;
        }

        static void Dunes(ComicMesh m, CqChunkDraft d, int s, float z0, float z1, float x0, float x1, int seed)
        {
            var r = new System.Random(seed);
            float F() => (float)r.NextDouble();
            m.Surface = ComicSurface.Foliage;
            for (float z = z0; z < z1; z += 7f + F() * 4f)
            {
                float x = Mathf.Lerp(x0, x1, F());
                m.Ellipsoid(V(s * x, d.Ground(z) + .5f, z), V(12f + F() * 6f, 4f + F() * 2f, 9f + F() * 4f), Color.Lerp(Scrub, P.GumLeafDark, F() * .4f));
            }
        }

        /// <summary>Norfolk Island pine (v0.6: Rockingham's tiered pine with frond pads, one tree for the whole coast;
        /// the old one here was a stack of cones).</summary>
        static void NorfolkPine(ComicMesh m, Vector3 p, float h) => RockinghamBuilder.NorfolkPine(m, p, h, Mathf.RoundToInt(Mathf.Abs(p.z) * 7f + h));

        /// <summary>Picnic shelter: limestone slab, four posts, a green hip roof, a table.</summary>
        static void Shelter(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, .08f, 0), V(5f, .16f, 5f), Limestone);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -2.1f, 2.1f })
                foreach (float z in new[] { -2.1f, 2.1f }) m.Box(p + V(x, 1.4f, z), V(.18f, 2.6f, .18f), FlareWhite);
            HipRoof(m, p.x - 2.8f, p.x + 2.8f, p.z - 2.8f, p.z + 2.8f, p.y + 2.7f, 1.3f, H("3F7A55"));
            m.Surface = ComicSurface.Wood;
            m.Box(p + V(0, .75f, 0), V(1f, .08f, 2f), H("A8774E"));
            m.Box(p + V(-.65f, .45f, 0), V(.3f, .06f, 2f), H("A8774E"));
            m.Box(p + V(.65f, .45f, 0), V(.3f, .06f, 2f), H("A8774E"));
        }

        /// <summary>The old ship's hull, half buried in the sand: a rust-red hull listing over, the bow raised with a post,
        /// broken ribs where the plates are gone, blotches, and a fallen stern section.</summary>
        static void Wreck(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(-5f, yaw, 7f);
            Vector3 W(float x, float y, float z) => p + q * V(x, y, z);
            void B(Vector3 c, Vector3 s, Color col) => m.Box(p + q * c, s, col, q);
            m.Surface = ComicSurface.Metal;
            B(V(0, .4f, -1f), V(5.4f, 3.2f, 15f), Rust);                                     // hull body (bottom under the sand)
            // the bow: a wedge rising to the stem
            Vector3 l0 = W(-2.7f, -1.2f, 6.5f), r0 = W(2.7f, -1.2f, 6.5f), l1 = W(-2.7f, 2.4f, 6.5f), r1 = W(2.7f, 2.4f, 6.5f);
            Vector3 tipLo = W(0, -1.2f, 11f), tipHi = W(0, 3.6f, 11.6f);
            K.Face(m, l0, tipLo, tipHi, l1, RustLight, q * V(-1, 0, .6f));
            K.Face(m, r0, tipLo, tipHi, r1, Rust, q * V(1, 0, .6f));
            K.TriFacing(m, l1, r1, tipHi, RustDark, q * Vector3.up);
            B(V(0, 2.5f, -1f), V(5.6f, .25f, 15.2f), RustDark);                               // gunwale
            B(V(0, 4.4f, 8.6f), V(.4f, 4.4f, .4f), RustDark);                                 // samson post
            B(V(0, 6.4f, 8.6f), V(2.2f, .3f, .3f), RustDark);
            B(V(0, 3.2f, 1.5f), V(3.4f, 1.6f, 3f), Rust);                                     // broken deck house
            B(V(-1.71f, 3.3f, 1.5f), V(.06f, .6f, 1.8f), K.R.ink);
            // missing plates at the stern: ink hole and ribs standing up
            B(V(-2.72f, 1f, -5f), V(.06f, 2f, 4.6f), K.R.ink);
            for (float z = -7.4f; z <= -2.6f; z += 1.2f)
            {
                B(V(-2.6f, 2.9f, z), V(.25f, 1.6f, .25f), RustDark);
                B(V(2.6f, 3.1f, z), V(.25f, 2f, .25f), RustDark);
            }
            var r = new System.Random(5);
            for (int i = 0; i < 7; i++)
                B(V(-2.73f, (float)r.NextDouble() * 2f - .2f, -6f + (float)r.NextDouble() * 11f), V(.06f, .5f + (float)r.NextDouble() * .5f, .8f + (float)r.NextDouble()), i % 2 == 0 ? RustLight : RustDark);
            // the stern section, broken off and sunk deeper
            Quaternion q2 = Quaternion.Euler(-12f, yaw - 14f, -10f);
            m.Box(p + q * V(.6f, 0, -11.5f), V(5f, 2.4f, 4.4f), Rust, q2);
            m.Box(p + q * V(.6f, 1.3f, -11.5f), V(5.2f, .2f, 4.6f), RustDark, q2);
        }

        static CqChunkDraft KwTrench()
        {
            // A loading-dock ramp across the whole road, then a 5 m pipe trench (WHOOPS!), then the road again. Left: a
            // warehouse with loading docks; right: stacked shipping containers behind the fence.
            var d = new CqChunkDraft("Kwinana_Trench", KwinanaDrop);
            float z0 = TrenchRampZ, lip = z0 + TrenchRise + TrenchTop, far = lip + TrenchGap;
            KwinanaStreet(d, lip, far);
            const float depth = -3f;
            for (int s = -1; s <= 1; s += 2)
            {
                Ground(d, s, 0, lip, 0, 72, Gravel);
                Ground(d, s, far, 40, 0, 72, Gravel);
                Ground(d, s, lip, far, 14f, 72, Gravel);                              // the trench ends 14 m past the sidewalk
                ChainFence(d, s, 0, lip - .5f, K.Edge + .6f, s > 0);
                ChainFence(d, s, far + .5f, 40, K.Edge + .6f, true);
            }
            // trench: walls, floor, pipes across, hazard edging
            ComicMesh t = d.Road;
            t.Surface = ComicSurface.Concrete;
            float w = K.Edge + 14f;
            K.MinMaxBox(t, V(-w, depth, lip - .35f), V(w, -.02f, lip), Concrete);
            K.MinMaxBox(t, V(-w, depth, far), V(w, -.02f, far + .35f), Concrete);
            K.MinMaxBox(t, V(-w, depth - .3f, lip), V(w, depth, far), H("7E7A72"));
            foreach (int s in new[] { -1, 1 }) K.MinMaxBox(t, V(s * w - .2f, depth, lip), V(s * w + .2f, K.Kerb - .02f, far), Concrete);
            K.Water(t, -w, w, lip, far, depth + .2f, H("4E6A64"), H("6E8A80"), 13);
            ComicMesh pm = d.SlopedFrontage;
            pm.Surface = ComicSurface.Metal;
            (float z, float y, float r, Color c)[] pipes = { (lip + 1.1f, depth + 1.1f, .55f, PipeSilver), (lip + 2.6f, depth + .9f, .45f, PipeYellow), (lip + 3.8f, depth + 1.4f, .35f, PipeGreen) };
            foreach (var pipe in pipes)
                for (float x = -(K.Edge + 4f); x < K.Edge + 4f - .01f; x += 6f)
                    pm.Cylinder(V(x + 3f, pipe.y, pipe.z), pipe.r, 6f, pipe.c, 8, 1f, Quaternion.Euler(0, 0, 90));
            // yellow and black edging along the far side of the trench (the ramp's lip has its own stripes)
            for (float x = -K.Edge; x < K.Edge - .01f; x += 1f)
                K.Flat(d.Road, x, x + .5f, x, x + .5f, far + .35f, far + .9f, .01f + (Mathf.Abs(x) >= K.Half ? K.Kerb : 0f), K.R.yellow);
            // barricades at the trench ends beyond the sidewalks
            foreach (int s in new[] { -1, 1 })
                for (float z = lip + .6f; z < far; z += 1.9f)
                {
                    pm.Surface = ComicSurface.PaintedMetal;
                    pm.Box(V(s * (K.Edge + .4f), K.Kerb + .7f, z), V(.12f, .5f, 1.6f), FlareWhite);
                    pm.Box(V(s * (K.Edge + .38f), K.Kerb + .7f, z), V(.1f, .2f, 1.62f), FlareRed);
                    pm.Box(V(s * (K.Edge + .4f), K.Kerb + .2f, z), V(.1f, .9f, .1f), K.R.ink);
                }
            var ramp = new CqRampSpec { Name = "LoadingDockRamp", X = 0f, Z = z0, Width = 2f * K.Edge, Rise = TrenchRise, Height = TrenchHeight, Top = TrenchTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(ramp);
            d.Gaps.Add(new CqGapSpec { Name = "PipeTrench", Z = lip, Length = TrenchGap, Width = 0f, Word = "WHOOPS", Water = false });
            var deck = new ComicMesh();
            K.RampMesh(deck, ramp, H("A9AEB2"), H("7D8288"), true, depth);
            // yellow chevrons up the slope, three across, so the dock ramp reads from far away
            float SlopeY(float zz) => TrenchHeight * (zz - z0) / TrenchRise + .02f;
            Vector3 slopeN = (Vector3.up + Vector3.back * (TrenchHeight / TrenchRise)).normalized;
            for (int i = 0; i < 2; i++)
            {
                float c = z0 + 1.1f + i * 2.4f;
                foreach (float off in new[] { -5.3f, 0f, 5.3f })
                    foreach (int s in new[] { -1, 1 })
                    {
                        Vector3 a = V(off + s * 1.8f, SlopeY(c), c), b = V(off, SlopeY(c + 1.3f), c + 1.3f);
                        Vector3 a2 = V(a.x, SlopeY(a.z + .6f), a.z + .6f), b2 = V(b.x, SlopeY(b.z + .6f), b.z + .6f);
                        K.Face(deck, a, b, b2, a2, K.R.yellow, slopeN, false);
                    }
            }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            d.KeepOut.Add(KeepOutBox(d, -1, z0, far)); d.KeepOut.Add(KeepOutBox(d, 1, z0, far));
            // left: warehouse with roller doors and a dock; right: containers
            Warehouse(d, new CqLot(-1, 2, 38, K.Edge + 6f, 18, d.Base(20f)));
            var colours = new[] { H("C8452F"), H("2F5FA8"), H("2F8A7A"), H("E07A2A"), H("8C9298"), H("6A4A8A") };
            int k = 0;
            for (float z = 2f; z < 38f; z += 6.6f, k++)
                for (int level = 0; level < (k % 3 == 1 ? 3 : 2); level++)
                    Container(d.Frontage, V(K.Edge + 6f + (k % 2) * .6f, d.Base(z + 3f) + level * 2.6f, z + 3f), colours[(k * 2 + level) % colours.Length]);
            for (float z = 2f; z < 38f; z += 6.6f)
                Container(d.Frontage, V(K.Edge + 9.4f, d.Base(z + 3f), z + 3f), colours[(int)z % colours.Length]);
            Chimney(d.Deep, V(-52f, d.Base(30f), 30f), 50f, 2f, true);
            d.GiftSlots.Add(new Vector2(0f, 28f));
            d.Slot(-2.6f, 36f); d.Slot(2.6f, 36f);
            return d;
        }

        static Bounds KeepOutBox(CqChunkDraft d, int s, float z0, float z1)
        {
            float yTop = d.Y(z0) + 5f, yBottom = d.Y(z1) - 4f;
            return new Bounds(new Vector3(s * (K.Half + K.Side * .5f + .3f), (yTop + yBottom) * .5f, (z0 + z1) * .5f), new Vector3(K.Side + .6f, yTop - yBottom, z1 - z0));
        }

        /// <summary>Shipping container (6 m along z): ribbed body, ink door frames and corner posts.</summary>
        static void Container(ComicMesh m, Vector3 p, Color c)
        {
            Color dark = Color.Lerp(c, K.R.ink, .35f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 1.3f, 0), V(2.44f, 2.56f, 6f), c);
            m.Box(p + V(0, 2.55f, 0), V(2.5f, .1f, 6.06f), dark);
            m.Box(p + V(0, .05f, 0), V(2.5f, .1f, 6.06f), dark);
            for (float z = -2.4f; z <= 2.41f; z += .8f) m.Box(p + V(0, 1.3f, z), V(2.48f, 2.4f, .1f), dark);
            m.Box(p + V(0, 1.3f, -3.01f), V(2.3f, 2.4f, .04f), dark);
        }

        static void Warehouse(CqChunkDraft d, CqLot l)
        {
            ComicMesh m = d.Frontage;
            Color wall = H("B9C3B5"), trim = H("3E6E5E"), door = H("8E959A");
            const float h = 8f;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, l.W, -1.5f, .2f, 0, -l.Depth, K.R.ink);
            K.LBox(m, l, 0, l.W, .2f, 1.3f, 2.6f, -.1f, Concrete);                                     // the dock
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, 0, l.W, .2f, h, 0f, -l.Depth, wall);
            for (float a = .5f; a < l.W; a += .9f) K.LQuad(m, l, a, a + .08f, .2f, h, .015f, Color.Lerp(wall, K.R.ink, .18f), false);
            for (int i = 0; i < 3; i++)
            {
                float a0 = 3f + i * 11f, a1 = a0 + 5f;
                K.LQuad(m, l, a0 - .2f, a1 + .2f, 1.2f, 6.4f, .02f, K.R.ink);
                K.LQuad(m, l, a0, a1, 1.3f, 6.2f, .04f, door);
                for (float y = 1.6f; y < 6.2f; y += .4f) K.LQuad(m, l, a0, a1, y, y + .06f, .05f, Color.Lerp(door, K.R.ink, .3f), false);
                foreach (float a in new[] { a0 - .5f, a1 + .5f }) K.LBox(m, l, a - .2f, a + .2f, .7f, 1.3f, 2.7f, 2.5f, K.R.ink);   // dock bumpers
                K.LBox(m, l, a0 + 1.5f, a1 - 1.5f, 6.8f, 7.4f, .05f, -.1f, K.R.yellow);                                         // dock light plate
            }
            K.LBox(m, l, -.2f, l.W + .2f, h - .4f, h, .25f, -.1f, trim);
            K.LSlope(m, l, -.3f, l.W + .3f, h, h + 1.6f, .25f, -l.Depth * .5f, -l.Depth - .3f, H("9AA39D"));
            // the site office at the far end: rendered block forward of the shed, window band with frames, an awning
            float oa = l.W - 4.6f;
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, oa, l.W, .2f, 4.2f, 1.4f, -.2f, K.R.cream);
            m.Surface = ComicSurface.Glass;
            K.LBox(m, l, oa + .4f, l.W - .4f, 1.4f, 3f, 1.42f, 1.2f, GlassDark);
            m.Surface = ComicSurface.PaintedMetal;
            for (float a = oa + 1.4f; a < l.W - .5f; a += 1.1f) K.LBox(m, l, a - .05f, a + .05f, 1.4f, 3f, 1.45f, 1.3f, trim);
            K.LBox(m, l, oa - .2f, l.W + .2f, 3.2f, 3.4f, 2.6f, 1.2f, trim);
            K.LBox(m, l, oa - .1f, l.W + .1f, 4.2f, 4.45f, 1.55f, -.3f, K.R.ink);
            // roof ventilators
            for (float a = 6f; a < l.W - 5f; a += 10f)
            {
                m.Surface = ComicSurface.Metal;
                Vector3 vp = l.P(a, h + 1.2f, -l.Depth * .5f);
                m.Cylinder(vp + V(0, .5f, 0), .45f, 1f, Steel, 8);
                m.Cylinder(vp + V(0, 1.1f, 0), .6f, .25f, SteelDark, 8, .6f);
            }
            d.Wall(l, h);
        }

        // ---------------------------------------------------------------- Kwinana obstacles and animals

        static void Wheels(ComicMesh m, float x, float r, float wdt, params float[] zs)
        {
            foreach (float z in zs)
                for (int s = -1; s <= 1; s += 2)
                {
                    m.Surface = ComicSurface.Rubber;
                    m.Cylinder(V(s * x, r, z), r, wdt, K.R.ink, 8, 1f, Quaternion.Euler(0, 0, 90));
                    m.Surface = ComicSurface.Metal;
                    m.Cylinder(V(s * (x + .02f), r, z), r * .45f, wdt + .02f, H("D1DBE5"), 8, 1f, Quaternion.Euler(0, 0, 90));
                }
        }

        static void Cab(ComicMesh m, float z, Color cab)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, 2.05f, z), V(2.45f, 2.3f, 1.9f), cab);
            m.Box(V(0, 3.25f, z - .05f), V(2.5f, .14f, 2f), K.R.ink);
            m.Box(V(0, 3.05f, z + 1.02f), V(2.4f, .14f, .32f), K.R.ink);                         // sun visor
            m.Box(V(0, 1.15f, z + .98f), V(2.5f, .5f, .2f), K.R.ink);                            // bumper
            m.Box(V(0, 1.7f, z + .97f), V(1.4f, .5f, .06f), Color.Lerp(cab, K.R.ink, .5f));      // grille
            foreach (int s in new[] { -1, 1 }) m.Box(V(s * .95f, 1.45f, z + .99f), V(.34f, .2f, .06f), K.R.cream);
            m.Surface = ComicSurface.Glass;
            m.Box(V(0, 2.5f, z + .96f), V(2.1f, .85f, .06f), GlassDark);
            foreach (int s in new[] { -1, 1 }) m.Box(V(s * 1.23f, 2.5f, z + .1f), V(.05f, .75f, 1.1f), GlassDark);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(V(1.05f, 3.2f, z - 1.15f), .1f, 2f, PipeSilver, 6);
            m.Box(V(0, .75f, z - 4f), V(1.6f, .3f, 9.5f), K.R.ink);                              // chassis
        }

        // Tipper truck (no brand), front at +Z: forward cab, steel bin with ribs and a heaped load, three axles.
        static ComicMesh TipperTruckMesh(Color cab, Color bin, Color load)
        {
            var m = new ComicMesh();
            Cab(m, 3.2f, cab);
            m.Surface = ComicSurface.PaintedMetal;
            Color dark = Color.Lerp(bin, K.R.ink, .35f);
            m.Box(V(0, 2.05f, -.9f), V(2.5f, 1.8f, 5.9f), bin);
            m.Box(V(0, 3f, -.9f), V(2.58f, .14f, 6f), dark);
            for (float z = -3.4f; z <= 1.6f; z += 1.25f) foreach (int s in new[] { -1, 1 }) m.Box(V(s * 1.28f, 2.05f, z), V(.08f, 1.7f, .14f), dark);
            m.Box(V(0, 2.05f, -3.88f), V(2.52f, 1.8f, .1f), dark);
            m.Surface = ComicSurface.Concrete;
            m.Ellipsoid(V(0, 3.05f, -.95f), V(2.2f, 1f, 5.4f), load);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (int s in new[] { -1, 1 }) m.Box(V(s * 1.15f, 1.25f, -2.3f), V(.3f, .12f, 2.2f), K.R.ink);       // mudguards
            Wheels(m, 1.08f, .52f, .42f, 3.2f, -1.7f, -3f);
            return m;
        }

        // Fuel tanker (no brand): cab and a silver tank with ink rings, a top walkway, a rear ladder and hose box.
        static ComicMesh TankerMesh()
        {
            var m = new ComicMesh();
            Cab(m, 4.7f, H("D9442E"));
            m.Surface = ComicSurface.Metal;
            m.Cylinder(V(0, 2.25f, -1.4f), 1.2f, 8.6f, PipeSilver, 12, 1f, Quaternion.Euler(90, 0, 0));
            for (float z = -5f; z <= 2.21f; z += 2.4f) m.Cylinder(V(0, 2.25f, z), 1.24f, .14f, K.R.ink, 12, 1f, Quaternion.Euler(90, 0, 0));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, 3.52f, -1.4f), V(.7f, .1f, 8f), K.R.yellow);
            for (float z = -5f; z <= 2.21f; z += 2.4f) m.Box(V(0, 3.48f, z + 1.2f), V(.5f, .14f, .5f), K.R.ink);   // hatches
            m.Box(V(0, 1.15f, -1.4f), V(1.4f, .5f, 3f), H("4A5056"));                                            // hose box
            m.Box(V(0, 2.2f, -5.75f), V(.5f, 2.2f, .1f), K.R.ink);                                               // ladder
            Wheels(m, 1.08f, .52f, .42f, 4.7f, 2.7f, -3.6f, -4.9f);
            return m;
        }

        // Plastic water-filled road barriers: one red, one white, chained by ink pins.
        static ComicMesh WaterBarrierMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            for (int i = 0; i < 2; i++)
            {
                float x = (i - .5f) * 1.06f;
                Color c = i == 0 ? FlareRed : FlareWhite;
                m.Box(V(x, .3f, 0), V(1f, .6f, .62f), c);
                m.Box(V(x, .72f, 0), V(.94f, .3f, .4f), c);
                m.Box(V(x, .92f, 0), V(.9f, .1f, .26f), Color.Lerp(c, K.R.ink, .2f));
                m.Box(V(x, .52f, -.32f), V(.6f, .14f, .03f), Color.Lerp(c, K.R.ink, .3f));
                m.Box(V(x, .05f, 0), V(1.02f, .1f, .66f), K.R.ink);
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(V(0, .5f, 0), .06f, .9f, K.R.ink, 6);
            return m;
        }

        // Two timber pallets and a shrink-wrapped load of sacks.
        static ComicMesh PalletsMesh()
        {
            var m = new ComicMesh();
            Color wood = H("C9A16E"), woodDark = H("9A7448");
            for (int k = 0; k < 2; k++)
            {
                float y = k * .16f;
                m.Surface = ComicSurface.Wood;
                foreach (float z in new[] { -.45f, 0f, .45f }) m.Box(V(0, y + .05f, z), V(1.2f, .1f, .12f), woodDark);
                for (float x = -.5f; x <= .51f; x += .25f) m.Box(V(x, y + .13f, 0), V(.16f, .04f, 1.05f), wood);
            }
            m.Surface = ComicSurface.Fabric;
            m.Box(V(0, .62f, 0), V(1.1f, .6f, 1f), H("E9E2CF"));
            m.Box(V(0, .62f, 0), V(1.12f, .1f, 1.02f), H("8DB3A4"));
            m.Box(V(0, .94f, 0), V(1f, .06f, .9f), H("D8D0BA"));
            return m;
        }

        // Three oil drums (blue, yellow, red) with ink rims and lids.
        static ComicMesh DrumsMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            (float x, float z, Color c)[] drums = { (-.42f, -.15f, TerminalBlue), (.4f, -.2f, K.R.yellow), (0f, .38f, FlareRed) };
            foreach (var dr in drums)
            {
                m.Cylinder(V(dr.x, .45f, dr.z), .3f, .9f, dr.c, 10);
                foreach (float y in new[] { .3f, .6f }) m.Cylinder(V(dr.x, y, dr.z), .315f, .05f, Color.Lerp(dr.c, K.R.ink, .4f), 10);
                m.Cylinder(V(dr.x, .89f, dr.z), .31f, .04f, K.R.ink, 10);
                m.Box(V(dr.x + .12f, .92f, dr.z), V(.08f, .03f, .08f), K.R.ink);
            }
            return m;
        }

        // Chunky silver gull: white body and head, grey wings with ink tips, yellow beak with a red spot, legs, feet.
        static ComicMesh SeagullMesh()
        {
            var a = new ComicMesh { Surface = ComicSurface.Plastic };
            Color white = FlareWhite, grey = H("AEB4BA"), yellow = H("F3B23A");
            a.Box(V(0, .34f, .02f), V(.3f, .26f, .52f), white);
            a.Box(V(0, .3f, -.2f), V(.26f, .2f, .18f), white);
            a.Box(V(0, .52f, -.24f), V(.2f, .2f, .22f), white);
            a.Box(V(0, .5f, -.4f), V(.07f, .07f, .14f), yellow);
            a.Box(V(0, .47f, -.45f), V(.06f, .05f, .05f), K.R.red);
            foreach (int s in new[] { -1, 1 })
            {
                a.Box(V(s * .17f, .4f, .08f), V(.07f, .2f, .46f), grey, Quaternion.Euler(0, 0, s * -10f));
                a.Box(V(s * .17f, .42f, .34f), V(.08f, .12f, .16f), K.R.ink, Quaternion.Euler(0, 0, s * -10f));
                a.Box(V(s * .105f, .56f, -.3f), V(.03f, .05f, .05f), K.R.ink);
                a.Box(V(s * .06f, .12f, 0), V(.06f, .22f, .06f), yellow);
                a.Box(V(s * .06f, .03f, -.04f), V(.1f, .06f, .14f), yellow);
            }
            a.Box(V(0, .38f, .33f), V(.2f, .08f, .16f), white);
            a.Box(V(0, .38f, .43f), V(.18f, .08f, .06f), K.R.ink);
            return a;
        }

        static readonly Dictionary<string, ComicMesh> AnimalArt = new Dictionary<string, ComicMesh>();

        /// <summary>An animal (kind Person, animal, never punchable) walking or standing on the sidewalk, facing -Z,
        /// feet at the pivot. Pass art null to reuse the mesh saved under the same meshId. crosses: waits at the kerb and
        /// crosses the road when the rider comes (a duck family).</summary>
        static GameObject SaveAnimal(string path, string meshId, ComicMesh art, Vector3 size, bool jump, float walk, bool crosses)
        {
            if (art != null) AnimalArt[meshId] = art;
            art = AnimalArt[meshId];
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            GameObject go = sync.Plain("Art");
            K.Renderer(go, meshId, art, true, false, false);
            sync.Finish();
            var box = PrefabBuildUtility.KeepSingle<BoxCollider>(root) ?? root.AddComponent<BoxCollider>();
            box.center = new Vector3(0, size.y * .5f, 0); box.size = size; box.isTrigger = false;
            var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
            o.kind = ObstacleKind.Person; o.animal = true; o.punchable = false; o.police = false; o.canJumpOver = jump; o.spansRoad = false; o.spawnWeight = 1f;
            var ped = PrefabBuildUtility.GetOrAdd<Pedestrian>(root);
            ped.walkSpeed = walk; ped.crosses = crosses; ped.crossSpeed = 1.3f; ped.crossLeadSeconds = 2.6f; ped.art = go.transform; ped.dodgeSpeed = 5f;
            PrefabBuildUtility.SaveAndClose(root, path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>A ramp prefab with its own art (no colliders).</summary>
        static Ramp SaveRampArt(string path, string meshId, ComicMesh art, float width, float rise, float height, float top, float launch, float hang)
        {
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            GameObject go = sync.Plain("Art");
            K.Renderer(go, meshId, art, true, true, false);
            sync.Finish();
            var ramp = PrefabBuildUtility.GetOrAdd<Ramp>(root);
            ramp.width = width; ramp.rise = rise; ramp.height = height; ramp.top = top; ramp.launch = launch; ramp.hangGravity = hang;
            ramp.lanes = Array.Empty<int>();
            PrefabBuildUtility.SaveAndClose(root, path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Ramp>();
        }

        /// <summary>Hip roof with ridge and hip caps (near the path); with solar panels on the slope facing -Z when asked.</summary>
        static void DetailedHipRoof(ComicMesh m, float x0, float x1, float z0, float z1, float y, float rise, Color c, bool solar)
        {
            HipRoof(m, x0, x1, z0, z1, y, rise, c);
            float cx = (x0 + x1) * .5f, cz = (z0 + z1) * .5f, w = x1 - x0, l = z1 - z0;
            Color cap = Color.Lerp(c, K.R.ink, .3f);
            Vector3 r0, r1;
            if (l >= w) { r0 = V(cx, y + rise, z0 + w * .5f); r1 = V(cx, y + rise, z1 - w * .5f); }
            else { r0 = V(x0 + l * .5f, y + rise, cz); r1 = V(x1 - l * .5f, y + rise, cz); }
            m.Surface = ComicSurface.PaintedMetal;
            if ((r1 - r0).sqrMagnitude > .01f) m.Beam(r0 + V(0, .05f, 0), r1 + V(0, .05f, 0), .16f, cap);
            m.Beam(V(x0, y, z0), r0, .1f, cap);
            m.Beam(V(x1, y, z0), l >= w ? r0 : r1, .1f, cap);
            m.Beam(V(x0, y, z1), l >= w ? r1 : r0, .1f, cap);
            m.Beam(V(x1, y, z1), r1, .1f, cap);
            if (!solar || l >= w) return;
            // panels on the slope facing -Z (towards riders coming down the street): dark blue with a light grid
            float inset = l * .5f, xa = x0 + inset * .85f, xb = x1 - inset * .85f;
            if (xb - xa < 1.5f) return;
            Vector3 P(float x, float t) => V(x, y + rise * t + .06f, z0 + (cz - z0) * t);
            Vector3 n = (Vector3.back * rise + Vector3.up * (cz - z0)).normalized;
            m.Surface = ComicSurface.Glass;
            K.Face(m, P(xa, .2f), P(xb, .2f), P(xb, .75f), P(xa, .75f), H("23324E"), n);
            m.Surface = ComicSurface.Plain;
            for (float x = xa + 1f; x < xb - .2f; x += 1f) K.Face(m, P(x - .02f, .2f) + n * .01f, P(x + .02f, .2f) + n * .01f, P(x + .02f, .75f) + n * .01f, P(x - .02f, .75f) + n * .01f, H("8FA3C0"), n, false);
            K.Face(m, P(xa, .47f) + n * .01f, P(xb, .47f) + n * .01f, P(xb, .49f) + n * .01f, P(xa, .49f) + n * .01f, H("8FA3C0"), n, false);
        }

        /// <summary>Hip roof over the rectangle x0..x1, z0..z1 at eaves height y, ridge `rise` higher, ink gutters.</summary>
        static void HipRoof(ComicMesh m, float x0, float x1, float z0, float z1, float y, float rise, Color c)
        {
            m.Surface = ComicSurface.PaintedMetal;
            float cx = (x0 + x1) * .5f, cz = (z0 + z1) * .5f, w = x1 - x0, l = z1 - z0;
            Vector3 a = V(x0, y, z0), b = V(x1, y, z0), cc = V(x1, y, z1), dd = V(x0, y, z1);
            Color side = Color.Lerp(c, K.R.ink, .12f);
            if (l >= w)
            {
                float inset = w * .5f;
                Vector3 r0 = V(cx, y + rise, z0 + inset), r1 = V(cx, y + rise, z1 - inset);
                K.Face(m, a, dd, r1, r0, side, (Vector3.left + Vector3.up).normalized);
                K.Face(m, b, cc, r1, r0, c, (Vector3.right + Vector3.up).normalized);
                K.TriFacing(m, a, b, r0, c, (Vector3.back + Vector3.up).normalized);
                K.TriFacing(m, dd, cc, r1, side, (Vector3.forward + Vector3.up).normalized);
            }
            else
            {
                float inset = l * .5f;
                Vector3 r0 = V(x0 + inset, y + rise, cz), r1 = V(x1 - inset, y + rise, cz);
                K.Face(m, a, b, r1, r0, c, (Vector3.back + Vector3.up).normalized);
                K.Face(m, dd, cc, r1, r0, side, (Vector3.forward + Vector3.up).normalized);
                K.TriFacing(m, a, dd, r0, side, (Vector3.left + Vector3.up).normalized);
                K.TriFacing(m, b, cc, r1, c, (Vector3.right + Vector3.up).normalized);
            }
            K.MinMaxBox(m, V(x0 - .05f, y - .2f, z0 - .05f), V(x1 + .05f, y, z0 + .1f), K.R.ink);
            K.MinMaxBox(m, V(x0 - .05f, y - .2f, z1 - .1f), V(x1 + .05f, y, z1 + .05f), K.R.ink);
            K.MinMaxBox(m, V(x0 - .05f, y - .2f, z0), V(x0 + .1f, y, z1), K.R.ink);
            K.MinMaxBox(m, V(x1 - .1f, y - .2f, z0), V(x1 + .05f, y, z1), K.R.ink);
        }

        // =====================================================================================================
        // KWINANA far scenery: the whole horizon (the Perth panorama is hidden this far south)
        // =====================================================================================================

        /// <summary>
        /// Right (west): Cockburn Sound out to Garden Island low on the horizon, bulk carriers at anchor, a jetty with
        /// port cranes ahead-right. Left (east): the industrial skyline in layers (tanks, columns, stacks with smoke,
        /// flare, refinery sheds), hazier with distance, and the Darling Range far away. Sloped with the district's road
        /// (pivot on the road centre line at the rider). No colliders, no outline.
        /// </summary>
        static ComicMesh KwinanaFar()
        {
            var m = new ComicMesh();
            var r = new System.Random(1201);
            float F() => (float)r.NextDouble();
            Color haze = new Color(.86f, .80f, .70f);
            Color Hz(Color c, float t) => Color.Lerp(c, haze, t);
            // ---- water: the sound on the right, out past Garden Island
            m.Surface = ComicSurface.Plain;
            for (float z = -160f; z < 640f; z += 40f)
            {
                K.Flat(m, 76f, 300f, 76f, 300f, z, z + 40f, SeaLevel - .06f, Sea);
                K.Flat(m, 300f, 1400f, 300f, 1400f, z, z + 40f, SeaLevel - .07f, Hz(SeaDeep, .12f));
            }
            for (int i = 0; i < 150; i++)
            {
                float x = 80f + F() * 520f, z = -100f + F() * 640f, len = 3f + F() * 12f;
                K.Flat(m, x, x + len, x, x + len, z, z + .45f, SeaLevel - .03f, i % 4 == 0 ? Foam : Ripple);
            }
            // ---- land on the left: scrub plain with red-earth scars
            for (float z = -160f; z < 640f; z += 40f) K.Flat(m, -76f, -1400f, -76f, -1400f, z, z + 40f, .1f, Scrub);
            for (int i = 0; i < 40; i++)
            {
                float x = 80f + F() * 300f, z = -80f + F() * 520f, w = 10f + F() * 30f;
                K.Flat(m, -x, -(x + w), -x, -(x + w * .8f), z, z + 6f + F() * 14f, .12f, i % 2 == 0 ? RedEarth : RedEarthDark);
            }
            // ---- Garden Island: a long low wooded island with a white sand rim
            m.Surface = ComicSurface.Foliage;
            for (float z = -260f; z < 640f; z += 60f + F() * 20f)
                m.Ellipsoid(V(450f + F() * 40f, 2f, z), V(150f + F() * 40f, 20f + F() * 12f, 120f + F() * 30f), Color.Lerp(H("4F7A4C"), H("9DB5B8"), .3f + F() * .1f));
            m.Surface = ComicSurface.Plain;
            for (float z = -260f; z < 640f; z += 40f) K.Flat(m, 372f, 384f, 372f, 384f, z, z + 40f, SeaLevel - .02f, Hz(Sand, .3f));
            // ---- bulk carriers at anchor
            (float x, float z, float yaw, float t)[] ships = { (210f, 70f, 20f, .25f), (300f, 190f, -10f, .4f), (170f, 250f, 160f, .35f), (330f, -40f, 5f, .4f) };
            foreach (var s in ships) Ship(m, V(s.x, SeaLevel - .05f, s.z), s.yaw, s.t, haze);
            // ---- a jetty with port cranes ahead-right
            FarJetty(m, 230f, 90f, 300f, .35f, haze);
            // ---- the industrial skyline on the left, in layers
            for (float z = -60f; z < 300f; z += 24f + F() * 10f)
            {
                float x = -(88f + F() * 50f);
                FarIndustry(m, V(x, .1f, z), (int)(F() * 6f), .08f + F() * .08f, haze, F);
            }
            for (float z = -40f; z < 340f; z += 34f + F() * 14f)
            {
                float x = -(170f + F() * 120f);
                FarIndustry(m, V(x, .1f, z), (int)(F() * 6f), .26f + F() * .1f, haze, F);
            }
            // ahead-left: more of the strip towards the vanishing point
            for (float x = -150f; x < -30f; x += 22f + F() * 12f) FarIndustry(m, V(x, .1f, 200f + F() * 50f), (int)(F() * 6f), .2f, haze, F);
            // ---- the Darling Range, far and blue
            Color hills = Color.Lerp(H("4A6688"), haze, .35f);
            m.Surface = ComicSurface.Foliage;
            for (float z = -400f; z < 700f; z += 120f + F() * 40f)
                m.Ellipsoid(V(-(760f + F() * 80f), 0, z), V(420f + F() * 120f, 90f + F() * 40f, 220f + F() * 60f), Color.Lerp(hills, haze, F() * .15f));
            m.Shear(-KwinanaDrop / 40f);
            return m;
        }

        /// <summary>One cluster on the industrial horizon (no outline): tanks, columns, a stack with smoke, a flare, a
        /// shed or a process frame; colours hazed by t towards the fog.</summary>
        static void FarIndustry(ComicMesh m, Vector3 p, int kind, float t, Color haze, Func<float> F)
        {
            Color Hz(Color c) => Color.Lerp(c, haze, t);
            switch (kind)
            {
                case 0: // tank group
                    m.Surface = ComicSurface.PaintedMetal;
                    for (int i = 0; i < 3; i++)
                    {
                        float rr = 7f + F() * 6f, h = 9f + F() * 6f;
                        Vector3 c = p + V(F() * 20f - 10f, 0, i * 16f);
                        m.Cylinder(c + V(0, h * .5f, 0), rr, h, Hz(TankWhite), 10);
                        m.Cylinder(c + V(0, h + .3f, 0), rr + .1f, .6f, Hz(SteelDark), 10);
                        m.Cylinder(c + V(0, h * .3f, 0), rr + .05f, h * .14f, Hz(TankBand), 10);
                    }
                    break;
                case 1: // columns
                    m.Surface = ComicSurface.Metal;
                    for (int i = 0; i < 3; i++)
                    {
                        float h = 22f + F() * 26f, rr = 1.6f + F() * 1.6f;
                        Vector3 c = p + V(i * 6f, 0, F() * 8f);
                        m.Cylinder(c + V(0, h * .5f, 0), rr, h, Hz(PipeSilver), 8);
                        m.Cylinder(c + V(0, h * .6f, 0), rr + .7f, .4f, Hz(SteelDark), 8);
                        m.Cylinder(c + V(0, h + rr * .3f, 0), rr, rr * .6f, Hz(PipeSilver), 8, .4f);
                    }
                    m.Box(p + V(6f, 8f, 4f), V(18f, 16f, 12f), Hz(SteelDark));
                    break;
                case 2: // tall stack with smoke
                {
                    float h = 60f + F() * 50f, rr = 2.2f + F() * 1.5f;
                    m.Surface = ComicSurface.Concrete;
                    m.Cylinder(p + V(0, h * .42f, 0), rr, h * .84f, Hz(Concrete), 8, .8f);
                    for (int i = 0; i < 4; i++) m.Cylinder(p + V(0, h * .84f + h * .04f * (i + .5f), 0), rr * .82f, h * .04f, Hz(i % 2 == 0 ? FlareRed : FlareWhite), 8);
                    m.Surface = ComicSurface.Plain;
                    for (int i = 0; i < 4; i++)
                        m.Ellipsoid(p + V(4f + i * 7f, h + 4f + i * 2.5f, i), V(7f + i * 3f, 5f + i, 6f + i * 2f), Hz(Color.Lerp(Smoke, FlareWhite, i * .2f)));
                    m.Surface = ComicSurface.Concrete;
                    m.Box(p + V(10f, 7f, 6f), V(22f, 14f, 16f), Hz(H("B9A48A")));
                    break;
                }
                case 3: // flare tower and its flame
                {
                    float h = 40f + F() * 20f;
                    m.Surface = ComicSurface.PaintedMetal;
                    for (int i = 0; i < 5; i++) m.Cylinder(p + V(0, h * (i + .5f) / 5f, 0), .9f, h / 5f, Hz(i % 2 == 0 ? FlareRed : FlareWhite), 6);
                    m.Beam(p + V(-3f, 0, 0), p + V(0, h, 0), .5f, Hz(SteelDark));
                    m.Beam(p + V(3f, 0, 0), p + V(0, h, 0), .5f, Hz(SteelDark));
                    m.Surface = ComicSurface.Plain;
                    m.Ellipsoid(p + V(0, h + 3f, 0), V(3.4f, 6.5f, 3.4f), Color.Lerp(Flame, haze, t * .5f));
                    m.Ellipsoid(p + V(0, h + 2.2f, 0), V(1.8f, 3.6f, 1.8f), Color.Lerp(FlameCore, haze, t * .5f));
                    break;
                }
                case 4: // refinery shed (alumina: rust-red and grey)
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Box(p + V(0, 12f, 0), V(30f, 24f, 40f), Hz(F() < .5f ? H("B8735A") : H("A8ADB0")));
                    m.Box(p + V(0, 24.5f, 0), V(31f, 1.2f, 41f), Hz(SteelDark));
                    m.Box(p + V(-6f, 30f, 8f), V(10f, 12f, 10f), Hz(H("C9C4B8")));
                    m.Cylinder(p + V(8f, 36f, -10f), 1.8f, 24f, Hz(Concrete), 8);
                    break;
                default: // process frame and a vessel
                    m.Surface = ComicSurface.PaintedMetal;
                    for (int i = 0; i < 4; i++) m.Box(p + V(i % 2 * 12f - 6f, 10f, i / 2 * 10f - 5f), V(.8f, 20f, .8f), Hz(SteelDark));
                    for (int i = 1; i <= 3; i++) m.Box(p + V(0, i * 6.5f, 0), V(13f, .6f, 11f), Hz(SteelDark));
                    m.Surface = ComicSurface.Metal;
                    m.Cylinder(p + V(2f, 14f, 0), 2.4f, 26f, Hz(PipeSilver), 8);
                    m.Cylinder(p + V(-12f, 6f, 6f), 6f, 12f, Hz(TankWhite), 10);
                    break;
            }
        }

        /// <summary>Bulk carrier (no name): dark red hull with a black top band, white stern superstructure, four deck cranes.</summary>
        static void Ship(ComicMesh m, Vector3 p, float yaw, float t, Color haze)
        {
            Color Hz(Color c) => Color.Lerp(c, haze, t);
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            void B(Vector3 c, Vector3 s, Color col) => m.Box(p + q * c, s, Hz(col), q);
            m.Surface = ComicSurface.PaintedMetal;
            B(V(0, 2.5f, 0), V(11f, 6f, 66f), H("7A2E24"));
            B(V(0, 5.9f, 0), V(11.2f, 1f, 66.2f), H("2A2C30"));
            B(V(0, 2.5f, 34.5f), V(7f, 6f, 4f), H("7A2E24"));
            B(V(0, 10f, -26f), V(10f, 8f, 9f), FlareWhite);
            B(V(0, 14.6f, -24f), V(13f, 1.2f, 4f), FlareWhite);
            B(V(0, 14.8f, -21.9f), V(9f, .9f, .1f), H("26333D"));
            B(V(0, 16.5f, -27f), V(2f, 3f, 2f), H("C8452F"));
            for (int i = 0; i < 4; i++)
            {
                float z = -12f + i * 12f;
                B(V(0, 6.4f, z), V(9.5f, .6f, 8f), H("2A2C30"));
                B(V(0, 10f, z + 5f), V(.8f, 7f, .8f), K.R.yellow);
                m.Beam(p + q * V(0, 13f, z + 5f), p + q * V(4f, 9f, z - 3f), .5f, Hz(K.R.yellow));
            }
        }

        /// <summary>A long low jetty on piles from the shore at x0 out to x1 at z, with two port cranes and a moored ship.</summary>
        static void FarJetty(ComicMesh m, float z, float x0, float x1, float t, Color haze)
        {
            Color Hz(Color c) => Color.Lerp(c, haze, t);
            m.Surface = ComicSurface.Concrete;
            for (float x = x0; x < x1; x += 20f)
            {
                m.Box(V(x + 10f, 5f, z), V(20f, 1.2f, 8f), Hz(Concrete));
                m.Box(V(x, 2f, z), V(1f, 6f, 6f), Hz(SteelDark));
            }
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 2; i++)
            {
                float x = x1 - 40f - i * 50f;
                m.Beam(V(x - 6f, 5f, z - 4f), V(x, 32f, z), 1.4f, Hz(TerminalBlue));
                m.Beam(V(x + 6f, 5f, z - 4f), V(x, 32f, z), 1.4f, Hz(TerminalBlue));
                m.Beam(V(x - 6f, 5f, z + 4f), V(x, 32f, z), 1.4f, Hz(TerminalBlue));
                m.Beam(V(x + 6f, 5f, z + 4f), V(x, 32f, z), 1.4f, Hz(TerminalBlue));
                m.Beam(V(x - 20f, 32f, z), V(x + 30f, 34f, z), 1.6f, Hz(TerminalBlue));
                m.Box(V(x, 34f, z), V(6f, 4f, 5f), Hz(FlareWhite));
            }
            Ship(m, V(x1 - 60f, SeaLevel - .05f, z + 14f), 90f, t, haze);
        }

        // =====================================================================================================
        // WELLARD
        // =====================================================================================================

        static void BuildWellard(bool atlas)
        {
            const string tag = "Wellard";
            string patternFolder = DataFolder + "/wellard";
            Begin(tag, patternFolder);

            Obstacle springer = K.SaveObstacle(Pf(tag, "OBS_Wellard_Springer"), "OBS_Wellard_Springer", SpringerMesh(), new Vector3(.8f, 1.05f, 1f), ObstacleKind.Thing, true, false, 1f);
            Obstacle balls = K.SaveObstacle(Pf(tag, "OBS_Wellard_Footballs"), "OBS_Wellard_Footballs", FootballsMesh(), new Vector3(1.3f, .55f, .9f), ObstacleKind.Thing, true, true, .8f);
            Obstacle bins = K.SaveObstacle(Pf(tag, "OBS_Wellard_WheelieBins"), "OBS_Wellard_WheelieBins", BinsMesh(), new Vector3(1.45f, 1.1f, .85f), ObstacleKind.Thing, true, true, 1f);
            Obstacle bollard = K.SaveObstacle(Pf(tag, "OBS_Wellard_Bollard"), "OBS_Wellard_Bollard", BollardMesh(), new Vector3(.45f, 1f, .45f), ObstacleKind.Thing, true, false, .8f);
            GameObject swan = SaveAnimal(Pf(tag, "ANM_Wellard_BlackSwan"), "ANM_Wellard_BlackSwan", SwanMesh(), new Vector3(.6f, 1.2f, 1f), true, .3f, false);
            GameObject swanStanding = SaveAnimal(Pf(tag, "ANM_Wellard_BlackSwanStanding"), "ANM_Wellard_BlackSwan", null, new Vector3(.6f, 1.2f, 1f), true, 0f, false);
            GameObject duck = SaveAnimal(Pf(tag, "ANM_Wellard_Duck"), "ANM_Wellard_Duck", DuckMesh(), new Vector3(.45f, .5f, .6f), true, .5f, false);
            GameObject duckCrossing = SaveAnimal(Pf(tag, "ANM_Wellard_DuckCrossing"), "ANM_Wellard_Duck", null, new Vector3(.45f, .5f, .6f), true, .4f, true);
            Ramp mound = SaveRampArt(Pf(tag, "RMP_Wellard_PumpMound"), "RMP_Wellard_PumpMound", MoundMesh(3.2f, 4f, 1.1f, 1.5f), 3.2f, 4f, 1.1f, 1.5f, 1.05f, .5f);

            K.WriteChunk(WlEntry(), ChunkPath(tag, "Entry"));
            K.WriteChunk(WlStation(), ChunkPath(tag, "Station"));
            K.WriteChunk(WlEstate(), ChunkPath(tag, "Estate"));
            K.WriteChunk(WlVillage(), ChunkPath(tag, "Village"));
            K.WriteChunk(WlPark(), ChunkPath(tag, "Park"));
            K.WriteChunk(WlWetland(), ChunkPath(tag, "Wetland"));
            K.WriteChunk(WlExit(), ChunkPath(tag, "Exit"));
            GameObject far = K.SaveFarScenery(Pf(tag, "FAR_Wellard_Horizon"), "FAR_Wellard_Horizon", WellardFar());

            var patterns = new List<HazardPattern>
            {
                K.Pattern(patternFolder, "HZ_Wellard_DuckCrossing", 45, 0, 1f, true, 30, (duckCrossing, -5.6f, 24f, 0f), (duckCrossing, -6.3f, 25f, 0f), (duckCrossing, -6.9f, 25.8f, 0f)),
                K.Pattern(patternFolder, "HZ_Wellard_BinDay", 50, 0, .9f, true, 30, (bins.gameObject, -2.6f, 18f, 0f), (bins.gameObject, 2.6f, 30f, 0f), (balls.gameObject, 0f, 40f, 0f)),
                K.Pattern(patternFolder, "HZ_Wellard_Swans", 45, 1, .8f, true, 30, (swan, -2.2f, 22f, 0f), (swanStanding, .2f, 24f, 30f), (duck, 2.4f, 27f, -20f)),
            };

            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme == null) throw new InvalidOperationException("No comic theme at " + ComicCityBuilder.ThemePath);
            District d = LoadDistrict(WellardPath);
            d.id = WellardId; d.displayName = "WELLARD"; d.order = 130; d.length = new Vector2(360f, 440f);
            d.chunks = WellardChunks.Select(c => Chunk(tag, c)).ToArray();
            d.orderedChunks = true;
            d.entry = Chunk(tag, "Entry");
            d.exit = Chunk(tag, "Exit");
            d.allowBoulevard = false;
            var obstacles = new List<Obstacle> { springer, balls, bins, bollard };
            if (theme.obstacles != null) obstacles.AddRange(theme.obstacles.Where(o => o != null && o.name == "OBS_Cones"));
            d.obstacles = obstacles.ToArray();
            d.jumpObstacles = new[] { springer, balls, bollard };
            d.gifts = Array.Empty<Gift>();
            var hazards = new List<HazardPattern>(StreetPatterns(theme, "HZ_SlowCar", "HZ_OncomingCar", "HZ_Crossing", "HZ_SidewalkStroll", "HZ_PoliceStop"));
            hazards.AddRange(patterns);
            d.hazards = hazards.ToArray();
            var people = new List<GameObject>(K.People(theme, "Kid", "Kid", "Kid", "DogWalker", "DogWalker", "Jogger", "Jogger", "Elder", "Shopper", "Student", "Skater", "Barista"));
            people.Add(duck); people.Add(duck); people.Add(swan);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { mound };
            d.rampFrequency = 1.2f;
            d.blendMeters = 150f;
            // Fresh, green and open: a clear suburban morning.
            d.fogColor = new Color(.80f, .88f, .86f);
            d.skyTint = new Color(.98f, 1f, .98f);
            d.ambientSky = new Color(.66f, .80f, .92f);
            d.ambientEquator = new Color(.84f, .86f, .76f);
            d.ambientGround = new Color(.55f, .58f, .42f);
            d.sunColor = new Color(1f, .96f, .88f);
            d.sunIntensity = 1.4f;
            d.farScenery = far;
            // gateway, ambience, ambienceVolume and the sky fields belong to the district system: never written here.
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            K.WriteReport("art-src/districts/wellard/catalog.txt");
            Debug.Log("[Districts/Wellard] built " + (WellardChunks.Length + 2) + " chunks: " + string.Join("; ", K.Report));
            if (!atlas) return;
            ComicMesh Make(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }
            K.RenderAtlas("art-src/districts/wellard/props-atlas.png", new List<(string, GameObject, ComicMesh)>
            {
                ("Paperbark", null, Make(m => Paperbark(m, Vector3.zero, 9f, 3))),
                ("YoungTree", null, Make(m => YoungTree(m, null, Vector3.zero, 2))),
                ("House", null, Make(m => HouseAtlas(m, 1))),
                ("HouseTwoStorey", null, Make(m => HouseAtlas(m, 4))),
                ("Springer", springer.gameObject, null),
                ("Footballs", balls.gameObject, null),
                ("WheelieBins", bins.gameObject, null),
                ("Bollard", bollard.gameObject, null),
                ("BlackSwan", swan, null),
                ("Duck", duck, null),
                ("PumpMound", mound.gameObject, null),
                ("ShadeSails", null, Make(m => Playground(m, Vector3.zero))),
                ("StationCanopy", null, Make(m => { var t = new ComicMesh(); StationCanopy(t, V(5f, 0, -4f)); Totem(t, V(1f, 0, -8f)); m.Add(t, Vector3.zero, Quaternion.Euler(0, 200f, 0), Vector3.one); })),
                ("Train", null, Make(m => TrainCar(m, Vector3.zero, true))),
                ("Car", null, CarMesh(H("C8452F"))),
                ("GoalPosts", null, Make(m => GoalPosts(m, Vector3.zero))),
                ("Reeds", null, Make(m => Reeds(m, Vector3.zero, 1))),
                ("ClockTower", null, Make(m => ClockTower(m, Vector3.zero))),
            });
        }

        // ---------------------------------------------------------------- Wellard surfaces

        /// <summary>Estate street: asphalt, concrete footpath by the kerb, grass verge on the outer 1.8 m.</summary>
        static void EstateStreet(CqChunkDraft d, bool verge = true, float vergeFrom = 0f, float vergeTo = -1f)
        {
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, Pave, KerbCap, false);
            if (!verge) return;
            if (vergeTo < 0f) vergeTo = d.Length;
            foreach ((float a, float b) in Pieces(d.Length, 2f))
                for (int s = -1; s <= 1; s += 2)
                {
                    if (a < vergeFrom - .01f || b > vergeTo + .01f) continue;
                    // the verge tapers in and out over 4 m where it starts or ends inside a chunk (boundary chunks)
                    float W(float z) => 1.8f * Mathf.Min(vergeFrom > 0f ? Mathf.Clamp01((z - vergeFrom) / 4f) : 1f, vergeTo < d.Length ? Mathf.Clamp01((vergeTo - z) / 4f) : 1f);
                    K.Flat(d.Road, s * (K.Edge - W(a)), s * K.Edge, s * (K.Edge - W(b)), s * K.Edge, a, b, K.Kerb + .03f, Lawn);
                }
        }

        /// <summary>v0.6: a chunk with its own surface (a path, a boardwalk, a shared street) begins and ends with a
        /// short stub of the street's surface (asphalt and the district's footpath) closed by a kerb band, so the change
        /// of surface happens inside the chunk and reads as made on purpose. District lengths vary, so any chunk, or the
        /// exit, can come next: every chunk line has the street on both sides.</summary>
        internal static void StreetStubs(CqChunkDraft d, Color footpath, Color band, float len = 2.4f)
        {
            ComicMesh m = d.Road;
            foreach ((float a, float b, float at) in new[] { (0f, len, len), (d.Length - len, d.Length, d.Length - len - .5f) })
            {
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(-K.Half, .034f, a), V(-K.Half, .034f, b), V(K.Half, .034f, b), V(K.Half, .034f, a), K.R.asphalt, Vector3.up, false);
                m.Surface = ComicSurface.Paving;
                for (int s = -1; s <= 1; s += 2)
                    K.Face(m, V(s * K.Half, K.Kerb + .032f, a), V(s * K.Half, K.Kerb + .032f, b), V(s * K.Edge, K.Kerb + .032f, b), V(s * K.Edge, K.Kerb + .032f, a), footpath, Vector3.up, false);
                SurfaceBand(m, at, .5f, band);
            }
        }

        /// <summary>The standard cross-section drawn as a path (park shared path, boardwalk): no joints on the sides,
        /// optional white centre dashes and cross joints (boards); the slab edges are closed at both ends and at a gap.</summary>
        static void PathSurface(CqChunkDraft d, Color path, ComicSurface pathSurface, Color side, ComicSurface sideSurface, Color edge,
            bool dashes, float boards, float skipFrom = -1f, float skipTo = -1f)
        {
            ComicMesh m = d.Road;
            float h = K.Kerb, w = K.Half, sw = K.Side, bottom = boards > 0f ? -.45f : -2f;
            Color joint = Color.Lerp(path, K.R.ink, .3f), sideJoint = Color.Lerp(side, K.R.ink, .3f);
            foreach ((float a, float b) in Pieces(d.Length, 4f, skipFrom, skipTo))
            {
                m.Surface = pathSurface;
                K.Face(m, V(-w, 0, a), V(-w, 0, b), V(w, 0, b), V(w, 0, a), path, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    m.Surface = sideSurface;
                    K.Face(m, V(s * w, h, a), V(s * w, h, b), V(s * (w + sw), h, b), V(s * (w + sw), h, a), side, Vector3.up, false);
                    K.Face(m, V(s * w, bottom, a), V(s * w, bottom, b), V(s * w, h, b), V(s * w, h, a), edge, new Vector3(-s, 0, 0), false);
                    K.Face(m, V(s * (w + sw), bottom, a), V(s * (w + sw), bottom, b), V(s * (w + sw), h, b), V(s * (w + sw), h, a), edge, new Vector3(s, 0, 0), false);
                    m.Surface = ComicSurface.Plain;
                    K.Flat(m, s * w, s * (w + .22f), s * w, s * (w + .22f), a, b, h + .02f, edge);
                }
                if (dashes)
                    for (float z = Mathf.Ceil(a / 5f) * 5f; z < b - .01f; z += 5f) K.Flat(m, -.07f, .07f, -.07f, .07f, z, Mathf.Min(z + 2.5f, b), .02f, FlareWhite);
                if (boards > 0f)
                    for (float z = Mathf.Ceil(a / boards) * boards; z < b - .05f; z += boards)
                    {
                        K.Flat(m, -w, w, -w, w, z, z + .05f, .012f, joint);
                        for (int s = -1; s <= 1; s += 2) K.Flat(m, s * w, s * (w + sw), s * w, s * (w + sw), z, z + .05f, h + .012f, sideJoint);
                    }
            }
            var ends = new List<(float z, Vector3 n)> { (0f, Vector3.back), (d.Length, Vector3.forward) };
            if (skipFrom >= 0f) { ends.Add((skipFrom, Vector3.forward)); ends.Add((skipTo, Vector3.back)); }
            foreach ((float ze, Vector3 n) in ends)
            {
                m.Surface = pathSurface;
                K.Face(m, V(-w, bottom, ze), V(w, bottom, ze), V(w, 0, ze), V(-w, 0, ze), skipFrom >= 0f && (ze == skipFrom || ze == skipTo) ? edge : path, n, false);
                for (int s = -1; s <= 1; s += 2) K.Face(m, V(s * w, bottom, ze), V(s * (w + sw), bottom, ze), V(s * (w + sw), h, ze), V(s * w, h, ze), edge, n, false);
            }
        }

        // ---------------------------------------------------------------- estate pieces

        static void YoungTree(ComicMesh m, List<DressingObstacle> collisions, Vector3 p, int seed)
        {
            Color[] greens = { H("5E9A44"), H("6FA84E"), H("4F8A3E"), H("7AA64A") };
            Color leaf = greens[Mathf.Abs(seed) % greens.Length];
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, .04f, 0), .55f, .08f, H("5A3F2C"), 8);
            m.Cylinder(p + V(0, 1.25f, 0), .08f, 2.5f, H("7A5C44"), 6, .7f);
            m.Box(p + V(.2f, .85f, 0), V(.05f, 1.7f, .05f), H("C9A16E"));
            m.Box(p + V(.1f, 1.15f, 0), V(.22f, .04f, .04f), K.R.ink);
            P.Plant(m, p + V(0, 2.75f, 0), 1.8f, leaf, Color.Lerp(leaf, K.R.yellow, .3f));
            P.Plant(m, p + V(.5f, 2.3f, .3f), 1.1f, Color.Lerp(leaf, K.R.ink, .12f), Color.Lerp(leaf, K.R.yellow, .3f));
            K.Thin(collisions, "YoungTree", p, 2.5f, .3f);
        }

        static void Window(ComicMesh m, CqLot l, float a, float u0, float u1, float w, float o, Color frame)
        {
            K.LQuad(m, l, a - w * .5f - .12f, a + w * .5f + .12f, u0 - .12f, u1 + .12f, o + .02f, frame);
            K.LQuad(m, l, a - w * .5f, a + w * .5f, u0, u1, o + .04f, GlassDark);
            K.LQuad(m, l, a - .04f, a + .04f, u0, u1, o + .05f, frame, false);
        }

        static void LotHipRoof(ComicMesh m, CqLot l, float a0, float a1, float o0, float o1, float y, float rise, Color c, bool solar = false)
        {
            float xa = l.S * (l.Line - o0), xb = l.S * (l.Line - o1);
            DetailedHipRoof(m, Mathf.Min(xa, xb), Mathf.Max(xa, xb), l.Z0 + a0, l.Z0 + a1, l.Base + y, rise, c, solar);
        }

        /// <summary>A WA estate house: rendered brick, a double garage with a Colorbond door at the front, a feature
        /// portico, white-framed windows, hip roofs in Colorbond colours (variant v picks walls, roof, layout, storeys).</summary>
        static void House(ComicMesh m, CqLot l, int v)
        {
            Color wall = HouseWalls[v % HouseWalls.Length], roof = Roofs[(v * 2 + 1) % Roofs.Length], trim = H("F4F2EA");
            Color feature = HouseWalls[(v + 3) % HouseWalls.Length], door = Color.Lerp(Roofs[(v + 2) % Roofs.Length], FlareWhite, .25f);
            bool two = v % 3 == 2, garageFirst = v % 2 == 0;
            float h = two ? 5.8f : 3f;
            float ga0 = garageFirst ? .4f : l.W - 6.4f, ga1 = ga0 + 6f;
            float b0 = garageFirst ? ga1 : .4f, b1 = garageFirst ? l.W - .4f : ga0;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, .2f, l.W - .2f, -1.4f, .25f, 0f, -l.Depth, H("8E8A80"));
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, ga0, ga1, .25f, 2.9f, 0f, -6.5f, wall);
            m.Surface = ComicSurface.PaintedMetal;
            K.LQuad(m, l, ga0 + .45f, ga1 - .45f, .25f, 2.5f, .02f, K.R.ink);
            K.LQuad(m, l, ga0 + .55f, ga1 - .55f, .25f, 2.4f, .04f, door);
            for (float y = .55f; y < 2.3f; y += .3f) K.LQuad(m, l, ga0 + .55f, ga1 - .55f, y, y + .05f, .05f, Color.Lerp(door, K.R.ink, .3f), false);
            m.Surface = two ? ComicSurface.Plaster : ComicSurface.Brick;
            K.LBox(m, l, b0, b1, .25f, h, -1.2f, -l.Depth, wall);
            m.Surface = ComicSurface.Plaster;
            float pa = garageFirst ? b0 : b1 - 2.4f;
            K.LBox(m, l, pa, pa + 2.4f, .25f, Mathf.Min(h, 3f) + .35f, .2f, -1.3f, feature);             // portico
            K.LBox(m, l, pa - .1f, pa + 2.5f, Mathf.Min(h, 3f) + .35f, Mathf.Min(h, 3f) + .55f, .3f, -1.3f, trim);
            m.Surface = ComicSurface.Wood;
            K.LQuad(m, l, pa + .7f, pa + 1.7f, .25f, 2.4f, -1.18f, H("6B4A32"));
            m.Surface = ComicSurface.Glass;
            float bw = b1 - b0;
            float wa = garageFirst ? b0 + bw * .62f : b0 + bw * .38f;
            Window(m, l, wa, 1.05f, 2.35f, Mathf.Min(2.4f, bw * .4f), -1.2f, trim);
            if (two)
            {
                Window(m, l, b0 + bw * .3f, 3.9f, 5.1f, 1.6f, -1.2f, trim);
                Window(m, l, b0 + bw * .72f, 3.9f, 5.1f, 1.6f, -1.2f, trim);
                Window(m, l, (ga0 + ga1) * .5f, 3.9f, 5.1f, 2f, -1.2f, trim);
                m.Surface = ComicSurface.Plaster;
                K.LBox(m, l, ga0, ga1, 2.9f, h, -1.2f, -6.5f, wall);
                LotHipRoof(m, l, Mathf.Min(ga0, b0) - .4f, Mathf.Max(ga1, b1) + .4f, -.7f, -l.Depth - .4f, h, 2f, roof, v % 2 == 1);
            }
            else
            {
                LotHipRoof(m, l, b0 - .4f, b1 + .4f, -.7f, -l.Depth - .4f, h, 2.2f, roof, v % 3 != 1);
                LotHipRoof(m, l, ga0 - .3f, ga1 + .2f, .4f, -6.8f, 2.9f, 1.3f, roof);
            }
            // window sills under the front windows, a downpipe at the garage corner, a feature light by the door
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, wa - Mathf.Min(2.4f, bw * .4f) * .5f - .2f, wa + Mathf.Min(2.4f, bw * .4f) * .5f + .2f, .85f, .97f, -1.02f, -1.2f, trim);
            m.Surface = ComicSurface.PaintedMetal;
            float dp = garageFirst ? ga1 - .15f : ga0 + .15f;
            K.LBox(m, l, dp - .06f, dp + .06f, .25f, 2.9f, .1f, -.02f, Color.Lerp(roof, K.R.ink, .2f));
            K.LBox(m, l, pa + 1.85f, pa + 2f, 1.9f, 2.1f, -1.05f, -1.2f, K.R.cream);
        }

        static void HouseAtlas(ComicMesh m, int v) => House(m, new CqLot(-1, 0, 13, 0f, 13f, 0f), v);   // facade towards the atlas camera

        /// <summary>One side of an estate street from z0 to z1: front lawns, a low limestone retaining wall with gaps for
        /// the driveways, houses on 13 m lots, Colorbond side fences, letterboxes, young street trees on the verge.</summary>
        static void EstateSide(CqChunkDraft d, int s, float z0, float z1, int seed, float ground = 72f)
        {
            const float setback = 6.5f, depth = 13f;
            Ground(d, s, z0, z1, 0, ground, Lawn, K.Kerb - .02f, ComicSurface.Foliage);
            int lots = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / 13f));
            float lw = (z1 - z0) / lots;
            float wallFrom = z0;
            for (int i = 0; i < lots; i++)
            {
                float za = z0 + i * lw, zb = za + lw;
                int v = seed + i * 5 + (s > 0 ? 3 : 0);
                var l = new CqLot(s, za, zb, K.Edge + setback, depth, d.Base((za + zb) * .5f));
                House(d.Frontage, l, v);
                d.Wall(l, 6f);
                // the driveway in front of the garage (the garage is at the lot's start or end, see House)
                bool garageFirst = v % 2 == 0;
                float g0 = garageFirst ? za + .9f : zb - 5.9f, g1 = g0 + 5f;
                d.Road.Surface = ComicSurface.Paving;
                foreach ((float a, float b) in new[] { (g0, (g0 + g1) * .5f), ((g0 + g1) * .5f, g1) })
                    K.Flat(d.Road, s * K.Edge, s * (K.Edge + setback), s * K.Edge, s * (K.Edge + setback), a, b, K.Kerb + .005f, H("B9A890"));
                LimestoneWall(d.SlopedFrontage, s, wallFrom, g0 - .3f, K.Edge + .5f, .4f);
                wallFrom = g1 + .3f;
                // letterbox pillar by the driveway, a garden shrub
                Vector3 lb = V(s * (K.Edge + 1f), d.Ground(g0 - .8f), g0 - .8f);
                d.Frontage.Surface = ComicSurface.Brick;
                d.Frontage.Box(lb + V(0, .55f, 0), V(.45f, 1.1f, .45f), HouseWalls[(v + 1) % HouseWalls.Length]);
                d.Frontage.Surface = ComicSurface.PaintedMetal;
                d.Frontage.Box(lb + V(0, 1.15f, 0), V(.5f, .1f, .5f), K.R.ink);
                float sz = garageFirst ? zb - 3f : za + 3f;
                P.Shrub(d.Frontage, V(s * (K.Edge + 3f), d.Ground(sz), sz), 1.1f, Color.Lerp(P.LeafGreen, Lawn, .3f));
                // Colorbond side fence on the lot line, from the front of the house back
                d.Frontage.Surface = ComicSurface.PaintedMetal;
                if (i > 0) K.MinMaxBox(d.Frontage, V(s * (K.Edge + setback - 1.2f), d.Base(za) - .2f, za - .05f), V(s * (K.Edge + setback + depth), d.Base(za) + 1.8f, za + .05f), H("6E7378"));
            }
            LimestoneWall(d.SlopedFrontage, s, wallFrom, z1, K.Edge + .5f, .4f);
            for (float z = z0 + 6.5f; z < z1 - 2f; z += lw)
                YoungTree(d.Props, d.Collisions, V(s * (K.Edge - .9f), d.Ground(z), z), seed + (int)z);
        }

        /// <summary>v0.6 boundary street side (replaces the hedge-and-lawn gap): the estate's low limestone wall and
        /// young street trees at the estate's spacing, lawn behind; bush: paperbarks and shrubs behind the wall (the
        /// bush between the industry and the estate), otherwise open lawn (so what runs alongside can be seen).</summary>
        static void EstateEdge(CqChunkDraft d, int s, float z0, float z1, int seed, bool bush, bool trees = true)
        {
            Ground(d, s, z0, z1, 0, 72, Lawn, K.Kerb - .02f, ComicSurface.Foliage);
            LimestoneWall(d.SlopedFrontage, s, z0, z1, K.Edge + .5f, .4f);
            if (trees)
                for (float z = z0 + 4f; z < z1 - 2f; z += 9f)
                    YoungTree(d.Props, d.Collisions, V(s * (K.Edge - .9f), d.Ground(z), z), seed + (int)z);
            var r = new System.Random(seed);
            float F() => (float)r.NextDouble();
            for (float z = z0 + 1.5f; z < z1 - 1f; z += 3.2f + F())
                P.Shrub(d.Frontage, V(s * (K.Edge + 2f + F() * (bush ? 9f : 3f)), d.Ground(z) - .05f, z), .9f + F() * .5f, Color.Lerp(P.LeafGreen, bush ? PaperLeaf : Lawn, .3f + F() * .3f));
            if (!bush) return;
            int k = 0;
            for (float z = z0 + 3f; z < z1 - 2f; z += 6.5f, k++)
                Paperbark(k % 2 == 0 ? d.Frontage : d.Deep, V(s * (K.Edge + 6f + F() * 4f + (k % 2) * 9f), d.Ground(z) - .05f, z), 8f + F() * 3f, seed + k);
        }

        // ---------------------------------------------------------------- Wellard chunks

        static CqChunkDraft WlEntry()
        {
            // From Kwinana: the bush thins into the estate. Left: the Mandurah line that came across the boundary runs
            // on behind the first houses to the station (the next chunk): same track, masts and fence. The grass verge
            // starts 6 m in (the boundary surface is plain footpath).
            var d = new CqChunkDraft("Wellard_Entry", WellardDrop);
            EstateStreet(d, true, 6f);
            EstateEdge(d, -1, 0, 14, 199, false);
            EstateEdge(d, 1, 0, 14, 201, true);
            EstateSide(d, -1, 14, 40, 8);
            EstateSide(d, 1, 14, 40, 3);
            RailLine(d, -1, z => K.Edge + RailOff, 0f, 40f, 0f, 2f, 22f);
            d.StandardSlots();
            return d;
        }

        /// <summary>Wellard's exit, sea side: how far the coast still is from where Rockingham's foreshore has it
        /// (0 at the boundary: the limestone wall 13 m and the water's edge 25 m beyond the sidewalk).</summary>
        static float WlCoastShift(float z) => 87f * Mathf.Pow(1f - Mathf.Clamp01(z / 40f), 2.2f);

        static CqChunkDraft WlExit()
        {
            // The road arrives at the sea. Left: the last houses, then lawn and the first Norfolk pine. Right: one last
            // house, then the estate ends and the coast comes in at an angle: dune scrub, white sand and the turquoise
            // water of Cockburn Sound, meeting Rockingham's foreshore (lawn, limestone wall, beach) on the boundary line.
            var d = new CqChunkDraft("Wellard_Exit", WellardDrop);
            EstateStreet(d, true, 0f, WlExitVergeEnd);
            EstateSide(d, -1, 0, 26, 11);
            Ground(d, -1, 26, 40, 0, 72, Lawn, K.Kerb - .02f, ComicSurface.Foliage);
            LimestoneWall(d.SlopedFrontage, -1, 26, 40, K.Edge + .5f, .4f);
            YoungTree(d.Props, d.Collisions, V(-(K.Edge - .9f), d.Ground(28.5f), 28.5f), 77);
            NorfolkPine(d.Frontage, V(-(K.Edge + 4.5f), d.Ground(35f), 35f), 13f);
            P.Shrub(d.Frontage, V(-(K.Edge + 3f), d.Ground(30f), 30f), 1.1f, Color.Lerp(P.LeafGreen, Lawn, .3f));
            EstateSide(d, 1, 0, 13, 16, 24f);
            WlCoastArrival(d);
            d.StandardSlots();
            return d;
        }

        /// <summary>The right (+X) side of Wellard's exit: land, a low foredune, sand and water as one height field, the
        /// shore line swinging in from 87 m further out (z = 0) to Rockingham's foreshore profile at z = 40.</summary>
        static void WlCoastArrival(CqChunkDraft d)
        {
            const float L = 40f;
            float sea = RockinghamBuilder.RSea, wallX = RockinghamBuilder.WallX;
            Color dune = H("A9B06E"), sand = RockinghamBuilder.Sand, wet = RockinghamBuilder.SandWet;
            float Wall(float z) => wallX + WlCoastShift(z);
            float Height(float x, float z)
            {
                float w = Wall(z), shift = WlCoastShift(z);
                if (x <= w)
                {
                    // land: lawn level, with a low foredune just behind the beach while the coast is still far out
                    float hump = Mathf.Clamp01((shift - 4f) / 10f) * 1.1f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Clamp01((w - x) / 11f))), 1.5f);   // Max: sin(pi) is a hair below 0, and Pow of that is NaN
                    return K.Kerb + .05f + hump;
                }
                return Mathf.Lerp(-.45f, sea - .4f, Mathf.InverseLerp(w + .7f, w + 17f, x));
            }
            Color Colour(float x, float z, float y)
            {
                float w = Wall(z);
                if (x > w + .3f) return x > w + 8.5f ? wet : sand;
                return Color.Lerp(Lawn, dune, Mathf.Clamp01((x - (w - 16f)) / 8f) * Mathf.Clamp01(WlCoastShift(z) / 3f));
            }
            // behind the last house (z 0..13) the lawn slab reaches 24 m; the height field carries on from there
            RockinghamBuilder.Terrain(d.Road, K.Edge + 24f, 172f, 0f, 13f, 4f, 2.6f, Height, Colour, ComicSurface.Foliage);
            RockinghamBuilder.Terrain(d.Road, K.Edge, 172f, 13f, L, 3f, 1.5f, Height, Colour, ComicSurface.Foliage);
            for (float z = 0f; z < L - .01f; z += 4f)
            {
                float shore = Wall(z + 4f) + 12f;
                K.Water(d.Road, shore - 4f, shore + 29f, z, z + 4f, sea, RockinghamBuilder.Turquoise, RockinghamBuilder.TurquoiseLight, 300 + (int)z);
                if (shore + 29f < 172f) K.Water(d.Road, shore + 29f, 172f, z, z + 4f, sea, RockinghamBuilder.SeaBlue, RockinghamBuilder.SeaRipple, 340 + (int)z);
            }
            // the estate's limestone wall and street trees to the end of the houses' street, bush on the dune
            LimestoneWall(d.SlopedFrontage, 1, 13, 30, K.Edge + .5f, .4f);
            YoungTree(d.Props, d.Collisions, V(K.Edge - .9f, d.Ground(19.5f), 19.5f), 78);
            YoungTree(d.Props, d.Collisions, V(K.Edge - .9f, d.Ground(28.5f), 28.5f), 79);
            var r = new System.Random(55);
            float F() => (float)r.NextDouble();
            for (float z = 14f; z < 38f; z += 2.2f + F() * 1.4f)
            {
                float w = Wall(z), x = Mathf.Lerp(K.Edge + 2.5f, Mathf.Max(K.Edge + 3f, w - 2f), F());
                if (WlCoastShift(z) < 3f && x > K.Edge + 5f) continue;                  // the mown foreshore lawn starts
                Vector3 p = V(x, d.Y(z) + Height(x, z) - .08f, z);
                int kind = r.Next(5);
                if (kind == 0) P.Balga(d.Frontage, p, .8f + F() * .2f, 400 + (int)z);
                else if (kind == 1) P.Banksia(d.Frontage, p, .8f + F() * .3f, 410 + (int)z);
                else P.Shrub(d.Frontage, p, .9f + F() * .6f, Color.Lerp(P.GumLeafDark, dune, .2f + F() * .5f));
            }
            // Rockingham's limestone wall between lawn and beach starts here (the last 8 m) and its first pine
            ComicMesh wall = d.SlopedFrontage;
            wall.Surface = ComicSurface.Concrete;
            for (float z = 32f; z < L - .01f; z += 2f)
            {
                float xa = Wall(z), xb = Wall(z + 2f), yaw = Mathf.Atan2(xb - xa, 2f) * Mathf.Rad2Deg, len = Mathf.Sqrt(4f + (xb - xa) * (xb - xa));
                wall.Box(V((xa + xb) * .5f + .32f, (K.Kerb + .32f - 1.1f) * .5f, z + 1f), V(.75f, K.Kerb + .32f + 1.1f, len), RockinghamBuilder.Limestone, Quaternion.Euler(0, yaw, 0));
                wall.Box(V((xa + xb) * .5f + .32f, K.Kerb + .38f, z + 1f), V(.9f, .12f, len), RockinghamBuilder.LimestoneCap, Quaternion.Euler(0, yaw, 0));
            }
            NorfolkPine(d.Frontage, V(13f, d.Ground(36f) + .05f, 36f), 12f);
            // Rockingham's foreshore path (2.4 m, 6.6 m beyond the sidewalk) starts here, off the roadside footpath
            ComicMesh road = d.Road;
            road.Surface = ComicSurface.Paving;
            K.Face(road, V(14.6f, K.Kerb + .07f, 30.6f), V(14.6f, K.Kerb + .07f, L), V(17f, K.Kerb + .07f, L), V(17f, K.Kerb + .07f, 30.6f), RockinghamBuilder.PathGrey, Vector3.up, false);
            K.Face(road, V(K.Edge, K.Kerb + .07f, 30.6f), V(K.Edge, K.Kerb + .07f, 33f), V(14.6f, K.Kerb + .07f, 33f), V(14.6f, K.Kerb + .07f, 30.6f), RockinghamBuilder.PathGrey, Vector3.up, false);
        }

        static CqChunkDraft WlEstate()
        {
            var d = new CqChunkDraft("Wellard_Estate", WellardDrop);
            EstateStreet(d);
            EstateSide(d, -1, 0, 40, 20);
            EstateSide(d, 1, 0, 40, 25);
            // bin day: a pair of wheelie bins out on the verge (decoration with a simple collider)
            d.Props.Add(BinsMesh(), V(K.Edge - .8f, d.Ground(21f), 21f), Quaternion.Euler(0, -90f, 0), Vector3.one);
            d.Collisions.Add(new DressingObstacle { id = "WheelieBins", position = V(K.Edge - .8f, d.Ground(21f), 21f), rotation = V(0, -90f, 0), centre = V(0, .55f, 0), size = V(1.45f, 1.1f, .85f), canJumpOver = true });
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft WlStation()
        {
            // Left: the forecourt and the teal entrance canopy (WELLARD), the station building, the platform, the Mandurah
            // line with a train and overhead wires. Right: the car park.
            var d = new CqChunkDraft("Wellard_Station", WellardDrop);
            EstateStreet(d);
            Ground(d, -1, 0, 40, 0, 72, Pave, K.Kerb - .02f, ComicSurface.Paving);
            d.Road.Surface = ComicSurface.Plain;
            foreach ((float a, float b) in Pieces(40f, 4f))
            {
                K.Flat(d.Road, -(K.Edge + 19f), -(K.Edge + 23f), -(K.Edge + 19f), -(K.Edge + 23f), a, b, K.Kerb + .01f, PlatformBrick);
                K.Flat(d.Road, -(K.Edge + 22.4f), -(K.Edge + 22.7f), -(K.Edge + 22.4f), -(K.Edge + 22.7f), a, b, K.Kerb + .02f, K.R.yellow);
            }
            Track(d, -1, K.Edge + 25f);
            TrainCar(d.Landmark, V(-(K.Edge + 25f), d.Ground(10f) + .3f, 10f), true);
            TrainCar(d.Landmark, V(-(K.Edge + 25f), d.Ground(29.5f) + .3f, 29.5f), false);
            for (float z = 2f; z < 40f; z += 20f) Mast(d.SlopedFrontage, -1, K.Edge + 28f, z);
            StationBuilding(d, new CqLot(-1, 22, 38, K.Edge + 11f, 7.5f, d.Base(30f)));
            StationCanopy(d.Landmark, V(-(K.Edge + .6f), d.Base(27f), 23f));
            PlatformShelter(d.Frontage, V(-(K.Edge + 21f), d.Base(10f), 10f));
            // forecourt: a totem sign, bike racks, bins
            Totem(d.Landmark, V(-(K.Edge + 2.6f), d.Ground(14f), 14f));
            YoungTree(d.Frontage, null, V(-(K.Edge + 5f), d.Ground(6f), 6f), 3);
            YoungTree(d.Frontage, null, V(-(K.Edge + 9f), d.Ground(17f), 17f), 4);
            YoungTree(d.Frontage, null, V(-(K.Edge + 5f), d.Ground(36f), 36f), 5);
            K.PropFromKit(d.Frontage, null, "Bench", V(-(K.Edge + 6.5f), d.Ground(9.5f), 9.5f), 90f);
            d.Road.Surface = ComicSurface.Foliage;
            K.SideBox(d.Road, -1, 2f, 11f, -2.4f, K.Kerb + .02f, 3.2f, 7.5f, Lawn);                   // a garden bed
            P.Shrub(d.Frontage, V(-(K.Edge + 4.2f), d.Ground(3.5f), 3.5f), .9f, H("4F8A3E"));
            P.Shrub(d.Frontage, V(-(K.Edge + 6.8f), d.Ground(4.5f), 4.5f), 1f, H("5E9A44"));
            K.PropFromKit(d.Props, d.Collisions, "BikeRack", V(-(K.Edge + 2.5f), d.Ground(28f), 28f), 90f);
            K.PropFromKit(d.Props, d.Collisions, "RecycleBins", V(-(K.Edge - .7f), d.Ground(24f), 24f), 90f);
            // right: the car park (asphalt, white bays, parked cars, young trees and lights in the islands)
            Ground(d, 1, 0, 40, 0, 1.5f, Lawn, K.Kerb - .02f, ComicSurface.Foliage);
            Ground(d, 1, 0, 40, 1.5f, 32f, K.R.asphalt, K.Kerb - .03f, ComicSurface.Asphalt);
            Ground(d, 1, 0, 40, 32f, 72f, Lawn, K.Kerb - .02f, ComicSurface.Foliage);
            d.Road.Surface = ComicSurface.Plain;
            for (float z = 1f; z < 39f; z += 2.7f)
                foreach (float x in new[] { K.Edge + 3.5f, K.Edge + 13.5f, K.Edge + 18.5f, K.Edge + 28.5f })
                    K.Flat(d.Road, x - 2.4f, x + 2.4f, x - 2.4f, x + 2.4f, z, z + .1f, K.Kerb - .015f, FlareWhite);
            Color[] cars = { H("C8452F"), H("F4F2EA"), H("2F5FA8"), H("5D666E"), H("D8DDE0"), H("2F8A7A"), H("1E2128"), H("E8C24A") };
            int ci = 0;
            foreach (float z in new[] { 2.35f, 5.05f, 10.45f, 15.85f, 21.25f, 29.35f, 34.75f })
            {
                d.Frontage.Add(CarMesh(cars[ci++ % cars.Length]), V(K.Edge + 4.2f, d.Base(z) - .02f, z), Quaternion.Euler(0, ci % 2 == 0 ? 90f : -90f, 0), Vector3.one);
                if (ci % 2 == 1) d.Frontage.Add(CarMesh(cars[(ci + 3) % cars.Length]), V(K.Edge + 12.8f, d.Base(z + 2.7f) - .02f, z + 2.7f), Quaternion.Euler(0, 90f, 0), Vector3.one);
            }
            d.SlopedFrontage.Surface = ComicSurface.Concrete;
            K.SideBox(d.SlopedFrontage, 1, 1f, 39f, K.Kerb - .03f, K.Kerb + .15f, 15.7f, 16.3f, KerbCap);
            YoungTree(d.Frontage, null, V(K.Edge + 16f, d.Ground(8f), 8f), 1);
            YoungTree(d.Frontage, null, V(K.Edge + 16f, d.Ground(30f), 30f), 2);
            K.PropFromKit(d.Frontage, null, "StreetLamp", V(K.Edge + 16f, d.Ground(19f), 19f), 0f);
            for (float z = 6f; z < 40f; z += 13f) YoungTree(d.Props, d.Collisions, V(K.Edge - .9f, d.Ground(z), z), (int)z);
            Paperbark(d.Deep, V(K.Edge + 36f, d.Ground(10f), 10f), 10f, 4);
            Paperbark(d.Deep, V(K.Edge + 38f, d.Ground(30f), 30f), 11f, 5);
            d.StandardSlots();
            return d;
        }

        /// <summary>The station building: grey render, a dark glass band, two pop-up clerestory boxes on the roof.</summary>
        static void StationBuilding(CqChunkDraft d, CqLot l)
        {
            ComicMesh m = d.Frontage;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, l.W, -1.2f, .2f, 0, -l.Depth, K.R.ink);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, l.W, .2f, 5f, 0, -l.Depth, StationGrey);
            K.LBox(m, l, 1f, 9f, .2f, 5f, .05f, -l.Depth, StationOrange);
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, 3f, 7f, .2f, 3.2f, .08f, H("1A2028"));                                     // the way in
            K.LBox(m, l, 9.6f, l.W - .8f, 1f, 3.2f, .03f, -.3f, GlassDark);
            for (float a = 10.6f; a < l.W - 1f; a += 2f) K.LBox(m, l, a - .06f, a + .06f, 1f, 3.2f, .12f, 0f, H("3E4C5A"));
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, 9.4f, l.W - .6f, 3.2f, 3.35f, .14f, 0f, H("3E4C5A"));                              // window head
            K.LBox(m, l, 9.4f, l.W - .6f, .85f, 1f, .2f, 0f, H("D9D6CE"));                                  // sill
            K.LBox(m, l, 9.2f, l.W - .4f, 3.7f, 3.9f, 1.6f, 0f, StationTeal);                                // sun hood
            K.LBox(m, l, 9.2f, l.W - .4f, 3.6f, 3.7f, 1.65f, 1.5f, K.R.ink);
            foreach (float a in new[] { 1f, 9f }) K.LBox(m, l, a - .15f, a + .15f, .2f, 5f, .25f, -.05f, H("BDB8AE"));   // piers either side of the orange entry
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.4f, l.W + .4f, 5f, 5.35f, .6f, -l.Depth - .4f, H("3E4C5A"));
            foreach (float a in new[] { 3f, l.W - 6f })
            {
                K.LBox(m, l, a, a + 4f, 5.35f, 7.4f, -1.5f, -l.Depth + 1f, H("3E4C5A"));
                m.Surface = ComicSurface.Glass;
                K.LQuad(m, l, a + .3f, a + 3.7f, 6f, 7f, -1.48f, H("8FB8C8"));
                m.Surface = ComicSurface.PaintedMetal;
                K.LBox(m, l, a - .3f, a + 4.3f, 7.4f, 7.6f, -1.2f, -l.Depth + .7f, H("2A3440"));
            }
            d.Wall(l, 7.5f);
        }

        /// <summary>The entrance canopy (teal steel, grey concrete columns, white WELLARD letters on the front fascia)
        /// running from the sidewalk (p, x = p.x) back to the building, front at p.z.</summary>
        static void StationCanopy(ComicMesh m, Vector3 p)
        {
            float x0 = p.x, x1 = p.x - 10.4f, z0 = p.z, z1 = p.z + 8f, y = p.y;
            m.Surface = ComicSurface.Concrete;
            foreach (float x in new[] { x0 - .5f, x1 + .5f })
                foreach (float z in new[] { z0 + .5f, z1 - .5f }) m.Box(V(x, y + 2.2f, z), V(.6f, 4.4f, .6f), H("BDB8AE"));
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, V(x1, y + 4.4f, z0), V(x0, y + 5.5f, z0 + .5f), StationTeal);              // front fascia
            K.MinMaxBox(m, V(x1, y + 4.4f, z1 - .5f), V(x0, y + 5.5f, z1), StationTeal);
            K.MinMaxBox(m, V(x1 - .3f, y + 5.5f, z0 - .4f), V(x0 + .3f, y + 5.8f, z1 + .4f), StationTealLight);
            K.MinMaxBox(m, V(x1, y + 4.4f, z0), V(x1 + .3f, y + 5.5f, z1), StationTeal);
            K.MinMaxBox(m, V(x0 - .3f, y + 4.4f, z0), V(x0, y + 5.5f, z1), StationTeal);
            for (float x = x1 + 1.4f; x < x0 - .5f; x += 2.2f)
            {
                m.Beam(V(x, y + 4.4f, z0 + .3f), V(x + 1.1f, y + 5.4f, (z0 + z1) * .5f), .14f, StationTeal);
                m.Beam(V(x + 1.1f, y + 5.4f, (z0 + z1) * .5f), V(x + 2.2f, y + 4.4f, z1 - .3f), .14f, StationTeal);
            }
            Letters(m, "WELLARD", (x0 + x1) * .5f, y + 4.95f, z0 - .04f, .15f, H("F4F2EA"));
        }

        /// <summary>A station totem: a teal blade with WELLARD in teal letters on a white band facing the rider, and a
        /// little train face (no logo).</summary>
        static void Totem(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, .15f, 0), V(4.4f, .3f, .7f), H("BDB8AE"));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 1.7f, 0), V(4.2f, 2.8f, .4f), StationTeal);
            m.Box(p + V(0, 3.2f, 0), V(4.3f, .2f, .5f), StationTealLight);
            m.Box(p + V(0, 2.45f, -.21f), V(3.9f, .95f, .02f), H("F4F2EA"));
            Letters(m, "WELLARD", p.x, p.y + 2.45f, p.z - .23f, .11f, StationTeal);
            // train face: white rounded body, two dark windows, two lamps
            m.Box(p + V(0, 1.15f, -.21f), V(.8f, .95f, .02f), H("F4F2EA"));
            m.Box(p + V(-.18f, 1.35f, -.23f), V(.26f, .26f, .02f), StationTeal);
            m.Box(p + V(.18f, 1.35f, -.23f), V(.26f, .26f, .02f), StationTeal);
            m.Box(p + V(-.22f, .9f, -.23f), V(.12f, .12f, .02f), StationOrange);
            m.Box(p + V(.22f, .9f, -.23f), V(.12f, .12f, .02f), StationOrange);
        }

        // Pixel letters facing -Z (towards the rider), 3 x 5 cells, W 5 cells wide (the kit's 3-wide W reads as an H).
        static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['W'] = new[] { "10001", "10001", "10101", "11011", "10001" },
            ['E'] = new[] { "111", "100", "110", "100", "111" },
            ['L'] = new[] { "100", "100", "100", "100", "111" },
            ['A'] = new[] { "010", "101", "111", "101", "101" },
            ['R'] = new[] { "110", "101", "110", "101", "101" },
            ['D'] = new[] { "110", "101", "101", "101", "110" },
        };

        static void Letters(ComicMesh m, string text, float cx, float cy, float z, float px, Color c)
        {
            m.Surface = ComicSurface.Plain;
            float width = -px;
            foreach (char ch in text) width += ((Glyphs.TryGetValue(ch, out string[] g) ? g[0].Length : 3) + 1) * px;
            float x0 = cx - width * .5f, top = cy + 2.5f * px;
            foreach (char ch in text)
            {
                if (!Glyphs.TryGetValue(ch, out string[] g)) { x0 += 4 * px; continue; }
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < g[row].Length; col++)
                    {
                        if (g[row][col] != '1') continue;
                        float x = x0 + col * px, y = top - (row + 1) * px;
                        m.Quad(V(x + px, y, z), V(x, y, z), V(x, y + px, z), V(x + px, y + px, z), c, false);
                    }
                x0 += (g[0].Length + 1) * px;
            }
        }

        static void PlatformShelter(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { -7f, 0f, 7f }) m.Box(p + V(0, 1.6f, z), V(.25f, 3.2f, .25f), H("3E4C5A"));
            m.Box(p + V(-.3f, 3.3f, 0), V(4f, .25f, 16f), StationTeal);
            m.Box(p + V(-.3f, 3.5f, 0), V(3.6f, .15f, 15.6f), StationTealLight);
            m.Surface = ComicSurface.Wood;
            m.Box(p + V(.4f, .45f, -3.5f), V(.5f, .08f, 2.4f), H("A8774E"));
            m.Box(p + V(.4f, .45f, 3.5f), V(.5f, .08f, 2.4f), H("A8774E"));
        }

        /// <summary>Overhead line mast with a cantilever over the track, and the contact wire along z (flat road space).</summary>
        static void Mast(ComicMesh m, int s, float x, float z)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(s * x, K.Kerb + 3.4f, z), V(.3f, 6.8f, .3f), H("8A9198"));
            m.Beam(V(s * x, K.Kerb + 6.2f, z), V(s * (x - 4.6f), K.Kerb + 6.2f, z), .15f, H("8A9198"));
            m.Beam(V(s * x, K.Kerb + 5.2f, z), V(s * (x - 4f), K.Kerb + 6.1f, z), .1f, H("8A9198"));
            for (float zz = z - 10f; zz < z + 10f; zz += 4f) K.MinMaxBox(m, V(s * (x - 3f) - .02f, K.Kerb + 5.6f, zz), V(s * (x - 3f) + .02f, K.Kerb + 5.64f, zz + 4f), K.R.ink);
        }

        /// <summary>A suburban electric railcar (no brand): silver body, dark window band, teal-blue doors, a sloped nose
        /// with a dark windscreen when `nose`, bogies and a pantograph.</summary>
        static void TrainCar(ComicMesh m, Vector3 p, bool nose)
        {
            const float len = 18f;
            m.Surface = ComicSurface.Metal;
            m.Box(p + V(0, 2.2f, 0), V(3f, 3f, len), TrainSilver);
            m.Box(p + V(0, .9f, 0), V(2.9f, .5f, len - .2f), H("3A3F46"));
            m.Box(p + V(0, 3.82f, 0), V(2.7f, .3f, len - .4f), H("B9BEC2"));
            m.Surface = ComicSurface.Glass;
            foreach (int s in new[] { -1, 1 }) m.Box(p + V(s * 1.51f, 2.7f, 0), V(.04f, .9f, len - 1.2f), GlassDark);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (int s in new[] { -1, 1 })
                foreach (float z in new[] { -6f, 0f, 6f }) m.Box(p + V(s * 1.53f, 2.05f, z), V(.05f, 2.6f, 1.6f), TrainBlue);
            m.Box(p + V(0, 1.4f, 0), V(3.04f, .15f, len), K.R.yellow);
            if (nose)
            {
                m.Box(p + V(0, 2.1f, -len * .5f - .5f), V(2.9f, 2.6f, 1.2f), TrainSilver, Quaternion.Euler(-14f, 0, 0));
                m.Surface = ComicSurface.Glass;
                m.Box(p + V(0, 2.7f, -len * .5f - 1.05f), V(2.4f, 1f, .06f), GlassDark, Quaternion.Euler(-14f, 0, 0));
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(p + V(0, 1.3f, -len * .5f - 1f), V(2.9f, .5f, .3f), K.R.yellow);
                m.Beam(p + V(-.6f, 4f, 2f), p + V(.4f, 5f, 0), .08f, K.R.ink);
                m.Beam(p + V(.4f, 5f, 0), p + V(-.6f, 5.3f, -1.6f), .08f, K.R.ink);
            }
            Bogies(m, p, len * .5f - 2.5f);
        }

        // A small hatchback (no brand), front at +Z.
        internal static ComicMesh CarMesh(Color body)
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Box(V(0, .62f, 0), V(1.8f, .6f, 4.2f), body);
            m.Box(V(0, 1.15f, -.3f), V(1.6f, .5f, 2.4f), body);
            m.Box(V(0, 1.42f, -.3f), V(1.62f, .06f, 2.3f), Color.Lerp(body, K.R.ink, .3f));
            m.Surface = ComicSurface.Glass;
            m.Box(V(0, 1.15f, .92f), V(1.5f, .42f, .06f), GlassDark);
            m.Box(V(0, 1.15f, -1.52f), V(1.5f, .4f, .06f), GlassDark);
            foreach (int s in new[] { -1, 1 }) m.Box(V(s * .81f, 1.15f, -.3f), V(.04f, .4f, 2.1f), GlassDark);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, .42f, 2.12f), V(1.84f, .2f, .1f), K.R.ink);
            m.Box(V(0, .42f, -2.12f), V(1.84f, .2f, .1f), K.R.ink);
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(V(s * .62f, .72f, 2.11f), V(.36f, .14f, .04f), K.R.cream);
                m.Box(V(s * .66f, .76f, -2.11f), V(.3f, .16f, .04f), K.R.red);
            }
            m.Surface = ComicSurface.Rubber;
            foreach (float z in new[] { 1.35f, -1.35f })
                foreach (int s in new[] { -1, 1 }) m.Cylinder(V(s * .82f, .33f, z), .33f, .26f, K.R.ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            return m;
        }

        /// <summary>v0.6: the side wall of the shop beyond the village square faces riders and was blank: two upper
        /// windows, a teal painted band with the place name, a low planter along its foot (the wall is at z, facing -Z).</summary>
        static void SideWallMural(ComicMesh m, float x0, float x1, float baseY, float z)
        {
            float zc = z - .04f, xc = (x0 + x1) * .5f;
            void Panel(float a, float b, float y0, float y1, float off, Color c, ComicSurface surface)
            {
                m.Surface = surface;
                K.Face(m, V(a, baseY + y0, z - off), V(b, baseY + y0, z - off), V(b, baseY + y1, z - off), V(a, baseY + y1, z - off), c, Vector3.back);
            }
            foreach (float x in new[] { Mathf.Lerp(x0, x1, .27f), Mathf.Lerp(x0, x1, .73f) })
            {
                Panel(x - 1f, x + 1f, 4.45f, 6.35f, .04f, H("F4F2EA"), ComicSurface.PaintedMetal);
                Panel(x - .85f, x + .85f, 4.6f, 6.2f, .06f, GlassDark, ComicSurface.Glass);
                Panel(x - .04f, x + .04f, 4.6f, 6.2f, .07f, H("F4F2EA"), ComicSurface.PaintedMetal);
                m.Surface = ComicSurface.Concrete;
                K.MinMaxBox(m, V(x - 1.1f, baseY + 4.3f, z - .22f), V(x + 1.1f, baseY + 4.45f, z), H("F4F2EA"));
            }
            Panel(x0 + .9f, x1 - .9f, 1.7f, 3.5f, .04f, StationTeal, ComicSurface.PaintedMetal);
            Panel(x0 + .9f, x1 - .9f, 1.55f, 1.7f, .05f, StationOrange, ComicSurface.PaintedMetal);
            Letters(m, "WELLARD", xc, baseY + 2.6f, zc - .03f, .2f, H("F4F2EA"));
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(x0 + .9f, baseY, z - 1.1f), V(x1 - .9f, baseY + .55f, z - .1f), Ret);
            K.MinMaxBox(m, V(x0 + .8f, baseY + .55f, z - 1.2f), V(x1 - .8f, baseY + .65f, z), RetCap);
            for (float x = x0 + 2f; x < x1 - 1.5f; x += 2.4f) P.Shrub(m, V(x, baseY + .6f, z - .6f), .8f, Color.Lerp(P.LeafGreen, Lawn, .35f));
        }

        static CqChunkDraft WlVillage()
        {
            // Wellard village: two-storey shops with awnings and cafe tables, the village square with a clock tower.
            var d = new CqChunkDraft("Wellard_Village", WellardDrop);
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, Pave, KerbCap, false);
            Ground(d, -1, 0, 40, 0, 72, Pave, K.Kerb - .02f, ComicSurface.Paving);
            Ground(d, 1, 0, 40, 0, 72, Pave, K.Kerb - .02f, ComicSurface.Paving);
            for (int i = 0; i < 4; i++) Shop(d, new CqLot(-1, i * 10f, i * 10f + 10f, K.Edge + .3f, 12f, d.Base(i * 10f + 5f)), i);
            Shop(d, new CqLot(1, 0, 12, K.Edge + .3f, 12f, d.Base(6f)), 5);
            Shop(d, new CqLot(1, 28, 40, K.Edge + .3f, 12f, d.Base(34f)), 6);
            SideWallMural(d.Frontage, K.Edge + .6f, K.Edge + 12f, d.Base(34f), 28.1f);
            Shop(d, new CqLot(1, 12, 28, K.Edge + 16f, 12f, d.Base(20f)), 7);
            // the square: lighter paving, a clock tower, a shade tree, benches
            d.Road.Surface = ComicSurface.Paving;
            K.SideBox(d.Road, 1, 12.3f, 27.7f, -2.4f, K.Kerb - .005f, .3f, 15.7f, H("E3D6BC"));
            ClockTower(d.Landmark, V(K.Edge + 5f, d.Ground(20f), 20f));
            K.PlaneTree(d.Frontage, null, V(K.Edge + 10f, d.Ground(15.5f), 15.5f), .8f);
            K.PlaneTree(d.Frontage, null, V(K.Edge + 10.5f, d.Ground(25f), 25f), .7f);
            K.PropFromKit(d.Frontage, null, "Bench", V(K.Edge + 3f, d.Ground(14f), 14f), -90f);
            K.PropFromKit(d.Frontage, null, "Bench", V(K.Edge + 3f, d.Ground(26f), 26f), -90f);
            // cafe tables on the outer sidewalk
            K.CafeUmbrella(d.Props, d.Collisions, V(-(K.Edge - 1.5f), d.Ground(6f), 6f), SailYellow);
            K.CafeUmbrella(d.Props, d.Collisions, V(-(K.Edge - 1.5f), d.Ground(27f), 27f), H("F4F2EA"));
            K.CafeUmbrella(d.Props, d.Collisions, V(K.Edge - 1.5f, d.Ground(5f), 5f), SailTeal);
            K.CafeUmbrella(d.Props, d.Collisions, V(K.Edge - 1.5f, d.Ground(35f), 35f), SailRed);
            K.Lamp(d.Props, d.Collisions, -1, d.Ground(16f), 16f);
            K.Lamp(d.Props, d.Collisions, 1, d.Ground(30f), 30f);
            K.PlaneTree(d.Props, d.Collisions, V(-(K.Edge - 1.1f), d.Ground(18.5f), 18.5f), .62f);
            K.PlaneTree(d.Props, d.Collisions, V(K.Edge - 1.1f, d.Ground(22f), 22f), .62f);
            d.StandardSlots();
            return d;
        }

        /// <summary>Village shop: two storeys, glass shopfront, a cantilever awning over the footpath, rendered upper
        /// floor with windows, a parapet (some with a small gable).</summary>
        static void Shop(CqChunkDraft d, CqLot l, int v)
        {
            ComicMesh m = d.Frontage;
            Color[] walls = { H("EDE3CF"), H("D9C3A0"), H("C97B5A"), H("B9C7B0"), H("E8D5BA"), H("D6A77A"), H("A9B4B8"), H("EDE3CF") };
            Color[] awnings = { H("3E4A52"), StationTeal, H("8E3B22"), H("3E4A52"), H("2F6B66"), H("3E4A52"), H("6A4A8A"), H("3E4A52") };
            Color[] signs = { H("2F6B66"), H("C8452F"), H("F3B23A"), H("2F5FA8"), H("E07A2A"), H("6A4A8A"), H("2F8A7A"), H("C8452F") };
            Color wall = walls[v % walls.Length], awn = awnings[v % awnings.Length], trim = H("F4F2EA"), sign = signs[v % signs.Length];
            Color pier = Color.Lerp(wall, trim, .35f), dark = Color.Lerp(wall, K.R.ink, .45f), glass = H("33505E");
            const float h = 7.2f, gf = 3.5f;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, l.W, -1.4f, .2f, 0, -l.Depth, K.R.ink);
            // the ground floor sits back behind its frame, the upper floor comes forward to the pilasters
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, .1f, l.W - .1f, .2f, gf, -.75f, -l.Depth, wall);
            K.LBox(m, l, .1f, l.W - .1f, gf, h, -.3f, -l.Depth, wall);
            foreach (float a in new[] { .1f, l.W - .7f })
            {
                K.LBox(m, l, a, a + .6f, .2f, h - .2f, .08f, -.5f, pier);                              // pilasters
                m.Surface = ComicSurface.Concrete;
                K.LBox(m, l, a - .03f, a + .63f, .2f, .8f, .12f, -.5f, dark);                          // their plinths
                m.Surface = ComicSurface.Plaster;
            }
            // shopfront: stall riser, glass with mullions, a recessed door, transom bar
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, .7f, l.W - .7f, .2f, .7f, -.35f, -.75f, dark);
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, .7f, l.W - .7f, .7f, 2.75f, -.73f, glass);
            m.Surface = ComicSurface.PaintedMetal;
            float doorA = v % 2 == 0 ? 1.2f : l.W - 2.6f;
            K.LQuad(m, l, doorA, doorA + 1.4f, .2f, 2.75f, -.71f, H("1A2028"));
            for (float a = .7f + (l.W - 1.4f) / 3f; a < l.W - .8f; a += (l.W - 1.4f) / 3f) K.LBox(m, l, a - .05f, a + .05f, .7f, 2.75f, -.55f, -.74f, K.R.ink);
            K.LBox(m, l, .7f, l.W - .7f, 2.75f, 2.95f, -.4f, -.75f, K.R.ink);
            // awning on two ties with a coloured valance (the sign band), ink underside
            K.LBox(m, l, .3f, l.W - .3f, 3.05f, 3.3f, 2.4f, -.3f, awn);
            K.LBox(m, l, .3f, l.W - .3f, 2.8f, 3.3f, 2.5f, 2.3f, sign);
            K.LBox(m, l, .3f, l.W - .3f, 2.98f, 3.05f, 2.35f, -.3f, K.R.ink);
            foreach (float a in new[] { 1f, l.W - 1f }) m.Beam(l.P(a, 3.3f, 2.2f), l.P(a, 4.6f, -.3f), .07f, K.R.ink);
            // string course, a sign panel with a simple icon (no words)
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, .1f, l.W - .1f, gf, gf + .18f, .1f, -.3f, trim);
            m.Surface = ComicSurface.PaintedMetal;
            float sa = l.W * .5f;
            K.LBox(m, l, sa - 1.3f, sa + 1.3f, gf + .35f, gf + .95f, -.18f, -.3f, sign);
            K.LBox(m, l, sa - .25f, sa + .15f, gf + .45f, gf + .85f, -.14f, -.18f, trim);          // a cup
            K.LBox(m, l, sa + .15f, sa + .3f, gf + .55f, gf + .75f, -.14f, -.18f, trim);
            // upper floor: framed windows with sills and hoods, or French doors on a balcony
            int n = l.W > 11f ? 3 : 2;
            for (int i = 0; i < n; i++)
            {
                float a = .7f + (l.W - 1.4f) * (i + .5f) / n, w = Mathf.Min(1.5f, (l.W - 1.4f) / n - .9f);
                bool balcony = v % 3 == 1 && i == n / 2;
                float u0 = balcony ? gf + .25f : 4.9f, u1 = 6.4f;
                m.Surface = ComicSurface.Plaster;
                K.LBox(m, l, a - w * .5f - .14f, a + w * .5f + .14f, u0 - .1f, u1 + .1f, -.16f, -.3f, trim);   // frame
                m.Surface = ComicSurface.Glass;
                K.LQuad(m, l, a - w * .5f, a + w * .5f, u0, u1, -.15f, glass);
                m.Surface = ComicSurface.PaintedMetal;
                K.LBox(m, l, a - .04f, a + .04f, u0, u1, -.12f, -.16f, trim);
                m.Surface = ComicSurface.Concrete;
                K.LBox(m, l, a - w * .5f - .25f, a + w * .5f + .25f, u1 + .1f, u1 + .3f, -.02f, -.3f, v % 2 == 0 ? trim : dark);   // hood
                if (!balcony) { K.LBox(m, l, a - w * .5f - .2f, a + w * .5f + .2f, u0 - .25f, u0 - .1f, .02f, -.3f, trim); continue; }
                K.LBox(m, l, a - w * .5f - .6f, a + w * .5f + .6f, gf + .1f, gf + .25f, 1.1f, -.3f, trim);                   // balcony slab
                m.Surface = ComicSurface.PaintedMetal;
                K.LBox(m, l, a - w * .5f - .6f, a + w * .5f + .6f, gf + 1.2f, gf + 1.28f, 1.1f, 1f, K.R.ink);                // rail
                for (float b = a - w * .5f - .55f; b <= a + w * .5f + .56f; b += .3f) K.LBox(m, l, b - .02f, b + .02f, gf + .25f, gf + 1.2f, 1.08f, 1.04f, K.R.ink);
            }
            // cornice, parapet with a cap, an occasional pediment, rooftop plant
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, -.05f, l.W + .05f, h - .35f, h, .25f, -.3f, trim);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, .1f, l.W - .1f, h, h + .7f, -.1f, -.4f, wall);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, l.W, h + .7f, h + .85f, 0f, -.5f, trim);
            if (v % 3 == 0) K.LGable(m, l, l.W * .3f, l.W * .7f, h + .85f, h + 1.9f, -.05f, -.45f, wall);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, l.W * .25f, l.W * .25f + 1.6f, h, h + 1f, -4f, -5.2f, K.R.cream);
            K.LBox(m, l, l.W * .6f, l.W * .6f + .5f, h, h + 1.4f, -6f, -6.5f, SteelDark);
            d.Wall(l, h);
        }

        static void ClockTower(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Brick;
            m.Box(p + V(0, .3f, 0), V(1.8f, .6f, 1.8f), H("B8694A"));
            m.Box(p + V(0, 3f, 0), V(1.2f, 4.8f, 1.2f), H("C97B5A"));
            m.Surface = ComicSurface.Plaster;
            m.Box(p + V(0, 5.9f, 0), V(1.5f, 1.4f, 1.5f), H("F4F2EA"));
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 7.1f, 0), 1.05f, 1f, StationTeal, 4, .05f, Quaternion.Euler(0, 45f, 0));
            K.Clock(m, p + V(0, 5.9f, -.76f), Quaternion.Euler(0, 180f, 0), .5f, H("F4F2EA"), K.R.ink);
            K.Clock(m, p + V(-.76f, 5.9f, 0), Quaternion.Euler(0, -90f, 0), .5f, H("F4F2EA"), K.R.ink);
        }

        static CqChunkDraft WlPark()
        {
            // A shared path through the park (no cars): the playground under shade sails on the left; the oval with
            // footy goal posts, gums and a BBQ shelter on the right.
            var d = new CqChunkDraft("Wellard_Park", WellardDrop) { Type = RoadType.PedestrianMall };
            PathSurface(d, H("D8D2C4"), ComicSurface.Concrete, Lawn, ComicSurface.Foliage, KerbCap, true, 0f);
            StreetStubs(d, Pave, KerbCap);
            for (int s = -1; s <= 1; s += 2) Ground(d, s, 0, 40, 0, 72, Lawn, K.Kerb - .02f, ComicSurface.Foliage);
            // mowing stripes on the oval
            d.Road.Surface = ComicSurface.Plain;
            for (float x = K.Edge + 14f; x < K.Edge + 60f; x += 8f)
                foreach ((float a, float b) in Pieces(40f, 8f)) K.Flat(d.Road, x, x + 4f, x, x + 4f, a, b, K.Kerb + .005f, LawnLight);
            // the park gate: limestone pillars with cream caps and a low wall either side, where the street ends
            for (int s = -1; s <= 1; s += 2)
            {
                LimestoneWall(d.SlopedFrontage, s, 1.2f, 9f, K.Edge + .5f, .5f);
                d.Frontage.Surface = ComicSurface.Concrete;
                d.Frontage.Box(V(s * (K.Edge + .5f), d.Ground(.8f) + .7f, .8f), V(.9f, 1.4f, .9f), Limestone);
                d.Frontage.Box(V(s * (K.Edge + .5f), d.Ground(.8f) + 1.47f, .8f), V(1.05f, .14f, 1.05f), RetCap);
            }
            Playground(d.Landmark, V(-(K.Edge + 9f), d.Ground(20f), 20f));
            d.Road.Surface = ComicSurface.Rubber;
            K.SideBox(d.Road, -1, 9f, 31f, -2.4f, K.Kerb + .01f, 2.5f, 15.5f, Softfall);
            d.Road.Surface = ComicSurface.Concrete;
            K.SideBox(d.Road, -1, 8.6f, 31.4f, -2.4f, K.Kerb + .05f, 2.1f, 2.5f, KerbCap);
            GoalPosts(d.Landmark, V(K.Edge + 40f, d.Ground(20f), 20f));
            for (float z = 4f; z < 40f; z += 12f) P.Gum(d.Frontage, V(K.Edge + 5f + (z % 24f) * .2f, d.Ground(z), z), 11f, (int)z + 300);
            P.Gum(d.Frontage, V(-(K.Edge + 3.5f), d.Ground(4f), 4f), 10f, 311);
            P.Gum(d.Frontage, V(-(K.Edge + 4f), d.Ground(36f), 36f), 12f, 312);
            Shelter(d.Frontage, V(K.Edge + 14f, d.Ground(33f), 33f));
            K.PropFromKit(d.Frontage, null, "Bench", V(K.Edge + 1.4f, d.Ground(10f), 10f), -90f);
            K.PropFromKit(d.Frontage, null, "Bench", V(-(K.Edge + 1.4f), d.Ground(34f), 34f), 90f);
            K.Lamp(d.Props, d.Collisions, 1, d.Ground(22f), 22f);
            d.StandardSlots();
            return d;
        }

        /// <summary>Playground: a tower with a roof, a yellow slide, a swing set, three shade sails on tall poles.</summary>
        static void Playground(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -1f, 1f })
                foreach (float z in new[] { -1f, 1f }) m.Box(p + V(x, 1.6f, z), V(.16f, 3.2f, .16f), SailRed);
            m.Surface = ComicSurface.Wood;
            m.Box(p + V(0, 1.5f, 0), V(2.3f, .14f, 2.3f), H("A8774E"));
            m.Surface = ComicSurface.Plastic;
            m.Cylinder(p + V(0, 3.6f, 0), 1.7f, .9f, SailTeal, 4, .05f, Quaternion.Euler(0, 45f, 0));
            m.Box(p + V(0, 1.1f, 2.6f), V(.9f, .1f, 3.2f), SailYellow, Quaternion.Euler(-24f, 0, 0));          // slide
            foreach (int s in new[] { -1, 1 }) m.Box(p + V(s * .48f, 1.3f, 2.6f), V(.1f, .3f, 3.2f), Color.Lerp(SailYellow, K.R.ink, .2f), Quaternion.Euler(-24f, 0, 0));
            m.Surface = ComicSurface.PaintedMetal;
            m.Beam(p + V(-1f, 0, -1f), p + V(-1.2f, 1.5f, -2.6f), .1f, StationTeal);                             // ladder
            // swing set
            Vector3 sw = p + V(4.5f, 0, -1f);
            foreach (float z in new[] { -1.6f, 1.6f })
            {
                m.Beam(sw + V(-1f, 0, z), sw + V(0, 2.6f, z), .12f, SailRed);
                m.Beam(sw + V(1f, 0, z), sw + V(0, 2.6f, z), .12f, SailRed);
            }
            m.Beam(sw + V(0, 2.6f, -1.7f), sw + V(0, 2.6f, 1.7f), .12f, SailRed);
            foreach (float z in new[] { -.7f, .7f })
            {
                m.Box(sw + V(0, 1.6f, z - .2f), V(.03f, 2f, .03f), K.R.ink);
                m.Box(sw + V(0, 1.6f, z + .2f), V(.03f, 2f, .03f), K.R.ink);
                m.Box(sw + V(0, .58f, z), V(.25f, .06f, .5f), K.R.ink);
            }
            // shade sails: triangles on tall ink poles, both faces
            (Vector3 a, Vector3 b, Vector3 c, Color col)[] sails =
            {
                (V(-4f, 5.2f, -4f), V(4f, 4.4f, -5f), V(0f, 4.8f, 3f), SailYellow),
                (V(-5f, 4.6f, 1f), V(-1f, 5.4f, 6f), V(-6f, 5.6f, 7f), SailTeal),
                (V(2f, 5f, 2f), V(7f, 4.4f, -3f), V(7.5f, 5.6f, 4f), SailRed),
            };
            foreach (var s in sails)
            {
                m.Surface = ComicSurface.Fabric;
                K.TriFacing(m, p + s.a, p + s.b, p + s.c, s.col, Vector3.up);
                K.TriFacing(m, p + s.a + V(0, -.04f, 0), p + s.b + V(0, -.04f, 0), p + s.c + V(0, -.04f, 0), Color.Lerp(s.col, K.R.ink, .25f), Vector3.down);
                m.Surface = ComicSurface.PaintedMetal;
                foreach (Vector3 corner in new[] { s.a, s.b, s.c }) m.Box(p + V(corner.x, corner.y * .5f, corner.z), V(.16f, corner.y + .2f, .16f), H("3E4C5A"));
            }
        }

        static void GoalPosts(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach ((float z, float h) in new[] { (-9.6f, 6f), (-3.2f, 10f), (3.2f, 10f), (9.6f, 6f) })
            {
                m.Cylinder(p + V(0, h * .5f, z), .16f, h, FlareWhite, 8);
                m.Cylinder(p + V(0, 1f, z), .2f, 2f, z * z < 20f ? SailRed : StationTeal, 8);
            }
        }

        /// <summary>Paperbark (melaleuca): two or three leaning cream papery trunks with brown patches, wispy olive crowns
        /// of several flattened blobs.</summary>
        static void Paperbark(ComicMesh m, Vector3 p, float h, int seed)
        {
            var r = new System.Random(seed * 131 + 7);
            float F() => (float)r.NextDouble();
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .08f, 0), .55f, .16f, K.R.ink, 8, .9f);
            int stems = 2 + seed % 2;
            for (int i = 0; i < stems; i++)
            {
                float a = (i * 137f + seed * 40f) * Mathf.Deg2Rad, lean = .12f + F() * .18f;
                Vector3 dir = new Vector3(Mathf.Cos(a) * lean, 1f, Mathf.Sin(a) * lean).normalized;
                float len = h * (.6f + F() * .25f);
                Vector3 top = p + dir * len;
                m.Surface = ComicSurface.Wood;
                P.Rod(m, p + V(0, .1f, 0), Vector3.Lerp(p, top, .55f), .26f - i * .04f, PaperTrunk, .85f);
                P.Rod(m, Vector3.Lerp(p, top, .55f), top, .2f - i * .03f, i % 2 == 0 ? PaperTrunk : Color.Lerp(PaperTrunk, PaperPatch, .4f), .7f);
                m.Box(Vector3.Lerp(p, top, .3f) + V(.18f, 0, 0), V(.08f, len * .18f, .22f), PaperPatch, Quaternion.FromToRotation(Vector3.up, dir));
                m.Surface = ComicSurface.Foliage;
                // wispy crown: a few soft blobs at different heights, hanging a little at the edges
                for (int k = 0; k < 4; k++)
                {
                    float ang = (k * 95f + seed * 30f + i * 50f) * Mathf.Deg2Rad, rad = k == 0 ? 0f : 1.1f + F() * .5f;
                    Vector3 c = top + V(Mathf.Cos(ang) * rad, (k == 0 ? .5f : -.35f) + F() * .3f, Mathf.Sin(ang) * rad);
                    m.Ellipsoid(c, V(2f + F() * .8f, 1.5f + F() * .6f, 1.9f + F() * .8f), k == 0 ? PaperLeafLight : k % 2 == 1 ? PaperLeaf : PaperLeafDark);
                }
            }
        }

        /// <summary>A clump of reeds (rushes): thin upright blades in two greens with a few brown heads.</summary>
        static void Reeds(ComicMesh m, Vector3 p, int seed)
        {
            var r = new System.Random(seed * 17 + 3);
            float F() => (float)r.NextDouble();
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < 9; i++)
            {
                Vector3 b = p + V((F() - .5f) * 1.6f, 0, (F() - .5f) * 1.6f);
                float h = 1.2f + F() * 1f;
                m.Box(b + V(0, h * .5f, 0), V(.08f, h, .14f), i % 3 == 0 ? ReedDark : Reed, Quaternion.Euler((F() - .5f) * 16f, F() * 180f, (F() - .5f) * 16f));
                if (i % 3 == 1) m.Box(b + V(0, h + .15f, 0), V(.12f, .35f, .12f), H("7A5C44"));
            }
        }

        static CqChunkDraft WlWetland()
        {
            // The Spectacles: a timber boardwalk (no cars) over paperbark wetland; a boardwalk ramp across the whole
            // path, then 5 m of open water (SPLASH!), then the boardwalk again. Black swans on the water.
            var d = new CqChunkDraft("Wellard_Wetland", WellardDrop) { Type = RoadType.PedestrianMall };
            float z0 = BoardwalkRampZ, lip = z0 + BoardwalkRise + BoardwalkTop, far = lip + WetlandGap;
            PathSurface(d, Boards, ComicSurface.Wood, BoardsLight, ComicSurface.Wood, Rail, false, .6f, lip, far);
            StreetStubs(d, Pave, KerbCap);
            K.Water(d.Road, -(K.Edge + 40f), K.Edge + 40f, 0, 40, PondLevel, Pond, PondLight, 21);
            d.Road.Surface = ComicSurface.Plain;
            var rr = new System.Random(77);
            float F() => (float)rr.NextDouble();
            for (int i = 0; i < 26; i++)
            {
                int s = i % 2 == 0 ? -1 : 1;
                float x = K.Edge + .5f + F() * 18f, z = F() * 37f, w = 1.5f + F() * 4f, l = 1.5f + F() * 3f;
                K.Flat(d.Road, s * x, s * (x + w), s * (x + w * .2f), s * (x + w * .9f), z, Mathf.Min(40f, z + l), PondLevel + .02f + i * .0005f, i % 3 == 0 ? H("B9D45A") : Duckweed);
            }
            // banks beyond the water
            for (int s = -1; s <= 1; s += 2) Ground(d, s, 0, 40, 40f, 72f, H("7FA04E"), -.4f, ComicSurface.Foliage);
            // piles, railings (broken at the gap), the ramp
            ComicMesh f = d.SlopedFrontage;
            f.Surface = ComicSurface.Wood;
            for (float z = 1f; z < 40f; z += 2.5f)
            {
                if (z > lip - .5f && z < far + .5f) continue;
                foreach (float x in new[] { -K.Edge + .2f, -K.Half, K.Half, K.Edge - .2f }) f.Box(V(x, (PondLevel - .6f - .4f) * .5f, z), V(.25f, -.4f - PondLevel + .6f, .25f), H("5E4B3A"));
            }
            foreach (float z in new[] { lip + .2f, far - .2f })
                foreach (float x in new[] { -K.Edge + .2f, -2f, 2f, K.Edge - .2f }) f.Box(V(x, (PondLevel - .6f + .1f) * .5f, z), V(.28f, -PondLevel + .6f + .1f, .28f), H("5E4B3A"));
            for (int s = -1; s <= 1; s += 2)
            {
                BoardwalkRail(f, s, 0f, lip - .1f);
                BoardwalkRail(f, s, far + .1f, 40f);
            }
            var ramp = new CqRampSpec { Name = "BoardwalkRamp", X = 0f, Z = z0, Width = 2f * K.Edge, Rise = BoardwalkRise, Height = BoardwalkHeight, Top = BoardwalkTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(ramp);
            d.Gaps.Add(new CqGapSpec { Name = "OpenWater", Z = lip, Length = WetlandGap, Width = 0f, Word = "SPLASH", Water = true });
            var deck = new ComicMesh();
            K.RampMesh(deck, ramp, BoardsLight, Color.Lerp(Boards, K.R.ink, .3f), false, PondLevel - .3f);
            for (int s = -1; s <= 1; s += 2)
            {
                deck.Surface = ComicSurface.Wood;
                deck.Beam(V(s * (K.Edge + .1f), 1.1f, z0), V(s * (K.Edge + .1f), BoardwalkHeight + 1.1f, z0 + BoardwalkRise), .12f, Rail);
                deck.Beam(V(s * (K.Edge + .1f), BoardwalkHeight + 1.1f, z0 + BoardwalkRise), V(s * (K.Edge + .1f), BoardwalkHeight + 1.1f, lip - .2f), .12f, Rail);
                deck.Box(V(s * (K.Edge + .1f), (BoardwalkHeight + 1.1f + PondLevel) * .5f, lip - .2f), V(.22f, BoardwalkHeight + 1.1f - PondLevel, .22f), H("5E4B3A"));
            }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            d.KeepOut.Add(KeepOutBox(d, -1, z0, far)); d.KeepOut.Add(KeepOutBox(d, 1, z0, far));
            // paperbarks standing in the water, reeds, swans
            int k = 0;
            foreach ((float x, float z, float h) in new[] { (-12f, 4f, 9f), (-18f, 17f, 11f), (-11f, 30f, 8f), (-24f, 36f, 10f), (13f, 8f, 10f), (20f, 24f, 9f), (12f, 36f, 11f), (26f, 2f, 12f) })
                Paperbark(Mathf.Abs(x) < 20f ? d.Frontage : d.Deep, V(x, d.Y(z) + PondLevel, z), h, k++);
            k = 0;
            foreach ((float x, float z) in new[] { (-9.5f, 12f), (-10f, 22f), (9.6f, 2f), (10f, 17f), (9.4f, 31f), (-9.6f, 38f), (-15f, 27f), (16f, 13f) })
                Reeds(d.Frontage, V(x, d.Y(z) + PondLevel, z), k++);
            d.Props.Add(SwanMesh(), V(-15f, d.Y(9f) + PondLevel - .35f, 9f), Quaternion.Euler(0, 140f, 0), Vector3.one);
            d.Props.Add(SwanMesh(), V(17f, d.Y(29f) + PondLevel - .35f, 29f), Quaternion.Euler(0, 200f, 0), Vector3.one);
            d.Props.Add(DuckMesh(), V(12.5f, d.Y(21f) + PondLevel - .15f, 21f), Quaternion.Euler(0, 60f, 0), Vector3.one);
            Paperbark(d.Deep, V(-(K.Edge + 46f), d.Ground(10f) - .4f, 10f), 12f, 9);
            Paperbark(d.Deep, V(K.Edge + 48f, d.Ground(28f) - .4f, 28f), 13f, 10);
            d.GiftSlots.Add(new Vector2(0f, 30f));
            d.Slot(-2.6f, 36f); d.Slot(2.6f, 36f);
            return d;
        }

        static void BoardwalkRail(ComicMesh m, int s, float z0, float z1)
        {
            float x = s * (K.Edge + .1f);
            m.Surface = ComicSurface.Wood;
            for (float z = z0 + .2f; z <= z1 - .1f; z += 2f) m.Box(V(x, K.Kerb + .55f, z), V(.16f, 1.1f, .16f), Rail);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.MinMaxBox(m, V(x - .08f, K.Kerb + 1.02f, z), V(x + .08f, K.Kerb + 1.14f, e), Rail);
                K.MinMaxBox(m, V(x - .05f, K.Kerb + .5f, z), V(x + .05f, K.Kerb + .6f, e), Color.Lerp(Rail, K.R.ink, .2f));
            }
        }

        // ---------------------------------------------------------------- Wellard obstacles and animals

        // Playground spring rider: an ink base plate, a steel coil and a bright red duck with yellow handles.
        static ComicMesh SpringerMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .04f, 0), .38f, .08f, K.R.ink, 8);
            for (int i = 0; i < 5; i++) m.Cylinder(V(0, .13f + i * .07f, 0), .17f, .045f, i % 2 == 0 ? SteelDark : Steel, 8);
            m.Surface = ComicSurface.Plastic;
            m.Ellipsoid(V(0, .64f, .05f), V(.38f, .34f, .82f), SailRed);
            m.Ellipsoid(V(0, .93f, -.34f), V(.28f, .3f, .3f), SailRed);
            m.Box(V(0, .9f, -.53f), V(.13f, .08f, .16f), K.R.yellow);
            foreach (int s in new[] { -1, 1 }) m.Box(V(s * .12f, .99f, -.4f), V(.03f, .06f, .06f), K.R.ink);
            m.Cylinder(V(0, .96f, -.16f), .04f, .5f, K.R.yellow, 6, 1f, Quaternion.Euler(0, 0, 90));
            m.Box(V(0, .83f, .14f), V(.3f, .06f, .34f), SailYellow);
            m.Box(V(0, .52f, -.06f), V(.62f, .05f, .12f), K.R.ink);
            m.Box(V(0, .78f, .48f), V(.14f, .2f, .12f), SailYellow);
            return m;
        }

        // Two footies (red-brown leather, white laces) and a yellow training cone.
        static ComicMesh FootballsMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Rubber };
            foreach ((float x, float z, float yaw) in new[] { (-.35f, -.1f, 20f), (.2f, .2f, -50f) })
            {
                Quaternion q = Quaternion.Euler(0, yaw, 0);
                m.Ellipsoid(V(x, .2f, z), q * V(.34f, .34f, .56f), H("A8432C"));
                m.Box(V(x, .37f, z), V(.06f, .03f, .2f), FlareWhite, q);
            }
            m.Surface = ComicSurface.Plastic;
            m.Cylinder(V(.45f, .2f, -.25f), .2f, .4f, K.R.yellow, 8, .15f);
            m.Cylinder(V(.45f, .02f, -.25f), .26f, .04f, K.R.yellow, 8);
            return m;
        }

        // A pair of wheelie bins: dark green bodies, red (rubbish) and yellow (recycling) lids, ink wheels.
        static ComicMesh BinsMesh()
        {
            var m = new ComicMesh();
            for (int i = 0; i < 2; i++)
            {
                float x = (i - .5f) * .7f;
                m.Surface = ComicSurface.Plastic;
                m.Box(V(x, .5f, 0), V(.6f, .9f, .66f), H("2F5D3A"));
                m.Box(V(x, .98f, -.02f), V(.64f, .07f, .74f), i == 0 ? K.R.red : K.R.yellow);
                m.Box(V(x, .95f, .36f), V(.5f, .08f, .08f), K.R.ink);
                m.Box(V(x, .55f, -.34f), V(.3f, .2f, .02f), Color.Lerp(H("2F5D3A"), FlareWhite, .4f));
                m.Surface = ComicSurface.Rubber;
                foreach (int s in new[] { -1, 1 }) m.Cylinder(V(x + s * .26f, .1f, .3f), .1f, .08f, K.R.ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            }
            return m;
        }

        // Timber park bollard with a white reflective band and a rounded cap.
        static ComicMesh BollardMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            m.Box(V(0, .45f, 0), V(.24f, .9f, .24f), H("8C6A48"));
            m.Cylinder(V(0, .93f, 0), .15f, .1f, H("6E5238"), 8, .6f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, .72f, 0), V(.26f, .1f, .26f), FlareWhite);
            m.Box(V(0, .04f, 0), V(.34f, .08f, .34f), K.R.ink);
            return m;
        }

        // Black swan facing -Z, feet at the pivot: chunky black body, curly white-tipped wing feathers, an S-curved
        // neck of round segments, red beak with a white band.
        static ComicMesh SwanMesh()
        {
            var m = new ComicMesh();
            Color swan = H("1E2128"), feather = H("2B2E36"), beak = H("D8423A"), white = FlareWhite;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (int s in new[] { -1, 1 }) { m.Box(V(s * .09f, .11f, .06f), V(.07f, .2f, .07f), H("2A2A2E")); m.Box(V(s * .09f, .03f, -.02f), V(.14f, .06f, .2f), K.R.ink); }
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(V(0, .43f, .12f), V(.5f, .42f, .84f), swan);
            m.Ellipsoid(V(0, .56f, .3f), V(.42f, .26f, .52f), feather);
            for (int i = 0; i < 3; i++) m.Box(V((i - 1) * .11f, .7f, .46f), V(.09f, .07f, .3f), feather, Quaternion.Euler(-35f, (i - 1) * 15f, 0));
            foreach (int s in new[] { -1, 1 }) m.Box(V(s * .2f, .45f, .46f), V(.07f, .09f, .22f), white);
            Vector3 n0 = V(0, .5f, -.22f), n1 = V(0, .74f, -.38f), n2 = V(0, .98f, -.36f), n3 = V(0, 1.14f, -.26f);
            P.Rod(m, n0, n1, .09f, swan, .9f);
            P.Rod(m, n1, n2, .08f, swan, .9f);
            P.Rod(m, n2, n3, .075f, swan, .95f);
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(V(0, 1.18f, -.3f), V(.15f, .15f, .21f), swan);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, 1.16f, -.44f), V(.07f, .06f, .15f), beak);
            m.Box(V(0, 1.165f, -.515f), V(.075f, .065f, .03f), white);
            foreach (int s in new[] { -1, 1 }) m.Box(V(s * .072f, 1.2f, -.34f), V(.02f, .035f, .035f), beak);
            return m;
        }

        // Pacific black duck facing -Z: mottled brown body, darker back, pale face with dark stripes, a teal wing
        // patch, grey bill, orange-brown feet.
        static ComicMesh DuckMesh()
        {
            var m = new ComicMesh();
            Color brown = H("6B4E36"), dark = H("4A3626"), face = H("D9C8A8");
            m.Surface = ComicSurface.PaintedMetal;
            foreach (int s in new[] { -1, 1 }) { m.Box(V(s * .07f, .06f, 0), V(.04f, .12f, .04f), H("C8823A")); m.Box(V(s * .07f, .02f, -.05f), V(.1f, .04f, .12f), H("C8823A")); }
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(V(0, .24f, .04f), V(.32f, .26f, .52f), brown);
            m.Ellipsoid(V(0, .31f, .1f), V(.26f, .14f, .4f), dark);
            foreach (int s in new[] { -1, 1 }) m.Box(V(s * .155f, .27f, .14f), V(.03f, .06f, .14f), H("1F8A86"));
            m.Box(V(0, .3f, .32f), V(.12f, .08f, .12f), dark, Quaternion.Euler(-30f, 0, 0));
            P.Rod(m, V(0, .3f, -.14f), V(0, .44f, -.2f), .06f, brown, .9f);
            m.Ellipsoid(V(0, .49f, -.22f), V(.15f, .15f, .19f), face);
            m.Box(V(0, .54f, -.22f), V(.155f, .05f, .2f), dark);
            foreach (int s in new[] { -1, 1 }) m.Box(V(s * .075f, .48f, -.25f), V(.02f, .03f, .1f), dark);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .47f, -.35f), V(.07f, .045f, .12f), H("3A3F46"));
            return m;
        }

        /// <summary>BMX pump-track mound: packed dirt wedge with a flat top and a lip, grass tufts on the sides and two
        /// tyre tracks up the slope.</summary>
        static ComicMesh MoundMesh(float w, float rise, float h, float top)
        {
            var m = new ComicMesh { Surface = ComicSurface.Concrete };
            float x0 = -w * .5f, x1 = w * .5f, z1 = rise, z2 = rise + top;
            K.Face(m, V(x0, 0, 0), V(x0, h, z1), V(x1, h, z1), V(x1, 0, 0), Dirt, (Vector3.up + Vector3.back * (h / rise)).normalized);
            K.Face(m, V(x0, h, z1), V(x0, h, z2), V(x1, h, z2), V(x1, h, z1), Dirt, Vector3.up);
            K.Face(m, V(x0, 0, z2), V(x1, 0, z2), V(x1, h, z2), V(x0, h, z2), DirtDark, Vector3.forward);
            foreach (float x in new[] { x0, x1 })
            {
                Vector3 n = new Vector3(Mathf.Sign(x), 0, 0);
                K.TriFacing(m, V(x, 0, 0), V(x, h, z1), V(x, 0, z1), DirtDark, n);
                K.Face(m, V(x, 0, z1), V(x, h, z1), V(x, h, z2), V(x, 0, z2), DirtDark, n);
            }
            m.Surface = ComicSurface.Plain;
            foreach (float x in new[] { -.45f, .45f })
            {
                K.Face(m, V(x - .09f, .015f, .2f), V(x - .09f, h * (z1 - .1f) / rise + .015f, z1 - .1f), V(x + .09f, h * (z1 - .1f) / rise + .015f, z1 - .1f), V(x + .09f, .015f, .2f), DirtDark, (Vector3.up + Vector3.back * (h / rise)).normalized, false);
                K.Face(m, V(x - .09f, h + .015f, z1), V(x - .09f, h + .015f, z2), V(x + .09f, h + .015f, z2), V(x + .09f, h + .015f, z1), DirtDark, Vector3.up, false);
            }
            m.Box(V(0, h + .05f, z2 - .12f), V(w + .02f, .1f, .24f), DirtDark);
            m.Surface = ComicSurface.Foliage;
            foreach (float x in new[] { x0 - .1f, x1 + .1f })
                for (float z = .8f; z < z2; z += 1.3f)
                    m.Ellipsoid(V(x, Mathf.Min(h, h * z / rise) * .5f + .1f, z), V(.5f, Mathf.Min(h, h * z / rise) * .6f + .3f, .9f), z % 2.6f < 1.3f ? Lawn : LawnLight);
            return m;
        }

        // =====================================================================================================
        // WELLARD far scenery
        // =====================================================================================================

        /// <summary>
        /// The whole horizon: lawns and wetland pools, bands of paperbark and tuart woodland (darker near, hazier far),
        /// low estate rooftops between them, a line of power pylons on the left and the Darling Range low and blue far
        /// to the east (left). Sloped with the road; no colliders, no outline.
        /// </summary>
        static ComicMesh WellardFar()
        {
            var m = new ComicMesh();
            var r = new System.Random(1301);
            float F() => (float)r.NextDouble();
            Color haze = new Color(.78f, .86f, .88f);
            Color Hz(Color c, float t) => Color.Lerp(c, haze, t);
            // ground both sides, slightly under the chunks' ground
            m.Surface = ComicSurface.Plain;
            for (float z = -160f; z < 640f; z += 40f)
                for (int s = -1; s <= 1; s += 2) K.Flat(m, s * 76f, s * 1400f, s * 76f, s * 1400f, z, z + 40f, .1f, H("7FA04E"));
            // wetland pools on the right (The Spectacles) and one on the left
            for (int i = 0; i < 12; i++)
            {
                int s = i % 4 == 0 ? -1 : 1;
                float x = 85f + F() * 140f, z = -60f + F() * 300f, w = 14f + F() * 30f, l = 10f + F() * 30f;
                K.Flat(m, s * x, s * (x + w), s * (x + w * .15f), s * (x + w * .85f), z, z + l, .14f + i * .002f, Pond);
                K.Flat(m, s * (x + w * .1f), s * (x + w * .4f), s * (x + w * .2f), s * (x + w * .45f), z + l * .2f, z + l * .6f, .16f + i * .002f, Duckweed);
            }
            // woodland bands and rooftops, in layers (near: dark and crisp; far: hazy)
            for (int layer = 0; layer < 3; layer++)
            {
                // v0.6: woodland as clumps standing on the ground (the old single crowns on thin trunks read as pale
                // lollipops), closer together and less hazed
                float xa = 78f + layer * 60f, t = .05f + layer * .14f;
                for (int s = -1; s <= 1; s += 2)
                    for (float z = -60f; z < 300f; z += 11f + F() * 7f)
                    {
                        float x = xa + F() * 40f, y = .1f;
                        if (F() < .62f)
                        {
                            float hgt = 9f + F() * 6f;
                            Color leaf = Color.Lerp(PaperLeafDark, H("4F6A3C"), F() * .6f);
                            m.Surface = ComicSurface.Foliage;
                            m.Ellipsoid(V(s * x, y + hgt * .42f, z), V(13f + F() * 6f, hgt, 12f + F() * 6f), Hz(leaf, t));
                            m.Ellipsoid(V(s * (x + 5f), y + hgt * .36f, z + 6f), V(10f + F() * 4f, hgt * .8f, 10f), Hz(Color.Lerp(leaf, PaperLeaf, .5f), t));
                            m.Ellipsoid(V(s * (x - 2f), y + hgt * .8f, z - 2f), V(7f, hgt * .45f, 7f), Hz(PaperLeafLight, t + .05f));
                            if (layer == 0)
                            {
                                m.Surface = ComicSurface.Wood;
                                m.Cylinder(V(s * (x - 5f), y + hgt * .3f, z - 5f), .45f, hgt * .6f, Hz(PaperTrunk, t), 6);
                            }
                        }
                        else
                        {
                            // a house: cream or brick walls under a hip roof (the roof is what reads from far)
                            float w = 10f + F() * 4f, dep = 11f + F() * 3f, h = F() < .2f ? 5.8f : 3f;
                            m.Surface = ComicSurface.Plaster;
                            m.Box(V(s * x, y + h * .5f, z), V(dep, h, w), Hz(HouseWalls[(int)(F() * HouseWalls.Length) % HouseWalls.Length], t));
                            HipRoof(m, s * x - dep * .5f - .5f, s * x + dep * .5f + .5f, z - w * .5f - .5f, z + w * .5f + .5f, y + h, 2.4f, Hz(Roofs[(int)(F() * Roofs.Length) % Roofs.Length], t));
                        }
                    }
            }
            // ahead: rooftops and trees across the road's vanishing point
            for (float x = -170f; x < 170f; x += 14f + F() * 8f)
            {
                if (Mathf.Abs(x) < 24f) continue;
                float z = 190f + F() * 60f;
                m.Surface = ComicSurface.Foliage;
                m.Ellipsoid(V(x, 6f, z), V(12f + F() * 6f, 7f + F() * 3f, 10f), Hz(PaperLeaf, .35f));
                if (F() < .5f) HipRoof(m, x + 6f, x + 18f, z - 6f, z + 6f, 3.2f, 2.4f, Hz(Roofs[(int)(F() * Roofs.Length) % Roofs.Length], .35f));
            }
            // power pylons on the left
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = -40f; z < 320f; z += 80f)
            {
                Vector3 p = V(-104f, .1f, z);
                Color steel = Hz(H("8A9198"), .25f);
                foreach (float dx in new[] { -2.5f, 2.5f })
                    foreach (float dz in new[] { -2.5f, 2.5f }) m.Beam(p + V(dx, 0, dz), p + V(0, 32f, 0), .4f, steel);
                m.Beam(p + V(-8f, 24f, 0), p + V(8f, 24f, 0), .4f, steel);
                m.Beam(p + V(-6f, 29f, 0), p + V(6f, 29f, 0), .4f, steel);
                foreach (float dx in new[] { -7.5f, 7.5f, -5.5f, 5.5f })
                    m.Beam(p + V(dx, dx * dx > 40f ? 23f : 28f, 0), p + V(dx, dx * dx > 40f ? 21f : 26f, 80f), .12f, Hz(K.R.ink, .3f));
            }
            // the Darling Range, low and blue to the east
            Color hills = Color.Lerp(H("3E5A8A"), haze, .3f);
            m.Surface = ComicSurface.Foliage;
            for (float z = -400f; z < 700f; z += 110f + F() * 40f)
                m.Ellipsoid(V(-(720f + F() * 80f), 0, z), V(440f + F() * 120f, 100f + F() * 40f, 230f + F() * 60f), Color.Lerp(hills, haze, F() * .15f));
            m.Shear(-WellardDrop / 40f);
            return m;
        }
    }
}
