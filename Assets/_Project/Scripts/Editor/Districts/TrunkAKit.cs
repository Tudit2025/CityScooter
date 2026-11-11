using System.Collections.Generic;
using CityScooter.EditorTools.Art;
using CityScooter.Rendering;
using UnityEngine;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// v0.6 polish kit of the first three districts (UWA, Matilda Bay, Kings Park; CrawleyBuilder and KingsParkBuilder):
    /// a pixel font whose wide letters (M, N, W, V, Y) are 5 pixels wide, real round-arched arcades (openings with depth
    /// on columns or piers), rock-faced stone patches and a few shared landmark pieces. Geometry only: the callers
    /// bring their colours. Nothing here changes ParkRiverKit.
    /// </summary>
    internal static class TrunkAKit
    {
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// <summary>A quad facing `normal` whatever order the corners come in.</summary>
        public static void Face(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector3 normal, bool ink = false)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) m.Quad(d, c, b, a, color, ink);
            else m.Quad(a, b, c, d, color, ink);
        }

        public static void Tri(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, Color color, Vector3 normal)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) m.Tri(a, c, b, color);
            else m.Tri(a, b, c, color);
        }

        // ------------------------------------------------------------------ pixel font (5 rows; wide letters 5 columns)

        static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".##.", "#..#", "####", "#..#", "#..#" }, ['B'] = new[] { "###.", "#..#", "###.", "#..#", "###." },
            ['C'] = new[] { ".###", "#...", "#...", "#...", ".###" }, ['D'] = new[] { "###.", "#..#", "#..#", "#..#", "###." },
            ['E'] = new[] { "####", "#...", "###.", "#...", "####" }, ['F'] = new[] { "####", "#...", "###.", "#...", "#..." },
            ['G'] = new[] { ".###", "#...", "#.##", "#..#", ".###" }, ['H'] = new[] { "#..#", "#..#", "####", "#..#", "#..#" },
            ['I'] = new[] { "###", ".#.", ".#.", ".#.", "###" }, ['J'] = new[] { "..##", "...#", "...#", "#..#", ".##." },
            ['K'] = new[] { "#..#", "#.#.", "##..", "#.#.", "#..#" }, ['L'] = new[] { "#...", "#...", "#...", "#...", "####" },
            ['M'] = new[] { "#...#", "##.##", "#.#.#", "#...#", "#...#" }, ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#" },
            ['O'] = new[] { ".##.", "#..#", "#..#", "#..#", ".##." }, ['P'] = new[] { "###.", "#..#", "###.", "#...", "#..." },
            ['R'] = new[] { "###.", "#..#", "###.", "#.#.", "#..#" }, ['S'] = new[] { ".###", "#...", ".##.", "...#", "###." },
            ['T'] = new[] { "###", ".#.", ".#.", ".#.", ".#." }, ['U'] = new[] { "#..#", "#..#", "#..#", "#..#", ".##." },
            ['V'] = new[] { "#...#", "#...#", ".#.#.", ".#.#.", "..#.." }, ['W'] = new[] { "#...#", "#...#", "#.#.#", "##.##", "#...#" },
            ['X'] = new[] { "#..#", "#..#", ".##.", "#..#", "#..#" }, ['Y'] = new[] { "#...#", ".#.#.", "..#..", "..#..", "..#.." },
            ['Z'] = new[] { "####", "...#", ".##.", "#...", "####" },
        };

        public static float TextWidth(string text, float px)
        {
            int n = 0;
            foreach (char ch in text) n += (Font.TryGetValue(ch, out string[] g) ? g[0].Length : 2) + 1;
            return (n - 1) * px;
        }

        /// <summary>Pixel text centred on c in the plane right / up, facing n (runs of pixels are one quad each).</summary>
        public static void Text(ComicMesh m, string text, Vector3 c, Vector3 right, Vector3 up, Vector3 n, float px, Color color)
        {
            m.Surface = ComicSurface.Plain;
            float x = -TextWidth(text, px) * .5f;
            foreach (char ch in text)
            {
                if (!Font.TryGetValue(ch, out string[] g)) { x += 3 * px; continue; }
                for (int row = 0; row < 5; row++)
                {
                    float y = (2.5f - row) * px;
                    for (int col = 0; col < g[row].Length; col++)
                    {
                        if (g[row][col] != '#') continue;
                        int end = col;
                        while (end + 1 < g[row].Length && g[row][end + 1] == '#') end++;
                        Vector3 a = c + right * (x + col * px) + up * (y - px), b = c + right * (x + (end + 1) * px) + up * (y - px);
                        Face(m, a, b, b + up * px, a + up * px, color, n);
                        col = end;
                    }
                }
                x += (g[0].Length + 1) * px;
            }
        }

        // ------------------------------------------------------------------ arcades

        /// <summary>
        /// A wall of `count` round-arched openings with real depth. p = bottom left corner of the wall's front plane,
        /// `right` along the wall, n = the way it faces; the wall is `width` long and `height` high, each opening `open`
        /// wide from `sill` up to the springing at `spring`, then a half circle. The reveal (soffit and jambs) goes
        /// `depth` back in the `soffit` colour. columns: the wall stands on round columns (radius `columnRadius`) with a
        /// square capital instead of piers. ring: a raised archivolt band in the `trim` colour.
        /// </summary>
        public static void Arcade(ComicMesh m, Vector3 p, Vector3 right, Vector3 n, float width, int count, float open, float sill, float spring,
            float height, float depth, Color wall, Color soffit, Color trim, bool columns = false, float columnRadius = .26f, bool ring = true, int segments = 6)
        {
            Vector3 up = Vector3.up, back = -n * depth;
            float bay = width / count, r = open * .5f;
            ComicSurface surface = m.Surface;
            for (int i = 0; i < count; i++)
            {
                float x0 = i * bay, cx = x0 + bay * .5f, xl = cx - r, xr = cx + r, pierBottom = columns ? spring : 0f;
                Vector3 P(float x, float y) => p + right * x + up * y;
                // wall beside the opening and (with a sill) under it
                if (xl - x0 > .01f)
                {
                    Face(m, P(x0, pierBottom), P(xl, pierBottom), P(xl, height), P(x0, height), wall, n);
                    Face(m, P(xr, pierBottom), P(x0 + bay, pierBottom), P(x0 + bay, height), P(xr, height), wall, n);
                }
                if (sill > .01f)
                {
                    Face(m, P(xl, 0), P(xr, 0), P(xr, sill), P(xl, sill), wall, n);
                    Face(m, P(xl, sill), P(xr, sill), P(xr, sill) + back, P(xl, sill) + back, trim, up);
                }
                if (columns)
                {
                    // underside of the wall between the arch feet, over the capital
                    Face(m, P(x0, spring), P(xl, spring), P(xl, spring) + back, P(x0, spring) + back, soffit, Vector3.down);
                    Face(m, P(xr, spring), P(x0 + bay, spring), P(x0 + bay, spring) + back, P(xr, spring) + back, soffit, Vector3.down);
                }
                else
                {
                    Face(m, P(xl, sill), P(xl, spring), P(xl, spring) + back, P(xl, sill) + back, soffit, right);
                    Face(m, P(xr, sill), P(xr, spring), P(xr, spring) + back, P(xr, sill) + back, soffit, -right);
                }
                for (int k = 0; k < segments; k++)
                {
                    float a0 = Mathf.PI * (1f - (float)k / segments), a1 = Mathf.PI * (1f - (float)(k + 1) / segments);
                    float ax = cx + Mathf.Cos(a0) * r, ay = spring + Mathf.Sin(a0) * r, bx = cx + Mathf.Cos(a1) * r, by = spring + Mathf.Sin(a1) * r;
                    Face(m, P(ax, ay), P(bx, by), P(bx, height), P(ax, height), wall, n);
                    Vector3 inward = (P(cx, spring) - P((ax + bx) * .5f, (ay + by) * .5f)).normalized;
                    Face(m, P(ax, ay), P(bx, by), P(bx, by) + back, P(ax, ay) + back, soffit, inward);
                    if (!ring) continue;
                    float ro = r + Mathf.Min(.22f, (bay - open) * .45f);
                    Vector3 lift = n * .035f;
                    Face(m, P(ax, ay) + lift, P(bx, by) + lift, P(cx + Mathf.Cos(a1) * ro, spring + Mathf.Sin(a1) * ro) + lift,
                        P(cx + Mathf.Cos(a0) * ro, spring + Mathf.Sin(a0) * ro) + lift, trim, n);
                }
            }
            if (!columns) return;
            for (int i = 0; i <= count; i++)
            {
                Vector3 c = p + right * (i * bay) - n * (depth * .5f);
                bool end = i == 0 || i == count;
                if (end)
                {
                    // square end piers carry the wall at both ends
                    m.Surface = surface;
                    Vector3 q = c + right * (i == 0 ? columnRadius : -columnRadius);
                    m.Box(q + up * (spring * .5f), Abs(right * (columnRadius * 2f) + up * spring + n * depth), wall);
                    continue;
                }
                m.Surface = surface;
                m.Cylinder(c + up * ((spring - .2f) * .5f), columnRadius, spring - .2f, wall, 8, .88f);
                m.Box(c + up * (spring - .1f), Abs(right * (columnRadius * 2.5f) + up * .2f + n * Mathf.Max(depth, columnRadius * 2.5f)), trim);
                m.Box(c + up * .12f, Abs(right * (columnRadius * 2.4f) + up * .24f + n * (columnRadius * 2.4f)), trim);
            }
            m.Surface = surface;
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        /// <summary>Rock-faced stone: a few blocks of the wall in slightly different tones (flat patches just off the wall).</summary>
        public static void StoneBlocks(ComicMesh m, Vector3 p, Vector3 right, Vector3 n, float width, float height, System.Random rng, int count, Color a, Color b,
            float course = .45f)
        {
            Vector3 up = Vector3.up;
            for (int i = 0; i < count; i++)
            {
                float w = course * (2 + rng.Next(3)), h = course * (1 + rng.Next(2));
                if (w > width - .2f || h > height - .1f) continue;
                float x = .1f + (float)rng.NextDouble() * (width - w - .2f), y = Mathf.Floor((float)rng.NextDouble() * (height - h) / course) * course + .05f;
                Vector3 o = p + right * x + up * y + n * .02f;
                Face(m, o, o + right * w, o + right * w + up * h, o + up * h, i % 2 == 0 ? a : b, n);
            }
        }

        /// <summary>A flat disc (clock faces, medallions), facing n.</summary>
        public static void Disc(ComicMesh m, Vector3 c, Vector3 n, float r, Color color, int sides = 12)
        {
            Vector3 a = Vector3.Cross(n, Mathf.Abs(n.y) > .9f ? Vector3.forward : Vector3.up).normalized, b = Vector3.Cross(n, a);
            for (int i = 0; i < sides; i++)
            {
                float t0 = Mathf.PI * 2f * i / sides, t1 = Mathf.PI * 2f * (i + 1) / sides;
                Tri(m, c, c + (a * Mathf.Cos(t0) + b * Mathf.Sin(t0)) * r, c + (a * Mathf.Cos(t1) + b * Mathf.Sin(t1)) * r, color, n);
            }
        }

        /// <summary>A round rod from a to b (8 sides).</summary>
        public static void Rod(ComicMesh m, Vector3 a, Vector3 b, float radius, Color color, float topScale = 1f, int sides = 8)
        {
            Vector3 d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            m.Cylinder((a + b) * .5f, radius, d.magnitude, color, sides, topScale, Quaternion.FromToRotation(Vector3.up, d.normalized));
        }

        /// <summary>
        /// Moreton Bay fig: a short, very thick trunk on buttress roots (fins running out along the ground), heavy limbs
        /// and a wide, dark, low dome of leaves with a lighter top. base at p, the crown about `spread` metres wide.
        /// </summary>
        public static void Fig(ComicMesh m, Vector3 p, float spread, int seed, Color bark, Color leaf, Color leafLight, Color ink)
        {
            var rng = new System.Random(seed * 9973 + 5);
            float R() => (float)rng.NextDouble();
            float s = spread / 16f, trunkH = 3.2f * s, tr = 1.05f * s;
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .06f, 0), tr * 1.5f, .12f, ink, 8);
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, trunkH * .5f, 0), tr, trunkH, bark, 8, .72f);
            float spin = R() * 6f;
            for (int i = 0; i < 7; i++)
            {
                // buttress: a tapering fin from the trunk out along the ground
                float a = spin + i * Mathf.PI * 2f / 7f + (R() - .5f) * .3f, len = (2.3f + R() * 1.4f) * s;
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a)), side = V(-dir.z, 0, dir.x) * (.16f * s);
                Vector3 foot = p + dir * (tr * .6f), tip = p + dir * (tr + len), top = p + dir * (tr * .55f) + V(0, (1.5f + R() * .7f) * s, 0);
                Color c = Color.Lerp(bark, ink, .12f * (i % 2));
                Tri(m, foot + side, tip, top + side, c, side);
                Tri(m, foot - side, tip, top - side, c, -side);
                Tri(m, top + side, top - side, tip, Color.Lerp(bark, Color.white, .12f), Vector3.up + dir);
            }
            Vector3 fork = p + V(0, trunkH, 0);
            Color dark = Color.Lerp(leaf, ink, .22f);
            for (int i = 0; i < 4; i++)
            {
                float a = spin + .5f + i * Mathf.PI * .5f + (R() - .5f) * .4f;
                Vector3 end = fork + V(Mathf.Cos(a) * 4.2f * s, (2.2f + R()) * s, Mathf.Sin(a) * 4.2f * s);
                m.Surface = ComicSurface.Wood;
                Rod(m, fork - V(0, .4f * s, 0), end, .42f * s, bark, .6f);
                m.Surface = ComicSurface.Foliage;
                m.Ellipsoid(end + V(Mathf.Cos(a) * 1.6f * s, 1.2f * s, Mathf.Sin(a) * 1.6f * s), V(8.5f, 4.6f, 8.5f) * s, i % 2 == 0 ? leaf : dark);
            }
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(fork + V(0, 4.6f * s, 0), V(11f, 5.6f, 11f) * s, leaf);
            m.Ellipsoid(fork + V(1.2f * s, 6.2f * s, -.6f * s), V(6.5f, 3.4f, 6.5f) * s, leafLight);
        }

        /// <summary>
        /// The State War Memorial's cenotaph (Kings Park): a grey rock-faced granite obelisk with a pyramid tip and a
        /// plain cross in relief, on a stepped podium with white rails and two flagpoles. Background only: the caller
        /// puts it out of reach and gives it no collider. Base at p, the obelisk `h` high (18 m in life); `front` is the
        /// side the cross faces.
        /// </summary>
        public static void Cenotaph(ComicMesh m, Vector3 p, float h, Vector3 front, Color granite, Color granitedark, Color pale, Color flag, Color ink)
        {
            Vector3 right = Vector3.Cross(Vector3.up, front).normalized;
            m.Surface = ComicSurface.Concrete;
            // podium: three steps and the undercroft block
            m.Box(p + V(0, .25f, 0), V(13f, .5f, 13f), pale);
            m.Box(p + V(0, .75f, 0), V(11f, .5f, 11f), pale);
            m.Box(p + V(0, 1.9f, 0), V(9f, 1.8f, 9f), granite);
            m.Box(p + V(0, 2.9f, 0), V(9.4f, .2f, 9.4f), pale);
            // white rails round the podium top
            m.Surface = ComicSurface.PaintedMetal;
            foreach (Vector3 d in new[] { front, -front, right, -right })
            {
                Vector3 side = Vector3.Cross(Vector3.up, d);
                m.Box(p + d * 4.55f + V(0, 3.55f, 0), Abs(side * 9.1f + Vector3.up * .08f + d * .08f), Color.white);
                for (int i = -1; i <= 1; i++) m.Box(p + d * 4.55f + side * (i * 4.5f) + V(0, 3.3f, 0), V(.1f, .6f, .1f), Color.white);
            }
            // pedestal and obelisk
            m.Surface = ComicSurface.Concrete;
            float y = 3f;
            m.Box(p + V(0, y + .9f, 0), V(4.2f, 1.8f, 4.2f), granitedark);
            m.Box(p + V(0, y + 2.1f, 0), V(3.4f, .6f, 3.4f), granite);
            float shaft = h - 5.4f - 1.6f, b0 = 2.5f, b1 = 1.5f, y0 = y + 2.4f, y1 = y0 + shaft;
            foreach (Vector3 d in new[] { front, -front, right, -right })
            {
                Vector3 side = Vector3.Cross(Vector3.up, d);
                Vector3 a = p + d * (b0 * .5f) - side * (b0 * .5f) + V(0, y0, 0), b = p + d * (b0 * .5f) + side * (b0 * .5f) + V(0, y0, 0);
                Vector3 c = p + d * (b1 * .5f) + side * (b1 * .5f) + V(0, y1, 0), e = p + d * (b1 * .5f) - side * (b1 * .5f) + V(0, y1, 0);
                Face(m, a, b, c, e, d == front || d == -front ? granite : granitedark, d, true);
                Tri(m, e, c, p + V(0, y1 + 1.6f, 0), pale, d + Vector3.up);
            }
            // the cross in relief on the front
            Vector3 f = p + front * ((b0 + b1) * .25f + .08f) + V(0, y0 + shaft * .55f, 0);
            m.Box(f, Abs(right * .28f + Vector3.up * 2.6f + front * .12f), pale);
            m.Box(f + V(0, .5f, 0), Abs(right * 1.1f + Vector3.up * .28f + front * .12f), pale);
            // bronze plaque and wreaths
            m.Surface = ComicSurface.Metal;
            m.Box(p + front * 2.12f + V(0, y + .95f, 0), Abs(right * 1.6f + Vector3.up * .9f + front * .06f), new Color(.45f, .33f, .2f));
            // two white flagpoles at the front corners of the podium
            m.Surface = ComicSurface.PaintedMetal;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 foot = p + front * 6f + right * (s * 6f);
                m.Cylinder(foot + V(0, .2f, 0), .22f, .4f, ink, 8);
                m.Cylinder(foot + V(0, 5.2f, 0), .07f, 10f, Color.white, 8, .7f);
                m.Surface = ComicSurface.Fabric;
                m.Box(foot + right * .75f + V(0, 9.6f, 0), Abs(right * 1.4f + Vector3.up * .8f + front * .06f), flag);
                m.Surface = ComicSurface.PaintedMetal;
            }
        }
    }
}
