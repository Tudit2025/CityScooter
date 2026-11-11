using System.Collections.Generic;
using CityScooter.EditorTools.Art;
using CityScooter.Rendering;
using UnityEngine;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// Pixel lettering for place-name signs in the CBD, Elizabeth Quay and Mounts Bay Rd (v0.6): a 5-row font whose
    /// M, N and W are 5 pixels wide (the 3-wide ones cannot be told apart from H and U at riding distance), drawn as
    /// flat quads on any plane (signs facing the rider, facing the road, on a wall).
    /// </summary>
    internal static class TrunkPixelText
    {
        // Rows top to bottom, '#' = pixel. Width = length of the first row.
        static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".#.", "#.#", "###", "#.#", "#.#" }, ['B'] = new[] { "##.", "#.#", "##.", "#.#", "##." },
            ['C'] = new[] { ".##", "#..", "#..", "#..", ".##" }, ['D'] = new[] { "##.", "#.#", "#.#", "#.#", "##." },
            ['E'] = new[] { "###", "#..", "##.", "#..", "###" }, ['F'] = new[] { "###", "#..", "##.", "#..", "#.." },
            ['G'] = new[] { ".##", "#..", "#.#", "#.#", ".##" }, ['H'] = new[] { "#.#", "#.#", "###", "#.#", "#.#" },
            ['I'] = new[] { "###", ".#.", ".#.", ".#.", "###" }, ['J'] = new[] { "..#", "..#", "..#", "#.#", ".#." },
            ['K'] = new[] { "#.#", "#.#", "##.", "#.#", "#.#" }, ['L'] = new[] { "#..", "#..", "#..", "#..", "###" },
            ['M'] = new[] { "#...#", "##.##", "#.#.#", "#...#", "#...#" }, ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#" },
            ['O'] = new[] { ".#.", "#.#", "#.#", "#.#", ".#." }, ['P'] = new[] { "##.", "#.#", "##.", "#..", "#.." },
            ['Q'] = new[] { ".#.", "#.#", "#.#", "###", ".##" }, ['R'] = new[] { "##.", "#.#", "##.", "#.#", "#.#" },
            ['S'] = new[] { ".##", "#..", ".#.", "..#", "##." }, ['T'] = new[] { "###", ".#.", ".#.", ".#.", ".#." },
            ['U'] = new[] { "#.#", "#.#", "#.#", "#.#", "###" }, ['V'] = new[] { "#.#", "#.#", "#.#", "#.#", ".#." },
            ['W'] = new[] { "#...#", "#...#", "#.#.#", "##.##", "#...#" }, ['X'] = new[] { "#.#", "#.#", ".#.", "#.#", "#.#" },
            ['Y'] = new[] { "#.#", "#.#", ".#.", ".#.", ".#." }, ['Z'] = new[] { "###", "..#", ".#.", "#..", "###" },
            ['0'] = new[] { "###", "#.#", "#.#", "#.#", "###" }, ['1'] = new[] { ".#.", "##.", ".#.", ".#.", "###" },
            ['2'] = new[] { "##.", "..#", ".#.", "#..", "###" }, ['3'] = new[] { "##.", "..#", ".#.", "..#", "##." },
            ['4'] = new[] { "#.#", "#.#", "###", "..#", "..#" }, ['5'] = new[] { "###", "#..", "##.", "..#", "##." },
            ['6'] = new[] { ".##", "#..", "###", "#.#", "###" }, ['7'] = new[] { "###", "..#", ".#.", ".#.", ".#." },
            ['8'] = new[] { "###", "#.#", "###", "#.#", "###" }, ['9'] = new[] { "###", "#.#", "###", "..#", "##." },
        };

        static int GlyphWidth(char ch) => ch == ' ' ? 2 : Glyphs.TryGetValue(ch, out string[] g) ? g[0].Length : 3;

        /// <summary>Width of the text in metres at pixel size px (one pixel between letters).</summary>
        public static float Width(string text, float px)
        {
            int cells = 0;
            foreach (char ch in text) cells += GlyphWidth(ch) + 1;
            return Mathf.Max(0, cells - 1) * px;
        }

        /// <summary>
        /// Draws the text centred on <paramref name="centre"/>, reading along <paramref name="right"/> with
        /// <paramref name="up"/> as the up direction; the quads face cross(up, right) (towards a viewer who sees
        /// "right" on their right). No ink (pixels, not blocks).
        /// </summary>
        public static void Draw(ComicMesh m, string text, Vector3 centre, Vector3 right, Vector3 up, float px, Color colour)
        {
            m.Surface = ComicSurface.Plain;
            right = right.normalized; up = up.normalized;
            Vector3 normal = Vector3.Cross(up, right);
            float x = -Width(text, px) * .5f;
            foreach (char ch in text)
            {
                if (Glyphs.TryGetValue(ch, out string[] g))
                    for (int row = 0; row < 5; row++)
                        for (int col = 0; col < g[row].Length; col++)
                        {
                            if (g[row][col] != '#') continue;
                            Vector3 a = centre + right * (x + col * px) + up * ((1.5f - row) * px);
                            Vector3 b = a + right * px, cc = b + up * px, d = a + up * px;
                            if (Vector3.Dot(Vector3.Cross(b - a, cc - a), normal) < 0f) m.Quad(d, cc, b, a, colour, false);
                            else m.Quad(a, b, cc, d, colour, false);
                        }
                x += (GlyphWidth(ch) + 1) * px;
            }
        }

        /// <summary>A sign board (ink frame, coloured panel) with the text on the side facing <c>cross(up, right)</c>;
        /// returns the board width. The panel is a closed box (thickness along the facing direction).</summary>
        public static float Board(ComicMesh m, string text, Vector3 centre, Vector3 right, float px, Color panel, Color letters, Color frame, float thick = .12f)
        {
            right = right.normalized;
            Vector3 up = Vector3.up, normal = Vector3.Cross(up, right);
            float w = Width(text, px) + 4f * px, h = 9f * px;
            Quaternion q = Quaternion.LookRotation(normal, up);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(centre, new Vector3(w + .12f, h + .12f, thick), frame, q);
            m.Box(centre + normal * (thick * .5f + .005f), new Vector3(w, h, .02f), panel, q);
            Draw(m, text, centre + normal * (thick * .5f + .02f), right, up, px, letters);
            return w + .12f;
        }
    }
}
