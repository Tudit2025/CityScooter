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
    /// <summary>4-lane boulevard sections in the real Main scene: In -> boulevard -> Out laid in order with continuous
    /// widths, the rider can ride out to the boulevard sidewalk and back and is guided through the narrowing, and hazard
    /// patterns never start on a transition.</summary>
    public class BoulevardTests
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
            Assert.IsTrue(_game.track.theme.HasBoulevard, "Theme_ComicDownhill has boulevard chunks (BoulevardBuilder)");
            _tuning = Object.Instantiate(_game.tuning);
            _tuning.fakeTurns = false;
            _tuning.districts = false; // v0.3: the comic street as before districts (from UWA, boulevards come only in West Perth)
            _game.tuning = _game.rider.tuning = _game.track.tuning = _tuning;
            var turns = _game.GetComponent<FakeTurnDirector>();
            if (turns != null) turns.tuning = _tuning;
            _game.EnterReady();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            if (_tuning != null) Object.Destroy(_tuning);
            DeviceHub.Instance?.Mixer.Reset();
        }

        TrackGenerator Track => _game.track;
        TrackTheme Theme => _game.track.theme;

        enum Part { Street, In, Wide, Out }

        // Pooled instances are named after their prefab ("CHN_BoulevardIn(Clone)").
        Part Kind(RoadChunk c)
        {
            if (c.name.StartsWith(Theme.boulevardIn.name)) return Part.In;
            if (c.name.StartsWith(Theme.boulevardOut.name)) return Part.Out;
            if (Theme.boulevardChunks.Any(b => c.name.StartsWith(b.name))) return Part.Wide;
            return Part.Street;
        }

        // Moves the (idle) rider along and records every chunk the generator lays down. Also checks that the rideable
        // width the rider is clamped to never jumps, inside transitions and across seams.
        IEnumerator March(float until, SortedDictionary<float, RoadChunk> laid, float step = 40f)
        {
            for (float z = 0f; z <= until; z += step)
            {
                _game.rider.transform.position = new Vector3(0f, Track.SurfaceHeightAt(z), z);
                yield return null;
                for (float ahead = z - 30f; ahead < z + Track.buildAhead; ahead += 20f)
                {
                    RoadChunk c = Track.ChunkAt(ahead, out float start);
                    if (c != null && ahead >= start && ahead < start + c.length) laid[Mathf.Round(start)] = c;
                }
                for (float s = z; s < z + step; s += .25f)
                    Assert.Less(Mathf.Abs(Track.RideableHalfWidthAt(s + .25f) - Track.RideableHalfWidthAt(s)), .1f, "rideable width step at z " + s);
            }
        }

        [UnityTest]
        public IEnumerator FixedSeedRun_LaysInBoulevardOut_InOrder_WithContinuousWidths()
        {
            Assert.AreNotEqual(0, Track.seed, "Main uses a fixed track seed");
            _game.EnterReady(); // rebuild with the copied tuning (default boulevard numbers)
            var laid = new SortedDictionary<float, RoadChunk>();
            yield return March(4000f, laid);

            var list = laid.ToList();
            int sections = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0)
                {
                    RoadChunk a = list[i - 1].Value, b = list[i].Value;
                    Assert.AreEqual(list[i - 1].Key + a.length, list[i].Key, .01f, "chunks are laid end to end");
                    Assert.AreEqual(a.HalfWidthAt(a.length), b.HalfWidthAt(0f), .001f, "road width jumps at z " + list[i].Key + " (" + a.name + " -> " + b.name + ")");
                    Assert.AreEqual(a.RideableHalfWidthAt(a.length), b.RideableHalfWidthAt(0f), .001f, "rideable width jumps at z " + list[i].Key);
                }
                if (Kind(list[i].Value) != Part.In) continue;
                sections++;
                Assert.GreaterOrEqual(list[i].Key, _tuning.boulevardMinDistance - 40f, "no boulevard in the first metres of a run");
                Assert.AreEqual(Part.Street, Kind(list[i - 1].Value), "a boulevard starts after a street chunk");
                StringAssert.StartsWith(Theme.chunks[Theme.chunks.Length - 1].name, list[i - 1].Value.name, "it starts where the ordered street cycle ends");
                int wide = 0, j = i + 1;
                while (j < list.Count && Kind(list[j].Value) == Part.Wide) { wide++; j++; }
                if (j >= list.Count) break; // the last section is still being laid
                Assert.That(wide, Is.InRange(_tuning.boulevardChunks.x, _tuning.boulevardChunks.y), "full-width chunks between In and Out");
                Assert.AreEqual(Part.Out, Kind(list[j].Value), "In -> boulevard -> Out");
                if (j + 1 < list.Count)
                {
                    Assert.AreEqual(Part.Street, Kind(list[j + 1].Value), "back to the street after Out");
                    StringAssert.StartsWith(Theme.chunks[0].name, list[j + 1].Value.name, "the street cycle starts again from its first chunk");
                }
            }
            Assert.That(sections, Is.InRange(1, 6), "a fixed-seed 4 km run has a boulevard section or a few");
            TestContext.WriteLine("boulevard sections in 4 km: " + sections);
        }

        [UnityTest]
        public IEnumerator HazardPatterns_NeverStartOnATransition_AndFitInsideTheBoulevard()
        {
            _tuning.hazardStartDistance = 0f; _tuning.hazardChanceStart = _tuning.hazardChanceMax = 1f; _tuning.hazardGap = 60f;
            _tuning.boulevardMinDistance = 0f; _tuning.boulevardChance = 1f; _tuning.boulevardGap = 0f;
            var scheduled = new List<(HazardPattern p, float start, RoadChunk chunk, Vector2 wide)>();
            void OnPattern(HazardPattern p, float start) => scheduled.Add((p, start, Track.ChunkAt(start, out _), Track.LastBoulevardRange));
            Track.PatternScheduled += OnPattern;
            try
            {
                _game.EnterReady();
                yield return March(6000f, new SortedDictionary<float, RoadChunk>());
            }
            finally { Track.PatternScheduled -= OnPattern; }

            Assert.Greater(Track.BoulevardSections, 3);
            int onBoulevard = 0;
            foreach (var s in scheduled)
            {
                Assert.IsFalse(s.chunk.ChangesWidth, s.p.name + " starts on the transition " + s.chunk.name + " at z " + s.start);
                bool wide = s.chunk.halfWidth > 4.5f;
                if (wide)
                {
                    onBoulevard++;
                    Assert.Contains(s.p, Theme.boulevardHazards, s.p.name);
                    Assert.LessOrEqual(s.start + s.p.length + 6f, s.wide.y + .01f, s.p.name + " runs out of the boulevard into the narrowing");
                    Assert.GreaterOrEqual(s.start, s.wide.x, s.p.name);
                }
                else Assert.LessOrEqual(s.p.roadHalfWidth, 4f, s.p.name + " (a 4-lane pattern) on a 2-lane street");
            }
            Assert.Greater(onBoulevard, 2, "hazard patterns are placed on the boulevard too");
            Assert.Greater(scheduled.Count - onBoulevard, 2);
            Assert.IsTrue(scheduled.Any(s => s.p.roadHalfWidth >= 8f), "the 4-lane traffic patterns are used");
        }

        void ClearTestPath()
        {
            foreach (Obstacle o in Track.GetComponentsInChildren<Obstacle>()) o.gameObject.SetActive(false);
        }

        IEnumerator Ride(float steer, float seconds, System.Action check = null)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                DeviceHub.Instance.Receive("boulevard-test", steer > 0f ? "st:100" : steer < 0f ? "st:-100" : "st:0");
                yield return null;
                check?.Invoke();
            }
        }

        void Teleport(float x, float z)
        {
            _game.rider.transform.position = new Vector3(x, Track.SurfaceHeightAt(x, z), z);
        }

        void AssertGrounded(string where)
        {
            Vector3 p = _game.rider.transform.position;
            Assert.AreEqual(RunState.Running, _game.State, "crashed " + where);
            Assert.LessOrEqual(Mathf.Abs(p.x), Track.RideableHalfWidthAt(p.z) - _game.rider.radius + .02f, "outside the rideable width " + where);
            Assert.AreEqual(Track.SurfaceHeightAt(p.x, p.z), p.y, .02f, "not on the surface " + where + " at " + p);
        }

        void AssertOnBoulevardSidewalk(int side)
        {
            Vector3 p = _game.rider.transform.position;
            Assert.AreEqual(12f, Track.RideableHalfWidthAt(p.z), .001f);
            Assert.Greater(p.x * side, 11.3f, "rides out to the boulevard sidewalk");
            Assert.AreEqual(.2f, p.y - Track.SurfaceHeightAt(p.z), .002f, "up on the kerb");
            var follow = Camera.main.GetComponent<FollowCamera>();
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(0f, Camera.main.transform.eulerAngles.y)), follow.maxYaw + .02f);
            Vector3 screen = Camera.main.WorldToViewportPoint(p + Vector3.up);
            Assert.That(screen.x, Is.InRange(.05f, .95f), "the chase camera keeps the rider in view at x " + p.x);
            Assert.That(screen.y, Is.InRange(.05f, .95f));
        }

        void SteadyBoulevardRun()
        {
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = _tuning.giftChance = 0f;
            _tuning.hazardChanceStart = _tuning.hazardChanceMax = 0f;
            _tuning.sidewalkPeople = Vector2.zero;
            _tuning.startSpeed = _tuning.maxSpeed = 12f; _tuning.maxSpeedMultiplier = 1f; _tuning.postRampMultiplierPerMinute = 0f;
            _tuning.boulevardMinDistance = 0f; _tuning.boulevardChance = 1f; _tuning.boulevardChunks = new Vector2Int(4, 4); _tuning.boulevardGap = 10000f;
            _game.StartRun();
            // The street cycle is 4 chunks, so the first boulevard starts at z 160: In 160..200, wide 200..360, Out 360..400.
            Assert.AreEqual(new Vector2(200f, 360f), Track.LastBoulevardRange);
        }

        [UnityTest]
        public IEnumerator Rider_RidesOutToTheBoulevardSidewalk_AndBack_AndIsGuidedThroughBothTransitions()
        {
            SteadyBoulevardRun();

            // Widening: hugging the right edge from the street sidewalk, the rider follows the kerb out and stays up on it.
            Teleport(7.6f, 150f);
            ClearTestPath();
            yield return Ride(1f, 5f, () => { AssertGrounded("in the widening"); Assert.AreEqual(.2f, _game.rider.transform.position.y - Track.SurfaceHeightAt(_game.rider.transform.position.z), .02f, "stays on the sidewalk"); });
            AssertOnBoulevardSidewalk(1);

            // Across all four lanes to the far sidewalk and back onto the road.
            yield return Ride(-1f, 4f, () => AssertGrounded("crossing the boulevard"));
            AssertOnBoulevardSidewalk(-1);
            yield return Ride(1f, .9f, () => AssertGrounded("back to the road"));
            Vector3 p = _game.rider.transform.position;
            Assert.That(p.x, Is.InRange(-8f, 0f), "back on the road");
            Assert.AreEqual(0f, p.y - Track.SurfaceHeightAt(p.z), .002f, "down off the kerb");

            // Narrowing: hugging the right edge, the garden bed guides the rider back to the street sidewalk. Never stuck,
            // never inside geometry, never more than a kerb step up or down in one frame.
            Teleport(11.5f, 345f);
            ClearTestPath();
            float lastY = _game.rider.transform.position.y;
            yield return Ride(1f, 6f, () =>
            {
                AssertGrounded("in the narrowing");
                float y = _game.rider.transform.position.y;
                Assert.Less(Mathf.Abs(y - lastY), .45f, "no sudden jump in height");
                lastY = y;
            });
            p = _game.rider.transform.position;
            Assert.Greater(p.z, 400f);
            Assert.AreEqual(8f, Track.RideableHalfWidthAt(p.z), .001f, "back on the 2-lane street");
            Assert.AreEqual(8f - _game.rider.radius, p.x, .02f, "at the street's sidewalk edge");
            Assert.AreEqual(.2f, p.y - Track.SurfaceHeightAt(p.z), .002f);

            // Riding on in the outer lane: the lane simply ends, the kerb slides under the rider (one kerb step up) and
            // the rider carries on along the street's sidewalk. No wall, no crash.
            SteadyBoulevardRun();
            Teleport(-6f, 350f);
            ClearTestPath();
            yield return Ride(0f, 5f, () => AssertGrounded("outer lane through the narrowing"));
            p = _game.rider.transform.position;
            Assert.Greater(p.z, 400f);
            Assert.AreEqual(-6f, p.x, .05f);
            Assert.AreEqual(.2f, p.y - Track.SurfaceHeightAt(p.z), .002f, "on the street's sidewalk");
        }

        // ------------------------------------------------------------------ review images (not run by default)

        [UnityTest, Explicit("Renders review images of the boulevard into art-src/boulevard (not committed)")]
        public IEnumerator Screenshots_TransitionsAndBoulevard_FromTheChaseCamera()
        {
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = .45f;
            _tuning.hazardChanceStart = _tuning.hazardChanceMax = 0f;
            _tuning.boulevardMinDistance = 0f; _tuning.boulevardChance = 1f; _tuning.boulevardChunks = new Vector2Int(4, 4); _tuning.boulevardGap = 10000f;
            _game.EnterReady();
            Assert.AreEqual(new Vector2(200f, 360f), Track.LastBoulevardRange);
            var follow = Camera.main.GetComponent<FollowCamera>();
            IEnumerator Shot(string file, float x, float z)
            {
                _game.rider.ResetRider(new Vector3(x, 0f, z));
                yield return null;
                yield return null;
                follow.SnapToTarget();
                yield return null;
                follow.SnapToTarget();
                Capture(Camera.main, file);
            }
            yield return Shot("01-approach.png", 0f, 140f);
            yield return Shot("02-widening.png", 1.5f, 168f);
            yield return Shot("03-widening-sidewalk.png", 8.5f, 182f);
            yield return Shot("04-boulevard-outer-lane.png", -6f, 212f);
            yield return Shot("05-boulevard-sidewalk.png", 10.6f, 238f);
            yield return Shot("06-boulevard-centre.png", 2f, 278f);
            yield return Shot("07-narrowing-ahead.png", 0f, 335f);
            yield return Shot("08-narrowing.png", -4f, 366f);
            yield return Shot("09-back-on-street.png", 0f, 392f);

            // Chunks behind the rider are recycled: rebuild the same (fixed-seed) track for the hazard shots.
            _game.EnterReady();
            HazardPattern Find(string name) => Theme.boulevardHazards.First(h => h.name == name);
            Track.PlacePattern(Find("HZB_RushHour"), 230f);
            yield return Shot("10-traffic.png", -6f, 200f);
            HazardPattern stroll = Theme.boulevardHazards.FirstOrDefault(h => h.name == "HZ_SidewalkStroll");
            if (stroll != null) { Track.PlacePattern(stroll, 255f); yield return Shot("11-street-pattern-on-boulevard.png", 0f, 240f); }
            Track.PlacePattern(Find("HZB_RoadworksKerbLane"), 300f);
            yield return Shot("12-roadworks.png", 2f, 286f);
        }

        static void Capture(Camera camera, string file)
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
                string folder = System.IO.Path.Combine(Application.dataPath, "../art-src/boulevard");
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
