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
using E = CityScooter.EditorTools.Districts.EastLineKit;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// East line, art agent 1 (v0.5; v0.6: the continuous corridor of EastLineKit): the first three districts of the
    /// Midland line after the CBD.
    /// MCIVER (order 210): Wellington St beside the railway: Royal Perth Hospital (the tan brick ward block with white
    /// balconies, North Block's precast bands and blue ribbon glazing, the enclosed link bridge over the street), the red
    /// brick heritage buildings and the Moreton Bay fig, McIver station (barrel-vault shelters, an A-series set), Wellington
    /// Square (lawn, figs, the playground's coloured shade discs), the Perth Mint, road works at the closed Moore St crossing.
    /// CLAISEBROOK (order 220): Claisebrook station and its white footbridge, the railcar depot (rows of stabled trains),
    /// Royal St, Claisebrook Cove (limestone walls, townhouses, alfresco, boats), Trafalgar Bridge, the Victoria Gardens
    /// shelter, a timber footbridge over the canal (SPLASH).
    /// EAST PERTH (order 230): the terminal (the long covered platform with the Indian Pacific, the brown brick Public
    /// Transport Centre with its angled concrete fins), the suburban island platform, the Prospector, the Swan River
    /// foreshore with Matagarup Bridge and Optus Stadium across the water (built with the foreshore chunk at reduced size),
    /// the freeway and rail bridges, the old East Perth Power Station.
    /// The railway runs along the left (-X) side of EVERY chunk as scenery (EastLineKit.Corridor: the same fence, tracks,
    /// wires and portals from z = 0 to the end, see docs/v0.6-polish.md); nothing there has a collider. Landmarks on the
    /// railway side stand behind the corridor.
    /// Uses the CBD / Quay comic kit (CbdQuayKit, CqChunkDraft): ComicPalette + ComicInk only, vertex colours.
    ///   Menu: CityScooter/Districts/Build East 1
    ///   Batch: -executeMethod CityScooter.EditorTools.Districts.East1Builder.BuildBatch
    ///   Batch with the props atlases (needs graphics): ...East1Builder.BuildAndRenderAtlasBatch
    /// Files: East1Builder.cs (entry points, shared parts), East1Rail.cs (railway kit), East1Mciver.cs,
    /// East1Claisebrook.cs, East1EastPerth.cs.
    /// </summary>
    public static partial class East1Builder
    {
        public const string DataFolder = CbdBuilder.DataFolder;
        static string Prefabs(string name) => ComicCityBuilder.Prefabs + "/Districts/" + name;
        static string Meshes(string name) => ComicCityBuilder.Root + "/Districts/" + name;
        public static string DistrictPath(string name) => DataFolder + "/District_" + name + ".asset";
        public static string ChunkPath(string name, string variant) => Prefabs(name) + "/CHN_" + name + "_" + variant + ".prefab";
        static string Pf(string name, string asset) => Prefabs(name) + "/" + asset + ".prefab";

        [MenuItem("CityScooter/Districts/Build East 1")]
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
            BuildMciver(LoadTheme(), atlas);
            BuildClaisebrook(LoadTheme(), atlas);
            BuildEastPerth(LoadTheme(), atlas);
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

        sealed class Look
        {
            public Color Fog, SkyTint, AmbSky, AmbEquator, AmbGround, Sun; public float SunIntensity;
        }

        /// <summary>Writes the fields this builder owns. gateway, ambience, ambienceVolume, skyYaw, skySize, skyHeight,
        /// skyline and line belong to the district system: never written here, so their values survive a rebuild.</summary>
        static District WriteDistrict(string name, string id, string display, int order, Vector2 length, string[] chunkIds,
            Obstacle[] obstacles, Obstacle[] jump, HazardPattern[] hazards, GameObject[] people, Ramp[] ramps, float rampFrequency, GameObject far, Look look)
        {
            District d = K.LoadOrCreate<District>(DistrictPath(name));
            RoadChunk Chunk(string v) => AssetDatabase.LoadAssetAtPath<GameObject>(ChunkPath(name, v)).GetComponent<RoadChunk>();
            d.id = id; d.displayName = display; d.order = order; d.length = length;
            d.chunks = chunkIds.Select(Chunk).ToArray();
            d.orderedChunks = true;
            d.entry = Chunk("Entry"); d.exit = Chunk("Exit");
            d.allowBoulevard = false;
            d.obstacles = obstacles; d.jumpObstacles = jump; d.gifts = Array.Empty<Gift>();
            d.hazards = hazards; d.pedestrians = people; d.ramps = ramps; d.rampFrequency = rampFrequency;
            d.blendMeters = 150f;
            d.fogColor = look.Fog; d.skyTint = look.SkyTint; d.ambientSky = look.AmbSky; d.ambientEquator = look.AmbEquator;
            d.ambientGround = look.AmbGround; d.sunColor = look.Sun; d.sunIntensity = look.SunIntensity;
            d.farScenery = far;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            return d;
        }

        static ComicMesh Make(Action<ComicMesh> build) { var m = new ComicMesh(); build(m); return m; }
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static float F(System.Random r, float a, float b) => Mathf.Lerp(a, b, (float)r.NextDouble());
        static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, t);

        // ================================================================================== palette (sRGB)
        // Sources and notes: art-src/districts/mciver|claisebrook|eastperth/SOURCES.txt

        static Color Ink => K.R.ink;
        static readonly Color White = K.Hex("F6F4EE"), OffWhite = K.Hex("ECE8DC"), Cream = K.Hex("EFE3C8"), TrimCream = K.Hex("F3E4C2");
        static readonly Color BrickRed = K.Hex("A8452E"), BrickDark = K.Hex("8A3A28"), BrickTan = K.Hex("C98A52"), BrickTanDark = K.Hex("A97042"), BrickBrown = K.Hex("6B4636"), BrickBeige = K.Hex("D8BE94");
        static readonly Color Limestone = K.Hex("E2D2A6"), LimestoneCap = K.Hex("F0E6CA"), LimestoneDark = K.Hex("BFA978");
        static readonly Color Terracotta = K.Hex("C4623C"), TerracottaDark = K.Hex("A34E30"), Slate = K.Hex("4A5560"), RoofGrey = K.Hex("8A9198"), RoofBrown = K.Hex("5E4638");
        static readonly Color WindowGlass = K.Hex("2F6F86"), GlassDark = K.Hex("23404E"), RibbonBlue = K.Hex("1F3E66"), GlassPale = K.Hex("BFDCE3");
        static readonly Color ConcreteGrey = K.Hex("C9C5BA"), ConcreteDark = K.Hex("A19D93"), PlantGrey = K.Hex("8F969C"), SteelDark = K.Hex("3A4148");
        static readonly Color Paving = K.Hex("D3CFC6"), PavingWarm = K.Hex("E4D6B4"), PaverRed = K.Hex("A8553F"), KerbCap = K.Hex("EDE8DC"), PathRed = K.Hex("B5553F");
        static readonly Color Lawn = K.Hex("6DA544"), LawnLight = K.Hex("82B654"), LawnDry = K.Hex("A9B45E"), Hedge = K.Hex("4E7F3A"), Mulch = K.Hex("7A5A44");
        static readonly Color FigDark = K.Hex("2F5A2C"), FigMid = K.Hex("3F7235"), FigLight = K.Hex("5C8E44"), FigBark = K.Hex("8B8378");
        static readonly Color GumLeaf = K.Hex("6E8F58"), GumLeafLight = K.Hex("8FA96A"), GumBark = K.Hex("DDD6C4"), GumBarkDark = K.Hex("B9AE98");
        static readonly Color Jacaranda = K.Hex("8F6FD0"), JacarandaLight = K.Hex("B49AE6"), JacarandaBark = K.Hex("6B5A4E");
        static readonly Color Timber = K.Hex("A8774E"), Deck = K.Hex("9A6B45"), DeckLight = K.Hex("B88757");
        static readonly Color River = K.Hex("4A8FB5"), RiverLight = K.Hex("8FC0D8"), CoveWater = K.Hex("3F8FA8"), CoveLight = K.Hex("86C6CF");
        static readonly Color SafetyOrange = K.Hex("F28C28"), SignGreen = K.Hex("1E8E4A"), SignBlue = K.Hex("2F6FB0"), Navy = K.Hex("2A3E6E"), Gold = K.Hex("E6B93C");
        static readonly Color Ochre = K.Hex("D9A441"), Peach = K.Hex("F0BF9A"), RenderTerracotta = K.Hex("CF7A4E"), Teal = K.Hex("3E9A96"), RenderCream = K.Hex("F2E6C4");
        static readonly Color[] SignColours = { K.Hex("E2463A"), K.Hex("F6C543"), K.Hex("2A9D9A"), K.Hex("2F6FB0"), K.Hex("F28C28"), K.Hex("E85A8C") };

        // ================================================================================== ground and street parts

        /// <summary>Ground slab beyond the sidewalk on side s, from d0 to d1 metres past its edge (sheared with the road).</summary>
        static void Ground(CqChunkDraft d, int s, float z0, float z1, float d0, float d1, Color c, ComicSurface surface, float top = K.Kerb + .02f)
        {
            d.Road.Surface = surface;
            K.SideBox(d.Road, s, z0, z1, -2.4f, top, d0, d1, c);
        }

        /// <summary>A flat patch on the ground beyond the sidewalk (no ink): paths, paving bands, markings.</summary>
        static void Patch(CqChunkDraft d, int s, float z0, float z1, float d0, float d1, Color c, float y = K.Kerb + .035f, ComicSurface surface = ComicSurface.Plain)
        {
            d.Road.Surface = surface;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.Face(d.Road, V(s * (K.Edge + d0), y, z), V(s * (K.Edge + d0), y, e), V(s * (K.Edge + d1), y, e), V(s * (K.Edge + d1), y, z), c, Vector3.up, false);
            }
        }

        /// <summary>A low wall with a cap just beyond the sidewalk (sheared with the street).</summary>
        static void LowWall(CqChunkDraft d, int s, float z0, float z1, float height, Color wall, Color cap, float off = 0f, ComicSurface surface = ComicSurface.Concrete)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = surface;
            K.SideBox(m, s, z0, z1, -1f, K.Kerb + height, off + .05f, off + .5f, wall);
            m.Surface = ComicSurface.Concrete;
            K.SideBox(m, s, z0, z1, K.Kerb + height, K.Kerb + height + .1f, off, off + .55f, cap);
        }

        /// <summary>A clipped hedge behind a low wall.</summary>
        static void HedgeRow(CqChunkDraft d, int s, float z0, float z1, float off, float height = 1.2f)
        {
            d.SlopedFrontage.Surface = ComicSurface.Foliage;
            K.SideBox(d.SlopedFrontage, s, z0, z1, K.Kerb, K.Kerb + height, off, off + 1.1f, Hedge);
        }

        /// <summary>An invisible wall along side s (keeps people out of the scenery beyond the sidewalk).</summary>
        static void SideWall(CqChunkDraft d, int s, float z0, float z1, float depth = 12f, float height = 5f)
        {
            float zc = (z0 + z1) * .5f;
            d.Walls.Add(new Bounds(new Vector3(s * (K.Edge + .4f + depth * .5f), d.Y(zc) + height * .5f - 1f, zc), new Vector3(depth, height + 3f, z1 - z0)));
        }

        static Bounds KeepOutBox(CqChunkDraft d, int s, float z0, float z1)
        {
            float yTop = d.Y(z0) + 5f, yBottom = d.Y(z1) - 2f;
            return new Bounds(new Vector3(s * (K.Half + K.Side * .5f + .3f), (yTop + yBottom) * .5f, (z0 + z1) * .5f), new Vector3(K.Side + .6f, yTop - yBottom, z1 - z0));
        }

        /// <summary>The line's street edge (EastLineKit) on the right at the outer end of an entry / exit chunk, with a
        /// low row of this district's own background blocks behind it: the street goes on, it does not open into a park.</summary>
        static void OuterEdge(CqChunkDraft d, float z0, float z1, Color ground, int seed, bool townhouses)
        {
            E.StreetEdge(d, 1, z0, z1, ground);
            if (townhouses) DeepTownhouses(d, 1, z0, z1, 6.5f, seed);
            else DeepBlocks(d, 1, z0, z1, 6.5f, seed, 8f, 13f);
        }

        /// <summary>The shared path (PedestrianMall): the line's red asphalt path with its dashed white centre line and
        /// pale edge lines, pale flagstone side strips. Same cross-section as the street.</summary>
        static void SharedPath(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f)
        {
            d.Type = RoadType.PedestrianMall;
            E.Street(d, E.StreetLook.SharedPath(Paving), null, -1f, skipFrom, skipTo);
        }

        /// <summary>A painted bicycle and walker pair of arrows is too fiddly at this size: a white diamond marks the path.</summary>
        static void PathDiamond(CqChunkDraft d, float z)
        {
            d.Road.Surface = ComicSurface.Plain;
            foreach (float x in new[] { -2f, 2f })
                K.Face(d.Road, V(x, .032f, z - .9f), V(x - .45f, .032f, z), V(x, .032f, z + .9f), V(x + .45f, .032f, z), White, Vector3.up, false);
        }

        // ================================================================================== facade parts
        // After CbdBuilder / RockinghamBuilder: ink frames with the glass set in, projecting heads and sills, real slabs.

        static void Plinth(ComicMesh f, CqLot l)
        {
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, 0, l.W, -2.6f, 0f, .02f, -l.Depth, Ink);
        }

        /// <summary>A framed window on the facade plane at o: ink frame, glass set in, ink mullions, projecting head and sill.</summary>
        static void Win(ComicMesh f, CqLot l, float a, float w, float y0, float y1, float o, Color trim, int panes = 2, bool sill = true, bool head = true, bool transom = false)
        {
            f.Surface = ComicSurface.PaintedMetal;
            K.LQuad(f, l, a - w * .5f, a + w * .5f, y0, y1, o + .015f, Ink);
            f.Surface = ComicSurface.Glass;
            K.LQuad(f, l, a - w * .5f + .09f, a + w * .5f - .09f, y0 + .09f, y1 - .09f, o + .03f, WindowGlass);
            f.Surface = ComicSurface.PaintedMetal;
            for (int i = 1; i < panes; i++)
            {
                float am = a - w * .5f + w * i / panes;
                K.LQuad(f, l, am - .035f, am + .035f, y0 + .09f, y1 - .09f, o + .04f, Ink, false);
            }
            if (transom) K.LQuad(f, l, a - w * .5f + .09f, a + w * .5f - .09f, y1 - (y1 - y0) * .3f - .03f, y1 - (y1 - y0) * .3f + .03f, o + .04f, Ink, false);
            f.Surface = ComicSurface.Concrete;
            if (sill) K.LBox(f, l, a - w * .5f - .1f, a + w * .5f + .1f, y0 - .14f, y0, o + .16f, o, trim);
            if (head) K.LBox(f, l, a - w * .5f - .1f, a + w * .5f + .1f, y1, y1 + .16f, o + .12f, o, trim);
        }

        /// <summary>An opening with a three-sided arch head (spring at ys, rise about 0.4 w): fill colour, trim voussoirs and
        /// a keystone. With fill = a dark colour on an arcade band it reads as the open arch.</summary>
        static void Arch(ComicMesh f, CqLot l, float a, float w, float y0, float ys, float o, Color fill, Color trim, bool glazed = false)
        {
            float r = w * .4f, h = w * .5f, q = w * .24f;
            f.Surface = glazed ? ComicSurface.Glass : ComicSurface.Plain;
            Vector3 n = new Vector3(-l.S, 0, 0);
            if (ys > y0 + .01f) K.LQuad(f, l, a - h, a + h, y0, ys, o + .02f, fill, glazed);
            K.Face(f, l.P(a - h, ys, o + .02f), l.P(a + h, ys, o + .02f), l.P(a + q, ys + r, o + .02f), l.P(a - q, ys + r, o + .02f), fill, n, false);
            if (glazed)
            {
                f.Surface = ComicSurface.PaintedMetal;
                K.LQuad(f, l, a - .035f, a + .035f, y0, ys + r, o + .035f, Ink, false);
                K.LQuad(f, l, a - h, a + h, ys - .035f, ys + .035f, o + .035f, Ink, false);
            }
            f.Surface = ComicSurface.Concrete;
            f.Beam(l.P(a - h - .04f, ys - .05f, o + .07f), l.P(a - q, ys + r + .05f, o + .07f), .15f, trim);
            f.Beam(l.P(a + h + .04f, ys - .05f, o + .07f), l.P(a + q, ys + r + .05f, o + .07f), .15f, trim);
            K.LBox(f, l, a - q - .05f, a + q + .05f, ys + r - .02f, ys + r + .13f, o + .14f, o, trim);
            K.LBox(f, l, a - .13f, a + .13f, ys + r - .1f, ys + r + .26f, o + .18f, o, trim);
        }

        /// <summary>A window on the lot's end facing the rider (-Z): ink frame and glass, o0..o1 across, y0..y1 up.</summary>
        static void EndWin(ComicMesh f, CqLot l, float along, float o0, float o1, float y0, float y1)
        {
            f.Surface = ComicSurface.PaintedMetal;
            K.Face(f, l.P(along - .02f, y0, o0), l.P(along - .02f, y0, o1), l.P(along - .02f, y1, o1), l.P(along - .02f, y1, o0), Ink, Vector3.back);
            f.Surface = ComicSurface.Glass;
            K.Face(f, l.P(along - .04f, y0 + .1f, o0 - .1f), l.P(along - .04f, y0 + .1f, o1 + .1f), l.P(along - .04f, y1 - .1f, o1 + .1f), l.P(along - .04f, y1 - .1f, o0 - .1f), WindowGlass, Vector3.back);
        }

        /// <summary>Shopfront glazing between a0 and a1: ink frame, glass above a kick plate, ink mullions, a transom.</summary>
        static void Shopfront(ComicMesh f, CqLot l, float a0, float a1, float y0, float y1, float o, bool door = false)
        {
            f.Surface = ComicSurface.PaintedMetal;
            K.LQuad(f, l, a0, a1, y0, y1, o, Ink);
            f.Surface = ComicSurface.Glass;
            K.LQuad(f, l, a0 + .12f, a1 - .12f, y0 + .45f, y1 - .12f, o + .02f, WindowGlass);
            f.Surface = ComicSurface.PaintedMetal;
            int n = Mathf.Max(1, Mathf.RoundToInt((a1 - a0) / 1.4f));
            for (int i = 1; i < n; i++) { float a = Mathf.Lerp(a0, a1, (float)i / n); K.LBox(f, l, a - .05f, a + .05f, y0 + .4f, y1 - .1f, o + .07f, o, Ink); }
            K.LBox(f, l, a0 + .1f, a1 - .1f, y1 - .75f, y1 - .66f, o + .07f, o, Ink);
            if (door)
            {
                float a = (a0 + a1) * .5f;
                f.Surface = ComicSurface.Glass;
                K.LQuad(f, l, a - .5f, a + .5f, y0 + .05f, y1 - .78f, o + .04f, GlassDark);
                f.Surface = ComicSurface.PaintedMetal;
                K.LBox(f, l, a + .3f, a + .36f, y0 + .9f, y0 + 1.3f, o + .1f, o, TrimCream);
            }
        }

        /// <summary>A thick sloped slab from the wall (height u0 at outward o0) to (u1 at o1): awnings, verandah and
        /// lean-to roofs, sunshades. Underside darker.</summary>
        static void SlopeSlab(ComicMesh f, CqLot l, float a0, float a1, float u0, float o0, float u1, float o1, float thick, Color top, Color under)
        {
            Vector3 A0 = l.P(a0, u0, o0), A1 = l.P(a1, u0, o0), B0 = l.P(a0, u1, o1), B1 = l.P(a1, u1, o1), t = V(0, -thick, 0);
            Vector3 toRoad = new Vector3(-l.S * Mathf.Sign(o1 - o0), 0, 0);
            K.Face(f, A0, A1, B1, B0, top, Vector3.up);
            K.Face(f, A0 + t, A1 + t, B1 + t, B0 + t, under, Vector3.down);
            K.Face(f, B0, B1, B1 + t, B0 + t, top, toRoad);
            K.Face(f, A0, B0, B0 + t, A0 + t, top, Vector3.back);
            K.Face(f, A1, B1, B1 + t, A1 + t, top, Vector3.forward);
        }

        /// <summary>A fabric awning: sloped slab in two alternating colours, an ink front bar, a hanging valance.</summary>
        static void Awning(ComicMesh f, CqLot l, float a0, float a1, float y, float depth, Color c, Color? stripe = null, float drop = .55f)
        {
            f.Surface = ComicSurface.Fabric;
            int n = stripe.HasValue ? Mathf.Max(3, Mathf.RoundToInt((a1 - a0) / 1.1f)) : 1;
            for (int i = 0; i < n; i++)
            {
                float b0 = Mathf.Lerp(a0, a1, (float)i / n), b1 = Mathf.Lerp(a0, a1, (float)(i + 1) / n);
                Color col = i % 2 == 0 ? c : stripe ?? c;
                SlopeSlab(f, l, b0, b1, y + drop, -.02f, y, depth, .07f, col, Mix(col, Ink, .3f));
                K.LQuad(f, l, b0, b1, y - .28f, y - .06f, depth + .01f, col, false);
            }
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, a0 - .04f, a1 + .04f, y - .08f, y + .02f, depth + .05f, depth - .05f, Ink);
        }

        /// <summary>A flat cantilevered street awning (the Perth shop verandah): slab, ink fascia, tie rods back to the wall.</summary>
        static void FlatAwning(ComicMesh f, CqLot l, float a0, float a1, float y, float depth, Color fascia, Color soffit)
        {
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, a0, a1, y, y + .14f, depth, -.05f, soffit);
            K.LBox(f, l, a0 - .05f, a1 + .05f, y - .16f, y + .3f, depth + .08f, depth - .08f, fascia);
            K.LBox(f, l, a0 - .06f, a1 + .06f, y - .22f, y - .15f, depth + .1f, depth - .1f, Ink);
            for (float a = a0 + 1f; a < a1; a += Mathf.Max(2.6f, (a1 - a0 - 2f) / 3f))
                f.Beam(l.P(a, y + .2f, depth - .3f), l.P(a, y + 1.5f, 0f), .06f, Ink);
        }

        /// <summary>A balcony: slab, a top rail, and either a glass panel or thin balusters.</summary>
        static void Balcony(ComicMesh f, CqLot l, float a0, float a1, float y, float o, float depth, Color slab, Color rail, bool glass, float railHeight = 1f)
        {
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, a0, a1, y - .16f, y, o + depth, o, slab);
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, a0, a1, y + railHeight - .07f, y + railHeight, o + depth + .02f, o + depth - .08f, rail);
            K.LBox(f, l, a0, a0 + .07f, y, y + railHeight, o + depth, o, rail);
            K.LBox(f, l, a1 - .07f, a1, y, y + railHeight, o + depth, o, rail);
            if (glass)
            {
                f.Surface = ComicSurface.Glass;
                K.LBox(f, l, a0 + .07f, a1 - .07f, y + .08f, y + railHeight - .09f, o + depth - .01f, o + depth - .06f, GlassPale);
            }
            else
            {
                int n = Mathf.Max(2, Mathf.RoundToInt((a1 - a0) / .45f));
                for (int i = 1; i < n; i++)
                {
                    float a = Mathf.Lerp(a0, a1, (float)i / n);
                    K.LQuad(f, l, a - .03f, a + .03f, y, y + railHeight - .07f, o + depth - .03f, rail, false);
                }
                K.LBox(f, l, a0, a1, y + .08f, y + .13f, o + depth, o + depth - .06f, rail);
            }
        }

        /// <summary>A hip roof over a0..a1 between oFront and oBack: eaves at u0, a ridge at u1 inset from the ends.</summary>
        static void HipRoof(ComicMesh f, CqLot l, float a0, float a1, float u0, float u1, float oFront, float oBack, Color c)
        {
            float oc = (oFront + oBack) * .5f, inset = Mathf.Min((a1 - a0) * .5f - .05f, Mathf.Abs(oFront - oBack) * .5f);
            Vector3 f0 = l.P(a0, u0, oFront), f1 = l.P(a1, u0, oFront), b0 = l.P(a0, u0, oBack), b1 = l.P(a1, u0, oBack);
            Vector3 r0 = l.P(a0 + inset, u1, oc), r1 = l.P(a1 - inset, u1, oc), toRoad = new Vector3(-l.S, 0, 0);
            K.Face(f, f0, f1, r1, r0, c, (toRoad + Vector3.up).normalized);
            K.Face(f, b0, b1, r1, r0, c, (-toRoad + Vector3.up).normalized);
            K.TriFacing(f, f0, b0, r0, c, (Vector3.back + Vector3.up).normalized);
            K.TriFacing(f, f1, b1, r1, c, (Vector3.forward + Vector3.up).normalized);
        }

        /// <summary>A brick chimney with a cream cap and a pot.</summary>
        static void Chimney(ComicMesh f, CqLot l, float a, float o, float y0, float h, Color brick)
        {
            f.Surface = ComicSurface.Brick;
            K.LBox(f, l, a - .4f, a + .4f, y0, y0 + h, o + .3f, o - .3f, brick);
            f.Surface = ComicSurface.Concrete;
            K.LBox(f, l, a - .5f, a + .5f, y0 + h, y0 + h + .18f, o + .4f, o - .4f, TrimCream);
            f.Surface = ComicSurface.Brick;
            f.Cylinder(l.P(a, y0 + h + .4f, o), .16f, .45f, Terracotta, 6, .8f);
        }

        /// <summary>Rooftop plant: a louvred box on an ink kerb with a fan cowl.</summary>
        static void RoofUnit(ComicMesh f, CqLot l, float a, float o, float y, float w = 1.6f, float h = 1f)
        {
            f.Surface = ComicSurface.Metal;
            K.LBox(f, l, a - w * .5f, a + w * .5f, y, y + h, o + w * .35f, o - w * .35f, PlantGrey);
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, a - w * .5f - .05f, a + w * .5f + .05f, y, y + .1f, o + w * .35f + .05f, o - w * .35f - .05f, Ink);
            for (int i = 1; i <= 3; i++) K.LQuad(f, l, a - w * .4f, a + w * .4f, y + h * i / 4f - .03f, y + h * i / 4f + .03f, o + w * .35f + .01f, SteelDark, false);
            f.Cylinder(l.P(a, y + h + .08f, o), w * .22f, .16f, SteelDark, 8);
        }

        /// <summary>Flat ink "brick course" lines on a wall (no geometry): a few horizontal joints so big walls are not blank.</summary>
        static void Courses(ComicMesh f, CqLot l, float a0, float a1, float y0, float y1, float o, Color line, float step = .9f)
        {
            f.Surface = ComicSurface.Plain;
            for (float y = y0 + step; y < y1 - .1f; y += step) K.LQuad(f, l, a0, a1, y - .02f, y + .02f, o + .012f, line, false);
        }

        // ================================================================================== trees and plants

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

        /// <summary>A limb from a to b (8-sided, tapering).</summary>
        static void Limb(ComicMesh m, Vector3 a, Vector3 b, float r0, float r1, Color c, int sides = 6)
        {
            m.Cylinder((a + b) * .5f, r0, (b - a).magnitude, c, sides, r1 / r0, Quaternion.FromToRotation(Vector3.up, b - a));
        }

        /// <summary>A leaf clump: a flattened faceted blob with a lighter one on its sunny side.</summary>
        static void Clump(ComicMesh m, Vector3 c, Vector3 size, Color dark, Color light, bool top = true)
        {
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(c, size, dark);
            if (top) m.Ellipsoid(c + V(size.x * .12f, size.y * .22f, -size.z * .1f), V(size.x * .62f, size.y * .62f, size.z * .62f), light);
        }

        /// <summary>Moreton Bay fig (Wellington Square, the hospital): a short massive trunk on buttress roots, heavy limbs
        /// spreading wide, a broad dark canopy of big clumps with lighter tops.</summary>
        static void MoretonBayFig(ComicMesh m, Vector3 p, float s, int seed)
        {
            var r = new System.Random(seed * 97 + 5);
            float spin = F(r, 0f, 360f);
            m.Surface = ComicSurface.Wood;
            for (int i = 0; i < 7; i++)
            {
                float a = (spin + i * 360f / 7f + F(r, -12f, 12f)) * Mathf.Deg2Rad;
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a)), side = Vector3.Cross(Vector3.up, dir);
                float len = F(r, 2.2f, 3.4f) * s;
                // a buttress: a thin fin from high on the trunk down to the ground away from it
                Vector3 hi = p + dir * .5f * s + V(0, F(r, 1.6f, 2.4f) * s, 0), lo = p + dir * len + V(0, .05f, 0), foot = p + dir * .4f * s + V(0, .05f, 0);
                K.TriFacing(m, hi + side * .14f * s, lo, foot + side * .2f * s, FigBark, side);
                K.TriFacing(m, hi - side * .14f * s, lo, foot - side * .2f * s, Mix(FigBark, Ink, .25f), -side);
                K.TriFacing(m, hi + side * .14f * s, hi - side * .14f * s, lo, Mix(FigBark, White, .15f), Vector3.up);
            }
            m.Cylinder(p + V(0, 1.6f * s, 0), 1.05f * s, 3.2f * s, FigBark, 8, .8f);
            Vector3 fork = p + V(0, 3f * s, 0);
            int limbs = 5;
            for (int i = 0; i < limbs; i++)
            {
                float a = (spin + 20f + i * 360f / limbs + F(r, -15f, 15f)) * Mathf.Deg2Rad;
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                float reach = F(r, 4.2f, 5.6f) * s;
                Vector3 elbow = fork + dir * reach * .55f + V(0, F(r, 1.6f, 2.4f) * s, 0), tip = fork + dir * reach + V(0, F(r, 2.6f, 3.4f) * s, 0);
                m.Surface = ComicSurface.Wood;
                Limb(m, fork - V(0, .4f * s, 0), elbow, .46f * s, .3f * s, FigBark);
                Limb(m, elbow, tip, .3f * s, .16f * s, FigBark);
                Clump(m, tip + V(0, .8f * s, 0), V(F(r, 5.8f, 7f), F(r, 3f, 3.8f), F(r, 5.8f, 7f)) * s, i % 2 == 0 ? FigDark : FigMid, FigLight);
            }
            Clump(m, fork + V(0, 5.2f * s, 0), V(8f, 4.4f, 8f) * s, FigMid, FigLight);
            Clump(m, fork + V(.8f * s, 3.4f * s, -.6f * s), V(6.5f, 3f, 6.5f) * s, FigDark, FigMid, false);
        }

        /// <summary>A gum tree (lemon-scented / spotted gum on the verges): a slender pale trunk with a kink, two rising
        /// limbs, open clumps of olive foliage high up. Colours can be swapped (jacaranda: purple, dark bark).</summary>
        static void Gum(ComicMesh m, Vector3 p, float h, int seed, Color? leaf = null, Color? leafLight = null, Color? bark = null)
        {
            var r = new System.Random(seed * 53 + 11);
            Color lf = leaf ?? GumLeaf, ll = leafLight ?? GumLeafLight, bk = bark ?? GumBark, bd = Mix(bk, Ink, .25f);
            float a0 = F(r, 0f, 6.28f), lean = h * .06f;
            Vector3 dir = V(Mathf.Cos(a0), 0, Mathf.Sin(a0));
            Vector3 mid = p + V(0, h * .42f, 0) + dir * lean, fork = p + V(0, h * .62f, 0) - dir * lean * .3f;
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .05f, 0), .1f + h * .035f, .1f, Ink, 8);
            m.Surface = ComicSurface.Wood;
            Limb(m, p, mid, h * .03f + .05f, h * .022f + .04f, bk, 7);
            Limb(m, mid - V(0, .1f, 0), fork, h * .022f + .04f, h * .016f + .03f, bd, 7);
            int limbs = 3;
            for (int i = 0; i < limbs; i++)
            {
                float a = a0 + 1f + i * 6.28f / limbs + F(r, -.4f, .4f);
                Vector3 d2 = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 tip = fork + d2 * h * F(r, .16f, .24f) + V(0, h * F(r, .18f, .3f), 0);
                m.Surface = ComicSurface.Wood;
                Limb(m, fork - V(0, .15f, 0), tip, h * .013f + .03f, h * .006f + .02f, i % 2 == 0 ? bk : bd, 5);
                Clump(m, tip + V(0, h * .03f, 0), V(h * F(r, .3f, .38f), h * F(r, .16f, .2f), h * F(r, .3f, .38f)), i % 2 == 0 ? lf : Mix(lf, Ink, .12f), ll);
            }
            Clump(m, fork + V(0, h * .36f, 0), V(h * .34f, h * .2f, h * .34f), lf, ll);
        }

        /// <summary>Jacaranda in November flower: a dark forked trunk and a wide, rounded purple crown.</summary>
        static void JacarandaTree(ComicMesh m, Vector3 p, float h, int seed) => Gum(m, p, h, seed, Jacaranda, JacarandaLight, JacarandaBark);

        /// <summary>Background gum (no outline back there): a leaning trunk that forks into two limbs, three flat clumps
        /// at different heights (v0.6: it was a ball on a stick).</summary>
        static void CheapGum(ComicMesh m, Vector3 p, float h, int seed, Color? leaf = null)
        {
            Color lf = leaf ?? GumLeaf;
            float s = (seed & 1) == 0 ? 1f : -1f, r = .13f + h * .011f;
            Vector3 fork = p + V(s * h * .05f, h * .46f, 0);
            Vector3 a = fork + V(s * h * .17f, h * .24f, h * .05f), b = fork + V(-s * h * .13f, h * .32f, -h * .04f);
            m.Surface = ComicSurface.Wood;
            Limb(m, p, fork, r, r * .7f, GumBarkDark, 5);
            Limb(m, fork, a, r * .7f, r * .4f, GumBarkDark, 4);
            Limb(m, fork, b, r * .7f, r * .4f, GumBark, 4);
            m.Surface = ComicSurface.Foliage;
            m.Ellipsoid(a + V(0, h * .05f, 0), V(h * .44f, h * .2f, h * .42f), lf);
            m.Ellipsoid(b + V(0, h * .06f, 0), V(h * .48f, h * .22f, h * .44f), Mix(lf, Ink, .13f));
            m.Ellipsoid(fork + V(s * h * .02f, h * .47f, 0), V(h * .3f, h * .15f, h * .3f), Mix(lf, White, .14f));
        }

        /// <summary>A low shrub bed: three or four small blobs.</summary>
        static void Shrubs(ComicMesh m, Vector3 p, float len, int seed, Color? c = null)
        {
            var r = new System.Random(seed * 31 + 3);
            for (float z = .4f; z < len; z += F(r, .9f, 1.3f))
                K.Plant(m, p + V(F(r, -.15f, .15f), .35f, z), F(r, .75f, 1.05f), c ?? ((seed + (int)(z * 3)) % 3 == 0 ? Hedge : GumLeaf));
        }

        /// <summary>Strappy native grasses in a mulched bed (Wellington Square, the cove gardens): spiky tufts of thin blades.</summary>
        static void GrassTuft(ComicMesh m, Vector3 p, float s, int seed)
        {
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < 5; i++)
            {
                float a = (seed * 40f + i * 72f) * Mathf.Deg2Rad;
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                Slab(m, p + V(0, .02f, 0), p + dir * .35f * s + V(0, .7f * s, 0), Vector3.Cross(Vector3.up, dir), .16f * s, .03f, .03f, i % 2 == 0 ? GumLeafLight : K.Hex("7FA05A"), Hedge);
            }
        }

        // ================================================================================== street furniture

        /// <summary>A tall modern street light (grey pole, a flat LED head on a short arm towards the road).</summary>
        static void StreetLight(ComicMesh m, Vector3 p, int side, float h = 8f)
        {
            Color pole = K.Hex("AEB6BC");
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .15f, 0), .2f, .3f, Ink, 8, .85f);
            m.Cylinder(p + V(0, h * .5f, 0), .1f, h, pole, 8, .7f);
            m.Box(p + V(-side * .9f, h - .05f, 0), V(1.8f, .09f, .09f), pole);
            m.Box(p + V(-side * 1.7f, h - .12f, 0), V(.7f, .1f, .3f), SteelDark);
            m.Surface = ComicSurface.Glass;
            m.Box(p + V(-side * 1.7f, h - .19f, 0), V(.55f, .04f, .22f), K.R.cream);
        }

        /// <summary>A park / path light: a slim dark pole with a round lantern cap.</summary>
        static void PathLight(ComicMesh m, Vector3 p, float h = 4.2f)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .1f, 0), .16f, .2f, Ink, 8);
            m.Cylinder(p + V(0, h * .5f, 0), .06f, h, SteelDark, 8);
            m.Cylinder(p + V(0, h + .22f, 0), .2f, .34f, K.R.cream, 8, .8f);
            m.Cylinder(p + V(0, h + .44f, 0), .3f, .08f, SteelDark, 8);
        }

        /// <summary>A timber-slat bench on grey steel frames (yaw 0 faces -Z).</summary>
        static void Bench(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.Wood;
            for (int i = 0; i < 3; i++) m.Box(p + q * V(0, .46f, -.16f + i * .16f), V(1.8f, .05f, .13f), Timber, q);
            for (int i = 0; i < 2; i++) m.Box(p + q * V(0, .68f + i * .18f, .26f), V(1.8f, .13f, .05f), Timber, q);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.75f, .75f }) { m.Box(p + q * V(x, .23f, 0f), V(.07f, .46f, .5f), SteelDark, q); m.Box(p + q * V(x, .68f, .29f), V(.07f, .5f, .06f), SteelDark, q); }
        }

        /// <summary>A name board on two posts facing the rider (-Z): dark board, white pixel letters, a green band.</summary>
        static void NameBoard(ComicMesh m, Vector3 p, string text, float px = .09f, Color? board = null, Color? band = null, float postHeight = 2.4f)
        {
            float w = PixelWidth(text, px) + px * 6f, h = px * 9f;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -w * .4f, w * .4f }) m.Box(p + V(x, postHeight * .5f, .06f), V(.1f, postHeight, .1f), PlantGrey);
            m.Box(p + V(0, postHeight + h * .5f - .2f, 0), V(w, h, .08f), board ?? K.Hex("20262C"));
            m.Box(p + V(0, postHeight - .2f + px * .8f, -.01f), V(w, px * 1.2f, .08f), band ?? SignGreen);
            PixelText(m, text, p.x, p.y + postHeight + h * .5f - .2f + px * .6f, p.z - .045f, px, White);
        }

        /// <summary>A board on the facade plane of a lot (facing the road): background and pixel letters.</summary>
        static void WallSign(ComicMesh f, CqLot l, float a, float y, float o, string text, float px, Color board, Color letters)
        {
            float w = PixelWidth(text, px) + px * 5f;
            f.Surface = ComicSurface.PaintedMetal;
            K.LBox(f, l, a - w * .5f, a + w * .5f, y - px * 4.5f, y + px * 4.5f, o + .06f, o, board);
            PixelTextFacing(f, text, l.P(a, y, o + .07f), Quaternion.Euler(0, l.S < 0 ? -90f : 90f, 0), px, letters);
        }

        // Pixel letters for signs (place names and public building names only). Rows top to bottom; M, W and N are wider.
        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "010/101/111/101/101", ['B'] = "110/101/110/101/110", ['C'] = "011/100/100/100/011", ['D'] = "110/101/101/101/110",
            ['E'] = "111/100/110/100/111", ['F'] = "111/100/110/100/100", ['G'] = "011/100/101/101/011", ['H'] = "101/101/111/101/101",
            ['I'] = "111/010/010/010/111", ['J'] = "001/001/001/101/010", ['K'] = "101/101/110/101/101", ['L'] = "100/100/100/100/111",
            ['M'] = "10001/11011/10101/10001/10001", ['N'] = "1001/1101/1011/1001/1001", ['O'] = "010/101/101/101/010", ['P'] = "110/101/110/100/100",
            ['Q'] = "010/101/101/110/011", ['R'] = "110/101/110/101/101", ['S'] = "011/100/010/001/110", ['T'] = "111/010/010/010/010",
            ['U'] = "101/101/101/101/111", ['V'] = "10001/10001/01010/01010/00100", ['W'] = "10001/10001/10101/10101/01010", ['X'] = "101/101/010/101/101",
            ['Y'] = "101/101/010/010/010", ['Z'] = "111/001/010/100/111", ['1'] = "010/110/010/010/111", ['2'] = "110/001/010/100/111",
            ['3'] = "110/001/010/001/110", ['4'] = "101/101/111/001/001",
        };

        static float PixelWidth(string text, float px)
        {
            int cols = 0;
            foreach (char ch in text) cols += (Glyphs.TryGetValue(ch, out string g) ? g.IndexOf('/') : 2) + 1;
            return Mathf.Max(0, cols - 1) * px;
        }

        /// <summary>Pixel text centred on c, facing the way rot turns -Z (yaw -90 faces +X: a wall on the left of the road).</summary>
        static void PixelTextFacing(ComicMesh m, string text, Vector3 c, Quaternion rot, float px, Color colour)
        {
            var local = new ComicMesh();
            PixelText(local, text, 0f, 0f, 0f, px, colour);
            m.Add(local, c, rot, Vector3.one);
        }

        /// <summary>Pixel text facing -Z (towards the rider), centred on (cx, cy) at z.</summary>
        static void PixelText(ComicMesh m, string text, float cx, float cy, float z, float px, Color c)
        {
            m.Surface = ComicSurface.Plain;
            float x = cx - PixelWidth(text, px) * .5f, y0 = cy + 2.5f * px;
            foreach (char ch in text)
            {
                if (!Glyphs.TryGetValue(ch, out string g)) { x += 3 * px; continue; }
                string[] rows = g.Split('/');
                for (int row = 0; row < rows.Length; row++)
                    for (int col = 0; col < rows[row].Length; col++)
                    {
                        if (rows[row][col] != '1') continue;
                        float qx = x + col * px, qy = y0 - (row + 1) * px;
                        m.Quad(V(qx + px, qy, z), V(qx, qy, z), V(qx, qy + px, z), V(qx + px, qy + px, z), c, false);
                    }
                x += (rows[0].Length + 1) * px;
            }
        }

        // ================================================================================== shared buildings

        /// <summary>A parked car (no brand): body, cabin with dark glass, ink bumpers and wheels. Front at +Z for yaw 0.</summary>
        static void ParkedCar(ComicMesh m, Vector3 p, float yaw, Color c)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + q * V(0, .62f, 0), V(1.8f, .62f, 4.3f), c, q);
            m.Box(p + q * V(0, 1.2f, -.25f), V(1.62f, .56f, 2.2f), Mix(c, White, .12f), q);
            m.Surface = ComicSurface.Glass;
            m.Box(p + q * V(0, 1.2f, -.25f), V(1.66f, .4f, 1.9f), GlassDark, q);
            m.Box(p + q * V(0, 1.2f, -.25f), V(1.4f, .42f, 2.24f), GlassDark, q);
            m.Surface = ComicSurface.Rubber;
            m.Box(p + q * V(0, .3f, 0), V(1.84f, .18f, 4.4f), Ink, q);
            foreach (float z in new[] { -1.35f, 1.35f }) foreach (float x in new[] { -.92f, .92f })
                m.Cylinder(p + q * V(x, .33f, z), .33f, .18f, Ink, 8, 1f, q * Quaternion.Euler(0, 0, 90));
        }

        /// <summary>An animal walker (kind Person, animal, never punchable). Facing -Z, feet at the pivot.</summary>
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

        static GameObject Cyclist() => AssetDatabase.LoadAssetAtPath<GameObject>(ComicCityBuilder.Prefabs + "/Districts/MountsBay/PED_MountsBay_Cyclist.prefab");

        /// <summary>A two-legged bird built from boxes: body, neck, head, beak, tail, legs. Facing -Z.</summary>
        static ComicMesh Bird(float s, Color body, Color wing, Color neck, Color head, Color beak, float beakLen, float neckLen, Color legs, float legLen, bool curvedBeak = false)
        {
            var m = new ComicMesh();
            m.Surface = ComicSurface.Fabric;
            m.Box(V(0, legLen + .2f * s, 0), V(.3f * s, .3f * s, .55f * s), body, Quaternion.Euler(-8, 0, 0));
            m.Box(V(0, legLen + .26f * s, .08f * s), V(.34f * s, .2f * s, .4f * s), wing, Quaternion.Euler(-8, 0, 0));
            m.Box(V(0, legLen + .24f * s, .36f * s), V(.2f * s, .1f * s, .25f * s), wing, Quaternion.Euler(-20, 0, 0));
            Vector3 neckBase = V(0, legLen + .3f * s, -.22f * s), headPos = neckBase + V(0, neckLen, -.08f * s - neckLen * .25f);
            m.Beam(neckBase, headPos, .1f * s, neck);
            m.Box(headPos + V(0, .04f * s, -.02f * s), V(.15f * s, .15f * s, .18f * s), head);
            m.Surface = ComicSurface.Plastic;
            if (curvedBeak)
            {
                m.Beam(headPos + V(0, .02f * s, -.08f * s), headPos + V(0, -.04f * s, -.08f * s - beakLen * .6f), .05f * s, beak);
                m.Beam(headPos + V(0, -.04f * s, -.08f * s - beakLen * .6f), headPos + V(0, -.16f * s, -.08f * s - beakLen), .04f * s, beak);
            }
            else m.Box(headPos + V(0, .02f * s, -.09f * s - beakLen * .5f), V(.07f * s, .06f * s, beakLen), beak);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(headPos + V(.08f * s, .07f * s, -.04f * s), V(.02f, .035f * s, .035f * s), Ink);
            m.Box(headPos + V(-.08f * s, .07f * s, -.04f * s), V(.02f, .035f * s, .035f * s), Ink);
            foreach (float x in new[] { -.08f * s, .08f * s })
            {
                m.Box(V(x, legLen * .5f, .02f * s), V(.035f * s + .01f, legLen, .035f * s + .01f), legs);
                m.Box(V(x, .02f, -.04f * s), V(.09f * s, .04f, .16f * s), legs);
            }
            return m;
        }
    }
}
