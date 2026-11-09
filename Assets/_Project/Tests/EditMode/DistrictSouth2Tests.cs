using System.Collections.Generic;
using System.Linq;
using CityScooter.Track;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CityScooter.Tests
{
    /// <summary>COCKBURN CENTRAL and AUBIN GROVE (south line, art agent 2; CockburnAubinBuilder): every District field the
    /// builder owns is filled, the street cross-section at both ends of every chunk, lengths and slopes, budgets, the two
    /// shared comic materials, spawn slots, ramps and road gaps (a clearable ramp before every road-wide gap), animals,
    /// patterns, and far scenery that fills the horizon (no city: the south line's skyline is sunk).</summary>
    public sealed class DistrictSouth2Tests
    {
        const string CockburnPath = "Assets/_Project/Data/Districts/District_Cockburn.asset";
        const string AubinPath = "Assets/_Project/Data/Districts/District_AubinGrove.asset";
        const string Palette = "Assets/_Project/Art/ComicCity/ComicPalette.mat";
        const string Ink = "Assets/_Project/Art/ComicCity/ComicInk.mat";
        const float RiderRadius = 0.35f, Margin = 0.15f;
        // Rider numbers (Tuning_ComicDownhill): start speed 9 m/s, gravity 20.
        const float StartSpeed = 9f, Gravity = 20f;

        static IEnumerable<string> Paths => new[] { CockburnPath, AubinPath };

        static District Load(string path)
        {
            var d = AssetDatabase.LoadAssetAtPath<District>(path);
            Assert.NotNull(d, path + " is missing (run CockburnAubinBuilder.BuildBatch)");
            return d;
        }

        static IEnumerable<RoadChunk> AllChunks(District d) => new[] { d.entry }.Concat(d.chunks).Concat(new[] { d.exit });

        [TestCaseSource(nameof(Paths))]
        public void DistrictAsset_IsFilledIn(string path)
        {
            District d = Load(path);
            bool cockburn = path == CockburnPath;
            Assert.AreEqual(cockburn ? "cockburn" : "aubingrove", d.id);
            Assert.AreEqual(cockburn ? "COCKBURN CENTRAL" : "AUBIN GROVE", d.displayName);
            Assert.AreEqual(cockburn ? 100 : 110, d.order);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(d.id, "^[a-z]+$"), "id: lower case letters only");
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(d.displayName, "^[A-Z ]+$"), "display name: A-Z and spaces");
            Assert.That(d.length.x, Is.InRange(300f, 450f));
            Assert.That(d.length.y, Is.InRange(d.length.x, 450f));
            Assert.IsTrue(d.HasChunks);
            Assert.That(d.chunks.Length, Is.InRange(4, 6), "4-6 variants");
            Assert.AreEqual(d.chunks.Length, d.chunks.Distinct().Count(), "variants are different chunks");
            Assert.NotNull(d.entry); Assert.NotNull(d.exit);
            Assert.IsFalse(d.chunks.Contains(d.entry) || d.chunks.Contains(d.exit), "entry and exit are their own chunks");
            Assert.IsTrue(d.orderedChunks);
            Assert.IsFalse(d.allowBoulevard);
            string prefix = cockburn ? "CHN_Cockburn_" : "CHN_AubinGrove_";
            foreach (RoadChunk c in AllChunks(d)) StringAssert.StartsWith(prefix, c.name);
            Assert.That(d.obstacles, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.obstacles.All(o => o != null));
            Assert.NotNull(d.jumpObstacles); Assert.NotNull(d.gifts);
            Assert.That(d.pedestrians, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.pedestrians.All(p => p != null && p.GetComponent<Pedestrian>() != null));
            Assert.That(d.hazards, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.hazards.All(h => h != null));
            Assert.That(d.ramps, Is.Not.Null.And.Not.Empty); Assert.IsTrue(d.ramps.All(r => r != null));
            Assert.Greater(d.rampFrequency, 0f);
            Assert.NotNull(d.farScenery, "far scenery");
            Assert.That(d.farScenery.GetComponentsInChildren<Collider>(true), Is.Empty, "far scenery has no colliders");
            Assert.IsTrue(d.gateway == null || d.gateway.name.StartsWith("GW_"), "gateway: empty or the generic sign from DistrictSetup");
            foreach (Color c in new[] { d.fogColor, d.skyTint, d.ambientSky, d.ambientEquator, d.ambientGround, d.sunColor })
                Assert.Greater(c.maxColorComponent, .3f, "look colours are set");
            Assert.That(d.sunIntensity, Is.InRange(.8f, 2f));
            if (cockburn)
            {
                Assert.IsTrue(d.chunks.All(c => c.roadType == RoadType.Street), "a town-centre street with traffic");
                Assert.IsTrue(d.obstacles.Any(o => o.name.Contains("Trolley") && o.canJumpOver), "shopping trolleys you can jump");
                Assert.IsTrue(d.obstacles.Any(o => o.name.Contains("DeliveryVan") && o.kind == ObstacleKind.Vehicle), "delivery vans");
                Assert.IsTrue(d.obstacles.Any(o => o.name.Contains("AFrame")), "A-frame signs");
                Assert.IsTrue(d.hazards.Any(h => h.name.StartsWith("HZ_Cockburn_")), "the district's own patterns");
                Assert.IsTrue(d.ramps.Any(r => r.name.Contains("CarPark")), "the car park ramp");
            }
            else
            {
                Assert.IsTrue(d.chunks.Any(c => c.roadType == RoadType.PedestrianMall), "shared path chunks (no cars)");
                Assert.IsTrue(d.chunks.Any(c => c.roadType == RoadType.Street), "an estate street");
                Assert.IsTrue(d.pedestrians.Any(p => p.GetComponent<Obstacle>().animal && p.name.Contains("Kangaroo")), "western grey kangaroos");
                Assert.IsTrue(d.pedestrians.Any(p => p.GetComponentInChildren<KangarooHop>(true) != null), "kangaroos hop");
                Assert.IsTrue(d.chunks.Any(c => c.GetComponentInChildren<RoadGap>(true) != null), "the wetland drain gap");
                Assert.IsTrue(d.obstacles.Any(o => o.name.Contains("WheelieBins")), "bin day");
                Assert.IsTrue(d.ramps.Any(r => r.name.Contains("Limestone")), "the limestone kicker");
            }
        }

        [TestCaseSource(nameof(Paths))]
        public void EveryChunk_KeepsTheStreetCrossSection_Length_AndSlope(string path)
        {
            District d = Load(path);
            foreach (RoadChunk c in AllChunks(d))
            {
                Assert.AreEqual(4f, c.halfWidth, 1e-4f, c.name + " halfWidth");
                Assert.IsTrue(c.endHalfWidth == 0f || Mathf.Abs(c.endHalfWidth - 4f) < 1e-4f, c.name + " endHalfWidth " + c.endHalfWidth);
                Assert.AreEqual(4f, c.HalfWidthAt(0f), 1e-4f, c.name + " starts at the street width");
                Assert.AreEqual(4f, c.HalfWidthAt(c.length), 1e-4f, c.name + " ends at the street width");
                Assert.AreEqual(4f, c.sidewalkWidth, c.name + " sidewalkWidth");
                Assert.AreEqual(.2f, c.sidewalkHeight, 1e-4f, c.name + " sidewalkHeight");
                Assert.AreEqual(.3f, c.curbTransitionWidth, 1e-4f, c.name + " curbTransitionWidth");
                Assert.Greater(c.length, 0f);
                Assert.AreEqual(0f, Mathf.Repeat(c.length, 10f), 1e-3f, c.name + ": length is a multiple of 10 m");
                float per40 = c.endHeight * 40f / c.length;
                Assert.That(per40, Is.InRange(-8f, 0f), c.name + ": slope between -8 and 0 per 40 m");
                Assert.Less(per40, -.5f, c.name + ": still downhill");
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
                Assert.LessOrEqual(tris, 25000, c.name + ": 25k triangles including the outlines");
                TestContext.WriteLine(c.name + ": " + tris + " triangles, " + renderers.Length + " renderers");
                Assert.That(c.GetComponentsInChildren<Obstacle>(true), Is.Empty, c.name + " has Obstacles baked in");
            }
            District d = Load(path);
            var prefabs = d.obstacles.Select(o => o.gameObject).Concat(d.ramps.Select(r => r.gameObject)).Concat(new[] { d.farScenery })
                .Concat(d.hazards.SelectMany(h => h.entries).Select(e => e.prefab)).Concat(d.pedestrians).Where(g => g != null).Distinct();
            foreach (GameObject g in prefabs)
                foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                    foreach (Material m in r.sharedMaterials)
                        Assert.IsTrue(m == palette || m == ink || !AssetDatabase.GetAssetPath(g).Contains("/Districts/"), g.name + "/" + r.name + " uses " + (m != null ? m.name : "nothing"));
            foreach (Obstacle o in d.obstacles.Where(o => AssetDatabase.GetAssetPath(o).Contains("/Districts/")))
            {
                long t = o.GetComponentsInChildren<MeshFilter>(true).Sum(f => (long)f.sharedMesh.GetIndexCount(0) / 3);
                Assert.LessOrEqual(t, 6000, o.name + ": props stay small");
            }
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
                    Assert.LessOrEqual(row.Count(), 3, c.name + " row " + row.Key / 2f + ": a free lane stays free (the generator fills at most slots - 1)");
                if (ramps.Length == 0 && gaps.Length == 0)
                {
                    // Plain chunks: exactly the street's rows (z = 10 / 25 per 40 m), a slot on every lane centre.
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
            int gapsSeen = 0;
            foreach (RoadChunk c in AllChunks(d))
            {
                Ramp[] ramps = c.GetComponentsInChildren<Ramp>(true);
                foreach (Ramp r in ramps)
                {
                    CheckRamp(r, c.name + "/" + r.name);
                    Vector3 p = r.transform.localPosition;
                    Assert.AreEqual(c.HeightAt(p.z), p.y, 1e-3f, c.name + "/" + r.name + ": pivot on the road surface at the start of the run-up");
                    Assert.AreEqual(Quaternion.identity, r.transform.localRotation, c.name + "/" + r.name + ": launch ramps face +Z");
                    Assert.LessOrEqual(p.z + r.Length, c.length, c.name + "/" + r.name + " fits in its chunk");
                    Assert.LessOrEqual(Mathf.Abs(p.x) + r.width * .5f, c.RideableHalfWidth + .5f, c.name + "/" + r.name + " stays on the rideable width");
                }
                foreach (RoadGap g in c.GetComponentsInChildren<RoadGap>(true))
                {
                    gapsSeen++;
                    Vector3 gp = g.transform.localPosition;
                    Assert.AreEqual(0f, gp.x, 1e-4f, c.name + "/" + g.name + " pivot on the centre line");
                    Assert.AreEqual(c.HeightAt(gp.z), gp.y, 1e-3f, c.name + "/" + g.name);
                    Assert.LessOrEqual(gp.z + g.length, c.length, c.name + "/" + g.name + " fits in its chunk");
                    Assert.AreEqual("SPLASH", g.word, c.name + "/" + g.name + ": water");
                    Assert.IsTrue(g.water, c.name + "/" + g.name);
                    bool roadWide = g.width <= 0f || g.width >= 2f * c.halfWidth;
                    if (!roadWide) continue;
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
            if (path == AubinPath) Assert.AreEqual(1, gapsSeen, "the wetland drain");
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
            float t = 0f;
            while (t < 5f)
            {
                t += .005f;
                float y = r.height + vy * t - .5f * g * t * t;
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
            if (path == AubinPath) Assert.That(animals, Is.Not.Empty, "kangaroos");
            foreach (GameObject a in animals)
            {
                var o = a.GetComponent<Obstacle>();
                Assert.AreEqual(ObstacleKind.Person, o.kind, a.name + ": animals are kind Person");
                Assert.IsFalse(o.punchable, a.name + ": a punch only shoos animals");
                var ped = a.GetComponent<Pedestrian>();
                Assert.NotNull(ped, a.name);
                Assert.NotNull(ped.art, a.name + ": the walk bob moves the art, not the collider");
                Assert.That(a.GetComponentsInChildren<BoxCollider>(true).Where(b => !b.isTrigger), Is.Not.Empty, a.name + " needs a collider");
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
                StringAssert.Contains("/Districts/" + d.id + "/", AssetDatabase.GetAssetPath(p), p.name);
                Assert.That(p.entries, Is.Not.Empty, p.name);
                Assert.Greater(p.length, 0f, p.name);
                Assert.AreEqual(4f, p.roadHalfWidth, 1e-4f, p.name + " roadHalfWidth");
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
                    Assert.IsTrue(HasGap(blocked, p.roadHalfWidth, .3f), p.name + (mirror ? " (mirrored)" : "") + " leaves no straight lane on the road");
                }
            }
        }

        [TestCaseSource(nameof(Paths))]
        public void FarScenery_FillsTheHorizon_AheadAndToBothSides(string path)
        {
            // The south line has no city skyline (it is sunk): the far scenery is the whole horizon, ahead and out to
            // about 70 degrees either side, and keeps out of the road corridor the chunks are laid in.
            District d = Load(path);
            var filters = d.farScenery.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(filters, Is.Not.Empty);
            var points = new List<Vector3>();
            foreach (MeshFilter f in filters)
            {
                Mesh mesh = f.sharedMesh;
                Assert.NotNull(mesh);
                Assert.IsTrue(f.GetComponent<MeshRenderer>().shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off, "far scenery casts no shadows");
                Bounds b = mesh.bounds;
                Assert.Less(b.min.x, -200f, "reaches far to the left");
                Assert.Greater(b.max.x, 200f, "reaches far to the right");
                Assert.Greater(b.max.z, 200f, "reaches ahead");
                Assert.Less(b.min.z, -50f, "the ground also continues behind the rider");
            }
            // Something stands up above the ground (hills, buildings, trees) in every 10-degree sector from -70 to 70
            // that is not the road corridor itself. Mesh vertices are readable only in the editor's import data: use the
            // bounds of the triangles via a temporary readable copy.
            Mesh src = filters[0].sharedMesh;
            string assetPath = AssetDatabase.GetAssetPath(src);
            Assert.IsFalse(string.IsNullOrEmpty(assetPath), "far scenery mesh is an asset");
            Mesh copy = Object.Instantiate(src);
            try
            {
                Vector3[] v = copy.vertices;
                if (v.Length == 0) Assert.Inconclusive("mesh data not readable here");
                float grade = -d.chunks[0].endHeight / d.chunks[0].length;
                for (int sector = -7; sector < 7; sector++)
                {
                    float a0 = sector * 10f, a1 = a0 + 10f;
                    bool found = v.Any(p =>
                    {
                        if (new Vector2(p.x, p.z).magnitude < 60f) return false;
                        float ang = Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg;
                        return ang >= a0 && ang < a1 && p.y + grade * p.z > 5f;
                    });
                    bool corridor = Mathf.Abs(a0 + 5f) < 10f; // straight ahead the road itself leads into the distance
                    if (!corridor) Assert.IsTrue(found, d.id + ": something on the horizon between " + a0 + " and " + a1 + " degrees");
                }
                Assert.IsFalse(v.Any(p => Mathf.Abs(p.x) < 44f && p.z > 0f && p.z < 330f && p.y + grade * p.z > .5f), d.id + ": nothing stands in the road corridor ahead");
            }
            finally { Object.DestroyImmediate(copy); }
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
