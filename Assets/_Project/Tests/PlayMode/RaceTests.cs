using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityScooter.Devices;
using CityScooter.Game;
using CityScooter.Gameplay;
using CityScooter.Track;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>
    /// v0.4 radio race (docs/radio-race.md "实现约定 v1"), fed like a real board: lines go in through DeviceHub.Receive
    /// ("race:AMY:12", "rx:p:BEN:120:2", "rx:go:...") and what the game sends back is read from DeviceHub.LineSent.
    /// Runtime districts "uwa" and "kingspark" (random chunk picks) so a seed and a start district show in the road.
    /// The rider is fast (40 m/s) and the road empty, so races get somewhere in a few seconds.
    /// </summary>
    public class RaceTests
    {
        const string Board = "race-usb";
        readonly List<Object> _made = new List<Object>();
        readonly List<(double time, string line)> _sent = new List<(double, string)>();
        GameTuning _tuning;
        TrackTheme _theme;
        GameController _game;
        RaceDirector _race;
        DeviceHub _hub;

        T Keep<T>(T o) where T : Object { _made.Add(o); return o; }

        GameObject Template(string name)
        {
            var go = Keep(new GameObject(name));
            go.transform.position = new Vector3(0f, -500f, 0f);
            return go;
        }

        RoadChunk Chunk(string name, float weight)
        {
            var c = Template(name).AddComponent<RoadChunk>();
            c.length = 40f; c.endHeight = -3.1471f; c.halfWidth = 4f; c.sidewalkWidth = 4f; c.sidewalkHeight = .2f; c.weight = weight;
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
            _tuning.fakeTurns = true; // the race seed must give the same turns
            _tuning.turnFirstAt = 60f;
            _tuning.turnMinStraight = 40f;
            _tuning.turnMinLength = 40f;
            _tuning.turnStraightSeconds = new Vector2(0.5f, 2f);
            _tuning.turnLengthSeconds = new Vector2(0.5f, 2f);
            _tuning.startDistrict = "uwa";

            _theme = Keep(ScriptableObject.CreateInstance<TrackTheme>());
            _theme.chunks = new[] { Chunk("S1", 1f) };
            _theme.districts = new[]
            {
                MakeDistrict("uwa", 10, new[] { Chunk("U1", 1f), Chunk("U2", 1f), Chunk("U3", 1f) }),
                MakeDistrict("kingspark", 20, new[] { Chunk("K1", 1f), Chunk("K2", 1f), Chunk("K3", 1f) }),
            };

            _sent.Clear();
            _hub.LineSent += OnSent;
            if (Camera.main == null)
            {
                var cam = Keep(new GameObject("Race test camera")).AddComponent<Camera>(); // name tags need a camera
                cam.tag = "MainCamera";
            }
            _game = GameSystemsFactory.Create(_tuning, _theme).GetComponent<GameController>();
            _game.track.seed = 42;
            Keep(_game.gameObject);
            yield return null; // GameController.Start adds the RaceDirector
            _race = _game.GetComponent<RaceDirector>();
            Assert.NotNull(_race, "GameController adds a RaceDirector");
            Assert.AreEqual(RaceState.Off, _race.State, "off until a board sends race:");
            _game.EnterReady();
            yield return null;
        }

        void OnSent(string target, string line) => _sent.Add((Time.unscaledTimeAsDouble, line));

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_hub != null) _hub.LineSent -= OnSent;
            for (int i = _made.Count - 1; i >= 0; i--) if (_made[i] != null) Object.Destroy(_made[i]);
            _made.Clear();
            yield return null;
            _hub?.Mixer.Reset();
        }

        void Say(string line) => _hub.Receive(Board, line);

        // Waits, while the board keeps saying race: about once a second (as the blocks do).
        IEnumerator Wait(float seconds, bool keepRacing = true, System.Action everyFrame = null)
        {
            double end = Time.unscaledTimeAsDouble + seconds, nextRace = 0d;
            while (Time.unscaledTimeAsDouble < end)
            {
                if (keepRacing && Time.unscaledTimeAsDouble >= nextRace) { Say("race:AMY:12"); nextRace = Time.unscaledTimeAsDouble + 0.9; }
                everyFrame?.Invoke();
                yield return null;
            }
        }

        IEnumerator JoinLobby()
        {
            Say("race:AMY:12");
            yield return null;
            Assert.AreEqual(RaceState.Lobby, _race.State);
        }

        IEnumerator StartRace(int seed = 4821, string start = "kingspark", int metres = 5000)
        {
            Say("rx:go:" + seed + ":" + start + ":" + metres);
            Assert.AreEqual(RaceState.Countdown, _race.State, "go: starts the countdown");
            yield return Wait(RaceDirector.CountdownSeconds + 0.2f);
            Assert.AreEqual(RaceState.Racing, _race.State);
            Assert.AreEqual(RunState.Running, _game.State);
        }

        [UnityTest]
        public IEnumerator RaceLine_EntersTheLobby_HudShowsGroupAndName()
        {
            yield return JoinLobby();
            yield return null;
            Assert.AreEqual("AMY", _race.MyName);
            Assert.AreEqual(12, _race.Group);
            StringAssert.Contains("RACE 12", _game.hud.RaceTitleText);
            StringAssert.Contains("YOU: AMY", _game.hud.RaceTitleText);
            Assert.AreEqual("", _game.hud.StartLineText, "the start district line belongs to normal play");
            Assert.AreEqual(RunState.Ready, _game.State);
            Assert.IsTrue(_game.track.fixedPlanning, "race mode plans the road without the live speed");
        }

        [UnityTest]
        public IEnumerator RxPositions_ListRidersInTheLobby_WithSignalBars()
        {
            yield return JoinLobby();
            Say("rx:p:BEN:0:0");
            Say("rx:p:chloe:0:0");
            Say("rx:p:AMY:0:0"); // my own name is never listed
            Say("rx:hello from somebody"); // not a race message: ignored
            Say("rx:p:BROKEN:far:0");
            yield return Wait(0.3f);
            List<string> names = _game.hud.RaceLobbyNames();
            CollectionAssert.AreEquivalent(new[] { "BEN", "CHLOE" }, names);
            Assert.AreEqual(2, _race.HeardRecently().Count);
            StringAssert.Contains("Riders I can hear", _game.hud.RaceSubText);
            Assert.AreEqual(3, RaceDirector.SignalBars(1f, 1f));
            Assert.AreEqual(1, RaceDirector.SignalBars(0.34f, 1f));
            // Nobody heard for 3 s: off the list.
            yield return Wait(RaceDirector.LobbyHeardSeconds + 0.3f);
            Assert.AreEqual(0, _game.hud.RaceLobbyNames().Count);
        }

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

        static List<string> Turns()
        {
            FakeTurnDirector turns = FakeTurnDirector.Instance;
            Assert.NotNull(turns);
            return turns.Planned.Select(t => t.Start.ToString("0.0") + ".." + t.End.ToString("0.0") + ":" + t.Curvature.ToString("0.000000")).ToList();
        }

        [UnityTest]
        public IEnumerator Go_CountsDown_ThenRacesOnTheSeed_SameGoSameRoad()
        {
            yield return JoinLobby();
            _hub.Receive("sim", "rx:go:4821:kingspark:5000"); // the host's own go comes back as rx:go too, from any device
            Assert.AreEqual(RaceState.Countdown, _race.State);
            yield return null;
            for (int i = 0; i < 4; i++) Say("rx:go:4821:kingspark:5000"); // the starter sends it 5 times in 0.5 s
            Say("rx:go:9:uwa:300");
            Assert.AreEqual(5, _race.IgnoredGo, "repeats during the countdown are ignored");
            Assert.AreEqual(4821, _game.track.seed);
            Assert.AreEqual("kingspark", _game.track.StartDistrict.id, "the go:'s start district");
            Say("jp"); // A during the countdown does not start a ride
            yield return Wait(1f);
            Assert.AreEqual(RunState.Ready, _game.State);
            StringAssert.Contains("RACE 12", _game.hud.RaceSubText);
            StringAssert.Contains("5000 m from KINGSPARK", _game.hud.RaceSubText);
            yield return Wait(RaceDirector.CountdownSeconds - 1f + 0.2f);
            Assert.AreEqual(RaceState.Racing, _race.State);
            Assert.AreEqual(RunState.Running, _game.State);
            yield return null;
            yield return null;
            List<string> chunksA = Chunks(_game.track, _game.startPosition.z - 2f), turnsA = Turns();
            Assert.Greater(turnsA.Count, 0, "turns are planned");
            Assert.Greater(chunksA.Count, 3);

            // The same go: again (back to the lobby first, as A on the results screen does).
            _race.BackToLobby();
            yield return null;
            Say("rx:go:4821:kingspark:5000");
            yield return Wait(RaceDirector.CountdownSeconds + 0.2f);
            yield return null;
            yield return null;
            Assert.AreEqual(RaceState.Racing, _race.State);
            CollectionAssert.AreEqual(chunksA, Chunks(_game.track, _game.startPosition.z - 2f), "the same seed builds the same road");
            CollectionAssert.AreEqual(turnsA, Turns(), "and the same turns");

            // Another seed: another road.
            _race.BackToLobby();
            yield return null;
            Say("rx:go:777:kingspark:5000");
            yield return Wait(RaceDirector.CountdownSeconds + 0.2f);
            yield return null;
            yield return null;
            List<string> chunksB = Chunks(_game.track, _game.startPosition.z - 2f), turnsB = Turns();
            Assert.IsFalse(chunksA.SequenceEqual(chunksB) && turnsA.SequenceEqual(turnsB), "a different seed gives a different road");

            // Normal play afterwards does not keep the race's seed: leave race mode, the track seed comes back.
            yield return Wait(RaceDirector.ModeTimeoutSeconds + 0.3f, false);
            Assert.AreEqual(RaceState.Off, _race.State);
            Assert.AreEqual(42, _game.track.seed);
        }

        [UnityTest]
        public IEnumerator InARace_GoIsIgnored_DoAndStartAndToonAreOff()
        {
            var triggers = _game.GetComponent<ProgramTriggers>();
            Say("do:car"); // normal play, between rides: kept for the next ride ...
            Assert.AreEqual(1, triggers.Queued);
            yield return JoinLobby();
            yield return StartRace();
            yield return Wait(0.6f);
            Assert.AreEqual(0, triggers.Queued, "... but the race's road stays the same for everyone");
            Assert.AreEqual(0, triggers.Done);
            Say("rx:go:1:uwa:900");
            Assert.AreEqual(1, _race.IgnoredGo);
            Assert.AreEqual(4821, _race.Seed);
            Assert.AreEqual(RaceState.Racing, _race.State);
            int dropped = triggers.Dropped;
            Say("do:chase");
            Say("do:car");
            Say("start:uwa");
            Say("toon:1");
            Say("open:chase"); // every-ride events too
            yield return Wait(0.3f);
            Assert.AreEqual(dropped + 2, triggers.Dropped, "do: events are ignored in a race");
            Assert.AreEqual(0, PoliceChase.Instance.Wanted, "no police");
            Assert.AreEqual(0, triggers.Opening, "open: is ignored in a race");
            Assert.IsFalse(PoliceChase.Instance.Chasing);
            Assert.AreEqual("kingspark", _game.track.startDistrictOverride, "start: is ignored: the go: picked the start");
            Assert.IsFalse(RiderActions.CartoonMode, "no cartoon mode");
            _hub.Mixer.Reset();
        }

        [UnityTest]
        public IEnumerator Pos_IsSentAbout4TimesASecondInARace_AndOnceASecondInTheLobby()
        {
            yield return JoinLobby();
            _sent.Clear();
            yield return Wait(2.5f);
            List<string> lobby = _sent.Select(s => s.line).Where(l => l.StartsWith("pos:")).ToList();
            Assert.That(lobby.Count, Is.InRange(2, 4), "about 1 Hz in the lobby: " + string.Join(", ", lobby));
            Assert.IsTrue(lobby.All(l => l == "pos:0:0"), "pos:0:0 in the lobby");

            yield return StartRace();
            _sent.Clear();
            yield return Wait(2f);
            List<string> race = _sent.Select(s => s.line).Where(l => l.StartsWith("pos:")).ToList();
            Assert.That(race.Count, Is.InRange(7, 10), "about 4 Hz in a race: " + string.Join(", ", race));
            int first = int.Parse(race[0].Split(':')[1]), last = int.Parse(race[race.Count - 1].Split(':')[1]);
            Assert.Greater(last, first + 30, "the distance goes up");
            Assert.AreEqual(Mathf.FloorToInt(_game.rider.Distance), int.Parse(race[race.Count - 1].Split(':')[1]), 25f);
        }

        [UnityTest]
        public IEnumerator Ghosts_StandAtTheirDistanceAndLane_MoveOnBetweenPackets_GreyThenGone()
        {
            yield return JoinLobby();
            yield return StartRace();
            float startZ = _race.StartZ;
            Say("rx:p:BEN:100:2");
            yield return Wait(0.25f);
            Say("rx:p:BEN:110:2");
            yield return null;
            yield return null;
            RaceDirector.Rival ben = _race.Rivals.Single(r => r.Name == "BEN");
            Assert.IsTrue(RaceDirector.GhostPosition(ben, out Vector3 at), "BEN is drawn");
            Assert.AreEqual(2f, at.x, 0.3f, "in his lane");
            // At his distance; a new packet is blended in over ~0.3 s instead of jumping, so a few metres either side.
            Assert.That(at.z - startZ, Is.InRange(104f, 125f), "at his distance (plus a little dead reckoning)");
            Assert.IsNull(ben.Ghost.GetComponentInChildren<Collider>(), "a ghost can never be hit");
            float z1 = at.z;
            yield return Wait(0.3f);
            RaceDirector.GhostPosition(ben, out at);
            Assert.Greater(at.z, z1 + 3f, "moved on between packets at his speed");
            Assert.Less(at.z - startZ, 110f + 60f * 1.35f * RaceDirector.ExtrapolateSeconds + 5f, "never guessed far ahead");
            Assert.IsFalse(ben.Ghost.Grey);
            CollectionAssert.Contains(_game.hud.RaceTagTexts(), "BEN", "name tag over the ghost");

            yield return Wait(RaceDirector.GhostGreySeconds);
            Assert.IsTrue(ben.Ghost.Grey, "grey after 2 s without data");
            Assert.IsTrue(_game.hud.RaceTagTexts().Any(t => t.StartsWith("BEN") && t.Contains("signal lost")), string.Join("|", _game.hud.RaceTagTexts()));

            yield return Wait(RaceDirector.GhostGoneSeconds - RaceDirector.GhostGreySeconds);
            Assert.IsFalse(_race.Rivals.Any(r => r.Name == "BEN"), "gone after 10 s without data");

            // At most 8 drawn (the nearest); the bar has everyone.
            float mine = _game.rider.Distance;
            for (int i = 0; i < 12; i++) Say("rx:p:R" + i + ":" + Mathf.RoundToInt(mine + 10f + i * 15f) + ":0");
            yield return null;
            yield return null;
            Assert.AreEqual(12, _race.Rivals.Count);
            Assert.AreEqual(RaceDirector.MaxGhosts, _race.Rivals.Count(r => r.Drawn));
            Assert.IsTrue(_race.Rivals.Where(r => r.Name == "R0" || r.Name == "R1").All(r => r.Drawn), "the nearest are drawn");
        }

        [UnityTest]
        public IEnumerator Plausibility_ImpossibleJumps_GetAQuestionMark_AndAreNotUsed()
        {
            yield return JoinLobby();
            yield return StartRace();
            Say("rx:p:EVE:50:0");
            yield return Wait(0.25f);
            Say("rx:p:EVE:99999:0"); // impersonation / a bug: nobody rides that far in 0.25 s
            yield return null;
            yield return null;
            RaceDirector.Rival eve = _race.Rivals.Single(r => r.Name == "EVE");
            Assert.IsTrue(eve.Suspicious);
            Assert.AreEqual(50f, eve.Distance, 0.01f, "the impossible position is not used");
            Assert.IsTrue(_game.hud.RaceTagTexts().Any(t => t.StartsWith("EVE") && t.Contains("?")), string.Join("|", _game.hud.RaceTagTexts()));
            Say("rx:p:EVE:60:0"); // a normal one afterwards is fine (EVE keeps her "?" until the next race)
            yield return null;
            Assert.AreEqual(60f, eve.Distance, 0.01f);
            Assert.IsTrue(eve.Suspicious);
            Say("rx:f:FAST:1.00"); // finishing 5 km in a second: also "?"
            Assert.IsTrue(_race.Rivals.Single(r => r.Name == "FAST").Suspicious);
        }

        [UnityTest]
        public IEnumerator Crash_IsA3SecondStop_AndTheRideGoesOnFromTheSameSpot()
        {
            yield return JoinLobby();
            yield return StartRace();
            yield return Wait(0.5f);
            _sent.Clear();
            float z = _game.rider.transform.position.z, distance = _game.rider.Distance;
            _game.EndRun("CRASH!", true);
            Assert.AreEqual(RunState.Running, _game.State, "the ride is not over");
            Assert.IsFalse(_game.rider.Running, "stopped");
            Assert.IsTrue(_sent.Any(s => s.line == "crash"), "the board still hears crash");
            yield return Wait(0.3f);
            StringAssert.Contains("+3 s", _game.hud.RacePenaltyText);
            Assert.AreEqual(1, _race.Crashes);
            Assert.AreEqual(z, _game.rider.transform.position.z, 0.01f, "waits where it crashed");
            yield return Wait(RaceDirector.CrashPenaltySeconds - 0.3f + 0.2f);
            Assert.IsTrue(_game.rider.Running, "back on the scooter after 3 s");
            Assert.GreaterOrEqual(_game.rider.Distance, distance - 0.01f, "from the same spot: the distance is kept");
            Assert.Less(_game.rider.Distance, distance + 40f);
            Assert.AreEqual(RaceState.Racing, _race.State);
        }

        [UnityTest]
        public IEnumerator Finish_SendsFinRepeatedly_ThenResults_ThenALobbyOnA()
        {
            yield return JoinLobby();
            Say("rx:p:BEN:0:0");
            yield return StartRace(4821, "uwa", 300);
            Assert.IsNotNull(_race.FinishArch, "a FINISH arch at the race distance");
            Assert.AreEqual(_race.StartZ + 300f, _race.FinishArch.transform.position.z, 0.01f);
            Assert.IsNull(_race.FinishArch.GetComponentInChildren<Collider>());
            // 300 m at 40+ m/s; BEN rides along at 70 % of my distance
            double until = Time.unscaledTimeAsDouble + 15d;
            while (_race.State == RaceState.Racing && Time.unscaledTimeAsDouble < until)
            {
                Say("rx:p:BEN:" + Mathf.RoundToInt(_game.rider.Distance * 0.7f) + ":1");
                yield return Wait(0.25f);
            }
            Assert.AreEqual(RaceState.Finished, _race.State);
            float time = _race.FinishTime;
            Assert.That(time, Is.InRange(4f, 9f));
            _sent.Clear();
            yield return Wait(1.6f);
            List<string> fins = _sent.Select(s => s.line).Where(l => l.StartsWith("fin:")).ToList();
            Assert.That(fins.Count, Is.InRange(3, 5), "fin: every 0.5 s: " + string.Join(", ", fins));
            Assert.IsTrue(fins.All(l => l == Protocol.Messages.Finish(time)));
            Assert.IsFalse(_sent.Any(s => s.line.StartsWith("pos:")), "fin: instead of pos:");
            StringAssert.Contains("FINISH", _game.hud.RaceTitleText);
            Assert.AreEqual("1st / 2", _game.hud.RacePlaceText);

            Say("rx:f:BEN:" + (time + 2.5f).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            yield return Wait(RaceDirector.ResultsMinSeconds);
            Assert.AreEqual(RaceState.Results, _race.State, "everyone has finished");
            StringAssert.Contains("RESULTS", _game.hud.RaceTitleText);
            List<RaceDirector.Standing> standings = _race.Standings();
            Assert.AreEqual(new[] { "AMY", "BEN" }, standings.Select(s => s.Name).ToArray());
            Assert.IsTrue(standings[0].Me);

            Say("rx:go:5:uwa:300"); // results: a new go: may start the next race ...
            Assert.AreEqual(RaceState.Countdown, _race.State);
            _race.BackToLobby();
            yield return null;
            // ... and A goes back to the lobby (practice).
            Say("rx:go:5:uwa:300");
            yield return Wait(RaceDirector.CountdownSeconds + 0.2f);
            until = Time.unscaledTimeAsDouble + 15d;
            while (_race.State == RaceState.Racing && Time.unscaledTimeAsDouble < until) yield return Wait(0.2f);
            yield return Wait(RaceDirector.ResultsMinSeconds + 0.2f);
            Assert.AreEqual(RaceState.Results, _race.State, "alone: results right after the FINISH! moment");
            Say("jp");
            yield return null;
            yield return null;
            Assert.AreEqual(RaceState.Lobby, _race.State);
            Assert.AreEqual(RunState.Ready, _game.State, "the press that left the results does not start a practice ride");
            Say("jp");
            yield return null;
            yield return null;
            Assert.AreEqual(RunState.Running, _game.State, "A in the lobby: a practice ride");
            Assert.AreEqual(RaceState.Lobby, _race.State);
        }

        [UnityTest]
        public IEnumerator NoRaceLineFor10Seconds_BackToNormalPlay()
        {
            yield return JoinLobby();
            yield return Wait(RaceDirector.ModeTimeoutSeconds - 1f, false);
            Assert.AreEqual(RaceState.Lobby, _race.State, "still racing 9 s after the last race:");
            yield return Wait(1.3f, false);
            Assert.AreEqual(RaceState.Off, _race.State);
            Assert.IsFalse(RaceDirector.On);
            Assert.IsFalse(_game.track.fixedPlanning);
            Assert.AreEqual(42, _game.track.seed);
            Assert.AreEqual("", _game.hud.RaceTitleText, "the race screens are gone");
            Assert.AreEqual(RunState.Ready, _game.State);
            _sent.Clear();
            yield return Wait(1.2f, false);
            Assert.IsFalse(_sent.Any(s => s.line.StartsWith("pos:")), "no pos: in normal play");
        }
    }
}
