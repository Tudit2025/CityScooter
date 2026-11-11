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
    /// <summary>Review images of CANNING BRIDGE and MURDOCH (south line, art agent 1) from the real chase camera: the ride
    /// starts in the district on the real route (startDistrict on a runtime copy of the tuning), so DistrictDirector applies
    /// its look, sky and far scenery as in the game (no asset is changed). Two extra shots per district tilt the camera up
    /// to check the far-scenery horizon band. Images go to art-src/districts/&lt;id&gt;/previews.</summary>
    public class DistrictSouth1Screenshots
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
            Assert.NotNull(d, path + " (run CanningMurdochBuilder.BuildBatch, then DistrictSetup.ApplyBatch)");
            return d;
#else
            Assert.Ignore("Editor only");
            return null;
#endif
        }

        readonly struct ShotSpec
        {
            public readonly string File, Chunk, Pattern;
            public readonly float X, Z, Pitch, Yaw;
            public ShotSpec(string file, string chunk, float x, float z, float pitch = float.NaN, float yaw = 0f, string pattern = null)
            { File = file; Chunk = chunk; X = x; Z = z; Pitch = pitch; Yaw = yaw; Pattern = pattern; }
        }

        IEnumerator Ride(District d, string prefix, ShotSpec[] shots)
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
                if (c == null || start < 0f || !c.name.StartsWith(prefix)) continue;
                string variant = c.name.Replace("(Clone)", "").Substring(prefix.Length);
                if (!starts.ContainsKey(variant)) starts[variant] = start;
            }
            Debug.Log("[South1Shots] " + d.id + " chunks: " + string.Join(", ", starts.Select(kv => kv.Key + "@" + kv.Value)));
            foreach (ShotSpec missing in shots.Where(s => !starts.ContainsKey(s.Chunk)))
                Debug.LogWarning("[South1Shots] " + d.id + ": no " + missing.Chunk + " chunk in range");
            // In route order: the generator recycles the chunks behind the rider, so never go back.
            var found = shots.Where(s => starts.ContainsKey(s.Chunk)).OrderBy(s => starts[s.Chunk] + s.Z).ToList();
            foreach (ShotSpec shot in found)
            {
                float z = Mathf.Max(1f, starts[shot.Chunk] + shot.Z);
                HazardPattern pattern = shot.Pattern == null ? null : d.hazards.FirstOrDefault(h => h.name == shot.Pattern);
                if (pattern != null) _game.track.PlacePattern(pattern, z + 4f); // animals walk on the path only in patterns
                yield return Shot(d.id, shot, z);
            }
            Assert.That(found.Count, Is.GreaterThanOrEqualTo(5), d.id + ": most review shots found their chunk");
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
            Transform cam = Camera.main.transform;
            Quaternion old = cam.rotation;
            // Horizon shots: the same camera, tilted up to the horizon (and turned for the side bands).
            if (!float.IsNaN(shot.Pitch)) cam.rotation = Quaternion.Euler(shot.Pitch, shot.Yaw, 0f);
            Capture(Camera.main, id, shot.File);
            cam.rotation = old;
        }

        [UnityTest, Explicit("Renders review images of CANNING BRIDGE and MURDOCH into art-src/districts/<id>/previews")]
        public IEnumerator Screenshots_CanningBridgeAndMurdoch_FromTheChaseCamera()
        {
            District canning = Load("Assets/_Project/Data/Districts/District_CanningBridge.asset");
            yield return Ride(canning, "CHN_CanningBridge_", new[]
            {
                new ShotSpec("02-foreshore.png", "Foreshore", 2f, 2f),
                new ShotSpec("03-station.png", "Station", 0f, 2f),
                new ShotSpec("04-river-bridge.png", "Bridge", 1f, 4f),
                new ShotSpec("05-applecross.png", "Applecross", 2.6f, 0f),
                new ShotSpec("10-applecross-buildings.png", "Bridge", 2f, 30f, 4f, 32f),
                new ShotSpec("06-jetty-and-gap.png", "Inlet", 0f, 0f),
                new ShotSpec("07-horizon-right.png", "Foreshore", 0f, 14f, 3f, 40f),
                new ShotSpec("08-horizon-left.png", "Bridge", 0f, 14f, 2f, -50f),
                new ShotSpec("09-pelicans.png", "Applecross", 0f, 14f, float.NaN, 0f, "HZ_CanningBridge_Pelicans"),
            });
            District murdoch = Load("Assets/_Project/Data/Districts/District_Murdoch.asset");
            yield return Ride(murdoch, "CHN_Murdoch_", new[]
            {
                new ShotSpec("02-noise-walls.png", "NoiseWall", 2f, 2f),
                new ShotSpec("03-station-bus-hall.png", "Station", 0f, -12f),
                new ShotSpec("04-bushland-hump.png", "Bushland", 1f, 0f),
                new ShotSpec("05-fiona-stanley.png", "Bushland", -2f, 32f),
                new ShotSpec("10-fiona-stanley-left.png", "Bushland", -2f, 33f, 5f, -32f),
                new ShotSpec("06-exit.png", "Exit", 0f, 0f),
                new ShotSpec("07-horizon-right.png", "NoiseWall", 0f, 14f, 3f, 40f),
                new ShotSpec("08-horizon-left.png", "Bushland", 0f, 30f, 2f, -50f),
                new ShotSpec("09-bobtail.png", "Hospital", 0f, 12f, float.NaN, 0f, "HZ_Murdoch_Bobtail"),
            });
        }

        // v0.6 polish: the whole district as the rider sees it, a picture every 20 m (see SouthRideThrough).
        [UnityTest, Explicit("Ride-through of CANNING BRIDGE and MURDOCH, a picture every 20 m, into art-src/districts/<id>/previews/polish")]
        public IEnumerator Screenshots_RideThrough_CanningBridgeAndMurdoch()
        {
            yield return SouthRideThrough.Ride(_game, _tuning, "canningbridge");
            yield return SouthRideThrough.Ride(_game, _tuning, "murdoch");
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
                // district director set the sky (skyline sunk on the south line) and the far fade (see DistrictSkyTests).
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

    /// <summary>The polish ride-through (docs/v0.6-polish.md): starts in the district BEFORE the one asked for, so the
    /// boundary, the gateway and the entry chunk are in the pictures; then a picture from the chase camera every 20 m
    /// from 20 m before the district to 20 m after it. Files: art-src/districts/&lt;id&gt;/previews/polish/
    /// &lt;tag&gt;-&lt;nn&gt;-&lt;metres&gt;.png (m020 = 20 m before the district). The tag comes from the command line:
    /// -cityscooterRideTag before|after (default "ride").</summary>
    internal static class SouthRideThrough
    {
        public static IEnumerator Ride(GameController game, GameTuning tuning, string id)
        {
            TrackTheme theme = game.track.theme;
            Assert.IsTrue(theme.HasDistricts, "run DistrictSetup.ApplyBatch first");
            int at = System.Array.FindIndex(theme.districts, d => d != null && d.id == id);
            Assert.GreaterOrEqual(at, 1, id + " and the district before it are on the route");
            string[] args = System.Environment.GetCommandLineArgs();
            int arg = System.Array.IndexOf(args, "-cityscooterRideTag");
            string tag = arg >= 0 && arg + 1 < args.Length ? args[arg + 1] : "ride";
            string folder = System.IO.Path.Combine(Application.dataPath, "../art-src/districts/" + id + "/previews/polish");
            System.IO.Directory.CreateDirectory(folder);

            tuning.startDistrict = theme.districts[at - 1].id;
            game.EnterReady();
            yield return null;
            TrackGenerator track = game.track;
            float z = game.rider.transform.position.z, begin = float.PositiveInfinity;
            for (int step = 0; step < 40; step++)
            {
                begin = track.NextBoundaryAfter(game.rider.transform.position.z);
                if (!float.IsInfinity(begin)) break;
                z += 60f;
                yield return At(game, z);
            }
            Assert.IsFalse(float.IsInfinity(begin), "the boundary into " + id);
            while (z < begin - 100f) { z += 60f; yield return At(game, z); }
            float end = float.PositiveInfinity;
            int n = 0;
            for (float offset = -20f; offset < 700f; offset += 20f, n++)
            {
                yield return At(game, begin + offset);
                for (int f = 0; f < 5; f++) yield return null;
                if (float.IsInfinity(end) && offset >= 0f) end = track.NextBoundaryAfter(begin + offset);
                string metres = offset < 0f ? "m" + Mathf.Abs(offset).ToString("000") : offset.ToString("000");
                Shot(Camera.main, System.IO.Path.Combine(folder, tag + "-" + n.ToString("00") + "-" + metres + ".png"));
                if (begin + offset >= end + 19f) break;
            }
            Debug.Log("[RideThrough] " + id + ": " + (n + 1) + " pictures, district " + begin + " to " + end);
        }

        static IEnumerator At(GameController game, float z)
        {
            game.rider.ResetRider(new Vector3(0f, game.track.SurfaceHeightAt(z), z));
            var follow = Camera.main.GetComponent<FollowCamera>();
            if (follow != null) follow.SnapToTarget();
            for (int i = 0; i < 4; i++) yield return null;
            if (follow != null) follow.SnapToTarget();
        }

        static void Shot(Camera camera, string path)
        {
            const int w = 960, h = 540;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            RenderTexture previous = RenderTexture.active, oldTarget = camera.targetTexture;
            Texture2D image = null;
            try
            {
                camera.targetTexture = rt;
                for (int pass = 0; pass < 2; pass++) camera.Render(); // camera.Render: see DistrictSkyTests.Shot
                RenderTexture.active = rt;
                image = new Texture2D(w, h, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                image.Apply();
                System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
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
