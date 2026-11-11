#if UNITY_EDITOR
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
    /// v0.6 polish review of the first three districts (UWA, Matilda Bay, Kings Park): what the rider sees every 20 m
    /// from the chase camera on the real route, and each boundary 60 m / 20 m before and 20 m after.
    ///   -cityscooterRide uwa,matilda,kingspark   districts to ride through (default: those three)
    ///   -cityscooterFrom warnbro,midland,uwa,matilda,kingspark   districts whose EXIT boundary to shoot
    ///   -cityscooterRound before|r1|r2           file prefix (default: ride)
    /// Output: art-src/districts/&lt;id&gt;/previews/polish/&lt;round&gt;-&lt;metres&gt;m.png, contact sheets
    /// &lt;round&gt;-sheet-N.jpg (6 pictures each, left to right, top to bottom), and boundary pictures
    /// &lt;round&gt;-from-&lt;id&gt;-&lt;offset&gt;.png in the folder of the district the boundary leads INTO.
    /// </summary>
    public class DistrictTrunkARide
    {
        Scene _main;
        GameController _game;
        GameTuning _tuning;

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
            _tuning = Object.Instantiate(_game.tuning);
            _game.tuning = _game.rider.tuning = _game.track.tuning = _tuning;
        }

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

        static string Folder(string id)
        {
            string folder = System.IO.Path.Combine(Application.dataPath, "../art-src/districts/" + id + "/previews/polish");
            System.IO.Directory.CreateDirectory(folder);
            return folder;
        }

        IEnumerator RiderAt(float z)
        {
            _game.rider.ResetRider(new Vector3(0f, _game.track.SurfaceHeightAt(z), z));
            var follow = Camera.main.GetComponent<FollowCamera>();
            follow?.SnapToTarget();
            for (int i = 0; i < 6; i++) yield return null;
            follow?.SnapToTarget();
            yield return null;
        }

        [UnityTest, Explicit("Renders a ride through UWA, Matilda Bay and Kings Park every 20 m (review)")]
        public IEnumerator RideThrough()
        {
            TrackTheme theme = _game.track.theme;
            Assert.IsTrue(theme.HasDistricts, "run DistrictSetup.ApplyBatch first");
            string round = Arg("-cityscooterRound", "ride");
            foreach (string id in Arg("-cityscooterRide", "uwa,matilda,kingspark").Split(','))
            {
                District d = System.Array.Find(theme.districts, x => x != null && x.id == id);
                Assert.NotNull(d, id);
                _tuning.startDistrict = id;
                _game.EnterReady();
                yield return null;
                yield return null;
                float start = _game.rider.transform.position.z;
                string folder = Folder(id);
                var tiles = new List<Texture2D>();
                int sheet = 0;
                for (float ahead = 0f; ahead < 760f; ahead += 20f)
                {
                    float z = start + ahead;
                    float boundary = _game.track.NextBoundaryAfter(start + 1f);
                    if (!float.IsInfinity(boundary) && z > boundary + 45f) break;
                    yield return RiderAt(z);
                    Texture2D shot = Shot(Camera.main);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, round + "-" + ((int)ahead).ToString("000") + "m.png"), shot.EncodeToPNG());
                    tiles.Add(shot);
                    if (tiles.Count == 6) SaveSheet(tiles, System.IO.Path.Combine(folder, round + "-sheet-" + (++sheet) + ".jpg"));
                }
                if (tiles.Count > 0) SaveSheet(tiles, System.IO.Path.Combine(folder, round + "-sheet-" + (++sheet) + ".jpg"));
            }
        }

        [UnityTest, Explicit("Renders the boundaries out of the named districts (review)")]
        public IEnumerator Boundaries()
        {
            TrackTheme theme = _game.track.theme;
            Assert.IsTrue(theme.HasDistricts, "run DistrictSetup.ApplyBatch first");
            string round = Arg("-cityscooterRound", "ride");
            foreach (string id in Arg("-cityscooterFrom", "warnbro,midland,uwa,matilda,kingspark").Split(','))
            {
                District d = System.Array.Find(theme.districts, x => x != null && x.id == id);
                Assert.NotNull(d, id);
                _tuning.startDistrict = id;
                _game.EnterReady();
                yield return null;
                float z = _game.rider.transform.position.z, boundary = float.PositiveInfinity;
                for (int step = 0; step < 40; step++)
                {
                    boundary = _game.track.NextBoundaryAfter(_game.rider.transform.position.z);
                    if (!float.IsInfinity(boundary)) break;
                    z += 60f;
                    yield return RiderAt(z);
                }
                if (float.IsInfinity(boundary)) continue;
                District next = _game.track.DistrictAt(boundary + 5f);
                while (z < boundary - 120f) { z += 60f; yield return RiderAt(z); }
                var tiles = new List<Texture2D>();
                // into the folder of the district the boundary leads into, if that is one of ours; otherwise our own
                bool ours = next != null && System.Array.IndexOf(new[] { "uwa", "matilda", "kingspark" }, next.id) >= 0;
                string folder = Folder(ours ? next.id : id);
                foreach (float offset in new[] { -60f, -40f, -20f, 0f, 20f, 40f })
                {
                    yield return RiderAt(boundary + offset);
                    Texture2D shot = Shot(Camera.main);
                    string name = round + "-from-" + id + (offset < 0 ? "-before" : "-after") + Mathf.Abs(offset) + "m.png";
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, name), shot.EncodeToPNG());
                    tiles.Add(shot);
                }
                SaveSheet(tiles, System.IO.Path.Combine(folder, round + "-from-" + id + "-sheet.jpg"));
            }
        }

        // 3 x 2 contact sheet at half size; destroys the tiles.
        static void SaveSheet(List<Texture2D> tiles, string path)
        {
            const int tw = 640, th = 360, cols = 3, rows = 2;
            var sheet = new Texture2D(tw * cols, th * rows, TextureFormat.RGB24, false);
            var fill = new Color32[tw * cols * th * rows];
            sheet.SetPixels32(fill);
            for (int i = 0; i < tiles.Count && i < cols * rows; i++)
            {
                Color32[] src = tiles[i].GetPixels32();
                int sw = tiles[i].width;
                var dst = new Color32[tw * th];
                for (int y = 0; y < th; y++)
                    for (int x = 0; x < tw; x++)
                    {
                        Color32 a = src[y * 2 * sw + x * 2], b = src[y * 2 * sw + x * 2 + 1], c = src[(y * 2 + 1) * sw + x * 2], e = src[(y * 2 + 1) * sw + x * 2 + 1];
                        dst[y * tw + x] = new Color32((byte)((a.r + b.r + c.r + e.r) / 4), (byte)((a.g + b.g + c.g + e.g) / 4), (byte)((a.b + b.b + c.b + e.b) / 4), 255);
                    }
                sheet.SetPixels32((i % cols) * tw, (rows - 1 - i / cols) * th, tw, th, dst);
            }
            sheet.Apply();
            System.IO.File.WriteAllBytes(path, sheet.EncodeToJPG(88));
            Object.Destroy(sheet);
            foreach (Texture2D t in tiles) Object.Destroy(t);
            tiles.Clear();
        }

        static Texture2D Shot(Camera camera)
        {
            const int w = 1280, h = 720;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            RenderTexture previous = RenderTexture.active, oldTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = rt;
                // camera.Render (not SubmitRenderRequest): it runs beginCameraRendering, where the panorama globals are set.
                for (int pass = 0; pass < 2; pass++) camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(w, h, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                image.Apply();
                return image;
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
#endif
