using System.Collections;
using System.Collections.Generic;
using CityScooter.Devices;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>
    /// WA police in the real Main scene: attacking police raises the wanted level and starts a chase; pulling over,
    /// surrendering or being boxed in = ARRESTED; the police car or a paint pellet = BUSTED; escaping drops stars and
    /// ends in LOST THEM!; the helicopter comes at 3 stars; and every step reaches the micro:bits as a message.
    /// The street is made quiet (no random obstacles, hazards, people or turns) so only the police can end a ride.
    /// </summary>
    public class PoliceTests
    {
        Scene _main;
        GameController _game;
        PoliceChase _police;
        RiderActions _actions;
        GameTuning _calm;
        readonly List<string> _sent = new List<string>();
        readonly List<float> _sentAt = new List<float>();

        void OnSent(string target, string line) { _sent.Add(line); _sentAt.Add(Time.unscaledTime); }

        IEnumerator LoadMain(System.Action<GameTuning> tune = null)
        {
            DeviceHub.Instance?.Mixer.Reset();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
            _main = SceneManager.GetSceneByName("Main");
            SceneManager.SetActiveScene(_main);
            yield return null;
            _game = GameController.Instance;
            Assert.NotNull(_game);
            _police = _game.GetComponent<PoliceChase>();
            _actions = _game.GetComponent<RiderActions>();
            Assert.NotNull(_police, "GameController adds the police at runtime (no scene changes)");
            _calm = Object.Instantiate(_game.rider.tuning != null ? _game.rider.tuning : GameTuning.Default);
            _calm.obstacleChanceStart = _calm.obstacleChanceMax = 0f;
            _calm.hazardChanceStart = _calm.hazardChanceMax = 0f;
            _calm.jumpObstacleChance = 0f;
            _calm.giftChance = 0f;
            _calm.sidewalkPeople = Vector2.zero;
            _calm.fakeTurns = false;
            tune?.Invoke(_calm);
            _game.tuning = _calm;
            _game.rider.tuning = _calm;
            _game.track.tuning = _calm;
            FakeTurnDirector turns = _game.GetComponent<FakeTurnDirector>();
            if (turns != null) turns.tuning = _calm;
            _sent.Clear();
            _sentAt.Clear();
            DeviceHub.Instance.LineSent += OnSent;
        }

        IEnumerator Ride()
        {
            _game.StartRun();
            yield return null;
            yield return null;
        }

        GameObject PlaceAhead(GameObject prefab, float dx, float dz)
        {
            Vector3 p = _game.rider.transform.position;
            float x = p.x + dx, z = p.z + dz;
            GameObject go = _game.track.PlaceAt(prefab, new Vector3(x, _game.track.SurfaceHeightAt(x, z), z));
            Assert.NotNull(go, prefab.name);
            return go;
        }

        IEnumerator WhileRunning(float seconds, System.Action everyFrame = null)
        {
            for (float t = 0f; t < seconds && _game.State == RunState.Running; t += Time.deltaTime)
            {
                everyFrame?.Invoke();
                yield return null;
            }
        }

        static void Board(string line) => DeviceHub.Instance.Receive("test", line);

        void AssertLastPoliceMessage(string expected)
        {
            int last = _sent.LastIndexOf(expected), crash = _sent.LastIndexOf("crash");
            Assert.GreaterOrEqual(last, 0, expected + " was sent (sent: " + string.Join(",", _sent) + ")");
            Assert.GreaterOrEqual(crash, 0, "crash was sent too, for Lesson 1 programs");
            Assert.Less(crash, last, "crash comes first, " + expected + " last");
            foreach (string m in new[] { "chase:0", "heli:0" })
                if (_sent.Contains(m)) Assert.Less(_sent.LastIndexOf(m), last, m + " comes before " + expected);
        }

        // ---------------------------------------------------------------- attacking police

        [UnityTest]
        public IEnumerator PunchingAnOfficer_KnocksTheCapOff_TheOfficerStays_AndTheChaseStarts()
        {
            yield return LoadMain(t => t.copFirstDelay = 60f);
            Board("toon:0");
            yield return Ride();
            GameObject officer = PlaceAhead(_game.track.theme.policeOfficer, 1.4f, 3.5f);
            _actions.Punch();
            float t = 0f;
            while (_police.Wanted == 0 && t < 0.5f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(1, _police.Wanted, "punching an officer: one star");
            Assert.IsTrue(_police.Chasing, "the first star starts a chase");
            Assert.IsTrue(officer.activeInHierarchy, "officers do not go flying with cartoon mode off");
            Assert.IsTrue(officer.GetComponent<PoliceOfficer>().CapOff, "the cap flew off");
            yield return null;
            Assert.Contains("wanted:1", _sent);
            Assert.Contains("chase:1", _sent);
            Assert.Less(_sent.IndexOf("wanted:1"), _sent.IndexOf("chase:1"));
            Assert.IsFalse(_sent.Contains("ko"), "no knock-out");
            yield return WhileRunning(0.8f);
            Assert.AreEqual(RunState.Running, _game.State, "riding past the officer is safe");
        }

        [UnityTest]
        public IEnumerator CartoonMode_OfficersGoFlying_StillAStarEachTime()
        {
            yield return LoadMain(t => t.copFirstDelay = 60f);
            Board("toon:1");
            yield return Ride();
            _game.GrantGifts(2);
            GameObject officer = PlaceAhead(_game.track.theme.policeOfficer, 1.4f, 3.5f);
            _actions.Punch();
            float t = 0f;
            while (_police.Wanted == 0 && t < 0.5f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(1, _police.Wanted);
            Knockout ko = officer.GetComponent<Knockout>();
            Assert.IsTrue(ko != null && (ko.KnockedOut || !officer.activeSelf), "cartoon mode: the officer goes flying");
            yield return new WaitForSeconds(0.8f);
            PlaceAhead(_game.track.theme.policeOfficer, 0f, 8f); // ride straight into a second one
            yield return WhileRunning(2f);
            Assert.AreEqual(RunState.Running, _game.State, "cartoon mode with a gift: no crash");
            Assert.AreEqual(2, _police.Wanted, "riding an officer flying is a star too");
        }

        [UnityTest]
        public IEnumerator PunchingAPoliceCar_RaisesTheWantedLevel()
        {
            yield return LoadMain(t => t.copFirstDelay = 60f);
            yield return Ride();
            PlaceAhead(_game.track.theme.policeCar, 1.6f, 4.5f);
            Assert.IsFalse(_actions.Punch(), "BONK: a police car does not fly");
            Assert.AreEqual(1, _police.Wanted);
            Assert.IsTrue(_police.Chasing);
        }

        // ---------------------------------------------------------------- ARRESTED

        [UnityTest]
        public IEnumerator PullingOver_WhileChased_IsArrested()
        {
            yield return LoadMain(t => t.copFirstDelay = 60f);
            yield return Ride();
            _police.SetWanted(1);
            yield return WhileRunning(6f, () => Board("br"));
            Assert.AreEqual(RunState.Crashed, _game.State, "braking to a stop while chased ends the ride");
            Assert.AreEqual("ARRESTED!", _game.EndTitle);
            Assert.Less(_game.rider.Speed, 3.1f, "braking while chased goes right down (pulling over)");
            yield return null;
            AssertLastPoliceMessage("arrested");
            Assert.IsFalse(_police.Chasing);
        }

        [UnityTest]
        public IEnumerator Surrender_PullsOver_ButOnlyWhenChased()
        {
            yield return LoadMain(t => t.copFirstDelay = 60f);
            yield return Ride();
            Board("sur");
            yield return WhileRunning(1.2f);
            Assert.AreEqual(RunState.Running, _game.State, "sur does nothing when nobody is chasing you");
            Assert.Greater(_game.rider.Speed, 5f);
            _police.SetWanted(1);
            yield return null;
            Board("sur");
            yield return WhileRunning(5f);
            Assert.AreEqual("ARRESTED!", _game.EndTitle, "sur: the scooter brakes to a stop, then ARRESTED");
            AssertLastPoliceMessage("arrested");
        }

        [UnityTest]
        public IEnumerator PoliceCarAlongside_ForTooLong_IsArrested_AndCopDistanceIsSent()
        {
            yield return LoadMain(t => { t.copFirstDelay = 60f; t.heliFirstShotSeconds = 99f; });
            yield return Ride();
            _police.SetWanted(1);
            Assert.IsTrue(_police.SendCop(30f, PoliceChase.Tactic.Alongside));
            yield return WhileRunning(14f);
            Assert.AreEqual("ARRESTED!", _game.EndTitle, "boxed in by the police car for 1.5 s");
            AssertLastPoliceMessage("arrested");
            var cop = new List<int>();
            for (int i = 0; i < _sent.Count; i++) if (_sent[i].StartsWith("cop:")) cop.Add(i);
            Assert.GreaterOrEqual(cop.Count, 2, "cop:N while it closes in (sent: " + string.Join(",", _sent) + ")");
            Assert.Contains("cop:0", _sent, "right beside you is cop:0");
            for (int i = 3; i < cop.Count; i++)
                Assert.GreaterOrEqual(_sentAt[cop[i]] - _sentAt[cop[i - 3]], 0.99f, "at most 3 cop: messages per second (V1 boards)");
        }

        // ---------------------------------------------------------------- BUSTED (the police car)

        [UnityTest]
        public IEnumerator PoliceCarRamsTheRider_IsBusted_AndThrowsTheRiderForward()
        {
            yield return LoadMain(t => { t.copFirstDelay = 60f; t.heliFirstShotSeconds = 99f; });
            yield return Ride();
            _police.SetWanted(1);
            Assert.IsTrue(_police.SendCop(14f, PoliceChase.Tactic.Ram));
            yield return WhileRunning(10f);
            Assert.AreEqual(RunState.Crashed, _game.State);
            Assert.AreEqual("BUSTED!", _game.EndTitle, "hit by the police car");
            yield return null;
            AssertLastPoliceMessage("busted");
            float z = _game.rider.transform.position.z;
            yield return new WaitForSeconds(1f);
            Assert.NotNull(_actions.CrashRider);
            Assert.Greater(_actions.CrashRider.transform.position.z, z + 2f, "hit from behind: thrown forward");
        }

        [UnityTest]
        public IEnumerator SidesteppingTheRam_MakesThePoliceCarSpinOut()
        {
            yield return LoadMain(t => { t.copFirstDelay = 60f; t.heliFirstShotSeconds = 99f; });
            yield return Ride();
            _police.SetWanted(1);
            Assert.IsTrue(_police.SendCop(14f, PoliceChase.Tactic.Ram));
            float t = 0f;
            while (_police.Mode != PoliceChase.CopMode.Lunge && t < 6f && _game.State == RunState.Running) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(PoliceChase.CopMode.Lunge, _police.Mode, "the police car commits to the ram");
            Assert.Greater(_police.CopGap, 5f, "it commits early enough to see it coming");
            Assert.IsTrue(_police.CopDanger, "the HUD warns (POLICE! in red)");
            yield return WhileRunning(0.45f, () => Board("st:100"));
            Board("st:0");
            yield return WhileRunning(2.5f);
            Assert.AreEqual(RunState.Running, _game.State, "a sidestep after it commits gets you out of the way");
            Assert.AreEqual(1, _police.SpinOuts, "it overshot and spun out (SCREECH!)");
        }

        [UnityTest]
        public IEnumerator PoliceCar_NeverCutsAcrossTheRider_ItBrakesForTrafficInstead()
        {
            yield return LoadMain(t => { t.copFirstDelay = 60f; t.heliFirstShotSeconds = 99f; t.arrestSeconds = 99f; });
            yield return Ride();
            _police.SetWanted(1);
            Assert.IsTrue(_police.SendCop(20f, PoliceChase.Tactic.Alongside));
            float t = 0f;
            while (_police.CopGap > -1.5f && t < 10f) { t += Time.deltaTime; yield return null; }
            Assert.LessOrEqual(_police.CopGap, -1.5f, "the police car pulled up alongside");
            float dx = _police.Cop.transform.position.x - _game.rider.transform.position.x;
            Assert.Greater(Mathf.Abs(dx), 1.5f);
            PlaceAhead(_game.track.theme.policeCar, dx, 22f); // a parked car in the police car's lane; the only way round is the rider's lane
            yield return WhileRunning(4f);
            Assert.AreEqual(RunState.Running, _game.State, "it brakes (or spins out) rather than swerving into the rider: " + _game.EndTitle);
        }

        // ---------------------------------------------------------------- escaping

        [UnityTest]
        public IEnumerator StayingAwayFromThePolice_DropsStars_ThenLostThem()
        {
            yield return LoadMain(t => { t.copFirstDelay = 60f; t.escapeSeconds = 1f; t.wantedDecaySeconds = 99f; });
            yield return Ride();
            _police.SetWanted(2);
            yield return WhileRunning(1.5f);
            Assert.AreEqual(1, _police.Wanted, "no police car near for escapeSeconds: one star less");
            yield return WhileRunning(1.5f);
            Assert.AreEqual(0, _police.Wanted);
            Assert.IsFalse(_police.Chasing);
            Assert.AreEqual(200, _police.LastBonus, "LOST THEM! bonus: 100 per star of the highest level");
            Assert.AreEqual(RunState.Running, _game.State);
            int w0 = _sent.IndexOf("wanted:0"), c0 = _sent.IndexOf("chase:0"), lost = _sent.IndexOf("lost");
            Assert.Contains("wanted:1", _sent);
            Assert.That(w0 >= 0 && c0 > w0 && lost > c0, "wanted:0, chase:0, lost (sent: " + string.Join(",", _sent) + ")");
        }

        [UnityTest]
        public IEnumerator Stars_WearOff_WithoutNewAttacks()
        {
            yield return LoadMain(t => { t.copFirstDelay = 60f; t.escapeSeconds = 99f; t.wantedDecaySeconds = 0.8f; });
            yield return Ride();
            _police.SetWanted(2);
            yield return WhileRunning(1.1f);
            Assert.AreEqual(1, _police.Wanted);
            yield return WhileRunning(1f);
            Assert.AreEqual(0, _police.Wanted);
            Assert.Contains("lost", _sent);
        }

        // ---------------------------------------------------------------- helicopter

        [UnityTest]
        public IEnumerator Helicopter_ComesAtThreeStars_HoversAheadAndAbove_LeavesBelow()
        {
            yield return LoadMain(t => { t.copFirstDelay = 60f; t.heliFirstShotSeconds = 99f; });
            yield return Ride();
            PoliceHelicopter heli = _police.Helicopter;
            Assert.NotNull(heli);
            _police.SetWanted(2);
            yield return null;
            Assert.IsFalse(heli.InAir, "no helicopter at 2 stars");
            _police.SetWanted(3);
            yield return null;
            yield return null;
            Assert.IsTrue(heli.InAir);
            Assert.Contains("heli:1", _sent);
            yield return WhileRunning(2.5f);
            Assert.NotNull(heli.Body, "the helicopter is in the sky");
            Vector3 r = _game.rider.transform.position, h = heli.Body.position;
            Assert.Greater(h.z, r.z + 10f, "ahead of the rider");
            Assert.Greater(h.y, r.y + 4f, "above the rider");
            Assert.Less(h.z, r.z + 40f, "close enough to see");
            _police.SetWanted(2);
            yield return null;
            Assert.IsFalse(heli.InAir);
            Assert.Contains("heli:0", _sent);
        }

        [UnityTest]
        public IEnumerator PaintPellet_BustsARiderWhoStaysInTheRing()
        {
            yield return LoadMain(t => { t.copFirstDelay = 60f; t.heliFirstShotSeconds = 99f; t.sniperMinStars = 5; });
            yield return Ride();
            _police.SetWanted(3);
            yield return null;
            yield return null;
            Assert.IsTrue(_police.Helicopter.DropPellets(1), "a ring on the road where the rider will be");
            yield return null;
            Assert.Contains("inc:C", _sent, "the board hears inc:C (the ring is on you)");
            float dropped = Time.time;
            yield return WhileRunning(2.5f);
            Assert.AreEqual("BUSTED!", _game.EndTitle, "stayed in the ring: paint splat");
            Assert.GreaterOrEqual(Time.time - dropped, 0.8f, "at least 0.8 s of warning");
            yield return null;
            AssertLastPoliceMessage("busted");
        }

        [UnityTest]
        public IEnumerator PaintPellet_MissesARiderWhoMovesAway()
        {
            yield return LoadMain(t => { t.copFirstDelay = 60f; t.heliFirstShotSeconds = 99f; t.sniperMinStars = 5; });
            yield return Ride();
            _police.SetWanted(3);
            yield return null;
            yield return null;
            Assert.IsTrue(_police.Helicopter.DropPellets(1));
            yield return WhileRunning(0.5f, () => Board("st:100"));
            Board("st:0");
            yield return WhileRunning(1.5f);
            Assert.AreEqual(RunState.Running, _game.State, "a sidestep out of the ring: the pellet misses");
            Assert.AreEqual(1, _police.Helicopter.Misses);
        }

        [UnityTest]
        public IEnumerator SniperDot_Locks_ThenFires_MoveAwayOrBusted()
        {
            yield return LoadMain(t => { t.copFirstDelay = 60f; t.heliFirstShotSeconds = 99f; t.sniperInterval = 99f; });
            for (int round = 0; round < 2; round++)
            {
                bool dodge = round == 1;
                yield return Ride();
                _police.SetWanted(4);
                yield return null;
                yield return null;
                PoliceHelicopter heli = _police.Helicopter;
                Assert.IsTrue(heli.StartSniper(), "the red dot appears beside the rider");
                float t = 0f;
                while (Mathf.Abs(heli.SniperX - _game.rider.transform.position.x) > 0.2f && t < 3f) { t += Time.deltaTime; yield return null; }
                Assert.Less(Mathf.Abs(heli.SniperX - _game.rider.transform.position.x), 0.25f, "it slides onto the rider");
                _sent.Clear();
                Assert.IsTrue(heli.LockSniper());
                yield return null;
                Assert.Contains("inc:C", _sent, "locked on you: inc:C");
                if (dodge) yield return WhileRunning(0.4f, () => Board("st:-100"));
                Board("st:0");
                yield return WhileRunning(1.5f);
                if (dodge)
                {
                    Assert.AreEqual(RunState.Running, _game.State, "moved away while it blinked: missed");
                    Assert.AreEqual(1, heli.Misses);
                }
                else Assert.AreEqual("BUSTED!", _game.EndTitle, "stayed under the blinking dot");
                yield return new WaitForSeconds(1.1f);
            }
        }

        // ---------------------------------------------------------------- previews (not part of the normal run)

        /// <summary>Renders the WA police look into art-src/hazards/ (police car rear and roof, officer, helicopter, ring, dot).</summary>
        [UnityTest, Explicit]
        public IEnumerator PoliceLook()
        {
            yield return LoadMain(t => { t.copFirstDelay = 60f; t.heliFirstShotSeconds = 99f; t.sniperInterval = 99f; t.wantedDecaySeconds = 99f; t.escapeSeconds = 99f; });
            yield return Ride();
            Camera cam = Camera.main;
            // the officer close up, then a punch: BONK, the cap flies, OI!
            PlaceAhead(_game.track.theme.policeOfficer, 1.3f, 5.5f);
            yield return null;
            Capture(cam, "police_officer_close");
            yield return null; // a capture makes the next frame long; let it pass so the punch window is a normal one
            _actions.Punch();
            for (float w = 0f; _police.Wanted == 0 && w < 0.5f; w += Time.deltaTime) yield return null;
            yield return WhileRunning(0.12f);
            Capture(cam, "police_bonk_oi");
            yield return WhileRunning(1f);
            _police.SetWanted(0);
            PlaceAhead(_game.track.theme.policeCar, -2.4f, 9f);
            PlaceAhead(_game.track.theme.policeOfficer, -1.05f, 10.5f);
            PlaceAhead(_game.track.theme.policeOfficer, -3.4f, 12.5f);
            yield return null;
            Capture(cam, "police_car_and_officers");
            // the chase car pulling up alongside
            _police.SetWanted(1);
            Assert.IsTrue(_police.SendCop(12f, PoliceChase.Tactic.Alongside));
            float t = 0f;
            while (_police.CopGap > -1.5f && t < 8f && _game.State == RunState.Running) { t += Time.deltaTime; yield return null; }
            Capture(cam, "police_car_alongside");
            // a new ride at 4 stars: helicopter, paint target ring, red dot
            yield return new WaitForSeconds(0.2f);
            yield return Ride();
            _police.SetWanted(4);
            yield return WhileRunning(3f);
            Assert.IsTrue(_police.Helicopter.DropPellets(1));
            Assert.IsTrue(_police.Helicopter.StartSniper());
            yield return WhileRunning(0.4f);
            Capture(cam, "police_heli_ring_dot");
            yield return WhileRunning(0.55f);
            Capture(cam, "police_pellet_falling");
            yield return WhileRunning(1f);
            Capture(cam, "police_busted_splat");
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
            if (DeviceHub.Instance != null)
            {
                DeviceHub.Instance.LineSent -= OnSent;
                DeviceHub.Instance.Receive("test", "toon:0");
                DeviceHub.Instance.Receive("test", "st:0");
            }
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            if (_calm != null) Object.Destroy(_calm);
            DeviceHub.Instance?.Mixer.Reset();
        }
    }
}
