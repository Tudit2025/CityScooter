using System.Collections.Generic;
using System.Linq;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CityScooter.Tests
{
    /// <summary>Boulevard sections (BoulevardBuilder): chunk contract and seams, 4-lane slots, comic shaders, the 25k
    /// triangle budget, and 4-lane hazard patterns that keep a straight lane and drive on the left.</summary>
    public sealed class BoulevardChunkTests
    {
        const string ThemePath = "Assets/_Project/Data/Theme_ComicDownhill.asset";
        const float Grade = 0.07867673f, RiderRadius = 0.35f, Margin = 0.15f;

        static TrackTheme Theme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ThemePath);
            Assert.NotNull(theme, ThemePath);
            Assert.IsTrue(theme.HasBoulevard, "the theme has In, boulevard and Out chunks (run BoulevardBuilder)");
            return theme;
        }

        static IEnumerable<RoadChunk> Chunks(TrackTheme t) => new[] { t.boulevardIn }.Concat(t.boulevardChunks).Concat(new[] { t.boulevardOut });

        [Test]
        public void ChunksKeepTheStreetContract_AndWidthsMeetAtEverySeam()
        {
            TrackTheme t = Theme();
            Assert.That(t.boulevardChunks.Length, Is.GreaterThanOrEqualTo(2), "two boulevard variants");
            foreach (RoadChunk c in Chunks(t))
            {
                Assert.AreEqual(40f, c.length, c.name);
                Assert.AreEqual(-40f * Grade, c.endHeight, 1e-4f, c.name + ": same slope as the street (-3.1470692)");
                Assert.AreEqual(4f, c.sidewalkWidth, c.name);
                Assert.AreEqual(.2f, c.sidewalkHeight, 1e-4f, c.name);
                Assert.AreEqual(RoadType.Street, c.roadType, c.name);
            }
            Assert.IsTrue(t.boulevardIn.ChangesWidth && t.boulevardOut.ChangesWidth);
            foreach (RoadChunk c in t.boulevardChunks) { Assert.IsFalse(c.ChangesWidth, c.name); Assert.AreEqual(8f, c.halfWidth, c.name); }
            RoadChunk street = t.chunks[0];
            Assert.AreEqual(street.halfWidth, t.boulevardIn.HalfWidthAt(0f), 1e-4f, "In starts like the street");
            Assert.AreEqual(8f, t.boulevardIn.HalfWidthAt(40f), 1e-4f);
            Assert.AreEqual(8f, t.boulevardOut.HalfWidthAt(0f), 1e-4f);
            Assert.AreEqual(street.halfWidth, t.boulevardOut.HalfWidthAt(40f), 1e-4f, "Out ends like the street");
            // The width only changes smoothly: never more than 0.3 m per metre of road.
            foreach (RoadChunk c in new[] { t.boulevardIn, t.boulevardOut })
                for (float z = 0f; z < 40f; z += .5f)
                    Assert.Less(Mathf.Abs(c.HalfWidthAt(z + .5f) - c.HalfWidthAt(z)), .15f, c.name + " at z " + z);
            // Legacy chunks keep their behaviour: one width for the whole chunk.
            foreach (RoadChunk c in t.chunks) { Assert.IsFalse(c.ChangesWidth, c.name); Assert.AreEqual(c.RideableHalfWidth, c.RideableHalfWidthAt(30f), 1e-5f); }
        }

        [Test]
        public void BoulevardHasFourLanesOfSpawnSlots_TransitionsStayOnTheRoad()
        {
            TrackTheme t = Theme();
            foreach (RoadChunk c in t.boulevardChunks)
            {
                var rows = c.GetSlots().GroupBy(s => Mathf.RoundToInt(s.transform.localPosition.z)).OrderBy(g => g.Key).ToList();
                CollectionAssert.AreEqual(new[] { 10, 25 }, rows.Select(r => r.Key).ToArray(), c.name);
                foreach (var row in rows)
                {
                    CollectionAssert.AreEqual(new[] { -6f, -2f, 2f, 6f }, row.Select(s => s.transform.localPosition.x).OrderBy(x => x).ToArray(), c.name + " row " + row.Key);
                    foreach (SpawnSlot s in row)
                    {
                        Assert.AreEqual(SpawnKind.Any, s.kind);
                        Assert.AreEqual(c.HeightAt(row.Key), s.transform.localPosition.y, 1e-3f, "slots sit on the road surface");
                    }
                }
            }
            foreach (RoadChunk c in new[] { t.boulevardIn, t.boulevardOut })
            {
                Assert.That(c.GetSlots(), Is.Not.Empty, c.name);
                foreach (SpawnSlot s in c.GetSlots())
                {
                    Vector3 p = s.transform.localPosition;
                    Assert.LessOrEqual(Mathf.Abs(p.x), c.HalfWidthAt(p.z) - 1f, c.name + "/" + s.name + " must stay well inside the kerbs");
                }
            }
        }

        [Test]
        public void EveryBoulevardRendererUsesTheComicShaders_AndMeshesCoverTheCityCurl()
        {
            foreach (RoadChunk c in Chunks(Theme()))
            {
                var renderers = c.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.Length, Is.InRange(4, 8), c.name + ": road, frontage + ink, furniture + ink, deep city");
                foreach (Renderer r in renderers)
                    foreach (Material m in r.sharedMaterials)
                    {
                        Assert.NotNull(m, c.name + "/" + r.name);
                        string shader = m.shader != null ? m.shader.name : "";
                        Assert.IsTrue(shader == "CityScooter/Comic Vertex" || shader == "CityScooter/Comic Outline",
                            c.name + "/" + r.name + " uses " + shader + ": it would not follow the city bend or fake turns");
                    }
                foreach (MeshFilter f in c.GetComponentsInChildren<MeshFilter>(true))
                {
                    Assert.NotNull(f.sharedMesh, c.name + "/" + f.name);
                    // Culling happens before the vertex shader lifts far chunks: bounds are enlarged like the street's.
                    Assert.Greater(f.sharedMesh.bounds.max.y, 40f, c.name + "/" + f.name + " bounds are not enlarged for the city curl");
                }
                var dressing = c.GetComponent<StreetDressing>();
                if (dressing != null)
                {
                    Assert.That(dressing.layouts, Is.Not.Empty);
                    Assert.AreEqual(dressing.layouts.Length, dressing.collisionLayouts.Length);
                    foreach (Mesh m in dressing.layouts) Assert.Greater(m.bounds.max.y, 40f, m.name);
                }
            }
        }

        [Test]
        public void ChunksStayWithinTheTriangleBudget()
        {
            foreach (RoadChunk c in Chunks(Theme()))
            {
                long tris = 0;
                foreach (MeshFilter f in c.GetComponentsInChildren<MeshFilter>(true)) tris += f.sharedMesh.GetIndexCount(0) / 3;
                var dressing = c.GetComponent<StreetDressing>();
                if (dressing != null && dressing.layouts.Length > 0)
                    tris += 2 * (dressing.layouts.Max(m => (long)m.GetIndexCount(0) / 3) - (long)dressing.layouts[0].GetIndexCount(0) / 3);
                Assert.LessOrEqual(tris, 25000, c.name + " exceeds the 25k triangle chunk budget (ComicCityBuilder)");
                TestContext.WriteLine(c.name + ": " + tris + " triangles");
            }
        }

        [Test]
        public void BoulevardPatterns_KeepAStraightLane_DriveOnTheLeft_AndSkipZebras()
        {
            TrackTheme t = Theme();
            Assert.That(t.boulevardHazards, Is.Not.Null.And.Not.Empty);
            Assert.IsTrue(t.boulevardHazards.Any(p => p.roadHalfWidth >= 8f), "some patterns are made for 4 lanes");
            foreach (HazardPattern p in t.boulevardHazards)
            {
                Assert.NotNull(p);
                foreach (HazardPattern.Entry e in p.entries)
                {
                    Assert.NotNull(e.prefab, p.name);
                    Assert.IsFalse(e.prefab.name.Contains("Zebra"), p.name + ": an 8 m zebra does not fit a 16 m road");
                    var ped = e.prefab.GetComponent<Pedestrian>();
                    Assert.IsFalse(ped != null && ped.crosses, p.name + ": no crossing patterns on the boulevard");
                    var mover = e.prefab.GetComponent<Mover>();
                    if (mover == null || p.roadHalfWidth < 8f) continue;
                    Assert.IsFalse(p.allowMirror, p.name + ": traffic is never mirrored");
                    if (mover.fractionOfRiderSpeed > 0f) Assert.Less(e.x, 0f, p.name + ": slower cars use the left lanes");
                    else
                    {
                        Assert.Greater(e.x, 0f, p.name + ": oncoming cars use the right lanes");
                        Assert.AreEqual(180f, e.yaw, p.name);
                    }
                    Assert.That(Mathf.Abs(e.x), Is.EqualTo(2f).Or.EqualTo(6f), p.name + ": cars drive in a lane (x = +-2 or +-6)");
                }
                foreach (bool mirror in p.allowMirror ? new[] { false, true } : new[] { false })
                {
                    var blocked = new List<Vector2>();
                    foreach (HazardPattern.Entry e in p.entries)
                    {
                        GameObject prefab = mirror && e.mirrorPrefab != null ? e.mirrorPrefab : e.prefab;
                        var o = prefab.GetComponent<Obstacle>();
                        if (o == null || o.canJumpOver) continue;
                        float x = mirror ? -e.x : e.x;
                        // Street patterns on the boulevard: sidewalk entries move out with the kerb (TrackGenerator).
                        if (Mathf.Abs(x) > p.roadHalfWidth + .01f) x += Mathf.Sign(x) * (8f - p.roadHalfWidth);
                        Vector2 span = Span(prefab, x, mirror ? -e.yaw : e.yaw);
                        blocked.Add(new Vector2(span.x - RiderRadius - Margin, span.y + RiderRadius + Margin));
                    }
                    Assert.IsTrue(HasGap(blocked, 8f, .3f), p.name + (mirror ? " (mirrored)" : "") + " leaves no straight lane on the boulevard");
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
                    Vector3 corner = b.center + Vector3.Scale(b.size * 0.5f, new Vector3((i & 1) == 0 ? -1 : 1, 0, (i & 2) == 0 ? -1 : 1));
                    float cx = x + (q * Vector3.Scale(b.transform.lossyScale, corner + b.transform.localPosition)).x;
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
