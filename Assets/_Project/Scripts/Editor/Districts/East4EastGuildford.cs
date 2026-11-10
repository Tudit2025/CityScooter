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
    // EAST GUILDFORD (route order 320): the shared path past white stables and paddocks and East Guildford station, then
    // Terrace Rd along Guildford Grammar School: the red-brick school house, the Chapel of St Mary and St George (the
    // hero of the district), and the ovals by the river.
    public static partial class East4Builder
    {
        // The station forecourt ramp (Station chunk): the path climbs to platform height, runs level, and drops off the end.
        public const float StationRampZ = 9f, StationRampRise = 6f, StationRampHeight = 1f, StationRampTop = 5f;
        /// <summary>Where the chapel stands in its chunk: the middle of its west front (x, z).</summary>
        public const float ChapelX = -(K.Edge + 13.5f), ChapelZ = 6f;

        static void BuildEastGuildford(TrackTheme theme, bool atlas)
        {
            const string id = EastGuildfordId;
            K.Begin(MeshFolder(id), PrefabFolder(id), CbdBuilder.DataFolder, PatternFolder(id));
            Obstacle bale = K.SaveObstacle(Pf(id, "OBS_Eastguildford_HayBale"), "OBS_Eastguildford_HayBale", HayBaleMesh(), new Vector3(1.1f, .9f, .7f), ObstacleKind.Thing, true, true, 1f);
            Obstacle bag = K.SaveObstacle(Pf(id, "OBS_Eastguildford_CricketBag"), "OBS_Eastguildford_CricketBag", CricketBagMesh(), new Vector3(1.2f, .5f, .5f), ObstacleKind.Thing, true, true, 1f);
            Obstacle tackle = K.SaveObstacle(Pf(id, "OBS_Eastguildford_TackleBag"), "OBS_Eastguildford_TackleBag", TackleBagMesh(), new Vector3(.55f, 1.55f, .55f), ObstacleKind.Thing, false, true, .8f);
            Obstacle cones = K.SaveObstacle(Pf(id, "OBS_Eastguildford_TrainingCones"), "OBS_Eastguildford_TrainingCones", TrainingConesMesh(), new Vector3(1.5f, .35f, .5f), ObstacleKind.Thing, true, true, .9f);
            GameObject magpie = SaveAnimal(Pf(id, "ANM_Eastguildford_Magpie"), "ANM_Eastguildford_Magpie", MagpieMesh(), new Vector3(0, .22f, 0), new Vector3(.4f, .45f, .7f), .5f, true);
            GameObject kooka = SaveAnimal(Pf(id, "ANM_Eastguildford_Kookaburra"), "ANM_Eastguildford_Kookaburra", KookaburraMesh(), new Vector3(0, .22f, 0), new Vector3(.4f, .45f, .6f), 0f, true);
            Ramp ramp = K.SaveRamp(Pf(id, "RMP_Eastguildford_PlatformRamp"), "RMP_Eastguildford_PlatformRamp", 3f, 5f, 1.2f, 1.5f, 1f, .5f, E4.Concrete, E4.ConcreteDark, true);

            K.WriteChunk(EgEntry(), ChunkPath(id, "Entry"));
            K.WriteChunk(EgStables(), ChunkPath(id, "Stables"));
            K.WriteChunk(EgStation(), ChunkPath(id, "Station"));
            K.WriteChunk(EgSchool(), ChunkPath(id, "School"));
            K.WriteChunk(EgChapel(), ChunkPath(id, "Chapel"));
            K.WriteChunk(EgOvals(), ChunkPath(id, "Ovals"));
            K.WriteChunk(EgExit(), ChunkPath(id, "Exit"));
            GameObject far = K.SaveFarScenery(Pf(id, "FAR_Eastguildford_RiverAndScarp"), "FAR_Eastguildford_RiverAndScarp", EgFar(-EastGuildfordDrop / 40f));

            GameObject cyclist = Cyclist();
            string patterns = PatternFolder(id);
            var own = new List<HazardPattern>
            {
                K.Pattern(patterns, "HZ_Eastguildford_Bales", 40, 0, .8f, true, 30, (bale.gameObject, -2.6f, 18f, 0f), (bale.gameObject, 0f, 18f, 90f), (cones.gameObject, 2.6f, 26f, 0f)),
                K.Pattern(patterns, "HZ_Eastguildford_Training", 40, 0, .8f, true, 30, (tackle.gameObject, -2.6f, 20f, 0f), (tackle.gameObject, 0f, 24f, 0f), (bag.gameObject, 2.6f, 28f, 20f)),
                K.Pattern(patterns, "HZ_Eastguildford_Birds", 40, 0, .7f, true, 30, (magpie, -1.2f, 20f, 0f), (kooka, 1.8f, 26f, 0f)),
            };
            if (cyclist != null) own.Add(K.Pattern(patterns, "HZ_Eastguildford_Cyclists", 50, 0, 1f, true, 30, (cyclist, -2.4f, 24f, 0f), (cyclist, -1.2f, 31f, 0f)));

            District d = Fill(id, "EAST GUILDFORD", 320, EastGuildfordChunks);
            d.obstacles = new[] { bale, bag, tackle, cones };
            d.jumpObstacles = new[] { bale, bag, cones };
            d.hazards = own.Concat(ThemeHazards(theme, "HZ_SidewalkStroll", "HZ_OncomingCar", "HZ_SlowCar", "HZ_Crossing")).ToArray();
            var people = new List<GameObject>(K.People(theme, "Student", "Student", "Student", "Kid", "Jogger", "Jogger", "DogWalker", "Elder", "Office"));
            if (cyclist != null) people.Add(cyclist);
            people.Add(magpie); people.Add(kooka);
            d.pedestrians = people.ToArray();
            d.ramps = new[] { ramp };
            d.rampFrequency = .9f;
            // Clear, bright light on pale stone and red brick; the hills are close.
            d.fogColor = new Color(.83f, .88f, .86f);
            d.skyTint = new Color(1f, 1f, .98f);
            d.ambientSky = new Color(.68f, .82f, .95f);
            d.ambientEquator = new Color(.88f, .88f, .80f);
            d.ambientGround = new Color(.64f, .60f, .48f);
            d.sunColor = new Color(1f, .96f, .87f);
            d.sunIntensity = 1.45f;
            d.farScenery = far;
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            Debug.Log("[Districts/EastGuildford] " + string.Join("; ", K.Report));
            if (!atlas) return;
            var items = new List<(string, GameObject, ComicMesh)>
            {
                ("ChapelWestFront", null, Make(m => Chapel(m, Vector3.zero))),
                ("ChapelFlank", null, Make(m => { var c = new ComicMesh(); Chapel(c, Vector3.zero); m.Add(c, Vector3.zero, Quaternion.Euler(0, 205f, 0), Vector3.one); })),
                ("SchoolHouse", null, Make(m => SchoolHouse(m, new CqLot(-1, -13f, 13f, 9f, 12f, 0f)))),
                ("Stables", null, Make(m => Stables(m, new CqLot(-1, -11f, 11f, 13f, 7f, 0f)))),
                ("Pavilion", null, Make(m => Pavilion(m, new CqLot(-1, -6f, 6f, 5f, 7f, 0f)))),
                ("Horses", null, Make(m => { Horse(m, Vector3.zero, 20f, K.Hex("8A5A3C"), false); Horse(m, V(2.4f, 0, .6f), -30f, K.Hex("D9D2C2"), true); })),
                ("Scoreboard", null, Make(m => Scoreboard(m, Vector3.zero))),
                ("GoalPosts", null, Make(m => GoalPosts(m, Vector3.zero))),
                ("SchoolSign", null, Make(m => SchoolSign(m, new E4Plane(V(0, 0, -4.5f), Vector3.forward, Vector3.right), 9f))),
                ("StationShelter", null, Make(m => E4.Shelter(m, 0f, 0f, 6f, 0f, E4.White, E4.Iron, E4.HeritageGreen, "EAST GUILDFORD"))),
                ("NorfolkPine", null, Make(m => E4.Pine(m, Vector3.zero, 18f, 5))),
                ("HayBale", bale.gameObject, null),
                ("CricketBag", bag.gameObject, null),
                ("TackleBag", tackle.gameObject, null),
                ("TrainingCones", cones.gameObject, null),
                ("Magpie", magpie, null),
                ("Kookaburra", kooka, null),
                ("PlatformRamp", ramp.gameObject, null),
            };
            K.RenderAtlas("art-src/districts/eastguildford/props-atlas.png", items);
        }

        // ---------------------------------------------------------------- chunks

        static CqChunkDraft EgEntry()
        {
            var d = new CqChunkDraft("Eastguildford_Entry", EastGuildfordDrop);
            E4.SharedPath(d);
            E4.Threshold(d);
            CalmLeft(d, 0f, 14f, 1);
            CalmRail(d, 0f, 14f, 2);
            E4.Ground(d, -1, 14f, 40f, 0f, 70f, K.Hex("8FB05A"), ComicSurface.Foliage);
            E4.PostAndRail(d.SlopedFrontage, -(K.Edge + .5f), 14.5f, 40f, K.Kerb, E4.White);
            E4.SignBoard(d.Props, V(-(K.Edge + 1.8f), d.Ground(17f), 17f), 0f, "EAST GUILDFORD", .09f, E4.HeritageGreen, E4.Cream);
            E4.Gum(d.Props, V(-(K.Edge + 5f), d.Ground(24f), 24f), 17f, 11);
            E4.Gum(d.Props, V(-(K.Edge + 10f), d.Ground(35f), 35f), 19f, 12);
            Horse(d.Props, V(-(K.Edge + 6f), d.Ground(31f), 31f), 160f, K.Hex("5E4634"), true);
            E4.PathLight(d.Props, V(-(K.Edge + .5f), d.Ground(28f), 28f), 180f);
            BackTrees(d, -1, 14f, 40f, 20f, 36f, 3);
            Railway(d, 4, true, 14f);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft EgStables()
        {
            // White stables behind a white post-and-rail paddock fence: a row of loose boxes under a deep iron roof, a
            // loft gable with its hoist, a cupola and weathervane; horses in the paddock, hay, big gums.
            var d = new CqChunkDraft("Eastguildford_Stables", EastGuildfordDrop);
            E4.SharedPath(d);
            E4.Ground(d, -1, 0f, 40f, 0f, 70f, K.Hex("8FB05A"), ComicSurface.Foliage);
            for (float z = 2f; z < 38f; z += 9f) E4.Patch(d.Road, -(K.Edge + 11f), -(K.Edge + .7f), z, z + 4f, K.Kerb + .035f, K.Hex("A2BE66"));
            E4.Patch(d.Road, -(K.Edge + 13.2f), -(K.Edge + 11f), 7f, 33f, K.Kerb + .04f, K.Hex("D8C79C"), ComicSurface.Concrete);
            ComicMesh s = d.SlopedFrontage;
            E4.PostAndRail(s, -(K.Edge + .5f), 0f, 40f, K.Kerb, E4.White);
            E4.PostAndRail(s, -(K.Edge + 11f), 0f, 7f, K.Kerb, E4.White);
            E4.PostAndRail(s, -(K.Edge + 11f), 33f, 40f, K.Kerb, E4.White);
            foreach (float z in new[] { 7f, 33f })
                for (int i = 0; i < 3; i++) { s.Surface = ComicSurface.Wood; s.Box(V(-(K.Edge + 5.75f), K.Kerb + .4f + i * .4f, z), V(10.5f, .12f, .06f), E4.White); }
            var lot = new CqLot(-1, 9f, 31f, K.Edge + 13.2f, 7f, d.Base(20f));
            Stables(d.Frontage, lot);
            d.Wall(lot, 6f);
            Horse(d.Props, V(-(K.Edge + 4.5f), d.Ground(12f), 12f), 70f, K.Hex("8A5A3C"), true);
            Horse(d.Props, V(-(K.Edge + 6.5f), d.Ground(27f), 27f), -110f, K.Hex("D9D2C2"), false);
            Horse(d.Props, V(-(K.Edge + 3.2f), d.Ground(36f), 36f), 15f, K.Hex("3E3028"), false);
            // hay by the yard gate, a water trough
            for (int i = 0; i < 3; i++) HayBaleArt(d.Props, V(-(K.Edge + 9.6f), d.Ground(8.2f) + (i == 2 ? .45f : 0f), 8.2f + (i == 1 ? .95f : i == 2 ? .45f : 0f)), 90f);
            d.Props.Surface = ComicSurface.Metal;
            d.Props.Box(V(-(K.Edge + 2.2f), d.Ground(20f) + .3f, 20f), V(.7f, .6f, 2f), E4.SteelDark);
            d.Props.Box(V(-(K.Edge + 2.2f), d.Ground(20f) + .58f, 20f), V(.56f, .04f, 1.86f), E4.RiverLight);
            E4.Gum(d.Props, V(-(K.Edge + 8.5f), d.Ground(3f), 3f), 19f, 21);
            E4.Gum(d.Props, V(-(K.Edge + 9f), d.Ground(37f), 37f), 21f, 22);
            BackTrees(d, -1, 0f, 40f, 24f, 40f, 23, 7f, 14f, 19f);
            Railway(d, 24);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft EgStation()
        {
            // East Guildford station: the platform and its white brick shelter, the yellow-railed pedestrian crossing;
            // the path climbs the station forecourt ramp to platform height and drops off its end (a jump). On the left
            // the school's tennis courts behind a tall fence.
            var d = new CqChunkDraft("Eastguildford_Station", EastGuildfordDrop);
            E4.SharedPath(d);
            E4.Ground(d, -1, 0f, 40f, 0f, 70f, E4.Lawn, ComicSurface.Foliage);
            TennisCourt(d, -(K.Edge + 3.5f), -(K.Edge + 14.5f), 5f, 29f);
            E4.Hedge(d.SlopedFrontage, -1, 0f, 2f, .4f, 1.3f, .8f, K.Hex("4E7F3A"));
            E4.Hedge(d.SlopedFrontage, -1, 32f, 40f, .4f, 1.3f, .8f, K.Hex("4E7F3A"));
            E4.Pine(d.Props, V(-(K.Edge + 5f), d.Ground(34.5f), 34.5f), 18f, 31);
            E4.Gum(d.Deep, V(-(K.Edge + 20f), d.Ground(10f), 10f), 18f, 32, true);
            E4.Gum(d.Deep, V(-(K.Edge + 22f), d.Ground(27f), 27f), 16f, 33, true);
            BackTrees(d, -1, 0f, 40f, 28f, 44f, 34, 8f, 14f, 19f);
            // the ramp on the path
            var spec = new CqRampSpec { Name = "StationRamp", X = 0f, Z = StationRampZ, Width = 2f * K.Half, Rise = StationRampRise, Height = StationRampHeight, Top = StationRampTop, Launch = 1.1f, Hang = .5f };
            d.Ramps.Add(spec);
            var art = new ComicMesh();
            K.RampMesh(art, spec, E4.PlatformRed, E4.Concrete, true, 0f);
            art.Surface = ComicSurface.Plain;
            float lip = spec.Lip;
            foreach (int s in new[] { -1, 1 })
            {
                K.Face(art, V(s * (K.Half - .5f), StationRampHeight + .012f, spec.Z + spec.Rise), V(s * (K.Half - .5f), StationRampHeight + .012f, lip - .5f), V(s * (K.Half - .2f), StationRampHeight + .012f, lip - .5f), V(s * (K.Half - .2f), StationRampHeight + .012f, spec.Z + spec.Rise), E4.White, Vector3.up, false);
                // the yellow tactile line of a platform edge along both sides of the level part (no rails: riders on the
                // verges cross on and off the ramp's sides)
                K.Face(art, V(s * (K.Half - 1.1f), StationRampHeight + .012f, spec.Z + spec.Rise), V(s * (K.Half - 1.1f), StationRampHeight + .012f, lip - .5f), V(s * (K.Half - .7f), StationRampHeight + .012f, lip - .5f), V(s * (K.Half - .7f), StationRampHeight + .012f, spec.Z + spec.Rise), E4.Yellow, Vector3.up, false);
            }
            d.SlopedFrontage.Add(art, Vector3.zero, Quaternion.identity, Vector3.one);
            // the station
            E4.RailCorridor(d, 0f, 40f, false);
            E4.Gantry(d, 20f);
            E4.Wires(d, 0f, 40f);
            E4.RailBackdrop(d, 0f, 40f, 35);
            E4.Platform(d, 6f, 33f);
            float top = E4.RailY + .95f;
            ComicMesh m = d.SlopedFrontage;
            E4.PlatformRamp(m, 2f, 6f);
            E4.Shelter(m, K.Edge + 1.3f, 24f, 7f, top, E4.White, E4.Iron, E4.HeritageGreen, "EAST GUILDFORD");
            E4.PlatformFurniture(m, top, 9f, 17f, 30.5f);
            m.Surface = ComicSurface.PaintedMetal;
            for (float z = 8f; z < 20f; z += 3f) m.Box(V(K.Edge + 1.25f, top + .55f, z), V(.07f, 1.1f, .07f), E4.Steel);
            m.Box(V(K.Edge + 1.25f, top + 1.05f, 13.5f), V(.07f, .07f, 12f), E4.Yellow);
            // the pedestrian crossing over the tracks: a yellow-railed maze and its warning lights
            for (int i = 0; i < 2; i++)
            {
                float z = 35f + i * 1.6f, x0 = K.Edge + 1f + i * .7f;
                m.Box(V(x0 + .9f, K.Kerb + 1.05f, z), V(1.8f, .07f, .07f), E4.Yellow);
                m.Box(V(x0 + .9f, K.Kerb + .55f, z), V(1.8f, .05f, .05f), E4.Yellow);
                foreach (float x in new[] { x0, x0 + 1.8f }) m.Box(V(x, K.Kerb + .55f, z), V(.07f, 1.1f, .07f), E4.Yellow);
            }
            E4.Patch(d.Road, K.Edge, E4.BedX1 + 1f, 35.3f, 37.3f, E4.RailY + .012f, E4.Concrete, ComicSurface.Concrete);
            m.Cylinder(V(K.Edge + .8f, 2f, 38f), .06f, 3.6f, E4.Steel, 6);
            m.Box(V(K.Edge + .8f, 3.5f, 38f), V(.7f, .7f, .06f), K.R.ink, Quaternion.Euler(0, 0, 45));
            m.Box(V(K.Edge + .8f, 3.5f, 37.96f), V(.5f, .5f, .04f), E4.Yellow, Quaternion.Euler(0, 0, 45));
            m.Box(V(K.Edge + .8f, 2.75f, 38f), V(.9f, .3f, .1f), K.R.ink);
            foreach (float x in new[] { -.25f, .25f }) m.Cylinder(V(K.Edge + .8f + x, 2.75f, 37.93f), .11f, .06f, K.R.red, 8, 1f, Quaternion.Euler(90, 0, 0));
            E4.StationSign(d.Props, V(K.Edge + .7f, d.Ground(3.2f), 3.2f), "EAST GUILDFORD");
            E4.Train(d.SlopedFrontage, E4.Track2, 9f, 1);
            d.GiftSlots.Add(new Vector2(0f, 31f));
            d.Slot(-2.6f, 36f); d.Slot(2.6f, 36f);
            return d;
        }

        static CqChunkDraft EgSchool()
        {
            // Terrace Rd: Guildford Grammar's Federation school house behind its lawn, the brick entrance wall with the
            // school's name, the flagpole.
            var d = new CqChunkDraft("Eastguildford_School", EastGuildfordDrop);
            E4.BrickStreet(d);
            E4.Threshold(d);
            E4.Ground(d, -1, 0f, 40f, 0f, 70f, E4.Lawn, ComicSurface.Foliage);
            for (float z = 1f; z < 38f; z += 9f) E4.Patch(d.Road, -(K.Edge + 8.5f), -(K.Edge + .4f), z, z + 4f, K.Kerb + .035f, E4.LawnLight);
            var lot = new CqLot(-1, 2.5f, 28.5f, K.Edge + 9.5f, 12f, d.Base(15.5f));
            SchoolHouse(d.Frontage, lot);
            d.Wall(lot, 10f);
            // brick path from the footpath to the entrance, bollards and chains along the lawn
            E4.Patch(d.Road, -(K.Edge + 9.5f), -K.Edge, 14.6f, 16.4f, K.Kerb + .045f, E4.PaveBrick, ComicSurface.Paving);
            E4.ChainBollards(d.SlopedFrontage, -(K.Edge + .35f), 1f, 13.5f, K.Kerb);
            E4.ChainBollards(d.SlopedFrontage, -(K.Edge + .35f), 17.5f, 30f, K.Kerb);
            // the entrance wall with the name, facing the road and the rider
            SchoolSign(d.Frontage, new E4Plane(V(-(K.Edge + 1.1f), d.Ground(35.4f), 31f), Vector3.forward, Vector3.right), 8.8f);
            Flagpole(d.Props, V(-(K.Edge + 4f), d.Ground(30f), 30f));
            E4.Jacaranda(d.Props, V(-(K.Edge + 4.5f), d.Ground(8f), 8f), 8f, 41);
            E4.PetalCarpet(d.Road, -(K.Edge + 4.5f), 8f, 2.8f, K.Kerb + .05f, 41);
            E4.Pine(d.Deep, V(-(K.Edge + 26f), d.Ground(34f), 34f), 20f, 42, true);
            BackTrees(d, -1, 0f, 40f, 26f, 40f, 43, 8f, 14f, 19f);
            Railway(d, 44);
            E4.Train(d.SlopedFrontage, E4.Track1, 10f, 2);
            foreach (float z in new[] { 6f, 26f })
            {
                Vector3 p = V(-(K.Half + .55f), d.Ground(z), z);
                E4.HeritageLamp(d.Props, p);
                K.Thin(d.Collisions, "HeritageLamp", p, 4.4f, .35f);
            }
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft EgChapel()
        {
            // The Chapel of St Mary and St George on its lawn: the west front with the twin turrets faces the rider, the
            // buttressed flank faces the road.
            var d = new CqChunkDraft("Eastguildford_Chapel", EastGuildfordDrop);
            E4.BrickStreet(d);
            E4.Ground(d, -1, 0f, 40f, 0f, 70f, E4.Lawn, ComicSurface.Foliage);
            for (float z = 0f; z < 40f; z += 8f) E4.Patch(d.Road, -(K.Edge + 7f), -(K.Edge + .4f), z, z + 4f, K.Kerb + .035f, E4.LawnLight);
            float baseY = d.Ground(ChapelZ + 16f);
            Chapel(d.Frontage, V(ChapelX, baseY, ChapelZ));
            d.Walls.Add(new Bounds(new Vector3(ChapelX, baseY + 8f, ChapelZ + 16.5f), new Vector3(15f, 20f, 34f)));
            // the brick path from the footpath round to the west door, bollards and chains
            E4.Patch(d.Road, ChapelX - 1.4f, -K.Edge, 1.6f, 3.6f, K.Kerb + .045f, E4.PaveBrick, ComicSurface.Paving);
            E4.Patch(d.Road, ChapelX - 1.4f, ChapelX + 1.4f, 3.6f, ChapelZ + .3f, K.Kerb + .045f, E4.PaveBrick, ComicSurface.Paving);
            E4.ChainBollards(d.SlopedFrontage, -(K.Edge + .35f), 4.5f, 39.5f, K.Kerb);
            // trees behind and beyond the chapel (none in front of it)
            E4.Gum(d.Deep, V(ChapelX - 13f, baseY, 4f), 23f, 51, true);
            E4.Gum(d.Deep, V(ChapelX - 15f, baseY, 20f), 25f, 52, true);
            E4.Pine(d.Deep, V(ChapelX - 12f, baseY, 35f), 24f, 53, true);
            E4.Gum(d.Props, V(-(K.Edge + 4.5f), d.Ground(39f), 39f), 15f, 54);
            BackTrees(d, -1, 0f, 40f, 34f, 46f, 55, 8f, 15f, 20f);
            Railway(d, 56);
            foreach (float z in new[] { 16f, 36f })
            {
                Vector3 p = V(-(K.Half + .55f), d.Ground(z), z);
                E4.HeritageLamp(d.Props, p);
                K.Thin(d.Collisions, "HeritageLamp", p, 4.4f, .35f);
            }
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft EgOvals()
        {
            // The ovals: mown stripes behind a white picket fence, the pavilion with its clock, the scoreboard, a cricket
            // pitch and sight screen, the four football posts, the rowing shed by the river beyond.
            var d = new CqChunkDraft("Eastguildford_Ovals", EastGuildfordDrop);
            E4.SharedPath(d);
            E4.Threshold(d);
            E4.Ground(d, -1, 0f, 40f, 0f, 90f, E4.Lawn, ComicSurface.Foliage);
            for (float z = 0f; z < 40f; z += 8f) E4.Patch(d.Road, -(K.Edge + 62f), -(K.Edge + 1.6f), z, z + 4f, K.Kerb + .035f, E4.LawnLight);
            E4.Patch(d.Road, -(K.Edge + 30.5f), -(K.Edge + 27.5f), 12f, 32f, K.Kerb + .05f, K.Hex("D9C89A"), ComicSurface.Concrete);
            E4.Pickets(d.SlopedFrontage, -(K.Edge + 1.2f), 0f, 40f, K.Kerb, .9f, E4.White);
            var lot = new CqLot(-1, 2f, 14f, K.Edge + 5.5f, 7f, d.Base(8f));
            Pavilion(d.Frontage, lot);
            d.Wall(lot, 7f);
            Scoreboard(d.Props, V(-(K.Edge + 5f), d.Ground(22f), 22f));
            GoalPosts(d.Props, V(-(K.Edge + 30f), d.Ground(37f), 37f));
            // stumps at both ends of the pitch, the sight screen behind the far end
            foreach (float z in new[] { 13f, 31f })
                for (int i = -1; i <= 1; i++) { d.Props.Surface = ComicSurface.Wood; d.Props.Box(V(-(K.Edge + 29f) + i * .11f, d.Ground(z) + .36f, z), V(.04f, .72f, .04f), E4.Cream); }
            ComicMesh deep = d.Deep;
            deep.Surface = ComicSurface.Wood;
            Vector3 screen = V(-(K.Edge + 29f), d.Ground(39f), 39f);
            deep.Box(screen + V(0, 2.4f, 0), V(6f, 4f, .2f), E4.White);
            deep.Box(screen + V(0, .3f, 0), V(6.4f, .3f, 1.4f), E4.SteelDark);
            // the rowing shed and its pontoon on the river, far behind the oval
            Vector3 shed = V(-(K.Edge + 72f), d.Ground(18f), 18f);
            deep.Surface = ComicSurface.Plaster;
            deep.Box(shed + V(0, 2.2f, 0), V(9f, 4.4f, 16f), E4.White);
            var roof = new E4Plane(shed + V(4.9f, 0, -8.4f), Vector3.forward, Vector3.right);
            deep.Surface = ComicSurface.Metal;
            E4.RidgeRoof(deep, roof, 0f, 16.8f, 4.4f, 6.6f, 0f, -9.8f, K.Hex("2A3E6E"));
            for (int i = 0; i < 3; i++) deep.Box(shed + V(4.52f, 1.6f, -5f + i * 5f), V(.06f, 3.2f, 3.6f), K.Hex("2A3E6E"));
            E4.Gum(d.Props, V(-(K.Edge + 6f), d.Ground(31f), 31f), 18f, 61);
            BackTrees(d, -1, 0f, 40f, 62f, 80f, 62, 6f, 15f, 21f);
            Railway(d, 63);
            d.StandardSlots();
            return d;
        }

        static CqChunkDraft EgExit()
        {
            var d = new CqChunkDraft("Eastguildford_Exit", EastGuildfordDrop);
            E4.SharedPath(d);
            E4.Ground(d, -1, 0f, 26f, 0f, 70f, E4.Lawn, ComicSurface.Foliage);
            E4.Pickets(d.SlopedFrontage, -(K.Edge + 1.2f), 0f, 12f, K.Kerb, .9f, E4.White);
            E4.Gum(d.Props, V(-(K.Edge + 4f), d.Ground(7f), 7f), 20f, 71);
            E4.Gum(d.Props, V(-(K.Edge + 8f), d.Ground(18f), 18f), 17f, 72);
            E4.Bench(d.Props, V(-(K.Edge + 1.8f), d.Ground(14f), 14f), 90f, E4.Timber);
            E4.PathLight(d.Props, V(-(K.Edge + .5f), d.Ground(22f), 22f), 180f);
            BackTrees(d, -1, 0f, 26f, 16f, 34f, 73);
            CalmLeft(d, 26f, 40f, 74);
            Railway(d, 75, true, 0f, 26f);
            CalmRail(d, 26f, 40f, 76);
            d.StandardSlots();
            return d;
        }

        // ---------------------------------------------------------------- the chapel

        /// <summary>
        /// The Chapel of St Mary and St George, Guildford Grammar School (Sir Walter Tapper, 1912-14; late Gothic
        /// Revival in Donnybrook sandstone). o is the middle of the west front at ground level; the nave runs back
        /// along +Z, so the west front (twin octagonal turrets with stone spires on stepped corner buttresses, the gable
        /// between them, the great traceried window over the small pointed door with its three niches) faces -Z, the
        /// rider coming along the road; the south flank (six bays between deep stepped buttresses with gabled caps
        /// above the parapet, a tall two-light window high in each bay under a relieving arch) faces +X, the road.
        /// Proportions from photographs: front 14 m wide and 17 m to the turret bases, gable 20.6 m, spires 25 m.
        /// </summary>
        static void Chapel(ComicMesh f, Vector3 o)
        {
            Color stone = E4.Stone, light = E4.StoneLight, shade = E4.StoneShade, dark = E4.StoneDark, glass = K.Hex("2B4468");
            Color spire = K.Hex("E2CCA8"), spire2 = K.Hex("CFB894"), recess = K.Hex("C4A47C");
            const float hw = 5.25f, L = 33f, wallTop = 15.6f, frontTop = 17f;
            var W = new E4Plane(o + V(-7f, 0, 0), Vector3.right, Vector3.back);      // the west front: u 0..14
            var S = new E4Plane(o + V(hw, 0, 0), Vector3.forward, Vector3.right);    // the south flank: u = metres along the nave

            // ---- the body
            f.Surface = ComicSurface.Paving;
            f.Box(o + V(0, wallTop * .5f, (2.6f + L) * .5f), V(2f * hw, wallTop, L - 2.6f), stone);
            f.Surface = ComicSurface.Concrete;
            f.Box(o + V(0, .5f, (2.6f + L) * .5f), V(2f * hw + .4f, 1f, L - 2.4f), shade);
            f.Box(o + V(0, 1.08f, (2.6f + L) * .5f), V(2f * hw + .2f, .16f, L - 2.5f), light);
            f.Box(o + V(0, wallTop + .16f, (2.6f + L) * .5f), V(2f * hw + .3f, .34f, L - 2.4f), light);   // parapet coping
            // roof (slate), the east gable
            f.Surface = ComicSurface.Metal;
            E4.RidgeRoof(f, S, 2.8f, L - .2f, wallTop + .1f, wallTop + 4.3f, -.4f, -(2f * hw) + .4f, E4.Slate);
            f.Surface = ComicSurface.Paving;
            K.TriFacing(f, o + V(-hw, wallTop, L), o + V(hw, wallTop, L), o + V(0, wallTop + 4.8f, L), stone, Vector3.forward);
            K.TriFacing(f, o + V(-hw, wallTop, L - .5f), o + V(hw, wallTop, L - .5f), o + V(0, wallTop + 4.8f, L - .5f), stone, Vector3.back);
            // the bell turret (fleche) on the ridge
            Vector3 fl = o + V(0, wallTop + 4.1f, L * .7f);
            f.Surface = ComicSurface.Concrete;
            f.Cylinder(fl + V(0, .8f, 0), .72f, 1.6f, light, 8);
            f.Cylinder(fl + V(0, 1.72f, 0), .9f, .2f, shade, 8);
            f.Cylinder(fl + V(0, 2.85f, 0), .8f, 2.1f, spire, 8, .1f);
            f.Box(fl + V(0, 4.2f, 0), V(.07f, .6f, .07f), K.R.ink);
            for (int i = 0; i < 8; i++)
            {
                float a = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                Vector3 n = V(Mathf.Cos(a), 0, Mathf.Sin(a)), t = V(-Mathf.Sin(a), 0, Mathf.Cos(a)), c = fl + V(0, .85f, 0) + n * .68f;
                K.Face(f, c - t * .16f - V(0, .5f, 0), c + t * .16f - V(0, .5f, 0), c + t * .16f + V(0, .5f, 0), c - t * .16f + V(0, .5f, 0), K.Hex("4A4F58"), n, false);
            }

            // ---- the west front: the wall between the towers and the gable
            f.Surface = ComicSurface.Paving;
            E4.Box(f, W, 3.4f, 10.6f, 0f, frontTop, 0f, -3f, stone);
            E4.GableRoof(f, W, 3.3f, 10.7f, frontTop, 20.6f, 0f, -3f, E4.Slate, stone);
            f.Surface = ComicSurface.Concrete;
            E4.Bar(f, W, 3.3f, frontTop + .05f, 7f, 20.72f, -.05f, .3f, .34f, light);
            E4.Bar(f, W, 10.7f, frontTop + .05f, 7f, 20.72f, -.05f, .3f, .34f, light);
            E4.Box(f, W, 6.68f, 7.32f, 20.5f, 21.15f, -.4f, .12f, light);
            f.Box(W.P(7f, 21.75f, -.15f), V(.12f, 1.2f, .12f), light);
            f.Box(W.P(7f, 21.95f, -.15f), V(.62f, .12f, .12f), light);
            E4.FlatArchWindow(f, W, 7f, .5f, 17.8f, 18.8f, .5f, 0f, light, glass, 3, false, false);
            E4.Box(f, W, 3.4f, 10.6f, 0f, 1f, 0f, .22f, shade);
            E4.Box(f, W, 3.4f, 10.6f, 1f, 1.16f, 0f, .12f, light);
            E4.Box(f, W, 3.4f, 10.6f, 8.3f, 8.52f, 0f, .16f, light);                                   // the string course

            // the door: steps, two orders of moulding, oak leaves
            E4.Box(f, W, 5.1f, 8.9f, 0f, .18f, .2f, 1.4f, shade);
            E4.Box(f, W, 5.4f, 8.6f, .18f, .34f, .2f, .95f, light);
            f.Surface = ComicSurface.PaintedMetal;
            E4.ArchFill(f, W, 7f, 2.3f, .34f, 2.7f, 1.6f, .012f, K.R.ink, 4);
            f.Surface = ComicSurface.Wood;
            E4.ArchFill(f, W, 7f, 2f, .38f, 2.7f, 1.42f, .024f, K.Hex("8A5A34"), 4);
            foreach (float u in new[] { 6.5f, 7f, 7.5f }) E4.Quad(f, W, u - .025f, u + .025f, .4f, u == 7f ? 4.05f : 3.6f, .034f, K.Hex("5E3C22"), false);
            f.Surface = ComicSurface.Concrete;
            E4.ArchTrim(f, W, 7f, 2.5f, .34f, 2.7f, 1.72f, 0f, .26f, .24f, light, 4);
            E4.ArchTrim(f, W, 7f, 3.05f, .34f, 2.7f, 2.02f, 0f, .2f, .1f, shade, 4);
            // the three niches with their figures, the tablet under them
            foreach (float u in new[] { 5.95f, 7f, 8.05f })
            {
                bool mid = u == 7f;
                f.Surface = ComicSurface.Concrete;
                E4.ArchFill(f, W, u, .62f, 5.5f, mid ? 6.9f : 6.6f, .42f, .012f, recess, 3);
                E4.ArchBand(f, W, u, .62f, 5.5f, mid ? 6.9f : 6.6f, .42f, .02f, .1f, light, 3);
                E4.Box(f, W, u - .13f, u + .13f, 5.6f, mid ? 6.75f : 6.45f, .02f, .16f, K.Hex("F6EFD8"));
                E4.Box(f, W, u - .09f, u + .09f, mid ? 6.75f : 6.45f, mid ? 6.98f : 6.66f, .04f, .18f, K.Hex("F6EFD8"));
                E4.Box(f, W, u - .36f, u + .36f, 5.36f, 5.5f, 0f, .2f, light);
            }
            E4.Tri(f, W, new Vector2(6.55f, 7.4f), new Vector2(7.45f, 7.4f), new Vector2(7f, 7.95f), .03f, light);
            E4.Quad(f, W, 6.7f, 7.3f, 4.5f, 5.05f, .014f, shade);

            // the great window: four lights, tracery, the hood mould
            f.Surface = ComicSurface.PaintedMetal;
            E4.ArchFill(f, W, 7f, 4.8f, 8.7f, 12.5f, 3.4f, .012f, K.R.ink, 5);
            f.Surface = ComicSurface.Glass;
            E4.ArchFill(f, W, 7f, 4.5f, 8.9f, 12.5f, 3.2f, .024f, glass, 5);
            f.Surface = ComicSurface.Plain;
            Color[] stained = { K.Hex("B8323A"), K.Hex("D9A62E"), K.Hex("3F7FB5"), K.Hex("7A4EA0"), K.Hex("2F8F6E") };
            for (int i = 0; i < 4; i++)
                for (int k = 0; k < 3; k++)
                {
                    float u = 5.35f + i * 1.1f, v = 9.5f + k * 1.05f + (i % 2) * .3f;
                    E4.Quad(f, W, u - .22f, u + .22f, v, v + .5f, .03f, Color.Lerp(stained[(i + k * 2) % stained.Length], glass, .45f), false);
                }
            f.Surface = ComicSurface.Concrete;
            foreach (float u in new[] { 5.9f, 7f, 8.1f }) E4.Box(f, W, u - .07f, u + .07f, 8.9f, u == 7f ? 14.3f : 12.6f, .02f, .16f, light);
            E4.Box(f, W, 4.8f, 9.2f, 10.7f, 10.82f, .02f, .12f, light);
            E4.ArchTrim(f, W, 5.9f, 2.2f, 12.5f, 12.5f, 1.75f, .02f, .12f, .12f, light, 2, false);
            E4.ArchTrim(f, W, 8.1f, 2.2f, 12.5f, 12.5f, 1.75f, .02f, .12f, .12f, light, 2, false);
            E4.Bar(f, W, 5.9f, 14.25f, 7f, 15.25f, .02f, .11f, .12f, light);
            E4.Bar(f, W, 8.1f, 14.25f, 7f, 15.25f, .02f, .11f, .12f, light);
            E4.ArchTrim(f, W, 7f, 4.95f, 8.8f, 12.5f, 3.5f, 0f, .3f, .22f, light, 5);
            E4.Box(f, W, 4.4f, 9.6f, 8.55f, 8.8f, 0f, .26f, light);                                    // the sill
            foreach (float u in new[] { 4.05f, 9.95f }) { E4.Box(f, W, u - .22f, u + .22f, 12.2f, 12.8f, 0f, .08f, shade); E4.Tri(f, W, new Vector2(u - .22f, 12.2f), new Vector2(u + .22f, 12.2f), new Vector2(u, 11.9f), .08f, shade); }

            // ---- the corner towers (stepped buttress masses) and their turrets
            foreach (int s in new[] { -1, 1 })
            {
                float uc = 7f + s * 5f;
                f.Surface = ComicSurface.Paving;
                E4.Box(f, W, uc - 2f, uc + 2f, 0f, 9.6f, .35f, -3.6f, stone);
                E4.Box(f, W, uc - 1.75f, uc + 1.75f, 9.6f, 14f, .2f, -3.4f, stone);
                E4.Box(f, W, uc - 1.5f, uc + 1.5f, 14f, frontTop + .2f, .05f, -3.1f, stone);
                f.Surface = ComicSurface.Concrete;
                E4.Box(f, W, uc - 2.15f, uc + 2.15f, 0f, 1f, .5f, -3.75f, shade);
                E4.Box(f, W, uc - 2.08f, uc + 2.08f, 1f, 1.16f, .43f, -3.68f, light);
                // set-offs (weathered slopes) at each step
                K.Face(f, W.P(uc - 2f, 9.6f, .35f), W.P(uc + 2f, 9.6f, .35f), W.P(uc + 1.75f, 10.15f, .2f), W.P(uc - 1.75f, 10.15f, .2f), light, (W.N + Vector3.up).normalized);
                K.Face(f, W.P(uc - 1.75f, 14f, .2f), W.P(uc + 1.75f, 14f, .2f), W.P(uc + 1.5f, 14.5f, .05f), W.P(uc - 1.5f, 14.5f, .05f), light, (W.N + Vector3.up).normalized);
                float side = uc + s * 2f, sideB = uc + s * 1.75f, sideC = uc + s * 1.5f;
                Vector3 outN = W.R * s;
                K.Face(f, W.P(side, 9.6f, .35f), W.P(side, 9.6f, -3.6f), W.P(sideB, 10.15f, -3.4f), W.P(sideB, 10.15f, .2f), light, (outN + Vector3.up).normalized);
                K.Face(f, W.P(sideB, 14f, .2f), W.P(sideB, 14f, -3.4f), W.P(sideC, 14.5f, -3.1f), W.P(sideC, 14.5f, .05f), light, (outN + Vector3.up).normalized);
                E4.Box(f, W, uc - 1.62f, uc + 1.62f, frontTop + .2f, frontTop + .48f, .17f, -3.22f, light);
                // the buttress ribs on the west face, stepping back as they rise
                foreach (float du in new[] { -1.55f, 1.55f })
                {
                    float u = uc + du;
                    f.Surface = ComicSurface.Paving;
                    E4.Box(f, W, u - .34f, u + .34f, 0f, 7.4f, .35f, 1.05f, stone);
                    E4.Box(f, W, u - .3f, u + .3f, 7.4f, 12.4f, .2f, .68f, stone);
                    f.Surface = ComicSurface.Concrete;
                    E4.Box(f, W, u - .42f, u + .42f, 0f, 1f, .35f, 1.2f, shade);
                    K.Face(f, W.P(u - .34f, 7.4f, 1.05f), W.P(u + .34f, 7.4f, 1.05f), W.P(u + .3f, 8.1f, .68f), W.P(u - .3f, 8.1f, .68f), light, (W.N + Vector3.up).normalized);
                    K.Face(f, W.P(u - .3f, 12.4f, .68f), W.P(u + .3f, 12.4f, .68f), W.P(u + .3f, 13.2f, .2f), W.P(u - .3f, 13.2f, .2f), light, (W.N + Vector3.up).normalized);
                }
                // the slit crosses between the ribs
                f.Surface = ComicSurface.PaintedMetal;
                foreach (float v in new[] { 3.4f, 6.4f, 11.2f, 15.4f })
                {
                    float o1 = v > 14f ? .062f : v > 9.6f ? .212f : .362f;
                    E4.Quad(f, W, uc - .05f, uc + .05f, v - .42f, v + .42f, o1, K.Hex("3A3F45"), false);
                    E4.Quad(f, W, uc - .2f, uc + .2f, v + .1f, v + .2f, o1, K.Hex("3A3F45"), false);
                }
                // the octagonal turret: drum, the arcaded stage, cornice, the stone spire and its cross
                Vector3 c = W.P(uc, frontTop + .48f, -1.5f);
                Quaternion q = Quaternion.Euler(0, 22.5f, 0);
                f.Surface = ComicSurface.Concrete;
                f.Cylinder(c + V(0, .45f, 0), 1.38f, .9f, shade, 8, 1f, q);
                f.Cylinder(c + V(0, 2.05f, 0), 1.16f, 2.3f, light, 8, 1f, q);
                f.Cylinder(c + V(0, 3.3f, 0), 1.4f, .22f, shade, 8, 1f, q);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * 45f * Mathf.Deg2Rad;
                    Vector3 n = V(Mathf.Cos(a), 0, Mathf.Sin(a)), t = V(-Mathf.Sin(a), 0, Mathf.Cos(a)), pc = c + V(0, 2f, 0) + n * (1.16f * Mathf.Cos(22.5f * Mathf.Deg2Rad) + .012f);
                    K.Face(f, pc - t * .24f - V(0, .8f, 0), pc + t * .24f - V(0, .8f, 0), pc + t * .24f + V(0, .45f, 0), pc - t * .24f + V(0, .45f, 0), K.Hex("4A4F58"), n, false);
                    K.TriFacing(f, pc - t * .24f + V(0, .45f, 0), pc + t * .24f + V(0, .45f, 0), pc + V(0, .82f, 0), K.Hex("4A4F58"), n);
                }
                f.Cylinder(c + V(0, 4.35f, 0), 1.26f, 1.9f, spire, 8, .62f, q);
                f.Cylinder(c + V(0, 6.05f, 0), .78f, 1.5f, spire2, 8, .34f, q);
                f.Cylinder(c + V(0, 7.1f, 0), .265f, .6f, spire, 8, .2f, q);
                f.Surface = ComicSurface.PaintedMetal;
                f.Box(c + V(0, 7.75f, 0), V(.06f, .7f, .06f), K.R.ink);
                f.Box(c + V(0, 7.85f, 0), V(.34f, .06f, .06f), K.R.ink);
            }

            // ---- the south flank: six bays between stepped buttresses
            const float bay0 = 3.9f, bay = 4.8f;
            for (int k = 0; k <= 6; k++)
            {
                float u = bay0 + bay * k;
                if (k > 0) ChapelButtress(f, S, u, stone, light, shade);
                if (k == 6) break;
                float uc = u + bay * .5f;
                f.Surface = ComicSurface.Concrete;
                E4.Box(f, S, u + .62f, u + bay - .62f, 8.72f, 8.92f, 0f, .14f, light);                  // string course
                E4.ArchFill(f, S, uc, 3.3f, 8.92f, 11.9f, 2.1f, .012f, shade, 4);                          // the relieving arch
                E4.ArchBand(f, S, uc, 3.3f, 8.92f, 11.9f, 2.1f, .02f, .2f, light, 4);
                f.Surface = ComicSurface.PaintedMetal;
                E4.ArchFill(f, S, uc, 1.6f, 9.3f, 11.7f, 1.5f, .026f, K.R.ink, 4);
                f.Surface = ComicSurface.Glass;
                E4.ArchFill(f, S, uc, 1.36f, 9.42f, 11.7f, 1.38f, .036f, glass, 4);
                f.Surface = ComicSurface.Concrete;
                E4.ArchBand(f, S, uc, 1.6f, 9.3f, 11.7f, 1.5f, .032f, .13f, light, 4);
                E4.Box(f, S, uc - .05f, uc + .05f, 9.4f, 12.9f, .03f, .1f, light);
                E4.Box(f, S, uc - .95f, uc + .95f, 9.14f, 9.3f, 0f, .18f, light);
                if (k == 0)
                {
                    // the low four-light window of the first bay
                    E4.FlatWindow(f, S, uc, 2.5f, 3.4f, 5f, 0f, light, glass);
                    foreach (float du in new[] { -.62f, .62f }) E4.Quad(f, S, uc + du - .04f, uc + du + .04f, 3.45f, 4.95f, .04f, light, false);
                }
                if (k == 4)
                {
                    f.Surface = ComicSurface.PaintedMetal;
                    E4.ArchFill(f, S, uc, 1.3f, 1.16f, 2.6f, .9f, .012f, K.R.ink, 3);
                    f.Surface = ComicSurface.Wood;
                    E4.ArchFill(f, S, uc, 1.1f, 1.2f, 2.6f, .78f, .024f, K.Hex("8A5A34"), 3);
                    f.Surface = ComicSurface.Concrete;
                    E4.ArchBand(f, S, uc, 1.3f, 1.16f, 2.6f, .9f, .02f, .16f, light, 3);
                    E4.Box(f, S, uc - 1f, uc + 1f, 0f, 1.16f, 0f, .7f, shade);
                }
            }
            // slit crosses on the tower's south face
            E4Plane TS = S.Move(0f, 0f, 1.75f);
            f.Surface = ComicSurface.PaintedMetal;
            foreach (float v in new[] { 4f, 7.6f })
            {
                E4.Quad(f, TS, 1.75f, 1.85f, v - .42f, v + .42f, .012f, K.Hex("3A3F45"), false);
                E4.Quad(f, TS, 1.6f, 2f, v + .1f, v + .2f, .012f, K.Hex("3A3F45"), false);
            }
            _ = dark;
        }

        /// <summary>One of the chapel's flank buttresses: three stages stepping back with weathered set-offs, a moulded
        /// base, and a gabled cap standing above the parapet.</summary>
        static void ChapelButtress(ComicMesh f, E4Plane S, float u, Color stone, Color light, Color shade)
        {
            f.Surface = ComicSurface.Paving;
            E4.Box(f, S, u - .6f, u + .6f, 0f, 7.6f, 0f, 1.7f, stone);
            E4.Box(f, S, u - .55f, u + .55f, 7.6f, 12.6f, 0f, 1.15f, stone);
            E4.Box(f, S, u - .5f, u + .5f, 12.6f, 16.5f, -.1f, .68f, stone);
            f.Surface = ComicSurface.Concrete;
            E4.Box(f, S, u - .72f, u + .72f, 0f, 1f, 0f, 1.86f, shade);
            E4.Box(f, S, u - .66f, u + .66f, 1f, 1.16f, 0f, 1.78f, light);
            K.Face(f, S.P(u - .6f, 7.6f, 1.7f), S.P(u + .6f, 7.6f, 1.7f), S.P(u + .55f, 8.35f, 1.15f), S.P(u - .55f, 8.35f, 1.15f), light, (S.N + Vector3.up).normalized);
            K.Face(f, S.P(u - .55f, 12.6f, 1.15f), S.P(u + .55f, 12.6f, 1.15f), S.P(u + .5f, 13.25f, .68f), S.P(u - .5f, 13.25f, .68f), light, (S.N + Vector3.up).normalized);
            E4.GableRoof(f, S, u - .56f, u + .56f, 16.5f, 17.45f, .74f, -.16f, light, light);
        }

        // ---------------------------------------------------------------- the school house, sign, flagpole

        /// <summary>
        /// Guildford Grammar's Federation school house: two storeys of red brick with cream render bands, two gabled
        /// wings with paired white sash windows and half-timbered gables, between them a brick arcade below and a
        /// white timber balcony above, a terracotta tiled roof, tall brick chimneys and a copper-capped ventilator.
        /// </summary>
        static void SchoolHouse(ComicMesh f, CqLot l)
        {
            E4Plane p = E4Plane.Lot(l), e = E4Plane.LotNear(l);
            float W = l.W, D = l.Depth, g = 4f, eave = 7.9f, ww = 6.5f, proj = 2.3f;
            Color brick = E4.Brick, cream = E4.Cream, white = E4.White;
            f.Surface = ComicSurface.Concrete;
            E4.Box(f, p, 0, W, -2.6f, .5f, proj + .08f, -D, K.Hex("DCCB9E"));
            f.Surface = ComicSurface.Brick;
            E4.Box(f, p, 0, W, .5f, eave, 0f, -D, brick);
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, p, -.05f, W + .05f, g - .12f, g + .14f, .05f, -D - .05f, cream);
            // the wings
            foreach (bool right in new[] { false, true })
            {
                float u0 = right ? W - ww : 0f, u1 = u0 + ww, uc = (u0 + u1) * .5f;
                f.Surface = ComicSurface.Brick;
                E4.Box(f, p, u0, u1, .5f, eave, proj, 0f, brick);
                f.Surface = ComicSurface.Plaster;
                E4.Box(f, p, u0 - .05f, u1 + .05f, g - .12f, g + .14f, proj + .05f, 0f, cream);
                E4.Box(f, p, u0 - .04f, u0 + .3f, .5f, eave, proj + .04f, proj - .26f, cream);
                E4.Box(f, p, u1 - .3f, u1 + .04f, .5f, eave, proj + .04f, proj - .26f, cream);
                foreach (float du in new[] { -1.05f, 1.05f })
                {
                    E4.Window(f, p, uc + du, 1.15f, 1.5f, 3.3f, proj, white, E4.GlassDark, 2, 3);
                    E4.Window(f, p, uc + du, 1.15f, g + 1.05f, g + 2.85f, proj, white, E4.GlassDark, 2, 3);
                }
                // the gable: roughcast, battens, a louvre, white bargeboards
                float gh = eave + .1f + (ww + .8f) * .46f;
                f.Surface = ComicSurface.Brick;
                E4.GableRoof(f, p, u0 - .4f, u1 + .4f, eave + .1f, gh, proj + .45f, -D * .5f, E4.Tile, cream);
                f.Surface = ComicSurface.Wood;
                E4.Bar(f, p, u0 - .4f, eave + .16f, uc, gh + .06f, proj + .46f, .2f, .12f, white);
                E4.Bar(f, p, u1 + .4f, eave + .16f, uc, gh + .06f, proj + .46f, .2f, .12f, white);
                Color batten = K.Hex("6B4630");
                foreach (float du in new[] { -1.5f, -.75f, 0f, .75f, 1.5f })
                    E4.Box(f, p, uc + du - .06f, uc + du + .06f, eave + .2f, gh - .35f - Mathf.Abs(du) * .92f, proj + .46f, proj + .52f, batten);
                E4.Box(f, p, u0 + .2f, u1 - .2f, eave + .1f, eave + .3f, proj + .46f, proj + .54f, batten);
                E4.FlatWindow(f, p, uc, .7f, eave + .95f, eave + 1.75f, proj + .5f, white, K.Hex("3A3F45"), false);
            }
            // the centre: the brick arcade, the balcony over it
            float c0 = ww, c1 = W - ww, ao = proj - .25f;
            f.Surface = ComicSurface.Brick;
            E4.Box(f, p, c0, c1, .5f, g - .12f, ao, ao - .4f, brick);
            f.Surface = ComicSurface.Wood;
            E4.Box(f, p, c0, c1, g + .14f, g + .3f, ao + .12f, 0f, E4.TimberDark);
            int arches = 5;
            float aw = (c1 - c0) / arches;
            for (int i = 0; i < arches; i++)
            {
                float uc = c0 + (i + .5f) * aw;
                bool mid = i == arches / 2;
                f.Surface = ComicSurface.PaintedMetal;
                E4.ArchFill(f, p, uc, aw - .8f, .5f, 2.35f, (aw - .8f) * .5f, ao + .012f, K.Hex("2A2F38"), 4);
                f.Surface = ComicSurface.Plaster;
                E4.ArchBand(f, p, uc, aw - .8f, .5f, 2.35f, (aw - .8f) * .5f, ao + .02f, .2f, cream, 4);
                if (mid)
                {
                    f.Surface = ComicSurface.Wood;
                    E4.Quad(f, p, uc - .7f, uc + .7f, .52f, 2.6f, ao + .02f, E4.HeritageGreen);
                    E4.Quad(f, p, uc - .03f, uc + .03f, .52f, 2.6f, ao + .03f, K.R.ink, false);
                    f.Surface = ComicSurface.Concrete;
                    E4.Box(f, p, uc - 1.5f, uc + 1.5f, 0f, .26f, ao, ao + 1.3f, K.Hex("DCCB9E"));
                    E4.Box(f, p, uc - 1.3f, uc + 1.3f, .26f, .5f, ao, ao + .8f, K.Hex("DCCB9E"));
                }
                else
                {
                    f.Surface = ComicSurface.Glass;
                    E4.Quad(f, p, uc - .5f, uc + .5f, 1.3f, 2.5f, ao + .02f, E4.GlassDark);
                    E4.Quad(f, p, uc - .56f, uc + .56f, 1.18f, 1.3f, ao + .02f, cream, false);
                }
                // the upper wall behind the balcony
                E4.FlatWindow(f, p, uc, 1.1f, g + 1.05f, g + 2.85f, 0f, white, E4.GlassDark, false);
            }
            f.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i <= arches; i++)
            {
                float u = Mathf.Clamp(c0 + i * aw, c0 + .12f, c1 - .12f);
                E4.Box(f, p, u - .08f, u + .08f, g + .3f, eave - .25f, ao - .02f, ao + .12f, white);
                if (i == arches) break;
                float next = Mathf.Clamp(c0 + (i + 1) * aw, c0 + .12f, c1 - .12f);
                E4.Balustrade(f, p, u + .08f, next - .08f, g + .3f, ao + .12f, white, white);
                E4.Tri(f, p, new Vector2(u + .08f, eave - .3f), new Vector2(u + .65f, eave - .3f), new Vector2(u + .08f, eave - .85f), ao + .05f, white);
                E4.Tri(f, p, new Vector2(next - .08f, eave - .3f), new Vector2(next - .65f, eave - .3f), new Vector2(next - .08f, eave - .85f), ao + .05f, white);
            }
            E4.Box(f, p, c0, c1, eave - .3f, eave - .12f, ao - .02f, ao + .14f, white);
            // the end facing the rider
            foreach (float u in new[] { D * .25f, D * .6f })
            {
                E4.FlatWindow(f, e, u, 1.1f, 1.5f, 3.3f, 0f, white, E4.GlassDark);
                E4.FlatWindow(f, e, u, 1.1f, g + 1.05f, g + 2.85f, 0f, white, E4.GlassDark);
            }
            E4.Box(f, e, -.05f, D + .05f, g - .12f, g + .14f, 0f, .05f, cream);
            // roof, chimneys, the ventilator
            f.Surface = ComicSurface.PaintedMetal;
            E4.Box(f, p, -.4f, W + .4f, eave - .05f, eave + .1f, proj + .4f, -D - .4f, K.R.ink);
            f.Surface = ComicSurface.Brick;
            E4.RidgeRoof(f, p, -.45f, W + .45f, eave + .1f, eave + 4.3f, proj + .45f, -D - .45f, E4.Tile, (D + proj) * .44f);
            E4.Chimney(f, p.P(W * .3f, eave + 1.6f, -D * .45f), 3.4f, brick, .85f);
            E4.Chimney(f, p.P(W * .7f, eave + 1.6f, -D * .45f), 3.4f, brick, .85f);
            E4.Chimney(f, p.P(W * .08f, eave + .8f, -D * .7f), 3f, brick, .8f);
            Vector3 v = p.P(W * .5f, eave + 4.2f, (proj - D) * .5f);
            f.Surface = ComicSurface.Wood;
            f.Box(v + V(0, .5f, 0), V(1.1f, 1f, 1.1f), white);
            f.Box(v + V(0, .5f, 0), V(1.14f, .5f, 1.14f), K.Hex("4A4F58"));
            f.Surface = ComicSurface.Metal;
            f.Cylinder(v + V(0, 1.6f, 0), 1f, 1.2f, K.Hex("6FA08A"), 4, .1f, Quaternion.Euler(0, 45, 0));
            f.Cylinder(v + V(0, 2.5f, 0), .04f, .7f, K.R.ink, 4);
        }

        /// <summary>The school's entrance wall on a wall plane p (len metres long): red brick with a cream cap and piers,
        /// a cream panel reading GUILDFORD GRAMMAR (the place's name: no crest, no logo).</summary>
        static void SchoolSign(ComicMesh f, E4Plane p, float len)
        {
            f.Surface = ComicSurface.Brick;
            E4.Box(f, p, 0f, len, -1.5f, 1.55f, 0f, -.36f, E4.Brick);
            foreach (float u in new[] { .3f, len - .3f })
            {
                E4.Box(f, p, u - .36f, u + .36f, -1.5f, 2f, .08f, -.44f, E4.Brick);
                f.Surface = ComicSurface.Plaster;
                E4.Box(f, p, u - .44f, u + .44f, 2f, 2.16f, .16f, -.52f, E4.Cream);
                f.Cylinder(p.P(u, 2.4f, -.18f), .2f, .42f, E4.Cream, 6, .45f);
                f.Surface = ComicSurface.Brick;
            }
            f.Surface = ComicSurface.Plaster;
            E4.Box(f, p, .66f, len - .66f, 1.55f, 1.7f, .06f, -.42f, E4.Cream);
            E4.Box(f, p, 1f, len - 1f, .35f, 1.3f, 0f, .05f, E4.Cream);
            float px = Mathf.Min(.1f, (len - 2.6f) / (E4.TextWidth("GUILDFORD GRAMMAR", 1f) + 1f));
            E4.Text(f, p, "GUILDFORD GRAMMAR", len * .5f, .84f, .058f, px, K.Hex("23282E"));
            // the same wall seen by the rider coming along the road: its end pier carries a small plain shield
            E4Plane near = new E4Plane(p.P(0f, 0f, -.44f), p.N, -p.R);
            E4.Box(f, near, .1f, .42f, .9f, 1.6f, 0f, .05f, E4.Cream);
        }

        static void Flagpole(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            m.Cylinder(p + V(0, .2f, 0), .3f, .4f, E4.Concrete, 8, .8f);
            m.Cylinder(p + V(0, 5.5f, 0), .07f, 10.6f, E4.White, 6, .6f);
            m.Cylinder(p + V(0, 10.9f, 0), .11f, .16f, K.Hex("C9A227"), 6);
            m.Surface = ComicSurface.Fabric;
            m.Box(p + V(0, 9.8f, .95f), V(.04f, 1.1f, 1.8f), K.Hex("2A3E6E"));
            m.Box(p + V(0, 9.8f, .95f), V(.05f, .16f, 1.8f), K.Hex("C9A227"));
        }

        // ---------------------------------------------------------------- stables, horses

        /// <summary>The white stables: painted brick loose boxes with green split doors (a horse looking out of two of
        /// them), a deep iron roof on posts over the walkway, the loft gable with its door and hoist beam, a louvred
        /// cupola with a weathervane.</summary>
        static void Stables(ComicMesh f, CqLot l)
        {
            E4Plane p = E4Plane.Lot(l);
            float W = l.W, D = l.Depth, wall = 3f, vd = 1.9f;
            Color green = E4.HeritageGreen;
            f.Surface = ComicSurface.Concrete;
            E4.Box(f, p, 0, W, -2.6f, .12f, vd + .2f, -D, K.Hex("CFC5AE"));
            f.Surface = ComicSurface.Brick;
            E4.Box(f, p, 0, W, .12f, wall, 0f, -D, E4.White);
            f.Surface = ComicSurface.PaintedMetal;
            E4.Box(f, p, -.02f, W + .02f, .12f, .75f, .03f, -D - .02f, green);
            int doors = 6;
            float bw = W / doors;
            for (int i = 0; i < doors; i++)
            {
                float uc = (i + .5f) * bw;
                bool gable = i == 2 || i == 3;
                f.Surface = ComicSurface.PaintedMetal;
                E4.Quad(f, p, uc - .68f, uc + .68f, .12f, 2.45f, .012f, K.R.ink);
                f.Surface = ComicSurface.Wood;
                E4.Quad(f, p, uc - .6f, uc + .6f, .16f, 1.28f, .034f, green);
                E4.Quad(f, p, uc - .56f, uc + .56f, 1.14f, 1.24f, .044f, E4.White, false);
                E4.Quad(f, p, uc - .56f, uc + .56f, .2f, .3f, .044f, E4.White, false);
                E4.Bar(f, p, uc - .54f, .3f, uc + .54f, 1.14f, .04f, .09f, .012f, E4.White);
                // the open top half: the leaf folded back against the wall
                E4.Box(f, p, uc + .7f, uc + 1.26f, 1.3f, 2.4f, 0f, .06f, green);
                if (!gable && i % 2 == 1)
                {
                    Vector3 h = p.P(uc - .05f, 1.78f, .3f);
                    Quaternion q = Quaternion.LookRotation(p.N) * Quaternion.Euler(18f, 0, 0);
                    f.Surface = ComicSurface.Fabric;
                    f.Box(h, V(.26f, .3f, .7f), i == 1 ? K.Hex("8A5A3C") : K.Hex("3E3028"), q);
                    f.Box(h + q * V(0, .02f, .3f), V(.2f, .2f, .22f), i == 1 ? K.Hex("6B4630") : K.Hex("2A2220"), q);
                    f.Box(h + q * V(0, .16f, .1f), V(.07f, .02f, .34f), E4.White, q);
                    foreach (float x in new[] { -.09f, .09f }) f.Box(h + q * V(x, .22f, -.25f), V(.06f, .14f, .06f), K.Hex("6B4630"), q);
                }
            }
            // the roof over the boxes and the walkway, on white posts
            f.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i <= doors; i++)
            {
                float u = Mathf.Clamp(i * bw, .1f, W - .1f);
                E4.Box(f, p, u - .07f, u + .07f, .12f, wall - .3f, vd - .12f, vd + .02f, E4.White);
            }
            E4.Box(f, p, -.2f, W + .2f, wall - .34f, wall - .16f, vd - .12f, vd + .04f, E4.White);
            f.Surface = ComicSurface.Metal;
            E4.RidgeRoof(f, p, -.5f, W + .5f, wall - .12f, wall + 2.3f, vd + .4f, -D - .4f, E4.Iron);
            // the loft gable in the middle
            float g0 = W * .5f - 2.6f, g1 = W * .5f + 2.6f, gh = wall + 2.9f;
            f.Surface = ComicSurface.Brick;
            E4.Box(f, p, g0, g1, wall - .2f, wall + .9f, .3f, -D * .4f, E4.White);
            f.Surface = ComicSurface.Metal;
            E4.GableRoof(f, p, g0 - .35f, g1 + .35f, wall + .9f, gh, .6f, -D * .5f, E4.Iron, E4.White);
            f.Surface = ComicSurface.Wood;
            E4.Bar(f, p, g0 - .35f, wall + .95f, W * .5f, gh + .05f, .61f, .16f, .1f, green);
            E4.Bar(f, p, g1 + .35f, wall + .95f, W * .5f, gh + .05f, .61f, .16f, .1f, green);
            E4.Quad(f, p, W * .5f - .6f, W * .5f + .6f, wall - .05f, wall + 1.25f, .312f, green);
            E4.Quad(f, p, W * .5f - .03f, W * .5f + .03f, wall - .05f, wall + 1.25f, .322f, K.R.ink, false);
            f.Box(p.P(W * .5f, gh - .55f, 1.1f), V(.14f, .14f, 1.6f), K.Hex("5E4634"), p.Rot);
            f.Surface = ComicSurface.PaintedMetal;
            f.Box(p.P(W * .5f, gh - .95f, 1.7f), V(.05f, .7f, .05f), K.R.ink);
            // the cupola and the weathervane
            Vector3 c = p.P(W * .5f, wall + 2.25f, (vd - D) * .5f);
            f.Surface = ComicSurface.Wood;
            f.Box(c + V(0, .55f, 0), V(1.2f, 1.1f, 1.2f), E4.White);
            f.Box(c + V(0, .55f, 0), V(1.24f, .6f, 1.24f), K.Hex("4A4F58"));
            f.Surface = ComicSurface.Metal;
            f.Cylinder(c + V(0, 1.55f, 0), 1.1f, .9f, green, 4, .08f, Quaternion.Euler(0, 45, 0));
            f.Surface = ComicSurface.PaintedMetal;
            f.Cylinder(c + V(0, 2.5f, 0), .035f, 1.1f, K.R.ink, 4);
            f.Box(c + V(0, 2.75f, 0), V(.05f, .05f, .9f), K.R.ink);
            f.Box(c + V(0, 2.75f, .5f), V(.05f, .3f, .24f), K.R.ink);
            f.Box(c + V(0, 2.95f, -.1f), V(.05f, .26f, .4f), K.R.ink);
        }

        /// <summary>A horse (scenery in the paddock). Faces -Z at yaw 0; grazing lowers its head to the grass.</summary>
        static void Horse(ComicMesh m, Vector3 p, float yaw, Color coat, bool grazing)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            Color dark = Color.Lerp(coat, K.R.ink, .55f), mane = coat.grayscale > .6f ? K.Hex("B9B2A2") : K.Hex("2A2220");
            m.Surface = ComicSurface.Fabric;
            E4.Blob(m, p + q * V(0, 1.22f, 0), V(.72f, .8f, 1.9f), coat, 6, 3, yaw);
            m.Box(p + q * V(0, 1.3f, -.62f), V(.58f, .74f, .5f), coat, q);
            m.Box(p + q * V(0, 1.3f, .66f), V(.62f, .76f, .5f), coat, q);
            Vector3 n0 = p + q * V(0, 1.42f, -.8f), n1 = p + q * (grazing ? V(0, .62f, -1.4f) : V(0, 2.02f, -1.22f));
            m.Beam(n0, n1, .34f, coat);
            Quaternion hq = q * Quaternion.Euler(grazing ? 62f : 28f, 0, 0);
            Vector3 head = n1 + hq * V(0, 0, -.28f);
            m.Box(head, V(.25f, .28f, .62f), coat, hq);
            m.Box(head + hq * V(0, -.02f, -.3f), V(.2f, .2f, .16f), dark, hq);
            m.Box(head + hq * V(0, .145f, -.05f), V(.07f, .02f, .34f), E4.White, hq);
            foreach (float x in new[] { -.09f, .09f }) m.Box(head + hq * V(x, .2f, .24f), V(.06f, .14f, .06f), dark, hq);
            m.Beam(n0 + q * V(0, .2f, .08f), n1 + q * V(0, .16f, .14f), .12f, mane);
            foreach (float x in new[] { -.2f, .2f })
                foreach (float z in new[] { -.62f, .64f })
                {
                    m.Box(p + q * V(x, .5f, z), V(.15f, 1f, .17f), coat, q);
                    m.Box(p + q * V(x, .07f, z), V(.17f, .14f, .2f), dark, q);
                }
            m.Beam(p + q * V(0, 1.5f, .92f), p + q * V(0, .7f, 1.1f), .14f, mane);
        }

        // ---------------------------------------------------------------- tennis courts, ovals

        /// <summary>A tennis court between x0 and x1 (x1 further from the path), z0..z1: green hard court with white
        /// lines and a net, a tall chain fence on posts round it, a light tower at each end.</summary>
        static void TennisCourt(CqChunkDraft d, float x0, float x1, float z0, float z1)
        {
            ComicMesh r = d.Road;
            float y = K.Kerb + .04f, xc = (x0 + x1) * .5f, zc = (z0 + z1) * .5f;
            E4.Patch(r, x1 - 2f, x0 + 2f, z0 - 2.5f, z1 + 2.5f, y, K.Hex("3F8A7A"), ComicSurface.Concrete);
            E4.Patch(r, x1, x0, z0, z1, y + .01f, K.Hex("2F6FA0"), ComicSurface.Concrete);
            r.Surface = ComicSurface.Plain;
            void Line(float xa, float xb, float za, float zb) => K.Face(r, V(xa, y + .02f, za), V(xa, y + .02f, zb), V(xb, y + .02f, zb), V(xb, y + .02f, za), E4.White, Vector3.up, false);
            Line(x0, x0 - .07f, z0, z1); Line(x1 + .07f, x1, z0, z1); Line(x0, x1, z0, z0 + .07f); Line(x0, x1, z1 - .07f, z1);
            Line(x0 - 1.37f, x0 - 1.44f, z0, z1); Line(x1 + 1.44f, x1 + 1.37f, z0, z1);
            Line(x0 - 1.37f, x1 + 1.37f, zc - 6.4f, zc - 6.33f); Line(x0 - 1.37f, x1 + 1.37f, zc + 6.33f, zc + 6.4f);
            Line(xc + .035f, xc - .035f, zc - 6.4f, zc + 6.4f);
            ComicMesh m = d.SlopedFrontage;
            m.Surface = ComicSurface.Metal;
            foreach (float x in new[] { x0 + .5f, x1 - .5f }) m.Box(V(x, y + .55f, zc), V(.08f, 1.1f, .08f), E4.SteelDark);
            r.Surface = ComicSurface.Plain;
            K.Face(r, V(x0 + .5f, y + .15f, zc), V(x1 - .5f, y + .15f, zc), V(x1 - .5f, y + .95f, zc), V(x0 + .5f, y + .95f, zc), K.Hex("3A3F45"), Vector3.back, false);
            K.Face(r, V(x0 + .5f, y + .15f, zc + .01f), V(x1 - .5f, y + .15f, zc + .01f), V(x1 - .5f, y + .95f, zc + .01f), V(x0 + .5f, y + .95f, zc + .01f), K.Hex("3A3F45"), Vector3.forward, false);
            m.Box(V(xc, y + .98f, zc), V(Mathf.Abs(x1 - x0) + 1f, .07f, .05f), E4.White);
            // the fence: posts and rails (no ink on the rails), lights
            float fx0 = x0 + 2f, fx1 = x1 - 2f, fz0 = z0 - 2.5f, fz1 = z1 + 2.5f;
            for (float z = fz0; z <= fz1 + .01f; z += 3.5f) { m.Box(V(fx0, y + 1.6f, z), V(.08f, 3.2f, .08f), E4.SteelDark); m.Box(V(fx1, y + 1.6f, z), V(.08f, 3.2f, .08f), E4.SteelDark); }
            r.Surface = ComicSurface.Metal;
            foreach (float x in new[] { fx0, fx1 })
                foreach (float h in new[] { .1f, 1.6f, 3.2f })
                    for (float z = fz0; z < fz1 - .01f; z += 7f) r.Box(V(x, y + h, (z + Mathf.Min(fz1, z + 7f)) * .5f), V(.04f, .04f, Mathf.Min(fz1, z + 7f) - z), E4.Steel);
            foreach (float z in new[] { fz0, fz1 })
                foreach (float h in new[] { .1f, 1.6f, 3.2f }) r.Box(V((fx0 + fx1) * .5f, y + h, z), V(Mathf.Abs(fx1 - fx0), .04f, .04f), E4.Steel);
            foreach (float z in new[] { fz0 + .4f, fz1 - .4f })
            {
                m.Surface = ComicSurface.Metal;
                m.Cylinder(V(fx1 + .4f, y + 4.5f, z), .1f, 9f, E4.Steel, 6, .7f);
                m.Box(V(fx1 + .4f, y + 9f, z), V(1.5f, .1f, .12f), E4.Steel);
                foreach (float x in new[] { -.55f, .55f }) m.Box(V(fx1 + .4f + x, y + 8.9f, z), V(.5f, .22f, .36f), E4.SteelDark);
            }
        }

        /// <summary>The cricket pavilion: red brick with a white verandah and balustrade, a tiled hip roof, and a small
        /// gable carrying the clock.</summary>
        static void Pavilion(ComicMesh f, CqLot l)
        {
            E4Plane p = E4Plane.Lot(l);
            float W = l.W, D = l.Depth, wall = 3.3f, vd = 2.2f;
            f.Surface = ComicSurface.Concrete;
            E4.Box(f, p, 0, W, -2.6f, .5f, vd + .1f, -D, K.Hex("DCCB9E"));
            f.Surface = ComicSurface.Brick;
            E4.Box(f, p, 0, W, .5f, wall + .5f, 0f, -D, E4.Brick);
            E4.Door(f, p, W * .5f, 1.5f, .55f, 3f, 0f, E4.White, E4.HeritageGreen, E4.GlassDark);
            foreach (float u in new[] { W * .2f, W * .8f }) E4.Window(f, p, u, 1.3f, 1.4f, 3f, 0f, E4.White, E4.GlassDark, 3, 2);
            f.Surface = ComicSurface.Wood;
            E4.Box(f, p, -.1f, W + .1f, .5f, .64f, 0f, vd + .1f, E4.Timber);
            E4.Box(f, p, W * .5f - 1.4f, W * .5f + 1.4f, .1f, .5f, vd + .1f, vd + .9f, K.Hex("CFC5AE"));
            f.Surface = ComicSurface.PaintedMetal;
            for (int i = 0; i < 5; i++)
            {
                float u = Mathf.Lerp(.1f, W - .1f, i / 4f), next = Mathf.Lerp(.1f, W - .1f, (i + 1) / 4f);
                E4.Box(f, p, u - .07f, u + .07f, .64f, wall + .2f, vd - .12f, vd + .02f, E4.White);
                if (i == 4) break;
                if (i != 1 && i != 2) E4.Balustrade(f, p, u + .07f, next - .07f, .64f, vd, E4.White, E4.White);
                E4.Tri(f, p, new Vector2(u + .07f, wall + .15f), new Vector2(u + .6f, wall + .15f), new Vector2(u + .07f, wall - .35f), vd - .05f, E4.White);
                E4.Tri(f, p, new Vector2(next - .07f, wall + .15f), new Vector2(next - .6f, wall + .15f), new Vector2(next - .07f, wall - .35f), vd - .05f, E4.White);
            }
            E4.Box(f, p, -.15f, W + .15f, wall + .15f, wall + .32f, vd - .12f, vd + .04f, E4.White);
            f.Surface = ComicSurface.Brick;
            E4.RidgeRoof(f, p, -.5f, W + .5f, wall + .3f, wall + 3.4f, vd + .45f, -D - .5f, E4.Tile, (D + vd) * .44f);
            float gc = W * .5f;
            E4.GableRoof(f, p, gc - 1.7f, gc + 1.7f, wall + .9f, wall + 2.9f, vd + .2f, -D * .3f, E4.Tile, E4.Cream);
            f.Surface = ComicSurface.Wood;
            E4.Bar(f, p, gc - 1.7f, wall + .95f, gc, wall + 2.95f, vd + .21f, .16f, .1f, E4.White);
            E4.Bar(f, p, gc + 1.7f, wall + .95f, gc, wall + 2.95f, vd + .21f, .16f, .1f, E4.White);
            K.Clock(f, p.P(gc, wall + 1.75f, vd + .22f), Quaternion.LookRotation(p.N), .5f, E4.White, K.R.ink);
            E4.Chimney(f, p.P(W * .85f, wall + 1.2f, -D * .5f), 2.6f, E4.Brick, .7f);
        }

        /// <summary>The scoreboard beside the oval: a dark green board on posts with white numbers (faces the path).</summary>
        static void Scoreboard(ComicMesh m, Vector3 p)
        {
            Quaternion q = Quaternion.Euler(0, -90f, 0);
            m.Surface = ComicSurface.Wood;
            foreach (float z in new[] { -1.6f, 1.6f }) m.Box(p + V(0, 1.3f, z), V(.16f, 2.6f, .16f), E4.TimberDark);
            m.Box(p + V(0, 3.2f, 0), V(.2f, 2.5f, 4.4f), E4.HeritageGreen);
            m.Box(p + V(0, 4.52f, 0), V(.5f, .1f, 4.7f), E4.White);
            E4.Text(m, "HOME 128", p + V(.11f, 3.75f, 0), q, .105f, E4.White);
            E4.Text(m, "AWAY 94", p + V(.11f, 2.75f, 0), q, .105f, E4.White);
            m.Surface = ComicSurface.Plain;
            m.Box(p + V(.11f, 3.25f, 0), V(.01f, .05f, 3.9f), E4.White);
        }

        /// <summary>Australian football goal posts: two tall posts and two shorter ones in a row across x, white.</summary>
        static void GoalPosts(ComicMesh m, Vector3 p)
        {
            m.Surface = ComicSurface.PaintedMetal;
            float[] xs = { -9.6f, -3.2f, 3.2f, 9.6f }, hs = { 6.5f, 11f, 11f, 6.5f };
            for (int i = 0; i < 4; i++)
            {
                m.Cylinder(p + V(xs[i], hs[i] * .5f, 0), .11f, hs[i], E4.White, 6, .7f);
                m.Cylinder(p + V(xs[i], .75f, 0), .26f, 1.5f, i == 1 || i == 2 ? K.Hex("2A3E6E") : K.Hex("C9A227"), 6);
            }
        }

        // ---------------------------------------------------------------- obstacles and animals

        static void HayBaleArt(ComicMesh m, Vector3 p, float yaw)
        {
            Quaternion q = Quaternion.Euler(0, yaw, 0);
            m.Surface = ComicSurface.Fabric;
            m.Box(p + V(0, .23f, 0), V(.95f, .46f, .5f), K.Hex("D9B84F"), q);
            m.Surface = ComicSurface.Plain;
            foreach (float x in new[] { -.25f, .25f }) m.Box(p + q * V(x, .23f, 0), V(.04f, .48f, .52f), K.Hex("8A5A34"), q);
        }

        // Two hay bales side by side with a third across them.
        static ComicMesh HayBaleMesh() => Make(m =>
        {
            HayBaleArt(m, V(0, 0, -.14f), 0f);
            HayBaleArt(m, V(0, 0, .14f) + V(0, .001f, .24f), 0f);
            HayBaleArt(m, V(0, .46f, .05f), 8f);
        });

        // A cricket kit bag lying across the path: a long navy bag with handles, a bat and pads strapped on top.
        static ComicMesh CricketBagMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            m.Box(V(0, .2f, 0), V(1.15f, .38f, .44f), K.Hex("2A3E6E"));
            m.Box(V(0, .41f, 0), V(1.05f, .05f, .34f), K.Hex("1E2D52"));
            m.Box(V(0, .22f, -.225f), V(.5f, .1f, .02f), K.Hex("C9A227"));
            foreach (float x in new[] { -.3f, .3f }) m.Box(V(x, .46f, 0), V(.05f, .08f, .3f), K.R.ink);
            m.Surface = ComicSurface.Wood;
            m.Box(V(.1f, .47f, .05f), V(.7f, .06f, .12f), K.Hex("E6CF8A"), Quaternion.Euler(0, 8f, 0));
            m.Box(V(-.42f, .47f, 0f), V(.34f, .05f, .05f), K.Hex("3A3F45"), Quaternion.Euler(0, 8f, 0));
            m.Surface = ComicSurface.Plastic;
            m.Cylinder(V(.5f, .08f, -.3f), .07f, .14f, K.Hex("B8323A"), 6);
            return m;
        }

        // A rugby / football tackle bag standing on its end: tall padded cylinder, navy with a gold band.
        static ComicMesh TackleBagMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            m.Cylinder(V(0, .05f, 0), .3f, .1f, K.R.ink, 8);
            m.Cylinder(V(0, .78f, 0), .26f, 1.4f, K.Hex("2A3E6E"), 8);
            m.Cylinder(V(0, .9f, 0), .268f, .22f, K.Hex("C9A227"), 8);
            m.Cylinder(V(0, 1.5f, 0), .26f, .06f, K.Hex("1E2D52"), 8, .8f);
            foreach (float y in new[] { .5f, 1.2f }) m.Box(V(0, y, -.27f), V(.2f, .06f, .05f), K.R.ink);
            return m;
        }

        // A row of training cones (marker domes) across a lane.
        static ComicMesh TrainingConesMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Plastic };
            Color[] cols = { K.Hex("F28C28"), K.Hex("F2C230"), K.Hex("F28C28") };
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = V(-.55f + i * .55f, 0, (i % 2) * .12f - .06f);
                m.Cylinder(p + V(0, .02f, 0), .2f, .04f, K.R.ink, 8);
                m.Cylinder(p + V(0, .17f, 0), .16f, .3f, cols[i], 8, .25f);
                m.Cylinder(p + V(0, .16f, 0), .11f, .06f, E4.White, 8, .8f);
            }
            return m;
        }

        // A laughing kookaburra: cream head and breast, a dark eye stripe, a heavy two-tone bill, brown wings with a
        // pale blue patch, a barred tail. Facing -Z.
        static ComicMesh KookaburraMesh()
        {
            var m = new ComicMesh { Surface = ComicSurface.Fabric };
            Color cream = K.Hex("EFE6D2"), brown = K.Hex("6B4630"), blue = K.Hex("6FA8C9");
            E4.Blob(m, V(0, .24f, .02f), V(.26f, .28f, .36f), cream, 6, 3, .3f);
            foreach (float x in new[] { -.125f, .125f })
            {
                m.Box(V(x, .27f, .08f), V(.05f, .2f, .32f), brown);
                m.Box(V(x * 1.06f, .27f, .04f), V(.05f, .08f, .12f), blue);
            }
            m.Box(V(0, .22f, .28f), V(.14f, .05f, .22f), K.Hex("9A5A34"), Quaternion.Euler(-12f, 0, 0));
            m.Box(V(0, .225f, .3f), V(.145f, .052f, .05f), brown, Quaternion.Euler(-12f, 0, 0));
            m.Box(V(0, .46f, -.08f), V(.22f, .19f, .22f), cream);
            m.Box(V(0, .56f, -.06f), V(.16f, .04f, .18f), brown);
            foreach (float x in new[] { -.112f, .112f }) m.Box(V(x, .47f, -.08f), V(.02f, .06f, .2f), brown);
            m.Surface = ComicSurface.Plastic;
            m.Box(V(0, .46f, -.3f), V(.08f, .05f, .24f), K.Hex("3A3F45"));
            m.Box(V(0, .415f, -.28f), V(.07f, .04f, .2f), K.Hex("E6CF8A"));
            foreach (float x in new[] { -.05f, .05f }) m.Box(V(x, .05f, 0f), V(.04f, .1f, .08f), K.Hex("6E6A66"));
            return m;
        }

        // ---------------------------------------------------------------- far scenery

        /// <summary>
        /// East Guildford's horizon: to the left behind the school grounds the Swan River in its belt of flooded gums,
        /// the Swan Valley flats and vines beyond, and the Darling Scarp, close now and high; to the right over the
        /// railway the roofs and trees of Guildford, the Helena River's tree line and the scarp again. Sheared with
        /// the road's grade. No colliders.
        /// </summary>
        static ComicMesh EgFar(float grade)
        {
            var m = new ComicMesh();
            var r = new System.Random(320);
            Color haze = K.Hex("CADCD6");
            const float z0 = -220f, z1 = 820f;
            E4.FarStrip(m, -K.Edge - .5f, -100f, 330f, z1, K.Kerb - .05f, E4.Lawn);
            E4.FarStrip(m, -100f, -150f, z0, z1, -1.2f, E4.Haze(E4.River, 120f, haze));
            E4.FarStrip(m, -150f, -300f, z0, z1, -.3f, E4.Haze(K.Hex("86A35C"), 220f, haze));
            E4.FarStrip(m, -300f, -520f, z0, z1, -.35f, E4.Haze(K.Hex("A9B870"), 400f, haze));
            E4.FarStrip(m, -520f, -1400f, z0, z1, -.4f, E4.Haze(K.Hex("9AAE7A"), 700f, haze));
            E4.FarStrip(m, 70f, 260f, z0, z1, -.25f, E4.Haze(K.Hex("93A866"), 160f, haze));
            E4.FarStrip(m, 260f, 1400f, z0, z1, -.3f, E4.Haze(K.Hex("9AAE7A"), 600f, haze));
            m.Surface = ComicSurface.Plain;
            for (int i = 0; i < 60; i++)
            {
                float x = -F(r, 103f, 147f), z = F(r, z0, z1), len = F(r, 2f, 6f);
                K.Face(m, V(x, -1.16f, z), V(x, -1.16f, z + len), V(x + .5f, -1.16f, z + len), V(x + .5f, -1.16f, z), E4.Haze(E4.RiverLight, 120f, haze), Vector3.up, false);
            }
            // the river's gums on both banks
            E4.TreeLine(m, r, -96f, 4f, z0, z1, 10f, 15f, K.Hex("5E7E46"), K.Hex("7C9A58"), haze);
            E4.TreeLine(m, r, -158f, 6f, z0, z1, 10f, 15f, K.Hex("58764A"), K.Hex("6F8F52"), haze);
            E4.TreeLine(m, r, -250f, 30f, z0, z1, 22f, 14f, K.Hex("5F7D52"), K.Hex("7A9460"), haze);
            E4.TreeLine(m, r, -400f, 50f, z0, z1, 30f, 15f, K.Hex("5F7D52"), K.Hex("7A9460"), haze);
            for (int b = 0; b < 8; b++)
            {
                float bx = -300f - (b % 3) * 70f, bz = z0 + 30f + b * 125f;
                m.Surface = ComicSurface.Plain;
                K.Face(m, V(bx + 4f, -.1f, bz - 4f), V(bx + 4f, -.1f, bz + 74f), V(bx - 58f, -.1f, bz + 74f), V(bx - 58f, -.1f, bz - 4f), E4.Haze(E4.Soil, Mathf.Abs(bx), haze), Vector3.up, false);
                for (int row = 0; row < 9; row++)
                {
                    float x = bx - row * 6f;
                    m.Surface = ComicSurface.Foliage;
                    m.Box(V(x, .9f, bz + 35f), V(2.6f, 2f, 70f), E4.Haze(K.Hex("5F8A3E"), Mathf.Abs(x), haze));
                }
            }
            // right: roofs and trees
            Color[] walls = { E4.Cream, E4.White, E4.Brick, K.Hex("D9CBB0") };
            Color[] roofs = { E4.Tile, E4.Iron, E4.IronDark, K.Hex("8A6A58") };
            for (float z = z0; z < z1; z += F(r, 12f, 18f))
                for (int row = 0; row < 3; row++)
                {
                    float x = 76f + row * 27f + F(r, 0f, 12f), dist = Mathf.Sqrt(x * x + z * z * .1f);
                    float w = F(r, 9f, 13f), h = F(r, 3.2f, 5.5f);
                    m.Surface = ComicSurface.Plaster;
                    m.Box(V(x, h * .5f - 1f, z), V(w, h + 2f, w * .8f), E4.Haze(walls[r.Next(walls.Length)], dist, haze));
                    m.Surface = ComicSurface.Metal;
                    m.Cylinder(V(x, h + .8f, z), w * .74f, 2.2f, E4.Haze(roofs[r.Next(roofs.Length)], dist, haze), 4, .15f, Quaternion.Euler(0, 45, 0));
                }
            E4.TreeLine(m, r, 68f, 6f, z0, z1, 13f, 13f, K.Hex("5E7E46"), K.Hex("7C9A58"), haze);
            E4.TreeLine(m, r, 175f, 30f, z0, z1, 19f, 15f, K.Hex("58764A"), K.Hex("6F8F52"), haze);
            E4.TreeLine(m, r, 320f, 50f, z0, z1, 28f, 15f, K.Hex("5F7D52"), K.Hex("7A9460"), haze);
            // the Darling Scarp, closer and higher again: two overlapping ridges on the left, one on the right
            E4.Ridge(m, -1, 470f, 240f, z0, z1, -.4f, z => 68f + 13f * Mathf.Sin(z * .007f + 2f) + 7f * Mathf.Sin(z * .021f), K.Hex("8CA070"), K.Hex("66887A"), K.Hex("789690"), haze, .3f);
            E4.Ridge(m, -1, 640f, 260f, z0, z1, -.4f, z => 92f + 12f * Mathf.Sin(z * .005f + .4f), K.Hex("8CA070"), K.Hex("7493A0"), K.Hex("86A2AE"), haze, .5f);
            E4.Ridge(m, 1, 520f, 260f, z0, z1, -.4f, z => 62f + 12f * Mathf.Sin(z * .006f + 1.1f) + 6f * Mathf.Sin(z * .019f), K.Hex("8CA070"), K.Hex("66887A"), K.Hex("789690"), haze, .38f);
            m.Shear(grade);
            return m;
        }
    }
}
