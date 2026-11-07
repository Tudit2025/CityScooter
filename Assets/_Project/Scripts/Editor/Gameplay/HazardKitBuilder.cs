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
    /// WA police (11/8, the teacher's decision for class): "WA POLICE" text, blue/white checks, a police officer, the
    /// police helicopter with its net (target ring, falling net, net on the road) and sniper (red dot, BANG!, tracer,
    /// dust puff). Still no real crest or logo, nothing gory: comic shapes only.
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
        static readonly Color UniformNavy = new Color(.1f, .13f, .24f);
        static readonly Color TrouserNavy = new Color(.07f, .09f, .17f);
        static readonly Color RopeTan = new Color(.88f, .74f, .46f);
        static readonly Color Tracer = new Color(1f, .95f, .55f);
        static readonly Color Dust = new Color(.86f, .8f, .68f);

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
                ["PoliceOfficer"] = PoliceOfficer(),
            };
            PoliceCap(); PoliceHeli(); TargetRing(); SniperDot(); PoliceNet(); NetOnRoad(); NetRope(); SniperTracer(); DustPuff();
            GameObject[] popups = { Popup("FxPow", "POW", _r.yellow, _r.red, 1f), Popup("FxDing", "DING", White, PoliceBlue, .55f),
                Popup("FxBang", "!", _r.yellow, _r.ink, .55f), Popup("FxSorry", "SORRY", White, _r.red, .9f),
                Popup("FxBoing", "BOING", _r.yellow, PoliceBlue, .9f), Popup("FxCrash", "CRASH!", White, _r.red, 1f),
                Popup("FxBonk", "BONK!", White, _r.ink, .8f), Swish(), Impact(), Popup("FxIgnore", "...", White, _r.ink, .5f),
                Popup("FxZoom", "ZOOM!", new Color(.3f, .85f, 1f), _r.ink, .85f),
                Popup("FxOi", "OI!", _r.yellow, PoliceBlue, .7f), Popup("FxNetted", "NETTED!", White, PoliceBlue, 1.15f),
                Popup("FxBangShot", "BANG!", _r.yellow, _r.red, 2.4f), Popup("FxScreech", "SCREECH!", White, _r.ink, 1.05f) };
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
            if (popups.Length != 15) throw new InvalidOperationException("pop-ups");
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
            theme.policeCar = Fx("PoliceCar"); theme.policeOfficer = Fx("PoliceOfficer"); theme.policeCap = Fx("PoliceCap"); theme.policeHeli = Fx("PoliceHeli");
            theme.policeNet = Fx("PoliceNet"); theme.netOnRoad = Fx("NetOnRoad"); theme.netRope = Fx("NetRope");
            theme.targetRing = Fx("TargetRing"); theme.sniperDot = Fx("SniperDot"); theme.sniperTracer = Fx("SniperTracer");
            theme.oiPopup = Fx("FxOi"); theme.nettedPopup = Fx("FxNetted"); theme.bangPopup = Fx("FxBangShot"); theme.dustPopup = Fx("FxDust");
            theme.screechPopup = Fx("FxScreech");
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
                E("PoliceOfficer", -1.05f, 31.5f), // at the driver's door (road side), facing the traffic
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
                E("PoliceCar", -2.6f, 20), E("PoliceOfficer", -4.4f, 21f), E("CarOncomingBlue", 2f, 75), // officer on the kerb
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
            /// <summary>Parts that get an ink outline (most parts are lights and have none).</summary>
            public readonly HashSet<string> Inked = new HashSet<string>();
            /// <summary>Local position of a part's own transform (rotors spin around it); its mesh is built around zero.</summary>
            public readonly Dictionary<string, Vector3> Pivots = new Dictionary<string, Vector3>();
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
                    if (kit.Pivots.TryGetValue(part.Key, out Vector3 pivot)) go.transform.localPosition = pivot;
                    AddRenderer(go, "HZ_" + id + "_" + part.Key, part.Value, kit.Inked.Contains(part.Key));
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

        // Pixel text on a surface facing +Y (a car roof), read from behind: letters run along +X, their tops point to +Z.
        static void TextUp(ComicMesh m, string text, float cx, float z, float y, float px, Color c)
        {
            var t = new ComicMesh();
            Text(t, text, 0f, 0f, 0f, px, c);
            m.Add(t, new Vector3(cx, y, z), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
        }

        // Pixel text on a side facing +X (side 1) or -X (side -1), reading the right way round from that side.
        static void TextSide(ComicMesh m, string text, int side, float x, float cy, float cz, float px, Color c)
        {
            var t = new ComicMesh();
            Text(t, text, 0f, 0f, 0f, px, c);
            m.Add(t, new Vector3(x, cy, cz), Quaternion.Euler(0f, side > 0 ? -90f : 90f, 0f), Vector3.one);
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
                // WA POLICE (the teacher's call for class): two lines between the tail lights, and on the roof for the chase camera.
                Text(m, "WA", 0, .79f, -2.115f, .03f, PoliceBlue);
                Text(m, "POLICE", 0, .6f, -2.115f, .03f, PoliceBlue);
                TextUp(m, "WA", 0f, -.72f, 1.465f, .09f, PoliceBlue);       // big: the chase camera sees the roof at a shallow angle
                TextUp(m, "POLICE", 0f, -1.17f, 1.465f, .06f, PoliceBlue);
                B(m, 0, 1.5f, -.25f, 1.3f, .08f, .36f, _r.ink);
                B(m, -.33f, 1.6f, -.25f, .58f, .13f, .3f, new Color(.35f, .05f, .07f));
                B(m, .33f, 1.6f, -.25f, .58f, .13f, .3f, new Color(.05f, .08f, .3f));
                B(k.Part("LightRed"), -.33f, 1.6f, -.25f, .62f, .16f, .34f, new Color(1f, .12f, .12f));
                B(k.Part("LightBlue"), .33f, 1.6f, -.25f, .62f, .16f, .34f, new Color(.2f, .45f, 1f));
            }
            return Save(id, k, new Vector3(0, .85f, 0), new Vector3(1.9f, 1.7f, 4.3f), r =>
            {
                MakeObstacle(r, ObstacleKind.Vehicle, false);
                if (police) { Blink(r, "LightRed", "LightBlue", 3f); r.GetComponent<Obstacle>().police = true; }
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
                // Up to 14 people around: no shadow pass for them (the web build's draw-call budget).
                foreach (MeshRenderer mr in r.GetComponentsInChildren<MeshRenderer>(true)) mr.shadowCastingMode = ShadowCastingMode.Off;
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

        // ---------------------------------------------------------------- WA police

        // Same boxy comic proportions as the street people (facing -Z, feet at the pivot): dark navy uniform, duty belt,
        // a plain silver badge (no real crest) and the cap with the blue/white checked band. The cap is its own piece:
        // a punch without cartoon mode knocks it off (PoliceOfficer), the officer stays.
        static GameObject PoliceOfficer()
        {
            var k = new Kit();
            ComicMesh m = k.Body;
            for (int s = -1; s <= 1; s += 2)
            {
                B(m, s * .11f, .05f, -.03f, .16f, .1f, .28f, _r.ink);
                B(m, s * .11f, .48f, 0, .17f, .82f, .2f, TrouserNavy);
                B(m, s * .31f, 1.12f, 0, .13f, .5f, .16f, UniformNavy);
                B(m, s * .31f, .82f, 0, .11f, .12f, .12f, SkinMedium);
                B(m, s * .2f, 1.41f, 0, .15f, .03f, .28f, PoliceBlue); // epaulettes
            }
            B(m, 0, 1.12f, 0, .48f, .6f, .27f, UniformNavy);
            B(m, 0, .87f, 0, .52f, .09f, .3f, _r.ink);                     // duty belt
            B(m, 0, .87f, -.155f, .1f, .06f, .02f, Silver);                // buckle
            B(m, .21f, .82f, -.09f, .1f, .14f, .12f, _r.ink);               // pouch
            B(m, -.2f, .82f, -.08f, .08f, .16f, .12f, _r.ink);              // radio
            B(m, -.13f, 1.27f, -.14f, .09f, .1f, .02f, Silver);             // plain badge
            B(m, .12f, 1.3f, -.14f, .12f, .03f, .02f, White);               // name bar
            B(m, 0, 1.55f, 0, .25f, .27f, .25f, SkinMedium);
            B(m, -.06f, 1.58f, -.13f, .04f, .05f, .02f, _r.ink); B(m, .06f, 1.58f, -.13f, .04f, .05f, .02f, _r.ink);
            B(m, 0, 1.49f, -.13f, .1f, .02f, .02f, _r.ink);                // a stern little mouth
            CapShape(k.Part("Cap"), new Vector3(0f, 1.73f, 0f));
            k.Inked.Add("Cap");
            return Save("PoliceOfficer", k, new Vector3(0, .9f, 0), new Vector3(.62f, 1.8f, .45f), r =>
            {
                MakeObstacle(r, ObstacleKind.Person, false);
                r.GetComponent<Obstacle>().police = true;
                var ped = r.AddComponent<Pedestrian>();
                ped.walkSpeed = 0f; ped.crosses = false; ped.art = r.transform.Find("Art");
                r.AddComponent<Track.PoliceOfficer>().cap = r.transform.Find("Art/Cap").gameObject;
            });
        }

        // Navy crown, a band of blue and white checks (two rows), black peak to the front (-Z). c = bottom of the band.
        static void CapShape(ComicMesh m, Vector3 c)
        {
            m.Cylinder(c + new Vector3(0f, .1f, .01f), .152f, .1f, UniformNavy, 10, 1.18f);
            const int n = 10;
            const float r = .158f, h = .04f;
            for (int i = 0; i < n; i++)
                for (int row = 0; row < 2; row++)
                {
                    float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n, y0 = c.y + row * h, y1 = y0 + h;
                    Vector3 p = c + new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r), q = c + new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r);
                    p.y = q.y = y0;
                    Color col = ((i + row) & 1) == 0 ? PoliceBlue : White;
                    m.Quad(q, p, p + Vector3.up * h, q + Vector3.up * h, col, false);
                }
            B(m, c.x, c.y + .005f, c.z - .16f, .27f, .025f, .15f, _r.ink);
        }

        // The cap that flies off (spawned at the officer's cap, launched with Knockout).
        static void PoliceCap()
        {
            var k = new Kit();
            CapShape(k.Body, new Vector3(0f, -.05f, 0f));
            Save("PoliceCap", k, Vector3.zero, Vector3.zero, r => { }, decor: true, outline: true);
        }

        // The police helicopter, nose at +Z: white cabin, blue belly band, blue/white checks and WA POLICE on both
        // sides, spinning rotors (Rotor, TailRotor: their own pivots), red/blue lights, a searchlight. Decoration only.
        static void PoliceHeli()
        {
            var k = new Kit();
            ComicMesh m = k.Body;
            B(m, 0, 1.45f, .1f, 1.7f, 1.5f, 3.1f, White);                         // cabin
            B(m, 0, 1.25f, 1.85f, 1.5f, 1.1f, .5f, White);                        // nose
            B(m, 0, 1.55f, 2.11f, 1.3f, .55f, .03f, _r.glass);                    // windscreen
            k.Body.Box(new Vector3(0, 1.95f, 1.75f), new Vector3(1.4f, .03f, .6f), _r.glass, Quaternion.Euler(-28f, 0f, 0f));
            B(m, 0, .82f, .2f, 1.72f, .26f, 3.4f, PoliceBlue);                    // belly band
            const float cell = .24f;
            for (int s = -1; s <= 1; s += 2)
            {
                B(m, s * .855f, 1.95f, 1.45f, .03f, .3f, .7f, _r.glass);            // side windows
                for (int i = 0; i < 12; i++)
                    for (int row = 0; row < 2; row++)
                    {
                        float z = -1.35f + i * cell, y = .97f + row * cell, x = s * .858f;
                        Color c = ((i + row) & 1) == 0 ? PoliceBlue : White;
                        if (s > 0) m.Quad(new Vector3(x, y, z), new Vector3(x, y, z + cell), new Vector3(x, y + cell, z + cell), new Vector3(x, y + cell, z), c, false);
                        else m.Quad(new Vector3(x, y, z + cell), new Vector3(x, y, z), new Vector3(x, y + cell, z), new Vector3(x, y + cell, z + cell), c, false);
                    }
                TextSide(m, "WA POLICE", s, s * .862f, 1.66f, -.1f, .066f, PoliceBlue);
                // skids and struts (the sniper's rifle pokes out of the door on the +X side, which faces the camera)
                m.Beam(new Vector3(s * .85f, .12f, -1.2f), new Vector3(s * .85f, .12f, 1.7f), .09f, _r.ink);
                m.Beam(new Vector3(s * .62f, .72f, -.7f), new Vector3(s * .85f, .14f, -.7f), .07f, _r.ink);
                m.Beam(new Vector3(s * .62f, .72f, .9f), new Vector3(s * .85f, .14f, .9f), .07f, _r.ink);
            }
            m.Beam(new Vector3(0, 1.75f, -1.3f), new Vector3(0, 2.05f, -5.4f), .42f, White);   // tail boom
            m.Beam(new Vector3(0, 1.58f, -1.3f), new Vector3(0, 1.86f, -5.2f), .12f, PoliceBlue);
            B(m, 0, 2.55f, -5.25f, .1f, 1f, .7f, PoliceBlue);                     // fin
            B(m, 0, 2.05f, -4.85f, 1.3f, .07f, .42f, White);                      // stabiliser
            m.Cylinder(new Vector3(0, 2.33f, .15f), .14f, .36f, _r.ink, 8);       // mast
            B(m, 0, 2.27f, .1f, .9f, .22f, 1.3f, White);                          // engine cowling
            m.Cylinder(new Vector3(0, .7f, 1.7f), .16f, .1f, LampOn, 8);          // searchlight
            B(m, -.5f, .66f, 1.2f, .28f, .1f, .28f, new Color(.35f, .05f, .07f));
            B(m, .5f, .66f, 1.2f, .28f, .1f, .28f, new Color(.05f, .08f, .3f));
            B(k.Part("LightRed"), -.5f, .66f, 1.2f, .32f, .13f, .32f, new Color(1f, .12f, .12f));
            B(k.Part("LightBlue"), .5f, .66f, 1.2f, .32f, .13f, .32f, new Color(.2f, .45f, 1f));
            ComicMesh rotor = k.Part("Rotor");
            B(rotor, 0, 0, 0, 7.4f, .06f, .3f, Steel);
            B(rotor, 0, 0, 0, .3f, .06f, 7.4f, Steel);
            rotor.Cylinder(Vector3.zero, .2f, .14f, _r.ink, 8);
            k.Pivots["Rotor"] = new Vector3(0f, 2.55f, .15f);
            ComicMesh tail = k.Part("TailRotor");
            B(tail, 0, 0, 0, .04f, 1.2f, .14f, Steel);
            B(tail, 0, 0, 0, .04f, .14f, 1.2f, Steel);
            k.Pivots["TailRotor"] = new Vector3(.12f, 2.35f, -5.3f);
            m.Beam(new Vector3(.62f, 1.24f, .78f), new Vector3(1.5f, 1.1f, 1.05f), .08f, _r.ink);   // rifle barrel
            B(m, .78f, 1.33f, .83f, .3f, .08f, .08f, _r.ink);                                       // scope
            Save("PoliceHeli", k, Vector3.zero, Vector3.zero, r =>
            {
                Blink(r, "LightRed", "LightBlue", 2.5f);
                var muzzle = new GameObject("Muzzle"); // where the shot's BANG! and streak start
                muzzle.transform.SetParent(r.transform.Find("Art"), false);
                muzzle.transform.localPosition = new Vector3(1.55f, 1.09f, 1.07f);
            }, decor: true, outline: true);
        }

        // A flat ring on the road (faces +Y).
        static void Ring(ComicMesh m, float radius, float width, float y, Color c, int segments = 28)
        {
            float ri = radius - width * .5f, ro = radius + width * .5f;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)), up = Vector3.up * y;
                m.Quad(d0 * ro + up, d0 * ri + up, d1 * ri + up, d1 * ro + up, c, false);
            }
        }

        // A flat disc on the road (faces +Y).
        static void Disc(ComicMesh m, float radius, float y, Color c, int segments = 16)
        {
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                m.Tri(new Vector3(0, y, 0), new Vector3(Mathf.Cos(a1) * radius, y, Mathf.Sin(a1) * radius), new Vector3(Mathf.Cos(a0) * radius, y, Mathf.Sin(a0) * radius), c);
            }
        }

        // Where a net will land: red and white target, crosshair ticks; Art/Pulse closes in until it lands.
        static void TargetRing()
        {
            var k = new Kit();
            float r = 1.3f;
            Ring(k.Body, r, .2f, .01f, _r.ink);
            Ring(k.Body, r, .13f, .015f, _r.red);
            Ring(k.Body, r - .22f, .07f, .015f, White);
            Ring(k.Body, .36f, .1f, .015f, _r.red);
            Disc(k.Body, .1f, .015f, _r.red, 10);
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward;
                k.Body.Box(d * (r - .55f) + Vector3.up * .015f, new Vector3(.07f, .01f, .4f), _r.red, Quaternion.LookRotation(d));
            }
            Ring(k.Part("Pulse"), r, .09f, .03f, _r.yellow);
            Save("TargetRing", k, Vector3.zero, Vector3.zero, r2 => { }, decor: true);
        }

        // The sniper's red dot: a dark ring that is always there, the red dot and crosshair blink (Art/Blink) when it locks.
        static void SniperDot()
        {
            var k = new Kit();
            Ring(k.Body, .7f, .08f, .01f, _r.ink);
            ComicMesh b = k.Part("Blink");
            Disc(b, .2f, .02f, _r.red, 12);
            Ring(b, .58f, .1f, .02f, _r.red);
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 90f + 45f, 0f) * Vector3.forward;
                b.Box(d * .78f + Vector3.up * .02f, new Vector3(.07f, .01f, .36f), _r.red, Quaternion.LookRotation(d));
            }
            Save("SniperDot", k, Vector3.zero, Vector3.zero, r => { }, decor: true);
        }

        // The net, built around zero: an ink grid (on a dome, or lying almost flat), a rope rim and small weights round
        // the edge. The dome is what falls and what lies over a caught rider; its top is at height + .04.
        static void NetShape(ComicMesh m, float radius, float height, bool onRoad)
        {
            const int lines = 9;
            float step = radius * 2f / (lines + 1);
            float H(float x, float z)
            {
                if (onRoad) return .03f + .02f * Mathf.Sin(x * 3.1f + 1f) * Mathf.Sin(z * 2.7f); // lying on the road, a little lumpy
                return height * (1f - Mathf.Clamp01((x * x + z * z) / (radius * radius))) + .02f;
            }
            for (int dir = 0; dir < 2; dir++)
                for (int i = 1; i <= lines; i++)
                {
                    float u = -radius + i * step, half = Mathf.Sqrt(Mathf.Max(0f, radius * radius - u * u));
                    int segs = Mathf.Max(2, Mathf.CeilToInt(half * 2f / .3f));
                    for (int s = 0; s < segs; s++)
                    {
                        float v0 = -half + 2f * half * s / segs, v1 = -half + 2f * half * (s + 1) / segs;
                        Vector3 a = dir == 0 ? new Vector3(u, 0f, v0) : new Vector3(v0, 0f, u), b = dir == 0 ? new Vector3(u, 0f, v1) : new Vector3(v1, 0f, u);
                        a.y = H(a.x, a.z); b.y = H(b.x, b.z);
                        m.Beam(a, b, .045f, _r.ink);
                    }
                }
            const int rim = 20;
            for (int i = 0; i < rim; i++)
            {
                float a0 = i * Mathf.PI * 2f / rim, a1 = (i + 1) * Mathf.PI * 2f / rim;
                var p = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                var q = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                p.y = H(p.x, p.z) + .02f; q.y = H(q.x, q.z) + .02f;
                m.Beam(p, q, .08f, RopeTan);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = (i + .5f) * Mathf.PI * 2f / 8;
                var w = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                w.y = H(w.x, w.z) + .06f;
                m.Ellipsoid(w, Vector3.one * .17f, Steel);
            }
            if (!onRoad) m.Cylinder(new Vector3(0f, height + .04f, 0f), .09f, .08f, RopeTan, 8); // where the rope is tied
        }

        // What the helicopter drops (theme.policeNet): a dome-shaped net that also lies over a caught rider.
        static void PoliceNet()
        {
            var k = new Kit();
            NetShape(k.Body, 1.45f, 1.9f, false);
            Save("PoliceNet", k, Vector3.zero, Vector3.zero, r => { }, decor: true, outline: true);
        }

        // A net that missed, lying on the road (decoration, no collider).
        static void NetOnRoad()
        {
            var k = new Kit();
            NetShape(k.Body, 1.45f, 0f, true);
            Save("NetOnRoad", k, Vector3.zero, Vector3.zero, r => { }, decor: true, outline: true);
        }

        // The rope from the helicopter to a falling net: one metre along +Z, stretched at runtime.
        static void NetRope()
        {
            var k = new Kit();
            k.Body.Beam(Vector3.zero, Vector3.forward, .06f, RopeTan);
            Save("NetRope", k, Vector3.zero, Vector3.zero, r => { }, decor: true);
        }

        // The sniper shot's bright streak: one metre along +Z, stretched from the helicopter to the red dot for a moment.
        static void SniperTracer()
        {
            var k = new Kit();
            k.Body.Beam(Vector3.zero, Vector3.forward, .13f, Tracer);
            Save("SniperTracer", k, Vector3.zero, Vector3.zero, r =>
            {
                foreach (MeshRenderer mr in r.GetComponentsInChildren<MeshRenderer>(true)) mr.shadowCastingMode = ShadowCastingMode.Off;
            }, decor: true);
        }

        // A comic puff of dust where a missed shot hits the road (billboard pop-up, faces -Z).
        static void DustPuff()
        {
            var k = new Kit();
            ComicMesh m = k.Body;
            void Circle(float cx, float cy, float r, float z, Color c)
            {
                const int segs = 14;
                for (int i = 0; i < segs; i++)
                {
                    float a0 = i * Mathf.PI * 2f / segs, a1 = (i + 1) * Mathf.PI * 2f / segs;
                    m.Tri(new Vector3(cx, cy, z), new Vector3(cx + Mathf.Cos(a1) * r, cy + Mathf.Sin(a1) * r, z), new Vector3(cx + Mathf.Cos(a0) * r, cy + Mathf.Sin(a0) * r, z), c);
                }
            }
            float[,] puffs = { { 0f, .27f, .63f }, { -.68f, 0f, .48f }, { .69f, .03f, .51f }, { -.33f, .63f, .42f }, { .36f, .66f, .39f } }; // about 2 m across
            for (int i = 0; i < puffs.GetLength(0); i++) Circle(puffs[i, 0], puffs[i, 1], puffs[i, 2] + .05f, .02f, _r.ink);
            for (int i = 0; i < puffs.GetLength(0); i++) Circle(puffs[i, 0], puffs[i, 1], puffs[i, 2], 0f, Dust);
            Save("FxDust", k, Vector3.zero, Vector3.zero, r =>
            {
                var popup = r.AddComponent<ComicPopup>(); popup.life = .5f; popup.rise = .45f;
                foreach (MeshRenderer mr in r.GetComponentsInChildren<MeshRenderer>(true)) mr.shadowCastingMode = ShadowCastingMode.Off;
            }, decor: true);
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
