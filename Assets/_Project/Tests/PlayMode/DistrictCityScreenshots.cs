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
    /// the Main scene rides a runtime copy of the comic theme whose chunks are the district's entry, chunks and exit in
    /// order (the theme asset is never changed). Images go to art-src/districts/&lt;id&gt;/previews.</summary>
    public class DistrictCityScreenshots
    {
        Scene _main;
        GameController _game;
        GameTuning _tuning;
        TrackTheme _original, _copy;
        GameObject _far;

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
            if (_far != null) Object.Destroy(_far);
            if (_game != null && _original != null) _game.track.theme = _original;
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            if (_tuning != null) Object.Destroy(_tuning);
            if (_copy != null) Object.Destroy(_copy);
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

        // Rides the district: entry, chunks, exit in order, its own obstacles and people, its look.
        float Ride(District d)
        {
            if (_copy != null) Object.Destroy(_copy);
            _copy = Object.Instantiate(_original);
            _copy.name = _original.name + " (" + d.id + " preview)";
            _copy.chunks = new[] { d.entry }.Concat(d.chunks).Concat(new[] { d.exit }).ToArray();
            _copy.orderedChunks = true;
            if (d.obstacles != null && d.obstacles.Length > 0) _copy.obstacles = d.obstacles;
            if (d.pedestrians != null && d.pedestrians.Length > 0) _copy.pedestrians = d.pedestrians;
            if (d.hazards != null && d.hazards.Length > 0) _copy.hazards = d.hazards;
            _copy.districts = null;
            _game.track.theme = _copy;
            RenderSettings.fogColor = d.fogColor;
            RenderSettings.ambientSkyColor = d.ambientSky;
            RenderSettings.ambientEquatorColor = d.ambientEquator;
            RenderSettings.ambientGroundColor = d.ambientGround;
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { l.color = d.sunColor; l.intensity = d.sunIntensity; }
            if (_far != null) Object.Destroy(_far);
            _far = d.farScenery != null ? Object.Instantiate(d.farScenery) : null;
            _game.EnterReady();
            _game.track.ChunkAt(0f, out float start);
            return start;
        }

        IEnumerator Shot(string id, string file, float x, float z)
        {
            var follow = Camera.main.GetComponent<FollowCamera>();
            _game.rider.ResetRider(new Vector3(x, 0f, z));
            if (_far != null) _far.transform.position = new Vector3(0f, _game.rider.transform.position.y, z);
            yield return null;
            yield return null;
            follow.SnapToTarget();
            yield return null;
            follow.SnapToTarget();
            Capture(Camera.main, id, file);
        }

        [UnityTest, Explicit("Renders review images of PERTH CBD and ELIZABETH QUAY into art-src/districts/<id>/previews")]
        public IEnumerator Screenshots_CbdAndQuay_FromTheChaseCamera()
        {
            // PERTH CBD: Entry, Widen, Terrace, TownHall, Plaza, CarPark, Narrow, MallArcade, MallShops, Exit (40 m each).
            District cbd = Load("Assets/_Project/Data/Districts/District_CBD.asset");
            float s = Ride(cbd);
            float C(int chunk, float local) => s + chunk * 40f + local;
            yield return Shot("cbd", "01-entry.png", 0f, C(0, 2f));
            yield return Shot("cbd", "02-widening.png", 1.5f, C(1, 0f));
            yield return Shot("cbd", "03-terrace-4-lanes.png", 2f, C(2, 0f));
            yield return Shot("cbd", "04-town-hall.png", -2f, C(2, 10f));
            yield return Shot("cbd", "05-plaza-towers.png", 2f, C(4, 2f));
            yield return Shot("cbd", "06-car-park-kicker.png", -6f, C(5, 0f));
            yield return Shot("cbd", "07-narrowing.png", 0f, C(6, 0f));
            yield return Shot("cbd", "08-hay-street-mall.png", 0f, C(7, 0f));
            yield return Shot("cbd", "09-mall-sidewalk.png", -5.5f, C(7, 22f));

            // ELIZABETH QUAY: Entry, Promenade, BellTower, Bridge, Spanda, Inlet, Exit.
            District quay = Load("Assets/_Project/Data/Districts/District_Quay.asset");
            s = Ride(quay);
            yield return Shot("quay", "01-entry.png", 0f, C(0, 2f));
            yield return Shot("quay", "02-promenade.png", 2f, C(1, 4f));
            yield return Shot("quay", "03-bell-tower.png", 2.6f, C(1, 34f));
            yield return Shot("quay", "04-bridge-approach.png", 0f, C(2, 30f));
            yield return Shot("quay", "05-spanda-island.png", -1f, C(3, 36f));
            yield return Shot("quay", "06-jetty-and-gap.png", 0f, C(4, 32f));
            yield return Shot("quay", "07-exit.png", 0f, C(6, 0f));
            HazardPattern gulls = quay.hazards.FirstOrDefault(h => h.name == "HZ_Quay_Seagulls");
            if (gulls != null) { _game.EnterReady(); _game.track.PlacePattern(gulls, C(1, 20f)); yield return Shot("quay", "08-seagulls.png", 1f, C(1, 18f)); }
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
