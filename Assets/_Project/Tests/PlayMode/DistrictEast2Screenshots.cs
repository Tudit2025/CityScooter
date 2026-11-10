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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>Review images of MT LAWLEY, MAYLANDS and MELTHAM (east line art agent 2) from the real chase camera on a
    /// real ride. The theme is copied at runtime and given a short route (a lead-in district, then the three districts in
    /// order, as copies with the CBD skyline hidden, the way the district system sets them at integration), so the shots
    /// work before DistrictSetup has put the districts into the theme and no asset is changed. The ride goes forward
    /// through each district's entry, chunks and exit, so DistrictDirector applies the look, sky and far scenery as in
    /// the game. The generator recycles what is behind the rider, so shots are taken in route order only. Side views
    /// turn the chase camera (same position, FOV and rendering) to check facades and the far-scenery horizon.
    /// Images go to art-src/districts/&lt;id&gt;/previews.</summary>
    public class DistrictEast2Screenshots
    {
        const string Folder = "Assets/_Project/Data/Districts/";
        Scene _main;
        GameController _game;
        GameTuning _tuning;
        TrackTheme _original, _theme;
        readonly List<Object> _copies = new List<Object>();

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
            foreach (Object o in _copies) if (o != null) Object.Destroy(o);
            _copies.Clear();
            DeviceHub.Instance?.Mixer.Reset();
        }

        sealed class ShotSpec
        {
            public readonly string Id, File, Chunk;
            public readonly float X, Z, Yaw, Pitch, Lift;
            public ShotSpec(string id, string file, string chunk, float x, float z, float yaw, float pitch, float lift) { Id = id; File = file; Chunk = chunk; X = x; Z = z; Yaw = yaw; Pitch = pitch; Lift = lift; }
            public bool Turned => Mathf.Abs(Yaw) > .01f || Mathf.Abs(Pitch) > .01f;
        }

        static ShotSpec S(string id, string file, string chunk, float x, float z, float yaw = 0f, float pitch = 0f, float lift = 1.5f)
        {
            string pascal = char.ToUpperInvariant(id[0]) + id.Substring(1);
            return new ShotSpec(id, file, "CHN_" + pascal + "_" + chunk, x, z, yaw, pitch, lift);
        }

        // A runtime route: a short lead-in (the theme's first district), then the wanted districts as copies with the
        // system-owned sky fields set the way the east line has them from Mt Lawley on (no CBD skyline).
        void UseRoute(params string[] ids)
        {
            Assert.IsTrue(_original.HasDistricts, "the comic theme has districts (run DistrictSetup)");
            _theme = Object.Instantiate(_original);
            _copies.Add(_theme);
            var route = new List<District>();
            District lead = Object.Instantiate(_original.districts[0]);
            lead.name = _original.districts[0].name;
            lead.order = 1; lead.line = ""; lead.length = new Vector2(80f, 80f);
            _copies.Add(lead);
            route.Add(lead);
            int order = 2;
            foreach (string id in ids)
            {
                string pascal = char.ToUpperInvariant(id[0]) + id.Substring(1);
                var asset = AssetDatabase.LoadAssetAtPath<District>(Folder + "District_" + pascal + ".asset");
                Assert.NotNull(asset, "District_" + pascal + ".asset (run East2Builder.BuildBatch)");
                District copy = Object.Instantiate(asset);
                copy.name = asset.name;
                copy.order = order++; copy.line = ""; copy.skyline = 0f; copy.skyYaw = 180f; copy.skySize = 1f; copy.skyHeight = 0f;
                _copies.Add(copy);
                route.Add(copy);
            }
            _theme.districts = route.ToArray();
            _game.track.theme = _theme;
            _tuning.startDistrict = lead.id;
        }

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

        // Rides forward from the lead-in, taking each shot when its chunk is built (route order, never back).
        IEnumerator RideForward(List<ShotSpec> shots)
        {
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
                if (step % 3 == 0) Debug.Log("[East2Shots] at " + riderZ + ": " + string.Join(", ", starts.Select(kv => kv.Key + "@" + kv.Value)));
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
            foreach (ShotSpec s in pending) Debug.LogWarning("[East2Shots] " + s.Id + ": no " + s.Chunk + " for " + s.File);
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

        static List<ShotSpec> MtLawleyShots() => new List<ShotSpec>
        {
            S("mtlawley", "01-entry.png", "Entry", 0f, 4f),
            S("mtlawley", "02-station.png", "Station", 0f, 0f),
            S("mtlawley", "03-station-platform.png", "Station", -5.5f, 8f, -48f, 10f, 1f),
            S("mtlawley", "04-federation.png", "Federation", 0f, 0f),
            S("mtlawley", "05-federation-houses.png", "Federation", -2f, 8f, -40f, 6f, .5f),
            S("mtlawley", "06-horizon-right.png", "Station", 2f, 6f, 62f, 0f, 3.5f),
            S("mtlawley", "07-beaufort.png", "Beaufort", 0f, 0f),
            S("mtlawley", "08-beaufort-shops.png", "Beaufort", 2f, 6f, 38f, 2f, .5f),
            S("mtlawley", "09-astor.png", "Astor", 1.5f, -6f),
            S("mtlawley", "10-astor-facade.png", "Astor", 3f, 6f, -42f, -8f, .5f),
            S("mtlawley", "11-stage-ramp.png", "Astor", -2.6f, 17f),
            S("mtlawley", "12-avenue.png", "Avenue", 0f, 0f),
            S("mtlawley", "13-villa.png", "Avenue", 2f, 2f, -40f, 0f, .5f),
            S("mtlawley", "14-exit.png", "Exit", 0f, 6f),
            S("mtlawley", "15-horizon-left.png", "Exit", -2f, 28f, -60f, 0f, 3f),
        };

        static List<ShotSpec> MaylandsShots() => new List<ShotSpec>
        {
            S("maylands", "01-entry.png", "Entry", 0f, 4f),
            S("maylands", "02-station.png", "Station", 0f, 0f),
            S("maylands", "03-station-building.png", "Station", -5.5f, 4f, -45f, 6f, 1f),
            S("maylands", "04-peninsula-hotel.png", "Station", -1f, 8f, 38f, -6f, .5f),
            S("maylands", "05-eighth-ave.png", "EighthAve", 0f, 0f),
            S("maylands", "06-the-rise.png", "EighthAve", -2f, 12f, 40f, -2f, .5f),
            S("maylands", "07-brickworks.png", "Brickworks", 0f, -4f),
            S("maylands", "08-kiln-and-chimney.png", "Brickworks", -2.6f, 12f, 52f, -10f, .5f),
            S("maylands", "08b-pug-mill.png", "Brickworks", 2.6f, 2f, -45f, -6f, .5f),
            S("maylands", "09-river.png", "River", 0f, 0f),
            S("maylands", "10-horizon-right.png", "River", 2f, 18f, 60f, 0f, 3f),
            S("maylands", "11-horizon-left.png", "River", -2f, 22f, -60f, 0f, 3f),
            S("maylands", "12-boardwalk-gap.png", "Boardwalk", 0f, 1f),
            S("maylands", "13-exit.png", "Exit", 0f, 6f),
        };

        static List<ShotSpec> MelthamShots() => new List<ShotSpec>
        {
            S("meltham", "01-entry.png", "Entry", 0f, 4f),
            S("meltham", "02-station.png", "Station", 0f, 0f),
            S("meltham", "03-station-building.png", "Station", -5.5f, 10f, -50f, 4f, 1f),
            S("meltham", "04-bungalows.png", "Bungalows", 0f, 0f),
            S("meltham", "05-bungalow-close.png", "Bungalows", -2f, 8f, -40f, 6f, .5f),
            S("meltham", "06-back-yards.png", "Bungalows", 2f, 22f, 42f, 6f, .5f),
            S("meltham", "07-deli.png", "Deli", 0f, -4f),
            S("meltham", "08-deli-close.png", "Deli", 2f, 8f, -40f, 2f, .5f),
            S("meltham", "09-park.png", "Park", 0f, 0f),
            S("meltham", "10-horizon-right.png", "Park", 2f, 20f, 60f, 0f, 3f),
            S("meltham", "11-bridge.png", "Bridge", 0f, -6f),
            S("meltham", "12-horizon-left.png", "Bridge", -2f, 24f, -60f, 0f, 3f),
            S("meltham", "13-exit.png", "Exit", 0f, 6f),
        };

        [UnityTest, Explicit("Renders review images of MT LAWLEY into art-src/districts/mtlawley/previews")]
        public IEnumerator Screenshots_MtLawley_FromTheChaseCamera()
        {
            UseRoute("mtlawley");
            yield return RideForward(MtLawleyShots());
        }

        [UnityTest, Explicit("Renders review images of MAYLANDS into art-src/districts/maylands/previews")]
        public IEnumerator Screenshots_Maylands_FromTheChaseCamera()
        {
            UseRoute("maylands");
            yield return RideForward(MaylandsShots());
        }

        [UnityTest, Explicit("Renders review images of MELTHAM into art-src/districts/meltham/previews")]
        public IEnumerator Screenshots_Meltham_FromTheChaseCamera()
        {
            UseRoute("meltham");
            yield return RideForward(MelthamShots());
        }

        [UnityTest, Explicit("Renders review images of all three districts on one ride (the boundaries between them too)")]
        public IEnumerator Screenshots_AllThree_OnOneRide()
        {
            UseRoute("mtlawley", "maylands", "meltham");
            var shots = MtLawleyShots();
            shots.AddRange(MaylandsShots());
            shots.AddRange(MelthamShots());
            yield return RideForward(shots);
        }

        /// <summary>What a player sees at speed: a frame every 20 m straight down the road through one district.</summary>
        IEnumerator RideThrough(string id)
        {
            UseRoute(id);
            _game.EnterReady();
            yield return null;
            yield return null;
            string pascal = char.ToUpperInvariant(id[0]) + id.Substring(1);
            float riderZ = 0f, start = -1f;
            for (int step = 0; step < 30 && start < 0f; step++)
            {
                foreach (var kv in Scan(riderZ - 25f)) if (kv.Key == "CHN_" + pascal + "_Entry") start = kv.Value;
                if (start >= 0f) break;
                riderZ += 60f;
                _game.rider.ResetRider(new Vector3(0f, 0f, riderZ));
                for (int i = 0; i < 4; i++) yield return null;
            }
            Assert.GreaterOrEqual(start, 0f, id + ": the entry chunk was built");
            float[] lanes = { 0f, -2.6f, 2.6f, 0f, 5.6f, -5.6f };
            for (int i = 0; i < 16; i++)
                yield return Shot(new ShotSpec(id, "ride-" + (i + 1).ToString("00") + ".png", "", lanes[i % lanes.Length], 0f, 0f, 0f, 0f), start - 20f + i * 20f);
        }

        [UnityTest, Explicit("Ride-through frames of MT LAWLEY (every 20 m) into art-src/districts/mtlawley/previews")]
        public IEnumerator RideThrough_MtLawley() { yield return RideThrough("mtlawley"); }

        [UnityTest, Explicit("Ride-through frames of MAYLANDS (every 20 m) into art-src/districts/maylands/previews")]
        public IEnumerator RideThrough_Maylands() { yield return RideThrough("maylands"); }

        [UnityTest, Explicit("Ride-through frames of MELTHAM (every 20 m) into art-src/districts/meltham/previews")]
        public IEnumerator RideThrough_Meltham() { yield return RideThrough("meltham"); }

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
#endif
