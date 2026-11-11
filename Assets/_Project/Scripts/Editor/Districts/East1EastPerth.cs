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
using E = CityScooter.EditorTools.Districts.EastLineKit;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>EAST PERTH (order 230): the terminal and the Public Transport Centre, the Swan River foreshore, the freeway
    /// and rail bridges, the old power station; Optus Stadium and Matagarup Bridge across the water. See East1Builder.</summary>
    public static partial class East1Builder
    {
        public const string EastPerthName = "Eastperth";
        public static readonly string[] EastPerthChunkIds = { "Terminal", "Concourse", "Platforms", "Foreshore", "PowerStation" };
        /// <summary>The river flat: -1.2 m per 40 m chunk.</summary>
        public const float EastPerthDrop = -1.2f;
        // The foreshore jump (timber boardwalk kicker + road-wide SPLASH gap: the power station's old cooling-water channel).
        public const float ChannelRampStart = 6f, ChannelRise = 6f, ChannelHeight = 1.4f, ChannelTop = 3f, ChannelGap = 5f;
        const float ESea = -1.8f, BankWidth = 11f;
        // Behind the line's standard pair of tracks (T1, T2): the unwired standard gauge line of the long-distance trains
        // (u = 19: 27 from the road centre line). It begins at a buffer stop at the terminal and joins track 2 in the exit
        // chunk. The suburban island platform lies between track 2 and it.
        const float ESG = 19f, IslandNear = T2 + E.PlatformEdge, IslandFar = ESG - E.PlatformEdge, Island = (IslandNear + IslandFar) * .5f;
        static readonly Color StuccoYellow = K.Hex("E3C987"), StuccoRed = K.Hex("D7A08A"), Rust = K.Hex("8A5A3C"), Bronze = K.Hex("B07A3E"), BronzeDark = K.Hex("8C5E2E"), BronzeLight = K.Hex("C99A5A");

        static void BuildEastPerth(TrackTheme theme, bool atlas)
        {
            string n = EastPerthName, patterns = DataFolder + "/eastperth";
            K.Begin(Meshes(n), Prefabs(n), DataFolder, patterns);
            Obstacle trolley = K.SaveObstacle(Pf(n, "OBS_Eastperth_LuggageTrolley"), "OBS_Eastperth_LuggageTrolley", LuggageTrolleyMesh(), new Vector3(1.1f, 1.5f, 1.9f), ObstacleKind.Thing, false, false, .8f);
            Obstacle cases = K.SaveObstacle(Pf(n, "OBS_Eastperth_Suitcases"), "OBS_Eastperth_Suitcases", SuitcasesMesh(), new Vector3(1f, .75f, .55f), ObstacleKind.Thing, true, true, 1f);
            Obstacle bollard = K.SaveObstacle(Pf(n, "OBS_Eastperth_PathBollard"), "OBS_Eastperth_PathBollard", PathBollardMesh(), new Vector3(.4f, 1.1f, .4f), ObstacleKind.Thing, true, false, 1f);
            GameObject swan = SaveAnimal(Pf(n, "ANM_Eastperth_Swan"), "ANM_Eastperth_Swan", SwanMesh(), new Vector3(0, .45f, 0), new Vector3(.5f, .9f, 1f), .4f, true);
            GameObject magpie = SaveAnimal(Pf(n, "ANM_Eastperth_Magpie"), "ANM_Eastperth_Magpie", MagpieMesh(), new Vector3(0, .2f, 0), new Vector3(.3f, .4f, .5f), .5f, true);
            Ramp dock = K.SaveRamp(Pf(n, "RMP_Eastperth_DockRamp"), "RMP_Eastperth_DockRamp", 3f, 5f, 1.3f, 1.5f, 1f, .5f, K.Hex("C9C5BA"), K.Hex("8E959B"), true);

            K.WriteChunk(EEntry(), ChunkPath(n, "Entry"));
            K.WriteChunk(ETerminal(), ChunkPath(n, "Terminal"));
            K.WriteChunk(EConcourse(), ChunkPath(n, "Concourse"));
            K.WriteChunk(EPlatforms(), ChunkPath(n, "Platforms"));
            K.WriteChunk(EForeshore(), ChunkPath(n, "Foreshore"));
            K.WriteChunk(EPowerStation(), ChunkPath(n, "PowerStation"));
            K.WriteChunk(EExit(), ChunkPath(n, "Exit"));
            GameObject far = K.SaveFarScenery(Pf(n, "FAR_Eastperth_SwanRiver"), "FAR_Eastperth_SwanRiver", EastPerthFar(-EastPerthDrop / 40f));

            GameObject cyclist = Cyclist();
            var own = new List<HazardPattern>
            {
                K.Pattern(patterns, "HZ_Eastperth_Swans", 45, 0, 1f, true, 30, (swan, -2.4f, 22f, 0f), (swan, -.9f, 24.5f, 0f), (swan, 2.2f, 28f, 0f)),
                K.Pattern(patterns, "HZ_Eastperth_Luggage", 45, 0, .9f, true, 30, (trolley.gameObject, -2.6f, 20f, 0f), (cases.gameObject, 0f, 23f, 0f), (cases.gameObject, 2.6f, 27f, 0f)),
                K.Pattern(patterns, "HZ_Eastperth_Bollards", 40, 1, .7f, false, 30, (bollard.gameObject, -2.6f, 20f, 0f), (bollard.gameObject, 0f, 20f, 0f), (bollard.gameObject, 2.6f, 20f, 0f)),
            };
            if (cyclist != null) own.Add(K.Pattern(patterns, "HZ_Eastperth_Cyclists", 50, 0, 1f, true, 30, (cyclist, -2.4f, 24f, 0f), (cyclist, -1.2f, 31f, 0f)));
            own.AddRange(ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_SidewalkStroll", "HZ_PoliceStop"));
            var people = new List<GameObject>(K.People(theme, "Tourist", "Tourist", "Tourist", "Jogger", "Jogger", "DogWalker", "Office", "Elder", "Kid", "Courier", "Student"));
            people.Add(swan); people.Add(swan); people.Add(magpie); people.Add(magpie);
            WriteDistrict(n, "eastperth", "EAST PERTH", 230, new Vector2(360f, 440f), EastPerthChunkIds,
                new[] { trolley, cases, bollard }, new[] { cases, bollard }, own.ToArray(), people.ToArray(), new[] { dock }, 1.1f, far,
                new Look
                {
                    // open river light: a little cooler and brighter than the inner city
                    Fog = new Color(.76f, .87f, .94f), SkyTint = new Color(.98f, 1f, 1f), AmbSky = new Color(.69f, .85f, .99f),
                    AmbEquator = new Color(.88f, .88f, .83f), AmbGround = new Color(.68f, .64f, .54f), Sun = new Color(1f, .97f, .89f), SunIntensity = 1.45f,
                });
            Debug.Log("[Districts/Eastperth] " + string.Join("; ", K.Report));
            if (atlas)
                K.RenderAtlas("art-src/districts/eastperth/props-atlas.png", new List<(string, GameObject, ComicMesh)>
                {
                    ("Locomotive", null, Make(m => Locomotive(m, 0f, -9f))),
                    ("LongDistanceCar", null, Make(m => LongDistanceCar(m, 0f, -10f))),
                    ("Prospector", null, Make(m => Emu(m, 0f, -19f, 2, Prospector, false, false))),
                    ("Coach", null, Make(m => Coach(m, Vector3.zero, 0f))),
                    ("TransportCentre", null, Make(m => { var d = new CqChunkDraft("a", 0f); TransportCentre(d, new CqLot(-1, -15f, 15f, 6f, 16f, 0f), true); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("TerminalCanopy", null, Make(m => { var d = new CqChunkDraft("a", 0f); Platform(d, 0f, 5.5f, -10f, 10f, true, false); LongCanopy(d, 2.75f, -10f, 10f); m.Add(d.SlopedFrontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("PowerStationAdmin", null, Make(m => { var d = new CqChunkDraft("a", 0f); PowerAdmin(d, new CqLot(-1, -5f, 5f, 6f, 10f, 0f)); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("PowerStationHall", null, Make(m => { var d = new CqChunkDraft("a", 0f); PowerHall(d, new CqLot(-1, -11f, 11f, 6f, 20f, 0f)); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("SteelFrames", null, Make(m => SteelFrames(m, V(0, 0, -5f), 10f, 9f))),
                    ("RiverBridges", null, Make(m => { var d = new CqChunkDraft("a", 0f); RiverBridges(d, 0f); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("OptusStadium", null, Make(m => Stadium(m, Vector3.zero, 80f, 70f, Color.clear))),
                    ("MatagarupBridge", null, Make(m => Matagarup(m, V(-80, 0, 0), V(80, 0, 0), Color.clear))),
                    ("ImpossibleTriangle", null, Make(m => ImpossibleTriangle(m, Vector3.zero))),
                    ("GraffitiWall", null, Make(m => { var d = new CqChunkDraft("a", 0f); GraffitiWall(d, -8f, 8f, 0f, 3); m.Add(d.SlopedFrontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("LuggageTrolley", trolley.gameObject, null),
                    ("Suitcases", cases.gameObject, null),
                    ("PathBollard", bollard.gameObject, null),
                    ("Swan", swan, null),
                    ("Magpie", magpie, null),
                    ("DockRamp", dock.gameObject, null),
                });
        }

        // ---------------------------------------------------------------- East Perth chunks

        /// <summary>East Perth's streets are the line's boundary surface (grey asphalt, pale flagstones).</summary>
        static void EStreet(CqChunkDraft d) => E.Street(d, E.StreetLook.Boundary);

        /// <summary>The railway on the left: the line's standard corridor and, behind it on ballast out to
        /// <paramref name="bed"/> (u), the unwired standard gauge track (gauge = false: the chunk draws it itself).</summary>
        static void ERail(CqChunkDraft d, float z0, float z1, float bed = 21f, bool gauge = true)
        {
            E.Corridor(d, RailSide, z0, z1, new E.CorridorOptions
            {
                UnwiredTracks = gauge ? new[] { K.Edge + ESG } : null, BedTo = K.Edge + bed, BackFence = false, Beyond = K.Hex("8E8A80"),
            });
        }

        /// <summary>The long terminal platform beyond the standard gauge track, under its canopy.</summary>
        static void TerminalPlatform(CqChunkDraft d, float z0, float z1, float canopyTo = -1f)
        {
            Platform(d, 21f, 26.6f, z0, z1, true, false);
            LongCanopy(d, 23.9f, z0, canopyTo > 0f ? canopyTo : z1);
        }

        /// <summary>East Parade beside the shared path: a grass verge with trees, the two-lane road (scenery: parked cars),
        /// the far footpath.</summary>
        static void EastParade(CqChunkDraft d, float z0, float z1, int seed, bool trees = true)
        {
            Ground(d, 1, z0, z1, 0f, 2.6f, Lawn, ComicSurface.Foliage);
            Ground(d, 1, z0, z1, 2.6f, 10.8f, K.R.asphalt, ComicSurface.Asphalt, .02f);
            Ground(d, 1, z0, z1, 10.8f, 60f, Paving, ComicSurface.Paving);
            d.Road.Surface = ComicSurface.Plain;
            for (float z = z0 + 1f; z < z1 - 3f; z += 6f)
                K.Face(d.Road, V(K.Edge + 6.62f, .03f, z), V(K.Edge + 6.62f, .03f, z + 3f), V(K.Edge + 6.78f, .03f, z + 3f), V(K.Edge + 6.78f, .03f, z), White, Vector3.up, false);
            d.SlopedFrontage.Surface = ComicSurface.Concrete;
            K.SideBox(d.SlopedFrontage, 1, z0, z1, -.2f, K.Kerb + .04f, 2.45f, 2.7f, KerbCap);
            var r = new System.Random(seed * 7 + 1);
            Color[] cars = { K.Hex("C9CED3"), K.Hex("2F6FB0"), K.Hex("A8452E"), White, K.Hex("3A4148"), K.Hex("E6B93C") };
            for (float z = z0 + 5f; z < z1 - 4f; z += F(r, 11f, 17f))
                ParkedCar(d.Props, V(K.Edge + 9.6f, d.Y(z) + .02f, z), r.NextDouble() < .5 ? 0f : 180f, cars[r.Next(cars.Length)]);
            if (trees)
                for (float z = z0 + 7f; z < z1 - 3f; z += 14f)
                {
                    if ((int)(z + seed) % 2 == 0) JacarandaTree(d.Props, V(K.Edge + 1.3f, d.Ground(z), z), F(r, 7f, 8.5f), seed + (int)z);
                    else Gum(d.Props, V(K.Edge + 1.3f, d.Ground(z), z), F(r, 9f, 11f), seed + (int)z);
                }
            SideWall(d, 1, z0, z1, 12f);
        }

        static CqChunkDraft EEntry()
        {
            // From Claisebrook: the railway runs on beside the street; the line's street edge on the right, then the lawn
            // of the East Parade roundabout with the Impossible Triangle. The street becomes the shared path at the start
            // of the terminal chunk (which also follows the power station chunk when the chunks come round again).
            var d = new CqChunkDraft("Eastperth_Entry", EastPerthDrop);
            EStreet(d);
            E.Corridor(d, RailSide, 0f, 40f, new E.CorridorOptions { Beyond = K.Hex("8E8A80") });
            MBackdrop(d, 0f, 40f, 91, 12f, 26f);
            OuterEdge(d, 0f, 14f, Lawn, 12, true);
            Ground(d, 1, 14f, 40f, 0f, 60f, Lawn, ComicSurface.Foliage);
            LowWall(d, 1, 14f, 40f, .35f, ConcreteGrey, KerbCap);
            ImpossibleTriangle(d.Landmark, V(K.Edge + 11f, d.Ground(27f), 27f));
            JacarandaTree(d.Props, V(K.Edge + 4f, d.Ground(18f), 18f), 8f, 3);
            JacarandaTree(d.Props, V(K.Edge + 5f, d.Ground(37f), 37f), 7.5f, 8);
            for (int i = 0; i < 5; i++) GrassTuft(d.Props, V(K.Edge + 8.5f + i * 1.2f, d.Ground(24f), 23.5f + (i % 2) * .8f), 1.1f, i);
            DeepTownhouses(d, 1, 14f, 40f, 26f, 92);
            SideWall(d, 1, 14f, 40f);
            MLights(d, 30f, 18f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft EExit()
        {
            // The exit follows one of the shared path chunks: PATH ENDS, a band of setts and a zebra bring the street back.
            // The standard gauge track swings over and joins track 2: from z = 28 the corridor is the plain standard that
            // Mt Lawley continues. A last terrace and townhouses, then the line's street edge on the right.
            var d = new CqChunkDraft("Eastperth_Exit", EastPerthDrop);
            E.Street(d, E.StreetLook.SharedPath(Paving), E.StreetLook.Boundary, 9f);
            E.Corridor(d, RailSide, 0f, 40f, new E.CorridorOptions { BedTo = K.Edge + 21f, WideFrom = 0f, WideTo = 28f, Beyond = K.Hex("8E8A80") });
            E.TrackShift(d, RailSide, K.Edge + ESG, E.Track2, 0f, 26f);
            MBackdrop(d, 0f, 40f, 95, 10f, 20f);
            Ground(d, 1, 0f, 26f, 0f, 60f, Paving, ComicSurface.Paving);
            ShopTerrace(d, new CqLot(1, .5f, 12.5f, K.Edge + 1.5f, 12f, d.Base(6.5f)), 2, BrickRed, TrimCream, K.Hex("2F5A3A"), 1, true);
            TownhouseRow(d, 1, 13f, 25.5f, K.Edge + 2.5f, 41, false);
            DeepTownhouses(d, 1, 0f, 26f, 18f, 96);
            OuterEdge(d, 26f, 40f, Paving, 14, true);
            MLights(d, 8f, 22f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft ETerminal()
        {
            // The shared path beside East Parade. Across the suburban tracks the Indian Pacific stands at the long covered
            // terminal platform, its blue and yellow locomotive at the head; the brown brick Public Transport Centre with
            // its angled fins rises behind.
            var d = new CqChunkDraft("Eastperth_Terminal", EastPerthDrop);
            // The street (the entry chunk, or the power station chunk) becomes the shared path here: zebra, a band of
            // setts, SHARED PATH on the red asphalt.
            d.Type = RoadType.PedestrianMall;
            E.Street(d, E.StreetLook.Boundary, E.StreetLook.SharedPath(Paving), 4f);
            PathDiamond(d, 16f);
            ERail(d, 0f, 40f, 21f, false);
            E.Track(d, RailSide, K.Edge + ESG, 1.4f, 40f);
            ComicMesh m = d.SlopedFrontage;
            E.BufferStop(m, RailSide, K.Edge + ESG, 1.3f);
            Locomotive(m, ESG, 2f);
            LongDistanceCar(m, ESG, 19.9f);
            TerminalPlatform(d, 0f, 40f);
            Platform(d, IslandNear, IslandFar, 24f, 40f, true, true);
            PlatformSign(d, Island, 26f, "EAST PERTH", .095f);
            ParkingDeck(d, 0f, 6f);
            TransportCentre(d, new CqLot(-1, 6.5f, 40f, K.Edge + 27.6f, 16f, d.Base(23f)), false);
            EastParade(d, 0f, 40f, 3);
            ShopTerrace(d, new CqLot(1, 1f, 14f, K.Edge + 12.4f, 12f, d.Base(7.5f)), 2, BrickRed, TrimCream, Navy, 0, true);
            TownhouseRow(d, 1, 15f, 39.5f, K.Edge + 12.8f, 51, false);
            DeepTownhouses(d, 1, 0f, 40f, 30f, 52);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft EConcourse()
        {
            // The carriages of the Indian Pacific, the island platform with its canopy and lift, the footbridge to the
            // terminal concourse, PUBLIC TRANSPORT CENTRE on the building's top band.
            var d = new CqChunkDraft("Eastperth_Concourse", EastPerthDrop);
            SharedPath(d);
            ERail(d, 0f, 40f);
            ComicMesh m = d.SlopedFrontage;
            LongDistanceCar(m, ESG, .3f);
            LongDistanceCar(m, ESG, 20.3f);
            TerminalPlatform(d, 0f, 40f);
            Platform(d, IslandNear, IslandFar, 0f, 40f, true, true);
            FlatCanopy(d, Island, 4f, 14f, 3.8f, K.Hex("C9CED3"), K.Hex("6E747A"));
            StationFootbridge(d, 25f, Island, 26f);
            PlatformSign(d, Island, 33f, "EAST PERTH", .095f);
            TransportCentre(d, new CqLot(-1, 0f, 32f, K.Edge + 27.6f, 16f, d.Base(16f)), true);
            Coach(d.SlopedFrontage, V(LX(31f), .12f, 36f), 200f);
            EastParade(d, 0f, 40f, 5);
            CoveApartments(d, new CqLot(1, 1.5f, 20f, K.Edge + 12.4f, 14f, d.Base(11f)), 4, K.Hex("E6E0D2"), SignBlue);
            ShopTerrace(d, new CqLot(1, 21f, 39f, K.Edge + 12.4f, 12f, d.Base(30f)), 2, K.Hex("D9B98A"), TrimCream, K.Hex("8A3A28"), 2, false);
            DeepTownhouses(d, 1, 0f, 40f, 30f, 54);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft EPlatforms()
        {
            // An A-series set at the island platform, the Prospector at the terminal platform, coaches in their bays; on
            // the right the old brick warehouse, then the street opens towards the river.
            var d = new CqChunkDraft("Eastperth_Platforms", EastPerthDrop);
            SharedPath(d);
            PathDiamond(d, 30f);
            ERail(d, 0f, 40f);
            ComicMesh m = d.SlopedFrontage;
            Emu(m, T2, .8f, 2, ASeries);
            Emu(m, ESG, .8f, 2, Prospector, false, false);
            TerminalPlatform(d, 0f, 40f, 26f);
            Platform(d, IslandNear, IslandFar, 0f, 33f, true, true);
            FlatCanopy(d, Island, 8f, 14f, 3.8f, K.Hex("C9CED3"), K.Hex("6E747A"));
            ValidatorPoles(d, Island, 30f);
            Coach(m, V(LX(31.5f), .12f, 31f), 8f);
            Coach(m, V(LX(35.5f), .12f, 32f), 8f);
            FlatCanopy(d, 33.5f, 26f, 12f, 9f, K.Hex("D9DEE3"), K.Hex("6E747A"));
            DeepBlocks(d, -1, 0f, 40f, 40f, 61, 16f, 24f);
            EastParade(d, 0f, 26f, 7);
            Warehouse(d, new CqLot(1, 1.5f, 23.5f, K.Edge + 12.4f, 14f, d.Base(12.5f)));
            DeepTownhouses(d, 1, 0f, 26f, 30f, 62);
            // the river comes into view
            RiverBank(d, 26f, 40f, 63, true);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft EForeshore()
        {
            // The Swan River foreshore: lawn, gums and reeds down to the water, Optus Stadium and Matagarup Bridge across it
            // (far scenery). The path crosses the power station's old cooling-water channel on a timber boardwalk whose
            // middle is gone (SPLASH). A graffiti noise wall behind the tracks.
            var d = new CqChunkDraft("Eastperth_Foreshore", EastPerthDrop);
            float ramp = ChannelRampStart, lip = ramp + ChannelRise + ChannelTop, far = lip + ChannelGap;
            SharedPath(d, lip, far);
            ERail(d, 0f, 40f, 23f);
            GraffitiWall(d, 0f, 40f, 23f, 71);
            MBackdrop(d, 0f, 40f, 72, 10f, 22f);
            // the channel under the path: water to the river on the right, a culvert headwall on the railway side
            ComicMesh m = d.Road;
            K.Water(m, -K.Edge, K.Edge + 160f, lip, far, ESea, River, RiverLight, 73);
            CanalWall(d.Road, -K.Edge - 1f, K.Edge + BankWidth, lip, true);
            CanalWall(d.Road, -K.Edge - 1f, K.Edge + BankWidth, far, false);
            ComicMesh w = d.SlopedFrontage;
            w.Surface = ComicSurface.Concrete;
            K.MinMaxBox(w, V(-K.Edge - .6f, ESea - 1f, lip - .4f), V(-K.Edge, K.Kerb + .5f, far + .4f), ConcreteGrey);
            w.Surface = ComicSurface.Plain;
            K.Face(w, V(-K.Edge + .012f, ESea - .2f, lip + 1.2f), V(-K.Edge + .012f, ESea - .2f, far - 1.2f), V(-K.Edge + .012f, -.6f, far - 1.2f), V(-K.Edge + .012f, -.6f, lip + 1.2f), K.Hex("14181D"), Vector3.right);
            var spec = new CqRampSpec { Name = "BoardwalkKicker", X = 0f, Z = ramp, Width = 2f * K.Edge, Rise = ChannelRise, Height = ChannelHeight, Top = ChannelTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            d.Gaps.Add(new CqGapSpec { Name = "CoolingChannel", Z = lip, Length = ChannelGap, Width = 0f, Word = "SPLASH", Water = true });
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, DeckLight, Deck, false, ESea - .3f);
            for (int s = -1; s <= 1; s += 2)
            {
                deck.Surface = ComicSurface.Wood;
                deck.Beam(V(s * (K.Edge + .12f), 1.05f, ramp), V(s * (K.Edge + .12f), ChannelHeight + 1.05f, ramp + ChannelRise), .12f, Timber);
                deck.Beam(V(s * (K.Edge + .12f), ChannelHeight + 1.05f, ramp + ChannelRise), V(s * (K.Edge + .12f), ChannelHeight + 1.05f, lip - .2f), .12f, Timber);
                for (float z = ramp; z <= lip; z += 3f)
                {
                    float h = ChannelHeight * Mathf.Clamp01((z - ramp) / ChannelRise);
                    deck.Box(V(s * (K.Edge + .12f), (h + ESea) * .5f + .5f, z), V(.22f, h - ESea + 1f, .22f), Deck);
                }
            }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            d.KeepOut.Add(KeepOutBox(d, -1, ramp, far)); d.KeepOut.Add(KeepOutBox(d, 1, ramp, far));
            RiverBank(d, 0f, lip, 74, false);
            RiverBank(d, far, 40f, 75, true);
            // Across the water (no outline back there): Matagarup Bridge from this bank back to the Burswood shore, and
            // Optus Stadium at its far end. Smaller than life (forced perspective) so the fog leaves them their colours.
            Vector3 landing = V(K.Edge + BankWidth - 1.5f, d.Y(33f) + ESea, 33f);
            Matagarup(d.Deep, landing, V(152f, d.Y(3f) + ESea, 3f), Color.clear, .4f);
            d.Deep.Surface = ComicSurface.Concrete;
            d.Deep.Box(landing + V(-1.5f, 1.6f, .4f), V(5f, 3.6f, 3f), K.Hex("C9C5BA"));
            Stadium(d.Deep, V(222f, d.Y(0f) + ESea + 1f, -14f), 60f, 52f, Color.clear, .7f);
            d.GiftSlots.Add(new Vector2(0f, 29f));
            d.Slot(-2.6f, 34f); d.Slot(2.6f, 34f);
            return d;
        }

        static CqChunkDraft EPowerStation()
        {
            // Under the Goongoongup rail bridge and the Windan freeway bridge, then the old East Perth Power Station: the
            // Art Deco office block with its corner tower, the great stucco hall with tall windows, bare steel frames.
            var d = new CqChunkDraft("Eastperth_PowerStation", EastPerthDrop);
            EStreet(d);
            E.PathEnd(d, .35f);     // the shared path of the foreshore chunk ends here
            ERail(d, 0f, 40f, 23f);
            GraffitiWall(d, 16f, 40f, 23f, 81);
            MBackdrop(d, 16f, 40f, 82, 10f, 20f);
            RiverBridges(d, .6f);
            RiverBank(d, 0f, 16f, 83, false);
            Ground(d, 1, 16f, 40f, 0f, 44f, K.Hex("B9B09A"), ComicSurface.Concrete);
            K.Water(d.Road, K.Edge + 44f, K.Edge + 160f, 16f, 40f, ESea, River, RiverLight, 84);
            Patch(d, 1, 16f, 40f, 0f, 1.6f, Paving, K.Kerb + .03f, ComicSurface.Paving);
            Palisade(d, 1, 27.5f, 40f, 1.8f, 2.1f, K.Hex("6E747A"));
            PowerHall(d, new CqLot(1, 17f, 39.5f, K.Edge + 14.5f, 20f, d.Base(28f)));
            PowerAdmin(d, new CqLot(1, 16.5f, 27f, K.Edge + 3.2f, 10f, d.Base(22f)));
            SteelFrames(d.Props, V(K.Edge + 8.4f, d.Ground(34f), 29.5f), 9.5f, 8.5f);
            NameBoard(d.Props, V(K.Edge + 1.9f, d.Ground(16.6f), 16.4f), "EAST PERTH POWER STATION", .06f, K.Hex("20262C"), SafetyOrange, 1.3f);
            for (int i = 0; i < 6; i++) GrassTuft(d.Props, V(K.Edge + 3f + (i % 3) * 1.1f, d.Ground(30f), 28.5f + i * 1.7f), 1.2f, i + 1);
            SideWall(d, 1, 16f, 40f, 12f);
            MLights(d, 20f, 30f);
            d.StandardSlots();
            return d;
        }

        // ---------------------------------------------------------------- East Perth parts

        /// <summary>The river bank beyond the right sidewalk: lawn, a sandy edge with reeds, the Swan River out to where the
        /// far scenery takes over; gums, a bench, a lamp, two black swans on the water.</summary>
        static void RiverBank(CqChunkDraft d, float z0, float z1, int seed, bool furniture)
        {
            Ground(d, 1, z0, z1, 0f, BankWidth, Lawn, ComicSurface.Foliage);
            Ground(d, 1, z0, z1, BankWidth, BankWidth + 1.4f, K.Hex("D9C9A0"), ComicSurface.Concrete, -.5f);
            K.Water(d.Road, K.Edge + BankWidth + 1.2f, K.Edge + 160f, z0, z1, ESea, River, RiverLight, seed);
            var r = new System.Random(seed * 11 + 3);
            Color reed = K.Hex("8A9A4E");
            for (float z = z0 + 1f; z < z1 - .5f; z += F(r, 1.6f, 2.6f))
            {
                ComicMesh m = d.Props;
                m.Surface = ComicSurface.Foliage;
                Vector3 p = V(K.Edge + BankWidth + F(r, .2f, 1.6f), d.Y(z) + ESea + .5f, z);
                for (int i = 0; i < 3; i++)
                {
                    float a = F(r, 0f, 6.28f);
                    Slab(m, p + V(i * .22f, 0, 0), p + V(i * .22f + Mathf.Cos(a) * .25f, F(r, 1.3f, 2.1f), Mathf.Sin(a) * .25f), Vector3.right, .16f, .04f, .04f, i % 2 == 0 ? reed : K.Hex("A3AE5E"), K.Hex("6E7A3E"));
                }
            }
            if (z1 - z0 > 9f)
            {
                Gum(d.Props, V(K.Edge + F(r, 4f, 6f), d.Ground(z0 + 4f), z0 + 4f), F(r, 10f, 12f), seed);
                if (z1 - z0 > 16f) Gum(d.Props, V(K.Edge + F(r, 6.5f, 8.5f), d.Ground(z1 - 5f), z1 - 5f), F(r, 8.5f, 10.5f), seed + 2, K.Hex("7F9A6A"), K.Hex("A3B88A"), K.Hex("E9E2D2"));
            }
            if (furniture && z1 - z0 > 9f)
            {
                Bench(d.Props, V(K.Edge + 2f, d.Ground((z0 + z1) * .5f), (z0 + z1) * .5f), 90f);
                PathLight(d.Props, V(K.Edge + .7f, d.Ground(z0 + 2f), z0 + 2f), 4.2f);
                var swan = SwanMesh();
                d.Props.Add(swan, V(K.Edge + BankWidth + 5f, d.Y(z1 - 4f) + ESea - .3f, z1 - 4f), Quaternion.Euler(0, 110, 0), Vector3.one * 1.2f);
                d.Props.Add(swan, V(K.Edge + BankWidth + 7.5f, d.Y(z1 - 6f) + ESea - .3f, z1 - 6f), Quaternion.Euler(0, 140, 0), Vector3.one * 1.1f);
            }
            SideWall(d, 1, z0, z1, 10f);
        }

        /// <summary>A long platform canopy: a pale sheet roof with an ink edge on a row of T-columns.</summary>
        static void LongCanopy(CqChunkDraft d, float u, float z0, float z1, float width = 6f)
        {
            ComicMesh m = d.SlopedFrontage;
            float x = LX(u), top = PlatformTop + 4.5f;
            Color column = K.Hex("7F868C"), roof = K.Hex("E1E4E6");
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                m.Surface = ComicSurface.Metal;
                m.Box(V(x, top + .1f, (z + e) * .5f), V(width, .2f, e - z), roof);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(x, top - .07f, (z + e) * .5f), V(width + .12f, .14f, e - z), Ink);
            }
            for (float z = z0 + 2f; z < z1 - .5f; z += 8f)
            {
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(x, (top + PlatformTop) * .5f, z), V(.3f, top - PlatformTop, .3f), column);
                m.Box(V(x, top - .3f, z), V(width - .4f, .3f, .22f), column);
                m.Beam(V(x, top - 1.5f, z), V(x + width * .4f, top - .4f, z), .12f, column);
                m.Beam(V(x, top - 1.5f, z), V(x - width * .4f, top - .4f, z), .12f, column);
            }
        }

        /// <summary>The covered car park at the centre's south end: two concrete decks on columns with a ramp parapet.</summary>
        static void ParkingDeck(CqChunkDraft d, float z0, float z1)
        {
            ComicMesh m = d.SlopedFrontage;
            float xa = LX(27.6f), xb = LX(43f), xc = (xa + xb) * .5f, w = xa - xb, zc = (z0 + z1) * .5f, len = z1 - z0;
            m.Surface = ComicSurface.Concrete;
            foreach (float y in new[] { 3.6f, 7f })
            {
                m.Box(V(xc, y, zc), V(w, .4f, len), ConcreteGrey);
                m.Box(V(xa + .1f, y + .7f, zc), V(.2f, 1f, len), ConcreteGrey);
                m.Box(V(xc, y + .7f, z0 + .1f), V(w, 1f, .2f), ConcreteGrey);
            }
            for (float x = xb + 1f; x <= xa; x += (w - 2f) / 3f) m.Box(V(x, 3.5f, z0 + .4f), V(.5f, 7f, .5f), ConcreteDark);
            m.Surface = ComicSurface.Plain;
            K.Face(m, V(xb + .5f, .2f, z0 + .3f), V(xa - .5f, .2f, z0 + .3f), V(xa - .5f, 3.3f, z0 + .3f), V(xb + .5f, 3.3f, z0 + .3f), K.Hex("2A2F36"), Vector3.back, false);
        }

        /// <summary>The Public Transport Centre (the 1976 Westrail Centre): a long five-storey block of dark brown face brick
        /// and off-form concrete: concrete spandrel bands, ribbons of dark glass, angled brick fins standing in front of the
        /// windows, a corrugated metal roof, a plant room and a mast. With entrance: the glazed concourse front.</summary>
        static void TransportCentre(CqChunkDraft d, CqLot l, bool entrance)
        {
            ComicMesh f = d.Frontage;
            float W = l.W, D = l.Depth, gh = 4.6f, fh = 3.75f; int upper = 4; float h = gh + upper * fh;
            Plinth(f, l); d.Wall(l, 22f);
            f.Surface = ComicSurface.Glass;
            K.LBox(f, l, .4f, W - .4f, 0f, gh, -1.2f, -D, GlassDark);
            f.Surface = ComicSurface.Concrete;
            for (float a = .4f; a <= W + .01f; a += (W - .8f) / Mathf.Max(2, Mathf.RoundToInt(W / 4.6f))) K.LBox(f, l, Mathf.Max(0f, a - .4f), Mathf.Min(W, a + .4f), 0f, gh, 0f, -1.2f, ConcreteGrey);
            f.Surface = ComicSurface.Brick;
            K.LBox(f, l, 0f, W, gh, h, -.9f, -D, BrickBrown);
            for (int k = 0; k <= upper; k++)
            {
                float y = gh + k * fh;
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, -.12f, W + .12f, y - .5f, y + .5f, .12f, -D - .12f, ConcreteGrey);
                if (k == upper) break;
                f.Surface = ComicSurface.Glass;
                K.LQuad(f, l, .4f, W - .4f, y + .85f, y + fh - .75f, -.88f, GlassDark);
                // the end towards the rider: a ribbon too
                K.Face(f, l.P(-.012f, y + .85f, -2f), l.P(-.012f, y + .85f, -D + 2f), l.P(-.012f, y + fh - .75f, -D + 2f), l.P(-.012f, y + fh - .75f, -2f), GlassDark, Vector3.back);
            }
            // the angled fins (broad faces turned towards the rider)
            f.Surface = ComicSurface.Brick;
            int fins = Mathf.Max(4, Mathf.RoundToInt(W / 2.3f));
            for (int i = 0; i <= fins; i++)
            {
                float a = Mathf.Lerp(.5f, W - .5f, (float)i / fins);
                f.Box(l.P(a, (gh + h) * .5f, -.35f), V(1.25f, h - gh - 1f, .3f), i % 2 == 0 ? BrickBrown : Mix(BrickBrown, BrickRed, .25f), Quaternion.Euler(0, l.S * 34f, 0));
            }
            // roof: fascia, low corrugated roof, plant room, mast with dishes
            f.Surface = ComicSurface.Metal;
            K.LSlope(f, l, -.2f, W + .2f, h + .5f, h + 2.1f, .3f, -D * .5f, -D - .2f, RoofGrey);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, W * .42f, W * .62f, h + .6f, h + 4.2f, -D * .32f, -D * .68f, ConcreteGrey);
            f.Surface = ComicSurface.PaintedMetal;
            Vector3 mast = l.P(W * .52f, h + 4.2f, -D * .5f);
            f.Cylinder(mast + V(0, 3.5f, 0), .12f, 7f, K.Hex("C9CED3"), 6, .5f);
            f.Cylinder(mast + V(.5f, 2.2f, 0), .55f, .2f, White, 8, 1f, Quaternion.Euler(0, 0, 90));
            f.Cylinder(mast + V(-.45f, 3.6f, .2f), .4f, .18f, White, 8, 1f, Quaternion.Euler(0, 0, 90));
            f.Box(mast + V(0, 6.9f, 0), V(.3f, .3f, .3f), K.R.red);
            if (!entrance) return;
            // the concourse front: a concrete portal two storeys high, glazing with mullions, a canopy to the platform
            float a0 = W * .52f, a1 = W * .86f;
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, a0 - .5f, a0, 0f, gh + fh, 1f, -.9f, ConcreteGrey);
            K.LBox(f, l, a1, a1 + .5f, 0f, gh + fh, 1f, -.9f, ConcreteGrey);
            K.LBox(f, l, a0 - .5f, a1 + .5f, gh + fh - .1f, gh + fh + .7f, 1f, -.9f, ConcreteGrey);
            f.Surface = ComicSurface.Glass;
            K.LQuad(f, l, a0, a1, .1f, gh + fh - .1f, .5f, WindowGlass);
            f.Surface = ComicSurface.PaintedMetal;
            for (float a = a0 + 1.6f; a < a1 - .5f; a += 1.6f) K.LQuad(f, l, a - .05f, a + .05f, .1f, gh + fh - .1f, .52f, Ink, false);
            K.LQuad(f, l, a0, a1, 3.2f, 3.36f, .52f, Ink, false);
            K.LBox(f, l, a0 - 1f, a1 + 1f, 3.6f, 3.9f, 3.4f, 1f, ConcreteGrey);
            WallSign(f, l, W * .26f, h, .13f, "PUBLIC TRANSPORT CENTRE", .13f, ConcreteGrey, BrickBrown);
            WallSign(f, l, (a0 + a1) * .5f, gh + fh + .3f, 1.01f, "EAST PERTH", .11f, ConcreteGrey, Navy);
        }

        /// <summary>The footbridge from the island platform to the terminal: a grey glazed span on two lift towers.</summary>
        static void StationFootbridge(CqChunkDraft d, float z, float u0, float u1)
        {
            ComicMesh m = d.SlopedFrontage;
            Color wall = K.Hex("D9DEE3"), roof = K.Hex("9AA2AA");
            float xa = LX(u0), xb = LX(u1), y = 7.2f;
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V((xa + xb) * .5f, y + .5f, z), V(xa - xb, 1f, 2.6f), wall);
            m.Box(V((xa + xb) * .5f, y + 2.8f, z), V(xa - xb, .5f, 2.6f), wall);
            m.Box(V((xa + xb) * .5f, y + 3.12f, z), V(xa - xb + .4f, .14f, 3f), roof);
            m.Surface = ComicSurface.Glass;
            m.Box(V((xa + xb) * .5f, y + 1.8f, z), V(xa - xb - .2f, 1.6f, 2.4f), GlassDark);
            m.Surface = ComicSurface.PaintedMetal;
            for (float x = xb + 1.2f; x < xa; x += 2.2f)
                K.Face(m, V(x - .05f, y + 1f, z - 1.212f), V(x + .05f, y + 1f, z - 1.212f), V(x + .05f, y + 2.6f, z - 1.212f), V(x - .05f, y + 2.6f, z - 1.212f), wall, Vector3.back, false);
            foreach (float u in new[] { u0, u1 - 2f })
            {
                float xt = LX(u);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(xt, (y + 4.2f + PlatformTop) * .5f, z + 2.6f), V(2.6f, y + 4.2f - PlatformTop, 2.6f), wall);
                m.Box(V(xt, y + 4.3f, z + 2.6f), V(3f, .2f, 3f), roof);
                m.Surface = ComicSurface.Glass;
                m.Box(V(xt, (y + PlatformTop) * .5f + 1.6f, z + 2.6f), V(1.4f, y - PlatformTop + 1.4f, 2.66f), GlassDark);
                // stairs down towards the rider
                Vector3 top = V(xt, y, z - 1.3f), foot = V(xt, PlatformTop, z - 12f);
                m.Surface = ComicSurface.Concrete;
                m.Box((top + foot) * .5f, V(2f, .3f, (foot - top).magnitude), ConcreteGrey, Quaternion.LookRotation(foot - top));
                m.Surface = ComicSurface.PaintedMetal;
                foreach (float sx in new[] { -1f, 1f }) m.Beam(top + V(sx, 1f, 0), foot + V(sx, 1f, 0), .07f, StainlessDark);
            }
        }

        /// <summary>A precast noise wall behind the tracks covered in graffiti: blocks, bars and drips of colour (no words).</summary>
        static void GraffitiWall(CqChunkDraft d, float z0, float z1, float u, int seed)
        {
            ComicMesh m = d.SlopedFrontage;
            float x = LX(u), h = 3.6f;
            m.Surface = ComicSurface.Concrete;
            K.SideBox(m, -1, z0, z1, -.5f, h, u, u + .3f, K.Hex("C4C1B8"));
            K.SideBox(m, -1, z0, z1, h, h + .15f, u - .06f, u + .36f, ConcreteDark);
            var r = new System.Random(seed * 19 + 7);
            Color[] paint = { K.Hex("E2463A"), K.Hex("F6C543"), K.Hex("2A9D9A"), K.Hex("2F6FB0"), K.Hex("F28C28"), K.Hex("E85A8C"), K.Hex("8F6FD0"), K.Hex("7FC04A"), White, K.Hex("20262C") };
            m.Surface = ComicSurface.Plain;
            float xf = x + .012f;
            for (float z = z0 + .4f; z < z1 - 1.5f; )
            {
                float w = F(r, 1.4f, 3.4f), y0 = F(r, .5f, 1.3f), y1 = F(r, 2.2f, 3.3f), skew = F(r, -.5f, .5f);
                Color c = paint[r.Next(paint.Length - 1)];
                K.Face(m, V(xf, y0, z), V(xf, y0, z + w), V(xf, y1, z + w + skew), V(xf, y1, z + skew), c, Vector3.right, false);
                // an inner shape, a highlight bar and a shadow edge
                Color c2 = paint[r.Next(paint.Length)];
                K.Face(m, V(xf + .006f, y0 + .3f, z + w * .2f), V(xf + .006f, y0 + .3f, z + w * .7f), V(xf + .006f, y1 - .4f, z + w * .8f + skew * .6f), V(xf + .006f, y1 - .4f, z + w * .3f + skew * .6f), c2, Vector3.right, false);
                K.Face(m, V(xf + .01f, y1 - .28f, z + w * .1f + skew), V(xf + .01f, y1 - .28f, z + w * .6f + skew), V(xf + .01f, y1 - .14f, z + w * .6f + skew), V(xf + .01f, y1 - .14f, z + w * .1f + skew), White, Vector3.right, false);
                K.Face(m, V(xf + .003f, y0 - .12f, z + .15f), V(xf + .003f, y0 - .12f, z + w + .15f), V(xf + .003f, y0, z + w), V(xf + .003f, y0, z), K.Hex("20262C"), Vector3.right, false);
                z += w + F(r, .2f, 1.2f);
            }
        }

        /// <summary>The two river bridges crossing overhead from z0: the lower rail bridge (Goongoongup) with its gantries and
        /// a train, then the wide freeway bridge (Windan) on twin box girders; rows of piers into the river.</summary>
        static void RiverBridges(CqChunkDraft d, float z0)
        {
            ComicMesh m = d.Frontage;
            float yb = d.Y(z0 + 7f), x0 = -64f, x1 = 150f, xc = (x0 + x1) * .5f, w = x1 - x0;
            float[] piers = { -42f, 27f, 60f, 93f, 126f };
            // the rail bridge
            float ry = yb + 8.4f, rz = z0 + 2.3f;
            m.Surface = ComicSurface.Concrete;
            m.Box(V(xc, ry + .7f, rz), V(w, 1.4f, 4.6f), ConcreteGrey);
            m.Box(V(xc, ry + 1.65f, rz - 2.2f), V(w, .5f, .2f), ConcreteDark);
            foreach (float x in piers) m.Box(V(x, (ry + yb - 4f) * .5f, rz), V(1.3f, ry - yb + 4f, 3.6f), ConcreteDark);
            m.Surface = ComicSurface.Metal;
            foreach (float x in new[] { -30f, 30f, 90f })
            {
                m.Box(V(x, ry + 4.6f, rz - 2.1f), V(.22f, 6.4f, .22f), MastGrey);
                m.Box(V(x, ry + 4.6f, rz + 2.1f), V(.22f, 6.4f, .22f), MastGrey);
                m.Box(V(x, ry + 7.6f, rz), V(.2f, .3f, 4.4f), MastGrey);
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(xc, ry + 6.9f, rz), V(w, .05f, .05f), FenceBlack);
            var train = new ComicMesh();
            Emu(train, -K.Edge, 0f, 2, ASeries, true, false);
            m.Add(train, V(24f, ry + 1.1f, rz), Quaternion.Euler(0, 90, 0), Vector3.one);
            // the freeway bridge: deck, twin box girders, barriers, light poles
            float fy = yb + 9.6f, fz = z0 + 10.6f;
            m.Surface = ComicSurface.Concrete;
            m.Box(V(xc, fy + 1.5f, fz), V(w, .6f, 9f), ConcreteGrey);
            foreach (float dz in new[] { -2.3f, 2.3f })
            {
                m.Box(V(xc, fy + .6f, fz + dz), V(w, 1.3f, 3f), K.Hex("B9B5AA"));
                foreach (float x in piers) m.Box(V(x + 2.5f, (fy + yb - 4f) * .5f, fz + dz), V(1.5f, fy - yb + 4f, 1.9f), ConcreteDark);
            }
            foreach (float dz in new[] { -4.4f, 4.4f }) m.Box(V(xc, fy + 2.2f, fz + dz), V(w, .9f, .25f), K.Hex("D9D4C8"));
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -40f, 0f, 40f, 80f, 120f })
            {
                m.Cylinder(V(x, fy + 6.8f, fz - 4.4f), .1f, 9f, K.Hex("AEB6BC"), 6, .7f);
                m.Box(V(x, fy + 11.2f, fz - 3.6f), V(.1f, .1f, 1.7f), K.Hex("AEB6BC"));
                m.Box(V(x, fy + 11.14f, fz - 2.9f), V(.3f, .1f, .7f), SteelDark);
            }
            // a lorry and a car on the freeway (boxes: only their tops show over the barrier)
            m.Box(V(34f, fy + 3.6f, fz - 1.5f), V(8f, 2.6f, 2.4f), White);
            m.Box(V(39.4f, fy + 3.1f, fz - 1.5f), V(2.2f, 1.9f, 2.3f), K.Hex("2F6FB0"));
            m.Box(V(-12f, fy + 2.7f, fz + 1.8f), V(4.2f, 1f, 1.8f), K.Hex("A8452E"));
        }

        /// <summary>The power station's Art Deco office block: two storeys of cream render with a band of red face brick at
        /// the upper windows, steel windows, a stepped parapet, and the tall square corner tower with its strip of glass
        /// blocks and vertical fins.</summary>
        static void PowerAdmin(CqChunkDraft d, CqLot l)
        {
            ComicMesh f = d.Frontage;
            float W = l.W, D = l.Depth, fh = 4.3f, h = 8.6f, tw = 3.5f, th = 15.5f;
            Color render = K.Hex("EADFC4"), trim = K.Hex("F6EEDA");
            Plinth(f, l); d.Wall(l, 12f);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0f, W, 0f, h, 0f, -D, render);
            f.Surface = ComicSurface.Brick;
            K.LBox(f, l, tw, W + .03f, fh + .75f, fh + 3.2f, .04f, -D - .03f, BrickRed);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.05f, W + .05f, fh - .1f, fh + .12f, .12f, -D - .05f, trim);
            K.LBox(f, l, -.05f, W + .05f, 0f, .7f, .08f, -D - .05f, K.Hex("B9B09A"));
            float span = W - tw; int n = Mathf.Max(2, Mathf.RoundToInt(span / 3.1f));
            for (int i = 0; i < n; i++)
            {
                float ac = tw + span * (i + .5f) / n;
                if (i == n / 2)
                {
                    // the entrance: a stepped surround, a dark door, a flat hood
                    f.Surface = ComicSurface.Concrete;
                    K.LBox(f, l, ac - 1.2f, ac + 1.2f, 0f, 3.5f, .22f, 0f, trim);
                    K.LBox(f, l, ac - 1.5f, ac + 1.5f, 3.3f, 3.55f, .9f, 0f, trim);
                    f.Surface = ComicSurface.Wood;
                    K.LQuad(f, l, ac - .75f, ac + .75f, .1f, 3f, .235f, K.Hex("3A4148"));
                }
                else Win(f, l, ac, 2f, 1.2f, 3.1f, 0f, trim, 3, true, true, true);
                Win(f, l, ac, 2f, fh + 1f, fh + 2.95f, .04f, trim, 3, true, true, true);
            }
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, tw, W, h, h + .8f, .06f, -.4f, render);
            K.LBox(f, l, tw + span * .3f, tw + span * .7f, h + .8f, h + 1.4f, .08f, -.4f, render);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, tw - .05f, W + .08f, h + .8f, h + .95f, .14f, -.46f, trim);
            // the tower
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, -.25f, tw, 0f, th, .45f, -tw - .2f, render);
            f.Surface = ComicSurface.Glass;
            K.LQuad(f, l, tw * .5f - .75f, tw * .5f + .45f, 2.6f, th - 2.6f, .47f, GlassPale);
            K.Face(f, l.P(-.262f, 2.6f, -.8f), l.P(-.262f, 2.6f, -2.2f), l.P(-.262f, th - 2.6f, -2.2f), l.P(-.262f, th - 2.6f, -.8f), GlassPale, Vector3.back);
            f.Surface = ComicSurface.PaintedMetal;
            for (float y = 3.4f; y < th - 2.7f; y += .8f) K.LQuad(f, l, tw * .5f - .75f, tw * .5f + .45f, y - .025f, y + .025f, .48f, K.Hex("8FA3B5"), false);
            K.LQuad(f, l, tw * .5f - .18f, tw * .5f - .12f, 2.6f, th - 2.6f, .48f, K.Hex("8FA3B5"), false);
            f.Surface = ComicSurface.Concrete;
            foreach (float a in new[] { tw * .5f - 1.1f, tw * .5f + .8f }) K.LBox(f, l, a - .12f, a + .12f, 2f, th + .9f, .62f, .45f, trim);
            K.LBox(f, l, -.35f, tw + .1f, th, th + .35f, .55f, -tw - .3f, trim);
            K.LBox(f, l, .3f, tw - .6f, th + .35f, th + 1.2f, .2f, -tw + .3f, render);
            f.Surface = ComicSurface.PaintedMetal;
            f.Cylinder(l.P(tw * .5f - .15f, th + 3.2f, -tw * .5f), .06f, 4f, K.Hex("C9CED3"), 6);
            RoofUnit(f, l, W * .7f, -D * .5f, h, 1.6f, 1f);
        }

        /// <summary>The power station's great hall: yellow stucco with pale red bands, tall multi-paned windows between
        /// stuccoed piers, stepped roof heights (the boiler house higher behind), corrugated roofs with ridge vents.</summary>
        static void PowerHall(CqChunkDraft d, CqLot l)
        {
            ComicMesh f = d.Frontage;
            float W = l.W, D = l.Depth, h1 = 16.5f, h2 = 22f, front = 9f;
            Color pane = K.Hex("9FBCC9"), bar = K.Hex("5E6E7A");
            Plinth(f, l); d.Wall(l, 22f);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0f, W, 0f, h1, 0f, -front, StuccoYellow);
            K.LBox(f, l, 0f, W, 0f, h2, -front, -D, Mix(StuccoYellow, White, .12f));
            K.LBox(f, l, -.06f, W + .06f, 0f, 2.3f, .1f, -D - .06f, StuccoRed);
            K.LBox(f, l, -.06f, W + .06f, h1 - 1.6f, h1 - .7f, .1f, -front, StuccoRed);
            K.LBox(f, l, -.06f, W + .06f, h2 - 1.5f, h2 - .7f, -front + .1f, -D - .06f, StuccoRed);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.12f, W + .12f, h1, h1 + .3f, .2f, -front, K.Hex("F3EBD2"));
            K.LBox(f, l, -.12f, W + .12f, h2, h2 + .3f, -front + .2f, -D - .12f, K.Hex("F3EBD2"));
            int bays = Mathf.Max(3, Mathf.RoundToInt(W / 3.8f));
            float bw = W / bays;
            for (int i = 0; i <= bays; i++)
            {
                float a = Mathf.Clamp(i * bw, .45f, W - .45f);
                f.Surface = ComicSurface.Plaster;
                K.LBox(f, l, a - .45f, a + .45f, 2.3f, h1 - 1.6f, .38f, 0f, Mix(StuccoYellow, White, .2f));
            }
            for (int i = 0; i < bays; i++)
            {
                float ac = (i + .5f) * bw, hw = bw * .5f - .75f, y0 = 3.3f, y1 = h1 - 2.6f;
                f.Surface = ComicSurface.PaintedMetal;
                K.LQuad(f, l, ac - hw, ac + hw, y0, y1, .015f, Ink);
                f.Surface = ComicSurface.Glass;
                K.LQuad(f, l, ac - hw + .1f, ac + hw - .1f, y0 + .1f, y1 - .1f, .03f, i % 3 == 1 ? Mix(pane, Ink, .25f) : pane);
                f.Surface = ComicSurface.PaintedMetal;
                foreach (float t in new[] { -.33f, .33f }) K.LQuad(f, l, ac + hw * t * 2f - .03f, ac + hw * t * 2f + .03f, y0 + .1f, y1 - .1f, .04f, bar, false);
                for (float y = y0 + 1.3f; y < y1 - .5f; y += 1.3f) K.LQuad(f, l, ac - hw + .1f, ac + hw - .1f, y - .03f, y + .03f, .04f, bar, false);
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, ac - hw - .1f, ac + hw + .1f, y0 - .2f, y0, .2f, 0f, K.Hex("F3EBD2"));
                // the boiler house's upper windows
                f.Surface = ComicSurface.Glass;
                K.LQuad(f, l, ac - hw, ac + hw, h1 + 1.2f, h2 - 2.2f, -front + .02f, Mix(pane, Ink, .2f));
            }
            // the end towards the rider: two tall windows in the hall, one in the boiler house
            f.Surface = ComicSurface.PaintedMetal;
            foreach ((float o0, float o1, float top) in new[] { (-1.6f, -3.8f, h1 - 2.6f), (-5.2f, -7.4f, h1 - 2.6f), (-11.5f, -16.5f, h2 - 3f) })
            {
                f.Surface = ComicSurface.PaintedMetal;
                K.Face(f, l.P(-.02f, 3.3f, o0), l.P(-.02f, 3.3f, o1), l.P(-.02f, top, o1), l.P(-.02f, top, o0), Ink, Vector3.back);
                f.Surface = ComicSurface.Glass;
                K.Face(f, l.P(-.04f, 3.4f, o0 - .1f), l.P(-.04f, 3.4f, o1 + .1f), l.P(-.04f, top - .1f, o1 + .1f), l.P(-.04f, top - .1f, o0 - .1f), pane, Vector3.back);
                f.Surface = ComicSurface.PaintedMetal;
                for (float y = 4.6f; y < top - .5f; y += 1.3f) K.Face(f, l.P(-.05f, y - .03f, o0 - .1f), l.P(-.05f, y - .03f, o1 + .1f), l.P(-.05f, y + .03f, o1 + .1f), l.P(-.05f, y + .03f, o0 - .1f), bar, Vector3.back, false);
            }
            f.Surface = ComicSurface.Metal;
            K.LSlope(f, l, -.2f, W + .2f, h1 + .3f, h1 + 1.9f, .3f, -front * .5f, -front, RoofGrey);
            K.LSlope(f, l, -.2f, W + .2f, h2 + .3f, h2 + 2.2f, -front + .1f, -(front + D) * .5f, -D - .2f, RoofGrey);
            for (float a = W * .2f; a < W; a += W * .3f) K.LBox(f, l, a - 1f, a + 1f, h2 + 1.8f, h2 + 2.9f, -(front + D) * .5f + .7f, -(front + D) * .5f - .7f, PlantGrey);
        }

        /// <summary>Bare rusty steel portal frames (what is left of the sheds): columns, rafters, two purlins.</summary>
        static void SteelFrames(ComicMesh m, Vector3 p, float width, float length)
        {
            m.Surface = ComicSurface.Metal;
            float h = 6.5f, hw = width * .5f;
            int n = Mathf.Max(2, Mathf.RoundToInt(length / 4.2f));
            for (int i = 0; i <= n; i++)
            {
                float z = length * i / n;
                foreach (float sx in new[] { -hw, hw })
                {
                    m.Box(p + V(sx, h * .5f, z), V(.26f, h, .3f), Rust);
                    m.Beam(p + V(sx, h, z), p + V(0, h + 1.9f, z), .22f, Mix(Rust, Ink, .15f));
                    m.Box(p + V(sx, .12f, z), V(.6f, .24f, .6f), ConcreteDark);
                }
            }
            foreach (float sx in new[] { -hw, 0f, hw }) m.Box(p + V(sx, sx == 0f ? h + 1.95f : h + .05f, length * .5f), V(.14f, .14f, length + .4f), Rust);
            m.Beam(p + V(-hw, .3f, 0), p + V(-hw, h - .3f, length / n), .1f, Rust);
            m.Beam(p + V(-hw, h - .3f, 0), p + V(-hw, .3f, length / n), .1f, Rust);
        }

        /// <summary>The Impossible Triangle of the East Parade roundabout (13.5 m): a polished Penrose triangle on a short
        /// mast: three square beams, each passing in front of the next at the corners.</summary>
        static void ImpossibleTriangle(ComicMesh m, Vector3 p)
        {
            Color steel = K.Hex("D3D8DC"), shade = K.Hex("9AA2AA");
            const float s = 11.5f, t = 1.15f;
            Vector3 a = p + V(-s * .5f, 3.2f, 0), b = p + V(s * .5f, 3.2f, 0), c = p + V(0, 3.2f + s * .866f, 0);
            m.Surface = ComicSurface.Metal;
            void BeamBox(Vector3 u, Vector3 v, float dz, Color col)
            {
                Vector3 dir = (v - u).normalized;
                m.Box((u + v) * .5f + V(0, 0, dz), V(t, t, (v - u).magnitude + t), col, Quaternion.LookRotation(dir, Vector3.forward));
            }
            BeamBox(a, b, -.12f, steel);
            BeamBox(b, c, 0f, shade);
            BeamBox(c, a, .12f, Mix(steel, White, .3f));
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(p + V(0, 1.4f, 0), .35f, 2.8f, ConcreteDark, 8);
            m.Cylinder(p + V(0, .15f, 0), 2.2f, .3f, ConcreteGrey, 10);
        }

        // ---------------------------------------------------------------- East Perth obstacles and animals

        // A platform luggage trolley piled with suitcases: too tall to jump.
        static ComicMesh LuggageTrolleyMesh()
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, .32f, 0), V(1f, .08f, 1.8f), K.Hex("2F6FB0"));
            foreach (float z in new[] { -.7f, .7f }) foreach (float x in new[] { -.42f, .42f })
                m.Cylinder(V(x, .14f, z), .14f, .08f, Ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            foreach (float x in new[] { -.45f, .45f }) m.Box(V(x, .85f, -.86f), V(.05f, 1.1f, .05f), K.Hex("C9CED3"));
            m.Box(V(0, 1.4f, -.86f), V(.95f, .05f, .05f), K.Hex("C9CED3"));
            m.Surface = ComicSurface.Fabric;
            m.Box(V(0, .6f, .25f), V(.85f, .5f, 1.2f), K.Hex("A8452E"));
            m.Box(V(-.1f, 1.02f, .35f), V(.7f, .34f, .9f), K.Hex("2A9D9A"));
            m.Box(V(.05f, 1.33f, .2f), V(.55f, .28f, .6f), K.Hex("E6B93C"));
            m.Box(V(0, .62f, -.55f), V(.8f, .55f, .3f), K.Hex("3A4148"));
            m.Surface = ComicSurface.Plain;
            m.Box(V(0, .6f, .25f), V(.87f, .06f, 1.22f), Ink);
            m.Box(V(-.1f, 1.02f, .35f), V(.72f, .05f, .92f), Ink);
            return m;
        }

        // Two suitcases standing on the path (can be jumped, and punched out of the way).
        static ComicMesh SuitcasesMesh()
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.Fabric;
            m.Box(V(-.25f, .35f, 0), V(.46f, .66f, .3f), K.Hex("2F6FB0"));
            m.Box(V(.26f, .28f, .05f), V(.42f, .52f, .28f), K.Hex("E2463A"), Quaternion.Euler(0, 20, 0));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(-.25f, .72f, 0), V(.2f, .08f, .05f), Ink);
            m.Box(V(.26f, .58f, .05f), V(.18f, .07f, .05f), Ink, Quaternion.Euler(0, 20, 0));
            m.Box(V(-.25f, .35f, 0), V(.48f, .05f, .32f), Ink);
            foreach (float x in new[] { -.42f, -.08f }) m.Box(V(x, .03f, 0), V(.08f, .06f, .3f), Ink);
            return m;
        }

        // The shared path's bollard: a yellow post with black bands and a reflective cap.
        static ComicMesh PathBollardMesh()
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(V(0, .5f, 0), .11f, 1f, K.Hex("F2C230"), 8);
            m.Cylinder(V(0, .78f, 0), .118f, .1f, Ink, 8);
            m.Cylinder(V(0, .56f, 0), .118f, .1f, Ink, 8);
            m.Cylinder(V(0, 1.04f, 0), .12f, .1f, White, 8, .6f);
            m.Cylinder(V(0, .04f, 0), .2f, .08f, Ink, 8);
            return m;
        }

        // Black swan: black body with raised, ruffled wing feathers and white flight-feather tips, a long S-curved neck,
        // a red bill with a pale band.
        static ComicMesh SwanMesh()
        {
            var m = new ComicMesh();
            Color black = K.Hex("20262C"), dark = K.Hex("2E343B");
            m.Surface = ComicSurface.Fabric;
            m.Box(V(0, .42f, .05f), V(.42f, .34f, .8f), black, Quaternion.Euler(-6, 0, 0));
            m.Box(V(0, .57f, .12f), V(.48f, .2f, .62f), dark, Quaternion.Euler(-10, 0, 0));
            m.Box(V(0, .64f, .42f), V(.34f, .14f, .3f), dark, Quaternion.Euler(-28, 0, 0));
            m.Box(V(0, .5f, .52f), V(.3f, .05f, .1f), White, Quaternion.Euler(-28, 0, 0));
            Vector3 a = V(0, .5f, -.3f), b = V(0, .84f, -.52f), c = V(0, 1.1f, -.36f), h = V(0, 1.3f, -.46f);
            m.Beam(a, b, .13f, black);
            m.Beam(b, c, .115f, black);
            m.Beam(c, h, .1f, black);
            m.Box(h + V(0, .03f, -.07f), V(.13f, .13f, .2f), black);
            m.Surface = ComicSurface.Plastic;
            m.Box(h + V(0, -.01f, -.24f), V(.08f, .07f, .16f), K.Hex("D9432F"));
            m.Box(h + V(0, -.01f, -.3f), V(.085f, .075f, .03f), White);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.07f, .07f }) m.Box(h + V(x, .06f, -.1f), V(.02f, .03f, .03f), K.Hex("D9432F"));
            foreach (float x in new[] { -.1f, .1f })
            {
                m.Box(V(x, .13f, .06f), V(.05f, .26f, .05f), K.Hex("3A4148"));
                m.Box(V(x, .02f, 0f), V(.12f, .04f, .2f), K.Hex("3A4148"));
            }
            return m;
        }

        // Australian magpie: black with a white nape, shoulders and rump, a pale bill.
        static ComicMesh MagpieMesh()
        {
            ComicMesh m = Bird(.8f, K.Hex("20262C"), White, White, K.Hex("20262C"), K.Hex("C9CED3"), .1f, .09f, K.Hex("3A4148"), .12f);
            return m;
        }

        // ---------------------------------------------------------------- East Perth far scenery

        /// <summary>Optus Stadium: a broad elliptical bowl on a pale plinth, wrapped in horizontal bands of bronze in three
        /// tones, a dark recess, then the white halo roof floating above with its triangulated edge.</summary>
        static void Stadium(ComicMesh m, Vector3 c, float rx, float rz, Color haze, float s = 1f)
        {
            Color H(Color col) => haze.a > 0f ? Color.Lerp(col, haze, .3f) : col;
            const int n = 28;
            Vector3 P(int i, float y, float grow) { float a = i * Mathf.PI * 2f / n; return c + V(Mathf.Cos(a) * (rx + grow * s), y * s, Mathf.Sin(a) * (rz + grow * s)); }
            (float y0, float y1, float g0, float g1, Color col)[] bands =
            {
                (0f, 6f, 2f, 1f, K.Hex("E6DCC2")), (6f, 13f, 0f, 1.5f, BronzeDark), (13f, 20f, 1.5f, 3f, Bronze), (20f, 27f, 3f, 4f, BronzeLight),
                (27f, 33f, 4f, 3f, Bronze), (33f, 35.5f, -2f, -2f, K.Hex("3A3028")), (35.5f, 41f, 5f, 8f, White), (41f, 42f, 8f, -6f, K.Hex("EDEDEA")),
            };
            for (int i = 0; i < n; i++)
            {
                Vector3 outward = (P(i, 0, 1f) + P(i + 1, 0, 1f)) * .5f - c;
                foreach (var b in bands)
                {
                    m.Surface = b.col == White ? ComicSurface.Fabric : ComicSurface.Metal;
                    Color col = b.col == Bronze && i % 2 == 0 ? Mix(Bronze, BronzeLight, .35f) : b.col;
                    K.Face(m, P(i, b.y0, b.g0), P(i + 1, b.y0, b.g0), P(i + 1, b.y1, b.g1), P(i, b.y1, b.g1), H(col), b.y1 >= 42f ? Vector3.up : outward, false);
                }
                // the halo's triangulated truss edge and the roof's underside
                m.Surface = ComicSurface.PaintedMetal;
                m.Beam(P(i, 35.5f, 5f), P(i + 1, 41f, 8f), .5f * s, H(K.Hex("C9CED3")));
                K.Face(m, P(i, 35.5f, 5f), P(i + 1, 35.5f, 5f), P(i + 1, 36.5f, -8f), P(i, 36.5f, -8f), H(K.Hex("D9DEE3")), Vector3.down, false);
            }
        }

        /// <summary>Matagarup Bridge from a to b: three spans hung from arches, the tall central one made of two wishbones
        /// leaning across each other, one black, one white (the two swans); a slim curved deck, white cables.</summary>
        static void Matagarup(ComicMesh m, Vector3 a, Vector3 b, Color haze, float s = 1f)
        {
            Color H(Color col) => haze.a > 0f ? Color.Lerp(col, haze, .22f) : col;
            Color black = K.Hex("23282E"), white = K.Hex("F2F2EE");
            Vector3 along = (b - a).normalized, side = Vector3.Cross(Vector3.up, along);
            float L = (b - a).magnitude;
            Vector3 DeckAt(float t) => Vector3.Lerp(a, b, t) + V(0, (9f + 5f * Mathf.Sin(Mathf.PI * t)) * s, 0) + side * (6f * s * Mathf.Sin(Mathf.PI * 2f * t));
            const int nd = 16;
            for (int i = 0; i < nd; i++)
            {
                Vector3 p0 = DeckAt((float)i / nd), p1 = DeckAt((float)(i + 1) / nd);
                m.Surface = ComicSurface.Concrete;
                m.Box((p0 + p1) * .5f, V(7f * s, 1.1f * s, (p1 - p0).magnitude + .3f), H(K.Hex("DADDDD")), Quaternion.LookRotation(p1 - p0));
            }
            foreach (float t in new[] { .27f, .73f })
            {
                Vector3 p = Vector3.Lerp(a, b, t);
                m.Box(p + V(0, 4f * s, 0), V(5f * s, 10f * s, 3f * s), H(K.Hex("C9C5BA")));
            }
            // arches: (start t, end t, height, lean)
            foreach ((float t0, float t1, float h, float lean) in new[] { (.02f, .27f, 34f, 9f), (.27f, .73f, 72f, 16f), (.73f, .98f, 34f, 9f) })
            {
                const int n = 12;
                for (int k = 0; k < 2; k++)
                {
                    float sgn = k == 0 ? 1f : -1f;
                    Color col = H(k == 0 ? black : white);
                    Vector3 ArchAt(float u)
                    {
                        // each wishbone starts on one side of the deck, leans across and comes down on the other
                        float sn = Mathf.Sin(Mathf.PI * u);
                        Vector3 basePos = Vector3.Lerp(a, b, Mathf.Lerp(t0, t1, u));
                        return basePos + V(0, (6f + h * Mathf.Pow(sn, .8f)) * s, 0) + side * sgn * lean * s * Mathf.Cos(Mathf.PI * u) + along * sgn * L * (t1 - t0) * .04f * sn;
                    }
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 p0 = ArchAt((float)i / n), p1 = ArchAt((float)(i + 1) / n);
                        float r = (h > 50f ? 2.1f : 1.3f) * s;
                        m.Surface = ComicSurface.PaintedMetal;
                        Limb(m, p0, p1 + (p1 - p0).normalized * .4f, r, r, col, 6);
                    }
                    for (int i = 2; i < n - 1; i += 2)
                    {
                        float u = (float)i / n;
                        m.Beam(ArchAt(u), DeckAt(Mathf.Lerp(t0, t1, u)) + V(0, .6f * s, 0), .35f * s, H(white));
                    }
                }
            }
        }

        /// <summary>The horizon around East Perth: the Swan River on the right with the Burswood shore's parkland and trees;
        /// roofs and trees on the railway side. (Optus Stadium and Matagarup Bridge are built with the foreshore chunk, so
        /// they grow as you ride towards them.)</summary>
        static ComicMesh EastPerthFar(float grade)
        {
            var m = new ComicMesh();
            var r = new System.Random(230);
            Color haze = K.Hex("C3DAEA");
            const float z0 = -220f, z1 = 820f, shore = 150f;
            FarStrip(m, K.Edge, K.Edge + BankWidth + 1.2f, 330f, z1, K.Kerb - .03f, Lawn);
            FarStrip(m, K.Edge + BankWidth + 1.2f, 150f, 330f, z1, ESea - .05f, River);
            FarStrip(m, shore, shore + 8f, z0, z1, ESea + .5f, Haze(K.Hex("D9C9A0"), 240f, haze));
            FarStrip(m, shore + 8f, 520f, z0, z1, ESea + 1f, Haze(K.Hex("7FA85A"), 300f, haze));
            FarStrip(m, 520f, 1400f, z0, z1, ESea + 1f, Haze(K.Hex("8FA567"), 700f, haze));
            FarStrip(m, -K.Edge, -30f, 330f, z1, .08f, BallastC);
            FarStrip(m, -30f, -60f, 330f, z1, .08f, K.Hex("8E8A80"));
            FarStrip(m, -60f, -400f, z0, z1, -.3f, Haze(K.Hex("9E9C92"), 200f, haze));
            FarStrip(m, -400f, -1400f, z0, z1, -.35f, Haze(K.Hex("9AAE7A"), 700f, haze));
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 70; i++)
            {
                float x = F(r, 40f, shore - 6f), z = F(r, 340f, 800f), len = F(r, 3f, 8f);
                K.Face(m, V(x, ESea - .03f, z), V(x, ESea - .03f, z + .6f), V(x + len, ESea - .03f, z + .6f), V(x + len, ESea - .03f, z), Haze(RiverLight, x, haze), Vector3.up, false);
            }
            // the Burswood shore: parkland trees, the stadium, the bridge
            for (float z = z0; z < z1; z += F(r, 14f, 24f))
            {
                m.Surface = ComicSurface.Foliage;
                m.Ellipsoid(V(F(r, shore + 14f, shore + 60f), ESea + 7f, z), V(F(r, 16f, 26f), F(r, 11f, 16f), F(r, 16f, 24f)), Haze(Mix(GumLeaf, FigMid, F(r, 0f, 1f)), 300f, haze));
            }
            FarTown(m, r, 1, shore + 190f, 420f, z0, z1, 10f, 40f, haze, 30f, false);
            FarTown(m, r, -1, 70f, 120f, z0, z1, 6f, 14f, haze, 14f);
            FarTown(m, r, -1, 140f, 380f, z0, z1, 8f, 26f, haze, 22f);
            m.Shear(grade);
            return m;
        }
    }
}
