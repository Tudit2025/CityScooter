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
    /// <summary>Review images of SUCCESS HILL, GUILDFORD and EAST GUILDFORD (east art agent 4) from the real chase camera
    /// on the real route: the ride starts in SUCCESS HILL (startDistrict on a runtime copy of the tuning) and goes forward
    /// through its entry, chunks and exit, into GUILDFORD and EAST GUILDFORD, so DistrictDirector applies the look, sky
    /// and far scenery as in the game (no asset is changed). The generator recycles what is behind the rider, so the
    /// shots are taken in route order only. "Horizon" and "close" views turn the chase camera to the side to check the
    /// far scenery band and the facades. Images go to art-src/districts/&lt;id&gt;/previews. Run DistrictSetup first.</summary>
    public class DistrictEast4Screenshots
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

        sealed class ShotSpec
        {
            public readonly string Id, File, Chunk;
            public readonly float X, Z, Yaw, Pitch, Lift;
            public ShotSpec(string id, string file, string chunk, float x, float z, float yaw, float pitch, float lift) { Id = id; File = file; Chunk = chunk; X = x; Z = z; Yaw = yaw; Pitch = pitch; Lift = lift; }
            public bool Turned => Mathf.Abs(Yaw) > .01f;
        }

        static ShotSpec S(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 1.5f) => new ShotSpec("successhill", file, "CHN_Successhill_" + chunk, x, z, yaw, pitch, lift);
        static ShotSpec G(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 1.5f) => new ShotSpec("guildford", file, "CHN_Guildford_" + chunk, x, z, yaw, pitch, lift);
        static ShotSpec E(string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 1.5f) => new ShotSpec("eastguildford", file, "CHN_Eastguildford_" + chunk, x, z, yaw, pitch, lift);

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
            Assert.IsTrue(_original.HasDistricts && _original.districts.Any(d => d != null && d.id == startDistrict), "run DistrictSetup: the theme lists " + startDistrict);
            _tuning.startDistrict = startDistrict;
            _game.track.startDistrictOverride = startDistrict;
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
                if (step == 0 || step % 3 == 0) Debug.Log("[East4Shots] at " + riderZ + ": " + string.Join(", ", starts.Select(kv => kv.Key + "@" + kv.Value)));
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
            foreach (ShotSpec s in pending) Debug.LogWarning("[East4Shots] " + s.Id + ": no " + s.Chunk + " for " + s.File);
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
            // The chase camera turned towards the side (same position, FOV and rendering), then put back.
            Vector3 pos = cam.transform.position;
            Quaternion rot = cam.transform.rotation;
            cam.transform.SetPositionAndRotation(pos + Vector3.up * shot.Lift, Quaternion.Euler(shot.Pitch, shot.Yaw, 0f));
            Capture(cam, shot.Id, shot.File);
            cam.transform.SetPositionAndRotation(pos, rot);
        }

        [UnityTest, Explicit("Renders review images of SUCCESS HILL, GUILDFORD and EAST GUILDFORD into art-src/districts/<id>/previews")]
        public IEnumerator Screenshots_SuccessHillGuildfordEastGuildford_FromTheChaseCamera()
        {
            yield return RideForward("successhill", new List<ShotSpec>
            {
                S("01-entry.png", "Entry", 0f, 6f),
                S("02-station.png", "Station", 0f, 0f),
                S("03-station-platform.png", "Station", 3f, 8f, 38f, 4f),
                S("04-federation-houses.png", "Station", -3f, 10f, -40f, 4f),
                S("05-reserve.png", "Reserve", 0f, 0f),
                S("06-stone-circle.png", "Reserve", -5f, 14f, -40f, 8f),
                S("07-lookout.png", "Lookout", -1.5f, 2f),
                S("08-horizon-left.png", "Lookout", -3f, 20f, -55f, 5f),
                S("09-boardwalk-and-gap.png", "Boardwalk", 0f, 0f),
                S("10-river-bridge.png", "RiverBridge", 0f, 0f),
                S("11-bridge-upstream.png", "RiverBridge", -3f, 14f, -50f, 6f),
                S("12-horizon-right.png", "RiverBridge", 3f, 14f, 50f, 4f),
                S("13-exit.png", "Exit", 0f, 4f),
                G("01-entry.png", "Entry", 0f, 6f),
                G("02-station-and-hotel.png", "Station", 0f, 0f),
                G("03-guildford-hotel.png", "Station", -2f, 6f, -42f, -4f),
                G("04-heritage-station.png", "Station", 3f, 8f, 40f, 3f),
                G("05-james-st.png", "JamesSt", 0f, 0f),
                G("06-antique-shops.png", "JamesSt", -5f, 8f, -38f, 4f),
                G("07-rose-and-crown.png", "RoseAndCrown", 0f, 0f),
                G("08-rose-and-crown-close.png", "RoseAndCrown", -4f, 10f, -42f, 2f),
                G("09-stirling-square.png", "StirlingSquare", 0f, 0f),
                G("10-st-matthews.png", "StirlingSquare", -3f, 8f, -45f, -2f),
                G("11-post-office.png", "PostOffice", 0f, -4f),
                G("12-post-office-close.png", "PostOffice", -3f, -2f, -32f, -10f),
                G("13-old-courthouse.png", "PostOffice", -4f, 22f, -42f, 4f),
                G("14-exit-vines.png", "Exit", 0f, 0f),
                G("15-horizon-left.png", "Exit", -3f, 10f, -55f, 4f),
                E("01-entry.png", "Entry", 0f, 6f),
                E("02-stables.png", "Stables", 0f, 0f),
                E("03-stables-close.png", "Stables", -4f, 8f, -42f, 4f),
                E("04-station-ramp.png", "Station", 0f, 0f),
                E("05-station-close.png", "Station", 3f, 8f, 40f, 4f),
                E("06-school.png", "School", 0f, 0f),
                E("07-school-close.png", "School", -4f, 8f, -42f, 0f),
                E("08-chapel-approach.png", "Chapel", 0f, -8f),
                E("09-chapel.png", "Chapel", -3f, 2f, -30f, -12f),
                E("10-chapel-flank.png", "Chapel", -4f, 16f, -62f, -10f),
                E("11-ovals.png", "Ovals", 0f, 0f),
                E("12-horizon-left.png", "Ovals", -3f, 16f, -55f, 4f),
                E("13-exit.png", "Exit", 0f, 4f),
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
