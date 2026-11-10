using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityScooter.Devices;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>Review images of MCIVER, CLAISEBROOK and EAST PERTH (east line art agent 1) from the real chase camera on
    /// the real route: the ride starts in the given district (startDistrict on a runtime copy of the tuning) and goes
    /// forward through its chunks and exit into the next one, so DistrictDirector applies the look, sky and far scenery as
    /// in the game (no asset is changed). The generator recycles what is behind the rider, so the shots are taken in route
    /// order only. Side views turn the chase camera to check the facades and the far scenery band. Images go to
    /// art-src/districts/&lt;id&gt;/previews. Run DistrictSetup first (the theme must list the three districts).</summary>
    public class DistrictEast1Screenshots
    {
        Scene _main;
        GameController _game;
        GameTuning _tuning;
        TrackTheme _original;

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
            _original = _game.track.theme;
            _tuning = Object.Instantiate(_game.tuning);
            _tuning.fakeTurns = false;
            _tuning.boulevardSections = false;
            _tuning.hazardChanceStart = _tuning.hazardChanceMax = 0f;
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = .35f;
            _tuning.safeStartDistance = 20f;
            _game.tuning = _game.rider.tuning = _game.track.tuning = _tuning;
            var turns = _game.GetComponent<FakeTurnDirector>();
            if (turns != null) turns.tuning = _tuning;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_game != null && _original != null) _game.track.theme = _original;
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            if (_tuning != null) Object.Destroy(_tuning);
            DeviceHub.Instance?.Mixer.Reset();
        }

        sealed class ShotSpec
        {
            public readonly string Id, File, Chunk;
            public readonly float X, Z, Yaw, Pitch, Lift;
            public ShotSpec(string id, string file, string chunk, float x, float z, float yaw, float pitch, float lift) { Id = id; File = file; Chunk = chunk; X = x; Z = z; Yaw = yaw; Pitch = pitch; Lift = lift; }
            public bool Turned => Mathf.Abs(Yaw) > .01f || Mathf.Abs(Pitch) > .01f || Mathf.Abs(Lift) > .01f;
        }

        static ShotSpec M(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 1.5f) => new ShotSpec("mciver", file, "CHN_Mciver_" + chunk, x, z, yaw, pitch, yaw == 0f && pitch == 0f ? 0f : lift);
        static ShotSpec C(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 1.5f) => new ShotSpec("claisebrook", file, "CHN_Claisebrook_" + chunk, x, z, yaw, pitch, yaw == 0f && pitch == 0f ? 0f : lift);
        static ShotSpec E(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 1.5f) => new ShotSpec("eastperth", file, "CHN_Eastperth_" + chunk, x, z, yaw, pitch, yaw == 0f && pitch == 0f ? 0f : lift);

        Dictionary<string, float> Scan(float from)
        {
            var starts = new Dictionary<string, float>();
            TrackGenerator track = _game.track;
            for (float z = Mathf.Max(0f, from); z < from + track.buildAhead + 40f; z += 5f)
            {
                RoadChunk c = track.ChunkAt(z, out float start);
                if (c == null || start < from) continue;
                string key = c.name.Replace("(Clone)", "").Trim();
                if (!starts.ContainsKey(key)) starts[key] = start;
            }
            return starts;
        }

        // Rides forward from the start district, taking each shot when its chunk is built (route order, never back).
        IEnumerator RideForward(string startDistrict, List<ShotSpec> shots)
        {
            Assert.IsTrue(_original.HasDistricts && _original.districts.Any(d => d != null && d.id == startDistrict), "run DistrictSetup: the theme lists " + startDistrict);
            _tuning.startDistrict = startDistrict;
            _game.EnterReady();
            yield return null;
            yield return null;
            var pending = new List<ShotSpec>(shots);
            float riderZ = 0f;
            int taken = 0;
            for (int step = 0; step < 60 && pending.Count > 0; step++)
            {
                float stepStart = riderZ;
                Dictionary<string, float> starts = Scan(riderZ - 25f);
                if (step % 3 == 0) Debug.Log("[East1Shots] at " + riderZ + ": " + string.Join(", ", starts.Select(kv => kv.Key + "@" + kv.Value)));
                foreach (ShotSpec s in pending.Where(s => starts.ContainsKey(s.Chunk)).OrderBy(s => starts[s.Chunk] + s.Z).ToList())
                {
                    float z = Mathf.Max(riderZ, starts[s.Chunk] + s.Z);
                    pending.Remove(s);
                    taken++;
                    yield return Shot(s, z);
                    riderZ = z;
                }
                // the next window overlaps this one (counted from where this step started, not from the last shot)
                riderZ = Mathf.Max(riderZ, stepStart + _game.track.buildAhead - 110f);
                _game.rider.ResetRider(new Vector3(0f, 0f, riderZ));
                for (int i = 0; i < 4; i++) yield return null;
            }
            foreach (ShotSpec s in pending) Debug.LogWarning("[East1Shots] " + s.Id + ": no " + s.Chunk + " for " + s.File);
            Assert.That(taken, Is.GreaterThanOrEqualTo(shots.Count - 3), "most review shots found their chunk");
        }

        IEnumerator Shot(ShotSpec shot, float z)
        {
            var follow = Camera.main.GetComponent<FollowCamera>();
            _game.rider.ResetRider(new Vector3(shot.X, 0f, z));
            yield return null;
            yield return null;
            follow.SnapToTarget();
            yield return null;
            follow.SnapToTarget();
            for (int i = 0; i < 3; i++) yield return null; // the director moves the far scenery and blends the look
            follow.SnapToTarget();
            Camera cam = Camera.main;
            if (!shot.Turned) { Capture(cam, shot.Id, shot.File); yield break; }
            // Side view: the chase camera turned (same position, FOV and rendering), then put back.
            Vector3 pos = cam.transform.position;
            Quaternion rot = cam.transform.rotation;
            cam.transform.SetPositionAndRotation(pos + Vector3.up * shot.Lift, Quaternion.Euler(shot.Pitch, shot.Yaw, 0f));
            Capture(cam, shot.Id, shot.File);
            cam.transform.SetPositionAndRotation(pos, rot);
        }

        [UnityTest, Explicit("Renders review images of MCIVER into art-src/districts/mciver/previews")]
        public IEnumerator Screenshots_Mciver_FromTheChaseCamera()
        {
            yield return RideForward("mciver", new List<ShotSpec>
            {
                M("01-hospital.png", "Hospital", 0f, 0f),
                M("02-hospital-ward-block.png", "Hospital", -3f, 4f, 38f, -14f),
                M("03-hospital-north-block.png", "Hospital", 3f, 4f, -38f, -12f),
                M("04-heritage.png", "Heritage", 1.5f, 0f),
                M("05-kirkman-house.png", "Heritage", 5f, 0f, 42f, -4f, .5f),
                M("06-fig-and-admin.png", "Heritage", 5.5f, 17f, 35f, -8f, .5f),
                M("07-station.png", "Station", -1.5f, 0f),
                M("08-station-platforms.png", "Station", -6f, 8f, -42f, 2f, .8f),
                M("09-shop-terraces.png", "Station", 5f, 6f, 40f, -4f, .5f),
                M("10-square.png", "Square", 1.5f, 0f),
                M("11-square-playground.png", "Square", 6f, 8f, 52f, 0f, 1f),
                M("12-railway-side.png", "Square", -5.5f, 10f, -50f, 2f, 1f),
                M("13-works-ramp.png", "Works", 0f, 1f),
                M("14-works-trench.png", "Works", 2f, 12f, 0f, 12f, 2.5f),
                M("15-perth-mint.png", "Works", 5f, 22f, 44f, -4f, .5f),
                M("16-exit.png", "Exit", 0f, 4f),
                M("17-sidewalk-ride.png", "Station", 6f, 20f),
            });
        }

        [UnityTest, Explicit("Renders review images of CLAISEBROOK into art-src/districts/claisebrook/previews")]
        public IEnumerator Screenshots_Claisebrook_FromTheChaseCamera()
        {
            yield return RideForward("claisebrook", new List<ShotSpec>
            {
                C("01-station.png", "Station", 0f, 0f),
                C("02-station-footbridge.png", "Station", -5f, 4f, -40f, -6f, 1f),
                C("03-depot.png", "Depot", -1.5f, 0f),
                C("04-depot-yard.png", "Depot", -6f, 10f, -48f, 0f, 1.5f),
                C("05-boans-warehouse.png", "Depot", 5f, 4f, 40f, -6f, .5f),
                C("06-royal-street.png", "RoyalStreet", 0f, 0f),
                C("07-townhouses.png", "RoyalStreet", -5f, 8f, -40f, -4f, .5f),
                C("08-royal-street-right.png", "RoyalStreet", 5f, 8f, 40f, -4f, .5f),
                C("09-cove.png", "Cove", 1.5f, 0f),
                C("10-cove-water.png", "Cove", 6f, 8f, 48f, 4f, 1.5f),
                C("11-trafalgar-bridge.png", "Cove", 5f, 20f, 30f, -4f, 1f),
                C("12-canal-bridge.png", "Canal", 0f, 0f),
                C("13-canal-gap.png", "Canal", 2f, 10f, 0f, 12f, 2.5f),
                C("14-victoria-gardens.png", "Canal", 5f, 24f, 45f, -2f, 1f),
                C("15-exit.png", "Exit", 0f, 4f),
                C("16-promenade-sidewalk.png", "Cove", -6f, 22f),
            });
        }

        [UnityTest, Explicit("Renders review images of EAST PERTH into art-src/districts/eastperth/previews")]
        public IEnumerator Screenshots_EastPerth_FromTheChaseCamera()
        {
            yield return RideForward("eastperth", new List<ShotSpec>
            {
                E("01-terminal.png", "Terminal", 0f, 0f),
                E("02-indian-pacific.png", "Terminal", -6f, 6f, -45f, -2f, 1f),
                E("03-concourse.png", "Concourse", -1.5f, 0f),
                E("04-transport-centre.png", "Concourse", -6f, 6f, -50f, -10f, 1.5f),
                E("05-east-parade.png", "Concourse", 5f, 6f, 42f, -4f, .5f),
                E("06-platforms.png", "Platforms", 0f, 0f),
                E("07-island-platform.png", "Platforms", -6f, 10f, -45f, 0f, 1f),
                E("08-foreshore.png", "Foreshore", 1.5f, 0f),
                E("09-stadium-and-bridge.png", "Foreshore", 5f, 4f, 58f, -4f, 2f),
                E("10-river-ahead-right.png", "Foreshore", 4f, 20f, 30f, -2f, 2f),
                E("11-channel-gap.png", "Foreshore", 0f, 9f, 0f, 12f, 2.5f),
                E("12-power-station.png", "PowerStation", 0f, 0f),
                E("13-power-station-side.png", "PowerStation", 5f, 6f, 42f, -8f, 1f),
                E("14-freeway-bridges.png", "PowerStation", -4f, 14f, -30f, -8f, 1f),
                E("15-exit.png", "Exit", 0f, 4f),
                E("16-horizon-right.png", "Terminal", 3f, 24f, 70f, -2f, 2f),
                E("17-river-approach.png", "Platforms", 2f, 22f, 28f, -4f, 1.5f),
            });
        }

        static void Capture(Camera camera, string id, string file)
        {
            const int w = 1600, h = 900;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            RenderTexture previous = RenderTexture.active, oldTarget = camera.targetTexture;
            Texture2D image = null;
            try
            {
                camera.targetTexture = rt;
                // camera.Render, not SubmitRenderRequest: only this runs beginCameraRendering, where the backdrop and the
                // district director set the sky and the city curl for this view.
                for (int pass = 0; pass < 2; pass++) camera.Render();
                RenderTexture.active = rt;
                image = new Texture2D(w, h, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                image.Apply();
                string folder = System.IO.Path.Combine(Application.dataPath, "../art-src/districts/" + id + "/previews");
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
