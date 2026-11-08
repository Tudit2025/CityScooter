#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityScooter.Devices;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>Review images of the UWA and Matilda Bay districts from the real chase camera (art-src/districts/&lt;id&gt;/
    /// previews). The game runs with a runtime copy of the theme whose chunks are the district's [entry, chunks, exit]
    /// on the real route: the ride starts in the district (GameTuning.startDistrict on a runtime copy of the tuning), so
    /// DistrictDirector applies its look, sky, gateway and far scenery. No asset is modified.</summary>
    public class DistrictCrawleyScreenshots
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
            _tuning.boulevardChance = 0f;
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

        [UnityTest, Explicit("Renders review images of UWA into art-src/districts/uwa/previews")]
        public IEnumerator Screenshots_Uwa()
        {
            yield return Ride("Assets/_Project/Data/Districts/District_UWA.asset", new[]
            {
                new Shot("01-entry.png", "Entry", 0f, 4f),
                new Shot("02-arcades.png", "Arcades", -1.5f, 6f),
                new Shot("03-grove.png", "Grove", 2.6f, 2f),
                new Shot("04-winthrop-approach.png", "WinthropHall", 0f, -26f),
                new Shot("05-winthrop-hall.png", "WinthropHall", -5.6f, 8f),
                new Shot("06-pond-jump.png", "PondJump", 3.5f, -2f),
                new Shot("07-cloisters-exit.png", "Cloisters", 0f, 10f),
            });
        }

        [UnityTest, Explicit("Renders review images of Matilda Bay into art-src/districts/matilda/previews")]
        public IEnumerator Screenshots_Matilda()
        {
            yield return Ride("Assets/_Project/Data/Districts/District_Matilda.asset", new[]
            {
                new Shot("01-entry.png", "Entry", 0f, 4f),
                new Shot("02-foreshore-red-path.png", "Foreshore", 6f, 4f),
                new Shot("03-boatshed-approach.png", "Boatshed", 2.6f, -18f),
                new Shot("04-boatshed.png", "Boatshed", 6.2f, 10f),
                new Shot("05-cafe.png", "Cafe", -5.8f, 2f),
                new Shot("06-inlet-jetty-jump.png", "Inlet", 0f, -4f),
                new Shot("07-picnic-exit.png", "Picnic", 1f, 8f),
            });
        }

        readonly struct Shot
        {
            public readonly string File, Chunk;
            public readonly float X, Z;
            public Shot(string file, string chunk, float x, float z) { File = file; Chunk = chunk; X = x; Z = z; }
        }

        IEnumerator Ride(string districtPath, Shot[] shots)
        {
            var d = AssetDatabase.LoadAssetAtPath<District>(districtPath);
            Assert.NotNull(d, districtPath);
            // The real route: the ride starts in this district, so DistrictDirector applies its look, sky, gateway and
            // far scenery exactly as in the game.
            Assert.IsTrue(_original.HasDistricts && System.Array.IndexOf(_original.districts, d) >= 0, "run DistrictSetup: the theme lists " + d.id);
            _tuning.startDistrict = d.id;
            _game.EnterReady();
            yield return null;
            yield return null;

            // Where the generator laid each chunk (the whole 7-chunk cycle is within the build range at the start).
            var starts = new Dictionary<string, float>();
            TrackGenerator track = _game.track;
            for (float z = 0f; z < track.buildAhead - 20f; z += 10f)
            {
                RoadChunk c = track.ChunkAt(z, out float start);
                if (c == null || start < 0f) continue;
                string variant = c.name.Replace("(Clone)", "");
                variant = variant.Substring(variant.LastIndexOf('_') + 1);
                if (!starts.ContainsKey(variant)) starts[variant] = start;
            }
            Debug.Log("[DistrictShots] " + d.id + " chunks: " + string.Join(", ", starts.Select(kv => kv.Key + "@" + kv.Value)));

            var follow = Camera.main.GetComponent<FollowCamera>();
            int taken = 0;
            foreach (Shot shot in shots)
            {
                if (!starts.ContainsKey(shot.Chunk)) { Debug.LogWarning("[DistrictShots] " + d.id + ": no " + shot.Chunk + " chunk within the build range"); continue; }
                taken++;
                float z = Mathf.Max(1f, starts[shot.Chunk] + shot.Z);
                _game.rider.ResetRider(new Vector3(shot.X, 0f, z));
                yield return null;
                yield return null;
                follow.SnapToTarget();
                yield return null;
                follow.SnapToTarget();
                for (int i = 0; i < 3; i++) yield return null; // the director moves the far scenery and blends the look
                follow.SnapToTarget();
                Capture(Camera.main, d.id, shot.File);
            }
            Assert.That(taken, Is.GreaterThanOrEqualTo(5), d.id + ": most review shots found their chunk");
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
                var request = new RenderPipeline.StandardRequest { destination = rt };
                for (int pass = 0; pass < 2; pass++)
                    if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request); else camera.Render();
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
#endif
