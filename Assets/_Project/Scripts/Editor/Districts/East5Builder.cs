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
    /// <summary>
    /// East line, art agent 5 (v0.5): the last two districts of the Midland line, WOODBRIDGE (route order 330) and
    /// MIDLAND (order 340, the end of the line; after it the route goes back to UWA). The ride heads east: the railway
    /// (fence, two tracks, overhead-wire gantries, trains) is always on the right (+X, the south side), the Swan River
    /// and the town on the left (-X), the Darling Scarp ahead and all round in the far scenery.
    /// WOODBRIDGE (East5Woodbridge.cs): railway workers' weatherboard cottages on a gum-lined road, Woodbridge station
    /// with its footbridge and green mural ramp, Woodbridge House (1885, National Trust) on its riverside lawns, the
    /// shared path along the Swan and the Blackadder Creek boardwalk (timber kicker + SPLASH gap).
    /// MIDLAND (East5Midland.cs): Great Eastern Hwy shopfronts, Midland Town Hall (round corner tower, grey dome, square
    /// clock tower), Midland Centrepoint, the new METRONET Midland station (raised concourse wrapped in rust-red fins,
    /// red shared path, bus interchange), the terminus (buffer stops, multi-storey car park) and the Midland Railway
    /// Workshops (red-brick sawtooth blocks, chimney, elevated water tank; loading ramp over the traverser pit).
    /// This file: entry points, the facade frame (Fr), the railway kit, trees, pixel letters, animals, far scenery parts.
    /// Uses the CBD / Quay comic kit (CbdQuayKit, CqChunkDraft) read-only: ComicPalette + ComicInk only, vertex colours.
    ///   Menu: CityScooter/Districts/Build East 5
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.East5Builder.BuildBatch
    ///   Batch with the props atlases (needs graphics): ...East5Builder.BuildAndRenderAtlasBatch
    /// Sources: art-src/districts/woodbridge|midland/SOURCES.txt.
    /// </summary>
    public static partial class East5Builder
    {
        public const string WoodId = "woodbridge", MidId = "midland";
        public const string WoodPrefabs = ComicCityBuilder.Prefabs + "/Districts/Woodbridge";
        public const string WoodMeshes = ComicCityBuilder.Root + "/Districts/Woodbridge";
        public const string MidPrefabs = ComicCityBuilder.Prefabs + "/Districts/Midland";
        public const string MidMeshes = ComicCityBuilder.Root + "/Districts/Midland";
        public const string WoodDistrictPath = CbdBuilder.DataFolder + "/District_Woodbridge.asset";
        public const string MidDistrictPath = CbdBuilder.DataFolder + "/District_Midland.asset";
        public static readonly string[] WoodChunkIds = { "Cottages", "Station", "House", "Riverside", "Creek" };
        public static readonly string[] MidChunkIds = { "Highway", "TownHall", "Centrepoint", "Station", "Terminus", "Workshops" };
        public static string WoodChunkPath(string variant) => WoodPrefabs + "/CHN_Woodbridge_" + variant + ".prefab";
        public static string MidChunkPath(string variant) => MidPrefabs + "/CHN_Midland_" + variant + ".prefab";
        static string WoodPf(string name) => WoodPrefabs + "/" + name + ".prefab";
        static string MidPf(string name) => MidPrefabs + "/" + name + ".prefab";

        /// <summary>The Swan coastal plain is nearly flat: -1.4 m (Woodbridge) and -1.2 m (Midland) per 40 m chunk.</summary>
        public const float WoodDrop = -1.4f, MidDrop = -1.2f;
        // The creek jump (Woodbridge) and the traverser pit jump (Midland): Ramp + road-wide RoadGap. Numbers for gameplay.
        public const float CreekRampStart = 9f, CreekRise = 6f, CreekHeight = 1.4f, CreekTop = 3f, CreekGap = 6f;
        public const float PitRampStart = 9f, PitRise = 6f, PitHeight = 1.4f, PitTop = 3f, PitGap = 6f;

        // The railway corridor beside the road (right side): fence, ballast, track centres, the far edge of the ballast.
        const float FenceX = K.Edge + 1f, BedX0 = K.Edge + 1.6f, T1 = 14f, T2 = 19f, T3 = 27.2f, BedX1 = 22.4f, BedY = K.Kerb + .1f;
        const float River = -1.5f;

        static Color Ink => K.R.ink;
        // Palette (sRGB; what each colour was taken from is in the SOURCES.txt files).
        static readonly Color Brick = K.Hex("B5573A"), BrickDark = K.Hex("94452D"), BrickLight = K.Hex("C96E4C"), RoseBrick = K.Hex("BF6347");
        static readonly Color Stone = K.Hex("E8DCC0"), StoneShade = K.Hex("CDBE9E"), Ochre = K.Hex("D9A95C"), VerandahBrown = K.Hex("7C5234"), LaceBrown = K.Hex("9A6A40");
        static readonly Color IronRoof = K.Hex("93A6B6"), IronRoofDark = K.Hex("73889B"), RedOxide = K.Hex("A04C3A"), GreenRoof = K.Hex("5F7D64"), Zinc = K.Hex("9AA3A8");
        static readonly Color White = K.Hex("F5F3EC"), Cream = K.Hex("EFE6CF"), Sage = K.Hex("C6D4BF"), PaleBlue = K.Hex("D3DDE6"), Butter = K.Hex("EBD49E"), PalePink = K.Hex("EBCFC2");
        static readonly Color Glass = K.Hex("2F6F86"), GlassDark = K.Hex("223A48"), GlassLight = K.Hex("7FB4C4");
        static readonly Color Ballast = K.Hex("8F887D"), BallastDark = K.Hex("777067"), RailSteel = K.Hex("4A4F56"), Sleeper = K.Hex("BDB8AD");
        static readonly Color Steel = K.Hex("A3ABB3"), SteelDark = K.Hex("626A72"), Charcoal = K.Hex("2F3339"), Concrete = K.Hex("CFCBC1"), ConcreteDark = K.Hex("AFAAA0");
        static readonly Color Rust = K.Hex("A9452E"), RustLight = K.Hex("CC6444"), RustDark = K.Hex("7E3222");
        static readonly Color PathRed = K.Hex("A94A3E"), Paving = K.Hex("D6D0C4"), PavingStripe = K.Hex("B4AEA2"), KerbCap = K.Hex("ECE7DB"), BrickPaving = K.Hex("B8664A");
        static readonly Color Bark = K.Hex("E6DFCF"), BarkShade = K.Hex("B5AA98"), BarkRed = K.Hex("A08C76"), Leaf = K.Hex("6E8F52"), LeafDark = K.Hex("54713E"), LeafLight = K.Hex("8FAA66");
        static readonly Color Lawn = K.Hex("6DA544"), LawnLight = K.Hex("82B654"), DryGrass = K.Hex("B9B26C"), Mulch = K.Hex("8A6A4C"), Hedge = K.Hex("3F6B34");
        static readonly Color RiverGreen = K.Hex("5F8F7C"), RiverLight = K.Hex("8DB7A2"), Mud = K.Hex("9D8B66"), Reed = K.Hex("7C9450");
        static readonly Color Jacaranda = K.Hex("8F6DC6"), JacarandaLight = K.Hex("AC8EDD"), Timber = K.Hex("A8774E"), TimberDark = K.Hex("86603F"), Deck = K.Hex("9A6B45"), DeckLight = K.Hex("B88757");
        static readonly Color TrainWhite = K.Hex("E8ECEE"), TrainGreen = K.Hex("1E8B4D"), TrainRoof = K.Hex("A8B0B7"), Yellow = K.Hex("F2C230"), SignGreen = K.Hex("1F6B4A");
        static readonly Color MuralGreen = K.Hex("2E7B55"), MuralLight = K.Hex("6FB46A"), MuralTeal = K.Hex("2FA79A"), MuralDark = K.Hex("1E5540");

        [MenuItem("CityScooter/Districts/Build East 5")]
        public static void Build() => Build(false);

        public static void BuildBatch()
        {
            try { Build(false); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Builds both districts and renders art-src/districts/woodbridge|midland/props-atlas.png (run with graphics).</summary>
        public static void BuildAndRenderAtlasBatch()
        {
            try { Build(true); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Build(bool atlas)
        {
            // The theme is loaded again for each district: rendering an atlas opens a fresh preview scene, which unloads it.
            BuildWoodbridge(LoadTheme(), atlas);
            BuildMidland(LoadTheme(), atlas);
            AssetDatabase.SaveAssets();
        }

        static TrackTheme LoadTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme == null || theme.hazards == null || theme.hazards.Length == 0) throw new InvalidOperationException("No comic theme (with hazards) at " + ComicCityBuilder.ThemePath);
            return theme;
        }

        static HazardPattern[] ThemeHazards(TrackTheme theme, params string[] names) =>
            theme.hazards == null ? Array.Empty<HazardPattern>() : theme.hazards.Where(h => h != null && names.Contains(h.name)).Distinct().ToArray();

        static RoadChunk Chunk(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<RoadChunk>();
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static float F(System.Random r, float a, float b) => Mathf.Lerp(a, b, (float)r.NextDouble());
        static ComicMesh Make(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }
        /// <summary>For the props atlas (its camera looks from +X +Z): the mesh turned half round, so a building's front
        /// (-Z) and the end the rider sees face the camera.</summary>
        static ComicMesh Shown(ComicMesh source) { var m = new ComicMesh(); m.Add(source, Vector3.zero, Quaternion.Euler(0, 180f, 0), Vector3.one); return m; }

        // ================================================================================== the facade frame

        /// <summary>A building drawn in its own frame: X across the front (left to right as you face it), Y up, Z the
        /// depth behind the facade plane (negative = towards the viewer). <see cref="Put"/> turns it to the road.</summary>
        sealed class Fr
        {
            public readonly ComicMesh M = new ComicMesh();
            public ComicSurface S { set { M.Surface = value; } }
            public void Box(float x0, float x1, float y0, float y1, float z0, float z1, Color c) => K.MinMaxBox(M, V(x0, y0, z0), V(x1, y1, z1), c);
            /// <summary>A quad on a plane parallel to the facade (facing the viewer).</summary>
            public void Q(float x0, float x1, float y0, float y1, float z, Color c, bool ink = true) =>
                K.Face(M, V(x0, y0, z), V(x1, y0, z), V(x1, y1, z), V(x0, y1, z), c, Vector3.back, ink);
            /// <summary>A quad on an end wall (the plane x = const), facing left (-X) or right (+X).</summary>
            public void EndQ(float x, float z0, float z1, float y0, float y1, Color c, bool left, bool ink = true) =>
                K.Face(M, V(x, y0, z0), V(x, y0, z1), V(x, y1, z1), V(x, y1, z0), c, left ? Vector3.left : Vector3.right, ink);
            public void Top(float x0, float x1, float z0, float z1, float y, Color c, bool ink = true) =>
                K.Face(M, V(x0, y, z0), V(x0, y, z1), V(x1, y, z1), V(x1, y, z0), c, Vector3.up, ink);
            public void Cyl(float x, float y0, float y1, float z, float r, Color c, int sides = 8, float topScale = 1f) =>
                M.Cylinder(V(x, (y0 + y1) * .5f, z), r, y1 - y0, c, sides, topScale);

            /// <summary>Gable roof with the ridge along X (parallel to the facade), at depth zr (default: the middle).</summary>
            public void RoofX(float x0, float x1, float y0, float y1, float z0, float z1, Color c, float zr = float.NaN)
            {
                if (float.IsNaN(zr)) zr = (z0 + z1) * .5f;
                Vector3 f0 = V(x0, y0, z0), f1 = V(x1, y0, z0), r0 = V(x0, y1, zr), r1 = V(x1, y1, zr), b0 = V(x0, y0, z1), b1 = V(x1, y0, z1);
                K.Face(M, f0, f1, r1, r0, c, V(0, 1, -1));
                K.Face(M, r0, r1, b1, b0, c, V(0, 1, 1));
                K.TriFacing(M, f0, r0, b0, c, Vector3.left);
                K.TriFacing(M, f1, r1, b1, c, Vector3.right);
            }

            /// <summary>Gable roof with the ridge along Z: the gable (colour <paramref name="gable"/>) faces the viewer.</summary>
            public void RoofZ(float x0, float x1, float y0, float y1, float z0, float z1, Color c, Color? gable = null)
            {
                float xm = (x0 + x1) * .5f;
                Vector3 l0 = V(x0, y0, z0), l1 = V(x0, y0, z1), a0 = V(xm, y1, z0), a1 = V(xm, y1, z1), r0 = V(x1, y0, z0), r1 = V(x1, y0, z1);
                K.Face(M, l0, l1, a1, a0, c, V(-1, 1, 0));
                K.Face(M, r0, r1, a1, a0, c, V(1, 1, 0));
                K.TriFacing(M, l0, r0, a0, gable ?? c, Vector3.back);
                K.TriFacing(M, l1, r1, a1, gable ?? c, Vector3.forward);
            }

            /// <summary>Hipped roof over a rectangle: the ridge runs along the longer side.</summary>
            public void Hip(float x0, float x1, float z0, float z1, float y0, float y1, Color c)
            {
                float w = x1 - x0, dpt = z1 - z0;
                if (w >= dpt)
                {
                    float h = dpt * .5f, zm = (z0 + z1) * .5f;
                    Vector3 ra = V(x0 + h, y1, zm), rb = V(x1 - h, y1, zm);
                    K.Face(M, V(x0, y0, z0), V(x1, y0, z0), rb, ra, c, V(0, 1, -1));
                    K.Face(M, V(x0, y0, z1), V(x1, y0, z1), rb, ra, c, V(0, 1, 1));
                    K.TriFacing(M, V(x0, y0, z0), V(x0, y0, z1), ra, c, V(-1, 1, 0));
                    K.TriFacing(M, V(x1, y0, z0), V(x1, y0, z1), rb, c, V(1, 1, 0));
                }
                else
                {
                    float h = w * .5f, xm = (x0 + x1) * .5f;
                    Vector3 ra = V(xm, y1, z0 + h), rb = V(xm, y1, z1 - h);
                    K.Face(M, V(x0, y0, z0), V(x0, y0, z1), rb, ra, c, V(-1, 1, 0));
                    K.Face(M, V(x1, y0, z0), V(x1, y0, z1), rb, ra, c, V(1, 1, 0));
                    K.TriFacing(M, V(x0, y0, z0), V(x1, y0, z0), ra, c, V(0, 1, -1));
                    K.TriFacing(M, V(x0, y0, z1), V(x1, y0, z1), rb, c, V(0, 1, 1));
                }
            }

            /// <summary>A lean-to (verandah) roof: high at the wall (zWall, yHigh), low at the front (zFront, yLow), thick.</summary>
            public void LeanTo(float x0, float x1, float zFront, float zWall, float yLow, float yHigh, float thick, Color c, Color under)
            {
                Vector3 a = V(x0, yLow, zFront), b = V(x1, yLow, zFront), cc = V(x1, yHigh, zWall), dd = V(x0, yHigh, zWall), t = V(0, thick, 0);
                K.Face(M, a + t, b + t, cc + t, dd + t, c, V(0, 1, -.3f));
                K.Face(M, a, b, cc, dd, under, V(0, -1, .3f));
                K.Face(M, a, b, b + t, a + t, c, Vector3.back);
                K.Face(M, a, dd, dd + t, a + t, c, Vector3.left);
                K.Face(M, b, cc, cc + t, b + t, c, Vector3.right);
            }

            /// <summary>A framed window on the wall plane z: ink frame, glass set in, mullions, a meeting rail, a
            /// protruding sill and (lintel) a protruding head.</summary>
            public void Win(float cx, float y0, float y1, float w, float z, Color trim, bool sill = true, bool lintel = false, int panes = 2, bool rail = true, Color? glass = null)
            {
                float a = cx - w * .5f, b = cx + w * .5f;
                S = ComicSurface.PaintedMetal;
                Q(a, b, y0, y1, z - .02f, Ink);
                S = ComicSurface.Glass;
                Q(a + .09f, b - .09f, y0 + .09f, y1 - .09f, z - .035f, glass ?? Glass, false);
                S = ComicSurface.PaintedMetal;
                for (int i = 1; i < panes; i++) { float x = Mathf.Lerp(a, b, (float)i / panes); Q(x - .035f, x + .035f, y0 + .09f, y1 - .09f, z - .05f, Ink, false); }
                if (rail) { float ym = (y0 + y1) * .5f; Q(a + .09f, b - .09f, ym - .035f, ym + .035f, z - .05f, Ink, false); }
                S = ComicSurface.Concrete;
                if (sill) Box(a - .12f, b + .12f, y0 - .16f, y0, z - .16f, z, trim);
                if (lintel) Box(a - .12f, b + .12f, y1, y1 + .2f, z - .1f, z, trim);
            }

            /// <summary>A round-headed window: the rectangle up to ySpring and a faceted arch above it, with a keystone.</summary>
            public void ArchWin(float cx, float y0, float ySpring, float w, float z, Color trim, bool sill = true, int panes = 2, Color? glass = null)
            {
                Win(cx, y0, ySpring, w, z, trim, sill, false, panes, false, glass);
                float r = w * .5f;
                Vector3 c = V(cx, ySpring, z - .02f), g = V(cx, ySpring, z - .035f);
                const int n = 4;
                for (int i = 0; i < n; i++)
                {
                    float a0 = Mathf.PI * i / n, a1 = Mathf.PI * (i + 1) / n;
                    Vector3 p0 = V(Mathf.Cos(a0), Mathf.Sin(a0), 0), p1 = V(Mathf.Cos(a1), Mathf.Sin(a1), 0);
                    S = ComicSurface.PaintedMetal;
                    K.TriFacing(M, c, c + p0 * r, c + p1 * r, Ink, Vector3.back);
                    S = ComicSurface.Glass;
                    K.TriFacing(M, g, g + p0 * (r - .09f), g + p1 * (r - .09f), glass ?? Glass, Vector3.back);
                }
                S = ComicSurface.Concrete;
                Box(cx - .14f, cx + .14f, ySpring + r - .08f, ySpring + r + .26f, z - .12f, z, trim);
            }

            /// <summary>A panelled door: ink frame, coloured leaf, a glass fanlight above.</summary>
            public void Door(float cx, float y0, float y1, float w, float z, Color leaf, bool fanlight = true)
            {
                float a = cx - w * .5f, b = cx + w * .5f, top = fanlight ? y1 - .45f : y1 - .08f;
                S = ComicSurface.PaintedMetal;
                Q(a, b, y0, y1, z - .02f, Ink);
                S = ComicSurface.Wood;
                Q(a + .08f, b - .08f, y0 + .04f, top, z - .035f, leaf, false);
                Q(a + .2f, b - .2f, y0 + .3f, top - .25f, z - .045f, Color.Lerp(leaf, Ink, .22f), false);
                if (!fanlight) return;
                S = ComicSurface.Glass;
                Q(a + .08f, b - .08f, top + .07f, y1 - .08f, z - .035f, GlassLight, false);
            }

            /// <summary>A shopfront between x0 and x1: ink frame, glass above a kick plate, ink mullions and a transom.</summary>
            public void Shopfront(float x0, float x1, float y0, float y1, float z, Color? glass = null)
            {
                S = ComicSurface.PaintedMetal;
                Q(x0, x1, y0, y1, z - .02f, Ink);
                S = ComicSurface.Glass;
                Q(x0 + .12f, x1 - .12f, y0 + .45f, y1 - .12f, z - .04f, glass ?? Glass, false);
                S = ComicSurface.PaintedMetal;
                int n = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 1.5f));
                for (int i = 1; i < n; i++) { float x = Mathf.Lerp(x0, x1, (float)i / n); Box(x - .05f, x + .05f, y0 + .4f, y1 - .1f, z - .09f, z - .02f, Ink); }
                Q(x0 + .1f, x1 - .1f, y1 - .78f, y1 - .68f, z - .06f, Ink, false);
            }
        }

        /// <summary>Adds a frame building to a mesh: the frame's origin at <paramref name="p"/>, turned by yaw
        /// (-90 = facing +X, the road, for a building on the left; +90 = facing -X for one on the right).</summary>
        static void Put(ComicMesh target, Fr f, Vector3 p, float yaw, float scale = 1f) => target.Add(f.M, p, Quaternion.Euler(0, yaw, 0), Vector3.one * scale);

        /// <summary>A building along the left side of the road: front on the plane x = -(Edge + setback), starting at z0
        /// (the frame's X runs along +Z, so its left end faces the rider).</summary>
        static void PutLeft(CqChunkDraft d, ComicMesh target, Fr f, float z0, float width, float setback, float depth, float wallHeight)
        {
            Put(target, f, V(-(K.Edge + setback), d.Base(z0 + width * .5f), z0), -90f);
            if (wallHeight > 0f) d.Walls.Add(new Bounds(V(-(K.Edge + setback + depth * .5f), d.Y(z0 + width * .5f) + wallHeight * .5f - 1f, z0 + width * .5f), V(depth, wallHeight + 2f, width)));
        }

        // ================================================================================== ground, railway

        /// <summary>CbdQuayKit.StreetSurface (copied: the kit is read-only) with the road in the asphalt surface whatever
        /// its colour, for the red shared path: road, kerbs, footpaths with joints, closed slab ends; no ink on slabs.</summary>
        static void AsphaltStreetSurface(ComicMesh m, float length, Color road, Color pavement, Color kerbCap, float skipFrom = -1f, float skipTo = -1f)
        {
            float h = K.Kerb, sw = K.Side, w = K.Half;
            Color joint = Color.Lerp(Ink, pavement, .5f);
            var cuts = new List<float>();
            for (float z = 0; z < length - .01f; z += 4f) cuts.Add(z);
            cuts.Add(length);
            if (skipFrom >= 0f) { cuts.Add(skipFrom); cuts.Add(skipTo); }
            cuts = cuts.Distinct().OrderBy(z => z).ToList();
            for (int ci = 0; ci + 1 < cuts.Count; ci++)
            {
                float z0 = cuts[ci], z1 = cuts[ci + 1];
                if (z1 - z0 < .01f || skipFrom >= 0f && z0 >= skipFrom - .01f && z1 <= skipTo + .01f) continue;
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(-w, 0, z0), V(-w, 0, z1), V(w, 0, z1), V(w, 0, z0), road, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    float i0 = s * w, o0 = s * (w + sw);
                    Vector3 inward = V(-s, 0, 0), outward = V(s, 0, 0);
                    m.Surface = ComicSurface.Paving;
                    K.Face(m, V(i0, h, z0), V(i0, h, z1), V(o0, h, z1), V(o0, h, z0), pavement, Vector3.up, false);
                    K.Face(m, V(i0, -.05f, z0), V(i0, -.05f, z1), V(i0, h, z1), V(i0, h, z0), pavement, inward, false);
                    K.Face(m, V(o0, -2f, z0), V(o0, -2f, z1), V(o0, h, z1), V(o0, h, z0), pavement, outward, false);
                    m.Surface = ComicSurface.Plain;
                    float c0 = s * (w + .24f), top = h + .04f;
                    K.Face(m, V(i0, top, z0), V(i0, top, z1), V(c0, top, z1), V(c0, top, z0), kerbCap, Vector3.up, false);
                    K.Face(m, V(i0, h, z0), V(i0, h, z1), V(i0, top, z1), V(i0, top, z0), kerbCap, inward, false);
                    K.Face(m, V(c0, h, z0), V(c0, h, z1), V(c0, top, z1), V(c0, top, z0), kerbCap, outward, false);
                    for (int k = 1; k < 4; k++) K.Flat(m, s * (w + k) - .0125f, s * (w + k) + .0125f, s * (w + k) - .0125f, s * (w + k) + .0125f, z0, z1, h + .025f, joint);
                }
                for (float z = Mathf.Ceil(z0 / 2f) * 2f; z < z1 - .01f; z += 2f)
                    for (int s = -1; s <= 1; s += 2) K.Flat(m, s * w, s * (w + sw), s * w, s * (w + sw), z - .0125f, z + .0125f, h + .025f, joint);
            }
            foreach (float ze in new[] { 0f, length })
            {
                Vector3 normal = ze <= 0f ? Vector3.back : Vector3.forward;
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(-w, -.8f, ze), V(w, -.8f, ze), V(w, 0, ze), V(-w, 0, ze), road, normal, false);
                m.Surface = ComicSurface.Paving;
                for (int s = -1; s <= 1; s += 2) K.Face(m, V(s * w, -2f, ze), V(s * (w + sw), -2f, ze), V(s * (w + sw), h, ze), V(s * w, h, ze), pavement, normal, false);
            }
        }

        static void Ground(CqChunkDraft d, int s, float z0, float z1, float from, float to, float y, Color c, ComicSurface surface)
        {
            ComicMesh m = d.Road;
            m.Surface = surface;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.Face(m, V(s * (K.Edge + from), y, z), V(s * (K.Edge + from), y, e), V(s * (K.Edge + to), y, e), V(s * (K.Edge + to), y, z), c, Vector3.up, false);
            }
        }

        static void Stripe(ComicMesh m, float x0, float x1, float z0, float z1, float y, Color c)
        {
            m.Surface = ComicSurface.Plain;
            K.Face(m, V(x0, y, z0), V(x0, y, z1), V(x1, y, z1), V(x1, y, z0), c, Vector3.up, false);
        }

        /// <summary>The railway beside the road (right side), in the road mesh (sheared with the street, no outline):
        /// ballast, darker track beds, steel rails, concrete sleepers, the black palisade fence, the land beyond.</summary>
        static void RailBed(CqChunkDraft d, float z0, float z1, float[] tracks, float bedEnd, bool fence = true, Color? beyond = null, float skipFrom = -1f, float skipTo = -1f)
        {
            ComicMesh m = d.Road;
            bool Skip(float a, float b) => skipFrom >= 0f && b > skipFrom + .01f && a < skipTo - .01f;
            var cuts = new List<float>();
            for (float z = z0; z < z1 - .01f; z += 4f) cuts.Add(z);
            cuts.Add(z1);
            if (skipFrom >= 0f) { cuts.Add(skipFrom); cuts.Add(skipTo); }
            cuts = cuts.Where(z => z >= z0 - .001f && z <= z1 + .001f).Distinct().OrderBy(z => z).ToList();
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float a = cuts[i], b = cuts[i + 1];
                if (b - a < .01f || Skip(a, b)) continue;
                m.Surface = ComicSurface.Concrete;
                K.Face(m, V(K.Edge, K.Kerb - .02f, a), V(K.Edge, K.Kerb - .02f, b), V(BedX0, K.Kerb - .02f, b), V(BedX0, K.Kerb - .02f, a), Color.Lerp(Ballast, DryGrass, .45f), Vector3.up, false);
                K.Face(m, V(BedX0, K.Kerb - .02f, a), V(BedX0, K.Kerb - .02f, b), V(BedX0, BedY, b), V(BedX0, BedY, a), BallastDark, Vector3.left, false);
                K.Face(m, V(BedX0, BedY, a), V(BedX0, BedY, b), V(bedEnd, BedY, b), V(bedEnd, BedY, a), Ballast, Vector3.up, false);
                m.Surface = ComicSurface.Plain;
                foreach (float t in tracks)
                    K.Face(m, V(t - 1.3f, BedY + .012f, a), V(t - 1.3f, BedY + .012f, b), V(t + 1.3f, BedY + .012f, b), V(t + 1.3f, BedY + .012f, a), BallastDark, Vector3.up, false);
                if (beyond.HasValue)
                {
                    m.Surface = ComicSurface.Foliage;
                    K.Face(m, V(bedEnd, BedY - .08f, a), V(bedEnd, BedY - .08f, b), V(K.Edge + 60f, BedY - .08f, b), V(K.Edge + 60f, BedY - .08f, a), beyond.Value, Vector3.up, false);
                }
            }
            m.Surface = ComicSurface.Plain;
            for (float z = z0 + .5f; z < z1 - .3f; z += 1.3f)
            {
                if (Skip(z, z + .26f)) continue;
                foreach (float t in tracks)
                    K.Face(m, V(t - 1.15f, BedY + .025f, z), V(t - 1.15f, BedY + .025f, z + .26f), V(t + 1.15f, BedY + .025f, z + .26f), V(t + 1.15f, BedY + .025f, z), Sleeper, Vector3.up, false);
            }
            m.Surface = ComicSurface.Metal;
            for (float z = z0; z < z1 - .01f; z += 8f)
            {
                float e = Mathf.Min(z1, z + 8f);
                foreach (float t in tracks)
                    foreach (int s in new[] { -1, 1 })
                        K.MinMaxBox(m, V(t + s * .72f - .05f, BedY + .02f, z), V(t + s * .72f + .05f, BedY + .17f, e), RailSteel);
            }
            if (fence) Palisade(d.Road, FenceX, z0, z1, 1.8f);
        }

        /// <summary>Black steel palisade fence along z at x (in flat road space; the road side is -X).</summary>
        static void Palisade(ComicMesh m, float x, float z0, float z1, float h, float yBase = K.Kerb)
        {
            Color c = Charcoal;
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = z0; z < z1 - .01f; z += 8f)
            {
                float e = Mathf.Min(z1, z + 8f);
                foreach (float y in new[] { .25f, h - .2f }) K.MinMaxBox(m, V(x - .03f, yBase + y - .04f, z), V(x + .03f, yBase + y + .04f, e), c);
            }
            for (float z = z0 + .1f; z < z1; z += 4f) K.MinMaxBox(m, V(x - .06f, yBase - .3f, z - .06f), V(x + .06f, yBase + h + .1f, z + .06f), c);
            for (float z = z0 + .43f; z < z1 - .05f; z += .66f)
            {
                K.Face(m, V(x - .04f, yBase + .05f, z - .03f), V(x - .04f, yBase + .05f, z + .03f), V(x - .04f, yBase + h, z + .03f), V(x - .04f, yBase + h, z - .03f), c, Vector3.left, false);
                K.Face(m, V(x - .04f, yBase + .05f, z - .03f), V(x + .04f, yBase + .05f, z - .03f), V(x + .04f, yBase + h, z - .03f), V(x - .04f, yBase + h, z - .03f), c, Vector3.back, false);
            }
        }

        /// <summary>Overhead wires above each track (contact wire and catenary), in the road mesh.</summary>
        static void Wires(CqChunkDraft d, float z0, float z1, params float[] tracks)
        {
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Metal;
            for (float z = z0; z < z1 - .01f; z += 8f)
            {
                float e = Mathf.Min(z1, z + 8f);
                foreach (float t in tracks)
                {
                    K.MinMaxBox(m, V(t - .025f, BedY + 5.3f, z), V(t + .025f, BedY + 5.35f, e), Charcoal);
                    K.MinMaxBox(m, V(t - .025f, BedY + 6.35f, z), V(t + .025f, BedY + 6.4f, e), Charcoal);
                }
            }
        }

        /// <summary>An overhead-wire portal (flat road space): two steel masts on concrete footings, a lattice beam
        /// across the tracks, a drop arm with an insulator over each track.</summary>
        static void Gantry(ComicMesh m, float z, float xa, float xb, params float[] tracks)
        {
            foreach (float x in new[] { xa, xb })
            {
                m.Surface = ComicSurface.Concrete;
                m.Box(V(x, BedY + .2f, z), V(.7f, .5f, .7f), ConcreteDark);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(x, BedY + 4.1f, z), V(.26f, 7.4f, .3f), Steel);
            }
            float xm = (xa + xb) * .5f, w = xb - xa;
            m.Box(V(xm, BedY + 7.7f, z), V(w + .5f, .14f, .22f), Steel);
            m.Box(V(xm, BedY + 6.95f, z), V(w + .5f, .14f, .22f), Steel);
            int n = Mathf.Max(2, Mathf.RoundToInt(w / 1.6f));
            for (int i = 0; i < n; i++)
            {
                float x0 = xa + w * i / n, x1 = xa + w * (i + 1) / n;
                m.Beam(V(x0, BedY + (i % 2 == 0 ? 6.95f : 7.7f), z), V(x1, BedY + (i % 2 == 0 ? 7.7f : 6.95f), z), .08f, Steel);
            }
            foreach (float t in tracks)
            {
                m.Box(V(t + .5f, BedY + 6.3f, z), V(.1f, 1.3f, .1f), SteelDark);
                m.Beam(V(t + .5f, BedY + 5.75f, z), V(t - .1f, BedY + 5.4f, z), .07f, SteelDark);
                m.Surface = ComicSurface.Plastic;
                m.Box(V(t + .5f, BedY + 6.5f, z), V(.2f, .3f, .2f), Cream);
                m.Surface = ComicSurface.PaintedMetal;
            }
        }

        /// <summary>A single overhead-wire mast with a cantilever arm over one track (flat road space).</summary>
        static void Mast(ComicMesh m, float z, float x, float track)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(V(x, BedY + .2f, z), V(.7f, .5f, .7f), ConcreteDark);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(x, BedY + 3.8f, z), V(.26f, 6.8f, .3f), Steel);
            m.Beam(V(x, BedY + 6.6f, z), V(track, BedY + 6.4f, z), .09f, Steel);
            m.Beam(V(x, BedY + 5f, z), V(track, BedY + 5.4f, z), .08f, SteelDark);
        }

        /// <summary>A Transperth electric train on the track at x (flat road space, cars along +Z from z0): white-silver
        /// cars with a dark window band, a green stripe and green doors, roof equipment, an ink underframe; a green
        /// cab end with a dark windscreen and lights at each end that has one; a pantograph on the second car.</summary>
        static void Train(ComicMesh m, float x, float z0, int cars, bool cabAtStart, bool cabAtEnd)
        {
            const float len = 19.4f, gap = .6f, floor = BedY + .95f, top = BedY + 3.95f;
            for (int i = 0; i < cars; i++)
            {
                float a = z0 + i * (len + gap), b = a + len, zc = (a + b) * .5f;
                m.Surface = ComicSurface.PaintedMetal;
                K.MinMaxBox(m, V(x - 1.2f, BedY + .2f, a + .8f), V(x + 1.2f, floor, b - .8f), Ink);
                m.Surface = ComicSurface.Metal;
                K.MinMaxBox(m, V(x - 1.45f, floor, a), V(x + 1.45f, top, b), TrainWhite);
                m.Surface = ComicSurface.Glass;
                K.MinMaxBox(m, V(x - 1.47f, floor + 1.45f, a + .9f), V(x + 1.47f, floor + 2.35f, b - .9f), GlassDark);
                m.Surface = ComicSurface.PaintedMetal;
                K.MinMaxBox(m, V(x - 1.48f, floor + 1.02f, a + .1f), V(x + 1.48f, floor + 1.26f, b - .1f), TrainGreen);
                foreach (float dz in new[] { -5.2f, 5.2f })
                {
                    m.Surface = ComicSurface.PaintedMetal;
                    K.MinMaxBox(m, V(x - 1.5f, floor + .08f, zc + dz - .75f), V(x + 1.5f, floor + 2.5f, zc + dz + .75f), TrainGreen);
                    m.Surface = ComicSurface.Glass;
                    K.MinMaxBox(m, V(x - 1.52f, floor + 1.4f, zc + dz - .55f), V(x + 1.52f, floor + 2.3f, zc + dz + .55f), GlassDark);
                    m.Surface = ComicSurface.PaintedMetal;
                    K.MinMaxBox(m, V(x - 1.53f, floor + .1f, zc + dz - .03f), V(x + 1.53f, floor + 2.48f, zc + dz + .03f), Ink);
                }
                m.Surface = ComicSurface.Metal;
                K.MinMaxBox(m, V(x - 1.15f, top, a + .4f), V(x + 1.15f, top + .22f, b - .4f), TrainRoof);
                K.MinMaxBox(m, V(x - .8f, top + .22f, zc - 6.5f), V(x + .8f, top + .5f, zc - 3.5f), SteelDark);
                if (i == 1 || cars == 1)
                {
                    m.Surface = ComicSurface.PaintedMetal;
                    Vector3 p = V(x, top + .22f, zc + 4f);
                    m.Beam(p + V(0, 0, -.9f), p + V(0, .65f, 0), .07f, Charcoal);
                    m.Beam(p + V(0, .65f, 0), p + V(0, 1.2f, -.7f), .07f, Charcoal);
                    m.Box(p + V(0, 1.2f, -.7f), V(1.5f, .06f, .12f), Charcoal);
                }
                foreach ((bool cab, float z, float dir) in new[] { (cabAtStart && i == 0, a, -1f), (cabAtEnd && i == cars - 1, b, 1f) })
                {
                    if (!cab) continue;
                    m.Surface = ComicSurface.PaintedMetal;
                    K.MinMaxBox(m, V(x - 1.4f, floor, z), V(x + 1.4f, top - .1f, z + dir * .22f), TrainGreen);
                    m.Surface = ComicSurface.Glass;
                    K.MinMaxBox(m, V(x - 1.15f, floor + 1.35f, z + dir * .2f), V(x + 1.15f, floor + 2.5f, z + dir * .28f), GlassDark);
                    m.Surface = ComicSurface.Plastic;
                    foreach (int s in new[] { -1, 1 }) K.MinMaxBox(m, V(x + s * .95f - .16f, floor + .45f, z + dir * .2f), V(x + s * .95f + .16f, floor + .7f, z + dir * .28f), Yellow);
                    K.MinMaxBox(m, V(x - 1.42f, floor + .92f, z + dir * .2f), V(x + 1.42f, floor + 1.12f, z + dir * .26f), White);
                    m.Surface = ComicSurface.PaintedMetal;
                    K.MinMaxBox(m, V(x - 1.2f, floor - .5f, z - dir * .3f), V(x + 1.2f, floor, z + dir * .32f), Ink);
                }
            }
        }

        // ================================================================================== trees and plants

        /// <summary>A faceted blob (a small ellipsoid: cheaper than the kit's 8 x 4 one).</summary>
        static void Blob(ComicMesh m, Vector3 c, Vector3 size, Color col, int sides = 6, int rings = 3)
        {
            Vector3 P(int ring, int side)
            {
                float a = Mathf.PI * ring / rings, b = Mathf.PI * 2f * side / sides;
                return c + Vector3.Scale(V(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b)), size) * .5f;
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++)
                {
                    if (r == 0) m.Tri(P(0, s), P(1, s + 1), P(1, s), col);
                    else if (r == rings - 1) m.Tri(P(r, s), P(r, s + 1), P(r + 1, s), col);
                    else m.Quad(P(r, s), P(r, s + 1), P(r + 1, s + 1), P(r + 1, s), col, false);
                }
        }

        /// <summary>A gum tree (eucalypt): a pale, leaning trunk that forks into limbs, an open, layered crown of
        /// olive-green clumps (one at each limb tip, lighter ones above, small ones hanging lower). far: fewer faces,
        /// for the unlit background mesh.</summary>
        static void Gum(ComicMesh m, Vector3 p, float h, int seed, bool far = false, Color? bark = null)
        {
            var r = new System.Random(seed * 7919 + 11);
            Color trunk = bark ?? Bark;
            float lean = F(r, -.1f, .1f), lean2 = F(r, -.08f, .08f), tr = .16f + h * .016f;
            Vector3 fork = p + V(lean * h, h * F(r, .4f, .5f), lean2 * h);
            int sides = far ? 4 : 6;
            m.Surface = ComicSurface.Wood;
            m.Cylinder((p + fork) * .5f, tr, (fork - p).magnitude, trunk, sides, .7f, Quaternion.FromToRotation(Vector3.up, fork - p));
            if (!far)
            {
                m.Surface = ComicSurface.PaintedMetal;
                m.Cylinder(p + V(0, .06f, 0), tr * 1.7f, .12f, Ink, 6, .9f);
                // bark shedding: a darker collar low on the trunk
                m.Surface = ComicSurface.Wood;
                m.Cylinder(Vector3.Lerp(p, fork, .18f), tr * 1.04f, h * .1f, Color.Lerp(trunk, BarkShade, .7f), sides, .93f, Quaternion.FromToRotation(Vector3.up, fork - p));
            }
            int limbs = far ? 2 : 3;
            float spin = F(r, 0f, 360f);
            var tips = new List<Vector3>();
            for (int i = 0; i < limbs; i++)
            {
                float a = (spin + i * 360f / limbs + F(r, -20f, 20f)) * Mathf.Deg2Rad, reach = h * F(r, .17f, .27f);
                Vector3 tip = fork + V(Mathf.Cos(a) * reach, h * F(r, .22f, .4f), Mathf.Sin(a) * reach);
                tips.Add(tip);
                m.Surface = ComicSurface.Wood;
                m.Cylinder((fork + tip) * .5f, tr * .55f, (tip - fork).magnitude, i % 2 == 0 ? trunk : Color.Lerp(trunk, BarkShade, .5f), far ? 4 : 5, .6f, Quaternion.FromToRotation(Vector3.up, tip - fork));
            }
            m.Surface = ComicSurface.Foliage;
            int bs = far ? 5 : 6;
            for (int i = 0; i < tips.Count; i++)
            {
                float s = h * F(r, .34f, .46f);
                Blob(m, tips[i] + V(0, s * .2f, 0), V(s, s * .78f, s * F(r, .82f, 1f)), i % 2 == 0 ? Leaf : LeafDark, bs, far ? 3 : 4);
                // a small clump hanging lower on the limb
                if (!far) Blob(m, Vector3.Lerp(fork, tips[i], .62f) + V(F(r, -.05f, .05f) * h, -h * .02f, F(r, -.05f, .05f) * h), V(s * .55f, s * .42f, s * .5f), LeafDark, 5, 3);
            }
            // the top of the crown: lighter clumps above and between the limbs
            Vector3 mid = Vector3.zero;
            foreach (Vector3 t in tips) mid += t;
            mid /= tips.Count;
            float ts = h * F(r, .4f, .5f);
            Blob(m, mid + V(0, h * .17f, 0), V(ts, ts * .74f, ts), LeafLight, bs, far ? 3 : 4);
            if (!far) Blob(m, Vector3.Lerp(mid, tips[0], .7f) + V(0, h * .2f, 0), V(ts * .6f, ts * .5f, ts * .6f), Color.Lerp(LeafLight, Leaf, .4f), 5, 3);
        }

        /// <summary>A jacaranda in flower (November): dark forked trunk, a broad crown of purple clumps.</summary>
        static void JacarandaTree(ComicMesh m, Vector3 p, float h, int seed)
        {
            var r = new System.Random(seed * 131 + 5);
            Color trunk = K.Hex("6B5847");
            Vector3 fork = p + V(0, h * .4f, 0);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .06f, 0), .5f, .12f, Ink, 6, .9f);
            m.Surface = ComicSurface.Wood;
            m.Cylinder((p + fork) * .5f, .24f, h * .4f, trunk, 6, .75f);
            float spin = F(r, 0f, 360f);
            for (int i = 0; i < 3; i++)
            {
                float a = (spin + i * 120f) * Mathf.Deg2Rad;
                Vector3 tip = fork + V(Mathf.Cos(a) * h * .28f, h * .3f, Mathf.Sin(a) * h * .28f);
                m.Surface = ComicSurface.Wood;
                m.Cylinder((fork + tip) * .5f, .13f, (tip - fork).magnitude, trunk, 5, .6f, Quaternion.FromToRotation(Vector3.up, tip - fork));
                m.Surface = ComicSurface.Foliage;
                Blob(m, tip + V(0, h * .08f, 0), V(h * .55f, h * .3f, h * .5f), i % 2 == 0 ? Jacaranda : JacarandaLight);
            }
            Blob(m, fork + V(0, h * .5f, 0), V(h * .5f, h * .3f, h * .5f), JacarandaLight);
        }

        /// <summary>A grass tree (balga): a black trunk, a round head of thin spiky leaves (stiff blades fanning up and
        /// out, a skirt of older ones hanging), a tall flower spike.</summary>
        static void GrassTree(ComicMesh m, Vector3 p, float s)
        {
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, .4f * s, 0), .17f * s, .8f * s, K.Hex("2B2A28"), 6, .85f);
            m.Surface = ComicSurface.Foliage;
            Vector3 c = p + V(0, .85f * s, 0);
            Color a = K.Hex("7E9A52"), b = K.Hex("5F7D3C");
            for (int i = 0; i < 9; i++)
            {
                float ang = i * Mathf.PI * 2f / 9f, up = i % 3 == 0 ? .95f : i % 3 == 1 ? .55f : .15f;
                Vector3 o = V(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                m.Beam(c, c + (o * (1.1f - up * .5f) + V(0, up, 0)) * .85f * s, .07f * s, i % 2 == 0 ? a : b);
            }
            for (int i = 0; i < 6; i++)
            {
                float ang = (i + .5f) * Mathf.PI * 2f / 6f;
                Vector3 o = V(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                m.Beam(c, c + (o * .8f + V(0, -.45f, 0)) * .8f * s, .06f * s, K.Hex("8A8552"));
            }
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, 1.9f * s, 0), .05f * s, 2f * s, K.Hex("5A4A36"), 5);
        }

        /// <summary>A clipped hedge box along z (flat road space).</summary>
        static void HedgeRow(ComicMesh m, int s, float z0, float z1, float from, float to, float h, Color? c = null)
        {
            m.Surface = ComicSurface.Foliage;
            Color body = c ?? Hedge;
            K.SideBox(m, s, z0, z1, K.Kerb - .1f, K.Kerb + h, from, to, body);
            K.SideBox(m, s, z0 + .05f, z1 - .05f, K.Kerb + h, K.Kerb + h + .12f, from + .12f, to - .12f, Color.Lerp(body, LeafLight, .45f));
        }

        /// <summary>A timber power pole (the old suburbs' street furniture): a tall brown pole, a crossarm with three
        /// insulators, a street light on a short arm towards the road.</summary>
        static void PowerPole(ComicMesh m, Vector3 p, int towards)
        {
            Color wood = K.Hex("7A6652");
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, 4.6f, 0), .16f, 9.2f, wood, 6, .75f);
            m.Box(p + V(0, 8.5f, 0), V(2.4f, .14f, .12f), wood);
            m.Surface = ComicSurface.Plastic;
            foreach (float x in new[] { -1.05f, 0f, 1.05f }) m.Box(p + V(x, 8.66f, 0), V(.12f, .2f, .12f), Cream);
            m.Surface = ComicSurface.PaintedMetal;
            m.Beam(p + V(0, 7.2f, 0), p + V(towards * 1.6f, 7.5f, 0), .07f, Steel);
            m.Box(p + V(towards * 1.8f, 7.46f, 0), V(.6f, .1f, .26f), Charcoal);
        }

        /// <summary>A low shrub: two squashed blobs.</summary>
        static void Shrub(ComicMesh m, Vector3 p, float s, Color c)
        {
            m.Surface = ComicSurface.Foliage;
            Blob(m, p + V(0, s * .3f, 0), V(s, s * .7f, s), c, 6, 3);
            Blob(m, p + V(s * .25f, s * .5f, s * .1f), V(s * .6f, s * .5f, s * .6f), Color.Lerp(c, K.R.yellow, .22f), 5, 3);
        }

        // ================================================================================== street furniture

        /// <summary>A street lamp: ink base, grey tapered pole, a curved arm over the road, a flat LED head.</summary>
        static void StreetLamp(ComicMesh m, Vector3 p, int towards)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .12f, 0), .2f, .24f, Ink, 8, .9f);
            m.Cylinder(p + V(0, 3.9f, 0), .09f, 7.6f, Steel, 6, .7f);
            m.Beam(p + V(0, 7.6f, 0), p + V(towards * 1.5f, 7.95f, 0), .09f, Steel);
            m.Box(p + V(towards * 1.9f, 7.92f, 0), V(.9f, .1f, .34f), Charcoal);
            m.Surface = ComicSurface.Plastic;
            m.Box(p + V(towards * 1.9f, 7.85f, 0), V(.7f, .05f, .26f), Cream);
        }

        /// <summary>A park bench facing the way yaw turns +Z... (seat back on the +Z side at yaw 0).</summary>
        static void Bench(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.Wood;
            m.Box(p + q * V(0, .45f, 0), V(1.8f, .08f, .45f), Timber, q);
            m.Box(p + q * V(0, .8f, .22f), V(1.8f, .35f, .07f), Timber, q);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.75f, .75f }) m.Box(p + q * V(x, .4f, .05f), V(.08f, .8f, .5f), Ink, q);
        }

        /// <summary>A parked car (front at +Z at yaw 0): body, cabin with a dark glass band, ink bumpers and wheels.</summary>
        static void ParkedCar(ComicMesh m, Vector3 p, float yaw, Color c)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + q * V(0, .62f, 0), V(1.8f, .6f, 4.2f), c, q);
            m.Box(p + q * V(0, 1.2f, -.2f), V(1.6f, .6f, 2.2f), Color.Lerp(c, Color.white, .12f), q);
            m.Surface = ComicSurface.Glass;
            m.Box(p + q * V(0, 1.2f, -.2f), V(1.64f, .4f, 2f), GlassDark, q);
            m.Box(p + q * V(0, 1.18f, -.2f), V(1.4f, .42f, 2.24f), GlassDark, q);
            m.Surface = ComicSurface.Rubber;
            foreach (float z in new[] { -1.3f, 1.3f }) m.Box(p + q * V(0, .32f, z), V(1.86f, .64f, .7f), Ink, q);
        }

        /// <summary>A name board facing the rider (-Z): white board with a green band on two grey posts, ink letters.</summary>
        static void NameBoard(ComicMesh m, ComicMesh text, Vector3 p, string name, float px = .1f)
        {
            float w = PixelWidth(name, px) + .8f, h = px * 9f;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -w * .4f, w * .4f }) m.Box(p + V(x, 1.3f, .06f), V(.12f, 2.6f, .12f), SteelDark);
            m.Box(p + V(0, 2.5f, 0), V(w, h, .08f), White);
            m.Box(p + V(0, 2.5f + h * .5f + .07f, 0), V(w, .14f, .1f), TrainGreen);
            PixelText(text, name, p.x, p.y + 2.5f, p.z - .05f, px, Ink);
        }

        // ---- pixel letters for the signs (place names and public buildings only)

        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "010/101/111/101/101", ['B'] = "110/101/110/101/110", ['C'] = "011/100/100/100/011", ['D'] = "110/101/101/101/110",
            ['E'] = "111/100/110/100/111", ['F'] = "111/100/110/100/100", ['G'] = "011/100/101/101/011", ['H'] = "101/101/111/101/101",
            ['I'] = "111/010/010/010/111", ['J'] = "001/001/001/101/010", ['K'] = "101/101/110/101/101", ['L'] = "100/100/100/100/111",
            ['M'] = "10001/11011/10101/10001/10001", ['N'] = "1001/1101/1011/1001/1001", ['O'] = "010/101/101/101/010", ['P'] = "110/101/110/100/100",
            ['Q'] = "010/101/101/110/011", ['R'] = "110/101/110/101/101", ['S'] = "011/100/010/001/110", ['T'] = "111/010/010/010/010",
            ['U'] = "101/101/101/101/111", ['V'] = "101/101/101/101/010", ['W'] = "10001/10001/10101/10101/01010", ['X'] = "101/101/010/101/101",
            ['Y'] = "101/101/010/010/010", ['Z'] = "111/001/010/100/111", ['1'] = "010/110/010/010/111", ['2'] = "110/001/010/100/111",
            ['3'] = "110/001/010/001/110",
        };

        static float PixelWidth(string text, float px)
        {
            int cols = 0;
            foreach (char ch in text) cols += (Glyphs.TryGetValue(ch, out string g) ? g.IndexOf('/') : 2) + 1;
            return Mathf.Max(0, cols - 1) * px;
        }

        /// <summary>Pixel text facing -Z (towards the rider), centred on (cx, cy) at z.</summary>
        static void PixelText(ComicMesh m, string text, float cx, float cy, float z, float px, Color c)
        {
            m.Surface = ComicSurface.Plain;
            float x = cx - PixelWidth(text, px) * .5f, y0 = cy + 2.5f * px;
            foreach (char ch in text)
            {
                if (!Glyphs.TryGetValue(ch, out string g)) { x += 3 * px; continue; }
                string[] rows = g.Split('/');
                for (int row = 0; row < rows.Length; row++)
                    for (int col = 0; col < rows[row].Length; col++)
                    {
                        if (rows[row][col] != '1') continue;
                        float qx = x + col * px, qy = y0 - (row + 1) * px;
                        m.Quad(V(qx + px, qy, z), V(qx, qy, z), V(qx, qy + px, z), V(qx + px, qy + px, z), c, false);
                    }
                x += (rows[0].Length + 1) * px;
            }
        }

        /// <summary>Pixel text centred on c, facing the way rot turns -Z (yaw -90 faces +X; yaw 90 faces -X).</summary>
        static void PixelTextFacing(ComicMesh m, string text, Vector3 c, float yaw, float px, Color colour)
        {
            var local = new ComicMesh();
            PixelText(local, text, 0f, 0f, 0f, px, colour);
            m.Add(local, c, Quaternion.Euler(0, yaw, 0), Vector3.one);
        }

        // ================================================================================== prefabs

        // An animal walker (kind Person, animal, never punchable). Facing -Z, feet at the pivot.
        static GameObject SaveAnimal(string path, string meshId, ComicMesh art, Vector3 centre, Vector3 size, float walk, bool jump)
        {
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            GameObject go = sync.Plain("Art");
            K.Renderer(go, meshId, art, true, false, false);
            sync.Finish();
            var box = PrefabBuildUtility.KeepSingle<BoxCollider>(root) ?? root.AddComponent<BoxCollider>();
            box.center = centre; box.size = size; box.isTrigger = false;
            var o = PrefabBuildUtility.GetOrAdd<Obstacle>(root);
            o.kind = ObstacleKind.Person; o.animal = true; o.punchable = false; o.police = false; o.canJumpOver = jump; o.spansRoad = false; o.spawnWeight = 1f;
            var ped = PrefabBuildUtility.GetOrAdd<Pedestrian>(root);
            ped.walkSpeed = walk; ped.crosses = false; ped.art = go.transform; ped.dodgeSpeed = 6f;
            PrefabBuildUtility.SaveAndClose(root, path);
            K.Report.Add(System.IO.Path.GetFileNameWithoutExtension(path) + " " + art.TriangleCount * 2 + " tris");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static Bounds KeepOutBox(CqChunkDraft d, int s, float z0, float z1)
        {
            float yTop = d.Y(z0) + 5f, yBottom = d.Y(z1) - 2f;
            return new Bounds(V(s * (K.Half + K.Side * .5f + .3f), (yTop + yBottom) * .5f, (z0 + z1) * .5f), V(K.Side + .6f, yTop - yBottom, z1 - z0));
        }

        /// <summary>An invisible wall along one side just beyond the sidewalk (keeps people off the tracks / the water).</summary>
        static void SideWall(CqChunkDraft d, int s, float z0, float z1, float depth = 14f)
        {
            float zc = (z0 + z1) * .5f;
            d.Walls.Add(new Bounds(V(s * (K.Edge + .6f + depth * .5f), d.Y(zc) + 1f, zc), V(depth, 5f, z1 - z0)));
        }

        // ================================================================================== far scenery parts

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

        // ---- the Darling Scarp: one ridge across the horizon
        //
        // The comic shader bends the far world (ComicWorldBend.hlsl): beyond 32 m ahead everything is lifted, beyond
        // 180 m it is also squeezed towards the road's vanishing point, and the linear distance fog (120 to 310 m) turns
        // anything far into the pale fog colour. So a hill placed far ahead ends as a pale point, and hills along the
        // sides turn into two pale domes with a notch between them. The ridge is therefore built the other way round:
        // every vertex is given where it should APPEAR from the chase camera (azimuth, elevation, range) and Unbend puts
        // it where the shader will carry it there. It stands only about 120 m from the rider, in front of nearly all the
        // fog, so it keeps a dark colour (FogProof pays for the little fog there is); as it follows the rider and
        // nothing ever passes behind its skyline, it still reads as a ridge kilometres away. Over the road corridor its
        // lower edge floats above everything that is still to come (chunks are built to 320 m ahead; nothing in these
        // two districts stands higher than 24 m), and a pale wall behind (310 m, where the fog is complete) closes the gap:
        // the road runs into haze at the foot of the hills and the skyline stays whole.

        const float CamBack = 5.6f, CamUp = 3.38f, FogStart = 120f, FogEnd = 310f;

        /// <summary>The far-scenery vertex (relative to the rider; NOT flat road space: add it after Shear) that the
        /// shader shows at xa across, ya above the rider's ground, d ahead.</summary>
        static Vector3 Unbend(float xa, float ya, float d)
        {
            ComicCityRecipe r = K.R;
            float s = ComicCityProjection.Scale(r, d), w = r.downhillGrade, rise = ComicCityProjection.Rise(r, d);
            return V(xa / s, (ya - rise + w * d) / s - w * d, d);
        }

        /// <summary>The vertex seen from the chase camera at this azimuth (degrees, 0 = ahead, + = right) and elevation
        /// (degrees above level), at this ground range from the rider.</summary>
        static Vector3 Apparent(float azimuth, float elevation, float range)
        {
            float a = azimuth * Mathf.Deg2Rad, xa = range * Mathf.Sin(a), d = range * Mathf.Cos(a);
            float rho = Mathf.Sqrt(xa * xa + (d + CamBack) * (d + CamBack));
            return Unbend(xa, CamUp + rho * Mathf.Tan(elevation * Mathf.Deg2Rad), d);
        }

        /// <summary>The vertex colour that ends up as <paramref name="target"/> on screen after the sun (lit band) and
        /// the linear fog at this view depth (it cannot be darker than the fog allows).</summary>
        static Color FogProof(Color target, Color fog, float depth)
        {
            float f = Mathf.Min(.85f, Mathf.Clamp01((depth - FogStart) / (FogEnd - FogStart)));
            Color t = target.linear, g = fog.linear;
            var c = new Color(Mathf.Max(0f, (t.r - f * g.r) / (1f - f)) / 1.5f, Mathf.Max(0f, (t.g - f * g.g) / (1f - f)) / 1.3f, Mathf.Max(0f, (t.b - f * g.b) / (1f - f)) / .95f, 1f);
            return c.gamma;
        }

        /// <summary>True when a far-scenery point stands in front of the ridge (inside its ring), or in the road corridor
        /// ahead, where the ridge floats.</summary>
        static bool InFrontOfScarp(float x, float z, Func<float, float> rangeAt) =>
            Mathf.Sqrt(x * x + z * z) < rangeAt(Mathf.Atan2(x, z) * Mathf.Rad2Deg) - 14f || (Mathf.Abs(x) < 96f && z > 0f && z < 330f);

        /// <summary>
        /// The Darling Scarp: one long, level ridge right across the view ahead and round both sides (lower as it runs
        /// away north and south), a dark eucalypt blue-green against the sky. Its top line is only gently uneven: low
        /// knolls, two gullies and the shallow dip of the highway cutting a little right of the road. Three tones (dark
        /// forest top, mid slope, paler dry foot fading into the haze), darker tree masses and lighter clearings on its face, pale quarry
        /// scars, masts on the crest, an ink line along the skyline only. topDeg: the skyline's height above level
        /// straight ahead, as seen from the camera; clearDeg: the height of its floating lower edge over the road
        /// corridor (above everything still to come); haze: how far its colours go towards the fog (further away =
        /// hazier); rangeAt: the ground range of its foot by azimuth. Add it AFTER the far scenery is sheared.
        /// </summary>
        static void ScarpRing(ComicMesh m, float topDeg, float clearDeg, Color fog, float haze, Func<float, float> rangeAt, int seed, int masts, bool highway)
        {
            var r = new System.Random(seed);
            Color dark = Color.Lerp(K.Hex("2F5959"), fog, haze), mid = Color.Lerp(K.Hex("4F7A62"), fog, haze), foot = Color.Lerp(K.Hex("96A174"), fog, haze),
                mass = Color.Lerp(K.Hex("21444A"), fog, haze), clearing = Color.Lerp(K.Hex("688C68"), fog, haze), quarry = Color.Lerp(K.Hex("DCC9A0"), fog, haze * .7f),
                ink = Color.Lerp(K.Hex("14282C"), fog, haze * .6f);
            float tan0 = Mathf.Tan(topDeg * Mathf.Deg2Rad);
            float Top(float a)
            {
                // A level line on the screen needs tan(elevation) to fall with cos(azimuth); a little is kept back so
                // the ridge still stands (lower) out to the sides.
                float level = Mathf.Max(.3f, .25f + .75f * Mathf.Cos(a * Mathf.Deg2Rad));
                float n = .022f * Mathf.Sin(a * .21f + 1.1f) + .014f * Mathf.Sin(a * .53f + 2.3f) + .008f * Mathf.Sin(a * 1.3f + .4f);
                float knolls = .03f * Mathf.Exp(-Mathf.Pow((a + 14f) / 5f, 2f)) + .025f * Mathf.Exp(-Mathf.Pow((a - 30f) / 4f, 2f)) + .02f * Mathf.Exp(-Mathf.Pow((a + 52f) / 6f, 2f));
                float saddle = .075f * Mathf.Exp(-Mathf.Pow((a - 5f) / 6f, 2f));
                float gully = .03f * Mathf.Exp(-Mathf.Pow((a + 27f) / 3f, 2f)) + .028f * Mathf.Exp(-Mathf.Pow((a - 42f) / 2.5f, 2f));
                return Mathf.Atan(tan0 * level * (1f + n + knolls - saddle - gully)) * Mathf.Rad2Deg;
            }
            // The lower edge. Straight ahead it clears whatever is still to come (the tallest things, up to 24 m, stand
            // within about 22 degrees of the road); further round only trees and roofs are left behind it, so it comes
            // down, and from 44 degrees, where nothing is behind it any more, it runs down into the ground like a spur.
            float Bottom(float a)
            {
                float aa = Mathf.Abs(a);
                float e = aa <= 22f ? clearDeg : aa <= 34f ? Mathf.Lerp(clearDeg, clearDeg - 1.8f, (aa - 22f) / 12f) : aa <= 44f ? Mathf.Lerp(clearDeg - 1.8f, clearDeg - 3f, (aa - 34f) / 10f)
                    : Mathf.Lerp(clearDeg - 3f, -4f, Mathf.SmoothStep(0f, 1f, (aa - 44f) / 24f));
                return Mathf.Min(e, Top(a) * .72f);
            }
            // where the solid colour starts: the lower edge, plus the strip of haze where it floats
            float Floor(float a) { float e = Bottom(a); return e > .5f ? e + Mathf.Min(1f, e) : e; }
            float Level(float a, float t) => Mathf.Lerp(Floor(a), Top(a), t);                 // t: 0 = lowest forest, 1 = skyline
            // The tones lie level along the ridge (fractions of the skyline's height that wander a little); the lower edge
            // cuts through them, with a strip of haze along it where it floats.
            float Tone(float a, int k) => k <= 0 ? -.3f : k == 1 ? .26f + .03f * Mathf.Sin(a * .61f + .3f) : k == 2 ? .5f + .035f * Mathf.Sin(a * .47f + 1.9f) : k == 3 ? .79f + .035f * Mathf.Sin(a * .39f + .6f) : 1f;
            Vector3 P(float a, float e, float nearer = 0f) => Apparent(a, e, rangeAt(a) + .5f * Mathf.Max(0f, e) - nearer);
            Vector3 Hint(float a) => V(-Mathf.Sin(a * Mathf.Deg2Rad), .9f, -Mathf.Cos(a * Mathf.Deg2Rad));
            float Depth(float a) => Mathf.Max(0f, (rangeAt(a) + 8f) * Mathf.Cos(a * Mathf.Deg2Rad) + CamBack);
            m.Surface = ComicSurface.Plain;
            const float step = 2f;
            Color[] tones = { Color.Lerp(foot, fog, .5f), foot, mid, dark };
            for (float a = -128f; a < 127.9f; a += step)
            {
                float b = a + step, fa = Floor(a), fb = Floor(b), ta = Top(a), tb = Top(b);
                Color cut = dark;
                for (int k = 0; k < 4; k++)
                {
                    float la = Mathf.Max(fa, ta * Tone(a, k)), lb = Mathf.Max(fb, tb * Tone(b, k)), ha = Mathf.Max(fa, ta * Tone(a, k + 1)), hb = Mathf.Max(fb, tb * Tone(b, k + 1));
                    if (k == 0) { la = Mathf.Max(fa, -4f); lb = Mathf.Max(fb, -4f); }
                    if (ha - la < .02f && hb - lb < .02f) continue;
                    if (ha - la > .02f && la <= fa + .001f) cut = tones[k];
                    K.Face(m, P(a, la), P(b, lb), P(b, hb), P(a, ha), FogProof(tones[k], fog, Depth(a + 1f)), Hint(a + 1f), false);
                }
                // haze along the floating lower edge
                float ba = Bottom(a), bb = Bottom(b);
                if (ba > .5f || bb > .5f)
                    K.Face(m, P(a, ba), P(b, bb), P(b, fb), P(a, fa), FogProof(Color.Lerp(cut, fog, .55f), fog, Depth(a + 1f)), Hint(a + 1f), false);
                // the ink line along the skyline
                K.Face(m, P(a, ta), P(b, tb), P(b, tb + .2f), P(a, ta + .2f), FogProof(ink, fog, Depth(a + 1f)), Hint(a + 1f), false);
            }
            // tree masses (darker) high on the face, lighter clearings lower down: lens shapes, not boxes
            for (int i = 0; i < 44; i++)
            {
                float a0 = Mathf.Round(F(r, -122f, 114f) / step) * step, w = step * r.Next(2, 6), t0 = F(r, .3f, .8f), t1 = Mathf.Min(.95f, t0 + F(r, .1f, .2f));
                bool high = t0 > .6f;
                for (float a = a0; a < a0 + w - .01f; a += step)
                {
                    float ea = Mathf.Sin(Mathf.PI * (a - a0) / w), eb = Mathf.Sin(Mathf.PI * (a + step - a0) / w), tm = (t0 + t1) * .5f, h = (t1 - t0) * .5f;
                    K.Face(m, P(a, Level(a, tm - h * ea), 1f), P(a + step, Level(a + step, tm - h * eb), 1f), P(a + step, Level(a + step, tm + h * eb), 1f), P(a, Level(a, tm + h * ea), 1f),
                        FogProof(high ? mass : clearing, fog, Depth(a + 1f)), Hint(a + 1f), false);
                }
            }
            // quarry scars: pale, low on the slope
            foreach ((float a0, float w, float t0, float t1) in new[] { (33f, 6f, .1f, .36f), (-64f, 8f, .14f, .34f), (76f, 6f, .2f, .4f) })
                for (float a = a0; a < a0 + w - .01f; a += step)
                {
                    float ea = Mathf.Sin(Mathf.PI * (a - a0) / w), eb = Mathf.Sin(Mathf.PI * (a + step - a0) / w), tm = (t0 + t1) * .5f, h = (t1 - t0) * .5f;
                    K.Face(m, P(a, Level(a, tm - h * ea), 1.4f), P(a + step, Level(a + step, tm - h * eb), 1.4f), P(a + step, Level(a + step, tm + h * eb * .6f), 1.4f), P(a, Level(a, tm + h * ea * .6f), 1.4f),
                        FogProof(quarry, fog, Depth(a + 1f)), Hint(a + 1f), false);
                }
            // the highway climbing to its cutting: a thin pale line up to the dip in the skyline
            if (highway)
                for (int i = 0; i < 4; i++)
                {
                    float a = 12f - i * 1.6f, b = a - 1.6f, ta = .1f + i * .2f, tb = ta + .2f;
                    K.Face(m, P(a, Level(a, ta), 1.6f), P(b, Level(b, tb), 1.6f), P(b, Level(b, tb) + .22f, 1.6f), P(a, Level(a, ta) + .22f, 1.6f), FogProof(quarry, fog, Depth(a)), Hint(a), false);
                }
            // masts on the crest: red and white
            float[] mastAt = { 22f, -36f, 61f };
            for (int i = 0; i < Mathf.Min(masts, mastAt.Length); i++)
            {
                float a = mastAt[i], t = Top(a);
                K.Face(m, P(a - .1f, t), P(a + .1f, t), P(a + .06f, t + 1.1f), P(a - .06f, t + 1.1f), FogProof(K.Hex("C8372D"), fog, Depth(a)), Hint(a), false);
                K.Face(m, P(a - .06f, t + 1.1f), P(a + .06f, t + 1.1f), P(a + .03f, t + 1.8f), P(a - .03f, t + 1.8f), FogProof(White, fog, Depth(a)), Hint(a), false);
            }
            // behind the corridor, where the fog is complete (so nothing is lost behind it): the pale haze the road runs
            // into, under the floating ridge. At 310 m it still sinks with the rest when the scenery leaves at a boundary.
            for (float a = -70f; a < 69.9f; a += 2f)
            {
                float b = a + 2f, da = FogEnd / Mathf.Cos(a * Mathf.Deg2Rad), db = FogEnd / Mathf.Cos(b * Mathf.Deg2Rad), ta = Bottom(a) + .8f, tb = Bottom(b) + .8f;
                if (ta < 1.2f && tb < 1.2f) continue;
                K.Face(m, Apparent(a, 0f, da), Apparent(b, 0f, db), Apparent(b, Mathf.Max(.2f, tb), db), Apparent(a, Mathf.Max(.2f, ta), da), foot, Vector3.back + Vector3.up, false);
            }
        }

        /// <summary>A background house: a box and a pyramid-hip roof (far scenery and deep rows; no outline).</summary>
        static void FarHouse(ComicMesh m, Vector3 p, float w, float h, Color wall, Color roof)
        {
            m.Surface = ComicSurface.Plaster;
            m.Box(p + V(0, h * .5f - 1f, 0), V(w, h + 2f, w * .8f), wall);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(p + V(0, h + .8f, 0), w * .74f, 2f, roof, 4, .15f, Quaternion.Euler(0, 45, 0));
        }

        // Height field in flat road space (x0..x1, z0..z1), cells of about dx by dz; y = h(x, z), colour col(x, z, y).
        static void Terrain(ComicMesh m, float x0, float x1, float z0, float z1, float dx, float dz, Func<float, float, float> h, Func<float, float, float, Color> col, ComicSurface surface)
        {
            m.Surface = surface;
            int nx = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(x1 - x0) / dx)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / dz));
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float xa = Mathf.Lerp(x0, x1, (float)i / nx), xb = Mathf.Lerp(x0, x1, (float)(i + 1) / nx);
                    float za = Mathf.Lerp(z0, z1, (float)j / nz), zb = Mathf.Lerp(z0, z1, (float)(j + 1) / nz);
                    Vector3 a = V(xa, h(xa, za), za), b = V(xa, h(xa, zb), zb), c = V(xb, h(xb, zb), zb), dd = V(xb, h(xb, za), za);
                    K.Face(m, a, b, c, dd, col((xa + xb) * .5f, (za + zb) * .5f, (a.y + b.y + c.y + dd.y) * .25f), Vector3.up, false);
                }
        }

        /// <summary>Shears a mesh built in flat road space with the chunk's grade (what WriteChunk does to the road), so
        /// it can go into an upright mesh (Deep, Landmark) and still follow the tracks.</summary>
        static ComicMesh Sheared(CqChunkDraft d, Action<ComicMesh> build)
        {
            var m = new ComicMesh();
            build(m);
            m.Shear(d.Grade);
            return m;
        }

        static void AddSheared(CqChunkDraft d, ComicMesh target, Action<ComicMesh> build) =>
            target.Add(Sheared(d, build), Vector3.zero, Quaternion.identity, Vector3.one);
    }
}
