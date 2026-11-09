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
    /// South line, art agent 4 (v0.4): the students' home ROCKINGHAM (route order 140) and WARNBRO (order 150, the last
    /// district before the loop goes back to UWA). Both keep the sea on the right (+X).
    /// ROCKINGHAM: Rockingham Beach Rd on the foreshore: a row of tall tiered Norfolk Island pines on the grassy foreshore,
    /// limestone walls, white sand and the calm turquoise water of Cockburn Sound; the cafe strip (white two-storey
    /// units with red gable roofs, glass balconies, colourful shade sails and umbrellas), apartments; the playground and
    /// picnic shelters; the naval memorial park (background only); the Village Green with its teal sculptures and the
    /// white sea rescue building; a chunk where the path runs out along the jetty (timber kicker + SPLASH gap).
    /// WARNBRO: Warnbro Sound: white-sand beach, foredunes with coastal heath and limestone, beach access paths with
    /// fences and signs, the surf life saving club and its patrol tower with red-and-yellow flags, Warnbro Beach Rd with
    /// tile-roofed houses, Warnbro station and its car park; coastal path chunks (PedestrianMall) between the dunes and a
    /// beach creek crossed with a dune kicker (SPLASH).
    /// Uses the CBD / Quay comic kit (CbdQuayKit, CqChunkDraft): ComicPalette + ComicInk only, vertex colours.
    ///   Menu: CityScooter/Districts/Build Rockingham and Warnbro
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.RockinghamBuilder.BuildBatch
    ///   Batch with the props atlases (needs graphics): ...RockinghamBuilder.BuildAndRenderAtlasBatch
    /// </summary>
    public static class RockinghamBuilder
    {
        public const string RockId = "rockingham", WarnId = "warnbro";
        public const string RockPrefabs = ComicCityBuilder.Prefabs + "/Districts/Rockingham";
        public const string RockMeshes = ComicCityBuilder.Root + "/Districts/Rockingham";
        public const string WarnPrefabs = ComicCityBuilder.Prefabs + "/Districts/Warnbro";
        public const string WarnMeshes = ComicCityBuilder.Root + "/Districts/Warnbro";
        public const string RockDistrictPath = CbdBuilder.DataFolder + "/District_Rockingham.asset";
        public const string WarnDistrictPath = CbdBuilder.DataFolder + "/District_Warnbro.asset";
        public static readonly string[] RockChunkIds = { "Pines", "Cafes", "Playground", "Jetty", "VillageGreen" };
        public static readonly string[] WarnChunkIds = { "Station", "BeachRoad", "SurfClub", "DunePath", "Creek" };
        public static string RockChunkPath(string variant) => RockPrefabs + "/CHN_Rockingham_" + variant + ".prefab";
        public static string WarnChunkPath(string variant) => WarnPrefabs + "/CHN_Warnbro_" + variant + ".prefab";
        static string RockPf(string name) => RockPrefabs + "/" + name + ".prefab";
        static string WarnPf(string name) => WarnPrefabs + "/" + name + ".prefab";

        /// <summary>The coast is nearly flat: -1.2 m (Rockingham) and -1.4 m (Warnbro) per 40 m chunk.</summary>
        public const float RockDrop = -1.2f, WarnDrop = -1.4f;
        const float RSea = -1.5f, WSea = -1.6f;
        // Rockingham foreshore (beyond the sea-side sidewalk edge at x = 8): lawn to x = 21, limestone wall, sand, water.
        const float LawnW = 13f, WallX = K.Edge + LawnW, ShoreX = 33f;
        // The jetty jump (Ramp + road-wide RoadGap) and the Warnbro creek jump. Numbers for gameplay:
        public const float JettyDeckStart = 3f, JettyRampStart = 10f, JettyRise = 6f, JettyHeight = 1.4f, JettyTop = 3f, JettyGap = 6f;
        public const float CreekRampStart = 6f, CreekRise = 6f, CreekHeight = 1.4f, CreekTop = 2f, CreekGap = 6f;

        // Palette (sRGB; sources and notes in art-src/districts/rockingham|warnbro/SOURCES.txt).
        static readonly Color Paving = K.Hex("D3CFC6"), PavingStripe = K.Hex("A9A59C"), KerbCap = K.Hex("EDE8DC"), SharedGrey = K.Hex("C9C4BA");
        static readonly Color Lawn = K.Hex("6DA544"), LawnLight = K.Hex("82B654"), PathGrey = K.Hex("DCD6C8");
        static readonly Color Limestone = K.Hex("E2D2A6"), LimestoneCap = K.Hex("F0E6CA"), LimestoneDark = K.Hex("BFA978");
        static readonly Color Sand = K.Hex("F2E9CF"), SandWet = K.Hex("DCCDA6"), Foam = K.Hex("F4FBFA");
        static readonly Color Turquoise = K.Hex("47C4C2"), TurquoiseLight = K.Hex("A5E8E0"), SeaBlue = K.Hex("2E9CC2"), SeaRipple = K.Hex("6CC6DE");
        static readonly Color PineDark = K.Hex("2C5A36"), PineMid = K.Hex("3B6F42"), PineLight = K.Hex("5A8C4C"), PineUnder = K.Hex("1C3826"), PineBark = K.Hex("6E5A47");
        static readonly Color White = K.Hex("F6F4EE"), Cream = K.Hex("F0E6CF"), PaleYellow = K.Hex("F3E3B0"), PaleBlue = K.Hex("DCE8EE");
        static readonly Color CafeRoof = K.Hex("D45A3A"), GlassDark = K.Hex("2D4B59"), Balustrade = K.Hex("BFDCE3"), Navy = K.Hex("2A3E6E");
        static readonly Color SailRed = K.Hex("E2463A"), SailOrange = K.Hex("F28C28"), SailYellow = K.Hex("F6C543"), SailPink = K.Hex("E85A8C"), SailTeal = K.Hex("2A9D9A");
        static readonly Color Sandstone = K.Hex("E2CDA3"), AptCream = K.Hex("EFE4CC"), Teal = K.Hex("2BB3A8"), Deck = K.Hex("9A6B45"), DeckLight = K.Hex("B88757");
        static readonly Color Timber = K.Hex("A8774E"), Bronze = K.Hex("6B4F2C"), AnchorBlack = K.Hex("23282E"), CanopyBlue = K.Hex("2F6FB0");
        // Warnbro
        static readonly Color PathConcrete = K.Hex("D8D2C4"), Shoulder = K.Hex("E6DAB9"), Heath = K.Hex("7F9364"), HeathDark = K.Hex("5F7650"),
            HeathLight = K.Hex("9AAA74"), HeathGrey = K.Hex("A3AE8E"), DuneSand = K.Hex("EBDFC0"), BeachWhite = K.Hex("F4EEDC");
        static readonly Color Terracotta = K.Hex("B85A3C"), Colorbond = K.Hex("4A5057"), Surfmist = K.Hex("E4E2D5"), Render = K.Hex("EDE6D6"),
            FencePost = K.Hex("8E8471"), MeshGreen = K.Hex("5E7D5A"), PatrolRed = K.Hex("E1322B"), PatrolYellow = K.Hex("F7D23C");
        static readonly Color Peppermint = K.Hex("5F7D3C"), PeppermintLight = K.Hex("7C9A48");

        [MenuItem("CityScooter/Districts/Build Rockingham and Warnbro")]
        public static void Build() => Build(false);

        public static void BuildBatch()
        {
            try { Build(false); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Builds both districts and renders art-src/districts/rockingham|warnbro/props-atlas.png (run with graphics).</summary>
        public static void BuildAndRenderAtlasBatch()
        {
            try { Build(true); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Build(bool atlas)
        {
            // The theme is loaded again for each district: rendering an atlas opens a fresh preview scene, which unloads it.
            BuildRockingham(LoadTheme(), atlas);
            BuildWarnbro(LoadTheme(), atlas);
            AssetDatabase.SaveAssets();
        }

        static TrackTheme LoadTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme == null || theme.hazards == null || theme.hazards.Length == 0) throw new InvalidOperationException("No comic theme (with hazards) at " + ComicCityBuilder.ThemePath);
            return theme;
        }

        // ================================================================================== ROCKINGHAM

        static void BuildRockingham(TrackTheme theme, bool atlas)
        {
            K.Begin(RockMeshes, RockPrefabs, CbdBuilder.DataFolder);
            Obstacle bin = K.SaveObstacle(RockPf("OBS_Rockingham_Bin"), "OBS_Rockingham_Bin", BeachBinMesh(), new Vector3(.8f, 1.05f, .8f), ObstacleKind.Thing, true, true, 1f);
            Obstacle rack = K.SaveObstacle(RockPf("OBS_Rockingham_BikeRack"), "OBS_Rockingham_BikeRack", BikeRackMesh(), new Vector3(2.3f, .9f, .4f), ObstacleKind.Thing, true, false, .8f);
            Obstacle umbrella = K.SaveObstacle(RockPf("OBS_Rockingham_BeachUmbrella"), "OBS_Rockingham_BeachUmbrella", BeachUmbrellaMesh(), new Vector3(1f, 2.2f, 1f), ObstacleKind.Thing, false, true, .8f);
            Obstacle esky = K.SaveObstacle(RockPf("OBS_Rockingham_Esky"), "OBS_Rockingham_Esky", EskyMesh(), new Vector3(.85f, .62f, .55f), ObstacleKind.Thing, true, true, 1f);
            Obstacle aFrame = K.SaveObstacle(RockPf("OBS_Rockingham_AFrame"), "OBS_Rockingham_AFrame", AFrameMesh(), new Vector3(.9f, 1.05f, .7f), ObstacleKind.Thing, true, true, .9f);
            GameObject pelican = SaveAnimal(RockPf("ANM_Rockingham_Pelican"), "ANM_Rockingham_Pelican", PelicanMesh(), new Vector3(0, .65f, 0), new Vector3(.7f, 1.3f, 1.4f), .35f, false);
            GameObject gull = SaveAnimal(RockPf("ANM_Rockingham_Seagull"), "ANM_Rockingham_Seagull", SeagullMesh(), new Vector3(0, .3f, 0), new Vector3(.5f, .6f, .7f), 0f, true);
            GameObject gullWalking = SaveAnimal(RockPf("ANM_Rockingham_SeagullWalking"), "ANM_Rockingham_SeagullWalking", SeagullMesh(), new Vector3(0, .3f, 0), new Vector3(.5f, .6f, .7f), .6f, true);
            Ramp boardwalk = K.SaveRamp(RockPf("RMP_Rockingham_Boardwalk"), "RMP_Rockingham_Boardwalk", 3f, 5f, 1.3f, 1.5f, 1f, .5f, DeckLight, Deck, false);

            K.WriteChunk(RockEntry(), RockChunkPath("Entry"));
            K.WriteChunk(RockPines(), RockChunkPath("Pines"));
            K.WriteChunk(RockCafes(), RockChunkPath("Cafes"));
            K.WriteChunk(RockPlayground(), RockChunkPath("Playground"));
            K.WriteChunk(RockJetty(), RockChunkPath("Jetty"));
            K.WriteChunk(RockVillageGreen(), RockChunkPath("VillageGreen"));
            K.WriteChunk(RockExit(), RockChunkPath("Exit"));
            GameObject far = K.SaveFarScenery(RockPf("FAR_Rockingham_CockburnSound"), "FAR_Rockingham_CockburnSound", RockFar(-RockDrop / 40f));

            bool created = AssetDatabase.LoadAssetAtPath<District>(RockDistrictPath) == null;
            District d = K.LoadOrCreate<District>(RockDistrictPath);
            // Placeholders when the asset is first created (far south of the city: the CBD skyline turned behind and sunk
            // away); the district system owns these fields after this and they are never written again.
            if (created) { d.skyYaw = 180f; d.skySize = .6f; d.skyHeight = -12f; d.skyline = 0f; }
            d.id = RockId; d.displayName = "ROCKINGHAM"; d.order = 140; d.length = new Vector2(360f, 440f);
            d.chunks = RockChunkIds.Select(c => AssetDatabase.LoadAssetAtPath<GameObject>(RockChunkPath(c)).GetComponent<RoadChunk>()).ToArray();
            d.orderedChunks = true;
            d.entry = AssetDatabase.LoadAssetAtPath<GameObject>(RockChunkPath("Entry")).GetComponent<RoadChunk>();
            d.exit = AssetDatabase.LoadAssetAtPath<GameObject>(RockChunkPath("Exit")).GetComponent<RoadChunk>();
            d.allowBoulevard = false;
            d.obstacles = new[] { bin, rack, umbrella, esky, aFrame };
            d.jumpObstacles = new[] { esky, bin };
            d.gifts = Array.Empty<Gift>();
            d.hazards = ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_OncomingPair", "HZ_Crossing", "HZ_SidewalkStroll", "HZ_PoliceStop");
            var people = new List<GameObject>(K.People(theme, "Tourist", "Surfer", "Jogger", "Jogger", "DogWalker", "Kid", "Kid", "Elder", "Shopper", "Barista", "Skater"));
            people.Add(pelican); people.Add(pelican); people.Add(gull); people.Add(gullWalking); people.Add(gullWalking);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { boardwalk };
            d.rampFrequency = 1.2f;
            d.blendMeters = 150f;
            // Bright coastal light, turquoise sea, white sand.
            d.fogColor = new Color(.74f, .87f, .95f);
            d.skyTint = new Color(.98f, 1f, 1f);
            d.ambientSky = new Color(.70f, .86f, 1f);
            d.ambientEquator = new Color(.90f, .90f, .86f);
            d.ambientGround = new Color(.80f, .74f, .62f);
            d.sunColor = new Color(1f, .97f, .9f);
            d.sunIntensity = 1.5f;
            d.farScenery = far;
            // gateway, ambience, ambienceVolume and the sky fields belong to the district system: not written here.
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/Rockingham] " + string.Join("; ", K.Report));
            if (atlas) RenderRockAtlas(bin, rack, umbrella, esky, aFrame, pelican, gull, boardwalk);
        }

        static HazardPattern[] ThemeHazards(TrackTheme theme, params string[] names) =>
            theme.hazards == null ? Array.Empty<HazardPattern>() : theme.hazards.Where(h => h != null && names.Contains(h.name)).Distinct().ToArray();

        static void RenderRockAtlas(Obstacle bin, Obstacle rack, Obstacle umbrella, Obstacle esky, Obstacle aFrame, GameObject pelican, GameObject gull, Ramp boardwalk)
        {
            ComicMesh Make(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("NorfolkPine", null, Make(m => NorfolkPine(m, Vector3.zero, 19f, 3))),
                ("NorfolkPineYoung", null, Make(m => NorfolkPine(m, Vector3.zero, 11f, 8))),
                ("TwinLamp", null, Make(m => TwinLamp(m, Vector3.zero))),
                ("BeachShade", null, Make(m => BeachShade(m, Vector3.zero, SailTeal))),
                ("PicnicShelter", null, Make(m => PicnicShelter(m, Vector3.zero))),
                ("Playground", null, Make(m => Playground(m, Vector3.zero))),
                ("CafeUnit", null, Make(m => CafeUnit(m, new CqLot(-1, -5f, 5f, 6f, 12f, 0f), White, CanopyBlue))),
                ("ShadeSails", null, Make(m => { ShadeSail(m, V(-2, 3.6f, -3), V(2, 4.3f, 0), V(-2, 3.4f, 3), SailRed, 0f); ShadeSail(m, V(2, 3.5f, 1), V(-1, 4.4f, 4), V(3, 3.7f, 6), SailYellow, 0f); })),
                ("TealSculptures", null, Make(m => { TealSculpture(m, V(0, 0, 0), 6.5f, .5f); TealSculpture(m, V(1.6f, 0, 1.2f), 5f, -.4f); })),
                ("SeaRescue", null, Make(m => SeaRescue(m, Vector3.zero))),
                ("NavalMemorial", null, Make(m => NavalMemorial(m, Vector3.zero))),
                ("JettyLamp", null, Make(m => JettyLamp(m, Vector3.zero))),
                ("Bin", bin.gameObject, null),
                ("BikeRack", rack.gameObject, null),
                ("BeachUmbrella", umbrella.gameObject, null),
                ("Esky", esky.gameObject, null),
                ("AFrame", aFrame.gameObject, null),
                ("Pelican", pelican, null),
                ("Seagull", gull, null),
                ("BoardwalkKicker", boardwalk.gameObject, null),
                ("Sailboat", null, Make(m => Sailboat(m, Vector3.zero, 1f, SailRed))),
                ("Dolphins", null, Make(m => { Dolphin(m, V(0, 0, 0), 0f, 1.4f); Dolphin(m, V(1.5f, 0, 2f), 20f, 1.2f); })),
            };
            K.RenderAtlas("art-src/districts/rockingham/props-atlas.png", items);
        }

        // ---------------------------------------------------------------- Rockingham chunks

        static void RockStreet(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f)
        {
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, Paving, KerbCap, true, skipFrom, skipTo);
            SidewalkStripes(d.Road, 0f, d.Length, skipFrom, skipTo);
        }

        // Rockingham's grey paving with darker diagonal bands across the sidewalks (the foreshore revitalisation look).
        static void SidewalkStripes(ComicMesh m, float z0, float z1, float skipFrom = -1f, float skipTo = -1f)
        {
            m.Surface = ComicSurface.Plain;
            for (int s = -1; s <= 1; s += 2)
                for (float z = z0 + .6f; z < z1 - 3f; z += 3.4f)
                {
                    if (skipFrom >= 0f && z + 3f > skipFrom && z < skipTo + .5f) continue;
                    float y = K.Kerb + .022f, xa = s * (K.Half + .3f), xb = s * (K.Edge - .08f);
                    K.Face(m, V(xa, y, z), V(xa, y, z + .6f), V(xb, y, z + 2.6f), V(xb, y, z + 2f), PavingStripe, Vector3.up, false);
                }
        }

        // The sea side: foreshore lawn with a path, the limestone wall, white sand, turquoise shallows and deeper blue.
        static void Foreshore(CqChunkDraft d, float z0, float z1, int seed)
        {
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Foliage;
            K.SideBox(m, 1, z0, z1, -2.4f, K.Kerb + .05f, 0f, LawnW, Lawn);
            m.Surface = ComicSurface.Plain;
            for (float z = z0 + .5f; z < z1 - 2.5f; z += 7f)
                K.Face(m, V(K.Edge + .3f, K.Kerb + .06f, z), V(K.Edge + .3f, K.Kerb + .06f, z + 3f), V(13f, K.Kerb + .06f, z + 3f), V(13f, K.Kerb + .06f, z), LawnLight, Vector3.up, false);
            m.Surface = ComicSurface.Paving;
            for (float z = z0; z < z1 - .01f; z += 4f)
                K.Face(m, V(14.6f, K.Kerb + .07f, z), V(14.6f, K.Kerb + .07f, Mathf.Min(z1, z + 4f)), V(17f, K.Kerb + .07f, Mathf.Min(z1, z + 4f)), V(17f, K.Kerb + .07f, z), PathGrey, Vector3.up, false);
            // the limestone wall between lawn and beach, block joints and a cream cap
            ComicMesh w = d.SlopedFrontage;
            w.Surface = ComicSurface.Concrete;
            K.SideBox(w, 1, z0, z1, -1.1f, K.Kerb + .32f, LawnW - .05f, LawnW + .7f, Limestone);
            K.SideBox(w, 1, z0, z1, K.Kerb + .32f, K.Kerb + .44f, LawnW - .12f, LawnW + .78f, LimestoneCap);
            w.Surface = ComicSurface.Plain;
            for (float z = z0 + .7f; z < z1 - .3f; z += 1.4f)
                K.Face(w, V(WallX + .705f, -.9f, z), V(WallX + .705f, -.9f, z + .05f), V(WallX + .705f, K.Kerb + .3f, z + .05f), V(WallX + .705f, K.Kerb + .3f, z), LimestoneDark, Vector3.right, false);
            for (float z = z0; z < z1 - .01f; z += 4f)
                K.Face(w, V(WallX + .706f, -.35f, z), V(WallX + .706f, -.35f, Mathf.Min(z1, z + 4f)), V(WallX + .706f, -.3f, Mathf.Min(z1, z + 4f)), V(WallX + .706f, -.3f, z), LimestoneDark, Vector3.right, false);
            // white sand sloping into the water, wet sand at the water's edge
            Terrain(m, WallX + .7f, 38f, z0, z1, 2.4f, 4f,
                (x, z) => Mathf.Lerp(-.45f, RSea - .4f, Mathf.InverseLerp(WallX + .7f, 38f, x)) + .06f * Mathf.Sin(z * .7f + x * .9f),
                (x, z, y) => x > ShoreX - 3.5f ? SandWet : Sand, ComicSurface.Paving);
            K.Water(m, ShoreX, 62f, z0, z1, RSea, Turquoise, TurquoiseLight, seed);
            K.Water(m, 62f, 160f, z0, z1, RSea, SeaBlue, SeaRipple, seed + 1);
            m.Surface = ComicSurface.Plain;
            var r = new System.Random(seed);
            for (float z = z0; z < z1 - 1f; z += F(r, 3f, 6f))
            {
                float e = Mathf.Min(z1, z + F(r, 1.5f, 4f));
                K.Face(m, V(ShoreX + .2f, RSea + .02f, z), V(ShoreX + .2f, RSea + .02f, e), V(ShoreX + .9f, RSea + .02f, e), V(ShoreX + .9f, RSea + .02f, z), Foam, Vector3.up, false);
            }
        }

        // Norfolk pines on the foreshore lawn (decoration beyond the rideable sidewalk: no colliders) and the white lamps.
        static void ForeshorePines(CqChunkDraft d, int seed, params float[] zs)
        {
            var r = new System.Random(seed);
            foreach (float z in zs)
                NorfolkPine(d.Props, V(F(r, 12.2f, 13.6f), d.Ground(z) + .05f, z), F(r, 16f, 21f), seed + (int)z);
        }

        static void ForeshoreLamps(CqChunkDraft d, float z0, float z1, float step = 20f)
        {
            for (float z = z0; z < z1; z += step) TwinLamp(d.Props, V(K.Edge + .6f, d.Ground(z) + .05f, z));
        }

        static CqChunkDraft RockPines()
        {
            var d = new CqChunkDraft("Rockingham_Pines", RockDrop);
            RockStreet(d);
            Foreshore(d, 0, 40, 141);
            ForeshorePines(d, 11, 4f, 14f, 24f, 34f);
            ForeshoreLamps(d, 9f, 40f);
            Bench(d.Props, V(17.8f, d.Ground(19f) + .05f, 19f), -90f);
            BeachShade(d.Props, V(26f, d.Y(10f) - .3f, 10f), SailRed);
            BeachShade(d.Props, V(27.5f, d.Y(29f) - .4f, 29f), SailTeal);
            // land side: low shops then an apartment block
            LeftGround(d, 0, 40, 60f, Paving, ComicSurface.Paving);
            SidewalkTrees(d, -1, 6f, 26f);
            ShopRow(d, 0f, 18f, 0);
            var apt = new CqLot(-1, 20f, 38f, K.Edge + 4f, 16f, d.Base(29f));
            Apartment(d.Frontage, apt, 7, Sandstone, White, true);
            d.Wall(apt, 24f);
            DeepTown(d, 0, 40, 3);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft RockCafes()
        {
            var d = new CqChunkDraft("Rockingham_Cafes", RockDrop);
            RockStreet(d);
            Foreshore(d, 0, 40, 142);
            ForeshorePines(d, 12, 8f, 30f);
            ForeshoreLamps(d, 19f, 40f);
            BeachShade(d.Props, V(25.5f, d.Y(6f) - .3f, 6f), SailOrange);
            BeachShade(d.Props, V(26.5f, d.Y(18f) - .35f, 18f), SailPink);
            BeachShade(d.Props, V(25.8f, d.Y(35f) - .3f, 35f), Navy);
            Bench(d.Props, V(17.8f, d.Ground(22f) + .05f, 22f), -90f);
            CafeStrip(d, 0f, 40f, 1);
            DeepTown(d, 0, 40, 5);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft RockPlayground()
        {
            var d = new CqChunkDraft("Rockingham_Playground", RockDrop);
            RockStreet(d);
            Foreshore(d, 0, 40, 143);
            ForeshorePines(d, 13, 3f, 37f);
            ForeshoreLamps(d, 20f, 40f, 40f);
            Playground(d.Frontage, V(17f, d.Ground(13f) + .06f, 13f));
            PicnicShelter(d.Frontage, V(16.5f, d.Ground(28f) + .06f, 28f));
            BeachShade(d.Props, V(26f, d.Y(20f) - .3f, 20f), SailYellow);
            // the naval memorial park on the land side: background only (no colliders, no Obstacle)
            LeftGround(d, 0, 40, 60f, Lawn, ComicSurface.Foliage);
            LowWall(d, -1, 0f, 40f, .55f);
            NavalMemorial(d.Landmark, V(-(K.Edge + 9f), d.Ground(20f) + .05f, 20f));
            d.Walls.Add(new Bounds(new Vector3(-(K.Edge + 9f), d.Y(20f) + 2f, 20f), new Vector3(14f, 6f, 30f)));
            var r = new System.Random(7);
            for (float z = 4f; z < 40f; z += 11f) NorfolkPine(d.Props, V(-(K.Edge + F(r, 17f, 20f)), d.Ground(z), z), F(r, 13f, 17f), 70 + (int)z);
            DeepTown(d, 0, 40, 7);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft RockVillageGreen()
        {
            // The Village Green: the road becomes the striped shared street, the white sea rescue building on the beach,
            // the teal sculptures, cafes and a lawn on the land side.
            var d = new CqChunkDraft("Rockingham_VillageGreen", RockDrop);
            K.StreetSurface(d.Road, d.Length, SharedGrey, Paving, KerbCap, false);
            SidewalkStripes(d.Road, 0f, d.Length);
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            for (float z = 1f; z < d.Length - 4.5f; z += 3.2f)
                K.Face(m, V(-K.Half + .1f, .022f, z), V(-K.Half + .1f, .022f, z + .7f), V(K.Half - .1f, .022f, z + 3.6f), V(K.Half - .1f, .022f, z + 2.9f), PavingStripe, Vector3.up, false);
            Foreshore(d, 0, 40, 144);
            ForeshorePines(d, 14, 5f, 36f);
            // the sea rescue building on a limestone pad on the beach
            ComicMesh w = d.SlopedFrontage;
            w.Surface = ComicSurface.Concrete;
            K.MinMaxBox(w, V(WallX + .6f, RSea - .6f, 8f), V(39f, K.Kerb - .02f, 31f), Limestone);
            K.MinMaxBox(w, V(WallX + .55f, K.Kerb - .02f, 8f), V(39.05f, K.Kerb + .08f, 31f), LimestoneCap);
            SeaRescue(d.Landmark, V(31f, d.Ground(19.5f) + .08f, 19.5f));
            for (int i = 0; i < 3; i++) TealSculpture(d.Landmark, V(11.5f + i * 1.3f, d.Ground(8f + i * 2.2f) + .05f, 8f + i * 2.2f), 6.8f - i * .9f, i % 2 == 0 ? .5f : -.4f);
            // land side: the green with sculptures and pines, a low cafe pavilion behind
            LeftGround(d, 0, 40, 60f, Lawn, ComicSurface.Foliage);
            ComicMesh g = d.Road;
            g.Surface = ComicSurface.Paving;
            for (float z = 0; z < 40f - .01f; z += 4f)
                K.Face(g, V(-K.Edge, K.Kerb + .03f, z), V(-K.Edge, K.Kerb + .03f, z + 4f), V(-(K.Edge + 3f), K.Kerb + .03f, z + 4f), V(-(K.Edge + 3f), K.Kerb + .03f, z), Paving, Vector3.up, false);
            for (int i = 0; i < 2; i++) TealSculpture(d.Landmark, V(-(K.Edge + 6f + i * 1.6f), d.Ground(24f + i * 2f) + .05f, 24f + i * 2f), 7.2f - i * 1.4f, i == 0 ? -.5f : .4f);
            NorfolkPine(d.Props, V(-(K.Edge + 8f), d.Ground(8f), 8f), 17f, 81);
            NorfolkPine(d.Props, V(-(K.Edge + 11f), d.Ground(36f), 36f), 14f, 82);
            Bench(d.Props, V(-(K.Edge + 4f), d.Ground(15f), 15f), 90f);
            var cafe = new CqLot(-1, 12f, 32f, K.Edge + 18f, 12f, d.Base(22f));
            CafeUnit(d.Frontage, new CqLot(-1, 12f, 22f, K.Edge + 18f, 12f, d.Base(17f)), PaleYellow, SailTeal);
            CafeUnit(d.Frontage, new CqLot(-1, 22f, 32f, K.Edge + 18f, 12f, d.Base(27f)), White, CanopyBlue);
            d.Wall(cafe, 10f);
            DeepTown(d, 0, 40, 9);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft RockJetty()
        {
            // The path runs out along the jetty over the water (timber deck on piles, black lamp posts), up a timber
            // kicker at the end, then 6 m of open water across the whole width (SPLASH!), then the seawall and the road.
            var d = new CqChunkDraft("Rockingham_Jetty", RockDrop);
            float j0 = JettyDeckStart, ramp = JettyRampStart, lip = ramp + JettyRise + JettyTop, far = lip + JettyGap;
            RockStreet(d, j0, far);
            JettyDeck(d, j0, ramp);
            ComicMesh m = d.Road;
            K.Water(m, -70f, ShoreX, j0, far, RSea, Turquoise, TurquoiseLight, 151);
            K.Water(m, ShoreX, 62f, j0, far, RSea, Turquoise, TurquoiseLight, 152);
            K.Water(m, 62f, 160f, j0, far, RSea, SeaBlue, SeaRipple, 153);
            Seawall(m, -70f, 40f, j0, true);
            Seawall(m, -70f, 40f, far, false);
            Foreshore(d, 0, j0, 154);
            Foreshore(d, far, 40, 155);
            LeftGround(d, 0, j0, 60f, Lawn, ComicSurface.Foliage);
            LeftGround(d, far, 40, 60f, Lawn, ComicSurface.Foliage);
            var spec = new CqRampSpec { Name = "JettyKicker", X = 0f, Z = ramp, Width = 2f * K.Edge, Rise = JettyRise, Height = JettyHeight, Top = JettyTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            d.Gaps.Add(new CqGapSpec { Name = "JettyEndWater", Z = lip, Length = JettyGap, Width = 0f, Word = "SPLASH", Water = true });
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, DeckLight, Deck, false, RSea - .3f);
            for (int s = -1; s <= 1; s += 2)
            {
                deck.Surface = ComicSurface.Wood;
                deck.Beam(V(s * (K.Edge + .12f), 1.05f, ramp), V(s * (K.Edge + .12f), JettyHeight + 1.05f, ramp + JettyRise), .12f, Timber);
                deck.Beam(V(s * (K.Edge + .12f), JettyHeight + 1.05f, ramp + JettyRise), V(s * (K.Edge + .12f), JettyHeight + 1.05f, lip - .2f), .12f, Timber);
                for (float z = ramp; z <= lip; z += 3f) deck.Box(V(s * (K.Edge + .12f), (JettyHeight * Mathf.Clamp01((z - ramp) / JettyRise) + RSea) * .5f + .5f, z), V(.25f, JettyHeight * Mathf.Clamp01((z - ramp) / JettyRise) - RSea + 1f, .25f), Deck);
                JettyLamp(deck, V(s * (K.Edge + .35f), JettyHeight + .05f, lip - .6f));
            }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            d.KeepOut.Add(KeepOutBox(d, -1, ramp, far)); d.KeepOut.Add(KeepOutBox(d, 1, ramp, far));
            // after the gap: pines and a shelter on the far lawn, a dinghy in the cove
            ForeshorePines(d, 15, 31f);
            NorfolkPine(d.Props, V(-(K.Edge + 6f), d.Ground(33f), 33f), 16f, 91);
            Dinghy(d.Props, V(-20f, d.Y(14f) + RSea, 14f), 30f, SailRed);
            Dinghy(d.Props, V(24f, d.Y(18f) + RSea, 18f), -20f, White);
            DeepTown(d, far, 40, 11);
            d.GiftSlots.Add(new Vector2(0f, 31f));
            d.Slot(-2.6f, 36f); d.Slot(2.6f, 36f);
            return d;
        }

        static CqChunkDraft RockEntry()
        {
            // Calm at z = 0 (low limestone walls, hedges, small trees), then the foreshore opens on the right behind a
            // rock groyne, beach houses and a pine on the left.
            var d = new CqChunkDraft("Rockingham_Entry", RockDrop);
            RockStreet(d);
            CalmEdge(d, -1, 0f, 14f, 1);
            CalmEdge(d, 1, 0f, 14f, 2);
            Foreshore(d, 14f, 40f, 161);
            Groyne(d, 14f);
            ForeshorePines(d, 16, 20f, 31f);
            ForeshoreLamps(d, 26f, 40f);
            LeftGround(d, 14f, 40f, 60f, Lawn, ComicSurface.Foliage);
            LowWall(d, -1, 14f, 40f, .6f);
            BeachHouse(d.Frontage, new CqLot(-1, 18f, 28f, K.Edge + 7f, 11f, d.Base(23f)), Cream, CafeRoof);
            BeachHouse(d.Frontage, new CqLot(-1, 29f, 39f, K.Edge + 8f, 11f, d.Base(34f)), PaleBlue, K.Hex("4A5057"));
            NorfolkPine(d.Props, V(-(K.Edge + 3.2f), d.Ground(16f), 16f), 14f, 93);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft RockExit()
        {
            // The foreshore and cafes first, then calm towards z = 40: a groyne, low walls, hedges, small trees.
            var d = new CqChunkDraft("Rockingham_Exit", RockDrop);
            RockStreet(d);
            Foreshore(d, 0f, 26f, 171);
            Groyne(d, 26f);
            ForeshorePines(d, 17, 6f, 17f);
            ForeshoreLamps(d, 2f, 26f);
            LeftGround(d, 0f, 26f, 60f, Lawn, ComicSurface.Foliage);
            LowWall(d, -1, 0f, 26f, .6f);
            BeachHouse(d.Frontage, new CqLot(-1, 2f, 12f, K.Edge + 7f, 11f, d.Base(7f)), White, CafeRoof);
            BeachHouse(d.Frontage, new CqLot(-1, 13f, 23f, K.Edge + 8f, 11f, d.Base(18f)), PaleYellow, CafeRoof);
            CalmEdge(d, -1, 26f, 40f, 3);
            CalmEdge(d, 1, 26f, 40f, 4);
            d.StandardSlots();
            return d;
        }

        // ---------------------------------------------------------------- Rockingham parts

        static void LeftGround(CqChunkDraft d, float z0, float z1, float depth, Color c, ComicSurface surface)
        {
            d.Road.Surface = surface;
            K.SideBox(d.Road, -1, z0, z1, -2.4f, K.Kerb + .02f, 0f, depth, c);
        }

        // A low limestone wall with a cream cap just beyond the sidewalk (sheared with the street).
        static void LowWall(CqChunkDraft d, int s, float z0, float z1, float height)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Concrete;
            K.SideBox(m, s, z0, z1, -1f, K.Kerb + height, .05f, .55f, Limestone);
            K.SideBox(m, s, z0, z1, K.Kerb + height, K.Kerb + height + .1f, 0f, .6f, LimestoneCap);
        }

        // Calm end of an entry / exit chunk: low limestone wall, clipped hedge, lawn, two small round trees.
        static void CalmEdge(CqChunkDraft d, int s, float z0, float z1, int seed)
        {
            ComicMesh m = d.SlopedFrontage;
            d.Road.Surface = ComicSurface.Foliage;
            K.SideBox(d.Road, s, z0, z1, -2.4f, K.Kerb + .03f, 0f, 60f, Lawn);
            LowWall(d, s, z0, z1, .55f);
            m.Surface = ComicSurface.Foliage;
            K.SideBox(m, s, z0 + .4f, z1 - .4f, K.Kerb, K.Kerb + 1.3f, .8f, 1.9f, K.Hex("4E7F3A"));
            var r = new System.Random(seed);
            for (float z = z0 + 3.5f; z < z1 - 1f; z += 7f)
                K.PlaneTree(d.Props, null, V(s * (K.Edge + F(r, 4f, 7f)), d.Ground(z), z), F(r, .75f, .95f), true);
            if (s > 0) for (float z = z0 + 2f; z < z1; z += 10f) TwinLamp(d.Props, V(K.Edge + .6f, d.Ground(z) + .05f, z));
            d.Walls.Add(new Bounds(new Vector3(s * (K.Edge + 7f), d.Y((z0 + z1) * .5f) + 1f, (z0 + z1) * .5f), new Vector3(13.5f, 4f, z1 - z0)));
        }

        // A rock groyne running out into the sea where the beach starts or ends (hides the end of the sand and water).
        static void Groyne(CqChunkDraft d, float z)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(K.Edge + .2f, RSea - 1f, z - 1.2f), V(70f, -.2f, z + 1.2f), LimestoneDark);
            var r = new System.Random((int)z * 13 + 5);
            for (float x = K.Edge + 1f; x < 70f; x += F(r, 1.6f, 2.4f))
            {
                float y = x < WallX ? K.Kerb : Mathf.Lerp(-.2f, RSea + .3f, Mathf.InverseLerp(WallX, 60f, x));
                m.Box(V(x, y + .25f, z + F(r, -.5f, .5f)), V(F(r, 1.4f, 2.2f), F(r, .8f, 1.3f), F(r, 1.6f, 2.4f)), Color.Lerp(Limestone, LimestoneDark, F(r, 0f, .8f)), Quaternion.Euler(F(r, -8f, 8f), F(r, 0f, 90f), F(r, -8f, 8f)));
            }
        }

        // Seawall across the cove (the jetty chunk): its face at z looks out over the water; limestone coping flush with the
        // sidewalks beside the road (the road part stays under the deck / road).
        static void Seawall(ComicMesh m, float x0, float x1, float z, bool facingForward)
        {
            m.Surface = ComicSurface.Concrete;
            float t = facingForward ? -.4f : .4f;
            K.MinMaxBox(m, V(x0, RSea - 1f, z), V(x1, -.03f, z + t), Limestone);
            K.MinMaxBox(m, V(x0, -.03f, z), V(-K.Half, K.Kerb + .02f, z + t), LimestoneCap);
            K.MinMaxBox(m, V(K.Half, -.03f, z), V(x1, K.Kerb + .02f, z + t), LimestoneCap);
        }

        // The jetty: timber deck with the street's cross-section (road at 0, walkways at the kerb height), fascia, piles.
        static void JettyDeck(CqChunkDraft d, float z0, float z1)
        {
            ComicMesh m = d.Road;
            Color board = Color.Lerp(Deck, K.R.ink, .3f), walk = Color.Lerp(DeckLight, K.R.cream, .15f);
            for (float z = z0; z < z1 - .01f; z += 1f)
            {
                float e = Mathf.Min(z1, z + 1f);
                m.Surface = ComicSurface.Wood;
                K.Face(m, V(-K.Half, 0f, z), V(-K.Half, 0f, e), V(K.Half, 0f, e), V(K.Half, 0f, z), DeckLight, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    K.Face(m, V(s * K.Half, K.Kerb, z), V(s * K.Half, K.Kerb, e), V(s * K.Edge, K.Kerb, e), V(s * K.Edge, K.Kerb, z), walk, Vector3.up, false);
                    K.Face(m, V(s * K.Half, -.05f, z), V(s * K.Half, -.05f, e), V(s * K.Half, K.Kerb, e), V(s * K.Half, K.Kerb, z), Deck, new Vector3(-s, 0, 0), false);
                    K.Face(m, V(s * K.Edge, -.6f, z), V(s * K.Edge, -.6f, e), V(s * K.Edge, K.Kerb, e), V(s * K.Edge, K.Kerb, z), Deck, new Vector3(s, 0, 0), false);
                }
                m.Surface = ComicSurface.Plain;
                K.Face(m, V(-K.Edge, K.Kerb + .012f, z + .45f), V(-K.Edge, K.Kerb + .012f, z + .5f), V(-K.Half, K.Kerb + .012f, z + .5f), V(-K.Half, K.Kerb + .012f, z + .45f), board, Vector3.up, false);
                K.Face(m, V(K.Half, K.Kerb + .012f, z + .45f), V(K.Half, K.Kerb + .012f, z + .5f), V(K.Edge, K.Kerb + .012f, z + .5f), V(K.Edge, K.Kerb + .012f, z + .45f), board, Vector3.up, false);
                K.Face(m, V(-K.Half, .012f, z + .45f), V(-K.Half, .012f, z + .5f), V(K.Half, .012f, z + .5f), V(K.Half, .012f, z + .45f), board, Vector3.up, false);
            }
            ComicMesh p = d.SlopedFrontage;
            p.Surface = ComicSurface.Wood;
            for (float z = z0 + 1f; z < z1; z += 3f)
                for (float x = -K.Edge + .3f; x <= K.Edge; x += (2f * K.Edge - .6f) / 3f)
                    p.Cylinder(V(x, (RSea - .8f - .6f) * .5f, z), .2f, -.6f - (RSea - .8f), Deck, 8);
            for (int s = -1; s <= 1; s += 2)
            {
                p.Box(V(s * (K.Edge + .12f), K.Kerb + .55f, (z0 + z1) * .5f), V(.12f, .12f, z1 - z0), Timber);
                for (float z = z0 + .5f; z < z1; z += 2.5f) p.Box(V(s * (K.Edge + .12f), K.Kerb + .3f, z), V(.16f, 1.1f, .16f), Deck);
                JettyLamp(p, V(s * (K.Edge + .35f), K.Kerb, z0 + 3f));
            }
        }

        static Bounds KeepOutBox(CqChunkDraft d, int s, float z0, float z1)
        {
            float yTop = d.Y(z0) + 5f, yBottom = d.Y(z1) - 2f;
            return new Bounds(new Vector3(s * (K.Half + K.Side * .5f + .3f), (yTop + yBottom) * .5f, (z0 + z1) * .5f), new Vector3(K.Side + .6f, yTop - yBottom, z1 - z0));
        }

        static void SidewalkTrees(CqChunkDraft d, int s, params float[] zs)
        {
            foreach (float z in zs) K.PlaneTree(d.Props, d.Collisions, V(s * (K.Edge - .9f), d.Ground(z), z), .8f);
        }

        // The cafe strip: alfresco paving with colourful umbrellas and shade sails, white two-storey units with red gables.
        static void CafeStrip(CqChunkDraft d, float z0, float z1, int seed)
        {
            LeftGround(d, z0, z1, 60f, Paving, ComicSurface.Paving);
            ComicMesh g = d.Road;
            g.Surface = ComicSurface.Plain;
            for (float z = z0 + 1.6f; z < z1 - 3f; z += 3.4f)
                K.Face(g, V(-(K.Edge + .2f), K.Kerb + .025f, z), V(-(K.Edge + .2f), K.Kerb + .025f, z + .6f), V(-(K.Edge + 5.8f), K.Kerb + .025f, z + 2.4f), V(-(K.Edge + 5.8f), K.Kerb + .025f, z + 1.8f), PavingStripe, Vector3.up, false);
            Color[] walls = { White, Cream, White, PaleYellow, PaleBlue };
            Color[] awnings = { CanopyBlue, SailTeal, Navy, CanopyBlue };
            int i = seed;
            for (float z = z0; z1 - z >= 7.9f; i++)
            {
                float w = Mathf.Min(10f, z1 - z);
                var lot = new CqLot(-1, z, z + w, K.Edge + 6f, 12f, d.Base(z + w * .5f));
                CafeUnit(d.Frontage, lot, walls[i % walls.Length], awnings[i % awnings.Length]);
                d.Wall(lot, 10f);
                z += w;
            }
            Color[] canopies = { SailRed, SailOrange, SailYellow, SailTeal, SailPink, White };
            int k = seed;
            for (float u = z0 + 2.4f; u < z1 - 1.5f; u += 5f, k++)
                K.CafeUmbrella(d.Props, null, V(-(K.Edge + 2.6f), d.Ground(u), u), canopies[k % canopies.Length]);
            float y = d.Ground((z0 + z1) * .5f);
            ShadeSail(d.Props, V(-(K.Edge + .6f), y + 3.4f, z0 + 4f), V(-(K.Edge + 5.2f), y + 4.3f, z0 + 9f), V(-(K.Edge + .8f), y + 3.6f, z0 + 14f), SailRed, y);
            ShadeSail(d.Props, V(-(K.Edge + 5f), y + 3.5f, z0 + 21f), V(-(K.Edge + .7f), y + 4.4f, z0 + 26f), V(-(K.Edge + 5.2f), y + 3.6f, z0 + 31f), SailYellow, y);
            d.Collisions.Add(new DressingObstacle { id = "AFrame", position = V(-(K.Edge - .6f), d.Ground(z0 + 12f), z0 + 12f), rotation = V(0, 90f, 0), centre = V(0, .5f, 0), size = V(.9f, 1.05f, .7f), canJumpOver = true });
            AFrameArt(d.Props, V(-(K.Edge - .6f), d.Ground(z0 + 12f), z0 + 12f), 90f);
        }

        // One cafe unit (facade towards the road): plinth, glazed ground floor with white piers and a blue awning, the
        // white first floor with french doors behind a glass balcony, and a red tiled gable roof with a white gable.
        static void CafeUnit(ComicMesh f, CqLot l, Color wall, Color awning)
        {
            float W = l.W, D = l.Depth;
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            f.Surface = ComicSurface.Glass;
            K.LBox(f, l, .3f, W - .3f, 0f, 3.1f, -.5f, -D + .5f, GlassDark);
            f.Surface = ComicSurface.Plaster;
            foreach (float a in new[] { .25f, W * .5f, W - .25f }) K.LBox(f, l, a - .25f, a + .25f, 0f, 3.1f, 0f, -.6f, wall);
            f.Surface = ComicSurface.PaintedMetal;
            for (float a = 1.5f; a < W - .5f; a += 1.6f) K.LBox(f, l, a - .04f, a + .04f, 0f, 3.1f, -.42f, -.5f, White);
            K.LBox(f, l, -.2f, W + .2f, 3.1f, 3.32f, 2.3f, -.3f, awning);
            K.LBox(f, l, -.2f, W + .2f, 3.0f, 3.12f, 2.32f, 2.2f, Color.Lerp(awning, K.R.ink, .35f));
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0f, W, 3.32f, 6.3f, 0f, -D, wall);
            f.Surface = ComicSurface.Glass;
            for (float a = 1.2f; a < W - 1f; a += 2.6f)
            {
                K.LBox(f, l, a, a + 1.6f, 3.6f, 5.8f, .04f, -.1f, White);
                K.LBox(f, l, a + .12f, a + 1.48f, 3.7f, 5.7f, .07f, -.1f, GlassDark);
            }
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, .4f, W - .4f, 3.32f, 3.46f, 1.5f, 0f, White);
            f.Surface = ComicSurface.Glass;
            K.LBox(f, l, .45f, W - .45f, 3.46f, 4.4f, 1.47f, 1.4f, Balustrade);
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, .4f, W - .4f, 4.4f, 4.5f, 1.52f, 1.36f, White);
            foreach (float a in new[] { .45f, W - .45f }) K.LBox(f, l, a - .06f, a + .06f, 3.46f, 4.4f, 1.5f, 1.38f, White);
            // the gable roof (ridge running back from the road), white gable infill, ink eaves
            K.LBox(f, l, -.35f, W + .35f, 6.2f, 6.34f, .65f, -D - .3f, K.R.ink);
            f.Surface = ComicSurface.Brick;
            float ridge = 6.3f + W * .34f;
            K.LGable(f, l, -.4f, W + .4f, 6.3f, ridge, .6f, -D - .3f, CafeRoof);
            f.Surface = ComicSurface.Plaster;
            K.TriFacing(f, l.P(.55f, 6.4f, .62f), l.P(W - .55f, 6.4f, .62f), l.P(W * .5f, ridge - .55f, .62f), White, new Vector3(-l.S, 0, 0));
            f.Surface = ComicSurface.Glass;
            K.LBox(f, l, W * .5f - .45f, W * .5f + .45f, 6.9f, 7.6f, .66f, .6f, GlassDark);
        }

        // A single-storey shop row with a deep verandah and a flat parapet roof.
        static void ShopRow(CqChunkDraft d, float z0, float z1, int seed)
        {
            var l = new CqLot(-1, z0, z1, K.Edge + 3.5f, 12f, d.Base((z0 + z1) * .5f));
            ComicMesh f = d.Frontage;
            float W = l.W;
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, W, -2.6f, 0f, .02f, -l.Depth, K.R.ink);
            f.Surface = ComicSurface.Glass;
            K.LBox(f, l, .3f, W - .3f, 0f, 3f, -.4f, -l.Depth + .5f, GlassDark);
            f.Surface = ComicSurface.Plaster;
            for (float a = 0f; a <= W + .01f; a += W / 3f) K.LBox(f, l, Mathf.Max(0f, a - .3f), Mathf.Min(W, a + .3f), 0f, 3f, 0f, -.5f, Cream);
            K.LBox(f, l, 0f, W, 3f, 4.6f, 0f, -l.Depth, Cream);
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, -.2f, W + .2f, 2.9f, 3.15f, 2.6f, -.2f, Navy);
            for (float a = .3f; a < W; a += W / 3f) K.LBox(f, l, a - .05f, a + .05f, 0f, 2.9f, 2.45f, 2.55f, White);
            K.LBox(f, l, -.05f, W + .05f, 4.6f, 4.8f, .05f, -l.Depth - .05f, White);
            d.Wall(l, 6f);
        }

        // A beach house (single storey, hip-ish roof) set back behind a garden.
        static void BeachHouse(ComicMesh f, CqLot l, Color wall, Color roof)
        {
            float W = l.W, D = l.Depth;
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, W, -2.4f, .3f, .02f, -D, Limestone);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0, W, .3f, 3.3f, 0f, -D, wall);
            f.Surface = ComicSurface.Glass;
            for (float a = 1f; a < W - 1.5f; a += 3f) { K.LBox(f, l, a, a + 1.8f, 1f, 2.6f, .05f, -.1f, White); K.LBox(f, l, a + .1f, a + 1.7f, 1.1f, 2.5f, .08f, -.1f, GlassDark); }
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, -.3f, W + .3f, 3.25f, 3.4f, .5f, -D - .3f, K.R.ink);
            f.Surface = ComicSurface.Brick;
            K.LSlope(f, l, -.3f, W + .3f, 3.35f, 5.6f, .5f, -D * .5f, -D - .3f, roof);
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, -.4f, W * .4f, 2.9f, 3.05f, 2f, 0f, White);
            K.LBox(f, l, -.3f, -.2f, .3f, 2.9f, 1.95f, 1.85f, White);
        }

        // An apartment block: glazed ground floor, stacked white balconies with glass rails, a parapet and a plant room.
        static void Apartment(ComicMesh f, CqLot l, int floors, Color wall, Color trim, bool redCap)
        {
            float W = l.W, D = l.Depth, top = 3.6f + 3f * (floors - 1);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            f.Surface = ComicSurface.Glass;
            K.LBox(f, l, .3f, W - .3f, 0f, 3.4f, -.6f, -D + .5f, GlassDark);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0f, .6f, 0f, 3.4f, 0f, -D, wall);
            K.LBox(f, l, W - .6f, W, 0f, 3.4f, 0f, -D, wall);
            K.LBox(f, l, 0f, W, 3.4f, top, 0f, -D, wall);
            for (int k = 1; k < floors; k++)
            {
                float y = 3.6f + 3f * (k - 1);
                f.Surface = ComicSurface.Glass;
                K.LBox(f, l, .9f, W - .9f, y + .25f, y + 2.6f, .04f, -.1f, GlassDark);
                f.Surface = ComicSurface.PaintedMetal;
                K.LBox(f, l, .5f, W - .5f, y - .12f, y + .04f, 1.6f, 0f, trim);
                f.Surface = ComicSurface.Glass;
                K.LBox(f, l, .55f, W - .55f, y + .04f, y + 1.05f, 1.58f, 1.5f, Balustrade);
            }
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, -.1f, W + .1f, top, top + .5f, .1f, -D - .1f, trim);
            K.LBox(f, l, W * .3f, W * .7f, top + .5f, top + 2f, -2f, -D * .6f, redCap ? CafeRoof : AptCream);
        }

        // Second and third rows: apartments and Norfolk pines on the land side (no outline, no shadows).
        static void DeepTown(CqChunkDraft d, float z0, float z1, int seed)
        {
            ComicMesh m = d.Deep;
            var r = new System.Random(seed * 17 + 3);
            Color[] walls = { Sandstone, AptCream, White, K.Hex("D9CBB0"), PaleYellow };
            for (float z = z0; z < z1 - 2f; z += 13f)
            {
                float len = Mathf.Min(12f, z1 - z - 1f), zc = z + len * .5f, h = F(r, 12f, 30f), x = -(K.Edge + F(r, 26f, 34f)), w = F(r, 12f, 16f);
                Color c = walls[r.Next(walls.Length)];
                float y = d.Ground(zc) - 2f;
                m.Surface = ComicSurface.Plaster;
                m.Box(V(x, y + h * .5f, zc), V(w, h, len), c);
                m.Surface = ComicSurface.Glass;
                for (float by = 5f; by < h - 1f; by += 3f) m.Box(V(x + w * .5f + .02f, y + by, zc), V(.06f, 1.4f, len - 1.4f), Color.Lerp(GlassDark, c, .25f));
                m.Surface = ComicSurface.PaintedMetal;
                for (float by = 4f; by < h - 1f; by += 3f) m.Box(V(x + w * .5f + .5f, y + by, zc), V(1f, .14f, len - 1f), White);
                if (r.NextDouble() < .5) m.Box(V(x, y + h + .6f, zc), V(w * .5f, 1.2f, len * .5f), CafeRoof);
            }
            for (float z = z0 + 4f; z < z1; z += 12f) CheapPine(m, V(-(K.Edge + F(r, 14f, 20f)), d.Ground(z), z), F(r, 13f, 19f));
        }

        static void Bench(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.Wood;
            m.Box(p + q * V(0, .45f, 0), V(1.8f, .08f, .45f), Timber, q);
            m.Box(p + q * V(0, .8f, .22f), V(1.8f, .35f, .07f), Timber, q);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.75f, .75f }) m.Box(p + q * V(x, .4f, .05f), V(.08f, .8f, .5f), K.R.ink, q);
        }

        // ---------------------------------------------------------------- Rockingham props and landmarks

        /// <summary>Norfolk Island pine: tall straight tapering trunk on an ink root ring, whorls of thick frond pads
        /// (two bent slabs each, dark green on top, darker underneath) shorter towards the top, a light leader tuft.</summary>
        static void NorfolkPine(ComicMesh m, Vector3 p, float h, int seed)
        {
            var r = new System.Random(seed * 131 + 7);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .08f, 0), .5f + h * .012f, .16f, K.R.ink, 8, .9f);
            float tr = .2f + h * .012f;
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, h * .5f, 0), tr, h, PineBark, 8, .25f);
            // Bare trunk for the lowest third (the foreshore pines are limbed up), then horizontal whorls of fronds: each
            // frond goes out level, droops, and its tip turns up again (three thin bent slabs, like the kit's palm fronds).
            int tiers = h > 14f ? 8 : 6;
            float spin = F(r, 0f, 360f);
            for (int i = 0; i < tiers; i++)
            {
                float t = i / (float)(tiers - 1);
                float y = Mathf.Lerp(h * .36f, h * .93f, t) + F(r, -.25f, .25f);
                float reach = Mathf.Lerp(h * .22f, h * .07f, t) * F(r, .85f, 1.1f);
                int arms = i >= tiers - 2 ? 4 : 5;
                Color top = i % 2 == 0 ? PineDark : PineMid, mid = Color.Lerp(top, PineLight, .3f), tip = Color.Lerp(top, PineLight, .6f);
                m.Surface = ComicSurface.Foliage;
                for (int j = 0; j < arms; j++)
                {
                    float a = (spin + i * 37f + j * 360f / arms + F(r, -14f, 14f)) * Mathf.Deg2Rad;
                    Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a)), side = Vector3.Cross(Vector3.up, dir);
                    float rad = Mathf.Lerp(tr, tr * .25f, y / h);
                    Vector3 a0 = p + V(0, y, 0) + dir * rad * .5f;
                    Vector3 a1 = a0 + dir * reach * .45f + V(0, reach * .05f, 0);
                    Vector3 a2 = a1 + dir * reach * .38f + V(0, -reach * .17f, 0);
                    Vector3 a3 = a2 + dir * reach * .22f + V(0, reach * .05f, 0);
                    float w = .35f + reach * .32f, thick = .14f + reach * .035f;
                    Slab(m, a0, a1, side, w * .35f, w, thick, top, PineUnder);
                    Slab(m, a1, a2, side, w, w * .85f, thick, mid, PineUnder);
                    Slab(m, a2, a3, side, w * .85f, w * .3f, thick * .8f, tip, PineUnder);
                }
            }
            m.Surface = ComicSurface.Foliage;
            m.Cylinder(p + V(0, h * .97f, 0), h * .03f + .15f, h * .1f, PineLight, 6, .2f);
        }

        /// <summary>Background Norfolk pine: thin trunk and four stacked flat tiers (no fronds).</summary>
        static void CheapPine(ComicMesh m, Vector3 p, float h, Color? tint = null)
        {
            Color dark = tint ?? PineDark, mid = tint.HasValue ? Color.Lerp(tint.Value, Color.white, .08f) : PineMid;
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, h * .5f, 0), .22f + h * .008f, h, tint.HasValue ? Color.Lerp(PineBark, tint.Value, .5f) : PineBark, 6, .3f);
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < 4; i++)
            {
                float t = i / 3f, y = Mathf.Lerp(h * .38f, h * .9f, t), rr = Mathf.Lerp(h * .22f, h * .08f, t);
                m.Cylinder(p + V(0, y, 0), rr, h * .07f, i % 2 == 0 ? dark : mid, 6, .55f);
            }
        }

        /// <summary>A slab from a to b (thick, width wa -> wb), top colour up, darker underneath (like the kit's palm fronds).</summary>
        static void Slab(ComicMesh m, Vector3 a, Vector3 b, Vector3 side, float wa, float wb, float thick, Color top, Color under)
        {
            Vector3 along = (b - a).normalized, n = Vector3.Cross(along, side).normalized;
            if (n.y < 0f) n = -n;
            Vector3 A(float w, float t) => a + side * w + n * t;
            Vector3 B(float w, float t) => b + side * w + n * t;
            float ha = wa * .5f, hb = wb * .5f, ht = thick * .5f;
            Color edge = Color.Lerp(top, under, .5f);
            K.Face(m, A(-ha, ht), B(-hb, ht), B(hb, ht), A(ha, ht), top, n);
            K.Face(m, A(-ha, -ht), B(-hb, -ht), B(hb, -ht), A(ha, -ht), under, -n);
            K.Face(m, A(-ha, -ht), B(-hb, -ht), B(-hb, ht), A(-ha, ht), edge, -side);
            K.Face(m, A(ha, -ht), B(hb, -ht), B(hb, ht), A(ha, ht), edge, side);
            K.Face(m, A(-ha, -ht), A(ha, -ht), A(ha, ht), A(-ha, ht), edge, -along);
            K.Face(m, B(-hb, -ht), B(hb, -ht), B(hb, ht), B(-hb, ht), edge, along);
        }

        /// <summary>The foreshore lamp: ink base, tall pale pole, a cross arm along the road with two lantern heads.</summary>
        static void TwinLamp(ComicMesh m, Vector3 p)
        {
            Color pole = K.Hex("E4EEF2");
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .12f, 0), .2f, .24f, K.R.ink, 8, .9f);
            m.Cylinder(p + V(0, 3.6f, 0), .09f, 7.2f, pole, 8, .75f);
            m.Box(p + V(0, 6.7f, 0), V(.09f, .09f, 2f), pole);
            foreach (float z in new[] { -.95f, .95f })
            {
                m.Cylinder(p + V(0, 6.42f, z), .2f, .42f, K.R.cream, 8, .7f);
                m.Cylinder(p + V(0, 6.68f, z), .25f, .1f, K.R.ink, 8);
            }
            m.Cylinder(p + V(0, 7.4f, 0), .05f, .4f, pole, 6);
        }

        /// <summary>A jetty lamp: black post with a lantern and a white cap.</summary>
        static void JettyLamp(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 2.2f, 0), .07f, 4.4f, AnchorBlack, 8);
            m.Box(p + V(0, 4.55f, 0), V(.34f, .44f, .34f), K.R.cream);
            m.Box(p + V(0, 4.82f, 0), V(.44f, .1f, .44f), AnchorBlack);
            m.Cylinder(p + V(0, .1f, 0), .16f, .2f, AnchorBlack, 8);
        }

        /// <summary>Beach shade: a coloured canvas cone on a white pole (the foreshore's shade umbrellas).</summary>
        static void BeachShade(ComicMesh m, Vector3 p, Color c)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .1f, 0), .25f, .2f, K.R.ink, 8);
            m.Cylinder(p + V(0, 1.75f, 0), .08f, 3.5f, White, 8);
            m.Surface = ComicSurface.Fabric;
            m.Cylinder(p + V(0, 3.5f, 0), 2.3f, .8f, c, 8, .04f);
            m.Cylinder(p + V(0, 3.05f, 0), 2.32f, .14f, Color.Lerp(c, K.R.ink, .3f), 8);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 3.98f, 0), .1f, .2f, White, 8);
        }

        /// <summary>A shade sail: a thick fabric triangle on three ink posts down to the ground.</summary>
        static void ShadeSail(ComicMesh f, Vector3 p0, Vector3 p1, Vector3 p2, Color c, float ground)
        {
            f.Surface = ComicSurface.Fabric;
            Vector3 t = V(0, .07f, 0), centre = (p0 + p1 + p2) / 3f;
            K.TriFacing(f, p0 + t, p1 + t, p2 + t, c, Vector3.up);
            K.TriFacing(f, p0, p1, p2, Color.Lerp(c, K.R.ink, .25f), Vector3.down);
            foreach ((Vector3 u, Vector3 v) in new[] { (p0, p1), (p1, p2), (p2, p0) })
            {
                Vector3 n = Vector3.Cross(v - u, Vector3.up).normalized;
                if (Vector3.Dot(n, (u + v) * .5f - centre) < 0f) n = -n;
                K.Face(f, u, v, v + t, u + t, c, n);
            }
            f.Surface = ComicSurface.PaintedMetal;
            foreach (Vector3 q in new[] { p0, p1, p2 })
            {
                f.Cylinder(new Vector3(q.x, (ground + q.y) * .5f, q.z), .07f, q.y - ground, K.R.ink, 6);
                f.Cylinder(new Vector3(q.x, ground + .08f, q.z), .18f, .16f, K.R.ink, 6);
            }
        }

        /// <summary>Picnic shelter: white posts on a pad, a teal-green hipped roof with ink eaves, timber table and benches.</summary>
        static void PicnicShelter(ComicMesh f, Vector3 p)
        {
            f.Surface = ComicSurface.Concrete;
            f.Box(p + V(0, .06f, 0), V(5.4f, .12f, 4.4f), K.Hex("D9D2C3"));
            f.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -2.3f, 2.3f }) foreach (float z in new[] { -1.8f, 1.8f }) f.Cylinder(p + V(x, 1.35f, z), .09f, 2.5f, White, 8);
            f.Box(p + V(0, 2.66f, 0), V(5.3f, .14f, 4.3f), K.R.ink);
            f.Cylinder(p + V(0, 3.2f, 0), 3.7f, 1f, K.Hex("2F7F78"), 4, .12f, Quaternion.Euler(0, 45, 0));
            f.Surface = ComicSurface.Wood;
            f.Box(p + V(0, .8f, 0), V(2.2f, .08f, .9f), Timber);
            foreach (float z in new[] { -.8f, .8f }) f.Box(p + V(0, .48f, z), V(2.2f, .07f, .32f), Timber);
            f.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.9f, .9f }) { f.Box(p + V(x, .42f, 0), V(.08f, .8f, .5f), K.R.ink); f.Box(p + V(x, .24f, 0), V(.08f, .08f, 1.8f), K.R.ink); }
        }

        /// <summary>Foreshore playground: soft-fall pad, red tower with a blue roof, yellow slide, blue climbing frame, sail.</summary>
        static void Playground(ComicMesh f, Vector3 pg)
        {
            Color blue = K.Hex("2E5E9E");
            f.Surface = ComicSurface.Rubber;
            f.Box(pg + V(0f, .04f, 1f), V(8f, .08f, 10f), K.Hex("2F8F7A"));
            f.Surface = ComicSurface.PaintedMetal;
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2) f.Box(pg + V(x * .75f, 1.6f, z * .75f), V(.16f, 3.2f, .16f), K.R.red);
            f.Box(pg + V(0, 1.5f, 0), V(1.8f, .14f, 1.8f), K.R.yellow);
            for (int x = -1; x <= 1; x += 2) f.Box(pg + V(x * .75f, 2.05f, 0), V(.1f, .5f, 1.5f), K.R.red);
            f.Box(pg + V(0, 3.25f, 0), V(2f, .12f, 2f), K.R.ink);
            f.Cylinder(pg + V(0, 3.75f, 0), 1.35f, .9f, blue, 4, .08f, Quaternion.Euler(0, 45, 0));
            f.Surface = ComicSurface.Plastic;
            Vector3 a = pg + V(-.9f, 1.55f, 0), b = pg + V(-3.6f, .3f, 0);
            f.Box((a + b) * .5f, V(.9f, .1f, (b - a).magnitude), K.R.yellow, Quaternion.LookRotation(b - a));
            for (int z = -1; z <= 1; z += 2) f.Beam(a + V(0, .15f, z * .45f), b + V(0, .15f, z * .45f), .1f, K.R.red);
            f.Box(b + V(-.4f, -.1f, 0), V(1f, .1f, .9f), K.R.yellow);
            f.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 2; i++)
            {
                float z = 2.4f + i * 1.8f;
                f.Beam(pg + V(-1f, 0, z), pg + V(0, 2.2f, z), .14f, blue);
                f.Beam(pg + V(1f, 0, z), pg + V(0, 2.2f, z), .14f, blue);
            }
            f.Box(pg + V(0, 2.2f, 3.3f), V(.14f, .14f, 2f), blue);
            // swings
            f.Beam(pg + V(-1.4f, 0, -3.2f), pg + V(-1.4f, 2.4f, -2.4f), .12f, K.R.yellow);
            f.Beam(pg + V(-1.4f, 0, -1.6f), pg + V(-1.4f, 2.4f, -2.4f), .12f, K.R.yellow);
            f.Beam(pg + V(1.4f, 0, -3.2f), pg + V(1.4f, 2.4f, -2.4f), .12f, K.R.yellow);
            f.Beam(pg + V(1.4f, 0, -1.6f), pg + V(1.4f, 2.4f, -2.4f), .12f, K.R.yellow);
            f.Box(pg + V(0, 2.4f, -2.4f), V(2.9f, .12f, .12f), K.R.yellow);
            foreach (float x in new[] { -.6f, .6f }) { f.Box(pg + V(x, 1.5f, -2.4f), V(.03f, 1.8f, .03f), K.R.ink); f.Box(pg + V(x, .58f, -2.4f), V(.5f, .06f, .22f), K.R.red); }
            ShadeSail(f, pg + V(-3.6f, 4.3f, 4.5f), pg + V(2.8f, 3.8f, 5.5f), pg + V(-1f, 4.6f, -1.5f), White, pg.y);
        }

        /// <summary>Teal "kelp" sculpture: a tall tapered stem that flares into a trumpet of thin ribs with a rim.</summary>
        static void TealSculpture(ComicMesh m, Vector3 p, float h, float lean)
        {
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(p + V(0, .1f, 0), .5f, .2f, K.Hex("C9C4BA"), 8);
            m.Surface = ComicSurface.PaintedMetal;
            Vector3 Stem(float t) => p + V(lean * t * t, h * .78f * t, 0);
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = Stem(i / 4f), b = Stem((i + 1) / 4f);
                m.Cylinder((a + b) * .5f, Mathf.Lerp(.2f, .1f, i / 4f), (b - a).magnitude, Teal, 8, .9f, Quaternion.FromToRotation(Vector3.up, b - a));
            }
            Vector3 top = Stem(1f);
            const int ribs = 9;
            var tips = new Vector3[ribs];
            for (int k = 0; k < ribs; k++)
            {
                float ang = k * Mathf.PI * 2f / ribs;
                tips[k] = top + V(Mathf.Cos(ang) * h * .17f, h * .22f, Mathf.Sin(ang) * h * .17f);
                m.Beam(top, tips[k], .09f, Color.Lerp(Teal, Color.white, k % 2 == 0 ? .0f : .15f));
            }
            for (int k = 0; k < ribs; k++) m.Beam(tips[k], tips[(k + 1) % ribs], .07f, Teal);
        }

        /// <summary>The white sea rescue building on the beach: lower hall with a window band and blue fabric canopies,
        /// the upper lookout with a dark window band all round, a flat overhanging roof, an antenna mast. Front at -X.</summary>
        static void SeaRescue(ComicMesh f, Vector3 p)
        {
            f.Surface = ComicSurface.Plaster;
            f.Box(p + V(0, 1.75f, 0), V(9f, 3.5f, 20f), White);
            f.Surface = ComicSurface.Glass;
            f.Box(p + V(-4.52f, 1.9f, 0), V(.06f, 1.7f, 17f), GlassDark);
            f.Box(p + V(4.52f, 1.9f, 0), V(.06f, 1.7f, 17f), GlassDark);
            f.Surface = ComicSurface.PaintedMetal;
            for (float z = -8f; z <= 8.01f; z += 2f) f.Box(p + V(-4.56f, 1.9f, z), V(.06f, 1.8f, .12f), White);
            f.Box(p + V(0, 3.55f, 0), V(9.4f, .2f, 20.4f), White);
            f.Box(p + V(0, 3.42f, 0), V(9.3f, .1f, 20.3f), K.R.ink);
            for (int k = -1; k <= 1; k++)
            {
                Vector3 c = p + V(-6.6f, 3.1f, k * 6.2f);
                f.Surface = ComicSurface.Fabric;
                f.Cylinder(c, 3.3f, .7f, CanopyBlue, 4, .2f, Quaternion.Euler(0, 45, 0));
                f.Surface = ComicSurface.PaintedMetal;
                foreach (float dz in new[] { -2.1f, 2.1f }) f.Cylinder(new Vector3(c.x - 1.7f, (p.y + c.y) * .5f, c.z + dz), .07f, c.y - p.y, White, 6);
            }
            // upper lookout
            f.Surface = ComicSurface.Plaster;
            f.Box(p + V(1f, 5.2f, -3f), V(6f, 3.2f, 8f), White);
            f.Surface = ComicSurface.Glass;
            f.Box(p + V(1f, 5.5f, -3f), V(6.1f, 1.3f, 8.1f), GlassDark);
            f.Surface = ComicSurface.PaintedMetal;
            for (float z = -6.5f; z <= .51f; z += 1.75f) f.Box(p + V(-2.07f, 5.5f, z), V(.06f, 1.4f, .1f), White);
            f.Box(p + V(1f, 6.95f, -3f), V(7.4f, .26f, 9.4f), White);
            f.Box(p + V(1f, 6.78f, -3f), V(7.2f, .1f, 9.2f), K.R.ink);
            for (float z = 1.8f; z < 9.8f; z += 2f) f.Box(p + V(-4.3f, 4.1f, z), V(.08f, 1f, .08f), White);
            f.Box(p + V(-4.3f, 4.55f, 5.8f), V(.1f, .1f, 8f), White);
            f.Cylinder(p + V(2.5f, 9f, -5f), .07f, 4f, K.R.ink, 6);
            f.Box(p + V(2.5f, 10.4f, -5f), V(1.6f, .06f, .06f), K.R.ink);
            f.Box(p + V(2.5f, 10.7f, -4.55f), V(.06f, .5f, .8f), K.R.red);
        }

        /// <summary>The naval memorial park (background, never hittable): a sandstone wall with bronze plaques, a big black
        /// anchor on a plinth, a white naval mast with a yardarm and signal flags, two flagpoles with navy flags.</summary>
        static void NavalMemorial(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(-3.5f, .75f, 6f), V(.9f, 1.5f, 10f), Sandstone);
            m.Box(p + V(-3.5f, 1.55f, 6f), V(1.05f, .12f, 10.2f), LimestoneCap);
            m.Surface = ComicSurface.Metal;
            for (float z = 2f; z <= 10.01f; z += 2.6f) m.Box(p + V(-3.03f, .9f, z), V(.06f, .7f, 1.4f), Bronze);
            // the anchor
            Vector3 a = p + V(1.5f, 0, -6f);
            m.Surface = ComicSurface.Concrete;
            m.Box(a + V(0, .4f, 0), V(2.4f, .8f, 2.4f), Sandstone);
            m.Surface = ComicSurface.PaintedMetal;
            Vector3 b = a + V(0, .8f, 0);
            m.Beam(b + V(0, .5f, 0), b + V(0, 4.2f, 0), .32f, AnchorBlack);
            m.Beam(b + V(0, 3.8f, -1.3f), b + V(0, 3.8f, 1.3f), .26f, AnchorBlack);
            m.Cylinder(b + V(0, 4.55f, 0), .4f, .14f, AnchorBlack, 8, 1f, Quaternion.Euler(90, 0, 0));
            for (int i = 1; i <= 4; i++)
            {
                float ang = i * 20f * Mathf.Deg2Rad;
                foreach (int s in new[] { -1, 1 })
                {
                    Vector3 q = b + V(0, .5f + 1.4f * (1f - Mathf.Cos(ang)), s * 1.4f * Mathf.Sin(ang));
                    Vector3 q0 = b + V(0, .5f + 1.4f * (1f - Mathf.Cos(ang - 20f * Mathf.Deg2Rad)), s * 1.4f * Mathf.Sin(ang - 20f * Mathf.Deg2Rad));
                    m.Beam(q0, q, .3f, AnchorBlack);
                    if (i == 4) m.Box(q + V(0, .15f, s * .1f), V(.3f, .6f, .5f), AnchorBlack, Quaternion.Euler(s * 30f, 0, 0));
                }
            }
            // the naval mast with yardarm, gaff and signal flags
            Vector3 mast = p + V(3f, 0, 3f);
            m.Surface = ComicSurface.Concrete;
            m.Box(mast + V(0, .2f, 0), V(1.6f, .4f, 1.6f), Sandstone);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(mast + V(0, 6.2f, 0), .14f, 11.6f, White, 8, .6f);
            m.Box(mast + V(0, 9f, 0), V(.12f, .12f, 5f), White);
            m.Beam(mast + V(0, 7f, 0), mast + V(1.6f, 8.6f, 0), .08f, White);
            // dressed with signal flags on a halyard from the yardarm down to a post
            Color[] flags = { K.R.red, K.R.yellow, Navy, White, K.R.red, K.R.yellow, Navy };
            Vector3 h0 = mast + V(0, 8.95f, -2.45f), h1 = mast + V(0, 1.2f, -6.5f);
            m.Beam(h0, h1, .03f, K.R.ink);
            m.Box(h1 + V(0, -.6f, 0), V(.12f, 1.2f, .12f), White);
            for (int i = 0; i < flags.Length; i++)
            {
                Vector3 q = Vector3.Lerp(h0, h1, (i + 1f) / (flags.Length + 1f));
                m.Surface = ComicSurface.Fabric;
                m.Box(q + V(0, -.2f, 0), V(.04f, .34f, .42f), flags[i]);
            }
            foreach (float z in new[] { 9f, 11.5f })
            {
                Vector3 f = p + V(-1f, 0, z);
                m.Surface = ComicSurface.PaintedMetal;
                m.Cylinder(f + V(0, 4.5f, 0), .07f, 9f, White, 8);
                m.Surface = ComicSurface.Fabric;
                m.Box(f + V(0, 8.2f, -.85f), V(.04f, 1f, 1.6f), Navy);
            }
        }

        static void Dinghy(ComicMesh m, Vector3 p, float yaw, Color hull)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + q * V(0, .25f, 0), V(1.4f, .5f, 3.2f), White, q);
            m.Box(p + q * V(0, .44f, 0), V(1.44f, .12f, 3.24f), hull, q);
            m.Box(p + q * V(0, .32f, 1.75f), V(.9f, .35f, .5f), White, q);
            m.Surface = ComicSurface.Wood;
            m.Box(p + q * V(0, .5f, -.2f), V(1.3f, .08f, .3f), Timber, q);
        }

        /// <summary>A sailboat: white hull with a coloured band, ink deck, mast, mainsail and jib (thick triangles).</summary>
        static void Sailboat(ComicMesh m, Vector3 p, float s, Color band)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, .35f * s, 0), V(1.5f * s, .7f * s, 4.6f * s), White);
            m.Box(p + V(0, .35f * s, 2.5f * s), V(.9f * s, .6f * s, .6f * s), White);
            m.Box(p + V(0, .62f * s, 0), V(1.52f * s, .14f * s, 4.62f * s), band);
            m.Box(p + V(0, .76f * s, -.4f * s), V(1f * s, .14f * s, 1.6f * s), K.R.ink);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(p + V(0, 4f * s, .4f * s), .07f * s, 6.6f * s, K.R.ink, 6);
            m.Surface = ComicSurface.Fabric;
            Vector3 foot = p + V(0, 1f * s, .3f * s), head = p + V(0, 7f * s, .35f * s), clew = p + V(0, 1.1f * s, -1.9f * s);
            foreach (float x in new[] { -.04f, .04f }) K.TriFacing(m, foot + V(x * s, 0, 0), head + V(x * s, 0, 0), clew + V(x * s, 0, 0), White, new Vector3(Mathf.Sign(x), 0, 0));
            Vector3 jf = p + V(0, 1f * s, 2.6f * s), jh = p + V(0, 5.6f * s, .5f * s), jc = p + V(0, 1f * s, .8f * s);
            foreach (float x in new[] { -.04f, .04f }) K.TriFacing(m, jf + V(x * s, 0, 0), jh + V(x * s, 0, 0), jc + V(x * s, 0, 0), Color.Lerp(White, band, .15f), new Vector3(Mathf.Sign(x), 0, 0));
        }

        /// <summary>A dolphin arcing out of the water: a curved grey body of beams, pale belly, dorsal fin, tail flukes.</summary>
        static void Dolphin(ComicMesh m, Vector3 p, float yaw, float s)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            Color grey = K.Hex("5F6F7C"), belly = K.Hex("C9D1D6");
            m.Surface = ComicSurface.Plastic;
            Vector3 P(float t) => p + q * V(0, Mathf.Sin(t * Mathf.PI) * 1.1f * s - .2f * s, (t - .5f) * 3f * s);
            for (int i = 0; i < 5; i++)
            {
                float t0 = i / 5f, t1 = (i + 1) / 5f, th = Mathf.Lerp(.55f, .22f, Mathf.Abs(t0 + .1f - .45f) * 2f) * s;
                m.Beam(P(t0), P(t1), th, grey);
                m.Beam(P(t0) + V(0, -th * .3f, 0), P(t1) + V(0, -th * .3f, 0), th * .7f, belly);
            }
            Vector3 top = P(.5f);
            m.Box(top + q * V(0, .35f * s, .1f * s), V(.12f * s, .5f * s, .5f * s), grey, q * Quaternion.Euler(-25f, 0, 0));
            Vector3 tail = P(0f);
            m.Box(tail + q * V(0, -.1f * s, -.15f * s), V(.9f * s, .08f * s, .35f * s), grey, q);
            m.Box(P(1f) + q * V(0, 0, .2f * s), V(.16f * s, .16f * s, .4f * s), grey, q);
        }

        // ---------------------------------------------------------------- Rockingham obstacles and animals

        // A beach bin in a timber-slat surround with an ink lid and a cream band.
        static ComicMesh BeachBinMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Box(V(0, .06f, 0), V(.78f, .12f, .78f), K.R.ink);
            m.Surface = ComicSurface.Wood;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                m.Box(V(Mathf.Cos(a) * .33f, .52f, Mathf.Sin(a) * .33f), V(.2f, .86f, .07f), i % 2 == 0 ? Timber : Color.Lerp(Timber, K.R.ink, .15f), Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90f, 0));
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(V(0, .5f, 0), .3f, .8f, K.Hex("2F6B3E"), 8);
            m.Cylinder(V(0, .99f, 0), .42f, .1f, K.R.ink, 8);
            m.Cylinder(V(0, .8f, 0), .4f, .08f, K.R.cream, 8);
            return m;
        }

        // Three ink-navy hoops on a ground rail.
        static ComicMesh BikeRackMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Box(V(0, .04f, 0), V(2.2f, .08f, .16f), K.R.ink);
            foreach (float x in new[] { -.75f, 0f, .75f })
            {
                m.Box(V(x - .27f, .42f, 0), V(.08f, .8f, .08f), Navy);
                m.Box(V(x + .27f, .42f, 0), V(.08f, .8f, .08f), Navy);
                m.Box(V(x, .84f, 0), V(.62f, .08f, .08f), Navy);
                m.Box(V(x, .55f, 0), V(.46f, .05f, .05f), K.R.cream);
            }
            return m;
        }

        // A striped beach umbrella planted in a sand pile, with a towel and a bag.
        static ComicMesh BeachUmbrellaMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Concrete };
            m.Cylinder(V(0, .08f, 0), .45f, .16f, Sand, 8, .5f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(V(0, 1.2f, 0), .05f, 2.3f, White, 8);
            StripedCanopy(m, V(0, 2.1f, 0), 1.35f, .45f, 8, SailRed, White);
            m.Cylinder(V(0, 2.6f, 0), .07f, .14f, K.R.ink, 8);
            m.Surface = ComicSurface.Fabric;
            m.Box(V(.25f, .03f, .35f), V(.7f, .04f, 1.5f), SailTeal, Quaternion.Euler(0, 12f, 0));
            m.Box(V(.32f, .04f, .5f), V(.72f, .045f, .18f), White, Quaternion.Euler(0, 12f, 0));
            m.Box(V(-.42f, .18f, .2f), V(.4f, .32f, .22f), SailYellow);
            m.Box(V(-.42f, .38f, .2f), V(.26f, .08f, .04f), K.R.ink);
            return m;
        }

        static void StripedCanopy(ComicMesh m, Vector3 c, float radius, float height, int sides, Color a, Color b)
        {
            m.Surface = ComicSurface.Fabric;
            Vector3 apex = c + V(0, height, 0);
            for (int i = 0; i < sides; i++)
            {
                float t0 = i * Mathf.PI * 2f / sides, t1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 r0 = c + V(Mathf.Cos(t0) * radius, 0, Mathf.Sin(t0) * radius), r1 = c + V(Mathf.Cos(t1) * radius, 0, Mathf.Sin(t1) * radius);
                Color col = i % 2 == 0 ? a : b;
                K.TriFacing(m, apex, r0, r1, col, Vector3.up);
                K.TriFacing(m, c + V(0, height * .3f, 0), r0, r1, Color.Lerp(col, K.R.ink, .35f), Vector3.down);
                Vector3 o = ((r0 + r1) * .5f - c).normalized;
                K.Face(m, r0, r1, r1 + V(0, -.16f, 0), r0 + V(0, -.16f, 0), col, o);
            }
        }

        // A red esky with a white lid and handle.
        static ComicMesh EskyMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            m.Box(V(0, .04f, 0), V(.84f, .08f, .54f), K.R.ink);
            m.Box(V(0, .3f, 0), V(.8f, .46f, .5f), K.Hex("D23B2E"));
            m.Box(V(0, .57f, 0), V(.84f, .1f, .54f), White);
            m.Box(V(0, .66f, 0), V(.5f, .07f, .1f), White);
            foreach (float x in new[] { -.21f, .21f }) m.Box(V(x, .64f, 0), V(.07f, .07f, .1f), White);
            m.Box(V(0, .32f, -.26f), V(.5f, .16f, .03f), White);
            foreach (float x in new[] { -.43f, .43f }) m.Box(V(x, .42f, 0), V(.06f, .1f, .26f), White);
            return m;
        }

        static ComicMesh AFrameMesh()
        {
            var m = new ComicMesh();
            AFrameArt(m, Vector3.zero, 0f);
            return m;
        }

        // A cafe's chalkboard A-frame: two leaning boards (white timber frame, dark board, chalk lines and a cup icon).
        static void AFrameArt(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            foreach (int s in new[] { -1, 1 })
            {
                Quaternion t = q * Quaternion.Euler(s * 14f, 0, 0);
                Vector3 c = p + q * V(0, .5f, s * .13f);
                m.Surface = ComicSurface.Wood;
                m.Box(c, V(.66f, 1.04f, .05f), White, t);
                m.Surface = ComicSurface.Plain;
                m.Box(c + t * V(0, .02f, s * .02f), V(.54f, .86f, .03f), K.Hex("2F3B36"), t);
                for (int i = 0; i < 3; i++) m.Box(c + t * V(0, .28f - i * .14f, s * .04f), V(.36f - i * .06f, .03f, .01f), K.R.cream, t);
                m.Box(c + t * V(0, -.18f, s * .04f), V(.16f, .14f, .01f), SailOrange, t);
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + q * V(0, 1.02f, 0), V(.66f, .06f, .1f), K.R.ink, q);
        }

        // Australian pelican (after MountsBayBuilder's): white body, black flight feathers, huge pale-pink bill and pouch.
        static ComicMesh PelicanMesh()
        {
            var m = new ComicMesh();
            Color white = new Color(.97f, .97f, .95f), black = new Color(.12f, .12f, .14f), bill = new Color(.98f, .78f, .7f),
                pouch = new Color(.98f, .86f, .6f), legs = new Color(.55f, .6f, .68f);
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(V(0, .64f, .12f), V(.6f, .54f, .9f), white);
            foreach (float x in new[] { -.27f, .27f })
            {
                m.Box(V(x, .74f, .12f), V(.09f, .22f, .5f), white, Quaternion.Euler(-10f, 0, 0));
                m.Box(V(x, .68f, .5f), V(.09f, .24f, .4f), black, Quaternion.Euler(-10f, 0, 0));
            }
            m.Box(V(0, .72f, .6f), V(.32f, .14f, .32f), black, Quaternion.Euler(-20f, 0, 0));
            m.Beam(V(0, .82f, -.12f), V(0, 1.14f, -.24f), .2f, white);
            m.Ellipsoid(V(0, 1.2f, -.3f), V(.26f, .24f, .3f), white);
            foreach (float x in new[] { -.08f, .08f }) m.Box(V(x, 1.25f, -.39f), V(.04f, .05f, .05f), K.R.ink);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, 1.15f, -.72f), V(.13f, .08f, .7f), bill, Quaternion.Euler(12f, 0, 0));
            m.Box(V(0, 1.06f, -.66f), V(.15f, .16f, .56f), pouch, Quaternion.Euler(16f, 0, 0));
            m.Box(V(0, 1.1f, -1.05f), V(.1f, .1f, .1f), new Color(.9f, .55f, .45f));
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.11f, .11f })
            {
                m.Cylinder(V(x, .2f, .1f), .04f, .4f, legs, 8);
                m.Box(V(x, .03f, .02f), V(.18f, .06f, .22f), legs);
            }
            return m;
        }

        // Chunky silver gull (after QuayBuilder's): white body, grey wings with ink tips, yellow beak, red spot, legs.
        static ComicMesh SeagullMesh()
        {
            var a = new ComicMesh { Surface = ComicSurface.Plastic };
            Color grey = K.Hex("AEB4BA"), yellow = K.Hex("F3B23A"), red = K.Hex("E04A3A");
            a.Box(V(0, .34f, .02f), V(.3f, .26f, .52f), White);
            a.Box(V(0, .3f, -.2f), V(.26f, .2f, .18f), White);
            a.Box(V(0, .52f, -.24f), V(.2f, .2f, .22f), White);
            a.Box(V(0, .5f, -.4f), V(.07f, .07f, .14f), yellow);
            a.Box(V(0, .47f, -.45f), V(.06f, .05f, .05f), K.R.red);
            foreach (int s in new[] { -1, 1 })
            {
                a.Box(V(s * .17f, .4f, .08f), V(.07f, .2f, .46f), grey, Quaternion.Euler(0, 0, s * -10f));
                a.Box(V(s * .17f, .42f, .34f), V(.08f, .12f, .16f), K.R.ink, Quaternion.Euler(0, 0, s * -10f));
                a.Box(V(s * .105f, .56f, -.3f), V(.03f, .05f, .05f), K.R.ink);
                a.Box(V(s * .06f, .12f, 0), V(.06f, .22f, .06f), red);
                a.Box(V(s * .06f, .03f, -.04f), V(.1f, .06f, .14f), red);
            }
            a.Box(V(0, .38f, .33f), V(.2f, .08f, .16f), White);
            a.Box(V(0, .38f, .43f), V(.18f, .08f, .06f), K.R.ink);
            return a;
        }

        // An animal walker (kind Person, animal, never punchable). Facing -Z, feet at the pivot.
        static GameObject SaveAnimal(string path, string meshId, ComicMesh art, Vector3 centre, Vector3 size, float walk, bool jump)
        {
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            GameObject go = sync.Plain("Art");
            K.Renderer(go, meshId, art, true, false, false);
            sync.Finish();
            var box = PrefabBuildUtility.KeepSingle<BoxCollider>(root) ?? root.AddComponent<BoxCollider>();
            box.center = centre; box.size = size; box.isTrigger = false;
            var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
            o.kind = ObstacleKind.Person; o.animal = true; o.punchable = false; o.police = false; o.canJumpOver = jump; o.spansRoad = false; o.spawnWeight = 1f;
            var ped = PrefabBuildUtility.GetOrAdd<Pedestrian>(root);
            ped.walkSpeed = walk; ped.crosses = false; ped.art = go.transform; ped.dodgeSpeed = 6f;
            PrefabBuildUtility.SaveAndClose(root, path);
            K.Report.Add(System.IO.Path.GetFileNameWithoutExtension(path) + " " + art.TriangleCount * 2 + " tris");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // ---------------------------------------------------------------- Rockingham far scenery

        /// <summary>
        /// The whole horizon around the rider (the CBD panorama is hidden down south): Cockburn Sound on the right, turquoise
        /// near and paler with distance, sailboats and dolphins; Garden Island long and low across the water; Penguin Island
        /// and the Point Peron headland ahead-right; on the land side rows of tiled roofs and Norfolk pines, a tuart tree line
        /// and the hazy Darling Scarp far behind. Sheared with the road's grade so it meets the chunks' water. No colliders.
        /// </summary>
        static ComicMesh RockFar(float grade)
        {
            var m = new ComicMesh();
            var r = new System.Random(140);
            Color haze = K.Hex("C3DCE8");
            const float z0 = -220f, z1 = 820f;
            // ground and water bands
            FarStrip(m, K.Edge, WallX, 330f, z1, K.Kerb - .02f, Lawn);
            FarStrip(m, WallX, ShoreX, 330f, z1, -.9f, Sand);
            FarStrip(m, ShoreX, 150f, 330f, z1, RSea - .06f, Turquoise);
            FarStrip(m, 150f, 260f, z0, z1, RSea - .06f, Color.Lerp(SeaBlue, haze, .1f));
            FarStrip(m, 260f, 460f, z0, z1, RSea - .06f, Color.Lerp(SeaBlue, haze, .3f));
            FarStrip(m, 460f, 1400f, z0, z1, RSea - .06f, Color.Lerp(SeaBlue, haze, .5f));
            FarStrip(m, -K.Edge - .5f, -60f, 330f, z1, K.Kerb - .05f, Lawn);
            FarStrip(m, -60f, -170f, z0, z1, -.25f, Haze(K.Hex("86A35C"), 110f, haze));
            FarStrip(m, -170f, -420f, z0, z1, -.3f, Haze(K.Hex("8FA567"), 280f, haze));
            FarStrip(m, -420f, -1400f, z0, z1, -.35f, Haze(K.Hex("9AAE7A"), 600f, haze));
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 160; i++)
            {
                float x = F(r, 40f, 520f), z = F(r, -150f, 760f), len = F(r, 2f, 7f);
                float y = RSea - .04f;
                K.Face(m, V(x, y, z), V(x, y, z + .5f), V(x + len, y, z + .5f), V(x + len, y, z), Haze(i % 4 == 0 ? Foam : SeaRipple, x, haze), Vector3.up, false);
            }
            // Garden Island: long and low across the water on the right, pale limestone shore, olive scrub
            Ridge(m, 1, 240f, 90f, z0, z1, RSea, z => 7f + 3.5f * Mathf.Sin(z * .011f + .4f) + 2f * Mathf.Sin(z * .037f + 1.7f), K.Hex("E9E0C6"), K.Hex("6F8B5E"), K.Hex("819A6A"), haze, .45f);
            for (float z = z0 + 10f; z < z1; z += F(r, 18f, 30f))
            {
                float h = 7f + 3.5f * Mathf.Sin(z * .011f + .4f) + 2f * Mathf.Sin(z * .037f + 1.7f);
                m.Surface = ComicSurface.Foliage;
                m.Cylinder(V(F(r, 262f, 290f), RSea + h + 1f, z), F(r, 7f, 12f), F(r, 2.5f, 4f), Haze(K.Hex("5E7A50"), 270f, haze), 6, .55f);
            }
            // Penguin Island: limestone cliffs, flat scrub top, a beach ring
            Vector3 pi = V(118f, RSea, 175f);
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(pi + V(0, .3f, 0), 19f, .6f, Haze(Sand, 200f, haze), 9, .95f);
            m.Cylinder(pi + V(0, 5.5f, 0), 15f, 10f, Haze(K.Hex("E3D3AE"), 200f, haze), 7, .82f);
            m.Surface = ComicSurface.Foliage;
            m.Cylinder(pi + V(1f, 11f, -1f), 12.6f, 1.6f, Haze(K.Hex("7D9466"), 200f, haze), 7, .7f);
            for (int i = 0; i < 6; i++) m.Cylinder(pi + V(F(r, -8f, 8f), 12.2f, F(r, -8f, 8f)), F(r, 2.5f, 4.5f), 1.8f, Haze(K.Hex("67804F"), 200f, haze), 6, .5f);
            // Point Peron: a low limestone headland reaching across ahead-right
            for (float x = 40f; x < 210f; x += 13f)
            {
                float h = 6f + 6f * Mathf.Sin((x - 40f) / 170f * Mathf.PI) + F(r, -1f, 1f), zc = 300f + (x - 40f) * .25f;
                m.Surface = ComicSurface.Concrete;
                m.Cylinder(V(x, RSea + h * .45f, zc), F(r, 11f, 14f), h * .9f, Haze(K.Hex("DDCCA4"), 320f, haze), 7, .8f);
                m.Surface = ComicSurface.Foliage;
                m.Cylinder(V(x + 1f, RSea + h * .9f + .6f, zc - 1f), F(r, 9f, 12f), 1.6f, Haze(K.Hex("7A9163"), 320f, haze), 7, .6f);
            }
            // sailboats and dolphins on the Sound
            Color[] bands = { K.R.red, Navy, SailTeal, SailYellow, White };
            for (int i = 0; i < 12; i++)
            {
                float x = F(r, 45f, 230f), z = F(r, -60f, 420f);
                var boat = new ComicMesh();
                Sailboat(boat, Vector3.zero, 1f, bands[i % bands.Length]);
                m.Add(boat, V(x, RSea - .1f, z), Quaternion.Euler(0, F(r, -40f, 40f) + (i % 2) * 180f, 0), Vector3.one * F(r, 1.3f, 1.9f));
            }
            foreach (Vector3 pod in new[] { V(46f, 0, 40f), V(58f, 0, 130f), V(50f, 0, 230f) })
                for (int k = 0; k < 3; k++) Dolphin(m, V(pod.x + k * 2.2f, RSea - .1f, pod.z + k * 1.6f), F(r, -20f, 20f), F(r, 1.1f, 1.5f));
            // land side: tiled roofs and Norfolk pines, a tuart tree line, the hazy scarp
            Color[] walls = { Cream, White, PaleYellow, K.Hex("D9CBB0") };
            Color[] roofs = { Terracotta, CafeRoof, Colorbond, K.Hex("8A8F94") };
            for (float z = z0; z < z1; z += F(r, 11f, 16f))
                for (int row = 0; row < 3; row++)
                {
                    float x = -(66f + row * 26f + F(r, 0f, 12f)), dist = Mathf.Sqrt(x * x + z * z);
                    float w = F(r, 9f, 13f), h = F(r, 3.2f, 6.5f);
                    m.Surface = ComicSurface.Plaster;
                    m.Box(V(x, h * .5f - 1f, z), V(w, h + 2f, w * .8f), Haze(walls[r.Next(walls.Length)], dist, haze));
                    m.Surface = ComicSurface.Brick;
                    m.Cylinder(V(x, h + .8f, z), w * .74f, 2.2f, Haze(roofs[r.Next(roofs.Length)], dist, haze), 4, .15f, Quaternion.Euler(0, 45, 0));
                }
            for (float z = z0; z < z1; z += F(r, 20f, 34f))
            {
                float x = -F(r, 64f, 150f);
                CheapPine(m, V(x, 0f, z), F(r, 16f, 24f), Haze(PineDark, Mathf.Abs(x) + Mathf.Abs(z) * .3f, haze));
            }
            for (float z = z0; z < z1; z += F(r, 14f, 22f))
            {
                float x = -F(r, 170f, 230f);
                m.Surface = ComicSurface.Foliage;
                m.Ellipsoid(V(x, 5f, z), V(F(r, 16f, 24f), F(r, 9f, 13f), F(r, 14f, 20f)), Haze(Color.Lerp(K.Hex("5E7E46"), K.Hex("7A9A58"), F(r, 0f, 1f)), 260f, haze));
            }
            Ridge(m, -1, 520f, 160f, z0, z1, -.4f, z => 34f + 8f * Mathf.Sin(z * .008f + 2f) + 4f * Mathf.Sin(z * .023f), K.Hex("8FA27E"), K.Hex("7F95A6"), K.Hex("93A7B8"), haze, .55f);
            m.Shear(grade);
            return m;
        }

        // A flat strip split every 20 m along z (the GPU curl bends per vertex).
        static void FarStrip(ComicMesh m, float x0, float x1, float z0, float z1, float y, Color c)
        {
            m.Surface = ComicSurface.Plain;
            for (float z = z0; z < z1 - .01f; z += 20f)
            {
                float e = Mathf.Min(z1, z + 20f);
                K.Face(m, V(x0, y, z), V(x0, y, e), V(x1, y, e), V(x1, y, z), c, Vector3.up, false);
            }
        }

        // Aerial perspective: further = closer to the haze colour.
        static Color Haze(Color c, float dist, Color haze) => Color.Lerp(c, haze, Mathf.Clamp01((Mathf.Abs(dist) - 60f) / 600f) * .7f);

        /// <summary>A long landmass parallel to the road on side s (near edge at |x| = xNear): a pale shore strip, a slope up
        /// to a wavy ridge facing the road, the top falling away behind; split every 15 m so the curl bends it.</summary>
        static void Ridge(ComicMesh m, int s, float xNear, float depth, float z0, float z1, float baseY, Func<float, float> height, Color shore, Color slope, Color top, Color haze, float hazeAmount)
        {
            Color sh = Color.Lerp(shore, haze, hazeAmount), sl = Color.Lerp(slope, haze, hazeAmount), tp = Color.Lerp(top, haze, hazeAmount);
            Vector3 toRoad = new Vector3(-s, 0, 0);
            for (float z = z0; z < z1 - .01f; z += 15f)
            {
                float za = z, zb = Mathf.Min(z1, z + 15f), ha = height(za), hb = height(zb);
                float xs = s * xNear, xm = s * (xNear + 5f), xr = s * (xNear + depth * .35f), xf = s * (xNear + depth);
                m.Surface = ComicSurface.Concrete;
                K.Face(m, V(xs, baseY, za), V(xs, baseY, zb), V(xm, baseY + 1.2f, zb), V(xm, baseY + 1.2f, za), sh, (toRoad + Vector3.up * 3f).normalized, false);
                m.Surface = ComicSurface.Foliage;
                K.Face(m, V(xm, baseY + 1.2f, za), V(xm, baseY + 1.2f, zb), V(xr, baseY + hb, zb), V(xr, baseY + ha, za), sl, (toRoad + Vector3.up).normalized, false);
                K.Face(m, V(xr, baseY + ha, za), V(xr, baseY + hb, zb), V(xf, baseY + hb * .5f, zb), V(xf, baseY + ha * .5f, za), tp, Vector3.up, false);
            }
        }

        // Height field in flat road space (x0..x1, z0..z1), cells of about dx by dz; y = h(x, z), colour col(x, z, y).
        static void Terrain(ComicMesh m, float x0, float x1, float z0, float z1, float dx, float dz, Func<float, float, float> h, Func<float, float, float, Color> col, ComicSurface surface)
        {
            m.Surface = surface;
            int nx = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(x1 - x0) / dx)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / dz));
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float xa = Mathf.Lerp(x0, x1, (float)i / nx), xb = Mathf.Lerp(x0, x1, (float)(i + 1) / nx);
                    float za = Mathf.Lerp(z0, z1, (float)j / nz), zb = Mathf.Lerp(z0, z1, (float)(j + 1) / nz);
                    Vector3 a = V(xa, h(xa, za), za), b = V(xa, h(xa, zb), zb), c = V(xb, h(xb, zb), zb), d = V(xb, h(xb, za), za);
                    float xc = (xa + xb) * .5f, zc = (za + zb) * .5f;
                    K.Face(m, a, b, c, d, col(xc, zc, (a.y + b.y + c.y + d.y) * .25f), Vector3.up, false);
                }
        }

        // ================================================================================== WARNBRO

        static void BuildWarnbro(TrackTheme theme, bool atlas)
        {
            K.Begin(WarnMeshes, WarnPrefabs, CbdBuilder.DataFolder);
            Obstacle rack = K.SaveObstacle(WarnPf("OBS_Warnbro_SurfRack"), "OBS_Warnbro_SurfRack", SurfRackMesh(), new Vector3(2f, 1.9f, .7f), ObstacleKind.Thing, false, false, .8f);
            Obstacle bin = K.SaveObstacle(WarnPf("OBS_Warnbro_Bin"), "OBS_Warnbro_Bin", WheelieBinMesh(), new Vector3(.7f, 1.1f, .75f), ObstacleKind.Thing, true, true, 1f);
            Obstacle fence = K.SaveObstacle(WarnPf("OBS_Warnbro_DuneFence"), "OBS_Warnbro_DuneFence", DuneFenceMesh(), new Vector3(2.4f, 1f, .3f), ObstacleKind.Thing, true, false, .9f);
            Obstacle flag = K.SaveObstacle(WarnPf("OBS_Warnbro_PatrolFlag"), "OBS_Warnbro_PatrolFlag", PatrolFlagMesh(), new Vector3(.5f, 2.6f, .5f), ObstacleKind.Thing, false, true, .8f);
            GameObject gull = SaveAnimal(WarnPf("ANM_Warnbro_Seagull"), "ANM_Warnbro_Seagull", SeagullMesh(), new Vector3(0, .3f, 0), new Vector3(.5f, .6f, .7f), 0f, true);
            GameObject gullWalking = SaveAnimal(WarnPf("ANM_Warnbro_SeagullWalking"), "ANM_Warnbro_SeagullWalking", SeagullMesh(), new Vector3(0, .3f, 0), new Vector3(.5f, .6f, .7f), .6f, true);
            GameObject bobtail = SaveAnimal(WarnPf("ANM_Warnbro_Bobtail"), "ANM_Warnbro_Bobtail", BobtailMesh(), new Vector3(0, .12f, 0), new Vector3(.4f, .25f, .9f), .15f, true);
            Ramp kicker = K.SaveRamp(WarnPf("RMP_Warnbro_DuneKicker"), "RMP_Warnbro_DuneKicker", 3.2f, 5.5f, 1.3f, 0f, 1f, .5f, DuneSand, K.Hex("C9B48A"), false);

            K.WriteChunk(WarnEntry(), WarnChunkPath("Entry"));
            K.WriteChunk(WarnStation(), WarnChunkPath("Station"));
            K.WriteChunk(WarnBeachRoad(), WarnChunkPath("BeachRoad"));
            K.WriteChunk(WarnSurfClub(), WarnChunkPath("SurfClub"));
            K.WriteChunk(WarnDunePath(), WarnChunkPath("DunePath"));
            K.WriteChunk(WarnCreek(), WarnChunkPath("Creek"));
            K.WriteChunk(WarnExit(), WarnChunkPath("Exit"));
            GameObject far = K.SaveFarScenery(WarnPf("FAR_Warnbro_Sound"), "FAR_Warnbro_Sound", WarnFar(-WarnDrop / 40f));

            bool created = AssetDatabase.LoadAssetAtPath<District>(WarnDistrictPath) == null;
            District d = K.LoadOrCreate<District>(WarnDistrictPath);
            if (created) { d.skyYaw = 180f; d.skySize = .6f; d.skyHeight = -12f; d.skyline = 0f; }
            d.id = WarnId; d.displayName = "WARNBRO"; d.order = 150; d.length = new Vector2(320f, 400f);
            d.chunks = WarnChunkIds.Select(c => AssetDatabase.LoadAssetAtPath<GameObject>(WarnChunkPath(c)).GetComponent<RoadChunk>()).ToArray();
            d.orderedChunks = true;
            d.entry = AssetDatabase.LoadAssetAtPath<GameObject>(WarnChunkPath("Entry")).GetComponent<RoadChunk>();
            d.exit = AssetDatabase.LoadAssetAtPath<GameObject>(WarnChunkPath("Exit")).GetComponent<RoadChunk>();
            d.allowBoulevard = false;
            d.obstacles = new[] { rack, bin, fence, flag };
            d.jumpObstacles = new[] { fence, bin };
            d.gifts = Array.Empty<Gift>();
            d.hazards = ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_SidewalkStroll", "HZ_PoliceStop");
            var people = new List<GameObject>(K.People(theme, "Surfer", "Surfer", "Surfer", "Jogger", "Jogger", "DogWalker", "DogWalker", "Kid", "Elder", "Skater"));
            people.Add(gull); people.Add(gullWalking); people.Add(gullWalking); people.Add(bobtail);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { kicker };
            d.rampFrequency = 1.2f;
            d.blendMeters = 150f;
            d.fogColor = new Color(.78f, .88f, .94f);
            d.skyTint = new Color(1f, 1f, .98f);
            d.ambientSky = new Color(.72f, .86f, .98f);
            d.ambientEquator = new Color(.92f, .90f, .84f);
            d.ambientGround = new Color(.84f, .78f, .64f);
            d.sunColor = new Color(1f, .96f, .88f);
            d.sunIntensity = 1.5f;
            d.farScenery = far;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/Warnbro] " + string.Join("; ", K.Report));
            if (atlas) RenderWarnAtlas(rack, bin, fence, flag, gull, bobtail, kicker);
        }

        static void RenderWarnAtlas(Obstacle rack, Obstacle bin, Obstacle fence, Obstacle flag, GameObject gull, GameObject bobtail, Ramp kicker)
        {
            ComicMesh Make(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("SurfClub", null, Make(m => SurfClub(m, Vector3.zero))),
                ("PatrolTower", null, Make(m => PatrolTower(m, Vector3.zero))),
                ("House", null, Make(m => House(m, new CqLot(-1, -6f, 6f, 6f, 11f, 0f), Render, Terracotta, false))),
                ("HouseColorbond", null, Make(m => House(m, new CqLot(-1, -6f, 6f, 6f, 11f, 0f), Surfmist, Colorbond, true))),
                ("Peppermint", null, Make(m => PeppermintTree(m, Vector3.zero, 6f, 2))),
                ("HeathShrubs", null, Make(m => { for (int i = 0; i < 6; i++) HeathBush(m, V((i % 3) * 1.3f, 0, (i / 3) * 1.3f), .9f + (i % 2) * .4f, i); })),
                ("LimestoneOutcrop", null, Make(m => Outcrop(m, Vector3.zero, 1.4f, 4))),
                ("BeachSign", null, Make(m => BeachSign(m, Vector3.zero, "WARNBRO BEACH"))),
                ("StationCanopy", null, Make(m => StationCanopy(m, V(0, 0, -6f), 12f))),
                ("Train", null, Make(m => Train(m, Vector3.zero, 1))),
                ("ParkedCar", null, Make(m => ParkedCar(m, Vector3.zero, 0f, K.Hex("C9CED3")))),
                ("AccessFence", null, Make(m => AccessFence(m, V(0, 0, 0), V(0, 0, 6f), (x, z) => 0f))),
                ("SurfRack", rack.gameObject, null),
                ("Bin", bin.gameObject, null),
                ("DuneFence", fence.gameObject, null),
                ("PatrolFlag", flag.gameObject, null),
                ("Seagull", gull, null),
                ("Bobtail", bobtail, null),
                ("DuneKicker", kicker.gameObject, null),
            };
            K.RenderAtlas("art-src/districts/warnbro/props-atlas.png", items);
        }

        // ---------------------------------------------------------------- Warnbro chunks

        static void WarnStreet(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f) =>
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, K.Hex("D6D1C6"), KerbCap, true, skipFrom, skipTo);

        // The coastal shared path (PedestrianMall): pale concrete with a dashed white centre line, compacted limestone shoulders.
        static void CoastPath(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f)
        {
            d.Type = RoadType.PedestrianMall;
            K.StreetSurface(d.Road, d.Length, PathConcrete, Shoulder, KerbCap, false, skipFrom, skipTo);
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            for (float z = 1f; z < d.Length - 2f; z += 5f)
            {
                if (skipFrom >= 0f && z + 2.6f > skipFrom && z < skipTo) continue;
                K.Face(m, V(-.07f, .03f, z), V(-.07f, .03f, z + 2.6f), V(.07f, .03f, z + 2.6f), V(.07f, .03f, z), White, Vector3.up, false);
            }
            for (float z = 0f; z < d.Length - .01f; z += 4f)
            {
                if (skipFrom >= 0f && z + 4f > skipFrom && z < skipTo) continue;
                foreach (int s in new[] { -1, 1 })
                    K.Face(m, V(s * (K.Half - .35f), .025f, z), V(s * (K.Half - .35f), .025f, z + 4f), V(s * (K.Half - .25f), .025f, z + 4f), V(s * (K.Half - .25f), .025f, z), K.Hex("B9B2A2"), Vector3.up, false);
            }
        }

        // Foredune profile on the sea side, u metres beyond the sidewalk edge: up to a heath-covered crest, down to the beach.
        static float SeaDune(float u, float z, float access, float crestScale = 1f)
        {
            float crest = (2.3f + .6f * Mathf.Sin(z * .21f + 1.3f) + .35f * Mathf.Sin(z * .53f)) * crestScale;
            float up = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.4f, 7f, u));
            float down = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(10f, 18f, u));
            float h = Mathf.Lerp(Mathf.Lerp(K.Kerb, crest, up), -.6f, down);
            float flat = Mathf.Lerp(K.Kerb, -.6f, Mathf.InverseLerp(1f, 18f, u));
            return Mathf.Lerp(h, flat, access);
        }

        // Inland dunes on the land side of the coastal path: rising to a rolling heath plateau about 3 m up.
        static float LandDune(float u, float z) =>
            Mathf.Lerp(K.Kerb, 2.6f + .8f * Mathf.Sin(z * .17f + .5f) + .5f * Mathf.Sin(u * .4f + z * .07f), Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.4f, 8f, u)));

        static Color DuneColour(float y, float x, float z)
        {
            float n = Mathf.Sin(x * 1.7f + z * .9f) * .5f + .5f;
            if (y > 1f) return Color.Lerp(Heath, HeathLight, n * .6f);
            if (y > .45f) return Color.Lerp(DuneSand, Heath, .55f + n * .2f);
            return Color.Lerp(DuneSand, BeachWhite, n * .5f);
        }

        // The sea side: foredune with heath, the white beach, turquoise water with surf lines. accessZ: beach access paths.
        static void SeaSide(CqChunkDraft d, float z0, float z1, int seed, float crestScale = 1f, params float[] accessZ)
        {
            float Access(float z) { float a = 0f; foreach (float c in accessZ) a = Mathf.Max(a, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.6f, 3.6f, Mathf.Abs(z - c)))); return a; }
            Terrain(d.Road, K.Edge, K.Edge + 18f, z0, z1, 1.5f, 4f, (x, z) => SeaDune(x - K.Edge, z, Access(z), crestScale), (x, z, y) => DuneColour(y, x, z), ComicSurface.Foliage);
            float bx0 = K.Edge + 18f, bx1 = K.Edge + 38f, shore = K.Edge + 33f;
            Terrain(d.Road, bx0, bx1, z0, z1, 2.5f, 4f, (x, z) => Mathf.Lerp(-.6f, WSea - .45f, Mathf.InverseLerp(bx0, bx1, x)) + .05f * Mathf.Sin(z * .8f + x),
                (x, z, y) => x > shore - 2.5f ? SandWet : BeachWhite, ComicSurface.Paving);
            K.Water(d.Road, shore, shore + 26f, z0, z1, WSea, Turquoise, TurquoiseLight, seed);
            K.Water(d.Road, shore + 26f, 170f, z0, z1, WSea, SeaBlue, SeaRipple, seed + 1);
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            var r = new System.Random(seed);
            foreach (float xs in new[] { shore + .3f, shore + 7f, shore + 15f })
                for (float z = z0; z < z1 - 1f; z += F(r, 2.5f, 5f))
                {
                    float e = Mathf.Min(z1, z + F(r, 2f, 6f)), w = xs < shore + 1f ? .8f : .45f;
                    K.Face(m, V(xs, WSea + .02f, z), V(xs, WSea + .02f, e), V(xs + w, WSea + .02f, e), V(xs + w, WSea + .02f, z), Foam, Vector3.up, false);
                }
            // heath shrubs on the dune (sheared with the road like the terrain)
            for (float z = z0 + 1f; z < z1 - 1f; z += F(r, 3.2f, 4.6f))
            {
                if (Access(z) > .05f) continue;
                for (int k = 0; k < 2; k++)
                {
                    float u = F(r, 3f, 12f), y = SeaDune(u, z, 0f, crestScale);
                    HeathBush(d.SlopedFrontage, V(K.Edge + u, y - .1f, z + F(r, -.6f, .6f)), F(r, .8f, 1.6f), seed + (int)(z * 7) + k);
                }
            }
            foreach (float c in accessZ)
            {
                if (c < z0 || c > z1) continue;
                AccessFence(d.SlopedFrontage, V(K.Edge + .6f, 0, c - 1.7f), V(K.Edge + 16f, 0, c - 1.7f), (x, z) => SeaDune(x - K.Edge, z, Access(z), crestScale));
                AccessFence(d.SlopedFrontage, V(K.Edge + .6f, 0, c + 1.7f), V(K.Edge + 16f, 0, c + 1.7f), (x, z) => SeaDune(x - K.Edge, z, Access(z), crestScale));
                BeachSign(d.Props, V(K.Edge + 2.4f, d.Ground(c - 2.6f) + .1f, c - 2.6f), "WARNBRO BEACH");
            }
        }

        // The land side of the coastal path: rolling dunes with heath, limestone outcrops and a few peppermints.
        static void LandDunes(CqChunkDraft d, float z0, float z1, int seed, bool roofs)
        {
            Terrain(d.Road, -K.Edge, -(K.Edge + 30f), z0, z1, 2f, 4f, (x, z) => LandDune(-x - K.Edge, z), (x, z, y) => DuneColour(y, x, z), ComicSurface.Foliage);
            var r = new System.Random(seed);
            for (float z = z0 + 1f; z < z1 - 1f; z += F(r, 3.4f, 4.8f))
                for (int k = 0; k < 2; k++)
                {
                    float u = F(r, 1.5f, 16f);
                    HeathBush(d.SlopedFrontage, V(-(K.Edge + u), LandDune(u, z) - .1f, z + F(r, -.6f, .6f)), F(r, .8f, 1.7f), seed + (int)(z * 5) + k);
                }
            for (float z = z0 + 5f; z < z1 - 3f; z += F(r, 11f, 16f))
            {
                float u = F(r, 3f, 9f);
                Outcrop(d.SlopedFrontage, V(-(K.Edge + u), LandDune(u, z) - .3f, z), F(r, 1.1f, 1.8f), seed + (int)z);
            }
            for (float z = z0 + 8f; z < z1 - 3f; z += 18f)
            {
                float u = F(r, 10f, 16f);
                PeppermintTree(d.Props, V(-(K.Edge + u), d.Y(z) + LandDune(u, z) - .2f, z), F(r, 5.5f, 7f), seed + (int)z);
            }
            if (!roofs) return;
            for (float z = z0 + 3f; z < z1 - 4f; z += 12f)
            {
                float x = -(K.Edge + F(r, 30f, 38f)), y = d.Y(z) + 2.2f;
                d.Deep.Surface = ComicSurface.Plaster;
                d.Deep.Box(V(x, y + 1.5f, z), V(10f, 4f, 9f), Render);
                d.Deep.Surface = ComicSurface.Brick;
                d.Deep.Cylinder(V(x, y + 4.4f, z), 7.4f, 2.2f, r.NextDouble() < .6 ? Terracotta : Colorbond, 4, .15f, Quaternion.Euler(0, 45, 0));
            }
        }

        // The land side of Warnbro Beach Rd: limestone retaining wall, gardens and tile-roofed houses, peppermint trees.
        static void HouseSide(CqChunkDraft d, float z0, float z1, int seed)
        {
            d.Road.Surface = ComicSurface.Foliage;
            K.SideBox(d.Road, -1, z0, z1, -2.4f, K.Kerb + .9f, .6f, 60f, K.Hex("8DAA5A"));
            ComicMesh w = d.SlopedFrontage;
            w.Surface = ComicSurface.Concrete;
            K.SideBox(w, -1, z0, z1, -1f, K.Kerb + 1f, .05f, .6f, Limestone);
            K.SideBox(w, -1, z0, z1, K.Kerb + 1f, K.Kerb + 1.12f, 0f, .66f, LimestoneCap);
            w.Surface = ComicSurface.Plain;
            for (float z = z0 + .5f; z < z1; z += 1.2f)
                K.Face(w, V(-(K.Edge + .045f), -.6f, z), V(-(K.Edge + .045f), -.6f, z + .05f), V(-(K.Edge + .045f), K.Kerb + .95f, z + .05f), V(-(K.Edge + .045f), K.Kerb + .95f, z), LimestoneDark, Vector3.right, false);
            K.Face(w, V(-(K.Edge + .046f), K.Kerb + .45f, z0), V(-(K.Edge + .046f), K.Kerb + .45f, z1), V(-(K.Edge + .046f), K.Kerb + .5f, z1), V(-(K.Edge + .046f), K.Kerb + .5f, z0), LimestoneDark, Vector3.right, false);
            var r = new System.Random(seed);
            Color[] walls = { Render, Surfmist, K.Hex("D8CDB8"), White };
            for (float z = z0 + .5f; z1 - z >= 10f; z += 13f)
            {
                bool cb = r.NextDouble() < .4;
                var lot = new CqLot(-1, z, z + 11f, K.Edge + 7f, 11f, d.Base(z + 5.5f) + .9f);
                House(d.Frontage, lot, walls[r.Next(walls.Length)], cb ? Colorbond : Terracotta, cb);
                d.Wall(lot, 7f);
            }
            for (float z = z0 + 6f; z < z1; z += 13f) PeppermintTree(d.Props, V(-(K.Edge + 3.4f), d.Ground(z) + .9f, z), F(r, 5f, 6.5f), seed + (int)z);
            for (float z = z0 + 4f; z < z1; z += 12f) CheapPine(d.Deep, V(-(K.Edge + F(r, 24f, 32f)), d.Ground(z) + 1f, z), F(r, 13f, 18f));
        }

        // Calm end for Warnbro entry / exit: low limestone wall, heath and peppermints on both sides.
        static void WarnCalm(CqChunkDraft d, int s, float z0, float z1, int seed)
        {
            d.Road.Surface = ComicSurface.Foliage;
            K.SideBox(d.Road, s, z0, z1, -2.4f, K.Kerb + .03f, 0f, 60f, K.Hex("8DAA5A"));
            LowWall(d, s, z0, z1, .5f);
            var r = new System.Random(seed);
            for (float z = z0 + 1f; z < z1 - 1f; z += 3.6f) HeathBush(d.SlopedFrontage, V(s * (K.Edge + F(r, 1.4f, 4f)), K.Kerb, z), F(r, .9f, 1.4f), seed + (int)(z * 3));
            for (float z = z0 + 4f; z < z1 - 2f; z += 8f) PeppermintTree(d.Props, V(s * (K.Edge + F(r, 5f, 8f)), d.Ground(z), z), F(r, 5f, 6.5f), seed + (int)z);
            d.Walls.Add(new Bounds(new Vector3(s * (K.Edge + 7f), d.Y((z0 + z1) * .5f) + 1f, (z0 + z1) * .5f), new Vector3(13.5f, 4f, z1 - z0)));
        }

        static CqChunkDraft WarnEntry()
        {
            var d = new CqChunkDraft("Warnbro_Entry", WarnDrop);
            WarnStreet(d);
            WarnCalm(d, -1, 0f, 14f, 1);
            WarnCalm(d, 1, 0f, 14f, 2);
            SeaSide(d, 14f, 40f, 201, .8f);
            DuneEnd(d, 14f);
            HouseSide(d, 14f, 40f, 3);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft WarnExit()
        {
            var d = new CqChunkDraft("Warnbro_Exit", WarnDrop);
            WarnStreet(d);
            SeaSide(d, 0f, 26f, 211, .8f);
            DuneEnd(d, 26f);
            HouseSide(d, 0f, 26f, 5);
            WarnCalm(d, -1, 26f, 40f, 6);
            WarnCalm(d, 1, 26f, 40f, 7);
            d.StandardSlots();
            return d;
        }

        // A limestone rock wall across the beach where the dunes start or end (hides the cut end of the sand and water).
        static void DuneEnd(CqChunkDraft d, float z)
        {
            ComicMesh m = d.SlopedFrontage;
            var r = new System.Random((int)z * 31 + 1);
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(K.Edge + .2f, WSea - 1f, z - 1f), V(80f, -.4f, z + 1f), LimestoneDark);
            for (float x = K.Edge + .8f; x < 80f; x += F(r, 1.8f, 2.6f))
            {
                float y = x < K.Edge + 18f ? Mathf.Lerp(K.Kerb, .2f, (x - K.Edge) / 18f) : Mathf.Lerp(-.4f, WSea + .2f, Mathf.InverseLerp(K.Edge + 18f, 70f, x));
                m.Box(V(x, y + .3f, z + F(r, -.5f, .5f)), V(F(r, 1.6f, 2.4f), F(r, .9f, 1.5f), F(r, 1.6f, 2.4f)), Color.Lerp(Limestone, LimestoneDark, F(r, .1f, .9f)), Quaternion.Euler(F(r, -8f, 8f), F(r, 0f, 90f), F(r, -8f, 8f)));
            }
        }

        static CqChunkDraft WarnStation()
        {
            // Warnbro Beach Rd past the station: the big car park and the platform canopy with a train on the left.
            var d = new CqChunkDraft("Warnbro_Station", WarnDrop);
            WarnStreet(d);
            SeaSide(d, 0f, 40f, 221, 1f, 30f);
            ComicMesh g = d.Road;
            g.Surface = ComicSurface.Asphalt;
            K.SideBox(g, -1, 0f, 40f, -2.4f, K.Kerb - .02f, 0f, 60f, K.R.asphalt);
            g.Surface = ComicSurface.Plain;
            foreach (float row in new[] { 4.5f, 11f, 22.5f, 29f })
                for (float z = 1f; z < 40f; z += 2.7f)
                    K.Face(g, V(-(K.Edge + row - 2.4f), K.Kerb + .005f, z), V(-(K.Edge + row - 2.4f), K.Kerb + .005f, z + .1f), V(-(K.Edge + row + 2.4f), K.Kerb + .005f, z + .1f), V(-(K.Edge + row + 2.4f), K.Kerb + .005f, z), White, Vector3.up, false);
            LowWall(d, -1, 0f, 40f, .35f);
            var r = new System.Random(22);
            Color[] cars = { K.Hex("F1F1EE"), K.Hex("C9CED3"), K.Hex("C8372D"), K.Hex("2F5D8A"), K.Hex("2A2E33"), K.Hex("E9E3D3"), K.Hex("6E7E58") };
            foreach (float row in new[] { 4.5f, 11f, 22.5f, 29f })
                for (float z = 2.35f; z < 39f; z += 2.7f)
                    if (r.NextDouble() < (row < 8f ? .55 : .75))
                        ParkedCar(row < 15f ? d.Frontage : d.Deep, V(-(K.Edge + row + (row == 4.5f || row == 22.5f ? .3f : -.3f)), d.Ground(z), z), row == 4.5f || row == 22.5f ? 90f : -90f, cars[r.Next(cars.Length)]);
            for (float z = 8f; z < 40f; z += 16f) CarParkLight(d.Props, V(-(K.Edge + 16.7f), d.Ground(z), z));
            StationCanopy(d.Landmark, V(-(K.Edge + 38f), d.Ground(6f), 6f), 32f);
            Train(d.Deep, V(-(K.Edge + 44.5f), d.Ground(4f) - .4f, 4f), 3);
            StationSign(d.Props, V(-(K.Edge + 1.2f), d.Ground(5f), 5f));
            d.Walls.Add(new Bounds(new Vector3(-(K.Edge + 20f), d.Y(20f) + 1f, 20f), new Vector3(38f, 4f, 40f)));
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft WarnBeachRoad()
        {
            var d = new CqChunkDraft("Warnbro_BeachRoad", WarnDrop);
            WarnStreet(d);
            SeaSide(d, 0f, 40f, 231, 1f, 12f);
            HouseSide(d, 0f, 40f, 9);
            for (float z = 18f; z < 40f; z += 20f) MountsBayStyleLamp(d.Props, V(K.Edge + .5f, d.Ground(z), z));
            Bench(d.Props, V(K.Edge + 1.6f, d.Ground(24f) + .2f, 24f), -90f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft WarnSurfClub()
        {
            var d = new CqChunkDraft("Warnbro_SurfClub", WarnDrop);
            WarnStreet(d);
            SeaSide(d, 0f, 40f, 241, .7f, 4f);
            // the club on a raised pad on the dune, the patrol tower and flags on the beach
            ComicMesh w = d.SlopedFrontage;
            w.Surface = ComicSurface.Concrete;
            K.MinMaxBox(w, V(K.Edge + 3f, -1.2f, 9f), V(K.Edge + 21f, 1.2f, 33f), Limestone);
            K.MinMaxBox(w, V(K.Edge + 2.95f, 1.2f, 9f), V(K.Edge + 21.05f, 1.32f, 33f), LimestoneCap);
            SurfClub(d.Landmark, V(K.Edge + 12f, d.Y(21f) + 1.32f, 21f));
            PatrolTower(d.Landmark, V(K.Edge + 25f, d.Y(18f) - 1f, 18f));
            foreach (float z in new[] { 6f, 34f }) PatrolFlagArt(d.Landmark, V(K.Edge + 29f, d.Y(z) - 1.25f, z));
            for (int i = 0; i < 3; i++) RescueBoard(d.Landmark, V(K.Edge + 23f + i * .5f, d.Y(26f + i * 1.2f) - .9f, 26f + i * 1.2f), i);
            // land side: the club's car park and a peppermint row
            ComicMesh g = d.Road;
            g.Surface = ComicSurface.Asphalt;
            K.SideBox(g, -1, 0f, 40f, -2.4f, K.Kerb - .02f, 0f, 22f, K.R.asphalt);
            g.Surface = ComicSurface.Foliage;
            K.SideBox(g, -1, 0f, 40f, -2.4f, K.Kerb + .4f, 22f, 60f, K.Hex("8DAA5A"));
            g.Surface = ComicSurface.Plain;
            for (float z = 1f; z < 40f; z += 2.7f)
                K.Face(g, V(-(K.Edge + 3f), K.Kerb + .005f, z), V(-(K.Edge + 3f), K.Kerb + .005f, z + .1f), V(-(K.Edge + 8f), K.Kerb + .005f, z + .1f), V(-(K.Edge + 8f), K.Kerb + .005f, z), White, Vector3.up, false);
            LowWall(d, -1, 0f, 40f, .35f);
            var r = new System.Random(31);
            Color[] cars = { K.Hex("F1F1EE"), K.Hex("C9CED3"), K.Hex("2F5D8A"), K.Hex("C8372D"), K.Hex("E9E3D3") };
            for (float z = 2.35f; z < 39f; z += 2.7f)
                if (r.NextDouble() < .6) ParkedCar(d.Frontage, V(-(K.Edge + 5.5f), d.Ground(z), z), 90f, cars[r.Next(cars.Length)]);
            for (float z = 5f; z < 40f; z += 10f) PeppermintTree(d.Props, V(-(K.Edge + 24f), d.Ground(z) + .2f, z), F(r, 5.5f, 7f), 300 + (int)z);
            for (float z = 4f; z < 40f; z += 13f) CheapPine(d.Deep, V(-(K.Edge + F(r, 30f, 36f)), d.Ground(z) + .3f, z), F(r, 14f, 19f));
            d.Walls.Add(new Bounds(new Vector3(-(K.Edge + 11f), d.Y(20f) + 1f, 20f), new Vector3(22f, 4f, 40f)));
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft WarnDunePath()
        {
            var d = new CqChunkDraft("Warnbro_DunePath", WarnDrop);
            CoastPath(d);
            SeaSide(d, 0f, 40f, 251, 1.1f, 24f);
            LandDunes(d, 0f, 40f, 252, true);
            PathPosts(d, 0f, 40f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft WarnCreek()
        {
            // The coastal path crosses a beach creek: a sand dune kicker across the whole path, 6 m of creek water (SPLASH!),
            // limestone banks, then the path again. The creek runs from the dunes on the left out across the beach.
            var d = new CqChunkDraft("Warnbro_Creek", WarnDrop);
            float ramp = CreekRampStart, lip = ramp + CreekRise + CreekTop, far = lip + CreekGap;
            CoastPath(d, lip, far);
            float Valley(float z) => 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(CreekGap * .5f + .2f, CreekGap * .5f + 5f, Mathf.Abs(z - (lip + far) * .5f)));
            const float creekY = -1.1f;
            // dunes either side, cut down to the creek
            Terrain(d.Road, K.Edge, K.Edge + 18f, 0f, 40f, 1.5f, 2f, (x, z) => Mathf.Lerp(SeaDune(x - K.Edge, z, 0f), creekY - .3f, Valley(z)), (x, z, y) => Valley(z) > .6f ? SandWet : DuneColour(y, x, z), ComicSurface.Foliage);
            Terrain(d.Road, -K.Edge, -(K.Edge + 30f), 0f, 40f, 2f, 2f, (x, z) => Mathf.Lerp(LandDune(-x - K.Edge, z), creekY - .3f, Valley(z)), (x, z, y) => Valley(z) > .6f ? SandWet : DuneColour(y, x, z), ComicSurface.Foliage);
            float bx0 = K.Edge + 18f, bx1 = K.Edge + 38f, shore = K.Edge + 33f;
            Terrain(d.Road, bx0, bx1, 0f, 40f, 2.5f, 4f, (x, z) => Mathf.Lerp(-.6f, WSea - .45f, Mathf.InverseLerp(bx0, bx1, x)), (x, z, y) => x > shore - 2.5f ? SandWet : BeachWhite, ComicSurface.Paving);
            K.Water(d.Road, shore, shore + 26f, 0f, 40f, WSea, Turquoise, TurquoiseLight, 261);
            K.Water(d.Road, shore + 26f, 170f, 0f, 40f, WSea, SeaBlue, SeaRipple, 262);
            // the creek: a turquoise ribbon from the dunes, under the gap, out over the beach to the sea
            K.Water(d.Road, -(K.Edge + 30f), shore, lip - .2f, far + .2f, creekY, K.Hex("5CC9C2"), TurquoiseLight, 263);
            ComicMesh banks = d.SlopedFrontage;
            var r = new System.Random(264);
            banks.Surface = ComicSurface.Concrete;
            foreach (float z in new[] { lip, far })
            {
                bool before = z == lip;
                K.MinMaxBox(banks, V(-K.Edge - .3f, creekY - .8f, z), V(K.Edge + .3f, -.03f, z + (before ? -.5f : .5f)), Limestone);
                K.MinMaxBox(banks, V(-K.Edge - .3f, -.03f, z), V(-K.Half, K.Kerb + .02f, z + (before ? -.5f : .5f)), LimestoneCap);
                K.MinMaxBox(banks, V(K.Half, -.03f, z), V(K.Edge + .3f, K.Kerb + .02f, z + (before ? -.5f : .5f)), LimestoneCap);
                for (float x = -K.Edge - 18f; x < K.Edge + 20f; x += F(r, 1.6f, 2.6f))
                {
                    if (Mathf.Abs(x) < K.Edge + .8f) continue;
                    banks.Box(V(x, creekY + .1f, z + (before ? -F(r, .2f, 1.2f) : F(r, .2f, 1.2f))), V(F(r, .9f, 1.8f), F(r, .6f, 1.2f), F(r, .9f, 1.6f)), Color.Lerp(Limestone, LimestoneDark, F(r, .1f, .9f)), Quaternion.Euler(F(r, -10f, 10f), F(r, 0f, 90f), F(r, -10f, 10f)));
                }
            }
            var spec = new CqRampSpec { Name = "DuneKicker", X = 0f, Z = ramp, Width = 2f * K.Edge, Rise = CreekRise, Height = CreekHeight, Top = CreekTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            d.Gaps.Add(new CqGapSpec { Name = "CreekWater", Z = lip, Length = CreekGap, Width = 0f, Word = "SPLASH", Water = true });
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, K.Hex("D9BC80"), K.Hex("B89A64"), false, creekY - .3f);
            // timber edging and sand ripples on the dune kicker
            deck.Surface = ComicSurface.Wood;
            foreach (int s in new[] { -1, 1 })
            {
                deck.Beam(V(s * (K.Edge - .1f), .15f, ramp), V(s * (K.Edge - .1f), CreekHeight + .15f, ramp + CreekRise), .2f, Timber);
                deck.Box(V(s * (K.Edge - .1f), CreekHeight + .15f, ramp + CreekRise + CreekTop * .5f), V(.2f, .2f, CreekTop), Timber);
            }
            deck.Surface = ComicSurface.Plain;
            for (float t = .2f; t < 1f; t += .2f)
            {
                float z = ramp + CreekRise * t, y = CreekHeight * t + .015f;
                K.Face(deck, V(-K.Edge + .3f, y, z), V(-K.Edge + .3f, y, z + .16f), V(K.Edge - .3f, y, z + .16f), V(K.Edge - .3f, y, z), K.Hex("B08F58"), Vector3.up, false);
            }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            d.KeepOut.Add(KeepOutBox(d, -1, ramp, far)); d.KeepOut.Add(KeepOutBox(d, 1, ramp, far));
            // heath and outcrops away from the creek
            for (float z = 1f; z < 40f; z += F(r, 3.2f, 4.4f))
            {
                if (Valley(z) > .05f) continue;
                HeathBush(d.SlopedFrontage, V(K.Edge + F(r, 3f, 11f), SeaDune(F(r, 4f, 10f), z, 0f) - .15f, z), F(r, .8f, 1.5f), 400 + (int)(z * 5));
                float u = F(r, 2f, 14f);
                HeathBush(d.SlopedFrontage, V(-(K.Edge + u), LandDune(u, z) - .1f, z), F(r, .8f, 1.6f), 500 + (int)(z * 5));
            }
            Outcrop(d.SlopedFrontage, V(-(K.Edge + 5f), LandDune(5f, 32f) - .3f, 32f), 1.6f, 7);
            Outcrop(d.SlopedFrontage, V(K.Edge + 4f, SeaDune(4f, 3f, 0f) - .3f, 3f), 1.2f, 8);
            Dinghy(d.Props, V(K.Edge + 30f, d.Y(far + 3f) + WSea + .05f, far + 3f), 70f, SailYellow);
            d.GiftSlots.Add(new Vector2(0f, 27f));
            d.Slot(-2.6f, 33f); d.Slot(2.6f, 33f);
            return d;
        }

        static void PathPosts(CqChunkDraft d, float z0, float z1)
        {
            for (float z = z0 + 2f; z < z1; z += 4f)
                foreach (int s in new[] { -1, 1 })
                {
                    d.SlopedFrontage.Surface = ComicSurface.Wood;
                    d.SlopedFrontage.Box(V(s * (K.Edge + .35f), K.Kerb + .45f, z), V(.16f, .9f, .16f), FencePost);
                }
            foreach (int s in new[] { -1, 1 })
                for (float z = z0; z < z1 - .01f; z += 4f)
                    d.SlopedFrontage.Box(V(s * (K.Edge + .35f), K.Kerb + .8f, z + 2f), V(.05f, .05f, 4f), K.Hex("6F6A60"));
        }

        // ---------------------------------------------------------------- Warnbro parts and props

        /// <summary>A coastal heath bush like the kit's plant blob, but low and wide: a squat grey-green ellipsoid and a
        /// smaller, lighter one on top of it, off-centre.</summary>
        static void HeathBush(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = new System.Random(seed * 37 + 11);
            Color[] tones = { Heath, HeathDark, HeathGrey, K.Hex("8B9A5E"), K.Hex("6E8A55") };
            Color c = tones[r.Next(tones.Length)];
            float a = F(r, 0f, Mathf.PI * 2f);
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(p + V(0, s * .26f, 0), V(s * 1.5f, s * .6f, s * 1.25f), c);
            m.Ellipsoid(p + V(Mathf.Cos(a) * s * .22f, s * .48f, Mathf.Sin(a) * s * .18f), V(s * .85f, s * .45f, s * .75f), Color.Lerp(c, HeathLight, .45f));
        }

        /// <summary>A limestone outcrop: a cluster of small craggy pale rocks, tilted and half buried in the dune, with a
        /// tuft of heath growing between them.</summary>
        static void Outcrop(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = new System.Random(seed * 19 + 3);
            m.Surface = ComicSurface.Concrete;
            for (int i = 0; i < 5; i++)
            {
                float a = i * 1.3f + F(r, 0f, .6f), d = i == 0 ? 0f : F(r, .45f, .8f) * s;
                Vector3 size = i == 0 ? V(1.1f, .8f, .9f) * s : V(F(r, .45f, .75f), F(r, .35f, .6f), F(r, .4f, .7f)) * s;
                m.Box(p + V(Mathf.Cos(a) * d, size.y * .3f, Mathf.Sin(a) * d), size,
                    Color.Lerp(K.Hex("E9DCB8"), LimestoneDark, F(r, .05f, .65f)), Quaternion.Euler(F(r, -25f, 25f), F(r, 0f, 90f), F(r, -25f, 25f)));
            }
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(p + V(-.5f * s, .25f * s, .55f * s), V(.8f, .4f, .7f) * s, Heath);
        }

        /// <summary>WA peppermint: leaning 8-sided trunk, a broad weeping crown of olive blobs with hanging tips.</summary>
        static void PeppermintTree(ComicMesh m, Vector3 p, float h, int seed)
        {
            var r = new System.Random(seed * 53 + 9);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .08f, 0), .34f, .16f, K.R.ink, 8, .9f);
            Vector3 top = p + V(F(r, -.5f, .5f), h * .5f, F(r, -.5f, .5f));
            m.Surface = ComicSurface.Wood;
            m.Cylinder((p + top) * .5f, .2f, (top - p).magnitude, K.Hex("6B5844"), 8, .7f, Quaternion.FromToRotation(Vector3.up, top - p));
            K.Plant(m, top + V(0, h * .18f, 0), h * .62f, Peppermint);
            K.Plant(m, top + V(h * .22f, h * .05f, h * .1f), h * .4f, PeppermintLight);
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad + F(r, 0f, .5f);
                m.Box(top + V(Mathf.Cos(a) * h * .26f, -h * .02f, Mathf.Sin(a) * h * .26f), V(.35f, h * .2f, .35f), Color.Lerp(Peppermint, K.R.ink, .12f), Quaternion.Euler(0, a * Mathf.Rad2Deg, 8f));
            }
        }

        /// <summary>A beach access fence: grey timber posts every 2 m following the dune, a top wire and green mesh.</summary>
        static void AccessFence(ComicMesh m, Vector3 a, Vector3 b, Func<float, float, float> ground)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt((b - a).magnitude / 2f));
            Vector3 Pt(int i) { Vector3 q = Vector3.Lerp(a, b, (float)i / n); q.y = ground(q.x, q.z); return q; }
            for (int i = 0; i <= n; i++)
            {
                Vector3 q = Pt(i);
                m.Surface = ComicSurface.Wood;
                m.Box(q + V(0, .5f, 0), V(.14f, 1.1f, .14f), FencePost);
                if (i == n) break;
                Vector3 e = Pt(i + 1);
                m.Surface = ComicSurface.Metal;
                m.Beam(q + V(0, 1f, 0), e + V(0, 1f, 0), .04f, K.Hex("6F6A60"));
                // the mesh: a thin closed panel (two loose quads back to back would be covered by the ink shell)
                m.Surface = ComicSurface.Fabric;
                Vector3 run = e - q;
                m.Box((q + e) * .5f + V(0, .45f, 0), V(.04f, .78f, Mathf.Max(.1f, run.magnitude - .16f)), MeshGreen, Quaternion.LookRotation(run.normalized));
            }
        }

        /// <summary>A beach sign (place name only): two grey posts and a green board with white pixel letters facing the rider.</summary>
        static void BeachSign(ComicMesh m, Vector3 p, string text)
        {
            const float px = .055f;
            float w = PixelWidth(text, px) + .5f;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -w * .38f, w * .38f }) m.Box(p + V(x, .9f, .05f), V(.1f, 1.8f, .1f), K.Hex("9AA0A6"));
            m.Box(p + V(0, 1.55f, 0), V(w, .62f, .06f), K.Hex("1F6B4A"));
            m.Box(p + V(0, 1.55f, .01f), V(w + .08f, .7f, .04f), White);
            PixelText(m, text, p.x, p.y + 1.55f, p.z - .035f, px, White);
        }

        /// <summary>The station name board by the road: a navy board on two posts, WARNBRO in white pixel letters.</summary>
        static void StationSign(ComicMesh m, Vector3 p)
        {
            const float px = .09f;
            float w = PixelWidth("WARNBRO", px) + .7f;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -w * .4f, w * .4f }) m.Box(p + V(x, 1.4f, .05f), V(.12f, 2.8f, .12f), K.Hex("9AA0A6"));
            m.Box(p + V(0, 2.5f, 0), V(w, .8f, .08f), Navy);
            PixelText(m, "WARNBRO", p.x, p.y + 2.5f, p.z - .045f, px, White);
        }

        // Pixel letters for the signs (place names only). Rows top to bottom; W, M and N are wider than 3 pixels so that
        // WARNBRO never reads as HARNBRO.
        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "010/101/111/101/101", ['B'] = "110/101/110/101/110", ['C'] = "011/100/100/100/011", ['E'] = "111/100/110/100/111",
            ['H'] = "101/101/111/101/101", ['N'] = "1001/1101/1011/1001/1001", ['O'] = "010/101/101/101/010", ['R'] = "110/101/110/101/101",
            ['S'] = "011/100/010/001/110", ['W'] = "10001/10001/10101/10101/01010", ['M'] = "10001/11011/10101/10001/10001",
        };

        static float PixelWidth(string text, float px)
        {
            int cols = 0;
            foreach (char ch in text) cols += (Glyphs.TryGetValue(ch, out string g) ? g.IndexOf('/') : 2) + 1;
            return Mathf.Max(0, cols - 1) * px;
        }

        /// <summary>Pixel text facing -Z (towards the rider), centred on (cx, cy) at z.</summary>
        static void PixelText(ComicMesh m, string text, float cx, float cy, float z, float px, Color c)
        {
            m.Surface = ComicSurface.Plain;
            float x = cx - PixelWidth(text, px) * .5f, y0 = cy + 2.5f * px;
            foreach (char ch in text)
            {
                if (!Glyphs.TryGetValue(ch, out string g)) { x += 3 * px; continue; }
                string[] rows = g.Split('/');
                for (int row = 0; row < rows.Length; row++)
                    for (int col = 0; col < rows[row].Length; col++)
                    {
                        if (rows[row][col] != '1') continue;
                        float qx = x + col * px, qy = y0 - (row + 1) * px;
                        m.Quad(V(qx + px, qy, z), V(qx, qy, z), V(qx, qy + px, z), V(qx + px, qy + px, z), c, false);
                    }
                x += (rows[0].Length + 1) * px;
            }
        }

        /// <summary>The surf life saving club: a cream ground floor with three blue roller doors (boat sheds), the glazed
        /// clubroom above with a deck and white balustrade facing the sea, a flat roof with a deep overhang. Road side at -X.</summary>
        static void SurfClub(ComicMesh f, Vector3 p)
        {
            f.Surface = ComicSurface.Plaster;
            f.Box(p + V(0, 1.8f, 0), V(14f, 3.6f, 22f), K.Hex("EDE3CC"));
            f.Surface = ComicSurface.PaintedMetal;
            for (int k = -1; k <= 1; k++)
            {
                f.Box(p + V(-7.02f, 1.5f, k * 6.5f), V(.06f, 3f, 4.8f), CanopyBlue);
                for (float y = .4f; y < 3f; y += .35f) f.Box(p + V(-7.05f, y, k * 6.5f), V(.03f, .05f, 4.7f), Color.Lerp(CanopyBlue, K.R.ink, .3f));
            }
            f.Box(p + V(0, 3.7f, 0), V(14.4f, .2f, 22.4f), White);
            f.Surface = ComicSurface.Glass;
            f.Box(p + V(1.5f, 5.3f, 0), V(9f, 3f, 18f), GlassDark);
            f.Surface = ComicSurface.PaintedMetal;
            for (float z = -8.5f; z <= 8.51f; z += 1.7f) { f.Box(p + V(6.03f, 5.3f, z), V(.08f, 3f, .12f), White); f.Box(p + V(-3.03f, 5.3f, z), V(.08f, 3f, .12f), White); }
            f.Box(p + V(1.5f, 6.95f, 0), V(11.5f, .32f, 21f), White);
            f.Box(p + V(1.5f, 6.75f, 0), V(11.3f, .1f, 20.8f), K.R.ink);
            // the sea-facing deck balustrade
            f.Surface = ComicSurface.Glass;
            f.Box(p + V(6.85f, 4.3f, 0), V(.08f, .9f, 21.6f), Balustrade);
            f.Surface = ComicSurface.PaintedMetal;
            f.Box(p + V(6.85f, 4.8f, 0), V(.14f, .1f, 21.8f), White);
            // the club's colours: a red and yellow band and a flagpole
            f.Box(p + V(-7.05f, 3.35f, 0), V(.04f, .25f, 21.8f), PatrolRed);
            f.Box(p + V(-7.06f, 3.1f, 0), V(.04f, .2f, 21.8f), PatrolYellow);
            f.Cylinder(p + V(-4f, 9f, 9f), .07f, 4f, White, 6);
            PatrolFlagCloth(f, p + V(-4f, 10.4f, 9f), 1.3f);
        }

        /// <summary>The patrol tower on the beach: four white legs with cross braces, a red cabin with dark windows, a flat
        /// white roof, a red-and-yellow flag on top.</summary>
        static void PatrolTower(ComicMesh f, Vector3 p)
        {
            f.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -1.2f, 1.2f }) foreach (float z in new[] { -1.2f, 1.2f }) f.Box(p + V(x, 2.2f, z), V(.18f, 4.4f, .18f), White);
            foreach (float x in new[] { -1.2f, 1.2f }) f.Beam(p + V(x, .4f, -1.2f), p + V(x, 4f, 1.2f), .1f, White);
            f.Box(p + V(0, 4.45f, 0), V(3.1f, .2f, 3.1f), K.R.ink);
            f.Box(p + V(0, 5.6f, 0), V(2.8f, 2.1f, 2.8f), PatrolRed);
            f.Surface = ComicSurface.Glass;
            f.Box(p + V(0, 5.9f, 0), V(2.86f, .9f, 2.4f), GlassDark);
            f.Box(p + V(0, 5.9f, 0), V(2.4f, .9f, 2.86f), GlassDark);
            f.Surface = ComicSurface.PaintedMetal;
            f.Box(p + V(0, 6.8f, 0), V(3.4f, .2f, 3.4f), White);
            f.Box(p + V(0, 5f, 0), V(2.84f, .25f, 2.84f), PatrolYellow);
            f.Cylinder(p + V(.9f, 7.9f, .9f), .05f, 2.2f, White, 6);
            PatrolFlagCloth(f, p + V(.9f, 8.6f, .9f), .9f);
            // ladder
            f.Beam(p + V(-1.6f, 0, 1.6f), p + V(-1.2f, 4.4f, 1f), .08f, White);
            f.Beam(p + V(-1.6f, 0, .8f), p + V(-1.2f, 4.4f, .3f), .08f, White);
        }

        /// <summary>A surf lifesaving flag: split diagonally, red over yellow (two thick triangles).</summary>
        static void PatrolFlagCloth(ComicMesh m, Vector3 pole, float s)
        {
            m.Surface = ComicSurface.Fabric;
            Vector3 a = pole + V(0, .35f * s, 0), b = pole + V(0, .35f * s, -1f * s), c = pole + V(0, -.35f * s, -1f * s), d = pole + V(0, -.35f * s, 0);
            foreach (float x in new[] { -.03f, .03f })
            {
                Vector3 o = V(x, 0, 0), n = new Vector3(Mathf.Sign(x), 0, 0);
                K.TriFacing(m, a + o, b + o, d + o, PatrolRed, n);
                K.TriFacing(m, b + o, c + o, d + o, PatrolYellow, n);
            }
        }

        static void PatrolFlagArt(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(p + V(0, .15f, 0), .3f, .3f, Sand, 8, .5f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 1.4f, 0), .04f, 2.8f, White, 6);
            PatrolFlagCloth(m, p + V(0, 2.4f, 0), .8f);
        }

        static void RescueBoard(ComicMesh m, Vector3 p, int i)
        {
            m.Surface = ComicSurface.Plastic;
            Quaternion q = Quaternion.Euler(-72f, 90f + i * 4f, 0);
            m.Box(p + V(0, 1.3f, 0), V(.6f, .1f, 3f), i % 2 == 0 ? PatrolYellow : PatrolRed, q);
            m.Box(p + V(0, 1.3f, 0) + q * V(0, .06f, 0), V(.2f, .02f, 2.6f), White, q);
        }

        /// <summary>A Warnbro house: limestone plinth, rendered walls, white-framed windows, a garage door, a hipped roof of
        /// terracotta tiles or Colorbond, ink eaves. Facade towards the road.</summary>
        static void House(ComicMesh f, CqLot l, Color wall, Color roof, bool colorbond)
        {
            float W = l.W, D = l.Depth;
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, W, -2.4f, .25f, .02f, -D, Limestone);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0, W, .25f, 3f, 0f, -D, wall);
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, W - 3.6f, W - .4f, .25f, 2.5f, .06f, -.1f, colorbond ? Surfmist : K.Hex("D9D4C7"));
            for (float y = .5f; y < 2.4f; y += .3f) K.LBox(f, l, W - 3.6f, W - .4f, y, y + .04f, .08f, -.1f, Color.Lerp(Surfmist, K.R.ink, .25f));
            f.Surface = ComicSurface.Glass;
            for (float a = .8f; a < W - 4.5f; a += 2.6f) { K.LBox(f, l, a, a + 1.8f, .9f, 2.4f, .05f, -.1f, White); K.LBox(f, l, a + .1f, a + 1.7f, 1f, 2.3f, .08f, -.1f, GlassDark); }
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, -.5f, W + .5f, 2.95f, 3.1f, .6f, -D - .5f, K.R.ink);
            f.Surface = colorbond ? ComicSurface.PaintedMetal : ComicSurface.Brick;
            K.LSlope(f, l, -.5f, W + .5f, 3.05f, 5.2f, .6f, -D * .5f, -D - .5f, roof);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, W * .65f, W * .65f + .7f, 4.2f, 5.6f, -D * .4f, -D * .4f - .7f, wall);
        }

        /// <summary>A parked car, nose towards +X at yaw 0: body, cabin with dark glass, ink bumpers, four wheels.</summary>
        static void ParkedCar(ComicMesh m, Vector3 p, float yaw, Color c)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            void B(Vector3 at, Vector3 size, Color col, ComicSurface s) { m.Surface = s; m.Box(p + q * at, size, col, q); }
            B(V(0, .62f, 0), V(1.75f, .6f, 4.1f), c, ComicSurface.PaintedMetal);
            B(V(0, 1.18f, -.25f), V(1.55f, .55f, 2.2f), c, ComicSurface.PaintedMetal);
            B(V(0, 1.18f, -.25f), V(1.6f, .42f, 2f), GlassDark, ComicSurface.Glass);
            B(V(0, .45f, 2.06f), V(1.8f, .22f, .1f), K.R.ink, ComicSurface.PaintedMetal);
            B(V(0, .45f, -2.06f), V(1.8f, .22f, .1f), K.R.ink, ComicSurface.PaintedMetal);
            foreach (float z in new[] { 1.3f, -1.3f }) foreach (float x in new[] { -.82f, .82f }) B(V(x, .32f, z), V(.24f, .62f, .62f), K.R.ink, ComicSurface.Rubber);
        }

        static void CarParkLight(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .3f, 0), .3f, .6f, K.Hex("C9C4BA"), 8);
            m.Cylinder(p + V(0, 4.6f, 0), .08f, 8.4f, K.Hex("9AA0A6"), 8, .7f);
            foreach (int s in new[] { -1, 1 }) m.Box(p + V(s * .6f, 8.75f, 0), V(1.1f, .18f, .4f), K.R.cream);
        }

        static void MountsBayStyleLamp(ComicMesh m, Vector3 p)
        {
            Color post = new Color(.28f, .32f, .36f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .15f, 0), .22f, .3f, K.R.ink, 8, .9f);
            m.Cylinder(p + V(0, 3.6f, 0), .08f, 6.9f, post, 8, .8f);
            m.Box(p + V(-.7f, 6.95f, 0), V(1.4f, .1f, .1f), post);
            m.Box(p + V(-1.35f, 6.82f, 0), V(.55f, .16f, .32f), K.R.cream);
            m.Box(p + V(-1.35f, 6.94f, 0), V(.6f, .08f, .36f), K.R.ink);
        }

        /// <summary>The station: a long platform under a white canopy roof on navy columns, a glass shelter, a lift tower.</summary>
        static void StationCanopy(ComicMesh f, Vector3 p, float length)
        {
            f.Surface = ComicSurface.Concrete;
            f.Box(p + V(0, .5f, length * .5f), V(6f, 1f, length), K.Hex("CFC9BC"));
            f.Box(p + V(-2.9f, 1.02f, length * .5f), V(.3f, .05f, length), PatrolYellow);
            f.Surface = ComicSurface.PaintedMetal;
            for (float z = 2f; z < length; z += 6f) f.Box(p + V(0, 3.2f, z), V(.25f, 4.4f, .25f), Navy);
            f.Box(p + V(0, 5.5f, length * .5f), V(7.6f, .35f, length + 1f), White);
            f.Box(p + V(0, 5.82f, length * .5f), V(6.8f, .3f, length + .6f), K.Hex("8FA3B5"));
            f.Box(p + V(0, 5.28f, length * .5f), V(7.62f, .1f, length + 1.02f), K.R.ink);
            f.Surface = ComicSurface.Glass;
            f.Box(p + V(1.2f, 2.2f, length * .3f), V(1.6f, 2.4f, 4f), Balustrade);
            f.Surface = ComicSurface.Plaster;
            f.Box(p + V(1.5f, 4f, length * .75f), V(2.6f, 8f, 2.6f), K.Hex("E4E2D5"));
            f.Box(p + V(1.5f, 8.1f, length * .75f), V(2.8f, .2f, 2.8f), Navy);
        }

        /// <summary>A suburban train (no branding): silver carriages with a navy window band, darker doors, ink roof gear.</summary>
        static void Train(ComicMesh m, Vector3 p, int cars)
        {
            for (int i = 0; i < cars; i++)
            {
                Vector3 c = p + V(0, 0, i * 20.5f + 10f);
                m.Surface = ComicSurface.Metal;
                m.Box(c + V(0, 2.3f, 0), V(3f, 3.4f, 20f), K.Hex("D9DEE3"));
                m.Surface = ComicSurface.Glass;
                m.Box(c + V(0, 2.8f, 0), V(3.04f, .9f, 19f), Navy);
                m.Surface = ComicSurface.PaintedMetal;
                for (float z = -7f; z <= 7.01f; z += 7f) m.Box(c + V(0, 2.1f, z), V(3.06f, 2.6f, 1.4f), K.Hex("8C96A0"));
                m.Box(c + V(0, 4.1f, 0), V(2.6f, .3f, 18f), K.Hex("9AA4AE"));
                m.Box(c + V(0, .5f, 0), V(2.6f, .6f, 18f), K.R.ink);
            }
        }

        // ---------------------------------------------------------------- Warnbro obstacles and animals

        // A timber-and-steel surf rack with four boards standing in it.
        static ComicMesh SurfRackMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            foreach (float x in new[] { -.9f, .9f }) m.Box(V(x, .55f, 0), V(.12f, 1.1f, .5f), FencePost);
            m.Box(V(0, .95f, 0), V(1.95f, .1f, .12f), FencePost);
            m.Box(V(0, .25f, 0), V(1.95f, .1f, .5f), FencePost);
            Color[] boards = { White, K.Hex("4FC3D0"), PatrolYellow, SailRed };
            m.Surface = ComicSurface.Plastic;
            for (int i = 0; i < 4; i++)
            {
                float x = -.65f + i * .43f;
                m.Box(V(x, 1f, 0), V(.08f, 1.9f, .5f), boards[i], Quaternion.Euler(0, 0, (i - 1.5f) * 3f));
                m.Box(V(x, 1.92f, 0), V(.085f, .1f, .34f), boards[i], Quaternion.Euler(0, 0, (i - 1.5f) * 3f));
                m.Box(V(x + .01f, 1f, 0), V(.09f, 1.7f, .04f), Color.Lerp(boards[i], K.R.ink, .3f), Quaternion.Euler(0, 0, (i - 1.5f) * 3f));
            }
            return m;
        }

        // A beach wheelie bin: dark green body, red lid, ink wheels.
        static ComicMesh WheelieBinMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            m.Box(V(0, .52f, 0), V(.6f, .9f, .66f), K.Hex("2F5D3A"));
            m.Box(V(0, 1.02f, .02f), V(.66f, .1f, .74f), K.Hex("D23B2E"));
            m.Box(V(0, 1f, .4f), V(.6f, .08f, .08f), K.R.ink);
            m.Surface = ComicSurface.Rubber;
            foreach (float x in new[] { -.28f, .28f }) m.Cylinder(V(x, .12f, .3f), .12f, .1f, K.R.ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            return m;
        }

        // A dune fence panel: two posts, top and middle rails and green mesh.
        static ComicMesh DuneFenceMesh()
        {
            var m = new ComicMesh();
            AccessFence(m, V(-1.2f, 0, 0), V(1.2f, 0, 0), (x, z) => 0f);
            return m;
        }

        static ComicMesh PatrolFlagMesh()
        {
            var m = new ComicMesh();
            PatrolFlagArt(m, Vector3.zero);
            return m;
        }

        // Bobtail (shingleback) lizard: a chunky scaly brown body with pale bands, a stumpy tail, a dark head, blue tongue.
        static ComicMesh BobtailMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            Color body = K.Hex("6E5A44"), band = K.Hex("B7A27E"), head = K.Hex("4C3F33");
            m.Ellipsoid(V(0, .12f, 0), V(.3f, .19f, .6f), body);
            for (int i = 0; i < 4; i++) m.Box(V(0, .2f, -.18f + i * .12f), V(.24f, .04f, .05f), band);
            m.Ellipsoid(V(0, .11f, .36f), V(.24f, .15f, .3f), body);
            m.Box(V(0, .1f, -.37f), V(.18f, .13f, .2f), head);
            m.Box(V(0, .08f, -.5f), V(.07f, .02f, .07f), K.Hex("3A6FD8"));
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(V(s * .08f, .15f, -.43f), V(.03f, .03f, .03f), K.R.ink);
                foreach (float z in new[] { -.16f, .16f }) m.Box(V(s * .17f, .04f, z), V(.1f, .07f, .08f), head);
            }
            return m;
        }

        // ---------------------------------------------------------------- Warnbro far scenery

        /// <summary>
        /// The whole horizon at Warnbro Sound: the open Indian Ocean on the right (paler with distance) with a few boats,
        /// the coastline curving out to the south-west ahead-right (white beach, low dunes), Garden Island faint far to the
        /// north on the right edge; on the land side rolling heath dunes, tile-roofed houses, Norfolk pines and the hazy
        /// scarp. Sheared with the grade so it meets the chunks' water. No colliders.
        /// </summary>
        static ComicMesh WarnFar(float grade)
        {
            var m = new ComicMesh();
            var r = new System.Random(150);
            Color haze = K.Hex("C6DDE8");
            const float z0 = -220f, z1 = 820f;
            float shore = K.Edge + 33f;
            FarStrip(m, K.Edge, K.Edge + 18f, 330f, z1, .6f, Heath);
            FarStrip(m, K.Edge + 18f, shore, 330f, z1, -.9f, BeachWhite);
            FarStrip(m, shore, 170f, 330f, z1, WSea - .06f, Turquoise);
            FarStrip(m, 160f, 300f, z0, z1, WSea - .06f, Color.Lerp(K.Hex("2A8CC0"), haze, .1f));
            FarStrip(m, 300f, 600f, z0, z1, WSea - .06f, Color.Lerp(K.Hex("2A8CC0"), haze, .3f));
            FarStrip(m, 600f, 1500f, z0, z1, WSea - .06f, Color.Lerp(K.Hex("2A8CC0"), haze, .5f));
            FarStrip(m, -K.Edge - .5f, -40f, 330f, z1, 1.2f, Heath);
            FarStrip(m, -40f, -200f, z0, z1, -.2f, Haze(K.Hex("8E9C6A"), 120f, haze));
            FarStrip(m, -200f, -480f, z0, z1, -.3f, Haze(K.Hex("97A57A"), 300f, haze));
            FarStrip(m, -480f, -1500f, z0, z1, -.35f, Haze(K.Hex("9FAE88"), 650f, haze));
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 170; i++)
            {
                float x = F(r, 60f, 620f), z = F(r, -150f, 760f), len = F(r, 2f, 8f), y = WSea - .04f;
                K.Face(m, V(x, y, z), V(x, y, z + .5f), V(x + len, y, z + .5f), V(x + len, y, z), Haze(i % 3 == 0 ? Foam : SeaRipple, x, haze), Vector3.up, false);
            }
            for (float z = z0; z < z1; z += 6f)
                K.Face(m, V(shore + 22f, WSea - .03f, z), V(shore + 22f, WSea - .03f, z + 4f), V(shore + 23f, WSea - .03f, z + 4f), V(shore + 23f, WSea - .03f, z), Foam, Vector3.up, false);
            // the coastline curving out to the south-west (Port Kennedy, Becher Point): beach and low dunes ahead-right
            Vector3 prevS = Vector3.zero;
            for (int i = 0; i <= 18; i++)
            {
                float t = i / 18f, ang = Mathf.Lerp(18f, 62f, t) * Mathf.Deg2Rad, dist = Mathf.Lerp(330f, 300f, t);
                Vector3 s = V(Mathf.Sin(ang) * dist, WSea, Mathf.Cos(ang) * dist);
                Vector3 inward = V(Mathf.Sin(ang), 0, Mathf.Cos(ang));
                if (i > 0)
                {
                    float d2 = (s.magnitude + prevS.magnitude) * .5f;
                    m.Surface = ComicSurface.Paving;
                    K.Face(m, prevS, s, s + inward * 14f + V(0, 1f, 0), prevS + inward * 14f + V(0, 1f, 0), Haze(BeachWhite, d2, haze), (Vector3.up * 2f - inward).normalized, false);
                    m.Surface = ComicSurface.Foliage;
                    float h0 = 5f + 2.5f * Mathf.Sin(i * 1.3f), h1 = 5f + 2.5f * Mathf.Sin((i - 1) * 1.3f);
                    K.Face(m, prevS + inward * 14f + V(0, 1f, 0), s + inward * 14f + V(0, 1f, 0), s + inward * 30f + V(0, h0, 0), prevS + inward * 30f + V(0, h1, 0), Haze(Heath, d2, haze), (Vector3.up - inward).normalized, false);
                    K.Face(m, prevS + inward * 30f + V(0, h1, 0), s + inward * 30f + V(0, h0, 0), s + inward * 90f + V(0, 1f, 0), prevS + inward * 90f + V(0, 1f, 0), Haze(HeathGrey, d2 + 40f, haze), Vector3.up, false);
                }
                prevS = s;
            }
            // Garden Island: very faint and low, far away on the right edge (to the north)
            Ridge(m, 1, 520f, 120f, z0, 120f, WSea, z => 9f + 3f * Mathf.Sin(z * .015f + 1f), K.Hex("E6DECA"), K.Hex("7D9471"), K.Hex("8EA27E"), haze, .62f);
            // boats on the Sound
            Color[] bands = { K.R.red, Navy, SailTeal, White };
            for (int i = 0; i < 7; i++)
            {
                float x = F(r, 70f, 300f), z = F(r, -40f, 260f);
                var boat = new ComicMesh();
                Sailboat(boat, Vector3.zero, 1f, bands[i % bands.Length]);
                m.Add(boat, V(x, WSea - .1f, z), Quaternion.Euler(0, F(r, -50f, 50f), 0), Vector3.one * F(r, 1.4f, 2f));
            }
            // land side: rolling heath dunes, houses with tiled and Colorbond roofs, Norfolk pines, the far scarp
            for (float z = z0; z < z1; z += F(r, 12f, 20f))
            {
                float x = -F(r, 44f, 90f), dist = Mathf.Sqrt(x * x + z * z);
                m.Surface = ComicSurface.Foliage;
                m.Cylinder(V(x, 1.2f, z), F(r, 9f, 15f), F(r, 3f, 6f), Haze(Color.Lerp(Heath, HeathGrey, F(r, 0f, 1f)), dist, haze), 7, .55f);
            }
            for (float z = z0; z < z1; z += F(r, 12f, 18f))
                for (int row = 0; row < 2; row++)
                {
                    float x = -(96f + row * 30f + F(r, 0f, 14f)), dist = Mathf.Sqrt(x * x + z * z), w = F(r, 9f, 13f), h = F(r, 3f, 5f);
                    m.Surface = ComicSurface.Plaster;
                    m.Box(V(x, h * .5f, z), V(w, h + 2f, w * .8f), Haze(r.NextDouble() < .5 ? Render : Surfmist, dist, haze));
                    m.Surface = ComicSurface.Brick;
                    m.Cylinder(V(x, h + 2f, z), w * .74f, 2.2f, Haze(r.NextDouble() < .6 ? Terracotta : Colorbond, dist, haze), 4, .15f, Quaternion.Euler(0, 45, 0));
                }
            for (float z = z0; z < z1; z += F(r, 26f, 40f)) CheapPine(m, V(-F(r, 60f, 160f), 1f, z), F(r, 15f, 22f), Haze(PineDark, 150f, haze));
            for (float z = z0; z < z1; z += F(r, 15f, 22f))
            {
                m.Surface = ComicSurface.Foliage;
                m.Ellipsoid(V(-F(r, 180f, 240f), 5f, z), V(F(r, 16f, 24f), F(r, 8f, 12f), F(r, 14f, 20f)), Haze(Color.Lerp(K.Hex("63804A"), K.Hex("7F9A5C"), F(r, 0f, 1f)), 260f, haze));
            }
            Ridge(m, -1, 560f, 160f, z0, z1, -.4f, z => 32f + 8f * Mathf.Sin(z * .009f + 1f) + 4f * Mathf.Sin(z * .027f), K.Hex("8FA27E"), K.Hex("7F95A6"), K.Hex("93A7B8"), haze, .55f);
            m.Shear(grade);
            return m;
        }

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static float F(System.Random r, float a, float b) => Mathf.Lerp(a, b, (float)r.NextDouble());
    }
}
