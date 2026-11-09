using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityScooter.Devices;
using CityScooter.Game;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>Ramp feel (11/9) in the real Main scene: climbing costs speed and a deck coming down gives some back, the
    /// pose follows the ramp and the flight path (no snapping), suspension, the pop just before the lip, landing impact
    /// (hard costs speed, a deck coming down is soft, a stomp after a trick keeps it), the camera's pull-back and dip,
    /// the shared flight prediction (RampFlight) against the real flight, and the air: / land: order with all of it.
    /// Fixed 60 fps (Time.captureFramerate), so frame-to-frame checks do not depend on the machine.</summary>
    public class RampFeelTests
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
            Time.captureFramerate = 60;
            DeviceHub.Instance?.Mixer.Reset();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
            _main = SceneManager.GetSceneByName("Main");
            SceneManager.SetActiveScene(_main);
            yield return null;
            _game = GameController.Instance;
            Assert.NotNull(_game);
            _tuning = Object.Instantiate(_game.tuning);
            _tuning.fakeTurns = false;
            _tuning.hazardChanceStart = _tuning.hazardChanceMax = 0f;
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = 0f;
            _tuning.jumpObstacleChance = 0f;
            _tuning.giftChance = 0f;
            _tuning.boulevardSections = false;
            _tuning.rampChance = 0f; // tests place their own ramps
            _tuning.rampAirGifts = 0;
            _tuning.districts = false;
            _tuning.startSpeed = _tuning.maxSpeed = 12f;
            _game.tuning = _game.rider.tuning = _game.track.tuning = _tuning;
            var turns = _game.GetComponent<FakeTurnDirector>();
            if (turns != null) turns.tuning = _tuning;
            _sent.Clear();
            if (DeviceHub.Instance != null) DeviceHub.Instance.LineSent += OnSent;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.captureFramerate = 0;
            if (DeviceHub.Instance != null) DeviceHub.Instance.LineSent -= OnSent;
            Clear();
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            if (_tuning != null) Object.Destroy(_tuning);
            DeviceHub.Instance?.Mixer.Reset();
        }

        void Clear()
        {
            foreach (GameObject go in _made) if (go != null) Object.Destroy(go);
            _made.Clear();
        }

        RiderController Rider => _game.rider;
        RiderActions Actions => _game.GetComponent<RiderActions>();
        FollowCamera Follow => Camera.main.GetComponent<FollowCamera>();

        IEnumerator Ride(float speed)
        {
            _tuning.startSpeed = _tuning.maxSpeed = speed;
            _game.StartRun();
            yield return null;
            _sent.Clear();
        }

        // An unrotated Ramp on the road surface (like the district chunks), road-wide so steering never misses it.
        Ramp RampAt(float z, float rise, float height, float top, float launch = 1f)
        {
            var go = new GameObject("TestRamp");
            _made.Add(go);
            go.transform.position = new Vector3(0f, _game.track.SurfaceHeightAt(0f, z), z);
            var r = go.AddComponent<Ramp>();
            r.width = 16f; r.rise = rise; r.height = height; r.top = top; r.launch = launch; r.hangGravity = 0.5f;
            return r;
        }

        Ramp Kicker(float metres) => RampAt(Rider.transform.position.z + metres, 5f, 1.4f, 0f); // RMP_Kicker's shape

        float LocalZ(Ramp r) => r.transform.InverseTransformPoint(Rider.transform.position).z;

        IEnumerator Until(System.Func<bool> condition, float seconds, string what)
        {
            float t = 0f;
            while (!condition() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(condition(), what);
        }

        // ------------------------------------------------------------------ slope physics

        [UnityTest]
        public IEnumerator RidingUpARamp_IsSlowerThanTheFlatRoad_AndTheTopGivesItBack()
        {
            yield return Ride(9f);
            yield return new WaitForSeconds(0.5f);
            float flat = Rider.Speed;
            Assert.AreEqual(9f, flat, 0.05f, "cruising on the flat road");
            Ramp ramp = RampAt(Rider.transform.position.z + 12f, 10f, 2.5f, 8f);
            yield return Until(() => LocalZ(ramp) > ramp.rise - 0.3f, 4f, "up the slope");
            Assert.IsFalse(Rider.Airborne);
            Assert.AreSame(ramp, Rider.OnRamp);
            float top = Rider.Speed;
            TestContext.WriteLine("flat " + flat + " m/s, top of the slope " + top + " m/s");
            Assert.Less(top, flat * 0.9f, "the climb costs speed");
            Assert.GreaterOrEqual(top, flat * _tuning.rampClimbKeep - 0.05f, "but never more than rampClimbKeep");
            float expected = RampFlight.AfterClimb(flat, ramp.height, flat, flat, _tuning);
            Assert.AreEqual(expected, top, 0.25f, "as RampFlight predicts");
            yield return Until(() => LocalZ(ramp) > ramp.Length - 0.5f, 3f, "along the top");
            Assert.Greater(Rider.Speed, top + 0.5f, "the motor wins speed back on the flat top");
            yield return Until(() => Rider.Airborne, 2f, "off the lip");
            Assert.AreEqual(RampFlight.LipSpeed(ramp, flat, _tuning), Rider.Speed, 0.35f, "lip speed as RampFlight predicts");
        }

        // CHN_Quay_Bridge style hump (rise 10, height 2.2, top 8) whose deck comes back down over 8 m (a Ramp turned round).
        [UnityTest]
        public IEnumerator SlowOverABridge_LandingOnTheDeckComingDown_IsSoft_AndRidingDownGivesSpeedBack()
        {
            yield return Ride(6f);
            float z = Rider.transform.position.z + 12f;
            RampAt(z, 10f, 2.2f, 8f);
            Ramp down = RampAt(z + 26f, 8f, 2.2f, 0f);
            down.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            yield return Until(() => Rider.Airborne, 6f, "off the crest");
            int landings = Rider.Landings;
            yield return Until(() => Rider.Landings > landings, 3f, "lands");
            Assert.AreSame(down, Rider.OnRamp, "on the deck coming down");
            Assert.Less(Rider.LastLandingHardness, 0.05f, "the deck meets the flight: soft (impact " + Rider.LastLandingImpact + " m/s)");
            Assert.AreEqual(0f, Rider.LastLandingSpeedLoss, 0.05f, "a soft landing keeps the speed");
            float landed = Rider.Speed, most = landed;
            while (Rider.OnRamp == down && !Rider.Airborne) { most = Mathf.Max(most, Rider.Speed); yield return null; }
            TestContext.WriteLine("landed on the deck at " + landed + " m/s, " + most + " m/s at the bottom");
            Assert.Greater(most, landed + 0.3f, "riding down the deck gives speed back");
            Assert.LessOrEqual(most, 6f * _tuning.rampDescentMaxBoost + 0.05f);
            Assert.AreEqual(RunState.Running, _game.State);
        }

        // ------------------------------------------------------------------ pose, suspension, camera

        [UnityTest]
        public IEnumerator Pose_FollowsTheRamp_ThenNoseUpRisingNoseDownFalling_NoSnapping_AndTheRiderCrouches()
        {
            yield return Ride(12f);
            yield return new WaitForSeconds(0.3f);
            Ramp ramp = Kicker(20f);
            float slopeAngle = Mathf.Atan(ramp.Slope) * Mathf.Rad2Deg;
            float last = Rider.RampPitch, worstStep = 0f, footCrouch = 0f, onSlope = float.NaN;
            while (!Rider.Airborne)
            {
                yield return null;
                worstStep = Mathf.Max(worstStep, Mathf.Abs(Rider.RampPitch - last));
                last = Rider.RampPitch;
                float lz = LocalZ(ramp);
                if (lz > -1f && lz < ramp.rise * 0.5f) footCrouch = Mathf.Max(footCrouch, Rider.Crouch);
                if (float.IsNaN(onSlope) && lz > ramp.rise * 0.7f) onSlope = Rider.RampPitch;
                Assert.Less(Rider.transform.position.z, ramp.LipZ + 3f, "takes off at the lip");
            }
            Assert.AreEqual(slopeAngle, onSlope, 2f, "nose up with the ramp's slope");
            Assert.Greater(footCrouch, 0.15f, "the rider crouches as the wheels hit the ramp's foot");

            float risingMax = float.MinValue, fallingMin = float.MaxValue, apexPitch = 0f, apex = 0f, stretch = 0f, pull = 0f;
            float risingAt = 0f, fallingAt = 0f;
            int landings = Rider.Landings;
            while (Rider.Airborne)
            {
                worstStep = Mathf.Max(worstStep, Mathf.Abs(Rider.RampPitch - last));
                last = Rider.RampPitch;
                if (Rider.RampPitch > risingMax) { risingMax = Rider.RampPitch; risingAt = Rider.AirTime; }
                if (Rider.RampPitch < fallingMin) { fallingMin = Rider.RampPitch; fallingAt = Rider.AirTime; }
                if (Rider.HeightAboveRoad > apex) { apex = Rider.HeightAboveRoad; apexPitch = Rider.RampPitch; }
                stretch = Mathf.Min(stretch, Rider.Crouch);
                pull = Mathf.Max(pull, Follow.PullBack);
                yield return null;
            }
            TestContext.WriteLine("pitch: slope " + onSlope + ", rising max " + risingMax + " at " + risingAt + " s, apex " + apexPitch +
                                  ", falling min " + fallingMin + " at " + fallingAt + " s; worst step " + worstStep + " deg/frame");
            Assert.Greater(risingMax, slopeAngle - 1f, "nose up leaving the lip (at least the ramp's angle)");
            Assert.Less(fallingMin, -12f, "nose down coming down");
            Assert.Less(risingAt, fallingAt, "up first, then down");
            Assert.Less(Mathf.Abs(apexPitch), 8f, "about level at the top");
            Assert.Less(worstStep, 4f, "no snapping at the foot, the lip or in the air (degrees per frame at 60 fps)");
            Assert.Less(stretch, -0.1f, "the rider stretches at take-off");
            Assert.Greater(pull, 0.6f * _tuning.rampCameraPullBack, "the camera pulls back for the flight");

            Assert.Greater(Rider.Landings, landings);
            float landCrouch = 0f, dip = 0f, levelAfter = 0f;
            for (int i = 0; i < 30; i++)
            {
                landCrouch = Mathf.Max(landCrouch, Rider.Crouch);
                dip = Mathf.Min(dip, Follow.LandingDip);
                yield return null;
            }
            levelAfter = Rider.RampPitch;
            Assert.Greater(landCrouch, 0.3f, "the rider crouches on landing (impact " + Rider.LastLandingImpact + " m/s)");
            Assert.Less(dip, -0.05f, "the camera dips at the landing");
            Assert.AreEqual(0f, levelAfter, 1.5f, "level on the road again");
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(0f, Rider.Crouch, 0.05f, "the suspension settles");
            Assert.AreEqual(0f, Follow.LandingDip, 0.05f, "so does the camera");
            Assert.AreEqual(RunState.Running, _game.State);
        }

        // ------------------------------------------------------------------ pop

        IEnumerator FlyOff(Ramp ramp, System.Action<float, float> result)
        {
            float top = 0f, takeOffZ = float.NaN;
            while (!Rider.Airborne && _game.State == RunState.Running) yield return null;
            takeOffZ = Rider.transform.position.z;
            while (Rider.Airborne && _game.State == RunState.Running) { top = Mathf.Max(top, Rider.HeightAboveRoad); yield return null; }
            result(top, takeOffZ);
        }

        [UnityTest]
        public IEnumerator Pop_JustBeforeTheLip_FliesHigher_AnEarlierJumpIsANormalJump()
        {
            // 1. No press: the plain flight off the kicker.
            yield return Ride(9f);
            Ramp ramp = Kicker(15f);
            float plain = 0f, plainZ = 0f;
            yield return FlyOff(ramp, (h, z) => { plain = h; plainZ = z; });
            Assert.IsFalse(Rider.LastTakeOffPopped);
            CollectionAssert.AreEqual(new[] { "air:1", "air:0", "land:ok" }, _sent);
            float plainLaunch = Rider.LastLaunchVelocity;

            // 2. A just before the lip: the pop.
            Clear();
            yield return Ride(9f);
            ramp = Kicker(15f);
            yield return Until(() => (ramp.LipZ - Rider.transform.position.z) / Mathf.Max(1f, Rider.Speed) < 0.15f, 4f, "close to the lip");
            Assert.IsFalse(Rider.Airborne);
            DeviceHub.Instance.Receive("test", "jp");
            yield return null;
            Assert.IsTrue(Rider.PopQueued || Rider.Airborne, "the pop waits for the lip");
            float popped = 0f, poppedZ = 0f;
            yield return FlyOff(ramp, (h, z) => { popped = h; poppedZ = z; });
            Assert.IsTrue(Rider.LastTakeOffPopped, "popped");
            Assert.Greater(poppedZ, ramp.LipZ - 0.6f, "off the lip, not the face");
            Assert.AreEqual(plainLaunch + _tuning.rampPopVelocity, Rider.LastLaunchVelocity, 0.3f, "pop velocity on top of the lip's launch");
            TestContext.WriteLine("highest: plain " + plain + " m, pop " + popped + " m");
            Assert.Greater(popped, plain + 0.8f, "a pop flies higher");
            CollectionAssert.AreEqual(new[] { "air:1", "air:0", "land:ok" }, _sent, "the same messages, once each, in order");
            Assert.AreEqual(RunState.Running, _game.State);

            // 3. A earlier on the face: a normal jump from the ramp surface, right away (as before).
            Clear();
            yield return Ride(9f);
            ramp = Kicker(15f);
            yield return Until(() => LocalZ(ramp) > ramp.rise * 0.3f, 4f, "on the face");
            Assert.Greater((ramp.LipZ - Rider.transform.position.z) / Rider.Speed, _tuning.rampPopWindow + 0.05f, "well before the pop window");
            DeviceHub.Instance.Receive("test", "jp");
            yield return Until(() => Rider.Airborne, 0.1f, "jumps at once");
            Assert.Less(Rider.transform.position.z, ramp.LipZ - 1f, "from the face");
            Assert.IsFalse(Rider.LastTakeOffPopped);
            Assert.AreEqual(_tuning.jumpVelocity, Rider.LastLaunchVelocity, 0.01f, "a normal jump");
            Assert.IsTrue(Rider.BigAir, "floats like a ramp flight (hang gravity), as before");
            yield return Until(() => !Rider.Airborne, 3f, "lands");
            Assert.AreEqual(RunState.Running, _game.State);
        }

        // ------------------------------------------------------------------ landing

        [UnityTest]
        public IEnumerator HardLanding_CostsMoreSpeed_ThanASoftOne()
        {
            // Soft: a low kicker at the start speed.
            yield return Ride(9f);
            Kicker(15f);
            yield return Until(() => Rider.Airborne, 4f, "takes off");
            yield return Until(() => !Rider.Airborne, 4f, "lands");
            float softImpact = Rider.LastLandingImpact, softLoss = Rider.LastLandingSpeedLoss;

            // Hard: a platform (2 m high) with a pop at 12 m/s.
            Clear();
            yield return Ride(12f);
            Ramp platform = RampAt(Rider.transform.position.z + 15f, 7f, 2f, 6f);
            yield return Until(() => (platform.LipZ - Rider.transform.position.z) / Mathf.Max(1f, Rider.Speed) < 0.15f, 4f, "close to the lip");
            DeviceHub.Instance.Receive("test", "jp");
            yield return Until(() => Rider.Airborne, 1f, "pops off the platform");
            float before = 0f;
            while (Rider.Airborne) { before = Rider.Speed; yield return null; }
            float hardImpact = Rider.LastLandingImpact, hardLoss = Rider.LastLandingSpeedLoss;
            TestContext.WriteLine("soft: impact " + softImpact + " m/s, lost " + softLoss + " m/s; hard: impact " + hardImpact + " m/s, lost " + hardLoss + " m/s of " + before);
            Assert.Greater(hardImpact, softImpact + 2f);
            Assert.Greater(Rider.LastLandingHardness, 0.4f, "a hard landing");
            Assert.Greater(hardLoss, softLoss + 0.5f, "a hard landing costs more speed");
            Assert.LessOrEqual(hardLoss, before * _tuning.rampLandMaxLoss + 0.01f, "never more than rampLandMaxLoss");
            Assert.AreEqual(before - hardLoss, Rider.Speed, 0.3f, "the speed really dropped");
            CollectionAssert.AreEqual(new[] { "air:1", "air:0", "land:ok" }, _sent);
            Assert.AreEqual(RunState.Running, _game.State);
        }

        [UnityTest]
        public IEnumerator TrickThenStomp_KeepsTheSpeed_AndLandTrickIsSentAsBefore()
        {
            yield return Ride(12f);
            Ramp ramp = Kicker(15f);
            yield return Until(() => (ramp.LipZ - Rider.transform.position.z) / Mathf.Max(1f, Rider.Speed) < 0.15f, 4f, "close to the lip");
            DeviceHub.Instance.Receive("test", "jp");
            yield return Until(() => Rider.Airborne, 1f, "pops");
            Assert.IsTrue(Actions.Trick(), "a spin");
            float before = 0f;
            while (Rider.Airborne) { before = Rider.Speed; yield return null; }
            Assert.AreEqual(1, Actions.Tricks);
            Assert.AreEqual(0, Actions.SketchyLandings);
            TestContext.WriteLine("stomp: " + before + " m/s in the air, " + Rider.Speed + " m/s after (impact " + Rider.LastLandingImpact + ")");
            Assert.AreEqual(0f, Rider.LastLandingSpeedLoss, 1e-4f, "a stomp loses nothing");
            Assert.Greater(Rider.Speed, before * (1f + _tuning.rampStompBoost * 0.5f), "and gets a little extra");
            CollectionAssert.AreEqual(new[] { "air:1", "air:0", "land:trick" }, _sent);
            Assert.AreEqual(RunState.Running, _game.State);
        }

        // ------------------------------------------------------------------ the shared prediction

        [UnityTest]
        public IEnumerator SharedFlightPrediction_MatchesTheRealFlight_MatildaInletAtStartSpeed()
        {
            // CHN_Matilda_Inlet: road-wide ramp (rise 6, height 1.6, top 2), 6 m gap right after the lip, 9 m/s.
            yield return Ride(9f);
            Ramp ramp = RampAt(Rider.transform.position.z + 12f, 6f, 1.6f, 2f);
            var go = new GameObject("TestGap");
            _made.Add(go);
            go.transform.position = new Vector3(0f, _game.track.SurfaceHeightAt(0f, ramp.LipZ), ramp.LipZ);
            var gap = go.AddComponent<RoadGap>();
            gap.length = 6f;
            float predicted = RampFlight.Distance(ramp, 9f, _tuning, out float seconds, out float vy, out float lip);
            yield return Until(() => Rider.Airborne, 4f, "takes off");
            Assert.AreEqual(lip, Rider.Speed, 0.35f, "speed at the lip");
            Assert.AreEqual(vy, Rider.LastLaunchVelocity, 0.15f, "take-off speed upwards");
            yield return Until(() => !Rider.Airborne || Rider.Falling, 3f, "comes down");
            Assert.IsFalse(Rider.Falling, "cleared the inlet");
            TestContext.WriteLine("predicted " + predicted + " m in " + seconds + " s, flew " + Rider.LastAirDistance + " m in " + Rider.LastAirTime + " s");
            Assert.AreEqual(predicted, Rider.LastAirDistance, 0.8f, "RampFlight predicts the real flight");
            Assert.AreEqual(seconds, Rider.LastAirTime, 0.06f);
            Assert.Greater(Rider.LastAirDistance, gap.length + 2f, "with room to spare");
        }

        // ------------------------------------------------------------------ review strip (not run by default)

        readonly List<Texture2D> _crops = new List<Texture2D>(), _sides = new List<Texture2D>();
        Camera _side;

        [UnityTest, Explicit("Renders a strip of chase-camera frames through one ramp flight into art-src/ramps/feel")]
        public IEnumerator Screenshots_RampFeel_FrameStrip()
        {
            _tuning.giftChance = 0.3f;
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = 0.3f;
            _tuning.rampAirGifts = 5;
            yield return Ride(11f);
            foreach (Obstacle o in _game.track.GetComponentsInChildren<Obstacle>())
                if (Mathf.Abs(o.transform.position.x) < 1.7f && o.GetComponent<Pedestrian>() == null) o.gameObject.SetActive(false);
            Ramp kicker = _game.track.theme.ramps.FirstOrDefault(r => r.name == "RMP_Kicker");
            Ramp ramp = kicker != null ? _game.track.PlaceRampAt(kicker, Rider.transform.position.z + 28f, 0f) : Kicker(28f);
            Assert.NotNull(ramp);
            try
            {
                yield return new WaitForSeconds(0.2f);
                yield return Until(() => LocalZ(ramp) > 0.2f, 4f, "at the foot");
                for (int i = 0; i < 4; i++) yield return null; // the crouch as the wheels hit the ramp
                Capture("01-foot.png");
                yield return Until(() => LocalZ(ramp) > ramp.rise * 0.7f, 2f, "riding up");
                Capture("02-up.png");
                yield return Until(() => (ramp.LipZ - Rider.transform.position.z) / Mathf.Max(1f, Rider.Speed) < 0.15f, 2f, "at the lip");
                DeviceHub.Instance.Receive("test", "jp"); // a pop
                yield return Until(() => Rider.Airborne, 1f, "pops off the lip");
                for (int i = 0; i < 4; i++) yield return null; // the stretch
                Capture("03-lip.png");
                yield return new WaitForSeconds(0.2f);
                Capture("04-rise.png");
                float last = Rider.HeightAboveRoad;
                while (Rider.Airborne) { if (Rider.HeightAboveRoad < last) break; last = Rider.HeightAboveRoad; yield return null; }
                Capture("05-apex.png");
                float apex = last;
                yield return Until(() => Rider.HeightAboveRoad < apex * 0.35f || !Rider.Airborne, 3f, "falling");
                Capture("06-fall.png");
                yield return Until(() => !Rider.Airborne, 3f, "lands");
                for (int i = 0; i < 6; i++) yield return null; // deepest squash
                Capture("07-landing.png");
                for (int i = 0; i < 9; i++) yield return null; // springs back up
                Capture("08-rebound.png");
                yield return new WaitForSeconds(0.6f);
                Capture("09-after.png");
                SaveSheet("00-sheet.png", _crops);
                SaveSheet("00-side.png", _sides);
                TestContext.WriteLine("landing impact " + Rider.LastLandingImpact + " m/s, lost " + Rider.LastLandingSpeedLoss + " m/s");
            }
            finally
            {
                foreach (Texture2D t in _crops.Concat(_sides)) if (t != null) Object.Destroy(t);
                _crops.Clear();
                _sides.Clear();
                if (_side != null) Object.Destroy(_side.gameObject);
            }
            Assert.Pass("images in art-src/ramps/feel");
        }

        static string StripFolder => System.IO.Path.Combine(Application.dataPath, "../art-src/ramps/feel");

        // One chase-camera frame (camera.Render), plus a crop around the rider for the contact sheet, plus the same moment
        // seen from the side (the chase camera is right behind the rider, where the pitch is hard to see).
        void Capture(string file)
        {
            CaptureSide();
            Camera camera = Camera.main;
            const int w = 1280, h = 720, crop = 360;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            RenderTexture previous = RenderTexture.active, oldTarget = camera.targetTexture;
            Texture2D image = null;
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                camera.Render();
                Vector3 at = camera.WorldToScreenPoint(Rider.transform.position + Vector3.up * 0.9f);
                RenderTexture.active = rt;
                image = new Texture2D(w, h, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                image.Apply();
                System.IO.Directory.CreateDirectory(StripFolder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(StripFolder, file), image.EncodeToPNG());
                int x0 = Mathf.Clamp(Mathf.RoundToInt(at.x) - crop / 2, 0, w - crop), y0 = Mathf.Clamp(Mathf.RoundToInt(at.y) - crop / 2, 0, h - crop);
                var c = new Texture2D(crop, crop, TextureFormat.RGB24, false);
                c.SetPixels(image.GetPixels(x0, y0, crop, crop));
                c.Apply();
                _crops.Add(c);
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = oldTarget;
                RenderTexture.ReleaseTemporary(rt);
                if (image != null) Object.Destroy(image);
            }
        }

        void CaptureSide()
        {
            if (_side == null)
            {
                _side = new GameObject("TestSideCamera").AddComponent<Camera>();
                _side.CopyFrom(Camera.main);
                _side.enabled = false;
                _side.fieldOfView = 34f;
            }
            Vector3 p = Rider.transform.position + Vector3.up * 0.9f;
            _side.transform.position = p + new Vector3(-6f, 0.5f, 0.3f);
            _side.transform.rotation = Quaternion.LookRotation(p - _side.transform.position, Vector3.up);
            const int size = 360;
            var rt = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            RenderTexture previous = RenderTexture.active;
            try
            {
                _side.targetTexture = rt;
                _side.Render();
                _side.Render();
                RenderTexture.active = rt;
                var c = new Texture2D(size, size, TextureFormat.RGB24, false);
                c.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                c.Apply();
                _sides.Add(c);
            }
            finally
            {
                RenderTexture.active = previous;
                _side.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        // Pictures in reading order (left to right, top to bottom), 3 per row.
        static void SaveSheet(string file, List<Texture2D> pictures)
        {
            if (pictures.Count == 0) return;
            int size = pictures[0].width, cols = 3, rows = (pictures.Count + cols - 1) / cols, gap = 6;
            var sheet = new Texture2D(cols * size + (cols + 1) * gap, rows * size + (rows + 1) * gap, TextureFormat.RGB24, false);
            var fill = new Color[sheet.width * sheet.height];
            for (int i = 0; i < fill.Length; i++) fill[i] = Color.white;
            sheet.SetPixels(fill);
            for (int i = 0; i < pictures.Count; i++)
            {
                int col = i % cols, row = rows - 1 - i / cols;
                sheet.SetPixels(gap + col * (size + gap), gap + row * (size + gap), size, size, pictures[i].GetPixels());
            }
            sheet.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(StripFolder, file), sheet.EncodeToPNG());
            Object.Destroy(sheet);
        }
    }
}
