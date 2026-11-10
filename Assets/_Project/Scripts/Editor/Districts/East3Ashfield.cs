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
    // ASHFIELD (order 280): the red shared path beside the Midland line (PedestrianMall: no cars). Railway, the tiny
    // station and the light industry on the left (-X); Ashfield Reserve, the State Housing cottages and then Ashfield
    // Flats (samphire, pools, paperbarks, the boardwalk over the Chapman St drain) on the right (+X).
    public static partial class East3Builder
    {
        /// <summary>The drain jump (Ramp + road-wide RoadGap): numbers for gameplay.</summary>
        public const float DrainDeckStart = 3f, DrainRampStart = 9f, DrainRise = 6f, DrainHeight = 1.4f, DrainTop = 3f, DrainGap = 6f;
        const float AshRailX = 13f, AshTrackA = -15.6f, AshTrackB = -22.4f, AshBallastY = .3f, DrainWater = -1.25f;

        static readonly Color Maroon = K.Hex("8A2F45"), MaroonLight = K.Hex("C4577A"), ShelterCream = K.Hex("EFEAD8"), BrickPlatform = K.Hex("A8553F");
        static readonly Color Samphire1 = K.Hex("7D7044"), Samphire2 = K.Hex("86573F"), Samphire3 = K.Hex("6F7D46"), SamphirePink = K.Hex("A2665A"), Mud = K.Hex("8F8869"), VergeDry = K.Hex("8FA05C");
        static readonly Color PoolBlue = K.Hex("86AFC2"), PoolLight = K.Hex("B4D2DC"), TeaWater = K.Hex("7A5E3C"), TeaLight = K.Hex("A88A5C");
        static readonly Color FactoryCream = K.Hex("E4DAB8"), ShedGreen = K.Hex("7E9A86"), ShedGrey = K.Hex("A9B0B4"), Fibro = K.Hex("DDE3DA"), FibroBlue = K.Hex("BFD3D9"), FibroPink = K.Hex("E6CDBE");
        static string AshPf(string name) => AshPrefabs + "/" + name + ".prefab";

        static void BuildAshfield(TrackTheme theme, bool atlas)
        {
            K.Begin(AshMeshes, AshPrefabs, CbdBuilder.DataFolder);
            Obstacle bollard = K.SaveObstacle(AshPf("OBS_Ashfield_PathBollard"), "OBS_Ashfield_PathBollard", PathBollardMesh(), new Vector3(.4f, 1.05f, .4f), ObstacleKind.Thing, true, false, 1f);
            Obstacle sign = K.SaveObstacle(AshPf("OBS_Ashfield_PathSign"), "OBS_Ashfield_PathSign", PathSignMesh(), new Vector3(.6f, 2.3f, .4f), ObstacleKind.Thing, false, false, .7f);
            Obstacle pallet = K.SaveObstacle(AshPf("OBS_Ashfield_PaverPallet"), "OBS_Ashfield_PaverPallet", PaverPalletMesh(), new Vector3(1.2f, .9f, 1.05f), ObstacleKind.Thing, true, false, .8f);
            Obstacle sprinkler = K.SaveObstacle(AshPf("OBS_Ashfield_Sprinkler"), "OBS_Ashfield_Sprinkler", SprinklerMesh(), new Vector3(1.5f, .8f, 1.5f), ObstacleKind.Thing, true, true, .8f);
            GameObject egret = SaveAnimal(AshPf("ANM_Ashfield_Egret"), "ANM_Ashfield_Egret", EgretMesh(), new Vector3(0, .6f, 0), new Vector3(.4f, 1.2f, .8f), .25f, true);
            GameObject ducks = SaveAnimal(AshPf("ANM_Ashfield_Ducks"), "ANM_Ashfield_Ducks", DucksMesh(), new Vector3(0, .22f, .35f), new Vector3(.5f, .45f, 1.5f), .5f, true);
            GameObject ibis = SaveAnimal(AshPf("ANM_Ashfield_Ibis"), "ANM_Ashfield_Ibis", IbisMesh(), new Vector3(0, .4f, 0), new Vector3(.4f, .8f, .8f), .4f, true);
            Ramp kicker = K.SaveRamp(AshPf("RMP_Ashfield_BoardwalkKicker"), "RMP_Ashfield_BoardwalkKicker", 3f, 5f, 1.25f, 1f, 1f, .5f, JarrahLight, Jarrah, false);

            Write(AshEntry(), AshChunkPath("Entry"));
            Write(AshStation(), AshChunkPath("Station"));
            Write(AshReserve(), AshChunkPath("Reserve"));
            Write(AshFlats(), AshChunkPath("Flats"));
            Write(AshDrain(), AshChunkPath("Drain"));
            Write(AshPaperbarks(), AshChunkPath("Paperbarks"));
            Write(AshExit(), AshChunkPath("Exit"));
            GameObject far = K.SaveFarScenery(AshPf("FAR_Ashfield_Flats"), "FAR_Ashfield_Flats", AshFar(-AshDrop / 40f));

            District d = OpenDistrict(AshDistrictPath, AshId, "ASHFIELD", 280);
            d.length = new Vector2(250f, 280f);
            d.chunks = AshChunkIds.Select(c => Chunk(AshChunkPath(c))).ToArray();
            d.entry = Chunk(AshChunkPath("Entry"));
            d.exit = Chunk(AshChunkPath("Exit"));
            d.obstacles = new[] { bollard, sign, pallet, sprinkler };
            d.jumpObstacles = new[] { bollard, pallet, sprinkler };
            d.hazards = ThemeHazards(theme, "HZ_SidewalkStroll", "HZ_Crossing", "HZ_PoliceStop");
            var people = new List<GameObject>(K.People(theme, "Jogger", "Jogger", "Jogger", "DogWalker", "DogWalker", "Skater", "Kid", "Elder", "Student", "Courier"));
            people.Add(egret); people.Add(egret); people.Add(ducks); people.Add(ducks); people.Add(ibis); people.Add(ibis);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { kicker };
            d.rampFrequency = 1f;
            // Soft wetland light: a pale green-blue haze over the flats, warm sun on the samphire.
            d.fogColor = new Color(.70f, .82f, .90f);
            d.skyTint = new Color(.99f, 1f, 1f);
            d.ambientSky = new Color(.70f, .85f, .95f);
            d.ambientEquator = new Color(.88f, .90f, .82f);
            d.ambientGround = new Color(.66f, .66f, .50f);
            d.sunColor = new Color(1f, .97f, .88f);
            d.sunIntensity = 1.4f;
            d.farScenery = far;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/Ashfield] " + string.Join("; ", K.Report));
            if (!atlas) return;
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("StationShelter", null, Make(m => StationShelter(m, V(0, 0, 0), 9f, "ASHFIELD"))),
                ("FootbridgeRamps", null, Make(m => Footbridge(m, 0f))),
                ("Paperbark", null, Make(m => Paperbark(m, Vector3.zero, 8f, 4))),
                ("PaperbarkLeaning", null, Make(m => Paperbark(m, Vector3.zero, 7f, 9, false, 0f))),
                ("Fig", null, Make(m => Fig(m, Vector3.zero, 1f, 2))),
                ("DeadTree", null, Make(m => DeadTree(m, Vector3.zero, 7f, 3))),
                ("SedgeAndBulrush", null, Make(m => { Sedge(m, V(0, 0, 0), 1f, 1); Sedge(m, V(.9f, 0, .4f), .8f, 2); Bulrush(m, V(-.8f, 0, .5f), .9f, 3); })),
                ("SamphireClumps", null, Make(m => { for (int i = 0; i < 5; i++) SamphireClump(m, V((i % 3) * 1.2f, 0, (i / 3) * 1.1f), .8f + (i % 2) * .3f, i); })),
                ("StateHouse", null, Make(m => StateHouse(m, new CqLot(-1, -5.5f, 5.5f, 0f, 8f, 0f), Fibro, Terracotta, 0))),
                ("HillsHoist", null, Make(m => HillsHoist(m, Vector3.zero))),
                ("Factory", null, Make(m => Factory(m, V(0, 0, 0), 26f, 14f, 0))),
                ("Shed", null, Make(m => Factory(m, V(0, 0, 0), 14f, 12f, 1))),
                ("Forklift", null, Make(m => Forklift(m, Vector3.zero, 30f))),
                ("Containers", null, Make(m => ContainerStack(m, Vector3.zero, 2))),
                ("Boardwalk", null, Make(m => { BoardwalkRails(m, 0f, 6f, K.Kerb); m.Surface = ComicSurface.Wood; m.Box(V(0, .1f, 3f), V(16f, .2f, 6f), JarrahLight); })),
                ("ViewingPlatform", null, Make(m => ViewingPlatform(m, Vector3.zero))),
                ("InterpretiveSign", null, Make(m => InterpretiveSign(m, Vector3.zero, 0f))),
                ("Spoonbill", null, Make(m => WaderArt(m, Vector3.zero, 0f, 1f, 1))),
                ("BlackSwan", null, Make(m => SwanArt(m, Vector3.zero, 0f, 1f))),
                ("PathBollard", bollard.gameObject, null),
                ("PathSign", sign.gameObject, null),
                ("PaverPallet", pallet.gameObject, null),
                ("Sprinkler", sprinkler.gameObject, null),
                ("Egret", egret, null),
                ("Ducks", ducks, null),
                ("Ibis", ibis, null),
                ("BoardwalkKicker", kicker.gameObject, null),
            };
            K.RenderAtlas("art-src/districts/ashfield/props-atlas.png", items);
        }

        // ---------------------------------------------------------------- Ashfield chunks

        /// <summary>The shared path with the street's cross-section: 8 m of red asphalt with a dashed white centre line and
        /// white edge lines, concrete edging, and rideable verges at kerb height (left: dry gravelly grass along the
        /// railway fence; right: mown lawn). No tile joints.</summary>
        static void SharedPath(CqChunkDraft d, Color vergeL, Color vergeR, float skipFrom = -1f, float skipTo = -1f)
        {
            d.Type = RoadType.PedestrianMall;
            ComicMesh m = d.Road;
            float h = K.Kerb, w = K.Half, o = K.Edge, L = d.Length;
            var cuts = new List<float>();
            for (float z = 0; z < L - .01f; z += 4f) cuts.Add(z);
            cuts.Add(L);
            if (skipFrom >= 0f) { cuts.Add(skipFrom); cuts.Add(skipTo); }
            cuts = cuts.Distinct().OrderBy(z => z).ToList();
            for (int ci = 0; ci + 1 < cuts.Count; ci++)
            {
                float z0 = cuts[ci], z1 = cuts[ci + 1];
                if (z1 - z0 < .01f || skipFrom >= 0f && z0 >= skipFrom - .01f && z1 <= skipTo + .01f) continue;
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(-w, 0, z0), V(-w, 0, z1), V(w, 0, z1), V(w, 0, z0), PathRed, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    Color c = s < 0 ? vergeL : vergeR;
                    Vector3 inward = new Vector3(-s, 0, 0), outward = new Vector3(s, 0, 0);
                    m.Surface = ComicSurface.Foliage;
                    K.Face(m, V(s * w, h, z0), V(s * w, h, z1), V(s * o, h, z1), V(s * o, h, z0), c, Vector3.up, false);
                    K.Face(m, V(s * o, -2f, z0), V(s * o, -2f, z1), V(s * o, h, z1), V(s * o, h, z0), c, outward, false);
                    m.Surface = ComicSurface.Concrete;
                    K.Face(m, V(s * w, -.05f, z0), V(s * w, -.05f, z1), V(s * w, h + .03f, z1), V(s * w, h + .03f, z0), KerbCap, inward, false);
                    K.Face(m, V(s * w, h + .03f, z0), V(s * w, h + .03f, z1), V(s * (w + .22f), h + .03f, z1), V(s * (w + .22f), h + .03f, z0), KerbCap, Vector3.up, false);
                    K.Face(m, V(s * (w + .22f), h, z0), V(s * (w + .22f), h, z1), V(s * (w + .22f), h + .03f, z1), V(s * (w + .22f), h + .03f, z0), KerbCap, outward, false);
                    m.Surface = ComicSurface.Plain;
                    K.Face(m, V(s * (w - .32f), .022f, z0), V(s * (w - .32f), .022f, z1), V(s * (w - .2f), .022f, z1), V(s * (w - .2f), .022f, z0), White, Vector3.up, false);
                }
            }
            m.Surface = ComicSurface.Plain;
            for (float z = 1f; z < L - 2f; z += 5f)
            {
                if (skipFrom >= 0f && z + 2.6f > skipFrom && z < skipTo) continue;
                K.Face(m, V(-.07f, .024f, z), V(-.07f, .024f, z + 2.6f), V(.07f, .024f, z + 2.6f), V(.07f, .024f, z), White, Vector3.up, false);
            }
            var vr = new System.Random(ComicKitGeometry.StableHash(d.Id));
            m.Surface = ComicSurface.Foliage;
            for (int s = -1; s <= 1; s += 2)
                for (float z = F(vr, .5f, 2f); z < L - 2.5f; z += F(vr, 2.2f, 4.2f))
                {
                    if (skipFrom >= 0f && z + 2.4f > skipFrom && z < skipTo) continue;
                    Color c = s < 0 ? vergeL : vergeR;
                    float x = s * F(vr, w + .6f, o - 1.3f), wd = F(vr, .5f, 1.3f), ln = F(vr, .8f, 2.2f);
                    Color tone = vr.Next(3) == 0 ? Color.Lerp(c, Dirt, .45f) : vr.Next(2) == 0 ? Color.Lerp(c, K.R.ink, .14f) : Color.Lerp(c, K.R.yellow, .2f);
                    K.Face(m, V(x - wd * .5f, h + .012f, z), V(x - wd * .3f, h + .012f, z + ln), V(x + wd * .5f, h + .012f, z + ln * .85f), V(x + wd * .4f, h + .012f, z + ln * .1f), tone, Vector3.up, false);
                }
            foreach (float ze in new[] { 0f, L })
            {
                Vector3 normal = ze <= 0f ? Vector3.back : Vector3.forward;
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(-w, -.8f, ze), V(w, -.8f, ze), V(w, 0, ze), V(-w, 0, ze), PathRed, normal, false);
                for (int s = -1; s <= 1; s += 2) K.Face(m, V(s * w, -2f, ze), V(s * o, -2f, ze), V(s * o, h, ze), V(s * w, h, ze), s < 0 ? vergeL : vergeR, normal, false);
            }
        }

        /// <summary>The railway at grade on the left: the black palisade fence beside the path, ballast, two tracks,
        /// portal gantries at gantryZ and the wires; beyond the tracks a far fence and Railway Parade.</summary>
        static void GradeRail(CqChunkDraft d, float z0, float z1, Color fence, params float[] gantryZ) => GradeRail(d, z0, z1, fence, 0f, true, gantryZ);

        /// <summary>shift moves the whole corridor further from the road (the palisade then stands on the corridor's own
        /// edge, not beside the path); parade = Railway Parade beyond the far fence.</summary>
        static void GradeRail(CqChunkDraft d, float z0, float z1, Color fence, float shift, bool parade, params float[] gantryZ)
        {
            ComicMesh g = d.Road;
            float near = -AshRailX - shift, far = -25.4f - shift;
            if (shift < .01f) Ground(g, near, -(K.Edge - .02f), z0, z1, K.Kerb + .02f, Color.Lerp(VergeDry, Ballast, .5f), ComicSurface.Concrete);
            TrackBed(g, near, far, z0, z1, AshBallastY, AshTrackA - shift, AshTrackB - shift);
            RailFence(g, shift < .01f ? -(K.Edge + .7f) : near + .6f, z0, z1, K.Kerb, 1.8f, fence, true);
            RailFence(g, far - .8f, z0, z1, K.Kerb, 1.8f, GalvDark, false, K.Hex("B9C0C6"));
            if (parade)
            {
                Ground(g, far - 10.1f, far, z0, z1, K.Kerb - .02f, K.Hex("8E9299"), ComicSurface.Asphalt);
                Ground(g, -72f - shift, far - 10.1f, z0, z1, K.Kerb + .01f, K.Hex("C4BCA6"), ComicSurface.Concrete);
                for (float z = z0 + 1f; z < z1 - 2f; z += 6f) Ground(g, far - 5.7f, far - 5.5f, z, z + 3f, K.Kerb, White);
            }
            foreach (float z in gantryZ) Gantry(d.SlopedFrontage, near - .5f, far + .5f, AshBallastY, z, AshTrackA - shift, AshTrackB - shift);
            Sloped(d, d.Deep, t => Catenary(t, z0, z1, AshBallastY + .2f, AshTrackA - shift, AshTrackB - shift));
            if (shift < .01f) WallBox(d, -(K.Edge + .5f), -(K.Edge + 40f), z0, z1, 7f);
        }

        static CqChunkDraft AshEntry()
        {
            // Calm at z = 0 (clipped hedges, lawn, gums), then the railway fence and the reserve's first trees.
            var d = new CqChunkDraft("Ashfield_Entry", AshDrop);
            SharedPath(d, VergeDry, Lawn);
            GradeRail(d, 0f, 40f, CharcoalDark, 14f, 34f);
            Hedge(d.SlopedFrontage, -1, .4f, 12f, 1.1f, 2.1f, 1.3f);
            Industry(d, 0f, 40f, 2801, 1);
            SideGround(d, 1, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            Hedge(d.SlopedFrontage, 1, .4f, 14f, .5f, 1.6f, 1.2f);
            Gum(d.Props, V(K.Edge + 4.5f, d.Ground(5f), 5f), 12f, 281);
            Gum(d.Props, V(K.Edge + 7f, d.Ground(22f), 22f), 14f, 282);
            StateHouse(d.SlopedFrontage, new CqLot(1, 27f, 38f, K.Edge + 9f, 8f, K.Kerb), FibroBlue, Terracotta, 1);
            HillsHoist(d.Props, V(K.Edge + 4.5f, d.Ground(33f), 33f));
            WallBox(d, K.Edge + 1f, K.Edge + 40f, 0f, 40f, 7f);
            DeepSuburb(d, 1, 0f, 40f, 2802, 26f);
            PathFurniture(d, 20f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft AshStation()
        {
            // Ashfield station: one narrow island platform between the tracks with its little barrel-vault shelter on
            // maroon posts, the footbridge and its long galvanised ramps down to the path, a train at the platform; the
            // long cream factory with its bands of steel windows across Railway Parade. Right: Ashfield Reserve.
            var d = new CqChunkDraft("Ashfield_Station", AshDrop);
            SharedPath(d, VergeDry, Lawn);
            GradeRail(d, 0f, 40f, CharcoalDark, 4f);
            ComicMesh m = d.SlopedFrontage;
            // the island platform (z 3..36): grey panel faces, red brick paving, yellow tactile edges
            m.Surface = ComicSurface.Concrete;
            Strip(m, V(-20.8f, AshBallastY - .2f, 3f), V(-17.2f, AshBallastY + 1.05f, 36f), K.Hex("B9BBB8"));
            Ground(m, -20.75f, -17.25f, 3f, 36f, AshBallastY + 1.06f, BrickPlatform, ComicSurface.Paving);
            Ground(m, -17.75f, -17.3f, 3f, 36f, AshBallastY + 1.07f, HazardYellow);
            Ground(m, -20.7f, -20.25f, 3f, 36f, AshBallastY + 1.07f, HazardYellow);
            StationShelter(m, V(-19f, AshBallastY + 1.06f, 6f), 9f, "ASHFIELD");
            Bench(d.Props, V(-19f, d.Y(19.2f) + AshBallastY + 1.06f, 19.2f), 90f);
            Footbridge(m, 31f);
            Sloped(d, d.Landmark, t => Train(t, V(AshTrackB, AshBallastY + .16f, 1f), 2, 0, 1, true, true, 17.5f));
            NameBoard(d.Props, V(-(K.Edge + 2.2f), d.Ground(8f), 8f), "ASHFIELD", .075f, Maroon, White, 2.6f);
            Industry(d, 0f, 40f, 2811, 0);
            // right: the reserve: lawn, the big figs, a bench, a drinking fountain
            SideGround(d, 1, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            ReserveLawn(d, 0f, 40f, 2812);
            Fig(d.Props, V(K.Edge + 9f, d.Ground(14f), 14f), 1.05f, 2);
            Fig(d.Deep, V(K.Edge + 27f, d.Ground(31f), 31f), 1.25f, 3);
            Bench(d.Props, V(K.Edge + 2.2f, d.Ground(26f), 26f), 90f);
            Gum(d.Props, V(K.Edge + 5f, d.Ground(36f), 36f), 13f, 285);
            DeepSuburb(d, 1, 0f, 40f, 2813, 40f);
            PathFurniture(d, 22f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft AshReserve()
        {
            // The reserve gives way to the State Housing cottages of the late 1950s: fibro and weatherboard, tiled hips,
            // no front fences, Hills Hoists in the yards, wide verges under gums. Left: the station car park beside the
            // track, steel sheds, stacked concrete products and a forklift.
            var d = new CqChunkDraft("Ashfield_Reserve", AshDrop);
            SharedPath(d, VergeDry, Lawn);
            GradeRail(d, 0f, 40f, CharcoalDark, 6f, 26f);
            Industry(d, 0f, 40f, 2821, 2);
            SideGround(d, 1, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            ReserveLawn(d, 0f, 12f, 2822);
            Fig(d.Props, V(K.Edge + 7.5f, d.Ground(5f), 5f), .95f, 5);
            ComicMesh f = d.SlopedFrontage;
            StateHouse(f, new CqLot(1, 13f, 24f, K.Edge + 8f, 8f, K.Kerb), Fibro, Terracotta, 0);
            StateHouse(f, new CqLot(1, 27f, 38f, K.Edge + 9f, 8f, K.Kerb), FibroPink, K.Hex("8A8F94"), 1);
            HillsHoist(d.Props, V(K.Edge + 4f, d.Ground(25.5f), 25.5f));
            Ground(d.Road, K.Edge, K.Edge + 8f, 17f, 19.4f, K.Kerb + .07f, ConcretePath, ComicSurface.Paving);
            Ground(d.Road, K.Edge, K.Edge + 9f, 31f, 33.4f, K.Kerb + .07f, ConcretePath, ComicSurface.Paving);
            ParkedCar(d.Props, V(K.Edge + 5f, d.Ground(32.2f), 32.2f), 90f, K.Hex("C9CED3"));
            Gum(d.Props, V(K.Edge + 3f, d.Ground(11.5f), 11.5f), 12f, 288);
            Gum(d.Props, V(K.Edge + 3.4f, d.Ground(39f), 39f), 10f, 289);
            PowerPole(d.Props, V(K.Edge + 1.2f, d.Ground(25f), 25f));
            Sloped(d, d.Deep, t => PowerLines(t, K.Edge + 1.2f, K.Kerb + 8.5f, 0f, 40f));
            WallBox(d, K.Edge + 1f, K.Edge + 40f, 0f, 40f, 7f);
            DeepSuburb(d, 1, 0f, 40f, 2823, 22f);
            PathFurniture(d, 15f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft AshFlats()
        {
            // Ashfield Flats open out on the right: the samphire saltmarsh in rust, olive and maroon, shallow pale pools
            // edged with sedges and bulrush, dead white trees, a line of paperbarks, waterbirds; the river beyond.
            var d = new CqChunkDraft("Ashfield_Flats", AshDrop);
            SharedPath(d, VergeDry, VergeDry);
            GradeRail(d, 0f, 40f, CharcoalDark, 8f, 28f);
            Sloped(d, d.Landmark, t => Train(t, V(AshTrackA, AshBallastY + .16f, 2f), 2, 1, 1, true, true, 18f));
            Industry(d, 0f, 40f, 2831, 3);
            Flats(d, 0f, 40f, 2832, true);
            InterpretiveSign(d.Props, V(K.Edge + 1.3f, d.Ground(9f), 9f), 90f);
            DeadTree(d.Props, V(K.Edge + 13f, d.Y(22f) - .5f, 22f), 7f, 3);
            DeadTree(d.Props, V(K.Edge + 24f, d.Y(8f) - .6f, 8f), 5.5f, 4);
            WaderArt(d.Props, V(K.Edge + 9.5f, d.Y(14f) - .62f, 14f), 40f, 1.35f, 0);
            WaderArt(d.Props, V(K.Edge + 11.4f, d.Y(16.4f) - .62f, 16.4f), -70f, 1.2f, 0);
            WaderArt(d.Props, V(K.Edge + 13f, d.Y(33f) - .62f, 33f), 110f, 1.35f, 1);
            SwanArt(d.Props, V(K.Edge + 12f, d.Y(27f) - .6f, 27f), 60f, 1.3f);
            SwanArt(d.Props, V(K.Edge + 14f, d.Y(25.6f) - .6f, 25.6f), 85f, 1.15f);
            PathFurniture(d, 30f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft AshDrain()
        {
            // The path crosses the Chapman St drain on the jarrah boardwalk: timber deck and rails, a kicker at the end,
            // then 6 m of tea-brown drain water across the whole width (SPLASH!), grassy banks under arching paperbarks.
            var d = new CqChunkDraft("Ashfield_Drain", AshDrop);
            float j0 = DrainDeckStart, ramp = DrainRampStart, lip = ramp + DrainRise + DrainTop, far = lip + DrainGap;
            SharedPath(d, VergeDry, VergeDry, j0, far);
            // left: the railway crosses the drain on a low concrete culvert; right: the flats, cut by the drain
            GradeRail(d, 0f, 40f, CharcoalDark, 30f);
            Industry(d, 0f, 40f, 2841, 1);
            float bank0 = lip - 4f, bank1 = far + 1.2f;
            Flats(d, 0f, bank0, 2842, false);
            Flats(d, bank1, 40f, 2843, false);
            ComicMesh g = d.Road;
            // the channel: tea-brown water under the boardwalk and out across the flats, sloping grassy banks
            K.Water(g, -K.Edge, 70f, bank0 + 1f, bank1 - .8f, DrainWater, TeaWater, TeaLight, 2844);
            Terrain(g, K.Edge, 70f, bank0, bank0 + 1f, 3.4f, 1f, (x, z) => Mathf.Lerp(FlatsH(x, z), DrainWater - .15f, Mathf.InverseLerp(bank0, bank0 + 1f, z)), (x, z, y) => Color.Lerp(VergeDry, Mud, .5f), ComicSurface.Foliage);
            Terrain(g, K.Edge, 70f, bank1 - .8f, bank1, 3.4f, .8f, (x, z) => Mathf.Lerp(DrainWater - .15f, FlatsH(x, z), Mathf.InverseLerp(bank1 - .8f, bank1, z)), (x, z, y) => Color.Lerp(VergeDry, Mud, .5f), ComicSurface.Foliage);
            ComicMesh w = d.SlopedFrontage;
            w.Surface = ComicSurface.Concrete;
            // the concrete headwall where the path resumes
            K.MinMaxBox(w, V(-K.Edge - .4f, DrainWater - .8f, far), V(K.Edge + .4f, -.03f, far + .45f), ConcreteGrey);
            K.MinMaxBox(w, V(-K.Edge - .4f, -.03f, far), V(-K.Half, K.Kerb + .02f, far + .45f), KerbCap);
            K.MinMaxBox(w, V(K.Half, -.03f, far), V(K.Edge + .4f, K.Kerb + .02f, far + .45f), KerbCap);
            // the boardwalk deck with the street's cross-section, on jarrah piles
            Boardwalk(d, j0, ramp);
            var spec = new CqRampSpec { Name = "BoardwalkKicker", X = 0f, Z = ramp, Width = 2f * K.Edge, Rise = DrainRise, Height = DrainHeight, Top = DrainTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            d.Gaps.Add(new CqGapSpec { Name = "DrainWater", Z = lip, Length = DrainGap, Width = 0f, Word = "SPLASH", Water = true });
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, JarrahLight, Jarrah, false, DrainWater - .3f);
            deck.Surface = ComicSurface.Wood;
            foreach (int s in new[] { -1, 1 })
            {
                float x = s * (K.Edge + .1f);
                deck.Beam(V(x, 1.1f, ramp), V(x, DrainHeight + 1.1f, ramp + DrainRise), .1f, Jarrah);
                deck.Beam(V(x, DrainHeight + 1.1f, ramp + DrainRise), V(x, DrainHeight + 1.1f, lip - .15f), .1f, Jarrah);
                deck.Beam(V(x, .6f, ramp), V(x, DrainHeight + .6f, ramp + DrainRise), .07f, Jarrah);
                for (float z = ramp; z <= lip + .01f; z += 3f)
                {
                    float hh = DrainHeight * Mathf.Clamp01((z - ramp) / DrainRise);
                    deck.Box(V(x, (hh + 1.15f + DrainWater - .6f) * .5f, Mathf.Min(z, lip - .1f)), V(.16f, hh + 1.15f - (DrainWater - .6f), .16f), TimberDark);
                }
            }
            for (float z = ramp + 1.5f; z <= lip; z += 3f)
                foreach (float x in new[] { -5f, 0f, 5f })
                {
                    float hh = DrainHeight * Mathf.Clamp01((z - ramp) / DrainRise);
                    deck.Box(V(x, (hh + DrainWater - .9f) * .5f - .1f, z), V(.22f, hh - (DrainWater - .9f) - .2f, .22f), TimberDark);
                }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            d.KeepOut.Add(KeepOutBox(d, -1, ramp, far)); d.KeepOut.Add(KeepOutBox(d, 1, ramp, far));
            // paperbarks arching over the channel, bulrush at the water's edge, ducks on the water
            Paperbark(d.Props, V(K.Edge + 3.2f, d.Y(lip - 4f) + .1f, lip - 4.5f), 8f, 2861, false, Mathf.PI * .5f);
            Paperbark(d.Props, V(K.Edge + 4f, d.Y(far + 3f) + .1f, far + 3.2f), 9f, 2862, false, -Mathf.PI * .5f);
            Paperbark(d.Props, V(K.Edge + 12f, d.Y(lip - 4f) - .2f, lip - 4.2f), 7.5f, 2863, false, Mathf.PI * .5f);
            Paperbark(d.Props, V(K.Edge + 14f, d.Y(far + 3f) - .2f, far + 3.4f), 8f, 2864, false, -Mathf.PI * .5f);
            for (int i = 0; i < 6; i++)
            {
                float x = K.Edge + 2f + i * 4.6f;
                Bulrush(d.Road, V(x, DrainWater - .05f, lip - 2.6f + (i % 2) * .5f), 1.1f, 30 + i);
                if (i % 2 == 0) Bulrush(d.Road, V(x + 2f, DrainWater - .05f, far + .9f), 1f, 40 + i);
            }
            DuckArt(d.Props, V(K.Edge + 5f, d.Y(lip + 3f) + DrainWater - .08f, lip + 3f), 80f, 1f);
            DuckArt(d.Props, V(K.Edge + 6.2f, d.Y(lip + 3.6f) + DrainWater - .08f, lip + 3.6f), 95f, .6f);
            DuckArt(d.Props, V(K.Edge + 6.9f, d.Y(lip + 3.2f) + DrainWater - .08f, lip + 3.2f), 100f, .6f);
            d.GiftSlots.Add(new Vector2(0f, 30.5f));
            d.Slot(-2.6f, 36f); d.Slot(2.6f, 36f);
            d.Slot(-2.6f, 5f); d.Slot(2.6f, 5f);
            return d;
        }

        static CqChunkDraft AshPaperbarks()
        {
            // A tunnel of leaning paperbarks over the path, the Roy Hookway viewing platform looking out over a lagoon,
            // an egret hunting in the shallows.
            var d = new CqChunkDraft("Ashfield_Paperbarks", AshDrop);
            SharedPath(d, VergeDry, VergeDry);
            GradeRail(d, 0f, 40f, CharcoalDark, 10f, 30f);
            Industry(d, 0f, 40f, 2851, 2);
            Flats(d, 0f, 40f, 2852, false);
            var r = new System.Random(2853);
            for (float z = 2.5f; z < 40f; z += 6.2f)
            {
                Paperbark(d.Props, V(K.Edge + F(r, 1.6f, 2.6f), d.Ground(z) - .1f, z), F(r, 8f, 10f), 600 + (int)z, false, Mathf.PI + F(r, -.3f, .3f));
                Paperbark(d.Deep, V(K.Edge + F(r, 9f, 15f), d.Y(z) - .3f, z + 2f), F(r, 7f, 9f), 700 + (int)z, true);
                Paperbark(d.Deep, V(K.Edge + F(r, 20f, 34f), d.Y(z) - .4f, z + 3f), F(r, 7f, 10f), 800 + (int)z, true);
            }
            for (float z = 6f; z < 40f; z += 12.4f)
                Paperbark(d.Props, V(-(K.Edge + 1.9f), d.Ground(z) - .1f, z), F(r, 7f, 8.5f), 900 + (int)z, false, F(r, -.4f, .4f));
            ViewingPlatform(d.SlopedFrontage, V(K.Edge + 3.4f, K.Kerb, 21.5f));
            WaderArt(d.Props, V(K.Edge + 10f, d.Y(22f) - .62f, 22f), -60f, 1.1f, 0);
            d.Walls.Add(new Bounds(new Vector3(K.Edge + 3.4f, d.Y(21.5f) + 1f, 21.5f), new Vector3(5f, 4f, 5f)));
            PathFurniture(d, 33f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft AshExit()
        {
            // The flats close in behind a last stand of paperbarks; hedges, lawn and gums towards z = 40.
            var d = new CqChunkDraft("Ashfield_Exit", AshDrop);
            SharedPath(d, VergeDry, Lawn);
            GradeRail(d, 0f, 40f, CharcoalDark, 6f, 26f);
            Hedge(d.SlopedFrontage, -1, 26f, 39.6f, 1.1f, 2.1f, 1.3f);
            Industry(d, 0f, 40f, 2861, 1);
            SideGround(d, 1, 0, 40, 62f, Lawn, ComicSurface.Foliage);
            Paperbark(d.Props, V(K.Edge + 3f, d.Ground(4f) - .1f, 4f), 9f, 951);
            Paperbark(d.Props, V(K.Edge + 6f, d.Ground(12f) - .1f, 12f), 8f, 952);
            Paperbark(d.Deep, V(K.Edge + 16f, d.Ground(8f), 8f), 9f, 953, true);
            Gum(d.Props, V(K.Edge + 5f, d.Ground(24f), 24f), 13f, 291);
            Hedge(d.SlopedFrontage, 1, 26f, 39.6f, .5f, 1.6f, 1.2f);
            Gum(d.Props, V(K.Edge + 6f, d.Ground(35f), 35f), 11f, 292);
            WallBox(d, K.Edge + 1f, K.Edge + 40f, 0f, 40f, 7f);
            DeepSuburb(d, 1, 0f, 40f, 2862, 26f);
            PathFurniture(d, 14f);
            d.StandardSlots();
            return d;
        }

        // ---------------------------------------------------------------- Ashfield parts

        /// <summary>A path light and a distance marker on the right verge (thin colliders).</summary>
        static void PathFurniture(CqChunkDraft d, float z)
        {
            Vector3 p = V(K.Edge - .6f, d.Ground(z), z);
            ComicMesh m = d.Props;
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, p, p + V(0, 5.2f, 0), .09f, .06f, Galv, 6);
            m.Beam(p + V(0, 5.2f, 0), p + V(-1.4f, 5.5f, 0), .07f, Galv);
            m.Box(p + V(-1.6f, 5.48f, 0), V(.6f, .1f, .26f), GalvDark);
            m.Box(p + V(0, .1f, 0), V(.3f, .2f, .3f), K.Hex("C9C5BA"));
            K.Thin(d.Collisions, "PathLight", p, 4f, .3f);
        }

        /// <summary>The reserve: mown stripes on the lawn, a concrete path wandering off, a timber post-and-rail edge.</summary>
        static void ReserveLawn(CqChunkDraft d, float z0, float z1, int seed)
        {
            ComicMesh g = d.Road;
            for (float z = z0 + 1f; z < z1 - 2.5f; z += 6f) Ground(g, K.Edge + .3f, K.Edge + 40f, z, z + 3f, K.Kerb + .06f, LawnLight, ComicSurface.Foliage);
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Wood;
            for (float z = z0 + 1f; z < z1; z += 2.5f) m.Box(V(K.Edge + .5f, K.Kerb + .3f, z), V(.18f, .6f, .18f), K.Hex("8E8471"));
            for (float z = z0; z < z1 - .01f; z += 8f) m.Box(V(K.Edge + .5f, K.Kerb + .5f, (z + Mathf.Min(z1, z + 8f)) * .5f), V(.08f, .1f, Mathf.Min(z1, z + 8f) - z), K.Hex("8E8471"));
            WallBox(d, K.Edge + .4f, K.Edge + 40f, z0, z1, 5f);
        }

        /// <summary>The little station shelter: a shallow barrel-vault roof in cream sheet on maroon posts, a cream block
        /// kiosk and a glass windbreak under it, and a maroon totem with the station name on a board. Along z from p.</summary>
        static void StationShelter(ComicMesh m, Vector3 p, float len, string name)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { .4f, len * .5f, len - .4f })
                foreach (float x in new[] { -1.2f, 1.2f })
                    m.Box(p + V(x, 1.35f, z), V(.12f, 2.7f, .12f), Maroon);
            m.Box(p + V(-1.2f, 2.72f, len * .5f), V(.14f, .14f, len), Maroon);
            m.Box(p + V(1.2f, 2.72f, len * .5f), V(.14f, .14f, len), Maroon);
            // the vault: five facets
            m.Surface = ComicSurface.Metal;
            const int n = 5;
            for (int i = 0; i < n; i++)
            {
                float t0 = Mathf.PI * (.1f + .8f * i / n), t1 = Mathf.PI * (.1f + .8f * (i + 1) / n);
                Vector3 a = p + V(-Mathf.Cos(t0) * 1.9f, 2.5f + Mathf.Sin(t0) * .8f, -.3f), b = p + V(-Mathf.Cos(t1) * 1.9f, 2.5f + Mathf.Sin(t1) * .8f, -.3f);
                K.Face(m, a, b, b + V(0, 0, len + .6f), a + V(0, 0, len + .6f), i % 2 == 0 ? ShelterCream : Color.Lerp(ShelterCream, White, .5f), Vector3.up);
                K.Face(m, a + V(0, -.07f, 0), b + V(0, -.07f, 0), b + V(0, -.07f, len + .6f), a + V(0, -.07f, len + .6f), Color.Lerp(ShelterCream, K.R.ink, .3f), Vector3.down);
            }
            m.Surface = ComicSurface.Plaster;
            m.Box(p + V(0, 1.15f, 1.5f), V(1.9f, 2.3f, 1.8f), K.Hex("EDE6D0"));
            m.Surface = ComicSurface.PaintedMetal;
            K.Face(m, p + V(.96f, .1f, 1.1f), p + V(.96f, .1f, 1.9f), p + V(.96f, 2f, 1.9f), p + V(.96f, 2f, 1.1f), Maroon, Vector3.right);
            m.Surface = ComicSurface.Glass;
            m.Box(p + V(-.9f, 1.3f, len * .7f), V(.06f, 1.9f, len * .4f), K.Hex("BFDCE3"));
            // totem and name board
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(1.1f, 1.5f, len + 1.4f), V(.5f, 3f, .2f), Maroon);
            m.Box(p + V(1.1f, 2.7f, len + 1.4f), V(.52f, .5f, .22f), MaroonLight);
            m.Box(p + V(1.26f, 2.0f, len * .5f), V(.06f, .55f, 3.4f), Maroon);
            TextFacingX(m, name, p + V(1.3f, 2.0f, len * .5f), 1, .06f, White);
        }

        /// <summary>The footbridge at z: a galvanised deck across both tracks and Railway Parade's fence, two flights of
        /// ramp folding back along the path side (grey mesh balustrades, yellow handrails), and a ramp down to the
        /// platform end. Flat road space.</summary>
        static void Footbridge(ComicMesh m, float z)
        {
            const float top = 6.3f, mid = 3.3f;
            Color deck = K.Hex("B4BABF"), yellow = HazardYellow;
            void Flight(Vector3 a, Vector3 b, float width)
            {
                Vector3 side = Vector3.Cross(Vector3.up, (b - a).normalized);
                m.Surface = ComicSurface.Metal;
                Slab(m, a, b, side, width, width, .22f, deck, GalvDark);
                foreach (int s in new[] { -1, 1 })
                {
                    m.Surface = ComicSurface.Fabric;
                    Slab(m, a + side * s * (width * .5f) + V(0, .62f, 0), b + side * s * (width * .5f) + V(0, .62f, 0), Vector3.up, 1f, 1f, .04f, K.Hex("9AA2A8"), K.Hex("9AA2A8"));
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Beam(a + side * s * (width * .5f) + V(0, 1.15f, 0), b + side * s * (width * .5f) + V(0, 1.15f, 0), .07f, yellow);
                }
            }
            void Post(float x, float zz, float h) { m.Surface = ComicSurface.Metal; m.Box(V(x, h * .5f - .5f, zz), V(.2f, h + 1f, .2f), Galv); }
            // the span over the tracks (x from the path side to beyond the far fence)
            Flight(V(-10.4f, top, z), V(-28.5f, top, z), 2f);
            foreach (float x in new[] { -12.6f, -19f, -27.6f }) { Post(x, z - .9f, top); Post(x, z + .9f, top); }
            // two flights along the path side: top landing at (z) down to the mid landing at z - 20, back to the ground at z
            Flight(V(-9.6f, top, z - 1.2f), V(-9.6f, mid, z - 20f), 1.3f);
            m.Surface = ComicSurface.Metal;
            m.Box(V(-10.4f, mid - .11f, z - 20.8f), V(3f, .22f, 1.6f), deck);
            m.Box(V(-10f, top - .11f, z - .1f), V(2.4f, .22f, 2.2f), deck);
            Flight(V(-11.2f, mid, z - 20f), V(-11.2f, K.Kerb + .1f, z - 1.5f), 1.3f);
            foreach (float zz in new[] { z - 6f, z - 12f, z - 20.6f }) Post(-10.4f, zz, Mathf.Lerp(top, mid, Mathf.InverseLerp(z - 1.2f, z - 20f, zz)) - .2f);
            Post(-11.2f, z - 14f, 2f);
            // stairs down to the platform
            Flight(V(-19f, top, z - 1f), V(-19f, AshBallastY + 1.1f, z - 9.5f), 1.4f);
        }

        /// <summary>The floodplain on the right from the verge out to x = 70: ground falling gently to the flats, samphire
        /// mats in rust, olive and maroon, pale pools edged with sedge and bulrush, samphire clumps, paperbarks behind.</summary>
        static void Flats(CqChunkDraft d, float z0, float z1, int seed, bool open)
        {
            if (z1 - z0 < 1f) return;
            var r = new System.Random(seed);
            ComicMesh g = d.Road;
            Color[] mats = { Samphire1, Samphire3, Mud, Samphire2, Samphire3, Samphire1, SamphirePink, Samphire3, Mud };
            Terrain(g, K.Edge, 70f, z0, z1, 2.3f, 2.2f, FlatsH, (x, z, y) =>
            {
                if (x < K.Edge + 4f) return Color.Lerp(VergeDry, Samphire3, Mathf.InverseLerp(K.Edge, K.Edge + 4f, x));
                int k = Mathf.Abs(Mathf.FloorToInt(x * .19f + Mathf.Sin(z * .23f) * 1.4f) * 7 + Mathf.FloorToInt(z * .14f + Mathf.Cos(x * .17f)) * 3 + seed) % mats.Length;
                return mats[k];
            }, ComicSurface.Foliage);
            // pools
            int pools = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / 13f));
            for (int i = 0; i < pools; i++)
            {
                float zc = Mathf.Lerp(z0, z1, (i + .5f) / pools) + F(r, -2f, 2f), xc = K.Edge + F(r, 10f, 17f) + (i % 2) * 14f, rx = F(r, 3.5f, 6f), rz = Mathf.Min(F(r, 3f, 5f), (z1 - z0) * .45f);
                Pool(g, V(xc, -.66f, zc), rx, rz, PoolBlue, PoolLight, seed + i);
                for (int k = 0; k < 7; k++)
                {
                    float a = k * .9f + F(r, 0f, .4f);
                    Vector3 p = V(xc + Mathf.Cos(a) * (rx + .3f), -.7f, Mathf.Clamp(zc + Mathf.Sin(a) * (rz + .3f), z0 + .3f, z1 - .3f));
                    if (k % 3 == 0) Bulrush(g, p, F(r, .9f, 1.2f), seed + i * 10 + k);
                    else Sedge(g, p, F(r, .8f, 1.2f), seed + i * 10 + k);
                }
            }
            // samphire clumps and sedge near the path, a paperbark line further out
            for (float z = z0 + .6f; z < z1 - .5f; z += F(r, .9f, 1.6f))
            {
                float x = K.Edge + F(r, 1.6f, 22f);
                if (r.Next(3) == 0) Sedge(g, V(x, FlatsH(x, z) - .05f, z), F(r, .6f, 1.1f), seed + (int)(z * 7), K.Hex("8FA65A"), K.Hex("B4C070"));
                else SamphireClump(g, V(x, FlatsH(x, z) - .05f, z), F(r, .7f, 1.4f), seed + (int)(z * 5));
            }
            for (float z = z0 + 2f; z < z1 - 1f; z += F(r, 5f, 8f))
            {
                float x = K.Edge + F(r, open ? 34f : 22f, 58f);
                Paperbark(d.Deep, V(x, d.Y(z) - .9f, z), F(r, 7f, 10f), seed + (int)z, true);
                if (r.Next(2) == 0) DeadTree(d.Deep, V(x - F(r, 4f, 9f), d.Y(z) - .8f, z + 1f), F(r, 4f, 7f), seed + (int)z);
            }
            WallBox(d, K.Edge + .6f, K.Edge + 40f, z0, z1, 5f);
            // post-and-wire fence keeping people off the saltmarsh
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Wood;
            for (float z = z0 + .6f; z < z1; z += 3f) m.Box(V(K.Edge + .45f, K.Kerb + .4f, z), V(.12f, .9f, .12f), K.Hex("8E8471"));
            m.Surface = ComicSurface.Metal;
            for (float z = z0; z < z1 - .01f; z += 8f)
                foreach (float y in new[] { .45f, .75f }) m.Box(V(K.Edge + .45f, K.Kerb + y, (z + Mathf.Min(z1, z + 8f)) * .5f), V(.03f, .03f, Mathf.Min(z1, z + 8f) - z), K.Hex("6F6A60"));
        }

        static float FlatsH(float x, float z) => Mathf.Lerp(K.Kerb + .02f, -.72f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(K.Edge, K.Edge + 7f, x))) + .05f * Mathf.Sin(x * .9f + z * .6f) * Mathf.InverseLerp(K.Edge, K.Edge + 3f, x);

        /// <summary>A shallow pool: an irregular 10-sided patch of pale water with a lighter centre and a muddy rim.</summary>
        static void Pool(ComicMesh m, Vector3 c, float rx, float rz, Color water, Color light, int seed)
        {
            var r = new System.Random(seed * 23 + 5);
            m.Surface = ComicSurface.Plain;
            const int n = 10;
            var rim = new Vector3[n];
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2f / n, k = F(r, .82f, 1.12f); rim[i] = c + V(Mathf.Cos(a) * rx * k, 0, Mathf.Sin(a) * rz * k); }
            for (int i = 0; i < n; i++)
            {
                Vector3 a = rim[i], b = rim[(i + 1) % n];
                K.TriFacing(m, c + V(0, .02f, 0), a + V(0, .02f, 0), b + V(0, .02f, 0), water, Vector3.up);
                K.TriFacing(m, c + V(0, .03f, 0), Vector3.Lerp(c, a, .45f) + V(0, .03f, 0), Vector3.Lerp(c, b, .45f) + V(0, .03f, 0), light, Vector3.up);
                K.Face(m, a + V(0, .01f, 0), b + V(0, .01f, 0), c + (b - c) * 1.18f + V(0, .01f, 0), c + (a - c) * 1.18f + V(0, .01f, 0), Mud, Vector3.up, false);
            }
        }

        /// <summary>A samphire clump: a low rusty-red and olive cushion with a few upright fleshy stems.</summary>
        static void SamphireClump(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = new System.Random(seed * 41 + 7);
            Color[] tones = { Samphire2, SamphirePink, Samphire3, K.Hex("9A5A44"), K.Hex("7F8A4C") };
            Color c = tones[r.Next(tones.Length)];
            Blob(m, p + V(0, s * .14f, 0), V(s * 1.4f, s * .4f, s * 1.2f), c, 6, 4, F(r, 0f, 3f));
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < 3; i++)
            {
                float a = F(r, 0f, 6.28f);
                Vector3 b = p + V(Mathf.Cos(a) * s * .3f, s * .1f, Mathf.Sin(a) * s * .3f);
                Rod(m, b, b + V(Mathf.Cos(a) * .08f, s * F(r, .3f, .5f), Mathf.Sin(a) * .08f), .05f * s, .03f * s, Color.Lerp(c, SamphirePink, .5f), 3);
            }
        }

        /// <summary>The boardwalk (z0..z1) with the street's cross-section: jarrah boards, fascias, piles, and timber rails
        /// on both sides.</summary>
        static void Boardwalk(CqChunkDraft d, float z0, float z1)
        {
            ComicMesh m = d.Road;
            Color board = Color.Lerp(Jarrah, K.R.ink, .3f), walk = Color.Lerp(JarrahLight, K.R.cream, .12f);
            for (float z = z0; z < z1 - .01f; z += 1f)
            {
                float e = Mathf.Min(z1, z + 1f);
                m.Surface = ComicSurface.Wood;
                K.Face(m, V(-K.Half, 0f, z), V(-K.Half, 0f, e), V(K.Half, 0f, e), V(K.Half, 0f, z), JarrahLight, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    K.Face(m, V(s * K.Half, K.Kerb, z), V(s * K.Half, K.Kerb, e), V(s * K.Edge, K.Kerb, e), V(s * K.Edge, K.Kerb, z), walk, Vector3.up, false);
                    K.Face(m, V(s * K.Half, -.05f, z), V(s * K.Half, -.05f, e), V(s * K.Half, K.Kerb, e), V(s * K.Half, K.Kerb, z), Jarrah, new Vector3(-s, 0, 0), false);
                    K.Face(m, V(s * K.Edge, -.6f, z), V(s * K.Edge, -.6f, e), V(s * K.Edge, K.Kerb, e), V(s * K.Edge, K.Kerb, z), Jarrah, new Vector3(s, 0, 0), false);
                }
                m.Surface = ComicSurface.Plain;
                K.Face(m, V(-K.Edge, K.Kerb + .012f, z + .45f), V(-K.Edge, K.Kerb + .012f, z + .5f), V(-K.Half, K.Kerb + .012f, z + .5f), V(-K.Half, K.Kerb + .012f, z + .45f), board, Vector3.up, false);
                K.Face(m, V(K.Half, K.Kerb + .012f, z + .45f), V(K.Half, K.Kerb + .012f, z + .5f), V(K.Edge, K.Kerb + .012f, z + .5f), V(K.Edge, K.Kerb + .012f, z + .45f), board, Vector3.up, false);
                K.Face(m, V(-K.Half, .012f, z + .45f), V(-K.Half, .012f, z + .5f), V(K.Half, .012f, z + .5f), V(K.Half, .012f, z + .45f), board, Vector3.up, false);
            }
            // the start of the deck (facing the rider) and the ground under it
            K.Face(m, V(-K.Edge, -.7f, z0), V(K.Edge, -.7f, z0), V(K.Edge, 0f, z0), V(-K.Edge, 0f, z0), Jarrah, Vector3.back, false);
            Ground(m, -AshRailX, K.Edge + .5f, z0, z1, -.72f, Mud);
            ComicMesh p = d.SlopedFrontage;
            p.Surface = ComicSurface.Wood;
            for (float z = z0 + 1f; z < z1; z += 3f)
                foreach (float x in new[] { -K.Edge + .3f, -2.6f, 2.6f, K.Edge - .3f })
                    p.Box(V(x, -.9f, z), V(.22f, 1.5f, .22f), TimberDark);
            BoardwalkRails(p, z0, z1, K.Kerb);
        }

        /// <summary>Timber post-and-rail balustrades along both edges of the boardwalk.</summary>
        static void BoardwalkRails(ComicMesh p, float z0, float z1, float y)
        {
            p.Surface = ComicSurface.Wood;
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * (K.Edge + .1f);
                p.Box(V(x, y + 1.02f, (z0 + z1) * .5f), V(.14f, .1f, z1 - z0), Jarrah);
                p.Box(V(x, y + .55f, (z0 + z1) * .5f), V(.07f, .08f, z1 - z0), Jarrah);
                for (float z = z0 + .3f; z < z1; z += 2f) p.Box(V(x, y + .5f, z), V(.14f, 1.05f, .14f), TimberDark);
            }
        }

        /// <summary>The viewing platform: a square jarrah deck on piles stepping out over the flats, rails on three sides,
        /// a bench, two steps up from the verge.</summary>
        static void ViewingPlatform(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Wood;
            m.Box(p + V(0, .45f, 0), V(4.4f, .16f, 4.4f), JarrahLight);
            m.Box(p + V(0, .34f, 0), V(4.5f, .12f, 4.5f), Jarrah);
            for (int i = 0; i < 8; i++) K.Face(m, p + V(-2.2f + i * .55f, .535f, -2.2f), p + V(-2.15f + i * .55f, .535f, -2.2f), p + V(-2.15f + i * .55f, .535f, 2.2f), p + V(-2.2f + i * .55f, .535f, 2.2f), Jarrah, Vector3.up, false);
            foreach (float x in new[] { -2f, 2f }) foreach (float z in new[] { -2f, 2f }) m.Box(p + V(x, -.3f, z), V(.22f, 1.5f, .22f), TimberDark);
            m.Box(p + V(-2.5f, .12f, 0), V(.6f, .24f, 2f), Jarrah);
            m.Box(p + V(-2.25f, .3f, 0), V(.3f, .14f, 2f), JarrahLight);
            foreach ((Vector3 a, Vector3 b) in new[] { (V(-2.1f, 0, 2.1f), V(2.1f, 0, 2.1f)), (V(2.1f, 0, 2.1f), V(2.1f, 0, -2.1f)), (V(2.1f, 0, -2.1f), V(-2.1f, 0, -2.1f)) })
            {
                m.Beam(p + a + V(0, 1.5f, 0), p + b + V(0, 1.5f, 0), .1f, Jarrah);
                m.Beam(p + a + V(0, 1.05f, 0), p + b + V(0, 1.05f, 0), .06f, Jarrah);
                m.Box(p + a + V(0, 1f, 0), V(.14f, 1.1f, .14f), TimberDark);
                m.Box(p + (a + b) * .5f + V(0, 1f, 0), V(.12f, 1.1f, .12f), TimberDark);
            }
            m.Box(p + V(2.1f, 1f, -2.1f) + V(-4.2f, 0, 0), V(.14f, 1.1f, .14f), TimberDark);
            m.Box(p + V(1.2f, .95f, 0), V(.45f, .08f, 1.8f), Timber);
            foreach (float z in new[] { -.7f, .7f }) m.Box(p + V(1.2f, .72f, z), V(.4f, .4f, .08f), Charcoal);
        }

        /// <summary>A maroon-framed interpretive sign on two posts, angled like a lectern (a map of the flats in greens and blue).</summary>
        static void InterpretiveSign(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0), tilt = q * Quaternion.Euler(-35f, 0, 0);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.5f, .5f }) m.Box(p + q * V(x, .5f, 0), V(.08f, 1f, .08f), Maroon, q);
            m.Box(p + q * V(0, 1.05f, 0), V(1.4f, .08f, .9f), Maroon, tilt);
            m.Box(p + q * V(0, 1.05f, 0) + tilt * V(0, .045f, 0), V(1.26f, .02f, .76f), K.Hex("E9E3D0"), tilt);
            m.Box(p + q * V(-.25f, 1.05f, 0) + tilt * V(0, .06f, .05f), V(.6f, .02f, .5f), K.Hex("7FA652"), tilt);
            m.Box(p + q * V(.3f, 1.05f, 0) + tilt * V(0, .06f, -.05f), V(.3f, .02f, .55f), PoolBlue, tilt);
        }

        /// <summary>A late-1950s State Housing cottage: fibro or weatherboard on brick stumps, a low tiled hip roof, a small
        /// porch with a flat roof on two pipe posts, steel-framed windows, no front fence (style 1: a gable-fronted room).</summary>
        static void StateHouse(ComicMesh f, CqLot l, Color wall, Color roof, int style)
        {
            float W = l.W, D = l.Depth, b = .5f, h = b + 2.7f;
            f.Surface = ComicSurface.Brick;
            K.LBox(f, l, .1f, W - .1f, -2.6f, b, -.05f, -D + .05f, RedBrickDark);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, 0, W, b, h, 0f, -D, wall);
            f.Surface = ComicSurface.Plain;
            // cover battens on the fibro sheets
            for (float a = 1.2f; a < W - .5f; a += 1.2f) K.LQuad(f, l, a - .03f, a + .03f, b, h, .012f, Color.Lerp(wall, K.R.ink, .2f), false);
            float door = W * .42f;
            f.Surface = ComicSurface.PaintedMetal;
            K.LQuad(f, l, door - .5f, door + .5f, b, b + 2.15f, .02f, K.R.ink);
            f.Surface = ComicSurface.Wood;
            K.LQuad(f, l, door - .4f, door + .4f, b + .04f, b + 2.05f, .035f, K.Hex("4F7F86"));
            FramedWindow(f, l, W * .16f, 1.3f, b + .9f, b + 2.1f, .02f, White, true, false, true);
            FramedWindow(f, l, W * .78f, 2f, b + .9f, b + 2.1f, .02f, White, true, false, true);
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, -.35f, W + .35f, h - .05f, h + .08f, .4f, -D - .35f, K.R.ink);
            f.Surface = ComicSurface.Brick;
            if (style == 1)
            {
                HipRoof(f, l.P(W * .5f, h + .08f, -D * .5f), D + .9f, W + .9f, 1.7f, roof);
                float g0 = W * .58f, g1 = W - .1f, gr = h + (g1 - g0) * .3f;
                f.Surface = ComicSurface.Plaster;
                K.LBox(f, l, g0, g1, b, h, 1.1f, 0f, wall);
                f.Surface = ComicSurface.Brick;
                K.LBox(f, l, g0 + .1f, g1 - .1f, -2.6f, b, 1.05f, 0f, RedBrickDark);
                FramedWindow(f, l, (g0 + g1) * .5f, 1.8f, b + .9f, b + 2.1f, 1.12f, White, true, false, true);
                K.LGable(f, l, g0 - .3f, g1 + .3f, h, gr, 1.45f, -2f, roof);
                f.Surface = ComicSurface.Plaster;
                K.TriFacing(f, l.P(g0, h + .02f, 1.47f), l.P(g1, h + .02f, 1.47f), l.P((g0 + g1) * .5f, gr - .25f, 1.47f), Color.Lerp(wall, White, .4f), new Vector3(-l.S, 0, 0));
            }
            else HipRoof(f, l.P(W * .5f, h + .08f, -D * .5f), D + .9f, W + .9f, 1.8f, roof);
            // porch: concrete step, flat roof, two pipe posts
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, door - 1.1f, door + 1.1f, 0f, b, 1.2f, 0f, K.Hex("C9C5BA"));
            K.LBox(f, l, door - .8f, door + .8f, 0f, b * .5f, 1.6f, 1.2f, K.Hex("C9C5BA"));
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, door - 1.2f, door + 1.2f, h - .45f, h - .33f, 1.3f, 0f, White);
            foreach (float a in new[] { door - 1.05f, door + 1.05f }) K.LBox(f, l, a - .04f, a + .04f, b, h - .45f, 1.2f, 1.12f, White);
            Chimney(f, l.P(W * .3f, h + .5f, -D * .6f), 1.5f, RedBrick);
            // a letterbox and a shrub
            f.Surface = ComicSurface.PaintedMetal;
            float fo = l.Line - K.Edge - .9f;
            K.LBox(f, l, door + 1.6f, door + 1.68f, 0f, .9f, fo, fo - .08f, White);
            K.LBox(f, l, door + 1.45f, door + 1.83f, .9f, 1.15f, fo + .12f, fo - .2f, K.Hex("4F7F86"));
            Bush(f, l.P(W * .9f, 0f, 1.6f), 1f, K.Hex("5F8A45"), K.Hex("D9A0B8"));
        }

        /// <summary>A Hills Hoist rotary clothes line with washing on it.</summary>
        static void HillsHoist(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Metal;
            Rod(m, p, p + V(0, 2f, 0), .05f, .04f, Galv, 5);
            Color[] wash = { White, K.Hex("E2463A"), K.Hex("2F6FB0"), K.Hex("F6C543") };
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * .5f + .4f, b = a + Mathf.PI * .5f;
                Vector3 da = V(Mathf.Cos(a), 0, Mathf.Sin(a)), db = V(Mathf.Cos(b), 0, Mathf.Sin(b));
                m.Surface = ComicSurface.Metal;
                m.Beam(p + V(0, 1.8f, 0), p + da * 1.5f + V(0, 2.1f, 0), .04f, Galv);
                m.Beam(p + da * 1.5f + V(0, 2.1f, 0), p + db * 1.5f + V(0, 2.1f, 0), .025f, GalvDark);
                m.Beam(p + da * .9f + V(0, 1.98f, 0), p + db * .9f + V(0, 1.98f, 0), .025f, GalvDark);
                m.Surface = ComicSurface.Fabric;
                Vector3 mid = p + (da + db) * .75f + V(0, 2.08f, 0), along = (db - da).normalized;
                Slab(m, mid - along * .45f + V(0, -.35f, 0), mid + along * .45f + V(0, -.35f, 0), Vector3.up, .7f, .7f, .03f, wash[i], wash[i]);
            }
        }

        /// <summary>The light industry across Railway Parade (decoration, no colliders): kind 0 the long low cream factory
        /// with continuous bands of steel-framed windows; 1 grey and green steel sheds; 2 sheds, stacked concrete products
        /// and a forklift; 3 a container stack and a shed. The nearest pieces are outlined (Landmark), the rest are Deep.</summary>
        static void Industry(CqChunkDraft d, float z0, float z1, int seed, int kind)
        {
            var r = new System.Random(seed);
            float x0 = -44f;
            // verge trees and parked cars along Railway Parade
            for (float z = z0 + F(r, 3f, 8f); z < z1 - 2f; z += F(r, 13f, 19f)) Gum(d.Deep, V(-37.5f + F(r, -1f, 1f), d.Ground(z), z), F(r, 8f, 12f), seed + (int)z, true);
            Color[] cars = { K.Hex("C9CED3"), K.Hex("C8463A"), K.Hex("2F6FB0"), K.Hex("F5F3EC"), K.Hex("3A4047") };
            for (float z = z0 + 3f; z < z1 - 4f; z += F(r, 6f, 11f)) ParkedCar(d.Deep, V(-28.2f, d.Ground(z) - .02f, z), 0f, cars[r.Next(cars.Length)]);
            switch (kind)
            {
                case 0:
                    Factory(d.Landmark, V(x0 - 9f, d.Ground((z0 + z1) * .5f), (z0 + z1) * .5f), z1 - z0 - 3f, 18f, 0);
                    break;
                case 1:
                    Factory(d.Landmark, V(x0 - 7f, d.Ground(z0 + 10f), z0 + 10f), 16f, 14f, 1);
                    Factory(d.Deep, V(x0 - 8f, d.Ground(z0 + 29f), z0 + 29f), 15f, 16f, 2);
                    break;
                case 2:
                    Factory(d.Landmark, V(x0 - 8f, d.Ground(z0 + 28f), z0 + 28f), 18f, 16f, 2);
                    for (int i = 0; i < 5; i++) PaverStack(d.Landmark, V(x0 + 2f - (i % 2) * 2.6f, d.Ground(z0 + 4f + i * 2.4f), z0 + 4f + i * 2.4f), 1 + i % 3);
                    Forklift(d.Landmark, V(x0 + 4f, d.Ground(z0 + 15f), z0 + 15f), 200f);
                    break;
                default:
                    ContainerStack(d.Landmark, V(x0 - 2f, d.Ground(z0 + 9f), z0 + 9f), 3);
                    Factory(d.Landmark, V(x0 - 8f, d.Ground(z0 + 29f), z0 + 29f), 17f, 15f, 1);
                    break;
            }
            // a second row of roofs and a couple of stacks and tanks on the skyline (unbranded)
            for (float z = z0 + 2f; z < z1 - 8f; z += F(r, 15f, 20f))
            {
                float w = F(r, 16f, 22f), len = Mathf.Min(F(r, 12f, 16f), z1 - z - 1f), h = F(r, 7f, 10f), x = x0 - 30f - F(r, 0f, 6f), zc = z + len * .5f;
                Color shed = new[] { K.Hex("C9CDD1"), ShedGreen, FactoryCream, K.Hex("9AA7B0") }[r.Next(4)];
                d.Deep.Surface = ComicSurface.Metal;
                d.Deep.Box(V(x, d.Ground(zc) + h * .5f - 1f, zc), V(w, h + 2f, len), shed);
                GableRoofX(d.Deep, V(x, d.Ground(zc) + h, zc), w + .6f, len + .6f, 1.8f, Zinc, shed);
            }
            if (kind == 1 || kind == 3)
            {
                Vector3 t = V(x0 - 20f, d.Ground(z0 + 20f), z0 + 20f);
                d.Deep.Surface = ComicSurface.Metal;
                d.Deep.Cylinder(t + V(0, 4.5f, 0), 2.6f, 9f, K.Hex("D3D7DA"), 8);
                d.Deep.Cylinder(t + V(0, 9.4f, 0), 2.6f, .9f, K.Hex("B9BEC2"), 8, .2f);
                d.Deep.Cylinder(t + V(5.6f, 3.5f, 1f), 2.2f, 7f, K.Hex("C9CDD1"), 8);
            }
        }

        /// <summary>An industrial building centred on p, len along z, depth along x, facing the road (+X). kind 0: the long
        /// low cream factory (continuous window bands, low-pitch roof, a loading awning); 1: a grey steel shed with a
        /// roller door and a skillion office; 2: a green shed with a sawtooth-free gable and a ridge vent.</summary>
        static void Factory(ComicMesh m, Vector3 p, float len, float depth, int kind)
        {
            float h = kind == 0 ? 5.6f : 7f, fx = depth * .5f;
            Color wall = kind == 0 ? FactoryCream : kind == 1 ? ShedGrey : ShedGreen, trim = Color.Lerp(wall, K.R.ink, .25f);
            m.Surface = kind == 0 ? ComicSurface.Plaster : ComicSurface.Metal;
            m.Box(p + V(0, h * .5f - 1f, 0), V(depth, h + 2f, len), wall);
            GableRoofX(m, p + V(0, h, 0), depth + .8f, len + .8f, kind == 0 ? 1.3f : 2.2f, kind == 2 ? K.Hex("9FB3A6") : Zinc, wall);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, h + .02f, 0), V(depth + .9f, .14f, len + .9f), K.R.ink);
            if (kind == 0)
            {
                // two continuous bands of small-paned steel windows, pilasters between bays, a brick plinth
                m.Surface = ComicSurface.Brick;
                m.Box(p + V(fx + .05f, .45f, 0), V(.12f, 1.1f, len), RedBrickDark);
                int bays = Mathf.Max(3, Mathf.RoundToInt(len / 5.2f));
                for (int i = 0; i < bays; i++)
                {
                    float za = -len * .5f + len * i / bays + .5f, zb = -len * .5f + len * (i + 1) / bays - .5f;
                    m.Surface = ComicSurface.PaintedMetal;
                    K.Face(m, p + V(fx + .02f, 1.6f, za), p + V(fx + .02f, 1.6f, zb), p + V(fx + .02f, 4.4f, zb), p + V(fx + .02f, 4.4f, za), K.R.ink, Vector3.right);
                    m.Surface = ComicSurface.Glass;
                    K.Face(m, p + V(fx + .04f, 1.72f, za + .12f), p + V(fx + .04f, 1.72f, zb - .12f), p + V(fx + .04f, 4.28f, zb - .12f), p + V(fx + .04f, 4.28f, za + .12f), K.Hex("7FA6B4"), Vector3.right);
                    m.Surface = ComicSurface.PaintedMetal;
                    for (float y = 2.35f; y < 4.2f; y += .65f) K.Face(m, p + V(fx + .05f, y, za + .12f), p + V(fx + .05f, y, zb - .12f), p + V(fx + .05f, y + .05f, zb - .12f), p + V(fx + .05f, y + .05f, za + .12f), K.R.ink, Vector3.right, false);
                    for (float z = za + .9f; z < zb - .3f; z += .8f) K.Face(m, p + V(fx + .05f, 1.72f, z), p + V(fx + .05f, 1.72f, z + .05f), p + V(fx + .05f, 4.28f, z + .05f), p + V(fx + .05f, 4.28f, z), K.R.ink, Vector3.right, false);
                    m.Surface = ComicSurface.Plaster;
                    m.Box(p + V(fx + .1f, h * .5f, zb + .5f), V(.25f, h, .5f), trim);
                }
                m.Surface = ComicSurface.Metal;
                m.Box(p + V(fx + 1.6f, 3.6f, -len * .32f), V(3.2f, .2f, 6f), Zinc);
                foreach (float z in new[] { -len * .32f - 2.8f, -len * .32f + 2.8f }) m.Box(p + V(fx + 3f, 1.75f, z), V(.14f, 3.5f, .14f), GalvDark);
                // a water tank on a stand at the far end
                m.Cylinder(p + V(fx - 3f, h + 3.2f, len * .5f - 3f), 1.6f, 2.4f, K.Hex("B9BEC2"), 8);
                foreach (float dx in new[] { -1f, 1f }) foreach (float dz in new[] { -1f, 1f }) m.Box(p + V(fx - 3f + dx, h + 1f, len * .5f - 3f + dz), V(.14f, 2.2f, .14f), GalvDark);
                return;
            }
            // sheds: ribbed sheeting lines, a big roller door, a personnel door, a skillion office or a ridge vent
            m.Surface = ComicSurface.Plain;
            for (float z = -len * .5f + .8f; z < len * .5f - .2f; z += 1.2f)
                K.Face(m, p + V(fx + .015f, 0f, z), p + V(fx + .015f, 0f, z + .06f), p + V(fx + .015f, h - .1f, z + .06f), p + V(fx + .015f, h - .1f, z), trim, Vector3.right, false);
            m.Surface = ComicSurface.PaintedMetal;
            float dz0 = -len * .28f, dw = 4.6f;
            K.Face(m, p + V(fx + .03f, 0f, dz0 - dw * .5f), p + V(fx + .03f, 0f, dz0 + dw * .5f), p + V(fx + .03f, 4.8f, dz0 + dw * .5f), p + V(fx + .03f, 4.8f, dz0 - dw * .5f), K.R.ink, Vector3.right);
            m.Surface = ComicSurface.Metal;
            K.Face(m, p + V(fx + .05f, 0f, dz0 - dw * .5f + .15f), p + V(fx + .05f, 0f, dz0 + dw * .5f - .15f), p + V(fx + .05f, 4.65f, dz0 + dw * .5f - .15f), p + V(fx + .05f, 4.65f, dz0 - dw * .5f + .15f), kind == 1 ? K.Hex("D9A441") : K.Hex("C9CDD1"), Vector3.right);
            m.Surface = ComicSurface.Plain;
            for (float y = .6f; y < 4.5f; y += .6f) K.Face(m, p + V(fx + .06f, y, dz0 - dw * .5f + .15f), p + V(fx + .06f, y, dz0 + dw * .5f - .15f), p + V(fx + .06f, y + .04f, dz0 + dw * .5f - .15f), p + V(fx + .06f, y + .04f, dz0 - dw * .5f + .15f), trim, Vector3.right, false);
            if (kind == 1)
            {
                m.Surface = ComicSurface.Plaster;
                m.Box(p + V(fx + 2f, 1.6f, len * .25f), V(4f, 3.2f, len * .4f), K.Hex("E4E2D5"));
                m.Surface = ComicSurface.Metal;
                m.Box(p + V(fx + 2.1f, 3.3f, len * .25f), V(4.5f, .2f, len * .4f + .5f), Zinc);
                m.Surface = ComicSurface.PaintedMetal;
                K.Face(m, p + V(fx + 4.02f, 1f, len * .25f - 1.6f), p + V(fx + 4.02f, 1f, len * .25f + 1.6f), p + V(fx + 4.02f, 2.4f, len * .25f + 1.6f), p + V(fx + 4.02f, 2.4f, len * .25f - 1.6f), K.R.ink, Vector3.right);
                m.Surface = ComicSurface.Glass;
                K.Face(m, p + V(fx + 4.04f, 1.1f, len * .25f - 1.5f), p + V(fx + 4.04f, 1.1f, len * .25f + 1.5f), p + V(fx + 4.04f, 2.3f, len * .25f + 1.5f), p + V(fx + 4.04f, 2.3f, len * .25f - 1.5f), WindowGlass, Vector3.right);
            }
            else
            {
                m.Surface = ComicSurface.Metal;
                m.Box(p + V(0, h + 2.3f, 0), V(1.4f, .7f, len * .7f), ZincDark);
                m.Box(p + V(0, h + 2.7f, 0), V(2f, .14f, len * .72f), Zinc);
            }
        }

        /// <summary>A stack of concrete products on pallets (pavers, kerbs): grey and buff blocks banded with straps.</summary>
        static void PaverStack(ComicMesh m, Vector3 p, int layers)
        {
            for (int i = 0; i < layers; i++)
            {
                m.Surface = ComicSurface.Wood;
                m.Box(p + V(0, i * 1.02f + .07f, 0), V(1.25f, .14f, 1.1f), Timber);
                m.Surface = ComicSurface.Concrete;
                m.Box(p + V(0, i * 1.02f + .56f, 0), V(1.15f, .84f, 1f), i % 2 == 0 ? K.Hex("BDB9AE") : K.Hex("D9C9A4"));
                m.Surface = ComicSurface.Plastic;
                m.Box(p + V(0, i * 1.02f + .56f, 0), V(1.17f, .86f, .08f), K.Hex("2F6FB0"));
            }
        }

        /// <summary>A yellow forklift: counterweight body, overhead guard, mast and forks with a pallet, ink wheels.</summary>
        static void Forklift(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            Color yellow = K.Hex("F2B21E");
            void B(Vector3 c, Vector3 size, Color col, ComicSurface s = ComicSurface.PaintedMetal) { m.Surface = s; m.Box(p + q * c, size, col, q); }
            B(V(0, .75f, .3f), V(1.2f, .9f, 1.9f), yellow);
            B(V(0, 1f, 1.1f), V(1.24f, .7f, .5f), K.Hex("3A4047"));
            foreach (float x in new[] { -.55f, .55f }) { B(V(x, 1.7f, -.35f), V(.07f, 1.1f, .07f), K.R.ink); B(V(x, 1.7f, .8f), V(.07f, 1.1f, .07f), K.R.ink); }
            B(V(0, 2.28f, .22f), V(1.24f, .08f, 1.3f), K.R.ink);
            B(V(0, 1.25f, .25f), V(.5f, .12f, .5f), K.R.ink, ComicSurface.Rubber);
            foreach (float x in new[] { -.38f, .38f }) B(V(x, 1.5f, -.75f), V(.1f, 3f, .12f), K.Hex("3A4047"));
            B(V(0, 2.95f, -.75f), V(.9f, .1f, .12f), K.Hex("3A4047"));
            B(V(0, .7f, -.84f), V(.9f, .5f, .06f), K.R.ink);
            foreach (float x in new[] { -.3f, .3f }) B(V(x, .5f, -1.4f), V(.12f, .06f, 1.1f), K.Hex("6B7178"), ComicSurface.Metal);
            foreach (float x in new[] { -.62f, .62f }) { B(V(x, .32f, -.45f), V(.22f, .64f, .64f), K.R.ink, ComicSurface.Rubber); B(V(x, .26f, .95f), V(.2f, .52f, .52f), K.R.ink, ComicSurface.Rubber); }
            var stack = new ComicMesh();
            PaverStack(stack, Vector3.zero, 1);
            m.Add(stack, p + q * V(0, .55f, -1.45f), q, Vector3.one * .8f);
        }

        /// <summary>A stack of shipping containers in plain colours (no logos): corrugation lines, door bars.</summary>
        static void ContainerStack(ComicMesh m, Vector3 p, int high)
        {
            Color[] cols = { K.Hex("B5553C"), K.Hex("2F6FB0"), K.Hex("3E8E55"), K.Hex("D9A441"), K.Hex("7F888F"), K.Hex("C8463A") };
            int k = 0;
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < high - row; i++, k++)
                {
                    Vector3 c = p + V(-row * 2.7f, 1.3f + i * 2.62f, row * 1.2f);
                    Color col = cols[(k * 3 + row) % cols.Length];
                    m.Surface = ComicSurface.Metal;
                    m.Box(c, V(2.44f, 2.6f, 12.2f), col);
                    m.Surface = ComicSurface.Plain;
                    for (float z = -5.6f; z < 5.8f; z += .8f)
                        K.Face(m, c + V(1.23f, -1.15f, z), c + V(1.23f, -1.15f, z + .1f), c + V(1.23f, 1.15f, z + .1f), c + V(1.23f, 1.15f, z), Color.Lerp(col, K.R.ink, .25f), Vector3.right, false);
                    foreach (float x in new[] { -.6f, -.2f, .2f, .6f })
                        K.Face(m, c + V(x - .03f, -1.1f, -6.11f), c + V(x + .03f, -1.1f, -6.11f), c + V(x + .03f, 1.1f, -6.11f), c + V(x - .03f, 1.1f, -6.11f), K.Hex("C9CED3"), Vector3.back, false);
                }
        }

        // ---------------------------------------------------------------- Ashfield birds (scenery and walkers)

        /// <summary>A wading bird standing in the shallows (scenery): kind 0 great egret (all white, long S-neck, yellow
        /// dagger bill, black legs); kind 1 yellow-billed spoonbill (white, long spatulate bill).</summary>
        static void WaderArt(ComicMesh m, Vector3 p, float yaw, float s, int kind)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            Color white = K.Hex("F6F5EF"), bill = K.Hex("F2C230"), legs = kind == 0 ? K.Hex("2A2F35") : K.Hex("E6D29A");
            Vector3 P(float x, float y, float z) => p + q * (V(x, y, z) * s);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.06f, .06f }) { Rod(m, P(x, 0, .05f), P(x, .55f, .05f), .018f * s, .022f * s, legs, 4); m.Box(P(x, .015f, -.02f), V(.06f, .03f, .16f) * s, legs, q); }
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(P(0, .7f, .08f), V(.24f, .26f, .5f) * s, white);
            m.Box(P(0, .68f, .36f), V(.14f, .1f, .22f) * s, white, q * Quaternion.Euler(-18f, 0, 0));
            Rod(m, P(0, .78f, -.1f), P(0, 1.02f, -.02f), .05f * s, .04f * s, white, 5);
            Rod(m, P(0, 1.02f, -.02f), P(0, 1.22f, -.16f), .04f * s, .035f * s, white, 5);
            m.Box(P(0, 1.25f, -.2f), V(.085f, .085f, .14f) * s, white, q);
            m.Surface = ComicSurface.Plastic;
            if (kind == 0) Rod(m, P(0, 1.25f, -.26f), P(0, 1.2f, -.52f), .028f * s, .006f, bill, 4, true);
            else { Rod(m, P(0, 1.24f, -.26f), P(0, 1.1f, -.5f), .022f * s, .02f * s, bill, 4); m.Box(P(0, 1.085f, -.54f), V(.075f, .02f, .11f) * s, bill, q * Quaternion.Euler(28f, 0, 0)); }
            foreach (float x in new[] { -.045f, .045f }) m.Box(P(x, 1.27f, -.24f), V(.02f, .02f, .02f) * s, K.R.ink, q);
        }

        /// <summary>A black swan on the water (scenery): black body, S-neck, red bill with a white band, white wing tips.</summary>
        static void SwanArt(ComicMesh m, Vector3 p, float yaw, float s)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            Color black = K.Hex("23252A");
            Vector3 P(float x, float y, float z) => p + q * (V(x, y, z) * s);
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(P(0, .2f, .1f), V(.5f, .4f, .9f) * s, black);
            m.Box(P(0, .36f, .45f), V(.3f, .16f, .3f) * s, black, q * Quaternion.Euler(-25f, 0, 0));
            m.Box(P(0, .3f, .62f), V(.2f, .06f, .1f) * s, K.Hex("F2F1EC"), q);
            Rod(m, P(0, .3f, -.28f), P(0, .7f, -.42f), .075f * s, .06f * s, black, 5);
            Rod(m, P(0, .7f, -.42f), P(0, .92f, -.3f), .06f * s, .05f * s, black, 5);
            m.Box(P(0, .93f, -.36f), V(.11f, .11f, .16f) * s, black, q);
            m.Surface = ComicSurface.Plastic;
            m.Box(P(0, .9f, -.5f), V(.07f, .06f, .16f) * s, K.Hex("D8332B"), q * Quaternion.Euler(15f, 0, 0));
            m.Box(P(0, .885f, -.55f), V(.072f, .062f, .03f) * s, K.Hex("F2F1EC"), q * Quaternion.Euler(15f, 0, 0));
        }

        /// <summary>A Pacific black duck (scenery or part of the walker): brown body, pale face with dark eye stripes, a
        /// green wing patch.</summary>
        static void DuckArt(ComicMesh m, Vector3 p, float yaw, float s)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            Color brown = K.Hex("6B5440"), dark = K.Hex("3E3028"), face = K.Hex("E6D7B0");
            Vector3 P(float x, float y, float z) => p + q * (V(x, y, z) * s);
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(P(0, .2f, .04f), V(.24f, .22f, .42f) * s, brown);
            m.Box(P(0, .26f, .26f), V(.14f, .08f, .14f) * s, dark, q * Quaternion.Euler(-20f, 0, 0));
            m.Box(P(0, .36f, -.17f), V(.13f, .14f, .15f) * s, face, q);
            m.Box(P(0, .43f, -.17f), V(.135f, .04f, .16f) * s, dark, q);
            m.Box(P(0, .365f, -.175f), V(.14f, .025f, .1f) * s, dark, q);
            m.Surface = ComicSurface.Plastic;
            m.Box(P(0, .33f, -.29f), V(.07f, .035f, .1f) * s, K.Hex("5F6B52"), q);
            foreach (int sd in new[] { -1, 1 })
            {
                m.Box(P(sd * .12f, .22f, .06f), V(.03f, .12f, .26f) * s, dark, q);
                m.Box(P(sd * .125f, .22f, .08f), V(.03f, .05f, .08f) * s, K.Hex("2F8F5A"), q);
                m.Box(P(sd * .05f, .04f, 0), V(.05f, .08f, .08f) * s, K.Hex("C9853A"), q);
            }
        }

        // Great egret walking the path: the wader on dry land.
        static ComicMesh EgretMesh() => Make(m => WaderArt(m, Vector3.zero, 0f, 1f, 0));

        // A Pacific black duck leading two ducklings in a line.
        static ComicMesh DucksMesh() => Make(m =>
        {
            DuckArt(m, Vector3.zero, 0f, 1.1f);
            DuckArt(m, V(.08f, 0, .55f), 0f, .5f);
            DuckArt(m, V(-.06f, 0, .9f), 0f, .5f);
        });

        // Australian white ibis: white body, bare black head and neck, a long down-curved bill, black tail plumes.
        static ComicMesh IbisMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color white = K.Hex("F1EFE6"), black = K.Hex("23252A");
            m.Ellipsoid(V(0, .5f, .06f), V(.28f, .28f, .5f), white);
            m.Box(V(0, .52f, .34f), V(.2f, .14f, .16f), black, Quaternion.Euler(-20f, 0, 0));
            m.Surface = ComicSurface.Plastic;
            Rod(m, V(0, .58f, -.12f), V(0, .82f, -.24f), .05f, .04f, black, 5);
            m.Box(V(0, .85f, -.28f), V(.09f, .09f, .12f), black);
            Rod(m, V(0, .85f, -.33f), V(0, .78f, -.5f), .025f, .018f, black, 4);
            Rod(m, V(0, .78f, -.5f), V(0, .64f, -.6f), .018f, .008f, black, 4, true);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.07f, .07f }) { Rod(m, V(x, 0, .05f), V(x, .4f, .05f), .018f, .022f, K.Hex("4A3F3A"), 4); m.Box(V(x, .015f, -.01f), V(.06f, .03f, .15f), K.Hex("4A3F3A")); }
            return m;
        }

        // ---------------------------------------------------------------- Ashfield obstacles

        // A shared-path bollard: white with red reflective bands and a rounded top.
        static ComicMesh PathBollardMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .04f, 0), .2f, .08f, K.R.ink, 8);
            Rod(m, V(0, .08f, 0), V(0, .95f, 0), .11f, .1f, White, 8);
            m.Cylinder(V(0, .62f, 0), .115f, .12f, HazardRed, 8);
            m.Cylinder(V(0, .82f, 0), .11f, .1f, HazardRed, 8);
            Blob(m, V(0, .98f, 0), V(.21f, .16f, .21f), White, 8, 3);
            return m;
        }

        // A shared-path sign: a blue disc with a white bike and walker blocks on a galvanised post, and a small plate.
        static ComicMesh PathSignMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Metal };
            Rod(m, V(0, 0, 0), V(0, 2.3f, 0), .04f, .04f, Galv, 6, true);
            m.Cylinder(V(0, .04f, 0), .16f, .08f, K.R.ink, 8);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(V(0, 1.85f, -.05f), .34f, .04f, White, 10, 1f, Quaternion.Euler(90, 0, 0));
            m.Cylinder(V(0, 1.85f, -.075f), .3f, .03f, SignBlue, 10, 1f, Quaternion.Euler(90, 0, 0));
            m.Surface = ComicSurface.Plain;
            void Q(float x0, float x1, float y0, float y1) => K.Face(m, V(x0, y0, -.095f), V(x1, y0, -.095f), V(x1, y1, -.095f), V(x0, y1, -.095f), White, Vector3.back, false);
            Q(-.2f, -.14f, 1.72f, 1.8f); Q(.08f, .14f, 1.72f, 1.8f); Q(-.17f, .11f, 1.8f, 1.83f); Q(-.06f, -.02f, 1.83f, 1.9f);  // the bike
            Q(-.04f, .02f, 1.92f, 2.04f); Q(-.07f, .05f, 2.04f, 2.08f);                                                         // the walker
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, 1.32f, -.05f), V(.5f, .22f, .03f), White);
            m.Box(V(0, 1.32f, -.07f), V(.4f, .05f, .01f), K.R.ink);
            return m;
        }

        // A pallet of concrete pavers that has fallen off a truck from the yards: strapped blocks, one loose paver.
        static ComicMesh PaverPalletMesh()
        {
            var m = new ComicMesh();
            PaverStack(m, Vector3.zero, 1);
            m.Surface = ComicSurface.Concrete;
            m.Box(V(.75f, .05f, -.2f), V(.3f, .1f, .5f), K.Hex("BDB9AE"), Quaternion.Euler(0, 25f, 0));
            return m;
        }

        // A pop-up sprinkler watering the verge: a short riser and a fan of water blades.
        static ComicMesh SprinklerMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            m.Cylinder(V(0, .1f, 0), .09f, .2f, K.R.ink, 8);
            m.Cylinder(V(0, .24f, 0), .05f, .1f, K.Hex("C8463A"), 8);
            m.Surface = ComicSurface.Glass;
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f;
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                Slab(m, V(0, .28f, 0) + dir * .08f, V(0, .72f, 0) + dir * .5f, Vector3.Cross(Vector3.up, dir), .03f, .16f, .03f, K.Hex("CFEAF2"), K.Hex("9FD3E4"));
                Slab(m, V(0, .72f, 0) + dir * .5f, V(0, .5f, 0) + dir * .74f, Vector3.Cross(Vector3.up, dir), .16f, .08f, .03f, K.Hex("E6F5F8"), K.Hex("9FD3E4"));
            }
            return m;
        }

        // ---------------------------------------------------------------- Ashfield far scenery

        static ComicMesh AshFar(float grade) => EastFar(grade, 280, 64f, 105f, 215f, K.Hex("C6DCDC"), K.Hex("7C8A4E"), true, (m, r) =>
        {
            Color haze = K.Hex("C6DCDC");
            // the flats continue out to the river: samphire patches, pools, paperbark thickets, dead trees
            Color[] mats = { Samphire1, Samphire2, Samphire3, SamphirePink };
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 80; i++)
            {
                float x = F(r, 62f, 102f), z = F(r, -200f, 800f), w = F(r, 5f, 12f), l = F(r, 6f, 16f);
                K.Face(m, V(x, -.27f, z), V(x, -.27f, z + l), V(x + w, -.27f, z + l * .8f), V(x + w * .9f, -.27f, z + l * .1f), Haze(i % 9 == 0 ? PoolBlue : mats[i % mats.Length], x, haze), Vector3.up, false);
            }
            for (float z = -210f; z < 810f; z += F(r, 10f, 18f))
            {
                float x = F(r, 64f, 100f);
                Rod(m, V(x, -.5f, z), V(x + F(r, -1.5f, 1.5f), 5f, z), .35f, .2f, Haze(PaperTrunk, x, haze), 4);
                Blob(m, V(x, 6.5f, z), V(F(r, 6f, 9f), F(r, 4f, 6f), F(r, 6f, 9f)), Haze(r.Next(2) == 0 ? PaperLeaf : PaperLeafDark, x, haze), 6, 4, F(r, 0f, 3f));
            }
            // Ron Courtney Island in the river: a low wooded island
            Ridge(m, 1, 135f, 36f, 180f, 520f, -1.2f, z => 3f + 1.5f * Mathf.Sin(z * .03f), K.Hex("B9A77E"), K.Hex("6F8F52"), K.Hex("7C9A5A"), haze, .3f);
            for (float z = 190f; z < 510f; z += F(r, 9f, 14f))
                Blob(m, V(F(r, 142f, 162f), 7f, z), V(F(r, 10f, 15f), F(r, 8f, 12f), F(r, 10f, 14f)), Haze(Color.Lerp(GumLeaf, GumLeafDark, F(r, 0f, 1f)), 150f, haze), 6, 4, F(r, 0f, 3f));
            // north: a tall stack and a water tower on the industrial skyline
            m.Surface = ComicSurface.Concrete;
            Rod(m, V(-190f, 0f, 260f), V(-190f, 46f, 260f), 2.6f, 1.6f, Haze(K.Hex("C9C2B4"), 220f, haze), 6);
            Rod(m, V(-190f, 40f, 260f), V(-190f, 42f, 260f), 1.9f, 1.85f, Haze(HazardRed, 220f, haze), 6);
            Rod(m, V(-240f, 0f, 520f), V(-240f, 20f, 520f), 2f, 2f, Haze(K.Hex("D3D7DA"), 280f, haze), 6);
            m.Cylinder(V(-240f, 24f, 520f), 7f, 8f, Haze(K.Hex("E4E2D5"), 280f, haze), 8, .75f);
        });
    }
}
