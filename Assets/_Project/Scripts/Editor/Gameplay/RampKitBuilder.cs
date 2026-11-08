using System;
using System.Collections.Generic;
using CityScooter.EditorTools.Art;
using CityScooter.Gameplay;
using CityScooter.Rendering;
using CityScooter.Track;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CityScooter.EditorTools
{
    /// <summary>
    /// v0.3 generic ramps in the comic style, all generated (no imported models), for districts without their own:
    ///   RMP_Kicker    a wood-and-steel kicker, 3 m wide, 1.4 m high over a 5 m run-up (Ramp defaults),
    ///   RMP_Platform  a longer, higher ramp with a flat top before the lip (2 m high, 7 m run-up, 6 m top),
    /// with bright yellow chevrons on the run-up and a hazard-striped lip so they read from the chase camera, plus the
    /// comic pop-ups SPLASH!, WHOOPS!, SPIN!, FLIP!, SKETCHY!, LANDED! and a water splash. Everything uses the tech
    /// artist's ComicPalette/ComicInk materials (vertex colours), like HazardKitBuilder. Writes theme.ramps and the
    /// pop-up fields of the theme (TrackTheme "Ramps and air").
    ///   Menu: CityScooter/Comic City/4 - Build ramps     Batch: -executeMethod CityScooter.EditorTools.RampKitBuilder.BuildBatch
    /// Run after HazardKitBuilder and BoulevardBuilder.
    /// </summary>
    public static class RampKitBuilder
    {
        public const string PrefabFolder = ComicCityBuilder.Prefabs + "/Ramps";
        const string MeshFolder = ComicCityBuilder.Root + "/Ramps";
        public const string KickerPath = PrefabFolder + "/RMP_Kicker.prefab";
        public const string PlatformPath = PrefabFolder + "/RMP_Platform.prefab";

        static Material _material, _ink;
        static ComicCityRecipe _r;

        static readonly Color White = new Color(.95f, .95f, .93f);
        static readonly Color WoodLight = new Color(.93f, .66f, .36f);
        static readonly Color WoodDark = new Color(.80f, .52f, .27f);
        static readonly Color SteelBlue = new Color(.16f, .46f, .82f);
        static readonly Color SteelDark = new Color(.24f, .27f, .33f);
        static readonly Color Concrete = new Color(.80f, .78f, .72f);
        static readonly Color DeckRed = new Color(.88f, .25f, .18f);
        static readonly Color Water = new Color(.25f, .62f, .95f);
        static readonly Color Foam = new Color(.9f, .97f, 1f);

        [MenuItem("CityScooter/Comic City/4 - Build ramps")]
        public static void Build()
        {
            _r = AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(ComicCityBuilder.RecipePath);
            _material = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicPalette.mat");
            _ink = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicInk.mat");
            if (_r == null || _material == null || _ink == null) throw new InvalidOperationException("Build the comic city first (recipe, ComicPalette.mat, ComicInk.mat).");
            PrefabBuildUtility.EnsureFolder(PrefabFolder);
            PrefabBuildUtility.EnsureFolder(MeshFolder);

            Kicker();
            Platform();
            GameObject[] popups =
            {
                Popup("FxSplash", "SPLASH!", new Color(.55f, .85f, 1f), _r.ink, 1.15f),
                Popup("FxWhoops", "WHOOPS!", _r.yellow, _r.red, 1.15f),
                Popup("FxSpin", "SPIN!", new Color(.3f, .85f, 1f), _r.ink, .85f),
                Popup("FxFlip", "FLIP!", _r.yellow, _r.red, .85f),
                Popup("FxSketchy", "SKETCHY!", White, _r.red, 1.15f),
                Popup("FxLanded", "LANDED!", new Color(.55f, .9f, .4f), _r.ink, 1.1f),
                WaterSplash(),
            };
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme != null) ApplyTo(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("[Ramps] built 2 ramps and " + popups.Length + " pop-ups" + (theme != null ? ", theme updated" : " (no theme)"));
        }

        /// <summary>Puts the generic ramps and the air pop-ups into a theme.</summary>
        public static void ApplyTo(TrackTheme theme)
        {
            var ramps = new List<Ramp>();
            foreach (string path in new[] { KickerPath, PlatformPath })
            {
                Ramp r = AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<Ramp>();
                if (r != null) ramps.Add(r);
            }
            theme.ramps = ramps.ToArray();
            GameObject Fx(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/RMP_" + id + ".prefab");
            theme.splashPopup = Fx("FxSplash"); theme.whoopsPopup = Fx("FxWhoops"); theme.splashEffect = Fx("FxSplashWater");
            theme.spinPopup = Fx("FxSpin"); theme.flipPopup = Fx("FxFlip"); theme.sketchyPopup = Fx("FxSketchy"); theme.landedPopup = Fx("FxLanded");
            EditorUtility.SetDirty(theme);
        }

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        // ---------------------------------------------------------------- ramps

        // A surface quad with its normal turned towards `facing` (whatever order the corners come in).
        static void Face(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector3 facing, bool ink = true)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0f) m.Quad(a, d, c, b, color, ink);
            else m.Quad(a, b, c, d, color, ink);
        }

        static void FaceTri(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, Color color, Vector3 facing)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0f) m.Tri(a, c, b, color);
            else m.Tri(a, b, c, color);
        }

        // Point on the run-up surface: u across (-w/2..w/2), s along the slope (0 at the start, 1 at the top).
        static Vector3 Slope(float u, float s, float rise, float height, float lift = 0f) =>
            new Vector3(u, s * height, s * rise) + new Vector3(0f, rise, -height).normalized * lift;

        static void Kicker()
        {
            const float w = 3f, rise = 5f, h = 1.4f, deck = .1f;
            var m = new ComicMesh();
            Vector3 up = new Vector3(0f, rise, -h).normalized;
            // Deck: planks across the run-up (alternating wood), then big yellow chevrons pointing up the ramp.
            const int planks = 10;
            for (int i = 0; i < planks; i++)
            {
                float s0 = i / (float)planks, s1 = (i + 1) / (float)planks;
                Face(m, Slope(-w / 2, s0, rise, h), Slope(w / 2, s0, rise, h), Slope(w / 2, s1, rise, h), Slope(-w / 2, s1, rise, h),
                    (i & 1) == 0 ? WoodLight : WoodDark, up, false);
            }
            for (int i = 0; i < 3; i++) Chevron(m, .16f + i * .25f, w * .36f, .1f, .07f, rise, h, _r.yellow, up);
            // Steel side rails along the deck edges (blue, so the ramp's outline reads from behind).
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * w / 2;
                Vector3 outward = new Vector3(side, 0f, 0f);
                // Side panel (triangle) and the rail on top of it.
                FaceTri(m, new Vector3(x, 0f, 0f), new Vector3(x, 0f, rise), new Vector3(x, h, rise), SteelBlue, outward);
                m.Beam(new Vector3(x, .04f, 0f), new Vector3(x, h + deck, rise), .12f, SteelBlue);
                // White stripe and struts on the side panel.
                Face(m, new Vector3(x + side * .01f, .22f, 1.6f), new Vector3(x + side * .01f, .22f, rise - .1f), new Vector3(x + side * .01f, .36f, rise - .1f), new Vector3(x + side * .01f, .36f + .2f, 2.1f), White, outward, false);
                for (int k = 1; k <= 3; k++)
                {
                    float z = rise * k / 4f;
                    m.Beam(new Vector3(x + side * .03f, 0f, z), new Vector3(x + side * .03f, h * z / rise, z), .07f, SteelDark);
                }
            }
            // The lip: a steel edge with hazard stripes, and the back face.
            HazardBar(m, -w / 2, w / 2, h + deck * .5f, rise, .16f);
            Face(m, new Vector3(-w / 2, 0f, rise), new Vector3(w / 2, 0f, rise), new Vector3(w / 2, h, rise), new Vector3(-w / 2, h, rise), SteelDark, Vector3.forward);
            // A steel toe plate where the run-up meets the road (no step for the wheels).
            Face(m, new Vector3(-w / 2, .005f, -.35f), new Vector3(w / 2, .005f, -.35f), new Vector3(w / 2, .02f, .02f), new Vector3(-w / 2, .02f, .02f), SteelDark, Vector3.up, false);
            Save("RMP_Kicker", KickerPath, m, r => { r.width = w; r.rise = rise; r.height = h; r.top = 0f; r.launch = 1f; r.hangGravity = .5f; r.lanes = new[] { -1, 0, 1 }; });
        }

        static void Platform()
        {
            const float w = 3.4f, rise = 7f, h = 2f, top = 6f;
            var m = new ComicMesh();
            Vector3 up = new Vector3(0f, rise, -h).normalized;
            // Run-up: red deck with three yellow chevrons and white edge lines.
            Face(m, Slope(-w / 2, 0f, rise, h), Slope(w / 2, 0f, rise, h), Slope(w / 2, 1f, rise, h), Slope(-w / 2, 1f, rise, h), DeckRed, up, false);
            foreach (float side in new[] { -1f, 1f })
                Face(m, Slope(side * (w / 2 - .18f), 0f, rise, h, .01f), Slope(side * (w / 2 - .06f), 0f, rise, h, .01f), Slope(side * (w / 2 - .06f), 1f, rise, h, .01f), Slope(side * (w / 2 - .18f), 1f, rise, h, .01f), White, up, false);
            for (int i = 0; i < 3; i++) Chevron(m, .14f + i * .27f, w * .34f, .09f, .065f, rise, h, _r.yellow, up);
            // Flat top: red with white edge lines and a checkered strip before the lip.
            Face(m, new Vector3(-w / 2, h, rise), new Vector3(w / 2, h, rise), new Vector3(w / 2, h, rise + top), new Vector3(-w / 2, h, rise + top), DeckRed, Vector3.up, false);
            foreach (float side in new[] { -1f, 1f })
                Face(m, new Vector3(side * (w / 2 - .18f), h + .01f, rise), new Vector3(side * (w / 2 - .06f), h + .01f, rise), new Vector3(side * (w / 2 - .06f), h + .01f, rise + top), new Vector3(side * (w / 2 - .18f), h + .01f, rise + top), White, Vector3.up, false);
            const int checks = 8;
            for (int i = 0; i < checks; i++)
                for (int j = 0; j < 2; j++)
                {
                    if (((i + j) & 1) == 1) continue;
                    float x0 = -w / 2 + w * i / checks, x1 = -w / 2 + w * (i + 1) / checks, z0 = rise + top - 1.1f + j * .45f, z1 = z0 + .45f;
                    Face(m, new Vector3(x0, h + .012f, z0), new Vector3(x1, h + .012f, z0), new Vector3(x1, h + .012f, z1), new Vector3(x0, h + .012f, z1), White, Vector3.up, false);
                }
            // Sides: concrete panels (a triangle under the run-up, a block under the top) with a blue stripe.
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * w / 2;
                Vector3 outward = new Vector3(side, 0f, 0f);
                FaceTri(m, new Vector3(x, 0f, 0f), new Vector3(x, 0f, rise), new Vector3(x, h, rise), Concrete, outward);
                Face(m, new Vector3(x, 0f, rise), new Vector3(x, 0f, rise + top), new Vector3(x, h, rise + top), new Vector3(x, h, rise), Concrete, outward);
                Face(m, new Vector3(x + side * .01f, h - .55f, rise), new Vector3(x + side * .01f, h - .55f, rise + top), new Vector3(x + side * .01f, h - .3f, rise + top), new Vector3(x + side * .01f, h - .3f, rise), SteelBlue, outward, false);
                m.Beam(new Vector3(x, .04f, 0f), new Vector3(x, h + .08f, rise), .12f, SteelBlue);
                m.Beam(new Vector3(x, h + .08f, rise), new Vector3(x, h + .08f, rise + top), .12f, SteelBlue);
            }
            HazardBar(m, -w / 2, w / 2, h + .06f, rise + top, .18f);
            Face(m, new Vector3(-w / 2, 0f, rise + top), new Vector3(w / 2, 0f, rise + top), new Vector3(w / 2, h, rise + top), new Vector3(-w / 2, h, rise + top), Concrete, Vector3.forward);
            Face(m, new Vector3(-w / 2, .005f, -.35f), new Vector3(w / 2, .005f, -.35f), new Vector3(w / 2, .02f, .02f), new Vector3(-w / 2, .02f, .02f), SteelDark, Vector3.up, false);
            Save("RMP_Platform", PlatformPath, m, r => { r.width = w; r.rise = rise; r.height = h; r.top = top; r.launch = .8f; r.hangGravity = .5f; r.lanes = new[] { -1, 0, 1 }; });
        }

        // A ">" chevron on the run-up pointing up the slope: centre at s, half width a, height dh (in s), thickness t.
        static void Chevron(ComicMesh m, float s, float a, float dh, float t, float rise, float h, Color c, Vector3 up)
        {
            const float lift = .012f;
            Face(m, Slope(-a, s, rise, h, lift), Slope(-a, s + t, rise, h, lift), Slope(0f, s + dh + t, rise, h, lift), Slope(0f, s + dh, rise, h, lift), c, up, false);
            Face(m, Slope(0f, s + dh, rise, h, lift), Slope(0f, s + dh + t, rise, h, lift), Slope(a, s + t, rise, h, lift), Slope(a, s, rise, h, lift), c, up, false);
        }

        // Yellow / black hazard stripes on a bar across the lip.
        static void HazardBar(ComicMesh m, float x0, float x1, float y, float z, float size)
        {
            const int stripes = 10;
            for (int i = 0; i < stripes; i++)
            {
                float a = Mathf.Lerp(x0, x1, i / (float)stripes), b = Mathf.Lerp(x0, x1, (i + 1) / (float)stripes);
                m.Box(new Vector3((a + b) * .5f, y, z - size * .5f), new Vector3(b - a, size, size), (i & 1) == 0 ? _r.yellow : _r.ink);
            }
        }

        static void Save(string id, string path, ComicMesh body, Action<Ramp> configure)
        {
            var root = new GameObject(id);
            try
            {
                var art = new GameObject("Art");
                art.transform.SetParent(root.transform, false);
                AddRenderer(art, id, body, true);
                var ramp = root.AddComponent<Ramp>();
                configure(ramp);
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
                if (!ok) throw new InvalidOperationException("Could not save " + path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void AddRenderer(GameObject go, string meshId, ComicMesh data, bool outline)
        {
            Mesh mesh = SaveMesh(meshId, data);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _material;
            mr.shadowCastingMode = outline ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (!outline) return;
            var ink = new GameObject("InkOutline");
            ink.transform.SetParent(go.transform, false);
            ink.AddComponent<MeshFilter>().sharedMesh = mesh;
            var imr = ink.AddComponent<MeshRenderer>();
            imr.sharedMaterial = _ink; imr.shadowCastingMode = ShadowCastingMode.Off; imr.receiveShadows = false;
        }

        static Mesh SaveMesh(string id, ComicMesh data)
        {
            string path = MeshFolder + "/" + id + ".asset";
            Mesh mesh = data.Bake(id);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        // ---------------------------------------------------------------- pop-ups (HazardKitBuilder's comic style)

        // Pixel font 3x5 (HazardKitBuilder's glyphs, plus F), quads facing -Z, no crease ink.
        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "010101111101101", ['C'] = "011100100100011", ['D'] = "110101101101110", ['E'] = "111100110100111",
            ['F'] = "111100110100100", ['H'] = "101101111101101", ['I'] = "111010010010111", ['K'] = "101101110101101",
            ['L'] = "100100100100111", ['O'] = "010101101101010", ['P'] = "110101110100100", ['R'] = "110101110101101",
            ['S'] = "011100010001110", ['T'] = "111010010010010", ['U'] = "101101101101111", ['W'] = "101101101111101",
            ['B'] = "110101110101110", ['G'] = "011100101101011", ['N'] = "110101101101101", ['Y'] = "101101010010010",
            ['!'] = "010010010000010", ['.'] = "000000000000010", ['Z'] = "111001010100111", ['M'] = "101111111101101",
        };

        static void Text(ComicMesh m, string text, float cx, float cy, float z, float px, Color c)
        {
            float width = (text.Length * 4 - 1) * px, x0 = cx - width / 2, y0 = cy + 2.5f * px;
            for (int i = 0; i < text.Length; i++)
            {
                if (!Glyphs.TryGetValue(text[i], out string g)) continue;
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < 3; col++)
                    {
                        if (g[row * 3 + col] != '1') continue;
                        float x = x0 + (i * 4 + col) * px, y = y0 - (row + 1) * px;
                        m.Quad(new Vector3(x + px, y, z), new Vector3(x, y, z), new Vector3(x, y + px, z), new Vector3(x + px, y + px, z), c, false);
                    }
            }
        }

        // Comic burst with pixel-font text, facing -Z; shown by ComicPopup (billboard to the camera).
        static GameObject Popup(string id, string text, Color fill, Color ink, float radius)
        {
            var m = new ComicMesh();
            void Burst(float r, float z, Color c)
            {
                const int points = 12;
                for (int i = 0; i < points * 2; i++)
                {
                    float a0 = i * Mathf.PI / points, a1 = (i + 1) * Mathf.PI / points;
                    float r0 = (i & 1) == 0 ? r : r * .68f, r1 = (i & 1) == 0 ? r * .68f : r;
                    var p0 = new Vector3(Mathf.Cos(a0) * r0 * 1.25f, Mathf.Sin(a0) * r0, z);
                    var p1 = new Vector3(Mathf.Cos(a1) * r1 * 1.25f, Mathf.Sin(a1) * r1, z);
                    m.Tri(new Vector3(0, 0, z), p1, p0, c);
                }
            }
            Burst(radius * 1.12f, .02f, _r.ink);
            Burst(radius, 0f, fill);
            float px = Mathf.Min(radius * .16f, radius * 1.7f / Mathf.Max(1, text.Length * 4 - 1));
            Text(m, text, 0, 0, -.02f, px, ink);
            return SaveFx(id, m, .8f, .7f);
        }

        // Water thrown up where the rider drops into a river gap: blue drops and white foam, facing -Z.
        static GameObject WaterSplash()
        {
            var m = new ComicMesh();
            const int drops = 11;
            for (int i = 0; i < drops; i++)
            {
                float a = Mathf.Lerp(.18f, Mathf.PI - .18f, i / (drops - 1f)), len = (i & 1) == 0 ? 2.1f : 1.5f, w = .16f;
                Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f), n = new Vector3(-dir.y, dir.x, 0f);
                m.Tri(dir * len, dir * .25f - n * w, dir * .25f + n * w, Water);
                m.Tri(dir * (len + .32f) + n * .0f, dir * (len + .12f) - n * .1f, dir * (len + .12f) + n * .1f, Water);
            }
            // Foam crest: a white half disc with ink edge.
            const int seg = 12;
            for (int i = 0; i < seg; i++)
            {
                float a0 = Mathf.PI * i / seg, a1 = Mathf.PI * (i + 1) / seg;
                m.Tri(new Vector3(0f, 0f, .01f), new Vector3(Mathf.Cos(a1) * 1.05f, Mathf.Sin(a1) * .55f, .01f), new Vector3(Mathf.Cos(a0) * 1.05f, Mathf.Sin(a0) * .55f, .01f), _r.ink);
                m.Tri(new Vector3(0f, 0f, -.01f), new Vector3(Mathf.Cos(a1) * .95f, Mathf.Sin(a1) * .48f, -.01f), new Vector3(Mathf.Cos(a0) * .95f, Mathf.Sin(a0) * .48f, -.01f), Foam);
            }
            return SaveFx("FxSplashWater", m, .65f, .35f);
        }

        static GameObject SaveFx(string id, ComicMesh m, float life, float rise)
        {
            string path = PrefabFolder + "/RMP_" + id + ".prefab";
            var root = new GameObject("RMP_" + id);
            try
            {
                var art = new GameObject("Art");
                art.transform.SetParent(root.transform, false);
                AddRenderer(art, "RMP_" + id, m, false);
                var popup = root.AddComponent<ComicPopup>();
                popup.life = life; popup.rise = rise;
                foreach (MeshRenderer mr in root.GetComponentsInChildren<MeshRenderer>(true)) { mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; }
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
                if (!ok) throw new InvalidOperationException("Could not save " + path);
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
