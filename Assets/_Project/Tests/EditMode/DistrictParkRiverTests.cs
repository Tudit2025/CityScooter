using System.Collections.Generic;
using System.Linq;
using CityScooter.Track;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CityScooter.Tests
{
    /// <summary>
    /// Kings Park and Mounts Bay Rd (v0.3 districts, KingsParkBuilder / MountsBayBuilder): the District asset is filled,
    /// every chunk keeps the standard cross-section at both ends, lengths and slopes are in the contract's range, the
    /// 25k triangle / 8 renderer budgets hold, only the two comic materials are used, ramps match their gaps, and the
    /// animals are animals.
    /// </summary>
    public sealed class DistrictParkRiverTests
    {
        static readonly string[] Paths = { "Assets/_Project/Data/Districts/District_KingsPark.asset", "Assets/_Project/Data/Districts/District_MountsBay.asset" };
        const string Palette = "Assets/_Project/Art/ComicCity/ComicPalette.mat", Ink = "Assets/_Project/Art/ComicCity/ComicInk.mat";

        static IEnumerable<District> Districts()
        {
            foreach (string p in Paths)
            {
                var d = AssetDatabase.LoadAssetAtPath<District>(p);
                Assert.NotNull(d, p + " (run CityScooter/Districts/Build Kings Park and Mounts Bay Rd)");
                yield return d;
            }
        }

        static IEnumerable<RoadChunk> AllChunks(District d) => new[] { d.entry }.Concat(d.chunks).Concat(new[] { d.exit });

        [Test]
        public void DistrictAssetsAreFilled()
        {
            var expected = new Dictionary<string, (string name, int order)> { ["kingspark"] = ("KINGS PARK", 30), ["mountsbay"] = ("MOUNTS BAY RD", 70) };
            foreach (District d in Districts())
            {
                Assert.IsTrue(expected.ContainsKey(d.id), d.name + " id " + d.id);
                Assert.AreEqual(expected[d.id].name, d.displayName, d.name);
                Assert.AreEqual(expected[d.id].order, d.order, d.name);
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(d.id, "^[a-z]+$"), "zone id is lower-case letters");
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(d.displayName, "^[A-Z ]+$"), "display name is capitals and spaces");
                Assert.That(d.length.x, Is.GreaterThanOrEqualTo(200f)); Assert.That(d.length.y, Is.GreaterThanOrEqualTo(d.length.x));
                Assert.IsTrue(d.HasChunks, d.name + " chunks");
                // Mounts Bay Rd (v0.6): twelve chunks, the six-chunk gameplay order twice with other places, so no landmark comes twice.
                Assert.That(d.chunks.Length, Is.InRange(4, d.id == "mountsbay" ? 12 : 6), d.name + ": 4-6 chunk variants (Mounts Bay Rd: two passes of six)");
                Assert.NotNull(d.entry, d.name + " entry"); Assert.NotNull(d.exit, d.name + " exit");
                Assert.IsFalse(d.allowBoulevard, d.name);
                Assert.That(d.obstacles, Is.Not.Empty); Assert.IsTrue(d.obstacles.All(o => o != null), d.name + " obstacles");
                Assert.That(d.pedestrians, Is.Not.Empty); Assert.IsTrue(d.pedestrians.All(p => p != null && p.GetComponent<Pedestrian>() != null), d.name + " pedestrians have Pedestrian");
                Assert.That(d.ramps, Is.Not.Empty); Assert.IsTrue(d.ramps.All(r => r != null), d.name + " ramps");
                Assert.That(d.rampFrequency, Is.GreaterThan(0f));
                Assert.NotNull(d.farScenery, d.name + " far scenery");
                Assert.IsEmpty(d.farScenery.GetComponentsInChildren<Collider>(true), "far scenery has no colliders");
                Assert.AreNotEqual(Color.white, d.fogColor);
                Assert.That(d.sunIntensity, Is.InRange(.5f, 3f));
                Assert.IsTrue(d.pedestrians.Concat(d.obstacles.Select(o => o.gameObject)).Any(g => g.GetComponent<Obstacle>().animal), d.name + " has an animal");
            }
        }

        static bool StdWidth(float w) => Mathf.Abs(w - 4f) < 1e-4f || Mathf.Abs(w - 8f) < 1e-4f;

        [Test]
        public void EveryChunkKeepsTheCrossSection_AndTheDistrictStartsAndEndsAsTheStreet()
        {
            foreach (District d in Districts())
            {
                foreach (RoadChunk c in AllChunks(d))
                {
                    Assert.NotNull(c, d.name);
                    string n = d.name + "/" + c.name;
                    Assert.AreEqual(0f, Mathf.Repeat(c.length, 10f), 1e-4f, n + ": length a multiple of 10");
                    // Ends: the street (half width 4) or, inside a district, the 4-lane road (8); same kerb and sidewalks.
                    Assert.IsTrue(StdWidth(c.HalfWidthAt(0f)) && StdWidth(c.HalfWidthAt(c.length)), n + " end widths " + c.HalfWidthAt(0f) + " / " + c.HalfWidthAt(c.length));
                    Assert.IsTrue(c.endHalfWidth == 0f || StdWidth(c.endHalfWidth), n + " endHalfWidth");
                    Assert.AreEqual(4f, c.sidewalkWidth, 1e-4f, n); Assert.AreEqual(.2f, c.sidewalkHeight, 1e-4f, n); Assert.AreEqual(.3f, c.curbTransitionWidth, 1e-4f, n);
                    for (float z = 0f; z < c.length; z += .5f)
                        Assert.Less(Mathf.Abs(c.HalfWidthAt(z + .5f) - c.HalfWidthAt(z)), .15f, n + ": width changes smoothly at z " + z);
                    float perForty = c.endHeight * 40f / c.length;
                    Assert.That(perForty, Is.InRange(-8f, 0f), n + ": slope per 40 m");
                    Assert.AreEqual(Vector3.zero, c.transform.localPosition, n); Assert.AreEqual(Vector3.one, c.transform.localScale, n);
                }
                // The district meets any other district with the standard street at both ends; inside, every seam matches.
                Assert.AreEqual(4f, d.entry.HalfWidthAt(0f), 1e-4f, d.name + ": entry starts as the street");
                Assert.AreEqual(4f, d.exit.HalfWidthAt(d.exit.length), 1e-4f, d.name + ": exit ends as the street");
                float inner = d.entry.HalfWidthAt(d.entry.length);
                foreach (RoadChunk c in d.chunks)
                {
                    Assert.AreEqual(inner, c.HalfWidthAt(0f), 1e-4f, c.name + " start meets the district's road");
                    Assert.AreEqual(inner, c.HalfWidthAt(c.length), 1e-4f, c.name + " end meets the district's road");
                }
                Assert.AreEqual(inner, d.exit.HalfWidthAt(0f), 1e-4f, d.name + ": exit starts like the district's road");
            }
            District mb = Districts().First(d => d.id == "mountsbay");
            Assert.AreEqual(8f, mb.chunks[0].halfWidth, 1e-4f, "Mounts Bay Rd is a 4-lane divided road");
            Assert.IsFalse(mb.allowBoulevard);
        }

        [Test]
        public void ChunksStayInBudget_AndUseOnlyTheTwoComicMaterials()
        {
            var palette = AssetDatabase.LoadAssetAtPath<Material>(Palette);
            var ink = AssetDatabase.LoadAssetAtPath<Material>(Ink);
            Assert.NotNull(palette); Assert.NotNull(ink);
            foreach (District d in Districts())
            {
                foreach (RoadChunk c in AllChunks(d))
                {
                    var renderers = c.GetComponentsInChildren<Renderer>(true);
                    Assert.LessOrEqual(renderers.Length, 8, c.name + " renderers");
                    long tris = 0;
                    foreach (MeshFilter mf in c.GetComponentsInChildren<MeshFilter>(true))
                    {
                        Assert.NotNull(mf.sharedMesh, c.name + "/" + mf.name);
                        tris += mf.sharedMesh.GetIndexCount(0) / 3;
                    }
                    Assert.LessOrEqual(tris, 25000, c.name + " triangles incl. outlines");
                    Assert.IsEmpty(c.GetComponentsInChildren<SkinnedMeshRenderer>(true), c.name);
                }
                var things = AllChunks(d).Select(c => c.gameObject).Concat(d.pedestrians).Concat(d.obstacles.Select(o => o.gameObject))
                    .Concat(d.ramps.Select(r => r.gameObject)).Concat(new[] { d.farScenery });
                foreach (GameObject g in things)
                    foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                        foreach (Material m in r.sharedMaterials)
                            Assert.IsTrue(m == palette || m == ink, g.name + "/" + r.name + " uses " + (m != null ? m.name : "null"));
            }
        }

        [Test]
        public void SlotsSitOnTheRoad_AndKeepAFreeLane_AwayFromRampsAndGaps()
        {
            foreach (District d in Districts())
                foreach (RoadChunk c in AllChunks(d))
                {
                    Ramp[] ramps = c.GetComponentsInChildren<Ramp>(true);
                    RoadGap[] gaps = c.GetComponentsInChildren<RoadGap>(true);
                    foreach (var row in c.GetSlots().GroupBy(s => Mathf.RoundToInt(s.transform.localPosition.z)))
                    {
                        Assert.GreaterOrEqual(row.Count(), 2, c.name + " row " + row.Key + ": at least two slots so one lane stays free");
                        foreach (SpawnSlot s in row)
                        {
                            Vector3 p = s.transform.localPosition;
                            Assert.AreEqual(c.HeightAt(p.z), p.y, 1e-3f, c.name + "/" + s.name + " on the road surface");
                            Assert.LessOrEqual(Mathf.Abs(p.x), c.HalfWidthAt(p.z) - 1f + 1e-3f, c.name + "/" + s.name + " in a lane, well inside the kerbs");
                            foreach (Ramp r in ramps)
                            {
                                Vector3 rp = r.transform.localPosition;
                                bool inside = Mathf.Abs(p.x - rp.x) < r.width * .5f + .6f && p.z > rp.z - 3f && p.z < rp.z + r.Length + 1f;
                                Assert.IsFalse(inside, c.name + "/" + s.name + " is on or just before " + r.name);
                            }
                            foreach (RoadGap g in gaps)
                            {
                                Vector3 gp = g.transform.localPosition;
                                bool inside = (g.width <= 0f || Mathf.Abs(p.x - gp.x) < g.width * .5f + .6f) && p.z > gp.z - 1f && p.z < gp.z + g.length + 1f;
                                Assert.IsFalse(inside, c.name + "/" + s.name + " is in " + g.name);
                            }
                        }
                    }
                }
        }

        static void CheckRampNumbers(Ramp r, string n)
        {
            Assert.That(r.width, Is.InRange(1.5f, 24.01f), n + " width");
            Assert.That(r.rise, Is.InRange(2f, 14f), n + " rise");
            Assert.That(r.height, Is.InRange(.5f, 3.5f), n + " height");
            Assert.That(r.height / r.rise, Is.InRange(.1f, .45f), n + " slope");
            Assert.That(r.top, Is.InRange(0f, 20f), n + " top");
            Assert.That(r.launch, Is.InRange(.5f, 1.5f), n + " launch");
            Assert.That(r.hangGravity, Is.InRange(.3f, 1f), n + " hang");
        }

        [Test]
        public void RampsAreSensible_AndEveryRoadWideGapHasARampRightBeforeIt()
        {
            int gapsSeen = 0;
            foreach (District d in Districts())
            {
                foreach (Ramp r in d.ramps)
                {
                    CheckRampNumbers(r, r.name);
                    Assert.AreEqual(Vector3.zero, r.transform.localPosition, r.name + " pivot at the start of the run-up");
                    Assert.IsEmpty(r.GetComponentsInChildren<Collider>(true), r.name + ": riding into a ramp's side is not a crash");
                }
                foreach (RoadChunk c in AllChunks(d))
                {
                    Ramp[] ramps = c.GetComponentsInChildren<Ramp>(true);
                    foreach (Ramp r in ramps)
                    {
                        Vector3 p = r.transform.localPosition;
                        string n = c.name + "/" + r.name;
                        CheckRampNumbers(r, n);
                        Assert.AreEqual(c.HeightAt(p.z), p.y, 1e-3f, n + " pivot on the road");
                        Assert.LessOrEqual(p.z + r.Length, c.length, n + " inside the chunk");
                        Assert.LessOrEqual(Mathf.Abs(p.x) + r.width * .5f, c.RideableHalfWidthAt(p.z) + .01f, n + " on the rideable width");
                    }
                    foreach (RoadGap g in c.GetComponentsInChildren<RoadGap>(true))
                    {
                        gapsSeen++;
                        Vector3 p = g.transform.localPosition;
                        string n = c.name + "/" + g.name;
                        Assert.AreEqual(c.HeightAt(p.z), p.y, 1e-3f, n + " pivot on the road");
                        Assert.LessOrEqual(p.z + g.length, c.length - 5f, n + ": road to land on after the gap, in the same chunk");
                        Assert.That(g.length, Is.InRange(3f, 8f), n + " clearable length");
                        Assert.IsFalse(string.IsNullOrEmpty(g.word), n);
                        bool roadWide = g.width <= 0f;
                        float gx0 = roadWide ? -c.RideableHalfWidthAt(p.z) : p.x - g.width * .5f, gx1 = roadWide ? c.RideableHalfWidthAt(p.z) : p.x + g.width * .5f;
                        bool covered = ramps.Any(r =>
                        {
                            Vector3 rp = r.transform.localPosition;
                            float lip = rp.z + r.Length;
                            return lip <= p.z + .01f && lip >= p.z - 12f && rp.x - r.width * .5f <= gx0 + .01f && rp.x + r.width * .5f >= gx1 - .01f;
                        });
                        Assert.IsTrue(covered, n + ": a ramp across the gap's whole width ends within 12 m before it");
                    }
                }
            }
            Assert.GreaterOrEqual(gapsSeen, 2, "the walkway gully and the inlet");
        }

        [Test]
        public void AnimalsAreAnimals_PeopleAreNotPunchable()
        {
            foreach (District d in Districts())
                foreach (GameObject g in d.pedestrians.Concat(d.obstacles.Select(o => o.gameObject)).Distinct())
                {
                    var o = g.GetComponent<Obstacle>();
                    Assert.NotNull(o, g.name);
                    Assert.IsNotEmpty(g.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger), g.name + " has a solid collider");
                    if (o.animal)
                    {
                        Assert.AreEqual(ObstacleKind.Person, o.kind, g.name + ": animals are kind Person");
                        Assert.IsFalse(o.punchable, g.name + ": a punch only shoos animals");
                        Assert.IsFalse(o.police, g.name);
                    }
                    if (o.kind == ObstacleKind.Person) Assert.IsFalse(o.punchable, g.name + ": people are never punchable");
                    if (g.GetComponent<Pedestrian>() != null) Assert.NotNull(g.GetComponent<Pedestrian>().art, g.name + " art for the walking bob");
                }
        }
    }
}
