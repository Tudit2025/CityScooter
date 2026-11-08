using System;
using System.Collections.Generic;
using System.Linq;
using CityScooter.EditorTools.Art;
using CityScooter.Rendering;
using CityScooter.Track;
using UnityEditor;
using UnityEngine;
using static CityScooter.EditorTools.Districts.ParkRiverKit;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// MOUNTS BAY RD (v0.3 district, order 70): the road squeezed between the Kings Park escarpment (pale limestone
    /// cliff, white retaining walls, bush and grass trees on the ledges, Jacob's Ladder) and the Swan River (red shared
    /// path, rock revetment, wide blue water, yachts). Near the city end: palms and apartment towers, the Narrows Bridge
    /// freeway deck overhead; further west the red-brick Old Swan Brewery with its footbridge, and a small inlet the road
    /// jumps over (river-wall ramp + SPLASH gap).
    ///   Menu: CityScooter/Districts/Build Mounts Bay Rd
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.MountsBayBuilder.BuildBatch
    /// The ride goes from the city back west towards UWA: the escarpment is on the right, the river on the left.
    /// </summary>
    public static class MountsBayBuilder
    {
        public const string Id = "mountsbay", Name = "MountsBay";
        public const string PrefabFolder = ComicCityBuilder.Prefabs + "/Districts/MountsBay";
        public const string MeshFolder = ComicCityBuilder.Root + "/Districts/MountsBay";
        public const string AssetPath = DistrictData + "/District_MountsBay.asset";
        /// <summary>Riverside: flatter than the street (-1.6 m per 40 m instead of -3.15).</summary>
        public const float River = -1.6f, Blend = -2.4f, Grade = 1.6f / 40f;
        public const float WaterY = -1.1f;
        // Inlet: a full-width river-wall ramp, then open water across the whole road.
        public const float InletRampZ = 12f, InletRise = 6f, InletHeight = 1.4f, InletGap = 5f;
        /// <summary>Mounts Bay Rd is a 4-lane divided road: half width 8 (two lanes each way), sidewalks 4 m.</summary>
        public const float WideHalf = 8f, Shift = 4f, WideEdge = 12f;
        /// <summary>Jacob's Ladder: centre z of the staircase in its chunk and its width.</summary>
        public const float LadderZ = 20f, LadderWidth = 2.6f;

        static readonly Color RedPath = new Color(.7f, .3f, .24f), Brick = new Color(.72f, .33f, .22f), BrickDark = new Color(.55f, .24f, .17f),
            Concrete = new Color(.8f, .78f, .73f), ConcreteDark = new Color(.55f, .54f, .52f), Truss = new Color(.33f, .3f, .32f);

        [MenuItem("CityScooter/Districts/Build Mounts Bay Rd")]
        public static void Build()
        {
            Init();
            PrefabBuildUtility.EnsureFolder(PrefabFolder);
            PrefabBuildUtility.EnsureFolder(MeshFolder);

            RoadChunk entry = Entry();
            var chunks = new List<RoadChunk> { Towers(), Narrows(), JacobsLadder(), Escarpment(), Inlet(), Brewery() };
            RoadChunk exit = Exit();

            GameObject bin = WheelieBin();
            GameObject pelican = Pelican();
            GameObject cyclist = Cyclist();
            GameObject ramp = RiverWallRamp();
            GameObject far = FarShore();

            SaveDistrict(AssetPath, d =>
            {
                d.id = Id; d.displayName = "MOUNTS BAY RD"; d.order = 70; d.length = new Vector2(440f, 600f);
                d.chunks = chunks.ToArray(); d.orderedChunks = true; d.entry = entry; d.exit = exit;
                d.allowBoulevard = false; // the whole district is already a 4-lane road
                var obstacles = new List<Obstacle> { bin.GetComponent<Obstacle>(), pelican.GetComponent<Obstacle>() };
                foreach (string id in new[] { "Cones", "Crates" }) { Obstacle o = BaseObstacle(id); if (o != null) obstacles.Add(o); }
                d.obstacles = obstacles.ToArray();
                d.jumpObstacles = Array.Empty<Obstacle>();
                d.gifts = Array.Empty<Gift>();
                d.hazards = Array.Empty<HazardPattern>();
                d.pedestrians = new[] { Person("Jogger"), Person("Jogger"), cyclist, cyclist, Person("DogWalker"), Person("Skater"), pelican };
                d.ramps = new[] { ramp.GetComponent<Ramp>() };
                d.rampFrequency = 1.2f;
                d.blendMeters = 150f;
                d.fogColor = new Color(.62f, .77f, .88f);
                d.skyTint = new Color(.95f, .98f, 1f);
                d.ambientSky = new Color(.62f, .8f, .95f);
                d.ambientEquator = new Color(.78f, .85f, .88f);
                d.ambientGround = new Color(.55f, .57f, .56f);
                d.sunColor = new Color(1f, .97f, .92f);
                d.sunIntensity = 1.4f;
                d.farScenery = far;
                // gateway and ambience belong to the district system (DistrictSetup): not touched here.
            });
            AssetDatabase.SaveAssets();
            Debug.Log("[MountsBay] built " + (chunks.Count + 2) + " chunks: " + string.Join("; ", Report));
        }

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static ChunkCtx New(string id, float endHeight) => new ChunkCtx(Name, id, PrefabFolder, MeshFolder, endHeight);

        /// <summary>Red shared path on the river side (white centre dashes), concrete footpath at the cliff.</summary>
        static void RiverRoad(ChunkCtx c, float redFrom = 0f, float redTo = 40f) =>
            Road(c, (s, z) => s < 0 && z >= redFrom && z <= redTo ? RedPath : R.pavement, (s, z) => !(s < 0 && z >= redFrom && z <= redTo));

        // ------------------------------------------------------------------ the river side (left)

        static void RiverSide(ChunkCtx c, float z0 = 0f, float z1 = 40f, bool lamps = true, bool palms = false)
        {
            // Lawn strip, rock revetment, water (sheared with the road so it meets the next chunk's water).
            Ground(c.RoadFlat, -1, Edge, 10.5f, .12f, Lawn, z0, z1);
            Bank(c.RoadFlat, -1, 10.5f, .12f, 13.5f, WaterY + .1f, Rock, z0, z1, ComicSurface.Concrete);
            RiverWater(c, z0, z1, 12.5f, 90f);
            for (float z = z0 + 1f; z < z1 - .5f; z += 1.6f)
                Boulder(c.Props, c.P(-F(c.Rnd, 10.8f, 13f), F(c.Rnd, -.7f, -.1f), z), F(c.Rnd, .7f, 1.2f), c.Rnd);
            if (lamps) for (float z = z0 + 8f; z < z1; z += 20f) Lamp(c.Props, c.P(-8.7f, .12f, z), 1);
            if (palms) for (float z = z0 + 4f; z < z1; z += 9f) Palm(c.Front, c.P(-9.6f, .12f, z), F(c.Rnd, 8f, 10f), c.NextSeed());
            // Yachts out on the river.
            for (int i = 0; i < 2; i++) Yacht(c.Deep, c.P(-F(c.Rnd, 28f, 60f), WaterY, F(c.Rnd, z0 + 4f, z1 - 4f)), F(c.Rnd, .8f, 1.2f), i == 0 ? R.red : new Color(.2f, .45f, .8f));
        }

        static void RiverWater(ChunkCtx c, float z0, float z1, float xa, float xb, int side = -1)
        {
            ComicMesh m = c.RoadFlat;
            m.Surface = ComicSurface.Plain;
            for (float z = z0; z < z1 - .001f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                Flat(m, side * xa, side * xb, z, e, WaterY, Water);
                // Sparkle dashes on the water.
                for (int i = 0; i < 2; i++)
                {
                    float x = F(c.Rnd, xa + 2f, xb - 6f), zz = F(c.Rnd, z + .3f, e - .5f);
                    Flat(m, side * x, side * (x + F(c.Rnd, 1.5f, 4f)), zz, zz + .22f, WaterY + .01f, i == 0 ? WaterLight : WaterFoam);
                }
            }
        }

        /// <summary>A yacht: white hull with a coloured band and navy deck stripe, round mast, sail of stacked thick slabs.</summary>
        internal static void Yacht(ComicMesh m, Vector3 p, float s, Color hull)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, .35f * s, 0), V(1.4f * s, .7f * s, 4.2f * s), Color.white);
            m.Box(p + V(0, .58f * s, 0), V(1.42f * s, .14f * s, 4.22f * s), hull);
            m.Box(p + V(0, .74f * s, .4f * s), V(.9f * s, .1f * s, 2f * s), R.ink);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(p + V(0, 3.6f * s, .2f * s), .07f * s, 5.8f * s, R.ink, 8);
            m.Surface = ComicSurface.Fabric;
            for (int k = 0; k < 5; k++)
            {
                float y = (1.1f + k * 1f) * s, len = (1.9f - k * .34f) * s;
                m.Box(p + V(0, y + .48f * s, .3f * s + len * .5f), V(.08f * s, .98f * s, len), Color.white);
            }
        }

        /// <summary>Riverside lamp: navy base, round grey post, arm and a cream lamp head with a navy cap.</summary>
        internal static void Lamp(ComicMesh m, Vector3 p, int armSide)
        {
            Color post = new Color(.28f, .32f, .36f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .15f, 0), .22f, .3f, R.ink, 8, .9f);
            m.Cylinder(p + V(0, 3.6f, 0), .08f, 6.9f, post, 8, .8f);
            m.Box(p + V(armSide * .7f, 6.95f, 0), V(1.4f, .1f, .1f), post);
            m.Box(p + V(armSide * 1.35f, 6.82f, 0), V(.55f, .16f, .32f), R.cream);
            m.Box(p + V(armSide * 1.35f, 6.94f, 0), V(.6f, .08f, .36f), R.ink);
        }

        // ------------------------------------------------------------------ the escarpment side (right)

        /// <summary>White retaining wall at the footpath, a bush ledge, then the stepped limestone cliff (Kings Park above).</summary>
        static void CliffSide(ChunkCtx c, float z0 = 0f, float z1 = 40f, float wall = 3f, float cliff = 24f, float cliffX = 12f, float skip0 = 1e4f, float skip1 = -1e4f)
        {
            ComicMesh f = c.FrontFlat;
            Wall(f, Edge + .35f, .6f, wall, z0, z1, WhiteWall, new Color(.86f, .85f, .8f), -.6f, 8f);
            f.Surface = ComicSurface.Plain;
            for (float z = z0 + 2f; z < z1; z += 4f) Face(f, V(Edge + .04f, .6f, z), V(Edge + .04f, .6f, z + .25f), V(Edge + .04f, .85f, z + .25f), V(Edge + .04f, .85f, z), R.ink, Vector3.left, false);
            Ground(c.RoadFlat, 1, Edge + .65f, cliffX + .5f, wall, BushFloor, z0, z1);
            for (float z = z0 + 2f; z < z1; z += 7f)
            {
                if (c.Rnd.NextDouble() < .5) Balga(c.Props, c.P(F(c.Rnd, 9.6f, cliffX - .6f), wall, z + F(c.Rnd, 0f, 4f)), F(c.Rnd, .8f, 1.1f), c.NextSeed());
                else Shrub(c.Props, c.P(F(c.Rnd, 9.6f, cliffX - .6f), wall, z + F(c.Rnd, 0f, 4f)), F(c.Rnd, .8f, 1.2f), Color.Lerp(BanksiaLeaf, BushFloor, .3f));
            }
            Cliff(c, z0, z1, cliffX, cliff, skip0, skip1);
        }

        /// <summary>Stepped limestone cliff from x0 up to height, bush on the ledges, Kings Park trees on top (none between skip0 and skip1).</summary>
        static void Cliff(ChunkCtx c, float z0, float z1, float x0, float height, float skip0 = 1e4f, float skip1 = -1e4f)
        {
            ComicMesh f = c.FrontFlat;
            Color[] layers = { Limestone, new Color(.84f, .76f, .58f), LimestoneDark, new Color(.9f, .85f, .7f) };
            f.Surface = ComicSurface.Concrete;
            for (float z = z0; z < z1 - .001f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f), zc = (z + e) * .5f, len = e - z + .02f;
                float x = x0 + F(c.Rnd, -.6f, .6f), top = 0f;
                for (int k = 0; k < 4; k++)
                {
                    top = height * (k + 1) / 4f + F(c.Rnd, -1.2f, 1.2f);
                    float xa = x + k * 3.2f + F(c.Rnd, -.5f, .5f);
                    f.Box(V((xa + 46f) * .5f, top * .5f - 1f, zc), V(46f - xa, top + 2f, len), layers[(k + (int)(z / 4f)) % layers.Length]);
                    if (k < 3 && c.Rnd.NextDouble() < .55)
                    {
                        c.DeepFlat.Surface = ComicSurface.Foliage;
                        c.DeepFlat.Ellipsoid(V(xa + 1.6f, top + .4f, zc + F(c.Rnd, -1f, 1f)), V(2.6f, 1.2f, 2.4f), Color.Lerp(GumLeafDark, BushFloor, F(c.Rnd, 0f, .5f)));
                    }
                }
            }
            // Kings Park bush along the top of the cliff.
            for (float z = z0 + 3f; z < z1; z += 8f)
            {
                if (z + 8f > skip0 && z - 2f < skip1) continue;
                Gum(c.Deep, c.P(F(c.Rnd, x0 + 16f, x0 + 28f), height + .5f, z), F(c.Rnd, 13f, 17f), c.NextSeed(), true);
                c.Deep.Surface = ComicSurface.Foliage;
                c.Deep.Ellipsoid(c.P(x0 + 14f, height + 1.5f, z + 4f), V(8f, 4f, 7f), Color.Lerp(GumLeaf, BanksiaLeaf, F(c.Rnd, 0f, 1f)));
            }
        }

        // ------------------------------------------------------------------ chunks (a 4-lane divided road)

        /// <summary>A 4-lane chunk: road half width 8 at both ends (two lanes each way, painted median).</summary>
        static ChunkCtx Wide(string id, float endHeight)
        {
            ChunkCtx c = New(id, endHeight);
            c.HalfStart = c.HalfEnd = WideHalf;
            return c;
        }

        /// <summary>Builds one side's dressing as for the 2-lane street, then moves it out by 4 m to the wide road's kerb line.</summary>
        static void Side(ChunkCtx c, int side, Action<ChunkCtx> build)
        {
            var s = new ChunkCtx(Name, c.Id + (side < 0 ? "L" : "R"), PrefabFolder, MeshFolder, -c.Grade * c.L, c.L);
            build(s);
            c.Absorb(s, V(side * Shift, 0f, 0f));
        }

        /// <summary>Four lane centres like the boulevard (x = -6, -2, 2, 6), rows at z 10 and 25.</summary>
        static void WideSlots(ChunkCtx c)
        {
            foreach (float z in new[] { 10f, 25f }) foreach (float x in new[] { -6f, -2f, 2f, 6f }) c.Slots.Add(new Vector2(x, z));
        }

        static void Row(ChunkCtx c, float z, params float[] xs) { foreach (float x in xs) c.Slots.Add(new Vector2(x, z)); }

        /// <summary>
        /// Entry / exit: the wedge between the angled sidewalk edge and the wide road's line (x = 12) is lawn behind a low
        /// limestone kerb wall that follows the edge, with clipped hedges (calm where the road is the 2-lane street).
        /// </summary>
        static void Wedge(ChunkCtx c)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (float z = 0f; z < c.L - .001f; z += 2f)
                {
                    float e0 = c.EdgeAt(z), e1 = c.EdgeAt(z + 2f), outer = WideEdge + .3f;
                    if (outer - Mathf.Min(e0, e1) > .05f)
                        Face(c.RoadFlat, V(side * e0, .14f, z), V(side * e1, .14f, z + 2f), V(side * outer, .14f, z + 2f), V(side * outer, .14f, z), Lawn, Vector3.up, false);
                    if (Mathf.Min(e0, e1) < WideEdge - .1f)
                    {
                        Vector3 a = V(side * (e0 + .3f), 0f, z), b = V(side * (e1 + .3f), 0f, z + 2f), d = b - a;
                        c.FrontFlat.Surface = ComicSurface.Concrete;
                        c.FrontFlat.Box((a + b) * .5f + V(0f, .1f, 0f), V(.5f, .9f, d.magnitude + .06f), Limestone, Quaternion.LookRotation(d));
                    }
                }
                for (float z = 1f; z < c.L; z += 2.6f)
                {
                    float e = c.EdgeAt(z);
                    if (WideEdge - e > 2.4f) Shrub(c.Props, c.P(side * (e + 1.5f), .14f, z), 1.15f, Color.Lerp(LeafGreen, Lawn, .3f));
                }
            }
        }

        static RoadChunk Escarpment()
        {
            ChunkCtx c = Wide("Escarpment", River);
            RiverRoad(c);
            Side(c, -1, s => RiverSide(s));
            Side(c, 1, s => CliffSide(s));
            WideSlots(c);
            return c.Save();
        }

        /// <summary>Near the city end: palms along both sides, apartment towers against the escarpment.</summary>
        static RoadChunk Towers()
        {
            ChunkCtx c = Wide("Towers", River);
            RiverRoad(c);
            Side(c, -1, s => RiverSide(s, 0f, 40f, false, true));
            Side(c, 1, s =>
            {
                Ground(s.RoadFlat, 1, Edge, 33f, .14f, Lawn, 0f, 40f);
                Wall(s.FrontFlat, Edge + .3f, .5f, .45f, 0f, 40f, WhiteWall, LimestoneCap);
                for (float z = 2f; z < 40f; z += 9f) Palm(s.Front, s.P(9.6f, .14f, z), F(s.Rnd, 9f, 11f), s.NextSeed());
                string[] ids = { "Tower", "Hotel", "Tower" };
                float[] lots = { 0f, 14f, 26f }, lens = { 14f, 12f, 14f };
                for (int i = 0; i < ids.Length; i++)
                {
                    ComicAssetSpec spec = R.buildings.First(b => b.id == ids[i]);
                    float zc = lots[i] + lens[i] * .5f, x = 14f + spec.depth * .5f;
                    Vector3 p = s.P(x, .14f, zc);
                    s.Front.Add(ComicKitGeometry.Building(spec, R), p, Quaternion.Euler(0, -90f, 0), V(lens[i] / spec.width, i == 1 ? 1.4f : 1f, 1f));
                    s.Front.Surface = ComicSurface.Concrete;
                    s.Front.Box(p + V(0, -1.1f, 0), V(spec.depth, 2.2f, lens[i]), R.ink);
                }
                Cliff(s, 0f, 40f, 32f, 30f);
            });
            WideSlots(c);
            return c.Save();
        }

        /// <summary>The Narrows Bridge freeway deck crossing high above the road on concrete piers (no colliders).</summary>
        static RoadChunk Narrows()
        {
            ChunkCtx c = Wide("Narrows", River);
            const float zc = 21f, y = 9.4f;
            RiverRoad(c);
            Side(c, -1, s => RiverSide(s));
            // A low escarpment here: the deck passes over it (nothing pokes through the deck).
            Side(c, 1, s => CliffSide(s, 0f, 40f, 3f, 7f, 13f, zc - 9f, zc + 9f));
            ComicMesh m = c.Front;
            m.Surface = ComicSurface.Concrete;
            for (float x = -160f; x < 80f - .001f; x += 40f)
            {
                Vector3 p = c.P(x + 20f, y, zc);
                m.Box(p, V(40.05f, 1.3f, 15f), Concrete);
                m.Box(p + V(0, -1.05f, -5f), V(40.05f, .8f, .6f), ConcreteDark);
                m.Box(p + V(0, -1.05f, 0f), V(40.05f, .8f, .6f), ConcreteDark);
                m.Box(p + V(0, -1.05f, 5f), V(40.05f, .8f, .6f), ConcreteDark);
                m.Box(p + V(0, 1.1f, -7.3f), V(40.05f, .9f, .4f), Concrete);
                m.Box(p + V(0, 1.1f, 7.3f), V(40.05f, .9f, .4f), Concrete);
                m.Box(p + V(0, .68f, 0f), V(40.05f, .06f, .2f), R.cream);
            }
            // Piers clear of the wide road (|x| > 12.1): on the cliff ledge on the right, in the river on the left. Each
            // goes from its ground up into the pier cap, which sits right under the girders.
            foreach (float x in new[] { -15.6f, 15.6f, -40f, -78f, -118f })
            {
                float baseY = x < 0f ? WaterY - .3f : 3f, capBottom = y - 1.85f;
                float h = capBottom - baseY + .05f;
                Vector3 b = c.P(x, baseY + h * .5f, zc);
                m.Box(b + V(0, 0, -4.5f), V(1.8f, h, 1.8f), Concrete);
                m.Box(b + V(0, 0, 4.5f), V(1.8f, h, 1.8f), Concrete);
                m.Box(c.P(x, y - 1.4f, zc), V(2.6f, .9f, 13f), ConcreteDark);
            }
            // Freeway traffic on the deck (driving across).
            Color[] cars = { R.red, R.yellow, new Color(.2f, .5f, .85f), Color.white };
            for (int i = 0; i < 4; i++)
                m.Add(ComicKitGeometry.Car(R, cars[i]), c.P(-30f + i * 17f, y + .65f, zc + (i % 2 == 0 ? -3f : 3f)), Quaternion.Euler(0, i % 2 == 0 ? 90f : -90f, 0), Vector3.one);
            WideSlots(c);
            return c.Save();
        }

        /// <summary>
        /// Jacob's Ladder: a long, straight concrete staircase climbing up and away from the road (perpendicular to it) from
        /// an opening in the retaining wall to the top of the escarpment: four flights with landings, solid under every
        /// step, set between stone side walls in a bushy slope, dark green handrails following the steps, climbers on it.
        /// </summary>
        static RoadChunk JacobsLadder()
        {
            ChunkCtx c = Wide("JacobsLadder", River);
            RiverRoad(c);
            Side(c, -1, s => RiverSide(s));
            const float E = WideEdge, ledge = 3f, hillX = 17f, hillTop = 23.6f, hillEnd = 48f;
            float za = LadderZ - LadderWidth * .5f, zb = LadderZ + LadderWidth * .5f, wz0 = za - 1.4f, wz1 = zb + 1.4f;
            float HillY(float x) => x <= hillX ? ledge : Mathf.Lerp(ledge, hillTop, Mathf.Clamp01((x - hillX) / (hillEnd - hillX)));
            Color wallCap = new Color(.86f, .85f, .8f), slope = Color.Lerp(BushFloor, Lawn, .25f);
            // Retaining wall with the opening at the footpath, the ledge behind it.
            Wall(c.FrontFlat, E + .35f, .6f, ledge, 0f, za, WhiteWall, wallCap, -.6f, 8f);
            Wall(c.FrontFlat, E + .35f, .6f, ledge, zb, 40f, WhiteWall, wallCap, -.6f, 8f);
            Ground(c.RoadFlat, 1, E + .65f, hillX + .5f, ledge, BushFloor, 0f, wz0);
            Ground(c.RoadFlat, 1, E + .65f, hillX + .5f, ledge, BushFloor, wz1, 40f);
            // Stepped cliff at the chunk ends, a bush slope in the middle that the stairs climb, the park on top.
            Cliff(c, 0f, 8f, hillX, 24f);
            Cliff(c, 32f, 40f, hillX, 24f);
            Bank(c.RoadFlat, 1, hillX, ledge, hillEnd, hillTop, slope, 8f, wz0);
            Bank(c.RoadFlat, 1, hillX, ledge, hillEnd, hillTop, slope, wz1, 32f);
            Ground(c.RoadFlat, 1, hillEnd - .5f, 90f, hillTop, BushFloor, 0f, 40f);
            for (int i = 0; i < 16; i++)
            {
                float x = F(c.Rnd, hillX + 1.5f, hillEnd - 1.5f), z = i % 2 == 0 ? F(c.Rnd, 9f, wz0 - 1f) : F(c.Rnd, wz1 + 1f, 31f);
                Vector3 p = c.P(x, HillY(x) - .1f, z);
                if (i % 4 == 0) Balga(c.Props, p, F(c.Rnd, .9f, 1.2f), c.NextSeed());
                else if (i % 4 == 1) Banksia(c.Props, p, F(c.Rnd, .8f, 1.1f), c.NextSeed());
                else Shrub(c.Props, p, F(c.Rnd, 1f, 1.6f), Color.Lerp(BanksiaLeaf, GumLeaf, F(c.Rnd, 0f, 1f)));
            }
            foreach (Vector2 g in new[] { new Vector2(30f, 11f), new Vector2(38f, 28.5f), new Vector2(51f, 15f), new Vector2(53f, 25.5f), new Vector2(58f, 20f) })
                Gum(c.Deep, c.P(g.x, HillY(g.x), g.y), F(c.Rnd, 14f, 18f), c.NextSeed(), true);
            Shrub(c.Props, c.P(hillEnd + .5f, hillTop, wz0 - .8f), 1.4f, GumLeafDark);
            Shrub(c.Props, c.P(hillEnd + .5f, hillTop, wz1 + .8f), 1.4f, BanksiaLeaf);

            ComicMesh m = c.Front;
            const int flights = 4, perFlight = 10;
            const float run = .72f, rise = .58f, landing = 1.6f;
            Color rail = new Color(.18f, .35f, .25f);
            float sx = E + .02f, sy = .2f;
            var treads = new List<Vector2>();
            // One tread (or landing) from x0 to x1 at height top: solid down into the ground, side walls up to the hill.
            void Tread(float x0, float x1, float top)
            {
                float xc = (x0 + x1) * .5f, len = x1 - x0 + .01f;
                m.Surface = ComicSurface.Concrete;
                m.Box(c.P(xc, (top - .8f) * .5f, LadderZ), V(len, top + .8f, LadderWidth), WhiteWall);
                m.Box(c.P(xc, top - .03f, za + .06f), V(len, .06f, .12f), LimestoneDark); // nosing shadow line at the edges
                float wallTop = Mathf.Max(top + 1f, HillY(x1) + .15f);
                foreach (float wz in new[] { (wz0 + za) * .5f, (zb + wz1) * .5f })
                    m.Box(c.P(xc, (wallTop - .8f) * .5f, wz), V(len, wallTop + .8f, za - wz0), Limestone);
                treads.Add(new Vector2(xc, top));
            }
            for (int f = 0; f < flights; f++)
            {
                float fx = sx, fy = sy;
                for (int i = 0; i < perFlight; i++) { sy += rise; Tread(sx, sx + run, sy); sx += run; }
                // Handrails along the flight (on the inside of both side walls) with posts on the steps.
                m.Surface = ComicSurface.PaintedMetal;
                foreach (float rz in new[] { za + .1f, zb - .1f })
                {
                    Rod(m, c.P(fx + run * .5f, fy + rise + .95f, rz), c.P(sx - run * .5f, sy + .95f, rz), .05f, rail);
                    for (int k = 0; k < perFlight; k += 3)
                        m.Cylinder(c.P(fx + (k + .5f) * run, fy + (k + 1) * rise + .48f, rz), .04f, .96f, rail, 8);
                }
                float lx = f < flights - 1 ? landing : hillEnd + 1f - sx;
                Tread(sx, sx + lx, sy);
                sx += lx;
            }
            // Climbers on the stairs (decor, out of reach), facing up the hill.
            Color[] tops = { new Color(1f, .42f, .62f), R.yellow, new Color(.3f, .7f, .9f) };
            int[] at = { 6, 19, 31 };
            for (int i = 0; i < at.Length; i++)
            {
                var climber = new ComicMesh();
                Climber(climber, Vector3.zero, tops[i]);
                Vector2 t = treads[Mathf.Min(at[i], treads.Count - 1)];
                m.Add(climber, c.P(t.x, t.y, LadderZ + (i % 2 == 0 ? -.55f : .55f)), Quaternion.Euler(0, -90f, 0), Vector3.one);
            }
            // Name board on the ledge beside the foot of the stairs, high enough to show over the tall wall before it.
            NameBlock(c.Front, c.P(E + .65f + 2.95f, ledge + .2f, wz0 - .7f), "JACOBS LADDER", .09f, WhiteWall, rail);
            WideSlots(c);
            return c.Save();
        }

        internal static void Climber(ComicMesh m, Vector3 p, Color top)
        {
            Color skin = new Color(.86f, .64f, .5f);
            m.Surface = ComicSurface.Fabric;
            m.Box(p + V(-.1f, .4f, .05f), V(.16f, .8f, .18f), R.ink, Quaternion.Euler(-25f, 0, 0));
            m.Box(p + V(.1f, .45f, -.1f), V(.16f, .8f, .18f), R.ink, Quaternion.Euler(20f, 0, 0));
            m.Box(p + V(0, 1.1f, 0), V(.46f, .6f, .26f), top, Quaternion.Euler(12f, 0, 0));
            m.Box(p + V(0, 1.55f, .05f), V(.24f, .26f, .24f), skin);
            m.Box(p + V(0, 1.7f, .07f), V(.26f, .08f, .26f), R.ink);
        }

        static RoadChunk Inlet()
        {
            ChunkCtx c = Wide("Inlet", River);
            const float E = WideEdge;
            float zr = InletRampZ, zl = zr + InletRise, zg = zl + InletGap;
            c.Ramps.Add(new RampSpec { X = 0f, Z = zr, Width = 2f * E, Rise = InletRise, Height = InletHeight, Top = 0f, Launch = 1f, Hang = .5f });
            c.Gaps.Add(new GapSpec { X = 0f, Z = zl, Length = InletGap, Width = 0f, Word = "SPLASH", Water = true });
            RiverRoad(c);
            Side(c, -1, s => { RiverSide(s, 0f, zl); RiverSide(s, zg, 40f); });
            Side(c, 1, s =>
            {
                Ground(s.RoadFlat, 1, Edge, 14f, .14f, Lawn, 0f, zl);
                Ground(s.RoadFlat, 1, Edge, 14f, .14f, Lawn, zg, 40f);
                Wall(s.FrontFlat, Edge + .3f, .5f, .5f, 0f, zl, WhiteWall, LimestoneCap);
                Wall(s.FrontFlat, Edge + .3f, .5f, .5f, zg, 40f, WhiteWall, LimestoneCap);
                Cliff(s, 0f, 40f, 16f, 18f);
                s.FrontFlat.Surface = ComicSurface.Concrete;
                s.FrontFlat.Box(V(15f, 1.2f, (zl + zg) * .5f), V(2.5f, 4.6f, InletGap + 2f), Limestone);   // culvert under the cliff
                s.FrontFlat.Box(V(13.7f, .1f, (zl + zg) * .5f), V(.2f, 2.2f, InletGap - .6f), R.ink);
            });
            RiverWater(c, zl, zg, -(E + 6f), 90f);    // the inlet: from the culvert, across the whole road, out to the river
            ComicMesh f = c.FrontFlat;
            // Limestone sea walls on both banks of the inlet (flush with the road).
            f.Surface = ComicSurface.Concrete;
            f.Box(V(2f, (WaterY - 1.02f) * .5f, zl - .3f), V(2f * E + 8f, 1.02f - WaterY, .6f), LimestoneDark);
            f.Box(V(2f, (WaterY - 1.02f) * .5f, zg + .3f), V(2f * E + 8f, 1.02f - WaterY, .6f), LimestoneDark);
            for (int i = 0; i < 10; i++)
            {
                float x = F(c.Rnd, -E, E + 4f), z = i % 2 == 0 ? zl + .5f : zg - .5f;
                f.Surface = ComicSurface.Foliage;
                f.Box(V(x, WaterY + .5f, z), V(.08f, 1.2f, .08f), new Color(.45f, .6f, .25f), Quaternion.Euler(F(c.Rnd, -10f, 10f), 0, F(c.Rnd, -10f, 10f)));
            }
            // The river-wall ramp: limestone blocks across the whole road and both paths, chevrons, yellow-black lip.
            RampBody(f, 0f, 2f * E, zr, InletRise, InletHeight, 0f, Limestone, LimestoneDark, R.yellow, ComicSurface.Concrete);
            f.Surface = ComicSurface.Plain;
            for (float x = -10.5f; x <= 10.5f; x += 3.5f)
                for (int k = 0; k < 2; k++)
                {
                    float z = zr + 1.4f + k * 1.6f, yy = InletHeight * (z - zr) / InletRise + .02f;
                    f.Tri(V(x - .8f, yy, z), V(x, yy + .08f, z + .9f), V(x + .8f, yy, z), R.yellow);
                }
            // No spawn slots: nothing on the run-up, the jump or the landing.
            return c.Save();
        }

        /// <summary>The red-brick Old Swan Brewery on the river bank and the steel footbridge over the road.</summary>
        static RoadChunk Brewery()
        {
            ChunkCtx c = Wide("Brewery", River);
            RiverRoad(c);
            Side(c, -1, s =>
            {
                Ground(s.RoadFlat, -1, Edge, 11f, .12f, Lawn, 0f, 40f);
                Ground(s.RoadFlat, -1, 11f, 34f, .3f, R.pavement, 0f, 40f, ComicSurface.Paving);
                RiverWater(s, 0f, 40f, 33.5f, 90f);
                for (float z = 2f; z < 40f; z += 3f) Boulder(s.Props, s.P(-F(s.Rnd, 33f, 34.5f), -.6f, z), 1f, s.Rnd);
                ComicMesh m = s.Front;
                Vector3 b = s.P(-22f, .3f, 18f);
                m.Surface = ComicSurface.Brick;
                void Block(Vector3 c, Vector3 size)
                {
                    m.Surface = ComicSurface.Brick;
                    m.Box(c, size, Brick);
                    m.Surface = ComicSurface.Concrete;
                    m.Box(c + V(0, -size.y * .5f + .35f, 0), V(size.x + .1f, .7f, size.z + .1f), R.ink);              // navy plinth
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Box(c + V(0, size.y * .5f - .15f, 0), V(size.x + .3f, .3f, size.z + .3f), BrickDark);           // cornice
                    m.Box(c + V(0, size.y * .5f + .06f, 0), V(size.x + .16f, .12f, size.z + .16f), R.cream);         // cream cap
                }
                Block(b + V(0, 5f, 0), V(14f, 10f, 26f));
                Block(b + V(2f, 7f, 15.5f), V(10f, 14f, 5f));
                Block(b + V(1f, 9f, -15f), V(8f, 18f, 6f));
                m.Surface = ComicSurface.Concrete;
                m.Box(b + V(1f, 19.2f, -15f), V(5f, 2.2f, 4f), R.cream);
                m.Box(b + V(1f, 20.4f, -15f), V(5.4f, .2f, 4.4f), R.ink);
                // Arched windows: navy recessed boxes with cream sills and cream arch heads (facing the road, +x).
                void Window(float fx, float y, float z, float w, float h)
                {
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Box(V(fx, y + h * .5f, z), V(.1f, h, w), R.ink);
                    m.Box(V(fx + .02f, y + h * .5f, z), V(.08f, h - .2f, w - .2f), R.glass);
                    m.Surface = ComicSurface.Concrete;
                    m.Box(V(fx + .08f, y - .06f, z), V(.22f, .12f, w + .3f), R.cream);
                    m.Box(V(fx + .04f, y + h + .1f, z), V(.14f, .2f, w + .2f), R.cream);
                }
                for (int level = 0; level < 3; level++)
                    for (int i = 0; i < 6; i++) Window(b.x + 7f, b.y + 1.6f + level * 3f, b.z - 10f + i * 4f, 1.4f, 1.9f);
                for (int level = 0; level < 5; level++) Window(b.x + 5f, b.y + 2f + level * 3.2f, b.z - 15f, 1.6f, 2f);
                for (float z = 6f; z < 40f; z += 14f) Palm(s.Front, s.P(-12f, .3f, z), F(s.Rnd, 8f, 10f), s.NextSeed());
            });
            Side(c, 1, s => CliffSide(s, 0f, 40f, 3f, 22f, 12f));
            // Footbridge: a dark steel truss from the brewery wing (x = -19) over the wide road into the cliff face.
            ComicMesh fm = c.Front;
            const float zb = 33f, yb = 7.6f, xa = -19.1f, xb = 20.6f;
            fm.Surface = ComicSurface.PaintedMetal;
            foreach (float dz in new[] { -1.3f, 1.3f })
            {
                fm.Beam(c.P(xa, yb, zb + dz), c.P(xb, yb, zb + dz), .25f, Truss);
                fm.Beam(c.P(xa, yb + 2.2f, zb + dz), c.P(xb, yb + 2.2f, zb + dz), .25f, Truss);
                for (int i = 0; i < 14; i++)
                {
                    float x0 = Mathf.Lerp(xa, xb, i / 14f), x1 = Mathf.Lerp(xa, xb, (i + 1) / 14f);
                    fm.Beam(c.P(x0, yb, zb + dz), c.P(x1, yb + 2.2f, zb + dz), .14f, Truss);
                    fm.Box(c.P(x0, yb + 1.1f, zb + dz), V(.14f, 2.2f, .14f), Truss);
                }
            }
            fm.Box(c.P((xa + xb) * .5f, yb - .1f, zb), V(xb - xa, .25f, 2.8f), ConcreteDark);
            // Piers on the brewery forecourt and on the cliff ledge, up to the underside of the deck.
            foreach (float x in new[] { -16.6f, 14.2f })
            {
                float baseY = x > 0f ? 3f : .3f;
                fm.Box(c.P(x, (yb - .2f + baseY) * .5f, zb), V(.7f, yb - .2f - baseY, .7f), Truss);
            }
            WideSlots(c);
            return c.Save();
        }

        static RoadChunk Entry()
        {
            ChunkCtx c = New("Entry", Blend);
            c.HalfStart = Half; c.HalfEnd = WideHalf;     // widens from the street to the 4-lane road (z 8 to 32)
            RiverRoad(c, 8f, 40f);
            Side(c, -1, s =>
            {
                Ground(s.RoadFlat, -1, Edge, 40f, .12f, Lawn, 0f, 12f);
                RiverSide(s, 12f, 40f, true, true);
                RiverWater(s, 0f, 12f, 40f, 90f);
                RoundTree(s.Front, s.P(-16f, .12f, 5f), 8f, Color.Lerp(Lawn, LeafGreen, .5f), 3);
            });
            Side(c, 1, s =>
            {
                Ground(s.RoadFlat, 1, Edge, 22f, .14f, Lawn, 0f, 14f);
                RoundTree(s.Front, s.P(14f, .14f, 6f), 8.5f, Color.Lerp(Lawn, LeafGreen, .5f), 4);
                CliffSide(s, 14f, 40f, 3f, 20f, 12f);
                Cliff(s, 0f, 14f, 20f, 16f);
                NameBlock(s.Front, s.P(13.4f, .14f, 12f), "MOUNTS BAY RD", .12f, WhiteWall, Water);
            });
            Wedge(c);
            Row(c, 5f, -2.6f, 0f, 2.6f); Row(c, 25f, -2f, 2f);
            return c.Save();
        }

        static RoadChunk Exit()
        {
            ChunkCtx c = New("Exit", Blend);
            c.HalfStart = WideHalf; c.HalfEnd = Half;     // narrows back to the street (z 8 to 32)
            RiverRoad(c, 0f, 32f);
            Side(c, -1, s =>
            {
                RiverSide(s, 0f, 26f);
                Ground(s.RoadFlat, -1, Edge, 40f, .12f, Lawn, 26f, 40f);
                RiverWater(s, 26f, 40f, 40f, 90f);
                RoundTree(s.Front, s.P(-15f, .14f, 33f), 9f, Color.Lerp(Lawn, LeafGreen, .5f), 49);
            });
            Side(c, 1, s =>
            {
                CliffSide(s, 0f, 26f, 3f, 18f, 12f);
                Ground(s.RoadFlat, 1, Edge, 40f, .14f, Lawn, 26f, 40f);
                Cliff(s, 26f, 40f, 24f, 14f);
                RoundTree(s.Front, s.P(15f, .14f, 33f), 9f, Color.Lerp(Lawn, LeafGreen, .5f), 51);
            });
            Wedge(c);
            Row(c, 15f, -2f, 2f); Row(c, 35f, -2.6f, 0f, 2.6f);
            return c.Save();
        }

        // ------------------------------------------------------------------ obstacles, people, animals, ramp, far shore

        /// <summary>A Perth council wheelie bin (dark green, yellow recycling lid) left on the road.</summary>
        static GameObject WheelieBin()
        {
            var m = new ComicMesh();
            Color body = new Color(.16f, .38f, .24f), lid = new Color(1f, .8f, .1f);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .54f, 0), V(.62f, .96f, .72f), body);
            m.Box(V(0, .1f, 0), V(.66f, .2f, .76f), R.ink);                    // navy base
            m.Box(V(0, 1.06f, -.02f), V(.7f, .1f, .82f), lid);
            m.Box(V(0, 1.01f, .42f), V(.52f, .09f, .09f), R.ink);              // handle
            m.Box(V(0, .7f, -.37f), V(.32f, .22f, .05f), R.cream);             // label
            m.Box(V(0, .7f, -.4f), V(.18f, .05f, .02f), R.ink);
            m.Surface = ComicSurface.Rubber;
            for (int s = -1; s <= 1; s += 2) m.Cylinder(V(s * .3f, .13f, .3f), .13f, .1f, R.ink, 8, 1f, Quaternion.Euler(0, 0, 90f));
            return SavePrefab(PrefabFolder + "/OBS_MountsBay_WheelieBin.prefab", MeshFolder, m, true, true, root =>
            {
                Box(root, V(0, .55f, 0), V(.7f, 1.1f, .8f));
                var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
                o.kind = ObstacleKind.Thing; o.canJumpOver = true; o.spansRoad = false; o.punchable = true; o.animal = false; o.police = false; o.spawnWeight = 1.2f;
            });
        }

        /// <summary>Australian pelican: chunky white body, black wings and tail, a huge pale-pink bill and pouch built from slabs.</summary>
        static GameObject Pelican()
        {
            var m = new ComicMesh();
            Color white = new Color(.97f, .97f, .95f), black = new Color(.12f, .12f, .14f), bill = new Color(.98f, .78f, .7f),
                pouch = new Color(.98f, .86f, .6f), legs = new Color(.55f, .6f, .68f);
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(V(0, .64f, .12f), V(.6f, .54f, .9f), white);
            foreach (float x in new[] { -.27f, .27f })
            {
                m.Box(V(x, .74f, .12f), V(.09f, .22f, .5f), white, Quaternion.Euler(-10f, 0, 0));   // white upper wing
                m.Box(V(x, .68f, .5f), V(.09f, .24f, .4f), black, Quaternion.Euler(-10f, 0, 0));   // black flight feathers
            }
            m.Box(V(0, .72f, .6f), V(.32f, .14f, .32f), black, Quaternion.Euler(-20f, 0, 0));
            Rod(m, V(0, .82f, -.12f), V(0, 1.14f, -.24f), .1f, white);
            m.Ellipsoid(V(0, 1.2f, -.3f), V(.26f, .24f, .3f), white);
            foreach (float x in new[] { -.08f, .08f }) m.Box(V(x, 1.25f, -.39f), V(.04f, .05f, .05f), R.ink);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, 1.15f, -.72f), V(.13f, .08f, .7f), bill, Quaternion.Euler(12f, 0, 0));          // upper bill
            m.Box(V(0, 1.06f, -.66f), V(.15f, .16f, .56f), pouch, Quaternion.Euler(16f, 0, 0));       // pouch
            m.Box(V(0, 1.1f, -1.05f), V(.1f, .1f, .1f), new Color(.9f, .55f, .45f));                    // hooked tip
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.11f, .11f })
            {
                m.Cylinder(V(x, .2f, .1f), .04f, .4f, legs, 8);
                m.Box(V(x, .03f, .02f), V(.18f, .06f, .22f), legs);
            }
            return SaveWalker(PrefabFolder + "/PED_MountsBay_Pelican.prefab", MeshFolder, m, V(0, .65f, 0), V(.7f, 1.3f, 1.4f), .4f, true, true);
        }

        /// <summary>A cyclist on the shared path: helmet (it's WA), road bike, bright jersey. A person (not punchable).</summary>
        static GameObject Cyclist()
        {
            var m = new ComicMesh();
            Color jersey = new Color(.1f, .62f, .6f), shorts = R.ink, skin = new Color(.86f, .64f, .5f), helmet = R.yellow, frame = R.red;
            m.Surface = ComicSurface.Rubber;
            foreach (float z in new[] { -.55f, .55f })
            {
                m.Cylinder(V(0, .36f, z), .34f, .08f, R.ink, 10, 1f, Quaternion.Euler(0, 0, 90f));
                m.Cylinder(V(0, .36f, z), .08f, .12f, Steel, 8, 1f, Quaternion.Euler(0, 0, 90f));
            }
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, V(0, .36f, .55f), V(0, .62f, .05f), .035f, frame);
            Rod(m, V(0, .62f, .05f), V(0, .92f, .12f), .035f, frame);
            Rod(m, V(0, .62f, .05f), V(0, .95f, -.45f), .035f, frame);
            Rod(m, V(0, .95f, -.45f), V(0, .36f, -.55f), .035f, frame);
            Rod(m, V(0, .92f, .12f), V(0, .95f, -.45f), .035f, frame);
            m.Box(V(0, 1.06f, -.5f), V(.48f, .06f, .06f), R.ink);
            m.Box(V(0, .97f, .14f), V(.13f, .07f, .27f), R.ink);
            m.Surface = ComicSurface.Fabric;
            m.Beam(V(.1f, 1f, .1f), V(.11f, .66f, -.1f), .14f, shorts);
            m.Beam(V(.11f, .66f, -.1f), V(.1f, .45f, .08f), .11f, skin);
            m.Beam(V(-.1f, 1f, .1f), V(-.11f, .78f, .12f), .14f, shorts);
            m.Beam(V(-.11f, .78f, .12f), V(-.1f, .5f, .02f), .11f, skin);
            m.Box(V(0, 1.28f, -.08f), V(.42f, .26f, .62f), jersey, Quaternion.Euler(-35f, 0, 0));
            m.Box(V(0, 1.31f, -.06f), V(.44f, .07f, .5f), R.cream, Quaternion.Euler(-35f, 0, 0));   // jersey stripe
            m.Beam(V(.2f, 1.42f, -.25f), V(.21f, 1.08f, -.48f), .1f, skin);
            m.Beam(V(-.2f, 1.42f, -.25f), V(-.21f, 1.08f, -.48f), .1f, skin);
            m.Box(V(0, 1.55f, -.38f), V(.22f, .22f, .22f), skin);
            m.Surface = ComicSurface.Plastic;
            m.Ellipsoid(V(0, 1.68f, -.36f), V(.3f, .2f, .36f), helmet);
            m.Box(V(0, 1.64f, -.36f), V(.32f, .06f, .38f), R.ink);
            return SaveWalker(PrefabFolder + "/PED_MountsBay_Cyclist.prefab", MeshFolder, m, V(0, .9f, 0), V(.6f, 1.8f, 1.8f), 3f, false, false);
        }

        /// <summary>A limestone river-wall kicker (generic ramp for this district).</summary>
        static GameObject RiverWallRamp()
        {
            const float width = 2.8f, rise = 5f, height = 1.3f;
            var m = new ComicMesh();
            RampBody(m, 0f, width, 0f, rise, height, 0f, Limestone, LimestoneDark, R.yellow, ComicSurface.Concrete);
            m.Surface = ComicSurface.Plain;
            for (float z = 1f; z < rise; z += 1f)
            {
                float y = height * z / rise + .01f;
                Face(m, V(-width * .5f + .05f, y, z - .03f), V(-width * .5f + .05f, y, z + .03f), V(width * .5f - .05f, y, z + .03f), V(width * .5f - .05f, y, z - .03f), LimestoneDark, Vector3.up, false);
            }
            return SaveRamp(PrefabFolder + "/RMP_MountsBay_RiverWall.prefab", MeshFolder, m, width, rise, height, 0f, 1f, .5f, Array.Empty<int>());
        }

        /// <summary>
        /// Far shore (follows the rider, no colliders): the wide river on the left with yachts, the South Perth shore with
        /// low apartment blocks and trees, sloped with the riverside road so it meets the chunks' water.
        /// </summary>
        static GameObject FarShore()
        {
            var m = new ComicMesh();
            var r = Rng(91);
            m.Surface = ComicSurface.Plain;
            for (float z = -60f; z < 260f; z += 20f)
            {
                Flat(m, -60f, -250f, z, z + 20f, WaterY - .15f, Water);
                for (int i = 0; i < 3; i++)
                {
                    float x = F(r, 70f, 230f), zz = z + F(r, 1f, 18f);
                    Flat(m, -x, -(x + F(r, 6f, 18f)), zz, zz + .5f, WaterY - .14f, i == 0 ? WaterFoam : WaterLight);
                }
                Flat(m, -250f, -320f, z, z + 20f, WaterY + .5f, new Color(.5f, .62f, .38f));
            }
            m.Surface = ComicSurface.Foliage;
            for (float z = -40f; z < 260f; z += 18f)
                m.Ellipsoid(V(-F(r, 255f, 275f), 4f, z), V(F(r, 14f, 22f), F(r, 8f, 12f), F(r, 12f, 18f)), Color.Lerp(GumLeafDark, Lawn, F(r, 0f, .5f)));
            m.Surface = ComicSurface.Concrete;
            for (int i = 0; i < 9; i++)
            {
                float h = F(r, 10f, 28f);
                m.Box(V(-F(r, 262f, 290f), h * .5f, F(r, -20f, 240f)), V(F(r, 10f, 16f), h, F(r, 10f, 16f)), Color.Lerp(R.cream, R.glass, F(r, 0f, .5f)));
            }
            for (int i = 0; i < 4; i++) Yacht(m, V(-F(r, 70f, 200f), WaterY - .15f, F(r, 0f, 220f)), F(r, 1.4f, 2f), i % 2 == 0 ? R.red : Color.white);
            m.Shear(Grade); // follows the riverside slope ahead of (and behind) the rider
            return SavePrefab(PrefabFolder + "/PRP_MountsBay_FarShore.prefab", MeshFolder, m, false, false, root =>
            {
                foreach (Collider col in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(col, true);
            });
        }
    }
}
