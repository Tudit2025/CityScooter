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
    /// <summary>MIDLAND (order 340, the end of the line): see <see cref="East5Builder"/>. The town on the left (Great
    /// Eastern Hwy shops, the Town Hall, Centrepoint, the bus interchange, the station car park), the railway yard,
    /// the new station and the Railway Workshops on the right.</summary>
    public static partial class East5Builder
    {
        const float T4 = 31.6f, YardEnd = 34.5f, ShopsX = 44f;
        static readonly float[] YardTracks = { T1, T2, T3, T4 };

        static void BuildMidland(TrackTheme theme, bool atlas)
        {
            K.Begin(MidMeshes, MidPrefabs, CbdBuilder.DataFolder);
            Obstacle trolley = K.SaveObstacle(MidPf("OBS_Midland_Trolley"), "OBS_Midland_Trolley", TrolleyMesh(), new Vector3(.65f, 1.05f, 1f), ObstacleKind.Thing, true, true, 1f);
            Obstacle barrier = K.SaveObstacle(MidPf("OBS_Midland_WaterBarrier"), "OBS_Midland_WaterBarrier", WaterBarrierMesh(), new Vector3(2f, .9f, .5f), ObstacleKind.Thing, true, false, .9f);
            Obstacle pallet = K.SaveObstacle(MidPf("OBS_Midland_Pallet"), "OBS_Midland_Pallet", PalletMesh(), new Vector3(1.2f, .9f, 1f), ObstacleKind.Thing, true, true, .9f);
            Obstacle hoops = K.SaveObstacle(MidPf("OBS_Midland_BikeHoops"), "OBS_Midland_BikeHoops", BikeHoopsMesh(), new Vector3(2.1f, .95f, .35f), ObstacleKind.Thing, true, false, .7f);
            GameObject magpie = SaveAnimal(MidPf("ANM_Midland_Magpie"), "ANM_Midland_Magpie", MagpieMesh(), new Vector3(0, .25f, 0), new Vector3(.4f, .5f, .7f), .5f, true);
            GameObject galah = SaveAnimal(MidPf("ANM_Midland_Galah"), "ANM_Midland_Galah", GalahMesh(), new Vector3(0, .22f, 0), new Vector3(.4f, .45f, .6f), .3f, true);
            GameObject pigeon = SaveAnimal(MidPf("ANM_Midland_Pigeon"), "ANM_Midland_Pigeon", PigeonMesh(), new Vector3(0, .2f, 0), new Vector3(.35f, .4f, .55f), 0f, true);
            Ramp loading = K.SaveRamp(MidPf("RMP_Midland_LoadingRamp"), "RMP_Midland_LoadingRamp", 3f, 5f, 1.3f, 1.5f, 1f, .5f, Steel, SteelDark, true);

            K.WriteChunk(MidEntry(), MidChunkPath("Entry"));
            K.WriteChunk(MidHighway(), MidChunkPath("Highway"));
            K.WriteChunk(MidTownHall(), MidChunkPath("TownHall"));
            K.WriteChunk(MidCentrepoint(), MidChunkPath("Centrepoint"));
            K.WriteChunk(MidStation(), MidChunkPath("Station"));
            K.WriteChunk(MidTerminus(), MidChunkPath("Terminus"));
            K.WriteChunk(MidWorkshops(), MidChunkPath("Workshops"));
            K.WriteChunk(MidExit(), MidChunkPath("Exit"));
            GameObject far = K.SaveFarScenery(MidPf("FAR_Midland_DarlingScarp"), "FAR_Midland_DarlingScarp", MidFar(-MidDrop / 40f));

            bool created = AssetDatabase.LoadAssetAtPath<District>(MidDistrictPath) == null;
            District d = K.LoadOrCreate<District>(MidDistrictPath);
            // Placeholders when the asset is first created; the district system owns these fields after this.
            if (created) { d.skyYaw = 180f; d.skySize = .6f; d.skyHeight = -12f; d.skyline = 0f; }
            d.id = MidId; d.displayName = "MIDLAND"; d.order = 340; d.length = new Vector2(320f, 320f);     // entry + the six chunks + exit: each passes once
            d.chunks = MidChunkIds.Select(c => Chunk(MidChunkPath(c))).ToArray();
            d.orderedChunks = true;
            d.entry = Chunk(MidChunkPath("Entry"));
            d.exit = Chunk(MidChunkPath("Exit"));
            d.allowBoulevard = false;
            d.obstacles = new[] { trolley, barrier, pallet, hoops };
            d.jumpObstacles = new[] { barrier, pallet, hoops };
            d.gifts = Array.Empty<Gift>();
            d.hazards = ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_OncomingPair", "HZ_Crossing", "HZ_CrossingBusy", "HZ_SidewalkStroll", "HZ_PoliceStop", "HZ_RoadworksShort");
            var people = new List<GameObject>(K.People(theme, "Shopper", "Shopper", "Office", "Student", "Student", "Courier", "Kid", "Elder", "Tourist", "Barista", "Skater"));
            people.Add(magpie); people.Add(galah); people.Add(galah); people.Add(pigeon);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { loading };
            d.rampFrequency = 1f;
            d.blendMeters = 150f;
            // The foot of the hills on a hot clear day: warm sun on red brick, a blue-lilac haze towards the scarp.
            d.fogColor = new Color(.80f, .85f, .90f);
            d.skyTint = new Color(1f, .99f, .97f);
            d.ambientSky = new Color(.70f, .83f, .97f);
            d.ambientEquator = new Color(.90f, .86f, .78f);
            d.ambientGround = new Color(.66f, .56f, .46f);
            d.sunColor = new Color(1f, .94f, .83f);
            d.sunIntensity = 1.45f;
            d.farScenery = far;
            // gateway, ambience, ambienceVolume, the sky fields and line belong to the district system: not written here.
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/Midland] " + string.Join("; ", K.Report));
            if (atlas) RenderMidAtlas(trolley, barrier, pallet, hoops, magpie, galah, pigeon, loading);
        }

        static void RenderMidAtlas(Obstacle trolley, Obstacle barrier, Obstacle pallet, Obstacle hoops, GameObject magpie, GameObject galah, GameObject pigeon, Ramp loading)
        {
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("TownHall", null, Shown(TownHall().M)),
                ("StationConcourse", null, Shown(Make(m => { Concourse(m, 7f, 33f); ConcourseBridge(m, 29.5f); }))),
                ("WorkshopBlock", null, Shown(WorkshopBlock(26.4f, 6, false, "2").M)),
                ("Centrepoint", null, Shown(Centrepoint(36f).M)),
                ("FederationShop", null, Shown(FedShop(12f, Brick, GreenRoof, 0, true).M)),
                ("FederationShopB", null, Shown(FedShop(13f, K.Hex("E3D3B0"), RedOxide, 1, true).M)),
                ("FederationShopC", null, Shown(FedShop(13f, BrickDark, K.Hex("2F4E6E"), 2, true).M)),
                ("CarPark", null, Shown(CarPark(30f).M)),
                ("WaterTank", null, Make(m => WaterTank(m, Vector3.zero))),
                ("Chimney", null, Make(m => Chimney(m, Vector3.zero, 30f))),
                ("Hospital", null, Shown(Make(m => Hospital(m, Vector3.zero)))),
                ("Bus", null, Make(m => Bus(m, Vector3.zero, 0f))),
                ("BusShelter", null, Shown(Make(m => BusCanopy(m, Vector3.zero, 12f, 1)))),
                ("Kiosk", null, Make(m => Kiosk(m, Vector3.zero))),
                ("Wheelset", null, Make(m => Wheelset(m, Vector3.zero))),
                ("BufferStop", null, Make(m => BufferStop(m, 0f, 0f))),
                ("StationTotem", null, Shown(Make(m => StationTotem(m, m, Vector3.zero)))),
                ("BrickPlanter", null, Make(m => { BrickPlanter(m, V(0, 0, 0), 4f, 3f, .6f); GrassTree(m, V(0, .6f, 0), 1.1f); })),
                ("ParkedCar", null, Make(m => ParkedCar(m, Vector3.zero, 0f, K.Hex("C8372D")))),
                ("TrolleyBay", null, Make(m => TrolleyBay(m, Vector3.zero))),
                ("PylonSign", null, Shown(Make(m => PylonSign(m, m, Vector3.zero)))),
                ("Trolley", trolley.gameObject, null),
                ("WaterBarrier", barrier.gameObject, null),
                ("Pallet", pallet.gameObject, null),
                ("BikeHoops", hoops.gameObject, null),
                ("Magpie", magpie, null),
                ("Galah", galah, null),
                ("Pigeon", pigeon, null),
                ("LoadingRamp", loading.gameObject, null),
            };
            K.RenderAtlas("art-src/districts/midland/props-atlas.png", items);
        }

        // ---------------------------------------------------------------- Midland sides

        static void MidStreet(CqChunkDraft d) => K.StreetSurface(d.Road, d.Length, K.R.asphalt, Paving, KerbCap, true);

        /// <summary>The railway yard on the right: four tracks, wires over the passenger lines, a wide portal at z = 20,
        /// dry grass beyond and the long sawtooth blocks of the Railway Workshops across the yard.</summary>
        static void YardSide(CqChunkDraft d, float z0, float z1, int seed, bool gantry = true, bool workshops = true)
        {
            RailBed(d, z0, z1, YardTracks, YardEnd, true, Color.Lerp(DryGrass, Ballast, .35f));
            Wires(d, z0, z1, T1, T2, T3);
            if (gantry && z0 <= 20f && z1 >= 20f) Gantry(d.SlopedFrontage, 20f, BedX0 + .5f, T3 + 2.2f, T1, T2, T3);
            SideWall(d, 1, z0, z1, 30f);
            if (workshops) AddSheared(d, d.Deep, m => WorkshopsAcrossTheYard(m, z0, z1));
            // a wagon or two on the back road
            if (seed % 2 == 0) AddSheared(d, d.Deep, m => Wagons(m, T4, z0 + 6f, 2, seed));
        }

        /// <summary>The Workshops seen across the yard (flat road space, no outline): the long red-brick side wall with
        /// piers, arched windows and a cornice, the sawtooth roof above (glazed faces towards the rider).</summary>
        static void WorkshopsAcrossTheYard(ComicMesh m, float z0, float z1)
        {
            const float x = ShopsX, h = 7.4f, bay = 4f;
            Vector3 n = Vector3.left;
            for (float z = z0; z < z1 - .01f; z += bay)
            {
                float e = Mathf.Min(z1, z + bay);
                m.Surface = ComicSurface.Brick;
                K.Face(m, V(x, -2f, z), V(x, -2f, e), V(x, h, e), V(x, h, z), Brick, n, false);
                m.Surface = ComicSurface.Concrete;
                K.Face(m, V(x - .02f, -.2f, z), V(x - .02f, -.2f, e), V(x - .02f, .8f, e), V(x - .02f, .8f, z), StoneShade, n, false);
                K.Face(m, V(x - .02f, h - .5f, z), V(x - .02f, h - .5f, e), V(x - .02f, h - .2f, e), V(x - .02f, h - .2f, z), BrickLight, n, false);
                m.Surface = ComicSurface.Brick;
                K.MinMaxBox(m, V(x - .3f, -.2f, z), V(x, h, z + .5f), BrickDark);
                m.Surface = ComicSurface.Glass;
                float zc = (z + e) * .5f + .25f;
                K.Face(m, V(x - .03f, 1.6f, zc - .8f), V(x - .03f, 1.6f, zc + .8f), V(x - .03f, 5.2f, zc + .8f), V(x - .03f, 5.2f, zc - .8f), GlassDark, n, false);
                K.TriFacing(m, V(x - .03f, 5.2f, zc - .8f), V(x - .03f, 5.2f, zc + .8f), V(x - .03f, 5.9f, zc), GlassDark, n);
                // one sawtooth: the glazed face looks back at the rider, the iron roof falls away behind it
                m.Surface = ComicSurface.Glass;
                K.Face(m, V(x, h, z), V(x + 18f, h, z), V(x + 18f, h + 2.2f, z), V(x, h + 2.2f, z), GlassLight, Vector3.back, false);
                m.Surface = ComicSurface.Metal;
                K.Face(m, V(x, h + 2.2f, z), V(x + 18f, h + 2.2f, z), V(x + 18f, h, e), V(x, h, e), IronRoofDark, V(0, 1, 1), false);
                K.TriFacing(m, V(x, h, z), V(x, h + 2.2f, z), V(x, h, e), Brick, n);
            }
        }

        /// <summary>Open goods wagons standing on a track (flat road space).</summary>
        static void Wagons(ComicMesh m, float x, float z, int count, int seed)
        {
            Color[] cols = { K.Hex("8C4A36"), K.Hex("4E6A80"), K.Hex("6E6E66") };
            for (int i = 0; i < count; i++)
            {
                float a = z + i * 11f;
                m.Surface = ComicSurface.PaintedMetal;
                K.MinMaxBox(m, V(x - 1.1f, BedY + .2f, a + .6f), V(x + 1.1f, BedY + .9f, a + 9.4f), Ink);
                K.MinMaxBox(m, V(x - 1.4f, BedY + .9f, a), V(x + 1.4f, BedY + 2.9f, a + 10f), cols[(seed + i) % cols.Length]);
                m.Surface = ComicSurface.Plain;
                for (float r = a + 2f; r < a + 9.5f; r += 2f) K.Face(m, V(x - 1.42f, BedY + 1f, r), V(x - 1.42f, BedY + 1f, r + .12f), V(x - 1.42f, BedY + 2.8f, r + .12f), V(x - 1.42f, BedY + 2.8f, r), Ink, Vector3.left, false);
            }
        }

        /// <summary>Paving out to the building line on the left (forecourts under the shop verandahs), asphalt behind.</summary>
        static void TownGround(CqChunkDraft d, float z0, float z1, float paved, Color? behind = null)
        {
            Ground(d, -1, z0, z1, 0f, paved, K.Kerb + .02f, Paving, ComicSurface.Paving);
            Ground(d, -1, z0, z1, paved, 60f, K.Kerb + .015f, behind ?? K.Hex("9C978C"), ComicSurface.Asphalt);
        }

        /// <summary>Roofs and treetops of the town behind the main street (no outline).</summary>
        static void TownBehind(CqChunkDraft d, float z0, float z1, int seed, float from = 26f)
        {
            var r = new System.Random(seed * 23 + 7);
            Color[] walls = { K.Hex("D9C9A8"), K.Hex("C9B08A"), Cream, K.Hex("B98A6A"), K.Hex("D6D2C8") };
            ComicMesh m = d.Deep;
            for (float z = z0 + 1f; z < z1 - 4f; z += F(r, 10f, 14f))
            {
                float w = F(r, 9f, 13f), h = F(r, 5f, 9f), x = -(K.Edge + from + F(r, 0f, 8f)), y = d.Ground(z) - 2f;
                Color c = walls[r.Next(walls.Length)];
                float zc = z + w * .4f, top = y + h + 2f, fx = x + w * .5f, fz = zc - w * .4f;
                m.Surface = ComicSurface.Plaster;
                m.Box(V(x, y + h * .5f + 1f, zc), V(w, h + 2f, w * .8f), c);
                m.Box(V(x, top + .2f, zc), V(w + .4f, .4f, w * .8f + .4f), Stone);
                m.Box(V(x, y + 3.6f, zc), V(w + .1f, 3.2f, w * .8f + .1f), Color.Lerp(c, Ink, .22f));
                // framed windows on the faces you see (towards the road and towards the rider)
                for (float wy = y + 6.1f; wy < top - 1.6f; wy += 2.9f)
                {
                    for (float wz = fz + 1.2f; wz < fz + w * .8f - 1.6f; wz += 2.6f)
                    {
                        m.Surface = ComicSurface.Plain;
                        K.Face(m, V(fx + .02f, wy, wz), V(fx + .02f, wy, wz + 1.4f), V(fx + .02f, wy + 1.6f, wz + 1.4f), V(fx + .02f, wy + 1.6f, wz), Ink, Vector3.right, false);
                        K.Face(m, V(fx + .04f, wy + .1f, wz + .1f), V(fx + .04f, wy + .1f, wz + 1.3f), V(fx + .04f, wy + 1.5f, wz + 1.3f), V(fx + .04f, wy + 1.5f, wz + .1f), Glass, Vector3.right, false);
                    }
                    for (float wx = x - w * .5f + 1.2f; wx < fx - 1.6f; wx += 2.6f)
                    {
                        K.Face(m, V(wx, wy, fz - .02f), V(wx + 1.4f, wy, fz - .02f), V(wx + 1.4f, wy + 1.6f, fz - .02f), V(wx, wy + 1.6f, fz - .02f), Ink, Vector3.back, false);
                        K.Face(m, V(wx + .1f, wy + .1f, fz - .04f), V(wx + 1.3f, wy + .1f, fz - .04f), V(wx + 1.3f, wy + 1.5f, fz - .04f), V(wx + .1f, wy + 1.5f, fz - .04f), Glass, Vector3.back, false);
                    }
                }
                m.Surface = ComicSurface.Metal;
                if (r.NextDouble() < .5) m.Cylinder(V(x, top + 1.4f, zc), w * .62f, 2f, r.NextDouble() < .5 ? RedOxide : IronRoofDark, 4, .2f, Quaternion.Euler(0, 45, 0));
                else m.Box(V(x + 1f, top + 1f, zc), V(w * .3f, 1.2f, w * .3f), SteelDark);
                Gum(m, V(x + F(r, 6f, 9f), d.Ground(z) - .3f, z + F(r, 5f, 9f)), F(r, 10f, 15f), seed + (int)z, true);
            }
        }

        // ---------------------------------------------------------------- Midland chunks

        static CqChunkDraft MidHighway()
        {
            // Great Eastern Hwy: two-storey Federation shops with posted verandahs on the left, the yard on the right.
            var d = new CqChunkDraft("Midland_Highway", MidDrop);
            MidStreet(d);
            YardSide(d, 0, 40, 341);
            AddSheared(d, d.Frontage, m => Train(m, T1, 12f, 2, true, false));
            TownGround(d, 0, 40, 3.4f);
            PutLeft(d, d.Frontage, FedShop(12f, Brick, GreenRoof, 0, true), 1f, 12f, 3.4f, 12f, 9f);
            PutLeft(d, d.Frontage, FedShop(13f, K.Hex("E3D3B0"), RedOxide, 1, false), 13f, 13f, 3.4f, 12f, 9f);
            PutLeft(d, d.Frontage, FedShop(13f, BrickDark, K.Hex("2F4E6E"), 2, false), 26f, 13f, 3.4f, 12f, 9f);
            foreach (float z in new[] { 7f, 27f })
            {
                Vector3 p = V(-(K.Half + 1f), d.Ground(z), z);
                K.TreePit(d.Props, p, 1.3f);
                JacarandaTree(d.Props, p + V(0, .1f, 0), 6.2f, (int)z);
                K.Thin(d.Collisions, "Jacaranda", p, 3f);
            }
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(16f), 16f), -1);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(36f), 36f), -1);
            K.TrafficLight(d.Props, d.Collisions, -1, d.Ground(38.5f), 38.5f);
            TownBehind(d, 0, 40, 342);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MidTownHall()
        {
            // Midland Town Hall on its corner: the colonnade, the round tower, the grey dome and the square clock tower.
            var d = new CqChunkDraft("Midland_TownHall", MidDrop);
            MidStreet(d);
            YardSide(d, 0, 40, 343);
            AddSheared(d, d.Deep, m => Chimney(m, V(ShopsX + 9f, 0f, 9f), 34f));
            Ground(d, -1, 0, 40, 0f, 60f, K.Kerb + .02f, BrickPaving, ComicSurface.Paving);
            Vector3 p = V(-(K.Edge + 4.6f), d.Ground(16f), 5f);
            Put(d.Landmark, TownHall(), p, -90f);
            d.Walls.Add(new Bounds(V(-(K.Edge + 11f), d.Y(18f) + 4f, 18f), V(18f, 12f, 30f)));
            // the square in front of the corner: a flagpole, a bench, plane trees along the footpath
            Flagpole(d.Props, V(-(K.Edge + 1.5f), d.Ground(1.5f), 1.5f), 9f);
            Bench(d.Props, V(-(K.Edge + 1f), d.Ground(34.5f), 34.5f), -90f);
            foreach (float z in new[] { 21f, 36f })
            {
                Vector3 t = V(-(K.Half + 1f), d.Ground(z), z);
                K.PlaneTree(d.Props, d.Collisions, t, .85f);
            }
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(10f), 10f), -1);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(30f), 30f), -1);
            JacarandaTree(d.Props, V(-(K.Edge + 4.5f), d.Ground(35f), 35f), 6.5f, 11);
            BrickPlanter(d.Frontage, V(-(K.Edge + 4.5f), d.Ground(35f) - .05f, 35f), 2.6f, 2.6f, .5f);
            SideWall(d, -1, 31f, 40f);
            TownBehind(d, 0, 40, 344, 30f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MidCentrepoint()
        {
            // Midland Centrepoint behind its car park: the entrance portal and the big sign, trolley bays, parked cars.
            var d = new CqChunkDraft("Midland_Centrepoint", MidDrop);
            MidStreet(d);
            YardSide(d, 0, 40, 345);
            AddSheared(d, d.Frontage, m => Train(m, T2, -8f, 2, false, true));
            AddSheared(d, d.Landmark, m => WaterTank(m, V(ShopsX - 5f, BedY, 30f)));
            ComicMesh g = d.Road;
            Ground(d, -1, 0, 40, 0f, 60f, K.Kerb + .015f, K.R.asphalt, ComicSurface.Asphalt);
            Ground(d, -1, 0, 40, 12.5f, 14.5f, K.Kerb + .03f, Paving, ComicSurface.Paving);
            // parking bays (two rows nose to nose), an entry lane, a planted strip along the footpath
            for (float z = 1.2f; z < 39f; z += 2.7f)
                foreach (float row in new[] { 4.4f, 10f })
                    Stripe(g, -(K.Edge + row + 2.4f), -(K.Edge + row - 2.4f), z, z + .1f, K.Kerb + .02f, White);
            HedgeRow(d.SlopedFrontage, -1, 0f, 15f, .25f, 1.1f, .7f);
            HedgeRow(d.SlopedFrontage, -1, 21f, 40f, .25f, 1.1f, .7f);
            var r = new System.Random(34);
            Color[] cars = { K.Hex("F1F1EE"), K.Hex("C9CED3"), K.Hex("C8372D"), K.Hex("2F5D8A"), K.Hex("2A2E33"), K.Hex("E9E3D3"), K.Hex("6E7E58") };
            foreach (float row in new[] { 4.4f, 10f })
                for (float z = 2.55f; z < 38f; z += 2.7f)
                    if (r.NextDouble() < (row < 8f ? .42 : .5) && Mathf.Abs(z - 18f) > 3.5f)
                        ParkedCar(d.Frontage, V(-(K.Edge + row + (row < 8f ? .2f : -.2f)), d.Ground(z), z), row < 8f ? 90f : -90f, cars[r.Next(cars.Length)]);
            TrolleyBay(d.Props, V(-(K.Edge + 7.2f), d.Ground(18f), 18f));
            PylonSign(d.Props, d.Deep, V(-(K.Edge + 1.8f), d.Ground(17.5f), 17.5f));
            foreach (float z in new[] { 6f, 31f }) StreetLamp(d.Props, V(-(K.Edge + 7.2f), d.Ground(z), z), -1);
            Gum(d.Props, V(-(K.Edge + 7.2f), d.Ground(11f), 11f), 9f, 351);
            Gum(d.Props, V(-(K.Edge + 7.2f), d.Ground(26f), 26f), 10f, 352);
            PutLeft(d, d.Landmark, Centrepoint(36f), 2f, 36f, 14.5f, 20f, 8f);
            // the name again on the end wall the rider is riding towards
            PixelText(d.Deep, "CENTREPOINT", -(K.Edge + 19.8f), d.Ground(20f) + 5f, 1.94f, .18f, White);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(4f), 4f), -1);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(24f), 24f), -1);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MidStation()
        {
            // The new Midland station: the red shared path runs under the raking prow of the raised concourse (rust-red
            // fins over dark glass, white columns), brick planters with grass trees, the dark footbridge over the tracks,
            // trains at the platforms; the bus interchange and its painted canopies on the left.
            var d = new CqChunkDraft("Midland_Station", MidDrop);
            SharedPath(d);
            RailBed(d, 0, 40, YardTracks, YardEnd, false, Color.Lerp(DryGrass, Ballast, .35f));
            Wires(d, 0, 40, T1, T2, T3);
            SideWall(d, 1, 0, 40, 30f);
            ComicMesh s = d.SlopedFrontage;
            Palisade(d.Road, FenceX, 34f, 40f, 1.8f);
            Platform(s, BedX0 + .1f, T1 - 1.55f, 3f, 40f, true, true, false);
            AddSheared(d, d.Landmark, m =>
            {
                Platform(m, T2 + 1.55f, T3 - 1.55f, 0f, 40f, false, false, false);
                IslandCanopy(m, 2f, 26f);
                ConcourseBridge(m, 29.5f);
                Train(m, T3, 8f, 2, true, false);
            });
            Concourse(s, 7f, 33f);
            Train(s, T2, -5f, 2, false, true);
            // the forecourt at the prow: brick planters stepping up, grass trees, the station totem
            AddSheared(d, d.Props, m =>
            {
                BrickPlanter(m, V(FenceX + 1.6f, K.Kerb, 2.4f), 4.2f, 3.2f, .55f);
                BrickPlanter(m, V(FenceX + 2.3f, K.Kerb + .55f, 2.8f), 2.6f, 2f, .5f);
                GrassTree(m, V(FenceX + 2.4f, K.Kerb + 1.05f, 2.6f), 1.1f);
                GrassTree(m, V(FenceX + .5f, K.Kerb + .55f, 1.6f), .8f);
                Shrub(m, V(FenceX + .6f, K.Kerb + .55f, 3.4f), .9f, K.Hex("8FA37A"));
                BikeHoopsArt(m, V(K.Edge + .5f, K.Kerb, 36f), 0f);
            });
            StationTotem(d.Props, d.Deep, V(K.Edge + .55f, d.Ground(5.2f), 5.2f));
            AddSheared(d, d.Deep, m => Hospital(m, V(ShopsX + 26f, 0f, 14f)));
            AddSheared(d, d.Deep, m => WorkshopsAcrossTheYard(m, 0f, 12f));
            // the bus interchange: apron, painted stands, two canopies, buses
            Ground(d, -1, 0, 40, 0f, 60f, K.Kerb + .015f, K.R.asphalt, ComicSurface.Asphalt);
            Ground(d, -1, 0, 40, 0f, 5.2f, K.Kerb + .03f, Paving, ComicSurface.Paving);
            for (float z = 3f; z < 38f; z += 11.5f)
            {
                Stripe(d.Road, -(K.Edge + 9.2f), -(K.Edge + 5.2f), z, z + .14f, K.Kerb + .022f, Yellow);
                Stripe(d.Road, -(K.Edge + 9.2f), -(K.Edge + 9.06f), z, z + 11.5f, K.Kerb + .022f, Yellow);
            }
            BusCanopy(d.Frontage, V(-(K.Edge + 2.9f), d.Ground(10f), 10f), 13f, 1);
            BusCanopy(d.Frontage, V(-(K.Edge + 2.9f), d.Ground(29f), 29f), 13f, 2);
            Bus(d.Frontage, V(-(K.Edge + 7.3f), d.Ground(11f), 11f), 180f);
            Bus(d.Frontage, V(-(K.Edge + 7.3f), d.Ground(30f), 30f), 180f);
            BusCanopy(d.Frontage, V(-(K.Edge + 15.5f), d.Ground(22f), 22f), 13f, 4);
            Bus(d.Frontage, V(-(K.Edge + 20f), d.Ground(21f), 21f), 180f);
            Bus(d.Deep, V(-(K.Edge + 26f), d.Ground(8f), 8f), 78f);
            Kiosk(d.Frontage, V(-(K.Edge + 3f), d.Ground(19.5f), 19.5f));
            foreach (float z in new[] { 3f, 37f }) StreetLamp(d.Props, V(-(K.Edge + 11.5f), d.Ground(z), z), -1);
            for (float z = 3f; z < 38f; z += 11.5f) Stripe(d.Road, -(K.Edge + 22f), -(K.Edge + 18f), z, z + .14f, K.Kerb + .022f, Yellow);
            Gum(d.Props, V(-(K.Edge + 12f), d.Ground(34f), 34f), 11f, 3461);
            d.Walls.Add(new Bounds(V(-(K.Edge + 10f), d.Y(20f) + 2f, 20f), V(18f, 6f, 40f)));
            PathLamp(d.Props, V(K.Edge - .4f, d.Ground(20f), 20f));
            TownBehind(d, 0, 40, 346, 30f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MidTerminus()
        {
            // The end of the line: the platforms run out, the last portal and the buffer stops with their red lamps, a train
            // stabled against them; the five-storey station car park on the left.
            var d = new CqChunkDraft("Midland_Terminus", MidDrop);
            MidStreet(d);
            ComicMesh s = d.SlopedFrontage;
            RailBed(d, 0, 26f, YardTracks, YardEnd, true, Color.Lerp(DryGrass, Ballast, .35f));
            RailBed(d, 26f, 40f, new[] { T3, T4 }, YardEnd, true, Color.Lerp(DryGrass, Ballast, .35f));
            Wires(d, 0, 22f, T1, T2);
            Wires(d, 0, 40f, T3);
            SideWall(d, 1, 0, 40, 30f);
            Platform(s, BedX0 + .1f, T1 - 1.55f, 0f, 9f, true, false, true);
            AddSheared(d, d.Landmark, m => Platform(m, T2 + 1.55f, T3 - 1.55f, 0f, 11f, false, false, true));
            Gantry(s, 8f, BedX0 + .5f, T3 + 2.2f, T1, T2, T3);
            Mast(s, 22f, BedX0 - .2f, T1);
            s.Surface = ComicSurface.PaintedMetal;
            s.Beam(V(BedX0 - .2f, BedY + 6.6f, 22f), V(BedX0 - .2f, BedY + .2f, 27f), .06f, Charcoal);       // the wire termination guy
            BufferStop(s, T1, 23.5f);
            BufferStop(s, T2, 23.5f);
            Train(s, T2, 2.6f, 1, false, true);
            AddSheared(d, d.Props, m =>
            {
                RelayHut(m, V(T1 + 2.4f, BedY, 31f));
                Signal(m, V(T3 - 2.2f, BedY, 30f));
            });
            Gum(d.Deep, V(K.Edge + 40f, d.Ground(30f) - .3f, 30f), 15f, 361, true);
            AddSheared(d, d.Deep, m => Hospital(m, V(ShopsX + 20f, 0f, -26f)));
            // the station car park
            TownGround(d, 0, 40, 3f);
            PutLeft(d, d.Frontage, CarPark(30f), 4f, 30f, 3f, 18f, 16f);
            Gum(d.Props, V(-(K.Edge + 1.4f), d.Ground(2f), 2f), 10f, 362);
            Gum(d.Props, V(-(K.Edge + 1.5f), d.Ground(37f), 37f), 11f, 363);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(14f), 14f), -1);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(34f), 34f), -1);
            TownBehind(d, 0, 40, 347, 30f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MidWorkshops()
        {
            // The Midland Railway Workshops: a brick-paved lane with the old rails still in it runs between two of the
            // long sawtooth blocks. Halfway along is the traverser pit: a steel loading ramp, then 6 m of pit across the
            // whole width (WHOOPS!). Past the left block the square opens: the elevated water tank, the chimney, the
            // white hospital behind.
            var d = new CqChunkDraft("Midland_Workshops", MidDrop);
            d.Type = RoadType.PedestrianMall;
            float ramp = PitRampStart, lip = ramp + PitRise + PitTop, far = lip + PitGap;
            K.StreetSurface(d.Road, d.Length, BrickPaving, Paving, KerbCap, false, lip, far);
            ComicMesh m = d.Road;
            for (float z = 0f; z < 40f - .01f; z += 4f)
            {
                if (z + 4f > lip && z < far) continue;
                foreach (int sg in new[] { -1, 1 }) Stripe(m, sg * .72f - .06f, sg * .72f + .06f, z, z + 4f, .02f, RailSteel);
                Stripe(m, -K.Half + .1f, -K.Half + .22f, z, z + 4f, .02f, Color.Lerp(BrickPaving, Ink, .3f));
                Stripe(m, K.Half - .22f, K.Half - .1f, z, z + 4f, .02f, Color.Lerp(BrickPaving, Ink, .3f));
            }
            foreach (int sd in new[] { -1, 1 })
            {
                Ground(d, sd, 0, lip, 0f, 2.6f, K.Kerb + .02f, Paving, ComicSurface.Paving);
                Ground(d, sd, far, 40, 0f, 2.6f, K.Kerb + .02f, Paving, ComicSurface.Paving);
            }
            // the traverser pit: floor, the transverse rails, concrete walls with a yellow edge, the traverser parked at one end
            const float pitY = -1.7f, pw = K.Edge + 2.6f;
            m.Surface = ComicSurface.Concrete;
            K.Face(m, V(-pw, pitY, lip), V(-pw, pitY, far), V(pw, pitY, far), V(pw, pitY, lip), ConcreteDark, Vector3.up, false);
            K.Face(m, V(-pw, pitY, far), V(pw, pitY, far), V(pw, K.Kerb, far), V(-pw, K.Kerb, far), Concrete, Vector3.back, false);
            K.Face(m, V(-pw, pitY, lip), V(pw, pitY, lip), V(pw, K.Kerb, lip), V(-pw, K.Kerb, lip), Concrete, Vector3.forward, false);
            for (float x = -pw; x < pw - .01f; x += 1f)
                K.Face(m, V(x, K.Kerb - .28f, far - .01f), V(x + 1f, K.Kerb - .28f, far - .01f), V(x + 1f, K.Kerb - .02f, far - .01f), V(x, K.Kerb - .02f, far - .01f), Mathf.RoundToInt(x + 50f) % 2 == 0 ? Yellow : Ink, Vector3.back, false);
            foreach (float z in new[] { lip + 1.6f, far - 1.6f })
            {
                m.Surface = ComicSurface.Metal;
                K.MinMaxBox(m, V(-pw, pitY, z - .06f), V(pw, pitY + .16f, z + .06f), RailSteel);
            }
            ComicMesh fr = d.SlopedFrontage;
            fr.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(fr, V(-pw + .2f, pitY + .2f, lip + .5f), V(-K.Edge - .1f, pitY + .7f, far - .5f), SteelDark);
            K.MinMaxBox(fr, V(-pw + .2f, pitY + .7f, lip + .4f), V(-K.Edge - .1f, pitY + .85f, far - .4f), Yellow);
            var spec = new CqRampSpec { Name = "LoadingRamp", X = 0f, Z = ramp, Width = 2f * K.Edge, Rise = PitRise, Height = PitHeight, Top = PitTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            d.Gaps.Add(new CqGapSpec { Name = "TraverserPit", Z = lip, Length = PitGap, Width = 0f, Word = "WHOOPS", Water = false });
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, Steel, SteelDark, true, pitY);
            // yellow chevrons painted up the slope, so it reads as a ramp from the rider's low view
            deck.Surface = ComicSurface.Plain;
            float SlopeY(float z) => PitHeight * (z - ramp) / PitRise + .015f;
            for (int k = 0; k < 3; k++)
            {
                float zc = ramp + .5f + k * 1.7f;
                foreach (int sd in new[] { -1, 1 })
                    K.Face(deck, V(0f, SlopeY(zc + 1.2f), zc + 1.2f), V(0f, SlopeY(zc + 1.75f), zc + 1.75f), V(sd * 2.6f, SlopeY(zc + .55f), zc + .55f), V(sd * 2.6f, SlopeY(zc), zc), Yellow, Vector3.up, false);
            }
            deck.Surface = ComicSurface.PaintedMetal;
            foreach (int sd in new[] { -1, 1 })
            {
                float x = sd * (K.Edge + .1f);
                deck.Beam(V(x, 1.05f, ramp), V(x, PitHeight + 1.05f, ramp + PitRise), .1f, Yellow);
                deck.Beam(V(x, PitHeight + 1.05f, ramp + PitRise), V(x, PitHeight + 1.05f, lip - .1f), .1f, Yellow);
                for (float z = ramp; z <= lip; z += 3f)
                {
                    float h = PitHeight * Mathf.Clamp01((z - ramp) / PitRise);
                    K.MinMaxBox(deck, V(x - .06f, 0f, z - .06f), V(x + .06f, h + 1.1f, z + .06f), Yellow);
                }
            }
            fr.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            d.KeepOut.Add(KeepOutBox(d, -1, ramp, far)); d.KeepOut.Add(KeepOutBox(d, 1, ramp, far));
            // the blocks: the right one runs the whole chunk, the left one stops before the square
            Fr right = WorkshopBlock(40f, 9, true, "1");
            Put(d.Frontage, right, V(K.Edge + 2.6f, d.Base(20f), 40f), 90f);
            d.Walls.Add(new Bounds(V(K.Edge + 2.6f + 9f, d.Y(20f) + 4f, 20f), V(18f, 12f, 40f)));
            PutLeft(d, d.Landmark, WorkshopBlock(26.4f, 6, false, "2"), -.4f, 26.4f, 2.6f, 18f, 9f);
            // the square: lawn, the water tank, the chimney, the hospital behind
            Ground(d, -1, 26f, 40f, 2.6f, 60f, K.Kerb + .02f, Lawn, ComicSurface.Foliage);
            Ground(d, -1, 0f, 26f, 2.6f, 60f, K.Kerb + .015f, K.Hex("9C978C"), ComicSurface.Asphalt);
            Ground(d, 1, 0f, 40f, 2.6f, 60f, K.Kerb + .015f, K.Hex("9C978C"), ComicSurface.Asphalt);
            WaterTank(d.Props, V(-(K.Edge + 13f), d.Ground(34f), 34f));
            Wheelset(d.Props, V(-(K.Edge + 5.5f), d.Ground(32.5f), 32.5f));
            foreach (int sd in new[] { -1, 1 })
                for (float z = 4.4f; z < (sd < 0 ? 25f : 39f); z += 8.8f)
                {
                    if (z > lip - 1f && z < far + 1f) continue;
                    WallLamp(d.Props, V(sd * (K.Edge + 2.2f), d.Ground(z) + 4.6f, z), sd);
                }
            Chimney(d.Deep, V(-(K.Edge + 30f), d.Ground(36f) - .5f, 38f), 34f);
            Hospital(d.Deep, V(-(K.Edge + 56f), d.Ground(30f) - .5f, 30f), 1);
            Gum(d.Props, V(-(K.Edge + 5f), d.Ground(29f), 29f), 9f, 371);
            Bench(d.Props, V(-(K.Edge + 4f), d.Ground(36f), 36f), -90f);
            d.GiftSlots.Add(new Vector2(0f, 31f));
            d.Slot(-2.6f, 36f); d.Slot(2.6f, 36f);
            d.Slot(-2.6f, 2f); d.Slot(2.6f, 2f);
            return d;
        }

        static CqChunkDraft MidEntry()
        {
            // Calm at z = 0 (brick wall, hedge, gums), then the yard opens on the right and the first shops on the left.
            var d = new CqChunkDraft("Midland_Entry", MidDrop);
            MidStreet(d);
            RailBed(d, 0, 40, TwoTracks, BedX1, true, Color.Lerp(DryGrass, Ballast, .35f));
            RailBed(d, 14f, 40f, new[] { T3, T4 }, YardEnd, false);
            Wires(d, 0, 40, T1, T2);
            Gantry(d.SlopedFrontage, 20f, BedX0 + .5f, BedX1 - .3f, T1, T2);
            SideWall(d, 1, 0, 40, 30f);
            HedgeRow(d.SlopedFrontage, 1, 0f, 13f, .12f, .8f, 1.15f);
            AddSheared(d, d.Deep, m => WorkshopsAcrossTheYard(m, 20f, 40f));
            AddSheared(d, d.Deep, m => Wagons(m, T4, 16f, 2, 1));
            Gum(d.Deep, V(K.Edge + 30f, d.Ground(6f) - .3f, 6f), 15f, 381, true);
            Gum(d.Deep, V(K.Edge + 24f, d.Ground(13f) - .3f, 13f), 13f, 382, true, BarkRed);
            CalmEdge(d, -1, 0f, 14f, 3811);
            Ground(d, -1, 0, 14, 0f, 60f, K.Kerb + .02f, Lawn, ComicSurface.Foliage);
            TownGround(d, 14f, 40f, 3.4f);
            PutLeft(d, d.Frontage, FedShop(12f, K.Hex("D8C7A2"), RedOxide, 2, true), 15f, 12f, 3.4f, 12f, 9f);
            PutLeft(d, d.Frontage, FedShop(12f, Brick, K.Hex("2F4E6E"), 0, false), 27f, 12f, 3.4f, 12f, 9f);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(26f), 26f), -1);
            TownBehind(d, 14, 40, 348);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MidExit()
        {
            // Out of the Workshops: the end of a block on the right, the timekeeper's office and young gums on the left,
            // then calm towards z = 40 (brick wall, hedge, trees).
            var d = new CqChunkDraft("Midland_Exit", MidDrop);
            MidStreet(d);
            Ground(d, -1, 0, 40, 0f, 60f, K.Kerb + .02f, Lawn, ComicSurface.Foliage);
            Ground(d, 1, 0, 40, 0f, 60f, K.Kerb + .02f, Lawn, ComicSurface.Foliage);
            Ground(d, 1, 0, 14, 0f, 2.6f, K.Kerb + .03f, Paving, ComicSurface.Paving);
            Put(d.Frontage, WorkshopBlock(13.2f, 3, true, "3"), V(K.Edge + 2.6f, d.Base(6f), 13.2f), 90f);
            d.Walls.Add(new Bounds(V(K.Edge + 2.6f + 9f, d.Y(7f) + 4f, 6.6f), V(18f, 12f, 13.2f)));
            PutLeft(d, d.Frontage, Cottage(8f, Brick, IronRoof, false, 5, true), 4f, 8f, 5f, 9f, 5f);
            LowBrickWall(d, -1, 0f, 26f, .5f);
            LowBrickWall(d, 1, 14f, 26f, .5f);
            Gum(d.Props, V(-(K.Edge + 3.5f), d.Ground(17f), 17f), 11f, 391);
            Gum(d.Props, V(K.Edge + 5f, d.Ground(19f), 19f), 12f, 392, false, BarkRed);
            Gum(d.Deep, V(-(K.Edge + 20f), d.Ground(10f) - .3f, 10f), 16f, 393, true);
            Gum(d.Deep, V(K.Edge + 24f, d.Ground(22f) - .3f, 22f), 15f, 394, true);
            CalmEdge(d, -1, 26f, 40f, 3911);
            CalmEdge(d, 1, 26f, 40f, 3912);
            Gum(d.Props, V(K.Edge + 5f, d.Ground(33f), 33f), 10f, 395);
            SideWall(d, 1, 14f, 26f);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(22f), 22f), -1);
            d.StandardSlots();
            return d;
        }

        // ---------------------------------------------------------------- Midland buildings

        /// <summary>A two-storey Federation shop on Great Eastern Hwy: shopfronts between piers under a posted verandah
        /// (corrugated lean-to roof, a deep fascia with plain sign panels, slim posts with brackets), a brick or
        /// rendered upper floor with framed sash windows (style 1: round-headed), a string course, a cornice and a
        /// parapet with a pediment (0 triangular, 1 stepped, 2 curved) and ball finials; a tin roof and a chimney
        /// behind. end: the side the rider sees is a bare wall with a faded painted panel and two small windows.</summary>
        static Fr FedShop(float W, Color wall, Color verandah, int style, bool end)
        {
            var f = new Fr();
            const float D = 12f, g = 3.9f, top = 7.5f, vz = -3.2f;
            bool brick = wall == Brick || wall == BrickDark;
            Color pier = Color.Lerp(wall, Ink, .16f), trim = brick ? Stone : Color.Lerp(wall, Color.white, .5f);
            f.S = ComicSurface.Concrete;
            f.Box(0, W, -2.4f, .15f, 0, D, Ink);
            f.S = brick ? ComicSurface.Brick : ComicSurface.Plaster;
            f.Box(0, W, .15f, top, 0, D, wall);
            // ground floor: piers, two shopfronts and a door set between them, a tiled stall riser
            foreach (float x in new[] { 0f, W * .5f - 1.1f, W * .5f + .7f, W - .4f }) f.Box(x, x + .4f, .15f, g - .3f, -.14f, 0, pier);
            f.Shopfront(.5f, W * .5f - 1.2f, .2f, 3.2f, 0f);
            f.Shopfront(W * .5f + 1.2f, W - .5f, .2f, 3.2f, 0f);
            f.Door(W * .5f, .15f, 3.05f, 1.3f, 0f, style == 1 ? K.Hex("2F5E4E") : K.Hex("6B2F28"));
            f.S = ComicSurface.Plain;
            f.Q(.52f, W * .5f - 1.22f, .22f, .6f, -.045f, style == 2 ? K.Hex("2F5E4E") : K.Hex("7A2E26"), false);
            f.Q(W * .5f + 1.22f, W - .52f, .22f, .6f, -.045f, style == 2 ? K.Hex("2F5E4E") : K.Hex("7A2E26"), false);
            // the verandah over the forecourt
            f.S = ComicSurface.Metal;
            int stripes = Mathf.RoundToInt(W / 1.3f);
            for (int i = 0; i < stripes; i++)
                f.LeanTo(-.1f + (W + .2f) * i / stripes, -.1f + (W + .2f) * (i + 1) / stripes, vz - .1f, 0f, 3.45f, 4.15f, .08f, style == 1 && i % 2 == 1 ? Cream : verandah, Color.Lerp(verandah, Ink, .45f));
            f.S = ComicSurface.PaintedMetal;
            f.Box(-.12f, W + .12f, 3.05f, 3.5f, vz - .16f, vz - .04f, Color.Lerp(verandah, Ink, .25f));
            f.Q(.4f, W * .5f - .5f, 3.12f, 3.43f, vz - .17f, Cream);
            f.Q(W * .5f + .5f, W - .4f, 3.12f, 3.43f, vz - .17f, style == 0 ? K.Hex("E2A23A") : Cream);
            int posts = 4;
            for (int i = 0; i < posts; i++)
            {
                float x = Mathf.Lerp(.12f, W - .12f, (float)i / (posts - 1));
                f.Box(x - .06f, x + .06f, 0f, 3.05f, vz - .12f, vz, Ink);
                f.Box(x - .1f, x + .1f, 0f, .5f, vz - .16f, vz + .04f, Ink);
                foreach (int s in new[] { -1, 1 })
                {
                    if ((i == 0 && s < 0) || (i == posts - 1 && s > 0)) continue;
                    K.TriFacing(f.M, V(x + s * .06f, 3.05f, vz - .06f), V(x + s * .6f, 3.05f, vz - .06f), V(x + s * .06f, 2.55f, vz - .06f), Ink, Vector3.back);
                }
            }
            // a sign hanging under the verandah, facing along the footpath
            f.Box(W * .5f - .04f, W * .5f + .04f, 2.45f, 2.95f, vz + .5f, vz + 1.9f, style == 2 ? K.Hex("E2A23A") : K.Hex("2A9D9A"));
            // upper floor
            f.S = ComicSurface.Concrete;
            f.Box(-.06f, W + .06f, g, g + .22f, -.12f, D + .06f, trim);
            foreach (float x in new[] { 0f, W - .45f }) f.Box(x, x + .45f, g + .22f, top, -.1f, 0f, pier);
            int n = W > 10f ? 3 : 2;
            for (int i = 0; i < n; i++)
            {
                float x = W * (i + .5f) / n;
                if (style == 1) f.ArchWin(x, g + 1f, top - 1.5f, 1.2f, 0f, trim);
                else f.Win(x, g + 1f, top - .9f, 1.25f, 0f, trim, true, true);
            }
            // cornice, parapet, pediment, finials
            f.S = ComicSurface.Concrete;
            f.Box(-.2f, W + .2f, top, top + .3f, -.32f, D + .1f, trim);
            f.Box(-.12f, W + .12f, top - .18f, top, -.2f, D + .06f, Color.Lerp(trim, Ink, .15f));
            f.S = brick ? ComicSurface.Brick : ComicSurface.Plaster;
            f.Box(0, W, top + .3f, top + 1.2f, -.08f, .4f, wall);
            f.S = ComicSurface.Concrete;
            f.Box(-.08f, W + .08f, top + 1.2f, top + 1.36f, -.16f, .48f, trim);
            float pc = W * .5f, pw = W * .26f;
            if (style == 0)
            {
                K.TriFacing(f.M, V(pc - pw, top + 1.36f, -.1f), V(pc + pw, top + 1.36f, -.1f), V(pc, top + 2.7f, -.1f), wall, Vector3.back);
                f.M.Beam(V(pc - pw - .1f, top + 1.36f, -.14f), V(pc, top + 2.78f, -.14f), .16f, trim);
                f.M.Beam(V(pc + pw + .1f, top + 1.36f, -.14f), V(pc, top + 2.78f, -.14f), .16f, trim);
                K.TriFacing(f.M, V(pc - pw, top + 1.36f, .4f), V(pc + pw, top + 1.36f, .4f), V(pc, top + 2.7f, .4f), wall, Vector3.forward);
            }
            else if (style == 1)
            {
                f.S = ComicSurface.Plaster;
                f.Box(pc - pw, pc + pw, top + 1.36f, top + 2f, -.08f, .4f, wall);
                f.Box(pc - pw * .55f, pc + pw * .55f, top + 2f, top + 2.5f, -.08f, .4f, wall);
                f.S = ComicSurface.Concrete;
                f.Box(pc - pw - .08f, pc + pw + .08f, top + 2f, top + 2.12f, -.16f, .48f, trim);
                f.Box(pc - pw * .55f - .08f, pc + pw * .55f + .08f, top + 2.5f, top + 2.64f, -.16f, .48f, trim);
            }
            else
            {
                f.S = brick ? ComicSurface.Brick : ComicSurface.Plaster;
                for (int i = 0; i < 4; i++)
                {
                    float a0 = Mathf.PI * i / 4f, a1 = Mathf.PI * (i + 1) / 4f;
                    K.TriFacing(f.M, V(pc, top + 1.36f, -.1f), V(pc + Mathf.Cos(a0) * pw, top + 1.36f + Mathf.Sin(a0) * pw * .7f, -.1f), V(pc + Mathf.Cos(a1) * pw, top + 1.36f + Mathf.Sin(a1) * pw * .7f, -.1f), wall, Vector3.back);
                    f.M.Beam(V(pc + Mathf.Cos(a0) * (pw + .06f), top + 1.36f + Mathf.Sin(a0) * (pw * .7f + .06f), -.14f), V(pc + Mathf.Cos(a1) * (pw + .06f), top + 1.36f + Mathf.Sin(a1) * (pw * .7f + .06f), -.14f), .14f, trim);
                }
            }
            f.S = ComicSurface.PaintedMetal;
            f.Q(pc - pw * .5f, pc + pw * .5f, top + .5f, top + 1.05f, -.1f, trim);
            f.S = ComicSurface.Concrete;
            foreach (float x in new[] { .2f, W - .2f })
            {
                f.Box(x - .22f, x + .22f, top + 1.36f, top + 1.7f, -.14f, .3f, trim);
                Blob(f.M, V(x, top + 1.95f, .08f), V(.5f, .5f, .5f), trim, 6, 3);
            }
            // the tin roof and a chimney behind the parapet
            f.S = ComicSurface.Metal;
            f.RoofX(0, W, top + .3f, top + 1.9f, .5f, D, IronRoof);
            f.S = ComicSurface.Brick;
            f.Box(W * .75f, W * .75f + .8f, top + .8f, top + 3.1f, D * .55f, D * .55f + .8f, brick ? wall : Brick);
            f.S = ComicSurface.Concrete;
            f.Box(W * .75f - .08f, W * .75f + .88f, top + 3.1f, top + 3.28f, D * .55f - .08f, D * .55f + .88f, Stone);
            if (!end) return f;
            f.S = ComicSurface.Plain;
            f.EndQ(-.02f, 1.5f, D - 2f, 4.3f, 6.6f, Color.Lerp(wall, Cream, .7f), true);
            f.EndQ(-.03f, 2.2f, D - 2.7f, 5.1f, 5.8f, Color.Lerp(wall, K.Hex("7A2E26"), .6f), true, false);
            foreach (float z in new[] { 3f, 7.5f })
            {
                f.S = ComicSurface.PaintedMetal;
                f.EndQ(-.02f, z, z + 1f, 1.4f, 2.8f, Ink, true);
                f.S = ComicSurface.Glass;
                f.EndQ(-.035f, z + .09f, z + .91f, 1.49f, 2.71f, Glass, true, false);
            }
            return f;
        }

        /// <summary>
        /// Midland Town Hall (1906; the memorial clock 1923), after the Commons photographs of April 2026. The corner
        /// tower stands at the frame's left end, towards the rider: a stone podium and a ring of columns round the
        /// entrance, a balustraded balcony, the round red-brick drum with round-headed windows between stone bands, a
        /// cornice, the ribbed grey dome and on it the square cream clock tower with four faces, a pyramid cap and a
        /// gold finial. The wings (26 m along the road, 12 m along the side street) are red brick with stone base,
        /// strings and cornice: square-headed windows below, paired round-headed ones above between pilasters, a
        /// balustraded parapet, a pedimented centre with the flagpole and the arched main door.
        /// </summary>
        static Fr TownHall()
        {
            var f = new Fr();
            const float W = 26f, D = 13f, g0 = .5f, f1 = 4.4f, eave = 8.3f, par = 9.5f;
            // ---- the wing along the road
            f.S = ComicSurface.Concrete;
            f.Box(-.1f, W + .1f, -2.6f, g0 + .5f, -.12f, D, StoneShade);
            f.S = ComicSurface.Brick;
            f.Box(0, W, g0 + .5f, eave, 0, D, Brick);
            f.S = ComicSurface.Concrete;
            f.Box(-.08f, W + .08f, f1 - .15f, f1 + .2f, -.14f, D + .05f, Stone);
            f.Box(-.08f, W + .08f, eave - 1.15f, eave - .95f, -.1f, D + .05f, StoneShade);
            f.Box(-.25f, W + .25f, eave, eave + .4f, -.4f, D + .2f, Stone);
            f.Box(-.12f, W + .12f, eave - .22f, eave, -.22f, D + .1f, StoneShade);
            // bays: pilasters, a square-headed window below, a pair of round-headed windows above
            const float bx0 = 3.4f;
            int bays = 5;
            float bw = (W - bx0) / bays;
            for (int i = 0; i <= bays; i++)
            {
                float x = Mathf.Min(W - .3f, bx0 + i * bw);
                f.S = ComicSurface.Brick;
                f.Box(x - .3f, x + .3f, g0 + .5f, eave - .22f, -.16f, 0f, BrickDark);
                f.S = ComicSurface.Concrete;
                f.Box(x - .36f, x + .36f, f1 - .15f, f1 + .2f, -.22f, 0f, Stone);
            }
            for (int i = 0; i < bays; i++)
            {
                float x = bx0 + (i + .5f) * bw;
                if (i == 2) continue;                                      // the centre bay is the entrance
                f.Win(x, g0 + 1.2f, f1 - .7f, 1.7f, 0f, Stone, true, true, 3);
                f.ArchWin(x - .62f, f1 + .9f, eave - 1.75f, .95f, 0f, Stone, true, 1);
                f.ArchWin(x + .62f, f1 + .9f, eave - 1.75f, .95f, 0f, Stone, true, 1);
            }
            // the entrance bay: standing forward, the arched door, a tall arched window, the pediment and the flagpole
            float ec = bx0 + 2.5f * bw;
            f.S = ComicSurface.Brick;
            f.Box(ec - bw * .5f + .3f, ec + bw * .5f - .3f, g0 + .5f, eave + .4f, -.4f, 0f, Brick);
            f.S = ComicSurface.Concrete;
            f.Box(ec - bw * .5f + .2f, ec + bw * .5f - .2f, -2.6f, g0 + .5f, -.5f, 0f, StoneShade);
            f.Box(ec - bw * .5f + .15f, ec + bw * .5f - .15f, eave + .4f, eave + .75f, -.6f, 0f, Stone);
            f.Box(ec - 1.5f, ec + 1.5f, g0 + .5f, g0 + 3.9f, -.52f, -.4f, Stone);
            f.ArchWin(ec, g0 + .1f, g0 + 2.5f, 1.9f, -.52f, Stone, false, 2, GlassDark);
            f.ArchWin(ec, f1 + .8f, eave - 1.6f, 1.7f, -.4f, Stone, true, 2);
            K.TriFacing(f.M, V(ec - bw * .5f + .15f, eave + .75f, -.55f), V(ec + bw * .5f - .15f, eave + .75f, -.55f), V(ec, eave + 2.3f, -.55f), Stone, Vector3.back);
            K.TriFacing(f.M, V(ec - bw * .5f + .7f, eave + .9f, -.57f), V(ec + bw * .5f - .7f, eave + .9f, -.57f), V(ec, eave + 1.95f, -.57f), Brick, Vector3.back);
            f.S = ComicSurface.PaintedMetal;
            f.Cyl(ec, eave + 2.2f, eave + 6f, -.3f, .06f, White, 6);
            f.S = ComicSurface.Fabric;
            f.Box(ec + .06f, ec + 1.4f, eave + 5.1f, eave + 5.9f, -.32f, -.28f, K.Hex("2A3E6E"));
            // the balustraded parapet either side of the pediment
            Balustrade(f, bx0 - .3f, ec - bw * .5f + .15f, eave + .4f, par, -.2f);
            Balustrade(f, ec + bw * .5f - .15f, W, eave + .4f, par, -.2f);
            // a shop awning on the last bay (the tea rooms), plain stripes
            f.S = ComicSurface.Fabric;
            for (int i = 0; i < 4; i++) f.LeanTo(bx0 + 4f * bw + .3f + i * (bw - .6f) / 4f, bx0 + 4f * bw + .3f + (i + 1) * (bw - .6f) / 4f, -1.6f, 0f, 3f, 3.7f, .06f, i % 2 == 0 ? K.Hex("2F5E4E") : Cream, K.Hex("1F3E34"));
            // ---- the side-street wing: the end the rider sees
            f.S = ComicSurface.Concrete;
            for (int k = 0; k < 2; k++)
            {
                float z = 4.6f + k * 4.2f;
                f.S = ComicSurface.Brick;
                f.Box(-.16f, 0f, g0 + .5f, eave - .22f, z - 2.4f, z - 1.8f, BrickDark);
                f.S = ComicSurface.PaintedMetal;
                f.EndQ(-.02f, z - .85f, z + .85f, g0 + 1.2f, f1 - .7f, Ink, true);
                f.S = ComicSurface.Glass;
                f.EndQ(-.035f, z - .76f, z + .76f, g0 + 1.29f, f1 - .79f, Glass, true, false);
                f.S = ComicSurface.Concrete;
                f.Box(-.18f, 0f, g0 + 1.04f, g0 + 1.2f, z - .97f, z + .97f, Stone);
                f.Box(-.14f, 0f, f1 - .7f, f1 - .5f, z - .97f, z + .97f, Stone);
                foreach (float dz in new[] { -.62f, .62f })
                {
                    f.S = ComicSurface.PaintedMetal;
                    f.EndQ(-.02f, z + dz - .47f, z + dz + .47f, f1 + .9f, eave - 1.4f, Ink, true);
                    f.S = ComicSurface.Glass;
                    f.EndQ(-.035f, z + dz - .38f, z + dz + .38f, f1 + .99f, eave - 1.49f, Glass, true, false);
                    f.S = ComicSurface.Concrete;
                    f.Box(-.16f, 0f, eave - 1.4f, eave - 1.2f, z + dz - .55f, z + dz + .55f, Stone);
                }
            }
            f.S = ComicSurface.Brick;
            f.Box(-.16f, 0f, g0 + .5f, eave - .22f, D - .6f, D, BrickDark);
            f.S = ComicSurface.Concrete;
            for (float z = 3.2f; z < D; z += .5f) f.Box(-.3f, -.16f, eave + .5f, par - .14f, z - .08f, z + .08f, Stone);
            f.Box(-.36f, -.1f, par - .14f, par, 2.6f, D, Stone);
            f.Box(-.36f, -.1f, eave + .4f, eave + .5f, 2.6f, D, Stone);
            // ---- the roofs behind the parapet
            f.S = ComicSurface.Metal;
            f.Hip(.4f, W - .4f, .8f, D - .4f, eave + .4f, eave + 2.6f, IronRoofDark);
            // ---- the corner tower
            Vector3 c = V(.9f, 0f, .9f);
            ComicMesh m = f.M;
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(c + V(0, g0 * .5f - 1f, 0), 4.5f, g0 + 2f, StoneShade, 12);
            m.Cylinder(c + V(0, g0 + .12f, 0), 4.1f, .24f, Stone, 12);
            m.Surface = ComicSurface.Brick;
            m.Cylinder(c + V(0, (g0 + f1) * .5f, 0), 2.9f, f1 - g0, Brick, 12);
            m.Surface = ComicSurface.Concrete;
            for (int i = 0; i < 8; i++)
            {
                float a = (140f + i * 23f) * Mathf.Deg2Rad;
                Vector3 p = c + V(Mathf.Cos(a) * 3.75f, 0, Mathf.Sin(a) * 3.75f);
                m.Cylinder(p + V(0, g0 + .5f, 0), .3f, .5f, StoneShade, 6);
                m.Cylinder(p + V(0, (g0 + .75f + f1 - .3f) * .5f, 0), .22f, f1 - .3f - g0 - .75f, Stone, 8, .85f);
                m.Cylinder(p + V(0, f1 - .2f, 0), .3f, .2f, StoneShade, 6);
            }
            m.Cylinder(c + V(0, f1 + .15f, 0), 4.15f, .5f, Stone, 12);
            m.Cylinder(c + V(0, f1 + .48f, 0), 4.3f, .16f, StoneShade, 12);
            // the balcony balustrade
            for (int i = 0; i < 22; i++)
            {
                float a = (128f + i * 9.2f) * Mathf.Deg2Rad;
                m.Cylinder(c + V(Mathf.Cos(a) * 4.05f, f1 + .98f, Mathf.Sin(a) * 4.05f), .09f, .85f, Stone, 4);
            }
            m.Cylinder(c + V(0, f1 + 1.47f, 0), 4.15f, .14f, Stone, 12);
            // the drum: brick between stone bands, round-headed windows on the faces you see
            m.Surface = ComicSurface.Brick;
            m.Cylinder(c + V(0, (f1 + eave) * .5f + .3f, 0), 3.05f, eave - f1 + .6f, Brick, 12);
            m.Surface = ComicSurface.Concrete;
            foreach (float y in new[] { f1 + 1.75f, eave - 1.05f }) m.Cylinder(c + V(0, y, 0), 3.13f, .2f, Stone, 12);
            for (int i = 0; i < 5; i++) DrumWindow(m, c, 3.05f, 165f + i * 30f, f1 + 2.2f, eave - 1.5f, .95f, Stone);
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(c + V(0, eave + .8f, 0), 3.5f, .4f, Stone, 12);
            m.Cylinder(c + V(0, eave + .5f, 0), 3.25f, .22f, StoneShade, 12);
            m.Cylinder(c + V(0, eave + 1.2f, 0), 3.1f, .4f, Stone, 12);
            // the dome (grey, faceted like its ribs) and the square clock tower on it
            float dy = eave + 1.4f;
            m.Surface = ComicSurface.Metal;
            m.Cylinder(c + V(0, dy + .5f, 0), 2.95f, 1f, Zinc, 12, .9f);
            m.Cylinder(c + V(0, dy + 1.45f, 0), 2.65f, .9f, Color.Lerp(Zinc, White, .12f), 12, .74f);
            m.Cylinder(c + V(0, dy + 2.2f, 0), 1.96f, .6f, Zinc, 12, .66f);
            float cy = dy + 2.5f;
            m.Surface = ComicSurface.Plaster;
            m.Box(c + V(0, cy + .25f, 0), V(2.5f, .5f, 2.5f), StoneShade);
            m.Box(c + V(0, cy + 1.6f, 0), V(2.2f, 2.2f, 2.2f), Cream);
            m.Surface = ComicSurface.Concrete;
            m.Box(c + V(0, cy + 2.8f, 0), V(2.6f, .24f, 2.6f), Stone);
            foreach (Vector3 n in new[] { Vector3.back, Vector3.left, Vector3.forward, Vector3.right }) ClockFace(m, c + V(0, cy + 1.65f, 0) + n * 1.11f, n, .82f);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(c + V(0, cy + 3.5f, 0), 1.75f, 1.2f, K.Hex("7F8F8A"), 4, .08f, Quaternion.Euler(0, 45, 0));
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(c + V(0, cy + 4.4f, 0), .05f, .8f, Ochre, 5);
            Blob(m, c + V(0, cy + 4.25f, 0), V(.34f, .34f, .34f), Ochre, 6, 3);
            return f;
        }

        /// <summary>A stone balustrade on the parapet from x0 to x1: base, balusters, rail, a pier at each end.</summary>
        static void Balustrade(Fr f, float x0, float x1, float y0, float y1, float z)
        {
            f.S = ComicSurface.Concrete;
            f.Box(x0, x1, y0, y0 + .12f, z - .08f, z + .24f, Stone);
            f.Box(x0, x1, y1 - .14f, y1, z - .08f, z + .24f, Stone);
            for (float x = x0 + .3f; x < x1 - .2f; x += .5f) f.Box(x - .08f, x + .08f, y0 + .12f, y1 - .14f, z, z + .16f, Stone);
            foreach (float x in new[] { x0 + .2f, x1 - .2f }) f.Box(x - .22f, x + .22f, y0, y1 + .18f, z - .12f, z + .3f, StoneShade);
        }

        /// <summary>A round-headed window on a face of a 12-sided drum (centre c, radius r), looking out at angle deg.</summary>
        static void DrumWindow(ComicMesh m, Vector3 c, float r, float deg, float y0, float y1, float w, Color trim)
        {
            float a = deg * Mathf.Deg2Rad;
            Vector3 n = V(Mathf.Cos(a), 0, Mathf.Sin(a)), t = V(-Mathf.Sin(a), 0, Mathf.Cos(a)), o = c + n * (r * .966f + .02f), u = Vector3.up;
            m.Surface = ComicSurface.PaintedMetal;
            K.Face(m, o - t * w * .5f + u * y0, o + t * w * .5f + u * y0, o + t * w * .5f + u * y1, o - t * w * .5f + u * y1, Ink, n);
            K.TriFacing(m, o - t * w * .5f + u * y1, o + t * w * .5f + u * y1, o + u * (y1 + w * .5f), Ink, n);
            m.Surface = ComicSurface.Glass;
            Vector3 g = o + n * .015f;
            float gw = w * .5f - .09f;
            K.Face(m, g - t * gw + u * (y0 + .09f), g + t * gw + u * (y0 + .09f), g + t * gw + u * y1, g - t * gw + u * y1, Glass, n, false);
            K.TriFacing(m, g - t * gw + u * y1, g + t * gw + u * y1, g + u * (y1 + gw), Glass, n);
            m.Surface = ComicSurface.Concrete;
            Quaternion q = Quaternion.LookRotation(n);
            m.Box(o + u * (y0 - .08f) + n * .06f, V(w + .24f, .16f, .2f), trim, q);
            m.Box(o + u * (y1 + w * .5f + .12f) + n * .05f, V(.26f, .3f, .16f), trim, q);
        }

        /// <summary>A clock face on a wall facing n: ink rim, cream dial, two ink hands, four hour marks.</summary>
        static void ClockFace(ComicMesh m, Vector3 c, Vector3 n, float r)
        {
            Vector3 t = Vector3.Cross(Vector3.up, n).normalized, u = Vector3.up;
            for (int i = 0; i < 8; i++)
            {
                float a0 = Mathf.PI * 2f * i / 8f, a1 = Mathf.PI * 2f * (i + 1) / 8f;
                Vector3 p0 = t * Mathf.Cos(a0) + u * Mathf.Sin(a0), p1 = t * Mathf.Cos(a1) + u * Mathf.Sin(a1);
                m.Surface = ComicSurface.PaintedMetal;
                K.TriFacing(m, c, c + p0 * (r + .12f), c + p1 * (r + .12f), Ink, n);
                m.Surface = ComicSurface.Plain;
                K.TriFacing(m, c + n * .02f, c + n * .02f + p0 * r, c + n * .02f + p1 * r, White, n);
            }
            Vector3 h = c + n * .04f;
            K.Face(m, h - t * .05f, h + t * .05f, h + t * .05f + u * r * .7f, h - t * .05f + u * r * .7f, Ink, n, false);
            K.Face(m, h - u * .05f, h + u * .05f, h + u * .05f + t * r * .5f, h - u * .05f + t * r * .5f, Ink, n, false);
            foreach (Vector3 dir in new[] { t, -t, u, -u }) K.Face(m, h + dir * r * .78f - Vector3.Cross(dir, n) * .05f, h + dir * r * .78f + Vector3.Cross(dir, n) * .05f, h + dir * r * .95f + Vector3.Cross(dir, n) * .05f, h + dir * r * .95f - Vector3.Cross(dir, n) * .05f, Ink, n, false);
        }

        static void Flagpole(ComicMesh m, Vector3 p, float h)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, .15f, 0), V(.8f, .3f, .8f), Stone);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, h * .5f, 0), .06f, h, White, 6);
            Blob(m, p + V(0, h + .1f, 0), V(.22f, .22f, .22f), Ochre, 5, 3);
            m.Surface = ComicSurface.Fabric;
            m.Box(p + V(0, h - .65f, .75f), V(.04f, .9f, 1.5f), K.Hex("2A3E6E"));
            m.Box(p + V(0, h - .45f, .4f), V(.05f, .45f, .7f), K.Hex("C8372D"));
        }

        /// <summary>
        /// Midland Centrepoint: a long single-storey shopping centre of pale panels with a teal band, a walkway canopy on
        /// posts over shopfront glazing, and the entrance portal standing forward and taller: a red frame round dark
        /// glass doors with CENTREPOINT in white letters across its head; rooftop plant and the glazed ridge of the mall
        /// behind. The end the rider sees has a loading door (the name is lettered above it by the chunk). No brands.
        /// </summary>
        static Fr Centrepoint(float W)
        {
            var f = new Fr();
            const float D = 20f, h = 6.2f;
            Color panel = K.Hex("E6E0D2"), band = K.Hex("1F8C8C"), red = K.Hex("C8372D");
            f.S = ComicSurface.Concrete;
            f.Box(0, W, -2.6f, .15f, 0, D, Ink);
            f.S = ComicSurface.Plaster;
            f.Box(0, W, .15f, h, 0, D, panel);
            f.S = ComicSurface.PaintedMetal;
            f.Box(-.06f, W + .06f, h - 1.1f, h - .5f, -.06f, D + .06f, band);
            f.Box(-.1f, W + .1f, h, h + .2f, -.1f, D + .1f, K.Hex("F4F1E8"));
            f.S = ComicSurface.Plain;
            for (float x = 3f; x < W - 1f; x += 3f) f.Q(x - .03f, x + .03f, 3.7f, h - 1.1f, -.012f, Color.Lerp(panel, Ink, .25f), false);
            float e0 = W * .5f - 4.5f, e1 = W * .5f + 4.5f;
            // shopfronts under the walkway canopy
            for (float x = 1f; x < W - 4f; x += 4.2f)
            {
                if (x + 3.6f > e0 && x < e1) continue;
                f.Shopfront(x, x + 3.6f, .2f, 3.1f, 0f);
                f.S = ComicSurface.PaintedMetal;
                f.Q(x + .3f, x + 3.3f, 3.2f, 3.62f, -.03f, new[] { K.Hex("E2A23A"), K.Hex("2F6FB0"), K.Hex("7A9A4A"), K.Hex("B0567A") }[Mathf.RoundToInt(x) % 4]);
            }
            f.S = ComicSurface.Metal;
            f.Box(-.3f, e0, 3.75f, 3.95f, -3f, 0f, K.Hex("F4F1E8"));
            f.Box(e1, W + .3f, 3.75f, 3.95f, -3f, 0f, K.Hex("F4F1E8"));
            f.S = ComicSurface.PaintedMetal;
            f.Box(-.32f, e0, 3.6f, 3.76f, -3.04f, -2.9f, band);
            f.Box(e1, W + .32f, 3.6f, 3.76f, -3.04f, -2.9f, band);
            for (float x = .2f; x < W; x += 4.2f)
            {
                if (x > e0 - .3f && x < e1 + .3f) continue;
                f.Box(x - .07f, x + .07f, .15f, 3.6f, -2.95f, -2.81f, SteelDark);
            }
            // the entrance portal
            f.S = ComicSurface.Plaster;
            f.Box(e0, e1, .15f, 9.2f, -3.6f, 2f, K.Hex("F4F1E8"));
            f.S = ComicSurface.PaintedMetal;
            f.Box(e0 - .25f, e0 + .7f, .15f, 9.4f, -3.9f, -3.5f, red);
            f.Box(e1 - .7f, e1 + .25f, .15f, 9.4f, -3.9f, -3.5f, red);
            f.Box(e0 - .25f, e1 + .25f, 6.4f, 9.4f, -3.9f, -3.5f, red);
            f.Box(e0 - .35f, e1 + .35f, 9.4f, 9.6f, -4f, 2.1f, Ink);
            f.S = ComicSurface.Glass;
            f.Q(e0 + .7f, e1 - .7f, .2f, 6.4f, -3.62f, GlassDark);
            f.S = ComicSurface.PaintedMetal;
            for (int i = 1; i < 6; i++) { float x = Mathf.Lerp(e0 + .7f, e1 - .7f, i / 6f); f.Box(x - .05f, x + .05f, .2f, 6.4f, -3.7f, -3.62f, i == 3 ? Ink : SteelDark); }
            f.Box(e0 + .7f, e1 - .7f, 3.1f, 3.22f, -3.7f, -3.62f, SteelDark);
            f.Box(e0 + .3f, e1 - .3f, 3.5f, 3.7f, -5.4f, -3.9f, K.Hex("F4F1E8"));
            f.Box(e0 + .3f, e1 - .3f, 3.36f, 3.5f, -5.44f, -5.3f, red);
            PixelText(f.M, "CENTREPOINT", W * .5f, 7.9f, -3.93f, .17f, White);
            // roof: plant, the glazed ridge of the mall
            f.S = ComicSurface.Metal;
            f.Box(4f, 8f, h + .2f, h + 1.6f, 6f, 10f, SteelDark);
            f.Box(W - 9f, W - 5f, h + .2f, h + 1.4f, 9f, 13f, Steel);
            f.S = ComicSurface.Glass;
            f.RoofX(e0 - 3f, e1 + 3f, h + .2f, h + 2f, 3f, 9f, GlassLight);
            // the end the rider sees: a roller door, a dock canopy, bollards
            f.S = ComicSurface.PaintedMetal;
            f.EndQ(-.02f, 11f, 15.5f, .2f, 4.2f, Ink, true);
            f.S = ComicSurface.Metal;
            f.EndQ(-.035f, 11.15f, 15.35f, .25f, 4.05f, Steel, true, false);
            for (float y = .7f; y < 4f; y += .55f) f.EndQ(-.04f, 11.15f, 15.35f, y, y + .05f, SteelDark, true, false);
            f.Box(-1.6f, 0f, 4.4f, 4.6f, 10.5f, 16f, band);
            f.S = ComicSurface.Plain;
            f.EndQ(-.02f, 1f, 9.6f, 4.1f, 5.9f, red, true);
            return f;
        }

        /// <summary>A trolley bay: steel hoops and a row of nested trolleys.</summary>
        static void TrolleyBay(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.75f, .75f })
            {
                m.Box(p + V(x, 1.1f, 0), V(.08f, .08f, 5f), SteelDark);
                foreach (float z in new[] { -2.4f, 0f, 2.4f }) m.Box(p + V(x, .55f, z), V(.08f, 1.1f, .08f), SteelDark);
            }
            m.Box(p + V(0, 1.9f, -2.4f), V(1.7f, .5f, .08f), K.Hex("1F8C8C"));
            m.Box(p + V(-.75f, 1.5f, -2.4f), V(.08f, .9f, .08f), SteelDark);
            m.Box(p + V(.75f, 1.5f, -2.4f), V(.08f, .9f, .08f), SteelDark);
            ComicMesh t = TrolleyMesh();
            for (int i = 0; i < 4; i++) m.Add(t, p + V(0, 0, -1.4f + i * .45f), Quaternion.identity, Vector3.one);
        }

        /// <summary>The centre's pylon sign by the car park entrance, facing the rider: a slab on two legs, a red head
        /// with CENTREPOINT in white, blank coloured tenant panels below (no brands).</summary>
        static void PylonSign(ComicMesh m, ComicMesh text, Vector3 p)
        {
            const float px = .075f;
            float w = PixelWidth("CENTREPOINT", px) + .6f;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -w * .32f, w * .32f }) m.Box(p + V(x, 1.5f, .04f), V(.3f, 3f, .3f), K.Hex("F4F1E8"));
            m.Box(p + V(0, 5.2f, 0), V(w, 4.6f, .36f), K.Hex("F4F1E8"));
            m.Box(p + V(0, 7.05f, -.02f), V(w + .16f, 1.1f, .42f), K.Hex("C8372D"));
            m.Box(p + V(0, 7.68f, 0), V(w + .3f, .14f, .5f), Ink);
            Color[] cols = { K.Hex("E2A23A"), K.Hex("2F6FB0"), K.Hex("7A9A4A"), K.Hex("B0567A"), K.Hex("1F8C8C") };
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 5; i++) m.Box(p + V(0, 6.05f - i * .62f, -.19f), V(w - .4f, .46f, .02f), cols[i]);
            PixelText(text, "CENTREPOINT", p.x, p.y + 7.05f, p.z - .24f, px, White);
        }

        /// <summary>The five-storey station car park: concrete decks with deep spandrels and dark openings between piers,
        /// a screen of rust-red fins over half the front (like the station's), the stair and lift tower at the end
        /// the rider sees (glass strip, charcoal cap, a blue P panel), a vehicle entry with a striped height bar.</summary>
        static Fr CarPark(float W)
        {
            var f = new Fr();
            const float D = 18f, lv = 3f;
            int levels = 5;
            float top = levels * lv;
            f.S = ComicSurface.Concrete;
            f.Box(0, W, -2.6f, .1f, 0, D, Ink);
            f.S = ComicSurface.Plain;
            f.Box(.2f, W - .2f, .1f, top, .25f, D - .2f, K.Hex("3A3F45"));
            for (int k = 0; k <= levels; k++)
            {
                float y = k * lv;
                f.S = ComicSurface.Concrete;
                f.Box(-.1f, W + .1f, y + (k == 0 ? .1f : -.2f), y + 1.15f, -.12f, D + .1f, k == levels ? K.Hex("E3DFD4") : Concrete);
                f.S = ComicSurface.Plain;
                if (k < levels) f.Q(.4f, W - .4f, y + 1.05f, y + 1.15f, -.13f, ConcreteDark, false);
            }
            f.S = ComicSurface.Concrete;
            for (float x = 4.4f; x < W; x += 6.4f) f.Box(x - .3f, x + .3f, .1f, top, -.06f, .5f, ConcreteDark);
            // the fin screen (levels 1 to 4) over the far half
            f.S = ComicSurface.PaintedMetal;
            for (float x = W * .46f; x < W - .3f; x += .85f)
            {
                int i = Mathf.RoundToInt(x * 7f);
                f.Box(x - .05f, x + .05f, lv + .9f, top + (i % 3) * .35f - .2f, -.6f, -.14f, i % 2 == 0 ? Rust : RustLight);
            }
            f.Box(W * .46f - .2f, W, lv + .8f, lv + .92f, -.62f, -.12f, Charcoal);
            // the stair and lift tower
            f.S = ComicSurface.Concrete;
            f.Box(-.3f, 4.2f, .1f, top + 2.6f, -1.6f, 4f, K.Hex("E3DFD4"));
            f.S = ComicSurface.Glass;
            f.Q(.8f, 3.2f, .6f, top + 1.8f, -1.62f, GlassLight);
            f.EndQ(-.32f, -.9f, 2.6f, .6f, top + 1.8f, GlassLight, true);
            f.S = ComicSurface.PaintedMetal;
            for (int k = 1; k <= levels; k++)
            {
                f.Q(.8f, 3.2f, k * lv - .08f, k * lv + .08f, -1.64f, Ink, false);
                f.EndQ(-.34f, -.9f, 2.6f, k * lv - .08f, k * lv + .08f, Ink, true, false);
            }
            f.Box(-.5f, 4.4f, top + 2.6f, top + 2.85f, -1.8f, 4.2f, Charcoal);
            f.S = ComicSurface.Plain;
            f.EndQ(-.33f, .2f, 1.8f, top - .2f, top + 1.5f, K.Hex("2F6FB0"), true);
            PixelTextFacing(f.M, "P", V(-.36f, top + .65f, 1f), 90f, .22f, White);
            // the vehicle entry: a dark opening, the height bar, a boom
            f.S = ComicSurface.Plain;
            f.Q(6f, 12f, .1f, 2.7f, -.14f, K.Hex("23272C"));
            f.S = ComicSurface.PaintedMetal;
            for (int i = 0; i < 8; i++) f.Box(6f + i * .75f, 6.75f + i * .75f, 2.3f, 2.55f, -.5f, -.4f, i % 2 == 0 ? Yellow : Ink);
            f.Box(8.9f, 9.1f, .1f, 1.2f, -1f, -.8f, Yellow);
            f.Box(9.1f, 11.8f, 1f, 1.1f, -.95f, -.85f, K.Hex("C8372D"));
            // the end the rider sees beyond the tower: the decks return
            for (int k = 1; k <= levels; k++)
            {
                f.S = ComicSurface.Plain;
                f.EndQ(-.11f, 4f, D, k * lv - .2f, k * lv + 1.15f, k == levels ? K.Hex("E3DFD4") : Concrete, true);
            }
            f.S = ComicSurface.Metal;
            f.Box(W * .6f, W * .6f + 3f, top + 1.15f, top + 3.2f, 6f, 9f, K.Hex("E3DFD4"));
            return f;
        }

        // ---------------------------------------------------------------- the new Midland station (flat road space)

        /// <summary>
        /// The raised concourse of the new Midland station (METRONET, opened February 2026), after the Commons photographs
        /// of April 2026: a long box above the platform beside the path, on white round columns, wrapped in rust-red
        /// vertical fins of uneven height over dark glazing between charcoal bands; at the end the rider meets, the box
        /// rakes forward into a sharp prow. Below it the glazed entrance hall and the platform canopy fascia.
        /// </summary>
        static void Concourse(ComicMesh m, float z0, float z1)
        {
            const float xa = FenceX - .1f, xb = T1 + 2.35f, y0 = BedY + 5.3f, y1 = BedY + 9.4f, rake = 3.2f;
            // the box
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, V(xa, y0, z0), V(xb, y1, z1), Charcoal);
            m.Surface = ComicSurface.Metal;
            K.MinMaxBox(m, V(xa - .15f, y0 - .22f, z0 - .1f), V(xb + .15f, y0, z1 + .1f), White);
            K.MinMaxBox(m, V(xa - .2f, y1, z0 - rake - .2f), V(xb + .2f, y1 + .25f, z1 + .2f), Charcoal);
            // the prow: the roof runs forward past the floor, the end wall rakes back down to it
            Vector3 a0 = V(xa, y0, z0), a1 = V(xb, y0, z0), b0 = V(xa, y1, z0 - rake), b1 = V(xb, y1, z0 - rake), c0 = V(xa, y1, z0), c1 = V(xb, y1, z0);
            m.Surface = ComicSurface.PaintedMetal;
            K.Face(m, a0, a1, b1, b0, RustDark, V(0, -1, -1));
            K.TriFacing(m, a0, b0, c0, Charcoal, Vector3.left);
            K.TriFacing(m, a1, b1, c1, Charcoal, Vector3.right);
            for (int i = 0; i < 7; i++)
            {
                float x = Mathf.Lerp(xa + .3f, xb - .3f, i / 6f);
                m.Beam(V(x, y0 + .05f, z0 - .08f), V(x, y1 - .05f, z0 - rake - .08f), .16f, i % 2 == 0 ? Rust : RustLight);
            }
            // glazing behind the fins on the road side
            m.Surface = ComicSurface.Glass;
            K.Face(m, V(xa - .02f, y0 + .9f, z0 + .4f), V(xa - .02f, y0 + .9f, z1 - .4f), V(xa - .02f, y1 - .7f, z1 - .4f), V(xa - .02f, y1 - .7f, z0 + .4f), GlassDark, Vector3.left);
            // the fins: rust-red blades of uneven height, some with a lighter top
            m.Surface = ComicSurface.PaintedMetal;
            int n = Mathf.RoundToInt((z1 - z0) / .8f);
            for (int i = 0; i <= n; i++)
            {
                float z = Mathf.Lerp(z0 - .4f, z1 - .2f, (float)i / n);
                int k = (i * 7 + 3) % 5;
                float up = k == 0 ? 1.1f : k == 2 ? .6f : .15f, lowY = y0 + .25f - (z < z0 ? (z0 - z) * .0f : 0f);
                K.MinMaxBox(m, V(xa - .55f, lowY, z - .05f), V(xa - .05f, y1 + up, z + .05f), i % 3 == 1 ? RustLight : Rust);
                if (k == 0) K.MinMaxBox(m, V(xa - .56f, y1 + up - .5f, z - .06f), V(xa - .04f, y1 + up, z + .06f), K.Hex("E38E6E"));
            }
            K.MinMaxBox(m, V(xa - .6f, y0 + .1f, z0 - .4f), V(xa, y0 + .28f, z1), Charcoal);
            // the columns: pairs on the platform and between the tracks
            m.Surface = ComicSurface.Concrete;
            for (float z = z0 + 2.5f; z < z1; z += 6f)
            {
                m.Cylinder(V(xa + 1.3f, (BedY + 1f + y0 - .22f) * .5f, z), .32f, y0 - .22f - BedY - 1f, White, 8);
                m.Cylinder(V(xb - .35f, (BedY + y0 - .22f) * .5f, z), .3f, y0 - .22f - BedY, White, 8);
            }
            // under it, on the platform: the glazed entrance hall with its ink frame, the lift, ticket gates
            float py = BedY + 1f;
            m.Surface = ComicSurface.Glass;
            K.MinMaxBox(m, V(xa + .5f, py, z0 + 5f), V(xa + 2.6f, py + 3f, z0 + 12f), GlassLight);
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, V(xa + .42f, py + 3f, z0 + 4.9f), V(xa + 2.68f, py + 3.2f, z0 + 12.1f), Ink);
            foreach (float z in new[] { z0 + 5f, z0 + 7.3f, z0 + 9.7f, z0 + 12f }) K.MinMaxBox(m, V(xa + .44f, py, z - .06f), V(xa + .54f, py + 3f, z + .06f), Ink);
            K.MinMaxBox(m, V(xa + .7f, py, z0 + 17f), V(xa + 2.3f, y0 - .2f, z0 + 19f), K.Hex("5FA8C4"));
            K.MinMaxBox(m, V(xa + .6f, py, z0 + 16.9f), V(xa + .72f, y0 - .2f, z0 + 19.1f), Ink);
            for (float z = z0 + 13.2f; z < z0 + 16f; z += .9f) K.MinMaxBox(m, V(xa + 1f, py, z), V(xa + 2f, py + 1f, z + .3f), SteelDark);
            // the fence between the path and the platform, in front of the hall: black palisade with a brick base
            m.Surface = ComicSurface.Brick;
            K.MinMaxBox(m, V(xa - .15f, K.Kerb - .3f, z0 - 1f), V(xa + .15f, K.Kerb + .55f, z1 + 1f), Brick);
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(xa - .2f, K.Kerb + .55f, z0 - 1f), V(xa + .2f, K.Kerb + .65f, z1 + 1f), Stone);
            Palisade(m, xa, z0 - 1f, z1 + 1f, 1.3f, K.Kerb + .65f);
        }

        /// <summary>The station's dark footbridge over the tracks at z (it leaves the concourse and lands beyond the
        /// yard): a charcoal box with a glass band and sparse rust fins on white piers, a lift tower on the island
        /// platform and one at the far end.</summary>
        static void ConcourseBridge(ComicMesh m, float z)
        {
            const float xa = T1 + 2.3f, xb = YardEnd + 4f, y0 = BedY + 5.5f, y1 = BedY + 8.9f;
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, V(xa, y0, z - 1.8f), V(xb, y1, z + 1.8f), Charcoal);
            K.MinMaxBox(m, V(xa, y1, z - 2f), V(xb + .2f, y1 + .2f, z + 2f), Ink);
            m.Surface = ComicSurface.Glass;
            K.Face(m, V(xa + .5f, y0 + 1f, z - 1.82f), V(xb - .5f, y0 + 1f, z - 1.82f), V(xb - .5f, y1 - .8f, z - 1.82f), V(xa + .5f, y1 - .8f, z - 1.82f), GlassDark, Vector3.back);
            m.Surface = ComicSurface.PaintedMetal;
            for (float x = xa + 1f; x < xb - .5f; x += 1.7f) K.MinMaxBox(m, V(x - .05f, y0 + .2f, z - 2.2f), V(x + .05f, y1 + (Mathf.RoundToInt(x) % 2) * .5f, z - 1.85f), Mathf.RoundToInt(x) % 3 == 0 ? RustLight : Rust);
            m.Surface = ComicSurface.Concrete;
            foreach (float x in new[] { T2 + 3.4f, T3 + 2.2f, xb - 1f }) m.Cylinder(V(x, (BedY + y0) * .5f, z), .35f, y0 - BedY, White, 8);
            m.Surface = ComicSurface.Glass;
            K.MinMaxBox(m, V(T2 + 3.9f, BedY + 1f, z - 4.4f), V(T2 + 5.9f, y1 + 1f, z - 2f), K.Hex("5FA8C4"));
            K.MinMaxBox(m, V(xb - .2f, BedY, z - 1.6f), V(xb + 2.4f, y1 + 1.2f, z + 1.6f), K.Hex("5FA8C4"));
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, V(T2 + 3.8f, y1 + 1f, z - 4.5f), V(T2 + 6f, y1 + 1.2f, z - 1.9f), Ink);
            K.MinMaxBox(m, V(xb - .3f, y1 + 1.2f, z - 1.7f), V(xb + 2.5f, y1 + 1.4f, z + 1.7f), Ink);
        }

        /// <summary>The island platform's canopy: a flat white roof with a grey fascia on a line of Y-posts, name boards.</summary>
        static void IslandCanopy(ComicMesh m, float z0, float z1)
        {
            float x = (T2 + T3) * .5f, y = BedY + 4.6f;
            m.Surface = ComicSurface.Metal;
            K.MinMaxBox(m, V(x - 2.3f, y, z0), V(x + 2.3f, y + .18f, z1), White);
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, V(x - 2.4f, y - .16f, z0 - .1f), V(x + 2.4f, y, z1 + .1f), SteelDark);
            for (float z = z0 + 2f; z < z1; z += 6f)
            {
                m.Box(V(x, BedY + 2.6f, z), V(.2f, 3.2f, .2f), SteelDark);
                m.Beam(V(x, BedY + 3.6f, z), V(x - 1.8f, y - .16f, z), .12f, SteelDark);
                m.Beam(V(x, BedY + 3.6f, z), V(x + 1.8f, y - .16f, z), .12f, SteelDark);
                m.Box(V(x, BedY + 1.5f, z + 2.6f), V(.5f, .08f, 1.8f), Steel);
            }
        }

        /// <summary>The station totem beside the path, facing the rider: a charcoal slab with a green head, MIDLAND in
        /// white letters, a white train symbol (a plain rounded box).</summary>
        static void StationTotem(ComicMesh m, ComicMesh text, Vector3 p)
        {
            const float px = .09f;
            float w = PixelWidth("MIDLAND", px) + .5f;
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 2.3f, 0), V(w, 4.6f, .3f), Charcoal);
            m.Box(p + V(0, 4.25f, -.02f), V(w + .06f, .7f, .34f), TrainGreen);
            m.Box(p + V(0, .1f, 0), V(w + .2f, .2f, .5f), Ink);
            m.Surface = ComicSurface.Plain;
            m.Box(p + V(0, 2.2f, -.16f), V(.8f, .7f, .02f), White);
            m.Box(p + V(0, 2.3f, -.175f), V(.6f, .26f, .02f), Charcoal);
            PixelText(text, "MIDLAND", p.x, p.y + 3.35f, p.z - .17f, px, White);
        }

        /// <summary>A terraced red-brick planter (the station forecourt): a brick box with a soldier-course cap, mulch.</summary>
        static void BrickPlanter(ComicMesh m, Vector3 p, float w, float l, float h)
        {
            m.Surface = ComicSurface.Brick;
            m.Box(p + V(0, h * .5f - .2f, 0), V(w, h + .4f, l), Brick);
            m.Box(p + V(0, h - .04f, 0), V(w + .12f, .12f, l + .12f), BrickLight);
            m.Surface = ComicSurface.Foliage;
            m.Box(p + V(0, h + .03f, 0), V(w - .5f, .04f, l - .5f), Mulch);
        }

        /// <summary>The interchange's customer service kiosk: a glazed box with an ink frame under a floating white roof.</summary>
        static void Kiosk(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, .1f, 0), V(3.4f, .2f, 4.4f), Concrete);
            m.Surface = ComicSurface.Glass;
            m.Box(p + V(0, 1.55f, 0), V(2.8f, 2.7f, 3.8f), GlassLight);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, .45f, 0), V(2.86f, .5f, 3.86f), TrainGreen);
            foreach (float z in new[] { -1.9f, 0f, 1.9f }) foreach (float x in new[] { -1.4f, 1.4f }) m.Box(p + V(x, 1.55f, z), V(.1f, 2.7f, .1f), Ink);
            m.Box(p + V(0, 2.95f, 0), V(3f, .12f, 4f), Ink);
            m.Surface = ComicSurface.Metal;
            m.Box(p + V(0, 3.3f, 0), V(4.2f, .16f, 5.2f), White);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { -2.3f, 2.3f }) m.Box(p + V(1.8f, 1.6f, z), V(.12f, 3.2f, .12f), SteelDark);
        }

        static void BikeHoopsArt(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + q * V(0, .04f, 0), V(2.1f, .08f, .2f), Ink, q);
            foreach (float x in new[] { -.7f, 0f, .7f })
            {
                m.Box(p + q * V(x - .25f, .46f, 0), V(.07f, .86f, .07f), Rust, q);
                m.Box(p + q * V(x + .25f, .46f, 0), V(.07f, .86f, .07f), Rust, q);
                m.Box(p + q * V(x, .9f, 0), V(.57f, .07f, .07f), Rust, q);
            }
        }

        /// <summary>A bus stand canopy (the interchange): slim posts, a flat roof with a charcoal fascia, a row of painted
        /// art panels hanging under its edge (the community artwork: bold shapes in ochre, teal, red and green), a
        /// glass back screen, a bench, a stand marker.</summary>
        static void BusCanopy(ComicMesh m, Vector3 p, float len, int seed)
        {
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = -len * .5f + .8f; z <= len * .5f; z += (len - 1.6f) / 2f) m.Box(p + V(1f, 1.75f, z), V(.16f, 3.5f, .16f), SteelDark);
            m.Surface = ComicSurface.Metal;
            m.Box(p + V(-.4f, 3.6f, 0), V(4.4f, .16f, len), White);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(-.4f, 3.47f, 0), V(4.5f, .12f, len + .1f), Charcoal);
            Color[] art = { Ochre, MuralTeal, K.Hex("C8553A"), MuralLight, K.Hex("5B6FB0"), Yellow };
            int n = Mathf.RoundToInt(len / 1.6f);
            for (int i = 0; i < n; i++)
            {
                float z = -len * .5f + (i + .5f) * len / n;
                Color c = art[(i + seed) % art.Length], c2 = art[(i + seed + 2) % art.Length];
                m.Surface = ComicSurface.Plain;
                m.Box(p + V(-2.55f, 3.05f, z), V(.05f, .7f, len / n - .12f), c);
                K.Face(m, p + V(-2.58f, 2.75f, z - .5f), p + V(-2.58f, 2.75f, z + .1f), p + V(-2.58f, 3.3f, z + .5f), p + V(-2.58f, 3.3f, z - .1f), c2, Vector3.left, false);
                K.Face(m, p + V(-2.52f, 2.75f, z - .5f), p + V(-2.52f, 2.75f, z + .1f), p + V(-2.52f, 3.3f, z + .5f), p + V(-2.52f, 3.3f, z - .1f), c2, Vector3.right, false);
            }
            m.Surface = ComicSurface.Glass;
            m.Box(p + V(1f, 1.5f, 0), V(.05f, 1.8f, len - 2.2f), GlassLight);
            m.Surface = ComicSurface.Metal;
            m.Box(p + V(.5f, .45f, 0), V(.5f, .08f, len * .5f), Steel);
            m.Box(p + V(.5f, .22f, -len * .2f), V(.4f, .44f, .08f), SteelDark);
            m.Box(p + V(.5f, .22f, len * .2f), V(.4f, .44f, .08f), SteelDark);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(-2.2f, 1.4f, -len * .5f + .4f), V(.1f, 2.8f, .1f), SteelDark);
            m.Box(p + V(-2.2f, 2.5f, -len * .5f + .4f), V(.5f, .6f, .12f), TrainGreen);
        }

        /// <summary>A Transperth bus (front at +Z at yaw 0): white body, a green skirt band, a dark window band, a big
        /// windscreen with an amber destination box, ink wheels, a roof pod.</summary>
        static void Bus(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            void B(Vector3 c, Vector3 size, Color col, ComicSurface s) { m.Surface = s; m.Box(p + q * c, size, col, q); }
            B(V(0, 1.75f, 0), V(2.5f, 2.7f, 11.6f), TrainWhite, ComicSurface.Metal);
            B(V(0, .78f, 0), V(2.54f, .6f, 11.64f), TrainGreen, ComicSurface.PaintedMetal);
            B(V(0, 2.1f, -.4f), V(2.56f, 1f, 10f), GlassDark, ComicSurface.Glass);
            B(V(0, 1.9f, 5.78f), V(2.2f, 1.5f, .1f), GlassDark, ComicSurface.Glass);
            B(V(0, 2.85f, 5.8f), V(1.6f, .34f, .1f), K.Hex("F2A62E"), ComicSurface.Plastic);
            B(V(0, 3.2f, -1f), V(2f, .3f, 4f), K.Hex("C9D0D6"), ComicSurface.Metal);
            B(V(0, .55f, 5.82f), V(2.4f, .3f, .1f), Ink, ComicSurface.PaintedMetal);
            foreach (float z in new[] { -3.4f, 3.6f }) B(V(0, .5f, z), V(2.6f, 1f, 1.1f), Ink, ComicSurface.Rubber);
            foreach (float z in new[] { 4.3f, -.2f }) B(V(-1.27f, 1.55f, z), V(.06f, 2.1f, 1.1f), SteelDark, ComicSurface.PaintedMetal);
        }

        /// <summary>A buffer stop at the end of the track at x: a steel frame, the red-and-white beam, two red lamps.</summary>
        static void BufferStop(ComicMesh m, float x, float z)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (int s in new[] { -1, 1 })
            {
                m.Beam(V(x + s * .72f, BedY + .1f, z - 2.4f), V(x + s * .72f, BedY + 1.45f, z), .16f, SteelDark);
                m.Box(V(x + s * .72f, BedY + .8f, z + .1f), V(.16f, 1.5f, .16f), SteelDark);
                m.Box(V(x + s * .72f, BedY + 1.2f, z - .22f), V(.36f, .36f, .26f), Charcoal);
            }
            for (int i = 0; i < 4; i++) m.Box(V(x - .9f + i * .6f, BedY + 1.6f, z), V(.6f, .34f, .2f), i % 2 == 0 ? K.Hex("D8382E") : White);
            m.Surface = ComicSurface.Plastic;
            foreach (int s in new[] { -1, 1 }) m.Box(V(x + s * .95f, BedY + 2f, z), V(.24f, .3f, .24f), K.Hex("E8322B"));
            m.Surface = ComicSurface.Concrete;
            m.Box(V(x, BedY + .15f, z + 1.4f), V(2.6f, .5f, 2f), ConcreteDark);
        }

        static void RelayHut(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, 1.3f, 0), V(2.6f, 2.6f, 3.6f), K.Hex("D9D4C8"));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 2.7f, 0), V(3f, .2f, 4f), SteelDark);
            m.Box(p + V(-1.32f, 1.1f, .4f), V(.06f, 2f, 1f), TrainGreen);
            m.Box(p + V(-1.33f, 1.9f, -1f), V(.06f, .5f, .8f), Yellow);
        }

        static void Signal(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 2.2f, 0), V(.14f, 4.4f, .14f), Steel);
            m.Box(p + V(0, 4.3f, -.05f), V(.5f, 1.3f, .3f), Charcoal);
            m.Surface = ComicSurface.Plastic;
            m.Box(p + V(0, 4.65f, -.21f), V(.22f, .22f, .04f), K.Hex("E8322B"));
            m.Box(p + V(0, 4.3f, -.21f), V(.22f, .22f, .04f), Color.Lerp(Yellow, Ink, .6f));
            m.Box(p + V(0, 3.95f, -.21f), V(.22f, .22f, .04f), Color.Lerp(K.Hex("3DBE5A"), Ink, .6f));
        }

        // ---------------------------------------------------------------- the Railway Workshops

        /// <summary>
        /// One of the Midland Railway Workshops blocks (1904), after the Commons photographs of August 2026: a long
        /// red-brick wall on a stone plinth, brick piers every bay, tall round-headed steel windows (a white roller door
        /// with a brick arch in some bays), stepped corbel courses and a stone coping, the sawtooth roof over it (each
        /// tooth's glazed face looks along the block). The end the rider sees is the parapet end wall with a stepped
        /// centre, recessed panels, two arched roller doors and the block's number. rightSide: the frame will stand on
        /// the right of the road (yaw 90), so its seen end is at x = W.
        /// </summary>
        static Fr WorkshopBlock(float W, int bays, bool rightSide, string number)
        {
            var f = new Fr();
            const float D = 18f, h = 7.6f;
            f.S = ComicSurface.Concrete;
            f.Box(-.08f, W + .08f, -2.6f, .9f, -.1f, D, StoneShade);
            f.S = ComicSurface.Brick;
            f.Box(0, W, .9f, h, 0, D, Brick);
            float bw = W / bays;
            for (int i = 0; i <= bays; i++)
            {
                float x = Mathf.Clamp(i * bw, .32f, W - .32f);
                f.S = ComicSurface.Brick;
                f.Box(x - .32f, x + .32f, .9f, h - .5f, -.28f, 0f, BrickDark);
                f.S = ComicSurface.Concrete;
                f.Box(x - .38f, x + .38f, .9f, 1.15f, -.34f, 0f, StoneShade);
            }
            for (int i = 0; i < bays; i++)
            {
                float x = (i + .5f) * bw;
                bool door = (i + (rightSide ? 1 : 0)) % 4 == 1;
                if (door)
                {
                    f.S = ComicSurface.PaintedMetal;
                    f.Q(x - 1.5f, x + 1.5f, .15f, 4.4f, -.02f, Ink);
                    f.S = ComicSurface.Metal;
                    f.Q(x - 1.38f, x + 1.38f, .2f, 4.3f, -.035f, White, false);
                    for (float y = .7f; y < 4.2f; y += .5f) f.Q(x - 1.38f, x + 1.38f, y, y + .04f, -.04f, K.Hex("C9CDD1"), false);
                    f.S = ComicSurface.Brick;
                    f.Box(x - 1.7f, x + 1.7f, 4.4f, 4.75f, -.1f, 0f, BrickLight);
                    f.S = ComicSurface.PaintedMetal;
                    f.Q(x - .3f, x + .3f, 5.1f, 5.7f, -.02f, White);
                }
                else f.ArchWin(x, 1.9f, 4.9f, 1.9f, 0f, StoneShade, true, 3, GlassDark);
            }
            // corbel courses and coping
            f.S = ComicSurface.Brick;
            f.Box(-.1f, W + .1f, h - .5f, h - .25f, -.36f, D + .1f, BrickLight);
            f.Box(-.16f, W + .16f, h - .25f, h, -.44f, D + .16f, BrickDark);
            f.S = ComicSurface.Concrete;
            f.Box(-.2f, W + .2f, h, h + .16f, -.5f, D + .2f, Stone);
            // the sawtooth roof: glazed faces look towards the rider
            for (int i = 0; i < bays; i++)
            {
                float a = i * bw, b = a + bw;
                float zg = rightSide ? b : a, zs = rightSide ? a : b;        // glazed edge / low edge along the frame's X
                Vector3 g0 = V(zg, h + .16f, .2f), g1 = V(zg, h + .16f, D - .2f), t0 = V(zg, h + 2.5f, .2f), t1 = V(zg, h + 2.5f, D - .2f), l0 = V(zs, h + .16f, .2f), l1 = V(zs, h + .16f, D - .2f);
                Vector3 look = rightSide ? Vector3.right : Vector3.left;
                f.S = ComicSurface.Glass;
                K.Face(f.M, g0, g1, t1, t0, GlassLight, look);
                f.S = ComicSurface.Metal;
                K.Face(f.M, t0, t1, l1, l0, IronRoofDark, Vector3.up - look * .5f);
                f.S = ComicSurface.Brick;
                K.TriFacing(f.M, g0, t0, l0, BrickDark, Vector3.back);
                f.S = ComicSurface.PaintedMetal;
                for (int k = 1; k < 4; k++)
                {
                    float z = Mathf.Lerp(.2f, D - .2f, k / 4f);
                    K.Face(f.M, V(zg, h + .2f, z - .05f) + look * .02f, V(zg, h + .2f, z + .05f) + look * .02f, V(zg, h + 2.45f, z + .05f) + look * .02f, V(zg, h + 2.45f, z - .05f) + look * .02f, Ink, look, false);
                }
            }
            // the end wall the rider sees
            float xe = rightSide ? W : 0f, o = rightSide ? .02f : -.02f;
            bool left = !rightSide;
            f.S = ComicSurface.Brick;
            f.Box(rightSide ? W : -.3f, rightSide ? W + .3f : 0f, h + .16f, h + 1.4f, 0f, D, Brick);
            f.Box(rightSide ? W : -.34f, rightSide ? W + .34f : 0f, h + 1.4f, h + 2.3f, D * .25f, D * .75f, Brick);
            f.S = ComicSurface.Concrete;
            f.Box(rightSide ? W - .06f : -.42f, rightSide ? W + .42f : .06f, h + 1.4f, h + 1.56f, -.1f, D + .1f, Stone);
            f.Box(rightSide ? W - .06f : -.46f, rightSide ? W + .46f : .06f, h + 2.3f, h + 2.46f, D * .25f - .1f, D * .75f + .1f, Stone);
            f.S = ComicSurface.Brick;
            foreach (float z in new[] { .3f, D * .5f, D - .3f }) f.Box(rightSide ? W : -.3f, rightSide ? W + .3f : 0f, .9f, h + 1.4f, z - .35f, z + .35f, BrickDark);
            foreach (float zc in new[] { D * .25f, D * .75f })
            {
                f.S = ComicSurface.PaintedMetal;
                f.EndQ(xe + o, zc - 1.7f, zc + 1.7f, .15f, 4.6f, Ink, left);
                f.S = ComicSurface.Metal;
                f.EndQ(xe + o * 1.8f, zc - 1.56f, zc + 1.56f, .2f, 4.48f, White, left, false);
                for (float y = .7f; y < 4.4f; y += .5f) f.EndQ(xe + o * 2.2f, zc - 1.56f, zc + 1.56f, y, y + .04f, K.Hex("C9CDD1"), left, false);
                f.S = ComicSurface.Brick;
                f.Box(rightSide ? W : -.12f, rightSide ? W + .12f : 0f, 4.6f, 5f, zc - 1.9f, zc + 1.9f, BrickLight);
                f.S = ComicSurface.Plain;
                f.EndQ(xe + o, zc - .45f, zc + .45f, 5.5f, 6.4f, White, left);
                PixelTextFacing(f.M, number, V(xe + o * 2.5f, 5.95f, zc), rightSide ? -90f : 90f, .11f, Ink);
            }
            return f;
        }

        /// <summary>A wall lamp on a pier of the Workshops: an ink bracket and a cream lantern with an ink cap.</summary>
        static void WallLamp(ComicMesh m, Vector3 p, int side)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Beam(p, p + V(-side * .9f, .35f, 0), .07f, Ink);
            m.Box(p + V(-side * .9f, .1f, 0), V(.34f, .08f, .34f), Ink);
            m.Surface = ComicSurface.Plastic;
            m.Box(p + V(-side * .9f, -.14f, 0), V(.26f, .4f, .26f), Cream);
        }

        /// <summary>A locomotive wheelset on a short length of rail, on show in the square.</summary>
        static void Wheelset(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, .1f, 0), V(2.6f, .2f, 2.2f), Concrete);
            m.Surface = ComicSurface.Metal;
            foreach (float x in new[] { -.75f, .75f })
            {
                m.Box(p + V(x, .28f, 0), V(.1f, .16f, 2.1f), RailSteel);
                m.Cylinder(p + V(x, .92f, 0), .58f, .14f, K.Hex("3C3F44"), 10, 1f, Quaternion.Euler(0, 0, 90));
                m.Cylinder(p + V(x + Mathf.Sign(x) * .09f, .92f, 0), .42f, .06f, K.Hex("8C4A36"), 10, 1f, Quaternion.Euler(0, 0, 90));
            }
            m.Cylinder(p + V(0, .92f, 0), .1f, 1.8f, K.Hex("3C3F44"), 6, 1f, Quaternion.Euler(0, 0, 90));
        }

        /// <summary>The Workshops' elevated water tank: a riveted dark steel drum with a shallow cone roof on a tall
        /// lattice tower (battered legs, ring girts, cross bracing), the riser pipe and a ladder.</summary>
        static void WaterTank(ComicMesh m, Vector3 p)
        {
            const float legTop = 15f, rBase = 3.3f, rTop = 2.5f;
            Color steel = K.Hex("5A6068"), dark = K.Hex("3C4148");
            m.Surface = ComicSurface.PaintedMetal;
            const int legs = 6;
            Vector3 L(int i, float t) { float a = i * Mathf.PI * 2f / legs; float r = Mathf.Lerp(rBase, rTop, t); return p + V(Mathf.Cos(a) * r, t * legTop, Mathf.Sin(a) * r); }
            for (int i = 0; i < legs; i++)
            {
                m.Beam(L(i, 0f), L(i, 1f), .2f, steel);
                m.Surface = ComicSurface.Concrete;
                m.Box(L(i, 0f) + V(0, .15f, 0), V(.6f, .3f, .6f), ConcreteDark);
                m.Surface = ComicSurface.PaintedMetal;
                for (int k = 0; k < 3; k++)
                {
                    float t0 = k / 3f, t1 = (k + 1) / 3f;
                    m.Beam(L(i, t1), L((i + 1) % legs, t1), .11f, steel);
                    m.Beam(L(i, t0), L((i + 1) % legs, t1), .07f, dark);
                    m.Beam(L((i + 1) % legs, t0), L(i, t1), .07f, dark);
                }
            }
            m.Cylinder(p + V(0, legTop * .5f, 0), .22f, legTop, dark, 6);
            m.Cylinder(p + V(0, legTop + .15f, 0), 3.35f, .3f, dark, 12);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(p + V(0, legTop + 2.1f, 0), 3.2f, 3.6f, steel, 12);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float y in new[] { 1.2f, 2.4f, 3.75f }) m.Cylinder(p + V(0, legTop + y, 0), 3.27f, .12f, dark, 12);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(p + V(0, legTop + 4.4f, 0), 3.35f, 1f, K.Hex("6E757D"), 12, .12f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, legTop + 5.1f, 0), .12f, .5f, dark, 5);
            m.Box(p + V(rTop + .5f, legTop * .5f + 2f, 0), V(.06f, legTop + 4f, .4f), dark);
        }

        /// <summary>The Workshops' chimney: a tall tapering round red-brick stack on a square plinth, dark steel bands,
        /// a corbelled cap.</summary>
        static void Chimney(ComicMesh m, Vector3 p, float h)
        {
            m.Surface = ComicSurface.Brick;
            m.Box(p + V(0, 2f, 0), V(4.4f, 6f, 4.4f), BrickDark);
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, 5.1f, 0), V(4.8f, .3f, 4.8f), StoneShade);
            m.Surface = ComicSurface.Brick;
            m.Cylinder(p + V(0, 5.2f + (h - 5.2f) * .5f, 0), 1.7f, h - 5.2f, Brick, 8, .6f);
            m.Surface = ComicSurface.PaintedMetal;
            for (float y = 8f; y < h - 2f; y += 3.2f) m.Cylinder(p + V(0, y, 0), Mathf.Lerp(1.7f, 1.02f, (y - 5.2f) / (h - 5.2f)) + .06f, .16f, K.Hex("4A3A34"), 8);
            m.Surface = ComicSurface.Brick;
            m.Cylinder(p + V(0, h - .4f, 0), 1.22f, .8f, BrickDark, 8, 1.12f);
            m.Cylinder(p + V(0, h + .1f, 0), 1.4f, .2f, StoneShade, 8);
        }

        /// <summary>The hospital behind the Workshops (background): a white ward block with uneven vertical window
        /// slots on a red-brick and charcoal podium, rooftop plant. Its long windowed side faces -X (a building on the
        /// right of the road) or, with face = 1, +X (one on the left).</summary>
        static void Hospital(ComicMesh m, Vector3 p, int face = -1)
        {
            float wx = face < 0 ? p.x - 6.02f : p.x + 14.02f, px2 = face < 0 ? p.x - 9.02f : p.x + 13.02f;
            Vector3 wn = face < 0 ? Vector3.left : Vector3.right;
            Color white = K.Hex("EEEDE8");
            m.Surface = ComicSurface.Brick;
            m.Box(p + V(2f, 3f, -8f), V(22f, 8f, 26f), Brick);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(2f, 3f, 16f), V(22f, 8f, 22f), Charcoal);
            m.Surface = ComicSurface.Plaster;
            m.Box(p + V(4f, 16f, 2f), V(20f, 18f, 44f), white);
            m.Box(p + V(4f, 25.3f, 2f), V(20.6f, .6f, 44.6f), K.Hex("D9D7D0"));
            m.Surface = ComicSurface.Metal;
            m.Box(p + V(6f, 27f, -6f), V(10f, 3f, 12f), K.Hex("C9CDD1"));
            m.Surface = ComicSurface.Glass;
            var r = new System.Random(77);
            for (int fl = 0; fl < 5; fl++)
                for (float z = -18f; z < 22f; z += F(r, 1.6f, 3.4f))
                {
                    float w = F(r, .6f, 1.3f), y = p.y + 8.6f + fl * 3.3f;
                    K.Face(m, V(wx, y, p.z + z), V(wx, y, p.z + z + w), V(wx, y + 2.3f, p.z + z + w), V(wx, y + 2.3f, p.z + z), GlassDark, wn, false);
                }
            for (int fl = 0; fl < 5; fl++)
                for (float x = -4f; x < 12f; x += F(r, 1.8f, 3.2f))
                {
                    float w = F(r, .6f, 1.2f), y = p.y + 8.6f + fl * 3.3f;
                    K.Face(m, V(p.x + x, y, p.z - 20.02f), V(p.x + x + w, y, p.z - 20.02f), V(p.x + x + w, y + 2.3f, p.z - 20.02f), V(p.x + x, y + 2.3f, p.z - 20.02f), GlassDark, Vector3.back, false);
                }
            for (float z = -19f; z < 4f; z += 2.6f)
                K.Face(m, V(px2, p.y + 1.6f, p.z + z), V(px2, p.y + 1.6f, p.z + z + 1.5f), V(px2, p.y + 5.4f, p.z + z + 1.5f), V(px2, p.y + 5.4f, p.z + z), GlassDark, wn, false);
        }

        // ---------------------------------------------------------------- Midland obstacles and animals

        // A shopping trolley: wire basket (grey panels with an ink rim), a red handle and child seat, four castors.
        static ComicMesh TrolleyMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Metal };
            Color wire = K.Hex("B9C0C6");
            m.Box(V(0, .62f, .05f), V(.52f, .06f, .86f), wire);
            m.Box(V(-.26f, .82f, .05f), V(.04f, .4f, .86f), wire);
            m.Box(V(.26f, .82f, .05f), V(.04f, .4f, .86f), wire);
            m.Box(V(0, .82f, .47f), V(.52f, .4f, .04f), wire);
            m.Box(V(0, .86f, -.38f), V(.56f, .48f, .04f), wire, Quaternion.Euler(-12f, 0, 0));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, 1.03f, .05f), V(.58f, .04f, .92f), Ink);
            m.Box(V(0, .3f, .05f), V(.5f, .05f, .8f), SteelDark);
            foreach (float x in new[] { -.24f, .24f }) m.Box(V(x, .45f, -.3f), V(.04f, .36f, .04f), SteelDark);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, 1.06f, -.46f), V(.6f, .07f, .07f), K.Hex("C8372D"));
            m.Box(V(0, .88f, -.3f), V(.44f, .22f, .04f), K.Hex("C8372D"));
            m.Surface = ComicSurface.Rubber;
            foreach (float x in new[] { -.22f, .22f }) foreach (float z in new[] { -.3f, .4f }) m.Box(V(x, .1f, z), V(.06f, .2f, .14f), Ink);
            return m;
        }

        // A plastic water-filled road barrier: red and white halves with the moulded waist and feet.
        static ComicMesh WaterBarrierMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            Color red = K.Hex("D8382E");
            for (int i = 0; i < 2; i++)
            {
                float x = -.5f + i;
                Color c = i == 0 ? red : White;
                m.Box(V(x, .2f, 0), V(.98f, .4f, .5f), c);
                m.Box(V(x, .56f, 0), V(.94f, .34f, .36f), c);
                m.Box(V(x, .8f, 0), V(.98f, .16f, .3f), c);
                m.Box(V(x, .56f, 0), V(.5f, .2f, .38f), Color.Lerp(c, Ink, .2f));
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, .03f, 0), V(2f, .06f, .52f), Ink);
            return m;
        }

        // A timber pallet of machine parts: slats on bearers, two crates and a banded drum.
        static ComicMesh PalletMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            foreach (float z in new[] { -.42f, 0f, .42f }) m.Box(V(0, .06f, z), V(1.2f, .1f, .12f), TimberDark);
            for (int i = 0; i < 5; i++) m.Box(V(-.5f + i * .25f, .14f, 0), V(.2f, .05f, 1f), Timber);
            m.Box(V(-.25f, .46f, -.15f), V(.62f, .58f, .6f), K.Hex("B98A5A"));
            m.Box(V(-.25f, .46f, -.15f), V(.66f, .08f, .64f), TimberDark);
            m.Box(V(-.2f, .86f, -.1f), V(.44f, .22f, .4f), K.Hex("C9A070"));
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(V(.36f, .52f, .18f), .22f, .7f, K.Hex("2F5D8A"), 8);
            m.Cylinder(V(.36f, .52f, .18f), .23f, .06f, Ink, 8);
            m.Cylinder(V(.36f, .88f, .18f), .23f, .04f, Ink, 8);
            return m;
        }

        static ComicMesh BikeHoopsMesh()
        {
            var m = new ComicMesh();
            BikeHoopsArt(m, Vector3.zero, 0f);
            return m;
        }

        // Australian magpie: black with the white nape, shoulder patch and rump, a pale blue-grey bill, dark legs.
        static ComicMesh MagpieMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color black = K.Hex("1F2125"), white = K.Hex("F3F1EA");
            Blob(m, V(0, .3f, .04f), V(.28f, .28f, .5f), black, 6, 3);
            m.Box(V(0, .4f, .0f), V(.2f, .08f, .22f), white);
            m.Box(V(0, .34f, .3f), V(.16f, .06f, .14f), white);
            m.Box(V(0, .3f, .42f), V(.14f, .05f, .26f), black, Quaternion.Euler(-10f, 0, 0));
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(V(s * .13f, .32f, .08f), V(.04f, .16f, .34f), black, Quaternion.Euler(0, 0, s * -10f));
                m.Box(V(s * .14f, .36f, 0f), V(.03f, .08f, .16f), white, Quaternion.Euler(0, 0, s * -10f));
                m.Box(V(s * .06f, .1f, .02f), V(.035f, .2f, .035f), black);
                m.Box(V(s * .06f, .015f, -.02f), V(.08f, .03f, .12f), black);
                m.Box(V(s * .07f, .5f, -.24f), V(.02f, .03f, .03f), K.Hex("B5651D"));
            }
            m.Box(V(0, .47f, -.18f), V(.16f, .16f, .18f), black);
            m.Box(V(0, .5f, -.1f), V(.14f, .08f, .08f), white);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .46f, -.33f), V(.05f, .05f, .14f), K.Hex("C9D3DC"));
            m.Box(V(0, .46f, -.41f), V(.04f, .04f, .03f), black);
            return m;
        }

        // Galah: grey back and wings, rose-pink breast and face, a pale pink crest, a small horn-coloured bill.
        static ComicMesh GalahMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color pink = K.Hex("E0708C"), grey = K.Hex("A9AEB6"), pale = K.Hex("F4D6DC");
            Blob(m, V(0, .26f, .02f), V(.28f, .3f, .42f), pink, 6, 3);
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(V(s * .13f, .3f, .08f), V(.05f, .2f, .34f), grey, Quaternion.Euler(0, 0, s * -10f));
                m.Box(V(s * .05f, .05f, 0f), V(.05f, .1f, .08f), K.Hex("6A6A70"));
                m.Box(V(s * .08f, .46f, -.2f), V(.02f, .03f, .03f), Ink);
            }
            m.Box(V(0, .3f, .32f), V(.14f, .05f, .2f), grey, Quaternion.Euler(-14f, 0, 0));
            m.Box(V(0, .44f, -.14f), V(.18f, .18f, .18f), pink);
            m.Box(V(0, .56f, -.12f), V(.16f, .07f, .16f), pale);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .42f, -.26f), V(.07f, .08f, .07f), K.Hex("E6DCC6"));
            return m;
        }

        // Pigeon: blue-grey with darker wing bars, a green-purple neck, red feet.
        static ComicMesh PigeonMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color grey = K.Hex("8E99A6"), dark = K.Hex("56606C");
            Blob(m, V(0, .22f, .02f), V(.24f, .24f, .4f), grey, 6, 3);
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(V(s * .11f, .25f, .08f), V(.04f, .14f, .3f), Color.Lerp(grey, White, .2f), Quaternion.Euler(0, 0, s * -10f));
                m.Box(V(s * .12f, .25f, .14f), V(.03f, .14f, .05f), dark, Quaternion.Euler(0, 0, s * -10f));
                m.Box(V(s * .05f, .04f, 0f), V(.04f, .08f, .08f), K.Hex("C8553A"));
                m.Box(V(s * .06f, .4f, -.19f), V(.02f, .03f, .03f), K.Hex("E2A23A"));
            }
            m.Box(V(0, .26f, .28f), V(.12f, .05f, .16f), dark, Quaternion.Euler(-10f, 0, 0));
            m.Box(V(0, .33f, -.12f), V(.13f, .12f, .12f), K.Hex("4E7A6A"));
            m.Box(V(0, .4f, -.15f), V(.12f, .12f, .14f), grey);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .38f, -.25f), V(.04f, .04f, .08f), Ink);
            return m;
        }

        // ---------------------------------------------------------------- Midland far scenery

        /// <summary>
        /// The whole horizon round the rider at the foot of the hills. The Darling Scarp is close and high: a forested
        /// wall on both sides that closes in over the road ahead (patches of darker and lighter forest on its face, a
        /// lower foothill in front of it, two masts on the crest). In front of it on the right, over the yard, the roofs and gums
        /// of the flats; on the left the roofs of the town and a line of trees. Sheared with the road's grade. No colliders.
        /// </summary>
        static ComicMesh MidFar(float grade)
        {
            var m = new ComicMesh();
            var r = new System.Random(340);
            Color haze = K.Hex("C6D4E0");
            const float z0 = -220f, z1 = 820f;
            FarStrip(m, K.Edge, 68f, 330f, z1, K.Kerb - .03f, Color.Lerp(DryGrass, Ballast, .35f));
            FarStrip(m, -K.Edge, -68f, 330f, z1, K.Kerb - .03f, K.Hex("A39E92"));
            FarStrip(m, 68f, 220f, z0, z1, -.25f, Haze(K.Hex("A3A56C"), 140f, haze));
            FarStrip(m, 220f, 1400f, z0, z1, -.3f, Haze(K.Hex("8FA26A"), 420f, haze));
            FarStrip(m, -68f, -220f, z0, z1, -.25f, Haze(K.Hex("9DA472"), 140f, haze));
            FarStrip(m, -220f, -1400f, z0, z1, -.3f, Haze(K.Hex("8FA26A"), 420f, haze));
            // roofs: the town on the left (flat roofs, a few two-storey), houses and sheds on the right
            Color[] walls = { Cream, White, Butter, K.Hex("D9CBB0"), K.Hex("C9B08A"), K.Hex("B98A6A") };
            Color[] roofs = { RedOxide, IronRoof, GreenRoof, K.Hex("8A8F94"), IronRoofDark };
            foreach (int s in new[] { -1, 1 })
                for (float z = z0; z < z1; z += F(r, 14f, 20f))
                    for (int row = 0; row < 3; row++)
                    {
                        if (row == 2 && (z < -60f || z > 520f)) continue;      // the third row only where it can be seen
                        float x = s * (76f + row * 30f + F(r, 0f, 14f)), dist = Mathf.Sqrt(x * x + z * z);
                        if (s < 0 && row == 0 && r.NextDouble() < .5)
                        {
                            float w = F(r, 12f, 20f), h = F(r, 6f, 11f);
                            m.Surface = ComicSurface.Plaster;
                            m.Box(V(x, h * .5f - 1f, z), V(w, h + 2f, w * .7f), Haze(walls[r.Next(walls.Length)], dist, haze));
                            m.Box(V(x, h + .2f, z), V(w + .5f, .4f, w * .7f + .5f), Haze(Stone, dist, haze));
                        }
                        else FarHouse(m, V(x, 0f, z), F(r, 9f, 14f), F(r, 3.2f, 5.5f), Haze(walls[r.Next(walls.Length)], dist, haze), Haze(roofs[r.Next(roofs.Length)], dist, haze));
                    }
            foreach (int s in new[] { -1, 1 })
                for (float z = z0; z < z1; z += F(r, 12f, 20f))
                {
                    float x = s * F(r, 72f, 210f), h = F(r, 13f, 22f);
                    m.Surface = ComicSurface.Wood;
                    m.Cylinder(V(x, h * .3f, z), .45f, h * .6f, Haze(Bark, x, haze), 4, .6f);
                    m.Surface = ComicSurface.Foliage;
                    Blob(m, V(x, h * .75f, z), V(F(r, 8f, 13f), h * .5f, F(r, 8f, 12f)), Haze(Color.Lerp(LeafDark, LeafLight, F(r, 0f, 1f)), x, haze), 5, 3);
                }
            // the scarp: both sides swing in towards the road ahead and rise
            Func<float, float> swing = z => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-120f, 430f, z));
            Func<float, float> hR = z => 62f + 44f * swing(z) + 8f * Mathf.Sin(z * .011f + .6f) + 4f * Mathf.Sin(z * .037f);
            Func<float, float> hL = z => 54f + 46f * swing(z) + 7f * Mathf.Sin(z * .013f + 2.1f) + 4f * Mathf.Sin(z * .031f + 1f);
            Func<float, float> nR = z => Mathf.Lerp(300f, 110f, swing(z));
            Func<float, float> nL = z => Mathf.Lerp(340f, 116f, swing(z));
            Color foot = Haze(K.Hex("86A064"), 240f, haze), slope = Haze(K.Hex("587A5C"), 330f, haze), crest = Haze(K.Hex("6F8E96"), 620f, haze);
            Scarp(m, 1, z0, z1, -.4f, nR, hR, 200f, foot, slope, crest);
            Scarp(m, -1, z0, z1, -.4f, nL, hL, 210f, foot, Haze(K.Hex("5E8062"), 360f, haze), crest);
            // the wooded face: darker and lighter patches of forest, and a lower foothill standing in front of each wall
            ScarpPatches(m, 1, z0, z1, -.4f, nR, hR, 200f, Haze(K.Hex("46664E"), 330f, haze), Haze(K.Hex("6E8E66"), 330f, haze), 341);
            ScarpPatches(m, -1, z0, z1, -.4f, nL, hL, 210f, Haze(K.Hex("4A6A50"), 360f, haze), Haze(K.Hex("72906A"), 360f, haze), 342);
            Scarp(m, 1, z0, z1, -.35f, z => Mathf.Max(96f, nR(z) - 60f), z => hR(z) * (.3f + .08f * Mathf.Sin(z * .021f)), 90f,
                Haze(K.Hex("8FA667"), 200f, haze), Haze(K.Hex("6A8A56"), 240f, haze), Haze(K.Hex("7C9A64"), 300f, haze));
            Scarp(m, -1, z0, z1, -.35f, z => Mathf.Max(100f, nL(z) - 70f), z => hL(z) * (.28f + .08f * Mathf.Sin(z * .017f + 1f)), 100f,
                Haze(K.Hex("8FA667"), 200f, haze), Haze(K.Hex("6E8C5A"), 240f, haze), Haze(K.Hex("7C9A64"), 300f, haze));
            // two masts on the crest
            foreach ((float z, int s) in new[] { (250f, 1), (330f, -1) })
            {
                float near = s > 0 ? nR(z) : nL(z), h = s > 0 ? hR(z) : hL(z);
                Vector3 p = V(s * (near + 104f), -.4f + h, z);
                m.Surface = ComicSurface.PaintedMetal;
                m.Cylinder(p + V(0, 13f, 0), .4f, 26f, Haze(K.Hex("D8382E"), 300f, haze), 4, .3f);
            }
            m.Shear(grade);
            return m;
        }
    }
}
