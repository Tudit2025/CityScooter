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
    /// South line, art agent 1 (v0.4): CANNING BRIDGE (order 80) and MURDOCH (order 90). The rider is on the Kwinana
    /// Freeway principal shared path (RoadType.PedestrianMall: no cars). On the left, behind a fence, the freeway and the
    /// Mandurah line in its median (decoration, out of reach); on the right the Canning River (Canning Bridge) or the noise
    /// walls and banksia bush (Murdoch). Same street cross-section as every district (path 8 m + 4 m verges at 0.2 m).
    ///   CANNING BRIDGE: foreshore with Norfolk pines, jetty, boats and pelicans; Canning Bridge station on its island
    ///   platform with the glass concourse bridge over the path and the freeway; the path bridge over the river beside the
    ///   freeway bridge and the long low Canning Highway bridge; the Applecross shore (jacarandas in bloom, the art-deco
    ///   riverside hotel shape, apartment towers); a river inlet with a timber jetty kicker and a SPLASH gap.
    ///   MURDOCH: patterned noise walls, Murdoch station under the silver bus-interchange hall that spans the freeway,
    ///   Murdoch University banksia woodland (sand, balga, banksia) with a path hump, Fiona Stanley Hospital with a rescue
    ///   helicopter on the roof helipad.
    /// Field ownership as QuayBuilder: gateway, ambience and the sky fields (skyYaw, skySize, skyHeight, skyline) are never
    /// written (placeholders only when the asset is created: the CBD turned behind and sunk, skyline 0, so the far scenery
    /// is the whole horizon). Landmarks are decoration (no colliders).
    ///   Menu:  CityScooter/Districts/Build Canning Bridge and Murdoch
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.CanningMurdochBuilder.BuildBatch
    ///          -executeMethod CityScooter.EditorTools.Districts.CanningMurdochBuilder.BuildAndRenderAtlasBatch (graphics)
    /// </summary>
    public static class CanningMurdochBuilder
    {
        public const string DataFolder = "Assets/_Project/Data/Districts";
        public const string CanningId = "canningbridge", MurdochId = "murdoch";
        public const string CanningTag = "CanningBridge", MurdochTag = "Murdoch";
        public static string PrefabFolder(string tag) => ComicCityBuilder.Prefabs + "/Districts/" + tag;
        public static string MeshFolder(string tag) => ComicCityBuilder.Root + "/Districts/" + tag;
        public static string DistrictPath(string tag) => DataFolder + "/District_" + tag + ".asset";
        public static string ChunkPath(string tag, string variant) => PrefabFolder(tag) + "/CHN_" + tag + "_" + variant + ".prefab";
        static string Pf(string tag, string name) => PrefabFolder(tag) + "/" + name + ".prefab";
        public static readonly string[] CanningChunks = { "Foreshore", "Station", "Bridge", "Applecross", "Inlet" };
        public static readonly string[] MurdochChunks = { "NoiseWall", "Station", "Bushland", "Hospital" };

        /// <summary>Riverside: flat (-1.6 m per 40 m). Murdoch: gentle (-2.4 m per 40 m).</summary>
        public const float CanningDrop = -1.6f, MurdochDrop = -2.4f;
        public const float WaterY = -1.3f, RiverFar = 260f;
        // Canning inlet: a timber jetty kicker over the whole width, then open water (SPLASH). Numbers for gameplay:
        public const float JettyStart = 6f, JettyRise = 6f, JettyHeight = 1.4f, JettyTop = 3f, GapLength = 6f;
        // Murdoch bushland: a path hump (up, crest, back down) over the path itself (|x| <= 4); the verges go round it.
        public const float HumpStart = 10f, HumpRise = 6f, HumpHeight = 1.2f, HumpTop = 4f, HumpDown = 6f, HumpWidth = 8f;

        // Left side (the freeway), distances from the road centre line (x is negative there).
        const float FenceX = 8.45f, CwA0 = 11.85f, CwA1 = 23.1f, Rail0 = 24.3f, Rail1 = 36.9f, CwB1 = 48.3f, FarVerge = 75f;
        static readonly float[] Tracks = { 25.8f, 35.4f };
        const float PlatformA = 27.2f, PlatformB = 34f, PlatformY = 1f;

        static Color H(string hex) => K.Hex(hex);
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // Palette (sRGB; sources in art-src/districts/canningbridge|murdoch/SOURCES.txt).
        static readonly Color PathGrey = H("5C636C"), PathRed = H("A4523F"), VergeGrey = H("CBC7BC"), VergeWarm = H("D6CDB6");
        static readonly Color FwyAsphalt = H("4B525B"), LineWhite = new Color(.95f, .95f, .93f), Barrier = H("D6D2C8"), Ballast = H("8C8378"),
            Sleeper = H("6C5B4A"), RailSteel = H("5E646C"), MastGrey = H("9AA1A8"), FenceInk = H("2A3038"), Thrie = H("A9AFB5");
        static readonly Color DryGrass = H("B9AA6E"), Lawn = H("79A64C"), Hedge = H("4C7F3A"), Peppermint = H("5F8B3A");
        static readonly Color Limestone = H("E2D4AF"), LimestoneDark = H("BFAE86"), LimeCap = H("F1E9D2"), Rock = H("A8A191");
        static readonly Color Water = H("2F6E9A"), Ripple = H("5D9AC6"), FarWater = H("3B79A6");
        static readonly Color Timber = H("8A5A3C"), RailWhite = H("F2F2EC"), Concrete = H("D8D4CA"), ConcreteDark = H("8E8B85");
        static readonly Color J1 = H("8E6CDF"), J2 = H("A98AF0"), J3 = H("C9B4FA"), Bark = H("5E4B3E");
        static readonly Color PineDark = H("2E5B3A"), PineLight = H("43784A"), PineBark = H("6B4E3A");
        static readonly Color Brick = H("B5523B"), Tactile = H("9A9EA4"), CanopyWhite = H("F1F1EC"), CanopyUnder = H("9AA3AC"), ConcourseGlass = H("3E7F86");
        static readonly Color TrainWhite = H("F0F0EC"), TrainGreen = H("2E8B57"), TrainBand = H("1E252C"), TrainNose = H("F2B632");
        static readonly Color HotelCream = H("F4EAD0"), HotelTrim = H("5E8C96"), DarkGlass = H("2E4652"), TowerGlass = H("4C8FA3"), TowerWhite = H("EEF0EE");
        static readonly Color Sand = H("E6D7AE"), SandDark = H("D3C08F"), Litter = H("B9A57A");
        static readonly Color[] WallColours = { H("E2CFA2"), H("C0643E"), H("D9A441"), H("3E8C8A") };
        static readonly Color Silver = H("BCC3C9"), SilverDark = H("8A939B"), FshWhite = H("F1EFE9"), FshGrey = H("C9CCD0");
        static readonly Color[] FinColours = { H("E58A3A"), H("6DB04A"), H("3F8FCF"), H("F2C440"), H("D84A3A") };
        static readonly Color HeliRed = H("D8322A"), SignGreen = H("1F7A4D"), SignNavy = H("1F3A6E"), TileRed = H("B5533C"), Colorbond = H("6E7C87");

        // ==================================================================== build

        [MenuItem("CityScooter/Districts/Build Canning Bridge and Murdoch")]
        public static void Build()
        {
            BuildCanning();
            BuildMurdoch();
            AssetDatabase.SaveAssets();
        }

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Build both districts, then render their props atlases (needs graphics: no -nographics).</summary>
        public static void BuildAndRenderAtlasBatch()
        {
            try { Build(); RenderAtlases(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static GameObject Cyclist() => AssetDatabase.LoadAssetAtPath<GameObject>(ComicCityBuilder.Prefabs + "/Districts/MountsBay/PED_MountsBay_Cyclist.prefab");

        static District FillCommon(string tag, string id, string name, int order, TrackTheme theme)
        {
            string path = DistrictPath(tag);
            bool created = AssetDatabase.LoadAssetAtPath<District>(path) == null;
            District d = K.LoadOrCreate<District>(path);
            // Sky fields belong to the system: placeholders only on creation (the CBD skyline turned behind and sunk).
            if (created) { d.skyYaw = 180f; d.skySize = .6f; d.skyHeight = -12f; d.skyline = 0f; }
            d.id = id; d.displayName = name; d.order = order; d.length = new Vector2(320f, 440f);
            d.chunks = (tag == CanningTag ? CanningChunks : MurdochChunks).Select(c => AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(tag, c)).GetComponent<RoadChunk>()).ToArray();
            d.orderedChunks = true;
            d.entry = AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(tag, "Entry")).GetComponent<RoadChunk>();
            d.exit = AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(tag, "Exit")).GetComponent<RoadChunk>();
            d.allowBoulevard = false;
            d.jumpObstacles = Array.Empty<Obstacle>();
            d.gifts = Array.Empty<Gift>();
            d.blendMeters = 150f;
            return d;
        }

        static void BuildCanning()
        {
            const string tag = CanningTag;
            string patterns = DataFolder + "/" + tag;
            K.Begin(MeshFolder(tag), PrefabFolder(tag), DataFolder, patterns);
            P.Init();

            Obstacle bollard = K.SaveObstacle(Pf(tag, "OBS_CanningBridge_Bollard"), "OBS_CanningBridge_Bollard", BollardMesh(RailWhite, H("2F6E9A")), V(.5f, 1f, .5f), ObstacleKind.Thing, true, true, 1.2f);
            Obstacle kayak = K.SaveObstacle(Pf(tag, "OBS_CanningBridge_Kayak"), "OBS_CanningBridge_Kayak", KayakMesh(), V(1f, .9f, 3.6f), ObstacleKind.Thing, true, true, .8f);
            Obstacle sign = K.SaveObstacle(Pf(tag, "OBS_CanningBridge_PathSign"), "OBS_CanningBridge_PathSign", WarningSignMesh(H("F28C28")), V(1f, 1.7f, .7f), ObstacleKind.Thing, false, true, .7f);
            GameObject pelican = SaveAnimal(Pf(tag, "ANM_CanningBridge_Pelican"), "ANM_CanningBridge_Pelican", PelicanMesh(), V(0, .65f, 0), V(.7f, 1.3f, 1.4f), .4f);
            Ramp jetty = K.SaveRamp(Pf(tag, "RMP_CanningBridge_Jetty"), "RMP_CanningBridge_Jetty", 3f, 5f, 1.3f, 1.5f, 1f, .5f, Timber, Color.Lerp(Timber, K.R.ink, .3f), false);
            Ramp joint = K.SaveRamp(Pf(tag, "RMP_CanningBridge_Joint"), "RMP_CanningBridge_Joint", 3.2f, 4.5f, 1.1f, 0f, 1f, .5f, Concrete, ConcreteDark, true);

            K.WriteChunk(CbEntry(), ChunkPath(tag, "Entry"));
            K.WriteChunk(CbForeshore(), ChunkPath(tag, "Foreshore"));
            K.WriteChunk(CbStation(), ChunkPath(tag, "Station"));
            K.WriteChunk(CbBridge(), ChunkPath(tag, "Bridge"));
            K.WriteChunk(CbApplecross(), ChunkPath(tag, "Applecross"));
            K.WriteChunk(CbInlet(), ChunkPath(tag, "Inlet"));
            K.WriteChunk(CbExit(), ChunkPath(tag, "Exit"));
            GameObject far = K.SaveFarScenery(Pf(tag, "FAR_CanningBridge_River"), "FAR_CanningBridge_River", CanningFar());

            GameObject cyclist = Cyclist();
            var own = new List<HazardPattern>
            {
                K.Pattern(patterns, "HZ_CanningBridge_Pelicans", 45, 0, 1f, true, 30, (pelican, -2.6f, 22f, 0f), (pelican, -1f, 24f, 0f), (pelican, 2.2f, 28f, 0f)),
                K.Pattern(patterns, "HZ_CanningBridge_Bollards", 40, 1, .7f, false, 30, (bollard.gameObject, -2.6f, 20f, 0f), (bollard.gameObject, 0f, 20f, 0f), (bollard.gameObject, 2.6f, 20f, 0f)),
            };
            if (cyclist != null) own.Add(K.Pattern(patterns, "HZ_CanningBridge_Cyclists", 50, 0, 1f, true, 30, (cyclist, -2.4f, 24f, 0f), (cyclist, -1.2f, 31f, 0f)));

            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme == null) throw new InvalidOperationException("No comic theme at " + ComicCityBuilder.ThemePath);
            District d = FillCommon(tag, CanningId, "CANNING BRIDGE", 80, theme);
            var obstacles = new List<Obstacle> { bollard, kayak, sign };
            Obstacle cones = P.BaseObstacle("Cones"); if (cones != null) obstacles.Add(cones);
            d.obstacles = obstacles.ToArray();
            var hazards = new List<HazardPattern>(own);
            if (theme.hazards != null) hazards.AddRange(theme.hazards.Where(h => h != null && h.name == "HZ_SidewalkStroll"));
            d.hazards = hazards.ToArray();
            var people = new List<GameObject>(K.People(theme, "Jogger", "Jogger", "DogWalker", "Elder", "Kid", "Tourist", "Student"));
            if (cyclist != null) { people.Add(cyclist); people.Add(cyclist); }
            people.Add(pelican); people.Add(pelican);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { jetty, joint };
            d.rampFrequency = 1.3f;
            // Bright river light, cool blue haze.
            d.fogColor = new Color(.70f, .83f, .92f);
            d.skyTint = new Color(.97f, 1f, 1f);
            d.ambientSky = new Color(.66f, .83f, .97f);
            d.ambientEquator = new Color(.85f, .88f, .86f);
            d.ambientGround = new Color(.60f, .60f, .55f);
            d.sunColor = new Color(1f, .97f, .90f);
            d.sunIntensity = 1.45f;
            d.farScenery = far;
            // gateway, ambience, ambienceVolume, skyYaw, skySize, skyHeight, skyline: the district system's (never written here).
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/CanningBridge] built 7 chunks: " + string.Join("; ", K.Report));
        }

        static void BuildMurdoch()
        {
            const string tag = MurdochTag;
            string patterns = DataFolder + "/" + tag;
            K.Begin(MeshFolder(tag), PrefabFolder(tag), DataFolder, patterns);
            P.Init();

            Obstacle bollard = K.SaveObstacle(Pf(tag, "OBS_Murdoch_Bollard"), "OBS_Murdoch_Bollard", BollardMesh(K.R.yellow, K.R.ink), V(.5f, 1f, .5f), ObstacleKind.Thing, true, true, 1.2f);
            Obstacle sign = K.SaveObstacle(Pf(tag, "OBS_Murdoch_WarningSign"), "OBS_Murdoch_WarningSign", WarningSignMesh(K.R.yellow), V(1f, 1.7f, .7f), ObstacleKind.Thing, false, true, .8f);
            Obstacle sprinkler = K.SaveObstacle(Pf(tag, "OBS_Murdoch_Sprinkler"), "OBS_Murdoch_Sprinkler", SprinklerMesh(), V(1.4f, .8f, .6f), ObstacleKind.Thing, true, true, 1f);
            GameObject bobtail = SaveAnimal(Pf(tag, "ANM_Murdoch_Bobtail"), "ANM_Murdoch_Bobtail", BobtailMesh(), V(0, .16f, 0), V(.45f, .32f, .9f), .25f);
            Ramp berm = SaveBermRamp(Pf(tag, "RMP_Murdoch_SandBerm"), "RMP_Murdoch_SandBerm");

            K.WriteChunk(MuEntry(), ChunkPath(tag, "Entry"));
            K.WriteChunk(MuNoiseWall(), ChunkPath(tag, "NoiseWall"));
            K.WriteChunk(MuStation(), ChunkPath(tag, "Station"));
            K.WriteChunk(MuBushland(), ChunkPath(tag, "Bushland"));
            K.WriteChunk(MuHospital(), ChunkPath(tag, "Hospital"));
            K.WriteChunk(MuExit(), ChunkPath(tag, "Exit"));
            GameObject far = K.SaveFarScenery(Pf(tag, "FAR_Murdoch_Bush"), "FAR_Murdoch_Bush", MurdochFar());

            GameObject cyclist = Cyclist();
            var own = new List<HazardPattern>
            {
                K.Pattern(patterns, "HZ_Murdoch_Bollards", 40, 0, .8f, false, 30, (bollard.gameObject, -2.6f, 20f, 0f), (bollard.gameObject, 0f, 20f, 0f), (bollard.gameObject, 2.6f, 20f, 0f)),
                K.Pattern(patterns, "HZ_Murdoch_Sprinklers", 40, 1, .8f, true, 30, (sprinkler.gameObject, -3f, 18f, 0f), (sprinkler.gameObject, 0f, 22f, 0f), (sprinkler.gameObject, 3f, 26f, 0f)),
                K.Pattern(patterns, "HZ_Murdoch_Bobtail", 40, 0, .9f, true, 30, (bobtail, -1.4f, 22f, 0f), (bobtail, 1.6f, 27f, 0f)),
            };
            if (cyclist != null) own.Add(K.Pattern(patterns, "HZ_Murdoch_Cyclists", 50, 0, 1f, true, 30, (cyclist, -2.4f, 24f, 0f), (cyclist, -1.2f, 31f, 0f)));

            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme == null) throw new InvalidOperationException("No comic theme at " + ComicCityBuilder.ThemePath);
            District d = FillCommon(tag, MurdochId, "MURDOCH", 90, theme);
            var obstacles = new List<Obstacle> { bollard, sign, sprinkler };
            Obstacle cones = P.BaseObstacle("Cones"); if (cones != null) obstacles.Add(cones);
            d.obstacles = obstacles.ToArray();
            var hazards = new List<HazardPattern>(own);
            if (theme.hazards != null) hazards.AddRange(theme.hazards.Where(h => h != null && h.name == "HZ_SidewalkStroll"));
            d.hazards = hazards.ToArray();
            var people = new List<GameObject>(K.People(theme, "Jogger", "Jogger", "DogWalker", "DogWalker", "Student", "Student", "Elder"));
            if (cyclist != null) { people.Add(cyclist); people.Add(cyclist); }
            people.Add(bobtail);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { berm };
            d.rampFrequency = 1.1f;
            // Warm, dusty bushland light.
            d.fogColor = new Color(.86f, .85f, .78f);
            d.skyTint = new Color(1f, .99f, .95f);
            d.ambientSky = new Color(.68f, .80f, .92f);
            d.ambientEquator = new Color(.88f, .85f, .76f);
            d.ambientGround = new Color(.66f, .60f, .48f);
            d.sunColor = new Color(1f, .95f, .85f);
            d.sunIntensity = 1.45f;
            d.farScenery = far;
            // gateway, ambience, ambienceVolume, skyYaw, skySize, skyHeight, skyline: the district system's (never written here).
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/Murdoch] built 6 chunks: " + string.Join("; ", K.Report));
        }

        // ==================================================================== chunk drafting

        /// <summary>A chunk being built. Everything is drawn FLAT (heights relative to the path at that z) and sheared to
        /// the slope when the chunk is written: Road (no ink), Front (ink, shadows), Props (ink), Deep (no ink), Land
        /// (ink, no shadows). Long things are split into 4 m pieces so the city curl stays smooth.</summary>
        sealed class Ch
        {
            public readonly CqChunkDraft D;
            public readonly ComicMesh Road, Front, Props = new ComicMesh(), Deep = new ComicMesh(), Land = new ComicMesh();
            public readonly System.Random Rng;
            public Ch(string tag, string variant, float drop)
            {
                D = new CqChunkDraft(tag + "_" + variant, drop) { Type = RoadType.PedestrianMall };
                Road = D.Road; Front = D.SlopedFrontage;
                Rng = new System.Random(ComicKitGeometry.StableHash(tag + variant));
            }
            public float F(float a, float b) => Mathf.Lerp(a, b, (float)Rng.NextDouble());
            public int Seed() => Rng.Next(1, 100000);
            /// <summary>A simple collider for something standing on a verge (StreetDressing), at its flat position.</summary>
            public void Collide(string id, float x, float z, Vector3 size, bool jump) =>
                D.Collisions.Add(new DressingObstacle { id = id, position = V(x, D.Ground(z), z), rotation = Vector3.zero, centre = V(0, size.y * .5f, 0), size = size, canJumpOver = jump });
            public CqChunkDraft Done()
            {
                foreach ((ComicMesh to, ComicMesh from) in new[] { (D.Props, Props), (D.Deep, Deep), (D.Landmark, Land) })
                {
                    if (from.TriangleCount == 0) continue;
                    from.Shear(D.Grade);
                    to.Add(from, Vector3.zero, Quaternion.identity, Vector3.one);
                }
                return D;
            }
        }

        // ---- primitives (flat space)

        static void Plane(ComicMesh m, float xa, float xb, float z0, float z1, float y, Color c, ComicSurface s = ComicSurface.Plain, float step = 4f)
        {
            m.Surface = s;
            for (float z = z0; z < z1 - .001f; z += step)
            {
                float e = Mathf.Min(z1, z + step);
                K.Face(m, V(xa, y, z), V(xa, y, e), V(xb, y, e), V(xb, y, z), c, Vector3.up, false);
            }
        }

        static void ZBox(ComicMesh m, float xa, float xb, float ya, float yb, float z0, float z1, Color c, float step = 4f)
        {
            for (float z = z0; z < z1 - .001f; z += step) K.MinMaxBox(m, V(xa, ya, z), V(xb, yb, Mathf.Min(z1, z + step)), c);
        }

        static void Line(ComicMesh m, float xa, float za, float xb, float zb, float y, float w, Color c)
        {
            Vector3 a = V(xa, y, za), b = V(xb, y, zb), side = Vector3.Cross(Vector3.up, (b - a).normalized) * (w * .5f);
            m.Surface = ComicSurface.Plain;
            K.Face(m, a - side, b - side, b + side, a + side, c, Vector3.up, false);
        }

        static bool Skip(float z0, float z1, float s0, float s1) => s0 >= 0f && z1 > s0 + .001f && z0 < s1 - .001f;

        // ---- the shared path: path surface (asphalt, district colour), verges at kerb height, kerb cap, white dashed
        // centre line and bike symbols. skip0..skip1 leaves a hole (the water gap).

        static void PathSurface(Ch c, Color path, Color verge, float skip0 = -1f, float skip1 = -1f)
        {
            ComicMesh m = c.Road;
            float L = c.D.Length, h = K.Kerb, w = K.Half, sw = K.Side;
            var cuts = new List<float>();
            for (float z = 0; z < L - .01f; z += 4f) cuts.Add(z);
            cuts.Add(L);
            if (skip0 >= 0f) { cuts.Add(skip0); cuts.Add(skip1); }
            cuts = cuts.Distinct().OrderBy(z => z).ToList();
            Color joint = Color.Lerp(K.R.ink, verge, .55f);
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float z0 = cuts[i], z1 = cuts[i + 1];
                if (z1 - z0 < .01f || Skip(z0, z1, skip0, skip1)) continue;
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(-w, 0, z0), V(-w, 0, z1), V(w, 0, z1), V(w, 0, z0), path, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    float i0 = s * w, o0 = s * (w + sw);
                    Vector3 inward = V(-s, 0, 0), outward = V(s, 0, 0);
                    m.Surface = ComicSurface.Concrete;
                    K.Face(m, V(i0, h, z0), V(i0, h, z1), V(o0, h, z1), V(o0, h, z0), verge, Vector3.up, false);
                    K.Face(m, V(i0, -.05f, z0), V(i0, -.05f, z1), V(i0, h, z1), V(i0, h, z0), verge, inward, false);
                    K.Face(m, V(o0, -2f, z0), V(o0, -2f, z1), V(o0, h, z1), V(o0, h, z0), verge, outward, false);
                    float c0 = s * (w + .24f), top = h + .04f;
                    m.Surface = ComicSurface.Plain;
                    K.Face(m, V(i0, top, z0), V(i0, top, z1), V(c0, top, z1), V(c0, top, z0), K.R.cream, Vector3.up, false);
                    K.Face(m, V(i0, h, z0), V(i0, h, z1), V(i0, top, z1), V(i0, top, z0), K.R.cream, inward, false);
                    K.Face(m, V(c0, h, z0), V(c0, h, z1), V(c0, top, z1), V(c0, top, z0), K.R.cream, outward, false);
                    // white edge line on the path
                    K.Flat(m, s * (w - .3f), s * (w - .2f), s * (w - .3f), s * (w - .2f), z0, z1, .031f, LineWhite);
                }
                for (float z = Mathf.Ceil(z0 / 2f) * 2f; z < z1 - .01f; z += 2f)
                    for (int s = -1; s <= 1; s += 2) K.Flat(m, s * (w + .24f), s * (w + sw), s * (w + .24f), s * (w + sw), z - .015f, z + .015f, h + .02f, joint);
            }
            foreach (float ze in new[] { 0f, L })
            {
                if (Skip(ze - .5f, ze + .5f, skip0, skip1)) continue;
                Vector3 normal = ze <= 0f ? Vector3.back : Vector3.forward;
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(-w, -.8f, ze), V(w, -.8f, ze), V(w, 0, ze), V(-w, 0, ze), path, normal, false);
                m.Surface = ComicSurface.Concrete;
                for (int s = -1; s <= 1; s += 2) K.Face(m, V(s * w, -2f, ze), V(s * (w + sw), -2f, ze), V(s * (w + sw), h, ze), V(s * w, h, ze), verge, normal, false);
            }
            // white dashed centre line: 3 m dashes every 6 m
            for (float z = 1.5f; z < L - 3f + .01f; z += 6f)
                if (!Skip(z, z + 3f, skip0, skip1)) K.Flat(m, -.07f, .07f, -.07f, .07f, z, z + 3f, .034f, LineWhite);
            // bike symbols (painted upright for riders coming along +Z)
            foreach (Vector2 b in new[] { new Vector2(-2f, 13f), new Vector2(2f, 33f) })
                if (!Skip(b.y - 2f, b.y + 2f, skip0, skip1)) BikeSymbol(m, b.x, b.y, .036f, LineWhite);
        }

        /// <summary>The shared-path bike symbol, flat on the path, stretched along +Z like real road markings.</summary>
        static void BikeSymbol(ComicMesh m, float cx, float cz, float y, Color c)
        {
            const float k = 2.2f, rx = .42f, rz = .42f * k;
            foreach (float wx in new[] { -.58f, .58f })
                for (int i = 0; i < 10; i++)
                {
                    float a0 = i * Mathf.PI * .2f, a1 = (i + 1) * Mathf.PI * .2f;
                    Line(m, cx + wx + Mathf.Cos(a0) * rx, cz + Mathf.Sin(a0) * rz, cx + wx + Mathf.Cos(a1) * rx, cz + Mathf.Sin(a1) * rz, y, .07f, c);
                }
            Vector2 rear = new Vector2(-.58f, 0), front = new Vector2(.58f, 0), crank = new Vector2(-.05f, 0), seat = new Vector2(-.22f, .55f * k), head = new Vector2(.38f, .55f * k), bar = new Vector2(.46f, .72f * k);
            foreach ((Vector2 a, Vector2 b) in new[] { (rear, seat), (seat, crank), (crank, rear), (seat, head), (head, crank), (head, front), (head, bar), (seat + new Vector2(-.12f, .12f * k), seat + new Vector2(.14f, .12f * k)) })
                Line(m, cx + a.x, cz + a.y, cx + b.x, cz + b.y, y + .001f, .075f, c);
        }

        // ---- the freeway and the Mandurah line on the left (decoration behind the fence, out of reach)

        /// <summary>Left of the path: fence (or a calm hedge), dry verge, jersey barrier, carriageway, thrie beam, the
        /// railway corridor (ballast, two tracks, catenary), the other carriageway, far verge with trees, a few cars.
        /// bridge = all of it on a deck over the river (water under it); skip0..skip1 = the inlet crossing.</summary>
        static void Freeway(Ch c, float z0, float z1, bool bridge = false, float calmTo = 0f, float skip0 = -1f, float skip1 = -1f, float farVerge = FarVerge)
        {
            ComicMesh r = c.Road, f = c.Front;
            float dy = bridge ? .9f : 0f;
            // calmTo > 0: the first calmTo metres have a calm edge (CalmEnd) instead of the verge and fence; < 0: from -calmTo on.
            bool Calm(float z) => calmTo > 0f ? z < calmTo - .01f : calmTo < 0f && z >= -calmTo - .01f;
            if (bridge)
            {
                K.Water(r, -(K.Edge + .1f), -RiverFar, z0, z1, WaterY, Water, Ripple, c.Seed());
                // the deck slab and its fascia, piers into the river
                f.Surface = ComicSurface.Concrete;
                ZBox(f, -CwB1 - .4f, -(CwA0 - .8f), dy - 1.4f, dy - .02f, z0, z1, Concrete);
                ZBox(f, -(CwA0 - .8f) - .25f, -(CwA0 - .8f), dy - 1.5f, dy + .15f, z0, z1, CanopyWhite);
                foreach (float zp in new[] { z0 + 8f, z0 + 28f })
                    if (zp < z1)
                        foreach (float x in new[] { 14f, 22f, 30f, 38f, 46f })
                            c.Land.Cylinder(V(-x, (WaterY - .6f + dy - 1.4f) * .5f, zp), .8f, dy - 1.4f - WaterY + .6f, Concrete, 8);
            }
            else
            {
                // verge between the fence and the barrier
                for (float z = z0; z < z1 - .001f; z += 4f)
                {
                    float e = Mathf.Min(z1, z + 4f);
                    if (Skip(z, e, skip0, skip1) || Calm(z)) continue;
                    Plane(r, -K.Edge, -(CwA0 - .7f), z, e, .14f, DryGrass, ComicSurface.Foliage);
                }
            }
            // jersey barrier (wide foot, narrow top)
            f.Surface = ComicSurface.Concrete;
            ZBox(f, -(CwA0 - .1f), -(CwA0 - .8f), dy, dy + .35f, z0, z1, Barrier);
            ZBox(f, -(CwA0 - .25f), -(CwA0 - .65f), dy + .35f, dy + .9f, z0, z1, Barrier);
            // carriageway A (same way as the rider), median grass, railway corridor, carriageway B, far verge
            Carriageway(r, CwA0, CwA1, z0, z1, dy);
            Plane(r, -CwA1, -Rail0, z0, z1, dy + .1f, Color.Lerp(DryGrass, Lawn, .4f), ComicSurface.Foliage);
            f.Surface = ComicSurface.Metal;
            ZBox(f, -(CwA1 + .5f), -(CwA1 + .7f), dy + .4f, dy + .8f, z0, z1, Thrie);
            Railway(c, z0, z1, dy);
            Carriageway(r, Rail1, CwB1, z0, z1, dy);
            ZBox(f, -(Rail1 + .5f), -(Rail1 + .7f), dy + .4f, dy + .8f, z0, z1, Thrie);
            if (!bridge)
            {
                Plane(r, -CwB1, -farVerge, z0, z1, .12f, DryGrass, ComicSurface.Foliage);
                for (float z = z0 + c.F(1f, 5f); z < z1; z += c.F(7f, 10f))
                {
                    float x = -(CwB1 + c.F(5f, 14f));
                    P.RoundTree(c.Deep, V(x, .12f, z), c.F(6f, 9f), Color.Lerp(Peppermint, DryGrass, c.F(0f, .3f)), c.Seed());
                }
            }
            // a few cars (static: the freeway is decoration)
            Color[] carColours = { K.R.red, RailWhite, H("2F6FB5"), H("F2C14E"), H("4A5560") };
            int cars = 2 + c.Rng.Next(2);
            for (int i = 0; i < cars; i++)
            {
                bool a = i % 2 == 0;
                float lane = a ? CwA0 + 1.9f + 3.75f * c.Rng.Next(3) : Rail1 + 1.9f + 3.75f * c.Rng.Next(3);
                float z = Mathf.Lerp(z0 + 3f, z1 - 3f, (i + c.F(.1f, .9f)) / cars);
                c.Deep.Add(ComicKitGeometry.Car(K.R, carColours[c.Rng.Next(carColours.Length)]), V(-lane, dy + .06f, z), Quaternion.Euler(0, a ? 0f : 180f, 0), Vector3.one);
            }
            // the path fence (or a calm limestone wall and hedge where the district begins / ends)
            if (bridge) return;
            for (float z = z0; z < z1 - .001f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                if (Skip(z, e, skip0, skip1) || Calm(z)) continue;
                f.Surface = ComicSurface.PaintedMetal;
                f.Box(V(-FenceX, .2f + .95f, z + .1f), V(.08f, 1.9f, .08f), FenceInk);
                if (z + 2f < e) f.Box(V(-FenceX, .2f + .95f, z + 2f), V(.08f, 1.9f, .08f), FenceInk);
                foreach (float y in new[] { .35f, 1.1f, 2.05f })
                    K.MinMaxBox(f, V(-FenceX - .03f, y, z), V(-FenceX + .03f, y + .06f, e), FenceInk);
                // the chain mesh: thin diagonal wires (ink), reads as mesh from the path
                f.Surface = ComicSurface.Metal;
                for (float a = z; a < e - .01f; a += 1f)
                    f.Beam(V(-FenceX, .38f, a), V(-FenceX, 2.04f, Mathf.Min(e, a + 1f)), .025f, Color.Lerp(FenceInk, MastGrey, .4f));
            }
        }

        static void Carriageway(ComicMesh r, float a, float b, float z0, float z1, float dy)
        {
            Plane(r, -a, -b, z0, z1, dy + .06f, FwyAsphalt, ComicSurface.Asphalt);
            K.Flat(r, -(a + .35f), -(a + .5f), -(a + .35f), -(a + .5f), z0, z1, dy + .075f, LineWhite);
            K.Flat(r, -(b - .35f), -(b - .5f), -(b - .35f), -(b - .5f), z0, z1, dy + .075f, LineWhite);
            float lane = (b - a) / 3f;
            for (int k = 1; k < 3; k++)
                for (float z = z0 + 1f; z < z1 - 3f + .01f; z += 9f)
                    K.Flat(r, -(a + lane * k - .07f), -(a + lane * k + .07f), -(a + lane * k - .07f), -(a + lane * k + .07f), z, z + 3f, dy + .075f, LineWhite);
        }

        static void Railway(Ch c, float z0, float z1, float dy)
        {
            ComicMesh r = c.Road, f = c.Front;
            Plane(r, -Rail0, -Rail1, z0, z1, dy + .22f, Ballast, ComicSurface.Concrete);
            foreach (float t in Tracks)
            {
                for (float z = z0 + .45f; z < z1; z += .9f) K.Flat(r, -(t - 1.3f), -(t + 1.3f), -(t - 1.3f), -(t + 1.3f), z - .12f, z + .12f, dy + .26f, Sleeper);
                r.Surface = ComicSurface.Metal;
                foreach (float o in new[] { -.72f, .72f }) ZBox(r, -(t + o) - .045f, -(t + o) + .045f, dy + .26f, dy + .4f, z0, z1, RailSteel);
                // contact wire
                ZBox(r, -t - .025f, -t + .025f, dy + 5.6f, dy + 5.65f, z0, z1, K.R.ink);
            }
            f.Surface = ComicSurface.PaintedMetal;
            foreach (float zm in new[] { z0 + 6f, z0 + 26f })
            {
                if (zm >= z1) continue;
                foreach ((float x, float to) in new[] { (Rail0 + .2f, Tracks[0] + .3f), (Rail1 - .2f, Tracks[1] - .3f) })
                {
                    f.Box(V(-x, dy + 3.25f, zm), V(.32f, 6.5f, .32f), MastGrey);
                    f.Beam(V(-x, dy + 6.1f, zm), V(-to, dy + 6.1f, zm), .14f, MastGrey);
                    f.Beam(V(-to, dy + 6.1f, zm), V(-to, dy + 5.65f, zm), .06f, K.R.ink);
                }
            }
        }

        // ---- a Transperth-style train (no brand): white body, dark window band, green stripe, yellow cab front.
        static void Train(ComicMesh m, float x, float z0, float z1, float baseY, bool noseFront, bool noseBack)
        {
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f), zc = (z + e) * .5f, len = e - z;
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(x, baseY + 2.35f, zc), V(3f, 3.1f, len), TrainWhite);
                m.Box(V(x, baseY + 2.9f, zc), V(3.04f, .9f, len - .3f), TrainBand);
                m.Box(V(x, baseY + 1.35f, zc), V(3.04f, .25f, len), TrainGreen);
                m.Box(V(x, baseY + 3.98f, zc), V(2.6f, .2f, len), CanopyUnder);
                m.Surface = ComicSurface.Rubber;
                m.Box(V(x, baseY + .55f, zc), V(2.4f, .5f, len - .8f), K.R.ink);
            }
            m.Surface = ComicSurface.PaintedMetal;
            foreach ((float ze, bool on, float dir) in new[] { (z0, noseFront, -1f), (z1, noseBack, 1f) })
            {
                if (!on) continue;
                m.Box(V(x, baseY + 2.2f, ze + dir * .45f), V(2.9f, 2.8f, .9f), TrainNose);
                m.Box(V(x, baseY + 3.05f, ze + dir * .92f), V(2.4f, .8f, .06f), TrainBand);
                m.Box(V(x, baseY + 1.6f, ze + dir * .92f), V(2.2f, .18f, .05f), K.R.ink);
            }
        }

        // ---- island platform: red brick, yellow edges, grey tactile strip, canopy on posts, station name board.
        static void IslandPlatform(Ch c, float z0, float z1, float canopy0, float canopy1, string name)
        {
            ComicMesh f = c.Front;
            float xa = -PlatformA, xb = -PlatformB, xc = (xa + xb) * .5f;
            f.Surface = ComicSurface.Concrete;
            ZBox(f, xb, xa, 0f, PlatformY - .05f, z0, z1, Concrete);
            Plane(c.Road, xb, xa, z0, z1, PlatformY, Brick, ComicSurface.Paving);
            foreach (float x in new[] { xa - .35f, xb + .35f }) K.Flat(c.Road, x - .12f, x + .12f, x - .12f, x + .12f, z0, z1, PlatformY + .01f, K.R.yellow);
            K.Flat(c.Road, xc - .6f, xc + .6f, xc - .6f, xc + .6f, z0, z1, PlatformY + .01f, Tactile);
            if (canopy1 > canopy0)
            {
                f.Surface = ComicSurface.PaintedMetal;
                for (float z = canopy0 + 1f; z < canopy1; z += 6f) f.Box(V(xc, PlatformY + 2f, z), V(.24f, 4f, .24f), MastGrey);
                // a shallow butterfly roof: two slabs falling to the middle, white on top, grey underneath
                for (float z = canopy0; z < canopy1 - .01f; z += 4f)
                {
                    float e = Mathf.Min(canopy1, z + 4f);
                    foreach (float s in new[] { -1f, 1f })
                    {
                        float xo = xc + s * 3.9f, yi = PlatformY + 4f, yo = PlatformY + 4.45f;
                        K.Face(f, V(xc, yi + .12f, z), V(xc, yi + .12f, e), V(xo, yo + .12f, e), V(xo, yo + .12f, z), CanopyWhite, Vector3.up);
                        K.Face(f, V(xc, yi, z), V(xc, yi, e), V(xo, yo, e), V(xo, yo, z), CanopyUnder, Vector3.down);
                        K.Face(f, V(xo, yo, z), V(xo, yo, e), V(xo, yo + .12f, e), V(xo, yo + .12f, z), CanopyWhite, V(s, 0, 0));
                    }
                }
            }
            if (!string.IsNullOrEmpty(name))
            {
                // the station name board at the platform's near end (navy, white pixel letters), facing the rider
                float px = .09f, w = P.TextWidth(name, px) + .6f, zb = z0 + 1.5f;
                f.Surface = ComicSurface.PaintedMetal;
                foreach (float s in new[] { -1f, 1f }) f.Box(V(xc + s * (w * .5f - .2f), PlatformY + 1.4f, zb), V(.12f, 2.8f, .12f), K.R.ink);
                f.Box(V(xc, PlatformY + 2.55f, zb), V(w, .85f, .1f), SignNavy);
                P.Text(f, name, xc, PlatformY + 2.55f, zb - .06f, px, RailWhite);
            }
        }

        // ---- the right side: the Canning River

        static void RiverBank(Ch c, float z0, float z1)
        {
            Plane(c.Road, K.Edge, 10.5f, z0, z1, .14f, Lawn, ComicSurface.Foliage);
            P.Bank(c.Road, 1, 10.5f, .14f, 13.5f, WaterY + .1f, Rock, z0, z1, ComicSurface.Concrete);
            K.Water(c.Road, 12.5f, RiverFar, z0, z1, WaterY, Water, Ripple, c.Seed());
            for (float z = z0 + .8f; z < z1 - .5f; z += c.F(1.4f, 2.2f))
                P.Boulder(c.Props, V(c.F(10.8f, 13f), c.F(-.7f, -.1f), z), c.F(.7f, 1.15f), c.Rng);
            // boats further out
            for (int i = 0; i < 2; i++)
                MountsBayBuilder.Yacht(c.Deep, V(c.F(32f, 90f), WaterY, c.F(z0 + 4f, z1 - 4f)), c.F(.9f, 1.3f), i == 0 ? H("2F6FB5") : K.R.red);
        }

        /// <summary>A timber jetty out from the bank at zc, moored dinghies, a pelican on a post.</summary>
        static void Jetty(Ch c, float zc, float x0, float length, bool pelicans = true)
        {
            ComicMesh f = c.Front;
            float deck = .3f, x1 = x0 + length;
            f.Surface = ComicSurface.Wood;
            for (float x = x0; x < x1 - .01f; x += 4f) K.MinMaxBox(f, V(x, deck - .18f, zc - 1.2f), V(Mathf.Min(x1, x + 4f), deck, zc + 1.2f), Timber);
            for (float x = x0 + .5f; x < x1; x += .6f) K.Flat(f, x, x + .04f, x, x + .04f, zc - 1.2f, zc + 1.2f, deck + .005f, Color.Lerp(Timber, K.R.ink, .35f));
            for (float x = x0 + 1f; x < x1 + .01f; x += 2.6f)
                foreach (float s in new[] { -1f, 1f })
                {
                    f.Cylinder(V(x, (WaterY - .6f + deck + .7f) * .5f, zc + s * 1.25f), .14f, deck + .7f - WaterY + .6f, Color.Lerp(Timber, K.R.ink, .2f), 8);
                    f.Cylinder(V(x, deck + .74f, zc + s * 1.25f), .16f, .08f, K.R.cream, 8);
                }
            Dinghy(c.Props, V(x0 + length * .45f, WaterY + .05f, zc + 2.4f), 92f, H("2F6FB5"));
            Dinghy(c.Props, V(x0 + length * .8f, WaterY + .05f, zc - 2.5f), 86f, K.R.red);
            if (!pelicans) return;
            c.Props.Add(PelicanMesh(), V(x1 - .4f, deck + .78f, zc + 1.25f), Quaternion.Euler(0, -120f, 0), Vector3.one * .9f);
            c.Props.Add(PelicanMesh(), V(x0 + length * .55f, deck, zc - .4f), Quaternion.Euler(0, 160f, 0), Vector3.one);
        }

        static void Railing(ComicMesh m, int s, float z0, float z1, float x)
        {
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = z0; z <= z1 + .01f; z += 2.5f) m.Box(V(s * x, K.Kerb + .62f, z), V(.16f, 1.24f, .16f), RailWhite);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.MinMaxBox(m, V(s * x - .07f, K.Kerb + 1.1f, z), V(s * x + .07f, K.Kerb + 1.22f, e), RailWhite);
                K.MinMaxBox(m, V(s * x - .03f, K.Kerb + .62f, z), V(s * x + .03f, K.Kerb + .68f, e), RailWhite);
                m.Surface = ComicSurface.Metal;
                for (float a = z; a < e - .01f; a += .8f)
                    m.Beam(V(s * x, K.Kerb + .15f, a), V(s * x, K.Kerb + 1.08f, Mathf.Min(e, a + .8f)), .02f, H("9AA6B0"));
                m.Surface = ComicSurface.PaintedMetal;
            }
        }

        /// <summary>The calm end of an entry / exit chunk: low limestone wall at the verge, a hedge, lawn and round trees.</summary>
        static void CalmEnd(Ch c, int s, float z0, float z1, float lawnTo)
        {
            ComicMesh f = c.Front;
            f.Surface = ComicSurface.Concrete;
            K.SideBox(f, s, z0, z1, -1.2f, K.Kerb + .55f, .05f, .5f, Limestone);
            K.SideBox(f, s, z0, z1, K.Kerb + .55f, K.Kerb + .68f, -.02f, .57f, LimeCap);
            f.Surface = ComicSurface.Foliage;
            K.SideBox(f, s, z0 + .4f, z1 - .4f, K.Kerb + .3f, K.Kerb + 1.45f, .75f, 1.9f, Hedge);
            Plane(c.Road, s * (K.Edge + .5f), s * lawnTo, z0, z1, .16f, Lawn, ComicSurface.Foliage);
            for (float z = z0 + 2.5f; z < z1 - 1f; z += 6.5f)
                P.RoundTree(c.Props, V(s * (K.Edge + c.F(3.2f, 4.2f)), .16f, z), c.F(6.5f, 8f), Peppermint, c.Seed());
        }

        // ==================================================================== CANNING BRIDGE chunks

        static CqChunkDraft CbEntry()
        {
            var c = new Ch(CanningTag, "Entry", CanningDrop);
            PathSurface(c, PathGrey, VergeGrey);
            Freeway(c, 0, 40, calmTo: 16f);
            CalmEnd(c, -1, 0, 16, CwA0 - .7f);
            CalmEnd(c, 1, 0, 16, 22f);
            K.Water(c.Road, 22f, RiverFar, 0, 16, WaterY, Water, Ripple, 801);
            RiverBank(c, 16, 40);
            NorfolkPine(c.Props, V(9.6f, .14f, 26f), 14f, 3);
            P.NameBlock(c.Front, V(14.2f, .16f, 13.5f), "CANNING BRIDGE", .11f, Limestone, SignNavy);
            MountsBayBuilder.Lamp(c.Props, V(8.7f, .14f, 34f), -1);
            c.D.StandardSlots();
            return c.Done();
        }

        static CqChunkDraft CbExit()
        {
            var c = new Ch(CanningTag, "Exit", CanningDrop);
            PathSurface(c, PathGrey, VergeGrey);
            Freeway(c, 0, 40, calmTo: -24f);
            RiverBank(c, 0, 24);
            CalmEnd(c, -1, 24, 40, CwA0 - .7f);
            CalmEnd(c, 1, 24, 40, 22f);
            K.Water(c.Road, 22f, RiverFar, 24, 40, WaterY, Water, Ripple, 802);
            NorfolkPine(c.Props, V(9.6f, .14f, 12f), 15f, 5);
            MountsBayBuilder.Lamp(c.Props, V(8.7f, .14f, 4f), -1);
            c.D.StandardSlots();
            return c.Done();
        }

        static CqChunkDraft CbForeshore()
        {
            var c = new Ch(CanningTag, "Foreshore", CanningDrop);
            PathSurface(c, PathGrey, VergeGrey);
            Freeway(c, 0, 40);
            RiverBank(c, 0, 40);
            Jetty(c, 20f, 12.5f, 15f);
            NorfolkPine(c.Props, V(9.5f, .14f, 6f), 14f, 1);
            NorfolkPine(c.Props, V(9.7f, .14f, 33f), 16f, 2);
            MountsBayBuilder.Lamp(c.Props, V(8.7f, .14f, 14f), -1);
            Bench(c, 7.3f, 26f);
            for (int i = 0; i < 3; i++) c.Deep.Add(PelicanMesh(), V(c.F(18f, 40f), WaterY - .55f, c.F(4f, 36f)), Quaternion.Euler(0, c.F(0f, 360f), 0), Vector3.one * 1.1f);
            c.D.StandardSlots();
            return c.Done();
        }

        static CqChunkDraft CbStation()
        {
            var c = new Ch(CanningTag, "Station", CanningDrop);
            PathSurface(c, PathGrey, VergeGrey);
            Freeway(c, 0, 40);
            IslandPlatform(c, 0, 40, 2f, 38f, "CANNING BRIDGE");
            Train(c.Land, -Tracks[0], 3f, 38f, .36f, true, false);
            RiverBank(c, 0, 40);
            CanningHighwayBridge(c, 0, 40);
            Concourse(c, 24f);
            c.D.StandardSlots();
            return c.Done();
        }

        /// <summary>The station's glass concourse bridge over the freeway and the path, with a lift tower on the river side
        /// and stairs down to the island platform. Bottom 6.6 m above the path.</summary>
        static void Concourse(Ch c, float z)
        {
            ComicMesh m = c.Land;
            float y0 = 6.6f, y1 = 9.6f, za = z, zb = z + 4f, xa = -(CwB1 + 2f), xb = 13.5f;
            m.Surface = ComicSurface.Concrete;
            m.Box(V((xa + xb) * .5f, y0 - .3f, (za + zb) * .5f), V(xb - xa, .6f, 4.4f), CanopyWhite);
            m.Box(V((xa + xb) * .5f, y1 + .15f, (za + zb) * .5f), V(xb - xa, .3f, 4.6f), CanopyWhite);
            m.Surface = ComicSurface.Glass;
            m.Box(V((xa + xb) * .5f, (y0 + y1) * .5f, (za + zb) * .5f), V(xb - xa - .2f, y1 - y0, 3.8f), ConcourseGlass);
            m.Surface = ComicSurface.PaintedMetal;
            for (float x = xa + 1.5f; x < xb - .5f; x += 3f) m.Box(V(x, (y0 + y1) * .5f, za - .02f), V(.14f, y1 - y0, .1f), CanopyWhite);
            m.Box(V((xa + xb) * .5f, y0 + 1.1f, za - .05f), V(xb - xa, .12f, .1f), CanopyWhite);
            // columns (outside the path) and the towers
            m.Surface = ComicSurface.Concrete;
            foreach (float x in new[] { -(CwA0 - .45f), -(CwA1 + .6f), -(Rail1 + .6f), -(CwB1 + 1.2f) })
                m.Box(V(x, (y0 - .6f) * .5f, (za + zb) * .5f), V(.9f, y0 - .6f + .4f, .9f), Concrete);
            foreach (float x in new[] { 11.8f })
            {
                m.Surface = ComicSurface.Glass;
                m.Box(V(x + 1f, (y1 - 2.2f) * .5f, (za + zb) * .5f), V(3.2f, y1 + 2.2f, 4.4f), ConcourseGlass);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(x + 1f, y1 + .4f, (za + zb) * .5f), V(3.6f, .3f, 4.8f), CanopyWhite);
                for (float y = 1f; y < y1; y += 2.2f) m.Box(V(x + 1f, y, za - .25f), V(3.24f, .12f, .1f), CanopyWhite);
            }
            // stairs and lift down to the platform
            float px = -(PlatformA + PlatformB) * .5f;
            m.Surface = ComicSurface.Glass;
            m.Box(V(px, PlatformY + (y0 - PlatformY) * .5f, zb + 1.5f), V(2.6f, y0 - PlatformY, 3f), ConcourseGlass);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(px, y0 + .05f, zb + 1.5f), V(3f, .2f, 3.4f), CanopyWhite);
        }

        /// <summary>The long low Canning Highway bridge on the right, parallel to the path: deck on piers, cream post
        /// railings, lamps, cars.</summary>
        static void CanningHighwayBridge(Ch c, float z0, float z1)
        {
            ComicMesh m = c.Land;
            const float xa = 34f, xb = 46f, top = 1.2f;
            m.Surface = ComicSurface.Concrete;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f), zc = (z + e) * .5f;
                m.Box(V((xa + xb) * .5f, top - .45f, zc), V(xb - xa, .9f, e - z), Concrete);
                m.Surface = ComicSurface.Asphalt;
                m.Box(V((xa + xb) * .5f, top + .02f, zc), V(xb - xa - 3f, .06f, e - z), FwyAsphalt);
                m.Surface = ComicSurface.Concrete;
            }
            for (float z = z0 + 4f; z < z1; z += 8f)
                foreach (float x in new[] { xa + 2f, xb - 2f }) m.Box(V(x, (WaterY - .5f + top - .9f) * .5f, z), V(.9f, top - .9f - WaterY + .5f, .9f), ConcreteDark);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { xa + .15f, xb - .15f })
            {
                for (float z = z0 + 1f; z < z1; z += 2f) m.Box(V(x, top + .45f, z), V(.22f, .9f, .22f), K.R.cream);
                ZBox(m, x - .06f, x + .06f, top + .78f, top + .9f, z0, z1, RailWhite);
            }
            for (float z = z0 + 10f; z < z1; z += 20f) MountsBayBuilder.Lamp(m, V(xa + .2f, top, z), 1);
            c.Deep.Add(ComicKitGeometry.Car(K.R, K.R.yellow), V(xa + 3.5f, top + .05f, z0 + c.F(6f, 16f)), Quaternion.identity, Vector3.one);
            c.Deep.Add(ComicKitGeometry.Car(K.R, RailWhite), V(xb - 3.5f, top + .05f, z0 + c.F(22f, 34f)), Quaternion.Euler(0, 180f, 0), Vector3.one);
        }

        static CqChunkDraft CbBridge()
        {
            // The path crosses the river on its own bridge: railings both sides, water under and around it, the freeway
            // bridge with the railway on the left, the Canning Highway bridge on the right, expansion joints at both ends.
            var c = new Ch(CanningTag, "Bridge", CanningDrop);
            PathSurface(c, PathGrey, VergeGrey);
            Freeway(c, 0, 40, bridge: true);
            ComicMesh f = c.Front;
            f.Surface = ComicSurface.Concrete;
            foreach (int s in new[] { -1, 1 }) K.SideBox(f, s, 0, 40, -1.25f, K.Kerb - .01f, -.02f, .18f, CanopyWhite);
            K.Water(c.Road, K.Edge + .1f, RiverFar, 0, 40, WaterY, Water, Ripple, 803);
            foreach (int s in new[] { -1, 1 }) Railing(f, s, .5f, 39.5f, K.Edge + .05f);
            foreach (float z in new[] { 1.2f, 38.8f })
            {
                K.Flat(c.Road, -K.Half, K.Half, -K.Half, K.Half, z - .05f, z + .05f, .033f, H("8E949B"));
                foreach (int s in new[] { -1, 1 }) K.Flat(c.Road, s * (K.Half + .24f), s * K.Edge, s * (K.Half + .24f), s * K.Edge, z - .05f, z + .05f, K.Kerb + .022f, H("8E949B"));
            }
            for (float z = 10f; z < 40f; z += 20f) { MountsBayBuilder.Lamp(c.Props, V(8.35f, K.Kerb, z), -1); MountsBayBuilder.Lamp(c.Props, V(-8.35f, K.Kerb, z + 10f), 1); }
            // piers under the path bridge
            foreach (float z in new[] { 12f, 28f })
                foreach (float x in new[] { -5f, 5f }) c.Land.Box(V(x, (WaterY - .6f - 1.25f) * .5f, z), V(1f, -1.25f - WaterY + .6f, 1.4f), Concrete);
            CanningHighwayBridge(c, 0, 40);
            FreewaySign(c, 30f, "ROCKINGHAM");
            for (int i = 0; i < 2; i++) c.Deep.Add(PelicanMesh(), V(c.F(14f, 28f), WaterY - .55f, c.F(4f, 36f)), Quaternion.Euler(0, c.F(0f, 360f), 0), Vector3.one * 1.1f);
            for (int i = 0; i < 2; i++) MountsBayBuilder.Yacht(c.Deep, V(c.F(55f, 110f), WaterY, c.F(4f, 36f)), c.F(1f, 1.3f), i == 0 ? RailWhite : H("2F6FB5"));
            c.D.StandardSlots();
            return c.Done();
        }

        /// <summary>A green freeway direction sign on a gantry over carriageway A, facing the rider (place name only).</summary>
        static void FreewaySign(Ch c, float z, string text)
        {
            ComicMesh m = c.Land;
            float dy = .9f, y = dy + 6.6f, xa = -(CwA0 - .3f), xb = -(CwA1 + .3f), xc = (xa + xb) * .5f;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { xa, xb }) m.Box(V(x, (dy + y) * .5f, z), V(.35f, y - dy, .35f), MastGrey);
            m.Box(V(xc, y - .2f, z), V(xa - xb + .4f, .3f, .3f), MastGrey);
            float px = .17f, w = P.TextWidth(text, px) + 1.2f;
            m.Box(V(xc, y + .9f, z - .25f), V(w + .16f, 2.36f, .1f), RailWhite);
            m.Box(V(xc, y + .9f, z - .32f), V(w, 2.2f, .08f), SignGreen);
            P.Text(m, text, xc, y + 1.2f, z - .37f, px, RailWhite);
            // an arrow pointing straight on, under the name
            m.Box(V(xc, y + .3f, z - .37f), V(.16f, .5f, .02f), RailWhite);
            K.TriFacing(m, V(xc - .32f, y + .52f, z - .375f), V(xc + .32f, y + .52f, z - .375f), V(xc, y + .85f, z - .375f), RailWhite, Vector3.back);
        }

        static CqChunkDraft CbApplecross()
        {
            // The Applecross shore: lawn with jacarandas in purple bloom (November) and fallen petals, the art-deco
            // riverside hotel shape, apartment towers behind.
            var c = new Ch(CanningTag, "Applecross", CanningDrop);
            PathSurface(c, PathGrey, VergeGrey);
            Freeway(c, 0, 40);
            Plane(c.Road, K.Edge, 72f, 0, 40, .14f, Lawn, ComicSurface.Foliage);
            P.Bank(c.Road, 1, 72f, .14f, 76f, WaterY + .1f, Sand, 0, 40, ComicSurface.Plain);
            K.Water(c.Road, 75f, RiverFar, 0, 40, WaterY, Water, Ripple, 806);
            P.Patches(c.Road, c.Rng, 1, 9f, 30f, .14f, H("8DB45A"), H("6E9A45"), 0, 40, 6);
            ComicMesh f = c.Front;
            f.Surface = ComicSurface.Concrete;
            K.SideBox(f, 1, 0, 40, -.4f, K.Kerb + .25f, .05f, .4f, Limestone);
            foreach (Vector3 t in new[] { V(11.6f, 6f, 1f), V(12.6f, 20f, 1.1f), V(11.2f, 34f, .95f) })
            {
                c.Props.Add(Jacaranda(c.Seed(), t.z), V(t.x, .14f, t.y), Quaternion.Euler(0, c.F(0f, 360f), 0), Vector3.one);
                Petals(c, t.x, t.y, 3.4f, 14);
            }
            foreach (Vector3 t in new[] { V(20f, 12f, 1.1f), V(19f, 27f, 1f), V(24f, 38f, 1.15f) })
            {
                c.Deep.Add(Jacaranda(c.Seed(), t.z), V(t.x, .14f, t.y), Quaternion.Euler(0, c.F(0f, 360f), 0), Vector3.one);
                Petals(c, t.x, t.y, 3f, 8);
            }
            Bench(c, 7.3f, 13f);
            DecoHotel(c.Land, V(28f, .14f, 6f));
            RoundTower(c.Land, V(64f, .14f, 34f), 7.5f, 64f);
            SlabTower(c.Land, V(58f, .14f, 8f), 13f, 11f, 46f);
            MountsBayBuilder.Lamp(c.Props, V(8.7f, .14f, 28f), -1);
            c.D.StandardSlots();
            return c.Done();
        }

        static void Petals(Ch c, float x, float z, float r, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float a = c.F(0f, Mathf.PI * 2f), d = Mathf.Sqrt(c.F(0f, 1f)) * r, px = x + Mathf.Cos(a) * d, pz = z + Mathf.Sin(a) * d;
                if (px < K.Edge + .5f) continue;
                float s = c.F(.5f, 1.1f);
                K.Flat(c.Road, px - s, px + s, px - s, px + s, pz - s * .7f, pz + s * .7f, .16f + i * .001f, i % 2 == 0 ? J3 : J2);
            }
        }

        static CqChunkDraft CbInlet()
        {
            // A timber jetty kicker right up to the bank, then 6 m of open water across the whole path (SPLASH!), then
            // the path again. The ramp covers the whole rideable width, so the gap can always be cleared.
            var c = new Ch(CanningTag, "Inlet", CanningDrop);
            float z0 = JettyStart, lip = z0 + JettyRise + JettyTop, far = lip + GapLength;
            PathSurface(c, PathGrey, VergeGrey, lip, far);
            Freeway(c, 0, 40, skip0: lip, skip1: far);
            K.Water(c.Road, -(CwA0 - .7f), RiverFar, lip, far, WaterY, Water, Ripple, 804);
            ComicMesh f = c.Front;
            // the culvert under the freeway and the bank walls either side of the inlet
            f.Surface = ComicSurface.Concrete;
            K.MinMaxBox(f, V(-(CwA0 - .5f), WaterY - 1f, lip - .4f), V(-(CwA0 - .9f), .2f, far + .4f), Limestone);
            K.MinMaxBox(f, V(-(CwA0 - .93f), WaterY + .05f, lip + .8f), V(-(CwA0 - .89f), -.35f, far - .8f), K.R.ink);
            foreach ((float z, float t) in new[] { (lip, -.3f), (far, .3f) })
            {
                K.MinMaxBox(f, V(-(CwA0 - .7f), WaterY - 1f, z), V(12.5f, -.03f, z + t), Limestone);
                K.MinMaxBox(f, V(-(CwA0 - .7f), -.03f, z), V(-K.Half, K.Kerb - .01f, z + t), LimeCap);
                K.MinMaxBox(f, V(K.Half, -.03f, z), V(12.5f, K.Kerb - .01f, z + t), LimeCap);
            }
            RiverBank(c, 0, lip);
            RiverBank(c, far, 40);
            var ramp = new CqRampSpec { Name = "JettyRamp", X = 0f, Z = z0, Width = 2f * K.Edge, Rise = JettyRise, Height = JettyHeight, Top = JettyTop, Launch = 1f, Hang = .5f };
            c.D.Ramps.Add(ramp);
            c.D.Gaps.Add(new CqGapSpec { Name = "InletWater", Z = lip, Length = GapLength, Width = 0f, Word = "SPLASH", Water = true });
            var deck = new ComicMesh();
            K.RampMesh(deck, ramp, Timber, Color.Lerp(Timber, K.R.ink, .3f), true, WaterY - .3f);
            Color board = Color.Lerp(Timber, K.R.ink, .25f);
            for (float t = .1f; t < 1f; t += .1f)
            {
                float z = z0 + JettyRise * t, y = JettyHeight * t + .012f;
                K.Face(deck, V(-K.Edge, y, z), V(-K.Edge, y, z + .05f), V(K.Edge, y, z + .05f), V(K.Edge, y, z), board, Vector3.up, false);
            }
            for (float z = z0 + JettyRise + .5f; z < lip - .5f; z += .8f)
                K.Face(deck, V(-K.Edge, JettyHeight + .012f, z), V(-K.Edge, JettyHeight + .012f, z + .05f), V(K.Edge, JettyHeight + .012f, z + .05f), V(K.Edge, JettyHeight + .012f, z), board, Vector3.up, false);
            deck.Surface = ComicSurface.PaintedMetal;
            for (int s = -1; s <= 1; s += 2)
            {
                deck.Box(V(s * (K.Edge + .15f), JettyHeight + .6f, lip - .3f), V(.25f, 1.2f + JettyHeight, .25f), RailWhite);
                deck.Box(V(s * (K.Edge + .15f), .6f, z0 + .2f), V(.25f, 1.2f, .25f), RailWhite);
                deck.Beam(V(s * (K.Edge + .15f), 1.15f, z0 + .2f), V(s * (K.Edge + .15f), JettyHeight + 1.15f, z0 + JettyRise), .12f, RailWhite);
                deck.Beam(V(s * (K.Edge + .15f), JettyHeight + 1.15f, z0 + JettyRise), V(s * (K.Edge + .15f), JettyHeight + 1.15f, lip - .3f), .12f, RailWhite);
            }
            f.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            Dinghy(c.Props, V(14f, WaterY + .05f, (lip + far) * .5f), 100f, K.R.yellow);
            c.Props.Add(PelicanMesh(), V(K.Edge + .15f, JettyHeight + 1.2f + .05f, lip - .3f), Quaternion.Euler(0, 200f, 0), Vector3.one * .85f);
            Jetty(c, 33f, 12.5f, 12f, false);
            float KeepOutTop = JettyHeight + 4f;
            foreach (int s in new[] { -1, 1 })
                c.D.KeepOut.Add(new Bounds(V(s * (K.Half + K.Side * .5f + .3f), c.D.Y((z0 + far) * .5f) + KeepOutTop * .5f - 1f, (z0 + far) * .5f), V(K.Side + .6f, KeepOutTop + 2f, far - z0)));
            c.D.GiftSlots.Add(new Vector2(0f, 28f));
            c.D.Slot(-2.6f, 36f); c.D.Slot(2.6f, 36f);
            return c.Done();
        }

        // ==================================================================== MURDOCH chunks

        static CqChunkDraft MuEntry()
        {
            var c = new Ch(MurdochTag, "Entry", MurdochDrop);
            PathSurface(c, PathRed, VergeWarm);
            Freeway(c, 0, 40, calmTo: 16f);
            CalmEnd(c, -1, 0, 16, CwA0 - .7f);
            CalmEnd(c, 1, 0, 16, 75f);
            NoiseWall(c, 16, 40);
            P.NameBlock(c.Front, V(13.5f, .16f, 13.5f), "MURDOCH", .14f, Limestone, H("2F6E3A"));
            c.D.StandardSlots();
            return c.Done();
        }

        static CqChunkDraft MuExit()
        {
            var c = new Ch(MurdochTag, "Exit", MurdochDrop);
            PathSurface(c, PathRed, VergeWarm);
            Freeway(c, 0, 40, calmTo: -24f);
            Bush(c, 0, 24, false);
            CalmEnd(c, -1, 24, 40, CwA0 - .7f);
            CalmEnd(c, 1, 24, 40, 75f);
            c.D.StandardSlots();
            return c.Done();
        }

        static CqChunkDraft MuNoiseWall()
        {
            var c = new Ch(MurdochTag, "NoiseWall", MurdochDrop);
            PathSurface(c, PathRed, VergeWarm);
            Freeway(c, 0, 40);
            NoiseWall(c, 0, 40);
            c.D.StandardSlots();
            return c.Done();
        }

        /// <summary>Patterned freeway noise wall on the right: coloured 4 m panels between steel posts, each with a raised
        /// motif (banksia cone, diamonds, dunes, sun), a sandy strip with kangaroo paw in front, roofs and trees behind.</summary>
        static void NoiseWall(Ch c, float z0, float z1)
        {
            ComicMesh f = c.Front;
            const float x0 = 9.3f, x1 = 9.7f, top = 4.6f;
            Plane(c.Road, K.Edge, x0, z0, z1, .12f, Sand, ComicSurface.Plain);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f), zc = (z + e) * .5f;
                int k = Mathf.Abs(Mathf.RoundToInt(z / 4f) + 7) % 4;
                Color baseCol = WallColours[k], motif = WallColours[(k + 2) % 4];
                f.Surface = ComicSurface.Concrete;
                f.Box(V((x0 + x1) * .5f, (top - .6f) * .5f, zc), V(x1 - x0, top + .6f, e - z - .05f), baseCol);
                f.Box(V((x0 + x1) * .5f, top + .1f, zc), V(x1 - x0 + .2f, .22f, e - z), LimeCap);
                f.Surface = ComicSurface.PaintedMetal;
                f.Box(V((x0 + x1) * .5f, (top + .3f) * .5f, z), V(.32f, top + .3f, .3f), H("7D8389"));
                float fx = x0 - .06f;
                f.Surface = ComicSurface.Concrete;
                switch (Mathf.Abs(Mathf.RoundToInt(z / 4f)) % 4)
                {
                    case 0: // banksia cone with leaves
                        f.Box(V(fx, 2.3f, zc), V(.14f, 1.9f, .8f), H("D9A441"));
                        for (int i = 0; i < 4; i++) f.Box(V(fx - .04f, 1.6f + i * .45f, zc), V(.1f, .14f, .86f), H("A8662E"));
                        foreach (float s in new[] { -1f, 1f }) f.Box(V(fx, 1.5f, zc + s * .75f), V(.12f, 1.4f, .32f), H("4F7A4A"), Quaternion.Euler(s * 35f, 0, 0));
                        break;
                    case 1: // stacked diamonds
                        for (int i = 0; i < 3; i++) f.Box(V(fx, 1.2f + i * 1.15f, zc), V(.12f, .75f, .75f), i % 2 == 0 ? motif : LimeCap, Quaternion.Euler(45f, 0, 0));
                        break;
                    case 2: // dunes: stepped bars
                        for (int i = 0; i < 3; i++) f.Box(V(fx, 1.1f + i * .9f, zc + (i - 1) * .3f), V(.12f, .35f, 3.2f - i * .9f), i % 2 == 0 ? motif : H("4F7A4A"));
                        break;
                    default: // sun disc and ring
                        f.Cylinder(V(fx, 2.6f, zc), 1.15f, .1f, LimeCap, 12, 1f, Quaternion.Euler(0, 0, 90f));
                        f.Cylinder(V(fx - .06f, 2.6f, zc), .7f, .1f, motif, 12, 1f, Quaternion.Euler(0, 0, 90f));
                        break;
                }
                // kangaroo paw and low shrubs in the sandy strip
                if (Mathf.RoundToInt(z) % 8 == 0) P.KangarooPaw(c.Props, V(8.75f, .12f, zc), .8f, c.Seed());
                else P.Shrub(c.Props, V(8.8f, .12f, zc + 1f), .55f, H("6E8A45"));
            }
            for (float z = z0 + 8f; z < z1; z += 20f) MountsBayBuilder.Lamp(c.Props, V(8.6f, .12f, z), -1);
            // behind the wall: roofs and trees of the suburb
            Plane(c.Road, x1, 70f, z0, z1, .4f, Color.Lerp(Lawn, DryGrass, .5f), ComicSurface.Foliage);
            for (float z = z0 + c.F(2f, 6f); z < z1; z += c.F(11f, 15f))
            {
                float x = c.F(15f, 22f);
                HouseRoof(c.Deep, V(x, .4f, z), c.F(10f, 13f), c.F(8f, 10f), c.Rng.Next(3) == 0 ? Colorbond : TileRed);
                P.RoundTree(c.Deep, V(x + c.F(8f, 12f), .4f, z + c.F(3f, 6f)), c.F(8f, 11f), Peppermint, c.Seed());
            }
            for (float z = z0 + 5f; z < z1; z += 13f) P.Gum(c.Deep, V(c.F(28f, 40f), .4f, z), c.F(13f, 17f), c.Seed(), true);
        }

        static void HouseRoof(ComicMesh m, Vector3 p, float w, float d, Color roof)
        {
            m.Surface = ComicSurface.Plaster;
            m.Box(p + V(0, 1.5f, 0), V(w, 3f, d), H("E8DCC4"));
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(p + V(0, 3.9f, 0), Mathf.Max(w, d) * .72f, 1.8f, roof, 4, .2f, Quaternion.Euler(0, 45f, 0));
        }

        static CqChunkDraft MuStation()
        {
            // Murdoch station: the island platform in the freeway median, a train, and the silver bus-interchange hall on
            // its bridge right across the freeway and the path (the rider rides under it).
            var c = new Ch(MurdochTag, "Station", MurdochDrop);
            PathSurface(c, PathRed, VergeWarm);
            Freeway(c, 0, 40);
            IslandPlatform(c, 0, 40, 33f, 40f, "MURDOCH");
            Train(c.Land, -Tracks[1], 6f, 40f, .36f, true, false);
            BusHall(c, 13f, 31f);
            // the right side: the bus ramp embankment, a bike shelter, the station name block
            Plane(c.Road, K.Edge, 12f, 0, 40, .12f, Sand);
            P.Bank(c.Road, 1, 12f, .12f, 24f, 7.2f, Color.Lerp(Lawn, DryGrass, .45f), 0, 40);
            Plane(c.Road, 24f, 80f, 0, 40, 7.2f, Color.Lerp(Lawn, DryGrass, .3f), ComicSurface.Foliage);
            BikeShelter(c.Front, V(10.4f, .12f, 5f));
            P.NameBlock(c.Front, V(10.9f, .12f, 37.5f), "MURDOCH", .12f, Limestone, H("2F6E3A"));
            for (float z = 3f; z < 40f; z += 9f) P.Shrub(c.Props, V(c.F(13f, 17f), 2.2f, z), c.F(.9f, 1.3f), H("6E8A45"));
            c.D.StandardSlots();
            return c.Done();
        }

        /// <summary>The bus interchange: a deck across the whole freeway at 7 m on columns, and on it the long silver hall
        /// with a window band and a curved roof, MURDOCH in big letters on its face.</summary>
        static void BusHall(Ch c, float za, float zb)
        {
            ComicMesh m = c.Land;
            float xa = -(CwB1 + 4f), xb = 24f, y0 = 7f, deck = 1.2f, wall = 4.6f, zc = (za + zb) * .5f, depth = zb - za;
            m.Surface = ComicSurface.Concrete;
            m.Box(V((xa + xb) * .5f, y0 + deck * .5f, zc), V(xb - xa, deck, depth + 1f), Concrete);
            m.Box(V((xa + xb) * .5f, y0 + .1f, zc), V(xb - xa - .2f, .2f, depth + .6f), ConcreteDark);
            // columns outside the path and on the medians
            foreach (float x in new[] { -(K.Edge + 1.2f), -(CwA1 + .6f), -(Rail1 + .6f), -(CwB1 + 2f), K.Edge + 1.6f })
                foreach (float z in new[] { za + 2f, zb - 2f })
                    m.Box(V(x, y0 * .5f, z), V(1.1f, y0, 1.1f), Concrete);
            // the hall: corrugated silver walls, a dark window band, white rim
            float yw = y0 + deck;
            m.Surface = ComicSurface.Metal;
            m.Box(V((xa + xb) * .5f, yw + wall * .5f, zc), V(xb - xa - 8f, wall, depth - 2f), Silver);
            for (float x = xa + 4.5f; x < xb - 4.5f; x += 1.2f) m.Box(V(x, yw + wall * .5f, za + 1f - .03f), V(.12f, wall, .06f), SilverDark);
            m.Surface = ComicSurface.Glass;
            m.Box(V((xa + xb) * .5f, yw + 1.9f, zc), V(xb - xa - 7.9f, 1.5f, depth - 1.9f), DarkGlass);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V((xa + xb) * .5f, yw + wall + .1f, zc), V(xb - xa - 7.6f, .2f, depth - 1.6f), CanopyWhite);
            // curved roof: a flattened half cylinder along x, in 8 m pieces
            for (float x = xa + 4f; x < xb - 4f - .01f; x += 8f)
            {
                var roof = new ComicMesh { Surface = ComicSurface.Metal };
                roof.Cylinder(Vector3.zero, (depth - 2f) * .5f, Mathf.Min(8f, xb - 4f - x), Silver, 14, 1f, Quaternion.Euler(0, 0, 90f));
                m.Add(roof, V(x + Mathf.Min(8f, xb - 4f - x) * .5f, yw + wall + .2f, zc), Quaternion.identity, V(1f, .4f, 1f));
            }
            // MURDOCH on the face towards the rider
            P.Text(m, "MURDOCH", -18f, yw + 3.5f, za + .93f, .32f, TrainBand);
            // canopy edge (the bus stands) over the right end
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(xb - 2f, yw + 3.6f, zc), V(5f, .25f, depth + 3f), CanopyWhite);
            foreach (float z in new[] { za - .8f, zb + .8f }) m.Box(V(xb - .2f, yw + 1.8f, z), V(.2f, 3.6f, .2f), MastGrey);
            // stairs / lifts down to the platform
            float px = -(PlatformA + PlatformB) * .5f;
            m.Surface = ComicSurface.Glass;
            m.Box(V(px, PlatformY + (y0 - PlatformY) * .5f, zc), V(2.8f, y0 - PlatformY, 4f), ConcourseGlass);
            // a bus on the deck
            Bus(m, V(9f, yw, zc - 3f));
        }

        static void Bus(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 1.75f, 0), V(12f, 2.9f, 2.5f), RailWhite);
            m.Box(p + V(0, 2.2f, 0), V(11.6f, 1.1f, 2.54f), TrainBand);
            m.Box(p + V(0, .75f, 0), V(12.04f, .3f, 2.54f), H("C73A2E"));
            m.Surface = ComicSurface.Rubber;
            foreach (float x in new[] { -3.8f, 3.6f }) foreach (float s in new[] { -1f, 1f }) m.Cylinder(p + V(x, .45f, s * 1.15f), .45f, .3f, K.R.ink, 8, 1f, Quaternion.Euler(90f, 0, 0));
        }

        static void BikeShelter(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { -2.2f, 2.2f }) foreach (float x in new[] { -.8f, .8f }) m.Box(p + V(x, 1.2f, z), V(.12f, 2.4f, .12f), MastGrey);
            m.Box(p + V(0, 2.45f, 0), V(2.2f, .12f, 5f), H("2E8B57"));
            m.Surface = ComicSurface.Metal;
            for (float z = -1.8f; z <= 1.81f; z += .9f)
            {
                m.Box(p + V(0, .45f, z), V(1.3f, .08f, .08f), MastGrey);
                m.Surface = ComicSurface.Rubber;
                m.Cylinder(p + V(-.35f, .35f, z + .2f), .32f, .06f, K.R.ink, 8, 1f, Quaternion.Euler(0, 0, 90f));
                m.Cylinder(p + V(.35f, .35f, z + .2f), .32f, .06f, K.R.ink, 8, 1f, Quaternion.Euler(0, 0, 90f));
                m.Surface = ComicSurface.PaintedMetal;
                m.Beam(p + V(-.35f, .35f, z + .2f), p + V(.1f, .75f, z + .2f), .05f, z > 0 ? K.R.red : H("2F6FB5"));
                m.Beam(p + V(.1f, .75f, z + .2f), p + V(.35f, .35f, z + .2f), .05f, z > 0 ? K.R.red : H("2F6FB5"));
                m.Surface = ComicSurface.Metal;
            }
        }

        static CqChunkDraft MuBushland()
        {
            // Murdoch University's banksia woodland: white sand, balga, banksias, gums; a path hump over the path itself.
            var c = new Ch(MurdochTag, "Bushland", MurdochDrop);
            PathSurface(c, PathRed, VergeWarm);
            Freeway(c, 0, 40);
            Bush(c, 0, 40, true);
            float z0 = HumpStart, z1 = z0 + HumpRise, z2 = z1 + HumpTop, z3 = z2 + HumpDown;
            c.D.Ramps.Add(new CqRampSpec { Name = "HumpUp", X = 0f, Z = z0, Width = HumpWidth, Rise = HumpRise, Height = HumpHeight, Top = HumpTop, Launch = 1f, Hang = .5f });
            c.D.Ramps.Add(new CqRampSpec { Name = "HumpDown", X = 0f, Z = z3, Width = HumpWidth, Rise = HumpDown, Height = HumpHeight, Top = 0f, Launch = 1f, Hang = .5f, Yaw = 180f });
            HumpDeck(c.Front, z0, z1, z2, z3);
            c.D.Slot(-2.6f, 3f); c.D.Slot(2.6f, 3f);
            foreach (float x in new[] { -2.6f, 0f, 2.6f }) c.D.Slot(x, 34f);
            return c.Done();
        }

        static float Hump(float z, float z0, float z1, float z2, float z3) =>
            z <= z0 || z >= z3 ? 0f : z < z1 ? HumpHeight * (z - z0) / (z1 - z0) : z <= z2 ? HumpHeight : HumpHeight * (1f - (z - z2) / (z3 - z2));

        /// <summary>The hump over the path: red asphalt deck, sandy concrete sides, yellow chevrons up the slope and a
        /// yellow crest line.</summary>
        static void HumpDeck(ComicMesh m, float z0, float z1, float z2, float z3)
        {
            float w = HumpWidth * .5f;
            var cuts = new List<float> { z0, (z0 + z1) * .5f, z1, (z1 + z2) * .5f, z2, (z2 + z3) * .5f, z3 };
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float a = cuts[i], b = cuts[i + 1], ha = Hump(a, z0, z1, z2, z3) + .01f, hb = Hump(b, z0, z1, z2, z3) + .01f;
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(-w, ha, a), V(-w, hb, b), V(w, hb, b), V(w, ha, a), PathRed, Vector3.up);
                m.Surface = ComicSurface.Concrete;
                foreach (int s in new[] { -1, 1 }) K.Face(m, V(s * w, -.05f, a), V(s * w, -.05f, b), V(s * w, hb, b), V(s * w, ha, a), SandDark, V(s, 0, 0));
            }
            m.Surface = ComicSurface.Plain;
            for (float x = -3f; x <= 3.01f; x += 2f)
                for (int k = 0; k < 2; k++)
                {
                    float z = z0 + 1.6f + k * 2.2f, y = HumpHeight * (z - z0) / (z1 - z0) + .03f;
                    m.Tri(V(x - .6f, y, z), V(x, y + HumpHeight * .8f / HumpRise, z + .8f), V(x + .6f, y, z), K.R.yellow);
                }
            for (float x = -w; x < w - .01f; x += 1f)
                K.Face(m, V(x, HumpHeight + .02f, z2 - .35f), V(x, HumpHeight + .02f, z2), V(x + .5f, HumpHeight + .02f, z2), V(x + .5f, HumpHeight + .02f, z2 - .35f), K.R.yellow, Vector3.up, false);
            K.Flat(m, -.07f, .07f, -.07f, .07f, z1 + .5f, z2 - .8f, HumpHeight + .021f, LineWhite);
        }

        /// <summary>Banksia woodland on the right: white sand with leaf litter, a low pine-log fence, balga, banksias,
        /// kangaroo paw, gums and bush beyond.</summary>
        static void Bush(Ch c, float z0, float z1, bool dense)
        {
            Plane(c.Road, K.Edge, 90f, z0, z1, .1f, Sand);
            P.Patches(c.Road, c.Rng, 1, 9.5f, 40f, .1f, SandDark, Litter, z0, z1, dense ? 9 : 5);
            ComicMesh f = c.Front;
            f.Surface = ComicSurface.Wood;
            for (float z = z0 + 1f; z < z1; z += 3f) f.Cylinder(V(8.7f, .45f, z), .1f, .7f, H("8A6A48"), 8);
            for (float z = z0 + 1f; z < z1 - 3f + .01f; z += 3f) f.Beam(V(8.7f, .72f, z), V(8.7f, .72f, z + 3f), .12f, H("9C7B55"));
            float span = z1 - z0;
            int near = Mathf.Max(2, Mathf.RoundToInt(span / (dense ? 7f : 11f)));
            for (int i = 0; i < near; i++)
            {
                float z = z0 + (i + c.F(.2f, .8f)) * span / near, x = c.F(10.2f, 14f);
                switch (i % 3)
                {
                    case 0: P.Balga(c.Props, V(x, .1f, z), c.F(1f, 1.3f), c.Seed()); break;
                    case 1: P.Banksia(c.Props, V(x, .1f, z), c.F(.9f, 1.2f), c.Seed()); break;
                    default: P.KangarooPaw(c.Props, V(x - 1f, .1f, z), c.F(.8f, 1f), c.Seed()); P.Shrub(c.Props, V(x + 1.5f, .1f, z + 1f), .8f, H("6E8A45")); break;
                }
            }
            for (float z = z0 + c.F(1f, 4f); z < z1; z += c.F(4f, 6f))
            {
                float x = c.F(17f, 34f);
                if (c.Rng.Next(3) == 0) P.Gum(c.Deep, V(x, .1f, z), c.F(12f, 17f), c.Seed(), true);
                else P.Plant(c.Deep, V(x, .1f + 1.1f, z), c.F(2.6f, 3.6f), Color.Lerp(P.BanksiaLeaf, P.GumLeaf, c.F(0f, 1f)), P.GumLeafLight);
                if (dense && c.Rng.Next(2) == 0) P.Balga(c.Deep, V(c.F(15f, 22f), .1f, z + 2f), c.F(1f, 1.4f), c.Seed());
            }
            for (float z = z0 + 3f; z < z1; z += 10f) P.Gum(c.Deep, V(c.F(38f, 55f), .1f, z), c.F(15f, 19f), c.Seed(), true);
        }

        static CqChunkDraft MuHospital()
        {
            // Fiona Stanley Hospital beyond the freeway: long white ward blocks with coloured fins, the tall block with the
            // triangulated facade and the rooftop helipad with a red rescue helicopter, the white flue stack.
            var c = new Ch(MurdochTag, "Hospital", MurdochDrop);
            PathSurface(c, PathRed, VergeWarm);
            Freeway(c, 0, 40, farVerge: 150f);
            Bush(c, 0, 40, false);
            Hospital(c.Land, -(CwB1 + 28f), 0f);
            c.D.StandardSlots();
            return c.Done();
        }

        static void Hospital(ComicMesh m, float fx, float z0)
        {
            // Block A: the ward block facing the freeway (facade at x = fx), in 8 m pieces along z
            const float hA = 24f, dA = 18f;
            for (float z = z0 - 8f; z < z0 + 48f - .01f; z += 8f)
            {
                m.Surface = ComicSurface.Plaster;
                m.Box(V(fx - dA * .5f, hA * .5f, z + 4f), V(dA, hA, 8f), FshWhite);
                m.Surface = ComicSurface.Glass;
                for (float y = 3.2f; y < hA - 1f; y += 4f) m.Box(V(fx + .02f, y + 1.2f, z + 4f), V(.1f, 1.8f, 7.6f), DarkGlass);
                m.Surface = ComicSurface.PaintedMetal;
                for (float a = .5f; a < 8f; a += 2f)
                {
                    int k = Mathf.Abs(Mathf.RoundToInt((z + a) / 2f)) % FinColours.Length;
                    m.Box(V(fx + .45f, hA * .5f + 1f, z + a), V(.9f, hA - 4f, .32f), FinColours[k]);
                }
                m.Box(V(fx - dA * .5f, hA + .2f, z + 4f), V(dA + .2f, .4f, 8.05f), FshGrey);
            }
            // Block B: the tall block behind with the triangulated (diamond) facade and the helipad
            const float hB = 38f;
            float bx0 = fx - dA - 4f, bx1 = bx0 - 26f, bz0 = z0 + 6f, bz1 = z0 + 34f;
            m.Surface = ComicSurface.Plaster;
            for (float z = bz0; z < bz1 - .01f; z += 7f) m.Box(V((bx0 + bx1) * .5f, hB * .5f, z + 3.5f), V(bx0 - bx1, hB, 7f), FshGrey);
            m.Surface = ComicSurface.PaintedMetal;
            for (int row = 0; row < 7; row++)
                for (float z = bz0 + 2f; z < bz1 - 1f; z += 3.5f)
                {
                    float y = hA + 2f + row * 2f;
                    if (y > hB - 1.2f) continue;
                    Color col = (row + Mathf.RoundToInt(z)) % 2 == 0 ? FshWhite : H("9EA6AE");
                    m.Box(V(bx0 + .05f, y, z + (row % 2) * 1.75f), V(.12f, 1.6f, 1.6f), col, Quaternion.Euler(45f, 0, 0));
                }
            for (int row = 0; row < 7; row++)
                for (float x = bx1 + 2f; x < bx0 - 1f; x += 3.5f)
                {
                    float y = 6f + row * 4.5f;
                    m.Box(V(x + (row % 2) * 1.75f, y, bz0 - .05f), V(2.4f, 2.4f, .12f), row % 2 == 0 ? FshWhite : H("9EA6AE"), Quaternion.Euler(0, 0, 45f));
                }
            // helipad with a red rescue helicopter
            Vector3 pad = V((bx0 + bx1) * .5f, hB, (bz0 + bz1) * .5f);
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(pad + V(0, .25f, 0), 9f, .5f, H("4A5058"), 12);
            m.Cylinder(pad + V(0, .52f, 0), 7.2f, .05f, K.R.yellow, 12);
            m.Cylinder(pad + V(0, .55f, 0), 6.6f, .05f, H("4A5058"), 12);
            m.Surface = ComicSurface.Plain;
            m.Box(pad + V(-1.4f, .6f, 0), V(.6f, .04f, 4f), RailWhite);
            m.Box(pad + V(1.4f, .6f, 0), V(.6f, .04f, 4f), RailWhite);
            m.Box(pad + V(0, .6f, 0), V(2.4f, .04f, .6f), RailWhite);
            m.Add(HelicopterMesh(), pad + V(0, .6f, 1.2f), Quaternion.Euler(0, -60f, 0), Vector3.one * 1.3f);
            // the white flue stack and a low orange entrance canopy
            m.Surface = ComicSurface.Plaster;
            m.Box(V(bx1 + 4f, 26f, z0 + 44f), V(3.5f, 52f, 3.5f), FshWhite);
            m.Box(V(bx1 + 4f, 52.3f, z0 + 44f), V(3.9f, .6f, 3.9f), FshGrey);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(fx + 4f, 4.2f, z0 + 16f), V(8f, .5f, 10f), FinColours[0]);
            foreach (float z in new[] { z0 + 12f, z0 + 20f }) m.Box(V(fx + 7.5f, 2f, z), V(.3f, 4f, .3f), MastGrey);
        }

        // ==================================================================== landmarks and props

        /// <summary>Norfolk Island pine: straight tapering trunk, tiers of flat layered branches getting smaller to the top.</summary>
        static void NorfolkPine(ComicMesh m, Vector3 p, float h, int seed)
        {
            var r = new System.Random(seed * 131);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .08f, 0), .45f, .16f, K.R.ink, 8);
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, h * .5f, 0), .3f, h, PineBark, 8, .35f);
            const int tiers = 7;
            for (int i = 0; i < tiers; i++)
            {
                float t = i / (tiers - 1f), y = h * (.28f + .66f * t), rad = Mathf.Lerp(2.7f, .7f, t) * (h / 14f);
                float spin = (float)r.NextDouble() * 45f;
                m.Surface = ComicSurface.Foliage;
                m.Ellipsoid(p + V(0, y, 0), V(rad * 2f, .9f + .2f * (1f - t), rad * 2f), i % 2 == 0 ? PineDark : PineLight);
                m.Ellipsoid(p + Quaternion.Euler(0, spin, 0) * V(rad * .35f, y + .3f, 0), V(rad * 1.1f, .6f, rad * 1.1f), Color.Lerp(PineLight, K.R.yellow, .15f));
            }
            m.Surface = ComicSurface.Foliage;
            m.Cylinder(p + V(0, h + .5f, 0), .35f, 1.4f, PineLight, 8, .1f);
        }

        // Jacaranda in November bloom (the UWA kit's tree): a trunk splitting into three limbs under a broad umbrella crown
        // of purple and lilac blobs.
        static ComicMesh Jacaranda(int seed, float size)
        {
            var rng = new System.Random(seed * 7919);
            float R() => (float)rng.NextDouble();
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .06f, 0), .44f, .12f, K.R.ink, 8);
            Vector3 fork = V((R() - .5f) * .3f, 2.2f, 0);
            Limb(m, V(0, .1f, 0), fork, .27f, Bark, .8f);
            float spin = R() * 6f;
            for (int i = 0; i < 3; i++)
            {
                float a = spin + i * 2.09f;
                Limb(m, fork - V(0, .2f, 0), fork + V(Mathf.Cos(a) * 1.6f, 2.1f + R() * .4f, Mathf.Sin(a) * 1.6f), .2f, Bark, .65f);
            }
            Blob(m, fork + V(0, 3.1f, 0), 3.8f, J1, J3, true, .5f);
            for (int i = 0; i < 5; i++)
            {
                float a = spin + .6f + i * 1.2566f, rr = 2.3f + R() * .5f;
                Blob(m, fork + V(Mathf.Cos(a) * rr, 2.5f + R() * .5f, Mathf.Sin(a) * rr), 2.5f + R() * .6f, i % 2 == 0 ? J2 : J1, J3, i % 2 == 0, .62f);
            }
            if (Mathf.Approximately(size, 1f)) return m;
            var scaled = new ComicMesh();
            scaled.Add(m, Vector3.zero, Quaternion.identity, Vector3.one * size);
            return scaled;
        }

        static void Blob(ComicMesh m, Vector3 p, float size, Color main, Color light, bool highlight, float squash)
        {
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(p, V(size, size * squash, size), main);
            if (highlight) m.Ellipsoid(p + V(size * .23f, size * squash * .2f, 0), V(size * .65f, size * squash * .85f, size * .65f), light);
        }

        static void Limb(ComicMesh m, Vector3 a, Vector3 b, float r, Color c, float taper)
        {
            m.Surface = ComicSurface.Wood;
            Vector3 d = b - a;
            m.Cylinder((a + b) * .5f, r, d.magnitude, c, 8, taper, Quaternion.FromToRotation(Vector3.up, d));
        }

        /// <summary>The art-deco riverside hotel shape (no name): cream render, a rounded corner tower with teal bands and
        /// a stepped pylon, ribbon windows, flat roof with a parapet. Corner at p (towards the rider), wings along +Z and +X.</summary>
        static void DecoHotel(ComicMesh m, Vector3 p)
        {
            const float floors = 3, fh = 3.4f, hgt = floors * fh;
            m.Surface = ComicSurface.Plaster;
            m.Box(p + V(3f, hgt * .5f, 12f), V(9f, hgt, 20f), HotelCream);       // wing along the river
            m.Box(p + V(11f, hgt * .5f, 2.5f), V(14f, hgt, 8f), HotelCream);      // wing inland
            m.Cylinder(p + V(0, (hgt + 2f) * .5f, 0), 4.6f, hgt + 2f, HotelCream, 14);
            for (int f = 0; f < floors; f++)
            {
                float y = f * fh + 1.9f;
                m.Surface = ComicSurface.Glass;
                m.Box(p + V(-1.52f, y, 12f), V(.1f, 1.3f, 18.4f), DarkGlass);
                m.Box(p + V(11f, y, -1.52f), V(12.4f, 1.3f, .1f), DarkGlass);
                m.Cylinder(p + V(0, y, 0), 4.65f, 1.3f, DarkGlass, 14);
                m.Surface = ComicSurface.PaintedMetal;
                m.Cylinder(p + V(0, y + .95f, 0), 4.75f, .22f, HotelTrim, 14);
                m.Box(p + V(-1.56f, y + .95f, 12f), V(.12f, .22f, 20f), HotelTrim);
                m.Box(p + V(11f, y + .95f, -1.56f), V(14f, .22f, .12f), HotelTrim);
            }
            m.Surface = ComicSurface.Plaster;
            m.Cylinder(p + V(0, hgt + 2.2f, 0), 4.9f, .4f, LimeCap, 14);
            m.Box(p + V(3f, hgt + .3f, 12f), V(9.3f, .6f, 20.3f), LimeCap);
            m.Box(p + V(11f, hgt + .3f, 2.5f), V(14.3f, .6f, 8.3f), LimeCap);
            // the stepped pylon on the corner
            m.Box(p + V(0, hgt + 4.5f, 0), V(1.6f, 5f, 1.6f), HotelCream);
            m.Box(p + V(0, hgt + 7.6f, 0), V(1f, 1.4f, 1f), HotelCream);
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 3; i++) m.Box(p + V(0, hgt + 3f + i * 1.3f, 0), V(1.7f, .25f, 1.7f), HotelTrim);
            m.Cylinder(p + V(0, hgt + 9f, 0), .08f, 1.6f, K.R.ink, 8);
            // a palm or two at its door
            P.Palm(m, p + V(-3f, 0, 8f), 8f, 11);
        }

        static void RoundTower(ComicMesh m, Vector3 p, float r, float h)
        {
            m.Surface = ComicSurface.Glass;
            m.Cylinder(p + V(0, h * .5f, 0), r, h, TowerGlass, 12);
            m.Surface = ComicSurface.PaintedMetal;
            for (float y = 3.2f; y < h - 1f; y += 3.2f) m.Cylinder(p + V(0, y, 0), r + .15f, .25f, TowerWhite, 12);
            m.Surface = ComicSurface.Plaster;
            m.Cylinder(p + V(0, 3f, 0), r + 1.2f, 6f, TowerWhite, 12);
            m.Cylinder(p + V(0, h + 1.2f, 0), r * .8f, 2.4f, TowerWhite, 12);
        }

        static void SlabTower(ComicMesh m, Vector3 p, float w, float d, float h)
        {
            m.Surface = ComicSurface.Plaster;
            m.Box(p + V(0, h * .5f, 0), V(w, h, d), TowerWhite);
            m.Surface = ComicSurface.Glass;
            for (float y = 4f; y < h - 2f; y += 3.2f)
            {
                m.Box(p + V(0, y + .9f, -d * .5f - .02f), V(w - 1.4f, 1.6f, .1f), H("5C8FA6"));
                m.Box(p + V(-w * .5f - .02f, y + .9f, 0), V(.1f, 1.6f, d - 1.4f), H("5C8FA6"));
            }
            m.Surface = ComicSurface.PaintedMetal;
            for (float y = 4f; y < h - 2f; y += 3.2f) m.Box(p + V(-.5f, y, -d * .5f - .5f), V(w - .4f, .18f, 1f), TowerWhite);
            m.Box(p + V(0, h + .8f, 0), V(w * .5f, 1.6f, d * .5f), HotelTrim);
        }

        static void Dinghy(ComicMesh m, Vector3 p, float yaw, Color band)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            void B(Vector3 c, Vector3 s, Color col, ComicSurface surf, Quaternion? rot = null) { m.Surface = surf; m.Box(p + q * c, s, col, q * (rot ?? Quaternion.identity)); }
            B(V(0, .3f, 0), V(1.5f, .6f, 3f), RailWhite, ComicSurface.PaintedMetal);
            B(V(0, .3f, 1.75f), V(1.06f, .56f, 1.06f), RailWhite, ComicSurface.PaintedMetal, Quaternion.Euler(0, 45f, 0));
            B(V(0, .52f, 0), V(1.54f, .14f, 3.04f), band, ComicSurface.PaintedMetal);
            B(V(0, .62f, -.2f), V(1.2f, .08f, .3f), Timber, ComicSurface.Wood);
            B(V(0, .78f, -1.55f), V(.32f, .55f, .3f), K.R.ink, ComicSurface.PaintedMetal);
        }

        static void Bench(Ch c, float x, float z)
        {
            ComicMesh m = c.Props;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float dz in new[] { -.8f, .8f }) m.Box(V(x, K.Kerb + .23f, z + dz), V(.6f, .46f, .1f), K.R.ink);
            m.Surface = ComicSurface.Wood;
            m.Box(V(x, K.Kerb + .5f, z), V(.55f, .08f, 2f), Timber);
            m.Box(V(x + .26f, K.Kerb + .82f, z), V(.08f, .45f, 2f), Timber);
            c.Collide("Bench", x, z, V(.7f, 1f, 2f), true);
        }

        // ---- obstacles and animals

        static ComicMesh BollardMesh(Color body, Color band)
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .08f, 0), .25f, .16f, K.R.ink, 8);
            m.Cylinder(V(0, .52f, 0), .14f, .76f, body, 8);
            m.Cylinder(V(0, .74f, 0), .15f, .14f, band, 8);
            m.Cylinder(V(0, .5f, 0), .15f, .08f, band, 8);
            m.Cylinder(V(0, .95f, 0), .16f, .1f, K.R.ink, 8, .7f);
            return m;
        }

        /// <summary>A sit-on-top kayak on a little trolley (left on the path by someone going to the river), with a paddle.</summary>
        static ComicMesh KayakMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            Color hull = H("F28C28");
            m.Ellipsoid(V(0, .58f, 0), V(.8f, .42f, 3.5f), hull);
            m.Box(V(0, .66f, 0), V(.72f, .14f, 2.2f), hull);
            m.Ellipsoid(V(0, .8f, -.2f), V(.5f, .14f, .9f), K.R.ink);
            m.Box(V(0, .74f, .7f), V(.5f, .06f, .5f), H("F7C98A"));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, .3f, .3f), V(.7f, .08f, .9f), K.R.ink);
            m.Beam(V(-.3f, .14f, .3f), V(-.3f, .42f, .3f), .06f, K.R.ink);
            m.Beam(V(.3f, .14f, .3f), V(.3f, .42f, .3f), .06f, K.R.ink);
            m.Surface = ComicSurface.Rubber;
            foreach (float s in new[] { -1f, 1f }) m.Cylinder(V(s * .42f, .15f, .3f), .15f, .1f, K.R.ink, 8, 1f, Quaternion.Euler(0, 0, 90f));
            m.Surface = ComicSurface.PaintedMetal;
            m.Beam(V(-.55f, .85f, -1.1f), V(.45f, .85f, 1.2f), .05f, H("3A3F46"));
            m.Box(V(-.55f, .85f, -1.1f), V(.2f, .04f, .5f), H("2F6FB5"), Quaternion.Euler(0, 23f, 0));
            m.Box(V(.45f, .85f, 1.2f), V(.2f, .04f, .5f), H("2F6FB5"), Quaternion.Euler(0, 23f, 0));
            return m;
        }

        /// <summary>A temporary warning sign: a diamond with an ink border and a bike symbol on a post, sandbags on its feet.</summary>
        static ComicMesh WarningSignMesh(Color face)
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Box(V(0, .1f, 0), V(.9f, .08f, .5f), K.R.ink);
            m.Cylinder(V(0, .75f, 0), .045f, 1.3f, K.R.ink, 8);
            Quaternion d = Quaternion.Euler(0, 0, 45f);
            m.Box(V(0, 1.25f, -.04f), V(.7f, .7f, .05f), K.R.ink, d);
            m.Box(V(0, 1.25f, -.07f), V(.6f, .6f, .03f), face, d);
            Color ink = K.R.ink;
            foreach (float x in new[] { -.13f, .13f }) m.Cylinder(V(x, 1.17f, -.09f), .08f, .02f, ink, 8, 1f, Quaternion.Euler(90f, 0, 0));
            m.Beam(V(-.13f, 1.17f, -.095f), V(0, 1.3f, -.095f), .03f, ink);
            m.Beam(V(0, 1.3f, -.095f), V(.13f, 1.17f, -.095f), .03f, ink);
            m.Box(V(-.02f, 1.36f, -.095f), V(.1f, .03f, .02f), ink);
            m.Surface = ComicSurface.Fabric;
            foreach (float x in new[] { -.3f, .3f }) m.Ellipsoid(V(x, .18f, 0), V(.42f, .2f, .32f), H("B59A68"));
            return m;
        }

        // Lawn sprinkler (the UWA kit's): ink base, green body, yellow arm, chunky water arcs.
        static ComicMesh SprinklerMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .05f, 0), .32f, .1f, K.R.ink, 8);
            m.Surface = ComicSurface.Plastic;
            m.Cylinder(V(0, .17f, 0), .25f, .14f, H("3C8A4A"), 8, .8f);
            m.Cylinder(V(0, .32f, 0), .08f, .18f, H("F2C230"), 8);
            m.Box(V(0, .44f, 0), V(.58f, .08f, .1f), H("F2C230"));
            m.Box(V(0, .47f, 0), V(.12f, .08f, .12f), K.R.ink);
            Color spray = H("BFE4F8");
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 p0 = V(s * .3f, .46f, 0), p1 = V(s * .42f, .74f, 0), p2 = V(s * .58f, .72f, 0), p3 = V(s * .7f, .32f, 0);
                m.Surface = ComicSurface.Glass;
                foreach ((Vector3 a, Vector3 b) in new[] { (p0, p1), (p1, p2), (p2, p3) })
                    m.Cylinder((a + b) * .5f, .05f, (b - a).magnitude, spray, 8, 1f, Quaternion.FromToRotation(Vector3.up, b - a));
                m.Ellipsoid(p3 - V(0, .14f, 0), V(.13f, .13f, .13f), Color.white);
                m.Ellipsoid(p1 + V(s * .06f, .12f, .1f), V(.1f, .1f, .1f), Color.white);
            }
            return m;
        }

        /// <summary>Australian pelican: chunky white body, black wings and tail, a huge pale-pink bill and pouch. Facing -Z.</summary>
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
            m.Cylinder(V(0, .98f, -.18f), .1f, .36f, white, 8, 1f, Quaternion.FromToRotation(Vector3.up, V(0, .32f, -.12f)));
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

        /// <summary>Bobtail (shingleback) lizard: a squat scaly body with cream bands, a stumpy pine-cone tail, a wedge head
        /// and its famous blue tongue. Facing -Z, feet at the pivot.</summary>
        static ComicMesh BobtailMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color body = H("6B5A4A"), band = H("C9B48A"), dark = H("4A3D33"), tongue = H("3F6FD8");
            m.Ellipsoid(V(0, .17f, .02f), V(.34f, .2f, .62f), body);
            for (int i = 0; i < 4; i++) m.Box(V(0, .245f, -.18f + i * .12f), V(.24f, .05f, .05f), band);
            m.Ellipsoid(V(0, .15f, .38f), V(.26f, .17f, .3f), dark);                     // the stumpy tail
            m.Box(V(0, .2f, .38f), V(.18f, .04f, .04f), band);
            m.Box(V(0, .15f, -.36f), V(.2f, .13f, .2f), body);                           // head
            m.Box(V(0, .13f, -.48f), V(.15f, .09f, .1f), dark);                          // snout
            foreach (float x in new[] { -.09f, .09f }) m.Box(V(x, .19f, -.42f), V(.03f, .03f, .03f), K.R.ink);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .11f, -.57f), V(.09f, .02f, .1f), tongue);
            m.Box(V(0, .11f, -.62f), V(.12f, .02f, .03f), tongue);
            m.Surface = ComicSurface.Fabric;
            foreach (float x in new[] { -.17f, .17f })
                foreach (float z in new[] { -.16f, .18f }) m.Box(V(x, .05f, z), V(.12f, .1f, .09f), dark, Quaternion.Euler(0, x * 120f, 0));
            return m;
        }

        /// <summary>A rescue helicopter (no markings): red body with a white band, dark windows, tail boom and fin, rotor, skids.</summary>
        static ComicMesh HelicopterMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Ellipsoid(V(0, 1.4f, 0), V(2.2f, 2f, 4.4f), HeliRed);
            m.Box(V(0, 1.4f, .2f), V(2.22f, .3f, 2.6f), RailWhite);
            m.Surface = ComicSurface.Glass;
            m.Ellipsoid(V(0, 1.75f, -1.35f), V(1.7f, 1.1f, 1.7f), DarkGlass);
            m.Surface = ComicSurface.PaintedMetal;
            m.Beam(V(0, 1.6f, 1.8f), V(0, 1.95f, 6.2f), .45f, HeliRed);
            m.Box(V(0, 2.5f, 6.1f), V(.15f, 1.3f, .8f), HeliRed);
            m.Box(V(.3f, 2.1f, 6.2f), V(.05f, .8f, .2f), K.R.ink);
            m.Box(V(0, 2.55f, .1f), V(.9f, .5f, 1.6f), HeliRed);
            m.Cylinder(V(0, 2.95f, .1f), .18f, .4f, K.R.ink, 8);
            foreach (float a in new[] { 20f, 110f }) m.Box(V(0, 3.15f, .1f), V(.3f, .05f, 9.5f), K.R.ink, Quaternion.Euler(0, a, 0));
            foreach (float x in new[] { -.85f, .85f })
            {
                m.Box(V(x, .08f, 0), V(.12f, .12f, 3.6f), K.R.ink);
                m.Beam(V(x, .1f, -.8f), V(x * .6f, .7f, -.7f), .08f, K.R.ink);
                m.Beam(V(x, .1f, .9f), V(x * .6f, .7f, .8f), .08f, K.R.ink);
            }
            return m;
        }

        static GameObject SaveAnimal(string path, string meshId, ComicMesh art, Vector3 centre, Vector3 size, float walk)
        {
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            GameObject go = sync.Plain("Art");
            K.Renderer(go, meshId, art, true, false, false);
            sync.Finish();
            var box = PrefabBuildUtility.KeepSingle<BoxCollider>(root) ?? root.AddComponent<BoxCollider>();
            box.center = centre; box.size = size; box.isTrigger = false;
            var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
            o.kind = ObstacleKind.Person; o.animal = true; o.punchable = false; o.police = false; o.canJumpOver = true; o.spansRoad = false; o.spawnWeight = 1f;
            var ped = PrefabBuildUtility.GetOrAdd<Pedestrian>(root);
            ped.walkSpeed = walk; ped.crosses = false; ped.art = go.transform; ped.dodgeSpeed = 5f;
            PrefabBuildUtility.SaveAndClose(root, path);
            K.Report.Add(System.IO.Path.GetFileNameWithoutExtension(path) + " " + art.TriangleCount * 2 + " tris");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>The sandy berm kicker: a sand wedge with grass tufts and a limestone lip (generic district ramp).</summary>
        static Ramp SaveBermRamp(string path, string meshId)
        {
            const float width = 3f, rise = 5f, height = 1.2f;
            var spec = new CqRampSpec { X = 0, Z = 0, Width = width, Rise = rise, Height = height, Top = 0f };
            var art = new ComicMesh();
            K.RampMesh(art, spec, Sand, SandDark, false, 0f);
            art.Surface = ComicSurface.Concrete;
            art.Box(V(0, height - .1f, rise - .15f), V(width + .1f, .22f, .32f), Limestone);
            art.Surface = ComicSurface.Foliage;
            foreach (float x in new[] { -1.6f, 1.6f })
                foreach (float z in new[] { 1f, 3.2f })
                    art.Ellipsoid(V(x, height * z / rise, z), V(.5f, .45f, .5f), H("7E9A45"));
            art.Surface = ComicSurface.Plain;
            for (float z = .8f; z < rise - .5f; z += 1.1f)
            {
                float y = height * z / rise + .02f;
                K.Face(art, V(-1f, y, z), V(-1f, y, z + .25f), V(1f, y, z + .25f), V(1f, y, z), SandDark, Vector3.up, false);
            }
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            GameObject go = sync.Plain("Art");
            K.Renderer(go, meshId, art, true, true, false);
            sync.Finish();
            var ramp = PrefabBuildUtility.GetOrAdd<Ramp>(root);
            ramp.width = width; ramp.rise = rise; ramp.height = height; ramp.top = 0f; ramp.launch = 1f; ramp.hangGravity = .5f;
            ramp.lanes = Array.Empty<int>();
            PrefabBuildUtility.SaveAndClose(root, path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Ramp>();
        }

        // ==================================================================== far scenery (no colliders, follows the rider)

        // The city curl narrows and fades everything beyond ~220-360 m ahead into the sky, so the horizon band is built
        // from long strips beside the road (they converge to the vanishing point) plus a few things ahead at 150-300 m.
        // Layers get paler and bluer with distance (aerial perspective). Built flat, sheared with the district's grade.

        static void Hill(ComicMesh m, float x, float z, float w, float h, float d, Color c)
        {
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(V(x, 0f, z), V(w, h * 2f, d), c);
        }

        static Color Haze(Color c, float t) => Color.Lerp(c, new Color(.62f, .74f, .86f), t);

        static void FarSuburb(ComicMesh m, System.Random r, int side, float xa, float xb, float z0, float z1, float y, float haze)
        {
            float R() => (float)r.NextDouble();
            for (float z = z0; z < z1; z += 9f + R() * 6f)
            {
                float x = side * Mathf.Lerp(xa, xb, R());
                if (R() < .6f)
                {
                    m.Surface = ComicSurface.Plaster;
                    m.Box(V(x, y + 1.6f, z), V(10f, 3.2f, 9f), Haze(H("E8DCC4"), haze));
                    m.Surface = ComicSurface.Concrete;
                    m.Cylinder(V(x, y + 4f, z), 9f, 1.8f, Haze(R() < .35f ? Colorbond : TileRed, haze), 4, .2f, Quaternion.Euler(0, 45f, 0));
                }
                m.Surface = ComicSurface.Foliage;
                m.Ellipsoid(V(x + side * (6f + R() * 8f), y + 5f, z + R() * 6f), V(9f + R() * 5f, 8f + R() * 4f, 9f), Haze(Color.Lerp(Peppermint, P.GumLeafDark, R()), haze));
            }
        }

        static void Ridge(ComicMesh m, System.Random r, int side, float x, float z0, float z1, float w, float h, Color c, float step)
        {
            for (float z = z0; z < z1; z += step * (.8f + (float)r.NextDouble() * .4f))
                Hill(m, side * (x + (float)r.NextDouble() * w * .3f), z, w * (.8f + (float)r.NextDouble() * .5f), h * (.7f + (float)r.NextDouble() * .5f), step * 1.8f, c);
        }

        static ComicMesh CanningFar()
        {
            var m = new ComicMesh();
            var r = new System.Random(8080);
            float R() => (float)r.NextDouble();
            // ground and water under everything beyond the chunks (a little lower, so it never fights them)
            Plane(m, -900f, -70f, -100f, 700f, -.35f, Haze(DryGrass, .1f), ComicSurface.Foliage, 20f);
            Plane(m, 250f, 900f, -100f, 700f, WaterY - .2f, FarWater, ComicSurface.Plain, 20f);
            for (int i = 0; i < 60; i++)
            {
                float x = 260f + R() * 300f, z = R() * 500f, w = 3f + R() * 8f;
                K.Flat(m, x, x + w, x, x + w, z, z + .5f, WaterY - .19f, i % 3 == 0 ? RailWhite : Ripple);
            }
            // left: Como / Mt Pleasant roofs and trees, then low hills, then the Darling Range, low and blue
            FarSuburb(m, r, -1, 80f, 150f, -60f, 520f, -.3f, .08f);
            FarSuburb(m, r, -1, 160f, 230f, -60f, 520f, -.3f, .25f);
            Ridge(m, r, -1, 270f, -80f, 560f, 110f, 26f, Haze(H("6E8F5E"), .35f), 45f);
            Ridge(m, r, -1, 420f, -100f, 620f, 170f, 55f, H("98AECB"), 70f);
            // right: the far shore (Applecross / Mt Henry) with trees, houses and a cluster of towers, low hills beyond
            Plane(m, 285f, 900f, -100f, 700f, .4f, Haze(H("7E9A5A"), .25f), ComicSurface.Foliage, 20f);
            for (float z = -60f; z < 520f; z += 16f + R() * 10f)
            {
                m.Surface = ComicSurface.Foliage;
                m.Ellipsoid(V(292f + R() * 14f, 4f, z), V(16f + R() * 10f, 12f + R() * 6f, 14f + R() * 6f), Haze(Color.Lerp(Peppermint, P.GumLeafDark, R()), .2f));
            }
            FarSuburb(m, r, 1, 310f, 360f, -60f, 520f, .4f, .3f);
            Ridge(m, r, 1, 360f, -80f, 600f, 110f, 22f, Haze(H("6E8F5E"), .35f), 45f);
            for (int i = 0; i < 7; i++)
            {
                float x = 312f + R() * 30f, z = 60f + i * 22f + R() * 8f, h = 30f + R() * 45f, w = 9f + R() * 6f;
                Color c = Haze(i % 3 == 0 ? TowerGlass : i % 3 == 1 ? TowerWhite : H("8FB2C2"), .3f);
                m.Surface = ComicSurface.Glass;
                if (i % 2 == 0) m.Cylinder(V(x, h * .5f, z), w * .5f, h, c, 10);
                else m.Box(V(x, h * .5f, z), V(w, h, w), c);
                m.Surface = ComicSurface.PaintedMetal;
                for (float y = 4f; y < h; y += 6f) m.Box(V(x, y, z), V(w + .3f, .5f, w + .3f), Haze(TowerWhite, .35f));
            }
            Ridge(m, r, 1, 430f, -80f, 600f, 150f, 30f, Haze(H("7A9670"), .45f), 60f);
            // ahead: the long low Mt Henry Bridge over the water on the right, Mt Henry's bush hill on the left
            for (float x = 70f; x < 300f; x += 20f)
            {
                m.Surface = ComicSurface.Concrete;
                m.Box(V(x + 10f, 3.4f, 255f), V(20.05f, 1.4f, 14f), Haze(Concrete, .3f));
                m.Box(V(x, 1.2f, 255f), V(1.6f, 3.2f, 8f), Haze(ConcreteDark, .3f));
            }
            Ridge(m, r, -1, 75f, 210f, 300f, 60f, 16f, Haze(H("5E7E48"), .25f), 25f);
            for (int i = 0; i < 8; i++) MountsBayBuilder.Yacht(m, V(80f + R() * 150f, WaterY - .2f, 30f + R() * 220f), 1.6f + R() * .6f, i % 2 == 0 ? RailWhite : K.R.red);
            m.Shear(-CanningDrop / 40f);
            return m;
        }

        static ComicMesh MurdochFar()
        {
            var m = new ComicMesh();
            var r = new System.Random(9090);
            float R() => (float)r.NextDouble();
            Plane(m, -900f, -70f, -100f, 700f, -.35f, Haze(Sand, .05f), ComicSurface.Plain, 20f);
            Plane(m, 70f, 900f, -100f, 700f, -.35f, Haze(Color.Lerp(DryGrass, Lawn, .3f), .05f), ComicSurface.Foliage, 20f);
            // left: Murdoch bush (banksia, gums), the Fiona Stanley Hospital skyline, hills, the Darling Range far east
            for (float z = -60f; z < 520f; z += 7f + R() * 5f)
            {
                float x = -(80f + R() * 70f);
                m.Surface = ComicSurface.Foliage;
                m.Ellipsoid(V(x, 1.5f, z), V(7f + R() * 5f, 5f + R() * 3f, 7f), Haze(Color.Lerp(P.BanksiaLeaf, P.GumLeaf, R()), .1f));
                if (R() < .35f) P.Gum(m, V(x - 8f, 0f, z + 3f), 15f + R() * 5f, r.Next(), true);
            }
            for (int i = 0; i < 6; i++)
            {
                float x = -(190f + R() * 40f), z = 40f + i * 32f + R() * 10f, h = 22f + R() * 18f, w = 26f + R() * 14f;
                m.Surface = ComicSurface.Plaster;
                m.Box(V(x, h * .5f, z), V(w, h, 24f), Haze(i % 2 == 0 ? FshWhite : FshGrey, .25f));
                m.Surface = ComicSurface.PaintedMetal;
                for (float a = -10f; a <= 10f; a += 2.5f) m.Box(V(x + w * .5f + .2f, h * .45f, z + a), V(.6f, h * .8f, .5f), Haze(FinColours[Mathf.Abs((int)(a + i * 3)) % FinColours.Length], .3f));
                if (i == 2)
                {
                    m.Cylinder(V(x, h + .3f, z), 8f, .6f, Haze(H("4A5058"), .25f), 10);
                    m.Add(HelicopterMesh(), V(x, h + .6f, z), Quaternion.Euler(0, 30f, 0), Vector3.one * 1.4f);
                }
            }
            Ridge(m, r, -1, 300f, -80f, 560f, 120f, 24f, Haze(H("6E8A55"), .4f), 50f);
            Ridge(m, r, -1, 430f, -100f, 640f, 180f, 62f, H("98AECB"), 70f);
            // right: Bull Creek / Bateman roofs among peppermints and gums, then a bush ridge
            FarSuburb(m, r, 1, 75f, 140f, -60f, 520f, -.3f, .08f);
            FarSuburb(m, r, 1, 150f, 220f, -60f, 520f, -.3f, .25f);
            Ridge(m, r, 1, 280f, -80f, 560f, 120f, 22f, Haze(H("6E8A55"), .4f), 50f);
            // ahead: bush hummocks either side of the vanishing point
            Ridge(m, r, -1, 80f, 200f, 300f, 60f, 14f, Haze(P.GumLeafDark, .25f), 22f);
            Ridge(m, r, 1, 80f, 200f, 300f, 60f, 12f, Haze(Peppermint, .25f), 22f);
            m.Shear(-MurdochDrop / 40f);
            return m;
        }

        // ==================================================================== props atlases

        static GameObject Load(string tag, string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Pf(tag, name));

        public static void RenderAtlases()
        {
            K.Begin(MeshFolder(CanningTag));
            P.Init();
            ComicMesh Make(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }
            var cb = new List<(string, GameObject, ComicMesh)>
            {
                ("Jacaranda", null, Jacaranda(3, 1f)),
                ("NorfolkPine", null, Make(m => NorfolkPine(m, Vector3.zero, 14f, 1))),
                ("Pelican", Load(CanningTag, "ANM_CanningBridge_Pelican"), null),
                ("Bollard", Load(CanningTag, "OBS_CanningBridge_Bollard"), null),
                ("Kayak", Load(CanningTag, "OBS_CanningBridge_Kayak"), null),
                ("PathSign", null, Turned(WarningSignMesh(H("F28C28")))),
                ("JettyRamp", Load(CanningTag, "RMP_CanningBridge_Jetty"), null),
                ("JointRamp", Load(CanningTag, "RMP_CanningBridge_Joint"), null),
                ("Dinghy", null, Make(m => Dinghy(m, Vector3.zero, 30f, H("2F6FB5")))),
                ("Train", null, Turned(Make(m => Train(m, 0f, 0f, 12f, 0f, true, false)))),
                ("DecoHotel", null, Turned(Make(m => DecoHotel(m, Vector3.zero)))),
                ("RoundTower", null, Make(m => RoundTower(m, Vector3.zero, 7.5f, 64f))),
            };
            K.RenderAtlas("art-src/districts/canningbridge/props-atlas.png", cb);
            var mu = new List<(string, GameObject, ComicMesh)>
            {
                ("Bobtail", null, Turned(BobtailMesh())),
                ("Bollard", Load(MurdochTag, "OBS_Murdoch_Bollard"), null),
                ("WarningSign", null, Turned(WarningSignMesh(K.R.yellow))),
                ("Sprinkler", Load(MurdochTag, "OBS_Murdoch_Sprinkler"), null),
                ("SandBerm", Load(MurdochTag, "RMP_Murdoch_SandBerm"), null),
                ("Balga", null, Make(m => P.Balga(m, Vector3.zero, 1.2f, 4))),
                ("Banksia", null, Make(m => P.Banksia(m, Vector3.zero, 1.1f, 5))),
                ("KangarooPaw", null, Make(m => P.KangarooPaw(m, Vector3.zero, 1f, 6))),
                ("NoiseWall", null, Turned(NoiseWallSample())),
                ("Helicopter", null, HelicopterMesh()),
                ("Hospital", null, Make(m => Hospital(m, 0f, 0f))),
                ("Bus", null, Make(m => Bus(m, Vector3.zero))),
            };
            K.RenderAtlas("art-src/districts/murdoch/props-atlas.png", mu);
        }

        static ComicMesh Turned(ComicMesh m)
        {
            var t = new ComicMesh();
            t.Add(m, Vector3.zero, Quaternion.Euler(0, 180f, 0), Vector3.one);
            return t;
        }

        static ComicMesh NoiseWallSample()
        {
            var c = new Ch(MurdochTag, "Atlas", 0f);
            NoiseWall(c, 0f, 16f);
            var m = new ComicMesh();
            m.Add(c.Front, Vector3.zero, Quaternion.identity, Vector3.one);
            m.Add(c.Props, Vector3.zero, Quaternion.identity, Vector3.one);
            return m;
        }
    }
}
