using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CityScooter.EditorTools.Art;
using CityScooter.Gameplay;
using CityScooter.Rendering;
using CityScooter.Track;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// Shared generator kit for the Kings Park and Mounts Bay Rd districts (v0.3, art agent 2): the standard street
    /// cross-section (road 8 m, sidewalks 4 m at 0.2 m, cream kerb cap) with optional holes for RoadGaps, ground and
    /// banks, native plants (lemon-scented gums, balga, kangaroo paw, banksia), palms, rocks, limestone walls, pixel
    /// signs, and the prefab/mesh/asset plumbing (stable names and GUIDs on rebuild, like BoulevardBuilder).
    /// Only ComicPalette.mat and ComicInk.mat, vertex colours, ComicMesh geometry.
    /// </summary>
    internal static class ParkRiverKit
    {
        public const string DistrictData = "Assets/_Project/Data/Districts";
        public const string PeoplePrefabs = ComicCityBuilder.Prefabs + "/Hazards";
        public const int TriangleBudget = 25000, RendererBudget = 8;
        public const float Half = 4f, Walk = 4f, Kerb = .2f, Edge = Half + Walk;

        public static ComicCityRecipe R;
        public static Material Palette, InkMaterial;
        public static readonly List<string> Report = new List<string>();

        // Shared colours (sRGB, baked to linear by ComicMesh).
        public static readonly Color Limestone = new Color(.89f, .83f, .66f), LimestoneDark = new Color(.76f, .68f, .5f),
            LimestoneCap = new Color(.95f, .91f, .8f), WhiteWall = new Color(.94f, .92f, .86f);
        public static readonly Color GumBark = new Color(.94f, .92f, .87f), GumPatch = new Color(.8f, .74f, .73f),
            GumLeaf = new Color(.46f, .57f, .31f), GumLeafDark = new Color(.33f, .45f, .25f), GumLeafLight = new Color(.6f, .67f, .37f);
        public static readonly Color Lawn = new Color(.47f, .64f, .3f), LawnLight = new Color(.56f, .71f, .34f),
            BushFloor = new Color(.64f, .58f, .38f), BushFloorDark = new Color(.52f, .48f, .32f);
        public static readonly Color PawRed = new Color(.86f, .16f, .13f), PawGreen = new Color(.4f, .66f, .25f), LeafGreen = new Color(.3f, .5f, .24f);
        public static readonly Color BalgaTrunk = new Color(.17f, .15f, .14f), BalgaGrass = new Color(.55f, .65f, .27f), BalgaGrassDark = new Color(.4f, .5f, .2f),
            Spear = new Color(.55f, .4f, .25f), SpearFlower = new Color(.93f, .87f, .66f);
        public static readonly Color BanksiaLeaf = new Color(.3f, .43f, .27f), BanksiaCone = new Color(.95f, .62f, .16f);
        public static readonly Color Water = new Color(.2f, .48f, .7f), WaterLight = new Color(.38f, .63f, .82f), WaterFoam = new Color(.78f, .9f, .96f);
        public static readonly Color Rock = new Color(.66f, .63f, .57f), RockDark = new Color(.5f, .48f, .44f);
        public static readonly Color PalmTrunk = new Color(.56f, .42f, .29f), PalmLeaf = new Color(.33f, .58f, .25f), PalmLeafDark = new Color(.24f, .45f, .2f);
        public static readonly Color Steel = new Color(.62f, .66f, .71f), SteelDark = new Color(.32f, .33f, .4f), Glass = new Color(.72f, .9f, .9f);
        public static readonly Color PinkDaisy = new Color(.96f, .55f, .7f), DaisyCentre = new Color(1f, .82f, .25f);

        public static void Init()
        {
            R = AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(ComicCityBuilder.RecipePath);
            Palette = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicPalette.mat");
            InkMaterial = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicInk.mat");
            if (R == null || Palette == null || InkMaterial == null)
                throw new InvalidOperationException("Build the comic city first (recipe, ComicPalette.mat, ComicInk.mat).");
            if (R.roadHalfWidth != Half || R.sidewalkWidth != Walk || Mathf.Abs(R.curbHeight - Kerb) > 1e-4f)
                throw new InvalidOperationException("District chunks assume the standard street cross-section (4 / 4 / 0.2).");
            ComicSurfaceAssets.EnsureLoaded();
            PrefabBuildUtility.EnsureFolder(DistrictData);
            Report.Clear();
        }

        public static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // A quad facing `normal` whatever order the corners come in.
        public static void Face(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector3 normal, bool ink = true)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) m.Quad(d, c, b, a, color, ink);
            else m.Quad(a, b, c, d, color, ink);
        }

        // Horizontal quad at height y.
        public static void Flat(ComicMesh m, float x0, float x1, float z0, float z1, float y, Color color, bool ink = false)
        {
            Face(m, V(x0, y, z0), V(x0, y, z1), V(x1, y, z1), V(x1, y, z0), color, Vector3.up, ink);
        }

        public static System.Random Rng(int seed) => new System.Random(unchecked(seed * 7919 + 104729));
        public static float F(System.Random r, float a, float b) => Mathf.Lerp(a, b, (float)r.NextDouble());

        // ------------------------------------------------------------------ road

        /// <summary>
        /// The street cross-section, built flat (the chunk shears it to its slope): road of half width c.HalfAt(z) (4 = the
        /// standard street, 8 = a 4-lane divided road, changing smoothly inside entry / exit chunks), 4 m sidewalks at
        /// 0.2 m, cream kerb cap. The 2-lane road gets the street's dashed double yellow; the wide road a painted median
        /// (double yellow and white hatching, flush: the rider can cross it) and dashed white lane lines at x = +-4.
        /// Holes (RoadGaps, only in constant-width chunks) leave the surface out; their walls are art of the chunk.
        /// walk(side, z) gives the sidewalk colour, joints(side, z) whether it is paving (joints) or a smooth path.
        /// </summary>
        public static void Road(ChunkCtx c, Func<int, float, Color> walk, Func<int, float, bool> joints, Color? centreLine = null)
        {
            ComicMesh m = c.RoadFlat;
            float L = c.L, h = Kerb, sw = Walk;
            Color joint = Color.Lerp(R.ink, R.pavement, .5f), yellow = centreLine ?? R.yellow, white = new Color(.95f, .95f, .93f);
            bool holes = c.Gaps.Count > 0;
            if (holes && c.ChangesWidth) throw new InvalidOperationException(c.Name + ": road gaps only in constant-width chunks.");
            const float step = 2f;
            for (float z0 = 0f; z0 < L - .001f; z0 += step)
            {
                float z1 = z0 + step, zc = (z0 + z1) * .5f, w0 = c.HalfAt(z0), w1 = c.HalfAt(z1);
                m.Surface = ComicSurface.Asphalt;
                if (!holes) Face(m, V(-w0, 0, z0), V(-w1, 0, z1), V(w1, 0, z1), V(w0, 0, z0), R.asphalt, Vector3.up);
                else foreach (Vector2 s in c.Free(-w0, w0, z0, z1)) Face(m, V(s.x, 0, z0), V(s.x, 0, z1), V(s.y, 0, z1), V(s.y, 0, z0), R.asphalt, Vector3.up);
                for (int side = -1; side <= 1; side += 2)
                {
                    Color pave = walk(side, zc);
                    bool paving = joints(side, zc);
                    m.Surface = paving ? ComicSurface.Paving : ComicSurface.Asphalt;
                    if (!holes) Face(m, V(side * w0, h, z0), V(side * w1, h, z1), V(side * (w1 + sw), h, z1), V(side * (w0 + sw), h, z0), pave, Vector3.up);
                    else
                    {
                        float lo = side < 0 ? -(w0 + sw) : w0, hi = side < 0 ? -w0 : w0 + sw;
                        foreach (Vector2 s in c.Free(lo, hi, z0, z1)) Face(m, V(s.x, h, z0), V(s.x, h, z1), V(s.y, h, z1), V(s.y, h, z0), pave, Vector3.up);
                    }
                    Vector3 inward = V(-side, 0, 0), outward = V(side, 0, 0);
                    float i0 = side * w0, i1 = side * w1, o0 = side * (w0 + sw), o1 = side * (w1 + sw);
                    if (!c.InHole(side * (w0 + .1f), z0, z1))
                    {
                        m.Surface = paving ? ComicSurface.Paving : ComicSurface.Asphalt;
                        Face(m, V(i0, -.05f, z0), V(i1, -.05f, z1), V(i1, h, z1), V(i0, h, z0), pave, inward);
                        m.Surface = ComicSurface.Plain;
                        float c0 = side * (w0 + .24f), c1 = side * (w1 + .24f), top = h + .04f;
                        Face(m, V(i0, top, z0), V(i1, top, z1), V(c1, top, z1), V(c0, top, z0), R.cream, Vector3.up);
                        Face(m, V(i0, h, z0), V(i1, h, z1), V(i1, top, z1), V(i0, top, z0), R.cream, inward);
                        Face(m, V(c0, h, z0), V(c1, h, z1), V(c1, top, z1), V(c0, top, z0), R.cream, outward);
                    }
                    if (!c.InHole(side * (w0 - .15f), z0, z1))
                        Strip(m, side * (w0 - .19f), side * (w0 - .11f), side * (w1 - .19f), side * (w1 - .11f), z0, z1, .0305f, R.cream);
                    if (!c.InHole(o0 - side * .1f, z0, z1))
                    {
                        m.Surface = ComicSurface.Concrete;
                        Face(m, V(o0, -.6f, z0), V(o1, -.6f, z1), V(o1, h, z1), V(o0, h, z0), pave, outward);
                    }
                    m.Surface = ComicSurface.Plain;
                    if (paving)
                    {
                        for (int k = 1; k < 4; k++)
                            if (!c.InHole(side * (w0 + k), z0, z1))
                                Strip(m, side * (w0 + k) - .0125f, side * (w0 + k) + .0125f, side * (w1 + k) - .0125f, side * (w1 + k) + .0125f, z0, z1, h + .025f, joint);
                        if (!c.InHole(side * (w0 + 2f), z0 - .02f, z0 + .02f))
                            Flat(m, side * (w0 + .24f), side * (w0 + sw), z0 - .0125f, z0 + .0125f, h + .025f, joint);
                    }
                    else if (side < 0 && walk(side, zc) != R.pavement && Mathf.FloorToInt(z0 / 2f) % 2 == 0 && !c.InHole(-(w0 + 2f), z0, z1))
                        Strip(m, -(w0 + 2.05f), -(w0 + 1.95f), -(w1 + 2.05f), -(w1 + 1.95f), z0 + .3f, z1 - .3f, h + .025f, white); // shared-path centre dashes
                }
            }
            // Centre: the street's dashed double yellow (3 m dashes every 5 m) where the road is 2 lanes; where it is wider,
            // a solid double yellow that opens into a painted median with white hatching.
            float Offset(float z) => Mathf.Lerp(.12f, .55f, Mathf.InverseLerp(4f, 8f, c.HalfAt(z)));
            for (float z = 0f; z < L - .001f; z += 5f)
                if (c.HalfAt(z) <= 4.05f && c.HalfAt(z + 3f) <= 4.05f && !c.InHole(0f, z, z + 3f))
                    for (int s = -1; s <= 1; s += 2)
                        for (float a = z; a < z + 3f - .001f; a += 1.5f) Flat(m, s * .12f - .05f, s * .12f + .05f, a, a + 1.5f, .035f, yellow);
            for (float z = 0f; z < L - .001f; z += 1f)
            {
                if (c.HalfAt(z) <= 4.05f && c.HalfAt(z + 1f) <= 4.05f || c.InHole(0f, z, z + 1f)) continue;
                float a = Offset(z), b = Offset(z + 1f);
                for (int s = -1; s <= 1; s += 2) Strip(m, s * a - .05f, s * a + .05f, s * b - .05f, s * b + .05f, z, z + 1f, .035f, yellow);
                if (a >= .5f && b >= .5f && Mathf.RoundToInt(z) % 2 == 0)
                    Face(m, V(-.42f, .034f, z), V(-.42f, .034f, z + .3f), V(.42f, .034f, z + 1f), V(.42f, .034f, z + .7f), white, Vector3.up, false);
            }
            // Lane lines: 3 m white dashes every 8 m at x = +-4 wherever both lanes on that side are wide enough.
            for (float z = 0f; z < L - .001f; z += 8f)
                if (Mathf.Min(c.HalfAt(z), c.HalfAt(z + 3f)) >= 6.9f && !c.InHole(4f, z, z + 3f))
                    for (int s = -1; s <= 1; s += 2)
                        for (float a = z; a < z + 3f - .001f; a += 1.5f) Flat(m, s * Half - .06f, s * Half + .06f, a, a + 1.5f, .035f, white);
            // Closed slab ends (hidden by the neighbouring chunk).
            foreach (float ze in new[] { 0f, L })
            {
                Vector3 normal = ze <= 0f ? Vector3.back : Vector3.forward;
                float za = ze <= 0f ? 0f : L - .5f, zb = ze <= 0f ? .5f : L, w = c.HalfAt(ze);
                if (c.InHole(0f, za, zb)) continue;
                m.Surface = ComicSurface.Asphalt;
                Face(m, V(-w, -.8f, ze), V(w, -.8f, ze), V(w, 0, ze), V(-w, 0, ze), R.asphalt, normal);
                m.Surface = ComicSurface.Paving;
                for (int s = -1; s <= 1; s += 2)
                    Face(m, V(s * w, -.6f, ze), V(s * (w + sw), -.6f, ze), V(s * (w + sw), h, ze), V(s * w, h, ze), R.pavement, normal);
            }
        }

        /// <summary>A flat quad at height y whose left / right edges move between z0 and z1 (paint on a widening road).</summary>
        public static void Strip(ComicMesh m, float l0, float r0, float l1, float r1, float z0, float z1, float y, Color color)
        {
            m.Surface = ComicSurface.Plain;
            Face(m, V(l0, y, z0), V(l1, y, z1), V(r1, y, z1), V(r0, y, z0), color, Vector3.up, false);
        }

        /// <summary>Flat ground from |x| = xa to xb on one side at height y, in 4 m spans (the GPU curl bends them).</summary>
        public static void Ground(ComicMesh m, int side, float xa, float xb, float y, Color color, float z0, float z1, ComicSurface surface = ComicSurface.Foliage)
        {
            m.Surface = surface;
            for (float z = z0; z < z1 - .001f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                Face(m, V(side * xa, y, z), V(side * xa, y, e), V(side * xb, y, e), V(side * xb, y, z), color, Vector3.up, false);
            }
        }

        /// <summary>A sloping bank from (|x| = xa, ya) to (xb, yb) on one side.</summary>
        public static void Bank(ComicMesh m, int side, float xa, float ya, float xb, float yb, Color color, float z0, float z1, ComicSurface surface = ComicSurface.Foliage)
        {
            m.Surface = surface;
            Vector3 n = Vector3.Cross(V(0, 0, 1), V(side * (xb - xa), yb - ya, 0)) * -side;
            if (n.y < 0f) n = -n;
            for (float z = z0; z < z1 - .001f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                Face(m, V(side * xa, ya, z), V(side * xa, ya, e), V(side * xb, yb, e), V(side * xb, yb, z), color, n, false);
            }
        }

        /// <summary>Random lighter / darker patches on flat ground (dappled light, dry grass).</summary>
        public static void Patches(ComicMesh m, System.Random rng, int side, float xa, float xb, float y, Color a, Color b, float z0, float z1, int count)
        {
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < count; i++)
            {
                float w = F(rng, 1.2f, 3.5f), d = F(rng, 1.2f, 3.5f);
                float x = F(rng, xa + w * .5f, xb - w * .5f), z = F(rng, z0 + d * .5f, z1 - d * .5f);
                Flat(m, side * (x - w * .5f), side * (x + w * .5f), z - d * .5f, z + d * .5f, y + .02f + i * .001f, i % 2 == 0 ? a : b);
            }
        }

        /// <summary>A low wall along z (flat; the chunk shears it): blocks of 4 m (their inked edges read as joints) and a cap.</summary>
        public static void Wall(ComicMesh m, float x, float thick, float height, float z0, float z1, Color body, Color cap, float bottom = -.6f, float block = 4f)
        {
            m.Surface = ComicSurface.Concrete;
            for (float z = z0; z < z1 - .001f; z += block)
            {
                float e = Mathf.Min(z1, z + block), zc = (z + e) * .5f, len = e - z;
                m.Box(V(x, (height + bottom) * .5f, zc), V(thick, height - bottom, len), body);
                m.Box(V(x, height + .06f, zc), V(thick + .12f, .12f, len), cap);
            }
        }

        // ------------------------------------------------------------------ plants and things (upright, base at p)

        // Like ComicKitGeometry's Plant(): a faceted low-poly blob and a smaller, lighter one on top of it (two tones).
        public static void Plant(ComicMesh m, Vector3 c, float size, Color dark, Color light)
        {
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(c, V(size, size * .7f, size), dark);
            m.Ellipsoid(c + V(size * .23f, size * .15f, 0f), V(size * .65f, size * .6f, size * .65f), light);
        }

        /// <summary>An 8-sided round rod from a to b (limbs, stems, rails): closed, never a plank.</summary>
        public static void Rod(ComicMesh m, Vector3 a, Vector3 b, float radius, Color color, float topScale = 1f, int sides = 8)
        {
            Vector3 d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            m.Cylinder((a + b) * .5f, radius, d.magnitude, color, sides, topScale, Quaternion.FromToRotation(Vector3.up, d.normalized));
        }

        /// <summary>A closed box between z0 and z1 whose bottom / top heights change along z (sloped deck pieces, rails).</summary>
        public static void SlopeBox(ComicMesh m, float x0, float x1, float z0, float z1, float b0, float t0, float b1, float t1, Color c)
        {
            Face(m, V(x0, t0, z0), V(x0, t1, z1), V(x1, t1, z1), V(x1, t0, z0), c, Vector3.up);
            Face(m, V(x0, b0, z0), V(x0, b1, z1), V(x1, b1, z1), V(x1, b0, z0), c, Vector3.down);
            Face(m, V(x0, b0, z0), V(x0, b1, z1), V(x0, t1, z1), V(x0, t0, z0), c, Vector3.left);
            Face(m, V(x1, b0, z0), V(x1, b1, z1), V(x1, t1, z1), V(x1, t0, z0), c, Vector3.right);
            Face(m, V(x0, b0, z0), V(x1, b0, z0), V(x1, t0, z0), V(x0, t0, z0), c, Vector3.back);
            Face(m, V(x0, b1, z1), V(x1, b1, z1), V(x1, t1, z1), V(x0, t1, z1), c, Vector3.forward);
        }

        /// <summary>
        /// Lemon-scented gum, built like the kit's street tree: navy ink root ring, a tall smooth cream-white 8-sided trunk
        /// (slightly tapered, slight lean), two 8-sided limbs from a high fork, and sparse high olive blob clusters (two
        /// tones). cheap = background version (6 sides, no ring, one cluster).
        /// </summary>
        public static void Gum(ComicMesh m, Vector3 p, float h, int seed, bool cheap = false)
        {
            var r = Rng(seed);
            float trunkH = h * F(r, .52f, .6f), r0 = .2f + .02f * h;
            Quaternion q = Quaternion.Euler(F(r, -3f, 3f), 0f, F(r, -4f, 4f));
            if (!cheap)
            {
                m.Surface = ComicSurface.PaintedMetal;
                m.Cylinder(p + V(0, .1f, 0), r0 * 1.45f, .2f, R.ink, 8, .9f);
            }
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + q * V(0, trunkH * .5f, 0), r0, trunkH, GumBark, cheap ? 6 : 8, .68f, q);
            Vector3 top = p + q * V(0, trunkH, 0);
            if (cheap)
            {
                Plant(m, top + V(0, h * .1f, 0), h * .34f, GumLeaf, GumLeafLight);
                return;
            }
            float a0 = F(r, 0f, 360f);
            m.Box(p + q * V(0, trunkH * F(r, .25f, .45f), 0) + Quaternion.Euler(0, a0, 0) * V(r0 * .74f, 0, 0), V(.08f, F(r, .9f, 1.5f), r0 * .9f), GumPatch, q * Quaternion.Euler(0, a0, 0));
            float spin = F(r, 0f, 360f);
            for (int i = 0; i < 2; i++)
            {
                float a = (spin + i * 155f + F(r, -20f, 20f)) * Mathf.Deg2Rad;
                Vector3 from = i == 0 ? top : p + q * V(0, trunkH * .86f, 0);
                Vector3 end = from + V(Mathf.Cos(a) * h * F(r, .1f, .17f), h * F(r, .17f, .27f), Mathf.Sin(a) * h * F(r, .1f, .15f));
                m.Surface = ComicSurface.Wood;
                Rod(m, from, end, r0 * .5f, GumBark, .7f);
                Plant(m, end + V(0, h * .02f, 0), h * (i == 0 ? F(r, .34f, .4f) : F(r, .27f, .32f)), i == 0 ? GumLeaf : GumLeafDark, GumLeafLight);
            }
        }

        /// <summary>A round-crowned tree (peppermint, fig) like the kit's street tree: ink root ring, 8-sided trunk, blob crown.</summary>
        public static void RoundTree(ComicMesh m, Vector3 p, float h, Color leaf, int seed)
        {
            var r = Rng(seed);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .1f, 0), .3f + h * .03f, .2f, R.ink, 8, .9f);
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, h * .27f, 0), .16f + h * .02f, h * .54f, new Color(.45f, .34f, .25f), 8, .75f);
            Plant(m, p + V(0, h * .6f, 0), h * .62f, leaf, Color.Lerp(leaf, R.yellow, .25f));
            Plant(m, p + V(F(r, -.15f, .15f) * h, h * .78f, F(r, -.12f, .12f) * h), h * .4f, Color.Lerp(leaf, R.ink, .1f), Color.Lerp(leaf, R.yellow, .3f));
        }

        /// <summary>Grass tree (balga): black 8-sided trunk, a spiky skirt of thin closed boxes, a flower spear.</summary>
        public static void Balga(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = Rng(seed);
            float trunk = s * F(r, .7f, 1.3f);
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, trunk * .5f, 0), .22f * s, trunk, BalgaTrunk, 8, .88f);
            Vector3 top = p + V(0, trunk, 0);
            m.Surface = ComicSurface.Foliage;
            float[] ups = { 1.6f, .5f, -.35f, -1.1f };
            for (int i = 0; i < 16; i++)
            {
                float a = (i * 22.5f + (i % 4) * 11f + F(r, -6f, 6f)) * Mathf.Deg2Rad, up = ups[i % 4];
                Vector3 dir = new Vector3(Mathf.Cos(a), up, Mathf.Sin(a)).normalized;
                m.Box(top + dir * .42f * s, V(.09f * s, .07f * s, .85f * s), i % 2 == 0 ? BalgaGrass : BalgaGrassDark, Quaternion.LookRotation(dir));
            }
            m.Ellipsoid(top + V(0, .05f * s, 0), V(.5f * s, .35f * s, .5f * s), BalgaGrassDark);
            m.Surface = ComicSurface.Wood;
            Rod(m, top, top + V(0, 1.75f * s, 0), .06f * s, Spear);
            m.Cylinder(top + V(0, 1.38f * s, 0), .1f * s, .72f * s, SpearFlower, 8, .8f);
        }

        /// <summary>Red-and-green kangaroo paw (WA's floral emblem): leaf blob, red 8-sided stems, green claw flowers.</summary>
        public static void KangarooPaw(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = Rng(seed);
            Plant(m, p + V(0, .22f * s, 0), .62f * s, LeafGreen, Color.Lerp(LeafGreen, R.yellow, .25f));
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + F(r, -30f, 30f)) * Mathf.Deg2Rad, tall = F(r, .95f, 1.25f) * s;
                Vector3 tip = p + V(Mathf.Cos(a) * .24f * s, tall, Mathf.Sin(a) * .24f * s);
                m.Surface = ComicSurface.Fabric;
                Rod(m, p + V(0, .2f * s, 0), tip, .04f * s, PawRed);
                m.Box(tip + V(0, .05f * s, 0), V(.15f * s, .13f * s, .15f * s), PawRed);
                m.Surface = ComicSurface.Foliage;
                m.Box(tip + V(.08f * s, .18f * s, 0), V(.08f * s, .24f * s, .1f * s), PawGreen, Quaternion.Euler(0, 0, -26f));
                m.Box(tip + V(-.08f * s, .18f * s, 0), V(.08f * s, .24f * s, .1f * s), PawGreen, Quaternion.Euler(0, 0, 26f));
            }
        }

        /// <summary>Banksia: a rounded shrub of dark blobs with a few upright yellow-orange flower cones.</summary>
        public static void Banksia(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = Rng(seed);
            Plant(m, p + V(0, .7f * s, 0), 1.8f * s, BanksiaLeaf, Color.Lerp(BanksiaLeaf, GumLeafLight, .5f));
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(p + V(-.55f * s, .45f * s, .3f * s), V(1.1f * s, .8f * s, 1f * s), Color.Lerp(BanksiaLeaf, R.ink, .12f));
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + F(r, 0f, 40f)) * Mathf.Deg2Rad;
                m.Surface = ComicSurface.Fabric;
                m.Cylinder(p + V(Mathf.Cos(a) * .5f * s, 1.25f * s, Mathf.Sin(a) * .45f * s), .13f * s, .45f * s, i % 2 == 0 ? BanksiaCone : R.yellow, 8, .7f);
            }
        }

        public static void Shrub(ComicMesh m, Vector3 p, float s, Color c)
        {
            Plant(m, p + V(0, .42f * s, 0), 1.45f * s, c, Color.Lerp(c, R.yellow, .25f));
        }

        /// <summary>Everlasting daisies (pink paper daisies) in a raised limestone bed with a cream rim.</summary>
        public static void DaisyBed(ComicMesh m, float x0, float x1, float z0, float z1, float y, System.Random r, int heads)
        {
            float xa = Mathf.Min(x0, x1), xb = Mathf.Max(x0, x1), xc = (xa + xb) * .5f, zc = (z0 + z1) * .5f;
            m.Surface = ComicSurface.Concrete;
            m.Box(V(xc, y + .14f, zc), V(xb - xa, .28f, z1 - z0), Limestone);
            m.Box(V(xc, y + .3f, zc), V(xb - xa + .06f, .08f, z1 - z0 + .06f), LimestoneCap);
            m.Surface = ComicSurface.Foliage;
            m.Box(V(xc, y + .31f, zc), V(xb - xa - .3f, .1f, z1 - z0 - .3f), Lawn);
            for (int i = 0; i < heads; i++)
            {
                float x = F(r, xa + .35f, xb - .35f), z = F(r, z0 + .35f, z1 - .35f), turn = F(r, 0f, 90f);
                Color c = i % 4 == 3 ? Color.white : i % 4 == 2 ? new Color(.98f, .7f, .8f) : PinkDaisy;
                m.Surface = ComicSurface.Fabric;
                m.Box(V(x, y + .5f, z), V(.3f, .07f, .12f), c, Quaternion.Euler(0, turn, 0));
                m.Box(V(x, y + .5f, z), V(.3f, .07f, .12f), c, Quaternion.Euler(0, turn + 90f, 0));
                m.Box(V(x, y + .55f, z), V(.11f, .06f, .11f), DaisyCentre);
                m.Box(V(x, y + .42f, z), V(.06f, .16f, .06f), LeafGreen);
            }
        }

        /// <summary>Washington palm: ringed 8-sided trunk segments, a crown knob, fronds of three thick bent slabs arching down.</summary>
        public static void Palm(ComicMesh m, Vector3 p, float h, int seed)
        {
            var r = Rng(seed);
            Quaternion q = Quaternion.Euler(F(r, -4f, 4f), 0, F(r, -5f, 5f));
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .1f, 0), .45f, .2f, R.ink, 8, .9f);
            m.Surface = ComicSurface.Wood;
            const int segs = 4;
            for (int i = 0; i < segs; i++)
            {
                float y0 = h * i / segs, len = h / segs;
                float rad = Mathf.Lerp(.32f, .24f, (float)i / segs);
                m.Cylinder(p + q * V(0, y0 + len * .5f, 0), rad, len - .06f, i % 2 == 0 ? PalmTrunk : Color.Lerp(PalmTrunk, R.ink, .18f), 8, .9f, q);
            }
            Vector3 top = p + q * V(0, h, 0);
            m.Cylinder(top + V(0, .2f, 0), .42f, .6f, new Color(.45f, .36f, .22f), 8, .75f);
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < 7; i++)
            {
                Quaternion yaw = Quaternion.Euler(0, i * 51.4f + F(r, -10f, 10f), 0);
                Vector3 at = top + V(0, .35f, 0);
                float pitch = -12f + F(r, -6f, 6f);
                for (int k = 0; k < 3; k++)
                {
                    Quaternion f = yaw * Quaternion.Euler(pitch, 0, 0);
                    float len = 1.75f - k * .25f, wide = .85f - k * .2f;
                    Vector3 mid = at + f * V(0, 0, len * .5f);
                    m.Box(mid, V(wide, .09f, len + .05f), (i + k) % 2 == 0 ? PalmLeaf : PalmLeafDark, f);
                    at += f * V(0, 0, len);
                    pitch += 26f;
                }
            }
        }

        /// <summary>A blocky boulder (comic rock).</summary>
        public static void Boulder(ComicMesh m, Vector3 p, float s, System.Random r, Color? colour = null)
        {
            m.Surface = ComicSurface.Concrete;
            Color c = colour ?? Color.Lerp(Rock, RockDark, F(r, 0f, .7f));
            m.Box(p, V(s * F(r, .9f, 1.4f), s * F(r, .6f, .9f), s * F(r, .8f, 1.2f)), c, Quaternion.Euler(F(r, -12f, 12f), F(r, 0f, 90f), F(r, -12f, 12f)));
        }

        // ------------------------------------------------------------------ pixel signs (place names only)

        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "010101111101101", ['B'] = "110101110101110", ['C'] = "011100100100011", ['D'] = "110101101101110",
            ['E'] = "111100110100111", ['F'] = "111100110100100", ['G'] = "011100101101011", ['H'] = "101101111101101",
            ['I'] = "111010010010111", ['J'] = "001001001101010", ['K'] = "101101110101101", ['L'] = "100100100100111",
            ['M'] = "101111111101101", ['N'] = "110101101101101", ['O'] = "010101101101010", ['P'] = "110101110100100",
            ['R'] = "110101110101101", ['S'] = "011100010001110", ['T'] = "111010010010010", ['U'] = "101101101101111",
            ['V'] = "101101101101010", ['W'] = "101101101111101", ['Y'] = "101101010010010", ['Z'] = "111001010100111",
        };

        /// <summary>Pixel text facing -Z (towards the rider), centred on (cx, cy) at z.</summary>
        public static void Text(ComicMesh m, string text, float cx, float cy, float z, float px, Color c)
        {
            m.Surface = ComicSurface.Plain;
            float width = (text.Length * 4 - 1) * px, x0 = cx - width / 2, y0 = cy + 2.5f * px;
            for (int i = 0; i < text.Length; i++)
            {
                if (!Glyphs.TryGetValue(text[i], out string g)) continue;
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < 3; col++)
                    {
                        if (g[row * 3 + col] != '1') continue;
                        float x = x0 + (i * 4 + col) * px, y = y0 - (row + 1) * px;
                        m.Quad(V(x + px, y, z), V(x, y, z), V(x, y + px, z), V(x + px, y + px, z), c, false);
                    }
            }
        }

        public static float TextWidth(string text, float px) => (text.Length * 4 - 1) * px;

        /// <summary>A limestone name block facing the rider (like the Kings Park entry walls), base at p.</summary>
        public static void NameBlock(ComicMesh m, Vector3 p, string text, float px, Color stone, Color letters)
        {
            float w = TextWidth(text, px) + 1.2f, h = 5 * px + .9f;
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, h * .5f - .3f, 0), V(w, h + .6f, .7f), stone);
            m.Box(p + V(0, .12f, 0), V(w + .14f, .24f, .84f), R.ink);             // navy plinth band
            m.Box(p + V(0, h + .05f, 0), V(w + .2f, .14f, .85f), LimestoneCap);  // cream cap
            Text(m, text, p.x, p.y + h * .55f, p.z - .36f, px, letters);
        }

        // ------------------------------------------------------------------ meshes, renderers, prefabs

        public static Mesh SaveMesh(string folder, string id, ComicMesh data)
        {
            PrefabBuildUtility.EnsureFolder(folder);
            string path = folder + "/" + id + ".asset";
            Mesh mesh = data.Bake(id), existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            // Unity culls on the CPU before the vertex shader curls the far city up (as BoulevardBuilder.SaveMesh).
            Bounds b = mesh.bounds;
            b.Encapsulate(b.max + Vector3.up * (ComicCityProjection.Rise(R, 360) + 1));
            mesh.bounds = b;
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); existing = mesh; }
            else
            {
                var writable = new SerializedObject(existing);
                writable.FindProperty("m_IsReadable").boolValue = true;
                writable.ApplyModifiedPropertiesWithoutUndo();
                existing.Clear(); existing.indexFormat = mesh.indexFormat;
                existing.SetVertices(mesh.vertices); existing.SetNormals(mesh.normals); existing.SetColors(mesh.colors);
                var uv = new List<Vector3>(); mesh.GetUVs(0, uv); existing.SetUVs(0, uv);
                mesh.GetUVs(1, uv); existing.SetUVs(1, uv);
                var surfaceUV = new List<Vector4>(); mesh.GetUVs(2, surfaceUV); existing.SetUVs(2, surfaceUV);
                existing.SetTriangles(mesh.triangles, 0); existing.bounds = mesh.bounds;
                existing.UploadMeshData(false);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
            }
            var so = new SerializedObject(existing);
            SerializedProperty readable = so.FindProperty("m_IsReadable");
            if (readable != null) { readable.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
            return existing;
        }

        /// <summary>ComicPalette renderer plus (optionally) the ComicInk outline child, like the street chunks.</summary>
        public static void Renderer(GameObject go, string folder, string id, ComicMesh data, bool outline, bool shadows)
        {
            Mesh mesh = SaveMesh(folder, id, data);
            PrefabBuildUtility.GetOrAdd<MeshFilter>(go).sharedMesh = mesh;
            var mr = PrefabBuildUtility.GetOrAdd<MeshRenderer>(go);
            mr.sharedMaterial = Palette;
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = true;
            var children = new ChildSync(go.transform);
            if (outline)
            {
                GameObject ink = children.Plain("InkOutline");
                PrefabBuildUtility.GetOrAdd<MeshFilter>(ink).sharedMesh = mesh;
                var imr = PrefabBuildUtility.GetOrAdd<MeshRenderer>(ink);
                imr.sharedMaterial = InkMaterial; imr.shadowCastingMode = ShadowCastingMode.Off; imr.receiveShadows = false;
            }
            children.Finish();
        }

        /// <summary>A small prefab: root (components via configure) + Art child (body and outline).</summary>
        public static GameObject SavePrefab(string path, string meshFolder, ComicMesh body, bool outline, bool shadows, Action<GameObject> configure)
        {
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            GameObject art = sync.Plain("Art");
            art.transform.localPosition = Vector3.zero; art.transform.localRotation = Quaternion.identity; art.transform.localScale = Vector3.one;
            Renderer(art, meshFolder, System.IO.Path.GetFileNameWithoutExtension(path), body, outline, shadows);
            sync.Finish();
            configure?.Invoke(root);
            PrefabBuildUtility.SaveAndClose(root, path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static void Box(GameObject root, Vector3 centre, Vector3 size)
        {
            var box = PrefabBuildUtility.KeepSingle<BoxCollider>(root) ?? root.AddComponent<BoxCollider>();
            box.center = centre; box.size = size; box.isTrigger = false;
        }

        /// <summary>A people / animal prefab for the sidewalks (and the road): Obstacle kind Person + Pedestrian.</summary>
        public static GameObject SaveWalker(string path, string meshFolder, ComicMesh body, Vector3 colliderCentre, Vector3 colliderSize,
            float walk, bool animal, bool jumpable)
        {
            return SavePrefab(path, meshFolder, body, true, false, root =>
            {
                Box(root, colliderCentre, colliderSize);
                var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
                o.kind = ObstacleKind.Person; o.animal = animal; o.punchable = false; o.police = false; o.canJumpOver = jumpable; o.spansRoad = false; o.spawnWeight = 1f;
                var p = PrefabBuildUtility.GetOrAdd<Pedestrian>(root);
                p.walkSpeed = walk; p.crosses = false; p.art = root.transform.Find("Art");
            });
        }

        /// <summary>A ramp prefab the generator can put on the road (Ramp on the root, art built to match its numbers).</summary>
        public static GameObject SaveRamp(string path, string meshFolder, ComicMesh body, float width, float rise, float height, float top, float launch, float hang, int[] lanes)
        {
            return SavePrefab(path, meshFolder, body, true, true, root =>
            {
                foreach (Collider c in root.GetComponents<Collider>()) Object.DestroyImmediate(c, true);
                var ramp = PrefabBuildUtility.GetOrAdd<Ramp>(root);
                ramp.width = width; ramp.rise = rise; ramp.height = height; ramp.top = top; ramp.launch = launch; ramp.hangGravity = hang; ramp.lanes = lanes;
            });
        }

        /// <summary>A generic ramp body: wedge + optional flat top, side skirts down to -0.8 m so it sits on any slope.</summary>
        public static void RampBody(ComicMesh m, float x, float width, float z0, float rise, float height, float top, Color deck, Color side, Color lip, ComicSurface deckSurface)
        {
            float w = width * .5f, zr = z0 + rise, zl = zr + top;
            const int steps = 4;
            for (int i = 0; i < steps; i++)
            {
                float a = z0 + rise * i / steps, b = z0 + rise * (i + 1) / steps, ya = height * i / steps, yb = height * (i + 1) / steps;
                m.Surface = deckSurface;
                Face(m, V(x - w, ya, a), V(x - w, yb, b), V(x + w, yb, b), V(x + w, ya, a), deck, Vector3.up);
                m.Surface = ComicSurface.Concrete;
                for (int s = -1; s <= 1; s += 2)
                    Face(m, V(x + s * w, -.8f, a), V(x + s * w, -.8f, b), V(x + s * w, yb, b), V(x + s * w, ya, a), side, V(s, 0, 0));
            }
            if (top > .01f)
                for (float a = zr; a < zl - .001f; a += 4f)
                {
                    float b = Mathf.Min(zl, a + 4f);
                    m.Surface = deckSurface;
                    Face(m, V(x - w, height, a), V(x - w, height, b), V(x + w, height, b), V(x + w, height, a), deck, Vector3.up);
                    m.Surface = ComicSurface.Concrete;
                    for (int s = -1; s <= 1; s += 2)
                        Face(m, V(x + s * w, -.8f, a), V(x + s * w, -.8f, b), V(x + s * w, height, b), V(x + s * w, height, a), side, V(s, 0, 0));
                }
            m.Surface = ComicSurface.Concrete;
            Face(m, V(x - w, -.8f, zl), V(x + w, -.8f, zl), V(x + w, height, zl), V(x - w, height, zl), side, Vector3.forward);
            // Yellow-and-black lip stripe so the edge reads from far away.
            int n = Mathf.Max(2, Mathf.RoundToInt(width / .5f));
            for (int i = 0; i < n; i++)
            {
                float xa = x - w + width * i / n, xb = x - w + width * (i + 1) / n;
                m.Surface = ComicSurface.PaintedMetal;
                Face(m, V(xa, height + .005f, zl - .45f), V(xa, height + .005f, zl), V(xb, height + .005f, zl), V(xb, height + .005f, zl - .45f), i % 2 == 0 ? lip : R.ink, Vector3.up, false);
            }
        }

        /// <summary>Loads the shared comic people from the hazard kit (HZ_Ped_*).</summary>
        public static GameObject Person(string look)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PeoplePrefabs + "/HZ_Ped_" + look + ".prefab");
            if (go == null) throw new InvalidOperationException("Build the hazard kit first (HZ_Ped_" + look + ").");
            return go;
        }

        public static Obstacle BaseObstacle(string id)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(ComicCityBuilder.Prefabs + "/Gameplay/OBS_" + id + ".prefab");
            return go != null ? go.GetComponent<Obstacle>() : null;
        }

        public static District SaveDistrict(string path, Action<District> fill)
        {
            PrefabBuildUtility.EnsureFolder(DistrictData);
            var d = AssetDatabase.LoadAssetAtPath<District>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<District>(); AssetDatabase.CreateAsset(d, path); }
            fill(d);
            EditorUtility.SetDirty(d);
            return d;
        }

        public static string Inv(float f) => f.ToString("0.###", CultureInfo.InvariantCulture);
    }

    /// <summary>Builds both of art agent 2's districts in one Unity run.
    /// Batch: -executeMethod CityScooter.EditorTools.Districts.ParkRiverDistricts.BuildBatch</summary>
    public static class ParkRiverDistricts
    {
        [MenuItem("CityScooter/Districts/Build Kings Park and Mounts Bay Rd")]
        public static void Build() { KingsParkBuilder.Build(); MountsBayBuilder.Build(); }

        public static void BuildBatch()
        {
            try { Build(); AssetDatabase.SaveAssets(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Build both districts, then render their props atlases (run with graphics, i.e. without -nographics).</summary>
        public static void BuildAndAtlasBatch()
        {
            try { Build(); AssetDatabase.SaveAssets(); ParkRiverAtlas.Render(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }

    /// <summary>A gap / ramp authored inside a chunk.</summary>
    internal struct RampSpec { public float X, Z, Width, Rise, Height, Top, Launch, Hang; }
    internal struct GapSpec { public float X, Z, Length, Width; public string Word; public bool Water; }

    /// <summary>Everything one chunk is made of while it is built. Flat meshes are sheared to the slope at the end.</summary>
    internal sealed class ChunkCtx
    {
        public readonly string District, Id, PrefabFolder, MeshFolder;
        public readonly float L, Grade;
        public readonly ComicMesh RoadFlat = new ComicMesh(), FrontFlat = new ComicMesh(), Front = new ComicMesh(),
            PropsFlat = new ComicMesh(), Props = new ComicMesh(), DeepFlat = new ComicMesh(), Deep = new ComicMesh();
        public readonly List<Vector2> Slots = new List<Vector2>();
        public readonly List<RampSpec> Ramps = new List<RampSpec>();
        public readonly List<GapSpec> Gaps = new List<GapSpec>();
        public readonly System.Random Rnd;
        public int Seed;
        /// <summary>Road half width at the start / end and where it changes (like RoadChunk.endHalfWidth / widthChangeZ).</summary>
        public float HalfStart = ParkRiverKit.Half, HalfEnd = ParkRiverKit.Half;
        public Vector2 WidthZ = new Vector2(8f, 32f);
        public bool ChangesWidth => Mathf.Abs(HalfEnd - HalfStart) > .001f;
        public float HalfAt(float z)
        {
            if (!ChangesWidth) return HalfStart;
            float t = Mathf.InverseLerp(WidthZ.x, WidthZ.y, z);
            return Mathf.Lerp(HalfStart, HalfEnd, t * t * (3f - 2f * t));
        }
        /// <summary>Outer edge of the sidewalk (the rideable edge) at z.</summary>
        public float EdgeAt(float z) => HalfAt(z) + ParkRiverKit.Walk;

        public ChunkCtx(string district, string id, string prefabFolder, string meshFolder, float endHeight, float length = 40f)
        {
            District = district; Id = id; PrefabFolder = prefabFolder; MeshFolder = meshFolder; L = length; Grade = -endHeight / length;
            Seed = ComicKitGeometry.StableHash(district + id);
            Rnd = ParkRiverKit.Rng(Seed);
        }

        public string Name => "CHN_" + District + "_" + Id;
        public string Path => PrefabFolder + "/" + Name + ".prefab";
        public float Y(float z) => -Grade * z;
        /// <summary>A point at height y above the road surface at z (upright things).</summary>
        public Vector3 P(float x, float y, float z) => new Vector3(x, -Grade * z + y, z);
        public int NextSeed() => Seed = unchecked(Seed * 31 + 17);

        public void StandardSlots(params float[] rows)
        {
            if (rows.Length == 0) rows = new[] { 10f, 25f };
            foreach (float z in rows) foreach (float x in new[] { -2.6f, 0f, 2.6f }) Slots.Add(new Vector2(x, z));
        }

        bool Overlaps(GapSpec g, float z0, float z1) => g.Z < z1 - .001f && g.Z + g.Length > z0 + .001f;
        float GapHalf(GapSpec g) => g.Width > 0f ? g.Width * .5f : 50f;

        public bool InHole(float x, float z0, float z1) => Gaps.Any(g => Overlaps(g, z0, z1) && Mathf.Abs(x - g.X) < GapHalf(g));

        /// <summary>Parts of [a, b] across the road not inside a gap between z0 and z1.</summary>
        public List<Vector2> Free(float a, float b, float z0, float z1)
        {
            var list = new List<Vector2> { new Vector2(a, b) };
            foreach (GapSpec g in Gaps)
            {
                if (!Overlaps(g, z0, z1)) continue;
                float h0 = g.X - GapHalf(g), h1 = g.X + GapHalf(g);
                var next = new List<Vector2>();
                foreach (Vector2 s in list)
                {
                    if (h1 <= s.x || h0 >= s.y) { next.Add(s); continue; }
                    if (h0 > s.x) next.Add(new Vector2(s.x, h0));
                    if (h1 < s.y) next.Add(new Vector2(h1, s.y));
                }
                list = next;
            }
            return list.Where(s => s.y - s.x > .01f).ToList();
        }

        /// <summary>Adds everything another context built (one side's dressing), moved by offset.</summary>
        public void Absorb(ChunkCtx s, Vector3 offset)
        {
            void A(ComicMesh to, ComicMesh from) => to.Add(from, offset, Quaternion.identity, Vector3.one);
            A(RoadFlat, s.RoadFlat); A(FrontFlat, s.FrontFlat); A(Front, s.Front); A(PropsFlat, s.PropsFlat); A(Props, s.Props);
            A(DeepFlat, s.DeepFlat); A(Deep, s.Deep);
        }

        static ComicMesh Merge(ComicMesh up, ComicMesh flat, float grade)
        {
            flat.Shear(grade);
            var m = new ComicMesh();
            m.Add(up, Vector3.zero, Quaternion.identity, Vector3.one);
            m.Add(flat, Vector3.zero, Quaternion.identity, Vector3.one);
            return m;
        }

        /// <summary>Writes the chunk prefab (6 renderers), its Ramp / RoadGap children and spawn slots; returns its RoadChunk.</summary>
        public RoadChunk Save()
        {
            GameObject root = PrefabBuildUtility.OpenPrefab(Path);
            var sync = new ChildSync(root.transform);
            var chunk = PrefabBuildUtility.GetOrAdd<RoadChunk>(root);
            chunk.length = L; chunk.endHeight = -Grade * L; chunk.roadType = RoadType.Street; chunk.weight = 1f; chunk.minDifficulty = 0;
            chunk.halfWidth = HalfStart; chunk.endHalfWidth = ChangesWidth ? HalfEnd : 0f; chunk.widthChangeZ = WidthZ;
            chunk.sidewalkWidth = ParkRiverKit.Walk; chunk.sidewalkHeight = ParkRiverKit.Kerb; chunk.curbTransitionWidth = .3f;

            ComicMesh road = new ComicMesh(); RoadFlat.ClearCreaseInk(); road.Add(Merge(new ComicMesh(), RoadFlat, Grade), Vector3.zero, Quaternion.identity, Vector3.one);
            ParkRiverKit.Renderer(sync.Plain("RoadAndGround"), MeshFolder, Name + "_Road", road, false, false);
            ParkRiverKit.Renderer(sync.Plain("Frontage"), MeshFolder, Name + "_Frontage", Merge(Front, FrontFlat, Grade), true, true);
            ParkRiverKit.Renderer(sync.Plain("Roadside"), MeshFolder, Name + "_Props", Merge(Props, PropsFlat, Grade), true, true);
            ParkRiverKit.Renderer(sync.Plain("DeepScenery"), MeshFolder, Name + "_Deep", Merge(Deep, DeepFlat, Grade), false, false);

            for (int i = 0; i < Ramps.Count; i++)
            {
                RampSpec s = Ramps[i];
                GameObject go = sync.Plain("Ramp" + i);
                go.transform.localPosition = new Vector3(s.X, Y(s.Z), s.Z); go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
                var ramp = PrefabBuildUtility.GetOrAdd<Ramp>(go);
                ramp.width = s.Width; ramp.rise = s.Rise; ramp.height = s.Height; ramp.top = s.Top; ramp.launch = s.Launch; ramp.hangGravity = s.Hang; ramp.lanes = null;
            }
            for (int i = 0; i < Gaps.Count; i++)
            {
                GapSpec s = Gaps[i];
                GameObject go = sync.Plain("Gap" + i);
                go.transform.localPosition = new Vector3(s.X, Y(s.Z), s.Z); go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
                var gap = PrefabBuildUtility.GetOrAdd<RoadGap>(go);
                gap.length = s.Length; gap.width = s.Width; gap.word = s.Word; gap.water = s.Water;
            }
            GameObject slotsGo = sync.Plain("SpawnSlots");
            var slots = new ChildSync(slotsGo.transform);
            foreach (Vector2 s in Slots)
            {
                GameObject slot = slots.Plain("Row" + ParkRiverKit.Inv(s.y) + "_Lane" + ParkRiverKit.Inv(s.x));
                slot.transform.localPosition = new Vector3(s.x, Y(s.y), s.y);
                PrefabBuildUtility.GetOrAdd<SpawnSlot>(slot).kind = SpawnKind.Any;
            }
            slots.Finish();
            sync.Finish();
            PrefabBuildUtility.SyncComponent<StreetDressing>(root, false);

            int tris = PrefabBuildUtility.CountTriangles(root), renderers = root.GetComponentsInChildren<MeshRenderer>(true).Length;
            PrefabBuildUtility.SaveAndClose(root, Path);
            ParkRiverKit.Report.Add(Name + " " + tris + " tris, " + renderers + " renderers");
            if (tris > ParkRiverKit.TriangleBudget) throw new InvalidOperationException(Name + " exceeds the 25k triangle chunk budget: " + tris);
            if (renderers > ParkRiverKit.RendererBudget) throw new InvalidOperationException(Name + " has more than 8 renderers: " + renderers);
            return AssetDatabase.LoadAssetAtPath<GameObject>(Path).GetComponent<RoadChunk>();
        }
    }
}
