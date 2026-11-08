using System.Collections;
using CityScooter.Controls;
using CityScooter.Devices;
using CityScooter.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CityScooter.Tests
{
    /// <summary>
    /// Lessons use the micro:bit-only page (index.html boots the shell with keyboard: false, so Unity gets
    /// SetKeyboardEnabled("0") in every mode): no text on screen may tell students to press a key, and it updates
    /// when the flag arrives after a screen is already showing.
    /// </summary>
    public class KeyboardOffTests
    {
        GameObject _hud;

        static string AllText(GameObject root)
        {
            var sb = new System.Text.StringBuilder();
            foreach (Text t in root.GetComponentsInChildren<Text>(true)) sb.Append(t.text).Append('\n');
            return sb.ToString();
        }

        static readonly string[] KeyWords = { "Space", "press Left", "Right to steer", "S to brake", "F punch", "R bell", "E boost", "T cartoon" };

        [UnityTest]
        public IEnumerator KeyboardOff_ScreensOnlyMentionTheMicrobit_AndSwitchBackWhenItIsOn()
        {
            yield return null; // Bootstrap creates DeviceBridge (DeviceHub)
            DeviceHub hub = DeviceHub.Instance;
            Assert.IsNotNull(hub, "Bootstrap should create the DeviceBridge object");

            _hud = new GameObject("HudUnderTest");
            HudView hud = _hud.AddComponent<HudView>();
            hud.ShowReady();
            yield return null;
            StringAssert.Contains("Space", AllText(_hud), "keyboard allowed (default): the start screen mentions Space");

            hub.SetKeyboardEnabled("0"); // the shell's micro:bit-only page, arriving after the start screen is up
            yield return null;
            string ready = AllText(_hud);
            StringAssert.Contains("Press A on your micro:bit to start", ready);
            foreach (string w in KeyWords) StringAssert.DoesNotContain(w, ready, "keyboard off: no key named on screen");

            hud.ShowCrashed(120, 300, "CRASH!", null);
            yield return null;
            string crashed = AllText(_hud);
            StringAssert.Contains("Press A on your micro:bit to ride again", crashed);
            foreach (string w in KeyWords) StringAssert.DoesNotContain(w, crashed);

            Assert.IsFalse(RiderInputSource.Read().Brake, "nothing pressed: no brake");

            hub.SetKeyboardEnabled("1");
            yield return null;
            StringAssert.Contains("Press A or Space to ride again", AllText(_hud), "keyboard back on: the crash screen is redrawn");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            DeviceHub.Instance?.SetKeyboardEnabled("1");
            if (_hud != null) Object.Destroy(_hud);
            yield return null;
        }
    }
}
