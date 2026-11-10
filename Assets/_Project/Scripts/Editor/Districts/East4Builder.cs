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
    /// East line, art agent 4 (v0.5, docs/v0.5-east-line.md): SUCCESS HILL (order 300), GUILDFORD (310) and
    /// EAST GUILDFORD (320) on the Midland line. The ride goes east (+Z); the railway (ballast, two tracks, overhead-wire
    /// gantries, platforms, trains: scenery, no colliders) is always on the right (+X, the south side, as on James St
    /// and Terrace Rd), the places are on the left.
    /// SUCCESS HILL: the shared path past Success Hill station (platform shelter, footbridge with its long ramp and
    /// mural), Federation houses, Success Hill Reserve (river red gums, a picnic shelter, the stone circle and the
    /// interpretive sign: an important Noongar place, shown quietly), the high clay bank over the Swan with the floating
    /// fishing platform, a timber boardwalk across a creek mouth (kicker + SPLASH gap) and the bridge over the river.
    /// GUILDFORD: James St (cars): the Guildford Hotel with its belvedere opposite the heritage station, the antique
    /// shops under post verandahs, the Rose and Crown, Stirling Square with St Matthew's and the pines, the Post
    /// Office clock tower, jacarandas in bloom, the old colonial gaol and the first Swan Valley vines.
    /// EAST GUILDFORD: white stables and paddocks, East Guildford station (the path goes up the platform ramp: a jump),
    /// Guildford Grammar School: red-brick Federation school buildings, the ovals and, as the hero, the Chapel of St
    /// Mary and St George (Donnybrook stone, twin octagonal turrets, great west window, buttressed bays).
    ///   Menu: CityScooter/Districts/Build East 4
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.East4Builder.BuildBatch
    ///   Batch with the props atlases (needs graphics): ...East4Builder.BuildAndRenderAtlasBatch
    /// The code is split: East4Kit.cs (shared parts), East4SuccessHill.cs, East4Guildford.cs, East4EastGuildford.cs.
    /// </summary>
    public static partial class East4Builder
    {
        public const string SuccessId = "successhill", GuildfordId = "guildford", EastGuildfordId = "eastguildford";
        public static readonly string[] SuccessChunks = { "Station", "Reserve", "Lookout", "Boardwalk", "RiverBridge" };
        public static readonly string[] GuildfordChunks = { "Station", "JamesSt", "RoseAndCrown", "StirlingSquare", "PostOffice" };
        public static readonly string[] EastGuildfordChunks = { "Stables", "Station", "School", "Chapel", "Ovals" };

        static string Tag(string id) => id == SuccessId ? "Successhill" : id == GuildfordId ? "Guildford" : "Eastguildford";
        public static string PrefabFolder(string id) => ComicCityBuilder.Prefabs + "/Districts/" + Tag(id);
        public static string MeshFolder(string id) => ComicCityBuilder.Root + "/Districts/" + Tag(id);
        public static string PatternFolder(string id) => CbdBuilder.DataFolder + "/" + Tag(id);
        public static string DistrictPath(string id) => CbdBuilder.DataFolder + "/District_" + Tag(id) + ".asset";
        public static string ChunkPath(string id, string variant) => PrefabFolder(id) + "/CHN_" + Tag(id) + "_" + variant + ".prefab";
        static string Pf(string id, string name) => PrefabFolder(id) + "/" + name + ".prefab";

        /// <summary>The river plain is nearly flat: metres of drop per 40 m chunk.</summary>
        public const float SuccessDrop = -1f, GuildfordDrop = -1.2f, EastGuildfordDrop = -1.1f;
        // The boardwalk jump (Success Hill), the goods platform (Guildford) and the station ramp (East Guildford).
        public const float BoardStart = 3f, BoardRampStart = 10f, BoardRise = 6f, BoardHeight = 1.4f, BoardTop = 3f, BoardGap = 6f;

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static float F(System.Random r, float a, float b) => Mathf.Lerp(a, b, (float)r.NextDouble());

        [MenuItem("CityScooter/Districts/Build East 4")]
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
            BuildSuccessHill(LoadTheme(), atlas);
            BuildGuildford(LoadTheme(), atlas);
            BuildEastGuildford(LoadTheme(), atlas);
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

        static GameObject Cyclist() => AssetDatabase.LoadAssetAtPath<GameObject>(ComicCityBuilder.Prefabs + "/Districts/MountsBay/PED_MountsBay_Cyclist.prefab");

        static ComicMesh Make(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }

        /// <summary>The fields the art builder owns. The system's fields (gateway, ambience, ambienceVolume, skyYaw,
        /// skySize, skyHeight, skyline, line) are never written, except placeholders when the asset is first created
        /// (far from the city: the CBD skyline behind and sunk away).</summary>
        static District Fill(string id, string name, int order, string[] chunkIds)
        {
            string path = DistrictPath(id);
            bool created = AssetDatabase.LoadAssetAtPath<District>(path) == null;
            District d = K.LoadOrCreate<District>(path);
            if (created) { d.skyYaw = 180f; d.skySize = .6f; d.skyHeight = -12f; d.skyline = 0f; }
            d.id = id; d.displayName = name; d.order = order;
            // Each chunk is a place you pass once: the length lays the entry, every chunk once and the exit.
            d.length = new Vector2(40f * chunkIds.Length + 50f, 40f * chunkIds.Length + 75f);
            d.chunks = chunkIds.Select(c => AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(id, c)).GetComponent<RoadChunk>()).ToArray();
            d.orderedChunks = true;
            d.entry = AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(id, "Entry")).GetComponent<RoadChunk>();
            d.exit = AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(id, "Exit")).GetComponent<RoadChunk>();
            d.allowBoulevard = false;
            d.gifts = Array.Empty<Gift>();
            d.blendMeters = 150f;
            return d;
        }

        // An animal walker (kind Person, animal, never punchable). Facing -Z, feet at the pivot.
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

        static Bounds KeepOutBox(CqChunkDraft d, int s, float z0, float z1)
        {
            float yTop = d.Y(z0) + 5f, yBottom = d.Y(z1) - 2f;
            return new Bounds(new Vector3(s * (K.Half + K.Side * .5f + .3f), (yTop + yBottom) * .5f, (z0 + z1) * .5f), new Vector3(K.Side + .6f, yTop - yBottom, z1 - z0));
        }

        // ================================================================ parts shared by the three districts

        /// <summary>The railway beside a whole chunk: corridor, a gantry in the middle, the wires, what is behind.</summary>
        static void Railway(CqChunkDraft d, int seed, bool roofs = true, float z0 = 0f, float z1 = -1f, bool nearFence = true)
        {
            if (z1 < 0f) z1 = d.Length;
            E4.RailCorridor(d, z0, z1, nearFence);
            if (z0 <= 20f && z1 >= 20f) E4.Gantry(d, 20f);
            E4.Wires(d, z0, z1);
            E4.RailBackdrop(d, z0, z1, seed, roofs);
        }

        /// <summary>The calm outer end of an entry / exit chunk on the rail side: a timber noise wall with a hedge in
        /// front and big gums whose crowns hide where the tracks and the wires start.</summary>
        static void CalmRail(CqChunkDraft d, float z0, float z1, int seed)
        {
            E4.Ground(d, 1, z0, z1, 0f, 70f, E4.LawnDry, ComicSurface.Foliage);
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Wood;
            K.SideBox(m, 1, z0, z1, -1f, K.Kerb + 2.6f, 1.5f, 1.72f, Color.Lerp(E4.Timber, E4.Cream, .35f));
            K.SideBox(m, 1, z0, z1, K.Kerb + 2.6f, K.Kerb + 2.72f, 1.42f, 1.8f, E4.TimberDark);
            E4.Hedge(m, 1, z0 + .3f, z1 - .3f, .3f, 1.4f, 1.3f, K.Hex("4E7F3A"));
            var r = new System.Random(seed * 31 + 7);
            float mid = (z0 + z1) * .5f;
            E4.Gum(d.Props, V(K.Edge + 5.5f, d.Ground(mid - 3.5f), mid - 3.5f), F(r, 13f, 15f), seed + 1);
            E4.Gum(d.Props, V(K.Edge + 9.5f, d.Ground(mid + 3f), mid + 3f), F(r, 14f, 17f), seed + 2);
            E4.Gum(d.Deep, V(K.Edge + 15f, d.Ground(mid), mid), F(r, 13f, 16f), seed + 3, true);
            E4.Gum(d.Deep, V(K.Edge + 22f, d.Ground(mid - 4f), mid - 4f), F(r, 12f, 15f), seed + 4, true);
            d.Walls.Add(new Bounds(new Vector3(K.Edge + 7f, d.Y(mid) + 1f, mid), new Vector3(13.5f, 4f, z1 - z0)));
        }

        /// <summary>The calm outer end on the left: a low limestone wall, a clipped hedge, lawn and two small trees.</summary>
        static void CalmLeft(CqChunkDraft d, float z0, float z1, int seed, bool jacaranda = false)
        {
            E4.Ground(d, -1, z0, z1, 0f, 70f, E4.Lawn, ComicSurface.Foliage);
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Concrete;
            K.SideBox(m, -1, z0, z1, -1f, K.Kerb + .55f, .05f, .55f, K.Hex("E2D2A6"));
            K.SideBox(m, -1, z0, z1, K.Kerb + .55f, K.Kerb + .65f, 0f, .6f, K.Hex("F0E6CA"));
            E4.Hedge(m, -1, z0 + .4f, z1 - .4f, .8f, 1.9f, 1.3f, K.Hex("4E7F3A"));
            var r = new System.Random(seed * 31 + 3);
            for (float z = z0 + 3.5f; z < z1 - 1f; z += 7f)
            {
                Vector3 p = V(-(K.Edge + F(r, 4.5f, 7f)), d.Ground(z), z);
                if (jacaranda) E4.Jacaranda(d.Props, p, F(r, 6.5f, 8f), seed + (int)z);
                else E4.Gum(d.Props, p, F(r, 9f, 12f), seed + (int)z);
            }
            for (float z = z0 + 2f; z < z1; z += 9f) E4.Gum(d.Deep, V(-(K.Edge + F(r, 14f, 24f)), d.Ground(z), z), F(r, 12f, 16f), seed + 50 + (int)z, true);
            d.Walls.Add(new Bounds(new Vector3(-(K.Edge + 7f), d.Y((z0 + z1) * .5f) + 1f, (z0 + z1) * .5f), new Vector3(13.5f, 4f, z1 - z0)));
        }

        /// <summary>A row of cheap trees in the background on side s (no outline), x metres beyond the sidewalk edge.</summary>
        static void BackTrees(CqChunkDraft d, int s, float z0, float z1, float x0, float x1, int seed, float step = 9f, float hMin = 12f, float hMax = 17f)
        {
            var r = new System.Random(seed * 71 + 5);
            for (float z = z0 + F(r, 0f, step * .5f); z < z1; z += step * F(r, .75f, 1.25f))
                E4.Gum(d.Deep, V(s * (K.Edge + F(r, x0, x1)), d.Ground(z) - .2f, z), F(r, hMin, hMax), seed + (int)(z * 7f), true);
        }

        /// <summary>
        /// A Federation house (about 1900-1915, the houses of Success Hill, Guildford and East Guildford): tuck-pointed
        /// red brick on a limestone plinth, a projecting front room with a bay window under a half-timbered gable, a
        /// verandah with turned posts, lace brackets and a timber floor beside it, the front door with a fanlight, a
        /// hipped corrugated iron roof, tall brick chimneys with pots.
        /// </summary>
        static void Bungalow(ComicMesh f, CqLot l, Color wall, Color roof, int seed, bool timberWalls = false)
        {
            E4Plane p = E4Plane.Lot(l);
            float W = l.W, D = l.Depth, room = W * .42f, proj = 1.7f, eave = 3.7f;
            bool flip = seed % 2 == 1;
            float U(float u) => flip ? W - u : u;
            void B(float u0, float u1, float v0, float v1, float o0, float o1, Color c) => E4.Box(f, p, Mathf.Min(U(u0), U(u1)), Mathf.Max(U(u0), U(u1)), v0, v1, o0, o1, c);
            Color trim = E4.White, dark = Color.Lerp(wall, K.R.ink, .18f);
            f.Surface = ComicSurface.Concrete;
            B(0, W, -2.4f, .45f, .06f, -D, K.Hex("DCCB9E"));
            B(0, room, -2.4f, .45f, proj + .06f, 0f, K.Hex("DCCB9E"));
            f.Surface = timberWalls ? ComicSurface.Wood : ComicSurface.Brick;
            B(0, W, .45f, eave, 0f, -D, wall);
            B(0, room, .45f, eave, proj, 0f, wall);
            // quoins / corner boards on the projecting room
            f.Surface = ComicSurface.Plaster;
            B(-.04f, .22f, .45f, eave, proj + .04f, proj - .2f, trim);
            B(room - .22f, room + .04f, .45f, eave, proj + .04f, proj - .2f, trim);
            // the bay window of the front room
            E4.Window(f, p, U(room * .5f), room * .52f, 1.25f, 2.95f, proj, trim, E4.Glass, 3, 2);
            f.Surface = ComicSurface.Metal;
            B(room * .5f - room * .36f, room * .5f + room * .36f, 3.05f, 3.2f, proj + .45f, proj, E4.IronDark);
            // verandah side: door with fanlight, a tall window, timber floor, posts, lace
            float v0 = room + .1f, v1 = W;
            E4.Door(f, p, U(v0 + 1.1f), 1.05f, .55f, 3.15f, 0f, trim, E4.HeritageGreen, E4.Glass);
            E4.Window(f, p, U((v0 + 2.2f + v1) * .5f), 1.25f, 1.15f, 3.0f, 0f, trim, E4.Glass, 2, 2);
            f.Surface = ComicSurface.Wood;
            B(v0 - .1f, v1 + .1f, .28f, .46f, proj + .25f, 0f, E4.Timber);
            E4.Verandah(f, p.Move(Mathf.Min(U(v0), U(v1)), 0, 0), 0f, v1 - v0, 3.05f, 3.65f, proj + .2f, 3, roof, trim, trim, true, .45f);
            // steps
            f.Surface = ComicSurface.Concrete;
            B(v0 + .5f, v0 + 1.7f, -.2f, .22f, proj + .8f, proj + .2f, K.Hex("CFC5AE"));
            // roofs: ink eaves, the main hip, the front gable with battens and a vent
            f.Surface = ComicSurface.PaintedMetal;
            B(-.35f, W + .35f, eave - .05f, eave + .1f, .4f, -D - .35f, K.R.ink);
            B(-.35f, room + .35f, eave - .05f, eave + .1f, proj + .4f, 0f, K.R.ink);
            f.Surface = ComicSurface.Metal;
            E4.RidgeRoof(f, p, -.4f, W + .4f, eave + .1f, eave + .1f + D * .3f, .45f, -D - .4f, roof, D * .42f);
            float g0 = flip ? W - room - .4f : -.4f, g1 = flip ? W + .4f : room + .4f, gh = eave + .1f + (room + .8f) * .42f;
            E4.GableRoof(f, p, g0, g1, eave + .1f, gh, proj + .45f, -D * .4f, roof, E4.Cream);
            f.Surface = ComicSurface.Wood;
            float gc = (g0 + g1) * .5f;
            E4.Bar(f, p, g0 + .05f, eave + .16f, gc, gh + .06f, proj + .46f, .16f, .1f, trim);
            E4.Bar(f, p, g1 - .05f, eave + .16f, gc, gh + .06f, proj + .46f, .16f, .1f, trim);
            E4.Box(f, p, gc - .06f, gc + .06f, eave + .2f, gh - .2f, proj + .46f, proj + .52f, dark);
            E4.Box(f, p, gc - .9f, gc - .8f, eave + .2f, gh - .9f, proj + .46f, proj + .52f, dark);
            E4.Box(f, p, gc + .8f, gc + .9f, eave + .2f, gh - .9f, proj + .46f, proj + .52f, dark);
            E4.Box(f, p, gc - .05f, gc + .05f, gh, gh + .7f, proj + .5f, proj + .4f, trim);
            E4.Chimney(f, p.P(U(W * .78f), eave + D * .1f, -D * .3f), 2.2f, timberWalls ? E4.Brick : wall);
            E4.Chimney(f, p.P(U(W * .3f), eave + D * .14f, -D * .62f), 2f, timberWalls ? E4.Brick : wall);
        }

        /// <summary>Where the front door of Bungalow(l, seed) is along z (for the garden path and the gate).</summary>
        static float BungalowDoorZ(CqLot l, int seed)
        {
            float u = l.W * .42f + 1.2f;
            return l.Z0 + (seed % 2 == 1 ? l.W - u : u);
        }

        /// <summary>A front garden for a house lot on the left: lawn, a brick path to the door, a white picket fence
        /// along the footpath with a gap for the gate, rose bushes.</summary>
        static void FrontGarden(CqChunkDraft d, CqLot l, float gate, int seed)
        {
            var r = new System.Random(seed * 13 + 1);
            float depth = l.Line - K.Edge;
            E4.Patch(d.Road, -l.Line, -K.Edge, gate - .6f, gate + .6f, K.Kerb + .045f, E4.PaveBrick, ComicSurface.Paving);
            E4.Pickets(d.SlopedFrontage, -(K.Edge + .25f), l.Z0 + .1f, gate - .7f, K.Kerb, .95f, E4.White);
            E4.Pickets(d.SlopedFrontage, -(K.Edge + .25f), gate + .7f, l.Z1 - .1f, K.Kerb, .95f, E4.White);
            Color[] roses = { K.Hex("D8475A"), K.Hex("F2C6D0"), K.Hex("F0D060"), K.Hex("5E8F45") };
            for (int i = 0; i < 3; i++)
            {
                float z = Mathf.Lerp(l.Z0 + 1.2f, l.Z1 - 1.2f, (i + .5f) / 3f);
                if (Mathf.Abs(z - gate) < 1.3f) continue;
                E4.Bush(d.Props, V(-(K.Edge + F(r, 1.1f, depth * .6f)), d.Ground(z), z), F(r, .8f, 1.1f), i % 2 == 0 ? K.Hex("5E8F45") : roses[r.Next(3)], seed + i);
            }
        }
    }
}
