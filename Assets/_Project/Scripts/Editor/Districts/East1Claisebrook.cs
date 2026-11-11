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
    /// <summary>CLAISEBROOK (order 220): the station and railcar depot, Royal St, Claisebrook Cove. See East1Builder.</summary>
    public static partial class East1Builder
    {
        public const string ClaisebrookName = "Claisebrook";
        public static readonly string[] ClaisebrookChunkIds = { "Station", "Depot", "RoyalStreet", "Cove", "Canal" };
        /// <summary>East Perth is nearly flat: -1.6 m per 40 m chunk.</summary>
        public const float ClaisebrookDrop = -1.6f;
        // The canal jump (timber footbridge hump + road-wide SPLASH gap). For gameplay:
        public const float CanalRampStart = 8f, CanalRise = 6f, CanalHeight = 1.4f, CanalTop = 3f, CanalGap = 5f;
        const float CSea = -1.7f, CoveWidth = 40f;
        /// <summary>The middle of Claisebrook's far side platform, in u (behind track 2; the corridor's back fence stands
        /// along its back edge).</summary>
        const float FarPlatform = T2 + E.PlatformEdge + 1.6f;
        static readonly Color YardGrey = K.Hex("8E8A80");
        static readonly Color BrickPaving = K.Hex("CFA088"), Promenade = K.Hex("E6D8B6"), PromenadeFlag = K.Hex("D6C7A2");
        static readonly Color[] RenderColours = { K.Hex("F2E6C4"), K.Hex("D9A441"), K.Hex("F0BF9A"), K.Hex("CF7A4E"), K.Hex("EFE3C8"), K.Hex("3E9A96"), K.Hex("E6C98A") };
        static readonly Color[] DoorColours = { K.Hex("2F5A3A"), K.Hex("7A2E2A"), K.Hex("2A3E6E"), K.Hex("20262C") };

        static void BuildClaisebrook(TrackTheme theme, bool atlas)
        {
            string n = ClaisebrookName, patterns = DataFolder + "/claisebrook";
            K.Begin(Meshes(n), Prefabs(n), DataFolder, patterns);
            Obstacle bollard = K.SaveObstacle(Pf(n, "OBS_Claisebrook_Bollard"), "OBS_Claisebrook_Bollard", Make(m => CoveBollard(m, Vector3.zero)), new Vector3(.5f, 1f, .5f), ObstacleKind.Thing, true, false, 1f);
            Obstacle table = K.SaveObstacle(Pf(n, "OBS_Claisebrook_CafeTable"), "OBS_Claisebrook_CafeTable", Make(m => K.CafeSet(m, Vector3.zero, K.Hex("2F6B66"))), new Vector3(1.9f, 1.05f, .9f), ObstacleKind.Thing, false, true, .8f);
            Obstacle rope = K.SaveObstacle(Pf(n, "OBS_Claisebrook_RopeCrate"), "OBS_Claisebrook_RopeCrate", RopeCrateMesh(), new Vector3(1f, .8f, .9f), ObstacleKind.Thing, true, true, 1f);
            Obstacle board = K.SaveObstacle(Pf(n, "OBS_Claisebrook_MenuBoard"), "OBS_Claisebrook_MenuBoard", MenuBoardMesh(), new Vector3(.8f, 1.1f, .7f), ObstacleKind.Thing, true, true, .9f);
            GameObject duck = SaveAnimal(Pf(n, "ANM_Claisebrook_Duck"), "ANM_Claisebrook_Duck", DuckMesh(), new Vector3(0, .2f, 0), new Vector3(.35f, .4f, .55f), .5f, true);
            GameObject cormorant = SaveAnimal(Pf(n, "ANM_Claisebrook_Cormorant"), "ANM_Claisebrook_Cormorant", CormorantMesh(), new Vector3(0, .4f, 0), new Vector3(.4f, .8f, .6f), 0f, true);
            Ramp gangway = K.SaveRamp(Pf(n, "RMP_Claisebrook_Gangway"), "RMP_Claisebrook_Gangway", 3f, 5f, 1.3f, 1.5f, 1f, .5f, DeckLight, Deck, false);

            K.WriteChunk(CEntry(), ChunkPath(n, "Entry"));
            K.WriteChunk(CStation(), ChunkPath(n, "Station"));
            K.WriteChunk(CDepot(), ChunkPath(n, "Depot"));
            K.WriteChunk(CRoyalStreet(), ChunkPath(n, "RoyalStreet"));
            K.WriteChunk(CCove(), ChunkPath(n, "Cove"));
            K.WriteChunk(CCanal(), ChunkPath(n, "Canal"));
            K.WriteChunk(CExit(), ChunkPath(n, "Exit"));
            // The same horizon as McIver: one far-scenery prefab for both (built by BuildMciver), so it stays up across
            // their boundary.
            GameObject far = AssetDatabase.LoadAssetAtPath<GameObject>(Pf(MciverName, InnerEastFarName));
            if (far == null) throw new InvalidOperationException("Build McIver first: " + InnerEastFarName + " is the far scenery of both districts.");

            GameObject cyclist = Cyclist();
            var own = new List<HazardPattern>
            {
                K.Pattern(patterns, "HZ_Claisebrook_Ducks", 45, 0, 1f, true, 30, (duck, -2.6f, 22f, 0f), (duck, -1.7f, 23.2f, 0f), (duck, -.8f, 24.4f, 0f), (duck, .1f, 25.6f, 0f)),
                K.Pattern(patterns, "HZ_Claisebrook_Bollards", 40, 0, .8f, false, 30, (bollard.gameObject, -2.6f, 20f, 0f), (bollard.gameObject, 0f, 20f, 0f), (bollard.gameObject, 2.6f, 20f, 0f)),
                K.Pattern(patterns, "HZ_Claisebrook_Alfresco", 45, 1, .8f, true, 30, (table.gameObject, -2.6f, 20f, 0f), (board.gameObject, 0f, 24f, 0f), (table.gameObject, 2.6f, 29f, 0f)),
            };
            if (cyclist != null) own.Add(K.Pattern(patterns, "HZ_Claisebrook_Cyclists", 50, 0, 1f, true, 30, (cyclist, -2.4f, 24f, 0f), (cyclist, -1.2f, 31f, 0f)));
            own.AddRange(ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_Crossing", "HZ_SidewalkStroll", "HZ_PoliceStop"));
            var people = new List<GameObject>(K.People(theme, "Barista", "Barista", "Tourist", "Tourist", "Jogger", "Jogger", "DogWalker", "DogWalker", "Elder", "Kid", "Shopper", "Office"));
            people.Add(duck); people.Add(duck); people.Add(cormorant);
            WriteDistrict(n, "claisebrook", "CLAISEBROOK", 220, new Vector2(360f, 440f), ClaisebrookChunkIds,
                new[] { bollard, table, rope, board }, new[] { rope, bollard }, own.ToArray(), people.ToArray(), new[] { gangway }, 1.1f, far,
                new Look
                {
                    // warm render and limestone by the water, clear light
                    Fog = new Color(.78f, .87f, .93f), SkyTint = new Color(.99f, 1f, 1f), AmbSky = new Color(.69f, .84f, .98f),
                    AmbEquator = new Color(.90f, .87f, .80f), AmbGround = new Color(.72f, .64f, .52f), Sun = new Color(1f, .96f, .87f), SunIntensity = 1.45f,
                });
            Debug.Log("[Districts/Claisebrook] " + string.Join("; ", K.Report));
            if (atlas)
                K.RenderAtlas("art-src/districts/claisebrook/props-atlas.png", new List<(string, GameObject, ComicMesh)>
                {
                    ("BSeries", null, Make(m => Emu(m, 0f, -19f, 2, BSeries))),
                    ("Footbridge", null, Make(m => { var d = new CqChunkDraft("a", 0f); Footbridge(d, 10f); m.Add(d.SlopedFrontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("DepotShed", null, Make(m => { var d = new CqChunkDraft("a", 0f); DepotShed(d, 0f, 17f, 0f, 30f); m.Add(d.SlopedFrontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("YardLight", null, Make(m => YardLight(m, Vector3.zero))),
                    ("Townhouses", null, Make(m => { var d = new CqChunkDraft("a", 0f); TownhouseRow(d, -1, -10f, 10f, 6f, 3, false); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("CafeTownhouses", null, Make(m => { var d = new CqChunkDraft("a", 0f); TownhouseRow(d, -1, -10f, 10f, 6f, 8, true); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("CoveApartments", null, Make(m => { var d = new CqChunkDraft("a", 0f); CoveApartments(d, new CqLot(-1, -9f, 9f, 6f, 14f, 0f), 5, K.Hex("F2E6C4"), Teal); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("Warehouse", null, Make(m => { var d = new CqChunkDraft("a", 0f); Warehouse(d, new CqLot(-1, -11f, 11f, 6f, 14f, 0f)); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("TrafalgarBridge", null, Make(m => { var d = new CqChunkDraft("a", 0f); TrafalgarBridge(d, 0f, -20f, 20f); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("VictoriaGardensShelter", null, Make(m => VictoriaShelter(m, Vector3.zero))),
                    ("Cruiser", null, Make(m => Cruiser(m, Vector3.zero, 0f, Navy))),
                    ("CoveLamp", null, Make(m => PathLight(m, Vector3.zero))),
                    ("Bollard", bollard.gameObject, null),
                    ("CafeTable", table.gameObject, null),
                    ("RopeCrate", rope.gameObject, null),
                    ("MenuBoard", board.gameObject, null),
                    ("Duck", duck, null),
                    ("Cormorant", cormorant, null),
                    ("Gangway", gangway.gameObject, null),
                });
        }

        // ---------------------------------------------------------------- Claisebrook chunks

        /// <summary>Claisebrook's own street: asphalt with brick-paved sidewalks.</summary>
        static E.StreetLook CBrick => E.StreetLook.Street(BrickPaving);
        /// <summary>The cove promenade's paving: cream with limestone flag side strips.</summary>
        static E.StreetLook CProm => E.StreetLook.Paved(Promenade, PromenadeFlag, LimestoneCap);

        static void CStreet(CqChunkDraft d) => E.Street(d, CBrick);

        /// <summary>The cove promenade (PedestrianMall): cream paving with limestone flag sidewalks and darker inlaid bands.
        /// threshold: the band of setts at its start (after a street chunk).</summary>
        static void CPromenade(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f, bool threshold = false)
        {
            d.Type = RoadType.PedestrianMall;
            E.Street(d, CProm, null, -1f, skipFrom, skipTo);
            if (threshold) E.Threshold(d, .35f);
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            for (float z = 2f; z < d.Length - 1f; z += 6f)
            {
                if (skipFrom >= 0f && z + 1f > skipFrom - 6.5f && z < skipTo + .5f) continue;
                K.Face(m, V(-K.Half + .3f, .022f, z), V(-K.Half + .3f, .022f, z + .5f), V(K.Half - .3f, .022f, z + .5f), V(K.Half - .3f, .022f, z), K.Hex("C9A27A"), Vector3.up, false);
                K.Face(m, V(0, .024f, z - .9f), V(-.7f, .024f, z + .25f), V(0, .024f, z + 1.4f), V(.7f, .024f, z + .25f), K.Hex("B5553F"), Vector3.up, false);
            }
        }

        static CqChunkDraft CEntry()
        {
            // From McIver: the railway runs on beside the street. The boundary surface (pale flagstones) gives way to
            // Claisebrook's brick paving at a band of setts; the line's street edge on the right, then the first townhouses.
            var d = new CqChunkDraft("Claisebrook_Entry", ClaisebrookDrop);
            E.Street(d, E.StreetLook.Boundary, CBrick, 12f);
            E.Corridor(d, RailSide, 0f, 40f, new E.CorridorOptions { Beyond = YardGrey });
            Emu(d.SlopedFrontage, T2, .8f, 2, BSeries);
            MBackdrop(d, 0f, 40f, 61, 14f, 34f);
            OuterEdge(d, 0f, 14f, BrickPaving, 3, true);
            Ground(d, 1, 14f, 40f, 0f, 60f, BrickPaving, ComicSurface.Paving);
            TownhouseRow(d, 1, 15f, 39.5f, K.Edge + 2.5f, 1, false);
            DeepTownhouses(d, 1, 14f, 40f, 18f, 2);
            K.PlaneTree(d.Props, d.Collisions, V(5.6f, d.Ground(21f), 21f), .85f);
            MLights(d, 32f, 20f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CExit()
        {
            // The exit follows Royal St or the cove promenade (the district's length decides): a band of setts at its
            // start takes either paving, then Claisebrook's brick-paved street, and at a second band the line's boundary
            // surface. The railway on the left with parkland gums behind it; a last row of townhouses, then the line's
            // street edge on the right.
            var d = new CqChunkDraft("Claisebrook_Exit", ClaisebrookDrop);
            E.Street(d, CBrick, E.StreetLook.Boundary, 28f);
            E.Threshold(d, .35f);
            E.Corridor(d, RailSide, 0f, 40f, new E.CorridorOptions { Beyond = Lawn });
            Gum(d.Props, V(-(K.Edge + 18f), d.Ground(12f), 12f), 11f, 3);
            Gum(d.Props, V(-(K.Edge + 21f), d.Ground(24f), 24f), 9f, 4);
            Gum(d.Props, V(-(K.Edge + 18.5f), d.Ground(35f), 35f), 10f, 6);
            DeepTownhouses(d, -1, 0f, 40f, 30f, 4);
            Ground(d, 1, 0f, 26f, 0f, 60f, BrickPaving, ComicSurface.Paving);
            TownhouseRow(d, 1, .5f, 25.5f, K.Edge + 2.5f, 5, false);
            DeepTownhouses(d, 1, 0f, 26f, 18f, 7);
            OuterEdge(d, 26f, 40f, BrickPaving, 8, true);
            MLights(d, 9f, 21f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CStation()
        {
            // Claisebrook station on the line's two tracks: two side platforms (the far one with a flat grey canopy, the
            // corridor's back fence as its railing), the white enclosed footbridge with its long ramp, an A-series set at
            // platform 1. The platforms stop short of the chunk's ends, where the corridor is the plain standard: this
            // chunk also follows the canal chunk when the district's chunks come round again (hence the band of setts).
            var d = new CqChunkDraft("Claisebrook_Station", ClaisebrookDrop);
            CStreet(d);
            E.Threshold(d, .35f);
            E.Corridor(d, RailSide, 0f, 40f, new E.CorridorOptions { Beyond = YardGrey });
            MBackdrop(d, 0f, 40f, 71, 14f, 30f);
            Platform(d, 1f, T1 - E.PlatformEdge, 3f, 37f, false, true);
            Platform(d, T2 + E.PlatformEdge, FarPlatform + 1.6f, 3f, 37f, true, false);
            Footbridge(d, 12f);
            FlatCanopy(d, FarPlatform, 24f, 12f, 3f, K.Hex("B9C0C6"), K.Hex("6E747A"));
            PlatformSign(d, 2.9f, 8.2f, "CLAISEBROOK", .085f);
            PlatformSign(d, FarPlatform, 21.5f, "CLAISEBROOK", .08f);
            Emu(d.SlopedFrontage, T1, .8f, 2, ASeries);
            ValidatorPoles(d, 2.8f, 35.5f);
            Ground(d, 1, 0f, 40f, 0f, 60f, BrickPaving, ComicSurface.Paving);
            CoveApartments(d, new CqLot(1, 1f, 20f, K.Edge + 3f, 14f, d.Base(10.5f)), 5, K.Hex("F2E6C4"), Teal);
            TownhouseRow(d, 1, 21f, 39.5f, K.Edge + 2.5f, 11, false);
            DeepTownhouses(d, 1, 0f, 40f, 20f, 12);
            Shrubs(d.Props, V(K.Edge + 1.3f, d.Ground(4f), 2f), 8f, 13);
            K.PlaneTree(d.Props, d.Collisions, V(5.6f, d.Ground(22f), 22f), .85f);
            K.PropFromKit(d.Props, d.Collisions, "BikeRack", V(-6.7f, d.Ground(33f), 33f), 90f);
            MLights(d, 10f, 28f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CDepot()
        {
            // The Claisebrook railcar depot behind the line's two tracks, all inside this chunk (its ends are the plain
            // corridor): a wider ballast yard, a siding off track 2 along the long white shed (brown roof, open doors),
            // three storage roads from their buffer stops into the shed with sets going in, yard floodlights. The old
            // brick warehouse (now apartments) across the road.
            var d = new CqChunkDraft("Claisebrook_Depot", ClaisebrookDrop);
            CStreet(d);
            E.Corridor(d, RailSide, 0f, 40f, new E.CorridorOptions { BedTo = 40f, WideFrom = 1f, WideTo = 39.5f, Beyond = YardGrey });
            const float siding = 15.5f, road3 = 20.33f, road4 = 25f, stub = 29.67f;     // u: 23.5, 28.33, 33 and 37.67 from the road centre line
            E.TrackShift(d, RailSide, E.Track2, K.Edge + siding, 1.5f, 13.5f);
            E.Track(d, RailSide, K.Edge + siding, 13.5f, 38f);
            E.Track(d, RailSide, K.Edge + road3, 4f, 23f);
            E.Track(d, RailSide, K.Edge + road4, 7f, 23f);
            E.Track(d, RailSide, K.Edge + stub, 12f, 23f);
            ComicMesh m = d.SlopedFrontage;
            E.BufferStop(m, RailSide, K.Edge + siding, 38.3f, -1);
            E.BufferStop(m, RailSide, K.Edge + road3, 3.8f);
            E.BufferStop(m, RailSide, K.Edge + road4, 6.8f);
            E.BufferStop(m, RailSide, K.Edge + stub, 11.8f);
            Emu(m, road3, 5f, 1, BSeries, true, false, true, false);       // going into the shed
            Emu(m, road4, 8f, 1, ASeries, true, false, true, false);
            DepotShed(d, 18f, 32f, 22f, 39.5f);
            YardLight(m, V(LX(27.3f), .1f, 6f), 15f);
            YardLight(m, V(LX(13.1f), .1f, 31f), 15f);
            MBackdrop(d, 0f, 20f, 81, 12f, 24f);
            Ground(d, 1, 0f, 40f, 0f, 60f, BrickPaving, ComicSurface.Paving);
            Warehouse(d, new CqLot(1, 1f, 23f, K.Edge + 1.5f, 14f, d.Base(12f)));
            TownhouseRow(d, 1, 24f, 39.5f, K.Edge + 2.5f, 21, false);
            DeepTownhouses(d, 1, 0f, 40f, 20f, 22);
            JacarandaTree(d.Props, V(K.Edge + 1.2f, d.Ground(24f), 23.6f), 7.5f, 4);
            K.PlaneTree(d.Props, d.Collisions, V(5.6f, d.Ground(30f), 30f), .85f);
            MLights(d, 8f, 26f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CRoyalStreet()
        {
            // Royal St beside the railway: cafes and shops under render-and-tile townhouses and alfresco umbrellas on the
            // right, an apartment block, plane trees; a B-series set passing; more townhouses across the tracks.
            var d = new CqChunkDraft("Claisebrook_RoyalStreet", ClaisebrookDrop);
            CStreet(d);
            E.Corridor(d, RailSide, 0f, 40f, new E.CorridorOptions { Beyond = BrickPaving });
            Emu(d.SlopedFrontage, T2, .8f, 2, BSeries);
            Ground(d, 1, 0f, 40f, 0f, 60f, BrickPaving, ComicSurface.Paving);
            TownhouseRow(d, -1, 1f, 39.5f, E.CorridorEnd + 1f, 31, true);
            TownhouseRow(d, 1, .5f, 19.5f, K.Edge + 4.5f, 35, true);
            CoveApartments(d, new CqLot(1, 20.5f, 39.5f, K.Edge + 3f, 14f, d.Base(30f)), 4, K.Hex("F0BF9A"), K.Hex("CF7A4E"));
            Color[] canopies = { White, K.Hex("E2463A"), White, K.Hex("2A9D9A") };
            int k = 0;
            for (float z = 3f; z < 19f; z += 5.2f) K.CafeUmbrella(d.Props, null, V(K.Edge + 2.2f, d.Ground(z), z), canopies[k++ % canopies.Length]);
            Shrubs(d.Props, V(K.Edge + 1.3f, d.Ground(26f), 22f), 7f, 33);
            DeepTownhouses(d, -1, 0f, 40f, 30f, 32);
            DeepTownhouses(d, 1, 0f, 40f, 22f, 36);
            K.PlaneTree(d.Props, d.Collisions, V(-5.6f, d.Ground(16f), 16f), .9f);
            K.PlaneTree(d.Props, d.Collisions, V(5.6f, d.Ground(31f), 31f), .9f);
            K.PropFromKit(d.Props, d.Collisions, "Bench", V(-6.9f, d.Ground(29f), 29f), 90f);
            MLights(d, 9f, 23f, 36f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CCove()
        {
            // The promenade between the railway and Claisebrook Cove: limestone wall and tapered bollards on the water
            // side, a timber jetty with moored cruisers, Trafalgar Bridge across the cove; across the tracks the cafe
            // townhouses with their boardwalk and umbrellas.
            var d = new CqChunkDraft("Claisebrook_Cove", ClaisebrookDrop);
            CPromenade(d, -1f, -1f, true);
            CoveSide(d, 0f, 40f, 41, true);
            CoveJetty(d, 9f);
            TrafalgarBridge(d, 30f, K.Edge + .1f, K.Edge + CoveWidth + .8f);
            E.Corridor(d, RailSide);
            Boardwalk(d, 0f, 40f, 44);
            DeepTownhouses(d, -1, 0f, 40f, 36f, 45);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft CCanal()
        {
            // The promenade crosses the canal that links the cove to the lake: a humped timber footbridge whose middle is
            // missing (SPLASH); the railway crosses the same canal on steel girders. After it: Victoria Gardens with the
            // red-and-white striped shelter on the right, the round lake across the tracks on the left.
            var d = new CqChunkDraft("Claisebrook_Canal", ClaisebrookDrop);
            float ramp = CanalRampStart, lip = ramp + CanalRise + CanalTop, far = lip + CanalGap;
            CPromenade(d, lip, far);
            ComicMesh m = d.Road;
            K.Water(m, -(K.Edge + 34f), K.Edge + 150f, lip, far, CSea, CoveWater, CoveLight, 51);
            CanalWall(d.Road, -K.Edge, K.Edge + .7f, lip, true);
            CanalWall(d.Road, -K.Edge, K.Edge + 60f, far, false);
            // the canal runs on under the railway to the lake: its end wall and the lawn beyond it
            d.Road.Surface = ComicSurface.Concrete;
            K.MinMaxBox(d.Road, V(-(K.Edge + 34f), CSea - 1f, lip), V(-(K.Edge + 34.4f), K.Kerb + .02f, far), Limestone);
            Ground(d, -1, lip, far, 34.4f, 60f, Lawn, ComicSurface.Foliage);
            E.Corridor(d, RailSide, 0f, 40f, new E.CorridorOptions { GroundGapFrom = lip, GroundGapTo = far });
            E.Girders(d, RailSide, lip, far);
            // before the gap: the cove on the right, the cafes across the tracks on the left
            CoveSide(d, 0f, lip, 52, false);
            Boardwalk(d, 0f, lip, 53);
            // the timber hump with rails and lamps
            var spec = new CqRampSpec { Name = "FootbridgeHump", X = 0f, Z = ramp, Width = 2f * K.Edge, Rise = CanalRise, Height = CanalHeight, Top = CanalTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            d.Gaps.Add(new CqGapSpec { Name = "CanalWater", Z = lip, Length = CanalGap, Width = 0f, Word = "SPLASH", Water = true });
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, DeckLight, Deck, false, CSea - .3f);
            for (int s = -1; s <= 1; s += 2)
            {
                deck.Surface = ComicSurface.PaintedMetal;
                deck.Beam(V(s * (K.Edge + .12f), 1.05f, ramp), V(s * (K.Edge + .12f), CanalHeight + 1.05f, ramp + CanalRise), .1f, White);
                deck.Beam(V(s * (K.Edge + .12f), CanalHeight + 1.05f, ramp + CanalRise), V(s * (K.Edge + .12f), CanalHeight + 1.05f, lip - .2f), .1f, White);
                for (float z = ramp; z <= lip; z += 1.5f)
                {
                    float h = CanalHeight * Mathf.Clamp01((z - ramp) / CanalRise);
                    deck.Box(V(s * (K.Edge + .12f), h + .52f, z), V(.08f, 1.05f, .08f), White);
                }
                deck.Surface = ComicSurface.Wood;
                for (float z = ramp + 3f; z <= lip; z += 3f) deck.Cylinder(V(s * (K.Edge - .3f), (CanalHeight * Mathf.Clamp01((z - ramp) / CanalRise) + CSea - .8f) * .5f, z), .18f, CanalHeight * Mathf.Clamp01((z - ramp) / CanalRise) - CSea + .8f, Deck, 8);
                var lamp = new ComicMesh();
                PathLight(lamp, Vector3.zero, 3.6f);
                deck.Add(lamp, V(s * (K.Edge + .4f), CanalHeight, lip - .6f), Quaternion.identity, Vector3.one);
            }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            d.KeepOut.Add(KeepOutBox(d, -1, ramp, far)); d.KeepOut.Add(KeepOutBox(d, 1, ramp, far));
            // after the gap, right: Victoria Gardens on its headland, the river beyond
            Ground(d, 1, far, 40f, 0f, 30f, Lawn, ComicSurface.Foliage);
            K.Water(m, K.Edge + 30f, K.Edge + 150f, far, 40f, CSea, River, RiverLight, 54);
            d.SlopedFrontage.Surface = ComicSurface.Concrete;
            K.SideBox(d.SlopedFrontage, 1, far, 40f, CSea - 1f, K.Kerb + .02f, 30f, 30.6f, Limestone);
            Patch(d, 1, far, 40f, 3.4f, 5.4f, K.Hex("C98F74"), K.Kerb + .04f, ComicSurface.Paving);
            VictoriaShelter(d.Landmark, V(K.Edge + 12f, d.Ground(31f), 31f));
            Gum(d.Props, V(K.Edge + 7f, d.Ground(25f), 25f), 10f, 7);
            Gum(d.Props, V(K.Edge + 21f, d.Ground(37f), 37f), 12f, 9);
            Bench(d.Props, V(K.Edge + 2.4f, d.Ground(35f), 35f), 90f);
            for (int i = 0; i < 4; i++) GrassTuft(d.Props, V(K.Edge + 1.3f + (i % 2) * .6f, d.Ground(26f), 25.5f + i * .6f), 1f, i + 3);
            SideWall(d, 1, far, 40f, 20f);
            // after the gap, left, across the tracks: the round lake with its limestone rim and a jet, paperbarks,
            // townhouses behind
            Ground(d, -1, far, 40f, BeyondRail, 60f, Lawn, ComicSurface.Foliage);
            RoundLake(d, -(K.Edge + 25.5f), 31.5f, 7.5f);
            Gum(d.Props, V(-(K.Edge + 17.4f), d.Ground(25f), 25f), 8.5f, 12, K.Hex("7F9A6A"), K.Hex("A3B88A"), K.Hex("E9E2D2"));
            Gum(d.Props, V(-(K.Edge + 17.8f), d.Ground(38.8f), 38.8f), 9.5f, 14, K.Hex("7F9A6A"), K.Hex("A3B88A"), K.Hex("E9E2D2"));
            DeepTownhouses(d, -1, far, 40f, 36f, 55);
            d.GiftSlots.Add(new Vector2(0f, 30f));
            d.Slot(-2.6f, 35f); d.Slot(2.6f, 35f);
            return d;
        }

        // ---------------------------------------------------------------- Claisebrook parts

        /// <summary>The water side of the promenade: limestone block wall with a coping, tapered bollards with inset lights,
        /// the cove, the far wall with townhouses and trees behind it.</summary>
        static void CoveSide(CqChunkDraft d, float z0, float z1, int seed, bool pine)
        {
            ComicMesh w = d.SlopedFrontage, m = d.Road;
            w.Surface = ComicSurface.Concrete;
            K.SideBox(w, 1, z0, z1, CSea - 1f, K.Kerb - .02f, 0f, .7f, Limestone);
            K.SideBox(w, 1, z0, z1, K.Kerb - .02f, K.Kerb + .1f, -.05f, .78f, LimestoneCap);
            w.Surface = ComicSurface.Plain;
            for (float z = z0 + .7f; z < z1 - .3f; z += 1.6f)
                K.Face(w, V(K.Edge + .705f, CSea, z), V(K.Edge + .705f, CSea, z + .05f), V(K.Edge + .705f, K.Kerb - .04f, z + .05f), V(K.Edge + .705f, K.Kerb - .04f, z), LimestoneDark, Vector3.right, false);
            for (float z = z0 + 2.5f; z < z1 - 1f; z += 5f) CoveBollard(w, V(K.Edge + .36f, K.Kerb + .1f, z));
            K.Water(m, K.Edge + .7f, K.Edge + CoveWidth, z0, z1, CSea, CoveWater, CoveLight, seed);
            // the far side: wall, promenade, townhouses (no outline back there)
            w.Surface = ComicSurface.Concrete;
            K.SideBox(w, 1, z0, z1, CSea - 1f, K.Kerb + .08f, CoveWidth, CoveWidth + .7f, Limestone);
            Ground(d, 1, z0, z1, CoveWidth + .7f, 110f, PromenadeFlag, ComicSurface.Paving);
            DeepTownhouses(d, 1, z0, z1, CoveWidth + 6f, seed + 1);
            var r = new System.Random(seed);
            for (float z = z0 + 4f; z < z1 - 2f; z += 11f) CheapGum(d.Deep, V(K.Edge + CoveWidth + F(r, 2.5f, 4.5f), d.Ground(z), z), F(r, 6f, 8f), (int)z, K.Hex("5F8A4A"));
            if (pine) CheapNorfolk(d.Deep, V(K.Edge + CoveWidth + 3.5f, d.Ground(34f), 35.5f), 17f);
            SideWall(d, 1, z0, z1, 20f);
        }

        static void CheapNorfolk(ComicMesh m, Vector3 p, float h)
        {
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, h * .5f, 0), .3f, h, K.Hex("6E5A47"), 6, .3f);
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                m.Cylinder(p + V(0, Mathf.Lerp(h * .3f, h * .92f, t), 0), Mathf.Lerp(h * .24f, h * .07f, t), h * .06f, i % 2 == 0 ? K.Hex("2C5A36") : K.Hex("3B6F42"), 6, .55f);
            }
        }

        /// <summary>A canal wall across the gap at z (its face looks over the water), limestone coping beside the path.</summary>
        static void CanalWall(ComicMesh m, float x0, float x1, float z, bool facingForward)
        {
            m.Surface = ComicSurface.Concrete;
            float t = facingForward ? -.4f : .4f;
            K.MinMaxBox(m, V(x0, CSea - 1f, z), V(x1, -.03f, z + t), Limestone);
            K.MinMaxBox(m, V(x0, -.03f, z), V(-K.Half, K.Kerb + .02f, z + t), LimestoneCap);
            K.MinMaxBox(m, V(K.Half, -.03f, z), V(x1, K.Kerb + .02f, z + t), LimestoneCap);
        }

        /// <summary>A tapered pale limestone bollard with an inset light band and a domed cap (the cove's own bollards).</summary>
        static void CoveBollard(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(p + V(0, .42f, 0), .22f, .84f, K.Hex("E9DDBC"), 8, .72f);
            m.Cylinder(p + V(0, .9f, 0), .19f, .12f, K.Hex("F3EBD2"), 8, .5f);
            m.Surface = ComicSurface.Glass;
            m.Cylinder(p + V(0, .68f, 0), .185f, .1f, K.R.cream, 8, .96f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .03f, 0), .26f, .06f, K.Hex("BFA978"), 8);
        }

        /// <summary>A timber jetty out from the cove wall at z with a T head, piles, and two moored cruisers and a dinghy.</summary>
        static void CoveJetty(CqChunkDraft d, float z)
        {
            ComicMesh m = d.SlopedFrontage;
            float x0 = K.Edge + .7f, x1 = K.Edge + 13f, y = K.Kerb - .55f;
            m.Surface = ComicSurface.Wood;
            m.Box(V((x0 + x1) * .5f, y, z), V(x1 - x0, .16f, 2f), DeckLight);
            m.Box(V(x1 - 1f, y, z + 4f), V(2f, .16f, 10f), DeckLight);
            for (float x = x0 + 1.5f; x < x1; x += 3.4f)
                foreach (float dz in new[] { -1f, 1f }) m.Cylinder(V(x, (y + CSea - 1f) * .5f + .4f, z + dz), .14f, y - CSea + 1.8f, Deck, 6);
            foreach (float dz in new[] { -.5f, 4f, 8.5f }) m.Cylinder(V(x1 - .1f, (y + CSea - 1f) * .5f + .5f, z + dz), .16f, y - CSea + 2f, Deck, 6);
            // steps down from the promenade
            m.Box(V(x0 + .5f, K.Kerb - .25f, z), V(1f, .5f, 1.8f), Limestone);
            Cruiser(m, V(x1 + 2.4f, CSea, z + 5f), 4f, Navy);
            Cruiser(m, V(x1 - 4.6f, CSea, z + 5.8f), 176f, K.Hex("C4623C"));
            Dinghy(m, V(x0 + 4f, CSea, z - 3.2f), 70f);
        }

        /// <summary>A small motor cruiser: white hull with a coloured boot stripe and a pointed bow, a cabin with dark
        /// windows, a raked windscreen, a rail. Bow at +Z for yaw 0.</summary>
        static void Cruiser(ComicMesh props, Vector3 p, float yaw, Color stripe)
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, .45f, -.6f), V(2.6f, 1f, 5.4f), White);
            // the bow: two faces meeting at the stem, a deck triangle
            Vector3 a = V(-1.3f, -.05f, 2.1f), b = V(1.3f, -.05f, 2.1f), c = V(0, .1f, 4.4f), a1 = V(-1.3f, .95f, 2.1f), b1 = V(1.3f, .95f, 2.1f), c1 = V(0, 1.05f, 4.6f);
            K.Face(m, a, c, c1, a1, White, V(-1, 0, .6f));
            K.Face(m, b, c, c1, b1, White, V(1, 0, .6f));
            K.TriFacing(m, a1, b1, c1, K.Hex("E9E4D6"), Vector3.up);
            m.Surface = ComicSurface.Plain;
            m.Box(V(0, .12f, -.6f), V(2.63f, .22f, 5.43f), stripe);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, 1.45f, -.9f), V(2.1f, 1f, 2.8f), White);
            m.Box(V(0, 2f, -.9f), V(2.3f, .1f, 3.1f), Mix(stripe, White, .2f));
            m.Surface = ComicSurface.Glass;
            m.Box(V(0, 1.5f, -.9f), V(2.14f, .5f, 2.3f), GlassDark);
            m.Box(V(0, 1.45f, .62f), V(1.9f, .7f, .08f), GlassDark, Quaternion.Euler(-22, 0, 0));
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -1.2f, 1.2f })
            {
                m.Box(V(x, 1.3f, 1.4f), V(.05f, .05f, 2f), K.Hex("C9CED3"));
                foreach (float z in new[] { .5f, 2.3f }) m.Box(V(x, 1.12f, z), V(.05f, .36f, .05f), K.Hex("C9CED3"));
            }
            m.Box(V(0, 1f, -3.4f), V(2.2f, .1f, .5f), Deck);
            props.Add(m, p, Quaternion.Euler(0, yaw, 0), Vector3.one);
        }

        static void Dinghy(ComicMesh props, Vector3 p, float yaw)
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.Wood;
            m.Box(V(0, .22f, 0), V(1.4f, .5f, 3f), K.Hex("E9E4D6"));
            m.Box(V(0, .4f, 0), V(1.1f, .2f, 2.6f), K.Hex("B88757"));
            m.Box(V(0, .5f, .3f), V(1.3f, .06f, .3f), Timber);
            m.Box(V(0, .22f, 1.7f), V(.9f, .5f, .5f), K.Hex("E9E4D6"), Quaternion.Euler(0, 45, 0));
            props.Add(m, p, Quaternion.Euler(0, yaw, 0), Vector3.one);
        }

        /// <summary>Trafalgar Bridge (1994): a slender cable-stayed footbridge across the cove at z: two towers, each a pair
        /// of dark cylindrical masts with cross-braces, white stays fanning to the timber deck, a white balustrade.</summary>
        static void TrafalgarBridge(CqChunkDraft d, float zc, float x0, float x1)
        {
            ComicMesh m = d.Frontage;
            float y0 = d.Y(zc) + K.Kerb + .05f, half = 1.5f, span = x1 - x0;
            Color mast = K.Hex("3A4148");
            float Deck01(float t) => y0 + 1.1f * Mathf.Sin(Mathf.PI * t);
            const int n = 10;
            for (int i = 0; i < n; i++)
            {
                float t0 = (float)i / n, t1 = (float)(i + 1) / n;
                Vector3 a = V(x0 + span * t0, Deck01(t0), zc), b = V(x0 + span * t1, Deck01(t1), zc);
                Quaternion q = Quaternion.LookRotation(b - a);
                m.Surface = ComicSurface.Wood;
                m.Box((a + b) * .5f - V(0, .12f, 0), V(half * 2f, .22f, (b - a).magnitude + .02f), DeckLight, q);
                m.Surface = ComicSurface.PaintedMetal;
                foreach (float dz in new[] { -half, half })
                {
                    m.Box((a + b) * .5f + V(0, 1.05f, dz), V(.07f, .07f, (b - a).magnitude + .02f), White, q);
                    m.Box((a + b) * .5f + V(0, .55f, dz), V(.04f, .04f, (b - a).magnitude + .02f), White, q);
                    m.Box(a + V(0, .52f, dz), V(.07f, 1.05f, .07f), White);
                }
            }
            foreach (float tt in new[] { .27f, .73f })
            {
                float xt = x0 + span * tt, yd = Deck01(tt), top = yd + 10.5f;
                foreach (float dz in new[] { -half - .45f, half + .45f })
                {
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Cylinder(V(xt, (top + y0 + CSea - 1f) * .5f, zc + dz), .26f, top - (y0 + CSea - 1f), mast, 8);
                    m.Cylinder(V(xt, top + .2f, zc + dz), .34f, .4f, mast, 8, .3f);
                    foreach (float off in new[] { -10f, -6.5f, -3.2f, 3.2f, 6.5f, 10f })
                    {
                        float xe = Mathf.Clamp(xt + off, x0 + .4f, x1 - .4f), te = (xe - x0) / span;
                        d.Deep.Surface = ComicSurface.PaintedMetal;
                        d.Deep.Beam(V(xt, top - .2f - Mathf.Abs(off) * .06f, zc + dz), V(xe, Deck01(te) + .1f, zc + Mathf.Sign(dz) * half), .09f, White);
                    }
                }
                m.Beam(V(xt, yd + 4.2f, zc - half - .45f), V(xt, yd + 8.2f, zc + half + .45f), .12f, mast);
                m.Beam(V(xt, yd + 8.2f, zc - half - .45f), V(xt, yd + 4.2f, zc + half + .45f), .12f, mast);
                m.Box(V(xt, yd + 9.2f, zc), V(.14f, .14f, half * 2f + .9f), mast);
                m.Box(V(xt, yd + 3.6f, zc), V(.14f, .14f, half * 2f + .9f), mast);
            }
        }

        /// <summary>Across the tracks from the promenade (it starts where the corridor ends): a timber boardwalk for
        /// alfresco tables under white umbrellas, black lamp posts, cafe townhouses behind.</summary>
        static void Boardwalk(CqChunkDraft d, float z0, float z1, int seed)
        {
            const float u0 = BeyondRail;
            Ground(d, -1, z0, z1, u0, 60f, PromenadeFlag, ComicSurface.Paving);
            ComicMesh m = d.Road;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                m.Surface = ComicSurface.Wood;
                K.Face(m, V(-(K.Edge + u0 + .3f), K.Kerb + .05f, z), V(-(K.Edge + u0 + .3f), K.Kerb + .05f, e), V(-(K.Edge + u0 + 5f), K.Kerb + .05f, e), V(-(K.Edge + u0 + 5f), K.Kerb + .05f, z), DeckLight, Vector3.up, false);
                m.Surface = ComicSurface.Plain;
                for (float u = u0 + 1.1f; u < u0 + 5f; u += .8f)
                    K.Face(m, V(-(K.Edge + u), K.Kerb + .06f, z), V(-(K.Edge + u), K.Kerb + .06f, e), V(-(K.Edge + u + .05f), K.Kerb + .06f, e), V(-(K.Edge + u + .05f), K.Kerb + .06f, z), Mix(Deck, Ink, .25f), Vector3.up, false);
            }
            if (z1 - z0 > 12f) TownhouseRow(d, -1, z0 + .5f, z1 - .5f, K.Edge + u0 + 5.6f, seed, true);
            Color[] canopies = { White, White, K.Hex("E9E4D6") };
            int k = 0;
            for (float z = z0 + 4f; z < z1 - 2f; z += 6.5f) K.CafeUmbrella(d.Props, null, V(-(K.Edge + u0 + 2.7f), d.Ground(z), z), canopies[k++ % canopies.Length]);
            for (float z = z0 + 7f; z < z1 - 1f; z += 13f) PathLight(d.Props, V(-(K.Edge + u0 + .7f), d.Ground(z), z), 4f);
        }

        /// <summary>The round lake at the end of the canal: water, a limestone rim, a white jet in the middle.</summary>
        static void RoundLake(CqChunkDraft d, float xc, float zc, float r)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(V(xc, K.Kerb - .25f, zc), r + .5f, .6f, Limestone, 12);
            m.Surface = ComicSurface.Plain;
            m.Cylinder(V(xc, K.Kerb + .02f, zc), r, .1f, CoveWater, 12);
            m.Cylinder(V(xc, K.Kerb + .08f, zc), r * .45f, .02f, CoveLight, 10);
            m.Surface = ComicSurface.Glass;
            m.Cylinder(V(xc, K.Kerb + 1.3f, zc), .14f, 2.5f, White, 6, .3f);
            m.Cylinder(V(xc, K.Kerb + .3f, zc), .5f, .4f, K.Hex("DCEFF2"), 8, .4f);
        }

        /// <summary>The Victoria Gardens shelter: a red-and-white striped wavy roof on white steel arches, a bench under it.</summary>
        static void VictoriaShelter(ComicMesh m, Vector3 p)
        {
            const float len = 13f, w = 2.6f;
            const int n = 13;
            float Wave(float z) => 3.5f + .55f * Mathf.Sin(z * 1.45f);
            for (int i = 0; i < n; i++)
            {
                float za = -len * .5f + len * i / n, zb = -len * .5f + len * (i + 1) / n;
                Color c = i % 2 == 0 ? K.Hex("D9432F") : White;
                Vector3 a0 = p + V(-w, Wave(za), za), a1 = p + V(w, Wave(za), za), b0 = p + V(-w, Wave(zb), zb), b1 = p + V(w, Wave(zb), zb), t = V(0, .12f, 0);
                m.Surface = ComicSurface.Fabric;
                K.Face(m, a0 + t, a1 + t, b1 + t, b0 + t, c, Vector3.up);
                K.Face(m, a0, a1, b1, b0, Mix(c, Ink, .25f), Vector3.down);
                K.Face(m, a0, b0, b0 + t, a0 + t, c, Vector3.left);
                K.Face(m, a1, b1, b1 + t, a1 + t, c, Vector3.right);
            }
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = -len * .5f + 1.1f; z < len * .5f; z += 3.6f)
            {
                float y = Wave(z);
                foreach (float x in new[] { -w + .3f, w - .3f })
                {
                    m.Beam(p + V(x * 1.25f, 0, z), p + V(x, y - 1f, z), .12f, White);
                    m.Beam(p + V(x, y - 1f, z), p + V(x * .5f, y - .05f, z), .1f, White);
                    m.Cylinder(p + V(x * 1.25f, .06f, z), .22f, .12f, Ink, 6);
                }
                m.Box(p + V(0, y - .06f, z), V(w * 1.1f, .1f, .1f), White);
            }
            Bench(m, p + V(0, 0, -2f), 90f);
            Bench(m, p + V(0, 0, 2.5f), -90f);
        }

        /// <summary>The white enclosed footbridge over the station: a lift tower on platform 1, the glazed bridge across the
        /// tracks on white columns, the long enclosed ramp coming down along the platform.</summary>
        static void Footbridge(CqChunkDraft d, float z)
        {
            ComicMesh m = d.SlopedFrontage;
            Color white = K.Hex("F2F2EE"), roof = K.Hex("DADDDD");
            float y = 7f, xa = LX(1.2f), xb = LX(FarPlatform + 2f);
            // the bridge
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V((xa + xb) * .5f, y + .5f, z + 1.4f), V(xa - xb, 1f, 2.8f), white);
            m.Box(V((xa + xb) * .5f, y + 2.75f, z + 1.4f), V(xa - xb, .5f, 2.8f), white);
            m.Box(V((xa + xb) * .5f, y + 3.1f, z + 1.4f), V(xa - xb + .4f, .14f, 3.3f), roof);
            m.Box(V((xa + xb) * .5f, y - .06f, z + 1.4f), V(xa - xb, .12f, 2.6f), SteelDark);
            m.Surface = ComicSurface.Glass;
            m.Box(V((xa + xb) * .5f, y + 1.75f, z + 1.4f), V(xa - xb - .2f, 1.5f, 2.6f), GlassDark);
            m.Surface = ComicSurface.PaintedMetal;
            for (float x = xb + 1.2f; x < xa; x += 2.4f)
                K.Face(m, V(x - .05f, y + 1f, z - .012f), V(x + .05f, y + 1f, z - .012f), V(x + .05f, y + 2.5f, z - .012f), V(x - .05f, y + 2.5f, z - .012f), white, Vector3.back, false);
            foreach (float u in new[] { 8.25f }) { m.Surface = ComicSurface.PaintedMetal; m.Cylinder(V(LX(u), y * .5f, z + 1.4f), .3f, y, white, 8); }
            // the lift towers (platform 1 and the island)
            foreach (float u in new[] { 2.7f, FarPlatform })
            {
                float xt = LX(u);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(xt, (y + 4.2f + PlatformTop) * .5f, z - 1.4f), V(2.6f, y + 4.2f - PlatformTop, 2.8f), white);
                m.Box(V(xt, y + 4.3f, z - 1.4f), V(3f, .2f, 3.2f), roof);
                m.Surface = ComicSurface.Glass;
                m.Box(V(xt, (y + PlatformTop) * .5f + 1.6f, z - 1.4f), V(1.5f, y - PlatformTop + 1.5f, 2.86f), GlassDark);
                K.Face(m, V(xt + 1.31f, PlatformTop + .1f, z - 2.2f), V(xt + 1.31f, PlatformTop + .1f, z - .6f), V(xt + 1.31f, PlatformTop + 2.3f, z - .6f), V(xt + 1.31f, PlatformTop + 2.3f, z - 2.2f), K.Hex("9AA2AA"), Vector3.right);
            }
            // the enclosed ramp down to platform 1 (three flights would be too busy: one long straight run)
            Vector3 top = V(LX(2.7f), y + 1.55f, z + 2.8f), foot = V(LX(2.7f), PlatformTop + 1.55f, z + 24f);
            Quaternion q = Quaternion.LookRotation(foot - top);
            float run = (foot - top).magnitude;
            m.Surface = ComicSurface.PaintedMetal;
            m.Box((top + foot) * .5f + q * V(0, -1.05f, 0), V(2.4f, 1f, run), white, q);
            m.Box((top + foot) * .5f + q * V(0, 1.2f, 0), V(2.4f, .45f, run), white, q);
            m.Box((top + foot) * .5f + q * V(0, 1.5f, 0), V(2.8f, .12f, run + .4f), roof, q);
            m.Surface = ComicSurface.Glass;
            m.Box((top + foot) * .5f + q * V(0, .2f, 0), V(2.3f, 1.5f, run - .3f), GlassDark, q);
            m.Surface = ComicSurface.PaintedMetal;
            for (float t = .22f; t < .9f; t += .26f)
            {
                Vector3 c = Vector3.Lerp(top, foot, t);
                m.Cylinder(V(c.x, (c.y - 1.5f + PlatformTop) * .5f, c.z), .2f, c.y - 1.5f - PlatformTop, white, 8);
            }
        }

        /// <summary>The depot's long shed: white walls, a dark brown low-pitch roof, a blue fascia, open doors on the end
        /// towards the rider with a train nose in each.</summary>
        static void DepotShed(CqChunkDraft d, float u0, float u1, float z0, float z1)
        {
            ComicMesh m = d.SlopedFrontage;
            float xa = LX(u0), xb = LX(u1), xc = (xa + xb) * .5f, w = xa - xb, h = 8f, len = z1 - z0, zc = (z0 + z1) * .5f;
            m.Surface = ComicSurface.Metal;
            m.Box(V(xc, h * .5f + .1f, zc), V(w, h, len), K.Hex("ECEBE4"));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(xc, h + .1f, zc), V(w + .3f, .5f, len + .3f), K.Hex("2F6FB0"));
            // roof: two slopes and brown gable ends
            Color roof = K.Hex("5E4638");
            Vector3 e0 = V(xa + .3f, h + .35f, z0 - .2f), e1 = V(xb - .3f, h + .35f, z0 - .2f), r0 = V(xc, h + 2.3f, z0 - .2f), L = V(0, 0, len + .4f);
            m.Surface = ComicSurface.Metal;
            K.Face(m, e0, r0, r0 + L, e0 + L, roof, V(1, 1, 0));
            K.Face(m, e1, r0, r0 + L, e1 + L, Mix(roof, Ink, .15f), V(-1, 1, 0));
            K.TriFacing(m, e0, e1, r0, K.Hex("ECEBE4"), Vector3.back);
            K.TriFacing(m, e0 + L, e1 + L, r0 + L, K.Hex("ECEBE4"), Vector3.forward);
            for (float z = z0 + 3f; z < z1; z += 4.5f) m.Box(V(xa + .02f, h * .55f, z), V(.06f, h * .9f, .12f), K.Hex("C9CED3"));
            // the doors on the end towards the rider: dark openings, a nose in each
            int doors = Mathf.Max(2, Mathf.RoundToInt(w / 5.5f));
            for (int i = 0; i < doors; i++)
            {
                float x = xb + w * (i + .5f) / doors;
                m.Surface = ComicSurface.Plain;
                K.Face(m, V(x - 2f, .2f, z0 - .012f), V(x + 2f, .2f, z0 - .012f), V(x + 2f, 5.8f, z0 - .012f), V(x - 2f, 5.8f, z0 - .012f), K.Hex("14181D"), Vector3.back);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(x, 6f, z0 - .05f), V(4.3f, .3f, .1f), K.Hex("C9CED3"));
                if (i == 0)       // the far road: a set looking out; the other two roads have sets going in
                {
                    var nose = new ComicMesh();
                    E.TrainLook k = i % 4 == 0 ? BSeries : ASeries;
                    nose.Surface = ComicSurface.Metal;
                    nose.Box(V(0, 2.65f, 1.6f), V(k.Width, 2.8f, 3f), k.Body);
                    E.Nose(nose, 0f, .1f, -1f, k);
                    m.Add(nose, V(x, 0f, z0 - .2f), Quaternion.identity, Vector3.one);
                }
            }
            d.Walls.Add(new Bounds(new Vector3(xc, d.Y(zc) + 4f, zc), new Vector3(w, 10f, len)));
        }

        /// <summary>A row of East Perth townhouses (and, with cafes, ground-floor shopfronts under awnings): two and three
        /// storeys of coloured render, arched doors and top windows, black metal balconies, tiled gables, parapets and
        /// curved corrugated roofs by turns.</summary>
        static void TownhouseRow(CqChunkDraft d, int s, float z0, float z1, float line, int seed, bool cafes)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / 6.4f));
            float w = (z1 - z0) / n;
            for (int i = 0; i < n; i++)
            {
                int k = seed * 3 + i * 5;
                var l = new CqLot(s, z0 + i * w, z0 + (i + 1) * w, line + ((k / 3) % 2) * .6f, 11f, d.Base(z0 + (i + .5f) * w));
                Townhouse(d, l, 2 + (k % 3 == 0 ? 0 : 1), RenderColours[Mathf.Abs(k) % RenderColours.Length], (k / 2) % 3, cafes, k);
            }
        }

        static void Townhouse(CqChunkDraft d, CqLot l, int floors, Color wall, int roofStyle, bool cafe, int seed)
        {
            ComicMesh f = d.Frontage;
            float W = l.W, D = l.Depth, gh = 3.4f, fh = 3.1f, h = gh + (floors - 1) * fh;
            Color trim = Mix(wall, White, .55f), door = DoorColours[Mathf.Abs(seed) % DoorColours.Length], roof = seed % 2 == 0 ? Terracotta : Slate;
            Plinth(f, l); d.Wall(l, h + 3f);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0f, W, 0f, h, 0f, -D, wall);
            K.LBox(f, l, 0f, .28f, 0f, h + .5f, .14f, -D, trim);
            K.LBox(f, l, W - .28f, W, 0f, h + .5f, .14f, -D, trim);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, .28f, W - .28f, gh - .12f, gh + .06f, .1f, 0f, trim);
            if (cafe)
            {
                Shopfront(f, l, .5f, W - .5f, .05f, 2.85f, .02f, true);
                Awning(f, l, .35f, W - .35f, 2.75f, 1.7f, SignColours[Mathf.Abs(seed) % SignColours.Length], seed % 3 == 0 ? (Color?)null : White);
            }
            else
            {
                Arch(f, l, W * .27f, 1.15f, 0f, 2.1f, 0f, door, trim);
                Win(f, l, W * .68f, Mathf.Min(1.9f, W * .3f), .9f, 2.6f, 0f, trim, 2);
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, W * .27f - .9f, W * .27f + .9f, 0f, .16f, .7f, 0f, trim);
            }
            // first floor: french doors onto a black metal balcony
            float y = gh;
            Win(f, l, W * .5f, Mathf.Min(2.2f, W * .36f), y + .12f, y + 2.45f, 0f, trim, 2, false, true, true);
            Balcony(f, l, W * .5f - Mathf.Min(1.9f, W * .34f), W * .5f + Mathf.Min(1.9f, W * .34f), y + .08f, 0f, .95f, Mix(wall, Ink, .25f), FenceBlack, false, .95f);
            if (floors >= 3)
            {
                y = gh + fh;
                if (seed % 2 == 0) Arch(f, l, W * .5f, 1.4f, y + .8f, y + 1.9f, 0f, WindowGlass, trim, true);
                else { Win(f, l, W * .32f, 1f, y + .8f, y + 2.4f, 0f, trim, 2); Win(f, l, W * .68f, 1f, y + .8f, y + 2.4f, 0f, trim, 2); }
            }
            if (roofStyle == 0)
            {
                // a tiled gable towards the street with a rendered face and a round vent
                f.Surface = ComicSurface.Brick;
                float rise = W * .34f;
                K.LGable(f, l, -.1f, W + .1f, h, h + rise, .32f, -D, roof);
                f.Surface = ComicSurface.Plaster;
                K.TriFacing(f, l.P(.45f, h + .06f, .34f), l.P(W - .45f, h + .06f, .34f), l.P(W * .5f, h + rise - .5f, .34f), wall, new Vector3(-l.S, 0, 0));
                f.Surface = ComicSurface.PaintedMetal;
                f.Cylinder(l.P(W * .5f, h + rise * .38f, .36f), .32f, .08f, Ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            }
            else if (roofStyle == 1)
            {
                f.Surface = ComicSurface.Plaster;
                K.LBox(f, l, .28f, W - .28f, h, h + .75f, .06f, -.35f, wall);
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, .2f, W - .2f, h + .75f, h + .9f, .14f, -.42f, trim);
                f.Surface = ComicSurface.Brick;
                HipRoof(f, l, .3f, W - .3f, h + .3f, h + 2.5f, -.4f, -D, roof);
            }
            else
            {
                // a curved corrugated roof
                f.Surface = ComicSurface.Metal;
                const int facets = 5;
                for (int i = 0; i < facets; i++)
                {
                    float t0 = (float)i / facets, t1 = (float)(i + 1) / facets;
                    float a0 = Mathf.Lerp(-.1f, W + .1f, t0), a1 = Mathf.Lerp(-.1f, W + .1f, t1), u0 = h + .2f + 1.5f * Mathf.Sin(Mathf.PI * t0), u1 = h + .2f + 1.5f * Mathf.Sin(Mathf.PI * t1);
                    K.Face(f, l.P(a0, u0, .5f), l.P(a1, u1, .5f), l.P(a1, u1, -D), l.P(a0, u0, -D), i % 2 == 0 ? K.Hex("B9C0C6") : K.Hex("A9B0B6"), Vector3.up);
                    K.Face(f, l.P(a0, h, .1f), l.P(a1, h, .1f), l.P(a1, u1, .1f), l.P(a0, u0, .1f), wall, new Vector3(-l.S, 0, 0), false);
                }
                f.Surface = ComicSurface.PaintedMetal;
                K.LBox(f, l, -.1f, W + .1f, h, h + .14f, .5f, .1f, Ink);
            }
            EndWin(f, l, 0f, -3.5f, -4.8f, gh + .8f, gh + 2.4f);
        }

        /// <summary>The waterside apartment block: a darker podium with framed shopfronts under a street awning, the floors
        /// set back above with alternating glass-balcony bays and window bays, a cornice, a curved corrugated roof on the
        /// lift core, rooftop plant.</summary>
        static void CoveApartments(CqChunkDraft d, CqLot l, int floors, Color wall, Color accent)
        {
            ComicMesh f = d.Frontage;
            float W = l.W, D = l.Depth, gh = 4.2f, fh = 3.1f, set = .9f, y0 = gh + .3f, top = y0 + fh * (floors - 1);
            Color podium = Mix(wall, Ink, .22f), pilaster = Mix(wall, White, .4f);
            Plinth(f, l); d.Wall(l, 16f);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0f, W, 0f, gh, -.3f, -D, podium);
            int n = Mathf.Max(2, Mathf.RoundToInt(W / 4.4f));
            float bw = W / n;
            for (int i = 0; i < n; i++)
            {
                f.Surface = ComicSurface.Plaster;
                K.LBox(f, l, i * bw, i * bw + .4f, 0f, gh, .05f, -.3f, podium);
                Shopfront(f, l, i * bw + .5f, (i + 1) * bw - .1f, .05f, 3.2f, -.28f, i % 2 == 1);
            }
            K.LBox(f, l, W - .4f, W, 0f, gh, .05f, -.3f, podium);
            FlatAwning(f, l, -.1f, W + .1f, 3.3f, 2.4f, accent, Mix(accent, White, .65f));
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.15f, W + .15f, gh, y0, .35f, -D, TrimCream);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, .3f, W - .3f, y0, top, -set, -D + .3f, wall);
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Clamp(i * bw, .55f, W - .55f);
                K.LBox(f, l, a - .25f, a + .25f, y0, top + .3f, -set + .22f, -set, pilaster);
            }
            for (int k = 0; k < floors - 1; k++)
            {
                float y = y0 + fh * k;
                for (int i = 0; i < n; i++)
                {
                    float ac = (i + .5f) * bw;
                    if ((i + k) % 2 == 0)
                    {
                        Win(f, l, ac, bw * .5f, y + .15f, y + 2.5f, -set, TrimCream, 2, false);
                        Balcony(f, l, ac - bw * .4f, ac + bw * .4f, y + .1f, -set, 1.5f, TrimCream, White, true);
                    }
                    else
                    {
                        f.Surface = ComicSurface.Plaster;
                        K.LQuad(f, l, ac - bw * .32f, ac + bw * .32f, y + .2f, y + .8f, -set + .02f, accent);
                        Win(f, l, ac, bw * .55f, y + .95f, y + 2.5f, -set, TrimCream, 2);
                    }
                }
                EndWin(f, l, .3f, -set - 2.5f, -set - 4.1f, y + .8f, y + 2.4f);
                EndWin(f, l, .3f, -set - 7f, -set - 8.6f, y + .8f, y + 2.4f);
            }
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, .1f, W - .1f, top, top + .35f, -set + .45f, -D + .1f, TrimCream);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, .3f, W - .3f, top + .35f, top + 1f, -set + .05f, -D + .3f, wall);
            // the lift core with a curved corrugated roof, and plant
            K.LBox(f, l, W * .5f, W * .82f, top + 1f, top + 3.2f, -set - 2.5f, -set - 7f, wall);
            f.Surface = ComicSurface.Metal;
            const int facets = 5;
            for (int i = 0; i < facets; i++)
            {
                float t0 = (float)i / facets, t1 = (float)(i + 1) / facets;
                float a0 = Mathf.Lerp(W * .46f, W * .86f, t0), a1 = Mathf.Lerp(W * .46f, W * .86f, t1), u0 = top + 3.2f + 1.1f * Mathf.Sin(Mathf.PI * t0), u1 = top + 3.2f + 1.1f * Mathf.Sin(Mathf.PI * t1);
                K.Face(f, l.P(a0, u0, -set - 2f), l.P(a1, u1, -set - 2f), l.P(a1, u1, -set - 7.5f), l.P(a0, u0, -set - 7.5f), K.Hex("B9C0C6"), Vector3.up);
                K.Face(f, l.P(a0, top + 3.2f, -set - 2.5f), l.P(a1, top + 3.2f, -set - 2.5f), l.P(a1, u1, -set - 2.5f), l.P(a0, u0, -set - 2.5f), wall, new Vector3(-l.S, 0, 0), false);
            }
            RoofUnit(f, l, W * .22f, -set - 3f, top + 1f, 1.8f, 1.1f);
            RoofUnit(f, l, W * .34f, -set - 6f, top + 1f, 1.4f, .9f);
        }

        /// <summary>The old brick warehouse at Saunders and Glyde St (1910-20, now apartments): three storeys of red brick
        /// between pilasters, windows with pale sills and heads, a timber loading door with a hoist beam above, steel
        /// balconies added to two bays, a cream cornice, a saw-tooth corrugated roof.</summary>
        static void Warehouse(CqChunkDraft d, CqLot l)
        {
            ComicMesh f = d.Frontage;
            float W = l.W, D = l.Depth, fh = 3.8f; int floors = 3; float h = floors * fh;
            Color sill = K.Hex("E6DCC6");
            Plinth(f, l); d.Wall(l, h + 3f);
            f.Surface = ComicSurface.Brick;
            K.LBox(f, l, 0f, W, 0f, h, 0f, -D, BrickRed);
            int n = Mathf.Max(3, Mathf.RoundToInt(W / 3.7f));
            float bay = W / n;
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Clamp(i * bay, .3f, W - .3f);
                f.Surface = ComicSurface.Brick;
                K.LBox(f, l, a - .3f, a + .3f, 0f, h + .5f, .2f, 0f, BrickDark);
            }
            for (int k = 0; k < floors; k++)
                for (int i = 0; i < n; i++)
                {
                    float ac = (i + .5f) * bay, y = k * fh;
                    if (k == 0 && i == n / 2)
                    {
                        f.Surface = ComicSurface.Wood;
                        K.LQuad(f, l, ac - bay * .36f, ac + bay * .36f, .05f, 3.1f, .02f, K.Hex("2F5A3A"));
                        f.Surface = ComicSurface.PaintedMetal;
                        K.LQuad(f, l, ac - .04f, ac + .04f, .05f, 3.1f, .035f, Ink, false);
                        f.Surface = ComicSurface.Concrete;
                        K.LBox(f, l, ac - bay * .4f, ac + bay * .4f, 3.1f, 3.4f, .14f, 0f, sill);
                        continue;
                    }
                    Win(f, l, ac, bay - 1.7f, y + 1f, y + 3f, 0f, sill, 2, true, true, true);
                    if (k > 0 && (i == 1 || i == n - 2)) Balcony(f, l, ac - bay * .42f, ac + bay * .42f, y + .85f, 0f, 1.1f, K.Hex("5A6068"), FenceBlack, false, 1f);
                }
            f.Surface = ComicSurface.Plaster;
            K.LQuad(f, l, bay * .6f, W - bay * .6f, h - .55f, h - .05f, .012f, K.Hex("E6DCC6"), false);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.12f, W + .12f, h + .5f, h + .85f, .35f, -D - .1f, sill);
            // the hoist beam over the loading door
            f.Surface = ComicSurface.PaintedMetal;
            float hc = (n / 2 + .5f) * bay;
            K.LBox(f, l, hc - .1f, hc + .1f, h - .2f, h + .05f, 1.6f, 0f, SteelDark);
            K.LBox(f, l, hc - .04f, hc + .04f, h - 1.3f, h - .2f, 1.4f, 1.32f, Ink);
            // the saw-tooth roof: glazed faces towards the rider
            int teeth = Mathf.Max(3, Mathf.RoundToInt(W / 5.5f));
            for (int i = 0; i < teeth; i++)
            {
                float a0 = W * i / teeth, a1 = W * (i + 1) / teeth;
                f.Surface = ComicSurface.Metal;
                K.Face(f, l.P(a0, h + 3f, -.4f), l.P(a1, h + .85f, -.4f), l.P(a1, h + .85f, -D + .2f), l.P(a0, h + 3f, -D + .2f), RoofGrey, V(0, 1, .5f));
                f.Surface = ComicSurface.Glass;
                K.Face(f, l.P(a0, h + .85f, -.4f), l.P(a0, h + 3f, -.4f), l.P(a0, h + 3f, -D + .2f), l.P(a0, h + .85f, -D + .2f), GlassDark, Vector3.back);
                f.Surface = ComicSurface.Brick;
                K.TriFacing(f, l.P(a0, h + .85f, -.4f), l.P(a1, h + .85f, -.4f), l.P(a0, h + 3f, -.4f), BrickRed, new Vector3(-l.S, 0, 0));
            }
            EndWin(f, l, 0f, -3f, -4.6f, fh + 1f, fh + 3f);
            EndWin(f, l, 0f, -8f, -9.6f, fh + 1f, fh + 3f);
            EndWin(f, l, 0f, -3f, -4.6f, 2f * fh + 1f, 2f * fh + 3f);
            EndWin(f, l, 0f, -8f, -9.6f, 2f * fh + 1f, 2f * fh + 3f);
        }

        /// <summary>Background townhouses (no outline): rendered boxes with tiled gables, a door and two windows each.</summary>
        static void DeepTownhouses(CqChunkDraft d, int s, float z0, float z1, float uNear, int seed)
        {
            ComicMesh m = d.Deep;
            var r = new System.Random(seed * 23 + 5);
            Vector3 toRoad = new Vector3(-s, 0, 0);
            for (float z = z0; z < z1 - 3f; )
            {
                float w = Mathf.Min(F(r, 6.5f, 9f), z1 - z), zc = z + w * .5f, h = F(r, 7f, 12.5f), dep = 12f, x = s * (K.Edge + uNear + dep * .5f + F(r, 0f, 2.5f)), y = d.Ground(zc) - 2f;
                Color c = RenderColours[r.Next(RenderColours.Length)], roof = r.NextDouble() < .6 ? Terracotta : Slate;
                m.Surface = ComicSurface.Plaster;
                m.Box(V(x, y + h * .5f, zc), V(dep, h, w - .15f), c);
                float fx = x - s * dep * .5f, rise = w * .32f;
                m.Surface = ComicSurface.Brick;
                Vector3 g0 = V(fx - s * .3f, y + h, zc - w * .5f), g1 = V(fx - s * .3f, y + h, zc + w * .5f), ga = V(fx - s * .3f, y + h + rise, zc), back = V(s * dep, 0, 0);
                K.TriFacing(m, g0, g1, ga, c, toRoad);
                K.Face(m, g0, ga, ga + back, g0 + back, roof, V(0, 1, -1));
                K.Face(m, g1, ga, ga + back, g1 + back, Mix(roof, Ink, .12f), V(0, 1, 1));
                m.Surface = ComicSurface.Plain;
                for (float by = 4.6f; by < h - 1f; by += 3.1f)
                    K.Face(m, V(fx - s * .03f, y + by, zc - w * .28f), V(fx - s * .03f, y + by, zc + w * .28f), V(fx - s * .03f, y + by + 1.6f, zc + w * .28f), V(fx - s * .03f, y + by + 1.6f, zc - w * .28f), Mix(WindowGlass, c, .15f), toRoad, false);
                K.Face(m, V(fx - s * .03f, y + 2f, zc - w * .3f), V(fx - s * .03f, y + 2f, zc - w * .3f + 1.1f), V(fx - s * .03f, y + 4.1f, zc - w * .3f + 1.1f), V(fx - s * .03f, y + 4.1f, zc - w * .3f), DoorColours[r.Next(DoorColours.Length)], toRoad, false);
                z += w;
            }
        }

        // ---------------------------------------------------------------- Claisebrook obstacles and animals

        // A mooring crate with a coil of rope on top (can be jumped).
        static ComicMesh RopeCrateMesh()
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.Wood;
            m.Box(V(0, .28f, 0), V(1f, .56f, .9f), Timber);
            foreach (float z in new[] { -.3f, 0f, .3f }) m.Box(V(0, .28f, z), V(1.02f, .5f, .04f), Deck);
            m.Box(V(0, .58f, 0), V(1.04f, .05f, .94f), Deck);
            m.Surface = ComicSurface.Fabric;
            for (int i = 0; i < 3; i++) m.Cylinder(V(.05f, .65f + i * .07f, 0), .34f - i * .04f, .07f, i % 2 == 0 ? K.Hex("E4D6B4") : K.Hex("CDBB98"), 8);
            return m;
        }

        // A cafe menu board: a dark A-frame with a cream panel and chalk lines.
        static ComicMesh MenuBoardMesh()
        {
            var m = new ComicMesh();
            foreach (int s in new[] { -1, 1 })
            {
                Quaternion q = Quaternion.Euler(s * 14f, 0, 0);
                m.Surface = ComicSurface.Wood;
                m.Box(V(0, .52f, s * -.17f), V(.72f, 1.05f, .05f), K.Hex("2A2F36"), q);
                m.Surface = ComicSurface.Plain;
                Vector3 c = V(0, .56f, s * -.2f);
                m.Box(c, V(.56f, .7f, .02f), K.Hex("F3E4C2"), q);
                for (int i = 0; i < 4; i++) m.Box(c + q * V(0, .22f - i * .13f, s * -.012f), V(.4f - (i % 2) * .1f, .035f, .01f), i == 0 ? K.Hex("E2463A") : K.Hex("2A2F36"), q);
            }
            return m;
        }

        // Pacific black duck: brown body, darker wings, a pale face, a grey-green bill, short orange legs.
        static ComicMesh DuckMesh() =>
            Bird(.95f, K.Hex("7A6248"), K.Hex("5A4634"), K.Hex("B89A72"), K.Hex("D9C29A"), K.Hex("6E7A5A"), .14f, .1f, K.Hex("E08A3A"), .1f);

        // A pied cormorant drying off: black back and wings, white front, a long neck and a hooked grey bill.
        static ComicMesh CormorantMesh() =>
            Bird(1.25f, K.Hex("F0EEE6"), K.Hex("20262C"), K.Hex("E4E2D8"), K.Hex("20262C"), K.Hex("8A9198"), .2f, .28f, K.Hex("20262C"), .14f, true);
    }
}
