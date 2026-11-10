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
    /// <summary>SUCCESS HILL, GUILDFORD and EAST GUILDFORD (East4Builder, east art agent 4): every field the builder owns
    /// is filled, id and order, the street cross-section at both ends of every chunk, lengths and slopes, budgets, the
    /// two shared comic materials, colliders (the railway and the landmarks are scenery), spawn slots, ramps and road
    /// gaps (a clearable ramp before every road-wide gap), animals, far scenery.</summary>
    public sealed class DistrictEast4Tests
    {
        const string Folder = "Assets/_Project/Data/Districts/";
        const string Palette = "Assets/_Project/Art/ComicCity/ComicPalette.mat";
        const string Ink = "Assets/_Project/Art/ComicCity/ComicInk.mat";
        const string TuningPath = "Assets/_Project/Data/Tuning_ComicDownhill.asset";

        static readonly string[] Ids = { "successhill", "guildford", "eastguildford" };
        static IEnumerable<string> AllIds => Ids;

        static string Tag(string id) => char.ToUpperInvariant(id[0]) + id.Substring(1);
        static string Name(string id) => id == "successhill" ? "SUCCESS HILL" : id == "guildford" ? "GUILDFORD" : "EAST GUILDFORD";
        static int Order(string id) => id == "successhill" ? 300 : id == "guildford" ? 310 : 320;

        static District Load(string id)
        {
            var d = AssetDatabase.LoadAssetAtPath<District>(Folder + "District_" + Tag(id) + ".asset");
            Assert.NotNull(d, "District_" + Tag(id) + ".asset is missing (run East4Builder.BuildBatch)");
            return d;
        }

        static IEnumerable<RoadChunk> AllChunks(District d) => new[] { d.entry }.Concat(d.chunks).Concat(new[] { d.exit });

        [TestCaseSource(nameof(AllIds))]
        public void DistrictAsset_EveryOwnedFieldIsFilled(string id)
        {
            District d = Load(id);
            Assert.AreEqual(id, d.id);
            Assert.AreEqual(Name(id), d.displayName);
            Assert.AreEqual(Order(id), d.order);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(d.id, "^[a-z]+$"), "id: lower case letters only");
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(d.displayName, "^[A-Z ]+$"), "display name: A-Z and spaces");
            Assert.IsTrue(d.HasChunks);
            Assert.That(d.chunks.Length, Is.InRange(4, 6), "4-6 variants");
            Assert.AreEqual(d.chunks.Length, d.chunks.Distinct().Count(), "variants are different chunks");
            Assert.NotNull(d.entry); Assert.NotNull(d.exit);
            Assert.IsTrue(d.orderedChunks);
            Assert.IsFalse(d.allowBoulevard, "no 4-lane sections out here");
            // Every chunk is a place (a station, a landmark): the district's length lays entry, each chunk once, exit.
            float body = d.chunks.Sum(c => c.length), entry = d.entry.length, exit = d.exit.length;
            float lastStart = entry + body - d.chunks[d.chunks.Length - 1].length;
            Assert.Greater(Mathf.Min(d.length.x, d.length.y), lastStart + exit, id + ": long enough for its last chunk");
            Assert.LessOrEqual(Mathf.Max(d.length.x, d.length.y), entry + body + exit, id + ": not so long that the landmarks come round twice");
            Assert.That(entry + body + exit, Is.InRange(240f, 460f), "about 250-450 m of road");
            foreach (RoadChunk c in AllChunks(d))
                Assert.IsTrue(c.name.StartsWith("CHN_" + Tag(id) + "_"), c.name + ": chunk naming");
            Assert.That(d.obstacles, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.obstacles.All(o => o != null));
            Assert.IsTrue(d.obstacles.All(o => o.name.StartsWith("OBS_" + Tag(id) + "_")), "own obstacles are OBS_<Id>_");
            Assert.That(d.jumpObstacles, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.jumpObstacles.All(o => o != null && o.canJumpOver));
            Assert.NotNull(d.gifts, "gifts: empty = the theme's");
            Assert.That(d.pedestrians, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.pedestrians.All(p => p != null && p.GetComponent<Pedestrian>() != null));
            Assert.That(d.hazards, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.hazards.All(h => h != null));
            Assert.That(d.ramps, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.ramps.All(r => r != null && r.name.StartsWith("RMP_" + Tag(id) + "_")));
            Assert.That(d.rampFrequency, Is.InRange(.5f, 2f));
            Assert.NotNull(d.farScenery, "far scenery");
            Assert.IsTrue(d.farScenery.name.StartsWith("FAR_" + Tag(id) + "_"));
            Assert.That(d.farScenery.GetComponentsInChildren<Collider>(true), Is.Empty, "far scenery has no colliders");
            Assert.That(d.farScenery.GetComponentsInChildren<MeshFilter>(true).Sum(f => f.sharedMesh.GetIndexCount(0) / 3), Is.GreaterThan(2000), "far scenery is a full horizon band");
            Assert.IsTrue(d.gateway == null || d.gateway.name.StartsWith("GW_"), "gateway: empty or the generic sign from DistrictSetup");
            foreach (Color c in new[] { d.fogColor, d.skyTint, d.ambientSky, d.ambientEquator, d.ambientGround, d.sunColor })
                Assert.Greater(c.maxColorComponent, .3f, "look colours are set");
            Assert.That(d.sunIntensity, Is.InRange(.8f, 2f));
            Assert.IsTrue(d.pedestrians.Any(p => p.GetComponent<Obstacle>().animal), "animals beside the path");
            Assert.IsTrue(AllChunks(d).Any(c => c.GetComponentInChildren<Ramp>(true) != null || c.GetComponentInChildren<RoadGap>(true) != null),
                id + ": at least one authored ramp or gap");
            if (id == "successhill")
            {
                Assert.IsTrue(AllChunks(d).All(c => c.roadType == RoadType.PedestrianMall), "the shared path: no cars");
                Assert.IsTrue(d.chunks.Any(c => c.GetComponentInChildren<RoadGap>(true) != null), "the boardwalk SPLASH gap");
            }
            else if (id == "guildford")
                Assert.IsTrue(AllChunks(d).All(c => c.roadType == RoadType.Street), "James St is a normal street");
            else
            {
                Assert.IsTrue(d.chunks.Any(c => c.roadType == RoadType.PedestrianMall), "shared path chunks (no cars)");
                Assert.IsTrue(d.chunks.Any(c => c.name.Contains("Chapel")), "the Guildford Grammar chapel chunk");
            }
        }

        [TestCaseSource(nameof(AllIds))]
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
                Assert.Less(per40, 0f, c.name + ": still downhill (gently, it is the river plain)");
                Assert.AreEqual(Vector3.zero, c.transform.position, c.name + " root at zero");
                Assert.AreEqual(Quaternion.identity, c.transform.rotation, c.name);
                Assert.AreEqual(Vector3.one, c.transform.localScale, c.name);
            }
        }

        [TestCaseSource(nameof(AllIds))]
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
            }
            var prefabs = d.obstacles.Select(o => o.gameObject).Concat(d.ramps.Select(r => r.gameObject)).Concat(new[] { d.farScenery })
                .Concat(d.pedestrians.Where(p => AssetDatabase.GetAssetPath(p).Contains("/Districts/" + Tag(id) + "/"))).Where(g => g != null).Distinct();
            foreach (GameObject g in prefabs)
                foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                    foreach (Material m in r.sharedMaterials)
                        Assert.IsTrue(m == palette || m == ink, g.name + "/" + r.name + " uses " + (m != null ? m.name : "nothing"));
        }

        [TestCaseSource(nameof(AllIds))]
        public void TheRailwayAndTheLandmarksAreScenery_NothingBakedInCanBeHit(string id)
        {
            foreach (RoadChunk c in AllChunks(Load(id)))
            {
                // Trains, platforms, gantries, the chapel, the hotels, the stone circle: decoration only.
                Assert.That(c.GetComponentsInChildren<Obstacle>(true), Is.Empty, c.name + " has Obstacles baked in");
                foreach (Collider col in c.GetComponentsInChildren<Collider>(true))
                {
                    string parent = col.transform.parent != null ? col.transform.parent.name : "";
                    Assert.IsTrue(parent == "BoundaryColliders" || parent == "PeopleKeepOut", c.name + "/" + col.name + ": only boundary walls and keep-out boxes carry colliders");
                    // boundary walls stay off the rideable width (road + both sidewalks)
                    if (parent != "BoundaryColliders") continue;
                    Bounds b = col.bounds;
                    Assert.IsTrue(b.min.x >= 7.99f || b.max.x <= -7.99f, c.name + "/" + col.name + " reaches onto the sidewalk: x " + b.min.x + " .. " + b.max.x);
                }
            }
        }

        [TestCaseSource(nameof(AllIds))]
        public void SpawnSlots_AreInLanes_KeepAFreeLane_AndAreNeverOnRampsOrWater(string id)
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
                        Assert.IsFalse(p.z > gz - 1f && p.z < gz + g.length + 3f, c.name + "/" + s.name + " is over the water gap");
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

        [TestCaseSource(nameof(AllIds))]
        public void RampsAreSensible_AndEveryRoadWideGapHasAClearableRampBeforeIt(string id)
        {
            var tuning = AssetDatabase.LoadAssetAtPath<GameTuning>(TuningPath);
            if (tuning == null) tuning = GameTuning.Default;
            District d = Load(id);
            foreach (Ramp r in d.ramps) CheckRamp(r, r.name);
            int authored = 0, roadWideGaps = 0;
            foreach (RoadChunk c in AllChunks(d))
            {
                Ramp[] ramps = c.GetComponentsInChildren<Ramp>(true);
                foreach (Ramp r in ramps)
                {
                    authored++;
                    CheckRamp(r, c.name + "/" + r.name);
                    Vector3 p = r.transform.localPosition;
                    Assert.AreEqual(c.HeightAt(p.z), p.y, 1e-3f, c.name + "/" + r.name + ": pivot on the road surface at the start of the run-up");
                    Assert.AreEqual(Quaternion.identity, r.transform.localRotation, c.name + "/" + r.name + " faces +Z");
                    Assert.LessOrEqual(p.z + r.Length, c.length, c.name + "/" + r.name + " fits in its chunk");
                    Assert.LessOrEqual(Mathf.Abs(p.x) + r.width * .5f, c.RideableHalfWidth + .5f, c.name + "/" + r.name + " stays on the rideable width");
                    Assert.Greater(p.z, 4f, c.name + "/" + r.name + ": room to see it coming");
                }
                foreach (RoadGap g in c.GetComponentsInChildren<RoadGap>(true))
                {
                    authored++;
                    Vector3 gp = g.transform.localPosition;
                    Assert.AreEqual(0f, gp.x, 1e-4f, c.name + "/" + g.name + " pivot on the centre line");
                    Assert.AreEqual(c.HeightAt(gp.z), gp.y, 1e-3f, c.name + "/" + g.name);
                    Assert.LessOrEqual(gp.z + g.length, c.length, c.name + "/" + g.name + " fits in its chunk");
                    Assert.AreEqual("SPLASH", g.word, c.name + "/" + g.name + " comic word");
                    Assert.IsTrue(g.water, c.name + "/" + g.name + " is water");
                    bool roadWide = g.width <= 0f || g.width >= 2f * c.halfWidth;
                    if (!roadWide) continue;
                    roadWideGaps++;
                    Ramp before = ramps.Where(r => r.transform.localPosition.z + r.Length <= gp.z + .01f && gp.z - (r.transform.localPosition.z + r.Length) <= 12f)
                        .OrderByDescending(r => r.transform.localPosition.z + r.Length).FirstOrDefault();
                    Assert.NotNull(before, c.name + "/" + g.name + ": a road-wide gap needs a Ramp ending within 12 m before it");
                    float covered = g.width <= 0f ? 2f * c.RideableHalfWidth : g.width;
                    Assert.GreaterOrEqual(before.width + .01f, covered, c.name + ": the ramp before " + g.name + " covers the whole gap width");
                    float run = gp.z - (before.transform.localPosition.z + before.Length);
                    float flight = RampFlight.Distance(before, tuning.startSpeed, tuning);
                    TestContext.WriteLine(c.name + "/" + g.name + ": gap " + g.length + " m, " + run + " m after the lip; flight at " + tuning.startSpeed + " m/s = " + flight.ToString("F1") + " m");
                    Assert.Greater(flight, run + g.length + .5f, c.name + "/" + g.name + " cannot be cleared at the start speed");
                }
            }
            Assert.Greater(authored, 0, id + ": an authored ramp or gap in its chunks");
            if (id == "successhill") Assert.Greater(roadWideGaps, 0, "the boardwalk SPLASH gap");
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

        [TestCaseSource(nameof(AllIds))]
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
                Assert.IsTrue(a.name.StartsWith("ANM_" + Tag(id) + "_"), a.name + ": own animals are ANM_<Id>_");
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
