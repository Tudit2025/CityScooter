using System;
using System.Collections.Generic;
using CityScooter.EditorTools.Art;
using CityScooter.Rendering;
using UnityEngine;
using K = CityScooter.EditorTools.Districts.CbdQuayKit;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// The east line's shared corridor kit (v0.6): ONE Midland line railway beside the road through all 14 districts
    /// (McIver ... Midland), so that any two chunks of the line join without a break. The standard and its rules are in
    /// docs/v0.6-polish.md, section "东线走廊标准". Read that first.
    ///
    /// Conventions:
    /// - side: -1 = the railway is on the left of the road (-X; McIver to Bassendean), +1 = on the right (Success Hill to
    ///   Midland). <see cref="SideOf"/> gives it for a district id.
    /// - x values are metres from the ROAD CENTRE LINE (always positive; the kit multiplies by side). The sidewalk's outer
    ///   edge is at 8.
    /// - y values are metres above the road surface at that z ("flat road space"): everything goes into the chunk's sheared
    ///   meshes (CqChunkDraft.Road: no ink; SlopedFrontage: ink and shadows), so the corridor follows the chunk's slope.
    /// - Scenery only: nothing here adds an Obstacle; the only colliders are the chunk's boundary boxes (people walls).
    /// - CbdQuayKit.Begin must have been called (the kit uses its recipe colours and helpers).
    /// Members that take a CqChunkDraft or a ComicMesh are internal because those types are (same editor assembly).
    /// </summary>
    public static class EastLineKit
    {
        // ================================================================== the standard cross-section (do not change)

        public const int Left = -1, Right = 1;
        /// <summary>The black palisade fence: its line, and its height above the sidewalk (top at 2.0 above the road).</summary>
        public const float FenceX = 8.5f, FenceHeight = 1.8f;
        /// <summary>The gravel verge runs from the sidewalk edge (8) to BedNear at VergeTop; the ballast from BedNear to
        /// BedFar at BedTop.</summary>
        public const float BedNear = 9.6f, BedFar = 22.9f, BedTop = .1f, VergeTop = .22f;
        /// <summary>Track centres, the rail head, and half the (narrow) gauge.</summary>
        public const float Track1 = 14f, Track2 = 18.5f, RailTop = .3f, RailOffset = .55f;
        /// <summary>The overhead-line portal: one in every 40 m at z = 20 (20, 60, ... in longer chunks).</summary>
        public const float MastNear = 11.25f, MastFar = 21.25f, PortalTop = 7.6f, PortalZ = 20f, PortalEvery = 40f;
        /// <summary>Contact wire over each track centre; the messenger wire hangs from MessengerAtPortal at a portal down
        /// to MessengerMidSpan half way between two (which is where chunks meet).</summary>
        public const float ContactWire = 5.6f, MessengerAtPortal = 6.85f, MessengerMidSpan = 6.05f;
        /// <summary>The light back fence, and where the standard corridor ends: beyond CorridorEnd is the district's own.</summary>
        public const float BackFenceX = 23.3f, BackFenceHeight = 1.8f, CorridorEnd = 23.6f;
        /// <summary>Platforms: top above the road, and the gap from a track centre to the platform edge.</summary>
        public const float PlatformTop = 1.25f, PlatformEdge = 1.55f;
        public const float CarLength = 19f, CarGap = .5f, CarWidth = 2.9f;
        /// <summary>The street edge on the other side in entry / exit chunks: the low limestone wall, the grass verge behind
        /// it, and the jacarandas (one species) on a fixed 10 m rhythm: z = 5, 15, 25, 35.</summary>
        public const float EdgeWallHeight = .55f, EdgeVergeTo = 12f, EdgeTreeX = 10.2f, EdgeTreeEvery = 10f, EdgeTreeFirst = 5f;
        /// <summary>The band of pale setts across the street where a district's own paving meets the boundary surface.</summary>
        public const float ThresholdWidth = .6f;

        static readonly float[] StandardTracks = { Track1, Track2 };

        // ---- colours (sRGB)
        public static readonly Color Ballast = K.Hex("9D9A92"), BallastDark = K.Hex("86837C"), Verge = K.Hex("A9A294"), Sleeper = K.Hex("6E6257");
        public static readonly Color RailSteel = K.Hex("4E545C"), RailShine = K.Hex("B9C0C6"), FenceBlack = K.Hex("2A2F36"), BackFenceGrey = K.Hex("8E979E");
        public static readonly Color MastGrey = K.Hex("A9B0B6"), Insulator = K.Hex("7A3B2A"), Wire = K.Hex("2A2F36"), Girder = K.Hex("5A6068");
        public static readonly Color Stainless = K.Hex("CDD2D6"), StainlessDark = K.Hex("9AA2AA"), TransGreen = K.Hex("1E9E4A"), SkirtRed = K.Hex("D8452B"), DoorYellow = K.Hex("F2D03A");
        public static readonly Color PlatformPaver = K.Hex("A8553F"), PlatformFace = K.Hex("B9B5AA"), Tactile = K.Hex("F0D060");
        public static readonly Color Limestone = K.Hex("E2D2A6"), LimestoneCap = K.Hex("F0E6CA"), LimestoneDark = K.Hex("BFA978"), VergeGrass = K.Hex("6DA544");
        public static readonly Color Jacaranda = K.Hex("8F6FD0"), JacarandaLight = K.Hex("B49AE6"), JacarandaDark = K.Hex("7A5CBC"), JacarandaBark = K.Hex("6B5A4E");
        /// <summary>The boundary street: pale flagstone sidewalks and kerb caps (the road is the recipe's asphalt).</summary>
        public static readonly Color FlagPaving = K.Hex("D3CFC6"), KerbCap = K.Hex("EDE8DC"), Setts = K.Hex("E4DED0"), SettsJoint = K.Hex("A8A294");
        /// <summary>The line's shared path (Principal Shared Path): red asphalt.</summary>
        public static readonly Color PathRed = K.Hex("B5553F");
        static readonly Color White = K.Hex("F6F4EE"), SteelDark = K.Hex("3A4148"), PlantGrey = K.Hex("8F969C"), GlassDark = K.Hex("23404E"), ConcreteDark = K.Hex("A19D93"), SafetyOrange = K.Hex("F28C28");
        static Color Ink => K.R.ink;

        /// <summary>Which side of the road the railway is on in a district: left from McIver to Bassendean, right from
        /// Success Hill to Midland (the line crosses over the road in Success Hill's entry chunk).</summary>
        public static int SideOf(string districtId)
        {
            switch (districtId)
            {
                case "successhill": case "guildford": case "eastguildford": case "woodbridge": case "midland": return Right;
                default: return Left;
            }
        }

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static float F(System.Random r, float a, float b) => Mathf.Lerp(a, b, (float)r.NextDouble());
        static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, t);

        // ================================================================== the corridor

        /// <summary>What to leave out of, or add to, the standard corridor in one call of <see cref="Corridor"/>.</summary>
        internal sealed class CorridorOptions
        {
            /// <summary>The parts. Leave one out when your own landmark draws it (a platform instead of the fence, a
            /// viaduct instead of the ground and tracks). BackFence is the back fence of the plain corridor;
            /// WideBackFence one behind a widened bed (at BedTo + 0.4).</summary>
            public bool Ground = true, Fence = true, Tracks = true, Wires = true, Portals = true, BackFence = true, WideBackFence = false, Wall = true;
            /// <summary>Nothing at all between these z values (your station, viaduct, bridge or depot replaces it).</summary>
            public float GapFrom = -1f, GapTo = -1f;
            /// <summary>No near fence between these z values (a level crossing, a station entrance).</summary>
            public float FenceGapFrom = -1f, FenceGapTo = -1f;
            /// <summary>No ground between these z values: the tracks, wires and fence still run across (a canal, a drain
            /// or a road underneath; add <see cref="Girders"/>).</summary>
            public float GroundGapFrom = -1f, GroundGapTo = -1f;
            /// <summary>More tracks beyond the standard pair (centre lines, 27.5 or more from the road centre): wired, and
            /// unwired (sidings, the standard gauge line). The ballast is widened to BedTo for them, and there is no
            /// (plain) back fence beside the widened bed.</summary>
            public float[] ExtraTracks, UnwiredTracks;
            public float BedTo = BedFar;
            /// <summary>Widen the bed to BedTo only between these z values (a yard, a junction, a siding in the middle of a
            /// chunk); outside them the corridor is the plain standard with its back fence. Below 0 = the whole call.</summary>
            public float WideFrom = -1f, WideTo = -1f;
            /// <summary>The far mast of the portals (move it out when extra tracks are wired).</summary>
            public float FarMast = MastFar;
            /// <summary>Ground colour from the back edge out to 80 m. Leave empty when the district draws its own.</summary>
            public Color? Beyond;
            internal CorridorOptions Copy() => (CorridorOptions)MemberwiseClone();
        }

        static readonly CorridorOptions Plain = new CorridorOptions();

        /// <summary>
        /// The standard corridor on <paramref name="side"/> from z0 to z1 (z1 below 0 = the chunk's end): gravel verge,
        /// black palisade fence, ballast, two tracks, contact and messenger wires, the portal at z = 20 (+ 40 k), the back
        /// fence, and the boundary box that keeps people out. Call it for the whole chunk; use the options to leave parts
        /// out where a landmark replaces them. Two calls with touching z ranges join without a gap (the fence gets a
        /// doubled post there: prefer one call with WideFrom / WideTo or the gap options).
        /// </summary>
        internal static void Corridor(CqChunkDraft d, int side, float z0 = 0f, float z1 = -1f, CorridorOptions o = null)
        {
            o = o ?? Plain;
            if (z1 < 0f) z1 = d.Length;
            if (z1 - z0 < .01f) return;
            if (o.GapFrom >= 0f && o.GapTo > o.GapFrom)
            {
                CorridorOptions p = o.Copy();
                p.GapFrom = p.GapTo = -1f;
                if (o.GapFrom > z0 + .01f) Corridor(d, side, z0, Mathf.Min(z1, o.GapFrom), p);
                if (o.GapTo < z1 - .01f) Corridor(d, side, Mathf.Max(z0, o.GapTo), z1, p);
                return;
            }
            float bedTo = Mathf.Max(o.BedTo, BedFar);
            // [w0, w1]: where the bed is widened; the rest of [z0, z1] is the plain standard
            bool wide = bedTo > BedFar + .01f;
            float w0 = !wide ? z1 : o.WideFrom >= 0f ? Mathf.Clamp(o.WideFrom, z0, z1) : z0, w1 = !wide ? z1 : o.WideTo >= 0f ? Mathf.Clamp(o.WideTo, w0, z1) : z1;
            if (o.Ground)
                foreach (Vector2 run in Runs(z0, z1, o.GroundGapFrom, o.GroundGapTo))
                {
                    if (Mathf.Min(run.y, w0) - run.x > .01f) CorridorGround(d, side, run.x, Mathf.Min(run.y, w0), BedFar, o.Beyond);
                    if (Mathf.Min(run.y, w1) - Mathf.Max(run.x, w0) > .01f) CorridorGround(d, side, Mathf.Max(run.x, w0), Mathf.Min(run.y, w1), bedTo, o.Beyond);
                    if (run.y - Mathf.Max(run.x, w1) > .01f) CorridorGround(d, side, Mathf.Max(run.x, w1), run.y, BedFar, o.Beyond);
                }
            if (o.Fence)
                foreach (Vector2 run in Runs(z0, z1, o.FenceGapFrom, o.FenceGapTo))
                    Fence(d, side, run.x, run.y);
            var wired = new List<float>(StandardTracks);
            if (o.ExtraTracks != null) wired.AddRange(o.ExtraTracks);
            if (o.Tracks)
            {
                foreach (float t in wired) Track(d, side, t, z0, z1);
                if (o.UnwiredTracks != null) foreach (float t in o.UnwiredTracks) Track(d, side, t, z0, z1);
            }
            if (o.Wires) foreach (float t in wired) Wires(d, side, t, z0, z1);
            if (o.Portals)
                for (float z = PortalZ; z < d.Length; z += PortalEvery)
                    if (z >= z0 - .001f && z < z1 - .001f) Portal(d.SlopedFrontage, side, z, MastNear, o.FarMast, 0f, wired.ToArray());
            if (o.BackFence)
            {
                BackFence(d, side, z0, w0);
                BackFence(d, side, w1, z1);
            }
            if (o.WideBackFence) BackFence(d, side, w0, w1, bedTo + .4f);
            if (o.Wall) PeopleWall(d, side, z0, z1);
        }

        /// <summary>[z0, z1] without [gap0, gap1] (0, 1 or 2 runs).</summary>
        static List<Vector2> Runs(float z0, float z1, float gap0, float gap1)
        {
            var runs = new List<Vector2>();
            if (gap0 < 0f || gap1 <= gap0 || gap1 <= z0 || gap0 >= z1) { runs.Add(new Vector2(z0, z1)); return runs; }
            if (gap0 > z0 + .01f) runs.Add(new Vector2(z0, gap0));
            if (gap1 < z1 - .01f) runs.Add(new Vector2(gap1, z1));
            return runs;
        }

        /// <summary>A slab of ground from xa to xb (metres from the road centre line) with its top at <paramref name="top"/>
        /// above the road: closed boxes down to -2.4, so gaps in the road or the sidewalk never show a hole.</summary>
        internal static void Ground(CqChunkDraft d, int side, float z0, float z1, float xa, float xb, float top, Color c, ComicSurface surface = ComicSurface.Concrete)
        {
            d.Road.Surface = surface;
            K.SideBox(d.Road, side, z0, z1, -2.4f, top, xa - K.Edge, xb - K.Edge, c);
        }

        /// <summary>The corridor's ground: gravel verge 8 .. 9.6, ballast 9.6 .. bedTo, a gravel strip behind it, and
        /// (with a colour) the land beyond out to 80 m.</summary>
        internal static void CorridorGround(CqChunkDraft d, int side, float z0, float z1, float bedTo = BedFar, Color? beyond = null)
        {
            Ground(d, side, z0, z1, K.Edge, BedNear, VergeTop, Verge);
            Ground(d, side, z0, z1, BedNear, bedTo, BedTop, Ballast);
            Ground(d, side, z0, z1, bedTo, bedTo + .7f, VergeTop, Verge);
            if (beyond.HasValue) Ground(d, side, z0, z1, bedTo + .7f, 80f, VergeTop, beyond.Value);
        }

        /// <summary>The boundary box along the corridor (keeps walkers out of the railway): from 8.4 to 38.4.</summary>
        internal static void PeopleWall(CqChunkDraft d, int side, float z0, float z1, float depth = 30f)
        {
            float zc = (z0 + z1) * .5f;
            d.Walls.Add(new Bounds(new Vector3(side * (K.Edge + .4f + depth * .5f), d.Y(zc) + 2f, zc), new Vector3(depth, 9f, z1 - z0)));
        }

        // ---------------------------------------------------------------- fences

        /// <summary>The line's fence: a black steel palisade (posts on the chunk's 4 m grid with ink, two rails, pointed
        /// pales every 0.4 m facing the road). Its base is on the sidewalk level (0.2) unless baseY says otherwise.</summary>
        internal static void Fence(CqChunkDraft d, int side, float z0, float z1, float x = FenceX, float height = FenceHeight, float baseY = K.Kerb)
        {
            if (z1 - z0 < .3f) return;
            ComicMesh f = d.SlopedFrontage, r = d.Road;
            float sx = side * x, face = sx - side * .035f;
            Vector3 n = new Vector3(-side, 0, 0);
            f.Surface = ComicSurface.PaintedMetal;
            var posts = new List<float> { z0 + .05f };
            for (float z = Mathf.Ceil((z0 + .6f) / 4f) * 4f; z < z1 - .6f; z += 4f) posts.Add(z);
            posts.Add(z1 - .05f);
            foreach (float z in posts) f.Box(V(sx, baseY + height * .5f + .05f, z), V(.1f, height + .1f, .1f), FenceBlack);
            r.Surface = ComicSurface.PaintedMetal;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                foreach (float t in new[] { .2f, .85f })
                {
                    float y = baseY + height * t;
                    K.Face(r, V(face, y - .035f, z), V(face, y - .035f, e), V(face, y + .035f, e), V(face, y + .035f, z), FenceBlack, n, false);
                    K.Face(r, V(sx - .03f, y + .035f, z), V(sx - .03f, y + .035f, e), V(sx + .03f, y + .035f, e), V(sx + .03f, y + .035f, z), FenceBlack, Vector3.up, false);
                }
            }
            for (float z = Mathf.Ceil((z0 - .2f) / .4f) * .4f + .2f; z < z1 - .1f; z += .4f)
            {
                if (z < z0 + .1f) continue;
                K.Face(r, V(face, baseY + .08f, z - .045f), V(face, baseY + .08f, z + .045f), V(face, baseY + height, z + .045f), V(face, baseY + height, z - .045f), FenceBlack, n, false);
                K.TriFacing(r, V(face, baseY + height, z - .045f), V(face, baseY + height, z + .045f), V(face, baseY + height + .12f, z), FenceBlack, n);
            }
        }

        /// <summary>The light fence along the back of the corridor: galvanised posts and three wires (no ink).</summary>
        internal static void BackFence(CqChunkDraft d, int side, float z0, float z1, float x = BackFenceX, float height = BackFenceHeight)
        {
            if (z1 - z0 < .3f) return;
            ComicMesh r = d.Road;
            float sx = side * x, y = VergeTop;
            Vector3 n = new Vector3(-side, 0, 0);
            r.Surface = ComicSurface.Metal;
            for (float z = Mathf.Ceil(z0 / 4f) * 4f; z < z1 + .01f; z += 4f)
                K.Face(r, V(sx, y, z - .05f), V(sx, y, z + .05f), V(sx, y + height + .1f, z + .05f), V(sx, y + height + .1f, z - .05f), BackFenceGrey, n, false);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                foreach (float t in new[] { .15f, .55f, .98f })
                    K.Face(r, V(sx, y + height * t - .03f, z), V(sx, y + height * t - .03f, e), V(sx, y + height * t + .03f, e), V(sx, y + height * t + .03f, z), BackFenceGrey, n, false);
            }
        }

        // ---------------------------------------------------------------- tracks

        /// <summary>One straight track centred x metres from the road centre line on <paramref name="side"/>.</summary>
        internal static void Track(CqChunkDraft d, int side, float x, float z0, float z1) =>
            TrackPath(d.Road, z0, z1, z => side * x, null, true, 4f);

        /// <summary>A track that moves sideways from xa (at z0) to xb (at z1) on a smooth S: crossovers, the lead into a
        /// siding, a track swinging round an island platform.</summary>
        internal static void TrackShift(CqChunkDraft d, int side, float xa, float xb, float z0, float z1) =>
            TrackPath(d.Road, z0, z1, z => side * Mathf.Lerp(xa, xb, Smooth((z - z0) / (z1 - z0))));

        internal static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        /// <summary>
        /// A track along any path (flat road space; add to a sheared mesh): x(z) is the SIGNED centre line (negative =
        /// left of the road), lift(z) the height above the standard level (0 = rail head 0.3 above the road). Darker bed,
        /// timber-brown sleepers every 1.25 m on the chunk's grid, two rails. For viaducts and the Success Hill
        /// cross-over; pass bed = false on a deck.
        /// </summary>
        internal static void TrackPath(ComicMesh m, float z0, float z1, Func<float, float> x, Func<float, float> lift = null, bool bed = true, float step = 2f)
        {
            if (z1 - z0 < .01f) return;
            float Y(float z) => lift != null ? lift(z) : 0f;
            int n = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / step - .001f));
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(z0, z1, (float)i / n), b = Mathf.Lerp(z0, z1, (float)(i + 1) / n), xa = x(a), xb = x(b), ya = Y(a), yb = Y(b);
                if (bed)
                {
                    m.Surface = ComicSurface.Concrete;
                    Strip(m, xa - 1.55f, xa + 1.55f, xb - 1.55f, xb + 1.55f, a, b, ya + BedTop + .015f, yb + BedTop + .015f, BallastDark);
                }
                m.Surface = ComicSurface.Metal;
                foreach (float o in new[] { -RailOffset, RailOffset })
                {
                    Strip(m, xa + o - .05f, xa + o + .05f, xb + o - .05f, xb + o + .05f, a, b, ya + RailTop, yb + RailTop, RailShine);
                    foreach (float e in new[] { -.05f, .05f })
                        K.Face(m, V(xa + o + e, ya + BedTop + .03f, a), V(xb + o + e, yb + BedTop + .03f, b), V(xb + o + e, yb + RailTop, b), V(xa + o + e, ya + RailTop, a), RailSteel, new Vector3(Mathf.Sign(e), 0, 0), false);
                }
            }
            m.Surface = ComicSurface.Wood;
            for (float z = Mathf.Ceil((z0 - .3f) / 1.25f - .001f) * 1.25f + .3f; z < z1 - .3f; z += 1.25f)
            {
                float xc = x(z + .14f), y = Y(z + .14f) + BedTop + .03f;
                K.Face(m, V(xc - 1.2f, y, z), V(xc - 1.2f, y, z + .28f), V(xc + 1.2f, y, z + .28f), V(xc + 1.2f, y, z), Sleeper, Vector3.up, false);
            }
        }

        /// <summary>A flat strip between (l0..r0 at z0, height y0) and (l1..r1 at z1, height y1), facing up, no ink.</summary>
        static void Strip(ComicMesh m, float l0, float r0, float l1, float r1, float z0, float z1, float y0, float y1, Color c) =>
            K.Face(m, V(l0, y0, z0), V(l1, y1, z1), V(r1, y1, z1), V(r0, y0, z0), c, Vector3.up, false);

        /// <summary>A buffer stop across the track at z (red frame, white beam): where a siding or the line ends.
        /// trackTowards = +1: the track runs on towards +Z from here (the stop is its near end); -1: the track comes from
        /// -Z and ends here (Midland: the end of the line). The braces stand on the side without track.</summary>
        internal static void BufferStop(ComicMesh m, int side, float x, float z, int trackTowards = 1, float lift = 0f)
        {
            float sx = side * x, back = -trackTowards;
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float o in new[] { -RailOffset, RailOffset })
            {
                m.Beam(V(sx + o, lift + RailTop, z + back * 1.6f), V(sx + o, lift + 1.5f, z), .14f, K.R.red);
                m.Box(V(sx + o, lift + .9f, z), V(.14f, 1.3f, .14f), K.R.red);
            }
            m.Box(V(sx, lift + 1.4f, z), V(2.4f, .3f, .2f), White);
            m.Box(V(sx, lift + 1.4f, z - back * .11f), V(.5f, .3f, .04f), K.R.red);
        }

        /// <summary>Steel plate girders carrying the tracks over a gap in the ground from z0 to z1 (a canal, a drain, a
        /// road below): two girders and a deck plate under each track. Use with CorridorOptions.GroundGapFrom / To.</summary>
        internal static void Girders(CqChunkDraft d, int side, float z0, float z1, params float[] tracks)
        {
            if (tracks == null || tracks.Length == 0) tracks = StandardTracks;
            ComicMesh m = d.SlopedFrontage;
            float zc = (z0 + z1) * .5f, len = z1 - z0;
            foreach (float t in tracks)
            {
                float sx = side * t;
                m.Surface = ComicSurface.PaintedMetal;
                foreach (float o in new[] { -1.25f, 1.25f }) m.Box(V(sx + o, BedTop - .45f, zc), V(.24f, 1.1f, len + 1f), Girder);
                m.Surface = ComicSurface.Metal;
                m.Box(V(sx, BedTop - .04f, zc), V(2.5f, .1f, len + .4f), BallastDark);
            }
        }

        // ---------------------------------------------------------------- overhead line

        static void Ribbon(ComicMesh m, Vector3 a, Vector3 b, float t, Color c)
        {
            K.Face(m, a, b, b + Vector3.up * t, a + Vector3.up * t, c, Vector3.right, false);
            K.Face(m, a, b, b + Vector3.up * t, a + Vector3.up * t, c, Vector3.left, false);
        }

        /// <summary>Height of the messenger wire above the road at z: MessengerAtPortal at every portal (z = 20 + 40 k),
        /// MessengerMidSpan half way.</summary>
        public static float MessengerAt(float z, float supportZ = PortalZ, float span = PortalEvery)
        {
            float t = Mathf.Repeat(z - supportZ, span) / span;
            return MessengerAtPortal - (MessengerAtPortal - MessengerMidSpan) * 4f * t * (1f - t);
        }

        /// <summary>The overhead line above the track at x: contact wire, messenger wire and droppers (no ink).</summary>
        internal static void Wires(CqChunkDraft d, int side, float x, float z0, float z1) => WirePath(d.Road, z0, z1, z => side * x);

        /// <summary>The overhead line along any path: x(z) signed, lift(z) above the standard level. Supports (where the
        /// messenger peaks) at supportZ + k * span: keep the standard 20 / 40 unless the wires hang from your own
        /// structure in the middle of a chunk; at the chunk's ends they must be back on the standard.</summary>
        internal static void WirePath(ComicMesh m, float z0, float z1, Func<float, float> x, Func<float, float> lift = null, float supportZ = PortalZ, float span = PortalEvery)
        {
            if (z1 - z0 < .01f) return;
            float Y(float z) => lift != null ? lift(z) : 0f;
            m.Surface = ComicSurface.Plain;
            var cuts = new List<float> { z0 };
            for (float z = Mathf.Ceil((z0 - supportZ) / 2f + .001f) * 2f + supportZ; z < z1 - .01f; z += 2f) cuts.Add(z);
            cuts.Add(z1);
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float a = cuts[i], b = cuts[i + 1];
                if (b - a < .01f) continue;
                float xa = x(a), xb = x(b);
                Ribbon(m, V(xa, Y(a) + ContactWire, a), V(xb, Y(b) + ContactWire, b), .05f, Wire);
                // the messenger peaks at a support: do not cut the corner there
                float ma = Y(a) + MessengerAt(a + .001f, supportZ, span), mb = Y(b) + MessengerAt(b - .001f, supportZ, span);
                Ribbon(m, V(xa, ma, a), V(xb, mb, b), .045f, Wire);
                if (i % 2 == 1 && b - a > 1f)
                {
                    float zm = (a + b) * .5f, xm = x(zm), y0 = Y(zm) + ContactWire;
                    Ribbon(m, V(xm, y0, zm), V(xm, y0, zm + .04f), Y(zm) + MessengerAt(zm, supportZ, span) - y0, Wire);
                }
            }
        }

        static void MastColumn(ComicMesh m, float x, float z, float top, float lift)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(V(x, lift + .35f, z), V(.6f, .5f, .6f), ConcreteDark);
            m.Surface = ComicSurface.Metal;
            m.Box(V(x, lift + top * .5f + .3f, z), V(.26f, top, .3f), MastGrey);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(x, lift + top + .34f, z), V(.34f, .08f, .38f), SteelDark);
        }

        /// <summary>The registration arm holding the contact wire of the track at x, hung from a support at xs.</summary>
        static void WireArm(ComicMesh m, float x, float xs, float z, float top, float lift)
        {
            m.Surface = ComicSurface.Metal;
            m.Box(V(xs, lift + (top + ContactWire + .5f) * .5f, z), V(.1f, top - ContactWire - .5f, .1f), MastGrey);
            m.Beam(V(xs, lift + MessengerAtPortal, z), V(x, lift + MessengerAtPortal, z), .07f, MastGrey);
            m.Beam(V(xs, lift + ContactWire + .55f, z), V(x, lift + ContactWire + .08f, z), .06f, MastGrey);
            m.Surface = ComicSurface.Plastic;
            m.Cylinder(V(Mathf.Lerp(xs, x, .35f), lift + MessengerAtPortal, z), .09f, .34f, Insulator, 6, 1f, Quaternion.Euler(0, 0, 90));
        }

        /// <summary>The standard portal at z on <paramref name="side"/>, into the chunk's SlopedFrontage.</summary>
        internal static void Portal(CqChunkDraft d, int side, float z) => Portal(d.SlopedFrontage, side, z, MastNear, MastFar, 0f, StandardTracks);

        /// <summary>A portal across the tracks at z: two galvanised masts (at nearMast and farMast from the road centre
        /// line), a lattice beam with its top at PortalTop, and an arm for each track centre in <paramref name="tracks"/>.
        /// lift raises it (a viaduct). Add to a sheared mesh.</summary>
        internal static void Portal(ComicMesh m, int side, float z, float nearMast, float farMast, float lift, params float[] tracks)
        {
            if (tracks == null || tracks.Length == 0) tracks = StandardTracks;
            float xa = side * nearMast, xb = side * farMast, y0 = lift + PortalTop - .7f, y1 = lift + PortalTop;
            MastColumn(m, xa, z, PortalTop, lift);
            MastColumn(m, xb, z, PortalTop, lift);
            m.Surface = ComicSurface.Metal;
            m.Box(V((xa + xb) * .5f, y1, z), V(Mathf.Abs(xa - xb) + .3f, .12f, .16f), MastGrey);
            m.Box(V((xa + xb) * .5f, y0, z), V(Mathf.Abs(xa - xb) + .3f, .12f, .16f), MastGrey);
            int n = Mathf.Max(3, Mathf.RoundToInt(Mathf.Abs(xa - xb) / 1.7f));
            for (int i = 0; i < n; i++)
            {
                float p0 = Mathf.Lerp(xa, xb, (float)i / n), p1 = Mathf.Lerp(xa, xb, (float)(i + 1) / n);
                m.Beam(V(p0, i % 2 == 0 ? y0 : y1, z), V(p1, i % 2 == 0 ? y1 : y0, z), .07f, MastGrey);
            }
            foreach (float t in tracks) WireArm(m, side * t, side * (t - 1.1f), z, PortalTop - .7f, lift);
        }

        /// <summary>A single mast at mastX with a cantilever over the tracks (where there is no room for the far mast:
        /// against a wall, on a platform). The mast must be nearer the road than the tracks.</summary>
        internal static void Mast(ComicMesh m, int side, float z, float mastX, float lift, params float[] tracks)
        {
            if (tracks == null || tracks.Length == 0) tracks = StandardTracks;
            float far = mastX;
            foreach (float t in tracks) far = Mathf.Max(far, t);
            float xm = side * mastX, xf = side * (far + .6f);
            MastColumn(m, xm, z, PortalTop, lift);
            m.Surface = ComicSurface.Metal;
            m.Beam(V(xm, lift + PortalTop - .5f, z), V(xf, lift + PortalTop - .5f, z), .12f, MastGrey);
            m.Beam(V(xm, lift + PortalTop + .25f, z), V(xf, lift + PortalTop - .42f, z), .06f, MastGrey);
            foreach (float t in tracks) WireArm(m, side * t, side * (t - .9f), z, PortalTop - .5f, lift);
        }

        // ---------------------------------------------------------------- platforms

        /// <summary>A platform from x0 to x1 (x0 nearer the road): red-brown pavers at PlatformTop, grey concrete faces, a
        /// pale coping and a yellow tactile strip along each track edge (near = the edge at x0, far = the edge at x1).
        /// A platform edge stands PlatformEdge (1.55) from the track centre.</summary>
        internal static void Platform(CqChunkDraft d, int side, float x0, float x1, float z0, float z1, bool near, bool far)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Paving;
            K.SideBox(m, side, z0, z1, .1f, PlatformTop, x0 - K.Edge, x1 - K.Edge, PlatformPaver);
            ComicMesh r = d.Road;
            Vector3 toRoad = new Vector3(-side, 0, 0);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f), xf = side * (x0 - .012f);
                r.Surface = ComicSurface.Concrete;
                K.Face(r, V(xf, .1f, z), V(xf, .1f, e), V(xf, PlatformTop - .14f, e), V(xf, PlatformTop - .14f, z), PlatformFace, toRoad, false);
                r.Surface = ComicSurface.Plain;
                foreach ((bool on, float x, float dir) in new[] { (near, x0, 1f), (far, x1, -1f) })
                {
                    if (!on) continue;
                    float y = PlatformTop + .012f;
                    K.Face(r, V(side * x, y, z), V(side * x, y, e), V(side * (x + dir * .18f), y, e), V(side * (x + dir * .18f), y, z), White, Vector3.up, false);
                    K.Face(r, V(side * (x + dir * .5f), y, z), V(side * (x + dir * .5f), y, e), V(side * (x + dir * .95f), y, e), V(side * (x + dir * .95f), y, z), Tactile, Vector3.up, false);
                }
            }
        }

        // ================================================================== trains

        /// <summary>How an electric set looks. Use <see cref="ASeries"/> and <see cref="BSeries"/> for the Transperth
        /// suburban sets; other stock (the Prospector) fills in its own colours.</summary>
        public sealed class TrainLook
        {
            public float CarLen = CarLength, Width = CarWidth;
            /// <summary>Door centres as fractions of the car length from its middle.</summary>
            public float[] Doors = { -.24f, .24f };
            public bool Raked, Ribs = true;
            public Color Body = Stainless, Nose = K.Hex("F6F4EE"), Mask = K.Hex("1A1E24"), Band = SkirtRed, Stripe = TransGreen, Skirt = Color.clear, DoorEdge = Color.clear;
        }

        /// <summary>A-series (two-car sets): boxy flat front, white cab end with a black windscreen surround and a red band
        /// under it, ribbed stainless sides, a thin green strip above the windows.</summary>
        public static TrainLook ASeries => new TrainLook();
        /// <summary>B-series (three-car sets): raked white nose with a black mask, green cab swoosh and roof edge,
        /// red-orange skirt, yellow door edges, ribbed stainless sides.</summary>
        public static TrainLook BSeries => new TrainLook { Raked = true, Skirt = SkirtRed, DoorEdge = DoorYellow };

        static void SideQuad(ComicMesh m, float xs, float z0, float z1, float y0, float y1, Color c, Vector3 n, bool ink = false) =>
            K.Face(m, V(xs, y0, z0), V(xs, y0, z1), V(xs, y1, z1), V(xs, y1, z0), c, n, ink);

        /// <summary>
        /// A set standing on the track x metres from the road centre line on <paramref name="side"/>, its end towards the
        /// rider (-Z) at z0, cars along +Z; the detailed side faces the road. Returns its length (cars * 19.5 - 0.5).
        /// lite: no wheels or roof pods (far tracks, depots). cabBack / cabFront: a cab at the -Z / +Z end (false where the
        /// set runs on into the next chunk). lift: for a track above the standard level. Add to a sheared mesh
        /// (SlopedFrontage; Landmark or Deep for far ones).
        /// </summary>
        internal static float Train(ComicMesh m, int side, float x, float z0, int cars, TrainLook k, bool lite = false, bool panto = true, bool cabBack = true, bool cabFront = true, float lift = 0f)
        {
            var t = new ComicMesh();
            float L = k.CarLen, W = k.Width;
            Vector3 n = new Vector3(-side, 0, 0);
            float Out(float e) => -side * (W * .5f + .012f + e);     // a plane e metres outside the body side that faces the road
            for (int i = 0; i < cars; i++)
            {
                float za = i * (L + CarGap), zb = za + L, zc = (za + zb) * .5f;
                t.Surface = ComicSurface.Metal;
                t.Box(V(0, 2.65f, zc), V(W, 2.8f, L), k.Body);
                t.Box(V(0, 4.14f, zc), V(W - .5f, .2f, L - .6f), Mix(k.Body, Ink, .28f));
                t.Surface = ComicSurface.PaintedMetal;
                t.Box(V(0, 1.05f, zc), V(W - .3f, .5f, L - 1f), Ink);
                foreach (float f in new[] { -.32f, .32f })
                {
                    float zbg = zc + f * L;
                    t.Box(V(0, .72f, zbg), V(2.2f, .55f, 3.4f), SteelDark);
                    if (lite) continue;
                    foreach (float dz in new[] { -.95f, .95f }) t.Cylinder(V(-side * 1.02f, .72f, zbg + dz), .42f, .14f, Ink, 8, 1f, Quaternion.Euler(0, 0, 90));
                }
                if (!lite)
                {
                    t.Surface = ComicSurface.Metal;
                    foreach (float f in new[] { -.22f, .22f }) t.Box(V(0, 4.38f, zc + f * L), V(W - 1.1f, .3f, 3.2f), PlantGrey);
                }
                // the side facing the road: ribs, window band between the doors, doors, stripes
                t.Surface = ComicSurface.Plain;
                if (k.Ribs) foreach (float y in new[] { 1.5f, 1.72f, 1.94f, 2.16f }) SideQuad(t, Out(0f), za + .3f, zb - .3f, y, y + .04f, Mix(k.Body, Ink, .3f), n);
                if (k.Skirt.a > 0f) SideQuad(t, Out(0f), za + .1f, zb - .1f, 1.25f, 1.42f, k.Skirt, n);
                if (k.Stripe.a > 0f) SideQuad(t, Out(0f), za + .1f, zb - .1f, k.Raked ? 3.82f : 3.56f, k.Raked ? 4.02f : 3.68f, k.Stripe, n);
                var cuts = new List<float> { za + 1.3f };
                foreach (float f in k.Doors) { cuts.Add(zc + f * L - 1f); cuts.Add(zc + f * L + 1f); }
                cuts.Add(zb - 1.3f);
                t.Surface = ComicSurface.Glass;
                for (int c = 0; c + 1 < cuts.Count; c += 2)
                    if (cuts[c + 1] - cuts[c] > .6f) SideQuad(t, Out(0f), cuts[c], cuts[c + 1], 2.5f, 3.4f, GlassDark, n, true);
                foreach (float f in k.Doors)
                {
                    float zd = zc + f * L;
                    t.Surface = ComicSurface.PaintedMetal;
                    SideQuad(t, Out(.004f), zd - .82f, zd + .82f, 1.3f, 3.55f, Mix(k.Body, White, .35f), n, true);
                    t.Surface = ComicSurface.Glass;
                    SideQuad(t, Out(.008f), zd - .62f, zd - .08f, 2.45f, 3.35f, GlassDark, n);
                    SideQuad(t, Out(.008f), zd + .08f, zd + .62f, 2.45f, 3.35f, GlassDark, n);
                    t.Surface = ComicSurface.Plain;
                    SideQuad(t, Out(.008f), zd - .02f, zd + .02f, 1.3f, 3.55f, Ink, n);
                    if (k.DoorEdge.a > 0f) { SideQuad(t, Out(.008f), zd - .82f, zd - .74f, 1.3f, 3.55f, k.DoorEdge, n); SideQuad(t, Out(.008f), zd + .74f, zd + .82f, 1.3f, 3.55f, k.DoorEdge, n); }
                }
                if (i == 0 && cabBack) Nose(t, 0f, za, -1f, k); else Gangway(t, 0f, za - .25f);
                if (i == cars - 1 && cabFront) Nose(t, 0f, zb, 1f, k);
                if (panto && i == 0 && !lite) Pantograph(t, 0f, zc + L * .3f);
            }
            m.Add(t, V(side * x, lift, z0), Quaternion.identity, Vector3.one);
            return cars * (L + CarGap) - CarGap;
        }

        /// <summary>The rubber gangway between two cars (also closes a car end that has no cab).</summary>
        internal static void Gangway(ComicMesh m, float x, float z)
        {
            m.Surface = ComicSurface.Rubber;
            m.Box(V(x, 2.55f, z), V(1.7f, 2.4f, .5f), K.Hex("2A2F36"));
        }

        /// <summary>A pantograph on the roof at z, its head just under the contact wire (x signed).</summary>
        internal static void Pantograph(ComicMesh m, float x, float z)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(x, 4.3f, z), V(1.2f, .12f, 1.7f), SteelDark);
            m.Beam(V(x, 4.36f, z - .6f), V(x, 4.98f, z + .3f), .07f, SteelDark);
            m.Beam(V(x, 4.98f, z + .3f), V(x, ContactWire - .08f, z - .3f), .06f, SteelDark);
            m.Box(V(x, ContactWire - .05f, z - .3f), V(1.5f, .06f, .3f), SteelDark);
        }

        /// <summary>A cab end at ze, pointing along dir (-1 = towards the rider); x signed. For a nose looking out of a
        /// shed door.</summary>
        internal static void Nose(ComicMesh m, float x, float ze, float dir, TrainLook k)
        {
            float W = k.Width, hw = W * .5f;
            Vector3 fwd = V(0, 0, dir);
            Vector3 P(float dx, float y, float dz) => V(x + dx, y, ze + dir * dz);
            if (!k.Raked)
            {
                m.Surface = ComicSurface.PaintedMetal;
                K.Face(m, P(-hw + .03f, 1.28f, .02f), P(hw - .03f, 1.28f, .02f), P(hw - .03f, 4.03f, .02f), P(-hw + .03f, 4.03f, .02f), k.Nose, fwd);
                K.Face(m, P(-hw + .1f, 1.95f, .03f), P(hw - .1f, 1.95f, .03f), P(hw - .1f, 2.38f, .03f), P(-hw + .1f, 2.38f, .03f), k.Band, fwd, false);
                m.Surface = ComicSurface.Glass;
                K.Face(m, P(-hw + .18f, 2.5f, .035f), P(hw - .18f, 2.5f, .035f), P(hw - .18f, 3.62f, .035f), P(-hw + .18f, 3.62f, .035f), k.Mask, fwd);
                K.Face(m, P(-.5f, 3.7f, .035f), P(.5f, 3.7f, .035f), P(.5f, 3.95f, .035f), P(-.5f, 3.95f, .035f), K.Hex("2A2F36"), fwd, false);
                m.Surface = ComicSurface.Plain;
                K.Face(m, P(-.4f, 3.76f, .04f), P(.4f, 3.76f, .04f), P(.4f, 3.89f, .04f), P(-.4f, 3.89f, .04f), SafetyOrange, fwd, false);
                foreach (float sx in new[] { -.95f, .95f }) K.Face(m, P(sx - .18f, 1.5f, .035f), P(sx + .18f, 1.5f, .035f), P(sx + .18f, 1.72f, .035f), P(sx - .18f, 1.72f, .035f), K.R.cream, fwd);
            }
            else
            {
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(P(0, 1.9f, .6f), V(W, 1.3f, 1.2f), k.Nose);
                K.Face(m, P(-hw, 1.26f, 1.21f), P(hw, 1.26f, 1.21f), P(hw, 1.56f, 1.21f), P(-hw, 1.56f, 1.21f), k.Band, fwd, false);
                foreach (float sx in new[] { -.95f, .95f }) K.Face(m, P(sx - .2f, 1.8f, 1.21f), P(sx + .2f, 1.8f, 1.21f), P(sx + .2f, 2f, 1.21f), P(sx - .2f, 2f, 1.21f), K.R.cream, fwd);
                // the raked windscreen, the cheeks and the roof cap
                m.Surface = ComicSurface.Glass;
                K.Face(m, P(-hw + .1f, 2.55f, 1.2f), P(hw - .1f, 2.55f, 1.2f), P(hw - .22f, 4f, .15f), P(-hw + .22f, 4f, .15f), k.Mask, (fwd + Vector3.up * .6f).normalized);
                m.Surface = ComicSurface.PaintedMetal;
                foreach (float sx in new[] { -1f, 1f })
                {
                    K.Face(m, P(sx * hw, 2.55f, 1.2f), P(sx * hw, 2.55f, 0f), P(sx * hw, 4.05f, 0f), P(sx * (hw - .12f), 4f, .15f), k.Nose, V(sx, 0, 0));
                    K.TriFacing(m, P(sx * (hw + .012f), 2.55f, 1.15f), P(sx * (hw + .012f), 2.55f, .05f), P(sx * (hw + .012f), 3.5f, .05f), k.Stripe.a > 0f ? k.Stripe : k.Band, V(sx, 0, 0));
                    K.Face(m, P(sx * hw, 2.55f, 1.2f), P(sx * (hw - .1f), 2.55f, 1.2f), P(sx * (hw - .22f), 4f, .15f), P(sx * (hw - .12f), 4f, .15f), k.Nose, (fwd + Vector3.up * .6f).normalized);
                }
                K.Face(m, P(-hw + .12f, 4f, .15f), P(hw - .12f, 4f, .15f), P(hw, 4.05f, 0f), P(-hw, 4.05f, 0f), k.Nose, Vector3.up);
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(P(0, 1.15f, k.Raked ? 1.45f : .3f), V(.4f, .3f, .7f), Ink);
        }

        // ================================================================== the street edge (the side without the railway)

        /// <summary>
        /// The line's street edge on <paramref name="side"/> (the side WITHOUT the railway) from z0 to z1, for the outer end
        /// of entry and exit chunks: a low limestone block wall along the sidewalk, a 4 m grass verge behind it (8 .. 12),
        /// jacarandas at 10.2 on the chunk's fixed rhythm (z = 5, 15, 25, 35), and the boundary box that keeps people out.
        /// Beyond 12 m is the district's own (its houses and shops, lower and sparser than in the middle chunks): give a
        /// ground colour to fill 12 .. 60, or leave it empty and draw your own. gapFrom / gapTo: an opening in the wall
        /// (a path, a driveway).
        /// </summary>
        internal static void StreetEdge(CqChunkDraft d, int side, float z0, float z1, Color? beyond = null, bool trees = true, float gapFrom = -1f, float gapTo = -1f)
        {
            if (z1 - z0 < .01f) return;
            Ground(d, side, z0, z1, K.Edge, EdgeVergeTo, VergeTop, VergeGrass, ComicSurface.Foliage);
            if (beyond.HasValue) Ground(d, side, z0, z1, EdgeVergeTo, 60f, VergeTop, beyond.Value, ComicSurface.Paving);
            ComicMesh w = d.SlopedFrontage, r = d.Road;
            float top = K.Kerb + EdgeWallHeight;
            foreach (Vector2 run in Runs(z0, z1, gapFrom, gapTo))
            {
                w.Surface = ComicSurface.Concrete;
                K.SideBox(w, side, run.x, run.y, -1f, top, .05f, .5f, Limestone);
                K.SideBox(w, side, run.x, run.y, top, top + .1f, 0f, .55f, LimestoneCap);
                // block joints on the face towards the road
                r.Surface = ComicSurface.Plain;
                float xf = side * (K.Edge + .05f - .012f);
                for (float z = Mathf.Ceil(run.x / 1.6f + .001f) * 1.6f; z < run.y - .2f; z += 1.6f)
                    K.Face(r, V(xf, K.Kerb + .02f, z), V(xf, K.Kerb + .02f, z + .05f), V(xf, top - .02f, z + .05f), V(xf, top - .02f, z), LimestoneDark, new Vector3(-side, 0, 0), false);
            }
            if (trees)
                for (float z = EdgeTreeFirst; z < d.Length; z += EdgeTreeEvery)
                {
                    if (z < z0 + .5f || z > z1 - .5f) continue;
                    StreetTree(d.Props, V(side * EdgeTreeX, d.Ground(z), z), 7.6f, Mathf.RoundToInt(z) + (side > 0 ? 3 : 0));
                    // fallen flowers under it
                    r.Surface = ComicSurface.Plain;
                    float y = VergeTop + .015f, x = side * EdgeTreeX;
                    K.Face(r, V(x - 1.7f, y, z), V(x, y, z + 2f), V(x + 1.7f, y, z), V(x, y, z - 2f), Mix(VergeGrass, JacarandaLight, .55f), Vector3.up, false);
                }
            float zc = (z0 + z1) * .5f;
            d.Walls.Add(new Bounds(new Vector3(side * (K.Edge + .4f + 6f), d.Y(zc) + 1.5f, zc), new Vector3(12f, 8f, z1 - z0)));
        }

        /// <summary>A faceted blob (7 x 4: lighter than ComicMesh.Ellipsoid).</summary>
        static void Blob(ComicMesh m, Vector3 c, Vector3 size, Color col)
        {
            const int sides = 7, rings = 4;
            Vector3 P(int ring, int s)
            {
                float a = Mathf.PI * ring / rings, b = Mathf.PI * 2f * s / sides;
                return c + Vector3.Scale(V(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b)), size) * .5f;
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++)
                {
                    if (r == 0) m.Tri(P(0, s), P(1, s + 1), P(1, s), col);
                    else if (r == rings - 1) m.Tri(P(r, s), P(r, s + 1), P(r + 1, s), col);
                    else m.Quad(P(r, s), P(r, s + 1), P(r + 1, s + 1), P(r + 1, s), col, false);
                }
        }

        static void Limb(ComicMesh m, Vector3 a, Vector3 b, float r0, float r1, Color c, int sides)
        {
            m.Cylinder((a + b) * .5f, r0, (b - a).magnitude, c, sides, r1 / r0, Quaternion.FromToRotation(Vector3.up, b - a));
        }

        /// <summary>The line's street tree: a jacaranda in November flower. A dark trunk forking low into three spreading
        /// limbs, a wide, flattish purple crown with lighter tops. Upright (add to Props), base at p.</summary>
        internal static void StreetTree(ComicMesh m, Vector3 p, float h = 7.6f, int seed = 0)
        {
            var r = new System.Random(seed * 53 + 11);
            float spin = F(r, 0f, 6.28f);
            Vector3 fork = p + V(F(r, -.25f, .25f), h * .36f, F(r, -.25f, .25f));
            Color barkDark = Mix(JacarandaBark, Ink, .25f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .05f, 0), .36f, .1f, Ink, 6);
            m.Surface = ComicSurface.Wood;
            Limb(m, p, fork, .21f, .16f, JacarandaBark, 6);
            for (int i = 0; i < 3; i++)
            {
                float a = spin + i * 2.094f + F(r, -.3f, .3f);
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 tip = fork + dir * h * F(r, .24f, .3f) + V(0, h * F(r, .26f, .34f), 0);
                m.Surface = ComicSurface.Wood;
                Limb(m, fork - V(0, .12f, 0), tip, .12f, .06f, i % 2 == 0 ? JacarandaBark : barkDark, 5);
                m.Surface = ComicSurface.Foliage;
                Vector3 size = V(h * F(r, .46f, .54f), h * F(r, .2f, .25f), h * F(r, .46f, .54f));
                Blob(m, tip + V(0, h * .03f, 0), size, i % 2 == 0 ? Jacaranda : JacarandaDark);
                Blob(m, tip + V(size.x * .1f, size.y * .3f, -size.z * .08f), size * .6f, JacarandaLight);
            }
            m.Surface = ComicSurface.Foliage;
            Blob(m, fork + V(0, h * .5f, 0), V(h * .58f, h * .26f, h * .58f), Jacaranda);
            Blob(m, fork + V(h * .05f, h * .58f, -h * .04f), V(h * .36f, h * .16f, h * .36f), JacarandaLight);
        }

        // ================================================================== the street surface and its transitions

        /// <summary>One paving of the standard street cross-section (road 8 m, sidewalks 4 m, kerb 0.2).</summary>
        internal sealed class StreetLook
        {
            public Color Road, Sidewalk, Cap;
            public ComicSurface RoadSurface = ComicSurface.Asphalt;
            /// <summary>A street: cream edge lines and the dashed double yellow centre line.</summary>
            public bool CentreLine;
            /// <summary>A shared path: pale edge lines and a dashed white centre line (set RoadType.PedestrianMall on the
            /// chunk yourself).</summary>
            public bool Path;

            /// <summary>The east line's boundary surface: grey asphalt, pale flagstone sidewalks. Entry chunks start with
            /// it at z = 0 and exit chunks end with it, always.</summary>
            public static StreetLook Boundary => new StreetLook { Road = K.R.asphalt, Sidewalk = FlagPaving, Cap = KerbCap, CentreLine = true };
            /// <summary>A street with the district's own sidewalk paving.</summary>
            public static StreetLook Street(Color sidewalk, Color? cap = null) => new StreetLook { Road = K.R.asphalt, Sidewalk = sidewalk, Cap = cap ?? KerbCap, CentreLine = true };
            /// <summary>The red shared path with the given side strips.</summary>
            public static StreetLook SharedPath(Color? sideStrips = null, Color? red = null) => new StreetLook { Road = red ?? PathRed, Sidewalk = sideStrips ?? FlagPaving, Cap = KerbCap, RoadSurface = ComicSurface.Paving, Path = true };
            /// <summary>Any other paving (a promenade, a mall): no lines.</summary>
            public static StreetLook Paved(Color road, Color sidewalk, Color cap) => new StreetLook { Road = road, Sidewalk = sidewalk, Cap = cap, RoadSurface = ComicSurface.Paving };
        }

        /// <summary>
        /// The street surface of a whole chunk (replaces CbdQuayKit.StreetSurface): look <paramref name="a"/> from z = 0,
        /// and, with <paramref name="b"/>, look b from z = at to the end with the threshold band between them (and, when
        /// one of the two is a shared path, the SHARED PATH / PATH ENDS markings and a zebra).
        /// Entry chunk: Street(d, StreetLook.Boundary, own, 12). Exit chunk: Street(d, own, StreetLook.Boundary, 28).
        /// A district whose own street is the boundary surface: Street(d, StreetLook.Boundary).
        /// skipFrom / skipTo: no road or sidewalk there (a road-wide gap).
        /// </summary>
        internal static void Street(CqChunkDraft d, StreetLook a, StreetLook b = null, float at = -1f, float skipFrom = -1f, float skipTo = -1f)
        {
            float L = d.Length;
            bool two = b != null && at > .01f && at < L - .01f;
            if (!two) Surface(d.Road, b != null && at <= .01f ? b : a, 0f, L, skipFrom, skipTo);
            else
            {
                Surface(d.Road, a, 0f, at, skipFrom, skipTo);
                Surface(d.Road, b, at, L, skipFrom, skipTo);
                if (b.Path && !a.Path) PathStart(d, at);
                else if (a.Path && !b.Path) PathEnd(d, at);
                else Threshold(d, at);
            }
            SlabEnd(d.Road, a, 0f);
            SlabEnd(d.Road, two || (b != null && at <= .01f) ? b : a, L);
        }

        static void Flat(ComicMesh m, float x0, float x1, float z0, float z1, float y, Color c) =>
            K.Face(m, V(x0, y, z0), V(x0, y, z1), V(x1, y, z1), V(x1, y, z0), c, Vector3.up, false);

        static bool Skipped(float z0, float z1, float skipFrom, float skipTo) => skipFrom >= 0f && z0 >= skipFrom - .01f && z1 <= skipTo + .01f;

        /// <summary>Dashes of length len every period (on the chunk's grid from phase) within [za, zb], cut at a gap.</summary>
        static void Dashes(float za, float zb, float period, float len, float phase, float skipFrom, float skipTo, Action<float, float> draw)
        {
            for (float z = Mathf.Floor((za - phase) / period) * period + phase; z < zb - .01f; z += period)
            {
                float s = Mathf.Max(z, za), e = Mathf.Min(z + len, zb);
                if (e - s < .05f) continue;
                if (skipFrom >= 0f && e > skipFrom && s < skipTo)
                {
                    if (s < skipFrom - .05f) draw(s, skipFrom);
                    if (e > skipTo + .05f) draw(skipTo, e);
                    continue;
                }
                draw(s, e);
            }
        }

        static void Surface(ComicMesh m, StreetLook k, float za, float zb, float skipFrom, float skipTo)
        {
            const float h = K.Kerb, sw = K.Side, w = K.Half;
            Color joint = Color.Lerp(K.R.ink, k.Sidewalk, .5f);
            var cuts = new List<float> { za };
            for (float z = Mathf.Floor(za / 4f) * 4f + 4f; z < zb - .01f; z += 4f) cuts.Add(z);
            cuts.Add(zb);
            if (skipFrom >= 0f) { if (skipFrom > za && skipFrom < zb) cuts.Add(skipFrom); if (skipTo > za && skipTo < zb) cuts.Add(skipTo); }
            cuts.Sort();
            for (int ci = 0; ci + 1 < cuts.Count; ci++)
            {
                float z0 = cuts[ci], z1 = cuts[ci + 1];
                if (z1 - z0 < .01f || Skipped(z0, z1, skipFrom, skipTo)) continue;
                m.Surface = k.RoadSurface;
                Flat(m, -w, w, z0, z1, 0f, k.Road);
                for (int s = -1; s <= 1; s += 2)
                {
                    float i0 = s * w, o0 = s * (w + sw);
                    Vector3 inward = new Vector3(-s, 0, 0), outward = new Vector3(s, 0, 0);
                    m.Surface = ComicSurface.Paving;
                    K.Face(m, V(i0, h, z0), V(i0, h, z1), V(o0, h, z1), V(o0, h, z0), k.Sidewalk, Vector3.up, false);
                    K.Face(m, V(i0, -.05f, z0), V(i0, -.05f, z1), V(i0, h, z1), V(i0, h, z0), k.Sidewalk, inward, false);
                    K.Face(m, V(o0, -2f, z0), V(o0, -2f, z1), V(o0, h, z1), V(o0, h, z0), k.Sidewalk, outward, false);
                    m.Surface = ComicSurface.Plain;
                    float c0 = s * (w + .24f), top = h + .04f;
                    K.Face(m, V(i0, top, z0), V(i0, top, z1), V(c0, top, z1), V(c0, top, z0), k.Cap, Vector3.up, false);
                    K.Face(m, V(i0, h, z0), V(i0, h, z1), V(i0, top, z1), V(i0, top, z0), k.Cap, inward, false);
                    K.Face(m, V(c0, h, z0), V(c0, h, z1), V(c0, top, z1), V(c0, top, z0), k.Cap, outward, false);
                    if (k.CentreLine) Flat(m, s * (w - .19f), s * (w - .11f), z0, z1, .0305f, K.R.cream);
                    if (k.Path) Flat(m, s * (w - .35f), s * (w - .25f), z0, z1, .025f, Mix(k.Road, White, .7f));
                    for (int j = 1; j < 4; j++) Flat(m, s * (w + j) - .0125f, s * (w + j) + .0125f, z0, z1, h + .025f, joint);
                }
                for (float z = Mathf.Ceil(z0 / 2f) * 2f; z < z1 - .01f; z += 2f)
                    for (int s = -1; s <= 1; s += 2) Flat(m, s * w, s * (w + sw), z - .0125f, z + .0125f, h + .025f, joint);
            }
            m.Surface = ComicSurface.Plain;
            if (k.CentreLine)
                foreach (int s in new[] { -1, 1 })
                    Dashes(za, zb, 5f, 3f, 0f, skipFrom, skipTo, (z0, z1) => Flat(m, s * .12f - .05f, s * .12f + .05f, z0, z1, .035f, K.R.yellow));
            if (k.Path) Dashes(za, zb, 5f, 2.6f, 1f, skipFrom, skipTo, (z0, z1) => Flat(m, -.07f, .07f, z0, z1, .03f, White));
        }

        /// <summary>Closed slab end (hidden by the neighbouring chunk).</summary>
        static void SlabEnd(ComicMesh m, StreetLook k, float ze)
        {
            const float h = K.Kerb, sw = K.Side, w = K.Half;
            Vector3 normal = ze <= 0f ? Vector3.back : Vector3.forward;
            m.Surface = k.RoadSurface;
            K.Face(m, V(-w, -.8f, ze), V(w, -.8f, ze), V(w, 0, ze), V(-w, 0, ze), k.Road, normal, false);
            m.Surface = ComicSurface.Paving;
            for (int s = -1; s <= 1; s += 2) K.Face(m, V(s * w, -2f, ze), V(s * (w + sw), -2f, ze), V(s * (w + sw), h, ze), V(s * w, h, ze), k.Sidewalk, normal, false);
        }

        /// <summary>The threshold band across the road and both sidewalks at z: 0.6 m of pale setts with a dark joint
        /// each side. Marks every change of paving (one paving before it, another after).</summary>
        internal static void Threshold(CqChunkDraft d, float z)
        {
            ComicMesh m = d.Road;
            const float w = K.Half, sw = K.Side, h = K.Kerb;
            float a = z - ThresholdWidth * .5f, b = z + ThresholdWidth * .5f;
            m.Surface = ComicSurface.Paving;
            Flat(m, -w, w, a, b, .038f, Setts);
            foreach (int s in new[] { -1, 1 }) Flat(m, s * w, s * (w + sw), a, b, h + .045f, Setts);
            m.Surface = ComicSurface.Plain;
            foreach (float e in new[] { a, b - .05f })
            {
                Flat(m, -w, w, e, e + .05f, .04f, SettsJoint);
                foreach (int s in new[] { -1, 1 }) Flat(m, s * w, s * (w + sw), e, e + .05f, h + .047f, SettsJoint);
            }
            for (float x = -w + 1f; x < w - .5f; x += 1f) Flat(m, x - .02f, x + .02f, a + .05f, b - .05f, .04f, SettsJoint);
        }

        /// <summary>A street becomes the shared path at z: zebra bars on the street side, the threshold band, SHARED PATH
        /// painted on the path (two lines; the first word is the far one, so it reads top to bottom on the screen).
        /// Leave 3.6 m before z and 8 m after it (without the room, the words are left out).</summary>
        internal static void PathStart(CqChunkDraft d, float z)
        {
            Zebra(d, z - 3.5f, z - .5f);
            Threshold(d, z);
            if (z + 8f <= d.Length + .01f) { RoadWord(d, "PATH", z + 1.3f); RoadWord(d, "SHARED", z + 4.7f); }
        }

        /// <summary>The shared path ends at z: PATH ENDS painted on the path, the threshold band, zebra bars on the street
        /// side. Leave 8 m before z and 3.6 m after it (without the room, the words are left out).</summary>
        internal static void PathEnd(CqChunkDraft d, float z)
        {
            if (z - 8f >= -.01f) { RoadWord(d, "ENDS", z - 7.4f); RoadWord(d, "PATH", z - 4f); }
            Threshold(d, z);
            Zebra(d, z + .5f, z + 3.5f);
        }

        /// <summary>White zebra bars on the road from z0 to z1 (0.5 m bars, 0.5 m apart, along the road).</summary>
        internal static void Zebra(CqChunkDraft d, float z0, float z1)
        {
            z0 = Mathf.Max(0f, z0); z1 = Mathf.Min(d.Length, z1);
            if (z1 - z0 < .5f) return;
            d.Road.Surface = ComicSurface.Plain;
            for (float x = -3.5f; x < 3.4f; x += 1f) Flat(d.Road, x, x + .5f, z0, z1, .036f, White);
        }

        // Pixel letters for road markings (rows top to bottom). Only what the line's markings need.
        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            ['A'] = "010/101/111/101/101", ['D'] = "110/101/101/101/110", ['E'] = "111/100/110/100/111", ['H'] = "101/101/111/101/101",
            ['N'] = "1001/1101/1011/1001/1001", ['P'] = "110/101/110/100/100", ['R'] = "110/101/110/101/101", ['S'] = "011/100/010/001/110",
            ['T'] = "111/010/010/010/010",
        };

        /// <summary>A word painted on the road, centred, its foot at z, read by a rider coming from -Z (letters 2.75 m
        /// long: stretched like real road lettering, so they still read from the chase camera).</summary>
        internal static void RoadWord(CqChunkDraft d, string text, float z, float px = .26f, float pz = .55f)
        {
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            int cols = 0;
            foreach (char ch in text) cols += (Glyphs.TryGetValue(ch, out string g) ? g.IndexOf('/') : 2) + 1;
            float x = -(cols - 1) * px * .5f;
            foreach (char ch in text)
            {
                if (!Glyphs.TryGetValue(ch, out string g)) { x += 3 * px; continue; }
                string[] rows = g.Split('/');
                for (int row = 0; row < rows.Length; row++)
                    for (int col = 0; col < rows[row].Length; col++)
                        if (rows[row][col] == '1') Flat(m, x + col * px, x + (col + 1) * px, z + (rows.Length - 1 - row) * pz, z + (rows.Length - row) * pz, .036f, White);
                x += (rows[0].Length + 1) * px;
            }
        }
    }
}
