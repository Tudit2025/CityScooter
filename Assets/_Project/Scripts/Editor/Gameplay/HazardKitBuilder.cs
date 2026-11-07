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
    /// v0.2 road hazards in the comic style, all generated (no imported models): police car, roadworks truck with an
    /// arrow board, cars, workers, a traffic controller, an open manhole, signs with pixel-font text, cones,
    /// barriers and comic gifts. Everything uses ComicPalette/ComicInk, so it follows the city bend and fake turns.
    /// Also writes the hazard patterns (roadworks zone, police stop, traffic...) and puts them in the theme.
    ///   Menu: CityScooter/Hazards/Build hazard kit       Batch: -executeMethod CityScooter.EditorTools.HazardKitBuilder.BuildBatch
    /// No real agency logos or crests: generic POLICE text and WA-style blue/white checks only.
    /// </summary>
    public static class HazardKitBuilder
    {
        const string Prefabs = ComicCityBuilder.Prefabs + "/Hazards";
        const string Meshes = ComicCityBuilder.Root + "/Hazards";
        const string Patterns = "Assets/_Project/Data/Hazards";

        static Material _material, _ink;
        static ComicCityRecipe _r;

        // Palette additions (sRGB). Kept generic: no agency branding.
        static readonly Color White = new Color(.95f, .95f, .93f);
        static readonly Color PoliceBlue = new Color(.08f, .22f, .62f);
        static readonly Color HiVis = new Color(1f, .45f, .05f);
        static readonly Color Silver = new Color(.82f, .86f, .9f);
        static readonly Color Navy = new Color(.16f, .2f, .32f);
        static readonly Color Skin = new Color(.86f, .64f, .5f);
        static readonly Color SignOrange = new Color(1f, .55f, .08f);
        static readonly Color Steel = new Color(.42f, .45f, .5f);
        static readonly Color LampOn = new Color(1f, .93f, .35f);
        static readonly Color LampOff = new Color(.35f, .3f, .12f);

        [MenuItem("CityScooter/Hazards/Build hazard kit")]
        public static void Build()
        {
            _r = AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(ComicCityBuilder.RecipePath);
            _material = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicPalette.mat");
            _ink = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicInk.mat");
            if (_r == null || _material == null || _ink == null) throw new InvalidOperationException("Build the comic city first (recipe, ComicPalette.mat, ComicInk.mat).");
            PrefabBuildUtility.EnsureFolder(Prefabs);
            PrefabBuildUtility.EnsureFolder(Meshes);
            PrefabBuildUtility.EnsureFolder(Patterns);

            var p = new Dictionary<string, GameObject>
            {
                ["Cone"] = Cone(),
                ["BarrierLong"] = BarrierLong(),
                ["Manhole"] = Manhole(),
                ["Worker"] = Worker(false),
                ["TrafficController"] = Worker(true),
                ["SignRoadWork"] = Sign("SignRoadWork", "ROAD WORK", "AHEAD", 0),
                ["SignDetourRight"] = Sign("SignDetourRight", "TAKE A", "DETOUR", 1),
                ["SignDetourLeft"] = Sign("SignDetourLeft", "TAKE A", "DETOUR", -1),
                ["PoliceCar"] = Car("PoliceCar", White, true, 0f, 0f),
                ["WorksTruck"] = WorksTruck(),
                ["CarOncomingRed"] = Car("CarOncomingRed", _r.red, false, -13f, 0f),
                ["CarOncomingBlue"] = Car("CarOncomingBlue", new Color(.2f, .5f, .85f), false, -16f, 0f),
                ["CarSlowYellow"] = Car("CarSlowYellow", _r.yellow, false, 0f, .3f),
                ["CarSlowGreen"] = Car("CarSlowGreen", _r.green, false, 0f, .35f),
                ["PedWalkerA"] = Person("PedWalkerA", Looks["Office"], -1.2f, false),
                ["PedWalkerB"] = Person("PedWalkerB", Looks["Shopper"], 1.1f, false),
                ["PedWalkerC"] = Person("PedWalkerC", Looks["Jogger"], -1.6f, false),
                ["PedCrosserA"] = Person("PedCrosserA", Looks["Student"], 1f, true),
                ["PedCrosserB"] = Person("PedCrosserB", Looks["Elder"], -0.8f, true),
                ["Zebra"] = Zebra(),
            };
            GameObject[] popups = { Popup("FxPow", "POW", _r.yellow, _r.red, 1f), Popup("FxDing", "DING", White, PoliceBlue, .55f),
                Popup("FxBang", "!", _r.yellow, _r.ink, .55f), Popup("FxSorry", "SORRY", White, _r.red, .9f),
                Popup("FxBoing", "BOING", _r.yellow, PoliceBlue, .9f), Popup("FxCrash", "CRASH!", White, _r.red, 1f),
                Popup("FxBonk", "BONK!", White, _r.ink, .8f), Swish(), Impact(), Popup("FxIgnore", "...", White, _r.ink, .5f),
                Popup("FxZoom", "ZOOM!", new Color(.3f, .85f, 1f), _r.ink, .85f) };
            PunchArmKit();
            foreach (KeyValuePair<string, Look> look in Looks) Person("Ped_" + look.Key, look.Value, 1f, false);
            CrashRider();
            CrashScooter();
            Gift[] gifts =
            {
                Present("GiftRed", _r.red, _r.yellow), Present("GiftBlue", new Color(.2f, .5f, .85f), _r.cream),
                Present("GiftGreen", _r.green, _r.red), Present("GiftYellow", _r.yellow, _r.red),
            };

            HazardPattern[] patterns = BuildPatterns(p);
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme != null) ApplyTo(theme);
            AssetDatabase.SaveAssets();
            if (popups.Length != 11) throw new InvalidOperationException("pop-ups");
            Debug.Log("[Hazards] built " + p.Count + " hazards, " + gifts.Length + " gifts, " + patterns.Length + " patterns");
        }

        /// <summary>Puts the comic gifts and hazard patterns into a theme (ComicCityBuilder.BuildTheme calls this too).</summary>
        public static void ApplyTo(TrackTheme theme)
        {
            var gifts = new List<Gift>();
            foreach (string id in new[] { "GiftRed", "GiftBlue", "GiftGreen", "GiftYellow" })
            {
                var g = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/HZ_" + id + ".prefab");
                if (g != null) gifts.Add(g.GetComponent<Gift>());
            }
            if (gifts.Count > 0) theme.gifts = gifts.ToArray();
            var patterns = new List<HazardPattern>();
            foreach (string guid in AssetDatabase.FindAssets("t:HazardPattern", new[] { Patterns }))
                patterns.Add(AssetDatabase.LoadAssetAtPath<HazardPattern>(AssetDatabase.GUIDToAssetPath(guid)));
            patterns.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            theme.hazards = patterns.ToArray();
            GameObject Fx(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/HZ_" + id + ".prefab");
            theme.punchPopup = Fx("FxPow"); theme.bellPopup = Fx("FxDing"); theme.startlePopup = Fx("FxBang"); theme.sorryPopup = Fx("FxSorry");
            theme.boingPopup = Fx("FxBoing"); theme.crashPopup = Fx("FxCrash");
            theme.bonkPopup = Fx("FxBonk"); theme.ignorePopup = Fx("FxIgnore"); theme.boostPopup = Fx("FxZoom"); theme.swishPopup = Fx("FxSwish"); theme.impactPopup = Fx("FxImpact");
            theme.punchArm = Fx("PunchArm");
            theme.crashRider = Fx("CrashRider"); theme.crashScooter = Fx("CrashScooter");
            var people = new List<GameObject>();
            foreach (string id in LookIds) { GameObject p = Fx("Ped_" + id); if (p != null) people.Add(p); }
            theme.pedestrians = people.ToArray();
            // Light props can be punched out of the way ("ko"); people, vehicles, barriers and signs cannot.
            if (theme.obstacles != null)
                foreach (Obstacle o in theme.obstacles)
                {
                    if (o == null) continue;
                    bool punch = o.name == "OBS_Crates" || o.name == "OBS_Bin" || o.name == "OBS_Cones";
                    if (o.punchable == punch) continue;
                    o.punchable = punch;
                    EditorUtility.SetDirty(o);
                    PrefabUtility.SavePrefabAsset(o.gameObject);
                }
            EditorUtility.SetDirty(theme);
        }

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        // ---------------------------------------------------------------- patterns

        static HazardPattern[] BuildPatterns(Dictionary<string, GameObject> p)
        {
            var list = new List<HazardPattern>();
            HazardPattern.Entry E(string id, float x, float z, string mirror = null) =>
                new HazardPattern.Entry { prefab = p[id], mirrorPrefab = mirror != null ? p[mirror] : null, x = x, z = z, yaw = id.StartsWith("CarOncoming") ? 180f : 0f };

            // Full roadworks zone: the left lane is closed, the open lane is x 0.6..3.6 the whole way.
            var works = new List<HazardPattern.Entry>
            {
                E("SignRoadWork", -4.9f, 0), E("SignDetourRight", -4.9f, 24, "SignDetourLeft"),
                E("TrafficController", -1.1f, 40), E("WorksTruck", -2.2f, 54),
                E("Worker", -3.3f, 62), E("Worker", -1.5f, 67), E("Manhole", -2.2f, 76),
            };
            float[] taperIn = { -3.6f, -2.8f, -2f, -1.2f, -.4f };
            for (int i = 0; i < taperIn.Length; i++) works.Add(E("Cone", taperIn[i], 12 + i * 5));
            for (int i = 0; i < 8; i++) works.Add(E("BarrierLong", -.35f, 45 + i * 6));
            for (int i = 0; i < 4; i++) works.Add(E("Cone", -.4f - i * .8f, 94 + i * 4));
            works.Add(E("Manhole", 1.9f, 112));
            list.Add(Pattern("HZ_RoadworksZone", works, 118, 0, 1.2f, true, 1, 30, .85f, 4.5f));

            list.Add(Pattern("HZ_RoadworksShort", new List<HazardPattern.Entry>
            {
                E("SignRoadWork", -4.9f, 0), E("Cone", -3.4f, 10), E("Cone", -2.4f, 14), E("Cone", -1.4f, 18),
                E("BarrierLong", -.6f, 24), E("Worker", -2.4f, 26), E("BarrierLong", -.6f, 30), E("Cone", -1.4f, 36),
            }, 40, 0, 1f, true));

            list.Add(Pattern("HZ_PoliceStop", new List<HazardPattern.Entry>
            {
                E("Cone", -3.5f, 10), E("Cone", -2.9f, 14), E("Cone", -2.3f, 18), E("PoliceCar", -2.5f, 30),
            }, 40, 0, .9f, true));

            list.Add(Pattern("HZ_Manhole", new List<HazardPattern.Entry> { E("Manhole", -2.2f, 12) }, 20, 0, .8f, true));
            list.Add(Pattern("HZ_ManholeMiddle", new List<HazardPattern.Entry> { E("Manhole", 0f, 12), E("Cone", -1.3f, 6), E("Cone", 1.3f, 6) }, 20, 2, .6f, true));

            // Traffic: Australia drives on the left, so oncoming cars use the right lane; never mirrored.
            list.Add(Pattern("HZ_OncomingCar", new List<HazardPattern.Entry> { E("CarOncomingRed", 2f, 50) }, 90, 2, 1f, false, meet: 50));
            list.Add(Pattern("HZ_OncomingPair", new List<HazardPattern.Entry> { E("CarOncomingBlue", 2f, 40), E("CarOncomingRed", 2.1f, 75) }, 110, 4, .6f, false, meet: 55));
            list.Add(Pattern("HZ_SlowCar", new List<HazardPattern.Entry> { E("CarSlowYellow", -2f, 50) }, 90, 1, .9f, false, meet: 50));
            list.Add(Pattern("HZ_SlowCarAndCones", new List<HazardPattern.Entry>
            {
                E("CarSlowGreen", -2f, 40), E("Cone", 3.2f, 70), E("Cone", 2.6f, 74),
            }, 90, 3, .6f, false, meet: 45));
            list.Add(Pattern("HZ_PoliceAndOncoming", new List<HazardPattern.Entry>
            {
                E("PoliceCar", -2.6f, 20), E("CarOncomingBlue", 2f, 75),
            }, 100, 5, .5f, false, meet: 70));
            // People: strollers on the sidewalk; crossers walk over the zebra when the rider is close (ring the bell!).
            list.Add(Pattern("HZ_SidewalkStroll", new List<HazardPattern.Entry>
            {
                E("PedWalkerA", -5.7f, 8), E("PedWalkerB", -6.5f, 26), E("PedWalkerC", 5.9f, 18), E("PedWalkerA", 6.4f, 40),
            }, 50, 0, .9f, true));
            list.Add(Pattern("HZ_Crossing", new List<HazardPattern.Entry>
            {
                E("Zebra", 0, 30), E("PedCrosserA", -5.6f, 30), E("PedWalkerB", 5.8f, 20),
            }, 45, 1, 1f, true));
            list.Add(Pattern("HZ_CrossingBusy", new List<HazardPattern.Entry>
            {
                E("Zebra", 0, 30), E("PedCrosserA", -5.6f, 29), E("PedCrosserB", 5.6f, 31.5f),
            }, 45, 3, .7f, true));
            return list.ToArray();
        }

        static HazardPattern Pattern(string name, List<HazardPattern.Entry> entries, float length, int minLevel, float weight, bool mirror,
            int turn = 0, float turnAt = 0, float turnStrength = .8f, float turnSeconds = 4f, float meet = 40f)
        {
            string path = Patterns + "/" + name + ".asset";
            var p = AssetDatabase.LoadAssetAtPath<HazardPattern>(path);
            if (p == null) { p = ScriptableObject.CreateInstance<HazardPattern>(); AssetDatabase.CreateAsset(p, path); }
            entries.Sort((a, b) => a.z.CompareTo(b.z));
            p.entries = entries.ToArray(); p.length = length; p.minLevel = minLevel; p.weight = weight; p.allowMirror = mirror;
            p.turnDirection = turn; p.turnAtZ = turnAt; p.turnStrength = turnStrength; p.turnSeconds = turnSeconds; p.meetAtZ = meet;
            EditorUtility.SetDirty(p);
            return p;
        }

        // ---------------------------------------------------------------- prefabs

        sealed class Kit
        {
            public readonly ComicMesh Body = new ComicMesh();
            public readonly Dictionary<string, ComicMesh> Parts = new Dictionary<string, ComicMesh>();
            public ComicMesh Part(string name) { if (!Parts.TryGetValue(name, out ComicMesh m)) Parts[name] = m = new ComicMesh(); return m; }
        }

        static void B(ComicMesh m, float x, float y, float z, float w, float h, float d, Color c) => m.Box(new Vector3(x, y, z), new Vector3(w, h, d), c);

        static GameObject Save(string id, Kit kit, Vector3 colliderCentre, Vector3 colliderSize, Action<GameObject> configure, bool trigger = false, bool decor = false, bool? outline = null)
        {
            var root = new GameObject("HZ_" + id);
            try
            {
                var art = new GameObject("Art");
                art.transform.SetParent(root.transform, false);
                AddRenderer(art, "HZ_" + id, kit.Body, outline ?? !decor);
                foreach (KeyValuePair<string, ComicMesh> part in kit.Parts)
                {
                    var go = new GameObject(part.Key);
                    go.transform.SetParent(art.transform, false);
                    AddRenderer(go, "HZ_" + id + "_" + part.Key, part.Value, false);
                }
                if (!decor)
                {
                    var box = root.AddComponent<BoxCollider>();
                    box.center = colliderCentre; box.size = colliderSize; box.isTrigger = trigger;
                }
                configure(root);
                string path = Prefabs + "/HZ_" + id + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
                if (!ok) throw new InvalidOperationException("Could not save " + path);
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
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
            string path = Meshes + "/" + id + ".asset";
            Mesh mesh = data.Bake(id);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        static void Blink(GameObject root, string a, string b, float rate)
        {
            var blink = root.AddComponent<Blinker>();
            Transform art = root.transform.Find("Art");
            blink.groupA = new[] { art.Find(a).gameObject };
            blink.groupB = b != null ? new[] { art.Find(b).gameObject } : Array.Empty<GameObject>();
            blink.alternate = b != null;
            blink.flashesPerSecond = rate;
        }

        static void MakeObstacle(GameObject root, ObstacleKind kind, bool jump)
        {
            var o = root.AddComponent<Obstacle>();
            o.kind = kind; o.canJumpOver = jump; o.spawnWeight = 1f;
        }

        // ---- pixel font: 3x5 glyphs, quads facing -Z (towards the rider), no crease ink.
        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "010101111101101", ['C'] = "011100100100011", ['D'] = "110101101101110", ['E'] = "111100110100111",
            ['H'] = "101101111101101", ['I'] = "111010010010111", ['K'] = "101101110101101", ['L'] = "100100100100111",
            ['O'] = "010101101101010", ['P'] = "110101110100100", ['R'] = "110101110101101", ['S'] = "011100010001110",
            ['T'] = "111010010010010", ['U'] = "101101101101111", ['W'] = "101101101111101",
            ['B'] = "110101110101110", ['G'] = "011100101101011", ['N'] = "110101101101101", ['Y'] = "101101010010010", ['!'] = "010010010000010", ['.'] = "000000000000010", ['Z'] = "111001010100111", ['M'] = "101111111101101",
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

        static GameObject Cone()
        {
            var k = new Kit();
            ConeShape(k.Body, 0, 0, 1f);
            return Save("Cone", k, new Vector3(0, .36f, 0), new Vector3(.45f, .72f, .45f), r => { MakeObstacle(r, ObstacleKind.Thing, true); r.GetComponent<Obstacle>().punchable = true; });
        }

        static void ConeShape(ComicMesh m, float x, float z, float s)
        {
            B(m, x, .03f * s, z, .44f * s, .06f * s, .44f * s, _r.ink);
            m.Cylinder(new Vector3(x, .38f * s, z), .18f * s, .64f * s, SignOrange, 8, .12f);
            m.Cylinder(new Vector3(x, .42f * s, z), .125f * s, .12f * s, White, 8, .85f);
        }

        static GameObject BarrierLong()
        {
            var k = new Kit();
            B(k.Body, 0, .06f, -1.2f, .5f, .12f, .3f, _r.ink); B(k.Body, 0, .06f, 1.2f, .5f, .12f, .3f, _r.ink);
            B(k.Body, 0, .58f, 0, .16f, .6f, 2.8f, White);
            for (int i = -3; i <= 3; i++)
                for (int side = -1; side <= 1; side += 2)
                    k.Body.Box(new Vector3(side * .085f, .58f, i * .38f), new Vector3(.02f, .52f, .14f), _r.red, Quaternion.Euler(35, 0, 0));
            B(k.Body, 0, .95f, 1.2f, .12f, .14f, .12f, LampOff);
            B(k.Part("Lamp"), 0, .95f, 1.2f, .14f, .16f, .14f, LampOn);
            return Save("BarrierLong", k, new Vector3(0, .5f, 0), new Vector3(.3f, 1f, 2.8f), r => { MakeObstacle(r, ObstacleKind.Thing, false); Blink(r, "Lamp", null, 1.5f); });
        }

        static GameObject Manhole()
        {
            var k = new Kit();
            k.Body.Cylinder(new Vector3(0, .02f, 0), .7f, .04f, Steel, 12);
            k.Body.Cylinder(new Vector3(0, .035f, 0), .56f, .04f, new Color(.03f, .03f, .05f), 12);
            k.Body.Cylinder(new Vector3(.95f, .4f, .15f), .54f, .07f, Steel, 12, 1, Quaternion.Euler(0, 0, 72));
            ConeShape(k.Body, -.8f, -.55f, .85f);
            ConeShape(k.Body, .2f, -.85f, .85f);
            return Save("Manhole", k, new Vector3(0, .3f, 0), new Vector3(2f, .6f, 1.6f), r => MakeObstacle(r, ObstacleKind.Hole, true));
        }

        static GameObject Worker(bool controller)
        {
            var k = new Kit();
            ComicMesh m = k.Body;
            for (int s = -1; s <= 1; s += 2)
            {
                B(m, s * .12f, .06f, -.03f, .17f, .12f, .3f, _r.ink);
                B(m, s * .12f, .5f, 0, .18f, .78f, .21f, Navy);
                B(m, s * .33f, 1.13f, 0, .14f, .56f, .17f, HiVis);
                B(m, s * .33f, .8f, 0, .12f, .11f, .13f, Skin);
                B(m, s * .13f, 1.18f, 0, .07f, .62f, .3f, Silver);
            }
            B(m, 0, 1.15f, 0, .52f, .62f, .28f, HiVis);
            B(m, 0, .99f, 0, .54f, .06f, .3f, Silver); B(m, 0, 1.24f, 0, .54f, .06f, .3f, Silver);
            B(m, 0, 1.58f, 0, .25f, .27f, .25f, Skin);
            B(m, 0, 1.6f, -.13f, .16f, .04f, .02f, _r.ink);
            m.Cylinder(new Vector3(0, 1.76f, 0), .18f, .14f, controller ? White : _r.yellow, 8, .8f);
            B(m, 0, 1.7f, -.05f, .4f, .03f, .44f, controller ? White : _r.yellow);
            Vector3 size = new Vector3(.7f, 1.85f, .45f);
            if (controller)
            {
                B(m, -.34f, 1.15f, -.08f, .04f, 2.1f, .04f, _r.ink);
                m.Cylinder(new Vector3(-.34f, 2.15f, -.08f), .34f, .03f, _r.red, 8, 1, Quaternion.Euler(90, 0, 0));
                m.Cylinder(new Vector3(-.34f, 2.15f, -.08f), .3f, .035f, White, 8, 1, Quaternion.Euler(90, 0, 0));
                m.Cylinder(new Vector3(-.34f, 2.15f, -.08f), .27f, .04f, _r.red, 8, 1, Quaternion.Euler(90, 0, 0));
                Text(m, "STOP", -.34f, 2.15f, -.105f, .028f, White);
                size = new Vector3(1.1f, 2.5f, .45f);
            }
            return Save(controller ? "TrafficController" : "Worker", k, new Vector3(controller ? -.15f : 0, size.y / 2, 0), size,
                r => MakeObstacle(r, ObstacleKind.Person, false));
        }

        static GameObject Sign(string id, string line1, string line2, int arrow)
        {
            var k = new Kit();
            ComicMesh m = k.Body;
            float h = arrow == 0 ? .9f : 1.25f, cy = 1.1f + h / 2;
            for (int s = -1; s <= 1; s += 2)
            {
                k.Body.Box(new Vector3(s * .55f, .55f, .12f), new Vector3(.06f, 1.2f, .06f), _r.ink, Quaternion.Euler(-12, 0, 0));
                B(m, s * .55f, .03f, .2f, .14f, .06f, .5f, _r.ink);
            }
            B(m, 0, cy, .02f, 1.62f, h + .08f, .04f, _r.ink);
            B(m, 0, cy, 0, 1.52f, h - .02f, .05f, SignOrange);
            float px = .036f, top = cy + h / 2 - .2f;
            Text(m, line1, 0, top, -.03f, px, _r.ink);
            Text(m, line2, 0, top - .25f, -.03f, px, _r.ink);
            if (arrow != 0)
            {
                float ay = top - .58f, d = arrow;
                B(m, -.12f * d, ay, -.04f, .62f, .09f, .02f, _r.ink);
                k.Body.Box(new Vector3(.2f * d, ay + .09f, -.04f), new Vector3(.3f, .09f, .02f), _r.ink, Quaternion.Euler(0, 0, -40 * d));
                k.Body.Box(new Vector3(.2f * d, ay - .09f, -.04f), new Vector3(.3f, .09f, .02f), _r.ink, Quaternion.Euler(0, 0, 40 * d));
            }
            return Save(id, k, new Vector3(0, (cy + h / 2) / 2, .1f), new Vector3(1.6f, cy + h / 2, .6f), r => MakeObstacle(r, ObstacleKind.Thing, false));
        }

        static void CarBody(ComicMesh m, Color body, float length = 4.2f)
        {
            float hz = length / 2;
            B(m, 0, .58f, 0, 1.8f, .62f, length, body);
            B(m, 0, 1.17f, -.25f, 1.6f, .58f, 2.2f, body);
            B(m, 0, 1.18f, .87f, 1.42f, .42f, .03f, _r.glass);
            B(m, 0, 1.18f, -1.37f, 1.42f, .42f, .03f, _r.glass);
            for (int s = -1; s <= 1; s += 2)
            {
                B(m, s * .805f, 1.18f, -.25f, .02f, .42f, 1.9f, _r.glass);
                B(m, s * .6f, .7f, hz + .01f, .36f, .14f, .04f, _r.cream);
                B(m, s * .62f, .7f, -hz - .01f, .32f, .14f, .04f, _r.red);
                for (int e = -1; e <= 1; e += 2)
                {
                    m.Cylinder(new Vector3(s * .84f, .34f, e * (hz - .75f)), .34f, .24f, _r.ink, 8, 1, Quaternion.Euler(0, 0, 90));
                    m.Cylinder(new Vector3(s * .86f, .34f, e * (hz - .75f)), .15f, .25f, Silver, 8, 1, Quaternion.Euler(0, 0, 90));
                }
            }
            B(m, 0, .36f, hz + .05f, 1.84f, .2f, .1f, _r.ink);
            B(m, 0, .36f, -hz - .05f, 1.84f, .2f, .1f, _r.ink);
        }

        // Front at +Z. Oncoming cars are placed with yaw 180 (patterns) so their headlights face the rider.
        static GameObject Car(string id, Color body, bool police, float speed, float fraction)
        {
            var k = new Kit();
            ComicMesh m = k.Body;
            CarBody(m, body);
            if (police)
            {
                const float cell = .3f;
                for (int s = -1; s <= 1; s += 2)
                    for (int i = 0; i < 13; i++)
                        for (int row = 0; row < 2; row++)
                        {
                            float z = -1.95f + i * cell, y = .45f + row * cell, x = s * .905f;
                            Color c = ((i + row) & 1) == 0 ? PoliceBlue : White;
                            if (s > 0) m.Quad(new Vector3(x, y, z), new Vector3(x, y, z + cell), new Vector3(x, y + cell, z + cell), new Vector3(x, y + cell, z), c, false);
                            else m.Quad(new Vector3(x, y, z + cell), new Vector3(x, y, z), new Vector3(x, y + cell, z), new Vector3(x, y + cell, z + cell), c, false);
                        }
                Text(m, "POLICE", 0, .66f, -2.115f, .034f, PoliceBlue);
                B(m, 0, 1.5f, -.25f, 1.3f, .08f, .36f, _r.ink);
                B(m, -.33f, 1.6f, -.25f, .58f, .13f, .3f, new Color(.35f, .05f, .07f));
                B(m, .33f, 1.6f, -.25f, .58f, .13f, .3f, new Color(.05f, .08f, .3f));
                B(k.Part("LightRed"), -.33f, 1.6f, -.25f, .62f, .16f, .34f, new Color(1f, .12f, .12f));
                B(k.Part("LightBlue"), .33f, 1.6f, -.25f, .62f, .16f, .34f, new Color(.2f, .45f, 1f));
            }
            return Save(id, k, new Vector3(0, .85f, 0), new Vector3(1.9f, 1.7f, 4.3f), r =>
            {
                MakeObstacle(r, ObstacleKind.Vehicle, false);
                if (police) Blink(r, "LightRed", "LightBlue", 3f);
                if (speed != 0f || fraction > 0f) { var mv = r.AddComponent<Mover>(); mv.speed = speed; mv.fractionOfRiderSpeed = fraction; }
            });
        }

        // Front at +Z, arrow board at the back facing the rider (yaw 0).
        static GameObject WorksTruck()
        {
            var k = new Kit();
            ComicMesh m = k.Body;
            B(m, 0, .62f, .2f, 2f, .7f, 5.4f, White);
            B(m, 0, 1.45f, 1.55f, 1.9f, 1f, 1.9f, White);
            B(m, 0, 1.6f, 2.51f, 1.6f, .5f, .03f, _r.glass);
            for (int s = -1; s <= 1; s += 2)
            {
                B(m, s * .955f, 1.6f, 1.6f, .02f, .45f, 1.3f, _r.glass);
                B(m, s * 1.005f, .75f, .2f, .02f, .14f, 5.3f, HiVis);
                for (int e = -1; e <= 1; e += 2)
                    m.Cylinder(new Vector3(s * .92f, .38f, .2f + e * 1.75f), .38f, .26f, _r.ink, 8, 1, Quaternion.Euler(0, 0, 90));
            }
            B(m, 0, 2.02f, 1.55f, .3f, .14f, .3f, LampOff);
            B(k.Part("Beacon"), 0, 2.02f, 1.55f, .34f, .17f, .34f, LampOn);
            // rear: red/white chevrons on the bumper
            B(m, 0, .45f, -2.55f, 2.02f, .3f, .05f, White);
            for (int i = -3; i <= 3; i++) m.Box(new Vector3(i * .28f, .45f, -2.585f), new Vector3(.1f, .36f, .02f), _r.red, Quaternion.Euler(0, 0, 40));
            // arrow board on posts
            for (int s = -1; s <= 1; s += 2) B(m, s * .7f, 1.5f, -2.2f, .08f, 1.3f, .08f, _r.ink);
            B(m, 0, 2.45f, -2.25f, 1.8f, 1.05f, .1f, _r.ink);
            var lamps = new List<Vector2>();
            for (int i = 0; i < 7; i++) lamps.Add(new Vector2(-.6f + i * .13f, 0));
            for (int i = 1; i <= 3; i++) { lamps.Add(new Vector2(.4f - i * .1f, i * .1f)); lamps.Add(new Vector2(.4f - i * .1f, -i * .1f)); }
            lamps.Add(new Vector2(.4f, 0));
            foreach (Vector2 l in lamps)
            {
                B(m, l.x, 2.45f + l.y, -2.31f, .07f, .07f, .02f, LampOff);
                B(k.Part("Arrow"), l.x, 2.45f + l.y, -2.325f, .085f, .085f, .02f, LampOn);
            }
            return Save("WorksTruck", k, new Vector3(0, 1.5f, .2f), new Vector3(2.05f, 3f, 5.5f), r =>
            {
                MakeObstacle(r, ObstacleKind.Vehicle, false);
                Blink(r, "Arrow", null, 1.6f);
                var beacon = r.AddComponent<Blinker>();
                beacon.groupA = new[] { r.transform.Find("Art/Beacon").gameObject }; beacon.alternate = false; beacon.flashesPerSecond = 4f;
            });
        }

        // ---------------------------------------------------------------- people: one comic set for the whole street

        sealed class Look
        {
            public Color Skin, Top, Bottom, Shoes, Hair, Prop;
            public int HairStyle;          // 0 short, 1 long, 2 ponytail, 3 thin grey, 4 beanie, 5 cap, 6 sun hat, 7 curls
            public float Scale = 1f;       // kids ~0.72
            public bool Shorts, Skirt, Apron;
            public string Item;            // backpack, briefcase, coffee, surfboard, stick, bags, camera, dog, skateboard, balloon, delivery, headphones
        }

        static readonly Color SkinLight = new Color(.96f, .8f, .69f), SkinPink = new Color(.98f, .74f, .64f), SkinMedium = new Color(.86f, .64f, .5f),
            SkinTan = new Color(.74f, .53f, .37f), SkinBrown = new Color(.55f, .38f, .26f), SkinDark = new Color(.38f, .26f, .18f);
        static readonly Color HairDark = new Color(.16f, .11f, .09f), HairBlond = new Color(.9f, .72f, .35f), HairGrey = new Color(.78f, .78f, .76f),
            Denim = new Color(.24f, .36f, .6f), Khaki = new Color(.7f, .62f, .42f), Charcoal = new Color(.25f, .26f, .3f);

        static readonly string[] LookIds = { "Office", "Student", "Surfer", "Jogger", "Elder", "Tourist", "Barista", "Skater", "DogWalker", "Shopper", "Kid", "Courier" };

        // Perth street life, drawn like the rest of the comic city: flat colours, ink edges, simple blocks.
        static Dictionary<string, Look> Looks => new Dictionary<string, Look>
        {
            ["Office"] = new Look { Skin = SkinMedium, Top = Navy, Bottom = Navy, Shoes = _r.ink, Hair = HairDark, HairStyle = 0, Item = "briefcase", Prop = new Color(.4f, .25f, .15f) },
            ["Student"] = new Look { Skin = SkinLight, Top = new Color(.55f, .75f, .92f), Bottom = Charcoal, Shoes = _r.ink, Hair = Navy, HairStyle = 5, Scale = .76f, Shorts = true, Item = "backpack", Prop = _r.red },
            ["Surfer"] = new Look { Skin = SkinTan, Top = _r.yellow, Bottom = new Color(.1f, .6f, .62f), Shoes = _r.cream, Hair = HairBlond, HairStyle = 1, Shorts = true, Item = "surfboard", Prop = White },
            ["Jogger"] = new Look { Skin = SkinBrown, Top = new Color(1f, .42f, .62f), Bottom = _r.ink, Shoes = White, Hair = HairDark, HairStyle = 2, Item = "headphones", Prop = _r.yellow },
            ["Elder"] = new Look { Skin = SkinLight, Top = new Color(.86f, .76f, .56f), Bottom = new Color(.5f, .5f, .52f), Shoes = new Color(.4f, .25f, .15f), Hair = _r.cream, HairStyle = 6, Item = "stick", Prop = new Color(.45f, .3f, .18f) },
            ["Tourist"] = new Look { Skin = SkinPink, Top = _r.red, Bottom = Khaki, Shoes = White, Hair = _r.green, HairStyle = 5, Shorts = true, Item = "camera", Prop = _r.ink },
            ["Barista"] = new Look { Skin = SkinDark, Top = new Color(.15f, .15f, .18f), Bottom = Denim, Shoes = _r.ink, Hair = HairDark, HairStyle = 7, Apron = true, Item = "coffee", Prop = _r.green },
            ["Skater"] = new Look { Skin = SkinMedium, Top = new Color(.55f, .35f, .75f), Bottom = Denim, Shoes = _r.red, Hair = new Color(1f, .5f, .1f), HairStyle = 4, Item = "skateboard", Prop = _r.yellow },
            ["DogWalker"] = new Look { Skin = SkinDark, Top = _r.green, Bottom = Denim, Shoes = _r.cream, Hair = HairGrey, HairStyle = 3, Item = "dog", Prop = new Color(.85f, .6f, .3f) },
            ["Shopper"] = new Look { Skin = SkinTan, Top = new Color(1f, .6f, .2f), Bottom = _r.red, Shoes = _r.ink, Hair = HairDark, HairStyle = 1, Skirt = true, Item = "bags", Prop = new Color(.3f, .7f, .85f) },
            ["Kid"] = new Look { Skin = SkinMedium, Top = _r.yellow, Bottom = new Color(.2f, .45f, .8f), Shoes = _r.red, Hair = HairDark, HairStyle = 0, Scale = .7f, Shorts = true, Item = "balloon", Prop = _r.red },
            ["Courier"] = new Look { Skin = SkinLight, Top = new Color(.1f, .62f, .6f), Bottom = _r.ink, Shoes = _r.ink, Hair = new Color(.1f, .62f, .6f), HairStyle = 5, Item = "delivery", Prop = new Color(.1f, .62f, .6f) },
        };

        // Facing -Z (towards the rider), feet at the pivot. Root: Obstacle (Person) + Pedestrian; the Art child bobs.
        static GameObject Person(string id, Look l, float walk, bool crosses)
        {
            var k = new Kit();
            var m = new ComicMesh();
            for (int s = -1; s <= 1; s += 2)
            {
                B(m, s * .11f, .05f, -.03f, .16f, .1f, .28f, l.Shoes);
                if (l.Shorts || l.Skirt) { B(m, s * .11f, .7f, 0, .18f, .36f, .21f, l.Bottom); B(m, s * .11f, .32f, 0, .15f, .5f, .17f, l.Skin); }
                else B(m, s * .11f, .48f, 0, .17f, .82f, .2f, l.Bottom);
                B(m, s * .31f, 1.12f, 0, .13f, .5f, .16f, l.Top);
                B(m, s * .31f, .82f, 0, .11f, .12f, .12f, l.Skin);
            }
            if (l.Skirt) B(m, 0, .72f, 0, .5f, .4f, .32f, l.Bottom);
            B(m, 0, 1.12f, 0, .48f, .6f, .27f, l.Top);
            if (l.Apron) B(m, 0, 1f, -.145f, .4f, .62f, .02f, l.Prop);
            B(m, 0, 1.55f, 0, .25f, .27f, .25f, l.Skin);
            B(m, -.06f, 1.58f, -.13f, .04f, .05f, .02f, _r.ink); B(m, .06f, 1.58f, -.13f, .04f, .05f, .02f, _r.ink);
            switch (l.HairStyle)
            {
                case 0: B(m, 0, 1.71f, .02f, .28f, .08f, .29f, l.Hair); break;
                case 1: B(m, 0, 1.7f, .03f, .28f, .1f, .3f, l.Hair); B(m, 0, 1.5f, .13f, .27f, .35f, .06f, l.Hair); break;
                case 2: B(m, 0, 1.71f, .02f, .28f, .08f, .29f, l.Hair); B(m, 0, 1.58f, .19f, .08f, .22f, .08f, l.Hair); break;
                case 3: B(m, 0, 1.69f, .04f, .27f, .05f, .26f, l.Hair); break;
                case 4: m.Cylinder(new Vector3(0, 1.73f, 0), .15f, .13f, l.Hair, 8, .8f); break;
                case 5: m.Cylinder(new Vector3(0, 1.73f, 0), .15f, .1f, l.Hair, 8, .9f); B(m, 0, 1.69f, -.15f, .26f, .03f, .16f, l.Hair); break;
                case 6: m.Cylinder(new Vector3(0, 1.74f, 0), .14f, .1f, l.Hair, 8, .9f); m.Cylinder(new Vector3(0, 1.69f, 0), .3f, .03f, l.Hair, 10); break;
                default: m.Ellipsoid(new Vector3(0, 1.72f, .02f), new Vector3(.36f, .26f, .36f), l.Hair); break;
            }
            float width = .62f;
            switch (l.Item)
            {
                case "backpack": B(m, 0, 1.15f, .2f, .4f, .46f, .18f, l.Prop); B(m, 0, 1.28f, .3f, .3f, .1f, .04f, _r.ink); break;
                case "briefcase": B(m, .33f, .62f, 0, .09f, .3f, .42f, l.Prop); B(m, .33f, .8f, 0, .04f, .06f, .14f, _r.ink); break;
                case "coffee": m.Cylinder(new Vector3(.33f, .93f, -.06f), .05f, .13f, White, 8, 1.2f); B(m, .33f, .93f, -.06f, .11f, .04f, .11f, l.Prop); break;
                case "surfboard": m.Ellipsoid(new Vector3(.45f, 1f, 0), new Vector3(.08f, 1.95f, .5f), l.Prop); B(m, .45f, 1f, 0, .09f, 1.6f, .05f, _r.red); width = 1f; break;
                case "stick": B(m, .38f, .44f, -.06f, .04f, .82f, .04f, l.Prop); B(m, .35f, .84f, -.06f, .1f, .04f, .04f, l.Prop); break;
                case "bags": B(m, -.37f, .6f, 0, .24f, .3f, .12f, l.Prop); B(m, .37f, .6f, 0, .22f, .28f, .12f, White); B(m, .37f, .78f, 0, .12f, .06f, .02f, _r.ink); width = .9f; break;
                case "camera": B(m, 0, 1.28f, -.16f, .18f, .12f, .08f, l.Prop); m.Cylinder(new Vector3(0, 1.28f, -.22f), .04f, .06f, Silver, 8, 1, Quaternion.Euler(90, 0, 0)); break;
                case "headphones": B(m, 0, 1.73f, 0, .3f, .05f, .06f, _r.ink); B(m, -.14f, 1.57f, 0, .06f, .12f, .1f, l.Prop); B(m, .14f, 1.57f, 0, .06f, .12f, .1f, l.Prop); break;
                case "skateboard": B(m, .4f, .95f, 0, .05f, .2f, .78f, l.Prop); B(m, .43f, .95f, .25f, .04f, .1f, .08f, _r.ink); B(m, .43f, .95f, -.25f, .04f, .1f, .08f, _r.ink); break;
                case "balloon": m.Beam(new Vector3(.31f, .85f, 0), new Vector3(.42f, 2.1f, 0), .015f, _r.ink); m.Ellipsoid(new Vector3(.42f, 2.36f, 0), new Vector3(.4f, .5f, .4f), l.Prop); break;
                case "delivery": B(m, 0, 1.25f, .29f, .52f, .52f, .44f, l.Prop); B(m, 0, 1.25f, .52f, .3f, .14f, .02f, White); break;
                case "dog":
                    Color fur = l.Prop;
                    B(m, .6f, .3f, -.25f, .22f, .2f, .52f, fur); B(m, .6f, .44f, -.55f, .18f, .18f, .2f, fur); B(m, .6f, .4f, -.66f, .08f, .07f, .06f, _r.ink);
                    for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2) B(m, .6f + x * .07f, .1f, -.25f + z * .18f, .06f, .2f, .06f, fur);
                    B(m, .6f, .4f, .04f, .05f, .05f, .16f, fur);
                    m.Beam(new Vector3(.31f, .82f, 0), new Vector3(.6f, .45f, -.48f), .015f, _r.red);
                    width = 1f; break;
            }
            k.Body.Add(m, Vector3.zero, Quaternion.identity, Vector3.one * l.Scale);
            float h = 1.8f * l.Scale;
            return Save(id, k, new Vector3(width > .7f ? (width - .62f) * .5f : 0f, h / 2, 0), new Vector3(width, h, .45f), r =>
            {
                MakeObstacle(r, ObstacleKind.Person, false);
                var ped = r.AddComponent<Pedestrian>();
                ped.walkSpeed = walk; ped.crosses = crosses; ped.art = r.transform.Find("Art");
            });
        }

        // ---------------------------------------------------------------- punch visuals

        // The rider's punching arm (upper arm, forearm, fist: unit pieces stretched at runtime) and the riding model
        // without the right / left arm, swapped in while the arm swings (PunchArm).
        static void PunchArmKit()
        {
            var root = new GameObject("HZ_PunchArm");
            try
            {
                Transform Piece(string name, ComicMesh mesh)
                {
                    var go = new GameObject(name);
                    go.transform.SetParent(root.transform, false);
                    AddRenderer(go, "HZ_PunchArm_" + name, mesh, true);
                    return go.transform;
                }
                var upper = new ComicMesh { Surface = ComicSurface.Fabric };
                upper.Beam(Vector3.zero, Vector3.forward, .17f, _r.ink);
                var fore = new ComicMesh { Surface = ComicSurface.Fabric };
                fore.Beam(Vector3.zero, Vector3.forward, .15f, _r.ink);
                var fist = new ComicMesh { Surface = ComicSurface.Fabric };
                fist.Ellipsoid(Vector3.zero, new Vector3(.2f, .19f, .22f), _r.cream);
                fist.Box(new Vector3(0, .02f, -.1f), new Vector3(.17f, .1f, .06f), _r.ink);
                var arm = root.AddComponent<PunchArm>();
                arm.upperArm = Piece("UpperArm", upper);
                arm.forearm = Piece("Forearm", fore);
                arm.fist = Piece("Fist", fist);
                arm.armlessRight = SaveMesh("HZ_Rider_NoRightArm", ComicKitGeometry.Rider(_r, 1));
                arm.armlessLeft = SaveMesh("HZ_Rider_NoLeftArm", ComicKitGeometry.Rider(_r, -1));
                string path = Prefabs + "/HZ_PunchArm.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
                if (!ok) throw new InvalidOperationException("Could not save " + path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        // Comic motion lines for a swing (faces -Z; ComicPopup turns it to the camera).
        static GameObject Swish()
        {
            var k = new Kit();
            ComicMesh m = k.Body;
            for (int line = 0; line < 3; line++)
            {
                float r = .55f + line * .22f, w = .07f - line * .012f;
                const int seg = 8;
                for (int i = 0; i < seg; i++)
                {
                    float a0 = Mathf.Lerp(-.2f, 1.3f, i / (float)seg), a1 = Mathf.Lerp(-.2f, 1.3f, (i + 1) / (float)seg);
                    Vector3 o0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0), o1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0);
                    float taper0 = Mathf.Sin(Mathf.PI * i / seg), taper1 = Mathf.Sin(Mathf.PI * (i + 1) / seg);
                    Vector3 a = o0 * (r - w * taper0), b = o0 * (r + w * taper0), c = o1 * (r + w * taper1), d = o1 * (r - w * taper1);
                    m.Quad(b, a, d, c, White, false);
                    m.Quad(b * 1.03f + Vector3.forward * .01f, a * .97f + Vector3.forward * .01f, d * .97f + Vector3.forward * .01f, c * 1.03f + Vector3.forward * .01f, _r.ink, false);
                }
            }
            return Save("FxSwish", k, Vector3.zero, Vector3.zero, r =>
            {
                var popup = r.AddComponent<ComicPopup>(); popup.life = .22f; popup.rise = 0f;
                foreach (MeshRenderer mr in r.GetComponentsInChildren<MeshRenderer>(true)) mr.shadowCastingMode = ShadowCastingMode.Off;
            }, decor: true);
        }

        // Ink burst where a punch lands.
        static GameObject Impact()
        {
            var k = new Kit();
            ComicMesh m = k.Body;
            const int spikes = 12;
            for (int i = 0; i < spikes; i++)
            {
                float a = i * Mathf.PI * 2f / spikes + (i & 1) * .12f, r0 = .35f, r1 = (i & 1) == 0 ? 1.25f : .95f, w = .07f;
                Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0), n = new Vector3(-dir.y, dir.x, 0);
                m.Tri(dir * r1, dir * r0 - n * w, dir * r0 + n * w, _r.ink);
            }
            return Save("FxImpact", k, Vector3.zero, Vector3.zero, r =>
            {
                var popup = r.AddComponent<ComicPopup>(); popup.life = .25f; popup.rise = 0f;
                foreach (MeshRenderer mr in r.GetComponentsInChildren<MeshRenderer>(true)) mr.shadowCastingMode = ShadowCastingMode.Off;
            }, decor: true);
        }

        // The rider thrown off in a crash, standing pose (feet at the pivot, facing +Z like the riding model).
        static void CrashRider()
        {
            var k = new Kit();
            ComicMesh m = k.Body;
            Color pants = new Color(.38f, .42f, .27f);
            for (int s = -1; s <= 1; s += 2)
            {
                B(m, s * .12f, .07f, .03f, .18f, .14f, .34f, _r.cream);
                B(m, s * .12f, .52f, 0, .22f, .8f, .25f, pants);
                B(m, s * .37f, 1.28f, 0, .16f, .56f, .18f, _r.ink);
                m.Ellipsoid(new Vector3(s * .37f, .98f, 0), Vector3.one * .17f, _r.cream);
            }
            B(m, 0, 1.0f, 0, .5f, .17f, .3f, _r.cream);
            B(m, 0, 1.33f, 0, .54f, .6f, .32f, _r.ink);
            m.Cylinder(new Vector3(0, 1.66f, 0), .2f, .13f, _r.red);
            m.Ellipsoid(new Vector3(0, 1.92f, 0), new Vector3(.49f, .49f, .52f), _r.cream);
            B(m, 0, 1.88f, .26f, .28f, .06f, .03f, _r.ink);
            Save("CrashRider", k, Vector3.zero, Vector3.zero, r => r.AddComponent<CrashTumble>(), decor: true, outline: true);
        }

        // The scooter on its own (the riding model is one mesh), front at +Z.
        static void CrashScooter()
        {
            var k = new Kit();
            ComicMesh m = k.Body;
            B(m, 0, .22f, 0, .34f, .12f, 1.2f, _r.ink);
            B(m, 0, .29f, 0, .28f, .04f, 1f, _r.red);
            for (int i = -1; i <= 1; i += 2) m.Cylinder(new Vector3(0, .18f, i * .52f), .18f, .12f, _r.ink, 12, 1, Quaternion.Euler(0, 0, 90));
            m.Beam(new Vector3(0, .3f, .48f), new Vector3(0, 1.22f, .65f), .055f, _r.cream);
            B(m, 0, 1.23f, .65f, .85f, .065f, .065f, _r.ink);
            Save("CrashScooter", k, Vector3.zero, Vector3.zero, r => r.AddComponent<CrashTumble>(), decor: true, outline: true);
        }

        static GameObject Zebra()
        {
            var k = new Kit();
            for (int i = 0; i < 7; i++) B(k.Body, -3.3f + i * 1.1f, .012f, 0, .55f, .024f, 3f, White);
            return Save("Zebra", k, Vector3.zero, Vector3.zero, r => { }, decor: true);
        }

        // Comic burst with pixel-font text, facing -Z; shown by ComicPopup (billboard to the camera).
        static GameObject Popup(string id, string text, Color fill, Color ink, float radius)
        {
            var k = new Kit();
            ComicMesh m = k.Body;
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
            return Save(id, k, Vector3.zero, Vector3.zero, r =>
            {
                var popup = r.AddComponent<ComicPopup>();
                popup.life = .7f; popup.rise = .7f;
                foreach (MeshRenderer mr in r.GetComponentsInChildren<MeshRenderer>(true)) mr.shadowCastingMode = ShadowCastingMode.Off;
            }, decor: true);
        }

        static Gift Present(string id, Color box, Color ribbon)
        {
            var k = new Kit();
            ComicMesh m = k.Body;
            B(m, 0, .45f, 0, .6f, .6f, .6f, box);
            B(m, 0, .45f, 0, .62f, .62f, .12f, ribbon);
            B(m, 0, .45f, 0, .12f, .62f, .62f, ribbon);
            k.Body.Box(new Vector3(-.1f, .8f, 0), new Vector3(.22f, .12f, .1f), ribbon, Quaternion.Euler(0, 0, 30));
            k.Body.Box(new Vector3(.1f, .8f, 0), new Vector3(.22f, .12f, .1f), ribbon, Quaternion.Euler(0, 0, -30));
            GameObject go = Save(id, k, new Vector3(0, .45f, 0), new Vector3(1.2f, 1.2f, 1.2f), r =>
            {
                r.AddComponent<Gift>().points = 10;
                r.transform.Find("Art").gameObject.AddComponent<Spin>().degreesPerSecond = 70f;
            }, true);
            return go.GetComponent<Gift>();
        }
    }
}
