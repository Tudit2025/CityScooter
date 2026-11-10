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
    // BASSENDEAN (order 290): the station and the rail museum beside the line, then Old Perth Road: parapet shopfronts,
    // the corner hotel, the war memorial in BIC Reserve and Bassendean Oval. Red-brown asphalt, red brick footpaths.
    public static partial class East3Builder
    {
        /// <summary>The sleeper loading ramp (Ramp) in the Museum chunk: numbers for gameplay.</summary>
        public const float SleeperRampZ = 9f, SleeperRampRise = 5f, SleeperRampHeight = 1.25f, SleeperRampTop = 2.5f, SleeperRampWidth = 3.2f;

        static readonly Color LocoBlack = K.Hex("26292E"), LocoGrey = K.Hex("3D4249"), LocoRed = K.Hex("C4352C"), LocoYellow = K.Hex("F2C230"), Brass = K.Hex("C9A24A");
        static readonly Color MuseumRed = K.Hex("B43A32"), Weatherboard = K.Hex("E6D7A0"), WeatherboardShade = K.Hex("C9B97F"), StandCream = K.Hex("E6D487");
        static readonly Color Granite = K.Hex("B9BCC0"), GraniteDark = K.Hex("8F949A"), GraniteLight = K.Hex("D6D8DA"), StoneWhite = K.Hex("F1EFE8");
        static readonly Color OvalGreen = K.Hex("5FA044"), OvalStripe = K.Hex("74B254"), GateCream = K.Hex("EADBB0"), NavyFrame = K.Hex("2B3550"), BlockGrey = K.Hex("A9ACAE"), TealGlass = K.Hex("2F8F8A");
        static string BasPf(string name) => BasPrefabs + "/" + name + ".prefab";

        static void BuildBassendean(TrackTheme theme, bool atlas)
        {
            K.Begin(BasMeshes, BasPrefabs, CbdBuilder.DataFolder);
            Obstacle trolley = K.SaveObstacle(BasPf("OBS_Bassendean_LuggageTrolley"), "OBS_Bassendean_LuggageTrolley", LuggageTrolleyMesh(), new Vector3(1.1f, 1.05f, 1.7f), ObstacleKind.Thing, true, false, .8f);
            Obstacle aFrame = K.SaveObstacle(BasPf("OBS_Bassendean_AFrame"), "OBS_Bassendean_AFrame", AFrameMesh(), new Vector3(.9f, 1.05f, .7f), ObstacleKind.Thing, true, true, .9f);
            Obstacle barrel = K.SaveObstacle(BasPf("OBS_Bassendean_BarrelPlanter"), "OBS_Bassendean_BarrelPlanter", BarrelPlanterMesh(), new Vector3(.95f, 1.05f, .95f), ObstacleKind.Thing, true, true, 1f);
            Obstacle bag = K.SaveObstacle(BasPf("OBS_Bassendean_TackleBag"), "OBS_Bassendean_TackleBag", TackleBagMesh(), new Vector3(.6f, 1.6f, .6f), ObstacleKind.Thing, false, true, .8f);
            GameObject galah = SaveAnimal(BasPf("ANM_Bassendean_Galah"), "ANM_Bassendean_Galah", GalahMesh(), new Vector3(0, .22f, 0), new Vector3(.36f, .45f, .5f), .3f, true);
            GameObject magpie = SaveAnimal(BasPf("ANM_Bassendean_Magpie"), "ANM_Bassendean_Magpie", MagpieMesh(), new Vector3(0, .28f, 0), new Vector3(.4f, .56f, .7f), .5f, true);
            Ramp sleepers = K.SaveRamp(BasPf("RMP_Bassendean_SleeperRamp"), "RMP_Bassendean_SleeperRamp", 3f, 5f, 1.25f, 1.5f, 1f, .5f, Timber, TimberDark, false);

            Write(BasEntry(), BasChunkPath("Entry"));
            Write(BasStation(), BasChunkPath("Station"));
            Write(BasMuseum(), BasChunkPath("Museum"));
            Write(BasOldPerthRd(), BasChunkPath("OldPerthRd"));
            Write(BasMemorial(), BasChunkPath("Memorial"));
            Write(BasOval(), BasChunkPath("Oval"));
            Write(BasExit(), BasChunkPath("Exit"));
            GameObject far = K.SaveFarScenery(BasPf("FAR_Bassendean_SwanAndScarp"), "FAR_Bassendean_SwanAndScarp", BasFar(-BasDrop / 40f));

            District d = OpenDistrict(BasDistrictPath, BasId, "BASSENDEAN", 290);
            d.length = new Vector2(250f, 280f);
            d.chunks = BasChunkIds.Select(c => Chunk(BasChunkPath(c))).ToArray();
            d.entry = Chunk(BasChunkPath("Entry"));
            d.exit = Chunk(BasChunkPath("Exit"));
            d.obstacles = new[] { trolley, aFrame, barrel, bag };
            d.jumpObstacles = new[] { barrel, aFrame, trolley };
            d.hazards = ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_OncomingPair", "HZ_Crossing", "HZ_SidewalkStroll", "HZ_PoliceStop");
            var people = new List<GameObject>(K.People(theme, "Shopper", "Shopper", "Elder", "Elder", "Kid", "Kid", "Student", "Barista", "DogWalker", "Jogger", "Tourist"));
            people.Add(galah); people.Add(galah); people.Add(magpie);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { sleepers };
            d.rampFrequency = 1f;
            // A warm afternoon in an old town: honey light on cream parapets, jacaranda shade, the scarp a clear blue line.
            d.fogColor = new Color(.70f, .80f, .93f);
            d.skyTint = new Color(1f, .99f, .97f);
            d.ambientSky = new Color(.70f, .82f, .97f);
            d.ambientEquator = new Color(.92f, .88f, .80f);
            d.ambientGround = new Color(.74f, .62f, .52f);
            d.sunColor = new Color(1f, .95f, .85f);
            d.sunIntensity = 1.45f;
            d.farScenery = far;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/Bassendean] " + string.Join("; ", K.Report));
            if (!atlas) return;
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("SteamLocomotive", null, Make(m => SteamLoco(m, Vector3.zero))),
                ("MuseumCanopy", null, Make(m => MuseumCanopy(m, V(0, 0, 0), 8f, 19f))),
                ("MuseumBuilding", null, Make(m => MuseumBuilding(m, new CqLot(-1, -6f, 6f, 0f, 8f, 0f)))),
                ("SignalBox", null, Make(m => SignalBox(m, Vector3.zero))),
                ("SemaphoreAndWaterColumn", null, Make(m => { Semaphore(m, Vector3.zero); WaterColumn(m, V(2.5f, 0, 0)); })),
                ("WarMemorial", null, Make(m => WarMemorial(m, Vector3.zero))),
                ("OvalGate", null, Make(m => OvalGate(m, new CqLot(-1, -7f, 7f, 0f, 1f, 0f)))),
                ("Grandstand", null, Make(m => m.Add(Make(g => Grandstand(g, Vector3.zero, 17f, 9f, 1)), Vector3.zero, Quaternion.Euler(0, 180f, 0), Vector3.one))),
                ("GoalPosts", null, Make(m => GoalPosts(m, V(0, 0, 0), 5f, true))),
                ("LightTower", null, Make(m => LightTower(m, Vector3.zero, 30f))),
                ("StationOverpass", null, Make(m => Overpass(m, 0f))),
                ("HotelBrick", null, Make(m => VerandahHotel(m, new CqLot(-1, -11f, 11f, 0f, 11f, 0f), true))),
                ("PadburyBuildings", null, Make(m => TwoStoreyShop(m, new CqLot(-1, -8f, 8f, 0f, 10f, 0f), K.Hex("F3F0E6"), K.Hex("7A2E2A"), "1918"))),
                ("Library", null, Make(m => Library(m, new CqLot(-1, -9f, 9f, 0f, 10f, 0f)))),
                ("Jacaranda", null, Make(m => Jacaranda(m, Vector3.zero, 1f, 3))),
                ("PaperbarkStreetTree", null, Make(m => { K.TreePit(m, Vector3.zero, 1.3f); Paperbark(m, Vector3.zero, 6.5f, 5); })),
                ("LuggageTrolley", trolley.gameObject, null),
                ("AFrame", aFrame.gameObject, null),
                ("BarrelPlanter", barrel.gameObject, null),
                ("TackleBag", bag.gameObject, null),
                ("Galah", galah, null),
                ("Magpie", magpie, null),
                ("SleeperRamp", sleepers.gameObject, null),
            };
            K.RenderAtlas("art-src/districts/bassendean/props-atlas.png", items);
        }

        // ---------------------------------------------------------------- Bassendean chunks

        static void BasStreet(CqChunkDraft d) => Street(d.Road, d.Length, RedAsphalt, BrickPaver, KerbCap);

        /// <summary>A paperbark street tree in a tree pit on the footpath (Old Perth Road's street trees).</summary>
        static void PaperbarkStreet(CqChunkDraft d, int s, float z, int seed)
        {
            Vector3 p = V(s * (K.Edge - 1f), d.Ground(z), z);
            K.TreePit(d.Props, p, 1.3f);
            Paperbark(d.Props, p + V(0, .3f, 0), 6.5f, seed);
            K.Thin(d.Collisions, "Paperbark", p, 3f);
        }

        static void BasLamp(CqChunkDraft d, int s, float z) => SwanNeckLamp(d.Props, d.Collisions, V(s * (K.Half + .7f), d.Ground(z), z), K.Hex("22262B"), false);

        static CqChunkDraft BasEntry()
        {
            // Calm at z = 0 (hedges, lawn, gums), then the railway behind its fence on the left and the first cottages
            // and jacarandas on the right.
            var d = new CqChunkDraft("Bassendean_Entry", BasDrop);
            BasStreet(d);
            GradeRail(d, 0f, 40f, CharcoalDark, 12f, 32f);
            Hedge(d.SlopedFrontage, -1, .4f, 14f, 1.1f, 2.1f, 1.3f);
            Gum(d.Props, V(-(K.Edge + 2.6f), d.Ground(20f), 20f), 10f, 301);
            DeepSuburb(d, -1, 0f, 40f, 2901, 44f);
            SideGround(d, 1, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            Hedge(d.SlopedFrontage, 1, .4f, 13f, .5f, 1.6f, 1.2f);
            Gum(d.Props, V(K.Edge + 5f, d.Ground(5f), 5f), 12f, 302);
            Jacaranda(d.Props, V(K.Edge + 3.6f, d.Ground(15.5f), 15.5f), 1.05f, 303);
            Bungalow(d.SlopedFrontage, new CqLot(1, 18f, 29f, K.Edge + 6f, 10f, K.Kerb), K.Hex("E9E2CF"), RedIron, 2);
            Bungalow(d.SlopedFrontage, new CqLot(1, 30f, 40f, K.Edge + 5.5f, 10f, K.Kerb), RedBrick, GreyIron, 1);
            WallBox(d, K.Edge + 1f, K.Edge + 40f, 0f, 40f, 8f);
            DeepSuburb(d, 1, 0f, 40f, 2902, 26f);
            BasLamp(d, 1, 28f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft BasStation()
        {
            // Bassendean station: the island platform under its flat navy-framed canopy, the covered overpass with its
            // glazed lift tower and stairs, a train at the platform. Right: the black corner building with the
            // crenellated parapet opposite the station, and two shops.
            var d = new CqChunkDraft("Bassendean_Station", BasDrop);
            BasStreet(d);
            GradeRail(d, 0f, 40f, CharcoalDark, 4f);
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Concrete;
            Strip(m, V(-20.8f, AshBallastY - .2f, 2f), V(-17.2f, AshBallastY + 1.05f, 38f), BlockGrey);
            Ground(m, -20.75f, -17.25f, 2f, 38f, AshBallastY + 1.06f, BrickPlatform, ComicSurface.Paving);
            Ground(m, -17.75f, -17.3f, 2f, 38f, AshBallastY + 1.07f, HazardYellow);
            // the platform canopy: a flat roof on navy frames, cream soffit
            m.Surface = ComicSurface.PaintedMetal;
            Strip(m, V(-21f, AshBallastY + 4.3f, 4f), V(-17f, AshBallastY + 4.55f, 22f), NavyFrame);
            K.Face(m, V(-20.9f, AshBallastY + 4.28f, 4f), V(-20.9f, AshBallastY + 4.28f, 22f), V(-17.1f, AshBallastY + 4.28f, 22f), V(-17.1f, AshBallastY + 4.28f, 4f), Cream, Vector3.down);
            for (float z = 5f; z < 22f; z += 5.5f) m.Box(V(-19f, AshBallastY + 2.7f, z), V(.2f, 3.3f, .2f), NavyFrame);
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(-20.2f, AshBallastY + 1.06f, 8f), V(-17.8f, AshBallastY + 3.6f, 13f), BlockGrey);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(-17.74f, AshBallastY + 3.2f, 16.5f), V(.06f, .55f, 4f), NavyFrame);
            TextFacingX(m, "BASSENDEAN", V(-17.7f, AshBallastY + 3.2f, 16.5f), 1, .06f, White);
            Overpass(m, 29f);
            Sloped(d, d.Landmark, t => Train(t, V(AshTrackB, AshBallastY + .16f, 1.5f), 2, 1, 1, true, true, 18f));
            Industry(d, 0f, 40f, 2911, 1);
            NameBoard(d.Props, V(-(K.Edge + 2.4f), d.Ground(9f), 9f), "BASSENDEAN", .07f, NavyFrame, White, 2.6f);
            // right
            SideGround(d, 1, 0, 40, 62f, BrickPaver);
            ComicMesh f = d.SlopedFrontage;
            ParapetShop(f, new CqLot(1, 0f, 13f, K.Edge + .3f, 11f, K.Kerb), K.Hex("2B2E33"), K.Hex("3D434A"), 4, 2, 2);
            ParapetShop(f, new CqLot(1, 13f, 22f, K.Edge + .3f, 11f, K.Kerb), CreamBrick, K.Hex("2F6B4A"), 1, 3);
            ParapetShop(f, new CqLot(1, 22f, 31f, K.Edge + .3f, 11f, K.Kerb), K.Hex("F3F0E6"), K.Hex("8A3B34"), 3, 6);
            ParapetShop(f, new CqLot(1, 31f, 40f, K.Edge + .3f, 11f, K.Kerb), RedBrick, Navy, 0, 0);
            WallBox(d, K.Edge + .3f, K.Edge + 13f, 0f, 40f, 9f);
            DeepSuburb(d, 1, 0f, 40f, 2912, 30f);
            PaperbarkStreet(d, 1, 12f, 311);
            PaperbarkStreet(d, 1, 33f, 312);
            BasLamp(d, 1, 22f);
            BasLamp(d, -1, 6f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft BasMuseum()
        {
            // The rail museum beside the line: the weatherboard station-style building, the little signal box, a black
            // steam locomotive with red buffer beams under the red-framed canopy, a semaphore and a water column on the
            // gravel; in the road, the old loading ramp of sleepers. Right: the banded-brick library and shops.
            var d = new CqChunkDraft("Bassendean_Museum", BasDrop);
            BasStreet(d);
            GradeRail(d, 0f, 40f, CharcoalDark, 24f, false, 10f, 30f);
            Ground(d.Road, -(AshRailX + 24f), -(K.Edge - .02f), 0f, 40f, K.Kerb + .02f, K.Hex("C9C2AE"), ComicSurface.Concrete);
            ComicMesh m = d.SlopedFrontage;
            LowWall(m, -1, 0f, 2f, .5f);
            MuseumBuilding(m, new CqLot(-1, 2f, 15f, K.Edge + 5f, 8f, K.Kerb));
            SignalBox(m, V(-(K.Edge + 3.2f), K.Kerb, 18.2f));
            MuseumCanopy(m, V(-(K.Edge + 6.2f), K.Kerb, 22f), 7.6f, 17.4f);
            m.Add(Make(l => SteamLoco(l, Vector3.zero)), V(-(K.Edge + 6.2f), K.Kerb + .18f, 22.5f), Quaternion.identity, Vector3.one);
            Semaphore(m, V(-(K.Edge + 1.5f), K.Kerb, 20.3f));
            WaterColumn(m, V(-(K.Edge + 1.6f), K.Kerb, 37.5f));
            CrossingSign(m, V(-(K.Edge + 1.3f), K.Kerb, 3.2f));
            m.Add(LuggageTrolleyMesh(), V(-(K.Edge + 3.4f), K.Kerb + .36f, 10.5f), Quaternion.Euler(0, 20f, 0), Vector3.one);
            // post-and-chain edge along the footpath
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = 16.5f; z < 40f; z += 3f) { m.Box(V(-(K.Edge + .4f), K.Kerb + .4f, z), V(.12f, .8f, .12f), White); m.Box(V(-(K.Edge + .4f), K.Kerb + .84f, z), V(.16f, .08f, .16f), MuseumRed); }
            for (float z = 16.5f; z < 37f; z += 3f) m.Beam(V(-(K.Edge + .4f), K.Kerb + .68f, z), V(-(K.Edge + .4f), K.Kerb + .68f, z + 3f), .04f, K.R.ink);
            // a second shed and a green diesel behind (no outline)
            Sloped(d, d.Deep, t =>
            {
                t.Surface = ComicSurface.Metal;
                K.MinMaxBox(t, V(-31f, -1f, 3f), V(-22.5f, 6.2f, 18f), K.Hex("B9BEC2"));
                GableRoofZ(t, V(-26.75f, 6.2f, 10.5f), 9.4f, 15.8f, 1.7f, Zinc, K.Hex("B9BEC2"));
                K.MinMaxBox(t, V(-27f, K.Kerb + .5f, 22f), V(-24.2f, K.Kerb + 3.9f, 37f), K.Hex("2F6B4A"));
                K.MinMaxBox(t, V(-27.05f, K.Kerb + 2.1f, 22.5f), V(-24.15f, K.Kerb + 2.5f, 36.5f), K.Hex("E9D9A6"));
                K.MinMaxBox(t, V(-27.02f, K.Kerb + 1.2f, 21.98f), V(-24.18f, K.Kerb + 1.7f, 22.2f), LocoRed);
                K.MinMaxBox(t, V(-26.6f, K.Kerb, 23f), V(-24.6f, K.Kerb + .7f, 36f), K.R.ink);
            });
            WallBox(d, -(K.Edge + .4f), -(K.Edge + 40f), 0f, 40f, 9f);
            DeepSuburb(d, -1, 0f, 40f, 2921, 60f);
            // the sleeper ramp in the road
            var spec = new CqRampSpec { Name = "SleeperRamp", X = 0f, Z = SleeperRampZ, Width = SleeperRampWidth, Rise = SleeperRampRise, Height = SleeperRampHeight, Top = SleeperRampTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, Timber, TimberDark, false, 0f);
            deck.Surface = ComicSurface.Metal;
            foreach (float x in new[] { -.72f, .72f })
            {
                deck.Beam(V(x, .08f, SleeperRampZ + .2f), V(x, SleeperRampHeight + .08f, SleeperRampZ + SleeperRampRise), .1f, RailSteel);
                deck.Beam(V(x, SleeperRampHeight + .08f, SleeperRampZ + SleeperRampRise), V(x, SleeperRampHeight + .08f, SleeperRampZ + SleeperRampRise + SleeperRampTop - .3f), .1f, RailSteel);
            }
            deck.Surface = ComicSurface.PaintedMetal;
            deck.Box(V(0, SleeperRampHeight + .3f, SleeperRampZ + SleeperRampRise + SleeperRampTop - .2f), V(2.2f, .3f, .2f), LocoRed);
            foreach (float x in new[] { -.85f, .85f }) deck.Box(V(x, SleeperRampHeight + .15f, SleeperRampZ + SleeperRampRise + SleeperRampTop - .2f), V(.16f, .3f, .16f), K.R.ink);
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            // right: the library and two shops
            SideGround(d, 1, 0, 40, 62f, BrickPaver);
            ComicMesh f = d.SlopedFrontage;
            Library(f, new CqLot(1, 1f, 21f, K.Edge + 2.4f, 12f, K.Kerb));
            ParapetShop(f, new CqLot(1, 22f, 31f, K.Edge + .3f, 11f, K.Kerb), K.Hex("E9E2CF"), K.Hex("2F4F6B"), 2, 4);
            ParapetShop(f, new CqLot(1, 31f, 40f, K.Edge + .3f, 11f, K.Kerb), K.Hex("D8845A"), K.Hex("3D434A"), 1, 1);
            WallBox(d, K.Edge + .3f, K.Edge + 14f, 0f, 40f, 9f);
            DeepSuburb(d, 1, 0f, 40f, 2922, 30f);
            PaperbarkStreet(d, 1, 26f, 321);
            BasLamp(d, 1, 6f);
            BasLamp(d, 1, 36f);
            foreach (float x in new[] { -2.6f, 0f, 2.6f }) d.Slot(x, 3f);
            d.GiftSlots.Add(new Vector2(0f, 24f));
            foreach (float x in new[] { -2.6f, 0f, 2.6f }) d.Slot(x, 31f);
            return d;
        }

        static CqChunkDraft BasOldPerthRd()
        {
            // Old Perth Road: single-storey brick shops with cream parapets and cantilever awnings on tie rods, the white
            // two-storey Padbury's Buildings with its curved pediment dated 1918, and the red-brick corner hotel with its
            // two-storey verandah; paperbark street trees, black lamps, cafe umbrellas.
            var d = new CqChunkDraft("Bassendean_OldPerthRd", BasDrop);
            BasStreet(d);
            ComicMesh f = d.SlopedFrontage;
            SideGround(d, -1, 0, 40, 62f, BrickPaver);
            ParapetShop(f, new CqLot(-1, 0f, 8.5f, K.Edge + .3f, 11f, K.Kerb), RedBrick, K.Hex("2F6B4A"), 3, 5);
            ParapetShop(f, new CqLot(-1, 8.5f, 16f, K.Edge + .3f, 11f, K.Kerb), CreamBrick, K.Hex("8A3B34"), 1, 2);
            TwoStoreyShop(f, new CqLot(-1, 16f, 32f, K.Edge + .3f, 11f, K.Kerb), K.Hex("F3F0E6"), K.Hex("7A2E2A"), "1918");
            ParapetShop(f, new CqLot(-1, 32f, 40f, K.Edge + .3f, 11f, K.Kerb), K.Hex("C9D6CF"), Navy, 2, 0);
            WallBox(d, -(K.Edge + .3f), -(K.Edge + 13f), 0f, 40f, 10f);
            DeepSuburb(d, -1, 0f, 40f, 2931, 30f);
            SideGround(d, 1, 0, 40, 62f, BrickPaver);
            VerandahHotel(f, new CqLot(1, 0f, 22f, K.Edge + .3f, 12f, K.Kerb), true);
            ParapetShop(f, new CqLot(1, 22f, 31f, K.Edge + .3f, 11f, K.Kerb), K.Hex("EDE3C4"), K.Hex("3D434A"), 2, 3);
            ParapetShop(f, new CqLot(1, 31f, 40f, K.Edge + .3f, 11f, K.Kerb), RedBrick, K.Hex("2F4F6B"), 3, 6);
            WallBox(d, K.Edge + .3f, K.Edge + 13f, 0f, 40f, 10f);
            DeepSuburb(d, 1, 0f, 40f, 2932, 30f);
            PaperbarkStreet(d, -1, 12f, 331);
            PaperbarkStreet(d, -1, 35f, 332);
            PaperbarkStreet(d, 1, 26.5f, 333);
            BasLamp(d, -1, 24f);
            BasLamp(d, 1, 37f);
            K.CafeUmbrella(d.Props, d.Collisions, V(K.Edge - 1.4f, d.Ground(8f), 8f), K.Hex("2A8F8A"));
            K.CafeUmbrella(d.Props, d.Collisions, V(K.Edge - 1.4f, d.Ground(15f), 15f), K.Hex("2A8F8A"));
            Bench(d.Props, V(-(K.Edge - .8f), d.Ground(20f), 20f), -90f);
            d.Collisions.Add(new DressingObstacle { id = "Bench", position = V(-(K.Edge - .8f), d.Ground(20f), 20f), rotation = V(0, -90f, 0), centre = V(0, .45f, 0), size = V(1.8f, .9f, .5f), canJumpOver = true });
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft BasMemorial()
        {
            // BIC Reserve on the left: the war memorial (a rough-hewn grey granite obelisk on a stepped base), the curved
            // white memorial wall and its round reflection pond, a flagpole, clipped hedges, jacarandas. Background only:
            // nothing here can be hit. Right: shops, then cottages under jacarandas.
            var d = new CqChunkDraft("Bassendean_Memorial", BasDrop);
            BasStreet(d);
            SideGround(d, -1, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            ComicMesh g = d.Road;
            for (float z = 1f; z < 38f; z += 6f) Ground(g, -(K.Edge + 40f), -(K.Edge + .3f), z, z + 3f, K.Kerb + .06f, LawnLight, ComicSurface.Foliage);
            Hedge(d.SlopedFrontage, -1, 0f, 14.5f, .3f, 1.2f, .8f);
            Hedge(d.SlopedFrontage, -1, 25.5f, 40f, .3f, 1.2f, .8f);
            // the paved forecourt from the footpath to the memorial, the round pond set flush in it
            Ground(g, -(K.Edge + 6f), -K.Edge, 15f, 25f, K.Kerb + .09f, K.Hex("D9D3C4"), ComicSurface.Paving);
            Ground(g, -(K.Edge + 15f), -(K.Edge + 6f), 12f, 28f, K.Kerb + .09f, K.Hex("D9D3C4"), ComicSurface.Paving);
            Pool(g, V(-(K.Edge + 4f), K.Kerb + .1f, 20f), 1.9f, 1.9f, K.Hex("3F7FA6"), K.Hex("7FB7D0"), 7);
            Sloped(d, d.Landmark, t =>
            {
                WarMemorial(t, V(-(K.Edge + 10.5f), K.Kerb + .09f, 20f));
                MemorialWall(t, V(-(K.Edge + 10.5f), K.Kerb + .09f, 20f), 6.6f);
                Flagpole(t, V(-(K.Edge + 5.5f), K.Kerb + .05f, 12.6f));
                t.Surface = ComicSurface.Concrete;
                foreach (float z in new[] { 15.6f, 24.4f }) foreach (float x in new[] { 1.6f, 4.6f }) t.Box(V(-(K.Edge + x), K.Kerb + .28f, z), V(.9f, .45f, .5f), StoneWhite);
            });
            Jacaranda(d.Props, V(-(K.Edge + 6f), d.Ground(5f), 5f), 1.15f, 341);
            Jacaranda(d.Props, V(-(K.Edge + 7f), d.Ground(34f), 34f), 1.1f, 342);
            Gum(d.Deep, V(-(K.Edge + 24f), d.Ground(10f), 10f), 16f, 343, true);
            Gum(d.Deep, V(-(K.Edge + 27f), d.Ground(31f), 31f), 15f, 344, true);
            Jacaranda(d.Deep, V(-(K.Edge + 21f), d.Ground(22f), 22f), 1.4f, 345, false, true);
            Bench(d.Props, V(-(K.Edge + 2.6f), d.Ground(29.5f), 29.5f), -90f);
            WallBox(d, -(K.Edge + .3f), -(K.Edge + 40f), 0f, 14.5f, 6f);
            WallBox(d, -(K.Edge + .3f), -(K.Edge + 40f), 25.5f, 40f, 6f);
            WallBox(d, -(K.Edge + 3f), -(K.Edge + 40f), 14.5f, 25.5f, 8f);
            DeepSuburb(d, -1, 0f, 40f, 2941, 36f);
            // right: two shops, then cottages and jacarandas
            SideGround(d, 1, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            ComicMesh f = d.SlopedFrontage;
            Ground(g, K.Edge, K.Edge + 13f, 0f, 18.4f, K.Kerb + .07f, BrickPaver, ComicSurface.Paving);
            ParapetShop(f, new CqLot(1, 0f, 9f, K.Edge + .3f, 11f, K.Kerb), CreamBrick, K.Hex("2F6B4A"), 2, 1);
            ParapetShop(f, new CqLot(1, 9f, 18f, K.Edge + .3f, 11f, K.Kerb), K.Hex("F3F0E6"), K.Hex("8A3B34"), 0, 4);
            Jacaranda(d.Props, V(K.Edge + 3.4f, d.Ground(21.5f), 21.5f), 1.1f, 346);
            Bungalow(d.SlopedFrontage, new CqLot(1, 25f, 37f, K.Edge + 6f, 10f, K.Kerb), RedBrick, GreyIron, 1);
            Jacaranda(d.Props, V(K.Edge + 3f, d.Ground(39f), 39f), .9f, 347);
            WallBox(d, K.Edge + .3f, K.Edge + 40f, 0f, 40f, 8f);
            DeepSuburb(d, 1, 0f, 40f, 2942, 30f);
            PaperbarkStreet(d, 1, 12f, 348);
            BasLamp(d, -1, 30f);
            BasLamp(d, 1, 4f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft BasOval()
        {
            // Bassendean Oval on the right: the 1929 gate (BASSENDEAN RECREATION RESERVE), the white picket fence, the
            // four goal posts close to the road, the two cream weatherboard grandstands facing back up the ground under
            // their oversailing zinc roofs, a light tower between them. Left: cottages under jacarandas.
            var d = new CqChunkDraft("Bassendean_Oval", BasDrop);
            BasStreet(d);
            SideGround(d, 1, 0, 40, 62f, OvalGreen, ComicSurface.Foliage);
            ComicMesh g = d.Road, f = d.SlopedFrontage;
            for (float x = K.Edge + 7f; x < K.Edge + 60f; x += 10f) Ground(g, x, x + 5f, 0f, 31f, K.Kerb + .06f, OvalStripe, ComicSurface.Foliage);
            // the white boundary line, the goal square and the behind line by the near goal
            Ground(g, K.Edge + 6.1f, K.Edge + 6.3f, 0f, 31f, K.Kerb + .1f, White);
            Ground(g, K.Edge + 6.3f, K.Edge + 14f, 16.9f, 17.1f, K.Kerb + .1f, White);
            Ground(g, K.Edge + 6.3f, K.Edge + 14f, 21.9f, 22.1f, K.Kerb + .1f, White);
            Ground(g, K.Edge + 13.9f, K.Edge + 14.1f, 16.9f, 22.1f, K.Kerb + .1f, White);
            OvalGate(f, new CqLot(1, 1f, 13f, K.Edge + 1.6f, 1f, K.Kerb));
            // the fence along the street after the gate: a low cream wall, then the picket fence round the ground
            LowWall(f, 1, 13f, 40f, .45f);
            PicketFence(f, K.Edge + 5f, 0f, 31f, K.Kerb);
            GoalPosts(f, V(K.Edge + 6.2f, K.Kerb, 19.5f), 5f, true);
            Sloped(d, d.Deep, t => GoalPosts(t, V(K.Edge + 58f, K.Kerb, 16f), 5f, false));
            Grandstand(f, V(K.Edge + 8.5f, K.Kerb, 30.8f), 17f, 8.6f, 0);
            Sloped(d, d.Landmark, t => Grandstand(t, V(K.Edge + 27.5f, K.Kerb, 30.8f), 19f, 8.6f, 1));
            Sloped(d, d.Landmark, t => LightTower(t, V(K.Edge + 26.5f, K.Kerb, 36f), 34f));
            Sloped(d, d.Deep, t =>
            {
                LightTower(t, V(K.Edge + 60f, K.Kerb, 34f), 34f);
                LightTower(t, V(K.Edge + 62f, K.Kerb, 2f), 34f);
                Scoreboard(t, V(K.Edge + 61f, K.Kerb, 14f));
                // the grass bank and far fence beyond the ground
                t.Surface = ComicSurface.Foliage;
                Strip(t, V(K.Edge + 64f, -1f, 0f), V(K.Edge + 70f, K.Kerb + 1.6f, 31f), Color.Lerp(OvalGreen, K.R.ink, .12f));
            });
            Galahs(d.Props, d, K.Edge + 9f, 8f);
            WallBox(d, K.Edge + .5f, K.Edge + 40f, 0f, 40f, 9f);
            BasLamp(d, 1, 16f);
            // left: cottages and jacarandas
            SideGround(d, -1, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            Bungalow(f, new CqLot(-1, 1f, 12f, K.Edge + 6f, 10f, K.Kerb), K.Hex("D9C9A8"), RedIron, 1);
            Bungalow(f, new CqLot(-1, 14.5f, 25.5f, K.Edge + 5.5f, 10f, K.Kerb), RedBrick, GreyIron, 2);
            Bungalow(f, new CqLot(-1, 28f, 39f, K.Edge + 6.5f, 10f, K.Kerb), K.Hex("E9E2CF"), TileBrown, 0);
            Jacaranda(d.Props, V(-(K.Edge + 3.2f), d.Ground(13.2f), 13.2f), 1.1f, 351);
            Jacaranda(d.Props, V(-(K.Edge + 3.4f), d.Ground(26.8f), 26.8f), 1f, 352);
            WallBox(d, -(K.Edge + 1f), -(K.Edge + 40f), 0f, 40f, 8f);
            DeepSuburb(d, -1, 0f, 40f, 2951, 26f);
            PowerPole(d.Props, V(-(K.Edge - .7f), d.Ground(20f), 20f));
            K.Thin(d.Collisions, "Pole", V(-(K.Edge - .7f), d.Ground(20f), 20f), 4f, .35f);
            Sloped(d, d.Deep, t => PowerLines(t, -(K.Edge - .7f), K.Kerb + 8.5f, 0f, 40f));
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft BasExit()
        {
            // Towards the river and Point Reserve: the last cottages, then lawn, river gums and hedges at z = 40.
            var d = new CqChunkDraft("Bassendean_Exit", BasDrop);
            BasStreet(d);
            foreach (int s in new[] { -1, 1 }) SideGround(d, s, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            ComicMesh f = d.SlopedFrontage;
            Bungalow(f, new CqLot(-1, 1f, 12f, K.Edge + 6f, 10f, K.Kerb), RedBrick, RedIron, 0);
            Bungalow(f, new CqLot(1, 2f, 13f, K.Edge + 5.5f, 10f, K.Kerb), K.Hex("E9E2CF"), GreyIron, 1);
            Jacaranda(d.Props, V(-(K.Edge + 3.4f), d.Ground(15f), 15f), 1.1f, 361);
            Jacaranda(d.Props, V(K.Edge + 3.6f, d.Ground(17f), 17f), 1f, 362);
            foreach (int s in new[] { -1, 1 })
            {
                Hedge(f, s, 24f, 39.6f, .5f, 1.6f, 1.2f);
                Gum(d.Props, V(s * (K.Edge + 4.5f), d.Ground(27f + s), 27f + s), 14f, 363 + s);
                Gum(d.Props, V(s * (K.Edge + 6f), d.Ground(36f - s), 36f - s), 12f, 366 + s);
                WallBox(d, s * (K.Edge + 1f), s * (K.Edge + 40f), 0f, 40f, 8f);
                DeepSuburb(d, s, 0f, 40f, 2961 + s, 26f);
            }
            BasLamp(d, 1, 10f);
            d.StandardSlots();
            return d;
        }

        // ---------------------------------------------------------------- station parts

        /// <summary>The covered overpass at z (flat road space): a glazed lift tower with grey panels and the stairs by the
        /// road, the covered bridge over the tracks on navy frames with a cream soffit and glass balustrades, a second
        /// lift down to the platform.</summary>
        static void Overpass(ComicMesh m, float z)
        {
            const float deck = 6.2f, roof = 9f;
            // the bridge
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, V(-29f, deck - .35f, z - 1.6f), V(-9.4f, deck, z + 1.6f), NavyFrame);
            K.MinMaxBox(m, V(-29.3f, roof, z - 1.9f), V(-9.1f, roof + .3f, z + 1.9f), NavyFrame);
            K.Face(m, V(-29.2f, roof - .02f, z - 1.8f), V(-9.2f, roof - .02f, z - 1.8f), V(-9.2f, roof - .02f, z + 1.8f), V(-29.2f, roof - .02f, z + 1.8f), Cream, Vector3.down);
            for (float x = -28.6f; x < -9.5f; x += 3.2f)
                foreach (float dz in new[] { -1.55f, 1.55f }) m.Box(V(x, (deck + roof) * .5f, z + dz), V(.14f, roof - deck, .14f), NavyFrame);
            m.Surface = ComicSurface.Glass;
            foreach (float dz in new[] { -1.58f, 1.58f }) K.MinMaxBox(m, V(-28.8f, deck, z + dz - .03f), V(-9.6f, deck + 1.25f, z + dz + .03f), K.Hex("BFDCE3"));
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -13.2f, -25.2f }) { m.Box(V(x, deck * .5f - .6f, z - 1.2f), V(.3f, deck + .6f, .3f), NavyFrame); m.Box(V(x, deck * .5f - .6f, z + 1.2f), V(.3f, deck + .6f, .3f), NavyFrame); }
            // lift towers: glazed shaft with grey panels and a navy cap (road side and platform)
            foreach (float x in new[] { -10.4f, -19f })
            {
                float baseY = x < -15f ? AshBallastY + 1.05f : K.Kerb;
                m.Surface = ComicSurface.Plaster;
                K.MinMaxBox(m, V(x - 1.3f, baseY - 1f, z + 1.7f), V(x + 1.3f, roof + 1.2f, z + 4.3f), K.Hex("C9CDD1"));
                m.Surface = ComicSurface.Glass;
                K.Face(m, V(x + 1.32f, baseY + .3f, z + 2.1f), V(x + 1.32f, baseY + .3f, z + 3.9f), V(x + 1.32f, roof + .7f, z + 3.9f), V(x + 1.32f, roof + .7f, z + 2.1f), WindowGlass, Vector3.right);
                K.Face(m, V(x - .9f, baseY + .3f, z + 1.68f), V(x + .9f, baseY + .3f, z + 1.68f), V(x + .9f, deck - .5f, z + 1.68f), V(x - .9f, deck - .5f, z + 1.68f), WindowGlass, Vector3.back);
                m.Surface = ComicSurface.PaintedMetal;
                for (float y = baseY + 2.4f; y < roof; y += 2.2f) K.Face(m, V(x + 1.33f, y, z + 2.1f), V(x + 1.33f, y, z + 3.9f), V(x + 1.33f, y + .08f, z + 3.9f), V(x + 1.33f, y + .08f, z + 2.1f), K.R.ink, Vector3.right, false);
                K.MinMaxBox(m, V(x - 1.45f, roof + 1.2f, z + 1.55f), V(x + 1.45f, roof + 1.5f, z + 4.45f), NavyFrame);
            }
            // the stairs along the footpath, coming down towards the rider
            Vector3 top = V(-10.4f, deck - .15f, z - 1.6f), bottom = V(-10.4f, K.Kerb + .1f, z - 13f);
            m.Surface = ComicSurface.Concrete;
            Slab(m, top, bottom, Vector3.right, 1.9f, 1.9f, .35f, K.Hex("7F858B"), NavyFrame);
            m.Surface = ComicSurface.Plain;
            for (int i = 1; i < 12; i++)
            {
                Vector3 c = Vector3.Lerp(top, bottom, i / 12f) + V(0, .19f, 0);
                K.Face(m, c + V(-.9f, 0, -.06f), c + V(.9f, 0, -.06f), c + V(.9f, 0, .06f), c + V(-.9f, 0, .06f), K.Hex("5B6168"), Vector3.up, false);
            }
            m.Surface = ComicSurface.Glass;
            foreach (float dx in new[] { -.95f, .95f })
                Slab(m, top + V(dx, .65f, 0), bottom + V(dx, .65f, 0), Vector3.up, 1f, 1f, .05f, K.Hex("BFDCE3"), K.Hex("9FC3CC"));
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float dx in new[] { -.95f, .95f }) m.Beam(top + V(dx, 1.2f, 0), bottom + V(dx, 1.2f, 0), .07f, NavyFrame);
            foreach (float t in new[] { .35f, .7f }) { Vector3 c = Vector3.Lerp(top, bottom, t); m.Box(V(c.x, c.y * .5f - .5f, c.z), V(.22f, c.y + 1f, .22f), NavyFrame); }
        }

        // ---------------------------------------------------------------- the rail museum

        /// <summary>The museum's main building, styled as a country station: cream weatherboards, a low gabled iron roof
        /// running out into a verandah on timber posts, central double doors, sash windows, a RAILWAY MUSEUM board.</summary>
        static void MuseumBuilding(ComicMesh f, CqLot l)
        {
            float W = l.W, D = l.Depth, b = .35f, h = b + 3.3f, vd = 2.6f, ridge = h + 2f;
            f.Surface = ComicSurface.Brick;
            K.LBox(f, l, -.1f, W + .1f, -2.6f, b, vd + .2f, -D, RedBrickDark);
            f.Surface = ComicSurface.Wood;
            K.LBox(f, l, 0, W, b, h, 0f, -D, Weatherboard);
            f.Surface = ComicSurface.Plain;
            for (float y = b + .3f; y < h - .1f; y += .3f) K.LQuad(f, l, .02f, W - .02f, y, y + .03f, .012f, WeatherboardShade, false);
            for (float y = b + .3f; y < h + 1.4f; y += .3f)
                K.Face(f, l.P(-.012f, y, 0f), l.P(-.012f, y, -D), l.P(-.012f, y + .03f, -D), l.P(-.012f, y + .03f, 0f), WeatherboardShade, Vector3.back, false);
            // double doors and four sash windows
            f.Surface = ComicSurface.PaintedMetal;
            K.LQuad(f, l, W * .5f - .95f, W * .5f + .95f, b, b + 2.5f, .02f, K.R.ink);
            f.Surface = ComicSurface.Wood;
            K.LQuad(f, l, W * .5f - .85f, W * .5f - .03f, b + .05f, b + 2.4f, .035f, MuseumRed);
            K.LQuad(f, l, W * .5f + .03f, W * .5f + .85f, b + .05f, b + 2.4f, .035f, MuseumRed);
            foreach (float a in new[] { W * .14f, W * .32f, W * .68f, W * .86f }) FramedWindow(f, l, a, 1f, b + .95f, b + 2.45f, .02f, White, true, true, true);
            // roof: one long gable (ridge along the road) whose front slope runs on over the verandah
            f.Surface = ComicSurface.Metal;
            Vector3 e0 = l.P(-.4f, h - .55f, vd + .25f), e1 = l.P(W + .4f, h - .55f, vd + .25f), r0 = l.P(-.4f, ridge, -D * .5f), r1 = l.P(W + .4f, ridge, -D * .5f), b0 = l.P(-.4f, h, -D - .35f), b1 = l.P(W + .4f, h, -D - .35f);
            Vector3 toRoad = new Vector3(-l.S, 0, 0);
            K.Face(f, e0, e1, r1, r0, RedIron, Vector3.up + toRoad);
            K.Face(f, r0, r1, b1, b0, Color.Lerp(RedIron, K.R.ink, .12f), Vector3.up - toRoad);
            K.Face(f, e0 + V(0, -.04f, 0), e1 + V(0, -.04f, 0), l.P(W + .4f, h - .04f, 0f), l.P(-.4f, h - .04f, 0f), Color.Lerp(RedIron, K.R.ink, .4f), Vector3.down);
            K.Face(f, e0, e1, e1 + V(0, -.18f, 0), e0 + V(0, -.18f, 0), K.R.ink, toRoad);
            foreach (float a in new[] { -.4f, W + .4f })
            {
                Vector3 n = a < 0 ? Vector3.back : Vector3.forward;
                f.Surface = ComicSurface.Wood;
                K.TriFacing(f, l.P(a, h, 0f), l.P(a, ridge, -D * .5f), l.P(a, h, -D - .35f), Weatherboard, n);
                K.TriFacing(f, l.P(a, h - .55f, vd + .25f), l.P(a, h, 0f), l.P(a, ridge, -D * .5f), RedIron, n);
            }
            f.Surface = ComicSurface.PaintedMetal;
            for (float a = .2f; a <= W - .19f; a += (W - .4f) / 4f)
            {
                K.LBox(f, l, a - .07f, a + .07f, b, h - .6f, vd, vd - .14f, White);
                f.Beam(l.P(a, h - 1.3f, vd - .07f), l.P(a + .6f, h - .68f, vd - .07f), .06f, White);
                f.Beam(l.P(a, h - 1.3f, vd - .07f), l.P(a - .6f, h - .68f, vd - .07f), .06f, White);
            }
            f.Surface = ComicSurface.Wood;
            K.LBox(f, l, 0f, W, b - .12f, b, vd + .1f, 0f, TimberDark);
            // the name board hanging under the verandah beam, a platform bench and a luggage barrow
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, W * .5f - 2.6f, W * .5f + 2.6f, h - 1.3f, h - .7f, vd + .06f, vd - .02f, K.Hex("2B3138"));
            TextOnLot(f, l, "RAILWAY MUSEUM", W * .5f, h - 1f, vd + .07f, .058f, K.Hex("F3E7C6"));
            Bench(f, l.P(W * .2f, b, 1f), l.S < 0 ? -90f : 90f);
            Chimney(f, l.P(W * .75f, ridge - .9f, -D * .5f), 1.8f, RedBrick);
        }

        /// <summary>A small square weatherboard signal box on a tall base, with a band of windows, a steep gable roof with a
        /// finial and an outside stair.</summary>
        static void SignalBox(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Wood;
            m.Box(p + V(0, 1.2f, 0), V(2.6f, 3.4f, 2.6f), Weatherboard);
            m.Box(p + V(0, 3.9f, 0), V(2.9f, 2f, 2.9f), Weatherboard);
            m.Surface = ComicSurface.Plain;
            for (float y = .3f; y < 2.8f; y += .3f)
            {
                K.Face(m, p + V(1.31f, y, -1.3f), p + V(1.31f, y, 1.3f), p + V(1.31f, y + .03f, 1.3f), p + V(1.31f, y + .03f, -1.3f), WeatherboardShade, Vector3.right, false);
                K.Face(m, p + V(-1.3f, y, -1.31f), p + V(1.3f, y, -1.31f), p + V(1.3f, y + .03f, -1.31f), p + V(-1.3f, y + .03f, -1.31f), WeatherboardShade, Vector3.back, false);
            }
            m.Surface = ComicSurface.PaintedMetal;
            K.Face(m, p + V(1.46f, 3.35f, -1.3f), p + V(1.46f, 3.35f, 1.3f), p + V(1.46f, 4.55f, 1.3f), p + V(1.46f, 4.55f, -1.3f), K.R.ink, Vector3.right);
            K.Face(m, p + V(-1.3f, 3.35f, -1.46f), p + V(1.3f, 3.35f, -1.46f), p + V(1.3f, 4.55f, -1.46f), p + V(-1.3f, 4.55f, -1.46f), K.R.ink, Vector3.back);
            m.Surface = ComicSurface.Glass;
            for (int i = 0; i < 3; i++)
            {
                float a = -1.2f + i * .82f;
                K.Face(m, p + V(1.47f, 3.45f, a), p + V(1.47f, 3.45f, a + .74f), p + V(1.47f, 4.45f, a + .74f), p + V(1.47f, 4.45f, a), WindowGlass, Vector3.right);
                K.Face(m, p + V(a, 3.45f, -1.47f), p + V(a + .74f, 3.45f, -1.47f), p + V(a + .74f, 4.45f, -1.47f), p + V(a, 4.45f, -1.47f), WindowGlass, Vector3.back);
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 4.95f, 0), V(3.3f, .14f, 3.3f), K.R.ink);
            m.Surface = ComicSurface.Metal;
            GableRoofX(m, p + V(0, 5f, 0), 3.4f, 3.4f, 1.9f, RedIron, Weatherboard);
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, p + V(1.7f, 6.7f, 0), p + V(1.7f, 7.6f, 0), .06f, .02f, White, 4, true);
            // outside stair on the far (+Z) side, with a landing
            m.Surface = ComicSurface.Wood;
            Slab(m, p + V(-1.2f, 0f, 2.2f), p + V(1.1f, 2.9f, 2.2f), Vector3.forward, .9f, .9f, .16f, TimberDark, TimberDark);
            m.Beam(p + V(-1.2f, 1f, 2.6f), p + V(1.1f, 3.9f, 2.6f), .07f, White);
            m.Box(p + V(1.6f, 2.9f, 2.1f), V(1.2f, .14f, 1.3f), TimberDark);
        }

        /// <summary>The open-sided display canopy: red-painted steel columns and trusses under a zinc gable roof, the
        /// display track on gravel beneath. Centre line at p.x, from p.z, len along z.</summary>
        static void MuseumCanopy(ComicMesh m, Vector3 p, float width, float len)
        {
            float hw = width * .5f, eave = 5.2f, ridge = 6.7f;
            // gravel bed, two rails and sleepers
            m.Surface = ComicSurface.Concrete;
            Strip(m, p + V(-1.7f, -.3f, -.5f), p + V(1.7f, .1f, len + .5f), Ballast);
            m.Surface = ComicSurface.Plain;
            for (float z = 0f; z < len; z += 1.2f) K.Face(m, p + V(-1.2f, .11f, z), p + V(-1.2f, .11f, z + .3f), p + V(1.2f, .11f, z + .3f), p + V(1.2f, .11f, z), K.Hex("6B5440"), Vector3.up, false);
            m.Surface = ComicSurface.Metal;
            foreach (float x in new[] { -.72f, .72f }) m.Box(p + V(x, .17f, len * .5f), V(.08f, .12f, len + .6f), RailSteel);
            // frames
            int frames = Mathf.Max(3, Mathf.RoundToInt(len / 6f) + 1);
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < frames; i++)
            {
                float z = len * i / (frames - 1);
                foreach (int s in new[] { -1, 1 })
                {
                    m.Box(p + V(s * hw, eave * .5f, z), V(.22f, eave, .22f), MuseumRed);
                    m.Box(p + V(s * hw, .12f, z), V(.5f, .24f, .5f), K.Hex("C9C5BA"));
                    m.Beam(p + V(s * hw, eave, z), p + V(0, ridge, z), .16f, MuseumRed);
                    m.Beam(p + V(s * hw, eave - 1f, z), p + V(s * (hw - 1.1f), eave, z), .09f, MuseumRed);
                }
                m.Box(p + V(0, eave, z), V(width, .16f, .16f), MuseumRed);
                m.Box(p + V(0, (eave + ridge) * .5f, z), V(.12f, ridge - eave, .12f), MuseumRed);
            }
            foreach (int s in new[] { -1, 1 }) m.Box(p + V(s * hw, eave + .05f, len * .5f), V(.16f, .16f, len), MuseumRed);
            m.Surface = ComicSurface.Metal;
            GableRoofZ(m, p + V(0, eave + .2f, len * .5f), width + 1.2f, len + 1.4f, ridge - eave + .1f, Zinc, MuseumRed);
        }

        /// <summary>A black WAGR-style steam locomotive and tender on display (no markings but its number): smokebox with a
        /// door, boiler with bands, chimney and domes, cab, running boards, four coupled driving wheels with a coupling
        /// rod, bogie and trailing wheels, cylinders, red buffer beams with buffers, a tender with coal. Front at -Z
        /// (z = 0), on the rails at y = 0, about 17 m long. The detailed side is +X (towards the road).</summary>
        static void SteamLoco(ComicMesh m, Vector3 p)
        {
            Vector3 P(float x, float y, float z) => p + V(x, y, z);
            // frames and running boards
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, P(-.95f, .75f, .7f), P(.95f, 1.25f, 10.4f), LocoBlack);
            K.MinMaxBox(m, P(-1.35f, 1.62f, 1.3f), P(1.35f, 1.74f, 8.2f), LocoGrey);
            K.Face(m, P(1.36f, 1.62f, 1.3f), P(1.36f, 1.62f, 8.2f), P(1.36f, 1.74f, 8.2f), P(1.36f, 1.74f, 1.3f), LocoRed, Vector3.right, false);
            // buffer beam, buffers, a little cowcatcher
            K.MinMaxBox(m, P(-1.3f, .85f, .4f), P(1.3f, 1.35f, .7f), LocoRed);
            foreach (float x in new[] { -.85f, .85f }) { Rod(m, P(x, 1.1f, .4f), P(x, 1.1f, .02f), .1f, .1f, LocoGrey, 6); Rod(m, P(x, 1.1f, .06f), P(x, 1.1f, -.04f), .24f, .24f, LocoGrey, 8, true); }
            Extrude(m, new[] { P(-1.1f, .2f, .1f), P(-1.1f, .85f, .55f), P(-1.1f, .2f, .7f) }, V(2.2f, 0, 0), LocoBlack);
            // smokebox, boiler, firebox
            m.Surface = ComicSurface.Metal;
            Rod(m, P(0, 2.55f, 2f), P(0, 2.55f, .75f), 1.02f, 1.02f, LocoBlack, 10, true);
            Rod(m, P(0, 2.55f, .78f), P(0, 2.55f, .66f), .8f, .7f, LocoGrey, 10, true);
            Rod(m, P(0, 2.55f, .7f), P(0, 2.55f, .58f), .12f, .1f, Brass, 6, true);
            m.Box(P(0, 2.55f, .6f), V(.9f, .06f, .05f), LocoGrey, Quaternion.Euler(0, 0, 40f));
            Rod(m, P(0, 2.55f, 2f), P(0, 2.55f, 7.6f), .93f, .93f, LocoBlack, 10);
            foreach (float z in new[] { 2.05f, 3.6f, 5.2f, 6.8f }) Rod(m, P(0, 2.55f, z), P(0, 2.55f, z + .1f), .96f, .96f, LocoGrey, 10);
            K.MinMaxBox(m, P(-.98f, 1.7f, 6.6f), P(.98f, 3.2f, 7.9f), LocoBlack);
            K.MinMaxBox(m, P(-.6f, 1.25f, .9f), P(.6f, 1.75f, 2f), LocoBlack);
            // chimney, dome, sand dome, safety valves, whistle
            Rod(m, P(0, 3.45f, 1.3f), P(0, 4.3f, 1.3f), .26f, .32f, LocoBlack, 8, true);
            Rod(m, P(0, 4.24f, 1.3f), P(0, 4.34f, 1.3f), .36f, .36f, LocoGrey, 8, true);
            m.Ellipsoid(P(0, 3.55f, 3.6f), V(.95f, .9f, .95f), LocoBlack);
            Rod(m, P(0, 3.4f, 5.3f), P(0, 3.85f, 5.3f), .34f, .3f, LocoBlack, 8, true);
            foreach (float x in new[] { -.18f, .18f }) Rod(m, P(x, 3.25f, 6.9f), P(x, 3.75f, 6.9f), .07f, .09f, Brass, 5, true);
            // cab: sides with a window and the number, a curved roof (three facets), a back opening
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, P(-1.3f, 1.25f, 7.9f), P(1.3f, 3.75f, 10f), LocoBlack);
            Extrude(m, new[] { P(-1.45f, 3.75f, 7.7f), P(-.75f, 4.05f, 7.7f), P(.75f, 4.05f, 7.7f), P(1.45f, 3.75f, 7.7f) }, V(0, 0, 2.7f), LocoGrey);
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 n = new Vector3(s, 0, 0);
                K.Face(m, P(s * 1.31f, 2.6f, 8.2f), P(s * 1.31f, 2.6f, 9.2f), P(s * 1.31f, 3.4f, 9.2f), P(s * 1.31f, 3.4f, 8.2f), K.Hex("7FA6B4"), n);
                K.Face(m, P(s * 1.32f, 2.56f, 8.16f), P(s * 1.32f, 2.56f, 9.24f), P(s * 1.32f, 2.62f, 9.24f), P(s * 1.32f, 2.62f, 8.16f), Brass, n, false);
                K.Face(m, P(s * 1.315f, 1.7f, 8.1f), P(s * 1.315f, 1.7f, 9.5f), P(s * 1.315f, 2.3f, 9.5f), P(s * 1.315f, 2.3f, 8.1f), LocoBlack, n);
            }
            TextFacingX(m, "W 953", P(1.33f, 2f, 8.8f), 1, .075f, LocoYellow);
            // +X side: cylinder, driving wheels with red rims... (kept black here), coupling rod, bogie and trailing wheels
            m.Surface = ComicSurface.Metal;
            Rod(m, P(1.15f, .95f, 1.9f), P(1.15f, .95f, 3f), .34f, .34f, LocoBlack, 8, true);
            Rod(m, P(1.15f, .95f, 3f), P(1.15f, .95f, 1.86f), .34f, .34f, LocoGrey, 8, true);
            float[] drivers = { 3.9f, 5.25f, 6.6f, 7.95f };
            foreach (int s in new[] { -1, 1 })
            {
                foreach (float z in drivers)
                {
                    m.Surface = ComicSurface.Rubber;
                    m.Cylinder(P(s * 1.02f, .62f, z), .62f, .16f, s > 0 ? LocoBlack : K.R.ink, s > 0 ? 10 : 6, 1f, Quaternion.Euler(0, 0, 90));
                    if (s < 0) continue;
                    m.Surface = ComicSurface.Metal;
                    m.Cylinder(P(1.11f, .62f, z), .2f, .06f, LocoGrey, 8, 1f, Quaternion.Euler(0, 0, 90));
                    for (int k = 0; k < 4; k++) m.Box(P(1.105f, .62f, z), V(.03f, 1.05f, .07f), LocoRed, Quaternion.Euler(k * 45f, 0, 0));
                }
                foreach (float z in new[] { 1.5f, 2.5f, 9.3f })
                    m.Cylinder(P(s * 1.02f, .38f, z), .38f, .14f, s > 0 ? LocoBlack : K.R.ink, s > 0 ? 8 : 6, 1f, Quaternion.Euler(0, 0, 90));
            }
            m.Surface = ComicSurface.Metal;
            m.Box(P(1.2f, .5f, (drivers[0] + drivers[3]) * .5f), V(.06f, .14f, drivers[3] - drivers[0] + .3f), K.Hex("C9CED3"));
            m.Beam(P(1.24f, .95f, 3f), P(1.24f, .5f, drivers[1]), .08f, K.Hex("C9CED3"));
            // tender: tank, coal, frame, three wheels a side, rear buffer beam
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, P(-1.3f, 1.2f, 10.5f), P(1.3f, 3.1f, 16.2f), LocoBlack);
            K.MinMaxBox(m, P(-1.36f, 3.1f, 10.45f), P(1.36f, 3.25f, 16.25f), LocoGrey);
            K.Face(m, P(1.31f, 1.9f, 11f), P(1.31f, 1.9f, 15.7f), P(1.31f, 1.96f, 15.7f), P(1.31f, 1.96f, 11f), LocoRed, Vector3.right, false);
            K.MinMaxBox(m, P(-.95f, .7f, 10.6f), P(.95f, 1.2f, 16.1f), LocoBlack);
            m.Surface = ComicSurface.Concrete;
            Blob(m, P(0, 3.3f, 12.2f), V(2.1f, .9f, 3f), K.Hex("1B1D21"), 6, 3);
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, P(-1.3f, .85f, 16.2f), P(1.3f, 1.35f, 16.5f), LocoRed);
            foreach (int s in new[] { -1, 1 })
                foreach (float z in new[] { 11.4f, 13.3f, 15.2f })
                    m.Cylinder(P(s * 1.02f, .45f, z), .45f, .14f, s > 0 ? LocoBlack : K.R.ink, s > 0 ? 8 : 6, 1f, Quaternion.Euler(0, 0, 90));
            m.Box(P(0, 1f, 10.25f), V(.5f, .14f, .6f), LocoGrey);
        }

        /// <summary>A lower-quadrant semaphore signal: white lattice-less post, a red arm with a white band, a lamp, a ladder.</summary>
        static void Semaphore(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, p, p + V(0, 6.4f, 0), .12f, .08f, White, 6, true);
            m.Box(p + V(0, .2f, 0), V(.4f, .4f, .4f), K.R.ink);
            Rod(m, p + V(0, 6.4f, 0), p + V(0, 6.9f, 0), .1f, .01f, K.R.ink, 5, true);
            Quaternion q = Quaternion.Euler(-28f, 0, 0);
            m.Box(p + V(.12f, 5.7f, 0) + q * V(0, 0, -.8f), V(.05f, .28f, 1.7f), LocoRed, q);
            m.Box(p + V(.15f, 5.7f, 0) + q * V(0, 0, -1.15f), V(.05f, .29f, .22f), White, q);
            m.Box(p + V(.12f, 5.7f, .2f), V(.2f, .3f, .3f), K.R.ink);
            m.Surface = ComicSurface.Glass;
            m.Box(p + V(.12f, 5.75f, .38f), V(.14f, .14f, .1f), K.Hex("3DBE5A"));
            m.Surface = ComicSurface.Metal;
            foreach (float z in new[] { .22f, .5f }) m.Box(p + V(0, 2.9f, z), V(.04f, 5.6f, .04f), LocoGrey);
            for (float y = .5f; y < 5.6f; y += .5f) m.Box(p + V(0, y, .36f), V(.04f, .04f, .3f), LocoGrey);
        }

        /// <summary>An old level-crossing sign: a white crossbuck with RAILWAY CROSSING on a post with red-and-white bands.</summary>
        static void CrossingSign(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, p, p + V(0, 3.6f, 0), .09f, .07f, White, 6, true);
            for (int i = 0; i < 3; i++) Rod(m, p + V(0, .3f + i * .6f, 0), p + V(0, .6f + i * .6f, 0), .095f, .09f, LocoRed, 6);
            foreach (float tilt in new[] { 28f, -28f })
            {
                Quaternion q = Quaternion.Euler(0, 0, tilt);
                m.Box(p + V(0, 3.2f, -.08f), V(2.5f, .3f, .05f), K.R.ink, q);
                m.Box(p + V(0, 3.2f, -.1f), V(2.42f, .24f, .05f), White, q);
            }
            Quaternion a = Quaternion.Euler(0, 0, 28f), b = Quaternion.Euler(0, 0, -28f);
            Vector3 c = p + V(0, 3.2f, -.13f);
            PixelText(m, "RAIL", c + a * V(-.72f, 0, 0), a * Vector3.right, a * Vector3.up, .036f, K.R.ink);
            PixelText(m, "WAY", c + a * V(.72f, 0, 0), a * Vector3.right, a * Vector3.up, .036f, K.R.ink);
            PixelText(m, "CROSS", c + b * V(-.72f, 0, 0), b * Vector3.right, b * Vector3.up, .036f, K.R.ink);
            PixelText(m, "ING", c + b * V(.72f, 0, 0), b * Vector3.right, b * Vector3.up, .036f, K.R.ink);
        }

        /// <summary>A water column for filling tenders: a cast-iron pillar, a swinging arm, a leather bag.</summary>
        static void WaterColumn(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, p, p + V(0, .5f, 0), .32f, .2f, LocoBlack, 8);
            Rod(m, p + V(0, .5f, 0), p + V(0, 3.9f, 0), .17f, .14f, MuseumRed, 8);
            Rod(m, p + V(0, 3.9f, 0), p + V(0, 4.2f, 0), .2f, .1f, LocoBlack, 8, true);
            m.Beam(p + V(0, 3.75f, 0), p + V(-2f, 3.9f, 0), .16f, MuseumRed);
            m.Surface = ComicSurface.Fabric;
            Rod(m, p + V(-2f, 3.85f, 0), p + V(-2.05f, 2.7f, 0), .12f, .09f, K.Hex("6B5440"), 6);
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, p + V(.25f, 1.2f, 0), p + V(.6f, 1.2f, 0), .04f, .04f, LocoBlack, 5);
            m.Cylinder(p + V(.62f, 1.2f, 0), .22f, .05f, LocoBlack, 8, 1f, Quaternion.Euler(0, 0, 90));
        }

        // ---------------------------------------------------------------- the war memorial (background only)

        /// <summary>Bassendean War Memorial: a slender rough-hewn pale grey granite obelisk in four stacked tapering
        /// blocks on a dressed cornice, a square pedestal with inscribed panels, a wide stepped base; a wreath of
        /// poppies at its foot.</summary>
        static void WarMemorial(ComicMesh m, Vector3 p)
        {
            Quaternion q = Quaternion.Euler(0, 45f, 0);
            const float k = 1.41421f;
            m.Surface = ComicSurface.Concrete;
            float y = 0f;
            foreach (float w in new[] { 4.4f, 3.5f, 2.6f })
            {
                m.Box(p + V(0, y + .14f, 0), V(w, .28f, w), w > 4f ? K.Hex("C9C5BA") : GraniteLight);
                y += .28f;
            }
            m.Box(p + V(0, y + .8f, 0), V(1.7f, 1.6f, 1.7f), GraniteLight);
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 4; i++)
            {
                Quaternion f = Quaternion.Euler(0, i * 90f, 0);
                Vector3 n = f * Vector3.back, c = p + V(0, y + .85f, 0) + n * .86f, r = f * Vector3.right;
                K.Face(m, c - r * .6f + V(0, -.55f, 0), c + r * .6f + V(0, -.55f, 0), c + r * .6f + V(0, .55f, 0), c - r * .6f + V(0, .55f, 0), GraniteDark, n);
                for (int line = 0; line < 5; line++)
                    K.Face(m, c + n * .01f - r * (.42f - line % 2 * .1f) + V(0, .34f - line * .17f, 0), c + n * .01f + r * (.42f - line % 2 * .1f) + V(0, .34f - line * .17f, 0), c + n * .01f + r * (.42f - line % 2 * .1f) + V(0, .4f - line * .17f, 0), c + n * .01f - r * (.42f - line % 2 * .1f) + V(0, .4f - line * .17f, 0), Brass, n, false);
            }
            y += 1.6f;
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, y + .11f, 0), V(2.05f, .22f, 2.05f), GraniteLight);
            y += .22f;
            float w0 = 1.3f;
            for (int i = 0; i < 4; i++)
            {
                float w1 = w0 * .86f, h = 1.05f;
                m.Cylinder(p + V(0, y + h * .5f, 0), w0 * .5f * k, h, i % 2 == 0 ? Granite : Color.Lerp(Granite, GraniteDark, .35f), 4, w1 / w0, q);
                // rough-hewn: a few chips on the faces
                m.Surface = ComicSurface.Plain;
                Quaternion f = Quaternion.Euler(0, i * 90f, 0);
                Vector3 n = f * Vector3.back, c = p + V(0, y + h * .5f, 0) + n * (w0 * .47f), r = f * Vector3.right;
                K.Face(m, c - r * .2f + V(0, -.15f, 0), c + r * .15f + V(0, -.2f, 0), c + r * .22f + V(0, .12f, 0), c - r * .12f + V(0, .2f, 0), GraniteDark, n, false);
                m.Surface = ComicSurface.Concrete;
                y += h + .03f;
                w0 = w1 * .97f;
            }
            m.Cylinder(p + V(0, y + .3f, 0), w0 * .5f * k, .6f, GraniteLight, 4, .05f, q);
            // the wreath
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Blob(m, p + V(2.35f + Mathf.Cos(a) * .0f, .5f + Mathf.Sin(a) * .32f, Mathf.Cos(a) * .32f), V(.2f, .2f, .2f), i % 2 == 0 ? K.Hex("C4352C") : K.Hex("3E7A3A"), 6, 4);
            }
        }

        /// <summary>The curved white stone-panel memorial wall behind the obelisk (radius r, on the far side from the
        /// road), about 1.8 m high, with its integral bench.</summary>
        static void MemorialWall(ComicMesh m, Vector3 centre, float r)
        {
            m.Surface = ComicSurface.Concrete;
            const int panels = 7;
            for (int i = 0; i < panels; i++)
            {
                float a = Mathf.Lerp(-62f, 62f, (i + .5f) / panels) * Mathf.Deg2Rad, w = r * 124f * Mathf.Deg2Rad / panels;
                Vector3 c = centre + V(-Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                Quaternion q = Quaternion.LookRotation(V(Mathf.Cos(a), 0, -Mathf.Sin(a)));
                m.Box(c + V(0, .9f, 0), V(w - .06f, 1.8f, .35f), StoneWhite, q);
                m.Box(c + V(0, 1.85f, 0), V(w, .1f, .45f), GraniteLight, q);
                m.Box(c + q * V(0, .25f, .45f), V(w - .1f, .5f, .55f), StoneWhite, q);
                m.Surface = ComicSurface.Plain;
                K.Face(m, c + q * V(-w * .32f, 1f, .18f), c + q * V(w * .32f, 1f, .18f), c + q * V(w * .32f, 1.5f, .18f), c + q * V(-w * .32f, 1.5f, .18f), GraniteDark, q * Vector3.forward, false);
                m.Surface = ComicSurface.Concrete;
            }
        }

        /// <summary>A white flagpole with the national flag stylised (blue field, a red-and-white canton, white stars).</summary>
        static void Flagpole(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, p, p + V(0, 9f, 0), .09f, .05f, White, 6, true);
            m.Box(p + V(0, .15f, 0), V(.6f, .3f, .6f), StoneWhite);
            Blob(m, p + V(0, 9.05f, 0), V(.2f, .2f, .2f), Brass, 6, 4);
            m.Surface = ComicSurface.Fabric;
            m.Box(p + V(0, 8.1f, 1.05f), V(.04f, 1.2f, 2f), K.Hex("1F3C88"));
            m.Surface = ComicSurface.Plain;
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 n = new Vector3(s, 0, 0);
                float x = s * .025f;
                Color wh = K.Hex("F2F1EC"), rd = K.Hex("C4352C");
                // the canton: white diagonals, a white cross and a narrower red cross over it
                K.Face(m, p + V(x, 8.1f, .08f), p + V(x, 8.2f, .08f), p + V(x, 8.68f, .95f), p + V(x, 8.58f, .95f), wh, n, false);
                K.Face(m, p + V(x, 8.68f, .08f), p + V(x, 8.58f, .08f), p + V(x, 8.1f, .95f), p + V(x, 8.2f, .95f), wh, n, false);
                K.Face(m, p + V(x * 1.1f, 8.32f, .08f), p + V(x * 1.1f, 8.32f, .95f), p + V(x * 1.1f, 8.46f, .95f), p + V(x * 1.1f, 8.46f, .08f), wh, n, false);
                K.Face(m, p + V(x * 1.1f, 8.1f, .44f), p + V(x * 1.1f, 8.1f, .59f), p + V(x * 1.1f, 8.68f, .59f), p + V(x * 1.1f, 8.68f, .44f), wh, n, false);
                K.Face(m, p + V(x * 1.2f, 8.36f, .08f), p + V(x * 1.2f, 8.36f, .95f), p + V(x * 1.2f, 8.42f, .95f), p + V(x * 1.2f, 8.42f, .08f), rd, n, false);
                K.Face(m, p + V(x * 1.2f, 8.1f, .485f), p + V(x * 1.2f, 8.1f, .545f), p + V(x * 1.2f, 8.68f, .545f), p + V(x * 1.2f, 8.68f, .485f), rd, n, false);
                foreach (Vector2 st in new[] { new Vector2(1.5f, 8.45f), new Vector2(1.75f, 8.2f), new Vector2(1.3f, 7.75f), new Vector2(1.6f, 7.65f), new Vector2(.5f, 7.75f) })
                    K.Face(m, p + V(x, st.y - .06f, st.x - .06f), p + V(x, st.y - .06f, st.x + .06f), p + V(x, st.y + .06f, st.x + .06f), p + V(x, st.y + .06f, st.x - .06f), K.Hex("F2F1EC"), n, false);
            }
        }

        // ---------------------------------------------------------------- Bassendean Oval

        /// <summary>The 1929 entrance gate: a cream rendered wall about 3 m high with capped pilasters, four openings with
        /// grey steel-bar gates, two ticket windows, a frieze with BASSENDEAN RECREATION RESERVE in raised letters and a
        /// central curved pediment with the year.</summary>
        static void OvalGate(ComicMesh f, CqLot l)
        {
            float W = l.W, h = 3.1f, th = .5f;
            Color trim = Color.Lerp(GateCream, White, .5f), shade = Color.Lerp(GateCream, K.R.ink, .2f);
            f.Surface = ComicSurface.Plaster;
            // wall between the openings: 5 piers and the band above
            float open = 1.5f, pier = (W - 4f * open) / 5f;
            for (int i = 0; i < 5; i++)
            {
                float a0 = i * (pier + open), a1 = a0 + pier;
                K.LBox(f, l, a0, a1, -1f, h - .9f, 0f, -th, GateCream);
                K.LBox(f, l, a0 + pier * .5f - .32f, a0 + pier * .5f + .32f, 0f, h + .35f, .12f, -th - .12f, GateCream);
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, a0 + pier * .5f - .42f, a0 + pier * .5f + .42f, h + .35f, h + .5f, .2f, -th - .2f, trim);
                K.LBox(f, l, a0 - .02f, a1 + .02f, 0f, .4f, .06f, -th - .06f, shade);
                f.Surface = ComicSurface.Plaster;
                if (i == 1 || i == 3)
                {
                    // a ticket window
                    float c = a0 + pier * .5f;
                    f.Surface = ComicSurface.PaintedMetal;
                    K.LQuad(f, l, c - .95f, c - .45f, 1.1f, 1.75f, .02f, K.R.ink);
                    K.LQuad(f, l, c + .45f, c + .95f, 1.1f, 1.75f, .02f, K.R.ink);
                    f.Surface = ComicSurface.Plaster;
                }
            }
            K.LBox(f, l, 0f, W, h - .9f, h, 0f, -th, GateCream);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.08f, W + .08f, h, h + .16f, .14f, -th - .14f, trim);
            K.LBox(f, l, -.04f, W + .04f, h - .95f, h - .86f, .08f, -th - .08f, trim);
            // the name on a raised frieze in front of the pilasters
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, .5f, W - .5f, h - .8f, h - .1f, .22f, 0f, Color.Lerp(GateCream, White, .35f));
            TextOnLot(f, l, "BASSENDEAN RECREATION RESERVE", W * .5f, h - .45f, .235f, .058f, K.Hex("5B4A2E"));
            LArch(f, l, W * .5f - 2.1f, W * .5f + 2.1f, h + .16f, 1.15f, .06f, -th - .06f, GateCream);
            TextOnLot(f, l, "1929", W * .5f, h + .6f, .08f, .07f, K.Hex("6B5A3A"));
            // gates: grey steel bars in each opening
            f.Surface = ComicSurface.Metal;
            for (int i = 0; i < 4; i++)
            {
                float a0 = pier + i * (pier + open), a1 = a0 + open;
                K.LBox(f, l, a0, a1, 2.05f, 2.12f, -.2f, -.26f, K.Hex("7F868C"));
                K.LBox(f, l, a0, a1, .2f, .27f, -.2f, -.26f, K.Hex("7F868C"));
                for (float a = a0 + .15f; a < a1 - .05f; a += .25f) K.LQuad(f, l, a - .025f, a + .025f, .1f, 2.2f, -.22f, K.Hex("8F969C"), false);
            }
        }

        /// <summary>A white picket fence along z at x (flat road space): rails and pickets.</summary>
        static void PicketFence(ComicMesh m, float x, float z0, float z1, float y)
        {
            m.Surface = ComicSurface.Wood;
            for (float z = z0; z < z1 - .01f; z += 8f)
            {
                float e = Mathf.Min(z1, z + 8f);
                m.Box(V(x, y + .75f, (z + e) * .5f), V(.06f, .07f, e - z), White);
                m.Box(V(x, y + .3f, (z + e) * .5f), V(.06f, .07f, e - z), White);
            }
            for (float z = z0 + .15f; z < z1; z += .34f)
                K.Face(m, V(x - .04f, y + .05f, z - .07f), V(x - .04f, y + .05f, z + .07f), V(x - .04f, y + 1f, z + .07f), V(x - .04f, y + 1f, z - .07f), White, Vector3.left, false);
            for (float z = z0; z <= z1 + .01f; z += 4f) m.Box(V(x, y + .55f, Mathf.Min(z, z1 - .06f)), V(.12f, 1.1f, .12f), White);
        }

        /// <summary>Australian-rules goal posts: two tall goal posts and two shorter behind posts in a row along z, white,
        /// with black-and-white padding round their feet. p = the middle of the goal line.</summary>
        static void GoalPosts(ComicMesh m, Vector3 p, float spacing, bool detail)
        {
            for (int i = 0; i < 4; i++)
            {
                bool goal = i == 1 || i == 2;
                Vector3 b = p + V(0, 0, (i - 1.5f) * spacing);
                float h = goal ? 12.5f : 8f;
                m.Surface = ComicSurface.PaintedMetal;
                Rod(m, b, b + V(0, h, 0), .11f, .07f, White, detail ? 6 : 4, true);
                if (!detail) continue;
                m.Surface = ComicSurface.Rubber;
                for (int k = 0; k < 4; k++) Rod(m, b + V(0, .05f + k * .55f, 0), b + V(0, .6f + k * .55f, 0), .26f, .26f, k % 2 == 0 ? K.Hex("1E2126") : White, 8, k == 3);
            }
        }

        /// <summary>A heritage grandstand: cream weatherboard walls, a raked bank of timber benches open towards -Z (up the
        /// ground), a zinc roof oversailing the front on posts, louvre vents under the eaves, an outside stair. p = the
        /// front left corner (lowest x), width along x, depth along z. variant 1 = the wider McDonald stand.</summary>
        static void Grandstand(ComicMesh m, Vector3 p, float width, float depth, int variant)
        {
            float front = 1.7f, back = 5.6f, eaveF = 7.6f, eaveB = 8.9f;
            Vector3 P(float x, float y, float z) => p + V(x, y, z);
            // the front wall (white balustrade with a black band: the club's colours), the raked seating
            m.Surface = ComicSurface.Wood;
            K.MinMaxBox(m, P(0, -1f, 0), P(width, front, .3f), StandCream);
            m.Surface = ComicSurface.Plain;
            K.Face(m, P(0, front - .5f, -.01f), P(width, front - .5f, -.01f), P(width, front - .12f, -.01f), P(0, front - .12f, -.01f), K.Hex("1E2126"), Vector3.back, false);
            for (float x = 1.2f; x < width - .5f; x += 2.4f) K.Face(m, P(x, front - .5f, -.02f), P(x + 1.2f, front - .5f, -.02f), P(x + 1.2f, front - .12f, -.02f), P(x, front - .12f, -.02f), White, Vector3.back, false);
            const int tiers = 6;
            for (int i = 0; i < tiers; i++)
            {
                float z0 = .3f + (depth - 1.2f) * i / tiers, z1 = .3f + (depth - 1.2f) * (i + 1) / tiers, y = Mathf.Lerp(front - .5f, back, (i + 1f) / tiers);
                m.Surface = ComicSurface.Wood;
                K.MinMaxBox(m, P(.3f, -1f, z0), P(width - .3f, y, z1), i % 2 == 0 ? K.Hex("8E7A5C") : K.Hex("A08A68"));
            }
            // back and side walls in cream weatherboard, stepped side profile
            K.MinMaxBox(m, P(0, -1f, depth - .9f), P(width, eaveB - .3f, depth), StandCream);
            foreach (float x in new[] { 0f, width - .3f })
            {
                Extrude(m, new[] { P(x, -1f, .3f), P(x, front + .6f, .3f), P(x, back + 1.3f, depth - .9f), P(x, -1f, depth - .9f) }, V(.3f, 0, 0), StandCream);
                m.Surface = ComicSurface.Plain;
                float fx = x < 1f ? x - .01f : x + .31f;
                Vector3 n = x < 1f ? Vector3.left : Vector3.right;
                for (float y = .3f; y < front + .5f; y += .32f) K.Face(m, V(p.x + fx, p.y + y, p.z + .35f), V(p.x + fx, p.y + y, p.z + depth - .1f), V(p.x + fx, p.y + y + .03f, p.z + depth - .1f), V(p.x + fx, p.y + y + .03f, p.z + .35f), WeatherboardShade, n, false);
                m.Surface = ComicSurface.Wood;
            }
            // roof: a skillion rising to the back, oversailing the front; ink fascia; posts; louvres under the back eave
            m.Surface = ComicSurface.Metal;
            Vector3 a = P(-.5f, eaveF, -1.6f), b = P(width + .5f, eaveF, -1.6f), c = P(width + .5f, eaveB, depth + .4f), d = P(-.5f, eaveB, depth + .4f);
            K.Face(m, a, b, c, d, Zinc, Vector3.up);
            K.Face(m, a + V(0, -.16f, 0), b + V(0, -.16f, 0), c + V(0, -.16f, 0), d + V(0, -.16f, 0), ZincDark, Vector3.down);
            K.Face(m, a, b, b + V(0, -.3f, 0), a + V(0, -.3f, 0), White, Vector3.back);
            K.Face(m, a, d, d + V(0, -.3f, 0), a + V(0, -.3f, 0), White, Vector3.left);
            K.Face(m, b, c, c + V(0, -.3f, 0), b + V(0, -.3f, 0), White, Vector3.right);
            m.Surface = ComicSurface.PaintedMetal;
            int posts = variant == 1 ? 5 : 4;
            for (int i = 0; i < posts; i++)
            {
                float x = Mathf.Lerp(.2f, width - .2f, (float)i / (posts - 1));
                m.Box(P(x, (front + eaveF) * .5f, .15f), V(.14f, eaveF - front, .14f), White);
                m.Beam(P(x, eaveF - 1f, .15f), P(x, eaveF - .2f, -.9f), .08f, White);
            }
            m.Surface = ComicSurface.Plain;
            for (float y = back + 1.5f; y < eaveB - .45f; y += .22f)
                K.Face(m, P(.6f, y, depth - .91f), P(width - .6f, y, depth - .91f), P(width - .6f, y + .1f, depth - .91f), P(.6f, y + .1f, depth - .91f), K.Hex("6B6A5C"), Vector3.back, false);
            // outside steel stair up the side facing the road
            m.Surface = ComicSurface.Metal;
            Slab(m, P(-.75f, 0f, .2f), P(-.75f, front + .9f, 4.2f), Vector3.right, 1.1f, 1.1f, .14f, K.Hex("8F969C"), K.Hex("6B7178"));
            m.Beam(P(-1.3f, 1f, .2f), P(-1.3f, front + 1.9f, 4.2f), .06f, White);
            if (variant == 1)
            {
                // the cream bullnose awning along the back is not seen from the road: a flag on the roof instead
                m.Surface = ComicSurface.PaintedMetal;
                Rod(m, P(width * .5f, eaveB - .2f, depth), P(width * .5f, eaveB + 3.4f, depth), .06f, .04f, White, 5, true);
                m.Surface = ComicSurface.Fabric;
                m.Box(P(width * .5f + .8f, eaveB + 2.9f, depth), V(1.6f, .9f, .04f), K.Hex("1E2126"));
                m.Box(P(width * .5f + .8f, eaveB + 2.9f, depth - .03f), V(1.6f, .3f, .02f), White);
            }
        }

        /// <summary>A galvanised light tower: a tapering mast with a rack of floodlights.</summary>
        static void LightTower(ComicMesh m, Vector3 p, float h)
        {
            m.Surface = ComicSurface.Metal;
            Rod(m, p, p + V(0, h, 0), .5f, .22f, Galv, 6);
            m.Box(p + V(0, .4f, 0), V(1.4f, .8f, 1.4f), K.Hex("C9C5BA"));
            m.Box(p + V(0, h + 1.2f, 0), V(.2f, 2.8f, 4.6f), GalvDark);
            m.Surface = ComicSurface.Glass;
            for (int r = 0; r < 3; r++) for (int c = 0; c < 4; c++) m.Box(p + V(-.2f, h + .3f + r * .9f, -1.65f + c * 1.1f), V(.3f, .6f, .8f), K.Hex("F4F0DC"));
        }

        /// <summary>The old scoreboard on the far side of the ground: a black board on a cream hut, HOME and VISITORS.</summary>
        static void Scoreboard(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Wood;
            m.Box(p + V(0, 2f, 0), V(3f, 6f, 9f), StandCream);
            m.Box(p + V(-1.56f, 4f, 0), V(.1f, 3.4f, 8.4f), K.Hex("1E2126"));
            TextFacingX(m, "HOME 12.8", p + V(-1.63f, 4.9f, 0), -1, .16f, White);
            TextFacingX(m, "VISITORS 9.11", p + V(-1.63f, 3.5f, 0), -1, .13f, White);
            m.Surface = ComicSurface.Metal;
            GableRoofZ(m, p + V(0, 5f, 0), 3.6f, 9.6f, 1f, RedIron, StandCream);
        }

        /// <summary>A few galahs feeding on the outfield (scenery, beyond the fence).</summary>
        static void Galahs(ComicMesh m, CqChunkDraft d, float x, float z)
        {
            var one = GalahMesh();
            var r = new System.Random(77);
            for (int i = 0; i < 5; i++)
            {
                float px = x + F(r, 0f, 5f), pz = z + F(r, -4f, 4f);
                m.Add(one, V(px, d.Ground(pz) + .04f, pz), Quaternion.Euler(0, F(r, 0f, 360f), 0), Vector3.one * 1.25f);
            }
        }

        // ---------------------------------------------------------------- Bassendean buildings

        /// <summary>A two-storey rendered shop row (Padbury's Buildings): ground-floor shopfronts under a boxed awning on
        /// tie rods, tall upper windows between capped pilasters, a moulded parapet with a curved pediment and its year.</summary>
        static void TwoStoreyShop(ComicMesh f, CqLot l, Color wall, Color awning, string year)
        {
            float W = l.W, D = l.Depth, gh = 4.2f, h = 8.2f;
            Color trim = White;
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0, W, 0f, h, 0f, -D, wall);
            int bays = Mathf.Max(2, Mathf.RoundToInt(W / 5.2f));
            for (int i = 0; i <= bays; i++)
            {
                float a = Mathf.Clamp(W * i / bays, .3f, W - .3f);
                K.LBox(f, l, a - .3f, a + .3f, 0f, h, .16f, 0f, Color.Lerp(wall, K.R.ink, .08f));
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, a - .36f, a + .36f, 0f, .5f, .22f, 0f, Color.Lerp(wall, K.R.ink, .35f));
                f.Surface = ComicSurface.Plaster;
            }
            for (int i = 0; i < bays; i++)
            {
                float a0 = W * i / bays + .6f, a1 = W * (i + 1) / bays - .6f, ac = (a0 + a1) * .5f;
                Shopfront(f, l, a0, a1, .05f, 3.1f, .03f, null, true);
                FramedWindow(f, l, ac - .85f, 1.05f, gh + 1.1f, gh + 3.2f, .02f, trim, true, true, true);
                FramedWindow(f, l, ac + .85f, 1.05f, gh + 1.1f, gh + 3.2f, .02f, trim, true, true, true);
                SignPanel(f, l, a0 + .4f, a1 - .4f, 3.27f, 3.67f, 2.36f, SignColours[(i * 2 + 1) % SignColours.Length]);
            }
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.06f, W + .06f, gh + .25f, gh + .5f, .2f, -D - .06f, trim);
            BoxAwning(f, l, -.02f, W + .02f, 3.2f, 2.35f, awning, true, 1.4f);
            Parapet(f, l, 0f, W, h, 1.2f, wall, trim, 2);
            TextOnLot(f, l, year, W * .5f, h + 1.75f, .13f, .075f, Color.Lerp(wall, K.R.ink, .6f));
            EndWindow(f, l, 0f, -1.5f, -3f, gh + 1.1f, gh + 3.2f);
            SkillionRoof(f, l, .2f, W - .2f, h + .3f, D, GreyIron);
        }

        /// <summary>The library: single storey in banded red and cream brick with tall teal glazing, a deep charcoal fascia
        /// and an entrance canopy; LIBRARY on a teal panel. Set back behind a small forecourt with a shrub bed.</summary>
        static void Library(ComicMesh f, CqLot l)
        {
            float W = l.W, D = l.Depth, h = 4.4f;
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            f.Surface = ComicSurface.Brick;
            K.LBox(f, l, 0, W, 0f, h, 0f, -D, RedBrick);
            f.Surface = ComicSurface.Plain;
            foreach (float y in new[] { .55f, 1.5f, 2.45f, 3.4f })
            {
                K.LQuad(f, l, 0f, W, y, y + .32f, .012f, CreamBrick, false);
                K.Face(f, l.P(-.012f, y, 0f), l.P(-.012f, y, -D), l.P(-.012f, y + .32f, -D), l.P(-.012f, y + .32f, 0f), CreamBrick, Vector3.back, false);
            }
            // tall glazing bays in teal between brick piers
            int bays = 4;
            for (int i = 0; i < bays; i++)
            {
                float a0 = 1.2f + (W - 2.4f) * i / bays + .5f, a1 = 1.2f + (W - 2.4f) * (i + 1) / bays - .5f;
                f.Surface = ComicSurface.PaintedMetal;
                K.LBox(f, l, a0 - .08f, a1 + .08f, .1f, 3.7f, .14f, 0f, K.R.ink);
                f.Surface = ComicSurface.Glass;
                K.LQuad(f, l, a0, a1, .5f, 3.6f, .15f, TealGlass);
                f.Surface = ComicSurface.PaintedMetal;
                K.LBox(f, l, (a0 + a1) * .5f - .04f, (a0 + a1) * .5f + .04f, .5f, 3.6f, .19f, .14f, K.R.ink);
                K.LBox(f, l, a0, a1, 2.5f, 2.58f, .19f, .14f, K.R.ink);
            }
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, -.3f, W + .3f, h, h + .7f, .5f, -D - .3f, Charcoal);
            K.LBox(f, l, -.34f, W + .34f, h - .08f, h + .02f, .54f, -D - .34f, K.R.ink);
            // entrance canopy on two posts and the name panel
            K.LBox(f, l, W * .5f - 2.6f, W * .5f + 2.6f, 3.1f, 3.35f, 2.2f, 0f, Charcoal);
            foreach (float a in new[] { W * .5f - 2.4f, W * .5f + 2.4f }) K.LBox(f, l, a - .07f, a + .07f, 0f, 3.1f, 2.1f, 1.96f, Charcoal);
            K.LBox(f, l, W * .5f - 2f, W * .5f + 2f, h + .08f, h + .62f, .56f, .5f, TealGlass);
            TextOnLot(f, l, "LIBRARY", W * .5f, h + .35f, .57f, .075f, White);
            f.Surface = ComicSurface.Metal;
            K.LBox(f, l, W * .6f, W * .8f, h + .7f, h + 1.6f, -3f, -6f, PlantGrey);
            // shrub bed and bike hoops on the forecourt
            float fo = l.Line - K.Edge;
            if (fo > 1.5f)
            {
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, .5f, W * .5f - 3f, 0f, .35f, fo - .3f, .6f, K.Hex("C9C5BA"));
                Bush(f, l.P(1.6f, .3f, fo * .5f + .2f), .9f);
                Bush(f, l.P(3.4f, .3f, fo * .5f + .1f), 1f, K.Hex("6E9A4E"), K.Hex("C85A8A"));
                f.Surface = ComicSurface.PaintedMetal;
                for (float a = W * .5f + 3.4f; a < W - 1f; a += .9f)
                {
                    K.LBox(f, l, a - .04f, a + .04f, 0f, .85f, fo - .4f, fo - .48f, K.R.ink);
                    K.LBox(f, l, a - .04f, a + .04f, 0f, .85f, fo - 1.1f, fo - 1.18f, K.R.ink);
                    K.LBox(f, l, a - .04f, a + .04f, .8f, .88f, fo - .4f, fo - 1.18f, K.R.ink);
                }
            }
        }

        // ---------------------------------------------------------------- Bassendean obstacles and animals

        // A station luggage barrow from the museum: a timber deck on two iron wheels with a trunk, a case and a milk churn.
        static ComicMesh LuggageTrolleyMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            m.Box(V(0, .48f, 0), V(.95f, .08f, 1.5f), Timber);
            m.Box(V(0, .78f, .72f), V(.95f, .6f, .07f), Timber);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.5f, .5f })
            {
                m.Cylinder(V(x, .32f, .15f), .32f, .07f, K.R.ink, 10, 1f, Quaternion.Euler(0, 0, 90));
                m.Cylinder(V(x * 1.06f, .32f, .15f), .1f, .05f, MuseumRed, 6, 1f, Quaternion.Euler(0, 0, 90));
                m.Beam(V(x * .8f, .5f, -.7f), V(x * .8f, .95f, -1.05f), .05f, K.R.ink);
            }
            m.Box(V(0, .97f, -1.05f), V(.9f, .05f, .05f), K.R.ink);
            m.Box(V(0, .22f, -.6f), V(.06f, .44f, .06f), K.R.ink);
            m.Surface = ComicSurface.Fabric;
            m.Box(V(-.12f, .72f, .25f), V(.6f, .4f, .8f), K.Hex("7A4A32"));
            m.Box(V(-.12f, .72f, .25f), V(.62f, .06f, .82f), Brass);
            m.Box(V(.05f, .64f, -.4f), V(.7f, .24f, .42f), K.Hex("2F6B4A"), Quaternion.Euler(0, 12f, 0));
            m.Surface = ComicSurface.Metal;
            Rod(m, V(.28f, .52f, .34f), V(.28f, .95f, .34f), .15f, .15f, K.Hex("C9CED3"), 8);
            Rod(m, V(.28f, .95f, .34f), V(.28f, 1.05f, .34f), .15f, .09f, K.Hex("AAB1B8"), 8, true);
            return m;
        }

        // A cafe's chalkboard A-frame: two leaning boards in a white timber frame, chalk lines and a cup.
        static ComicMesh AFrameMesh()
        {
            var m = new ComicMesh();
            foreach (int s in new[] { -1, 1 })
            {
                Quaternion q = Quaternion.Euler(s * 14f, 0, 0);
                Vector3 c = V(0, .52f, s * .14f);
                m.Surface = ComicSurface.Wood;
                m.Box(c, V(.8f, 1.02f, .05f), White, q);
                m.Surface = ComicSurface.Plain;
                Vector3 n = q * new Vector3(0, 0, s), face = c + n * .03f;
                K.Face(m, face + q * V(-.33f, -.42f, 0), face + q * V(.33f, -.42f, 0), face + q * V(.33f, .42f, 0), face + q * V(-.33f, .42f, 0), K.Hex("2B3138"), n, false);
                for (int i = 0; i < 3; i++)
                    K.Face(m, face + n * .005f + q * V(-.24f, .26f - i * .14f, 0), face + n * .005f + q * V(.24f - i * .06f, .26f - i * .14f, 0), face + n * .005f + q * V(.24f - i * .06f, .3f - i * .14f, 0), face + n * .005f + q * V(-.24f, .3f - i * .14f, 0), K.Hex("E9E6DC"), n, false);
                K.Face(m, face + n * .005f + q * V(-.1f, -.3f, 0), face + n * .005f + q * V(.1f, -.3f, 0), face + n * .005f + q * V(.13f, -.12f, 0), face + n * .005f + q * V(-.13f, -.12f, 0), K.Hex("E58A2E"), n, false);
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, 1.03f, 0), V(.84f, .06f, .14f), K.R.ink);
            return m;
        }

        // Half a wine barrel as a planter (the Swan Valley starts just up the road): staves, ink hoops, red and yellow flowers.
        static ComicMesh BarrelPlanterMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            Rod(m, V(0, 0, 0), V(0, .34f, 0), .36f, .45f, K.Hex("8E5A3A"), 10);
            Rod(m, V(0, .34f, 0), V(0, .66f, 0), .45f, .42f, K.Hex("A06A44"), 10, true);
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, V(0, .12f, 0), V(0, .2f, 0), .405f, .425f, K.R.ink, 10);
            Rod(m, V(0, .48f, 0), V(0, .56f, 0), .45f, .44f, K.R.ink, 10);
            m.Surface = ComicSurface.Foliage;
            m.Cylinder(V(0, .67f, 0), .4f, .04f, Mulch, 10);
            Blob(m, V(0, .82f, 0), V(.8f, .4f, .8f), K.Hex("4F8A40"), 6, 3);
            Color[] petals = { K.Hex("E2463A"), K.Hex("F6C543"), K.Hex("E85A8C"), K.Hex("F5F3EC") };
            for (int i = 0; i < 7; i++)
            {
                float a = i * .9f;
                Blob(m, V(Mathf.Cos(a) * .24f, .95f + (i % 3) * .05f, Mathf.Sin(a) * .24f), V(.2f, .16f, .2f), petals[i % petals.Length], 6, 4);
            }
            return m;
        }

        // A football tackle bag standing on the footpath: a tall black cylinder with white bands and handles.
        static ComicMesh TackleBagMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color black = K.Hex("1E2126");
            Rod(m, V(0, 0, 0), V(0, 1.5f, 0), .26f, .26f, black, 10);
            Rod(m, V(0, 1.5f, 0), V(0, 1.58f, 0), .26f, .2f, black, 10, true);
            Rod(m, V(0, .35f, 0), V(0, .6f, 0), .265f, .265f, White, 10);
            Rod(m, V(0, 1f, 0), V(0, 1.25f, 0), .265f, .265f, White, 10);
            m.Surface = ComicSurface.Rubber;
            foreach (int s in new[] { -1, 1 }) m.Box(V(s * .28f, .8f, 0), V(.05f, .3f, .08f), K.Hex("C4352C"));
            m.Box(V(0, .03f, 0), V(.6f, .06f, .6f), K.R.ink);
            // a yellow football leaning against it
            m.Surface = ComicSurface.Plastic;
            m.Ellipsoid(V(.32f, .13f, -.2f), V(.2f, .2f, .32f), K.Hex("F2C230"));
            return m;
        }

        // A galah: grey back and wings, a rose-pink face and breast, a pale pink crest, a short bone beak.
        static ComicMesh GalahMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color pink = K.Hex("E0708F"), grey = K.Hex("9AA3AA"), crest = K.Hex("F6CFD8");
            m.Ellipsoid(V(0, .22f, .03f), V(.2f, .22f, .32f), pink);
            m.Box(V(0, .26f, .08f), V(.21f, .13f, .26f), grey, Quaternion.Euler(-12f, 0, 0));
            m.Box(V(0, .2f, .26f), V(.12f, .05f, .16f), Color.Lerp(grey, K.R.ink, .3f), Quaternion.Euler(-10f, 0, 0));
            m.Box(V(0, .37f, -.1f), V(.14f, .14f, .14f), pink);
            m.Box(V(0, .46f, -.08f), V(.12f, .05f, .12f), crest);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .36f, -.19f), V(.05f, .06f, .05f), K.Hex("E6DFCB"));
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(V(s * .062f, .4f, -.15f), V(.025f, .025f, .025f), K.R.ink);
                m.Box(V(s * .04f, .05f, 0), V(.03f, .1f, .03f), K.Hex("6B7178"));
                m.Box(V(s * .04f, .012f, -.02f), V(.05f, .025f, .09f), K.Hex("6B7178"));
            }
            return m;
        }

        // ---------------------------------------------------------------- Bassendean far scenery

        static ComicMesh BasFar(float grade) => EastFar(grade, 290, 80f, 150f, 250f, K.Hex("CCDCE8"), K.Hex("78A650"), false, (m, r) =>
        {
            Color haze = K.Hex("CCDCE8");
            // the oval's light towers stand over the roofs to the south, jacarandas in bloom through the suburb
            foreach (Vector2 t in new[] { new Vector2(74f, 250f), new Vector2(118f, 262f), new Vector2(80f, 330f), new Vector2(124f, 340f) })
            {
                Rod(m, V(t.x, 0, t.y), V(t.x, 36f, t.y), .7f, .3f, Haze(Galv, 120f, haze), 5);
                m.Box(V(t.x, 37.5f, t.y), V(.4f, 3f, 5f), Haze(GalvDark, 120f, haze));
            }
            for (int i = 0; i < 40; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * F(r, 64f, 150f), z = F(r, -200f, 800f);
                if (x > 0 && x > 140f) continue;
                Rod(m, V(x, 0, z), V(x, 4.5f, z), .4f, .3f, Haze(JacBark, Mathf.Abs(x), haze), 4);
                Blob(m, V(x, 6.5f, z), V(F(r, 8f, 11f), F(r, 4.5f, 6f), F(r, 8f, 11f)), Haze(i % 3 == 0 ? Jac2 : Jac1, Mathf.Abs(x), haze), 6, 4, F(r, 0f, 3f));
            }
            // a water tower on the rise to the north
            Rod(m, V(-230f, 0f, 380f), V(-230f, 18f, 380f), 2.2f, 2.2f, Haze(K.Hex("D3D7DA"), 260f, haze), 6);
            m.Cylinder(V(-230f, 23f, 380f), 8f, 9f, Haze(K.Hex("EDE9DD"), 260f, haze), 8, .8f);
        });
    }
}
