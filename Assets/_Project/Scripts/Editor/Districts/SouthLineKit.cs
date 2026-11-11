using System;
using System.Collections.Generic;
using CityScooter.EditorTools.Art;
using CityScooter.Rendering;
using UnityEngine;
using K = CityScooter.EditorTools.Districts.CbdQuayKit;
using P = CityScooter.EditorTools.Districts.ParkRiverKit;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// The south line's shared corridor (v0.6, docs/v0.6-polish.md, the south line corridor standard): the Kwinana Freeway with the Mandurah
    /// line in its median, on the rider's LEFT (-X, the east side: the shared path runs down the freeway's west side)
    /// from Canning Bridge to Aubin Grove. Every chunk on the line starts and ends with the same cross-section, so any
    /// two chunks meet: same fence, same barriers, same lanes, same tracks, masts in the same two places of every 40 m
    /// chunk, the contact wires at one height. Stations, bridges and interchanges change it only in the middle.
    ///
    /// Everything is drawn FLAT (heights relative to the path at that z) and follows the chunk's slope: ground and
    /// markings go into the draft's Road mesh and fences / barriers / masts into SlopedFrontage (both sheared when the
    /// chunk is written); trees, cars and trains are sheared here and added to Deep / Landmark / Props.
    ///
    /// Use (inside a chunk draft, K.Begin and P.Init done):
    ///   SouthLineKit.Path(d, 0, 40);                               // the boundary path surface
    ///   SouthLineKit.Freeway(d, 0, 40);                            // the corridor, z = 0 to the end
    ///   SouthLineKit.Train(d, 0, 4, 38);                           // a B-series set on the near (northbound) track
    ///   SouthLineKit.StreetEdge(d, 1, 0, 16, 12f, 7, true);        // the other side of an entry chunk
    /// Leaving the freeway (a town centre, the next line): Shift = SouthLineKit.Ease(4, 40, 0, 50) moves the whole
    /// corridor away to the left along the chunk; Ease(0, 36, 50, 0) brings it back.
    /// </summary>
    public static class SouthLineKit
    {
        // ---- the standard cross-section: metres to the LEFT of the path centre line (x = -value), heights above the path
        /// <summary>The path fence (black posts, rails and wires), just behind the left verge.</summary>
        public const float FenceX = 8.45f, FenceTop = 2.1f;
        /// <summary>Jersey barrier between the dry verge and the freeway (centre line; 0.9 m high).</summary>
        public const float BarrierX = 11.4f, BarrierTop = .9f;
        /// <summary>Carriageway A (the near one: NORTHBOUND, traffic comes towards the rider) and B (southbound).</summary>
        public const float RoadA0 = 11.85f, RoadA1 = 23.1f, RoadB0 = 36.9f, RoadB1 = 48.3f, RoadY = .06f;
        /// <summary>The railway: ballast bed, the two track centre lines (A = near / northbound, B = far / southbound).</summary>
        public const float Rail0 = 24.3f, Rail1 = 36.9f, TrackA = 25.8f, TrackB = 35.4f, BallastY = .22f, RailTopY = .4f;
        /// <summary>Barriers either side of the railway (centre lines).</summary>
        public const float MedianBarrierA = 23.7f, MedianBarrierB = 37.25f;
        /// <summary>Overhead line: wire height, mast height, and where the two masts stand in every 40 m chunk.</summary>
        public const float WireY = 5.6f, MastTop = 6.5f, MastZ0 = 6f, MastZ1 = 26f;
        /// <summary>Island platform between the tracks (stations only).</summary>
        public const float PlatformA = 27.2f, PlatformB = 34f, PlatformY = 1f;
        /// <summary>Far verge beyond carriageway B, out to here.</summary>
        public const float FarVerge = 75f;
        /// <summary>Where a chunk changes between its own paving and the boundary path surface.</summary>
        public const float SurfaceChange = 14f;

        // ---- palette (sRGB)
        public static readonly Color PathGrey = K.Hex("5C636C"), VergeGrey = K.Hex("CBC7BC"), LineWhite = new Color(.95f, .95f, .93f);
        public static readonly Color FwyAsphalt = K.Hex("4B525B"), Barrier = K.Hex("D6D2C8"), Ballast = K.Hex("8C8378"), Sleeper = K.Hex("6C5B4A"),
            RailSteel = K.Hex("5E646C"), MastGrey = K.Hex("9AA1A8"), FenceInk = K.Hex("2A3038"), FenceWire = K.Hex("596069");
        public static readonly Color DryGrass = K.Hex("B9AA6E"), Lawn = K.Hex("79A64C"), LawnLight = K.Hex("8DB45A"), Peppermint = K.Hex("5F8B3A"),
            BushOlive = K.Hex("6E8A45"), Sand = K.Hex("E6D7AE");
        public static readonly Color Limestone = K.Hex("E2D4AF"), LimeCap = K.Hex("F1E9D2"), LimeJoint = K.Hex("BFA97C"), Concrete = K.Hex("D8D4CA"), ConcreteDark = K.Hex("8E8B85");
        public static readonly Color TrainWhite = K.Hex("F0F0EC"), TrainGreen = K.Hex("1F8A4F"), TrainBand = K.Hex("1E252C"), TrainRoof = K.Hex("9AA3AC"), TrainDoor = K.Hex("D5D8D6");
        public static readonly Color PlatformBrick = K.Hex("B5523B"), Tactile = K.Hex("9A9EA4"), CanopyWhite = K.Hex("F1F1EC"), CanopyUnder = K.Hex("9AA3AC"), SignNavy = K.Hex("1F3A6E");
        public static readonly Color WallSand = K.Hex("D7C3A0"), WallTerracotta = K.Hex("C0643E"), WallCream = K.Hex("E9DDC0");
        static readonly Color[] CarColours = { K.Hex("D8322A"), K.Hex("F2F2EC"), K.Hex("2F6FB5"), K.Hex("F2C14E"), K.Hex("4A5560"), K.Hex("8C9195"), K.Hex("2C3238") };

        /// <summary>What stands beyond the far carriageway (Open = bare verge: the chunk puts its own things there).</summary>
        public enum FarSide { Trees, NoiseWall, Bush, Open }

        /// <summary>How one piece of corridor differs from the standard. Shift and Lift must be 0 at a chunk's two ends
        /// unless the chunk really leaves the freeway (a town centre, the end of the line).</summary>
        public sealed class Corridor
        {
            /// <summary>Extra metres to the left at z (the whole corridor moves away from the path). null = 0.</summary>
            public Func<float, float> Shift;
            /// <summary>Metres above the path at z (an embankment, a bridge approach). null = 0.</summary>
            public Func<float, float> Lift;
            /// <summary>The freeway is on a bridge deck: no verges, a deck slab with a fascia and piers (water is the chunk's).</summary>
            public bool Deck;
            /// <summary>The path fence (off where a railing, a wall or a platform replaces it).</summary>
            public bool Fence = true;
            /// <summary>No verge and no fence between these z (a creek or drain passing under the path).</summary>
            public float GapFrom = -1f, GapTo = -1f;
            public FarSide Far = FarSide.Trees;
            public float FarTo = FarVerge;
            /// <summary>Parked-in-time cars on the two carriageways.</summary>
            public int Cars = 3;
            public int Seed = 1;
        }

        static readonly Corridor Standard = new Corridor();

        /// <summary>Rebuilds the four districts drawn with this kit (Canning Bridge, Murdoch, Cockburn Central, Aubin
        /// Grove). -executeMethod CityScooter.EditorTools.Districts.SouthLineKit.BuildBatch (works with -nographics).</summary>
        public static void BuildBatch()
        {
            try { CanningMurdochBuilder.Build(); CockburnAubinBuilder.Build(false); UnityEditor.EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); UnityEditor.EditorApplication.Exit(1); }
        }

        /// <summary>Height above the path of the left verge at x metres left of the centre line (between the path and
        /// the barrier), for planting things on the wedge when the corridor is shifted or lifted.</summary>
        public static float VergeY(Corridor o, float x, float z)
        {
            float shift = o != null && o.Shift != null ? o.Shift(z) : 0f, lift = o != null && o.Lift != null ? o.Lift(z) : 0f;
            return .14f + lift * Mathf.InverseLerp(K.Edge, BarrierX - .25f + shift, x);
        }

        /// <summary>Ground of another colour (grass) on the wedge that opens between the path and the fence when the
        /// corridor is shifted: from the path's verge to just short of the fence, z0 to z1, lying on the dry verge.</summary>
        internal static void Wedge(CqChunkDraft d, Corridor o, float z0, float z1, Color colour, ComicSurface surface = ComicSurface.Foliage)
        {
            var w = new Sweep(o);
            ComicMesh r = d.Road;
            r.Surface = surface;
            for (float z = z0; z < z1 - .001f; z += 2f)
            {
                float e = Mathf.Min(z1, z + 2f);
                float xa = Mathf.Max(K.Edge, FenceX - .4f + w.ShiftAt(z)), xb = Mathf.Max(K.Edge, FenceX - .4f + w.ShiftAt(e));
                if (xa - K.Edge < .3f && xb - K.Edge < .3f) continue;
                K.Face(r, V(-K.Edge, .16f, z), V(-K.Edge, .16f, e), V(-xb, VergeY(o, xb, e) + .02f, e), V(-xa, VergeY(o, xa, z) + .02f, z), colour, Vector3.up, false);
            }
        }

        /// <summary>A smooth change from <paramref name="from"/> (up to zFrom) to <paramref name="to"/> (from zTo on).</summary>
        public static Func<float, float> Ease(float zFrom, float zTo, float from, float to) => z =>
        {
            float t = Mathf.Clamp01((z - zFrom) / Mathf.Max(.001f, zTo - zFrom));
            return Mathf.Lerp(from, to, t * t * (3f - 2f * t));
        };

        // ==================================================================== layers

        sealed class Layers
        {
            public readonly ComicMesh Road = new ComicMesh(), Front = new ComicMesh(), Props = new ComicMesh(), Deep = new ComicMesh(), Land = new ComicMesh();

            public void Commit(CqChunkDraft d)
            {
                d.Road.Add(Road, Vector3.zero, Quaternion.identity, Vector3.one);
                d.SlopedFrontage.Add(Front, Vector3.zero, Quaternion.identity, Vector3.one);
                foreach ((ComicMesh to, ComicMesh from) in new[] { (d.Props, Props), (d.Deep, Deep), (d.Landmark, Land) })
                {
                    if (from.TriangleCount == 0) continue;
                    from.Shear(d.Grade);
                    to.Add(from, Vector3.zero, Quaternion.identity, Vector3.one);
                }
            }
        }

        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static float F(System.Random r, float a, float b) => Mathf.Lerp(a, b, (float)r.NextDouble());

        /// <summary>Sweeps cross-section shapes along z, following the corridor's shift and lift.</summary>
        sealed class Sweep
        {
            readonly Func<float, float> _shift, _lift;
            public readonly float Step;
            public Sweep(Corridor o)
            {
                _shift = o.Shift; _lift = o.Lift;
                Step = o.Shift != null || o.Lift != null ? 2f : 4f;
            }
            public float ShiftAt(float z) => _shift != null ? _shift(z) : 0f;
            public float LiftAt(float z) => _lift != null ? _lift(z) : 0f;
            /// <summary>x = metres left of the path centre line (before the shift), y above the path (before the lift).</summary>
            public Vector3 At(float x, float y, float z) => new Vector3(-(x + ShiftAt(z)), y + LiftAt(z), z);

            /// <summary>A strip of ground from a (height ya) to b (height yb).</summary>
            public void Strip(ComicMesh m, float a, float b, float ya, float yb, float z0, float z1, Color c, ComicSurface s)
            {
                m.Surface = s;
                for (float z = z0; z < z1 - .001f; z += Step)
                {
                    float e = Mathf.Min(z1, z + Step);
                    K.Face(m, At(a, ya, z), At(a, ya, e), At(b, yb, e), At(b, yb, z), c, Vector3.up, false);
                }
            }

            /// <summary>A bar of rectangular section a..b, y0..y1 (top and both sides; under = the underside too).</summary>
            public void Bar(ComicMesh m, float a, float b, float y0, float y1, float z0, float z1, Color c, ComicSurface s, bool ink = true, bool under = false, float step = 0f)
            {
                m.Surface = s;
                if (step <= 0f) step = Step;
                for (float z = z0; z < z1 - .001f; z += step)
                {
                    float e = Mathf.Min(z1, z + step);
                    K.Face(m, At(a, y1, z), At(a, y1, e), At(b, y1, e), At(b, y1, z), c, Vector3.up, ink);
                    K.Face(m, At(a, y0, z), At(a, y0, e), At(a, y1, e), At(a, y1, z), c, Vector3.right, ink);
                    K.Face(m, At(b, y0, z), At(b, y0, e), At(b, y1, e), At(b, y1, z), c, Vector3.left, ink);
                    if (under) K.Face(m, At(a, y0, z), At(a, y0, e), At(b, y0, e), At(b, y0, z), c, Vector3.down, ink);
                }
                K.Face(m, At(a, y0, z0), At(b, y0, z0), At(b, y1, z0), At(a, y1, z0), c, Vector3.back, ink);
                K.Face(m, At(a, y0, z1), At(b, y0, z1), At(b, y1, z1), At(a, y1, z1), c, Vector3.forward, ink);
            }

            /// <summary>A flat marking a..b from z0 to z1 (one quad).</summary>
            public void Mark(ComicMesh m, float a, float b, float y, float z0, float z1, Color c)
            {
                m.Surface = ComicSurface.Plain;
                K.Face(m, At(a, y, z0), At(a, y, z1), At(b, y, z1), At(b, y, z0), c, Vector3.up, false);
            }

            /// <summary>An upright box standing at (x, z), base at y.</summary>
            public void Post(ComicMesh m, float x, float y, float z, Vector3 size, Color c)
            {
                Vector3 p = At(x, y, z);
                m.Box(p + V(0, size.y * .5f, 0), size, c);
            }
        }

        // ==================================================================== the corridor

        /// <summary>The freeway and the railway on the left from z0 to z1 (chunk-local). Standard unless <paramref name="o"/>
        /// says otherwise. Fence, verge, barrier, carriageway A (northbound), railway (ballast, two tracks, masts at
        /// z = 6 and 26, contact wires), carriageway B (southbound), far verge, a few cars.</summary>
        internal static void Freeway(CqChunkDraft d, float z0, float z1, Corridor o = null)
        {
            o = o ?? Standard;
            var L = new Layers();
            var w = new Sweep(o);
            var rng = new System.Random(ComicKitGeometry.StableHash(d.Id) ^ (o.Seed * 7919) ^ Mathf.RoundToInt(z0 * 131f));
            ComicMesh r = L.Road, f = L.Front;
            bool gap = o.GapFrom >= 0f;

            // ---- between the path and the freeway
            if (o.Deck)
            {
                // the bridge: one slab under both carriageways and the railway, a white fascia towards the path, piers
                w.Bar(f, RoadA0 - .8f, RoadB1 + .4f, -1.4f, -.02f, z0, z1, Concrete, ComicSurface.Concrete, true, true);
                w.Bar(f, RoadA0 - 1.05f, RoadA0 - .8f, -1.5f, .15f, z0, z1, CanopyWhite, ComicSurface.Concrete);
                for (float zp = Mathf.Ceil((z0 - 8f) / 20f) * 20f + 8f; zp < z1; zp += 20f)
                {
                    if (zp < z0) continue;
                    foreach (float x in new[] { 14f, 22f, 30f, 38f, 46f })
                    {
                        Vector3 p = w.At(x, -1.4f, zp);
                        L.Land.Surface = ComicSurface.Concrete;
                        L.Land.Cylinder(p + V(0, -1.5f, 0), .8f, 3f, Concrete, 8);
                    }
                }
            }
            else
            {
                foreach ((float a, float b) in Spans(z0, z1, o.GapFrom, o.GapTo))
                {
                    // dry verge from the path's verge out to the barrier (wider when the corridor moves away)
                    Verge(w, r, a, b);
                    if (o.Fence) Fence(w, f, a, b);
                }
            }

            // ---- jersey barrier, carriageway A, the railway behind its barriers, carriageway B
            JerseyBarrier(w, f, BarrierX, z0, z1);
            Carriageway(w, r, RoadA0, RoadA1, RoadA0 + .35f, z0, z1);
            w.Strip(r, RoadA1, Rail0, RoadY + .04f, RoadY + .04f, z0, z1, Color.Lerp(DryGrass, Lawn, .4f), ComicSurface.Foliage);
            JerseyBarrier(w, f, MedianBarrierA, z0, z1);
            Railway(w, r, f, z0, z1);
            Carriageway(w, r, RoadB0, RoadB1, RoadB0 + 1.05f, z0, z1);
            JerseyBarrier(w, f, MedianBarrierB, z0, z1);

            // ---- the far side
            if (!o.Deck)
            {
                w.Strip(r, RoadB1, o.FarTo, .12f, .12f, z0, z1, DryGrass, ComicSurface.Foliage);
                if (o.Far != FarSide.Open) FarEdge(w, L, o, rng, z0, z1);
            }

            // ---- a few cars: carriageway A comes towards the rider (we drive on the left), B goes the rider's way
            for (int i = 0; i < o.Cars; i++)
            {
                bool south = i % 2 == 1;
                float laneWidth = south ? (RoadB1 - RoadB0 - .7f) / 3f : (RoadA1 - RoadA0) / 3f;
                float lane = (south ? RoadB0 + .7f : RoadA0) + laneWidth * (rng.Next(3) + .5f);
                float z = Mathf.Lerp(z0 + 3f, z1 - 3f, (i + F(rng, .15f, .85f)) / Mathf.Max(1, o.Cars));
                ComicMesh car = rng.Next(5) == 0 ? Van(CarColours[rng.Next(CarColours.Length)]) : ComicKitGeometry.Car(K.R, CarColours[rng.Next(CarColours.Length)]);
                L.Deep.Add(car, w.At(lane, RoadY, z), Quaternion.Euler(0, south ? 0f : 180f, 0), Vector3.one);
            }
            L.Commit(d);
        }

        static IEnumerable<(float, float)> Spans(float z0, float z1, float gapFrom, float gapTo)
        {
            if (gapFrom < 0f || gapTo <= z0 || gapFrom >= z1) { yield return (z0, z1); yield break; }
            if (gapFrom > z0) yield return (z0, gapFrom);
            if (gapTo < z1) yield return (gapTo, z1);
        }

        static void Verge(Sweep w, ComicMesh r, float z0, float z1)
        {
            // From the path's own verge (a fixed line beside the path) to the foot of the barrier (which moves with the
            // corridor): one quad per step, level with the path at the near edge.
            r.Surface = ComicSurface.Foliage;
            for (float z = z0; z < z1 - .001f; z += w.Step)
            {
                float e = Mathf.Min(z1, z + w.Step);
                Vector3 a0 = V(-K.Edge, .14f, z), a1 = V(-K.Edge, .14f, e);
                Vector3 b0 = w.At(BarrierX - .25f, .14f, z), b1 = w.At(BarrierX - .25f, .14f, e);
                K.Face(r, a0, a1, b1, b0, DryGrass, Vector3.up, false);
            }
        }

        /// <summary>The path fence: black posts every 2 m, top and bottom rails, three wires between. No diagonal mesh:
        /// thin crossing lines shimmer at distance, lines along the road do not.</summary>
        static void Fence(Sweep w, ComicMesh f, float z0, float z1)
        {
            f.Surface = ComicSurface.PaintedMetal;
            for (float z = Mathf.Ceil((z0 - .001f) / 2f) * 2f; z < z1 - .05f; z += 2f)
                w.Post(f, FenceX, K.Kerb, z + .06f, V(.09f, FenceTop - K.Kerb, .09f), FenceInk);
            foreach (float y in new[] { .35f, FenceTop - .08f })
                w.Bar(f, FenceX - .035f, FenceX + .035f, y, y + .07f, z0, z1, FenceInk, ComicSurface.PaintedMetal);
            foreach (float y in new[] { .78f, 1.2f, 1.62f })
                w.Bar(f, FenceX - .02f, FenceX + .02f, y, y + .04f, z0, z1, FenceWire, ComicSurface.Metal);
        }

        /// <summary>Concrete jersey barrier: wide foot, narrow top.</summary>
        static void JerseyBarrier(Sweep w, ComicMesh f, float x, float z0, float z1)
        {
            w.Bar(f, x - .35f, x + .35f, RoadY - .06f, RoadY + .32f, z0, z1, Barrier, ComicSurface.Concrete);
            w.Bar(f, x - .16f, x + .16f, RoadY + .32f, BarrierTop, z0, z1, Barrier, ComicSurface.Concrete);
        }

        /// <summary>Three lanes: asphalt, solid white edge lines, dashed lane lines (3 m every 8 m: five per chunk, so
        /// the rhythm carries across chunk ends).</summary>
        static void Carriageway(Sweep w, ComicMesh r, float a, float b, float firstLine, float z0, float z1)
        {
            w.Strip(r, a, b, RoadY, RoadY, z0, z1, FwyAsphalt, ComicSurface.Asphalt);
            float y = RoadY + .015f, last = b - .35f;
            for (float z = z0; z < z1 - .001f; z += w.Step)
            {
                float e = Mathf.Min(z1, z + w.Step);
                w.Mark(r, firstLine, firstLine + .15f, y, z, e, LineWhite);
                w.Mark(r, last - .15f, last, y, z, e, LineWhite);
            }
            float lane = (last - firstLine) / 3f;
            for (int k = 1; k < 3; k++)
                for (float z = Mathf.Ceil((z0 - 1f) / 8f) * 8f + 1f; z < z1 - 3f + .01f; z += 8f)
                {
                    if (z < z0) continue;
                    float x = firstLine + lane * k;
                    w.Mark(r, x - .07f, x + .07f, y, z, z + 3f, LineWhite);
                }
        }

        /// <summary>Ballast, sleepers, rails, the contact wire over each track, and the two masts of the chunk.</summary>
        static void Railway(Sweep w, ComicMesh r, ComicMesh f, float z0, float z1)
        {
            w.Strip(r, Rail0, Rail1, BallastY, BallastY, z0, z1, Ballast, ComicSurface.Concrete);
            w.Strip(r, Rail0 - .25f, Rail0, RoadY + .04f, BallastY, z0, z1, Color.Lerp(Ballast, K.R.ink, .15f), ComicSurface.Concrete);
            foreach (float t in new[] { TrackA, TrackB })
            {
                for (float z = Mathf.Ceil((z0 - .45f) / .9f) * .9f + .45f; z < z1 - .12f; z += .9f)
                    if (z > z0 + .12f) w.Mark(r, t - 1.3f, t + 1.3f, BallastY + .04f, z - .12f, z + .12f, Sleeper);
                foreach (float side in new[] { -.72f, .72f })
                    w.Bar(r, t + side - .045f, t + side + .045f, BallastY + .04f, RailTopY, z0, z1, RailSteel, ComicSurface.Metal, false);
                w.Bar(r, t - .03f, t + .03f, WireY, WireY + .06f, z0, z1, K.R.ink, ComicSurface.Metal, false, true);
            }
            for (float zc = Mathf.Floor(z0 / 40f) * 40f; zc < z1; zc += 40f)
                foreach (float zm in new[] { zc + MastZ0, zc + MastZ1 })
                {
                    if (zm < z0 - .001f || zm >= z1) continue;
                    foreach ((float x, float to) in new[] { (Rail0 + .2f, TrackA + .3f), (Rail1 - .2f, TrackB - .3f) })
                    {
                        f.Surface = ComicSurface.PaintedMetal;
                        w.Post(f, x, BallastY, zm, V(.32f, MastTop - BallastY, .32f), MastGrey);
                        Vector3 a = w.At(x, 6.1f, zm), b = w.At(to, 6.1f, zm), c = w.At(to, WireY + .06f, zm);
                        f.Box((a + b) * .5f, V(Mathf.Abs(a.x - b.x), .14f, .14f), MastGrey);
                        f.Box((b + c) * .5f, V(.07f, Mathf.Abs(b.y - c.y), .07f), K.R.ink);
                        f.Box(w.At(x, 5.2f, zm) + V(x < TrackB ? -.35f : .35f, 0, 0), V(.7f, .1f, .1f), MastGrey);   // lower stay
                    }
                }
        }

        static void FarEdge(Sweep w, Layers L, Corridor o, System.Random rng, float z0, float z1)
        {
            ComicMesh deep = L.Deep;
            switch (o.Far)
            {
                case FarSide.NoiseWall:
                {
                    // 4 m concrete panels in two sand tones with a terracotta band, posts between, gums looking over it
                    const float x = RoadB1 + 3.6f, top = 4.4f;
                    for (float z = Mathf.Floor(z0 / 4f) * 4f; z < z1 - .01f; z += 4f)
                    {
                        float a = Mathf.Max(z, z0), e = Mathf.Min(z1, z + 4f);
                        if (e - a < .1f) continue;
                        bool even = Mathf.RoundToInt(z / 4f) % 2 == 0;
                        w.Bar(deep, x, x + .4f, -.2f, top, a, e, even ? WallSand : WallCream, ComicSurface.Concrete, false, false, 4f);
                        w.Bar(deep, x - .05f, x, 2.6f, 3.3f, a + .5f, e - .5f, WallTerracotta, ComicSurface.Concrete, false, false, 4f);
                        w.Post(deep, x + .2f, -.2f, a, V(.3f, top + .5f, .3f), ConcreteDark);
                    }
                    for (float z = z0 + F(rng, 2f, 7f); z < z1; z += F(rng, 9f, 14f))
                        P.Gum(deep, w.At(x + F(rng, 5f, 13f), .12f, z), F(rng, 12f, 16f), rng.Next(), true);
                    break;
                }
                case FarSide.Bush:
                {
                    for (float z = z0 + F(rng, 1f, 4f); z < z1; z += F(rng, 4.5f, 7f))
                    {
                        float x = RoadB1 + F(rng, 4f, 20f);
                        if (rng.Next(3) == 0) P.Gum(deep, w.At(x, .12f, z), F(rng, 11f, 16f), rng.Next(), true);
                        else P.Plant(deep, w.At(x, 1.2f, z), F(rng, 2.6f, 3.8f), Color.Lerp(P.BanksiaLeaf, BushOlive, F(rng, 0f, 1f)), Color.Lerp(BushOlive, K.R.yellow, .2f));
                    }
                    break;
                }
                default:
                {
                    // a loose row: peppermints of different sizes, a gum now and then, a shrub between
                    for (float z = z0 + F(rng, 1f, 5f); z < z1; z += F(rng, 6f, 10f))
                    {
                        float x = RoadB1 + F(rng, 5f, 15f);
                        int kind = rng.Next(5);
                        if (kind == 0) P.Gum(deep, w.At(x, .12f, z), F(rng, 12f, 16f), rng.Next(), true);
                        else P.RoundTree(deep, w.At(x, .12f, z), F(rng, 5.5f, 9.5f), Color.Lerp(Peppermint, DryGrass, F(rng, 0f, .3f)), rng.Next());
                        if (kind > 2) P.Shrub(deep, w.At(x - F(rng, 2f, 4f), .12f, z + F(rng, 1.5f, 3f)), F(rng, .8f, 1.3f), BushOlive);
                    }
                    break;
                }
            }
        }

        /// <summary>A delivery van for the freeway (plain white-ish box van, no brand).</summary>
        static ComicMesh Van(Color colour)
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Box(V(0, 1.25f, -.4f), V(1.9f, 1.9f, 3.6f), Color.Lerp(colour, Color.white, .6f));
            m.Box(V(0, .95f, 1.9f), V(1.85f, 1.3f, 1.1f), colour);
            m.Surface = ComicSurface.Glass;
            m.Box(V(0, 1.3f, 2.46f), V(1.6f, .5f, .04f), K.R.glass);
            m.Surface = ComicSurface.Rubber;
            for (int side = -1; side <= 1; side += 2)
                for (int end = -1; end <= 1; end += 2) m.Cylinder(V(side * .9f, .38f, end * 1.5f), .38f, .18f, K.R.ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            return m;
        }

        // ==================================================================== trains and stations

        /// <summary>A Transperth B-series set (no brand): white cars with a dark window band, a green stripe low on the
        /// side, grey doors, a grey roof with air-conditioning pods and one pantograph, green-and-white cab ends.
        /// track 0 = the near track (northbound: the cab with white headlights faces the rider), 1 = the far track
        /// (southbound). z0..z1 is the whole set; it is cut into cars of at most 24 m.</summary>
        internal static void Train(CqChunkDraft d, int track, float z0, float z1, Corridor o = null)
        {
            var L = new Layers();
            TrainMesh(L.Land, new Sweep(o ?? Standard), track, z0, z1);
            L.Commit(d);
        }

        /// <summary>The same train as a loose mesh on a straight track at x = 0 (rail top at y = 0), for atlases and far scenery.</summary>
        internal static ComicMesh TrainSample(float length, bool northbound)
        {
            var m = new ComicMesh();
            TrainCars(m, z => V(0, 0, z), 0f, length, northbound);
            return m;
        }

        static void TrainMesh(ComicMesh m, Sweep w, int track, float z0, float z1)
        {
            float x = track == 0 ? TrackA : TrackB;
            TrainCars(m, z => w.At(x, RailTopY, z), z0, z1, track == 0);
        }

        static void TrainCars(ComicMesh m, Func<float, Vector3> rail, float z0, float z1, bool northbound)
        {
            float length = z1 - z0;
            int cars = Mathf.Max(1, Mathf.CeilToInt(length / 24f));
            float carLen = (length - (cars - 1) * .5f) / cars;
            for (int i = 0; i < cars; i++)
            {
                float a = z0 + i * (carLen + .5f), b = a + carLen, zc = (a + b) * .5f;
                Vector3 p = rail(zc);
                m.Surface = ComicSurface.Rubber;
                m.Box(p + V(0, .42f, 0), V(2.3f, .5f, carLen - 1.4f), K.R.ink);                                 // underframe and bogies
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(p + V(0, 2.1f, 0), V(2.9f, 2.9f, carLen), TrainWhite);                                    // body
                m.Box(p + V(0, 1.15f, 0), V(2.94f, .32f, carLen - .04f), TrainGreen);                           // green stripe
                m.Box(p + V(0, 3.38f, 0), V(2.94f, .1f, carLen - .04f), TrainGreen);
                m.Surface = ComicSurface.Glass;
                m.Box(p + V(0, 2.7f, 0), V(2.95f, .85f, carLen - 1.8f), TrainBand);                             // window band
                m.Surface = ComicSurface.PaintedMetal;
                for (float z = -carLen * .5f + 2.2f; z < carLen * .5f - 1.6f; z += 2.4f) m.Box(p + V(0, 2.7f, z), V(2.97f, .87f, .14f), TrainWhite);
                foreach (float dz in new[] { -carLen * .27f, carLen * .27f })
                {
                    m.Box(p + V(0, 2.05f, dz), V(2.98f, 2.3f, 1.5f), TrainDoor);                                // sliding doors
                    m.Box(p + V(0, 2.05f, dz), V(3f, 2.3f, .06f), TrainBand);
                    m.Surface = ComicSurface.Glass;
                    m.Box(p + V(0, 2.65f, dz - .4f), V(3f, .8f, .5f), TrainBand);
                    m.Box(p + V(0, 2.65f, dz + .4f), V(3f, .8f, .5f), TrainBand);
                    m.Surface = ComicSurface.PaintedMetal;
                }
                m.Box(p + V(0, 3.64f, 0), V(2.5f, .2f, carLen - .5f), TrainRoof);                               // roof
                m.Box(p + V(0, 3.86f, carLen * .22f), V(1.7f, .26f, 3.2f), Color.Lerp(TrainRoof, K.R.ink, .15f));
                m.Box(p + V(0, 3.86f, -carLen * .22f), V(1.7f, .26f, 3.2f), Color.Lerp(TrainRoof, K.R.ink, .15f));
                if (i == 0)
                {
                    // pantograph up to the wire (wire is WireY above the path; the rail top is RailTopY)
                    float top = WireY - RailTopY - .03f;
                    m.Beam(p + V(0, 3.74f, -.9f), p + V(0, (3.74f + top) * .5f + .1f, .2f), .08f, K.R.ink);
                    m.Beam(p + V(0, (3.74f + top) * .5f + .1f, .2f), p + V(0, top, -.5f), .08f, K.R.ink);
                    m.Box(p + V(0, top, -.5f), V(1.5f, .07f, .3f), K.R.ink);
                }
                // cab ends: the two outer ends of the set
                foreach ((bool on, float ze, float dir) in new[] { (i == 0, a, -1f), (i == cars - 1, b, 1f) })
                {
                    if (!on) continue;
                    Vector3 e = rail(ze);
                    bool head = northbound ? dir < 0f : dir > 0f;
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Box(e + V(0, 2.05f, dir * .3f), V(2.8f, 2.8f, .6f), TrainWhite);
                    m.Box(e + V(0, 1.2f, dir * .34f), V(2.84f, 1.1f, .62f), TrainGreen);
                    m.Surface = ComicSurface.Glass;
                    m.Box(e + V(0, 2.65f, dir * .62f), V(2.3f, .95f, .06f), TrainBand);
                    m.Surface = ComicSurface.PaintedMetal;
                    m.Box(e + V(0, 3.28f, dir * .62f), V(1.5f, .22f, .05f), TrainBand);                         // destination strip
                    foreach (float sx in new[] { -1f, 1f })
                        m.Box(e + V(sx * .95f, 1.45f, dir * .66f), V(.34f, .2f, .05f), head ? K.R.cream : K.Hex("D8322A"));
                    m.Box(e + V(0, .62f, dir * .5f), V(2.2f, .3f, .5f), K.R.ink);                               // coupler box
                }
            }
        }

        /// <summary>A station's island platform between the tracks: brick paving with yellow edges and a tactile strip, a
        /// butterfly canopy on posts from canopy0 to canopy1, the name board at the near end (facing the rider).</summary>
        internal static void IslandPlatform(CqChunkDraft d, float z0, float z1, float canopy0, float canopy1, string name)
        {
            var L = new Layers();
            ComicMesh f = L.Front, r = L.Road;
            float xa = -PlatformA, xb = -PlatformB, xc = (xa + xb) * .5f;
            f.Surface = ComicSurface.Concrete;
            for (float z = z0; z < z1 - .001f; z += 4f) K.MinMaxBox(f, V(xb, BallastY - .05f, z), V(xa, PlatformY - .05f, Mathf.Min(z1, z + 4f)), Concrete);
            r.Surface = ComicSurface.Paving;
            for (float z = z0; z < z1 - .001f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.Face(r, V(xb, PlatformY, z), V(xb, PlatformY, e), V(xa, PlatformY, e), V(xa, PlatformY, z), PlatformBrick, Vector3.up, false);
            }
            foreach (float x in new[] { xa - .35f, xb + .35f }) K.Flat(r, x - .12f, x + .12f, x - .12f, x + .12f, z0, z1, PlatformY + .01f, K.R.yellow);
            K.Flat(r, xc - .6f, xc + .6f, xc - .6f, xc + .6f, z0, z1, PlatformY + .01f, Tactile);
            if (canopy1 > canopy0)
            {
                f.Surface = ComicSurface.PaintedMetal;
                for (float z = canopy0 + 1f; z < canopy1; z += 6f) f.Box(V(xc, PlatformY + 2f, z), V(.24f, 4f, .24f), MastGrey);
                for (float z = canopy0; z < canopy1 - .01f; z += 4f)
                {
                    float e = Mathf.Min(canopy1, z + 4f);
                    foreach (float s in new[] { -1f, 1f })
                    {
                        float xo = xc + s * 3.9f, yi = PlatformY + 4f, yo = PlatformY + 4.45f;
                        K.Face(f, V(xc, yi + .12f, z), V(xc, yi + .12f, e), V(xo, yo + .12f, e), V(xo, yo + .12f, z), CanopyWhite, Vector3.up);
                        K.Face(f, V(xc, yi, z), V(xc, yi, e), V(xo, yo, e), V(xo, yo, z), CanopyUnder, Vector3.down);
                        K.Face(f, V(xo, yo, z), V(xo, yo, e), V(xo, yo + .12f, e), V(xo, yo + .12f, z), CanopyWhite, V(s, 0, 0));
                    }
                }
                // benches and a shelter screen under the canopy, so the platform is not an empty slab
                for (float z = canopy0 + 4f; z < canopy1 - 3f; z += 12f)
                {
                    f.Surface = ComicSurface.Wood;
                    f.Box(V(xc, PlatformY + .45f, z), V(.5f, .1f, 2f), K.Hex("8A5A3C"));
                    f.Surface = ComicSurface.PaintedMetal;
                    foreach (float dz in new[] { -.8f, .8f }) f.Box(V(xc, PlatformY + .22f, z + dz), V(.4f, .44f, .1f), K.R.ink);
                    f.Surface = ComicSurface.Glass;
                    f.Box(V(xc, PlatformY + 1.3f, z + 3.2f), V(.08f, 2f, 2.2f), K.Hex("7FA8C8"));
                }
            }
            if (!string.IsNullOrEmpty(name))
            {
                float px = .1f, wide = TextWidth(name, px) + .7f, zb = z0 + 1.5f;
                f.Surface = ComicSurface.PaintedMetal;
                foreach (float s in new[] { -1f, 1f }) f.Box(V(xc + s * (wide * .5f - .2f), PlatformY + 1.4f, zb), V(.12f, 2.8f, .12f), K.R.ink);
                f.Box(V(xc, PlatformY + 2.55f, zb), V(wide, .95f, .1f), SignNavy);
                Text(f, name, V(xc, PlatformY + 2.55f, zb - .06f), Vector3.right, Vector3.up, px, LineWhite);
            }
            L.Commit(d);
        }

        // ==================================================================== the path

        /// <summary>The shared path surface from z0 to z1: 8 m of asphalt with white edge lines, a dashed white centre
        /// line (3 m every 8 m) and bike symbols; 4 m concrete verges at kerb height with a cream kerb cap.
        /// Default colours = the south line's BOUNDARY surface (grey path, grey concrete). gapFrom..gapTo leaves a
        /// hole right across (a water gap). markings = false: no lines or symbols (a street between two path pieces).</summary>
        internal static void Path(CqChunkDraft d, float z0, float z1, Color? path = null, Color? verge = null, float gapFrom = -1f, float gapTo = -1f, bool markings = true)
        {
            ComicMesh m = d.Road;
            Color pc = path ?? PathGrey, vc = verge ?? VergeGrey;
            float L = d.Length, h = K.Kerb, w = K.Half, sw = K.Side;
            bool Hole(float a, float b) => gapFrom >= 0f && b > gapFrom + .001f && a < gapTo - .001f;
            var cuts = new List<float>();
            for (float z = Mathf.Ceil(z0 / 4f) * 4f; z < z1 - .01f; z += 4f) cuts.Add(z);
            cuts.Add(z0); cuts.Add(z1);
            if (gapFrom >= 0f) { cuts.Add(Mathf.Clamp(gapFrom, z0, z1)); cuts.Add(Mathf.Clamp(gapTo, z0, z1)); }
            cuts.Sort();
            Color joint = Color.Lerp(K.R.ink, vc, .55f);
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float a = cuts[i], b = cuts[i + 1];
                if (b - a < .01f || Hole(a, b)) continue;
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(-w, 0, a), V(-w, 0, b), V(w, 0, b), V(w, 0, a), pc, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    float i0 = s * w, o0 = s * (w + sw);
                    Vector3 inward = V(-s, 0, 0), outward = V(s, 0, 0);
                    m.Surface = ComicSurface.Concrete;
                    K.Face(m, V(i0, h, a), V(i0, h, b), V(o0, h, b), V(o0, h, a), vc, Vector3.up, false);
                    K.Face(m, V(i0, -.05f, a), V(i0, -.05f, b), V(i0, h, b), V(i0, h, a), vc, inward, false);
                    K.Face(m, V(o0, -2f, a), V(o0, -2f, b), V(o0, h, b), V(o0, h, a), vc, outward, false);
                    float c0 = s * (w + .24f), top = h + .04f;
                    m.Surface = ComicSurface.Plain;
                    K.Face(m, V(i0, top, a), V(i0, top, b), V(c0, top, b), V(c0, top, a), K.R.cream, Vector3.up, false);
                    K.Face(m, V(i0, h, a), V(i0, h, b), V(i0, top, b), V(i0, top, a), K.R.cream, inward, false);
                    K.Face(m, V(c0, h, a), V(c0, h, b), V(c0, top, b), V(c0, top, a), K.R.cream, outward, false);
                    if (markings) K.Flat(m, s * (w - .3f), s * (w - .2f), s * (w - .3f), s * (w - .2f), a, b, .031f, LineWhite);
                }
                for (float z = Mathf.Ceil(a / 2f) * 2f; z < b - .01f; z += 2f)
                    for (int s = -1; s <= 1; s += 2) K.Flat(m, s * (w + .24f), s * (w + sw), s * (w + .24f), s * (w + sw), z - .015f, z + .015f, h + .02f, joint);
            }
            foreach (float ze in new[] { 0f, L })
            {
                if (ze < z0 - .001f || ze > z1 + .001f || Hole(ze - .5f, ze + .5f)) continue;
                Vector3 normal = ze <= 0f ? Vector3.back : Vector3.forward;
                m.Surface = ComicSurface.Asphalt;
                K.Face(m, V(-w, -.8f, ze), V(w, -.8f, ze), V(w, 0, ze), V(-w, 0, ze), pc, normal, false);
                m.Surface = ComicSurface.Concrete;
                for (int s = -1; s <= 1; s += 2) K.Face(m, V(s * w, -2f, ze), V(s * (w + sw), -2f, ze), V(s * (w + sw), h, ze), V(s * w, h, ze), vc, normal, false);
            }
            if (!markings) return;
            for (float z = 1f; z < L - 3f + .01f; z += 8f)
                if (z >= z0 - .01f && z + 3f <= z1 + .01f && !Hole(z, z + 3f)) K.Flat(m, -.07f, .07f, -.07f, .07f, z, z + 3f, .034f, LineWhite);
            foreach (Vector2 b in new[] { new Vector2(-2f, 6.5f), new Vector2(2f, 30.5f) })
                if (b.y - 2f >= z0 && b.y + 2f <= z1 && !Hole(b.y - 2f, b.y + 2f)) BikeSymbol(m, b.x, b.y, .036f, LineWhite);
        }

        /// <summary>The line where a chunk's own paving meets the boundary surface: a limestone sett band right across
        /// the path and both verges, with a white bar either side of it on the path. Readable at speed.</summary>
        internal static void Threshold(CqChunkDraft d, float z)
        {
            ComicMesh m = d.Road;
            float w = K.Half, e = K.Edge;
            K.Flat(m, -w, w, -w, w, z - .35f, z + .35f, .037f, LimeCap);
            for (float x = -w; x < w - .01f; x += .5f) K.Flat(m, x + .24f, x + .26f, x + .24f, x + .26f, z - .35f, z + .35f, .038f, LimeJoint);
            K.Flat(m, -w, w, -w, w, z - .012f, z + .012f, .038f, LimeJoint);
            foreach (float dz in new[] { -.75f, .75f }) K.Flat(m, -w + .2f, w - .2f, -w + .2f, w - .2f, z + dz - .1f, z + dz + .1f, .036f, LineWhite);
            for (int s = -1; s <= 1; s += 2)
                K.Flat(m, Mathf.Min(s * (w + .24f), s * e), Mathf.Max(s * (w + .24f), s * e), Mathf.Min(s * (w + .24f), s * e), Mathf.Max(s * (w + .24f), s * e), z - .35f, z + .35f, K.Kerb + .024f, LimeCap);
        }

        /// <summary>The shared-path bike symbol, flat on the path, stretched along +Z like real road markings.</summary>
        internal static void BikeSymbol(ComicMesh m, float cx, float cz, float y, Color c)
        {
            const float k = 2.2f, rx = .42f, rz = .42f * k;
            void Line(float xa, float za, float xb, float zb, float yy, float wd)
            {
                Vector3 a = V(xa, yy, za), b = V(xb, yy, zb), side = Vector3.Cross(Vector3.up, (b - a).normalized) * (wd * .5f);
                m.Surface = ComicSurface.Plain;
                K.Face(m, a - side, b - side, b + side, a + side, c, Vector3.up, false);
            }
            foreach (float wx in new[] { -.58f, .58f })
                for (int i = 0; i < 10; i++)
                {
                    float a0 = i * Mathf.PI * .2f, a1 = (i + 1) * Mathf.PI * .2f;
                    Line(cx + wx + Mathf.Cos(a0) * rx, cz + Mathf.Sin(a0) * rz, cx + wx + Mathf.Cos(a1) * rx, cz + Mathf.Sin(a1) * rz, y, .07f);
                }
            Vector2 rear = new Vector2(-.58f, 0), front = new Vector2(.58f, 0), crank = new Vector2(-.05f, 0), seat = new Vector2(-.22f, .55f * k), head = new Vector2(.38f, .55f * k), bar = new Vector2(.46f, .72f * k);
            foreach ((Vector2 a, Vector2 b) in new[] { (rear, seat), (seat, crank), (crank, rear), (seat, head), (head, crank), (head, front), (head, bar), (seat + new Vector2(-.12f, .12f * k), seat + new Vector2(.14f, .12f * k)) })
                Line(cx + a.x, cz + a.y, cx + b.x, cz + b.y, y + .001f, .075f);
        }

        // ==================================================================== the other side of entry and exit chunks

        /// <summary>The line's standard "street edge" for the side away from the freeway in entry and exit chunks: a low
        /// limestone wall right behind the verge, a grass strip out to |x| = lawnTo, peppermint trees at a fixed 10 m
        /// spacing (z = 5, 15, 25, 35 of the chunk). lawnTo = 0: no grass (the chunk has its own there). bush = the road
        /// reserve behind it too (dry grass, shrubs, gums) out to 70 m; without it the chunk draws what is behind (a river
        /// bank, a lake, a wall). Not a hedged lawn.</summary>
        internal static void StreetEdge(CqChunkDraft d, int s, float z0, float z1, float lawnTo, int seed, bool bush)
        {
            var L = new Layers();
            var rng = new System.Random(seed * 9973 + 17);
            ComicMesh f = L.Front, r = L.Road;
            float e = K.Edge;
            bool lawn = lawnTo > e + .6f;   // lawnTo = 0: the chunk has its own grass there already
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float b = Mathf.Min(z1, z + 4f);
                f.Surface = ComicSurface.Concrete;
                K.MinMaxBox(f, V(s * (e + .05f), -1.2f, z), V(s * (e + .5f), K.Kerb + .55f, b), Limestone);
                K.MinMaxBox(f, V(s * (e + .01f), K.Kerb + .55f, z), V(s * (e + .54f), K.Kerb + .67f, b), LimeCap);
                // block joints on the face towards the path
                f.Surface = ComicSurface.Plain;
                float fx = s * (e + .04f);
                Vector3 n = V(-s, 0, 0);
                for (float jz = z + 1f; jz < b - .1f; jz += 1f)
                    K.Face(f, V(fx, K.Kerb, jz - .02f), V(fx, K.Kerb, jz + .02f), V(fx, K.Kerb + .55f, jz + .02f), V(fx, K.Kerb + .55f, jz - .02f), LimeJoint, n, false);
                if (!lawn) continue;
                r.Surface = ComicSurface.Foliage;
                K.Face(r, V(s * (e + .5f), .16f, z), V(s * (e + .5f), .16f, b), V(s * lawnTo, .16f, b), V(s * lawnTo, .16f, z), Lawn, Vector3.up, false);
            }
            if (lawn) P.Patches(r, rng, s, e + .8f, Mathf.Max(e + 2.2f, lawnTo - .3f), .16f, LawnLight, Color.Lerp(Lawn, DryGrass, .5f), z0, z1, Mathf.Max(2, Mathf.RoundToInt((z1 - z0) / 5f)));
            float[] heights = { 7.2f, 6.2f, 7.8f, 6.6f };
            for (float zc = Mathf.Floor(z0 / 40f) * 40f; zc < z1; zc += 40f)
                for (int i = 0; i < 4; i++)
                {
                    float z = zc + 5f + i * 10f;
                    if (z < z0 + 1f || z > z1 - 1f) continue;
                    P.RoundTree(L.Props, V(s * (e + 1.8f), .16f, z), heights[i], Color.Lerp(Peppermint, i % 2 == 0 ? Lawn : K.R.ink, .12f), seed * 31 + i);
                }
            if (bush)
            {
                float far = 70f;
                for (float z = z0; z < z1 - .01f; z += 4f)
                {
                    float b = Mathf.Min(z1, z + 4f);
                    r.Surface = ComicSurface.Foliage;
                    K.Face(r, V(s * lawnTo, .13f, z), V(s * lawnTo, .13f, b), V(s * far, .13f, b), V(s * far, .13f, z), DryGrass, Vector3.up, false);
                }
                P.Patches(r, rng, s, lawnTo + 1f, lawnTo + 22f, .13f, Sand, Color.Lerp(DryGrass, Lawn, .4f), z0, z1, Mathf.Max(2, Mathf.RoundToInt((z1 - z0) / 4f)));
                for (float z = z0 + F(rng, 1f, 3f); z < z1 - 1f; z += F(rng, 3.5f, 5.5f))
                {
                    float x = lawnTo + F(rng, 1.5f, 6f);
                    if (rng.Next(3) == 0) P.Banksia(L.Props, V(s * x, .13f, z), F(rng, .7f, 1f), rng.Next());
                    else P.Shrub(L.Props, V(s * x, .13f, z), F(rng, .6f, 1f), Color.Lerp(BushOlive, Peppermint, F(rng, 0f, 1f)));
                }
                for (float z = z0 + F(rng, 1f, 4f); z < z1; z += F(rng, 5f, 8f))
                {
                    float x = lawnTo + F(rng, 9f, 30f);
                    if (rng.Next(2) == 0) P.Gum(L.Deep, V(s * x, .13f, z), F(rng, 12f, 17f), rng.Next(), true);
                    else P.RoundTree(L.Deep, V(s * x, .13f, z), F(rng, 6f, 9f), Color.Lerp(Peppermint, DryGrass, F(rng, 0f, .3f)), rng.Next());
                }
            }
            L.Commit(d);
            d.Walls.Add(new Bounds(V(s * (e + 7f), d.Y((z0 + z1) * .5f) + 1f, (z0 + z1) * .5f), V(13.5f, 4f, z1 - z0)));
        }

        /// <summary>A limestone name block facing the rider (like ParkRiverKit.NameBlock, with this kit's lettering: M, N
        /// and W five pixels wide), base at p.</summary>
        internal static void NameBlock(ComicMesh m, Vector3 p, string text, float px, Color letters)
        {
            float w = TextWidth(text, px) + 1.2f, h = 5 * px + .9f;
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, h * .5f - .3f, 0), V(w, h + .6f, .7f), Limestone);
            m.Box(p + V(0, .12f, 0), V(w + .14f, .24f, .84f), K.R.ink);
            m.Box(p + V(0, h + .05f, 0), V(w + .2f, .14f, .85f), LimeCap);
            Text(m, text, V(p.x, p.y + h * .55f, p.z - .36f), Vector3.right, Vector3.up, px, letters);
        }

        // ==================================================================== pixel text (M, N and W are 5 wide)

        static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['A'] = new[] { "010", "101", "111", "101", "101" }, ['B'] = new[] { "110", "101", "110", "101", "110" },
            ['C'] = new[] { "011", "100", "100", "100", "011" }, ['D'] = new[] { "110", "101", "101", "101", "110" },
            ['E'] = new[] { "111", "100", "110", "100", "111" }, ['F'] = new[] { "111", "100", "110", "100", "100" },
            ['G'] = new[] { "011", "100", "101", "101", "011" }, ['H'] = new[] { "101", "101", "111", "101", "101" },
            ['I'] = new[] { "111", "010", "010", "010", "111" }, ['J'] = new[] { "001", "001", "001", "101", "010" },
            ['K'] = new[] { "101", "101", "110", "101", "101" }, ['L'] = new[] { "100", "100", "100", "100", "111" },
            ['M'] = new[] { "10001", "11011", "10101", "10001", "10001" }, ['N'] = new[] { "10001", "11001", "10101", "10011", "10001" },
            ['O'] = new[] { "010", "101", "101", "101", "010" }, ['P'] = new[] { "110", "101", "110", "100", "100" },
            ['Q'] = new[] { "010", "101", "101", "111", "011" }, ['R'] = new[] { "110", "101", "110", "101", "101" },
            ['S'] = new[] { "011", "100", "010", "001", "110" }, ['T'] = new[] { "111", "010", "010", "010", "010" },
            ['U'] = new[] { "101", "101", "101", "101", "111" }, ['V'] = new[] { "101", "101", "101", "101", "010" },
            ['W'] = new[] { "10001", "10001", "10101", "11011", "10001" }, ['X'] = new[] { "101", "101", "010", "101", "101" },
            ['Y'] = new[] { "101", "101", "010", "010", "010" }, ['Z'] = new[] { "111", "001", "010", "100", "111" },
            [' '] = new[] { "00", "00", "00", "00", "00" },
        };

        /// <summary>Width of <paramref name="text"/> in metres at pixel size px.</summary>
        public static float TextWidth(string text, float px)
        {
            int columns = 0;
            foreach (char ch in text) columns += (Glyphs.TryGetValue(ch, out string[] g) ? g[0].Length : 3) + 1;
            return Mathf.Max(0, columns - 1) * px;
        }

        /// <summary>Pixel text centred on <paramref name="centre"/>, reading along <paramref name="right"/>, standing along
        /// <paramref name="up"/> (facing the rider: right = +X, up = +Y; painted on the road: right = +X, up = +Z).</summary>
        internal static void Text(ComicMesh m, string text, Vector3 centre, Vector3 right, Vector3 up, float px, Color colour)
        {
            m.Surface = ComicSurface.Plain;
            Vector3 normal = Vector3.Cross(up, right).normalized;
            float width = TextWidth(text, px);
            Vector3 origin = centre - right * (width * .5f) + up * (2.5f * px);
            int column = 0;
            foreach (char ch in text)
            {
                if (!Glyphs.TryGetValue(ch, out string[] g)) { column += 4; continue; }
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < g[row].Length; col++)
                    {
                        if (g[row][col] != '1') continue;
                        Vector3 a = origin + right * ((column + col) * px) - up * ((row + 1) * px);
                        K.Face(m, a, a + right * px, a + right * px + up * px, a + up * px, colour, normal, false);
                    }
                column += g[0].Length + 1;
            }
        }
    }
}
