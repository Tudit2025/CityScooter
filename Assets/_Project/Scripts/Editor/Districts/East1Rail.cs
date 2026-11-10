using System;
using System.Collections.Generic;
using CityScooter.EditorTools.Art;
using CityScooter.Rendering;
using UnityEngine;
using K = CityScooter.EditorTools.Districts.CbdQuayKit;

namespace CityScooter.EditorTools.Districts
{
    /// <summary>
    /// The railway kit of the east line (art agent 1): ballast, tracks, overhead wires and gantries, palisade fences,
    /// platforms and shelters, Transperth A-series and B-series sets, the Indian Pacific, the Prospector, a road coach.
    /// Everything is built in flat road space on the LEFT of the road (u = metres beyond the sidewalk edge) and goes into
    /// the chunk's sheared meshes (Road: no ink; SlopedFrontage: ink), so platforms, wires and trains follow the grade.
    /// Scenery only: nothing here adds a collider.
    /// </summary>
    public static partial class East1Builder
    {
        const float RailTop = .3f, PlatformTop = 1.25f, ContactWire = 5.6f, PortalTop = 7.6f;
        static readonly Color BallastC = K.Hex("9D9A92"), BallastDark = K.Hex("86837C"), Sleeper = K.Hex("6E6257"), RailSteel = K.Hex("4E545C"), RailShine = K.Hex("B9C0C6");
        static readonly Color MastGrey = K.Hex("A9B0B6"), Insulator = K.Hex("7A3B2A"), Tactile = K.Hex("F0D060"), PlatformFace = K.Hex("B9B5AA");
        static readonly Color Stainless = K.Hex("CDD2D6"), StainlessDark = K.Hex("9AA2AA"), TransGreen = K.Hex("1E9E4A"), SkirtRed = K.Hex("D8452B"), DoorYellow = K.Hex("F2D03A");
        static readonly Color LocoBlue = K.Hex("1F3F8A"), LocoYellow = K.Hex("F2C230"), FenceBlack = K.Hex("2A2F36");

        /// <summary>x of a point u metres beyond the left sidewalk edge.</summary>
        static float LX(float u) => -(K.Edge + u);

        /// <summary>The ballast bed from u0 to u1 beyond the left sidewalk.</summary>
        static void Ballast(CqChunkDraft d, float z0, float z1, float u0, float u1) =>
            Ground(d, -1, z0, z1, u0, u1, BallastC, ComicSurface.Concrete, .1f);

