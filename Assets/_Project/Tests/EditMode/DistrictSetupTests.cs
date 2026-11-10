using System.Collections.Generic;
using System.Linq;
using CityScooter.Gameplay;
using CityScooter.Protocol;
using CityScooter.Rendering;
using CityScooter.Track;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CityScooter.Tests
{
    /// <summary>v0.3 districts: route order (DistrictRoute, used by DistrictSetup and the track generator), the theme as
    /// DistrictSetup wrote it, West Perth = the street as it looked before districts, and zone:&lt;id&gt;.</summary>
    public class DistrictSetupTests
    {
        const string ThemePath = "Assets/_Project/Data/Theme_ComicDownhill.asset";
        const string DataFolder = "Assets/_Project/Data/Districts";
        readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void Cleanup()
        {
            foreach (Object o in _made) if (o != null) Object.DestroyImmediate(o);
            _made.Clear();
        }

        District D(string id, int order)
        {
            var d = ScriptableObject.CreateInstance<District>();
            d.id = id;
            d.order = order;
            _made.Add(d);
            return d;
        }

        [Test]
        public void Route_SortsByOrder_ThenId_SkipsEmptyAndRepeated()
        {
            District cbd = D("cbd", 50), uwa = D("uwa", 10), wp = D("westperth", 40), kp = D("kingspark", 30), also30 = D("aaa", 30);
            District[] route = DistrictRoute.Sort(new[] { cbd, null, uwa, wp, kp, uwa, also30 });
            CollectionAssert.AreEqual(new[] { "uwa", "aaa", "kingspark", "westperth", "cbd" }, route.Select(d => d.id).ToArray());
            Assert.AreEqual(3, DistrictRoute.IndexOf(route, "WestPerth "));
            Assert.AreEqual(-1, DistrictRoute.IndexOf(route, "quay"));
            Assert.AreEqual(-1, DistrictRoute.IndexOf(route, null));
            Assert.IsEmpty(DistrictRoute.Sort(null));
        }

        // v0.5: the route is a trunk with lines. After the last trunk district the ride takes one of the lines (the
        // pick comes from the ride's seed), follows it to its end, then the trunk starts again.
        [Test]
        public void Route_AfterTheTrunk_TakesOneLine_ThenStartsAgain()
        {
            District uwa = D("uwa", 10), cbd = D("cbd", 50), quay = D("quay", 60), warnbro = D("warnbro", 150),
                mciver = D("mciver", 210), midland = D("midland", 340);
            quay.line = warnbro.line = "south";
            mciver.line = midland.line = "east";
            District[] route = DistrictRoute.Sort(new[] { midland, quay, uwa, mciver, cbd, warnbro });
            CollectionAssert.AreEqual(new[] { "uwa", "cbd", "quay", "warnbro", "mciver", "midland" }, route.Select(d => d.id).ToArray());
            CollectionAssert.AreEqual(new[] { 2, 4 }, DistrictRoute.Branches(route));
            Assert.AreEqual(1, DistrictRoute.Next(route, 0, 0.9), "inside the trunk: the next one");
            Assert.AreEqual(2, DistrictRoute.Next(route, 1, 0.0), "after the CBD: south ...");
            Assert.AreEqual(2, DistrictRoute.Next(route, 1, 0.49));
            Assert.AreEqual(4, DistrictRoute.Next(route, 1, 0.5), "... or east, by the pick");
            Assert.AreEqual(4, DistrictRoute.Next(route, 1, 1.0));
            Assert.AreEqual(3, DistrictRoute.Next(route, 2, 0.9), "inside a line: the next one");
            Assert.AreEqual(0, DistrictRoute.Next(route, 3, 0.9), "the south line ends: back to UWA, not on to the east line");
            Assert.AreEqual(5, DistrictRoute.Next(route, 4, 0.0));
            Assert.AreEqual(0, DistrictRoute.Next(route, 5, 0.0), "the east line ends: back to UWA");
            // No lines: the old loop.
            District[] loop = DistrictRoute.Sort(new[] { D("a", 1), D("b", 2), D("c", 3) });
            Assert.IsEmpty(DistrictRoute.Branches(loop));
            Assert.AreEqual(1, DistrictRoute.Next(loop, 0, 0.7));
            Assert.AreEqual(0, DistrictRoute.Next(loop, 2, 0.7));
        }

        [Test]
        public void SouthAndEastLineIds_AreOnTheirLines()
        {
            Assert.AreEqual("", DistrictRoute.LineOf("cbd"));
            Assert.AreEqual("south", DistrictRoute.LineOf("quay"));
            Assert.AreEqual("south", DistrictRoute.LineOf("warnbro"));
            Assert.AreEqual("east", DistrictRoute.LineOf("mciver"));
            Assert.AreEqual("east", DistrictRoute.LineOf("midland"));
            foreach (string id in DistrictRoute.EastLine)
                Assert.LessOrEqual(("go:99999:" + id + ":5000").Length, 29, id + " fits in one radio message");
        }

        [Test]
        public void ZoneMessage_IsLowerCaseLetters_AndAGameMessage()
        {
            Assert.AreEqual("zone:kingspark", Messages.Zone("kingspark"));
            Assert.AreEqual("zone:kingspark", Messages.Zone("Kings Park!"));
            Assert.AreEqual("zone:", Messages.Zone(null));
            Assert.IsTrue(Messages.IsGameMessage("zone:uwa"), "a board cannot make the game say zone:uwa");
            Assert.IsFalse(Messages.IsSayWord("zone:uwa"));
            CollectionAssert.IsSubsetOf(new[] { "uwa", "matilda", "kingspark", "westperth", "cbd", "quay", "mountsbay" }, Messages.ZoneIds);
        }

        // The protocol's id list (blocks, web monitor, docs copy it) is exactly the theme's route, in route order.
        [Test]
        public void ZoneIds_AreTheThemesRoute_InOrder()
        {
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ThemePath);
            Assert.IsNotNull(theme);
            string[] route = System.Array.ConvertAll(DistrictRoute.Sort(theme.districts), d => d.id);
            CollectionAssert.AreEqual(route, Messages.ZoneIds, "run DistrictSetup, and keep Messages.ZoneIds, game-cityscooter.js DISTRICTS and shell.js ZONE_NAMES in step");
        }

        [Test]
        public void Theme_ListsEveryUsableDistrict_InRouteOrder()
        {
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ThemePath);
            Assert.IsNotNull(theme);
            Assert.IsNotNull(theme.districts, "DistrictSetup.ApplyBatch has written the districts");
            Assert.IsTrue(theme.HasDistricts, "every district in the theme has its chunks");
            for (int i = 1; i < theme.districts.Length; i++)
                Assert.LessOrEqual(theme.districts[i - 1].order, theme.districts[i].order, "route order");
            CollectionAssert.AreEqual(DistrictRoute.Sort(theme.districts), theme.districts);
            var ids = new HashSet<string>();
            foreach (District d in theme.districts)
            {
                Assert.IsTrue(ids.Add(d.id), "id used once: " + d.id);
                StringAssert.IsMatch("^[a-z]+$", d.id, "zone:<id> is lower case letters");
                Assert.IsNotNull(d.gateway, d.id + " has a gateway (its own or DistrictSetup's)");
                Assert.IsEmpty(d.gateway.GetComponentsInChildren<Collider>(true), d.id + " gateway is decoration");
            }
            // Every usable District asset under Data/Districts is on the route (DistrictSetup was run after the art).
            foreach (string guid in AssetDatabase.FindAssets("t:District", new[] { DataFolder }))
            {
                var d = AssetDatabase.LoadAssetAtPath<District>(AssetDatabase.GUIDToAssetPath(guid));
                if (d == null || !d.HasChunks) continue;
                CollectionAssert.Contains(theme.districts, d, d.name + " is in the theme: run CityScooter/Districts/Apply districts to theme");
            }
        }

        [Test]
        public void WestPerth_IsTheComicStreet_WithTodaysLook()
        {
            var theme = AssetDatabase.LoadAssetAtPath<TrackTheme>(ThemePath);
            var wp = AssetDatabase.LoadAssetAtPath<District>(DataFolder + "/District_WestPerth.asset");
            var look = AssetDatabase.LoadAssetAtPath<SceneLook>("Assets/_Project/Art/ComicCity/Look_ComicCity.asset");
            Assert.IsNotNull(wp);
            Assert.IsNotNull(look);
            Assert.AreEqual("westperth", wp.id);
            Assert.AreEqual("WEST PERTH", wp.displayName);
            Assert.AreEqual(40, wp.order);
            Assert.IsTrue(wp.allowBoulevard);
            Assert.IsTrue(wp.orderedChunks);
            CollectionAssert.AreEqual(theme.chunks, wp.chunks, "the street chunks, in order");
            Assert.IsEmpty(wp.obstacles ?? new Obstacle[0], "the theme's own obstacles");
            Assert.IsEmpty(wp.hazards ?? new HazardPattern[0], "the theme's own hazard patterns");
            Assert.AreEqual(look.fogColor, wp.fogColor);
            Assert.AreEqual(look.ambientSky, wp.ambientSky);
            Assert.AreEqual(look.ambientEquator, wp.ambientEquator);
            Assert.AreEqual(look.ambientGround, wp.ambientGround);
            Assert.AreEqual(look.sunColor, wp.sunColor);
            Assert.AreEqual(look.sunIntensity, wp.sunIntensity);
            Assert.AreEqual(Color.white, wp.skyTint, "the sky as it is");
            CollectionAssert.Contains(theme.districts, wp);
        }
    }
}
