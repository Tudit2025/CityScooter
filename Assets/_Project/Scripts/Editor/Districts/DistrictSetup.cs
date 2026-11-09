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
    /// v0.3 districts (系统 A): puts every District asset under Data/Districts/ (any sub-folder) into the comic theme in
    /// route order (District.order), and builds a generic comic gateway (a gantry over the road with the district's
    /// name in the 3x5 pixel font of the hazard signs) for each district whose art left District.gateway empty.
    /// Also keeps District_WestPerth (the existing comic street, order 40) in step with the theme's street chunks and
    /// the scene look (Look_ComicCity), so West Perth looks exactly like the street did before districts.
    /// Works with any subset of the art districts present. Run it after the art builders.
    ///   Menu: CityScooter/Districts/Apply districts to theme     Batch: -executeMethod CityScooter.EditorTools.DistrictSetup.ApplyBatch
    /// </summary>
    public static class DistrictSetup
    {
        public const string DataFolder = "Assets/_Project/Data/Districts";
        public const string WestPerthPath = DataFolder + "/District_WestPerth.asset";
        public const string GatewayPrefabs = ComicCityBuilder.Prefabs + "/Districts/Gateways";
        public const string GatewayMeshes = ComicCityBuilder.Root + "/Districts/Gateways";
        public const string LookPath = ComicCityBuilder.Root + "/Look_ComicCity.asset";

        static Material _material, _ink;
        static ComicCityRecipe _r;

        [MenuItem("CityScooter/Districts/Apply districts to theme")]
        public static void Apply()
        {
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme == null) throw new InvalidOperationException("No theme at " + ComicCityBuilder.ThemePath + ": build the comic city first.");
            PrefabBuildUtility.EnsureFolder(DataFolder);
            EnsureWestPerth(theme);
            District[] route = Collect(DataFolder, out List<string> skipped);
            foreach (string s in skipped) Debug.LogWarning("[Districts] skipped " + s);
            BuildGateways(route);
            ApplyTo(theme, route);
            AssetDatabase.SaveAssets();
            var names = new List<string>();
            foreach (District d in route) names.Add(d.order + ":" + d.id);
            Debug.Log("[Districts] theme route: " + string.Join(", ", names));
        }

        public static void ApplyBatch()
        {
            try { Apply(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>All usable District assets under a folder (recursively), in route order. Unusable ones (no chunks,
        /// a missing chunk, a bad id) are left out and named in <paramref name="skipped"/>, so the theme's
        /// HasDistricts stays true for the rest.</summary>
        public static District[] Collect(string folder, out List<string> skipped)
        {
            skipped = new List<string>();
            var found = new List<District>();
            if (!AssetDatabase.IsValidFolder(folder)) return Array.Empty<District>();
            var ids = new HashSet<string>();
            foreach (District d in DistrictRoute.Sort(LoadAll(folder)))
            {
                string why = Problem(d);
                if (why == null && !ids.Add(d.id)) why = "the id is used twice";
                if (why != null) { skipped.Add(AssetDatabase.GetAssetPath(d) + ": " + why); continue; }
                found.Add(d);
            }
            return found.ToArray();
        }

        static List<District> LoadAll(string folder)
        {
            var list = new List<District>();
            foreach (string guid in AssetDatabase.FindAssets("t:District", new[] { folder }))
            {
                var d = AssetDatabase.LoadAssetAtPath<District>(AssetDatabase.GUIDToAssetPath(guid));
                if (d != null) list.Add(d);
            }
            return list;
        }

        /// <summary>Why a district cannot go into the theme (null = fine).</summary>
        public static string Problem(District d)
        {
            if (d == null) return "missing";
            if (string.IsNullOrEmpty(d.id)) return "no id";
            foreach (char c in d.id) if (c < 'a' || c > 'z') return "id '" + d.id + "' must be lower case letters only (it is sent as zone:" + d.id + ")";
            if (!d.HasChunks) return "no chunks (or a missing one)";
            return null;
        }

        /// <summary>Writes the route into the theme (route order).</summary>
        public static void ApplyTo(TrackTheme theme, District[] route)
        {
            var so = new SerializedObject(theme);
            SerializedProperty list = so.FindProperty("districts");
            list.arraySize = route.Length;
            for (int i = 0; i < route.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = route[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

        /// <summary>West Perth = the existing comic street: the theme's chunks in order, 4-lane boulevards allowed, and
        /// exactly the scene's look (Look_ComicCity) so it looks as it did before districts.</summary>
        public static District EnsureWestPerth(TrackTheme theme)
        {
            var d = AssetDatabase.LoadAssetAtPath<District>(WestPerthPath);
            bool created = d == null;
            if (created) { d = ScriptableObject.CreateInstance<District>(); AssetDatabase.CreateAsset(d, WestPerthPath); }
            d.id = "westperth";
            d.displayName = "WEST PERTH";
            d.order = 40;
            if (created) d.length = new Vector2(600f, 800f);
            d.chunks = theme.chunks != null ? (RoadChunk[])theme.chunks.Clone() : Array.Empty<RoadChunk>();
            d.orderedChunks = true;
            d.allowBoulevard = true;
            var look = AssetDatabase.LoadAssetAtPath<SceneLook>(LookPath);
            if (look != null) CopyLook(look, d);
            else Debug.LogWarning("[Districts] no " + LookPath + ": West Perth keeps its look colours");
            d.skyTint = Color.white;
            EditorUtility.SetDirty(d);
            return d;
        }

        /// <summary>A district's look from a scene look (fog, ambient, sun).</summary>
        public static void CopyLook(SceneLook look, District d)
        {
            d.fogColor = look.fogColor;
            d.ambientSky = look.ambientSky;
            d.ambientEquator = look.ambientEquator;
            d.ambientGround = look.ambientGround;
            d.sunColor = look.sunColor;
            d.sunIntensity = look.sunIntensity;
        }

        // ---------------------------------------------------------------- gateways

        static void BuildGateways(District[] route)
        {
            _r = AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(ComicCityBuilder.RecipePath);
            _material = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicPalette.mat");
            _ink = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicInk.mat");
            if (_r == null || _material == null || _ink == null) { Debug.LogWarning("[Districts] comic kit missing: no gateways built"); return; }
            PrefabBuildUtility.EnsureFolder(GatewayPrefabs);
            PrefabBuildUtility.EnsureFolder(GatewayMeshes);
            foreach (District d in route)
            {
                string mine = GatewayPath(d);
                string current = d.gateway != null ? AssetDatabase.GetAssetPath(d.gateway) : null;
                if (!string.IsNullOrEmpty(current) && current != mine) continue; // the art agent's own gateway
                GameObject prefab = Gateway(d);
                if (d.gateway == prefab) continue;
                d.gateway = prefab;
                EditorUtility.SetDirty(d);
            }
        }

        public static string GatewayPath(District d) => GatewayPrefabs + "/GW_" + Pascal(d.id) + ".prefab";

        static string Pascal(string id) => string.IsNullOrEmpty(id) ? "District" : char.ToUpperInvariant(id[0]) + id.Substring(1);

        // A comic gantry over the whole street: two striped posts just outside the rideable width (|x| > 8), a double
        // beam and a yellow sign in the middle with the name in pixel letters, facing the rider (-Z). No colliders.
        static GameObject Gateway(District d)
        {
            string id = "GW_" + Pascal(d.id);
            string text = (d.displayName ?? d.id ?? "").ToUpperInvariant();
            var m = new ComicMesh();
            Color steel = new Color(.42f, .45f, .5f), white = new Color(.95f, .95f, .93f);
            const float postX = 8.75f, top = 6.3f;
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * postX, .2f, 0f), new Vector3(1f, .4f, 1f), _r.ink);
                for (int band = 0; band < 6; band++)
                    m.Box(new Vector3(s * postX, .4f + band * 1f + .5f, 0f), new Vector3(.45f, 1f, .45f), band % 2 == 0 ? _r.red : white);
                m.Box(new Vector3(s * postX, top + .15f, 0f), new Vector3(.6f, .3f, .6f), _r.ink);
            }
            m.Box(new Vector3(0f, top - .1f, 0f), new Vector3(postX * 2f + .6f, .35f, .35f), steel);
            m.Box(new Vector3(0f, top - 1.5f, 0f), new Vector3(postX * 2f + .6f, .25f, .3f), steel);
            for (int i = -3; i <= 3; i++) m.Box(new Vector3(i * 2.4f, top - .8f, 0f), new Vector3(.12f, 1.3f, .12f), steel);

            const float px = .16f;
            float textWidth = Mathf.Max(1, TextColumns(text)) * px;
            float boardW = Mathf.Min(postX * 2f - 1f, textWidth + 1.2f), boardH = 5f * px + .9f, cy = top - .8f;
            m.Box(new Vector3(0f, cy, -.3f), new Vector3(boardW + .24f, boardH + .24f, .08f), _r.ink);
            m.Box(new Vector3(0f, cy, -.36f), new Vector3(boardW, boardH, .08f), _r.yellow);
            Text(m, text, 0f, cy, -.41f, Mathf.Min(px, (boardW - .6f) / Mathf.Max(1, TextColumns(text))), _r.ink);

            var root = new GameObject(id);
            try
            {
                var art = new GameObject("Art");
                art.transform.SetParent(root.transform, false);
                AddRenderer(art, id, m);
                string path = GatewayPath(d);
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
                if (!ok) throw new InvalidOperationException("Could not save " + path);
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void AddRenderer(GameObject go, string meshId, ComicMesh data)
        {
            Mesh mesh = SaveMesh(meshId, data);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _material;
            mr.shadowCastingMode = ShadowCastingMode.On;
            var ink = new GameObject("InkOutline");
            ink.transform.SetParent(go.transform, false);
            ink.AddComponent<MeshFilter>().sharedMesh = mesh;
            var imr = ink.AddComponent<MeshRenderer>();
            imr.sharedMaterial = _ink; imr.shadowCastingMode = ShadowCastingMode.Off; imr.receiveShadows = false;
        }

        static Mesh SaveMesh(string id, ComicMesh data)
        {
            string path = GatewayMeshes + "/" + id + ".asset";
            Mesh mesh = data.Bake(id);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        // ---- pixel font: 3x5 glyphs (copied from HazardKitBuilder, plus F J Q V X and digits for district names),
        // quads facing -Z (towards the rider), no crease ink. W and M are 5 wide: a 3-wide W read as H ("HARNBRO").
        public static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "010101111101101", ['C'] = "011100100100011", ['D'] = "110101101101110", ['E'] = "111100110100111",
            ['H'] = "101101111101101", ['I'] = "111010010010111", ['K'] = "101101110101101", ['L'] = "100100100100111",
            ['O'] = "010101101101010", ['P'] = "110101110100100", ['R'] = "110101110101101", ['S'] = "011100010001110",
            ['T'] = "111010010010010", ['U'] = "101101101101111", ['W'] = "1000110001101011010101010",
            ['B'] = "110101110101110", ['G'] = "011100101101011", ['N'] = "110101101101101", ['Y'] = "101101010010010", ['!'] = "010010010000010", ['.'] = "000000000000010", ['Z'] = "111001010100111", ['M'] = "1000111011101011000110001",
            ['F'] = "111100110100100", ['J'] = "001001001101010", ['Q'] = "010101101110011", ['V'] = "101101101101010", ['X'] = "101101010101101",
            ['0'] = "111101101101111", ['1'] = "010110010010111", ['2'] = "110001010100111", ['3'] = "110001010001110", ['4'] = "101101111001001",
            ['5'] = "111100110001110", ['6'] = "011100111101111", ['7'] = "111001010010010", ['8'] = "111101111101111", ['9'] = "111101111001110",
            ['\''] = "010010000000000", ['-'] = "000000111000000",
        };

        /// <summary>True when every character of the text can be drawn (spaces are gaps).</summary>
        public static bool CanWrite(string text)
        {
            foreach (char c in text ?? "") if (c != ' ' && !Glyphs.ContainsKey(c)) return false;
            return true;
        }

        /// <summary>Glyph width in pixels: 5 rows, so 3 or 5 columns (a space is 3).</summary>
        static int GlyphWidth(char c) => Glyphs.TryGetValue(c, out string g) ? g.Length / 5 : 3;

        /// <summary>Pixel columns the text takes: glyph widths plus one empty column between glyphs.</summary>
        public static int TextColumns(string text)
        {
            int columns = 0;
            foreach (char c in text ?? "") columns += GlyphWidth(c) + 1;
            return Mathf.Max(0, columns - 1);
        }

        static void Text(ComicMesh m, string text, float cx, float cy, float z, float px, Color c)
        {
            float width = TextColumns(text) * px, x0 = cx - width / 2, y0 = cy + 2.5f * px;
            int left = 0;
            for (int i = 0; i < text.Length; i++)
            {
                int w = GlyphWidth(text[i]);
                if (Glyphs.TryGetValue(text[i], out string g))
                    for (int row = 0; row < 5; row++)
                        for (int col = 0; col < w; col++)
                        {
                            if (g[row * w + col] != '1') continue;
                            float x = x0 + (left + col) * px, y = y0 - (row + 1) * px;
                            m.Quad(new Vector3(x + px, y, z), new Vector3(x, y, z), new Vector3(x, y + px, z), new Vector3(x + px, y + px, z), c, false);
                        }
                left += w + 1;
            }
        }
    }
}
