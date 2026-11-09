using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>Review images of COCKBURN CENTRAL and AUBIN GROVE (south line, art agent 2) from the real chase camera on
    /// the real route: the ride starts in the district (startDistrict on a runtime copy of the tuning), so DistrictDirector
    /// applies its look, sky and far scenery as in the game. Shots are placed by chunk (CHN_Cockburn_ / CHN_AubinGrove_),
    /// plus horizon shots (the camera turned towards the far scenery). Images go to art-src/districts/&lt;id&gt;/previews.
    /// Needs DistrictSetup.ApplyBatch first (the theme must list both districts).</summary>
    public class DistrictSouth2Screenshots
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

        static District Load(string path)
        {
#if UNITY_EDITOR
            var d = UnityEditor.AssetDatabase.LoadAssetAtPath<District>(path);
            Assert.NotNull(d, path + " (run CockburnAubinBuilder.BuildBatch)");
            return d;
#else
            Assert.Ignore("Editor only");
            return null;
#endif
        }

        readonly struct ShotSpec
        {
            public readonly string File, Chunk;
            public readonly float X, Z, Yaw, Pitch;
            public readonly bool Horizon;
            /// <summary>A hazard pattern of the district placed just ahead of this shot (e.g. the kangaroos).</summary>
            public readonly string Pattern;
            public ShotSpec(string file, string chunk, float x, float z, string pattern = null) { File = file; Chunk = chunk; X = x; Z = z; Yaw = Pitch = 0f; Horizon = false; Pattern = pattern; }
            public ShotSpec(string file, string chunk, float x, float z, float yaw, float pitch) { File = file; Chunk = chunk; X = x; Z = z; Yaw = yaw; Pitch = pitch; Horizon = true; Pattern = null; }
        }

        // The ride only goes forward: chunks behind the rider are recycled, so every shot looks for the NEXT chunk of its
        // name ahead of the previous shot (shot.Chunk is matched against the chunk name, e.g. "CHN_Cockburn_Mall").
        float _riderZ;

        float NextChunk(string name, float fromZ)
        {
            TrackGenerator track = _game.track;
            for (float z = fromZ; z < fromZ + track.buildAhead - 10f; z += 5f)
            {
                RoadChunk c = track.ChunkAt(z, out float start);
                if (c != null && start >= fromZ - .01f && c.name.Contains(name)) return start;
            }
            return -1f;
        }

        IEnumerator Ride(District d, ShotSpec[] shots, System.Func<IEnumerator> extra = null)
        {
            Assert.IsTrue(_original.HasDistricts && System.Array.IndexOf(_original.districts, d) >= 0, "run DistrictSetup: the theme lists " + d.id);
            _tuning.startDistrict = d.id;
            _game.EnterReady();
            yield return null;
            yield return null;
            _riderZ = 0f;
            int taken = 0;
            foreach (ShotSpec shot in shots)
            {
                float start = NextChunk(shot.Chunk, Mathf.Max(0f, _riderZ - 2f));
                if (start < 0f) { Debug.LogWarning("[South2Shots] " + d.id + ": no " + shot.Chunk + " ahead of " + _riderZ); continue; }
                taken++;
                _riderZ = Mathf.Max(1f, start + shot.Z);
                Debug.Log("[South2Shots] " + d.id + " " + shot.File + ": " + shot.Chunk + "@" + start);
                HazardPattern pattern = shot.Pattern == null ? null : d.hazards.FirstOrDefault(h => h != null && h.name == shot.Pattern);
                if (pattern != null)
                {
                    _game.track.PlacePattern(pattern, _riderZ - 8f);
                    for (int i = 0; i < 10; i++) yield return null; // let them hop a little
                }
                yield return Shot(d.id, shot, _riderZ);
            }
            Assert.That(taken, Is.GreaterThanOrEqualTo(6), d.id + ": most review shots found their chunk");
            if (extra != null) yield return extra();
        }

        IEnumerator Shot(string id, ShotSpec shot, float z)
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
            Quaternion keep = cam.transform.rotation;
            // Horizon shots: the same camera turned towards the far scenery (level-ish, to one side).
            if (shot.Horizon) cam.transform.rotation = Quaternion.Euler(shot.Pitch, shot.Yaw, 0f);
            Capture(cam, id, shot.File);
            cam.transform.rotation = keep;
        }

        [UnityTest, Explicit("Renders review images of COCKBURN CENTRAL and AUBIN GROVE into art-src/districts/<id>/previews")]
        public IEnumerator Screenshots_CockburnAndAubinGrove_FromTheChaseCamera()
        {
            // Starting in a district lays its first variant at once (its entry only comes after another district's exit):
            // the entry of AUBIN GROVE is shot where COCKBURN CENTRAL ends, the exit at the end of each district.
            District cockburn = Load("Assets/_Project/Data/Districts/District_Cockburn.asset");
            yield return Ride(cockburn, new[]
            {
                new ShotSpec("01-station-footbridge.png", "CHN_Cockburn_Station", 1.2f, 0f),
                new ShotSpec("02-shopping-centre.png", "CHN_Cockburn_Mall", 1.5f, -6f),
                new ShotSpec("03-town-square.png", "CHN_Cockburn_Square", 1.5f, 0f),
                new ShotSpec("04-arc-waterslides.png", "CHN_Cockburn_Arc", 2f, 4f),
                new ShotSpec("05-apartments.png", "CHN_Cockburn_Apartments", -1.5f, 4f),
                new ShotSpec("06-horizon-left.png", "CHN_Cockburn_Station", 0f, 10f, -40f, 4f),
                new ShotSpec("07-horizon-right.png", "CHN_Cockburn_Mall", 0f, 10f, 40f, 4f),
                new ShotSpec("08-horizon-ahead.png", "CHN_Cockburn_Square", 0f, 10f, 0f, 2f),
                new ShotSpec("09-exit.png", "CHN_Cockburn_Exit", 0f, 2f),
            });
            yield return Shot("aubingrove", new ShotSpec("01-entry.png", "", 0f, 0f), EntryAfterExit("CHN_AubinGrove_Entry"));
            District aubin = Load("Assets/_Project/Data/Districts/District_AubinGrove.asset");
            yield return Ride(aubin, new[]
            {
                new ShotSpec("02-shared-path-overpass.png", "CHN_AubinGrove_Path", 1f, 0f),
                new ShotSpec("03-estate-bin-day.png", "CHN_AubinGrove_Estate", 1.5f, 0f),
                new ShotSpec("04-station-footbridge.png", "CHN_AubinGrove_Station", 1f, 0f),
                new ShotSpec("05-wetland.png", "CHN_AubinGrove_Wetland", 2.5f, 0f),
                new ShotSpec("06-drain-jump.png", "CHN_AubinGrove_Drain", 0f, 0f),
                new ShotSpec("07-horizon-left.png", "CHN_AubinGrove_Path", 0f, 10f, -40f, 4f),
                new ShotSpec("08-horizon-ahead.png", "CHN_AubinGrove_Estate", 0f, 10f, 0f, 2f),
                new ShotSpec("09-kangaroos.png", "CHN_AubinGrove_Wetland", .8f, 2f, "HZ_AubinGrove_Roos"),
                new ShotSpec("10-horizon-right.png", "CHN_AubinGrove_Wetland", 0f, 22f, 40f, 4f),
            }, () => AubinExit());
        }

        float EntryAfterExit(string name)
        {
            float start = NextChunk(name, Mathf.Max(0f, _riderZ - 2f));
            Assert.GreaterOrEqual(start, 0f, name + " after the previous district's exit");
            return start + 2f;
        }

        IEnumerator AubinExit()
        {
            float exit = NextChunk("CHN_AubinGrove_Exit", _riderZ);
            if (exit >= 0f) yield return Shot("aubingrove", new ShotSpec("11-exit.png", "", 0f, 0f), exit + 2f);
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
                // camera.Render, not SubmitRenderRequest: only this runs beginCameraRendering, where ComicCityBackdrop and
                // DistrictDirector write the panorama globals (sky size, height, skyline) as in the game.
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
