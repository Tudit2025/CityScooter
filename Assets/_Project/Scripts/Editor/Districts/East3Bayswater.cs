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
    // BAYSWATER (order 270): Whatley Crescent beside the new elevated station. Railway and viaduct on the left (-X),
    // the old town-centre shopfronts on the right (+X).
    public static partial class East3Builder
    {
        // The viaduct in flat road space, on the left: near edge, far edge, soffit, rail deck, platform, canopy underside.
        const float VNear = 13f, VFar = 39f, VSoffit = 5.5f, VDeck = 6.7f, VPlat = 7.8f, VCanopy = 11.4f;
        static readonly float[] VTracks = { -15.2f, -24.8f, -27.2f, -36.8f };
        const float VPlatA = -20f, VPlatB = -32f;
        /// <summary>The plaza kicker (Ramp) in the Plaza chunk: numbers for gameplay.</summary>
        public const float PlazaRampZ = 14f, PlazaRampRise = 5f, PlazaRampHeight = 1.2f, PlazaRampTop = 2f, PlazaRampWidth = 3.2f;

        static readonly Color FluteCream = K.Hex("F7EBC4"), FluteShade = K.Hex("D6C28C"), PierCream = K.Hex("F0E2BA"), ConcreteGrey = K.Hex("BDBFBC"), SoffitGrey = K.Hex("A4A7A5");
        static readonly Color Copper = K.Hex("C97B3C"), CopperDark = K.Hex("9C5A2A"), RoyalBlue = K.Hex("2456B4"), TileWhite = K.Hex("F2F1EA");
        static readonly Color MuralBlue = K.Hex("2F7FC0"), MuralLight = K.Hex("7FC3E6"), MuralDeep = K.Hex("1F5C96"), NumbatOrange = K.Hex("D9812E"), NumbatOchre = K.Hex("E2A64A");
        static readonly Color OldBridgeCream = K.Hex("E8DDB8"), OldBridgeBlue = K.Hex("9CC3E0");
        static string BayPf(string name) => BayPrefabs + "/" + name + ".prefab";

        static void BuildBayswater(TrackTheme theme, bool atlas)
        {
            K.Begin(BayMeshes, BayPrefabs, CbdBuilder.DataFolder);
            Obstacle bollard = K.SaveObstacle(BayPf("OBS_Bayswater_Bollard"), "OBS_Bayswater_Bollard", Make(m => Bollard(m, Vector3.zero)), new Vector3(.4f, 1.05f, .4f), ObstacleKind.Thing, true, false, .8f);
            Obstacle barrier = K.SaveObstacle(BayPf("OBS_Bayswater_WaterBarrier"), "OBS_Bayswater_WaterBarrier", WaterBarrierMesh(), new Vector3(2f, .95f, .55f), ObstacleKind.Thing, true, true, 1f);
            Obstacle suitcase = K.SaveObstacle(BayPf("OBS_Bayswater_Suitcase"), "OBS_Bayswater_Suitcase", SuitcaseMesh(), new Vector3(.7f, .95f, .5f), ObstacleKind.Thing, true, true, 1f);
            Obstacle fence = K.SaveObstacle(BayPf("OBS_Bayswater_TempFence"), "OBS_Bayswater_TempFence", TempFenceMesh(), new Vector3(2.4f, 2f, .5f), ObstacleKind.Thing, false, false, .7f);
            Obstacle heightBar = K.SaveObstacle(BayPf("OBS_Bayswater_Planter"), "OBS_Bayswater_Planter", PlanterMesh(), new Vector3(1.3f, 1.25f, 1.3f), ObstacleKind.Thing, false, false, .7f);
            GameObject magpie = SaveAnimal(BayPf("ANM_Bayswater_Magpie"), "ANM_Bayswater_Magpie", MagpieMesh(), new Vector3(0, .28f, 0), new Vector3(.4f, .56f, .7f), .5f, true);
            Ramp works = K.SaveRamp(BayPf("RMP_Bayswater_WorksRamp"), "RMP_Bayswater_WorksRamp", 3f, 5f, 1.25f, 1.5f, 1f, .5f, K.Hex("D9B35A"), K.Hex("8F6A3C"), true);

            Write(BayEntry(), BayChunkPath("Entry"));
            Write(BayShops(), BayChunkPath("Shops"));
            Write(BayViaduct(), BayChunkPath("Viaduct"));
            Write(BayStation(), BayChunkPath("Station"));
            Write(BayPlaza(), BayChunkPath("Plaza"));
            Write(BayJunction(), BayChunkPath("Junction"));
            Write(BayExit(), BayChunkPath("Exit"));
            GameObject far = K.SaveFarScenery(BayPf("FAR_Bayswater_SwanPlain"), "FAR_Bayswater_SwanPlain", BayFar(-BayDrop / 40f));

            District d = OpenDistrict(BayDistrictPath, BayId, "BAYSWATER", 270);
            d.length = new Vector2(250f, 280f); // entry + the five chunks once + exit
            d.chunks = BayChunkIds.Select(c => Chunk(BayChunkPath(c))).ToArray();
            d.entry = Chunk(BayChunkPath("Entry"));
            d.exit = Chunk(BayChunkPath("Exit"));
            d.obstacles = new[] { bollard, barrier, suitcase, fence, heightBar };
            d.jumpObstacles = new[] { barrier, suitcase, bollard };
            d.hazards = ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_OncomingPair", "HZ_Crossing", "HZ_SidewalkStroll", "HZ_PoliceStop", "HZ_RoadworksShort");
            var people = new List<GameObject>(K.People(theme, "Office", "Shopper", "Shopper", "Tourist", "Tourist", "Student", "Elder", "Courier", "Barista", "Kid", "DogWalker"));
            people.Add(magpie); people.Add(magpie);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { works };
            d.rampFrequency = 1f;
            // A bright suburban morning: warm light on cream concrete, a soft blue-grey haze towards the scarp.
            d.fogColor = new Color(.69f, .80f, .93f);
            d.skyTint = new Color(1f, 1f, .99f);
            d.ambientSky = new Color(.70f, .83f, .97f);
            d.ambientEquator = new Color(.90f, .88f, .82f);
            d.ambientGround = new Color(.72f, .64f, .54f);
            d.sunColor = new Color(1f, .96f, .88f);
            d.sunIntensity = 1.45f;
            d.farScenery = far;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/Bayswater] " + string.Join("; ", K.Report));
            if (!atlas) return;
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("ViaductPier", null, Make(m => Pier(m, 0f, 0f, VSoffit, true))),
                ("StationCanopy", null, Make(m => { Canopy(m, 0f, 0f, 16f, 0f, true, true); m.Surface = ComicSurface.Concrete; m.Box(V(0, -.2f, 8f), V(6f, .4f, 17f), K.Hex("9FA4A8")); LiftPod(m, V(0, 0, 11f)); })),
                ("OldBridgeMemorial", null, Make(m => OldBridgeMemorial(m, Vector3.zero))),
                ("TrainB", null, Make(m => Train(m, Vector3.zero, 2, 1))),
                ("TrainA", null, Make(m => Train(m, Vector3.zero, 2, 0))),
                ("Gantry", null, Make(m => Gantry(m, -5f, 5f, 0f, 0f, -2.2f, 2.2f))),
                ("CornerShop", null, Make(m => CornerShop(m, new CqLot(-1, -7f, 7f, 0f, 10f, 0f)))),
                ("ParapetShops", null, Make(m => { ParapetShop(m, new CqLot(-1, -8f, 0f, 0f, 10f, 0f), CreamBrick, K.Hex("7A2E2A"), 2, 1); ParapetShop(m, new CqLot(-1, 0f, 8f, 0f, 10f, 0f), K.Hex("D8845A"), Navy, 3, 4); })),
                ("Hotel", null, Make(m => VerandahHotel(m, new CqLot(-1, -10f, 10f, 0f, 11f, 0f), false))),
                ("PostOffice", null, Make(m => PostOffice(m, new CqLot(-1, -6f, 6f, 0f, 9f, 0f)))),
                ("Bungalow", null, Make(m => Bungalow(m, new CqLot(-1, -6f, 6f, 0f, 10f, 0f), RedBrick, GreyIron, 1))),
                ("BusShelter", null, Make(m => BusShelter(m, Vector3.zero, 90f))),
                ("SwanNeckLamp", null, Make(m => SwanNeckLamp(m, null, Vector3.zero, SignBlue))),
                ("Gum", null, Make(m => Gum(m, Vector3.zero, 13f, 3))),
                ("StreetTree", null, Make(m => StreetTree(m, null, Vector3.zero, 1f, 2))),
                ("GrassTrees", null, Make(m => { GrassTree(m, Vector3.zero, 1f, 1); GrassTree(m, V(1.4f, 0, .6f), .8f, 2); })),
                ("TunnelPortal", null, Make(m => m.Add(Make(g => AirportPortal(g, 0f, 0f, 0f)), Vector3.zero, Quaternion.Euler(0, 180f, 0), Vector3.one))),
                ("Bollard", bollard.gameObject, null),
                ("WaterBarrier", barrier.gameObject, null),
                ("Suitcase", suitcase.gameObject, null),
                ("TempFence", fence.gameObject, null),
                ("Planter", heightBar.gameObject, null),
                ("Magpie", magpie, null),
                ("WorksRamp", works.gameObject, null),
            };
            K.RenderAtlas("art-src/districts/bayswater/props-atlas.png", items);
        }

        // ---------------------------------------------------------------- Bayswater chunks

        static void BayStreet(CqChunkDraft d) => Street(d.Road, d.Length, RedAsphalt, BuffPaver, KerbCap);

        static void SideGround(CqChunkDraft d, int s, float z0, float z1, float depth, Color c, ComicSurface surface = ComicSurface.Paving)
        {
            d.Road.Surface = surface;
            K.SideBox(d.Road, s, z0, z1, -2.4f, K.Kerb + .02f, 0f, depth, c);
        }

        /// <summary>A box split along z into pieces of at most seg metres (so the curl bends long decks and walls).</summary>
        static void Strip(ComicMesh m, Vector3 a, Vector3 b, Color c, float seg = 8f)
        {
            float z0 = Mathf.Min(a.z, b.z), z1 = Mathf.Max(a.z, b.z);
            for (float z = z0; z < z1 - .01f; z += seg)
                K.MinMaxBox(m, new Vector3(a.x, a.y, z), new Vector3(b.x, b.y, Mathf.Min(z1, z + seg)), c);
        }

        static CqChunkDraft BayEntry()
        {
            // Calm at z = 0 (hedges, lawn, gums; the railway at grade behind its fence), then the line starts to climb on
            // a limestone-walled embankment on the left and the first cottages appear on the right.
            var d = new CqChunkDraft("Bayswater_Entry", BayDrop);
            BayStreet(d);
            SideGround(d, -1, 0, 40, 5f, Lawn, ComicSurface.Foliage);
            SideGround(d, 1, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            // left: verge, hedge, then the tracks rising from grade to 3 m
            Hedge(d.SlopedFrontage, -1, .4f, 14f, .5f, 1.6f, 1.2f);
            float H(float z) => Mathf.Lerp(.25f, 3f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(10f, 40f, z)));
            Embankment(d, 0f, 40f, H, 1, false);
            for (float z = 5f; z < 40f; z += 20f) Sloped(d, d.Deep, t => Gantry(t, -VNear - .6f, -VNear - 12.4f, H(z) - .1f, z, -15.2f - 1.2f, -21.8f));
            Sloped(d, d.Deep, t => Catenary2(t, 0f, 40f, H, -16.4f, -21.8f));
            Gum(d.Props, V(-(K.Edge + 2.6f), d.Ground(4f), 4f), 11f, 271);
            Gum(d.Props, V(-(K.Edge + 3.2f), d.Ground(21f), 21f), 9f, 272);
            DeepSuburb(d, -1, 0f, 40f, 2701, 44f);
            WallBox(d, -(K.Edge + 2f), -(K.Edge + 40f), 0f, 40f, 8f);
            // right: hedge and trees, then two cottages behind limestone walls and a jacaranda in bloom
            Hedge(d.SlopedFrontage, 1, .4f, 13f, .5f, 1.6f, 1.2f);
            Gum(d.Props, V(K.Edge + 4.5f, d.Ground(6f), 6f), 12f, 273);
            Jacaranda(d.Props, V(K.Edge + 4f, d.Ground(16f), 16f), 1f, 274);
            Bungalow(d.SlopedFrontage, new CqLot(1, 18f, 29f, K.Edge + 6f, 10f, K.Kerb), RedBrick, GreyIron, 1);
            Bungalow(d.SlopedFrontage, new CqLot(1, 30f, 40f, K.Edge + 5f, 10f, K.Kerb), K.Hex("E9E2CF"), RedIron, 0);
            WallBox(d, K.Edge + 1f, K.Edge + 40f, 0f, 40f, 8f);
            DeepSuburb(d, 1, 0f, 40f, 2702, 26f);
            SwanNeckLamp(d.Props, d.Collisions, V(K.Half + .7f, d.Ground(30f), 30f), SignBlue);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft BayShops()
        {
            // Whatley Crescent: the embankment climbs to viaduct height behind a cream panel wall with the blue "river"
            // mural; the town centre starts on the right with the white two-storey corner building and old shops.
            var d = new CqChunkDraft("Bayswater_Shops", BayDrop);
            BayStreet(d);
            SideGround(d, -1, 0, 40, 5f, BuffPaver);
            float H(float z) => Mathf.Lerp(3f, VDeck, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 26f, z)));
            Embankment(d, 0f, 30f, H, 2, true);
            Viaduct(d, 30f, 40f, false, false, VFar, 33f);
            Sloped(d, d.Deep, t => { Gantry(t, -VNear - .6f, -VFar + .6f, H(8f) - .1f, 8f, -16.4f, -21.8f); Gantry(t, -VNear - .6f, -VFar + .6f, VDeck, 28f, VTracks); });
            Sloped(d, d.Deep, t => Catenary2(t, 0f, 40f, H, -16.4f, -21.8f));
            PlazaPlanting(d, -1, 2f, 38f, 31);
            DeepSuburb(d, -1, 0f, 40f, 2711, 46f);
            WallBox(d, -(K.Edge + 4.6f), -(K.Edge + 40f), 0f, 40f, 9f);
            // right: the corner building and three shops
            SideGround(d, 1, 0, 40, 62f, BuffPaver);
            ComicMesh f = d.SlopedFrontage;
            CornerShop(f, new CqLot(1, 0f, 14f, K.Edge + .3f, 12f, K.Kerb));
            ParapetShop(f, new CqLot(1, 14f, 22.5f, K.Edge + .3f, 11f, K.Kerb), CreamBrick, K.Hex("7A2E2A"), 2, 1);
            ParapetShop(f, new CqLot(1, 22.5f, 31f, K.Edge + .3f, 11f, K.Kerb), K.Hex("D8845A"), Navy, 3, 4);
            ParapetShop(f, new CqLot(1, 31f, 40f, K.Edge + .3f, 11f, K.Kerb), K.Hex("E9E2CF"), K.Hex("2F6B4A"), 1, 2);
            WallBox(d, K.Edge + .3f, K.Edge + 13f, 0f, 40f, 9f);
            DeepSuburb(d, 1, 0f, 40f, 2712, 30f);
            StreetTree(d.Props, d.Collisions, V(K.Edge - 1f, d.Ground(8f), 8f), .9f, 5);
            StreetTree(d.Props, d.Collisions, V(K.Edge - 1f, d.Ground(27f), 27f), .95f, 6);
            SwanNeckLamp(d.Props, d.Collisions, V(K.Half + .7f, d.Ground(18f), 18f), SignBlue);
            SwanNeckLamp(d.Props, d.Collisions, V(-(K.Half + .7f), d.Ground(36f), 36f), SignBlue);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft BayViaduct()
        {
            // The viaduct: hammerhead piers, the fluted cream edge, the west ends of the platform canopies and a train
            // pulling in; bus stands under the deck. Right: the two-storey verandah hotel.
            var d = new CqChunkDraft("Bayswater_Viaduct", BayDrop);
            BayStreet(d);
            SideGround(d, -1, 0, 40, 62f, BuffPaver);
            Viaduct(d, 0f, 40f, true, true, VFar, 13f, 33f);
            UnderDeck(d, 0f, 40f);
            Canopy(d.SlopedFrontage, VPlatA, 10f, 40f, VPlat, true, false);
            Sloped(d, d.Deep, t => Canopy(t, VPlatB, 10f, 40f, VPlat, true, false));
            Sloped(d, d.Landmark, t => Train(t, V(VTracks[0], VDeck + .2f, 1f), 2, 1, 1, true, false));
            Sloped(d, d.Deep, t => { Gantry(t, -VNear - .3f, -VFar + .3f, VDeck, 3f, VTracks); Catenary(t, 0f, 40f, VDeck, VTracks); });
            PlazaPlanting(d, -1, 1f, 39f, 32);
            BusShelter(d.SlopedFrontage, V(-(K.Edge + 2.6f), K.Kerb, 22f), 90f);
            Sloped(d, d.Landmark, t => { Bus(t, V(-27.4f, K.Kerb + .1f, 22f), 0f); BusShelter(t, V(-22.6f, K.Kerb + .06f, 24f), 90f); });
            DeepSuburb(d, -1, 0f, 40f, 2721, 48f);
            WallBox(d, -(K.Edge + 4.6f), -(K.Edge + 40f), 0f, 40f, 9f);
            // right: the hotel, its white gabled wing and a shop
            SideGround(d, 1, 0, 40, 62f, BuffPaver);
            ComicMesh f = d.SlopedFrontage;
            VerandahHotel(f, new CqLot(1, 1f, 23f, K.Edge + .3f, 12f, K.Kerb), false);
            GabledWing(f, new CqLot(1, 23f, 31f, K.Edge + .3f, 11f, K.Kerb));
            ParapetShop(f, new CqLot(1, 31f, 40f, K.Edge + .3f, 11f, K.Kerb), K.Hex("3D434A"), K.Hex("C8463A"), 0, 0);
            WallBox(d, K.Edge + .3f, K.Edge + 13f, 0f, 40f, 9f);
            DeepSuburb(d, 1, 0f, 40f, 2722, 30f);
            SwanNeckLamp(d.Props, d.Collisions, V(K.Half + .7f, d.Ground(8f), 8f), SignBlue);
            SwanNeckLamp(d.Props, d.Collisions, V(K.Half + .7f, d.Ground(30f), 30f), SignBlue);
            StreetTree(d.Props, d.Collisions, V(K.Edge - 1f, d.Ground(36f), 36f), .9f, 8);
            d.Collisions.Add(new DressingObstacle { id = "Bollard", position = V(-(K.Half + .6f), d.Ground(6f), 6f), rotation = Vector3.zero, centre = V(0, .5f, 0), size = V(.35f, 1f, .35f), canJumpOver = true });
            Bollard(d.Props, V(-(K.Half + .6f), d.Ground(6f), 6f));
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft BayStation()
        {
            // The station: King William St runs under the new 4.8 m bridge (z 1..11); the concourse under the deck with
            // its glazed fronts, copper stripe and the BAYSWATER STATION band; platforms, canopies and blue lift pods
            // above; the memorial to the old 3.8 m bridge on the forecourt. Right: the old post office and shops.
            var d = new CqChunkDraft("Bayswater_Station", BayDrop);
            BayStreet(d);
            SideGround(d, -1, 0, 40, 62f, BuffPaver);
            Viaduct(d, 0f, 40f, true, true, VFar, 13f, 33f);
            UnderDeck(d, 13f, 40f);
            // King William St: red asphalt under the bridge, kerbs, a stop line, the clearance sign on the bridge edge
            ComicMesh g = d.Road;
            Ground(g, -62f, -K.Edge - .02f, 2.2f, 9.8f, K.Kerb + .07f, RedAsphalt, ComicSurface.Asphalt);
            Ground(g, -62f, -K.Edge - .02f, 1.9f, 2.2f, K.Kerb + .1f, KerbCap);
            Ground(g, -62f, -K.Edge - .02f, 9.8f, 10.1f, K.Kerb + .1f, KerbCap);
            Ground(g, -(K.Edge + .9f), -(K.Edge + .5f), 2.4f, 9.6f, K.Kerb + .1f, White);
            for (float x = -14f; x > -60f; x -= 6f) Ground(g, x - 2.6f, x, 5.92f, 6.08f, K.Kerb + .1f, White);
            ClearanceSign(d.SlopedFrontage, V(-VNear + .46f, VSoffit + .9f, 6f));
            foreach (float z in new[] { 1.4f, 10.6f })
            {
                // red-and-white hazard boards on the piers either side of the street, as on the old bridge
                for (int i = 0; i < 5; i++)
                    K.Face(d.SlopedFrontage, V(-VNear - .2f, .5f + i * .5f, z - .45f), V(-VNear - .2f, .5f + i * .5f, z + .45f), V(-VNear - .2f, 1f + i * .5f, z + .45f), V(-VNear - .2f, 1f + i * .5f, z - .45f), i % 2 == 0 ? HazardRed : White, Vector3.right, false);
                d.SlopedFrontage.Surface = ComicSurface.Concrete;
                K.MinMaxBox(d.SlopedFrontage, V(-VNear - 1.6f, -1f, z - .5f), V(-VNear - .22f, VSoffit, z + .5f), PierCream);
            }
            Concourse(d, 15f, 31f);
            Canopy(d.SlopedFrontage, VPlatA, 0f, 40f, VPlat, false, false);
            Sloped(d, d.Deep, t => Canopy(t, VPlatB, 0f, 40f, VPlat, false, false));
            LiftPod(d.SlopedFrontage, V(VPlatA, VPlat, 24f));
            Sloped(d, d.Landmark, t => Train(t, V(VTracks[0], VDeck + .2f, 0f), 1, 1, 1, false, true));
            Sloped(d, d.Deep, t => { Gantry(t, -VNear - .3f, -VFar + .3f, VDeck, 3f, VTracks); Catenary(t, 0f, 40f, VDeck, VTracks); });
            OldBridgeMemorial(d.SlopedFrontage, V(-(K.Edge + 2.3f), K.Kerb, 34.5f));
            PlazaPlanting(d, -1, 12f, 30f, 33);
            DeepSuburb(d, -1, 0f, 40f, 2731, 48f);
            d.Walls.Add(new Bounds(new Vector3(-(K.Edge + 22f), d.Y(26f) + 3f, 26f), new Vector3(34f, 10f, 28f)));
            d.Walls.Add(new Bounds(new Vector3(-(K.Edge + 2.3f), d.Y(34.5f) + 1.5f, 34.5f), new Vector3(1.6f, 4f, 7.5f)));
            // right: the old post office on its limestone terrace, two shops
            SideGround(d, 1, 0, 40, 62f, BuffPaver);
            ComicMesh f = d.SlopedFrontage;
            ParapetShop(f, new CqLot(1, 0f, 9f, K.Edge + .3f, 11f, K.Kerb), K.Hex("C9D6CF"), K.Hex("8A3B34"), 2, 5);
            PostOffice(f, new CqLot(1, 10.5f, 25.5f, K.Edge + 3.2f, 10f, K.Kerb));
            ParapetShop(f, new CqLot(1, 27f, 40f, K.Edge + .3f, 11f, K.Kerb), CreamBrick, K.Hex("2F4F6B"), 4, 3, 2);
            WallBox(d, K.Edge + .3f, K.Edge + 13f, 0f, 40f, 9f);
            DeepSuburb(d, 1, 0f, 40f, 2732, 30f);
            SwanNeckLamp(d.Props, d.Collisions, V(K.Half + .7f, d.Ground(13f), 13f), SignBlue);
            SwanNeckLamp(d.Props, d.Collisions, V(-(K.Half + .7f), d.Ground(13.5f), 13.5f), SignBlue);
            StreetTree(d.Props, d.Collisions, V(K.Edge - 1f, d.Ground(33f), 33f), .9f, 9);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft BayPlaza()
        {
            // The plaza east of the bridge: the east entry with its escalators, bus stands, young gums and grass trees,
            // and a brick-paver kicker in the road (the plaza's stepped ramp). Right: Bert Wright Park-style green with
            // big gums, a rotunda and a playground glimpse.
            var d = new CqChunkDraft("Bayswater_Plaza", BayDrop);
            BayStreet(d);
            SideGround(d, -1, 0, 40, 62f, BuffPaver);
            Viaduct(d, 0f, 40f, true, true, VFar, 13f, 33f);
            UnderDeck(d, 0f, 40f);
            EastEntry(d, 15f, 27f);
            Canopy(d.SlopedFrontage, VPlatA, 0f, 26f, VPlat, false, true);
            Sloped(d, d.Deep, t => Canopy(t, VPlatB, 0f, 26f, VPlat, false, true));
            LiftPod(d.SlopedFrontage, V(VPlatA, VPlat, 8f));
            Sloped(d, d.Deep, t => { Gantry(t, -VNear - .3f, -VFar + .3f, VDeck, 3f, VTracks); Gantry(t, -VNear - .3f, -VFar + .3f, VDeck, 30f, VTracks); Catenary(t, 0f, 40f, VDeck, VTracks); });
            PlazaPlanting(d, -1, 1f, 12f, 34);
            PlazaPlanting(d, -1, 29f, 39f, 35);
            BusShelter(d.SlopedFrontage, V(-(K.Edge + 2.6f), K.Kerb, 33f), 90f);
            NameBoard(d.Props, V(-(K.Edge + 1.2f), d.Ground(3f), 3f), "BAYSWATER", .07f, White, CharcoalDark, 2.7f);
            DeepSuburb(d, -1, 0f, 40f, 2741, 48f);
            d.Walls.Add(new Bounds(new Vector3(-(K.Edge + 22f), d.Y(21f) + 3f, 21f), new Vector3(34f, 10f, 14f)));
            // the kicker: buff and red brick pavers with steel edges, in the middle of the road
            var spec = new CqRampSpec { Name = "PlazaKicker", X = 0f, Z = PlazaRampZ, Width = PlazaRampWidth, Rise = PlazaRampRise, Height = PlazaRampHeight, Top = PlazaRampTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, BuffPaver, BrickPaver, false, 0f);
            deck.Surface = ComicSurface.Plain;
            for (float t = .25f; t < 1f; t += .25f)
            {
                float z = PlazaRampZ + PlazaRampRise * t, y = PlazaRampHeight * t + .014f;
                K.Face(deck, V(-PlazaRampWidth * .5f + .1f, y, z), V(-PlazaRampWidth * .5f + .1f, y, z + .3f), V(PlazaRampWidth * .5f - .1f, y, z + .3f), V(PlazaRampWidth * .5f - .1f, y, z), BrickPaver, Vector3.up, false);
            }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            // right: the park
            SideGround(d, 1, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            LowWall(d.SlopedFrontage, 1, 0f, 16f, .5f);
            LowWall(d.SlopedFrontage, 1, 22f, 40f, .5f);
            Ground(d.Road, K.Edge, K.Edge + 22f, 16.4f, 21.6f, K.Kerb + .07f, BuffPaver, ComicSurface.Paving);
            Rotunda(d.Frontage, V(K.Edge + 15f, d.Ground(19f) + .03f, 19f));
            Gum(d.Props, V(K.Edge + 5f, d.Ground(6f), 6f), 15f, 281);
            Gum(d.Props, V(K.Edge + 6.5f, d.Ground(31f), 31f), 13f, 282);
            Jacaranda(d.Props, V(K.Edge + 12f, d.Ground(36f), 36f), 1.1f, 283);
            Bench(d.Props, V(K.Edge + 2f, d.Ground(12f), 12f), 90f);
            Bench(d.Props, V(K.Edge + 2f, d.Ground(27f), 27f), 90f);
            CheapPine(d.Deep, V(K.Edge + 27f, d.Ground(9f), 9f), 19f);
            CheapPine(d.Deep, V(K.Edge + 31f, d.Ground(30f), 30f), 22f);
            WallBox(d, K.Edge + 1f, K.Edge + 40f, 0f, 16f, 6f);
            WallBox(d, K.Edge + 1f, K.Edge + 40f, 22f, 40f, 6f);
            DeepSuburb(d, 1, 0f, 40f, 2742, 36f);
            SwanNeckLamp(d.Props, d.Collisions, V(K.Half + .7f, d.Ground(4f), 4f), SignBlue);
            SwanNeckLamp(d.Props, d.Collisions, V(K.Half + .7f, d.Ground(34f), 34f), SignBlue);
            foreach (float x in new[] { -2.6f, 0f, 2.6f }) d.Slot(x, 8f);
            d.GiftSlots.Add(new Vector2(0f, 28.5f));
            foreach (float x in new[] { -2.6f, 0f, 2.6f }) d.Slot(x, 34f);
            return d;
        }

        static CqChunkDraft BayJunction()
        {
            // The junction east of the station: the Midland line carries on along its own narrow grey viaduct; behind
            // and below it the Airport line runs into its tunnel mouth under a pale louvred building, and the Ellenbrook
            // line's viaduct climbs away to the north on single flared columns with a train on it; portal gantries.
            var d = new CqChunkDraft("Bayswater_Junction", BayDrop);
            BayStreet(d);
            const float mid = 25.6f;
            SideGround(d, -1, 0, 40, 5f, BuffPaver);
            Ground(d.Road, -72f, -VNear, 0f, 40f, K.Kerb + .06f, K.Hex("C9C2AE"), ComicSurface.Concrete);
            TrackBed(d.Road, -27f, -35.2f, 0f, 31f, K.Kerb + .24f, -29.2f, -33f);
            Viaduct(d, 0f, 40f, false, false, mid, 13f, 33f);
            Sloped(d, d.Deep, t =>
            {
                Gantry(t, -VNear - .3f, -mid + .3f, VDeck, 6f, -16.4f, -21.8f);
                Gantry(t, -VNear - .3f, -mid + .3f, VDeck, 26f, -16.4f, -21.8f);
                Catenary(t, 0f, 40f, VDeck, -16.4f, -21.8f);
                Gantry(t, -26.4f, -36f, K.Kerb + .2f, 9f, -29.2f, -33f);
                Catenary(t, 0f, 30f, K.Kerb + .2f, -29.2f, -33f);
                RailFence(t, -26.2f, 0f, 30f, K.Kerb, 1.6f, GalvDark, false);
            });
            Sloped(d, d.Landmark, t =>
            {
                AirportPortal(t, -31.1f, K.Kerb, 30.5f);
                Train(t, V(-29.2f, K.Kerb + .36f, 12.6f), 1, 1, 1, true, false, 17.6f);
                EllenbrookViaduct(t, 0f, 40f);
                var train = new ComicMesh();
                Train(train, Vector3.zero, 1, 1, 1, true, true, 17f);
                Vector3 a = EllenbrookPath(15f, 0f, 40f), b = EllenbrookPath(32f, 0f, 40f);
                t.Add(train, a + V(0, .62f, 0), Quaternion.LookRotation(b - a), Vector3.one);
            });
            PlazaPlanting(d, -1, 11f, 38f, 36);
            LineSign(d.Props, V(-(K.Edge + 1.4f), d.Ground(5f), 5f));
            DeepSuburb(d, -1, 0f, 40f, 2751, 62f);
            WallBox(d, -(K.Edge + 4.6f), -(K.Edge + 40f), 0f, 40f, 9f);
            // right: cottages, jacarandas and tall gums by the line
            SideGround(d, 1, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            Bungalow(d.SlopedFrontage, new CqLot(1, 1f, 13f, K.Edge + 6f, 10f, K.Kerb), K.Hex("E9E2CF"), GreyIron, 1);
            Bungalow(d.SlopedFrontage, new CqLot(1, 15f, 26f, K.Edge + 5f, 10f, K.Kerb), RedBrick, RedIron, 2);
            Bungalow(d.SlopedFrontage, new CqLot(1, 28f, 39f, K.Edge + 6.5f, 10f, K.Kerb), K.Hex("D9C9A8"), TileBrown, 0);
            Jacaranda(d.Props, V(K.Edge + 3.4f, d.Ground(14f), 14f), .95f, 291);
            Gum(d.Props, V(K.Edge + 3.2f, d.Ground(27f), 27f), 13f, 292);
            K.Palm(d.Props, null, V(K.Edge + 4f, d.Ground(37f), 37f), 7f, 5);
            WallBox(d, K.Edge + 1f, K.Edge + 40f, 0f, 40f, 8f);
            DeepSuburb(d, 1, 0f, 40f, 2752, 26f);
            PowerPole(d.Props, V(K.Edge - .7f, d.Ground(20f), 20f));
            K.Thin(d.Collisions, "Pole", V(K.Edge - .7f, d.Ground(20f), 20f), 4f, .35f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft BayExit()
        {
            // The line is back at grade behind its fence; cottages and gums give way to hedges and lawn towards z = 40.
            var d = new CqChunkDraft("Bayswater_Exit", BayDrop);
            BayStreet(d);
            SideGround(d, -1, 0, 40, 5f, Lawn, ComicSurface.Foliage);
            float H(float z) => Mathf.Lerp(VDeck, .25f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 33f, z)));
            Embankment(d, 0f, 40f, H, 3, false);
            Sloped(d, d.Deep, t =>
            {
                Catenary2(t, 0f, 40f, H, -16.4f, -21.8f);
                Gantry(t, -VNear - .6f, -VNear - 12.4f, H(12f) - .1f, 12f, -16.4f, -21.8f);
                Gantry(t, -VNear - .6f, -VNear - 12.4f, H(32f) - .1f, 32f, -16.4f, -21.8f);
            });
            Hedge(d.SlopedFrontage, -1, 26f, 39.6f, .5f, 1.6f, 1.2f);
            Gum(d.Props, V(-(K.Edge + 3f), d.Ground(34f), 34f), 10f, 296);
            DeepSuburb(d, -1, 0f, 40f, 2761, 44f);
            WallBox(d, -(K.Edge + 2f), -(K.Edge + 40f), 0f, 40f, 8f);
            SideGround(d, 1, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            Hedge(d.SlopedFrontage, 1, 26f, 39.6f, .5f, 1.6f, 1.2f);
            Bungalow(d.SlopedFrontage, new CqLot(1, 2f, 13f, K.Edge + 6f, 10f, K.Kerb), K.Hex("D9C9A8"), RedIron, 2);
            Bungalow(d.SlopedFrontage, new CqLot(1, 14.5f, 25f, K.Edge + 5.5f, 10f, K.Kerb), RedBrick, GreyIron, 0);
            Jacaranda(d.Props, V(K.Edge + 3.6f, d.Ground(27.5f), 27.5f), 1.05f, 297);
            Gum(d.Props, V(K.Edge + 5f, d.Ground(35f), 35f), 12f, 298);
            WallBox(d, K.Edge + 1f, K.Edge + 40f, 0f, 40f, 8f);
            DeepSuburb(d, 1, 0f, 40f, 2762, 26f);
            d.StandardSlots();
            return d;
        }

        // ---------------------------------------------------------------- the railway on its embankment and viaduct

        /// <summary>A low limestone wall with a cream cap just beyond the sidewalk (flat road space: add to a sheared mesh).</summary>
        static void LowWall(ComicMesh m, int s, float z0, float z1, float height)
        {
            m.Surface = ComicSurface.Concrete;
            K.SideBox(m, s, z0, z1, -1f, K.Kerb + height, .05f, .5f, Limestone);
            K.SideBox(m, s, z0, z1, K.Kerb + height, K.Kerb + height + .1f, 0f, .55f, LimestoneCap);
            m.Surface = ComicSurface.Plain;
            for (float z = z0 + 1f; z < z1 - .2f; z += 2f)
                K.Face(m, V(s * (K.Edge + .045f), K.Kerb + .05f, z), V(s * (K.Edge + .045f), K.Kerb + .05f, z + .05f), V(s * (K.Edge + .045f), K.Kerb + height, z + .05f), V(s * (K.Edge + .045f), K.Kerb + height, z), LimestoneDark, new Vector3(-s, 0, 0), false);
        }

        /// <summary>Overhead wires following a varying track height.</summary>
        static void Catenary2(ComicMesh m, float z0, float z1, Func<float, float> h, params float[] tracks)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float tx in tracks)
                for (float z = z0; z < z1 - .01f; z += 8f)
                {
                    float e = Mathf.Min(z1, z + 8f);
                    m.Beam(V(tx, h(z) + 5.2f, z), V(tx, h(e) + 5.2f, e), .04f, K.Hex("3A3F45"));
                    m.Beam(V(tx, h(z) + 6.4f, z), V(tx, h(e) + 6.4f, e), .04f, K.Hex("4A5057"));
                }
        }

        /// <summary>The Midland line on a walled embankment on the left: the retaining wall along x = -VNear (style 1
        /// limestone blocks, 2 cream panels with the blue river mural, 3 cream panels with numbat murals), a grey coping
        /// and galvanised rail, two tracks on top following h(z) (rail level above the road).</summary>
        static void Embankment(CqChunkDraft d, float z0, float z1, Func<float, float> h, int style, bool wide)
        {
            ComicMesh m = d.SlopedFrontage;
            float xw = -VNear, far = wide ? -VFar - 6f : -VNear - 13f;
            Color wall = style == 1 ? Limestone : PierCream, cap = style == 1 ? LimestoneCap : ConcreteGrey;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f), ha = h(z), hb = h(e);
                m.Surface = style == 1 ? ComicSurface.Concrete : ComicSurface.Plaster;
                Extrude(m, new[] { V(xw, -1.5f, z), V(xw, ha, z), V(xw, hb, e), V(xw, -1.5f, e) }, V(-.5f, 0, 0), wall);
                Extrude(m, new[] { V(xw + .08f, ha, z), V(xw + .08f, ha + .18f, z), V(xw + .08f, hb + .18f, e), V(xw + .08f, hb, e) }, V(-.7f, 0, 0), cap);
                // joints: blocks for limestone, panel joints for concrete
                m.Surface = ComicSurface.Plain;
                if (style == 1)
                {
                    for (float y = .6f; y < Mathf.Min(ha, hb) - .2f; y += .7f)
                        K.Face(m, V(xw + .01f, y, z), V(xw + .01f, y, e), V(xw + .01f, y + .05f, e), V(xw + .01f, y + .05f, z), LimestoneDark, Vector3.right, false);
                    K.Face(m, V(xw + .01f, K.Kerb, z), V(xw + .01f, K.Kerb, z + .05f), V(xw + .01f, ha, z + .05f), V(xw + .01f, ha, z), LimestoneDark, Vector3.right, false);
                }
                else K.Face(m, V(xw + .01f, K.Kerb, z), V(xw + .01f, K.Kerb, z + .06f), V(xw + .01f, ha, z + .06f), V(xw + .01f, ha, z), FluteShade, Vector3.right, false);
                // ballast and the far slope of the embankment (not inked: on the road mesh)
                ComicMesh g = d.Road;
                g.Surface = ComicSurface.Concrete;
                K.Face(g, V(xw - .5f, ha - .25f, z), V(xw - .5f, hb - .25f, e), V(far, hb - .25f, e), V(far, ha - .25f, z), Ballast, Vector3.up, false);
                K.Face(g, V(far, ha - .25f, z), V(far, hb - .25f, e), V(far - 4f, K.Kerb - .1f, e), V(far - 4f, K.Kerb - .1f, z), Color.Lerp(LawnDry, Ballast, .3f), Vector3.up + Vector3.left, false);
            }
            // rails and sleepers
            ComicMesh r = d.Road;
            foreach (float tx in new[] { -16.4f, -21.8f })
            {
                r.Surface = ComicSurface.Concrete;
                for (float z = z0 + .4f; z < z1; z += 1.3f)
                    K.Face(r, V(tx - 1.15f, h(z) - .22f, z), V(tx - 1.15f, h(z + .3f) - .22f, z + .3f), V(tx + 1.15f, h(z + .3f) - .22f, z + .3f), V(tx + 1.15f, h(z) - .22f, z), Sleeper, Vector3.up, false);
                r.Surface = ComicSurface.Metal;
                foreach (float rx in new[] { tx - .72f, tx + .72f })
                    for (float z = z0; z < z1 - .01f; z += 4f)
                    {
                        float e = Mathf.Min(z1, z + 4f);
                        K.Face(r, V(rx - .05f, h(z) - .08f, z), V(rx - .05f, h(e) - .08f, e), V(rx + .05f, h(e) - .08f, e), V(rx + .05f, h(z) - .08f, z), K.Hex("9CA3AA"), Vector3.up, false);
                        K.Face(r, V(rx + .05f, h(z) - .22f, z), V(rx + .05f, h(e) - .22f, e), V(rx + .05f, h(e) - .08f, e), V(rx + .05f, h(z) - .08f, z), RailSteel, Vector3.right, false);
                    }
            }
            // galvanised rail on the coping (posts every 2.5 m and a top rail)
            Sloped(d, d.Deep, t =>
            {
                t.Surface = ComicSurface.Metal;
                for (float z = z0 + .5f; z < z1; z += 2.5f) t.Box(V(xw - .25f, h(z) + .75f, z), V(.07f, 1.2f, .07f), GalvDark);
                for (float z = z0; z < z1 - .01f; z += 4f)
                {
                    float e = Mathf.Min(z1, z + 4f);
                    t.Beam(V(xw - .25f, h(z) + 1.3f, z), V(xw - .25f, h(e) + 1.3f, e), .06f, Galv);
                    t.Beam(V(xw - .25f, h(z) + .75f, z), V(xw - .25f, h(e) + .75f, e), .04f, Galv);
                }
            });
            if (style == 2) RiverMural(m, xw + .02f, Mathf.Max(z0, 4f), Mathf.Min(z1, 30f), h);
            if (style == 3) NumbatMural(m, xw + .02f, z0 + 1.5f, z1 - 2f, h);
        }

        /// <summary>The blue wavy "river" mural on the abutment wall: three flowing bands in blues.</summary>
        static void RiverMural(ComicMesh m, float x, float z0, float z1, Func<float, float> h)
        {
            m.Surface = ComicSurface.Plain;
            Color[] bands = { MuralDeep, MuralBlue, MuralLight };
            for (float z = z0; z < z1 - .01f; z += 1f)
            {
                float e = Mathf.Min(z1, z + 1f);
                for (int b = 0; b < 3; b++)
                {
                    float top = Mathf.Min(h(z), h(e)) - .25f, scale = Mathf.Clamp01((top - .6f) / 4f);
                    float W(float zz) => .55f + b * .9f * scale + (.35f + .15f * b) * scale * Mathf.Sin(zz * .55f + b * 1.3f);
                    float ya = K.Kerb + W(z), yb = K.Kerb + W(e), th = (.5f + .12f * b) * scale;
                    if (th < .08f) continue;
                    K.Face(m, V(x, ya, z), V(x, yb, e), V(x, yb + th, e), V(x, ya + th, z), bands[b], Vector3.right, false);
                }
            }
        }

        /// <summary>Numbat murals in orange and ochre on the wall (as under the Ellenbrook viaduct): a pointed snout with a
        /// dark eye stripe, a pricked ear, white bands across the rump, four short legs and a raised bushy tail.</summary>
        static void NumbatMural(ComicMesh m, float x, float z0, float z1, Func<float, float> h)
        {
            m.Surface = ComicSurface.Plain;
            int i = 0;
            for (float z = z0; z + 6.4f < z1; z += 7.2f, i++)
            {
                float top = h(z + 6.4f);
                if (top < 2.3f) break;
                float s = Mathf.Min(1.25f, (top - .5f) / 2.9f), y = K.Kerb + .25f;
                Color body = i % 2 == 0 ? NumbatOrange : NumbatOchre, tail = Color.Lerp(body, K.Hex("7A4A2A"), .35f);
                float zz = z;
                void Q(float a0, float b0, float a1, float b1, float a2, float b2, float a3, float b3, Color c) =>
                    K.Face(m, V(x, y + b0 * s, zz + a0 * s), V(x, y + b1 * s, zz + a1 * s), V(x, y + b2 * s, zz + a2 * s), V(x, y + b3 * s, zz + a3 * s), c, Vector3.right, false);
                Q(1.7f, .75f, 4.5f, .7f, 4.6f, 1.75f, 1.7f, 1.65f, body);                      // body
                Q(.95f, .95f, 1.7f, .8f, 1.7f, 1.65f, 1f, 1.55f, body);                        // head
                Q(.15f, 1.1f, .95f, .95f, 1f, 1.45f, .15f, 1.2f, body);                        // snout
                Q(1.35f, 1.55f, 1.65f, 1.6f, 1.6f, 2.05f, 1.42f, 1.95f, body);                 // ear
                Q(.5f, 1.25f, 1.6f, 1.3f, 1.6f, 1.4f, .5f, 1.32f, K.Hex("3A2A22"));            // eye stripe
                Q(4.5f, 1.15f, 6.3f, 1.9f, 6.2f, 2.6f, 4.55f, 1.75f, tail);                    // tail
                foreach (float a in new[] { 1.9f, 2.45f, 3.75f, 4.2f }) Q(a, 0f, a + .3f, 0f, a + .3f, .78f, a, .78f, body);
                for (int k = 0; k < 5; k++) Q(2.75f + k * .36f, .78f, 2.9f + k * .36f, .78f, 2.9f + k * .36f, 1.72f, 2.75f + k * .36f, 1.7f, White);
                Q(.12f, 1.1f, .2f, 1.1f, .2f, 1.2f, .12f, 1.2f, K.Hex("3A2A22"));              // nose
            }
        }

        /// <summary>One hammerhead pier: a fluted cream column flaring in two steps into a wide headstock.</summary>
        static void Pier(ComicMesh m, float x, float z, float top, bool flutes, float half = 3.2f)
        {
            const float cw = 1.5f, cd = 1.9f;
            float colTop = top - 2.5f, midTop = top - 1.5f, flareTop = top - .85f;
            m.Surface = ComicSurface.Plaster;
            K.MinMaxBox(m, V(x - cw * .5f, -1.5f, z - cd * .5f), V(x + cw * .5f, colTop, z + cd * .5f), PierCream);
            Extrude(m, new[] { V(x - cw * .5f, colTop, z - cd * .5f), V(x + cw * .5f, colTop, z - cd * .5f), V(x + cw * .5f + .55f, midTop, z - cd * .5f), V(x - cw * .5f - .55f, midTop, z - cd * .5f) }, V(0, 0, cd), PierCream);
            Extrude(m, new[] { V(x - cw * .5f - .55f, midTop, z - cd * .5f), V(x + cw * .5f + .55f, midTop, z - cd * .5f), V(x + half, flareTop, z - cd * .5f), V(x - half, flareTop, z - cd * .5f) }, V(0, 0, cd), PierCream);
            K.MinMaxBox(m, V(x - half, flareTop, z - cd * .5f - .04f), V(x + half, top, z + cd * .5f + .04f), PierCream);
            if (!flutes) return;
            m.Surface = ComicSurface.Plain;
            for (int i = -1; i <= 1; i++)
            {
                K.Face(m, V(x + i * .42f - .05f, .25f, z - cd * .5f - .012f), V(x + i * .42f + .05f, .25f, z - cd * .5f - .012f), V(x + i * .42f + .05f, colTop - .1f, z - cd * .5f - .012f), V(x + i * .42f - .05f, colTop - .1f, z - cd * .5f - .012f), FluteShade, Vector3.back, false);
                K.Face(m, V(x + cw * .5f + .012f, .25f, z + i * .55f - .05f), V(x + cw * .5f + .012f, .25f, z + i * .55f + .05f), V(x + cw * .5f + .012f, colTop - .1f, z + i * .55f + .05f), V(x + cw * .5f + .012f, colTop - .1f, z + i * .55f - .05f), FluteShade, Vector3.right, false);
            }
        }

        /// <summary>The viaduct from z0 to z1 (flat road space, left side, from x = -VNear to -far): the deck, the edge
        /// towards the road (fluted: the station's cream fluted aluminium band with its rounded lower edge; else a plain
        /// grey parapet), an open galvanised balustrade, the ballast, and hammerhead piers at each z in pierRows (three
        /// across the full-width station deck, one under the narrow two-track viaduct).</summary>
        static void Viaduct(CqChunkDraft d, float z0, float z1, bool station, bool fluted, float far, params float[] pierRows)
        {
            ComicMesh m = d.SlopedFrontage;
            bool narrow = far < 30f;
            m.Surface = ComicSurface.Concrete;
            Strip(m, V(-far, VSoffit, z0), V(-VNear - .02f, VDeck, z1), SoffitGrey);
            float edgeTop = station ? VPlat - .1f : VDeck + .9f;
            if (fluted)
            {
                m.Surface = ComicSurface.PaintedMetal;
                Strip(m, V(-VNear, VSoffit - .25f, z0), V(-VNear + .4f, edgeTop, z1), FluteCream);
                Strip(m, V(-VNear + .04f, VSoffit - .55f, z0), V(-VNear + .5f, VSoffit - .2f, z1), FluteShade);
                Strip(m, V(-VNear - .04f, edgeTop, z0), V(-VNear + .46f, edgeTop + .12f, z1), White);
                m.Surface = ComicSurface.Plain;
                for (float z = z0 + .25f; z < z1 - .2f; z += .5f)
                    K.Face(m, V(-VNear + .41f, VSoffit - .15f, z), V(-VNear + .41f, VSoffit - .15f, z + .25f), V(-VNear + .41f, edgeTop - .06f, z + .25f), V(-VNear + .41f, edgeTop - .06f, z), FluteShade, Vector3.right, false);
            }
            else
            {
                Strip(m, V(-VNear, VSoffit - .1f, z0), V(-VNear + .3f, edgeTop, z1), ConcreteGrey);
                Strip(m, V(-VNear - .05f, edgeTop, z0), V(-VNear + .38f, edgeTop + .14f, z1), K.Hex("D3D5D2"));
            }
            Sloped(d, d.Deep, t =>
            {
                t.Surface = ComicSurface.Metal;
                for (float z = z0 + .6f; z < z1; z += 2.5f) t.Box(V(-VNear + .2f, edgeTop + .7f, z), V(.07f, 1.4f, .07f), GalvDark);
                for (float z = z0; z < z1 - .01f; z += 8f)
                {
                    float e = Mathf.Min(z1, z + 8f);
                    foreach (float y in new[] { .45f, .9f, 1.4f }) t.Box(V(-VNear + .2f, edgeTop + y, (z + e) * .5f), V(.05f, .05f, e - z), Galv);
                }
                // far edge parapet and the ballast on the deck
                t.Surface = ComicSurface.Concrete;
                Strip(t, V(-far - .3f, VSoffit - .1f, z0), V(-far, VDeck + 1f, z1), ConcreteGrey);
                K.Face(t, V(-far, VDeck + .02f, z0), V(-far, VDeck + .02f, z1), V(-VNear, VDeck + .02f, z1), V(-VNear, VDeck + .02f, z0), Ballast, Vector3.up, false);
            });
            foreach (float z in pierRows)
            {
                if (narrow)
                {
                    float cx = -(VNear + far) * .5f;
                    Pier(m, cx, z, VSoffit, false, (far - VNear) * .5f - .5f);
                    d.Walls.Add(new Bounds(new Vector3(cx, d.Y(z) + 2f, z), new Vector3(1.9f, 6f, 2.3f)));
                    continue;
                }
                Pier(m, -17.4f, z, VSoffit, true);
                Sloped(d, d.Deep, t =>
                {
                    Pier(t, -26f, z, VSoffit, false, 3.6f);
                    Pier(t, -34.8f, z, VSoffit, false, 3.6f);
                    t.Surface = ComicSurface.Plaster;
                    K.MinMaxBox(t, V(-22.4f, VSoffit - .8f, z - .9f), V(-20.7f, VSoffit - .02f, z + .9f), PierCream);
                    K.MinMaxBox(t, V(-31.2f, VSoffit - .8f, z - .9f), V(-29.6f, VSoffit - .02f, z + .9f), PierCream);
                });
                d.Walls.Add(new Bounds(new Vector3(-17.4f, d.Y(z) + 2f, z), new Vector3(1.9f, 6f, 2.3f)));
            }
        }

        /// <summary>The ground under the deck: grey pavers, the bus lane in red asphalt with a kerb, bike hoops.</summary>
        static void UnderDeck(CqChunkDraft d, float z0, float z1)
        {
            ComicMesh g = d.Road;
            Ground(g, -VFar - 2f, -VNear, z0, z1, K.Kerb + .06f, ConcretePath, ComicSurface.Paving);
            Ground(g, -31f, -24f, z0, z1, K.Kerb + .1f, RedAsphalt, ComicSurface.Asphalt);
            Ground(g, -24f, -23.7f, z0, z1, K.Kerb + .14f, KerbCap);
            Ground(g, -31.3f, -31f, z0, z1, K.Kerb + .14f, KerbCap);
        }

        /// <summary>A platform canopy over an island platform centred on x: a thin flat white slab with rounded ends, a warm
        /// copper soffit, one central row of round charcoal columns, the platform edge and its yellow tactile line.</summary>
        static void Canopy(ComicMesh m, float x, float z0, float z1, float plat, bool roundStart, bool roundEnd)
        {
            const float hw = 3.7f;
            float a = z0 + (roundStart ? hw : 0f), b = z1 - (roundEnd ? hw : 0f), top = plat + (VCanopy - VPlat);
            m.Surface = ComicSurface.PaintedMetal;
            Strip(m, V(x - hw, top, a), V(x + hw, top + .45f, b), White);
            if (roundStart) m.Cylinder(V(x, top + .225f, a), hw, .45f, White, 8, 1f, Quaternion.Euler(0, 22.5f, 0));
            if (roundEnd) m.Cylinder(V(x, top + .225f, b), hw, .45f, White, 8, 1f, Quaternion.Euler(0, 22.5f, 0));
            m.Surface = ComicSurface.Metal;
            for (float z = a; z < b - .01f; z += 8f)
            {
                float e = Mathf.Min(b, z + 8f);
                K.Face(m, V(x - hw + .25f, top - .02f, z), V(x - hw + .25f, top - .02f, e), V(x + hw - .25f, top - .02f, e), V(x + hw - .25f, top - .02f, z), Copper, Vector3.down);
            }
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = a + 2f; z < b - 1f; z += 8f) Rod(m, V(x, plat, z), V(x, top, z), .2f, .2f, Charcoal, 8);
            // the platform itself: grey pavers, a pale edge and the yellow tactile strip towards the road
            m.Surface = ComicSurface.Concrete;
            Strip(m, V(x - 3f, plat - 1f, z0), V(x + 3f, plat, z1), K.Hex("9FA4A8"));
            m.Surface = ComicSurface.Plain;
            K.Face(m, V(x + 2.4f, plat + .012f, z0), V(x + 2.4f, plat + .012f, z1), V(x + 2.85f, plat + .012f, z1), V(x + 2.85f, plat + .012f, z0), HazardYellow, Vector3.up, false);
            K.Face(m, V(x + 3.01f, plat - .3f, z0), V(x + 3.01f, plat - .3f, z1), V(x + 3.01f, plat, z1), V(x + 3.01f, plat, z0), K.Hex("E9E6DC"), Vector3.right, false);
        }

        /// <summary>A royal-blue platform pod (lift and services) with a stainless door, the copper band wrapping down from
        /// the soffit, and BAYSWATER in white on the band.</summary>
        static void LiftPod(ComicMesh m, Vector3 p)
        {
            float top = VCanopy - VPlat;
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, top * .5f, 0), V(2.6f, top, 4.6f), RoyalBlue);
            m.Surface = ComicSurface.Metal;
            m.Box(p + V(0, top - .45f, 0), V(2.7f, .9f, 4.7f), Copper);
            K.Face(m, p + V(1.31f, .05f, -.7f), p + V(1.31f, .05f, .7f), p + V(1.31f, 2.2f, .7f), p + V(1.31f, 2.2f, -.7f), Stainless, Vector3.right);
            K.Face(m, p + V(1.32f, .05f, -.03f), p + V(1.32f, .05f, .03f), p + V(1.32f, 2.2f, .03f), p + V(1.32f, 2.2f, -.03f), StainlessDark, Vector3.right, false);
            TextFacingX(m, "BAYSWATER", p + V(1.36f, top - .45f, 0), 1, .075f, White);
        }

        /// <summary>The concourse under the deck (flat road space, sheared with the deck): white-tiled base, charcoal panels
        /// with a copper stripe, glazed retail fronts towards Whatley Crescent, the open entry with the stairs going up, a
        /// bronze batten ceiling and the white band reading BAYSWATER STATION.</summary>
        static void Concourse(CqChunkDraft d, float z0, float z1)
        {
            ComicMesh m = d.SlopedFrontage;
            var l = new CqLot(-1, z0, z1, 15f, 16f, K.Kerb);
            float W = l.W, h = VSoffit - K.Kerb - .02f, e0 = W * .5f - 2.6f, e1 = W * .5f + 2.6f;
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, 0, e0, 0, h, 0, -l.Depth, Charcoal);
            K.LBox(m, l, e1, W, 0, h, 0, -l.Depth, Charcoal);
            K.LBox(m, l, e0, e1, 0, h, -5f, -l.Depth, CharcoalDark);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, -.05f, e0 + .05f, 0, 1.1f, .08f, 0, TileWhite);
            K.LBox(m, l, e1 - .05f, W + .05f, 0, 1.1f, .08f, 0, TileWhite);
            m.Surface = ComicSurface.Metal;
            K.LBox(m, l, -.03f, e0 + .03f, 3.55f, 3.75f, .06f, 0, Copper);
            K.LBox(m, l, e1 - .03f, W + .03f, 3.55f, 3.75f, .06f, 0, Copper);
            Shopfront(m, l, .6f, e0 - .5f, 1.1f, 3.4f, .02f, K.Hex("3C6274"));
            Shopfront(m, l, e1 + .5f, W - .6f, 1.1f, 3.4f, .02f, K.Hex("3C6274"));
            // the entry: stairs with stainless rails and glass sides, a ticket-gate line, a bronze batten ceiling
            m.Surface = ComicSurface.Concrete;
            for (int i = 0; i < 6; i++)
                K.LBox(m, l, e0 + .5f, e1 - .5f, 0, .5f + i * .62f, -1.2f - i * .62f, -1.82f - i * .62f, i % 2 == 0 ? K.Hex("5B6168") : K.Hex("6B7178"));
            m.Surface = ComicSurface.Metal;
            foreach (float a in new[] { e0 + .5f, e1 - .5f })
                m.Beam(l.P(a, 1.3f, -1.2f), l.P(a, 4.5f, -4.9f), .09f, Stainless);
            m.Beam(l.P(W * .5f, 1.3f, -1.2f), l.P(W * .5f, 4.5f, -4.9f), .09f, Stainless);
            for (float a = e0 + .9f; a < e1 - .5f; a += 1.1f) K.LBox(m, l, a - .12f, a + .12f, 0, 1f, -.3f, -.9f, K.Hex("C9CED3"));
            m.Surface = ComicSurface.Wood;
            for (float a = e0 + .3f; a < e1; a += .7f) K.LBox(m, l, a, a + .3f, h - .22f, h - .04f, -.1f, -5f, CopperDark);
            // sign band (white, black capitals) with the blue "Trains" plate
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, e0 - 2.2f, e1 + 2.2f, 3.95f, 4.85f, .22f, 0, White);
            K.LBox(m, l, e0 - 2.25f, e1 + 2.25f, 3.88f, 3.96f, .24f, 0, K.R.ink);
            TextOnLot(m, l, "BAYSWATER STATION", W * .5f, 4.4f, .235f, .078f, CharcoalDark);
            K.LBox(m, l, e1 + 2.6f, e1 + 3.5f, 3.95f, 4.85f, .2f, 0, SignBlue);
            K.LQuad(m, l, e1 + 2.8f, e1 + 3.3f, 4.2f, 4.6f, .21f, White, false);
            // totem at the forecourt
            m.Box(l.P(-1.2f, 1.9f, 1.4f), V(.22f, 3.8f, 1f), White);
            m.Box(l.P(-1.2f, 3.6f, 1.4f), V(.24f, .5f, 1.02f), TransGreen);
            m.Box(l.P(-1.2f, .25f, 1.4f), V(.24f, .5f, 1.02f), TransGreen);
        }

        /// <summary>The east entry: escalators and stairs in a glass and stainless box under the deck, charcoal walls, a
        /// copper-soffit canopy lip towards the plaza.</summary>
        static void EastEntry(CqChunkDraft d, float z0, float z1)
        {
            ComicMesh m = d.SlopedFrontage;
            var l = new CqLot(-1, z0, z1, 15.5f, 14f, K.Kerb);
            float W = l.W, h = VSoffit - K.Kerb - .02f;
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, 0, W, 0, h, -6f, -l.Depth, Charcoal);
            K.LBox(m, l, 0, .4f, 0, h, 0, -6f, Charcoal);
            K.LBox(m, l, W - .4f, W, 0, h, 0, -6f, Charcoal);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, -.04f, .44f, 0, 1.1f, .06f, -6f, TileWhite);
            K.LBox(m, l, W - .44f, W + .04f, 0, 1.1f, .06f, -6f, TileWhite);
            // two escalators and a stair between them, rising towards the back
            for (int k = 0; k < 3; k++)
            {
                float a0 = 1f + k * (W - 2f) / 3f, a1 = a0 + (W - 2f) / 3f - .4f;
                bool esc = k != 1;
                m.Surface = esc ? ComicSurface.Metal : ComicSurface.Concrete;
                Vector3 lo = l.P((a0 + a1) * .5f, .1f, -.6f), hi = l.P((a0 + a1) * .5f, h - .4f, -5.8f);
                Slab(m, lo, hi, Vector3.forward, a1 - a0, a1 - a0, .5f, esc ? K.Hex("4A5057") : K.Hex("6B7178"), CharcoalDark);
                m.Surface = ComicSurface.Glass;
                foreach (float a in new[] { a0, a1 })
                    Slab(m, l.P(a, .7f, -.6f), l.P(a, h + .2f, -5.8f), Vector3.forward, .06f, .06f, .9f, K.Hex("BFDCE3"), K.Hex("9FC3CC"));
                m.Surface = ComicSurface.Metal;
                foreach (float a in new[] { a0, a1 }) m.Beam(l.P(a, 1.2f, -.6f), l.P(a, h + .7f, -5.8f), .08f, esc ? K.R.ink : Stainless);
            }
            m.Surface = ComicSurface.Metal;
            K.LBox(m, l, -.3f, W + .3f, 3.9f, 4.2f, 1.4f, 0f, Copper);
            K.LBox(m, l, -.34f, W + .34f, 4.2f, 4.42f, 1.46f, -.05f, White);
            TextOnLot(m, l, "TRAINS", W * .5f, 4.31f, 1.47f, .05f, CharcoalDark);
        }

        /// <summary>The yellow clearance sign on the new bridge: CLEARANCE 4.8 M in black.</summary>
        static void ClearanceSign(ComicMesh m, Vector3 c)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(c, V(.08f, 1.1f, 3.6f), K.R.ink);
            m.Box(c + V(.03f, 0, 0), V(.08f, .96f, 3.46f), HazardYellow);
            TextFacingX(m, "CLEARANCE", c + V(.08f, .2f, 0), 1, .062f, K.R.ink);
            TextFacingX(m, "4.8 M", c + V(.08f, -.22f, 0), 1, .075f, K.R.ink);
        }

        /// <summary>The Easter egg: a piece of the old Bayswater Bridge kept on the forecourt: the cream beam with its
        /// pale-blue panels and the red-and-white striped lower edge on two limestone blocks, the LOW CLEARANCE 3.8 M plate
        /// (the bridge trucks kept hitting until 2023), and a scoreboard: BRIDGE 74 TRUCKS 0. Faces the road (+X).</summary>
        static void OldBridgeMemorial(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Concrete;
            foreach (float z in new[] { -2.6f, 2.6f })
            {
                m.Box(p + V(0, .55f, z), V(1.1f, 1.1f, 1f), Limestone);
                m.Box(p + V(0, 1.14f, z), V(1.2f, .1f, 1.1f), LimestoneCap);
            }
            m.Box(p + V(0, 1.95f, 0), V(.7f, 1.5f, 6.6f), OldBridgeCream);
            m.Surface = ComicSurface.Plain;
            foreach (float z in new[] { -2.2f, 2.2f })
            {
                K.Face(m, p + V(.36f, 1.75f, z - .85f), p + V(.36f, 1.75f, z + .85f), p + V(.36f, 2.5f, z + .85f), p + V(.36f, 2.5f, z - .85f), OldBridgeBlue, Vector3.right, false);
                K.Face(m, p + V(.37f, 2.06f, z - .6f), p + V(.37f, 2.06f, z + .6f), p + V(.37f, 2.18f, z + .6f), p + V(.37f, 2.18f, z - .6f), White, Vector3.right, false);
            }
            for (int i = 0; i < 22; i++)
                K.Face(m, p + V(.36f, 1.2f, -3.3f + i * .3f), p + V(.36f, 1.2f, -3f + i * .3f), p + V(.36f, 1.52f, -3f + i * .3f), p + V(.36f, 1.52f, -3.3f + i * .3f), i % 2 == 0 ? HazardRed : White, Vector3.right, false);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(.38f, 2.12f, 0), V(.06f, .8f, 2.3f), K.R.ink);
            m.Box(p + V(.4f, 2.12f, 0), V(.06f, .7f, 2.2f), White);
            TextFacingX(m, "LOW CLEARANCE", p + V(.44f, 2.27f, 0), 1, .034f, K.R.ink);
            TextFacingX(m, "3.8 M", p + V(.44f, 1.98f, 0), 1, .05f, K.R.ink);
            // the scoreboard on a post beside it
            Vector3 s = p + V(.1f, 0, -4.4f);
            m.Box(s + V(0, 1.1f, 0), V(.1f, 2.2f, .1f), Galv);
            m.Box(s + V(.08f, 2.2f, 0), V(.08f, 1.05f, 1.9f), SignGreen);
            m.Box(s + V(.05f, 2.2f, 0), V(.06f, 1.15f, 2f), White);
            TextFacingX(m, "BRIDGE 74", s + V(.13f, 2.43f, 0), 1, .045f, White);
            TextFacingX(m, "TRUCKS 0", s + V(.13f, 2f, 0), 1, .045f, HazardYellow);
        }

        /// <summary>A bus shelter: charcoal frame with rounded corners, a copper soffit, a perforated side screen, a bench
        /// and a tall white totem with green top and bottom. Opens towards yaw (90 = towards +X).</summary>
        static void BusShelter(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            void B(Vector3 c, Vector3 size, Color col, ComicSurface s) { m.Surface = s; m.Box(p + q * c, size, col, q); }
            B(V(0, 2.62f, 0), V(4.2f, .16f, 1.7f), Charcoal, ComicSurface.PaintedMetal);
            B(V(0, 2.52f, 0), V(4f, .06f, 1.5f), Copper, ComicSurface.Metal);
            foreach (float x in new[] { -1.95f, 1.95f }) B(V(x, 1.3f, .7f), V(.12f, 2.6f, .12f), Charcoal, ComicSurface.PaintedMetal);
            B(V(0, 1.45f, .76f), V(3.8f, 1.9f, .05f), K.Hex("BFDCE3"), ComicSurface.Glass);
            B(V(-1.95f, 1.45f, .1f), V(.05f, 1.9f, 1.1f), K.Hex("6B7178"), ComicSurface.Metal);
            B(V(0, .46f, .45f), V(2.6f, .08f, .42f), Timber, ComicSurface.Wood);
            foreach (float x in new[] { -1.1f, 1.1f }) B(V(x, .22f, .45f), V(.08f, .44f, .4f), Charcoal, ComicSurface.PaintedMetal);
            B(V(2.9f, 1.6f, .5f), V(.5f, 3.2f, .14f), White, ComicSurface.PaintedMetal);
            B(V(2.9f, 3f, .5f), V(.52f, .45f, .16f), TransGreen, ComicSurface.PaintedMetal);
            B(V(2.9f, .2f, .5f), V(.52f, .4f, .16f), TransGreen, ComicSurface.PaintedMetal);
        }

        /// <summary>Plaza planting between the footpath and the viaduct: mulch beds with young gums, grass trees and
        /// strappy natives, timber benches.</summary>
        static void PlazaPlanting(CqChunkDraft d, int s, float z0, float z1, int seed)
        {
            var r = new System.Random(seed * 13 + 1);
            for (float z = z0; z + 5f <= z1 + .01f; z += 9f)
            {
                float e = Mathf.Min(z1, z + 5.5f);
                Ground(d.Road, s * (K.Edge + .8f), s * (K.Edge + 3.6f), z, e, K.Kerb + .06f, Mulch);
                d.SlopedFrontage.Surface = ComicSurface.Concrete;
                K.MinMaxBox(d.SlopedFrontage, V(s * (K.Edge + .7f), K.Kerb - .1f, z - .1f), V(s * (K.Edge + .82f), K.Kerb + .22f, e + .1f), ConcreteGrey);
                float zc = (z + e) * .5f;
                Gum(d.Props, V(s * (K.Edge + 2.3f), d.Ground(zc), zc), F(r, 6f, 8f), seed * 7 + (int)z);
                GrassTree(d.Props, V(s * (K.Edge + 1.7f), d.Ground(z + 1f), z + 1f), F(r, .7f, 1f), seed + (int)z);
                float ez = e, zs = z;
                Sloped(d, d.Deep, t =>
                {
                    Sedge(t, V(s * (K.Edge + 2.8f), K.Kerb + .06f, ez - 1f), .8f, seed + (int)zs, K.Hex("6F9A52"), K.Hex("9BBE6A"));
                    Sedge(t, V(s * (K.Edge + 1.6f), K.Kerb + .06f, ez - .6f), .6f, seed + 3 + (int)zs, K.Hex("7FA650"), K.Hex("B4C878"));
                    Sedge(t, V(s * (K.Edge + 3f), K.Kerb + .06f, zs + .8f), .7f, seed + 5 + (int)zs, K.Hex("86A85A"), K.Hex("B4C878"));
                });
                if (e + 2.6f < z1) Bench(d.Props, V(s * (K.Edge + 2.4f), d.Ground(e + 1.7f), e + 1.7f), s < 0 ? -90f : 90f);
            }
        }

        /// <summary>A wayfinding totem at the junction: three coloured plates, one for each line that parts here.</summary>
        static void LineSign(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 2.1f, .1f), V(2.5f, 4.2f, .16f), White);
            m.Box(p + V(0, 4.3f, .1f), V(2.56f, .3f, .2f), TransGreen);
            m.Box(p + V(0, .2f, .1f), V(2.56f, .4f, .2f), TransGreen);
            string[] names = { "MIDLAND", "AIRPORT", "ELLENBROOK" };
            Color[] cols = { K.Hex("7A3E96"), K.Hex("2A9D8F"), K.Hex("D8332B") };
            for (int i = 0; i < 3; i++)
            {
                float y = 3.5f - i * 1.05f;
                m.Box(p + V(0, y, 0f), V(2.3f, .8f, .06f), cols[i]);
                TextToRider(m, names[i], p + V(.15f, y, -.04f), .042f, White);
                // an arrow: Midland straight on, Airport down into the tunnel, Ellenbrook up and away to the left
                Quaternion q = Quaternion.Euler(0, 0, i == 0 ? 0f : i == 1 ? -135f : 135f);
                m.Box(p + V(-.9f, y, -.04f), V(.07f, .42f, .02f), White, q);
                m.Box(p + V(-.9f, y, -.04f) + q * V(-.1f, .14f, 0), V(.07f, .22f, .02f), White, q * Quaternion.Euler(0, 0, -45f));
                m.Box(p + V(-.9f, y, -.04f) + q * V(.1f, .14f, 0), V(.07f, .22f, .02f), White, q * Quaternion.Euler(0, 0, 45f));
            }
        }

        /// <summary>The Airport line's dive: a grey concrete trough between the tracks going down into the tunnel mouth,
        /// under a pale-grey flat-roofed louvred building, behind a fenced compound. Centre x, from z.</summary>
        static void AirportPortal(ComicMesh m, float x, float y, float z)
        {
            m.Surface = ComicSurface.Concrete;
            foreach (int s in new[] { -1, 1 }) K.MinMaxBox(m, V(x + s * 4.2f - .25f, y - 1f, z - 20f), V(x + s * 4.2f + .25f, y + 1.3f, z), ConcreteGrey);
            K.MinMaxBox(m, V(x - 4.5f, y - 1f, z), V(x + 4.5f, y + 5.2f, z + 1f), ConcreteGrey);
            m.Surface = ComicSurface.Plain;
            K.Face(m, V(x - 3.6f, y - .9f, z - .02f), V(x + 3.6f, y - .9f, z - .02f), V(x + 3.6f, y + 3.4f, z - .02f), V(x - 3.6f, y + 3.4f, z - .02f), K.Hex("1E2126"), Vector3.back, false);
            K.Face(m, V(x - .12f, y - .9f, z - .04f), V(x + .12f, y - .9f, z - .04f), V(x + .12f, y + 3.4f, z - .04f), V(x - .12f, y + 3.4f, z - .04f), ConcreteGrey, Vector3.back, false);
            m.Surface = ComicSurface.Metal;
            K.MinMaxBox(m, V(x - 5f, y + 5.2f, z + .2f), V(x + 5f, y + 8.4f, z + 9f), K.Hex("D6DADC"));
            K.MinMaxBox(m, V(x - 5.3f, y + 8.4f, z - .1f), V(x + 5.3f, y + 8.7f, z + 9.3f), K.Hex("B9BEC2"));
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 5; i++)
            {
                K.Face(m, V(x - 4.3f, y + 5.7f + i * .5f, z + .18f), V(x + 4.3f, y + 5.7f + i * .5f, z + .18f), V(x + 4.3f, y + 5.92f + i * .5f, z + .18f), V(x - 4.3f, y + 5.7f + .22f + i * .5f, z + .18f), K.Hex("9BA2A8"), Vector3.back, false);
                K.Face(m, V(x + 5.02f, y + 5.7f + i * .5f, z + 1f), V(x + 5.02f, y + 5.7f + i * .5f, z + 8.2f), V(x + 5.02f, y + 5.92f + i * .5f, z + 8.2f), V(x + 5.02f, y + 5.92f + i * .5f, z + 1f), K.Hex("9BA2A8"), Vector3.right, false);
            }
        }

        static Vector3 EllenbrookPath(float z, float z0, float z1) { float t = Mathf.InverseLerp(z0, z1, z); return V(-36f - 14f * t * t, 6.1f + 3.2f * t, z); }

        /// <summary>The Ellenbrook line's viaduct behind the junction: plain grey concrete on single round columns with
        /// flared heads, a galvanised mesh balustrade, climbing and swinging away to the north (-X).</summary>
        static void EllenbrookViaduct(ComicMesh m, float z0, float z1)
        {
            Vector3 P(float z) => EllenbrookPath(z, z0, z1);
            for (float z = z0; z < z1 - .01f; z += 5f)
            {
                Vector3 a = P(z), b = P(Mathf.Min(z1, z + 5f)), side = Vector3.Cross(Vector3.up, (b - a).normalized);
                m.Surface = ComicSurface.Concrete;
                Slab(m, a, b, side, 6.4f, 6.4f, 1.2f, K.Hex("C4C6C3"), SoffitGrey);
                m.Surface = ComicSurface.Metal;
                foreach (int s in new[] { -1, 1 }) m.Beam(a + side * s * 3.1f + V(0, 1.7f, 0), b + side * s * 3.1f + V(0, 1.7f, 0), .08f, Galv);
            }
            for (float z = z0 + 4f; z < z1; z += 12f)
            {
                Vector3 c = P(z);
                m.Surface = ComicSurface.Concrete;
                Rod(m, V(c.x, -1.5f, z), V(c.x, c.y - 2.2f, z), .95f, .95f, K.Hex("C9CBC8"), 8);
                Rod(m, V(c.x, c.y - 2.2f, z), V(c.x, c.y - .6f, z), .95f, 2.6f, K.Hex("C9CBC8"), 8);
            }
        }

        // ---------------------------------------------------------------- Bayswater buildings (lot space)

        /// <summary>A single-storey parapet shop (Federation / inter-war): ink plinth, brick or rendered wall, capped piers,
        /// framed shopfront glazing and a recessed door, a boxed cantilever awning on tie rods with a plain sign panel, a
        /// cornice and parapet (centre: 0 flat, 1 gable, 2 curved, 3 stepped, 4 crenellated), an iron roof behind.</summary>
        static void ParapetShop(ComicMesh f, CqLot l, Color wall, Color awning, int centre, int seed, int bays = 1)
        {
            float W = l.W, D = l.Depth, h = 4.5f;
            Color pier = Color.Lerp(wall, K.R.ink, .14f), trim = Color.Lerp(wall, White, .6f);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            f.Surface = wall == CreamBrick || wall == RedBrick ? ComicSurface.Brick : ComicSurface.Plaster;
            K.LBox(f, l, 0, W, 0f, h, 0f, -D, wall);
            // tiled stall risers and piers at both ends and between bays
            for (int i = 0; i <= bays; i++)
            {
                float a = Mathf.Clamp(W * i / bays, .28f, W - .28f);
                K.LBox(f, l, a - .28f, a + .28f, 0f, h, .16f, 0f, pier);
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, a - .34f, a + .34f, 0f, .5f, .22f, 0f, Color.Lerp(pier, K.R.ink, .3f));
                f.Surface = ComicSurface.Plaster;
            }
            for (int i = 0; i < bays; i++)
            {
                float a0 = W * i / bays + .56f, a1 = W * (i + 1) / bays - .56f;
                Shopfront(f, l, a0, a1, .05f, 3.1f, .03f, null, true);
                f.Surface = ComicSurface.Glass;
                K.LQuad(f, l, a0 + .05f, a1 - .05f, 3.75f, 4.2f, .02f, Color.Lerp(ShopGlass, White, .35f));  // leadlight highlights
                SignPanel(f, l, a0 + .5f, a1 - .5f, 3.27f, 3.67f, 2.36f, SignColours[(seed + i) % SignColours.Length]);
            }
            BoxAwning(f, l, -.02f, W + .02f, 3.2f, 2.35f, awning, true, 1.2f);
            Parapet(f, l, 0f, W, h, 1.1f, wall, trim, centre);
            SkillionRoof(f, l, .2f, W - .2f, h + .3f, D, seed % 2 == 0 ? RedIron : GreyIron);
        }

        /// <summary>The white two-storey corner building: rendered walls, a moulded parapet with a curved pediment, tall
        /// upper windows with heads between pilasters, and a blue boxed awning wrapping the corner (towards the rider).</summary>
        static void CornerShop(ComicMesh f, CqLot l)
        {
            float W = l.W, D = l.Depth, gh = 4.2f, h = 8.2f;
            Color wall = K.Hex("F3F0E6"), trim = White, awning = K.Hex("2F5FA8");
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0, W, 0f, h, 0f, -D, wall);
            int bays = 3;
            for (int i = 0; i <= bays; i++)
            {
                float a = Mathf.Clamp(W * i / bays, .3f, W - .3f);
                K.LBox(f, l, a - .3f, a + .3f, 0f, h, .16f, 0f, Color.Lerp(wall, K.R.ink, .08f));
            }
            for (int i = 0; i < bays; i++)
            {
                float a0 = W * i / bays + .6f, a1 = W * (i + 1) / bays - .6f, ac = (a0 + a1) * .5f;
                Shopfront(f, l, a0, a1, .05f, 3.1f, .03f, null, i == 1);
                FramedWindow(f, l, ac - .75f, 1.05f, gh + 1.1f, gh + 3.2f, .02f, trim, true, true, true);
                FramedWindow(f, l, ac + .75f, 1.05f, gh + 1.1f, gh + 3.2f, .02f, trim, true, true, true);
            }
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.06f, W + .06f, gh + .25f, gh + .5f, .2f, -D - .06f, trim);
            BoxAwning(f, l, -.02f, W + .02f, 3.2f, 2.35f, awning, true, 1.4f);
            // the awning wraps round the end facing the rider
            f.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(f, l.P(-2.2f, 3.2f, 2.35f), l.P(0f, 3.62f, -D * .6f), awning);
            K.MinMaxBox(f, l.P(-2.24f, 3.13f, 2.39f), l.P(-2.05f, 3.22f, -D * .6f), K.R.ink);
            EndWindow(f, l, 0f, -1.5f, -3f, gh + 1.1f, gh + 3.2f);
            EndWindow(f, l, 0f, -5f, -6.5f, gh + 1.1f, gh + 3.2f);
            f.Surface = ComicSurface.PaintedMetal;
            K.Face(f, l.P(-.02f, .05f, -1f), l.P(-.02f, .05f, -D * .55f), l.P(-.02f, 3.05f, -D * .55f), l.P(-.02f, 3.05f, -1f), K.R.ink, Vector3.back);
            f.Surface = ComicSurface.Glass;
            K.Face(f, l.P(-.04f, .5f, -1.15f), l.P(-.04f, .5f, -D * .55f + .15f), l.P(-.04f, 2.9f, -D * .55f + .15f), l.P(-.04f, 2.9f, -1.15f), ShopGlass, Vector3.back);
            Parapet(f, l, 0f, W, h, 1.2f, wall, trim, 2);
            SkillionRoof(f, l, .2f, W - .2f, h + .3f, D, GreyIron);
            SignPanel(f, l, W * .5f - 2f, W * .5f + 2f, 3.27f, 3.67f, 2.36f, White);
        }

        /// <summary>The two-storey verandah hotel. Bayswater (brick = false): white walls, a hipped terracotta-tile roof,
        /// a two-level verandah with thin posts and a dark balustrade, red HOTEL letters on a rooftop frame. Bassendean
        /// (brick = true): red face brick below, a cream parapet with a raised corner pediment, charcoal posts.</summary>
        static void VerandahHotel(ComicMesh f, CqLot l, bool brick)
        {
            float W = l.W, D = l.Depth, gh = 4f, h = 7.8f, vd = 2.7f;
            Color wall = brick ? RedBrick : K.Hex("F3F0E6"), upper = brick ? K.Hex("EFE4C8") : wall, post = brick ? Charcoal : White, rail = brick ? Charcoal : K.Hex("3A3F45");
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            f.Surface = brick ? ComicSurface.Brick : ComicSurface.Plaster;
            K.LBox(f, l, 0, W, 0f, gh, 0f, -D, wall);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0, W, gh, h, 0f, -D, upper);
            int bays = Mathf.Max(3, Mathf.RoundToInt(W / 3.6f));
            float bw = W / bays;
            for (int i = 0; i < bays; i++)
            {
                float ac = (i + .5f) * bw;
                bool door = i == bays / 2 || i == 0;
                if (door)
                {
                    f.Surface = ComicSurface.PaintedMetal;
                    K.LQuad(f, l, ac - .8f, ac + .8f, .05f, 3f, .02f, K.R.ink);
                    f.Surface = ComicSurface.Wood;
                    K.LQuad(f, l, ac - .68f, ac + .68f, .08f, 2.3f, .04f, K.Hex("5B4636"));
                    f.Surface = ComicSurface.Glass;
                    K.LQuad(f, l, ac - .68f, ac + .68f, 2.4f, 2.9f, .04f, WindowGlass);
                }
                else FramedWindow(f, l, ac, 1.5f, 1f, 3f, .02f, TrimCream, true, true, true);
                // french doors onto the upper verandah
                FramedWindow(f, l, ac, 1.2f, gh + .35f, gh + 2.75f, .02f, TrimCream, false, true);
            }
            // verandah: deck, posts in pairs of levels, balustrade, a skillion roof with an ink fascia
            f.Surface = ComicSurface.Wood;
            K.LBox(f, l, -.1f, W + .1f, gh - .12f, gh + .1f, vd, 0f, Color.Lerp(post, K.R.ink, .15f));
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, -.14f, W + .14f, gh - .22f, gh - .1f, vd + .05f, vd - .2f, K.R.ink);
            for (int i = 0; i <= bays; i++)
            {
                float a = Mathf.Clamp(i * bw, .12f, W - .12f);
                K.LBox(f, l, a - .08f, a + .08f, 0f, gh - .12f, vd - .1f, vd - .26f, post);
                K.LBox(f, l, a - .07f, a + .07f, gh + .1f, h - .55f, vd - .1f, vd - .24f, post);
                K.LBox(f, l, a - .16f, a + .16f, gh - .5f, gh - .12f, vd - .08f, vd - .28f, post);   // bracket block
            }
            K.LBox(f, l, 0f, W, gh + 1f, gh + 1.1f, vd - .1f, vd - .22f, rail);
            K.LBox(f, l, 0f, W, gh + .2f, gh + .28f, vd - .12f, vd - .2f, rail);
            f.Surface = ComicSurface.Plain;
            for (float a = .2f; a < W - .1f; a += .3f) K.LQuad(f, l, a - .035f, a + .035f, gh + .28f, gh + 1f, vd - .16f, brick ? K.Hex("4A5057") : White, false);
            f.Surface = ComicSurface.Metal;
            Vector3 r0 = l.P(-.2f, h - .1f, 0f), r1 = l.P(W + .2f, h - .1f, 0f), e0 = l.P(-.2f, h - .6f, vd + .25f), e1 = l.P(W + .2f, h - .6f, vd + .25f);
            K.Face(f, r0, r1, e1, e0, brick ? GreyIron : K.Hex("B9C0C6"), Vector3.up);
            K.Face(f, e0, e1, e1 + V(0, -.16f, 0), e0 + V(0, -.16f, 0), K.R.ink, new Vector3(-l.S, 0, 0));
            K.Face(f, r0 + V(0, -.02f, 0), r1 + V(0, -.02f, 0), e1 + V(0, -.02f, 0), e0 + V(0, -.02f, 0), Color.Lerp(post, K.R.ink, .3f), Vector3.down);
            if (brick)
            {
                // cream parapet with a raised corner pediment and the word HOTEL in relief
                Parapet(f, l, 0f, W, h, 1.2f, upper, TrimCream, 0);
                f.Surface = ComicSurface.Plaster;
                K.LBox(f, l, 0f, 5.2f, h + 1.3f, h + 2.3f, .1f, -.5f, upper);
                LArch(f, l, .4f, 4.8f, h + 2.3f, .8f, .12f, -.45f, TrimCream);
                TextOnLot(f, l, "HOTEL", 2.6f, h + 1.8f, .11f, .085f, RedBrickDark);
                SkillionRoof(f, l, .2f, W - .2f, h + .3f, D, RedIron);
                return;
            }
            // hipped tile roof, two chimneys, the red letters on a frame
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.25f, W + .25f, h - .12f, h + .08f, .3f, -D - .25f, K.R.ink);
            f.Surface = ComicSurface.Brick;
            Vector3 c = l.P(W * .5f, h + .08f, -D * .5f);
            HipRoof(f, c, D + .9f, W + .9f, 2.6f, TileBrown);
            Chimney(f, l.P(W * .22f, h + 1.2f, -D * .5f), 2.2f, K.Hex("E9E2CF"));
            Chimney(f, l.P(W * .8f, h + 1.2f, -D * .5f), 2.2f, K.Hex("E9E2CF"));
            f.Surface = ComicSurface.PaintedMetal;
            float fa0 = W * .5f - 4f, fa1 = W * .5f + 4f;
            foreach (float a in new[] { fa0, W * .5f - 1.4f, W * .5f + 1.4f, fa1 }) f.Beam(l.P(a, h + .2f, -.8f), l.P(a, h + 3.5f, -.8f), .1f, K.R.ink);
            K.LBox(f, l, fa0 - .1f, fa1 + .1f, h + 1.38f, h + 1.48f, -.72f, -.88f, K.R.ink);
            K.LBox(f, l, fa0 - .1f, fa1 + .1f, h + 3.4f, h + 3.5f, -.72f, -.88f, K.R.ink);
            // HOTEL: each lit pixel is a small red block so the letters stand free on the frame
            TextBlocks(f, "HOTEL", l.P(W * .5f, h + 2.44f, -.68f), new Vector3(0, 0, -l.S), .34f, K.Hex("D8332B"), .14f, new Vector3(-l.S, 0, 0));
        }

        /// <summary>Free-standing pixel letters made of small boxes (run-merged), thick along normal.</summary>
        static void TextBlocks(ComicMesh m, string text, Vector3 c, Vector3 right, float px, Color colour, float thick, Vector3 normal)
        {
            m.Surface = ComicSurface.PaintedMetal;
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            float x = -PixelWidth(text, px) * .5f, y0 = 2.5f * px;
            foreach (char ch in text)
            {
                if (!Glyphs.TryGetValue(ch, out string g)) { x += 3 * px; continue; }
                string[] rows = g.Split('/');
                for (int row = 0; row < rows.Length; row++)
                    for (int col = 0; col < rows[row].Length; col++)
                    {
                        if (rows[row][col] != '1') continue;
                        int end = col;
                        while (end + 1 < rows[row].Length && rows[row][end + 1] == '1') end++;
                        float xa = x + col * px, xb = x + (end + 1) * px, y = y0 - (row + .5f) * px;
                        m.Box(c + right * ((xa + xb) * .5f) + Vector3.up * y, new Vector3(xb - xa, px, thick), colour, q);
                        col = end;
                    }
                x += (rows[0].Length + 1) * px;
            }
        }

        /// <summary>The white gabled wing beside the hotel: a rendered gable end to the street with red trim (bargeboards,
        /// window frames), a bullnose awning.</summary>
        static void GabledWing(ComicMesh f, CqLot l)
        {
            float W = l.W, D = l.Depth, h = 4.3f, ridge = h + W * .36f;
            Color wall = K.Hex("F3F0E6"), red = K.Hex("B43A32");
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0, W, 0f, h, 0f, -D, wall);
            Shopfront(f, l, .5f, W - .5f, .05f, 3f, .02f, null, true);
            f.Surface = ComicSurface.Metal;
            K.LGable(f, l, -.3f, W + .3f, h, ridge, .35f, -D, RedIron);
            f.Surface = ComicSurface.Plaster;
            K.TriFacing(f, l.P(.2f, h + .02f, .37f), l.P(W - .2f, h + .02f, .37f), l.P(W * .5f, ridge - .35f, .37f), wall, new Vector3(-l.S, 0, 0));
            f.Surface = ComicSurface.PaintedMetal;
            f.Beam(l.P(-.3f, h + .02f, .42f), l.P(W * .5f, ridge + .04f, .42f), .16f, red);
            f.Beam(l.P(W + .3f, h + .02f, .42f), l.P(W * .5f, ridge + .04f, .42f), .16f, red);
            Rod(f, l.P(W * .5f, ridge - .1f, .4f), l.P(W * .5f, ridge + .9f, .4f), .07f, .03f, red, 4, true);
            K.LQuad(f, l, W * .5f - .45f, W * .5f + .45f, h + .6f, h + 1.6f, .385f, red);
            f.Surface = ComicSurface.Glass;
            K.LQuad(f, l, W * .5f - .35f, W * .5f + .35f, h + .7f, h + 1.5f, .4f, WindowGlass);
            // bullnose verandah awning on two posts
            f.Surface = ComicSurface.Metal;
            Vector3 a0 = l.P(-.1f, 3.5f, 0f), a1 = l.P(W + .1f, 3.5f, 0f), b0 = l.P(-.1f, 3.25f, 1.6f), b1 = l.P(W + .1f, 3.25f, 1.6f), c0 = l.P(-.1f, 2.75f, 2.2f), c1 = l.P(W + .1f, 2.75f, 2.2f);
            K.Face(f, a0, a1, b1, b0, red, Vector3.up);
            K.Face(f, b0, b1, c1, c0, Color.Lerp(red, K.R.ink, .15f), Vector3.up + new Vector3(-l.S, 0, 0));
            K.Face(f, a0 + V(0, -.03f, 0), a1 + V(0, -.03f, 0), c1 + V(0, -.03f, 0), c0 + V(0, -.03f, 0), Color.Lerp(red, K.R.ink, .4f), Vector3.down);
            f.Surface = ComicSurface.PaintedMetal;
            foreach (float a in new[] { .15f, W - .15f }) K.LBox(f, l, a - .07f, a + .07f, 0f, 2.75f, 2.2f, 2.06f, White);
        }

        /// <summary>The old post office: single storey, cream render, a stripped-classical flat parapet with POST OFFICE
        /// incised, three tall windows, a panelled door under a small hood, a white chimney; it sits back above a limestone
        /// wall with steps and a low colourful mural fence.</summary>
        static void PostOffice(ComicMesh f, CqLot l)
        {
            float W = l.W, D = l.Depth, b = .9f, h = b + 4.6f;
            Color wall = K.Hex("EDE3C4"), trim = K.Hex("F7F1DC");
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.6f, W + .6f, -2.6f, b, 2.9f, -D, Limestone);
            K.LBox(f, l, -.66f, W + .66f, b, b + .1f, 2.96f, 2.4f, LimestoneCap);
            f.Surface = ComicSurface.Plain;
            for (float a = .4f; a < W + .4f; a += 1.4f) K.LQuad(f, l, a, a + .05f, .05f, b - .05f, 2.91f, LimestoneDark, false);
            Color[] mural = { K.Hex("E2463A"), K.Hex("F6C543"), K.Hex("2A9D9A"), K.Hex("2F6FB0"), K.Hex("E85A8C"), K.Hex("7A5FA8") };
            for (int i = 0; i < 10; i++)
            {
                float a = -.4f + i * (W + .8f) / 10f;
                if (Mathf.Abs(a + .7f - W * .5f) < 1.8f) continue;
                f.Surface = ComicSurface.Wood;
                K.LBox(f, l, a + .08f, a + (W + .8f) / 10f - .08f, b + .1f, b + .95f, 2.8f, 2.72f, mural[i % mural.Length]);
            }
            f.Surface = ComicSurface.Concrete;
            for (int i = 0; i < 4; i++) K.LBox(f, l, W * .5f - 1.5f, W * .5f + 1.5f, 0f, b - i * .22f, 2.9f + (4 - i) * .32f, 2.9f + (3 - i) * .32f, LimestoneCap);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0, W, b, h, 0f, -D, wall);
            K.LBox(f, l, -.1f, W + .1f, b, b + .45f, .1f, -D - .1f, Color.Lerp(wall, K.R.ink, .18f));
            foreach (float a in new[] { .35f, W - .35f }) K.LBox(f, l, a - .35f, a + .35f, b, h, .14f, 0f, trim);
            foreach (float a in new[] { W * .2f, W * .8f, W * .5f + 2.6f })
                if (a < W - 1f) FramedWindow(f, l, a, 1.2f, b + 1.1f, b + 3.5f, .02f, trim, true, true, true);
            FramedWindow(f, l, W * .5f - 2.6f, 1.2f, b + 1.1f, b + 3.5f, .02f, trim, true, true, true);
            f.Surface = ComicSurface.PaintedMetal;
            K.LQuad(f, l, W * .5f - .75f, W * .5f + .75f, b + .02f, b + 3.2f, .02f, K.R.ink);
            f.Surface = ComicSurface.Wood;
            K.LQuad(f, l, W * .5f - .62f, W * .5f + .62f, b + .05f, b + 2.5f, .04f, K.Hex("6A4B36"));
            f.Surface = ComicSurface.Glass;
            K.LQuad(f, l, W * .5f - .62f, W * .5f + .62f, b + 2.6f, b + 3.1f, .04f, WindowGlass);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, W * .5f - 1.2f, W * .5f + 1.2f, b + 3.3f, b + 3.5f, .9f, 0f, trim);
            // the flat stepped parapet with the incised name
            K.LBox(f, l, -.15f, W + .15f, h, h + .3f, .28f, -D - .1f, trim);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0f, W, h + .3f, h + 1.2f, .06f, -.5f, wall);
            K.LBox(f, l, W * .5f - 3.2f, W * .5f + 3.2f, h + 1.2f, h + 1.75f, .1f, -.5f, wall);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, W * .5f - 3.3f, W * .5f + 3.3f, h + 1.75f, h + 1.9f, .18f, -.56f, trim);
            K.LBox(f, l, -.05f, W + .05f, h + 1.2f, h + 1.32f, .12f, -.54f, trim);
            TextOnLot(f, l, "POST OFFICE", W * .5f, h + .82f, .07f, .085f, Color.Lerp(wall, K.R.ink, .55f));
            SkillionRoof(f, l, .2f, W - .2f, h + .3f, D, RedIron);
            Chimney(f, l.P(W - 1.6f, h + .4f, -D * .5f), 2.4f, K.Hex("F3F0E6"));
        }

        /// <summary>A Federation / inter-war bungalow behind a garden: brick or weatherboard walls on a limestone base, a
        /// corrugated-iron hip roof (style 1: plus a gabled front room with a finial; 2: a street-facing gable over the
        /// whole front), a bullnose verandah on turned timber posts, a chimney, framed windows, a picket fence.</summary>
        static void Bungalow(ComicMesh f, CqLot l, Color wall, Color roof, int style)
        {
            float W = l.W, D = l.Depth, b = .45f, h = b + 3.1f;
            bool boards = wall != RedBrick;
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, W, -2.6f, b, .02f, -D, Limestone);
            f.Surface = boards ? ComicSurface.Wood : ComicSurface.Brick;
            K.LBox(f, l, 0, W, b, h, 0f, -D, wall);
            if (boards)
            {
                f.Surface = ComicSurface.Plain;
                for (float y = b + .35f; y < h - .1f; y += .35f) K.LQuad(f, l, .02f, W - .02f, y, y + .03f, .012f, Color.Lerp(wall, K.R.ink, .22f), false);
            }
            float porchA0 = style == 1 ? W * .42f : .2f, porchA1 = W - .2f;
            // windows and the front door
            float door = style == 1 ? W * .55f : W * .5f;
            f.Surface = ComicSurface.PaintedMetal;
            K.LQuad(f, l, door - .55f, door + .55f, b, b + 2.4f, .02f, K.R.ink);
            f.Surface = ComicSurface.Wood;
            K.LQuad(f, l, door - .44f, door + .44f, b + .04f, b + 2.3f, .035f, K.Hex("7A3B32"));
            foreach (float a in style == 1 ? new[] { W * .8f } : new[] { W * .2f, W * .8f })
                FramedWindow(f, l, a, 1.5f, b + .9f, b + 2.4f, .02f, White, true, true, true);
            // roof
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, -.3f, W + .3f, h - .06f, h + .1f, .35f, -D - .3f, K.R.ink);
            f.Surface = ComicSurface.Metal;
            if (style == 2)
            {
                float ridge = h + W * .3f;
                K.LGable(f, l, -.35f, W + .35f, h + .08f, ridge, .4f, -D - .3f, roof);
                f.Surface = boards ? ComicSurface.Wood : ComicSurface.Plaster;
                K.TriFacing(f, l.P(.3f, h + .1f, .42f), l.P(W - .3f, h + .1f, .42f), l.P(W * .5f, ridge - .4f, .42f), boards ? Color.Lerp(wall, White, .5f) : Cream, new Vector3(-l.S, 0, 0));
                f.Surface = ComicSurface.PaintedMetal;
                f.Beam(l.P(-.35f, h + .1f, .46f), l.P(W * .5f, ridge + .04f, .46f), .14f, White);
                f.Beam(l.P(W + .35f, h + .1f, .46f), l.P(W * .5f, ridge + .04f, .46f), .14f, White);
                K.LQuad(f, l, W * .5f - .4f, W * .5f + .4f, h + .7f, h + 1.4f, .435f, K.R.ink);
            }
            else
            {
                HipRoof(f, l.P(W * .5f, h + .1f, -D * .5f), D + .8f, W + .8f, 2.3f, roof);
                if (style == 1)
                {
                    // the gabled front room pushing forward
                    float g0 = .2f, g1 = W * .4f, gr = h + (g1 - g0) * .42f;
                    f.Surface = boards ? ComicSurface.Wood : ComicSurface.Brick;
                    K.LBox(f, l, g0, g1, b, h, 1.5f, 0f, wall);
                    f.Surface = ComicSurface.Concrete;
                    K.LBox(f, l, g0 - .02f, g1 + .02f, -2.6f, b, 1.52f, 0f, Limestone);
                    FramedWindow(f, l, (g0 + g1) * .5f, 1.5f, b + .9f, b + 2.4f, 1.52f, White, true, true, true);
                    f.Surface = ComicSurface.Metal;
                    K.LGable(f, l, g0 - .3f, g1 + .3f, h, gr, 1.85f, -2f, roof);
                    f.Surface = ComicSurface.Wood;
                    K.TriFacing(f, l.P(g0, h + .02f, 1.87f), l.P(g1, h + .02f, 1.87f), l.P((g0 + g1) * .5f, gr - .3f, 1.87f), boards ? Color.Lerp(wall, White, .55f) : Cream, new Vector3(-l.S, 0, 0));
                    f.Surface = ComicSurface.PaintedMetal;
                    f.Beam(l.P(g0 - .3f, h + .02f, 1.92f), l.P((g0 + g1) * .5f, gr + .04f, 1.92f), .13f, White);
                    f.Beam(l.P(g1 + .3f, h + .02f, 1.92f), l.P((g0 + g1) * .5f, gr + .04f, 1.92f), .13f, White);
                    Rod(f, l.P((g0 + g1) * .5f, gr - .1f, 1.9f), l.P((g0 + g1) * .5f, gr + .7f, 1.9f), .06f, .02f, White, 4, true);
                }
            }
            // bullnose verandah
            f.Surface = ComicSurface.Wood;
            K.LBox(f, l, porchA0, porchA1, b - .12f, b, 1.9f, 0f, TimberDark);
            f.Surface = ComicSurface.Metal;
            Vector3 a0 = l.P(porchA0 - .1f, h - .15f, 0f), a1 = l.P(porchA1 + .1f, h - .15f, 0f), m0 = l.P(porchA0 - .1f, h - .45f, 1.5f), m1 = l.P(porchA1 + .1f, h - .45f, 1.5f), c0 = l.P(porchA0 - .1f, h - .95f, 2.05f), c1 = l.P(porchA1 + .1f, h - .95f, 2.05f);
            K.Face(f, a0, a1, m1, m0, Color.Lerp(roof, White, .12f), Vector3.up);
            K.Face(f, m0, m1, c1, c0, roof, Vector3.up + new Vector3(-l.S, 0, 0));
            K.Face(f, a0 + V(0, -.03f, 0), a1 + V(0, -.03f, 0), c1 + V(0, -.03f, 0), c0 + V(0, -.03f, 0), Color.Lerp(roof, K.R.ink, .4f), Vector3.down);
            f.Surface = ComicSurface.PaintedMetal;
            int posts = Mathf.Max(2, Mathf.RoundToInt((porchA1 - porchA0) / 2.6f) + 1);
            for (int i = 0; i < posts; i++)
            {
                float a = Mathf.Lerp(porchA0 + .1f, porchA1 - .1f, (float)i / (posts - 1));
                K.LBox(f, l, a - .06f, a + .06f, b, h - .95f, 1.95f, 1.83f, White);
            }
            K.LBox(f, l, porchA0, porchA1, h - 1.2f, h - 1.05f, 1.93f, 1.85f, White);
            Chimney(f, l.P(W * .72f, h + .9f, -D * .45f), 1.9f, boards ? RedBrick : RedBrickDark);
            // picket fence and a garden path
            f.Surface = ComicSurface.Wood;
            float fo = l.Line - K.Edge - .75f;
            if (fo > 1.5f)
            {
                K.LBox(f, l, 0f, door - .7f, .55f, .63f, fo, fo - .06f, White);
                K.LBox(f, l, door + .7f, W, .55f, .63f, fo, fo - .06f, White);
                for (float a = .15f; a < W; a += .45f)
                {
                    if (Mathf.Abs(a - door) < .75f) continue;
                    K.LQuad(f, l, a - .06f, a + .06f, 0f, .85f, fo + .01f, White, false);
                }
                f.Surface = ComicSurface.Foliage;
                Bush(f, l.P(W * .2f, 0f, fo - 1.2f), .9f);
                Bush(f, l.P(W * .86f, 0f, fo - 1f), .8f, K.Hex("C85A8A"), K.Hex("E58AAE"));
            }
        }

        /// <summary>A park rotunda: an octagonal timber bandstand with a white balustrade and a red-iron roof with a finial.</summary>
        static void Rotunda(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(p + V(0, .3f, 0), 3.4f, .6f, K.Hex("D9D2C3"), 8);
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 8; i++)
            {
                float a = (i + .5f) * Mathf.PI / 4f, b = (i + 1.5f) * Mathf.PI / 4f;
                Vector3 pa = p + V(Mathf.Cos(a) * 3f, 0, Mathf.Sin(a) * 3f), pb = p + V(Mathf.Cos(b) * 3f, 0, Mathf.Sin(b) * 3f);
                Rod(m, pa + V(0, .6f, 0), pa + V(0, 3.5f, 0), .09f, .08f, White, 5);
                if (i == 5) continue;
                m.Beam(pa + V(0, 1.5f, 0), pb + V(0, 1.5f, 0), .08f, White);
                m.Beam(pa + V(0, 1f, 0), pb + V(0, 1f, 0), .05f, White);
            }
            m.Cylinder(p + V(0, 3.56f, 0), 3.5f, .14f, K.R.ink, 8);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(p + V(0, 4.45f, 0), 3.7f, 1.7f, RedIron, 8, .12f);
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, p + V(0, 5.2f, 0), p + V(0, 6.2f, 0), .1f, .03f, White, 5, true);
        }

        /// <summary>Second rows behind the frontage (no outline, no shadows): hip-roofed houses, gums, the odd Norfolk
        /// pine, palm and jacaranda, from |x| = Edge + near outwards.</summary>
        static void DeepSuburb(CqChunkDraft d, int s, float z0, float z1, int seed, float near)
        {
            ComicMesh m = d.Deep;
            var r = new System.Random(seed);
            Color[] walls = { K.Hex("E6DCC4"), K.Hex("D9C9A8"), K.Hex("EDE9DD"), K.Hex("C9B38E"), K.Hex("D7DDD9") };
            Color[] roofs = { Terracotta, TileBrown, GreyIron, RedIron, K.Hex("8A8F94") };
            for (float z = z0 + 1f; z < z1 - 6f; z += F(r, 12f, 15f))
            {
                float w = F(r, 10f, 13f), len = Mathf.Min(F(r, 9f, 12f), z1 - z - .5f), x = s * (K.Edge + near + w * .5f + F(r, 0f, 5f)), zc = z + len * .5f;
                FarHouse(m, V(x, d.Ground(zc), zc), w, len, F(r, 3.4f, 4.6f), walls[r.Next(walls.Length)], roofs[r.Next(roofs.Length)]);
                if (r.Next(3) == 0) FarHouse(m, V(x + s * F(r, 16f, 20f), d.Ground(zc), zc + F(r, -2f, 2f)), w, len, F(r, 5.5f, 7.2f), walls[r.Next(walls.Length)], roofs[r.Next(roofs.Length)]);
            }
            for (float z = z0 + F(r, 2f, 6f); z < z1 - 1f; z += F(r, 8f, 12f))
            {
                float x = s * (K.Edge + near - F(r, 1f, 6f) + (r.Next(2) == 0 ? F(r, 14f, 24f) : 0f));
                int kind = r.Next(7);
                if (kind == 0) CheapPine(m, V(x, d.Ground(z), z), F(r, 15f, 21f));
                else if (kind == 1) Jacaranda(m, V(x, d.Ground(z), z), F(r, 1.1f, 1.5f), seed + (int)z, false, true);
                else Gum(m, V(x, d.Ground(z), z), F(r, 11f, 17f), seed + (int)(z * 3f), true);
            }
        }

        // ---------------------------------------------------------------- Bayswater obstacles

        // A water-filled road-works barrier: orange with a white one alongside, interlocking ends, ink feet.
        static ComicMesh WaterBarrierMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            Color orange = K.Hex("F07A22");
            for (int i = 0; i < 2; i++)
            {
                float x = -.5f + i * 1f;
                Color c = i == 0 ? orange : White;
                Extrude(m, new[] { V(x - .48f, .06f, -.26f), V(x - .48f, .06f, .26f), V(x - .48f, .5f, .2f), V(x - .48f, .9f, .1f), V(x - .48f, .9f, -.1f), V(x - .48f, .5f, -.2f) }, V(.96f, 0, 0), c);
                m.Box(V(x, .03f, 0), V(.9f, .06f, .56f), K.R.ink);
                m.Box(V(x, .93f, 0), V(.2f, .08f, .16f), K.R.ink);
                K.Face(m, V(x - .3f, .56f, -.215f), V(x + .3f, .56f, -.215f), V(x + .3f, .72f, -.19f), V(x - .3f, .72f, -.19f), i == 0 ? White : orange, Vector3.back, false);
            }
            return m;
        }

        // An airport traveller's suitcase standing on its wheels with the handle up, a tag and a strap.
        static ComicMesh SuitcaseMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            Color body = K.Hex("2A8F8A");
            m.Box(V(0, .46f, 0), V(.52f, .74f, .3f), body);
            foreach (float y in new[] { .28f, .46f, .64f }) m.Box(V(0, y, 0), V(.54f, .04f, .32f), Color.Lerp(body, K.R.ink, .3f));
            m.Box(V(0, .46f, -.16f), V(.08f, .76f, .02f), HazardYellow);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.13f, .13f }) m.Box(V(x, .98f, .1f), V(.03f, .32f, .03f), Stainless);
            m.Box(V(0, 1.14f, .1f), V(.32f, .05f, .06f), K.R.ink);
            m.Surface = ComicSurface.Rubber;
            foreach (float x in new[] { -.2f, .2f }) foreach (float z in new[] { -.1f, .1f }) m.Box(V(x, .045f, z), V(.07f, .09f, .09f), K.R.ink);
            m.Surface = ComicSurface.Fabric;
            m.Box(V(.2f, .72f, -.17f), V(.1f, .16f, .02f), White);
            // a duffel bag leaning on it
            m.Box(V(-.02f, .16f, -.32f), V(.56f, .3f, .28f), K.Hex("C8463A"), Quaternion.Euler(0, 8f, 0));
            m.Box(V(-.02f, .33f, -.32f), V(.3f, .05f, .05f), K.R.ink, Quaternion.Euler(0, 8f, 0));
            return m;
        }

        // A temporary site fence panel: galvanised frame and mesh on concrete feet, an orange banner.
        static ComicMesh TempFenceMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Metal };
            foreach (float x in new[] { -1.15f, 1.15f }) m.Box(V(x, 1.02f, 0), V(.06f, 1.9f, .06f), Galv);
            m.Box(V(0, 1.95f, 0), V(2.36f, .06f, .06f), Galv);
            m.Box(V(0, .22f, 0), V(2.36f, .06f, .06f), Galv);
            m.Surface = ComicSurface.Fabric;
            m.Box(V(0, 1.1f, 0), V(2.24f, 1.66f, .025f), K.Hex("B9C0C6"));
            m.Box(V(0, 1.2f, -.02f), V(2.1f, .8f, .03f), K.Hex("F07A22"));
            m.Box(V(0, 1.2f, -.04f), V(1.4f, .12f, .02f), White);
            m.Surface = ComicSurface.Concrete;
            foreach (float x in new[] { -1.15f, 1.15f }) m.Box(V(x, .08f, 0), V(.3f, .16f, .5f), K.Hex("C9C5BA"));
            return m;
        }

        // A square plaza planter: cream precast with a fluted face (like the piers), a clipped native shrub and a grass tree.
        static ComicMesh PlanterMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Concrete };
            m.Box(V(0, .04f, 0), V(1.28f, .08f, 1.28f), K.R.ink);
            m.Box(V(0, .42f, 0), V(1.2f, .7f, 1.2f), PierCream);
            m.Box(V(0, .8f, 0), V(1.3f, .1f, 1.3f), FluteCream);
            m.Surface = ComicSurface.Plain;
            for (int i = -2; i <= 2; i++)
            {
                K.Face(m, V(i * .22f - .03f, .1f, -.605f), V(i * .22f + .03f, .1f, -.605f), V(i * .22f + .03f, .74f, -.605f), V(i * .22f - .03f, .74f, -.605f), FluteShade, Vector3.back, false);
                K.Face(m, V(.605f, .1f, i * .22f - .03f), V(.605f, .1f, i * .22f + .03f), V(.605f, .74f, i * .22f + .03f), V(.605f, .74f, i * .22f - .03f), FluteShade, Vector3.right, false);
                K.Face(m, V(-.605f, .1f, i * .22f - .03f), V(-.605f, .1f, i * .22f + .03f), V(-.605f, .74f, i * .22f + .03f), V(-.605f, .74f, i * .22f - .03f), FluteShade, Vector3.left, false);
            }
            m.Surface = ComicSurface.Foliage;
            m.Box(V(0, .84f, 0), V(1.06f, .06f, 1.06f), Mulch);
            Bush(m, V(-.15f, .82f, .1f), .75f);
            Sedge(m, V(.3f, .85f, -.25f), .5f, 3);
            return m;
        }

        // ---------------------------------------------------------------- Bayswater far scenery

        static ComicMesh BayFar(float grade) => EastFar(grade, 270, 52f, 210f, 330f, K.Hex("C9DCE8"), K.Hex("7FA652"), false, (m, r) =>
        {
            Color haze = K.Hex("C9DCE8");
            // the gentle hill of Bayswater behind the station (houses climbing it, Norfolk pines on top)
            Ridge(m, -1, 150f, 150f, -220f, 820f, -.3f, z => 13f + 4f * Mathf.Sin(z * .012f + .6f), K.Hex("8FA567"), K.Hex("7C9A5A"), K.Hex("8CA76A"), haze, .25f);
            for (float z = -200f; z < 800f; z += F(r, 20f, 34f))
                CheapPine(m, V(-F(r, 200f, 250f), 13f, z), F(r, 16f, 22f), Haze(K.Hex("2C5A36"), 240f, haze));
            for (float z = -200f; z < 800f; z += F(r, 16f, 24f))
                FarHouse(m, V(-F(r, 165f, 195f), F(r, 4f, 9f), z), F(r, 10f, 14f), F(r, 9f, 12f), F(r, 4f, 6f), Haze(K.Hex("EDE9DD"), 190f, haze), Haze(r.Next(2) == 0 ? Terracotta : GreyIron, 190f, haze));
            // riverside: the white hexagonal-roofed gazebo on its jetty at Riverside Gardens
            m.Surface = ComicSurface.Wood;
            m.Box(V(224f, -.6f, 240f), V(30f, .5f, 3f), Haze(Timber, 220f, haze));
            m.Cylinder(V(238f, 1.2f, 240f), 4f, .3f, Haze(Timber, 230f, haze), 6);
            m.Cylinder(V(238f, 5.2f, 240f), 5f, 2f, Haze(White, 230f, haze), 6, .1f);
            for (int i = 0; i < 6; i++) m.Box(V(238f + Mathf.Cos(i * 1.047f) * 3.6f, 2.8f, 240f + Mathf.Sin(i * 1.047f) * 3.6f), V(.3f, 3f, .3f), Haze(White, 230f, haze));
        });
    }
}
