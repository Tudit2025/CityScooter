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
    /// <summary>MAYLANDS (order 250): the station with the old parcels office and the Seventh Ave bridge, the Peninsula
    /// Hotel, Eighth Ave and The RISE, then the peninsula: the Maylands Brickworks, the Swan River foreshore, the
    /// boardwalk over the samphire flats. Also the road bridge over the line that Meltham shares.</summary>
    public static partial class East2Builder
    {
        public const string MayId = "maylands";
        const string May = "Maylands";
        public static readonly string[] MayChunkIds = { "Station", "EighthAve", "Brickworks", "River", "Boardwalk" };
        /// <summary>The river flats: nearly level.</summary>
        public const float MayDrop = -1.5f;
        /// <summary>The Swan River's surface below the road.</summary>
        const float MayRiver = -1.5f;
        // The boardwalk's missing span (Ramp + road-wide RoadGap): numbers for gameplay.
        public const float BoardwalkDeckStart = 3f, BoardwalkRampStart = 10f, BoardwalkRise = 6f, BoardwalkHeight = 1.4f, BoardwalkTop = 3f, BoardwalkGap = 6f;

        static string MayPf(string name) => Prefabs(May) + "/" + name + ".prefab";

        static void BuildMaylands(TrackTheme theme, bool atlas)
        {
            K.Begin(Meshes(May), Prefabs(May), DataFolder);
            Obstacle pallet = K.SaveObstacle(MayPf("OBS_Maylands_BrickPallet"), "OBS_Maylands_BrickPallet", BrickPalletMesh(), new Vector3(1.2f, .95f, 1.1f), ObstacleKind.Thing, true, false, 1f);
            Obstacle bollard = K.SaveObstacle(MayPf("OBS_Maylands_PathBollard"), "OBS_Maylands_PathBollard", Make(m => Bollard(m, Vector3.zero, Hex("F2C230"))), new Vector3(.4f, 1.1f, .4f), ObstacleKind.Thing, false, false, .8f);
            Obstacle crates = K.SaveObstacle(MayPf("OBS_Maylands_FruitCrates"), "OBS_Maylands_FruitCrates", FruitCratesMesh(), new Vector3(1.1f, .8f, .8f), ObstacleKind.Thing, true, true, 1f);
            Obstacle golfBag = K.SaveObstacle(MayPf("OBS_Maylands_GolfBag"), "OBS_Maylands_GolfBag", GolfBagMesh(), new Vector3(.6f, 1.5f, .6f), ObstacleKind.Thing, false, true, .6f);
            GameObject swan = SaveAnimal(MayPf("ANM_Maylands_BlackSwan"), "ANM_Maylands_BlackSwan", BlackSwanMesh(), new Vector3(0, .5f, 0), new Vector3(.6f, 1f, 1.2f), .35f, false);
            GameObject swanStill = SaveAnimal(MayPf("ANM_Maylands_BlackSwanResting"), "ANM_Maylands_BlackSwanResting", BlackSwanMesh(), new Vector3(0, .5f, 0), new Vector3(.6f, 1f, 1.2f), 0f, false);
            GameObject duck = SaveAnimal(MayPf("ANM_Maylands_Duck"), "ANM_Maylands_Duck", DuckMesh(), new Vector3(0, .2f, 0), new Vector3(.35f, .4f, .6f), .5f, true);
            Ramp kicker = K.SaveRamp(MayPf("RMP_Maylands_Boardwalk"), "RMP_Maylands_Boardwalk", 3f, 5f, 1.3f, 1.5f, 1f, .5f, DeckLight, Deck, false);

            K.WriteChunk(MayEntry(), ChunkPath(May, "Entry"));
            K.WriteChunk(MayStation(), ChunkPath(May, "Station"));
            K.WriteChunk(MayEighthAve(), ChunkPath(May, "EighthAve"));
            K.WriteChunk(MayBrickworks(), ChunkPath(May, "Brickworks"));
            K.WriteChunk(MayRiverChunk(), ChunkPath(May, "River"));
            K.WriteChunk(MayBoardwalk(), ChunkPath(May, "Boardwalk"));
            K.WriteChunk(MayExit(), ChunkPath(May, "Exit"));
            GameObject far = K.SaveFarScenery(MayPf("FAR_Maylands_Horizon"), "FAR_Maylands_Horizon", MayFar(-MayDrop / 40f));

            District d = WriteDistrict(May, MayId, "MAYLANDS", 250, OnePass(MayChunkIds.Length), MayChunkIds);
            d.obstacles = new[] { pallet, bollard, crates, golfBag };
            d.jumpObstacles = new[] { crates, pallet };
            d.hazards = ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_OncomingPair", "HZ_Crossing", "HZ_SidewalkStroll", "HZ_PoliceStop");
            var people = new List<GameObject>(K.People(theme, "Jogger", "Jogger", "DogWalker", "DogWalker", "Shopper", "Barista", "Elder", "Kid", "Tourist", "Skater"));
            people.Add(swan); people.Add(swanStill); people.Add(duck); people.Add(duck);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { kicker };
            d.rampFrequency = 1f;
            // Bright river light: a cool, clear haze off the water.
            d.fogColor = new Color(.70f, .82f, .93f);
            d.skyTint = new Color(.99f, 1f, 1f);
            d.ambientSky = new Color(.69f, .84f, .98f);
            d.ambientEquator = new Color(.88f, .88f, .82f);
            d.ambientGround = new Color(.66f, .62f, .5f);
            d.sunColor = new Color(1f, .96f, .88f);
            d.sunIntensity = 1.45f;
            d.farScenery = far;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/Maylands] " + string.Join("; ", K.Report));
            if (!atlas) return;
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("PeninsulaHotel", null, Make(m => PeninsulaHotel(m, new CqLot(-1, -13f, 13f, 6f, 13f, 0f)))),
                ("ParcelsOffice", null, Make(m => ParcelsOffice(m, Vector3.zero, 10f))),
                ("PlatformCanopy", null, Make(m => TrussCanopy(m, Vector3.zero, 10f))),
                ("TheRise", null, Make(m => TheRise(m, new CqLot(-1, -11f, 11f, 6f, 14f, 0f)))),
                ("HoffmanKiln", null, Make(m => HoffmanKiln(m, new CqLot(-1, -13f, 13f, 6f, 12f, 0f)))),
                ("Chimney", null, Make(m => BrickChimney(m, Vector3.zero, 34f))),
                ("PugMill", null, Make(m => PugMill(m, new CqLot(-1, -5f, 5f, 5f, 9f, 0f)))),
                ("DryingShed", null, Make(m => DryingShed(m, new CqLot(-1, -9f, 9f, 3f, 5f, 0f)))),
                ("Gatehouse", null, Make(m => Gatehouse(m, new CqLot(-1, -3f, 3f, 3f, 6f, 0f)))),
                ("RoadBridge", null, Make(m => RoadBridgeArt(m, 0f, 6f, 0f, "SEVENTH AVE", 1))),
                ("Paperbark", null, Make(m => Paperbark(m, Vector3.zero, 7f, 2))),
                ("Sheoak", null, Make(m => Sheoak(m, Vector3.zero, 9f, 1))),
                ("Reeds", null, Make(m => { for (int i = 0; i < 5; i++) Reeds(m, V(i * .9f, 0, (i % 2) * .7f), 1.2f, i); })),
                ("GolfGreen", null, Make(m => GolfGreen(m, Vector3.zero))),
                ("GolfBuggy", null, Make(m => GolfBuggy(m, Vector3.zero, 30f))),
                ("PicnicShelter", null, Make(m => PicnicShelter(m, Vector3.zero))),
                ("Rowers", null, Make(m => RowingShell(m, Vector3.zero, 0f))),
                ("SunburstLampBlack", null, Make(m => SunburstLamp(m, Vector3.zero, Charcoal, true))),
                ("BrickPallet", pallet.gameObject, null),
                ("PathBollard", bollard.gameObject, null),
                ("FruitCrates", crates.gameObject, null),
                ("GolfBag", golfBag.gameObject, null),
                ("BlackSwan", swan, null),
                ("Duck", duck, null),
                ("BoardwalkKicker", kicker.gameObject, null),
                ("TrainB", null, Make(m => Train(m, 0f, 0f, -9f, 1, false))),
                ("TrainA", null, Make(m => Train(m, 0f, 0f, -9f, 1, true))),
                ("PlatformLampCone", null, Make(m => PlatformLamp(m, Vector3.zero, true))),
            };
            K.RenderAtlas("art-src/districts/maylands/props-atlas.png", items);
        }

        // ================================================================================== the railway at grade (shared)

        static Rail FlatRail(float b0, float b1) => new Rail { S = -1, Level = .16f, A0 = 4.4f, A1 = 4.4f, B0 = b0, B1 = b1, BedFrom = 1.7f, BedTo = 15.8f };

        /// <summary>The railway beside the road at street level from z0 to z1: a chain-link fence at the footpath's edge,
        /// ballast and tracks, wires, and on the far side a second fence, lawn, and the trees and roofs of Railway Parade.</summary>
        static void RailCorridor(CqChunkDraft d, Rail rl, float z0, float z1, int seed, bool farTrees = true, float gap0 = -1f, float gap1 = -1f)
        {
            ComicMesh w = d.SlopedFrontage;
            d.Road.Surface = ComicSurface.Asphalt;
            K.SideBox(d.Road, -1, z0, z1, -2.4f, rl.Level - .52f, 0f, 19f, BallastDark);
            w.Surface = ComicSurface.Concrete;
            K.SideBox(w, -1, z0, z1, -1f, K.Kerb + .1f, .02f, .5f, Concrete);
            if (gap0 < 0f) MeshFence(w, -(K.Edge + .26f), z0, z1, K.Kerb + .1f, 1.8f, 1);
            else
            {
                if (gap0 - z0 > .5f) MeshFence(w, -(K.Edge + .26f), z0, gap0, K.Kerb + .1f, 1.8f, 1);
                MeshFence(w, -(K.Edge + .26f), gap1, z1, K.Kerb + .1f, 1.8f, 1);
            }
            Tracks(d, rl, z0, z1);
            Wires(d, rl, z0, z1);
            MeshFence(w, -(K.Edge + 17.4f), z0, z1, rl.Level - .5f, 1.8f, 1);
            d.Road.Surface = ComicSurface.Foliage;
            K.SideBox(d.Road, -1, z0, z1, -2.4f, K.Kerb, 18f, 70f, Lawn);
            Ground(d.Road, -(K.Edge + 19.5f), -(K.Edge + 26.5f), z0, z1, K.Kerb + .02f, K.R.asphalt, ComicSurface.Asphalt);   // Railway Parade
            if (!farTrees) return;
            var r = new System.Random(seed * 17 + 3);
            for (float z = z0 + F(r, 2f, 6f); z < z1 - 1f; z += F(r, 9f, 14f))
            {
                Vector3 p = V(-(K.Edge + F(r, 28f, 31f)), d.Ground(z), z);
                if (r.Next(3) == 0) GumTree(d.Props, p, F(r, 9f, 12f), seed + (int)z, false);
                else Bottlebrush(d.Props, p, F(r, 1.3f, 1.7f), seed + (int)z);
            }
            DeepSuburb(d, -1, z0, z1, 32f, seed + 40);
        }

        /// <summary>Where the railway begins or ends beside the road (the calm end of an entry or exit chunk): a bank of
        /// shrubs across the tracks, so the line disappears into the green.</summary>
        static void RailEnd(CqChunkDraft d, float z, int seed)
        {
            ComicMesh w = d.SlopedFrontage;
            w.Surface = ComicSurface.Foliage;
            var r = new System.Random(seed * 77 + 5);
            for (float u = 1.6f; u < 19f; u += 3.1f)
                Blob(w, V(-(K.Edge + u), K.Kerb + F(r, .9f, 1.5f), z + F(r, -.5f, .5f)), V(F(r, 4f, 5f), F(r, 3.2f, 4.6f), F(r, 3f, 4f)), r.Next(2) == 0 ? Hedge : HedgeDark, 7, 4, u, .18f);
        }

        /// <summary>A road bridge over the railway and the street (Third Ave in Mt Lawley, Seventh Ave in Maylands, Hotham St
        /// in Meltham): the deck with concrete parapets and safety screens, pier rows beside and beyond the tracks,
        /// abutment walls with a mural, limestone-walled approaches carrying the road away on both sides, lamp posts, and
        /// a green name board over the road facing the rider. It closes the view where the railway joins or leaves the
        /// road. No colliders.</summary>
        static void RoadBridge(CqChunkDraft d, float z0, float z1, string name, int seed, int close = 0, float bed = -.6f)
        {
            float y = d.Y((z0 + z1) * .5f);
            RoadBridgeArt(d.Frontage, z0, z1, y, name, seed);
            if (close == 0) return;
            // close: the railway only exists on one side of the bridge (-1: it starts here, +1: it ends here). The far
            // side of the span over the tracks is shut with a dark face, so the line runs into the shade under the deck.
            ComicMesh m = d.Frontage;
            float z = close > 0 ? z1 - .32f : z0 + .32f, t = close > 0 ? .3f : -.3f, xa = -(K.Edge + 1.75f), xb = -(K.Edge + 27f);
            if (close > 0)
            {
                m.Surface = ComicSurface.Plain;
                K.MinMaxBox(m, V(xa, y + bed - 2f, z), V(xb, y + 5.62f, z + t), Hex("1E242C"));
                return;
            }
            // Seen from in front (the line starts behind it): a concrete wall under the deck with a long mural of big
            // simple shapes (no words).
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(xa, y + bed - 2f, z), V(xb, y + 5.62f, z + t), Concrete);
            float zf = z + t - .012f;
            Color[] paint = { Hex("E2463A"), Hex("F6C543"), Hex("2A9D9A"), Hex("2F6FB0"), Jac2, Hex("F28C28"), White, Hex("7FC66A") };
            var r = new System.Random(seed * 37 + 11);
            m.Surface = ComicSurface.Plain;
            K.Face(m, V(xa - .6f, y + .5f, zf), V(xb + 2f, y + .5f, zf), V(xb + 2f, y + 4.4f, zf), V(xa - .6f, y + 4.4f, zf), Hex("2C4A6E"), Vector3.back, false);
            for (int i = 0; i < 16; i++)
            {
                float x = Mathf.Lerp(xa - 1.2f, xb + 2.8f, (i + F(r, .1f, .9f)) / 16f), u = F(r, .8f, 3.2f), w = F(r, .5f, 1.1f), h = F(r, .5f, 1.2f);
                K.Face(m, V(x - w, y + u, zf - .012f), V(x + w, y + u - h * .2f, zf - .012f), V(x + w * .8f, y + u + h, zf - .012f), V(x - w * .7f, y + u + h * 1.1f, zf - .012f), Pick(paint, seed + i), Vector3.back, false);
            }
        }

        static void RoadBridgeArt(ComicMesh m, float z0, float z1, float y, string name, int seed)
        {
            float xl = -(K.Edge + 27f), xr = K.Edge + 2.2f, zc = (z0 + z1) * .5f, len = z1 - z0, top = y + 7.1f, under = y + 6.1f;
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(xl - 30f, under, z0), V(xr + 30f, top, z1), Concrete);
            K.MinMaxBox(m, V(xl - 30f, under - .5f, z0 + .5f), V(xr + 30f, under, z1 - .5f), ConcreteDark);          // girders
            m.Surface = ComicSurface.Asphalt;
            K.MinMaxBox(m, V(xl - 30f, top, z0 + .4f), V(xr + 30f, top + .05f, z1 - .4f), K.R.asphalt);
            // parapets and safety screens
            foreach (float z in new[] { z0, z1 - .35f })
            {
                m.Surface = ComicSurface.Concrete;
                K.MinMaxBox(m, V(xl - 30f, top, z), V(xr + 30f, top + 1.05f, z + .35f), Concrete);
                K.MinMaxBox(m, V(xl - 30f, top + 1.05f, z - .04f), V(xr + 30f, top + 1.15f, z + .39f), LimeCap);
                m.Surface = ComicSurface.PaintedMetal;
                for (float x = xl + 1f; x <= xr - .5f; x += 3f) m.Box(V(x, top + 2.1f, z + .17f), V(.08f, 1.9f, .08f), MastDark);
                m.Box(V((xl + xr) * .5f, top + 3.02f, z + .17f), V(xr - xl - 1f, .07f, .07f), MastDark);
                foreach (float h in new[] { 1.7f, 2.3f })
                    K.Face(m, V(xl + 1f, top + h, z + .17f), V(xr - .5f, top + h, z + .17f), V(xr - .5f, top + h + .04f, z + .17f), V(xl + 1f, top + h + .04f, z + .17f), Mast, z == z0 ? Vector3.back : Vector3.forward, false);
            }
            // piers: beside the footpath, between the tracks' far side and Railway Parade
            foreach (float x in new[] { -(K.Edge + 1.2f), -(K.Edge + 17.2f) })
            {
                m.Surface = ComicSurface.Concrete;
                foreach (float z in new[] { z0 + 1f, z1 - 1f }) m.Cylinder(V(x, y + 1.3f, z), .42f, 9.6f, Concrete, 8);
                m.Box(V(x, under - .75f, zc), V(1.1f, .5f, len - .6f), Concrete);
            }
            // abutments: Railway Parade side and the street side, with a mural of big simple shapes
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(xl, y - 2.5f, z0 + .2f), V(xl - 1f, under, z1 - .2f), Concrete);
            K.MinMaxBox(m, V(xr, y - 2.5f, z0 + .2f), V(xr + 1f, under, z1 - .2f), Concrete);
            // the walled approaches beyond the abutments (limestone block retaining walls, block joints)
            K.MinMaxBox(m, V(xl - 1f, y - 2.5f, z0 + .3f), V(xl - 30f, under, z1 - .3f), Limestone);
            K.MinMaxBox(m, V(xr + 1f, y - 2.5f, z0 + .3f), V(xr + 30f, under, z1 - .3f), Limestone);
            m.Surface = ComicSurface.Plain;
            foreach (float zf in new[] { z0 + .3f - .012f, z1 - .3f + .012f })
            {
                Vector3 nf = zf < zc ? Vector3.back : Vector3.forward;
                foreach (float u in new[] { 1.5f, 3f, 4.5f })
                {
                    K.Face(m, V(xl - 1f, y + u, zf), V(xl - 30f, y + u, zf), V(xl - 30f, y + u + .05f, zf), V(xl - 1f, y + u + .05f, zf), LimeDark, nf, false);
                    K.Face(m, V(xr + 1f, y + u, zf), V(xr + 30f, y + u, zf), V(xr + 30f, y + u + .05f, zf), V(xr + 1f, y + u + .05f, zf), LimeDark, nf, false);
                }
            }
            m.Surface = ComicSurface.Concrete;
            Color[] paint = { Hex("E2463A"), Hex("F6C543"), Hex("2A9D9A"), Hex("2F6FB0"), Jac2, Hex("F28C28"), White };
            var r = new System.Random(seed * 91 + 7);
            m.Surface = ComicSurface.Plain;
            float fx = xr - .012f;
            K.Face(m, V(fx, y + .3f, z0 + .5f), V(fx, y + .3f, z1 - .5f), V(fx, y + 4.6f, z1 - .5f), V(fx, y + 4.6f, z0 + .5f), Hex("2C4A6E"), Vector3.left, false);
            for (int i = 0; i < 9; i++)
            {
                float z = Mathf.Lerp(z0 + .9f, z1 - .9f, F(r, 0f, 1f)), u = F(r, .7f, 3.8f), w = F(r, .35f, .9f), h = F(r, .4f, 1.1f);
                K.Face(m, V(fx - .012f, y + u, z - w), V(fx - .012f, y + u - h * .2f, z + w), V(fx - .012f, y + u + h, z + w * .8f), V(fx - .012f, y + u + h * 1.1f, z - w * .7f), Pick(paint, seed + i), Vector3.left, false);
            }
            // the name over the road (white on green), a clearance plate, lamp posts on the deck
            m.Surface = ComicSurface.PaintedMetal;
            float w2 = TextCols(name) * .13f + .7f;
            m.Box(V(0, under + .55f, z0 - .08f), V(w2 + .14f, 1.24f, .08f), White);
            m.Box(V(0, under + .55f, z0 - .12f), V(w2, 1.1f, .06f), Hex("1F6B4A"));
            Text(m, name, V(0, under + .55f, z0 - .16f), Vector3.right, Vector3.up, .13f, White);
            m.Box(V(-5.6f, under + .45f, z0 - .08f), V(1.5f, .7f, .08f), K.R.yellow);
            Text(m, "5.0M", V(-5.6f, under + .45f, z0 - .125f), Vector3.right, Vector3.up, .1f, Ink);
            foreach (float x in new[] { xl + 6f, -1f })
            {
                m.Cylinder(V(x, top + 3.6f, z1 - .6f), .07f, 5f, Mast, 6, .8f);
                m.Beam(V(x, top + 6f, z1 - .6f), V(x, top + 6.3f, zc), .06f, Mast);
                m.Box(V(x, top + 6.25f, zc), V(.3f, .1f, .6f), Cream);
            }
        }

        // ================================================================================== Maylands chunks

        static CqChunkDraft MayEntry()
        {
            // Calm at z = 0, then Whatley Crescent with the railway at street level on the left (the tracks parting for
            // the island platform ahead) and the cream shops of the town centre on the right.
            var d = new CqChunkDraft("Maylands_Entry", MayDrop);
            BrickStreet(d, K.R.asphalt);
            CalmEdge(d, -1, 0f, 16f, 11, 2);
            CalmEdge(d, 1, 0f, 16f, 12, 1);
            Rail rl = FlatRail(8.8f, 13.6f);
            RailCorridor(d, rl, 16f, 40f, 13);
            // big trees hide where the tracks begin
            FigTree(d.Props, V(-(K.Edge + 7f), d.Ground(13.5f), 13.5f), 1.05f, 6);
            FigTree(d.Props, V(-(K.Edge + 16f), d.Ground(15f), 15f), 1.2f, 7);
            RailEnd(d, 16.6f, 1);
            Gantry(d.SlopedFrontage, rl, 16.4f);
            Gantry(d.SlopedFrontage, rl, 34f);
            SideGround(d, 1, 16f, 40f, 140f, PavingRedDark, ComicSurface.Paving);
            var s1 = new CqLot(1, 16f, 28f, K.Edge, 12f, d.Base(22f));
            FederationShop(d.Frontage, s1, Hex("F2DFA6"), White, HeritageGreen, 1, 1, false, "1904", 11);
            d.Wall(s1, 7f);
            var s2 = new CqLot(1, 28f, 40f, K.Edge, 12f, d.Base(34f));
            FederationShop(d.Frontage, s2, Hex("D9DCE2"), White, Charcoal, 1, 2, false, "1909", 12);
            d.Wall(s2, 7f);
            Vector3 lamp = V(K.Half + .6f, d.Ground(24f), 24f);
            SunburstLamp(d.Props, lamp, Charcoal, true);
            K.Thin(d.Collisions, "SunburstLamp", lamp, 7.6f, .35f);
            Apartments(d.Deep, V(K.Edge + 22f, d.Ground(28f) - 1f, 28f), 22f, 14f, 13f, 3);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MayStation()
        {
            // Maylands station: the island platform with the old brick parcels office (1900) under its red iron roof, the
            // steel canopy and the subway stairs, a train at the far platform; the Peninsula Hotel across Whatley Crescent
            // on the right; the Seventh Ave bridge over everything at the end.
            var d = new CqChunkDraft("Maylands_Station", MayDrop);
            BrickStreet(d, K.R.asphalt);
            Rail rl = FlatRail(13.6f, 13.6f);
            RailCorridor(d, rl, 0f, 40f, 21, false);
            IslandPlatform(d, rl, 1f, 31f, 6.1f, 11.9f);
            ComicMesh w = d.SlopedFrontage;
            float top = rl.Level + 1.05f, xc = rl.X(9f);
            SubwayStairs(w, V(xc, top, 2.2f));
            ParcelsOffice(w, V(xc, top, 7.5f), 9.5f);
            TrussCanopy(w, V(xc, top, 18.6f), 10.4f);
            PlatformLamp(w, V(xc + 2.1f, top, 5.6f), true);
            PlatformLamp(w, V(xc - 2.1f, top, 30f), true);
            // platform end ramps
            w.Surface = ComicSurface.Concrete;
            K.Face(w, V(rl.X(6.1f), top, 31f), V(rl.X(11.9f), top, 31f), V(rl.X(11.9f), rl.Level - .2f, 34f), V(rl.X(6.1f), rl.Level - .2f, 34f), ConcreteDark, Vector3.up);
            Gantry(w, rl, 3f);
            Gantry(w, rl, 23f);
            Train(w, rl.X(13.6f), rl.Level, 1f, 2, false);
            // Norfolk pines and figs along Railway Parade
            for (int i = 0; i < 3; i++)
            {
                float z = 6f + i * 11f;
                if (i == 1) FigTree(d.Props, V(-(K.Edge + 30f), d.Ground(z), z), 1.3f, 20 + i);
                else NorfolkPine(d.Props, V(-(K.Edge + 29.5f), d.Ground(z), z), 17f + i, 20 + i);
            }
            DeepSuburb(d, -1, 0f, 30f, 36f, 23);
            // the Peninsula Hotel behind its picket fence
            SideGround(d, 1, 0f, 40f, 140f, Lawn, ComicSurface.Foliage, .03f);
            var hotel = new CqLot(1, 4f, 30f, K.Edge + 5f, 13f, d.Base(17f));
            PeninsulaHotel(d.Frontage, hotel);
            d.Wall(hotel, 12f);
            PicketFence(d.SlopedFrontage, 1, .2f, 31.5f, .22f, 1.1f, Cream, 19.5f);
            d.Road.Surface = ComicSurface.Paving;
            K.Face(d.Road, V(K.Edge, K.Kerb + .05f, 18.7f), V(K.Edge, K.Kerb + .05f, 20.3f), V(hotel.Line - 3.6f, K.Kerb + .05f, 20.3f), V(hotel.Line - 3.6f, K.Kerb + .05f, 18.7f), PavingRed, Vector3.up, false);
            FlowerBush(d.Props, V(K.Edge + 2.2f, d.Ground(7f), 7f), 1.1f, Hex("E85A8C"), 3);
            FlowerBush(d.Props, V(K.Edge + 2.4f, d.Ground(14f), 14f), 1.1f, White, 4);
            FlowerBush(d.Props, V(K.Edge + 2.2f, d.Ground(25f), 25f), 1.1f, Hex("D8323A"), 5);
            CottonPalm(d.Props, V(K.Edge + 2.6f, d.Ground(2.2f), 2.2f), 9f, 3);
            Vector3 lamp = V(K.Half + .6f, d.Ground(12f), 12f);
            SunburstLamp(d.Props, lamp, Charcoal, true);
            K.Thin(d.Collisions, "SunburstLamp", lamp, 7.6f, .35f);
            RoadBridge(d, 33f, 39f, "SEVENTH AVE", 2, 1);
            DeepSuburb(d, 1, 0f, 30f, 22f, 24);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MayEighthAve()
        {
            // Eighth Ave: the red road, the cream Federation shops with their parapets and street awnings, the black
            // sunburst lamps, blue and red bollards, cafe tables; The RISE (the community centre and library) on the right.
            var d = new CqChunkDraft("Maylands_EighthAve", MayDrop);
            BrickStreet(d, K.R.asphalt);
            RedAsphalt(d.Road, d.Length, -K.Half + .26f, K.Half - .26f, Hex("9A5548"));
            SideGround(d, -1, 0f, 40f, 40f, PavingRedDark, ComicSurface.Paving);
            SideGround(d, 1, 0f, 40f, 140f, Lawn, ComicSurface.Foliage, .03f);
            float z = 0f;
            Color[] walls = { Hex("F2DFA6"), Hex("D9DCE2"), Hex("F0E6CF"), Hex("E9D3B8") };
            string[] years = { "1906", "1911", "1923", "1914" };
            for (int i = 0; i < 4; i++)
            {
                float wdt = i == 3 ? 40f - z : new[] { 10f, 9f, 11f }[i];
                var l = new CqLot(-1, z, z + wdt, K.Edge, 12f, d.Base(z + wdt * .5f));
                if (i == 2) DecoShop(d.Frontage, l, Hex("E6E0D0"), Burgundy, Charcoal, 7);
                else FederationShop(d.Frontage, l, walls[i], White, Pick(new[] { Charcoal, HeritageGreen, Burgundy, Navy }, i), i == 1 ? 2 : 1, i % 3, false, years[i], 20 + i);
                d.Wall(l, 8f);
                z += wdt;
            }
            var rise = new CqLot(1, 6f, 30f, K.Edge + 4f, 14f, d.Base(18f));
            TheRise(d.Frontage, rise);
            d.Wall(rise, 10f);
            // the forecourt: paving, a young gum, the painted book-exchange box
            Ground(d.Road, K.Edge, K.Edge + 4f, 6f, 30f, K.Kerb + .05f, PavingRed, ComicSurface.Paving);
            GumTree(d.Props, V(K.Edge + 2.4f, d.Ground(3f), 3f), 8f, 31, false);
            GumTree(d.Props, V(K.Edge + 3.2f, d.Ground(35f), 35f), 9f, 32, false);
            ComicMesh p = d.Props;
            p.Surface = ComicSurface.PaintedMetal;
            Vector3 box = V(K.Edge + 1.8f, d.Ground(33f), 32.2f);
            p.Box(box + V(0, 1.1f, 0), V(1.2f, 2.2f, 3f), Hex("C9483A"));
            Color[] stripes = { Hex("F6C543"), Hex("2A9D9A"), Hex("2F6FB0"), White };
            for (int i = 0; i < 4; i++) p.Box(box + V(-.61f, .5f + i * .42f, 0), V(.03f, .28f, 2.8f), stripes[i]);
            foreach ((int s, float zz) in new[] { (-1, 7f), (1, 21f), (-1, 33f) })
            {
                Vector3 lamp = V(s * (K.Half + .6f), d.Ground(zz), zz);
                SunburstLamp(d.Props, lamp, Charcoal, true);
                K.Thin(d.Collisions, "SunburstLamp", lamp, 7.6f, .35f);
            }
            foreach ((int s, float zz, Color c) in new[] { (-1, 13f, DecoBlue), (-1, 14.5f, Hex("D8323A")), (-1, 16f, DecoBlue), (1, 3f, Hex("D8323A")), (1, 4.5f, DecoBlue) })
            {
                Vector3 b = V(s * (K.Half + .55f), d.Ground(zz), zz);
                Bollard(d.Props, b, c);
                K.Thin(d.Collisions, "Bollard", b, 1.1f, .3f);
            }
            K.CafeUmbrella(d.Props, d.Collisions, V(-(K.Edge - 1.5f), d.Ground(4f), 4f), Mustard);
            K.CafeUmbrella(d.Props, d.Collisions, V(-(K.Edge - 1.5f), d.Ground(24f), 24f), Hex("B8483A"));
            K.PropFromKit(d.Props, d.Collisions, "BikeRack", V(-(K.Half + 1f), d.Ground(19.5f), 19.5f), 90f);
            K.PropFromKit(d.Props, d.Collisions, "Bench", V(K.Half + 1f, d.Ground(12f), 12f), -90f);
            K.PropFromKit(d.Props, d.Collisions, "Bin", V(K.Half + .8f, d.Ground(14.4f), 14.4f), 0f);
            K.PropFromKit(d.Props, d.Collisions, "FlowerPot", V(-(K.Edge - .7f), d.Ground(29f), 29f), 0f);
            StreetNameSign(d.Props, V(K.Edge - .5f, d.Ground(1.2f), 1.2f), "EIGHTH AVE", "", 1);
            Apartments(d.Deep, V(-(K.Edge + 22f), d.Ground(14f) - 1f, 14f), 24f, 14f, 14f, 5);
            DeepSuburb(d, -1, 28f, 40f, 16f, 33);
            DeepSuburb(d, 1, 0f, 40f, 22f, 34);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MayBrickworks()
        {
            // Down the peninsula: the Maylands Brickworks (1927) on the right: the gate and gatehouse, the Hoffman kiln
            // with its battered brick walls and wide iron roof, the 34 m square chimney; across the road the iron pug
            // mill and the timber drying sheds beside the old clay pit, now a lake.
            var d = new CqChunkDraft("Maylands_Brickworks", MayDrop);
            SuburbStreet(d, Lawn);
            SideGround(d, 1, 0f, 40f, 140f, Lawn, ComicSurface.Foliage, .03f);
            SideGround(d, -1, 0f, 40f, 60f, Lawn, ComicSurface.Foliage, .03f);
            // the gate: brick piers, a dark red steel fence, the name over the gate
            ComicMesh w = d.SlopedFrontage;
            Color fence = Hex("7A2E2A");
            BarFence(w, K.Edge + .5f, .2f, 4.6f, K.Kerb, 1.7f, fence, -1, .22f);
            BarFence(w, K.Edge + .5f, 9.4f, 39.8f, K.Kerb, 1.7f, fence, -1, .22f);
            foreach (float z in new[] { 4.9f, 9.1f })
            {
                w.Surface = ComicSurface.Brick;
                w.Box(V(K.Edge + .5f, K.Kerb + 1.1f, z), V(.7f, 2.2f, .7f), Brick);
                w.Surface = ComicSurface.Concrete;
                w.Box(V(K.Edge + .5f, K.Kerb + 2.26f, z), V(.84f, .12f, .84f), LimeCap);
            }
            w.Surface = ComicSurface.PaintedMetal;
            w.Box(V(K.Edge + .5f, K.Kerb + 2.9f, 7f), V(.08f, .5f, 4.2f), fence);
            Text(w, "MAYLANDS", V(K.Edge + .455f, K.Kerb + 3.03f, 7f), Vector3.back, Vector3.up, .07f, Cream);
            Text(w, "BRICKWORKS", V(K.Edge + .455f, K.Kerb + 2.78f, 7f), Vector3.back, Vector3.up, .045f, Cream);
            Ground(d.Road, K.Edge, K.Edge + 12f, 5.3f, 8.7f, K.Kerb + .05f, PavingRed, ComicSurface.Paving);
            var gate = new CqLot(1, 0.6f, 4.4f, K.Edge + 3.5f, 6f, d.Base(2.5f));
            Gatehouse(d.Frontage, gate);
            var kiln = new CqLot(1, 11f, 38f, K.Edge + 5.5f, 12f, d.Base(24.5f));
            HoffmanKiln(d.Frontage, kiln);
            d.Wall(kiln, 8f);
            BrickChimney(d.Landmark, V(K.Edge + 25f, d.Ground(27f) - 1f, 27f), 35f);
            GumTree(d.Props, V(K.Edge + 3.4f, d.Ground(39f), 39f), 11f, 41, false);
            GumTree(d.Props, V(K.Edge + 23f, d.Ground(9f), 9f), 13f, 42, false);
            // left: the pug mill and the drying sheds, the lake behind them
            var mill = new CqLot(-1, 3f, 13f, K.Edge + 6f, 9f, d.Base(8f));
            PugMill(d.Frontage, mill);
            d.Wall(mill, 10f);
            var shed = new CqLot(-1, 16f, 38f, K.Edge + 5f, 5f, d.Base(27f));
            DryingShed(d.Frontage, shed);
            BarFence(w, -(K.Edge + .5f), .2f, 39.8f, K.Kerb, 1.7f, fence, 1, .22f);
            K.Water(d.Road, -(K.Edge + 16f), -(K.Edge + 58f), 0f, 40f, K.Kerb + .12f, River, RiverLight, 43);
            var r = new System.Random(431);
            for (float z = 2f; z < 39f; z += F(r, 2.2f, 3.6f)) Reeds(d.Props, V(-(K.Edge + F(r, 15f, 17f)), d.Ground(z), z), F(r, 1.2f, 1.7f), (int)(z * 3));
            Paperbark(d.Props, V(-(K.Edge + 14f), d.Ground(14.5f), 14.5f), 7f, 44);
            GumTree(d.Props, V(-(K.Edge + 3f), d.Ground(1.4f), 1.4f), 10f, 45, false);
            DeepSuburb(d, 1, 0f, 40f, 34f, 46);
            DeepSuburb(d, -1, 0f, 40f, 60f, 47);
            d.StandardSlots();
            return d;
        }

        /// <summary>The Swan River on the right of the path from z0 to z1: a lawn bank with a line of reeds, a strip of sand,
        /// the wide blue river with ripples, swans on it.</summary>
        static void RiverSide(CqChunkDraft d, float z0, float z1, int seed, bool swans = true)
        {
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Foliage;
            K.SideBox(m, 1, z0, z1, -3f, K.Kerb + .03f, 0f, 7f, Lawn);
            float x0 = K.Edge + 7f, x1 = K.Edge + 12f;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f), xm = (x0 + x1) * .5f;
                m.Surface = ComicSurface.Foliage;
                K.Face(m, V(x0, K.Kerb + .03f, z), V(x0, K.Kerb + .03f, e), V(xm, MayRiver + .25f, e), V(xm, MayRiver + .25f, z), LawnLight, Vector3.up, false);
                m.Surface = ComicSurface.Paving;
                K.Face(m, V(xm, MayRiver + .25f, z), V(xm, MayRiver + .25f, e), V(x1 + 1f, MayRiver - .12f, e), V(x1 + 1f, MayRiver - .12f, z), Hex("E6D9B4"), Vector3.up, false);
            }
            K.Water(m, x1, 70f, z0, z1, MayRiver, River, RiverLight, seed);
            K.Water(m, 70f, 160f, z0, z1, MayRiver, RiverDeep, RiverLight, seed + 1);
            var r = new System.Random(seed * 23 + 1);
            for (float z = z0 + 1f; z < z1 - .5f; z += F(r, 1.8f, 3.2f))
                Reeds(d.SlopedFrontage, V(x1 - F(r, .2f, 1.4f), MayRiver + .05f, z), F(r, 1.1f, 1.8f), (int)(z * 5) + seed);
            if (!swans) return;
            for (int i = 0; i < 3; i++)
            {
                float z = Mathf.Lerp(z0 + 4f, z1 - 4f, (i + F(r, .1f, .9f)) / 3f), x = x1 + F(r, 2.5f, 9f);
                d.SlopedFrontage.Add(SwimmingSwanMesh(), V(x, MayRiver, z), Quaternion.Euler(0, F(r, -60f, 60f) + 180f, 0), Vector3.one * 1.5f);
            }
        }

        /// <summary>The golf course on the left of the path: mown fairway stripes, a line of gums, a hedge at the path.</summary>
        static void GolfSide(CqChunkDraft d, float z0, float z1, int seed)
        {
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Foliage;
            K.SideBox(m, -1, z0, z1, -2.4f, K.Kerb + .03f, 0f, 70f, Lawn);
            for (float z = z0; z < z1 - .01f; z += 8f)
                K.Face(m, V(-(K.Edge + 4f), K.Kerb + .045f, z), V(-(K.Edge + 4f), K.Kerb + .045f, Mathf.Min(z1, z + 4f)), V(-(K.Edge + 60f), K.Kerb + .045f, Mathf.Min(z1, z + 4f)), V(-(K.Edge + 60f), K.Kerb + .045f, z), LawnLight, Vector3.up, false);
            ComicMesh w = d.SlopedFrontage;
            w.Surface = ComicSurface.Wood;
            for (float z = z0 + .5f; z < z1; z += 3f) w.Box(V(-(K.Edge + .6f), K.Kerb + .35f, z), V(.16f, .7f, .16f), Timber);
            for (float z = z0 + .5f; z < z1 - 3f; z += 3f) w.Box(V(-(K.Edge + .6f), K.Kerb + .55f, z + 1.5f), V(.08f, .08f, 3f), TimberDark);
            var r = new System.Random(seed * 29 + 3);
            for (float z = z0 + F(r, 2f, 6f); z < z1 - 2f; z += F(r, 11f, 15f))
                GumTree(d.Props, V(-(K.Edge + F(r, 5f, 9f)), d.Ground(z), z), F(r, 10f, 14f), seed + (int)z, false);
            for (float z = z0 + 4f; z < z1; z += F(r, 7f, 11f))
                FarTree(d.Deep, V(-(K.Edge + F(r, 34f, 62f)), d.Ground(z) - .3f, z), F(r, 9f, 14f), r.Next(2) == 0 ? GumLeaf : GumLeafDark, true);
        }

        static CqChunkDraft MayRiverChunk()
        {
            // The foreshore path: the Swan River wide on the right (black swans, a rowing crew, reeds, sheoaks and a picnic
            // shelter on the bank), the peninsula golf course on the left (a green with its flag, a bunker, a buggy).
            var d = new CqChunkDraft("Maylands_River", MayDrop);
            SharedPath(d);
            BikeSymbol(d.Road, -2f, 30f);
            RiverSide(d, 0f, 40f, 51);
            Sheoak(d.Props, V(K.Edge + 4.6f, d.Ground(5f), 5f), 9.5f, 1);
            Paperbark(d.Props, V(K.Edge + 5.6f, d.Ground(31f), 31f), 7f, 2);
            Sheoak(d.Props, V(K.Edge + 3.8f, d.Ground(37f), 37f), 8.5f, 3);
            PicnicShelter(d.Frontage, V(K.Edge + 3.6f, d.Ground(18f), 18f));
            Bench(d.Props, V(K.Edge + 1.3f, d.Ground(27f), 27f), -90f, Timber);
            d.SlopedFrontage.Add(Make(m => RowingShell(m, Vector3.zero, 0f)), V(36f, MayRiver, 24f), Quaternion.Euler(0, 12f, 0), Vector3.one * 1.2f);
            GolfSide(d, 0f, 40f, 52);
            GolfGreen(d.SlopedFrontage, V(-(K.Edge + 13f), K.Kerb + .05f, 22f));
            GolfBuggy(d.Props, V(-(K.Edge + 5f), d.Ground(11f), 11f), 20f);
            PostSign(d.Props, V(-(K.Edge + 1.6f), d.Ground(3f), 3f), "SWAN RIVER", .07f, Cream, Hex("1F6B4A"), 1.9f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft MayBoardwalk()
        {
            // The path runs out over the samphire flats on a timber boardwalk; one span is missing over the tidal creek: up
            // the timber kicker and over 6 m of water (SPLASH), landing on the path again.
            var d = new CqChunkDraft("Maylands_Boardwalk", MayDrop);
            float j0 = BoardwalkDeckStart, ramp = BoardwalkRampStart, lip = ramp + BoardwalkRise + BoardwalkTop, far = lip + BoardwalkGap;
            SharedPath(d, j0, far);
            ComicMesh m = d.Road;
            // the flats: shallow water under the boardwalk, samphire beds on both sides, the river beyond on the right
            K.Water(m, -70f, K.Edge + 12f, j0, far, MayRiver, River, RiverLight, 61);
            Seawall(m, -70f, K.Edge + 12f, j0, true);
            Seawall(m, -70f, K.Edge + 12f, far, false);
            RiverSide(d, 0f, j0, 62, false);
            RiverSide(d, far, 40f, 63);
            K.Water(m, K.Edge + 12f, 70f, j0, far, MayRiver, River, RiverLight, 64);
            K.Water(m, 70f, 160f, j0, far, MayRiver, RiverDeep, RiverLight, 65);
            GolfSide(d, 0f, j0, 66);
            GolfSide(d, far, 40f, 67);
            BoardwalkDeck(d, j0, ramp);
            var spec = new CqRampSpec { Name = "BoardwalkKicker", X = 0f, Z = ramp, Width = 2f * K.Edge, Rise = BoardwalkRise, Height = BoardwalkHeight, Top = BoardwalkTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            d.Gaps.Add(new CqGapSpec { Name = "TidalCreek", Z = lip, Length = BoardwalkGap, Width = 0f, Word = "SPLASH", Water = true });
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, DeckLight, Deck, false, MayRiver - .3f);
            for (int s = -1; s <= 1; s += 2)
            {
                deck.Surface = ComicSurface.Wood;
                deck.Beam(V(s * (K.Edge + .12f), 1.05f, ramp), V(s * (K.Edge + .12f), BoardwalkHeight + 1.05f, ramp + BoardwalkRise), .12f, Timber);
                deck.Beam(V(s * (K.Edge + .12f), BoardwalkHeight + 1.05f, ramp + BoardwalkRise), V(s * (K.Edge + .12f), BoardwalkHeight + 1.05f, lip - .2f), .12f, Timber);
                for (float z = ramp; z <= lip; z += 3f)
                {
                    float h = BoardwalkHeight * Mathf.Clamp01((z - ramp) / BoardwalkRise);
                    deck.Box(V(s * (K.Edge + .12f), (h + MayRiver) * .5f + .5f, z), V(.22f, h - MayRiver + 1f, .22f), Deck);
                }
            }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            d.KeepOut.Add(KeepOutBox(d, -1, ramp, far)); d.KeepOut.Add(KeepOutBox(d, 1, ramp, far));
            // samphire beds and reeds in the shallows, paperbarks, an egret, a warning sign
            var r = new System.Random(68);
            for (int i = 0; i < 16; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * F(r, K.Edge + 1.5f, K.Edge + 11f), z = F(r, j0 + 1f, far - 1f);
                m.Surface = ComicSurface.Foliage;
                float w = F(r, 1.2f, 2.6f);
                K.Face(m, V(x - w, MayRiver + .04f, z - w * .6f), V(x - w * .7f, MayRiver + .04f, z + w * .7f), V(x + w, MayRiver + .04f, z + w * .5f), V(x + w * .8f, MayRiver + .04f, z - w * .8f), i % 3 == 0 ? Reed : Samphire, Vector3.up, false);
                if (i % 2 == 0) Reeds(d.SlopedFrontage, V(x, MayRiver, z), F(r, 1f, 1.6f), i);
            }
            d.SlopedFrontage.Add(EgretMesh(), V(-(K.Edge + 4f), MayRiver, 14f), Quaternion.Euler(0, 110f, 0), Vector3.one);
            d.SlopedFrontage.Add(EgretMesh(), V(K.Edge + 6f, MayRiver, 21f), Quaternion.Euler(0, -70f, 0), Vector3.one * .9f);
            Paperbark(d.Props, V(-(K.Edge + 5f), d.Ground(1.4f), 1.4f), 7f, 5);
            Paperbark(d.Props, V(-(K.Edge + 4f), d.Ground(29f), 29f), 8f, 6);
            Paperbark(d.Props, V(K.Edge + 4.5f, d.Ground(33f), 33f), 7f, 7);
            PostSign(d.Props, V(K.Edge + 1.4f, d.Ground(1.2f), 1.2f), "BOARDWALK", .06f, K.R.yellow, Ink, 1.9f);
            d.GiftSlots.Add(new Vector2(0f, 31f));
            d.Slot(-2.6f, 36f); d.Slot(2.6f, 36f);
            return d;
        }

        static CqChunkDraft MayExit()
        {
            // The last of the river and the golf course, then calm towards z = 40: hedges, low walls, gums.
            var d = new CqChunkDraft("Maylands_Exit", MayDrop);
            SharedPath(d);
            RiverSide(d, 0f, 26f, 71);
            GolfSide(d, 0f, 26f, 72);
            Sheoak(d.Props, V(K.Edge + 4.2f, d.Ground(8f), 8f), 9f, 8);
            Paperbark(d.Props, V(K.Edge + 5f, d.Ground(20f), 20f), 7.5f, 9);
            // a grassy point closes the river towards the calm end
            d.Road.Surface = ComicSurface.Foliage;
            K.MinMaxBox(d.Road, V(K.Edge + 60f, -3f, 26f), V(170f, K.Kerb + .05f, 40f), Lawn);
            d.SlopedFrontage.Surface = ComicSurface.Foliage;
            for (float x = K.Edge + 9f; x < 60f; x += 7f) Blob(d.SlopedFrontage, V(x, .7f, 26.4f), V(8f, 3.2f, 3f), Reed, 6, 3, x);
            CalmEdge(d, -1, 26f, 40f, 13, 2);
            CalmEdge(d, 1, 26f, 40f, 14, 1);
            d.StandardSlots();
            return d;
        }

        // ================================================================================== boardwalk parts

        // A limestone wall across the flats where the boardwalk leaves and regains the bank.
        static void Seawall(ComicMesh m, float x0, float x1, float z, bool facingForward)
        {
            m.Surface = ComicSurface.Concrete;
            float t = facingForward ? -.4f : .4f;
            K.MinMaxBox(m, V(x0, MayRiver - 1f, z), V(x1, -.03f, z + t), Limestone);
            K.MinMaxBox(m, V(x0, -.03f, z), V(-K.Half, K.Kerb + .02f, z + t), LimeCap);
            K.MinMaxBox(m, V(K.Half, -.03f, z), V(x1, K.Kerb + .02f, z + t), LimeCap);
        }

        // The boardwalk: a timber deck with the street's cross-section (path at 0, walkways at the kerb height), piles, rails.
        static void BoardwalkDeck(CqChunkDraft d, float z0, float z1)
        {
            ComicMesh m = d.Road;
            Color board = Mix(Deck, Ink, .3f), walk = Mix(DeckLight, Cream, .15f);
            for (float z = z0; z < z1 - .01f; z += 1f)
            {
                float e = Mathf.Min(z1, z + 1f);
                m.Surface = ComicSurface.Wood;
                K.Face(m, V(-K.Half, 0f, z), V(-K.Half, 0f, e), V(K.Half, 0f, e), V(K.Half, 0f, z), DeckLight, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    K.Face(m, V(s * K.Half, K.Kerb, z), V(s * K.Half, K.Kerb, e), V(s * K.Edge, K.Kerb, e), V(s * K.Edge, K.Kerb, z), walk, Vector3.up, false);
                    K.Face(m, V(s * K.Half, -.05f, z), V(s * K.Half, -.05f, e), V(s * K.Half, K.Kerb, e), V(s * K.Half, K.Kerb, z), Deck, new Vector3(-s, 0, 0), false);
                    K.Face(m, V(s * K.Edge, -.6f, z), V(s * K.Edge, -.6f, e), V(s * K.Edge, K.Kerb, e), V(s * K.Edge, K.Kerb, z), Deck, new Vector3(s, 0, 0), false);
                }
                m.Surface = ComicSurface.Plain;
                K.Face(m, V(-K.Edge, K.Kerb + .012f, z + .45f), V(-K.Edge, K.Kerb + .012f, z + .5f), V(-K.Half, K.Kerb + .012f, z + .5f), V(-K.Half, K.Kerb + .012f, z + .45f), board, Vector3.up, false);
                K.Face(m, V(K.Half, K.Kerb + .012f, z + .45f), V(K.Half, K.Kerb + .012f, z + .5f), V(K.Edge, K.Kerb + .012f, z + .5f), V(K.Edge, K.Kerb + .012f, z + .45f), board, Vector3.up, false);
                K.Face(m, V(-K.Half, .012f, z + .45f), V(-K.Half, .012f, z + .5f), V(K.Half, .012f, z + .5f), V(K.Half, .012f, z + .45f), board, Vector3.up, false);
            }
            ComicMesh p = d.SlopedFrontage;
            p.Surface = ComicSurface.Wood;
            for (float z = z0 + 1f; z < z1; z += 3f)
                for (float x = -K.Edge + .3f; x <= K.Edge; x += (2f * K.Edge - .6f) / 3f)
                    p.Cylinder(V(x, (MayRiver - .8f - .6f) * .5f, z), .2f, -.6f - (MayRiver - .8f), Deck, 8);
            for (int s = -1; s <= 1; s += 2)
            {
                p.Box(V(s * (K.Edge + .12f), K.Kerb + .95f, (z0 + z1) * .5f), V(.12f, .12f, z1 - z0), Timber);
                p.Box(V(s * (K.Edge + .12f), K.Kerb + .5f, (z0 + z1) * .5f), V(.08f, .08f, z1 - z0), Timber);
                for (float z = z0 + .5f; z < z1; z += 2.2f) p.Box(V(s * (K.Edge + .12f), K.Kerb + .5f, z), V(.16f, 1.1f, .16f), Deck);
            }
        }

        // ================================================================================== Maylands buildings

        /// <summary>Modern apartments behind the old shop fronts (background, no outline): dark render, white slab edges,
        /// long window bands, a flat roof with an overhang.</summary>
        static void Apartments(ComicMesh m, Vector3 p, float len, float wide, float h, int seed)
        {
            Color wall = seed % 2 == 0 ? Hex("5E5450") : Hex("D8D4CC");
            m.Surface = ComicSurface.Plaster;
            m.Box(p + V(0, h * .5f, 0), V(wide, h, len), wall);
            int floors = Mathf.RoundToInt(h / 3.2f);
            for (int f = 1; f <= floors; f++)
            {
                float y = f * h / floors;
                m.Surface = ComicSurface.Concrete;
                m.Box(p + V(0, y, 0), V(wide + 1.4f, .22f, len + 1.2f), White);
                m.Surface = ComicSurface.Glass;
                m.Box(p + V(0, y - h / floors * .5f, 0), V(wide + .08f, h / floors * .45f, len * .8f), GlassDark);
            }
        }

        /// <summary>
        /// The Peninsula Hotel (1906), Railway Parade, Maylands: two storeys of red brick with cream render bands and
        /// quoins; a central rendered bay; verandahs on both floors on green iron posts with cream lace brackets and
        /// balustrade; the corner tower with its rendered belvedere, pediments and zinc dome with a weathervane; a
        /// terracotta tile roof with ridge cresting and tall chimneys. Front towards the road.
        /// </summary>
        static void PeninsulaHotel(ComicMesh m, CqLot l)
        {
            float W = l.W, D = l.Depth, floor = .5f, f1 = 4.4f, eave = 8.2f, tw = 5f, vd = 2.6f;
            Color wall = Brick, cream = Hex("F0E0B4"), iron = Hex("2F6B50"), roof = Tile;
            Vector3 toRoad = new Vector3(-l.S, 0, 0);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, floor, .05f, -D, Limestone);
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, tw, W, floor, eave, 0f, -D, wall);
            m.Surface = ComicSurface.Plaster;
            foreach (float u in new[] { floor + .1f, f1 - .3f, f1 + .9f, eave - .5f })
                K.LBox(m, l, tw, W + .03f, u, u + .22f, .04f, -D - .03f, cream);
            // the central bay
            float b0 = W * .5f - 1.2f, b1 = W * .5f + 3.4f, bc = (b0 + b1) * .5f;
            K.LBox(m, l, b0, b1, floor, eave + .6f, 1.1f, 0f, cream);
            m.Surface = ComicSurface.Brick;
            foreach (float u in new[] { floor + .9f, f1 + 1f })
            {
                K.LBox(m, l, b0 + .5f, bc - .8f, u, u + 2.3f, 1.13f, 1.1f, wall);
                K.LBox(m, l, bc + .8f, b1 - .5f, u, u + 2.3f, 1.13f, 1.1f, wall);
                Sash(m, l, bc, 1.2f, u + .2f, u + 2.1f, 1.1f, HeritageGreen, cream, true);
            }
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, b0 - .15f, b1 + .15f, eave + .6f, eave + .9f, 1.3f, -.2f, cream);
            m.Surface = ComicSurface.Brick;
            HipRoof(m, l, b0 - .3f, b1 + .3f, eave + .9f, eave + 2.9f, 1.45f, -3.5f, roof);
            // the main roof with ridge cresting and chimneys
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, tw - .4f, W + .5f, eave - .1f, eave + .08f, .5f, -D - .5f, cream);
            m.Surface = ComicSurface.Brick;
            HipRoof(m, l, tw - .5f, W + .55f, eave + .08f, eave + 3.4f, .55f, -D - .55f, roof);
            for (float a = tw + D * .5f + .6f; a < W - D * .5f; a += .8f) K.LBox(m, l, a, a + .4f, eave + 3.4f, eave + 3.62f, -D * .5f + .06f, -D * .5f - .06f, roof);
            foreach ((float a, float o) in new[] { (W * .36f, -D * .28f), (W * .78f, -D * .3f), (W * .6f, -D * .74f) })
            {
                m.Surface = ComicSurface.Brick;
                K.LBox(m, l, a - .42f, a + .42f, eave + 1f, eave + 5.2f, o + .36f, o - .36f, wall);
                m.Surface = ComicSurface.Plaster;
                K.LBox(m, l, a - .5f, a + .5f, eave + 4.6f, eave + 4.9f, o + .44f, o - .44f, cream);
                K.LBox(m, l, a - .46f, a + .46f, eave + 5.2f, eave + 5.32f, o + .4f, o - .4f, cream);
                m.Surface = ComicSurface.Brick;
                foreach (float dx in new[] { -.2f, .2f }) m.Cylinder(l.P(a + dx, eave + 5.5f, o), .1f, .36f, roof, 5, .8f);
            }
            // the verandahs, both floors, either side of the bay
            foreach ((float a0, float a1) in new[] { (tw, b0), (b1, W) })
            {
                foreach (float fl in new[] { floor, f1 })
                {
                    bool upper = fl > floor + .1f;
                    float topU = fl + (upper ? 3.1f : 3.6f);
                    m.Surface = ComicSurface.Wood;
                    K.LBox(m, l, a0, a1, fl - .22f, fl, vd, 0f, Mix(Timber, cream, .3f));
                    m.Surface = ComicSurface.PaintedMetal;
                    int n = Mathf.Max(2, Mathf.RoundToInt((a1 - a0) / 2.6f));
                    for (int i = 0; i <= n; i++)
                    {
                        float a = Mathf.Lerp(a0 + .1f, a1 - .1f, (float)i / n);
                        K.LBox(m, l, a - .07f, a + .07f, fl, topU, vd - .05f, vd - .19f, iron);
                        foreach (int sg in new[] { -1, 1 })
                        {
                            if (i == 0 && sg < 0 || i == n && sg > 0) continue;
                            K.TriFacing(m, l.P(a + sg * .07f, topU - .05f, vd - .12f), l.P(a + sg * .7f, topU, vd - .12f), l.P(a + sg * .07f, topU - .75f, vd - .12f), cream, toRoad);
                        }
                    }
                    K.LBox(m, l, a0, a1, topU, topU + .2f, vd + .02f, vd - .2f, cream);
                    K.LBox(m, l, a0 + .1f, a1 - .1f, topU - .32f, topU - .24f, vd - .06f, vd - .14f, cream);
                    if (upper)
                    {
                        // lace balustrade: rails and a cream panel with diamond openings
                        K.LBox(m, l, a0 + .1f, a1 - .1f, fl + .95f, fl + 1.05f, vd - .04f, vd - .16f, cream);
                        K.LQuad(m, l, a0 + .1f, a1 - .1f, fl + .1f, fl + .95f, vd - .1f, cream);
                        m.Surface = ComicSurface.Plain;
                        for (float a = a0 + .5f; a < a1 - .4f; a += .7f)
                        {
                            Vector3 c = l.P(a, fl + .52f, vd - .088f);
                            K.Face(m, c + V(0, -.26f, 0), c + V(0, 0, .2f), c + V(0, .26f, 0), c + V(0, 0, -.2f), Mix(wall, Ink, .35f), toRoad, false);
                        }
                        m.Surface = ComicSurface.PaintedMetal;
                    }
                    // doors and windows behind the verandah
                    int bays = Mathf.Max(1, Mathf.RoundToInt((a1 - a0) / 3f));
                    for (int i = 0; i < bays; i++)
                    {
                        float a = Mathf.Lerp(a0, a1, (i + .5f) / bays);
                        bool doorBay = !upper && i == bays / 2;
                        if (doorBay)
                        {
                            m.Surface = ComicSurface.PaintedMetal;
                            K.LQuad(m, l, a - .75f, a + .75f, fl, fl + 3f, .012f, cream);
                            m.Surface = ComicSurface.Wood;
                            K.LQuad(m, l, a - .55f, a + .55f, fl + .02f, fl + 2.3f, .024f, HeritageGreen);
                            m.Surface = ComicSurface.Glass;
                            K.LQuad(m, l, a - .55f, a + .55f, fl + 2.4f, fl + 2.9f, .024f, Glass);
                        }
                        else Sash(m, l, a, 1.15f, fl + (upper ? .7f : .85f), fl + (upper ? 2.6f : 2.9f), 0f, HeritageGreen, cream, false);
                    }
                }
                // verandah roof
                m.Surface = ComicSurface.Metal;
                float vr = f1 + 3.3f;
                K.Face(m, l.P(a0 - .1f, vr + .65f, 0f), l.P(a1 + .1f, vr + .65f, 0f), l.P(a1 + .1f, vr, vd + .35f), l.P(a0 - .1f, vr, vd + .35f), Hex("8C3B33"), toRoad + Vector3.up * 2f);
                m.Surface = ComicSurface.Concrete;
                K.LBox(m, l, a0, a1, -2.6f, floor - .22f, vd - .1f, 0f, Limestone);
            }
            // the name on the ground-floor frieze
            m.Surface = ComicSurface.PaintedMetal;
            float sa = (b1 + W) * .5f;
            K.LBox(m, l, sa - 3.2f, sa + 3.2f, floor + 2.75f, floor + 3.3f, vd + .06f, vd + .02f, iron);
            LText(m, l, "PENINSULA HOTEL", sa, floor + 3.03f, vd + .06f, .085f, cream);
            // the corner tower: brick with quoins, the rendered belvedere with pediments, the zinc dome and weathervane
            float to = .7f;
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, 0f, tw, floor, eave + .4f, to, -tw + to, wall);
            m.Surface = ComicSurface.Plaster;
            foreach (float u in new[] { floor + .1f, f1 - .3f, f1 + .9f, eave - .1f })
                K.LBox(m, l, -.04f, tw + .04f, u, u + .24f, to + .04f, -tw + to - .04f, cream);
            for (float u = floor + .6f; u < eave - .5f; u += .95f)
            {
                K.LBox(m, l, -.03f, .42f, u, u + .48f, to + .03f, to - .45f, cream);
                K.LBox(m, l, tw - .42f, tw + .03f, u, u + .48f, to + .03f, to - .45f, cream);
            }
            Sash(m, l, tw * .5f, 1.2f, floor + .9f, floor + 2.9f, to, HeritageGreen, cream, true);
            Sash(m, l, tw * .5f, 1.2f, f1 + .8f, f1 + 2.7f, to, HeritageGreen, cream, true);
            EndWindow(m, l, 0f, to - 1.9f, to - 3.1f, floor + .9f, floor + 2.9f, HeritageGreen);
            EndWindow(m, l, 0f, to - 1.9f, to - 3.1f, f1 + .8f, f1 + 2.7f, HeritageGreen);
            float by = eave + .4f;
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, -.2f, tw + .2f, by, by + .35f, to + .2f, -tw + to - .2f, cream);
            K.LBox(m, l, .15f, tw - .15f, by + .35f, by + 2.9f, to - .15f, -tw + to + .15f, cream);
            K.LBox(m, l, -.3f, tw + .3f, by + 2.9f, by + 3.3f, to + .3f, -tw + to - .3f, cream);
            // pediments on the road face and the face towards the rider, small round-headed openings below them
            K.LGable(m, l, .7f, tw - .7f, by + 1.85f, by + 2.75f, to + .12f, to - .15f, cream);
            K.LQuad(m, l, tw * .5f - .5f, tw * .5f + .5f, by + .6f, by + 1.75f, to - .138f, Mix(wall, Ink, .3f));
            K.Face(m, l.P(-.012f + .15f, by + .6f, to - 1.9f), l.P(-.012f + .15f, by + .6f, to - 3.1f), l.P(-.012f + .15f, by + 1.75f, to - 3.1f), l.P(-.012f + .15f, by + 1.75f, to - 1.9f), Mix(wall, Ink, .3f), Vector3.back);
            Vector3 dc = l.P(tw * .5f, by + 3.3f, to - tw * .5f);
            m.Surface = ComicSurface.Metal;
            // a bell-shaped dome: steep at the foot, swelling, then drawn in to the lantern
            float[] dr = { 2.75f, 2.7f, 2.35f, 1.7f, .95f, .55f }, dy = { 0f, .7f, 1.6f, 2.5f, 3.2f, 3.6f };
            for (int i = 0; i + 1 < dr.Length; i++)
                m.Cylinder(dc + V(0, (dy[i] + dy[i + 1]) * .5f, 0), dr[i], dy[i + 1] - dy[i], i % 2 == 0 ? Zinc : Mix(Zinc, White, .14f), 8, dr[i + 1] / dr[i], Quaternion.Euler(0, 22.5f, 0));
            m.Surface = ComicSurface.Plaster;
            m.Box(dc + V(0, 3.9f, 0), V(.9f, .6f, .9f), cream);
            m.Box(dc + V(0, 4.26f, 0), V(1.2f, .12f, 1.2f), cream);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(dc + V(0, 4.6f, 0), .6f, .6f, Zinc, 4, .1f, Quaternion.Euler(0, 45f, 0));
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(dc + V(0, 5.5f, 0), .05f, 1.4f, Ink, 5);
            m.Box(dc + V(0, 5.7f, 0), V(.7f, .05f, .05f), Ink);
            m.Box(dc + V(0, 5.7f, 0), V(.05f, .05f, .7f), Ink);
            m.Box(dc + V(.22f, 6.05f, 0), V(.5f, .22f, .04f), Ink);
        }

        /// <summary>The Maylands parcels office (1900) on the island platform: a small brick station building with render
        /// bands, under a gabled red iron roof whose wide eaves shelter the platform on timber brackets; white bargeboards
        /// and boarded gables, sash windows, green doors, a chimney, the station name. p = platform surface at the
        /// building's near end on the platform's centre line.</summary>
        static void ParcelsOffice(ComicMesh m, Vector3 p, float len)
        {
            const float hw = 1.9f, wall = 3.3f, eaveX = 3.7f, ridge = 5.1f, eaveY = 3.25f;
            Color green = HeritageGreen;
            m.Surface = ComicSurface.Brick;
            m.Box(p + V(0, wall * .5f, len * .5f), V(hw * 2f, wall, len), Brick);
            m.Surface = ComicSurface.Plaster;
            m.Box(p + V(0, .35f, len * .5f), V(hw * 2f + .08f, .7f, len + .08f), Hex("D9CBB0"));
            m.Box(p + V(0, 2.55f, len * .5f), V(hw * 2f + .06f, .16f, len + .06f), Cream);
            // gable ends: boarded, with a louvre
            foreach (int e in new[] { 0, 1 })
            {
                float z = e == 0 ? 0f : len;
                Vector3 n = e == 0 ? Vector3.back : Vector3.forward;
                m.Surface = ComicSurface.Wood;
                K.TriFacing(m, p + V(-hw, wall, z), p + V(hw, wall, z), p + V(0, ridge - .25f, z), Cream, n);
                m.Surface = ComicSurface.PaintedMetal;
                K.Face(m, p + V(-.3f, wall + .4f, z + (e == 0 ? -.012f : .012f)), p + V(.3f, wall + .4f, z + (e == 0 ? -.012f : .012f)), p + V(.3f, wall + 1f, z + (e == 0 ? -.012f : .012f)), p + V(-.3f, wall + 1f, z + (e == 0 ? -.012f : .012f)), green, n);
            }
            // the roof: two planes out to the wide eaves, soffits, white bargeboards, brackets
            m.Surface = ComicSurface.Metal;
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 r0 = p + V(0, ridge, -.5f), r1 = p + V(0, ridge, len + .5f), e0 = p + V(s * eaveX, eaveY, -.5f), e1 = p + V(s * eaveX, eaveY, len + .5f);
                K.Face(m, r0, r1, e1, e0, RoofRed, new Vector3(s, 2, 0));
                K.Face(m, r0 + V(0, -.1f, 0), r1 + V(0, -.1f, 0), e1 + V(0, -.1f, 0), e0 + V(0, -.1f, 0), Hex("E6DFCF"), new Vector3(-s, -2, 0));
                m.Surface = ComicSurface.PaintedMetal;
                m.Beam(r0 + V(0, -.02f, -.02f), e0 + V(0, -.02f, -.02f), .16f, White);
                m.Beam(r1 + V(0, -.02f, .02f), e1 + V(0, -.02f, .02f), .16f, White);
                m.Box(p + V(s * eaveX, eaveY - .04f, len * .5f), V(.14f, .16f, len + 1.1f), White);
                for (float z = .5f; z <= len - .4f; z += (len - 1f) / 3f)
                    m.Beam(p + V(s * hw, 2.2f, z), p + V(s * (eaveX - .5f), eaveY - .12f, z), .1f, White);
                m.Surface = ComicSurface.Metal;
            }
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, ridge + .04f, len * .5f), V(.3f, .1f, len + 1f), Hex("7E2F26"));
            m.Surface = ComicSurface.Brick;
            m.Box(p + V(.5f, ridge + .5f, len * .7f), V(.6f, 1.9f, .6f), Brick);
            m.Surface = ComicSurface.Plaster;
            m.Box(p + V(.5f, ridge + 1.5f, len * .7f), V(.74f, .16f, .74f), Cream);
            // doors and windows on both long sides, the name board on the side facing the street (+X)
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 n = new Vector3(s, 0, 0);
                float x = s * (hw + .012f);
                float[] zs = { len * .16f, len * .4f, len * .62f, len * .86f };
                for (int i = 0; i < zs.Length; i++)
                {
                    bool door = i == 1 || i == 3;
                    float z = zs[i], y0 = door ? 0f : 1f, y1 = door ? 2.3f : 2.35f, hwid = door ? .5f : .45f;
                    m.Surface = ComicSurface.PaintedMetal;
                    K.Face(m, p + V(x, y0, z - hwid - .08f), p + V(x, y0, z + hwid + .08f), p + V(x, y1 + .08f, z + hwid + .08f), p + V(x, y1 + .08f, z - hwid - .08f), White, n);
                    m.Surface = door ? ComicSurface.Wood : ComicSurface.Glass;
                    K.Face(m, p + V(x + s * .012f, y0 + .04f, z - hwid), p + V(x + s * .012f, y0 + .04f, z + hwid), p + V(x + s * .012f, y1, z + hwid), p + V(x + s * .012f, y1, z - hwid), door ? green : Glass, n);
                    if (door) continue;
                    m.Surface = ComicSurface.PaintedMetal;
                    K.Face(m, p + V(x + s * .024f, 1.64f, z - hwid), p + V(x + s * .024f, 1.64f, z + hwid), p + V(x + s * .024f, 1.7f, z + hwid), p + V(x + s * .024f, 1.7f, z - hwid), White, n, false);
                    m.Surface = ComicSurface.Concrete;
                    m.Box(p + V(s * (hw + .07f), .95f, z), V(.14f, .1f, hwid * 2f + .3f), Cream);
                }
            }
            float bw = TextCols("MAYLANDS") * .085f + .4f;
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(hw + .05f, 2.95f, len * .5f), V(.06f, .62f, bw + .12f), White);
            m.Box(p + V(hw + .08f, 2.95f, len * .5f), V(.04f, .52f, bw), Hex("B32F45"));
            Text(m, "MAYLANDS", p + V(hw + .105f, 2.95f, len * .5f), Vector3.forward, Vector3.up, .085f, White);
            Bench(m, p + V(hw + .55f, 0, len * .4f), -90f, Hex("7FA0B0"));
        }

        /// <summary>The platform canopy of the 2001 upgrade: pairs of blue-grey steel columns, white trusses, a low-pitched
        /// corrugated roof with skylight strips. p = platform surface at the near end on the centre line.</summary>
        static void TrussCanopy(ComicMesh m, Vector3 p, float len)
        {
            Color col = Hex("6F8A9C");
            m.Surface = ComicSurface.PaintedMetal;
            int bays = Mathf.Max(2, Mathf.RoundToInt(len / 4.5f));
            for (int i = 0; i <= bays; i++)
            {
                float z = Mathf.Lerp(.4f, len - .4f, (float)i / bays);
                foreach (int s in new[] { -1, 1 })
                {
                    m.Cylinder(p + V(s * 1.5f, 1.75f, z), .13f, 3.5f, col, 8);
                    m.Beam(p + V(s * 1.5f, 3.3f, z), p + V(s * 2.85f, 3.62f, z), .1f, White);
                    m.Beam(p + V(s * 1.5f, 3.3f, z), p + V(0, 4.1f, z), .1f, White);
                }
                m.Box(p + V(0, 3.5f, z), V(3.2f, .12f, .12f), White);
            }
            foreach (int s in new[] { -1, 1 })
            {
                m.Surface = ComicSurface.Metal;
                Vector3 r0 = p + V(0, 4.25f, -.3f), r1 = p + V(0, 4.25f, len + .3f), e0 = p + V(s * 3.1f, 3.7f, -.3f), e1 = p + V(s * 3.1f, 3.7f, len + .3f);
                K.Face(m, r0, r1, e1, e0, Galv, new Vector3(s, 3, 0));
                K.Face(m, r0 + V(0, -.08f, 0), r1 + V(0, -.08f, 0), e1 + V(0, -.08f, 0), e0 + V(0, -.08f, 0), Hex("E4E2D8"), new Vector3(-s, -3, 0));
                m.Surface = ComicSurface.Glass;
                for (float z = 1.2f; z < len - 2f; z += 4.4f)
                    K.Face(m, p + V(s * .9f, 4.11f, z), p + V(s * .9f, 4.11f, z + 1.8f), p + V(s * 2.2f, 3.88f, z + 1.8f), p + V(s * 2.2f, 3.88f, z), GlassPale, new Vector3(s, 3, 0), false);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(p + V(s * 3.1f, 3.66f, len * .5f), V(.12f, .14f, len + .7f), col);
            }
            // platform number boards at the near end
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 c = p + V(s * .62f, 2.5f, .15f);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(c, V(1.1f, 1.2f, .1f), Charcoal);
                m.Surface = ComicSurface.Plain;
                K.Face(m, c + V(-.5f, -.2f, -.06f), c + V(.5f, -.2f, -.06f), c + V(.5f, .52f, -.06f), c + V(-.5f, .52f, -.06f), Hex("B32F45"), Vector3.back, false);
                K.Face(m, c + V(-.5f, -.52f, -.06f), c + V(.5f, -.52f, -.06f), c + V(.5f, -.2f, -.06f), c + V(-.5f, -.2f, -.06f), TrainGreen, Vector3.back, false);
                Text(m, s < 0 ? "2" : "1", c + V(0, .16f, -.075f), Vector3.right, Vector3.up, .1f, White);
            }
            Bench(m, p + V(-.8f, 0, len * .45f), 90f, Hex("7FA0B0"));
            Bench(m, p + V(.8f, 0, len * .75f), -90f, Hex("7FA0B0"));
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(1.7f, .5f, len * .25f), .26f, 1f, Hex("C9CED3"), 8);
        }

        /// <summary>The stairs down to the pedestrian subway under the tracks: a dark well in the platform, mesh panels
        /// round it, yellow handrails going down.</summary>
        static void SubwayStairs(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Plain;
            K.Face(m, p + V(-1.1f, .02f, -1f), p + V(-1.1f, .02f, 1.8f), p + V(1.1f, .02f, 1.8f), p + V(1.1f, .02f, -1f), Hex("20262E"), Vector3.up, false);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (int s in new[] { -1, 1 })
            {
                for (float z = -1f; z <= 1.81f; z += 1.4f) m.Box(p + V(s * 1.15f, .6f, z), V(.07f, 1.2f, .07f), Mast);
                m.Box(p + V(s * 1.15f, 1.18f, .4f), V(.06f, .06f, 2.8f), Mast);
                K.Face(m, p + V(s * 1.15f, .15f, -1f), p + V(s * 1.15f, .15f, 1.8f), p + V(s * 1.15f, 1.1f, 1.8f), p + V(s * 1.15f, 1.1f, -1f), Hex("8E979E"), new Vector3(s, 0, 0));
                K.Face(m, p + V(s * 1.15f, .15f, -1f), p + V(s * 1.15f, .15f, 1.8f), p + V(s * 1.15f, 1.1f, 1.8f), p + V(s * 1.15f, 1.1f, -1f), Hex("8E979E"), new Vector3(-s, 0, 0));
                m.Beam(p + V(s * .8f, 1f, 1.8f), p + V(s * .8f, .1f, -.6f), .07f, K.R.yellow);
            }
            m.Box(p + V(0, .6f, -1f), V(2.3f, 1.2f, .07f), Hex("8E979E"));
        }

        /// <summary>
        /// The RISE, Eighth Ave (Maylands' community centre and library): two glazed storeys behind white columns, a
        /// balcony with a glass balustrade, a band of coloured panels along the parapet, a barrel-vaulted roof over the
        /// entrance, bright feature walls (yellow, red), and the brick arcade tower with its cream top and pyramid roof.
        /// </summary>
        static void TheRise(ComicMesh m, CqLot l)
        {
            float W = l.W, D = l.Depth, f1 = 3.8f, top = 7.6f, tw = 5f, a1 = W - tw;
            Vector3 toRoad = new Vector3(-l.S, 0, 0);
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, 0f, .02f, -D, Ink);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, 0, a1, 0f, top, -1.6f, -D, Hex("D9D4C7"));
            // feature walls at the near end
            K.LBox(m, l, 0f, 2.4f, 0f, f1, -1.2f, -1.7f, Hex("F2C230"));
            K.LBox(m, l, 0f, 1.2f, f1, top, -1.2f, -1.7f, Hex("C9483A"));
            // glazing on both floors
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, 2.4f, a1 - .2f, .1f, f1 - .5f, -1.58f, GlassDark);
            K.LQuad(m, l, 1.2f, a1 - .2f, f1 + .2f, top - .9f, -1.58f, Glass);
            m.Surface = ComicSurface.PaintedMetal;
            for (float a = 2.4f; a <= a1 - .1f; a += 1.5f)
            {
                K.LBox(m, l, a - .04f, a + .04f, .1f, f1 - .5f, -1.52f, -1.58f, Ink);
                K.LBox(m, l, a - .04f, a + .04f, f1 + .2f, top - .9f, -1.52f, -1.58f, Ink);
            }
            K.LBox(m, l, 2.4f, a1 - .2f, 2.3f, 2.38f, -1.52f, -1.58f, Ink);
            // entrance doors in the middle (lighter), under the vault
            float ec = a1 * .55f;
            m.Surface = ComicSurface.Glass;
            K.LQuad(m, l, ec - 1.3f, ec + 1.3f, .1f, 2.5f, -1.5f, GlassPale);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, ec - .04f, ec + .04f, .1f, 2.5f, -1.44f, -1.5f, Ink);
            // the balcony slab on white columns, glass balustrade
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0f, a1, f1 - .3f, f1, .3f, -1.6f, White);
            m.Surface = ComicSurface.PaintedMetal;
            for (float a = .5f; a <= a1; a += (a1 - 1f) / 4f) K.LBox(m, l, a - .14f, a + .14f, 0f, f1 - .3f, .1f, -.18f, White);
            m.Surface = ComicSurface.Glass;
            K.LBox(m, l, .2f, a1 - .2f, f1, f1 + 1f, .26f, .2f, GlassPale);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, .15f, a1 - .15f, f1 + 1f, f1 + 1.08f, .3f, .16f, White);
            // the roof: a deep fascia of coloured panels, and the barrel vault over the entrance
            K.LBox(m, l, -.3f, a1 + .1f, top - .9f, top, .6f, -D, Hex("B9C2CA"));
            Color[] panels = { Hex("9AA7B4"), Hex("C9B48E"), Hex("8FA3B5"), Hex("D9D4C7"), Hex("B8A38C"), Hex("7F93A6") };
            m.Surface = ComicSurface.Plain;
            int np = Mathf.RoundToInt(a1 / 1.9f);
            for (int i = 0; i < np; i++)
                K.LQuad(m, l, -.3f + (a1 + .4f) * i / np + .04f, -.3f + (a1 + .4f) * (i + 1) / np - .04f, top - .82f, top - .08f, .612f, Pick(panels, i));
            LText(m, l, "THE RISE", ec, top - .45f, .615f, .11f, Ink);
            m.Surface = ComicSurface.Metal;
            const int segs = 6;
            float v0 = ec - 4.2f, v1 = ec + 4.2f;
            for (int i = 0; i < segs; i++)
            {
                float t0 = (float)i / segs, t1 = (float)(i + 1) / segs;
                float aa = Mathf.Lerp(v0, v1, t0), ab = Mathf.Lerp(v0, v1, t1), ua = top + 1.9f * Mathf.Sin(t0 * Mathf.PI), ub = top + 1.9f * Mathf.Sin(t1 * Mathf.PI);
                Vector3 n = new Vector3(0, 1, 0) + Vector3.forward * (t0 + t1 < 1f ? -.5f : .5f);
                K.Face(m, l.P(aa, ua, 1.2f), l.P(ab, ub, 1.2f), l.P(ab, ub, -8f), l.P(aa, ua, -8f), Zinc, n);
                K.Face(m, l.P(aa, ua - .14f, 1.2f), l.P(ab, ub - .14f, 1.2f), l.P(ab, ub - .14f, -8f), l.P(aa, ua - .14f, -8f), White, -n);
                K.Face(m, l.P(aa, ua - .14f, 1.2f), l.P(ab, ub - .14f, 1.2f), l.P(ab, ub, 1.2f), l.P(aa, ua, 1.2f), White, toRoad);
                m.Surface = ComicSurface.Glass;
                K.Face(m, l.P(aa, top, .58f), l.P(ab, top, .58f), l.P(ab, ub - .14f, .58f), l.P(aa, ua - .14f, .58f), Glass, toRoad, false);
                m.Surface = ComicSurface.Metal;
            }
            m.Surface = ComicSurface.Metal;
            K.LBox(m, l, 2f, 5f, top, top + .9f, -5f, -8f, Mast);
            // the brick arcade tower: piers and arches, a cream upper stage with openings, a pyramid roof
            m.Surface = ComicSurface.Brick;
            float t0a = a1, t1a = W, to = 1.6f;
            foreach (float a in new[] { t0a + .35f, t1a - .35f })
                foreach (float o in new[] { to - .35f, -tw + to + .35f })
                    K.LBox(m, l, a - .35f, a + .35f, 0f, f1 + 1.3f, o + .35f, o - .35f, Brick);
            K.LBox(m, l, t0a, t1a, 2.9f, f1 + 1.3f, to, -tw + to, Brick);
            m.Surface = ComicSurface.Plain;
            K.LQuad(m, l, t0a + .7f, t1a - .7f, 0f, 2.9f, to - .6f, Hex("3A2E2A"));
            K.Face(m, l.P(t0a - .012f + .7f, 0f, to - .7f), l.P(t0a - .012f + .7f, 0f, -tw + to + .7f), l.P(t0a - .012f + .7f, 2.9f, -tw + to + .7f), l.P(t0a - .012f + .7f, 2.9f, to - .7f), Hex("3A2E2A"), Vector3.back);
            m.Surface = ComicSurface.Plaster;
            K.LBox(m, l, t0a - .1f, t1a + .1f, f1 + 1.3f, f1 + 1.5f, to + .1f, -tw + to - .1f, Cream);
            foreach (float a in new[] { t0a + .3f, t1a - .3f })
                foreach (float o in new[] { to - .3f, -tw + to + .3f })
                    K.LBox(m, l, a - .3f, a + .3f, f1 + 1.5f, top + 1.2f, o + .3f, o - .3f, Cream);
            K.LBox(m, l, t0a, t1a, top + .4f, top + 1.2f, to, -tw + to, Cream);
            m.Surface = ComicSurface.Glass;
            K.LBox(m, l, t0a + .5f, t1a - .5f, f1 + 1.5f, top + .4f, to - .5f, -tw + to + .5f, GlassDark);
            m.Surface = ComicSurface.Metal;
            K.LBox(m, l, t0a - .3f, t1a + .3f, top + 1.2f, top + 1.34f, to + .3f, -tw + to - .3f, White);
            m.Cylinder(l.P((t0a + t1a) * .5f, top + 2.1f, to - tw * .5f), tw * .82f, 1.5f, Galv, 4, .06f, Quaternion.Euler(0, 45, 0));
            TextDown(m, "LIBRARY", l.P(t0a + .35f, f1 + .7f, to + .012f), new Vector3(0, 0, l.S < 0 ? 1 : -1), Vector3.up, .07f, .46f, Cream);
        }

        /// <summary>The Hoffman kiln of the Maylands Brickworks: a long brick kiln with rounded ends, its lower walls
        /// battered (sloping) between brick buttresses, arched wicket openings along the base, brick piers carrying a wide
        /// low iron roof. Front (a long side) towards the road.</summary>
        static void HoffmanKiln(ComicMesh m, CqLot l)
        {
            float W = l.W, D = l.Depth, R = D * .5f, batter = 1.3f, hb = 2.7f, ht = 4.5f;
            Color low = Hex("9C4630"), high = Brick;
            Vector3 centre(float a) => l.P(a, 0f, -R);
            // outline of the plan (stadium): points round the kiln at offset off from the nominal wall
            List<Vector3> Ring(float off, float u)
            {
                var pts = new List<Vector3>();
                float r = R + off;
                for (int i = 0; i <= 4; i++) { float t = Mathf.PI * (i / 4f) - Mathf.PI * .5f; pts.Add(l.P(W - R + Mathf.Cos(t) * r, u, -R + (-Mathf.Sin(t)) * r)); }
                for (int i = 0; i <= 4; i++) { float t = Mathf.PI * (i / 4f) + Mathf.PI * .5f; pts.Add(l.P(R + Mathf.Cos(t) * r, u, -R + (-Mathf.Sin(t)) * r)); }
                return pts;
            }
            List<Vector3> b = Ring(batter, -1.5f), mid = Ring(0f, hb), top = Ring(0f, ht);
            int n = b.Count;
            Vector3 mc = (centre(R) + centre(W - R)) * .5f;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector3 outward = ((b[i] + b[j]) * .5f - mc); outward.y = 0f;
                m.Surface = ComicSurface.Brick;
                K.Face(m, b[i], b[j], mid[j], mid[i], low, outward.normalized + Vector3.up * .4f);
                K.Face(m, mid[i], mid[j], top[j], top[i], high, outward.normalized);
            }
            m.Surface = ComicSurface.Concrete;
            for (int i = 1; i < n - 1; i++) K.TriFacing(m, top[0], top[i], top[i + 1], ConcreteDark, Vector3.up);
            // buttresses and wickets along both long sides, a wicket at each rounded end
            foreach (int side in new[] { 1, -1 })
            {
                float oWall = side > 0 ? 0f : -D;
                for (float a = R; a <= W - R + .01f; a += (W - 2f * R) / 6f)
                {
                    m.Surface = ComicSurface.Brick;
                    float of = oWall + side * (batter + .75f), oh = oWall + side * .1f;
                    Vector3 nrm = new Vector3(-l.S * side, .5f, 0);
                    K.Face(m, l.P(a - .32f, -1.5f, of), l.P(a + .32f, -1.5f, of), l.P(a + .32f, hb + 1f, oh), l.P(a - .32f, hb + 1f, oh), high, nrm);
                    foreach (float da in new[] { -.32f, .32f })
                        K.TriFacing(m, l.P(a + da, -1.5f, of), l.P(a + da, hb + 1f, oh), l.P(a + da, -1.5f, oWall - side * .1f), Mix(high, Ink, .12f), da < 0f ? Vector3.back : Vector3.forward);
                }
                for (float a = R + (W - 2f * R) / 12f; a < W - R; a += (W - 2f * R) / 6f)
                {
                    m.Surface = ComicSurface.Plain;
                    float On(float u) => oWall + side * (batter * (1f - (u + 1.5f) / (hb + 1.5f)) + .03f);
                    K.Face(m, l.P(a - .5f, .05f, On(.05f)), l.P(a + .5f, .05f, On(.05f)), l.P(a + .5f, 1.5f, On(1.5f)), l.P(a - .5f, 1.5f, On(1.5f)), Hex("2A1E1A"), new Vector3(-l.S * side, .5f, 0));
                    K.TriFacing(m, l.P(a - .5f, 1.5f, On(1.5f)), l.P(a + .5f, 1.5f, On(1.5f)), l.P(a, 1.95f, On(1.95f)), Hex("2A1E1A"), new Vector3(-l.S * side, .5f, 0));
                }
            }
            // openings high in the wall under the roof
            m.Surface = ComicSurface.Plain;
            for (float a = R + 1f; a < W - R; a += 2.6f) K.LQuad(m, l, a - .25f, a + .25f, hb + .7f, hb + 1.5f, .012f, Hex("2A1E1A"));
            // brick piers and the roof
            m.Surface = ComicSurface.Brick;
            float eave = ht + .9f;
            foreach (float a in new[] { .4f, W * .33f, W * .66f, W - .4f })
                foreach (float o in new[] { batter + .2f, -D - batter - .2f })
                    K.LBox(m, l, a - .35f, a + .35f, -1.5f, eave, o + .35f, o - .35f, high);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -1.7f, W + 1.7f, eave, eave + .18f, batter + 1.2f, -D - batter - 1.2f, Hex("E4E2D8"));
            m.Surface = ComicSurface.Metal;
            HipRoof(m, l, -1.8f, W + 1.8f, eave + .18f, eave + 2.3f, batter + 1.3f, -D - batter - 1.3f, Galv);
        }

        /// <summary>The brickworks chimney: 34 m, square, tapering, in dark red brick with steel straps, a cream band
        /// below the top and a dark cap.</summary>
        static void BrickChimney(ComicMesh m, Vector3 p, float h)
        {
            const float r0 = 2.4f, taper = .6f;
            float Rad(float y) => r0 * Mathf.Lerp(1f, taper, y / h);
            Quaternion q = Quaternion.Euler(0, 45, 0);
            void Seg(float y0, float y1, Color c, ComicSurface s, float grow = 0f)
            {
                m.Surface = s;
                m.Cylinder(p + V(0, (y0 + y1) * .5f, 0), Rad(y0) + grow, y1 - y0, c, 4, (Rad(y1) + grow) / (Rad(y0) + grow), q);
            }
            m.Surface = ComicSurface.Brick;
            m.Box(p + V(0, 1.2f, 0), V(4.2f, 2.4f, 4.2f), BrickDark);
            Seg(2.4f, h * .78f, BrickDark, ComicSurface.Brick);
            Seg(h * .78f, h * .9f, Hex("E3D29A"), ComicSurface.Brick);
            Seg(h * .9f, h, Hex("7A3B2A"), ComicSurface.Brick);
            Seg(h, h + .5f, Hex("3A2E2A"), ComicSurface.Concrete, .12f);
            for (float y = 5f; y < h * .78f; y += 3.2f) Seg(y, y + .16f, Hex("4A4F57"), ComicSurface.Metal, .05f);
        }

        /// <summary>The pug mill: a tall shed of silver corrugated iron with a gable roof, a lean-to, a hoist door.</summary>
        static void PugMill(ComicMesh m, CqLot l)
        {
            float W = l.W, D = l.Depth, h = 9f;
            Color iron = Hex("C3CAD0"), ironDark = Hex("9AA3AA");
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, .2f, .05f, -D, ConcreteDark);
            m.Surface = ComicSurface.Metal;
            K.LBox(m, l, 0, W, .2f, h, 0f, -D, iron);
            m.Surface = ComicSurface.Plain;
            for (float a = .5f; a < W; a += .5f) K.LQuad(m, l, a - .02f, a + .02f, .3f, h - .1f, .012f, ironDark, false);
            for (float o = -.5f; o > -D; o -= .5f)
                K.Face(m, l.P(-.012f, .3f, o - .02f), l.P(-.012f, .3f, o + .02f), l.P(-.012f, h - .1f, o + .02f), l.P(-.012f, h - .1f, o - .02f), ironDark, Vector3.back, false);
            m.Surface = ComicSurface.Metal;
            // gable roof with the ridge running back from the road
            K.LGable(m, l, -.3f, W + .3f, h, h + 2.2f, .3f, -D - .3f, ironDark);
            m.Surface = ComicSurface.Plain;
            K.TriFacing(m, l.P(.1f, h, .31f), l.P(W - .1f, h, .31f), l.P(W * .5f, h + 1.9f, .31f), iron, new Vector3(-l.S, 0, 0));
            // the lean-to on the road side and a big dark doorway
            m.Surface = ComicSurface.Metal;
            K.Face(m, l.P(W * .2f, 4.4f, 0f), l.P(W + .2f, 4.4f, 0f), l.P(W + .2f, 3.4f, 3.2f), l.P(W * .2f, 3.4f, 3.2f), ironDark, new Vector3(-l.S, 2, 0));
            m.Surface = ComicSurface.Wood;
            foreach (float a in new[] { W * .2f + .1f, W * .6f, W + .1f }) K.LBox(m, l, a - .1f, a + .1f, .2f, 3.4f, 3.1f, 2.9f, TimberDark);
            m.Surface = ComicSurface.Plain;
            K.LQuad(m, l, W * .3f, W * .7f, .2f, 3.2f, .014f, Hex("2A2622"));
            K.LQuad(m, l, W * .62f, W * .86f, 5.6f, 7.2f, .014f, Hex("2A2622"));
            m.Surface = ComicSurface.Wood;
            m.Beam(l.P(W * .74f, 7.4f, 0f), l.P(W * .74f, 7.6f, 1.6f), .16f, TimberDark);
        }

        /// <summary>A drying shed: a long low iron roof on rows of timber posts, racks of bricks drying underneath.</summary>
        static void DryingShed(ComicMesh m, CqLot l)
        {
            float W = l.W, D = l.Depth;
            m.Surface = ComicSurface.Wood;
            for (float a = .3f; a <= W; a += (W - .6f) / 5f)
                foreach (float o in new[] { -.2f, -D + .2f }) K.LBox(m, l, a - .09f, a + .09f, -1f, 2.9f, o + .09f, o - .09f, TimberDark);
            K.LBox(m, l, 0f, W, 2.8f, 2.95f, 0f, -.3f, TimberDark);
            m.Surface = ComicSurface.Metal;
            K.LSlope(m, l, -.4f, W + .4f, 2.95f, 3.9f, .5f, -D * .5f, -D - .5f, Hex("9AA3AA"));
            m.Surface = ComicSurface.Brick;
            for (float a = 1.2f; a < W - 1.5f; a += 3.4f)
            {
                K.LBox(m, l, a, a + 2.2f, .2f, 1.3f, -1.2f, -D + 1.2f, a % 2 < 1 ? BrickOrange : Brick);
                m.Surface = ComicSurface.Wood;
                K.LBox(m, l, a - .05f, a + 2.25f, .05f, .2f, -1.1f, -D + 1.1f, Timber);
                m.Surface = ComicSurface.Brick;
            }
        }

        /// <summary>The brickworks gatehouse: a small brick office with a terracotta hipped roof.</summary>
        static void Gatehouse(ComicMesh m, CqLot l)
        {
            float W = l.W, D = l.Depth;
            m.Surface = ComicSurface.Concrete;
            K.LBox(m, l, 0, W, -2.6f, .2f, .05f, -D, Limestone);
            m.Surface = ComicSurface.Brick;
            K.LBox(m, l, 0, W, .2f, 3f, 0f, -D, Brick);
            Sash(m, l, W * .5f, 1f, 1.1f, 2.4f, 0f, White, Cream, true);
            EndWindow(m, l, 0f, -1.4f, -2.5f, 1.1f, 2.4f, White);
            m.Surface = ComicSurface.PaintedMetal;
            K.LBox(m, l, -.4f, W + .4f, 2.9f, 3.05f, .4f, -D - .4f, White);
            m.Surface = ComicSurface.Brick;
            HipRoof(m, l, -.45f, W + .45f, 3.05f, 4.5f, .45f, -D - .45f, Tile);
        }

        // ================================================================================== river, wetland and golf props

        /// <summary>Sheoak (casuarina) of the river bank: a dark trunk, fine drooping grey-green foliage in tall narrow
        /// plumes.</summary>
        static void Sheoak(ComicMesh m, Vector3 p, float h, int seed)
        {
            var r = new System.Random(seed * 311 + 5);
            Color bark = Hex("5A4A3E"), leaf = Hex("5B7A58"), leafLight = Hex("7A9670"), leafDark = Hex("4A6648");
            m.Surface = ComicSurface.Wood;
            Vector3 top = p + V(.2f, h * .6f, 0);
            Limb(m, p, top, .18f + h * .012f, .1f, bark, 6);
            float spin = F(r, 0f, 6f);
            for (int i = 0; i < 6; i++)
            {
                float a = spin + i * 1.05f, rad = i == 0 ? 0f : h * F(r, .1f, .16f);
                Vector3 c = p + V(Mathf.Cos(a) * rad, h * (i == 0 ? .8f : F(r, .45f, .7f)), Mathf.Sin(a) * rad);
                if (i > 0) { m.Surface = ComicSurface.Wood; Limb(m, p + V(.1f, h * .35f, 0), c, .08f, .04f, bark, 4); }
                m.Surface = ComicSurface.Foliage;
                Blob(m, c, V(h * .17f, h * (i == 0 ? .42f : .34f), h * .17f), i % 3 == 0 ? leaf : i % 3 == 1 ? leafLight : leafDark, 6, 4, a, .2f);
            }
        }

        /// <summary>A clump of reeds / rushes: a fan of broad blades, a few with dark seed heads.</summary>
        static void Reeds(ComicMesh m, Vector3 p, float h, int seed)
        {
            m.Surface = ComicSurface.Foliage;
            for (int i = 0; i < 7; i++)
            {
                float a = seed * 1.7f + i * .9f, lean = .18f + .12f * (i % 3);
                Vector3 dir = V(Mathf.Cos(a), 0, Mathf.Sin(a)), side = Vector3.Cross(Vector3.up, dir) * .11f;
                Vector3 b = p + dir * .15f, t = p + dir * (.15f + h * lean) + V(0, h * (.75f + .12f * (i % 2)), 0);
                Color c = i % 3 == 0 ? Reed : i % 3 == 1 ? ReedDark : Hex("A3B062");
                m.Tri(b - side, b + side, t, c);
                m.Tri(b + side, b - side, t, c);
                if (i % 4 == 0) { m.Surface = ComicSurface.Wood; m.Box(t + V(0, -.1f, 0), V(.06f, .2f, .06f), Hex("6B4E36")); m.Surface = ComicSurface.Foliage; }
            }
        }

        /// <summary>A golf green: a paler mown disc with a flag on a pin, a sand bunker beside it.</summary>
        static void GolfGreen(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Foliage;
            m.Cylinder(p + V(0, .03f, 0), 6f, .06f, Hex("8CC65A"), 10);
            m.Surface = ComicSurface.Paving;
            m.Cylinder(p + V(-5f, .03f, -7.5f), 3.2f, .06f, Hex("EFE2B8"), 8, 1f, Quaternion.Euler(0, 20f, 0));
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(.5f, 1.1f, 0), .03f, 2.2f, White, 5);
            m.Surface = ComicSurface.Fabric;
            m.Tri(p + V(.5f, 2.2f, 0), p + V(.5f, 1.75f, 0), p + V(.5f, 1.97f, .7f), Hex("E2463A"));
            m.Tri(p + V(.5f, 1.75f, 0), p + V(.5f, 2.2f, 0), p + V(.5f, 1.97f, .7f), Hex("E2463A"));
        }

        /// <summary>A golf buggy: white body, a canopy roof on four posts, a bench seat, two bags of clubs on the back.</summary>
        static void GolfBuggy(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            void B(Vector3 at, Vector3 size, Color col, ComicSurface s) { m.Surface = s; m.Box(p + q * at, size, col, q); }
            B(V(0, .5f, 0), V(1.2f, .45f, 2.3f), White, ComicSurface.PaintedMetal);
            B(V(0, .78f, -.25f), V(1.1f, .2f, .6f), Hex("C9B48E"), ComicSurface.Fabric);
            B(V(0, 1.02f, -.56f), V(1.1f, .5f, .12f), Hex("C9B48E"), ComicSurface.Fabric);
            B(V(0, 1.95f, -.1f), V(1.3f, .08f, 1.9f), White, ComicSurface.PaintedMetal);
            foreach (float x in new[] { -.55f, .55f }) foreach (float z in new[] { -.9f, .75f }) B(V(x, 1.3f, z), V(.05f, 1.3f, .05f), Ink, ComicSurface.PaintedMetal);
            foreach (float x in new[] { -.6f, .6f }) foreach (float z in new[] { -.8f, .8f }) B(V(x, .22f, z), V(.2f, .44f, .44f), Ink, ComicSurface.Rubber);
            B(V(-.25f, 1f, -1f), V(.28f, .9f, .28f), Hex("2F6FB0"), ComicSurface.Fabric);
            B(V(.25f, 1f, -1f), V(.28f, .9f, .28f), Hex("B33A32"), ComicSurface.Fabric);
        }

        /// <summary>Picnic shelter of the foreshore: timber posts, a hipped iron roof, a table with benches on a slab.</summary>
        static void PicnicShelter(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.Concrete;
            m.Box(p + V(0, .06f, 0), V(4.6f, .12f, 4.6f), Concrete);
            m.Surface = ComicSurface.Wood;
            foreach (float x in new[] { -2f, 2f }) foreach (float z in new[] { -2f, 2f }) m.Box(p + V(x, 1.3f, z), V(.16f, 2.6f, .16f), TimberDark);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 2.66f, 0), V(4.8f, .14f, 4.8f), White);
            m.Surface = ComicSurface.Metal;
            m.Cylinder(p + V(0, 3.25f, 0), 3.6f, 1.1f, RoofGreen, 4, .1f, Quaternion.Euler(0, 45, 0));
            m.Surface = ComicSurface.Wood;
            m.Box(p + V(0, .8f, 0), V(2f, .08f, .85f), Timber);
            foreach (float z in new[] { -.75f, .75f }) m.Box(p + V(0, .48f, z), V(2f, .07f, .3f), Timber);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.8f, .8f }) { m.Box(p + V(x, .42f, 0), V(.08f, .8f, .5f), Ink); m.Box(p + V(x, .24f, 0), V(.08f, .08f, 1.7f), Ink); }
        }

        /// <summary>A rowing four on the river: a long thin shell, four rowers, oars out both sides.</summary>
        static void RowingShell(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.Plastic;
            m.Box(p + q * V(0, .1f, 0), V(.5f, .2f, 9f), White, q);
            m.Box(p + q * V(0, .1f, 5f), V(.3f, .16f, 1.2f), White, q);
            m.Box(p + q * V(0, .1f, -5f), V(.3f, .16f, 1.2f), White, q);
            Color[] shirts = { Hex("2F6FB0"), Hex("F6C543"), Hex("2F6FB0"), Hex("F6C543") };
            for (int i = 0; i < 4; i++)
            {
                float z = -2.7f + i * 1.8f;
                m.Surface = ComicSurface.Fabric;
                m.Box(p + q * V(0, .55f, z), V(.42f, .6f, .3f), shirts[i], q);
                m.Surface = ComicSurface.Plastic;
                m.Box(p + q * V(0, 1f, z), V(.26f, .26f, .26f), Hex("E8B48C"), q);
                m.Surface = ComicSurface.Wood;
                int s = i % 2 == 0 ? -1 : 1;
                m.Beam(p + q * V(0, .45f, z + .3f), p + q * V(s * 2.6f, .12f, z + .9f), .06f, Timber);
                m.Box(p + q * V(s * 2.8f, .1f, z + .95f), V(.5f, .04f, .2f), Hex("E2463A"), q);
            }
        }

        // ================================================================================== Maylands obstacles and animals

        /// <summary>A pallet of red bricks, strapped, fresh from the kiln.</summary>
        static ComicMesh BrickPalletMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            foreach (float z in new[] { -.42f, 0f, .42f }) m.Box(V(0, .06f, z), V(1.16f, .12f, .14f), Timber);
            m.Box(V(0, .15f, 0), V(1.16f, .06f, 1.04f), DeckLight);
            m.Surface = ComicSurface.Brick;
            for (int layer = 0; layer < 3; layer++)
                for (int i = 0; i < 2; i++)
                {
                    bool turn = layer % 2 == 1;
                    Vector3 c = V(turn ? 0f : (i - .5f) * .54f, .31f + layer * .22f, turn ? (i - .5f) * .5f : 0f);
                    m.Box(c, turn ? V(1.06f, .2f, .46f) : V(.5f, .2f, .98f), (layer + i) % 2 == 0 ? Brick : BrickOrange);
                }
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.28f, .28f }) m.Box(V(x, .52f, 0), V(.05f, .7f, 1.02f), Ink);
            return m;
        }

        /// <summary>Market crates of oranges and apples outside the grocer.</summary>
        static ComicMesh FruitCratesMesh()
        {
            var m = new ComicMesh();
            for (int i = 0; i < 3; i++)
            {
                Vector3 c = i == 2 ? V(0, .56f, 0) : V((i - .5f) * .54f, .2f, 0);
                m.Surface = ComicSurface.Wood;
                m.Box(c, V(.5f, .36f, .7f), Timber);
                m.Box(c + V(0, .1f, -.36f), V(.5f, .08f, .02f), TimberDark);
                m.Surface = ComicSurface.Plastic;
                Color fruit = i == 0 ? Hex("F28C28") : i == 1 ? Hex("D8323A") : Hex("8CC65A");
                for (int k = 0; k < 6; k++) m.Box(c + V(-.15f + (k % 3) * .15f, .2f, -.18f + (k / 3) * .34f), Vector3.one * .14f, fruit, Quaternion.Euler(20, k * 30, 20));
            }
            return m;
        }

        /// <summary>A golf bag standing on its legs with clubs sticking out.</summary>
        static ComicMesh GolfBagMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Quaternion lean = Quaternion.Euler(-12f, 0, 0);
            m.Cylinder(V(0, .62f, .05f), .2f, 1.05f, Hex("2F6FB0"), 8, 1.1f, lean);
            m.Cylinder(V(0, 1.14f, -.06f), .23f, .1f, Ink, 8, 1f, lean);
            m.Box(V(0, .62f, -.14f), V(.22f, .5f, .1f), White, lean);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (int s in new[] { -1, 1 }) m.Beam(V(s * .12f, .95f, .1f), V(s * .28f, 0f, .5f), .04f, Ink);
            m.Surface = ComicSurface.Metal;
            for (int i = 0; i < 3; i++)
            {
                Vector3 b = V(-.1f + i * .1f, 1.15f, -.08f), t = b + V(0, .32f, -.08f + i * .02f);
                m.Beam(b, t, .03f, Hex("C9CED3"));
                m.Box(t + V(0, .04f, -.04f), V(.07f, .1f, .14f), i == 1 ? Ink : Hex("C9CED3"));
            }
            return m;
        }

        /// <summary>A black swan walking: black body with ruffled wing feathers and white wing tips, a long S neck, a red
        /// bill with a white band, grey legs. Facing -Z, feet at the pivot.</summary>
        static ComicMesh BlackSwanMesh()
        {
            var m = new ComicMesh();
            Color black = Hex("23262B"), dark = Hex("32363C"), red = Hex("D8323A"), white = Hex("F2F2EE"), legs = Hex("4A4F57");
            m.Surface = ComicSurface.Fabric;
            Blob(m, V(0, .5f, .1f), V(.5f, .46f, .95f), black, 6, 4, 0f, .06f);
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(V(s * .22f, .58f, .16f), V(.08f, .26f, .6f), dark, Quaternion.Euler(-8f, 0, 0));
                m.Box(V(s * .2f, .64f, .5f), V(.07f, .12f, .2f), white, Quaternion.Euler(-14f, 0, 0));
            }
            m.Box(V(0, .62f, .6f), V(.26f, .16f, .26f), dark, Quaternion.Euler(-24f, 0, 0));
            m.Beam(V(0, .62f, -.28f), V(0, .98f, -.44f), .15f, black);
            m.Beam(V(0, .98f, -.44f), V(0, 1.22f, -.3f), .13f, black);
            m.Beam(V(0, 1.22f, -.3f), V(0, 1.28f, -.46f), .13f, black);
            m.Box(V(0, 1.27f, -.52f), V(.14f, .14f, .16f), black);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, 1.23f, -.68f), V(.09f, .08f, .2f), red, Quaternion.Euler(14f, 0, 0));
            m.Box(V(0, 1.2f, -.74f), V(.095f, .085f, .04f), white, Quaternion.Euler(14f, 0, 0));
            foreach (float x in new[] { -.05f, .05f }) m.Box(V(x, 1.3f, -.57f), V(.03f, .04f, .04f), red);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.1f, .1f })
            {
                m.Box(V(x, .16f, .08f), V(.05f, .32f, .05f), legs);
                m.Box(V(x, .02f, 0f), V(.16f, .04f, .22f), legs);
            }
            return m;
        }

        /// <summary>A black swan on the water (scenery): the body low, the neck curved, a white wake line.</summary>
        static ComicMesh SwimmingSwanMesh()
        {
            var m = new ComicMesh();
            Color black = Hex("23262B"), dark = Hex("32363C"), red = Hex("D8323A"), white = Hex("F2F2EE");
            m.Surface = ComicSurface.Fabric;
            Blob(m, V(0, .18f, .1f), V(.5f, .42f, .95f), black, 6, 4, 0f, .06f);
            foreach (int s in new[] { -1, 1 }) m.Box(V(s * .2f, .3f, .2f), V(.08f, .22f, .56f), dark, Quaternion.Euler(-14f, 0, 0));
            m.Box(V(0, .36f, .56f), V(.24f, .14f, .22f), white, Quaternion.Euler(-28f, 0, 0));
            m.Beam(V(0, .28f, -.28f), V(0, .68f, -.42f), .14f, black);
            m.Beam(V(0, .68f, -.42f), V(0, .9f, -.3f), .12f, black);
            m.Beam(V(0, .9f, -.3f), V(0, .94f, -.46f), .12f, black);
            m.Box(V(0, .93f, -.52f), V(.13f, .13f, .15f), black);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .89f, -.67f), V(.085f, .075f, .19f), red, Quaternion.Euler(14f, 0, 0));
            m.Surface = ComicSurface.Plain;
            K.Face(m, V(-.5f, .02f, .5f), V(-.9f, .02f, 1.9f), V(-.8f, .02f, 1.9f), V(-.4f, .02f, .5f), RiverLight, Vector3.up, false);
            K.Face(m, V(.5f, .02f, .5f), V(.9f, .02f, 1.9f), V(.8f, .02f, 1.9f), V(.4f, .02f, .5f), RiverLight, Vector3.up, false);
            return m;
        }

        /// <summary>A great egret standing in the shallows (scenery): white, a long neck, a yellow dagger bill, dark legs.</summary>
        static ComicMesh EgretMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color white = Hex("F4F4F0");
            Blob(m, V(0, .75f, .05f), V(.3f, .3f, .6f), white, 6, 3, 0f, .05f);
            m.Beam(V(0, .82f, -.18f), V(0, 1.2f, -.3f), .08f, white);
            m.Beam(V(0, 1.2f, -.3f), V(0, 1.42f, -.2f), .07f, white);
            m.Box(V(0, 1.45f, -.26f), V(.1f, .1f, .14f), white);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, 1.44f, -.42f), V(.04f, .04f, .22f), Hex("F2C230"));
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.05f, .05f }) m.Box(V(x, .3f, .06f), V(.03f, .6f, .03f), Hex("2A2E34"));
            return m;
        }

        /// <summary>A Pacific black duck: mottled brown, a dark crown and eye stripe on a pale face, a green wing patch.</summary>
        static ComicMesh DuckMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color brown = Hex("6B5440"), pale = Hex("D9C39A"), dark = Hex("3A2E26");
            m.Box(V(0, .2f, .04f), V(.2f, .17f, .36f), brown);
            m.Box(V(0, .22f, .24f), V(.14f, .1f, .12f), dark, Quaternion.Euler(-20f, 0, 0));
            m.Box(V(0, .34f, -.14f), V(.13f, .14f, .14f), pale);
            m.Box(V(0, .41f, -.13f), V(.135f, .04f, .15f), dark);
            m.Box(V(0, .35f, -.145f), V(.14f, .025f, .15f), dark);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .31f, -.26f), V(.07f, .035f, .12f), Hex("5F6B5A"));
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(V(s * .105f, .22f, .06f), V(.02f, .08f, .14f), Hex("2F8F6A"));
                m.Box(V(s * .05f, .06f, 0f), V(.03f, .12f, .03f), Hex("C98A3A"));
                m.Box(V(s * .05f, .012f, -.03f), V(.07f, .024f, .1f), Hex("C98A3A"));
            }
            return m;
        }

        // ================================================================================== far scenery

        /// <summary>
        /// Maylands' horizon: on the right the Swan River, wide and blue with a few sails and rowing boats, the far bank's
        /// river gums and a cluster of pale apartment towers, the flat land beyond and the blue Darling Scarp; on the left,
        /// across the railway, the roofs of Maylands with Norfolk pines and a water tower, and the Scarp again far behind.
        /// Sheared with the road's grade so it meets the chunks' water. No colliders.
        /// </summary>
        static ComicMesh MayFar(float grade)
        {
            var m = new ComicMesh();
            var r = new System.Random(250);
            Color haze = Hex("C9DDE8");
            const float z0 = -220f, z1 = 820f;
            // right: the river from where the chunks' own ground and water end
            FarStrip(m, 150f, 300f, z0, z1, MayRiver - .05f, Haze(RiverDeep, 200f, haze));
            FarStrip(m, 300f, 470f, z0, z1, MayRiver - .05f, Haze(River, 420f, haze));
            FarStrip(m, 470f, 1500f, z0, z1, MayRiver + .6f, Haze(Hex("8FA376"), 700f, haze));
            FarStrip(m, K.Edge, 160f, 330f, z1, MayRiver - .06f, Haze(RiverDeep, 200f, haze));
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 120; i++)
            {
                float x = F(r, 155f, 460f), z = F(r, z0 + 20f, z1 - 20f), len = F(r, 3f, 9f);
                K.Face(m, V(x, MayRiver - .02f, z), V(x, MayRiver - .02f, z + .6f), V(x + len, MayRiver - .02f, z + .6f), V(x + len, MayRiver - .02f, z), Haze(RiverLight, x, haze), Vector3.up, false);
            }
            for (int i = 0; i < 7; i++)
            {
                float x = F(r, 170f, 400f), z = F(r, -80f, 560f), s = F(r, 1.6f, 2.4f);
                m.Surface = ComicSurface.PaintedMetal;
                m.Box(V(x, MayRiver + .35f * s, z), V(1.5f * s, .7f * s, 4.6f * s), White);
                m.Cylinder(V(x, MayRiver + 4f * s, z + .4f * s), .07f * s, 6.6f * s, Ink, 5);
                m.Surface = ComicSurface.Fabric;
                Vector3 foot = V(x, MayRiver + 1f * s, z + .3f * s), head = V(x, MayRiver + 7f * s, z + .35f * s), clew = V(x, MayRiver + 1.1f * s, z - 1.9f * s);
                m.Tri(foot, head, clew, White);
                m.Tri(head, foot, clew, White);
            }
            FarTreeLine(m, r, 1, 472f, 40f, z0, z1, MayRiver + .6f, 12f, 19f, GumLeafDark, GumLeaf, haze, 500f);
            FarTreeLine(m, r, 1, 540f, 60f, z0, z1, MayRiver + .6f, 10f, 15f, GumLeaf, FigMid, haze, 640f);
            foreach (float z in new[] { -140f, -90f, 180f, 230f, 270f, 520f })
            {
                float h = F(r, 28f, 55f), x = F(r, 560f, 640f);
                m.Surface = ComicSurface.Plaster;
                m.Box(V(x, h * .5f, z), V(18f, h, 22f), Haze(White, 660f, haze));
                m.Box(V(x - 9.2f, h * .5f, z), V(.6f, h * .86f, 16f), Haze(GlassPale, 660f, haze));
            }
            Scarp(m, 1, 880f, z0, z1, MayRiver, 70f, haze, .6f);
            // left: Maylands across the railway
            FarStrip(m, -60f, -1500f, z0, z1, -.3f, Haze(Hex("8FA86A"), 300f, haze));
            FarStrip(m, -K.Edge, -60f, 330f, z1, K.Kerb - .05f, Lawn);
            FarSuburb(m, r, -1, 64f, 300f, z0, z1, -.3f, haze, .36f);
            for (float z = z0 + 30f; z < z1; z += F(r, 60f, 110f))
            {
                float x = -F(r, 90f, 240f), h = F(r, 20f, 27f);
                m.Surface = ComicSurface.Wood;
                m.Cylinder(V(x, h * .5f, z), .5f, h, Haze(Hex("6E5A47"), 200f, haze), 5, .3f);
                m.Surface = ComicSurface.Foliage;
                for (int i = 0; i < 4; i++) m.Cylinder(V(x, Mathf.Lerp(h * .38f, h * .92f, i / 3f), z), Mathf.Lerp(h * .2f, h * .07f, i / 3f), h * .07f, Haze(Hex("2C5A36"), 200f, haze), 6, .55f);
            }
            // a water tower above the roofs
            Vector3 wt = V(-230f, 0f, 150f);
            m.Surface = ComicSurface.Concrete;
            m.Cylinder(wt + V(0, 9f, 0), 3f, 18f, Haze(Concrete, 250f, haze), 8, .8f);
            m.Cylinder(wt + V(0, 21f, 0), 7f, 6f, Haze(Hex("E4E2D8"), 250f, haze), 10, 1.15f);
            m.Cylinder(wt + V(0, 24.6f, 0), 8.1f, 1.2f, Haze(Zinc, 250f, haze), 10, .3f);
            Scarp(m, -1, 960f, z0, z1, -.3f, 60f, haze, 2.2f);
            m.Shear(grade);
            return m;
        }
    }
}
