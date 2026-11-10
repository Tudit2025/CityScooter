using System.Collections;
using CityScooter.Devices;
using CityScooter.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>
    /// Review images of the item race (v0.5, docs/item-race.md) in the real Main scene, HUD included: the row of boxes,
    /// an item in the slot, a rocket fired, the shield and a blocked hit, the boost, oil on the road and slipping on it,
    /// a bolt, a rocket coming in and BOOM!, the police in a race. A fake radio plays three other boards (BEN and CHLOE
    /// ahead, DAN behind) through DeviceHub.Receive, as the student's board would pass them on.
    /// Same capture as RaceScreenshots (camera into a 1600x900 texture, the HUD canvas drawn by that camera).
    /// Images go to art-src/race/previews/items-*.png.
    /// </summary>
    public class RaceItemScreenshots
    {
        const string Board = "usb1";
        Scene _main;
        GameController _game;
        GameTuning _tuning;
        RaceDirector _race;
        RaceItems _items;
        DeviceHub _hub;
        RenderTexture _rt;
        Canvas _canvas;
        Camera _camera;
        RenderMode _oldMode;
        bool _oldKeyboard;
        float _nextRace, _nextBots;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _hub = DeviceHub.Instance;
            Assert.NotNull(_hub);
            _hub.Mixer.Reset();
            _oldKeyboard = _hub.KeyboardEnabled;
            _hub.SetKeyboardEnabled("0"); // the class build: micro:bit only
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
            _main = SceneManager.GetSceneByName("Main");
            SceneManager.SetActiveScene(_main);
            yield return null;
            _game = GameController.Instance;
            Assert.NotNull(_game);
            _tuning = Object.Instantiate(_game.tuning);
            _tuning.hazardChanceStart = _tuning.hazardChanceMax = 0f;
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = 0f;
            _tuning.rampChance = 0f;
            _tuning.startSpeed = 20f;
            _tuning.copFirstDelay = 0.6f;
            _tuning.heliFirstShotSeconds = 30f; // the picture is about the police car
            _game.tuning = _game.rider.tuning = _game.track.tuning = _tuning;
            var turns = _game.GetComponent<FakeTurnDirector>();
            if (turns != null) turns.tuning = _tuning;
            yield return null;
            _race = _game.GetComponent<RaceDirector>();
            _items = _game.GetComponent<RaceItems>();
            Assert.NotNull(_race);
            Assert.NotNull(_items);

            _camera = Camera.main;
            _rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4, name = "ItemShots" };
            _camera.targetTexture = _rt;
            Transform hudCanvas = _game.hud.transform.Find("HUD Canvas");
            Assert.NotNull(hudCanvas);
            _canvas = hudCanvas.GetComponent<Canvas>();
            _oldMode = _canvas.renderMode;
            _game.EnterReady();
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_canvas != null) { _canvas.renderMode = _oldMode; _canvas.worldCamera = null; }
            if (_camera != null) _camera.targetTexture = null;
            if (_rt != null) { _rt.Release(); Object.Destroy(_rt); }
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            if (_tuning != null) Object.Destroy(_tuning);
            _hub?.SetKeyboardEnabled(_oldKeyboard ? "1" : "0");
            _hub?.Mixer.Reset();
        }

        void Say(string line) => _hub.Receive(Board, line);
        void Hear(string payload) => _hub.Receive(Board, "rx:" + payload);

        // The fake radio: BEN and CHLOE ride ahead of AMY, DAN behind.
        void Radio()
        {
            float now = Time.unscaledTime;
            if (now >= _nextRace) { Say("race:AMY:12"); _nextRace = now + 1f; }
            if (now < _nextBots) return;
            _nextBots = now + 0.25f;
            if (_race.State == RaceState.Lobby || _race.State == RaceState.Countdown)
            {
                foreach (string name in new[] { "BEN", "CHLOE", "DAN" }) Hear("p:" + name + ":0:0");
                return;
            }
            float mine = _game.rider.Distance, spread = Mathf.Clamp01(_race.RaceTime / 3f);
            Hear("p:BEN:" + Mathf.RoundToInt(mine + 17f * spread) + ":2");
            Hear("p:CHLOE:" + Mathf.RoundToInt(mine + 44f * spread) + ":-2");
            Hear("p:DAN:" + Mathf.RoundToInt(Mathf.Max(0f, mine - 14f * spread)) + ":-3");
        }

        IEnumerator Run(float seconds)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end) { Radio(); yield return null; }
        }

        IEnumerator Until(System.Func<bool> done, float timeout)
        {
            float end = Time.unscaledTime + timeout;
            while (!done() && Time.unscaledTime < end) { Radio(); yield return null; }
        }

        [UnityTest, Explicit("Renders review images of the item race into art-src/race/previews")]
        public IEnumerator Screenshots_ItemRace()
        {
            Say("race:AMY:12");
            yield return Run(1.5f);
            Assert.AreEqual(RaceState.Lobby, _race.State);

            Hear("go:4821:uwa:1500");
            yield return Run(RaceDirector.CountdownSeconds + 0.3f);
            Assert.AreEqual(RaceState.Racing, _race.State);
            _game.rider.PassThroughUntil = Time.time + 1e6f; // the review ride never crashes into the street
            // The left lane: the street's own ramps and steps are in the middle here.
            Vector3 lane = _game.rider.transform.position;
            _game.rider.transform.position = new Vector3(-2.6f, _game.track.SurfaceHeightAt(-2.6f, lane.z), lane.z);
            // Third of four: the middle table. A seed whose first item is a rocket.
            for (int seed = 1; seed < 200; seed++)
                if (RaceItems.PickItem(3, 4, new System.Random(seed)) == "rocket") { _items.SeedRandom(seed); break; }

            // The first row of boxes, 150 m after the start.
            yield return Until(() => _game.rider.Distance > 142f, 20f);
            yield return Shot("items-01-boxes.png");
            yield return Until(() => _items.Held != null, 5f);
            Assert.AreEqual("rocket", _items.Held, "place " + _race.MyPlace(out int n) + " of " + n);
            yield return Run(0.12f);
            yield return Shot("items-02-got-a-rocket.png");

            // "use": the rocket goes for BEN, just ahead.
            yield return Run(0.8f);
            Say("use");
            yield return Run(0.22f);
            yield return Shot("items-03-rocket-fired.png");
            Assert.AreEqual("BEN", _items.LastTarget);

            // Shield, and a bolt that it stops.
            _items.boxesOn = false; // from here the review hands the items out itself
            yield return Run(1.4f);
            Assert.IsTrue(_items.Give("shield"));
            Say("use");
            yield return Run(0.9f);
            yield return Shot("items-04-shield.png");
            Hear("i:BEN:bolt:1");
            yield return Run(0.2f);
            yield return Shot("items-05-blocked.png");

            // Boost.
            yield return Run(1.6f);
            _items.Give("boost");
            Say("use");
            yield return Run(0.9f);
            yield return Shot("items-06-boost.png");
            yield return Run(RaceItems.BoostSeconds);

            // Oil: BEN drops some ahead of me in my lane, CHLOE one lane over.
            _items.Give("oil"); // one in the slot too
            float d = _game.rider.Distance;
            int oilLane = Mathf.RoundToInt(_game.rider.transform.position.x);
            Hear("i:BEN:oil:" + Mathf.RoundToInt(d + 40f) + ":" + oilLane + ":2");
            Hear("i:CHLOE:oil:" + Mathf.RoundToInt(d + 52f) + ":" + (oilLane + 3) + ":1");
            yield return Until(() => _game.rider.Distance > d + 31f, 5f);
            yield return Shot("items-07-oil-ahead.png");
            yield return Until(() => _game.rider.ItemSlowed, 5f);
            Assert.IsTrue(_game.rider.ItemSlowed);
            yield return Run(0.25f);
            yield return Shot("items-08-slip.png");

            // A bolt from CHLOE: the flash, then half speed with the bolt over the rider.
            yield return Run(3f);
            Hear("i:CHLOE:bolt:2");
            yield return Run(0.1f);
            yield return Shot("items-09-bolt.png");
            yield return Run(1.2f);
            yield return Shot("items-10-slowed-by-the-bolt.png");

            // A rocket from DAN: the warning with the target ring and the rocket diving in, then BOOM!
            yield return Run(3f);
            Hear("i:DAN:rocket:AMY:1");
            yield return Until(() => _items.RocketIn <= 0.3f, 2f);
            yield return Shot("items-11-rocket-incoming.png");
            yield return Until(() => !_game.rider.Running, 2f);
            Assert.IsFalse(_game.rider.Running);
            yield return Run(0.3f);
            yield return Shot("items-12-boom.png");
            yield return Until(() => _game.rider.Running, 3f);
            _game.rider.PassThroughUntil = Time.time + 1e6f; // back on the scooter: again nothing in the street stops the review
            Vector3 middle = _game.rider.transform.position;
            _game.rider.transform.position = new Vector3(0f, _game.track.SurfaceHeightAt(0f, middle.z), middle.z);

            // Somebody else's fight: a line on the screen only.
            yield return Run(1f);
            Hear("i:DAN:rocket:CHLOE:2");
            yield return Run(0.3f);
            yield return Shot("items-13-others.png");

            // The police: BEN sent them. Three stars under the race bar; the police car pulls up alongside and, with
            // nowhere to go, that is ARRESTED: in a race the same 3 s stop as a crash.
            yield return Run(1.6f);
            yield return Until(() => !_game.rider.Airborne && _game.rider.HeightAboveRoad < 0.05f, 8f); // not while flying off a ramp
            yield return Run(0.4f);
            PoliceChase police = _game.GetComponent<PoliceChase>();
            police.ForceNextTactic(PoliceChase.Tactic.Alongside);
            Hear("i:BEN:police:AMY:3");
            yield return Run(0.4f);
            Assert.AreEqual(RaceItems.PoliceStars, police.Wanted);
            yield return Shot("items-14-police-sent.png");
            yield return Until(() => police.CopOnRoad && police.CopGap < -1.2f, 12f); // level with the rider: in front of the chase camera
            yield return Shot("items-15-police-car.png");
            yield return Until(() => !_game.rider.Running, 6f);
            if (!_game.rider.Running)
            {
                yield return Run(0.5f);
                yield return Shot("items-16-arrested.png");
                Assert.AreEqual(RunState.Running, _game.State, "arrested in a race: the ride goes on");
            }
        }

        IEnumerator Shot(string file)
        {
            yield return null; // the HUD lays out for this frame
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            try
            {
                _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                _canvas.worldCamera = _camera;
                _canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                _game.hud.RelayoutRaceTags();
                Canvas.ForceUpdateCanvases();
                var request = new RenderPipeline.StandardRequest { destination = _rt };
                for (int pass = 0; pass < 2; pass++)
                    if (RenderPipeline.SupportsRenderRequest(_camera, request)) RenderPipeline.SubmitRenderRequest(_camera, request); else _camera.Render();
                RenderTexture.active = _rt;
                image.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
                image.Apply();
                string folder = System.IO.Path.Combine(Application.dataPath, "../art-src/race/previews");
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, file), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                _canvas.renderMode = _oldMode;
                _canvas.worldCamera = null;
                Object.Destroy(image);
            }
        }
    }
}
