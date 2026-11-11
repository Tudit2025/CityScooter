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
            // v0.6: the cycle is longer than the district (CBD 16, quay 10), a second pass with other places and the same
            // gameplay order, so no landmark comes round twice; plain chunks may be the same prefab in both passes.
            Assert.That(d.chunks.Length, Is.InRange(4, 16), "the chunk cycle");
            Assert.That(d.chunks.Distinct().Count(), Is.InRange(4, 10), "4-6 variants (plus the CBD's widen / narrow transitions and the second pass)");
            Assert.That(d.chunks.Length * 40f, Is.GreaterThanOrEqualTo(d.length.y - 80f), "the cycle covers the longest ride through the district (no landmark twice)");
                        Assert.NotNull(d.entry); Assert.NotNull(d.exit);
            Assert.IsTrue(d.orderedChunks);
            Assert.IsFalse(d.allowBoulevard, "no boulevard sections: the Terrace is 4 lanes by itself, the quay is a promenade");
            Assert.That(d.obstacles, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.obstacles.All(o => o != null));
            Assert.That(d.pedestrians, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.pedestrians.All(p => p != null && p.GetComponent<Pedestrian>() != null));
            Assert.That(d.hazards, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.hazards.All(h => h != null));
            Assert.That(d.ramps, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.ramps.All(r => r != null));
            Assert.Greater(d.rampFrequency, 0f);
            Assert.NotNull(d.farScenery, "far scenery");
            Assert.That(d.farScenery.GetComponentsInChildren<Collider>(true), Is.Empty, "far scenery has no colliders");
            // The generic gateway sign comes from DistrictSetup (GW_<Id>): empty before it ran, its sign after.
            Assert.IsTrue(d.gateway == null || d.gateway.name.StartsWith("GW_"), "gateway: empty or the generic sign from DistrictSetup");
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
                Assert.That(d.chunks.Count(c => !c.ChangesWidth && c.halfWidth >= 7.99f), Is.GreaterThanOrEqualTo(3), "the 4-lane Terrace (halfWidth 8)");
                foreach (RoadChunk c in d.chunks.Where(c => c.roadType == RoadType.PedestrianMall))
                    Assert.IsTrue(!c.ChangesWidth && Mathf.Abs(c.halfWidth - 4f) < 1e-4f, c.name + ": Hay Street Mall stays narrow");
                Assert.IsTrue(d.hazards.Any(h => h.roadHalfWidth >= 7.99f), "4-lane traffic patterns for the Terrace");
            }
            else
            {
                Assert.IsTrue(d.pedestrians.Any(p => p.GetComponent<Obstacle>().animal), "seagulls");
                Assert.IsTrue(d.chunks.Any(c => c.GetComponentInChildren<RoadGap>(true) != null), "a water gap");
                Assert.IsTrue(d.chunks.Any(c => c.GetComponentsInChildren<Ramp>(true).Any(r => r.top > 0f)), "the bridge hump platform");
            }
        }

        static bool IsStandardOrWide(float w) => Mathf.Abs(w - 4f) < 1e-4f || Mathf.Abs(w - 8f) < 1e-4f;

        [TestCaseSource(nameof(Paths))]
        public void EveryChunk_KeepsTheStreetCrossSection_Length_AndSlope(string path)
        {
            District d = Load(path);
            // The district starts and ends at the standard street (entry and exit at both ends), and so does its ordered
            // cycle (first chunk starts, last chunk ends at 4); inside it the road may be 4 lanes (halfWidth 8).
            Assert.AreEqual(4f, d.entry.HalfWidthAt(0f), 1e-4f, "entry starts at the street width");
            Assert.AreEqual(4f, d.entry.HalfWidthAt(d.entry.length), 1e-4f, "entry ends at the street width");
            Assert.AreEqual(4f, d.exit.HalfWidthAt(0f), 1e-4f, "exit starts at the street width");
            Assert.AreEqual(4f, d.exit.HalfWidthAt(d.exit.length), 1e-4f, "exit ends at the street width");
            Assert.AreEqual(4f, d.chunks[0].HalfWidthAt(0f), 1e-4f, d.chunks[0].name + ": the cycle starts at the street width");
            RoadChunk last = d.chunks[d.chunks.Length - 1];
            Assert.AreEqual(4f, last.HalfWidthAt(last.length), 1e-4f, last.name + ": the cycle ends at the street width");
            for (int i = 0; i + 1 < d.chunks.Length; i++)
                Assert.AreEqual(d.chunks[i].HalfWidthAt(d.chunks[i].length), d.chunks[i + 1].HalfWidthAt(0f), 1e-4f, d.chunks[i].name + " -> " + d.chunks[i + 1].name + ": widths meet at the seam");
            foreach (RoadChunk c in AllChunks(d))
            {
                Assert.IsTrue(IsStandardOrWide(c.halfWidth), c.name + " halfWidth " + c.halfWidth);
                Assert.IsTrue(c.endHalfWidth == 0f || IsStandardOrWide(c.endHalfWidth), c.name + " endHalfWidth " + c.endHalfWidth);
                Assert.AreEqual(4f, c.sidewalkWidth, c.name + " sidewalkWidth");
                Assert.AreEqual(.2f, c.sidewalkHeight, 1e-4f, c.name + " sidewalkHeight");
                Assert.AreEqual(.3f, c.curbTransitionWidth, 1e-4f, c.name + " curbTransitionWidth");
                for (float z = 0f; z < c.length; z += .5f)
                    Assert.Less(Mathf.Abs(c.HalfWidthAt(z + .5f) - c.HalfWidthAt(z)), .15f, c.name + ": the width only changes smoothly, at z " + z);
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
                    float[] lanes = c.HalfWidthAt(p.z) >= 7.9f ? new[] { -6f, -2f, 2f, 6f } : new[] { -2.6f, 0f, 2.6f, -2f, 2f };
                    Assert.That(lanes.Any(x => Mathf.Abs(p.x - x) < 1e-3f), c.name + "/" + s.name + ": in a lane (street -2.6 / 0 / 2.6, 4 lanes -6 / -2 / 2 / 6)");
                    Assert.LessOrEqual(Mathf.Abs(p.x), c.HalfWidthAt(p.z) - 1f, c.name + "/" + s.name + " stays well inside the kerbs");
                    Assert.AreEqual(c.HeightAt(p.z), p.y, 1e-3f, c.name + "/" + s.name + " sits on the road");
                    Assert.That(p.z, Is.InRange(0f, c.length), c.name + "/" + s.name);
                    foreach (Ramp r in ramps)
                    {
                        Vector3 rp = r.transform.localPosition;
                        bool inLane = Mathf.Abs(p.x - rp.x) <= r.width * .5f + 1.3f;
                        // a launch ramp: its run-up, deck and landing zone; a down ramp (yaw 180): the deck it describes
                        bool near = IsDownRamp(r) ? p.z > rp.z - r.Length - 3f && p.z < rp.z + 3f : p.z > rp.z - 3f && p.z < rp.z + r.Length + 6f;
                        Assert.IsFalse(inLane && near, c.name + "/" + s.name + " is on or right next to ramp " + r.name);
                    }
                    foreach (RoadGap g in gaps)
                    {
                        float gz = g.transform.localPosition.z;
                        Assert.IsFalse(p.z > gz - 1f && p.z < gz + g.length + 3f, c.name + "/" + s.name + " is over the water gap");
                    }
                }
                // Every row keeps a free lane (the generator fills at most slots - 1 per row).
                foreach (var row in slots.GroupBy(s => Mathf.RoundToInt(s.transform.localPosition.z * 2f)))
                    Assert.LessOrEqual(row.Count(), 4, c.name + " row " + row.Key / 2f);
                if (ramps.Length == 0 && gaps.Length == 0 && !c.ChangesWidth)
                {
                    // Plain chunks: exactly the street's rows (z = 10 / 25 per 40 m), a slot on every lane centre.
                    var rows = slots.GroupBy(s => Mathf.RoundToInt(s.transform.localPosition.z)).OrderBy(g => g.Key).ToList();
                    CollectionAssert.AreEqual(new[] { 10, 25 }, rows.Select(r => r.Key).ToArray(), c.name);
                    foreach (var row in rows) Assert.AreEqual(c.halfWidth >= 7.9f ? 4 : 3, row.Count(), c.name + " row " + row.Key);
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
                    if (IsDownRamp(r)) Assert.GreaterOrEqual(p.z - r.Length, -.01f, c.name + "/" + r.name + " (down ramp) fits in its chunk");
                    else
                    {
                        Assert.AreEqual(Quaternion.identity, r.transform.localRotation, c.name + "/" + r.name + ": launch ramps face +Z, down ramps yaw 180");
                        Assert.LessOrEqual(p.z + r.Length, c.length, c.name + "/" + r.name + " fits in its chunk");
                    }
                    Assert.LessOrEqual(p.z, c.length + .01f, c.name + "/" + r.name);
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
                    Ramp before = ramps.Where(r => !IsDownRamp(r) && r.transform.localPosition.z + r.Length <= gp.z + .01f && gp.z - (r.transform.localPosition.z + r.Length) <= 12f)
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

        // A "down ramp" (yaw 180) describes a deck coming back down; it is not a launch.
        static bool IsDownRamp(Ramp r) => Quaternion.Angle(r.transform.localRotation, Quaternion.Euler(0f, 180f, 0f)) < .5f;

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

        // Metres from the lip to the landing at this speed at the ramp's foot: RiderController's own maths, shared by every
        // district test (RampFlightCheck). The flight follows the road downhill, so the grade does not change it.
        static float Flight(Ramp r, float speed, float grade) => RampFlightCheck.Flight(r, speed);

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
                Assert.IsTrue(IsStandardOrWide(p.roadHalfWidth), p.name + " roadHalfWidth");
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
                    if (p.roadHalfWidth >= 7.99f) Assert.That(Mathf.Abs(e.x), Is.EqualTo(2f).Or.EqualTo(6f), p.name + ": 4-lane traffic drives in a lane (x = +-2 or +-6)");
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
                    Assert.IsTrue(HasGap(blocked, p.roadHalfWidth, .3f), p.name + (mirror ? " (mirrored)" : "") + " leaves no straight lane on the road");
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
