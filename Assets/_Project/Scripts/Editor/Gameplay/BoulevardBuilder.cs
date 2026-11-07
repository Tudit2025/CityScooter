using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    /// 4-lane boulevard sections in the comic style, all generated:
    ///   CHN_BoulevardIn      the 2-lane street widens to 4 lanes (kerbs angle out, a raised garden bed fills the corner),
    ///   CHN_BoulevardShops / CHN_BoulevardTowers   full width: 16 m road, 4 m sidewalks, dashed lane lines, double
    ///                        yellow centre line, street trees and lamps,
    ///   CHN_BoulevardOut     back to the normal street,
    /// plus boulevard traffic patterns (Data/Boulevard). Same palette, kerbs, paving and lane paint as the street chunks
    /// (ComicCityBuilder); buildings are baked from the same BLD_ geometry (ComicKitGeometry). The TrackGenerator inserts
    /// the sections (GameTuning: Boulevard sections); RoadChunk.endHalfWidth makes the rideable width follow the kerbs.
    ///   Menu: CityScooter/Comic City/3 - Build boulevard sections
    ///   Batch: -executeMethod CityScooter.EditorTools.BoulevardBuilder.BuildBatch
    /// </summary>
    public static class BoulevardBuilder
    {
        public const string PrefabFolder = ComicCityBuilder.Prefabs + "/Boulevard";
        const string MeshFolder = ComicCityBuilder.Root + "/Boulevard";
        // Not under Data/Hazards: HazardKitBuilder.ApplyTo puts everything in that folder into the street list.
        public const string PatternFolder = "Assets/_Project/Data/Boulevard";
        public const float Half = 8f, StreetHalf = 4f;
        public const int TriangleBudget = 25000;
        public static readonly string[] WideIds = { "BoulevardShops", "BoulevardTowers" };
        public static string ChunkPath(string id) => PrefabFolder + "/CHN_" + id + ".prefab";

        enum Kind { In, Wide, Out }

        struct Lot
        {
            public string Id;
            public float Start, Length, Line;
            public Lot(string id, float start, float length, float line) { Id = id; Start = start; Length = length; Line = line; }
        }

        static ComicCityRecipe _r;
        static Material _material, _ink;
        static readonly Dictionary<string, ComicMesh> Geometry = new Dictionary<string, ComicMesh>();
        static readonly List<string> Report = new List<string>();
        static readonly Color White = new Color(.95f, .95f, .93f); // lane lines (HazardKitBuilder's zebra white)

        [MenuItem("CityScooter/Comic City/3 - Build boulevard sections")]
        public static void Build()
        {
            _r = AssetDatabase.LoadAssetAtPath<ComicCityRecipe>(ComicCityBuilder.RecipePath);
            _material = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicPalette.mat");
            _ink = AssetDatabase.LoadAssetAtPath<Material>(ComicCityBuilder.Root + "/ComicInk.mat");
            if (_r == null || _material == null || _ink == null) throw new InvalidOperationException("Build the comic city first (recipe, ComicPalette.mat, ComicInk.mat).");
            if (_r.roadHalfWidth != StreetHalf || _r.chunkLength != 40f) throw new InvalidOperationException("Boulevard layout assumes 40 m chunks and an 8 m street.");
            ComicSurfaceAssets.EnsureLoaded();
            PrefabBuildUtility.EnsureFolder(PrefabFolder);
            PrefabBuildUtility.EnsureFolder(MeshFolder);
            PrefabBuildUtility.EnsureFolder(PatternFolder);
            Geometry.Clear();
            Report.Clear();

            BuildChunk("BoulevardIn", Kind.In, 0, new[]
            {
                new Lot("CornerApartment", 0, 8, StreetHalf + 4), new Lot("Bookshop", 8, 8, Half + 4), new Lot("ShopApartment", 16, 8, Half + 4),
                new Lot("Office", 24, 8, Half + 4), new Lot("Cafe", 32, 8, Half + 4),
            }, new[]
            {
                new Lot("CornerStore", 0, 8, StreetHalf + 4), new Lot("Hotel", 8, 8, Half + 4), new Lot("Bakery", 16, 8, Half + 4),
                new Lot("Clinic", 24, 8, Half + 4), new Lot("PizzaShop", 32, 8, Half + 4),
            });
            BuildChunk(WideIds[0], Kind.Wide, 0, new[]
            {
                new Lot("CornerApartment", 0, 8, Half + 4), new Lot("Bookshop", 8, 8, Half + 4), new Lot("ShopApartment", 16, 8, Half + 4),
                new Lot("Cafe", 24, 8, Half + 4), new Lot("Hotel", 32, 8, Half + 4),
            }, new[]
            {
                new Lot("Bakery", 0, 8, Half + 4), new Lot("Office", 8, 8, Half + 4), new Lot("PizzaShop", 16, 8, Half + 4),
                new Lot("Pharmacy", 24, 8, Half + 4), new Lot("ScooterShop", 32, 8, Half + 4),
            });
            BuildChunk(WideIds[1], Kind.Wide, 1, new[]
            {
                new Lot("Tower", 0, 12, Half + 4), new Lot("Office", 12, 8, Half + 4), new Lot("Bank", 20, 12, Half + 4), new Lot("Hotel", 32, 8, Half + 4),
            }, new[]
            {
                new Lot("Office", 0, 8, Half + 4), new Lot("Cinema", 8, 12, Half + 4), new Lot("ShopApartment", 20, 8, Half + 4), new Lot("Tower", 28, 12, Half + 4),
            });
            BuildChunk("BoulevardOut", Kind.Out, 0, new[]
            {
                new Lot("Laundry", 0, 8, Half + 4), new Lot("Office", 8, 8, Half + 4), new Lot("Diner", 16, 8, Half + 4),
                new Lot("ShopApartment", 24, 8, Half + 4), new Lot("CornerApartment", 32, 8, StreetHalf + 4),
            }, new[]
            {
                new Lot("PostOffice", 0, 8, Half + 4), new Lot("Hotel", 8, 8, Half + 4), new Lot("Cafe", 16, 8, Half + 4),
                new Lot("Bookshop", 24, 8, Half + 4), new Lot("CornerStore", 32, 8, StreetHalf + 4),
            });

            HazardPattern[] patterns = BuildPatterns();
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ComicCityBuilder.ThemePath);
            if (theme != null) ApplyTo(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("[Boulevard] built 4 chunks (" + string.Join("; ", Report) + "), " + patterns.Length + " boulevard patterns");
        }

        /// <summary>Puts the boulevard chunks and patterns into a theme (ComicCityBuilder.BuildTheme calls this too).</summary>
        public static void ApplyTo(TrackTheme theme)
        {
            RoadChunk Chunk(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(id))?.GetComponent<RoadChunk>();
            theme.boulevardIn = Chunk("BoulevardIn");
            theme.boulevardOut = Chunk("BoulevardOut");
            theme.boulevardChunks = WideIds.Select(Chunk).Where(c => c != null).ToArray();
            // Street patterns work on the boulevard (sidewalk entries move out with the kerb), except zebra crossings:
            // an 8 m zebra on a 16 m road would look wrong. Then the 4-lane patterns.
            var list = new List<HazardPattern>();
            if (theme.hazards != null)
                foreach (HazardPattern p in theme.hazards)
                    if (p != null && p.roadHalfWidth <= StreetHalf + .01f && !HasCrossing(p)) list.Add(p);
            if (AssetDatabase.IsValidFolder(PatternFolder))
            {
                var own = new List<HazardPattern>();
                foreach (string guid in AssetDatabase.FindAssets("t:HazardPattern", new[] { PatternFolder }))
                    own.Add(AssetDatabase.LoadAssetAtPath<HazardPattern>(AssetDatabase.GUIDToAssetPath(guid)));
                own.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                list.AddRange(own);
            }
            theme.boulevardHazards = list.ToArray();
            EditorUtility.SetDirty(theme);
        }

        static bool HasCrossing(HazardPattern p) =>
            p.entries.Any(e => e.prefab != null && (e.prefab.name.Contains("Zebra") || (e.prefab.GetComponent<Pedestrian>() != null && e.prefab.GetComponent<Pedestrian>().crosses)));

        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        // ---------------------------------------------------------------- chunks

        static void BuildChunk(string id, Kind kind, int variant, Lot[] left, Lot[] right)
        {
            string path = ChunkPath(id);
            GameObject root = PrefabBuildUtility.OpenPrefab(path);
            var sync = new ChildSync(root.transform);
            float L = _r.chunkLength, g = _r.downhillGrade, h = _r.curbHeight, sw = _r.sidewalkWidth;
            var chunk = PrefabBuildUtility.GetOrAdd<RoadChunk>(root);
            chunk.length = L; chunk.endHeight = -L * g; chunk.roadType = RoadType.Street; chunk.weight = 1f; chunk.minDifficulty = 0;
            chunk.sidewalkWidth = sw; chunk.sidewalkHeight = h; chunk.curbTransitionWidth = .3f;
            chunk.halfWidth = kind == Kind.In ? StreetHalf : Half;
            chunk.endHalfWidth = kind == Kind.In ? Half : kind == Kind.Out ? StreetHalf : 0f;
            chunk.widthChangeZ = new Vector2(8f, 32f);

            ComicMesh road = Road(chunk, kind);
            var buildings = new ComicMesh();
            var background = new ComicMesh();
            var props = new ComicMesh();
            var fixedCollisions = new List<DressingObstacle>();
            var boundaryGroup = sync.Plain("BoundaryColliders");
            var boundaries = new ChildSync(boundaryGroup.transform);
            for (int side = -1; side <= 1; side += 2)
            {
                Lot[] lots = side < 0 ? left : right;
                for (int j = 0; j < lots.Length; j++) AddBuilding(buildings, background, boundaries, side, lots[j], j, variant);
                if (kind != Kind.Wide)
                {
                    ComicMesh bed = GardenBed(chunk, side);
                    bed.Shear(g);
                    buildings.Add(bed, Vector3.zero, Quaternion.identity, Vector3.one);
                    BedPlants(props, chunk, side, kind);
                }
                else StreetTrees(props, fixedCollisions, side, variant);
            }
            boundaries.Finish();
            if (!LotsCover(left, L) || !LotsCover(right, L)) throw new InvalidOperationException(id + ": building lots must cover the whole chunk (no gaps in the facades).");

            Renderer(sync.Plain("RoadAndSidewalk"), "CHN_" + id + "_Road", road, false);
            Renderer(sync.Plain("Frontage"), "CHN_" + id + "_Frontage", buildings, true);
            GameObject furniture = sync.Plain("StreetFurniture");
            Mesh[] layouts;
            if (kind == Kind.Wide)
            {
                // Like the street chunks: a few layouts of small sidewalk props; StreetDressing picks one per chunk and
                // turns their colliders into Obstacles.
                const int layoutCount = 6;
                layouts = new Mesh[layoutCount];
                var collisionLayouts = new DressingCollisionLayout[layoutCount];
                for (int layout = 0; layout < layoutCount; layout++)
                {
                    var dressing = new ComicMesh();
                    dressing.Add(props, Vector3.zero, Quaternion.identity, Vector3.one);
                    var collisions = new List<DressingObstacle>(fixedCollisions);
                    ScatterWallProps(dressing, collisions, variant, layout);
                    collisionLayouts[layout] = new DressingCollisionLayout { obstacles = collisions.ToArray() };
                    if (layout == 0)
                    {
                        Renderer(furniture, "CHN_" + id + "_Props", dressing, true);
                        layouts[0] = furniture.GetComponent<MeshFilter>().sharedMesh;
                    }
                    else layouts[layout] = SaveMesh("CHN_" + id + "_Props_" + layout.ToString("D2"), dressing);
                }
                var selection = PrefabBuildUtility.GetOrAdd<StreetDressing>(root);
                selection.surface = furniture.GetComponent<MeshFilter>();
                selection.outline = furniture.transform.Find("InkOutline").GetComponent<MeshFilter>();
                selection.layouts = layouts;
                selection.collisionLayouts = collisionLayouts;
            }
            else
            {
                // Transitions: only plants in the garden beds, out of reach of the rider (no colliders).
                Renderer(furniture, "CHN_" + id + "_Props", props, true);
                layouts = new[] { furniture.GetComponent<MeshFilter>().sharedMesh };
                PrefabBuildUtility.SyncComponent<StreetDressing>(root, false);
            }
            Renderer(sync.Plain("DeepCity"), "CHN_" + id + "_Background", background, false);

            var slotsGo = sync.Plain("SpawnSlots");
            var slots = new ChildSync(slotsGo.transform);
            void Row(float z, params float[] lanes)
            {
                foreach (float x in lanes)
                {
                    GameObject slot = slots.Plain("Row" + z.ToString(CultureInfo.InvariantCulture) + "_Lane" + x.ToString(CultureInfo.InvariantCulture));
                    slot.transform.localPosition = new Vector3(x, -g * z, z);
                    PrefabBuildUtility.GetOrAdd<SpawnSlot>(slot).kind = SpawnKind.Any;
                }
            }
            // Four lanes on the boulevard; the transitions keep to where the road is wide enough for sure.
            if (kind == Kind.Wide) { Row(10, -6, -2, 2, 6); Row(25, -6, -2, 2, 6); }
            else if (kind == Kind.In) { Row(5, -2.6f, 0, 2.6f); Row(25, -2, 2); }
            else { Row(15, -2, 2); Row(35, -2.6f, 0, 2.6f); }
            slots.Finish();
            sync.Finish();

            int tris = PrefabBuildUtility.CountTriangles(root), renderers = root.GetComponentsInChildren<MeshRenderer>(true).Length;
            tris += 2 * (layouts.Max(m => (int)m.GetIndexCount(0) / 3) - (int)layouts[0].GetIndexCount(0) / 3);
            PrefabBuildUtility.SaveAndClose(root, path);
            Report.Add(id + " " + tris + " tris, " + renderers + " renderers");
            if (tris > TriangleBudget) throw new InvalidOperationException(id + " exceeds the 25k triangle chunk budget: " + tris);
        }

        static bool LotsCover(Lot[] lots, float length)
        {
            float z = 0f;
            foreach (Lot lot in lots.OrderBy(l => l.Start))
            {
                if (Mathf.Abs(lot.Start - z) > .01f) return false;
                z = lot.Start + lot.Length;
            }
            return Mathf.Abs(z - length) < .01f;
        }

        // ---- road, kerbs, sidewalks and paint. Everything follows RoadChunk.HalfWidthAt so art and gameplay agree.

        static ComicMesh Road(RoadChunk c, Kind kind)
        {
            float L = c.length, h = c.sidewalkHeight, sw = c.sidewalkWidth;
            var m = new ComicMesh();
            Color joint = Color.Lerp(_r.ink, _r.pavement, .5f);
            float step = c.ChangesWidth ? 2f : 4f; // short spans: the GPU city curl and fake turns bend them smoothly
            int n = Mathf.RoundToInt(L / step);
            for (int i = 0; i < n; i++)
            {
                float z0 = i * step, z1 = z0 + step, w0 = c.HalfWidthAt(z0), w1 = c.HalfWidthAt(z1);
                m.Surface = ComicSurface.Asphalt;
                Face(m, V(-w0, 0, z0), V(-w1, 0, z1), V(w1, 0, z1), V(w0, 0, z0), _r.asphalt, Vector3.up);
                for (int s = -1; s <= 1; s += 2)
                {
                    float i0 = s * w0, i1 = s * w1, o0 = s * (w0 + sw), o1 = s * (w1 + sw);
                    Vector3 inward = new Vector3(-s, 0, 0), outward = new Vector3(s, 0, 0);
                    m.Surface = ComicSurface.Paving;
                    Face(m, V(i0, h, z0), V(i1, h, z1), V(o1, h, z1), V(o0, h, z0), _r.pavement, Vector3.up);
                    Face(m, V(i0, -.05f, z0), V(i1, -.05f, z1), V(i1, h, z1), V(i0, h, z0), _r.pavement, inward); // kerb face
                    Face(m, V(o0, -.4f, z0), V(o1, -.4f, z1), V(o1, h, z1), V(o0, h, z0), _r.pavement, outward);
                    // Cream kerb cap (street chunks: 0.24 m wide, top 4 cm above the sidewalk).
                    m.Surface = ComicSurface.Plain;
                    float c0 = s * (w0 + .24f), c1 = s * (w1 + .24f), top = h + .04f;
                    Face(m, V(i0, top, z0), V(i1, top, z1), V(c1, top, z1), V(c0, top, z0), _r.cream, Vector3.up);
                    Face(m, V(i0, h, z0), V(i1, h, z1), V(i1, top, z1), V(i0, top, z0), _r.cream, inward);
                    Face(m, V(c0, h, z0), V(c1, h, z1), V(c1, top, z1), V(c0, top, z0), _r.cream, outward);
                    // Road edge line 0.15 m inside the kerb, paving joints parallel to the kerb.
                    Flat(m, s * (w0 - .19f), s * (w0 - .11f), s * (w1 - .19f), s * (w1 - .11f), z0, z1, .0305f, _r.cream);
                    for (int k = 1; k < 4; k++)
                        Flat(m, s * (w0 + k) - .0125f, s * (w0 + k) + .0125f, s * (w1 + k) - .0125f, s * (w1 + k) + .0125f, z0, z1, h + .025f, joint);
                }
            }
            // Cross joints every 2 m, like the street chunks.
            for (int z = 0; z < 40; z += 2)
                for (int s = -1; s <= 1; s += 2)
                {
                    float w = c.HalfWidthAt(z);
                    Flat(m, s * w, s * (w + sw), s * w, s * (w + sw), z - .0125f, z + .0125f, h + .025f, joint);
                }
            // Centre: the street's dashed double yellow where the road is 2 lanes, solid double yellow where it is 4.
            float solidFrom = kind == Kind.In ? c.widthChangeZ.x : 0f, solidTo = kind == Kind.Out ? c.widthChangeZ.y : L;
            for (int s = -1; s <= 1; s += 2)
            {
                Line(m, s * .12f, solidFrom, solidTo, _r.yellow);
                for (int z = 0; z < 40; z += 5)
                    if (z + 3 <= solidFrom + .01f || z >= solidTo - .01f) Line(m, s * .12f, z, z + 3, _r.yellow);
                // Lane lines: 3 m white dashes every 8 m at x = +-4, wherever both lanes on that side are wide enough.
                for (int z = 0; z < 40; z += 8)
                    if (Mathf.Min(c.HalfWidthAt(z), c.HalfWidthAt(z + 3)) >= 6.9f) Line(m, s * StreetHalf, z, z + 3, White);
            }
            // Closed slab ends (hidden by the neighbouring chunk, like the street chunks' box ends).
            foreach (float ze in new[] { 0f, L })
            {
                Vector3 normal = ze <= 0f ? Vector3.back : Vector3.forward;
                float w = c.HalfWidthAt(ze);
                m.Surface = ComicSurface.Asphalt;
                Face(m, V(-w, -.8f, ze), V(w, -.8f, ze), V(w, 0, ze), V(-w, 0, ze), _r.asphalt, normal);
                m.Surface = ComicSurface.Paving;
                for (int s = -1; s <= 1; s += 2)
                    Face(m, V(s * w, -.4f, ze), V(s * (w + sw), -.4f, ze), V(s * (w + sw), h, ze), V(s * w, h, ze), _r.pavement, normal);
            }
            m.Shear(_r.downhillGrade);
            m.ClearCreaseInk(); // the road never gets crease ink (street chunks: no ink on recycled slab seams)
            return m;
        }

        // Straight paint line along z at x, in spans of at most 4 m.
        static void Line(ComicMesh m, float x, float from, float to, Color color)
        {
            for (float z = from; z < to - .001f; z += 4f)
            {
                float end = Mathf.Min(to, z + 4f);
                Flat(m, x - .05f, x + .05f, x - .05f, x + .05f, z, end, .035f, color);
            }
        }

        // A flat quad at height y (paint, joints): left/right x at z0 and z1.
        static void Flat(ComicMesh m, float l0, float r0, float l1, float r1, float z0, float z1, float y, Color color)
        {
            m.Surface = ComicSurface.Plain;
            Face(m, V(l0, y, z0), V(l1, y, z1), V(r1, y, z1), V(r0, y, z0), color, Vector3.up);
        }

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        // A quad facing `normal` whatever order the corners come in.
        static void Face(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector3 normal, bool ink = true)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f) m.Quad(d, c, b, a, color, ink);
            else m.Quad(a, b, c, d, color, ink);
        }

        // A curved strip piece: ink only along its two long edges (and the strip ends at v = 0 / 1).
        static void Ribbon(ComicMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector3 normal, float v0, float v1)
        {
            m.RibbonQuad(a, b, c, d, color, v0, v1, Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f);
        }

        // ---- the raised garden bed between the angled kerb line and the boulevard building line (In / Out chunks)

        const float BedWallTop = .9f, BedSoil = .72f, BedBottom = -.4f, BedWall = .3f;

        static float BuildingLine => Half + 4f;

        static ComicMesh GardenBed(RoadChunk c, int side)
        {
            var m = new ComicMesh();
            float sw = c.sidewalkWidth, from = c.widthChangeZ.x, to = c.widthChangeZ.y, line = BuildingLine;
            Color wall = Color.Lerp(_r.pavement, _r.ink, .3f), soil = Color.Lerp(_r.green, _r.ink, .12f);
            const int n = 24;
            float span = to - from;
            for (int i = 0; i < n; i++)
            {
                float z0 = from + span * i / n, z1 = from + span * (i + 1) / n;
                float e0 = c.HalfWidthAt(z0) + sw, e1 = c.HalfWidthAt(z1) + sw;
                if (line - e0 < .02f && line - e1 < .02f) continue;
                float x0 = side * e0, x1 = side * e1;
                float k0 = side * Mathf.Min(e0 + BedWall, line), k1 = side * Mathf.Min(e1 + BedWall, line), b = side * line;
                float v0 = (float)i / n, v1 = (float)(i + 1) / n;
                Vector3 inward = new Vector3(-side, 0, 0), outward = new Vector3(side, 0, 0);
                m.Surface = ComicSurface.Concrete;
                Ribbon(m, V(x0, BedBottom, z0), V(x1, BedBottom, z1), V(x1, BedWallTop, z1), V(x0, BedWallTop, z0), wall, inward, v0, v1);
                Ribbon(m, V(x0, BedWallTop, z0), V(x1, BedWallTop, z1), V(k1, BedWallTop, z1), V(k0, BedWallTop, z0), _r.cream, Vector3.up, v0, v1);
                Ribbon(m, V(k0, BedSoil, z0), V(k1, BedSoil, z1), V(k1, BedWallTop, z1), V(k0, BedWallTop, z0), wall, outward, v0, v1);
                m.Surface = ComicSurface.Foliage;
                Face(m, V(k0, BedSoil, z0), V(k1, BedSoil, z1), V(b, BedSoil, z1), V(b, BedSoil, z0), soil, Vector3.up, false);
            }
            // The wide end of the bed is closed against the corner building's side wall.
            bool widening = c.endHalfWidth > c.halfWidth;
            float ze = widening ? from : to, e = c.HalfWidthAt(ze) + sw;
            m.Surface = ComicSurface.Concrete;
            Face(m, V(side * e, BedBottom, ze), V(side * line, BedBottom, ze), V(side * line, BedWallTop, ze), V(side * e, BedWallTop, ze), wall,
                widening ? Vector3.back : Vector3.forward);
            return m;
        }

        static void BedPlants(ComicMesh props, RoadChunk c, int side, Kind kind)
        {
            float g = _r.downhillGrade;
            foreach (float at in new[] { 10f, 15.5f, 21f })
            {
                float z = kind == Kind.In ? at : c.length - at;
                float inner = c.HalfWidthAt(z) + c.sidewalkWidth + BedWall, room = BuildingLine - inner, x = side * (inner + BuildingLine) * .5f;
                if (room >= 2.1f) props.Add(PropMesh("TreePit"), V(x, -g * z + BedSoil - .35f, z), Quaternion.identity, Vector3.one);
                else if (room >= .9f) props.Add(PropMesh("FlowerPot"), V(x, -g * z + BedSoil - .05f, z), Quaternion.identity, Vector3.one);
            }
        }

        // ---- buildings (same BLD_ geometry as the street), deep foundations, invisible walls, distant blocks

        static void AddBuilding(ComicMesh buildings, ComicMesh background, ChildSync boundaries, int side, Lot lot, int index, int variant)
        {
            float g = _r.downhillGrade, h = _r.curbHeight;
            ComicAssetSpec spec = Spec(lot.Id, true);
            float zc = lot.Start + lot.Length * .5f, x = side * (lot.Line + spec.depth * .5f);
            Vector3 p = V(x, -g * zc + h, zc);
            buildings.Add(BuildingMesh(lot.Id), p, Quaternion.Euler(0, side < 0 ? 90 : -90, 0), V(lot.Length / spec.width, 1, 1));
            // Deep foundation fills the downhill gap below the upright building (as in the street chunks).
            buildings.Surface = ComicSurface.Concrete;
            buildings.Box(V(x, p.y - 1.1f, zc), V(spec.depth, 2.2f, lot.Length), _r.ink);
            GameObject wall = boundaries.Plain((side < 0 ? "L" : "R") + index);
            wall.transform.localPosition = V(x, p.y + spec.height * .5f - 1f, zc);
            var box = PrefabBuildUtility.GetOrAdd<BoxCollider>(wall);
            box.center = Vector3.zero;
            box.size = V(spec.depth, spec.height + 2f, lot.Length);
            float bx = side * (lot.Line + spec.depth + 7f), by = -g * zc;
            background.Surface = ComicSurface.Concrete;
            for (int layer = 0; layer < 2; layer++)
            {
                float bh = 12 + ((index * 7 + variant * 3 + layer * 5 + (side + 1)) % 5) * 4;
                Color bc = Color.Lerp(spec.color, new Color(.49f, .64f, .75f), .25f + layer * .22f);
                background.Box(V(bx + side * layer * 13, by + bh * .5f - 2f, zc), V(14, bh + 4, lot.Length + .2f), bc);
                background.Box(V(bx + side * layer * 13, by + bh, zc), V(14.2f, .3f, lot.Length + .3f), Color.Lerp(bc, _r.ink, .2f));
            }
        }

        // ---- boulevard sidewalk furniture

        // Fixed per chunk: a row of street trees in the kerb strip, lamps between them, a bus shelter on the Towers chunk.
        static void StreetTrees(ComicMesh props, List<DressingObstacle> collisions, int side, int variant)
        {
            float g = _r.downhillGrade, h = _r.curbHeight, yaw = side < 0 ? 90f : -90f;
            void Put(string id, float x, float z)
            {
                Vector3 p = V(side * x, -g * z + h, z);
                props.Add(PropMesh(id), p, Quaternion.Euler(0, yaw, 0), Vector3.one);
                collisions.Add(Collision(id, p, yaw));
            }
            bool shelter = variant == 1 && side > 0;
            for (float z = side < 0 ? 5f : 0f; z < 40f; z += 10f) Put("TreePit", Half + 1f, z);
            foreach (float z in side < 0 ? new[] { 10f, 30f } : new[] { 5f, 25f }) Put("StreetLamp", Half + .55f, z);
            if (shelter) Put("BusShelter", Half + 1.25f, 15f);
        }

        // Layout variety: a few small props against the building fronts (benches, bins, bike racks...).
        static void ScatterWallProps(ComicMesh mesh, List<DressingObstacle> collisions, int variant, int layout)
        {
            string[] pool = { "Bench", "Bin", "BikeRack", "Planter", "FlowerPot", "NewspaperBox", "Mailbox", "ShopBoard", "VendingMachine", "RecycleBins" };
            var rng = new System.Random(unchecked(_r.seed * 7919 ^ (variant + 11) * 104729 ^ layout * 15485863));
            float g = _r.downhillGrade, h = _r.curbHeight;
            for (int side = -1; side <= 1; side += 2)
            {
                var occupied = new List<Vector2>();
                int count = 1 + rng.Next(2);
                for (int i = 0; i < count; i++)
                {
                    string id = pool[rng.Next(pool.Length)];
                    ComicAssetSpec spec = Spec(id, false);
                    float yaw = (side < 0 ? 90f : -90f) + (float)(rng.NextDouble() * 16 - 8);
                    float extentX = spec.depth * .5f + .05f, extentZ = spec.width * .5f + .05f;
                    for (int attempt = 0; attempt < 40; attempt++)
                    {
                        float z = Mathf.Lerp(2f + extentZ, 38f - extentZ, (float)rng.NextDouble());
                        if (occupied.Any(o => Mathf.Abs(o.x - z) < o.y + extentZ + .6f)) continue;
                        occupied.Add(new Vector2(z, extentZ));
                        Vector3 p = V(side * (BuildingLine - extentX - .15f), -g * z + h, z);
                        mesh.Add(PropMesh(id), p, Quaternion.Euler(0, yaw, 0), Vector3.one);
                        collisions.Add(Collision(id, p, yaw));
                        break;
                    }
                }
            }
        }

        // Same collider as the PRP_ prefab (ComicCityBuilder.BuildAsset): thin trunk for trees, lamps and signs.
        static DressingObstacle Collision(string id, Vector3 position, float yaw)
        {
            ComicAssetSpec spec = Spec(id, false);
            bool thin = spec.style == "tree" || spec.style == "lamp" || spec.style == "sign";
            return new DressingObstacle
            {
                id = id, position = position, rotation = V(0, yaw, 0), centre = V(0, spec.height * .5f, 0),
                size = thin ? V(.35f, spec.height, .35f) : V(spec.width, spec.height, spec.depth), canJumpOver = spec.height <= 1.1f,
            };
        }

        static ComicAssetSpec Spec(string id, bool building)
        {
            ComicAssetSpec spec = (building ? _r.buildings : _r.props).FirstOrDefault(s => s.id == id);
            if (spec == null) throw new InvalidOperationException("No " + (building ? "building " : "prop ") + id + " in the comic city recipe.");
            return spec;
        }

        static ComicMesh BuildingMesh(string id)
        {
            if (!Geometry.TryGetValue("BLD_" + id, out ComicMesh m)) Geometry["BLD_" + id] = m = ComicKitGeometry.Building(Spec(id, true), _r);
            return m;
        }

        static ComicMesh PropMesh(string id)
        {
            if (!Geometry.TryGetValue("PRP_" + id, out ComicMesh m)) Geometry["PRP_" + id] = m = ComicKitGeometry.Prop(Spec(id, false), _r);
            return m;
        }

        // ---- meshes and renderers (same conventions as ComicCityBuilder: ComicPalette + optional ComicInk outline)

        static Mesh SaveMesh(string id, ComicMesh data)
        {
            string path = MeshFolder + "/" + id + ".asset";
            Mesh mesh = data.Bake(id), existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            // Unity culls on the CPU, before the vertex shader curls the far city up: cover the build range + one chunk,
            // exactly like the street chunks (ComicCityBuilder.SaveMesh).
            Bounds b = mesh.bounds;
            b.Encapsulate(b.max + Vector3.up * (ComicCityProjection.Rise(_r, 360) + 1));
            mesh.bounds = b;
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); existing = mesh; }
            else
            {
                var writable = new SerializedObject(existing);
                writable.FindProperty("m_IsReadable").boolValue = true;
                writable.ApplyModifiedPropertiesWithoutUndo();
                existing.Clear(); existing.indexFormat = mesh.indexFormat;
                existing.SetVertices(mesh.vertices); existing.SetNormals(mesh.normals); existing.SetColors(mesh.colors);
                var uv = new List<Vector3>(); mesh.GetUVs(0, uv); existing.SetUVs(0, uv);
                mesh.GetUVs(1, uv); existing.SetUVs(1, uv);
                var surfaceUV = new List<Vector4>(); mesh.GetUVs(2, surfaceUV); existing.SetUVs(2, surfaceUV);
                existing.SetTriangles(mesh.triangles, 0); existing.bounds = mesh.bounds;
                existing.UploadMeshData(false);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
            }
            var so = new SerializedObject(existing);
            SerializedProperty readable = so.FindProperty("m_IsReadable");
            if (readable != null) { readable.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
            return existing;
        }

        static void Renderer(GameObject go, string id, ComicMesh data, bool outline)
        {
            Mesh mesh = SaveMesh(id, data);
            PrefabBuildUtility.GetOrAdd<MeshFilter>(go).sharedMesh = mesh;
            var mr = PrefabBuildUtility.GetOrAdd<MeshRenderer>(go);
            mr.sharedMaterial = _material;
            mr.shadowCastingMode = outline ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = true;
            var children = new ChildSync(go.transform);
            if (outline)
            {
                GameObject ink = children.Plain("InkOutline");
                PrefabBuildUtility.GetOrAdd<MeshFilter>(ink).sharedMesh = mesh;
                var imr = PrefabBuildUtility.GetOrAdd<MeshRenderer>(ink);
                imr.sharedMaterial = _ink; imr.shadowCastingMode = ShadowCastingMode.Off; imr.receiveShadows = false;
            }
            children.Finish();
        }

        // ---------------------------------------------------------------- 4-lane traffic patterns

        static HazardPattern[] BuildPatterns()
        {
            GameObject P(string id)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(ComicCityBuilder.Prefabs + "/Hazards/HZ_" + id + ".prefab");
                if (go == null) throw new InvalidOperationException("Build the hazard kit first (HZ_" + id + ").");
                return go;
            }
            HazardPattern.Entry E(string id, float x, float z) =>
                new HazardPattern.Entry { prefab = P(id), x = x, z = z, yaw = id.StartsWith("CarOncoming") ? 180f : 0f };

            var list = new List<HazardPattern>();
            // Australia drives on the left: same-direction (slower) cars in x = -2 / -6, oncoming cars in x = +2 / +6.
            // Traffic patterns are never mirrored.
            list.Add(Pattern("HZB_TwoWayTraffic", new List<HazardPattern.Entry>
            {
                E("CarSlowYellow", -2f, 40), E("CarOncomingRed", 6f, 55), E("CarOncomingBlue", 2f, 95),
            }, 105, 1, 1.2f, false, 50));
            list.Add(Pattern("HZB_OncomingBothLanes", new List<HazardPattern.Entry>
            {
                E("CarOncomingRed", 2f, 45), E("CarOncomingBlue", 6f, 80),
            }, 95, 2, 1f, false, 50));
            list.Add(Pattern("HZB_SlowBothLanes", new List<HazardPattern.Entry>
            {
                E("CarSlowGreen", -6f, 35), E("CarSlowYellow", -2f, 70),
            }, 95, 2, 1f, false, 45));
            list.Add(Pattern("HZB_RushHour", new List<HazardPattern.Entry>
            {
                E("CarSlowYellow", -2f, 35), E("CarOncomingRed", 2f, 60), E("CarSlowGreen", -6f, 85), E("CarOncomingBlue", 6f, 100),
            }, 115, 4, .8f, false, 55));
            // Roadworks closing the kerbside lane (mirrored: the oncoming kerbside lane). Three lanes stay open.
            var works = new List<HazardPattern.Entry> { E("SignRoadWork", -(Half + .9f), 0) };
            float[] taper = { -7.6f, -6.8f, -6f, -5.2f, -4.4f };
            for (int i = 0; i < taper.Length; i++) works.Add(E("Cone", taper[i], 10 + i * 4));
            for (int i = 0; i < 6; i++) works.Add(E("BarrierLong", -4.35f, 32 + i * 6));
            works.Add(E("WorksTruck", -6.2f, 44)); works.Add(E("Worker", -7.3f, 52)); works.Add(E("Worker", -5.5f, 58));
            works.Add(E("Manhole", -6.2f, 66));
            for (int i = 0; i < 4; i++) works.Add(E("Cone", -4.4f - i * 1.05f, 72 + i * 4));
            list.Add(Pattern("HZB_RoadworksKerbLane", works, 90, 0, 1.2f, true));
            return list.ToArray();
        }

        static HazardPattern Pattern(string name, List<HazardPattern.Entry> entries, float length, int minLevel, float weight, bool mirror, float meet = 40f)
        {
            string path = PatternFolder + "/" + name + ".asset";
            var p = AssetDatabase.LoadAssetAtPath<HazardPattern>(path);
            if (p == null) { p = ScriptableObject.CreateInstance<HazardPattern>(); AssetDatabase.CreateAsset(p, path); }
            entries.Sort((a, b) => a.z.CompareTo(b.z));
            p.entries = entries.ToArray(); p.length = length; p.minLevel = minLevel; p.weight = weight; p.allowMirror = mirror;
            p.turnDirection = 0; p.turnAtZ = 0; p.meetAtZ = meet; p.roadHalfWidth = Half;
            EditorUtility.SetDirty(p);
            return p;
        }
    }
}
