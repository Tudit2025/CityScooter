using System;
using System.Collections;
using CityScooter.Devices;
using CityScooter.Game;
using CityScooter.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CityScooter.Tests
{
    /// <summary>Core loop on grey-box placeholders: move, steer from a device, crash, jump a railing, collect a gift.</summary>
    public class GameplayTests
    {
        GameController _game;
        GameTuning _tuning;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<GameTuning>();
            _tuning.obstacleChanceStart = 0f; // tests place their own obstacles
            _tuning.obstacleChanceMax = 0f;
            _tuning.giftChance = 0f;
            _tuning.jumpObstacleChance = 0f;
            _tuning.startSpeed = 10f;
            DeviceHub.Instance?.Mixer.Reset(); // a previous test's steer must not leak into this one
            GameObject go = GameSystemsFactory.Create(_tuning);
            _game = go.GetComponent<GameController>();
            _game.track.seed = 42;
            yield return null; // Start → Ready
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_game.gameObject);
            Object.Destroy(_tuning);
            DeviceHub.Instance?.Mixer.Reset();
            yield return null;
        }

        static IEnumerator WaitUntil(Func<bool> done, float timeout)
        {
            float t = 0f;
            while (!done() && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        float RiderZ => _game.rider.transform.position.z;

        [UnityTest]
        public IEnumerator Run_MovesForward_AndKeepsTrackBuiltAhead()
        {
            Assert.AreEqual(RunState.Ready, _game.State);
            _game.StartRun();
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(RunState.Running, _game.State);
            Assert.Greater(_game.rider.Distance, 7f);
            Assert.GreaterOrEqual(_game.track.ActiveChunkCount, 6);
            Assert.Greater(_game.rider.SpeedLevel, 0);
        }

        [UnityTest]
        public IEnumerator DeviceSteer_MovesRiderSideways()
        {
            _game.StartRun();
            float t = 0f;
            while (t < 0.6f)
            {
                DeviceHub.Instance.Receive("test", "st:100");
                t += Time.deltaTime;
                yield return null;
            }
            Assert.Greater(_game.rider.transform.position.x, 1.5f);
        }

        [UnityTest]
        public IEnumerator HittingTallBarrier_Crashes_AndRaisesEvent()
        {
            bool crashed = false;
            Action onCrash = () => crashed = true;
            GameEvents.Crashed += onCrash;
            try
            {
                _game.StartRun();
                _game.track.PlaceAt("placeholder-obstacle-1", new Vector3(0f, 0f, RiderZ + 12f));
                yield return WaitUntil(() => _game.State == RunState.Crashed, 3f);
                Assert.AreEqual(RunState.Crashed, _game.State);
                Assert.IsTrue(crashed);
            }
            finally
            {
                GameEvents.Crashed -= onCrash;
            }
        }

        [UnityTest]
        public IEnumerator JumpingOverRailing_Survives()
        {
            _game.StartRun();
            float railZ = RiderZ + 15f;
            _game.track.PlaceAt("placeholder-railing", new Vector3(0f, 0f, railZ));
            yield return WaitUntil(() => RiderZ > railZ - 4f, 3f);
            DeviceHub.Instance.Receive("test", "jp");
            yield return WaitUntil(() => RiderZ > railZ + 3f || _game.State != RunState.Running, 3f);
            Assert.AreEqual(RunState.Running, _game.State, "an airborne rider clears a railing");
            Assert.Greater(RiderZ, railZ + 3f);
        }

        [UnityTest]
        public IEnumerator NotJumpingAtRailing_Crashes()
        {
            _game.StartRun();
            float railZ = RiderZ + 15f;
            _game.track.PlaceAt("placeholder-railing", new Vector3(0f, 0f, railZ));
            yield return WaitUntil(() => _game.State == RunState.Crashed, 3f);
            Assert.AreEqual(RunState.Crashed, _game.State);
        }

        [UnityTest]
        public IEnumerator Gift_AddsScore_AndRaisesEvent()
        {
            int events = 0;
            Action onGift = () => events++;
            GameEvents.GiftCollected += onGift;
            try
            {
                _game.StartRun();
                _game.track.PlaceAt("placeholder-gift", new Vector3(0f, 0f, RiderZ + 10f));
                yield return WaitUntil(() => _game.Gifts > 0, 3f);
                Assert.AreEqual(1, _game.Gifts);
                Assert.AreEqual(1, events);
                Assert.Greater(_game.Score, 9);
            }
            finally
            {
                GameEvents.GiftCollected -= onGift;
            }
        }
    }
}
