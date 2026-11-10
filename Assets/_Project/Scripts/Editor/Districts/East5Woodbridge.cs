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
    /// <summary>WOODBRIDGE (order 330): see <see cref="East5Builder"/>. River and Woodbridge House on the left, the
    /// Midland line on the right.</summary>
    public static partial class East5Builder
    {
        static readonly float[] TwoTracks = { T1, T2 };

        static void BuildWoodbridge(TrackTheme theme, bool atlas)
        {
            K.Begin(WoodMeshes, WoodPrefabs, CbdBuilder.DataFolder);
            Obstacle bin = K.SaveObstacle(WoodPf("OBS_Woodbridge_WheelieBin"), "OBS_Woodbridge_WheelieBin", WheelieBinMesh(K.Hex("2F6B3E"), Yellow), new Vector3(.7f, 1.1f, .75f), ObstacleKind.Thing, true, true, 1f);
            Obstacle bollard = K.SaveObstacle(WoodPf("OBS_Woodbridge_PathBollard"), "OBS_Woodbridge_PathBollard", PathBollardMesh(), new Vector3(.45f, 1f, .45f), ObstacleKind.Thing, true, false, .9f);
            Obstacle crates = K.SaveObstacle(WoodPf("OBS_Woodbridge_GrapeCrates"), "OBS_Woodbridge_GrapeCrates", GrapeCratesMesh(), new Vector3(1.1f, .95f, .8f), ObstacleKind.Thing, true, true, .9f);
            Obstacle gate = K.SaveObstacle(WoodPf("OBS_Woodbridge_MazeGate"), "OBS_Woodbridge_MazeGate", MazeGateMesh(), new Vector3(1.9f, 1.25f, .3f), ObstacleKind.Thing, false, false, .7f);
            GameObject duck = SaveAnimal(WoodPf("ANM_Woodbridge_Duck"), "ANM_Woodbridge_Duck", DuckMesh(), new Vector3(0, .22f, 0), new Vector3(.4f, .45f, .65f), .5f, true);
            GameObject ibis = SaveAnimal(WoodPf("ANM_Woodbridge_Ibis"), "ANM_Woodbridge_Ibis", IbisMesh(), new Vector3(0, .5f, 0), new Vector3(.5f, 1f, .9f), .35f, false);
            GameObject swan = SaveAnimal(WoodPf("ANM_Woodbridge_BlackSwan"), "ANM_Woodbridge_BlackSwan", BlackSwanMesh(), new Vector3(0, .5f, 0), new Vector3(.6f, 1f, 1.1f), .25f, false);
            Ramp kicker = K.SaveRamp(WoodPf("RMP_Woodbridge_Boardwalk"), "RMP_Woodbridge_Boardwalk", 3f, 5f, 1.3f, 1.5f, 1f, .5f, DeckLight, Deck, false);

            K.WriteChunk(WoodEntry(), WoodChunkPath("Entry"));
            K.WriteChunk(WoodCottages(), WoodChunkPath("Cottages"));
            K.WriteChunk(WoodStation(), WoodChunkPath("Station"));
            K.WriteChunk(WoodHouse(), WoodChunkPath("House"));
            K.WriteChunk(WoodRiverside(), WoodChunkPath("Riverside"));
            K.WriteChunk(WoodCreek(), WoodChunkPath("Creek"));
            K.WriteChunk(WoodExit(), WoodChunkPath("Exit"));
            GameObject far = K.SaveFarScenery(WoodPf("FAR_Woodbridge_SwanAndScarp"), "FAR_Woodbridge_SwanAndScarp", WoodFar(-WoodDrop / 40f));

            bool created = AssetDatabase.LoadAssetAtPath<District>(WoodDistrictPath) == null;
            District d = K.LoadOrCreate<District>(WoodDistrictPath);
            // Placeholders when the asset is first created (far from the city: the CBD skyline behind and sunk away); the
            // district system owns these fields after this and they are never written again.
            if (created) { d.skyYaw = 180f; d.skySize = .6f; d.skyHeight = -12f; d.skyline = 0f; }
            d.id = WoodId; d.displayName = "WOODBRIDGE"; d.order = 330; d.length = new Vector2(280f, 280f);   // entry + the five chunks + exit: each passes once
            d.chunks = WoodChunkIds.Select(c => Chunk(WoodChunkPath(c))).ToArray();
            d.orderedChunks = true;
            d.entry = Chunk(WoodChunkPath("Entry"));
            d.exit = Chunk(WoodChunkPath("Exit"));
            d.allowBoulevard = false;
            d.obstacles = new[] { bin, bollard, crates, gate };
            d.jumpObstacles = new[] { bollard, crates, bin };
            d.gifts = Array.Empty<Gift>();
            d.hazards = ThemeHazards(theme, "HZ_OncomingCar", "HZ_SlowCar", "HZ_Crossing", "HZ_SidewalkStroll", "HZ_PoliceStop");
            var people = new List<GameObject>(K.People(theme, "Jogger", "Jogger", "DogWalker", "DogWalker", "Elder", "Kid", "Tourist", "Student", "Skater"));
            people.Add(duck); people.Add(duck); people.Add(ibis); people.Add(swan);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { kicker };
            d.rampFrequency = 1f;
            d.blendMeters = 150f;
            // A warm late-spring afternoon under the gums: soft green-gold light, pale haze towards the hills.
            d.fogColor = new Color(.80f, .87f, .86f);
            d.skyTint = new Color(1f, .99f, .96f);
            d.ambientSky = new Color(.70f, .84f, .95f);
            d.ambientEquator = new Color(.88f, .88f, .78f);
            d.ambientGround = new Color(.62f, .64f, .46f);
            d.sunColor = new Color(1f, .95f, .84f);
            d.sunIntensity = 1.4f;
            d.farScenery = far;
            // gateway, ambience, ambienceVolume, the sky fields and line belong to the district system: not written here.
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/Woodbridge] " + string.Join("; ", K.Report));
            if (atlas) RenderWoodAtlas(bin, bollard, crates, gate, duck, ibis, swan, kicker);
        }

        static void RenderWoodAtlas(Obstacle bin, Obstacle bollard, Obstacle crates, Obstacle gate, GameObject duck, GameObject ibis, GameObject swan, Ramp kicker)
        {
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("WoodbridgeHouse", null, Shown(WoodbridgeHouse().M)),
                ("Cottage", null, Shown(Cottage(9f, Cream, RedOxide, false, 1).M)),
                ("CottageGable", null, Shown(Cottage(9.5f, Sage, IronRoof, true, 2).M)),
                ("WardenCottage", null, Shown(Cottage(8f, RoseBrick, IronRoof, false, 3, true).M)),
                ("Deli", null, Shown(Deli(10f).M)),
                ("Gum", null, Make(m => Gum(m, Vector3.zero, 13f, 4))),
                ("RiverRedGum", null, Make(m => Gum(m, Vector3.zero, 16f, 7, false, BarkRed))),
                ("Jacaranda", null, Make(m => JacarandaTree(m, Vector3.zero, 7f, 2))),
                ("Footbridge", null, Shown(Make(m => Footbridge(m, 30f)))),
                ("PlatformShelter", null, Make(m => PlatformShelter(m, V(0, 0, 0), 1))),
                ("PowerPole", null, Make(m => PowerPole(m, Vector3.zero, 1))),
                ("Train", null, Make(m => Train(m, 0f, 0f, 1, true, true))),
                ("Gantry", null, Make(m => Gantry(m, 0f, BedX0 + .5f, BedX1 - .3f, T1, T2))),
                ("FishingJetty", null, Make(m => FishingJetty(m, V(0, 0, 0)))),
                ("PicnicShelter", null, Make(m => PicnicShelter(m, Vector3.zero))),
                ("GatePillars", null, Shown(Make(m => GatePillars(m, Vector3.zero)))),
                ("StreetLamp", null, Make(m => StreetLamp(m, Vector3.zero, 1))),
                ("WheelieBin", bin.gameObject, null),
                ("PathBollard", bollard.gameObject, null),
                ("GrapeCrates", crates.gameObject, null),
                ("MazeGate", gate.gameObject, null),
                ("Duck", duck, null),
                ("Ibis", ibis, null),
                ("BlackSwan", swan, null),
                ("BoardwalkKicker", kicker.gameObject, null),
            };
            K.RenderAtlas("art-src/districts/woodbridge/props-atlas.png", items);
        }

        // ---------------------------------------------------------------- Woodbridge surfaces and sides

        static void WoodStreet(CqChunkDraft d) => K.StreetSurface(d.Road, d.Length, K.R.asphalt, Paving, KerbCap, true);

        /// <summary>The shared path (PedestrianMall): red asphalt with white edge lines and a dashed centre line.</summary>
        static void SharedPath(CqChunkDraft d, float skipFrom = -1f, float skipTo = -1f)
        {
            d.Type = RoadType.PedestrianMall;
            AsphaltStreetSurface(d.Road, d.Length, PathRed, Paving, KerbCap, skipFrom, skipTo);
            ComicMesh m = d.Road;
            for (float z = 1f; z < d.Length - 2f; z += 5f)
            {
                if (skipFrom >= 0f && z + 2.6f > skipFrom && z < skipTo) continue;
                Stripe(m, -.07f, .07f, z, z + 2.6f, .03f, White);
            }
            for (float z = 0f; z < d.Length - .01f; z += 4f)
            {
                if (skipFrom >= 0f && z + 4f > skipFrom && z < skipTo) continue;
                foreach (int s in new[] { -1, 1 }) Stripe(m, s * (K.Half - .4f), s * (K.Half - .28f), z, z + 4f, .025f, White);
            }
        }

        /// <summary>The railway side of a Woodbridge chunk: tracks, fence, wires, a gantry at z = 20, the rendered noise
        /// wall with painted panels behind the tracks, back gardens, roofs and gums beyond.</summary>
        static void RailSide(CqChunkDraft d, float z0, float z1, int seed, bool wall = true, float skipFrom = -1f, float skipTo = -1f, bool gantry = true)
        {
            RailBed(d, z0, z1, TwoTracks, BedX1, true, Color.Lerp(DryGrass, Lawn, .5f), skipFrom, skipTo);
            Wires(d, z0, z1, T1, T2);
            if (gantry && z0 <= 20f && z1 >= 20f) Gantry(d.SlopedFrontage, 20f, BedX0 + .5f, BedX1 - .3f, T1, T2);
            SideWall(d, 1, z0, z1);
            if (wall) NoiseWall(d.Road, z0, z1, seed, skipFrom, skipTo);
            BackRow(d, 1, z0, z1, seed, 18f);
        }

        /// <summary>A rendered noise wall behind the tracks (road mesh): panels between piers, a few painted murals.</summary>
        static void NoiseWall(ComicMesh m, float z0, float z1, int seed, float skipFrom = -1f, float skipTo = -1f)
        {
            var r = new System.Random(seed * 31 + 9);
            Color[] paint = { MuralGreen, MuralTeal, K.Hex("E2A23A"), K.Hex("C8553A"), K.Hex("5B6FB0"), MuralLight };
            const float x = BedX1 + 1.2f;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                if (skipFrom >= 0f && e > skipFrom && z < skipTo) continue;
                m.Surface = ComicSurface.Concrete;
                K.MinMaxBox(m, V(x, BedY - .6f, z), V(x + .25f, BedY + 2.5f, e), K.Hex("D9D2C2"));
                K.MinMaxBox(m, V(x - .08f, BedY - .6f, z), V(x + .33f, BedY + 2.7f, z + .3f), K.Hex("C4BCAA"));
                if (r.NextDouble() < .55)
                {
                    m.Surface = ComicSurface.Plain;
                    Color c = paint[r.Next(paint.Length)];
                    float a = z + .5f, b = e - .2f, h = F(r, 1.2f, 2.2f);
                    K.Face(m, V(x - .01f, BedY + .1f, a), V(x - .01f, BedY + .1f, b), V(x - .01f, BedY + h, b), V(x - .01f, BedY + h, a), c, Vector3.left, false);
                    K.Face(m, V(x - .02f, BedY + .3f, a + .5f), V(x - .02f, BedY + .3f, a + 1.6f), V(x - .02f, BedY + h * .7f, a + 1.9f), V(x - .02f, BedY + h * .8f, a + .7f), Color.Lerp(c, Color.white, .4f), Vector3.left, false);
                }
            }
        }

        /// <summary>Second row beyond a side (no outline, no shadows): roofs between gums.</summary>
        static void BackRow(CqChunkDraft d, int s, float z0, float z1, int seed, float from, bool houses = true)
        {
            var r = new System.Random(seed * 17 + (s > 0 ? 3 : 5));
            Color[] walls = { Cream, White, Butter, PaleBlue, K.Hex("D9CBB0"), Sage };
            Color[] roofs = { RedOxide, IronRoof, GreenRoof, K.Hex("8A8F94"), IronRoofDark };
            for (float z = z0 + 2f; z < z1 - 3f; z += F(r, 9f, 13f))
            {
                float x = s * (K.Edge + from + F(r, 2f, 9f));
                if (houses) FarHouse(d.Deep, V(x, d.Ground(z), z), F(r, 8f, 11f), F(r, 3f, 4.2f), walls[r.Next(walls.Length)], roofs[r.Next(roofs.Length)]);
                Gum(d.Deep, V(x + s * F(r, 7f, 14f), d.Ground(z + 4f) - .3f, z + F(r, 3f, 6f)), F(r, 11f, 17f), seed * 5 + (int)z, true, r.NextDouble() < .4 ? BarkRed : (Color?)null);
            }
        }

        /// <summary>The land side of a street chunk: lawn out to the back row.</summary>
        static void LeftLawn(CqChunkDraft d, float z0, float z1, Color? c = null)
        {
            Ground(d, -1, z0, z1, 0f, 60f, K.Kerb + .02f, c ?? Lawn, ComicSurface.Foliage);
        }

        /// <summary>The Swan on the left: lawn to the top of the bank, the bank sloping to the water (reeds at the edge),
        /// the green-brown river out to the far scenery's.</summary>
        static void RiverSide(CqChunkDraft d, float z0, float z1, float bank, int seed, bool reeds = true)
        {
            ComicMesh m = d.Road;
            Ground(d, -1, z0, z1, 0f, bank - 5f, K.Kerb + .02f, Lawn, ComicSurface.Foliage);
            for (float z = z0 + 1f; z < z1 - 3f; z += 7f)
                Stripe(m, -(K.Edge + bank - 5.5f), -(K.Edge + .4f), z, z + 3f, K.Kerb + .03f, LawnLight);
            float xa = -(K.Edge + bank - 5f), xb = -(K.Edge + bank + 3f);
            Terrain(m, xa, xb, z0, z1, 2f, 4f,
                (x, z) => Mathf.Lerp(K.Kerb + .02f, River - .5f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(xa, xb, x))) + .08f * Mathf.Sin(z * .6f + x),
                (x, z, y) => y < River + .45f ? Mud : Color.Lerp(Lawn, DryGrass, Mathf.InverseLerp(K.Kerb, River, y) * .7f), ComicSurface.Foliage);
            K.Water(m, -(K.Edge + bank + 1.5f), -(K.Edge + 62f), z0, z1, River, RiverGreen, RiverLight, seed);
            SideWall(d, -1, z0, z1, bank + 40f);
            if (!reeds) return;
            var r = new System.Random(seed * 13 + 1);
            for (float z = z0 + 1.5f; z < z1 - 1f; z += F(r, 2.2f, 4.5f))
            {
                Vector3 p = V(-(K.Edge + bank + F(r, .6f, 1.8f)), River - .1f, z);
                m.Surface = ComicSurface.Foliage;
                m.Cylinder(p + V(0, .7f, 0), F(r, .5f, .9f), 1.4f, r.NextDouble() < .5 ? Reed : Color.Lerp(Reed, DryGrass, .5f), 5, .15f);
            }
        }

        /// <summary>Timber power poles on the verge of the land side (at the given z), their three lines from z0 to z1.</summary>
        static void Poles(CqChunkDraft d, float z0, float z1, params float[] at)
        {
            const float x = -(K.Edge + .12f);
            foreach (float z in at) PowerPole(d.Props, V(x, d.Ground(z), z), 1);
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Metal;
            for (float z = z0; z < z1 - .01f; z += 8f)
                foreach (float dz in new[] { -1.05f, 0f, 1.05f })
                    K.MinMaxBox(m, V(x + dz - .02f, K.Kerb + 8.7f, z), V(x + dz + .02f, K.Kerb + 8.74f, Mathf.Min(z1, z + 8f)), Charcoal);
        }

        static void LowBrickWall(CqChunkDraft d, int s, float z0, float z1, float h)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Brick;
            K.SideBox(m, s, z0, z1, -1f, K.Kerb + h, .08f, .5f, Brick);
            m.Surface = ComicSurface.Concrete;
            K.SideBox(m, s, z0, z1, K.Kerb + h, K.Kerb + h + .1f, .02f, .56f, Stone);
        }

        /// <summary>A post-and-rail timber fence along z (flat road space).</summary>
        static void PostAndRail(ComicMesh m, int s, float z0, float z1, float from)
        {
            m.Surface = ComicSurface.Wood;
            float x = s * (K.Edge + from);
            for (float z = z0 + .2f; z < z1; z += 2.4f) K.MinMaxBox(m, V(x - .08f, K.Kerb - .3f, z - .08f), V(x + .08f, K.Kerb + 1.15f, z + .08f), TimberDark);
            for (float z = z0; z < z1 - .01f; z += 4f)
                foreach (float y in new[] { .55f, .95f }) K.MinMaxBox(m, V(x - .04f, K.Kerb + y - .06f, z), V(x + .04f, K.Kerb + y + .06f, Mathf.Min(z1, z + 4f)), Timber);
        }

        /// <summary>A white picket fence along z (flat road space), with gate gaps.</summary>
        static void PicketFence(ComicMesh m, int s, float z0, float z1, float from, params float[] gates)
        {
            float x = s * (K.Edge + from);
            bool Gate(float z) => gates.Any(g => Mathf.Abs(z - g) < .75f);
            m.Surface = ComicSurface.Wood;
            for (float z = z0; z < z1 - .01f; z += 2f)
            {
                float e = Mathf.Min(z1, z + 2f);
                if (Gate(z + 1f)) continue;
                foreach (float y in new[] { .3f, .8f }) K.MinMaxBox(m, V(x - .03f, K.Kerb + y - .04f, z), V(x + .03f, K.Kerb + y + .04f, e), White);
                K.MinMaxBox(m, V(x - .07f, K.Kerb - .2f, z - .07f), V(x + .07f, K.Kerb + 1.15f, z + .07f), White);
            }
            Vector3 n = V(-s, 0, 0);
            for (float z = z0 + .2f; z < z1 - .1f; z += .25f)
            {
                if (Gate(z)) continue;
                float xf = x - s * .04f;
                K.Face(m, V(xf, K.Kerb + .1f, z - .05f), V(xf, K.Kerb + .1f, z + .05f), V(xf, K.Kerb + 1f, z + .05f), V(xf, K.Kerb + 1f, z - .05f), White, n, false);
            }
        }

        static void CalmEdge(CqChunkDraft d, int s, float z0, float z1, int seed)
        {
            LowBrickWall(d, s, z0, z1, .5f);
            HedgeRow(d.SlopedFrontage, s, z0 + .3f, z1 - .3f, .7f, 1.9f, 1.05f);
            var r = new System.Random(seed);
            if (s < 0)
                for (float z = z0 + 3.5f; z < z1 - 1f; z += 7f)
                    Gum(d.Props, V(s * (K.Edge + F(r, 4f, 7f)), d.Ground(z), z), F(r, 9f, 12f), seed + (int)z);
            SideWall(d, s, z0, z1);
        }

        // ---------------------------------------------------------------- Woodbridge chunks

        static CqChunkDraft WoodCottages()
        {
            // A gum-lined road past three railway workers' weatherboard cottages (picket fences, jacaranda in flower).
            var d = new CqChunkDraft("Woodbridge_Cottages", WoodDrop);
            WoodStreet(d);
            RailSide(d, 0, 40, 331);
            AddSheared(d, d.Frontage, m => Train(m, T2, 2f, 2, true, false));
            LeftLawn(d, 0, 40);
            CottageRow(d, 0f, 1);
            foreach (float z in new[] { 6f, 26f })
            {
                Vector3 p = V(-(K.Edge - .8f), d.Ground(z), z);
                K.TreePit(d.Props, p, 1.4f);
                Gum(d.Props, p + V(0, .1f, 0), 12f, 40 + (int)z);
                K.Thin(d.Collisions, "Gum", p, 4f);
            }
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(14f), 14f), -1);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(34f), 34f), -1);
            Poles(d, 0f, 40f, 2f, 22f);
            BackRow(d, -1, 0, 40, 332, 22f);
            d.StandardSlots();
            return d;
        }

        /// <summary>Three cottages behind picket fences from z0 (each about 9 m wide, 5.5 m back from the sidewalk).</summary>
        static void CottageRow(CqChunkDraft d, float z0, int seed, int count = 3)
        {
            Color[] walls = { Cream, Sage, PaleBlue, Butter, PalePink };
            Color[] roofs = { RedOxide, IronRoof, GreenRoof, IronRoofDark };
            var gates = new List<float>();
            for (int i = 0; i < count; i++)
            {
                float z = z0 + 2f + i * 12.6f, w = i % 2 == 0 ? 9f : 9.5f;
                PutLeft(d, d.Frontage, Cottage(w, walls[(seed + i) % walls.Length], roofs[(seed + i * 3) % roofs.Length], i % 2 == 1, seed + i), z, w, 5.5f, 9f, 5f);
                gates.Add(z + (i % 2 == 1 ? 6.6f : w * .5f));
                // the garden path from the gate to the verandah steps, and a shrub or two
                Stripe(d.Road, -(K.Edge + 3.4f), -(K.Edge + .1f), gates[i] - .5f, gates[i] + .5f, K.Kerb + .035f, Paving);
                Shrub(d.Frontage, V(-(K.Edge + 2.2f), d.Ground(z + 1.2f), z + 1.2f), 1.1f, i % 2 == 0 ? K.Hex("5E8F3F") : K.Hex("7A9A4A"));
            }
            PicketFence(d.SlopedFrontage, -1, z0, z0 + count * 12.6f + 1f, .35f, gates.ToArray());
            JacarandaTree(d.Props, V(-(K.Edge + 2.6f), d.Ground(z0 + 12.8f), z0 + 12.8f), 6.5f, seed);
            // fallen purple flowers on the lawn and the footpath
            var r = new System.Random(seed * 3 + 1);
            for (int i = 0; i < 16; i++)
            {
                float x = -(K.Edge + 2.6f) + F(r, -3.2f, 3.2f), z = z0 + 12.8f + F(r, -3.2f, 3.2f), s = F(r, .12f, .26f);
                Stripe(d.Road, x - s, x + s, z - s, z + s, K.Kerb + .045f, i % 2 == 0 ? Jacaranda : JacarandaLight);
            }
        }

        static CqChunkDraft WoodStation()
        {
            // Woodbridge station: two side platforms with small shelters, a train in, the footbridge with its long ramp
            // behind green mural walls; a corner deli and a cottage on the land side.
            var d = new CqChunkDraft("Woodbridge_Station", WoodDrop);
            WoodStreet(d);
            RailSide(d, 0, 40, 333, false, -1f, -1f, false);
            ComicMesh s = d.SlopedFrontage;
            Platform(s, BedX0 + .1f, T1 - 1.55f, 3f, 37f, true);
            Platform(s, T2 + 1.55f, T2 + 4.4f, 3f, 37f, false);
            PlatformShelter(s, V(BedX0 + .9f, BedY + 1f, 12f), 1);
            PlatformShelter(s, V(T2 + 3.3f, BedY + 1f, 16f), -1);
            Mast(s, 6f, T2 + 4.9f, T2);
            Mast(s, 6f, BedX0 - .25f, T1);
            Train(s, T1, 7f, 1, true, true);
            Footbridge(s, 30f);
            NameBoard(d.Props, d.Deep, V(K.Edge + .5f, d.Ground(3.5f), 3.5f), "WOODBRIDGE");
            // land side: the corner deli, a cottage, kerbside parking bays
            LeftLawn(d, 0, 40);
            Ground(d, -1, 1f, 15f, 0f, 3.2f, K.Kerb + .03f, Paving, ComicSurface.Paving);
            PutLeft(d, d.Frontage, Deli(11f), 2.5f, 11f, 3.2f, 9f, 5.5f);
            PutLeft(d, d.Frontage, Cottage(9f, Butter, IronRoof, true, 9), 27f, 9f, 5.5f, 9f, 5f);
            PicketFence(d.SlopedFrontage, -1, 25f, 38f, .35f, 33.6f);
            Stripe(d.Road, -(K.Edge + 3.4f), -(K.Edge + .1f), 33.1f, 34.1f, K.Kerb + .035f, Paving);
            Gum(d.Props, V(-(K.Edge + 2.2f), d.Ground(19.5f), 19.5f), 14f, 51);
            Gum(d.Props, V(-(K.Edge + 12f), d.Ground(22f), 22f), 17f, 52, false, BarkRed);
            Bench(d.Props, V(-(K.Edge + .8f), d.Ground(16.5f), 16.5f), -90f);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(22f), 22f), -1);
            Poles(d, 0f, 40f, 2f, 22f);
            BackRow(d, -1, 0, 40, 334, 22f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft WoodHouse()
        {
            // Woodbridge House on its lawns above the river: the gate pillars and the drive, the house turned towards the
            // rider (tower, twin gables, verandahs), the warden's cottage behind, old gums, the Swan beyond.
            var d = new CqChunkDraft("Woodbridge_House", WoodDrop);
            WoodStreet(d);
            RailSide(d, 0, 40, 335);
            RiverSide(d, 0, 40, 44f, 336, false);
            LowBrickWall(d, -1, 0f, 9.5f, .6f);
            LowBrickWall(d, -1, 14.5f, 40f, .6f);
            GatePillars(d.Frontage, V(-(K.Edge + .3f), d.Ground(12f), 12f));
            // the gravel drive sweeping up to the entrance
            Color gravel = K.Hex("D8C7A0");
            Stripe(d.Road, -(K.Edge + 5f), -(K.Edge + .05f), 10.2f, 13.8f, K.Kerb + .035f, gravel);
            Stripe(d.Road, -(K.Edge + 13f), -(K.Edge + 5f), 9f, 15f, K.Kerb + .035f, gravel);
            Stripe(d.Road, -(K.Edge + 15f), -(K.Edge + 8f), 13f, 30f, K.Kerb + .035f, gravel);
            Vector3 hp = V(-(K.Edge + 21.5f), d.Ground(16f), 3f);
            Put(d.Landmark, WoodbridgeHouse(), hp, -55f, 1.12f);
            d.Walls.Add(new Bounds(V(-(K.Edge + 24f), d.Y(18f) + 3f, 18f), V(26f, 10f, 34f)));
            NameBoardLeft(d, V(-(K.Edge + 1.6f), d.Ground(6.5f), 6.5f), "WOODBRIDGE");
            Gum(d.Props, V(-(K.Edge + 12f), d.Ground(37f), 37f), 18f, 62);
            JacarandaTree(d.Props, V(-(K.Edge + 5f), d.Ground(36.5f), 36.5f), 6f, 6);
            FanPalm(d.Props, V(-(K.Edge + 15.5f), d.Ground(34f), 34f), 13f);
            Gum(d.Deep, V(-(K.Edge + 40f), d.Ground(8f) - 1f, 2f), 19f, 64, true, BarkRed);
            Gum(d.Deep, V(-(K.Edge + 41f), d.Ground(36f) - 1f, 37f), 17f, 65, true);
            Bench(d.Props, V(-(K.Edge + 4f), d.Ground(27f), 27f), -90f);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(8f), 8f), -1);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(30f), 30f), -1);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft WoodRiverside()
        {
            // The shared path runs along the river: lawn, the bank and reeds, a fishing jetty, a picnic shelter, old
            // river gums leaning over the water; a train passes on the right.
            var d = new CqChunkDraft("Woodbridge_Riverside", WoodDrop);
            SharedPath(d);
            RailSide(d, 0, 40, 337);
            AddSheared(d, d.Frontage, m => Train(m, T1, -6f, 2, false, true));
            RiverSide(d, 0, 40, 17f, 338);
            ComicMesh f = d.SlopedFrontage;
            FishingJetty(f, V(-(K.Edge + 15.5f), K.Kerb - .5f, 21f));
            PicnicShelter(d.Frontage, V(-(K.Edge + 6f), d.Ground(8f), 8f));
            Bench(d.Props, V(-(K.Edge + 1.2f), d.Ground(18f), 18f), -90f);
            Bench(d.Props, V(-(K.Edge + 1.2f), d.Ground(32f), 32f), -90f);
            Gum(d.Props, V(-(K.Edge + 9.5f), d.Ground(15f), 15f), 17f, 71, false, BarkRed);
            Gum(d.Props, V(-(K.Edge + 4f), d.Ground(27f), 27f), 12f, 72);
            Gum(d.Props, V(-(K.Edge + 10.5f), d.Ground(36f), 36f), 15f, 73, false, BarkRed);
            Gum(d.Props, V(-(K.Edge + 3f), d.Ground(2f), 2f), 13f, 74);
            // a path sign and the lamps of the shared path
            PathLamp(d.Props, V(K.Edge - .4f, d.Ground(10f), 10f));
            PathLamp(d.Props, V(K.Edge - .4f, d.Ground(30f), 30f));
            // birds on the water (decoration)
            AddSheared(d, d.Deep, m =>
            {
                var sw = BlackSwanMesh();
                m.Add(sw, V(-(K.Edge + 27f), River - .25f, 12f), Quaternion.Euler(0, 110f, 0), Vector3.one * 1.3f);
                m.Add(sw, V(-(K.Edge + 31f), River - .25f, 15f), Quaternion.Euler(0, 80f, 0), Vector3.one * 1.3f);
                m.Add(DuckMesh(), V(-(K.Edge + 22f), River - .12f, 31f), Quaternion.Euler(0, 200f, 0), Vector3.one * 1.3f);
            });
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft WoodCreek()
        {
            // Blackadder Creek runs into the Swan here: the path crosses it on a timber boardwalk, up a kicker at the end
            // of the deck, 6 m of open creek across the whole width (SPLASH!), then the far abutment. The railway crosses
            // on a steel girder bridge.
            var d = new CqChunkDraft("Woodbridge_Creek", WoodDrop);
            float ramp = CreekRampStart, lip = ramp + CreekRise + CreekTop, far = lip + CreekGap, c0 = 5f;
            SharedPath(d, c0, far);
            BoardwalkDeck(d, c0, ramp);
            ComicMesh m = d.Road;
            K.Water(m, -(K.Edge + 62f), K.Edge + 60f, c0, far, River, RiverGreen, RiverLight, 339);
            Abutment(m, c0, true);
            Abutment(m, far, false);
            RailSide(d, 0, 40, 340, true, c0, far, false);
            // the railway's girder bridge over the creek
            ComicMesh g = d.SlopedFrontage;
            g.Surface = ComicSurface.PaintedMetal;
            foreach (float t in TwoTracks)
            {
                K.MinMaxBox(g, V(t - 1.5f, BedY - 1.1f, c0 - .4f), V(t - 1.2f, BedY + .5f, far + .4f), SteelDark);
                K.MinMaxBox(g, V(t + 1.2f, BedY - 1.1f, c0 - .4f), V(t + 1.5f, BedY + .5f, far + .4f), SteelDark);
                K.MinMaxBox(g, V(t - 1.2f, BedY - .25f, c0 - .4f), V(t + 1.2f, BedY - .02f, far + .4f), Charcoal);
            }
            g.Surface = ComicSurface.Concrete;
            foreach (float z in new[] { c0 - .7f, far + .7f }) K.MinMaxBox(g, V(BedX0 - .3f, River - 1f, z - .5f), V(BedX1 + .3f, BedY - .25f, z + .5f), ConcreteDark);
            Gantry(g, 33f, BedX0 + .5f, BedX1 - .3f, T1, T2);
            RiverSide(d, 0, c0, 17f, 341);
            RiverSide(d, far, 40, 17f, 342);
            var spec = new CqRampSpec { Name = "BoardwalkKicker", X = 0f, Z = ramp, Width = 2f * K.Edge, Rise = CreekRise, Height = CreekHeight, Top = CreekTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            d.Gaps.Add(new CqGapSpec { Name = "BlackadderCreek", Z = lip, Length = CreekGap, Width = 0f, Word = "SPLASH", Water = true });
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, DeckLight, Deck, false, River - .3f);
            foreach (int s in new[] { -1, 1 })
            {
                deck.Surface = ComicSurface.Wood;
                float x = s * (K.Edge + .12f);
                deck.Beam(V(x, 1.05f, ramp), V(x, CreekHeight + 1.05f, ramp + CreekRise), .12f, Timber);
                deck.Beam(V(x, CreekHeight + 1.05f, ramp + CreekRise), V(x, CreekHeight + 1.05f, lip - .2f), .12f, Timber);
                for (float z = ramp; z <= lip; z += 3f)
                {
                    float h = CreekHeight * Mathf.Clamp01((z - ramp) / CreekRise);
                    K.MinMaxBox(deck, V(x - .12f, River - .6f, z - .12f), V(x + .12f, h + 1.1f, z + .12f), Deck);
                }
            }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            d.KeepOut.Add(KeepOutBox(d, -1, ramp, far)); d.KeepOut.Add(KeepOutBox(d, 1, ramp, far));
            // paperbarks and gums along the creek, a warning sign before the deck
            Gum(d.Props, V(-(K.Edge + 4f), d.Ground(2f), 2f), 12f, 81);
            Gum(d.Props, V(-(K.Edge + 6f), d.Ground(29f), 29f), 15f, 82, false, BarkRed);
            Gum(d.Props, V(-(K.Edge + 3f), d.Ground(36f), 36f), 11f, 83);
            Gum(d.Deep, V(-(K.Edge + 30f), d.Y(12f) + River, 12f), 12f, 84, true);
            PathLamp(d.Props, V(K.Edge - .4f, d.Ground(30f), 30f));
            d.GiftSlots.Add(new Vector2(0f, 31f));
            d.Slot(-2.6f, 36f); d.Slot(2.6f, 36f);
            d.Slot(-2.6f, 2f); d.Slot(2.6f, 2f);
            return d;
        }

        static CqChunkDraft WoodEntry()
        {
            // Calm at z = 0 (low brick wall, hedge, gums), then the railway fence shows on the right and the first
            // cottages on the left.
            var d = new CqChunkDraft("Woodbridge_Entry", WoodDrop);
            WoodStreet(d);
            RailSide(d, 0, 40, 343);
            HedgeRow(d.SlopedFrontage, 1, 0f, 13f, .12f, .8f, 1.15f);
            LeftLawn(d, 0, 40);
            CalmEdge(d, -1, 0f, 14f, 3431);
            Poles(d, 16f, 40f, 22f);
            PostAndRail(d.SlopedFrontage, -1, 14f, 16f, .35f);
            CottageRow(d, 14.6f, 3, 2);
            Gum(d.Props, V(-(K.Edge + 2.4f), d.Ground(38f), 38f), 13f, 91);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(24f), 24f), -1);
            BackRow(d, -1, 0, 40, 344, 22f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft WoodExit()
        {
            // The riverside park ends: a last cottage and gums, then calm towards z = 40 (brick wall, hedge, small trees).
            var d = new CqChunkDraft("Woodbridge_Exit", WoodDrop);
            WoodStreet(d);
            RailSide(d, 0, 40, 345);
            HedgeRow(d.SlopedFrontage, 1, 27f, 40f, .12f, .8f, 1.15f);
            LeftLawn(d, 0, 40);
            CottageRow(d, 0f, 2, 2);
            Poles(d, 0f, 26f, 2f, 22f);
            CalmEdge(d, -1, 26f, 40f, 3451);
            StreetLamp(d.Props, V(K.Half + .55f, d.Ground(12f), 12f), -1);
            BackRow(d, -1, 0, 40, 346, 22f);
            d.StandardSlots();
            return d;
        }

        // ---------------------------------------------------------------- Woodbridge buildings

        /// <summary>A railway worker's cottage: weatherboards on a dark base, corner boards, a corrugated-iron roof with
        /// ink eaves (gableFront: a projecting gabled room with a finial and a vent), a verandah on timber posts with
        /// brackets, a panelled door between sash windows, a brick chimney, a rainwater tank at the end the rider sees.
        /// brick: the warden's cottage at Woodbridge House (rose brick, no boards).</summary>
        static Fr Cottage(float W, Color wall, Color roof, bool gableFront, int seed, bool brick = false)
        {
            var f = new Fr();
            const float D = 8.5f, eave = 3.5f;
            Color shade = Color.Lerp(wall, Ink, .16f), door = new[] { K.Hex("2F5E4E"), K.Hex("8A3B30"), K.Hex("2E4E7E"), K.Hex("6B4A2E") }[seed % 4];
            float g = gableFront ? 4.2f : 0f;
            f.S = ComicSurface.Concrete;
            f.Box(0, W, -2.4f, .45f, 0, D, brick ? Stone : K.Hex("6E6257"));
            f.S = brick ? ComicSurface.Brick : ComicSurface.Wood;
            f.Box(0, W, .45f, eave, 0, D, wall);
            if (gableFront) f.Box(0, g, .45f, eave, -1.4f, 0, wall);
            if (!brick)
            {
                f.S = ComicSurface.Plain;
                for (float y = .9f; y < eave - .1f; y += .5f)
                {
                    f.Q(g, W, y, y + .04f, -.012f, shade, false);
                    if (gableFront) f.Q(0, g, y, y + .04f, -1.412f, shade, false);
                    f.EndQ(-.012f, gableFront ? -1.4f : 0f, D, y, y + .04f, shade, true, false);
                }
                f.S = ComicSurface.Wood;
                f.Box(-.06f, .1f, .45f, eave, gableFront ? -1.46f : -.06f, gableFront ? -1.3f : .1f, White);
                f.Box(W - .1f, W + .06f, .45f, eave, -.06f, .1f, White);
            }
            // roof
            f.S = ComicSurface.PaintedMetal;
            f.Box(-.42f, W + .42f, eave - .08f, eave + .02f, -.36f, D + .36f, Ink);
            f.S = ComicSurface.Metal;
            f.RoofX(-.4f, W + .4f, eave, eave + 2.3f, -.34f, D + .34f, roof);
            f.Box(-.46f, W + .46f, eave + 2.24f, eave + 2.38f, D * .5f - .14f, D * .5f + .14f, Color.Lerp(roof, Color.white, .25f));
            f.Box(-.44f, W + .44f, eave - .02f, eave + .1f, -.46f, -.34f, White);
            f.S = ComicSurface.Plain;
            for (float x = (gableFront ? 5f : .5f); x < W + .2f; x += 1.1f)
                K.Face(f.M, V(x - .03f, eave + .06f, -.3f), V(x + .03f, eave + .06f, -.3f), V(x + .03f, eave + 2.3f, D * .5f - .14f), V(x - .03f, eave + 2.3f, D * .5f - .14f), Color.Lerp(roof, Ink, .22f), V(0, 1, -1), false);
            f.S = ComicSurface.Metal;
            if (gableFront)
            {
                f.S = ComicSurface.PaintedMetal;
                f.Box(-.4f, g + .4f, eave - .08f, eave + .02f, -1.76f, 0f, Ink);
                f.S = ComicSurface.Metal;
                f.RoofZ(-.4f, g + .4f, eave, eave + 1.9f, -1.74f, D * .5f, roof, wall);
                f.S = ComicSurface.Wood;
                f.M.Beam(V(-.4f, eave + .02f, -1.78f), V(g * .5f, eave + 1.95f, -1.78f), .14f, White);
                f.M.Beam(V(g + .4f, eave + .02f, -1.78f), V(g * .5f, eave + 1.95f, -1.78f), .14f, White);
                f.Box(g * .5f - .06f, g * .5f + .06f, eave + 1.7f, eave + 2.6f, -1.82f, -1.7f, White);
                f.S = ComicSurface.PaintedMetal;
                f.Q(g * .5f - .3f, g * .5f + .3f, eave + .5f, eave + 1.1f, -1.76f, Ink);
                f.Win(g * .5f, 1.25f, 2.75f, 1.2f, -1.4f, White, true, true);
            }
            // the verandah
            float v0 = g, vz = -2.3f;
            f.S = ComicSurface.Wood;
            f.Box(v0, W, .28f, .45f, vz, 0, TimberDark);
            f.S = ComicSurface.Metal;
            f.LeanTo(v0 - (gableFront ? 0f : .25f), W + .25f, vz - .2f, 0f, 2.72f, 3.3f, .09f, Color.Lerp(roof, Color.white, .12f), Color.Lerp(roof, Ink, .35f));
            f.S = ComicSurface.Wood;
            f.Box(v0, W + .2f, 2.6f, 2.74f, vz - .08f, vz + .06f, White);
            int posts = W - v0 > 6f ? 4 : 3;
            for (int i = 0; i < posts; i++)
            {
                float x = Mathf.Lerp(v0 + .12f, W - .02f, (float)i / (posts - 1));
                f.Box(x - .07f, x + .07f, .45f, 2.6f, vz - .07f, vz + .07f, White);
                foreach (int s in new[] { -1, 1 })
                {
                    if ((i == 0 && s < 0) || (i == posts - 1 && s > 0)) continue;
                    K.TriFacing(f.M, V(x + s * .07f, 2.6f, vz - .02f), V(x + s * .55f, 2.6f, vz - .02f), V(x + s * .07f, 2.12f, vz - .02f), White, Vector3.back);
                }
            }
            f.S = ComicSurface.Concrete;
            float dc = v0 + (W - v0) * .5f;
            f.Box(dc - .8f, dc + .8f, -.3f, .16f, vz - .7f, vz, Concrete);
            f.Box(dc - .8f, dc + .8f, .16f, .3f, vz - .35f, vz, Concrete);
            f.Door(dc, .45f, 2.6f, 1f, 0f, door);
            Color trim = brick ? Stone : White;
            if (W - v0 > 6f) { f.Win(dc - 2.3f, 1.2f, 2.6f, 1.1f, 0f, trim, true, true); f.Win(dc + 2.3f, 1.2f, 2.6f, 1.1f, 0f, trim, true, true); }
            else f.Win(dc + 1.8f, 1.2f, 2.6f, 1f, 0f, trim, true, true);
            // the end the rider sees: a window, the tank
            f.S = ComicSurface.PaintedMetal;
            f.EndQ(-.02f, D * .45f, D * .45f + 1.1f, 1.3f, 2.6f, Ink, true);
            f.S = ComicSurface.Glass;
            f.EndQ(-.035f, D * .45f + .09f, D * .45f + 1.01f, 1.39f, 2.51f, Glass, true, false);
            f.S = ComicSurface.Wood;
            f.Box(-1.9f, -.3f, 0f, .7f, D * .72f, D * .72f + 1.6f, TimberDark);
            f.S = ComicSurface.Metal;
            f.M.Cylinder(V(-1.1f, 1.55f, D * .72f + .8f), .85f, 1.7f, Zinc, 8);
            f.M.Cylinder(V(-1.1f, 2.48f, D * .72f + .8f), .88f, .16f, SteelDark, 8, .6f);
            // chimney
            f.S = ComicSurface.Brick;
            f.Box(W * .68f, W * .68f + .75f, eave + .6f, eave + 3.4f, D * .5f, D * .5f + .75f, Brick);
            f.S = ComicSurface.Concrete;
            f.Box(W * .68f - .08f, W * .68f + .83f, eave + 3.4f, eave + 3.58f, D * .5f - .08f, D * .5f + .83f, Stone);
            return f;
        }

        /// <summary>The corner deli: a painted-brick shop with a stepped parapet, a deep awning on posts over the
        /// forecourt, shopfront glazing either side of a recessed door, plain sign boxes, an ice chest and a bench.</summary>
        static Fr Deli(float W)
        {
            var f = new Fr();
            const float D = 9f, h = 4.4f;
            Color wall = K.Hex("EAD9B0"), pier = Color.Lerp(wall, Ink, .14f), awn = K.Hex("2F6FB0");
            f.S = ComicSurface.Concrete;
            f.Box(0, W, -2.4f, .15f, 0, D, Ink);
            f.S = ComicSurface.Plaster;
            f.Box(0, W, .15f, h, 0, D, wall);
            foreach (float x in new[] { 0f, W * .5f - .7f, W * .5f + .3f, W - .4f }) f.Box(x, x + .4f, .15f, 3.3f, -.12f, 0, pier);
            f.Shopfront(.5f, W * .5f - .8f, .2f, 3.1f, 0f);
            f.Shopfront(W * .5f + .8f, W - .5f, .2f, 3.1f, 0f);
            f.Door(W * .5f, .15f, 2.9f, 1.2f, 0f, K.Hex("2E4E7E"));
            // parapet: cornice, a stepped centre with a plain panel, end piers
            f.S = ComicSurface.Concrete;
            f.Box(-.12f, W + .12f, h, h + .25f, -.25f, D + .1f, Stone);
            f.S = ComicSurface.Plaster;
            f.Box(0, W, h + .25f, h + 1f, -.06f, .4f, wall);
            f.Box(W * .28f, W * .72f, h + 1f, h + 1.6f, -.06f, .4f, wall);
            f.S = ComicSurface.Concrete;
            f.Box(W * .28f - .1f, W * .72f + .1f, h + 1.6f, h + 1.78f, -.16f, .46f, Stone);
            f.Box(-.1f, W * .28f, h + 1f, h + 1.14f, -.16f, .46f, Stone);
            f.Box(W * .72f, W + .1f, h + 1f, h + 1.14f, -.16f, .46f, Stone);
            f.S = ComicSurface.PaintedMetal;
            f.Q(W * .32f, W * .68f, h + .4f, h + 1.4f, -.08f, K.Hex("C8372D"));
            f.Q(W * .36f, W * .64f, h + .75f, h + 1.05f, -.1f, Cream, false);
            // the awning over the forecourt: striped iron, ink fascia, posts
            f.S = ComicSurface.Metal;
            int stripes = 8;
            for (int i = 0; i < stripes; i++)
                f.LeanTo(-.2f + (W + .4f) * i / stripes, -.2f + (W + .4f) * (i + 1) / stripes, -2.9f, 0f, 3.2f, 3.55f, .08f, i % 2 == 0 ? awn : White, Color.Lerp(awn, Ink, .4f));
            f.S = ComicSurface.PaintedMetal;
            f.Box(-.22f, W + .22f, 3.02f, 3.22f, -2.96f, -2.82f, Ink);
            foreach (float x in new[] { .1f, W * .5f, W - .1f }) f.Box(x - .06f, x + .06f, .15f, 3.05f, -2.92f, -2.8f, Ink);
            // an ice chest and a paper stand by the door
            f.S = ComicSurface.Metal;
            f.Box(W * .5f + 1.1f, W * .5f + 2.5f, .15f, 1.2f, -.9f, -.1f, White);
            f.Q(W * .5f + 1.2f, W * .5f + 2.4f, .5f, 1.05f, -.92f, GlassLight);
            f.S = ComicSurface.Plastic;
            f.Box(W * .5f - 2.1f, W * .5f - 1.3f, .15f, 1f, -.6f, -.1f, Yellow);
            // the end the rider sees: a painted panel (no letters), a side window
            f.S = ComicSurface.Plain;
            f.EndQ(-.02f, 1f, D - 1.4f, 1.6f, 3.6f, K.Hex("C8372D"), true);
            f.EndQ(-.03f, 1.4f, D - 1.8f, 2.3f, 2.9f, Cream, true, false);
            f.S = ComicSurface.Metal;
            f.Box(W * .2f, W * .45f, h + .25f, h + 1.2f, 3.5f, 5.6f, SteelDark);
            return f;
        }

        /// <summary>
        /// Woodbridge House (1883-85, National Trust), after the Commons photographs: rose-red brick on a cream plinth,
        /// tall sash windows with cream sills and heads; the square three-storey tower with its ochre top, cornice and
        /// corner finials; two steep gables with corbel bands beside it; the cream balustraded entrance porch; the
        /// two-storey verandah of the west wing and the single-storey one of the east wing in brown iron lace (diamond
        /// balustrade, fringe valance, curved brackets); blue-grey corrugated-iron hipped roofs and corbelled chimneys.
        /// Front towards -Z, 24 m across.
        /// </summary>
        static Fr WoodbridgeHouse()
        {
            var f = new Fr();
            const float W = 24f, D = 11f, g0 = .7f, f1 = 4.1f, eave = 7.3f;
            Color brick = RoseBrick, roof = IronRoof;
            // ---- the body: plinth, two storeys, string course, eaves
            f.S = ComicSurface.Concrete;
            f.Box(-.1f, W + .1f, -3f, g0, -.1f, D + .1f, Stone);
            f.S = ComicSurface.Brick;
            f.Box(0, W, g0, eave, 0, D, brick);
            f.S = ComicSurface.Concrete;
            f.Box(-.06f, W + .06f, f1 - .12f, f1 + .1f, -.08f, D + .06f, StoneShade);
            f.Box(-.3f, W + .3f, eave - .1f, eave + .12f, -.3f, D + .3f, Stone);
            f.S = ComicSurface.Metal;
            f.Hip(-.35f, W + .35f, -.35f, D + .35f, eave + .12f, eave + 3.4f, roof);
            // ---- west wing (x 0..7): the two-storey lace verandah along the front and round the end
            foreach (float x in new[] { 1.8f, 5.2f })
            {
                f.Win(x, g0 + .5f, f1 - .7f, 1.15f, 0f, Stone, true, true);
                f.Win(x, f1 + .6f, eave - .8f, 1.1f, 0f, Stone, true, true);
            }
            Verandah(f, -2.5f, 7f, -2.6f, g0, f1, eave - .75f, true);
            // the end the rider sees first: windows on both floors behind the verandah's return
            for (int k = 0; k < 2; k++)
            {
                float y0 = k == 0 ? g0 + .5f : f1 + .6f, y1 = k == 0 ? f1 - .7f : eave - .8f;
                foreach (float z in new[] { 2.2f, 6.6f })
                {
                    f.S = ComicSurface.PaintedMetal;
                    f.EndQ(-.02f, z, z + 1.15f, y0, y1, Ink, true);
                    f.S = ComicSurface.Glass;
                    f.EndQ(-.035f, z + .09f, z + 1.06f, y0 + .09f, y1 - .09f, Glass, true, false);
                    f.S = ComicSurface.Concrete;
                    f.Box(-.16f, 0f, y0 - .16f, y0, z - .12f, z + 1.27f, Stone);
                }
            }
            // ---- the tower (x 7..10.6), standing forward of the front
            const float tx0 = 7f, tx1 = 10.6f, tz = -1f, tTop = 11.6f, tCap = 14.2f;
            f.S = ComicSurface.Brick;
            f.Box(tx0, tx1, g0, tTop, tz, 2.6f, brick);
            f.S = ComicSurface.Concrete;
            f.Box(tx0 - .08f, tx1 + .08f, -3f, g0, tz - .08f, 2.6f, Stone);
            foreach (float y in new[] { f1 - .1f, eave - .1f, tTop - .25f }) f.Box(tx0 - .1f, tx1 + .1f, y, y + .22f, tz - .1f, 2.7f, StoneShade);
            f.Box(tx0 - .05f, tx1 + .05f, tTop, tCap, tz - .05f, 2.65f, Ochre);
            f.Box(tx0 - .3f, tx1 + .3f, tCap, tCap + .3f, tz - .3f, 2.9f, Stone);
            f.Box(tx0 - .12f, tx1 + .12f, tCap + .3f, tCap + .75f, tz - .12f, 2.72f, Ochre);
            foreach (float x in new[] { tx0 + .05f, tx1 - .05f })
                foreach (float z in new[] { tz + .05f, 2.55f })
                {
                    f.Box(x - .28f, x + .28f, tCap + .3f, tCap + 1.25f, z - .28f, z + .28f, Stone);
                    f.M.Cylinder(V(x, tCap + 1.55f, z), .2f, .6f, Ochre, 4, .1f, Quaternion.Euler(0, 45, 0));
                }
            float tc = (tx0 + tx1) * .5f;
            f.ArchWin(tc, g0 + .1f, g0 + 2.2f, 1.3f, tz, Stone, false, 1, GlassDark);     // the arched entrance in the tower's foot
            f.Win(tc, f1 + .7f, eave - .9f, .9f, tz, Stone, true, true, 1);
            f.Win(tc, eave + 1.1f, eave + 2.9f, .9f, tz, Stone, true, true, 1);
            foreach (float x in new[] { tc - .65f, tc + .65f }) f.Win(x, tTop + .7f, tTop + 2f, .6f, tz - .05f, Stone, true, false, 1, false);
            for (int k = 0; k < 2; k++)
            {
                float y0 = k == 0 ? f1 + .7f : eave + 1.1f, y1 = k == 0 ? eave - .9f : eave + 2.9f;
                f.S = ComicSurface.PaintedMetal;
                f.EndQ(tx0 - .02f, .3f, 1.2f, y0, y1, Ink, true);
                f.S = ComicSurface.Glass;
                f.EndQ(tx0 - .035f, .39f, 1.11f, y0 + .09f, y1 - .09f, Glass, true, false);
            }
            f.S = ComicSurface.PaintedMetal;
            f.EndQ(tx0 - .07f, .2f, .8f, tTop + .7f, tTop + 2f, Ink, true);
            f.EndQ(tx0 - .07f, 1f, 1.6f, tTop + .7f, tTop + 2f, Ink, true);
            // the cream entrance porch against the tower, with its balustrade
            f.S = ComicSurface.Concrete;
            float px0 = tx0 - 1.6f, px1 = tx1 + .5f, pz = tz - 2.2f;
            f.Box(px0, px1, g0 - .2f, g0, pz - .4f, tz, Stone);
            f.Box(px0 - .1f, px1 + .1f, 3.5f, 3.95f, pz - .1f, tz, Stone);
            foreach (float x in new[] { px0 + .2f, px1 - .2f }) { f.Box(x - .2f, x + .2f, g0, 3.5f, pz, pz + .4f, Stone); f.Box(x - .26f, x + .26f, 3.2f, 3.5f, pz - .06f, pz + .46f, StoneShade); }
            f.Box(px0, px1, 4.75f, 4.9f, pz - .05f, pz + .15f, Stone);
            f.Box(px0, px0 + .15f, 4.75f, 4.9f, pz, tz, Stone);
            for (float x = px0 + .15f; x < px1; x += .42f) f.Box(x - .07f, x + .07f, 3.95f, 4.75f, pz - .02f, pz + .12f, Stone);
            for (float z = pz + .4f; z < tz; z += .42f) f.Box(px0, px0 + .14f, 3.95f, 4.75f, z - .07f, z + .07f, Stone);
            // ---- the two gables (x 10.6..14.2 standing forward, x 14.2..17.4 flush), corbel bands under them
            f.S = ComicSurface.Brick;
            f.Box(10.6f, 14.2f, g0, eave + .6f, -.5f, 0f, brick);
            f.RoofZ(10.45f, 14.35f, eave + .6f, eave + 4f, -.55f, D * .5f, roof, brick);
            f.RoofZ(14.05f, 17.55f, eave + .1f, eave + 3.1f, -.06f, D * .5f, roof, brick);
            f.S = ComicSurface.Concrete;
            foreach ((float a, float b, float y, float z) in new[] { (10.5f, 14.3f, eave + .5f, -.6f), (14.1f, 17.5f, eave, -.1f) })
            {
                f.Box(a, b, y, y + .16f, z, z + .2f, StoneShade);
                f.Box(a + .25f, b - .25f, y - .3f, y - .16f, z + .03f, z + .2f, BrickDark);
                f.M.Cylinder(V((a + b) * .5f, y + (a < 12f ? 3.9f : 3.5f), z + .1f), .16f, .9f, Ochre, 4, .15f, Quaternion.Euler(0, 45, 0));
            }
            f.Win(12.4f, g0 + .5f, f1 - .7f, 1.2f, -.5f, Stone, true, true);
            f.Win(12.4f, f1 + .6f, eave - .5f, 1.15f, -.5f, Stone, true, true);
            f.Win(12.4f, eave + 1.2f, eave + 2.2f, .6f, -.5f, Stone, true, false, 1, false);
            f.Win(15.8f, g0 + .5f, f1 - .7f, 1.2f, 0f, Stone, true, true);
            f.Win(15.8f, f1 + .6f, eave - .7f, 1.15f, 0f, Stone, true, true);
            f.Win(15.8f, eave + .8f, eave + 1.7f, .6f, 0f, Stone, true, false, 1, false);
            // ---- east wing (x 17.4..24): the single-storey verandah, upstairs windows over its roof
            foreach (float x in new[] { 19.2f, 22.2f })
            {
                f.Win(x, g0 + .5f, f1 - .8f, 1.2f, 0f, Stone, true, true);
                f.Win(x, f1 + .9f, eave - .7f, 1.1f, 0f, Stone, true, true);
            }
            Verandah(f, 10.6f, W + .2f, -3f, g0, f1, 0f, false);
            // the far end (seen as you pass): windows
            for (int k = 0; k < 2; k++)
                foreach (float z in new[] { 2.5f, 7f })
                {
                    float y0 = k == 0 ? g0 + .5f : f1 + .8f, y1 = k == 0 ? f1 - .8f : eave - .7f;
                    f.S = ComicSurface.PaintedMetal;
                    f.EndQ(W + .02f, z, z + 1.15f, y0, y1, Ink, false);
                    f.S = ComicSurface.Glass;
                    f.EndQ(W + .035f, z + .09f, z + 1.06f, y0 + .09f, y1 - .09f, Glass, false, false);
                }
            // ---- chimneys: brick shafts with corbelled cream caps
            foreach ((float x, float z) in new[] { (3.2f, 6f), (13f, 7.5f), (20.5f, 5.5f) })
            {
                f.S = ComicSurface.Brick;
                f.Box(x - .5f, x + .5f, eave + 1.2f, eave + 5.1f, z - .4f, z + .4f, brick);
                f.S = ComicSurface.Concrete;
                f.Box(x - .62f, x + .62f, eave + 4.5f, eave + 4.7f, z - .52f, z + .52f, StoneShade);
                f.Box(x - .62f, x + .62f, eave + 5.1f, eave + 5.3f, z - .52f, z + .52f, Stone);
                f.S = ComicSurface.Brick;
                foreach (float dx in new[] { -.25f, .25f }) f.M.Cylinder(V(x + dx, eave + 5.6f, z), .16f, .6f, K.Hex("C97A55"), 6, .8f);
            }
            return f;
        }

        /// <summary>An iron-lace verandah on the front of the frame from x0 to x1, out to zOut: a timber floor on the
        /// plinth, slim brown posts, curved brackets, a fringe valance; two storeys (upper deck, a balustrade of
        /// diamonds between rails, a roof under the eaves at yRoof) or one (a lean-to roof at the first floor).
        /// A two-storey verandah also returns along the frame's left end.</summary>
        static void Verandah(Fr f, float x0, float x1, float zOut, float g0, float f1, float yRoof, bool two)
        {
            Color post = VerandahBrown, lace = LaceBrown, roofC = IronRoof;
            float front = x0 < 0f ? 0f : x0;                 // where the front run starts (a negative x0 = it wraps the left end)
            f.S = ComicSurface.Wood;
            f.Box(x0, x1, g0 - .18f, g0, zOut, 0f, TimberDark);
            float top = two ? yRoof : f1 - .05f;
            if (two)
            {
                f.Box(x0, x1, f1 - .16f, f1, zOut, 0f, post);
                if (x0 < 0f) f.Box(x0, 0f, f1 - .16f, f1, 0f, 9f, post);
                if (x0 < 0f) f.Box(x0, 0f, g0 - .18f, g0, 0f, 9f, TimberDark);
            }
            f.S = ComicSurface.Metal;
            f.LeanTo(x0 - .2f, x1 + .2f, zOut - .25f, 0f, top, top + .55f, .1f, roofC, Color.Lerp(roofC, Ink, .4f));
            if (two && x0 < 0f)
            {
                Vector3 a = V(x0 - .2f, top, zOut - .25f), b = V(x0 - .2f, top, 9.2f), c = V(0f, top + .55f, 9.2f), d = V(0f, top + .55f, 0f), t = V(0, .1f, 0);
                K.Face(f.M, a + t, b + t, c + t, d + t, roofC, V(-.3f, 1, 0));
                K.Face(f.M, a, b, b + t, a + t, roofC, Vector3.left);
            }
            f.S = ComicSurface.PaintedMetal;
            int n = Mathf.Max(2, Mathf.RoundToInt((x1 - x0) / 2.3f));
            for (int i = 0; i <= n; i++)
            {
                float x = Mathf.Lerp(x0 + .08f, x1 - .08f, (float)i / n);
                f.Box(x - .07f, x + .07f, g0, top, zOut - .07f, zOut + .07f, post);
                foreach (int s in new[] { -1, 1 })
                {
                    if ((i == 0 && s < 0) || (i == n && s > 0)) continue;
                    foreach (float y in two ? new[] { f1 - .16f, top } : new[] { top })
                    {
                        K.TriFacing(f.M, V(x + s * .07f, y, zOut), V(x + s * .7f, y, zOut), V(x + s * .07f, y - .6f, zOut), lace, Vector3.back);
                        K.TriFacing(f.M, V(x + s * .07f, y, zOut), V(x + s * .7f, y, zOut), V(x + s * .07f, y - .6f, zOut), lace, Vector3.forward);
                    }
                }
            }
            // the fringe valance under each beam, the beam itself
            foreach (float y in two ? new[] { f1 - .16f, top } : new[] { top })
            {
                f.Box(x0, x1, y - .12f, y, zOut - .06f, zOut + .06f, post);
                for (float x = x0 + .35f; x < x1 - .3f; x += .4f)
                    K.TriFacing(f.M, V(x - .16f, y - .12f, zOut - .01f), V(x + .16f, y - .12f, zOut - .01f), V(x, y - .42f, zOut - .01f), lace, Vector3.back);
            }
            if (!two) return;
            // the upper balustrade: rails and a row of lace diamonds, along the front and the left return
            f.Box(x0, x1, f1 + .95f, f1 + 1.03f, zOut - .05f, zOut + .05f, post);
            f.Box(x0, x1, f1 + .08f, f1 + .14f, zOut - .04f, zOut + .04f, post);
            for (float x = x0 + .3f; x < x1 - .1f; x += .46f) Diamond(f.M, V(x, f1 + .54f, zOut - .01f), .2f, .38f, lace, Vector3.back);
            if (x0 >= 0f) return;
            f.Box(x0 - .05f, x0 + .05f, f1 + .95f, f1 + 1.03f, zOut, 9f, post);
            f.Box(x0 - .05f, x0 + .05f, top - .12f, top, zOut, 9f, post);
            for (float z = zOut + .3f; z < 9f; z += .46f) Diamond(f.M, V(x0 - .01f, f1 + .54f, z), .2f, .38f, lace, Vector3.left);
            for (float z = 1.2f; z < 9.2f; z += 2.6f)
            {
                f.Box(x0 - .07f, x0 + .07f, g0, top, z - .07f, z + .07f, post);
                K.TriFacing(f.M, V(x0, top - .12f, z - .07f), V(x0, top - .12f, z - .7f), V(x0, top - .72f, z - .07f), lace, Vector3.left);
            }
        }

        /// <summary>A flat diamond (lace panel) on a vertical plane facing <paramref name="n"/> (-Z or -X).</summary>
        static void Diamond(ComicMesh m, Vector3 c, float w, float h, Color col, Vector3 n)
        {
            Vector3 side = Mathf.Abs(n.x) > .5f ? Vector3.forward : Vector3.right;
            K.Face(m, c - side * w, c - Vector3.up * h, c + side * w, c + Vector3.up * h, col, n, false);
        }

        /// <summary>The entrance: two brick gate pillars with cream caps and ball finials, an open iron gate leaf each.</summary>
        static void GatePillars(ComicMesh m, Vector3 p)
        {
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 c = p + V(0, 0, s * 2.2f);
                m.Surface = ComicSurface.Brick;
                m.Box(c + V(0, 1.1f, 0), V(.8f, 2.2f, .8f), RoseBrick);
                m.Surface = ComicSurface.Concrete;
                m.Box(c + V(0, .15f, 0), V(.95f, .3f, .95f), Stone);
                m.Box(c + V(0, 2.3f, 0), V(1f, .2f, 1f), Stone);
                Blob(m, c + V(0, 2.7f, 0), V(.6f, .6f, .6f), Stone, 6, 3);
                m.Surface = ComicSurface.PaintedMetal;
                Quaternion q = Quaternion.Euler(0, s * 62f, 0);
                Vector3 hinge = c + V(-.1f, 0, -s * .4f);
                m.Box(hinge + q * V(0, 1.75f, -s * .9f), V(.06f, .08f, 1.8f), Charcoal, q);
                m.Box(hinge + q * V(0, .3f, -s * .9f), V(.06f, .08f, 1.8f), Charcoal, q);
                for (float t = .15f; t < 1.8f; t += .3f) m.Box(hinge + q * V(0, 1.05f, -s * t), V(.04f, 1.6f, .04f), Charcoal, q);
            }
        }

        /// <summary>A heritage name board beside the footpath on the left, facing the rider: dark green with cream letters.</summary>
        static void NameBoardLeft(CqChunkDraft d, Vector3 p, string name)
        {
            const float px = .09f;
            float w = PixelWidth(name, px) + .7f;
            ComicMesh m = d.Props;
            m.Surface = ComicSurface.Wood;
            foreach (float x in new[] { -w * .42f, w * .42f }) m.Box(p + V(x, 1f, .06f), V(.14f, 2f, .14f), White);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(0, 1.55f, 0), V(w, .9f, .08f), SignGreen);
            m.Box(p + V(0, 2.04f, 0), V(w + .12f, .1f, .12f), White);
            PixelText(d.Deep, name, p.x, p.y + 1.62f, p.z - .05f, px, Cream);
            m.Surface = ComicSurface.Plain;
            m.Box(p + V(0, 1.22f, -.045f), V(w * .6f, .04f, .01f), Cream);
        }

        /// <summary>A tall fan palm (the old palms beside the house): ringed trunk, a round head of stiff fronds.</summary>
        static void FanPalm(ComicMesh m, Vector3 p, float h)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .06f, 0), .5f, .12f, Ink, 6, .9f);
            m.Surface = ComicSurface.Wood;
            m.Cylinder(p + V(0, h * .5f, 0), .3f, h, K.Hex("8F7A60"), 6, .7f);
            m.Cylinder(p + V(0, h - .9f, 0), .45f, 1.6f, K.Hex("7A6548"), 6, 1.5f);
            m.Surface = ComicSurface.Foliage;
            Blob(m, p + V(0, h + .5f, 0), V(4.2f, 2.6f, 4.2f), K.Hex("5F8A45"), 7, 3);
            Blob(m, p + V(0, h + 1f, 0), V(2.8f, 2f, 2.8f), K.Hex("7BA455"), 6, 3);
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f;
                Vector3 o = V(Mathf.Cos(a), 0, Mathf.Sin(a));
                m.Beam(p + V(0, h + .2f, 0) + o * 1.4f, p + V(0, h - 1.2f, 0) + o * 2.5f, .28f, K.Hex("4E7A3B"));
            }
        }

        // ---------------------------------------------------------------- Woodbridge station parts (flat road space)

        /// <summary>A side platform from x0 to x1 (its track edge on the side `edgeLow` says): concrete face, asphalt top,
        /// a white edge line and a yellow tactile strip, ramps at both ends.</summary>
        static void Platform(ComicMesh m, float x0, float x1, float z0, float z1, bool edgeAtX1, bool rampStart = true, bool rampEnd = true)
        {
            float top = BedY + 1f;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f);
                m.Surface = ComicSurface.Concrete;
                K.MinMaxBox(m, V(x0, BedY - .3f, z), V(x1, top, e), Concrete);
                m.Surface = ComicSurface.Plain;
                float xe = edgeAtX1 ? x1 : x0, sg = edgeAtX1 ? -1f : 1f;
                K.Face(m, V(xe + sg * .5f, top + .012f, z), V(xe + sg * .5f, top + .012f, e), V(xe + sg * 2.4f, top + .012f, e), V(xe + sg * 2.4f, top + .012f, z), K.Hex("6A6E73"), Vector3.up, false);
                K.Face(m, V(xe + sg * .5f, top + .014f, z), V(xe + sg * .5f, top + .014f, e), V(xe + sg * .9f, top + .014f, e), V(xe + sg * .9f, top + .014f, z), Yellow, Vector3.up, false);
                K.Face(m, V(xe, top + .014f, z), V(xe, top + .014f, e), V(xe + sg * .14f, top + .014f, e), V(xe + sg * .14f, top + .014f, z), White, Vector3.up, false);
            }
            m.Surface = ComicSurface.Concrete;
            foreach ((float za, float zb, bool on) in new[] { (z0 - 3f, z0, rampStart), (z1 + 3f, z1, rampEnd) })
            {
                if (!on) continue;
                Vector3 a = V(x0, BedY - .1f, za), b = V(x1, BedY - .1f, za), c = V(x1, top, zb), d = V(x0, top, zb);
                K.Face(m, a, b, c, d, ConcreteDark, Vector3.up);
                K.TriFacing(m, V(x0, BedY - .3f, zb), a, d, Concrete, Vector3.left);
                K.TriFacing(m, V(x1, BedY - .3f, zb), b, c, Concrete, Vector3.right);
            }
        }

        /// <summary>A small platform shelter: steel posts, a cantilevered skillion roof with an ink edge, a glass back
        /// screen, a bench, a green bin; `open` = +1 opens towards +X (the track), -1 towards -X.</summary>
        static void PlatformShelter(ComicMesh m, Vector3 p, int open)
        {
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { -3f, 0f, 3f }) m.Box(p + V(-open * .5f, 1.4f, z), V(.14f, 2.8f, .14f), SteelDark);
            Vector3 a = p + V(-open * .9f, 2.75f, -3.6f), b = p + V(-open * .9f, 2.75f, 3.6f), c = p + V(open * 1.6f, 3.2f, 3.6f), d = p + V(open * 1.6f, 3.2f, -3.6f), t = V(0, .12f, 0);
            m.Surface = ComicSurface.Metal;
            K.Face(m, a + t, b + t, c + t, d + t, White, Vector3.up);
            K.Face(m, a, b, c, d, K.Hex("C9D0D6"), Vector3.down);
            K.Face(m, c, d, d + t, c + t, Ink, V(open, 0, 0));
            K.Face(m, a, b, b + t, a + t, Ink, V(-open, 0, 0));
            K.Face(m, a, d, d + t, a + t, Ink, Vector3.back);
            K.Face(m, b, c, c + t, b + t, Ink, Vector3.forward);
            m.Surface = ComicSurface.Glass;
            m.Box(p + V(-open * .5f, 1.5f, 0), V(.05f, 1.7f, 5.6f), GlassLight);
            m.Surface = ComicSurface.Metal;
            m.Box(p + V(-open * .1f, .45f, -.8f), V(.5f, .08f, 2.6f), Steel);
            m.Box(p + V(-open * .1f, .22f, -1.8f), V(.4f, .44f, .08f), SteelDark);
            m.Box(p + V(-open * .1f, .22f, .2f), V(.4f, .44f, .08f), SteelDark);
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(p + V(-open * .1f, .5f, 2.2f), V(.45f, 1f, .45f), TrainGreen);
            m.Box(p + V(-open * .1f, 1.04f, 2.2f), V(.5f, .08f, .5f), Ink);
        }

        /// <summary>Woodbridge's footbridge (after the Commons photographs of December 2022): a caged deck over the
        /// tracks at z, a lift and stair tower beside the road, and on the far side the long ramp between walls painted
        /// with the green mural (leaves, a bird), yellow handrails, mesh above the walls.</summary>
        static void Footbridge(ComicMesh m, float z)
        {
            const float deckY = BedY + 6.9f, x0 = FenceX - .3f, x1 = T2 + 7.6f;
            // deck and cage
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(x0, deckY - .35f, z - 1.3f), V(x1, deckY, z + 1.3f), Concrete);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float dz in new[] { -1.3f, 1.3f })
            {
                K.MinMaxBox(m, V(x0, deckY, z + dz - .05f), V(x1, deckY + 1.1f, z + dz + .05f), SteelDark);
                K.MinMaxBox(m, V(x0, deckY + 2.3f, z + dz - .05f), V(x1, deckY + 2.42f, z + dz + .05f), SteelDark);
                for (float x = x0 + .4f; x < x1; x += 1.6f) K.MinMaxBox(m, V(x - .04f, deckY + 1.1f, z + dz - .04f), V(x + .04f, deckY + 2.3f, z + dz + .04f), SteelDark);
            }
            m.Surface = ComicSurface.Concrete;
            foreach (float x in new[] { T1 + 2.5f, T2 + 4.7f }) K.MinMaxBox(m, V(x - .35f, BedY - .3f, z - .5f), V(x + .35f, deckY - .35f, z + .5f), ConcreteDark);
            // the lift and stair tower beside the road
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(x0 - .1f, K.Kerb - .3f, z - 1.5f), V(x0 + 1.9f, deckY + 3.2f, z + 1.5f), K.Hex("D9D4C8"));
            m.Surface = ComicSurface.Glass;
            K.Face(m, V(x0 - .12f, K.Kerb + .4f, z - 1f), V(x0 - .12f, K.Kerb + .4f, z + 1f), V(x0 - .12f, deckY + 2.6f, z + 1f), V(x0 - .12f, deckY + 2.6f, z - 1f), GlassLight, Vector3.left);
            K.Face(m, V(x0 + .2f, K.Kerb + .4f, z - 1.52f), V(x0 + 1.6f, K.Kerb + .4f, z - 1.52f), V(x0 + 1.6f, deckY + 2.6f, z - 1.52f), V(x0 + .2f, deckY + 2.6f, z - 1.52f), GlassLight, Vector3.back);
            m.Surface = ComicSurface.PaintedMetal;
            K.MinMaxBox(m, V(x0 - .25f, deckY + 3.2f, z - 1.7f), V(x0 + 2.1f, deckY + 3.4f, z + 1.7f), Ink);
            foreach (float y in new[] { 2.4f, 4.6f }) K.MinMaxBox(m, V(x0 - .14f, K.Kerb + y, z - 1.05f), V(x0 - .1f, K.Kerb + y + .08f, z + 1.05f), Ink);
            K.MinMaxBox(m, V(x0 - .16f, K.Kerb + .05f, z - .5f), V(x0 - .1f, K.Kerb + 2.1f, z + .5f), TrainGreen);
            // the far ramp: a concrete ramp from the ground at z - 26 up to the deck, between mural walls
            float ra = z - 27f, rb = z - 1.3f, rx0 = x1 - 2.5f, rx1 = x1;
            for (int i = 0; i < 6; i++)
            {
                float za = Mathf.Lerp(ra, rb, i / 6f), zb = Mathf.Lerp(ra, rb, (i + 1) / 6f);
                float ya = Mathf.Lerp(BedY - .1f, deckY, i / 6f), yb = Mathf.Lerp(BedY - .1f, deckY, (i + 1) / 6f);
                m.Surface = ComicSurface.Concrete;
                K.Face(m, V(rx0, ya, za), V(rx1, ya, za), V(rx1, yb, zb), V(rx0, yb, zb), Concrete, Vector3.up);
                // the wall facing the tracks and the road: green mural, a darker base, lighter leaf shapes
                Vector3 n = Vector3.left;
                float xw = rx0 - .02f;
                m.Surface = ComicSurface.Plain;
                K.Face(m, V(xw, BedY - .4f, za), V(xw, BedY - .4f, zb), V(xw, yb + 1.3f, zb), V(xw, ya + 1.3f, za), i % 2 == 0 ? MuralGreen : Color.Lerp(MuralGreen, MuralTeal, .45f), n);
                K.Face(m, V(xw - .01f, ya * .45f + .3f, za + .5f), V(xw - .01f, yb * .45f + .1f, zb - .4f), V(xw - .01f, yb + .9f, zb - 1.2f), V(xw - .01f, ya + 1.1f, za + 1.2f), i % 2 == 0 ? MuralLight : MuralDark, n, false);
                K.Face(m, V(xw - .015f, ya + .2f, za + 1.4f), V(xw - .015f, ya + .35f, za + 2.6f), V(xw - .015f, ya + 1f, za + 2.9f), V(xw - .015f, ya + .9f, za + 1.7f), i % 3 == 0 ? Yellow : MuralTeal, n, false);
                m.Surface = ComicSurface.PaintedMetal;
                m.Beam(V(xw - .05f, ya + 1.75f, za), V(xw - .05f, yb + 1.75f, zb), .07f, Yellow);
                m.Beam(V(xw, ya + 2.9f, za), V(xw, yb + 2.9f, zb), .07f, SteelDark);
                K.MinMaxBox(m, V(xw - .04f, ya + 1.3f, za - .04f), V(xw + .04f, ya + 2.9f, za + .04f), SteelDark);
            }
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(rx0, BedY - .4f, rb - .5f), V(rx1, deckY - .35f, z + 1.3f), ConcreteDark);
        }

        /// <summary>A lamp of the shared path: slim charcoal post, a flat head towards the path.</summary>
        static void PathLamp(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .1f, 0), .16f, .2f, Ink, 6);
            m.Cylinder(p + V(0, 2.5f, 0), .07f, 5f, Charcoal, 6);
            m.Box(p + V(-.45f, 4.95f, 0), V(1f, .1f, .3f), Charcoal);
            m.Surface = ComicSurface.Plastic;
            m.Box(p + V(-.7f, 4.88f, 0), V(.5f, .05f, .22f), Cream);
        }

        /// <summary>A fishing jetty running out over the river towards -X from p: timber deck on tall weathered piles
        /// (some standing free, as at the house's old jetty), a rail on one side.</summary>
        static void FishingJetty(ComicMesh m, Vector3 p)
        {
            Color grey = K.Hex("A39C8E"), greyDark = K.Hex("857E72");
            m.Surface = ComicSurface.Wood;
            K.MinMaxBox(m, p + V(-9f, .1f, -1f), p + V(2.5f, .28f, 1f), DeckLight);
            m.Surface = ComicSurface.Plain;
            for (float x = -8.6f; x < 2.4f; x += .5f) K.Face(m, p + V(x, .29f, -1f), p + V(x, .29f, 1f), p + V(x + .05f, .29f, 1f), p + V(x + .05f, .29f, -1f), Deck, Vector3.up, false);
            m.Surface = ComicSurface.Wood;
            for (float x = -8.5f; x <= .5f; x += 3f)
                foreach (float z in new[] { -1.1f, 1.1f }) m.Cylinder(p + V(x, -.6f, z), .16f, 3.2f, x < -8f ? greyDark : grey, 6);
            foreach (float z in new[] { -2.6f, 2.5f, 3.7f }) m.Cylinder(p + V(-9.6f - Mathf.Abs(z) * .3f, -.7f, z), .17f, 3f, greyDark, 6);
            m.Box(p + V(-3.5f, 1.25f, 1.05f), V(11f, .1f, .1f), grey);
            for (float x = -8.5f; x <= 2f; x += 2.1f) m.Box(p + V(x, .75f, 1.05f), V(.1f, 1f, .1f), grey);
        }

        /// <summary>Picnic shelter: timber posts on a pad, a corrugated hipped roof with ink eaves, a table and benches.</summary>
        static void PicnicShelter(ComicMesh f, Vector3 p)
        {
            f.Surface = ComicSurface.Concrete;
            f.Box(p + V(0, .06f, 0), V(5.4f, .12f, 4.4f), K.Hex("D9D2C3"));
            f.Surface = ComicSurface.Wood;
            foreach (float x in new[] { -2.3f, 2.3f }) foreach (float z in new[] { -1.8f, 1.8f }) f.Box(p + V(x, 1.35f, z), V(.16f, 2.5f, .16f), TimberDark);
            f.Surface = ComicSurface.PaintedMetal;
            f.Box(p + V(0, 2.66f, 0), V(5.5f, .14f, 4.5f), Ink);
            f.Surface = ComicSurface.Metal;
            f.Cylinder(p + V(0, 3.25f, 0), 3.8f, 1.1f, GreenRoof, 4, .12f, Quaternion.Euler(0, 45, 0));
            f.Surface = ComicSurface.Wood;
            f.Box(p + V(0, .8f, 0), V(2.2f, .08f, .9f), Timber);
            foreach (float z in new[] { -.8f, .8f }) f.Box(p + V(0, .48f, z), V(2.2f, .07f, .32f), Timber);
            f.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.9f, .9f }) { f.Box(p + V(x, .42f, 0), V(.08f, .8f, .5f), Ink); f.Box(p + V(x, .24f, 0), V(.08f, .08f, 1.8f), Ink); }
        }

        /// <summary>The boardwalk: timber deck with the street's cross-section (path at 0, walkways at the kerb height),
        /// board joints, fascia, piles, a post-and-rail balustrade.</summary>
        static void BoardwalkDeck(CqChunkDraft d, float z0, float z1)
        {
            ComicMesh m = d.Road;
            Color board = Color.Lerp(Deck, Ink, .3f), walk = Color.Lerp(DeckLight, K.R.cream, .15f);
            for (float z = z0; z < z1 - .01f; z += 1f)
            {
                float e = Mathf.Min(z1, z + 1f);
                m.Surface = ComicSurface.Wood;
                K.Face(m, V(-K.Half, 0f, z), V(-K.Half, 0f, e), V(K.Half, 0f, e), V(K.Half, 0f, z), DeckLight, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    K.Face(m, V(s * K.Half, K.Kerb, z), V(s * K.Half, K.Kerb, e), V(s * K.Edge, K.Kerb, e), V(s * K.Edge, K.Kerb, z), walk, Vector3.up, false);
                    K.Face(m, V(s * K.Half, -.05f, z), V(s * K.Half, -.05f, e), V(s * K.Half, K.Kerb, e), V(s * K.Half, K.Kerb, z), Deck, V(-s, 0, 0), false);
                    K.Face(m, V(s * K.Edge, -.6f, z), V(s * K.Edge, -.6f, e), V(s * K.Edge, K.Kerb, e), V(s * K.Edge, K.Kerb, z), Deck, V(s, 0, 0), false);
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
                    p.Cylinder(V(x, (River - .8f - .6f) * .5f, z), .2f, -.6f - (River - .8f), Deck, 6);
            for (int s = -1; s <= 1; s += 2)
            {
                p.Box(V(s * (K.Edge + .12f), K.Kerb + .95f, (z0 + z1) * .5f), V(.12f, .12f, z1 - z0), Timber);
                p.Box(V(s * (K.Edge + .12f), K.Kerb + .5f, (z0 + z1) * .5f), V(.08f, .08f, z1 - z0), Timber);
                for (float z = z0 + .3f; z < z1; z += 1.8f) p.Box(V(s * (K.Edge + .12f), K.Kerb + .45f, z), V(.16f, 1.2f, .16f), Deck);
            }
        }

        /// <summary>The creek's retaining wall across the whole chunk width at z (limestone blocks, a cream cap flush
        /// with the footpaths); its face looks out over the water.</summary>
        static void Abutment(ComicMesh m, float z, bool facingForward)
        {
            Color stone = K.Hex("D9C9A0"), cap = K.Hex("EDE2C4");
            float t = facingForward ? -.5f : .5f;
            m.Surface = ComicSurface.Concrete;
            K.MinMaxBox(m, V(-(K.Edge + 20f), River - 1f, z), V(K.Edge + 60f, -.03f, z + t), stone);
            K.MinMaxBox(m, V(-(K.Edge + 20f), -.03f, z), V(-K.Half, K.Kerb + .02f, z + t), cap);
            K.MinMaxBox(m, V(K.Half, -.03f, z), V(K.Edge + 60f, K.Kerb + .02f, z + t), cap);
        }

        // ---------------------------------------------------------------- Woodbridge obstacles and animals

        static ComicMesh WheelieBinMesh(Color body, Color lid)
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            m.Box(V(0, .52f, 0), V(.6f, .92f, .66f), body);
            m.Box(V(0, .22f, 0), V(.62f, .06f, .68f), Color.Lerp(body, Ink, .3f));
            m.Box(V(0, 1.02f, .02f), V(.66f, .1f, .74f), lid);
            m.Box(V(0, 1.08f, -.36f), V(.5f, .06f, .08f), Color.Lerp(lid, Ink, .3f));
            m.Box(V(0, .9f, .38f), V(.5f, .08f, .1f), Color.Lerp(body, Ink, .3f));
            m.Surface = ComicSurface.Rubber;
            foreach (float x in new[] { -.32f, .32f }) m.Cylinder(V(x, .13f, .3f), .13f, .08f, Ink, 8, 1f, Quaternion.Euler(0, 0, 90));
            return m;
        }

        // A shared-path bollard: yellow post on an ink base with white reflective bands and a rounded cap.
        static ComicMesh PathBollardMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .05f, 0), .24f, .1f, Ink, 8);
            m.Cylinder(V(0, .5f, 0), .13f, .86f, Yellow, 8);
            m.Cylinder(V(0, .7f, 0), .14f, .1f, White, 8);
            m.Cylinder(V(0, .5f, 0), .14f, .1f, White, 8);
            m.Cylinder(V(0, .97f, 0), .14f, .1f, Ink, 8, .6f);
            return m;
        }

        // Swan Valley grape crates: two stacked timber crates of purple and green grapes.
        static ComicMesh GrapeCratesMesh()
        {
            var m = new ComicMesh();
            for (int i = 0; i < 2; i++)
            {
                Vector3 c = V(i == 0 ? 0f : .06f, .22f + i * .46f, i == 0 ? 0f : -.03f);
                Quaternion q = Quaternion.Euler(0, i * 9f, 0);
                m.Surface = ComicSurface.Wood;
                m.Box(c, V(1.04f, .08f, .74f), TimberDark, q);
                m.Box(c + q * V(0, .2f, .35f), V(1.04f, .34f, .05f), Timber, q);
                m.Box(c + q * V(0, .2f, -.35f), V(1.04f, .34f, .05f), Timber, q);
                m.Box(c + q * V(.5f, .2f, 0), V(.05f, .34f, .74f), Timber, q);
                m.Box(c + q * V(-.5f, .2f, 0), V(.05f, .34f, .74f), Timber, q);
                m.Surface = ComicSurface.Foliage;
                for (int k = 0; k < 3; k++) Blob(m, c + q * V(-.3f + k * .3f, .34f, (k % 2) * .16f - .08f), V(.34f, .26f, .4f), (k + i) % 2 == 0 ? K.Hex("6A3F8F") : K.Hex("9BB84A"), 5, 3);
            }
            return m;
        }

        // The pedestrian maze gate of a railway crossing: two offset steel hoops with red-and-white bands.
        static ComicMesh MazeGateMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            Color red = K.Hex("D8382E");
            m.Box(V(0, .04f, 0), V(1.9f, .08f, .3f), Ink);
            foreach (float x in new[] { -.85f, 0f, .85f }) m.Box(V(x, .62f, 0), V(.09f, 1.16f, .09f), White);
            m.Box(V(0, 1.2f, 0), V(1.8f, .09f, .09f), White);
            m.Box(V(0, .65f, 0), V(1.8f, .07f, .07f), White);
            foreach (float x in new[] { -.42f, .42f }) { m.Box(V(x, 1.2f, 0), V(.4f, .1f, .1f), red); m.Box(V(x, .65f, 0), V(.4f, .08f, .08f), red); }
            foreach (float x in new[] { -.85f, 0f, .85f }) m.Box(V(x, .9f, 0), V(.1f, .22f, .1f), red);
            return m;
        }

        // Pacific black duck: brown body, darker wings, cream face with the dark eye stripe, green speculum, olive bill.
        static ComicMesh DuckMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color brown = K.Hex("7A5E44"), dark = K.Hex("4E3B2B"), cream = K.Hex("E4D2A8");
            Blob(m, V(0, .24f, .04f), V(.3f, .26f, .52f), brown, 6, 3);
            foreach (int s in new[] { -1, 1 })
            {
                m.Box(V(s * .13f, .28f, .08f), V(.05f, .14f, .34f), dark, Quaternion.Euler(0, 0, s * -12f));
                m.Box(V(s * .15f, .27f, .12f), V(.03f, .06f, .12f), K.Hex("2E8B57"), Quaternion.Euler(0, 0, s * -12f));
                m.Box(V(s * .06f, .04f, 0), V(.06f, .08f, .1f), K.Hex("C98A3A"));
                m.Box(V(s * .075f, .46f, -.22f), V(.02f, .03f, .1f), dark);
            }
            m.Box(V(0, .3f, .3f), V(.14f, .06f, .12f), dark, Quaternion.Euler(-20f, 0, 0));
            m.Beam(V(0, .3f, -.14f), V(0, .44f, -.2f), .12f, brown);
            m.Box(V(0, .46f, -.22f), V(.14f, .13f, .16f), cream);
            m.Box(V(0, .53f, -.2f), V(.12f, .04f, .16f), dark);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .43f, -.34f), V(.08f, .04f, .12f), K.Hex("6E7A4A"));
            return m;
        }

        // Australian white ibis: white body, black bare head and long down-curved bill, black tail plumes, dark legs.
        static ComicMesh IbisMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color white = K.Hex("F3F1EA"), black = K.Hex("1F2125"), legs = K.Hex("5A4A4A");
            Blob(m, V(0, .62f, .08f), V(.36f, .34f, .62f), white, 6, 3);
            m.Box(V(0, .66f, .42f), V(.22f, .1f, .2f), black, Quaternion.Euler(-25f, 0, 0));
            m.Beam(V(0, .72f, -.16f), V(0, .98f, -.3f), .1f, white);
            m.Box(V(0, 1.02f, -.33f), V(.11f, .12f, .14f), black);
            m.Surface = ComicSurface.Plastic;
            m.Beam(V(0, 1.02f, -.4f), V(0, .94f, -.58f), .045f, black);
            m.Beam(V(0, .94f, -.58f), V(0, .8f, -.68f), .035f, black);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.07f, .07f })
            {
                m.Box(V(x, .24f, .08f), V(.035f, .48f, .035f), legs);
                m.Box(V(x, .02f, .03f), V(.1f, .04f, .16f), legs);
            }
            return m;
        }

        // Black swan: black body with ruffled wing feathers, a long S neck, the red bill with a white band.
        static ComicMesh BlackSwanMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color black = K.Hex("23252A"), soft = K.Hex("3A3D44"), red = K.Hex("D8342A");
            Blob(m, V(0, .42f, .1f), V(.5f, .44f, .9f), black, 6, 3);
            foreach (int s in new[] { -1, 1 }) m.Box(V(s * .2f, .56f, .2f), V(.1f, .22f, .5f), soft, Quaternion.Euler(-12f, 0, s * -14f));
            m.Box(V(0, .62f, .5f), V(.2f, .12f, .2f), K.Hex("E8E6E0"), Quaternion.Euler(-30f, 0, 0));
            m.Beam(V(0, .5f, -.28f), V(0, .82f, -.42f), .11f, black);
            m.Beam(V(0, .82f, -.42f), V(0, 1.1f, -.3f), .1f, black);
            m.Beam(V(0, 1.1f, -.3f), V(0, 1.16f, -.44f), .1f, black);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, 1.13f, -.56f), V(.07f, .06f, .16f), red);
            m.Box(V(0, 1.13f, -.62f), V(.075f, .065f, .03f), White);
            return m;
        }

        // ---------------------------------------------------------------- Woodbridge far scenery

        /// <summary>
        /// The whole horizon round the rider (the CBD panorama is hidden out here): on the left the Swan, green-brown and
        /// wide, its far bank of river gums, the vineyard rows and sheds of the Swan Valley behind and a long low range;
        /// on the right, over the railway, tin roofs between gums and the Darling Scarp, which swings in towards the road
        /// ahead (Midland lies at its foot). Sheared with the road's grade so it meets the chunks' ground. No colliders.
        /// </summary>
        static ComicMesh WoodFar(float grade)
        {
            var m = new ComicMesh();
            var r = new System.Random(330);
            Color haze = K.Hex("C9DAD8");
            const float z0 = -220f, z1 = 820f;
            // the ground ahead of the built chunks and beside them, the river band on the left
            FarStrip(m, K.Edge, 68f, 330f, z1, K.Kerb - .03f, Color.Lerp(DryGrass, Lawn, .5f));
            FarStrip(m, -K.Edge, -24f, 330f, z1, K.Kerb - .03f, Lawn);
            FarStrip(m, -24f, -68f, 330f, z1, River - .05f, RiverGreen);
            FarStrip(m, 68f, 240f, z0, z1, -.25f, Haze(K.Hex("8DA562"), 150f, haze));
            FarStrip(m, 240f, 1400f, z0, z1, -.3f, Haze(K.Hex("93A870"), 500f, haze));
            FarStrip(m, -66f, -150f, z0, z1, River - .06f, Color.Lerp(RiverGreen, haze, .12f));
            FarStrip(m, -150f, -330f, z0, z1, -.4f, Haze(K.Hex("86A45A"), 240f, haze));
            FarStrip(m, -330f, -1400f, z0, z1, -.45f, Haze(K.Hex("94AA74"), 600f, haze));
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 110; i++)
            {
                float x = -F(r, 70f, 146f), z = F(r, -180f, 780f), len = F(r, 2f, 6f), y = River - .04f;
                K.Face(m, V(x, y, z), V(x, y, z + .5f), V(x + len, y, z + .5f), V(x + len, y, z), Haze(RiverLight, x, haze), Vector3.up, false);
            }
            // the far bank: a mud edge, river gums and casuarinas, then vineyard rows
            for (float z = z0; z < z1; z += 15f)
            {
                K.Face(m, V(-148f, River - .06f, z), V(-148f, River - .06f, z + 15f), V(-153f, .6f, z + 15f), V(-153f, .6f, z), Haze(Mud, 150f, haze), V(1, 2, 0), false);
            }
            for (float z = z0 + 4f; z < z1; z += F(r, 9f, 16f))
            {
                float x = -F(r, 154f, 176f), h = F(r, 11f, 18f);
                m.Surface = ComicSurface.Wood;
                m.Cylinder(V(x, h * .3f, z), .5f, h * .6f, Haze(BarkShade, x, haze), 4, .6f);
                m.Surface = ComicSurface.Foliage;
                Blob(m, V(x, h * .72f, z), V(F(r, 9f, 14f), h * .55f, F(r, 9f, 13f)), Haze(Color.Lerp(LeafDark, Leaf, F(r, 0f, 1f)), 170f, haze), 5, 3);
            }
            Color vine = Haze(K.Hex("6F9A45"), 260f, haze), soil = Haze(K.Hex("B89B6C"), 260f, haze);
            for (float zb = z0 + 30f; zb < z1 - 60f; zb += 150f)
            {
                FarStrip(m, -190f, -320f, zb, zb + 100f, .1f, soil);
                m.Surface = ComicSurface.Foliage;
                for (float z = zb + 3f; z < zb + 98f; z += 6f) K.MinMaxBox(m, V(-194f, .1f, z), V(-316f, 1.9f, z + 1.6f), vine);
                m.Surface = ComicSurface.Metal;
                m.Box(V(-200f, 3f, zb + 122f), V(22f, 6f, 14f), Haze(K.Hex("D9D2C2"), 220f, haze));
                m.Cylinder(V(-200f, 6.8f, zb + 122f), 15f, 2.6f, Haze(RedOxide, 220f, haze), 4, .2f, Quaternion.Euler(0, 45, 0));
            }
            // tin roofs and gums behind the railway on the right, and behind the back gardens on the left
            Color[] walls = { Cream, White, Butter, K.Hex("D9CBB0") };
            Color[] roofs = { RedOxide, IronRoof, GreenRoof, K.Hex("8A8F94") };
            for (float z = z0; z < z1; z += F(r, 11f, 16f))
                for (int row = 0; row < 3; row++)
                {
                    float x = 74f + row * 28f + F(r, 0f, 12f), dist = Mathf.Sqrt(x * x + z * z);
                    FarHouse(m, V(x, 0f, z), F(r, 9f, 13f), F(r, 3.2f, 5.5f), Haze(walls[r.Next(walls.Length)], dist, haze), Haze(roofs[r.Next(roofs.Length)], dist, haze));
                }
            for (float z = z0; z < z1; z += F(r, 13f, 22f))
            {
                float x = F(r, 70f, 190f), h = F(r, 13f, 22f);
                m.Surface = ComicSurface.Wood;
                m.Cylinder(V(x, h * .3f, z), .45f, h * .6f, Haze(Bark, x, haze), 4, .6f);
                m.Surface = ComicSurface.Foliage;
                Blob(m, V(x, h * .75f, z), V(F(r, 8f, 13f), h * .5f, F(r, 8f, 12f)), Haze(Color.Lerp(LeafDark, LeafLight, F(r, 0f, 1f)), x, haze), 5, 3);
            }
            // the Darling Scarp: on the right it swings in towards the road ahead; a lower range closes the left
            Scarp(m, 1, z0, z1, -.4f, z => Mathf.Lerp(430f, 150f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-100f, 520f, z))),
                z => 46f + 26f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-100f, 480f, z)) + 6f * Mathf.Sin(z * .012f + 1f) + 3f * Mathf.Sin(z * .041f),
                190f, Haze(K.Hex("7C9A62"), 300f, haze), Haze(K.Hex("5F8068"), 420f, haze), Haze(K.Hex("7893A0"), 700f, haze));
            Scarp(m, -1, z0, z1, -.4f, z => Mathf.Lerp(520f, 330f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-100f, 560f, z))),
                z => 26f + 22f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 520f, z)) + 5f * Mathf.Sin(z * .01f + 2f) + 3f * Mathf.Sin(z * .033f),
                200f, Haze(K.Hex("86A26A"), 420f, haze), Haze(K.Hex("6C8C78"), 560f, haze), Haze(K.Hex("86A0AC"), 800f, haze));
            ScarpPatches(m, 1, z0, z1, -.4f, z => Mathf.Lerp(430f, 150f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-100f, 520f, z))),
                z => 46f + 26f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-100f, 480f, z)) + 6f * Mathf.Sin(z * .012f + 1f) + 3f * Mathf.Sin(z * .041f),
                190f, Haze(K.Hex("4C6C56"), 420f, haze), Haze(K.Hex("74926E"), 420f, haze), 331);
            m.Shear(grade);
            return m;
        }
    }
}
