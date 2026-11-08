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
    /// KINGS PARK (v0.3 district, order 30): Fraser Avenue's double row of lemon-scented gums, limestone walls, native
    /// bush (balga, banksia, kangaroo paw, pink everlastings), the DNA Tower, a lookout over the river and the city, the
    /// Federation Walkway (a glass-and-steel platform ramp through the treetops that ends over a gully: WHOOPS), and a
    /// steeper bush descent towards the city.
    ///   Menu: CityScooter/Districts/Build Kings Park
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.KingsParkBuilder.BuildBatch
    /// Riding east along Fraser Ave towards the city: the river and the view are on the right.
    /// </summary>
    public static class KingsParkBuilder
    {
        public const string Id = "kingspark", Name = "KingsPark";
        public const string PrefabFolder = ComicCityBuilder.Prefabs + "/Districts/KingsPark";
        public const string MeshFolder = ComicCityBuilder.Root + "/Districts/KingsPark";
        public const string AssetPath = DistrictData + "/District_KingsPark.asset";
        const float Standard = -3.1470692f;

        // Federation Walkway: right lane, from the kerb in to x = 0.8.
        public const float WalkwayX = 2.4f, WalkwayWidth = 3.2f, WalkwayStart = 4f, WalkwayRise = 10f, WalkwayHeight = 3f, WalkwayTop = 14f, GullyLength = 6f;

        static readonly Color DeckMesh = new Color(.34f, .33f, .42f), Timber = new Color(.66f, .47f, .3f);

        [MenuItem("CityScooter/Districts/Build Kings Park")]
        public static void Build()
        {
            Init();
            PrefabBuildUtility.EnsureFolder(PrefabFolder);
            PrefabBuildUtility.EnsureFolder(MeshFolder);

            RoadChunk entry = Entry();
            var chunks = new List<RoadChunk> { FraserAve(), Wildflowers(), Walkway(), Lookout(), BushDescent() };
            RoadChunk exit = Exit();

            GameObject branch = GumBranch();
            GameObject magpie = Magpie();
            GameObject ramp = BoardwalkRamp();
            GameObject far = FarView();

            SaveDistrict(AssetPath, d =>
            {
                d.id = Id; d.displayName = "KINGS PARK"; d.order = 30; d.length = new Vector2(440f, 600f);
                d.chunks = chunks.ToArray(); d.orderedChunks = true; d.entry = entry; d.exit = exit; d.allowBoulevard = false;
                var obstacles = new List<Obstacle> { branch.GetComponent<Obstacle>(), magpie.GetComponent<Obstacle>() };
                foreach (string id in new[] { "Cones", "Bin" }) { Obstacle o = BaseObstacle(id); if (o != null) obstacles.Add(o); }
                d.obstacles = obstacles.ToArray();
                d.jumpObstacles = Array.Empty<Obstacle>();
                d.gifts = Array.Empty<Gift>();
                d.hazards = Array.Empty<HazardPattern>();
                d.pedestrians = new[] { Person("Tourist"), Person("Tourist"), Person("Jogger"), Person("Elder"), Person("DogWalker"), Person("Kid"), magpie };
                d.ramps = new[] { ramp.GetComponent<Ramp>() };
                d.rampFrequency = 1f;
                d.blendMeters = 150f;
                d.fogColor = new Color(.7f, .79f, .72f);
                d.skyTint = new Color(1f, .98f, .93f);
                d.ambientSky = new Color(.64f, .78f, .8f);
                d.ambientEquator = new Color(.76f, .8f, .64f);
                d.ambientGround = new Color(.52f, .5f, .36f);
                d.sunColor = new Color(1f, .94f, .8f);
                d.sunIntensity = 1.4f;
                d.farScenery = far;
                // gateway and ambience belong to the district system (DistrictSetup): not touched here.
            });
            AssetDatabase.SaveAssets();
            Debug.Log("[KingsPark] built " + (chunks.Count + 2) + " chunks: " + string.Join("; ", Report));
        }

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static ChunkCtx New(string id, float endHeight) => new ChunkCtx(Name, id, PrefabFolder, MeshFolder, endHeight);

        static void StandardRoad(ChunkCtx c) => Road(c, (s, z) => R.pavement, (s, z) => true);

        // ------------------------------------------------------------------ side dressing

        /// <summary>Lawn verge and dry bush floor beyond the sidewalk, dappled.</summary>
        static void ParkGround(ChunkCtx c, int side, float z0 = 0f, float z1 = 40f, float verge = 15f)
        {
            Ground(c.RoadFlat, side, Edge, verge, .14f, Lawn, z0, z1);
            Ground(c.RoadFlat, side, verge, 80f, .1f, BushFloor, z0, z1);
            Patches(c.RoadFlat, c.Rnd, side, Edge + .5f, verge - .3f, .14f, LawnLight, new Color(.4f, .55f, .27f), z0, z1, 3);
            Patches(c.RoadFlat, c.Rnd, side, verge + .5f, 40f, .1f, BushFloorDark, new Color(.7f, .63f, .42f), z0, z1, 4);
        }

        /// <summary>Fraser Ave: the double row of lemon-scented gums on one side, bush behind.</summary>
        static void GumRows(ChunkCtx c, int side, float z0 = 0f, float z1 = 40f, bool outer = true)
        {
            for (float z = z0 + (side < 0 ? 3f : 7f); z < z1 - .5f; z += 10f)
                Gum(c.Front, c.P(side * F(c.Rnd, 10f, 11f), .1f, z + F(c.Rnd, -1f, 1f)), F(c.Rnd, 16f, 20f), c.NextSeed());
            if (outer)
                for (float z = z0 + (side < 0 ? 8f : 2f); z < z1 - .5f; z += 10f)
                    Gum(c.Front, c.P(side * F(c.Rnd, 15.5f, 17f), .1f, z + F(c.Rnd, -1.5f, 1.5f)), F(c.Rnd, 17f, 22f), c.NextSeed());
        }

        static void Undergrowth(ChunkCtx c, int side, int count, float xa = 18f, float xb = 26f, float z0 = 0f, float z1 = 40f)
        {
            for (int i = 0; i < count; i++)
            {
                float x = side * F(c.Rnd, xa, xb), z = F(c.Rnd, z0 + 1f, z1 - 1f);
                Vector3 p = c.P(x, .1f, z);
                switch (i % 4)
                {
                    case 0: Balga(c.Props, p, F(c.Rnd, .9f, 1.2f), c.NextSeed()); break;
                    case 1: Banksia(c.Props, p, F(c.Rnd, .8f, 1.1f), c.NextSeed()); break;
                    case 2: Shrub(c.Props, p, F(c.Rnd, 1f, 1.6f), Color.Lerp(BanksiaLeaf, GumLeaf, F(c.Rnd, 0f, 1f))); break;
                    default: KangarooPaw(c.Props, p, 1.1f, c.NextSeed()); break;
                }
            }
        }

        /// <summary>Deep bush: cheap gums and bush mass (no outline, no shadows).</summary>
        static void DeepBush(ChunkCtx c, int side, float z0 = 0f, float z1 = 40f, float x0 = 25f)
        {
            for (float z = z0 + 2f; z < z1; z += 6.5f)
                Gum(c.Deep, c.P(side * F(c.Rnd, x0, x0 + 20f), .1f, z + F(c.Rnd, -2f, 2f)), F(c.Rnd, 15f, 22f), c.NextSeed(), true);
            c.Deep.Surface = ComicSurface.Foliage;
            for (float z = z0 + 5f; z < z1; z += 10f)
                c.Deep.Ellipsoid(c.P(side * F(c.Rnd, x0 + 18f, x0 + 30f), 3f, z), V(14f, 8f, 11f), Color.Lerp(GumLeafDark, BanksiaLeaf, F(c.Rnd, 0f, 1f)));
        }

        static void LimestoneEdge(ChunkCtx c, int side, float z0 = 0f, float z1 = 40f, float height = .5f)
        {
            Wall(c.FrontFlat, side * (Edge + .3f), .5f, height, z0, z1, Limestone, LimestoneCap);
        }

        static void FraserSide(ChunkCtx c, int side, float z0 = 0f, float z1 = 40f)
        {
            ParkGround(c, side, z0, z1);
            LimestoneEdge(c, side, z0, z1);
            GumRows(c, side, z0, z1);
            Undergrowth(c, side, 3, 19f, 25f, z0, z1);
            DeepBush(c, side, z0, z1);
        }

        // ------------------------------------------------------------------ chunks

        static RoadChunk FraserAve()
        {
            ChunkCtx c = New("FraserAve", Standard);
            StandardRoad(c);
            FraserSide(c, -1); FraserSide(c, 1);
            // A kangaroo paw and a balga at the foot of the near gums, behind the wall.
            KangarooPaw(c.Props, c.P(-9.2f, .14f, 18f), 1.2f, 3); KangarooPaw(c.Props, c.P(9.3f, .14f, 31f), 1.2f, 4);
            c.StandardSlots();
            return c.Save();
        }

        static RoadChunk Wildflowers()
        {
            ChunkCtx c = New("Wildflowers", Standard);
            StandardRoad(c);
            FraserSide(c, -1);
            // Right: botanic garden beds behind a limestone edge, the DNA Tower above the bush.
            ParkGround(c, 1, 0f, 40f, 30f);
            LimestoneEdge(c, 1, 0f, 40f, .4f);
            for (float z = 3f; z < 38f; z += 12f) DaisyBed(c.DeepFlat, 9.2f, 13.5f, z, z + 8f, .14f, c.Rnd, 14);
            for (float z = 6f; z < 38f; z += 7f) KangarooPaw(c.Props, c.P(F(c.Rnd, 14.5f, 16.5f), .14f, z), F(c.Rnd, 1.1f, 1.4f), c.NextSeed());
            Balga(c.Props, c.P(18.5f, .12f, 9f), 1.3f, 7); Balga(c.Props, c.P(20f, .12f, 31f), 1.1f, 8);
            Banksia(c.Props, c.P(17.5f, .12f, 21f), 1.2f, 9);
            Gum(c.Front, c.P(12.5f, .1f, 2f), 18f, 11); Gum(c.Front, c.P(13f, .1f, 37f), 17f, 12);
            DnaTower(c.Front, c.P(23f, .1f, 26f));
            DeepBush(c, 1, 0f, 40f, 30f);
            c.StandardSlots();
            return c.Save();
        }

        /// <summary>The DNA Tower: a white double-helix lookout staircase (15 m), a well-known Kings Park climb.</summary>
        internal static void DnaTower(ComicMesh m, Vector3 p)
        {
            Color white = new Color(.97f, .97f, .95f), grey = new Color(.84f, .87f, .9f);
            m.Surface = ComicSurface.Plaster;
            m.Cylinder(p + V(0, 7.6f, 0), .7f, 15.2f, white, 8);
            m.Cylinder(p + V(0, .2f, 0), 2.8f, .4f, LimestoneCap, 8);
            m.Surface = ComicSurface.PaintedMetal;
            const int n = 24;
            for (int helix = 0; helix < 2; helix++)
            {
                Vector3 last = Vector3.zero;
                for (int i = 0; i < n; i++)
                {
                    float a = i * 30f + helix * 180f, y = .6f + i * .6f;
                    Quaternion q = Quaternion.Euler(0, a, 0);
                    m.Box(p + q * V(1.45f, y, 0), V(1.7f, .22f, .75f), helix == 0 ? white : grey, q);
                    Vector3 rail = p + q * V(2.3f, y + 1f, 0);
                    if (i > 0) m.Beam(last, rail, .12f, white);
                    last = rail;
                }
            }
            m.Surface = ComicSurface.Plaster;
            m.Cylinder(p + V(0, 15.4f, 0), 2.3f, .25f, white, 8);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 16.05f, 0), 2.3f, 1f, Steel, 8, 1f);
            m.Cylinder(p + V(0, 16.05f, 0), 2.15f, 1.05f, R.glass, 8, 1f);
        }

        static RoadChunk Walkway()
        {
            ChunkCtx c = New("Walkway", Standard);
            float z0 = WalkwayStart, zr = z0 + WalkwayRise, zl = zr + WalkwayTop, zg = zl + GullyLength;
            c.Gaps.Add(new GapSpec { X = WalkwayX, Z = zl, Length = GullyLength, Width = WalkwayWidth, Word = "WHOOPS", Water = false });
            c.Ramps.Add(new RampSpec { X = WalkwayX, Z = z0, Width = WalkwayWidth, Rise = WalkwayRise, Height = WalkwayHeight, Top = WalkwayTop, Launch = 1f, Hang = .5f });
            StandardRoad(c);
            FraserSide(c, -1);
            // Right: garden bush the walkway flies through; tall gums lean over it.
            ParkGround(c, 1, 0f, 40f, 12f);
            LimestoneEdge(c, 1, 0f, 40f);
            for (float z = 1f; z < 40f; z += 10f) Gum(c.Front, c.P(F(c.Rnd, 10f, 12f), .1f, z + F(c.Rnd, -1f, 1f)), F(c.Rnd, 18f, 22f), c.NextSeed());
            Undergrowth(c, 1, 3, 13f, 22f);
            DeepBush(c, 1, 0f, 40f, 22f);
            WalkwayArt(c, z0, zr, zl);
            Gully(c, zl, zg);
            // The walkway owns the right lane: obstacles only in the two lanes beside it.
            foreach (float z in new[] { 10f, 25f }) { c.Slots.Add(new Vector2(-2.6f, z)); c.Slots.Add(new Vector2(-.3f, z)); }
            return c.Save();
        }

        /// <summary>
        /// Federation Walkway: dark steel-mesh deck on a grey steel arch, glass balustrades with steel posts and handrails.
        /// Matches the Ramp numbers exactly (pivot at z0 on the road, rise to height, flat top to the lip).
        /// </summary>
        static void WalkwayArt(ChunkCtx c, float z0, float zr, float zl)
        {
            ComicMesh m = c.FrontFlat; // sheared with the road: the Ramp's height is above the road
            float x = WalkwayX, w = WalkwayWidth * .5f, H = WalkwayHeight;
            float Deck(float z) => z <= z0 ? 0f : z >= zr ? H : H * (z - z0) / (zr - z0);
            // Deck: a closed dark steel-mesh slab between closed steel edge beams, glass panels as thin closed slabs,
            // round handrails on round posts (in 2 m spans).
            int span = 0;
            for (float a = z0; a < zl - .001f; a += 2f, span++)
            {
                float b = a + 2f, ya = Deck(a), yb = Deck(b);
                m.Surface = ComicSurface.Metal;
                SlopeBox(m, x - w + .16f, x + w - .16f, a, b, ya - .4f, ya, yb - .4f, yb, DeckMesh);
                for (int s = -1; s <= 1; s += 2)
                {
                    float e = x + s * w, ei = e - s * .16f, g0 = e - s * .12f, g1 = e - s * .05f;
                    m.Surface = ComicSurface.PaintedMetal;
                    SlopeBox(m, Mathf.Min(e, ei), Mathf.Max(e, ei), a, b, ya - .5f, ya + .14f, yb - .5f, yb + .14f, Steel);
                    m.Surface = ComicSurface.Glass;
                    SlopeBox(m, Mathf.Min(g0, g1), Mathf.Max(g0, g1), a + .05f, b - .05f, ya + .14f, ya + 1.12f, yb + .14f, yb + 1.12f, Glass);
                    m.Surface = ComicSurface.Metal;
                    if (span % 2 == 0) m.Cylinder(V(e - s * .085f, ya + .67f, a), .045f, 1.06f, SteelDark, 8);
                }
                m.Surface = ComicSurface.Metal;
                m.Box(V(x, Mathf.Min(ya, yb) + .02f, a + 1f), V(WalkwayWidth - .5f, .06f, .08f), SteelDark); // mesh joint strip
            }
            for (int s = -1; s <= 1; s += 2)
            {
                float rx = x + s * (w - .085f);
                m.Cylinder(V(rx, H + .67f, zl), .045f, 1.06f, SteelDark, 8);
                m.Surface = ComicSurface.Metal;
                Rod(m, V(rx, 1.2f, z0), V(rx, H + 1.2f, zr), .05f, Steel);   // handrails: one round rail per straight run
                Rod(m, V(rx, H + 1.2f, zr), V(rx, H + 1.2f, zl), .05f, Steel);
            }
            // Lip: yellow-and-black edge painted on the deck end.
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 6; i++)
            {
                float xa = x - w + .16f + (WalkwayWidth - .32f) * i / 6f, xb = x - w + .16f + (WalkwayWidth - .32f) * (i + 1) / 6f;
                Face(m, V(xa, H + .015f, zl - .5f), V(xa, H + .015f, zl), V(xb, H + .015f, zl), V(xb, H + .015f, zl - .5f), i % 2 == 0 ? R.yellow : R.ink, Vector3.up, false);
            }
            // Columns under the climbing part, the arch under the flat span (the bridge's signature curve).
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = z0 + 4f; z < zr + .1f; z += 3f)
                for (int s = -1; s <= 1; s += 2) m.Cylinder(V(x + s * (w - .3f), (Deck(z) - .45f) * .5f, z), .13f, Deck(z) - .45f, SteelDark, 8);
            const int seg = 8;
            for (int s = -1; s <= 1; s += 2)
            {
                float ax = x + s * (w - .35f);
                for (int i = 0; i < seg; i++)
                {
                    float t0 = (float)i / seg, t1 = (float)(i + 1) / seg;
                    float za = Mathf.Lerp(zr - 1f, zl - .4f, t0), zb = Mathf.Lerp(zr - 1f, zl - .4f, t1);
                    float ya = Mathf.Sin(t0 * Mathf.PI) * (H - .7f), yb = Mathf.Sin(t1 * Mathf.PI) * (H - .7f);
                    Rod(m, V(ax, ya, za), V(ax, yb, zb), .16f, Steel);
                    if (i % 2 == 1 && ya < H - .8f) m.Cylinder(V(ax, (ya + H - .45f) * .5f, za), .06f, H - .45f - ya, SteelDark, 8);
                }
            }
            // Entrance portal with the name board (dark park green, cream pixel letters), high enough to ride under.
            ComicMesh u = c.Front;
            float pz = z0 - .8f;
            Color board = new Color(.16f, .3f, .2f);
            u.Surface = ComicSurface.PaintedMetal;
            // One post beyond the sidewalk (out of reach) and an arm over the walkway lane.
            u.Cylinder(c.P(Edge + .45f, .12f, pz), .26f, .24f, R.ink, 8, .9f);
            u.Cylinder(c.P(Edge + .45f, 2.5f, pz), .13f, 5f, SteelDark, 8);
            u.Box(c.P((x - w + Edge + .45f) * .5f, 4.62f, pz), V(Edge + .45f - (x - w), .14f, .14f), SteelDark);
            u.Box(c.P(x, 3.98f, pz), V(WalkwayWidth + .3f, .95f, .14f), board);
            u.Box(c.P(x, 4.5f, pz), V(WalkwayWidth + .4f, .12f, .22f), R.ink);
            u.Box(c.P(x, 3.47f, pz), V(WalkwayWidth + .36f, .08f, .2f), R.cream);
            Text(u, "FEDERATION", x, c.Y(pz) + 4.2f, pz - .08f, .066f, LimestoneCap);
            Text(u, "WALKWAY", x, c.Y(pz) + 3.78f, pz - .08f, .066f, LimestoneCap);
        }

        /// <summary>The gully under the end of the walkway: a limestone-lined drop with ferns at the bottom.</summary>
        static void Gully(ChunkCtx c, float za, float zb)
        {
            ComicMesh m = c.FrontFlat;
            float x0 = WalkwayX - WalkwayWidth * .5f, x1 = WalkwayX + WalkwayWidth * .5f;
            const float depth = 3.2f;
            m.Surface = ComicSurface.Concrete;
            Color wall = LimestoneDark;
            Face(m, V(x0, -depth, za), V(x0, -depth, zb), V(x0, 0, zb), V(x0, 0, za), wall, Vector3.right);
            Face(m, V(x1, -depth, za), V(x1, -depth, zb), V(x1, .2f, zb), V(x1, .2f, za), wall, Vector3.left);
            Face(m, V(x0, -depth, za), V(x1, -depth, za), V(x1, 0, za), V(x0, 0, za), wall, Vector3.forward);
            Face(m, V(x0, -depth, zb), V(x1, -depth, zb), V(x1, 0, zb), V(x0, 0, zb), wall, Vector3.back);
            m.Surface = ComicSurface.Foliage;
            Face(m, V(x0, -depth, za), V(x0, -depth, zb), V(x1, -depth, zb), V(x1, -depth, za), BushFloorDark, Vector3.up);
            for (int i = 0; i < 4; i++) Shrub(m, V(F(c.Rnd, x0 + .6f, x1 - .6f), -depth, F(c.Rnd, za + .8f, zb - .8f)), .9f, Color.Lerp(LeafGreen, GumLeafDark, i * .3f));
            // Yellow warning paint round the hole on the road.
            m.Surface = ComicSurface.Plain;
            Flat(m, x0 - .35f, x0 - .1f, za - .3f, zb + .3f, .033f, R.yellow);
            Flat(m, x0 - .1f, x1, zb + .1f, zb + .35f, .033f, R.yellow);
        }

        static RoadChunk Lookout()
        {
            ChunkCtx c = New("Lookout", Standard);
            StandardRoad(c);
            FraserSide(c, -1);
            // Right: limestone balustrade, a lawn ledge, then the escarpment drops away to the river; the city beyond.
            Wall(c.FrontFlat, Edge + .3f, .5f, .95f, 0f, 40f, Limestone, LimestoneCap);
            Ground(c.RoadFlat, 1, Edge, 11f, .14f, Lawn, 0f, 40f);
            // Convex hillside: a gentle top you can see over the wall, then the steep escarpment down to the river.
            Bank(c.RoadFlat, 1, 11f, .14f, 40f, -3.5f, Color.Lerp(BushFloor, Lawn, .35f), 0f, 40f);
            Bank(c.RoadFlat, 1, 40f, -3.5f, 90f, -40f, BushFloorDark, 0f, 40f);
            for (int i = 0; i < 8; i++)
            {
                float x = F(c.Rnd, 15f, 38f), y = Mathf.Lerp(.14f, -3.5f, (x - 11f) / 29f);
                c.Deep.Surface = ComicSurface.Foliage;
                c.Deep.Ellipsoid(c.P(x, y + .6f, F(c.Rnd, 2f, 38f)), V(F(c.Rnd, 3f, 6f), 2.2f, F(c.Rnd, 3f, 5f)), Color.Lerp(GumLeaf, BanksiaLeaf, F(c.Rnd, 0f, 1f)));
            }
            for (int i = 0; i < 3; i++) Balga(c.Props, c.P(F(c.Rnd, 13f, 22f), Mathf.Lerp(.14f, -3.5f, .2f), 6f + i * 12f), 1.1f, c.NextSeed());
            Gum(c.Front, c.P(10.2f, .14f, 2f), 19f, 21); Gum(c.Front, c.P(10.4f, .14f, 39f), 18f, 22);
            // Treetops of the bush on the escarpment below the lookout.
            for (int i = 0; i < 6; i++)
            {
                float x = F(c.Rnd, 44f, 62f), y = Mathf.Lerp(-3.5f, -40f, (x - 40f) / 50f);
                Gum(c.Deep, c.P(x, y, 3f + i * 6.5f), F(c.Rnd, 16f, 20f), c.NextSeed(), true);
            }
            // Coin binoculars on the balustrade and a bench for the photo crowd (behind the wall, out of reach).
            Viewer(c.Props, c.P(Edge + .3f, 1.0f, 15f)); Viewer(c.Props, c.P(Edge + .3f, 1.0f, 27f));
            c.Props.Surface = ComicSurface.Wood;
            c.Props.Box(c.P(9.8f, .55f, 21f), V(.6f, .12f, 2f), Timber); c.Props.Box(c.P(10.1f, .85f, 21f), V(.1f, .5f, 2f), Timber);
            c.StandardSlots();
            return c.Save();
        }

        internal static void Viewer(ComicMesh m, Vector3 p)
        {
            Color green = new Color(.2f, .5f, .32f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .08f, 0), .2f, .16f, R.ink, 8, .9f);
            m.Cylinder(p + V(0, .52f, 0), .07f, .76f, green, 8);
            m.Cylinder(p + V(0, .93f, 0), .11f, .08f, R.cream, 8);
            m.Box(p + V(0, 1.1f, 0), V(.32f, .28f, .44f), green);
            m.Box(p + V(0, 1.26f, 0), V(.36f, .06f, .48f), R.ink);
            m.Surface = ComicSurface.Glass;
            foreach (float dz in new[] { -.11f, .11f })
            {
                m.Cylinder(p + V(.19f, 1.1f, dz), .08f, .08f, R.glass, 8, 1f, Quaternion.Euler(0, 0, 90f));
                m.Cylinder(p + V(-.2f, 1.12f, dz), .05f, .1f, R.ink, 8, 1f, Quaternion.Euler(0, 0, 90f));
            }
        }

        static RoadChunk BushDescent()
        {
            ChunkCtx c = New("BushDescent", -4.8f);
            StandardRoad(c);
            // Left: the road is cut into the hill: a limestone retaining wall, bush above it.
            Wall(c.FrontFlat, -(Edge + .35f), .6f, 1.8f, 0f, 40f, Limestone, LimestoneCap);
            Ground(c.RoadFlat, -1, Edge + .6f, 14f, 1.9f, BushFloor, 0f, 40f);
            Bank(c.RoadFlat, -1, 14f, 1.9f, 70f, 9f, BushFloorDark, 0f, 40f);
            for (int i = 0; i < 6; i++) Balga(c.Props, c.P(-F(c.Rnd, 10f, 14f), 1.9f, 3f + i * 6.5f), F(c.Rnd, .9f, 1.3f), c.NextSeed());
            for (float z = 5f; z < 40f; z += 9f) Gum(c.Front, c.P(-F(c.Rnd, 11f, 15f), 1.9f, z), F(c.Rnd, 15f, 19f), c.NextSeed());
            DeepBush(c, -1, 0f, 40f, 22f);
            // Right: the hillside falls away; gums and banksias down the slope.
            Ground(c.RoadFlat, 1, Edge, 10f, .14f, Lawn, 0f, 40f);
            Bank(c.RoadFlat, 1, 10f, .14f, 70f, -16f, BushFloor, 0f, 40f);
            LimestoneEdge(c, 1, 0f, 40f, .45f);
            for (float z = 2f; z < 40f; z += 9.5f) Gum(c.Front, c.P(F(c.Rnd, 10.5f, 13f), -.4f, z), F(c.Rnd, 16f, 20f), c.NextSeed());
            for (int i = 0; i < 4; i++)
            {
                float x = F(c.Rnd, 15f, 24f);
                Banksia(c.Props, c.P(x, .14f - (x - 10f) * 16f / 60f, F(c.Rnd, 3f, 37f)), F(c.Rnd, .9f, 1.2f), c.NextSeed());
            }
            for (float z = 4f; z < 40f; z += 8f) Gum(c.Deep, c.P(F(c.Rnd, 26f, 40f), -6f, z), F(c.Rnd, 15f, 20f), c.NextSeed(), true);
            c.StandardSlots();
            return c.Save();
        }

        /// <summary>Entry: calm limestone walls and hedges at z = 0, the KINGS PARK name wall, then the gums begin.</summary>
        static RoadChunk Entry()
        {
            ChunkCtx c = New("Entry", Standard);
            StandardRoad(c);
            for (int side = -1; side <= 1; side += 2)
            {
                ParkGround(c, side);
                LimestoneEdge(c, side, 0f, 40f, .45f);
                // Clipped hedges near the boundary (any district can meet them), gums from z 14 on.
                for (float z = 1f; z < 13f; z += 2.6f)
                    if (side < 0 || z < 5f) Shrub(c.Props, c.P(side * 10f, .14f, z), 1.25f, Color.Lerp(LeafGreen, Lawn, .3f));
                GumRows(c, side, 13f, 40f);
                DeepBush(c, side, 10f, 40f);
                Undergrowth(c, side, 2, 19f, 24f, 16f, 40f);
            }
            NameBlock(c.Front, c.P(12.5f, .14f, 9f), "KINGS PARK", .17f, Limestone, new Color(.24f, .36f, .2f));
            KangarooPaw(c.Props, c.P(9.6f, .14f, 8.6f), 1.2f, 31); KangarooPaw(c.Props, c.P(15.6f, .14f, 8.8f), 1.2f, 32);
            c.StandardSlots();
            return c.Save();
        }

        /// <summary>Exit: the steep run down towards the city; gums thin out to lawns, hedges and low walls at z = 40.</summary>
        static RoadChunk Exit()
        {
            ChunkCtx c = New("Exit", -6f);
            StandardRoad(c);
            for (int side = -1; side <= 1; side += 2)
            {
                ParkGround(c, side);
                LimestoneEdge(c, side, 0f, 40f, .45f);
                GumRows(c, side, 0f, 24f, side < 0);
                for (float z = 27f; z < 40f; z += 2.6f) Shrub(c.Props, c.P(side * 10f, .14f, z), 1.25f, Color.Lerp(LeafGreen, Lawn, .3f));
                DeepBush(c, side, 0f, 26f);
                RoundTree(c.Deep, c.P(side * 22f, .1f, 33f), 9f, Color.Lerp(Lawn, LeafGreen, .5f), 40 + side);
            }
            c.StandardSlots();
            return c.Save();
        }

        // ------------------------------------------------------------------ obstacles, animals, ramp, far view

        /// <summary>A fallen gum branch across a lane: jump it (or punch it away). Round limbs, a broken end, leaf blobs.</summary>
        static GameObject GumBranch()
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.Wood;
            Rod(m, V(-1.15f, .17f, .05f), V(1.1f, .24f, -.1f), .17f, GumBark, .75f);
            Rod(m, V(.2f, .24f, 0f), V(.72f, .55f, .45f), .08f, GumBark, .7f);
            Rod(m, V(-.5f, .2f, 0f), V(-.92f, .48f, -.4f), .07f, GumBark, .7f);
            m.Cylinder(V(-1.17f, .17f, .05f), .18f, .1f, new Color(.86f, .68f, .55f), 8, 1f, Quaternion.Euler(0, 0, 90f)); // broken end
            m.Box(V(-.3f, .26f, .14f), V(.5f, .15f, .07f), GumPatch, Quaternion.Euler(0, 0, 3f));
            Plant(m, V(.82f, .55f, .5f), .95f, GumLeaf, GumLeafLight);
            Plant(m, V(-.95f, .48f, -.42f), .75f, GumLeafDark, GumLeafLight);
            string path = PrefabFolder + "/OBS_KingsPark_GumBranch.prefab";
            return SavePrefab(path, MeshFolder, m, true, true, root =>
            {
                Box(root, V(0, .4f, 0), V(2.3f, .8f, .9f));
                var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
                o.kind = ObstacleKind.Thing; o.canJumpOver = true; o.spansRoad = false; o.punchable = true; o.animal = false; o.police = false; o.spawnWeight = 1.4f;
            });
        }

        /// <summary>Australian magpie (black and white, a bit bigger than life so it reads): an animal, not punchable.</summary>
        static GameObject Magpie()
        {
            var b = new ComicMesh();
            Color black = new Color(.1f, .1f, .12f), white = new Color(.97f, .97f, .95f), beak = new Color(.82f, .82f, .8f), eye = new Color(.75f, .25f, .1f);
            b.Surface = ComicSurface.Fabric;
            b.Box(V(0, .44f, .06f), V(.32f, .28f, .5f), black, Quaternion.Euler(-8f, 0, 0));   // body
            b.Box(V(0, .4f, -.14f), V(.3f, .24f, .16f), black);                                   // chest
            b.Box(V(0, .6f, -.06f), V(.24f, .16f, .14f), white);                                  // white nape
            foreach (float x in new[] { -.165f, .165f }) b.Box(V(x, .5f, .02f), V(.05f, .12f, .22f), white); // shoulder patches
            b.Box(V(0, .4f, .3f), V(.3f, .1f, .1f), white);                                       // white rump band
            b.Box(V(0, .38f, .46f), V(.2f, .08f, .26f), black, Quaternion.Euler(-10f, 0, 0));    // tail
            b.Box(V(0, .66f, -.2f), V(.22f, .2f, .22f), black);                                   // head
            b.Surface = ComicSurface.Plastic;
            b.Box(V(0, .63f, -.37f), V(.08f, .08f, .14f), beak);
            b.Box(V(0, .62f, -.46f), V(.06f, .06f, .06f), R.ink);
            foreach (float x in new[] { -.112f, .112f }) b.Box(V(x, .69f, -.25f), V(.03f, .05f, .05f), eye);
            b.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.07f, .07f })
            {
                b.Cylinder(V(x, .16f, .0f), .03f, .3f, new Color(.25f, .25f, .27f), 8);
                b.Box(V(x, .025f, -.04f), V(.08f, .05f, .16f), R.ink);
            }
            var m = new ComicMesh();
            m.Add(b, Vector3.zero, Quaternion.identity, Vector3.one * 1.35f);
            return SaveWalker(PrefabFolder + "/PED_KingsPark_Magpie.prefab", MeshFolder, m, V(0, .5f, 0), V(.55f, 1f, .9f), .5f, true, true);
        }

        /// <summary>A timber boardwalk kicker on limestone sides (generic ramp for this district).</summary>
        static GameObject BoardwalkRamp()
        {
            const float width = 2.6f, rise = 5f, height = 1.2f;
            var m = new ComicMesh();
            RampBody(m, 0f, width, 0f, rise, height, 0f, Timber, Limestone, R.yellow, ComicSurface.Wood);
            m.Surface = ComicSurface.Plain;
            for (float z = .5f; z < rise; z += .5f)
            {
                float y = height * z / rise + .01f;
                Face(m, V(-width * .5f + .05f, y, z - .025f), V(-width * .5f + .05f, y, z + .025f), V(width * .5f - .05f, y, z + .025f), V(width * .5f - .05f, y, z - .025f), new Color(.45f, .3f, .18f), Vector3.up, false);
            }
            return SaveRamp(PrefabFolder + "/RMP_KingsPark_Boardwalk.prefab", MeshFolder, m, width, rise, height, 0f, 1f, .5f, Array.Empty<int>());
        }

        /// <summary>
        /// Far view (follows the rider at the horizon, no colliders): the Swan River far below on the right with the
        /// South Perth shore, and the bush line of the park on both sides.
        /// </summary>
        static GameObject FarView()
        {
            var m = new ComicMesh();
            var r = Rng(77);
            m.Surface = ComicSurface.Plain;
            for (float z = -60f; z < 200f; z += 20f)
            {
                Flat(m, 60f, 260f, z, z + 20f, -42f, Water);
                Flat(m, 260f, 420f, z, z + 20f, -40f, new Color(.55f, .6f, .42f));
                for (int i = 0; i < 2; i++)
                {
                    float x = F(r, 80f, 220f), zz = z + F(r, 2f, 17f);
                    Flat(m, x, x + F(r, 15f, 35f), zz, zz + .8f, -41.9f, WaterLight);
                }
            }
            m.Surface = ComicSurface.Foliage;
            for (float z = -40f; z < 200f; z += 26f)
            {
                m.Ellipsoid(V(F(r, 270f, 330f), -36f, z), V(F(r, 50f, 80f), 14f, 30f), new Color(.42f, .52f, .34f));
                m.Ellipsoid(V(-F(r, 80f, 120f), 2f, z), V(F(r, 40f, 60f), 22f, 34f), GumLeafDark);
            }
            m.Surface = ComicSurface.Concrete;
            for (int i = 0; i < 6; i++)
                m.Box(V(F(r, 280f, 340f), -34f + 6f, F(r, 40f, 180f)), V(10f, 12f + i % 3 * 6f, 10f), Color.Lerp(R.cream, R.glass, .3f));
            m.Shear(-Standard / 40f); // follows the downhill ahead of the rider like the chunks do
            return SavePrefab(PrefabFolder + "/PRP_KingsPark_FarView.prefab", MeshFolder, m, false, false, root =>
            {
                foreach (Collider col in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(col, true);
            });
        }
    }
}
