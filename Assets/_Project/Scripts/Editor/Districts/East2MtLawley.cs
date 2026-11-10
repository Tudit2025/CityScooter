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
    /// <summary>MT LAWLEY (order 240) and the pieces it shares with the other two districts: street surfaces, calm ends,
    /// Federation houses and their gardens, heritage and Art Deco shops, the island platform, far-scenery helpers.</summary>
    public static partial class East2Builder
    {
        public const string MtlId = "mtlawley";
        const string Mtl = "Mtlawley";
        public static readonly string[] MtlChunkIds = { "Station", "Federation", "Beaufort", "Astor", "Avenue" };
        /// <summary>Mt Lawley sits on a hill above the river: a steady fall towards Maylands.</summary>
        public const float MtlDrop = -2.6f;
        // The roadies' loading ramp at the Astor's stage door (left lane): numbers for gameplay.
        public const float StageRampX = -2.6f, StageRampZ = 27f, StageRampWidth = 2.6f, StageRampRise = 4f, StageRampHeight = 1f, StageRampTop = 2.5f;

        static string MtlPf(string name) => Prefabs(Mtl) + "/" + name + ".prefab";

        static void BuildMtLawley(TrackTheme theme, bool atlas)
        {
            K.Begin(Meshes(Mtl), Prefabs(Mtl), DataFolder);
            Obstacle board = K.SaveObstacle(MtlPf("OBS_Mtlawley_CafeBoard"), "OBS_Mtlawley_CafeBoard", CafeBoardMesh(), new Vector3(.9f, 1.05f, .7f), ObstacleKind.Thing, true, true, .9f);
            Obstacle planter = K.SaveObstacle(MtlPf("OBS_Mtlawley_Planter"), "OBS_Mtlawley_Planter", PlanterMesh(), new Vector3(1.4f, .95f, .7f), ObstacleKind.Thing, true, false, 1f);
            Obstacle flightCase = K.SaveObstacle(MtlPf("OBS_Mtlawley_FlightCase"), "OBS_Mtlawley_FlightCase", FlightCaseMesh(), new Vector3(1.1f, .9f, .7f), ObstacleKind.Thing, true, true, 1f);
            Obstacle speakers = K.SaveObstacle(MtlPf("OBS_Mtlawley_SpeakerStack"), "OBS_Mtlawley_SpeakerStack", SpeakerStackMesh(), new Vector3(.9f, 2f, .8f), ObstacleKind.Thing, false, true, .6f);
            GameObject magpie = SaveAnimal(MtlPf("ANM_Mtlawley_Magpie"), "ANM_Mtlawley_Magpie", MagpieMesh(), new Vector3(0, .28f, 0), new Vector3(.4f, .56f, .7f), .5f, true);
            GameObject magpieStill = SaveAnimal(MtlPf("ANM_Mtlawley_MagpieStanding"), "ANM_Mtlawley_MagpieStanding", MagpieMesh(), new Vector3(0, .28f, 0), new Vector3(.4f, .56f, .7f), 0f, true);
            Ramp stage = K.SaveRamp(MtlPf("RMP_Mtlawley_StageRamp"), "RMP_Mtlawley_StageRamp", 2.6f, 4f, 1f, 2f, 1f, .5f, Hex("A9B1B7"), Charcoal, true);

            K.WriteChunk(MtlEntry(), ChunkPath(Mtl, "Entry"));
            K.WriteChunk(MtlStation(), ChunkPath(Mtl, "Station"));
            K.WriteChunk(MtlFederation(), ChunkPath(Mtl, "Federation"));
            K.WriteChunk(MtlBeaufort(), ChunkPath(Mtl, "Beaufort"));
            K.WriteChunk(MtlAstor(), ChunkPath(Mtl, "Astor"));
            K.WriteChunk(MtlAvenue(), ChunkPath(Mtl, "Avenue"));
            K.WriteChunk(MtlExit(), ChunkPath(Mtl, "Exit"));
            GameObject far = K.SaveFarScenery(MtlPf("FAR_Mtlawley_Horizon"), "FAR_Mtlawley_Horizon", MtlFar(-MtlDrop / 40f));

            District d = WriteDistrict(Mtl, MtlId, "MT LAWLEY", 240, OnePass(MtlChunkIds.Length), MtlChunkIds);
            d.obstacles = new[] { board, planter, flightCase, speakers };
            d.jumpObstacles = new[] { flightCase, planter };
            d.hazards = ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_OncomingPair", "HZ_Crossing", "HZ_SidewalkStroll", "HZ_PoliceStop");
            var people = new List<GameObject>(K.People(theme, "Barista", "Shopper", "Shopper", "Student", "Tourist", "DogWalker", "Jogger", "Elder", "Kid", "Office", "Skater"));
            people.Add(magpie); people.Add(magpieStill);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { stage };
            d.rampFrequency = .8f;
            // A warm afternoon under the jacarandas: a faint lavender haze.
            d.fogColor = new Color(.76f, .79f, .93f);
            d.skyTint = new Color(1f, .99f, 1f);
            d.ambientSky = new Color(.70f, .79f, .96f);
            d.ambientEquator = new Color(.89f, .85f, .82f);
            d.ambientGround = new Color(.63f, .56f, .50f);
            d.sunColor = new Color(1f, .95f, .86f);
            d.sunIntensity = 1.4f;
            d.farScenery = far;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/MtLawley] " + string.Join("; ", K.Report));
            if (!atlas) return;
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("Jacaranda", null, Make(m => Jacaranda(m, Vector3.zero, 1.15f, 3))),
                ("FigTree", null, Make(m => FigTree(m, Vector3.zero, 1f, 2))),
                ("CottonPalm", null, Make(m => CottonPalm(m, Vector3.zero, 11f, 1))),
                ("FederationHouse", null, Make(m => FederationHouse(m, new CqLot(-1, -6f, 6f, 5f, 10f, 0f), 0))),
                ("FederationHouseB", null, Make(m => FederationHouse(m, new CqLot(-1, -6f, 6f, 5f, 10f, 0f), 3))),
                ("FederationVilla", null, Make(m => FederationVilla(m, new CqLot(-1, -8f, 8f, 5f, 11f, 0f)))),
                ("AstorTheatre", null, Make(m => Astor(m, new CqLot(-1, -17f, 17f, 8f, 16f, 0f)))),
                ("FederationShop", null, Make(m => FederationShop(m, new CqLot(-1, -5f, 5f, 5f, 12f, 0f), Cream, White, Burgundy, 2, 1, false, "1908", 1))),
                ("DecoShop", null, Make(m => DecoShop(m, new CqLot(-1, -6f, 6f, 5f, 12f, 0f), Hex("F2E3BC"), Teal, Charcoal, 2))),
                ("SunburstLamp", null, Make(m => SunburstLamp(m, Vector3.zero, DecoBlue, true))),
                ("PlatformCanopy", null, Make(m => ButterflyCanopy(m, Vector3.zero, 12f, "MT LAWLEY"))),
                ("Train", null, Make(m => Train(m, 0f, 0f, -9f, 1, false))),
                ("Gantry", null, Make(m => Gantry(m, new Rail { A0 = 4.4f, A1 = 4.4f, B0 = 8.8f, B1 = 8.8f }, 0f))),
                ("PicketFence", null, Make(m => PicketFence(m, -1, 0f, 6f, .2f, 1.05f, White, 3f))),
                ("CafeBoard", board.gameObject, null),
                ("Planter", planter.gameObject, null),
                ("FlightCase", flightCase.gameObject, null),
                ("SpeakerStack", speakers.gameObject, null),
                ("Magpie", magpie, null),
                ("StageRamp", stage.gameObject, null),
                ("Bottlebrush", null, Make(m => Bottlebrush(m, Vector3.zero, 1f, 1))),
                ("GumTree", null, Make(m => GumTree(m, Vector3.zero, 12f, 2))),
                ("RoseBushes", null, Make(m => { FlowerBush(m, V(0, 0, 0), 1f, Hex("E85A8C"), 1); FlowerBush(m, V(1.3f, 0, .4f), .9f, White, 2); FlowerBush(m, V(.5f, 0, 1.4f), 1.1f, Hex("D8323A"), 3); })),
                ("PowerPole", null, Make(m => PowerPole(m, Vector3.zero, true))),
            };
            K.RenderAtlas("art-src/districts/mtlawley/props-atlas.png", items);
        }

        // ================================================================================== street surfaces (shared)

        /// <summary>A suburban street: asphalt, concrete footpaths with a grass verge along the kerb (driveway crossovers
        /// stay concrete).</summary>
        static void SuburbStreet(CqChunkDraft d, Color verge, float skipFrom = -1f, float skipTo = -1f, params float[] crossovers)
        {
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, PathConcrete, KerbCap, true, skipFrom, skipTo);
            for (int s = -1; s <= 1; s += 2)
                for (float z = 0f; z < d.Length - .01f; z += 2f)
                {
                    if (skipFrom >= 0f && z + 2f > skipFrom && z < skipTo) continue;
                    bool crossover = false;
                    foreach (float c in crossovers) if (Mathf.Abs(c) > .01f && Mathf.Sign(c) == s && Mathf.Abs(z + 1f - Mathf.Abs(c)) < 1.6f) crossover = true;
                    if (crossover) continue;
                    d.Road.Surface = ComicSurface.Foliage;
                    K.Face(d.Road, V(s * (K.Half + .34f), K.Kerb + .03f, z), V(s * (K.Half + .34f), K.Kerb + .03f, z + 2f), V(s * (K.Half + 2.3f), K.Kerb + .03f, z + 2f), V(s * (K.Half + 2.3f), K.Kerb + .03f, z),
                        (Mathf.RoundToInt(z / 2f) + (s > 0 ? 1 : 0)) % 4 == 0 ? Mix(verge, LawnLight, .5f) : verge, Vector3.up, false);
                }
        }

        /// <summary>Beaufort St / Eighth Ave: asphalt (or the red asphalt of Eighth Ave), red-brick paved footpaths with a
        /// cream header course along the kerb and cream bands across.</summary>
        static void BrickStreet(CqChunkDraft d, Color road, float skipFrom = -1f, float skipTo = -1f)
        {
            K.StreetSurface(d.Road, d.Length, road, PavingRed, KerbCap, true, skipFrom, skipTo);
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            for (int s = -1; s <= 1; s += 2)
                for (float z = 0f; z < d.Length - .01f; z += 4f)
                {
                    if (skipFrom >= 0f && z + 4f > skipFrom && z < skipTo) continue;
                    float y = K.Kerb + .03f;
                    K.Face(m, V(s * (K.Half + .3f), y, z), V(s * (K.Half + .3f), y, z + 4f), V(s * (K.Half + .62f), y, z + 4f), V(s * (K.Half + .62f), y, z), LimeCap, Vector3.up, false);
                    K.Face(m, V(s * (K.Half + .62f), y, z + 1.8f), V(s * (K.Half + .62f), y, z + 2.1f), V(s * (K.Edge - .05f), y, z + 2.1f), V(s * (K.Edge - .05f), y, z + 1.8f), Mix(PavingRed, LimeCap, .6f), Vector3.up, false);
                }
        }

        /// <summary>Red asphalt (the colour of Perth's shared paths and of Eighth Ave) laid over the road surface from x0
        /// to x1, cut like the street surface so it follows the same curve.</summary>
        static void RedAsphalt(ComicMesh m, float length, float x0, float x1, Color c, float skipFrom = -1f, float skipTo = -1f)
        {
            var cuts = new List<float>();
            for (float z = 0; z < length - .01f; z += 4f) cuts.Add(z);
            cuts.Add(length);
            if (skipFrom >= 0f) { cuts.Add(skipFrom); cuts.Add(skipTo); }
            cuts = cuts.Distinct().OrderBy(z => z).ToList();
            m.Surface = ComicSurface.Asphalt;
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float z0 = cuts[i], z1 = cuts[i + 1];
                if (z1 - z0 < .01f || skipFrom >= 0f && z0 >= skipFrom - .01f && z1 <= skipTo + .01f) continue;
                K.Face(m, V(x0, .016f, z0), V(x0, .016f, z1), V(x1, .016f, z1), V(x1, .016f, z0), c, Vector3.up, false);
            }
        }

        /// <summary>The Principal Shared Path beside the railway (PedestrianMall): red asphalt, a dashed white centre line,
        /// solid white edge lines; concrete shoulders with a strip of dry grass along their outer half.</summary>
        static void SharedPath(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f)
        {
            d.Type = RoadType.PedestrianMall;
            K.StreetSurface(d.Road, d.Length, K.R.asphalt, PathConcrete, KerbCap, false, skipFrom, skipTo);
            RedAsphalt(d.Road, d.Length, -K.Half, K.Half, PspRed, skipFrom, skipTo);
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            for (float z = 1f; z < d.Length - 2f; z += 4f)
            {
                if (skipFrom >= 0f && z + 2f > skipFrom && z < skipTo) continue;
                K.Face(m, V(-.06f, .03f, z), V(-.06f, .03f, z + 1.6f), V(.06f, .03f, z + 1.6f), V(.06f, .03f, z), White, Vector3.up, false);
            }
            for (float z = 0f; z < d.Length - .01f; z += 4f)
            {
                if (skipFrom >= 0f && z + 4f > skipFrom && z < skipTo) continue;
                foreach (int s in new[] { -1, 1 })
                {
                    K.Face(m, V(s * (K.Half - .5f), .03f, z), V(s * (K.Half - .5f), .03f, z + 4f), V(s * (K.Half - .4f), .03f, z + 4f), V(s * (K.Half - .4f), .03f, z), White, Vector3.up, false);
                    m.Surface = ComicSurface.Foliage;
                    K.Face(m, V(s * (K.Half + 2.1f), K.Kerb + .03f, z), V(s * (K.Half + 2.1f), K.Kerb + .03f, z + 4f), V(s * (K.Edge - .04f), K.Kerb + .03f, z + 4f), V(s * (K.Edge - .04f), K.Kerb + .03f, z),
                        Mathf.RoundToInt(z / 4f) % 3 == 0 ? Mix(LawnDry, Lawn, .4f) : LawnDry, Vector3.up, false);
                    m.Surface = ComicSurface.Plain;
                }
            }
        }

        /// <summary>A painted bicycle symbol on the shared path (white, flat), centred at (x, z), pointing along +Z.</summary>
        static void BikeSymbol(ComicMesh m, float x, float z)
        {
            m.Surface = ComicSurface.Plain;
            const float y = .032f;
            void Q(float x0, float z0, float x1, float z1) => K.Face(m, V(x + x0, y, z + z0), V(x + x0, y, z + z1), V(x + x1, y, z + z1), V(x + x1, y, z + z0), White, Vector3.up, false);
            foreach (float wz in new[] { -.55f, .55f })
            {
                Q(-.3f, wz - .3f, .3f, wz - .22f); Q(-.3f, wz + .22f, .3f, wz + .3f);
                Q(-.3f, wz - .3f, -.22f, wz + .3f); Q(.22f, wz - .3f, .3f, wz + .3f);
            }
            Q(-.04f, -.55f, .04f, .55f);
            Q(-.3f, .5f, .3f, .58f);
        }

        /// <summary>Calm end of an entry / exit chunk: lawn, a low limestone wall, a clipped hedge, small trees.</summary>
        static void CalmEdge(CqChunkDraft d, int s, float z0, float z1, int seed, int tree = 0)
        {
            SideGround(d, s, z0, z1, 60f, Lawn, ComicSurface.Foliage, .03f);
            ComicMesh w = d.SlopedFrontage;
            w.Surface = ComicSurface.Concrete;
            K.SideBox(w, s, z0, z1, -1f, K.Kerb + .55f, .05f, .55f, Limestone);
            K.SideBox(w, s, z0, z1, K.Kerb + .55f, K.Kerb + .65f, 0f, .6f, LimeCap);
            HedgeRow(w, s, z0 + .4f, z1 - .4f, .8f, 1.9f, 1.35f, Hedge);
            var r = new System.Random(seed * 53 + 11);
            for (float z = z0 + 3.5f; z < z1 - 1f; z += 7f)
            {
                Vector3 p = V(s * (K.Edge + F(r, 4.5f, 7f)), d.Ground(z), z);
                if (tree == 0) Jacaranda(d.Props, p, F(r, .62f, .75f), seed + (int)z);
                else if (tree == 1) Bottlebrush(d.Props, p, F(r, 1.1f, 1.4f), seed + (int)z);
                else GumTree(d.Props, p, F(r, 7f, 9f), seed + (int)z, false);
            }
            d.Walls.Add(new Bounds(new Vector3(s * (K.Edge + 7f), d.Y((z0 + z1) * .5f) + 1f, (z0 + z1) * .5f), new Vector3(13.5f, 4f, z1 - z0)));
            DeepSuburb(d, s, z0, z1, 18f, seed + 300);
        }

        /// <summary>A street name blade (or two, crossing) on a grey pole: white letters on dark green, facing the rider
        /// (the first) and the road (the second).</summary>
        static void StreetNameSign(ComicMesh m, Vector3 p, string towardsRider, string towardsRoad, int side)
        {
            Color green = Hex("1F6B4A");
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 1.6f, 0), .05f, 3.2f, Mast, 6);
            float w1 = TextCols(towardsRider) * .05f + .2f;
            m.Box(p + V(-side * w1 * .5f, 3f, 0), V(w1, .34f, .04f), green);
            Text(m, towardsRider, p + V(-side * w1 * .5f, 3f, -.025f), Vector3.right, Vector3.up, .05f, White);
            if (string.IsNullOrEmpty(towardsRoad)) return;
            float w2 = TextCols(towardsRoad) * .05f + .2f;
            m.Box(p + V(0, 2.6f, w2 * .5f), V(.04f, .34f, w2), green);
            Text(m, towardsRoad, p + V(-side * .025f, 2.6f, w2 * .5f), new Vector3(0, 0, side < 0 ? -1 : 1) * -1f, Vector3.up, .05f, White);
        }

        /// <summary>Festival bunting across the street from a to b: a sagging line with coloured pennants.</summary>
        static void Bunting(ComicMesh m, Vector3 a, Vector3 b, int seed)
        {
            Color[] cols = { Hex("E2463A"), Hex("F6C543"), Hex("2A9D9A"), Jac2, White, Hex("F28C28"), Hex("2F6FB0") };
            const int n = 12;
            Vector3 P(float t) => Vector3.Lerp(a, b, t) + V(0, -1.1f * Mathf.Sin(t * Mathf.PI), 0);
            m.Surface = ComicSurface.Fabric;
            for (int i = 0; i < n; i++)
            {
                Vector3 p0 = P(i / (float)n), p1 = P((i + 1) / (float)n);
                m.Surface = ComicSurface.PaintedMetal;
                m.Beam(p0, p1, .03f, Ink);
                m.Surface = ComicSurface.Fabric;
                Vector3 q0 = Vector3.Lerp(p0, p1, .15f), q1 = Vector3.Lerp(p0, p1, .85f), tip = (q0 + q1) * .5f + V(0, -.62f, 0);
                Color c = Pick(cols, seed + i);
                m.Tri(q0, q1, tip, c);
                m.Tri(q1, q0, tip, c);
            }
        }

        // ================================================================================== Mt Lawley chunks

        static Rail MtlRail(float b0, float b1) => new Rail { S = -1, Level = -2f, A0 = 4.6f, A1 = 4.6f, B0 = b0, B1 = b1, BedFrom = 1.5f, BedTo = 15.2f };

        /// <summary>The railway cutting on the left of the path: a limestone retaining wall and railing at the path's edge,
        /// the ballast bed and tracks 2 m below, a grass bank up to a limestone block wall with a chain-link fence, lawn
        /// and big trees above it.</summary>
        static void Cutting(CqChunkDraft d, Rail rl, float z0, float z1, int seed)
        {
            ComicMesh m = d.Road, w = d.SlopedFrontage;
            w.Surface = ComicSurface.Concrete;
            K.SideBox(w, -1, z0, z1, rl.Level - .6f, K.Kerb, 1f, 1.45f, Limestone);
            K.SideBox(w, -1, z0, z1, K.Kerb, K.Kerb + .12f, .95f, 1.5f, LimeCap);
            BarFence(w, -(K.Edge + 1.22f), z0, z1, K.Kerb + .12f, 1.2f, MastDark, 1);
            Tracks(d, rl, z0, z1);
            Wires(d, rl, z0, z1);
            float bx0 = rl.X(rl.BedTo + .7f), bx1 = rl.X(21f), by0 = rl.Level - .55f, by1 = .6f;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                m.Surface = ComicSurface.Foliage;
                K.Face(m, V(bx0, by0, z), V(bx0, by0, e), V(bx1, by1, e), V(bx1, by1, z), Mathf.RoundToInt(z / 4f) % 2 == 0 ? LawnDry : Mix(LawnDry, Lawn, .5f), Vector3.up + Vector3.right * .3f, false);
            }
            w.Surface = ComicSurface.Concrete;
            K.SideBox(w, -1, z0, z1, 0f, 2.4f, 21f, 21.5f, Limestone);
            K.SideBox(w, -1, z0, z1, 2.4f, 2.5f, 20.95f, 21.55f, LimeCap);
            w.Surface = ComicSurface.Plain;
            float fx = -(K.Edge + 21f) + .012f;
            for (float z = z0 + 1f; z < z1 - .2f; z += 2f)
                K.Face(w, V(fx, .7f, z), V(fx, .7f, z + .05f), V(fx, 2.35f, z + .05f), V(fx, 2.35f, z), LimeDark, Vector3.right, false);
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                K.Face(w, V(fx, 1.5f, z), V(fx, 1.5f, e), V(fx, 1.55f, e), V(fx, 1.55f, z), LimeDark, Vector3.right, false);
            }
            MeshFence(w, -(K.Edge + 21.25f), z0, z1, 2.5f, 1.7f, 1);
            m.Surface = ComicSurface.Foliage;
            K.SideBox(m, -1, z0, z1, -1f, 2.42f, 21.5f, 70f, Lawn);
            var r = new System.Random(seed * 71 + 5);
            for (float z = z0 + 2f; z < z1 - 1f; z += F(r, 4.5f, 7f))
            {
                float t = F(r, .15f, .85f);
                w.Surface = ComicSurface.Foliage;
                Blob(w, V(Mathf.Lerp(bx0, bx1, t), Mathf.Lerp(by0, by1, t) + .35f, z), V(F(r, 1.4f, 2.2f), F(r, .9f, 1.3f), F(r, 1.4f, 2.2f)), r.Next(2) == 0 ? Hedge : GumLeaf, 6, 3, z);
            }
        }

        /// <summary>The island platform between the tracks: deck with a dark face, red-brick paving on top, grey tactile
        /// strips and yellow lines along both edges (the top in the road mesh: no joint lines).</summary>
        static void IslandPlatform(CqChunkDraft d, Rail rl, float z0, float z1, float u0, float u1)
        {
            ComicMesh w = d.SlopedFrontage;
            float top = rl.Level + 1.05f;
            for (float z = z0; z < z1 - .01f; z += 8f)
            {
                float e = Mathf.Min(z1, z + 8f);
                w.Surface = ComicSurface.Concrete;
                K.MinMaxBox(w, V(rl.X(u0), rl.Level - .4f, z), V(rl.X(u1), top, e), Hex("8F8A80"));
            }
            w.Box(V(rl.X((u0 + u1) * .5f), top - .08f, (z0 + z1) * .5f), V(u1 - u0 + .16f, .16f, z1 - z0 + .1f), Concrete);
            Ground(d.Road, rl.X(u0 + .08f), rl.X(u1 - .08f), z0, z1, top + .012f, PlatformBrick, ComicSurface.Paving);
            foreach (float e in new[] { u0, u1 })
            {
                float sgn = e == u0 ? 1f : -1f;
                Ground(d.Road, rl.X(e + sgn * .62f), rl.X(e + sgn * 1.22f), z0, z1, top + .02f, Tactile, ComicSurface.Concrete);
                Ground(d.Road, rl.X(e + sgn * .46f), rl.X(e + sgn * .56f), z0, z1, top + .02f, K.R.yellow);
            }
        }

        /// <summary>Mt Lawley's platform shelter: a butterfly roof (two wings rising outwards) on a row of grey columns, the
        /// red-and-green platform boards at the end facing the rider, benches, a station name board facing the path.
        /// p = platform surface on the platform's centre line, the shelter runs from p.z to p.z + length.</summary>
        static void ButterflyCanopy(ComicMesh m, Vector3 p, float length, string name)
        {
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = .8f; z < length; z += (length - 1.6f) / 2f - .001f)
                m.Box(p + V(0, 1.55f, z), V(.24f, 3.1f, .24f), MastDark);
            m.Box(p + V(0, 3.05f, length * .5f), V(.5f, .22f, length), Mast);
            foreach (int s in new[] { -1, 1 })
            {
                m.Surface = ComicSurface.Metal;
                m.Box(p + V(s * 1.45f, 3.42f, length * .5f), V(2.9f, .12f, length + .6f), Galv, Quaternion.Euler(0, 0, s * 11f));
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(p + V(s * 2.86f, 3.69f, length * .5f), V(.12f, .2f, length + .7f), White, Quaternion.Euler(0, 0, s * 11f));
            }
            // platform boards (rider's end): red over green, white numbers
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 c = p + V(s * .62f, 1.55f, .5f);
                m.Box(c, V(1.1f, 2.2f, .12f), Charcoal);
                m.Surface = ComicSurface.Plain;
                K.Face(m, c + V(-.5f, -.2f, -.07f), c + V(.5f, -.2f, -.07f), c + V(.5f, 1f, -.07f), c + V(-.5f, 1f, -.07f), Hex("B32F45"), Vector3.back, false);
                K.Face(m, c + V(-.5f, -.75f, -.07f), c + V(.5f, -.75f, -.07f), c + V(.5f, -.2f, -.07f), c + V(-.5f, -.2f, -.07f), TrainGreen, Vector3.back, false);
                Text(m, s < 0 ? "2" : "1", c + V(0, .45f, -.085f), Vector3.right, Vector3.up, .13f, White);
                m.Surface = ComicSurface.PaintedMetal;
            }
            // the station name on a white board facing the path (+X)
            Vector3 n = p + V(1.2f, 2.35f, length * .5f);
            float w = TextCols(name) * .1f + .5f;
            m.Box(n, V(.08f, .8f, w + .1f), Charcoal);
            m.Box(n + V(.03f, 0, 0), V(.06f, .7f, w), White);
            Text(m, name, n + V(.065f, 0, 0), Vector3.forward, Vector3.up, .1f, Charcoal);
            Bench(m, p + V(-.9f, 0, length * .32f), 90f, Hex("7FA0B0"));
            Bench(m, p + V(.9f, 0, length * .7f), -90f, Hex("7FA0B0"));
        }

        /// <summary>A platform light: tall grey pole with two arms and flat lanterns (twin = Mt Lawley) or the white cone
        /// shade of Maylands and Meltham.</summary>
        static void PlatformLamp(ComicMesh m, Vector3 p, bool cone)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, 2.6f, 0), .07f, 5.2f, Mast, 6, .8f);
            if (cone)
            {
                m.Cylinder(p + V(0, 5.5f, 0), .62f, .5f, White, 8, .06f);
                m.Cylinder(p + V(0, 5.22f, 0), .5f, .07f, Mast, 8);
                return;
            }
            foreach (int s in new[] { -1, 1 })
            {
                m.Beam(p + V(0, 5f, 0), p + V(0, 5.5f, s * .9f), .06f, Mast);
                m.Box(p + V(0, 5.5f, s * 1.2f), V(.3f, .12f, .7f), Cream);
                m.Box(p + V(0, 5.58f, s * 1.2f), V(.34f, .06f, .74f), MastDark);
            }
        }

        static CqChunkDraft MtlStation()
        {
            // The shared path past Mt Lawley station: the island platform down in its cutting on the left (butterfly
            // shelter, a train at the far platform, masts and wires, the path down from the street), big figs above the
            // far wall; two Federation houses behind picket fences on the right.
            var d = new CqChunkDraft("Mtlawley_Station", MtlDrop);
            SharedPath(d);
            BikeSymbol(d.Road, -2f, 6f);
            BikeSymbol(d.Road, 2f, 26f);
            Rail rl = MtlRail(13.4f, 13.4f);
            Cutting(d, rl, 0f, 40f, 3);
            RoadBridge(d, 33.5f, 39.5f, "THIRD AVE", 1, 1, rl.Level - .6f);
            IslandPlatform(d, rl, 3f, 31.5f, 6.3f, 11.7f);
            ComicMesh w = d.SlopedFrontage;
            float top = rl.Level + 1.05f, xc = rl.X(9f);
            ButterflyCanopy(w, V(xc, top, 13f), 14f, "MT LAWLEY");
            PlatformLamp(w, V(xc, top, 8f), false);
            PlatformLamp(w, V(xc, top, 29.5f), false);
            // ticket machine and bin
            w.Surface = ComicSurface.PaintedMetal;
            w.Box(V(xc - .4f, top + .9f, 28.6f), V(.6f, 1.8f, .5f), TrainGreen);
            w.Box(V(xc - .4f, top + 1.25f, 28.34f), V(.4f, .4f, .04f), GlassDark);
            w.Cylinder(V(xc + 1f, top + .5f, 29.2f), .28f, 1f, MastDark, 8);
            // the way down: a ramp along the retaining wall, a crossing over the near track, the ramp up to the platform
            w.Surface = ComicSurface.Concrete;
            float rx0 = rl.X(1.5f), rx1 = rl.X(2.9f), rz0 = 1f, rz1 = 9.5f;
            K.Face(w, V(rx0, K.Kerb, rz0), V(rx1, K.Kerb, rz0), V(rx1, rl.Level - .1f, rz1), V(rx0, rl.Level - .1f, rz1), Concrete, Vector3.up);
            K.Face(w, V(rx1, rl.Level - .5f, rz0), V(rx1, K.Kerb, rz0), V(rx1, rl.Level - .1f, rz1), V(rx1, rl.Level - .5f, rz1), ConcreteDark, Vector3.left);
            w.Surface = ComicSurface.PaintedMetal;
            w.Beam(V(rx1, K.Kerb + 1f, rz0), V(rx1, rl.Level + .9f, rz1), .07f, K.R.yellow);
            for (float t = 0f; t <= 1.01f; t += .25f)
                w.Box(V(rx1, Mathf.Lerp(K.Kerb, rl.Level - .1f, t) + .5f, Mathf.Lerp(rz0, rz1, t)), V(.07f, 1f, .07f), Mast);
            w.Surface = ComicSurface.Rubber;
            K.MinMaxBox(w, V(rl.X(1.5f), rl.Level - .12f, 9.5f), V(rl.X(6.3f), rl.Level + .02f, 11.5f), Charcoal);
            w.Surface = ComicSurface.Concrete;
            K.Face(w, V(rl.X(6.3f), rl.Level, 9.6f), V(rl.X(6.3f), rl.Level, 11.4f), V(rl.X(8.2f), top, 11.4f), V(rl.X(8.2f), top, 9.6f), Concrete, Vector3.up);
            K.MinMaxBox(w, V(rl.X(8.2f), rl.Level - .4f, 9.6f), V(rl.X(11.7f), top, 11.4f), Hex("8F8A80"));
            Gantry(w, rl, 10f);
            Gantry(w, rl, 28f);
            Train(w, rl.X(13.4f), rl.Level, 1.5f, 2, false);
            // figs above the far wall, the roofs of Railway Parade behind
            FigTree(d.Props, V(-(K.Edge + 27f), d.Y(8f) + 2.4f, 8f), 1.25f, 1);
            FigTree(d.Props, V(-(K.Edge + 29f), d.Y(24f) + 2.4f, 24f), 1.4f, 2);
            PostSign(d.Props, V(-(K.Edge - .9f), d.Ground(.8f), .8f), "MT LAWLEY STN", .06f, White, Hex("1F6B4A"), 2.1f);
            DeepSuburb(d, -1, 0f, 32f, 38f, 21);
            // right: Federation houses facing the path
            HouseRow(d, 1, 0f, 32f, 4, 2, true);
            Jacaranda(d.Props, V(K.Edge + 2.6f, d.Ground(16f), 16f), 1.05f, 7, d.Road, K.Kerb + .034f);
            SideGround(d, 1, 32f, 40f, 60f, Lawn, ComicSurface.Foliage, .03f);
            DeepSuburb(d, 1, 0f, 32f, 22f, 22);
            d.StandardSlots();
            return d;
        }

        /// <summary>A row of Federation houses with their gardens along side s from z0 to z1 (three in 40 m), fences
        /// alternating picket and limestone, side driveways with a parked car now and then.</summary>
        static void HouseRow(CqChunkDraft d, int s, float z0, float z1, int variant, int count, bool path = false)
        {
            SideGround(d, s, z0, z1, 60f, Lawn, ComicSurface.Foliage, .03f);
            float pitch = (z1 - z0) / count;
            Color[] cars = { Hex("C9CED3"), Hex("3E5C76"), Hex("B33A32"), White, Hex("4A4F57") };
            for (int i = 0; i < count; i++)
            {
                float a = z0 + i * pitch, w = Mathf.Min(12f, pitch - 2.2f), h0 = a + (pitch - w) * .5f + (i % 2 == 0 ? -.5f : .5f);
                int v = variant + i;
                var lot = new CqLot(s, h0, h0 + w, K.Edge + 5.2f, 10f, d.Base(h0 + w * .5f));
                FederationHouse(d.Frontage, lot, v);
                d.Wall(lot, 7f);
                FrontGarden(d, lot, a, a + pitch, v, path);
            }
        }

        /// <summary>The garden in front of a house lot between za and zb: a path from the gate to the door, a picket fence
        /// or a low limestone wall with a hedge, rose bushes, a letterbox.</summary>
        static void FrontGarden(CqChunkDraft d, CqLot l, float za, float zb, int v, bool path)
        {
            int s = l.S;
            bool mirror = v % 2 == 1;
            float W = l.W, doorA = mirror ? W - (W - 4.6f - 1.2f) : W - 4.6f - 1.2f, gate = l.Z0 + doorA;
            ComicMesh g = d.Road;
            g.Surface = ComicSurface.Paving;
            float y = K.Kerb + .05f;
            K.Face(g, V(s * K.Edge, y, gate - .55f), V(s * K.Edge, y, gate + .55f), V(s * (l.Line - 2.2f), y, gate + .55f), V(s * (l.Line - 2.2f), y, gate - .55f), v % 3 == 0 ? PavingRed : PathConcrete, Vector3.up, false);
            ComicMesh w = d.SlopedFrontage;
            if (v % 3 == 1)
            {
                LowWall(w, s, za + .1f, zb - .1f, .1f, .6f, Limestone, LimeCap, gate);
                HedgeRow(w, s, za + .6f, gate - 1f, .55f, 1.3f, 1.05f, Hedge);
                HedgeRow(w, s, gate + 1f, zb - .6f, .55f, 1.3f, 1.05f, Hedge);
            }
            else PicketFence(w, s, za + .1f, zb - .1f, .22f, 1.05f, v % 3 == 0 ? White : Cream, gate);
            // side fence between the lots (the far side of this lot)
            w.Surface = ComicSurface.Wood;
            for (float u = .4f; u < 5f; u += 2.3f) w.Box(V(s * (K.Edge + u + 1.1f), K.Kerb + .55f, zb - .05f), V(2.2f, 1.1f, .07f), Mix(Timber, Cream, .5f));
            Color[] flowers = { Hex("E85A8C"), White, Hex("D8323A"), Hex("F6C543"), Jac2 };
            var r = new System.Random(v * 37 + (int)za);
            for (int i = 0; i < 3; i++)
            {
                float z = Mathf.Lerp(za + 1.2f, zb - 1.2f, (i + F(r, .2f, .8f)) / 3f);
                if (Mathf.Abs(z - gate) < 1.1f) z += 1.6f;
                FlowerBush(d.Props, V(s * (K.Edge + F(r, 1.5f, 3.4f)), d.Ground(z), z), F(r, .85f, 1.15f), Pick(flowers, v + i), v + i);
            }
            Letterbox(d.Props, V(s * (K.Edge + .5f), d.Ground(gate + 1.1f), gate + 1.1f), v % 2 == 0 ? HeritageGreen : Burgundy, v % 3 == 1);
            if (path) return;
        }

        static CqChunkDraft MtlFederation()
        {
            // A Mt Lawley street: Federation houses on both sides, jacarandas on the verges, a carpet of fallen flowers.
            var d = new CqChunkDraft("Mtlawley_Federation", MtlDrop);
            SuburbStreet(d, Lawn);
            HouseRow(d, -1, 0f, 40f, 0, 3);
            HouseRow(d, 1, 0f, 40f, 5, 3);
            VergeJacarandas(d, 1.1f, 51, -7f, 26f, 17f, -36f);
            PowerPole(d.Props, V(K.Half + .7f, d.Ground(3f), 3f), true);
            K.Thin(d.Collisions, "PowerPole", V(K.Half + .7f, d.Ground(3f), 3f), 8f, .35f);
            ParkedCar(d.Props, V(-(K.Edge + 3.4f), d.Ground(13.4f), 13.4f), 90f, Hex("3E5C76"));
            ParkedCar(d.Props, V(K.Edge + 3.4f, d.Ground(26.6f), 26.6f), -90f, Hex("C9CED3"));
            DeepSuburb(d, -1, 0f, 40f, 22f, 31);
            DeepSuburb(d, 1, 0f, 40f, 22f, 32);
            d.StandardSlots();
            return d;
        }

        /// <summary>Jacarandas on the grass verges: zs &gt; 0 on the right side, &lt; 0 on the left (|z| = position).</summary>
        static void VergeJacarandas(CqChunkDraft d, float scale, int seed, params float[] zs)
        {
            foreach (float sz in zs)
            {
                int s = sz < 0 ? -1 : 1;
                float z = Mathf.Abs(sz);
                Vector3 p = V(s * (K.Half + 1.3f), d.Ground(z), z);
                Jacaranda(d.Props, p, scale, seed + (int)z, d.Road, K.Kerb + .04f);
                K.Thin(d.Collisions, "Jacaranda", p, 2.4f, .5f);
                // more fallen flowers out on the road
                var r = new System.Random(seed + (int)z * 7);
                d.Road.Surface = ComicSurface.Plain;
                for (int i = 0; i < 5; i++)
                {
                    float x = s * F(r, .6f, 3.7f), zz = z + F(r, -3.5f, 3.5f), w = F(r, .35f, .8f);
                    if (zz < .5f || zz > d.Length - .5f) continue;
                    K.Face(d.Road, V(x - w, .024f, zz - w * .6f), V(x - w * .5f, .024f, zz + w * .7f), V(x + w, .024f, zz + w * .5f), V(x + w * .6f, .024f, zz - w * .7f), i % 2 == 0 ? Jac2 : Jac3, Vector3.up, false);
                }
            }
        }

        static CqChunkDraft MtlAvenue()
        {
            // The jacaranda avenue: the trees meet over the street; a two-storey Federation villa behind its rendered
            // wall and hedge on the left, houses on the right.
            var d = new CqChunkDraft("Mtlawley_Avenue", MtlDrop);
            SuburbStreet(d, Lawn);
            SideGround(d, -1, 0f, 40f, 60f, Lawn, ComicSurface.Foliage, .03f);
            var villa = new CqLot(-1, 3f, 20f, K.Edge + 6f, 11f, d.Base(11.5f));
            FederationVilla(d.Frontage, villa);
            d.Wall(villa, 9f);
            ComicMesh w = d.SlopedFrontage;
            LowWall(w, -1, .2f, 22.8f, .1f, 1.1f, Cream, White, 9.4f);
            HedgeRow(w, -1, .8f, 8.2f, .6f, 1.5f, 1.5f, HedgeDark);
            HedgeRow(w, -1, 10.6f, 22.4f, .6f, 1.5f, 1.5f, HedgeDark);
            d.Road.Surface = ComicSurface.Paving;
            K.Face(d.Road, V(-K.Edge, K.Kerb + .05f, 8.8f), V(-K.Edge, K.Kerb + .05f, 10f), V(-(villa.Line - 2.4f), K.Kerb + .05f, 10f), V(-(villa.Line - 2.4f), K.Kerb + .05f, 8.8f), PavingRed, Vector3.up, false);
            FlowerBush(d.Props, V(-(K.Edge + 3f), d.Ground(5f), 5f), 1.2f, Hex("E85A8C"), 4);
            FlowerBush(d.Props, V(-(K.Edge + 3.2f), d.Ground(14f), 14f), 1.2f, White, 5);
            var cottage = new CqLot(-1, 26f, 38f, K.Edge + 5.2f, 10f, d.Base(32f));
            FederationHouse(d.Frontage, cottage, 2);
            d.Wall(cottage, 7f);
            FrontGarden(d, cottage, 23.5f, 40f, 2, false);
            HouseRow(d, 1, 0f, 40f, 9, 3);
            VergeJacarandas(d, 1.2f, 71, -5f, 11f, -19f, 25f, -33f, 38f);
            DeepSuburb(d, -1, 0f, 40f, 24f, 41);
            DeepSuburb(d, 1, 0f, 40f, 22f, 42);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MtlBeaufort()
        {
            // The Beaufort St cafe strip: Federation and inter-war shops under street awnings, red-brick paving, the blue
            // sunburst lamps, cotton palms, tables under umbrellas.
            var d = new CqChunkDraft("Mtlawley_Beaufort", MtlDrop);
            BrickStreet(d, K.R.asphalt);
            SideGround(d, -1, 0f, 40f, 40f, PavingRedDark, ComicSurface.Paving);
            SideGround(d, 1, 0f, 40f, 40f, PavingRedDark, ComicSurface.Paving);
            ShopStrip(d, -1, 0f, 40f, 0);
            ShopStrip(d, 1, 0f, 40f, 4);
            StripFurniture(d, 0);
            DeepSuburb(d, -1, 0f, 40f, 16f, 51, true);
            DeepSuburb(d, 1, 0f, 40f, 16f, 52, true);
            d.StandardSlots();
            return d;
        }

        /// <summary>Lamps, palms and alfresco tables of the Beaufort St strip (colliders for what stands on the footpath).</summary>
        static void StripFurniture(CqChunkDraft d, int seed)
        {
            foreach ((int s, float z) in new[] { (-1, 9f), (1, 29f) })
            {
                Vector3 p = V(s * (K.Half + .6f), d.Ground(z), z);
                SunburstLamp(d.Props, p, DecoBlue, true);
                K.Thin(d.Collisions, "SunburstLamp", p, 7.6f, .35f);
            }
            foreach ((int s, float z) in new[] { (1, 6f), (-1, 31f) })
            {
                Vector3 p = V(s * (K.Half + .75f), d.Ground(z), z);
                CottonPalm(d.Props, p, 10.5f + s, seed + (int)z);
                K.Thin(d.Collisions, "CottonPalm", p, 9f, .5f);
            }
            Color[] canopies = { Burgundy, Cream, Teal, Mustard };
            int k = seed;
            foreach ((int s, float z) in new[] { (-1, 17f), (1, 14f), (1, 36f) })
                K.CafeUmbrella(d.Props, d.Collisions, V(s * (K.Edge - 1.5f), d.Ground(z), z), Pick(canopies, k++));
            K.PropFromKit(d.Props, d.Collisions, "Bench", V(-(K.Half + 1f), d.Ground(24f), 24f), 90f);
            K.PropFromKit(d.Props, d.Collisions, "Bin", V(-(K.Half + .8f), d.Ground(26.4f), 26.4f), 0f);
            K.PropFromKit(d.Props, d.Collisions, "BikeRack", V(K.Half + 1f, d.Ground(20f), 20f), 90f);
            K.PropFromKit(d.Props, d.Collisions, "Mailbox", V(K.Half + .8f, d.Ground(33.5f), 33.5f), -90f);
            K.PropFromKit(d.Props, d.Collisions, "FlowerPot", V(-(K.Edge - .7f), d.Ground(4f), 4f), 0f);
            K.PropFromKit(d.Props, d.Collisions, "FlowerPot", V(-(K.Edge - .7f), d.Ground(37f), 37f), 0f);
            K.PropFromKit(d.Props, d.Collisions, "FlowerPot", V(K.Edge - .7f, d.Ground(24f), 24f), 0f);
            // festival bunting between the lamp posts and the shop fronts
            Bunting(d.Props, V(-(K.Half + .6f), d.Ground(9f) + 6.6f, 9f), V(K.Edge, d.Ground(9f) + 6.4f, 12f), seed);
            Bunting(d.Props, V(K.Half + .6f, d.Ground(29f) + 6.6f, 29f), V(-K.Edge, d.Ground(29f) + 6.4f, 26f), seed + 3);
        }

        /// <summary>Four shopfronts along side s: Federation two-storey, single-storey with a pediment, Art Deco.</summary>
        static void ShopStrip(CqChunkDraft d, int s, float z0, float z1, int seed)
        {
            Color[] walls = { Cream, Hex("E9D3B8"), White, Hex("D8E0E4"), Hex("F2DCA0"), Hex("E4C9A8"), Hex("C9D9C4"), Hex("F0D9D0") };
            Color[] awn = { Burgundy, Charcoal, HeritageGreen, Navy, Teal, Hex("B8483A") };
            string[] dates = { "1905", "1912", "1908", "1924", "1910", "1936", "1914", "1927" };
            float[] widths = { 10f, 9f, 11f, 10f };
            float z = z0;
            for (int i = 0; i < widths.Length && z < z1 - 1f; i++)
            {
                float w = Mathf.Min(widths[(i + seed) % widths.Length], z1 - z);
                if (z1 - z - w < 4f) w = z1 - z;
                var l = new CqLot(s, z, z + w, K.Edge, 12f, d.Base(z + w * .5f));
                int kind = (i + seed) % 4;
                if (kind == 2) DecoShop(d.Frontage, l, Pick(walls, seed + i + 4), (seed + i) % 2 == 0 ? Teal : Burgundy, Pick(awn, seed + i + 1), seed + i);
                else FederationShop(d.Frontage, l, kind == 1 ? Brick : Pick(walls, seed + i), kind == 1 ? Cream : White, Pick(awn, seed + i), kind == 3 ? 1 : 2, (seed + i) % 3, kind == 1, Pick2(dates, seed + i), seed + i);
                d.Wall(l, 9f);
                z += w;
            }
        }

        static string Pick2(string[] list, int i) => list[((i % list.Length) + list.Length) % list.Length];

        static CqChunkDraft MtlAstor()
        {
            // The Astor Theatre on the corner (left), the Art Deco shops opposite, and the roadies' loading ramp in the
            // left lane by the stage door: flight cases and a speaker stack on the footpath.
            var d = new CqChunkDraft("Mtlawley_Astor", MtlDrop);
            BrickStreet(d, K.R.asphalt);
            SideGround(d, -1, 0f, 40f, 40f, PavingRedDark, ComicSurface.Paving);
            SideGround(d, 1, 0f, 40f, 40f, PavingRedDark, ComicSurface.Paving);
            var astor = new CqLot(-1, 3f, 37f, K.Edge, 16f, d.Base(20f));
            Astor(d.Frontage, astor);
            d.Wall(astor, 14f);
            var a1 = new CqLot(1, 0f, 13f, K.Edge, 12f, d.Base(6.5f));
            DecoShop(d.Frontage, a1, Hex("F2E3BC"), Teal, Charcoal, 3);
            d.Wall(a1, 9f);
            var a2 = new CqLot(1, 13f, 27f, K.Edge, 12f, d.Base(20f));
            DecoShop(d.Frontage, a2, White, Burgundy, Navy, 4);
            d.Wall(a2, 9f);
            var a3 = new CqLot(1, 27f, 40f, K.Edge, 12f, d.Base(33.5f));
            FederationShop(d.Frontage, a3, Hex("E9D3B8"), White, HeritageGreen, 2, 1, false, "1911", 6);
            d.Wall(a3, 9f);
            Vector3 lamp = V(K.Half + .6f, d.Ground(8f), 8f);
            SunburstLamp(d.Props, lamp, DecoBlue, true);
            K.Thin(d.Collisions, "SunburstLamp", lamp, 7.6f, .35f);
            Vector3 palm = V(K.Half + .75f, d.Ground(22f), 22f);
            CottonPalm(d.Props, palm, 11f, 5);
            K.Thin(d.Collisions, "CottonPalm", palm, 9f, .5f);
            K.CafeUmbrella(d.Props, d.Collisions, V(K.Edge - 1.5f, d.Ground(33f), 33f), Teal);
            // the corner of Beaufort St and Walcott St: traffic lights and the street names
            K.TrafficLight(d.Props, d.Collisions, -1, d.Ground(1.4f), 1.4f);
            StreetNameSign(d.Props, V(-(K.Edge - .5f), d.Ground(1.6f), 1.6f), "WALCOTT ST", "BEAUFORT ST", -1);
            K.PropFromKit(d.Props, d.Collisions, "Bench", V(K.Half + 1f, d.Ground(15f), 15f), -90f);
            K.PropFromKit(d.Props, d.Collisions, "Bin", V(K.Half + .8f, d.Ground(17.4f), 17.4f), 0f);
            K.PropFromKit(d.Props, d.Collisions, "FlowerPot", V(-(K.Edge - .7f), d.Ground(9.4f), 9.4f), 0f);
            K.PropFromKit(d.Props, d.Collisions, "FlowerPot", V(-(K.Edge - .7f), d.Ground(21.6f), 21.6f), 0f);
            Bunting(d.Props, V(K.Half + .6f, d.Ground(8f) + 6.6f, 8f), V(-K.Edge, d.Ground(8f) + 6.2f, 11.2f), 5);
            // the loading ramp (checker plate, hazard stripes) and the gear waiting on the footpath
            var spec = new CqRampSpec { Name = "StageRamp", X = StageRampX, Z = StageRampZ, Width = StageRampWidth, Rise = StageRampRise, Height = StageRampHeight, Top = StageRampTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, Hex("A9B1B7"), Charcoal, true, 0f);
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            ComicMesh p = d.Props;
            Vector3 c0 = V(-(K.Half + 1.5f), d.Ground(30f), 30f);
            p.Add(FlightCaseMesh(), c0, Quaternion.Euler(0, 20f, 0), Vector3.one);
            p.Add(FlightCaseMesh(), c0 + V(-.1f, .9f, .05f), Quaternion.Euler(0, 8f, 0), Vector3.one * .9f);
            p.Add(SpeakerStackMesh(), c0 + V(.1f, 0, 1.6f), Quaternion.Euler(0, 100f, 0), Vector3.one);
            d.Collisions.Add(new DressingObstacle { id = "StageGear", position = c0 + V(0, 0, .8f), rotation = Vector3.zero, centre = V(0, .9f, 0), size = V(1.3f, 1.8f, 2.6f), canJumpOver = false });
            DeepSuburb(d, 1, 0f, 40f, 16f, 61, true);
            foreach (float x in new[] { -2.6f, 0f, 2.6f }) d.Slot(x, 10f);
            d.Slot(2.6f, 25f);
            return d;
        }

        static CqChunkDraft MtlEntry()
        {
            // Calm at z = 0 (limestone walls, hedges, young jacarandas), then the first houses and the railway cutting
            // opening on the left.
            var d = new CqChunkDraft("Mtlawley_Entry", MtlDrop);
            SuburbStreet(d, Lawn);
            CalmEdge(d, -1, 0f, 16f, 1);
            CalmEdge(d, 1, 0f, 16f, 2);
            Rail rl = new Rail { S = -1, Level = -2f, A0 = 4.6f, A1 = 4.6f, B0 = 9f, B1 = 13.4f, BedFrom = 1.5f, BedTo = 15.2f };
            Cutting(d, rl, 16f, 40f, 4);
            // the bank closes the cutting towards the calm end
            d.Road.Surface = ComicSurface.Foliage;
            K.Face(d.Road, V(-(K.Edge + 1f), K.Kerb + .03f, 16f), V(-(K.Edge + 22f), K.Kerb + .03f, 16f), V(-(K.Edge + 22f), rl.Level - .6f, 19.5f), V(-(K.Edge + 1f), rl.Level - .6f, 19.5f), LawnDry, Vector3.up + Vector3.forward, false);
            Gantry(d.SlopedFrontage, rl, 16.5f);
            Gantry(d.SlopedFrontage, rl, 34f);
            FigTree(d.Props, V(-(K.Edge + 28f), d.Y(27f) + 2.4f, 27f), 1.2f, 4);
            HouseRow(d, 1, 16f, 40f, 7, 2);
            VergeJacarandas(d, 1f, 81, 21f);
            DeepSuburb(d, -1, 16f, 40f, 38f, 71);
            DeepSuburb(d, 1, 16f, 40f, 22f, 72);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MtlExit()
        {
            // Two last houses under jacarandas, then calm towards z = 40: low walls, hedges, small trees.
            var d = new CqChunkDraft("Mtlawley_Exit", MtlDrop);
            SuburbStreet(d, Lawn);
            HouseRow(d, -1, 0f, 26f, 12, 2);
            HouseRow(d, 1, 0f, 26f, 14, 2);
            VergeJacarandas(d, 1.1f, 91, -8f, 15f);
            CalmEdge(d, -1, 26f, 40f, 3, 1);
            CalmEdge(d, 1, 26f, 40f, 4);
            DeepSuburb(d, -1, 0f, 26f, 22f, 81);
            DeepSuburb(d, 1, 0f, 26f, 22f, 82);
            d.StandardSlots();
            return d;
        }

        // ================================================================================== houses

        /// <summary>
        /// A Federation house (Mt Lawley, c. 1905-1915), facade towards the road: limestone footings, tuck-pointed red
        /// brick with two cream render bands, a hipped iron or tile roof, a projecting wing with a half-timbered gable
        /// (bargeboards, finial), a bullnose verandah on turned white posts with brackets and a frieze rail, sash
        /// windows with sills and heads, a panelled door with a fanlight, two tall corbelled chimneys with pots.
        /// v picks colours and mirrors the plan.
        /// </summary>
        static void FederationHouse(ComicMesh m, CqLot l, int v)
        {
            float W = l.W, D = l.Depth, floor = .55f, eave = 3.95f, wingW = 4.6f, wo = 2.3f;
            bool mirror = v % 2 == 1;
            Color wall = Pick(new[] { Brick, BrickDark, BrickOrange, Hex("E9E2D0") }, v), band = v % 4 == 3 ? Hex("C9B48E") : Cream;
            Color roof = Pick(new[] { Galv, RoofRed, Tile, RoofGreen, GalvDark }, v), door = Pick(new[] { HeritageGreen, Burgundy, Navy, Hex("2F3B36") }, v);
            ComicSurface roofSurface = roof == Tile ? ComicSurface.Brick : ComicSurface.Metal;
            float A(float a) => mirror ? W - a : a;
            void Box(float a0, float a1, float u0, float u1, float o0, float o1, Color c) => K.LBox(m, l, Mathf.Min(A(a0), A(a1)), Mathf.Max(A(a0), A(a1)), u0, u1, o0, o1, c);
            float w0 = W - wingW;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, floor, .05f, -D, Limestone);
            Box(w0, W, -2.6f, floor, wo + .05f, 0f, Limestone);
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, 0, W, floor, eave, 0f, -D, wall);
            Box(w0, W, floor, eave, wo, 0f, wall);
            m.Surface = ComicSurface.Plaster;
            foreach (float u in new[] { 1.5f, 3.05f })
            {
                K.LBox(m, l, -.02f, W + .02f, u, u + .17f, .03f, -D - .02f, band);
                Box(w0 - .02f, W + .02f, u, u + .17f, wo + .03f, 0f, band);
            }
            // roofs: white eaves board, the main hip, the wing's gable
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.45f, W + .45f, eave - .12f, eave + .04f, .45f, -D - .45f, White);
            m.Surface = roofSurface;
            HipRoof(m, l, -.5f, W + .5f, eave + .04f, eave + 2.7f, .5f, -D - .5f, roof);
            float g0 = Mathf.Min(A(w0), A(W)), g1 = Mathf.Max(A(w0), A(W)), gc = (g0 + g1) * .5f;
            float ridge = GableRoof(m, l, g0, g1, eave, .62f, wo, -D * .5f, .42f, roof, Cream, White);
            m.Surface = ComicSurface.Wood;
            foreach (float da in new[] { -1.1f, 0f, 1.1f })
                K.LBox(m, l, gc + da - .06f, gc + da + .06f, eave + .05f, eave + (wingW * .5f - Mathf.Abs(da)) * .62f - .05f, wo + .05f, wo, TimberDark);
            K.LBox(m, l, g0 + .2f, g1 - .2f, eave + .5f, eave + .6f, wo + .05f, wo, TimberDark);
            K.LBox(m, l, g0 - .1f, g1 + .1f, eave - .06f, eave + .08f, wo + .14f, wo, White);
            // the wing's window: a pair of sashes under a little hood
            Sash(m, l, gc - .62f, 1.05f, floor + .8f, floor + 2.5f, wo, White, band, false, 1);
            Sash(m, l, gc + .62f, 1.05f, floor + .8f, floor + 2.5f, wo, White, band, false, 1);
            m.Surface = roofSurface;
            K.Face(m, l.P(gc - 1.5f, floor + 2.85f, wo + .7f), l.P(gc + 1.5f, floor + 2.85f, wo + .7f), l.P(gc + 1.5f, floor + 3.2f, wo), l.P(gc - 1.5f, floor + 3.2f, wo), roof, new Vector3(-l.S, 1, 0));
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float da in new[] { -1.4f, 1.4f }) m.Beam(l.P(gc + da, floor + 2.82f, wo + .68f), l.P(gc + da, floor + 2.3f, wo + .02f), .07f, White);
            // the verandah along the rest of the front
            float v0 = Mathf.Min(A(0f), A(w0)), v1 = Mathf.Max(A(0f), A(w0)), vd = 2.2f, post = floor + 2.3f;
            m.Surface = ComicSurface.Wood;
            K.LBox(m, l, v0, v1, floor - .22f, floor, vd, 0f, Mix(Timber, Cream, .25f));
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, v0, v1, -2.6f, floor - .22f, vd - .1f, 0f, Limestone);
            m.Surface = ComicSurface.PaintedMetal;
            int posts = 3;
            for (int i = 0; i < posts; i++)
            {
                float a = Mathf.Lerp(v0 + .12f, v1 - .12f, mirror ? 1f - (float)i / (posts - 1) * .86f : (float)i / (posts - 1) * .86f);
                K.LBox(m, l, a - .06f, a + .06f, floor, post, vd - .06f, vd - .18f, White);
                K.LBox(m, l, a - .1f, a + .1f, post - .22f, post - .14f, vd - .02f, vd - .22f, White);
                foreach (int sg in new[] { -1, 1 })
                    K.TriFacing(m, l.P(a + sg * .06f, post - .14f, vd - .1f), l.P(a + sg * .5f, post, vd - .1f), l.P(a + sg * .06f, post - .62f, vd - .1f), White, new Vector3(-l.S, 0, 0));
            }
            K.LBox(m, l, v0, v1, post, post + .14f, vd, vd - .16f, White);
            K.LBox(m, l, v0 + .1f, v1 - .1f, post - .34f, post - .28f, vd - .08f, vd - .14f, White);
            m.Surface = roofSurface;
            Vector3 up = new Vector3(-l.S, 2.4f, 0);
            K.Face(m, l.P(v0 - .1f, post + .78f, 0f), l.P(v1 + .1f, post + .78f, 0f), l.P(v1 + .1f, post + .5f, vd * .62f), l.P(v0 - .1f, post + .5f, vd * .62f), Mix(roof, White, .08f), up);
            K.Face(m, l.P(v0 - .1f, post + .5f, vd * .62f), l.P(v1 + .1f, post + .5f, vd * .62f), l.P(v1 + .1f, post + .12f, vd + .3f), l.P(v0 - .1f, post + .12f, vd + .3f), roof, up);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, v0 - .12f, v1 + .12f, post + .04f, post + .16f, vd + .36f, vd + .26f, White);
            // the front door (next to the wing) with a fanlight, and sash windows under the verandah
            float da0 = A(w0 - 1.2f);
            m.Surface = ComicSurface.PaintedMetal;
            K.LQuad(m, l, da0 - .62f, da0 + .62f, floor, floor + 2.75f, .012f, White);
            m.Surface = ComicSurface.Wood;
            K.LQuad(m, l, da0 - .46f, da0 + .46f, floor + .02f, floor + 2.1f, .024f, door);
            K.LQuad(m, l, da0 - .32f, da0 + .32f, floor + .2f, floor + .95f, .034f, Mix(door, Ink, .25f), false);
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, da0 - .32f, da0 + .32f, floor + 1.15f, floor + 1.95f, .034f, Hex("C9A23A"), false);
            K.LQuad(m, l, da0 - .46f, da0 + .46f, floor + 2.2f, floor + 2.65f, .024f, Glass);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, da0 - .9f, da0 + .9f, -2.6f, floor - .28f, vd + .9f, vd - .1f, Limestone);
            K.LBox(m, l, da0 - .9f, da0 + .9f, -2.6f, floor - .5f, vd + 1.4f, vd + .9f, LimeDark);
            float span = Mathf.Abs(A(w0 - 2.2f) - A(.4f));
            int windows = span > 3.6f ? 2 : 1;
            for (int i = 0; i < windows; i++)
            {
                float a = A(Mathf.Lerp(.4f, w0 - 2.2f, (i + .5f) / windows));
                Sash(m, l, a, 1.1f, floor + .8f, floor + 2.5f, 0f, White, band, false, 1);
            }
            // side window towards the rider
            EndWindow(m, l, 0f, -2.2f, -3.4f, floor + .9f, floor + 2.4f, White);
            // chimneys
            foreach ((float a, float o) in new[] { (A(W * .28f), -D * .32f), (A(W * .7f), -D * .62f) })
            {
                m.Surface = ComicSurface.Brick;
                K.LBox(m, l, a - .36f, a + .36f, eave + .6f, eave + 4.2f, o + .3f, o - .3f, wall == Hex("E9E2D0") ? Brick : wall);
                m.Surface = ComicSurface.Plaster;
                K.LBox(m, l, a - .44f, a + .44f, eave + 3.7f, eave + 3.95f, o + .38f, o - .38f, band);
                K.LBox(m, l, a - .4f, a + .4f, eave + 4.2f, eave + 4.3f, o + .34f, o - .34f, band);
                m.Surface = ComicSurface.Brick;
                foreach (float dx in new[] { -.16f, .16f }) m.Cylinder(l.P(a + dx, eave + 4.5f, o), .1f, .4f, Tile, 5, .8f);
            }
            if (ridge < 0f) return;
        }

        /// <summary>A two-storey Federation Queen Anne villa: cream render with red-brick quoins, a terracotta tile roof
        /// with a gabled bay, verandahs on both floors (white posts, balustrade and frieze), tall chimneys.</summary>
        static void FederationVilla(ComicMesh m, CqLot l)
        {
            float W = l.W, D = l.Depth, floor = .6f, f1 = 3.9f, eave = 7.2f, bay = 5.6f, bo = 2.4f;
            Color wall = Hex("F0E4C6"), trim = White;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, floor, .05f, -D, Limestone);
            K.LBox(m, l, W - bay, W, -2.6f, floor, bo + .05f, 0f, Limestone);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, W, floor, eave, 0f, -D, wall);
            K.LBox(m, l, W - bay, W, floor, eave, bo, 0f, wall);
            m.Surface = ComicSurface.Brick;
            foreach (float a in new[] { W - bay, W - .4f })
                for (float u = floor + .2f; u < eave - .4f; u += .9f) K.LBox(m, l, a, a + .4f, u, u + .45f, bo + .04f, bo, Brick);
            K.LBox(m, l, -.03f, W + .03f, f1 - .1f, f1 + .12f, .05f, -D - .03f, Brick);
            K.LBox(m, l, W - bay - .03f, W + .03f, f1 - .1f, f1 + .12f, bo + .05f, 0f, Brick);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.5f, W + .5f, eave - .12f, eave + .04f, .5f, -D - .5f, trim);
            m.Surface = ComicSurface.Brick;
            HipRoof(m, l, -.55f, W + .55f, eave + .04f, eave + 3.2f, .55f, -D - .55f, Tile);
            GableRoof(m, l, W - bay, W, eave, .68f, bo, -D * .5f, .45f, Tile, wall, trim);
            m.Surface = ComicSurface.Wood;
            float gc = W - bay * .5f;
            foreach (float da in new[] { -1.4f, -.7f, 0f, .7f, 1.4f })
                K.LBox(m, l, gc + da - .05f, gc + da + .05f, eave + .05f, eave + (bay * .5f - Mathf.Abs(da)) * .68f - .05f, bo + .05f, bo, Burgundy);
            K.LBox(m, l, W - bay - .1f, W + .1f, eave - .06f, eave + .1f, bo + .14f, bo, trim);
            // bay windows on both floors
            foreach (float u in new[] { floor + .8f, f1 + .8f })
            {
                Sash(m, l, gc - 1.2f, 1f, u, u + 1.8f, bo, trim, trim, true);
                Sash(m, l, gc, 1f, u, u + 1.8f, bo, trim, trim, true);
                Sash(m, l, gc + 1.2f, 1f, u, u + 1.8f, bo, trim, trim, true);
            }
            // the two-storey verandah
            float v1 = W - bay, vd = 2.4f;
            foreach (float fl in new[] { floor, f1 + .12f })
            {
                m.Surface = ComicSurface.Wood;
                K.LBox(m, l, 0f, v1, fl - .2f, fl, vd, 0f, Mix(Timber, Cream, .3f));
                m.Surface = ComicSurface.PaintedMetal;
                float top = fl + 2.75f;
                for (int i = 0; i < 4; i++)
                {
                    float a = Mathf.Lerp(.12f, v1 - .12f, i / 3.4f);
                    K.LBox(m, l, a - .07f, a + .07f, fl, top, vd - .06f, vd - .2f, trim);
                    foreach (int sg in new[] { -1, 1 })
                        K.TriFacing(m, l.P(a + sg * .07f, top - .1f, vd - .12f), l.P(a + sg * .55f, top, vd - .12f), l.P(a + sg * .07f, top - .6f, vd - .12f), trim, new Vector3(-l.S, 0, 0));
                }
                K.LBox(m, l, 0f, v1, top, top + .16f, vd, vd - .18f, trim);
                // balustrade: top and bottom rails, balusters as single quads
                K.LBox(m, l, .1f, v1 - .1f, fl + .95f, fl + 1.03f, vd - .08f, vd - .16f, trim);
                K.LBox(m, l, .1f, v1 - .1f, fl + .12f, fl + .18f, vd - .08f, vd - .16f, trim);
                if (fl > floor + .1f)
                    for (float a = .3f; a < v1 - .2f; a += .28f) K.LQuad(m, l, a - .04f, a + .04f, fl + .18f, fl + .95f, vd - .1f, trim, false);
                Sash(m, l, v1 * .28f, 1.1f, fl + .75f, fl + 2.4f, 0f, trim, trim);
                Sash(m, l, v1 * .72f, 1.1f, fl + (fl > floor + .1f ? .75f : 0f), fl + 2.4f, 0f, trim, trim);
            }
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0f, v1, -2.6f, floor - .2f, vd - .1f, 0f, Limestone);
            K.LBox(m, l, v1 * .72f - .9f, v1 * .72f + .9f, -2.6f, floor - .3f, vd + .9f, vd - .1f, Limestone);
            m.Surface = ComicSurface.Wood;
            K.LQuad(m, l, v1 * .72f - .42f, v1 * .72f + .42f, floor + .02f, floor + 2.05f, .03f, Burgundy);
            m.Surface = ComicSurface.Brick;
            float vr = f1 + .12f + 2.75f;
            Vector3 up = new Vector3(-l.S, 2.4f, 0);
            K.Face(m, l.P(-.1f, vr + .8f, 0f), l.P(v1 + .1f, vr + .8f, 0f), l.P(v1 + .1f, vr + .12f, vd + .35f), l.P(-.1f, vr + .12f, vd + .35f), Tile, up);
            EndWindow(m, l, 0f, -2.2f, -3.4f, floor + .9f, floor + 2.4f, trim);
            EndWindow(m, l, 0f, -2.2f, -3.4f, f1 + .9f, f1 + 2.4f, trim);
            EndWindow(m, l, 0f, -6f, -7.2f, f1 + .9f, f1 + 2.4f, trim);
            foreach ((float a, float o) in new[] { (W * .2f, -D * .4f), (W * .62f, -D * .7f) })
            {
                m.Surface = ComicSurface.Brick;
                K.LBox(m, l, a - .4f, a + .4f, eave + .6f, eave + 5f, o + .32f, o - .32f, Brick);
                m.Surface = ComicSurface.Plaster;
                K.LBox(m, l, a - .48f, a + .48f, eave + 4.5f, eave + 4.75f, o + .4f, o - .4f, wall);
                K.LBox(m, l, a - .44f, a + .44f, eave + 5f, eave + 5.1f, o + .36f, o - .36f, wall);
                m.Surface = ComicSurface.Brick;
                foreach (float dx in new[] { -.18f, .18f }) m.Cylinder(l.P(a + dx, eave + 5.3f, o), .1f, .4f, Tile, 5, .8f);
            }
        }

        // ================================================================================== shops

        /// <summary>
        /// A Federation shop on the footpath edge: ink foundation; a shadowed ground floor with tiled piers, framed
        /// display windows over a tiled stall riser, a recessed door and a leadlight transom; a cantilevered street awning
        /// on tie rods with a hanging blade sign; (two storeys) the upper wall in render or face brick with pilasters, a
        /// sill course and sash windows in moulded surrounds; a cornice, a parapet with end piers and ball finials and a
        /// pediment / raised panel carrying the year it was built; the iron roof and a chimney behind.
        /// </summary>
        static void FederationShop(ComicMesh m, CqLot l, Color wall, Color trim, Color awning, int floors, int parapet, bool brick, string year, int seed)
        {
            float W = l.W, D = l.Depth, gh = 3.5f, top = floors > 1 ? 7.4f : 4.3f;
            Color pier = brick ? BrickDark : Mix(wall, Ink, .2f), riser = Pick(new[] { HeritageGreen, Charcoal, Burgundy, Navy }, seed);
            Color doorColour = Pick(new[] { HeritageGreen, Burgundy, Hex("2F3B36"), Navy }, seed + 1);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, 0f, .02f, -D, Ink);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, W, 0f, gh, -.3f, -D, Mix(wall, Ink, .14f));
            m.Surface = brick ? ComicSurface.Brick : ComicSurface.Plaster;
            K.LBox(m, l, 0, W, gh, top, 0f, -D, wall);
            int shops = W > 9.5f ? 2 : 1;
            m.Surface = ComicSurface.Brick;
            for (int i = 0; i <= shops; i++)
            {
                float a = Mathf.Clamp(W * i / shops, .22f, W - .22f);
                K.LBox(m, l, a - .22f, a + .22f, 0f, gh, .04f, -.3f, pier);
            }
            for (int i = 0; i < shops; i++)
                Shopfront(m, l, W * i / shops + .5f, W * (i + 1) / shops - .5f, gh - .12f, -.28f, riser, true, doorColour);
            Awning(m, l, -.04f, W + .04f, gh, 2.7f, Mix(awning, Ink, .35f), awning);
            // a blade sign hanging under the awning, facing the rider
            Color sign = Pick(SignColours, seed);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, W * .5f - .04f, W * .5f + .04f, gh - .95f, gh - .35f, 2.2f, 1f, sign);
            K.LBox(m, l, W * .5f - .05f, W * .5f + .05f, gh - .35f, gh, 2.1f, 2.04f, Ink);
            K.LBox(m, l, W * .5f - .05f, W * .5f + .05f, gh - .35f, gh, 1.16f, 1.1f, Ink);
            if (floors > 1)
            {
                m.Surface = ComicSurface.Concrete;
                K.LBox(m, l, -.03f, W + .03f, gh + .78f, gh + .94f, .12f, 0f, trim);
                K.LBox(m, l, 0f, .45f, gh, top, .12f, 0f, trim);
                K.LBox(m, l, W - .45f, W, gh, top, .12f, 0f, trim);
                int n = Mathf.Max(2, Mathf.RoundToInt(W / 2.7f));
                for (int i = 0; i < n; i++)
                {
                    float a = Mathf.Lerp(.45f, W - .45f, (i + .5f) / n);
                    Sash(m, l, a, 1.1f, gh + .94f, gh + 2.9f, 0f, White, trim, true);
                    if (parapet != 0) continue;
                    m.Surface = ComicSurface.Concrete;
                    K.LBox(m, l, a - .7f, a + .7f, gh + 3.06f, gh + 3.22f, .2f, 0f, trim);                 // a little hood on brackets
                }
            }
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, -.15f, W + .15f, top, top + .3f, .36f, -.3f, trim);
            K.LBox(m, l, -.08f, W + .08f, top - .22f, top, .2f, -.2f, trim);
            m.Surface = brick ? ComicSurface.Brick : ComicSurface.Plaster;
            K.LBox(m, l, 0f, W, top + .3f, top + 1.15f, .06f, -.4f, wall);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, -.05f, W + .05f, top + 1.15f, top + 1.26f, .12f, -.46f, trim);
            foreach (float a in new[] { .3f, W - .3f })
            {
                K.LBox(m, l, a - .3f, a + .3f, top + .3f, top + 1.5f, .12f, -.46f, trim);
                m.Cylinder(l.P(a, top + 1.72f, -.17f), .2f, .34f, trim, 6, .7f);
            }
            float c = W * .5f;
            if (parapet == 1)
            {
                K.LBox(m, l, c - W * .2f, c + W * .2f, top + 1.26f, top + 1.5f, .14f, -.44f, trim);
                K.LGable(m, l, c - W * .2f, c + W * .2f, top + 1.5f, top + 2.5f, .12f, -.42f, trim);
                m.Surface = ComicSurface.Plaster;
                K.TriFacing(m, l.P(c - W * .14f, top + 1.6f, .135f), l.P(c + W * .14f, top + 1.6f, .135f), l.P(c, top + 2.28f, .135f), wall, new Vector3(-l.S, 0, 0));
            }
            else if (parapet == 2)
            {
                K.LBox(m, l, c - W * .18f, c + W * .18f, top + 1.26f, top + 2f, .14f, -.44f, trim);
                K.LBox(m, l, c - W * .12f, c + W * .12f, top + 2f, top + 2.4f, .14f, -.44f, trim);
                m.Cylinder(l.P(c, top + 2.62f, -.15f), .22f, .4f, trim, 6, .7f);
            }
            else
            {
                K.LBox(m, l, c - W * .16f, c + W * .16f, top + 1.26f, top + 1.7f, .14f, -.44f, trim);
            }
            m.Surface = ComicSurface.Plaster;
            K.LQuad(m, l, c - .9f, c + .9f, top + .42f, top + 1.04f, .072f, Mix(wall, Ink, .1f));
            LText(m, l, year, c, top + .73f, .075f, .085f, Mix(wall, Ink, .7f));
            m.Surface = ComicSurface.Metal;
            K.LBox(m, l, .3f, W - .3f, top + .2f, top + .65f, -.6f, -D + .3f, GalvDark);
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, W * .7f, W * .7f + .7f, top + .65f, top + 2.3f, -D * .55f, -D * .55f - .7f, Brick);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, W * .7f - .06f, W * .7f + .76f, top + 2.3f, top + 2.42f, -D * .55f + .06f, -D * .55f - .76f, Cream);
        }

        /// <summary>
        /// An inter-war (Art Deco) shop like the Alexander and Beaucott buildings: rendered front with horizontal speed
        /// lines, a central stepped element with three fins rising above the parapet, steel-framed windows with a grid of
        /// bars, black-tiled piers, framed display windows, a cantilevered awning with a coloured fascia.
        /// </summary>
        static void DecoShop(ComicMesh m, CqLot l, Color wall, Color accent, Color awning, int seed)
        {
            float W = l.W, D = l.Depth, gh = 3.5f, top = 7.7f, c = W * .5f;
            Color tile = Hex("2C3238");
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, 0f, .02f, -D, Ink);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, W, 0f, gh, -.3f, -D, Mix(wall, Ink, .16f));
            K.LBox(m, l, 0, W, gh, top, 0f, -D, wall);
            int shops = W > 9.5f ? 2 : 1;
            m.Surface = ComicSurface.Brick;
            for (int i = 0; i <= shops; i++)
            {
                float a = Mathf.Clamp(W * i / shops, .25f, W - .25f);
                K.LBox(m, l, a - .25f, a + .25f, 0f, gh, .04f, -.3f, tile);
                K.LBox(m, l, a - .26f, a + .26f, 2.4f, 2.5f, .05f, -.3f, accent);
            }
            for (int i = 0; i < shops; i++)
                Shopfront(m, l, W * i / shops + .55f, W * (i + 1) / shops - .55f, gh - .12f, -.28f, tile, true, Pick(new[] { Charcoal, Burgundy, Navy }, seed + i));
            Awning(m, l, -.04f, W + .04f, gh, 2.7f, Mix(awning, Ink, .3f), awning);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.05f, W + .05f, gh + .1f, gh + .16f, 2.8f, 2.7f, accent);
            // the stepped centre with three fins
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, c - 1.5f, c + 1.5f, gh, top + .9f, .14f, -.5f, wall);
            K.LBox(m, l, c - 1f, c + 1f, top + .9f, top + 1.4f, .14f, -.5f, wall);
            for (int i = -1; i <= 1; i++)
                K.LBox(m, l, c + i * .5f - .14f, c + i * .5f + .14f, gh + .4f, top + 1.9f - Mathf.Abs(i) * .35f, .3f, .14f, accent);
            // speed lines and steel windows either side
            foreach (int sg in new[] { -1, 1 })
            {
                float a0 = sg < 0 ? .3f : c + 1.5f, a1 = sg < 0 ? c - 1.5f : W - .3f, ac = (a0 + a1) * .5f, hw = Mathf.Min(1.5f, (a1 - a0) * .5f - .35f);
                m.Surface = ComicSurface.PaintedMetal;
                foreach (float u in new[] { gh + .5f, gh + .68f, gh + .86f, top - .5f })
                    K.LBox(m, l, a0, a1, u, u + .07f, .05f, 0f, accent);
                GridWindow(m, l, ac - hw, ac + hw, gh + 1.35f, gh + 3.05f, 0f, 3, 2, Charcoal, wall);
            }
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0f, W, top, top + .55f, .05f, -.4f, wall);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.03f, W + .03f, top + .55f, top + .65f, .1f, -.44f, accent);
            m.Surface = ComicSurface.Metal;
            K.LBox(m, l, .3f, W - .3f, top - .1f, top + .35f, -.6f, -D + .3f, GalvDark);
            K.LBox(m, l, W * .2f, W * .2f + 1.6f, top + .35f, top + 1.2f, -4f, -5.4f, Mast);
        }

        /// <summary>
        /// The Astor Theatre, Beaufort St at Walcott St (1914, remodelled Art Deco in 1939 by William Leighton), front
        /// towards the road, 34 m long: the corner block with its three burgundy fins, the recessed middle with speed
        /// lines and steel windows, the tower with the tall window group, three portholes, a row of small square
        /// openings and the vertical ASTOR sign on its corner (white letter boxes, red letters), the lower wing; a long
        /// street awning with the marquee over the entrance doors and poster cases; teal parapet lines; the auditorium and
        /// fly tower behind. The only lettering is ASTOR.
        /// </summary>
        static void Astor(ComicMesh m, CqLot l)
        {
            float W = l.W, D = l.Depth, gh = 3.5f, up0 = 3.7f;
            Color wall = Hex("F4F0E6"), red = Burgundy, teal = Teal, tile = Hex("2C3238");
            Vector3 toRoad = new Vector3(-l.S, 0, 0);
            Quaternion face = Quaternion.FromToRotation(Vector3.up, toRoad);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, 0f, .02f, -D, Ink);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, W, 0f, up0, -.3f, -D, Mix(wall, Ink, .22f));
            // ground floor: black-tiled piers, shopfronts, the theatre entrance under the tower
            float[] bays = { 0f, 8f, 14f, 20f, 27f, 34f };
            m.Surface = ComicSurface.Brick;
            foreach (float b in bays)
            {
                float a = Mathf.Clamp(b * W / 34f, .28f, W - .28f);
                K.LBox(m, l, a - .28f, a + .28f, 0f, gh, .05f, -.3f, tile);
                K.LBox(m, l, a - .29f, a + .29f, 2.5f, 2.6f, .06f, -.3f, teal);
            }
            for (int i = 0; i < 5; i++)
            {
                float a0 = bays[i] * W / 34f + .6f, a1 = bays[i + 1] * W / 34f - .6f;
                if (i != 3) { Shopfront(m, l, a0, a1, gh - .12f, -.28f, i % 2 == 0 ? tile : red, true, i % 2 == 0 ? red : tile); continue; }
                m.Surface = ComicSurface.PaintedMetal;
                K.LQuad(m, l, a0, a1, 0f, gh - .1f, -.28f, Ink);
                for (int k = 0; k < 3; k++)
                {
                    float dc = Mathf.Lerp(a0 + 1.5f, a1 - 1.5f, k / 2f);
                    m.Surface = ComicSurface.Wood;
                    K.LQuad(m, l, dc - .72f, dc + .72f, .04f, 2.5f, -.26f, Hex("8A4B34"));
                    m.Surface = ComicSurface.Glass;
                    K.LQuad(m, l, dc - .6f, dc - .06f, .5f, 2.3f, -.25f, GlassDark, false);
                    K.LQuad(m, l, dc + .06f, dc + .6f, .5f, 2.3f, -.25f, GlassDark, false);
                    m.Surface = ComicSurface.PaintedMetal;
                    K.LBox(m, l, dc - .12f, dc - .06f, 1.1f, 1.5f, -.18f, -.26f, Mustard);
                    K.LBox(m, l, dc + .06f, dc + .12f, 1.1f, 1.5f, -.18f, -.26f, Mustard);
                }
                // poster cases either side of the doors
                Color[] posters = { Mustard, Hex("2F6FB0"), red, Hex("2A9D9A") };
                for (int k = 0; k < 2; k++)
                {
                    float pc = k == 0 ? a0 + .45f : a1 - .45f;
                    m.Surface = ComicSurface.PaintedMetal;
                    K.LBox(m, l, pc - .4f, pc + .4f, .9f, 2.4f, -.2f, -.28f, Cream);
                    m.Surface = ComicSurface.Plain;
                    K.LQuad(m, l, pc - .32f, pc + .32f, 1f, 2.3f, -.19f, posters[k * 2], false);
                    K.LQuad(m, l, pc - .22f, pc + .22f, 1.5f, 2.1f, -.18f, posters[k * 2 + 1], false);
                }
                m.Surface = ComicSurface.Concrete;
                K.LBox(m, l, a0 - .2f, a1 + .2f, 0f, .08f, .6f, -.3f, Hex("C9BBA0"));
            }
            // the street awning with the lit marquee over the entrance
            Awning(m, l, -.06f, W + .06f, gh, 2.8f, Hex("4A4F57"), tile);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.07f, W + .07f, gh + .1f, gh + .17f, 2.9f, 2.8f, teal);
            float e0 = 20f * W / 34f, e1 = 27f * W / 34f, ec = (e0 + e1) * .5f;
            K.LBox(m, l, e0 + .2f, e1 - .2f, gh - .5f, gh + .7f, 3.2f, 2.86f, Ink);
            K.LBox(m, l, e0 + .3f, e1 - .3f, gh - .4f, gh + .6f, 3.24f, 3.2f, White);
            m.Surface = ComicSurface.Plain;
            for (int row = 0; row < 3; row++)
            {
                float u = gh + .36f - row * .3f, a = e0 + .6f;
                var r = new System.Random(row * 13 + 5);
                while (a < e1 - 1f)
                {
                    float w = F(r, .35f, 1.1f);
                    K.LQuad(m, l, a, Mathf.Min(a + w, e1 - .6f), u - .08f, u + .08f, 3.25f, Ink, false);
                    a += w + .22f;
                }
            }
            m.Surface = ComicSurface.PaintedMetal;
            // marquee end facing the rider: red border, blank white panel
            K.Face(m, l.P(e0 + .2f - .012f, gh - .4f, 2.9f), l.P(e0 + .2f - .012f, gh - .4f, 3.2f), l.P(e0 + .2f - .012f, gh + .6f, 3.2f), l.P(e0 + .2f - .012f, gh + .6f, 2.9f), red, Vector3.back, false);

            // ---- upper masses
            float cb = 8f * W / 34f, mid1 = 20f * W / 34f, tw1 = 27f * W / 34f;
            // the corner block (three burgundy fins, a steel window, a porthole)
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0f, cb, up0, 10.6f, 0f, -9f, wall);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.06f, cb + .06f, 10.6f, 10.76f, .08f, -9.06f, teal);
            m.Surface = ComicSurface.Plaster;
            for (int i = 0; i < 3; i++) K.LBox(m, l, 1.3f + i * .62f, 1.66f + i * .62f, up0 + .2f, 11.2f, .24f, 0f, red);
            GridWindow(m, l, 4.3f, 6.9f, 5.2f, 7.4f, 0f, 3, 3, Charcoal, wall);
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(l.P(5.6f, 9f, .03f), .5f, .08f, Charcoal, 10, 1f, face);
            m.Surface = ComicSurface.Glass;
            m.Cylinder(l.P(5.6f, 9f, .06f), .38f, .06f, GlassPale, 10, 1f, face);
            // its end towards the rider (the Walcott St side): fins round the corner, two windows, speed lines
            m.Surface = ComicSurface.Plaster;
            for (int i = 0; i < 3; i++)
                K.Face(m, l.P(-.012f, up0 + .2f, -1.3f - i * .62f), l.P(-.012f, up0 + .2f, -1.66f - i * .62f), l.P(-.012f, 10.6f, -1.66f - i * .62f), l.P(-.012f, 10.6f, -1.3f - i * .62f), red, Vector3.back);
            EndWindow(m, l, 0f, -4.2f, -6.2f, 5.2f, 7.4f, Charcoal);
            EndWindow(m, l, 0f, -1.2f, -7.6f, .6f, 2.9f, Ink);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float u in new[] { 8.3f, 8.5f, 8.7f })
                K.Face(m, l.P(-.012f, u, -3.8f), l.P(-.012f, u, -8.6f), l.P(-.012f, u + .07f, -8.6f), l.P(-.012f, u + .07f, -3.8f), teal, Vector3.back, false);
            // the recessed middle: speed lines, three steel windows, a plain band where the old name was
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, cb, mid1, up0, 9.4f, -.7f, -9f, wall);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, cb, mid1, 9.4f, 9.56f, -.62f, -9.06f, teal);
            foreach (float u in new[] { 4.5f, 4.72f, 4.94f })
                K.LBox(m, l, cb, mid1, u, u + .07f, -.63f, -.7f, teal);
            for (int i = 0; i < 3; i++)
            {
                float a = Mathf.Lerp(cb + 2.2f, mid1 - 2.2f, i / 2f);
                GridWindow(m, l, a - 1.2f, a + 1.2f, 5.5f, 7.4f, -.7f, 3, 3, Charcoal, wall);
            }
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, cb + 1f, mid1 - 1f, 8f, 8.9f, -.64f, -.7f, Mix(wall, teal, .12f));
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, cb + 1f, mid1 - 1f, 7.93f, 8f, -.62f, -.7f, teal);
            K.LBox(m, l, cb + 1f, mid1 - 1f, 8.9f, 8.97f, -.62f, -.7f, teal);
            // the tower
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, mid1, tw1, up0, 13.6f, .25f, -9f, wall);
            K.LBox(m, l, mid1 + 1.1f, tw1 - .5f, 4.3f, 12.3f, .31f, .25f, red);
            K.LBox(m, l, mid1 + 1.9f, tw1 - 1.3f, 4.7f, 9.9f, .38f, .31f, wall);
            float tc = (mid1 + 1.9f + tw1 - 1.3f) * .5f;
            for (int i = -1; i <= 1; i++)
            {
                m.Surface = ComicSurface.PaintedMetal;
                K.LQuad(m, l, tc + i * 1.05f - .4f, tc + i * 1.05f + .4f, 5f, 9.6f, .39f, Charcoal);
                m.Surface = ComicSurface.Glass;
                K.LQuad(m, l, tc + i * 1.05f - .32f, tc + i * 1.05f + .32f, 5.08f, 9.52f, .4f, Glass);
                m.Surface = ComicSurface.PaintedMetal;
                for (float u = 5.9f; u < 9.4f; u += .9f) K.LQuad(m, l, tc + i * 1.05f - .32f, tc + i * 1.05f + .32f, u, u + .05f, .41f, Charcoal, false);
            }
            for (int i = -1; i <= 1; i++)
            {
                m.Surface = ComicSurface.PaintedMetal;
                m.Cylinder(l.P(tc + i * 1.15f, 11.1f, .34f), .42f, .08f, wall, 10, 1f, face);
                m.Surface = ComicSurface.Glass;
                m.Cylinder(l.P(tc + i * 1.15f, 11.1f, .37f), .3f, .06f, GlassPale, 10, 1f, face);
            }
            m.Surface = ComicSurface.Plaster;
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.Lerp(mid1 + 1.5f, tw1 - .9f, i / 5f);
                K.LQuad(m, l, a - .22f, a + .22f, 12.65f, 13.1f, .262f, red);
            }
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, mid1 - .08f, tw1 + .08f, 13.6f, 13.78f, .33f, -9.08f, teal);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, mid1 + 1f, tw1 - 1f, 13.78f, 14.5f, .1f, -7f, wall);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, mid1 + .94f, tw1 - .94f, 14.5f, 14.64f, .16f, -7.06f, teal);
            // the tower's face towards the rider: red panel and a porthole
            m.Surface = ComicSurface.Plaster;
            K.Face(m, l.P(mid1 - .012f, 9.8f, -1.2f), l.P(mid1 - .012f, 9.8f, -6.5f), l.P(mid1 - .012f, 12.8f, -6.5f), l.P(mid1 - .012f, 12.8f, -1.2f), red, Vector3.back);
            // the vertical ASTOR sign: a blade on the tower's corner, white letter boxes with red letters, facing the rider
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, mid1 - .16f, mid1 + .16f, 5.3f, 13.2f, 1.75f, .25f, red);
            K.LBox(m, l, mid1 - .2f, mid1 + .2f, 13.2f, 13.36f, 1.8f, .25f, teal);
            K.LBox(m, l, mid1 - .2f, mid1 + .2f, 5.14f, 5.3f, 1.8f, .25f, teal);
            const string name = "ASTOR";
            for (int i = 0; i < name.Length; i++)
            {
                float u = 12.42f - i * 1.5f;
                Vector3 c = l.P(mid1 - .17f, u, 1f);
                m.Surface = ComicSurface.Plain;
                K.Face(m, c + V(-.6f, -.66f, 0), c + V(.6f, -.66f, 0), c + V(.6f, .66f, 0), c + V(-.6f, .66f, 0), White, Vector3.back);
                Text(m, name[i].ToString(), c + V(0, 0, -.012f), Vector3.right, Vector3.up, .2f, Hex("D8323A"));
                // the same letters towards the road, so the sign also reads from across the street
                Vector3 c2 = l.P(mid1, u, 1.76f);
                K.Face(m, c2 + V(0, -.5f, -.14f), c2 + V(0, -.5f, .14f), c2 + V(0, .5f, .14f), c2 + V(0, .5f, -.14f), White, toRoad, false);
            }
            // the lower wing
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, tw1, W, up0, 8.4f, 0f, -9f, wall);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, tw1, W + .06f, 8.4f, 8.56f, .08f, -9.06f, teal);
            foreach (float u in new[] { 7.3f, 7.5f, 7.7f }) K.LBox(m, l, tw1 + .5f, W - .5f, u, u + .07f, .05f, 0f, teal);
            GridWindow(m, l, tw1 + 1.6f, W - 1.6f, 5f, 6.8f, 0f, 4, 2, Charcoal, wall);
            // auditorium and fly tower behind
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 3f, W - 2f, up0, 12.2f, -9f, -D, Hex("EDE6D6"));
            K.LBox(m, l, 2.94f, W - 1.94f, 11f, 11.6f, -8.94f, -D - .06f, red);
            K.LBox(m, l, W * .6f, W - 3f, 12.2f, 15.6f, -10.5f, -D, wall);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, W * .6f - .06f, W - 2.94f, 15.6f, 15.76f, -10.44f, -D - .06f, teal);
            m.Surface = ComicSurface.Metal;
            K.LBox(m, l, 6f, 8.4f, 12.2f, 13.3f, -10f, -12f, Mast);
            K.LBox(m, l, 10f, 11.4f, 12.2f, 12.9f, -10.5f, -11.8f, Mast);
        }

        // ================================================================================== obstacles

        /// <summary>A cafe's chalkboard A-frame: two leaning boards (timber frame, dark board, chalk lines, a cup).</summary>
        static ComicMesh CafeBoardMesh()
        {
            var m = new ComicMesh();
            foreach (int s in new[] { -1, 1 })
            {
                Quaternion t = Quaternion.Euler(s * 14f, 0, 0);
                Vector3 c = V(0, .5f, s * .13f);
                m.Surface = ComicSurface.Wood;
                m.Box(c, V(.66f, 1.04f, .05f), Timber, t);
                m.Surface = ComicSurface.Plain;
                m.Box(c + t * V(0, .02f, s * .02f), V(.54f, .86f, .03f), Hex("2F3B36"), t);
                for (int i = 0; i < 3; i++) m.Box(c + t * V(0, .28f - i * .14f, s * .04f), V(.36f - i * .06f, .03f, .01f), Cream, t);
                m.Box(c + t * V(-.04f, -.2f, s * .04f), V(.16f, .14f, .01f), White, t);
                m.Box(c + t * V(.07f, -.2f, s * .04f), V(.05f, .08f, .01f), White, t);
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, 1.02f, 0), V(.66f, .06f, .1f), Ink);
            return m;
        }

        /// <summary>A timber planter box with lavender and white flowers.</summary>
        static ComicMesh PlanterMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Box(V(0, .04f, 0), V(1.36f, .08f, .66f), Ink);
            m.Surface = ComicSurface.Wood;
            m.Box(V(0, .36f, 0), V(1.3f, .56f, .6f), Timber);
            m.Box(V(0, .62f, 0), V(1.36f, .08f, .66f), TimberDark);
            for (float x = -.45f; x < .5f; x += .3f) m.Box(V(x, .36f, -.305f), V(.04f, .5f, .02f), TimberDark);
            m.Surface = ComicSurface.Foliage;
            Blob(m, V(-.32f, .78f, 0), V(.7f, .44f, .56f), Hedge, 6, 3);
            Blob(m, V(.32f, .8f, .02f), V(.72f, .48f, .56f), Hex("5F8F45"), 6, 3, 1f);
            m.Surface = ComicSurface.Plastic;
            Color[] fl = { Jac2, White, Jac3, Hex("E85A8C") };
            for (int i = 0; i < 6; i++) m.Box(V(-.5f + i * .2f, .92f + .04f * (i % 2), -.08f + .12f * (i % 3)), Vector3.one * .1f, fl[i % 4], Quaternion.Euler(20, i * 40, 20));
            return m;
        }

        /// <summary>A roadie's flight case on castors: black panels, silver edges and corners, butterfly latches.</summary>
        static ComicMesh FlightCaseMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Rubber };
            foreach (float x in new[] { -.42f, .42f }) foreach (float z in new[] { -.24f, .24f }) m.Box(V(x, .07f, z), V(.1f, .14f, .1f), Ink);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .5f, 0), V(1.04f, .72f, .64f), Hex("2A2E34"));
            m.Surface = ComicSurface.Metal;
            Color silver = Hex("C9CED3");
            foreach (float y in new[] { .15f, .5f, .85f }) m.Box(V(0, y, 0), V(1.08f, .05f, .68f), silver);
            foreach (float x in new[] { -.52f, .52f }) foreach (float z in new[] { -.32f, .32f }) m.Box(V(x, .5f, z), V(.06f, .74f, .06f), silver);
            foreach (float x in new[] { -.25f, .25f }) m.Box(V(x, .5f, -.335f), V(.12f, .14f, .03f), silver);
            m.Surface = ComicSurface.Plain;
            m.Box(V(.3f, .68f, -.33f), V(.26f, .14f, .012f), White);
            return m;
        }

        /// <summary>A stack of two PA speakers: black cabinets with grilles, a horn, silver corners.</summary>
        static ComicMesh SpeakerStackMesh()
        {
            var m = new ComicMesh();
            for (int i = 0; i < 2; i++)
            {
                float y = i * .98f;
                m.Surface = ComicSurface.Plastic;
                m.Box(V(0, y + .5f, 0), V(.84f, .96f, .74f), Hex("23272C"));
                m.Surface = ComicSurface.Metal;
                m.Box(V(0, y + .42f, -.375f), V(.66f, .6f, .02f), Hex("4A5057"));
                m.Cylinder(V(0, y + .4f, -.39f), .24f, .03f, Hex("2F343A"), 8, 1f, Quaternion.Euler(90, 0, 0));
                m.Box(V(0, y + .84f, -.38f), V(.5f, .14f, .03f), Hex("5B636B"));
                foreach (float x in new[] { -.4f, .4f }) foreach (float c in new[] { .06f, .92f }) m.Box(V(x, y + c, -.35f), V(.08f, .08f, .08f), Hex("C9CED3"));
            }
            return m;
        }

        // ================================================================================== far scenery (shared helpers)

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

        /// <summary>A long landmass parallel to the road on side s (near edge at |x| = xNear): a foot strip, a slope up to a
        /// wavy ridge facing the road, the top falling away behind; split every 15 m so the curl bends it.</summary>
        static void Ridge(ComicMesh m, int s, float xNear, float depth, float z0, float z1, float baseY, Func<float, float> height, Color foot, Color slope, Color top, Color haze, float hazeAmount)
        {
            Color sh = Color.Lerp(foot, haze, hazeAmount), sl = Color.Lerp(slope, haze, hazeAmount), tp = Color.Lerp(top, haze, hazeAmount);
            Vector3 toRoad = new Vector3(-s, 0, 0);
            for (float z = z0; z < z1 - .01f; z += 15f)
            {
                float za = z, zb = Mathf.Min(z1, z + 15f), ha = height(za), hb = height(zb);
                float xs = s * xNear, xm = s * (xNear + 5f), xr = s * (xNear + depth * .35f), xf = s * (xNear + depth);
                m.Surface = ComicSurface.Foliage;
                K.Face(m, V(xs, baseY, za), V(xs, baseY, zb), V(xm, baseY + 1.2f, zb), V(xm, baseY + 1.2f, za), sh, (toRoad + Vector3.up * 3f).normalized, false);
                K.Face(m, V(xm, baseY + 1.2f, za), V(xm, baseY + 1.2f, zb), V(xr, baseY + hb, zb), V(xr, baseY + ha, za), sl, (toRoad + Vector3.up).normalized, false);
                K.Face(m, V(xr, baseY + ha, za), V(xr, baseY + hb, zb), V(xf, baseY + hb * .5f, zb), V(xf, baseY + ha * .5f, za), tp, Vector3.up, false);
            }
        }

        /// <summary>Rows of suburban roofs with trees between them on side s, from |x| = x0 to x1 (far scenery).</summary>
        static void FarSuburb(ComicMesh m, System.Random r, int s, float x0, float x1, float z0, float z1, float baseY, Color haze, float treeChance, Func<float, float> lift = null)
        {
            Color[] walls = { Cream, White, BrickCream, Hex("D9CBB0"), BrickSalmon, Brick };
            Color[] roofs = { Tile, RoofRed, Galv, TileBrown, GalvDark, Tile, RoofGreen };
            Color[] leaves = { FigMid, GumLeaf, Hedge, GumLeafDark, FigDark, Jac2 };
            for (float x = x0; x < x1; x += 24f)
                for (float z = z0 + F(r, 0f, 8f); z < z1; z += F(r, 13f, 19f))
                {
                    float xx = s * (x + F(r, 0f, 12f)), dist = Mathf.Sqrt(xx * xx + z * z * .25f), y = baseY + (lift != null ? lift(Mathf.Abs(xx)) : 0f);
                    if (r.NextDouble() < treeChance)
                    {
                        float h = F(r, 8f, 15f);
                        m.Surface = ComicSurface.Foliage;
                        Blob(m, V(xx, y + h * .6f, z), V(h * .85f, h * .8f, h * .85f), Haze(Pick(leaves, r.Next(99)), dist, haze), 6, 3, z);
                        continue;
                    }
                    float w = F(r, 9f, 13f), h2 = F(r, 3.2f, 5.5f);
                    m.Surface = ComicSurface.Plaster;
                    m.Box(V(xx, y + h2 * .5f - 1f, z), V(w, h2 + 2f, w * .8f), Haze(Pick(walls, r.Next(99)), dist, haze));
                    m.Surface = ComicSurface.Metal;
                    m.Cylinder(V(xx, y + h2 + .9f, z), w * .74f, 2.2f, Haze(Pick(roofs, r.Next(99)), dist, haze), 4, .15f, Quaternion.Euler(0, 45, 0));
                }
        }

        /// <summary>A hazy tree line (big blobs) on side s around |x| = x.</summary>
        static void FarTreeLine(ComicMesh m, System.Random r, int s, float x, float spread, float z0, float z1, float baseY, float hMin, float hMax, Color a, Color b, Color haze, float hazeDist)
        {
            m.Surface = ComicSurface.Foliage;
            for (float z = z0; z < z1; z += F(r, 12f, 20f))
            {
                float h = F(r, hMin, hMax), xx = s * (x + F(r, 0f, spread));
                Blob(m, V(xx, baseY + h * .42f, z), V(F(r, 16f, 26f), h, F(r, 14f, 22f)), Haze(Color.Lerp(a, b, F(r, 0f, 1f)), hazeDist, haze), 6, 3, z);
            }
        }

        /// <summary>The Darling Scarp: the long blue ridge to the east, far beyond everything on side s.</summary>
        static void Scarp(ComicMesh m, int s, float xNear, float z0, float z1, float baseY, float height, Color haze, float seed)
        {
            Ridge(m, s, xNear, 260f, z0, z1, baseY, z => height * (1f + .16f * Mathf.Sin(z * .006f + seed) + .08f * Mathf.Sin(z * .021f + seed * 2f)),
                Hex("8FA28A"), Hex("5F7FA6"), Hex("7C98B8"), haze, .3f);
        }

        /// <summary>
        /// Mt Lawley's horizon (the CBD panorama is hidden from here on): on the left, across the railway, the suburb
        /// climbing the hill: tiled roofs, jacarandas, Norfolk pines, a church spire; on the right the roofs falling to the
        /// Swan River, the far bank's tree line with a few pale apartment towers, and the blue Darling Scarp beyond.
        /// Sheared with the road's grade. No colliders.
        /// </summary>
        static ComicMesh MtlFar(float grade)
        {
            var m = new ComicMesh();
            var r = new System.Random(240);
            Color haze = Hex("D2D3E6");
            const float z0 = -220f, z1 = 820f;
            FarStrip(m, 60f, 230f, z0, z1, -.3f, Haze(Hex("8FA86A"), 140f, haze));
            FarStrip(m, 230f, 300f, z0, z1, -4f, Haze(Reed, 260f, haze));
            FarStrip(m, 300f, 470f, z0, z1, -5f, Haze(River, 380f, haze));
            FarStrip(m, 470f, 1500f, z0, z1, -4.5f, Haze(Hex("8FA376"), 700f, haze));
            FarStrip(m, -60f, -1500f, z0, z1, -.3f, Haze(Hex("8FA86A"), 300f, haze));
            FarStrip(m, K.Edge, 60f, 330f, z1, K.Kerb - .05f, Lawn);
            FarStrip(m, -K.Edge, -60f, 330f, z1, K.Kerb - .05f, Lawn);
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 70; i++)
            {
                float x = F(r, 305f, 460f), z = F(r, z0 + 20f, z1 - 20f), len = F(r, 3f, 9f);
                K.Face(m, V(x, -4.96f, z), V(x, -4.96f, z + .6f), V(x + len, -4.96f, z + .6f), V(x + len, -4.96f, z), Haze(RiverLight, x, haze), Vector3.up, false);
            }
            // right: roofs down to the river, the far bank, the scarp
            FarSuburb(m, r, 1, 62f, 215f, z0, z1, -.3f, haze, .34f, x => -3.5f * Mathf.InverseLerp(120f, 230f, x));
            FarTreeLine(m, r, 1, 232f, 30f, z0, z1, -4f, 9f, 14f, GumLeaf, FigMid, haze, 250f);
            FarTreeLine(m, r, 1, 474f, 40f, z0, z1, -4.5f, 12f, 18f, GumLeafDark, FigMid, haze, 520f);
            foreach (float z in new[] { -120f, 60f, 110f, 330f, 420f, 640f })
            {
                float h = F(r, 26f, 48f), x = F(r, 520f, 600f);
                m.Surface = ComicSurface.Plaster;
                m.Box(V(x, h * .5f - 4f, z), V(18f, h, 22f), Haze(White, 640f, haze));
                m.Box(V(x - 9.2f, h * .5f - 4f, z), V(.6f, h * .86f, 16f), Haze(GlassPale, 640f, haze));
            }
            Scarp(m, 1, 900f, z0, z1, -4f, 60f, haze, 1.3f);
            // left: the hill of Mt Lawley
            Ridge(m, -1, 150f, 320f, z0, z1, -.3f, z => 15f + 3f * Mathf.Sin(z * .009f + 1f) + 2f * Mathf.Sin(z * .03f), Hex("86A35C"), Hex("8DA468"), Hex("9AAE7A"), haze, .25f);
            FarSuburb(m, r, -1, 62f, 150f, z0, z1, -.3f, haze, .4f);
            FarSuburb(m, r, -1, 160f, 260f, z0, z1, 5f, haze, .45f, x => 9f * Mathf.InverseLerp(160f, 262f, x));
            for (float z = z0 + 30f; z < z1; z += F(r, 70f, 120f))
            {
                // Norfolk pines above the roofs
                float x = -F(r, 110f, 240f), y = x < -160f ? 8f : 0f, h = F(r, 20f, 27f);
                m.Surface = ComicSurface.Wood;
                m.Cylinder(V(x, y + h * .5f, z), .5f, h, Haze(Hex("6E5A47"), 200f, haze), 5, .3f);
                m.Surface = ComicSurface.Foliage;
                for (int i = 0; i < 4; i++) m.Cylinder(V(x, y + Mathf.Lerp(h * .38f, h * .92f, i / 3f), z), Mathf.Lerp(h * .2f, h * .07f, i / 3f), h * .07f, Haze(Hex("2C5A36"), 200f, haze), 6, .55f);
            }
            // a church on the hill: nave, tower and spire
            Vector3 ch = V(-205f, 9f, 210f);
            m.Surface = ComicSurface.Brick;
            m.Box(ch + V(0, 5f, 0), V(12f, 10f, 24f), Haze(Brick, 240f, haze));
            m.Box(ch + V(4f, 10f, -14f), V(6f, 20f, 6f), Haze(Brick, 240f, haze));
            m.Surface = ComicSurface.Metal;
            m.Cylinder(ch + V(0, 12.4f, 0), 11f, 5f, Haze(GalvDark, 240f, haze), 4, .1f, Quaternion.Euler(0, 45, 0));
            m.Cylinder(ch + V(4f, 26f, -14f), 4.2f, 12f, Haze(Zinc, 240f, haze), 4, .04f, Quaternion.Euler(0, 45, 0));
            Scarp(m, -1, 1000f, z0, z1, -.3f, 52f, haze, 3.1f);
            m.Shear(grade);
            return m;
        }
    }
}
