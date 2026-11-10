using System;
using System.Collections.Generic;
using System.Linq;
using CityScooter.EditorTools.Art;
using CityScooter.Rendering;
using CityScooter.Track;
using UnityEngine;
using K = CityScooter.EditorTools.Districts.CbdQuayKit;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>A facade plane: origin O, R along the wall (to the viewer's right), U up, N out of the wall towards the
    /// viewer. Everything on a wall (windows, arches, verandahs, lettering) is placed as (u along, v up, o out).</summary>
    internal struct E4Plane
    {
        public Vector3 O, R, U, N;
        public E4Plane(Vector3 origin, Vector3 right, Vector3 normal) { O = origin; R = right.normalized; N = normal.normalized; U = Vector3.up; }
        /// <summary>The road-facing wall of a lot: u runs along +Z from Z0, o is towards the road.</summary>
        public static E4Plane Lot(CqLot l) => new E4Plane(l.S < 0 ? l.P(0, 0, 0) : l.P(l.W, 0, 0), l.S < 0 ? Vector3.forward : Vector3.back, new Vector3(-l.S, 0, 0));
        /// <summary>The end wall of a lot that faces the oncoming rider (-Z): u runs along +X, 0..Depth.</summary>
        public static E4Plane LotNear(CqLot l) => new E4Plane(new Vector3(l.S < 0 ? -(l.Line + l.Depth) : l.Line, l.Base, l.Z0), Vector3.right, Vector3.back);
        /// <summary>The end wall of a lot that faces away (+Z): u runs along -X, 0..Depth.</summary>
        public static E4Plane LotFar(CqLot l) => new E4Plane(new Vector3(l.S < 0 ? -l.Line : l.Line + l.Depth, l.Base, l.Z1), Vector3.left, Vector3.forward);
        public Vector3 P(float u, float v, float o = 0f) => O + R * u + U * v + N * o;
        public Quaternion Rot => Quaternion.LookRotation(N, U);
        /// <summary>The same wall moved: du along, dv up, dout outwards.</summary>
        public E4Plane Move(float du, float dv, float dout) { E4Plane p = this; p.O = P(du, dv, dout); return p; }
    }

    /// <summary>
    /// East line, art agent 4 (v0.5): shared parts for SUCCESS HILL, GUILDFORD and EAST GUILDFORD (East4Builder).
    /// Facade toolkit (planes, recessed framed windows, round / pointed arches, roofs, verandahs with posts and lace),
    /// trees (river red gum, jacaranda in bloom, Norfolk pine), the Midland line beside the road (ballast, sleepers,
    /// rails, overhead-wire gantries, fences, platforms, B-series style trains, footbridge) and pixel lettering.
    /// ComicPalette + ComicInk only (vertex colours); uses the CBD / Quay kit (CbdQuayKit) read-only.
    /// </summary>
    internal static class E4
    {
        public static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        public static float F(System.Random r, float a, float b) => Mathf.Lerp(a, b, (float)r.NextDouble());
        static Color H(string hex) => K.Hex(hex);

        // ---- palette (sRGB; sources in art-src/districts/successhill|guildford|eastguildford/SOURCES.txt)
        public static readonly Color RedPath = H("A4584C"), PathLine = H("F1ECDD"), Verge = H("74A348"), Gravel = H("BFB08E"), KerbCap = H("E4DED0");
        public static readonly Color Lawn = H("6DA544"), LawnLight = H("84B857"), LawnDry = H("A9B866"), Mulch = H("9C7B56"), Soil = H("B08C5E");
        public static readonly Color Ballast = H("8E8679"), BallastDark = H("746D62"), Sleeper = H("BDB7A9"), Rail = H("6B6259"), RailTop = H("B4BABF");
        public static readonly Color Steel = H("A3AAB0"), SteelDark = H("6F7780"), Wire = H("3B4148"), Insulator = H("B7623E");
        public static readonly Color GumBark = H("E9E2D2"), GumBarkGrey = H("B7AE9C"), GumBarkLow = H("8A7B66");
        public static readonly Color GumA = H("6F8F52"), GumB = H("5C7C48"), GumC = H("8AA763"), GumDark = H("46623B");
        public static readonly Color JacA = H("8E6BD8"), JacB = H("A98BE8"), JacC = H("7453BC"), JacBark = H("5B4B43"), Petal = H("B9A2EC");
        public static readonly Color PineDark = H("2C5A36"), PineMid = H("3B6F42"), PineLight = H("5A8C4C"), PineUnder = H("1C3826"), PineBark = H("6E5A47");
        public static readonly Color River = H("5F8272"), RiverLight = H("8FB09A"), RiverDeep = H("4B6B60"), Clay = H("B89C70"), ClayDark = H("9A8058");
        public static readonly Color Brick = H("B5533B"), BrickDark = H("98432F"), BrickOrange = H("C96A3E"), Salmon = H("DE8E78"), BrickDeep = H("8B2F2A");
        public static readonly Color Cream = H("F4E8C8"), White = H("F7F4EC"), HotelWhite = H("FFF8EA"), HotelBand = H("EFE6D4"), TrimCream = H("F3E4C2");
        public static readonly Color Iron = H("BCC3C7"), IronDark = H("98A0A6"), Tile = H("C4623E"), TileDark = H("A44E32"), Slate = H("5C616B"), Zinc = H("6C7882");
        public static readonly Color HeritageGreen = H("2F5D46"), Maroon = H("7A2E2E"), Glass = H("2F5F78"), GlassDark = H("243E52"), Timber = H("A8774E"), TimberDark = H("7A5436");
        public static readonly Color Stone = H("F8DEBC"), StoneLight = H("FFEFD6"), StoneShade = H("EBCBA2"), StoneDark = H("D2B488");
        public static readonly Color Deck = H("9A6B45"), DeckLight = H("B88757"), PlatformRed = H("B5604C"), Concrete = H("CFCABD"), ConcreteDark = H("A9A498");
        public static readonly Color TrainWhite = H("EEF0F0"), TrainGreen = H("1F9A57"), TrainGrey = H("8D959C"), TrainDark = H("2A3138"), Yellow = H("F2C230");
        public static readonly Color PaveBrick = H("A8644F"), PaveBrickDark = H("8F5241");

        // The railway corridor beyond the right-hand sidewalk edge (x = 8): metres from the road centre line.
        public const float FenceX = 9.7f, BedX0 = 10.2f, BedX1 = 21.4f, Track1 = 12.9f, Track2 = 17.4f, BedTop = .1f, RailY = .28f;

        // ================================================================ primitives on a plane

        public static void Box(ComicMesh m, E4Plane p, float u0, float u1, float v0, float v1, float o0, float o1, Color c) =>
            m.Box(p.P((u0 + u1) * .5f, (v0 + v1) * .5f, (o0 + o1) * .5f), new Vector3(Mathf.Abs(u1 - u0), Mathf.Abs(v1 - v0), Mathf.Abs(o1 - o0)), c, p.Rot);

        public static void Quad(ComicMesh m, E4Plane p, float u0, float u1, float v0, float v1, float o, Color c, bool ink = true) =>
            K.Face(m, p.P(u0, v0, o), p.P(u1, v0, o), p.P(u1, v1, o), p.P(u0, v1, o), c, p.N, ink);

        public static void Tri(ComicMesh m, E4Plane p, Vector2 a, Vector2 b, Vector2 c, float o, Color col) =>
            K.TriFacing(m, p.P(a.x, a.y, o), p.P(b.x, b.y, o), p.P(c.x, c.y, o), col, p.N);

        /// <summary>A bar (box) from a to b lying on a wall with normal n: th wide in the wall plane, depth out of it.</summary>
        public static void Bar(ComicMesh m, Vector3 a, Vector3 b, float th, float depth, Vector3 n, Color c)
        {
            Vector3 s = b - a;
            float len = s.magnitude;
            if (len < 1e-4f) return;
            s /= len;
            m.Box((a + b) * .5f + n * (depth * .5f), new Vector3(len + th * .35f, th, depth), c, Quaternion.LookRotation(n, Vector3.Cross(n, s)));
        }

        public static void Bar(ComicMesh m, E4Plane p, float ua, float va, float ub, float vb, float o, float th, float depth, Color c) =>
            Bar(m, p.P(ua, va, o), p.P(ub, vb, o), th, depth, p.N, c);

        /// <summary>A free beam between two points (square section).</summary>
        public static void Beam(ComicMesh m, Vector3 a, Vector3 b, float th, Color c)
        {
            if ((b - a).sqrMagnitude < 1e-6f) return;
            m.Beam(a, b, th, c);
        }

        /// <summary>A tapered 6-sided limb from a to b.</summary>
        public static void Limb(ComicMesh m, Vector3 a, Vector3 b, float ra, float rb, Color c, int sides = 6)
        {
            Vector3 d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            m.Cylinder((a + b) * .5f, ra, d.magnitude, c, sides, rb / Mathf.Max(ra, 1e-3f), Quaternion.FromToRotation(Vector3.up, d));
        }

        /// <summary>A cheap faceted blob (6 sides, 3 rings = 36 triangles; the kit's Ellipsoid is 64).</summary>
        public static void Blob(ComicMesh m, Vector3 c, Vector3 size, Color col, int sides = 6, int rings = 3, float spin = 0f)
        {
            Vector3 P(int ring, int side)
            {
                float a = Mathf.PI * ring / rings, b = Mathf.PI * 2f * side / sides + spin;
                return c + Vector3.Scale(new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b)), size) * .5f;
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++)
                {
                    if (r == 0) m.Tri(P(0, s), P(1, s + 1), P(1, s), col);
                    else if (r == rings - 1) m.Tri(P(r, s), P(r, s + 1), P(r + 1, s), col);
                    else m.Quad(P(r, s), P(r, s + 1), P(r + 1, s + 1), P(r + 1, s), col, false);
                }
        }

        /// <summary>A slab from a to b (width wa -> wb, thick), top colour up, darker underneath (fronds, pine boughs).</summary>
        public static void Slab(ComicMesh m, Vector3 a, Vector3 b, Vector3 side, float wa, float wb, float thick, Color top, Color under)
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

        // ================================================================ pixel lettering (place names, plain words)

        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "010/101/111/101/101", ['B'] = "110/101/110/101/110", ['C'] = "011/100/100/100/011", ['D'] = "110/101/101/101/110",
            ['E'] = "111/100/110/100/111", ['F'] = "111/100/110/100/100", ['G'] = "0111/1000/1011/1001/0110", ['H'] = "101/101/111/101/101",
            ['I'] = "111/010/010/010/111", ['J'] = "001/001/001/101/010", ['K'] = "101/101/110/101/101", ['L'] = "100/100/100/100/111",
            ['M'] = "10001/11011/10101/10001/10001", ['N'] = "1001/1101/1011/1001/1001", ['O'] = "111/101/101/101/111", ['P'] = "110/101/110/100/100",
            ['Q'] = "111/101/101/111/001", ['R'] = "110/101/110/101/101", ['S'] = "011/100/010/001/110", ['T'] = "111/010/010/010/010",
            ['U'] = "101/101/101/101/111", ['V'] = "101/101/101/101/010", ['W'] = "10001/10001/10101/10101/01010", ['X'] = "101/101/010/101/101",
            ['Y'] = "101/101/010/010/010", ['Z'] = "111/001/010/100/111", ['1'] = "010/110/010/010/111", ['2'] = "110/001/010/100/111",
            ['8'] = "111/101/111/101/111", ['9'] = "111/101/111/001/111", ['0'] = "111/101/101/101/111", ['4'] = "101/101/111/001/001",
        };

        public static float TextWidth(string text, float px)
        {
            int cols = 0;
            foreach (char ch in text) cols += (Glyphs.TryGetValue(ch, out string g) ? g.IndexOf('/') : 2) + 1;
            return Mathf.Max(0, cols - 1) * px;
        }

        /// <summary>Pixel text centred on c; rot turns the text's own -Z (its face) and +X (reading direction).</summary>
        public static void Text(ComicMesh m, string text, Vector3 c, Quaternion rot, float px, Color colour)
        {
            m.Surface = ComicSurface.Plain;
            float x = -TextWidth(text, px) * .5f, y0 = 2.5f * px;
            foreach (char ch in text)
            {
                if (!Glyphs.TryGetValue(ch, out string g)) { x += 3 * px; continue; }
                string[] rows = g.Split('/');
                for (int row = 0; row < rows.Length; row++)
                    for (int col = 0; col < rows[row].Length; col++)
                    {
                        if (rows[row][col] != '1') continue;
                        int end = col;
                        while (end + 1 < rows[row].Length && rows[row][end + 1] == '1') end++;
                        float xa = x + col * px, xb = x + (end + 1) * px, ya = y0 - (row + 1) * px, yb = ya + px;
                        m.Quad(c + rot * V(xb, ya, 0), c + rot * V(xa, ya, 0), c + rot * V(xa, yb, 0), c + rot * V(xb, yb, 0), colour, false);
                        col = end;
                    }
                x += (rows[0].Length + 1) * px;
            }
        }

        /// <summary>Pixel text on a wall, centred at (u, v), o out of the wall.</summary>
        public static void Text(ComicMesh m, E4Plane p, string text, float u, float v, float o, float px, Color colour) =>
            Text(m, text, p.P(u, v, o), Quaternion.LookRotation(-p.N, p.U), px, colour);

        // ================================================================ arches

        /// <summary>The outline of an arch from its left spring to its right spring: round when rise = w / 2, pointed
        /// (Gothic, two arcs) when higher, segmental when lower. n segments per half.</summary>
        public static List<Vector2> Arc(float uc, float w, float vSpring, float rise, int n = 4)
        {
            var pts = new List<Vector2>();
            float half = w * .5f;
            if (rise >= half - 1e-4f)
            {
                float c = (rise * rise - half * half) / w, rad = c + half, a1 = Mathf.Atan2(rise, -c);
                for (int i = 0; i <= n; i++)
                {
                    float a = Mathf.Lerp(Mathf.PI, a1, (float)i / n);
                    pts.Add(new Vector2(uc + c + rad * Mathf.Cos(a), vSpring + rad * Mathf.Sin(a)));
                }
            }
            else
            {
                float k = (half * half - rise * rise) / (2f * rise), rad = k + rise, a0 = Mathf.Atan2(k, -half);
                for (int i = 0; i <= n; i++)
                {
                    float a = Mathf.Lerp(a0, Mathf.PI * .5f, (float)i / n);
                    pts.Add(new Vector2(uc + rad * Mathf.Cos(a), vSpring - k + rad * Mathf.Sin(a)));
                }
            }
            for (int i = n - 1; i >= 0; i--) pts.Add(new Vector2(2f * uc - pts[i].x, pts[i].y));
            return pts;
        }

        /// <summary>An arched opening filled with one colour: the rectangle v0..vSpring and the arch head above it.</summary>
        public static void ArchFill(ComicMesh m, E4Plane p, float uc, float w, float v0, float vSpring, float rise, float o, Color c, int n = 4)
        {
            if (vSpring > v0 + .01f) Quad(m, p, uc - w * .5f, uc + w * .5f, v0, vSpring, o, c, false);
            List<Vector2> pts = Arc(uc, w, vSpring, rise, n);
            var centre = new Vector2(uc, vSpring);
            for (int i = 0; i + 1 < pts.Count; i++) Tri(m, p, centre, pts[i], pts[i + 1], o, c);
        }

        /// <summary>The moulding round an arched opening: bars along the arch head and (jambs) down both sides to v0.</summary>
        public static void ArchTrim(ComicMesh m, E4Plane p, float uc, float w, float v0, float vSpring, float rise, float o, float th, float depth, Color c, int n = 4, bool jambs = true)
        {
            List<Vector2> pts = Arc(uc, w, vSpring, rise, n);
            for (int i = 0; i + 1 < pts.Count; i++) Bar(m, p, pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y, o, th, depth, c);
            if (!jambs || vSpring <= v0 + .01f) return;
            Box(m, p, uc - w * .5f - th * .5f, uc - w * .5f + th * .5f, v0, vSpring, o, o + depth, c);
            Box(m, p, uc + w * .5f - th * .5f, uc + w * .5f + th * .5f, v0, vSpring, o, o + depth, c);
        }

        /// <summary>A flat band round an arched opening (a painted or stone arch ring, th wide), cheap: one quad per segment.</summary>
        public static void ArchBand(ComicMesh m, E4Plane p, float uc, float w, float v0, float vSpring, float rise, float o, float th, Color c, int n = 3, bool jambs = true)
        {
            List<Vector2> inner = Arc(uc, w, vSpring, rise, n), outer = Arc(uc, w + 2f * th, vSpring, rise + th, n);
            for (int i = 0; i + 1 < inner.Count; i++)
                K.Face(m, p.P(inner[i].x, inner[i].y, o), p.P(inner[i + 1].x, inner[i + 1].y, o), p.P(outer[i + 1].x, outer[i + 1].y, o), p.P(outer[i].x, outer[i].y, o), c, p.N, false);
            if (!jambs || vSpring <= v0 + .01f) return;
            Quad(m, p, uc - w * .5f - th, uc - w * .5f, v0, vSpring, o, c, false);
            Quad(m, p, uc + w * .5f, uc + w * .5f + th, v0, vSpring, o, c, false);
        }

        /// <summary>A cheap arched window for walls in shade or far away: ink reveal, glass, a flat band, a sill.</summary>
        public static void FlatArchWindow(ComicMesh m, E4Plane p, float uc, float w, float v0, float vSpring, float rise, float o, Color trim, Color glass, int n = 3, bool sill = true, bool mullion = true)
        {
            m.Surface = ComicSurface.PaintedMetal;
            ArchFill(m, p, uc, w, v0, vSpring, rise, o + .012f, K.R.ink, n);
            m.Surface = ComicSurface.Glass;
            ArchFill(m, p, uc, w - .16f, v0 + .08f, vSpring, rise - .08f, o + .024f, glass, n);
            m.Surface = ComicSurface.Plaster;
            ArchBand(m, p, uc, w, v0, vSpring, rise, o + .018f, .16f, trim, n);
            if (mullion) Quad(m, p, uc - .035f, uc + .035f, v0 + .06f, vSpring + rise - .1f, o + .034f, trim, false);
            if (sill) Box(m, p, uc - w * .5f - .22f, uc + w * .5f + .22f, v0 - .14f, v0, o, o + .14f, trim);
        }

        /// <summary>A pier (pilaster) standing out of a wall from o0 to o1, with proud horizontal bands (rustication) in a
        /// second colour every step metres, and a cap when capital > 0.</summary>
        public static void Pier(ComicMesh m, E4Plane p, float u0, float u1, float v0, float v1, float o0, float o1, Color c, Color band, float step = .62f, float capital = 0f)
        {
            Box(m, p, u0, u1, v0, v1, o0, o1, c);
            if (step > 0f)
                for (float v = v0 + step * .75f; v < v1 - .3f - capital; v += step)
                    Box(m, p, u0 - .025f, u1 + .025f, v, v + step * .32f, o0, o1 + .025f, band);
            if (capital <= 0f) return;
            Box(m, p, u0 - .08f, u1 + .08f, v1 - capital, v1 - capital * .5f, o0, o1 + .08f, band);
            Box(m, p, u0 - .14f, u1 + .14f, v1 - capital * .5f, v1, o0, o1 + .14f, c);
        }

        /// <summary>A parapet balustrade between u0 and u1: bottom and top rails, a shadowed panel with pale balusters.</summary>
        public static void ParapetBalusters(ComicMesh m, E4Plane p, float u0, float u1, float v0, float v1, float o, Color c)
        {
            Box(m, p, u0, u1, v0, v0 + .14f, o - .3f, o + .04f, c);
            Box(m, p, u0, u1, v1 - .16f, v1, o - .32f, o + .06f, c);
            Quad(m, p, u0, u1, v0 + .14f, v1 - .16f, o - .12f, Color.Lerp(c, K.R.ink, .55f), false);
            int n = Mathf.Max(2, Mathf.RoundToInt((u1 - u0) / .36f));
            for (int i = 0; i < n; i++)
            {
                float u = Mathf.Lerp(u0, u1, (i + .5f) / n);
                Quad(m, p, u - .07f, u + .07f, v0 + .14f, v1 - .16f, o - .02f, c, false);
            }
        }

        /// <summary>An arched window: ink reveal, glass, a stone / render surround standing out of the wall, a centre
        /// mullion and a sill.</summary>
        public static void ArchWindow(ComicMesh m, E4Plane p, float uc, float w, float v0, float vSpring, float rise, float o, Color trim, Color glass, int n = 4, bool mullion = true, bool sill = true)
        {
            m.Surface = ComicSurface.PaintedMetal;
            ArchFill(m, p, uc, w + .1f, v0 - .02f, vSpring, rise + .05f, o + .012f, K.R.ink, n);
            m.Surface = ComicSurface.Glass;
            ArchFill(m, p, uc, w - .12f, v0 + .08f, vSpring, rise - .06f, o + .024f, glass, n);
            m.Surface = ComicSurface.Plaster;
            ArchTrim(m, p, uc, w + .16f, v0, vSpring, rise + .08f, o, .16f, .1f, trim, n);
            if (mullion) { m.Surface = ComicSurface.PaintedMetal; Box(m, p, uc - .035f, uc + .035f, v0 + .06f, vSpring + rise - .12f, o + .02f, o + .07f, trim); }
            if (sill) { m.Surface = ComicSurface.Plaster; Box(m, p, uc - w * .5f - .2f, uc + w * .5f + .2f, v0 - .14f, v0, o, o + .16f, trim); }
        }

        // ================================================================ windows, doors

        /// <summary>A recessed framed window: a four-sided surround standing out of the wall (the bottom is the sill), an
        /// ink reveal and the glass set back inside it, glazing bars (cols x rows panes).</summary>
        public static void Window(ComicMesh m, E4Plane p, float uc, float w, float v0, float v1, float o, Color trim, Color glass, int cols = 2, int rows = 2, float frame = .12f, float depth = .1f)
        {
            float u0 = uc - w * .5f, u1 = uc + w * .5f;
            m.Surface = ComicSurface.Plaster;
            Box(m, p, u0 - frame, u0, v0, v1, o, o + depth, trim);
            Box(m, p, u1, u1 + frame, v0, v1, o, o + depth, trim);
            Box(m, p, u0 - frame, u1 + frame, v1, v1 + frame, o, o + depth + .02f, trim);
            Box(m, p, u0 - frame - .08f, u1 + frame + .08f, v0 - frame, v0, o, o + depth + .08f, trim);
            m.Surface = ComicSurface.PaintedMetal;
            Quad(m, p, u0, u1, v0, v1, o + .012f, K.R.ink);
            m.Surface = ComicSurface.Glass;
            Quad(m, p, u0 + .06f, u1 - .06f, v0 + .06f, v1 - .06f, o + .024f, glass);
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 1; i < cols; i++) { float u = Mathf.Lerp(u0, u1, (float)i / cols); Quad(m, p, u - .03f, u + .03f, v0 + .05f, v1 - .05f, o + .034f, trim, false); }
            for (int i = 1; i < rows; i++) { float v = Mathf.Lerp(v0, v1, (float)i / rows); Quad(m, p, u0 + .05f, u1 - .05f, v - .03f, v + .03f, o + .034f, trim, false); }
        }

        /// <summary>A cheap window for walls seen from far or at an angle: ink frame quad, glass quad, a sill box.</summary>
        public static void FlatWindow(ComicMesh m, E4Plane p, float uc, float w, float v0, float v1, float o, Color trim, Color glass, bool sill = true)
        {
            float u0 = uc - w * .5f, u1 = uc + w * .5f;
            m.Surface = ComicSurface.PaintedMetal;
            Quad(m, p, u0 - .08f, u1 + .08f, v0 - .08f, v1 + .08f, o + .012f, trim);
            Quad(m, p, u0, u1, v0, v1, o + .022f, K.R.ink, false);
            m.Surface = ComicSurface.Glass;
            Quad(m, p, u0 + .06f, u1 - .06f, v0 + .06f, v1 - .06f, o + .032f, glass, false);
            m.Surface = ComicSurface.PaintedMetal;
            Quad(m, p, uc - .03f, uc + .03f, v0 + .05f, v1 - .05f, o + .04f, trim, false);
            if (sill) { m.Surface = ComicSurface.Plaster; Box(m, p, u0 - .16f, u1 + .16f, v0 - .18f, v0 - .08f, o, o + .14f, trim); }
        }

        /// <summary>A panelled door in a frame, with a fanlight above when v1 - v0 is over 2.5 m.</summary>
        public static void Door(ComicMesh m, E4Plane p, float uc, float w, float v0, float v1, float o, Color trim, Color leaf, Color glass)
        {
            float u0 = uc - w * .5f, u1 = uc + w * .5f, top = v1 - v0 > 2.5f ? v0 + 2.15f : v1;
            m.Surface = ComicSurface.Plaster;
            Box(m, p, u0 - .12f, u0, v0, v1, o, o + .1f, trim);
            Box(m, p, u1, u1 + .12f, v0, v1, o, o + .1f, trim);
            Box(m, p, u0 - .12f, u1 + .12f, v1, v1 + .12f, o, o + .12f, trim);
            m.Surface = ComicSurface.PaintedMetal;
            Quad(m, p, u0, u1, v0, v1, o + .012f, K.R.ink);
            m.Surface = ComicSurface.Wood;
            Quad(m, p, u0 + .05f, u1 - .05f, v0 + .04f, top - .04f, o + .024f, leaf);
            Color panel = Color.Lerp(leaf, K.R.ink, .3f);
            Quad(m, p, u0 + .16f, u1 - .16f, v0 + .2f, v0 + .95f, o + .034f, panel, false);
            Quad(m, p, u0 + .16f, u1 - .16f, v0 + 1.1f, top - .2f, o + .034f, panel, false);
            if (top < v1 - .01f) { m.Surface = ComicSurface.Glass; Quad(m, p, u0 + .06f, u1 - .06f, top + .04f, v1 - .06f, o + .024f, glass); }
        }

        /// <summary>A shopfront: ink frame, big glass above a coloured stall board, mullions, a recessed door in the middle.</summary>
        public static void Shopfront(ComicMesh m, E4Plane p, float u0, float u1, float v0, float v1, float o, Color stall, Color glass, Color door)
        {
            m.Surface = ComicSurface.PaintedMetal;
            Quad(m, p, u0, u1, v0, v1, o + .012f, K.R.ink);
            float uc = (u0 + u1) * .5f, dw = Mathf.Min(1.1f, (u1 - u0) * .3f);
            m.Surface = ComicSurface.Plaster;
            Box(m, p, u0 + .06f, uc - dw * .5f - .04f, v0 + .04f, v0 + .62f, o, o + .08f, stall);
            Box(m, p, uc + dw * .5f + .04f, u1 - .06f, v0 + .04f, v0 + .62f, o, o + .08f, stall);
            m.Surface = ComicSurface.Glass;
            Quad(m, p, u0 + .1f, uc - dw * .5f - .08f, v0 + .66f, v1 - .5f, o + .024f, glass);
            Quad(m, p, uc + dw * .5f + .08f, u1 - .1f, v0 + .66f, v1 - .5f, o + .024f, glass);
            Quad(m, p, u0 + .1f, u1 - .1f, v1 - .42f, v1 - .08f, o + .024f, Color.Lerp(glass, Color.white, .25f));
            m.Surface = ComicSurface.Wood;
            Quad(m, p, uc - dw * .5f, uc + dw * .5f, v0 + .04f, v1 - .5f, o + .024f, door);
            m.Surface = ComicSurface.Glass;
            Quad(m, p, uc - dw * .5f + .12f, uc + dw * .5f - .12f, v0 + .9f, v1 - .66f, o + .034f, glass, false);
            m.Surface = ComicSurface.PaintedMetal;
            Box(m, p, uc - dw * .5f - .07f, uc - dw * .5f - .01f, v0, v1 - .46f, o, o + .09f, K.R.ink);
            Box(m, p, uc + dw * .5f + .01f, uc + dw * .5f + .07f, v0, v1 - .46f, o, o + .09f, K.R.ink);
            Box(m, p, u0, u1, v1 - .5f, v1 - .43f, o, o + .09f, K.R.ink);
        }

        // ================================================================ roofs

        /// <summary>A roof with its ridge parallel to the wall: up from the front eave (oFront, v0) to the ridge and down
        /// to the back, closed gable ends. hip > 0 pulls the ridge in at both ends (a hipped roof).</summary>
        public static void RidgeRoof(ComicMesh m, E4Plane p, float u0, float u1, float v0, float v1, float oFront, float oBack, Color c, float hip = 0f)
        {
            float oR = (oFront + oBack) * .5f;
            Vector3 f0 = p.P(u0, v0, oFront), f1 = p.P(u1, v0, oFront), b0 = p.P(u0, v0, oBack), b1 = p.P(u1, v0, oBack);
            Vector3 r0 = p.P(u0 + hip, v1, oR), r1 = p.P(u1 - hip, v1, oR);
            K.Face(m, f0, f1, r1, r0, c, (p.N + Vector3.up).normalized);
            K.Face(m, b0, b1, r1, r0, c, (-p.N + Vector3.up).normalized);
            Color end = Color.Lerp(c, K.R.ink, .12f);
            K.TriFacing(m, f0, b0, r0, hip > 0f ? end : c, hip > 0f ? (-p.R + Vector3.up).normalized : -p.R);
            K.TriFacing(m, f1, b1, r1, hip > 0f ? end : c, hip > 0f ? (p.R + Vector3.up).normalized : p.R);
        }

        /// <summary>A roof with its ridge running back from the wall: a gable (triangle) facing the viewer at oFront.</summary>
        public static void GableRoof(ComicMesh m, E4Plane p, float u0, float u1, float v0, float v1, float oFront, float oBack, Color roof, Color gable)
        {
            float uc = (u0 + u1) * .5f;
            Vector3 f0 = p.P(u0, v0, oFront), f1 = p.P(u1, v0, oFront), fa = p.P(uc, v1, oFront);
            Vector3 b0 = p.P(u0, v0, oBack), b1 = p.P(u1, v0, oBack), ba = p.P(uc, v1, oBack);
            K.TriFacing(m, f0, f1, fa, gable, p.N);
            K.TriFacing(m, b0, b1, ba, gable, -p.N);
            K.Face(m, f0, fa, ba, b0, roof, (-p.R + Vector3.up).normalized);
            K.Face(m, f1, fa, ba, b1, roof, (p.R + Vector3.up).normalized);
        }

        /// <summary>A brick chimney with a corbelled cap and two pots.</summary>
        public static void Chimney(ComicMesh m, Vector3 p, float h, Color brick, float w = .7f)
        {
            m.Surface = ComicSurface.Brick;
            m.Box(p + V(0, h * .5f, 0), V(w, h, w), brick);
            m.Surface = ComicSurface.Plaster;
            m.Box(p + V(0, h + .08f, 0), V(w + .2f, .16f, w + .2f), Color.Lerp(brick, Cream, .55f));
            m.Surface = ComicSurface.Brick;
            m.Box(p + V(0, h + .26f, 0), V(w + .06f, .2f, w + .06f), brick);
            foreach (float x in new[] { -.16f, .16f }) m.Cylinder(p + V(x, h + .52f, 0), .11f, .34f, Tile, 6, .8f);
        }

        // ================================================================ verandahs and balconies

        /// <summary>A post verandah on a wall: a corrugated iron roof falling from vWall at the wall to vEave at out
        /// metres (bullnose: a curved-down lip), a fascia beam, square posts, lace brackets.</summary>
        public static void Verandah(ComicMesh m, E4Plane p, float u0, float u1, float vEave, float vWall, float outward, int posts, Color roof, Color post, Color lace, bool bullnose = false, float floor = 0f)
        {
            m.Surface = ComicSurface.Metal;
            float lip = bullnose ? .55f : 0f;
            Vector3 w0 = p.P(u0, vWall, 0), w1 = p.P(u1, vWall, 0), e0 = p.P(u0, vEave + (bullnose ? .3f : 0f), outward - lip), e1 = p.P(u1, vEave + (bullnose ? .3f : 0f), outward - lip);
            K.Face(m, w0, w1, e1, e0, roof, (p.N + Vector3.up * 3f).normalized);
            K.Face(m, w0 - Vector3.up * .08f, w1 - Vector3.up * .08f, e1 - Vector3.up * .08f, e0 - Vector3.up * .08f, Color.Lerp(roof, K.R.ink, .45f), -Vector3.up);
            if (bullnose)
            {
                Vector3 l0 = p.P(u0, vEave - .05f, outward + .05f), l1 = p.P(u1, vEave - .05f, outward + .05f);
                K.Face(m, e0, e1, l1, l0, Color.Lerp(roof, Color.white, .12f), (p.N * 2f + Vector3.up).normalized);
            }
            m.Surface = ComicSurface.PaintedMetal;
            Box(m, p, u0 - .06f, u1 + .06f, vEave - .22f, vEave - .04f, outward - .12f, outward + .02f, post);
            for (int i = 0; i < posts; i++)
            {
                float u = posts == 1 ? (u0 + u1) * .5f : Mathf.Lerp(u0 + .12f, u1 - .12f, (float)i / (posts - 1));
                Box(m, p, u - .07f, u + .07f, floor, vEave - .2f, outward - .12f, outward + .02f, post);
                Box(m, p, u - .11f, u + .11f, floor, floor + .5f, outward - .16f, outward + .06f, post);
                float bv = vEave - .22f;
                if (i > 0) Tri(m, p, new Vector2(u - .07f, bv), new Vector2(u - .62f, bv), new Vector2(u - .07f, bv - .55f), outward - .05f, lace);
                if (i < posts - 1) Tri(m, p, new Vector2(u + .07f, bv), new Vector2(u + .62f, bv), new Vector2(u + .07f, bv - .55f), outward - .05f, lace);
                if (i > 0) K.TriFacing(m, p.P(u - .07f, bv, outward - .06f), p.P(u - .62f, bv, outward - .06f), p.P(u - .07f, bv - .55f, outward - .06f), lace, -p.N);
                if (i < posts - 1) K.TriFacing(m, p.P(u + .07f, bv, outward - .06f), p.P(u + .62f, bv, outward - .06f), p.P(u + .07f, bv - .55f, outward - .06f), lace, -p.N);
            }
        }

        /// <summary>A balcony balustrade between u0 and u1 at out metres: bottom and top rails, a lace panel (a pale
        /// sheet with ink bars: cast iron), newel posts. Seen from both sides.</summary>
        public static void Balustrade(ComicMesh m, E4Plane p, float u0, float u1, float vFloor, float outward, Color rail, Color lace, int newels = 0)
        {
            m.Surface = ComicSurface.PaintedMetal;
            Box(m, p, u0, u1, vFloor + .05f, vFloor + .13f, outward - .07f, outward + .01f, rail);
            Box(m, p, u0, u1, vFloor + .92f, vFloor + 1.02f, outward - .09f, outward + .03f, rail);
            Quad(m, p, u0, u1, vFloor + .13f, vFloor + .92f, outward - .02f, lace, false);
            K.Face(m, p.P(u0, vFloor + .13f, outward - .04f), p.P(u1, vFloor + .13f, outward - .04f), p.P(u1, vFloor + .92f, outward - .04f), p.P(u0, vFloor + .92f, outward - .04f), Color.Lerp(lace, K.R.ink, .2f), -p.N, false);
            Color bar = Color.Lerp(lace, K.R.ink, .55f);
            int n = Mathf.Max(2, Mathf.RoundToInt((u1 - u0) / .42f));
            for (int i = 1; i < n; i++)
            {
                float u = Mathf.Lerp(u0, u1, (float)i / n);
                Quad(m, p, u - .025f, u + .025f, vFloor + .15f, vFloor + .9f, outward - .012f, bar, false);
            }
            Quad(m, p, u0, u1, vFloor + .5f, vFloor + .55f, outward - .012f, bar, false);
            for (int i = 0; i < newels; i++)
            {
                float u = newels == 1 ? (u0 + u1) * .5f : Mathf.Lerp(u0, u1, (float)i / (newels - 1));
                Box(m, p, u - .08f, u + .08f, vFloor, vFloor + 1.1f, outward - .12f, outward + .04f, rail);
            }
        }

        // ================================================================ trees and plants

        /// <summary>A lumpy foliage clump: a faceted ball whose rings are pushed in and out a little (seeded), so a
        /// crown built from several of them never shows a regular outline. sides x rings facets.</summary>
        public static void Clump(ComicMesh m, Vector3 c, Vector3 size, Color col, int seed, int sides = 7, int rings = 4, float jitter = .16f)
        {
            var r = new System.Random(seed * 2654435 + 17);
            float spin = F(r, 0f, 6.28f);
            var pts = new Vector3[rings + 1, sides];
            for (int ring = 0; ring <= rings; ring++)
                for (int side = 0; side < sides; side++)
                {
                    float a = Mathf.PI * ring / rings, b = Mathf.PI * 2f * side / sides + spin + (ring % 2) * Mathf.PI / sides;
                    float k = ring == 0 || ring == rings ? 1f : 1f + F(r, -jitter, jitter);
                    pts[ring, side] = c + Vector3.Scale(new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a) * (ring == 0 || ring == rings ? F(r, .85f, 1f) : 1f), Mathf.Sin(a) * Mathf.Sin(b)), size) * (.5f * k);
                }
            m.Surface = ComicSurface.Foliage;
            for (int ring = 0; ring < rings; ring++)
                for (int side = 0; side < sides; side++)
                {
                    int n = (side + 1) % sides;
                    if (ring == 0) m.Tri(pts[0, 0], pts[1, n], pts[1, side], col);
                    else if (ring == rings - 1) m.Tri(pts[ring, side], pts[ring, n], pts[rings, 0], col);
                    else
                    {
                        // rings are offset by half a facet: two triangles per facet (no flat quad to ink)
                        if (ring % 2 == 1) { m.Tri(pts[ring, side], pts[ring, n], pts[ring + 1, n], col); m.Tri(pts[ring, side], pts[ring + 1, n], pts[ring + 1, side], col); }
                        else { m.Tri(pts[ring, side], pts[ring, n], pts[ring + 1, side], col); m.Tri(pts[ring, n], pts[ring + 1, n], pts[ring + 1, side], col); }
                    }
                }
        }

        /// <summary>River red gum / flooded gum (the big eucalypts of the upper Swan): a rough dark stocking at the base,
        /// a smooth pale trunk with grey patches leaning a little, three heavy forked limbs, and an open crown: lumpy
        /// olive-green clumps of several sizes at the limb ends, lighter on top, darker ones drooping underneath, with
        /// the pale limbs showing between them. far = a lighter tree for the rows without outline.</summary>
        public static void Gum(ComicMesh m, Vector3 p, float h, int seed, bool far = false, Color? tint = null)
        {
            var r = new System.Random(seed * 7919 + 13);
            float tr = .2f + h * .014f, spin = F(r, 0f, 360f);
            Color Tone(Color c) => tint.HasValue ? Color.Lerp(c, tint.Value, .45f) : c;
            Vector3 lean = Quaternion.Euler(0, spin, 0) * V(F(r, .04f, .09f) * h, 0, 0);
            float forkY = h * F(r, .36f, .46f);
            Vector3 mid = p + V(0, forkY * .4f, 0) + lean * .25f, fork = p + V(0, forkY, 0) + lean;
            int sides = far ? 6 : 8;
            m.Surface = ComicSurface.Wood;
            if (!far) m.Cylinder(p + V(0, .12f, 0), tr * 1.45f, .24f, Tone(GumBarkLow), sides, .8f);
            Limb(m, p, mid, tr * 1.12f, tr * .95f, Tone(far ? GumBarkGrey : GumBarkLow), sides);
            Limb(m, mid, fork, tr * .95f, tr * .8f, Tone(GumBark), sides);
            int limbs = 3;
            Color[] greens = { GumA, GumB, GumC };
            for (int i = 0; i < limbs; i++)
            {
                float a = (spin + 40f + i * 360f / limbs + F(r, -25f, 25f)) * Mathf.Deg2Rad;
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a)), side = Vector3.Cross(Vector3.up, dir);
                float reach = h * F(r, .2f, .3f), up = h * F(r, .28f, .4f);
                Vector3 elbow = fork + dir * reach * .5f + V(0, up * .55f, 0), tip = fork + dir * reach + V(0, up, 0);
                m.Surface = ComicSurface.Wood;
                if (far) Limb(m, fork, tip, tr * .56f, tr * .16f, Tone(i == 1 ? GumBarkGrey : GumBark), 5);
                else
                {
                    Limb(m, fork, elbow, tr * .58f, tr * .4f, Tone(i == 1 ? GumBarkGrey : GumBark), 6);
                    Limb(m, elbow, tip, tr * .4f, tr * .16f, Tone(GumBark), 6);
                }
                float s = h * F(r, .27f, .34f);
                Color main = Tone(greens[(i + seed) % 3]);
                Clump(m, tip + V(0, s * .1f, 0), V(s * 1.2f, s * .72f, s * 1.1f), main, seed * 11 + i, far ? 6 : 7, 4, far ? .22f : .16f);
                Clump(m, tip + dir * s * .3f + V(0, s * .42f, 0), V(s * .75f, s * .5f, s * .75f), Tone(GumC), seed * 13 + i, far ? 6 : 7, far ? 3 : 4);
                if (far) continue;
                Vector3 twig = elbow + side * reach * F(r, .35f, .55f) * (i % 2 == 0 ? 1f : -1f) + V(0, up * F(r, .1f, .3f), 0);
                m.Surface = ComicSurface.Wood;
                Limb(m, elbow, twig, tr * .3f, tr * .1f, Tone(GumBark), 5);
                Clump(m, twig + V(0, s * .08f, 0), V(s * .85f, s * .52f, s * .8f), Tone(greens[(i + seed + 1) % 3]), seed * 17 + i);
                Clump(m, tip - dir * s * .1f + side * s * .5f * (i % 2 == 0 ? -1f : 1f) - V(0, s * .3f, 0), V(s * .7f, s * .42f, s * .66f), Tone(GumDark), seed * 19 + i);
            }
            Vector3 top = fork + V(0, h - forkY - h * .16f, 0) + lean * .4f;
            m.Surface = ComicSurface.Wood;
            Limb(m, fork, top, tr * .5f, tr * .14f, Tone(GumBark), 5);
            float ts = h * .28f;
            Clump(m, top + V(0, ts * .1f, 0), V(ts * 1.1f, ts * .7f, ts * 1.05f), Tone(GumB), seed * 23, far ? 6 : 7);
            if (!far) Clump(m, top + V(ts * .3f, ts * .4f, ts * .1f), V(ts * .6f, ts * .42f, ts * .6f), Tone(GumC), seed * 29);
        }

        /// <summary>Jacaranda in November bloom: a dark forked trunk and a wide, low dome of purple (three violets, the
        /// lightest on top), hardly any leaves. The fallen petals are drawn by the caller on the ground (PetalCarpet).</summary>
        public static void Jacaranda(ComicMesh m, Vector3 p, float h, int seed, bool far = false)
        {
            var r = new System.Random(seed * 3571 + 5);
            float tr = .15f + h * .016f, spin = F(r, 0f, 360f), forkY = h * .36f;
            Vector3 fork = p + V(0, forkY, 0);
            m.Surface = ComicSurface.Wood;
            if (!far) m.Cylinder(p + V(0, .08f, 0), tr * 1.5f, .16f, K.R.ink, 6, .8f);
            Limb(m, p, fork, tr, tr * .75f, JacBark, far ? 6 : 8);
            int limbs = far ? 3 : 5;
            Color[] cols = { JacA, JacC, JacA, JacB, JacC };
            for (int i = 0; i < limbs; i++)
            {
                float a = (spin + i * 360f / limbs + F(r, -18f, 18f)) * Mathf.Deg2Rad;
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 tip = fork + dir * h * F(r, .3f, .42f) + V(0, h * F(r, .26f, .36f), 0);
                m.Surface = ComicSurface.Wood;
                Limb(m, fork, tip, tr * .5f, tr * .16f, JacBark, 5);
                float s = h * F(r, .3f, .38f);
                Clump(m, tip + V(0, s * .12f, 0), V(s * 1.15f, s * .7f, s * 1.1f), cols[(i + seed) % 5], seed * 7 + i, far ? 6 : 7);
                if (far) continue;
                Vector3 side = Vector3.Cross(Vector3.up, dir) * (i % 2 == 0 ? 1f : -1f);
                Clump(m, tip + dir * s * .45f + side * s * .35f - V(0, s * .12f, 0), V(s * .68f, s * .44f, s * .66f), cols[(i + seed + 2) % 5], seed * 9 + i, 6, 3);
            }
            Clump(m, fork + V(0, h * .52f, 0), V(h * .5f, h * .34f, h * .5f), JacB, seed * 31, far ? 6 : 7);
            if (far) return;
            for (int i = 0; i < 3; i++)
            {
                float a = (spin + 60f + i * 120f) * Mathf.Deg2Rad, rr = h * F(r, .22f, .34f);
                Clump(m, fork + V(Mathf.Cos(a) * rr, h * F(r, .4f, .52f), Mathf.Sin(a) * rr), V(h * .26f, h * .17f, h * .26f), i == 1 ? Petal : JacB, seed * 43 + i, 6, 3);
            }
            Clump(m, fork + V(h * .14f, h * .6f, -h * .1f), V(h * .32f, h * .22f, h * .32f), Petal, seed * 37, 6, 3);
            Clump(m, fork + V(-h * .2f, h * .3f, h * .14f), V(h * .3f, h * .18f, h * .3f), Color.Lerp(JacC, H("5E7F45"), .4f), seed * 41, 6, 3);
        }

        /// <summary>Fallen jacaranda petals: a pale purple patch on the ground (flat road space, y = the surface there).</summary>
        public static void PetalCarpet(ComicMesh road, float x, float z, float radius, float y, int seed)
        {
            road.Surface = ComicSurface.Plain;
            var r = new System.Random(seed * 131 + 7);
            for (int i = 0; i < 7; i++)
            {
                float a = F(r, 0f, 6.28f), d = F(r, 0f, radius), w = F(r, .5f, 1.2f), l = F(r, .4f, 1f);
                float cx = x + Mathf.Cos(a) * d, cz = z + Mathf.Sin(a) * d;
                K.Face(road, V(cx - w, y, cz - l), V(cx - w * .6f, y, cz + l), V(cx + w, y, cz + l * .8f), V(cx + w * .7f, y, cz - l * .9f), i % 2 == 0 ? Petal : JacB, Vector3.up, false);
            }
        }

        /// <summary>Norfolk Island pine (Stirling Square, the school grounds): a straight trunk, whorls of level boughs
        /// getting shorter towards the top, a leader.</summary>
        public static void Pine(ComicMesh m, Vector3 p, float h, int seed, bool far = false)
        {
            var r = new System.Random(seed * 131 + 7);
            float tr = .2f + h * .012f, spin = F(r, 0f, 360f);
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, h * .5f, 0), tr, h, PineBark, 6, .25f);
            int tiers = far ? 5 : 7, arms = far ? 4 : 5;
            for (int i = 0; i < tiers; i++)
            {
                float t = i / (float)(tiers - 1), y = Mathf.Lerp(h * .3f, h * .92f, t), reach = Mathf.Lerp(h * .24f, h * .07f, t);
                Color top = i % 2 == 0 ? PineDark : PineMid;
                m.Surface = ComicSurface.Foliage;
                for (int j = 0; j < arms; j++)
                {
                    float a = (spin + i * 37f + j * 360f / arms) * Mathf.Deg2Rad;
                    Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a)), side = Vector3.Cross(Vector3.up, dir);
                    Vector3 a0 = p + V(0, y, 0), a1 = a0 + dir * reach * .62f + V(0, -reach * .08f, 0), a2 = a1 + dir * reach * .38f + V(0, reach * .06f, 0);
                    float w = .4f + reach * .34f;
                    Slab(m, a0, a1, side, w * .4f, w, .16f + reach * .03f, top, PineUnder);
                    if (!far) Slab(m, a1, a2, side, w, w * .3f, .14f, Color.Lerp(top, PineLight, .5f), PineUnder);
                }
            }
            m.Surface = ComicSurface.Foliage;
            m.Cylinder(p + V(0, h * .97f, 0), h * .03f + .14f, h * .1f, PineLight, 6, .2f);
        }

        /// <summary>A rounded shrub (two blobs).</summary>
        public static void Bush(ComicMesh m, Vector3 p, float s, Color c, int seed = 0)
        {
            m.Surface = ComicSurface.Foliage;
            Clump(m, p + V(0, s * .34f, 0), V(s * 1.25f, s * .8f, s * 1.1f), c, seed * 5 + 1, 6, 4);
            Clump(m, p + V(s * .22f, s * .6f, s * .1f), V(s * .7f, s * .5f, s * .7f), Color.Lerp(c, Yellow, .18f), seed * 5 + 2, 6, 3);
        }

        /// <summary>Reeds / rushes at the water's edge: a tuft of thin leaning blades.</summary>
        public static void Reeds(ComicMesh m, Vector3 p, float h, int seed)
        {
            var r = new System.Random(seed * 17 + 3);
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < 5; i++)
            {
                Vector3 b = p + V(F(r, -.35f, .35f), 0, F(r, -.35f, .35f)), t = b + V(F(r, -.3f, .3f), h * F(r, .7f, 1.1f), F(r, -.3f, .3f));
                Limb(m, b, t, .07f, .02f, i % 2 == 0 ? H("7E9A4C") : H("A3B25E"), 4);
            }
        }

        /// <summary>A long clipped hedge in flat road space (sheared with the street), d0..d1 beyond the sidewalk edge.</summary>
        public static void Hedge(ComicMesh sloped, int s, float z0, float z1, float d0, float d1, float h, Color c)
        {
            sloped.Surface = ComicSurface.Foliage;
            K.SideBox(sloped, s, z0, z1, K.Kerb - .05f, K.Kerb + h, d0, d1, c);
        }

        /// <summary>A white picket fence in flat road space along z at x (posts, two rails, pickets every 0.5 m).</summary>
        public static void Pickets(ComicMesh sloped, float x, float z0, float z1, float y, float h, Color c)
        {
            sloped.Surface = ComicSurface.Wood;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                sloped.Box(V(x, y + h * .32f, (z + e) * .5f), V(.05f, .08f, e - z), c);
                sloped.Box(V(x, y + h * .78f, (z + e) * .5f), V(.05f, .08f, e - z), c);
            }
            for (float z = z0 + .1f; z < z1; z += .5f) sloped.Box(V(x - .04f, y + h * .5f, z), V(.04f, h, .12f), c);
            for (float z = z0; z <= z1 + .01f; z += 4f) sloped.Box(V(x, y + h * .55f, Mathf.Min(z, z1)), V(.14f, h * 1.1f, .14f), c);
        }

        /// <summary>A post-and-rail paddock fence (white) in flat road space along z at x.</summary>
        public static void PostAndRail(ComicMesh sloped, float x, float z0, float z1, float y, Color c, int rails = 3)
        {
            sloped.Surface = ComicSurface.Wood;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                for (int i = 0; i < rails; i++) sloped.Box(V(x, y + .4f + i * .4f, (z + e) * .5f), V(.06f, .12f, e - z), c);
            }
            for (float z = z0; z <= z1 + .01f; z += 2.5f) sloped.Box(V(x, y + .7f, Mathf.Min(z, z1)), V(.14f, 1.4f, .14f), c);
        }

        /// <summary>White timber bollards with a black chain between them (the school lawns, Stirling Square).</summary>
        public static void ChainBollards(ComicMesh sloped, float x, float z0, float z1, float y)
        {
            sloped.Surface = ComicSurface.Wood;
            for (float z = z0; z <= z1 + .01f; z += 2.5f)
            {
                sloped.Box(V(x, y + .38f, z), V(.2f, .76f, .2f), White);
                sloped.Cylinder(V(x, y + .82f, z), .14f, .16f, White, 4, .2f, Quaternion.Euler(0, 45, 0));
                if (z + 2.5f <= z1 + .01f)
                {
                    sloped.Surface = ComicSurface.PaintedMetal;
                    sloped.Beam(V(x, y + .6f, z), V(x, y + .44f, z + 1.25f), .045f, K.R.ink);
                    sloped.Beam(V(x, y + .44f, z + 1.25f), V(x, y + .6f, z + 2.5f), .045f, K.R.ink);
                    sloped.Surface = ComicSurface.Wood;
                }
            }
        }

        /// <summary>A park bench facing +Z at yaw 0.</summary>
        public static void Bench(ComicMesh m, Vector3 p, float yaw, Color timber)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.Wood;
            m.Box(p + q * V(0, .45f, 0), V(1.8f, .08f, .45f), timber, q);
            m.Box(p + q * V(0, .8f, .22f), V(1.8f, .35f, .07f), timber, q);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.75f, .75f }) m.Box(p + q * V(x, .4f, .05f), V(.08f, .8f, .5f), K.R.ink, q);
        }

        /// <summary>A heritage street lamp: fluted black post on a base, a cross bar, a glass lantern with a cap.</summary>
        public static void HeritageLamp(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .25f, 0), .2f, .5f, K.R.ink, 6, .7f);
            m.Cylinder(p + V(0, 2.1f, 0), .08f, 3.4f, K.R.ink, 6, .8f);
            m.Box(p + V(0, 3.5f, 0), V(.7f, .06f, .06f), K.R.ink);
            m.Cylinder(p + V(0, 3.95f, 0), .2f, .5f, K.R.cream, 6, 1.35f);
            m.Cylinder(p + V(0, 4.3f, 0), .32f, .18f, K.R.ink, 6, .2f);
            m.Cylinder(p + V(0, 4.45f, 0), .05f, .16f, K.R.ink, 4);
        }

        /// <summary>A modern path light: a slim grey pole with a flat LED head over the path (yaw 0 = arm towards -X).</summary>
        public static void PathLight(ComicMesh m, Vector3 p, float yaw = 0f)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .1f, 0), .16f, .2f, SteelDark, 6);
            m.Cylinder(p + V(0, 2.8f, 0), .07f, 5.6f, Steel, 6, .8f);
            m.Box(p + q * V(-.5f, 5.55f, 0), V(1.1f, .07f, .1f), Steel, q);
            m.Box(p + q * V(-.95f, 5.5f, 0), V(.5f, .1f, .26f), SteelDark, q);
            m.Box(p + q * V(-.95f, 5.44f, 0), V(.4f, .03f, .2f), K.R.cream, q);
        }

        /// <summary>A sign board on two posts facing the way rot turns -Z: a coloured board with a pale border and pixel text.</summary>
        public static void SignBoard(ComicMesh m, Vector3 p, float yaw, string text, float px, Color board, Color letters, float height = 2.2f)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            float w = TextWidth(text, px) + px * 6f, bh = px * 9f;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -w * .4f, w * .4f }) m.Box(p + q * V(x, height * .5f, .06f), V(.1f, height, .1f), Steel, q);
            m.Box(p + q * V(0, height - bh * .5f, 0), V(w + .1f, bh + .1f, .05f), letters, q);
            m.Box(p + q * V(0, height - bh * .5f, -.02f), V(w, bh, .05f), board, q);
            Text(m, text, p + q * V(0, height - bh * .5f, -.056f), q, px, letters);
        }

        // ================================================================ ground and street surfaces

        /// <summary>The street cross-section (road, kerbs, sidewalks) like CbdQuayKit.StreetSurface, with the surface
        /// kinds chosen by the caller (red asphalt path, brick paving, grass verge) and optional paving joints.</summary>
        public static void Street(ComicMesh m, float length, Color road, ComicSurface roadSurface, Color paveL, Color paveR, ComicSurface paveSurface, Color kerbCap, bool joints, float skipFrom = -1f, float skipTo = -1f)
        {
            float h = K.Kerb, sw = K.Side, w = K.Half;
            var cuts = new List<float>();
            for (float z = 0; z < length - .01f; z += 4f) cuts.Add(z);
            cuts.Add(length);
            if (skipFrom >= 0f) { cuts.Add(skipFrom); cuts.Add(skipTo); }
            cuts = cuts.Distinct().OrderBy(z => z).ToList();
            for (int ci = 0; ci + 1 < cuts.Count; ci++)
            {
                float z0 = cuts[ci], z1 = cuts[ci + 1];
                if (z1 - z0 < .01f || skipFrom >= 0f && z0 >= skipFrom - .01f && z1 <= skipTo + .01f) continue;
                m.Surface = roadSurface;
                K.Face(m, V(-w, 0, z0), V(-w, 0, z1), V(w, 0, z1), V(w, 0, z0), road, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    Color pavement = s < 0 ? paveL : paveR, joint = Color.Lerp(K.R.ink, pavement, .55f);
                    float i0 = s * w, o0 = s * (w + sw);
                    Vector3 inward = new Vector3(-s, 0, 0), outward = new Vector3(s, 0, 0);
                    m.Surface = paveSurface;
                    K.Face(m, V(i0, h, z0), V(i0, h, z1), V(o0, h, z1), V(o0, h, z0), pavement, Vector3.up, false);
                    K.Face(m, V(i0, -.05f, z0), V(i0, -.05f, z1), V(i0, h, z1), V(i0, h, z0), kerbCap, inward, false);
                    K.Face(m, V(o0, -2f, z0), V(o0, -2f, z1), V(o0, h, z1), V(o0, h, z0), pavement, outward, false);
                    m.Surface = ComicSurface.Plain;
                    float c0 = s * (w + .24f), top = h + .04f;
                    K.Face(m, V(i0, top, z0), V(i0, top, z1), V(c0, top, z1), V(c0, top, z0), kerbCap, Vector3.up, false);
                    K.Face(m, V(i0, h, z0), V(i0, h, z1), V(i0, top, z1), V(i0, top, z0), kerbCap, inward, false);
                    K.Face(m, V(c0, h, z0), V(c0, h, z1), V(c0, top, z1), V(c0, top, z0), kerbCap, outward, false);
                    if (joints)
                        for (int k = 1; k < 4; k++) K.Flat(m, s * (w + k) - .0125f, s * (w + k) + .0125f, s * (w + k) - .0125f, s * (w + k) + .0125f, z0, z1, h + .025f, joint);
                }
                if (joints)
                    for (float z = Mathf.Ceil(z0 / 2f) * 2f; z < z1 - .01f; z += 2f)
                        for (int s = -1; s <= 1; s += 2) K.Flat(m, s * w, s * (w + sw), s * w, s * (w + sw), z - .0125f, z + .0125f, h + .025f, Color.Lerp(K.R.ink, s < 0 ? paveL : paveR, .55f));
            }
            foreach (float ze in new[] { 0f, length })
            {
                Vector3 normal = ze <= 0f ? Vector3.back : Vector3.forward;
                m.Surface = roadSurface;
                K.Face(m, V(-w, -.8f, ze), V(w, -.8f, ze), V(w, 0, ze), V(-w, 0, ze), road, normal, false);
                m.Surface = paveSurface;
                for (int s = -1; s <= 1; s += 2) K.Face(m, V(s * w, -2f, ze), V(s * (w + sw), -2f, ze), V(s * (w + sw), h, ze), V(s * w, h, ze), s < 0 ? paveL : paveR, normal, false);
            }
        }

        /// <summary>The Principal Shared Path beside the Midland line (PedestrianMall: no cars): red asphalt with a
        /// dashed white centre line and solid edge lines, a mown grass verge on the left, a gravel strip on the rail side.</summary>
        public static void SharedPath(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f)
        {
            d.Type = RoadType.PedestrianMall;
            Street(d.Road, d.Length, RedPath, ComicSurface.Asphalt, Verge, Verge, ComicSurface.Foliage, KerbCap, false, skipFrom, skipTo);
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            // mown stripes on both verges, a worn gravel edge beside the path
            for (float z = 2f; z < d.Length - 3f; z += 8f)
            {
                if (skipFrom >= 0f && z + 4f > skipFrom && z < skipTo) continue;
                foreach (int s in new[] { -1, 1 })
                    K.Face(m, V(s * (K.Half + .9f), K.Kerb + .012f, z), V(s * (K.Half + .9f), K.Kerb + .012f, z + 4f), V(s * (K.Edge - .1f), K.Kerb + .012f, z + 4f), V(s * (K.Edge - .1f), K.Kerb + .012f, z), Color.Lerp(Verge, LawnLight, .6f), Vector3.up, false);
            }
            for (float z = 0f; z < d.Length - .01f; z += 4f)
            {
                if (skipFrom >= 0f && z + 4f > skipFrom && z < skipTo) continue;
                foreach (int s in new[] { -1, 1 })
                    K.Face(m, V(s * (K.Half + .26f), K.Kerb + .014f, z), V(s * (K.Half + .26f), K.Kerb + .014f, z + 4f), V(s * (K.Half + .6f), K.Kerb + .014f, z + 4f), V(s * (K.Half + .6f), K.Kerb + .014f, z), Gravel, Vector3.up, false);
            }
            for (float z = 1f; z < d.Length - 2f; z += 5f)
            {
                if (skipFrom >= 0f && z + 2.6f > skipFrom && z < skipTo) continue;
                K.Face(m, V(-.07f, .03f, z), V(-.07f, .03f, z + 2.6f), V(.07f, .03f, z + 2.6f), V(.07f, .03f, z), PathLine, Vector3.up, false);
            }
            for (float z = 0f; z < d.Length - .01f; z += 4f)
            {
                if (skipFrom >= 0f && z + 4f > skipFrom && z < skipTo) continue;
                foreach (int s in new[] { -1, 1 })
                    K.Face(m, V(s * (K.Half - .38f), .025f, z), V(s * (K.Half - .38f), .025f, z + 4f), V(s * (K.Half - .26f), .025f, z + 4f), V(s * (K.Half - .26f), .025f, z), PathLine, Vector3.up, false);
            }
        }

        /// <summary>James St and Terrace Rd: asphalt with the centre line, red brick paved footpaths (Guildford's paving).</summary>
        public static void BrickStreet(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f)
        {
            Street(d.Road, d.Length, K.R.asphalt, ComicSurface.Asphalt, PaveBrick, PaveBrick, ComicSurface.Paving, KerbCap, true, skipFrom, skipTo);
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            for (int s = -1; s <= 1; s += 2)
                for (float z = 0f; z < d.Length - .01f; z += 5f)
                {
                    if (skipFrom >= 0f && z + 3f > skipFrom && z < skipTo) continue;
                    K.Flat(m, s * .12f - .05f, s * .12f + .05f, s * .12f - .05f, s * .12f + .05f, z, Mathf.Min(z + 3f, d.Length), .035f, K.R.yellow);
                }
            for (float z = 0f; z < d.Length - .01f; z += 4f)
            {
                if (skipFrom >= 0f && z + 4f > skipFrom && z < skipTo) continue;
                foreach (int s in new[] { -1, 1 })
                {
                    K.Flat(m, s * (K.Half - .19f), s * (K.Half - .11f), s * (K.Half - .19f), s * (K.Half - .11f), z, z + 4f, .0305f, K.R.cream);
                    // a darker header course along the kerb and the building line
                    K.Flat(m, s * (K.Half + .3f), s * (K.Half + .62f), s * (K.Half + .3f), s * (K.Half + .62f), z, z + 4f, K.Kerb + .028f, PaveBrickDark);
                    K.Flat(m, s * (K.Edge - .34f), s * (K.Edge - .02f), s * (K.Edge - .34f), s * (K.Edge - .02f), z, z + 4f, K.Kerb + .028f, PaveBrickDark);
                }
            }
        }

        /// <summary>Where a street meets the shared path (the start of a chunk whose road kind differs from the one before
        /// it): a band of pale granite setts across the road with darker joints, so the change of surface has a reason.</summary>
        public static void Threshold(CqChunkDraft d, float z0 = 0f, float z1 = 2.4f)
        {
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Paving;
            K.Face(m, V(-K.Half, .012f, z0), V(-K.Half, .012f, z1), V(K.Half, .012f, z1), V(K.Half, .012f, z0), H("C9C3B4"), Vector3.up, false);
            m.Surface = ComicSurface.Plain;
            foreach (float z in new[] { z0, z1 - .3f })
                K.Face(m, V(-K.Half, .019f, z), V(-K.Half, .019f, z + .3f), V(K.Half, .019f, z + .3f), V(K.Half, .019f, z), H("8E897C"), Vector3.up, false);
            for (float x = -K.Half + .9f; x < K.Half - .2f; x += 1f)
                K.Face(m, V(x, .019f, z0 + .7f), V(x, .019f, z1 - .7f), V(x + .5f, .019f, z1 - .7f), V(x + .5f, .019f, z0 + .7f), PathLine, Vector3.up, false);
        }

        /// <summary>Ground beyond the sidewalk edge on side s (flat road space): d0..d1 metres out.</summary>
        public static void Ground(CqChunkDraft d, int s, float z0, float z1, float d0, float d1, Color c, ComicSurface surface, float y = .02f)
        {
            d.Road.Surface = surface;
            K.SideBox(d.Road, s, z0, z1, -2.4f, K.Kerb + y, d0, d1, c);
        }

        /// <summary>A flat patch on the ground (flat road space, no ink): x0..x1, z0..z1 at height y above the road.</summary>
        public static void Patch(ComicMesh road, float x0, float x1, float z0, float z1, float y, Color c, ComicSurface surface = ComicSurface.Plain)
        {
            road.Surface = surface;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.Face(road, V(x0, y, z), V(x0, y, e), V(x1, y, e), V(x1, y, z), c, Vector3.up, false);
            }
        }

        /// <summary>Height field in flat road space (x0..x1, z0..z1), cells of about dx by dz.</summary>
        public static void Terrain(ComicMesh m, float x0, float x1, float z0, float z1, float dx, float dz, Func<float, float, float> h, Func<float, float, float, Color> col, ComicSurface surface)
        {
            m.Surface = surface;
            int nx = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(x1 - x0) / dx)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / dz));
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float xa = Mathf.Lerp(x0, x1, (float)i / nx), xb = Mathf.Lerp(x0, x1, (float)(i + 1) / nx);
                    float za = Mathf.Lerp(z0, z1, (float)j / nz), zb = Mathf.Lerp(z0, z1, (float)(j + 1) / nz);
                    Vector3 a = V(xa, h(xa, za), za), b = V(xa, h(xa, zb), zb), c = V(xb, h(xb, zb), zb), e = V(xb, h(xb, za), za);
                    K.Face(m, a, b, c, e, col((xa + xb) * .5f, (za + zb) * .5f, (a.y + b.y + c.y + e.y) * .25f), Vector3.up, false);
                }
        }

        // ================================================================ the Midland line beside the road (right side)

        /// <summary>The rail corridor from z0 to z1 (flat road space, sheared with the street): a gravel verge and the
        /// steel corridor fence, the ballast bed, two narrow-gauge tracks (concrete sleepers, rails), the far fence.</summary>
        public static void RailCorridor(CqChunkDraft d, float z0, float z1, bool nearFence = true, bool bed = true)
        {
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            K.SideBox(m, 1, z0, z1, -2.4f, K.Kerb + .02f, 0f, BedX0 - K.Edge, Gravel);
            if (bed)
            {
                m.Surface = ComicSurface.Concrete;
                K.SideBox(m, 1, z0, z1, -2.4f, BedTop, BedX0 - K.Edge, BedX1 - K.Edge, Ballast);
                K.SideBox(m, 1, z0, z1, -2.4f, K.Kerb + .02f, BedX1 - K.Edge, BedX1 - K.Edge + 1.6f, Gravel);
                Tracks(m, z0, z1, BedTop);
            }
            if (nearFence) Fence(d, FenceX, z0, z1, K.Kerb, 1.8f);
            Fence(d, BedX1 + .6f, z0, z1, K.Kerb, 1.8f, false);
        }

        /// <summary>Both tracks on a bed whose top is at y: sleepers (flat, no ink) and four rails.</summary>
        public static void Tracks(ComicMesh m, float z0, float z1, float y)
        {
            foreach (float xc in new[] { Track1, Track2 })
            {
                m.Surface = ComicSurface.Concrete;
                for (float z = Mathf.Ceil(z0 / .8f) * .8f + .2f; z < z1 - .1f; z += .8f)
                    K.Face(m, V(xc - 1.05f, y + .03f, z - .13f), V(xc - 1.05f, y + .03f, z + .13f), V(xc + 1.05f, y + .03f, z + .13f), V(xc + 1.05f, y + .03f, z - .13f), Sleeper, Vector3.up, false);
                m.Surface = ComicSurface.Metal;
                foreach (float off in new[] { -.535f, .535f })
                    for (float z = z0; z < z1 - .01f; z += 8f)
                    {
                        float e = Mathf.Min(z1, z + 8f);
                        K.MinMaxBox(m, V(xc + off - .05f, y + .03f, z), V(xc + off + .05f, y + (RailY - BedTop), e), Rail);
                        K.Face(m, V(xc + off - .035f, y + (RailY - BedTop) + .004f, z), V(xc + off - .035f, y + (RailY - BedTop) + .004f, e), V(xc + off + .035f, y + (RailY - BedTop) + .004f, e), V(xc + off + .035f, y + (RailY - BedTop) + .004f, z), RailTop, Vector3.up, false);
                    }
            }
        }

        /// <summary>The galvanised corridor fence along z at x: posts (ink outline), three rails and thin mesh wires (no ink).</summary>
        public static void Fence(CqChunkDraft d, float x, float z0, float z1, float y, float h, bool posts = true)
        {
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Metal;
            for (float z = z0; z < z1 - .01f; z += 8f)
            {
                float e = Mathf.Min(z1, z + 8f);
                foreach (float t in new[] { .12f, .55f, .98f }) m.Box(V(x, y + h * t, (z + e) * .5f), V(.05f, .05f, e - z), Steel);
                m.Box(V(x, y + h * .33f, (z + e) * .5f), V(.02f, .02f, e - z), SteelDark);
                m.Box(V(x, y + h * .77f, (z + e) * .5f), V(.02f, .02f, e - z), SteelDark);
            }
            ComicMesh f = posts ? d.SlopedFrontage : d.Road;
            f.Surface = ComicSurface.Metal;
            for (float z = z0; z <= z1 - .01f; z += 2.5f) f.Box(V(x, y + h * .5f, z + .05f), V(.09f, h + .1f, .09f), posts ? Steel : SteelDark);
        }

        /// <summary>An overhead-wire gantry at z: two steel masts on concrete footings, a lattice cross beam, a drop tube,
        /// insulators and a registration arm over each track.</summary>
        public static void Gantry(CqChunkDraft d, float z, bool nearMast = true)
        {
            ComicMesh m = d.SlopedFrontage;
            float xa = BedX0 + .35f, xb = BedX1 - .35f, top = 7.3f;
            if (!nearMast)
            {
                // a cantilever from the far mast only (where a platform or a building stands on the near side)
                xa = Track1 - 1.6f;
                m.Surface = ComicSurface.Metal;
                m.Beam(V(xb, top + 1.5f, z), V(xa + .2f, top + .05f, z), .07f, SteelDark);
                m.Box(V(xb, top + .9f, z), V(.26f, 1.5f, .22f), Steel);
            }
            foreach (float x in new[] { xa, xb })
            {
                if (!nearMast && x == xa) continue;
                m.Surface = ComicSurface.Concrete;
                m.Box(V(x, BedTop + .25f, z), V(.7f, .5f, .7f), Concrete);
                m.Surface = ComicSurface.Metal;
                m.Box(V(x, top * .5f + .2f, z), V(.26f, top, .22f), Steel);
                m.Box(V(x, top * .5f + .2f, z), V(.1f, top, .3f), SteelDark);
            }
            m.Box(V((xa + xb) * .5f, top, z), V(xb - xa + .5f, .14f, .2f), Steel);
            m.Box(V((xa + xb) * .5f, top - .55f, z), V(xb - xa + .3f, .12f, .18f), Steel);
            int bays = 8;
            for (int i = 0; i < bays; i++)
            {
                float x0 = Mathf.Lerp(xa, xb, (float)i / bays), x1 = Mathf.Lerp(xa, xb, (float)(i + 1) / bays);
                m.Beam(V(x0, i % 2 == 0 ? top - .55f : top, z), V(x1, i % 2 == 0 ? top : top - .55f, z), .07f, SteelDark);
            }
            foreach (float xc in new[] { Track1, Track2 })
            {
                m.Surface = ComicSurface.Metal;
                m.Box(V(xc - .9f, top - 1.3f, z), V(.1f, 1.5f, .1f), Steel);
                m.Beam(V(xc - .9f, top - 1.95f, z), V(xc + .15f, 5.45f, z), .06f, SteelDark);
                m.Beam(V(xc - .9f, top - .75f, z), V(xc, 6.55f, z), .06f, SteelDark);
                m.Surface = ComicSurface.Plastic;
                m.Box(V(xc - .5f, top - 1.76f, z), V(.3f, .16f, .16f), Insulator);
                m.Box(V(xc - .45f, top - .68f, z), V(.3f, .16f, .16f), Insulator);
            }
        }

        /// <summary>Contact wire, sagging messenger wire and droppers over both tracks, from z0 to z1, hung from the
        /// gantries at every mast z (flat road space, no ink).</summary>
        public static void Wires(CqChunkDraft d, float z0, float z1, float mastZ = 20f, float span = 40f)
        {
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Metal;
            float Messenger(float z)
            {
                float t = Mathf.Abs(Mathf.Repeat(z - mastZ + span * .5f, span) - span * .5f) / (span * .5f); // 0 at a mast, 1 mid-span
                return 5.8f + .75f * (1f - t) * (1f - t);
            }
            foreach (float xc in new[] { Track1, Track2 })
            {
                for (float z = z0; z < z1 - .01f; z += 10f) m.Box(V(xc, 5.4f, (z + Mathf.Min(z1, z + 10f)) * .5f), V(.045f, .045f, Mathf.Min(z1, z + 10f) - z), Wire);
                for (float z = z0; z < z1 - .01f; z += 5f)
                {
                    float e = Mathf.Min(z1, z + 5f);
                    m.Beam(V(xc, Messenger(z), z), V(xc, Messenger(e), e), .04f, Wire);
                    if (z > z0 + .1f) m.Box(V(xc, (Messenger(z) + 5.4f) * .5f, z), V(.025f, Messenger(z) - 5.4f, .025f), Wire);
                }
            }
        }

        /// <summary>What is beyond the far rail fence: a paling fence, back-garden trees and house roofs (no outline).</summary>
        public static void RailBackdrop(CqChunkDraft d, float z0, float z1, int seed, bool roofs = true)
        {
            ComicMesh m = d.Deep;
            var r = new System.Random(seed * 53 + 11);
            float x0 = BedX1 + 1.6f;
            d.Road.Surface = ComicSurface.Foliage;
            K.SideBox(d.Road, 1, z0, z1, -2.4f, K.Kerb, x0 - K.Edge, 70f, LawnDry);
            m.Surface = ComicSurface.Wood;
            for (float z = z0; z < z1 - .01f; z += 8f)
            {
                float e = Mathf.Min(z1, z + 8f), zc = (z + e) * .5f;
                m.Box(V(x0 + .4f, d.Ground(zc) + .9f, zc), V(.12f, 1.9f, e - z), Color.Lerp(Timber, Cream, F(r, .1f, .5f)));
            }
            Color[] roofCols = { Tile, TileDark, Iron, IronDark, H("6E5A50") };
            Color[] walls = { Cream, White, H("E3D2B4"), Brick, H("D9C9A8") };
            for (float z = z0 + F(r, 1f, 4f); z < z1 - 3f; z += F(r, 9f, 13f))
            {
                float x = x0 + F(r, 5f, 9f);
                Gum(m, V(x, d.Ground(z), z), F(r, 9f, 15f), seed + (int)(z * 3f), true);
                if (!roofs) continue;
                float hx = x0 + F(r, 13f, 19f), hz = z + F(r, 3f, 6f), w = F(r, 8f, 11f), h = F(r, 3f, 3.6f);
                if (hz > z1 - 3f) continue;
                m.Surface = ComicSurface.Plaster;
                m.Box(V(hx, d.Ground(hz) + h * .5f - .5f, hz), V(w, h + 1f, w * .8f), walls[r.Next(walls.Length)]);
                m.Surface = ComicSurface.Metal;
                m.Cylinder(V(hx, d.Ground(hz) + h + 1f, hz), w * .76f, 2f, roofCols[r.Next(roofCols.Length)], 4, .12f, Quaternion.Euler(0, 45, 0));
                Chimney(m, V(hx + w * .2f, d.Ground(hz) + h + .6f, hz - w * .1f), 1.5f, Brick, .5f);
            }
        }

        /// <summary>A B-series style suburban electric train on a track (flat road space; cars 24 m). White and silver
        /// with a green band, dark window band, doors, roof equipment and a pantograph on the first car. No logos.</summary>
        public static void Train(ComicMesh m, float xc, float zFront, int cars, bool frontTowardsRider = true)
        {
            const float L = 24f, w = 2.9f, y0 = RailY + .05f;
            for (int car = 0; car < cars; car++)
            {
                float z0 = zFront + car * (L + .6f), z1 = z0 + L, zc = (z0 + z1) * .5f;
                m.Surface = ComicSurface.Metal;
                m.Box(V(xc, y0 + .45f, zc), V(w - .5f, .5f, L - 1.2f), TrainDark);
                foreach (float bz in new[] { z0 + 3.6f, z1 - 3.6f })
                {
                    m.Box(V(xc, y0 + .3f, bz), V(w - .3f, .56f, 3.2f), H("3B434B"));
                    foreach (float wz in new[] { -1f, 1f }) m.Cylinder(V(xc - w * .5f + .2f, y0 + .36f, bz + wz), .4f, .12f, H("596069"), 8, 1f, Quaternion.Euler(0, 0, 90));
                }
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(xc, y0 + 2.2f, zc), V(w, 3f, L), TrainWhite);
                m.Box(V(xc, y0 + 3.82f, zc), V(w - .5f, .26f, L - .6f), TrainGrey);
                // the side towards the road (-X): green band, the dark window band, two pairs of doors
                float sx = xc - w * .5f - .012f;
                Vector3 n = Vector3.left;
                K.Face(m, V(sx, y0 + 1.25f, z0 + .1f), V(sx, y0 + 1.25f, z1 - .1f), V(sx, y0 + 1.6f, z1 - .1f), V(sx, y0 + 1.6f, z0 + .1f), TrainGreen, n, false);
                m.Surface = ComicSurface.Glass;
                K.Face(m, V(sx, y0 + 2.05f, z0 + 1.6f), V(sx, y0 + 2.05f, z1 - 1.6f), V(sx, y0 + 3.05f, z1 - 1.6f), V(sx, y0 + 3.05f, z0 + 1.6f), TrainDark, n);
                foreach (float dz in new[] { z0 + 6.2f, z1 - 6.2f })
                {
                    m.Surface = ComicSurface.PaintedMetal;
                    K.Face(m, V(sx - .01f, y0 + .78f, dz - .85f), V(sx - .01f, y0 + .78f, dz + .85f), V(sx - .01f, y0 + 3.2f, dz + .85f), V(sx - .01f, y0 + 3.2f, dz - .85f), H("DADFE2"), n);
                    K.Face(m, V(sx - .02f, y0 + .78f, dz - .03f), V(sx - .02f, y0 + .78f, dz + .03f), V(sx - .02f, y0 + 3.2f, dz + .03f), V(sx - .02f, y0 + 3.2f, dz - .03f), TrainDark, n, false);
                    m.Surface = ComicSurface.Glass;
                    foreach (float o in new[] { -.43f, .43f })
                        K.Face(m, V(sx - .02f, y0 + 2f, dz + o - .28f), V(sx - .02f, y0 + 2f, dz + o + .28f), V(sx - .02f, y0 + 3f, dz + o + .28f), V(sx - .02f, y0 + 3f, dz + o - .28f), TrainDark, n, false);
                    m.Surface = ComicSurface.PaintedMetal;
                    K.Face(m, V(sx - .02f, y0 + 1.25f, dz - .85f), V(sx - .02f, y0 + 1.25f, dz + .85f), V(sx - .02f, y0 + 1.6f, dz + .85f), V(sx - .02f, y0 + 1.6f, dz - .85f), Yellow, n, false);
                }
                // roof equipment
                m.Surface = ComicSurface.Metal;
                m.Box(V(xc, y0 + 4.08f, z0 + 6f), V(1.7f, .3f, 3.4f), H("AEB5BB"));
                m.Box(V(xc, y0 + 4.08f, z1 - 6f), V(1.7f, .3f, 3.4f), H("AEB5BB"));
                if (car == 0)
                {
                    m.Box(V(xc, y0 + 4.02f, zc), V(1.2f, .14f, 1.6f), TrainDark);
                    m.Beam(V(xc, y0 + 4.05f, zc - .7f), V(xc, y0 + 4.75f, zc + .3f), .07f, TrainDark);
                    m.Beam(V(xc, y0 + 4.75f, zc + .3f), V(xc, 5.33f, zc - .4f), .07f, TrainDark);
                    m.Box(V(xc, 5.36f, zc - .4f), V(1.5f, .06f, .3f), TrainDark);
                }
                // the cab ends: dark windscreen, green lower panel, lights
                foreach (int end in new[] { -1, 1 })
                {
                    if (end < 0 && car != 0 || end > 0 && car != cars - 1) continue;
                    float ze = end < 0 ? z0 - .012f : z1 + .012f;
                    Vector3 en = end < 0 ? Vector3.back : Vector3.forward;
                    m.Surface = ComicSurface.Glass;
                    K.Face(m, V(xc - w * .5f + .25f, y0 + 2.05f, ze), V(xc + w * .5f - .25f, y0 + 2.05f, ze), V(xc + w * .5f - .25f, y0 + 3.3f, ze), V(xc - w * .5f + .25f, y0 + 3.3f, ze), TrainDark, en);
                    m.Surface = ComicSurface.PaintedMetal;
                    K.Face(m, V(xc - w * .5f + .1f, y0 + .8f, ze), V(xc + w * .5f - .1f, y0 + .8f, ze), V(xc + w * .5f - .1f, y0 + 1.6f, ze), V(xc - w * .5f + .1f, y0 + 1.6f, ze), TrainGreen, en, false);
                    foreach (float lx in new[] { -1f, 1f })
                        K.Face(m, V(xc + lx - .18f, y0 + 1.7f, ze * 1f + end * .004f), V(xc + lx + .18f, y0 + 1.7f, ze + end * .004f), V(xc + lx + .18f, y0 + 1.92f, ze + end * .004f), V(xc + lx - .18f, y0 + 1.92f, ze + end * .004f), end < 0 == frontTowardsRider ? K.R.cream : K.R.red, en, false);
                }
            }
        }

        /// <summary>A side platform between the path and the near track (flat road space): concrete wall, red asphalt
        /// top with a white edge and the yellow tactile line, a fence along the back.</summary>
        public static void Platform(CqChunkDraft d, float z0, float z1, bool farSide = false)
        {
            ComicMesh m = d.SlopedFrontage;
            float x0 = farSide ? Track2 + 1.5f : K.Edge + 1.2f, x1 = farSide ? BedX1 + 1.2f : Track1 - 1.5f, top = RailY + .95f;
            m.Surface = ComicSurface.Concrete;
            for (float z = z0; z < z1 - .01f; z += 8f)
            {
                float e = Mathf.Min(z1, z + 8f);
                K.MinMaxBox(m, V(x0, -.6f, z), V(x1, top - .1f, e), Concrete);
                m.Surface = ComicSurface.Asphalt;
                K.MinMaxBox(m, V(x0 - .06f, top - .1f, z), V(x1 + .06f, top, e), PlatformRed);
                m.Surface = ComicSurface.Concrete;
            }
            ComicMesh r = d.Road;
            float edge = farSide ? x0 : x1, s = farSide ? 1f : -1f;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                r.Surface = ComicSurface.Plain;
                K.Face(r, V(edge, top + .012f, z), V(edge, top + .012f, e), V(edge + s * .3f, top + .012f, e), V(edge + s * .3f, top + .012f, z), White, Vector3.up, false);
                K.Face(r, V(edge + s * .55f, top + .012f, z), V(edge + s * .55f, top + .012f, e), V(edge + s * .95f, top + .012f, e), V(edge + s * .95f, top + .012f, z), Yellow, Vector3.up, false);
            }
        }

        /// <summary>The wedge of an access ramp up onto the near platform, from z0 (path level) to z1 (platform level).</summary>
        public static void PlatformRamp(ComicMesh m, float z0, float z1)
        {
            float x0 = K.Edge + 1.2f, x1 = Track1 - 1.5f, top = RailY + .95f, y = K.Kerb;
            m.Surface = ComicSurface.Concrete;
            K.Face(m, V(x0, y, z0), V(x0, top, z1), V(x1, top, z1), V(x1, y, z0), Concrete, Vector3.up);
            K.TriFacing(m, V(x0, y, z0), V(x0, top, z1), V(x0, y, z1), ConcreteDark, Vector3.left);
            K.TriFacing(m, V(x1, y, z0), V(x1, top, z1), V(x1, y, z1), ConcreteDark, Vector3.right);
            K.Face(m, V(x0, -.6f, z0), V(x0, -.6f, z1), V(x0, y, z1), V(x0, y, z0), ConcreteDark, Vector3.left);
        }

        /// <summary>A small platform shelter (flat road space): steel posts, a skillion roof, a painted brick back wall,
        /// a bench and the station name board.</summary>
        public static void Shelter(ComicMesh m, float xBack, float zc, float len, float baseY, Color wall, Color roof, Color post, string name)
        {
            float x1 = xBack + 1.75f, h = 2.7f;
            m.Surface = ComicSurface.Brick;
            K.MinMaxBox(m, V(xBack, baseY, zc - len * .5f + .3f), V(xBack + .22f, baseY + 2.2f, zc + len * .5f - .3f), wall);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { zc - len * .5f + .15f, zc, zc + len * .5f - .15f })
            {
                m.Box(V(xBack + .11f, baseY + h * .5f, z), V(.14f, h, .14f), post);
                m.Box(V(x1 - .15f, baseY + h * .5f - .1f, z), V(.14f, h - .2f, .14f), post);
            }
            m.Surface = ComicSurface.Metal;
            K.Face(m, V(xBack - .4f, baseY + h + .25f, zc - len * .5f - .3f), V(xBack - .4f, baseY + h + .25f, zc + len * .5f + .3f), V(x1 + .3f, baseY + h - .2f, zc + len * .5f + .3f), V(x1 + .3f, baseY + h - .2f, zc - len * .5f - .3f), roof, Vector3.up);
            K.Face(m, V(xBack - .4f, baseY + h + .13f, zc - len * .5f - .3f), V(xBack - .4f, baseY + h + .13f, zc + len * .5f + .3f), V(x1 + .3f, baseY + h - .32f, zc + len * .5f + .3f), V(x1 + .3f, baseY + h - .32f, zc - len * .5f - .3f), Color.Lerp(roof, K.R.ink, .4f), Vector3.down);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(x1 + .3f, baseY + h - .26f, zc), V(.08f, .16f, len + .6f), post);
            m.Box(V(xBack - .4f, baseY + h + .19f, zc), V(.08f, .16f, len + .6f), post);
            m.Surface = ComicSurface.Metal;
            m.Box(V(xBack + .65f, baseY + .45f, zc), V(.5f, .07f, len * .55f), Steel);
            m.Box(V(xBack + .36f, baseY + .78f, zc), V(.06f, .5f, len * .55f), Steel);
            if (string.IsNullOrEmpty(name)) return;
            // the name board hangs under the roof edge facing the track side and the path side
            float px = .07f, w = TextWidth(name, px) + .5f;
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(xBack - .06f, baseY + 1.65f, zc), V(.06f, .62f, w), H("21456E"));
            Text(m, name, V(xBack - .1f, baseY + 1.65f, zc), Quaternion.Euler(0, 90f, 0), px, White);
        }

        /// <summary>Platform furniture on the near platform (flat road space): lamp posts with twin heads at the given
        /// z positions, a ticket machine and a bin by the first one.</summary>
        public static void PlatformFurniture(ComicMesh m, float top, params float[] lampZ)
        {
            float x = K.Edge + 1.55f;
            foreach (float z in lampZ)
            {
                m.Surface = ComicSurface.PaintedMetal;
                m.Cylinder(V(x, top + 2.4f, z), .06f, 4.8f, Steel, 6, .8f);
                m.Box(V(x + .5f, top + 4.75f, z), V(1.3f, .06f, .08f), Steel);
                m.Box(V(x + 1.05f, top + 4.7f, z), V(.5f, .1f, .24f), SteelDark);
                m.Box(V(x + 1.05f, top + 4.64f, z), V(.4f, .03f, .18f), K.R.cream);
            }
            if (lampZ.Length == 0) return;
            float tz = lampZ[0] + 1.2f;
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(x + .1f, top + .85f, tz), V(.5f, 1.7f, .7f), TrainGreen);
            m.Box(V(x + .37f, top + 1.2f, tz), V(.04f, .5f, .5f), TrainDark);
            m.Box(V(x + .37f, top + .75f, tz), V(.04f, .16f, .3f), Yellow);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(V(x + .1f, top + .45f, tz + 1.3f), .26f, .9f, SteelDark, 8);
            m.Cylinder(V(x + .1f, top + .93f, tz + 1.3f), .29f, .08f, Yellow, 8);
        }

        /// <summary>The station name board beside the path (faces the path): a navy board with white pixel letters on posts.</summary>
        public static void StationSign(ComicMesh m, Vector3 p, string name)
        {
            float px = Mathf.Min(.1f, 3.4f / Mathf.Max(1f, TextWidth(name, 1f)));
            float w = TextWidth(name, px) + .8f;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { -w * .42f, w * .42f }) m.Box(p + V(.06f, 1.5f, z), V(.12f, 3f, .12f), Steel);
            m.Box(p + V(0, 2.55f, 0), V(.08f, 1f, w), H("21456E"));
            m.Box(p + V(-.02f, 2.03f, 0), V(.09f, .1f, w), TrainGreen);
            Text(m, name, p + V(-.05f, 2.62f, 0), Quaternion.Euler(0, 90f, 0), px, White);
        }

        /// <summary>A steel footbridge over the tracks at z (flat road space): a mesh-sided deck on two portal piers, and
        /// on the path side a long ramp running back along the fence (towards -Z) with yellow handrails. mural = the
        /// painted panels under the ramp (Success Hill).</summary>
        public static void Footbridge(CqChunkDraft d, float z, float rampLength, bool mural)
        {
            ComicMesh m = d.SlopedFrontage;
            float x0 = K.Edge + .15f, x1 = BedX1 + 2.4f, deck = 6.3f, w = 2.2f;
            m.Surface = ComicSurface.Metal;
            m.Box(V((x0 + x1) * .5f, deck - .2f, z), V(x1 - x0, .4f, w), SteelDark);
            m.Surface = ComicSurface.Concrete;
            m.Box(V((x0 + x1) * .5f, deck + .03f, z), V(x1 - x0, .08f, w - .1f), Concrete);
            foreach (float zz in new[] { z - w * .5f, z + w * .5f })
            {
                m.Surface = ComicSurface.Metal;
                m.Box(V((x0 + x1) * .5f, deck + 1.55f, zz), V(x1 - x0, .09f, .09f), Steel);
                m.Box(V((x0 + x1) * .5f, deck + .75f, zz), V(x1 - x0, .05f, .05f), Steel);
                for (float x = x0; x <= x1 + .01f; x += 1.9f) m.Box(V(x, deck + .8f, zz), V(.08f, 1.6f, .08f), Steel);
                K.Face(m, V(x0, deck + .1f, zz), V(x1, deck + .1f, zz), V(x1, deck + 1.5f, zz), V(x0, deck + 1.5f, zz), Color.Lerp(Steel, Color.white, .25f), zz < z ? Vector3.back : Vector3.forward, false);
            }
            foreach (float x in new[] { BedX0 - .3f, BedX1 + 1.4f })
            {
                foreach (float zz in new[] { z - w * .5f + .15f, z + w * .5f - .15f }) m.Box(V(x, deck * .5f - .3f, zz), V(.3f, deck - .2f, .3f), Steel);
                m.Box(V(x, deck - .6f, z), V(.3f, .3f, w), Steel);
                m.Surface = ComicSurface.Concrete;
                m.Box(V(x, .25f, z), V(.9f, .5f, w + .5f), Concrete);
                m.Surface = ComicSurface.Metal;
            }
            // the ramp: from the deck down to the path, along the fence
            float rx = K.Edge + .95f, rz1 = z - w * .5f, rz0 = rz1 - rampLength;
            int segs = Mathf.Max(2, Mathf.RoundToInt(rampLength / 6f));
            for (int i = 0; i < segs; i++)
            {
                float za = Mathf.Lerp(rz0, rz1, (float)i / segs), zb = Mathf.Lerp(rz0, rz1, (float)(i + 1) / segs);
                float ya = Mathf.Lerp(K.Kerb, deck, (float)i / segs), yb = Mathf.Lerp(K.Kerb, deck, (float)(i + 1) / segs);
                m.Surface = ComicSurface.Concrete;
                K.Face(m, V(rx - .8f, ya, za), V(rx - .8f, yb, zb), V(rx + .8f, yb, zb), V(rx + .8f, ya, za), Concrete, Vector3.up);
                K.Face(m, V(rx - .8f, ya - .3f, za), V(rx - .8f, yb - .3f, zb), V(rx + .8f, yb - .3f, zb), V(rx + .8f, ya - .3f, za), ConcreteDark, Vector3.down);
                K.Face(m, V(rx - .8f, ya - .3f, za), V(rx - .8f, yb - .3f, zb), V(rx - .8f, yb, zb), V(rx - .8f, ya, za), SteelDark, Vector3.left);
                m.Surface = ComicSurface.PaintedMetal;
                m.Beam(V(rx - .8f, ya + 1.05f, za), V(rx - .8f, yb + 1.05f, zb), .08f, Yellow);
                m.Beam(V(rx + .8f, ya + 1.05f, za), V(rx + .8f, yb + 1.05f, zb), .08f, Yellow);
                m.Surface = ComicSurface.Metal;
                m.Box(V(rx - .8f, yb + .5f, zb), V(.07f, 1.1f, .07f), Steel);
                m.Box(V(rx + .8f, yb + .5f, zb), V(.07f, 1.1f, .07f), Steel);
                if (i > 0) m.Box(V(rx, ya * .5f - .2f, za), V(.24f, Mathf.Max(.3f, ya - .2f), .24f), Steel);
                if (!mural || i == 0) continue;
                // painted panels under the ramp: gum leaves and river blues (community mural)
                Color[] cols = { H("F0C23A"), H("2A9D9A"), H("E8744A"), H("7BB661"), H("3F7FB5") };
                m.Surface = ComicSurface.Plaster;
                K.Face(m, V(rx - .82f, K.Kerb, za), V(rx - .82f, K.Kerb, zb), V(rx - .82f, yb - .4f, zb), V(rx - .82f, ya - .4f, za), H("F4EBD2"), Vector3.left);
                for (int k = 0; k < 3; k++)
                {
                    float t = (k + .5f) / 3f, zc = Mathf.Lerp(za, zb, t), yc = Mathf.Lerp(.6f, Mathf.Max(.9f, ya - .9f), (k * 37 % 10) / 10f);
                    K.Face(m, V(rx - .83f, yc - .35f, zc - .5f), V(rx - .83f, yc + .1f, zc + .45f), V(rx - .83f, yc + .45f, zc + .2f), V(rx - .83f, yc + .05f, zc - .6f), cols[(i * 3 + k) % cols.Length], Vector3.left, false);
                }
            }
        }

        // ================================================================ far scenery helpers

        /// <summary>A flat strip split every 20 m along z (the GPU curl bends per vertex).</summary>
        public static void FarStrip(ComicMesh m, float x0, float x1, float z0, float z1, float y, Color c)
        {
            m.Surface = ComicSurface.Plain;
            for (float z = z0; z < z1 - .01f; z += 20f)
            {
                float e = Mathf.Min(z1, z + 20f);
                K.Face(m, V(x0, y, z), V(x0, y, e), V(x1, y, e), V(x1, y, z), c, Vector3.up, false);
            }
        }

        /// <summary>Aerial perspective: further = closer to the haze colour.</summary>
        public static Color Haze(Color c, float dist, Color haze) => Color.Lerp(c, haze, Mathf.Clamp01((Mathf.Abs(dist) - 60f) / 600f) * .7f);

        /// <summary>A long ridge parallel to the road on side s (the Darling Scarp): a foot slope, the main face up to a
        /// wavy crest, the top falling away behind; split every 15 m so the curl bends it.</summary>
        public static void Ridge(ComicMesh m, int s, float xNear, float depth, float z0, float z1, float baseY, Func<float, float> height, Color foot, Color slope, Color top, Color haze, float hazeAmount)
        {
            Color ft = Color.Lerp(foot, haze, hazeAmount), sl = Color.Lerp(slope, haze, hazeAmount), tp = Color.Lerp(top, haze, hazeAmount);
            Vector3 toRoad = new Vector3(-s, 0, 0);
            for (float z = z0; z < z1 - .01f; z += 15f)
            {
                float za = z, zb = Mathf.Min(z1, z + 15f), ha = height(za), hb = height(zb);
                float xs = s * xNear, xm = s * (xNear + depth * .18f), xr = s * (xNear + depth * .5f), xf = s * (xNear + depth);
                m.Surface = ComicSurface.Foliage;
                K.Face(m, V(xs, baseY, za), V(xs, baseY, zb), V(xm, baseY + hb * .3f, zb), V(xm, baseY + ha * .3f, za), ft, (toRoad + Vector3.up * 2f).normalized, false);
                K.Face(m, V(xm, baseY + ha * .3f, za), V(xm, baseY + hb * .3f, zb), V(xr, baseY + hb, zb), V(xr, baseY + ha, za), sl, (toRoad + Vector3.up).normalized, false);
                K.Face(m, V(xr, baseY + ha, za), V(xr, baseY + hb, zb), V(xf, baseY + hb * .6f, zb), V(xf, baseY + ha * .6f, za), tp, Vector3.up, false);
            }
        }

        /// <summary>A far tree line: a row of two-tone blobs along z at about x.</summary>
        public static void TreeLine(ComicMesh m, System.Random r, float x, float jitter, float z0, float z1, float step, float h, Color a, Color b, Color haze, float y = 0f)
        {
            m.Surface = ComicSurface.Foliage;
            for (float z = z0; z < z1; z += step * F(r, .7f, 1.3f))
            {
                float xx = x + F(r, -jitter, jitter), hh = h * F(r, .7f, 1.25f), w = hh * F(r, 1.1f, 1.7f);
                Color c = Haze(Color.Lerp(a, b, F(r, 0f, 1f)), Mathf.Abs(xx), haze);
                Clump(m, V(xx, y + hh * .5f, z), V(w, hh * 1.1f, w * 1.1f), c, (int)(z * 3f + xx), 6, 3, .2f);
            }
        }
    }
}
