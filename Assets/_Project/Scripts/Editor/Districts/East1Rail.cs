using System;
using System.Collections.Generic;
using CityScooter.EditorTools.Art;
using CityScooter.Rendering;
using UnityEngine;
using K = CityScooter.EditorTools.Districts.CbdQuayKit;
using E = CityScooter.EditorTools.Districts.EastLineKit;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// The railway parts that only McIver, Claisebrook and East Perth have: station shelters and canopies, name boards,
    /// the Indian Pacific, the Prospector, a road coach, yard lights. The corridor itself (fence, ballast, tracks, wires,
    /// portals, the A-series and B-series sets, platforms) is the east line's shared EastLineKit since v0.6.
    /// Everything is built in flat road space on the LEFT of the road (u = metres beyond the sidewalk edge) and goes into
    /// the chunk's sheared meshes (Road: no ink; SlopedFrontage: ink), so platforms, wires and trains follow the grade.
    /// Scenery only: nothing here adds a collider.
    /// </summary>
    public static partial class East1Builder
    {
        // The corridor itself (fence, ballast, tracks, wires, portals, trains, platforms) is the east line's shared kit:
        // EastLineKit (alias E). Station furniture below is placed in "u" = metres beyond the left sidewalk edge.
        const int RailSide = E.Left;
        const float RailTop = E.RailTop, PlatformTop = E.PlatformTop, ContactWire = E.ContactWire, PortalTop = E.PortalTop;
        /// <summary>The standard pair of tracks in u (6 and 10.5), and the two extra tracks of the four-track section from
        /// the city to the Claisebrook depot (20.5 and 25: 28.5 and 33 from the road centre line).</summary>
        const float T1 = E.Track1 - K.Edge, T2 = E.Track2 - K.Edge, T3 = 20.5f, T4 = 25f;
        /// <summary>Where the standard corridor ends and the district's own ground begins, in u (15.6).</summary>
        const float BeyondRail = E.CorridorEnd - K.Edge;
        static readonly Color BallastC = E.Ballast, BallastDark = E.BallastDark, Sleeper = E.Sleeper, RailSteel = E.RailSteel, RailShine = E.RailShine;
        static readonly Color MastGrey = E.MastGrey, Insulator = E.Insulator, Tactile = E.Tactile, PlatformFace = E.PlatformFace;
        static readonly Color Stainless = E.Stainless, StainlessDark = E.StainlessDark, TransGreen = E.TransGreen, SkirtRed = E.SkirtRed, DoorYellow = E.DoorYellow;
        static readonly Color LocoBlue = K.Hex("1F3F8A"), LocoYellow = K.Hex("F2C230"), FenceBlack = E.FenceBlack;

        /// <summary>x of a point u metres beyond the left sidewalk edge.</summary>
        static float LX(float u) => -(K.Edge + u);

        /// <summary>The four-track section (McIver, and Claisebrook as far as the depot): the standard corridor plus two
        /// more wired tracks behind it, the ballast out to 36, the portals' far mast at 35.4, the grey yard beyond.</summary>
        static E.CorridorOptions FourTracks(bool yard = true) => new E.CorridorOptions
        {
            ExtraTracks = new[] { K.Edge + T3, K.Edge + T4 }, BedTo = 36f, FarMast = 35.4f, BackFence = false,
            Beyond = yard ? K.Hex("8E8A80") : (Color?)null,
        };

        /// <summary>A fence of another colour away from the railway (the power station's): posts, two rails, pointed pales,
        /// u metres beyond the sidewalk on side s.</summary>
        static void Palisade(CqChunkDraft d, int s, float z0, float z1, float u, float h = 1.9f, Color? colour = null)
        {
            Color c = colour ?? FenceBlack;
            ComicMesh m = d.SlopedFrontage;
            float x = s * (K.Edge + u), y = K.Kerb;
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = z0; z <= z1 + .01f; z += 5f) m.Box(V(x, y + h * .5f + .05f, Mathf.Min(z, z1 - .04f) + .02f), V(.09f, h + .1f, .09f), c);
            for (float z = z0; z < z1 - .01f; z += 5f)
            {
                float e = Mathf.Min(z1, z + 5f);
                m.Box(V(x, y + h * .85f, (z + e) * .5f), V(.05f, .07f, e - z), c);
                m.Box(V(x, y + h * .2f, (z + e) * .5f), V(.05f, .07f, e - z), c);
            }
            ComicMesh r = d.Road;
            r.Surface = ComicSurface.PaintedMetal;
            Vector3 n = new Vector3(-s, 0, 0);
            for (float z = z0 + .25f; z < z1 - .1f; z += .42f)
            {
                K.Face(r, V(x - s * .03f, y + .08f, z), V(x - s * .03f, y + .08f, z + .09f), V(x - s * .03f, y + h, z + .09f), V(x - s * .03f, y + h, z), c, n, false);
                K.TriFacing(r, V(x - s * .03f, y + h, z), V(x - s * .03f, y + h, z + .09f), V(x - s * .03f, y + h + .12f, z + .045f), c, n);
            }
        }

        /// <summary>A platform from u0 to u1 (the kit's, on the left): near = a track edge at u0, far = one at u1.</summary>
        static void Platform(CqChunkDraft d, float u0, float u1, float z0, float z1, bool near, bool far) =>
            E.Platform(d, RailSide, K.Edge + u0, K.Edge + u1, z0, z1, near, far);

        /// <summary>McIver's shelter: a barrel vault of off-white corrugated sheet on dark steel posts with branching struts,
        /// a white tiled screen wall with poster cases, a bench, a hanging platform-number sign.</summary>
        static void BarrelShelter(CqChunkDraft d, float u, float z0, float len, int screenSide, string number)
        {
            ComicMesh m = d.SlopedFrontage;
            float x = LX(u), y = PlatformTop, eave = y + 2.75f, half = 1.9f, R = half / Mathf.Sin(62f * Mathf.Deg2Rad), yc = eave - R * Mathf.Cos(62f * Mathf.Deg2Rad);
            Color roof = K.Hex("E6E7E2"), under = K.Hex("B4B8B6");
            const int facets = 6;
            for (int i = 0; i < facets; i++)
            {
                float t0 = Mathf.Lerp(-62f, 62f, (float)i / facets) * Mathf.Deg2Rad, t1 = Mathf.Lerp(-62f, 62f, (float)(i + 1) / facets) * Mathf.Deg2Rad;
                Vector3 a = V(x + R * Mathf.Sin(t0), yc + R * Mathf.Cos(t0), z0), b = V(x + R * Mathf.Sin(t1), yc + R * Mathf.Cos(t1), z0), up = V(Mathf.Sin((t0 + t1) * .5f), Mathf.Cos((t0 + t1) * .5f), 0);
                Vector3 L = V(0, 0, len);
                m.Surface = ComicSurface.Metal;
                K.Face(m, a + up * .07f, b + up * .07f, b + up * .07f + L, a + up * .07f + L, roof, up);
                K.Face(m, a, b, b + L, a + L, under, -up);
                K.Face(m, a, b, b + up * .07f, a + up * .07f, roof, Vector3.back);
                K.Face(m, a + L, b + L, b + up * .07f + L, a + up * .07f + L, roof, Vector3.forward);
            }
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float sx in new[] { -half, half }) m.Box(V(x + sx, eave - .02f, z0 + len * .5f), V(.12f, .12f, len + .1f), SteelDark);
            for (float z = z0 + .8f; z <= z0 + len - .7f; z += Mathf.Max(3f, (len - 1.6f) / Mathf.Max(1, Mathf.RoundToInt((len - 1.6f) / 3.6f))))
            {
                m.Box(V(x, y + 1.25f, z), V(.16f, 2.5f, .16f), SteelDark);
                m.Beam(V(x, y + 2.1f, z), V(x - half + .05f, eave - .05f, z), .09f, SteelDark);
                m.Beam(V(x, y + 2.1f, z), V(x + half - .05f, eave - .05f, z), .09f, SteelDark);
                m.Box(V(x, y + .06f, z), V(.4f, .12f, .4f), Ink);
            }
            // the screen wall (white tiles, ink joints) with a poster case, and a bench against it
            float xs = x + screenSide * 1.2f;
            m.Surface = ComicSurface.Plaster;
            m.Box(V(xs, y + 1.05f, z0 + len * .5f), V(.14f, 2.1f, len - 2.4f), White);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(xs, y + 2.13f, z0 + len * .5f), V(.2f, .08f, len - 2.3f), SteelDark);
            Vector3 n = new Vector3(-screenSide, 0, 0);
            float xf = xs - screenSide * .075f, zp = z0 + len * .5f;
            K.Face(m, V(xf, y + .9f, zp - .75f), V(xf, y + .9f, zp + .75f), V(xf, y + 1.9f, zp + .75f), V(xf, y + 1.9f, zp - .75f), Ink, n);
            K.Face(m, V(xf - screenSide * .01f, y + .98f, zp - .67f), V(xf - screenSide * .01f, y + .98f, zp - .03f), V(xf - screenSide * .01f, y + 1.82f, zp - .03f), V(xf - screenSide * .01f, y + 1.82f, zp - .67f), SignGreen, n, false);
            K.Face(m, V(xf - screenSide * .01f, y + .98f, zp + .03f), V(xf - screenSide * .01f, y + .98f, zp + .67f), V(xf - screenSide * .01f, y + 1.82f, zp + .67f), V(xf - screenSide * .01f, y + 1.82f, zp + .03f), K.Hex("F3E4C2"), n, false);
            m.Surface = ComicSurface.Metal;
            m.Box(V(xs - screenSide * .42f, y + .48f, zp), V(.42f, .07f, 2.2f), StainlessDark);
            foreach (float dz in new[] { -.9f, .9f }) m.Box(V(xs - screenSide * .42f, y + .23f, zp + dz), V(.36f, .46f, .07f), SteelDark);
            // the hanging platform number (faces the rider)
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(x - screenSide * .9f, eave - .55f, z0 + .5f), V(.8f, .42f, .07f), K.Hex("20262C"));
            m.Box(V(x - screenSide * .9f, eave - .2f, z0 + .5f), V(.05f, .3f, .05f), SteelDark);
            PixelText(m, number, x - screenSide * .9f, eave - .55f, z0 + .46f, .06f, White);
        }

        /// <summary>A flat platform canopy (Claisebrook, East Perth): grey sheet roof with an ink fascia on a row of
        /// columns, a glass windbreak and a bench.</summary>
        static void FlatCanopy(CqChunkDraft d, float u, float z0, float len, float width, Color roof, Color column)
        {
            ComicMesh m = d.SlopedFrontage;
            float x = LX(u), y = PlatformTop, top = y + 3.1f;
            m.Surface = ComicSurface.Metal;
            for (float z = z0; z < z0 + len - .01f; z += 4f)
            {
                float e = Mathf.Min(z0 + len, z + 4f);
                m.Box(V(x, top + .09f, (z + e) * .5f), V(width, .18f, e - z), roof);
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(x, top - .06f, z0 + len * .5f), V(width + .1f, .12f, len + .1f), Ink);
            m.Box(V(x, top - .2f, z0 + len * .5f), V(.2f, .2f, len - .6f), column);
            for (float z = z0 + 1.2f; z <= z0 + len - 1f; z += Mathf.Max(3.5f, (len - 2.4f) / Mathf.Max(1, Mathf.RoundToInt((len - 2.4f) / 4.5f))))
            {
                m.Box(V(x, y + 1.5f, z), V(.2f, 3f, .2f), column);
                m.Box(V(x, top - .25f, z), V(width - .5f, .12f, .14f), column);
                m.Box(V(x, y + .06f, z), V(.44f, .12f, .44f), Ink);
            }
            m.Surface = ComicSurface.Glass;
            m.Box(V(x - .1f, y + 1.15f, z0 + len * .5f), V(.06f, 1.9f, Mathf.Min(4f, len * .4f)), GlassPale);
            m.Surface = ComicSurface.Metal;
            m.Box(V(x + .45f, y + .48f, z0 + len * .5f), V(.42f, .07f, 2f), StainlessDark);
            foreach (float dz in new[] { -.8f, .8f }) m.Box(V(x + .45f, y + .23f, z0 + len * .5f + dz), V(.36f, .46f, .07f), SteelDark);
        }

        /// <summary>A station name board on the platform, facing the rider and the road.</summary>
        static void PlatformSign(CqChunkDraft d, float u, float z, string text, float px = .1f)
        {
            var sign = new ComicMesh();
            NameBoard(sign, Vector3.zero, text, px, null, SignGreen, 2.2f);
            d.SlopedFrontage.Add(sign, V(LX(u), PlatformTop, z), Quaternion.Euler(0, -28f, 0), Vector3.one);
        }

        // ---------------------------------------------------------------- trains (the sets are the kit's)

        static E.TrainLook ASeries => E.ASeries;
        static E.TrainLook BSeries => E.BSeries;
        /// <summary>The Prospector railcar: stainless, a raked yellow cab front, one door at each end.</summary>
        static E.TrainLook Prospector => new E.TrainLook { Raked = true, Nose = LocoYellow, Band = K.Hex("2A3E6E"), Stripe = K.Hex("2A3E6E"), Doors = new[] { -.42f, .42f } };

        static void SideQuad(ComicMesh m, float xs, float z0, float z1, float y0, float y1, Color c, bool ink = false) =>
            K.Face(m, V(xs, y0, z0), V(xs, y0, z1), V(xs, y1, z1), V(xs, y1, z0), c, Vector3.right, ink);

        /// <summary>An electric set standing on the track at u, its rear (-Z, facing the rider) end at z0. Returns its length.</summary>
        static float Emu(ComicMesh m, float u, float z0, int cars, E.TrainLook k, bool lite = false, bool panto = true, bool noseFront = true, bool noseBack = true) =>
            E.Train(m, RailSide, K.Edge + u, z0, cars, k, lite, panto, noseFront, noseBack);

        static void GangwayEnd(ComicMesh m, float x, float z) => E.Gangway(m, x, z);

        /// <summary>An Indian Pacific carriage (stainless Comeng stock, no lettering): fluted panels below and above a row of
        /// small windows, a plain navy name board with a gold line, a rounded roof, underframe tanks.</summary>
        static void LongDistanceCar(ComicMesh m, float u, float za, bool lite = false)
        {
            const float L = 19.4f, W = 2.95f;
            float x = LX(u), zb = za + L, zc = za + L * .5f, xs = x + W * .5f + .012f;
            m.Surface = ComicSurface.Metal;
            m.Box(V(x, 2.75f, zc), V(W, 2.9f, L), Stainless);
            m.Box(V(x, 4.3f, zc), V(W - .5f, .22f, L - .3f), StainlessDark);
            m.Box(V(x, 4.48f, zc), V(W - 1.4f, .16f, L - 1.6f), StainlessDark);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(x, 1.1f, zc), V(W - .4f, .45f, L - 1.2f), Ink);
            m.Box(V(x, .8f, zc), V(2f, .6f, 5f), SteelDark);
            foreach (float f in new[] { -.36f, .36f })
            {
                m.Box(V(x, .72f, zc + f * L), V(2.2f, .55f, 3.2f), SteelDark);
                if (!lite) foreach (float dz in new[] { -.9f, .9f }) m.Cylinder(V(x + 1.02f, .72f, zc + f * L + dz), .42f, .14f, Ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            }
            m.Surface = ComicSurface.Plain;
            foreach (float y in new[] { 1.45f, 1.62f, 1.79f, 1.96f, 2.13f, 2.3f, 3.86f, 4.02f }) SideQuad(m, xs, za + .2f, zb - .2f, y, y + .045f, Mix(Stainless, Ink, .32f));
            SideQuad(m, xs, za + 2.2f, zb - 2.2f, 3.5f, 3.74f, Navy);
            SideQuad(m, xs + .004f, za + 2.2f, zb - 2.2f, 3.5f, 3.54f, Gold);
            m.Surface = ComicSurface.Glass;
            for (float z = za + 2.6f; z < zb - 3.4f; z += 2.25f) SideQuad(m, xs, z, z + 1.35f, 2.55f, 3.35f, GlassDark, true);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { za + .5f, zb - 1.4f })
            {
                SideQuad(m, xs, z, z + .9f, 1.35f, 3.4f, Mix(Stainless, Ink, .2f), true);
                m.Surface = ComicSurface.Glass; SideQuad(m, xs + .004f, z + .2f, z + .7f, 2.6f, 3.2f, GlassDark); m.Surface = ComicSurface.PaintedMetal;
            }
            GangwayEnd(m, x, za - .2f);
        }

        /// <summary>The Indian Pacific's locomotive (NR class shape, no emblem): a long blue hood, a full-width cab behind a
        /// low yellow nose, a yellow sill stripe, roof fans, heavy bogies and a fuel tank. Cab towards -Z.</summary>
        static void Locomotive(ComicMesh m, float u, float za)
        {
            const float L = 17.5f, W = 2.9f;
            float x = LX(u), zb = za + L, xs = x + W * .5f + .012f;
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(x, 1.2f, za + L * .5f), V(W, .4f, L), Ink);
            foreach (float f in new[] { -.33f, .33f })
            {
                m.Box(V(x, .72f, za + L * (.5f + f)), V(2.4f, .7f, 4.2f), SteelDark);
                foreach (float dz in new[] { -1.4f, 0f, 1.4f }) m.Cylinder(V(x + 1.1f, .75f, za + L * (.5f + f) + dz), .5f, .14f, Ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            }
            m.Box(V(x, .85f, za + L * .5f), V(2.5f, .8f, 3f), K.Hex("2A2F36"));
            m.Box(V(x, 2.15f, za + .8f), V(W, 1.5f, 1.6f), LocoYellow);                              // the low nose
            m.Box(V(x, 2.95f, za + 2.9f), V(W, 3.1f, 2.6f), LocoBlue);                               // the cab
            m.Box(V(x, 2.9f, (za + 4.2f + zb - .4f) * .5f), V(W - .25f, 3f, zb - .4f - za - 4.2f), LocoBlue); // the hood
            m.Box(V(x, 4.55f, za + 2.9f), V(W - .2f, .12f, 2.7f), Mix(LocoBlue, Ink, .3f));
            m.Surface = ComicSurface.Glass;
            foreach (float sx in new[] { -.68f, .68f })
                K.Face(m, V(x + sx - .58f, 3.25f, za + 1.585f), V(x + sx + .58f, 3.25f, za + 1.585f), V(x + sx + .58f, 4.2f, za + 1.585f), V(x + sx - .58f, 4.2f, za + 1.585f), GlassDark, Vector3.back);
            SideQuad(m, xs, za + 2.2f, za + 3.5f, 3.2f, 4.05f, GlassDark, true);
            m.Surface = ComicSurface.Plain;
            SideQuad(m, xs - .125f + .0f, za + 4.3f, zb - .5f, 1.45f, 1.8f, LocoYellow);
            SideQuad(m, xs, za + .1f, za + 4.2f, 1.42f, 1.62f, LocoYellow);
            for (float z = za + 6f; z < zb - 3f; z += 3.2f) SideQuad(m, xs - .12f, z, z + 2.2f, 3.2f, 4.1f, Mix(LocoBlue, Ink, .35f), true); // louvre panels
            foreach (float sx in new[] { -.9f, .9f })
                K.Face(m, V(x + sx - .2f, 2.3f, za - .012f), V(x + sx + .2f, 2.3f, za - .012f), V(x + sx + .2f, 2.5f, za - .012f), V(x + sx - .2f, 2.5f, za - .012f), K.R.cream, Vector3.back);
            m.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 3; i++) m.Cylinder(V(x, 4.46f, za + 6.5f + i * 3.2f), .8f, .14f, SteelDark, 8);
            m.Box(V(x, 4.55f, zb - 1.6f), V(1.2f, .35f, 1.4f), SteelDark);
            m.Box(V(x, 1.1f, za - .3f), V(.4f, .3f, .7f), Ink);
            m.Box(V(x, 1.55f, za + .02f), V(W + .1f, .3f, .12f), Ink);
        }

        /// <summary>A long-distance road coach (no brand): white body with a navy skirt and a gold line, a deep dark window
        /// band, a big windscreen. Front at +Z for yaw 0.</summary>
        static void Coach(ComicMesh m, Vector3 p, float yaw)
        {
            var c = new ComicMesh();
            c.Surface = ComicSurface.PaintedMetal;
            c.Box(V(0, 2f, 0), V(2.55f, 3f, 12.4f), White);
            c.Box(V(0, .78f, 0), V(2.58f, .6f, 12.42f), Navy);
            c.Box(V(0, 1.12f, 0), V(2.59f, .08f, 12.43f), Gold);
            c.Surface = ComicSurface.Glass;
            c.Box(V(0, 2.55f, -.4f), V(2.6f, 1.15f, 10.6f), GlassDark);
            c.Box(V(0, 2.35f, 6.18f), V(2.3f, 1.7f, .08f), GlassDark);
            c.Surface = ComicSurface.Rubber;
            foreach (float z in new[] { -3.9f, -2.7f, 4.2f }) foreach (float x in new[] { -1.2f, 1.2f })
                c.Cylinder(V(x, .5f, z), .5f, .26f, Ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            c.Surface = ComicSurface.PaintedMetal;
            c.Box(V(0, 3.6f, -3f), V(1.8f, .25f, 2.6f), PlantGrey);
            foreach (float x in new[] { -1.45f, 1.45f }) c.Box(V(x, 2.6f, 6.3f), V(.12f, .5f, .22f), Ink);
            c.Box(V(0, .62f, 6.2f), V(2.5f, .3f, .1f), Ink);
            m.Add(c, p, Quaternion.Euler(0, yaw, 0), Vector3.one);
        }

        /// <summary>A tall yard floodlight: a slim mast with a cluster of lamp heads on a head frame.</summary>
        static void YardLight(ComicMesh m, Vector3 p, float h = 16f)
        {
            m.Surface = ComicSurface.Metal;
            m.Cylinder(p + V(0, h * .5f, 0), .2f, h, MastGrey, 8, .5f);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, h, 0), V(2.6f, .1f, .1f), SteelDark);
            m.Box(p + V(0, h + .5f, 0), V(1.8f, .1f, .1f), SteelDark);
            for (int i = -2; i <= 2; i++) m.Box(p + V(i * .6f, h - .2f, -.12f), V(.42f, .34f, .22f), i % 2 == 0 ? K.R.cream : White, Quaternion.Euler(25, 0, 0));
            for (int i = -1; i <= 1; i++) m.Box(p + V(i * .6f, h + .32f, -.12f), V(.42f, .34f, .22f), K.R.cream, Quaternion.Euler(25, 0, 0));
            m.Box(p + V(0, .2f, 0), V(.7f, .4f, .7f), ConcreteDark);
        }
    }
}
