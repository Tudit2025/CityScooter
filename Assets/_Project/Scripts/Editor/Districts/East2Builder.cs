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
    /// East line, art agent 2 (v0.5): MT LAWLEY (route order 240), MAYLANDS (250) and MELTHAM (260) on the Midland train
    /// line. The road follows Whatley Crescent and the shared path on the south side of the railway, so the tracks (with
    /// overhead-wire masts, island platforms and Transperth-style trains) are on the LEFT (-X) and the suburb and the Swan
    /// River on the right. All of it is scenery: nothing of the railway has a collider.
    /// MT LAWLEY: the station in its cutting under big fig trees; Federation houses (red brick with render bands, white
    /// timber verandahs, iron roofs, tall chimneys, picket fences) under jacarandas in bloom; the Beaufort St cafe strip
    /// with red-brick paving, blue Art Deco lamps and cotton palms; the Art Deco Astor Theatre (white, burgundy fins,
    /// vertical ASTOR sign) with a roadies' loading ramp at its stage door.
    /// MAYLANDS: the old brick parcels office on the island platform, the Peninsula Hotel (two-storey verandahs, the
    /// corner tower with its zinc dome), the Eighth Ave shops and The RISE, then down the peninsula: the Maylands
    /// Brickworks (Hoffman kiln, the 34 m square chimney, the iron pug mill), the Swan River with black swans, and a
    /// boardwalk over the samphire flats with a missing span (SPLASH).
    /// MELTHAM: the quiet island platform with its brick shelter and murals; classic Perth suburbia: brick-and-tile and
    /// fibro bungalows, Hills Hoists, super-six fences, verge trees, a corner deli, a pocket park, a speed hump, and the
    /// Hotham St bridge over the line.
    /// This file: entry points, palette and the shared kit (text, trees, houses, shops, railway). The districts are in
    /// East2MtLawley.cs, East2Maylands.cs and East2Meltham.cs. Uses the CBD / Quay comic kit (CbdQuayKit, CqChunkDraft)
    /// read-only: ComicPalette + ComicInk only, vertex colours.
    ///   Menu: CityScooter/Districts/Build East 2
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.East2Builder.BuildBatch
    ///   Batch with the props atlases (needs graphics): ...East2Builder.BuildAndRenderAtlasBatch
    /// </summary>
    public static partial class East2Builder
    {
        public const string DataFolder = "Assets/_Project/Data/Districts";
        static string Prefabs(string pascal) => ComicCityBuilder.Prefabs + "/Districts/" + pascal;
        static string Meshes(string pascal) => ComicCityBuilder.Root + "/Districts/" + pascal;
        public static string ChunkPath(string pascal, string variant) => Prefabs(pascal) + "/CHN_" + pascal + "_" + variant + ".prefab";
        public static string DistrictPath(string pascal) => DataFolder + "/District_" + pascal + ".asset";

        [MenuItem("CityScooter/Districts/Build East 2")]
        public static void Build() => Build(false);

        public static void BuildBatch()
        {
            try { Build(false); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Builds the three districts and renders art-src/districts/&lt;id&gt;/props-atlas.png (run with graphics).</summary>
        public static void BuildAndRenderAtlasBatch()
        {
            try { Build(true); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Build(bool atlas)
        {
            // The theme is loaded again for each district: rendering an atlas opens a fresh preview scene, which unloads it.
            BuildMtLawley(LoadTheme(), atlas);
            BuildMaylands(LoadTheme(), atlas);
            BuildMeltham(LoadTheme(), atlas);
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

        static RoadChunk Chunk(string pascal, string variant) => AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(pascal, variant)).GetComponent<RoadChunk>();

        /// <summary>Fills the fields this builder owns. gateway, ambience, ambienceVolume, skyYaw, skySize, skyHeight,
        /// skyline and line belong to the district system: only given placeholders when the asset is first created (the
        /// CBD skyline sunk away behind the rider), never written again.</summary>
        static District WriteDistrict(string pascal, string id, string displayName, int order, Vector2 length, string[] chunkIds)
        {
            string path = DistrictPath(pascal);
            bool created = AssetDatabase.LoadAssetAtPath<District>(path) == null;
            District d = K.LoadOrCreate<District>(path);
            if (created) { d.skyYaw = 180f; d.skySize = 1f; d.skyHeight = 0f; d.skyline = 0f; }
            d.id = id; d.displayName = displayName; d.order = order; d.length = length;
            d.chunks = chunkIds.Select(c => Chunk(pascal, c)).ToArray();
            d.orderedChunks = true;
            d.entry = Chunk(pascal, "Entry");
            d.exit = Chunk(pascal, "Exit");
            d.allowBoulevard = false;
            d.gifts = Array.Empty<Gift>();
            d.blendMeters = 150f;
            return d;
        }

        /// <summary>District.length for one pass through the district's own chunks: the generator lays the entry, then
        /// own chunks while the next one still leaves room for the exit, so a length just under entry + chunks + exit
        /// (40 m each) gives each chunk exactly once. The landmarks (the Astor, the Peninsula Hotel, the brickworks) are
        /// one of a kind: they must not come round twice in one visit.</summary>
        static Vector2 OnePass(int chunks) => new Vector2(40f * (chunks + 1) + 10f, 40f * (chunks + 2) - 5f);

        // ================================================================================== palette (sRGB)

        static Color Hex(string h) => K.Hex(h);
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static float F(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);
        static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, t);
        static Color Ink => K.R.ink;

        static readonly Color White = Hex("F4F1E8"), Cream = Hex("EFE3C4"), CreamDark = Hex("D9C9A2"), Charcoal = Hex("3A4048");
        static readonly Color Brick = Hex("A84A32"), BrickDark = Hex("8C3B29"), BrickOrange = Hex("BC5F3C"), BrickCream = Hex("D9BE8E"), BrickSalmon = Hex("CF8C6C");
        static readonly Color Limestone = Hex("E2D2A6"), LimeDark = Hex("BFA978"), LimeCap = Hex("F0E6CA");
        static readonly Color Galv = Hex("AEB7BC"), GalvDark = Hex("8E989E"), RoofRed = Hex("9E3D2E"), RoofGreen = Hex("56755F"), Tile = Hex("BE5E3E"), TileBrown = Hex("8C5A44"), Zinc = Hex("8FA0AC");
        static readonly Color Lawn = Hex("6FA548"), LawnLight = Hex("86B858"), LawnDry = Hex("A3B062"), Hedge = Hex("4E7F3A"), HedgeDark = Hex("3E6A34"), Soil = Hex("8A6A4A"), Mulch = Hex("9A6B4C");
        static readonly Color Jac0 = Hex("7461C4"), Jac1 = Hex("8A76D8"), Jac2 = Hex("A28DE8"), Jac3 = Hex("C3B1F4"), JacBark = Hex("5E4C40"), JacLeaf = Hex("7FA650");
        static readonly Color GumTrunk = Hex("E8E0CF"), GumPatch = Hex("B8AE9C"), GumLeaf = Hex("6E8C54"), GumLeafLight = Hex("90A86C"), GumLeafDark = Hex("567340");
        static readonly Color FigDark = Hex("2F5C36"), FigMid = Hex("3F7040"), FigLight = Hex("5B8A4A"), FigBark = Hex("7A6C5E");
        static readonly Color Glass = Hex("2F6F86"), GlassDark = Hex("25424F"), GlassPale = Hex("9CC6D4"), Timber = Hex("A8774E"), TimberDark = Hex("6E4B32"), Deck = Hex("9A6B45"), DeckLight = Hex("B88757");
        static readonly Color Ballast = Hex("A6A198"), BallastDark = Hex("8F8A80"), Sleeper = Hex("7C766C"), RailRust = Hex("7A5642"), RailTop = Hex("C9CDD0"), Mast = Hex("A3ACB3"), MastDark = Hex("7B858D");
        static readonly Color PlatformBrick = Hex("B4583F"), Tactile = Hex("C6C8C8"), PspRed = Hex("A85743"), PspRedDark = Hex("9C4E3A"), Concrete = Hex("CFC9BC"), ConcreteDark = Hex("ACA69A");
        static readonly Color TrainSilver = Hex("D5DADF"), TrainGreen = Hex("2F9E5B"), TrainGreenDark = Hex("1F7A45"), TrainYellow = Hex("F2C230");
        static readonly Color River = Hex("4B8FBE"), RiverLight = Hex("8FC0DC"), RiverDeep = Hex("3D78AE"), Reed = Hex("8A9A4A"), ReedDark = Hex("6C7E3E"), Samphire = Hex("9C6E5A");
        static readonly Color PavingRed = Hex("B8694E"), PavingRedDark = Hex("9E563F"), PavingGrey = Hex("D6D1C6"), KerbCap = Hex("EDE8DC"), PathConcrete = Hex("D8D2C4");
        static readonly Color Burgundy = Hex("9C2F35"), Teal = Hex("2F8F8A"), DecoBlue = Hex("2F5FA8"), Navy = Hex("2A3E6E"), Mustard = Hex("E0A93A"), HeritageGreen = Hex("2F5D46");
        static readonly Color[] SignColours = { Hex("E2463A"), Hex("F6C543"), Hex("2A9D9A"), Hex("2F6FB0"), Hex("F28C28"), Hex("E85A8C"), Hex("5B8A4A") };

        static Color Pick(Color[] list, int i) => list[((i % list.Length) + list.Length) % list.Length];
        static int Hash(string s) => Mathf.Abs(ComicKitGeometry.StableHash(s));

        // ================================================================================== small geometry helpers

        /// <summary>A low-poly lumpy ellipsoid (7 sides, 4 rings = 42 triangles by default): tree crowns, bushes, clouds of
        /// blossom. lump = how far each vertex is pushed in or out (deterministic), so no two crowns are the same box.</summary>
        static void Blob(ComicMesh m, Vector3 c, Vector3 size, Color col, int sides = 7, int rings = 4, float spin = 0f, float lump = .13f)
        {
            int seed = Mathf.RoundToInt(c.x * 13.1f + c.y * 7.7f + c.z * 3.3f + spin * 5f);
            Vector3 P(int ring, int side)
            {
                side %= sides;
                float a = Mathf.PI * ring / rings, b = Mathf.PI * 2f * side / sides + spin + (ring % 2) * Mathf.PI / sides;
                float k = ring == 0 || ring == rings ? 1f : 1f + lump * Mathf.Sin(seed * 1.7f + ring * 12.9898f + side * 78.233f) * 1.4f;
                return c + Vector3.Scale(new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b)), size) * (.5f * k);
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++)
                {
                    if (r == 0) m.Tri(P(0, s), P(1, s + 1), P(1, s), col);
                    else if (r == rings - 1) m.Tri(P(r, s), P(r, s + 1), P(r + 1, s), col);
                    else if (r % 2 == 1) { m.Tri(P(r, s), P(r, s + 1), P(r + 1, s + 1), col); m.Tri(P(r, s), P(r + 1, s + 1), P(r + 1, s), col); }
                    else { m.Tri(P(r, s), P(r, s + 1), P(r + 1, s), col); m.Tri(P(r, s + 1), P(r + 1, s + 1), P(r + 1, s), col); }
                }
        }

        /// <summary>A tapered limb from a to b (6 sides).</summary>
        static void Limb(ComicMesh m, Vector3 a, Vector3 b, float ra, float rb, Color c, int sides = 6) =>
            m.Cylinder((a + b) * .5f, ra, (b - a).magnitude, c, sides, rb / ra, Quaternion.FromToRotation(Vector3.up, b - a));

        /// <summary>A slab from a to b (thick, width wa -> wb), top colour up, darker underneath (palm fronds).</summary>
        static void Slab(ComicMesh m, Vector3 a, Vector3 b, Vector3 side, float wa, float wb, float thick, Color top, Color under)
        {
            Vector3 along = (b - a).normalized, n = Vector3.Cross(along, side).normalized;
            if (n.y < 0f) n = -n;
            Vector3 A(float w, float t) => a + side * w + n * t;
            Vector3 B(float w, float t) => b + side * w + n * t;
            float ha = wa * .5f, hb = wb * .5f, ht = thick * .5f;
            Color edge = Color.Lerp(top, under, .5f);
            K.Face(m, A(-ha, ht), B(-hb, ht), B(hb, ht), A(ha, ht), top, n);
            K.Face(m, A(-ha, -ht), B(-hb, -ht), B(hb, -ht), A(ha, -ht), under, -n);
            K.Face(m, A(-ha, -ht), B(-hb, -ht), B(-hb, ht), A(-ha, ht), edge, -side);
            K.Face(m, A(ha, -ht), B(hb, -ht), B(hb, ht), A(ha, ht), edge, side);
            K.Face(m, B(-hb, -ht), B(hb, -ht), B(hb, ht), B(-hb, ht), edge, along);
        }

        /// <summary>A flat quad lying on the ground (no ink), x0..x1 by z0..z1 at height y, split every 4 m along z.</summary>
        static void Ground(ComicMesh m, float x0, float x1, float z0, float z1, float y, Color c, ComicSurface surface = ComicSurface.Plain)
        {
            m.Surface = surface;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.Face(m, V(x0, y, z), V(x0, y, e), V(x1, y, e), V(x1, y, z), c, Vector3.up, false);
            }
        }

        /// <summary>Ground beyond the sidewalk on side s (a slab down to -2.4, sheared with the road).</summary>
        static void SideGround(CqChunkDraft d, int s, float z0, float z1, float depth, Color c, ComicSurface surface, float lift = .02f)
        {
            d.Road.Surface = surface;
            K.SideBox(d.Road, s, z0, z1, -2.4f, K.Kerb + lift, 0f, depth, c);
        }

        // ---- pixel text (the 3x5 font of the gateway signs, DistrictSetup.Glyphs), on any plane; runs of pixels are merged.

        // The gateway font's N (3 wide) reads as a D at a glance ("MAYLADDS"): a 5-wide N with a proper diagonal here.
        static readonly Dictionary<char, string> GlyphFix = new Dictionary<char, string> { ['N'] = "1000111001101011001110001" };
        static string Glyph(char c) => GlyphFix.TryGetValue(c, out string g) ? g : DistrictSetup.Glyphs.TryGetValue(c, out g) ? g : null;

        /// <summary>Pixel columns the text takes (glyph widths plus one empty column between glyphs; a space is 3).</summary>
        static int TextCols(string text)
        {
            int n = 0;
            foreach (char c in text ?? "") { string g = Glyph(c); n += (g != null ? g.Length / 5 : 3) + 1; }
            return Mathf.Max(0, n - 1);
        }

        /// <summary>Text centred on <paramref name="centre"/>: right = reading direction, up = letter up. Faces cross(up, right).</summary>
        static void Text(ComicMesh m, string text, Vector3 centre, Vector3 right, Vector3 up, float px, Color c)
        {
            m.Surface = ComicSurface.Plain;
            Vector3 n = Vector3.Cross(up, right).normalized;
            int cols = TextCols(text), left = 0;
            Vector3 origin = centre - right * (cols * px * .5f) + up * (2.5f * px);
            foreach (char ch in text)
            {
                string g = Glyph(ch);
                if (g == null) { left += 4; continue; }
                int w = g.Length / 5;
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < w; col++)
                    {
                        if (g[row * w + col] != '1') continue;
                        int end = col;
                        while (end + 1 < w && g[row * w + end + 1] == '1') end++;
                        Vector3 a = origin + right * ((left + col) * px) - up * ((row + 1) * px);
                        Vector3 b = origin + right * ((left + end + 1) * px) - up * ((row + 1) * px);
                        K.Face(m, a, b, b + up * px, a + up * px, c, n, false);
                        col = end;
                    }
                left += w + 1;
            }
        }

        /// <summary>Letters stacked downwards from <paramref name="top"/> (the centre of the first letter), one every pitch metres.</summary>
        static void TextDown(ComicMesh m, string text, Vector3 top, Vector3 right, Vector3 up, float px, float pitch, Color c)
        {
            for (int i = 0; i < text.Length; i++) Text(m, text[i].ToString(), top - up * (i * pitch), right, up, px, c);
        }

        /// <summary>Text on a lot's facade plane (outward o), facing the road, centred at (along a, up u).</summary>
        static void LText(ComicMesh m, CqLot l, string text, float a, float u, float o, float px, Color c) =>
            Text(m, text, l.P(a, u, o + .015f), new Vector3(0, 0, l.S < 0 ? 1 : -1), Vector3.up, px, c);

        /// <summary>A sign board on two posts beside the path, facing the rider (-Z): cream board, ink frame, text.</summary>
        static void PostSign(ComicMesh m, Vector3 p, string text, float px, Color board, Color ink, float height = 2.2f)
        {
            float w = TextCols(text) * px + .5f, h = 5f * px + .36f;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -w * .5f + .08f, w * .5f - .08f }) m.Box(p + V(x, height * .5f, .06f), V(.09f, height, .09f), MastDark);
            m.Box(p + V(0, height - h * .5f, 0), V(w + .1f, h + .1f, .06f), ink);
            m.Box(p + V(0, height - h * .5f, -.03f), V(w, h, .05f), board);
            Text(m, text, p + V(0, height - h * .5f, -.062f), Vector3.right, Vector3.up, px, ink);
        }

        // ================================================================================== trees and plants

        /// <summary>Jacaranda in November: a dark trunk forking low into spreading limbs, a broad airy dome of lavender-blue
        /// blossom (lumpy clouds in three tones, deeper underneath, a few sprigs of ferny green). petals: also scatters
        /// fallen flowers on the ground mesh.</summary>
        static void Jacaranda(ComicMesh m, Vector3 p, float s, int seed, ComicMesh petals = null, float petalY = 0f)
        {
            var r = new System.Random(seed * 977 + 13);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .06f, 0), .5f * s, .12f, Ink, 8, .85f);
            m.Surface = ComicSurface.Wood;
            Vector3 fork = p + V(F(r, -.2f, .2f) * s, 2.3f * s, F(r, -.2f, .2f) * s);
            Limb(m, p + V(0, .1f, 0), fork, .33f * s, .24f * s, JacBark, 7);
            float spin = F(r, 0f, 360f);
            const int limbs = 4;
            Vector3 centre = fork + V(0, 3.1f * s, 0);
            for (int i = 0; i < limbs; i++)
            {
                float a = (spin + i * 360f / limbs + F(r, -20f, 20f)) * Mathf.Deg2Rad;
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 mid = fork + dir * F(r, 1.1f, 1.5f) * s + V(0, F(r, 1.2f, 1.6f) * s, 0);
                Vector3 tip = mid + dir * F(r, 1.3f, 1.9f) * s + V(0, F(r, .7f, 1.2f) * s, 0);
                m.Surface = ComicSurface.Wood;
                Limb(m, fork, mid, .19f * s, .13f * s, JacBark, 5);
                Limb(m, mid, tip, .13f * s, .06f * s, JacBark, 5);
                m.Surface = ComicSurface.Foliage;
                float w = F(r, 2.5f, 3.2f) * s;
                Blob(m, tip + V(0, .45f * s, 0), V(w, w * .62f, w), i % 2 == 0 ? Jac1 : Jac2, 7, 4, a, .16f);
                Blob(m, tip + dir * .9f * s + V(0, -.15f * s, 0) + Vector3.Cross(Vector3.up, dir) * F(r, -.8f, .8f) * s, V(w * .62f, w * .42f, w * .62f), Jac0, 6, 3, a + 1f, .14f);
                Blob(m, Vector3.Lerp(tip, centre, .45f) + V(0, .9f * s, 0), V(w * .7f, w * .45f, w * .7f), Jac3, 6, 3, a + 2f, .14f);
            }
            m.Surface = ComicSurface.Foliage;
            Blob(m, centre + V(0, .2f * s, 0), V(3.4f * s, 2.1f * s, 3.4f * s), Jac2, 7, 4, spin, .16f);
            Blob(m, centre + V(.4f * s, 1.05f * s, -.3f * s), V(2f * s, 1.1f * s, 2f * s), Jac3, 6, 3, spin + 1f, .14f);
            Blob(m, fork + V(-1.1f * s, 1.3f * s, .8f * s), V(1.2f * s, .8f * s, 1.2f * s), JacLeaf, 5, 3, spin + 2f, .12f);
            if (petals == null) return;
            petals.Surface = ComicSurface.Plain;
            for (int i = 0; i < 26; i++)
            {
                float a = F(r, 0f, Mathf.PI * 2f), rad = Mathf.Sqrt(F(r, 0f, 1f)) * 3.6f * s + .3f, w = F(r, .1f, .28f), dz = F(r, .08f, .22f);
                Vector3 c = V(p.x + Mathf.Cos(a) * rad, petalY, p.z + Mathf.Sin(a) * rad);
                K.Face(petals, c + V(-w, 0, -dz), c + V(-w * .6f, 0, dz), c + V(w, 0, dz * .8f), c + V(w * .7f, 0, -dz), i % 3 == 0 ? Jac3 : i % 3 == 1 ? Jac2 : Jac1, Vector3.up, false);
            }
        }

        /// <summary>A gum tree (river red gum / lemon-scented): tall pale trunk with grey bark patches, forking limbs, open
        /// clumps of olive leaves high up (lumpy, lighter on top), sky showing between them.</summary>
        static void GumTree(ComicMesh m, Vector3 p, float h, int seed, bool ring = true)
        {
            var r = new System.Random(seed * 613 + 5);
            if (ring) { m.Surface = ComicSurface.PaintedMetal; m.Cylinder(p + V(0, .06f, 0), .34f + h * .02f, .12f, Ink, 8, .85f); }
            float tr = .2f + h * .016f;
            Vector3 lean = V(F(r, -.6f, .6f), 0, F(r, -.6f, .6f));
            Vector3 a = p, b = p + V(0, h * .36f, 0) + lean * .3f, c = b + V(0, h * .24f, 0) + lean * .6f;
            m.Surface = ComicSurface.Wood;
            Limb(m, a, b, tr, tr * .78f, GumTrunk, 7);
            Limb(m, b, c, tr * .78f, tr * .5f, GumTrunk, 6);
            m.Box(Vector3.Lerp(a, b, .45f) + V(tr * .55f, 0, 0), V(tr * .5f, h * .1f, tr * .9f), GumPatch);
            m.Box(Vector3.Lerp(a, b, .8f) + V(-tr * .5f, 0, tr * .2f), V(tr * .45f, h * .07f, tr * .8f), GumPatch);
            float spin = F(r, 0f, 360f);
            for (int i = 0; i < 4; i++)
            {
                float ang = (spin + i * 90f + F(r, -25f, 25f)) * Mathf.Deg2Rad;
                Vector3 dir = V(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                Vector3 from = i < 2 ? b : c;
                Vector3 tip = from + dir * F(r, h * .15f, h * .23f) + V(0, F(r, h * .14f, h * .24f), 0);
                m.Surface = ComicSurface.Wood;
                Limb(m, from, tip, tr * .42f, tr * .18f, GumTrunk, 5);
                m.Surface = ComicSurface.Foliage;
                float w = F(r, h * .2f, h * .27f);
                Blob(m, tip + V(0, w * .18f, 0), V(w, w * .62f, w), i % 2 == 0 ? GumLeaf : GumLeafDark, 7, 4, ang, .2f);
                Blob(m, tip + dir * w * .3f + V(0, w * .5f, 0), V(w * .62f, w * .4f, w * .62f), GumLeafLight, 6, 3, ang + 1f, .18f);
            }
            m.Surface = ComicSurface.Foliage;
            Blob(m, c + V(0, h * .2f, 0) + lean * .3f, V(h * .26f, h * .17f, h * .26f), GumLeaf, 7, 4, spin, .2f);
            Blob(m, c + V(h * .05f, h * .28f, -h * .03f) + lean * .3f, V(h * .15f, h * .09f, h * .15f), GumLeafLight, 6, 3, spin + 1f, .18f);
        }

        /// <summary>A big fig (Moreton Bay / Port Jackson): short buttressed trunk, heavy limbs, a huge dark dome of lumpy
        /// lobes, lighter on top.</summary>
        static void FigTree(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = new System.Random(seed * 211 + 3);
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, 1.4f * s, 0), .75f * s, 2.8f * s, FigBark, 8, .7f);
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 30f) * Mathf.Deg2Rad;
                m.Box(p + V(Mathf.Cos(a) * .7f * s, .45f * s, Mathf.Sin(a) * .7f * s), V(.3f * s, .9f * s, 1.1f * s), FigBark, Quaternion.Euler(0, -i * 90f - 30f + 90f, 0));
            }
            float spin = F(r, 0f, 6f);
            for (int i = 0; i < 3; i++)
            {
                float a = spin + i * 2.1f;
                Limb(m, p + V(0, 2.4f * s, 0), p + V(Mathf.Cos(a) * 2.6f * s, 4.4f * s, Mathf.Sin(a) * 2.6f * s), .34f * s, .2f * s, FigBark, 5);
            }
            m.Surface = ComicSurface.Foliage;
            Blob(m, p + V(0, 6f * s, 0), V(7.4f * s, 4.6f * s, 7.4f * s), FigDark, 8, 4, spin, .14f);
            for (int i = 0; i < 5; i++)
            {
                float a = spin + i * Mathf.PI * .4f + F(r, -.25f, .25f);
                Vector3 c = p + V(Mathf.Cos(a) * 3.5f * s, F(r, 4.3f, 5.6f) * s, Mathf.Sin(a) * 3.5f * s);
                Blob(m, c, V(4.6f * s, 3.2f * s, 4.6f * s), i % 2 == 0 ? FigMid : FigDark, 7, 4, a, .18f);
            }
            Blob(m, p + V(.8f * s, 8.1f * s, -.5f * s), V(4.2f * s, 2.2f * s, 4.2f * s), FigLight, 7, 4, spin + 1f, .16f);
            Blob(m, p + V(-1.8f * s, 7.3f * s, 1.2f * s), V(3f * s, 1.7f * s, 3f * s), FigMid, 6, 3, spin + 2f, .16f);
        }

        /// <summary>Bottlebrush (callistemon): a small weeping street tree, the red brushes hanging among the leaves.</summary>
        static void Bottlebrush(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = new System.Random(seed * 131 + 9);
            Color bark = Hex("6B5A4A"), leaf = Hex("5D7F45"), leafLight = Hex("7A9A58"), red = Hex("D8323A");
            m.Surface = ComicSurface.Wood;
            Vector3 fork = p + V(.1f * s, 1.5f * s, 0);
            Limb(m, p, fork, .14f * s, .1f * s, bark, 6);
            float spin = F(r, 0, 6f);
            for (int i = 0; i < 3; i++)
            {
                float a = spin + i * 2.1f + F(r, -.3f, .3f);
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a)), tip = fork + dir * .8f * s + V(0, 1f * s, 0);
                m.Surface = ComicSurface.Wood;
                Limb(m, fork, tip, .08f * s, .04f * s, bark, 4);
                m.Surface = ComicSurface.Foliage;
                Blob(m, tip + dir * .25f * s + V(0, .05f * s, 0), V(1.7f * s, 1.5f * s, 1.7f * s), i % 2 == 0 ? leaf : Hex("567A42"), 6, 4, a, .2f);
                m.Surface = ComicSurface.Plastic;
                for (int k = 0; k < 3; k++)
                {
                    float b = a + (k - 1) * .8f;
                    Vector3 q = tip + V(Mathf.Cos(b) * .8f * s, F(r, -.5f, .2f) * s, Mathf.Sin(b) * .8f * s);
                    m.Box(q, V(.09f, .3f, .09f), red, Quaternion.Euler(F(r, -30f, 30f), b * 57f, F(r, -30f, 30f)));
                }
            }
            m.Surface = ComicSurface.Foliage;
            Blob(m, fork + V(0, 1.7f * s, 0), V(1.8f * s, 1.3f * s, 1.8f * s), leafLight, 6, 4, spin + 1f, .2f);
        }

        /// <summary>Paperbark (melaleuca) for the river flats: leaning white trunks, a grey-green drooping crown.</summary>
        static void Paperbark(ComicMesh m, Vector3 p, float h, int seed)
        {
            var r = new System.Random(seed * 409 + 1);
            Color bark = Hex("EDE6D8"), leaf = Hex("6F8A5E"), leafLight = Hex("8FA676");
            float spin = F(r, 0f, 6f);
            for (int i = 0; i < 2; i++)
            {
                float a = spin + i * 2.4f;
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 mid = p + dir * .5f + V(0, h * .45f, 0), tip = mid + dir * .8f + V(0, h * .3f, 0);
                m.Surface = ComicSurface.Wood;
                Limb(m, p + dir * .15f, mid, .2f + h * .012f, .15f, bark);
                Limb(m, mid, tip, .15f, .08f, bark);
                m.Surface = ComicSurface.Foliage;
                Blob(m, tip + V(0, h * .05f, 0), V(h * .5f, h * .34f, h * .5f), i == 0 ? leaf : Hex("5E7A52"), 6, 3, a);
                Blob(m, tip + dir * .5f + V(0, h * .16f, 0), V(h * .3f, h * .2f, h * .3f), leafLight, 6, 3, a + 1f);
            }
        }

        /// <summary>Cotton palm (washingtonia), the tall thin palms of Beaufort St: slim ringed trunk, a small crown of fan
        /// leaves, a skirt of dry fronds under it.</summary>
        static void CottonPalm(ComicMesh m, Vector3 p, float h, int seed)
        {
            Color trunk = Hex("8E7458"), trunkDark = Hex("7A624A"), leaf = Hex("4F8A3A"), leafLight = Hex("6FA84A"), under = Hex("335E27");
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .06f, 0), .4f, .12f, Ink, 8);
            m.Surface = ComicSurface.Wood;
            int segs = 4;
            for (int i = 0; i < segs; i++)
                m.Cylinder(p + V(0, h * (i + .5f) / segs, 0), .24f - .03f * i, h / segs * .96f, i % 2 == 0 ? trunk : trunkDark, 6, .92f);
            Vector3 top = p + V(0, h, 0);
            m.Cylinder(top + V(0, -.5f, 0), .5f, 1.1f, Hex("A58A5C"), 6, .45f);                    // skirt of dry fronds
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < 8; i++)
            {
                float a = (i * 45f + seed * 19f) * Mathf.Deg2Rad, tilt = i % 2 == 0 ? .55f : .1f;
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a)), side = Vector3.Cross(Vector3.up, dir);
                Vector3 a0 = top + V(0, .2f, 0), a1 = a0 + dir * 1.1f + V(0, tilt * 1.2f, 0), a2 = a1 + dir * .9f + V(0, tilt * .3f - .45f, 0);
                Slab(m, a0, a1, side, .25f, 1.1f, .07f, i % 2 == 0 ? leaf : leafLight, under);
                Slab(m, a1, a2, side, 1.1f, .35f, .06f, i % 2 == 0 ? leafLight : leaf, under);
            }
        }

        /// <summary>Norfolk Island pine (the tall pines along Railway Parade in Maylands): a straight trunk and stacked
        /// star-shaped tiers of branches, shorter towards the top.</summary>
        static void NorfolkPine(ComicMesh m, Vector3 p, float h, int seed)
        {
            Color dark = Hex("2C5A36"), mid = Hex("3B6F42"), under = Hex("1C3826");
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, h * .5f, 0), .22f + h * .01f, h, Hex("6E5A47"), 6, .25f);
            const int tiers = 7;
            for (int i = 0; i < tiers; i++)
            {
                float t = i / (float)(tiers - 1), y = Mathf.Lerp(h * .3f, h * .93f, t), reach = Mathf.Lerp(h * .2f, h * .06f, t);
                m.Surface = ComicSurface.Foliage;
                for (int j = 0; j < 5; j++)
                {
                    float a = (seed * 31f + i * 37f + j * 72f) * Mathf.Deg2Rad;
                    Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a)), side = Vector3.Cross(Vector3.up, dir);
                    Vector3 a0 = p + V(0, y, 0), a1 = a0 + dir * reach + V(0, -reach * .14f, 0);
                    float w = .3f + reach * .3f;
                    K.Face(m, a0 - side * w * .3f, a1 - side * w * .5f, a1 + side * w * .5f, a0 + side * w * .3f, i % 2 == 0 ? dark : mid, Vector3.up);
                    K.Face(m, a0 - side * w * .3f + V(0, -.12f, 0), a1 - side * w * .5f + V(0, -.12f, 0), a1 + side * w * .5f + V(0, -.12f, 0), a0 + side * w * .3f + V(0, -.12f, 0), under, Vector3.down);
                }
            }
            m.Surface = ComicSurface.Foliage;
            m.Cylinder(p + V(0, h * .97f, 0), h * .025f + .12f, h * .09f, mid, 5, .2f);
        }

        /// <summary>A round shrub with flowers (roses, hydrangeas, agapanthus heads).</summary>
        static void FlowerBush(ComicMesh m, Vector3 p, float s, Color flower, int seed)
        {
            m.Surface = ComicSurface.Foliage;
            Blob(m, p + V(0, .45f * s, 0), V(1.1f * s, .9f * s, 1.1f * s), seed % 2 == 0 ? Hedge : Hex("5F8F45"), 6, 3, seed);
            m.Surface = ComicSurface.Plastic;
            for (int i = 0; i < 4; i++)
            {
                float a = seed * 1.3f + i * 1.7f;
                m.Box(p + V(Mathf.Cos(a) * .38f * s, (.62f + .12f * (i % 2)) * s, Mathf.Sin(a) * .38f * s), Vector3.one * .2f * s, flower, Quaternion.Euler(20f, a * 57f, 20f));
            }
        }

        /// <summary>A clipped hedge box in flat road space beyond the sidewalk (sheared with the road).</summary>
        static void HedgeRow(ComicMesh sloped, int s, float z0, float z1, float d0, float d1, float h, Color c)
        {
            sloped.Surface = ComicSurface.Foliage;
            K.SideBox(sloped, s, z0, z1, K.Kerb - .05f, K.Kerb + h, d0, d1, c);
        }

        /// <summary>A cheap background tree (no outline): trunk and two blobs.</summary>
        static void FarTree(ComicMesh m, Vector3 p, float h, Color leaf, bool gum = false)
        {
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, h * .3f, 0), .22f + h * .012f, h * .6f, gum ? GumTrunk : FigBark, 5, .7f);
            m.Surface = ComicSurface.Foliage;
            Blob(m, p + V(0, h * .68f, 0), V(h * .62f, h * .5f, h * .62f), leaf, 6, 3);
            Blob(m, p + V(h * .12f, h * .86f, h * .05f), V(h * .36f, h * .26f, h * .36f), Mix(leaf, Color.white, .14f), 5, 3);
        }

        // ================================================================================== roofs, windows, fences

        /// <summary>A hipped roof over a0..a1 by oFront..oBack (lot space): eaves at u0, ridge at u1 along the longer side.</summary>
        static void HipRoof(ComicMesh m, CqLot l, float a0, float a1, float u0, float u1, float oF, float oB, Color c)
        {
            Vector3 toRoad = new Vector3(-l.S, 0, 0);
            Vector3 e00 = l.P(a0, u0, oF), e10 = l.P(a1, u0, oF), e11 = l.P(a1, u0, oB), e01 = l.P(a0, u0, oB);
            float da = a1 - a0, dO = oF - oB;
            if (da >= dO)
            {
                Vector3 r0 = l.P(a0 + dO * .5f, u1, (oF + oB) * .5f), r1 = l.P(a1 - dO * .5f, u1, (oF + oB) * .5f);
                K.Face(m, e00, e10, r1, r0, c, toRoad + Vector3.up);
                K.Face(m, e01, e11, r1, r0, c, -toRoad + Vector3.up);
                K.TriFacing(m, e00, e01, r0, c, Vector3.back + Vector3.up);
                K.TriFacing(m, e10, e11, r1, c, Vector3.forward + Vector3.up);
            }
            else
            {
                Vector3 r0 = l.P((a0 + a1) * .5f, u1, oF - da * .5f), r1 = l.P((a0 + a1) * .5f, u1, oB + da * .5f);
                K.TriFacing(m, e00, e10, r0, c, toRoad + Vector3.up);
                K.TriFacing(m, e01, e11, r1, c, -toRoad + Vector3.up);
                K.Face(m, e00, e01, r1, r0, c, Vector3.back + Vector3.up);
                K.Face(m, e10, e11, r1, r0, c, Vector3.forward + Vector3.up);
            }
        }

        /// <summary>A gable facing the road: the gable wall triangle at oWall, two roof planes overhanging it by ov and
        /// running back to oBack, dark soffits, white bargeboards and a finial. pitch = rise per metre of half span.
        /// Returns the ridge height.</summary>
        static float GableRoof(ComicMesh m, CqLot l, float a0, float a1, float u0, float pitch, float oWall, float oBack, float ov, Color roof, Color gable, Color barge)
        {
            Vector3 toRoad = new Vector3(-l.S, 0, 0);
            float ac = (a0 + a1) * .5f, ua = u0 + (a1 - a0) * .5f * pitch, ur = ua + .14f, ue = u0 - ov * pitch + .14f, oF = oWall + ov;
            m.Surface = ComicSurface.Plaster;
            K.TriFacing(m, l.P(a0, u0, oWall), l.P(a1, u0, oWall), l.P(ac, ua, oWall), gable, toRoad);
            Vector3 fl = l.P(a0 - ov, ue, oF), fr = l.P(a1 + ov, ue, oF), fa = l.P(ac, ur, oF), bl = l.P(a0 - ov, ue, oBack), br = l.P(a1 + ov, ue, oBack), ba = l.P(ac, ur, oBack);
            m.Surface = ComicSurface.Metal;
            K.Face(m, fl, fa, ba, bl, roof, Vector3.back + Vector3.up);
            K.Face(m, fr, fa, ba, br, roof, Vector3.forward + Vector3.up);
            Color soffit = Mix(roof, Ink, .45f);
            Vector3 dn = V(0, -.1f, 0);
            K.Face(m, fl + dn, fa + dn, ba + dn, bl + dn, soffit, Vector3.forward + Vector3.down);
            K.Face(m, fr + dn, fa + dn, ba + dn, br + dn, soffit, Vector3.back + Vector3.down);
            m.Surface = ComicSurface.PaintedMetal;
            m.Beam(fl + toRoad * .03f, fa + toRoad * .03f, .16f, barge);
            m.Beam(fr + toRoad * .03f, fa + toRoad * .03f, .16f, barge);
            m.Cylinder(fa + V(0, .5f, 0) + toRoad * .03f, .07f, 1f, barge, 5);
            return ur;
        }

        /// <summary>A timber sash window on the facade plane o: painted frame, glass, meeting rail, a sill and a head; deep
        /// = proud jambs as well, so the glass sits in a real reveal.</summary>
        static void Sash(ComicMesh m, CqLot l, float a, float w, float u0, float u1, float o, Color frame, Color surround, bool deep = false, int panes = 1)
        {
            float h = w * .5f;
            m.Surface = ComicSurface.PaintedMetal;
            K.LQuad(m, l, a - h, a + h, u0, u1, o + .012f, frame);
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, a - h + .09f, a + h - .09f, u0 + .09f, u1 - .09f, o + .024f, Glass);
            m.Surface = ComicSurface.PaintedMetal;
            K.LQuad(m, l, a - h + .09f, a + h - .09f, (u0 + u1) * .5f - .035f, (u0 + u1) * .5f + .035f, o + .034f, frame, false);
            for (int i = 1; i < panes; i++)
            {
                float x = Mathf.Lerp(a - h, a + h, (float)i / panes);
                K.LQuad(m, l, x - .03f, x + .03f, u0 + .09f, u1 - .09f, o + .034f, frame, false);
            }
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, a - h - .1f, a + h + .1f, u0 - .13f, u0, o + .16f, o, surround);
            K.LBox(m, l, a - h - .1f, a + h + .1f, u1, u1 + .16f, o + .1f, o, surround);
            if (!deep) return;
            K.LBox(m, l, a - h - .1f, a - h, u0, u1, o + .1f, o, surround);
            K.LBox(m, l, a + h, a + h + .1f, u0, u1, o + .1f, o, surround);
        }

        /// <summary>A steel-framed window with a grid of glazing bars (inter-war shops, the Astor): ink frame, glass, bars.</summary>
        static void GridWindow(ComicMesh m, CqLot l, float a0, float a1, float u0, float u1, float o, int cols, int rows, Color frame, Color sill)
        {
            m.Surface = ComicSurface.PaintedMetal;
            K.LQuad(m, l, a0, a1, u0, u1, o + .012f, frame);
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, a0 + .08f, a1 - .08f, u0 + .08f, u1 - .08f, o + .024f, Glass);
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 1; i < cols; i++) { float x = Mathf.Lerp(a0, a1, (float)i / cols); K.LQuad(m, l, x - .025f, x + .025f, u0 + .08f, u1 - .08f, o + .034f, frame, false); }
            for (int i = 1; i < rows; i++) { float y = Mathf.Lerp(u0, u1, (float)i / rows); K.LQuad(m, l, a0 + .08f, a1 - .08f, y - .025f, y + .025f, o + .034f, frame, false); }
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, a0 - .08f, a1 + .08f, u0 - .12f, u0, o + .14f, o, sill);
        }

        /// <summary>A window on the lot's end facing the rider (-Z): frame and glass, o0..o1 across, u0..u1 up.</summary>
        static void EndWindow(ComicMesh m, CqLot l, float along, float o0, float o1, float u0, float u1, Color frame)
        {
            m.Surface = ComicSurface.PaintedMetal;
            K.Face(m, l.P(along - .012f, u0, o0), l.P(along - .012f, u0, o1), l.P(along - .012f, u1, o1), l.P(along - .012f, u1, o0), frame, Vector3.back);
            m.Surface = ComicSurface.Glass;
            float d = o0 > o1 ? -.09f : .09f;
            K.Face(m, l.P(along - .024f, u0 + .09f, o0 + d), l.P(along - .024f, u0 + .09f, o1 - d), l.P(along - .024f, u1 - .09f, o1 - d), l.P(along - .024f, u1 - .09f, o0 + d), Glass, Vector3.back);
        }

        /// <summary>Shopfront glazing between a0 and a1: ink frame, glass set in above a tiled stall riser, mullions, a
        /// lighter transom band of leadlight, and a recessed door bay in the middle when door is true.</summary>
        static void Shopfront(ComicMesh m, CqLot l, float a0, float a1, float u1, float o, Color riser, bool door, Color doorColour)
        {
            m.Surface = ComicSurface.PaintedMetal;
            K.LQuad(m, l, a0, a1, 0f, u1, o + .012f, Ink);
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, a0 + .06f, a1 - .06f, .02f, .55f, o + .08f, o, riser);
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, a0 + .1f, a1 - .1f, .6f, u1 - .7f, o + .024f, Glass);
            K.LQuad(m, l, a0 + .1f, a1 - .1f, u1 - .62f, u1 - .1f, o + .024f, GlassPale);
            m.Surface = ComicSurface.PaintedMetal;
            int n = Mathf.Max(2, Mathf.RoundToInt((a1 - a0) / 1.3f));
            for (int i = 1; i < n; i++) { float a = Mathf.Lerp(a0, a1, (float)i / n); K.LBox(m, l, a - .04f, a + .04f, .55f, u1 - .08f, o + .07f, o, Ink); }
            if (!door) return;
            float c = (a0 + a1) * .5f;
            K.LQuad(m, l, c - .62f, c + .62f, 0f, u1 - .7f, o + .04f, Ink);
            m.Surface = ComicSurface.Wood;
            K.LQuad(m, l, c - .5f, c + .5f, .04f, u1 - .8f, o + .05f, doorColour);
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, c - .34f, c + .34f, 1f, u1 - 1f, o + .06f, GlassDark);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, c + .3f, c + .38f, 1f, 1.25f, o + .12f, o + .05f, Mustard);
        }

        /// <summary>A flat cantilevered street awning: slab, ink soffit edge, a coloured fascia, two tie rods back to the wall.</summary>
        static void Awning(ComicMesh m, CqLot l, float a0, float a1, float u, float reach, Color top, Color fascia)
        {
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, a0, a1, u, u + .16f, reach, 0f, top);
            K.LBox(m, l, a0 - .02f, a1 + .02f, u - .3f, u + .2f, reach + .08f, reach - .06f, fascia);
            K.LBox(m, l, a0 - .02f, a1 + .02f, u - .36f, u - .3f, reach + .1f, reach - .08f, Ink);
            foreach (float a in new[] { a0 + .6f, a1 - .6f })
                m.Beam(l.P(a, u + .16f, reach - .3f), l.P(a, u + 1.9f, .02f), .05f, Ink);
        }

        /// <summary>A picket fence in flat road space (sheared with the road) on side s, dist beyond the sidewalk edge:
        /// posts with caps, two rails, pointed pickets (single quads facing the road). gates: z centres of 1.2 m openings.</summary>
        static void PicketFence(ComicMesh sloped, int s, float z0, float z1, float dist, float h, Color c, params float[] gates)
        {
            float x = s * (K.Edge + dist), xf = s * (K.Edge + dist - .04f);
            Vector3 toRoad = new Vector3(-s, 0, 0);
            bool Gate(float z) { foreach (float g in gates) if (Mathf.Abs(z - g) < .65f) return true; return false; }
            sloped.Surface = ComicSurface.Wood;
            for (float z = z0; z <= z1 + .01f; z += 2.4f)
                if (!Gate(z))
                {
                    sloped.Box(V(x, K.Kerb + (h + .15f) * .5f, z), V(.12f, h + .15f, .12f), c);
                    sloped.Box(V(x, K.Kerb + h + .2f, z), V(.17f, .06f, .17f), c);
                }
            foreach (float g in gates)
                foreach (float e in new[] { -.7f, .7f })
                {
                    sloped.Box(V(x, K.Kerb + (h + .35f) * .5f, g + e), V(.16f, h + .35f, .16f), c);
                    sloped.Box(V(x, K.Kerb + h + .42f, g + e), V(.22f, .08f, .22f), c);
                }
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                foreach (float y in new[] { .28f, h - .22f })
                    K.Face(sloped, V(xf + s * .02f, K.Kerb + y, z), V(xf + s * .02f, K.Kerb + y, e), V(xf + s * .02f, K.Kerb + y + .08f, e), V(xf + s * .02f, K.Kerb + y + .08f, z), Mix(c, Ink, .12f), toRoad, false);
            }
            for (float z = z0 + .14f; z < z1 - .1f; z += .26f)
            {
                if (Gate(z)) continue;
                float a = z - .07f, b = z + .07f, yb = K.Kerb + .1f, yt = K.Kerb + h - .1f;
                K.Face(sloped, V(xf, yb, a), V(xf, yb, b), V(xf, yt, b), V(xf, yt, a), c, toRoad);
                K.TriFacing(sloped, V(xf, yt, a), V(xf, yt, b), V(xf, yt + .1f, z), c, toRoad);
            }
        }

        /// <summary>A low masonry fence (limestone or brick) with a cap and piers, sheared with the road.</summary>
        static void LowWall(ComicMesh sloped, int s, float z0, float z1, float dist, float h, Color wall, Color cap, params float[] gates)
        {
            var cuts = new List<float> { z0 };
            foreach (float g in gates.OrderBy(g => g)) { cuts.Add(g - .7f); cuts.Add(g + .7f); }
            cuts.Add(z1);
            sloped.Surface = ComicSurface.Concrete;
            for (int i = 0; i + 1 < cuts.Count; i += 2)
            {
                float a = cuts[i], b = cuts[i + 1];
                if (b - a < .2f) continue;
                K.SideBox(sloped, s, a, b, -1f, K.Kerb + h, dist, dist + .32f, wall);
                K.SideBox(sloped, s, a, b, K.Kerb + h, K.Kerb + h + .09f, dist - .04f, dist + .36f, cap);
                foreach (float e in new[] { a, b })
                {
                    sloped.Box(V(s * (K.Edge + dist + .16f), K.Kerb + (h + .3f) * .5f - .2f, e), V(.46f, h + .7f, .46f), wall);
                    sloped.Box(V(s * (K.Edge + dist + .16f), K.Kerb + h + .2f, e), V(.56f, .1f, .56f), cap);
                }
            }
        }

        /// <summary>A tubular steel fence (vertical bars as single quads facing the road), sheared with the road. x is the
        /// fence line, y the ground it stands on.</summary>
        static void BarFence(ComicMesh sloped, float x, float z0, float z1, float y, float h, Color c, int facing, float pitch = .3f)
        {
            Vector3 n = new Vector3(facing, 0, 0);
            sloped.Surface = ComicSurface.PaintedMetal;
            for (float z = z0; z <= z1 + .01f; z += 2.5f) sloped.Box(V(x, y + h * .5f, z), V(.09f, h, .09f), c);
            for (float z = z0; z < z1 - .01f; z += 5f)
            {
                float e = Mathf.Min(z1, z + 5f);
                sloped.Box(V(x, y + h - .06f, (z + e) * .5f), V(.06f, .06f, e - z), c);
                sloped.Box(V(x, y + .16f, (z + e) * .5f), V(.06f, .06f, e - z), c);
            }
            float xf = x + facing * .035f;
            for (float z = z0 + pitch * .5f; z < z1; z += pitch)
                K.Face(sloped, V(xf, y + .18f, z - .02f), V(xf, y + .18f, z + .02f), V(xf, y + h - .08f, z + .02f), V(xf, y + h - .08f, z - .02f), c, n, false);
        }

        /// <summary>A chain-link fence: grey posts, top rail and a pale mesh panel (one thin translucent-looking quad).</summary>
        static void MeshFence(ComicMesh sloped, float x, float z0, float z1, float y, float h, int facing)
        {
            Vector3 n = new Vector3(facing, 0, 0);
            sloped.Surface = ComicSurface.PaintedMetal;
            for (float z = z0; z <= z1 + .01f; z += 2.5f) sloped.Box(V(x, y + h * .5f, z), V(.08f, h, .08f), MastDark);
            for (float z = z0; z < z1 - .01f; z += 5f)
            {
                float e = Mathf.Min(z1, z + 5f);
                sloped.Box(V(x, y + h - .04f, (z + e) * .5f), V(.05f, .05f, e - z), MastDark);
                foreach (float t in new[] { .33f, .66f })
                    K.Face(sloped, V(x + facing * .03f, y + h * t, z), V(x + facing * .03f, y + h * t, e), V(x + facing * .03f, y + h * t + .03f, e), V(x + facing * .03f, y + h * t + .03f, z), Mast, n, false);
            }
        }

        // ================================================================================== street furniture

        static void Bench(ComicMesh m, Vector3 p, float yaw, Color timber)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.Wood;
            m.Box(p + q * V(0, .45f, 0), V(1.8f, .08f, .45f), timber, q);
            m.Box(p + q * V(0, .8f, .22f), V(1.8f, .35f, .07f), timber, q);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.75f, .75f }) m.Box(p + q * V(x, .4f, .05f), V(.08f, .8f, .5f), Ink, q);
        }

        /// <summary>A parked car, nose towards +Z at yaw 0: body, cabin with dark glass, ink bumpers, wheels, lights.</summary>
        static void ParkedCar(ComicMesh m, Vector3 p, float yaw, Color c)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            void B(Vector3 at, Vector3 size, Color col, ComicSurface s) { m.Surface = s; m.Box(p + q * at, size, col, q); }
            B(V(0, .62f, 0), V(1.75f, .6f, 4.1f), c, ComicSurface.PaintedMetal);
            B(V(0, 1.18f, -.25f), V(1.55f, .55f, 2.2f), c, ComicSurface.PaintedMetal);
            B(V(0, 1.18f, -.25f), V(1.6f, .42f, 2f), GlassDark, ComicSurface.Glass);
            B(V(0, .45f, 2.06f), V(1.8f, .22f, .1f), Ink, ComicSurface.PaintedMetal);
            B(V(0, .45f, -2.06f), V(1.8f, .22f, .1f), Ink, ComicSurface.PaintedMetal);
            B(V(0, .78f, -2.07f), V(1.5f, .12f, .06f), Hex("D8323A"), ComicSurface.Plastic);
            foreach (float z in new[] { 1.3f, -1.3f }) foreach (float x in new[] { -.82f, .82f }) B(V(x, .32f, z), V(.24f, .62f, .62f), Ink, ComicSurface.Rubber);
        }

        /// <summary>A wheelie bin (240 L): dark green body, coloured lid (yellow = recycling, red = rubbish), wheels.</summary>
        static void WheelieBin(ComicMesh m, Vector3 p, float yaw, Color lid)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.Plastic;
            m.Box(p + q * V(0, .52f, 0), V(.56f, .9f, .62f), Hex("2F5D3A"), q);
            m.Box(p + q * V(0, 1.01f, .02f), V(.62f, .1f, .7f), lid, q);
            m.Box(p + q * V(0, 1f, .38f), V(.56f, .07f, .08f), Ink, q);
            m.Surface = ComicSurface.Rubber;
            foreach (float x in new[] { -.26f, .26f }) m.Box(p + q * V(x, .12f, .28f), V(.08f, .24f, .24f), Ink, q);
        }

        /// <summary>A letterbox on a post (suburban) or a brick pier box.</summary>
        static void Letterbox(ComicMesh m, Vector3 p, Color c, bool brick)
        {
            if (brick)
            {
                m.Surface = ComicSurface.Brick;
                m.Box(p + V(0, .55f, 0), V(.42f, 1.1f, .42f), c);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(p + V(0, 1.14f, 0), V(.5f, .08f, .5f), Cream);
                m.Box(p + V(0, .8f, 0), V(.44f, .1f, .26f), Ink);
                return;
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, .5f, 0), V(.07f, 1f, .07f), White);
            m.Box(p + V(0, 1.1f, 0), V(.26f, .26f, .42f), c);
            m.Box(p + V(0, 1.25f, 0), V(.3f, .05f, .46f), Ink);
        }

        /// <summary>The Hills Hoist: the rotary clothes line of every Perth back yard, with washing pegged on it.</summary>
        static void HillsHoist(ComicMesh m, Vector3 p, float yaw, int seed)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            Color steel = Hex("B9C0C5");
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 1.05f, 0), .05f, 2.1f, steel, 6);
            Vector3 hub = p + V(0, 1.85f, 0);
            var tips = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                tips[i] = hub + q * (Quaternion.Euler(0, i * 90f, 0) * V(1.55f, .3f, 0));
                m.Beam(hub, tips[i], .045f, steel);
            }
            for (int i = 0; i < 4; i++)
            {
                m.Beam(tips[i], tips[(i + 1) % 4], .03f, steel);
                m.Beam(Vector3.Lerp(hub, tips[i], .6f), Vector3.Lerp(hub, tips[(i + 1) % 4], .6f), .025f, steel);
            }
            m.Box(p + V(0, 1f, 0) + q * V(.12f, 0, 0), V(.2f, .08f, .06f), Ink, q);                  // the winder handle
            Color[] washing = { White, Hex("E2463A"), Hex("2F6FB0"), Hex("F6C543"), Hex("7FC6B8") };
            m.Surface = ComicSurface.Fabric;
            for (int i = 0; i < 3; i++)
            {
                Vector3 a = tips[i], b = tips[(i + 1) % 4], c = Vector3.Lerp(a, b, .3f + .2f * i), along = (b - a).normalized;
                float w = .55f + .1f * ((seed + i) % 3), h = .5f + .12f * ((seed + i) % 2);
                m.Box(c + V(0, -h * .5f - .02f, 0), V(.03f, h, w), Pick(washing, seed + i), Quaternion.LookRotation(along));
            }
        }

        /// <summary>Bollard of the shared path: yellow steel with a reflective red band.</summary>
        static void Bollard(ComicMesh m, Vector3 p, Color c)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .05f, 0), .16f, .1f, Ink, 8);
            m.Cylinder(p + V(0, .55f, 0), .09f, 1f, c, 8);
            m.Cylinder(p + V(0, .86f, 0), .1f, .12f, Hex("D8323A"), 8);
            m.Cylinder(p + V(0, 1.07f, 0), .1f, .06f, Ink, 8);
        }

        /// <summary>The ornate street lamp of Beaufort St and Eighth Ave: a tall pole, two swept arms with lanterns, a
        /// sunburst fan at the top (blue in Mt Lawley, black in Maylands).</summary>
        static void SunburstLamp(ComicMesh m, Vector3 p, Color c, bool acrossRoad)
        {
            Quaternion q = Quaternion.Euler(0, acrossRoad ? 0f : 90f, 0);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .25f, 0), .2f, .5f, c, 8, .8f);
            m.Cylinder(p + V(0, 4f, 0), .09f, 7.6f, c, 8, .7f);
            Vector3 top = p + V(0, 7.6f, 0);
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 a = top + V(0, -.5f, 0), b = top + q * V(s * .9f, .5f, 0), e = top + q * V(s * 1.9f, .35f, 0);
                m.Beam(a, b, .07f, c);
                m.Beam(b, e, .07f, c);
                m.Box(e + V(0, -.16f, 0), V(.36f, .2f, .36f), Cream);
                m.Box(e + V(0, -.03f, 0), V(.44f, .08f, .44f), c);
            }
            for (int i = 0; i < 5; i++)
            {
                float a = (30f + i * 30f) * Mathf.Deg2Rad;
                m.Beam(top, top + q * V(Mathf.Cos(a) * .75f, Mathf.Sin(a) * .75f + .1f, 0), .045f, c);
            }
            for (int i = 0; i < 4; i++)
            {
                float a0 = (30f + i * 30f) * Mathf.Deg2Rad, a1 = (60f + i * 30f) * Mathf.Deg2Rad;
                m.Beam(top + q * V(Mathf.Cos(a0) * .75f, Mathf.Sin(a0) * .75f + .1f, 0), top + q * V(Mathf.Cos(a1) * .75f, Mathf.Sin(a1) * .75f + .1f, 0), .04f, c);
            }
        }

        /// <summary>A plain suburban street light: grey pole with one outreach arm and a flat lantern.</summary>
        static void StreetLight(ComicMesh m, Vector3 p, int towards)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .15f, 0), .18f, .3f, MastDark, 8, .9f);
            m.Cylinder(p + V(0, 3.9f, 0), .08f, 7.5f, Mast, 8, .75f);
            m.Beam(p + V(0, 7.6f, 0), p + V(towards * 1.6f, 7.9f, 0), .07f, Mast);
            m.Box(p + V(towards * 1.75f, 7.84f, 0), V(.6f, .12f, .3f), Cream);
            m.Box(p + V(towards * 1.75f, 7.93f, 0), V(.66f, .07f, .34f), MastDark);
        }

        /// <summary>A timber power pole with a cross-arm, insulators and a transformer can (suburban streets).</summary>
        static void PowerPole(ComicMesh m, Vector3 p, bool transformer)
        {
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, 4.4f, 0), .16f, 8.8f, Hex("7A6652"), 6, .7f);
            m.Box(p + V(0, 8.1f, 0), V(2.2f, .12f, .12f), Hex("6B5846"));
            m.Surface = ComicSurface.Plastic;
            foreach (float x in new[] { -1f, -.4f, .4f, 1f }) m.Box(p + V(x, 8.24f, 0), V(.09f, .16f, .09f), White);
            if (!transformer) return;
            m.Surface = ComicSurface.Metal;
            m.Cylinder(p + V(.36f, 6.6f, 0), .26f, .8f, Mast, 6);
        }

        // ================================================================================== the railway (scenery only)

        /// <summary>Where the two tracks run beside the road in one chunk: offsets (metres beyond the sidewalk edge on side
        /// S) of the track centres at z = 0 and z = Length, the rail-head level relative to the road, and the ballast bed.</summary>
        sealed class Rail
        {
            public int S = -1;
            public float Level = .12f, A0 = 4.4f, A1 = 4.4f, B0 = 8.8f, B1 = 8.8f, BedFrom = 1.5f, BedTo = 12f, Len = 40f;
            static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
            public float A(float z) => Mathf.Lerp(A0, A1, Smooth(z / Len));
            public float B(float z) => Mathf.Lerp(B0, B1, Smooth(z / Len));
            public float X(float u) => S * (K.Edge + u);
        }

        /// <summary>Ballast bed, concrete sleepers and four rails from z0 to z1 (flat road space, sheared on write; in the
        /// road mesh: no outline).</summary>
        static void Tracks(CqChunkDraft d, Rail rl, float z0, float z1)
        {
            ComicMesh m = d.Road;
            float yb = rl.Level - .2f;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(rl.X(rl.BedFrom), yb, z), V(rl.X(rl.BedFrom), yb, e), V(rl.X(rl.BedTo), yb, e), V(rl.X(rl.BedTo), yb, z), Ballast, Vector3.up, false);
                // shoulders down to the surrounding ground
                K.Face(m, V(rl.X(rl.BedFrom - .7f), yb - .35f, z), V(rl.X(rl.BedFrom - .7f), yb - .35f, e), V(rl.X(rl.BedFrom), yb, e), V(rl.X(rl.BedFrom), yb, z), BallastDark, Vector3.up, false);
                K.Face(m, V(rl.X(rl.BedTo), yb, z), V(rl.X(rl.BedTo), yb, e), V(rl.X(rl.BedTo + .7f), yb - .35f, e), V(rl.X(rl.BedTo + .7f), yb - .35f, z), BallastDark, Vector3.up, false);
                for (int t = 0; t < 2; t++)
                {
                    float c0 = t == 0 ? rl.A(z) : rl.B(z), c1 = t == 0 ? rl.A(e) : rl.B(e);
                    foreach (float g in new[] { -.55f, .55f })
                    {
                        float xa = rl.X(c0 + g), xb = rl.X(c1 + g), w = .05f, y1 = rl.Level, y0 = yb + .02f;
                        m.Surface = ComicSurface.Metal;
                        K.Face(m, V(xa - w, y1, z), V(xb - w, y1, e), V(xb + w, y1, e), V(xa + w, y1, z), RailTop, Vector3.up, false);
                        K.Face(m, V(xa - w, y0, z), V(xb - w, y0, e), V(xb - w, y1, e), V(xa - w, y1, z), RailRust, Vector3.left, false);
                        K.Face(m, V(xa + w, y0, z), V(xb + w, y0, e), V(xb + w, y1, e), V(xa + w, y1, z), RailRust, Vector3.right, false);
                    }
                }
            }
            m.Surface = ComicSurface.Concrete;
            for (float z = z0 + .4f; z < z1 - .2f; z += 1.25f)
                for (int t = 0; t < 2; t++)
                {
                    float c = rl.X(t == 0 ? rl.A(z) : rl.B(z));
                    K.Face(m, V(c - 1.15f, yb + .03f, z - .13f), V(c - 1.15f, yb + .03f, z + .13f), V(c + 1.15f, yb + .03f, z + .13f), V(c + 1.15f, yb + .03f, z - .13f), Sleeper, Vector3.up, false);
                }
        }

        /// <summary>One overhead-wire structure at z: a galvanised mast on each side of the pair of tracks, each with a
        /// cantilever (tube, diagonal stay, insulators, registration arm) reaching over its track. Sheared with the road.</summary>
        static void Gantry(ComicMesh sloped, Rail rl, float z, bool nearMast = true, bool farMast = true)
        {
            sloped.Surface = ComicSurface.PaintedMetal;
            for (int t = 0; t < 2; t++)
            {
                if (t == 0 && !nearMast || t == 1 && !farMast) continue;
                float track = t == 0 ? rl.A(z) : rl.B(z), u = t == 0 ? track - 2.5f : track + 2.5f;
                float x = rl.X(u), xt = rl.X(track), y = rl.Level - .3f, dir = Mathf.Sign(xt - x);
                sloped.Box(V(x, y + .25f, z), V(.6f, .5f, .6f), ConcreteDark);
                sloped.Box(V(x, y + 3.9f, z), V(.24f, 7.3f, .2f), Mast);
                sloped.Box(V(x, y + 7.6f, z), V(.3f, .1f, .26f), MastDark);
                Vector3 root = V(x + dir * .12f, rl.Level + 6f, z), tip = V(xt + dir * .5f, rl.Level + 6.15f, z);
                sloped.Beam(root, tip, .09f, Mast);
                sloped.Beam(V(x + dir * .12f, rl.Level + 7.2f, z), Vector3.Lerp(root, tip, .8f), .06f, Mast);
                sloped.Beam(V(x + dir * .12f, rl.Level + 5.2f, z), V(xt - dir * .1f, rl.Level + 5.25f, z), .06f, Mast);
                sloped.Surface = ComicSurface.Plastic;
                sloped.Box(V(x + dir * .5f, rl.Level + 6.03f, z), V(.42f, .2f, .2f), Hex("8A4B34"));
                sloped.Box(V(x + dir * .5f, rl.Level + 5.21f, z), V(.42f, .18f, .18f), Hex("8A4B34"));
                sloped.Surface = ComicSurface.PaintedMetal;
                sloped.Box(V(xt, rl.Level + 5.62f, z), V(.05f, .95f, .05f), MastDark);                 // dropper to the contact wire
            }
        }

        /// <summary>Contact and catenary wires over both tracks (road mesh: thin, no outline, sheared with the grade).</summary>
        static void Wires(CqChunkDraft d, Rail rl, float z0, float z1)
        {
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            Color wire = Hex("4A5560");
            for (float z = z0; z < z1 - .01f; z += 5f)
            {
                float e = Mathf.Min(z1, z + 5f);
                for (int t = 0; t < 2; t++)
                {
                    float xa = rl.X(t == 0 ? rl.A(z) : rl.B(z)), xb = rl.X(t == 0 ? rl.A(e) : rl.B(e));
                    foreach (float h in new[] { 5.15f, 6.1f })
                    {
                        float y = rl.Level + h;
                        K.Face(m, V(xa - .03f, y, z), V(xb - .03f, y, e), V(xb + .03f, y, e), V(xa + .03f, y, z), wire, Vector3.up, false);
                        K.Face(m, V(xa, y - .03f, z), V(xb, y - .03f, e), V(xb, y + .03f, e), V(xa, y + .03f, z), wire, Vector3.right, false);
                        K.Face(m, V(xa, y - .03f, z), V(xb, y - .03f, e), V(xb, y + .03f, e), V(xa, y + .03f, z), wire, Vector3.left, false);
                    }
                }
            }
        }

        /// <summary>A suburban electric train (stylised Transperth A-series / B-series, no branding): silver cars with a
        /// dark window band, a green stripe along the roof edge, doors, a grey roof with air-conditioning pods and a
        /// pantograph, ink underframe and bogies; the cab ends are white with a black windscreen mask (yellowFront: the
        /// older sets' yellow panel). Rear at z, running towards +Z; x = track centre, y = rail level.</summary>
        static void Train(ComicMesh m, float x, float y, float z, int cars, bool yellowFront)
        {
            const float L = 17.6f, W = 2.86f, gap = .7f;
            for (int i = 0; i < cars; i++)
            {
                float zc = z + L * .5f + i * (L + gap);
                Vector3 c = V(x, y, zc);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(c + V(0, .62f, 0), V(W - .5f, .6f, L - 1f), Ink);
                foreach (float bz in new[] { -L * .32f, L * .32f }) m.Box(c + V(0, .34f, bz), V(W - .3f, .56f, 2.8f), Charcoal);
                m.Surface = ComicSurface.Metal;
                m.Box(c + V(0, 2.32f, 0), V(W, 2.8f, L), TrainSilver);
                m.Box(c + V(0, 3.86f, 0), V(W - .5f, .3f, L - .6f), GalvDark);
                foreach (float az in new[] { -L * .25f, L * .25f }) m.Box(c + V(0, 4.12f, az), V(1.7f, .26f, 2.6f), Mast);
                foreach (int s in new[] { -1, 1 })
                {
                    float xs = s * (W * .5f + .012f);
                    Vector3 n = new Vector3(s, 0, 0);
                    m.Surface = ComicSurface.Glass;
                    K.Face(m, c + V(xs, 2.25f, -L * .5f + .5f), c + V(xs, 2.25f, L * .5f - .5f), c + V(xs, 3.15f, L * .5f - .5f), c + V(xs, 3.15f, -L * .5f + .5f), GlassDark, n);
                    m.Surface = ComicSurface.PaintedMetal;
                    K.Face(m, c + V(xs, 3.46f, -L * .5f), c + V(xs, 3.46f, L * .5f), c + V(xs, 3.7f, L * .5f), c + V(xs, 3.7f, -L * .5f), TrainGreen, n, false);
                    K.Face(m, c + V(xs, 1f, -L * .5f), c + V(xs, 1f, L * .5f), c + V(xs, 1.16f, L * .5f), c + V(xs, 1.16f, -L * .5f), TrainGreenDark, n, false);
                    foreach (float dz in new[] { -L * .3f, 0f, L * .3f })
                    {
                        float xd = s * (W * .5f + .024f);
                        K.Face(m, c + V(xd, 1.05f, dz - .75f), c + V(xd, 1.05f, dz + .75f), c + V(xd, 3.3f, dz + .75f), c + V(xd, 3.3f, dz - .75f), yellowFront ? TrainSilver : TrainGreen, n);
                        m.Surface = ComicSurface.Glass;
                        K.Face(m, c + V(s * (W * .5f + .036f), 2.2f, dz - .55f), c + V(s * (W * .5f + .036f), 2.2f, dz + .55f), c + V(s * (W * .5f + .036f), 3.1f, dz + .55f), c + V(s * (W * .5f + .036f), 3.1f, dz - .55f), GlassDark, n, false);
                        m.Surface = ComicSurface.PaintedMetal;
                        K.Face(m, c + V(s * (W * .5f + .04f), 1.1f, dz - .02f), c + V(s * (W * .5f + .04f), 1.1f, dz + .02f), c + V(s * (W * .5f + .04f), 3.25f, dz + .02f), c + V(s * (W * .5f + .04f), 3.25f, dz - .02f), Ink, n, false);
                    }
                }
                if (i > 0) { m.Surface = ComicSurface.Rubber; m.Box(V(x, y + 2.2f, zc - L * .5f - gap * .5f), V(W - .7f, 2.4f, gap + .1f), Charcoal); }
            }
            // cab ends
            float zf = z + cars * L + (cars - 1) * gap;
            foreach (int e in new[] { -1, 1 })
            {
                float ze = e < 0 ? z : zf;
                Vector3 n = new Vector3(0, 0, e);
                Vector3 c = V(x, y, ze + e * .014f);
                m.Surface = ComicSurface.PaintedMetal;
                K.Face(m, c + V(-W * .5f + .1f, 1f, 0), c + V(W * .5f - .1f, 1f, 0), c + V(W * .5f - .1f, 3.7f, 0), c + V(-W * .5f + .1f, 3.7f, 0), White, n);
                if (yellowFront) K.Face(m, c + V(-W * .5f + .1f, 1f, e * .012f), c + V(W * .5f - .1f, 1f, e * .012f), c + V(W * .5f - .1f, 2.05f, e * .012f), c + V(-W * .5f + .1f, 2.05f, e * .012f), TrainYellow, n);
                m.Surface = ComicSurface.Glass;
                K.Face(m, c + V(-W * .5f + .3f, 2.15f, e * .024f), c + V(W * .5f - .3f, 2.15f, e * .024f), c + V(W * .5f - .3f, 3.35f, e * .024f), c + V(-W * .5f + .3f, 3.35f, e * .024f), Ink, n);
                m.Surface = ComicSurface.Plastic;
                foreach (int s in new[] { -1, 1 })
                {
                    m.Box(c + V(s * .95f, 1.55f, e * .04f), V(.3f, .2f, .06f), e > 0 ? Cream : Hex("D8323A"));
                    m.Box(c + V(s * .95f, 3.5f, e * .04f), V(.22f, .12f, .06f), e > 0 ? Cream : Hex("D8323A"));
                }
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(c + V(0, .85f, e * .2f), V(.5f, .3f, .5f), Charcoal);                                  // coupler
            }
            // pantograph on the first car
            Vector3 pan = V(x, y + 4.02f, z + L * .5f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Beam(pan + V(0, 0, -.9f), pan + V(0, .6f, 0), .06f, Ink);
            m.Beam(pan + V(0, .6f, 0), pan + V(0, 1.1f, -.8f), .06f, Ink);
            m.Box(pan + V(0, 1.12f, -.8f), V(1.5f, .06f, .12f), Ink);
        }

        /// <summary>The far side of the railway: a second row of roofs and trees across the tracks (no outline).</summary>
        static void DeepSuburb(CqChunkDraft d, int s, float z0, float z1, float from, int seed, bool tall = false)
        {
            ComicMesh m = d.Deep;
            var r = new System.Random(seed * 31 + 7);
            Color[] walls = { Cream, White, BrickCream, Hex("D9CBB0"), BrickSalmon, Hex("C9D6DA") };
            Color[] roofs = { Tile, RoofRed, Galv, TileBrown, GalvDark, RoofGreen };
            for (float z = z0 + 1f; z < z1 - 5f; z += F(r, 11f, 14f))
            {
                float w = F(r, 9f, 12f), len = F(r, 8f, 10f), h = tall ? F(r, 6.5f, 9.5f) : F(r, 3.4f, 4.4f), x = s * (K.Edge + from + w * .5f + F(r, 0f, 5f));
                float y = d.Ground(z + len * .5f) - 1.5f;
                m.Surface = ComicSurface.Plaster;
                m.Box(V(x, y + (h + 1.5f) * .5f, z + len * .5f), V(w, h + 1.5f, len), Pick(walls, r.Next(99)));
                m.Surface = ComicSurface.Metal;
                m.Cylinder(V(x, y + h + 1.5f + 1.1f, z + len * .5f), Mathf.Max(w, len) * .74f, 2.2f, Pick(roofs, r.Next(99)), 4, .12f, Quaternion.Euler(0, 45, 0));
            }
            Color[] leaves = { FigMid, GumLeaf, Hedge, GumLeafDark, Jac2 };
            for (float z = z0 + 3f; z < z1; z += F(r, 6f, 10f))
                FarTree(m, V(s * (K.Edge + from + F(r, -2f, 22f)), d.Ground(z) - .5f, z), F(r, 7f, 12f), Pick(leaves, r.Next(99)), r.Next(3) == 0);
        }

        // ================================================================================== prefabs

        /// <summary>An animal walker (kind Person, animal, never punchable). Facing -Z, feet at the pivot.</summary>
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

        static ComicMesh Make(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }

        static Bounds KeepOutBox(CqChunkDraft d, int s, float z0, float z1)
        {
            float yTop = d.Y(z0) + 5f, yBottom = d.Y(z1) - 2f;
            return new Bounds(new Vector3(s * (K.Half + K.Side * .5f + .3f), (yTop + yBottom) * .5f, (z0 + z1) * .5f), new Vector3(K.Side + .6f, yTop - yBottom, z1 - z0));
        }

        /// <summary>A plain collider box (no Obstacle) beyond the sidewalk on side s: nobody is placed inside buildings or gardens.</summary>
        static void SideWall(CqChunkDraft d, int s, float z0, float z1, float depth = 13f, float height = 5f) =>
            d.Walls.Add(new Bounds(new Vector3(s * (K.Edge + .4f + depth * .5f), d.Y((z0 + z1) * .5f) + height * .5f - 1f, (z0 + z1) * .5f), new Vector3(depth, height + 2f, z1 - z0)));

        /// <summary>A magpie (the black-and-white bird of every Perth verge; November is swooping season).</summary>
        static ComicMesh MagpieMesh()
        {
            var a = new ComicMesh { Surface = ComicSurface.Plastic };
            Color black = Hex("1E2228"), white = Hex("F2F2EE"), beak = Hex("B9C3CC");
            a.Box(V(0, .27f, .02f), V(.2f, .2f, .4f), black, Quaternion.Euler(-12f, 0, 0));
            a.Box(V(0, .36f, .12f), V(.21f, .08f, .2f), white, Quaternion.Euler(-12f, 0, 0));
            a.Box(V(0, .42f, -.2f), V(.17f, .17f, .18f), black);
            a.Box(V(0, .5f, -.14f), V(.175f, .06f, .1f), white);
            a.Box(V(0, .41f, -.34f), V(.05f, .05f, .14f), beak);
            a.Box(V(0, .4f, -.42f), V(.04f, .04f, .04f), black);
            a.Box(V(0, .24f, .3f), V(.14f, .05f, .26f), black, Quaternion.Euler(18f, 0, 0));
            a.Box(V(0, .2f, .4f), V(.13f, .04f, .08f), white, Quaternion.Euler(18f, 0, 0));
            foreach (int s in new[] { -1, 1 })
            {
                a.Box(V(s * .11f, .29f, .06f), V(.03f, .16f, .32f), black, Quaternion.Euler(-12f, 0, 0));
                a.Box(V(s * .115f, .31f, .02f), V(.03f, .06f, .16f), white, Quaternion.Euler(-12f, 0, 0));
                a.Box(V(s * .09f, .45f, -.26f), V(.03f, .04f, .04f), Hex("B5562F"));
                a.Box(V(s * .05f, .09f, 0), V(.03f, .18f, .03f), black);
                a.Box(V(s * .05f, .015f, -.04f), V(.07f, .03f, .12f), black);
            }
            return a;
        }
    }
}
