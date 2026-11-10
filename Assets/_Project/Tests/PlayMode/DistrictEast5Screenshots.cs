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
    /// <summary>Review images of WOODBRIDGE and MIDLAND (east art agent 5) from the real chase camera on the real track:
    /// the ride starts in the district before WOODBRIDGE and goes forward through WOODBRIDGE's entry, its chunks and
    /// exit, into MIDLAND and out of it, so DistrictDirector applies the look, sky and far scenery as in the game.
    /// When the theme already lists both districts (after DistrictSetup) the real route is used. Before that (the art
    /// agent's own worktree) the test rides a runtime copy of the theme with a three-district route: a short copy of
    /// KINGS PARK standing in for the end of the trunk, then copies of the two districts marked as the east line. No asset is changed.
    /// The generator recycles what is behind the rider, so the shots are taken in route order only. Horizon views turn
    /// the chase camera to the side or lift it to check the far scenery. Images go to
    /// art-src/districts/&lt;id&gt;/previews.</summary>
    public class DistrictEast5Screenshots
    {
        const string WoodPath = "Assets/_Project/Data/Districts/District_Woodbridge.asset";
        const string MidPath = "Assets/_Project/Data/Districts/District_Midland.asset";
        const string StubPath = "Assets/_Project/Data/Districts/District_KingsPark.asset";

        Scene _main;
        GameController _game;
        GameTuning _tuning;
        TrackTheme _original, _copy;
        readonly List<Object> _temp = new List<Object>();

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
            if (_copy != null) Object.Destroy(_copy);
            foreach (Object o in _temp) if (o != null) Object.Destroy(o);
            _temp.Clear();
            DeviceHub.Instance?.Mixer.Reset();
        }

        static District Load(string path)
        {
#if UNITY_EDITOR
            var d = UnityEditor.AssetDatabase.LoadAssetAtPath<District>(path);
            Assert.NotNull(d, path + " (run East5Builder.BuildBatch)");
            return d;
#else
            Assert.Ignore("Editor only");
            return null;
#endif
        }

        District Copy(District source, string line, Vector2? length = null)
        {
            District d = Object.Instantiate(source);
            d.name = source.name;
            d.line = line;
            if (length.HasValue) d.length = length.Value;
            _temp.Add(d);
            return d;
        }

        /// <summary>The id of the district to start in (the one before WOODBRIDGE on the route that will be ridden).</summary>
        string PrepareRoute()
        {
            District[] real = _original.HasDistricts ? DistrictRoute.Sort(_original.districts) : new District[0];
            int wood = DistrictRoute.IndexOf(real, "woodbridge"), mid = DistrictRoute.IndexOf(real, "midland");
            if (wood > 0 && mid == wood + 1 && real[wood].line == "east" && real[wood - 1].line == "east")
                return real[wood - 1].id;
            // The art agent's worktree: a short stand-in trunk district, then the two districts as the whole east line.
            District cbd = Copy(Load(StubPath), "", new Vector2(120f, 120f));
            cbd.order = 50;
            _copy = Object.Instantiate(_original);
            _copy.districts = new[] { cbd, Copy(Load(WoodPath), "east"), Copy(Load(MidPath), "east") };
            _game.track.theme = _copy;
            return cbd.id;
        }

        sealed class ShotSpec
        {
            public readonly string Id, File, Chunk;
            public readonly float X, Z, Yaw, Pitch, Lift;
            public ShotSpec(string id, string file, string chunk, float x, float z, float yaw, float pitch, float lift) { Id = id; File = file; Chunk = chunk; X = x; Z = z; Yaw = yaw; Pitch = pitch; Lift = lift; }
            public bool Turned => Mathf.Abs(Yaw) > .01f || Mathf.Abs(Pitch) > .01f || Mathf.Abs(Lift) > .01f;
        }

        static ShotSpec W(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 0f) => new ShotSpec("woodbridge", file, "CHN_Woodbridge_" + chunk, x, z, yaw, pitch, lift);
        static ShotSpec M(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 0f) => new ShotSpec("midland", file, "CHN_Midland_" + chunk, x, z, yaw, pitch, lift);

        Dictionary<string, float> Scan(float from)
        {
            var starts = new Dictionary<string, float>();
            TrackGenerator track = _game.track;
            for (float z = Mathf.Max(0f, from); z < from + track.buildAhead + 40f; z += 5f)
            {
                RoadChunk c = track.ChunkAt(z, out float start);
                if (c == null || start < from) continue;
                string key = c.name.Replace("(Clone)", "").Trim();
                if (!starts.ContainsKey(key)) starts[key] = start;
            }
            return starts;
        }

        // Rides forward from the start district, taking each shot when its chunk is built (route order, never back).
        IEnumerator RideForward(string startDistrict, List<ShotSpec> shots)
        {
            _tuning.startDistrict = startDistrict;
            _game.EnterReady();
            yield return null;
            yield return null;
            var pending = new List<ShotSpec>(shots);
            float riderZ = 0f;
            int taken = 0;
            for (int step = 0; step < 60 && pending.Count > 0; step++)
            {
                float stepStart = riderZ;
                Dictionary<string, float> starts = Scan(riderZ - 25f);
                if (step % 3 == 0) Debug.Log("[East5Shots] at " + riderZ + ": " + string.Join(", ", starts.Select(kv => kv.Key + "@" + kv.Value)));
                foreach (ShotSpec s in pending.Where(s => starts.ContainsKey(s.Chunk)).OrderBy(s => starts[s.Chunk] + s.Z).ToList())
                {
                    float z = Mathf.Max(riderZ, starts[s.Chunk] + s.Z);
                    pending.Remove(s);
                    taken++;
                    yield return Shot(s, z);
                    riderZ = z;
                }
                // the next window overlaps this one (counted from where this step started, not from the last shot)
                riderZ = Mathf.Max(riderZ, stepStart + _game.track.buildAhead - 110f);
                _game.rider.ResetRider(new Vector3(0f, 0f, riderZ));
                for (int i = 0; i < 4; i++) yield return null;
            }
            foreach (ShotSpec s in pending) Debug.LogWarning("[East5Shots] " + s.Id + ": no " + s.Chunk + " for " + s.File);
            Assert.That(taken, Is.GreaterThanOrEqualTo(shots.Count - 2), "most review shots found their chunk");
        }

        IEnumerator Shot(ShotSpec shot, float z)
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
            if (!shot.Turned) { Capture(cam, shot.Id, shot.File); yield break; }
            // The chase camera turned to the side and / or lifted (same position, FOV and rendering), then put back.
            Vector3 pos = cam.transform.position;
            Quaternion rot = cam.transform.rotation;
            cam.transform.SetPositionAndRotation(pos + Vector3.up * (1.5f + shot.Lift), Quaternion.Euler(shot.Pitch, shot.Yaw, 0f));
            Capture(cam, shot.Id, shot.File);
            cam.transform.SetPositionAndRotation(pos, rot);
        }

        [UnityTest, Explicit("Renders review images of WOODBRIDGE and MIDLAND into art-src/districts/<id>/previews")]
        public IEnumerator Screenshots_WoodbridgeAndMidland_FromTheChaseCamera()
        {
            string start = PrepareRoute();
            yield return RideForward(start, new List<ShotSpec>
            {
                W("01-entry.png", "Entry", 0f, 4f),
                W("02-cottages.png", "Cottages", -1.5f, 0f),
                W("03-cottages-close.png", "Cottages", -5.5f, 12f, -35f, 6f),
                W("04-railway-side.png", "Cottages", 5.5f, 22f, 35f, 6f),
                W("05-station.png", "Station", 1.5f, -2f),
                W("06-station-close.png", "Station", 5.5f, 14f, 40f, 2f),
                W("07-woodbridge-house.png", "House", -1f, -6f),
                W("08-house-close.png", "House", -5.5f, 2f, -42f, -4f),
                W("09-horizon-left.png", "House", -2f, 30f, -60f, 2f, 3f),
                W("10-riverside.png", "Riverside", 0f, 0f),
                W("11-river-left.png", "Riverside", -5.5f, 16f, -50f, 6f),
                W("12-horizon-right.png", "Riverside", 2f, 26f, 55f, 0f, 3f),
                W("13-creek-kicker.png", "Creek", 0f, -3f),
                W("14-exit.png", "Exit", 0f, 4f),
                M("01-entry.png", "Entry", 0f, 4f),
                M("02-highway-shops.png", "Highway", -1.5f, 0f),
                M("03-shops-close.png", "Highway", -5.5f, 10f, -38f, 2f),
                M("04-yard-and-workshops.png", "Highway", 5f, 24f, 45f, 2f, 2f),
                M("05-town-hall.png", "TownHall", 0f, -8f),
                M("06-town-hall-close.png", "TownHall", -4f, -2f, -30f, -12f),
                M("07-centrepoint.png", "Centrepoint", -1.5f, -4f),
                M("08-centrepoint-close.png", "Centrepoint", -5.5f, 8f, -55f, -2f),
                M("09-station.png", "Station", 0f, -8f),
                M("10-station-prow.png", "Station", 3f, 0f, 25f, -10f),
                M("11-bus-interchange.png", "Station", -4f, 4f, -40f, 2f),
                M("12-horizon-ahead.png", "Station", 0f, 30f, 0f, -8f, 5f),
                M("13-terminus.png", "Terminus", 1.5f, -2f),
                M("14-buffer-stops.png", "Terminus", 5.5f, 12f, 35f, 6f),
                M("15-car-park.png", "Terminus", -5f, 2f, -38f, -8f),
                M("16-workshops.png", "Workshops", 0f, -4f),
                M("17-workshops-square.png", "Workshops", -3f, 25f, -35f, -6f),
                M("18-exit.png", "Exit", 0f, 4f),
                M("19-horizon-left.png", "Exit", -2f, 20f, -60f, -2f, 3f),
            });
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
                // district director set the sky and the city curl for this view.
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
