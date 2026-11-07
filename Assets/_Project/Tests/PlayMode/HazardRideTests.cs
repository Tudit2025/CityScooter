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
            Assert.IsTrue(actions.Punch(), "a cone right ahead gets punched");
            Assert.Contains("ko", _events);
            Assert.IsFalse(actions.Punch(), "cooldown: punching again at once does nothing");
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(cone.activeSelf, "the cone flew away");
            Assert.AreEqual(RunState.Running, _game.State, "riding through the punched cone's spot is safe");
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
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            DeviceHub.Instance?.Mixer.Reset();
        }
    }
}
