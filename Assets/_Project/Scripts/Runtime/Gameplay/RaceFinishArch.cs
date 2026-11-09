using System.Collections.Generic;
using CityScooter.Rendering;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Radio race (v0.4): the FINISH arch over the road at the race distance. Built at runtime from comic boxes (vertex
    /// colours, per-face ink creases) drawn with the rider's own comic palette material, so it bends with the world
    /// and needs no new material or shader. Candy-striped posts on the kerbs, a crossbar, and a red banner with
    /// chequered edges and FINISH in blocky letters. No colliders: nothing to crash into.
    /// </summary>
    public static class RaceFinishArch
    {
        const float PostHeight = 7f, BarY = 6.75f, BannerY = 5.2f;

        static readonly Color Red = new Color(0.86f, 0.13f, 0.11f);
        static readonly Color White = new Color(0.97f, 0.96f, 0.93f);
        static readonly Color Black = new Color(0.07f, 0.07f, 0.09f);
        static readonly Color Navy = new Color(0.14f, 0.2f, 0.36f);
        static readonly Color Yellow = new Color(1f, 0.82f, 0.18f);

        /// <summary>The comic palette material on the rider's model (not the ink outline, not the scarf); a plain
        /// placeholder material when the rider is a grey box.</summary>
        public static Material FindPalette(Transform model)
        {
            if (model != null)
                foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
                {
                    Material m = r.sharedMaterial;
                    if (m != null && !m.HasProperty("_OutlinePixels") && r.GetComponent<ScarfRibbon>() == null) return m;
                }
            return PlaceholderTrack.MaterialFor(Color.white);
        }

        public static GameObject Build(Transform parent, Material material)
        {
            var root = new GameObject("FinishArch");
            root.transform.SetParent(parent, false);
            var post = new Boxes();
            for (int i = 0; i < 10; i++)
                post.Box(new Vector3(0f, (i + 0.5f) * PostHeight / 10f, 0f), new Vector3(0.38f, PostHeight / 10f, 0.38f), (i & 1) == 0 ? Red : White);
            post.Box(new Vector3(0f, PostHeight + 0.12f, 0f), new Vector3(0.5f, 0.24f, 0.5f), Yellow);
            Mesh postMesh = post.Build("FinishPost");
            Part(root.transform, "PostL", postMesh, material);
            Part(root.transform, "PostR", postMesh, material);

            var bar = new Boxes();
            bar.Box(Vector3.zero, new Vector3(1f, 0.3f, 0.3f), Navy);
            Part(root.transform, "Bar", bar.Build("FinishBar"), material).localPosition = new Vector3(0f, BarY, 0f);

            Part(root.transform, "Banner", Banner(), material).localPosition = new Vector3(0f, BannerY, 0f);
            return root;
        }

        /// <summary>Stand the arch on the road at world z: posts on the kerbs (the road may be 2 or 4 lanes wide there).</summary>
        public static void Fit(GameObject arch, TrackGenerator track, float z)
        {
            if (arch == null) return;
            float half = (track != null ? Mathf.Max(3f, track.HalfWidthAt(z)) : 4f) + 0.55f;
            float y = track != null ? track.SurfaceHeightAt(0f, z) : 0f;
            Transform t = arch.transform;
            t.position = new Vector3(0f, y, z);
            Transform l = t.Find("PostL"), r = t.Find("PostR"), bar = t.Find("Bar");
            if (l != null) l.localPosition = new Vector3(-half, (track != null ? track.SurfaceHeightAt(-half, z) : y) - y, 0f);
            if (r != null) r.localPosition = new Vector3(half, (track != null ? track.SurfaceHeightAt(half, z) : y) - y, 0f);
            if (bar != null) bar.localScale = new Vector3(2f * half + 0.38f, 1f, 1f);
        }

        static Transform Part(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go.transform;
        }

        // 5 x 7 block letters (top row first).
        static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
        {
            ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
            ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#.#.#", "#..##", "#...#", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        };

        // The banner faces the riders coming up the road (-z). Centred on its own origin.
        static Mesh Banner()
        {
            const float w = 8.6f, h = 2.3f, d = 0.22f, px = 0.2f, front = -d * 0.5f - 0.03f;
            var b = new Boxes();
            b.Box(Vector3.zero, new Vector3(w, h, d), Red);
            // hangers up to the crossbar
            foreach (float x in new[] { -3.2f, 3.2f }) b.Box(new Vector3(x, h * 0.5f + 0.4f, 0f), new Vector3(0.12f, 0.8f, 0.12f), Navy);
            // chequered edges, top and bottom, front and back
            const int n = 20;
            float sq = w / n;
            for (int i = 0; i < n; i++)
                foreach (float y in new[] { h * 0.5f - 0.14f, -h * 0.5f + 0.14f })
                {
                    bool dark = ((i + (y > 0f ? 0 : 1)) & 1) == 0;
                    foreach (float z in new[] { front, -front })
                        b.Box(new Vector3(-w * 0.5f + (i + 0.5f) * sq, y, z), new Vector3(sq, 0.28f, 0.06f), dark ? Black : White, false);
                }
            // FINISH in white blocks (one box per run of pixels in a row), front and back
            const string word = "FINISH";
            float letterW = 5 * px, gap = px;
            float total = word.Length * letterW + (word.Length - 1) * gap;
            for (int li = 0; li < word.Length; li++)
            {
                string[] rows = Font[word[li]];
                float x0 = -total * 0.5f + li * (letterW + gap);
                for (int row = 0; row < rows.Length; row++)
                {
                    float y = (3 - row) * px;
                    string bits = rows[row];
                    int c = 0;
                    while (c < bits.Length)
                    {
                        if (bits[c] != '#') { c++; continue; }
                        int start = c;
                        while (c < bits.Length && bits[c] == '#') c++;
                        float cx = x0 + (start + c) * 0.5f * px;
                        foreach (float z in new[] { front, -front })
                        {
                            // the back reads the right way round from behind
                            float x = z < 0f ? cx : -cx;
                            b.Box(new Vector3(x, y, z), new Vector3((c - start) * px, px, 0.06f), White, false);
                        }
                    }
                }
            }
            return b.Build("FinishBanner");
        }

        /// <summary>Flat-shaded boxes in the comic vertex format: colour (linear), uv0 = per-face 0..1 with z = 1 for the
        /// crease ink at the face edges (0 = no ink), uv2 = no surface detail.</summary>
        sealed class Boxes
        {
            readonly List<Vector3> _v = new List<Vector3>(), _n = new List<Vector3>(), _uv0 = new List<Vector3>();
            readonly List<Vector4> _uv2 = new List<Vector4>();
            readonly List<Color> _c = new List<Color>();
            readonly List<int> _t = new List<int>();

            // Each face: its normal and two edge directions with cross(t1, t2) = normal (Unity's clockwise front faces).
            static readonly Vector3[,] Faces =
            {
                { Vector3.right, Vector3.up, Vector3.forward }, { Vector3.left, Vector3.forward, Vector3.up },
                { Vector3.up, Vector3.forward, Vector3.right }, { Vector3.down, Vector3.right, Vector3.forward },
                { Vector3.forward, Vector3.right, Vector3.up }, { Vector3.back, Vector3.up, Vector3.right },
            };

            public void Box(Vector3 centre, Vector3 size, Color colour, bool ink = true)
            {
                Vector3 half = size * 0.5f;
                Color linear = colour.linear;
                for (int f = 0; f < 6; f++)
                {
                    Vector3 n = Faces[f, 0], a = Faces[f, 1], b = Faces[f, 2];
                    Vector3 c = centre + Vector3.Scale(n, half), ha = Vector3.Scale(a, half), hb = Vector3.Scale(b, half);
                    int i = _v.Count;
                    _v.Add(c - ha - hb); _v.Add(c - ha + hb); _v.Add(c + ha + hb); _v.Add(c + ha - hb);
                    float k = ink ? 1f : 0f;
                    _uv0.Add(new Vector3(0f, 0f, k)); _uv0.Add(new Vector3(0f, 1f, k)); _uv0.Add(new Vector3(1f, 1f, k)); _uv0.Add(new Vector3(1f, 0f, k));
                    for (int j = 0; j < 4; j++) { _n.Add(n); _c.Add(linear); _uv2.Add(Vector4.zero); }
                    _t.Add(i); _t.Add(i + 2); _t.Add(i + 1);
                    _t.Add(i); _t.Add(i + 3); _t.Add(i + 2);
                }
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
                if (_v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(_v);
                mesh.SetNormals(_n);
                mesh.SetColors(_c);
                mesh.SetUVs(0, _uv0);
                mesh.SetUVs(2, _uv2);
                mesh.SetTriangles(_t, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
