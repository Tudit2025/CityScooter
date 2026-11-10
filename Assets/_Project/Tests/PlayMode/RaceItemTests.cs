using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityScooter.Devices;
using CityScooter.Gameplay;
using CityScooter.Protocol;
using CityScooter.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>
    /// v0.5 item race (docs/item-race.md), fed like a real board. The "radio" is fake: what the other boards would
    /// broadcast goes straight in as "rx:i:..." through DeviceHub.Receive (as AMY's board would pass it on), and what
    /// the game asks its board to send is read back from DeviceHub.LineSent ("tx:i:...").
    /// The same runtime districts and fast rider as RaceTests; the chunks have spawn slots, so a test can turn
    /// obstacles and gifts on to see that the item boxes leave the road alone.
    /// </summary>
    public class RaceItemTests
    {
        const string Board = "item-usb";
        readonly List<Object> _made = new List<Object>();
        readonly List<(double time, string line)> _sent = new List<(double, string)>();
        GameTuning _tuning;
        TrackTheme _theme;
        GameController _game;
        RaceDirector _race;
        RaceItems _items;
        PoliceChase _police;
        DeviceHub _hub;

        T Keep<T>(T o) where T : Object { _made.Add(o); return o; }

        RoadChunk Chunk(string name)
        {
            var go = Keep(new GameObject(name));
            go.transform.position = new Vector3(0f, -500f, 0f);
            var c = go.AddComponent<RoadChunk>();
            c.length = 40f; c.endHeight = -3.1471f; c.halfWidth = 4f; c.sidewalkWidth = 4f; c.sidewalkHeight = .2f; c.weight = 1f;
            // one row of three slots in the middle of the chunk (the lanes the generator uses)
            for (int i = -1; i <= 1; i++)
            {
                var slot = new GameObject("Slot" + i);
                slot.transform.SetParent(go.transform, false);
                slot.transform.localPosition = new Vector3(i * 2.6f, 0f, 20f);
                slot.AddComponent<SpawnSlot>().kind = SpawnKind.Any;
            }
            return c;
        }

        District MakeDistrict(string id, int order, RoadChunk[] chunks)
        {
            var d = Keep(ScriptableObject.CreateInstance<District>());
            d.id = id; d.displayName = id.ToUpperInvariant(); d.order = order; d.length = new Vector2(400f, 400f);
            d.chunks = chunks; d.orderedChunks = false;
            d.fogColor = d.ambientSky = d.ambientEquator = d.ambientGround = d.sunColor = Color.grey; d.sunIntensity = 1f;
            d.blendMeters = 60f;
            return d;
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _hub = DeviceHub.Instance;
            if (_hub == null) Assert.Ignore("no DeviceHub (Bootstrap)");
            _hub.Mixer.Reset();
            _tuning = Keep(ScriptableObject.CreateInstance<GameTuning>());
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = 0f;
            _tuning.giftChance = _tuning.jumpObstacleChance = 0f;
            _tuning.hazardChanceStart = _tuning.hazardChanceMax = 0f;
            _tuning.sidewalkPeople = Vector2.zero;
            _tuning.safeStartDistance = 0f;
            _tuning.boulevardSections = false;
            _tuning.startSpeed = 40f;
            _tuning.maxSpeed = 60f;
            _tuning.fakeTurns = false;
            _tuning.rampChance = 0f;
            _tuning.startDistrict = "uwa";
            _tuning.copFirstDelay = 60f; // the test theme has no police car anyway

            _theme = Keep(ScriptableObject.CreateInstance<TrackTheme>());
            _theme.chunks = new[] { Chunk("S1") };
            _theme.districts = new[]
            {
                MakeDistrict("uwa", 10, new[] { Chunk("U1"), Chunk("U2"), Chunk("U3") }),
                MakeDistrict("kingspark", 20, new[] { Chunk("K1"), Chunk("K2"), Chunk("K3") }),
            };

            _sent.Clear();
            _hub.LineSent += OnSent;
            if (Camera.main == null)
            {
                var cam = Keep(new GameObject("Item test camera")).AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            _game = GameSystemsFactory.Create(_tuning, _theme).GetComponent<GameController>();
            _game.track.seed = 42;
            Keep(_game.gameObject);
            yield return null; // GameController.Start adds the RaceDirector and RaceItems
            _race = _game.GetComponent<RaceDirector>();
            _items = _game.GetComponent<RaceItems>();
            _police = _game.GetComponent<PoliceChase>();
            Assert.NotNull(_race);
            Assert.NotNull(_items, "GameController adds RaceItems");
            Assert.AreSame(_race, _items.race);
            _items.SeedRandom(2026);
            _game.EnterReady();
            yield return null;
        }

        void OnSent(string target, string line) => _sent.Add((Time.unscaledTimeAsDouble, line));

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_hub != null) _hub.LineSent -= OnSent;
            Time.timeScale = 1f;
            for (int i = _made.Count - 1; i >= 0; i--) if (_made[i] != null) Object.Destroy(_made[i]);
            _made.Clear();
            yield return null;
            _hub?.Mixer.Reset();
        }

        // ---- the fake board and the fake radio
        void Say(string line) => _hub.Receive(Board, line);
        /// <summary>Another board broadcast this; AMY's board heard it and passes it on.</summary>
        void Radio(string payload) => _hub.Receive(Board, "rx:" + payload);
        List<string> Sent(string prefix) => _sent.Select(s => s.line).Where(l => l.StartsWith(prefix)).ToList();
        int Count(string line) => _sent.Count(s => s.line == line);

        IEnumerator Wait(float seconds, System.Action everyFrame = null)
        {
            double end = Time.unscaledTimeAsDouble + seconds, nextRace = 0d;
            while (Time.unscaledTimeAsDouble < end)
            {
                if (Time.unscaledTimeAsDouble >= nextRace) { Say("race:AMY:12"); nextRace = Time.unscaledTimeAsDouble + 0.9; }
                everyFrame?.Invoke();
                yield return null;
            }
        }

        IEnumerator WaitUntil(System.Func<bool> done, float timeout, System.Action everyFrame = null)
        {
            double end = Time.unscaledTimeAsDouble + timeout, nextRace = 0d;
            while (!done() && Time.unscaledTimeAsDouble < end)
            {
                if (Time.unscaledTimeAsDouble >= nextRace) { Say("race:AMY:12"); nextRace = Time.unscaledTimeAsDouble + 0.9; }
                everyFrame?.Invoke();
                yield return null;
            }
        }

        // boxes off: the test hands the items out itself (nothing is picked up on the way)
        IEnumerator StartRace(int seed = 4821, string start = "kingspark", int metres = 5000, bool boxes = false)
        {
            _items.boxesOn = boxes;
            Say("race:AMY:12");
            yield return null;
            Assert.AreEqual(RaceState.Lobby, _race.State);
            Radio("go:" + seed + ":" + start + ":" + metres);
            Assert.AreEqual(RaceState.Countdown, _race.State);
            yield return Wait(RaceDirector.CountdownSeconds + 0.2f);
            Assert.AreEqual(RaceState.Racing, _race.State);
            Assert.AreEqual(RunState.Running, _game.State);
            yield return null;
        }

        // Other riders, at these distances from mine (positive = ahead), heard right now.
        void Riders(params (string name, float gap)[] riders)
        {
            foreach ((string name, float gap) in riders)
                Radio("p:" + name + ":" + Mathf.RoundToInt(Mathf.Max(0f, _game.rider.Distance + gap)) + ":0");
        }

        // ---------------------------------------------------------------- boxes and the road

        static List<string> Chunks(TrackGenerator track, float fromZ)
        {
            var list = new List<string>();
            float last = float.MinValue;
            for (float z = fromZ; z < fromZ + track.buildAhead - 10f; z += 5f)
            {
                RoadChunk c = track.ChunkAt(z, out float start);
                if (c == null || Mathf.Approximately(start, last)) continue;
                last = start;
                list.Add(c.name.Replace("(Clone)", "") + "@" + start.ToString("0.0"));
            }
            return list;
        }

        // Every obstacle and gift standing on the road, as text.
        List<string> Things()
        {
            var list = new List<string>();
            foreach (Obstacle o in _game.track.GetComponentsInChildren<Obstacle>(false)) list.Add("O " + o.name.Replace("(Clone)", "") + " " + Round(o.transform.position));
            foreach (Gift g in _game.track.GetComponentsInChildren<Gift>(false)) list.Add("G " + Round(g.transform.position));
            list.Sort(string.CompareOrdinal);
            return list;
        }

        static string Round(Vector3 p) => p.x.ToString("0.00") + "," + p.y.ToString("0.00") + "," + p.z.ToString("0.00");

        List<string> Boxes() => _items.BoxesShown().Select(b => b.Row + "/" + b.Lane + " " + b.transform.position.x.ToString("0.00") + "," + b.transform.position.z.ToString("0.00")).OrderBy(s => s, System.StringComparer.Ordinal).ToList();

        [UnityTest]
        public IEnumerator Boxes_StandInRowsOfThree_TheSameForTheSameGo_AndLeaveTheRoadAlone()
        {
            // A busy road for this one: obstacles and gifts in the chunks' slots.
            _tuning.obstacleChanceStart = _tuning.obstacleChanceMax = 0.6f;
            _tuning.giftChance = 0.8f;
            yield return StartRace(4821, "kingspark", 2000, true);
            _game.rider.PassThroughUntil = Time.time + 1e6f;
            yield return null;
            float startZ = _race.StartZ;
            List<string> chunksA = Chunks(_game.track, _game.startPosition.z - 2f), thingsA = Things(), boxesA = Boxes();
            Assert.Greater(thingsA.Count(t => t.StartsWith("O ")), 2, "there are obstacles on this road");
            Assert.Greater(thingsA.Count(t => t.StartsWith("G ")), 2, "and gifts");

            // The first row: 150 m after the start, one box in each of three lanes; the next one 250 m on is not built yet.
            Assert.AreEqual(150f, RaceItems.RowDistance(0));
            Assert.AreEqual(400f, RaceItems.RowDistance(1));
            Assert.AreEqual(3, boxesA.Count, string.Join(" | ", boxesA));
            ItemBox[] row = _items.BoxesShown().OrderBy(b => b.Lane).ToArray();
            for (int lane = 0; lane < 3; lane++)
            {
                Assert.AreEqual(0, row[lane].Row);
                Assert.AreEqual(RaceItems.BoxLanes[lane], row[lane].transform.position.x, 0.001f);
                Assert.AreEqual(startZ + 150f, row[lane].transform.position.z, 0.001f);
                Assert.AreEqual(_game.track.SurfaceHeightAt(RaceItems.BoxLanes[lane], startZ + 150f) + RaceItems.BoxHeight, row[lane].transform.position.y, 0.01f);
                Assert.IsNull(row[lane].GetComponentInChildren<Collider>(), "a box can never be crashed into");
            }
            CollectionAssert.AreEqual(new[] { -2.6f, 0f, 2.6f }, RaceItems.BoxLanes);
            // Rows only well before the finish line (2000 m): 150, 400 ... 1900.
            Assert.IsTrue(_items.RowExists(7), "1900 m");
            Assert.IsFalse(_items.RowExists(8), "2150 m is past the line");

            // Ride on: the second row appears where it should.
            yield return WaitUntil(() => _game.rider.Distance > 230f, 10f);
            Assert.IsTrue(_items.BoxesShown().Any(b => b.Row == 1 && Mathf.Approximately(b.transform.position.z, startZ + 400f)), string.Join(" | ", Boxes()));

            // The same go: again: the same road, the same boxes.
            _race.BackToLobby();
            yield return null;
            Radio("go:4821:kingspark:2000");
            yield return Wait(RaceDirector.CountdownSeconds + 0.2f);
            yield return null;
            yield return null;
            CollectionAssert.AreEqual(chunksA, Chunks(_game.track, _game.startPosition.z - 2f));
            CollectionAssert.AreEqual(thingsA, Things(), "the same obstacles and gifts");
            CollectionAssert.AreEqual(boxesA, Boxes(), "the same boxes");

            // Without the items the road is exactly the same: the boxes are laid over it and use none of its random numbers.
            _items.enabled = false;
            _race.BackToLobby();
            yield return null;
            Radio("go:4821:kingspark:2000");
            yield return Wait(RaceDirector.CountdownSeconds + 0.2f);
            yield return null;
            yield return null;
            Assert.AreEqual(0, Boxes().Count, "no boxes with the items off");
            CollectionAssert.AreEqual(chunksA, Chunks(_game.track, _game.startPosition.z - 2f));
            CollectionAssert.AreEqual(thingsA, Things(), "the road does not depend on the boxes");
            _items.enabled = true;
        }

        [UnityTest]
        public IEnumerator RidingThroughABox_GivesOneItem_TellsTheBoard_AndThatRowIsGone()
        {
            yield return StartRace(boxes: true);
            Assert.IsNull(_items.Held);
            Assert.IsTrue(_game.hud.ItemSlotShown, "the item slot is on the screen in a race");
            Assert.AreEqual("", _game.hud.ItemSlotText);
            Say("use"); // empty hands: nothing happens
            yield return null;
            Assert.AreEqual(1, _items.IgnoredUses);
            Assert.AreEqual(0, _items.Used);
            Assert.AreEqual(0, Sent("item:").Count);

            // The rider is in the middle lane: straight through the middle box of the first row.
            yield return WaitUntil(() => _game.rider.Distance > 165f, 10f);
            Assert.IsNotNull(_items.Held, "took the box at 150 m");
            Assert.IsTrue(Messages.IsItem(_items.Held));
            CollectionAssert.AreEqual(new[] { "item:" + _items.Held }, Sent("item:"), "the board hears which item");
            Assert.IsTrue(_items.RowTaken(0));
            Assert.IsFalse(_items.BoxesShown().Any(b => b.Row == 0), "the whole row is gone for me");
            yield return null;
            Assert.AreEqual(_items.Held.ToUpperInvariant(), _game.hud.ItemSlotText, "the slot shows it");
            Assert.AreEqual(RunState.Running, _game.State);
            Assert.IsTrue(_game.rider.Running, "a box is not an obstacle");
            // Alone in the race: the "middle" table (docs: practice gets everything but bolt and police).
            Assert.AreEqual(RaceItems.PickItem(1, 1, new System.Random(2026)), _items.Held, "the item comes from the seeded random");

            // With full hands the next row gives nothing and stays.
            string held = _items.Held;
            yield return WaitUntil(() => _game.rider.Distance > 415f, 15f);
            Assert.AreEqual(held, _items.Held);
            Assert.IsFalse(_items.RowTaken(1));
            Assert.AreEqual(1, _items.Picked);
            Assert.AreEqual(1, Sent("item:").Count, "items never use themselves");
        }

        [UnityTest]
        public IEnumerator RidingBetweenTwoBoxes_GivesNothing()
        {
            yield return StartRace(boxes: true);
            // Half way between the middle box (x = 0) and the right one (x = 2.6).
            Vector3 p = _game.rider.transform.position;
            _game.rider.transform.position = new Vector3(1.3f, _game.track.SurfaceHeightAt(1.3f, p.z), p.z);
            yield return WaitUntil(() => _game.rider.Distance > 165f, 10f);
            Assert.AreEqual(1.3f, _game.rider.transform.position.x, 0.05f);
            Assert.IsNull(_items.Held, "rode between two boxes");
            Assert.IsFalse(_items.RowTaken(0));
            Assert.AreEqual(0, Sent("item:").Count);
            // And the left box, from its edge.
            p = _game.rider.transform.position;
            _game.rider.transform.position = new Vector3(-2.6f + 0.9f, p.y, p.z);
            yield return WaitUntil(() => _game.rider.Distance > 415f, 15f);
            Assert.IsNotNull(_items.Held, "near enough to the left box at 400 m");
            Assert.IsTrue(_items.RowTaken(1));
        }

        [UnityTest]
        public IEnumerator WhichItem_DependsOnThePlace_FixedSeed()
        {
            // The table itself, with a fixed seed.
            var counts = new Dictionary<string, int>();
            void Draw(int place, int count, int n)
            {
                counts.Clear();
                var rng = new System.Random(12345);
                for (int i = 0; i < n; i++)
                {
                    string item = RaceItems.PickItem(place, count, rng);
                    counts.TryGetValue(item, out int c);
                    counts[item] = c + 1;
                }
            }
            void Share(string item, int percent) => Assert.AreEqual(percent, 100f * counts[item] / 20000f, 1.5f, item);

            Draw(1, 5, 20000); // first: oil 50, shield 30, boost 20
            CollectionAssert.AreEquivalent(new[] { "oil", "shield", "boost" }, counts.Keys);
            Share("oil", 50); Share("shield", 30); Share("boost", 20);
            Draw(3, 5, 20000); // middle: rocket 35, boost 25, oil 20, shield 20
            CollectionAssert.AreEquivalent(new[] { "rocket", "boost", "oil", "shield" }, counts.Keys);
            Share("rocket", 35); Share("boost", 25); Share("oil", 20); Share("shield", 20);
            Draw(5, 5, 20000); // last: rocket 30, police 25, boost 25, bolt 20
            CollectionAssert.AreEquivalent(new[] { "rocket", "police", "boost", "bolt" }, counts.Keys);
            Share("rocket", 30); Share("police", 25); Share("boost", 25); Share("bolt", 20);
            Draw(2, 2, 20000); // two riders: the second is "last"
            CollectionAssert.AreEquivalent(new[] { "rocket", "police", "boost", "bolt" }, counts.Keys);
            Draw(1, 1, 20000); // alone (practice): "middle"
            CollectionAssert.AreEquivalent(new[] { "rocket", "boost", "oil", "shield" }, counts.Keys);
            foreach ((int place, int count) in new[] { (1, 4), (2, 4), (4, 4), (1, 1), (2, 2) })
                Assert.AreEqual(100, Messages.ItemNames.Sum(i => RaceItems.Chance(place, count, i)), "the chances add up to 100");
            // The same seed gives the same items.
            var a = new System.Random(7); var b = new System.Random(7);
            for (int i = 0; i < 50; i++) Assert.AreEqual(RaceItems.PickItem(2, 4, a), RaceItems.PickItem(2, 4, b));

            // In the game: BEN is ahead, so AMY is last of two and gets something from the last rider's table.
            yield return StartRace(boxes: true);
            _items.SeedRandom(99);
            yield return WaitUntil(() => _items.Held != null, 10f, () => { if (Time.frameCount % 10 == 0) Riders(("BEN", 60f)); });
            Assert.AreEqual(2, _race.MyPlace(out int riders));
            Assert.AreEqual(2, riders);
            Assert.AreEqual(RaceItems.PickItem(2, 2, new System.Random(99)), _items.Held);
            CollectionAssert.Contains(new[] { "rocket", "police", "boost", "bolt" }, _items.Held);
        }

        // ---------------------------------------------------------------- the six items

        [UnityTest]
        public IEnumerator Boost_IsStrongerThanBo_NeedsNoCooldown_AndShowsAFlame()
        {
            yield return StartRace();
            yield return Wait(0.5f);
            Say("bo"); // the normal boost: now it is recharging
            yield return Wait(2.3f);
            Assert.IsFalse(_game.rider.BoostReady, "bo is still recharging");
            Assert.IsTrue(_items.Give("boost"));
            Assert.AreEqual("item:boost", Sent("item:").Last());
            Assert.IsFalse(_items.Give("shield"), "one item at a time");
            float before = _game.rider.Speed;
            Say("use");
            yield return null;
            yield return null;
            Assert.IsNull(_items.Held);
            Assert.AreEqual("item:none", Sent("item:").Last(), "used up");
            Assert.AreEqual("boost", _items.LastUsed);
            Assert.IsTrue(_game.rider.ItemBoosting);
            Assert.IsTrue(_items.FlameShown, "a flame behind the scooter");
            Assert.IsTrue(_game.hud.ItemSpeedLinesShown, "speed lines on the screen");
            StringAssert.Contains("BOOST", _game.hud.ItemStatusText);
            yield return Wait(1.5f);
            float normal = Mathf.Lerp(_tuning.startSpeed, _game.rider.CurrentMaxSpeed, _tuning.Difficulty01(_game.rider.Distance));
            Assert.Greater(_game.rider.Speed, before + 5f);
            Assert.Greater(_game.rider.Speed, normal * 1.45f, "about 1.6 times the normal speed (bo is 1.35)");
            Assert.AreEqual(0, Sent("tx:").Count, "a boost is nobody else's business");
            yield return Wait(RaceItems.BoostSeconds - 1.5f + 0.2f);
            Assert.IsFalse(_game.rider.ItemBoosting, "3 s");
            yield return null;
            Assert.IsFalse(_items.FlameShown);
            Assert.IsFalse(_game.hud.ItemSpeedLinesShown);
        }

        [UnityTest]
        public IEnumerator Shield_StopsOneItem_ThenItIsGone()
        {
            yield return StartRace();
            yield return Wait(0.3f);
            _items.Give("shield");
            Say("use");
            yield return null;
            yield return null;
            Assert.IsTrue(_items.ShieldOn);
            Assert.AreEqual(RaceItems.ShieldSeconds, _items.ShieldLeft, 0.2f);
            Assert.IsTrue(_items.ShieldShown, "a bubble round the rider");
            StringAssert.Contains("SHIELD", _game.hud.ItemStatusText);
            Assert.AreEqual(0, Sent("tx:").Count);

            Radio("i:BEN:bolt:1");
            yield return null;
            CollectionAssert.AreEqual(new[] { "blocked:bolt" }, Sent("blocked:"));
            Assert.AreEqual(0, Sent("hit:").Count);
            Assert.IsFalse(_game.rider.ItemSlowed, "the bolt did nothing");
            Assert.IsFalse(_items.ShieldOn, "the shield is used up");
            Assert.AreEqual("BLOCKED!", _game.hud.FlashText);
            yield return null;
            Assert.IsFalse(_items.ShieldShown);

            Radio("i:BEN:bolt:2"); // the next one gets through
            yield return null;
            CollectionAssert.AreEqual(new[] { "hit:bolt" }, Sent("hit:"));
            Assert.IsTrue(_game.rider.ItemSlowed);
        }

        [UnityTest]
        public IEnumerator Shield_StopsOneCrash_AndWearsOffAfter8Seconds()
        {
            yield return StartRace();
            yield return Wait(0.3f);
            _items.Give("shield");
            Say("use");
            yield return null;
            Vector3 p = _game.rider.transform.position;
            float z = p.z + 25f;
            Assert.NotNull(_game.track.PlaceAt("placeholder-obstacle-1", new Vector3(p.x, _game.track.SurfaceHeightAt(p.x, z), z)), "a barrier in my lane");
            yield return WaitUntil(() => _game.rider.transform.position.z > z + 5f, 3f);
            Assert.IsTrue(_game.rider.Running, "the shield took the crash");
            Assert.AreEqual(0f, _race.PenaltyLeft);
            Assert.AreEqual(0, _race.Crashes);
            CollectionAssert.AreEqual(new[] { "blocked:crash" }, Sent("blocked:"));
            Assert.AreEqual(0, Count("crash"), "no crash for the board");
            Assert.IsFalse(_items.ShieldOn);

            // Without a shield the same barrier is a crash (the 3 s stop of a race).
            yield return Wait(1.2f); // the pass-through after a block is over
            p = _game.rider.transform.position;
            z = p.z + 25f;
            _game.track.PlaceAt("placeholder-obstacle-1", new Vector3(p.x, _game.track.SurfaceHeightAt(p.x, z), z));
            yield return WaitUntil(() => !_game.rider.Running, 3f);
            Assert.IsFalse(_game.rider.Running);
            Assert.AreEqual(1, Count("crash"));
            Assert.AreEqual(1, _race.Crashes);
            yield return Wait(RaceDirector.CrashPenaltySeconds + 0.3f);
            Assert.IsTrue(_game.rider.Running);

            // A shield nobody needed is gone after 8 s.
            _items.Give("shield");
            Say("use");
            yield return Wait(RaceItems.ShieldSeconds - 0.6f);
            Assert.IsTrue(_items.ShieldOn);
            yield return Wait(0.9f);
            Assert.IsFalse(_items.ShieldOn);
            Radio("i:BEN:bolt:4");
            yield return null;
            Assert.AreEqual("hit:bolt", Sent("hit:").Last(), "no shield any more");
        }

        // tx: lines come 3 times, 0.15 s apart, the same every time.
        void AssertSentThreeTimes(string line)
        {
            List<double> times = _sent.Where(s => s.line == line).Select(s => s.time).ToList();
            Assert.AreEqual(RaceItems.SendRepeats, times.Count, line + " is sent 3 times (sent: " + string.Join(", ", Sent("tx:")) + ")");
            for (int i = 1; i < times.Count; i++)
                Assert.That(times[i] - times[i - 1], Is.InRange(RaceItems.SendSpacing - 0.06, RaceItems.SendSpacing + 0.12), "0.15 s apart");
        }

        [UnityTest]
        public IEnumerator Oil_GoesOnTheRoadBehindMe_AndToTheOthersThreeTimes()
        {
            yield return StartRace();
            yield return Wait(1f);
            int sequence = _items.NextSequence;
            _items.Give("oil");
            float distance = _game.rider.Distance;
            Say("use");
            yield return null;
            Assert.AreEqual(1, _items.Slicks.Count, "it appears in my own game too");
            RaceItems.Slick mine = _items.Slicks[0];
            Assert.IsTrue(mine.Mine);
            Assert.That(mine.Distance, Is.InRange(distance - RaceItems.OilBehindMetres - 1f, distance - RaceItems.OilBehindMetres + 6f), "6 m behind me");
            Assert.AreEqual(Mathf.Round(_game.rider.transform.position.x), mine.Lane, 0.001f, "in my lane");
            string line = "tx:" + RadioMessage.ItemOil("AMY", Mathf.RoundToInt(mine.Distance), Mathf.RoundToInt(mine.Lane), sequence);
            Assert.AreEqual(line, Sent("tx:").First());
            yield return Wait(0.6f);
            AssertSentThreeTimes(line);
            Assert.AreEqual((sequence + 1) % 10, _items.NextSequence, "the next item gets the next number");
            Assert.AreEqual(0, Sent("hit:").Count, "nobody slips on the oil they just dropped");
            Assert.IsTrue(_game.rider.Running);
            yield return null;
            Assert.AreEqual(1, _items.SlicksShown, "drawn on the road behind me");
        }

        [UnityTest]
        public IEnumerator OilFromSomeoneElse_AppearsAhead_AndSlipsOnce()
        {
            yield return StartRace();
            yield return Wait(0.5f);
            float d = _game.rider.Distance;
            Radio("i:BEN:oil:" + Mathf.RoundToInt(d - 60f) + ":0:1"); // far behind me: it can never matter here
            Assert.AreEqual(0, _items.Slicks.Count);
            Radio("i:BEN:oil:" + Mathf.RoundToInt(d + 150f) + ":3:2"); // ahead, but in the lane at x = 3
            Radio("i:BEN:oil:" + Mathf.RoundToInt(d + 90f) + ":0:3"); // ahead in my lane
            Radio("i:BEN:oil:" + Mathf.RoundToInt(d + 90f) + ":0:3"); // the copy
            Assert.AreEqual(2, _items.Slicks.Count);
            yield return null;
            yield return null;
            Assert.AreEqual(2, _items.SlicksShown);
            float before = 0f;
            yield return WaitUntil(() => _game.rider.ItemSlowed, 4f, () => { if (!_game.rider.ItemSlowed) before = _game.rider.Speed; });
            Assert.IsTrue(_game.rider.ItemSlowed, "rode onto the oil");
            CollectionAssert.AreEqual(new[] { "hit:oil" }, Sent("hit:"), "hit:oil when I ride onto it, not before");
            Assert.AreEqual("oil", _items.SlowedBy);
            Assert.AreEqual("SLIP!", _game.hud.FlashText);
            Assert.AreEqual(1, _items.Slicks.Count, "used up on my screen");
            yield return Wait(0.8f);
            Assert.Less(_game.rider.Speed, before * (RaceItems.OilSlowFactor + 0.12f), "down to 40% of the speed");
            Assert.IsTrue(_game.rider.Running, "slipping is not a crash");
            Assert.AreEqual(0, Count("crash"));
            Assert.AreEqual(0f, _race.PenaltyLeft);
            yield return Wait(RaceItems.OilSlowSeconds - 0.8f + 0.15f);
            Assert.IsFalse(_game.rider.ItemSlowed, "1.5 s");
            yield return Wait(1.6f);
            Assert.Greater(_game.rider.Speed, before * 0.7f, "back up to speed soon after");
            // The slick in the other lane is ridden past.
            yield return WaitUntil(() => _game.rider.Distance > d + 170f, 5f);
            Assert.AreEqual(1, Sent("hit:").Count);
            Assert.AreEqual(1, _items.Slicks.Count, "still lying there for 60 s");
        }

        [UnityTest]
        public IEnumerator Rocket_GoesToTheRiderJustAhead_OrNowhere()
        {
            yield return StartRace();
            yield return Wait(0.5f);
            // Alone: nobody ahead. The item is used up, nothing is sent.
            _items.Give("rocket");
            Say("use");
            yield return null;
            Assert.IsNull(_items.Held);
            Assert.AreEqual("item:none", Sent("item:").Last());
            Assert.AreEqual(0, Sent("tx:").Count);
            Assert.IsNull(_items.LastTarget);
            Assert.AreEqual("no one ahead", _game.hud.FlashSubText);

            // CHLOE far ahead, BEN just ahead, DAN behind: BEN gets it.
            Riders(("CHLOE", 200f), ("BEN", 40f), ("DAN", -30f));
            yield return null;
            Assert.AreEqual("BEN", _items.RivalAhead().Name);
            int sequence = _items.NextSequence;
            _items.Give("rocket");
            Say("use");
            yield return null;
            Assert.AreEqual("BEN", _items.LastTarget);
            string line = "tx:i:AMY:rocket:BEN:" + sequence;
            yield return Wait(0.6f, () => { if (Time.frameCount % 10 == 0) Riders(("CHLOE", 200f), ("BEN", 40f), ("DAN", -30f)); });
            AssertSentThreeTimes(line);
            Assert.AreEqual(0, Sent("hit:").Count, "my own rocket does nothing to me");
            Assert.IsTrue(_game.rider.Running);
        }

        [UnityTest]
        public IEnumerator RocketForMe_WarnsForASecond_ThenStopsMeFor1point5Seconds()
        {
            yield return StartRace();
            yield return Wait(0.5f);
            Radio("i:BEN:rocket:CHLOE:1"); // not for me: only a line on the screen
            yield return null;
            Assert.AreEqual(0f, _items.RocketIn);
            StringAssert.Contains("BEN", _game.hud.FlashText);
            StringAssert.Contains("CHLOE: ROCKET!", _game.hud.FlashText);

            Radio("i:BEN:rocket:AMY:2");
            yield return null;
            Assert.Greater(_items.RocketIn, 0.5f, "a warning first");
            Assert.AreEqual("BEN", _items.RocketFrom);
            Assert.AreEqual("ROCKET!", _game.hud.FlashText);
            StringAssert.Contains("ROCKET!", _game.hud.ItemStatusText);
            Assert.IsTrue(_game.rider.Running);
            Assert.AreEqual(0, Sent("hit:").Count);
            yield return Wait(RaceItems.RocketWarnSeconds - 0.25f);
            Assert.IsTrue(_game.rider.Running, "still riding during the warning");
            yield return Wait(0.45f);
            Assert.IsFalse(_game.rider.Running, "knocked off");
            CollectionAssert.AreEqual(new[] { "hit:rocket" }, Sent("hit:"));
            Assert.AreEqual(0, Count("crash"), "the board hears hit:rocket, not crash");
            Assert.AreEqual(0, _race.Crashes);
            Assert.AreEqual(RaceItems.RocketStopSeconds, _race.PenaltySeconds, 0.001f);
            Assert.That(_race.PenaltyLeft, Is.InRange(0.9f, 1.5f));
            Assert.AreEqual("BOOM!", _game.hud.FlashText);
            StringAssert.Contains("+1.5 s", _game.hud.RacePenaltyText);
            Assert.AreEqual(RunState.Running, _game.State, "the race goes on");
            yield return Wait(RaceItems.RocketStopSeconds + 0.1f);
            Assert.IsTrue(_game.rider.Running, "back on the scooter after 1.5 s");
            Assert.AreEqual(RaceState.Racing, _race.State);

            // A shield put up during the warning stops it.
            Radio("i:BEN:rocket:AMY:3");
            yield return Wait(0.3f);
            _items.Give("shield");
            Say("use");
            yield return Wait(RaceItems.RocketWarnSeconds);
            Assert.IsTrue(_game.rider.Running);
            CollectionAssert.AreEqual(new[] { "blocked:rocket" }, Sent("blocked:"));
            Assert.AreEqual(1, Sent("hit:").Count);
        }

        [UnityTest]
        public IEnumerator Bolt_SlowsEveryoneElse_For3Seconds()
        {
            yield return StartRace();
            yield return Wait(0.5f);
            // Mine: it goes out, nothing happens to me.
            int sequence = _items.NextSequence;
            _items.Give("bolt");
            Say("use");
            yield return Wait(0.6f);
            AssertSentThreeTimes("tx:i:AMY:bolt:" + sequence);
            Assert.IsFalse(_game.rider.ItemSlowed);

            // BEN's: I am slowed to half speed for 3 s.
            float before = _game.rider.Speed;
            Radio("i:BEN:bolt:7");
            yield return null;
            CollectionAssert.AreEqual(new[] { "hit:bolt" }, Sent("hit:"));
            Assert.IsTrue(_game.rider.ItemSlowed);
            Assert.AreEqual("bolt", _items.SlowedBy);
            Assert.AreEqual("ZAP!", _game.hud.FlashText);
            Assert.Greater(_game.hud.ItemScreenFlashNow, 0.2f, "the whole screen flashes");
            yield return null;
            Assert.IsTrue(_items.BoltShown, "a bolt over the rider");
            StringAssert.Contains("SLOW", _game.hud.ItemStatusText);
            yield return Wait(1f);
            Assert.That(_game.rider.Speed, Is.InRange(before * 0.4f, before * 0.6f), "half speed");
            Assert.IsTrue(_game.rider.Running);
            yield return Wait(RaceItems.BoltSlowSeconds - 1f + 0.15f);
            Assert.IsFalse(_game.rider.ItemSlowed, "3 s");
            Assert.AreEqual(0, Count("crash"));
        }

        [UnityTest]
        public IEnumerator Police_GoAfterTheLeader_AndForMeItIs3StarsFor15Seconds()
        {
            yield return StartRace();
            yield return Wait(0.5f);
            // Sending: CHLOE leads, so she gets them (BEN is nearer, but second).
            Riders(("CHLOE", 200f), ("BEN", 40f));
            yield return null;
            Assert.AreEqual("CHLOE", _items.Leader());
            int sequence = _items.NextSequence;
            _items.Give("police");
            Say("use");
            yield return null;
            Assert.AreEqual("tx:i:AMY:police:CHLOE:" + sequence, Sent("tx:").First());
            Assert.AreEqual(0, _police.Wanted, "sending the police does not get me wanted");

            // Receiving: wanted, 3 stars, the usual police messages.
            _sent.Clear();
            Radio("i:BEN:police:CHLOE:1");
            yield return null;
            Assert.AreEqual(0, _police.Wanted, "not for me");
            Radio("i:BEN:police:AMY:2");
            yield return null;
            Assert.AreEqual(RaceItems.PoliceStars, _police.Wanted);
            Assert.IsTrue(_police.Chasing);
            CollectionAssert.AreEqual(new[] { "hit:police" }, Sent("hit:"));
            CollectionAssert.Contains(Sent("wanted:"), "wanted:3");
            CollectionAssert.Contains(Sent("chase:"), "chase:1");
            Assert.AreEqual("POLICE!", _game.hud.FlashText);
            Assert.That(_items.PoliceLeft, Is.InRange(RaceItems.PoliceSeconds - 0.5f, RaceItems.PoliceSeconds));
            Assert.IsTrue(_game.rider.Running);
            yield return Wait(RaceItems.PoliceSeconds - 1.5f);
            Assert.Greater(_police.Wanted, 0, "still wanted after 13.5 s");
            yield return Wait(2f);
            Assert.AreEqual(0, _police.Wanted, "the police leave after 15 s");
            Assert.IsFalse(_police.Chasing);
            Assert.AreEqual("wanted:0", Sent("wanted:").Last());
            Assert.AreEqual(0f, _items.PoliceLeft);
            Assert.IsTrue(_game.rider.Running);
        }

        [UnityTest]
        public IEnumerator WhenILead_ThePoliceGoAfterTheSecond_AndAloneNowhere()
        {
            yield return StartRace();
            yield return Wait(2.5f);
            _items.Give("police");
            Say("use");
            yield return null;
            Assert.AreEqual(0, Sent("tx:").Count, "alone: nobody to send them after");
            Assert.AreEqual("no one ahead", _game.hud.FlashSubText);
            Riders(("DAN", -80f), ("BEN", -20f));
            yield return null;
            Assert.AreEqual(1, _race.MyPlace(out _));
            Assert.AreEqual("BEN", _items.Leader(), "I lead: the second rider");
            Assert.IsNull(_items.RivalAhead(), "and a rocket would find nobody");
        }

        // ---------------------------------------------------------------- the radio: copies, my own name

        [UnityTest]
        public IEnumerator Copies_AreDropped_BySenderAndItemNumber_For20Seconds()
        {
            yield return StartRace();
            yield return Wait(0.5f);
            Radio("i:BEN:bolt:4");
            Radio("i:BEN:bolt:4"); // the board sends everything 3 times
            yield return Wait(0.2f);
            Radio("i:BEN:bolt:4");
            yield return null;
            Assert.AreEqual(1, _items.Heard);
            Assert.AreEqual(2, _items.Duplicates);
            Assert.AreEqual(1, Sent("hit:").Count, "one bolt, not three");

            Radio("i:CHLOE:bolt:4"); // the same number from someone else is another item
            yield return null;
            Assert.AreEqual(2, _items.Heard);
            Assert.AreEqual(2, Sent("hit:").Count);
            Radio("i:BEN:bolt:5"); // and BEN's next one too
            yield return null;
            Assert.AreEqual(3, Sent("hit:").Count);

            Radio("i:AMY:bolt:1"); // my own name: never me (my board cannot hear itself; a copycat gets nowhere)
            Radio("i:AMY:rocket:AMY:2");
            Radio("i:BEN:banana:AMY:6"); // not an item: dropped
            Radio("i:BEN:rocket:AMY"); // broken: dropped
            yield return null;
            Assert.AreEqual(3, _items.Heard);
            Assert.AreEqual(0f, _items.RocketIn);

            // 20 s later BEN's numbers have gone round: 4 is a new item again.
            yield return Wait(RaceItems.DedupSeconds - 1.5f);
            Radio("i:BEN:bolt:4");
            yield return null;
            Assert.AreEqual(3, _items.Heard, "19 s: still the same one");
            yield return Wait(2f);
            Radio("i:BEN:bolt:4");
            yield return null;
            Assert.AreEqual(4, _items.Heard, "after 20 s it counts again");
            Assert.AreEqual(4, Sent("hit:").Count);
        }

        // ---------------------------------------------------------------- police events of my own program in a race

        [UnityTest]
        public IEnumerator DoStars_WorksInARace_RoadEventsDoNot_AndBeingArrestedIsA3SecondStop()
        {
            var triggers = _game.GetComponent<ProgramTriggers>();
            yield return StartRace();
            yield return Wait(0.5f);
            int dropped = triggers.Dropped, done = triggers.Done;
            Say("do:stars:3");
            yield return null;
            Assert.AreEqual(3, _police.Wanted, "set the wanted stars works in a race");
            Assert.IsTrue(_police.Chasing);
            Assert.AreEqual(done + 1, triggers.Done);
            CollectionAssert.Contains(Sent("wanted:"), "wanted:3");
            CollectionAssert.Contains(Sent("chase:"), "chase:1");
            yield return Wait(1.1f); // one trigger a second per board
            foreach (string road in new[] { "do:car", "do:turn:L", "do:ramp", "do:police", "do:manhole" })
            {
                Say(road);
                yield return null;
            }
            Assert.AreEqual(dropped + 5, triggers.Dropped, "events that change the road stay ignored");
            Assert.AreEqual(done + 1, triggers.Done);
            StringAssert.Contains("not in race mode", CaptionText());

            // Pull over: ARRESTED. In a race that is the 3 s stop of a crash, then the police are gone.
            _sent.Clear();
            float distance = 0f;
            yield return WaitUntil(() => Count("arrested") > 0, 12f, () => { Say("br"); distance = _game.rider.Distance; });
            Assert.AreEqual(1, Count("arrested"), "sent: " + string.Join(",", _sent.Select(s => s.line).Where(l => !l.StartsWith("pos:") && !l.StartsWith("spd:"))));
            Assert.AreEqual(RunState.Running, _game.State, "the ride does not end");
            Assert.AreEqual(RaceState.Racing, _race.State);
            Assert.IsFalse(_game.rider.Running, "stopped");
            Assert.AreEqual(0, _police.Wanted, "wanted is 0 again");
            Assert.IsFalse(_police.Chasing);
            List<string> order = _sent.Select(s => s.line).Where(l => l == "wanted:0" || l == "chase:0" || l == "crash" || l == "arrested").ToList();
            CollectionAssert.AreEqual(new[] { "wanted:0", "chase:0", "crash", "arrested" }, order, "the usual messages, arrested last");
            Assert.That(_race.PenaltyLeft, Is.InRange(2.5f, 3f));
            Assert.AreEqual("ARRESTED!", _game.hud.FlashText);
            yield return Wait(RaceDirector.CrashPenaltySeconds + 0.3f);
            Assert.IsTrue(_game.rider.Running, "back on the scooter after 3 s");
            Assert.GreaterOrEqual(_game.rider.Distance, distance - 0.5f, "from the same spot");
            Assert.AreEqual(0, _police.Wanted);

            // Crashing while wanted: the same stop, and the police leave too.
            Say("do:chase");
            yield return null;
            Assert.AreEqual(1, _police.Wanted);
            _sent.Clear();
            Vector3 p = _game.rider.transform.position;
            float z = p.z + 25f;
            yield return Wait(RaceDirector.RespawnPassSeconds); // just respawned: things are passed through for a moment
            p = _game.rider.transform.position;
            z = p.z + 25f;
            _game.track.PlaceAt("placeholder-obstacle-1", new Vector3(p.x, _game.track.SurfaceHeightAt(p.x, z), z));
            yield return WaitUntil(() => !_game.rider.Running, 3f);
            yield return null;
            Assert.AreEqual(0, _police.Wanted);
            order = _sent.Select(s => s.line).Where(l => l == "wanted:0" || l == "chase:0" || l == "crash").ToList();
            CollectionAssert.AreEqual(new[] { "crash", "wanted:0", "chase:0" }, order);
            Assert.AreEqual(0, Count("busted"), "a barrier is not the police car");
        }

        string CaptionText()
        {
            foreach (UnityEngine.UI.Text t in _game.hud.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (t.name == "Say") return t.text;
            return "";
        }

        [UnityTest]
        public IEnumerator OpenStars_HappensAtTheStartOfEveryRace_AndOnPracticeRides()
        {
            var triggers = _game.GetComponent<ProgramTriggers>();
            Say("race:AMY:12");
            yield return null;
            Assert.AreEqual(RaceState.Lobby, _race.State);
            double next = 0d;
            void Program() // "on start: set the wanted stars to 2", said again about once a second
            {
                if (Time.unscaledTimeAsDouble < next) return;
                next = Time.unscaledTimeAsDouble + 0.9;
                Say("open:stars:2");
                Say("open:car"); // a road event: never kept in race mode
            }
            yield return Wait(0.3f, Program);
            Assert.AreEqual(1, triggers.Opening, "only the police event is kept");
            StringAssert.Contains("not in race mode", CaptionText());

            // A practice ride in the lobby.
            Say("jp");
            yield return Wait(0.2f, Program);
            Assert.AreEqual(RunState.Running, _game.State);
            Assert.AreEqual(RaceState.Lobby, _race.State);
            yield return Wait(triggers.queuedDelay + 0.3f, Program);
            Assert.AreEqual(2, _police.Wanted, "the lobby's practice rides too");

            // The race itself.
            Radio("go:4821:kingspark:5000");
            yield return Wait(RaceDirector.CountdownSeconds + 0.1f, Program);
            Assert.AreEqual(RaceState.Racing, _race.State);
            Assert.AreEqual(0, _police.Wanted, "a new ride starts clean");
            yield return Wait(triggers.queuedDelay + 0.3f, Program);
            Assert.AreEqual(2, _police.Wanted, "every race starts with it");
            Assert.IsTrue(_police.Chasing);
        }

        [UnityTest]
        public IEnumerator CrossingTheLine_EndsThePoliceAndTheItems()
        {
            yield return StartRace(4821, "uwa", 300, true);
            Say("do:stars:2");
            _items.Give("shield");
            yield return null;
            Assert.AreEqual(2, _police.Wanted);
            Assert.IsTrue(_items.BoxesShown().Any(b => b.Row == 0), "a row at 150 m of a 300 m race");
            Assert.IsFalse(_items.RowExists(1), "none at 400 m");
            _game.rider.PassThroughUntil = Time.time + 1e6f;
            yield return WaitUntil(() => _race.State == RaceState.Finished, 15f);
            Assert.AreEqual(RaceState.Finished, _race.State);
            yield return null;
            Assert.AreEqual(0, _police.Wanted, "no police after the line");
            Assert.IsFalse(_police.Chasing);
            Assert.IsNull(_items.Held);
            Assert.AreEqual("item:none", Sent("item:").Last(), "the item in hand is gone, and the board knows");
            Assert.IsFalse(_game.hud.ItemSlotShown);
            Assert.AreEqual(0, _items.BoxesShown().Count());
            Radio("i:BEN:bolt:1");
            Radio("i:BEN:police:AMY:2");
            Say("do:stars:5");
            yield return Wait(0.5f);
            Assert.AreEqual(0, Sent("hit:").Count, "nothing hits a rider who has finished");
            Assert.AreEqual(0, _police.Wanted);
            yield return WaitUntil(() => !_game.rider.Running, 12f);
            Assert.IsFalse(_game.rider.Running, "rolled to a stop past the line");
        }

        [UnityTest]
        public IEnumerator PracticeRideInTheLobby_HasBoxesAndItems_NormalPlayHasNone()
        {
            // Normal play: no boxes, "use" does nothing, radio items are ignored.
            _game.StartRun();
            yield return null;
            yield return null;
            Assert.AreEqual(0, _items.BoxesShown().Count());
            Assert.IsFalse(_game.hud.ItemSlotShown);
            Assert.IsFalse(_items.Give("rocket"));
            _hub.Receive(Board, "use");
            _hub.Receive(Board, "rx:i:BEN:bolt:1");
            yield return null;
            Assert.AreEqual(0, _sent.Count(s => s.line.StartsWith("item:") || s.line.StartsWith("hit:") || s.line.StartsWith("tx:")));
            Assert.IsFalse(_game.rider.ItemSlowed);

            // Race mode: the lobby, then a practice ride.
            _sent.Clear();
            Say("race:AMY:12");
            yield return null;
            Assert.AreEqual(RaceState.Lobby, _race.State);
            yield return Wait(0.2f);
            Assert.IsTrue(_game.hud.ItemSlotShown);
            Say("jp");
            yield return Wait(0.2f);
            Assert.AreEqual(RunState.Running, _game.State);
            Assert.AreEqual(3, _items.BoxesShown().Count(), "boxes on practice rides too");
            Assert.IsTrue(_items.RowExists(40), "every 250 m, as far as the ride goes");
            yield return WaitUntil(() => _items.Held != null, 10f);
            Assert.IsNotNull(_items.Held);
            CollectionAssert.Contains(new[] { "rocket", "boost", "oil", "shield" }, _items.Held, "practice: the middle table");
            Radio("i:BOT1:bolt:3"); // the simulator's robots: so "when an item hits me" can be tried without a race
            yield return null;
            CollectionAssert.AreEqual(new[] { "hit:bolt" }, Sent("hit:"));

            // Race mode ends (no race: for 10 s): the item is dropped and everything goes away.
            double end = Time.unscaledTimeAsDouble + RaceDirector.ModeTimeoutSeconds + 0.5;
            while (Time.unscaledTimeAsDouble < end) yield return null;
            Assert.AreEqual(RaceState.Off, _race.State);
            Assert.IsNull(_items.Held);
            Assert.AreEqual("item:none", Sent("item:").Last());
            yield return null;
            Assert.AreEqual(0, _items.BoxesShown().Count());
            Assert.IsFalse(_game.hud.ItemSlotShown);
        }
    }
}
