using System.Collections.Generic;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CityScooter.Tests
{
    /// <summary>v0.2 hazard patterns: always a free straight lane, traffic on the correct side, comic shaders only.</summary>
    public sealed class HazardPatternTests
    {
        const string ThemePath = "Assets/_Project/Data/Theme_ComicDownhill.asset";
        const float RiderRadius = 0.35f, Margin = 0.15f, RoadEdge = 4f;

        static TrackTheme Theme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ThemePath);
            Assert.NotNull(theme, ThemePath);
            return theme;
        }

        [Test]
        public void ThemeHasHazardPatternsAndComicGifts()
        {
            TrackTheme theme = Theme();
            Assert.That(theme.hazards, Is.Not.Null.And.Not.Empty);
            Assert.That(theme.gifts, Is.Not.Null.And.Not.Empty);
            foreach (Gift g in theme.gifts) AssertBends(g.gameObject);
        }

        [Test]
        public void EveryPatternKeepsAStraightLaneFree()
        {
            foreach (HazardPattern p in Theme().hazards)
            {
                Assert.NotNull(p);
                foreach (bool mirror in p.allowMirror ? new[] { false, true } : new[] { false })
                {
                    var blocked = new List<Vector2>();
                    foreach (HazardPattern.Entry e in p.entries)
                    {
                        GameObject prefab = mirror && e.mirrorPrefab != null ? e.mirrorPrefab : e.prefab;
                        Assert.NotNull(prefab, p.name + " has an empty entry");
                        var o = prefab.GetComponent<Obstacle>();
                        if (o == null)
                        {
                            Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true), prefab.name + ": decoration (no Obstacle) must have no colliders");
                            continue;
                        }
                        if (o.canJumpOver) continue; // jump it
                        var ped = prefab.GetComponent<Pedestrian>();
                        if (ped != null && ped.crosses) continue; // moves across; dodge it or ring the bell (PlayMode test)
                        Vector2 span = Span(prefab, mirror ? -e.x : e.x, mirror ? -e.yaw : e.yaw);
                        blocked.Add(new Vector2(span.x - RiderRadius - Margin, span.y + RiderRadius + Margin));
                    }
                    Assert.IsTrue(HasGap(blocked, 0.3f), p.name + (mirror ? " (mirrored)" : "") + " leaves no straight lane on the road");
                }
            }
        }

        [Test]
        public void PeopleAndVehiclesCannotBePunched()
        {
            foreach (HazardPattern p in Theme().hazards)
                foreach (HazardPattern.Entry e in p.entries)
                {
                    var o = e.prefab.GetComponent<Obstacle>();
                    if (o != null && (o.kind == ObstacleKind.Person || o.kind == ObstacleKind.Vehicle))
                        Assert.IsFalse(o.punchable, e.prefab.name + " (teacher decision, docs/v0.2-plan.md section 2)");
                }
            Assert.IsTrue(System.Array.Exists(Theme().obstacles, o => o.punchable), "some street props can be punched out of the way");
            Assert.NotNull(Theme().punchPopup);
            Assert.NotNull(Theme().bellPopup);
        }

        [Test]
        public void TrafficDrivesOnTheLeft()
        {
            foreach (HazardPattern p in Theme().hazards)
                foreach (HazardPattern.Entry e in p.entries)
                {
                    var m = e.prefab.GetComponent<Mover>();
                    if (m == null) continue;
                    Assert.IsFalse(p.allowMirror, p.name + ": patterns with moving cars must not be mirrored");
                    if (m.fractionOfRiderSpeed > 0f) Assert.Less(e.x, 0f, p.name + ": slower cars ahead use the left lane");
                    else
                    {
                        Assert.Less(m.speed, 0f, e.prefab.name);
                        Assert.Greater(e.x, 0f, p.name + ": oncoming cars use the right lane");
                        Assert.AreEqual(180f, e.yaw, p.name + ": oncoming cars face the rider");
                    }
                }
        }

        [Test]
        public void HazardPrefabsFollowTheCityBendAndFakeTurns()
        {
            var seen = new HashSet<GameObject>();
            foreach (HazardPattern p in Theme().hazards)
                foreach (HazardPattern.Entry e in p.entries)
                {
                    if (seen.Add(e.prefab)) AssertBends(e.prefab);
                    if (e.mirrorPrefab != null && seen.Add(e.mirrorPrefab)) AssertBends(e.mirrorPrefab);
                }
            foreach (Obstacle o in Theme().obstacles) AssertBends(o.gameObject);
        }

        static void AssertBends(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, prefab.name);
            foreach (Renderer r in renderers)
                foreach (Material m in r.sharedMaterials)
                {
                    Assert.NotNull(m, prefab.name + "/" + r.name + " has no material");
                    string shader = m.shader != null ? m.shader.name : "";
                    Assert.IsTrue(shader == "CityScooter/Comic Vertex" || shader == "CityScooter/Comic Outline",
                        prefab.name + "/" + r.name + " uses " + shader + ": it would not follow the city bend or fake turns");
                }
        }

        static Vector2 Span(GameObject prefab, float x, float yaw)
        {
            float min = float.MaxValue, max = float.MinValue;
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            foreach (BoxCollider b in prefab.GetComponentsInChildren<BoxCollider>(true))
            {
                if (b.GetComponentInParent<Obstacle>(true) == null) continue; // prefab assets are not active in a hierarchy
                for (int i = 0; i < 4; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.size * 0.5f, new Vector3((i & 1) == 0 ? -1 : 1, 0, (i & 2) == 0 ? -1 : 1));
                    float cx = x + (q * Vector3.Scale(b.transform.lossyScale, corner + b.transform.localPosition)).x;
                    min = Mathf.Min(min, cx); max = Mathf.Max(max, cx);
                }
            }
            Assert.Less(min, max, prefab.name + " has no BoxCollider");
            return new Vector2(min, max);
        }

        // Is some x on the road (|x| <= 4 - radius) outside every blocked interval, with a little width to spare?
        static bool HasGap(List<Vector2> blocked, float width)
        {
            float lo = -RoadEdge + RiderRadius, hi = RoadEdge - RiderRadius;
            blocked.Sort((a, b) => a.x.CompareTo(b.x));
            float cursor = lo;
            foreach (Vector2 b in blocked)
            {
                if (b.x - cursor >= width) return true;
                cursor = Mathf.Max(cursor, b.y);
            }
            return hi - cursor >= width;
        }
    }
}
