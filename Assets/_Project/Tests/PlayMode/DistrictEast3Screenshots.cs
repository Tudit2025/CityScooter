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
    /// <summary>Review images of BAYSWATER, ASHFIELD and BASSENDEAN (east art agent 3) from the real chase camera in the
    /// real Main scene. The three districts are put on a runtime copy of the theme (no asset is changed and DistrictSetup
    /// is not needed), the ride starts in one of them and goes forward through entry, chunks and exit of all three, so
    /// DistrictDirector applies each look, the hidden skyline and the far scenery as in the game. The generator recycles
    /// what is behind the rider, so shots are taken in route order only. Shots with a yaw turn the chase camera to check
    /// the facades and the horizon band. Images go to art-src/districts/&lt;id&gt;/previews.</summary>
    public class DistrictEast3Screenshots
    {
        Scene _main;
        GameController _game;
        GameTuning _tuning;
        TrackTheme _original, _theme;

        static readonly string[] Ids = { "bayswater", "ashfield", "bassendean" };

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
            _theme = Object.Instantiate(_original);
            _theme.districts = new[] { Load("Bayswater"), Load("Ashfield"), Load("Bassendean") };
            _game.track.theme = _theme;
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
            if (_theme != null) Object.Destroy(_theme);
            DeviceHub.Instance?.Mixer.Reset();
        }

        static District Load(string name)
        {
#if UNITY_EDITOR
            string path = "Assets/_Project/Data/Districts/District_" + name + ".asset";
            var d = UnityEditor.AssetDatabase.LoadAssetAtPath<District>(path);
            Assert.NotNull(d, path + " (run East3Builder.BuildBatch)");
            return d;
#else
            Assert.Ignore("Editor only");
            return null;
#endif
        }

        sealed class ShotSpec
        {
            public readonly string Id, File, Chunk;
            public readonly float X, Z, Yaw, Pitch, Lift;
            public ShotSpec(string id, string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 1.5f)
            { Id = id; File = file; Chunk = chunk; X = x; Z = z; Yaw = yaw; Pitch = pitch; Lift = lift; }
            public bool Turned => Mathf.Abs(Yaw) > .01f || Mathf.Abs(Pitch) > .01f;
        }

        static ShotSpec Bay(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 1.5f) => new ShotSpec("bayswater", file, "CHN_Bayswater_" + chunk, x, z, yaw, pitch, lift);
        static ShotSpec Ash(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 1.5f) => new ShotSpec("ashfield", file, "CHN_Ashfield_" + chunk, x, z, yaw, pitch, lift);
        static ShotSpec Bas(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 1.5f) => new ShotSpec("bassendean", file, "CHN_Bassendean_" + chunk, x, z, yaw, pitch, lift);

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
                if (step % 3 == 0) Debug.Log("[East3Shots] at " + riderZ + ": " + string.Join(", ", starts.Select(kv => kv.Key + "@" + kv.Value)));
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
            foreach (ShotSpec s in pending) Debug.LogWarning("[East3Shots] " + s.Id + ": no " + s.Chunk + " for " + s.File);
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
            if (!shot.Turned) { Capture(cam, shot.Id, shot.File); yield break; }
            // Facade / horizon check: the chase camera turned (same FOV and rendering), a little higher, then put back.
            Vector3 pos = cam.transform.position;
            Quaternion rot = cam.transform.rotation;
            cam.transform.SetPositionAndRotation(pos + Vector3.up * shot.Lift, Quaternion.Euler(shot.Pitch, shot.Yaw, 0f));
            Capture(cam, shot.Id, shot.File);
            cam.transform.SetPositionAndRotation(pos, rot);
        }

        [UnityTest, Explicit("Renders review images of BAYSWATER into art-src/districts/bayswater/previews")]
        public IEnumerator Screenshots_Bayswater_FromTheChaseCamera()
        {
            yield return RideForward("bassendean", new List<ShotSpec>
            {
                Bay("01-entry.png", "Entry", 0f, 4f),
                Bay("02-shops-and-embankment.png", "Shops", 1.5f, 0f),
                Bay("03-shopfronts-close.png", "Shops", 5.5f, 12f, 38f, 4f, .6f),
                Bay("04-viaduct.png", "Viaduct", 0f, -2f),
                Bay("05-viaduct-side.png", "Viaduct", 2.6f, 10f, -52f, -8f, .5f),
                Bay("06-hotel.png", "Viaduct", -2.6f, 4f, 40f, -2f, .6f),
                Bay("07-station-king-william-st.png", "Station", 0f, -6f),
                Bay("08-station-concourse.png", "Station", 2.6f, 10f, -55f, -6f, .5f),
                Bay("09-old-bridge-memorial.png", "Station", -5.5f, 27f, -40f, 2f, .2f),
                Bay("10-post-office.png", "Station", -2.6f, 8f, 42f, 0f, .6f),
                Bay("11-plaza-kicker.png", "Plaza", 0f, 2f),
                Bay("12-plaza-east-entry.png", "Plaza", 2.6f, 12f, -55f, -6f, .5f),
                Bay("13-junction.png", "Junction", 0f, 0f),
                Bay("14-junction-side.png", "Junction", 2.6f, 10f, -50f, -8f, 1.5f),
                Bay("15-horizon-right.png", "Junction", 2f, 24f, 55f, 2f, 2.5f),
                Bay("16-exit.png", "Exit", 0f, 6f),
                Bay("17-sidewalk-ride.png", "Viaduct", -5.5f, 20f),
            });
        }

        [UnityTest, Explicit("Renders review images of ASHFIELD into art-src/districts/ashfield/previews")]
        public IEnumerator Screenshots_Ashfield_FromTheChaseCamera()
        {
            yield return RideForward("bayswater", new List<ShotSpec>
            {
                Ash("01-entry.png", "Entry", 0f, 4f),
                Ash("02-station.png", "Station", 1.5f, -2f),
                Ash("03-station-side.png", "Station", 2.6f, 8f, -50f, -2f, .8f),
                Ash("04-reserve-figs.png", "Station", -2f, 6f, 40f, 0f, .8f),
                Ash("05-state-housing.png", "Reserve", 0f, 0f),
                Ash("06-cottages-close.png", "Reserve", -2.6f, 12f, 42f, 2f, .6f),
                Ash("07-industry.png", "Reserve", 2.6f, 6f, -50f, -3f, 1.5f),
                Ash("08-flats.png", "Flats", 0f, 0f),
                Ash("09-flats-pools-birds.png", "Flats", -2f, 10f, 45f, 6f, .8f),
                Ash("10-boardwalk-drain.png", "Drain", 0f, -3f),
                Ash("11-drain-side.png", "Drain", 5.5f, 9f, 35f, 12f, 1.2f),
                Ash("12-paperbark-tunnel.png", "Paperbarks", 0f, 0f),
                Ash("13-viewing-platform.png", "Paperbarks", -2f, 14f, 50f, 6f, .8f),
                Ash("14-horizon-right.png", "Flats", 2f, 26f, 60f, 0f, 3f),
                Ash("15-horizon-left.png", "Paperbarks", -2f, 30f, -60f, 0f, 3f),
                Ash("16-exit.png", "Exit", 0f, 6f),
                Ash("17-verge-ride.png", "Flats", 5.6f, 18f),
            });
        }

        [UnityTest, Explicit("Renders review images of BASSENDEAN into art-src/districts/bassendean/previews")]
        public IEnumerator Screenshots_Bassendean_FromTheChaseCamera()
        {
            yield return RideForward("ashfield", new List<ShotSpec>
            {
                Bas("01-entry.png", "Entry", 0f, 4f),
                Bas("02-station.png", "Station", 0f, -2f),
                Bas("03-station-overpass.png", "Station", 2.6f, 12f, -48f, -6f, .8f),
                Bas("04-corner-building.png", "Station", -2.6f, 2f, 40f, 0f, .6f),
                Bas("05-museum.png", "Museum", 0f, -3f),
                Bas("06-steam-locomotive.png", "Museum", 2.6f, 22f, -55f, 0f, .2f),
                Bas("07-library.png", "Museum", -2.6f, 6f, 42f, 0f, .6f),
                Bas("08-old-perth-road.png", "OldPerthRd", 0f, 0f),
                Bas("09-hotel.png", "OldPerthRd", -2.6f, 4f, 40f, -2f, .6f),
                Bas("10-padbury-buildings.png", "OldPerthRd", 2.6f, 16f, -42f, -2f, .6f),
                Bas("11-war-memorial.png", "Memorial", 0f, 0f),
                Bas("12-memorial-close.png", "Memorial", 2.6f, 14f, -55f, 0f, .4f),
                Bas("13-oval.png", "Oval", 0f, -2f),
                Bas("14-oval-gate.png", "Oval", -2.6f, 4f, 48f, 0f, .4f),
                Bas("15-grandstands-goal-posts.png", "Oval", -2.6f, 18f, 35f, -6f, 1.5f),
                Bas("16-horizon-right.png", "Exit", 2f, 12f, 60f, 0f, 3f),
                Bas("17-exit.png", "Exit", 0f, 6f),
                Bas("18-footpath-ride.png", "OldPerthRd", 5.5f, 22f),
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
