using System.Collections.Generic;
using CityScooter.Controls;
using CityScooter.Devices;
using CityScooter.Game;
using CityScooter.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CityScooter.DevTools
{
    /// <summary>
    /// F1 developer overlay: connected devices, recent lines in/out, merged input, and buttons that
    /// fire game events so micro:bit feedback can be tested before gameplay exists.
    /// English only on purpose: IMGUI in Web builds has no CJK font.
    /// </summary>
    [RequireComponent(typeof(DeviceHub))]
    public sealed class DeviceDebugOverlay : MonoBehaviour
    {
        public bool visible;

        readonly Queue<string> _log = new Queue<string>();
        DeviceHub _hub;
        int _speed;
        GUIStyle _style;

        // Unity APIs are not allowed in field initializers, so the default is set here.
        void Awake() => visible = Application.isEditor;

        void OnEnable()
        {
            _hub = GetComponent<DeviceHub>();
            _hub.LineReceived += OnLineReceived;
            _hub.LineSent += OnLineSent;
        }

        void OnDisable()
        {
            _hub.LineReceived -= OnLineReceived;
            _hub.LineSent -= OnLineSent;
        }

        void OnLineReceived(string id, string line, Command cmd)
        {
            if (cmd.Kind == CommandKind.Steer || cmd.Kind == CommandKind.Brake || cmd.Kind == CommandKind.Hello) return;
            string note = cmd.Kind == CommandKind.Unknown ? "   (not a command)" : cmd.Kind == CommandKind.Malformed ? "   (bad value)" : "";
            Add(id + " > " + line + note);
        }

        void OnLineSent(string target, string line) => Add("game > " + target + ": " + line);

        void Add(string s)
        {
            _log.Enqueue(s);
            while (_log.Count > 14) _log.Dequeue();
        }

        void Update()
        {
            Keyboard k = Keyboard.current;
            if (k != null && k.f1Key.wasPressedThisFrame) visible = !visible;
        }

        void OnGUI()
        {
            if (!visible) return;
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 };
            GUILayout.BeginArea(new Rect(10, 10, 440, Screen.height - 20), GUI.skin.box);
            GUILayout.Label("<b>CityScooter devices</b>  (F1 to hide)", _style);

            RiderInput input = RiderInputSource.Last;
            GUILayout.Label("steer " + input.Steer.ToString("+0.00;-0.00") + (input.DeviceSteering ? " (device)" : " (keys)")
                + "   brake " + (input.Brake ? "ON" : "-") + "   keyboard " + (_hub.KeyboardEnabled ? "on" : "off"), _style);

            if (_hub.Devices.Count == 0)
            {
                GUILayout.Label("no devices yet", _style);
                GUILayout.Label("  real micro:bit: run tools/serial_bridge.cmd (its window says why a board is silent)", _style);
                GUILayout.Label("  no micro:bit: run python tools/fake_microbit.py", _style);
            }
            foreach (DeviceInfo d in _hub.Devices.Values)
            {
                GUILayout.Label((d.Connected ? "<color=#5f5>●</color> " : "<color=#f55>●</color> ") + d.Id + " [" + d.Kind + "] "
                    + (d.Version ?? "?") + "  lines " + d.Lines + "  st " + d.SteerLines + "  jp " + d.JumpLines
                    + "  br " + d.BrakeLines + "  other " + d.NonProtocolLines, _style);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("go")) GameEvents.RaiseRunStarted();
            if (GUILayout.Button("crash")) GameEvents.RaiseCrashed();
            if (GUILayout.Button("gift")) GameEvents.RaiseGiftCollected();
            if (GUILayout.Button("spd " + _speed))
            {
                _speed = (_speed + 1) % (Messages.MaxSpeedLevel + 1);
                GameEvents.RaiseSpeedLevel(_speed);
            }
            GUILayout.EndHorizontal();

            foreach (string s in _log) GUILayout.Label(s, _style);
            GUILayout.EndArea();
        }
    }
}
