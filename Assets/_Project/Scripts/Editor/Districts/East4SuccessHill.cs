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
    // SUCCESS HILL (route order 300): the shared path beside the Midland line, the station, Success Hill Reserve on the
    // high bank where Bennett Brook meets the Swan, the boardwalk and the bridge over the river to Guildford.
    public static partial class East4Builder
    {
        // The river on the left (flat road space): water level, width of the clay bank, where the far bank starts.
        const float RiverY = -3.4f, BankW = 7f, FarBankX = -86f;

        static void BuildSuccessHill(TrackTheme theme, bool atlas)
        {
            const string id = SuccessId;
            K.Begin(MeshFolder(id), PrefabFolder(id), CbdBuilder.DataFolder, PatternFolder(id));
            Obstacle bollard = K.SaveObstacle(Pf(id, "OBS_Successhill_Bollard"), "OBS_Successhill_Bollard", PathBollardMesh(), new Vector3(.45f, 1.05f, .45f), ObstacleKind.Thing, true, true, 1.1f);
            Obstacle branch = K.SaveObstacle(Pf(id, "OBS_Successhill_FallenBranch"), "OBS_Successhill_FallenBranch", FallenBranchMesh(), new Vector3(2.3f, .75f, 1f), ObstacleKind.Thing, true, true, 1f);
            Obstacle bin = K.SaveObstacle(Pf(id, "OBS_Successhill_ParkBin"), "OBS_Successhill_ParkBin", ParkBinMesh(), new Vector3(.8f, 1.1f, .8f), ObstacleKind.Thing, true, true, .9f);
            Obstacle sign = K.SaveObstacle(Pf(id, "OBS_Successhill_PathSign"), "OBS_Successhill_PathSign", PathSignMesh(), new Vector3(.7f, 2.3f, .4f), ObstacleKind.Thing, false, true, .7f);
            GameObject ducks = SaveAnimal(Pf(id, "ANM_Successhill_Ducks"), "ANM_Successhill_Ducks", DuckFamilyMesh(), new Vector3(0, .25f, .5f), new Vector3(.5f, .5f, 1.7f), .45f, true);
            GameObject ibis = SaveAnimal(Pf(id, "ANM_Successhill_Ibis"), "ANM_Successhill_Ibis", IbisMesh(), new Vector3(0, .5f, 0), new Vector3(.45f, 1f, .9f), .35f, false);
            Ramp kicker = K.SaveRamp(Pf(id, "RMP_Successhill_Boardwalk"), "RMP_Successhill_Boardwalk", 3f, 5f, 1.3f, 1.5f, 1f, .5f, E4.DeckLight, E4.Deck, false);

            K.WriteChunk(ShEntry(), ChunkPath(id, "Entry"));
            K.WriteChunk(ShStation(), ChunkPath(id, "Station"));
            K.WriteChunk(ShReserve(), ChunkPath(id, "Reserve"));
            K.WriteChunk(ShLookout(), ChunkPath(id, "Lookout"));
            K.WriteChunk(ShBoardwalk(), ChunkPath(id, "Boardwalk"));
            K.WriteChunk(ShRiverBridge(), ChunkPath(id, "RiverBridge"));
            K.WriteChunk(ShExit(), ChunkPath(id, "Exit"));
            GameObject far = K.SaveFarScenery(Pf(id, "FAR_Successhill_SwanRiver"), "FAR_Successhill_SwanRiver", ShFar(-SuccessDrop / 40f));

            GameObject cyclist = Cyclist();
            string patterns = PatternFolder(id);
            var own = new List<HazardPattern>
            {
                K.Pattern(patterns, "HZ_Successhill_Bollards", 40, 0, .8f, false, 30, (bollard.gameObject, -2.6f, 20f, 0f), (bollard.gameObject, 0f, 20f, 0f), (bollard.gameObject, 2.6f, 20f, 0f)),
                K.Pattern(patterns, "HZ_Successhill_Ducks", 40, 0, .9f, true, 30, (ducks, -1.4f, 20f, 0f), (ibis, 1.8f, 27f, 0f)),
                K.Pattern(patterns, "HZ_Successhill_Branches", 40, 1, .8f, true, 30, (branch.gameObject, -2.6f, 18f, 20f), (branch.gameObject, 1.3f, 26f, -15f)),
            };
            if (cyclist != null) own.Add(K.Pattern(patterns, "HZ_Successhill_Cyclists", 50, 0, 1f, true, 30, (cyclist, -2.4f, 24f, 0f), (cyclist, -1.2f, 31f, 0f)));

            District d = Fill(id, "SUCCESS HILL", 300, SuccessChunks);
            d.obstacles = new[] { bollard, branch, bin, sign };
            d.jumpObstacles = new[] { branch, bollard, bin };
            d.hazards = own.Concat(ThemeHazards(theme, "HZ_SidewalkStroll")).ToArray();
            var people = new List<GameObject>(K.People(theme, "Jogger", "Jogger", "DogWalker", "DogWalker", "Elder", "Kid", "Student", "Tourist"));
            if (cyclist != null) { people.Add(cyclist); people.Add(cyclist); }
            people.Add(ducks); people.Add(ducks); people.Add(ibis);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { kicker };
            d.rampFrequency = 1f;
            // Soft river light under the gums.
            d.fogColor = new Color(.80f, .87f, .84f);
            d.skyTint = new Color(1f, 1f, .98f);
            d.ambientSky = new Color(.68f, .82f, .94f);
            d.ambientEquator = new Color(.86f, .88f, .80f);
            d.ambientGround = new Color(.62f, .62f, .48f);
            d.sunColor = new Color(1f, .96f, .86f);
            d.sunIntensity = 1.4f;
            d.farScenery = far;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/SuccessHill] " + string.Join("; ", K.Report));
            if (!atlas) return;
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("RiverRedGum", null, Make(m => E4.Gum(m, Vector3.zero, 16f, 3))),
                ("GumFar", null, Make(m => E4.Gum(m, Vector3.zero, 13f, 5, true))),
                ("Jacaranda", null, Make(m => E4.Jacaranda(m, Vector3.zero, 8f, 2))),
                ("FederationHouse", null, Make(m => Bungalow(m, new CqLot(-1, -5.5f, 5.5f, 6f, 10f, 0f), E4.Brick, E4.Iron, 0))),
                ("PicnicShelter", null, Make(m => PicnicShelter(m, Vector3.zero))),
                ("StoneCircleAndSign", null, Make(m => { StoneCircle(m, Vector3.zero); InterpretiveSign(m, V(3.2f, 0, 0), -60f); })),
                ("FishingPlatform", null, Make(m => FishingPlatform(m, Vector3.zero, V(9f, 3f, 0f)))),
                ("StationShelter", null, Make(m => E4.Shelter(m, 0f, 0f, 6f, 0f, E4.Maroon, E4.Iron, E4.HeritageGreen, "SUCCESS HILL"))),
                ("StationSign", null, Make(m => E4.StationSign(m, Vector3.zero, "SUCCESS HILL"))),
                ("Train", null, Make(m => E4.Train(m, 0f, -12f, 1))),
                ("PathLight", null, Make(m => E4.PathLight(m, Vector3.zero))),
                ("BlackSwans", null, Make(m => { BlackSwan(m, Vector3.zero, 20f); BlackSwan(m, V(1.6f, 0, .8f), -30f); })),
                ("Bollard", bollard.gameObject, null),
                ("FallenBranch", branch.gameObject, null),
                ("ParkBin", bin.gameObject, null),
                ("PathSign", sign.gameObject, null),
                ("Ducks", ducks, null),
                ("Ibis", ibis, null),
                ("BoardwalkKicker", kicker.gameObject, null),
            };
            K.RenderAtlas("art-src/districts/successhill/props-atlas.png", items);
        }

        // ---------------------------------------------------------------- chunks

        static CqChunkDraft ShEntry()
        {
            var d = new CqChunkDraft("Successhill_Entry", SuccessDrop);
            E4.SharedPath(d);
            CalmLeft(d, 0f, 14f, 1);
            CalmRail(d, 0f, 14f, 2);
            // then the path opens: lawn and gums on the left, the railway on the right
            E4.Ground(d, -1, 14f, 40f, 0f, 70f, E4.Lawn, ComicSurface.Foliage);
            E4.Gum(d.Props, V(-(K.Edge + 4f), d.Ground(22f), 22f), 15f, 11);
            E4.Gum(d.Props, V(-(K.Edge + 8.5f), d.Ground(33f), 33f), 17f, 12);
            E4.PathLight(d.Props, V(-(K.Edge + .5f), d.Ground(28f), 28f), 180f);
            E4.SignBoard(d.Props, V(-(K.Edge + 1.6f), d.Ground(17f), 17f), 0f, "SUCCESS HILL", .09f, K.Hex("2F5D46"), E4.Cream);
            BackTrees(d, -1, 14f, 40f, 14f, 30f, 3);
            Railway(d, 4, true, 14f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft ShStation()
        {
            // Success Hill station: the platform, its small red shelter and the footbridge with the long ramp and the
            // mural; a train standing at the platform. Federation houses behind picket fences on the left.
            var d = new CqChunkDraft("Successhill_Station", SuccessDrop);
            E4.SharedPath(d);
            E4.Ground(d, -1, 0f, 40f, 0f, 70f, E4.Lawn, ComicSurface.Foliage);
            var a = new CqLot(-1, 1.5f, 12.5f, K.Edge + 6f, 10f, d.Base(7f));
            var b = new CqLot(-1, 15f, 26f, K.Edge + 7f, 10f, d.Base(20.5f));
            var c = new CqLot(-1, 28.5f, 39f, K.Edge + 6f, 9f, d.Base(34f));
            Bungalow(d.Frontage, a, E4.Brick, E4.Iron, 0);
            Bungalow(d.Frontage, b, K.Hex("EFE6D2"), E4.Tile, 1, true);
            Bungalow(d.Frontage, c, E4.BrickDark, E4.IronDark, 2);
            FrontGarden(d, a, BungalowDoorZ(a, 0), 1); FrontGarden(d, b, BungalowDoorZ(b, 1), 2); FrontGarden(d, c, BungalowDoorZ(c, 2), 3);
            foreach (CqLot l in new[] { a, b, c }) d.Wall(l, 7f);
            E4.Jacaranda(d.Props, V(-(K.Edge + 3.2f), d.Ground(13.7f), 13.7f), 7.5f, 4);
            E4.PetalCarpet(d.Road, -(K.Edge + 3.2f), 13.7f, 2.6f, K.Kerb + .05f, 4);
            E4.Gum(d.Props, V(-(K.Edge + 3.4f), d.Ground(27.2f), 27.2f), 14f, 5);
            BackTrees(d, -1, 0f, 40f, 19f, 32f, 6, 8f);
            // the station
            E4.RailCorridor(d, 0f, 40f, false);
            E4.Gantry(d, 20f);
            E4.Wires(d, 0f, 40f);
            E4.RailBackdrop(d, 0f, 40f, 7, false);
            E4.Platform(d, 5f, 23f);
            float top = E4.RailY + .95f;
            E4.Shelter(d.SlopedFrontage, K.Edge + 1.3f, 12f, 6f, top, E4.Maroon, E4.Iron, E4.HeritageGreen, "SUCCESS HILL");
            // the access ramp at the platform's start, the yellow handrail along its back
            ComicMesh m = d.SlopedFrontage;
            E4.PlatformRamp(m, 1f, 5f);
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = 2f; z < 23f; z += 3f)
            {
                if (z > 8f && z < 15.5f) continue;
                float y = z < 5f ? Mathf.Lerp(K.Kerb, top, (z - 1f) / 4f) : top;
                m.Box(V(K.Edge + 1.25f, y + .55f, z), V(.07f, 1.1f, .07f), E4.Steel);
            }
            m.Box(V(K.Edge + 1.25f, top + 1.05f, 6.5f), V(.07f, .07f, 3f), E4.Yellow);
            m.Box(V(K.Edge + 1.25f, top + 1.05f, 19f), V(.07f, .07f, 8f), E4.Yellow);
            E4.StationSign(d.Props, V(K.Edge + .7f, d.Ground(27f), 27f), "SUCCESS HILL");
            E4.PlatformFurniture(m, top, 7.5f, 17.5f, 21.5f);
            E4.Train(d.SlopedFrontage, E4.Track1, .5f, 1);
            E4.Footbridge(d, 36f, 11f, true);
            E4.PathLight(d.Props, V(K.Edge + .5f, d.Ground(30f), 30f));
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft ShReserve()
        {
            // Success Hill Reserve: big river red gums on the lawn, a picnic shelter, and the meeting place: a ring of
            // sandstone boulders with an interpretive sign (an important Noongar place: kept quiet, no figures).
            var d = new CqChunkDraft("Successhill_Reserve", SuccessDrop);
            E4.SharedPath(d);
            Func<float, float> bank = z => z < 14f ? Mathf.Lerp(62f, 36f, Mathf.SmoothStep(0f, 1f, z / 14f)) : Mathf.Lerp(36f, 24f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(22f, 40f, z)));
            RiverSide(d, 0f, 40f, bank, 21);
            ComicMesh g = d.Road;
            for (float z = 2f; z < 38f; z += 9f) E4.Patch(g, -(K.Edge + 18f), -(K.Edge + .3f), z, z + 4f, K.Kerb + .035f, E4.LawnLight);
            // a gravel walk from the path to the stone circle and the shelter
            E4.Patch(g, -(K.Edge + 9f), -K.Edge, 25.2f, 26.8f, K.Kerb + .05f, E4.Gravel, ComicSurface.Concrete);
            E4.Patch(g, -(K.Edge + 6.5f), -K.Edge, 9.2f, 10.6f, K.Kerb + .05f, E4.Gravel, ComicSurface.Concrete);
            E4.SignBoard(d.Props, V(-(K.Edge + 1.4f), d.Ground(3f), 3f), 0f, "SUCCESS HILL RESERVE", .075f, K.Hex("2F5D46"), E4.Cream);
            PicnicShelter(d.Frontage, V(-(K.Edge + 9.5f), d.Ground(10f) + .03f, 10f));
            StoneCircle(d.Frontage, V(-(K.Edge + 12.5f), d.Ground(26f) + .03f, 26f));
            InterpretiveSign(d.Props, V(-(K.Edge + 6.2f), d.Ground(22.6f), 22.6f), -40f);
            E4.Bench(d.Props, V(-(K.Edge + 2.2f), d.Ground(31.5f), 31.5f), 90f, E4.Timber);
            E4.PathLight(d.Props, V(-(K.Edge + .5f), d.Ground(18f), 18f), 180f);
            // the gums: the big ones close to the path, more behind
            E4.Gum(d.Props, V(-(K.Edge + 3.6f), d.Ground(4.5f), 4.5f), 19f, 31);
            E4.Gum(d.Props, V(-(K.Edge + 5f), d.Ground(17f), 17f), 22f, 32);
            E4.Gum(d.Props, V(-(K.Edge + 17f), d.Ground(18f), 18f), 20f, 33);
            E4.Gum(d.Props, V(-(K.Edge + 4.2f), d.Ground(36f), 36f), 18f, 34);
            E4.Gum(d.Props, V(-(K.Edge + 19f), d.Ground(33f), 33f), 17f, 35);
            E4.Gum(d.Deep, V(-(K.Edge + 23f), d.Ground(6f), 6f), 19f, 36, true);
            E4.Gum(d.Deep, V(-(K.Edge + 25f), d.Ground(25f), 25f), 16f, 37, true);
            Railway(d, 22);
            E4.Train(d.SlopedFrontage, E4.Track2, 8f, 2, false);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft ShLookout()
        {
            // The high bank: a timber rail along the edge, the clay bank falling to the brown-green Swan, the floating
            // fishing platform, black swans, and across the water the mouth of Bennett Brook between paperbarks.
            var d = new CqChunkDraft("Successhill_Lookout", SuccessDrop);
            E4.SharedPath(d);
            Func<float, float> bank = z => 24f - 9.5f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 12f, z));
            RiverSide(d, 0f, 40f, bank, 23);
            ComicMesh m = d.SlopedFrontage;
            // the lookout rail along the bank edge (timber posts, two rails, a cap)
            m.Surface = ComicSurface.Wood;
            for (float z = 12f; z < 40f - .01f; z += 4f)
            {
                if (z > 19f && z < 27f || z > 31f && z < 35f) continue; // the deck and the gangway open here
                float x = -(14.5f - .5f);
                m.Box(V(x, K.Kerb + 1.05f, z + 2f), V(.16f, .08f, 4f), E4.Timber);
                m.Box(V(x, K.Kerb + .6f, z + 2f), V(.06f, .1f, 4f), E4.TimberDark);
                m.Box(V(x, K.Kerb + .55f, z), V(.16f, 1.1f, .16f), E4.TimberDark);
            }
            // a lookout deck stepping out over the bank, with a bench and the reserve's second sign
            m.Box(V(-16.4f, K.Kerb + .02f, 24f), V(5f, .16f, 8f), E4.DeckLight);
            foreach (float z in new[] { 20.6f, 27.4f }) m.Box(V(-17.9f, -1.8f, z), V(.24f, 4f, .24f), E4.TimberDark);
            foreach (float z in new[] { 20.1f, 27.9f }) { m.Box(V(-16.4f, K.Kerb + 1.05f, z), V(5f, .08f, .14f), E4.Timber); m.Box(V(-16.4f, K.Kerb + .6f, z), V(5f, .1f, .06f), E4.TimberDark); }
            m.Box(V(-18.85f, K.Kerb + 1.05f, 24f), V(.14f, .08f, 7.9f), E4.Timber);
            m.Box(V(-18.85f, K.Kerb + .6f, 24f), V(.06f, .1f, 7.9f), E4.TimberDark);
            foreach (float z in new[] { 20.1f, 22.7f, 25.3f, 27.9f }) m.Box(V(-18.85f, K.Kerb + .55f, z), V(.16f, 1.1f, .16f), E4.TimberDark);
            E4.Bench(d.Props, V(-17.6f, d.Ground(24f) + .1f, 24f), 90f, E4.Timber);
            InterpretiveSign(d.Props, V(-(K.Edge + 2.6f), d.Ground(15f), 15f), -40f);
            // gums leaning over the water, shrubs on the bank
            E4.Gum(d.Props, V(-(K.Edge + 3.4f), d.Ground(7f), 7f), 18f, 41);
            E4.Gum(d.Props, V(-(K.Edge + 4.6f), d.Ground(31f), 31f), 21f, 42);
            E4.Gum(d.Props, V(-18.5f, d.Y(36.5f) - 1.3f, 36.5f), 15f, 43);
            E4.Gum(d.Props, V(-17.5f, d.Y(13.5f) - 1f, 13.5f), 13f, 44);
            var r = new System.Random(45);
            for (float z = 13f; z < 40f; z += 3.6f)
                E4.Bush(d.Props, V(-(16.2f + F(r, 0f, 3.2f)), d.Y(z) - F(r, .7f, 2.2f), z + F(r, -1f, 1f)), F(r, .9f, 1.5f), r.Next(2) == 0 ? K.Hex("6F8F52") : K.Hex("8C9C5A"), (int)z);
            for (float z = 14f; z < 40f; z += 5f) E4.Reeds(d.Props, V(-21.3f, d.Y(z) + RiverY - .1f, z), 1.5f, (int)z);
            // on the water
            FishingPlatform(d.Landmark, V(-27f, d.Y(33f) + RiverY, 33f), V(-14.3f, d.Ground(33f), 33f));
            BlackSwan(d.Props, V(-33f, d.Y(18f) + RiverY, 18f), 40f);
            BlackSwan(d.Props, V(-35f, d.Y(20f) + RiverY, 20f), 60f);
            BlackSwan(d.Props, V(-46f, d.Y(34f) + RiverY, 34f), -70f);
            E4.PathLight(d.Props, V(-(K.Edge + .5f), d.Ground(26f), 26f), 180f);
            Railway(d, 24);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft ShBoardwalk()
        {
            // The path runs out along a timber boardwalk over a creek mouth, up a kicker, then 6 m of open water across
            // the whole width (SPLASH!), then the bank again. The railway crosses the creek on a short girder bridge.
            var d = new CqChunkDraft("Successhill_Boardwalk", SuccessDrop);
            float j0 = BoardStart, ramp = BoardRampStart, lip = ramp + BoardRise + BoardTop, far = lip + BoardGap;
            E4.SharedPath(d, j0, far);
            BoardwalkDeck(d, j0, ramp);
            const float creek = RiverY;
            ComicMesh m = d.Road;
            K.Water(m, -(14.5f + BankW * .55f), 70f, j0, far, creek, E4.River, E4.RiverLight, 251);
            // rock walls where the banks end (limestone spalls), under the path and the rail
            foreach (float z in new[] { j0, far })
            {
                float t = z == j0 ? -.5f : .5f;
                m.Surface = ComicSurface.Concrete;
                K.MinMaxBox(m, V(-24f, creek - 1f, z), V(40f, -.03f, z + t), K.Hex("C9B98F"));
                K.MinMaxBox(m, V(-24f, -.03f, z), V(-K.Half, K.Kerb + .02f, z + t), K.Hex("DCCDA2"));
                K.MinMaxBox(m, V(K.Half, -.03f, z), V(E4.BedX0, K.Kerb + .02f, z + t), K.Hex("DCCDA2"));
            }
            var spec = new CqRampSpec { Name = "BoardwalkKicker", X = 0f, Z = ramp, Width = 2f * K.Edge, Rise = BoardRise, Height = BoardHeight, Top = BoardTop, Launch = 1f, Hang = .5f };
            d.Ramps.Add(spec);
            d.Gaps.Add(new CqGapSpec { Name = "CreekMouth", Z = lip, Length = BoardGap, Width = 0f, Word = "SPLASH", Water = true });
            var deck = new ComicMesh();
            K.RampMesh(deck, spec, E4.DeckLight, E4.Deck, false, creek - .3f);
            for (int s = -1; s <= 1; s += 2)
            {
                deck.Surface = ComicSurface.Wood;
                deck.Beam(V(s * (K.Edge + .12f), 1.05f, ramp), V(s * (K.Edge + .12f), BoardHeight + 1.05f, ramp + BoardRise), .12f, E4.Timber);
                deck.Beam(V(s * (K.Edge + .12f), BoardHeight + 1.05f, ramp + BoardRise), V(s * (K.Edge + .12f), BoardHeight + 1.05f, lip - .2f), .12f, E4.Timber);
                for (float z = ramp; z <= lip; z += 3f)
                {
                    float h = BoardHeight * Mathf.Clamp01((z - ramp) / BoardRise);
                    deck.Box(V(s * (K.Edge + .12f), (h + creek) * .5f + .5f, z), V(.25f, h - creek + 1f, .25f), E4.Deck);
                }
            }
            d.SlopedFrontage.Add(deck, Vector3.zero, Quaternion.identity, Vector3.one);
            d.KeepOut.Add(KeepOutBox(d, -1, j0, far)); d.KeepOut.Add(KeepOutBox(d, 1, j0, far));
            // left: the river goes on; the creek mouth opens into it
            RiverSide(d, 0f, j0, z => 14.5f, 252, false, 14.5f);
            RiverSide(d, far, 40f, z => 14.5f + 6f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(far, 40f, z)), 253, false, 14.5f);
            RiverWater(d, j0, far, 14.5f, 254);
            FarBank(d, 0f, 40f, 255);
            E4.Gum(d.Props, V(-(K.Edge + 3.5f), d.Ground(1.4f), 1.4f), 17f, 51);
            E4.Gum(d.Props, V(-(K.Edge + 4f), d.Ground(31f), 31f), 20f, 52);
            E4.Gum(d.Props, V(-(K.Edge + 8.5f), d.Ground(37f), 37f), 16f, 53);
            for (float z = 6f; z < 24f; z += 4.5f) E4.Reeds(d.Props, V(-(K.Edge + 3f + (z % 2f)), d.Y(z) + creek - .1f, z), 1.6f, (int)z);
            E4.Reeds(d.Props, V(9.2f, d.Y(8f) + creek - .1f, 8f), 1.5f, 3);
            E4.Reeds(d.Props, V(9.4f, d.Y(21f) + creek - .1f, 21f), 1.4f, 4);
            // right: the corridor on both banks, a girder bridge over the creek
            E4.RailCorridor(d, 0f, j0);
            E4.RailCorridor(d, far, 40f);
            RailGirder(d, j0, far, creek, 0);
            E4.Gantry(d, 20f);
            E4.Wires(d, 0f, 40f);
            E4.RailBackdrop(d, 0f, j0, 25);
            E4.RailBackdrop(d, far, 40f, 26);
            d.GiftSlots.Add(new Vector2(0f, 31f));
            d.Slot(-2.6f, 36f); d.Slot(2.6f, 36f);
            return d;
        }

        static CqChunkDraft ShRiverBridge()
        {
            // Across the Swan to Guildford: the path on its own low bridge (concrete deck, steel rails, lamps), the
            // railway bridge beside it with a train on it, the river wide on both sides between banks of gums.
            var d = new CqChunkDraft("Successhill_RiverBridge", SuccessDrop);
            const float b0 = 7f, b1 = 33f;
            d.Type = RoadType.PedestrianMall;
            E4.Street(d.Road, d.Length, E4.RedPath, ComicSurface.Asphalt, E4.Concrete, E4.Concrete, ComicSurface.Paving, E4.KerbCap, true);
            ComicMesh m = d.Road;
            m.Surface = ComicSurface.Plain;
            for (float z = 1f; z < 38f; z += 5f) K.Face(m, V(-.07f, .03f, z), V(-.07f, .03f, z + 2.6f), V(.07f, .03f, z + 2.6f), V(.07f, .03f, z), E4.PathLine, Vector3.up, false);
            // water everywhere under the bridges, the two banks
            K.Water(m, -160f, -K.Edge, 0f, 40f, RiverY, E4.River, E4.RiverLight, 261);
            K.Water(m, K.Edge, 160f, 0f, 40f, RiverY, E4.River, E4.RiverLight, 262);
            E4.Patch(m, -K.Edge, K.Edge, b0 - 2f, b1 + 2f, RiverY, E4.RiverDeep);
            BridgeBank(d, -1, 0f, b0, true, 262, true);
            BridgeBank(d, 1, 0f, b0, true, 264);
            BridgeBank(d, -1, b1, 40f, false, 266);
            BridgeBank(d, 1, b1, 40f, false, 268);
            // the path bridge: fascia girders, piers, rails, lamps
            ComicMesh f = d.SlopedFrontage;
            foreach (int s in new[] { -1, 1 })
            {
                f.Surface = ComicSurface.Concrete;
                for (float z = b0 - 1f; z < b1 + 1f - .01f; z += 8f)
                    K.MinMaxBox(f, V(s * (K.Edge + .02f), -1.1f, z), V(s * (K.Edge + .5f), K.Kerb + .12f, Mathf.Min(b1 + 1f, z + 8f)), E4.Concrete);
                f.Surface = ComicSurface.Metal;
                for (float z = b0 - 1f; z < b1 + 1f - .01f; z += 8f)
                {
                    float e = Mathf.Min(b1 + 1f, z + 8f);
                    f.Box(V(s * (K.Edge + .26f), K.Kerb + 1.25f, (z + e) * .5f), V(.1f, .1f, e - z), E4.Steel);
                    f.Box(V(s * (K.Edge + .26f), K.Kerb + .7f, (z + e) * .5f), V(.05f, .05f, e - z), E4.SteelDark);
                }
                for (float z = b0 - 1f; z <= b1 + 1.01f; z += 2f) f.Box(V(s * (K.Edge + .26f), K.Kerb + .7f, z), V(.09f, 1.2f, .09f), E4.Steel);
            }
            foreach (float z in new[] { 12f, 20f, 28f })
            {
                f.Surface = ComicSurface.Concrete;
                f.Box(V(0, -.75f, z), V(2f * K.Edge + .6f, .7f, 1f), E4.ConcreteDark);
                foreach (float x in new[] { -5.2f, 5.2f }) f.Cylinder(V(x, (RiverY - 1.1f) * .5f - .2f, z), .55f, -RiverY + 1.1f, E4.Concrete, 8);
            }
            E4.PathLight(d.Props, V(-(K.Edge + .28f), d.Ground(10f), 10f), 180f);
            E4.PathLight(d.Props, V(K.Edge + .28f, d.Ground(30f), 30f));
            // the railway bridge
            E4.RailCorridor(d, 0f, b0);
            E4.RailCorridor(d, b1, 40f);
            RailGirder(d, b0, b1, RiverY, 3);
            E4.Gantry(d, 20f);
            E4.Wires(d, 0f, 40f);
            E4.Train(d.SlopedFrontage, E4.Track1, 4f, 1);
            // swans under the bridge side, a dinghy tied to a pile
            BlackSwan(d.Props, V(-22f, d.Y(16f) + RiverY, 16f), 110f);
            BlackSwan(d.Props, V(-25f, d.Y(18.5f) + RiverY, 18.5f), 130f);
            BlackSwan(d.Props, V(-48f, d.Y(26f) + RiverY, 26f), 80f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft ShExit()
        {
            // The Guildford bank: lawn and gums, then calm towards z = 40 (wall, hedge, small trees).
            var d = new CqChunkDraft("Successhill_Exit", SuccessDrop);
            E4.SharedPath(d);
            E4.Ground(d, -1, 0f, 26f, 0f, 70f, E4.Lawn, ComicSurface.Foliage);
            E4.Gum(d.Props, V(-(K.Edge + 3.6f), d.Ground(5f), 5f), 18f, 61);
            E4.Gum(d.Props, V(-(K.Edge + 9f), d.Ground(15f), 15f), 16f, 62);
            E4.Jacaranda(d.Props, V(-(K.Edge + 3.4f), d.Ground(21f), 21f), 7.5f, 63);
            E4.PetalCarpet(d.Road, -(K.Edge + 3.4f), 21f, 2.6f, K.Kerb + .05f, 63);
            E4.Bench(d.Props, V(-(K.Edge + 1.6f), d.Ground(11f), 11f), 90f, E4.Timber);
            BackTrees(d, -1, 0f, 26f, 14f, 30f, 64);
            CalmLeft(d, 26f, 40f, 65, true);
            Railway(d, 66, true, 0f, 26f);
            CalmRail(d, 26f, 40f, 67);
            d.StandardSlots();
            return d;
        }

        // ---------------------------------------------------------------- the river side

        /// <summary>The left side of a river chunk from z0 to z1 (flat road space): lawn to the bank edge at x = -bank(z),
        /// the clay bank in two facets down to the water, the Swan, and (farBank) the other bank with its trees.</summary>
        static void RiverSide(CqChunkDraft d, float z0, float z1, Func<float, float> bank, int seed, bool farBank = true, float waterFrom = -1f)
        {
            ComicMesh m = d.Road;
            float y = K.Kerb, minBank = float.MaxValue;
            for (float z = z0; z < z1 - .01f; z += 4f)
            {
                float e = Mathf.Min(z1, z + 4f), a = bank(z), b = bank(e);
                minBank = Mathf.Min(minBank, Mathf.Min(a, b));
                m.Surface = ComicSurface.Foliage;
                K.Face(m, V(-K.Edge, y, z), V(-K.Edge, y, e), V(-b, y, e), V(-a, y, z), E4.Lawn, Vector3.up, false);
                m.Surface = ComicSurface.Concrete;
                K.Face(m, V(-a, y, z), V(-b, y, e), V(-(b + BankW * .45f), RiverY * .5f, e), V(-(a + BankW * .45f), RiverY * .5f, z), E4.Clay, Vector3.up, false);
                K.Face(m, V(-(a + BankW * .45f), RiverY * .5f, z), V(-(b + BankW * .45f), RiverY * .5f, e), V(-(b + BankW), RiverY - .4f, e), V(-(a + BankW), RiverY - .4f, z), E4.ClayDark, Vector3.up, false);
            }
            RiverWater(d, z0, z1, waterFrom > 0f ? waterFrom : minBank, seed);
            if (farBank) FarBank(d, z0, z1, seed + 5);
        }

        static void RiverWater(CqChunkDraft d, float z0, float z1, float bank, int seed)
        {
            K.Water(d.Road, FarBankX - 3f, -(bank + BankW * .55f), z0, z1, RiverY, E4.River, E4.RiverLight, seed);
        }

        /// <summary>The far bank of the Swan: a clay edge, reeds green, a dense line of gums and paperbarks.</summary>
        static void FarBank(CqChunkDraft d, float z0, float z1, int seed)
        {
            ComicMesh m = d.Road;
            for (float z = z0; z < z1 - .01f; z += 8f)
            {
                float e = Mathf.Min(z1, z + 8f);
                m.Surface = ComicSurface.Concrete;
                K.Face(m, V(FarBankX, RiverY - .3f, z), V(FarBankX, RiverY - .3f, e), V(FarBankX - 2f, RiverY + 1.2f, e), V(FarBankX - 2f, RiverY + 1.2f, z), E4.Clay, Vector3.up, false);
                m.Surface = ComicSurface.Foliage;
                K.Face(m, V(FarBankX - 2f, RiverY + 1.2f, z), V(FarBankX - 2f, RiverY + 1.2f, e), V(FarBankX - 5f, .6f, e), V(FarBankX - 5f, .6f, z), K.Hex("6E8E55"), Vector3.up, false);
                m.Surface = ComicSurface.Foliage;
                K.Face(m, V(FarBankX - 5f, .6f, z), V(FarBankX - 5f, .6f, e), V(-170f, .6f, e), V(-170f, .6f, z), K.Hex("7C9C58"), Vector3.up, false);
            }
            var r = new System.Random(seed * 29 + 1);
            for (float z = z0 + F(r, 0f, 3f); z < z1; z += F(r, 5.5f, 8f))
                E4.Gum(d.Deep, V(FarBankX - F(r, 5f, 13f), d.Y(z) + .4f, z), F(r, 13f, 19f), seed + (int)(z * 5f), true);
            d.Deep.Surface = ComicSurface.Foliage;
            for (float z = z0 + 2f; z < z1; z += F(r, 6f, 9f))
                E4.Clump(d.Deep, V(FarBankX - F(r, 2f, 5f), d.Y(z) + 1.2f, z), V(F(r, 5f, 8f), F(r, 3f, 5f), F(r, 5f, 8f)), Color.Lerp(K.Hex("6E8E55"), K.Hex("93A86A"), F(r, 0f, 1f)), (int)(z * 7f) + seed, 6, 3);
        }

        /// <summary>A river bank across the chunk at a bridge end (side s): flat ground up to a wavy edge that falls back
        /// with distance (the river widens away from the bridge), a grassy slope and a clay foot into the water, gums and
        /// reeds along it. point = the land runs out a little way off: the river comes round the point from the chunk
        /// before (where it was running beside the path).</summary>
        static void BridgeBank(CqChunkDraft d, int s, float z0, float z1, bool slopeAtEnd, int seed, bool point = false)
        {
            ComicMesh m = d.Road;
            float zs = slopeAtEnd ? z1 : z0, zl = slopeAtEnd ? z0 : z1, dz = slopeAtEnd ? 1f : -1f, y = K.Kerb, room = Mathf.Abs(zs - zl);
            float Retreat(float ax)
            {
                float w = 1.5f + 1.2f * (1f + Mathf.Sin(ax * .11f + seed)) + Mathf.Max(0f, ax - K.Edge) * .03f;
                if (point) w += Mathf.SmoothStep(0f, 1f, (ax - 12f) / 16f) * 60f;
                return Mathf.Min(w, room);
            }
            Color land = s < 0 ? E4.Lawn : E4.LawnDry, grass = K.Hex("7C9C58");
            float xStart = s > 0 ? E4.BedX1 + 1.6f : K.Edge;
            for (float ax = K.Edge; ax < 160f; ax += 8f)
            {
                float bx = ax + 8f, ra = Retreat(ax), rb = Retreat(bx);
                if (ra >= room - .01f && rb >= room - .01f) continue;
                float xa = s * ax, xb = s * bx, za = zs - dz * ra, zb = zs - dz * rb;
                if (bx > xStart)
                {
                    float fa = s * Mathf.Max(ax, xStart);
                    m.Surface = ComicSurface.Foliage;
                    K.Face(m, V(fa, y, zl), V(fa, y, za), V(xb, y, zb), V(xb, y, zl), land, Vector3.up, false);
                }
                float ym = Mathf.Lerp(y, RiverY - .4f, .45f);
                m.Surface = ComicSurface.Foliage;
                K.Face(m, V(xa, y, za), V(xa, ym, za + dz * 2f), V(xb, ym, zb + dz * 2f), V(xb, y, zb), grass, Vector3.up, false);
                m.Surface = ComicSurface.Concrete;
                K.Face(m, V(xa, ym, za + dz * 2f), V(xa, RiverY - .4f, za + dz * 4.5f), V(xb, RiverY - .4f, zb + dz * 4.5f), V(xb, ym, zb + dz * 2f), E4.Clay, Vector3.up, false);
            }
            // the abutment under the path and the rail
            if (s < 0) { m.Surface = ComicSurface.Concrete; K.MinMaxBox(m, V(-K.Edge - .5f, RiverY - 1f, zs - dz * 1.7f), V(E4.BedX1 + .5f, -.04f, zs - dz * .9f), E4.ConcreteDark); }
            var r = new System.Random(seed * 37 + 11);
            for (float x = 13f; x < 150f; x += F(r, 9f, 13f))
            {
                if (s > 0 && x < E4.BedX1 + 5f) continue;
                float back = Retreat(x);
                if (back >= room - 1.2f) continue;
                float z = zs - dz * (back + F(r, .6f, Mathf.Max(.7f, Mathf.Min(2.5f, room - back - .5f)))), h = F(r, 13f, 19f);
                if (x < 30f && s < 0) E4.Gum(d.Props, V(s * x, d.Ground(z), z), h, seed + (int)x);
                else if (x < 70f) E4.Gum(d.Deep, V(s * x, d.Ground(z), z), h, seed + (int)x, true);
                else E4.Clump(d.Deep, V(s * x, d.Ground(z) + h * .45f, z), V(h * .9f, h * .9f, h * .7f), Color.Lerp(E4.GumB, E4.GumC, F(r, 0f, 1f)), (int)x + seed, 6, 4);
            }
            for (float x = 10f; x < 62f; x += F(r, 7f, 10f))
            {
                if (s > 0 && x < E4.BedX1 + 2f) continue;
                float back = Retreat(x);
                if (back >= room - .5f) continue;
                E4.Reeds(d.Deep, V(s * x, d.Y(zs) + RiverY - .1f, zs - dz * (back - 4f)), F(r, 1.3f, 1.8f), seed + (int)x);
                if (x > 14f) E4.Bush(d.Deep, V(s * (x + 2f), d.Y(zs) + RiverY * .3f, zs - dz * (back - 1.4f)), F(r, 1.4f, 2.2f), r.Next(2) == 0 ? K.Hex("6F8F52") : K.Hex("8C9C5A"), seed + (int)x);
            }
        }

        /// <summary>The timber boardwalk before the kicker: the street's cross-section in boards, on piles, with rails.</summary>
        static void BoardwalkDeck(CqChunkDraft d, float z0, float z1)
        {
            ComicMesh m = d.Road;
            Color board = Color.Lerp(E4.Deck, K.R.ink, .3f), walk = Color.Lerp(E4.DeckLight, K.R.cream, .15f);
            for (float z = z0; z < z1 - .01f; z += 1f)
            {
                float e = Mathf.Min(z1, z + 1f);
                m.Surface = ComicSurface.Wood;
                K.Face(m, V(-K.Half, 0f, z), V(-K.Half, 0f, e), V(K.Half, 0f, e), V(K.Half, 0f, z), E4.DeckLight, Vector3.up, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    K.Face(m, V(s * K.Half, K.Kerb, z), V(s * K.Half, K.Kerb, e), V(s * K.Edge, K.Kerb, e), V(s * K.Edge, K.Kerb, z), walk, Vector3.up, false);
                    K.Face(m, V(s * K.Half, -.05f, z), V(s * K.Half, -.05f, e), V(s * K.Half, K.Kerb, e), V(s * K.Half, K.Kerb, z), E4.Deck, new Vector3(-s, 0, 0), false);
                    K.Face(m, V(s * K.Edge, -.6f, z), V(s * K.Edge, -.6f, e), V(s * K.Edge, K.Kerb, e), V(s * K.Edge, K.Kerb, z), E4.Deck, new Vector3(s, 0, 0), false);
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
                    p.Cylinder(V(x, (RiverY - 1.2f) * .5f, z), .2f, -RiverY, E4.Deck, 8);
            for (int s = -1; s <= 1; s += 2)
            {
                p.Box(V(s * (K.Edge + .12f), K.Kerb + 1.05f, (z0 + z1) * .5f), V(.12f, .12f, z1 - z0), E4.Timber);
                p.Box(V(s * (K.Edge + .12f), K.Kerb + .55f, (z0 + z1) * .5f), V(.06f, .1f, z1 - z0), E4.TimberDark);
                for (float z = z0 + .5f; z < z1; z += 2.5f) p.Box(V(s * (K.Edge + .12f), K.Kerb + .5f, z), V(.16f, 1.2f, .16f), E4.Deck);
            }
        }

        /// <summary>The railway on a steel girder bridge from z0 to z1 over water at level: deck, plate girders, piers,
        /// a walkway rail; the tracks go on across it.</summary>
        static void RailGirder(CqChunkDraft d, float z0, float z1, float level, int piers)
        {
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Metal;
            for (float z = z0; z < z1 - .01f; z += 8f)
            {
                float e = Mathf.Min(z1, z + 8f);
                K.MinMaxBox(m, V(E4.BedX0, E4.BedTop - .35f, z), V(E4.BedX1, E4.BedTop, e), E4.BallastDark);
                K.MinMaxBox(m, V(E4.BedX0 - .25f, E4.BedTop - 1.5f, z), V(E4.BedX0 + .1f, E4.BedTop + .5f, e), K.Hex("4D6B62"));
                K.MinMaxBox(m, V(E4.BedX1 - .1f, E4.BedTop - 1.5f, z), V(E4.BedX1 + .25f, E4.BedTop + .5f, e), K.Hex("4D6B62"));
            }
            for (float z = z0 + 2f; z < z1 - .5f; z += 4f)
                m.Box(V(E4.BedX0 - .27f, E4.BedTop - .5f, z), V(.08f, 2f, .12f), K.Hex("3B544D"));
            E4.Tracks(d.Road, z0, z1, E4.BedTop);
            for (int i = 0; i < piers; i++)
            {
                float z = Mathf.Lerp(z0, z1, (i + .5f) / piers) + (piers == 3 ? 0f : 0f);
                m.Surface = ComicSurface.Concrete;
                m.Box(V((E4.BedX0 + E4.BedX1) * .5f, (level - 1.4f + E4.BedTop - 1.5f) * .5f, z), V(E4.BedX1 - E4.BedX0 - 1f, E4.BedTop - 1.5f - (level - 1.4f), 1.2f), E4.Concrete);
                m.Box(V((E4.BedX0 + E4.BedX1) * .5f, E4.BedTop - 1.7f, z), V(E4.BedX1 - E4.BedX0 + .4f, .4f, 1.6f), E4.ConcreteDark);
            }
        }

        // ---------------------------------------------------------------- reserve furniture and landmarks

        /// <summary>A picnic shelter: four timber posts on a slab, a hipped green iron roof with deep eaves, a table
        /// with two benches, a barbecue plate on a brick plinth beside it.</summary>
        static void PicnicShelter(ComicMesh f, Vector3 p)
        {
            f.Surface = ComicSurface.Concrete;
            f.Box(p + V(0, .06f, 0), V(5.6f, .12f, 4.6f), K.Hex("D9D2C2"));
            f.Surface = ComicSurface.Wood;
            foreach (float x in new[] { -2.3f, 2.3f })
                foreach (float z in new[] { -1.8f, 1.8f })
                {
                    f.Box(p + V(x, 1.4f, z), V(.2f, 2.7f, .2f), E4.TimberDark);
                    f.Beam(p + V(x, 2.2f, z), p + V(x * .72f, 2.72f, z), .1f, E4.TimberDark);
                }
            f.Box(p + V(0, 2.78f, -1.8f), V(5f, .16f, .16f), E4.TimberDark);
            f.Box(p + V(0, 2.78f, 1.8f), V(5f, .16f, .16f), E4.TimberDark);
            f.Surface = ComicSurface.Metal;
            var roof = new E4Plane(p + V(3.3f, 0, -2.8f), Vector3.forward, Vector3.right);
            E4.RidgeRoof(f, roof, 0f, 5.6f, 2.85f, 4.25f, 0f, -6.6f, E4.HeritageGreen, 2.2f);
            f.Surface = ComicSurface.PaintedMetal;
            f.Box(p + V(0, 2.84f, 0), V(6.7f, .1f, 5.7f), K.R.ink);
            f.Surface = ComicSurface.Wood;
            f.Box(p + V(0, .78f, 0), V(2.2f, .08f, .9f), E4.Timber);
            foreach (float z in new[] { -.75f, .75f }) f.Box(p + V(0, .48f, z), V(2.2f, .07f, .32f), E4.Timber);
            f.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.8f, .8f }) { f.Box(p + V(x, .4f, 0), V(.08f, .7f, 1.7f), K.R.ink); }
            f.Surface = ComicSurface.Brick;
            f.Box(p + V(3.7f, .45f, 1f), V(1.1f, .9f, .8f), E4.Brick);
            f.Surface = ComicSurface.Metal;
            f.Box(p + V(3.7f, .93f, 1f), V(1.2f, .07f, .9f), E4.SteelDark);
        }

        /// <summary>The meeting place: a ring of low sandstone boulders round a circle of raked ochre gravel, one
        /// taller standing stone. No figures, no artwork: a quiet place.</summary>
        static void StoneCircle(ComicMesh f, Vector3 p)
        {
            f.Surface = ComicSurface.Concrete;
            f.Cylinder(p + V(0, .04f, 0), 3.4f, .08f, K.Hex("D7A86A"), 10);
            f.Cylinder(p + V(0, .09f, 0), 2.2f, .04f, K.Hex("C99357"), 10);
            var r = new System.Random(9);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2f / 9f + .2f;
                Vector3 c = p + V(Mathf.Cos(a) * 3.1f, 0, Mathf.Sin(a) * 3.1f);
                float s = F(r, .7f, 1.05f);
                Color stone = Color.Lerp(K.Hex("D9C39A"), K.Hex("B99C72"), F(r, 0f, 1f));
                f.Box(c + V(0, s * .3f, 0), V(s * 1.1f, s * .62f, s * .85f), stone, Quaternion.Euler(F(r, -6f, 6f), a * Mathf.Rad2Deg + F(r, -20f, 20f), F(r, -6f, 6f)));
                f.Box(c + V(0, s * .62f, 0), V(s * .75f, s * .22f, s * .6f), Color.Lerp(stone, Color.white, .15f), Quaternion.Euler(0, a * Mathf.Rad2Deg + 20f, F(r, -5f, 5f)));
            }
            f.Box(p + V(-4.1f, .85f, -.6f), V(.75f, 1.7f, .55f), K.Hex("C8AC80"), Quaternion.Euler(0, 25f, 4f));
            f.Box(p + V(-4.1f, 1.78f, -.6f), V(.55f, .25f, .42f), K.Hex("D9C39A"), Quaternion.Euler(0, 25f, 4f));
        }

        /// <summary>An interpretive sign: a sloping panel on two weathered-steel legs. The panel shows only the bend of
        /// the river (a pale line on ochre) and lines of text.</summary>
        static void InterpretiveSign(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0), tilt = q * Quaternion.Euler(-35f, 0, 0);
            m.Surface = ComicSurface.Metal;
            foreach (float x in new[] { -.55f, .55f }) m.Box(p + q * V(x, .5f, 0), V(.1f, 1f, .14f), K.Hex("8A5A3C"), q);
            m.Box(p + q * V(0, 1.06f, 0), V(1.6f, .08f, .95f), K.Hex("8A5A3C"), tilt);
            m.Surface = ComicSurface.Plaster;
            m.Box(p + q * V(0, 1.1f, 0) + tilt * V(0, .02f, 0), V(1.46f, .05f, .82f), K.Hex("E9D9AE"), tilt);
            Vector3 o = p + q * V(0, 1.1f, 0) + tilt * V(0, .05f, 0);
            m.Surface = ComicSurface.Plain;
            void Q(float x0, float x1, float z0, float z1, Color c) =>
                K.Face(m, o + tilt * V(x0, 0, z0), o + tilt * V(x0, 0, z1), o + tilt * V(x1, 0, z1), o + tilt * V(x1, 0, z0), c, tilt * Vector3.up, false);
            Q(-.66f, -.05f, -.3f, .3f, K.Hex("D7A86A"));
            o += tilt * V(0, .004f, 0);
            Q(-.6f, -.42f, -.05f, .05f, E4.RiverLight); Q(-.46f, -.3f, .0f, .16f, E4.RiverLight); Q(-.34f, -.12f, .1f, .2f, E4.RiverLight);
            for (int i = 0; i < 5; i++) Q(.08f, i % 2 == 0 ? .62f : .5f, .26f - i * .12f, .22f - i * .12f, K.R.ink);
        }

        /// <summary>The floating fishing platform: a timber pontoon between two tall black piles with white caps, a low
        /// rail, a gangway back to the bank (towards +X).</summary>
        static void FishingPlatform(ComicMesh m, Vector3 p, Vector3 landing)
        {
            m.Surface = ComicSurface.Wood;
            m.Box(p + V(0, .25f, 0), V(3.4f, .3f, 6f), E4.DeckLight);
            m.Box(p + V(0, .08f, 0), V(3.6f, .2f, 6.2f), K.Hex("4C5258"));
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float z in new[] { -3.4f, 3.4f })
            {
                m.Cylinder(p + V(0, 1.3f, z), .22f, 4.6f, K.R.ink, 8);
                m.Cylinder(p + V(0, 3.72f, z), .25f, .26f, E4.White, 8, .5f);
            }
            m.Surface = ComicSurface.Metal;
            m.Box(p + V(-1.6f, 1.3f, 0), V(.07f, .07f, 5.6f), E4.Steel);
            foreach (float z in new[] { -2.8f, -1f, 1f, 2.8f }) m.Box(p + V(-1.6f, .85f, z), V(.07f, .95f, .07f), E4.Steel);
            m.Surface = ComicSurface.Wood;
            Vector3 g0 = p + V(1.7f, .4f, 0), g1 = landing;
            foreach (float z in new[] { -.6f, .6f }) m.Beam(g0 + V(0, -.08f, z), g1 + V(0, -.08f, z), .14f, E4.Deck);
            K.Face(m, g0 + V(0, 0, -.6f), g0 + V(0, 0, .6f), g1 + V(0, 0, .6f), g1 + V(0, 0, -.6f), E4.DeckLight, Vector3.up);
            m.Surface = ComicSurface.Metal;
            foreach (float z in new[] { -.6f, .6f })
            {
                m.Beam(g0 + V(0, 1f, z), g1 + V(0, 1f, z), .06f, E4.Steel);
                for (int i = 0; i <= 4; i++) { Vector3 b = Vector3.Lerp(g0, g1, i / 4f) + V(0, 0, z); m.Box(b + V(0, .5f, 0), V(.06f, 1f, .06f), E4.Steel); }
            }
        }

        /// <summary>A black swan on the water (scenery): black body, raised wing ruffles, S neck, red bill with a white tip.</summary>
        static void BlackSwan(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            Color black = K.Hex("23262B"), ruffle = K.Hex("3A3F47");
            m.Surface = ComicSurface.Fabric;
            E4.Blob(m, p + q * V(0, .2f, 0), V(.7f, .5f, 1.25f), black, 6, 3, yaw);
            m.Box(p + q * V(0, .42f, .25f), V(.5f, .22f, .6f), ruffle, q * Quaternion.Euler(-15f, 0, 0));
            m.Box(p + q * V(0, .34f, .68f), V(.2f, .1f, .2f), E4.White, q);
            m.Beam(p + q * V(0, .3f, -.45f), p + q * V(0, .85f, -.62f), .13f, black);
            m.Beam(p + q * V(0, .85f, -.62f), p + q * V(0, 1.02f, -.78f), .12f, black);
            m.Box(p + q * V(0, 1f, -.84f), V(.16f, .16f, .2f), black, q);
            m.Surface = ComicSurface.Plastic;
            m.Box(p + q * V(0, .96f, -1.02f), V(.1f, .08f, .2f), K.Hex("D8352B"), q);
            m.Box(p + q * V(0, .96f, -1.13f), V(.09f, .07f, .04f), E4.White, q);
        }

        // ---------------------------------------------------------------- obstacles and animals

        static ComicMesh PathBollardMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .06f, 0), .2f, .12f, K.R.ink, 8);
            m.Cylinder(V(0, .52f, 0), .13f, .86f, E4.White, 8);
            m.Cylinder(V(0, .74f, 0), .14f, .16f, K.Hex("D8352B"), 8);
            m.Cylinder(V(0, 1f, 0), .14f, .1f, K.R.ink, 8, .5f);
            return m;
        }

        static ComicMesh FallenBranchMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            E4.Limb(m, V(-1.1f, .16f, .1f), V(.5f, .22f, -.1f), .16f, .1f, E4.GumBark, 6);
            E4.Limb(m, V(.5f, .22f, -.1f), V(1.1f, .5f, .2f), .1f, .04f, E4.GumBarkGrey, 6);
            E4.Limb(m, V(-.2f, .2f, 0f), V(.2f, .55f, .42f), .08f, .03f, E4.GumBark, 6);
            E4.Limb(m, V(.1f, .2f, -.05f), V(.6f, .46f, -.42f), .07f, .03f, E4.GumBarkGrey, 6);
            m.Surface = ComicSurface.Foliage;
            E4.Clump(m, V(.95f, .46f, .15f), V(.9f, .5f, .8f), E4.GumA, 1, 6, 3);
            E4.Clump(m, V(.3f, .5f, .4f), V(.7f, .42f, .6f), E4.GumC, 2, 6, 3);
            E4.Clump(m, V(.6f, .42f, -.38f), V(.7f, .4f, .5f), E4.GumDark, 3, 6, 3);
            return m;
        }

        static ComicMesh ParkBinMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Wood };
            m.Box(V(0, .06f, 0), V(.84f, .12f, .84f), K.R.ink);
            for (int i = 0; i < 4; i++)
            {
                Quaternion q = Quaternion.Euler(0, i * 90f, 0);
                foreach (float x in new[] { -.26f, 0f, .26f }) m.Box(q * V(x, .56f, .36f), V(.2f, .86f, .05f), E4.Timber, q);
            }
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .55f, 0), V(.62f, .84f, .62f), K.Hex("2F6B45"));
            m.Surface = ComicSurface.PaintedMetal;
            m.Box(V(0, 1.03f, 0), V(.86f, .1f, .86f), E4.HeritageGreen);
            m.Box(V(0, 1.1f, 0), V(.5f, .05f, .5f), K.R.ink);
            return m;
        }

        // A shared-path sign: a post with a blue plate (a white bar for the rider above one for the walker), faces -Z.
        static ComicMesh PathSignMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.PaintedMetal };
            m.Cylinder(V(0, .06f, 0), .16f, .12f, K.R.ink, 8);
            m.Cylinder(V(0, 1.1f, 0), .05f, 2.2f, E4.Steel, 8);
            m.Box(V(0, 1.85f, -.04f), V(.6f, .8f, .05f), E4.White);
            m.Box(V(0, 1.85f, -.06f), V(.52f, .72f, .04f), K.Hex("2466B0"));
            m.Surface = ComicSurface.Plain;
            void Q(float x0, float x1, float y0, float y1) => m.Quad(V(x1, y0, -.085f), V(x0, y0, -.085f), V(x0, y1, -.085f), V(x1, y1, -.085f), E4.White, false);
            // a bicycle (two wheels and a frame) above a walker
            Q(-.19f, -.07f, 1.92f, 2.04f); Q(.07f, .19f, 1.92f, 2.04f); Q(-.13f, .13f, 2.02f, 2.06f); Q(-.02f, .04f, 2.04f, 2.14f);
            Q(-.2f, .2f, 1.83f, 1.86f);
            Q(-.04f, .04f, 1.7f, 1.78f); Q(-.05f, .05f, 1.6f, 1.7f); Q(-.07f, -.02f, 1.52f, 1.6f); Q(.02f, .07f, 1.52f, 1.6f);
            return m;
        }

        // A Pacific black duck leading three ducklings (one animal: they move together). Facing -Z.
        static ComicMesh DuckFamilyMesh()
        {
            var m = new ComicMesh();
            void Duck(Vector3 p, float s, bool duckling)
            {
                Color body = duckling ? K.Hex("E6C860") : K.Hex("7A5E44"), dark = duckling ? K.Hex("8A6B3C") : K.Hex("4E3B2C"), face = duckling ? K.Hex("F0DC8C") : K.Hex("E8D8B0");
                m.Surface = ComicSurface.Fabric;
                E4.Blob(m, p + V(0, .2f * s, .05f * s), V(.28f, .26f, .46f) * s, body, 6, 3, .3f);
                m.Box(p + V(0, .26f * s, .28f * s), V(.14f, .1f, .14f) * s, dark, Quaternion.Euler(-25f, 0, 0));
                m.Box(p + V(0, .34f * s, -.16f * s), V(.14f, .2f, .14f) * s, body);
                m.Box(p + V(0, .46f * s, -.2f * s), V(.17f, .15f, .2f) * s, face);
                m.Box(p + V(0, .5f * s, -.2f * s), V(.18f, .05f, .21f) * s, dark);
                m.Surface = ComicSurface.Plastic;
                m.Box(p + V(0, .43f * s, -.35f * s), V(.1f, .05f, .14f) * s, duckling ? K.Hex("C98A3A") : K.Hex("5F6B52"));
                foreach (float x in new[] { -.06f, .06f }) m.Box(p + V(x * s, .04f * s, 0), V(.07f, .08f, .12f) * s, K.Hex("D98A3A"));
                if (!duckling) foreach (float x in new[] { -.15f, .15f }) { m.Surface = ComicSurface.Fabric; m.Box(p + V(x * s, .22f * s, .08f * s), V(.04f, .12f, .26f) * s, K.Hex("3F7A5E")); }
            }
            Duck(V(0, 0, 0), 1.25f, false);
            Duck(V(-.08f, 0, .55f), .62f, true);
            Duck(V(.1f, 0, .88f), .62f, true);
            Duck(V(-.06f, 0, 1.2f), .62f, true);
            return m;
        }

        // An Australian white ibis: white body, black bald head and long down-curved bill, black tail plumes, long legs. Facing -Z.
        static ComicMesh IbisMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color white = K.Hex("F2F0E8"), black = K.Hex("22252A"), leg = K.Hex("5A4A48");
            E4.Blob(m, V(0, .62f, .08f), V(.34f, .34f, .62f), white, 6, 3, .3f);
            m.Box(V(0, .68f, .4f), V(.2f, .14f, .22f), black, Quaternion.Euler(-20f, 0, 0));
            foreach (float x in new[] { -.17f, .17f }) m.Box(V(x, .64f, .1f), V(.05f, .2f, .42f), K.Hex("E2DED2"));
            m.Beam(V(0, .7f, -.16f), V(0, .98f, -.3f), .1f, white);
            m.Box(V(0, 1.04f, -.34f), V(.12f, .13f, .16f), black);
            m.Surface = ComicSurface.Plastic;
            m.Beam(V(0, 1.03f, -.4f), V(0, .94f, -.62f), .055f, black);
            m.Beam(V(0, .94f, -.62f), V(0, .78f, -.72f), .045f, black);
            m.Surface = ComicSurface.PaintedMetal;
            foreach (float x in new[] { -.07f, .07f })
            {
                m.Box(V(x, .24f, .06f), V(.04f, .48f, .04f), leg);
                m.Box(V(x, .02f, 0f), V(.1f, .04f, .16f), leg);
            }
            return m;
        }

        // ---------------------------------------------------------------- far scenery

        /// <summary>
        /// The horizon of the upper Swan (the CBD panorama is hidden out here): on the left the river plain behind the
        /// reserve, tree lines of flooded gum and paperbark, the brown-green river reaches, the pale paddocks and first
        /// vineyards of the Swan Valley, and the Darling Scarp as a long blue-green ridge; on the right the roofs and
        /// gums of Bassendean and Guildford, and the scarp again further off. Sheared with the road's grade. No colliders.
        /// </summary>
        static ComicMesh ShFar(float grade)
        {
            var m = new ComicMesh();
            var r = new System.Random(300);
            Color haze = K.Hex("C8DDD8");
            const float z0 = -220f, z1 = 820f;
            // left: plain beyond the far bank
            E4.FarStrip(m, -K.Edge - .5f, -60f, 330f, z1, K.Kerb - .05f, E4.Lawn);
            E4.FarStrip(m, -60f, -110f, 330f, z1, RiverY - .06f, E4.River);
            E4.FarStrip(m, -110f, -260f, z0, z1, -.3f, E4.Haze(K.Hex("86A35C"), 170f, haze));
            E4.FarStrip(m, -260f, -520f, z0, z1, -.35f, E4.Haze(K.Hex("B9B878"), 380f, haze));
            E4.FarStrip(m, -520f, -1400f, z0, z1, -.4f, E4.Haze(K.Hex("9AAE7A"), 700f, haze));
            E4.TreeLine(m, r, -122f, 10f, z0, z1, 11f, 13f, K.Hex("5E7E46"), K.Hex("7C9A58"), haze, 0f);
            E4.TreeLine(m, r, -200f, 25f, z0, z1, 17f, 12f, K.Hex("58764A"), K.Hex("6F8F52"), haze, 0f);
            E4.TreeLine(m, r, -330f, 40f, z0, z1, 26f, 14f, K.Hex("5F7D52"), K.Hex("7A9460"), haze, 0f);
            // a reach of the river curling away through the trees
            for (float z = 60f; z < 520f; z += 20f)
            {
                float x = -150f - 60f * Mathf.Sin((z - 60f) * .008f);
                m.Surface = ComicSurface.Plain;
                K.Face(m, V(x - 16f, -.2f, z), V(x - 16f - 1.2f, -.2f, z + 20f), V(x + 16f - 1.2f, -.2f, z + 20f), V(x + 16f, -.2f, z), E4.Haze(E4.River, 200f, haze), Vector3.up, false);
            }
            // vineyard blocks on the valley floor (rows as dark stripes)
            for (int b = 0; b < 7; b++)
            {
                float bx = -290f - (b % 3) * 70f, bz = z0 + 60f + b * 140f;
                for (int row = 0; row < 9; row++)
                {
                    float x = bx - row * 5f;
                    m.Surface = ComicSurface.Foliage;
                    m.Box(V(x, .9f, bz + 30f), V(2.2f, 2f, 60f), E4.Haze(K.Hex("5F8A3E"), Mathf.Abs(x), haze));
                }
            }
            // right: Bassendean / Guildford roofs and gums
            E4.FarStrip(m, 70f, 260f, z0, z1, -.25f, E4.Haze(K.Hex("93A866"), 160f, haze));
            E4.FarStrip(m, 260f, 1400f, z0, z1, -.3f, E4.Haze(K.Hex("9AAE7A"), 600f, haze));
            Color[] walls = { E4.Cream, E4.White, E4.Brick, K.Hex("D9CBB0") };
            Color[] roofs = { E4.Tile, E4.Iron, E4.IronDark, K.Hex("8A6A58") };
            for (float z = z0; z < z1; z += F(r, 12f, 18f))
                for (int row = 0; row < 3; row++)
                {
                    float x = 74f + row * 28f + F(r, 0f, 12f), dist = Mathf.Sqrt(x * x + z * z * .1f);
                    float w = F(r, 9f, 13f), h = F(r, 3.2f, 5f);
                    m.Surface = ComicSurface.Plaster;
                    m.Box(V(x, h * .5f - 1f, z), V(w, h + 2f, w * .8f), E4.Haze(walls[r.Next(walls.Length)], dist, haze));
                    m.Surface = ComicSurface.Metal;
                    m.Cylinder(V(x, h + .8f, z), w * .74f, 2.2f, E4.Haze(roofs[r.Next(roofs.Length)], dist, haze), 4, .15f, Quaternion.Euler(0, 45, 0));
                }
            E4.TreeLine(m, r, 66f, 6f, z0, z1, 14f, 12f, K.Hex("5E7E46"), K.Hex("7C9A58"), haze, 0f);
            E4.TreeLine(m, r, 170f, 30f, z0, z1, 20f, 14f, K.Hex("58764A"), K.Hex("6F8F52"), haze, 0f);
            E4.TreeLine(m, r, 300f, 50f, z0, z1, 30f, 15f, K.Hex("5F7D52"), K.Hex("7A9460"), haze, 0f);
            // the Darling Scarp: nearer on the left (the valley side), further on the right
            E4.Ridge(m, -1, 560f, 260f, z0, z1, -.4f, z => 46f + 9f * Mathf.Sin(z * .007f + 1.2f) + 5f * Mathf.Sin(z * .021f), K.Hex("8CA070"), K.Hex("6F8C7C"), K.Hex("7F9A92"), haze, .42f);
            E4.Ridge(m, 1, 640f, 260f, z0, z1, -.4f, z => 40f + 8f * Mathf.Sin(z * .006f + 2.4f) + 4f * Mathf.Sin(z * .019f), K.Hex("8CA070"), K.Hex("6F8C7C"), K.Hex("7F9A92"), haze, .5f);
            m.Shear(grade);
            return m;
        }
    }
}
