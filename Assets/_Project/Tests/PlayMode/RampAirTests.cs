using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityScooter.Devices;
using CityScooter.Game;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>v0.3 ramps and air in the real Main scene: kicker flights (air time, distance), road gaps (SPLASH!),
    /// tricks (points, combo, SKETCHY!, spam), air:/land: messages, do:ramp, the generator's ramp placement, air gifts,
    /// clearing obstacles below a flight, the camera rising, reset.</summary>
    public class RampAirTests
    {
        Scene _main;
        GameController _game;
        GameTuning _tuning;
        readonly List<string> _sent = new List<string>();
        readonly List<GameObject> _made = new List<GameObject>();

        void OnSent(string target, string line) { if (line.StartsWith("air:") || line.StartsWith("land:") || line == "crash") _sent.Add(line); }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            DeviceHub.Instance?.Mixer.Reset();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
            _main = SceneManager.GetSceneByName("Main");
            SceneManager.SetActiveScene(_main);
            yield return null;
            _game = GameController.Instance;
            Assert.NotNull(_game);
            Assert.IsNotEmpty(Theme.ramps, "Theme_ComicDownhill has generic ramps (RampKitBuilder)");
            _tuning = Object.Instantiate(_game.tuning);
            _tuning.fakeTurns = false;
            _tuning.hazardChanceStart = _tuning.hazardChanceMax = 0f;
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = 0f;
            _tuning.jumpObstacleChance = 0f;
            _tuning.giftChance = 0f;
            _tuning.boulevardSections = false;
            _tuning.rampChance = 0f; // tests place their own ramps
            _tuning.startSpeed = _tuning.maxSpeed = 15f;
            _game.tuning = _game.rider.tuning = _game.track.tuning = _tuning;
            var turns = _game.GetComponent<FakeTurnDirector>();
            if (turns != null) turns.tuning = _tuning;
            _sent.Clear();
            if (DeviceHub.Instance != null) DeviceHub.Instance.LineSent += OnSent;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (DeviceHub.Instance != null) DeviceHub.Instance.LineSent -= OnSent;
            foreach (GameObject go in _made) if (go != null) Object.Destroy(go);
            _made.Clear();
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            if (_tuning != null) Object.Destroy(_tuning);
            DeviceHub.Instance?.Mixer.Reset();
        }

        TrackTheme Theme => _game.track.theme;
        RiderController Rider => _game.rider;
        RiderActions Actions => _game.GetComponent<RiderActions>();
        Ramp Kicker => Theme.ramps.First(r => r.name == "RMP_Kicker");
        Ramp Platform => Theme.ramps.First(r => r.name == "RMP_Platform");

        IEnumerator Ride()
        {
            _game.StartRun();
            yield return null;
            _sent.Clear();
        }

        Ramp KickerAhead(float metres, float x = 0f)
        {
            Ramp r = _game.track.PlaceRampAt(Kicker, Rider.transform.position.z + metres, x);
            Assert.NotNull(r, "ramp placed on the built road");
            return r;
        }

        RoadGap Gap(float z, float length, string word = "SPLASH", bool water = true)
        {
            var go = new GameObject("TestGap");
            _made.Add(go);
            go.transform.position = new Vector3(0f, _game.track.SurfaceHeightAt(0f, z), z);
            var gap = go.AddComponent<RoadGap>();
            gap.length = length; gap.width = 0f; gap.word = word; gap.water = water;
            return gap;
        }

        GameObject Wall(float z, Vector3 size, bool jumpable = false)
        {
            var go = new GameObject("TestCar");
            _made.Add(go);
            go.transform.position = new Vector3(0f, _game.track.SurfaceHeightAt(0f, z), z);
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, size.y * 0.5f, 0f);
            box.size = size;
            var o = go.AddComponent<Obstacle>();
            o.kind = ObstacleKind.Vehicle;
            o.canJumpOver = jumpable;
            Physics.SyncTransforms();
            return go;
        }

        IEnumerator Until(System.Func<bool> condition, float seconds, string what)
        {
            float t = 0f;
            while (!condition() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(condition(), what);
        }

        // ------------------------------------------------------------------ flights

        [UnityTest]
        public IEnumerator Kicker_LaunchesTheRider_HangsInTheAir_AndLands_SendingAirAndLandOnce()
        {
            yield return Ride();
            yield return new WaitForSeconds(0.4f); // jumps are locked for a moment after the start
            // A normal jump on the flat road is short: no air: / land: messages.
            DeviceHub.Instance.Receive("test", "jp");
            yield return Until(() => Rider.Airborne, 0.5f, "normal jump");
            Assert.IsFalse(Rider.BigAir, "a normal jump is not big air");
            yield return Until(() => !Rider.Airborne, 1.5f, "normal jump lands");
            Assert.IsEmpty(_sent, "no air:1 for a 0.65 s jump");

            Ramp ramp = KickerAhead(25f);
            float maxHeight = 0f, cameraLift = 0f;
            yield return Until(() => Rider.Airborne, 4f, "flies off the kicker");
            Assert.IsTrue(Rider.BigAir);
            Assert.Greater(Rider.transform.position.z, ramp.LipZ - 0.6f, "takes off at the lip, not on the face");
            var follow = Camera.main.GetComponent<FollowCamera>();
            while (Rider.Airborne && Rider.AirTime < 4f)
            {
                maxHeight = Mathf.Max(maxHeight, Rider.HeightAboveRoad);
                cameraLift = Mathf.Max(cameraLift, follow.AirLift);
                yield return null;
            }
            Assert.IsFalse(Rider.Airborne, "lands");
            Assert.AreEqual(RunState.Running, _game.State, "a clean landing on the road");
            Assert.That(Rider.LastAirTime, Is.InRange(1.5f, 2.2f), "hang time at 15 m/s");
            Assert.That(Rider.LastAirDistance, Is.InRange(24f, 36f), "metres flown at 15 m/s");
            Assert.Greater(maxHeight, 3f, "high in the air");
            Assert.Greater(cameraLift, 0.6f, "the camera rises with the flight");
            CollectionAssert.AreEqual(new[] { "air:1", "air:0", "land:ok" }, _sent, "each message once, in order");
            yield return new WaitForSeconds(1.5f);
            Assert.Less(follow.AirLift, 0.25f, "the camera settles after landing");
        }

        [UnityTest]
        public IEnumerator RidingUpTheFace_LiftsTheRider_NoCrash()
        {
            yield return Ride();
            Ramp ramp = KickerAhead(15f);
            float start = ramp.transform.position.z;
            yield return Until(() => Rider.transform.position.z > start + ramp.rise * 0.5f, 3f, "on the ramp");
            Assert.IsFalse(Rider.Airborne);
            Assert.AreSame(ramp, Rider.OnRamp);
            Assert.AreEqual(ramp.height * 0.5f, Rider.HeightAboveRoad, 0.25f, "half way up the slope");
            Assert.AreEqual(RunState.Running, _game.State);
        }

        [UnityTest]
        public IEnumerator Platform_JumpOnTheTop_Works_AndFliesOff()
        {
            yield return Ride();
            Ramp ramp = _game.track.PlaceRampAt(Platform, Rider.transform.position.z + 15f, 0f);
            float topStart = ramp.transform.position.z + ramp.rise + 0.5f;
            yield return Until(() => Rider.transform.position.z > topStart, 3f, "on the platform top");
            Assert.AreEqual(ramp.height, Rider.HeightAboveRoad, 0.15f);
            DeviceHub.Instance.Receive("test", "jp");
            yield return Until(() => Rider.Airborne, 0.5f, "jump on the top");
            Assert.IsTrue(Rider.BigAir, "a jump from a platform floats like a ramp flight");
            yield return Until(() => !Rider.Airborne, 4f, "lands");
            Assert.AreEqual(RunState.Running, _game.State);
            Assert.AreEqual(0f, Rider.HeightAboveRoad, 0.01f, "back on the road");
        }

        [UnityTest]
        public IEnumerator RampThenGap_FlyingOver_Survives()
        {
            yield return Ride();
            Ramp ramp = KickerAhead(20f);
            RoadGap gap = Gap(ramp.LipZ + 2f, 14f);
            yield return Until(() => Rider.transform.position.z > gap.EndZ + 6f || _game.State != RunState.Running, 6f, "past the gap");
            Assert.AreEqual(RunState.Running, _game.State, "flew over the water");
            Assert.IsFalse(Rider.Falling);
        }

        [UnityTest]
        public IEnumerator GapWithoutRamp_FallsIn_SplashEndsTheRunAsACrash()
        {
            bool crashed = false;
            System.Action onCrash = () => crashed = true;
            GameEvents.Crashed += onCrash;
            try
            {
                yield return Ride();
                RoadGap gap = Gap(Rider.transform.position.z + 15f, 8f);
                yield return Until(() => Rider.Falling, 3f, "rides into the gap and falls");
                Assert.AreSame(gap, Rider.FallingInto);
                yield return new WaitForSeconds(0.4f);
                Assert.Less(Rider.transform.position.y, _game.track.SurfaceHeightAt(Rider.transform.position.z) - 0.5f, "drops below the road");
                Assert.LessOrEqual(Rider.transform.position.z, gap.EndZ, "stays in the gap");
                yield return Until(() => _game.State == RunState.Crashed, 2f, "the ride ends");
                Assert.AreEqual("SPLASH!", _game.EndTitle);
                Assert.IsTrue(crashed, "a crash (\"crash\" to the boards)");
                Assert.Contains("crash", _sent);
            }
            finally { GameEvents.Crashed -= onCrash; }
        }

        [UnityTest]
        public IEnumerator GapThatIsADrop_SaysWhoops()
        {
            yield return Ride();
            Gap(Rider.transform.position.z + 12f, 6f, "whoops", false);
            yield return Until(() => _game.State == RunState.Crashed, 4f, "falls and the ride ends");
            Assert.AreEqual("WHOOPS!", _game.EndTitle);
        }

        [UnityTest]
        public IEnumerator LandingInsideAGap_Falls()
        {
            yield return Ride();
            Ramp ramp = KickerAhead(20f);
            // The landing point at 15 m/s is about 28 m past the lip: water there.
            Gap(ramp.LipZ + 18f, 22f);
            yield return Until(() => Rider.Falling || _game.State != RunState.Running, 6f, "comes down into the water");
            yield return Until(() => _game.State == RunState.Crashed, 2f, "the ride ends");
            Assert.AreEqual("SPLASH!", _game.EndTitle);
            Assert.AreEqual("air:1", _sent[0]);
            Assert.AreEqual("air:0", _sent[1], "air:0 before crash");
            Assert.IsFalse(_sent.Any(s => s.StartsWith("land:")), "no land: for a splash");
        }

        // Like the district chunks (art agent 1): an unrotated Ramp root on the downhill road surface, a gap right
        // after its lip. CHN_Matilda_Inlet: road-wide ramp (width 16, rise 6, height 1.6, top 2), road-wide 6 m gap.
        Ramp AuthoredRamp(float z, float x, float width, float rise, float height, float top)
        {
            var go = new GameObject("TestAuthoredRamp");
            _made.Add(go);
            go.transform.position = new Vector3(x, _game.track.SurfaceHeightAt(x, z), z);
            var r = go.AddComponent<Ramp>();
            r.width = width; r.rise = rise; r.height = height; r.top = top; r.launch = 1f; r.hangGravity = 0.5f;
            return r;
        }

        // Rides an authored-style jump (ramp, then a gap right after its lip) at a fixed speed; true if it was cleared.
        IEnumerator AuthoredJump(float speed, float x, float width, float rise, float height, float top, float gapLength, float gapWidth,
            string word, bool water, System.Action<bool, float> result)
        {
            _tuning.startSpeed = _tuning.maxSpeed = speed;
            yield return Ride();
            Vector3 p = Rider.transform.position;
            Rider.transform.position = new Vector3(x, p.y, p.z);
            float z = p.z + 12f;
            Ramp ramp = AuthoredRamp(z, x, width, rise, height, top);
            var go = new GameObject("TestAuthoredGap");
            _made.Add(go);
            go.transform.position = new Vector3(x, _game.track.SurfaceHeightAt(x, z + ramp.Length), z + ramp.Length);
            var gap = go.AddComponent<RoadGap>();
            gap.length = gapLength; gap.width = gapWidth; gap.word = word; gap.water = water;
            yield return Until(() => Rider.Airborne || Rider.Falling, 6f, "reaches the lip");
            Assert.Greater(Rider.transform.position.z, ramp.LipZ - 0.6f, "takes off from the lip, not the face");
            yield return Until(() => !Rider.Airborne || Rider.Falling, 4f, "comes down");
            result(!Rider.Falling, Rider.LastAirDistance);
        }

        [UnityTest]
        public IEnumerator AuthoredMatildaInlet_AtStartSpeed_ClearsTheGap()
        {
            bool cleared = false; float flown = 0f;
            yield return AuthoredJump(9f, 0f, 16f, 6f, 1.6f, 2f, 6f, 0f, "SPLASH", true, (c, d) => { cleared = c; flown = d; });
            Assert.IsTrue(cleared, "cleared the 6 m inlet at 9 m/s");
            Assert.Greater(flown, 6f + 3f, "with room to spare");
            Assert.AreEqual(RunState.Running, _game.State);
        }

        [UnityTest]
        public IEnumerator AuthoredKingsParkWalkway_InTheRightLane_AtStartSpeed_ClearsTheGap()
        {
            bool cleared = false;
            yield return AuthoredJump(9f, 2.4f, 3.2f, 10f, 3f, 14f, 6f, 3.2f, "WHOOPS", false, (c, d) => cleared = c);
            Assert.IsTrue(cleared, "flew off the 3 m walkway over the 6 m gully at 9 m/s");
            Assert.AreEqual(RunState.Running, _game.State);
        }

        [UnityTest]
        public IEnumerator AuthoredMountsBayInlet_ClearsAtStartSpeed_SplashesWhenBraking()
        {
            bool cleared = false;
            yield return AuthoredJump(9f, 0f, 16f, 6f, 1.4f, 0f, 5f, 0f, "SPLASH", true, (c, d) => cleared = c);
            Assert.IsTrue(cleared, "cleared the 5 m inlet at 9 m/s");
            foreach (GameObject go in _made) if (go != null) Object.Destroy(go);
            _made.Clear();
            yield return null;
            yield return AuthoredJump(4f, 0f, 16f, 6f, 1.4f, 0f, 5f, 0f, "SPLASH", true, (c, d) => cleared = c);
            Assert.IsFalse(cleared, "at 4 m/s (braking) the rider lands in the water");
            yield return Until(() => _game.State == RunState.Crashed, 2f, "SPLASH ends the ride");
            Assert.AreEqual("SPLASH!", _game.EndTitle);
        }

        // CHN_Quay_Bridge: a hump (rise 10, height 2.2, top 8) whose deck comes back down over 8 m. The way down is a
        // Ramp turned round (yaw 180, pivot at the bottom end): slow riders land on it and ride down, fast ones fly over.
        [UnityTest]
        public IEnumerator BridgeHump_Slow_LandsOnTheDeckComingDown_AndRidesItDown() => BridgeHump(6f);

        [UnityTest]
        public IEnumerator BridgeHump_Fast_FliesOverTheDeckComingDown() => BridgeHump(13f);

        IEnumerator BridgeHump(float speed)
        {
            _tuning.startSpeed = _tuning.maxSpeed = speed;
            yield return Ride();
            float z = Rider.transform.position.z + 12f;
            Ramp up = AuthoredRamp(z, 0f, 16f, 10f, 2.2f, 8f);
            Ramp down = AuthoredRamp(z + 26f, 0f, 16f, 8f, 2.2f, 0f);
            down.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            int takeOffs = 0;
            System.Action<bool> onTakeOff = _ => takeOffs++;
            Rider.TookOff += onTakeOff;
            try
            {
                float worst = 0f;
                while ((Rider.transform.position.z < z + 30f || Rider.Airborne) && Rider.transform.position.z < z + 60f && _game.State == RunState.Running)
                {
                    Vector3 p = Rider.transform.position;
                    float road = _game.track.SurfaceHeightAt(p.x, p.z);
                    float deck = Mathf.Max(up.WorldHeightAt(p.x, p.z, road, out _), down.WorldHeightAt(p.x, p.z, road, out _));
                    worst = Mathf.Max(worst, deck - (p.y - road));
                    yield return null;
                }
                Assert.AreEqual(RunState.Running, _game.State);
                Assert.Less(worst, 0.12f, "never inside the deck");
                Assert.AreEqual(1, takeOffs, "one flight off the crest, no hopping down the deck");
                Assert.IsFalse(Rider.Airborne);
                Assert.AreEqual(0f, Rider.HeightAboveRoad, 0.05f, "back on the road");
            }
            finally { Rider.TookOff -= onTakeOff; }
        }

        [UnityTest]
        public IEnumerator OffsetGap_OnlyCoversItsOwnLanes()
        {
            // CHN_Uwa_PondJump: the pond covers x 0..8 (gap at x 4, width 8); the left lane rides past it.
            yield return Ride();
            float z = Rider.transform.position.z + 15f;
            var go = new GameObject("TestPond");
            _made.Add(go);
            go.transform.position = new Vector3(4f, _game.track.SurfaceHeightAt(4f, z), z);
            var pond = go.AddComponent<RoadGap>();
            pond.length = 6f; pond.width = 8f;
            Vector3 p = Rider.transform.position;
            Rider.transform.position = new Vector3(-2.6f, p.y, p.z);
            yield return Until(() => Rider.transform.position.z > z + 8f || Rider.Falling, 3f, "past the pond");
            Assert.IsFalse(Rider.Falling, "the left lane is dry");
            _game.StartRun();
            yield return null;
            p = Rider.transform.position;
            Rider.transform.position = new Vector3(2.6f, p.y, p.z);
            yield return Until(() => Rider.Falling, 3f, "the right lane rides into the pond");
        }

        // ------------------------------------------------------------------ obstacles below a flight

        [UnityTest]
        public IEnumerator FlyingOverACar_IsFine_LandingOnOne_IsACrash()
        {
            yield return Ride();
            Ramp ramp = KickerAhead(20f);
            Wall(ramp.LipZ + 9f, new Vector3(2f, 1.6f, 4f)); // a car right under the climb
            yield return Until(() => Rider.Airborne, 4f, "takes off");
            yield return Until(() => !Rider.Airborne || _game.State != RunState.Running, 4f, "lands");
            Assert.AreEqual(RunState.Running, _game.State, "flew over the car (feet above its roof)");

            _game.StartRun();
            yield return null;
            ramp = KickerAhead(20f);
            Wall(ramp.LipZ + 27f, new Vector3(2.4f, 1.6f, 8f)); // a long car where the rider comes down
            yield return Until(() => _game.State != RunState.Running, 6f, "comes down on the car");
            Assert.AreEqual(RunState.Crashed, _game.State);
        }

        // ------------------------------------------------------------------ tricks

        [UnityTest]
        public IEnumerator TricksInTheAir_ScorePoints_ComboBonus_AndLandTrick()
        {
            _tuning.rampAirGifts = 0;
            yield return Ride();
            KickerAhead(20f);
            yield return Until(() => Rider.Airborne, 4f, "takes off");
            int before = _game.Score - Mathf.FloorToInt(Rider.Distance);
            Assert.IsTrue(Actions.Trick(), "a spin");
            Assert.IsTrue(Actions.Trick(), "a flip lined up after it");
            Assert.IsTrue(Actions.Tricking);
            yield return null;
            yield return null;
            Assert.AreNotEqual(Quaternion.identity, Rider.TrickPose, "the rider turns");
            yield return Until(() => !Rider.Airborne, 4f, "lands");
            Assert.AreEqual(RunState.Running, _game.State);
            Assert.AreEqual(2, Actions.Tricks, "spin and flip");
            Assert.AreEqual(0, Actions.SketchyLandings);
            int bonus = _game.Score - Mathf.FloorToInt(Rider.Distance) - before;
            Assert.AreEqual(2 * _tuning.trickPoints + 2 * _tuning.trickComboPointsPerTrick, bonus, 1, "trick points and the combo bonus");
            CollectionAssert.AreEqual(new[] { "air:1", "air:0", "land:trick" }, _sent);
            Assert.AreEqual(Quaternion.identity, Rider.TrickPose);
        }

        [UnityTest]
        public IEnumerator TrickFromTheBoard_UsesTheProtocol()
        {
            yield return Ride();
            KickerAhead(20f);
            yield return Until(() => Rider.Airborne, 4f, "takes off");
            DeviceHub.Instance.Receive("test", "trick");
            yield return Until(() => Actions.Tricks == 1, 1.5f, "\"trick\" from a board does a spin");
            yield return Until(() => !Rider.Airborne, 4f, "lands");
            Assert.Contains("land:trick", _sent);
        }

        [UnityTest]
        public IEnumerator LandingMidTrick_IsSketchy_NotACrash_NoPoints()
        {
            _tuning.rampAirGifts = 0;
            yield return Ride();
            KickerAhead(20f);
            yield return Until(() => Rider.Airborne, 4f, "takes off");
            yield return Until(() => Rider.AirTime > 1.55f, 3f, "late in the flight");
            int before = _game.Score - Mathf.FloorToInt(Rider.Distance);
            Assert.IsTrue(Actions.Trick());
            yield return Until(() => !Rider.Airborne, 2f, "lands mid-trick");
            Assert.AreEqual(RunState.Running, _game.State, "sketchy, not a crash");
            Assert.AreEqual(1, Actions.SketchyLandings);
            Assert.AreEqual(0, Actions.Tricks);
            Assert.AreEqual(before, _game.Score - Mathf.FloorToInt(Rider.Distance), "no points for it");
            Assert.AreEqual("land:ok", _sent.Last());
        }

        [UnityTest]
        public IEnumerator TrickOnTheGround_DoesNothing_AndSpamMakesTheRiderDizzy()
        {
            yield return Ride();
            int before = _game.Score;
            Assert.IsFalse(Actions.Trick(), "on the ground: nothing");
            Assert.IsFalse(Actions.Tricking);
            Assert.AreEqual(Quaternion.identity, Rider.TrickPose);
            Assert.AreEqual(1, Actions.IgnoredTricks);
            for (int i = 0; i < _tuning.trickSpamCount + 2; i++) Actions.Trick();
            Assert.IsTrue(Actions.Dizzy, "sending trick in a loop does not pay off");
            KickerAhead(15f);
            yield return Until(() => Rider.Airborne, 4f, "takes off");
            Assert.IsFalse(Actions.Trick(), "still dizzy in the air");
            Assert.LessOrEqual(_game.Score - before, Mathf.CeilToInt(Rider.Distance) + 1, "no trick points");
            Assert.IsEmpty(_sent.Where(s => s.StartsWith("land:trick")));
        }

        // ------------------------------------------------------------------ gifts, programs, generator, reset

        [UnityTest]
        public IEnumerator AirGifts_HangAlongTheFlight_AndAreCollectedFlying()
        {
            yield return Ride();
            Ramp ramp = KickerAhead(25f);
            Gift[] air = Object.FindObjectsByType<Gift>(FindObjectsSortMode.None)
                .Where(g => g.gameObject.activeInHierarchy && g.transform.position.z > ramp.LipZ && g.transform.position.z < ramp.LipZ + 40f).ToArray();
            Assert.GreaterOrEqual(air.Length, 3, "an arc of gifts after the lip");
            foreach (Gift g in air)
                Assert.Greater(g.transform.position.y - _game.track.SurfaceHeightAt(g.transform.position.z), 2.8f, "out of reach of a normal jump");
            yield return Until(() => Rider.Airborne, 4f, "takes off");
            yield return Until(() => !Rider.Airborne, 4f, "lands");
            Assert.GreaterOrEqual(_game.Gifts, air.Length - 1, "collected in the air");
        }

        [UnityTest]
        public IEnumerator DoRamp_FromAProgram_PlacesARampAhead()
        {
            yield return Ride();
            int before = Ramp.Active.Count;
            DeviceHub.Instance.Receive("test", "do:ramp");
            yield return null;
            Assert.AreEqual(1, _game.track.RampsPlaced);
            Assert.AreEqual(before + 1, Ramp.Active.Count);
            float z = Rider.transform.position.z;
            Ramp placed = Ramp.Active.OrderBy(r => r.transform.position.z).Last();
            Assert.Greater(placed.transform.position.z, z + 50f, "out of sight ahead");
            Assert.Less(placed.transform.position.z, z + _game.track.buildAhead, "on the built road");
            Assert.AreEqual(0f, placed.transform.position.x, 0.01f, "in the rider's lane");
        }

        [UnityTest]
        public IEnumerator GeneratorRamps_NeverOverlapPatternsTransitionsOrEachOther()
        {
            _tuning.rampChance = 1f; _tuning.rampFirstDistance = 120f; _tuning.rampSpacing = new Vector2(90f, 140f);
            _tuning.hazardChanceStart = _tuning.hazardChanceMax = 0.5f; _tuning.hazardStartDistance = 60f; _tuning.hazardGap = 30f;
            _tuning.boulevardSections = true; _tuning.boulevardMinDistance = 0f; _tuning.boulevardChance = 1f; _tuning.boulevardGap = 300f;
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = 0.5f;
            var patterns = new List<Vector2>();
            var zones = new List<Vector2>();
            TrackGenerator track = _game.track;
            System.Action<HazardPattern, float> onPattern = (p, start) => patterns.Add(new Vector2(start - 6f, start + p.length + 6f));
            System.Action<Ramp, Vector2> onRamp = (r, zone) =>
            {
                zones.Add(zone);
                for (float z = zone.x; z <= zone.y; z += 2f)
                {
                    RoadChunk c = track.ChunkAt(z, out _);
                    Assert.IsFalse(c != null && c.ChangesWidth, "ramp zone on a boulevard transition at z " + z);
                }
                Vector3 p = r.transform.position;
                foreach (Obstacle o in Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None))
                {
                    if (!o.gameObject.activeInHierarchy || o.GetComponent<Pedestrian>() != null || o.GetComponent<Mover>() != null) continue;
                    Vector3 q = o.transform.position;
                    bool inZone = q.z > r.LipZ && q.z < zone.y && Mathf.Abs(q.x) < track.HalfWidthAt(q.z);
                    Assert.IsFalse(inZone, "obstacle " + o.name + " left in the landing zone at z " + q.z);
                }
            };
            track.PatternScheduled += onPattern;
            track.RampPlaced += onRamp;
            try
            {
                _game.EnterReady();
                for (float z = 0f; z < 2400f; z += 20f)
                {
                    Rider.transform.position = new Vector3(0f, track.SurfaceHeightAt(z), z);
                    yield return null;
                }
            }
            finally { track.PatternScheduled -= onPattern; track.RampPlaced -= onRamp; }
            Assert.GreaterOrEqual(zones.Count, 5, "ramps were placed");
            Assert.Greater(patterns.Count, 3, "patterns were placed");
            Assert.Greater(track.BoulevardSections, 0, "boulevards were planned");
            foreach (Vector2 zone in zones)
            {
                foreach (Vector2 p in patterns)
                    Assert.IsFalse(zone.x < p.y && zone.y > p.x, "ramp zone " + zone + " overlaps pattern " + p);
                foreach (Vector2 other in zones)
                    if (other != zone) Assert.IsFalse(zone.x < other.y + _tuning.rampMinGap && zone.y > other.x - _tuning.rampMinGap, "ramps too close: " + zone + " " + other);
            }
        }

        [UnityTest]
        public IEnumerator NewRide_ClearsFlightGapAndTricks()
        {
            yield return Ride();
            KickerAhead(20f);
            yield return Until(() => Rider.Airborne, 4f, "takes off");
            Actions.Trick();
            yield return null;
            _game.StartRun();
            yield return null;
            Assert.IsFalse(Rider.Airborne);
            Assert.IsFalse(Rider.BigAir);
            Assert.IsFalse(Rider.Falling);
            Assert.AreEqual(0f, Rider.HeightAboveRoad, 0.01f);
            Assert.IsFalse(Actions.Tricking);
            Assert.AreEqual(Quaternion.identity, Rider.TrickPose);
            Assert.AreEqual(0, _game.track.RampsPlaced, "the generator starts over");
            Assert.AreEqual(RunState.Running, _game.State);
        }

        // ------------------------------------------------------------------ review images (not run by default)

        [UnityTest, Explicit("Renders review images of a ramp flight into art-src/ramps")]
        public IEnumerator Screenshots_RampFlight_FromTheChaseCamera()
        {
            _tuning.giftChance = 0.3f;
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = 0.35f;
            yield return Ride();
            // Keep the rider's own lane clear (the other lanes keep their cones and crates for the picture).
            foreach (Obstacle o in _game.track.GetComponentsInChildren<Obstacle>())
                if (Mathf.Abs(o.transform.position.x) < 1.7f && o.GetComponent<Pedestrian>() == null) o.gameObject.SetActive(false);
            var follow = Camera.main.GetComponent<FollowCamera>();
            Ramp ramp = KickerAhead(30f);
            Ramp platform = _game.track.PlaceRampAt(Platform, ramp.LipZ + 80f, 0f);
            yield return new WaitForSeconds(0.25f);
            Capture("01-kicker-ahead.png");
            yield return Until(() => Rider.transform.position.z > ramp.transform.position.z + ramp.rise * 0.6f, 4f, "on the face");
            Capture("02-riding-up.png");
            yield return Until(() => Rider.Airborne, 2f, "takes off");
            yield return new WaitForSeconds(0.15f);
            Capture("03-take-off.png");
            Actions.Trick();
            yield return new WaitForSeconds(0.3f);
            Capture("04-spin.png");
            yield return new WaitForSeconds(0.32f);
            Capture("05-spin-done.png");
            Actions.Trick();
            yield return new WaitForSeconds(0.3f);
            Capture("06-flip.png");
            yield return Until(() => !Rider.Airborne, 3f, "lands");
            yield return new WaitForSeconds(0.08f);
            Capture("07-landed.png");
            yield return Until(() => Rider.transform.position.z > platform.transform.position.z - 14f, 6f, "platform ahead");
            Capture("08-platform-ahead.png");
            yield return Until(() => Rider.Airborne, 4f, "off the platform");
            yield return new WaitForSeconds(0.45f);
            Capture("09-platform-flight.png");
            yield return Until(() => !Rider.Airborne, 4f, "lands");
            // A river gap with no ramp: SPLASH!
            Gap(Rider.transform.position.z + 22f, 10f);
            yield return Until(() => Rider.Falling, 4f, "falls in");
            yield return new WaitForSeconds(0.2f);
            Capture("10-splash.png");
            Assert.Pass("images in art-src/ramps");
        }

        static void Capture(string file)
        {
            Camera camera = Camera.main;
            const int w = 1600, h = 900;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            RenderTexture previous = RenderTexture.active, oldTarget = camera.targetTexture;
            Texture2D image = null;
            try
            {
                camera.targetTexture = rt;
                var request = new RenderPipeline.StandardRequest { destination = rt };
                for (int pass = 0; pass < 2; pass++)
                    if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request); else camera.Render();
                RenderTexture.active = rt;
                image = new Texture2D(w, h, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                image.Apply();
                string folder = System.IO.Path.Combine(Application.dataPath, "../art-src/ramps");
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, file), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = oldTarget;
                RenderTexture.ReleaseTemporary(rt);
                if (image != null) Object.Destroy(image);
            }
        }
    }
}
