using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityScooter.Devices;
using CityScooter.Game;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CityScooter.Tests
{
    /// <summary>
    /// v0.3 districts with test districts made at runtime (no art needed): ALPHA (order 10, 200 m, exit chunk) and
    /// BRAVO (order 20, 160 m, entry and exit chunks, its own obstacles, gateway and far scenery). The theme lists
    /// them out of order on purpose. Layout from z 0: a1 a2 a1 a2 xA | eB b1 b1 xB | a1 a2 ... (boundaries 200, 360).
    /// </summary>
    public class DistrictTests
    {
        readonly List<Object> _made = new List<Object>();
        GameTuning _tuning;
        TrackTheme _theme;
        GameController _game;
        District _a, _b;
        Light _sun;
        Light _oldSun;
        Color _oldFog, _oldSky;
        readonly List<string> _zones = new List<string>();
        readonly List<string> _sent = new List<string>();

        static readonly Color Baseline = new Color(0.3f, 0.6f, 0.3f, 1f);

        T Keep<T>(T o) where T : Object { _made.Add(o); return o; }

        GameObject Template(string name)
        {
            var go = Keep(new GameObject(name));
            go.transform.position = new Vector3(0f, -500f, 0f);
            return go;
        }

        RoadChunk Chunk(string name, float halfWidth = 4f, float endHalfWidth = 0f)
        {
            GameObject go = Template(name);
            var c = go.AddComponent<RoadChunk>();
            c.length = 40f; c.endHeight = -3.1471f; c.halfWidth = halfWidth; c.endHalfWidth = endHalfWidth;
            c.sidewalkWidth = 4f; c.sidewalkHeight = .2f;
            foreach (float z in new[] { 10f, 25f })
                foreach (float x in new[] { -2.6f, 0f, 2.6f })
                {
                    var slot = new GameObject("Slot").AddComponent<SpawnSlot>();
                    slot.kind = SpawnKind.Obstacle;
                    slot.transform.SetParent(go.transform, false);
                    slot.transform.localPosition = new Vector3(x, 0f, z);
                }
            return c;
        }

        Obstacle ObstaclePrefab(string name)
        {
            GameObject go = Template(name);
            go.AddComponent<BoxCollider>();
            return go.AddComponent<Obstacle>();
        }

        District MakeDistrict(string id, int order, float length, RoadChunk[] chunks, RoadChunk entry, RoadChunk exit, Color fog, float sun)
        {
            var d = Keep(ScriptableObject.CreateInstance<District>());
            d.id = id; d.displayName = id.ToUpperInvariant(); d.order = order; d.length = new Vector2(length, length);
            d.chunks = chunks; d.orderedChunks = true; d.entry = entry; d.exit = exit;
            d.fogColor = fog; d.ambientSky = fog; d.ambientEquator = fog; d.ambientGround = fog; d.sunColor = fog; d.sunIntensity = sun;
            d.blendMeters = 100f;
            return d;
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            DeviceHub.Instance?.Mixer.Reset();
            _oldFog = RenderSettings.fogColor;
            _oldSky = RenderSettings.ambientSkyColor;
            _oldSun = RenderSettings.sun;
            RenderSettings.fogColor = Baseline;
            RenderSettings.ambientSkyColor = Baseline;
            _sun = Keep(new GameObject("Test sun")).AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.intensity = 0.7f;
            RenderSettings.sun = _sun;

            _tuning = Keep(ScriptableObject.CreateInstance<GameTuning>());
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = 0f;
            _tuning.giftChance = _tuning.jumpObstacleChance = 0f;
            _tuning.hazardChanceStart = _tuning.hazardChanceMax = 0f;
            _tuning.sidewalkPeople = Vector2.zero;
            _tuning.safeStartDistance = 0f;
            _tuning.fakeTurns = false;
            _tuning.boulevardSections = false;
            _tuning.startDistrict = "alpha";

            _theme = Keep(ScriptableObject.CreateInstance<TrackTheme>());
            _theme.chunks = new[] { Chunk("S1"), Chunk("S2") };
            _theme.orderedChunks = true;
            _theme.obstacles = new[] { ObstaclePrefab("OBS_Theme") };
            _a = MakeDistrict("alpha", 10, 200f, new[] { Chunk("A1"), Chunk("A2") }, null, Chunk("XA"), Color.red, 1f);
            _b = MakeDistrict("bravo", 20, 160f, new[] { Chunk("B1") }, Chunk("EB"), Chunk("XB"), Color.blue, 2f);
            _b.obstacles = new[] { ObstaclePrefab("OBS_B") };
            _b.gateway = Template("GW_Test");
            _b.farScenery = Template("FAR_B");
            _theme.districts = new[] { _b, _a }; // out of order: the route sorts them

            _zones.Clear();
            _sent.Clear();
            GameEvents.ZoneEntered += OnZone;
            if (DeviceHub.Instance != null) DeviceHub.Instance.LineSent += OnSent;
            _game = GameSystemsFactory.Create(_tuning, _theme).GetComponent<GameController>();
            _game.track.seed = 42;
            Keep(_game.gameObject);
            yield return null; // GameController.Start adds the DistrictDirector
            _game.EnterReady();
            yield return null;
        }

        void OnZone(string id) => _zones.Add(id);
        void OnSent(string target, string line) { if (line.StartsWith("zone:")) _sent.Add(line); }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            GameEvents.ZoneEntered -= OnZone;
            if (DeviceHub.Instance != null) DeviceHub.Instance.LineSent -= OnSent;
            for (int i = _made.Count - 1; i >= 0; i--) if (_made[i] != null) Object.Destroy(_made[i]);
            _made.Clear();
            yield return null;
            RenderSettings.fogColor = _oldFog;
            RenderSettings.ambientSkyColor = _oldSky;
            RenderSettings.sun = _oldSun;
            DeviceHub.Instance?.Mixer.Reset();
        }

        TrackGenerator Track => _game.track;
        DistrictDirector Director => _game.GetComponent<DistrictDirector>();

        IEnumerator RiderTo(float z, int frames = 2)
        {
            _game.rider.transform.position = new Vector3(0f, Track.SurfaceHeightAt(z), z);
            for (int i = 0; i < frames; i++) yield return null;
        }

        // Moves the idle rider along and records every chunk laid (start z -> prefab name).
        IEnumerator March(float until, SortedDictionary<int, string> laid)
        {
            for (float z = 0f; z <= until; z += 40f)
            {
                yield return RiderTo(z, 1);
                for (float s = z - 20f; s < z + Track.buildAhead; s += 10f)
                {
                    RoadChunk c = Track.ChunkAt(s, out float start);
                    if (c != null && s >= start && s < start + c.length) laid[Mathf.RoundToInt(start)] = c.name.Replace("(Clone)", "");
                }
            }
        }

        static string Name(RoadChunk c) => c != null ? c.name.Replace("(Clone)", "") : "null";

        [UnityTest]
        public IEnumerator StartsInTheStartDistrict_ElseTheFirstOnTheRoute()
        {
            Assert.IsTrue(Track.DistrictsOn);
            Assert.AreSame(_a, Track.Route[0], "route order by District.order");
            Assert.AreSame(_b, Track.Route[1]);
            Assert.AreSame(_a, Track.StartDistrict);
            Assert.AreSame(_a, Track.DistrictAt(0f));
            Assert.AreEqual("A1", Name(Track.ChunkAt(1f, out _)));

            _tuning.startDistrict = "bravo";
            _game.EnterReady();
            yield return null;
            Assert.AreSame(_b, Track.StartDistrict);
            Assert.AreSame(_b, Track.CurrentDistrict);
            Assert.AreEqual("B1", Name(Track.ChunkAt(1f, out _)), "a ride starts inside its district, not at the entry");
            Assert.IsFalse(Track.GetComponentsInChildren<Transform>().Any(t => t.name.StartsWith("GW_Test") && t.gameObject.activeInHierarchy && t.position.z < 10f),
                "no gateway at the start of a ride");

            _tuning.startDistrict = "nowhere";
            _game.EnterReady();
            yield return null;
            Assert.AreSame(_a, Track.StartDistrict, "unknown id: the first district on the route");
        }

        [UnityTest]
        public IEnumerator Boundaries_AtTheDistrictLength_ExitThenEntry_InOrder_AndLoop()
        {
            var laid = new SortedDictionary<int, string>();
            yield return March(640f, laid);
            string Got(int z) => laid.TryGetValue(z, out string n) ? n : "nothing";
            string[] expect = { "A1", "A2", "A1", "A2", "XA", "EB", "B1", "B1", "XB", "A1", "A2" };
            for (int i = 0; i < expect.Length; i++) Assert.AreEqual(expect[i], Got(i * 40), "chunk at z " + (i * 40) + " (" + string.Join(",", laid.Select(kv => kv.Key + ":" + kv.Value)) + ")");
            Assert.GreaterOrEqual(Track.DistrictSpans.Count, 3);
            Assert.AreEqual(200f, Track.DistrictSpans[1].StartZ, .01f, "ALPHA lasts 200 m (its exit chunk included)");
            Assert.AreEqual(360f, Track.DistrictSpans[2].StartZ, .01f, "BRAVO lasts 160 m from its entry to the end of its exit");
            Assert.AreSame(_a, Track.DistrictAt(199f));
            Assert.AreSame(_b, Track.DistrictAt(200f));
            Assert.AreSame(_b, Track.DistrictAt(359f));
            Assert.AreSame(_a, Track.DistrictAt(360f), "the route loops");
            Assert.AreEqual(560f, Track.NextBoundaryAfter(370f), .01f, "ALPHA again for 200 m");
        }

        [UnityTest]
        public IEnumerator Gateway_StandsAtTheStartOfTheEntryChunk()
        {
            yield return RiderTo(150f);
            Assert.AreEqual(200f, Track.NextBoundaryZ, .01f);
            Assert.AreSame(_b, Track.DistrictForChunkAt(250f));
            Assert.AreSame(_a, Track.DistrictForChunkAt(170f), "an exit chunk belongs to its own district");
            Transform gate = Track.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.StartsWith("GW_Test") && t.gameObject.activeInHierarchy);
            Assert.IsNotNull(gate, "BRAVO's gateway is placed");
            Assert.AreEqual(200.5f, gate.position.z, .01f);
            Assert.AreEqual("EB", Name(gate.parent.GetComponent<RoadChunk>()), "on BRAVO's entry chunk");
        }

        [UnityTest]
        public IEnumerator Obstacles_ComeFromTheChunksDistrict()
        {
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = 1f;
            _game.EnterReady();
            yield return RiderTo(120f);
            int own = 0, theme = 0;
            foreach (float z in new[] { 90f, 130f, 170f, 210f, 250f, 290f, 330f })
            {
                RoadChunk c = Track.ChunkAt(z, out float start);
                bool inB = start >= 199f && start < 359f;
                foreach (Obstacle o in c.GetComponentsInChildren<Obstacle>())
                {
                    string n = o.name.Replace("(Clone)", "");
                    Assert.AreEqual(inB ? "OBS_B" : "OBS_Theme", n, "obstacle on " + c.name + " at z " + start);
                    if (inB) own++; else theme++;
                }
            }
            Assert.Greater(own, 0, "BRAVO's own obstacles");
            Assert.Greater(theme, 0, "ALPHA has none of its own: the theme's");
        }

        [UnityTest]
        public IEnumerator SidewalkLife_ComesFromTheDistrict_AnimalsKeepTheirOwnPace()
        {
            GameObject peacock = Template("PED_Peacock");
            Obstacle o = peacock.AddComponent<Obstacle>();
            o.kind = ObstacleKind.Person;
            o.animal = true;
            var box = peacock.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, .5f, 0f);
            box.size = new Vector3(.5f, 1f, .5f);
            peacock.AddComponent<Pedestrian>().walkSpeed = .4f;
            _b.pedestrians = new[] { peacock };
            _tuning.sidewalkPeople = new Vector2(3f, 3f);
            _tuning.sidewalkPeopleWalking = 1f;
            _tuning.maxPeople = 40;
            _game.EnterReady();
            yield return RiderTo(150f);
            Pedestrian[] birds = Track.GetComponentsInChildren<Pedestrian>().Where(p => p.name.StartsWith("PED_Peacock")).ToArray();
            Assert.IsNotEmpty(birds, "BRAVO's own sidewalk life");
            foreach (Pedestrian p in birds)
            {
                Assert.AreSame(_b, Track.DistrictAt(p.transform.position.z), "only in BRAVO (ALPHA and the theme have nobody)");
                Assert.AreEqual(.4f, Mathf.Abs(p.walkSpeed), .001f, "a peacock keeps its own pace");
            }
        }

        [UnityTest]
        public IEnumerator Boulevards_OnlyInDistrictsThatAllowThem_NeverAcrossABoundary()
        {
            _theme.boulevardIn = Chunk("BIn", 4f, 8f);
            _theme.boulevardChunks = new[] { Chunk("BWide", 8f) };
            _theme.boulevardOut = Chunk("BOut", 8f, 4f);
            _a.allowBoulevard = true;
            _a.length = new Vector2(400f, 400f);
            _b.allowBoulevard = false;
            _tuning.boulevardSections = true;
            _tuning.boulevardMinDistance = 0f;
            _tuning.boulevardChance = 1f;
            _tuning.boulevardGap = 0f;
            _tuning.boulevardChunks = new Vector2Int(1, 2);
            _game.EnterReady();
            var laid = new SortedDictionary<int, string>();
            yield return March(1800f, laid);
            int sections = 0;
            foreach (KeyValuePair<int, string> kv in laid)
            {
                if (!kv.Value.StartsWith("BIn") && !kv.Value.StartsWith("BWide") && !kv.Value.StartsWith("BOut")) continue;
                if (kv.Value == "BIn") sections++;
                Assert.AreSame(_a, Track.DistrictAt(kv.Key), kv.Value + " at z " + kv.Key + " is in ALPHA");
                Assert.AreSame(_a, Track.DistrictAt(kv.Key + 39.9f), kv.Value + " at z " + kv.Key + " ends inside ALPHA");
                Assert.AreSame(_a, Track.DistrictAt(kv.Key + 40f + 39.9f), "the exit chunk still follows inside ALPHA after " + kv.Value);
            }
            Assert.Greater(sections, 1, "boulevard sections in ALPHA (" + string.Join(",", laid.Select(kv => kv.Key + ":" + kv.Value)) + ")");
        }

        [UnityTest]
        public IEnumerator ZoneMessage_AtTheStart_AndOncePerCrossing()
        {
            _game.StartRun();
            yield return null;
            CollectionAssert.AreEqual(new[] { "alpha" }, _zones, "zone at the start of a ride");
            yield return RiderTo(190f, 3);
            Assert.AreEqual(1, _zones.Count, "not before the boundary");
            yield return RiderTo(203f, 3);
            yield return RiderTo(210f, 3);
            CollectionAssert.AreEqual(new[] { "alpha", "bravo" }, _zones, "once per crossing");
            yield return RiderTo(362f, 3);
            CollectionAssert.AreEqual(new[] { "alpha", "bravo", "alpha" }, _zones, "the route loops");
            if (DeviceHub.Instance != null)
                CollectionAssert.AreEqual(new[] { "zone:alpha", "zone:bravo", "zone:alpha" }, _sent, "the boards get zone:<id>");
            Text say = _game.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name == "Say");
            Assert.IsNotNull(say);
            yield return RiderTo(205f, 1); // back again (a reset-like jump): no new zone
            Assert.AreEqual(3, _zones.Count);

            _game.StartRun();
            yield return null;
            Assert.AreEqual("alpha", _zones.Last(), "a new ride sends its start district again");
            Assert.AreEqual(4, _zones.Count);
        }

        [UnityTest]
        public IEnumerator Caption_EnteringTheDistrict()
        {
            _game.StartRun();
            yield return RiderTo(205f, 3);
            Text say = _game.GetComponentsInChildren<Text>(true).First(t => t.name == "Say");
            Assert.AreEqual("ENTERING BRAVO", say.text);
        }

        [UnityTest]
        public IEnumerator Look_BlendsOverBlendMeters_HalfwayAtTheBoundary()
        {
            yield return RiderTo(149f);
            Assert.That(RenderSettings.fogColor, Is.EqualTo(Color.red).Using(ColorEqualityComparer.Instance), "pure ALPHA before the blend");
            Assert.AreEqual(1f, _sun.intensity, .001f);
            yield return RiderTo(200f);
            Assert.AreEqual(.5f, Director.LookBlend, .001f, "half way at the boundary");
            Assert.AreEqual(.5f, RenderSettings.fogColor.r, .01f);
            Assert.AreEqual(.5f, RenderSettings.fogColor.b, .01f);
            Assert.AreEqual(.5f, RenderSettings.ambientSkyColor.b, .01f);
            Assert.AreEqual(1.5f, _sun.intensity, .01f);
            yield return RiderTo(175f);
            Assert.That(Director.LookBlend, Is.InRange(.1f, .2f), "smoothstep (0.156 a quarter of the way in)");
            yield return RiderTo(251f);
            Assert.That(RenderSettings.fogColor, Is.EqualTo(Color.blue).Using(ColorEqualityComparer.Instance), "pure BRAVO after the blend");
            Assert.AreEqual(2f, _sun.intensity, .001f);
        }

        [UnityTest]
        public IEnumerator Reset_GoesBackToTheStartLook_AndDisableRestoresTheScene()
        {
            yield return RiderTo(300f);
            Assert.That(RenderSettings.fogColor, Is.EqualTo(Color.blue).Using(ColorEqualityComparer.Instance));
            _game.EnterReady();
            yield return null;
            yield return null;
            Assert.That(RenderSettings.fogColor, Is.EqualTo(Color.red).Using(ColorEqualityComparer.Instance), "reset: the start district's look");
            Director.enabled = false;
            Assert.That(RenderSettings.fogColor, Is.EqualTo(Baseline).Using(ColorEqualityComparer.Instance), "disabled: the scene's own look");
            Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(Baseline).Using(ColorEqualityComparer.Instance));
            Assert.AreEqual(.7f, _sun.intensity, .001f);
            Assert.IsFalse(Director.LookApplied);
        }

        [UnityTest]
        public IEnumerator FarScenery_FollowsTheRider_InItsDistrict_AndSinksAtTheBoundary()
        {
            yield return RiderTo(100f);
            GameObject far = Director.SceneryOf(_b);
            Assert.IsTrue(far == null || !far.activeSelf, "not in ALPHA");
            yield return RiderTo(300f);
            far = Director.SceneryOf(_b);
            Assert.IsNotNull(far);
            Assert.IsTrue(far.activeSelf);
            Assert.AreEqual(300f, far.transform.position.z, .01f);
            Assert.AreEqual(Track.SurfaceHeightAt(300f), far.transform.position.y, .01f);
            yield return RiderTo(200f);
            Assert.AreEqual(Track.SurfaceHeightAt(200f) - .5f * Director.scenerySink, far.transform.position.y, .05f, "half sunk at the boundary");
        }

        [UnityTest]
        public IEnumerator FakeTurn_StartsBeforeTheBoundary()
        {
            _tuning.fakeTurns = true;
            var turns = _game.GetComponent<FakeTurnDirector>();
            Assert.IsNotNull(turns);
            _game.StartRun(); // turns restart, then the track plans the first boundary (200) and asks for a turn
            yield return null;
            Assert.IsTrue(turns.Planned.Any(t => t.Start >= 200f - 80.5f && t.Start <= 200f - 39.5f && t.End > 200f),
                "a turn from 40-80 m before the boundary past it: " + string.Join(", ", turns.Planned.Select(t => t.Start + ".." + t.End)));
        }

        [UnityTest]
        public IEnumerator DistrictsOff_IsTheThemesOwnStreet_NoZone_NoLook()
        {
            _tuning.districts = false;
            _game.EnterReady();
            yield return RiderTo(10f, 3);
            Assert.IsFalse(Track.DistrictsOn);
            Assert.IsNull(Track.DistrictAt(0f));
            Assert.IsNull(Track.CurrentDistrict);
            Assert.AreEqual(float.PositiveInfinity, Track.NextBoundaryZ);
            var laid = new SortedDictionary<int, string>();
            yield return March(400f, laid);
            for (int i = 0; i < 10; i++) Assert.AreEqual(i % 2 == 0 ? "S1" : "S2", laid[i * 40], "theme chunks at z " + (i * 40));
            _game.StartRun();
            yield return null;
            Assert.IsEmpty(_zones);
            Assert.IsFalse(Director.LookApplied);
            Assert.That(RenderSettings.fogColor, Is.EqualTo(Baseline).Using(ColorEqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator StartScreen_ShowsTheStartDistrict_AndNumberKeysChooseIt()
        {
            yield return null;
            Assert.AreEqual("Start: ALPHA  (keys 1-2)", _game.hud.StartLineText);
            Assert.IsTrue(Director.SelectStartDistrict(1));
            yield return null;
            Assert.AreSame(_b, Track.StartDistrict);
            Assert.AreEqual("B1", Name(Track.ChunkAt(1f, out _)));
            Assert.AreEqual("Start: BRAVO  (keys 1-2)", _game.hud.StartLineText);
            Assert.IsFalse(Director.SelectStartDistrict(5));
            _game.StartRun();
            yield return null;
            Assert.AreEqual("bravo", _zones.Last());
            Assert.AreEqual("", _game.hud.StartLineText, "only on the start screen");
        }

        [UnityTest]
        public IEnumerator StartScreen_KeyboardOff_NoKeysMentioned()
        {
            DeviceHub hub = DeviceHub.Instance;
            if (hub == null) Assert.Ignore("no DeviceHub (Bootstrap)");
            hub.SetKeyboardEnabled("0"); // the class build: micro:bit only
            try
            {
                yield return null;
                Assert.AreEqual("Start: ALPHA", _game.hud.StartLineText);
            }
            finally { hub.SetKeyboardEnabled("1"); }
            yield return null;
            Assert.AreEqual("Start: ALPHA  (keys 1-2)", _game.hud.StartLineText);
        }

    }

    sealed class ColorEqualityComparer : IEqualityComparer<Color>
    {
        public static readonly ColorEqualityComparer Instance = new ColorEqualityComparer();
        public bool Equals(Color a, Color b) => Mathf.Abs(a.r - b.r) < .002f && Mathf.Abs(a.g - b.g) < .002f && Mathf.Abs(a.b - b.b) < .002f;
        public int GetHashCode(Color c) => 0;
    }

    /// <summary>West Perth in the real Main scene looks exactly as the street did before districts (Look_ComicCity).</summary>
    public class DistrictMainSceneTests
    {
        UnityEngine.SceneManagement.Scene _main;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_main.IsValid() && _main.isLoaded) yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(_main);
            DeviceHub.Instance?.Mixer.Reset();
        }

        [UnityTest]
        public IEnumerator WestPerth_KeepsTheScenesOwnLook()
        {
            DeviceHub.Instance?.Mixer.Reset();
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Main", UnityEngine.SceneManagement.LoadSceneMode.Additive);
            _main = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Main");
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(_main);
            yield return null;
            GameController game = GameController.Instance;
            Assert.IsNotNull(game);
            var lookRig = Object.FindAnyObjectByType<CityScooter.Rendering.ApplySceneLook>();
            Assert.IsNotNull(lookRig);
            CityScooter.Rendering.SceneLook look = lookRig.look;
            Material sky = RenderSettings.skybox;
            Assert.IsTrue(game.track.DistrictsOn, "the comic theme has its districts (DistrictSetup)");
            game.track.startDistrictOverride = "westperth";
            game.EnterReady();
            yield return null;
            yield return null;
            var director = game.GetComponent<DistrictDirector>();
            Assert.IsNotNull(director, "GameController adds the DistrictDirector at runtime");
            Assert.AreEqual("westperth", game.track.CurrentDistrict.id);
            Assert.IsTrue(director.LookApplied);
            Assert.AreEqual("westperth", director.LookFrom.id);
            Assert.AreEqual("westperth", director.LookTo.id);
            Assert.That(RenderSettings.fogColor, Is.EqualTo(look.fogColor).Using(ColorEqualityComparer.Instance));
            Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(look.ambientSky).Using(ColorEqualityComparer.Instance));
            Assert.That(RenderSettings.ambientEquatorColor, Is.EqualTo(look.ambientEquator).Using(ColorEqualityComparer.Instance));
            Assert.That(RenderSettings.ambientGroundColor, Is.EqualTo(look.ambientGround).Using(ColorEqualityComparer.Instance));
            Assert.That(RenderSettings.sun.color, Is.EqualTo(look.sunColor).Using(ColorEqualityComparer.Instance));
            Assert.AreEqual(look.sunIntensity, RenderSettings.sun.intensity, .001f);
            Assert.AreSame(sky, RenderSettings.skybox, "the sky material is untouched (no tint)");
            director.enabled = false;
            Assert.That(RenderSettings.fogColor, Is.EqualTo(look.fogColor).Using(ColorEqualityComparer.Instance));
            director.enabled = true;
        }

        /// <summary>Not part of the normal run: screenshots of a boundary between West Perth and a made-up warm-light
        /// district (gateway, look blend, sky tint) in %TEMP%/cityscooter-sysA/shots.</summary>
        [UnityTest, Explicit]
        public IEnumerator BoundaryScreens()
        {
            DeviceHub.Instance?.Mixer.Reset();
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Main", UnityEngine.SceneManagement.LoadSceneMode.Additive);
            _main = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Main");
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(_main);
            yield return null;
            GameController game = GameController.Instance;
            TrackTheme theme = Object.Instantiate(game.track.theme);
            District wp = System.Array.Find(theme.districts, d => d.id == "westperth");
            District dusk = Object.Instantiate(wp);
            dusk.id = "dusk"; dusk.displayName = "DUSK TEST"; dusk.order = 41; dusk.length = new Vector2(300f, 300f);
            dusk.fogColor = new Color(.95f, .62f, .42f); dusk.sunColor = new Color(1f, .7f, .45f); dusk.sunIntensity = .8f;
            dusk.ambientSky = new Color(.8f, .6f, .55f); dusk.skyTint = new Color(1f, .75f, .55f);
            wp = Object.Instantiate(wp);
            wp.length = new Vector2(300f, 300f);
            theme.districts = new[] { wp, dusk };
            GameTuning tuning = Object.Instantiate(game.tuning);
            tuning.obstacleChanceStart = tuning.obstacleChanceMax = tuning.hazardChanceStart = tuning.hazardChanceMax = 0f;
            tuning.startDistrict = "westperth";
            tuning.fakeTurns = false;
            game.tuning = game.rider.tuning = game.track.tuning = tuning;
            game.track.theme = theme;
            game.EnterReady();
            yield return null;
            float b = game.track.NextBoundaryZ;
            Assert.Less(b, 400f);
            string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cityscooter-sysA", "shots");
            System.IO.Directory.CreateDirectory(folder);
            foreach (float before in new[] { 160f, 90f, 20f, -40f, -120f })
            {
                float z = b - before;
                game.rider.ResetRider(new Vector3(0f, game.track.SurfaceHeightAt(z), z));
                Camera.main.GetComponent<FollowCamera>()?.SnapToTarget();
                for (int i = 0; i < 4; i++) yield return null;
                Shot(Camera.main, System.IO.Path.Combine(folder, "boundary_" + (before >= 0 ? "minus" : "plus") + Mathf.Abs(before) + ".png"));
            }
        }

        static void Shot(Camera camera, string path)
        {
            var rt = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            RenderTexture previous = RenderTexture.active, oldTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = rt;
                var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = rt };
                for (int pass = 0; pass < 2; pass++)
                    if (UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(camera, request)) UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request); else camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
                Object.Destroy(image);
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = oldTarget;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
