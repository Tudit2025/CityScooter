using System.Collections;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>
    /// The one Perth panorama is turned (District.skyYaw: which way each road really runs) and sized (how far the city
    /// is) per district, blended across a boundary like the rest of the look. Uses the real Main scene.
    /// </summary>
    public class DistrictSkyTests
    {
        Scene _main;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            DeviceHub_Reset();
        }

        static void DeviceHub_Reset() => Devices.DeviceHub.Instance?.Mixer.Reset();

        IEnumerator LoadMain()
        {
            DeviceHub_Reset();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
            _main = SceneManager.GetSceneByName("Main");
            SceneManager.SetActiveScene(_main);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SkyTurnsHalfWayAtABoundary_AndAllTheWayAfter()
        {
            yield return LoadMain();
            GameController game = GameController.Instance;
            TrackTheme theme = Object.Instantiate(game.track.theme);
            District wp = System.Array.Find(theme.districts, d => d.id == "westperth");
            Assert.IsNotNull(wp, "the theme has the West Perth district");
            District a = Object.Instantiate(wp), b = Object.Instantiate(wp);
            a.length = b.length = new Vector2(300f, 300f);
            a.skyYaw = 0f; a.skySize = 1f; a.skyHeight = 0f;
            b.id = "riverview"; b.order = 41; b.skyYaw = 90f; b.skySize = 0.8f; b.skyHeight = -2f;
            theme.districts = new[] { a, b };
            GameTuning tuning = Object.Instantiate(game.tuning);
            tuning.obstacleChanceStart = tuning.obstacleChanceMax = tuning.hazardChanceStart = tuning.hazardChanceMax = 0f;
            tuning.startDistrict = "westperth";
            game.tuning = game.rider.tuning = game.track.tuning = tuning;
            game.track.theme = theme;
            game.EnterReady();
            yield return null;

            float boundary = game.track.NextBoundaryZ;
            Assert.Less(boundary, 400f, "a boundary is planned after 300 m");
            DistrictDirector director = DistrictDirector.Instance;
            FakeTurnDirector turns = FakeTurnDirector.Instance;
            Assert.IsNotNull(director);
            Assert.IsNotNull(turns);

            yield return RiderAt(game, boundary - 200f);
            Assert.AreEqual(0f, director.SkyView.x, 0.5f, "before the boundary: the first district's sky");
            Assert.AreEqual(0f, turns.SkyOffset, 0.01f);

            yield return RiderAt(game, boundary);
            Assert.AreEqual(45f, director.SkyView.x, 3f, "at the boundary the sky is half way round");
            Assert.AreEqual(45f * Mathf.Deg2Rad, turns.SkyOffset, 0.06f, "the sky heading carries it");
            Assert.AreEqual(0.9f, director.SkyView.y, 0.03f, "size half way");

            yield return RiderAt(game, boundary + 200f);
            Assert.AreEqual(90f, director.SkyView.x, 0.5f, "after the blend: the second district's sky");
            Assert.AreEqual(0.8f, director.SkyView.y, 0.01f);
            Assert.AreEqual(-2f, director.SkyView.z, 0.01f);
            Vector4 heading = Shader.GetGlobalVector("_ComicTurn");
            Assert.AreEqual(turns.Heading + 90f * Mathf.Deg2Rad, heading.w, 0.02f, "the panorama yaw global includes the district's yaw");

            director.Restore();
            Assert.AreEqual(0f, turns.SkyOffset, 0.001f, "restore puts the sky back");
        }

        // Lesson 1 (11/9): at UWA (size 0.72) the city floated above the horizon on a pale band, because size scaled the
        // panorama about its middle row (~16 degrees up). Size now scales about the horizon; skyline 0 sinks the city.
        [UnityTest]
        public IEnumerator SkySize_KeepsTheCityOnTheHorizon_Skyline0_SinksIt()
        {
            yield return LoadMain();
            GameController game = GameController.Instance;
            TrackTheme theme = Object.Instantiate(game.track.theme);
            District wp = System.Array.Find(theme.districts, d => d.id == "westperth");
            District a = Object.Instantiate(wp), b = Object.Instantiate(wp);
            a.length = b.length = new Vector2(300f, 300f);
            a.skyYaw = 0f; a.skySize = 1f; a.skyHeight = 0f; a.skyline = 1f;
            b.id = "southline"; b.order = 41; b.skyYaw = 0f; b.skySize = 0.8f; b.skyHeight = -2f; b.skyline = 0f;
            theme.districts = new[] { a, b };
            GameTuning tuning = Object.Instantiate(game.tuning);
            tuning.obstacleChanceStart = tuning.obstacleChanceMax = tuning.hazardChanceStart = tuning.hazardChanceMax = 0f;
            tuning.startDistrict = "westperth";
            game.tuning = game.rider.tuning = game.track.tuning = tuning;
            game.track.theme = theme;
            game.EnterReady();
            yield return null;
            float boundary = game.track.NextBoundaryZ;
            DistrictDirector director = DistrictDirector.Instance;

            yield return RiderAt(game, boundary - 200f);
            Assert.AreEqual(1f, director.SkylineShown, 0.001f);
            float scale = Shader.GetGlobalFloat("_ComicSkyScale");
            Vector4 mapping = Shader.GetGlobalVector("_ComicSkyMapping"); // ComicCityBackdrop's own values (nothing to change)
            Assume.That(mapping.w, Is.GreaterThan(0.5f), "the panorama sky is on in Main");
#if UNITY_EDITOR
            // The authored sky material (a district tint may already have swapped in a runtime copy of it).
            Material authored = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/ComicCity/Sky_ComicCity.mat");
#else
            Material authored = RenderSettings.skybox;
#endif
            Assert.NotNull(authored);
            Vector4 matMapping = authored.GetVector("_ComicSkyMapping");
            float matScale = authored.GetFloat("_ComicSkyScale");

            yield return RiderAt(game, boundary);
            Assert.AreEqual(0.5f, director.SkylineShown, 0.05f, "half sunk at the boundary");

            yield return RiderAt(game, boundary + 200f);
            Assert.AreEqual(0f, director.SkylineShown, 0.001f);
            Camera.main.Render(); // the panorama globals are adjusted just before a camera renders
            Assert.AreEqual(scale * 0.8f, Shader.GetGlobalFloat("_ComicSkyScale"), 0.001f, "size scales the apparent size");
            float expected = mapping.y * 0.8f + (-2f - DistrictDirector.SkylineSinkDegrees) * Mathf.Deg2Rad;
            Assert.AreEqual(expected, Shader.GetGlobalVector("_ComicSkyMapping").y, 0.001f, "scaled about the horizon, moved, then sunk (globals: far facades)");
            // The sky material has its own values (they win over globals): the district copy of it carries the change.
            Material sky = RenderSettings.skybox;
            Assert.AreNotSame(authored, sky, "a runtime copy of the sky material");
            StringAssert.Contains("(district)", sky.name);
            Assert.AreEqual(matMapping.y * 0.8f + (-2f - DistrictDirector.SkylineSinkDegrees) * Mathf.Deg2Rad, sky.GetVector("_ComicSkyMapping").y, 0.001f, "the sky itself");
            Assert.AreEqual(matScale * 0.8f, sky.GetFloat("_ComicSkyScale"), 0.001f);
            Assert.AreEqual(matMapping.x, sky.GetVector("_ComicSkyMapping").x, 0.0001f, "yaw stays with _ComicTurn");

            // The horizon row of the panorama stays on the horizon when only the size changes.
            float m = mapping.y, k = 0.72f, span = Mathf.PI * Mathf.Max(.1f, mapping.z);
            float vBefore = 0.5f + (0f - m) / (span * scale);
            float vAfter = 0.5f + (0f - DistrictDirector.SkyElevation(m, k, 0f, 1f)) / (span * scale * k);
            Assert.AreEqual(vBefore, vAfter, 0.0001f);
            director.Restore();
            Assert.IsFalse(RenderSettings.skybox != null && RenderSettings.skybox.name.Contains("(district)"), "restore puts the scene's sky material back");
        }

        IEnumerator RiderAt(GameController game, float z)
        {
            game.rider.ResetRider(new Vector3(0f, game.track.SurfaceHeightAt(z), z));
            Camera.main.GetComponent<FollowCamera>()?.SnapToTarget();
            for (int i = 0; i < 4; i++) yield return null;
        }

        [UnityTest, Explicit("Renders every district of the real route from the chase camera into art-src/districts/route (review)")]
        public IEnumerator Screenshots_EveryDistrictOfTheRoute()
        {
            yield return LoadMain();
            GameController game = GameController.Instance;
            TrackTheme theme = game.track.theme;
            Assert.IsTrue(theme.HasDistricts, "run DistrictSetup.ApplyBatch first");
            string folder = System.IO.Path.Combine(Application.dataPath, "../art-src/districts/route");
            System.IO.Directory.CreateDirectory(folder);
            GameTuning tuning = Object.Instantiate(game.tuning);
            game.tuning = game.rider.tuning = game.track.tuning = tuning;
            for (int i = 0; i < theme.districts.Length; i++)
            {
                District d = theme.districts[i];
                tuning.startDistrict = d.id;
                game.EnterReady();
                yield return null;
                float start = game.rider.transform.position.z;
                foreach (float ahead in new[] { 0f, 30f, 140f })
                {
                    yield return RiderAt(game, start + ahead);
                    for (int f = 0; f < 6; f++) yield return null;
                    Shot(Camera.main, System.IO.Path.Combine(folder, (i + 1) + "-" + d.id + "-" + ahead + "m.png"));
                }
            }
        }

        static void Shot(Camera camera, string path)
        {
            var rt = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            RenderTexture previous = RenderTexture.active, oldTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = rt;
                // camera.Render, not SubmitRenderRequest: only this runs beginCameraRendering, where ComicCityBackdrop and
                // DistrictDirector write the panorama globals (sky size, height, skyline) as in the game.
                for (int pass = 0; pass < 2; pass++) camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
                Object.Destroy(image);
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
