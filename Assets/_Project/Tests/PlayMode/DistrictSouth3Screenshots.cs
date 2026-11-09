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
    /// <summary>Review images of the KWINANA and WELLARD districts (south art agent 3) from the real chase camera on the
    /// real route: the ride starts in the district (startDistrict on a runtime copy of the tuning), so DistrictDirector
    /// applies its look, sky and far scenery as in the game (no asset is changed). Two horizon shots per district turn
    /// the camera to the side to check the far scenery. Images go to art-src/districts/&lt;id&gt;/previews.
    /// Needs DistrictSetup to have listed the districts in the theme.</summary>
    public class DistrictSouth3Screenshots
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
            Assert.NotNull(d, path + " (run KwinanaWellardBuilder.BuildBatch, then DistrictSetup.ApplyBatch)");
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
            public ShotSpec(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f)
            { File = file; Chunk = chunk; X = x; Z = z; Yaw = yaw; Pitch = pitch; }
        }

        IEnumerator Ride(District d, ShotSpec[] shots)
        {
            Assert.IsTrue(_original.HasDistricts && System.Array.IndexOf(_original.districts, d) >= 0, "run DistrictSetup: the theme lists " + d.id);
            _tuning.startDistrict = d.id;
            _game.EnterReady();
            yield return null;
            yield return null;
            var starts = new Dictionary<string, float>();
            TrackGenerator track = _game.track;
            string tag = "CHN_" + char.ToUpperInvariant(d.id[0]) + d.id.Substring(1) + "_";
            for (float z = 0f; z < track.buildAhead - 20f; z += 10f)
            {
                RoadChunk c = track.ChunkAt(z, out float start);
                if (c == null || start < 0f || !c.name.StartsWith(tag)) continue;
                string variant = c.name.Replace("(Clone)", "").Substring(tag.Length);
                if (!starts.ContainsKey(variant)) starts[variant] = start;
            }
            Debug.Log("[South3Shots] " + d.id + " chunks: " + string.Join(", ", starts.Select(kv => kv.Key + "@" + kv.Value)));
            int taken = 0;
            // In order along the road: chunks behind the rider are recycled, so never go back.
            foreach (ShotSpec shot in shots.Where(s => starts.ContainsKey(s.Chunk)).OrderBy(s => starts[s.Chunk] + s.Z))
            {
                taken++;
                yield return Shot(d.id, shot, Mathf.Max(1f, starts[shot.Chunk] + shot.Z));
            }
            foreach (ShotSpec shot in shots.Where(s => !starts.ContainsKey(s.Chunk))) Debug.LogWarning("[South3Shots] " + d.id + ": no " + shot.Chunk + " chunk in range");
            Assert.That(taken, Is.GreaterThanOrEqualTo(5), d.id + ": most review shots found their chunk");
        }

        /// <summary>Rides on (teleporting 40 m at a time, the track builds ahead) until a chunk whose name starts with
        /// `chunk` is built, then shoots `dz` metres from its start (district boundaries: exits and entries).</summary>
        IEnumerator Forward(string chunk, string id, string file, float dz)
        {
            TrackGenerator track = _game.track;
            float riderZ = _game.rider.transform.position.z;
            for (int step = 0; step < 40; step++)
            {
                for (float z = riderZ; z < riderZ + track.buildAhead - 10f; z += 5f)
                {
                    RoadChunk c = track.ChunkAt(z, out float start);
                    if (c == null || !c.name.StartsWith(chunk) || start + dz < riderZ) continue;
                    yield return Shot(id, new ShotSpec(file, chunk, 0f, 0f), start + dz);
                    yield break;
                }
                riderZ += 40f;
                _game.rider.ResetRider(new Vector3(0f, 0f, riderZ));
                for (int i = 0; i < 4; i++) yield return null;
            }
            Debug.LogWarning("[South3Shots] no " + chunk + " ahead");
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
            if (shot.Yaw != 0f || shot.Pitch != 0f)
                cam.transform.rotation = Quaternion.Euler(shot.Pitch, shot.Yaw, 0f);   // look out at the horizon
            Capture(cam, id, shot.File);
            cam.transform.rotation = keep;
        }

        [UnityTest, Explicit("Renders review images of KWINANA and WELLARD into art-src/districts/<id>/previews")]
        public IEnumerator Screenshots_KwinanaAndWellard_FromTheChaseCamera()
        {
            District kwinana = Load("Assets/_Project/Data/Districts/District_Kwinana.asset");
            District wellard = Load("Assets/_Project/Data/Districts/District_Wellard.asset");
            yield return Ride(kwinana, new[]
            {
                new ShotSpec("02-refinery.png", "Refinery", 1.5f, 2f),
                new ShotSpec("03-tank-farm.png", "Tanks", -1.5f, 4f),
                new ShotSpec("04-grain-terminal.png", "Tanks", 2f, 26f),
                new ShotSpec("05-beach-wreck.png", "Beach", 2.6f, 0f),
                new ShotSpec("06-dock-ramp-trench.png", "Trench", 0f, -4f),
                new ShotSpec("08-horizon-sound.png", "Beach", 0f, 10f, 55f, -4f),
                new ShotSpec("09-horizon-industry.png", "Refinery", 0f, 14f, -55f, -4f),
                new ShotSpec("10-trench-looking-back.png", "Trench", 2f, 26f, 200f, 28f),
            });
            // On along the route: the exit, then the boundary into Wellard.
            yield return Forward("CHN_Kwinana_Exit", "kwinana", "07-exit.png", -2f);
            yield return Forward("CHN_Wellard_Entry", "wellard", "01-entry.png", -6f);
            yield return Ride(wellard, new[]
            {
                new ShotSpec("02-station.png", "Station", -1.5f, 6f),
                new ShotSpec("03-estate.png", "Estate", 1.5f, 2f),
                new ShotSpec("04-village.png", "Village", 0f, 0f),
                new ShotSpec("05-park.png", "Park", -1.5f, 0f),
                new ShotSpec("06-wetland-boardwalk.png", "Wetland", 0f, -4f),
                new ShotSpec("08-horizon-east.png", "Park", 0f, 10f, -55f, -4f),
                new ShotSpec("09-horizon-wetland.png", "Wetland", 0f, 20f, 55f, -4f),
                new ShotSpec("10-water-gap-looking-back.png", "Wetland", 2f, 27f, 200f, 24f),
            });
            yield return Forward("CHN_Wellard_Exit", "wellard", "07-exit.png", -2f);
            // Kwinana's entry, coming from the district before it on the route.
            int index = System.Array.IndexOf(_original.districts, kwinana);
            District before = _original.districts[(index + _original.districts.Length - 1) % _original.districts.Length];
            _tuning.startDistrict = before.id;
            _game.EnterReady();
            yield return null;
            yield return null;
            yield return Forward("CHN_Kwinana_Entry", "kwinana", "01-entry.png", -6f);
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
                // district director set the sky (see DistrictSkyTests).
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
