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
                d.chunks = chunks.ToArray(); d.orderedChunks = true; d.entry = entry; d.exit = exit; d.allowBoulevard = false;
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
                d.gateway = null;
                d.ambience = null; d.ambienceVolume = .35f;
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

        static void Yacht(ComicMesh m, Vector3 p, float s, Color hull)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, .35f * s, 0), V(1.4f * s, .7f * s, 4.2f * s), Color.white);
            m.Box(p + V(0, .62f * s, 0), V(1.42f * s, .12f * s, 4.22f * s), hull);
            m.Beam(p + V(0, .6f * s, .2f * s), p + V(0, 6.5f * s, .2f * s), .1f * s, R.ink);
            m.Surface = ComicSurface.Fabric;
            m.Tri(p + V(0, 1f * s, .35f * s), p + V(0, 6.3f * s, .35f * s), p + V(0, 1f * s, 2.2f * s), Color.white);
            m.Tri(p + V(0, 1f * s, 2.2f * s), p + V(0, 6.3f * s, .35f * s), p + V(0, 1f * s, .35f * s), Color.white);
        }

        static void Lamp(ComicMesh m, Vector3 p, int armSide)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .15f, 0), .2f, .3f, R.ink, 6);
            m.Box(p + V(0, 3.5f, 0), V(.13f, 7f, .13f), new Color(.28f, .32f, .36f));
            m.Box(p + V(armSide * .7f, 6.95f, 0), V(1.4f, .1f, .1f), new Color(.28f, .32f, .36f));
            m.Box(p + V(armSide * 1.35f, 6.85f, 0), V(.55f, .16f, .3f), R.cream);
        }

        // ------------------------------------------------------------------ the escarpment side (right)

        /// <summary>White retaining wall at the footpath, a bush ledge, then the stepped limestone cliff (Kings Park above).</summary>
        static void CliffSide(ChunkCtx c, float z0 = 0f, float z1 = 40f, float wall = 3f, float cliff = 24f, float cliffX = 12f)
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
            Cliff(c, z0, z1, cliffX, cliff);
        }

        static void Cliff(ChunkCtx c, float z0, float z1, float x0, float height)
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
                Gum(c.Deep, c.P(F(c.Rnd, x0 + 16f, x0 + 28f), height + .5f, z), F(c.Rnd, 13f, 17f), c.NextSeed(), true);
                c.Deep.Surface = ComicSurface.Foliage;
                c.Deep.Ellipsoid(c.P(x0 + 14f, height + 1.5f, z + 4f), V(8f, 4f, 7f), Color.Lerp(GumLeaf, BanksiaLeaf, F(c.Rnd, 0f, 1f)));
            }
        }

        // ------------------------------------------------------------------ chunks

        static RoadChunk Escarpment()
        {
            ChunkCtx c = New("Escarpment", River);
            RiverRoad(c);
            RiverSide(c);
            CliffSide(c);
            c.StandardSlots();
            return c.Save();
        }

        /// <summary>Near the city end: palms along both sides, apartment towers against the escarpment.</summary>
        static RoadChunk Towers()
        {
            ChunkCtx c = New("Towers", River);
            RiverRoad(c);
            RiverSide(c, 0f, 40f, false, true);
            Ground(c.RoadFlat, 1, Edge, 33f, .14f, Lawn, 0f, 40f);
            Wall(c.FrontFlat, Edge + .3f, .5f, .45f, 0f, 40f, WhiteWall, LimestoneCap);
            for (float z = 2f; z < 40f; z += 9f) Palm(c.Front, c.P(9.6f, .14f, z), F(c.Rnd, 9f, 11f), c.NextSeed());
            string[] ids = { "Tower", "Hotel", "Tower" };
            float[] lots = { 0f, 14f, 26f }, lens = { 14f, 12f, 14f };
            for (int i = 0; i < ids.Length; i++)
            {
                ComicAssetSpec spec = R.buildings.First(s => s.id == ids[i]);
                float zc = lots[i] + lens[i] * .5f, x = 14f + spec.depth * .5f;
                Vector3 p = c.P(x, .14f, zc);
                ComicMesh b = ComicKitGeometry.Building(spec, R);
                c.Front.Add(b, p, Quaternion.Euler(0, -90f, 0), V(lens[i] / spec.width, i == 1 ? 1.4f : 1f, 1f));
                c.Front.Surface = ComicSurface.Concrete;
                c.Front.Box(p + V(0, -1.1f, 0), V(spec.depth, 2.2f, lens[i]), R.ink);
            }
            Cliff(c, 0f, 40f, 32f, 30f);
            c.StandardSlots();
            return c.Save();
        }

        /// <summary>The Narrows Bridge freeway deck crossing high above the road on concrete piers (no colliders).</summary>
        static RoadChunk Narrows()
        {
            ChunkCtx c = New("Narrows", River);
            RiverRoad(c);
            RiverSide(c);
            CliffSide(c, 0f, 40f, 3f, 16f, 13f);
            ComicMesh m = c.Front;
            float zc = 21f, y = 9.4f;
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
            // Piers: clear of the rideable width (|x| > 8.1); in the river on the left.
            foreach (float x in new[] { -11.2f, 11.4f, -40f, 36f, -78f, -118f })
            {
                float baseY = x < 0f ? WaterY : 3f;
                float h = y - .65f - baseY;
                Vector3 b = c.P(x, baseY + h * .5f, zc);
                m.Box(b + V(0, 0, -4.5f), V(1.8f, h, 1.8f), Concrete);
                m.Box(b + V(0, 0, 4.5f), V(1.8f, h, 1.8f), Concrete);
                m.Box(c.P(x, y - 1.3f, zc), V(2.6f, 1.1f, 13f), ConcreteDark);
            }
            // Freeway traffic on the deck (driving across).
            Color[] cars = { R.red, R.yellow, new Color(.2f, .5f, .85f), Color.white };
            for (int i = 0; i < 4; i++)
                m.Add(ComicKitGeometry.Car(R, cars[i]), c.P(-30f + i * 17f, y + .65f, zc + (i % 2 == 0 ? -3f : 3f)), Quaternion.Euler(0, i % 2 == 0 ? 90f : -90f, 0), Vector3.one);
            c.StandardSlots();
            return c.Save();
        }

        /// <summary>Jacob's Ladder: the steep white staircase up the cliff, joggers climbing it.</summary>
        static RoadChunk JacobsLadder()
        {
            ChunkCtx c = New("JacobsLadder", River);
            RiverRoad(c);
            RiverSide(c);
            const float x0 = 9.2f, x1 = 11.6f, zs = 6f, ze = 37f, top = 23f;
            // White wall with a gap for the stairs; the cliff stands back behind them.
            Wall(c.FrontFlat, Edge + .35f, .6f, .6f, 0f, zs, WhiteWall, new Color(.86f, .85f, .8f), -.6f, 8f);
            Wall(c.FrontFlat, Edge + .35f, .6f, .6f, zs + 2f, 40f, WhiteWall, new Color(.86f, .85f, .8f), -.6f, 8f);
            Ground(c.RoadFlat, 1, Edge, 13f, .16f, BushFloor, 0f, 40f);
            Cliff(c, 0f, 40f, 13f, top + 1f);
            ComicMesh m = c.Front;
            int steps = 40;
            float run = (ze - zs) / steps, rise = top / steps;
            m.Surface = ComicSurface.Concrete;
            for (int i = 0; i < steps; i++)
            {
                float z = zs + (i + .5f) * run, yTop = (i + 1) * rise;
                Vector3 p = c.P((x0 + x1) * .5f, .16f + yTop - .6f, z);
                m.Box(p, V(x1 - x0, 1.2f, run + .01f), i % 10 == 9 ? LimestoneCap : WhiteWall);
            }
            // Handrails (dark green, like the real ones) and posts.
            Color rail = new Color(.18f, .35f, .25f);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { x0 + .08f, x1 - .08f })
            {
                m.Beam(c.P(x, 1.15f, zs), c.P(x, top + 1.15f, ze), .08f, rail);
                for (int i = 0; i <= steps; i += 5)
                {
                    float z = zs + i * run;
                    m.Box(c.P(x, i * rise + .6f, z), V(.07f, 1.1f, .07f), rail);
                }
            }
            // Climbers (decor, out of reach).
            Color[] tops = { new Color(1f, .42f, .62f), R.yellow, new Color(.3f, .7f, .9f) };
            for (int i = 0; i < 3; i++)
            {
                int k = 8 + i * 11;
                Climber(c.Props, c.P(x0 + .7f + i % 2 * .9f, .16f + (k + 1) * rise, zs + (k + .5f) * run), tops[i]);
            }
            // On two white piers so it shows over the tall retaining wall of the chunk before.
            NameBlock(c.Front, c.P(12.6f, 2.3f, 3.5f), "JACOBS LADDER", .11f, WhiteWall, new Color(.18f, .35f, .25f));
            foreach (float px in new[] { 10.3f, 14.9f }) c.Front.Box(c.P(px, .9f, 3.5f), V(.6f, 2.2f, .5f), WhiteWall);
            c.StandardSlots();
            return c.Save();
        }

        static void Climber(ComicMesh m, Vector3 p, Color top)
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
            ChunkCtx c = New("Inlet", River);
            float zr = InletRampZ, zl = zr + InletRise, zg = zl + InletGap;
            c.Ramps.Add(new RampSpec { X = 0f, Z = zr, Width = 2f * Edge, Rise = InletRise, Height = InletHeight, Top = 0f, Launch = 1f, Hang = .5f });
            c.Gaps.Add(new GapSpec { X = 0f, Z = zl, Length = InletGap, Width = 0f, Word = "SPLASH", Water = true });
            RiverRoad(c);
            RiverSide(c, 0f, zl);
            RiverSide(c, zg, 40f);
            RiverWater(c, zl, zg, -14f, 90f);         // the inlet: water across the road and out to the river
            Ground(c.RoadFlat, 1, Edge, 14f, .14f, Lawn, 0f, zl);
            Ground(c.RoadFlat, 1, Edge, 14f, .14f, Lawn, zg, 40f);
            Wall(c.FrontFlat, Edge + .3f, .5f, .5f, 0f, zl, WhiteWall, LimestoneCap);
            Wall(c.FrontFlat, Edge + .3f, .5f, .5f, zg, 40f, WhiteWall, LimestoneCap);
            Cliff(c, 0f, 40f, 16f, 18f);
            ComicMesh f = c.FrontFlat;
            // Limestone sea walls on both banks of the inlet, a dark culvert under the cliff.
            f.Surface = ComicSurface.Concrete;
            f.Box(V(0f, (WaterY - 1.02f) * .5f, zl - .3f), V(2f * Edge + 4f, 1.02f - WaterY, .6f), LimestoneDark);
            f.Box(V(0f, (WaterY - 1.02f) * .5f, zg + .3f), V(2f * Edge + 4f, 1.02f - WaterY, .6f), LimestoneDark);
            f.Box(V(15f, 1.2f, (zl + zg) * .5f), V(2.5f, 4.6f, InletGap + 2f), Limestone);
            f.Box(V(13.7f, .1f, (zl + zg) * .5f), V(.2f, 2.2f, InletGap - .6f), R.ink);
            for (int i = 0; i < 8; i++)
            {
                float x = F(c.Rnd, -8f, 12f), z = i % 2 == 0 ? zl + .5f : zg - .5f;
                f.Surface = ComicSurface.Foliage;
                f.Box(V(x, WaterY + .5f, z), V(.08f, 1.2f, .08f), new Color(.45f, .6f, .25f), Quaternion.Euler(F(c.Rnd, -10f, 10f), 0, F(c.Rnd, -10f, 10f)));
            }
            // The river-wall ramp: limestone blocks across the road and both paths, chevrons at the lip.
            RampBody(f, 0f, 2f * Edge, zr, InletRise, InletHeight, 0f, Limestone, LimestoneDark, R.yellow, ComicSurface.Concrete);
            f.Surface = ComicSurface.Plain;
            for (float x = -7f; x <= 7f; x += 3.5f)
                for (int k = 0; k < 2; k++)
                {
                    float z = zr + 1.4f + k * 1.6f, y = InletHeight * (z - zr) / InletRise + .02f;
                    f.Tri(V(x - .8f, y, z), V(x, y + .08f, z + .9f), V(x + .8f, y, z), R.yellow);
                }
            // No spawn slots: nothing on the run-up, the jump or the landing.
            return c.Save();
        }

        /// <summary>The red-brick Old Swan Brewery on the river bank and the steel footbridge over the road.</summary>
        static RoadChunk Brewery()
        {
            ChunkCtx c = New("Brewery", River);
            RiverRoad(c);
            Ground(c.RoadFlat, -1, Edge, 11f, .12f, Lawn, 0f, 40f);
            Ground(c.RoadFlat, -1, 11f, 34f, .3f, R.pavement, 0f, 40f, ComicSurface.Paving);
            RiverWater(c, 0f, 40f, 33.5f, 90f);
            for (float z = 2f; z < 40f; z += 3f) Boulder(c.Props, c.P(-F(c.Rnd, 33f, 34.5f), -.6f, z), 1f, c.Rnd);
            ComicMesh m = c.Front;
            // Main hall, wings, the tall tower, arched windows (ink), cream trim.
            Vector3 b = c.P(-22f, .3f, 18f);
            m.Surface = ComicSurface.Brick;
            m.Box(b + V(0, 5f, 0), V(14f, 10f, 26f), Brick);
            m.Box(b + V(0, 10.6f, 0), V(14.4f, 1.2f, 26.4f), BrickDark);
            m.Box(b + V(2f, 7f, 15.5f), V(10f, 14f, 5f), Brick);
            m.Box(b + V(1f, 9f, -15f), V(8f, 18f, 6f), Brick);
            m.Box(b + V(1f, 18.6f, -15f), V(8.6f, 1.2f, 6.6f), BrickDark);
            m.Box(b + V(1f, 20f, -15f), V(5f, 2.2f, 4f), R.cream);
            m.Surface = ComicSurface.Plain;
            for (int level = 0; level < 3; level++)
                for (int i = 0; i < 6; i++)
                {
                    float z = b.z - 10f + i * 4f, y = b.y + 1.6f + level * 3f;
                    Face(m, V(b.x + 7.02f, y, z - .7f), V(b.x + 7.02f, y, z + .7f), V(b.x + 7.02f, y + 1.9f, z + .7f), V(b.x + 7.02f, y + 1.9f, z - .7f), R.ink, Vector3.right, false);
                    Face(m, V(b.x + 7.04f, y + 1.9f, z - .8f), V(b.x + 7.04f, y + 1.9f, z + .8f), V(b.x + 7.04f, y + 2.15f, z + .8f), V(b.x + 7.04f, y + 2.15f, z - .8f), R.cream, Vector3.right, false);
                }
            for (int level = 0; level < 5; level++)
            {
                float y = b.y + 2f + level * 3.2f, z = b.z - 15f;
                Face(m, V(b.x + 5.02f, y, z - .8f), V(b.x + 5.02f, y, z + .8f), V(b.x + 5.02f, y + 2f, z + .8f), V(b.x + 5.02f, y + 2f, z - .8f), R.ink, Vector3.right, false);
            }
            for (float z = 6f; z < 40f; z += 14f) Palm(c.Front, c.P(-12f, .3f, z), F(c.Rnd, 8f, 10f), c.NextSeed());
            CliffSide(c, 0f, 40f, 3f, 22f, 12f);
            // Footbridge: a dark steel truss from the brewery side up to the cliff, high over the road.
            const float zb = 33f, yb = 7.6f, xa = -14f, xb = 12.5f;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float dz in new[] { -1.3f, 1.3f })
            {
                m.Beam(c.P(xa, yb, zb + dz), c.P(xb, yb, zb + dz), .25f, Truss);
                m.Beam(c.P(xa, yb + 2.2f, zb + dz), c.P(xb, yb + 2.2f, zb + dz), .25f, Truss);
                for (int i = 0; i < 10; i++)
                {
                    float x0 = Mathf.Lerp(xa, xb, i / 10f), x1 = Mathf.Lerp(xa, xb, (i + 1) / 10f);
                    m.Beam(c.P(x0, yb, zb + dz), c.P(x1, yb + 2.2f, zb + dz), .14f, Truss);
                    m.Box(c.P(x0, yb + 1.1f, zb + dz), V(.14f, 2.2f, .14f), Truss);
                }
            }
            m.Box(c.P((xa + xb) * .5f, yb - .1f, zb), V(xb - xa, .25f, 2.8f), ConcreteDark);
            foreach (float x in new[] { -10.5f, 9.4f })
            {
                float baseY = x > 0f ? 3f : .3f;
                m.Box(c.P(x, (yb + baseY) * .5f, zb), V(.7f, yb - baseY, .7f), Truss);
            }
            c.StandardSlots();
            return c.Save();
        }

        static RoadChunk Entry()
        {
            ChunkCtx c = New("Entry", Blend);
            RiverRoad(c, 8f, 40f);
            // Calm at z = 0: low walls and hedges on both sides; the river opens up after.
            for (int side = -1; side <= 1; side += 2)
            {
                Wall(c.FrontFlat, side * (Edge + .3f), .5f, .45f, 0f, 14f, Limestone, LimestoneCap);
                for (float z = 1f; z < 12f; z += 2.6f) Shrub(c.Props, c.P(side * 10f, .14f, z), 1.2f, Color.Lerp(LeafGreen, Lawn, .3f));
            }
            Ground(c.RoadFlat, -1, Edge, 40f, .12f, Lawn, 0f, 12f);
            RiverSide(c, 12f, 40f, true, true);
            RiverWater(c, 0f, 12f, 40f, 90f);
            Ground(c.RoadFlat, 1, Edge, 22f, .14f, Lawn, 0f, 14f);
            RoundTree(c.Front, c.P(-16f, .12f, 5f), 8f, Color.Lerp(Lawn, LeafGreen, .5f), 3);
            RoundTree(c.Front, c.P(14f, .14f, 6f), 8.5f, Color.Lerp(Lawn, LeafGreen, .5f), 4);
            CliffSide(c, 14f, 40f, 3f, 20f, 12f);
            Cliff(c, 0f, 14f, 20f, 16f);
            NameBlock(c.Front, c.P(13.4f, .14f, 12f), "MOUNTS BAY RD", .12f, WhiteWall, Water);
            c.StandardSlots();
            return c.Save();
        }

        static RoadChunk Exit()
        {
            ChunkCtx c = New("Exit", Blend);
            RiverRoad(c, 0f, 32f);
            RiverSide(c, 0f, 26f);
            Ground(c.RoadFlat, -1, Edge, 40f, .12f, Lawn, 26f, 40f);
            RiverWater(c, 26f, 40f, 40f, 90f);
            CliffSide(c, 0f, 26f, 3f, 18f, 12f);
            Ground(c.RoadFlat, 1, Edge, 40f, .14f, Lawn, 26f, 40f);
            Cliff(c, 26f, 40f, 24f, 14f);
            for (int side = -1; side <= 1; side += 2)
            {
                Wall(c.FrontFlat, side * (Edge + .3f), .5f, .45f, 26f, 40f, Limestone, LimestoneCap);
                for (float z = 28f; z < 40f; z += 2.6f) Shrub(c.Props, c.P(side * 10f, .14f, z), 1.2f, Color.Lerp(LeafGreen, Lawn, .3f));
                RoundTree(c.Front, c.P(side * 15f, .14f, 33f), 9f, Color.Lerp(Lawn, LeafGreen, .5f), 50 + side);
            }
            c.StandardSlots();
            return c.Save();
        }

        // ------------------------------------------------------------------ obstacles, people, animals, ramp, far shore

        /// <summary>A Perth council wheelie bin (dark green, yellow recycling lid) left on the road.</summary>
        static GameObject WheelieBin()
        {
            var m = new ComicMesh();
            Color body = new Color(.16f, .38f, .24f), lid = new Color(1f, .8f, .1f);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .52f, 0), V(.62f, .98f, .72f), body);
            m.Box(V(0, 1.05f, -.02f), V(.68f, .08f, .8f), lid);
            m.Box(V(0, 1f, .4f), V(.5f, .08f, .08f), R.ink);
            m.Box(V(0, .7f, -.37f), V(.3f, .2f, .02f), Color.white);
            m.Surface = ComicSurface.Rubber;
            for (int s = -1; s <= 1; s += 2) m.Cylinder(V(s * .3f, .12f, .3f), .12f, .08f, R.ink, 8, 1f, Quaternion.Euler(0, 0, 90f));
            return SavePrefab(PrefabFolder + "/OBS_MountsBay_WheelieBin.prefab", MeshFolder, m, true, true, root =>
            {
                Box(root, V(0, .55f, 0), V(.7f, 1.1f, .8f));
                var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
                o.kind = ObstacleKind.Thing; o.canJumpOver = true; o.spansRoad = false; o.punchable = true; o.animal = false; o.police = false; o.spawnWeight = 1.2f;
            });
        }

        /// <summary>Australian pelican: white with black wings, a huge pale-pink bill. Animal, not punchable.</summary>
        static GameObject Pelican()
        {
            var m = new ComicMesh();
            Color white = new Color(.97f, .97f, .95f), black = new Color(.12f, .12f, .14f), bill = new Color(.98f, .78f, .7f), legs = new Color(.55f, .6f, .68f);
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(V(0, .62f, .12f), V(.55f, .5f, .85f), white);
            m.Box(V(.26f, .66f, .26f), V(.08f, .32f, .6f), black, Quaternion.Euler(-10f, 0, 0));
            m.Box(V(-.26f, .66f, .26f), V(.08f, .32f, .6f), black, Quaternion.Euler(-10f, 0, 0));
            m.Box(V(0, .7f, .58f), V(.3f, .12f, .3f), black, Quaternion.Euler(-20f, 0, 0));
            m.Beam(V(0, .8f, -.15f), V(0, 1.12f, -.25f), .16f, white);
            m.Ellipsoid(V(0, 1.18f, -.3f), V(.22f, .22f, .26f), white);
            m.Box(V(.07f, 1.22f, -.38f), V(.02f, .04f, .04f), R.ink);
            m.Box(V(-.07f, 1.22f, -.38f), V(.02f, .04f, .04f), R.ink);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, 1.13f, -.68f), V(.1f, .07f, .6f), bill, Quaternion.Euler(14f, 0, 0));
            m.Box(V(0, 1.03f, -.62f), V(.12f, .14f, .48f), Color.Lerp(bill, R.yellow, .3f), Quaternion.Euler(18f, 0, 0));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(.1f, .2f, .1f), V(.06f, .4f, .06f), legs);
            m.Box(V(-.1f, .2f, .1f), V(.06f, .4f, .06f), legs);
            m.Box(V(.1f, .02f, .02f), V(.16f, .04f, .2f), legs);
            m.Box(V(-.1f, .02f, .02f), V(.16f, .04f, .2f), legs);
            return SaveWalker(PrefabFolder + "/PED_MountsBay_Pelican.prefab", MeshFolder, m, V(0, .65f, 0), V(.7f, 1.3f, 1.4f), .4f, true, true);
        }

        /// <summary>A cyclist on the shared path: helmet (it's WA), road bike, bright jersey. A person (not punchable).</summary>
        static GameObject Cyclist()
        {
            var m = new ComicMesh();
            Color jersey = new Color(.1f, .62f, .6f), shorts = R.ink, skin = new Color(.86f, .64f, .5f), helmet = R.yellow, frame = R.red;
            m.Surface = ComicSurface.Rubber;
            foreach (float z in new[] { -.55f, .55f }) m.Cylinder(V(0, .36f, z), .34f, .05f, R.ink, 10, 1f, Quaternion.Euler(0, 0, 90f));
            m.Surface = ComicSurface.PaintedMetal;
            m.Beam(V(0, .36f, .55f), V(0, .62f, .05f), .05f, frame);
            m.Beam(V(0, .62f, .05f), V(0, .92f, .12f), .05f, frame);
            m.Beam(V(0, .62f, .05f), V(0, .95f, -.45f), .05f, frame);
            m.Beam(V(0, .95f, -.45f), V(0, .36f, -.55f), .05f, frame);
            m.Beam(V(0, .92f, .12f), V(0, .95f, -.45f), .05f, frame);
            m.Box(V(0, 1.06f, -.5f), V(.46f, .04f, .04f), R.ink);
            m.Box(V(0, .96f, .14f), V(.12f, .05f, .26f), R.ink);
            m.Surface = ComicSurface.Fabric;
            m.Beam(V(.1f, 1f, .1f), V(.11f, .66f, -.1f), .14f, shorts);
            m.Beam(V(.11f, .66f, -.1f), V(.1f, .45f, .08f), .11f, skin);
            m.Beam(V(-.1f, 1f, .1f), V(-.11f, .78f, .12f), .14f, shorts);
            m.Beam(V(-.11f, .78f, .12f), V(-.1f, .5f, .02f), .11f, skin);
            m.Box(V(0, 1.28f, -.08f), V(.42f, .26f, .62f), jersey, Quaternion.Euler(-35f, 0, 0));
            m.Beam(V(.2f, 1.42f, -.25f), V(.21f, 1.08f, -.48f), .1f, skin);
            m.Beam(V(-.2f, 1.42f, -.25f), V(-.21f, 1.08f, -.48f), .1f, skin);
            m.Box(V(0, 1.55f, -.38f), V(.22f, .22f, .22f), skin);
            m.Surface = ComicSurface.Plastic;
            m.Ellipsoid(V(0, 1.68f, -.36f), V(.3f, .2f, .36f), helmet);
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