        /// <summary>One track centred u metres beyond the sidewalk: darker bed, sleepers, two rails (road-side faces shown).</summary>
        static void Track(CqChunkDraft d, float u, float z0, float z1)
        {
            ComicMesh m = d.Road;
            float x = LX(u);
            m.Surface = ComicSurface.Concrete;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.Face(m, V(x - 1.55f, .115f, z), V(x - 1.55f, .115f, e), V(x + 1.55f, .115f, e), V(x + 1.55f, .115f, z), BallastDark, Vector3.up, false);
            }
            m.Surface = ComicSurface.Wood;
            for (float z = z0 + .3f; z < z1 - .3f; z += 1.3f)
                K.Face(m, V(x - 1.25f, .13f, z), V(x - 1.25f, .13f, z + .3f), V(x + 1.25f, .13f, z + .3f), V(x + 1.25f, .13f, z), Sleeper, Vector3.up, false);
            m.Surface = ComicSurface.Metal;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                foreach (float sx in new[] { -.72f, .72f })
                {
                    K.Face(m, V(x + sx - .05f, RailTop, z), V(x + sx - .05f, RailTop, e), V(x + sx + .05f, RailTop, e), V(x + sx + .05f, RailTop, z), RailShine, Vector3.up, false);
                    K.Face(m, V(x + sx + .05f, .13f, z), V(x + sx + .05f, .13f, e), V(x + sx + .05f, RailTop, e), V(x + sx + .05f, RailTop, z), RailSteel, Vector3.right, false);
                }
            }
        }

        static void Ribbon(ComicMesh m, Vector3 a, Vector3 b, float t, Color c)
        {
            K.Face(m, a, b, b + Vector3.up * t, a + Vector3.up * t, c, Vector3.right, false);
            K.Face(m, a, b, b + Vector3.up * t, a + Vector3.up * t, c, Vector3.left, false);
        }

        /// <summary>The overhead line above a track: the contact wire and the catenary sagging between supports every
        /// <paramref name="span"/> metres (first support at <paramref name="support"/>), with droppers.</summary>
        static void Wires(CqChunkDraft d, float u, float z0, float z1, float support = 10f, float span = 20f)
        {
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            float x = LX(u);
            float Cat(float z) { float t = Mathf.Repeat(z - support, span) / span; return ContactWire + 1.25f - 3.2f * t * (1f - t); }
            for (float z = z0; z < z1 - .01f; z += 2f)
            {
                float e = Mathf.Min(z1, z + 2f);
                Ribbon(m, V(x, ContactWire, z), V(x, ContactWire, e), .05f, FenceBlack);
                // the catenary peaks at a support: do not cut the corner there
                float ya = Cat(z + .001f), yb = Cat(e - .001f);
                Ribbon(m, V(x, ya, z), V(x, yb, e), .045f, FenceBlack);
                if (Mathf.RoundToInt(z) % 4 == 0) Ribbon(m, V(x, ContactWire, z + 1f), V(x, ContactWire, z + 1.04f), Cat(z + 1f) - ContactWire, FenceBlack);
            }
        }

        static void MastColumn(ComicMesh m, float x, float z, float h)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(V(x, .35f, z), V(.6f, .5f, .6f), ConcreteDark);
            m.Surface = ComicSurface.Metal;
            m.Box(V(x, h * .5f + .3f, z), V(.26f, h, .3f), MastGrey);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(x, h + .34f, z), V(.34f, .08f, .38f), SteelDark);
        }

        /// <summary>The registration arm holding the contact wire of the track at x, hung from a support at xs.</summary>
        static void WireArm(ComicMesh m, float x, float xs, float z, float top)
        {
            m.Surface = ComicSurface.Metal;
            m.Box(V(xs, (top + ContactWire + .5f) * .5f, z), V(.1f, top - ContactWire - .5f, .1f), MastGrey);
            m.Beam(V(xs, ContactWire + 1.25f, z), V(x, ContactWire + 1.25f, z), .07f, MastGrey);
            m.Beam(V(xs, ContactWire + .55f, z), V(x, ContactWire + .08f, z), .06f, MastGrey);
            m.Surface = ComicSurface.Plastic;
            m.Cylinder(V(Mathf.Lerp(xs, x, .35f), ContactWire + 1.25f, z), .09f, .34f, Insulator, 6, 1f, Quaternion.Euler(0, 0, 90));
        }

        /// <summary>A portal gantry across the tracks at z: two galvanised masts (uL nearest the road), a lattice beam, and
        /// an arm for each track.</summary>
        static void Portal(CqChunkDraft d, float z, float uL, float uR, params float[] tracks)
        {
            ComicMesh m = d.SlopedFrontage;
            float xa = LX(uL), xb = LX(uR), y0 = PortalTop - .7f, y1 = PortalTop;
            MastColumn(m, xa, z, PortalTop);
            MastColumn(m, xb, z, PortalTop);
            m.Surface = ComicSurface.Metal;
            m.Box(V((xa + xb) * .5f, y1, z), V(Mathf.Abs(xa - xb) + .3f, .12f, .16f), MastGrey);
            m.Box(V((xa + xb) * .5f, y0, z), V(Mathf.Abs(xa - xb) + .3f, .12f, .16f), MastGrey);
            int n = Mathf.Max(3, Mathf.RoundToInt(Mathf.Abs(xa - xb) / 1.7f));
            for (int i = 0; i < n; i++)
            {
                float p0 = Mathf.Lerp(xa, xb, (float)i / n), p1 = Mathf.Lerp(xa, xb, (float)(i + 1) / n);
                m.Beam(V(p0, i % 2 == 0 ? y0 : y1, z), V(p1, i % 2 == 0 ? y1 : y0, z), .07f, MastGrey);
            }
            foreach (float u in tracks) WireArm(m, LX(u), LX(u) + 1.1f, z, y0);
        }

        /// <summary>A single mast beside the track(s) with a cantilever arm over them (uMast nearer the road than the tracks).</summary>
        static void Mast(CqChunkDraft d, float z, float uMast, params float[] tracks)
        {
            ComicMesh m = d.SlopedFrontage;
            float xm = LX(uMast), far = xm;
            foreach (float u in tracks) far = Mathf.Min(far, LX(u));
            MastColumn(m, xm, z, PortalTop);
            m.Surface = ComicSurface.Metal;
            m.Beam(V(xm, PortalTop - .5f, z), V(far - .6f, PortalTop - .5f, z), .12f, MastGrey);
            m.Beam(V(xm, PortalTop + .25f, z), V(far - .6f, PortalTop - .42f, z), .06f, MastGrey);
            foreach (float u in tracks) WireArm(m, LX(u), LX(u) + .9f, z, PortalTop - .5f);
        }

        /// <summary>A black palisade fence u metres beyond the left sidewalk (or the right one with s = 1): posts, two rails,
        /// pointed pales.</summary>
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

        /// <summary>A platform from u0 to u1: red-brown pavers, grey concrete faces, a pale coping and a yellow tactile strip
        /// along each track edge (near = the edge at u0, far = the edge at u1).</summary>
        static void Platform(CqChunkDraft d, float u0, float u1, float z0, float z1, bool near, bool far)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Paving;
            K.SideBox(m, -1, z0, z1, .1f, PlatformTop, u0, u1, PaverRed);
            ComicMesh r = d.Road;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                r.Surface = ComicSurface.Concrete;
                K.Face(r, V(LX(u0) + .012f, .1f, z), V(LX(u0) + .012f, .1f, e), V(LX(u0) + .012f, PlatformTop - .14f, e), V(LX(u0) + .012f, PlatformTop - .14f, z), PlatformFace, Vector3.right, false);
                r.Surface = ComicSurface.Plain;
                foreach ((bool on, float u, float dir) in new[] { (near, u0, 1f), (far, u1, -1f) })
                {
                    if (!on) continue;
                    float xe = LX(u);
                    K.Face(r, V(xe, PlatformTop + .012f, z), V(xe, PlatformTop + .012f, e), V(xe - dir * .18f, PlatformTop + .012f, e), V(xe - dir * .18f, PlatformTop + .012f, z), White, Vector3.up, false);
                    K.Face(r, V(xe - dir * .5f, PlatformTop + .012f, z), V(xe - dir * .5f, PlatformTop + .012f, e), V(xe - dir * .95f, PlatformTop + .012f, e), V(xe - dir * .95f, PlatformTop + .012f, z), Tactile, Vector3.up, false);
                }
            }
        }

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

        // ---------------------------------------------------------------- trains

        sealed class TrainLook
        {
            public float CarLen = 19f, Width = 2.9f;
            public float[] Doors = { -.24f, .24f };
            public bool Raked, Ribs = true;
            public Color Body = Stainless, Nose = White, Mask = K.Hex("1A1E24"), Band = SkirtRed, Stripe = TransGreen, Skirt = Color.clear, DoorEdge = Color.clear;
        }

        /// <summary>A-series: boxy flat front, white cab end with a black windscreen surround and a red band under it,
        /// ribbed stainless sides, a thin green strip above the windows.</summary>
        static TrainLook ASeries => new TrainLook();
        /// <summary>B-series: raked white nose with a black mask, green cab swoosh and roof edge, red-orange skirt, yellow
        /// door edges, ribbed stainless sides.</summary>
        static TrainLook BSeries => new TrainLook { Raked = true, Skirt = SkirtRed, DoorEdge = DoorYellow };
        /// <summary>The Prospector railcar: stainless, a raked yellow cab front, one door at each end.</summary>
        static TrainLook Prospector => new TrainLook { CarLen = 19f, Raked = true, Nose = LocoYellow, Band = K.Hex("2A3E6E"), Stripe = K.Hex("2A3E6E"), Doors = new[] { -.42f, .42f } };

        static void SideQuad(ComicMesh m, float xs, float z0, float z1, float y0, float y1, Color c, bool ink = false) =>
            K.Face(m, V(xs, y0, z0), V(xs, y0, z1), V(xs, y1, z1), V(xs, y1, z0), c, Vector3.right, ink);

        /// <summary>An electric set standing on the track at u, its rear (-Z, facing the rider) end at z0. Returns its length.</summary>
        static float Emu(ComicMesh m, float u, float z0, int cars, TrainLook k, bool lite = false, bool panto = true, bool noseFront = true, bool noseBack = true)
        {
            float x = LX(u), L = k.CarLen, W = k.Width, xs = x + W * .5f + .012f;
            for (int i = 0; i < cars; i++)
            {
                float za = z0 + i * (L + .5f), zb = za + L, zc = (za + zb) * .5f;
                m.Surface = ComicSurface.Metal;
                m.Box(V(x, 2.65f, zc), V(W, 2.8f, L), k.Body);
                m.Box(V(x, 4.14f, zc), V(W - .5f, .2f, L - .6f), Mix(k.Body, Ink, .28f));
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(x, 1.05f, zc), V(W - .3f, .5f, L - 1f), Ink);
                foreach (float f in new[] { -.32f, .32f })
                {
                    float zbg = zc + f * L;
                    m.Box(V(x, .72f, zbg), V(2.2f, .55f, 3.4f), SteelDark);
                    if (lite) continue;
                    foreach (float dz in new[] { -.95f, .95f }) m.Cylinder(V(x + 1.02f, .72f, zbg + dz), .42f, .14f, Ink, 8, 1f, Quaternion.Euler(0, 0, 90));
                }
                if (!lite)
                {
                    m.Surface = ComicSurface.Metal;
                    foreach (float f in new[] { -.22f, .22f }) m.Box(V(x, 4.38f, zc + f * L), V(W - 1.1f, .3f, 3.2f), PlantGrey);
                }
                // the side facing the road: ribs, window band between the doors, doors, stripes
                m.Surface = ComicSurface.Plain;
                if (k.Ribs) foreach (float y in new[] { 1.5f, 1.72f, 1.94f, 2.16f }) SideQuad(m, xs, za + .3f, zb - .3f, y, y + .04f, Mix(k.Body, Ink, .3f));
                if (k.Skirt.a > 0f) SideQuad(m, xs, za + .1f, zb - .1f, 1.25f, 1.42f, k.Skirt);
                if (k.Stripe.a > 0f) SideQuad(m, xs, za + .1f, zb - .1f, k.Raked ? 3.82f : 3.56f, k.Raked ? 4.02f : 3.68f, k.Stripe);
                var cuts = new List<float> { za + 1.3f };
                foreach (float f in k.Doors) { cuts.Add(zc + f * L - 1f); cuts.Add(zc + f * L + 1f); }
                cuts.Add(zb - 1.3f);
                m.Surface = ComicSurface.Glass;
                for (int c = 0; c + 1 < cuts.Count; c += 2)
                    if (cuts[c + 1] - cuts[c] > .6f) SideQuad(m, xs, cuts[c], cuts[c + 1], 2.5f, 3.4f, GlassDark, true);
                foreach (float f in k.Doors)
                {
                    float zd = zc + f * L;
                    m.Surface = ComicSurface.PaintedMetal;
                    SideQuad(m, xs + .004f, zd - .82f, zd + .82f, 1.3f, 3.55f, Mix(k.Body, White, .35f), true);
                    m.Surface = ComicSurface.Glass;
                    SideQuad(m, xs + .008f, zd - .62f, zd - .08f, 2.45f, 3.35f, GlassDark);
                    SideQuad(m, xs + .008f, zd + .08f, zd + .62f, 2.45f, 3.35f, GlassDark);
                    m.Surface = ComicSurface.Plain;
                    SideQuad(m, xs + .008f, zd - .02f, zd + .02f, 1.3f, 3.55f, Ink);
                    if (k.DoorEdge.a > 0f) { SideQuad(m, xs + .008f, zd - .82f, zd - .74f, 1.3f, 3.55f, k.DoorEdge); SideQuad(m, xs + .008f, zd + .74f, zd + .82f, 1.3f, 3.55f, k.DoorEdge); }
                }
                if (i == 0 && noseFront) Nose(m, x, za, -1f, k); else GangwayEnd(m, x, za - .25f);
                if (i == cars - 1 && noseBack) Nose(m, x, zb, 1f, k);
                if (panto && i == 0 && !lite) Pantograph(m, x, zc + L * .3f);
            }
            return cars * (L + .5f) - .5f;
        }

        static void GangwayEnd(ComicMesh m, float x, float z)
        {
            m.Surface = ComicSurface.Rubber;
            m.Box(V(x, 2.55f, z), V(1.7f, 2.4f, .5f), K.Hex("2A2F36"));
        }

        static void Pantograph(ComicMesh m, float x, float z)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(x, 4.3f, z), V(1.2f, .12f, 1.7f), SteelDark);
            m.Beam(V(x, 4.36f, z - .6f), V(x, 4.98f, z + .3f), .07f, SteelDark);
            m.Beam(V(x, 4.98f, z + .3f), V(x, ContactWire - .08f, z - .3f), .06f, SteelDark);
            m.Box(V(x, ContactWire - .05f, z - .3f), V(1.5f, .06f, .3f), SteelDark);
        }

        /// <summary>A cab end at ze, pointing along dir (-1 = towards the rider).</summary>
        static void Nose(ComicMesh m, float x, float ze, float dir, TrainLook k)
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
