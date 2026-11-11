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
    /// MOUNTS BAY RD (v0.3 district, order 70; polished in v0.6): the road squeezed between the Kings Park escarpment
    /// (white limestone retaining walls, a steep bush slope with limestone outcrops, grass trees, the park's gums on
    /// top, Jacob's Ladder, the State War Memorial obelisk) and the Swan River (red shared path, rock revetment, wide
    /// blue water, yachts, black swans, a jetty). Near the city end: palms and apartment towers, the freeway interchange
    /// deck overhead; further on the red-brick Old Swan Brewery (gables, tile roofs, stair tower, chimney) with its
    /// footbridge to the car park in the escarpment, and a small inlet the road jumps over (river-wall ramp + SPLASH gap).
    /// The entry continues Elizabeth Quay's waterfront (water on both sides, then the bank and the escarpment rise on
    /// the right); the exit is the Narrows Bridge approach: the land ends at an abutment, the freeway and the railway
    /// arrive on the left, the path runs on over the water behind a bridge balustrade.
    ///   Menu: CityScooter/Districts/Build Mounts Bay Rd
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.MountsBayBuilder.BuildBatch
    /// The ride goes west along the river from the city: the escarpment is on the right, the river on the left.
    /// </summary>
    public static class MountsBayBuilder
    {
        public const string Id = "mountsbay", Name = "MountsBay";
        public const string PrefabFolder = ComicCityBuilder.Prefabs + "/Districts/MountsBay";
        public const string MeshFolder = ComicCityBuilder.Root + "/Districts/MountsBay";
        public const string AssetPath = DistrictData + "/District_MountsBay.asset";
        /// <summary>The far scenery shared with Elizabeth Quay (one river, one South Perth shore: v0.6 rule 7).</summary>
        public const string FarShorePath = PrefabFolder + "/PRP_MountsBay_FarShore.prefab";
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
        static readonly Color Tile = new Color(.5f, .29f, .24f), TileDark = new Color(.4f, .23f, .2f), RailGrey = new Color(.6f, .63f, .65f),
            SwanBlack = new Color(.13f, .13f, .15f), Timber = new Color(.54f, .36f, .24f), SignGreen = new Color(.1f, .42f, .3f),
            SignBlue = new Color(.16f, .36f, .62f), Ballast = new Color(.5f, .47f, .44f);

        [MenuItem("CityScooter/Districts/Build Mounts Bay Rd")]
        public static void Build()
        {
            Init();
            PrefabBuildUtility.EnsureFolder(PrefabFolder);
            PrefabBuildUtility.EnsureFolder(MeshFolder);

            RoadChunk entry = Entry();
            // v0.6: the district (440-600 m) is longer than six chunks, so the cycle used to start again and the rider
            // passed a second Jacob's Ladder, a second brewery and a second freeway bridge. Now twelve chunks in the real
            // order from the city (convention centre, freeway interchange, apartment towers, Jacob's Ladder, the war
            // memorial above the road, the Kokoda Track walk at Kennedy Fountain, the brewery, Eliza in the river), with
            // the SAME gameplay as before: every chunk is a plain 4-lane chunk except the inlet jump, which is still the
            // 5th of every six (the same prefab each time).
            // The longest rides lay a 13th chunk: the cycle goes on with plain bush stretches (not the convention centre
            // again) before the exit to the bridge.
            RoadChunk inlet = Inlet(), bushA = Bush("BushA", 0), bushB = Bush("BushB", 1);
            var chunks = new List<RoadChunk>
            {
                Convention(), Narrows(), Towers(), JacobsLadder(), inlet, Escarpment(),
                Kokoda(), Brewery(), Eliza(), bushA, inlet, bushB,
                bushA, bushB, bushA, bushB, inlet, bushA,
            };
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
            Debug.Log("[MountsBay] built " + (chunks.Distinct().Count() + 2) + " chunks: " + string.Join("; ", Report));
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

        static void Tri(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, Color color, Vector3 normal)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) m.Tri(a, c, b, color); else m.Tri(a, b, c, color);
        }

        // ------------------------------------------------------------------ the river side (left)

        static void RiverSide(ChunkCtx c, float z0 = 0f, float z1 = 40f, bool lamps = true, bool palms = false, bool trees = false)
        {
            // Lawn strip, rock revetment, water (sheared with the road so it meets the next chunk's water).
            Ground(c.RoadFlat, -1, Edge, 10.5f, .12f, Lawn, z0, z1);
            Bank(c.RoadFlat, -1, 10.5f, .12f, 13.5f, WaterY + .1f, Rock, z0, z1, ComicSurface.Concrete);
            RiverWater(c, z0, z1, 12.5f, 90f);
            for (float z = z0 + 1f; z < z1 - .5f; z += 1.6f)
                Boulder(c.Props, c.P(-F(c.Rnd, 10.8f, 13f), F(c.Rnd, -.7f, -.1f), z), F(c.Rnd, .7f, 1.2f), c.Rnd);
            if (lamps) for (float z = z0 + 8f; z < z1; z += 20f) Lamp(c.Props, c.P(-8.7f, .12f, z), 1);
            if (palms) for (float z = z0 + 4f; z < z1; z += 9f) Palm(c.Front, c.P(-9.6f, .12f, z), F(c.Rnd, 8f, 10f), c.NextSeed());
            // River-bank trees (peppermints and sheoaks): two kinds, never a row of one.
            if (trees)
                for (float z = z0 + 15f; z < z1 - 2f; z += 17f)
                    RoundTree(c.Front, c.P(-9.7f, .12f, z), F(c.Rnd, 5.5f, 7f), Color.Lerp(GumLeafDark, Lawn, F(c.Rnd, .1f, .6f)), c.NextSeed());
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

        /// <summary>A black swan on the water (the river's emblem): black body and raised wing feathers, a curved neck of
        /// three round pieces, red bill with a pale tip. Decoration out on the river.</summary>
        static void Swan(ComicMesh m, Vector3 p, float yaw, float s = 1f)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.Fabric;
            m.Ellipsoid(p + q * V(0, .22f * s, 0), V(.62f * s, .5f * s, 1.15f * s), SwanBlack);
            m.Box(p + q * V(0, .42f * s, .32f * s), V(.4f * s, .2f * s, .5f * s), Color.Lerp(SwanBlack, R.ink, .4f), q * Quaternion.Euler(-18f, 0, 0));
            m.Box(p + q * V(0, .34f * s, .58f * s), V(.3f * s, .08f * s, .16f * s), Color.white, q);
            Vector3 a = p + q * V(0, .35f * s, -.42f * s), b = p + q * V(0, .8f * s, -.5f * s), c = p + q * V(0, 1.12f * s, -.62f * s), d = p + q * V(0, 1.02f * s, -.82f * s);
            Rod(m, a, b, .085f * s, SwanBlack, 1f, 6); Rod(m, b, c, .075f * s, SwanBlack, 1f, 6); Rod(m, c, d, .08f * s, SwanBlack, 1f, 6);
            m.Surface = ComicSurface.Plastic;
            m.Box(d + q * V(0, -.05f * s, -.12f * s), V(.09f * s, .08f * s, .22f * s), new Color(.86f, .16f, .14f), q * Quaternion.Euler(20f, 0, 0));
            m.Box(d + q * V(0, -.1f * s, -.24f * s), V(.08f * s, .05f * s, .06f * s), Color.white, q);
        }

        /// <summary>A small timber jetty out from the bank with mooring posts and a dinghy tied to it.</summary>
        static void Jetty(ChunkCtx c, float z, float len = 12f)
        {
            ComicMesh m = c.Front;
            float x0 = 12.6f, x1 = x0 + len, y = -.25f;
            m.Surface = ComicSurface.Wood;
            m.Box(c.P(-(x0 + x1) * .5f, y, z), V(len, .16f, 2.2f), Timber);
            for (float x = x0 + .6f; x < x1; x += 1.2f) m.Box(c.P(-x, y + .085f, z), V(.06f, .02f, 2.2f), Color.Lerp(Timber, R.ink, .35f));
            for (float x = x0 + 1f; x <= x1; x += 3.6f)
                foreach (float dz in new[] { -1.15f, 1.15f })
                {
                    m.Surface = ComicSurface.Wood;
                    m.Cylinder(c.P(-x, -.55f, z + dz), .14f, 2.1f, Color.Lerp(Timber, R.ink, .3f), 8);
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Cylinder(c.P(-x, .55f, z + dz), .16f, .1f, Color.white, 8);
                }
            // dinghy
            Vector3 b = c.P(-(x1 - 2f), WaterY, z + 2.6f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(b + V(0, .25f, 0), V(1.3f, .5f, 3.2f), Color.white);
            m.Box(b + V(0, .44f, 0), V(1.34f, .12f, 3.24f), new Color(.2f, .45f, .8f));
            m.Box(b + V(0, .5f, 0), V(.9f, .06f, 2.4f), R.ink);
        }

        /// <summary>Limestone quay wall with the grey mesh railing of Elizabeth Quay along the footpath's outer edge
        /// (following the edge where the road changes width): the river comes right up to the path.</summary>
        static void QuayEdge(ChunkCtx c, int side, float z0, float z1, bool rail = true)
        {
            ComicMesh m = c.FrontFlat;
            Color panel = Color.Lerp(RailGrey, R.cream, .35f);
            for (float z = z0; z < z1 - .001f; z += 2f)
            {
                float e = Mathf.Min(z1, z + 2f);
                Vector3 a = V(side * (c.EdgeAt(z) + .12f), 0f, z), b = V(side * (c.EdgeAt(e) + .12f), 0f, e), d = b - a;
                Quaternion q = Quaternion.LookRotation(d);
                m.Surface = ComicSurface.Concrete;
                m.Box((a + b) * .5f + V(0, (Kerb + .06f + WaterY - 1f) * .5f, 0), V(.28f, Kerb + .06f - (WaterY - 1f), d.magnitude + .02f), LimestoneCap, q);
                if (!rail) continue;
                m.Surface = ComicSurface.Metal;
                Vector3 off = V(side * .18f, 0, 0);
                m.Box(a + off + V(0, Kerb + .55f, 0), V(.08f, 1.1f, .08f), RailGrey);
                m.Box((a + b) * .5f + off + V(0, Kerb + 1.11f, 0), V(.12f, .1f, d.magnitude), RailGrey, q);
                m.Box((a + b) * .5f + off + V(0, Kerb + .58f, 0), V(.06f, .85f, d.magnitude), panel, q);
            }
        }

        // ------------------------------------------------------------------ the escarpment side (right)

        /// <summary>Grass tree read from a distance: black trunk, a hanging skirt of dry leaves, a green tuft opening
        /// upwards and a flower spear. A few big closed shapes (the kit's balga of thin leaves turns into an ink blot
        /// 15 m away).</summary>
        static void GrassTree(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = Rng(seed);
            float trunk = s * F(r, .9f, 1.5f);
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, trunk * .5f, 0), .2f * s, trunk, BalgaTrunk, 6, .9f);
            m.Surface = ComicSurface.Foliage;
            m.Cylinder(p + V(0, trunk - .2f * s, 0), .62f * s, .8f * s, new Color(.62f, .54f, .32f), 8, .35f);
            m.Ellipsoid(p + V(0, trunk + .42f * s, 0), V(1.35f * s, 1f * s, 1.35f * s), new Color(.47f, .64f, .26f));
            m.Surface = ComicSurface.Wood;
            Rod(m, p + V(0, trunk + .6f * s, 0), p + V(0, trunk + 2.2f * s, 0), .06f * s, Spear, 1f, 6);
            m.Cylinder(p + V(0, trunk + 1.85f * s, 0), .12f * s, .65f * s, SpearFlower, 6, .8f);
        }

        /// <summary>
        /// The Kings Park escarpment (Mount Eliza), in the 2-lane frame (Side moves it out to the wide road): the white
        /// limestone retaining wall at the footpath with buttresses, a bush ledge, then a steep scrub slope up to the
        /// park (three facets, the colour changing every few metres), limestone outcrops breaking through it, shrubs,
        /// grass trees and banksias near the road, gums on the slope and the park's tall gums on top.
        ///   wall / top: height of the retaining wall and of the park above the road; x0, run: where the slope starts and
        ///   how far back it goes; rise(z): 0..1 scale of the whole profile along z (the hill growing or sinking);
        ///   bulge: 1 = a convex hill (steep at the foot), 0 = a straight slope; wallGap / gap: z ranges without the
        ///   retaining wall and without ledge / slope (Jacob's Ladder climbs there); noTall: no tall trees in this z range;
        ///   keepOut(x, z): nothing is planted where it returns true.
        /// </summary>
        static void Scarp(ChunkCtx c, float z0, float z1, float wall = 3f, float top = 24f, float x0 = 12f, float run = 24f,
            Func<float, float> rise = null, float bulge = 1f, Vector2? wallGap = null, Vector2? gap = null, Vector2? noTall = null,
            Func<float, float, bool> keepOut = null, float rocks = 1f, float bush = 1f)
        {
            rise = rise ?? (z => 1f);
            float[] ax = { 0f, .3f, .64f, 1f };
            float[] ay = { 0f, Mathf.Lerp(.3f, .44f, bulge), Mathf.Lerp(.64f, .8f, bulge), 1f };
            float W(float z) => wall * rise(z);
            float T(float z) => Mathf.Max(W(z), top * rise(z));
            // Height of the hill at (x, z), above the road.
            float H(float x, float z)
            {
                float w = W(z), t = T(z);
                if (x <= x0) return w;
                float u = Mathf.Clamp01((x - x0) / run);
                for (int k = 0; k < 3; k++)
                    if (u <= ax[k + 1]) return w + (t - w) * Mathf.Lerp(ay[k], ay[k + 1], Mathf.InverseLerp(ax[k], ax[k + 1], u));
                return t;
            }
            bool In(Vector2? range, float a, float b) => range.HasValue && b > range.Value.x + .001f && a < range.Value.y - .001f;
            ComicMesh f = c.FrontFlat, g = c.RoadFlat;
            Color cap = new Color(.86f, .85f, .8f), baseCourse = Color.Lerp(WhiteWall, LimestoneDark, .45f);
            Color[] scrub = { new Color(.33f, .42f, .24f), new Color(.38f, .44f, .26f), new Color(.43f, .46f, .28f), new Color(.36f, .39f, .23f), new Color(.47f, .45f, .3f) };
            var cuts = new List<float>();
            for (float z = z0; z < z1 - .001f; z += 4f) cuts.Add(z);
            cuts.Add(z1);
            foreach (Vector2? range in new[] { wallGap, gap })
                if (range.HasValue) { cuts.Add(range.Value.x); cuts.Add(range.Value.y); }
            cuts = cuts.Where(z => z >= z0 - .001f && z <= z1 + .001f).Distinct().OrderBy(z => z).ToList();
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float z = cuts[i], e = cuts[i + 1];
                if (e - z < .01f) continue;
                float w0 = W(z), w1 = W(e), t0 = T(z), t1 = T(e);
                bool walled = !In(wallGap, z, e), sloped = !In(gap, z, e);
                if (walled && Mathf.Max(w0, w1) > .35f)
                {
                    f.Surface = ComicSurface.Concrete;
                    SlopeBox(f, Edge + .05f, Edge + .65f, z, e, -.6f, w0, -.6f, w1, WhiteWall);
                    SlopeBox(f, Edge - .01f, Edge + .71f, z, e, w0, w0 + .12f, w1, w1 + .12f, cap);
                    SlopeBox(f, Edge + .0f, Edge + .06f, z, e, -.6f, Mathf.Min(.5f, w0), -.6f, Mathf.Min(.5f, w1), baseCourse);
                    // creepers hanging over the cap now and then
                    if (Mathf.RoundToInt(z / 4f) % 3 == 1 && w0 > 2f && e - z > 3f)
                    {
                        Color ivy = Color.Lerp(LeafGreen, GumLeaf, F(c.Rnd, 0f, 1f));
                        c.Props.Surface = ComicSurface.Foliage;
                        c.Props.Ellipsoid(c.P(Edge + .3f, w0 + .15f, z + 2f), V(1.5f, .9f, F(c.Rnd, 2.6f, 3.6f)), ivy);
                        c.Props.Box(c.P(Edge - .02f, w0 - .55f, z + 2f), V(.14f, 1.3f, F(c.Rnd, 1.2f, 2f)), Color.Lerp(ivy, R.ink, .12f));
                        c.Props.Box(c.P(Edge - .02f, w0 - .35f, z + 3.2f), V(.14f, .8f, .8f), ivy);
                    }
                    // a buttress and a weep hole every 8 m: the wall reads as built, not as one white slab
                    if (Mathf.RoundToInt(z / 4f) % 2 == 0 && w0 > 1.2f)
                    {
                        f.Box(V(Edge + .02f, w0 * .5f - .3f, z + .25f), V(.3f, w0 + .6f, .5f), Color.Lerp(WhiteWall, LimestoneDark, .2f));
                        f.Surface = ComicSurface.Plain;
                        Face(f, V(Edge + .04f, .6f, z + 2f), V(Edge + .04f, .6f, z + 2.25f), V(Edge + .04f, .85f, z + 2.25f), V(Edge + .04f, .85f, z + 2f), R.ink, Vector3.left, false);
                    }
                }
                if (sloped)
                {
                    g.Surface = ComicSurface.Foliage;
                    if (wall > .35f) Face(g, V(Edge + .65f, w0, z), V(Edge + .65f, w1, e), V(x0 + .05f, w1, e), V(x0 + .05f, w0, z), BushFloor, Vector3.up, false);
                    for (int k = 0; k < 3; k++)
                    {
                        float xa = x0 + run * ax[k], xb = x0 + run * ax[k + 1];
                        float ya0 = w0 + (t0 - w0) * ay[k], yb0 = w0 + (t0 - w0) * ay[k + 1], ya1 = w1 + (t1 - w1) * ay[k], yb1 = w1 + (t1 - w1) * ay[k + 1];
                        Color col = scrub[(Mathf.RoundToInt(z / 4f) * 3 + k * 5 + c.Rnd.Next(2)) % scrub.Length];
                        Face(g, V(xa, ya0, z), V(xa, ya1, e), V(xb, yb1, e), V(xb, yb0, z), col, Vector3.up, false);
                    }
                }
                g.Surface = ComicSurface.Foliage;
                Face(g, V(x0 + run - .05f, t0, z), V(x0 + run - .05f, t1, e), V(92f, t1, e), V(92f, t0, z), scrub[Mathf.RoundToInt(z / 4f) % 2], Vector3.up, false);
            }
            // Closed ends: the neighbouring chunk's hill has another profile, the sky must not show between them.
            foreach (float ze in new[] { z0, z1 })
            {
                Vector3 n = ze <= z0 ? Vector3.back : Vector3.forward;
                float w = W(ze), t = T(ze);
                Color under = Color.Lerp(GumLeafDark, BushFloorDark, .5f);
                g.Surface = ComicSurface.Foliage;
                if (wall > .35f) Face(g, V(Edge + .65f, -1f, ze), V(x0 + .05f, -1f, ze), V(x0 + .05f, w, ze), V(Edge + .65f, w, ze), under, n, false);
                for (int k = 0; k < 3; k++)
                {
                    float xa = x0 + run * ax[k], xb = x0 + run * ax[k + 1];
                    Face(g, V(xa, -1f, ze), V(xb, -1f, ze), V(xb, w + (t - w) * ay[k + 1], ze), V(xa, w + (t - w) * ay[k], ze), under, n, false);
                }
                Face(g, V(x0 + run, -1f, ze), V(92f, -1f, ze), V(92f, t, ze), V(x0 + run, t, ze), under, n, false);
            }
            bool Free(float x, float z) => !(gap.HasValue && z > gap.Value.x - 1.2f && z < gap.Value.y + 1.2f) && (keepOut == null || !keepOut(x, z));
            float len = z1 - z0;
            // Limestone outcrops: tilted blocks half buried in the slope, in twos and threes (crags).
            int crags = Mathf.RoundToInt(len / 40f * 7f * rocks);
            Color[] stone = { Limestone, new Color(.84f, .76f, .58f), LimestoneDark, new Color(.9f, .85f, .7f) };
            for (int i = 0; i < crags; i++)
            {
                float x = x0 + run * F(c.Rnd, .08f, .82f), z = F(c.Rnd, z0 + 2f, z1 - 2f);
                if (!Free(x, z) || rise(z) < .3f) { c.Rnd.Next(); continue; }
                int blocks = 1 + c.Rnd.Next(3);
                for (int k = 0; k < blocks; k++)
                {
                    float bx = x + k * F(c.Rnd, 1.2f, 2.4f), bz = z + F(c.Rnd, -2.2f, 2.2f);
                    Vector3 size = V(F(c.Rnd, 2.6f, 5f), F(c.Rnd, 2.2f, 4.6f), F(c.Rnd, 3f, 6f));
                    c.Front.Surface = ComicSurface.Concrete;
                    c.Front.Box(c.P(bx, Mathf.Min(H(bx - size.x * .5f, bz - size.z * .5f), H(bx - size.x * .5f, bz + size.z * .5f)) + size.y * .28f, bz), size, stone[(i + k) % stone.Length], Quaternion.Euler(F(c.Rnd, -6f, 6f), F(c.Rnd, -20f, 20f), F(c.Rnd, -4f, 8f)));
                }
            }
            // Scrub: soft blobs all over the slope (no outline: a mass of bush, not single plants).
            int blobs = Mathf.RoundToInt(len / 40f * 34f * bush);
            Color[] leaf = { GumLeafDark, BanksiaLeaf, GumLeaf, Color.Lerp(BushFloor, GumLeaf, .5f), Color.Lerp(GumLeafDark, LeafGreen, .5f) };
            c.Deep.Surface = ComicSurface.Foliage;
            for (int i = 0; i < blobs; i++)
            {
                float x = x0 + run * F(c.Rnd, .04f, 1.05f), z = F(c.Rnd, z0 + 1f, z1 - 1f), s = F(c.Rnd, 3f, 5.6f);
                if (!Free(x, z) || rise(z) < .12f) continue;
                c.Deep.Surface = ComicSurface.Foliage;
                c.Deep.Ellipsoid(c.P(x, H(x, z) + s * .16f, z), V(s, s * F(c.Rnd, .45f, .65f), s * F(c.Rnd, .8f, 1.1f)), leaf[c.Rnd.Next(leaf.Length)]);
            }
            // Near the road, on the ledge and the foot of the slope: grass trees, banksias and shrubs with ink outlines.
            for (float z = z0 + 2f; z < z1 - 1f; z += 5.5f)
            {
                float x = F(c.Rnd, Edge + 1.6f, x0 + 3f), zz = z + F(c.Rnd, 0f, 3.5f);
                int kind = c.Rnd.Next(4);
                if (zz > z1 - .8f || !Free(x, zz) || W(zz) < .9f) continue;
                Vector3 p = c.P(x, H(x, zz) - .05f, zz);
                if (kind == 0) GrassTree(c.Props, p, F(c.Rnd, .9f, 1.25f), c.NextSeed());
                else if (kind == 1) Banksia(c.Props, p, F(c.Rnd, .7f, 1f), c.NextSeed());
                else Shrub(c.Props, p, F(c.Rnd, .8f, 1.3f), Color.Lerp(BanksiaLeaf, BushFloor, F(c.Rnd, .1f, .5f)));
            }
            // Gums on the slope and the park's tall gums along the top.
            for (float z = z0 + 3f; z < z1; z += 8f)
            {
                if (noTall.HasValue && z + 8f > noTall.Value.x && z - 2f < noTall.Value.y) continue;
                if (rise(z) < .35f) continue;
                float xm = x0 + run * F(c.Rnd, .3f, .7f), zm = z + F(c.Rnd, 0f, 5f);
                if (Free(xm, zm) && zm < z1 - 1f) Gum(c.Deep, c.P(xm, H(xm, zm) - .3f, zm), F(c.Rnd, 8f, 11f), c.NextSeed(), true);
                Gum(c.Deep, c.P(F(c.Rnd, x0 + run + 2f, x0 + run + 14f), T(z) + .3f, z), F(c.Rnd, 13f, 17f), c.NextSeed(), true);
                c.Deep.Surface = ComicSurface.Foliage;
                c.Deep.Ellipsoid(c.P(x0 + run + 1f, T(z) + 1.5f, z + 4f), V(8f, 4f, 7f), Color.Lerp(GumLeaf, BanksiaLeaf, F(c.Rnd, 0f, 1f)));
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
        /// Entry / exit: the wedge between the angled sidewalk edge and the wide road's line (x = 12) is a lawn verge
        /// behind a low limestone kerb wall that follows the edge, planted with the road's palms at a fixed spacing
        /// (the same verge as the rest of Mounts Bay Rd, not a hedged garden). from / to: the z range per side.
        /// </summary>
        static void Wedge(ChunkCtx c, int side, float from, float to)
        {
            for (float z = from; z < to - .001f; z += 2f)
            {
                float e0 = c.EdgeAt(z), e1 = c.EdgeAt(z + 2f), outer = WideEdge + .3f;
                if (outer - Mathf.Min(e0, e1) > .05f)
                    Face(c.RoadFlat, V(side * e0, .14f, z), V(side * e1, .14f, z + 2f), V(side * outer, .14f, z + 2f), V(side * outer, .14f, z), Lawn, Vector3.up, false);
                if (Mathf.Min(e0, e1) < WideEdge - .1f)
                {
                    Vector3 a = V(side * (e0 + .3f), 0f, z), b = V(side * (e1 + .3f), 0f, z + 2f), d = b - a;
                    c.FrontFlat.Surface = ComicSurface.Concrete;
                    c.FrontFlat.Box((a + b) * .5f + V(0f, .05f, 0f), V(.5f, .7f, d.magnitude + .06f), Limestone, Quaternion.LookRotation(d));
                    c.FrontFlat.Box((a + b) * .5f + V(0f, .44f, 0f), V(.6f, .1f, d.magnitude + .06f), LimestoneCap, Quaternion.LookRotation(d));
                }
            }
            for (float z = from + 3f; z < to; z += 9f)
            {
                float e = c.EdgeAt(z);
                if (WideEdge - e > 1.9f) Palm(c.Front, c.P(side * (e + 1.5f), .14f, z), F(c.Rnd, 7.5f, 9.5f), c.NextSeed());
            }
        }

        /// <summary>A place-name wall facing the rider: limestone block, navy plinth, cream cap, pixel letters whose
        /// M, N and W are five pixels wide. Base at p.</summary>
        static void NameWall(ComicMesh m, Vector3 p, string text, float px, Color stone, Color letters)
        {
            float w = TrunkPixelText.Width(text, px) + 1.2f, h = 5 * px + .9f;
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, h * .5f - .3f, 0), V(w, h + .6f, .7f), stone);
            m.Box(p + V(0, .12f, 0), V(w + .14f, .24f, .84f), R.ink);
            m.Box(p + V(0, h + .05f, 0), V(w + .2f, .14f, .85f), LimestoneCap);
            TrunkPixelText.Draw(m, text, p + V(0, h * .55f, -.36f), Vector3.right, Vector3.up, px, letters);
        }

        static RoadChunk Escarpment()
        {
            ChunkCtx c = Wide("Escarpment", River);
            RiverRoad(c);
            Side(c, -1, s =>
            {
                RiverSide(s, 0f, 40f, true, false, true);
                Jetty(s, 26f);
                Swan(s.Props, s.P(-17f, WaterY, 9f), 150f);
                Swan(s.Props, s.P(-19.5f, WaterY, 11.5f), 120f, .85f);
                Swan(s.Props, s.P(-22f, WaterY, 33f), 200f);
            });
            Side(c, 1, s =>
            {
                Scarp(s, 0f, 40f, 3f, 26f, 12f, 26f, keepOut: (x, z) => x > 30f && Mathf.Abs(z - 22f) < 7f);
                WarMemorial(s, 44f, 26f, 22f);
            });
            WideSlots(c);
            return c.Save();
        }

        /// <summary>
        /// The city end of the road: the Perth Convention and Exhibition Centre, a long low silver hall with a wavy roof
        /// line and a glass front behind slim columns, palms on its forecourt (v0.6, first chunk after the entry).
        /// </summary>
        static RoadChunk Convention()
        {
            ChunkCtx c = Wide("Convention", River);
            RiverRoad(c);
            Side(c, -1, s => RiverSide(s, 0f, 40f, true, true));
            Side(c, 1, s =>
            {
                Color silver = new Color(.84f, .87f, .9f), silverDark = new Color(.62f, .67f, .73f), glass = new Color(.2f, .3f, .4f);
                Ground(s.RoadFlat, 1, Edge, 40f, .14f, R.pavement, 0f, 40f, ComicSurface.Paving);
                Wall(s.FrontFlat, Edge + .3f, .5f, .45f, 0f, 40f, WhiteWall, LimestoneCap);
                for (float z = 5f; z < 40f; z += 10f) Palm(s.Front, s.P(10.2f, .14f, z), F(s.Rnd, 8f, 10f), s.NextSeed());
                ComicMesh m = s.Front;
                const float x0 = 14f, x1 = 36f, z0 = 2f, z1 = 38f, h = 11f;
                m.Surface = ComicSurface.Concrete;
                m.Box(s.P((x0 + x1) * .5f, -1f, 20f), V(x1 - x0, 2.4f, z1 - z0), R.ink);
                m.Surface = ComicSurface.Glass;
                m.Box(s.P((x0 + x1) * .5f + .5f, 3.4f, 20f), V(x1 - x0 - 1f, 6.6f, z1 - z0 - .6f), glass);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(s.P((x0 + x1) * .5f, 8.9f, 20f), V(x1 - x0 + .6f, 4.4f, z1 - z0 + .6f), silver);
                m.Box(s.P(x0 - .32f, 7f, 20f), V(.1f, .5f, z1 - z0 + .6f), silverDark);
                // the wavy roof: fins rising and falling along the hall
                for (float z = z0; z < z1 - .01f; z += 3f)
                {
                    float wave = 1.6f + 1.4f * Mathf.Sin((z - z0) * .52f);
                    m.Box(s.P((x0 + x1) * .5f - 1f, h + .1f + wave * .5f, z + 1.5f), V(x1 - x0 + 3f, wave, 2.9f), Mathf.RoundToInt(z / 3f) % 2 == 0 ? silver : Color.white);
                }
                // slim columns and a canopy along the glass front; lit doors
                for (float z = z0 + 1f; z < z1; z += 4.25f) m.Cylinder(s.P(x0 - 1.2f, 3.4f, z), .2f, 6.6f, Color.white, 8);
                m.Box(s.P(x0 - .9f, 6.8f, 20f), V(2.4f, .3f, z1 - z0), Color.white);
                m.Surface = ComicSurface.Plain;
                for (float z = 8f; z < 36f; z += 8f)
                    Face(m, s.P(x0 - .02f, .3f, z - 1.2f), s.P(x0 - .02f, .3f, z + 1.2f), s.P(x0 - .02f, 3f, z + 1.2f), s.P(x0 - .02f, 3f, z - 1.2f), new Color(.96f, .9f, .69f), Vector3.left, false);
                NameWall(s.Front, s.P(15.6f, .14f, 1f), "CONVENTION CENTRE", .1f, WhiteWall, SignBlue);
                Scarp(s, 0f, 40f, .2f, 22f, 37f, 18f, rocks: .4f, bush: .5f);
            });
            WideSlots(c);
            return c.Save();
        }

        /// <summary>
        /// Kennedy Fountain and the Kokoda Track Memorial Walk: the spring in a limestone alcove at the foot of Mount
        /// Eliza, and the steep stepped path that zigzags from it up to the park (three legs with rails and landings).
        /// </summary>
        static RoadChunk Kokoda()
        {
            ChunkCtx c = Wide("Kokoda", River);
            RiverRoad(c);
            Side(c, -1, s =>
            {
                RiverSide(s, 0f, 40f, true, false, true);
                Swan(s.Props, s.P(-17f, WaterY, 14f), 160f);
            });
            Side(c, 1, s =>
            {
                const float wall = 3f, top = 24f, x0 = 12f, run = 26f, fz = 12f;
                float H(float x) => x <= x0 ? wall : wall + (top - wall) * Mathf.Clamp01((x - x0) / run);
                // the walk's three legs: (x, z) corners up the slope
                var path = new[] { new Vector2(x0 + .5f, 17f), new Vector2(x0 + 9f, 33f), new Vector2(x0 + 17f, 15f), new Vector2(x0 + 25.5f, 31f) };
                bool Near(float x, float z)
                {
                    for (int i = 0; i + 1 < path.Length; i++)
                    {
                        Vector2 a = path[i], b = path[i + 1], q = new Vector2(x, z);
                        float t = Mathf.Clamp01(Vector2.Dot(q - a, b - a) / (b - a).sqrMagnitude);
                        if ((q - (a + (b - a) * t)).magnitude < 3f) return true;
                    }
                    return x < x0 + 1f && Mathf.Abs(z - fz) < 5f;
                }
                Scarp(s, 0f, 40f, wall, top, x0, run, bulge: 0f, wallGap: new Vector2(fz - 2.2f, fz + 2.2f), keepOut: Near, rocks: .8f);
                ComicMesh m = s.Front;
                Color rail = new Color(.25f, .33f, .28f), step = new Color(.78f, .74f, .64f);
                for (int i = 0; i + 1 < path.Length; i++)
                {
                    Vector3 a = s.P(path[i].x, H(path[i].x) + .2f, path[i].y), b = s.P(path[i + 1].x, H(path[i + 1].x) + .2f, path[i + 1].y);
                    m.Surface = ComicSurface.Concrete;
                    m.Box((a + b) * .5f + V(0, -.5f, 0), V(1.8f, 1.2f, (b - a).magnitude + 1.6f), step, Quaternion.LookRotation(b - a));
                    int treads = 9;
                    for (int k = 1; k < treads; k++) m.Box(Vector3.Lerp(a, b, (float)k / treads) + V(0, .14f, 0), V(1.8f, .1f, .16f), LimestoneDark, Quaternion.LookRotation(b - a));
                    m.Surface = ComicSurface.PaintedMetal;
                    Vector3 side = Vector3.Cross(Vector3.up, (b - a).normalized) * .95f;
                    foreach (float sd in new[] { -1f, 1f })
                    {
                        Rod(m, a + side * sd + V(0, 1f, 0), b + side * sd + V(0, 1f, 0), .05f, rail, 1f, 6);
                        for (int k = 0; k <= 4; k++) m.Cylinder(Vector3.Lerp(a, b, k / 4f) + side * sd + V(0, .5f, 0), .045f, 1f, rail, 6);
                    }
                    m.Surface = ComicSurface.Concrete;
                    m.Box(b + V(0, -.6f, 0), V(2.6f, 1.4f, 2.6f), step);                         // landing
                }
                var walker = new ComicMesh();
                Climber(walker, Vector3.zero, new Color(1f, .55f, .2f));
                Vector3 w0 = s.P(path[1].x, H(path[1].x) + .25f, path[1].y), w1 = s.P(path[2].x, H(path[2].x) + .25f, path[2].y);
                m.Add(walker, Vector3.Lerp(w0, w1, .45f) + V(0, .1f, 0), Quaternion.LookRotation(new Vector3(w1.x - w0.x, 0, w1.z - w0.z)), Vector3.one);
                // Kennedy Fountain: an arched limestone alcove in the retaining wall, the spring's pool in front of it
                m.Surface = ComicSurface.Concrete;
                m.Box(s.P(Edge + 1.6f, 1.7f, fz), V(2.6f, 3.6f, 4.4f), Limestone);
                m.Box(s.P(Edge + 1.6f, 3.6f, fz), V(2.9f, .3f, 4.8f), LimestoneCap);
                m.Box(s.P(Edge + .35f, 1.2f, fz), V(.3f, 2.4f, 2.2f), R.ink);
                m.Cylinder(s.P(Edge + .36f, 2.4f, fz), 1.1f, .3f, R.ink, 8, 1f, Quaternion.Euler(0, 0, 90f));
                m.Box(s.P(Edge + .22f, .45f, fz), V(.5f, .5f, 2.8f), LimestoneDark);
                m.Surface = ComicSurface.Plain;
                m.Box(s.P(Edge + .22f, .72f, fz), V(.34f, .06f, 2.5f), Water);
                NameWall(s.Front, s.P(Edge + 3.4f, wall + .2f, fz + 6.2f), "KOKODA TRACK", .1f, WhiteWall, rail);
            });
            WideSlots(c);
            return c.Save();
        }

        /// <summary>
        /// Eliza: the bronze diver on her pile in the river at the old Crawley Baths (the locals dress her up: she wears
        /// a red shirt today), a few of the baths' old piles around her; a low stretch of the escarpment on the right.
        /// </summary>
        static RoadChunk Eliza()
        {
            ChunkCtx c = Wide("Eliza", River);
            RiverRoad(c);
            Side(c, -1, s =>
            {
                RiverSide(s, 0f, 40f, true, false, true);
                ComicMesh m = s.Front;
                Color bronze = new Color(.42f, .31f, .2f), pile = new Color(.4f, .33f, .28f);
                Vector3 p = s.P(-21f, WaterY, 22f);
                m.Surface = ComicSurface.Wood;
                m.Cylinder(p + V(0, .9f, 0), .32f, 3.4f, pile, 8);
                m.Cylinder(p + V(0, 2.65f, 0), .5f, .14f, pile, 8);
                // the diver (larger than life, as she is): standing on her toes, leaning out over the water, arms
                // stretched above her head
                var fig = new ComicMesh();
                Quaternion lean = Quaternion.Euler(0, 0, -20f);
                fig.Surface = ComicSurface.Metal;
                fig.Box(lean * V(-.09f, .5f, 0), V(.15f, 1f, .17f), bronze, lean);
                fig.Box(lean * V(.09f, .5f, 0), V(.15f, 1f, .17f), bronze, lean);
                fig.Box(lean * V(0, 1.38f, 0), V(.42f, .8f, .26f), bronze, lean);
                fig.Box(lean * V(0, 1.95f, 0), V(.22f, .26f, .22f), bronze, lean);
                fig.Box(lean * V(-.13f, 2.35f, 0), V(.1f, .8f, .12f), bronze, lean);
                fig.Box(lean * V(.13f, 2.35f, 0), V(.1f, .8f, .12f), bronze, lean);
                fig.Surface = ComicSurface.Fabric;
                fig.Box(lean * V(0, 1.42f, 0), V(.46f, .6f, .3f), R.red, lean);
                m.Add(fig, p + V(0, 2.72f, 0), Quaternion.identity, Vector3.one * 1.7f);
                // old piles of the baths
                foreach (Vector2 o in new[] { new Vector2(-3f, -5f), new Vector2(3.5f, -3f), new Vector2(-2f, 5f), new Vector2(4f, 6f), new Vector2(7f, 0f) })
                {
                    m.Surface = ComicSurface.Wood;
                    m.Cylinder(p + V(-o.x, .2f + (o.x + 8f) % .5f, o.y), .2f, 1.6f, pile, 6);
                }
                Swan(s.Props, s.P(-17f, WaterY, 30f), 30f);
                NameWall(s.Front, s.P(-9.6f, .12f, 17.5f), "ELIZA", .1f, WhiteWall, SignBlue);
            });
            Side(c, 1, s => Scarp(s, 0f, 40f, 2.4f, 16f, 12f, 22f, rocks: 1.2f));
            WideSlots(c);
            return c.Save();
        }

        /// <summary>A plain stretch between the landmarks: bush escarpment, river bank with trees; rowers (0) or a picnic
        /// shelter and pelican posts (1) so the two are not the same.</summary>
        static RoadChunk Bush(string id, int variant)
        {
            ChunkCtx c = Wide(id, River);
            RiverRoad(c);
            Side(c, -1, s =>
            {
                RiverSide(s, 0f, 40f, true, variant == 1, variant == 0);
                ComicMesh m = s.Front;
                if (variant == 0)
                {
                    // a rowing eight out on the river: a long thin shell, eight rowers, oars
                    Vector3 p = s.P(-26f, WaterY + .1f, 20f);
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Box(p, V(.5f, .22f, 14f), Color.white);
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 r = p + V(0, .1f, -5.2f + i * 1.5f);
                        m.Surface = ComicSurface.Fabric;
                        m.Box(r + V(0, .35f, 0), V(.36f, .5f, .3f), i % 2 == 0 ? new Color(.1f, .25f, .6f) : R.yellow);
                        m.Box(r + V(0, .72f, 0), V(.2f, .22f, .2f), new Color(.86f, .64f, .5f));
                        m.Surface = ComicSurface.Wood;
                        m.Box(r + V(i % 2 == 0 ? 1.5f : -1.5f, .2f, 0), V(2.8f, .05f, .12f), new Color(.85f, .2f, .2f));
                    }
                    Jetty(s, 31f, 9f);
                }
                else
                {
                    // a picnic shelter on the bank and pelican posts in the shallows
                    Vector3 p = s.P(-9.7f, .12f, 22f);
                    m.Surface = ComicSurface.PaintedMetal;
                    foreach (float dz in new[] { -1.6f, 1.6f }) m.Cylinder(p + V(0, 1.3f, dz), .09f, 2.6f, SteelDark, 6);
                    m.Box(p + V(0, 2.7f, 0), V(2.2f, .14f, 4.4f), new Color(.2f, .45f, .5f), Quaternion.Euler(0, 0, 8f));
                    m.Surface = ComicSurface.Wood;
                    m.Box(p + V(0, .75f, 0), V(.8f, .08f, 2f), Timber);
                    m.Box(p + V(0, .38f, 0), V(.12f, .7f, 1.4f), SteelDark);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 q = s.P(-15.5f - i * .4f, WaterY, 6f + i * 9f);
                        m.Surface = ComicSurface.Wood;
                        m.Cylinder(q + V(0, .6f, 0), .16f, 2f, new Color(.4f, .33f, .28f), 6);
                        if (i % 2 == 0)
                        {
                            m.Surface = ComicSurface.Fabric;
                            m.Ellipsoid(q + V(0, 1.95f, 0), V(.5f, .5f, .8f), Color.white);
                            m.Box(q + V(0, 2.25f, -.45f), V(.1f, .1f, .5f), new Color(.98f, .78f, .7f));
                        }
                    }
                }
            });
            Side(c, 1, s => Scarp(s, 0f, 40f, variant == 0 ? 3.4f : 2.2f, variant == 0 ? 22f : 27f, 12f, variant == 0 ? 22f : 28f, rocks: variant == 0 ? 1.5f : .7f, bush: 1.2f));
            WideSlots(c);
            return c.Save();
        }

        /// <summary>The State War Memorial on the edge of Mount Eliza, seen from the road below: a grey granite obelisk
        /// on a stepped base, the lookout wall and two flag poles. Far above the road: decoration only.</summary>
        static void WarMemorial(ChunkCtx c, float x, float y, float z)
        {
            ComicMesh m = c.Front;
            Color granite = new Color(.74f, .74f, .76f), granitDark = new Color(.6f, .6f, .63f);
            m.Surface = ComicSurface.Concrete;
            m.Box(c.P(x - 5f, y + .6f, z), V(.6f, 1.2f, 18f), LimestoneCap);                      // lookout wall at the edge
            m.Box(c.P(x, y + .5f, z), V(7f, 1f, 7f), granitDark);
            m.Box(c.P(x, y + 1.4f, z), V(5f, .8f, 5f), granite);
            m.Box(c.P(x, y + 2.9f, z), V(3f, 2.2f, 3f), granite);
            m.Cylinder(c.P(x, y + 4f + 7f, z), 1.25f, 14f, granite, 4, .45f, Quaternion.Euler(0, 45f, 0));
            m.Cylinder(c.P(x, y + 18f + .5f, z), .56f, 1f, granitDark, 4, .05f, Quaternion.Euler(0, 45f, 0));
            foreach (float dz in new[] { -7f, 7f })
            {
                m.Surface = ComicSurface.Metal;
                m.Cylinder(c.P(x - 3f, y + 6f, z + dz), .09f, 12f, Color.white, 6);
                m.Surface = ComicSurface.Fabric;
                m.Box(c.P(x - 3f, y + 11f, z + dz + 1.1f), V(.06f, 1.2f, 2.1f), dz < 0 ? new Color(.1f, .2f, .5f) : new Color(.85f, .15f, .15f));
            }
        }

        /// <summary>Near the city end: palms along both sides, apartment towers against the escarpment.</summary>
        static RoadChunk Towers()
        {
            ChunkCtx c = Wide("Towers", River);
            RiverRoad(c);
            Side(c, -1, s =>
            {
                RiverSide(s, 0f, 40f, false, true);
                Swan(s.Props, s.P(-18f, WaterY, 20f), 170f);
            });
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
                Scarp(s, 0f, 40f, .2f, 30f, 32f, 20f, rocks: .6f, bush: .5f);
            });
            WideSlots(c);
            return c.Save();
        }

        /// <summary>The freeway interchange deck crossing high above the road on concrete piers (no colliders), with
        /// the green direction board for the Narrows Bridge on its side.</summary>
        static RoadChunk Narrows()
        {
            ChunkCtx c = Wide("Narrows", River);
            const float zc = 21f, y = 9.4f;
            RiverRoad(c);
            Side(c, -1, s => RiverSide(s, 0f, 40f, true, false, true));
            // A low escarpment here: the deck passes over it (nothing pokes through the deck).
            Side(c, 1, s => Scarp(s, 0f, 40f, 3f, 7f, 13f, 20f, noTall: new Vector2(zc - 9f, zc + 9f), rocks: .5f,
                keepOut: (x, z) => Mathf.Abs(z - zc) < 9f && x > 14f));
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
            // Green direction board on the deck's parapet, facing the rider.
            TrunkPixelText.Board(m, "NARROWS BRIDGE", c.P(0f, y + 2.3f, zc - 7.55f), Vector3.right, .2f, SignGreen, Color.white, Color.white);
            WideSlots(c);
            return c.Save();
        }

        /// <summary>
        /// Jacob's Ladder: a long, straight concrete staircase climbing up and away from the road (perpendicular to it) from
        /// an opening in the retaining wall to the top of the escarpment: four flights with landings, solid under every
        /// step, set between stone side walls in the bush slope, dark green handrails following the steps, climbers on it.
        /// </summary>
        static RoadChunk JacobsLadder()
        {
            ChunkCtx c = Wide("JacobsLadder", River);
            RiverRoad(c);
            Side(c, -1, s =>
            {
                RiverSide(s);
                Swan(s.Props, s.P(-16.5f, WaterY, 30f), 140f);
            });
            const float E = WideEdge, ledge = 3f, hillX = 17f, hillTop = 23.6f, hillEnd = 48f;
            float za = LadderZ - LadderWidth * .5f, zb = LadderZ + LadderWidth * .5f, wz0 = za - 1.4f, wz1 = zb + 1.4f;
            float HillY(float x) => x <= hillX ? ledge : Mathf.Lerp(ledge, hillTop, Mathf.Clamp01((x - hillX) / (hillEnd - hillX)));
            // The escarpment as a straight slope (the stairs follow it), open where the staircase is.
            Side(c, 1, s => Scarp(s, 0f, 40f, ledge, hillTop, hillX - Shift, hillEnd - hillX, bulge: 0f,
                wallGap: new Vector2(za, zb), gap: new Vector2(wz0, wz1), noTall: new Vector2(wz0 - 5f, wz1 + 3f), rocks: .8f));

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
            NameWall(c.Front, c.P(E + .65f + 3.2f, ledge + .2f, wz0 - .7f), "JACOBS LADDER", .1f, WhiteWall, rail);
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
            Side(c, -1, s => { RiverSide(s, 0f, zl); RiverSide(s, zg, 40f, true, false, true); });
            Side(c, 1, s =>
            {
                Ground(s.RoadFlat, 1, Edge, 14f, .14f, Lawn, 0f, zl);
                Ground(s.RoadFlat, 1, Edge, 14f, .14f, Lawn, zg, 40f);
                Wall(s.FrontFlat, Edge + .3f, .5f, .5f, 0f, zl, WhiteWall, LimestoneCap);
                Wall(s.FrontFlat, Edge + .3f, .5f, .5f, zg, 40f, WhiteWall, LimestoneCap);
                // The creek comes out of a limestone culvert at the foot of the slope.
                Scarp(s, 0f, 40f, .2f, 20f, 14f, 24f, keepOut: (x, z) => x < 19f && z > zl - 2.5f && z < zg + 2.5f);
                s.FrontFlat.Surface = ComicSurface.Concrete;
                s.FrontFlat.Box(V(15.4f, 1.2f, (zl + zg) * .5f), V(3.4f, 4.6f, InletGap + 2f), Limestone);
                s.FrontFlat.Box(V(15.4f, 3.6f, (zl + zg) * .5f), V(3.7f, .3f, InletGap + 2.4f), LimestoneCap);
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

        /// <summary>A gable roof: eaves rectangle centred on c (at eaves height), ridge along z or x, tiled slopes and
        /// brick (or rendered) gable ends.</summary>
        static void GableRoof(ComicMesh m, Vector3 c, float sx, float sz, float h, bool ridgeAlongZ, Color tiles, Color ends)
        {
            float hx = sx * .5f, hz = sz * .5f;
            m.Surface = ComicSurface.PaintedMetal;
            if (ridgeAlongZ)
            {
                Vector3 r0 = c + V(0, h, -hz), r1 = c + V(0, h, hz);
                Face(m, c + V(-hx, 0, -hz), c + V(-hx, 0, hz), r1, r0, tiles, V(-h, hx, 0));
                Face(m, c + V(hx, 0, -hz), c + V(hx, 0, hz), r1, r0, Color.Lerp(tiles, R.ink, .12f), V(h, hx, 0));
                m.Surface = ComicSurface.Brick;
                Tri(m, c + V(-hx, 0, -hz), c + V(hx, 0, -hz), r0, ends, Vector3.back);
                Tri(m, c + V(-hx, 0, hz), c + V(hx, 0, hz), r1, ends, Vector3.forward);
            }
            else
            {
                Vector3 r0 = c + V(-hx, h, 0), r1 = c + V(hx, h, 0);
                Face(m, c + V(-hx, 0, -hz), c + V(hx, 0, -hz), r1, r0, tiles, V(0, hz, -h));
                Face(m, c + V(-hx, 0, hz), c + V(hx, 0, hz), r1, r0, Color.Lerp(tiles, R.ink, .12f), V(0, hz, h));
                m.Surface = ComicSurface.Brick;
                Tri(m, c + V(-hx, 0, -hz), c + V(-hx, 0, hz), r0, ends, Vector3.left);
                Tri(m, c + V(hx, 0, -hz), c + V(hx, 0, hz), r1, ends, Vector3.right);
            }
        }

        /// <summary>
        /// The Old Swan Brewery on the river bank (red brick and tile, Federation style): a long three-storey range
        /// under a tiled gable roof with two cross gables towards the road, cream string courses and arched windows, the
        /// stair tower with its steep hipped roof and corner turrets, a tall brick chimney, a boardwalk on the river; the
        /// steel footbridge over the road to the car park decks built into the escarpment.
        /// </summary>
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
                float road = b.x + 7f;    // the facade towards the road (+x)
                // One brick block: navy plinth, cream string courses at every floor, a brick-dark cornice.
                void Block(Vector3 centre, Vector3 size, int floors)
                {
                    m.Surface = ComicSurface.Brick;
                    m.Box(centre, size, Brick);
                    m.Surface = ComicSurface.Concrete;
                    m.Box(centre + V(0, -size.y * .5f + .35f, 0), V(size.x + .1f, .7f, size.z + .1f), R.ink);
                    for (int f = 1; f < floors; f++)
                        m.Box(centre + V(0, -size.y * .5f + size.y * f / floors, 0), V(size.x + .14f, .22f, size.z + .14f), R.cream);
                    m.Box(centre + V(0, size.y * .5f - .15f, 0), V(size.x + .3f, .3f, size.z + .3f), BrickDark);
                }
                // Arched window on a wall facing +x: ink frame, glass, cream sill and a cream round head.
                void Window(float fx, float y, float z, float w, float h)
                {
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Box(V(fx, y + h * .5f, z), V(.1f, h, w), R.ink);
                    m.Surface = ComicSurface.Glass;
                    m.Box(V(fx + .02f, y + h * .5f, z), V(.08f, h - .2f, w - .2f), R.glass);
                    m.Surface = ComicSurface.Concrete;
                    m.Box(V(fx + .08f, y - .06f, z), V(.22f, .12f, w + .3f), R.cream);
                    m.Cylinder(V(fx - .02f, y + h - .05f, z), w * .5f + .14f, .1f, R.cream, 8, 1f, Quaternion.Euler(0, 0, 90f));
                }
                // The long range and its roof (ridge along the road).
                Block(b + V(0, 5f, 0), V(14f, 10f, 26f), 3);
                GableRoof(m, b + V(0, 10f, 0), 14.6f, 26.6f, 4.2f, true, Tile, Brick);
                m.Surface = ComicSurface.Concrete;
                m.Box(b + V(0, 14.25f, 0), V(.5f, .3f, 26.8f), R.cream);                          // ridge capping
                for (int level = 0; level < 3; level++)
                    for (int i = 0; i < 6; i++)
                        if (!(level == 2 && (i == 1 || i == 4))) Window(road, b.y + 1.5f + level * 3.3f, b.z - 10f + i * 4f, 1.4f, 1.8f);
                // Two cross gables towards the road: brick parapet gables with a tall arched window and a cream coping.
                foreach (float gz in new[] { -6f, 6f })
                {
                    Vector3 g = b + V(5.2f, 10f, gz);
                    GableRoof(m, g, 5f, 6f, 3.6f, false, Tile, Brick);
                    m.Surface = ComicSurface.Brick;
                    m.Box(g + V(2.2f, -1.2f, 0), V(.7f, 2.4f, 6.2f), Brick);
                    Tri(m, g + V(2.56f, 0, -3.1f), g + V(2.56f, 0, 3.1f), g + V(2.56f, 3.9f, 0), Brick, Vector3.right);
                    m.Surface = ComicSurface.Concrete;
                    m.Beam(g + V(2.6f, -.05f, -3.2f), g + V(2.6f, 3.95f, 0), .26f, R.cream);
                    m.Beam(g + V(2.6f, -.05f, 3.2f), g + V(2.6f, 3.95f, 0), .26f, R.cream);
                    m.Surface = ComicSurface.Glass;
                    m.Box(g + V(2.6f, .9f, 0), V(.1f, 1.6f, 1.2f), R.glass);
                    m.Surface = ComicSurface.Concrete;
                    m.Cylinder(g + V(2.62f, 1.7f, 0), .72f, .14f, R.cream, 8, 1f, Quaternion.Euler(0, 0, 90f));
                }
                // The stair tower at the near end (the first thing the rider sees): four floors, a steep hipped roof,
                // four corner turrets and a finial.
                Vector3 t = b + V(1f, 0, -16f);
                Block(t + V(0, 9f, 0), V(8f, 18f, 6f), 5);
                for (int level = 0; level < 5; level++) Window(t.x + 4f, t.y + 1.8f + level * 3.4f, t.z, 1.6f, 2f);
                for (int level = 1; level < 5; level++)
                {
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Box(V(t.x, t.y + 1.8f + level * 3.4f + 1f, t.z - 3.02f), V(1.5f, 2f, .1f), R.ink);
                    m.Surface = ComicSurface.Glass;
                    m.Box(V(t.x, t.y + 1.8f + level * 3.4f + 1f, t.z - 3.05f), V(1.3f, 1.8f, .08f), R.glass);
                }
                m.Surface = ComicSurface.PaintedMetal;
                m.Cylinder(t + V(0, 18f + 3f, 0), 5.2f, 6f, Tile, 4, .12f, Quaternion.Euler(0, 45f, 0));
                m.Cylinder(t + V(0, 24.6f, 0), .1f, 1.4f, R.ink, 6);
                foreach (float dx in new[] { -3.7f, 3.7f })
                    foreach (float dz in new[] { -2.7f, 2.7f })
                    {
                        m.Surface = ComicSurface.Brick;
                        m.Box(t + V(dx, 18.8f, dz), V(.9f, 1.8f, .9f), Brick);
                        m.Surface = ComicSurface.PaintedMetal;
                        m.Cylinder(t + V(dx, 20.4f, dz), .75f, 1.5f, TileDark, 4, .05f, Quaternion.Euler(0, 45f, 0));
                    }
                // The far wing, lower, with its own gable; and the brewery chimney behind it.
                Block(b + V(2f, 4f, 16.5f), V(10f, 8f, 7f), 2);
                GableRoof(m, b + V(2f, 8f, 16.5f), 10.5f, 7.5f, 3.2f, false, Tile, Brick);
                for (int i = 0; i < 2; i++) Window(b.x + 7f, b.y + 1.5f + i * 3.6f, b.z + 16.5f, 1.4f, 1.8f);
                Vector3 ch = b + V(-4f, 0, 15f);
                m.Surface = ComicSurface.Brick;
                m.Cylinder(ch + V(0, 13f, 0), 1.25f, 26f, Brick, 8, .62f);
                m.Surface = ComicSurface.Concrete;
                m.Cylinder(ch + V(0, 25.6f, 0), .92f, .5f, R.cream, 8);
                m.Cylinder(ch + V(0, 20f, 0), 1.02f, .4f, R.cream, 8);
                m.Cylinder(ch + V(0, 26.1f, 0), .8f, .5f, BrickDark, 8);
                // Boardwalk on the river side and the forecourt's palms.
                m.Surface = ComicSurface.Wood;
                m.Box(s.P(-31.5f, .2f, 18f), V(5f, .2f, 34f), Timber);
                for (float z = 6f; z < 40f; z += 14f) Palm(s.Front, s.P(-12f, .3f, z), F(s.Rnd, 8f, 10f), s.NextSeed());
                NameWall(s.Front, s.P(-13.2f, .3f, 1.2f), "OLD BREWERY", .1f, WhiteWall, Brick);
                Swan(s.Props, s.P(-38f, WaterY, 30f), 100f);
            });
            const float zbridge = 33f, yb = 7.6f, xa = -19.1f, xb = 17.2f;
            Side(c, 1, s =>
            {
                Scarp(s, 0f, 40f, 3f, 22f, 12f, 24f, keepOut: (x, z) => z > 23f && x < 25f);
                // The car park decks built into the foot of the escarpment (the footbridge lands on the middle deck).
                ComicMesh m = s.Front;
                Color slab = new Color(.72f, .71f, .68f), dark = new Color(.2f, .22f, .26f);
                const float x0 = 9.2f, x1 = 22f, z0 = 24.5f, z1 = 39.5f;
                m.Surface = ComicSurface.Concrete;
                m.Box(s.P((x0 + x1) * .5f + .6f, 6.6f, (z0 + z1) * .5f), V(x1 - x0 - 1.2f, 7.2f, z1 - z0 - .6f), dark);
                for (int level = 0; level < 3; level++)
                {
                    float y = 3.1f + level * 3.6f;
                    m.Box(s.P((x0 + x1) * .5f, y, (z0 + z1) * .5f), V(x1 - x0, .5f, z1 - z0), slab);
                    m.Box(s.P(x0 + .12f, y + .75f, (z0 + z1) * .5f), V(.24f, 1f, z1 - z0), level % 2 == 0 ? R.cream : slab);
                }
                for (float z = z0 + .4f; z <= z1; z += 4.9f) m.Box(s.P(x0 + .3f, 6.7f, z), V(.6f, 7.4f, .6f), slab);
            });
            // Footbridge: a dark steel truss from the brewery wing (x = -19) over the wide road onto the car park.
            ComicMesh fm = c.Front;
            fm.Surface = ComicSurface.PaintedMetal;
            foreach (float dz in new[] { -1.3f, 1.3f })
            {
                fm.Beam(c.P(xa, yb, zbridge + dz), c.P(xb, yb, zbridge + dz), .25f, Truss);
                fm.Beam(c.P(xa, yb + 2.2f, zbridge + dz), c.P(xb, yb + 2.2f, zbridge + dz), .25f, Truss);
                for (int i = 0; i < 14; i++)
                {
                    float x0 = Mathf.Lerp(xa, xb, i / 14f), x1 = Mathf.Lerp(xa, xb, (i + 1) / 14f);
                    fm.Beam(c.P(x0, yb, zbridge + dz), c.P(x1, yb + 2.2f, zbridge + dz), .14f, Truss);
                    fm.Box(c.P(x0, yb + 1.1f, zbridge + dz), V(.14f, 2.2f, .14f), Truss);
                }
            }
            fm.Box(c.P((xa + xb) * .5f, yb - .1f, zbridge), V(xb - xa, .25f, 2.8f), ConcreteDark);
            // Piers on the brewery forecourt and on the cliff ledge, up to the underside of the deck.
            foreach (float x in new[] { -16.6f, 13.2f })
            {
                float baseY = x > 0f ? 3f : .3f;
                fm.Box(c.P(x, (yb - .2f + baseY) * .5f, zbridge), V(.7f, yb - .2f - baseY, .7f), Truss);
            }
            WideSlots(c);
            return c.Save();
        }

        /// <summary>
        /// Entry (after Elizabeth Quay's exit): the quay's waterfront runs on. Water on both sides behind the quay's
        /// railing for the first 12 m (the mouth of the inlet on the right), then the river bank and the palms start on
        /// the left, and on the right the land comes back: a lawn with the name wall, the retaining wall and the
        /// escarpment growing out of it. The road widens to four lanes; the red shared path starts at z = 8.
        /// </summary>
        static RoadChunk Entry()
        {
            ChunkCtx c = New("Entry", Blend);
            c.HalfStart = Half; c.HalfEnd = WideHalf;     // widens from the street to the 4-lane road (z 8 to 32)
            const float land = 12f;
            RiverRoad(c, 8f, 40f);
            // A stone band across the left path where the red shared path starts.
            Flat(c.RoadFlat, -(c.EdgeAt(8f)), -(c.HalfAt(8f) + .24f), 7.6f, 8f, Kerb + .03f, R.cream);
            for (int side = -1; side <= 1; side += 2)
            {
                QuayEdge(c, side, 0f, land);
                RiverWater(c, 0f, land, c.EdgeAt(0f) + .1f, side < 0 ? 94f : 240f, side);
                Wedge(c, side, land, 40f);
                // The quay's end wall: the bank starts here.
                c.FrontFlat.Surface = ComicSurface.Concrete;
                c.FrontFlat.Box(V(side * (c.EdgeAt(land) + (side < 0 ? 4.6f : 30f)), (WaterY - 1f + .3f) * .5f, land + .25f), V(side < 0 ? 9.2f : 60f, .3f - (WaterY - 1f), .5f), LimestoneCap);
            }
            Side(c, -1, s => RiverSide(s, land, 40f, true, true));
            Side(c, 1, s =>
            {
                float Rise(float z) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(land + 1f, 25f, z));
                Ground(s.RoadFlat, 1, Edge - 4.2f, 92f, .14f, Lawn, land + .4f, 22f);
                Scarp(s, land + .4f, 40f, 3f, 20f, 12f, 24f, Rise, rocks: 1.2f, bush: 1.5f);
                for (float z = land + 3f; z < 30f; z += 9f) Palm(s.Front, s.P(10.2f, 3f * Rise(z) + .14f, z), F(s.Rnd, 8f, 10f), s.NextSeed());
            });
            NameWall(c.Front, c.P(14.6f, .14f, land + 2.2f), "MOUNTS BAY RD", .12f, WhiteWall, new Color(.16f, .36f, .62f));
            Lamp(c.Props, c.P(-(c.EdgeAt(4f) + .12f), Kerb, 4f), 1);
            Lamp(c.Props, c.P(c.EdgeAt(4f) + .12f, Kerb, 4f), -1);
            Row(c, 5f, -2.6f, 0f, 2.6f); Row(c, 25f, -2f, 2f);
            return c.Save();
        }

        /// <summary>
        /// Exit (before Canning Bridge's entry: the Kwinana Freeway shared path): the Narrows Bridge approach. The red
        /// path ends, the escarpment sinks away and the land stops at a limestone abutment (z = 14); from there the path
        /// is on the bridge: balustrade and river on the right, on the left the freeway that swings in over the water
        /// (concrete barrier and fence, three lanes, the railway in the middle with its gantries, the far carriageway),
        /// a green direction board over it. The road narrows back to the street.
        /// </summary>
        static RoadChunk Exit()
        {
            ChunkCtx c = New("Exit", Blend);
            c.HalfStart = WideHalf; c.HalfEnd = Half;     // narrows back to the street (z 8 to 32)
            const float abut = 14f, redEnd = 10f;
            RiverRoad(c, 0f, redEnd);
            Flat(c.RoadFlat, -(c.EdgeAt(redEnd)), -(c.HalfAt(redEnd) + .24f), redEnd, redEnd + .4f, Kerb + .03f, R.cream);
            Side(c, -1, s => RiverSide(s, 0f, abut - 2f, false));
            Side(c, 1, s =>
            {
                float Sink(float z) => Mathf.Lerp(1f, .18f, Mathf.SmoothStep(0f, 1f, z / abut));
                Scarp(s, 0f, abut, 3f, 18f, 12f, 24f, Sink, rocks: .6f);
            });
            Wedge(c, -1, 0f, abut - 2f);
            Wedge(c, 1, 0f, abut);
            ComicMesh f = c.FrontFlat, m = c.Front, g = c.RoadFlat;
            // The abutment: wing walls across both banks where the land ends, a name wall on the right one.
            f.Surface = ComicSurface.Concrete;
            f.Box(V(-(WideEdge + 22f), (WaterY - 1f + .3f) * .5f, abut - 1.6f), V(44f, .3f - (WaterY - 1f), .8f), Limestone);
            f.Box(V(WideEdge + 40f, (-5f + 3.6f) * .5f, abut + .2f), V(80f, 3.6f + 5f, .8f), Limestone);
            f.Box(V(WideEdge + 40f, 3.7f, abut + .2f), V(80.2f, .2f, 1f), LimestoneCap);
            m.Surface = ComicSurface.Concrete;
            m.Box(c.P(WideEdge + 6.2f, 2.2f, abut - .26f), V(9.4f, 1.5f, .14f), LimestoneCap);
            TrunkPixelText.Draw(m, "NARROWS BRIDGE", c.P(WideEdge + 6.2f, 2.2f, abut - .34f), Vector3.right, Vector3.up, .14f, SignBlue);
            // Water under and beside the bridge.
            RiverWater(c, abut - 2f, 40f, 12f, 94f);
            // On the right the river is LEVEL (not sheared with the road): 2.7 m below the path where the bridge starts,
            // 1.1 m (the riverside's usual level) where the chunk ends, so the path comes down off the bridge.
            c.Deep.Surface = ComicSurface.Plain;
            for (float z = abut; z < 40f - .001f; z += 4f)
            {
                float e = Mathf.Min(40f, z + 4f), level = c.Y(40f) + WaterY;
                Flat(c.Deep, c.EdgeAt(40f) - .5f, 240f, z, e, level, Water);
                for (int i = 0; i < 3; i++)
                {
                    float x = F(c.Rnd, 12f, 200f), zz = F(c.Rnd, z + .3f, e - .5f);
                    Flat(c.Deep, x, x + F(c.Rnd, 2f, 6f), zz, zz + .25f, level + .02f, i == 0 ? WaterFoam : WaterLight);
                }
            }
            // The bridge deck's edge beams and the balustrade on the river side (lamp standards in the fence line).
            for (float z = abut; z < 40f - .001f; z += 2f)
            {
                float e = z + 2f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 a = V(side * (c.EdgeAt(z) + .2f), 0f, z), b = V(side * (c.EdgeAt(e) + .2f), 0f, e), d = b - a;
                    Quaternion q = Quaternion.LookRotation(d);
                    f.Surface = ComicSurface.Concrete;
                    f.Box((a + b) * .5f + V(0, -.45f, 0), V(.5f, 1.3f, d.magnitude + .02f), Concrete, q);
                    if (side > 0)
                    {
                        f.Surface = ComicSurface.Metal;
                        f.Box(a + V(.05f, Kerb + .6f, 0), V(.09f, 1.2f, .09f), Steel);
                        f.Box((a + b) * .5f + V(.05f, Kerb + 1.2f, 0), V(.12f, .1f, d.magnitude), Steel, q);
                        f.Box((a + b) * .5f + V(.05f, Kerb + .7f, 0), V(.05f, .06f, d.magnitude), Steel, q);
                        f.Box((a + b) * .5f + V(.05f, Kerb + .3f, 0), V(.05f, .06f, d.magnitude), Steel, q);
                    }
                    else
                    {
                        // towards the freeway: a concrete barrier with a dark mesh fence on it
                        f.Surface = ComicSurface.Concrete;
                        f.Box((a + b) * .5f + V(-.1f, Kerb + .45f, 0), V(.4f, .9f, d.magnitude + .02f), Concrete, q);
                        f.Surface = ComicSurface.Metal;
                        f.Box(a + V(-.1f, Kerb + 1.7f, 0), V(.08f, 1.6f, .08f), SteelDark);
                        f.Box((a + b) * .5f + V(-.1f, Kerb + 2.45f, 0), V(.08f, .08f, d.magnitude), SteelDark, q);
                        f.Box((a + b) * .5f + V(-.1f, Kerb + 1.7f, 0), V(.03f, 1.4f, d.magnitude), Color.Lerp(SteelDark, Steel, .5f), q);
                    }
                }
            }
            for (float z = abut + 6f; z < 40f; z += 20f) Lamp(c.Props, c.P(c.EdgeAt(z) + .3f, Kerb, z), -1);
            // The freeway: the near carriageway swings in from the left behind the rider and runs beside the path.
            const float inner = 13.2f, lanes = 11.4f, rail = 7.6f;
            float Swing(float z) => z >= abut + 2f ? 0f : (abut + 2f - z) * (abut + 2f - z) * .12f;   // how far out it still is
            for (float z = -32f; z < 40f - .001f; z += 4f)
            {
                float e = Mathf.Min(40f, z + 4f), o0 = Swing(z), o1 = Swing(e);
                if (z < abut - 2f && o0 < 3.5f) continue;                                             // not over the bank
                g.Surface = ComicSurface.Asphalt;
                Face(g, V(-(inner + o0), .02f, z), V(-(inner + o1), .02f, e), V(-(inner + lanes + o1), .02f, e), V(-(inner + lanes + o0), .02f, z), R.asphalt, Vector3.up, false);
                g.Surface = ComicSurface.Concrete;
                Face(g, V(-(inner + lanes + o0), .02f, z), V(-(inner + lanes + o1), .02f, e), V(-(inner + lanes + rail + o1), .02f, e), V(-(inner + lanes + rail + o0), .02f, z), Ballast, Vector3.up, false);
                g.Surface = ComicSurface.Asphalt;
                Face(g, V(-(inner + lanes + rail + o0), .02f, z), V(-(inner + lanes + rail + o1), .02f, e), V(-(inner + 2f * lanes + rail + o1), .02f, e), V(-(inner + 2f * lanes + rail + o0), .02f, z), R.asphalt, Vector3.up, false);
                // deck edges (fascia) so the freeway reads as a bridge over the water
                f.Surface = ComicSurface.Concrete;
                SlopeBoxX(f, -(inner + o0), -(inner + o1), z, e, -1.1f, .5f, .3f, Concrete);
                SlopeBoxX(f, -(inner + 2f * lanes + rail + o0), -(inner + 2f * lanes + rail + o1), z, e, -1.1f, .9f, .3f, Concrete);
                SlopeBoxX(f, -(inner + lanes + o0), -(inner + lanes + o1), z, e, 0f, .8f, .3f, Concrete);
                SlopeBoxX(f, -(inner + lanes + rail + o0), -(inner + lanes + rail + o1), z, e, 0f, .8f, .3f, Concrete);
                // lane lines and the two tracks
                g.Surface = ComicSurface.Plain;
                Color white = new Color(.95f, .95f, .93f);
                if (Mathf.RoundToInt(z / 4f) % 2 == 0)
                    foreach (float lane in new[] { 3.8f, 7.6f, lanes + rail + 3.8f, lanes + rail + 7.6f })
                        Face(g, V(-(inner + lane + o0) - .07f, .04f, z), V(-(inner + lane + o1) - .07f, .04f, e - 1f), V(-(inner + lane + o1) + .07f, .04f, e - 1f), V(-(inner + lane + o0) + .07f, .04f, z), white, Vector3.up, false);
                foreach (float track in new[] { 1.5f, 2.9f, 4.7f, 6.1f })
                    Face(g, V(-(inner + lanes + track + o0) - .06f, .1f, z), V(-(inner + lanes + track + o1) - .06f, .1f, e), V(-(inner + lanes + track + o1) + .06f, .1f, e), V(-(inner + lanes + track + o0) + .06f, .1f, z), SteelDark, Vector3.up, false);
            }
            // The gore between the path and the freeway where the road is still wide: plain concrete deck.
            for (float z = abut; z < 40f - .001f; z += 2f)
                Face(g, V(-(c.EdgeAt(z) + .4f), .02f, z), V(-(c.EdgeAt(z + 2f) + .4f), .02f, z + 2f), V(-(inner + Swing(z + 2f) + .1f), .02f, z + 2f), V(-(inner + Swing(z) + .1f), .02f, z), Color.Lerp(Concrete, R.pavement, .5f), Vector3.up, false);
            // Overhead line gantries over the railway, and the trains' wires between them.
            foreach (float z in new[] { abut + 8f, abut + 24f })
            {
                float xc = -(inner + lanes + rail * .5f);
                m.Surface = ComicSurface.Metal;
                m.Box(c.P(xc - rail * .5f + .2f, 3.4f, z), V(.24f, 6.8f, .24f), Steel);
                m.Box(c.P(xc + rail * .5f - .2f, 3.4f, z), V(.24f, 6.8f, .24f), Steel);
                m.Box(c.P(xc, 6.6f, z), V(rail, .22f, .22f), Steel);
            }
            foreach (float track in new[] { 2.2f, 5.4f })
                Rod(c.Deep, c.P(-(inner + lanes + track), 5.9f, abut + 2f), c.P(-(inner + lanes + track), 5.9f, 40f), .03f, SteelDark, 1f, 4);
            // Freeway traffic (decoration) and the green direction board on a gantry over the near carriageway.
            Color[] cars = { R.red, Color.white, new Color(.2f, .5f, .85f), R.yellow };
            for (int i = 0; i < 4; i++)
            {
                bool far = i >= 2;
                float x = -(inner + (far ? lanes + rail : 0f) + (i % 2 == 0 ? 2f : 5.7f)), z = abut + 5f + i * 6.2f;
                m.Add(ComicKitGeometry.Car(R, cars[i]), c.P(x, .67f, z), Quaternion.Euler(0, far ? 180f : 0f, 0), Vector3.one);
            }
            float gz = abut + 16f, gx = -(inner + lanes * .5f);
            m.Surface = ComicSurface.Metal;
            m.Box(c.P(-(inner - .6f), 3.6f, gz), V(.3f, 7.2f, .3f), Steel);
            m.Box(c.P(-(inner + lanes + .4f), 3.6f, gz), V(.3f, 7.2f, .3f), Steel);
            m.Box(c.P(gx, 7f, gz), V(lanes + 1.2f, .3f, .3f), Steel);
            TrunkPixelText.Board(m, "KWINANA FWY", c.P(gx, 8.25f, gz - .2f), Vector3.right, .22f, SignGreen, Color.white, Color.white);
            Row(c, 15f, -2f, 2f); Row(c, 35f, -2.6f, 0f, 2.6f);
            return c.Save();
        }

        /// <summary>An upright slab along a line whose x changes between z0 and z1 (barriers, deck edges): from y0 to y1,
        /// thick metres wide.</summary>
        static void SlopeBoxX(ComicMesh m, float xa, float xb, float z0, float z1, float y0, float y1, float thick, Color colour)
        {
            Vector3 a = V(xa, 0f, z0), b = V(xb, 0f, z1), d = b - a;
            m.Box((a + b) * .5f + V(0, (y0 + y1) * .5f, 0), V(thick, y1 - y0, d.magnitude + .02f), colour, Quaternion.LookRotation(d));
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
        /// Far shore (follows the rider, no colliders; shared by Elizabeth Quay and Mounts Bay Rd): the wide river on
        /// the left with yachts and a ferry, and the South Perth shore: the foreshore lawn, a line of trees with Norfolk
        /// pines, and the peninsula's apartment towers with their balcony bands, the tallest in the middle. Sloped with
        /// the riverside road so it meets the chunks' water.
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
                Flat(m, -250f, -256f, z, z + 20f, WaterY + .35f, new Color(.86f, .8f, .62f));    // the beach
                Flat(m, -256f, -340f, z, z + 20f, WaterY + .5f, new Color(.5f, .66f, .36f));
            }
            m.Surface = ComicSurface.Foliage;
            for (float z = -40f; z < 260f; z += 15f)
                m.Ellipsoid(V(-F(r, 262f, 276f), 3.5f, z), V(F(r, 12f, 18f), F(r, 7f, 10f), F(r, 11f, 16f)), Color.Lerp(GumLeafDark, Lawn, F(r, 0f, .5f)));
            // Norfolk pines on the foreshore: a trunk and three tiers.
            for (float z = -20f; z < 250f; z += 34f)
            {
                Vector3 p = V(-F(r, 258f, 264f), WaterY + .5f, z + F(r, 0f, 10f));
                m.Surface = ComicSurface.Wood;
                m.Cylinder(p + V(0, 8f, 0), .5f, 16f, new Color(.4f, .32f, .26f), 6, .4f);
                m.Surface = ComicSurface.Foliage;
                for (int k = 0; k < 3; k++) m.Cylinder(p + V(0, 7f + k * 4f, 0), 4.2f - k * 1.1f, 3.4f, new Color(.2f, .4f, .27f), 6, .25f);
            }
            // South Perth's apartment towers: pale bodies with darker balcony bands, a roof slab, the tallest mid-shore.
            Color[] walls = { new Color(.93f, .91f, .86f), new Color(.86f, .88f, .9f), new Color(.92f, .85f, .74f), new Color(.8f, .86f, .9f) };
            for (int i = 0; i < 15; i++)
            {
                float z = -30f + i * 19f + F(r, -4f, 4f), mid = 1f - Mathf.Abs(i - 7f) / 8f;
                float h = Mathf.Lerp(12f, 46f, mid * mid) + F(r, -3f, 8f), w = F(r, 10f, 15f), x = -F(r, 280f, 300f);
                Color wall = walls[i % walls.Length], band = Color.Lerp(wall, new Color(.25f, .38f, .5f), .55f);
                m.Surface = ComicSurface.Concrete;
                m.Box(V(x, h * .5f, z), V(w, h, w), wall);
                m.Surface = ComicSurface.Glass;
                int bands = Mathf.Clamp(Mathf.RoundToInt(h / 6f), 2, 7);
                for (int k = 0; k < bands; k++) m.Box(V(x + .2f, (k + .7f) * h / bands, z), V(w + .1f, h / bands * .35f, w + .3f), band);
                m.Surface = ComicSurface.Concrete;
                m.Box(V(x, h + .5f, z), V(w * .6f, 1f, w * .6f), Color.Lerp(wall, R.ink, .25f));
            }
            for (int i = 0; i < 4; i++) Yacht(m, V(-F(r, 70f, 200f), WaterY - .15f, F(r, 0f, 220f)), F(r, 1.4f, 2f), i % 2 == 0 ? R.red : Color.white);
            // A river ferry on its way across (white, green band: like the ones at Elizabeth Quay).
            Vector3 fp = V(-150f, WaterY - .15f, 120f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(fp + V(0, .7f, 0), V(15f, 1.4f, 4.4f), Color.white);
            m.Box(fp + V(0, .2f, 0), V(15.1f, .4f, 4.5f), new Color(.12f, .48f, .36f));
            m.Box(fp + V(-.6f, 2.1f, 0), V(10f, 1.4f, 3.7f), Color.white);
            m.Box(fp + V(-.6f, 2.15f, 0), V(9.6f, .7f, 3.8f), new Color(.15f, .2f, .24f));
            m.Box(fp + V(-.6f, 2.9f, 0), V(10.4f, .16f, 4f), new Color(.12f, .48f, .36f));
            m.Shear(Grade); // follows the riverside slope ahead of (and behind) the rider
            return SavePrefab(FarShorePath, MeshFolder, m, false, false, root =>
            {
                foreach (Collider col in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(col, true);
            });
        }
    }
}
