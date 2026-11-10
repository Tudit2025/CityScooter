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
    /// East line, art agent 3 (v0.5): BAYSWATER (route order 270), ASHFIELD (280) and BASSENDEAN (290) on the Midland
    /// train line. The rider goes east: the railway is on the left (-X) and the Swan River side on the right (+X).
    /// BAYSWATER: Whatley Crescent beside the new elevated METRONET station (2023-24): the long viaduct on cream fluted
    ///   hammerhead piers, the fluted cream edge band, two island platforms under flat white canopies with copper soffits,
    ///   the concourse and bus stands under the deck, King William St passing under the new 4.8 m bridge (with a memorial
    ///   to the old 3.8 m "Bayswater Bridge" that trucks kept hitting), the plaza, the junction where the Airport line dives
    ///   into its tunnel and the Ellenbrook line climbs onto its viaduct; old shopfronts with boxed awnings, the two-storey
    ///   verandah hotel and the old post office on the right.
    /// ASHFIELD: the red shared path beside the railway: the tiny island-platform station with its barrel-vault shelter
    ///   and long footbridge ramps, the light industry beyond the tracks, Ashfield Reserve with its big figs and the
    ///   1950s State Housing cottages, then Ashfield Flats: samphire, sedges, pools, leaning paperbarks, the jarrah
    ///   boardwalk over the Chapman St drain (a kicker and a SPLASH gap), egrets, ibis and ducks.
    /// BASSENDEAN: the station overpass and the rail museum's steam locomotive under its red canopy (a sleeper loading
    ///   ramp), Old Perth Rd with its parapet shopfronts, cantilever awnings and the corner hotel, the granite war memorial
    ///   in BIC Reserve (background only), Bassendean Oval: the 1929 gate, the two cream weatherboard grandstands, the
    ///   white picket fence, the four goal posts and the light towers; jacarandas in bloom.
    /// Uses the CBD / Quay comic kit (CbdQuayKit, CqChunkDraft): ComicPalette + ComicInk only, vertex colours.
    ///   Menu: CityScooter/Districts/Build East 3
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.East3Builder.BuildBatch
    ///   Batch with the props atlases (needs graphics): ...East3Builder.BuildAndRenderAtlasBatch
    /// The files: East3Builder.cs (this: entry points and the shared kit), East3Bayswater.cs, East3Ashfield.cs,
    /// East3Bassendean.cs.
    /// </summary>
    public static partial class East3Builder
    {
        public const string BayId = "bayswater", AshId = "ashfield", BasId = "bassendean";
        public const string BayPrefabs = ComicCityBuilder.Prefabs + "/Districts/Bayswater", BayMeshes = ComicCityBuilder.Root + "/Districts/Bayswater";
        public const string AshPrefabs = ComicCityBuilder.Prefabs + "/Districts/Ashfield", AshMeshes = ComicCityBuilder.Root + "/Districts/Ashfield";
        public const string BasPrefabs = ComicCityBuilder.Prefabs + "/Districts/Bassendean", BasMeshes = ComicCityBuilder.Root + "/Districts/Bassendean";
        public const string BayDistrictPath = CbdBuilder.DataFolder + "/District_Bayswater.asset";
        public const string AshDistrictPath = CbdBuilder.DataFolder + "/District_Ashfield.asset";
        public const string BasDistrictPath = CbdBuilder.DataFolder + "/District_Bassendean.asset";
        public static readonly string[] BayChunkIds = { "Shops", "Viaduct", "Station", "Plaza", "Junction" };
        public static readonly string[] AshChunkIds = { "Station", "Reserve", "Flats", "Drain", "Paperbarks" };
        public static readonly string[] BasChunkIds = { "Station", "Museum", "OldPerthRd", "Memorial", "Oval" };
        public static string BayChunkPath(string v) => BayPrefabs + "/CHN_Bayswater_" + v + ".prefab";
        public static string AshChunkPath(string v) => AshPrefabs + "/CHN_Ashfield_" + v + ".prefab";
        public static string BasChunkPath(string v) => BasPrefabs + "/CHN_Bassendean_" + v + ".prefab";

        /// <summary>Drop per 40 m chunk: the Swan River plain is nearly flat.</summary>
        public const float BayDrop = -1.6f, AshDrop = -1f, BasDrop = -1.4f;

        // ---- shared palette (sRGB; notes in art-src/districts/<id>/SOURCES.txt)
        static readonly Color White = K.Hex("F5F3EC"), Cream = K.Hex("EFE4C8"), TrimCream = K.Hex("F3E7C6"), Charcoal = K.Hex("3A4047"), CharcoalDark = K.Hex("2A2F35");
        static readonly Color RedAsphalt = K.Hex("7F4C42"), PathRed = K.Hex("9C5647"), BuffPaver = K.Hex("DBC9A4"), BrickPaver = K.Hex("AC7A64"), KerbCap = K.Hex("E9E3D6"), ConcretePath = K.Hex("CFCBC0");
        static readonly Color Lawn = K.Hex("6FA646"), LawnLight = K.Hex("86B757"), LawnDry = K.Hex("9AAE5C"), Mulch = K.Hex("8A6A4C"), Dirt = K.Hex("B79E78");
        static readonly Color Limestone = K.Hex("E2D2A6"), LimestoneCap = K.Hex("F0E6CA"), LimestoneDark = K.Hex("BFA978");
        static readonly Color Ballast = K.Hex("8F877B"), BallastDark = K.Hex("7A7368"), Sleeper = K.Hex("B9B4A8"), RailSteel = K.Hex("4B5057"), Galv = K.Hex("AEB5BB"), GalvDark = K.Hex("8C949B");
        static readonly Color Stainless = K.Hex("CDD2D7"), StainlessDark = K.Hex("AAB1B8"), TrainBand = K.Hex("2B3138"), TransGreen = K.Hex("35A852"), DoorYellow = K.Hex("F2C230");
        static readonly Color WindowGlass = K.Hex("2F6F86"), ShopGlass = K.Hex("33505E"), PlantGrey = K.Hex("8F969C");
        static readonly Color RedBrick = K.Hex("B5553C"), RedBrickDark = K.Hex("96442F"), CreamBrick = K.Hex("E3CFA0"), Terracotta = K.Hex("C0613F"), TileBrown = K.Hex("A5553A"), RedIron = K.Hex("A8443A"), Zinc = K.Hex("BFC6CB"), ZincDark = K.Hex("9BA3A9"), GreyIron = K.Hex("7F888F");
        static readonly Color Timber = K.Hex("A8774E"), TimberDark = K.Hex("7C573A"), Jarrah = K.Hex("8E5A45"), JarrahLight = K.Hex("B07A5C");
        static readonly Color GumTrunk = K.Hex("D9D2C2"), GumTrunkShade = K.Hex("B5AC9A"), GumLeaf = K.Hex("64864A"), GumLeafLight = K.Hex("86A45C"), GumLeafDark = K.Hex("4D6E3F");
        static readonly Color Jac1 = K.Hex("8E6CC9"), Jac2 = K.Hex("A486DB"), Jac3 = K.Hex("C4ADEB"), JacBark = K.Hex("6F5E52");
        static readonly Color PaperTrunk = K.Hex("EDE6D6"), PaperPatch = K.Hex("C9B49A"), PaperLeaf = K.Hex("6F8F5A"), PaperLeafLight = K.Hex("8FAA6C"), PaperLeafDark = K.Hex("55704A");
        static readonly Color FigLeaf = K.Hex("2F5E3A"), FigLeafLight = K.Hex("47794A"), FigBark = K.Hex("7D7468");
        static readonly Color HedgeGreen = K.Hex("4E7F3A"), Shrub = K.Hex("5F8A45"), ShrubLight = K.Hex("86AA5A");
        static readonly Color SignGreen = K.Hex("1F7A4D"), SignBlue = K.Hex("2456B4"), Navy = K.Hex("27375F"), HazardRed = K.Hex("D8332B"), HazardYellow = K.Hex("F6C61E");
        static readonly Color[] SignColours = { K.Hex("C8463A"), K.Hex("E9B83C"), K.Hex("2A8F8A"), K.Hex("2F6FB0"), K.Hex("E58A2E"), K.Hex("7A5FA8"), K.Hex("3E8E55") };

        [MenuItem("CityScooter/Districts/Build East 3")]
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
            BuildBayswater(LoadTheme(), atlas);
            BuildAshfield(LoadTheme(), atlas);
            BuildBassendean(LoadTheme(), atlas);
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

        static RoadChunk Chunk(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<RoadChunk>();

        /// <summary>Writes a chunk and logs where its triangles went (outlined meshes count twice in the budget).</summary>
        static void Write(CqChunkDraft d, string path)
        {
            int outlined = d.Frontage.TriangleCount + d.SlopedFrontage.TriangleCount + d.Props.TriangleCount + d.Landmark.TriangleCount;
            Debug.Log("[East3] " + d.Id + ": road " + d.Road.TriangleCount + ", frontage " + d.Frontage.TriangleCount + " + sloped " + d.SlopedFrontage.TriangleCount +
                      ", props " + d.Props.TriangleCount + ", landmark " + d.Landmark.TriangleCount + ", deep " + d.Deep.TriangleCount +
                      " => " + (d.Road.TriangleCount + d.Deep.TriangleCount + 2 * outlined) + " / 25000");
            K.WriteChunk(d, path);
        }

        /// <summary>The owned District fields; gateway, ambience, ambienceVolume, skyYaw, skySize, skyHeight, skyline and
        /// line belong to the district system and keep whatever is there (placeholders only when the asset is new).</summary>
        static District OpenDistrict(string path, string id, string name, int order)
        {
            bool created = AssetDatabase.LoadAssetAtPath<District>(path) == null;
            District d = K.LoadOrCreate<District>(path);
            // Placeholders for a new asset only (east of the city: skyline behind and sunk away); the system sets them.
            if (created) { d.skyYaw = 180f; d.skySize = .6f; d.skyHeight = -12f; d.skyline = 0f; }
            d.id = id; d.displayName = name; d.order = order;
            d.orderedChunks = true; d.allowBoulevard = false; d.gifts = Array.Empty<Gift>(); d.blendMeters = 150f;
            return d;
        }

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static float F(System.Random r, float a, float b) => Mathf.Lerp(a, b, (float)r.NextDouble());
        static ComicMesh Make(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }

        /// <summary>Builds in flat road space, shears with the chunk's grade and adds to a mesh that is not sheared on
        /// write (Landmark, Deep, Props): long things that follow the road (viaduct, tracks, fences, trains).</summary>
        static void Sloped(CqChunkDraft d, ComicMesh target, Action<ComicMesh> build)
        {
            var t = new ComicMesh();
            build(t);
            t.Shear(d.Grade);
            target.Add(t, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        static Bounds KeepOutBox(CqChunkDraft d, int s, float z0, float z1)
        {
            float yTop = d.Y(z0) + 5f, yBottom = d.Y(z1) - 2f;
            return new Bounds(new Vector3(s * (K.Half + K.Side * .5f + .3f), (yTop + yBottom) * .5f, (z0 + z1) * .5f), new Vector3(K.Side + .6f, yTop - yBottom, z1 - z0));
        }

        static void WallBox(CqChunkDraft d, float x0, float x1, float z0, float z1, float height = 6f)
        {
            float zc = (z0 + z1) * .5f;
            d.Walls.Add(new Bounds(new Vector3((x0 + x1) * .5f, d.Y(zc) + height * .5f - 1f, zc), new Vector3(Mathf.Abs(x1 - x0), height + 2f, z1 - z0)));
        }

        // ================================================================== primitives

        /// <summary>A tapered n-sided rod from a to b (no end caps unless cap: the far end is closed).</summary>
        static void Rod(ComicMesh m, Vector3 a, Vector3 b, float ra, float rb, Color c, int sides = 6, bool cap = false)
        {
            Vector3 axis = (b - a).normalized;
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) > .9f ? Vector3.right : Vector3.up).normalized, w = Vector3.Cross(axis, u);
            for (int i = 0; i < sides; i++)
            {
                float t0 = i * Mathf.PI * 2f / sides, t1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = u * Mathf.Cos(t0) + w * Mathf.Sin(t0), d1 = u * Mathf.Cos(t1) + w * Mathf.Sin(t1);
                K.Face(m, a + d0 * ra, a + d1 * ra, b + d1 * rb, b + d0 * rb, c, d0 + d1);
                if (cap) K.TriFacing(m, b, b + d0 * rb, b + d1 * rb, c, axis);
            }
        }

        /// <summary>A faceted foliage blob (6 x 4 by default: 36 triangles, poles as fans).</summary>
        static void Blob(ComicMesh m, Vector3 c, Vector3 size, Color col, int sides = 6, int rings = 4, float spin = 0f)
        {
            m.Surface = ComicSurface.Foliage;
            Vector3 P(int ring, int side)
            {
                float a = Mathf.PI * ring / rings, b = Mathf.PI * 2f * side / sides + spin;
                return c + Vector3.Scale(new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b)), size) * .5f;
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++)
                {
                    if (r == 0) m.Tri(P(0, s), P(1, s + 1), P(1, s), col);
                    else if (r == rings - 1) m.Tri(P(r, s), P(r, s + 1), P(rings, s), col);
                    else m.Quad(P(r, s), P(r, s + 1), P(r + 1, s + 1), P(r + 1, s), col, false);
                }
        }

        /// <summary>A two-tone crown: the blob and a smaller, lighter one on its sunny side.</summary>
        static void Crown(ComicMesh m, Vector3 c, float size, float squash, Color col, Color light, float spin = 0f, bool cheap = false)
        {
            Blob(m, c, V(size, size * squash, size), col, cheap ? 6 : 7, cheap ? 4 : 5, spin);
            if (!cheap) Blob(m, c + V(size * .2f, size * squash * .22f, -size * .08f), V(size * .62f, size * squash * .7f, size * .62f), light, 6, 4, spin + .5f);
        }

        /// <summary>Extrudes a convex planar polygon along dir: two caps and inked sides.</summary>
        static void Extrude(ComicMesh m, Vector3[] poly, Vector3 dir, Color c, Color? sides = null)
        {
            int n = poly.Length;
            Vector3 centre = Vector3.zero;
            foreach (Vector3 p in poly) centre += p;
            centre /= n;
            if (n == 4)
            {
                K.Face(m, poly[0], poly[1], poly[2], poly[3], c, -dir);
                K.Face(m, poly[0] + dir, poly[1] + dir, poly[2] + dir, poly[3] + dir, c, dir);
            }
            else
                for (int i = 1; i < n - 1; i++)
                {
                    K.TriFacing(m, poly[0], poly[i], poly[i + 1], c, -dir);
                    K.TriFacing(m, poly[0] + dir, poly[i] + dir, poly[i + 1] + dir, c, dir);
                }
            for (int i = 0; i < n; i++)
            {
                Vector3 a = poly[i], b = poly[(i + 1) % n];
                K.Face(m, a, b, b + dir, a + dir, sides ?? c, (a + b) * .5f - centre);
            }
        }

        /// <summary>A convex polygon given in lot space (along, up) at outward o0, extruded back to o1.</summary>
        static void LPoly(ComicMesh m, CqLot l, Vector2[] profile, float o0, float o1, Color c, Color? sides = null) =>
            Extrude(m, profile.Select(p => l.P(p.x, p.y, o0)).ToArray(), l.P(0, 0, o1) - l.P(0, 0, o0), c, sides);

        /// <summary>A segmental (curved) pediment on a parapet: base a0..a1 at u0, rising by rise in the middle.</summary>
        static void LArch(ComicMesh m, CqLot l, float a0, float a1, float u0, float rise, float o0, float o1, Color c)
        {
            const int n = 6;
            var pts = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                pts[i] = new Vector2(Mathf.Lerp(a0, a1, t), u0 + Mathf.Sin(t * Mathf.PI) * rise);
            }
            LPoly(m, l, pts, o0, o1, c);
        }

        /// <summary>A hipped roof: eaves rectangle (lenX by lenZ) centred on c at eaves height, ridge along the longer side.</summary>
        static void HipRoof(ComicMesh m, Vector3 c, float lenX, float lenZ, float h, Color col, float ridgeRatio = .5f)
        {
            float hx = lenX * .5f, hz = lenZ * .5f;
            Vector3 A = c + V(-hx, 0, -hz), B = c + V(hx, 0, -hz), C = c + V(hx, 0, hz), D = c + V(-hx, 0, hz);
            Vector3 r0, r1;
            if (lenZ >= lenX) { float r = Mathf.Max(0f, hz - hx * (1f / Mathf.Max(.05f, ridgeRatio)) * .5f); r0 = c + V(0, h, -r); r1 = c + V(0, h, r); }
            else { float r = Mathf.Max(0f, hx - hz * (1f / Mathf.Max(.05f, ridgeRatio)) * .5f); r0 = c + V(-r, h, 0); r1 = c + V(r, h, 0); }
            if (lenZ >= lenX)
            {
                K.TriFacing(m, A, B, r0, col, Vector3.back + Vector3.up);
                K.TriFacing(m, C, D, r1, col, Vector3.forward + Vector3.up);
                K.Face(m, B, C, r1, r0, col, Vector3.right + Vector3.up);
                K.Face(m, D, A, r0, r1, col, Vector3.left + Vector3.up);
            }
            else
            {
                K.TriFacing(m, D, A, r0, col, Vector3.left + Vector3.up);
                K.TriFacing(m, B, C, r1, col, Vector3.right + Vector3.up);
                K.Face(m, A, B, r1, r0, col, Vector3.back + Vector3.up);
                K.Face(m, C, D, r0, r1, col, Vector3.forward + Vector3.up);
            }
            K.Face(m, A, B, C, D, Color.Lerp(col, K.R.ink, .35f), Vector3.down);
        }

        /// <summary>A gable roof with the ridge along z: eaves rectangle centred on c, both slopes, two gable triangles.</summary>
        static void GableRoofZ(ComicMesh m, Vector3 c, float lenX, float lenZ, float h, Color col, Color gable)
        {
            float hx = lenX * .5f, hz = lenZ * .5f;
            Vector3 A = c + V(-hx, 0, -hz), B = c + V(hx, 0, -hz), C = c + V(hx, 0, hz), D = c + V(-hx, 0, hz), r0 = c + V(0, h, -hz), r1 = c + V(0, h, hz);
            K.Face(m, B, C, r1, r0, col, Vector3.right + Vector3.up);
            K.Face(m, D, A, r0, r1, col, Vector3.left + Vector3.up);
            K.TriFacing(m, A, B, r0, gable, Vector3.back);
            K.TriFacing(m, C, D, r1, gable, Vector3.forward);
            K.Face(m, A, B, C, D, Color.Lerp(col, K.R.ink, .35f), Vector3.down);
        }

        /// <summary>A gable roof with the ridge along x.</summary>
        static void GableRoofX(ComicMesh m, Vector3 c, float lenX, float lenZ, float h, Color col, Color gable)
        {
            float hx = lenX * .5f, hz = lenZ * .5f;
            Vector3 A = c + V(-hx, 0, -hz), B = c + V(hx, 0, -hz), C = c + V(hx, 0, hz), D = c + V(-hx, 0, hz), r0 = c + V(-hx, h, 0), r1 = c + V(hx, h, 0);
            K.Face(m, A, B, r1, r0, col, Vector3.back + Vector3.up);
            K.Face(m, C, D, r0, r1, col, Vector3.forward + Vector3.up);
            K.TriFacing(m, D, A, r0, gable, Vector3.left);
            K.TriFacing(m, B, C, r1, gable, Vector3.right);
            K.Face(m, A, B, C, D, Color.Lerp(col, K.R.ink, .35f), Vector3.down);
        }

        /// <summary>A slab from a to b (thick, width wa -> wb), top colour up, darker underneath.</summary>
        static void Slab(ComicMesh m, Vector3 a, Vector3 b, Vector3 side, float wa, float wb, float thick, Color top, Color under)
        {
            Vector3 along = (b - a).normalized, n = Vector3.Cross(along, side).normalized;
            if (n.y < 0f) n = -n;
            Vector3 A(float w, float t) => a + side * w + n * t;
            Vector3 B(float w, float t) => b + side * w + n * t;
            float ha = wa * .5f, hb = wb * .5f, ht = thick * .5f;
            Color edge = Color.Lerp(top, under, .5f);
            K.Face(m, A(-ha, ht), B(-hb, ht), B(hb, ht), A(ha, ht), top, n);
            K.Face(m, A(-ha, -ht), B(-hb, -ht), B(hb, -ht), A(ha, -ht), under, -n);
            K.Face(m, A(-ha, -ht), B(-hb, -ht), B(-hb, ht), A(-ha, ht), edge, -side);
            K.Face(m, A(ha, -ht), B(hb, -ht), B(hb, ht), A(ha, ht), edge, side);
            K.Face(m, A(-ha, -ht), A(ha, -ht), A(ha, ht), A(-ha, ht), edge, -along);
            K.Face(m, B(-hb, -ht), B(hb, -ht), B(hb, ht), B(-hb, ht), edge, along);
        }

        /// <summary>A flat quad lying on the ground (road space): x0..x1, z0..z1 at y.</summary>
        static void Ground(ComicMesh m, float x0, float x1, float z0, float z1, float y, Color c, ComicSurface surface = ComicSurface.Plain)
        {
            m.Surface = surface;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.Face(m, V(x0, y, z), V(x0, y, e), V(x1, y, e), V(x1, y, z), c, Vector3.up, false);
            }
        }

        /// <summary>Height field in flat road space (x0..x1, z0..z1), cells of about dx by dz.</summary>
        static void Terrain(ComicMesh m, float x0, float x1, float z0, float z1, float dx, float dz, Func<float, float, float> h, Func<float, float, float, Color> col, ComicSurface surface)
        {
            m.Surface = surface;
            int nx = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(x1 - x0) / dx)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / dz));
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float xa = Mathf.Lerp(x0, x1, (float)i / nx), xb = Mathf.Lerp(x0, x1, (float)(i + 1) / nx);
                    float za = Mathf.Lerp(z0, z1, (float)j / nz), zb = Mathf.Lerp(z0, z1, (float)(j + 1) / nz);
                    Vector3 a = V(xa, h(xa, za), za), b = V(xa, h(xa, zb), zb), c = V(xb, h(xb, zb), zb), dd = V(xb, h(xb, za), za);
                    K.Face(m, a, b, c, dd, col((xa + xb) * .5f, (za + zb) * .5f, (a.y + b.y + c.y + dd.y) * .25f), Vector3.up, false);
                }
        }

        /// <summary>The street's cross-section (after CbdQuayKit.StreetSurface, which is read-only and gives any road that
        /// is not the kit's grey a paving texture): red-brown asphalt, kerbs with a pale cap, paved sidewalks with joints,
        /// edge lines and a dashed double yellow centre line.</summary>
        static void Street(ComicMesh m, float length, Color road, Color pavement, Color kerbCap)
        {
            float h = K.Kerb, sw = K.Side, w = K.Half;
            Color joint = Color.Lerp(K.R.ink, pavement, .55f);
            for (float z0 = 0; z0 < length - .01f; z0 += 4f)
            {
                float z1 = Mathf.Min(length, z0 + 4f);
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(-w, 0, z0), V(-w, 0, z1), V(w, 0, z1), V(w, 0, z0), road, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    float i0 = s * w, o0 = s * (w + sw);
                    Vector3 inward = new Vector3(-s, 0, 0), outward = new Vector3(s, 0, 0);
                    m.Surface = ComicSurface.Paving;
                    K.Face(m, V(i0, h, z0), V(i0, h, z1), V(o0, h, z1), V(o0, h, z0), pavement, Vector3.up, false);
                    K.Face(m, V(i0, -.05f, z0), V(i0, -.05f, z1), V(i0, h, z1), V(i0, h, z0), pavement, inward, false);
                    K.Face(m, V(o0, -2f, z0), V(o0, -2f, z1), V(o0, h, z1), V(o0, h, z0), pavement, outward, false);
                    m.Surface = ComicSurface.Plain;
                    float c0 = s * (w + .24f), top = h + .04f;
                    K.Face(m, V(i0, top, z0), V(i0, top, z1), V(c0, top, z1), V(c0, top, z0), kerbCap, Vector3.up, false);
                    K.Face(m, V(i0, h, z0), V(i0, h, z1), V(i0, top, z1), V(i0, top, z0), kerbCap, inward, false);
                    K.Face(m, V(c0, h, z0), V(c0, h, z1), V(c0, top, z1), V(c0, top, z0), kerbCap, outward, false);
                    K.Flat(m, s * (w - .19f), s * (w - .11f), s * (w - .19f), s * (w - .11f), z0, z1, .0305f, K.R.cream);
                    for (int k = 1; k < 4; k++) K.Flat(m, s * (w + k) - .0125f, s * (w + k) + .0125f, s * (w + k) - .0125f, s * (w + k) + .0125f, z0, z1, h + .025f, joint);
                }
            }
            for (float z = 2f; z < length - .01f; z += 2f)
                for (int s = -1; s <= 1; s += 2) K.Flat(m, s * w, s * (w + sw), s * w, s * (w + sw), z - .0125f, z + .0125f, h + .025f, joint);
            for (int s = -1; s <= 1; s += 2)
                for (float z = 0f; z < length - .01f; z += 5f) K.Flat(m, s * .12f - .05f, s * .12f + .05f, s * .12f - .05f, s * .12f + .05f, z, Mathf.Min(z + 3f, length), .035f, K.R.yellow);
            foreach (float ze in new[] { 0f, length })
            {
                Vector3 normal = ze <= 0f ? Vector3.back : Vector3.forward;
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(-w, -.8f, ze), V(w, -.8f, ze), V(w, 0, ze), V(-w, 0, ze), road, normal, false);
                m.Surface = ComicSurface.Paving;
                for (int s = -1; s <= 1; s += 2) K.Face(m, V(s * w, -2f, ze), V(s * (w + sw), -2f, ze), V(s * (w + sw), h, ze), V(s * w, h, ze), pavement, normal, false);
            }
        }

        // ================================================================== pixel letters (place names only)

        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "010/101/111/101/101", ['B'] = "110/101/110/101/110", ['C'] = "011/100/100/100/011", ['D'] = "110/101/101/101/110",
            ['E'] = "111/100/110/100/111", ['F'] = "111/100/110/100/100", ['G'] = "0111/1000/1011/1001/0111", ['H'] = "101/101/111/101/101",
            ['I'] = "111/010/010/010/111", ['J'] = "001/001/001/101/010", ['K'] = "101/101/110/101/101", ['L'] = "100/100/100/100/111",
            ['M'] = "10001/11011/10101/10001/10001", ['N'] = "1001/1101/1011/1001/1001", ['O'] = "010/101/101/101/010", ['P'] = "110/101/110/100/100",
            ['Q'] = "010/101/101/111/011", ['R'] = "110/101/110/101/101", ['S'] = "011/100/010/001/110", ['T'] = "111/010/010/010/010",
            ['U'] = "101/101/101/101/111", ['V'] = "101/101/101/101/010", ['W'] = "10001/10001/10101/10101/01010", ['X'] = "101/101/010/101/101",
            ['Y'] = "101/101/010/010/010", ['Z'] = "111/001/010/100/111",
            ['0'] = "111/101/101/101/111", ['1'] = "010/110/010/010/111", ['2'] = "110/001/010/100/111", ['3'] = "110/001/010/001/110",
            ['4'] = "101/101/111/001/001", ['5'] = "111/100/110/001/110", ['6'] = "011/100/111/101/111", ['7'] = "111/001/010/010/010",
            ['8'] = "111/101/111/101/111", ['9'] = "111/101/111/001/110", ['.'] = "0/0/0/0/1", ['-'] = "000/000/111/000/000", [':'] = "0/1/0/1/0",
        };

        static float PixelWidth(string text, float px)
        {
            int cols = 0;
            foreach (char ch in text) cols += (Glyphs.TryGetValue(ch, out string g) ? g.IndexOf('/') : 2) + 1;
            return Mathf.Max(0, cols - 1) * px;
        }

        /// <summary>Pixel text centred on c, running along right, readable from the side Cross(up, right) points to. Runs of
        /// lit pixels in a row are one quad.</summary>
        static void PixelText(ComicMesh m, string text, Vector3 c, Vector3 right, Vector3 up, float px, Color colour)
        {
            m.Surface = ComicSurface.Plain;
            Vector3 n = Vector3.Cross(up, right);
            float x = -PixelWidth(text, px) * .5f, y0 = 2.5f * px;
            foreach (char ch in text)
            {
                if (!Glyphs.TryGetValue(ch, out string g)) { x += 3 * px; continue; }
                string[] rows = g.Split('/');
                for (int row = 0; row < rows.Length; row++)
                    for (int col = 0; col < rows[row].Length; col++)
                    {
                        if (rows[row][col] != '1') continue;
                        int end = col;
                        while (end + 1 < rows[row].Length && rows[row][end + 1] == '1') end++;
                        float xa = x + col * px, xb = x + (end + 1) * px, ya = y0 - (row + 1) * px, yb = ya + px;
                        K.Face(m, c + right * xa + up * ya, c + right * xb + up * ya, c + right * xb + up * yb, c + right * xa + up * yb, colour, n, false);
                        col = end;
                    }
                x += (rows[0].Length + 1) * px;
            }
        }

        /// <summary>Pixel text facing the rider (-Z).</summary>
        static void TextToRider(ComicMesh m, string text, Vector3 c, float px, Color colour) => PixelText(m, text, c, Vector3.right, Vector3.up, px, colour);

        /// <summary>Pixel text on a lot's facade, facing the road.</summary>
        static void TextOnLot(ComicMesh m, CqLot l, string text, float along, float up, float outward, float px, Color colour) =>
            PixelText(m, text, l.P(along, up, outward), new Vector3(0, 0, -l.S), Vector3.up, px, colour);

        /// <summary>Pixel text on a wall facing +X or -X (faceX = the way the wall faces).</summary>
        static void TextFacingX(ComicMesh m, string text, Vector3 c, int faceX, float px, Color colour) =>
            PixelText(m, text, c, new Vector3(0, 0, faceX), Vector3.up, px, colour);

        /// <summary>A sign board on two posts facing the rider, with a place name.</summary>
        static void NameBoard(ComicMesh m, Vector3 p, string text, float px, Color board, Color letters, float height = 2.4f)
        {
            float w = PixelWidth(text, px) + px * 8f, h = px * 9f;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -w * .38f, w * .38f }) m.Box(p + V(x, height * .5f, .06f), V(.1f, height, .1f), Galv);
            m.Box(p + V(0, height, 0), V(w, h, .07f), board);
            m.Box(p + V(0, height, .012f), V(w + .1f, h + .1f, .05f), White);
            TextToRider(m, text, p + V(0, height, -.04f), px, letters);
        }

        // ================================================================== facade parts

        /// <summary>Shopfront glazing between a0 and a1: ink frame, glass set in above a kick plate, ink mullions, a transom.</summary>
        static void Shopfront(ComicMesh f, CqLot l, float a0, float a1, float y0, float y1, float o, Color? glass = null, bool door = false)
        {
            f.Surface = ComicSurface.PaintedMetal;
            K.LQuad(f, l, a0, a1, y0, y1, o, K.R.ink);
            f.Surface = ComicSurface.Glass;
            K.LQuad(f, l, a0 + .12f, a1 - .12f, y0 + .45f, y1 - .12f, o + .02f, glass ?? ShopGlass);
            if (a1 - a0 > 2.4f && !door)
            {
                // comic glass: two slanted streaks of reflected sky
                Color glint = Color.Lerp(glass ?? ShopGlass, K.Hex("BFE0EA"), .32f);
                float gx = a0 + (a1 - a0) * .16f, top = y1 - .85f, bot = y0 + .6f;
                Vector3 n3 = new Vector3(-l.S, 0, 0);
                K.Face(f, l.P(gx, bot, o + .03f), l.P(gx + .34f, bot, o + .03f), l.P(gx + .94f, top, o + .03f), l.P(gx + .6f, top, o + .03f), glint, n3, false);
                K.Face(f, l.P(gx + .55f, bot, o + .03f), l.P(gx + .68f, bot, o + .03f), l.P(gx + 1.28f, top, o + .03f), l.P(gx + 1.15f, top, o + .03f), glint, n3, false);
            }
            f.Surface = ComicSurface.PaintedMetal;
            int n = Mathf.Max(1, Mathf.RoundToInt((a1 - a0) / 1.5f));
            for (int i = 1; i < n; i++) { float a = Mathf.Lerp(a0, a1, (float)i / n); K.LBox(f, l, a - .05f, a + .05f, y0 + .4f, y1 - .1f, o + .07f, o, K.R.ink); }
            K.LBox(f, l, a0 + .1f, a1 - .1f, y1 - .72f, y1 - .64f, o + .07f, o, K.R.ink);
            if (door)
            {
                float ac = (a0 + a1) * .5f;
                f.Surface = ComicSurface.Wood;
                K.LQuad(f, l, ac - .5f, ac + .5f, y0 + .02f, y1 - .74f, o + .03f, K.Hex("5B4636"));
                f.Surface = ComicSurface.Glass;
                K.LQuad(f, l, ac - .36f, ac + .36f, y0 + .9f, y1 - .9f, o + .04f, glass ?? ShopGlass);
            }
        }

        /// <summary>A framed window on the facade plane at o: ink frame, glass, a glazing bar, a protruding sill and head.</summary>
        static void FramedWindow(ComicMesh f, CqLot l, float a, float w, float y0, float y1, float o, Color trim, bool sill = true, bool head = false, bool cross = false)
        {
            f.Surface = ComicSurface.PaintedMetal;
            K.LQuad(f, l, a - w * .5f, a + w * .5f, y0, y1, o, K.R.ink);
            f.Surface = ComicSurface.Glass;
            K.LQuad(f, l, a - w * .5f + .1f, a + w * .5f - .1f, y0 + .1f, y1 - .1f, o + .02f, WindowGlass);
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, a - .04f, a + .04f, y0 + .1f, y1 - .1f, o + .06f, o, K.R.ink);
            if (cross) K.LBox(f, l, a - w * .5f + .1f, a + w * .5f - .1f, (y0 + y1) * .5f - .04f, (y0 + y1) * .5f + .04f, o + .06f, o, K.R.ink);
            f.Surface = ComicSurface.Concrete;
            if (sill) K.LBox(f, l, a - w * .5f - .12f, a + w * .5f + .12f, y0 - .14f, y0, o + .16f, o - .02f, trim);
            if (head) K.LBox(f, l, a - w * .5f - .12f, a + w * .5f + .12f, y1, y1 + .2f, o + .12f, o - .02f, trim);
        }

        /// <summary>A window on the lot's end facing the rider (-Z): ink frame and glass, o0..o1 across, y0..y1 up.</summary>
        static void EndWindow(ComicMesh f, CqLot l, float along, float o0, float o1, float y0, float y1)
        {
            f.Surface = ComicSurface.PaintedMetal;
            K.Face(f, l.P(along - .02f, y0, o0), l.P(along - .02f, y0, o1), l.P(along - .02f, y1, o1), l.P(along - .02f, y1, o0), K.R.ink, Vector3.back);
            f.Surface = ComicSurface.Glass;
            K.Face(f, l.P(along - .04f, y0 + .1f, o0 - .1f), l.P(along - .04f, y0 + .1f, o1 + .1f), l.P(along - .04f, y1 - .1f, o1 + .1f), l.P(along - .04f, y1 - .1f, o0 - .1f), WindowGlass, Vector3.back);
        }

        /// <summary>A boxed cantilever awning over the footpath (the Perth town-centre kind): a deep fascia box with an ink
        /// lower edge and a lighter soffit, hung from the parapet on steel tie rods. depth = how far it reaches out.</summary>
        static void BoxAwning(ComicMesh f, CqLot l, float a0, float a1, float y, float depth, Color fascia, bool rods = true, float rodTop = 1.5f)
        {
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, a0, a1, y, y + .42f, depth, -.05f, fascia);
            K.LBox(f, l, a0 - .03f, a1 + .03f, y - .07f, y + .02f, depth + .04f, depth - .16f, K.R.ink);
            K.LBox(f, l, a0 + .05f, a1 - .05f, y + .42f, y + .5f, depth - .05f, 0f, Color.Lerp(fascia, K.R.ink, .35f));
            if (!rods) return;
            int n = Mathf.Max(2, Mathf.RoundToInt((a1 - a0) / 3.2f));
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(a0 + .5f, a1 - .5f, n == 1 ? .5f : (float)i / (n - 1));
                f.Beam(l.P(a, y + .5f, depth - .35f), l.P(a, y + .5f + rodTop, .03f), .05f, K.R.ink);
            }
        }

        /// <summary>A plain coloured sign panel (no letters) with an ink frame, on the facade or the awning fascia.</summary>
        static void SignPanel(ComicMesh f, CqLot l, float a0, float a1, float y0, float y1, float o, Color c)
        {
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, a0 - .05f, a1 + .05f, y0 - .05f, y1 + .05f, o + .04f, o - .04f, K.R.ink);
            K.LBox(f, l, a0, a1, y0, y1, o + .09f, o, c);
        }

        /// <summary>A parapet above a shop: cornice, parapet wall with capped piers at both ends, and a centre piece
        /// (0 flat panel, 1 triangular pediment, 2 curved pediment, 3 stepped, 4 crenellated).</summary>
        static void Parapet(ComicMesh f, CqLot l, float a0, float a1, float y, float height, Color wall, Color trim, int centre)
        {
            float W = a1 - a0, c = (a0 + a1) * .5f;
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, a0 - .12f, a1 + .12f, y, y + .28f, .3f, -.6f, trim);
            f.Surface = ComicSurface.Plaster;
            K.LBox(f, l, a0, a1, y + .28f, y + height, .06f, -.45f, wall);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, a0 - .05f, a1 + .05f, y + height, y + height + .14f, .14f, -.52f, trim);
            foreach (float a in new[] { a0 + .35f, a1 - .35f })
            {
                K.LBox(f, l, a - .35f, a + .35f, y + .28f, y + height + .45f, .16f, -.5f, wall);
                K.LBox(f, l, a - .43f, a + .43f, y + height + .45f, y + height + .6f, .24f, -.58f, trim);
            }
            float t = y + height + .14f, hw = Mathf.Min(W * .28f, 2.4f);
            switch (centre)
            {
                case 1: K.LGable(f, l, c - hw, c + hw, t, t + hw * .55f, .12f, -.4f, trim); break;
                case 2: LArch(f, l, c - hw * .85f, c + hw * .85f, t, hw * .4f, .12f, -.4f, trim); break;
                case 3:
                    K.LBox(f, l, c - hw, c + hw, t, t + .5f, .1f, -.42f, wall);
                    K.LBox(f, l, c - hw * .55f, c + hw * .55f, t + .5f, t + 1f, .1f, -.42f, wall);
                    K.LBox(f, l, c - hw * .62f, c + hw * .62f, t + 1f, t + 1.12f, .16f, -.48f, trim);
                    break;
                case 4:
                    for (float a = a0 + 1.2f; a < a1 - 1.2f; a += 1.3f) K.LBox(f, l, a, a + .65f, t, t + .5f, .06f, -.45f, wall);
                    break;
                default: break;
            }
        }

        /// <summary>The roof seen behind a parapet: a low corrugated-iron skillion falling to the back, with ribs.</summary>
        static void SkillionRoof(ComicMesh f, CqLot l, float a0, float a1, float y, float depth, Color iron)
        {
            f.Surface = ComicSurface.Metal;
            Vector3 f0 = l.P(a0, y + .9f, -.5f), f1 = l.P(a1, y + .9f, -.5f), b0 = l.P(a0, y + .1f, -depth), b1 = l.P(a1, y + .1f, -depth);
            K.Face(f, f0, f1, b1, b0, iron, Vector3.up);
            K.Face(f, f0, f1, l.P(a1, y - .2f, -.5f), l.P(a0, y - .2f, -.5f), Color.Lerp(iron, K.R.ink, .3f), new Vector3(-l.S, 0, 0));
        }

        /// <summary>A brick chimney with a cream cap and two pots.</summary>
        static void Chimney(ComicMesh f, Vector3 p, float h, Color brick)
        {
            f.Surface = ComicSurface.Brick;
            f.Box(p + V(0, h * .5f, 0), V(.8f, h, .6f), brick);
            f.Surface = ComicSurface.Concrete;
            f.Box(p + V(0, h + .08f, 0), V(1f, .16f, .8f), TrimCream);
            f.Surface = ComicSurface.Brick;
            foreach (float x in new[] { -.2f, .2f }) f.Box(p + V(x, h + .36f, 0), V(.24f, .4f, .24f), Terracotta);
        }

        // ================================================================== trees and plants

        /// <summary>A eucalypt (river gum / marri): a tall pale bending trunk on an ink root ring, limbs forking off it, a
        /// rounded crown at the leader and at each limb end in three greens. cheap = background (fewer sides, two limbs).</summary>
        static void Gum(ComicMesh m, Vector3 p, float h, int seed, bool cheap = false)
        {
            var r = new System.Random(seed * 131 + 17);
            float lean = F(r, -.1f, .1f) * h, lean2 = F(r, -.08f, .08f) * h;
            Vector3 mid = p + V(lean * .5f, h * .4f, lean2 * .4f), top = p + V(lean, h * .66f, lean2);
            float tr = .1f + h * .015f;
            if (!cheap) { m.Surface = ComicSurface.PaintedMetal; Rod(m, p, p + V(0, .14f, 0), tr * 1.9f, tr * 1.5f, K.R.ink, 6, true); }
            m.Surface = ComicSurface.Wood;
            Rod(m, p, mid, tr, tr * .72f, GumTrunk, cheap ? 5 : 6);
            Rod(m, mid, top, tr * .72f, tr * .45f, GumTrunkShade, cheap ? 5 : 6);
            Color[] greens = { GumLeaf, GumLeafDark, Color.Lerp(GumLeaf, GumLeafLight, .5f) };
            float spin = F(r, 0f, 6.28f);
            Crown(m, top + V(0, h * .15f, 0), h * .4f, .74f, greens[seed % 2 == 0 ? 0 : 2], GumLeafLight, spin, cheap);
            int limbs = cheap ? 2 : 3;
            for (int i = 0; i < limbs; i++)
            {
                float a = spin + i * 6.283f / limbs + F(r, -.3f, .3f), reach = h * F(r, .2f, .28f);
                Vector3 from = Vector3.Lerp(mid, top, .1f + i * .28f), to = from + V(Mathf.Cos(a) * reach, h * F(r, .14f, .22f), Mathf.Sin(a) * reach);
                m.Surface = ComicSurface.Wood;
                Rod(m, from, to, tr * .42f, tr * .22f, i == 1 ? GumTrunk : GumTrunkShade, cheap ? 4 : 5);
                Crown(m, to + V(0, h * .08f, 0), h * F(r, .3f, .36f), .74f, greens[(i + 1) % 3], GumLeafLight, F(r, 0f, 3f), cheap);
            }
        }

        /// <summary>Jacaranda in November bloom: a trunk forking into three limbs under a broad umbrella crown of purple and
        /// lilac blobs, and a ring of fallen petals on the ground.</summary>
        static void Jacaranda(ComicMesh m, Vector3 p, float s, int seed, bool petals = true, bool cheap = false)
        {
            var r = new System.Random(seed * 7919 + 3);
            Vector3 fork = p + V(F(r, -.2f, .2f) * s, 2.3f * s, F(r, -.2f, .2f) * s);
            if (!cheap) { m.Surface = ComicSurface.PaintedMetal; Rod(m, p, p + V(0, .12f, 0), .5f * s, .42f * s, K.R.ink, 6, true); }
            m.Surface = ComicSurface.Wood;
            Rod(m, p, fork, .26f * s, .2f * s, JacBark, cheap ? 5 : 6);
            float spin = F(r, 0f, 6.28f);
            Blob(m, fork + V(0, 3.2f * s, 0), V(4.2f, 2.8f, 4.2f) * s, Jac1, cheap ? 6 : 7, cheap ? 4 : 5, spin);
            int n = cheap ? 2 : 3;
            for (int i = 0; i < n; i++)
            {
                float a = spin + i * 6.283f / n;
                Vector3 tip = fork + V(Mathf.Cos(a) * 1.7f * s, F(r, 2f, 2.5f) * s, Mathf.Sin(a) * 1.7f * s);
                m.Surface = ComicSurface.Wood;
                Rod(m, fork - V(0, .2f * s, 0), tip, .17f * s, .1f * s, JacBark, 5);
                Vector3 c = fork + V(Mathf.Cos(a + .5f) * 2.4f * s, F(r, 2.4f, 2.9f) * s, Mathf.Sin(a + .5f) * 2.4f * s);
                Blob(m, c, V(2.9f, 2.1f, 2.9f) * s * F(r, .9f, 1.1f), i % 2 == 0 ? Jac2 : Jac1, 6, 4, spin + i);
                if (!cheap) Blob(m, c + V(.5f, .55f, -.2f) * s, V(1.7f, 1.2f, 1.7f) * s, Jac3, 6, 4, spin);
            }
            if (!petals) return;
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 6; i++)
            {
                float a = spin + i * 1.05f, rr = F(r, .8f, 2.6f) * s, w = F(r, .7f, 1.3f) * s;
                Vector3 c = p + V(Mathf.Cos(a) * rr, .07f, Mathf.Sin(a) * rr);
                K.Face(m, c + V(-w, 0, -w * .6f), c + V(-w * .7f, 0, w * .6f), c + V(w, 0, w * .5f), c + V(w * .8f, 0, -w * .7f), i % 2 == 0 ? Jac3 : Jac2, Vector3.up, false);
            }
        }

        /// <summary>Paperbark (melaleuca): leaning cream papery trunks with tan peeling patches, layered grey-green crowns.</summary>
        static void Paperbark(ComicMesh m, Vector3 p, float h, int seed, bool cheap = false, float leanTowards = float.NaN)
        {
            var r = new System.Random(seed * 53 + 9);
            int trunks = cheap ? 1 : 2;
            for (int i = 0; i < trunks; i++)
            {
                float a = float.IsNaN(leanTowards) ? F(r, 0f, 6.28f) : leanTowards + F(r, -.5f, .5f) + i * .7f, lean = F(r, .12f, .3f);
                Vector3 top = p + V(Mathf.Cos(a) * lean * h, h * F(r, .52f, .62f), Mathf.Sin(a) * lean * h), mid = Vector3.Lerp(p, top, .5f) + V(0, h * .04f, 0);
                m.Surface = ComicSurface.Wood;
                float tr = .12f + .017f * h;
                Rod(m, p + V(0, -.3f, 0), mid, tr, tr * .8f, PaperTrunk, cheap ? 5 : 6);
                Rod(m, mid, top, tr * .8f, tr * .5f, i == 0 ? PaperTrunk : Color.Lerp(PaperTrunk, PaperPatch, .4f), cheap ? 5 : 6);
                if (!cheap)
                {
                    Quaternion q = Quaternion.FromToRotation(Vector3.up, mid - p);
                    m.Box(Vector3.Lerp(p, mid, .55f) + q * V(tr * .85f, 0, 0), V(.06f, F(r, .5f, .9f), .2f), PaperPatch, q);
                    m.Box(Vector3.Lerp(p, mid, .25f) + q * V(-tr * .7f, 0, .08f), V(.06f, .45f, .16f), PaperPatch, q * Quaternion.Euler(0, 120f, 0));
                }
                Crown(m, top + V(0, h * .12f, 0), h * (cheap ? .5f : .44f), .78f, PaperLeaf, PaperLeafLight, F(r, 0f, 3f), cheap);
                if (!cheap) Crown(m, top + V(F(r, -.2f, .2f) * h, h * .26f, F(r, -.15f, .15f) * h), h * .3f, .75f, PaperLeafDark, PaperLeafLight, F(r, 0f, 3f), true);
            }
        }

        /// <summary>A big fig (Ashfield Reserve): a massive short buttressed trunk, heavy limbs, a very wide dark dome.</summary>
        static void Fig(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = new System.Random(seed * 71 + 5);
            m.Surface = ComicSurface.Wood;
            Rod(m, p, p + V(0, 3.2f * s, 0), 1.25f * s, .8f * s, FigBark, 7);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 1.2566f + F(r, 0f, .5f);
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                Slab(m, p + dir * 2.3f * s + V(0, .05f, 0), p + dir * .6f * s + V(0, 1.5f * s, 0), Vector3.Cross(Vector3.up, dir), .25f * s, .35f * s, .5f * s, FigBark, Color.Lerp(FigBark, K.R.ink, .3f));
            }
            Vector3 c = p + V(0, 3.1f * s, 0);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 1.571f + F(r, 0f, .8f);
                Vector3 tip = c + V(Mathf.Cos(a) * 3.4f * s, F(r, 1.6f, 2.4f) * s, Mathf.Sin(a) * 3.4f * s);
                m.Surface = ComicSurface.Wood;
                Rod(m, c, tip, .5f * s, .28f * s, FigBark, 5);
                Crown(m, tip + V(Mathf.Cos(a) * 1.2f * s, 1.1f * s, Mathf.Sin(a) * 1.2f * s), 6.4f * s * F(r, .9f, 1.1f), .62f, i % 2 == 0 ? FigLeaf : Color.Lerp(FigLeaf, FigLeafLight, .4f), FigLeafLight, a);
            }
            Crown(m, c + V(0, 4.4f * s, 0), 7.6f * s, .6f, FigLeaf, FigLeafLight, F(r, 0f, 3f));
        }

        /// <summary>Grass tree (xanthorrhoea): a stubby black trunk, a spiky green skirt, a tall flower spear.</summary>
        static void GrassTree(ComicMesh m, Vector3 p, float s, int seed)
        {
            m.Surface = ComicSurface.Wood;
            Rod(m, p, p + V(0, .8f * s, 0), .2f * s, .17f * s, K.Hex("2E2A26"), 6);
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < 9; i++)
            {
                float a = i * .698f + seed, tilt = i % 2 == 0 ? .5f : .95f;
                Vector3 dir = V(Mathf.Cos(a) * Mathf.Sin(tilt), Mathf.Cos(tilt), Mathf.Sin(a) * Mathf.Sin(tilt));
                Rod(m, p + V(0, .75f * s, 0), p + V(0, .75f * s, 0) + dir * 1.05f * s, .11f * s, .02f, i % 2 == 0 ? K.Hex("7FAE56") : K.Hex("A3C870"), 4);
            }
            m.Surface = ComicSurface.Wood;
            Rod(m, p + V(0, .8f * s, 0), p + V(.05f * s, 2.5f * s, 0), .05f * s, .04f * s, K.Hex("5B4A38"), 4, true);
        }

        /// <summary>A sedge / rush tussock: a tuft of thin leaning spikes in two greens.</summary>
        static void Sedge(ComicMesh m, Vector3 p, float s, int seed, Color? a0 = null, Color? a1 = null, int blades = 6)
        {
            var r = new System.Random(seed * 37 + 1);
            Color c0 = a0 ?? K.Hex("7FA650"), c1 = a1 ?? K.Hex("9DBE62");
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < blades; i++)
            {
                float a = F(r, 0f, 6.28f), tilt = F(r, .05f, .5f), len = s * F(r, .7f, 1.3f);
                Vector3 dir = V(Mathf.Cos(a) * Mathf.Sin(tilt), Mathf.Cos(tilt), Mathf.Sin(a) * Mathf.Sin(tilt));
                Vector3 b = p + V(Mathf.Cos(a) * .12f * s, 0, Mathf.Sin(a) * .12f * s);
                Rod(m, b, b + dir * len, .1f * s, .02f, i % 2 == 0 ? c0 : c1, 4);
            }
        }

        /// <summary>A bulrush clump (Typha): tall blades with brown seed heads.</summary>
        static void Bulrush(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = new System.Random(seed * 91 + 2);
            Sedge(m, p, s * 1.5f, seed, K.Hex("6F9A48"), K.Hex("8FB45C"), 5);
            for (int i = 0; i < 3; i++)
            {
                Vector3 b = p + V(F(r, -.25f, .25f) * s, 0, F(r, -.25f, .25f) * s), t = b + V(F(r, -.15f, .15f) * s, s * F(r, 1.5f, 2f), F(r, -.15f, .15f) * s);
                m.Surface = ComicSurface.Foliage;
                Rod(m, b, t, .025f * s, .02f * s, K.Hex("7C9A4C"), 3);
                m.Surface = ComicSurface.Fabric;
                Rod(m, Vector3.Lerp(b, t, .78f), Vector3.Lerp(b, t, .97f), .07f * s, .07f * s, K.Hex("6B4630"), 4, true);
            }
        }

        /// <summary>A dead white tree standing in the flats: a bare trunk and a few bare branches.</summary>
        static void DeadTree(ComicMesh m, Vector3 p, float h, int seed)
        {
            var r = new System.Random(seed * 19 + 4);
            Color c = K.Hex("E6E0D2"), c2 = K.Hex("CFC7B6");
            m.Surface = ComicSurface.Wood;
            Vector3 top = p + V(F(r, -.1f, .1f) * h, h, F(r, -.1f, .1f) * h);
            Rod(m, p + V(0, -.3f, 0), top, .16f + h * .012f, .05f, c, 5);
            for (int i = 0; i < 4; i++)
            {
                float a = F(r, 0f, 6.28f), t = F(r, .45f, .85f);
                Vector3 from = Vector3.Lerp(p, top, t), to = from + V(Mathf.Cos(a) * h * .22f, h * F(r, .08f, .2f), Mathf.Sin(a) * h * .22f);
                Rod(m, from, to, .07f, .025f, i % 2 == 0 ? c2 : c, 4);
            }
        }

        /// <summary>A low rounded shrub (two-tone).</summary>
        static void Bush(ComicMesh m, Vector3 p, float s, Color? col = null, Color? light = null)
        {
            Blob(m, p + V(0, s * .3f, 0), V(s * 1.3f, s * .75f, s * 1.2f), col ?? Shrub, 6, 4);
            Blob(m, p + V(s * .2f, s * .5f, -s * .1f), V(s * .75f, s * .5f, s * .7f), light ?? ShrubLight, 6, 4, 1f);
        }

        /// <summary>A small round street tree in a tree pit (evergreen, clipped: the town-centre kind).</summary>
        static void StreetTree(ComicMesh m, List<DressingObstacle> collisions, Vector3 p, float s, int seed, Color? leaf = null, Color? light = null)
        {
            K.TreePit(m, p, 1.3f);
            m.Surface = ComicSurface.Wood;
            Rod(m, p + V(0, .1f, 0), p + V(0, 2.6f * s, 0), .17f * s, .12f * s, K.Hex("7A6A58"), 6);
            Color g = leaf ?? K.Hex("4F8A40"), gl = light ?? K.Hex("76AC55");
            Crown(m, p + V(0, 3.7f * s, 0), 3.4f * s, .8f, g, gl, seed);
            Blob(m, p + V(-.9f * s, 3.2f * s, .6f * s), V(2f, 1.5f, 2f) * s, Color.Lerp(g, K.R.ink, .15f), 6, 4, seed + 1f);
            K.Thin(collisions, "StreetTree", p, 2.6f * s);
        }

        /// <summary>Background Norfolk pine: thin trunk and four stacked tiers.</summary>
        static void CheapPine(ComicMesh m, Vector3 p, float h, Color? tint = null)
        {
            Color dark = tint ?? K.Hex("2C5A36"), mid = tint.HasValue ? Color.Lerp(tint.Value, Color.white, .08f) : K.Hex("3B6F42");
            m.Surface = ComicSurface.Wood;
            Rod(m, p, p + V(0, h, 0), .22f + h * .008f, .06f, tint.HasValue ? Color.Lerp(K.Hex("6E5A47"), tint.Value, .5f) : K.Hex("6E5A47"), 5);
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < 4; i++)
            {
                float t = i / 3f, y = Mathf.Lerp(h * .38f, h * .9f, t), rr = Mathf.Lerp(h * .22f, h * .08f, t);
                m.Cylinder(p + V(0, y, 0), rr, h * .07f, i % 2 == 0 ? dark : mid, 6, .55f);
            }
        }

        /// <summary>A clipped hedge in flat road space beside the sidewalk (add to a sheared mesh).</summary>
        static void Hedge(ComicMesh m, int s, float z0, float z1, float d0, float d1, float h, Color? c = null)
        {
            m.Surface = ComicSurface.Foliage;
            K.SideBox(m, s, z0, z1, K.Kerb - .05f, K.Kerb + h, d0, d1, c ?? HedgeGreen);
        }

        // ================================================================== street furniture

        /// <summary>The town-centre lamp: a black post with two swan-neck arms and lanterns, and a pair of banners.</summary>
        static void SwanNeckLamp(ComicMesh m, List<DressingObstacle> collisions, Vector3 p, Color banner, bool banners = true)
        {
            Color black = K.Hex("22262B");
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, p, p + V(0, .5f, 0), .2f, .12f, black, 6);
            Rod(m, p + V(0, .5f, 0), p + V(0, 6.2f, 0), .09f, .07f, black, 6, true);
            foreach (int s in new[] { -1, 1 })
            {
                m.Beam(p + V(0, 5.5f, 0), p + V(0, 6f, s * .7f), .07f, black);
                m.Beam(p + V(0, 6f, s * .7f), p + V(0, 5.75f, s * 1.15f), .07f, black);
                m.Surface = ComicSurface.Glass;
                Rod(m, p + V(0, 5.3f, s * 1.15f), p + V(0, 5.7f, s * 1.15f), .2f, .12f, K.R.cream, 6, true);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(p + V(0, 5.74f, s * 1.15f), V(.34f, .08f, .34f), black);
                if (!banners) continue;
                m.Box(p + V(0, 4.7f, s * .45f), V(.05f, .05f, .8f), black);
                m.Surface = ComicSurface.Fabric;
                m.Box(p + V(0, 3.95f, s * .48f), V(.04f, 1.4f, .66f), s < 0 ? banner : Color.Lerp(banner, White, .25f));
                m.Box(p + V(0, 4.2f, s * .48f), V(.05f, .3f, .5f), White);
                m.Surface = ComicSurface.PaintedMetal;
            }
            K.Thin(collisions, "Lamp", p, 4f, .3f);
        }

        /// <summary>A black ball-top bollard.</summary>
        static void Bollard(ComicMesh m, Vector3 p)
        {
            Color black = K.Hex("22262B");
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, p, p + V(0, .85f, 0), .11f, .1f, black, 6);
            m.Cylinder(p + V(0, .7f, 0), .125f, .06f, K.R.cream, 6);
            Blob(m, p + V(0, .95f, 0), V(.27f, .27f, .27f), black, 6, 3);
            m.Surface = ComicSurface.PaintedMetal;
        }

        /// <summary>A park bench: timber slats on charcoal frames. Faces the way yaw turns +Z... the seat front is at -Z for yaw 0.</summary>
        static void Bench(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.Wood;
            m.Box(p + q * V(0, .45f, 0), V(1.8f, .08f, .45f), Timber, q);
            m.Box(p + q * V(0, .8f, .22f), V(1.8f, .35f, .07f), Timber, q);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.75f, .75f }) m.Box(p + q * V(x, .4f, .05f), V(.08f, .8f, .5f), Charcoal, q);
        }

        /// <summary>A bin: perforated stainless drum with a charcoal hood.</summary>
        static void Bin(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Metal;
            m.Cylinder(p + V(0, .5f, 0), .32f, .9f, Stainless, 8);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .05f, 0), .36f, .1f, Charcoal, 8);
            m.Cylinder(p + V(0, 1.02f, 0), .37f, .16f, Charcoal, 8, .8f);
            m.Cylinder(p + V(0, .66f, 0), .33f, .06f, StainlessDark, 8);
        }

        /// <summary>A timber power pole with a cross arm, insulators and a street light arm (the suburbs' skyline).</summary>
        static void PowerPole(ComicMesh m, Vector3 p, float h = 9f, bool light = true)
        {
            m.Surface = ComicSurface.Wood;
            Rod(m, p, p + V(0, h, 0), .16f, .11f, K.Hex("6F5B49"), 5, true);
            m.Box(p + V(0, h - .6f, 0), V(2.2f, .12f, .12f), K.Hex("6F5B49"));
            m.Surface = ComicSurface.Plastic;
            foreach (float x in new[] { -1f, -.4f, .4f, 1f }) m.Box(p + V(x, h - .45f, 0), V(.1f, .18f, .1f), K.Hex("D9D4C4"));
            if (!light) return;
            m.Surface = ComicSurface.PaintedMetal;
            m.Beam(p + V(0, h - 1.6f, 0), p + V(1.6f, h - 1.1f, 0), .07f, Galv);
            m.Box(p + V(1.75f, h - 1.12f, 0), V(.5f, .1f, .22f), GalvDark);
        }

        /// <summary>Power lines between poles: three thin wires (segments of 8 m so the curl bends them).</summary>
        static void PowerLines(ComicMesh m, float x, float y, float z0, float z1)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float dx in new[] { -1f, 0f, 1f })
                for (float z = z0; z < z1 - .01f; z += 8f)
                    m.Box(V(x + dx, y, (z + Mathf.Min(z1, z + 8f)) * .5f), V(.03f, .03f, Mathf.Min(z1, z + 8f) - z), K.Hex("3A3F45"));
        }

        /// <summary>A parked car (generic: no badges): body, cabin with dark glass, ink wheels and bumpers.</summary>
        static void ParkedCar(ComicMesh m, Vector3 p, float yaw, Color c)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + q * V(0, .62f, 0), V(1.8f, .62f, 4.2f), c, q);
            m.Box(p + q * V(0, .28f, 0), V(1.84f, .2f, 4.26f), K.R.ink, q);
            m.Surface = ComicSurface.Glass;
            m.Box(p + q * V(0, 1.2f, -.2f), V(1.6f, .56f, 2.2f), K.Hex("2D4B59"), q);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + q * V(0, 1.5f, -.2f), V(1.64f, .08f, 2.1f), c, q);
            m.Surface = ComicSurface.Rubber;
            foreach (float x in new[] { -.92f, .92f }) foreach (float z in new[] { -1.35f, 1.35f }) m.Box(p + q * V(x, .32f, z), V(.2f, .62f, .62f), K.R.ink, q);
        }

        /// <summary>A city bus standing at its stop (no branding): silver-grey body, green lower front and skirt, a dark
        /// windscreen surround and window band, doors, ink wheels, a roof pod. Front at +Z for yaw 0.</summary>
        static void Bus(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            Color body = K.Hex("C9CED3"), dark = K.Hex("2B3138");
            void B(Vector3 c, Vector3 size, Color col, ComicSurface s = ComicSurface.PaintedMetal) { m.Surface = s; m.Box(p + q * c, size, col, q); }
            B(V(0, 1.75f, 0), V(2.5f, 2.6f, 11.6f), body);
            B(V(0, .7f, 0), V(2.54f, .5f, 11.64f), TransGreen);
            B(V(0, 2.2f, 0), V(2.56f, 1f, 10.6f), dark, ComicSurface.Glass);
            B(V(0, 2.1f, 5.78f), V(2.3f, 1.5f, .1f), dark, ComicSurface.Glass);
            B(V(0, 1f, 5.8f), V(2.52f, .7f, .08f), TransGreen);
            B(V(0, 3.02f, 5.7f), V(1.6f, .3f, .2f), dark);
            B(V(0, 3.15f, -1f), V(1.9f, .26f, 4f), K.Hex("AAB1B8"));
            foreach (float z in new[] { 4.2f, -.4f }) B(V(1.27f, 1.55f, z), V(.06f, 2.3f, 1.2f), K.Hex("8C96A0"));
            foreach (float x in new[] { -.7f, .7f }) B(V(x, .8f, 5.83f), V(.4f, .2f, .06f), K.R.cream);
            foreach (float x in new[] { -1.18f, 1.18f }) foreach (float z in new[] { 3.4f, -3.6f }) B(V(x, .5f, z), V(.3f, 1f, 1f), K.R.ink, ComicSurface.Rubber);
        }

        // ================================================================== the railway

        /// <summary>Track bed in flat road space (add to d.Road): ballast shoulder from x0 to x1 (x1 further from the road),
        /// sleepers and two steel rails for each track centre in tracks. y = top of the ballast.</summary>
        static void TrackBed(ComicMesh m, float x0, float x1, float z0, float z1, float y, params float[] tracks)
        {
            float lo = Mathf.Min(x0, x1), hi = Mathf.Max(x0, x1);
            m.Surface = ComicSurface.Concrete;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.Face(m, V(lo, y, z), V(lo, y, e), V(hi, y, e), V(hi, y, z), Ballast, Vector3.up, false);
                K.Face(m, V(hi, y - 1.6f, z), V(hi, y - 1.6f, e), V(hi, y, e), V(hi, y, z), BallastDark, Vector3.right, false);
                K.Face(m, V(lo, y - 1.6f, z), V(lo, y - 1.6f, e), V(lo, y, e), V(lo, y, z), BallastDark, Vector3.left, false);
            }
            foreach (float tx in tracks)
            {
                m.Surface = ComicSurface.Concrete;
                for (float z = z0 + .4f; z < z1; z += 1.3f)
                    K.Face(m, V(tx - 1.15f, y + .03f, z), V(tx - 1.15f, y + .03f, z + .3f), V(tx + 1.15f, y + .03f, z + .3f), V(tx + 1.15f, y + .03f, z), Sleeper, Vector3.up, false);
                m.Surface = ComicSurface.Metal;
                foreach (float rx in new[] { tx - .72f, tx + .72f })
                    for (float z = z0; z < z1 - .01f; z += 4f)
                    {
                        float e = Mathf.Min(z1, z + 4f);
                        K.Face(m, V(rx - .05f, y + .16f, z), V(rx - .05f, y + .16f, e), V(rx + .05f, y + .16f, e), V(rx + .05f, y + .16f, z), K.Hex("9CA3AA"), Vector3.up, false);
                        float side = rx > 0 ? -1f : 1f; // the face towards the road
                        K.Face(m, V(rx + side * .05f, y + .03f, z), V(rx + side * .05f, y + .03f, e), V(rx + side * .05f, y + .16f, e), V(rx + side * .05f, y + .16f, z), RailSteel, new Vector3(side, 0, 0), false);
                    }
            }
        }

        /// <summary>Overhead wires above each track: contact wire and messenger (8 m segments).</summary>
        static void Catenary(ComicMesh m, float z0, float z1, float y, params float[] tracks)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float tx in tracks)
                for (float z = z0; z < z1 - .01f; z += 8f)
                {
                    float e = Mathf.Min(z1, z + 8f);
                    m.Box(V(tx, y + 5.3f, (z + e) * .5f), V(.04f, .04f, e - z), K.Hex("3A3F45"));
                    m.Box(V(tx, y + 6.5f, (z + e) * .5f), V(.04f, .04f, e - z), K.Hex("4A5057"));
                }
        }

        /// <summary>A galvanised portal gantry across the tracks between xa and xb at z: two H masts, a lattice boom,
        /// a drop tube and registration arm over each track.</summary>
        static void Gantry(ComicMesh m, float xa, float xb, float y, float z, params float[] tracks)
        {
            m.Surface = ComicSurface.Metal;
            foreach (float x in new[] { xa, xb })
            {
                m.Box(V(x, y + 3.9f, z), V(.26f, 7.8f, .3f), Galv);
                m.Box(V(x, y + .25f, z), V(.6f, .5f, .6f), K.Hex("C9C5BA"));
            }
            float lo = Mathf.Min(xa, xb), hi = Mathf.Max(xa, xb), w = hi - lo;
            m.Box(V((lo + hi) * .5f, y + 7.7f, z), V(w + .5f, .14f, .2f), Galv);
            m.Box(V((lo + hi) * .5f, y + 7.1f, z), V(w + .5f, .14f, .2f), Galv);
            int n = Mathf.Max(2, Mathf.RoundToInt(w / 1.6f));
            for (int i = 0; i < n; i++)
            {
                float x0 = lo + w * i / n, x1 = lo + w * (i + 1) / n;
                m.Beam(V(x0, y + (i % 2 == 0 ? 7.1f : 7.7f), z), V(x1, y + (i % 2 == 0 ? 7.7f : 7.1f), z), .07f, GalvDark);
            }
            foreach (float tx in tracks)
            {
                m.Box(V(tx + .5f, y + 6.3f, z), V(.09f, 1.5f, .09f), Galv);
                m.Beam(V(tx + .5f, y + 5.6f, z), V(tx - .3f, y + 5.36f, z), .06f, GalvDark);
                m.Surface = ComicSurface.Plastic;
                m.Box(V(tx + .5f, y + 6.6f, z), V(.16f, .3f, .16f), K.Hex("7A4A3A"));
                m.Surface = ComicSurface.Metal;
            }
        }

        /// <summary>A cantilever catenary mast at x, with an arm reaching towards arm (-1 / +1) over one track.</summary>
        static void Mast(ComicMesh m, float x, float y, float z, int arm, float reach = 2.6f)
        {
            m.Surface = ComicSurface.Metal;
            m.Box(V(x, y + 3.7f, z), V(.24f, 7.4f, .28f), Galv);
            m.Box(V(x, y + .22f, z), V(.55f, .44f, .55f), K.Hex("C9C5BA"));
            m.Beam(V(x, y + 6.6f, z), V(x + arm * reach, y + 6.5f, z), .08f, Galv);
            m.Beam(V(x, y + 5f, z), V(x + arm * reach, y + 5.4f, z), .07f, GalvDark);
            m.Beam(V(x, y + 7.2f, z), V(x + arm * reach * .9f, y + 6.55f, z), .05f, GalvDark);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(x + arm * .5f, y + 6.6f, z), V(.3f, .16f, .16f), K.Hex("7A4A3A"));
        }

        /// <summary>A Transperth-style electric train standing on the track at x (no branding), nose at p.z, running to +Z.
        /// series 0 = the older two-car set (fluted stainless, flat white cab, green and yellow cab band);
        /// series 1 = the newer three-car set (smooth stainless, charcoal window band, a green band at the roofline,
        /// yellow door frames, a raked white-and-green nose). y = rail level. Faces drawn for a viewer on side viewSide.</summary>
        static void Train(ComicMesh m, Vector3 p, int cars, int series, int viewSide = 1, bool headCab = true, bool tailCab = true, float len = 19f)
        {
            const float gap = .5f, bw = 2.9f;
            for (int i = 0; i < cars; i++)
            {
                Vector3 c = p + V(0, 0, i * (len + gap) + len * .5f);
                bool first = i == 0 && headCab, last = i == cars - 1 && tailCab;
                m.Surface = ComicSurface.Metal;
                m.Box(c + V(0, 2.35f, 0), V(bw, 2.9f, len), Stainless);
                m.Box(c + V(0, 3.9f, 0), V(bw - .5f, .3f, len - .6f), StainlessDark);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(c + V(0, .62f, 0), V(bw - .3f, .6f, len - 1f), CharcoalDark);
                foreach (float bz in new[] { -len * .32f, len * .32f }) m.Box(c + V(0, .4f, bz), V(bw - .1f, .7f, 3.2f), K.R.ink);
                float fx = viewSide * (bw * .5f + .02f);
                Vector3 n = new Vector3(viewSide, 0, 0);
                // window band, doors, flutes or roofline band on the side that is seen
                m.Surface = ComicSurface.Glass;
                K.Face(m, c + V(fx, 2.25f, -len * .5f + 1f), c + V(fx, 2.25f, len * .5f - 1f), c + V(fx, 3.2f, len * .5f - 1f), c + V(fx, 3.2f, -len * .5f + 1f), TrainBand, n);
                m.Surface = ComicSurface.PaintedMetal;
                foreach (float dz in new[] { -len * .3f, 0f, len * .3f })
                {
                    if (series == 0 && dz == 0f) continue;
                    K.Face(m, c + V(fx + .02f, 1f, dz - .8f), c + V(fx + .02f, 1f, dz + .8f), c + V(fx + .02f, 3.3f, dz + .8f), c + V(fx + .02f, 3.3f, dz - .8f), series == 1 ? DoorYellow : StainlessDark, n);
                    K.Face(m, c + V(fx + .04f, 1.05f, dz - .68f), c + V(fx + .04f, 1.05f, dz + .68f), c + V(fx + .04f, 3.18f, dz + .68f), c + V(fx + .04f, 3.18f, dz - .68f), series == 1 ? Stainless : K.Hex("B9C0C6"), n);
                    K.Face(m, c + V(fx + .06f, 2.1f, dz - .5f), c + V(fx + .06f, 2.1f, dz + .5f), c + V(fx + .06f, 3.05f, dz + .5f), c + V(fx + .06f, 3.05f, dz - .5f), TrainBand, n);
                }
                if (series == 1)
                    K.Face(m, c + V(fx + .02f, 3.42f, -len * .5f + .3f), c + V(fx + .02f, 3.42f, len * .5f - .3f), c + V(fx + .02f, 3.74f, len * .5f - .3f), c + V(fx + .02f, 3.74f, -len * .5f + .3f), TransGreen, n);
                else
                {
                    foreach (float fy in new[] { 1.25f, 1.5f, 1.75f })
                        K.Face(m, c + V(fx + .02f, fy, -len * .5f + .4f), c + V(fx + .02f, fy, len * .5f - .4f), c + V(fx + .02f, fy + .06f, len * .5f - .4f), c + V(fx + .02f, fy + .06f, -len * .5f + .4f), StainlessDark, n, false);
                    K.Face(m, c + V(fx + .02f, .98f, -len * .5f + .3f), c + V(fx + .02f, .98f, len * .5f - .3f), c + V(fx + .02f, 1.14f, len * .5f - .3f), c + V(fx + .02f, 1.14f, -len * .5f + .3f), TransGreen, n, false);
                }
                // cab ends
                foreach (int e in new[] { -1, 1 })
                {
                    if (e < 0 && !first || e > 0 && !last) continue;
                    float z = e * len * .5f;
                    Vector3 fn = new Vector3(0, 0, e);
                    if (series == 1)
                    {
                        // raked nose: a wedge in white with a green lower half and a dark windscreen
                        Vector3[] prof = { c + V(-bw * .5f, .95f, z), c + V(-bw * .5f, .95f, z + e * .9f), c + V(-bw * .5f, 2.2f, z + e * .7f), c + V(-bw * .5f, 3.8f, z) };
                        Extrude(m, prof, V(bw, 0, 0), White);
                        K.Face(m, c + V(-bw * .5f + .15f, 2.3f, z + e * .7f), c + V(bw * .5f - .15f, 2.3f, z + e * .7f), c + V(bw * .5f - .15f, 3.5f, z + e * .16f), c + V(-bw * .5f + .15f, 3.5f, z + e * .16f), TrainBand, fn + Vector3.up * .4f);
                        K.Face(m, c + V(-bw * .5f - .01f, .96f, z + e * .92f), c + V(bw * .5f + .01f, .96f, z + e * .92f), c + V(bw * .5f + .01f, 1.7f, z + e * .8f), c + V(-bw * .5f - .01f, 1.7f, z + e * .8f), TransGreen, fn);
                    }
                    else
                    {
                        m.Box(c + V(0, 2.35f, z + e * .12f), V(bw, 2.9f, .24f), White);
                        K.Face(m, c + V(-bw * .5f + .2f, 2.3f, z + e * .25f), c + V(bw * .5f - .2f, 2.3f, z + e * .25f), c + V(bw * .5f - .2f, 3.45f, z + e * .25f), c + V(-bw * .5f + .2f, 3.45f, z + e * .25f), TrainBand, fn);
                        K.Face(m, c + V(-bw * .5f, 1.5f, z + e * .25f), c + V(bw * .5f, 1.5f, z + e * .25f), c + V(bw * .5f, 1.95f, z + e * .25f), c + V(-bw * .5f, 1.95f, z + e * .25f), TransGreen, fn);
                        K.Face(m, c + V(-bw * .5f, 1.3f, z + e * .25f), c + V(bw * .5f, 1.3f, z + e * .25f), c + V(bw * .5f, 1.5f, z + e * .25f), c + V(-bw * .5f, 1.5f, z + e * .25f), DoorYellow, fn);
                    }
                    foreach (float lx in new[] { -1f, 1f }) m.Box(c + V(lx, 1.15f, z + e * (series == 1 ? .88f : .27f)), V(.3f, .16f, .06f), K.R.cream);
                }
                // pantograph on the first car
                if (i == 0)
                {
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Beam(c + V(0, 4.05f, -1.2f), c + V(0, 4.7f, 0f), .07f, K.R.ink);
                    m.Beam(c + V(0, 4.7f, 0f), c + V(0, 5.25f, -1f), .07f, K.R.ink);
                    m.Box(c + V(0, 5.27f, -1f), V(1.6f, .06f, .25f), K.R.ink);
                }
            }
        }

        /// <summary>A fence in flat road space along z at x: posts every 2.5 m, top and bottom rails and (bars) upright bars
        /// every 0.6 m: the black palisade along the railway; without bars, a mesh panel.</summary>
        static void RailFence(ComicMesh m, float x, float z0, float z1, float y, float h, Color c, bool bars, Color? mesh = null)
        {
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = z0; z <= z1 + .01f; z += 2.5f) m.Box(V(x, y + h * .5f, Mathf.Min(z, z1 - .04f)), V(.09f, h + .1f, .09f), c);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                m.Box(V(x, y + h - .12f, (z + e) * .5f), V(.05f, .06f, e - z), c);
                m.Box(V(x, y + .18f, (z + e) * .5f), V(.05f, .06f, e - z), c);
                if (mesh.HasValue) { m.Surface = ComicSurface.Fabric; m.Box(V(x, y + h * .5f, (z + e) * .5f), V(.025f, h - .36f, e - z - .1f), mesh.Value); m.Surface = ComicSurface.PaintedMetal; }
            }
            if (!bars) return;
            for (float z = z0 + .3f; z < z1; z += .6f)
            {
                if (Mathf.Abs(Mathf.Repeat(z - z0, 2.5f)) < .12f) continue;
                K.Face(m, V(x - .03f, y + .1f, z - .03f), V(x - .03f, y + .1f, z + .03f), V(x - .03f, y + h + .08f, z + .03f), V(x - .03f, y + h + .08f, z - .03f), c, Vector3.left, false);
                K.Face(m, V(x + .03f, y + .1f, z - .03f), V(x + .03f, y + .1f, z + .03f), V(x + .03f, y + h + .08f, z + .03f), V(x + .03f, y + h + .08f, z - .03f), c, Vector3.right, false);
                K.Face(m, V(x - .03f, y + .1f, z - .03f), V(x + .03f, y + .1f, z - .03f), V(x + .03f, y + h + .08f, z - .03f), V(x - .03f, y + h + .08f, z - .03f), c, Vector3.back, false);
            }
        }

        // ================================================================== far scenery helpers

        // A flat strip split every 20 m along z (the GPU curl bends per vertex).
        static void FarStrip(ComicMesh m, float x0, float x1, float z0, float z1, float y, Color c)
        {
            m.Surface = ComicSurface.Plain;
            for (float z = z0; z < z1 - .01f; z += 20f)
            {
                float e = Mathf.Min(z1, z + 20f);
                K.Face(m, V(x0, y, z), V(x0, y, e), V(x1, y, e), V(x1, y, z), c, Vector3.up, false);
            }
        }

        // Aerial perspective: further = closer to the haze colour.
        static Color Haze(Color c, float dist, Color haze) => Color.Lerp(c, haze, Mathf.Clamp01((Mathf.Abs(dist) - 60f) / 600f) * .7f);

        /// <summary>A long landmass parallel to the road on side s (near edge at |x| = xNear): a pale foot, a slope up to a
        /// wavy ridge facing the road, the top falling away behind; split every 15 m so the curl bends it.</summary>
        static void Ridge(ComicMesh m, int s, float xNear, float depth, float z0, float z1, float baseY, Func<float, float> height, Color foot, Color slope, Color top, Color haze, float hazeAmount)
        {
            Color sh = Color.Lerp(foot, haze, hazeAmount), sl = Color.Lerp(slope, haze, hazeAmount), tp = Color.Lerp(top, haze, hazeAmount);
            Vector3 toRoad = new Vector3(-s, 0, 0);
            for (float z = z0; z < z1 - .01f; z += 15f)
            {
                float za = z, zb = Mathf.Min(z1, z + 15f), ha = height(za), hb = height(zb);
                float xs = s * xNear, xm = s * (xNear + 5f), xr = s * (xNear + depth * .35f), xf = s * (xNear + depth);
                m.Surface = ComicSurface.Concrete;
                K.Face(m, V(xs, baseY, za), V(xs, baseY, zb), V(xm, baseY + 1.2f, zb), V(xm, baseY + 1.2f, za), sh, (toRoad + Vector3.up * 3f).normalized, false);
                m.Surface = ComicSurface.Foliage;
                K.Face(m, V(xm, baseY + 1.2f, za), V(xm, baseY + 1.2f, zb), V(xr, baseY + hb, zb), V(xr, baseY + ha, za), sl, (toRoad + Vector3.up).normalized, false);
                K.Face(m, V(xr, baseY + ha, za), V(xr, baseY + hb, zb), V(xf, baseY + hb * .5f, zb), V(xf, baseY + ha * .5f, za), tp, Vector3.up, false);
            }
        }

        /// <summary>A far house: a wall box and a hipped roof (no outline: for Deep and the far scenery).</summary>
        static void FarHouse(ComicMesh m, Vector3 p, float w, float d, float h, Color wall, Color roof)
        {
            m.Surface = ComicSurface.Plaster;
            m.Box(p + V(0, h * .5f - 1f, 0), V(w, h + 2f, d), wall);
            m.Surface = ComicSurface.Brick;
            HipRoof(m, p + V(0, h, 0), w + .8f, d + .8f, Mathf.Min(w, d) * .28f, roof);
        }

        /// <summary>The horizon the east line needs (the CBD panorama is hidden out here). Right (+X, south): the river
        /// plain, the Swan River, its far bank of river gums, the flat suburbs and the airport beyond, and the long blue
        /// Darling Scarp; left (-X, north): roofs, gums, Norfolk pines and jacarandas, a tree line, the scarp again, higher
        /// towards +Z (the east, where the line is heading). scarp = ridge height in metres. No colliders.</summary>
        static ComicMesh EastFar(float grade, int seed, float scarp, float riverNear, float riverFar, Color haze, Color plain, bool industry, Action<ComicMesh, System.Random> extras = null)
        {
            var m = new ComicMesh();
            var r = new System.Random(seed);
            const float z0 = -220f, z1 = 820f;
            Color river = K.Hex("5E8FA6"), riverDeep = K.Hex("4B7A93"), bank = K.Hex("55733F");
            // ground ahead of the built chunks, then bands out to the horizon
            FarStrip(m, K.Edge, 62f, 330f, z1, K.Kerb - .05f, plain);
            FarStrip(m, -K.Edge, -62f, 330f, z1, K.Kerb - .05f, Color.Lerp(plain, Ballast, .35f));
            FarStrip(m, 60f, riverNear, z0, z1, -.3f, Haze(plain, 100f, haze));
            FarStrip(m, riverNear, (riverNear + riverFar) * .5f, z0, z1, -1.2f, Haze(river, riverNear, haze));
            FarStrip(m, (riverNear + riverFar) * .5f, riverFar, z0, z1, -1.2f, Haze(riverDeep, riverFar, haze));
            FarStrip(m, riverFar, riverFar + 260f, z0, z1, -.5f, Haze(K.Hex("7E9A58"), riverFar + 100f, haze));
            FarStrip(m, riverFar + 260f, 1500f, z0, z1, -.6f, Haze(K.Hex("90A66E"), 700f, haze));
            FarStrip(m, -60f, -200f, z0, z1, -.3f, Haze(K.Hex("86A05C"), 120f, haze));
            FarStrip(m, -200f, -480f, z0, z1, -.35f, Haze(K.Hex("8FA567"), 300f, haze));
            FarStrip(m, -480f, -1500f, z0, z1, -.4f, Haze(K.Hex("9AAE7A"), 650f, haze));
            // ripples and sandbars on the river
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 70; i++)
            {
                float x = F(r, riverNear + 4f, riverFar - 8f), z = F(r, -160f, 760f), len = F(r, 3f, 8f);
                K.Face(m, V(x, -1.17f, z), V(x, -1.17f, z + .6f), V(x + len, -1.17f, z + .6f), V(x + len, -1.17f, z), Haze(i % 3 == 0 ? K.Hex("D9E6E4") : K.Hex("86B0C0"), x, haze), Vector3.up, false);
            }
            // near bank: reeds and paperbarks; far bank: a continuous line of river gums with gaps of mown grass
            for (float z = z0; z < z1; z += F(r, 9f, 15f))
            {
                float x = riverNear - F(r, 2f, 16f);
                Blob(m, V(x, F(r, 2f, 4f), z), V(F(r, 8f, 13f), F(r, 5f, 9f), F(r, 8f, 13f)), Haze(Color.Lerp(PaperLeaf, GumLeafDark, F(r, 0f, 1f)), x, haze), 6, 4, F(r, 0f, 3f));
            }
            Ridge(m, 1, riverFar, 30f, z0, z1, -1.2f, z => 2.2f + .8f * Mathf.Sin(z * .05f), K.Hex("B9A77E"), K.Hex("7C9A55"), K.Hex("88A561"), haze, .3f);
            for (float z = z0; z < z1; z += F(r, 7f, 12f))
            {
                float x = riverFar + F(r, 8f, 34f), h = F(r, 11f, 19f);
                m.Surface = ComicSurface.Wood;
                Rod(m, V(x, 0, z), V(x + F(r, -1f, 1f), h * .6f, z), .5f, .3f, Haze(GumTrunk, x, haze), 4);
                Blob(m, V(x, h * .72f, z), V(F(r, 10f, 16f), h * .55f, F(r, 10f, 15f)), Haze(Color.Lerp(bank, GumLeaf, F(r, 0f, 1f)), x + 60f, haze), 6, 4, F(r, 0f, 3f));
            }
            // beyond the river: flat suburbs (roofs), hangars and the airport's control tower, far tree lines
            Color[] walls = { K.Hex("E6DCC4"), K.Hex("D9C9A8"), K.Hex("EDE9DD"), K.Hex("CDBB98") };
            Color[] roofs = { Terracotta, TileBrown, GreyIron, K.Hex("8A8F94"), RedIron };
            for (float z = z0; z < z1; z += F(r, 13f, 19f))
                for (int row = 0; row < 3; row++)
                {
                    float x = riverFar + 70f + row * 34f + F(r, 0f, 16f), dist = Mathf.Sqrt(x * x + z * z * .3f);
                    FarHouse(m, V(x, 0, z), F(r, 10f, 15f), F(r, 9f, 12f), F(r, 3.4f, 6f), Haze(walls[r.Next(walls.Length)], dist, haze), Haze(roofs[r.Next(roofs.Length)], dist, haze));
                }
            for (float z = z0; z < z1; z += F(r, 16f, 26f))
            {
                float x = riverFar + F(r, 170f, 300f);
                Blob(m, V(x, 6f, z), V(F(r, 18f, 28f), F(r, 11f, 16f), F(r, 16f, 24f)), Haze(Color.Lerp(K.Hex("5E7E46"), K.Hex("7A9A58"), F(r, 0f, 1f)), x + 80f, haze), 6, 4, F(r, 0f, 3f));
            }
            // the Darling Scarp: a long even blue ridge beyond the plain on both sides, rising towards the east (+Z)
            float Scarp(float z, float k) => scarp * (.72f + .28f * Mathf.InverseLerp(z0, z1, z)) * (1f + .12f * Mathf.Sin(z * .006f + k) + .05f * Mathf.Sin(z * .021f + k * 2f));
            Ridge(m, 1, 760f, 260f, z0, z1, -.6f, z => Scarp(z, 1f), K.Hex("7F9A8E"), K.Hex("5C7FA6"), K.Hex("7396BA"), haze, .35f);
            Ridge(m, -1, 700f, 260f, z0, z1, -.4f, z => Scarp(z, 2.3f) * .9f, K.Hex("7F9A8E"), K.Hex("6184AA"), K.Hex("789ABC"), haze, .4f);
            // left (north): the suburbs behind the railway
            for (float z = z0; z < z1; z += F(r, 12f, 17f))
                for (int row = 0; row < 3; row++)
                {
                    float x = -(70f + row * 30f + F(r, 0f, 14f)), dist = Mathf.Sqrt(x * x + z * z * .3f);
                    if (industry && row == 0)
                    {
                        float w = F(r, 18f, 30f), h = F(r, 6f, 9f), len = F(r, 11f, 15f);
                        Color shed = Haze(new[] { K.Hex("D9D2BC"), K.Hex("9AA7A0"), K.Hex("7F9488"), K.Hex("C9CDD1") }[r.Next(4)], dist, haze);
                        m.Surface = ComicSurface.Metal;
                        m.Box(V(x, h * .5f - 1f, z), V(w, h + 2f, len), shed);
                        GableRoofX(m, V(x, h, z), w + .6f, len + .6f, 1.6f, Haze(Zinc, dist, haze), shed);
                        continue;
                    }
                    FarHouse(m, V(x, 0, z), F(r, 10f, 14f), F(r, 9f, 12f), F(r, 3.4f, 6.2f), Haze(walls[r.Next(walls.Length)], dist, haze), Haze(roofs[r.Next(roofs.Length)], dist, haze));
                }
            for (float z = z0; z < z1; z += F(r, 15f, 24f))
            {
                float x = -F(r, 64f, 170f), d2 = Mathf.Abs(x) + Mathf.Abs(z) * .3f;
                int kind = r.Next(6);
                if (kind == 0) CheapPine(m, V(x, 0f, z), F(r, 16f, 24f), Haze(K.Hex("2C5A36"), d2, haze));
                else if (kind == 1)
                {
                    m.Surface = ComicSurface.Wood;
                    Rod(m, V(x, 0, z), V(x, 5f, z), .4f, .3f, Haze(JacBark, d2, haze), 4);
                    Blob(m, V(x, 7f, z), V(F(r, 9f, 12f), F(r, 5f, 6.5f), F(r, 9f, 12f)), Haze(r.Next(2) == 0 ? Jac1 : Jac2, d2, haze), 6, 4, F(r, 0f, 3f));
                }
                else
                {
                    float h = F(r, 12f, 20f);
                    m.Surface = ComicSurface.Wood;
                    Rod(m, V(x, 0, z), V(x + F(r, -1.5f, 1.5f), h * .62f, z), .5f, .28f, Haze(GumTrunk, d2, haze), 4);
                    Blob(m, V(x, h * .74f, z), V(F(r, 9f, 14f), h * .5f, F(r, 9f, 14f)), Haze(Color.Lerp(GumLeaf, GumLeafDark, F(r, 0f, 1f)), d2, haze), 6, 4, F(r, 0f, 3f));
                }
            }
            for (float z = z0; z < z1; z += F(r, 14f, 22f))
            {
                float x = -F(r, 180f, 260f);
                Blob(m, V(x, 6f, z), V(F(r, 18f, 26f), F(r, 10f, 15f), F(r, 16f, 22f)), Haze(Color.Lerp(K.Hex("5E7E46"), K.Hex("7A9A58"), F(r, 0f, 1f)), 280f, haze), 6, 4, F(r, 0f, 3f));
            }
            // an airliner on approach to Perth Airport (the flight path crosses just east of here)
            Airliner(m, V(310f, 95f, 430f), 205f, 1.6f, haze);
            extras?.Invoke(m, r);
            m.Shear(grade);
            return m;
        }

        /// <summary>A small far airliner (no livery): white fuselage, swept wings, a tail fin, two engines.</summary>
        static void Airliner(ComicMesh m, Vector3 p, float yaw, float s, Color haze)
        {
            Quaternion q = Quaternion.Euler(-4f, yaw, 0);
            Color body = Color.Lerp(White, haze, .25f), dark = Color.Lerp(K.Hex("8B95A0"), haze, .3f);
            m.Surface = ComicSurface.PaintedMetal;
            Rod(m, p + q * V(0, 0, -14f * s), p + q * V(0, 0, 13f * s), 1.9f * s, 1.9f * s, body, 6);
            Rod(m, p + q * V(0, 0, 13f * s), p + q * V(0, .4f * s, 18f * s), 1.9f * s, .3f * s, body, 6, true);
            Rod(m, p + q * V(0, 0, -14f * s), p + q * V(0, .9f * s, -20f * s), 1.9f * s, .4f * s, body, 6, true);
            foreach (int sd in new[] { -1, 1 })
            {
                Slab(m, p + q * V(sd * 1.5f * s, -.6f * s, 2f * s), p + q * V(sd * 16f * s, .4f * s, -5f * s), q * Vector3.forward, 6f * s, 1.8f * s, .5f * s, body, dark);
                Slab(m, p + q * V(sd * 1f * s, .6f * s, -16f * s), p + q * V(sd * 6f * s, 1f * s, -19f * s), q * Vector3.forward, 3f * s, 1.2f * s, .3f * s, body, dark);
                Rod(m, p + q * V(sd * 6f * s, -1.6f * s, 3.5f * s), p + q * V(sd * 6f * s, -1.6f * s, -.5f * s), 1f * s, .8f * s, dark, 6, true);
            }
            Extrude(m, new[] { p + q * V(-.15f * s, 1.2f * s, -13f * s), p + q * V(-.15f * s, 7.5f * s, -20f * s), p + q * V(-.15f * s, 7.5f * s, -22f * s), p + q * V(-.15f * s, 1.2f * s, -19.5f * s) }, q * V(.3f * s, 0, 0), Color.Lerp(K.Hex("3A6FB0"), haze, .3f));
        }

        // ================================================================== animals

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

        /// <summary>Australian magpie: black body, white nape, shoulder and rump, a pale bill, walking pose.</summary>
        static ComicMesh MagpieMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            Color black = K.Hex("1E2126"), white = K.Hex("F2F1EC"), bill = K.Hex("C9D0D6");
            m.Ellipsoid(V(0, .3f, .04f), V(.24f, .22f, .4f), black);
            m.Box(V(0, .4f, -.16f), V(.17f, .17f, .17f), black);
            m.Box(V(0, .46f, -.08f), V(.16f, .08f, .1f), white);
            m.Box(V(0, .39f, -.3f), V(.05f, .05f, .14f), bill);
            m.Box(V(0, .385f, -.38f), V(.04f, .04f, .04f), black);
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(V(s * .12f, .32f, .06f), V(.04f, .14f, .3f), black, Quaternion.Euler(0, 0, s * -8f));
                m.Box(V(s * .125f, .36f, -.02f), V(.045f, .07f, .12f), white, Quaternion.Euler(0, 0, s * -8f));
                m.Box(V(s * .075f, .43f, -.22f), V(.03f, .03f, .03f), K.Hex("B5651D"));
                m.Box(V(s * .05f, .1f, .02f), V(.035f, .2f, .035f), K.Hex("3A3F45"));
                m.Box(V(s * .05f, .02f, -.02f), V(.07f, .04f, .12f), K.Hex("3A3F45"));
            }
            m.Box(V(0, .3f, .3f), V(.14f, .05f, .2f), black, Quaternion.Euler(-12f, 0, 0));
            m.Box(V(0, .33f, .22f), V(.15f, .05f, .08f), white);
            return m;
        }
    }
}
