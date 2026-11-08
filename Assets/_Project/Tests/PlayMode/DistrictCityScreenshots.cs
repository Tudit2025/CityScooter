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
    /// <summary>Review images of the PERTH CBD and ELIZABETH QUAY districts (art agent 3) from the real chase camera:
    /// the ride starts in the district on the real route (startDistrict on a runtime copy of the tuning), so
    /// DistrictDirector applies its look, sky and far scenery as in the game (no asset is changed). Images go to art-src/districts/&lt;id&gt;/previews.</summary>
    public class DistrictCityScreenshots
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
            Assert.NotNull(d, path + " (run QuayBuilder.BuildCbdAndQuayBatch)");
            return d;
#else
            Assert.Ignore("Editor only");
            return null;
#endif
        }

        readonly struct ShotSpec
        {
            public readonly string File, Chunk;
            public readonly float X, Z;
            public ShotSpec(string file, string chunk, float x, float z) { File = file; Chunk = chunk; X = x; Z = z; }
        }

        // The real route: the ride starts in the district (startDistrict on the runtime tuning copy), so DistrictDirector
        // applies its look, sky, gateway and far scenery exactly as in the game. Shots are placed by chunk name.
        IEnumerator Ride(District d, ShotSpec[] shots, System.Func<Dictionary<string, float>, IEnumerator> extra = null)
        {
            Assert.IsTrue(_original.HasDistricts && System.Array.IndexOf(_original.districts, d) >= 0, "run DistrictSetup: the theme lists " + d.id);
            _tuning.startDistrict = d.id;
            _game.EnterReady();
            yield return null;
            yield return null;
            var starts = new Dictionary<string, float>();
            TrackGenerator track = _game.track;
            for (float z = 0f; z < track.buildAhead - 20f; z += 10f)
            {
                RoadChunk c = track.ChunkAt(z, out float start);
                if (c == null || start < 0f || !c.name.Contains("_" + char.ToUpperInvariant(d.id[0]) + d.id.Substring(1) + "_")) continue;
                string variant = c.name.Replace("(Clone)", "");
                variant = variant.Substring(variant.LastIndexOf('_') + 1);
                if (!starts.ContainsKey(variant)) starts[variant] = start;
            }
            Debug.Log("[DistrictCityShots] " + d.id + " chunks: " + string.Join(", ", starts.Select(kv => kv.Key + "@" + kv.Value)));
            int taken = 0;
            foreach (ShotSpec shot in shots)
            {
                if (!starts.ContainsKey(shot.Chunk)) { Debug.LogWarning("[DistrictCityShots] " + d.id + ": no " + shot.Chunk + " chunk in range"); continue; }
                taken++;
                yield return Shot(d.id, shot.File, shot.X, Mathf.Max(1f, starts[shot.Chunk] + shot.Z));
            }
            Assert.That(taken, Is.GreaterThanOrEqualTo(5), d.id + ": most review shots found their chunk");
            if (extra != null) yield return extra(starts);
        }

        IEnumerator Shot(string id, string file, float x, float z)
        {
            var follow = Camera.main.GetComponent<FollowCamera>();
            _game.rider.ResetRider(new Vector3(x, 0f, z));
            yield return null;
            yield return null;
            follow.SnapToTarget();
            yield return null;
            follow.SnapToTarget();
            for (int i = 0; i < 3; i++) yield return null; // the director moves the far scenery and blends the look
            follow.SnapToTarget();
            Capture(Camera.main, id, file);
        }

        [UnityTest, Explicit("Renders review images of PERTH CBD and ELIZABETH QUAY into art-src/districts/<id>/previews")]
        public IEnumerator Screenshots_CbdAndQuay_FromTheChaseCamera()
        {
            District cbd = Load("Assets/_Project/Data/Districts/District_CBD.asset");
            yield return Ride(cbd, new[]
            {
                new ShotSpec("01-entry.png", "Entry", 0f, 2f),
                new ShotSpec("02-widening.png", "Widen", 1.5f, 0f),
                new ShotSpec("03-terrace-4-lanes.png", "Terrace", 2f, 0f),
                new ShotSpec("04-town-hall.png", "Terrace", -2f, 10f),
                new ShotSpec("05-plaza-towers.png", "Plaza", 2f, 2f),
                new ShotSpec("06-car-park-kicker.png", "CarPark", -6f, 0f),
                new ShotSpec("07-narrowing.png", "Narrow", 0f, 0f),
                new ShotSpec("08-hay-street-mall.png", "MallArcade", 0f, 0f),
                new ShotSpec("09-mall-sidewalk.png", "MallArcade", -5.5f, 22f),
            });
            District quay = Load("Assets/_Project/Data/Districts/District_Quay.asset");
            yield return Ride(quay, new[]
            {
                new ShotSpec("01-entry.png", "Entry", 0f, 2f),
                new ShotSpec("02-promenade.png", "Promenade", 2f, 4f),
                new ShotSpec("03-bell-tower.png", "Promenade", 2.6f, 34f),
                new ShotSpec("04-bridge-approach.png", "BellTower", 0f, 30f),
                new ShotSpec("05-spanda-island.png", "Bridge", -1f, 36f),
                new ShotSpec("06-jetty-and-gap.png", "Spanda", 0f, 32f),
                new ShotSpec("07-exit.png", "Exit", 0f, 0f),
            }, starts => Gulls(quay, starts));
        }

        IEnumerator Gulls(District quay, Dictionary<string, float> starts)
        {
            HazardPattern gulls = quay.hazards.FirstOrDefault(h => h.name == "HZ_Quay_Seagulls");
            if (gulls == null || !starts.ContainsKey("Promenade")) yield break;
            _game.track.PlacePattern(gulls, starts["Promenade"] + 20f);
            yield return Shot("quay", "08-seagulls.png", 1f, starts["Promenade"] + 18f);
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
