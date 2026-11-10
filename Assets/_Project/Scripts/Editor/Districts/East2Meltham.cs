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
    /// <summary>MELTHAM (order 260): the quiet station (a long brick shelter under a tiled roof with wide eaves, fish
    /// murals, a pedestrian crossing over the track), the shared path past back fences and Hills Hoists, and classic
    /// Perth suburbia: brick-and-tile and fibro bungalows, carports, verge trees, bin day, a corner deli, a speed hump,
    /// a pocket park, and the Hotham St bridge over the line.</summary>
    public static partial class East2Builder
    {
        public const string MelId = "meltham";
        const string Mel = "Meltham";
        public static readonly string[] MelChunkIds = { "Station", "Bungalows", "Deli", "Park", "Bridge" };
        public const float MelDrop = -1.9f;
        // The speed hump outside the deli (a road-wide Ramp): numbers for gameplay.
        public const float HumpZ = 22f, HumpRise = 2.6f, HumpHeight = .5f, HumpWidth = 8f;

        static string MelPf(string name) => Prefabs(Mel) + "/" + name + ".prefab";

        static void BuildMeltham(TrackTheme theme, bool atlas)
        {
            K.Begin(Meshes(Mel), Prefabs(Mel), DataFolder);
            Obstacle bin = K.SaveObstacle(MelPf("OBS_Meltham_WheelieBin"), "OBS_Meltham_WheelieBin", Make(m => WheelieBin(m, Vector3.zero, 0f, Hex("F2C230"))), new Vector3(.62f, 1.06f, .7f), ObstacleKind.Thing, true, true, 1f);
            Obstacle trike = K.SaveObstacle(MelPf("OBS_Meltham_Trike"), "OBS_Meltham_Trike", TrikeMesh(), new Vector3(.6f, .65f, .9f), ObstacleKind.Thing, true, true, .8f);
            Obstacle couch = K.SaveObstacle(MelPf("OBS_Meltham_VergeCouch"), "OBS_Meltham_VergeCouch", VergeCouchMesh(), new Vector3(1.9f, 1f, .9f), ObstacleKind.Thing, false, false, .6f);
            Obstacle trolley = K.SaveObstacle(MelPf("OBS_Meltham_Trolley"), "OBS_Meltham_Trolley", TrolleyMesh(), new Vector3(.65f, 1.05f, 1f), ObstacleKind.Thing, false, true, .7f);
            GameObject galah = SaveAnimal(MelPf("ANM_Meltham_Galah"), "ANM_Meltham_Galah", GalahMesh(), new Vector3(0, .22f, 0), new Vector3(.35f, .45f, .55f), .4f, true);
            GameObject galahStill = SaveAnimal(MelPf("ANM_Meltham_GalahFeeding"), "ANM_Meltham_GalahFeeding", GalahMesh(), new Vector3(0, .22f, 0), new Vector3(.35f, .45f, .55f), 0f, true);
            GameObject magpie = SaveAnimal(MelPf("ANM_Meltham_Magpie"), "ANM_Meltham_Magpie", MagpieMesh(), new Vector3(0, .28f, 0), new Vector3(.4f, .56f, .7f), .5f, true);
            Ramp plywood = K.SaveRamp(MelPf("RMP_Meltham_PlywoodJump"), "RMP_Meltham_PlywoodJump", 2.2f, 3f, .8f, 0f, 1f, .5f, Hex("D2B078"), Timber, false);

            K.WriteChunk(MelEntry(), ChunkPath(Mel, "Entry"));
            K.WriteChunk(MelStation(), ChunkPath(Mel, "Station"));
            K.WriteChunk(MelBungalows(), ChunkPath(Mel, "Bungalows"));
            K.WriteChunk(MelDeli(), ChunkPath(Mel, "Deli"));
            K.WriteChunk(MelPark(), ChunkPath(Mel, "Park"));
            K.WriteChunk(MelBridge(), ChunkPath(Mel, "Bridge"));
            K.WriteChunk(MelExit(), ChunkPath(Mel, "Exit"));
            GameObject far = K.SaveFarScenery(MelPf("FAR_Meltham_Horizon"), "FAR_Meltham_Horizon", MelFar(-MelDrop / 40f));

            District d = WriteDistrict(Mel, MelId, "MELTHAM", 260, OnePass(MelChunkIds.Length), MelChunkIds);
            d.obstacles = new[] { bin, trike, couch, trolley };
            d.jumpObstacles = new[] { trike, bin };
            d.hazards = ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_Crossing", "HZ_SidewalkStroll", "HZ_PoliceStop");
            var people = new List<GameObject>(K.People(theme, "DogWalker", "DogWalker", "Kid", "Kid", "Elder", "Elder", "Jogger", "Student", "Skater", "Courier"));
            people.Add(galah); people.Add(galahStill); people.Add(magpie);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { plywood };
            d.rampFrequency = .8f;
            // A still, bright suburban afternoon: a pale warm haze, strong sun.
            d.fogColor = new Color(.74f, .82f, .90f);
            d.skyTint = new Color(1f, 1f, .99f);
            d.ambientSky = new Color(.72f, .84f, .97f);
            d.ambientEquator = new Color(.9f, .88f, .8f);
            d.ambientGround = new Color(.68f, .62f, .5f);
            d.sunColor = new Color(1f, .96f, .86f);
            d.sunIntensity = 1.5f;
            d.farScenery = far;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/Meltham] " + string.Join("; ", K.Report));
            if (!atlas) return;
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("StationShelter", null, Make(m => MelthamShelter(m, Vector3.zero, 14f))),
                ("BungalowBrickTile", null, Make(m => Bungalow(m, new CqLot(-1, -5f, 5f, 5f, 9f, 0f), 0))),
                ("BungalowFibro", null, Make(m => Bungalow(m, new CqLot(-1, -5f, 5f, 5f, 9f, 0f), 1))),
                ("BungalowCreamBrick", null, Make(m => Bungalow(m, new CqLot(-1, -5f, 5f, 5f, 9f, 0f), 2))),
                ("BungalowRenovated", null, Make(m => Bungalow(m, new CqLot(-1, -5f, 5f, 5f, 9f, 0f), 3))),
                ("CornerDeli", null, Make(m => CornerDeli(m, new CqLot(-1, -6f, 6f, 5f, 10f, 0f), null))),
                ("Carport", null, Make(m => { Carport(m, new CqLot(-1, -1.6f, 1.6f, 5f, 6f, 0f)); ParkedCar(m, V(-9f, .1f, 0f), 90f, Hex("3E5C76")); })),
                ("HillsHoist", null, Make(m => HillsHoist(m, Vector3.zero, 20f, 1))),
                ("SuperSixFence", null, Make(m => FibroFence(m, 8f, 0f, 7f, 0f, 1.8f, -1))),
                ("BackYard", null, Make(m => { GardenShed(m, V(0, 0, 0), 0f); Trampoline(m, V(4f, 0, 1f)); LemonTree(m, V(2f, 0, 4f), 1f, 1); })),
                ("PlaySet", null, Make(m => PlaySet(m, Vector3.zero))),
                ("BasketballHoop", null, Make(m => BasketballHoop(m, Vector3.zero))),
                ("DeliFreezer", null, Make(m => { IceFreezer(m, V(0, 0, 0), 0f); GasCage(m, V(2f, 0, 0), 0f); NewsRack(m, V(3.6f, 0, 0), 0f); })),
                ("LevelCrossing", null, Make(m => CrossingGates(m, V(0, 0, 0)))),
                ("BikeShelter", null, Make(m => BikeShelter(m, Vector3.zero))),
                ("PowerPole", null, Make(m => PowerPole(m, Vector3.zero, true))),
                ("StreetLight", null, Make(m => StreetLight(m, Vector3.zero, 1))),
                ("Letterboxes", null, Make(m => { Letterbox(m, V(0, 0, 0), Hex("2F6FB0"), false); Letterbox(m, V(1f, 0, 0), BrickSalmon, true); })),
                ("WheelieBin", bin.gameObject, null),
                ("Trike", trike.gameObject, null),
                ("VergeCouch", couch.gameObject, null),
                ("Trolley", trolley.gameObject, null),
                ("Galah", galah, null),
                ("Magpie", magpie, null),
                ("PlywoodJump", plywood.gameObject, null),
                ("HotamStBridge", null, Make(m => RoadBridgeArt(m, 0f, 6f, 0f, "HOTHAM ST", 3))),
                ("TrainA", null, Make(m => Train(m, 0f, 0f, -9f, 1, true))),
                ("Bottlebrush", null, Make(m => Bottlebrush(m, Vector3.zero, 1.5f, 3))),
            };
            K.RenderAtlas("art-src/districts/meltham/props-atlas.png", items);
        }

        // ================================================================================== Meltham chunks

        /// <summary>Back yards along the path on the right: a super-six fibro fence, lawns with Hills Hoists, garden sheds,
        /// lemon trees and a trampoline, the backs of the houses with their patios.</summary>
        static void BackYards(CqChunkDraft d, float z0, float z1, int seed)
        {
            SideGround(d, 1, z0, z1, 60f, LawnDry, ComicSurface.Foliage, .03f);
            ComicMesh w = d.SlopedFrontage;
            FibroFence(w, K.Edge + .3f, z0 + .1f, z1 - .1f, K.Kerb, 1.75f, -1);
            var r = new System.Random(seed * 41 + 7);
            int i = 0;
            for (float z = z0; z < z1 - 6f; z += 13.3f, i++)
            {
                float e = Mathf.Min(z1, z + 13.3f), zc = (z + e) * .5f;
                // side fence between the yards
                w.Surface = ComicSurface.Concrete;
                if (z > z0 + 1f) w.Box(V(K.Edge + 6.5f, K.Kerb + .85f, z), V(12f, 1.7f, .07f), Hex("B7B4AC"));
                HillsHoist(d.Props, V(K.Edge + F(r, 4f, 6f), d.Ground(zc), zc + F(r, -2f, 2f)), F(r, 0f, 90f), seed + i);
                int extra = (seed + i) % 3;
                if (extra == 0) GardenShed(d.Props, V(K.Edge + 2.2f, d.Ground(z + 2.4f), z + 2.4f), 0f);
                else if (extra == 1) Trampoline(d.Props, V(K.Edge + 3f, d.Ground(e - 3f), e - 3f));
                else LemonTree(d.Props, V(K.Edge + 2.4f, d.Ground(e - 2.6f), e - 2.6f), 1.1f, seed + i);
                if (i % 2 == 0) LemonTree(d.Props, V(K.Edge + 8f, d.Ground(z + 2f), z + 2f), 1f, seed + i + 9);
                else Bottlebrush(d.Props, V(K.Edge + 8.5f, d.Ground(z + 2f), z + 2f), 1.3f, seed + i);
                var lot = new CqLot(1, z + 1.6f, e - 1.6f, K.Edge + 12f, 9f, d.Base(zc));
                HouseBack(d.Frontage, lot, seed + i);
            }
            SideWall(d, 1, z0, z1, 20f, 4f);
            DeepSuburb(d, 1, z0, z1, 26f, seed + 60);
        }

        static CqChunkDraft MelEntry()
        {
            // Calm at z = 0, then the shared path beside the railway (the tracks parting for the island platform), back
            // fences on the right.
            var d = new CqChunkDraft("Meltham_Entry", MelDrop);
            SharedPath(d);
            CalmEdge(d, -1, 0f, 16f, 21, 2);
            CalmEdge(d, 1, 0f, 16f, 22, 1);
            Rail rl = FlatRail(8.8f, 13.6f);
            RailCorridor(d, rl, 16f, 40f, 23);
            FigTree(d.Props, V(-(K.Edge + 7f), d.Ground(13.5f), 13.5f), 1f, 8);
            GumTree(d.Props, V(-(K.Edge + 15f), d.Ground(15f), 15f), 13f, 9, false);
            RailEnd(d, 16.6f, 2);
            Gantry(d.SlopedFrontage, rl, 16.4f);
            Gantry(d.SlopedFrontage, rl, 34f);
            BackYards(d, 16f, 40f, 3);
            BikeSymbol(d.Road, 2f, 24f);
            PostSign(d.Props, V(K.Edge - .9f, d.Ground(18f), 18f), "MELTHAM STN", .06f, White, Hex("1F6B4A"), 2.1f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MelStation()
        {
            // Meltham station from the shared path: the pedestrian crossing and its gates at the near end, the island
            // platform with the long grey brick shelter under its tiled roof (fish murals, the red name sign), a train at
            // the far platform; back yards on the right.
            var d = new CqChunkDraft("Meltham_Station", MelDrop);
            SharedPath(d);
            BikeSymbol(d.Road, -2f, 14f);
            Rail rl = FlatRail(13.6f, 13.6f);
            RailCorridor(d, rl, 0f, 40f, 31, true, .9f, 3.1f);
            IslandPlatform(d, rl, 4f, 38f, 6.1f, 11.9f);
            ComicMesh w = d.SlopedFrontage;
            float top = rl.Level + 1.05f, xc = rl.X(9f);
            MelthamShelter(w, V(xc, top, 12f), 16f);
            PlatformLamp(w, V(xc, top, 7.5f), true);
            PlatformLamp(w, V(xc, top, 33f), true);
            Bench(w, V(xc + 1.2f, top, 32f), -90f, Hex("7FA0B0"));
            // the crossing: a paved way over the near track to the platform's ramp, gates at the path
            w.Surface = ComicSurface.Rubber;
            K.MinMaxBox(w, V(rl.X(.5f), rl.Level - .14f, 1f), V(rl.X(6.1f), rl.Level + .02f, 3f), Charcoal);
            w.Surface = ComicSurface.Concrete;
            K.Face(w, V(rl.X(6.1f), rl.Level, 1.1f), V(rl.X(6.1f), rl.Level, 2.9f), V(rl.X(9f), top, 2.9f), V(rl.X(9f), top, 1.1f), Concrete, Vector3.up);
            K.MinMaxBox(w, V(rl.X(9f), rl.Level - .4f, 1.1f), V(rl.X(11.9f), top, 4f), Hex("8F8A80"));
            Ground(d.Road, rl.X(0f), rl.X(.5f), 1f, 3f, K.Kerb + .04f, PavingRed, ComicSurface.Paving);
            CrossingGates(w, V(rl.X(.9f), K.Kerb, 2f));
            Gantry(w, rl, 8f);
            Gantry(w, rl, 30f);
            Train(w, rl.X(13.6f), rl.Level, 3f, 2, true);
            BackYards(d, 0f, 40f, 5);
            BikeShelter(d.Props, V(K.Edge - 1.6f, d.Ground(8f), 8f));
            d.Collisions.Add(new DressingObstacle { id = "BikeShelter", position = V(K.Edge - 1.6f, d.Ground(8f), 8f), rotation = Vector3.zero, centre = V(0, 1f, 0), size = V(1.8f, 2f, 3.6f), canJumpOver = false });
            d.StandardSlots();
            return d;
        }

        /// <summary>A row of bungalows with their front yards along side s (three in 40 m): each with a carport or a
        /// driveway, a letterbox, a front fence of some kind, garden beds; side fences of fibro between them.</summary>
        static void BungalowRow(CqChunkDraft d, int s, float z0, float z1, int variant, int count)
        {
            SideGround(d, s, z0, z1, 60f, LawnDry, ComicSurface.Foliage, .03f);
            float pitch = (z1 - z0) / count;
            Color[] cars = { Hex("C9CED3"), Hex("3E5C76"), Hex("B33A32"), White, Hex("4A4F57"), Hex("7A8F5A") };
            for (int i = 0; i < count; i++)
            {
                int v = variant + i;
                float a = z0 + i * pitch, drive = 3.4f, w = pitch - drive - 1f;
                bool driveFirst = v % 2 == 0;
                float h0 = driveFirst ? a + drive + .5f : a + .5f, c0 = driveFirst ? a + .2f : a + .5f + w + .1f;
                var lot = new CqLot(s, h0, h0 + w, K.Edge + 6f, 9f, d.Base(h0 + w * .5f));
                Bungalow(d.Frontage, lot, v);
                d.Wall(lot, 6f);
                // the driveway: two concrete strips with grass between, a carport at the house line, sometimes a car
                float dc = c0 + drive * .5f - .1f;
                ComicMesh g = d.Road;
                foreach (float off in new[] { -.75f, .75f })
                    Ground(g, s * K.Edge, s * (K.Edge + 12f), dc + off - .32f, dc + off + .32f, K.Kerb + .05f, PathConcrete, ComicSurface.Concrete);
                Ground(g, s * (K.Half + .3f), s * (K.Half + 2.32f), dc - 1.3f, dc + 1.3f, K.Kerb + .045f, PathConcrete, ComicSurface.Concrete);
                var port = new CqLot(s, dc - 1.6f, dc + 1.6f, K.Edge + 6.4f, 6f, d.Base(dc));
                if (v % 3 != 1) Carport(d.Frontage, port);
                if (v % 3 != 2) ParkedCar(d.Props, V(s * (K.Edge + (v % 3 == 1 ? 4.2f : 9f)), d.Ground(dc) + .02f, dc), s > 0 ? -90f : 90f, Pick(cars, v));
                // front fence and garden
                ComicMesh fw = d.SlopedFrontage;
                float f0 = driveFirst ? dc + 1.7f : a + .1f, f1 = driveFirst ? a + pitch - .1f : dc - 1.7f, gate = lot.Z0 + (v % 2 == 0 ? w * .62f : w * .38f);
                int fenceKind = v % 4;
                if (fenceKind == 0) LowWall(fw, s, f0, f1, .1f, .55f, v % 8 == 0 ? BrickSalmon : BrickCream, Cream, gate);
                else if (fenceKind == 1) PipeMeshFence(fw, s * (K.Edge + .25f), f0, f1, K.Kerb, .95f, -s, gate);
                else if (fenceKind == 2) PicketFence(fw, s, f0, f1, .22f, .85f, White, gate);
                g.Surface = ComicSurface.Paving;
                K.Face(g, V(s * K.Edge, K.Kerb + .05f, gate - .45f), V(s * K.Edge, K.Kerb + .05f, gate + .45f), V(s * (lot.Line - 1.2f), K.Kerb + .05f, gate + .45f), V(s * (lot.Line - 1.2f), K.Kerb + .05f, gate - .45f), v % 2 == 0 ? PavingRed : PathConcrete, Vector3.up, false);
                Letterbox(d.Props, V(s * (K.Edge + .55f), d.Ground(gate + .9f), gate + .9f), Pick(new[] { Hex("2F6FB0"), BrickSalmon, HeritageGreen, White }, v), fenceKind == 0);
                // garden bed along the front of the house
                g.Surface = ComicSurface.Plain;
                K.Face(g, V(s * (lot.Line - 1.1f), K.Kerb + .05f, lot.Z0 + .2f), V(s * (lot.Line - 1.1f), K.Kerb + .05f, lot.Z1 - .2f), V(s * (lot.Line - .1f), K.Kerb + .05f, lot.Z1 - .2f), V(s * (lot.Line - .1f), K.Kerb + .05f, lot.Z0 + .2f), Soil, Vector3.up, false);
                Color[] flowers = { Hex("E85A8C"), Hex("F6C543"), White, Hex("D8323A"), Jac2 };
                for (int k = 0; k < 2; k++)
                {
                    float z = lot.Z0 + w * (.2f + .55f * k);
                    if (Mathf.Abs(z - gate) < .9f) z += 1.4f;
                    FlowerBush(d.Props, V(s * (lot.Line - .7f), d.Ground(z), z), .8f, Pick(flowers, v + k), v + k);
                }
                if (v % 3 == 0) LemonTree(d.Props, V(s * (K.Edge + 2.6f), d.Ground(h0 + 1.6f), h0 + 1.6f), 1f, v);
                // the fibro side fence running back from the building line, a Hills Hoist showing behind it
                FibroFence(fw, s * (K.Edge + 6f), a + pitch - .05f, a + pitch - .05f, K.Kerb, 1.6f, 0, 9f);
                if (v % 2 == 1) HillsHoist(d.Props, V(s * (K.Edge + 13.5f), d.Ground(dc), dc), v * 20f, v);
            }
        }

        static CqChunkDraft MelBungalows()
        {
            // Whatley Crescent's quiet end: bungalows on both sides, bottlebrush and peppermint on the verges, power poles
            // and wires, bins out for collection.
            var d = new CqChunkDraft("Meltham_Bungalows", MelDrop);
            SuburbStreet(d, LawnDry);
            BungalowRow(d, -1, 0f, 40f, 0, 3);
            BungalowRow(d, 1, 0f, 40f, 5, 3);
            VergeTrees(d, 3, -8f, 19f, -30f, 36f);
            PoleLine(d, 1, 3f, 37f);
            BinDay(d, -1, 15.5f, 0);
            BinDay(d, 1, 27.8f, 1);
            DeepSuburb(d, -1, 0f, 40f, 24f, 41);
            DeepSuburb(d, 1, 0f, 40f, 24f, 42);
            d.StandardSlots();
            return d;
        }

        /// <summary>Street trees on the verges (bottlebrush, a gum now and then): zs &gt; 0 right side, &lt; 0 left.</summary>
        static void VergeTrees(CqChunkDraft d, int seed, params float[] zs)
        {
            int i = 0;
            foreach (float sz in zs)
            {
                int s = sz < 0 ? -1 : 1;
                float z = Mathf.Abs(sz);
                Vector3 p = V(s * (K.Half + 1.3f), d.Ground(z), z);
                if ((seed + i) % 3 == 0) { GumTree(d.Props, p, 9.5f, seed + i, true); K.Thin(d.Collisions, "GumTree", p, 5f, .45f); }
                else { Bottlebrush(d.Props, p, 1.55f, seed + i); K.Thin(d.Collisions, "Bottlebrush", p, 2.4f, .35f); }
                i++;
            }
        }

        /// <summary>Timber power poles on side s at z0 and z1 with three wires strung between them (wires in the road mesh).</summary>
        static void PoleLine(CqChunkDraft d, int s, float z0, float z1)
        {
            float x = s * (K.Half + .7f);
            foreach (float z in new[] { z0, z1 })
            {
                PowerPole(d.Props, V(x, d.Ground(z), z), z == z0);
                K.Thin(d.Collisions, "PowerPole", V(x, d.Ground(z), z), 8f, .35f);
            }
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            Color wire = Hex("3A4048");
            foreach (float dx in new[] { -1f, -.4f, .4f, 1f })
                for (float z = z0; z < z1 - .01f; z += 4.25f)
                {
                    float e = Mathf.Min(z1, z + 4.25f);
                    float Sag(float t) => K.Kerb + 8.3f - 1.1f * Mathf.Sin(Mathf.InverseLerp(z0, z1, t) * Mathf.PI);
                    K.Face(m, V(x + dx - .025f, Sag(z), z), V(x + dx - .025f, Sag(e), e), V(x + dx + .025f, Sag(e), e), V(x + dx + .025f, Sag(z), z), wire, Vector3.up, false);
                    K.Face(m, V(x + dx, Sag(z) - .025f, z), V(x + dx, Sag(e) - .025f, e), V(x + dx, Sag(e) + .025f, e), V(x + dx, Sag(z) + .025f, z), wire, new Vector3(-s, 0, 0), false);
                }
        }

        /// <summary>Bin day: a yellow-lid and a red-lid wheelie bin out on the verge (they block the footpath's kerb strip).</summary>
        static void BinDay(CqChunkDraft d, int s, float z, int seed)
        {
            Vector3 p = V(s * (K.Half + .95f), d.Ground(z), z);
            WheelieBin(d.Props, p, s > 0 ? 90f : -90f, Hex("F2C230"));
            WheelieBin(d.Props, p + V(0, 0, .8f), s > 0 ? 80f : -100f, seed % 2 == 0 ? Hex("D8323A") : Hex("7A9A58"));
            d.Collisions.Add(new DressingObstacle { id = "WheelieBins", position = p + V(0, 0, .4f), rotation = Vector3.zero, centre = V(0, .53f, 0), size = V(.75f, 1.06f, 1.5f), canJumpOver = true });
        }

        static CqChunkDraft MelDeli()
        {
            // The corner deli under its bullnose verandah (ice freezer, gas bottles, the paper rack, plastic strips in the
            // doorway), a speed hump across the road in front of it, bungalows on.
            var d = new CqChunkDraft("Meltham_Deli", MelDrop);
            SuburbStreet(d, LawnDry);
            SideGround(d, -1, 0f, 21f, 60f, PathConcrete, ComicSurface.Concrete, .03f);
            var deli = new CqLot(-1, 4f, 17f, K.Edge, 10f, d.Base(10.5f));
            CornerDeli(d.Frontage, deli, d);
            d.Wall(deli, 6f);
            Ground(d.Road, -(K.Half + .3f), -(K.Edge - .02f), 2f, 19f, K.Kerb + .04f, PavingRed, ComicSurface.Paving);
            ComicMesh p = d.Props;
            IceFreezer(p, V(-(K.Edge - .55f), d.Ground(6.2f), 6.2f), -90f);
            NewsRack(p, V(-(K.Edge - .45f), d.Ground(13.2f), 13.2f), -90f);
            GasCage(p, V(-(K.Edge - .6f), d.Ground(15.6f), 15.6f), -90f);
            d.Collisions.Add(new DressingObstacle { id = "DeliFront", position = V(-(K.Edge - .55f), d.Ground(6.2f), 6.2f), rotation = Vector3.zero, centre = V(0, .5f, 0), size = V(.9f, 1f, 1.6f), canJumpOver = false });
            d.Collisions.Add(new DressingObstacle { id = "DeliFront", position = V(-(K.Edge - .55f), d.Ground(14.4f), 14.4f), rotation = Vector3.zero, centre = V(0, .7f, 0), size = V(.9f, 1.4f, 3.2f), canJumpOver = false });
            K.PropFromKit(d.Props, d.Collisions, "Mailbox", V(-(K.Half + .8f), d.Ground(2.6f), 2.6f), 90f);
            K.PropFromKit(d.Props, d.Collisions, "Bin", V(-(K.Half + .8f), d.Ground(18.4f), 18.4f), 0f);
            StreetNameSign(d.Props, V(-(K.Edge - .5f), d.Ground(1.2f), 1.2f), "WHATLEY CR", "", -1);
            // the deli's yard and car park behind a fibro fence, then one more bungalow on this side
            FibroFence(d.SlopedFrontage, -(K.Edge + .3f), 17.4f, 21f, K.Kerb, 1.7f, 1);
            BungalowRow(d, -1, 21f, 40f, 9, 1);
            BungalowRow(d, 1, 0f, 40f, 12, 3);
            // the hump: a red-brick paved rise across the road with white arrow markings and a warning sign
            var spec = new CqRampSpec { Name = "SpeedHump", X = 0f, Z = HumpZ, Width = HumpWidth, Rise = HumpRise, Height = HumpHeight, Top = 0f, Launch = 1f, Hang = .6f };
            d.Ramps.Add(spec);
            SpeedHump(d, spec);
            HumpSign(d.Props, V(K.Half + .7f, d.Ground(15f), 15f));
            K.Thin(d.Collisions, "HumpSign", V(K.Half + .7f, d.Ground(15f), 15f), 2.6f, .3f);
            VergeTrees(d, 7, 9f, -33f, 37f);
            PoleLine(d, 1, 2f, 30f);
            BinDay(d, 1, 21.5f, 2);
            DeepSuburb(d, -1, 0f, 40f, 24f, 51);
            DeepSuburb(d, 1, 0f, 40f, 24f, 52);
            foreach (float x in new[] { -2.6f, 0f, 2.6f }) { d.Slot(x, 10f); d.Slot(x, 36f); }
            return d;
        }

        /// <summary>The speed hump (the Ramp's art): a brick-paved rise over the whole road, a short drop on the far side,
        /// white triangle markings on the approach.</summary>
        static void SpeedHump(CqChunkDraft d, CqRampSpec r)
        {
            ComicMesh m = d.Road;
            float x0 = -K.Half, x1 = K.Half, z0 = r.Z, z1 = r.Z + r.Rise, h = r.Height;
            m.Surface = ComicSurface.Paving;
            for (int i = 0; i < 4; i++)
            {
                float t0 = i / 4f, t1 = (i + 1) / 4f;
                K.Face(m, V(x0, h * t0, Mathf.Lerp(z0, z1, t0)), V(x0, h * t1, Mathf.Lerp(z0, z1, t1)), V(x1, h * t1, Mathf.Lerp(z0, z1, t1)), V(x1, h * t0, Mathf.Lerp(z0, z1, t0)), i % 2 == 0 ? PavingRed : PavingRedDark, Vector3.up, false);
            }
            K.Face(m, V(x0, 0f, z1), V(x1, 0f, z1), V(x1, h, z1), V(x0, h, z1), PavingRedDark, Vector3.forward, false);
            m.Surface = ComicSurface.Plain;
            K.Face(m, V(x0, h + .012f, z1 - .28f), V(x0, h + .012f, z1), V(x1, h + .012f, z1), V(x1, h + .012f, z1 - .28f), K.R.yellow, Vector3.up, false);
            for (float x = -3.2f; x <= 3.21f; x += 1.6f)
            {
                float y0 = h * .12f + .012f, y1 = h * .62f + .012f;
                K.TriFacing(m, V(x - .5f, y0, z0 + r.Rise * .12f), V(x + .5f, y0, z0 + r.Rise * .12f), V(x, y1, z0 + r.Rise * .62f), White, Vector3.up);
            }
        }

        /// <summary>The yellow diamond warning sign for the hump.</summary>
        static void HumpSign(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 1.2f, 0), .04f, 2.4f, Mast, 6);
            Quaternion q = Quaternion.Euler(0, 0, 45f);
            m.Box(p + V(0, 2.3f, -.03f), V(.86f, .86f, .04f), Ink, q);
            m.Box(p + V(0, 2.3f, -.05f), V(.76f, .76f, .04f), K.R.yellow, q);
            m.Surface = ComicSurface.Plain;
            // a hump pictogram: a black bump on a line
            K.Face(m, p + V(-.3f, 2.16f, -.075f), p + V(.3f, 2.16f, -.075f), p + V(.3f, 2.21f, -.075f), p + V(-.3f, 2.21f, -.075f), Ink, Vector3.back, false);
            K.Face(m, p + V(-.2f, 2.21f, -.075f), p + V(.2f, 2.21f, -.075f), p + V(.1f, 2.38f, -.075f), p + V(-.1f, 2.38f, -.075f), Ink, Vector3.back, false);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 1.55f, -.03f), V(.7f, .3f, .04f), K.R.yellow);
            Text(m, "HUMP", p + V(0, 1.55f, -.055f), Vector3.right, Vector3.up, .04f, Ink);
        }

        static CqChunkDraft MelPark()
        {
            // A pocket park on the right: big gums, a play set, a basketball half-court, galahs on the lawn; bungalows on
            // the left.
            var d = new CqChunkDraft("Meltham_Park", MelDrop);
            SuburbStreet(d, LawnDry);
            BungalowRow(d, -1, 0f, 40f, 16, 3);
            SideGround(d, 1, 0f, 40f, 60f, Lawn, ComicSurface.Foliage, .03f);
            ComicMesh w = d.SlopedFrontage;
            // a low log barrier along the footpath with an opening, a path into the park
            w.Surface = ComicSurface.Wood;
            for (float z = .6f; z < 39.6f; z += 2.4f)
            {
                if (Mathf.Abs(z - 20f) < 1.6f) continue;
                w.Box(V(K.Edge + .35f, K.Kerb + .3f, z), V(.2f, .6f, .2f), TimberDark);
                if (Mathf.Abs(z + 2.4f - 20f) >= 1.6f && z + 2.4f < 39.6f) w.Box(V(K.Edge + .35f, K.Kerb + .42f, z + 1.2f), V(.14f, .14f, 2.4f), Timber);
            }
            Ground(d.Road, K.Edge, K.Edge + 14f, 19.2f, 20.8f, K.Kerb + .05f, PathConcrete, ComicSurface.Concrete);
            // soft-fall under the play set, the court
            Ground(d.Road, K.Edge + 4f, K.Edge + 13f, 3f, 15f, K.Kerb + .05f, Hex("D9B26A"), ComicSurface.Rubber);
            PlaySet(d.Frontage, V(K.Edge + 8.5f, d.Ground(9f) + .05f, 9f));
            Ground(d.Road, K.Edge + 4f, K.Edge + 15f, 24f, 37f, K.Kerb + .05f, Hex("4A8FA8"), ComicSurface.Asphalt);
            d.Road.Surface = ComicSurface.Plain;
            float cy = K.Kerb + .06f;
            foreach ((float a, float b, float c, float e) in new[] { (4.3f, 24.3f, 14.7f, 24.42f), (4.3f, 36.58f, 14.7f, 36.7f), (4.3f, 24.3f, 4.42f, 36.7f), (14.58f, 24.3f, 14.7f, 36.7f), (10f, 28f, 14.7f, 28.12f), (10f, 32.88f, 14.7f, 33f), (10f, 28f, 10.12f, 33f) })
                K.Face(d.Road, V(K.Edge + a, cy, b), V(K.Edge + a, cy, e), V(K.Edge + c, cy, e), V(K.Edge + c, cy, b), White, Vector3.up, false);
            BasketballHoop(d.Frontage, V(K.Edge + 14.9f, d.Ground(30.5f), 30.5f));
            GumTree(d.Props, V(K.Edge + 3.4f, d.Ground(17.4f), 17.4f), 14f, 61, false);
            GumTree(d.Props, V(K.Edge + 16f, d.Ground(21f), 21f), 16f, 62, false);
            GumTree(d.Props, V(K.Edge + 15f, d.Ground(4f), 4f), 13f, 63, false);
            Bench(d.Props, V(K.Edge + 2.6f, d.Ground(22.6f), 22.6f), -90f, Timber);
            Bench(d.Props, V(K.Edge + 13.6f, d.Ground(16.6f), 16.6f), 180f, Timber);
            PostSign(d.Props, V(K.Edge + 1.3f, d.Ground(17.7f), 17.7f), "RESERVE", .07f, Cream, Hex("1F6B4A"), 1.5f);
            // galahs feeding on the lawn (scenery)
            var r = new System.Random(64);
            for (int i = 0; i < 4; i++)
                d.Props.Add(GalahMesh(), V(K.Edge + F(r, 2f, 3.6f), d.Ground(28f + i * 1.3f), 28f + i * 1.3f + F(r, -.4f, .4f)), Quaternion.Euler(0, F(r, 0f, 360f), 0), Vector3.one);
            SideWall(d, 1, 0f, 40f, 18f, 3f);
            VergeTrees(d, 11, -6f, 12f, -26f, 34f);
            PoleLine(d, -1, 3f, 37f);
            BinDay(d, -1, 28.6f, 3);
            Vector3 light = V(K.Half + .6f, d.Ground(22f), 22f);
            StreetLight(d.Props, light, -1);
            K.Thin(d.Collisions, "StreetLight", light, 7.5f, .3f);
            DeepSuburb(d, -1, 0f, 40f, 24f, 71);
            DeepSuburb(d, 1, 0f, 40f, 30f, 72);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MelBridge()
        {
            // Under the Hotham St bridge and back beside the railway: a train passing on the near track, a limestone wall
            // covered in graffiti on the right with back yards behind it.
            var d = new CqChunkDraft("Meltham_Bridge", MelDrop);
            SharedPath(d);
            Rail rl = FlatRail(8.8f, 8.8f);
            RailCorridor(d, rl, 2.2f, 40f, 81);
            SideGround(d, -1, 0f, 2.2f, 60f, LawnDry, ComicSurface.Foliage, .03f);
            RoadBridge(d, 2f, 8f, "HOTHAM ST", 3, -1);
            Gantry(d.SlopedFrontage, rl, 14f);
            Gantry(d.SlopedFrontage, rl, 34f);
            Train(d.SlopedFrontage, rl.X(4.4f), rl.Level, 3.4f, 2, true);
            // right: the graffiti wall, then yards
            GraffitiWall(d, 9f, 40f, 4);
            SideGround(d, 1, 0f, 40f, 60f, LawnDry, ComicSurface.Foliage, .03f);
            var r = new System.Random(82);
            for (float z = 12f; z < 40f; z += 9f)
            {
                var lot = new CqLot(1, z, z + 7.5f, K.Edge + 11f, 9f, d.Base(z + 3.75f));
                HouseBack(d.Frontage, lot, (int)z);
                HillsHoist(d.Props, V(K.Edge + F(r, 4f, 6f), d.Ground(z + 4f), z + 4f), F(r, 0f, 90f), (int)z);
            }
            GumTree(d.Props, V(K.Edge + 4f, d.Ground(11f), 11f), 12f, 83, false);
            SideWall(d, 1, 8f, 40f, 20f, 4f);
            DeepSuburb(d, 1, 8f, 40f, 24f, 84);
            BikeSymbol(d.Road, -2f, 20f);
            d.StandardSlots();
            return d;
        }

        /// <summary>A limestone block wall along the path's right edge with graffiti: big bubble pieces in bright colours
        /// (shapes only, no words), drips and outlines.</summary>
        static void GraffitiWall(CqChunkDraft d, float z0, float z1, int seed)
        {
            ComicMesh w = d.SlopedFrontage;
            w.Surface = ComicSurface.Concrete;
            K.SideBox(w, 1, z0, z1, -1f, K.Kerb + 2.3f, .1f, .5f, Limestone);
            K.SideBox(w, 1, z0, z1, K.Kerb + 2.3f, K.Kerb + 2.42f, .05f, .55f, LimeCap);
            float x = K.Edge + .1f - .012f;
            var r = new System.Random(seed * 59 + 1);
            Color[] paint = { Hex("E2463A"), Hex("F6C543"), Hex("2A9D9A"), Hex("2F6FB0"), Jac2, Hex("F28C28"), Hex("E85A8C"), Hex("7FC66A"), White };
            w.Surface = ComicSurface.Plain;
            for (float z = z0 + 1f; z < z1 - 3f; z += F(r, 3.6f, 5.2f))
            {
                // one piece: a dark backing blob, three or four fat letter-like blocks, a highlight stripe
                float len = F(r, 2.4f, 3.4f), y0 = K.Kerb + F(r, .25f, .5f), h = F(r, 1.1f, 1.6f);
                Color a = Pick(paint, r.Next(99)), b = Pick(paint, r.Next(99));
                K.Face(w, V(x, y0 - .12f, z - .15f), V(x, y0 - .12f, z + len + .15f), V(x, y0 + h + .15f, z + len + .1f), V(x, y0 + h + .1f, z - .1f), Ink, Vector3.left, false);
                int blocks = r.Next(3, 5);
                for (int i = 0; i < blocks; i++)
                {
                    float a0 = z + len * i / blocks + .06f, a1 = z + len * (i + 1) / blocks - .04f, tilt = F(r, -.12f, .12f);
                    K.Face(w, V(x - .012f, y0, a0), V(x - .012f, y0 + tilt, a1), V(x - .012f, y0 + h + tilt, a1 + .06f), V(x - .012f, y0 + h, a0 + .06f), i % 2 == 0 ? a : b, Vector3.left, false);
                    K.Face(w, V(x - .024f, y0 + h * .58f, a0 + .08f), V(x - .024f, y0 + h * .58f + tilt, a1 - .08f), V(x - .024f, y0 + h * .72f + tilt, a1 - .08f), V(x - .024f, y0 + h * .72f, a0 + .08f), Mix(i % 2 == 0 ? a : b, White, .55f), Vector3.left, false);
                }
            }
        }

        static CqChunkDraft MelExit()
        {
            // The path and the railway a little further, then calm towards z = 40 (trees hide where the tracks go on).
            var d = new CqChunkDraft("Meltham_Exit", MelDrop);
            SharedPath(d);
            Rail rl = FlatRail(8.8f, 8.8f);
            RailCorridor(d, rl, 0f, 26f, 91);
            Gantry(d.SlopedFrontage, rl, 6f);
            Gantry(d.SlopedFrontage, rl, 25.6f);
            BackYards(d, 0f, 26f, 8);
            CalmEdge(d, -1, 26f, 40f, 23, 2);
            CalmEdge(d, 1, 26f, 40f, 24, 1);
            FigTree(d.Props, V(-(K.Edge + 7f), d.Ground(28.5f), 28.5f), 1f, 10);
            GumTree(d.Props, V(-(K.Edge + 15f), d.Ground(27.5f), 27.5f), 13f, 11, false);
            RailEnd(d, 25.4f, 3);
            d.StandardSlots();
            return d;
        }

        // ================================================================================== the station

        /// <summary>Meltham's platform building: a long, low building of grey-painted brick under a terracotta tile roof
        /// whose wide flat-soffited eaves shelter both platforms; platform boards hang at the gable end facing the rider;
        /// fish murals (the station's public art) and the red name sign on the side facing the path; a red painted door
        /// on the end. p = platform surface at the building's near end on the centre line.</summary>
        static void MelthamShelter(ComicMesh m, Vector3 p, float len)
        {
            const float hw = 1.7f, wall = 3.1f, eave = 4.3f, ridge = 5f;
            Color grey = Hex("B9BAC6"), greyDark = Hex("9FA0AE");
            m.Surface = ComicSurface.Brick;
            m.Box(p + V(0, wall * .5f, len * .5f), V(hw * 2f, wall, len), grey);
            m.Box(p + V(0, .3f, len * .5f), V(hw * 2f + .06f, .6f, len + .06f), greyDark);
            // the flat soffit and the tiled roof over it
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, wall + .14f, len * .5f), V(eave * 2f, .28f, len + 1.6f), White);
            m.Surface = ComicSurface.Brick;
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 r0 = p + V(0, ridge, -.8f), r1 = p + V(0, ridge, len + .8f), e0 = p + V(s * (eave + .1f), wall + .28f, -.8f), e1 = p + V(s * (eave + .1f), wall + .28f, len + .8f);
                K.Face(m, r0, r1, e1, e0, Tile, new Vector3(s, 3, 0));
            }
            foreach (int e in new[] { 0, 1 })
            {
                float z = e == 0 ? -.8f : len + .8f;
                m.Surface = ComicSurface.Plaster;
                K.TriFacing(m, p + V(-eave - .1f, wall + .28f, z), p + V(eave + .1f, wall + .28f, z), p + V(0, ridge, z), White, e == 0 ? Vector3.back : Vector3.forward);
            }
            m.Surface = ComicSurface.Brick;
            m.Box(p + V(0, ridge + .03f, len * .5f), V(.3f, .12f, len + 1.6f), Hex("9E4A32"));
            // platform boards under the eaves at the rider's end, and the painted red door on the end wall
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 c = p + V(s * (hw + 1.35f), 2.42f, -.4f);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(c, V(1.9f, 1.3f, .08f), Charcoal);
                m.Surface = ComicSurface.Plain;
                K.Face(m, c + V(-.9f, -.2f, -.05f), c + V(.9f, -.2f, -.05f), c + V(.9f, .58f, -.05f), c + V(-.9f, .58f, -.05f), Hex("B32F45"), Vector3.back, false);
                K.Face(m, c + V(-.9f, -.58f, -.05f), c + V(.9f, -.58f, -.05f), c + V(.9f, -.2f, -.05f), c + V(-.9f, -.2f, -.05f), TrainGreen, Vector3.back, false);
                Text(m, s < 0 ? "2" : "1", c + V(0, .2f, -.065f), Vector3.right, Vector3.up, .11f, White);
            }
            m.Surface = ComicSurface.Plain;
            K.Face(m, p + V(-.75f, 0f, -.012f), p + V(.75f, 0f, -.012f), p + V(.75f, 2.5f, -.012f), p + V(-.75f, 2.5f, -.012f), greyDark, Vector3.back);
            K.Face(m, p + V(-.6f, .05f, -.024f), p + V(.6f, .05f, -.024f), p + V(.6f, 2.35f, -.024f), p + V(-.6f, 2.35f, -.024f), Hex("2C4A8E"), Vector3.back, false);
            K.Face(m, p + V(-.05f, .05f, -.036f), p + V(.55f, .05f, -.036f), p + V(.55f, 1.9f, -.036f), p + V(-.05f, 1.9f, -.036f), Hex("D8483A"), Vector3.back, false);
            // the side facing the path (+X): two fish murals, the name sign between them, a door, a window; plain on the far side
            foreach (int s in new[] { -1, 1 })
            {
                float x = s * (hw + .012f);
                Vector3 n = new Vector3(s, 0, 0);
                foreach (float zc in new[] { len * .2f, len * .62f })
                {
                    m.Surface = ComicSurface.Plain;
                    float mw = zc < len * .4f ? 1.5f : .9f;
                    K.Face(m, p + V(x, .55f, zc - mw), p + V(x, .55f, zc + mw), p + V(x, 2.75f, zc + mw), p + V(x, 2.75f, zc - mw), Ink, n);
                    K.Face(m, p + V(x + s * .012f, .65f, zc - mw + .08f), p + V(x + s * .012f, .65f, zc + mw - .08f), p + V(x + s * .012f, 1.6f, zc + mw - .08f), p + V(x + s * .012f, 1.6f, zc - mw + .08f), Hex("2A9D9A"), n, false);
                    K.Face(m, p + V(x + s * .012f, 1.6f, zc - mw + .08f), p + V(x + s * .012f, 1.6f, zc + mw - .08f), p + V(x + s * .012f, 2.65f, zc + mw - .08f), p + V(x + s * .012f, 2.65f, zc - mw + .08f), Hex("2C4A8E"), n, false);
                    for (int f = 0; f < 3; f++)
                    {
                        // a fish: a pale lozenge body, a tail, a dark eye
                        float fy = .95f + f * .62f, fz = zc + (f % 2 == 0 ? -.15f : .2f) * mw, fl = mw * .62f, dir = f % 2 == 0 ? 1f : -1f;
                        float xf = x + s * .024f;
                        K.Face(m, p + V(xf, fy, fz - fl), p + V(xf, fy - .2f, fz), p + V(xf, fy, fz + fl), p + V(xf, fy + .2f, fz), White, n, false);
                        K.TriFacing(m, p + V(xf, fy, fz - dir * fl * .8f), p + V(xf, fy + .2f, fz - dir * fl * 1.25f), p + V(xf, fy - .2f, fz - dir * fl * 1.25f), White, n);
                        K.Face(m, p + V(xf + s * .012f, fy + .02f, fz + dir * fl * .5f - .04f), p + V(xf + s * .012f, fy + .02f, fz + dir * fl * .5f + .04f), p + V(xf + s * .012f, fy + .1f, fz + dir * fl * .5f + .04f), p + V(xf + s * .012f, fy + .1f, fz + dir * fl * .5f - .04f), Ink, n, false);
                    }
                }
                // door and window further along
                m.Surface = ComicSurface.PaintedMetal;
                K.Face(m, p + V(x, 0f, len * .8f - .5f), p + V(x, 0f, len * .8f + .5f), p + V(x, 2.2f, len * .8f + .5f), p + V(x, 2.2f, len * .8f - .5f), greyDark, n);
                K.Face(m, p + V(x + s * .012f, .05f, len * .8f - .42f), p + V(x + s * .012f, .05f, len * .8f + .42f), p + V(x + s * .012f, 2.1f, len * .8f + .42f), p + V(x + s * .012f, 2.1f, len * .8f - .42f), Hex("5B6B8A"), n, false);
                K.Face(m, p + V(x, 1.9f, len * .92f - .5f), p + V(x, 1.9f, len * .92f + .5f), p + V(x, 2.6f, len * .92f + .5f), p + V(x, 2.6f, len * .92f - .5f), White, n);
                m.Surface = ComicSurface.Glass;
                K.Face(m, p + V(x + s * .012f, 1.97f, len * .92f - .43f), p + V(x + s * .012f, 1.97f, len * .92f + .43f), p + V(x + s * .012f, 2.53f, len * .92f + .43f), p + V(x + s * .012f, 2.53f, len * .92f - .43f), GlassDark, n, false);
            }
            // the red name sign, white letters, in its pale frame (facing the path)
            float bw = TextCols("MELTHAM") * .09f + .4f;
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(hw + .05f, 2.2f, len * .42f), V(.06f, .7f, bw + .14f), Hex("D9D4E0"));
            m.Box(p + V(hw + .08f, 2.2f, len * .42f), V(.04f, .58f, bw), Hex("C9304A"));
            Text(m, "MELTHAM", p + V(hw + .105f, 2.2f, len * .42f), Vector3.forward, Vector3.up, .09f, White);
            Bench(m, p + V(hw + .6f, 0, len * .42f), -90f, Hex("7FA0B0"));
            Bench(m, p + V(-hw - .6f, 0, len * .55f), 90f, Hex("7FA0B0"));
        }

        /// <summary>The pedestrian crossing's maze gates: grey rails, a yellow self-closing gate, a red warning light on a post.</summary>
        static void CrossingGates(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { -1.1f, 1.1f })
            {
                m.Box(p + V(0, .55f, z), V(.08f, 1.1f, .08f), Mast);
                m.Box(p + V(-.9f, .55f, z), V(.08f, 1.1f, .08f), Mast);
                m.Box(p + V(-.45f, 1.06f, z), V(.98f, .07f, .07f), Mast);
                m.Box(p + V(-.45f, .55f, z), V(.98f, .07f, .07f), Mast);
            }
            m.Box(p + V(0, .9f, 0), V(.07f, .07f, 1.4f), K.R.yellow);
            m.Box(p + V(0, .45f, 0), V(.07f, .07f, 1.4f), K.R.yellow);
            m.Box(p + V(0, .68f, .68f), V(.07f, .6f, .07f), K.R.yellow);
            m.Cylinder(p + V(-.9f, 1.6f, -1.1f), .05f, 3.2f, Mast, 6);
            m.Box(p + V(-.9f, 3.1f, -1.16f), V(.5f, .5f, .1f), Ink);
            m.Surface = ComicSurface.Plastic;
            m.Box(p + V(-.9f, 3.1f, -1.22f), V(.26f, .26f, .04f), Hex("D8323A"));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(-.9f, 2.5f, -1.16f), V(.7f, .34f, .05f), White);
            Text(m, "LOOK", p + V(-.9f, 2.5f, -1.19f), Vector3.right, Vector3.up, .04f, Hex("D8323A"));
        }

        /// <summary>The bike shelter: a curved roof on two hoops, three bikes' worth of rails.</summary>
        static void BikeShelter(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { -1.6f, 1.6f })
            {
                m.Box(p + V(.7f, 1.1f, z), V(.08f, 2.2f, .08f), Mast);
                m.Beam(p + V(.7f, 2.2f, z), p + V(-.1f, 2.5f, z), .08f, Mast);
                m.Beam(p + V(-.1f, 2.5f, z), p + V(-.9f, 2.3f, z), .08f, Mast);
            }
            m.Surface = ComicSurface.Metal;
            K.Face(m, p + V(.75f, 2.24f, -1.8f), p + V(.75f, 2.24f, 1.8f), p + V(-.1f, 2.56f, 1.8f), p + V(-.1f, 2.56f, -1.8f), Hex("7FA0B0"), Vector3.up);
            K.Face(m, p + V(-.1f, 2.56f, -1.8f), p + V(-.1f, 2.56f, 1.8f), p + V(-.95f, 2.34f, 1.8f), p + V(-.95f, 2.34f, -1.8f), Hex("9BB6C4"), Vector3.up);
            K.Face(m, p + V(.75f, 2.2f, -1.8f), p + V(.75f, 2.2f, 1.8f), p + V(-.95f, 2.3f, 1.8f), p + V(-.95f, 2.3f, -1.8f), Hex("5F7F90"), Vector3.down);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { -1f, 0f, 1f })
            {
                m.Box(p + V(.2f, .45f, z), V(.07f, .9f, .07f), Ink);
                m.Box(p + V(-.3f, .45f, z), V(.07f, .9f, .07f), Ink);
                m.Box(p + V(-.05f, .88f, z), V(.57f, .07f, .07f), Ink);
            }
        }

        // ================================================================================== houses and yards

        /// <summary>
        /// A Perth bungalow, front towards the road. v picks one of four kinds: 0 a 1950s salmon-brick-and-tile house (a
        /// projecting front room, a porch on a wrought-iron post, wide steel windows, a striped canvas awning, a squat
        /// chimney); 1 a 1940s fibro cottage on stumps (cover battens, a faded iron roof, a small gabled porch, window
        /// hoods); 2 a cream-brick house of the 1960s (low grey-tiled roof, a long aluminium window, a brown feature
        /// wall); 3 a renovated one (white-painted brick, dark iron roof, a timber screen). All have a TV antenna or a
        /// rooftop air conditioner.
        /// </summary>
        static void Bungalow(ComicMesh m, CqLot l, int v)
        {
            int kind = ((v % 4) + 4) % 4;
            bool mirror = (v / 4) % 2 == 1;
            float W = l.W, D = l.Depth, floor = kind == 1 ? .6f : .3f, eave = floor + 2.75f;
            Color wall = new[] { BrickSalmon, Hex("E4E0D2"), BrickCream, White }[kind];
            Color trim = kind == 3 ? Charcoal : White;
            Color roof = new[] { Tile, Hex("8E5A50"), Hex("8C8F92"), Hex("4A5057") }[kind];
            if (v % 8 >= 4 && kind == 0) { wall = BrickOrange; roof = TileBrown; }
            if (v % 8 >= 4 && kind == 1) { wall = Hex("C9D6DA"); roof = RoofGreen; }
            ComicSurface wallSurface = kind == 1 ? ComicSurface.Plaster : ComicSurface.Brick, roofSurface = kind == 0 || kind == 2 ? ComicSurface.Brick : ComicSurface.Metal;
            float A(float a) => mirror ? W - a : a;
            void Box(float a0, float a1, float u0, float u1, float o0, float o1, Color c) => K.LBox(m, l, Mathf.Min(A(a0), A(a1)), Mathf.Max(A(a0), A(a1)), u0, u1, o0, o1, c);
            float roomW = W * .46f, ro = kind == 2 ? 0f : 1.6f;
            // base: brick footing, or stumps with a lattice skirt for the fibro cottage
            m.Surface = kind == 1 ? ComicSurface.Wood : ComicSurface.Brick;
            K.LBox(m, l, 0, W, -2.4f, floor, .04f, -D, kind == 1 ? Hex("6E5A47") : Mix(wall, Ink, .3f));
            if (ro > 0f) Box(W - roomW, W, -2.4f, floor, ro + .04f, 0f, kind == 1 ? Hex("6E5A47") : Mix(wall, Ink, .3f));
            m.Surface = wallSurface;
            K.LBox(m, l, 0, W, floor, eave, 0f, -D, wall);
            if (ro > 0f) Box(W - roomW, W, floor, eave, ro, 0f, wall);
            if (kind == 1)
            {
                // fibro sheets: cover battens
                m.Surface = ComicSurface.Plain;
                for (float a = .9f; a < W - roomW - .2f; a += .9f) K.LQuad(m, l, A(a) - .025f, A(a) + .025f, floor + .05f, eave - .05f, .012f, Mix(wall, Ink, .18f), false);
                for (float a = W - roomW + .6f; a < W - .2f; a += .9f) K.LQuad(m, l, A(a) - .025f, A(a) + .025f, floor + .05f, eave - .05f, ro + .012f, Mix(wall, Ink, .18f), false);
            }
            if (kind == 2)
            {
                m.Surface = ComicSurface.Brick;
                Box(W - 2.2f, W, floor, eave, .08f, 0f, Hex("8A5A3C"));                                    // the feature wall
            }
            // roofs: eaves board, the main hip, a second hip over the front room
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.5f, W + .5f, eave - .1f, eave + .04f, .5f, -D - .5f, trim);
            m.Surface = roofSurface;
            float rise = kind == 2 ? 1.5f : 2.1f;
            HipRoof(m, l, -.55f, W + .55f, eave + .04f, eave + rise, .55f, -D - .55f, roof);
            if (ro > 0f)
            {
                float g0 = Mathf.Min(A(W - roomW), A(W)), g1 = Mathf.Max(A(W - roomW), A(W));
                m.Surface = ComicSurface.PaintedMetal;
                K.LBox(m, l, g0 - .45f, g1 + .45f, eave - .1f, eave + .04f, ro + .45f, 0f, trim);
                m.Surface = roofSurface;
                HipRoof(m, l, g0 - .5f, g1 + .5f, eave + .04f, eave + rise * .82f, ro + .5f, -D * .45f, roof);
            }
            // the front room's window (wide, three panes) with an awning or a hood
            float rc = A(W - roomW * .5f), rww = Mathf.Min(2.6f, roomW - 1.2f);
            WideWindow(m, l, rc, rww, floor + .95f, floor + 2.25f, ro, trim, kind == 3 ? Charcoal : White);
            if (kind == 0)
            {
                // a striped canvas awning
                m.Surface = ComicSurface.Fabric;
                int stripes = 6;
                for (int i = 0; i < stripes; i++)
                {
                    float a0 = rc - rww * .5f - .1f + (rww + .2f) * i / stripes, a1 = rc - rww * .5f - .1f + (rww + .2f) * (i + 1) / stripes;
                    K.Face(m, l.P(a0, floor + 2.55f, ro + .02f), l.P(a1, floor + 2.55f, ro + .02f), l.P(a1, floor + 2f, ro + .8f), l.P(a0, floor + 2f, ro + .8f), i % 2 == 0 ? HeritageGreen : White, new Vector3(-l.S, 1, 0));
                }
                K.LBox(m, l, rc - rww * .5f - .1f, rc + rww * .5f + .1f, floor + 1.86f, floor + 2f, .84f + ro, .78f + ro, HeritageGreen);
            }
            else
            {
                m.Surface = ComicSurface.PaintedMetal;
                K.LBox(m, l, rc - rww * .5f - .15f, rc + rww * .5f + .15f, floor + 2.4f, floor + 2.5f, ro + .5f, ro, kind == 3 ? Charcoal : Mix(roof, White, .2f));
            }
            // the porch in the corner beside the front room: slab, a flat or gabled roof, a post
            float p0 = Mathf.Min(A(W - roomW - 2.6f), A(W - roomW)), p1 = Mathf.Max(A(W - roomW - 2.6f), A(W - roomW)), pd = Mathf.Max(ro, 1.5f);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, p0, p1, -2.4f, floor, pd, 0f, kind == 1 ? Mix(Timber, Cream, .2f) : Hex("B5503A"));
            K.LBox(m, l, p0 + .3f, p1 - .3f, -2.4f, floor - .15f, pd + .5f, pd, ConcreteDark);
            m.Surface = ComicSurface.PaintedMetal;
            float postA = mirror ? p1 - .12f : p0 + .12f;
            if (kind == 1)
            {
                K.LBox(m, l, postA - .06f, postA + .06f, floor, eave - .1f, pd - .05f, pd - .17f, White);
                m.Surface = roofSurface;
                K.LGable(m, l, p0 - .2f, p1 + .2f, eave - .15f, eave + .75f, pd + .25f, -.2f, roof);
                m.Surface = ComicSurface.Plaster;
                K.TriFacing(m, l.P(p0 + .1f, eave - .1f, pd + .26f), l.P(p1 - .1f, eave - .1f, pd + .26f), l.P((p0 + p1) * .5f, eave + .55f, pd + .26f), wall, new Vector3(-l.S, 0, 0));
            }
            else
            {
                // wrought iron: a post with a scroll
                K.LBox(m, l, postA - .03f, postA + .03f, floor, eave - .12f, pd - .06f, pd - .12f, kind == 3 ? Charcoal : White);
                K.LBox(m, l, postA - .2f, postA + .2f, floor + 1.1f, floor + 1.15f, pd - .06f, pd - .12f, kind == 3 ? Charcoal : White);
                K.LBox(m, l, postA - .2f, postA + .2f, floor + 1.7f, floor + 1.75f, pd - .06f, pd - .12f, kind == 3 ? Charcoal : White);
                K.LBox(m, l, p0 - .1f, p1 + .1f, eave - .14f, eave + .02f, pd + .2f, -.1f, kind == 2 ? Hex("E4E0D2") : trim);
            }
            // the front door (with a flyscreen) and the window beside it
            float da = (p0 + p1) * .5f;
            m.Surface = ComicSurface.PaintedMetal;
            K.LQuad(m, l, da - .55f, da + .55f, floor, floor + 2.2f, .012f, trim);
            m.Surface = ComicSurface.Wood;
            K.LQuad(m, l, da - .43f, da + .43f, floor + .03f, floor + 2.1f, .024f, new[] { HeritageGreen, Hex("7A8F9C"), Hex("8A5A3C"), Hex("E0A93A") }[kind]);
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, da - .28f, da + .28f, floor + 1.2f, floor + 1.9f, .034f, GlassDark, false);
            float rest0 = Mathf.Min(A(0f), A(W - roomW - 2.6f)), rest1 = Mathf.Max(A(0f), A(W - roomW - 2.6f));
            if (rest1 - rest0 > 2.2f)
            {
                float wc = (rest0 + rest1) * .5f, ww = Mathf.Min(2f, rest1 - rest0 - .9f);
                if (kind == 2 || kind == 3) WideWindow(m, l, wc, ww, floor + .95f, floor + 2.25f, 0f, trim, kind == 3 ? Charcoal : White);
                else Sash(m, l, wc, 1.1f, floor + .85f, floor + 2.25f, 0f, White, kind == 1 ? White : Cream, false, 2);
                if (kind == 1) { m.Surface = ComicSurface.Metal; K.Face(m, l.P(wc - .8f, floor + 2.55f, 0f), l.P(wc + .8f, floor + 2.55f, 0f), l.P(wc + .8f, floor + 2.3f, .5f), l.P(wc - .8f, floor + 2.3f, .5f), roof, new Vector3(-l.S, 1, 0)); }
            }
            if (kind == 3)
            {
                // a timber screen beside the door
                m.Surface = ComicSurface.Wood;
                for (float u = floor + .2f; u < eave - .3f; u += .22f) K.LBox(m, l, p0 + (mirror ? 0f : 0f) + .05f, p0 + .9f, u, u + .1f, pd - .02f, pd - .08f, Timber);
            }
            EndWindow(m, l, 0f, -1.8f, -3.2f, floor + .95f, floor + 2.2f, trim);
            // roof gear: a chimney (old houses), an antenna, an evaporative air conditioner or a solar hot water system
            if (kind == 0 || kind == 1)
            {
                float ca = A(W * .3f);
                m.Surface = ComicSurface.Brick;
                K.LBox(m, l, ca - .4f, ca + .4f, eave + .6f, eave + rise + .6f, -D * .4f + .3f, -D * .4f - .3f, kind == 0 ? Mix(wall, Ink, .15f) : Brick);
                m.Surface = ComicSurface.Concrete;
                K.LBox(m, l, ca - .48f, ca + .48f, eave + rise + .6f, eave + rise + .72f, -D * .4f + .38f, -D * .4f - .38f, ConcreteDark);
            }
            m.Surface = ComicSurface.PaintedMetal;
            Vector3 ant = l.P(A(W * .62f), eave + rise * .9f, -D * .5f);
            m.Cylinder(ant + V(0, .9f, 0), .03f, 1.8f, Mast, 4);
            for (int i = 0; i < 4; i++) m.Box(ant + V(0, 1.2f + i * .16f, 0), V(.03f, .03f, .9f - i * .15f), Mast);
            if (kind >= 2 || v % 8 >= 4)
            {
                m.Surface = ComicSurface.Metal;
                Vector3 ac = l.P(A(W * .3f), eave + rise * .62f, -D * .62f);
                m.Box(ac + V(0, .45f, 0), V(1.1f, .9f, 1.1f), kind == 3 ? Hex("C9CED3") : Hex("D9D4C7"));
                m.Box(ac + V(0, .92f, 0), V(1.2f, .06f, 1.2f), Mast);
            }
        }

        /// <summary>A wide steel or aluminium window: frame, three panes with two mullions, a sill.</summary>
        static void WideWindow(ComicMesh m, CqLot l, float a, float w, float u0, float u1, float o, Color frame, Color sill)
        {
            float h = w * .5f;
            m.Surface = ComicSurface.PaintedMetal;
            K.LQuad(m, l, a - h, a + h, u0, u1, o + .012f, frame);
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, a - h + .08f, a + h - .08f, u0 + .08f, u1 - .08f, o + .024f, Glass);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float t in new[] { .28f, .72f })
                K.LQuad(m, l, Mathf.Lerp(a - h, a + h, t) - .03f, Mathf.Lerp(a - h, a + h, t) + .03f, u0 + .08f, u1 - .08f, o + .034f, frame, false);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, a - h - .08f, a + h + .08f, u0 - .11f, u0, o + .14f, o, sill);
        }

        /// <summary>A flat-roofed carport at the side of the house: two steel posts, a fascia, a dark soffit.</summary>
        static void Carport(ComicMesh m, CqLot l)
        {
            float W = l.W, D = l.Depth;
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.1f, W + .1f, 2.5f, 2.74f, .4f, -D, White);
            K.LBox(m, l, -.06f, W + .06f, 2.44f, 2.5f, .36f, -D + .04f, Mix(White, Ink, .35f));
            foreach (float a in new[] { .12f, W - .12f }) K.LBox(m, l, a - .05f, a + .05f, -.2f, 2.44f, .2f, .1f, White);
            m.Surface = ComicSurface.Plain;
            K.LQuad(m, l, .2f, W - .2f, 0f, 2.3f, -D + .02f, Hex("8E979E"));                                 // the roller door at the back
            for (float u = .3f; u < 2.2f; u += .3f) K.LQuad(m, l, .2f, W - .2f, u, u + .03f, -D + .032f, Hex("6F7A82"), false);
        }

        /// <summary>The back of a house seen over the fence: a plain wall with a door and windows, a hip roof, a flat patio
        /// roof on posts, an air conditioner on the roof.</summary>
        static void HouseBack(ComicMesh m, CqLot l, int v)
        {
            float W = l.W, D = l.Depth, eave = 3f;
            Color wall = Pick(new[] { BrickSalmon, Hex("E4E0D2"), BrickCream, White, BrickOrange }, v), roof = Pick(new[] { Tile, Hex("8C8F92"), TileBrown, Hex("4A5057"), RoofGreen }, v);
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, 0, W, -2.4f, eave, 0f, -D, wall);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.45f, W + .45f, eave - .1f, eave + .04f, .45f, -D - .45f, White);
            m.Surface = v % 2 == 0 ? ComicSurface.Brick : ComicSurface.Metal;
            HipRoof(m, l, -.5f, W + .5f, eave + .04f, eave + 2f, .5f, -D - .5f, roof);
            WideWindow(m, l, W * .3f, 1.8f, 1.2f, 2.4f, 0f, White, White);
            m.Surface = ComicSurface.PaintedMetal;
            K.LQuad(m, l, W * .68f - .5f, W * .68f + .5f, .3f, 2.4f, .012f, White);
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, W * .68f - .4f, W * .68f + .4f, .4f, 2.3f, .024f, GlassDark);
            // patio
            m.Surface = ComicSurface.Metal;
            K.LBox(m, l, W * .4f, W, 2.5f, 2.62f, 2.8f, 0f, Hex("C9CED3"));
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float a in new[] { W * .4f + .1f, W - .1f }) K.LBox(m, l, a - .05f, a + .05f, 0f, 2.5f, 2.7f, 2.6f, White);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, W * .4f, W, -1f, .3f, 2.8f, 0f, Concrete);
            m.Surface = ComicSurface.Metal;
            Vector3 ac = l.P(W * .35f, eave + 1.2f, -D * .5f);
            m.Box(ac + V(0, .4f, 0), V(1.1f, .8f, 1.1f), Hex("D9D4C7"));
        }

        /// <summary>A super-six fibro fence (grey corrugated asbestos-cement sheets, the classic Perth side fence) with a
        /// capping. Along z from z0 to z1 at x when length is 0; or running away from the road (in x) for length metres
        /// at z0.</summary>
        static void FibroFence(ComicMesh sloped, float x, float z0, float z1, float y, float h, int facing, float length = 0f)
        {
            Color c = Hex("B7B4AC"), dark = Hex("9A978F");
            sloped.Surface = ComicSurface.Concrete;
            if (length > 0f)
            {
                float sx = Mathf.Sign(x);
                sloped.Box(V(x + sx * length * .5f, y + h * .5f, z0), V(length, h, .07f), c);
                sloped.Box(V(x + sx * length * .5f, y + h + .03f, z0), V(length + .04f, .07f, .12f), dark);
                sloped.Surface = ComicSurface.Plain;
                for (float u = .3f; u < length; u += .34f)
                    K.Face(sloped, V(x + sx * u, y + .05f, z0 - .04f), V(x + sx * (u + .05f), y + .05f, z0 - .04f), V(x + sx * (u + .05f), y + h - .02f, z0 - .04f), V(x + sx * u, y + h - .02f, z0 - .04f), dark, Vector3.back, false);
                return;
            }
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                sloped.Surface = ComicSurface.Concrete;
                sloped.Box(V(x, y + h * .5f, (z + e) * .5f), V(.07f, h, e - z), c);
                sloped.Box(V(x, y + h + .03f, (z + e) * .5f), V(.12f, .07f, e - z + .02f), dark);
            }
            if (facing == 0) return;
            sloped.Surface = ComicSurface.Plain;
            Vector3 n = new Vector3(facing, 0, 0);
            float xf = x + facing * .04f;
            for (float z = z0 + .17f; z < z1 - .05f; z += .34f)
                K.Face(sloped, V(xf, y + .05f, z - .025f), V(xf, y + .05f, z + .025f), V(xf, y + h - .02f, z + .025f), V(xf, y + h - .02f, z - .025f), dark, n, false);
        }

        /// <summary>A pipe-and-chain-mesh front fence (grey pipe frame, pale mesh), with a gate opening.</summary>
        static void PipeMeshFence(ComicMesh sloped, float x, float z0, float z1, float y, float h, int facing, float gate)
        {
            Vector3 n = new Vector3(facing, 0, 0);
            sloped.Surface = ComicSurface.PaintedMetal;
            var cuts = new List<float> { z0, gate - .55f, gate + .55f, z1 };
            for (int i = 0; i + 1 < cuts.Count; i += 2)
            {
                float a = cuts[i], b = cuts[i + 1];
                if (b - a < .4f) continue;
                for (float z = a; z <= b + .01f; z += Mathf.Max(1.2f, (b - a) / Mathf.Max(1f, Mathf.Round((b - a) / 2.4f))))
                    sloped.Box(V(x, y + h * .5f, z), V(.06f, h, .06f), Mast);
                sloped.Box(V(x, y + h, (a + b) * .5f), V(.06f, .06f, b - a), Mast);
                sloped.Box(V(x, y + .08f, (a + b) * .5f), V(.05f, .05f, b - a), Mast);
                foreach (float t in new[] { .3f, .52f, .74f })
                    K.Face(sloped, V(x + facing * .02f, y + h * t, a), V(x + facing * .02f, y + h * t, b), V(x + facing * .02f, y + h * t + .025f, b), V(x + facing * .02f, y + h * t + .025f, a), Hex("C3CAD0"), n, false);
            }
        }

        /// <summary>A zinc garden shed with a skillion roof and a sliding door.</summary>
        static void GardenShed(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            Color zinc = Hex("B9C2C8"), dark = Hex("8E989E");
            m.Surface = ComicSurface.Metal;
            m.Box(p + q * V(0, 1f, 0), V(2.2f, 2f, 1.8f), zinc, q);
            m.Box(p + q * V(0, 2.08f, 0), V(2.4f, .1f, 2f), dark, q * Quaternion.Euler(0, 0, 6f));
            m.Surface = ComicSurface.Plain;
            m.Box(p + q * V(-1.11f, .95f, .2f), V(.02f, 1.8f, .9f), dark, q);
            for (float z = -.8f; z < .9f; z += .4f) m.Box(p + q * V(-1.105f, 1f, z), V(.012f, 1.96f, .03f), dark, q);
        }

        /// <summary>A round back-yard trampoline with a safety net on poles.</summary>
        static void Trampoline(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Fabric;
            m.Cylinder(p + V(0, .7f, 0), 1.5f, .1f, Hex("2F6FB0"), 10);
            m.Cylinder(p + V(0, .76f, 0), 1.2f, .04f, Hex("2A2E34"), 10);
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * .4f;
                Vector3 c = p + V(Mathf.Cos(a) * 1.45f, 0, Mathf.Sin(a) * 1.45f);
                m.Box(c + V(0, .35f, 0), V(.06f, .7f, .06f), Mast);
                m.Box(c + V(0, 1.6f, 0), V(.07f, 1.8f, .07f), Hex("2F6FB0"));
            }
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * .4f, b = (i + 1) * Mathf.PI * .4f;
                m.Beam(p + V(Mathf.Cos(a) * 1.45f, 2.45f, Mathf.Sin(a) * 1.45f), p + V(Mathf.Cos(b) * 1.45f, 2.45f, Mathf.Sin(b) * 1.45f), .04f, Ink);
            }
        }

        /// <summary>A lemon tree: a small round crown on a short trunk, dotted with yellow fruit.</summary>
        static void LemonTree(ComicMesh m, Vector3 p, float s, int seed)
        {
            m.Surface = ComicSurface.Wood;
            Limb(m, p, p + V(0, 1.1f * s, 0), .1f * s, .07f * s, Hex("6B5A4A"), 5);
            m.Surface = ComicSurface.Foliage;
            Blob(m, p + V(0, 1.9f * s, 0), V(2.2f * s, 1.9f * s, 2.2f * s), Hex("3F7A3C"), 7, 4, seed, .16f);
            Blob(m, p + V(.3f * s, 2.4f * s, .2f * s), V(1.2f * s, .9f * s, 1.2f * s), Hex("5B9A4A"), 6, 3, seed + 1f, .14f);
            m.Surface = ComicSurface.Plastic;
            for (int i = 0; i < 7; i++)
            {
                float a = seed * 1.3f + i * .9f;
                m.Box(p + V(Mathf.Cos(a) * .95f * s, (1.5f + .14f * (i % 4)) * s, Mathf.Sin(a) * .95f * s), Vector3.one * .16f * s, Hex("F6D23C"), Quaternion.Euler(20, a * 57f, 20));
            }
        }

        // ================================================================================== the deli and the park

        /// <summary>
        /// The corner deli: a painted-brick shop under a bullnose verandah on timber posts at the kerb, a stepped parapet
        /// with DELI painted on it (and on the verandah fascia towards the rider), a green-tiled shopfront with posters,
        /// coloured plastic strips in the doorway, a painted ice-cream cone on the side wall, the owners' house roof
        /// behind. d (optional) receives the verandah posts' colliders.
        /// </summary>
        static void CornerDeli(ComicMesh m, CqLot l, CqChunkDraft d)
        {
            float W = l.W, D = l.Depth, gh = 3.3f, top = 4.6f, vd = 3.5f;
            Color wall = Hex("F2E6B8"), trim = White, red = Hex("C9383A");
            Vector3 toRoad = new Vector3(-l.S, 0, 0);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, 0f, .02f, -D, Ink);
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, 0, W, 0f, top, 0f, -D * .55f, wall);
            K.LBox(m, l, 0, W, 0f, .9f, .03f, -D * .55f - .03f, HeritageGreen);
            // shop windows with posters, the door with its strip curtain
            float dc = W * .5f;
            foreach ((float a0, float a1) in new[] { (.8f, dc - 1f), (dc + 1f, W - .8f) })
            {
                m.Surface = ComicSurface.PaintedMetal;
                K.LQuad(m, l, a0, a1, .9f, 2.8f, .034f, trim);
                m.Surface = ComicSurface.Glass;
                K.LQuad(m, l, a0 + .1f, a1 - .1f, 1f, 2.7f, .046f, Glass);
                m.Surface = ComicSurface.Plain;
                Color[] posters = { Hex("F6C543"), Hex("E2463A"), Hex("2F6FB0"), White, Hex("F28C28") };
                int n = Mathf.Max(2, Mathf.RoundToInt((a1 - a0) / .9f));
                for (int i = 0; i < n; i++)
                {
                    float a = Mathf.Lerp(a0 + .2f, a1 - .2f, (i + .5f) / n), u = 1.2f + (i % 2) * .6f;
                    K.LQuad(m, l, a - .28f, a + .28f, u, u + .7f, .058f, Pick(posters, i + (int)a0), false);
                }
            }
            m.Surface = ComicSurface.PaintedMetal;
            K.LQuad(m, l, dc - .7f, dc + .7f, 0f, 2.8f, .034f, trim);
            m.Surface = ComicSurface.Plain;
            K.LQuad(m, l, dc - .58f, dc + .58f, .03f, 2.6f, .046f, Hex("3A2E2A"));
            Color[] strips = { Hex("E2463A"), Hex("F6C543"), Hex("2A9D9A"), Hex("2F6FB0"), Hex("F28C28"), Hex("7FC66A"), Hex("E85A8C") };
            for (int i = 0; i < 9; i++) K.LQuad(m, l, dc - .56f + i * .125f, dc - .56f + i * .125f + .1f, .25f, 2.58f, .058f, Pick(strips, i), false);
            // the parapet: stepped, with DELI painted in red
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, -.1f, W + .1f, top, top + .18f, .15f, -D * .55f - .05f, trim);
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, W * .22f, W * .78f, top + .18f, top + 1.2f, .08f, -.4f, wall);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, W * .22f - .08f, W * .78f + .08f, top + 1.2f, top + 1.34f, .14f, -.46f, trim);
            LText(m, l, "DELI", W * .5f, top + .7f, .085f, .15f, red);
            // the bullnose verandah over the footpath
            m.Surface = ComicSurface.PaintedMetal;
            float[] posts = { .2f, W * .34f, W * .66f, W - .2f };
            foreach (float a in posts)
            {
                K.LBox(m, l, a - .07f, a + .07f, 0f, gh - .3f, vd - .02f, vd - .16f, trim);
                K.LBox(m, l, a - .11f, a + .11f, 0f, .9f, vd + .02f, vd - .2f, HeritageGreen);
                if (d != null) K.Thin(d.Collisions, "VerandahPost", new Vector3(l.P(a, 0f, vd - .09f).x, d.Ground(l.Z0 + a), l.Z0 + a), 3f, .3f);
            }
            K.LBox(m, l, -.1f, W + .1f, gh - .3f, gh, vd + .06f, vd - .2f, trim);
            m.Surface = ComicSurface.Metal;
            int panels = 8;
            for (int i = 0; i < panels; i++)
            {
                float a0 = -.15f + (W + .3f) * i / panels, a1 = -.15f + (W + .3f) * (i + 1) / panels;
                Color c = i % 2 == 0 ? red : Hex("EDE6D6");
                K.Face(m, l.P(a0, gh + .75f, 0f), l.P(a1, gh + .75f, 0f), l.P(a1, gh + .45f, vd * .6f), l.P(a0, gh + .45f, vd * .6f), c, toRoad + Vector3.up * 3f);
                K.Face(m, l.P(a0, gh + .45f, vd * .6f), l.P(a1, gh + .45f, vd * .6f), l.P(a1, gh - .02f, vd + .25f), l.P(a0, gh - .02f, vd + .25f), c, toRoad + Vector3.up);
            }
            m.Surface = ComicSurface.Plain;
            K.Face(m, l.P(-.16f, gh - .3f, 0f), l.P(-.16f, gh - .3f, vd), l.P(-.16f, gh + .4f, vd * .6f), l.P(-.16f, gh + .7f, 0f), Hex("EDE6D6"), Vector3.back);
            // DELI on the fascia facing the road, and a hanging board facing the rider
            LText(m, l, "DELI", W * .5f, gh - .15f, vd + .065f, .05f, red);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.2f, -.14f, gh - 1.1f, gh - .35f, vd - .3f, vd - 2.3f, trim);
            Text(m, "DELI", l.P(-.215f, gh - .72f, vd - 1.3f), Vector3.right, Vector3.up, .11f, red);
            // the side wall towards the rider: a painted panel with an ice-cream cone
            m.Surface = ComicSurface.Plain;
            Vector3 c0 = l.P(-.012f, 2.3f, -3f);
            K.Face(m, c0 + V(-1.5f, -1.2f, 0), c0 + V(1.5f, -1.2f, 0), c0 + V(1.5f, 1.4f, 0), c0 + V(-1.5f, 1.4f, 0), Hex("2F6FB0"), Vector3.back);
            K.TriFacing(m, c0 + V(-.35f, .1f, -.012f), c0 + V(.35f, .1f, -.012f), c0 + V(0, -.95f, -.012f), Hex("E0A93A"), Vector3.back);
            K.Face(m, c0 + V(-.42f, .1f, -.012f), c0 + V(.42f, .1f, -.012f), c0 + V(.36f, .62f, -.012f), c0 + V(-.36f, .62f, -.012f), Hex("F4C6D8"), Vector3.back, false);
            K.Face(m, c0 + V(-.3f, .62f, -.012f), c0 + V(.3f, .62f, -.012f), c0 + V(.2f, 1.05f, -.012f), c0 + V(-.2f, 1.05f, -.012f), White, Vector3.back, false);
            // the house behind
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, 0, W, 0f, 3.2f, -D * .55f, -D, BrickSalmon);
            HipRoof(m, l, -.4f, W + .4f, 3.2f, 5.4f, -D * .5f, -D - .4f, Tile);
            EndWindow(m, l, 0f, -D * .7f, -D * .7f - 1.4f, 1.1f, 2.4f, White);
        }

        /// <summary>The ice freezer outside the deli: a white chest with a sloped blue lid and ICE on the front.</summary>
        static void IceFreezer(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + q * V(0, .45f, 0), V(1.4f, .9f, .8f), White, q);
            m.Box(p + q * V(0, .96f, 0), V(1.44f, .12f, .84f), Hex("2F6FB0"), q * Quaternion.Euler(8f, 0, 0));
            m.Box(p + q * V(0, .04f, 0), V(1.44f, .08f, .84f), Ink, q);
            Text(m, "ICE", p + q * V(0, .5f, -.412f), q * Vector3.right, Vector3.up, .1f, Hex("2F6FB0"));
        }

        /// <summary>A cage of gas bottles: grey mesh, red and silver cylinders.</summary>
        static void GasCage(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.6f, .6f }) foreach (float z in new[] { -.3f, .3f }) m.Box(p + q * V(x, .7f, z), V(.05f, 1.4f, .05f), MastDark, q);
            m.Box(p + q * V(0, 1.4f, 0), V(1.3f, .05f, .7f), MastDark, q);
            m.Box(p + q * V(0, .05f, 0), V(1.3f, .1f, .7f), MastDark, q);
            foreach (float y in new[] { .5f, .95f }) m.Box(p + q * V(0, y, -.3f), V(1.2f, .03f, .03f), MastDark, q);
            m.Surface = ComicSurface.Metal;
            for (int i = 0; i < 3; i++) m.Cylinder(p + q * V(-.38f + i * .38f, .55f, 0), .16f, .9f, i == 1 ? Hex("C9CED3") : Hex("D8323A"), 6);
        }

        /// <summary>A newspaper rack: a wire stand with a stack of papers and a headline board.</summary>
        static void NewsRack(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.3f, .3f }) m.Box(p + q * V(x, .4f, 0), V(.04f, .8f, .4f), Ink, q);
            m.Surface = ComicSurface.Fabric;
            m.Box(p + q * V(0, .5f, 0), V(.56f, .3f, .36f), Hex("EDEAE0"), q);
            m.Box(p + q * V(0, .66f, 0), V(.56f, .02f, .36f), Hex("8E979E"), q);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + q * V(0, 1.05f, .16f), V(.6f, .6f, .04f), White, q);
            m.Box(p + q * V(0, 1.2f, .135f), V(.48f, .12f, .02f), Ink, q);
            m.Box(p + q * V(0, 1f, .135f), V(.48f, .05f, .02f), Hex("8E979E"), q);
            m.Box(p + q * V(0, .9f, .135f), V(.4f, .05f, .02f), Hex("8E979E"), q);
        }

        /// <summary>A small play set: a fort on four posts with a peaked roof, a slide, a ladder, a two-seat swing.</summary>
        static void PlaySet(ComicMesh f, Vector3 pg)
        {
            Color blue = Hex("2E5E9E"), yellow = Hex("F2C230"), red = Hex("D8323A");
            f.Surface = ComicSurface.PaintedMetal;
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2) f.Box(pg + V(x * .75f, 1.5f, z * .75f), V(.14f, 3f, .14f), blue);
            f.Box(pg + V(0, 1.4f, 0), V(1.7f, .12f, 1.7f), yellow);
            for (int x = -1; x <= 1; x += 2) f.Box(pg + V(x * .75f, 1.95f, 0), V(.08f, .5f, 1.4f), red);
            f.Cylinder(pg + V(0, 3.4f, 0), 1.3f, .9f, red, 4, .08f, Quaternion.Euler(0, 45, 0));
            f.Surface = ComicSurface.Plastic;
            Vector3 a = pg + V(-.9f, 1.45f, 0), b = pg + V(-3.4f, .25f, 0);
            f.Box((a + b) * .5f, V(.85f, .1f, (b - a).magnitude), yellow, Quaternion.LookRotation(b - a));
            for (int z = -1; z <= 1; z += 2) f.Beam(a + V(0, .14f, z * .43f), b + V(0, .14f, z * .43f), .09f, blue);
            f.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 4; i++) f.Box(pg + V(.95f, .3f + i * .32f, 0), V(.08f, .06f, .7f), blue);
            foreach (float z in new[] { -.36f, .36f }) f.Beam(pg + V(1.2f, 0, z), pg + V(.85f, 1.4f, z), .07f, blue);
            // swings
            Vector3 sw = pg + V(0, 0, 3.4f);
            foreach (float x in new[] { -1.6f, 1.6f })
            {
                f.Beam(sw + V(x, 0, -.8f), sw + V(x, 2.3f, 0), .1f, yellow);
                f.Beam(sw + V(x, 0, .8f), sw + V(x, 2.3f, 0), .1f, yellow);
            }
            f.Box(sw + V(0, 2.3f, 0), V(3.3f, .1f, .1f), yellow);
            foreach (float x in new[] { -.7f, .7f })
            {
                foreach (float dx in new[] { -.2f, .2f }) f.Box(sw + V(x + dx, 1.45f, 0), V(.03f, 1.7f, .03f), Ink);
                f.Surface = ComicSurface.Rubber;
                f.Box(sw + V(x, .58f, 0), V(.5f, .06f, .22f), red);
                f.Surface = ComicSurface.PaintedMetal;
            }
        }

        /// <summary>A basketball hoop on a goose-neck post: white backboard with a red square, an orange ring.</summary>
        static void BasketballHoop(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 1.6f, 0), .08f, 3.2f, Ink, 6);
            m.Beam(p + V(0, 3.2f, 0), p + V(-.9f, 3.5f, 0), .1f, Ink);
            m.Box(p + V(-.95f, 3.5f, 0), V(.06f, 1.1f, 1.7f), White);
            m.Surface = ComicSurface.Plain;
            K.Face(m, p + V(-.985f, 3.2f, -.3f), p + V(-.985f, 3.2f, .3f), p + V(-.985f, 3.65f, .3f), p + V(-.985f, 3.65f, -.3f), Hex("D8323A"), Vector3.left, false);
            K.Face(m, p + V(-.99f, 3.26f, -.24f), p + V(-.99f, 3.26f, .24f), p + V(-.99f, 3.59f, .24f), p + V(-.99f, 3.59f, -.24f), White, Vector3.left, false);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(-1.25f, 3.1f, 0), .26f, .04f, Hex("F28C28"), 8);
        }

        // ================================================================================== Meltham obstacles and animals

        /// <summary>A kid's tricycle left on the path: red frame, white handlebars, three black wheels.</summary>
        static ComicMesh TrikeMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Rubber };
            m.Cylinder(V(0, .18f, -.3f), .18f, .08f, Ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            foreach (float x in new[] { -.22f, .22f }) m.Cylinder(V(x, .12f, .3f), .12f, .07f, Ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            m.Surface = ComicSurface.PaintedMetal;
            Color red = Hex("D8323A");
            m.Beam(V(0, .2f, -.3f), V(0, .42f, .1f), .07f, red);
            m.Box(V(0, .16f, .3f), V(.5f, .06f, .06f), red);
            m.Beam(V(0, .42f, .1f), V(0, .16f, .3f), .06f, red);
            m.Beam(V(0, .2f, -.3f), V(0, .6f, -.22f), .05f, White);
            m.Box(V(0, .62f, -.22f), V(.46f, .05f, .05f), White);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .46f, .14f), V(.2f, .05f, .24f), Hex("F2C230"));
            foreach (float x in new[] { -.24f, .24f }) m.Box(V(x, .62f, -.22f), V(.08f, .07f, .07f), Ink);
            return m;
        }

        /// <summary>An old couch put out for the verge collection: brown cushions, a sagging arm, a cardboard box on it.</summary>
        static ComicMesh VergeCouchMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color c = Hex("8A6A4E"), dark = Hex("6E5440");
            m.Box(V(0, .28f, 0), V(1.8f, .36f, .8f), dark);
            m.Box(V(0, .52f, -.05f), V(1.5f, .16f, .66f), c);
            m.Box(V(0, .7f, .32f), V(1.8f, .6f, .2f), c);
            m.Box(V(-.82f, .52f, 0f), V(.2f, .44f, .8f), c);
            m.Box(V(.82f, .5f, 0f), V(.2f, .4f, .8f), c, Quaternion.Euler(0, 0, -6f));
            m.Box(V(-.02f, .61f, -.05f), V(.03f, .02f, .64f), dark);
            m.Surface = ComicSurface.Wood;
            m.Box(V(.35f, .78f, -.05f), V(.5f, .36f, .4f), Hex("C9A56A"), Quaternion.Euler(0, 14f, 0));
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.8f, .8f }) foreach (float z in new[] { -.32f, .32f }) m.Box(V(x, .05f, z), V(.08f, .1f, .08f), Ink);
            return m;
        }

        /// <summary>A shopping trolley left on the footpath (no markings): a wire basket on a frame, a red handle.</summary>
        static ComicMesh TrolleyMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Metal };
            Color wire = Hex("B9C0C5");
            foreach (float x in new[] { -.26f, .26f })
            {
                m.Box(V(x, .65f, 0f), V(.03f, .5f, .86f), wire);
                m.Box(V(x, .22f, .05f), V(.04f, .04f, .9f), wire);
                m.Beam(V(x, .22f, .45f), V(x, .95f, .45f), .04f, wire);
            }
            m.Box(V(0, .65f, -.42f), V(.52f, .5f, .03f), wire);
            m.Box(V(0, .65f, .42f), V(.52f, .5f, .03f), wire);
            m.Box(V(0, .4f, 0f), V(.52f, .03f, .86f), wire);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .98f, .47f), V(.58f, .06f, .06f), Hex("D8323A"));
            m.Surface = ComicSurface.Rubber;
            foreach (float x in new[] { -.26f, .26f }) foreach (float z in new[] { -.4f, .4f }) m.Box(V(x, .07f, z), V(.06f, .14f, .14f), Ink);
            return m;
        }

        /// <summary>A galah (pink and grey cockatoo) feeding on the lawn: rose-pink head and chest, pale crest, grey wings.</summary>
        static ComicMesh GalahMesh()
        {
            var a = new ComicMesh { Surface = ComicSurface.Plastic };
            Color pink = Hex("E8789A"), pale = Hex("F6CAD6"), grey = Hex("9AA3AA"), dark = Hex("6F7A82");
            a.Box(V(0, .2f, .02f), V(.17f, .2f, .3f), pink, Quaternion.Euler(-18f, 0, 0));
            a.Box(V(0, .34f, -.12f), V(.15f, .15f, .15f), pink);
            a.Box(V(0, .43f, -.1f), V(.13f, .06f, .14f), pale);
            a.Box(V(0, .33f, -.21f), V(.06f, .06f, .06f), Hex("E6DFCF"));
            a.Box(V(0, .16f, .22f), V(.1f, .04f, .2f), dark, Quaternion.Euler(14f, 0, 0));
            foreach (int s in new[] { -1, 1 })
            {
                a.Box(V(s * .095f, .22f, .05f), V(.03f, .16f, .26f), grey, Quaternion.Euler(-18f, 0, 0));
                a.Box(V(s * .08f, .37f, -.17f), V(.02f, .03f, .03f), Ink);
                a.Box(V(s * .04f, .05f, 0f), V(.03f, .1f, .03f), dark);
                a.Box(V(s * .04f, .012f, -.03f), V(.06f, .024f, .1f), dark);
            }
            return a;
        }

        // ================================================================================== far scenery

        /// <summary>
        /// Meltham's horizon: suburbia on both sides: tiled and iron roofs between gums and jacarandas, a church hall,
        /// the light towers of a sports oval; on the right, far off, a glint of the Swan River and its line of river gums;
        /// and the Darling Scarp, bigger and closer now, with the TV masts on its ridge. No colliders.
        /// </summary>
        static ComicMesh MelFar(float grade)
        {
            var m = new ComicMesh();
            var r = new System.Random(260);
            Color haze = Hex("D6DEDF");
            const float z0 = -220f, z1 = 820f;
            FarStrip(m, 60f, 340f, z0, z1, -.3f, Haze(Hex("93A86C"), 200f, haze));
            FarStrip(m, 340f, 460f, z0, z1, -3f, Haze(River, 400f, haze));
            FarStrip(m, 460f, 1500f, z0, z1, -2.5f, Haze(Hex("8FA376"), 700f, haze));
            FarStrip(m, -60f, -1500f, z0, z1, -.3f, Haze(Hex("93A86C"), 300f, haze));
            FarStrip(m, K.Edge, 60f, 330f, z1, K.Kerb - .05f, LawnDry);
            FarStrip(m, -K.Edge, -60f, 330f, z1, K.Kerb - .05f, LawnDry);
            FarSuburb(m, r, 1, 64f, 320f, z0, z1, -.3f, haze, .42f);
            FarSuburb(m, r, -1, 64f, 320f, z0, z1, -.3f, haze, .42f);
            FarTreeLine(m, r, 1, 326f, 16f, z0, z1, -1f, 11f, 17f, GumLeaf, GumLeafDark, haze, 330f);
            FarTreeLine(m, r, 1, 462f, 40f, z0, z1, -2.5f, 12f, 18f, GumLeafDark, FigMid, haze, 520f);
            FarTreeLine(m, r, -1, 330f, 60f, z0, z1, -.3f, 12f, 18f, GumLeaf, GumLeafDark, haze, 380f);
            // oval light towers and a hall on the left
            foreach (float z in new[] { 120f, 190f })
                foreach (float x in new[] { -150f, -215f })
                {
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Cylinder(V(x, 14f, z), .45f, 28f, Haze(Mast, 200f, haze), 5, .5f);
                    m.Box(V(x, 28.6f, z), V(4.5f, 2.2f, .6f), Haze(Hex("E4E2D8"), 200f, haze), Quaternion.Euler(0, x < -180f ? 40f : -40f, 0));
                }
            m.Surface = ComicSurface.Brick;
            m.Box(V(-120f, 4f, 330f), V(14f, 8f, 26f), Haze(BrickCream, 300f, haze));
            m.Surface = ComicSurface.Metal;
            K.MinMaxBox(m, V(-128f, 8f, 316f), V(-112f, 8.3f, 344f), Haze(Galv, 300f, haze));
            Scarp(m, 1, 820f, z0, z1, -2.5f, 84f, haze, 4.2f);
            Scarp(m, -1, 880f, z0, z1, -.3f, 74f, haze, 5.3f);
            // the TV masts on the scarp
            foreach ((float x, float z, float h) in new[] { (930f, 150f, 60f), (950f, 240f, 75f), (940f, 420f, 55f), (-1010f, 300f, 60f) })
            {
                m.Surface = ComicSurface.PaintedMetal;
                float baseY = 84f;
                for (int i = 0; i < 5; i++)
                    m.Cylinder(V(x, baseY + h * (i + .5f) / 5f, z), 1.6f - i * .2f, h / 5f, Haze(i % 2 == 0 ? Hex("D8323A") : White, 500f, haze), 4, .9f);
            }
            m.Shear(grade);
            return m;
        }
    }
}
