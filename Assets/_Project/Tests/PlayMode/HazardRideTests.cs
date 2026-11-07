using System.Collections;
using System.Collections.Generic;
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
    /// <summary>v0.2 in the real Main scene: fake turns, hazard patterns, moving cars, punch, bell, warnings.</summary>
    public class HazardRideTests
    {
        Scene _main;
        GameController _game;
        readonly List<string> _events = new List<string>();

        void OnKo() => _events.Add("ko");
        void OnWarn(string m) => _events.Add(m);
        void OnTurn(int d) => _events.Add("turn:" + d);
        void OnCrash() => _events.Add("crash");
        void OnBoing() => _events.Add("boing");

        IEnumerator LoadMain()
        {
            DeviceHub.Instance?.Mixer.Reset();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
            _main = SceneManager.GetSceneByName("Main");
            SceneManager.SetActiveScene(_main);
            yield return null;
            _game = GameController.Instance;
            Assert.NotNull(_game);
            _events.Clear();
            GameEvents.PunchLanded += OnKo;
            GameEvents.Warning += OnWarn;
            GameEvents.TurnChanged += OnTurn;
            GameEvents.Crashed += OnCrash;
            GameEvents.PersonBounced += OnBoing;
        }

        HazardPattern Pattern(string name)
        {
            foreach (HazardPattern p in _game.track.theme.hazards) if (p != null && p.name == name) return p;
            Assert.Fail("no pattern " + name);
            return null;
        }

        GameObject Prefab(HazardPattern p, string name)
        {
            foreach (HazardPattern.Entry e in p.entries) if (e.prefab != null && e.prefab.name == name) return e.prefab;
            Assert.Fail("no " + name + " in " + p.name);
            return null;
        }

        [UnityTest]
        public IEnumerator FakeTurn_BendsTheFarCity_TurnsTheSky_AndClearsOnUnload()
        {
            yield return LoadMain();
            var turns = _game.GetComponent<FakeTurnDirector>();
            Assert.NotNull(turns, "GameController adds the fake-turn director at runtime");
            _game.StartRun();
            turns.ForceTurn(1, 1f, 5f);
            yield return new WaitForSeconds(2.5f);
            Vector4 turn = Shader.GetGlobalVector("_ComicTurn");
            Assert.Greater(turn.x, 0f, "far city swings right");
            Assert.Greater(turns.Heading, 0f, "sky turned right");
            Assert.AreEqual(1, turns.Direction);
            Assert.Contains("turn:1", _events);
            Assert.AreEqual(RunState.Running, _game.State, "a fake turn never crashes the rider");
            yield return SceneManager.UnloadSceneAsync(_main);
            Assert.AreEqual(Vector4.zero, Shader.GetGlobalVector("_ComicTurn"), "unloading must not leave other scenes turned");
        }

        [UnityTest]
        public IEnumerator Punch_KnocksAConeOutOfTheWay_SendsKo_AndHasACooldown()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return null;
            HazardPattern works = Pattern("HZ_RoadworksZone");
            GameObject cone = _game.track.PlaceAt(Prefab(works, "HZ_Cone"), _game.rider.transform.position + new Vector3(0f, 0f, 3.5f));
            Assert.NotNull(cone);
            var actions = _game.GetComponent<RiderActions>();
            Assert.NotNull(actions);
            actions.Punch();
            Assert.IsFalse(actions.Punch(), "cooldown: punching again at once does nothing");
            float t = 0f;
            while (!_events.Contains("ko") && t < 0.4f) { t += Time.deltaTime; yield return null; }
            Assert.Contains("ko", _events, "the cone comes into the fist's reach while the punch is out");
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(cone.activeSelf, "the cone flew away");
            Assert.AreEqual(RunState.Running, _game.State, "riding through the punched cone's spot is safe");
        }

        [UnityTest]
        public IEnumerator Punch_SwingsTheRidersArm_EvenOnAMiss_ThenPutsItBack()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return null;
            var actions = _game.GetComponent<RiderActions>();
            MeshFilter body = _game.rider.model.Find("RiderAndScooter").GetComponent<MeshFilter>();
            Mesh riding = body.sharedMesh;
            Assert.IsFalse(actions.Punch(), "nothing to hit right at the start");
            Assert.NotNull(actions.Arm, "the punch arm is fitted to the rider");
            Assert.IsTrue(actions.Arm.Busy, "the arm swings anyway");
            Assert.AreNotSame(riding, body.sharedMesh, "the riding model loses that arm while the separate arm swings");
            Assert.IsTrue(actions.Arm.fist.gameObject.activeSelf);
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(actions.Arm.Busy);
            Assert.AreSame(riding, body.sharedMesh, "and gets it back afterwards");
            Assert.IsFalse(actions.Arm.fist.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Punch_OnACar_Bonks_ButDoesNotMoveIt()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return null;
            Vector3 p = _game.rider.transform.position;
            GameObject car = _game.track.PlaceAt(Prefab(Pattern("HZ_PoliceStop"), "HZ_PoliceCar"), new Vector3(p.x + 1.6f, p.y, p.z + 4.5f));
            Vector3 carAt = car.transform.position;
            var actions = _game.GetComponent<RiderActions>();
            Assert.IsFalse(actions.Punch(), "a police car cannot be punched away");
            Assert.NotNull(car.GetComponent<Wobble>(), "it shakes");
            Assert.AreEqual(carAt, car.transform.position);
            Assert.IsFalse(_events.Contains("ko"));
        }

        [UnityTest]
        public IEnumerator SpammingPunch_GetsTired_AndCostsSteering()
        {
            yield return LoadMain();
            bool tired = false;
            System.Action onTired = () => tired = true;
            GameEvents.Tired += onTired;
            _game.StartRun();
            yield return null;
            var actions = _game.GetComponent<RiderActions>();
            int thrown = 0;
            for (float t = 0f; t < 2f; t += Time.deltaTime)
            {
                if (actions.Punch()) thrown++;
                yield return null;
                if (actions.Arm != null && actions.Arm.Busy) Assert.Less(_game.rider.SteerFactor, 0.5f, "one hand off the handlebar while punching");
            }
            GameEvents.Tired -= onTired;
            Assert.LessOrEqual(actions.Punches, 5, "about 3 punches plus what recharges in 2 s, however fast pu arrives");
            Assert.Greater(actions.IgnoredPunches, 0);
            Assert.IsTrue(tired, "the board hears \"tired\"");
            Assert.Greater(actions.Misses, 0, "punching the air costs points");
            Assert.AreEqual(0, thrown, "nothing to hit");
        }

        sealed class CaptureOutput : IDeviceOutput
        {
            public readonly List<string> Lines = new List<string>();
            public void Send(string target, string line) => Lines.Add(target + "|" + line);
        }

        [UnityTest]
        public IEnumerator Say_TheGameRepeatsTheWordToEveryBoard_TwiceASecondAtMost()
        {
            yield return LoadMain();
            var hub = DeviceHub.Instance;
            Assert.NotNull(hub);
            var capture = new CaptureOutput();
            hub.RegisterOutput(capture);
            try
            {
                hub.Receive("usb1", "say:party");
                hub.Receive("usb1", "say:party"); // too soon: dropped
                hub.Receive("usb2", "say:jump!");
                hub.Receive("usb1", "say:crash"); // a game word: refused
                yield return null;
                Assert.Contains("*|party", capture.Lines);
                Assert.Contains("*|jump!", capture.Lines);
                Assert.AreEqual(1, capture.Lines.FindAll(l => l == "*|party").Count, "at most 2 a second per board");
                Assert.IsFalse(capture.Lines.Contains("*|crash"), "a board cannot make the game say crash");
                yield return new WaitForSecondsRealtime(0.55f);
                hub.Receive("usb1", "say:party");
                yield return null;
                Assert.AreEqual(2, capture.Lines.FindAll(l => l == "*|party").Count);
            }
            finally { hub.UnregisterOutput(capture); }
        }

        [UnityTest]
        public IEnumerator Do_TriggersTheGamesOwnEvents_OncePerSecondPerBoard()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return new WaitForSeconds(0.5f);
            var hub = DeviceHub.Instance;
            PoliceChase police = PoliceChase.Instance;
            Assert.NotNull(police);
            Assert.NotNull(_game.GetComponent<ProgramTriggers>(), "GameController adds the trigger handler");
            hub.Receive("usb1", "do:chase");
            yield return null;
            Assert.AreEqual(1, police.Wanted, "do:chase starts a police chase");
            hub.Receive("usb1", "do:stars:3");
            yield return null;
            Assert.AreEqual(1, police.Wanted, "one trigger a second per board");
            yield return new WaitForSeconds(1.05f);
            hub.Receive("usb1", "do:stars:3");
            yield return null;
            Assert.AreEqual(3, police.Wanted);
            float z = _game.rider.transform.position.z;
            hub.Receive("usb2", "do:turn:R");
            yield return null;
            Assert.IsTrue(System.Linq.Enumerable.Any(FakeTurnDirector.Instance.Planned, t => t.Direction == 1 && t.Start > z + 100f), "a right turn is planned ahead");
            yield return new WaitForSeconds(1.05f);
            hub.Receive("usb1", "do:calm");
            hub.Receive("usb2", "do:car");
            yield return null;
            Assert.AreEqual(0, police.Wanted, "do:calm: the police give up");
            Assert.Greater(_game.track.LastHazardRange.x, _game.rider.transform.position.z + 100f, "the oncoming car is placed out of sight ahead");
            Assert.AreEqual(RunState.Running, _game.State);
        }

        [UnityTest]
        public IEnumerator Boost_GoesPastTheSpeedLimit_ThenRecharges()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return new WaitForSeconds(1.5f);
            float before = _game.rider.Speed;
            Assert.IsTrue(_game.rider.Boost());
            Assert.IsFalse(_game.rider.Boost(), "a second boost right away is ignored (cooldown)");
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(_game.rider.Boosting);
            Assert.Greater(_game.rider.Speed, before * 1.15f, "faster while boosting");
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(_game.rider.Boosting);
            Assert.IsFalse(_game.rider.BoostReady, "still recharging");
        }

        [UnityTest]
        public IEnumerator SpammingTheBell_PeopleStopListening()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return null;
            var actions = _game.GetComponent<RiderActions>();
            for (int i = 0; i < 3; i++) { actions.Bell(); yield return new WaitForSeconds(1.05f); }
            PersonAhead(20f, out GameObject person);
            actions.Bell();
            Assert.AreEqual(1, actions.IgnoredBells, "the 4th ring within 5 s is ignored");
            Assert.IsFalse(person.GetComponent<Pedestrian>().IsStartled);
        }

        [UnityTest]
        public IEnumerator CartoonMode_WithoutGifts_BumpingSomeoneIsACrash()
        {
            yield return LoadMain();
            DeviceHub.Instance?.Receive("test", "toon:1");
            _game.StartRun();
            yield return null;
            Assert.AreEqual(0, _game.Gifts);
            PersonAhead(6f, out GameObject person);
            float t = 0f;
            while (_game.State == RunState.Running && t < 3f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(RunState.Crashed, _game.State, "each person sent flying costs a gift; none left means a crash");
        }

        [UnityTest]
        public IEnumerator Bell_MakesPeopleAheadStepAside()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return null;
            HazardPattern stroll = Pattern("HZ_SidewalkStroll");
            Vector3 p = _game.rider.transform.position;
            GameObject ped = _game.track.PlaceAt(Prefab(stroll, "HZ_PedWalkerA"), new Vector3(p.x + 0.3f, p.y, p.z + 25f));
            Assert.NotNull(ped);
            _game.GetComponent<RiderActions>().Bell();
            Assert.IsTrue(ped.GetComponent<Pedestrian>().IsStartled);
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(Mathf.Abs(ped.transform.position.x - _game.rider.transform.position.x), 1.2f, "stepped out of the rider's line");
        }

        [UnityTest]
        public IEnumerator OncomingCar_DrivesAtTheRider_Warns_AndCannotBePassedThrough()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return null;
            Vector3 p = _game.rider.transform.position;
            _game.rider.transform.position = new Vector3(2f, p.y, p.z); // in the oncoming lane on purpose
            _game.track.PlacePattern(Pattern("HZ_OncomingCar"), p.z + 30f);
            float t = 0f;
            while (_game.State == RunState.Running && t < 12f) { t += Time.deltaTime; yield return null; }
            Assert.Greater(_game.track.MovingCount, 0, "the car is driving");
            Assert.Contains("warn:car", _events);
            Assert.AreEqual(RunState.Crashed, _game.State, "riding into an oncoming car is a crash, at any frame rate");
        }

        [UnityTest]
        public IEnumerator RoadworksZone_HasAnOpenLane()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return null;
            Vector3 p = _game.rider.transform.position;
            _game.rider.transform.position = new Vector3(2.2f, p.y, p.z); // the open lane (not mirrored)
            _game.track.PlacePattern(Pattern("HZ_RoadworksZone"), p.z + 20f);
            float t = 0f;
            // Jump the manhole at the end of the zone like a student would.
            while (t < 25f && _game.rider.transform.position.z < p.z + 20f + 125f)
            {
                t += Time.deltaTime;
                yield return null;
                Assert.AreEqual(RunState.Running, _game.State, "crashed at z " + _game.rider.transform.position.z + " (events: " + string.Join(",", _events) + ")");
                if (Mathf.Abs(_game.rider.transform.position.z - (p.z + 20f + 112f)) < 2f) DeviceHub.Instance?.Receive("test", "jp");
            }
        }

        void PersonAhead(float dz, out GameObject person)
        {
            Vector3 p = _game.rider.transform.position;
            float z = p.z + dz;
            person = _game.track.PlaceAt(Prefab(Pattern("HZ_SidewalkStroll"), "HZ_PedWalkerA"), new Vector3(p.x, _game.track.SurfaceHeightAt(p.x, z), z));
            person.GetComponent<Pedestrian>().Configure(0f); // stands still in the rider's line
        }

        [UnityTest]
        public IEnumerator StreetLife_PutsPeopleOnTheSidewalks()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return null;
            int onSidewalk = 0;
            foreach (Pedestrian ped in Pedestrian.Active) if (Mathf.Abs(ped.transform.position.x) > 4.3f) onSidewalk++;
            Assert.GreaterOrEqual(onSidewalk, 12, "about 2-4 people per 40 m along the built street");
            Assert.That(_game.track.theme.pedestrians.Length, Is.GreaterThanOrEqualTo(10), "a whole set of people");
        }

        [UnityTest]
        public IEnumerator WalkingPeople_StayOnTheDownhillSidewalk()
        {
            yield return LoadMain();
            _game.StartRun();
            _game.rider.Stop(); // the rider stays put; the people walk
            yield return null;
            Vector3 p = _game.rider.transform.position;
            var people = new System.Collections.Generic.List<Transform>();
            foreach (float walk in new[] { 1.5f, -1.5f })
            {
                float x = walk > 0 ? 6f : -6f, z = p.z + 20f;
                GameObject go = _game.track.PlaceAt(Prefab(Pattern("HZ_SidewalkStroll"), "HZ_PedWalkerA"), new Vector3(x, _game.track.SurfaceHeightAt(x, z), z));
                go.GetComponent<Pedestrian>().Configure(walk);
                people.Add(go.transform);
            }
            yield return new WaitForSeconds(6f);
            foreach (Transform t in people)
            {
                Assert.Greater(Mathf.Abs(t.position.z - (p.z + 20f)), 6f, "walked");
                Assert.AreEqual(_game.track.SurfaceHeightAt(t.position.x, t.position.z), t.position.y, 0.05f, "feet on the sidewalk, not in the air");
                Assert.Less(Quaternion.Angle(t.rotation, Quaternion.identity), 0.5f, "standing upright");
            }
        }

        [UnityTest]
        public IEnumerator CartoonOff_BumpingSomeoneEndsTheRide_AndThrowsTheRider()
        {
            yield return LoadMain();
            DeviceHub.Instance?.Receive("test", "toon:0");
            _game.StartRun();
            yield return null;
            PersonAhead(6f, out GameObject person);
            float t = 0f;
            while (_game.State == RunState.Running && t < 4f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(RunState.Crashed, _game.State);
            Assert.IsTrue(person.activeInHierarchy, "people do not fly unless cartoon mode is on");
            var actions = _game.GetComponent<RiderActions>();
            Assert.NotNull(actions.CrashRider, "the rider is thrown off the scooter");
            Assert.IsTrue(actions.CrashRider.gameObject.activeSelf);
            Assert.IsFalse(_game.rider.model.gameObject.activeSelf, "the riding model is hidden while the crash plays");
            yield return new WaitForSeconds(1.2f);
            _game.StartRun();
            yield return null;
            Assert.IsTrue(_game.rider.model.gameObject.activeSelf, "a new ride brings the rider back on the scooter");
            Assert.IsFalse(actions.CrashRider.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator CartoonOn_PeopleGoFlying_SendsBoing_AndTheRideGoesOn()
        {
            yield return LoadMain();
            DeviceHub.Instance?.Receive("test", "toon:1");
            int collected = 0;
            System.Action onGift = () => collected++;
            GameEvents.GiftCollected += onGift;
            _game.StartRun();
            _game.GrantGifts(2);
            yield return null;
            Assert.IsTrue(RiderActions.CartoonMode, "toon:1 from a board turns cartoon mode on");
            PersonAhead(6f, out GameObject person);
            yield return new WaitForSeconds(2.2f);
            Assert.AreEqual(RunState.Running, _game.State, "no crash in cartoon mode");
            Assert.Contains("boing", _events);
            Assert.IsFalse(person.activeSelf, "the person flew off");
            GameEvents.GiftCollected -= onGift;
            Assert.AreEqual(2 + collected - 1, _game.Gifts, "it cost a gift");
            DeviceHub.Instance?.Receive("test", "toon:0");
            yield return null;
            Assert.IsFalse(RiderActions.CartoonMode);
        }

        [UnityTest]
        public IEnumerator Manhole_StopsTheScooterInTheHole_RiderFliesOn()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return null;
            Vector3 p = _game.rider.transform.position;
            _game.track.PlaceAt(Prefab(Pattern("HZ_Manhole"), "HZ_Manhole"), new Vector3(p.x, p.y, p.z + 8f));
            float t = 0f;
            while (_game.State == RunState.Running && t < 4f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(RunState.Crashed, _game.State);
            float crashZ = _game.rider.transform.position.z;
            yield return new WaitForSeconds(1.5f);
            var actions = _game.GetComponent<RiderActions>();
            Assert.Greater(actions.CrashRider.transform.position.z, crashZ + 1.5f, "over the handlebars");
        }

        /// <summary>Not part of the normal run: renders previews of the new hazards into art-src/hazards/.</summary>
        [UnityTest, Explicit]
        public IEnumerator Previews()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return null;
            _game.rider.Stop();
            var cam = Camera.main;
            float z0 = _game.rider.transform.position.z;
            string[] names = { "HZ_RoadworksZone", "HZ_PoliceStop", "HZ_Crossing", "HZ_SidewalkStroll", "HZ_ManholeMiddle" };
            for (int i = 0; i < names.Length; i++)
            {
                float z = z0 + i * 200f;
                _game.rider.ResetRider(new Vector3(0.8f, 0f, z));
                yield return null;
                _game.track.PlacePattern(Pattern(names[i]), z + 14f);
                for (int f = 0; f < 3; f++) yield return null;
                cam.GetComponent<FollowCamera>().SnapToTarget();
                Capture(cam, names[i]);
            }
            // a car pattern and a turn
            _game.rider.ResetRider(new Vector3(-1.5f, 0f, z0 + 1200f));
            _game.StartRun();
            _game.track.PlacePattern(Pattern("HZ_SlowCar"), _game.rider.transform.position.z + 20f);
            _game.GetComponent<FakeTurnDirector>().ForceTurn(-1, 1f, 6f);
            yield return new WaitForSeconds(2.5f);
            Capture(cam, "turn_left_slow_car");
        }

        /// <summary>Not part of the normal run: every new prop close up, plus the POW and DING pop-ups.</summary>
        [UnityTest, Explicit]
        public IEnumerator Lineup()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return null;
            Vector3 p = _game.rider.transform.position;
            _game.track.PlacePattern(Pattern("HZ_SidewalkStroll"), p.z + 200f); // clears random props near the start too
            HazardPattern works = Pattern("HZ_RoadworksZone"), police = Pattern("HZ_PoliceStop"), cross = Pattern("HZ_Crossing");
            void Put(HazardPattern src, string name, float x, float z, float yaw = 0f)
            {
                GameObject go = _game.track.PlaceAt(Prefab(src, name), new Vector3(x, _game.track.SurfaceHeightAt(x, p.z + z), p.z + z));
                go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
            string[] people = { "Office", "Student", "Surfer", "Jogger", "Elder", "Tourist", "Barista", "Skater", "DogWalker", "Shopper", "Kid", "Courier" };
            var crowd = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < people.Length; i++)
            {
                GameObject prefab = System.Array.Find(_game.track.theme.pedestrians, g => g.name == "HZ_Ped_" + people[i]);
                float x = -3.4f + (i % 6) * 1.36f, zz = 6.5f + (i / 6) * 2.6f;
                GameObject go = _game.track.PlaceAt(prefab, new Vector3(x, _game.track.SurfaceHeightAt(x, p.z + zz), p.z + zz));
                go.GetComponent<Pedestrian>().Configure(0f);
                crowd.Add(go);
            }
            _game.rider.Stop();
            for (int f = 0; f < 3; f++) yield return null;
            Camera.main.GetComponent<FollowCamera>().SnapToTarget();
            Capture(Camera.main, "people");
            foreach (GameObject go in crowd) go.SetActive(false);
            _game.rider.Begin();
            Put(works, "HZ_SignRoadWork", -3.6f, 9f);
            Put(works, "HZ_SignDetourRight", 3.4f, 9f);
            Put(works, "HZ_TrafficController", -1.2f, 7f);
            Put(works, "HZ_Worker", 1.3f, 7.5f);
            Put(works, "HZ_Manhole", 0.2f, 12f);
            Put(cross, "HZ_PedCrosserA", -2.4f, 14f);
            Put(cross, "HZ_PedWalkerB", 2.6f, 14f);
            Put(police, "HZ_PoliceCar", -2.3f, 19f);
            Put(works, "HZ_WorksTruck", 2.3f, 21f);
            Put(works, "HZ_BarrierLong", 0f, 26f);
            _game.rider.Stop();
            for (int f = 0; f < 3; f++) yield return null;
            Camera.main.GetComponent<FollowCamera>().SnapToTarget();
            Capture(Camera.main, "lineup");
            GameObject cone = _game.track.PlaceAt(Prefab(works, "HZ_Cone"), p + new Vector3(0f, 0f, 3f));
            _game.rider.Begin();
            _game.GetComponent<RiderActions>().Punch();
            _game.GetComponent<RiderActions>().Bell();
            _game.rider.Stop();
            for (int f = 0; f < 4; f++) yield return null;
            Capture(Camera.main, "popups");
            Assert.NotNull(cone);
        }

        /// <summary>Not part of the normal run: frames of a punch that lands on a crate stack.</summary>
        [UnityTest, Explicit]
        public IEnumerator PunchShots()
        {
            yield return LoadMain();
            _game.StartRun();
            yield return new WaitForSeconds(0.5f);
            Vector3 p = _game.rider.transform.position;
            Obstacle crates = System.Array.Find(_game.track.theme.obstacles, o => o.name == "OBS_Crates");
            _game.track.PlaceAt(crates.gameObject, new Vector3(p.x + 1.3f, _game.track.SurfaceHeightAt(p.x + 1.3f, p.z + 4.5f), p.z + 4.5f));
            var actions = _game.GetComponent<RiderActions>();
            actions.Punch();
            float t = 0f;
            while (!_events.Contains("ko") && t < 0.5f) { t += Time.deltaTime; yield return null; }
            Capture(Camera.main, "punch_1_contact");
            yield return new WaitForSecondsRealtime(0.12f);
            yield return null;
            Capture(Camera.main, "punch_2_after");
            yield return new WaitForSeconds(0.5f);
            actions.Punch(); // a miss: the arm still swings
            yield return null; yield return null; yield return null;
            Capture(Camera.main, "punch_miss");
        }

        /// <summary>Not part of the normal run: a right turn appears far ahead, comes closer, is ridden through.</summary>
        [UnityTest, Explicit]
        public IEnumerator TurnSequence()
        {
            yield return LoadMain();
            GameTuning calm = Object.Instantiate(_game.track.tuning != null ? _game.track.tuning : GameTuning.Default);
            calm.obstacleChanceStart = calm.obstacleChanceMax = 0f;
            calm.hazardChanceStart = calm.hazardChanceMax = 0f;
            calm.fakeTurns = false; // only the turn requested below
            _game.track.tuning = calm;
            var turns = _game.GetComponent<FakeTurnDirector>();
            turns.tuning = calm;
            _game.StartRun();
            yield return null;
            float z = _game.rider.transform.position.z;
            Assert.IsTrue(turns.RequestTurn(z + 150f, 1, 1f, 200f));
            for (int i = 0; i < 16; i++)
            {
                yield return new WaitForSeconds(1.5f);
                Capture(Camera.main, "turn_" + i.ToString("00") + "_z" + Mathf.RoundToInt(_game.rider.transform.position.z - z) + "_h" + Mathf.RoundToInt(turns.Heading * Mathf.Rad2Deg));
            }
            Object.Destroy(calm);
        }

        static void Capture(Camera camera, string name)
        {
            var rt = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            RenderTexture previous = RenderTexture.active, oldTarget = camera.targetTexture;
            Texture2D image = null;
            try
            {
                camera.targetTexture = rt;
                var request = new RenderPipeline.StandardRequest { destination = rt };
                for (int pass = 0; pass < 2; pass++)
                    if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request); else camera.Render();
                RenderTexture.active = rt;
                image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                string folder = System.IO.Path.Combine(Application.dataPath, "../art-src/hazards");
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = oldTarget;
                RenderTexture.ReleaseTemporary(rt);
                if (image != null) Object.Destroy(image);
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            GameEvents.PunchLanded -= OnKo;
            GameEvents.Warning -= OnWarn;
            GameEvents.TurnChanged -= OnTurn;
            GameEvents.Crashed -= OnCrash;
            GameEvents.PersonBounced -= OnBoing;
            DeviceHub.Instance?.Receive("test", "toon:0");
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            DeviceHub.Instance?.Mixer.Reset();
        }
    }
}
