using System.Collections;
using System.Collections.Generic;
using CityScooter.Devices;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>
    /// v0.6 polish (docs/v0.6-polish.md): a ride-through of one or more districts on the real route, a picture from the
    /// rider's chase camera every 20 m, on through the district's exit, the boundary and the next district's entry.
    /// Command line:
    ///   -cityscooterPolishIds cbd,quay,mountsbay   which districts (default: these three); one entry may also be
    ///                                              "cbd>mciver@-100": the next district wanted and where pictures start
    ///   -cityscooterPolishTag before               file prefix (before, after1, after2 ...)
    ///   -cityscooterPolishNext mciver              which line the ride takes after a trunk district (a seed is searched)
    ///   -cityscooterPolishFrom -100                first picture, metres relative to the boundary (default: the whole district)
    ///   -cityscooterPolishOut D:/some/folder       where the pictures go (default: Temp/polish in the project)
    /// Every picture is also put on contact sheets (16 per sheet) so a whole ride can be read at a glance.
    /// </summary>
    public class DistrictPolishRideShots
    {
        Scene _main;
        GameTuning _tuning;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            if (_tuning != null) Object.Destroy(_tuning);
            DeviceHub.Instance?.Mixer.Reset();
        }

        static string Arg(string name, string fallback)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : fallback;
        }

        IEnumerator RiderAt(GameController game, float x, float z)
        {
            game.rider.ResetRider(new Vector3(x, game.track.SurfaceHeightAt(z), z));
            var follow = Camera.main.GetComponent<FollowCamera>();
            follow?.SnapToTarget();
            for (int i = 0; i < 3; i++) yield return null;
            follow?.SnapToTarget();
            yield return null;
        }

        [UnityTest, Explicit("Renders a ride through districts every 20 m (v0.6 polish review)")]
        public IEnumerator RideThrough()
        {
            DeviceHub.Instance?.Mixer.Reset();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
            _main = SceneManager.GetSceneByName("Main");
            SceneManager.SetActiveScene(_main);
            yield return null;
            GameController game = GameController.Instance;
            TrackTheme theme = game.track.theme;
            Assert.IsTrue(theme.HasDistricts, "run DistrictSetup.ApplyBatch first");
            string[] ids = Arg("-cityscooterPolishIds", "cbd,quay,mountsbay").Split(',');
            string tag = Arg("-cityscooterPolishTag", "ride"), wantNextAll = Arg("-cityscooterPolishNext", "");
            string outRoot = Arg("-cityscooterPolishOut", System.IO.Path.Combine(Application.dataPath, "../Temp/polish"));
            float fromAll = float.Parse(Arg("-cityscooterPolishFrom", "-100000"), System.Globalization.CultureInfo.InvariantCulture);
            _tuning = Object.Instantiate(game.tuning);
            _tuning.fakeTurns = false;
            _tuning.boulevardSections = false;
            _tuning.hazardChanceStart = _tuning.hazardChanceMax = 0f;
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = .12f;
            _tuning.safeStartDistance = 20f;
            game.tuning = game.rider.tuning = game.track.tuning = _tuning;
            var turns = game.GetComponent<FakeTurnDirector>();
            if (turns != null) turns.tuning = _tuning;
            District[] route = DistrictRoute.Sort(theme.districts);

            foreach (string raw in ids)
            {
                // "cbd>mciver@-100": district, optionally the next district wanted and where the pictures start.
                string id = raw.Trim();
                string wantNext = wantNextAll;
                float from = fromAll;
                int cut = id.IndexOf('@');
                if (cut >= 0) { from = float.Parse(id.Substring(cut + 1), System.Globalization.CultureInfo.InvariantCulture); id = id.Substring(0, cut); }
                cut = id.IndexOf('>');
                if (cut >= 0) { wantNext = id.Substring(cut + 1); id = id.Substring(0, cut); }
                int index = DistrictRoute.IndexOf(route, id);
                Assert.GreaterOrEqual(index, 0, "no district " + id);
                // A seed whose first line pick (TrackGenerator: _runSeed ^ 0x51ed270b) leads to the wanted next district.
                int seed = 11;
                if (wantNext.Length > 0)
                    for (int s = 11; s < 400; s++)
                    {
                        int next = DistrictRoute.Next(route, index, new System.Random(s ^ 0x51ed270b).NextDouble());
                        if (route[next].id == wantNext) { seed = s; break; }
                    }
                game.track.seed = seed;
                _tuning.startDistrict = id;
                game.EnterReady();
                yield return null;
                yield return null;
                float start = game.rider.transform.position.z, z = start, boundary = float.PositiveInfinity;
                // Walk to the boundary first so its place is known (the generator plans it as it lays the road).
                for (int step = 0; step < 60 && float.IsInfinity(boundary); step++)
                {
                    boundary = game.track.NextBoundaryAfter(start);
                    if (!float.IsInfinity(boundary)) break;
                    z += 60f;
                    yield return RiderAt(game, 0f, z);
                }
                Assert.IsFalse(float.IsInfinity(boundary), id + ": no boundary found");
                District after = game.track.DistrictAt(boundary + 5f);
                string folder = System.IO.Path.Combine(outRoot, id, tag + (wantNext.Length > 0 ? "-to-" + wantNext : ""));
                System.IO.Directory.CreateDirectory(folder);
                Debug.Log("[PolishRide] " + id + " seed " + seed + " start " + start + " boundary " + boundary + " next " + (after != null ? after.id : "?") + " -> " + folder);
                // Back to the start (the track behind is recycled: restart the same ride).
                game.EnterReady();
                yield return null;
                yield return null;
                float first = Mathf.Max(start + 1f, boundary + from);
                first = start + Mathf.Floor((first - start) / 20f) * 20f + 1f;
                z = start;
                while (z < first - 120f) { z += 60f; yield return RiderAt(game, 0f, z); }
                var frames = new List<Texture2D>();
                var labels = new List<string>();
                for (float at = first; at <= boundary + 81f; at += 20f)
                {
                    yield return RiderAt(game, 0f, at);
                    for (int f = 0; f < 4; f++) yield return null;
                    RoadChunk c = game.track.ChunkAt(at, out float chunkStart);
                    string chunk = c != null ? c.name.Replace("(Clone)", "").Replace("CHN_", "") : "none";
                    float rel = at - boundary;
                    string name = Mathf.RoundToInt(at - start).ToString("0000") + "m_" + chunk + "+" + Mathf.RoundToInt(at - chunkStart) + (rel > -100f ? "_b" + (rel < 0 ? "m" : "p") + Mathf.RoundToInt(Mathf.Abs(rel)) : "");
                    Shot(Camera.main, System.IO.Path.Combine(folder, name + ".png"), 1280, 720, null);
                    var small = new Texture2D(400, 225, TextureFormat.RGB24, false);
                    Shot(Camera.main, null, 400, 225, small);
                    frames.Add(small);
                    labels.Add(name);
                }
                // Contact sheets: 4 x 4 frames of 400 x 225, in riding order (left to right, top to bottom).
                for (int sheet = 0; sheet * 16 < frames.Count; sheet++)
                {
                    var page = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                    page.SetPixels32(new Color32[1600 * 900]);
                    for (int i = 0; i < 16 && sheet * 16 + i < frames.Count; i++)
                        page.SetPixels((i % 4) * 400, (3 - i / 4) * 225, 400, 225, frames[sheet * 16 + i].GetPixels());
                    page.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, "_sheet" + (sheet + 1) + ".png"), page.EncodeToPNG());
                    Object.Destroy(page);
                }
                System.IO.File.WriteAllLines(System.IO.Path.Combine(folder, "_frames.txt"), labels);
                foreach (Texture2D t in frames) Object.Destroy(t);
            }
        }

        static void Shot(Camera camera, string path, int w, int h, Texture2D into)
        {
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            RenderTexture previous = RenderTexture.active, oldTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = rt;
                // camera.Render (not SubmitRenderRequest): it runs beginCameraRendering, where the sky globals are written.
                for (int pass = 0; pass < 2; pass++) camera.Render();
                RenderTexture.active = rt;
                Texture2D image = into != null ? into : new Texture2D(w, h, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                image.Apply();
                if (path != null) System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
                if (into == null) Object.Destroy(image);
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = oldTarget;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
