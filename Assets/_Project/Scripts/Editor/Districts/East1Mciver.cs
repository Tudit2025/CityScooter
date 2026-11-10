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
    /// <summary>MCIVER (order 210): Wellington St beside the railway, east of the city. See East1Builder.</summary>
    public static partial class East1Builder
    {
        public const string MciverName = "Mciver";
        public static readonly string[] MciverChunkIds = { "Hospital", "Heritage", "Station", "Square", "Works" };
        /// <summary>Wellington St still runs gently downhill from the city: -2 m per 40 m chunk.</summary>
        public const float MciverDrop = -2f;
        // The road works jump (Ramp + road-wide RoadGap: the trench where the Moore St level crossing was). For gameplay:
        public const float WorksRampStart = 8f, WorksRise = 6f, WorksHeight = 1.4f, WorksTop = 3f, WorksGap = 5f;
        // McIver has four tracks: metres beyond the left sidewalk edge.
        static readonly float[] MTracks = { 7f, 11.5f, 20.5f, 25f };

        static void BuildMciver(TrackTheme theme, bool atlas)
        {
            string n = MciverName, patterns = DataFolder + "/mciver";
            K.Begin(Meshes(n), Prefabs(n), DataFolder, patterns);
            Obstacle bin = K.SaveObstacle(Pf(n, "OBS_Mciver_Bin"), "OBS_Mciver_Bin", SteelBinMesh(), new Vector3(.7f, 1.1f, .7f), ObstacleKind.Thing, true, true, 1f);
            Obstacle barrier = K.SaveObstacle(Pf(n, "OBS_Mciver_WaterBarrier"), "OBS_Mciver_WaterBarrier", WaterBarrierMesh(), new Vector3(2f, .9f, .5f), ObstacleKind.Thing, true, false, 1f);
            Obstacle planter = K.SaveObstacle(Pf(n, "OBS_Mciver_Planter"), "OBS_Mciver_Planter", BlockPlanterMesh(), new Vector3(1.6f, 1.5f, 1.6f), ObstacleKind.Thing, false, false, .7f);
            Obstacle validator = K.SaveObstacle(Pf(n, "OBS_Mciver_RoadSign"), "OBS_Mciver_RoadSign", RoadWorkSignMesh(), new Vector3(1.1f, 1.5f, .8f), ObstacleKind.Thing, false, true, .8f);
            GameObject ibis = SaveAnimal(Pf(n, "ANM_Mciver_Ibis"), "ANM_Mciver_Ibis", IbisMesh(), new Vector3(0, .45f, 0), new Vector3(.45f, .9f, .9f), .5f, true);
            GameObject pigeon = SaveAnimal(Pf(n, "ANM_Mciver_Pigeon"), "ANM_Mciver_Pigeon", PigeonMesh(), new Vector3(0, .15f, 0), new Vector3(.3f, .3f, .4f), .3f, true);
            Ramp plate = K.SaveRamp(Pf(n, "RMP_Mciver_RoadPlate"), "RMP_Mciver_RoadPlate", 3f, 5f, 1.3f, 1.5f, 1f, .5f, K.Hex("8E959B"), K.Hex("4E545C"), true);
            GameObject ambulanceOncoming = K.SaveVehicle(Pf(n, "VEH_Mciver_AmbulanceOncoming"), "VEH_Mciver_Ambulance", AmbulanceMesh(), new Vector3(2.2f, 2.7f, 6.2f), -12f, 0f);
            GameObject ambulanceAhead = K.SaveVehicle(Pf(n, "VEH_Mciver_AmbulanceAhead"), "VEH_Mciver_Ambulance", null, new Vector3(2.2f, 2.7f, 6.2f), 0f, .35f);

            K.WriteChunk(MEntry(), ChunkPath(n, "Entry"));
            K.WriteChunk(MHospital(), ChunkPath(n, "Hospital"));
            K.WriteChunk(MHeritage(), ChunkPath(n, "Heritage"));
            K.WriteChunk(MStation(), ChunkPath(n, "Station"));
            K.WriteChunk(MSquare(), ChunkPath(n, "Square"));
            K.WriteChunk(MWorks(), ChunkPath(n, "Works"));
            K.WriteChunk(MExit(), ChunkPath(n, "Exit"));
            GameObject far = K.SaveFarScenery(Pf(n, "FAR_Mciver_EastPerth"), "FAR_Mciver_EastPerth", MciverFar(-MciverDrop / 40f));

            var own = new List<HazardPattern>
            {
                K.Pattern(patterns, "HZ_Mciver_AmbulanceOncoming", 100, 1, 1f, false, 55, (ambulanceOncoming, 2f, 55f, 180f)),
                K.Pattern(patterns, "HZ_Mciver_AmbulanceAhead", 90, 1, .8f, false, 45, (ambulanceAhead, -2f, 45f, 0f)),
                K.Pattern(patterns, "HZ_Mciver_Ibises", 45, 0, 1f, true, 30, (ibis, -2.6f, 22f, 0f), (ibis, -.8f, 25f, 0f), (ibis, 2.4f, 29f, 0f)),
                K.Pattern(patterns, "HZ_Mciver_Barriers", 40, 0, .8f, true, 30, (barrier.gameObject, -2.6f, 20f, 0f), (barrier.gameObject, 0f, 20f, 0f)),
            };
            own.AddRange(ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_OncomingPair", "HZ_Crossing", "HZ_SidewalkStroll", "HZ_PoliceStop", "HZ_RoadworksShort"));
            var people = new List<GameObject>(K.People(theme, "Office", "Office", "Student", "Shopper", "Elder", "Elder", "Courier", "Jogger", "DogWalker", "Kid", "Tourist"));
            people.Add(ibis); people.Add(ibis); people.Add(pigeon); people.Add(pigeon);
            WriteDistrict(n, "mciver", "MCIVER", 210, new Vector2(360f, 440f), MciverChunkIds,
                new[] { bin, barrier, planter, validator }, new[] { barrier, bin }, own.ToArray(), people.ToArray(), new[] { plate }, 1f, far,
                new Look
                {
                    // inner-city afternoon: warm brick, a slightly dusty pale blue distance
                    Fog = new Color(.80f, .86f, .92f), SkyTint = new Color(1f, .99f, .97f), AmbSky = new Color(.68f, .82f, .96f),
                    AmbEquator = new Color(.88f, .85f, .78f), AmbGround = new Color(.66f, .58f, .50f), Sun = new Color(1f, .95f, .86f), SunIntensity = 1.4f,
                });
            Debug.Log("[Districts/Mciver] " + string.Join("; ", K.Report));
            if (atlas)
                K.RenderAtlas("art-src/districts/mciver/props-atlas.png", new List<(string, GameObject, ComicMesh)>
                {
                    ("MoretonBayFig", null, Make(m => MoretonBayFig(m, Vector3.zero, 1f, 3))),
                    ("Gum", null, Make(m => Gum(m, Vector3.zero, 11f, 2))),
                    ("Jacaranda", null, Make(m => JacarandaTree(m, Vector3.zero, 8f, 5))),
                    ("KirkmanHouse", null, Make(m => { var d = new CqChunkDraft("a", 0f); KirkmanHouse(d, new CqLot(-1, -8f, 8f, 6f, 11f, 0f)); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("AdminBuilding", null, Make(m => { var d = new CqChunkDraft("a", 0f); AdminBuilding(d, new CqLot(-1, -6.5f, 6.5f, 6f, 12f, 0f)); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("PerthMint", null, Make(m => { var d = new CqChunkDraft("a", 0f); PerthMint(d, new CqLot(-1, -9f, 9f, 6f, 12f, 0f)); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("WardBlock", null, Make(m => { var d = new CqChunkDraft("a", 0f); WardBlock(d, new CqLot(-1, -16f, 16f, 6f, 14f, 0f)); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("NorthBlock", null, Make(m => { var d = new CqChunkDraft("a", 0f); NorthBlock(d, new CqLot(1, -18f, 18f, 6f, 16f, 0f)); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("ShopTerrace", null, Make(m => { var d = new CqChunkDraft("a", 0f); ShopTerrace(d, new CqLot(-1, -6.5f, 6.5f, 6f, 12f, 0f), 2, BrickRed, TrimCream, K.Hex("2A3E6E"), 1, true); m.Add(d.Frontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("BarrelShelter", null, Make(m => { var d = new CqChunkDraft("a", 0f); Platform(d, 1f, 5f, -6f, 6f, false, true); BarrelShelter(d, 3f, -4f, 8f, 1, "1"); PlatformSign(d, 3f, 5f, "MCIVER"); m.Add(d.SlopedFrontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("ASeries", null, Make(m => Emu(m, 0f, -19f, 2, ASeries))),
                    ("Portal", null, Make(m => { var d = new CqChunkDraft("a", 0f); Portal(d, 0f, 3f, 16f, 7f, 11.5f); m.Add(d.SlopedFrontage, Vector3.zero, Quaternion.identity, Vector3.one); })),
                    ("PlaygroundShade", null, Make(m => SquarePlayground(m, Vector3.zero))),
                    ("MiaMias", null, Make(m => MiaMias(m, Vector3.zero))),
                    ("Excavator", null, Make(m => Excavator(m, Vector3.zero, 0f))),
                    ("BoomGate", null, Make(m => BoomGate(m, Vector3.zero, 1))),
                    ("StreetLight", null, Make(m => StreetLight(m, Vector3.zero, 1))),
                    ("Bench", null, Make(m => Bench(m, Vector3.zero, 180f))),
                    ("Bin", bin.gameObject, null),
                    ("WaterBarrier", barrier.gameObject, null),
                    ("Planter", planter.gameObject, null),
                    ("RoadSign", validator.gameObject, null),
                    ("Ibis", ibis, null),
                    ("Pigeon", pigeon, null),
                    ("Ambulance", ambulanceAhead, null),
                    ("RoadPlate", plate.gameObject, null),
                });
        }

        // ---------------------------------------------------------------- McIver chunks

        static void MStreet(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f) =>
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, Paving, KerbCap, true, skipFrom, skipTo);

        /// <summary>The four tracks along the left side with their fence, wires and (at z = 20) a portal gantry.</summary>
        static void MRail(CqChunkDraft d, float z0, float z1, bool fence = true, bool yard = true)
        {
            if (fence) Palisade(d, -1, z0, z1, .45f);
            Ballast(d, z0, z1, 0f, 32f);
            if (yard) Ground(d, -1, z0, z1, 32f, 75f, K.Hex("8E8A80"), ComicSurface.Concrete, .12f);
            foreach (float t in MTracks) { Track(d, t, z0, z1); Wires(d, t, z0, z1, 20f, 40f); }
            if (z0 <= 20f && z1 >= 20f) Portal(d, 20f, 3f, 29f, MTracks);
            SideWall(d, -1, z0, z1, 30f, 6f);
        }

        static void MLights(CqChunkDraft d, params float[] zs)
        {
            for (int i = 0; i < zs.Length; i++)
            {
                int s = i % 2 == 0 ? 1 : -1;
                Vector3 p = V(s * (K.Half + .55f), d.Ground(zs[i]), zs[i]);
                StreetLight(d.Props, p, s);
                K.Thin(d.Collisions, "StreetLight", p, 8f, .3f);
            }
        }

        static CqChunkDraft MEntry()
        {
            // Calm at z = 0, then the railway comes out from under a brick road bridge on the left and the first old
            // brick shopfronts and plane trees stand on the right.
            var d = new CqChunkDraft("Mciver_Entry", MciverDrop);
            MStreet(d);
            CalmEdge(d, -1, 0f, 14f, 1, BrickRed, TrimCream);
            CalmEdge(d, 1, 0f, 14f, 2, BrickRed, TrimCream);
            RailBridgeWall(d, 14f, true, MTracks);
            MRail(d, 14f, 40f);
            MBackdrop(d, 14f, 40f, 3);
            Ground(d, 1, 14f, 40f, 0f, 60f, Paving, ComicSurface.Paving);
            ShopTerrace(d, new CqLot(1, 15f, 27f, K.Edge + 1.5f, 12f, d.Base(21f)), 2, BrickRed, TrimCream, Navy, 1, true);
            ShopTerrace(d, new CqLot(1, 27.5f, 39.5f, K.Edge + 1.5f, 12f, d.Base(33.5f)), 3, K.Hex("E6D7B5"), K.Hex("B98A5A"), K.Hex("8A3A28"), 2, false);
            DeepBlocks(d, 1, 14f, 40f, 16f, 5, 14f, 30f);
            K.PlaneTree(d.Props, d.Collisions, V(5.6f, d.Ground(20f), 20f), .9f);
            MLights(d, 30f, 18f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MExit()
        {
            // The railway and a last brick terrace first, then the tracks go under a road bridge and it calms down.
            var d = new CqChunkDraft("Mciver_Exit", MciverDrop);
            MStreet(d);
            MRail(d, 0f, 26f);
            MBackdrop(d, 0f, 26f, 9);
            RailBridgeWall(d, 26f, false, MTracks);
            Ground(d, 1, 0f, 26f, 0f, 60f, Paving, ComicSurface.Paving);
            ShopTerrace(d, new CqLot(1, .5f, 12.5f, K.Edge + 1.5f, 12f, d.Base(6.5f)), 2, K.Hex("D9B98A"), TrimCream, SignGreen, 2, false);
            ShopTerrace(d, new CqLot(1, 13f, 25f, K.Edge + 1.5f, 12f, d.Base(19f)), 2, BrickRed, TrimCream, Navy, 0, true);
            DeepBlocks(d, 1, 0f, 26f, 16f, 6, 12f, 24f);
            CalmEdge(d, -1, 26f, 40f, 3, BrickRed, TrimCream);
            CalmEdge(d, 1, 26f, 40f, 4, BrickRed, TrimCream);
            K.PlaneTree(d.Props, d.Collisions, V(5.6f, d.Ground(14f), 14f), .9f);
            MLights(d, 8f, 22f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MHospital()
        {
            // Royal Perth Hospital on both sides of Wellington St: North Block (precast bands, blue ribbon glazing) on the
            // railway side, the tall tan brick ward block with its white balconies opposite, and the enclosed link bridge.
            var d = new CqChunkDraft("Mciver_Hospital", MciverDrop);
            MStreet(d);
            Ground(d, -1, 0f, 40f, 0f, 60f, Paving, ComicSurface.Paving);
            Ground(d, 1, 0f, 40f, 0f, 60f, Paving, ComicSurface.Paving);
            NorthBlock(d, new CqLot(-1, 1.5f, 38.5f, K.Edge + 2.2f, 16f, d.Base(20f)));
            WardBlock(d, new CqLot(1, 4f, 36f, K.Edge + 3.2f, 14f, d.Base(20f)));
            LinkBridge(d, 18f, 22f);
            // forecourt planting and a bus shelter under the bridge side
            Shrubs(d.Props, V(K.Edge + 1.2f, d.Ground(6f), 5f), 8f, 4);
            Shrubs(d.Props, V(K.Edge + 1.2f, d.Ground(30f), 26f), 9f, 7);
            K.Palm(d.Props, null, V(K.Edge + 1.6f, d.Ground(15f), 15f), 7.5f, 3);
            K.PropFromKit(d.Props, d.Collisions, "BusShelter", V(6.9f, d.Ground(29f), 29f), -90f);
            K.PlaneTree(d.Props, d.Collisions, V(-5.6f, d.Ground(9f), 9f), .9f);
            K.PlaneTree(d.Props, d.Collisions, V(-5.6f, d.Ground(33f), 33f), .95f);
            MLights(d, 12f, 26f);
            DeepBlocks(d, -1, 0f, 40f, 34f, 11, 40f, 90f);
            DeepBlocks(d, 1, 0f, 40f, 24f, 12, 18f, 36f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MHeritage()
        {
            // The hospital's heritage precinct behind its cream piers and white railings: Kirkman House with its arcade and
            // verandah, the huge Moreton Bay fig, the red brick Administration Building. The railway on the left.
            var d = new CqChunkDraft("Mciver_Heritage", MciverDrop);
            MStreet(d);
            MRail(d, 0f, 40f);
            MBackdrop(d, 0f, 40f, 21);
            Trackside(d, 2.6f, 6f, 31f);
            Ground(d, 1, 0f, 40f, 0f, 60f, Lawn, ComicSurface.Foliage);
            Patch(d, 1, 0f, 40f, 0f, 1.2f, Paving, K.Kerb + .03f, ComicSurface.Paving);
            PierFence(d, 1, 0f, 40f, .6f, 19f, 23f);
            Patch(d, 1, 19f, 23f, 1.2f, 7f, PavingWarm, K.Kerb + .035f, ComicSurface.Paving);
            KirkmanHouse(d, new CqLot(1, 1.5f, 17.5f, K.Edge + 7.5f, 11f, d.Base(9.5f)));
            MoretonBayFig(d.Props, V(K.Edge + 6.5f, d.Ground(24.5f), 24.5f), 1.05f, 3);
            AdminBuilding(d, new CqLot(1, 27f, 39.5f, K.Edge + 12f, 12f, d.Base(33f)));
            Shrubs(d.Props, V(K.Edge + 2.2f, d.Ground(6f), 3f), 12f, 9, Hedge);
            Bench(d.Props, V(K.Edge + 3.2f, d.Ground(30f), 30f), 90f);
            PathLight(d.Props, V(K.Edge + 2f, d.Ground(21f), 17.6f));
            NameBoard(d.Props, V(K.Edge + 2.4f, d.Ground(1.2f), 1.6f), "ROYAL PERTH HOSPITAL", .075f, Navy, Gold, 1.5f);
            DeepBlocks(d, 1, 0f, 40f, 28f, 22, 20f, 38f);
            K.PlaneTree(d.Props, d.Collisions, V(-5.6f, d.Ground(28f), 28f), .9f);
            MLights(d, 10f, 14f, 34f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MStation()
        {
            // McIver station: two side platforms and an island, barrel-vault shelters, an A-series set at platform 1; old
            // brick shopfronts with street awnings across the road.
            var d = new CqChunkDraft("Mciver_Station", MciverDrop);
            MStreet(d);
            MRail(d, 0f, 40f);
            MBackdrop(d, 0f, 40f, 31);
            Platform(d, 1f, 5.1f, 0f, 40f, false, true);
            Platform(d, 13.4f, 18.6f, 0f, 40f, true, true);
            Platform(d, 26.9f, 31f, 0f, 40f, true, false);
            BarrelShelter(d, 2.9f, 9f, 9f, 1, "1");
            BarrelShelter(d, 16f, 23f, 9f, 1, "2");
            BarrelShelter(d, 29f, 6f, 9f, -1, "4");
            PlatformSign(d, 3.2f, 3.5f, "MCIVER", .11f);
            PlatformSign(d, 16f, 5f, "MCIVER", .1f);
            Emu(d.SlopedFrontage, MTracks[0], .8f, 2, ASeries);
            ValidatorPoles(d, 3f, 22f);
            Underpass(d, 1.3f, 3.4f, 29f, 39f);
            Ground(d, 1, 0f, 40f, 0f, 60f, Paving, ComicSurface.Paving);
            ShopTerrace(d, new CqLot(1, .5f, 12.5f, K.Edge + 1.5f, 12f, d.Base(6.5f)), 2, BrickRed, TrimCream, K.Hex("2F5A3A"), 1, true);
            ShopTerrace(d, new CqLot(1, 13f, 27f, K.Edge + 1.5f, 13f, d.Base(20f)), 3, K.Hex("E9DCC0"), K.Hex("A8452E"), Navy, 2, false);
            ShopTerrace(d, new CqLot(1, 27.5f, 39.5f, K.Edge + 1.5f, 12f, d.Base(33.5f)), 2, BrickTan, TrimCream, K.Hex("8A3A28"), 0, true);
            DeepBlocks(d, 1, 0f, 40f, 17f, 32, 14f, 30f);
            K.PlaneTree(d.Props, d.Collisions, V(5.6f, d.Ground(13f), 13f), .85f);
            K.PropFromKit(d.Props, d.Collisions, "BikeRack", V(-6.6f, d.Ground(31f), 31f), 90f);
            MLights(d, 26f, 20f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MSquare()
        {
            // Wellington Square (Moort-ak Waadiny): the big lawn, Moreton Bay figs, curving paths, the playground under its
            // net of coloured discs, floodlight masts, the Mia Mias in the far corner (background, kept simple).
            var d = new CqChunkDraft("Mciver_Square", MciverDrop);
            MStreet(d);
            MRail(d, 0f, 40f);
            MBackdrop(d, 0f, 40f, 41);
            Emu(d.SlopedFrontage, MTracks[2], .8f, 2, ASeries);
            Trackside(d, 2.6f, 12f, 33f);
            Ground(d, 1, 0f, 40f, 0f, 90f, Lawn, ComicSurface.Foliage);
            LowWall(d, 1, 0f, 13f, .4f, Limestone, LimestoneCap);
            LowWall(d, 1, 17f, 40f, .4f, Limestone, LimestoneCap);
            // lighter mown bands, a curving asphalt path with pale edging
            for (float z = 2f; z < 38f; z += 9f) Patch(d, 1, z, z + 4f, 1.2f, 46f, LawnLight, K.Kerb + .03f);
            SquarePath(d);
            MoretonBayFig(d.Props, V(K.Edge + 7f, d.Ground(6f), 6f), 1f, 5);
            MoretonBayFig(d.Props, V(K.Edge + 24f, d.Ground(37f), 37f), 1.15f, 8);
            SquarePlayground(d.Landmark, V(K.Edge + 15f, d.Ground(24f), 24f));
            MiaMias(d.Landmark, V(K.Edge + 44f, d.Ground(30f), 30f));
            YardLight(d.Props, V(K.Edge + 31f, d.Ground(4f), 4f), 18f);
            Bench(d.Props, V(K.Edge + 3.4f, d.Ground(21f), 21f), 90f);
            PathLight(d.Props, V(K.Edge + 2.2f, d.Ground(14f), 12.4f));
            PathLight(d.Props, V(K.Edge + 2.2f, d.Ground(27f), 27f));
            NameBoard(d.Props, V(K.Edge + 2.6f, d.Ground(1.5f), 1.8f), "WELLINGTON SQUARE", .08f, K.Hex("20262C"), SafetyOrange, 1.4f);
            for (int i = 0; i < 5; i++) GrassTuft(d.Props, V(K.Edge + 1.4f + (i % 2) * .7f, d.Ground(15f), 14.2f + i * .5f), 1f, i);
            var r = new System.Random(44);
            for (float z = 3f; z < 40f; z += 8f) CheapFig(d.Deep, V(K.Edge + F(r, 50f, 62f), d.Ground(z) - .3f, z), F(r, .8f, 1.1f));
            DeepBlocks(d, 1, 0f, 40f, 70f, 42, 14f, 30f);
            K.PlaneTree(d.Props, d.Collisions, V(-5.6f, d.Ground(24f), 24f), .9f);
            MLights(d, 9f, 31f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MWorks()
        {
            // Road works where the Moore St level crossing was removed: a steel road-plate ramp over the open trench
            // (WHOOPS), the folded boom gates still standing by the tracks; the Perth Mint behind its limestone wall.
            var d = new CqChunkDraft("Mciver_Works", MciverDrop);
            float ramp = WorksRampStart, lip = ramp + WorksRise + WorksTop, far = lip + WorksGap;
            MStreet(d, lip, far);
            MRail(d, 0f, 40f, false);
            Palisade(d, -1, 0f, lip - 2f, .45f);
            Palisade(d, -1, far + 2f, 40f, .45f);
            MBackdrop(d, 0f, 40f, 51);
            Ground(d, 1, 0f, lip, 0f, 60f, K.Hex("B9A27E"), ComicSurface.Concrete);
            Ground(d, 1, far, 40f, 0f, 60f, Lawn, ComicSurface.Foliage);
            Trench(d, lip, far);
            var spec = new CqRampSpec { Name = "RoadPlateRamp", X = 0f, Z = ramp, Width = 2f * K.Edge, Rise = WorksRise, Height = WorksHeight, Top = WorksTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            d.Gaps.Add(new CqGapSpec { Name = "CrossingTrench", Z = lip, Length = WorksGap, Width = 0f, Word = "WHOOPS", Water = false });
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, K.Hex("8E959B"), K.Hex("4E545C"), true, -2.4f);
            // plate joints and bolt-down strips so it reads as steel road plates on a scaffold frame
            deck.Surface = ComicSurface.Metal;
            for (float x = -6f; x <= 6.01f; x += 4f) deck.Box(V(x, WorksHeight * .5f + .03f, ramp + WorksRise * .5f), V(.12f, .05f, Mathf.Sqrt(WorksRise * WorksRise + WorksHeight * WorksHeight)), K.Hex("5A6068"), Quaternion.LookRotation(V(0, WorksHeight, WorksRise)));
            for (int s = -1; s <= 1; s += 2)
            {
                deck.Surface = ComicSurface.PaintedMetal;
                for (float z = ramp + 2f; z <= lip; z += 2.3f)
                    deck.Box(V(s * (K.Edge + .1f), WorksHeight * Mathf.Clamp01((z - ramp) / WorksRise) * .5f, z), V(.12f, WorksHeight * Mathf.Clamp01((z - ramp) / WorksRise) + .02f, .12f), SafetyOrange);
                deck.Beam(V(s * (K.Edge + .1f), 1f, ramp), V(s * (K.Edge + .1f), WorksHeight + 1f, ramp + WorksRise), .08f, SafetyOrange);
                deck.Beam(V(s * (K.Edge + .1f), WorksHeight + 1f, ramp + WorksRise), V(s * (K.Edge + .1f), WorksHeight + 1f, lip), .08f, SafetyOrange);
                for (float z = ramp; z <= lip + .01f; z += 3f)
                    deck.Box(V(s * (K.Edge + .1f), WorksHeight * Mathf.Clamp01((z - ramp) / WorksRise) + .5f, z), V(.08f, 1f, .08f), SafetyOrange);
            }
            deck.Surface = ComicSurface.Plain;
            for (int i = 0; i < 16; i++)
            {
                float xa = -K.Edge + i * 1f, t0 = .02f, t1 = .12f;
                K.Face(deck, V(xa, WorksHeight * t0 + .012f, ramp + WorksRise * t0), V(xa + 1f, WorksHeight * t0 + .012f, ramp + WorksRise * t0), V(xa + 1f, WorksHeight * t1 + .012f, ramp + WorksRise * t1), V(xa, WorksHeight * t1 + .012f, ramp + WorksRise * t1), i % 2 == 0 ? K.R.yellow : Ink, Vector3.up, false);
            }
            for (int i = 0; i < 3; i++)
                foreach (float x in new[] { -2.6f, 0f, 2.6f })
                {
                    float t = .3f + i * .2f, y = WorksHeight * t + .014f, z = ramp + WorksRise * t, y2 = WorksHeight * (t + .09f) + .014f, z2 = ramp + WorksRise * (t + .09f);
                    K.Face(deck, V(x - .7f, y, z), V(x - .45f, y, z), V(x, y2, z2), V(x - .12f, y2, z2 + .25f), White, Vector3.up, false);
                    K.Face(deck, V(x + .7f, y, z), V(x + .45f, y, z), V(x, y2, z2), V(x + .12f, y2, z2 + .25f), White, Vector3.up, false);
                }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            d.KeepOut.Add(KeepOutBox(d, -1, ramp, far)); d.KeepOut.Add(KeepOutBox(d, 1, ramp, far));
            // the works site on the right before the trench
            Excavator(d.Props, V(K.Edge + 4.6f, d.Ground(13f), 13f), -55f);
            SiteOffice(d.Props, V(K.Edge + 6f, d.Ground(4f), 4f));
            for (float z = 1f; z < 8f; z += 2.3f) WaterBarrierArt(d.Props, V(K.Edge + .6f, d.Ground(z), z), 90f, (int)z % 2 == 0);
            RoadWorkBoard(d.Props, V(K.Edge + .9f, d.Ground(.6f), .6f));
            SideWall(d, 1, 0f, lip, 14f);
            // the old crossing on the railway side
            BoomGate(d.Props, V(-(K.Edge + .9f), d.Ground(lip - 1f), lip - 1f), 1);
            BoomGate(d.Props, V(-(K.Edge + .9f), d.Ground(far + 1f), far + 1f), -1);
            // after the trench: the Perth Mint
            PerthMint(d, new CqLot(1, 23f, 39.6f, K.Edge + 8.5f, 12f, d.Base(31f)));
            MintForecourt(d, 23f, 39.6f);
            DeepBlocks(d, 1, 0f, 40f, 26f, 52, 14f, 28f);
            d.GiftSlots.Add(new Vector2(0f, 30f));
            d.Slot(-2.6f, 35f); d.Slot(2.6f, 35f);
            return d;
        }

        // ---------------------------------------------------------------- McIver parts

        /// <summary>Where the railway starts or ends in an entry / exit chunk: a brick road bridge across the corridor with
        /// dark openings over the tracks, a cream coping and a steel parapet, trees on the embankment behind it.</summary>
        static void RailBridgeWall(CqChunkDraft d, float z, bool tracksAhead, float[] tracks, float width = 34f)
        {
            ComicMesh m = d.SlopedFrontage;
            float t = tracksAhead ? -1f : 1f, zb = z + t * 6f;          // the bridge body lies on the side away from the tracks
            m.Surface = ComicSurface.Brick;
            K.MinMaxBox(m, V(LX(.1f), -1f, z), V(LX(width), 7.2f, zb), BrickDark);
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(LX(-.02f), 7.2f, z - t * .15f), V(LX(width + .1f), 7.55f, zb + t * .1f), TrimCream);
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, V(LX(.1f), 8.5f, z - t * .05f), V(LX(width), 8.6f, z + t * .1f), SteelDark);
            for (float u = .3f; u < width; u += 2.8f) m.Box(V(LX(u), 8.05f, z + t * .03f), V(.1f, 1f, .1f), SteelDark);
            Vector3 n = V(0, 0, -t);
            float zf = z - t * .02f;
            foreach (float u in tracks)
            {
                float x = LX(u);
                m.Surface = ComicSurface.Plain;
                K.Face(m, V(x - 2f, .1f, zf), V(x + 2f, .1f, zf), V(x + 2f, 5.6f, zf), V(x - 2f, 5.6f, zf), K.Hex("14181D"), n);
                K.Face(m, V(x - 2f, 5.6f, zf), V(x + 2f, 5.6f, zf), V(x + 1.3f, 6.4f, zf), V(x - 1.3f, 6.4f, zf), K.Hex("14181D"), n, false);
                m.Surface = ComicSurface.Concrete;
                m.Box(V(x, 6.62f, zf), V(3.2f, .22f, .2f), TrimCream);
                m.Box(V(x - 2.15f, 2.9f, zf), V(.3f, 5.6f, .2f), TrimCream);
                m.Box(V(x + 2.15f, 2.9f, zf), V(.3f, 5.6f, .2f), TrimCream);
            }
            // the side along the road: a steel parapet on top, two blind arches in the brick, a cream plinth band
            float xr = LX(.1f), za = Mathf.Min(z, zb), zc2 = (z + zb) * .5f;
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(xr, 8.55f, zc2), V(.1f, .1f, 6f), SteelDark);
            for (float dz = .3f; dz < 6f; dz += 1.8f) m.Box(V(xr, 8.05f, za + dz), V(.1f, 1f, .1f), SteelDark);
            m.Surface = ComicSurface.Concrete;
            m.Box(V(xr + .04f, .55f, zc2), V(.12f, .7f, 6f), TrimCream);
            foreach (float dz in new[] { 1.6f, 4.4f })
            {
                float zm = za + dz;
                m.Surface = ComicSurface.Plain;
                K.Face(m, V(xr + .012f, .9f, zm - 1f), V(xr + .012f, .9f, zm + 1f), V(xr + .012f, 4.4f, zm + 1f), V(xr + .012f, 4.4f, zm - 1f), K.Hex("6E2E20"), Vector3.right);
                K.Face(m, V(xr + .012f, 4.4f, zm - 1f), V(xr + .012f, 4.4f, zm + 1f), V(xr + .012f, 5.2f, zm + .5f), V(xr + .012f, 5.2f, zm - .5f), K.Hex("6E2E20"), Vector3.right, false);
                m.Surface = ComicSurface.Concrete;
                m.Beam(V(xr + .07f, 4.35f, zm - 1.05f), V(xr + .07f, 5.25f, zm - .5f), .14f, TrimCream);
                m.Beam(V(xr + .07f, 4.35f, zm + 1.05f), V(xr + .07f, 5.25f, zm + .5f), .14f, TrimCream);
                m.Box(V(xr + .07f, 5.27f, zm), V(.14f, .14f, 1.1f), TrimCream);
            }
            // a bus crossing the bridge: only its top shows over the parapet
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(LX(9f), 9.6f, zc2), V(10f, 2.9f, 2.5f), White);
            m.Surface = ComicSurface.Glass;
            m.Box(V(LX(9f), 10f, zc2), V(9.4f, 1.1f, 2.54f), GlassDark);
            m.Surface = ComicSurface.Plain;
            m.Box(V(LX(9f), 8.9f, zc2), V(10.02f, .5f, 2.52f), SignGreen);
            d.Walls.Add(new Bounds(new Vector3(LX(width * .5f), d.Y(z) + 3f, (z + zb) * .5f), new Vector3(width, 9f, 6f)));
        }

        /// <summary>Trackside clutter between the fence and the first track: relay cabinets, a signal, weeds.</summary>
        static void Trackside(CqChunkDraft d, float u, params float[] zs)
        {
            ComicMesh m = d.SlopedFrontage;
            for (int i = 0; i < zs.Length; i++)
            {
                float z = zs[i], x = LX(u);
                if (i % 2 == 0)
                {
                    m.Surface = ComicSurface.Metal;
                    m.Box(V(x, .85f, z), V(.8f, 1.3f, 1.6f), K.Hex("B9C0C6"));
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Box(V(x, .16f, z), V(1f, .14f, 1.8f), ConcreteDark);
                    m.Box(V(x + .41f, .9f, z), V(.02f, 1.1f, .04f), SteelDark);
                    m.Box(V(x, 1.53f, z), V(.9f, .06f, 1.7f), SteelDark);
                }
                else
                {
                    // a colour-light signal on a post with a ladder
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Cylinder(V(x, 2.4f, z), .09f, 4.6f, MastGrey, 8);
                    m.Box(V(x, 4.9f, z), V(.5f, 1.3f, .3f), K.Hex("20262C"));
                    m.Box(V(x, 5.6f, z - .08f), V(.6f, .08f, .5f), K.Hex("20262C"));
                    m.Surface = ComicSurface.Glass;
                    m.Box(V(x, 5.25f, z - .16f), V(.22f, .22f, .04f), K.Hex("3DBE5A"));
                    m.Box(V(x, 4.9f, z - .16f), V(.22f, .22f, .04f), Mix(K.Hex("F3B23A"), Ink, .55f));
                    m.Box(V(x, 4.55f, z - .16f), V(.22f, .22f, .04f), Mix(K.Hex("E8322B"), Ink, .55f));
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Box(V(x - .3f, 2.2f, z), V(.04f, 4.2f, .3f), SteelDark);
                }
            }
        }

        /// <summary>Behind the tracks: low grey sheds with saw-tooth roofs, a dark-glass office block, gums, and the city's
        /// taller blocks further back (no outline, no shadows).</summary>
        static void MBackdrop(CqChunkDraft d, float z0, float z1, int seed, float towerMin = 36f, float towerMax = 80f)
        {
            ComicMesh m = d.Deep;
            var r = new System.Random(seed * 13 + 1);
            for (float z = z0 + .5f; z < z1 - 6f; z += 13.5f)
            {
                float len = Mathf.Min(12.5f, z1 - z - .5f), zc = z + len * .5f, y = d.Y(zc), x = LX(38f + F(r, 0f, 4f));
                if (r.NextDouble() < .45)
                {
                    float h = F(r, 16f, 26f);
                    m.Surface = ComicSurface.Glass;
                    m.Box(V(x - 3f, y + h * .5f - 1f, zc), V(14f, h + 2f, len), K.Hex("2B3F52"));
                    m.Surface = ComicSurface.Plain;
                    for (float by = 4f; by < h - 1f; by += 3.4f) m.Box(V(x - 3f, y + by, zc), V(14.1f, .3f, len + .1f), K.Hex("8FA3B5"));
                    m.Box(V(x - 3f, y + h + .5f, zc), V(8f, 1.4f, len * .5f), PlantGrey);
                }
                else
                {
                    float h = F(r, 5f, 7.5f);
                    m.Surface = ComicSurface.Metal;
                    m.Box(V(x, y + h * .5f - 1f, zc), V(10f, h + 2f, len), K.Hex("B4B9BC"));
                    int teeth = 3;
                    for (int i = 0; i < teeth; i++)
                    {
                        float za = zc - len * .5f + len * i / teeth, zb2 = zc - len * .5f + len * (i + 1) / teeth;
                        K.Face(m, V(x - 5f, y + h, za), V(x + 5f, y + h, za), V(x + 5f, y + h + 1.6f, zb2), V(x - 5f, y + h + 1.6f, zb2), K.Hex("8A9198"), Vector3.up, false);
                        K.Face(m, V(x - 5f, y + h, zb2), V(x + 5f, y + h, zb2), V(x + 5f, y + h + 1.6f, zb2), V(x - 5f, y + h + 1.6f, zb2), K.Hex("2B3F52"), Vector3.forward, false);
                        K.TriFacing(m, V(x + 5f, y + h, za), V(x + 5f, y + h, zb2), V(x + 5f, y + h + 1.6f, zb2), K.Hex("B4B9BC"), Vector3.right);
                    }
                    m.Surface = ComicSurface.Plain;
                    K.Face(m, V(x + 5.02f, y + .2f, zc - 2f), V(x + 5.02f, y + .2f, zc + 2f), V(x + 5.02f, y + 3.6f, zc + 2f), V(x + 5.02f, y + 3.6f, zc - 2f), K.Hex("4E6A86"), Vector3.right, false);
                }
                CheapGum(m, V(LX(33.5f), y, z + len + .4f), F(r, 8f, 12f), seed + (int)z);
            }
            DeepBlocks(d, -1, z0, z1, 54f, seed + 3, towerMin, towerMax);
        }

        /// <summary>Second and third rows on side s from uNear metres beyond the sidewalk: blocks with floor bands, dark
        /// window bays on the faces you see and roof plant (no outline, no shadows).</summary>
        static void DeepBlocks(CqChunkDraft d, int s, float z0, float z1, float uNear, int seed, float hMin, float hMax)
        {
            ComicMesh m = d.Deep;
            var r = new System.Random(seed * 17 + 3);
            Color[] walls = { K.Hex("D9B98A"), K.Hex("EAD7AE"), K.Hex("C9CED3"), K.Hex("B98A6A"), K.Hex("9FB4C4"), K.Hex("E6E0D2"), K.Hex("B7A58C") };
            Color haze = new Color(.62f, .73f, .83f);
            for (float z = z0; z < z1 - 3f; z += 13f)
            {
                float len = Mathf.Min(12f, z1 - z - .6f), zc = z + len * .5f, h = F(r, hMin, hMax), w = F(r, 12f, 16f);
                float x = s * (K.Edge + uNear + w * .5f + F(r, 0f, 5f)), y = d.Ground(zc) - 2f;
                Color c = Mix(walls[r.Next(walls.Length)], haze, h > 34f ? .28f : .1f);
                bool glassy = h > 34f && r.NextDouble() < .6;
                if (glassy) c = Mix(K.Hex(r.NextDouble() < .5 ? "3E6E8E" : "2F5A6E"), haze, .25f);
                m.Surface = glassy ? ComicSurface.Glass : ComicSurface.Plaster;
                m.Box(V(x, y + h * .5f, zc), V(w, h, len), c);
                m.Surface = ComicSurface.Plain;
                m.Box(V(x, y + h + .25f, zc), V(w + .4f, .5f, len + .4f), Mix(c, White, .35f));
                float fx = x - s * w * .5f, fz = zc - len * .5f, step = h > 34f ? 4.2f : 3.2f;
                Color win = glassy ? Mix(c, White, .3f) : Mix(WindowGlass, haze, .2f);
                Vector3 toRoad = new Vector3(-s, 0, 0);
                for (float by = 4.6f; by < h - 1.6f; by += step)
                {
                    float y0 = y + by, y1 = y + by + (glassy ? .3f : 1.7f);
                    K.Face(m, V(fx - s * .03f, y0, zc - len * .42f), V(fx - s * .03f, y0, zc + len * .42f), V(fx - s * .03f, y1, zc + len * .42f), V(fx - s * .03f, y1, zc - len * .42f), win, toRoad, false);
                    K.Face(m, V(x - w * .42f, y0, fz - .03f), V(x + w * .42f, y0, fz - .03f), V(x + w * .42f, y1, fz - .03f), V(x - w * .42f, y1, fz - .03f), win, Vector3.back, false);
                }
                if (!glassy)
                {
                    // piers across the window bands so they read as separate windows
                    int bays = Mathf.Max(2, Mathf.RoundToInt(len / 3.2f));
                    for (int i = 1; i < bays; i++)
                    {
                        float bz = zc - len * .42f + len * .84f * i / bays;
                        K.Face(m, V(fx - s * .05f, y + 4.4f, bz - .35f), V(fx - s * .05f, y + 4.4f, bz + .35f), V(fx - s * .05f, y + h - 1f, bz + .35f), V(fx - s * .05f, y + h - 1f, bz - .35f), c, toRoad, false);
                    }
                    int side = Mathf.Max(2, Mathf.RoundToInt(w / 3.4f));
                    for (int i = 1; i < side; i++)
                    {
                        float bx = x - w * .42f + w * .84f * i / side;
                        K.Face(m, V(bx - .35f, y + 4.4f, fz - .05f), V(bx + .35f, y + 4.4f, fz - .05f), V(bx + .35f, y + h - 1f, fz - .05f), V(bx - .35f, y + h - 1f, fz - .05f), c, Vector3.back, false);
                    }
                }
                m.Surface = ComicSurface.Metal;
                m.Box(V(x + w * .12f, y + h + 1.2f, zc), V(w * .4f, 1.6f, len * .4f), Mix(PlantGrey, haze, .2f));
            }
        }

        /// <summary>Royal Perth Hospital's North Block: horizontal bands of off-white precast alternating with dark-blue
        /// ribbon glazing over a set-in lobby on round columns, a beige brick service tower, rooftop plant, the helipad.</summary>
        static void NorthBlock(CqChunkDraft d, CqLot l)
        {
            ComicMesh f = d.Frontage;
            float W = l.W, D = l.Depth, gh = 5f, fh = 3.6f; int floors = 6; float top = gh + floors * fh;
            Plinth(f, l); d.Wall(l, 20f);
            f.Surface = ComicSurface.Glass;
            K.LBox(f, l, .4f, W - .4f, 0f, gh, -1.4f, -D, GlassDark);
            K.LBox(f, l, .3f, W - 6.3f, gh, top, -.35f, -D + .3f, RibbonBlue);
            f.Surface = ComicSurface.Concrete;
            for (float a = 1f; a < W - 6f; a += 4.2f) f.Cylinder(l.P(a, gh * .5f, -.5f), .34f, gh, OffWhite, 8);
            for (int k = 0; k <= floors; k++)
            {
                float y = gh + k * fh, y0 = k == 0 ? y - .5f : y - .95f, y1 = k == floors ? y + .5f : y + .95f;
                K.LBox(f, l, 0f, W - 6f, y0, y1, 0f, -D, OffWhite);
            }
            // mullions on the ribbons (road face) and a soffit shadow line under each band
            f.Surface = ComicSurface.PaintedMetal;
            for (int k = 0; k < floors; k++)
                for (float a = 1.6f; a < W - 6.4f; a += 1.6f)
                    K.LQuad(f, l, a - .04f, a + .04f, gh + k * fh + .95f, gh + (k + 1) * fh - .95f, -.33f, OffWhite, false);
            // the entrance: a deep canopy on two columns, glass doors
            K.LBox(f, l, W * .28f, W * .5f, 3.3f, 3.6f, 2f, -1.4f, OffWhite);
            K.LBox(f, l, W * .28f - .05f, W * .5f + .05f, 3.18f, 3.32f, 2.05f, 1.85f, Ink);
            f.Surface = ComicSurface.Glass;
            K.LQuad(f, l, W * .33f, W * .45f, .05f, 2.9f, -1.38f, WindowGlass);
            f.Surface = ComicSurface.PaintedMetal;
            K.LQuad(f, l, W * .39f - .04f, W * .39f + .04f, .05f, 2.9f, -1.37f, Ink, false);
            // the beige brick service tower at the far end
            f.Surface = ComicSurface.Brick;
            K.LBox(f, l, W - 6f, W, 0f, top + 6.5f, .5f, -D * .7f, BrickBeige);
            Courses(f, l, W - 6f, W, 0f, top + 6.5f, .5f, Mix(BrickBeige, Ink, .22f), 2.4f);
            f.Surface = ComicSurface.Glass;
            K.LQuad(f, l, W - 3.5f, W - 2.5f, 3f, top + 5f, .52f, RibbonBlue);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, W - 6.15f, W + .15f, top + 6.5f, top + 6.95f, .65f, -D * .7f - .15f, OffWhite);
            // roof: parapet, plant room, units, the helipad deck on its frame with a windsock
            K.LBox(f, l, .6f, W - 6.6f, top + .5f, top + 3.3f, -4f, -D + 2f, K.Hex("C9C5BA"));
            RoofUnit(f, l, 4f, -2f, top + .5f, 2.2f, 1.3f);
            RoofUnit(f, l, 9f, -2.2f, top + .5f, 1.6f, 1f);
            f.Surface = ComicSurface.Concrete;
            Vector3 pad = l.P(W * .5f, top + 4.6f, -D * .5f);
            f.Cylinder(pad, 6.2f, .35f, ConcreteGrey, 10);
            f.Surface = ComicSurface.Plain;
            f.Cylinder(pad + V(0, .2f, 0), 4.6f, .06f, SignGreen, 10);
            f.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 4; i++) { float a = i * Mathf.PI * .5f + .6f; f.Box(pad + V(Mathf.Cos(a) * 4.5f, -.75f, Mathf.Sin(a) * 4.5f), V(.3f, 1.3f, .3f), SteelDark); }
            Vector3 sock = l.P(W * .5f + 5.5f, top + 4.8f, -D * .5f - 3f);
            f.Cylinder(sock + V(0, 1.4f, 0), .05f, 2.8f, White, 6);
            f.Surface = ComicSurface.Fabric;
            f.Cylinder(sock + V(0, 2.65f, .7f), .22f, 1.3f, SafetyOrange, 6, .45f, Quaternion.Euler(80, 0, 0));
        }

        /// <summary>The hospital's tall ward block on Wellington St: a tan brick slab, nine floors of continuous balconies
        /// with white upstands and rails between brick end piers, a darker podium with the entrance canopy, a stepped top
        /// with a white glazed strip, plant and flues.</summary>
        static void WardBlock(CqChunkDraft d, CqLot l)
        {
            ComicMesh f = d.Frontage;
            float W = l.W, D = l.Depth, gh = 5.4f, fh = 3.3f; int floors = 9; float top = gh + floors * fh, set = 1.7f;
            Plinth(f, l); d.Wall(l, 20f);
            f.Surface = ComicSurface.Brick;
            K.LBox(f, l, 0f, W, 0f, gh - .4f, 0f, -D, BrickTanDark);
            K.LBox(f, l, .7f, W - .7f, gh, top, -set, -D + .4f, BrickTan);
            K.LBox(f, l, 0f, .7f, gh, top, 0f, -D + .4f, BrickTan);
            K.LBox(f, l, W - .7f, W, gh, top, 0f, -D + .4f, BrickTan);
            Courses(f, l, 0f, .7f, gh, top, 0f, Mix(BrickTan, Ink, .25f), fh);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.12f, W + .12f, gh - .4f, gh, .35f, -D - .05f, TrimCream);
            // podium: glazed bays between brick piers, the entrance canopy
            int bays = Mathf.Max(4, Mathf.RoundToInt(W / 4f));
            float bw = W / bays;
            for (int i = 0; i < bays; i++)
            {
                f.Surface = ComicSurface.Brick;
                K.LBox(f, l, i * bw, i * bw + .5f, 0f, gh - .4f, .14f, 0f, BrickTanDark);
                if (i == bays / 2) Shopfront(f, l, i * bw + .6f, (i + 1) * bw - .1f, .05f, 3.6f, .02f, true);
                else Win(f, l, (i + .5f) * bw + .25f, bw - 1.4f, 1.1f, 3.6f, 0f, TrimCream, 3);
            }
            float ec = (bays / 2 + .5f) * bw;
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, ec - 3.4f, ec + 3.4f, 3.8f, 4.05f, 2.9f, 0f, White);
            K.LBox(f, l, ec - 3.45f, ec + 3.45f, 3.68f, 3.82f, 2.95f, 2.78f, Ink);
            foreach (float a in new[] { ec - 3.1f, ec + 3.1f }) f.Cylinder(l.P(a, 1.9f, 2.6f), .11f, 3.8f, White, 8);
            // the balconies
            int posts = 5;
            for (int k = 0; k < floors; k++)
            {
                float y = gh + k * fh;
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, .7f, W - .7f, y - .2f, y, -.02f, -set, OffWhite);
                K.LBox(f, l, .7f, W - .7f, y, y + .46f, -.02f, -.14f, White);
                f.Surface = ComicSurface.PaintedMetal;
                K.LBox(f, l, .7f, W - .7f, y + 1.02f, y + 1.1f, -.02f, -.12f, White);
                for (int i = 1; i < posts; i++)
                {
                    float a = Mathf.Lerp(.7f, W - .7f, (float)i / posts);
                    K.LBox(f, l, a - .04f, a + .04f, y + .46f, y + 1.02f, -.03f, -.11f, White);
                }
                int rooms = bays * 2;
                for (int i = 0; i < rooms; i++)
                {
                    float ac = .7f + (W - 1.4f) * (i + .5f) / rooms, hw = (W - 1.4f) / rooms * .36f;
                    K.LQuad(f, l, ac - hw, ac + hw, y + .1f, y + 2.5f, -set + .02f, Ink);
                    f.Surface = ComicSurface.Glass;
                    K.LQuad(f, l, ac - hw + .1f, ac + hw - .1f, y + .2f, y + 2.4f, -set + .035f, i % 3 == 1 ? GlassDark : WindowGlass, false);
                    f.Surface = ComicSurface.PaintedMetal;
                }
                // the end wall towards the rider
                EndWin(f, l, 0f, -3.6f, -5.2f, y + .9f, y + 2.4f);
                EndWin(f, l, 0f, -8.4f, -10f, y + .9f, y + 2.4f);
            }
            // top: parapet, the stepped storey with its white glazed strip, flues and plant
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.08f, W + .08f, top, top + .55f, .08f, -D + .32f, TrimCream);
            f.Surface = ComicSurface.Brick;
            K.LBox(f, l, 2.5f, W - 2.5f, top + .55f, top + 3.6f, -3f, -D + 1.8f, BrickTan);
            f.Surface = ComicSurface.Glass;
            K.LBox(f, l, 2.44f, W - 2.44f, top + 1.3f, top + 2.7f, -2.94f, -D + 1.74f, GlassPale);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 2.3f, W - 2.3f, top + 3.6f, top + 3.9f, -2.8f, -D + 1.6f, OffWhite);
            f.Surface = ComicSurface.Metal;
            for (int i = 0; i < 3; i++) f.Cylinder(l.P(W * .3f + i * 1.3f, top + 5.2f, -D * .55f), .28f, 2.6f, PlantGrey, 8);
            RoofUnit(f, l, W * .7f, -D * .5f, top + 3.9f, 2.6f, 1.5f);
        }

        /// <summary>The enclosed link bridge over Wellington St between the two hospital blocks: white precast bands, a
        /// ribbon of dark glass, ROYAL PERTH HOSPITAL on the side the rider sees, two round columns beyond the sidewalks.</summary>
        static void LinkBridge(CqChunkDraft d, float z0, float z1)
        {
            ComicMesh m = d.Frontage;
            float y = d.Y((z0 + z1) * .5f) + 7.6f, xa = -(K.Edge + 2.4f), xb = K.Edge + 3.4f, zc = (z0 + z1) * .5f, len = z1 - z0;
            m.Surface = ComicSurface.Concrete;
            m.Box(V((xa + xb) * .5f, y + .55f, zc), V(xb - xa, 1.1f, len), OffWhite);
            m.Box(V((xa + xb) * .5f, y + 3.25f, zc), V(xb - xa, 1.3f, len), OffWhite);
            m.Box(V((xa + xb) * .5f, y + 4f, zc), V(xb - xa + .2f, .16f, len + .5f), K.Hex("B9B5AA"));
            m.Surface = ComicSurface.Glass;
            m.Box(V((xa + xb) * .5f, y + 1.85f, zc), V(xb - xa, 1.5f, len - .3f), RibbonBlue);
            m.Surface = ComicSurface.PaintedMetal;
            for (float x = xa + 1.5f; x < xb - 1f; x += 1.75f)
            {
                K.Face(m, V(x - .04f, y + 1.1f, z0 + .14f), V(x + .04f, y + 1.1f, z0 + .14f), V(x + .04f, y + 2.6f, z0 + .14f), V(x - .04f, y + 2.6f, z0 + .14f), OffWhite, Vector3.back, false);
                K.Face(m, V(x - .04f, y + 1.1f, z1 - .14f), V(x + .04f, y + 1.1f, z1 - .14f), V(x + .04f, y + 2.6f, z1 - .14f), V(x - .04f, y + 2.6f, z1 - .14f), OffWhite, Vector3.forward, false);
            }
            m.Box(V((xa + xb) * .5f, y - .04f, zc), V(xb - xa, .08f, len - .2f), Ink);
            PixelText(m, "ROYAL PERTH HOSPITAL", 0f, y + 3.25f, z0 - .02f, .15f, Navy);
            m.Surface = ComicSurface.Concrete;
            foreach (float x in new[] { -(K.Edge + .7f), K.Edge + .7f })
            {
                m.Cylinder(V(x, d.Y(zc) + 3.6f, zc), .42f, 8.2f, OffWhite, 8);
                m.Box(V(x, y - .25f, zc), V(1.4f, .5f, len - .6f), OffWhite);
            }
        }

        /// <summary>The hospital's street fence: cream rendered piers with caps, white iron railings between them. A gap from
        /// gate0 to gate1 for the path.</summary>
        static void PierFence(CqChunkDraft d, int s, float z0, float z1, float u, float gate0 = -1f, float gate1 = -1f)
        {
            ComicMesh m = d.SlopedFrontage;
            float x = s * (K.Edge + u), y = K.Kerb;
            var stops = new List<float>();
            for (float z = z0 + .3f; z < z1; z += 4.95f) if (!(z > gate0 + .3f && z < gate1 - .3f)) stops.Add(z);
            if (gate0 >= 0f) { stops.Add(gate0); stops.Add(gate1); }
            stops.Sort();
            for (int i = 0; i < stops.Count; i++)
            {
                float z = stops[i];
                bool gate = gate0 >= 0f && (Mathf.Abs(z - gate0) < .01f || Mathf.Abs(z - gate1) < .01f);
                m.Surface = ComicSurface.Plaster;
                m.Box(V(x, y + (gate ? 1.15f : .85f), z), V(gate ? .7f : .5f, gate ? 2.3f : 1.7f, gate ? .7f : .5f), Cream);
                m.Surface = ComicSurface.Concrete;
                m.Box(V(x, y + (gate ? 2.38f : 1.76f), z), V(gate ? .9f : .66f, .14f, gate ? .9f : .66f), TrimCream);
                if (gate) m.Cylinder(V(x, y + 2.65f, z), .22f, .4f, TrimCream, 6, .2f);
                if (i + 1 >= stops.Count) break;
                float e = stops[i + 1];
                if (gate0 >= 0f && z >= gate0 - .01f && e <= gate1 + .01f) continue;
                m.Surface = ComicSurface.Plaster;
                m.Box(V(x, y + .22f, (z + e) * .5f), V(.3f, .44f, e - z - .4f), Cream);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(x, y + 1.42f, (z + e) * .5f), V(.05f, .06f, e - z - .4f), White);
                ComicMesh r = d.Road;
                r.Surface = ComicSurface.PaintedMetal;
                Vector3 n = new Vector3(-s, 0, 0);
                for (float p = z + .5f; p < e - .4f; p += .3f)
                    K.Face(r, V(x - s * .02f, y + .44f, p), V(x - s * .02f, y + .44f, p + .06f), V(x - s * .02f, y + 1.55f, p + .06f), V(x - s * .02f, y + 1.55f, p), White, n, false);
            }
            SideWall(d, s, z0, gate0 >= 0f ? gate0 : z1, 6f);
            if (gate0 >= 0f) SideWall(d, s, gate1, z1, 6f);
        }

        /// <summary>Kirkman House (1909): two storeys of red brick with cream bands, a round-arched arcade on the ground
        /// floor, a white-balustraded verandah above under a lean-to roof, a terracotta hip roof and tall chimneys.</summary>
        static void KirkmanHouse(CqChunkDraft d, CqLot l)
        {
            ComicMesh f = d.Frontage;
            float W = l.W, D = l.Depth, fh = 4.2f, h = 8.4f, ver = 2f;
            Plinth(f, l); d.Wall(l, 10f);
            f.Surface = ComicSurface.Brick;
            K.LBox(f, l, 0f, W, 0f, h, -ver, -D, BrickRed);
            int n = Mathf.Max(3, Mathf.RoundToInt(W / 3.2f));
            float bay = W / n, ys = 2.35f;
            K.LBox(f, l, 0f, W, ys, fh - .12f, 0f, -.5f, BrickRed);
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Clamp(i * bay, .35f, W - .35f);
                f.Surface = ComicSurface.Brick;
                K.LBox(f, l, a - .35f, a + .35f, 0f, ys, 0f, -.5f, BrickRed);
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, a - .42f, a + .42f, ys - .22f, ys, .06f, -.56f, TrimCream);
                K.LBox(f, l, a - .42f, a + .42f, 0f, .35f, .06f, -.56f, TrimCream);
            }
            Color shade = Mix(BrickDark, Ink, .5f);
            for (int i = 0; i < n; i++) Arch(f, l, (i + .5f) * bay, bay - .7f, ys, ys, 0f, shade, TrimCream);
            // the arcade's back wall in shadow, with doors and windows; its paved floor
            f.Surface = ComicSurface.Plain;
            K.LQuad(f, l, .2f, W - .2f, .15f, fh - .2f, -ver + .02f, shade);
            for (int i = 0; i < n; i++)
                if (i == n / 2) { f.Surface = ComicSurface.Wood; K.LQuad(f, l, (i + .5f) * bay - .6f, (i + .5f) * bay + .6f, .15f, 2.7f, -ver + .04f, K.Hex("2F5A3A")); }
                else Win(f, l, (i + .5f) * bay, 1.2f, 1f, 2.9f, -ver + .02f, TrimCream, 2);
            f.Surface = ComicSurface.Paving;
            K.LBox(f, l, 0f, W, 0f, .15f, .3f, -ver, PavingWarm);
            // string course, the upper verandah: floor, posts, balustrade, french doors, the lean-to roof
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.08f, W + .08f, fh - .14f, fh + .1f, .16f, -ver, TrimCream);
            f.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Clamp(i * bay, .12f, W - .12f);
                K.LBox(f, l, a - .07f, a + .07f, fh + .1f, h - .25f, -.02f, -.16f, White);
                if (i < n) { f.Beam(l.P(a + .07f, h - .8f, -.09f), l.P(a + .6f, h - .3f, -.09f), .06f, White); f.Beam(l.P(a + bay - .07f, h - .8f, -.09f), l.P(a + bay - .6f, h - .3f, -.09f), .06f, White); }
            }
            Balcony(f, l, .1f, W - .1f, fh + .1f, -.2f, .16f, TrimCream, White, false, .95f);
            K.LBox(f, l, 0f, W, h - .38f, h - .25f, .02f, -.2f, White);
            for (int i = 0; i < n; i++) Win(f, l, (i + .5f) * bay, 1.25f, fh + .25f, fh + 2.9f, -ver, TrimCream, 2, false, true, true);
            Courses(f, l, 0f, W, fh, h, -ver, TrimCream, 1.4f);
            f.Surface = ComicSurface.Brick;
            SlopeSlab(f, l, -.25f, W + .25f, h + .75f, -ver - .1f, h - .2f, .45f, .1f, Terracotta, TerracottaDark);
            // the main hip roof, ink eaves, chimneys
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, -.4f, W + .4f, h + .7f, h + .84f, -ver + .35f, -D - .35f, Ink);
            f.Surface = ComicSurface.Brick;
            HipRoof(f, l, -.4f, W + .4f, h + .82f, h + 4.3f, -ver + .35f, -D - .35f, Terracotta);
            Chimney(f, l, W * .22f, -D * .55f, h + 2f, 3.6f, BrickRed);
            Chimney(f, l, W * .78f, -D * .55f, h + 2f, 3.6f, BrickRed);
            // the end towards the rider: two sash windows, a cream band
            EndWin(f, l, 0f, -ver - 2f, -ver - 3.4f, 1f, 3f);
            EndWin(f, l, 0f, -ver - 2f, -ver - 3.4f, fh + .9f, fh + 2.9f);
        }

        /// <summary>The Administration Building (1890s, Federation Queen Anne): four storeys of red face brick with cream
        /// string courses and a roughcast top storey, two gabled bays with half-timbered gables, an arched entrance, a
        /// terracotta tile roof and tall chimneys.</summary>
        static void AdminBuilding(CqChunkDraft d, CqLot l)
        {
            ComicMesh f = d.Frontage;
            float W = l.W, D = l.Depth, fh = 3.7f; int floors = 4; float h = floors * fh, bw = Mathf.Min(4.2f, W * .32f);
            Plinth(f, l); d.Wall(l, 14f);
            f.Surface = ComicSurface.Brick;
            K.LBox(f, l, 0f, W, 0f, h, -.7f, -D, BrickRed);
            K.LBox(f, l, 0f, bw, 0f, h, 0f, -.7f, BrickRed);
            K.LBox(f, l, W - bw, W, 0f, h, 0f, -.7f, BrickRed);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, -.02f, bw + .02f, h - fh + .2f, h, .03f, -.7f, Cream);
            K.LBox(f, l, W - bw - .02f, W + .02f, h - fh + .2f, h, .03f, -.7f, Cream);
            K.LBox(f, l, bw, W - bw, h - fh + .2f, h, -.67f, -.75f, Cream);
            f.Surface = ComicSurface.Concrete;
            for (int k = 1; k <= floors; k++) K.LBox(f, l, -.06f, W + .06f, k * fh - .14f, k * fh + .08f, .1f, -D - .04f, TrimCream);
            K.LBox(f, l, -.06f, W + .06f, 0f, .5f, .08f, -D - .04f, K.Hex("B9A27E"));
            for (int k = 0; k < floors; k++)
            {
                float y = k * fh;
                Win(f, l, bw * .5f, 1.7f, y + 1f, y + 3f, k == floors - 1 ? .03f : 0f, TrimCream, 2, true, true, true);
                Win(f, l, W - bw * .5f, 1.7f, y + 1f, y + 3f, k == floors - 1 ? .03f : 0f, TrimCream, 2, true, true, true);
                float mid = W * .5f, gap = (W - 2f * bw) * .27f;
                if (k > 0) { Win(f, l, mid - gap, 1.05f, y + 1f, y + 3f, -.7f, TrimCream, 2); Win(f, l, mid + gap, 1.05f, y + 1f, y + 3f, -.7f, TrimCream, 2); }
                EndWin(f, l, 0f, -3f, -4.4f, y + 1f, y + 3f);
                EndWin(f, l, 0f, -7.4f, -8.8f, y + 1f, y + 3f);
            }
            Arch(f, l, W * .5f, 2.1f, 0f, 2.5f, -.7f, K.Hex("2F5A3A"), TrimCream);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, W * .5f - 1.7f, W * .5f + 1.7f, 0f, .3f, .4f, -.7f, TrimCream);
            // roofs: the long tiled roof, gables over the bays with half-timbering, eaves and chimneys
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, -.3f, W + .3f, h, h + .14f, .35f, -D - .3f, Ink);
            f.Surface = ComicSurface.Brick;
            K.LSlope(f, l, -.3f, W + .3f, h + .12f, h + 3.6f, -.4f, -D * .5f, -D - .3f, Terracotta);
            foreach (float a0 in new[] { -.25f, W - bw - .25f })
            {
                f.Surface = ComicSurface.Brick;
                K.LGable(f, l, a0, a0 + bw + .5f, h + .12f, h + 3.1f, .4f, -D * .45f, Terracotta);
                f.Surface = ComicSurface.Plaster;
                float ac = a0 + bw * .5f + .25f;
                K.TriFacing(f, l.P(a0 + .5f, h + .2f, .42f), l.P(a0 + bw, h + .2f, .42f), l.P(ac, h + 2.55f, .42f), Cream, new Vector3(-l.S, 0, 0));
                f.Surface = ComicSurface.Wood;
                f.Beam(l.P(ac, h + .2f, .45f), l.P(ac, h + 2.5f, .45f), .12f, K.Hex("5E4638"));
                f.Beam(l.P(ac - bw * .25f, h + .2f, .45f), l.P(ac - bw * .25f, h + 1.3f, .45f), .1f, K.Hex("5E4638"));
                f.Beam(l.P(ac + bw * .25f, h + .2f, .45f), l.P(ac + bw * .25f, h + 1.3f, .45f), .1f, K.Hex("5E4638"));
                f.Surface = ComicSurface.PaintedMetal;
                f.Beam(l.P(a0 - .05f, h + .12f, .5f), l.P(ac, h + 3.15f, .5f), .14f, White);
                f.Beam(l.P(a0 + bw + .55f, h + .12f, .5f), l.P(ac, h + 3.15f, .5f), .14f, White);
                f.Cylinder(l.P(ac, h + 3.55f, .45f), .08f, .8f, Ink, 6);
            }
            Chimney(f, l, W * .36f, -D * .5f, h + 2.4f, 3.4f, BrickRed);
            Chimney(f, l, W * .7f, -D * .62f, h + 2f, 3.6f, BrickRed);
        }

        /// <summary>A two or three storey shop terrace (the old brick fronts of Wellington St and East Perth): framed
        /// shopfronts between piers under a cantilevered street awning, sash windows with heads and sills, a cornice and a
        /// parapet (0 flat with urns, 1 pediment, 2 stepped), a roof with plant behind it, a chimney.</summary>
        static void ShopTerrace(CqChunkDraft d, CqLot l, int floors, Color wall, Color trim, Color awning, int parapet, bool brick)
        {
            ComicMesh f = d.Frontage;
            float W = l.W, D = l.Depth, gh = 4.2f, fh = 3.5f, h = gh + (floors - 1) * fh;
            Plinth(f, l); d.Wall(l, h + 2f);
            f.Surface = brick ? ComicSurface.Brick : ComicSurface.Plaster;
            K.LBox(f, l, 0f, W, 0f, h, 0f, -D, wall);
            Color pier = Mix(wall, Ink, .2f);
            int shops = Mathf.Max(2, Mathf.RoundToInt(W / 5f));
            float sw = W / shops;
            for (int i = 0; i < shops; i++)
            {
                float a0 = i * sw, a1 = a0 + sw;
                f.Surface = brick ? ComicSurface.Brick : ComicSurface.Plaster;
                K.LBox(f, l, a0, a0 + .4f, 0f, gh - .6f, .14f, 0f, pier);
                if (i == shops - 1) K.LBox(f, l, W - .4f, W, 0f, gh - .6f, .14f, 0f, pier);
                Shopfront(f, l, a0 + .5f, a1 - (i == shops - 1 ? .5f : .1f), .05f, 3.1f, .02f, i % 2 == 0);
                f.Surface = ComicSurface.PaintedMetal;
                K.LBox(f, l, a0 + .7f, a1 - .5f, 3.75f, 4.3f, .12f, 0f, SignColours[Mathf.Abs(i * 3 + (int)(l.Z0 * 2f) + floors) % SignColours.Length]);
                for (int k = 1; k < floors; k++)
                {
                    float y = gh + (k - 1) * fh;
                    Win(f, l, a0 + sw * .3f, 1.05f, y + .95f, y + 2.75f, 0f, trim, 2, true, true, true);
                    Win(f, l, a0 + sw * .7f, 1.05f, y + .95f, y + 2.75f, 0f, trim, 2, true, true, true);
                }
            }
            FlatAwning(f, l, -.1f, W + .1f, 3.25f, 2.7f, awning, Mix(awning, White, .6f));
            f.Surface = ComicSurface.Concrete;
            for (int k = 1; k < floors; k++) K.LBox(f, l, -.05f, W + .05f, gh + (k - 1) * fh + .3f, gh + (k - 1) * fh + .48f, .1f, -.05f, trim);
            K.LBox(f, l, -.15f, W + .15f, h, h + .36f, .36f, -D - .1f, trim);
            f.Surface = brick ? ComicSurface.Brick : ComicSurface.Plaster;
            K.LBox(f, l, 0f, W, h + .36f, h + 1.15f, .08f, -.42f, wall);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.06f, W + .06f, h + 1.15f, h + 1.3f, .14f, -.48f, trim);
            if (parapet == 1)
            {
                K.LGable(f, l, W * .32f, W * .68f, h + 1.3f, h + 2.5f, .12f, -.44f, trim);
                f.Surface = brick ? ComicSurface.Brick : ComicSurface.Plaster;
                K.TriFacing(f, l.P(W * .37f, h + 1.36f, .14f), l.P(W * .63f, h + 1.36f, .14f), l.P(W * .5f, h + 2.22f, .14f), wall, new Vector3(-l.S, 0, 0));
            }
            else if (parapet == 2)
            {
                f.Surface = brick ? ComicSurface.Brick : ComicSurface.Plaster;
                K.LBox(f, l, W * .3f, W * .7f, h + 1.3f, h + 2f, .1f, -.44f, wall);
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, W * .28f, W * .72f, h + 2f, h + 2.16f, .16f, -.5f, trim);
                K.LBox(f, l, W * .42f, W * .58f, h + 2.16f, h + 2.6f, .1f, -.44f, trim);
            }
            else
                foreach (float a in new[] { .35f, W - .35f }) { K.LBox(f, l, a - .3f, a + .3f, h + 1.3f, h + 1.75f, .14f, -.46f, trim); f.Cylinder(l.P(a, h + 2.02f, -.16f), .2f, .5f, trim, 6, .5f); }
            f.Surface = ComicSurface.Metal;
            K.LBox(f, l, .4f, W - .4f, h + .36f, h + .6f, -.5f, -D + .3f, RoofGrey);
            RoofUnit(f, l, W * .3f, -4.5f, h + .6f, 1.5f, .9f);
            if (floors == 2) Chimney(f, l, W * .78f, -5f, h + .6f, 2.4f, brick ? wall : BrickRed);
            EndWin(f, l, 0f, -3.5f, -4.9f, gh + .95f, gh + 2.75f);
        }

        /// <summary>The Perth Mint (1899): rock-faced cream limestone, two storeys, a taller central pavilion with an arched
        /// entrance and a loggia, flanking wings with round-arched arcades below and white-balustraded loggias above,
        /// orange-red tiled hip roofs, red brick chimneys and one tall stack.</summary>
        static void PerthMint(CqChunkDraft d, CqLot l)
        {
            ComicMesh f = d.Frontage;
            float W = l.W, D = l.Depth, fh = 4.3f, h = 8.6f, pw = Mathf.Min(6f, W * .34f), p0 = (W - pw) * .5f, p1 = p0 + pw, ar = 1.6f;
            Plinth(f, l); d.Wall(l, 12f);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0f, W, 0f, h, -ar, -D, Limestone);
            K.LBox(f, l, p0, p1, 0f, h + 1.3f, .9f, -ar, Limestone);
            Courses(f, l, p0, p1, 0f, h + 1.3f, .9f, LimestoneDark, .75f);
            Arch(f, l, W * .5f, 2.2f, 0f, 2.7f, .9f, K.Hex("2F5A3A"), LimestoneCap);
            for (int i = -1; i <= 1; i++) Arch(f, l, W * .5f + i * 1.7f, 1.1f, fh + .7f, fh + 2.2f, .9f, Mix(LimestoneDark, Ink, .6f), LimestoneCap);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, p0 - .12f, p1 + .12f, fh - .1f, fh + .16f, 1.02f, -ar, LimestoneCap);
            K.LBox(f, l, p0 - .2f, p1 + .2f, h + 1.3f, h + 1.62f, 1.15f, -ar - .1f, LimestoneCap);
            K.LBox(f, l, p0 + .6f, p1 - .6f, 0f, .25f, 1.7f, .9f, LimestoneCap);
            // the wings: arcade below, loggia above
            Color shade = Mix(LimestoneDark, Ink, .55f);
            foreach ((float a0, float a1) in new[] { (0f, p0), (p1, W) })
            {
                float w = a1 - a0; int n = Mathf.Max(2, Mathf.RoundToInt(w / 2.9f)); float bay = w / n, ys = 2.4f;
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, a0, a1, ys, fh, 0f, -.5f, Limestone);
                for (int i = 0; i <= n; i++)
                {
                    float a = Mathf.Clamp(a0 + i * bay, a0 + .3f, a1 - .3f);
                    K.LBox(f, l, a - .3f, a + .3f, 0f, ys, 0f, -.5f, Limestone);
                    K.LBox(f, l, a - .36f, a + .36f, ys - .2f, ys, .06f, -.56f, LimestoneCap);
                    if (i > 0 && i < n) f.Cylinder(l.P(a, fh + 1.9f, -.25f), .16f, 2f, LimestoneCap, 8);
                }
                for (int i = 0; i < n; i++)
                {
                    float ac = a0 + (i + .5f) * bay;
                    Arch(f, l, ac, bay - .6f, ys, ys, 0f, shade, LimestoneCap);
                    Win(f, l, ac, 1.1f, 1f, 2.9f, -ar + .02f, LimestoneCap, 2);
                    Win(f, l, ac, 1.1f, fh + .5f, fh + 2.7f, -ar + .02f, LimestoneCap, 2, false);
                }
                f.Surface = ComicSurface.Plain;
                K.LQuad(f, l, a0 + .1f, a1 - .1f, .1f, fh - .1f, -ar + .01f, shade, false);
                f.Surface = ComicSurface.Concrete;
                K.LBox(f, l, a0, a1, fh - .12f, fh + .12f, .14f, -ar, LimestoneCap);
                Balcony(f, l, a0 + .05f, a1 - .05f, fh + .12f, -.22f, .2f, LimestoneCap, White, false, .9f);
                K.LBox(f, l, a0, a0 + .5f, fh + .12f, h, 0f, -.5f, Limestone);
                K.LBox(f, l, a1 - .5f, a1, fh + .12f, h, 0f, -.5f, Limestone);
                K.LBox(f, l, a0, a1, h - .7f, h, 0f, -.5f, Limestone);
            }
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, -.2f, W + .2f, h, h + .3f, .3f, -D - .2f, LimestoneCap);
            f.Surface = ComicSurface.Brick;
            HipRoof(f, l, -.4f, W + .4f, h + .3f, h + 3.5f, .5f, -D - .3f, Terracotta);
            HipRoof(f, l, p0 - .5f, p1 + .5f, h + 1.62f, h + 4.6f, 1.4f, -4.5f, Terracotta);
            Chimney(f, l, W * .16f, -D * .5f, h + 2f, 3.2f, BrickRed);
            Chimney(f, l, W * .84f, -D * .5f, h + 2f, 3.2f, BrickRed);
            // the tall brick stack behind
            f.Surface = ComicSurface.Brick;
            f.Cylinder(l.P(W * .68f, 9f, -D - 3f), 1f, 18f, BrickRed, 8, .7f);
            f.Surface = ComicSurface.Concrete;
            f.Cylinder(l.P(W * .68f, 18.2f, -D - 3f), .85f, .4f, TrimCream, 8);
            EndWin(f, l, 0f, -ar - 2f, -ar - 3.3f, 1f, 2.9f);
            EndWin(f, l, 0f, -ar - 2f, -ar - 3.3f, fh + .8f, fh + 2.7f);
        }

        /// <summary>The Mint's front: lawn, clipped hedges, the limestone street wall with its arched gateway and black
        /// gates with gold tips, white bollards, a name board.</summary>
        static void MintForecourt(CqChunkDraft d, float z0, float z1)
        {
            float zc = (z0 + z1) * .5f;
            LowWall(d, 1, z0, zc - 1.7f, 1f, Limestone, LimestoneCap);
            LowWall(d, 1, zc + 1.7f, z1, 1f, Limestone, LimestoneCap);
            HedgeRow(d, 1, z0 + .5f, zc - 2.4f, 1.3f, .9f);
            HedgeRow(d, 1, zc + 2.4f, z1 - .3f, 1.3f, .9f);
            Patch(d, 1, zc - 1.5f, zc + 1.5f, 0f, 9.5f, PavingWarm, K.Kerb + .04f, ComicSurface.Paving);
            ComicMesh m = d.SlopedFrontage;
            float x = K.Edge + .3f, y = K.Kerb;
            m.Surface = ComicSurface.Concrete;
            foreach (float z in new[] { zc - 1.9f, zc + 1.9f })
            {
                m.Box(V(x, y + 1.7f, z), V(.8f, 3.4f, .8f), Limestone);
                m.Box(V(x, y + 3.48f, z), V(1f, .16f, 1f), LimestoneCap);
            }
            m.Box(V(x, y + 3.95f, zc), V(.7f, .7f, 4.6f), Limestone);
            m.Box(V(x, y + 4.38f, zc), V(.9f, .16f, 4.9f), LimestoneCap);
            m.Surface = ComicSurface.Plain;
            K.Face(m, V(x - .36f, y + 3.6f, zc - 1.5f), V(x - .36f, y + 3.6f, zc + 1.5f), V(x - .36f, y + 3.0f, zc + .9f), V(x - .36f, y + 3.0f, zc - .9f), Limestone, Vector3.left, false);
            // the gates, folded open against the piers: black bars with gold tips
            m.Surface = ComicSurface.PaintedMetal;
            foreach (int g in new[] { -1, 1 })
            {
                float zg = zc + g * 1.45f;
                m.Box(V(x + .9f, y + 1.9f, zg), V(1.6f, .07f, .07f), FenceBlack);
                m.Box(V(x + .9f, y + .3f, zg), V(1.6f, .07f, .07f), FenceBlack);
                for (float dx = .2f; dx <= 1.65f; dx += .28f)
                {
                    m.Box(V(x + dx, y + 1.15f, zg), V(.05f, 2.1f, .05f), FenceBlack);
                    m.Box(V(x + dx, y + 2.3f, zg), V(.09f, .16f, .09f), Gold);
                }
            }
            foreach (float z in new[] { zc - 3.4f, zc + 3.4f }) { m.Surface = ComicSurface.Concrete; m.Cylinder(V(K.Edge - .5f, y + .45f, z), .16f, .9f, White, 8); }
            NameBoard(d.Props, V(K.Edge + 2.6f, d.Ground(z0 + 1.2f), z0 + 1.4f), "THE PERTH MINT", .075f, K.Hex("1F4A36"), Gold, 1.3f);
            Gum(d.Props, V(K.Edge + 4.5f, d.Ground(z1 - 2f), z1 - 2.5f), 9f, 6);
            SideWall(d, 1, z0, zc - 1.7f, 8f); SideWall(d, 1, zc + 1.7f, z1, 8f);
        }

        /// <summary>Wellington Square's curving path: dark asphalt with pale concrete edging, from the gap in the wall.</summary>
        static void SquarePath(CqChunkDraft d)
        {
            ComicMesh m = d.Road;
            Color asphalt = K.Hex("5E646C"), edge = K.Hex("D9D4C8");
            Vector3 P(float t) => V(K.Edge + 1f + t * 44f, K.Kerb + .045f, 15f + 9f * Mathf.Sin(t * 3.4f) + t * 6f);
            const int n = 14;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = P((float)i / n), b = P((float)(i + 1) / n), side = Vector3.Cross(Vector3.up, (b - a).normalized);
                m.Surface = ComicSurface.Plain;
                K.Face(m, a - side * 1.55f - V(0, .008f, 0), b - side * 1.55f - V(0, .008f, 0), b + side * 1.55f - V(0, .008f, 0), a + side * 1.55f - V(0, .008f, 0), edge, Vector3.up, false);
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, a - side * 1.3f, b - side * 1.3f, b + side * 1.3f, a + side * 1.3f, asphalt, Vector3.up, false);
            }
            Patch(d, 1, 13f, 17f, 0f, 1.6f, asphalt, K.Kerb + .04f, ComicSurface.Asphalt);
        }

        /// <summary>The Wellington Square playground (Koolangka Koolangka Waabiny): a cable net of flat discs in red, orange,
        /// yellow, ochre, tan and a few green on tall leaning poles, a timber play tower with a slide, a climbing net,
        /// rubber mounds.</summary>
        static void SquarePlayground(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Rubber;
            m.Box(p + V(0, .05f, 0), V(15f, .1f, 17f), K.Hex("D9B26A"));
            m.Ellipsoid(p + V(-3.5f, .1f, -4.5f), V(5f, 1.6f, 5f), K.Hex("C9763E"));
            m.Ellipsoid(p + V(4f, .1f, 5.5f), V(4.2f, 1.2f, 4.2f), K.Hex("3E9A96"));
            // poles and the net of discs
            Vector3[] poles = { V(-6.5f, 0, -7f), V(6.5f, 0, -7.5f), V(7f, 0, 7f), V(-6.8f, 0, 7.5f), V(0f, 0, -9f), V(0f, 0, 9f) };
            m.Surface = ComicSurface.PaintedMetal;
            foreach (Vector3 q in poles)
            {
                Vector3 top = p + q * .92f + V(0, 8.2f, 0);
                Limb(m, p + q * 1.08f, top, .16f, .1f, K.Hex("E6E7E2"), 6);
                m.Cylinder(p + q * 1.08f + V(0, .1f, 0), .3f, .2f, Ink, 6);
            }
            Color[] discs = { K.Hex("D9432F"), K.Hex("F28C28"), K.Hex("F6C543"), K.Hex("C98A3E"), K.Hex("E2C79A"), K.Hex("D9432F"), K.Hex("F28C28"), K.Hex("7FA05A"), K.Hex("F6C543"), K.Hex("C9763E") };
            var r = new System.Random(77);
            int k = 0;
            for (int ix = -2; ix <= 2; ix++)
                for (int iz = -2; iz <= 2; iz++)
                {
                    if (Mathf.Abs(ix) == 2 && Mathf.Abs(iz) == 2) continue;
                    Vector3 c = p + V(ix * 2.9f + F(r, -.4f, .4f), 5.6f + 1.5f * Mathf.Cos(ix * .7f) * Mathf.Cos(iz * .7f) + F(r, -.2f, .2f), iz * 3.3f + F(r, -.4f, .4f));
                    m.Surface = ComicSurface.Fabric;
                    m.Cylinder(c, F(r, 1.3f, 1.75f), .16f, discs[k++ % discs.Length], 8, 1f, Quaternion.Euler(F(r, -12f, 12f), F(r, 0f, 45f), F(r, -12f, 12f)));
                }
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 4; i++) m.Beam(p + poles[i] * .92f + V(0, 8.2f, 0), p + poles[(i + 1) % 4] * .92f + V(0, 8.2f, 0), .05f, SteelDark);
            m.Beam(p + poles[4] * .92f + V(0, 8.2f, 0), p + poles[5] * .92f + V(0, 8.2f, 0), .05f, SteelDark);
            // the timber tower, its slide and the climbing net
            Vector3 t = p + V(2.5f, .1f, -3f);
            m.Surface = ComicSurface.Wood;
            foreach (float x in new[] { -.9f, .9f }) foreach (float z in new[] { -.9f, .9f }) m.Box(t + V(x, 2.2f, z), V(.18f, 4.4f, .18f), Timber);
            m.Box(t + V(0, 2.3f, 0), V(2.1f, .14f, 2.1f), DeckLight);
            m.Box(t + V(0, 3.3f, -.95f), V(2f, .9f, .08f), Timber);
            m.Box(t + V(.95f, 3.3f, 0), V(.08f, .9f, 2f), Timber);
            m.Cylinder(t + V(0, 4.9f, 0), 1.7f, 1f, K.Hex("C9763E"), 4, .1f, Quaternion.Euler(0, 45, 0));
            m.Surface = ComicSurface.Plastic;
            Vector3 s0 = t + V(-1f, 2.35f, 0), s1 = t + V(-4.2f, .35f, 0);
            m.Box((s0 + s1) * .5f, V(.95f, .1f, (s1 - s0).magnitude), K.Hex("F6C543"), Quaternion.LookRotation(s1 - s0));
            foreach (float z in new[] { -.48f, .48f }) m.Beam(s0 + V(0, .16f, z), s1 + V(0, .16f, z), .1f, K.Hex("D9432F"));
            m.Surface = ComicSurface.PaintedMetal;
            Vector3 n0 = t + V(0, 2.3f, 1f), n1 = t + V(0, 0, 3.6f);
            foreach (float x in new[] { -.9f, -.3f, .3f, .9f }) m.Beam(n0 + V(x, 0, 0), n1 + V(x, 0, 0), .05f, Ink);
            for (int i = 1; i < 4; i++) { Vector3 c = Vector3.Lerp(n0, n1, i / 4f); m.Box(c, V(1.9f, .05f, .05f), Ink); }
        }

        /// <summary>The Mia Mias (Sandra Hill's artwork in the square's corner), far back and kept simple: five open lattice
        /// shelter frames around a slim central beacon. Background only.</summary>
        static void MiaMias(ComicMesh m, Vector3 p)
        {
            Color steel = K.Hex("8A5A3C");
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f + .3f;
                Vector3 c = p + V(Mathf.Cos(a) * 5.5f, 0, Mathf.Sin(a) * 5.5f);
                for (int k = 0; k < 4; k++)
                {
                    float b = a + Mathf.PI * .5f + (k - 1.5f) * .55f;
                    Vector3 foot = c + V(Mathf.Cos(b) * 1.5f, 0, Mathf.Sin(b) * 1.5f), apex = c + V(Mathf.Cos(a) * -.5f, 2.6f, Mathf.Sin(a) * -.5f);
                    m.Beam(foot, Vector3.Lerp(foot, apex, .55f) + (foot - c).normalized * .35f, .09f, steel);
                    m.Beam(Vector3.Lerp(foot, apex, .55f) + (foot - c).normalized * .35f, apex, .08f, steel);
                }
            }
            m.Cylinder(p + V(0, 3f, 0), .12f, 6f, K.Hex("3A4148"), 6, .6f);
            m.Cylinder(p + V(0, .1f, 0), 2.2f, .2f, K.Hex("D9D4C8"), 10);
        }

        /// <summary>Background Moreton Bay fig (no outline): a stout trunk and three broad clumps.</summary>
        static void CheapFig(ComicMesh m, Vector3 p, float s)
        {
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, 2f * s, 0), 1f * s, 4f * s, FigBark, 6, .7f);
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(p + V(0, 7.5f * s, 0), V(13f, 6f, 13f) * s, FigMid);
            m.Ellipsoid(p + V(4.5f * s, 6f * s, 1f * s), V(9f, 4.6f, 9f) * s, FigDark);
            m.Ellipsoid(p + V(-4f * s, 6.4f * s, -2f * s), V(9f, 4.6f, 9f) * s, FigDark);
            m.Ellipsoid(p + V(.5f * s, 9.6f * s, -.5f * s), V(7f, 3f, 7f) * s, FigLight);
        }

        /// <summary>The open trench across the street (the gap): shored walls, a dirt floor, a blue water main and orange
        /// conduits, spoil heaps beyond the sidewalks.</summary>
        static void Trench(CqChunkDraft d, float z0, float z1)
        {
            ComicMesh m = d.Road;
            Color dirt = K.Hex("9A7652"), dirtDark = K.Hex("7A5A3E"), sheet = K.Hex("7F868C");
            float y = -2.3f, x0 = -(K.Edge + 3.5f), x1 = K.Edge + 3.5f;
            m.Surface = ComicSurface.Concrete;
            K.Face(m, V(x0, y, z0), V(x0, y, z1), V(x1, y, z1), V(x1, y, z0), dirtDark, Vector3.up, false);
            ComicMesh w = d.SlopedFrontage;
            w.Surface = ComicSurface.Metal;
            K.MinMaxBox(w, V(x0, y - .2f, z0 - .25f), V(x1, -.03f, z0), sheet);
            K.MinMaxBox(w, V(x0, y - .2f, z1), V(x1, -.03f, z1 + .25f), sheet);
            w.Surface = ComicSurface.Plain;
            for (float x = x0 + .8f; x < x1; x += 1.6f)
            {
                K.Face(w, V(x, y, z0 + .005f), V(x + .08f, y, z0 + .005f), V(x + .08f, -.05f, z0 + .005f), V(x, -.05f, z0 + .005f), Mix(sheet, Ink, .4f), Vector3.forward, false);
                K.Face(w, V(x, y, z1 - .005f), V(x + .08f, y, z1 - .005f), V(x + .08f, -.05f, z1 - .005f), V(x, -.05f, z1 - .005f), Mix(sheet, Ink, .4f), Vector3.back, false);
            }
            foreach (int s in new[] { -1, 1 })
            {
                w.Surface = ComicSurface.Concrete;
                K.MinMaxBox(w, V(s * (K.Edge + 3.5f), y - .2f, z0 - .25f), V(s * (K.Edge + 3.75f), K.Kerb, z1 + .25f), sheet);
                w.Ellipsoid(V(s * (K.Edge + 5.6f), K.Kerb - .1f, (z0 + z1) * .5f + s), V(4f, 2.2f, 5.4f), dirt);
                w.Ellipsoid(V(s * (K.Edge + 4.9f), K.Kerb - .1f, (z0 + z1) * .5f - 1.6f * s), V(2.6f, 1.5f, 3f), dirtDark);
            }
            w.Surface = ComicSurface.Plastic;
            w.Cylinder(V(0, y + .75f, z0 + 1.6f), .32f, x1 - x0, K.Hex("2F6FB0"), 8, 1f, Quaternion.Euler(0, 0, 90));
            w.Cylinder(V(0, y + .45f, z1 - 1.3f), .16f, x1 - x0, SafetyOrange, 6, 1f, Quaternion.Euler(0, 0, 90));
            w.Cylinder(V(0, y + .45f, z1 - .9f), .16f, x1 - x0, SafetyOrange, 6, 1f, Quaternion.Euler(0, 0, 90));
            w.Surface = ComicSurface.Wood;
            for (float x = -6f; x <= 6.01f; x += 4f) w.Box(V(x, y + 1.5f, (z0 + z1) * .5f), V(.2f, .2f, z1 - z0), Timber);
        }

        /// <summary>A tracked excavator (yellow, no brand): tracks, a slewing cab and engine deck, a two-piece arm and bucket.</summary>
        static void Excavator(ComicMesh props, Vector3 p, float yaw)
        {
            var m = new ComicMesh();
            Color yellow = K.Hex("F2B63A"), dark = K.Hex("2A2F36");
            m.Surface = ComicSurface.Rubber;
            foreach (float x in new[] { -1.1f, 1.1f })
            {
                m.Box(V(x, .45f, 0), V(.6f, .9f, 3.8f), dark);
                m.Cylinder(V(x, .45f, 1.9f), .45f, .6f, dark, 8, 1f, Quaternion.Euler(0, 0, 90));
                m.Cylinder(V(x, .45f, -1.9f), .45f, .6f, dark, 8, 1f, Quaternion.Euler(0, 0, 90));
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, 1.05f, 0), V(1.6f, .3f, 1.8f), dark);
            m.Box(V(0, 1.75f, -.5f), V(2.6f, 1.1f, 3.2f), yellow);
            m.Box(V(0, 1.45f, -2f), V(2.5f, .7f, .5f), dark);
            m.Box(V(-.7f, 2.55f, .5f), V(1.1f, 1.6f, 1.5f), yellow);
            m.Surface = ComicSurface.Glass;
            m.Box(V(-.7f, 2.65f, .56f), V(1.14f, 1.1f, 1.42f), GlassDark);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(-.7f, 3.38f, .5f), V(1.2f, .1f, 1.6f), dark);
            Vector3 a = V(.5f, 2f, 1f), b = V(.5f, 4.6f, 3.6f), c = V(.5f, 2.2f, 5.8f);
            m.Box((a + b) * .5f, V(.45f, .55f, (b - a).magnitude), yellow, Quaternion.LookRotation(b - a));
            m.Box((b + c) * .5f, V(.35f, .45f, (c - b).magnitude), yellow, Quaternion.LookRotation(c - b));
            m.Beam(V(.5f, 2.6f, .2f), Vector3.Lerp(a, b, .6f) + V(0, .35f, 0), .16f, K.Hex("C9CED3"));
            m.Beam(Vector3.Lerp(a, b, .75f) + V(0, .4f, 0), Vector3.Lerp(b, c, .35f) + V(0, .35f, 0), .14f, K.Hex("C9CED3"));
            m.Box(c + V(0, -.35f, -.1f), V(1f, .8f, .9f), dark, Quaternion.Euler(25, 0, 0));
            props.Add(m, p, Quaternion.Euler(0, yaw, 0), Vector3.one);
        }

        /// <summary>A site office: a blue container cabin on blocks with a window, a door and steps.</summary>
        static void SiteOffice(ComicMesh m, Vector3 p)
        {
            Color blue = K.Hex("3E6E9E");
            m.Surface = ComicSurface.Metal;
            m.Box(p + V(0, 1.65f, 0), V(2.6f, 2.6f, 6f), blue);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 3f, 0), V(2.7f, .12f, 6.1f), White);
            for (float z = -2.6f; z <= 2.61f; z += .65f) m.Box(p + V(-1.31f, 1.65f, z), V(.04f, 2.5f, .1f), Mix(blue, Ink, .25f));
            foreach (float z in new[] { -2.4f, 2.4f }) m.Box(p + V(0, .18f, z), V(2.4f, .36f, .5f), ConcreteDark);
            m.Surface = ComicSurface.Glass;
            m.Box(p + V(-1.32f, 1.95f, 1f), V(.05f, .9f, 1.5f), GlassDark);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(-1.32f, 1.45f, -1.5f), V(.05f, 2f, .9f), White);
            m.Box(p + V(-1.7f, .22f, -1.5f), V(.6f, .16f, 1f), SteelDark);
        }

        /// <summary>A folded-up level crossing boom: striped post, the red-and-white boom pointing up, flashing lights, a
        /// crossbuck (side: which way the boom leans).</summary>
        static void BoomGate(ComicMesh m, Vector3 p, int side)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, .25f, 0), V(.7f, .5f, .7f), ConcreteDark);
            m.Cylinder(p + V(0, 2.2f, 0), .1f, 3.6f, K.Hex("C9CED3"), 8);
            m.Box(p + V(0, 1.2f, 0), V(.5f, .7f, .6f), K.Hex("C9CED3"));
            Vector3 a = p + V(0, 1.3f, .25f), dir = (Vector3.up * 4.6f + Vector3.forward * side * 1.1f);
            for (int i = 0; i < 5; i++)
            {
                Vector3 s0 = a + dir * (i / 5f), s1 = a + dir * ((i + 1) / 5f);
                m.Box((s0 + s1) * .5f, V(.1f, .16f, (s1 - s0).magnitude), i % 2 == 0 ? K.R.red : White, Quaternion.LookRotation(s1 - s0));
            }
            m.Box(p + V(0, 3.3f, 0), V(1.5f, .16f, .08f), K.Hex("20262C"));
            m.Surface = ComicSurface.Glass;
            foreach (float x in new[] { -.55f, .55f }) m.Cylinder(p + V(x, 3.3f, -.08f), .2f, .1f, Mix(K.R.red, Ink, .4f), 8, 1f, Quaternion.Euler(90, 0, 0));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 4.2f, -.02f), V(1.5f, .2f, .05f), White, Quaternion.Euler(0, 0, 30));
            m.Box(p + V(0, 4.2f, -.03f), V(1.5f, .2f, .05f), White, Quaternion.Euler(0, 0, -30));
        }

        /// <summary>The bright green SmartRider validator poles with white bands at the platform entrance.</summary>
        static void ValidatorPoles(CqChunkDraft d, float u, float z)
        {
            ComicMesh m = d.SlopedFrontage;
            foreach (float dz in new[] { -.7f, .7f })
            {
                Vector3 p = V(LX(u), PlatformTop, z + dz);
                m.Surface = ComicSurface.Plastic;
                m.Box(p + V(0, .65f, 0), V(.22f, 1.3f, .22f), K.Hex("3DBE5A"));
                m.Box(p + V(0, .95f, 0), V(.235f, .1f, .235f), White);
                m.Box(p + V(0, 1.38f, -.03f), V(.3f, .28f, .26f), K.Hex("20262C"), Quaternion.Euler(-25, 0, 0));
            }
        }

        /// <summary>The way down to the pedestrian underpass at the platform's east end: low concrete walls with a steel
        /// handrail round a ramp going down into the dark.</summary>
        static void Underpass(CqChunkDraft d, float u0, float u1, float z0, float z1)
        {
            ComicMesh m = d.SlopedFrontage;
            float xa = LX(u0), xb = LX(u1), y = PlatformTop;
            m.Surface = ComicSurface.Plain;
            K.Face(m, V(xa, y + .02f, z0), V(xb, y + .02f, z0), V(xb, y + .02f, z1), V(xa, y + .02f, z1), K.Hex("20262C"), Vector3.up, false);
            K.Face(m, V(xa - .1f, y + .03f, z0), V(xb + .1f, y + .03f, z0), V(xb + .1f, y + .03f, z0 + (z1 - z0) * .45f), V(xa - .1f, y + .03f, z0 + (z1 - z0) * .45f), K.Hex("6E747A"), Vector3.up, false);
            m.Surface = ComicSurface.Concrete;
            foreach (float x in new[] { xa, xb }) m.Box(V(x, y + .5f, (z0 + z1) * .5f), V(.2f, 1f, z1 - z0), ConcreteGrey);
            m.Box(V((xa + xb) * .5f, y + .5f, z1), V(Mathf.Abs(xa - xb) + .2f, 1f, .2f), ConcreteGrey);
            m.Surface = ComicSurface.Metal;
            foreach (float x in new[] { xa, xb }) m.Box(V(x, y + 1.15f, (z0 + z1) * .5f), V(.06f, .06f, z1 - z0), StainlessDark);
            // a green way-out board over the ramp
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V((xa + xb) * .5f, y + 2.6f, z0), V(Mathf.Abs(xa - xb) + .2f, .5f, .08f), SignGreen);
            foreach (float x in new[] { xa, xb }) m.Box(V(x, y + 1.7f, z0), V(.08f, 2.3f, .08f), SteelDark);
            PixelText(m, "WAY OUT", (xa + xb) * .5f, y + 2.6f, z0 - .045f, .055f, White);
        }

        // ---------------------------------------------------------------- McIver obstacles, vehicles and animals

        // The stations' perforated stainless bin: a round drum with a dark band of holes, a domed lid, a liner ring.
        static ComicMesh SteelBinMesh()
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.Metal;
            m.Cylinder(V(0, .5f, 0), .32f, .9f, K.Hex("C9CED3"), 10);
            m.Cylinder(V(0, .55f, 0), .33f, .45f, K.Hex("7F868C"), 10);
            m.Cylinder(V(0, .97f, 0), .35f, .08f, K.Hex("9AA2AA"), 10);
            m.Cylinder(V(0, 1.05f, 0), .3f, .1f, K.Hex("C9CED3"), 10, .5f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(V(0, .04f, 0), .34f, .08f, Ink, 10);
            return m;
        }

        // An orange water-filled road barrier (with a white one for the row beside the works): can be jumped.
        static ComicMesh WaterBarrierMesh()
        {
            var m = new ComicMesh();
            WaterBarrierArt(m, Vector3.zero, 0f, true);
            return m;
        }

        static void WaterBarrierArt(ComicMesh m, Vector3 p, float yaw, bool orange)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            Color c = orange ? SafetyOrange : White;
            m.Surface = ComicSurface.Plastic;
            m.Box(p + q * V(0, .22f, 0), V(2f, .44f, .5f), c, q);
            m.Box(p + q * V(0, .62f, 0), V(1.9f, .46f, .3f), c, q);
            m.Box(p + q * V(0, .88f, 0), V(1.8f, .08f, .22f), Mix(c, Ink, .2f), q);
            m.Surface = ComicSurface.Plain;
            foreach (float x in new[] { -.55f, .55f }) m.Box(p + q * V(x, .6f, 0), V(.45f, .2f, .32f), Mix(c, Ink, .3f), q);
            m.Box(p + q * V(0, .28f, 0), V(1.5f, .1f, .52f), orange ? White : K.R.red, q);
        }

        // The platform planters of dark-grey split-face block with a clipped shrub: too tall to jump.
        static ComicMesh BlockPlanterMesh()
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.Concrete;
            m.Box(V(0, .35f, 0), V(1.6f, .7f, 1.6f), K.Hex("5E646C"));
            m.Box(V(0, .73f, 0), V(1.7f, .08f, 1.7f), K.Hex("7F868C"));
            m.Surface = ComicSurface.Plain;
            foreach (float y in new[] { .22f, .46f }) { m.Box(V(0, y, 0), V(1.62f, .03f, 1.62f), K.Hex("3A4148")); }
            K.Plant(m, V(0, 1.1f, 0), 1.3f, Hedge);
            return m;
        }

        // An orange ROAD WORK board on a folding frame with a sandbag (punchable, not jumpable).
        static ComicMesh RoadWorkSignMesh()
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.45f, .45f })
            {
                m.Beam(V(x, 0, -.35f), V(x, 1.5f, 0f), .06f, K.Hex("C9CED3"));
                m.Beam(V(x, 0, .35f), V(x, 1.5f, 0f), .06f, K.Hex("C9CED3"));
            }
            m.Box(V(0, .95f, -.12f), V(1.1f, .8f, .04f), SafetyOrange, Quaternion.Euler(-13, 0, 0));
            m.Box(V(0, .95f, -.1f), V(1.16f, .86f, .03f), Ink, Quaternion.Euler(-13, 0, 0));
            var text = new ComicMesh();
            PixelText(text, "ROAD", 0f, .13f, 0f, .05f, Ink);
            PixelText(text, "WORK", 0f, -.17f, 0f, .05f, Ink);
            m.Add(text, V(0, .95f, -.145f), Quaternion.Euler(-13, 0, 0), Vector3.one);
            m.Surface = ComicSurface.Fabric;
            m.Box(V(0, .1f, .3f), V(.7f, .18f, .35f), K.Hex("C9B48A"));
            return m;
        }

        /// <summary>The works' big ROAD WORK AHEAD board at the edge of the sidewalk, facing the rider.</summary>
        static void RoadWorkBoard(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.8f, .8f }) m.Box(p + V(x, 1.1f, .05f), V(.08f, 2.2f, .08f), K.Hex("C9CED3"));
            m.Box(p + V(0, 2.1f, 0), V(2.2f, 1.2f, .06f), SafetyOrange);
            m.Box(p + V(0, 2.1f, .02f), V(2.3f, 1.3f, .05f), Ink);
            PixelText(m, "ROAD WORK", p.x, p.y + 2.35f, p.z - .035f, .055f, Ink);
            PixelText(m, "AHEAD", p.x, p.y + 1.85f, p.z - .035f, .055f, Ink);
        }

        // An ambulance (no service name): white van body, a red band over yellow-green checks, a light bar, dark glass.
        static ComicMesh AmbulanceMesh()
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, 1.55f, -.7f), V(2.2f, 2.3f, 4.4f), White);
            m.Box(V(0, 1.2f, 2.2f), V(2.1f, 1.6f, 1.5f), White);
            m.Box(V(0, .95f, 2.85f), V(2.05f, 1.1f, .5f), White);
            m.Surface = ComicSurface.Glass;
            m.Box(V(0, 1.75f, 2.72f), V(1.9f, .75f, .5f), GlassDark, Quaternion.Euler(-18, 0, 0));
            m.Box(V(0, 1.7f, 2.2f), V(2.14f, .6f, 1.1f), GlassDark);
            m.Box(V(0, 2f, -2.92f), V(1.5f, .7f, .04f), GlassDark);
            m.Surface = ComicSurface.Plain;
            m.Box(V(0, 1.75f, -.7f), V(2.23f, .22f, 4.42f), K.R.red);
            for (int i = 0; i < 8; i++)
                m.Box(V(0, 1.15f, -2.65f + i * .55f), V(2.235f, .42f, .55f), i % 2 == 0 ? K.Hex("D9E04A") : K.Hex("2E9E5A"));
            m.Surface = ComicSurface.Glass;
            m.Box(V(-.6f, 2.8f, 1.2f), V(.6f, .2f, .35f), K.R.red);
            m.Box(V(.6f, 2.8f, 1.2f), V(.6f, .2f, .35f), K.Hex("2F6FB0"));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, 2.8f, 1.2f), V(.6f, .16f, .3f), K.Hex("C9CED3"));
            m.Box(V(0, .4f, 3.05f), V(2.1f, .3f, .2f), Ink);
            m.Box(V(0, .4f, -2.95f), V(2.1f, .3f, .2f), Ink);
            m.Surface = ComicSurface.Rubber;
            foreach (float z in new[] { -1.9f, 1.9f }) foreach (float x in new[] { -1f, 1f })
                m.Cylinder(V(x, .4f, z), .4f, .28f, Ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            return m;
        }

        // Australian white ibis: white body, black bare head and neck, a long down-curved black beak, black tail plumes.
        static ComicMesh IbisMesh() =>
            Bird(1.5f, K.Hex("F2F0E8"), K.Hex("E4E2D8"), K.Hex("20262C"), K.Hex("20262C"), K.Hex("20262C"), .42f, .32f, K.Hex("6E5A5A"), .42f, true);

        // A city pigeon: grey with darker wings, a short beak, pink feet.
        static ComicMesh PigeonMesh() =>
            Bird(.75f, K.Hex("8E97A6"), K.Hex("5E6878"), K.Hex("6E8E86"), K.Hex("7F8896"), K.Hex("E4D6B4"), .06f, .08f, K.Hex("D98A8A"), .1f);

        // ---------------------------------------------------------------- far scenery (shared helpers, then McIver's)

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

        /// <summary>Rows of far buildings on side s between x0 and x1 (boxes with a roof cap or a hip roof and a window
        /// band) and round trees between them.</summary>
        static void FarTown(ComicMesh m, System.Random r, int s, float x0, float x1, float z0, float z1, float hMin, float hMax, Color haze, float step = 15f, bool roofs = true)
        {
            Color[] walls = { K.Hex("D9B98A"), K.Hex("EAD7AE"), K.Hex("C9CED3"), K.Hex("B98A6A"), K.Hex("E6E0D2"), K.Hex("A8452E"), K.Hex("9FB4C4") };
            Color[] caps = { Terracotta, RoofGrey, K.Hex("8A3A28"), K.Hex("B9C0C6") };
            for (float z = z0; z < z1; z += F(r, step * .8f, step * 1.3f))
            {
                float x = s * F(r, x0, x1), dist = Mathf.Sqrt(x * x + z * z), w = F(r, 10f, 18f), len = F(r, 9f, 15f), h = F(r, hMin, hMax);
                Color c = Haze(walls[r.Next(walls.Length)], dist, haze);
                m.Surface = ComicSurface.Plaster;
                m.Box(V(x, h * .5f - 1f, z), V(w, h + 2f, len), c);
                m.Surface = ComicSurface.Plain;
                if (h > 7f)
                    for (float by = 3.5f; by < h - 1.5f; by += 3.6f)
                        m.Box(V(x, by, z), V(w + .1f, 1.3f, len + .1f), Haze(Mix(WindowGlass, c, .35f), dist, haze));
                if (roofs && h < 12f) m.Cylinder(V(x, h + 1f, z), Mathf.Max(w, len) * .72f, 2.4f, Haze(caps[r.Next(caps.Length)], dist, haze), 4, .2f, Quaternion.Euler(0, 45, 0));
                else m.Box(V(x, h + .5f, z), V(w * .5f, 1.4f, len * .5f), Haze(PlantGrey, dist, haze));
                if (r.NextDouble() < .7)
                {
                    m.Surface = ComicSurface.Foliage;
                    m.Ellipsoid(V(x - s * (w * .5f + F(r, 2f, 6f)), F(r, 4f, 7f), z + F(r, -6f, 6f)), V(F(r, 8f, 13f), F(r, 7f, 10f), F(r, 8f, 13f)), Haze(Mix(GumLeaf, FigMid, F(r, 0f, 1f)), dist, haze));
                }
            }
        }

        /// <summary>The horizon around McIver: the city is in the sky behind, so this is a modest band: paving and ballast
        /// running on, rows of brick and render buildings with trees between them, taller blocks behind the railway.</summary>
        static ComicMesh MciverFar(float grade)
        {
            var m = new ComicMesh();
            var r = new System.Random(210);
            Color haze = K.Hex("C9D8E6");
            const float z0 = -220f, z1 = 820f;
            FarStrip(m, K.Edge, 60f, 330f, z1, K.Kerb - .03f, Paving);
            FarStrip(m, -K.Edge, -40f, 330f, z1, .08f, BallastC);
            FarStrip(m, -40f, -60f, 330f, z1, .08f, K.Hex("8E8A80"));
            FarStrip(m, 60f, 400f, z0, z1, -.3f, Haze(K.Hex("A9A79C"), 200f, haze));
            FarStrip(m, -60f, -400f, z0, z1, -.3f, Haze(K.Hex("9E9C92"), 200f, haze));
            FarStrip(m, 400f, 1400f, z0, z1, -.35f, Haze(K.Hex("9AAE7A"), 700f, haze));
            FarStrip(m, -400f, -1400f, z0, z1, -.35f, Haze(K.Hex("9AAE7A"), 700f, haze));
            FarTown(m, r, 1, 66f, 100f, z0, z1, 6f, 16f, haze);
            FarTown(m, r, 1, 120f, 190f, z0, z1, 10f, 34f, haze, 18f);
            FarTown(m, r, -1, 70f, 110f, z0, z1, 6f, 18f, haze);
            FarTown(m, r, -1, 130f, 220f, z0, z1, 14f, 46f, haze, 18f, false);
            FarTown(m, r, 1, 220f, 380f, z0, z1, 8f, 26f, haze, 26f);
            FarTown(m, r, -1, 240f, 380f, z0, z1, 8f, 30f, haze, 26f);
            m.Shear(grade);
            return m;
        }
    }
}
