using System.Collections.Generic;
using CityScooter.Track;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Item race (v0.5, docs/item-race.md): everything the items put in the world, built at runtime in the comic vertex
    /// format (vertex colours, per-face crease ink) and drawn with the rider's own comic palette and ink materials, like
    /// the FINISH arch: the question-mark item box, the oil slick, the boost flame, the rocket, the bolt over a slowed
    /// rider, the comic bursts (BOOM!, ZAP!, SLIP!, BLOCK!, ITEM!) and the shield bubble. Only the bubble needs a
    /// see-through material: the built-in Sprites/Default shader (always in the build), tinted; without it the bubble is
    /// just its comic highlights. No prefabs, no new assets, nothing in the scene. One kit per RaceItems; Dispose frees
    /// the meshes and the bubble material.
    /// </summary>
    public sealed class RaceItemArt
    {
        static readonly Color White = new Color(0.97f, 0.96f, 0.93f);
        static readonly Color Ink = new Color(0.07f, 0.07f, 0.09f);
        static readonly Color Red = new Color(0.9f, 0.16f, 0.12f);
        static readonly Color Navy = new Color(0.14f, 0.2f, 0.4f);
        static readonly Color Yellow = new Color(1f, 0.84f, 0.14f);
        static readonly Color Orange = new Color(1f, 0.5f, 0.08f);
        static readonly Color Blue = new Color(0.2f, 0.45f, 1f);
        static readonly Color Cyan = new Color(0.25f, 0.82f, 1f);

        /// <summary>Box colours, one per lane (left, middle, right).</summary>
        public static readonly Color[] BoxColours = { new Color(1f, 0.78f, 0.1f), new Color(0.2f, 0.8f, 1f), new Color(1f, 0.42f, 0.72f) };

        readonly Material _palette, _ink;
        Material _glass;
        readonly List<Object> _owned = new List<Object>();
        readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();

        public RaceItemArt(Transform riderModel)
        {
            _palette = RaceFinishArch.FindPalette(riderModel);
            _ink = FindInk(riderModel);
        }

        /// <summary>The comic ink outline material on the rider's model (null on a grey-box rider: then no outlines).</summary>
        static Material FindInk(Transform model)
        {
            if (model == null) return null;
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material m = r.sharedMaterial;
                if (m != null && m.HasProperty("_OutlinePixels")) return m;
            }
            return null;
        }

        public void Dispose()
        {
            foreach (Object o in _owned) if (o != null) Object.Destroy(o);
            _owned.Clear();
            _meshes.Clear();
            _glass = null;
        }

        Mesh Cached(string name, System.Func<Mesher> build)
        {
            if (_meshes.TryGetValue(name, out Mesh mesh) && mesh != null) return mesh;
            mesh = build().Build(name);
            _meshes[name] = mesh;
            _owned.Add(mesh);
            return mesh;
        }

        GameObject Part(Transform parent, string name, Mesh mesh, bool outline, bool shadows)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _palette;
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (outline && _ink != null)
            {
                var ink = new GameObject("InkOutline", typeof(MeshFilter), typeof(MeshRenderer));
                ink.transform.SetParent(go.transform, false);
                ink.GetComponent<MeshFilter>().sharedMesh = mesh;
                var imr = ink.GetComponent<MeshRenderer>();
                imr.sharedMaterial = _ink;
                imr.shadowCastingMode = ShadowCastingMode.Off;
                imr.receiveShadows = false;
            }
            return go;
        }

        // ---------------------------------------------------------------- the item box

        /// <summary>A question-mark box for lane 0..2 (its colour), turning slowly. Root at the box centre.</summary>
        public ItemBox Box(Transform parent, int lane)
        {
            int colour = Mathf.Clamp(lane, 0, BoxColours.Length - 1);
            var root = new GameObject("ItemBox");
            root.transform.SetParent(parent, false);
            GameObject art = Part(root.transform, "Art", Cached("ItemBox" + colour, () => BoxMesh(BoxColours[colour])), true, true);
            var box = root.AddComponent<ItemBox>();
            box.Init(art.transform, lane);
            return box;
        }

        static readonly string[] Question = { ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.." };

        static Mesher BoxMesh(Color colour)
        {
            const float size = 1.25f, half = size * 0.5f, px = 0.14f;
            var m = new Mesher();
            m.Box(Vector3.zero, new Vector3(size, size, size), colour);
            // white corner blocks, like a comic prize box
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
                m.Box(corner * (half - 0.04f), Vector3.one * 0.23f, White);
            }
            // "?" on the four sides, raised a little
            Vector3[] normals = { Vector3.back, Vector3.forward, Vector3.left, Vector3.right };
            foreach (Vector3 n in normals)
            {
                Vector3 right = Vector3.Cross(Vector3.up, -n); // the viewer's right when reading this face
                Quaternion turn = Quaternion.LookRotation(n, Vector3.up);
                for (int row = 0; row < Question.Length; row++)
                {
                    string bits = Question[row];
                    int c = 0;
                    while (c < bits.Length)
                    {
                        if (bits[c] != '#') { c++; continue; }
                        int start = c;
                        while (c < bits.Length && bits[c] == '#') c++;
                        float x = ((start + c) * 0.5f - 2.5f) * px, y = (3f - row) * px;
                        m.Box(n * (half + 0.03f) + right * x + Vector3.up * y, new Vector3((c - start) * px, px, 0.08f), White, false, turn);
                    }
                }
            }
            return m;
        }

        // ---------------------------------------------------------------- oil

        /// <summary>An oil slick lying on the road, with the drum it came from tipped over in it, so it is seen from far
        /// away (root on the road surface, y up). Decoration only: RaceItems works out who rides onto it.</summary>
        public GameObject Oil(Transform parent)
        {
            var root = new GameObject("OilSlick");
            root.transform.SetParent(parent, false);
            Part(root.transform, "Art", Cached("OilSlick", OilMesh), false, false);
            Part(root.transform, "Drum", Cached("OilDrum", DrumMesh), true, true);
            return root;
        }

        static Mesher DrumMesh()
        {
            var m = new Mesher();
            var body = new Color(0.1f, 0.42f, 0.5f);
            Vector3 a = new Vector3(-0.52f, 0.34f, 0.12f), b = new Vector3(0.36f, 0.34f, -0.2f), along = (b - a).normalized;
            m.Tube(a, b, 0.33f, 10, body);
            m.Tube(a + along * 0.12f, a + along * 0.24f, 0.36f, 10, Yellow);
            m.Tube(b - along * 0.24f, b - along * 0.12f, 0.36f, 10, Yellow);
            m.Tube(b, b + along * 0.03f, 0.2f, 8, new Color(0.035f, 0.035f, 0.05f)); // the open end, oil running out
            return m;
        }

        static Mesher OilMesh()
        {
            var m = new Mesher();
            float r = RaceItems.OilRadius;
            var black = new Color(0.035f, 0.035f, 0.05f);
            var sheen = new Color(0.16f, 0.14f, 0.3f);
            var shine = new Color(0.72f, 0.78f, 0.95f);
            var rim = new Color(0.62f, 0.42f, 0.95f);
            // An uneven puddle with a few drops around it; a bright oily rim so it shows on dark asphalt.
            m.Blob(new Vector3(0f, 0.018f, 0f), r * 1.1f, r * 1.02f, 16, 0.16f, 3f, 0.6f, rim);
            m.Blob(new Vector3(0f, 0.025f, 0f), r, r * 0.92f, 16, 0.16f, 3f, 0.6f, black);
            m.Blob(new Vector3(r * 0.95f, 0.025f, r * 0.55f), 0.22f, 0.2f, 8, 0f, 1f, 0f, black);
            m.Blob(new Vector3(-r * 1.02f, 0.025f, -r * 0.3f), 0.17f, 0.15f, 8, 0f, 1f, 0f, black);
            m.Blob(new Vector3(r * 0.25f, 0.025f, -r * 1.08f), 0.14f, 0.12f, 8, 0f, 1f, 0f, black);
            m.Blob(new Vector3(-0.12f, 0.035f, 0.1f), r * 0.66f, r * 0.56f, 14, 0.14f, 2f, 1.9f, sheen);
            m.Blob(new Vector3(-0.42f, 0.045f, 0.36f), 0.36f, 0.15f, 10, 0f, 1f, 0f, shine);
            m.Blob(new Vector3(0.12f, 0.045f, 0.52f), 0.13f, 0.09f, 8, 0f, 1f, 0f, shine);
            return m;
        }

        // ---------------------------------------------------------------- boost flame

        /// <summary>The boost item's flame: points along -z (behind the scooter); its Flicker child is scaled every frame.</summary>
        public GameObject Flame(Transform parent)
        {
            var root = new GameObject("BoostFlame");
            root.transform.SetParent(parent, false);
            Part(root.transform, "Flicker", Cached("BoostFlame", FlameMesh), false, false);
            return root;
        }

        static Mesher FlameMesh()
        {
            var m = new Mesher();
            m.Cone(new Vector3(0f, 0f, -1.35f), Vector3.zero, 0.3f, 7, Orange, Vector3.forward);
            m.Cone(new Vector3(0f, 0.02f, -1.95f), new Vector3(0f, 0.02f, -0.3f), 0.13f, 6, Yellow, Vector3.forward);
            m.Cone(new Vector3(-0.2f, 0.05f, -0.95f), new Vector3(-0.14f, 0f, -0.05f), 0.11f, 5, Yellow, Vector3.forward);
            m.Cone(new Vector3(0.2f, 0.05f, -0.95f), new Vector3(0.14f, 0f, -0.05f), 0.11f, 5, Yellow, Vector3.forward);
            return m;
        }

        // ---------------------------------------------------------------- rocket

        /// <summary>A comic rocket flying along +z (root at its middle).</summary>
        public GameObject Rocket(Transform parent)
        {
            var root = new GameObject("Rocket");
            root.transform.SetParent(parent, false);
            Part(root.transform, "Art", Cached("Rocket", RocketMesh), true, true);
            return root;
        }

        static Mesher RocketMesh()
        {
            var m = new Mesher();
            const float r = 0.19f;
            m.Tube(new Vector3(0f, 0f, -0.55f), new Vector3(0f, 0f, 0.35f), r, 8, Red);
            m.Tube(new Vector3(0f, 0f, 0.05f), new Vector3(0f, 0f, 0.17f), r + 0.015f, 8, White);
            m.Cone(new Vector3(0f, 0f, 0.85f), new Vector3(0f, 0f, 0.35f), r, 8, White, Vector3.back);
            for (int i = 0; i < 4; i++)
            {
                Quaternion turn = Quaternion.Euler(0f, 0f, 45f + i * 90f);
                m.Box(turn * new Vector3(0f, r + 0.12f, -0.45f), new Vector3(0.06f, 0.3f, 0.34f), Navy, true, turn);
            }
            m.Tube(new Vector3(0f, 0f, -0.66f), new Vector3(0f, 0f, -0.55f), r * 0.7f, 8, Ink);
            m.Cone(new Vector3(0f, 0f, -1.5f), new Vector3(0f, 0f, -0.66f), r * 0.62f, 6, Yellow, Vector3.forward);
            m.Cone(new Vector3(0f, 0f, -1.15f), new Vector3(0f, 0f, -0.64f), r * 0.85f, 6, Orange, Vector3.forward);
            return m;
        }

        /// <summary>The ring on the road round a rider a rocket is coming for (lies flat, radius about 1.4 m).</summary>
        public GameObject Target(Transform parent)
        {
            var root = new GameObject("RocketTarget");
            root.transform.SetParent(parent, false);
            Part(root.transform, "Art", Cached("RocketTarget", TargetMesh), false, false);
            return root;
        }

        static Mesher TargetMesh()
        {
            var m = new Mesher();
            m.Ring(new Vector3(0f, 0.04f, 0f), 1.15f, 1.5f, 24, Red, White);
            for (int i = 0; i < 4; i++)
            {
                Quaternion turn = Quaternion.Euler(0f, i * 90f, 0f);
                m.Box(turn * new Vector3(0f, 0.05f, 1.75f), new Vector3(0.2f, 0.02f, 0.6f), Red, false, turn);
            }
            return m;
        }

        // ---------------------------------------------------------------- bolt mark and bursts (face -z: turned to the camera)

        /// <summary>A lightning bolt (over a slowed rider's head); faces -z, RaceItems turns it to the camera.</summary>
        public GameObject BoltMark(Transform parent)
        {
            var root = new GameObject("BoltMark");
            root.transform.SetParent(parent, false);
            Part(root.transform, "Art", Cached("BoltMark", BoltMesh), false, false);
            return root;
        }

        static readonly Vector2[] BoltUpper = { new Vector2(-0.02f, 0.62f), new Vector2(0.3f, 0.62f), new Vector2(0.1f, 0.02f), new Vector2(-0.26f, 0.02f) };
        static readonly Vector2[] BoltLower = { new Vector2(-0.08f, 0.16f), new Vector2(0.26f, 0.16f), new Vector2(-0.12f, -0.62f) };

        static Mesher BoltMesh()
        {
            var m = new Mesher();
            m.Flat(BoltUpper, 0.02f, Ink, 1.22f);
            m.Flat(BoltLower, 0.02f, Ink, 1.26f);
            m.Flat(BoltUpper, 0f, Yellow, 1f);
            m.Flat(BoltLower, 0f, Yellow, 1f);
            return m;
        }

        public enum Burst { Boom, Zap, Slip, Block, Item }

        /// <summary>
        /// A comic burst with a word (BOOM!, ZAP!, SLIP!, BLOCK!, ITEM!), like the theme's pop-ups: an inactive template
        /// with ComicPopup, for RiderActions.Popup (which makes and pools the copies).
        /// </summary>
        public GameObject BurstTemplate(Transform parent, Burst kind)
        {
            string text; Color fill, ink; float radius;
            switch (kind)
            {
                case Burst.Boom: text = "BOOM!"; fill = Yellow; ink = Red; radius = 1.5f; break;
                case Burst.Zap: text = "ZAP!"; fill = Yellow; ink = Ink; radius = 1.05f; break;
                case Burst.Slip: text = "SLIP!"; fill = White; ink = Ink; radius = 1f; break;
                case Burst.Block: text = "BLOCK!"; fill = new Color(0.72f, 0.94f, 1f); ink = Navy; radius = 1.15f; break;
                default: text = "ITEM!"; fill = White; ink = Blue; radius = 0.7f; break;
            }
            var root = new GameObject("Fx" + kind);
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            Part(root.transform, "Art", Cached("Fx" + kind, () => BurstMesh(text, fill, ink, radius)), false, false);
            var popup = root.AddComponent<ComicPopup>();
            popup.life = kind == Burst.Boom ? 0.95f : 0.7f;
            popup.rise = 0.7f;
            return root;
        }

        // 3 x 5 glyphs (rows from the top), the same pixel font as the theme's pop-ups.
        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "010101111101101", ['B'] = "110101110101110", ['C'] = "011100100100011", ['E'] = "111100110100111",
            ['I'] = "111010010010111", ['K'] = "101101110101101", ['L'] = "100100100100111", ['M'] = "101111111101101",
            ['O'] = "010101101101010", ['P'] = "110101110100100", ['S'] = "011100010001110", ['T'] = "111010010010010",
            ['Z'] = "111001010100111", ['!'] = "010010010000010", ['?'] = "110001010000010",
        };

        static Mesher BurstMesh(string text, Color fill, Color ink, float radius)
        {
            var m = new Mesher();
            void Star(float r, float z, Color c)
            {
                const int points = 12;
                for (int i = 0; i < points * 2; i++)
                {
                    float a0 = i * Mathf.PI / points, a1 = (i + 1) * Mathf.PI / points;
                    float r0 = (i & 1) == 0 ? r : r * 0.68f, r1 = (i & 1) == 0 ? r * 0.68f : r;
                    var p0 = new Vector3(Mathf.Cos(a0) * r0 * 1.25f, Mathf.Sin(a0) * r0, z);
                    var p1 = new Vector3(Mathf.Cos(a1) * r1 * 1.25f, Mathf.Sin(a1) * r1, z);
                    m.Tri(new Vector3(0f, 0f, z), p1, p0, c);
                }
            }
            Star(radius * 1.12f, 0.02f, Ink);
            Star(radius, 0f, fill);
            float px = Mathf.Min(radius * 0.16f, radius * 1.7f / Mathf.Max(1, text.Length * 4 - 1));
            float width = (text.Length * 4 - 1) * px, x0 = -width * 0.5f, y0 = 2.5f * px;
            for (int i = 0; i < text.Length; i++)
            {
                if (!Glyphs.TryGetValue(text[i], out string g)) continue;
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < 3; col++)
                    {
                        if (g[row * 3 + col] != '1') continue;
                        float x = x0 + (i * 4 + col) * px, y = y0 - (row + 1) * px;
                        m.Quad(new Vector3(x, y, -0.02f), new Vector3(x, y + px, -0.02f), new Vector3(x + px, y + px, -0.02f), new Vector3(x + px, y, -0.02f), ink, false);
                    }
            }
            return m;
        }

        // ---------------------------------------------------------------- shield

        /// <summary>
        /// The shield: a see-through bubble (radius 1, scaled by RaceItems) with comic highlights. <paramref name="bubble"/>
        /// is the see-through part (null when the Sprites/Default shader is missing; then only the highlights show).
        /// </summary>
        public GameObject Shield(Transform parent, out Renderer bubble)
        {
            var root = new GameObject("Shield");
            root.transform.SetParent(parent, false);
            bubble = null;
            Material glass = Glass();
            if (glass != null)
            {
                var go = new GameObject("Bubble", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root.transform, false);
                go.GetComponent<MeshFilter>().sharedMesh = SphereMesh();
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = glass;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                bubble = mr;
            }
            Part(root.transform, "Glints", Cached("ShieldGlints", GlintMesh), false, false);
            return root;
        }

        Material Glass()
        {
            if (_glass != null) return _glass;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            _glass = new Material(shader) { name = "ItemShieldGlass", color = new Color(0.35f, 0.85f, 1f, 0.3f), renderQueue = 3000 };
            _owned.Add(_glass);
            return _glass;
        }

        Mesh SphereMesh()
        {
            if (_meshes.TryGetValue("ShieldBubble", out Mesh mesh) && mesh != null) return mesh;
            const int lon = 20, lat = 12;
            var v = new List<Vector3>();
            var c = new List<Color>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            for (int y = 0; y <= lat; y++)
                for (int x = 0; x <= lon; x++)
                {
                    float a = x * Mathf.PI * 2f / lon, b = y * Mathf.PI / lat;
                    v.Add(new Vector3(Mathf.Sin(b) * Mathf.Cos(a), Mathf.Cos(b), Mathf.Sin(b) * Mathf.Sin(a)));
                    c.Add(Color.white);
                    uv.Add(new Vector2(0.5f, 0.5f));
                }
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int i = y * (lon + 1) + x;
                    t.Add(i); t.Add(i + 1); t.Add(i + lon + 1);
                    t.Add(i + 1); t.Add(i + lon + 2); t.Add(i + lon + 1);
                }
            mesh = new Mesh { name = "ShieldBubble" };
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _meshes["ShieldBubble"] = mesh;
            _owned.Add(mesh);
            return mesh;
        }

        // Comic highlights on the bubble, on the side the chase camera sees (-z): a long curved streak up on the left, a
        // short one under it, and a thin ring round the rim.
        static Mesher GlintMesh()
        {
            var m = new Mesher();
            var light = new Color(0.9f, 0.98f, 1f);
            m.Arc(1.012f, 36f, 64f, 200f, 252f, 7, light);
            m.Arc(1.012f, 26f, 33f, 212f, 232f, 3, light);
            m.Arc(1.012f, -42f, -38f, 0f, 360f, 28, Cyan);
            return m;
        }

        // ---------------------------------------------------------------- mesh builder

        /// <summary>Flat-shaded comic geometry: colour (linear), uv0 = per-face 0..1 with z = 1 for crease ink at the face
        /// edges (0 = none), uv2 = no surface detail. A triangle's front is the side its corners run clockwise on.</summary>
        sealed class Mesher
        {
            readonly List<Vector3> _v = new List<Vector3>(), _n = new List<Vector3>(), _uv0 = new List<Vector3>();
            readonly List<Vector4> _uv2 = new List<Vector4>();
            readonly List<Color> _c = new List<Color>();
            readonly List<int> _t = new List<int>();

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Color colour)
            {
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                Color linear = colour.linear;
                int i = _v.Count;
                _v.Add(a); _v.Add(b); _v.Add(c);
                for (int j = 0; j < 3; j++) { _n.Add(n); _c.Add(linear); _uv0.Add(Vector3.zero); _uv2.Add(Vector4.zero); _t.Add(i + j); }
            }

            /// <summary>A triangle that faces <paramref name="facing"/> whatever order its corners come in.</summary>
            public void Tri(Vector3 a, Vector3 b, Vector3 c, Color colour, Vector3 facing)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0f) Tri(a, c, b, colour); else Tri(a, b, c, colour);
            }

            /// <summary>Corners in order round the face, clockwise seen from the front.</summary>
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color colour, bool ink)
            {
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                Color linear = colour.linear;
                float k = ink ? 1f : 0f;
                int i = _v.Count;
                _v.Add(a); _v.Add(b); _v.Add(c); _v.Add(d);
                _uv0.Add(new Vector3(0f, 0f, k)); _uv0.Add(new Vector3(0f, 1f, k)); _uv0.Add(new Vector3(1f, 1f, k)); _uv0.Add(new Vector3(1f, 0f, k));
                for (int j = 0; j < 4; j++) { _n.Add(n); _c.Add(linear); _uv2.Add(Vector4.zero); }
                _t.Add(i); _t.Add(i + 1); _t.Add(i + 2);
                _t.Add(i); _t.Add(i + 2); _t.Add(i + 3);
            }

            static readonly Vector3[,] Faces =
            {
                { Vector3.right, Vector3.up, Vector3.forward }, { Vector3.left, Vector3.forward, Vector3.up },
                { Vector3.up, Vector3.forward, Vector3.right }, { Vector3.down, Vector3.right, Vector3.forward },
                { Vector3.forward, Vector3.right, Vector3.up }, { Vector3.back, Vector3.up, Vector3.right },
            };

            public void Box(Vector3 centre, Vector3 size, Color colour, bool ink = true, Quaternion? rotation = null)
            {
                Quaternion q = rotation ?? Quaternion.identity;
                Vector3 half = size * 0.5f;
                for (int f = 0; f < 6; f++)
                {
                    Vector3 n = Faces[f, 0], a = Faces[f, 1], b = Faces[f, 2];
                    Vector3 c = Vector3.Scale(n, half), ha = Vector3.Scale(a, half), hb = Vector3.Scale(b, half);
                    // cross(a, b) = n: (c-ha-hb, c+ha-hb... ) clockwise from the front is -a-b, -a+b ... reversed
                    Quad(centre + q * (c - ha - hb), centre + q * (c + ha - hb), centre + q * (c + ha + hb), centre + q * (c - ha + hb), colour, ink);
                }
            }

            /// <summary>A flat convex outline in the xy plane at <paramref name="z"/>, facing -z, scaled about its middle.</summary>
            public void Flat(Vector2[] points, float z, Color colour, float scale)
            {
                Vector2 mid = Vector2.zero;
                foreach (Vector2 p in points) mid += p;
                mid /= points.Length;
                var centre = new Vector3(mid.x, mid.y, z);
                for (int i = 0; i < points.Length; i++)
                {
                    Vector2 p0 = mid + (points[i] - mid) * scale, p1 = mid + (points[(i + 1) % points.Length] - mid) * scale;
                    Tri(centre, new Vector3(p0.x, p0.y, z), new Vector3(p1.x, p1.y, z), colour, Vector3.back);
                }
            }

            /// <summary>An uneven round patch lying flat (facing up): radius rx by rz, wobbling by <paramref name="wobble"/>
            /// with <paramref name="lobes"/> lobes.</summary>
            public void Blob(Vector3 centre, float rx, float rz, int segments, float wobble, float lobes, float phase, Color colour)
            {
                Vector3 At(int i)
                {
                    float a = i * Mathf.PI * 2f / segments;
                    float k = 1f + wobble * Mathf.Sin(a * lobes + phase) + wobble * 0.5f * Mathf.Sin(a * (lobes + 2f) + phase * 2.3f);
                    return centre + new Vector3(Mathf.Cos(a) * rx * k, 0f, Mathf.Sin(a) * rz * k);
                }
                for (int i = 0; i < segments; i++) Tri(centre, At(i), At(i + 1), colour, Vector3.up);
            }

            /// <summary>A flat ring facing up, its segments in two colours in turn.</summary>
            public void Ring(Vector3 centre, float inner, float outer, int segments, Color a, Color b)
            {
                Vector3 At(int i, float r) { float t = i * Mathf.PI * 2f / segments; return centre + new Vector3(Mathf.Cos(t) * r, 0f, Mathf.Sin(t) * r); }
                for (int i = 0; i < segments; i++)
                {
                    Color c = (i & 1) == 0 ? a : b;
                    Tri(At(i, inner), At(i, outer), At(i + 1, outer), c, Vector3.up);
                    Tri(At(i, inner), At(i + 1, outer), At(i + 1, inner), c, Vector3.up);
                }
            }

            /// <summary>A cone from its base circle to <paramref name="apex"/>; <paramref name="capFacing"/> is the way the
            /// base cap looks.</summary>
            public void Cone(Vector3 apex, Vector3 baseCentre, float radius, int segments, Color colour, Vector3 capFacing)
            {
                Vector3 axis = (apex - baseCentre).normalized;
                Vector3 side = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized, up = Vector3.Cross(axis, side);
                Vector3 Rim(int i) { float a = i * Mathf.PI * 2f / segments; return baseCentre + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius; }
                for (int i = 0; i < segments; i++)
                {
                    Vector3 p0 = Rim(i), p1 = Rim(i + 1), mid = (p0 + p1) * 0.5f;
                    Tri(apex, p0, p1, colour, mid - baseCentre + axis * radius * 0.2f);
                    Tri(baseCentre, p0, p1, colour, capFacing);
                }
            }

            /// <summary>A round tube (prism) from <paramref name="from"/> to <paramref name="to"/>, capped at both ends.</summary>
            public void Tube(Vector3 from, Vector3 to, float radius, int segments, Color colour)
            {
                Vector3 axis = (to - from).normalized;
                Vector3 side = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized, up = Vector3.Cross(axis, side);
                Vector3 Dir(int i) { float a = i * Mathf.PI * 2f / segments; return (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius; }
                for (int i = 0; i < segments; i++)
                {
                    Vector3 d0 = Dir(i), d1 = Dir(i + 1), outward = d0 + d1;
                    Vector3 a = from + d0, b = to + d0, c = to + d1, d = from + d1;
                    if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) >= 0f) Quad(a, b, c, d, colour, true); else Quad(d, c, b, a, colour, true);
                    Tri(from, from + d0, from + d1, colour, -axis);
                    Tri(to, to + d0, to + d1, colour, axis);
                }
            }

            /// <summary>A band on a sphere of this radius between two latitudes and two longitudes (degrees; longitude 270 is
            /// -z, towards the chase camera), seen from both sides.</summary>
            public void Arc(float radius, float lat0, float lat1, float lon0, float lon1, int segments, Color colour)
            {
                Vector3 P(float lat, float lon)
                {
                    float la = lat * Mathf.Deg2Rad, lo = lon * Mathf.Deg2Rad;
                    return new Vector3(Mathf.Cos(la) * Mathf.Cos(lo), Mathf.Sin(la), Mathf.Cos(la) * Mathf.Sin(lo)) * radius;
                }
                for (int i = 0; i < segments; i++)
                {
                    float a0 = Mathf.Lerp(lon0, lon1, i / (float)segments), a1 = Mathf.Lerp(lon0, lon1, (i + 1) / (float)segments);
                    Vector3 a = P(lat0, a0), b = P(lat1, a0), c = P(lat1, a1), d = P(lat0, a1);
                    Vector3 outward = a + c;
                    if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) { Vector3 t = b; b = d; d = t; }
                    Quad(a, b, c, d, colour, false);
                    Quad(d, c, b, a, colour, false);
                }
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
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
