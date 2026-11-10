using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityScooter.Devices;
using CityScooter.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>
    /// v0.5 item race (docs/item-race.md) in the real Main scene, where the police have their car: being rammed in a race
    /// (BUSTED) is a 3 s stop, not the end of the ride; and the item pictures are built from the real comic materials.
    /// The street is made quiet (no random obstacles, hazards, people, turns or ramps) so only the police can stop a ride.
    /// </summary>
    public class RaceItemMainSceneTests
    {
        const string Board = "item-main";
        Scene _main;
        GameController _game;
        RaceDirector _race;
        RaceItems _items;
        PoliceChase _police;
        GameTuning _calm;
        DeviceHub _hub;
        readonly List<string> _sent = new List<string>();

        void OnSent(string target, string line) => _sent.Add(line);

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _hub = DeviceHub.Instance;
            Assert.NotNull(_hub);
            _hub.Mixer.Reset();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);
            _main = SceneManager.GetSceneByName("Main");
            SceneManager.SetActiveScene(_main);
            yield return null;
            _game = GameController.Instance;
            Assert.NotNull(_game);
            _calm = Object.Instantiate(_game.rider.tuning != null ? _game.rider.tuning : GameTuning.Default);
            _calm.obstacleChanceStart = _calm.obstacleChanceMax = 0f;
            _calm.hazardChanceStart = _calm.hazardChanceMax = 0f;
            _calm.jumpObstacleChance = 0f;
            _calm.giftChance = 0f;
            _calm.rampChance = 0f;
            _calm.sidewalkPeople = Vector2.zero;
            _calm.fakeTurns = false;
            _calm.districts = false; // the plain comic street
            _calm.copFirstDelay = 60f; // the test sends the police car itself
            _calm.heliFirstShotSeconds = 99f;
            _game.tuning = _game.rider.tuning = _game.track.tuning = _calm;
            FakeTurnDirector turns = _game.GetComponent<FakeTurnDirector>();
            if (turns != null) turns.tuning = _calm;
            yield return null;
            _race = _game.GetComponent<RaceDirector>();
            _items = _game.GetComponent<RaceItems>();
            _police = _game.GetComponent<PoliceChase>();
            Assert.NotNull(_race);
            Assert.NotNull(_items);
            Assert.NotNull(_police);
            _game.EnterReady();
            _sent.Clear();
            _hub.LineSent += OnSent;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_hub != null) _hub.LineSent -= OnSent;
            Time.timeScale = 1f;
            if (_main.IsValid() && _main.isLoaded) yield return SceneManager.UnloadSceneAsync(_main);
            if (_calm != null) Object.Destroy(_calm);
            _hub?.Mixer.Reset();
        }

        void Say(string line) => _hub.Receive(Board, line);

        IEnumerator Wait(float seconds, System.Func<bool> until = null)
        {
            double end = Time.unscaledTimeAsDouble + seconds, nextRace = 0d;
            while (Time.unscaledTimeAsDouble < end && (until == null || !until()))
            {
                if (Time.unscaledTimeAsDouble >= nextRace) { Say("race:AMY:12"); nextRace = Time.unscaledTimeAsDouble + 0.9; }
                yield return null;
            }
        }

        IEnumerator StartRace()
        {
            Say("race:AMY:12");
            yield return null;
            Assert.AreEqual(RaceState.Lobby, _race.State);
            Say("rx:go:4821:uwa:3000");
            yield return Wait(RaceDirector.CountdownSeconds + 0.3f);
            Assert.AreEqual(RaceState.Racing, _race.State);
            Assert.AreEqual(RunState.Running, _game.State);
        }

        [UnityTest]
        public IEnumerator RammedByThePoliceCarInARace_IsBusted_A3SecondStop_ThenTheRideGoesOn()
        {
            yield return StartRace();
            _items.boxesOn = false;
            yield return Wait(0.5f);
            Say("do:stars:1");
            yield return null;
            Assert.AreEqual(1, _police.Wanted, "a program's own police event in a race");
            _sent.Clear();
            Assert.IsTrue(_police.SendCop(14f, PoliceChase.Tactic.Ram), "the police car comes (the same PoliceChase as in normal play)");
            yield return Wait(10f, () => !_game.rider.Running);
            Assert.IsFalse(_game.rider.Running, "rammed");
            yield return null;
            Assert.AreEqual(RunState.Running, _game.State, "BUSTED does not end the ride in a race");
            Assert.AreEqual(RaceState.Racing, _race.State);
            Assert.AreEqual(0, _police.Wanted, "the wanted level is 0 again");
            Assert.IsFalse(_police.Chasing);
            List<string> order = _sent.Where(l => l == "crash" || l == "wanted:0" || l == "chase:0" || l == "busted").ToList();
            CollectionAssert.AreEqual(new[] { "crash", "wanted:0", "chase:0", "busted" }, order, "the usual messages, busted last");
            Assert.AreEqual("BUSTED!", _game.hud.FlashText);
            Assert.That(_race.PenaltyLeft, Is.InRange(2.3f, 3f));
            float distance = _game.rider.Distance;
            yield return Wait(RaceDirector.CrashPenaltySeconds + 0.4f);
            Assert.IsTrue(_game.rider.Running, "back on the scooter after 3 s");
            Assert.GreaterOrEqual(_game.rider.Distance, distance - 0.5f);
            Assert.AreEqual(0, _police.Wanted);
        }

        [UnityTest]
        public IEnumerator ItemPictures_AreBuiltFromTheComicMaterials_AndNeverGetInTheWay()
        {
            yield return StartRace();
            yield return Wait(0.3f);
            // The first row of boxes, drawn with the rider's own comic material and ink outline.
            ItemBox[] boxes = _items.BoxesShown().ToArray();
            Assert.AreEqual(3, boxes.Length);
            Material palette = RaceFinishArch.FindPalette(_game.rider.model);
            foreach (ItemBox box in boxes)
            {
                Assert.IsNull(box.GetComponentInChildren<Collider>(), "nothing of the items can be crashed into");
                MeshRenderer[] parts = box.GetComponentsInChildren<MeshRenderer>();
                Assert.IsTrue(parts.Any(r => r.sharedMaterial == palette), "the comic palette material");
                Assert.IsTrue(parts.Any(r => r.sharedMaterial != null && r.sharedMaterial.HasProperty("_OutlinePixels")), "and the ink outline");
                Assert.Greater(box.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount, 100);
            }
            // Shield, flame, oil, bolt and the rocket's warning: all there, none with a collider.
            _items.boxesOn = false;
            Assert.IsTrue(_items.Give("shield"));
            Assert.IsTrue(_items.Use());
            Assert.IsTrue(_items.Give("boost"));
            Assert.IsTrue(_items.Use());
            Say("rx:i:BEN:oil:" + Mathf.RoundToInt(_game.rider.Distance + 60f) + ":0:1");
            yield return Wait(0.2f);
            Assert.IsTrue(_items.ShieldShown);
            Assert.IsTrue(_items.FlameShown);
            Assert.AreEqual(1, _items.SlicksShown);
            Transform art = _game.transform.Find("RaceItems");
            Assert.NotNull(art, "everything the items draw is under one object");
            Assert.AreEqual(0, art.GetComponentsInChildren<Collider>(true).Length);
            Transform bubble = art.Find("Shield/Bubble");
            Assert.NotNull(bubble, "the see-through bubble (Sprites/Default is always in the build)");
            Assert.AreEqual("Sprites/Default", bubble.GetComponent<MeshRenderer>().sharedMaterial.shader.name);
            Assert.Less(bubble.GetComponent<MeshRenderer>().sharedMaterial.color.a, 0.6f, "see-through");
            Say("rx:i:BEN:bolt:2"); // the shield takes it
            Say("rx:i:BEN:bolt:3");
            Say("rx:i:BEN:rocket:AMY:4");
            yield return Wait(0.2f);
            Assert.IsTrue(_items.BoltShown);
            Assert.Greater(_items.RocketIn, 0f);
            Assert.NotNull(art.Find("RocketTarget"), "a target ring round the rider");
            Assert.IsTrue(art.Find("RocketTarget").gameObject.activeSelf);
            Assert.AreEqual(0, art.GetComponentsInChildren<Collider>(true).Length);
        }
    }
}
