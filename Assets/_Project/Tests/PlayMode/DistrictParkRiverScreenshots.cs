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
    /// <summary>
    /// Review images of Kings Park and Mounts Bay Rd from the real chase camera in the Main scene (art-src/districts/&lt;id&gt;/
    /// previews). The game runs on a runtime copy of the comic theme whose chunks are the district's entry, chunks and exit
    /// in order (the asset is never modified); the district's look colours and far scenery are applied by hand, like the
    /// district system will.
    /// </summary>
    public class DistrictParkRiverScreenshots
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
            _tuning = Object.Instantiate(_game.tuning);
            _tuning.fakeTurns = false; _tuning.boulevardSections = false;
            _tuning.hazardChanceStart = _tuning.hazardChanceMax = 0f;
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = .3f;
            _tuning.safeStartDistance = 20f;
            _game.tuning = _game.rider.tuning = _game.track.tuning = _tuning;
            var turns = _game.GetComponent<FakeTurnDirector>();
            if (turns != null) turns.tuning = _tuning;
            _original = _game.track.theme;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_game != null) _game.track.theme = _original;
            if (_far != null) Object.Destroy(_far);
            if (_copy != null) Object.Destroy(_copy);
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            if (_tuning != null) Object.Destroy(_tuning);
            DeviceHub.Instance?.Mixer.Reset();
        }

        void Use(District d)
        {
            _copy = Object.Instantiate(_original);
            _copy.chunks = new[] { d.entry }.Concat(d.chunks).Concat(new[] { d.exit }).ToArray();
            _copy.orderedChunks = true;
            if (d.obstacles != null && d.obstacles.Length > 0) _copy.obstacles = d.obstacles;
            if (d.pedestrians != null && d.pedestrians.Length > 0) _copy.pedestrians = d.pedestrians;
            _copy.hazards = new HazardPattern[0];
            _copy.districts = new District[0];
            // Main starts in the route's first district: switch the district director off and hide what it placed (its far
            // scenery), so the review shows only this district.
            foreach (DistrictDirector dd in Object.FindObjectsByType<DistrictDirector>(FindObjectsSortMode.None))
            {
                dd.enabled = false;
                foreach (Transform child in dd.transform)
                    if (_original.districts != null && System.Array.Exists(_original.districts, o => o != null && o.farScenery != null && child.name.StartsWith(o.farScenery.name)))
                        child.gameObject.SetActive(false);
            }
            _game.track.theme = _copy;
            RenderSettings.fogColor = d.fogColor;
            RenderSettings.ambientSkyColor = d.ambientSky;
            RenderSettings.ambientEquatorColor = d.ambientEquator;
            RenderSettings.ambientGroundColor = d.ambientGround;
            Shader.SetGlobalColor("_CS_FogColor", new Color(d.fogColor.r, d.fogColor.g, d.fogColor.b, 1f));
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { l.color = d.sunColor; l.intensity = d.sunIntensity; }
            if (d.farScenery != null) _far = Object.Instantiate(d.farScenery);
            _game.EnterReady();
        }

        // A district ramp on the road ahead, so the review shows it (the district system places them in the game).
        void PlaceRamp(District d, float x, float z)
        {
            RoadChunk c = _game.track.ChunkAt(z, out float start);
            Assert.NotNull(c);
            var ramp = Object.Instantiate(d.ramps[0].gameObject, c.transform);
            ramp.transform.localPosition = new Vector3(x, c.HeightAt(z - start), z - start);
        }

        static District Load(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<District>(path);
#else
            return null;
#endif
        }

        IEnumerator Shot(string id, string file, float x, float z, float lift = 0f)
        {
            var follow = Camera.main.GetComponent<FollowCamera>();
            _game.rider.ResetRider(new Vector3(x, 0f, z));
            if (_far != null) _far.transform.position = new Vector3(0f, _game.track.SurfaceHeightAt(z), z);
            yield return null;
            yield return null;
            follow.SnapToTarget();
            yield return null;
            follow.SnapToTarget();
            // Riding on a platform (the ramp physics is system B's): lift rider and camera together for the picture.
            if (lift > 0f) { _game.rider.transform.position += Vector3.up * lift; Camera.main.transform.position += Vector3.up * lift; }
            Capture(Camera.main, id, file);
        }

        [UnityTest, Explicit("Renders review images of Kings Park into art-src/districts/kingspark/previews")]
        public IEnumerator Screenshots_KingsPark()
        {
            District d = Load("Assets/_Project/Data/Districts/District_KingsPark.asset");
            Assert.NotNull(d);
            Use(d);
            PlaceRamp(d, -2.6f, 66f);
            // Chunks: entry 0, FraserAve 40, Wildflowers 80, Walkway 120, Lookout 160, BushDescent 200, exit 240.
            yield return Shot("kingspark", "01-entry.png", 0f, 1f);
            yield return Shot("kingspark", "02-fraser-ave.png", -1.5f, 44f);
            yield return Shot("kingspark", "03-wildflowers-dna-tower.png", 1.5f, 86f);
            yield return Shot("kingspark", "04-walkway-ahead.png", 2.4f, 114f);
            yield return Shot("kingspark", "05-walkway-on-top.png", 2.4f, 138f, 3f);
            yield return Shot("kingspark", "06-lookout.png", 2f, 163f);
            yield return Shot("kingspark", "07-descent-and-exit.png", 0f, 222f);
        }

        [UnityTest, Explicit("Renders review images of Mounts Bay Rd into art-src/districts/mountsbay/previews")]
        public IEnumerator Screenshots_MountsBay()
        {
            District d = Load("Assets/_Project/Data/Districts/District_MountsBay.asset");
            Assert.NotNull(d);
            Use(d);
            PlaceRamp(d, 2f, 172f);
            // 4-lane road (lane centres x = -6, -2, 2, 6). Chunks: entry 0 (widens), Towers 40, Narrows 80, JacobsLadder 120
            // (stairs at z 140), Escarpment 160, Inlet 200, Brewery 240, exit 280 (narrows).
            yield return Shot("mountsbay", "01-entry-widening.png", 0f, 1f);
            yield return Shot("mountsbay", "02-towers-and-palms.png", -2f, 42f);
            yield return Shot("mountsbay", "03-narrows-bridge.png", 2f, 84f);
            yield return Shot("mountsbay", "04-jacobs-ladder.png", 6f, 112f);
            yield return Shot("mountsbay", "04b-jacobs-ladder-near.png", 9.5f, 128f);
            yield return Shot("mountsbay", "05-escarpment-river.png", -10f, 162f);
            yield return Shot("mountsbay", "06-inlet-jump.png", -2f, 198f);
            yield return Shot("mountsbay", "07-brewery.png", 2f, 246f);
            yield return Shot("mountsbay", "08-exit-narrowing.png", 0f, 284f);
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
