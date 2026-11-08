using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CityScooter.Track;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CityScooter.Tests
{
    /// <summary>UWA and Matilda Bay districts (CrawleyBuilder): the District assets are filled in, every chunk keeps the
    /// street cross-section at both ends (docs/v0.3-districts.md), chunk budgets (25k triangles incl. outlines, 8
    /// renderers), only the two shared comic materials, sensible ramps, every road-wide gap has a ramp right before it,
    /// and animals are animals.</summary>
    public sealed class DistrictCrawleyTests
    {
        const string UwaPath = "Assets/_Project/Data/Districts/District_UWA.asset";
        const string MatildaPath = "Assets/_Project/Data/Districts/District_Matilda.asset";
        const string PalettePath = "Assets/_Project/Art/ComicCity/ComicPalette.mat";
        const string InkPath = "Assets/_Project/Art/ComicCity/ComicInk.mat";

        static District Load(string path)
        {
            var d = AssetDatabase.LoadAssetAtPath<District>(path);
            Assert.NotNull(d, path + " exists (run CityScooter/Districts/Build UWA + Matilda Bay)");
            return d;
        }

        static IEnumerable<RoadChunk> AllChunks(District d) => new[] { d.entry }.Concat(d.chunks).Concat(new[] { d.exit });

        static Vector3 Local(RoadChunk c, Transform t) => c.transform.InverseTransformPoint(t.position);

        [TestCase(UwaPath, "uwa", "UWA", 10)]
        [TestCase(MatildaPath, "matilda", "MATILDA BAY", 20)]
        public void DistrictAssetIsFilledIn(string path, string id, string displayName, int order)
        {
            District d = Load(path);
            Assert.AreEqual(id, d.id);
            Assert.IsTrue(Regex.IsMatch(d.id, "^[a-z]+$"), "zone id: lower case letters only");
            Assert.AreEqual(displayName, d.displayName);
            Assert.IsTrue(Regex.IsMatch(d.displayName, "^[A-Z ]+$"), "display name: capitals and spaces");
            Assert.AreEqual(order, d.order);
            Assert.That(d.length.x, Is.GreaterThanOrEqualTo(200f));
            Assert.That(d.length.y, Is.GreaterThanOrEqualTo(d.length.x));
            Assert.IsTrue(d.HasChunks);
            Assert.That(d.chunks.Length, Is.InRange(4, 6), "4-6 chunk variants");
            Assert.NotNull(d.entry, "entry chunk");
            Assert.NotNull(d.exit, "exit chunk");
            Assert.IsFalse(AllChunks(d).GroupBy(c => c).Any(g => g.Count() > 1), "entry, variants and exit are different chunks");
            Assert.IsFalse(d.allowBoulevard, "no 4-lane boulevard on campus or by the river");

            Assert.That(d.obstacles.Length, Is.GreaterThanOrEqualTo(2));
            foreach (Obstacle o in d.obstacles)
            {
                Assert.NotNull(o, "obstacle");
                Assert.IsTrue(o.GetComponentsInChildren<Collider>(true).Any(c => !c.isTrigger), o.name + " has a solid collider");
                Assert.AreNotEqual(ObstacleKind.Person, o.kind, o.name + ": people and animals go in pedestrians");
            }
            Assert.That(d.pedestrians.Length, Is.GreaterThanOrEqualTo(3));
            foreach (GameObject p in d.pedestrians)
            {
                Assert.NotNull(p, "pedestrian");
                Assert.NotNull(p.GetComponent<Pedestrian>(), p.name + " has Pedestrian");
                Assert.AreEqual(ObstacleKind.Person, p.GetComponent<Obstacle>().kind, p.name);
                Assert.IsTrue(p.GetComponentsInChildren<Collider>(true).Any(c => !c.isTrigger), p.name + " has a solid collider");
            }
            Assert.IsTrue(d.pedestrians.Any(p => p.GetComponent<Obstacle>().animal), "the district's animal walks around");
            Assert.That(d.ramps.Length, Is.GreaterThanOrEqualTo(1));
            Assert.IsTrue(d.ramps.All(r => r != null), "ramps");
            Assert.That(d.rampFrequency, Is.GreaterThan(0f));

            Assert.NotNull(d.farScenery, "far scenery");
            Assert.IsEmpty(d.farScenery.GetComponentsInChildren<Collider>(true), "far scenery has no colliders");
            Assert.IsNull(d.farScenery.GetComponentInChildren<Obstacle>(true));
            // The generic gateway sign comes from DistrictSetup (GW_<Id>): empty before it ran, its sign after.
            Assert.IsTrue(d.gateway == null || d.gateway.name.StartsWith("GW_"), "gateway: empty or the generic sign from DistrictSetup");
            foreach (Color c in new[] { d.fogColor, d.ambientSky, d.ambientEquator, d.ambientGround, d.sunColor, d.skyTint })
                Assert.AreEqual(1f, c.a, 1e-4f, "opaque look colours");
            Assert.That(d.sunIntensity, Is.InRange(.8f, 2f));
            Assert.That(d.blendMeters, Is.GreaterThanOrEqualTo(10f));
        }

        [TestCase(UwaPath)]
        [TestCase(MatildaPath)]
        public void EveryChunkStartsAndEndsWithTheStreetCrossSection(string path)
        {
            District d = Load(path);
            foreach (RoadChunk c in AllChunks(d))
            {
                Assert.NotNull(c);
                string n = c.name;
                Assert.AreEqual(Vector3.zero, c.transform.localPosition, n + ": root at zero");
                Assert.AreEqual(4f, c.halfWidth, 1e-4f, n + " halfWidth");
                Assert.AreEqual(4f, c.sidewalkWidth, 1e-4f, n + " sidewalkWidth");
                Assert.AreEqual(.2f, c.sidewalkHeight, 1e-4f, n + " sidewalkHeight");
                Assert.AreEqual(.3f, c.curbTransitionWidth, 1e-4f, n + " curbTransitionWidth");
                Assert.IsTrue(c.endHalfWidth == 0f || Mathf.Abs(c.endHalfWidth - 4f) < 1e-4f, n + " endHalfWidth 0 or 4");
                Assert.AreEqual(4f, c.HalfWidthAt(0f), 1e-4f, n + " starts 8 m wide");
                Assert.AreEqual(4f, c.HalfWidthAt(c.length), 1e-4f, n + " ends 8 m wide");
                Assert.AreEqual(0f, Mathf.Repeat(c.length, 10f), 1e-3f, n + ": length is a multiple of 10 m");
                Assert.That(c.length, Is.InRange(10f, 80f), n);
                float perForty = c.endHeight * 40f / c.length;
                Assert.That(perForty, Is.InRange(-8f, 0f), n + ": downhill between 0 and -8 per 40 m");
                Assert.AreEqual(RoadType.Street, c.roadType, n);
                Assert.That(c.weight, Is.GreaterThan(0f), n);

                SpawnSlot[] slots = c.GetSlots();
                Assert.That(slots.Length, Is.GreaterThanOrEqualTo(4), n + " has spawn slots");
                foreach (SpawnSlot s in slots)
                {
                    Vector3 p = Local(c, s.transform);
                    Assert.That(Mathf.Abs(p.x), Is.LessThanOrEqualTo(c.halfWidth), n + "/" + s.name + " on the road");
                    Assert.That(p.z, Is.InRange(0f, c.length), n + "/" + s.name);
                    Assert.AreEqual(c.HeightAt(p.z), p.y, 1e-3f, n + "/" + s.name + " on the surface");
                    Assert.IsTrue(new[] { -2.6f, 0f, 2.6f }.Any(x => Mathf.Abs(x - p.x) < 1e-3f), n + "/" + s.name + " in a lane column");
                }
                // Every row keeps at least one lane free of slots... i.e. never fills more than the three columns.
                foreach (var row in slots.GroupBy(s => Mathf.Round(Local(c, s.transform).z)))
                    Assert.That(row.Count(), Is.LessThanOrEqualTo(3), n + " row " + row.Key);
            }
        }

        [TestCase(UwaPath)]
        [TestCase(MatildaPath)]
        public void ChunksStayWithinBudget_AndUseOnlyTheComicMaterials(string path)
        {
            District d = Load(path);
            var palette = AssetDatabase.LoadAssetAtPath<Material>(PalettePath);
            var ink = AssetDatabase.LoadAssetAtPath<Material>(InkPath);
            Assert.NotNull(palette); Assert.NotNull(ink);
            var objects = new List<GameObject>();
            objects.AddRange(AllChunks(d).Select(c => c.gameObject));
            objects.AddRange(d.obstacles.Select(o => o.gameObject));
            objects.AddRange(d.pedestrians.Where(p => p.GetComponent<Obstacle>().animal));
            objects.AddRange(d.ramps.Select(r => r.gameObject));
            objects.Add(d.farScenery);
            foreach (GameObject go in objects)
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                    foreach (Material m in r.sharedMaterials)
                        Assert.IsTrue(m == palette || m == ink, go.name + "/" + r.name + " uses " + (m != null ? m.name : "null"));
            foreach (RoadChunk c in AllChunks(d))
            {
                int tris = c.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Sum(f => (int)f.sharedMesh.GetIndexCount(0) / 3);
                int renderers = c.GetComponentsInChildren<MeshRenderer>(true).Length;
                Assert.That(tris, Is.LessThanOrEqualTo(25000), c.name + " triangles incl. outlines");
                Assert.That(tris, Is.GreaterThan(3000), c.name + " is not empty");
                Assert.That(renderers, Is.LessThanOrEqualTo(8), c.name + " renderers");
                Assert.IsNull(c.GetComponentInChildren<SkinnedMeshRenderer>(true), c.name);
                // Road surfaces and grounds never cast shadows.
                foreach (MeshRenderer r in c.GetComponentsInChildren<MeshRenderer>(true))
                    if (r.name == "RoadAndSidewalk" || r.name == "Grounds" || r.name == "InkOutline")
                        Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, r.shadowCastingMode, c.name + "/" + r.name);
                // Obstacles placed in the chunk are on the sidewalk or the road, never crash walls across the road.
                foreach (Obstacle o in c.GetComponentsInChildren<Obstacle>(true))
                    Assert.IsFalse(o.spansRoad, c.name + "/" + o.name);
            }
        }

        [TestCase(UwaPath)]
        [TestCase(MatildaPath)]
        public void RampsAreSensible_AndEveryRoadWideGapHasARampRightBeforeIt(string path)
        {
            District d = Load(path);
            void CheckRamp(Ramp r, string where)
            {
                Assert.That(r.width, Is.InRange(1f, 16.01f), where + " width");
                Assert.That(r.rise, Is.InRange(2f, 12f), where + " rise");
                Assert.That(r.height, Is.InRange(.5f, 2.5f), where + " height");
                Assert.That(r.height / r.rise, Is.LessThanOrEqualTo(.45f), where + " slope");
                Assert.That(r.top, Is.InRange(0f, 6f), where + " top");
                Assert.That(r.launch, Is.InRange(.5f, 1.6f), where + " launch");
                Assert.That(r.hangGravity, Is.InRange(.2f, 1f), where + " hangGravity");
                Assert.IsTrue(r.GetComponentsInChildren<Collider>(true).All(c => c.GetComponentInParent<Obstacle>() != null), where + ": a ramp has no plain colliders");
            }
            foreach (Ramp r in d.ramps) CheckRamp(r, r.name);
            int gaps = 0, inChunkRamps = 0;
            foreach (RoadChunk c in AllChunks(d))
            {
                Ramp[] ramps = c.GetComponentsInChildren<Ramp>(true);
                inChunkRamps += ramps.Length;
                foreach (Ramp r in ramps)
                {
                    CheckRamp(r, c.name + "/" + r.name);
                    Vector3 p = Local(c, r.transform);
                    Assert.That(p.z, Is.GreaterThanOrEqualTo(2f), c.name + "/" + r.name + " starts inside the chunk");
                    Assert.That(p.z + r.Length, Is.LessThanOrEqualTo(c.length - 2f), c.name + "/" + r.name + " ends inside the chunk");
                    Assert.AreEqual(c.HeightAt(p.z), p.y, 1e-3f, c.name + "/" + r.name + " on the road surface");
                    Assert.That(Mathf.Abs(p.x) + r.width * .5f, Is.LessThanOrEqualTo(c.RideableHalfWidth + .01f), c.name + "/" + r.name + " within the rideable width");
                }
                foreach (RoadGap gap in c.GetComponentsInChildren<RoadGap>(true))
                {
                    gaps++;
                    string n = c.name + "/" + gap.name;
                    Vector3 g = Local(c, gap.transform);
                    Assert.That(gap.length, Is.InRange(2f, 8f), n + ": short enough to fly over at normal speed");
                    Assert.That(g.z, Is.GreaterThan(0f), n);
                    Assert.That(g.z + gap.length, Is.LessThan(c.length), n + " ends inside the chunk");
                    Assert.IsFalse(string.IsNullOrEmpty(gap.word), n + " comic word");
                    float gx0 = gap.width <= 0f ? -c.RideableHalfWidth : g.x - gap.width * .5f, gx1 = gap.width <= 0f ? c.RideableHalfWidth : g.x + gap.width * .5f;
                    Ramp before = ramps.FirstOrDefault(r =>
                    {
                        Vector3 p = Local(c, r.transform);
                        float lip = p.z + r.Length;
                        return lip <= g.z + .01f && lip >= g.z - 12f && p.x - r.width * .5f <= gx0 + .01f && p.x + r.width * .5f >= gx1 - .01f;
                    });
                    if (gap.width <= 0f) Assert.NotNull(before, n + ": a road-wide gap needs a ramp across the whole width ending within 12 m before it");
                    else Assert.NotNull(before, n + ": the gap's lanes have a ramp right before it");
                }
            }
            Assert.That(gaps, Is.GreaterThanOrEqualTo(1), "a SPLASH gap somewhere in the district");
            Assert.That(inChunkRamps, Is.GreaterThanOrEqualTo(1), "a ramp built into a chunk");
        }

        [TestCase(UwaPath, "Peacock")]
        [TestCase(MatildaPath, "BlackSwan")]
        public void AnimalsAreAnimals(string path, string animal)
        {
            District d = Load(path);
            GameObject[] animals = d.pedestrians.Where(p => p.name.Contains(animal)).Distinct().ToArray();
            Assert.That(animals.Length, Is.GreaterThanOrEqualTo(1), animal);
            foreach (GameObject a in animals)
            {
                var o = a.GetComponent<Obstacle>();
                Assert.AreEqual(ObstacleKind.Person, o.kind, a.name);
                Assert.IsTrue(o.animal, a.name + " is an animal (punch only shoos it)");
                Assert.IsFalse(o.punchable, a.name + " is never punchable");
                Assert.IsFalse(o.police, a.name);
                var ped = a.GetComponent<Pedestrian>();
                Assert.NotNull(ped.art, a.name + " has an Art child that bobs");
                Assert.That(ped.walkSpeed, Is.InRange(0f, 1f), a.name + " wanders slowly");
                Assert.IsFalse(ped.crosses, a.name);
            }
            foreach (GameObject p in d.pedestrians.Where(p => !p.GetComponent<Obstacle>().animal))
                Assert.IsTrue(p.name.StartsWith("HZ_Ped_"), p.name + ": people are the shared street people");
        }
    }
}
