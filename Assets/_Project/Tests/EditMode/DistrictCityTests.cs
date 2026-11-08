using System.Collections.Generic;
using System.Linq;
using CityScooter.Track;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CityScooter.Tests
{
    /// <summary>PERTH CBD and ELIZABETH QUAY districts (CbdBuilder, QuayBuilder): every District field filled, the street
    /// cross-section at both ends of every chunk, lengths and slopes, budgets, the two shared comic materials, spawn
    /// slots, ramps and road gaps (a ramp before every road-wide gap, clearable), animals and traffic patterns.</summary>
    public sealed class DistrictCityTests
    {
        const string CbdPath = "Assets/_Project/Data/Districts/District_CBD.asset";
        const string QuayPath = "Assets/_Project/Data/Districts/District_Quay.asset";
        const string Palette = "Assets/_Project/Art/ComicCity/ComicPalette.mat";
        const string Ink = "Assets/_Project/Art/ComicCity/ComicInk.mat";
        const float RiderRadius = 0.35f, Margin = 0.15f;
        // Rider numbers (Tuning_ComicDownhill): start speed 9 m/s, gravity 20.
        const float StartSpeed = 9f, Gravity = 20f;

        static IEnumerable<string> Paths => new[] { CbdPath, QuayPath };

        static District Load(string path)
        {
            var d = AssetDatabase.LoadAssetAtPath<District>(path);
            Assert.NotNull(d, path + " is missing (run QuayBuilder.BuildCbdAndQuayBatch)");
            return d;
        }

        static IEnumerable<RoadChunk> AllChunks(District d) => new[] { d.entry }.Concat(d.chunks).Concat(new[] { d.exit });

        [TestCaseSource(nameof(Paths))]
        public void DistrictAsset_IsFilledIn(string path)
        {
            District d = Load(path);
            bool cbd = path == CbdPath;
            Assert.AreEqual(cbd ? "cbd" : "quay", d.id);
            Assert.AreEqual(cbd ? "PERTH CBD" : "ELIZABETH QUAY", d.displayName);
            Assert.AreEqual(cbd ? 50 : 60, d.order);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(d.id, "^[a-z]+$"), "id: lower case letters only");
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(d.displayName, "^[A-Z ]+$"), "display name: A-Z and spaces");
            Assert.That(d.length.x, Is.GreaterThanOrEqualTo(200f));
            Assert.That(d.length.y, Is.GreaterThanOrEqualTo(d.length.x));
            Assert.IsTrue(d.HasChunks);
            Assert.That(d.chunks.Length, Is.InRange(4, 6), "4-6 chunk variants");
            Assert.AreEqual(d.chunks.Length, d.chunks.Distinct().Count(), "variants are different chunks");
            Assert.NotNull(d.entry); Assert.NotNull(d.exit);
            Assert.IsTrue(d.orderedChunks);
            Assert.AreEqual(cbd, d.allowBoulevard, "boulevards only in the CBD");
            Assert.That(d.obstacles, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.obstacles.All(o => o != null));
            Assert.That(d.pedestrians, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.pedestrians.All(p => p != null && p.GetComponent<Pedestrian>() != null));
            Assert.That(d.hazards, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.hazards.All(h => h != null));
            Assert.That(d.ramps, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.ramps.All(r => r != null));
            Assert.Greater(d.rampFrequency, 0f);
            Assert.NotNull(d.farScenery, "far scenery");
            Assert.That(d.farScenery.GetComponentsInChildren<Collider>(true), Is.Empty, "far scenery has no colliders");
            Assert.IsNull(d.gateway, "the generic gateway sign is made by the district system");
            foreach (Color c in new[] { d.fogColor, d.skyTint, d.ambientSky, d.ambientEquator, d.ambientGround, d.sunColor })
                Assert.Greater(c.maxColorComponent, .3f, "look colours are set");
            Assert.That(d.sunIntensity, Is.InRange(.8f, 2f));
            // The district's own stuff (and the street's patterns so roadworks and police still happen in the CBD).
            if (cbd)
            {
                Assert.IsTrue(d.hazards.Any(h => h.name.StartsWith("HZ_Cbd_Bus")), "bus traffic");
                Assert.IsTrue(d.hazards.Any(h => h.name.StartsWith("HZ_Cbd_DeliveryVan")), "delivery vans");
                Assert.IsTrue(d.hazards.Any(h => h.name.StartsWith("HZ_Roadworks")), "roadworks from the street");
                Assert.IsTrue(d.hazards.Any(h => h.name.StartsWith("HZ_Police")), "police from the street");
                Assert.IsTrue(d.chunks.Any(c => c.roadType == RoadType.PedestrianMall), "Hay Street Mall chunks");
            }
            else
            {
                Assert.IsTrue(d.pedestrians.Any(p => p.GetComponent<Obstacle>().animal), "seagulls");
                Assert.IsTrue(d.chunks.Any(c => c.GetComponentInChildren<RoadGap>(true) != null), "a water gap");
                Assert.IsTrue(d.chunks.Any(c => c.GetComponentsInChildren<Ramp>(true).Any(r => r.top > 0f)), "the bridge hump platform");
            }
        }

        [TestCaseSource(nameof(Paths))]
        public void EveryChunk_KeepsTheStreetCrossSection_Length_AndSlope(string path)
        {
            foreach (RoadChunk c in AllChunks(Load(path)))
            {
                Assert.AreEqual(4f, c.halfWidth, c.name + " halfWidth");
                Assert.AreEqual(4f, c.sidewalkWidth, c.name + " sidewalkWidth");
                Assert.AreEqual(.2f, c.sidewalkHeight, 1e-4f, c.name + " sidewalkHeight");
                Assert.AreEqual(.3f, c.curbTransitionWidth, 1e-4f, c.name + " curbTransitionWidth");
                Assert.IsTrue(c.endHalfWidth == 0f || Mathf.Abs(c.endHalfWidth - 4f) < 1e-4f, c.name + " ends at the street width");
                Assert.AreEqual(4f, c.HalfWidthAt(0f), 1e-4f, c.name); Assert.AreEqual(4f, c.HalfWidthAt(c.length), 1e-4f, c.name);
                Assert.Greater(c.length, 0f);
                Assert.AreEqual(0f, Mathf.Repeat(c.length, 10f), 1e-3f, c.name + ": length is a multiple of 10 m");
                float per40 = c.endHeight * 40f / c.length;
                Assert.That(per40, Is.InRange(-8f, 0f), c.name + ": slope between -8 and 0 per 40 m");
                Assert.AreEqual(Vector3.zero, c.transform.position, c.name + " root at zero");
                Assert.AreEqual(Quaternion.identity, c.transform.rotation, c.name);
                Assert.AreEqual(Vector3.one, c.transform.localScale, c.name);
            }
        }

        [TestCaseSource(nameof(Paths))]
        public void EveryChunk_StaysInBudget_UsesOnlyTheTwoComicMaterials_AndCoversTheCityCurl(string path)
        {
            var palette = AssetDatabase.LoadAssetAtPath<Material>(Palette);
            var ink = AssetDatabase.LoadAssetAtPath<Material>(Ink);
            foreach (RoadChunk c in AllChunks(Load(path)))
            {
                var renderers = c.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.InRange(4, 8), c.name + ": at most 8 renderers");
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
                var dressing = c.GetComponent<StreetDressing>();
                if (dressing != null && dressing.layouts != null && dressing.layouts.Length > 0)
                {
                    Assert.AreEqual(dressing.layouts.Length, dressing.collisionLayouts.Length);
                    tris += 2 * (dressing.layouts.Max(m => (long)m.GetIndexCount(0) / 3) - (long)dressing.layouts[0].GetIndexCount(0) / 3);
                }
                Assert.LessOrEqual(tris, 25000, c.name + ": 25k triangles including the outlines");
                TestContext.WriteLine(c.name + ": " + tris + " triangles, " + renderers.Length + " renderers");
                // Landmarks and buildings are decoration: nothing in a chunk can be hit except what gameplay spawns.
                Assert.That(c.GetComponentsInChildren<Obstacle>(true), Is.Empty, c.name + " has Obstacles baked in");
            }
            District d = Load(path);
            var prefabs = d.obstacles.Select(o => o.gameObject).Concat(d.ramps.Select(r => r.gameObject)).Concat(new[] { d.farScenery })
                .Concat(d.hazards.SelectMany(h => h.entries).Select(e => e.prefab)).Concat(d.pedestrians).Where(g => g != null).Distinct();
            foreach (GameObject g in prefabs)
                foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                    foreach (Material m in r.sharedMaterials)
                        Assert.IsTrue(m == palette || m == ink || !AssetDatabase.GetAssetPath(g).Contains("/Districts/"), g.name + "/" + r.name + " uses " + (m != null ? m.name : "nothing"));
        }

        [TestCaseSource(nameof(Paths))]
        public void SpawnSlots_AreOnTheRoad_InLanes_AndNeverOnRampsOrWater(string path)
        {
            foreach (RoadChunk c in AllChunks(Load(path)))
            {
                SpawnSlot[] slots = c.GetSlots();
                Assert.That(slots, Is.Not.Empty, c.name);
                Ramp[] ramps = c.GetComponentsInChildren<Ramp>(true);
                RoadGap[] gaps = c.GetComponentsInChildren<RoadGap>(true);
                foreach (SpawnSlot s in slots)
                {
                    Vector3 p = s.transform.localPosition;
                    Assert.That(new[] { -2.6f, 0f, 2.6f }.Any(x => Mathf.Abs(p.x - x) < 1e-3f), c.name + "/" + s.name + ": lanes x = -2.6 / 0 / 2.6");
                    Assert.AreEqual(c.HeightAt(p.z), p.y, 1e-3f, c.name + "/" + s.name + " sits on the road");
                    Assert.That(p.z, Is.InRange(0f, c.length), c.name + "/" + s.name);
                    foreach (Ramp r in ramps)
                    {
                        Vector3 rp = r.transform.localPosition;
                        bool inLane = Mathf.Abs(p.x - rp.x) <= r.width * .5f + 1.3f;
                        Assert.IsFalse(inLane && p.z > rp.z - 3f && p.z < rp.z + r.Length + 6f, c.name + "/" + s.name + " is on or right next to ramp " + r.name);
                    }
                    foreach (RoadGap g in gaps)
                    {
                        float gz = g.transform.localPosition.z;
                        Assert.IsFalse(p.z > gz - 1f && p.z < gz + g.length + 3f, c.name + "/" + s.name + " is over the water gap");
                    }
                }
                // Every row keeps a free lane (the generator fills at most slots - 1 per row).
                foreach (var row in slots.GroupBy(s => Mathf.RoundToInt(s.transform.localPosition.z * 2f)))
                    Assert.LessOrEqual(row.Count(), 3, c.name + " row " + row.Key / 2f);
                if (ramps.Length == 0 && gaps.Length == 0)
                {
                    // Plain chunks: exactly the street's rows (z = 10 / 25 per 40 m) and lanes.
                    var rows = slots.GroupBy(s => Mathf.RoundToInt(s.transform.localPosition.z)).OrderBy(g => g.Key).ToList();
                    CollectionAssert.AreEqual(new[] { 10, 25 }, rows.Select(r => r.Key).ToArray(), c.name);
                    foreach (var row in rows) Assert.AreEqual(3, row.Count(), c.name + " row " + row.Key);
                }
            }
        }

        [TestCaseSource(nameof(Paths))]
        public void RampsAreSensible_AndEveryRoadWideGapHasAClearableRampBeforeIt(string path)
        {
            District d = Load(path);
            foreach (Ramp r in d.ramps) CheckRamp(r, r.name);
            foreach (RoadChunk c in AllChunks(d))
            {
                Ramp[] ramps = c.GetComponentsInChildren<Ramp>(true);
                foreach (Ramp r in ramps)
                {
                    CheckRamp(r, c.name + "/" + r.name);
                    Vector3 p = r.transform.localPosition;
                    Assert.AreEqual(c.HeightAt(p.z), p.y, 1e-3f, c.name + "/" + r.name + ": pivot on the road surface at the start of the run-up");
                    Assert.AreEqual(Quaternion.identity, r.transform.localRotation, c.name + "/" + r.name);
                    Assert.LessOrEqual(p.z + r.Length, c.length, c.name + "/" + r.name + " fits in its chunk");
                    Assert.LessOrEqual(Mathf.Abs(p.x) + r.width * .5f, c.RideableHalfWidth + .5f, c.name + "/" + r.name + " stays on the rideable width");
                }
                foreach (RoadGap g in c.GetComponentsInChildren<RoadGap>(true))
                {
                    Vector3 gp = g.transform.localPosition;
                    Assert.AreEqual(0f, gp.x, 1e-4f, c.name + "/" + g.name + " pivot on the centre line");
                    Assert.AreEqual(c.HeightAt(gp.z), gp.y, 1e-3f, c.name + "/" + g.name);
                    Assert.LessOrEqual(gp.z + g.length, c.length, c.name + "/" + g.name + " fits in its chunk");
                    Assert.IsFalse(string.IsNullOrEmpty(g.word), c.name + "/" + g.name + " comic word");
                    bool roadWide = g.width <= 0f || g.width >= 2f * c.halfWidth;
                    if (!roadWide) continue;
                    // A ramp whose lip is at most 12 m before the gap, wide enough that nobody can ride round it.
                    Ramp before = ramps.Where(r => r.transform.localPosition.z + r.Length <= gp.z + .01f && gp.z - (r.transform.localPosition.z + r.Length) <= 12f)
                        .OrderByDescending(r => r.transform.localPosition.z + r.Length).FirstOrDefault();
                    Assert.NotNull(before, c.name + "/" + g.name + ": a road-wide gap needs a ramp ending within 12 m before it");
                    float covered = g.width <= 0f ? 2f * c.RideableHalfWidth : g.width;
                    Assert.GreaterOrEqual(before.width + .01f, covered, c.name + ": the ramp before " + g.name + " covers the whole gap width");
                    float run = gp.z - (before.transform.localPosition.z + before.Length);
                    float flight = Flight(before, StartSpeed, c.endHeight / c.length);
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

        // Horizontal distance from the lip back down to the road (which keeps going downhill at the chunk's grade).
        static float Flight(Ramp r, float speed, float grade)
        {
            float vy = speed * r.height / r.rise * r.launch, g = Gravity * r.hangGravity;
            float t = 0f, y = r.height;
            while (t < 5f)
            {
                t += .005f;
                y = r.height + vy * t - .5f * g * t * t;
                if (y <= grade * speed * t) break;
            }
            return speed * t;
        }

        [TestCaseSource(nameof(Paths))]
        public void AnimalsAreAnimals_AndPeopleAreNotPunchable(string path)
        {
            District d = Load(path);
            foreach (GameObject p in d.pedestrians)
            {
                var o = p.GetComponent<Obstacle>();
                Assert.NotNull(o, p.name);
                Assert.AreEqual(ObstacleKind.Person, o.kind, p.name);
                Assert.IsFalse(o.punchable, p.name + ": people and animals are never punchable");
                Assert.IsFalse(o.police, p.name);
            }
            var animals = d.pedestrians.Concat(d.hazards.SelectMany(h => h.entries).Select(e => e.prefab)).Where(g => g != null && g.GetComponent<Obstacle>() != null && g.GetComponent<Obstacle>().animal).Distinct().ToList();
            foreach (GameObject a in animals)
            {
                var o = a.GetComponent<Obstacle>();
                Assert.AreEqual(ObstacleKind.Person, o.kind, a.name + ": animals are kind Person");
                Assert.IsFalse(o.punchable, a.name + ": a punch only shoos animals");
                Assert.NotNull(a.GetComponent<Pedestrian>(), a.name);
            }
            foreach (Obstacle o in d.obstacles)
            {
                Assert.That(o.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger), Is.Not.Empty, o.name + " needs a collider");
                Assert.IsFalse(o.animal, o.name);
                if (o.kind == ObstacleKind.Person || o.kind == ObstacleKind.Vehicle) Assert.IsFalse(o.punchable, o.name);
            }
        }

        [TestCaseSource(nameof(Paths))]
        public void DistrictPatterns_AreValid_DriveOnTheLeft_AndKeepAStraightLane(string path)
        {
            District d = Load(path);
            var own = d.hazards.Where(h => AssetDatabase.GetAssetPath(h).StartsWith("Assets/_Project/Data/Districts/")).ToList();
            Assert.That(own, Is.Not.Empty, "district patterns live in Data/Districts/<id>/");
            foreach (HazardPattern p in own)
            {
                Assert.That(p.entries, Is.Not.Empty, p.name);
                Assert.Greater(p.length, 0f, p.name);
                Assert.AreEqual(4f, p.roadHalfWidth, p.name);
                foreach (HazardPattern.Entry e in p.entries)
                {
                    Assert.NotNull(e.prefab, p.name);
                    Assert.NotNull(e.prefab.GetComponent<Obstacle>(), p.name + "/" + e.prefab.name);
                    Assert.That(e.z, Is.InRange(0f, p.length), p.name + "/" + e.prefab.name);
                    var mover = e.prefab.GetComponent<Mover>();
                    if (mover == null) continue;
                    Assert.IsFalse(p.allowMirror, p.name + ": traffic is never mirrored");
                    if (mover.fractionOfRiderSpeed > 0f) Assert.Less(e.x, 0f, p.name + ": slower traffic keeps left");
                    else { Assert.Greater(e.x, 0f, p.name + ": oncoming traffic on the right"); Assert.AreEqual(180f, e.yaw, p.name); }
                }
                foreach (bool mirror in p.allowMirror ? new[] { false, true } : new[] { false })
                {
                    var blocked = new List<Vector2>();
                    foreach (HazardPattern.Entry e in p.entries)
                    {
                        var o = e.prefab.GetComponent<Obstacle>();
                        if (o.canJumpOver) continue;
                        float x = mirror ? -e.x : e.x, yaw = mirror ? -e.yaw : e.yaw;
                        Vector2 span = Span(e.prefab, x, yaw);
                        blocked.Add(new Vector2(span.x - RiderRadius - Margin, span.y + RiderRadius + Margin));
                    }
                    Assert.IsTrue(HasGap(blocked, 4f, .3f), p.name + (mirror ? " (mirrored)" : "") + " leaves no straight lane on the road");
                }
            }
        }

        static Vector2 Span(GameObject prefab, float x, float yaw)
        {
            float min = float.MaxValue, max = float.MinValue;
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            foreach (BoxCollider b in prefab.GetComponentsInChildren<BoxCollider>(true))
                for (int i = 0; i < 4; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.size * .5f, new Vector3((i & 1) == 0 ? -1 : 1, 0, (i & 2) == 0 ? -1 : 1));
                    float cx = x + (q * (corner + b.transform.localPosition)).x;
                    min = Mathf.Min(min, cx); max = Mathf.Max(max, cx);
                }
            Assert.Less(min, max, prefab.name + " has no BoxCollider");
            return new Vector2(min, max);
        }

        static bool HasGap(List<Vector2> blocked, float roadEdge, float width)
        {
            float lo = -roadEdge + RiderRadius, hi = roadEdge - RiderRadius, cursor = lo;
            blocked.Sort((a, b) => a.x.CompareTo(b.x));
            foreach (Vector2 b in blocked)
            {
                if (b.x - cursor >= width) return true;
                cursor = Mathf.Max(cursor, b.y);
            }
            return hi - cursor >= width;
        }
    }
}
