#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CityScooter.Tests
{
    /// <summary>MCIVER, CLAISEBROOK and EAST PERTH (East1Builder, east line art agent 1): every field the builder owns is
    /// filled, the street cross-section at both ends of every chunk, lengths and slopes, budgets, the two shared comic
    /// materials, spawn slots, ramps and road gaps (a clearable ramp before every road-wide gap), animals, far scenery,
    /// and the railway is scenery (no colliders outside the boundary and keep-out groups).</summary>
    public sealed class DistrictEast1Tests
    {
        const string Folder = "Assets/_Project/Data/Districts/";
        const string Palette = "Assets/_Project/Art/ComicCity/ComicPalette.mat";
        const string Ink = "Assets/_Project/Art/ComicCity/ComicInk.mat";
        const string TuningPath = "Assets/_Project/Data/Tuning_ComicDownhill.asset";
        // Rider numbers (Tuning_ComicDownhill): start speed 9 m/s.
        const float StartSpeed = 9f;

        static readonly (string asset, string id, string name, int order, string prefix)[] Specs =
        {
            ("District_Mciver.asset", "mciver", "MCIVER", 210, "CHN_Mciver_"),
            ("District_Claisebrook.asset", "claisebrook", "CLAISEBROOK", 220, "CHN_Claisebrook_"),
            ("District_Eastperth.asset", "eastperth", "EAST PERTH", 230, "CHN_Eastperth_"),
        };

        static IEnumerable<string> Ids => Specs.Select(s => s.id);

        static (string asset, string id, string name, int order, string prefix) Spec(string id) => Specs.First(s => s.id == id);

        static District Load(string id)
        {
            var d = AssetDatabase.LoadAssetAtPath<District>(Folder + Spec(id).asset);
            Assert.NotNull(d, Spec(id).asset + " is missing (run East1Builder.BuildBatch)");
            return d;
        }

        static IEnumerable<RoadChunk> AllChunks(District d) => new[] { d.entry }.Concat(d.chunks).Concat(new[] { d.exit });

        [TestCaseSource(nameof(Ids))]
        public void DistrictAsset_EveryOwnedFieldIsFilled(string id)
        {
            District d = Load(id);
            var spec = Spec(id);
            Assert.AreEqual(spec.id, d.id);
            Assert.AreEqual(spec.name, d.displayName);
            Assert.AreEqual(spec.order, d.order);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(d.id, "^[a-z]+$"), "id: lower case letters only");
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(d.displayName, "^[A-Z ]+$"), "display name: A-Z and spaces");
            Assert.That(d.length.x, Is.InRange(280f, 480f), "about 300-450 m");
            Assert.That(d.length.y, Is.InRange(d.length.x, 480f));
            Assert.IsTrue(d.HasChunks);
            Assert.That(d.chunks.Length, Is.InRange(4, 6), "4-6 variants");
            Assert.AreEqual(d.chunks.Length, d.chunks.Distinct().Count(), "variants are different chunks");
            Assert.NotNull(d.entry); Assert.NotNull(d.exit);
            Assert.IsTrue(d.orderedChunks);
            Assert.IsFalse(d.allowBoulevard, "two-lane streets and shared paths: no 4-lane sections");
            foreach (RoadChunk c in AllChunks(d)) Assert.IsTrue(c.name.StartsWith(spec.prefix), c.name + ": chunk naming");
            Assert.That(d.obstacles, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.obstacles.All(o => o != null));
            Assert.That(d.obstacles.Length, Is.InRange(2, 5), "2-4 own obstacles (or so)");
            Assert.IsTrue(d.obstacles.All(o => o.name.StartsWith("OBS_")), "own obstacles are OBS_");
            Assert.That(d.jumpObstacles, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.jumpObstacles.All(o => o != null && o.canJumpOver));
            Assert.NotNull(d.gifts, "gifts: empty = the theme's");
            Assert.That(d.pedestrians, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.pedestrians.All(p => p != null && p.GetComponent<Pedestrian>() != null));
            Assert.That(d.hazards, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.hazards.All(h => h != null));
            Assert.That(d.ramps, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.ramps.All(r => r != null && r.name.StartsWith("RMP_")));
            Assert.That(d.rampFrequency, Is.InRange(.5f, 2f));
            Assert.NotNull(d.farScenery, "far scenery");
            Assert.That(d.farScenery.GetComponentsInChildren<Collider>(true), Is.Empty, "far scenery has no colliders");
            Assert.That(d.farScenery.GetComponentsInChildren<MeshFilter>(true).Sum(f => f.sharedMesh.GetIndexCount(0) / 3), Is.GreaterThan(1500), "far scenery is a full horizon band");
            Assert.IsTrue(d.gateway == null || d.gateway.name.StartsWith("GW_"), "gateway: empty or the generic sign from DistrictSetup");
            foreach (Color c in new[] { d.fogColor, d.skyTint, d.ambientSky, d.ambientEquator, d.ambientGround, d.sunColor })
                Assert.Greater(c.maxColorComponent, .3f, "look colours are set");
            Assert.That(d.sunIntensity, Is.InRange(.8f, 2f));
            Assert.IsTrue(d.pedestrians.Any(p => p.GetComponent<Obstacle>().animal), "animals on the path");
            Assert.IsTrue(d.chunks.Any(c => c.GetComponentInChildren<Ramp>(true) != null || c.GetComponentInChildren<RoadGap>(true) != null), "an authored ramp or gap chunk");
            switch (id)
            {
                case "mciver":
                    Assert.IsTrue(AllChunks(d).All(c => c.roadType == RoadType.Street), "Wellington St is a normal street");
                    Assert.IsTrue(d.pedestrians.Any(p => p.name.Contains("Ibis")), "ibises");
                    break;
                case "claisebrook":
                    Assert.IsTrue(d.chunks.Any(c => c.roadType == RoadType.PedestrianMall), "the cove promenade (no cars)");
                    Assert.IsTrue(d.chunks.Any(c => c.roadType == RoadType.Street), "Royal St and the depot road");
                    Assert.IsTrue(d.chunks.Any(c => c.GetComponentInChildren<RoadGap>(true) != null), "the canal SPLASH gap");
                    break;
                case "eastperth":
                    Assert.IsTrue(d.chunks.Any(c => c.roadType == RoadType.PedestrianMall), "the shared path beside the terminal (no cars)");
                    Assert.IsTrue(d.pedestrians.Any(p => p.name.Contains("Swan")), "black swans");
                    break;
            }
        }

        [TestCaseSource(nameof(Ids))]
        public void EveryChunk_KeepsTheStreetCrossSection_Length_AndSlope(string id)
        {
            District d = Load(id);
            foreach (RoadChunk c in AllChunks(d))
            {
                Assert.AreEqual(4f, c.halfWidth, 1e-4f, c.name + " halfWidth");
                Assert.IsTrue(c.endHalfWidth == 0f || Mathf.Abs(c.endHalfWidth - 4f) < 1e-4f, c.name + " endHalfWidth 0 or 4");
                Assert.AreEqual(4f, c.HalfWidthAt(0f), 1e-4f, c.name + " starts at the street width");
                Assert.AreEqual(4f, c.HalfWidthAt(c.length), 1e-4f, c.name + " ends at the street width");
                Assert.AreEqual(4f, c.sidewalkWidth, 1e-4f, c.name + " sidewalkWidth");
                Assert.AreEqual(.2f, c.sidewalkHeight, 1e-4f, c.name + " sidewalkHeight");
                Assert.AreEqual(.3f, c.curbTransitionWidth, 1e-4f, c.name + " curbTransitionWidth");
                Assert.Greater(c.length, 0f);
                Assert.AreEqual(0f, Mathf.Repeat(c.length, 10f), 1e-3f, c.name + ": length is a multiple of 10 m");
                float per40 = c.endHeight * 40f / c.length;
                Assert.That(per40, Is.InRange(-8f, 0f), c.name + ": slope between -8 and 0 per 40 m");
                Assert.Less(per40, 0f, c.name + ": still downhill");
                Assert.AreEqual(Vector3.zero, c.transform.position, c.name + " root at zero");
                Assert.AreEqual(Quaternion.identity, c.transform.rotation, c.name);
                Assert.AreEqual(Vector3.one, c.transform.localScale, c.name);
            }
        }

        [TestCaseSource(nameof(Ids))]
        public void EveryChunk_StaysInBudget_AndUsesOnlyTheTwoComicMaterials(string id)
        {
            var palette = AssetDatabase.LoadAssetAtPath<Material>(Palette);
            var ink = AssetDatabase.LoadAssetAtPath<Material>(Ink);
            District d = Load(id);
            foreach (RoadChunk c in AllChunks(d))
            {
                var renderers = c.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.InRange(1, 8), c.name + ": at most 8 renderers");
                foreach (Renderer r in renderers)
                    foreach (Material m in r.sharedMaterials)
                        Assert.IsTrue(m == palette || m == ink, c.name + "/" + r.name + " uses " + (m != null ? m.name : "nothing"));
                long tris = 0;
                foreach (MeshFilter f in c.GetComponentsInChildren<MeshFilter>(true))
                {
                    Assert.NotNull(f.sharedMesh, c.name + "/" + f.name);
                    tris += f.sharedMesh.GetIndexCount(0) / 3;
                    Assert.Greater(f.sharedMesh.bounds.max.y, 40f, c.name + "/" + f.name + " bounds are not enlarged for the city curl");
                }
                Assert.LessOrEqual(tris, 25000, c.name + ": 25k triangles including the outlines");
                TestContext.WriteLine(c.name + ": " + tris + " triangles, " + renderers.Length + " renderers");
                // Landmarks and the railway (trains, platforms, gantries) are decoration: nothing baked in can be hit.
                Assert.That(c.GetComponentsInChildren<Obstacle>(true), Is.Empty, c.name + " has Obstacles baked in");
                foreach (Collider col in c.GetComponentsInChildren<Collider>(true))
                {
                    string group = col.transform.parent != null ? col.transform.parent.name : "";
                    Assert.IsTrue(group == "BoundaryColliders" || group == "PeopleKeepOut", c.name + "/" + col.name + ": a collider outside the boundary / keep-out groups");
                    Assert.IsInstanceOf<BoxCollider>(col, c.name + "/" + col.name);
                }
            }
            var prefabs = d.obstacles.Select(o => o.gameObject).Concat(d.ramps.Select(r => r.gameObject)).Concat(new[] { d.farScenery })
                .Concat(d.pedestrians.Where(p => AssetDatabase.GetAssetPath(p).Contains("/Districts/"))).Where(g => g != null).Distinct();
            foreach (GameObject g in prefabs)
                foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                    foreach (Material m in r.sharedMaterials)
                        Assert.IsTrue(m == palette || m == ink, g.name + "/" + r.name + " uses " + (m != null ? m.name : "nothing"));
        }

        [TestCaseSource(nameof(Ids))]
        public void TheRailwayIsBesideTheRoad_NeverOnTheRideableWidth(string id)
        {
            // Boundary walls keep people out of the scenery: none of them may reach into the rideable width.
            foreach (RoadChunk c in AllChunks(Load(id)))
            {
                Transform walls = c.transform.Find("BoundaryColliders");
                if (walls == null) continue;
                foreach (BoxCollider b in walls.GetComponentsInChildren<BoxCollider>(true))
                {
                    float inner = Mathf.Abs(b.transform.localPosition.x) - b.size.x * .5f;
                    Assert.GreaterOrEqual(inner, c.RideableHalfWidth - .01f, c.name + "/" + b.name + " reaches into the rideable width");
                }
            }
        }

        // ---- v0.6: the continuous corridor (docs/v0.6-polish.md, the east line corridor standard; numbers of EastLineKit,
        // repeated here on purpose: changing the standard must break this test)
        const float Track1 = 14f, Track2 = 18.5f, RailOffset = .55f, RailTop = .3f, ContactWire = 5.6f, MessengerMidSpan = 6.05f;
        const float FenceX = 8.5f, FenceTop = 2f, MastNear = 11.25f, MastFar = 21.25f, PortalTop = 7.6f, PortalZ = 20f;

        sealed class FlatMesh
        {
            public Vector3[] V;
            public Color[] C;
        }

        /// <summary>A chunk mesh with the chunk's slope taken out: y = metres above the road at that z. (Vertices are
        /// read from a temporary copy: the saved meshes are not readable.)</summary>
        static FlatMesh Flat(RoadChunk c, string child)
        {
            Transform t = c.transform.Find(child);
            Assert.NotNull(t, c.name + "/" + child);
            Mesh copy = Object.Instantiate(t.GetComponent<MeshFilter>().sharedMesh);
            try
            {
                // The saved meshes are not readable (the builders clear the flag); the editor still holds their data.
                var so = new SerializedObject(copy);
                SerializedProperty readable = so.FindProperty("m_IsReadable");
                if (readable != null) { readable.boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }
                var m = new FlatMesh { V = copy.vertices, C = copy.colors };
                if (m.V.Length == 0) Assert.Inconclusive("mesh data not readable here");
                float grade = -c.endHeight / c.length;
                for (int i = 0; i < m.V.Length; i++) m.V[i].y += grade * m.V[i].z;
                return m;
            }
            finally { Object.DestroyImmediate(copy); }
        }

        [TestCaseSource(nameof(Ids))]
        public void TheRailwayCorridor_RunsThroughEveryChunk_AndIsTheStandardAtBothEnds(string id)
        {
            // Rule 1 and 2 of v0.6: the Midland line (left of the road here) passes every chunk from z = 0 to its end, and
            // at both ends of every chunk the fence, the two tracks and the wires are where the standard puts them, so any
            // two chunks join: the next one in order, the first one again (the cycle), the exit, the next district.
            foreach (RoadChunk c in AllChunks(Load(id)))
            {
                FlatMesh road = Flat(c, "RoadAndSidewalk"), front = Flat(c, "Frontage");
                foreach (float end in new[] { 0f, c.length })
                {
                    if (c.name == "CHN_Mciver_Entry" && end == 0f) continue; // the line begins here: a tunnel under a road bridge
                    string at = c.name + " at z = " + end + ": ";
                    foreach (float track in new[] { Track1, Track2 })
                    {
                        foreach (float o in new[] { -RailOffset, RailOffset })
                            Assert.IsTrue(road.V.Any(p => Mathf.Abs(p.z - end) < .05f && Mathf.Abs(-p.x - (track + o)) < .06f && Mathf.Abs(p.y - RailTop) < .01f),
                                at + "a rail of the track at " + track + " (rail head " + RailTop + " above the road)");
                        Assert.IsTrue(road.V.Any(p => Mathf.Abs(p.z - end) < .05f && Mathf.Abs(-p.x - track) < .01f && Mathf.Abs(p.y - ContactWire) < .01f),
                            at + "the contact wire over the track at " + track + " at " + ContactWire);
                        Assert.IsTrue(road.V.Any(p => Mathf.Abs(p.z - end) < .05f && Mathf.Abs(-p.x - track) < .01f && Mathf.Abs(p.y - MessengerMidSpan) < .02f),
                            at + "the messenger wire over the track at " + track + " at its lowest (" + MessengerMidSpan + ")");
                    }
                    Assert.IsTrue(front.V.Any(p => Mathf.Abs(p.z - end) < .12f && Mathf.Abs(-p.x - FenceX) < .06f && p.y > FenceTop),
                        at + "a post of the palisade fence at " + FenceX);
                }
                // the portal on the line's rhythm: both masts at z = 20, their caps above the lattice beam
                foreach (float mast in new[] { MastNear, c.name.StartsWith("CHN_Mciver_") ? -1f : MastFar })
                    if (mast > 0f)
                        Assert.IsTrue(front.V.Any(p => Mathf.Abs(p.z - PortalZ) < .25f && Mathf.Abs(-p.x - mast) < .2f && p.y > PortalTop),
                            c.name + ": the overhead-line portal's mast at " + mast + ", z = " + PortalZ);
            }
        }

        [Test]
        public void EntryAndExit_MeetTheirNeighbours_OnTheBoundarySurface()
        {
            // Rule 4: grey asphalt and pale flagstone sidewalks at z = 0 of every entry and at the end of every exit; a
            // district's own paving begins and ends inside its own chunks.
            Color flagstone = new Color32(0xD3, 0xCF, 0xC6, 0xFF);
            foreach (string id in Ids)
            {
                District d = Load(id);
                foreach ((RoadChunk c, float end) in new[] { (d.entry, 0f), (d.exit, d.exit.length) })
                {
                    FlatMesh road = Flat(c, "RoadAndSidewalk");
                    // the sidewalk's outer edge (x = 8, at kerb height): its top, its outer face and the slab end meet there
                    var sidewalk = Enumerable.Range(0, road.V.Length)
                        .Where(i => Mathf.Abs(road.V[i].z - end) < .05f && Mathf.Abs(road.V[i].y - .2f) < .003f && Mathf.Abs(Mathf.Abs(road.V[i].x) - 8f) < .01f).ToList();
                    Assert.That(sidewalk, Is.Not.Empty, c.name + ": sidewalk at z = " + end);
                    foreach (int i in sidewalk)
                    {
                        Color got = road.C[i];
                        Assert.Less(Mathf.Abs(got.r - flagstone.linear.r) + Mathf.Abs(got.g - flagstone.linear.g) + Mathf.Abs(got.b - flagstone.linear.b), .03f,
                            c.name + ": the sidewalk at z = " + end + " is the boundary flagstone");
                    }
                }
            }
        }

        [Test]
        public void McIverAndClaisebrook_ShareOneFarScenery()
        {
            // Rule 7: the same horizon = the same prefab, so it stays up across the boundary.
            Assert.AreSame(Load("mciver").farScenery, Load("claisebrook").farScenery);
            Assert.AreNotSame(Load("claisebrook").farScenery, Load("eastperth").farScenery, "East Perth's horizon is the river: its own far scenery");
        }

        [TestCaseSource(nameof(Ids))]
        public void SpawnSlots_AreInLanes_KeepAFreeLane_AndAreNeverOnRampsOrGaps(string id)
        {
            foreach (RoadChunk c in AllChunks(Load(id)))
            {
                SpawnSlot[] slots = c.GetSlots();
                Assert.That(slots, Is.Not.Empty, c.name);
                Ramp[] ramps = c.GetComponentsInChildren<Ramp>(true);
                RoadGap[] gaps = c.GetComponentsInChildren<RoadGap>(true);
                foreach (SpawnSlot s in slots)
                {
                    Vector3 p = s.transform.localPosition;
                    Assert.That(new[] { -2.6f, 0f, 2.6f }.Any(x => Mathf.Abs(p.x - x) < 1e-3f), c.name + "/" + s.name + ": in a lane (-2.6 / 0 / 2.6)");
                    Assert.AreEqual(c.HeightAt(p.z), p.y, 1e-3f, c.name + "/" + s.name + " sits on the road");
                    Assert.That(p.z, Is.InRange(0f, c.length), c.name + "/" + s.name);
                    foreach (Ramp r in ramps)
                    {
                        Vector3 rp = r.transform.localPosition;
                        bool inLane = Mathf.Abs(p.x - rp.x) <= r.width * .5f + 1.3f;
                        bool near = p.z > rp.z - 3f && p.z < rp.z + r.Length + 6f;
                        Assert.IsFalse(inLane && near, c.name + "/" + s.name + " is on or right next to ramp " + r.name);
                    }
                    foreach (RoadGap g in gaps)
                    {
                        float gz = g.transform.localPosition.z;
                        Assert.IsFalse(p.z > gz - 1f && p.z < gz + g.length + 3f, c.name + "/" + s.name + " is over the gap");
                    }
                }
                foreach (var row in slots.GroupBy(s => Mathf.RoundToInt(s.transform.localPosition.z * 2f)))
                    Assert.LessOrEqual(row.Count(), 3, c.name + " row " + row.Key / 2f + ": the generator always leaves one lane free");
                if (ramps.Length == 0 && gaps.Length == 0)
                {
                    var rows = slots.GroupBy(s => Mathf.RoundToInt(s.transform.localPosition.z)).OrderBy(g => g.Key).ToList();
                    CollectionAssert.AreEqual(new[] { 10, 25 }, rows.Select(r => r.Key).ToArray(), c.name + ": the street's rows z = 10 / 25");
                    foreach (var row in rows) Assert.AreEqual(3, row.Count(), c.name + " row " + row.Key);
                }
            }
        }

        [TestCaseSource(nameof(Ids))]
        public void RampsAreSensible_AndEveryRoadWideGapHasAClearableRampBeforeIt(string id)
        {
            District d = Load(id);
            foreach (Ramp r in d.ramps) CheckRamp(r, r.name);
            var tuning = AssetDatabase.LoadAssetAtPath<GameTuning>(TuningPath);
            if (tuning == null) tuning = GameTuning.Default;
            foreach (RoadChunk c in AllChunks(d))
            {
                Ramp[] ramps = c.GetComponentsInChildren<Ramp>(true);
                foreach (Ramp r in ramps)
                {
                    CheckRamp(r, c.name + "/" + r.name);
                    Vector3 p = r.transform.localPosition;
                    Assert.AreEqual(c.HeightAt(p.z), p.y, 1e-3f, c.name + "/" + r.name + ": pivot on the road surface at the start of the run-up");
                    Assert.AreEqual(Quaternion.identity, r.transform.localRotation, c.name + "/" + r.name + " faces +Z");
                    Assert.LessOrEqual(p.z + r.Length, c.length, c.name + "/" + r.name + " fits in its chunk");
                    Assert.LessOrEqual(Mathf.Abs(p.x) + r.width * .5f, c.RideableHalfWidth + .5f, c.name + "/" + r.name + " stays on the rideable width");
                }
                foreach (RoadGap g in c.GetComponentsInChildren<RoadGap>(true))
                {
                    Vector3 gp = g.transform.localPosition;
                    Assert.AreEqual(0f, gp.x, 1e-4f, c.name + "/" + g.name + " pivot on the centre line");
                    Assert.AreEqual(c.HeightAt(gp.z), gp.y, 1e-3f, c.name + "/" + g.name);
                    Assert.LessOrEqual(gp.z + g.length, c.length, c.name + "/" + g.name + " fits in its chunk");
                    Assert.IsTrue(g.water ? g.word == "SPLASH" : g.word == "WHOOPS", c.name + "/" + g.name + ": SPLASH for water, WHOOPS for a drop");
                    bool roadWide = g.width <= 0f || g.width >= 2f * c.halfWidth;
                    if (!roadWide) continue;
                    Ramp before = ramps.Where(r => r.transform.localPosition.z + r.Length <= gp.z + .01f && gp.z - (r.transform.localPosition.z + r.Length) <= 12f)
                        .OrderByDescending(r => r.transform.localPosition.z + r.Length).FirstOrDefault();
                    Assert.NotNull(before, c.name + "/" + g.name + ": a road-wide gap needs a Ramp ending within 12 m before it");
                    float covered = g.width <= 0f ? 2f * c.RideableHalfWidth : g.width;
                    Assert.GreaterOrEqual(before.width + .01f, covered, c.name + ": the ramp before " + g.name + " covers the whole gap width");
                    float run = gp.z - (before.transform.localPosition.z + before.Length);
                    // RiderController's own maths (RampFlight); the flight follows the road downhill, so the grade does not change it.
                    float flight = RampFlight.Distance(before, StartSpeed, tuning);
                    TestContext.WriteLine(c.name + "/" + g.name + ": gap " + g.length + " m, " + run + " m after the lip; flight at " + StartSpeed + " m/s = " + flight.ToString("F1") + " m");
                    Assert.Greater(flight, run + g.length + .5f, c.name + "/" + g.name + " cannot be cleared at the start speed");
                }
            }
        }

        static void CheckRamp(Ramp r, string label)
        {
            Assert.That(r.width, Is.InRange(1.5f, 16.01f), label + " width");
            Assert.That(r.rise, Is.InRange(1f, 15f), label + " rise");
            Assert.That(r.height, Is.InRange(.3f, 3f), label + " height");
            Assert.That(r.height / r.rise, Is.InRange(.1f, .45f), label + " slope");
            Assert.That(r.top, Is.InRange(0f, 12f), label + " top");
            Assert.That(r.launch, Is.InRange(.5f, 1.6f), label + " launch");
            Assert.That(r.hangGravity, Is.InRange(.3f, 1f), label + " hangGravity");
            Assert.That(r.GetComponentsInChildren<Collider>(true), Is.Empty, label + ": ramps have no colliders");
            Assert.IsNull(r.GetComponent<Obstacle>(), label);
        }

        [TestCaseSource(nameof(Ids))]
        public void AnimalsAreAnimals_AndNeverPunchable(string id)
        {
            District d = Load(id);
            foreach (GameObject p in d.pedestrians)
            {
                var o = p.GetComponent<Obstacle>();
                Assert.NotNull(o, p.name);
                Assert.AreEqual(ObstacleKind.Person, o.kind, p.name);
                Assert.IsFalse(o.punchable, p.name + ": people and animals are never punchable");
                Assert.IsFalse(o.police, p.name);
            }
            var animals = d.pedestrians.Where(p => p.name.StartsWith("ANM_")).Distinct().ToList();
            Assert.That(animals, Is.Not.Empty, "the district's own animals");
            foreach (GameObject a in animals)
            {
                var o = a.GetComponent<Obstacle>();
                Assert.IsTrue(o.animal, a.name + " is an animal");
                Assert.AreEqual(ObstacleKind.Person, o.kind, a.name + ": animals are kind Person");
                Assert.IsFalse(o.punchable, a.name + ": a punch only shoos animals");
                Assert.NotNull(a.GetComponent<Pedestrian>(), a.name);
                Assert.That(a.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger), Is.Not.Empty, a.name + " needs a collider");
            }
            foreach (Obstacle o in d.obstacles)
            {
                Assert.That(o.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger), Is.Not.Empty, o.name + " needs a collider");
                Assert.IsFalse(o.animal, o.name);
                Assert.AreEqual(ObstacleKind.Thing, o.kind, o.name);
            }
        }
    }
}
#endif
