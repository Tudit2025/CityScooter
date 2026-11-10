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
    // GUILDFORD (route order 310): James St beside the railway through the 1829 heritage town. Left: the Guildford Hotel
    // opposite the station, the antique shops, the Rose and Crown, Stirling Square with St Matthew's, the Post Office
    // and the old courthouse. Right: the heritage station and the Midland line. Jacarandas in bloom.
    public static partial class East4Builder
    {
        /// <summary>The names on the two historic hotels (as on the real parapets: they are heritage place names). Set to
        /// false for plain tablets without letters.</summary>
        public const bool HotelNames = true;
        // The antique dealer's trailer in the left lane of James St (JamesSt chunk): planks up, the flat deck, off the end.
        public const float TrailerX = -2.6f, TrailerZ = 12f, TrailerWidth = 2.4f, TrailerRise = 4f, TrailerHeight = .9f, TrailerTop = 4.5f;

        static void BuildGuildford(TrackTheme theme, bool atlas)
        {
            const string id = GuildfordId;
            K.Begin(MeshFolder(id), PrefabFolder(id), CbdBuilder.DataFolder, PatternFolder(id));
            Obstacle barrel = K.SaveObstacle(Pf(id, "OBS_Guildford_WineBarrel"), "OBS_Guildford_WineBarrel", WineBarrelMesh(), new Vector3(.85f, 1f, .85f), ObstacleKind.Thing, true, true, 1f);
            Obstacle aFrame = K.SaveObstacle(Pf(id, "OBS_Guildford_AFrame"), "OBS_Guildford_AFrame", AntiqueAFrameMesh(), new Vector3(.9f, 1.05f, .7f), ObstacleKind.Thing, true, true, .9f);
            Obstacle chest = K.SaveObstacle(Pf(id, "OBS_Guildford_TeaChest"), "OBS_Guildford_TeaChest", TeaChestMesh(), new Vector3(.8f, .78f, .7f), ObstacleKind.Thing, true, true, 1f);
            Obstacle hatStand = K.SaveObstacle(Pf(id, "OBS_Guildford_HatStand"), "OBS_Guildford_HatStand", HatStandMesh(), new Vector3(.6f, 1.9f, .6f), ObstacleKind.Thing, false, true, .6f);
            GameObject magpie = SaveAnimal(Pf(id, "ANM_Guildford_Magpie"), "ANM_Guildford_Magpie", MagpieMesh(), new Vector3(0, .22f, 0), new Vector3(.4f, .45f, .7f), .5f, true);
            GameObject galah = SaveAnimal(Pf(id, "ANM_Guildford_Galah"), "ANM_Guildford_Galah", GalahMesh(), new Vector3(0, .2f, 0), new Vector3(.4f, .42f, .6f), 0f, true);
            Ramp dray = K.SaveRamp(Pf(id, "RMP_Guildford_DrayRamp"), "RMP_Guildford_DrayRamp", 3f, 5f, 1.3f, 1.5f, 1f, .5f, E4.DeckLight, E4.Deck, false);

            K.WriteChunk(GfEntry(), ChunkPath(id, "Entry"));
            K.WriteChunk(GfStation(), ChunkPath(id, "Station"));
            K.WriteChunk(GfJamesSt(), ChunkPath(id, "JamesSt"));
            K.WriteChunk(GfRoseAndCrown(), ChunkPath(id, "RoseAndCrown"));
            K.WriteChunk(GfStirlingSquare(), ChunkPath(id, "StirlingSquare"));
            K.WriteChunk(GfPostOffice(), ChunkPath(id, "PostOffice"));
            K.WriteChunk(GfExit(), ChunkPath(id, "Exit"));
            GameObject far = K.SaveFarScenery(Pf(id, "FAR_Guildford_SwanValley"), "FAR_Guildford_SwanValley", GfFar(-GuildfordDrop / 40f));

            string patterns = PatternFolder(id);
            var own = new List<HazardPattern>
            {
                K.Pattern(patterns, "HZ_Guildford_Barrels", 40, 0, .8f, true, 30, (barrel.gameObject, -2.6f, 18f, 0f), (barrel.gameObject, 0f, 22f, 0f), (chest.gameObject, 2.6f, 27f, 10f)),
                K.Pattern(patterns, "HZ_Guildford_Magpies", 40, 0, .7f, true, 30, (magpie, -1.2f, 20f, 0f), (galah, 1.6f, 25f, 0f), (galah, 2.3f, 26.2f, 40f)),
            };
            District d = Fill(id, "GUILDFORD", 310, GuildfordChunks);
            d.obstacles = new[] { barrel, aFrame, chest, hatStand };
            d.jumpObstacles = new[] { barrel, chest, aFrame };
            d.hazards = own.Concat(ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_OncomingPair", "HZ_Crossing", "HZ_SidewalkStroll", "HZ_PoliceStop")).ToArray();
            var people = new List<GameObject>(K.People(theme, "Tourist", "Tourist", "Shopper", "Shopper", "Elder", "Elder", "Barista", "Kid", "DogWalker", "Student", "Office"));
            people.Add(magpie); people.Add(magpie); people.Add(galah);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { dray };
            d.rampFrequency = .8f;
            // Warm afternoon light on old brick and render.
            d.fogColor = new Color(.86f, .87f, .82f);
            d.skyTint = new Color(1f, .99f, .96f);
            d.ambientSky = new Color(.68f, .80f, .93f);
            d.ambientEquator = new Color(.89f, .86f, .78f);
            d.ambientGround = new Color(.66f, .56f, .46f);
            d.sunColor = new Color(1f, .95f, .84f);
            d.sunIntensity = 1.42f;
            d.farScenery = far;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/Guildford] " + string.Join("; ", K.Report));
            if (!atlas) return;
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("GuildfordHotel", null, Make(m => GuildfordHotel(m, new CqLot(-1, -12f, 12f, 3f, 15f, 0f)))),
                ("RoseAndCrown", null, Make(m => RoseAndCrown(m, new CqLot(-1, -15f, -2f, 3.5f, 12f, 0f), new CqLot(-1, -2f, 15f, 6.5f, 11f, 0f)))),
                ("PostOffice", null, Make(m => PostOffice(m, new CqLot(-1, -8.5f, 8.5f, 4f, 12f, 0f)))),
                ("StMatthews", null, Make(m => StMatthews(m, Vector3.zero))),
                ("AntiqueShop", null, Make(m => HeritageShop(m, new CqLot(-1, -4.7f, 4.7f, 2.6f, 11f, 0f), 1, E4.Cream, E4.Maroon, E4.HeritageGreen, "ANTIQUES", 2.5f))),
                ("TwoStoreyShop", null, Make(m => HeritageShop(m, new CqLot(-1, -4.7f, 4.7f, 2.6f, 11f, 0f), 2, E4.Brick, E4.Cream, E4.HeritageGreen, "BOOKS", 2.5f))),
                ("OldCourthouse", null, Make(m => ColonialBuilding(m, new CqLot(-1, -7.5f, 7.5f, 5f, 9f, 0f)))),
                ("HeritageLamp", null, Make(m => E4.HeritageLamp(m, Vector3.zero))),
                ("NorfolkPine", null, Make(m => E4.Pine(m, Vector3.zero, 18f, 3))),
                ("Jacaranda", null, Make(m => E4.Jacaranda(m, Vector3.zero, 8f, 4))),
                ("PillarBox", null, Make(m => PillarBox(m, Vector3.zero))),
                ("Trailer", null, Make(m => TrailerArt(m, new CqRampSpec { X = 0, Z = 0, Width = TrailerWidth, Rise = TrailerRise, Height = TrailerHeight, Top = TrailerTop }))),
                ("WineBarrel", barrel.gameObject, null),
                ("AFrame", aFrame.gameObject, null),
                ("TeaChest", chest.gameObject, null),
                ("HatStand", hatStand.gameObject, null),
                ("Magpie", magpie, null),
                ("Galah", galah, null),
                ("DrayRamp", dray.gameObject, null),
            };
            K.RenderAtlas("art-src/districts/guildford/props-atlas.png", items);
        }

        // ---------------------------------------------------------------- chunks

        /// <summary>James St's own things: jacarandas in bloom along the rail-side footpath (with their petals), black
        /// heritage lamps on the left kerb.</summary>
        static void JamesStreet(CqChunkDraft d, int seed, bool trees = true, bool lamps = true, float z0 = 0f, float z1 = 40f)
        {
            var r = new System.Random(seed * 19 + 3);
            if (trees)
                for (float z = 6f; z < 40f; z += 20f)
                {
                    if (z < z0 || z > z1) continue;
                    Vector3 p = V(K.Edge - .9f, d.Ground(z), z);
                    K.TreePit(d.Props, p, 1.3f);
                    E4.Jacaranda(d.Props, p + V(0, .1f, 0), F(r, 7f, 8.5f), seed + (int)z);
                    K.Thin(d.Collisions, "Jacaranda", p, 3f);
                    E4.PetalCarpet(d.Road, K.Edge - .9f, z, 2.4f, K.Kerb + .04f, seed + (int)z);
                }
            if (lamps)
                for (float z = 16f; z < 40f; z += 20f)
                {
                    if (z < z0 || z > z1) continue;
                    Vector3 p = V(-(K.Half + .55f), d.Ground(z), z);
                    E4.HeritageLamp(d.Props, p);
                    K.Thin(d.Collisions, "HeritageLamp", p, 4.4f, .35f);
                }
        }

        /// <summary>Brick paving beyond the left footpath (under verandahs, hotel forecourts): depth metres.</summary>
        static void Forecourt(CqChunkDraft d, float z0, float z1, float depth)
        {
            E4.Ground(d, -1, z0, z1, 0f, depth, E4.PaveBrick, ComicSurface.Paving);
        }

        static CqChunkDraft GfEntry()
        {
            // Off the river bridge into town: calm at z = 0, then picket fences, a Federation cottage, the jacarandas.
            var d = new CqChunkDraft("Guildford_Entry", GuildfordDrop);
            E4.BrickStreet(d);
            E4.Threshold(d);
            CalmLeft(d, 0f, 14f, 1, true);
            CalmRail(d, 0f, 14f, 2);
            E4.Ground(d, -1, 14f, 40f, 0f, 70f, E4.Lawn, ComicSurface.Foliage);
            var a = new CqLot(-1, 16f, 27f, K.Edge + 6f, 10f, d.Base(21.5f));
            var b = new CqLot(-1, 29f, 39.5f, K.Edge + 5f, 9f, d.Base(34f));
            Bungalow(d.Frontage, a, E4.Brick, E4.Iron, 3);
            Bungalow(d.Frontage, b, K.Hex("E9E2CE"), E4.IronDark, 4, true);
            FrontGarden(d, a, BungalowDoorZ(a, 3), 5); FrontGarden(d, b, BungalowDoorZ(b, 4), 6);
            d.Wall(a, 7f); d.Wall(b, 7f);
            E4.SignBoard(d.Props, V(-(K.Edge + 1.5f), d.Ground(15f), 15f), 0f, "HISTORIC GUILDFORD 1829", .07f, E4.HeritageGreen, E4.Cream, 2.4f);
            BackTrees(d, -1, 14f, 40f, 19f, 32f, 7, 8f);
            Railway(d, 8, true, 14f);
            JamesStreet(d, 9, true, true, 16f, 40f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft GfStation()
        {
            // The Guildford Hotel (belvedere, loggias, parapet with its name tablet) on the left, opposite the heritage
            // station building on its platform on the right.
            var d = new CqChunkDraft("Guildford_Station", GuildfordDrop);
            E4.BrickStreet(d);
            Forecourt(d, 0f, 40f, 4f);
            E4.Ground(d, -1, 0f, 40f, 4f, 70f, E4.Lawn, ComicSurface.Foliage);
            var hotel = new CqLot(-1, 7f, 31f, K.Edge + 3f, 15f, d.Base(19f));
            GuildfordHotel(d.Frontage, hotel);
            d.Wall(hotel, 12f);
            // the beer garden beyond: a rendered wall with an iron gate, a palm and a jacaranda behind it
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Plaster;
            K.SideBox(m, -1, 31.5f, 40f, -1f, K.Kerb + 1.9f, 2.6f, 2.9f, E4.HotelWhite);
            K.SideBox(m, -1, 31.5f, 40f, K.Kerb + 1.9f, K.Kerb + 2.05f, 2.5f, 3f, E4.HotelBand);
            K.SideBox(m, -1, 0f, 6.4f, -1f, K.Kerb + 1.9f, 2.6f, 2.9f, E4.HotelWhite);
            K.SideBox(m, -1, 0f, 6.4f, K.Kerb + 1.9f, K.Kerb + 2.05f, 2.5f, 3f, E4.HotelBand);
            foreach (float z in new[] { .3f, 3.2f, 6.1f, 31.8f, 34.6f, 37.4f, 39.7f })
            {
                m.Surface = ComicSurface.Plaster;
                K.MinMaxBox(m, V(-(K.Edge + 2.5f), -1f, z - .28f), V(-(K.Edge + 3f), K.Kerb + 2.3f, z + .28f), E4.HotelBand);
                m.Cylinder(V(-(K.Edge + 2.75f), K.Kerb + 2.5f, z), .2f, .4f, E4.HotelWhite, 6, .45f);
            }
            for (float z = 33f; z < 40f; z += 2.6f) E4.Bush(d.Props, V(-(K.Edge + 3.4f), d.Ground(z) + 1.5f, z), 1.5f, K.Hex("4E7F3A"), (int)z);
            for (float z = 1.6f; z < 6f; z += 2.6f) E4.Bush(d.Props, V(-(K.Edge + 3.4f), d.Ground(z) + 1.5f, z), 1.5f, K.Hex("5E8F45"), (int)z);
            K.Palm(d.Props, null, V(-(K.Edge + 7f), d.Ground(35.5f), 35.5f), 8.5f, 3);
            E4.Jacaranda(d.Props, V(-(K.Edge + 7.5f), d.Ground(2.8f), 2.8f), 8.5f, 11);
            E4.Bench(d.Props, V(-(K.Edge + 1.4f), d.Ground(34f), 34f), 90f, E4.HeritageGreen);
            d.Walls.Add(new Bounds(new Vector3(-(K.Edge + 3.2f), d.Y(3f) + 1f, 3f), new Vector3(1f, 4f, 6f)));
            d.Walls.Add(new Bounds(new Vector3(-(K.Edge + 3.2f), d.Y(36f) + 1f, 36f), new Vector3(1f, 4f, 8f)));
            BackTrees(d, -1, 0f, 40f, 22f, 34f, 12, 10f);
            // the station
            E4.RailCorridor(d, 0f, 40f, false);
            E4.Gantry(d, 20f, false);
            E4.Wires(d, 0f, 40f);
            E4.RailBackdrop(d, 0f, 40f, 13);
            HeritageStation(d, 11f, 27f);
            E4.Train(d.SlopedFrontage, E4.Track2, 5f, 1, false);
            JamesStreet(d, 14, false, true);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft GfJamesSt()
        {
            // The antique shops of James St under their post verandahs; the dealer's trailer in the left lane (a jump).
            var d = new CqChunkDraft("Guildford_JamesSt", GuildfordDrop);
            E4.BrickStreet(d);
            const float setback = 2.6f;
            Forecourt(d, 0f, 40f, setback + .2f);
            E4.Ground(d, -1, 0f, 40f, setback + .2f, 70f, E4.Lawn, ComicSurface.Foliage);
            (float z0, float z1, int kind, Color wall, Color trim, Color accent, string text)[] shops =
            {
                (.6f, 10.2f, 1, E4.Cream, E4.Maroon, E4.HeritageGreen, "ANTIQUES"),
                (10.4f, 20.2f, 2, E4.Brick, E4.Cream, E4.HeritageGreen, "BOOKS"),
                (20.4f, 29.6f, 0, K.Hex("D9E2D2"), E4.HeritageGreen, E4.Maroon, "CURIOS"),
                (29.8f, 39.4f, 1, K.Hex("F3D9B0"), K.Hex("2C4A6E"), K.Hex("2C4A6E"), "TEA ROOMS"),
            };
            foreach (var s in shops)
            {
                var l = new CqLot(-1, s.z0, s.z1, K.Edge + setback, 11f, d.Base((s.z0 + s.z1) * .5f));
                HeritageShop(d.Frontage, l, s.kind, s.wall, s.trim, s.accent, s.text, setback - .12f);
                d.Wall(l, 9f);
            }
            // goods on the footpath edge under the verandahs (beyond the rideable width)
            WineBarrelArt(d.Props, V(-(K.Edge + 1.5f), d.Ground(31.5f), 31.5f));
            WineBarrelArt(d.Props, V(-(K.Edge + 1.9f), d.Ground(32.6f), 32.6f));
            TeaChestArt(d.Props, V(-(K.Edge + 1.6f), d.Ground(3.4f), 3.4f), 15f);
            TeaChestArt(d.Props, V(-(K.Edge + 1.7f), d.Ground(22.2f), 22.2f), -10f);
            BackTrees(d, -1, 0f, 40f, 20f, 32f, 21, 10f);
            // the trailer: ramp planks, the deck, off the end
            var spec = new CqRampSpec { Name = "AntiqueTrailer", X = TrailerX, Z = TrailerZ, Width = TrailerWidth, Rise = TrailerRise, Height = TrailerHeight, Top = TrailerTop, Launch = 1.15f, Hang = .5f };
            d.Ramps.Add(spec);
            var art = new ComicMesh();
            TrailerArt(art, spec);
            d.SlopedFrontage.Add(art, Vector3.zero, Quaternion.identity, Vector3.one);
            Railway(d, 22);
            JamesStreet(d, 23);
            d.Slot(0f, 10f); d.Slot(2.6f, 10f); d.Slot(0f, 25f); d.Slot(2.6f, 25f); d.Slot(-2.6f, 34f);
            d.GiftSlots.Add(new Vector2(-2.6f, 28f));
            return d;
        }

        static CqChunkDraft GfRoseAndCrown()
        {
            // The Rose and Crown (1841): the long low bar wing under its rendered parapet, the two-storey brick house
            // with the lace verandah behind a lawn.
            var d = new CqChunkDraft("Guildford_RoseAndCrown", GuildfordDrop);
            E4.BrickStreet(d);
            Forecourt(d, 2f, 17.6f, 3.6f);
            E4.Ground(d, -1, 0f, 2f, 0f, 3.6f, E4.Lawn, ComicSurface.Foliage);
            E4.Ground(d, -1, 17.6f, 40f, 0f, 3.6f, E4.Lawn, ComicSurface.Foliage);
            E4.Ground(d, -1, 0f, 40f, 3.6f, 70f, E4.Lawn, ComicSurface.Foliage);
            var wing = new CqLot(-1, 3f, 17.4f, K.Edge + 3.5f, 12f, d.Base(10f));
            var house = new CqLot(-1, 17.4f, 34f, K.Edge + 6.5f, 11f, d.Base(26f));
            RoseAndCrown(d.Frontage, wing, house);
            d.Wall(wing, 7f); d.Wall(house, 10f);
            // brick path to the house door, bollards along the lawn, hedge, cafe tables under the wing's verandah
            E4.Patch(d.Road, -(K.Edge + 6.5f), -K.Edge, 25f, 26.6f, K.Kerb + .045f, E4.PaveBrick, ComicSurface.Paving);
            E4.ChainBollards(d.SlopedFrontage, -(K.Edge + .3f), 18.5f, 23.5f, K.Kerb);
            E4.ChainBollards(d.SlopedFrontage, -(K.Edge + .3f), 28f, 38f, K.Kerb);
            for (float z = 18.6f; z < 24f; z += 1.5f) E4.Bush(d.Props, V(-(K.Edge + 3.9f), d.Ground(z), z), 1f, K.Hex("4E7F3A"), (int)z);
            for (float z = 28f; z < 34f; z += 1.5f) E4.Bush(d.Props, V(-(K.Edge + 3.9f), d.Ground(z), z), 1f, K.Hex("4E7F3A"), (int)z);
            K.CafeSet(d.Props, V(-(K.Edge + 1.9f), d.Ground(6.5f), 6.5f), K.Hex("7A5436"));
            K.CafeSet(d.Props, V(-(K.Edge + 1.9f), d.Ground(13.5f), 13.5f), K.Hex("7A5436"));
            E4.Gum(d.Props, V(-(K.Edge + 5f), d.Ground(37.5f), 37.5f), 17f, 31);
            E4.Jacaranda(d.Props, V(-(K.Edge + 3f), d.Ground(.8f), .8f), 7.5f, 32);
            BackTrees(d, -1, 0f, 40f, 22f, 34f, 33, 9f);
            Railway(d, 34);
            E4.Train(d.SlopedFrontage, E4.Track1, 14f, 2, false);
            JamesStreet(d, 35);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft GfStirlingSquare()
        {
            // Stirling Square: the lawn behind white bollards and chains, St Matthew's (1873) in the middle with its
            // steep tiled roof and bellcote, Norfolk pines and palms, the memorial gates.
            var d = new CqChunkDraft("Guildford_StirlingSquare", GuildfordDrop);
            E4.BrickStreet(d);
            E4.Ground(d, -1, 0f, 40f, 0f, 70f, E4.Lawn, ComicSurface.Foliage);
            for (float z = 3f; z < 38f; z += 10f) E4.Patch(d.Road, -(K.Edge + 30f), -(K.Edge + .4f), z, z + 4f, K.Kerb + .035f, E4.LawnLight);
            E4.ChainBollards(d.SlopedFrontage, -(K.Edge + .35f), 1f, 16f, K.Kerb);
            E4.ChainBollards(d.SlopedFrontage, -(K.Edge + .35f), 23.5f, 38.5f, K.Kerb);
            // the gates and the path to the church door
            MemorialGates(d.Frontage, V(-(K.Edge + .9f), d.Ground(19.8f), 19.8f));
            E4.Patch(d.Road, -(K.Edge + 15f), -K.Edge, 18.9f, 20.7f, K.Kerb + .045f, E4.PaveBrick, ComicSurface.Paving);
            StMatthews(d.Frontage, V(-(K.Edge + 15f), d.Ground(19.8f), 19.8f));
            d.Walls.Add(new Bounds(new Vector3(-(K.Edge + 24f), d.Y(20f) + 3f, 20f), new Vector3(19f, 8f, 13f)));
            E4.Pine(d.Props, V(-(K.Edge + 6f), d.Ground(6f), 6f), 19f, 41);
            E4.Pine(d.Props, V(-(K.Edge + 8f), d.Ground(33f), 33f), 22f, 42);
            E4.Pine(d.Deep, V(-(K.Edge + 31f), d.Ground(4f), 4f), 21f, 43, true);
            E4.Pine(d.Deep, V(-(K.Edge + 34f), d.Ground(36f), 36f), 19f, 44, true);
            K.Palm(d.Props, null, V(-(K.Edge + 9.5f), d.Ground(12.5f), 12.5f), 9f, 5);
            E4.Jacaranda(d.Props, V(-(K.Edge + 6.5f), d.Ground(28.5f), 28.5f), 8f, 48);
            E4.PetalCarpet(d.Road, -(K.Edge + 6.5f), 28.5f, 2.8f, K.Kerb + .05f, 48);
            E4.Bench(d.Props, V(-(K.Edge + 3.2f), d.Ground(9f), 9f), 90f, E4.HeritageGreen);
            E4.Bench(d.Props, V(-(K.Edge + 3.2f), d.Ground(30f), 30f), 90f, E4.HeritageGreen);
            E4.SignBoard(d.Props, V(-(K.Edge + 1.5f), d.Ground(2f), 2f), 0f, "STIRLING SQUARE", .08f, E4.HeritageGreen, E4.Cream, 2.2f);
            BackTrees(d, -1, 0f, 40f, 40f, 55f, 45, 10f, 14f, 19f);
            Railway(d, 46);
            JamesStreet(d, 47, false);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft GfPostOffice()
        {
            // The Post Office (1900) with its clock tower on the corner, then the old colonial courthouse behind a
            // picket fence and a big gum.
            var d = new CqChunkDraft("Guildford_PostOffice", GuildfordDrop);
            E4.BrickStreet(d);
            Forecourt(d, 0f, 20f, 4.2f);
            E4.Ground(d, -1, 0f, 20f, 4.2f, 70f, E4.Lawn, ComicSurface.Foliage);
            E4.Ground(d, -1, 20f, 40f, 0f, 70f, E4.Lawn, ComicSurface.Foliage);
            var po = new CqLot(-1, 2f, 19f, K.Edge + 4f, 12f, d.Base(10.5f));
            PostOffice(d.Frontage, po);
            d.Wall(po, 12f);
            PillarBox(d.Props, V(-(K.Edge + 1.3f), d.Ground(17.2f), 17.2f));
            E4.Pickets(d.SlopedFrontage, -(K.Edge + .3f), 6.5f, 12f, K.Kerb, .9f, E4.White);
            var court = new CqLot(-1, 23f, 38f, K.Edge + 7f, 9f, d.Base(30.5f));
            ColonialBuilding(d.Frontage, court);
            d.Wall(court, 7f);
            E4.Pickets(d.SlopedFrontage, -(K.Edge + .3f), 21f, 29.6f, K.Kerb, .9f, E4.White);
            E4.Pickets(d.SlopedFrontage, -(K.Edge + .3f), 31.2f, 39.8f, K.Kerb, .9f, E4.White);
            E4.Patch(d.Road, -(K.Edge + 7f), -K.Edge, 29.7f, 31.1f, K.Kerb + .045f, E4.Gravel, ComicSurface.Concrete);
            E4.SignBoard(d.Props, V(-(K.Edge + 1.6f), d.Ground(22.2f), 22.2f), 0f, "OLD COURTHOUSE", .06f, E4.HeritageGreen, E4.Cream, 1.7f);
            E4.Gum(d.Props, V(-(K.Edge + 3.4f), d.Ground(21f), 21f), 18f, 51);
            E4.Bush(d.Props, V(-(K.Edge + 3f), d.Ground(27f), 27f), 1.2f, K.Hex("D8475A"), 3);
            E4.Bush(d.Props, V(-(K.Edge + 3.2f), d.Ground(34.5f), 34.5f), 1.2f, K.Hex("5E8F45"), 4);
            BackTrees(d, -1, 0f, 40f, 22f, 34f, 52, 9f);
            Railway(d, 53);
            JamesStreet(d, 54);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft GfExit()
        {
            // The edge of town: the first Swan Valley vines in rows behind a rail fence, then calm towards z = 40.
            var d = new CqChunkDraft("Guildford_Exit", GuildfordDrop);
            E4.BrickStreet(d);
            E4.Ground(d, -1, 0f, 26f, 0f, 70f, E4.Soil, ComicSurface.Plain);
            E4.Ground(d, -1, 0f, 26f, 0f, 2.4f, E4.Lawn, ComicSurface.Foliage, .03f);
            ComicMesh m = d.SlopedFrontage;
            for (float z = 2.2f; z < 25f; z += 2.6f)
            {
                m.Surface = ComicSurface.Foliage;
                K.MinMaxBox(m, V(-(K.Edge + 4f), K.Kerb + .5f, z - .45f), V(-(K.Edge + 34f), K.Kerb + 1.65f, z + .45f), K.Hex("5F8A3E"));
                K.MinMaxBox(m, V(-(K.Edge + 4.2f), K.Kerb + 1.65f, z - .3f), V(-(K.Edge + 33.8f), K.Kerb + 1.85f, z + .3f), K.Hex("7CA64C"));
                m.Surface = ComicSurface.Wood;
                m.Box(V(-(K.Edge + 3.8f), K.Kerb + .95f, z), V(.16f, 1.9f, .16f), E4.TimberDark);
            }
            E4.PostAndRail(m, -(K.Edge + 2.6f), .5f, 25.5f, K.Kerb, E4.TimberDark, 2);
            E4.SignBoard(d.Props, V(-(K.Edge + 1.3f), d.Ground(4f), 4f), 0f, "SWAN VALLEY", .09f, E4.Maroon, E4.Cream, 2.3f);
            // the cellar shed among the vines, gums behind
            ComicMesh deep = d.Deep;
            deep.Surface = ComicSurface.Metal;
            Vector3 shed = V(-(K.Edge + 44f), d.Ground(14f), 14f);
            deep.Box(shed + V(0, 2f, 0), V(9f, 4f, 13f), E4.Iron);
            var roof = new E4Plane(shed + V(4.8f, 0, -6.8f), Vector3.forward, Vector3.right);
            E4.RidgeRoof(deep, roof, 0f, 13.6f, 4f, 6.2f, 0f, -9.6f, E4.Tile);
            BackTrees(d, -1, 0f, 26f, 48f, 60f, 61, 7f, 14f, 18f);
            CalmLeft(d, 26f, 40f, 62, true);
            Railway(d, 63, true, 0f, 26f);
            CalmRail(d, 26f, 40f, 64);
            JamesStreet(d, 65, true, true, 0f, 24f);
            d.StandardSlots();
            return d;
        }

        // ---------------------------------------------------------------- the Guildford Hotel

        /// <summary>
        /// The Guildford Hotel (1886, rebuilt 1899 in Federation Free Classical; burnt out in 2008 and restored): white
        /// render, two storeys of loggias between banded piers, round-arched ground-floor windows, a cornice and a
        /// balustraded parapet with pediments and the name tablet under a curved pediment, and the timber belvedere
        /// with its wide eave and zinc bell dome behind. Loggias wrap round the corner facing the rider.
        /// </summary>
        static void GuildfordHotel(ComicMesh f, CqLot l)
        {
            E4Plane p = E4Plane.Lot(l);
            float W = l.W, D = l.Depth, g = 4.5f, top = 8.7f, lg = 2.2f;
            Color w = E4.HotelWhite, b = E4.HotelBand, shade = Color.Lerp(w, K.R.ink, .12f);
            int n = Mathf.Max(3, Mathf.RoundToInt(W / 4.8f));
            float bay = W / n;
            f.Surface = ComicSurface.Concrete;
            E4.Box(f, p, 0, W, -2.6f, 0f, lg + .02f, -D, K.R.ink);
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, p, 0, W, 0f, top, 0f, -D, shade);
            E4.Box(f, p, -.1f, W + .1f, 0f, .22f, lg + .1f, 0f, b);                                  // loggia floor
            E4.Box(f, p, -.1f, W + .1f, g - .05f, g + .3f, lg + .2f, 0f, b);                         // balcony slab
            E4.Box(f, p, -.12f, W + .12f, top - .1f, top + .42f, lg + .3f, -D - .1f, w);              // entablature
            E4.Box(f, p, -.25f, W + .25f, top + .42f, top + .58f, lg + .5f, -D - .2f, b);             // cornice
            for (int i = 0; i <= n; i++)
            {
                float u = Mathf.Clamp(i * bay, .45f, W - .45f);
                E4.Pier(f, p, u - .45f, u + .45f, .22f, g - .05f, lg - .85f, lg, w, b, .62f);
                E4.Pier(f, p, u - .4f, u + .4f, g + .3f, top - .1f, lg - .75f, lg, w, b, .62f, .42f);
                // parapet pedestal and its ball
                E4.Box(f, p, u - .4f, u + .4f, top + .58f, top + 1.75f, lg - .5f, lg + .12f, w);
                E4.Box(f, p, u - .48f, u + .48f, top + 1.75f, top + 1.88f, lg - .58f, lg + .2f, b);
                if (i % 2 == 0) f.Cylinder(p.P(u, top + 2.12f, lg - .2f), .24f, .46f, w, 6, .45f);
            }
            int tablet = 0, pediment = Mathf.Min(n - 1, 3), door = n / 2;
            for (int i = 0; i < n; i++)
            {
                float u0 = Mathf.Max(.45f, i * bay) + .45f, u1 = Mathf.Min(W - .45f, (i + 1) * bay) - .45f, uc = (u0 + u1) * .5f;
                f.Surface = ComicSurface.Plaster;
                E4.Box(f, p, u0 - .05f, u1 + .05f, g - .72f, g - .05f, lg - .62f, lg - .06f, w);          // ground lintel
                E4.Box(f, p, u0 - .05f, u1 + .05f, top - .78f, top - .1f, lg - .6f, lg - .06f, w);         // upper lintel
                E4.Box(f, p, u0 - .05f, u1 + .05f, g + .3f, g + 1.22f, lg - .32f, lg - .08f, w);           // solid balustrade
                E4.Box(f, p, u0 - .05f, u1 + .05f, g + 1.22f, g + 1.36f, lg - .4f, lg, b);
                E4.Quad(f, p, u0 + .3f, u1 - .3f, g + .5f, g + 1.04f, lg - .07f, b);
                // the wall behind: arched below, tall sash windows above
                if (i == door)
                {
                    f.Surface = ComicSurface.PaintedMetal;
                    E4.ArchFill(f, p, uc, 2f, .22f, 2.7f, 1f, .012f, K.R.ink, 3);
                    f.Surface = ComicSurface.Wood;
                    E4.Quad(f, p, uc - .85f, uc + .85f, .25f, 2.6f, .024f, E4.TimberDark);
                    E4.Quad(f, p, uc - .03f, uc + .03f, .25f, 2.6f, .034f, K.R.ink, false);
                    f.Surface = ComicSurface.Glass;
                    E4.ArchFill(f, p, uc, 1.8f, 2.7f, 2.7f, .9f, .024f, E4.GlassDark, 3);
                    f.Surface = ComicSurface.Plaster;
                    E4.ArchBand(f, p, uc, 2f, .22f, 2.7f, 1f, .018f, .2f, w, 3);
                }
                else E4.FlatArchWindow(f, p, uc, 1.5f, 1.1f, 2.75f, .75f, 0f, w, E4.GlassDark);
                E4.FlatWindow(f, p, uc, 1.25f, g + 1.05f, g + 3.15f, 0f, w, E4.GlassDark, false);
                // the parapet over this bay
                f.Surface = ComicSurface.Plaster;
                if (i == tablet)
                {
                    E4.Box(f, p, u0 - .05f, u1 + .05f, top + .58f, top + 2.35f, lg - .45f, lg + .06f, w);
                    E4.Quad(f, p, u0 + .2f, u1 - .2f, top + .85f, top + 2.1f, lg + .07f, b);
                    if (HotelNames)
                    {
                        E4.Text(f, p, "GUILDFORD", uc, top + 1.72f, lg + .085f, .078f, K.R.ink);
                        E4.Text(f, p, "HOTEL", uc, top + 1.18f, lg + .085f, .078f, K.R.ink);
                    }
                    f.Surface = ComicSurface.Plaster;
                    E4.ArchFill(f, p, uc, u1 - u0 + .1f, top + 2.35f, top + 2.35f, 1.05f, lg + .06f, w, 4);
                    E4.ArchTrim(f, p, uc, u1 - u0 + .1f, top + 2.35f, top + 2.35f, 1.05f, lg - .45f, .16f, .56f, b, 4, false);
                    E4.Box(f, p, u0 - .12f, u1 + .12f, top + 2.3f, top + 2.44f, lg - .5f, lg + .14f, b);
                    f.Cylinder(p.P(uc, top + 3.75f, lg - .2f), .26f, .5f, w, 6, .45f);
                }
                else if (i == pediment)
                {
                    E4.Box(f, p, u0 - .05f, u1 + .05f, top + .58f, top + 1.75f, lg - .45f, lg + .06f, w);
                    E4.Quad(f, p, u0 + .3f, u1 - .3f, top + .85f, top + 1.5f, lg + .07f, b);
                    E4.GableRoof(f, p, u0 - .3f, u1 + .3f, top + 1.75f, top + 3.05f, lg + .14f, lg - .5f, w, w);
                    E4.Tri(f, p, new Vector2(u0 + .3f, top + 1.9f), new Vector2(u1 - .3f, top + 1.9f), new Vector2(uc, top + 2.75f), lg + .15f, b);
                    E4.Box(f, p, u0 - .36f, u1 + .36f, top + 1.7f, top + 1.84f, lg - .52f, lg + .2f, b);
                }
                else E4.ParapetBalusters(f, p, u0 - .05f, u1 + .05f, top + .58f, top + 1.6f, lg - .06f, w);
            }
            // the corner facing the rider: the loggia's open end, windows, the parapet returning along the side
            E4Plane e = E4Plane.LotNear(l);
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, p, -.02f, .22f, g + .3f, g + 1.22f, lg - .85f, .05f, w);
            E4.Box(f, e, 0f, D, top + .58f, top + 1.5f, .1f, -.25f, w);
            E4.Box(f, e, -.1f, D + .1f, top + 1.5f, top + 1.64f, .18f, -.33f, b);
            for (int k = 0; k < 3; k++)
            {
                float u = D - 2.6f - k * 4.2f;
                if (u < 1.5f) break;
                E4.FlatArchWindow(f, e, u, 1.4f, 1.1f, 2.75f, .7f, 0f, w, E4.GlassDark);
                E4.FlatWindow(f, e, u, 1.2f, g + 1.05f, g + 3.15f, 0f, w, E4.GlassDark);
            }
            E4.Box(f, e, -.05f, D + .05f, g - .05f, g + .28f, .1f, 0f, b);
            // roof, chimneys
            f.Surface = ComicSurface.Metal;
            E4.RidgeRoof(f, p, .5f, W - .5f, top + .5f, top + 2.7f, -.6f, -D + .4f, E4.Iron, 3.2f);
            E4.Chimney(f, p.P(W * .2f, top + 1.2f, -D * .35f), 2.9f, w, .8f);
            E4.Chimney(f, p.P(W * .93f, top + .9f, -D * .5f), 2.9f, w, .8f);
            Belvedere(f, p.P(W * .62f, top + .5f, -D * .22f));
        }

        /// <summary>The hotel's belvedere: a rendered base, the open timber lookout (eight posts, a solid balustrade), a
        /// wide flat eave, the zinc bell dome and a tall finial.</summary>
        static void Belvedere(ComicMesh f, Vector3 c)
        {
            Color w = E4.HotelWhite, b = E4.HotelBand;
            Quaternion q = Quaternion.Euler(0, 22.5f, 0);
            f.Surface = ComicSurface.Plaster;
            f.Box(c + V(0, 2.4f, 0), V(4f, 4.8f, 4f), w);
            f.Box(c + V(0, 4.9f, 0), V(4.4f, .26f, 4.4f), b);
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 }) f.Box(c + V(sx * 1.9f, 2.4f, sz * 1.9f), V(.5f, 4.8f, .5f), b);
            c += V(0, 1.8f, 0);
            f.Cylinder(c + V(0, 3.75f, 0), 2.05f, 1.1f, w, 8, 1f, q);
            f.Cylinder(c + V(0, 4.34f, 0), 2.15f, .14f, b, 8, 1f, q);
            f.Surface = ComicSurface.PaintedMetal;
            f.Cylinder(c + V(0, 5.4f, 0), 1.45f, 2.1f, K.Hex("3A4650"), 8, 1f, q);                     // the shadowed room behind the posts
            for (int i = 0; i < 8; i++)
            {
                float a = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                Vector3 post = c + V(Mathf.Cos(a) * 1.95f, 5.45f, Mathf.Sin(a) * 1.95f);
                f.Box(post, V(.2f, 2.2f, .2f), w, Quaternion.Euler(0, -i * 45f, 0));
            }
            f.Surface = ComicSurface.Metal;
            f.Cylinder(c + V(0, 6.66f, 0), 3.4f, .22f, E4.Zinc, 8, 1f, q);                             // the wide eave
            f.Cylinder(c + V(0, 6.5f, 0), 3.15f, .12f, K.Hex("C9CFD3"), 8, 1f, q);
            f.Cylinder(c + V(0, 7.5f, 0), 2.4f, 1.5f, E4.Zinc, 8, .62f, q);                            // the bell dome
            f.Cylinder(c + V(0, 8.75f, 0), 1.48f, 1f, Color.Lerp(E4.Zinc, Color.white, .12f), 8, .3f, q);
            f.Cylinder(c + V(0, 9.4f, 0), .38f, .34f, E4.Zinc, 8, .6f, q);
            f.Surface = ComicSurface.PaintedMetal;
            f.Cylinder(c + V(0, 10.6f, 0), .06f, 2.2f, K.R.ink, 4);
            f.Cylinder(c + V(0, 9.8f, 0), .2f, .36f, K.R.ink, 6, .4f);
        }

        // ---------------------------------------------------------------- the heritage station

        /// <summary>Guildford station (flat road space): the platform, and on it the old station building in deep red
        /// painted brick with a cream band and a green plinth, a hipped iron roof, a canopy on green iron brackets
        /// towards the street, chimneys, the name boards, a white picket fence along the platform's back.</summary>
        static void HeritageStation(CqChunkDraft d, float z0, float z1)
        {
            E4.Platform(d, 2f, 38f);
            ComicMesh m = d.SlopedFrontage;
            float top = E4.RailY + .95f, len = z1 - z0;
            var p = new E4Plane(V(K.Edge + 1.05f, top, z1), Vector3.back, Vector3.left);
            Color wall = E4.BrickDeep, green = E4.HeritageGreen;
            m.Surface = ComicSurface.Brick;
            E4.Box(m, p, 0f, len, -(top - K.Kerb) - .4f, 0f, 0f, -.3f, Color.Lerp(wall, K.R.ink, .25f));
            E4.Box(m, p, 0f, len, 0f, 3.5f, 0f, -2.15f, wall);
            m.Surface = ComicSurface.PaintedMetal;
            E4.Box(m, p, -.03f, len + .03f, 0f, .36f, .03f, -2.18f, green);
            m.Surface = ComicSurface.Plaster;
            E4.Box(m, p, -.02f, len + .02f, 1.12f, 1.38f, .025f, -2.17f, E4.Cream);
            float[] windows = { len * .13f, len * .34f, len * .66f, len * .87f };
            foreach (float u in windows) E4.Window(m, p, u, 1.05f, 1.5f, 2.95f, 0f, E4.Cream, E4.GlassDark, 2, 2, .14f, .08f);
            E4.Door(m, p, len * .5f, 1.3f, 0f, 2.95f, 0f, E4.Cream, green, E4.GlassDark);
            // the name board over the door
            m.Surface = ComicSurface.PaintedMetal;
            E4.Box(m, p, len * .5f - 1.7f, len * .5f + 1.7f, 3.05f, 3.5f, 0f, .06f, E4.Cream);
            E4.Text(m, p, "GUILDFORD", len * .5f, 3.275f, .068f, .055f, E4.Maroon);
            // canopy on brackets, roof, chimneys
            m.Surface = ComicSurface.Metal;
            K.Face(m, p.P(-.6f, 3.45f, 0f), p.P(len + .6f, 3.45f, 0f), p.P(len + .6f, 3.05f, .95f), p.P(-.6f, 3.05f, .95f), E4.Iron, (p.N + Vector3.up * 2f).normalized);
            K.Face(m, p.P(-.6f, 3.38f, 0f), p.P(len + .6f, 3.38f, 0f), p.P(len + .6f, 2.98f, .95f), p.P(-.6f, 2.98f, .95f), Color.Lerp(E4.Iron, K.R.ink, .45f), Vector3.down);
            m.Surface = ComicSurface.PaintedMetal;
            E4.Box(m, p, -.62f, len + .62f, 2.86f, 3.04f, .9f, .98f, green);
            for (float u = .15f; u <= len; u += (len - .3f) / 5f)
            {
                E4.Beam(m, p.P(u, 2.2f, .04f), p.P(u, 3f, .9f), .08f, green);
                E4.Box(m, p, u - .05f, u + .05f, 2.1f, 3.3f, 0f, .08f, green);
            }
            m.Surface = ComicSurface.Metal;
            E4.RidgeRoof(m, p, -.45f, len + .45f, 3.5f, 4.9f, .3f, -2.5f, E4.Iron, 1.5f);
            E4.Chimney(m, p.P(len * .25f, 3.9f, -1.1f), 1.9f, wall, .6f);
            E4.Chimney(m, p.P(len * .75f, 3.9f, -1.1f), 1.9f, wall, .6f);
            // picket fence along the back of the platform, lamps, a luggage trolley
            E4.Pickets(m, K.Edge + 1.3f, 2.2f, z0 - .4f, top, 1f, E4.White);
            E4.Pickets(m, K.Edge + 1.3f, z1 + .4f, 37.8f, top, 1f, E4.White);
            E4.PlatformFurniture(m, top, 5f, 32f);
            E4.StationSign(d.Props, V(K.Edge + .6f, d.Ground(30.5f), 30.5f), "GUILDFORD");
        }

        // ---------------------------------------------------------------- the shops of James St

        /// <summary>
        /// A James St shop under a post verandah. kind 0: single storey, stepped parapet; 1: single storey, curved
        /// pediment with ball finials; 2: two storeys with a lace balcony over the footpath and a gable. The fascia
        /// carries a plain trade word (ANTIQUES, BOOKS: no brands).
        /// </summary>
        static void HeritageShop(ComicMesh f, CqLot l, int kind, Color wall, Color trim, Color accent, string text, float verandah)
        {
            E4Plane p = E4Plane.Lot(l);
            float W = l.W, D = l.Depth, sf = 3.3f, par = kind == 2 ? 7.4f : 5.5f;
            Color pier = Color.Lerp(wall, K.R.ink, .14f);
            f.Surface = ComicSurface.Concrete;
            E4.Box(f, p, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            f.Surface = kind == 2 ? ComicSurface.Brick : ComicSurface.Plaster;
            E4.Box(f, p, 0, W, 0f, par, 0f, -D, wall);
            f.Surface = ComicSurface.Plaster;
            E4.Pier(f, p, 0f, .5f, 0f, par, 0f, .12f, pier, trim, 0f);
            E4.Pier(f, p, W - .5f, W, 0f, par, 0f, .12f, pier, trim, 0f);
            E4.Box(f, p, W * .5f - .22f, W * .5f + .22f, 0f, sf, 0f, .1f, pier);
            E4.Shopfront(f, p, .62f, W * .5f - .3f, .05f, sf, 0f, accent, E4.Glass, trim);
            E4.Shopfront(f, p, W * .5f + .3f, W - .62f, .05f, sf, 0f, accent, E4.Glass, trim);
            // goods in the windows: a few coloured shapes behind the glass
            var r = new System.Random((int)(l.Z0 * 7f) + kind);
            Color[] goods = { K.Hex("C9A227"), K.Hex("8C3B2E"), K.Hex("E9E2CE"), K.Hex("3F6E8C"), K.Hex("7A5436") };
            f.Surface = ComicSurface.Plain;
            foreach (float uc in new[] { W * .22f, W * .8f })
                for (int i = 0; i < 3; i++)
                {
                    float u = uc + (i - 1) * .5f, h = F(r, .25f, .55f), wq = F(r, .1f, .17f);
                    E4.Quad(f, p, u - wq, u + wq, .72f, .72f + h, .03f, goods[r.Next(goods.Length)], false);
                }
            // the verandah: iron roof, posts at the kerb side of the forecourt, lace, the fascia sign
            float vEave = kind == 2 ? 3.45f : 3.25f, vWall = kind == 2 ? 3.6f : 3.95f;
            if (kind == 2)
            {
                // a balcony: deck, lace balustrade, upper posts and a second roof
                f.Surface = ComicSurface.Wood;
                E4.Box(f, p, -.05f, W + .05f, 3.42f, 3.62f, 0f, verandah + .05f, E4.TimberDark);
                f.Surface = ComicSurface.PaintedMetal;
                for (int i = 0; i < 4; i++)
                {
                    float u = Mathf.Lerp(.12f, W - .12f, i / 3f);
                    E4.Box(f, p, u - .07f, u + .07f, 0f, 6.35f, verandah - .12f, verandah + .02f, trim);
                    E4.Box(f, p, u - .11f, u + .11f, 0f, .5f, verandah - .16f, verandah + .06f, trim);
                    if (i < 3) E4.Balustrade(f, p, u + .07f, Mathf.Lerp(.12f, W - .12f, (i + 1) / 3f) - .07f, 3.62f, verandah, trim, E4.White);
                    if (i > 0) E4.Tri(f, p, new Vector2(u - .07f, 3.42f), new Vector2(u - .6f, 3.42f), new Vector2(u - .07f, 2.9f), verandah - .05f, E4.White);
                    if (i < 3) E4.Tri(f, p, new Vector2(u + .07f, 3.42f), new Vector2(u + .6f, 3.42f), new Vector2(u + .07f, 2.9f), verandah - .05f, E4.White);
                }
                f.Surface = ComicSurface.Metal;
                K.Face(f, p.P(-.2f, 6.9f, 0f), p.P(W + .2f, 6.9f, 0f), p.P(W + .2f, 6.4f, verandah + .25f), p.P(-.2f, 6.4f, verandah + .25f), E4.Iron, (p.N + Vector3.up * 3f).normalized);
                K.Face(f, p.P(-.2f, 6.82f, 0f), p.P(W + .2f, 6.82f, 0f), p.P(W + .2f, 6.32f, verandah + .25f), p.P(-.2f, 6.32f, verandah + .25f), Color.Lerp(E4.Iron, K.R.ink, .45f), Vector3.down);
                f.Surface = ComicSurface.PaintedMetal;
                E4.Box(f, p, -.2f, W + .2f, 6.2f, 6.38f, verandah - .12f, verandah + .04f, trim);
                // french doors onto the balcony
                E4.Window(f, p, W * .27f, 1.2f, 3.75f, 6f, 0f, E4.Cream, E4.GlassDark, 2, 3);
                E4.Window(f, p, W * .73f, 1.2f, 3.75f, 6f, 0f, E4.Cream, E4.GlassDark, 2, 3);
            }
            else E4.Verandah(f, p, -.05f, W + .05f, vEave, vWall, verandah, 4, E4.Iron, trim, E4.White, kind == 1);
            f.Surface = ComicSurface.PaintedMetal;
            float fv = kind == 2 ? 3.1f : 2.82f;
            E4.Box(f, p, W * .12f, W * .88f, fv, fv + .42f, verandah - .02f, verandah + .08f, accent);
            E4.Text(f, p, text, W * .5f, fv + .21f, verandah + .09f, Mathf.Min(.085f, (W * .7f) / (E4.TextWidth(text, 1f) + 1f)), E4.Cream);
            // parapet: cornice, a centre piece, the lean-to roof behind
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, p, -.12f, W + .12f, par - .1f, par + .2f, .22f, -.5f, trim);
            if (kind != 2)
            {
                // a dentil course under the cornice and a framed panel on the parapet wall
                for (float u = .7f; u < W - .7f; u += .56f) E4.Box(f, p, u, u + .24f, par - .32f, par - .1f, 0f, .12f, trim);
                E4.Quad(f, p, W * .14f, W * .86f, vWall + .3f, par - .5f, .012f, Color.Lerp(wall, trim, .22f));
                E4.Box(f, p, W * .12f, W * .88f, vWall + .18f, vWall + .3f, 0f, .08f, trim);
            }
            if (kind == 0)
            {
                E4.Box(f, p, W * .2f, W * .8f, par + .2f, par + .75f, .08f, -.4f, wall);
                E4.Box(f, p, W * .34f, W * .66f, par + .75f, par + 1.3f, .08f, -.4f, wall);
                E4.Box(f, p, W * .32f, W * .68f, par + 1.3f, par + 1.45f, .16f, -.46f, trim);
                E4.Quad(f, p, W * .39f, W * .61f, par + .5f, par + 1.1f, .09f, accent);
            }
            else if (kind == 1)
            {
                E4.Box(f, p, W * .22f, W * .78f, par + .2f, par + .7f, .08f, -.4f, wall);
                E4.ArchFill(f, p, W * .5f, W * .56f, par + .7f, par + .7f, 1.05f, .08f, wall, 4);
                E4.ArchTrim(f, p, W * .5f, W * .56f, par + .7f, par + .7f, 1.05f, -.4f, .16f, .5f, trim, 4, false);
                E4.ArchFill(f, p, W * .5f, W * .26f, par + .8f, par + .8f, .5f, .09f, accent, 3);
                foreach (float u in new[] { .25f, W - .25f }) f.Cylinder(p.P(u, par + .5f, -.14f), .24f, .56f, trim, 6, .4f);
            }
            else
            {
                E4.GableRoof(f, p, W * .16f, W * .84f, par + .2f, par + 2.5f, .1f, -D * .6f, E4.Iron, wall);
                E4.Bar(f, p, W * .16f, par + .25f, W * .5f, par + 2.55f, .11f, .18f, .12f, trim);
                E4.Bar(f, p, W * .84f, par + .25f, W * .5f, par + 2.55f, .11f, .18f, .12f, trim);
                E4.FlatArchWindow(f, p, W * .5f, .8f, par + .55f, par + 1.2f, .4f, .1f, E4.Cream, E4.GlassDark, 3, false, false);
                f.Cylinder(p.P(W * .5f, par + 2.95f, -.1f), .09f, .8f, K.R.ink, 4);
            }
            f.Surface = ComicSurface.Metal;
            K.Face(f, p.P(.1f, par - .2f, -.5f), p.P(W - .1f, par - .2f, -.5f), p.P(W - .1f, par - 1.1f, -D + .1f), p.P(.1f, par - 1.1f, -D + .1f), E4.IronDark, Vector3.up);
            if (kind != 2) E4.Chimney(f, p.P(W * .8f, par - .6f, -D * .55f), 1.9f, E4.Brick, .6f);
        }

        // ---------------------------------------------------------------- the Rose and Crown

        /// <summary>
        /// The Rose and Crown (1841, the oldest hotel in Western Australia still trading). wing: the single-storey bar
        /// wing by the footpath: whitewashed brick with red-brick quoins, a grey rendered parapet with the name and a
        /// low pediment, a skillion verandah on timber posts. house: the two-storey red brick house set back, with a
        /// hipped iron roof, tall chimneys and a two-storey timber verandah with a lace balustrade and lattice.
        /// </summary>
        static void RoseAndCrown(ComicMesh f, CqLot wing, CqLot house)
        {
            // ---- the bar wing
            E4Plane p = E4Plane.Lot(wing);
            float W = wing.W, D = wing.Depth, wh = 3.35f, par = 4.9f, ver = wing.Line - K.Edge - .15f;
            Color render = K.Hex("9A9B90"), renderDark = K.Hex("7F8076"), post = K.Hex("5E4634");
            f.Surface = ComicSurface.Concrete;
            E4.Box(f, p, 0, W, -2.6f, 0f, .02f, -D, K.R.ink);
            f.Surface = ComicSurface.Brick;
            E4.Box(f, p, 0, W, 0f, wh, 0f, -D, E4.White);
            foreach (float u in new[] { .3f, W * .36f, W * .68f, W - .3f }) E4.Box(f, p, u - .3f, u + .3f, 0f, wh, 0f, .06f, E4.Brick);
            E4.Door(f, p, W * .2f, 1.3f, .05f, 2.75f, 0f, E4.Brick, E4.TimberDark, E4.GlassDark);
            E4.Window(f, p, W * .52f, 1.4f, 1f, 2.6f, 0f, E4.Brick, E4.GlassDark, 3, 2);
            E4.Door(f, p, W * .84f, 1.2f, .05f, 2.75f, 0f, E4.Brick, E4.TimberDark, E4.GlassDark);
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, p, -.1f, W + .1f, wh, par, .12f, -D - .1f, render);
            E4.Box(f, p, -.2f, W + .2f, wh - .02f, wh + .16f, .2f, -D - .2f, renderDark);
            E4.Box(f, p, -.22f, W + .22f, par, par + .18f, .26f, -D - .22f, renderDark);
            if (HotelNames) E4.Text(f, p, "ROSE AND CROWN", W * .52f, (wh + par) * .5f + .06f, .135f, .1f, E4.White);
            // the low pediment at the near corner, turned to the rider as well
            E4.GableRoof(f, p, 0f, W * .34f, par + .18f, par + 1.25f, .2f, -.5f, renderDark, render);
            E4.Box(f, p, -.05f, W * .34f + .05f, par + .12f, par + .26f, .28f, -.56f, renderDark);
            E4Plane e = E4Plane.LotNear(wing);
            E4.Box(f, e, D - 4.4f, D + .12f, wh, par + .18f, .14f, 0f, render);
            E4.GableRoof(f, e, D - 4.5f, D + .2f, par + .18f, par + 1.25f, .2f, -.5f, renderDark, render);
            E4.FlatWindow(f, e, D - 2.2f, 1.2f, 1f, 2.6f, 0f, E4.Brick, E4.GlassDark);
            // the skillion verandah on timber posts
            E4.Verandah(f, p, -.3f, W + .1f, 2.72f, 3.3f, ver, 5, E4.Iron, post, post, false);
            f.Surface = ComicSurface.Metal;
            E4.RidgeRoof(f, p, .3f, W - .3f, par - .3f, par + 1.5f, -.6f, -D + .3f, E4.Iron, 2.6f);
            E4.Chimney(f, p.P(W * .55f, par + .3f, -D * .5f), 2.6f, E4.Brick, .75f);

            // ---- the two-storey house
            E4Plane h = E4Plane.Lot(house);
            float HW = house.W, HD = house.Depth, g = 3.6f, eave = 7f, vd = 2.3f;
            f.Surface = ComicSurface.Concrete;
            E4.Box(f, h, 0, HW, -2.6f, .3f, vd + .1f, -HD, K.Hex("CFC5AE"));
            f.Surface = ComicSurface.Brick;
            E4.Box(f, h, 0, HW, .3f, eave, 0f, -HD, E4.Brick);
            // openings: a door and two sash windows below, three french doors above (cream lintels)
            E4.Door(f, h, HW * .5f, 1.2f, .35f, 2.95f, 0f, E4.Cream, E4.HeritageGreen, E4.GlassDark);
            foreach (float u in new[] { HW * .2f, HW * .8f }) E4.Window(f, h, u, 1.15f, 1.15f, 2.9f, 0f, E4.Cream, E4.GlassDark, 2, 2);
            foreach (float u in new[] { HW * .2f, HW * .5f, HW * .8f }) E4.Window(f, h, u, 1.1f, g + .35f, g + 2.6f, 0f, E4.Cream, E4.GlassDark, 2, 3);
            // the verandah: deck, posts in two tiers, lace balustrade, lattice under the deck, the roof
            Color timber = K.Hex("A9A794");
            f.Surface = ComicSurface.Wood;
            E4.Box(f, h, -.1f, HW + .1f, .3f, .46f, 0f, vd + .1f, E4.Timber);
            E4.Box(f, h, -.1f, HW + .1f, g - .05f, g + .16f, 0f, vd + .1f, timber);
            f.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 5; i++)
            {
                float u = Mathf.Lerp(.1f, HW - .1f, i / 4f), next = Mathf.Lerp(.1f, HW - .1f, (i + 1) / 4f);
                E4.Box(f, h, u - .08f, u + .08f, .46f, eave - .45f, vd - .14f, vd + .02f, timber);
                if (i == 4) break;
                E4.Balustrade(f, h, u + .08f, next - .08f, g + .16f, vd, timber, E4.White);
                // lattice valance under the deck and lace brackets under the roof
                E4.Quad(f, h, u + .08f, next - .08f, g - .6f, g - .05f, vd - .06f, K.Hex("C9C2A8"), false);
                for (float x = u + .4f; x < next - .2f; x += .42f) E4.Quad(f, h, x - .03f, x + .03f, g - .58f, g - .07f, vd - .05f, K.Hex("8E8872"), false);
                E4.Tri(f, h, new Vector2(u + .08f, eave - .5f), new Vector2(u + .7f, eave - .5f), new Vector2(u + .08f, eave - 1.1f), vd - .05f, E4.White);
                E4.Tri(f, h, new Vector2(next - .08f, eave - .5f), new Vector2(next - .7f, eave - .5f), new Vector2(next - .08f, eave - 1.1f), vd - .05f, E4.White);
            }
            E4.Box(f, h, -.15f, HW + .15f, eave - .5f, eave - .32f, vd - .14f, vd + .04f, timber);
            f.Surface = ComicSurface.Metal;
            K.Face(f, h.P(-.3f, eave + .1f, 0f), h.P(HW + .3f, eave + .1f, 0f), h.P(HW + .3f, eave - .35f, vd + .3f), h.P(-.3f, eave - .35f, vd + .3f), E4.Iron, (h.N + Vector3.up * 3f).normalized);
            K.Face(f, h.P(-.3f, eave + .02f, 0f), h.P(HW + .3f, eave + .02f, 0f), h.P(HW + .3f, eave - .43f, vd + .3f), h.P(-.3f, eave - .43f, vd + .3f), Color.Lerp(E4.Iron, K.R.ink, .45f), Vector3.down);
            f.Surface = ComicSurface.PaintedMetal;
            E4.Box(f, h, -.3f, HW + .3f, eave, eave + .14f, .35f, -HD - .3f, K.R.ink);
            f.Surface = ComicSurface.Metal;
            E4.RidgeRoof(f, h, -.35f, HW + .35f, eave + .14f, eave + 3.1f, .4f, -HD - .35f, E4.Iron, HD * .42f);
            E4.Chimney(f, h.P(HW * .14f, eave + .9f, -HD * .5f), 3f, E4.Brick, .8f);
            E4.Chimney(f, h.P(HW * .86f, eave + .9f, -HD * .5f), 3f, E4.Brick, .8f);
            E4.Chimney(f, h.P(HW * .5f, eave + 1.4f, -HD * .75f), 2.6f, E4.Brick, .7f);
            // the end facing the rider (above the wing) and the hanging sign on its post by the path
            E4Plane he = E4Plane.LotNear(house);
            E4.FlatWindow(f, he, HD * .5f, 1.1f, g + .6f, g + 2.4f, 0f, E4.Cream, E4.GlassDark);
            Vector3 s = h.P(HW * .62f, 0f, house.Line - K.Edge - 1.2f);
            f.Surface = ComicSurface.PaintedMetal;
            f.Box(s + V(0, 1.9f, 0), V(.12f, 3.8f, .12f), K.R.ink);
            f.Box(s + V(0, 3.65f, .55f), V(.07f, .07f, 1.2f), K.R.ink);
            f.Box(s + V(0, 3.0f, .6f), V(.08f, 1f, .9f), E4.Cream);
            f.Box(s + V(.045f, 3.0f, .6f), V(.02f, .8f, .7f), E4.Maroon);
            f.Box(s + V(.06f, 3.05f, .6f), V(.02f, .3f, .3f), K.Hex("E0B43A"));
        }

        // ---------------------------------------------------------------- Stirling Square

        /// <summary>
        /// St Matthew's, Guildford (1873): orange-red brick Gothic with a salmon-painted plinth, a steep terracotta
        /// tiled roof, stone copings, stepped buttresses, lancet windows with cream surrounds, the white-painted pointed
        /// door, a gabled bellcote, and a cross-gable (transept) on the side facing the rider. The west front faces
        /// the road (+X); p is the middle of the front at ground level.
        /// </summary>
        static void StMatthews(ComicMesh f, Vector3 p)
        {
            const float w = 9f, len = 18f, wall = 5.2f, ridge = 11.2f;
            Color brick = E4.BrickOrange, plinth = E4.Salmon, stone = K.Hex("DDD3B8"), roof = K.Hex("B56A48");
            var front = new E4Plane(p + V(0, 0, -w * .5f), Vector3.forward, Vector3.right);
            var side = new E4Plane(p + V(-len, 0, -w * .5f), Vector3.right, Vector3.back);
            f.Surface = ComicSurface.Concrete;
            E4.Box(f, front, -.1f, w + .1f, -2f, 0f, .1f, -len - .1f, Color.Lerp(plinth, K.R.ink, .3f));
            f.Surface = ComicSurface.Brick;
            E4.Box(f, front, 0f, w, 0f, wall, 0f, -len, brick);
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, front, -.06f, w + .06f, 0f, 1.25f, .06f, -len - .06f, plinth);
            // the roof and the front gable
            f.Surface = ComicSurface.Brick;
            E4.GableRoof(f, front, -.35f, w + .35f, wall, ridge, 0f, -len - .3f, roof, brick);
            f.Surface = ComicSurface.Plaster;
            E4.Bar(f, front, -.35f, wall + .05f, w * .5f, ridge + .12f, .02f, .26f, .3f, stone);
            E4.Bar(f, front, w + .35f, wall + .05f, w * .5f, ridge + .12f, .02f, .26f, .3f, stone);
            E4.Box(f, front, -.5f, .25f, wall - .25f, wall + .2f, -.2f, .36f, stone);
            E4.Box(f, front, w - .25f, w + .5f, wall - .25f, wall + .2f, -.2f, .36f, stone);
            // the bellcote on the apex
            f.Surface = ComicSurface.Brick;
            E4.Box(f, front, w * .5f - .65f, w * .5f + .65f, ridge - .7f, ridge + 1.5f, .1f, -.55f, brick);
            E4.GableRoof(f, front, w * .5f - .8f, w * .5f + .8f, ridge + 1.5f, ridge + 2.5f, .2f, -.65f, stone, brick);
            f.Surface = ComicSurface.PaintedMetal;
            E4.ArchFill(f, front, w * .5f, .6f, ridge + .1f, ridge + .85f, .45f, .11f, K.R.ink, 3);
            f.Cylinder(front.P(w * .5f, ridge + .55f, .12f), .16f, .3f, K.Hex("C9A227"), 6, .6f);
            f.Box(front.P(w * .5f, ridge + 2.95f, -.2f), V(.1f, .9f, .1f), K.R.ink);
            f.Box(front.P(w * .5f, ridge + 3.1f, -.2f), V(.1f, .1f, .5f), K.R.ink);
            // the door: white-painted pointed arch, dark timber, steps with low piers
            E4.ArchFill(f, front, w * .5f, 2.2f, .55f, 2.5f, 1.55f, .012f, K.R.ink, 4);
            f.Surface = ComicSurface.Wood;
            E4.ArchFill(f, front, w * .5f, 1.95f, .6f, 2.5f, 1.4f, .024f, K.Hex("4E3F36"), 4);
            E4.Quad(f, front, w * .5f - .03f, w * .5f + .03f, .6f, 3.8f, .034f, K.R.ink, false);
            f.Surface = ComicSurface.Plaster;
            E4.ArchTrim(f, front, w * .5f, 2.35f, .55f, 2.5f, 1.65f, 0f, .26f, .14f, E4.White, 4);
            E4.Box(f, front, w * .5f - 1.6f, w * .5f + 1.6f, 0f, .28f, .12f, 1.5f, plinth);
            E4.Box(f, front, w * .5f - 1.4f, w * .5f + 1.4f, .28f, .55f, .12f, .9f, plinth);
            foreach (float u in new[] { w * .5f - 1.85f, w * .5f + 1.85f }) E4.Box(f, front, u - .28f, u + .28f, 0f, .95f, .9f, 1.6f, plinth);
            // a cross and a lancet in the gable, a stone string course
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, front, w * .5f - .06f, w * .5f + .06f, 4.5f, 5.5f, 0f, .06f, stone);
            E4.Box(f, front, w * .5f - .32f, w * .5f + .32f, 5.05f, 5.17f, 0f, .06f, stone);
            E4.FlatArchWindow(f, front, w * .5f, .7f, 6.6f, 8f, .7f, 0f, stone, E4.GlassDark, 3, true, false);
            // buttresses on the front: brick with salmon bases and sloped stone caps
            foreach (float u in new[] { .35f, w * .5f - 2.6f, w * .5f + 2.6f, w - .35f }) Buttress(f, front, u, .7f, 1.05f, 4.2f, brick, plinth, stone);
            // the side facing the rider: buttressed bays with lancets, the transept gable
            float tu0 = len * .28f, tu1 = len * .62f;
            f.Surface = ComicSurface.Brick;
            E4.Box(f, side, tu0, tu1, 0f, wall + .6f, 0f, 2.4f, brick);
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, side, tu0 - .06f, tu1 + .06f, 0f, 1.25f, 0f, 2.46f, plinth);
            f.Surface = ComicSurface.Brick;
            E4.GableRoof(f, side, tu0 - .3f, tu1 + .3f, wall + .6f, wall + 4.8f, 2.4f, -w * .5f, roof, brick);
            f.Surface = ComicSurface.Plaster;
            float tc = (tu0 + tu1) * .5f;
            E4.Bar(f, side, tu0 - .3f, wall + .65f, tc, wall + 4.9f, 2.42f, .24f, .28f, stone);
            E4.Bar(f, side, tu1 + .3f, wall + .65f, tc, wall + 4.9f, 2.42f, .24f, .28f, stone);
            foreach (float u in new[] { tc - 1.15f, tc, tc + 1.15f }) E4.FlatArchWindow(f, side, u, .7f, 2.4f, u == tc ? 4.5f : 4f, .7f, 2.4f, E4.Cream, E4.GlassDark, 3, true, false);
            Buttress(f, side, tu0 + .3f, .6f, 2.4f + .9f, 4.2f, brick, plinth, stone, 2.4f);
            Buttress(f, side, tu1 - .3f, .6f, 2.4f + .9f, 4.2f, brick, plinth, stone, 2.4f);
            foreach (float u in new[] { len * .12f, len * .8f, len * .93f })
                E4.FlatArchWindow(f, side, u, .75f, 2.1f, 3.6f, .7f, 0f, E4.Cream, E4.GlassDark, 3, true, false);
            foreach (float u in new[] { .35f, len * .7f, len - .35f }) Buttress(f, side, u, .7f, 1f, 4.2f, brick, plinth, stone);
        }

        /// <summary>A stepped Gothic buttress on a wall at u: w wide, standing out from o0 to outward, h high.</summary>
        static void Buttress(ComicMesh f, E4Plane p, float u, float w, float outward, float h, Color brick, Color plinth, Color stone, float o0 = 0f)
        {
            float mid = o0 + (outward - o0) * .6f;
            f.Surface = ComicSurface.Brick;
            E4.Box(f, p, u - w * .5f, u + w * .5f, 0f, h * .55f, o0, outward, brick);
            E4.Box(f, p, u - w * .5f, u + w * .5f, h * .55f, h, o0, mid, brick);
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, p, u - w * .5f - .05f, u + w * .5f + .05f, 0f, 1.25f, o0, outward + .06f, plinth);
            K.Face(f, p.P(u - w * .5f, h * .55f, outward), p.P(u + w * .5f, h * .55f, outward), p.P(u + w * .5f, h * .55f + .45f, mid), p.P(u - w * .5f, h * .55f + .45f, mid), stone, (p.N + Vector3.up).normalized);
            K.Face(f, p.P(u - w * .5f, h, mid), p.P(u + w * .5f, h, mid), p.P(u + w * .5f, h + .45f, o0), p.P(u - w * .5f, h + .45f, o0), stone, (p.N + Vector3.up).normalized);
        }

        /// <summary>The Stirling Square memorial gates: two brick piers with stone caps and balls, low curved wing
        /// walls, an iron arch between the piers (background only). p is on the gate's centre line.</summary>
        static void MemorialGates(ComicMesh f, Vector3 p)
        {
            foreach (float z in new[] { -1.5f, 1.5f })
            {
                f.Surface = ComicSurface.Brick;
                f.Box(p + V(0, 1.4f, z), V(.8f, 2.8f, .8f), E4.Brick);
                f.Surface = ComicSurface.Plaster;
                f.Box(p + V(0, 2.9f, z), V(1f, .22f, 1f), E4.StoneLight);
                f.Box(p + V(0, .25f, z), V(.95f, .5f, .95f), E4.StoneLight);
                f.Cylinder(p + V(0, 3.25f, z), .26f, .5f, E4.StoneLight, 6, .45f);
                f.Surface = ComicSurface.Brick;
                f.Box(p + V(0, .6f, z * 2.1f), V(.4f, 1.2f, 1.9f), E4.Brick);
                f.Surface = ComicSurface.Plaster;
                f.Box(p + V(0, 1.25f, z * 2.1f), V(.5f, .12f, 2f), E4.StoneLight);
            }
            f.Surface = ComicSurface.PaintedMetal;
            f.Beam(p + V(0, 2.9f, -1.3f), p + V(0, 3.7f, 0), .08f, K.R.ink);
            f.Beam(p + V(0, 3.7f, 0), p + V(0, 2.9f, 1.3f), .08f, K.R.ink);
            f.Box(p + V(0, 4f, 0), V(.08f, .5f, .08f), K.R.ink);
            f.Box(p + V(0, 4.1f, 0), V(.08f, .08f, .34f), K.R.ink);
        }

        // ---------------------------------------------------------------- the Post Office and the courthouse

        /// <summary>
        /// Guildford Post Office (1900, Federation Free Classical): cream render, an arcaded ground floor, sash windows
        /// with hoods above, a frieze reading POST OFFICE, a hipped terracotta tile roof, and on the corner facing the
        /// rider the four-stage clock tower: arched belfry openings, clock faces, a balustraded top with a flagpole.
        /// </summary>
        static void PostOffice(ComicMesh f, CqLot l)
        {
            E4Plane p = E4Plane.Lot(l), e = E4Plane.LotNear(l);
            float W = l.W, D = l.Depth, g = 4.4f, eave = 8.4f, tw = 4.4f;
            Color c = E4.Cream, band = K.Hex("E0CFA6"), dark = K.Hex("CDBB90");
            f.Surface = ComicSurface.Concrete;
            E4.Box(f, p, 0, W, -2.6f, 0f, .5f, -D, K.R.ink);
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, p, 0f, W, 0f, eave, 0f, -D, c);
            E4.Box(f, p, -.06f, W + .06f, 0f, .7f, .08f, -D - .06f, dark);
            E4.Box(f, p, -.1f, W + .1f, eave - .12f, eave + .3f, 0f, -D - .16f, band);
            E4.Box(f, p, tw, W + .08f, g - .1f, g + .14f, .14f, -D - .08f, band);
            E4.Box(f, p, tw, W + .1f, g + .14f, g + .95f, .04f, -D - .02f, band);
            E4.Text(f, p, "POST OFFICE", (tw + W) * .5f, g + .56f, .055f, .095f, K.Hex("5B4B3A"));
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, p, tw, W + .16f, eave - .12f, eave + .3f, .32f, -D - .16f, band);
            E4.Box(f, p, tw, W + .28f, eave + .3f, eave + .44f, .46f, -D - .28f, c);
            // three bays: arches below (the middle one is the door), hooded sash windows above
            int n = 3;
            float bay = (W - tw) / n;
            for (int i = 0; i < n; i++)
            {
                float uc = tw + (i + .5f) * bay;
                E4.Pier(f, p, uc - bay * .5f - .28f + (i == 0 ? .3f : 0f), uc - bay * .5f + .28f + (i == 0 ? .3f : 0f), .7f, g - .1f, 0f, .14f, c, band, .55f);
                if (i == 1)
                {
                    f.Surface = ComicSurface.PaintedMetal;
                    E4.ArchFill(f, p, uc, 2f, .1f, 2.55f, 1f, .012f, K.R.ink, 4);
                    f.Surface = ComicSurface.Wood;
                    E4.Quad(f, p, uc - .85f, uc + .85f, .12f, 2.5f, .024f, K.Hex("5A3F2E"));
                    E4.Quad(f, p, uc - .03f, uc + .03f, .12f, 2.5f, .034f, K.R.ink, false);
                    f.Surface = ComicSurface.Glass;
                    E4.ArchFill(f, p, uc, 1.8f, 2.6f, 2.6f, .85f, .024f, E4.GlassDark, 4);
                    f.Surface = ComicSurface.Plaster;
                    E4.ArchTrim(f, p, uc, 2.1f, .1f, 2.55f, 1.05f, 0f, .2f, .12f, band, 4);
                    E4.Box(f, p, uc - 1.3f, uc + 1.3f, 0f, .14f, .1f, 1.1f, dark);
                }
                else E4.ArchWindow(f, p, uc, 1.7f, 1.2f, 2.6f, .85f, 0f, band, E4.GlassDark, 4);
                E4.Window(f, p, uc, 1.3f, g + 1.5f, g + 3.3f, 0f, band, E4.GlassDark, 2, 2);
                f.Surface = ComicSurface.Plaster;
                E4.Box(f, p, uc - .95f, uc + .95f, g + 3.5f, g + 3.66f, 0f, .24f, c);
            }
            E4.Pier(f, p, W - .5f, W, .7f, g - .1f, 0f, .14f, c, band, .55f);
            // the roof behind and two rendered chimneys
            f.Surface = ComicSurface.Brick;
            E4.RidgeRoof(f, p, -.4f, W + .4f, eave + .3f, eave + 3.2f, .5f, -D - .4f, E4.Tile, D * .42f);
            E4.Chimney(f, p.P(W * .55f, eave + 1.2f, -D * .3f), 2.8f, c, .8f);
            E4.Chimney(f, p.P(W * .9f, eave + .9f, -D * .6f), 2.6f, c, .8f);
            // the end wall beside the tower
            f.Surface = ComicSurface.Plaster;
            foreach (float u in new[] { D * .3f, D * .62f })
            {
                if (u > D - tw - .8f) continue;
                E4.FlatArchWindow(f, e, u, 1.4f, 1.2f, 2.6f, .7f, 0f, band, E4.GlassDark);
                E4.FlatWindow(f, e, u, 1.2f, g + 1.5f, g + 3.3f, 0f, band, E4.GlassDark);
            }
            // ---- the clock tower on the corner (stands out half a metre on both faces)
            float t0 = -.1f, t1 = tw, of = .5f, ob = -(tw - .4f);
            float s1 = eave + .4f, s2 = s1 + 3.3f, s3 = s2 + 3.2f, s4 = s3 + .95f;
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, p, t0, t1, 0f, s3, of, ob, c);
            E4.Box(f, p, t0 - .08f, t1 + .08f, 0f, .7f, of + .08f, ob - .08f, dark);
            foreach (float v in new[] { g + .02f, s1, s2, s3 })
            {
                E4.Box(f, p, t0 - .14f, t1 + .14f, v - .16f, v + .12f, of + .14f, ob - .14f, band);
                E4.Box(f, p, t0 - .26f, t1 + .26f, v + .12f, v + .26f, of + .26f, ob - .26f, c);
            }
            float tc = (t0 + t1) * .5f;
            E4Plane te = new E4Plane(p.P(t0, 0f, ob), p.N, Vector3.back);             // the tower face towards the rider
            float td = of - ob, tec = td * .5f;
            foreach (E4Plane face in new[] { p.Move(0f, 0f, of), te })
            {
                bool road = Vector3.Dot(face.N, p.N) > .5f;
                float uc = road ? tc : tec;
                E4.FlatArchWindow(f, face, uc, 1.3f, 1.3f, 2.7f, .65f, 0f, band, E4.GlassDark);
                E4.FlatWindow(f, face, uc, 1.1f, g + 1.4f, g + 3.1f, 0f, band, E4.GlassDark);
                foreach (float du in new[] { -.75f, .75f }) E4.FlatArchWindow(f, face, uc + du, .8f, s1 + .7f, s1 + 2f, .4f, 0f, band, K.Hex("3A3F45"), 3, true, false);
                f.Surface = ComicSurface.Plaster;
                // corner pilasters of the upper stages
                foreach (float cu in new[] { road ? t0 + .3f : .3f, road ? t1 - .3f : td - .3f })
                    E4.Box(f, face, cu - .3f, cu + .3f, s1 + .26f, s3 - .16f, 0f, .08f, band);
                E4.ParapetBalusters(f, face, uc - 1.4f, uc + 1.4f, s3 + .26f, s4, -.05f, c);
            }
            K.Clock(f, p.P(tc, s2 + 1.65f, of + .02f), Quaternion.LookRotation(p.N), 1.05f, E4.White, K.R.ink);
            K.Clock(f, te.P(tec, s2 + 1.65f, .02f), Quaternion.LookRotation(te.N), 1.05f, E4.White, K.R.ink);
            // the top: corner pedestals with balls, the solid sides, the flagpole
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, p, t0, t1, s3 + .26f, s3 + .5f, of - .1f, ob + .1f, band);
            foreach (float u in new[] { t0 + .15f, t1 - .15f })
                foreach (float o in new[] { of - .15f, ob + .15f })
                {
                    E4.Box(f, p, u - .3f, u + .3f, s3 + .26f, s4 + .15f, o - .3f, o + .3f, c);
                    f.Cylinder(p.P(u, s4 + .42f, o), .22f, .44f, c, 6, .45f);
                }
            f.Surface = ComicSurface.PaintedMetal;
            f.Cylinder(p.P(tc, s4 + 1.6f, (of + ob) * .5f), .06f, 3.6f, E4.White, 6, .6f);
            f.Box(p.P(tc, s4 + 3f, (of + ob) * .5f) + V(0, 0, .55f), V(.04f, .6f, 1f), K.Hex("2A3E6E"));
        }

        /// <summary>The old colonial courthouse (1866): soft red brick, a steep hipped shingle roof with wide eaves over
        /// a verandah on plain posts, twelve-pane windows, a panelled door, brick chimneys.</summary>
        static void ColonialBuilding(ComicMesh f, CqLot l)
        {
            E4Plane p = E4Plane.Lot(l);
            float W = l.W, D = l.Depth, wall = 3.5f, vd = 2.2f;
            Color brick = K.Hex("B9634A"), shingle = K.Hex("6F5D52");
            f.Surface = ComicSurface.Concrete;
            E4.Box(f, p, 0, W, -2.6f, .3f, vd + .1f, -D, K.Hex("CFC5AE"));
            f.Surface = ComicSurface.Brick;
            E4.Box(f, p, 0, W, .3f, wall, 0f, -D, brick);
            E4.Door(f, p, W * .5f, 1.2f, .35f, 2.9f, 0f, E4.White, E4.HeritageGreen, E4.GlassDark);
            foreach (float u in new[] { W * .17f, W * .34f, W * .66f, W * .83f }) E4.Window(f, p, u, 1.05f, 1.1f, 2.8f, 0f, E4.White, E4.GlassDark, 3, 4, .1f, .08f);
            f.Surface = ComicSurface.Wood;
            E4.Box(f, p, -.1f, W + .1f, .3f, .44f, 0f, vd + .1f, E4.Timber);
            f.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 6; i++)
            {
                float u = Mathf.Lerp(.1f, W - .1f, i / 5f);
                E4.Box(f, p, u - .08f, u + .08f, .44f, wall - .35f, vd - .14f, vd + .02f, E4.White);
            }
            E4.Box(f, p, -.15f, W + .15f, wall - .4f, wall - .22f, vd - .14f, vd + .04f, E4.White);
            // one big roof sweeping down over the verandah
            f.Surface = ComicSurface.Wood;
            E4.RidgeRoof(f, p, -.5f, W + .5f, wall - .2f, wall + 3.9f, vd + .45f, -D - .5f, shingle, (D + vd) * .45f);
            f.Surface = ComicSurface.PaintedMetal;
            E4.Box(f, p, -.5f, W + .5f, wall - .3f, wall - .16f, vd + .45f, -D - .5f, K.R.ink);
            E4.Chimney(f, p.P(W * .2f, wall + 1.2f, -D * .5f), 3.2f, brick, .75f);
            E4.Chimney(f, p.P(W * .8f, wall + 1.2f, -D * .5f), 3.2f, brick, .75f);
        }

        /// <summary>A red pillar box (no markings).</summary>
        static void PillarBox(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .1f, 0), .3f, .2f, K.R.ink, 8);
            m.Cylinder(p + V(0, .8f, 0), .26f, 1.2f, K.Hex("C8322B"), 8);
            m.Cylinder(p + V(0, 1.46f, 0), .31f, .12f, K.Hex("A8261F"), 8);
            m.Cylinder(p + V(0, 1.62f, 0), .29f, .22f, K.Hex("C8322B"), 8, .35f);
            m.Box(p + V(.27f, 1.12f, 0), V(.03f, .06f, .24f), K.R.ink);
        }

        // ---------------------------------------------------------------- the trailer, obstacles, animals

        /// <summary>The antique dealer's flatbed trailer (in flat road space at the ramp spec): two loading planks up to
        /// the deck, timber deck with a steel frame, wheels and mudguards, the drawbar ahead.</summary>
        static void TrailerArt(ComicMesh m, CqRampSpec r)
        {
            K.RampMesh(m, r, E4.DeckLight, E4.Deck, false, 0f);
            float x0 = r.X - r.Width * .5f, x1 = r.X + r.Width * .5f, z1 = r.Z + r.Rise, z2 = r.Lip, h = r.Height;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { x0 - .04f, x1 + .04f })
            {
                m.Box(V(x, h - .06f, (z1 + z2) * .5f), V(.1f, .16f, z2 - z1), K.Hex("2C4A6E"));
                float sx = x < r.X ? -1f : 1f;
                m.Surface = ComicSurface.Rubber;
                m.Cylinder(V(x + sx * .16f, .36f, (z1 + z2) * .5f + .2f), .36f, .24f, K.R.ink, 8, 1f, Quaternion.Euler(0, 0, 90));
                m.Surface = ComicSurface.PaintedMetal;
                m.Cylinder(V(x + sx * .3f, .36f, (z1 + z2) * .5f + .2f), .16f, .04f, E4.Steel, 6, 1f, Quaternion.Euler(0, 0, 90));
                m.Box(V(x + sx * .18f, .8f, (z1 + z2) * .5f + .2f), V(.34f, .06f, 1f), K.Hex("2C4A6E"));
            }
            m.Box(V(r.X, h - .1f, z2 + .02f), V(r.Width + .2f, .2f, .08f), K.Hex("2C4A6E"));
            m.Beam(V(r.X - .5f, h - .25f, z2), V(r.X, .5f, z2 + 1.5f), .08f, K.Hex("2C4A6E"));
            m.Beam(V(r.X + .5f, h - .25f, z2), V(r.X, .5f, z2 + 1.5f), .08f, K.Hex("2C4A6E"));
            m.Box(V(r.X, .28f, z2 + 1.45f), V(.08f, .56f, .08f), E4.Steel);
            foreach (float x in new[] { x0 + .3f, x1 - .3f }) m.Box(V(x, h - .12f, z2 + .06f), V(.2f, .12f, .05f), K.R.red);
        }

        static void WineBarrelArt(ComicMesh m, Vector3 p)
        {
            Color oak = K.Hex("9A6B45"), dark = K.Hex("7A5436");
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, .17f, 0), .33f, .34f, oak, 8, 1.2f);
            m.Cylinder(p + V(0, .48f, 0), .4f, .3f, dark, 8);
            m.Cylinder(p + V(0, .8f, 0), .4f, .34f, oak, 8, .83f);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float y in new[] { .1f, .34f, .63f, .9f }) m.Cylinder(p + V(0, y, 0), y > .2f && y < .8f ? .415f : .36f, .05f, K.R.ink, 8);
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, .98f, 0), .3f, .03f, Color.Lerp(oak, Color.white, .2f), 8);
        }

        static ComicMesh WineBarrelMesh() => Make(m => WineBarrelArt(m, Vector3.zero));

        static void TeaChestArt(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.Wood;
            m.Box(p + V(0, .39f, 0), V(.74f, .74f, .64f), K.Hex("C9A66B"), q);
            m.Surface = ComicSurface.Metal;
            foreach (float x in new[] { -.36f, .36f })
                foreach (float z in new[] { -.31f, .31f }) m.Box(p + q * V(x, .39f, z), V(.07f, .78f, .07f), E4.SteelDark, q);
            m.Box(p + V(0, .77f, 0), V(.78f, .05f, .68f), E4.SteelDark, q);
            m.Surface = ComicSurface.Plain;
            m.Box(p + q * V(0, .42f, -.325f), V(.3f, .3f, .01f), K.Hex("8C3B2E"), q * Quaternion.Euler(0, 0, 45));
        }

        static ComicMesh TeaChestMesh() => Make(m => TeaChestArt(m, Vector3.zero, 0f));

        // A sandwich board outside an antique shop: green frame, cream panels, a teapot shape and OPEN. Faces -Z and +Z.
        static ComicMesh AntiqueAFrameMesh()
        {
            var m = new ComicMesh();
            foreach (int s in new[] { -1, 1 })
            {
                Quaternion q = Quaternion.Euler(s * 14f, 0, 0);
                Vector3 c = V(0, .5f, s * .2f);
                m.Surface = ComicSurface.Wood;
                m.Box(c, V(.82f, 1.04f, .05f), E4.HeritageGreen, q);
                m.Box(c + q * V(0, 0, s * .03f), V(.68f, .88f, .02f), E4.Cream, q);
                Quaternion face = q * Quaternion.Euler(0, s > 0 ? 180f : 0f, 0);
                Vector3 o = c + q * V(0, 0, s * .045f);
                E4.Text(m, "OPEN", o + q * V(0, -.2f, 0), face, .055f, E4.Maroon);
                m.Surface = ComicSurface.Plain;
                m.Box(o + q * V(0, .16f, 0), V(.3f, .2f, .004f), K.Hex("C9A227"), q);
                m.Box(o + q * V(.2f, .2f, 0), V(.12f, .06f, .004f), K.Hex("C9A227"), q);
                m.Box(o + q * V(0, .29f, 0), V(.12f, .05f, .004f), K.Hex("C9A227"), q);
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, 1.03f, 0), V(.84f, .05f, .14f), K.R.ink);
            return m;
        }

        // An old hat stand out on the footpath: turned post on a tripod, curved pegs, a straw hat and a scarf.
        static ComicMesh HatStandMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            Color wood = K.Hex("6B4630");
            for (int i = 0; i < 3; i++)
            {
                Quaternion q = Quaternion.Euler(0, i * 120f, 0);
                E4.Limb(m, q * V(.3f, .03f, 0), V(0, .35f, 0), .04f, .05f, wood, 5);
            }
            m.Cylinder(V(0, 1f, 0), .05f, 1.5f, wood, 6);
            m.Cylinder(V(0, 1.8f, 0), .07f, .12f, wood, 6, .5f);
            for (int i = 0; i < 4; i++)
            {
                Quaternion q = Quaternion.Euler(0, i * 90f + 20f, 0);
                E4.Limb(m, V(0, 1.55f, 0), q * V(.26f, 1.7f, 0), .03f, .025f, wood, 4);
            }
            m.Surface = ComicSurface.Fabric;
            m.Cylinder(V(.26f, 1.72f, .09f), .2f, .03f, K.Hex("E6CF8A"), 8);
            m.Cylinder(V(.26f, 1.79f, .09f), .11f, .12f, K.Hex("E6CF8A"), 8, .85f);
            m.Cylinder(V(.26f, 1.75f, .09f), .115f, .04f, E4.Maroon, 8);
            m.Box(V(-.2f, 1.35f, -.16f), V(.1f, .7f, .05f), K.Hex("3F6E8C"), Quaternion.Euler(0, 40f, 6f));
            return m;
        }

        // An Australian magpie: black with a white nape, back panel and wing bar, grey-white bill, walks. Facing -Z.
        static ComicMesh MagpieMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color black = K.Hex("1E2126"), white = K.Hex("F2F0E8");
            E4.Blob(m, V(0, .26f, .04f), V(.24f, .22f, .42f), black, 6, 3, .3f);
            m.Box(V(0, .3f, .3f), V(.12f, .05f, .26f), black, Quaternion.Euler(-12f, 0, 0));
            m.Box(V(0, .34f, .06f), V(.2f, .04f, .2f), white);
            m.Box(V(0, .42f, -.14f), V(.15f, .15f, .17f), black);
            m.Box(V(0, .45f, -.07f), V(.155f, .1f, .06f), white);
            foreach (float x in new[] { -.125f, .125f }) m.Box(V(x, .27f, .1f), V(.02f, .06f, .22f), white);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .41f, -.28f), V(.05f, .05f, .14f), K.Hex("C9CFD3"));
            m.Box(V(0, .41f, -.36f), V(.04f, .04f, .04f), black);
            foreach (float x in new[] { -.05f, .05f }) { m.Box(V(x, .08f, .02f), V(.03f, .16f, .03f), black); m.Box(V(x, .015f, -.02f), V(.06f, .03f, .12f), black); }
            foreach (float x in new[] { -.08f, .08f }) m.Box(V(x, .45f, -.2f), V(.02f, .03f, .03f), K.Hex("B5522E"));
            return m;
        }

        // A galah: rose-pink breast and face, pale crest, grey wings and tail. Facing -Z.
        static ComicMesh GalahMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color pink = K.Hex("E0708C"), grey = K.Hex("A9AEB6"), crest = K.Hex("F3D5DC");
            E4.Blob(m, V(0, .24f, .02f), V(.24f, .26f, .34f), pink, 6, 3, .3f);
            foreach (float x in new[] { -.12f, .12f }) m.Box(V(x, .26f, .08f), V(.05f, .18f, .3f), grey);
            m.Box(V(0, .24f, .26f), V(.14f, .05f, .2f), grey, Quaternion.Euler(-15f, 0, 0));
            m.Box(V(0, .42f, -.08f), V(.17f, .16f, .17f), pink);
            m.Box(V(0, .52f, -.06f), V(.15f, .06f, .16f), crest);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .4f, -.19f), V(.07f, .07f, .06f), K.Hex("E9E2CE"));
            foreach (float x in new[] { -.05f, .05f }) m.Box(V(x, .05f, 0f), V(.04f, .1f, .08f), K.Hex("6E6A66"));
            foreach (float x in new[] { -.088f, .088f }) m.Box(V(x, .44f, -.13f), V(.02f, .03f, .03f), K.R.ink);
            return m;
        }

        // ---------------------------------------------------------------- far scenery

        /// <summary>
        /// Guildford's horizon (no CBD out here): to the left the town's roofs, church gables and big trees, the flat
        /// green Swan Valley with its striped vineyard blocks, and the Darling Scarp, now close and high; to the right
        /// beyond the railway more roofs, the Helena River tree line, and the scarp again. Sheared with the road's grade.
        /// </summary>
        static ComicMesh GfFar(float grade)
        {
            var m = new ComicMesh();
            var r = new System.Random(310);
            Color haze = K.Hex("CFDDD2");
            const float z0 = -220f, z1 = 820f;
            E4.FarStrip(m, -K.Edge - .5f, -70f, 330f, z1, K.Kerb - .05f, E4.Lawn);
            E4.FarStrip(m, -70f, -240f, z0, z1, -.3f, E4.Haze(K.Hex("86A35C"), 150f, haze));
            E4.FarStrip(m, -240f, -520f, z0, z1, -.35f, E4.Haze(K.Hex("A9B870"), 380f, haze));
            E4.FarStrip(m, -520f, -1400f, z0, z1, -.4f, E4.Haze(K.Hex("9AAE7A"), 700f, haze));
            E4.FarStrip(m, 70f, 260f, z0, z1, -.25f, E4.Haze(K.Hex("93A866"), 160f, haze));
            E4.FarStrip(m, 260f, 1400f, z0, z1, -.3f, E4.Haze(K.Hex("9AAE7A"), 600f, haze));
            Color[] walls = { E4.Cream, E4.White, E4.Brick, K.Hex("D9CBB0"), E4.BrickOrange };
            Color[] roofs = { E4.Tile, E4.Iron, E4.IronDark, K.Hex("8A6A58") };
            foreach (int s in new[] { -1, 1 })
                for (float z = z0; z < z1; z += F(r, 12f, 18f))
                    for (int row = 0; row < 3; row++)
                    {
                        float x = s * (76f + row * 27f + F(r, 0f, 12f)), dist = Mathf.Sqrt(x * x + z * z * .1f);
                        float w = F(r, 9f, 13f), h = F(r, 3.2f, 6.5f);
                        m.Surface = ComicSurface.Plaster;
                        m.Box(V(x, h * .5f - 1f, z), V(w, h + 2f, w * .8f), E4.Haze(walls[r.Next(walls.Length)], dist, haze));
                        m.Surface = ComicSurface.Metal;
                        m.Cylinder(V(x, h + .8f, z), w * .74f, 2.2f, E4.Haze(roofs[r.Next(roofs.Length)], dist, haze), 4, .15f, Quaternion.Euler(0, 45, 0));
                    }
            foreach (int s in new[] { -1, 1 })
            {
                E4.TreeLine(m, r, s * 68f, 6f, z0, z1, 13f, 13f, K.Hex("5E7E46"), K.Hex("7C9A58"), haze);
                E4.TreeLine(m, r, s * 175f, 30f, z0, z1, 19f, 15f, K.Hex("58764A"), K.Hex("6F8F52"), haze);
                E4.TreeLine(m, r, s * 320f, 50f, z0, z1, 28f, 15f, K.Hex("5F7D52"), K.Hex("7A9460"), haze);
            }
            // jacarandas among the roofs, a church gable and a water tower
            for (int i = 0; i < 16; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * F(r, 72f, 150f), z = F(r, z0, z1);
                E4.Clump(m, V(x, 7f, z), V(11f, 6f, 11f), E4.Haze(i % 3 == 0 ? E4.JacB : E4.JacA, Mathf.Abs(x), haze), i, 6, 3);
            }
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(V(-210f, 16f, 420f), 5f, 4f, E4.Haze(K.Hex("C9CED3"), 300f, haze), 8, 1.25f);
            m.Cylinder(V(-210f, 7f, 420f), 1.6f, 14f, E4.Haze(K.Hex("AEB5BB"), 300f, haze), 6);
            // vineyard blocks of the Swan Valley (rows as stripes), running away to the left
            for (int b = 0; b < 9; b++)
            {
                float bx = -255f - (b % 3) * 78f, bz = z0 + 40f + b * 112f;
                m.Surface = ComicSurface.Plain;
                K.Face(m, V(bx + 4f, -.1f, bz - 4f), V(bx + 4f, -.1f, bz + 74f), V(bx - 62f, -.1f, bz + 74f), V(bx - 62f, -.1f, bz - 4f), E4.Haze(E4.Soil, Mathf.Abs(bx), haze), Vector3.up, false);
                for (int row = 0; row < 10; row++)
                {
                    float x = bx - row * 6f;
                    m.Surface = ComicSurface.Foliage;
                    m.Box(V(x, .9f, bz + 35f), V(2.6f, 2f, 70f), E4.Haze(K.Hex("5F8A3E"), Mathf.Abs(x), haze));
                }
            }
            E4.Ridge(m, -1, 500f, 260f, z0, z1, -.4f, z => 58f + 11f * Mathf.Sin(z * .007f + .6f) + 6f * Mathf.Sin(z * .021f), K.Hex("8CA070"), K.Hex("6A8A76"), K.Hex("7C988E"), haze, .36f);
            E4.Ridge(m, 1, 560f, 260f, z0, z1, -.4f, z => 52f + 10f * Mathf.Sin(z * .006f + 2.1f) + 5f * Mathf.Sin(z * .019f), K.Hex("8CA070"), K.Hex("6A8A76"), K.Hex("7C988E"), haze, .42f);
            m.Shear(grade);
            return m;
        }
    }
}
