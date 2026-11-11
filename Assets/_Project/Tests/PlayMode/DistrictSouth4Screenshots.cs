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
    /// <summary>Review images of ROCKINGHAM and WARNBRO (south art agent 4) from the real chase camera on the real route:
    /// the ride starts in the district before ROCKINGHAM (startDistrict on a runtime copy of the tuning) and goes forward
    /// through ROCKINGHAM's entry, its chunks and exit, into WARNBRO and out of it, so DistrictDirector applies the look,
    /// sky and far scenery as in the game (no asset is changed). The generator recycles what is behind the rider, so the
    /// shots are taken in route order only. Two horizon views per district turn the chase camera to the side to check
    /// the far scenery band. Images go to art-src/districts/&lt;id&gt;/previews. Run DistrictSetup first.</summary>
    public class DistrictSouth4Screenshots
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
            public readonly float X, Z, Yaw, Pitch;
            public ShotSpec(string id, string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f) { Id = id; File = file; Chunk = chunk; X = x; Z = z; Yaw = yaw; Pitch = pitch; }
            public bool Horizon => Mathf.Abs(Yaw) > .01f;
        }

        static ShotSpec R(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f) => new ShotSpec("rockingham", file, "CHN_Rockingham_" + chunk, x, z, yaw, pitch);
        static ShotSpec W(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f) => new ShotSpec("warnbro", file, "CHN_Warnbro_" + chunk, x, z, yaw, pitch);

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
            Assert.IsTrue(_original.HasDistricts && _original.districts.Any(d => d != null && d.id == "rockingham") && _original.districts.Any(d => d != null && d.id == "warnbro"),
                "run DistrictSetup: the theme lists rockingham and warnbro");
            _tuning.startDistrict = startDistrict;
            _game.EnterReady();
            yield return null;
            yield return null;
            var pending = new List<ShotSpec>(shots);
            float riderZ = 0f;
            int taken = 0;
            for (int step = 0; step < 40 && pending.Count > 0; step++)
            {
                float stepStart = riderZ;
                Dictionary<string, float> starts = Scan(riderZ - 25f);
                if (step == 0 || step % 3 == 0) Debug.Log("[South4Shots] at " + riderZ + ": " + string.Join(", ", starts.Select(kv => kv.Key + "@" + kv.Value)));
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
            foreach (ShotSpec s in pending) Debug.LogWarning("[South4Shots] " + s.Id + ": no " + s.Chunk + " for " + s.File);
            Assert.That(taken, Is.GreaterThanOrEqualTo(shots.Count - 2), "most review shots found their chunk");
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
            if (!shot.Horizon) { Capture(cam, shot.Id, shot.File); yield break; }
            // Horizon check: the chase camera turned towards the side (same position, FOV and rendering), then put back.
            Vector3 pos = cam.transform.position;
            Quaternion rot = cam.transform.rotation;
            cam.transform.SetPositionAndRotation(pos + Vector3.up * 1.5f, Quaternion.Euler(shot.Pitch, shot.Yaw, 0f));
            Capture(cam, shot.Id, shot.File);
            cam.transform.SetPositionAndRotation(pos, rot);
        }

        // v0.6 polish: a frame every 20 m through the district, packed into contact sheets (see SouthBRideThrough).
        [UnityTest, Explicit("Ride-through contact sheets of ROCKINGHAM into art-src/districts/rockingham/previews/polish")]
        public IEnumerator RideThrough_Rockingham() { yield return SouthBRideThrough.Ride(_game, _tuning, "rockingham"); }

        [UnityTest, Explicit("Ride-through contact sheets of WARNBRO into art-src/districts/warnbro/previews/polish")]
        public IEnumerator RideThrough_Warnbro() { yield return SouthBRideThrough.Ride(_game, _tuning, "warnbro"); }

        [UnityTest, Explicit("Renders review images of ROCKINGHAM and WARNBRO into art-src/districts/<id>/previews")]
        public IEnumerator Screenshots_RockinghamAndWarnbro_FromTheChaseCamera()
        {
            int before = System.Array.FindIndex(_original.districts, d => d != null && d.id == "rockingham") - 1;
            Assert.GreaterOrEqual(before, 0, "a district before Rockingham on the route");
            yield return RideForward(_original.districts[before].id, new List<ShotSpec>
            {
                R("01-entry.png", "Entry", 0f, 4f),
                R("02-norfolk-pines.png", "Pines", 1.5f, 0f),
                R("03-horizon-right.png", "Pines", 2f, 18f, 50f, 3f),
                R("04-cafe-strip.png", "Cafes", -1.5f, 2f),
                R("05-sidewalk-sea-side.png", "Cafes", 5.5f, 24f),
                R("06-playground-memorial.png", "Playground", 0f, 0f),
                R("07-horizon-left.png", "Playground", -2f, 22f, -50f, 3f),
                R("08-jetty-and-gap.png", "Jetty", 0f, 1f),
                R("09-village-green.png", "VillageGreen", 1f, -2f),
                R("10-exit.png", "Exit", 0f, 6f),
                R("11-apartments-shops.png", "Pines", -5f, 4f, -32f, 6f),
                R("12-cafe-facades.png", "Cafes", -5f, 6f, -32f, 6f),
                W("01-entry.png", "Entry", 0f, 4f),
                W("02-station.png", "Station", -1.5f, 0f),
                W("03-beach-road.png", "BeachRoad", 1.5f, 0f),
                W("04-horizon-right.png", "BeachRoad", 2f, 20f, 50f, 3f),
                W("05-surf-club.png", "SurfClub", 1.5f, -2f),
                W("06-dune-path.png", "DunePath", 0f, 0f),
                W("07-horizon-left.png", "DunePath", -2f, 20f, -50f, 3f),
                W("08-creek-kicker.png", "Creek", 0f, -3f),
                W("09-exit.png", "Exit", 0f, 6f),
                W("10-station-hall.png", "Station", -4f, 6f, -40f, 5f),
                W("11-surf-club-close.png", "SurfClub", 5f, 0f, 40f, 4f),
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
