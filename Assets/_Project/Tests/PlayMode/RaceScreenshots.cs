using System.Collections;
using System.Globalization;
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
    /// Review images of the radio race (v0.4) in the real Main scene, HUD included: lobby, countdown, mid-race with
    /// ghosts, the FINISH arch ahead, FINISH!, results. A fake "radio" plays five other boards (BEN, CHLOE, DAN, EVE whose
    /// signal is lost, FRED who sends an impossible position) through DeviceHub.Receive, as the student's board would.
    /// The camera renders into a 1600x900 texture for the whole test and the HUD canvas is switched to that camera, so the
    /// overlay is in the images. Images go to art-src/race/previews.
    /// </summary>
    public class RaceScreenshots
    {
        const string Board = "usb1";
        Scene _main;
        GameController _game;
        GameTuning _tuning;
        RaceDirector _race;
        DeviceHub _hub;
        RenderTexture _rt;
        Canvas _canvas;
        Camera _camera;
        RenderMode _oldMode;
        bool _oldKeyboard;

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
            _tuning.startSpeed = 26f; // a short race for the review: 300 m in about 10 s
            _game.tuning = _game.rider.tuning = _game.track.tuning = _tuning;
            var turns = _game.GetComponent<FakeTurnDirector>();
            if (turns != null) turns.tuning = _tuning;
            yield return null;
            _race = _game.GetComponent<RaceDirector>();
            Assert.NotNull(_race);

            _camera = Camera.main;
            _rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4, name = "RaceShots" };
            _camera.targetTexture = _rt;
            // At each shot the HUD canvas is drawn by the camera (as CityAudioTests captures its panel).
            Transform hudCanvas = _game.hud.transform.Find("HUD Canvas"); // not the first canvas below GameSystems (the track has some)
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

        static string F(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);

        // ---- the fake radio: what AMY's board would pass on from the other boards
        float _nextRace, _nextBots, _raceClock;
        bool _eveGone, _fredLied;
        readonly System.Collections.Generic.HashSet<string> _finished = new System.Collections.Generic.HashSet<string>();
        readonly System.Collections.Generic.Dictionary<string, float> _nextLobby = new System.Collections.Generic.Dictionary<string, float>();

        struct Bot { public string Name; public float From, To, Lane; }

        static readonly Bot[] Bots =
        {
            new Bot { Name = "BEN", From = 16f, To = 14f, Lane = 2.3f },
            new Bot { Name = "CHLOE", From = 34f, To = -24f, Lane = -2.1f },
            new Bot { Name = "DAN", From = 7f, To = -40f, Lane = -2.6f },
            new Bot { Name = "EVE", From = 52f, To = 52f, Lane = 0.9f },
            new Bot { Name = "FRED", From = 26f, To = 8f, Lane = -0.4f },
        };

        void Radio(bool racing)
        {
            float now = Time.unscaledTime;
            if (now >= _nextRace) { Say("race:AMY:12"); _nextRace = now + 1f; }
            if (!racing && _race.State == RaceState.Lobby)
            {
                // Lobby boards send pos:0:0 once a second; DAN's board is far away (a weak signal: most packets lost).
                foreach (Bot b in Bots)
                {
                    _nextLobby.TryGetValue(b.Name, out float due);
                    if (now < due) continue;
                    Say("rx:p:" + b.Name + ":0:0");
                    _nextLobby[b.Name] = now + (b.Name == "DAN" ? 2.6f : 1f) + Random.Range(-0.05f, 0.05f);
                }
                return;
            }
            if (now < _nextBots) return;
            _nextBots = now + 0.25f;
            if (!racing)
            {
                foreach (Bot b in Bots) Say("rx:p:" + b.Name + ":0:0"); // countdown: 4 a second
                return;
            }
            float mine = _game.rider.Distance, p = Mathf.Clamp01(mine / Mathf.Max(1f, _race.Metres));
            foreach (Bot b in Bots)
            {
                if (_finished.Contains(b.Name)) { Say("rx:f:" + b.Name + ":" + F(FinishOf(b.Name))); continue; }
                if (b.Name == "EVE" && _raceClock > 2.5f) { _eveGone = true; continue; } // EVE's board went quiet: grey, then gone
                if (b.Name == "FRED" && !_fredLied && _raceClock > 3f) { _fredLied = true; Say("rx:p:FRED:99999:0"); continue; }
                // Everyone leaves the start line together, then spreads out (a jump to +34 m at GO would rightly get a "?").
                float d = Mathf.Max(0f, mine + Mathf.Lerp(b.From, b.To, p) * Mathf.Clamp01(_raceClock / 2.5f));
                Say("rx:p:" + b.Name + ":" + Mathf.RoundToInt(d) + ":" + Mathf.RoundToInt(b.Lane));
            }
        }

        float FinishOf(string name)
        {
            float mine = _race.FinishTime > 0f ? _race.FinishTime : _race.RaceTime;
            switch (name) { case "BEN": return mine - 1.6f; case "CHLOE": return mine + 1.9f; case "DAN": return mine + 3.4f; default: return 3f; }
        }

        IEnumerator Run(float seconds, bool racing)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end)
            {
                Radio(racing);
                yield return null;
            }
        }

        [UnityTest, Explicit("Renders review images of the radio race into art-src/race/previews")]
        public IEnumerator Screenshots_RadioRace()
        {
            // Lobby: AMY's board says race:AMY:12 and passes on what it hears.
            Say("race:AMY:12");
            yield return Run(3.2f, false);
            Assert.AreEqual(RaceState.Lobby, _race.State);
            yield return Shot("01-lobby.png");

            // go: 3, 2, 1 with the others on the start grid
            Say("rx:go:4821:uwa:300");
            yield return Run(1.4f, false);
            yield return Shot("02-countdown.png");
            yield return Run(RaceDirector.CountdownSeconds - 1.4f + 0.3f, false);
            Assert.AreEqual(RaceState.Racing, _race.State);
            _game.rider.PassThroughUntil = Time.time + 1e6f; // the review ride never crashes
            float start = Time.unscaledTime;

            // Mid-race: BEN and FRED ahead, CHLOE, DAN beside, EVE's signal lost, FRED "?" after an impossible jump.
            while (Time.unscaledTime - start < 4.6f) { _raceClock = Time.unscaledTime - start; Radio(true); yield return null; }
            yield return Shot("03-mid-race.png");

            // The FINISH arch ahead.
            while (_game.rider.Distance < 228f && _race.State == RaceState.Racing) { _raceClock = Time.unscaledTime - start; Radio(true); yield return null; }
            yield return Shot("04-finish-ahead.png");

            while (_race.State == RaceState.Racing && Time.unscaledTime - start < 40f) { _raceClock = Time.unscaledTime - start; Radio(true); yield return null; }
            Assert.AreEqual(RaceState.Finished, _race.State);
            _finished.Add("BEN");
            _finished.Add("FRED");
            yield return Run(0.8f, true);
            yield return Shot("05-finish.png");
            float finishedAt = Time.unscaledTime;
            while (_race.State != RaceState.Results && Time.unscaledTime - finishedAt < 35f)
            {
                if (Time.unscaledTime - finishedAt > 1.9f) _finished.Add("CHLOE");
                if (Time.unscaledTime - finishedAt > 3.4f) _finished.Add("DAN");
                Radio(true);
                yield return null;
            }
            Assert.AreEqual(RaceState.Results, _race.State);
            yield return Run(0.4f, true);
            yield return Shot("06-results.png");
            Assert.IsTrue(_eveGone);
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
