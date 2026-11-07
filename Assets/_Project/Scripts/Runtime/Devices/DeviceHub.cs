using System;
using System.Collections.Generic;
using CityScooter.Protocol;
using UnityEngine;

namespace CityScooter.Devices
{
    /// <summary>Something that can deliver a line to devices (web shell, UDP dev tools, ...).</summary>
    public interface IDeviceOutput
    {
        /// <param name="target">A device id, or "*" for every connected device.</param>
        void Send(string target, string line);
    }

    public sealed class DeviceInfo
    {
        public string Id;
        /// <summary>usb | sim | udp | web</summary>
        public string Kind = "?";
        /// <summary>"v1" / "v2" once the board said "hi:..."; null until then.</summary>
        public string Version;
        public bool Connected;
        public double LastSeen;
        public int Lines;
        public int SteerLines;
        public int JumpLines;
        public int BrakeLines;
        public int NonProtocolLines;
    }

    /// <summary>
    /// Single entry point for every controller. Transports push lines in with <see cref="Receive"/>,
    /// gameplay reads the merged result through <see cref="Controls.RiderInputSource"/>, and game
    /// messages go out to all devices with <see cref="Send"/>.
    /// The GameObject is always named "DeviceBridge": the web shell calls
    /// unityInstance.SendMessage("DeviceBridge", "OnDeviceLines", ...). Created by <see cref="Bootstrap"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeviceHub : MonoBehaviour
    {
        public const string GameObjectName = "DeviceBridge";

        public static DeviceHub Instance { get; private set; }
        public static double Now => Time.realtimeSinceStartupAsDouble;

        /// <summary>deviceId, raw line, parsed command. For the debug overlay and future in-game monitor.</summary>
        public event Action<string, string, Command> LineReceived;
        /// <summary>target, line.</summary>
        public event Action<string, string> LineSent;
        public event Action<DeviceInfo> DeviceChanged;

        public ControlMixer Mixer { get; } = new ControlMixer();
        public IReadOnlyDictionary<string, DeviceInfo> Devices => _devices;

        /// <summary>The shell turns this off in simulator mode, where arrow keys tilt the simulated board instead.</summary>
        public bool KeyboardEnabled { get; private set; } = true;

        readonly Dictionary<string, DeviceInfo> _devices = new Dictionary<string, DeviceInfo>();
        readonly List<IDeviceOutput> _outputs = new List<IDeviceOutput>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            gameObject.name = GameObjectName;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RegisterOutput(IDeviceOutput output)
        {
            if (output != null && !_outputs.Contains(output)) _outputs.Add(output);
        }

        public void UnregisterOutput(IDeviceOutput output) => _outputs.Remove(output);

        /// <summary>Feed one line from any device.</summary>
        public void Receive(string deviceId, string line)
        {
            if (string.IsNullOrEmpty(deviceId)) deviceId = "?";
            Command cmd = CommandParser.Parse(line);
            DeviceInfo dev = GetOrAdd(deviceId);
            dev.LastSeen = Now;
            dev.Lines++;
            bool changed = false;
            if (!dev.Connected)
            {
                dev.Connected = true;
                changed = true;
            }
            switch (cmd.Kind)
            {
                case CommandKind.Steer: dev.SteerLines++; break;
                case CommandKind.Jump: dev.JumpLines++; break;
                case CommandKind.Brake: dev.BrakeLines++; break;
                case CommandKind.Trigger:
                    Game.GameEvents.RaiseTriggerRequested(deviceId, cmd.Text, Mathf.RoundToInt(cmd.Value));
                    break;
                case CommandKind.Say:
                    Game.GameEvents.RaiseBoardSaid(deviceId, cmd.Text);
                    break;
                case CommandKind.Hello:
                    if (dev.Version != cmd.Text)
                    {
                        dev.Version = cmd.Text;
                        changed = true;
                    }
                    break;
                case CommandKind.Unknown:
                case CommandKind.Malformed:
                    dev.NonProtocolLines++;
                    break;
            }
            Mixer.Apply(cmd, Now);
            if (changed) DeviceChanged?.Invoke(dev);
            LineReceived?.Invoke(deviceId, line, cmd);
        }

        public void SetStatus(string deviceId, string kind, bool connected, string version = null)
        {
            DeviceInfo dev = GetOrAdd(deviceId);
            if (!string.IsNullOrEmpty(kind)) dev.Kind = kind;
            if (!string.IsNullOrEmpty(version)) dev.Version = version;
            dev.Connected = connected;
            dev.LastSeen = Now;
            DeviceChanged?.Invoke(dev);
        }

        /// <summary>Send a game message ("crash", "spd:4", ...) to every device, or to one device id.</summary>
        public void Send(string line, string target = "*")
        {
            for (int i = 0; i < _outputs.Count; i++) _outputs[i].Send(target, line);
            LineSent?.Invoke(target, line);
        }

        DeviceInfo GetOrAdd(string id)
        {
            if (!_devices.TryGetValue(id, out DeviceInfo dev))
            {
                dev = new DeviceInfo { Id = id };
                _devices.Add(id, dev);
            }
            return dev;
        }

        // ------------------------------------------------------------------
        // Called by the web shell via unityInstance.SendMessage (docs/bridge.md).

        /// <summary>payload: one or more entries separated by '\n'; each entry is "deviceId|line".</summary>
        public void OnDeviceLines(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return;
            int start = 0;
            while (start < payload.Length)
            {
                int end = payload.IndexOf('\n', start);
                if (end < 0) end = payload.Length;
                if (end > start)
                {
                    string entry = payload.Substring(start, end - start);
                    int bar = entry.IndexOf('|');
                    if (bar > 0) Receive(entry.Substring(0, bar), entry.Substring(bar + 1));
                    else Receive("web", entry);
                }
                start = end + 1;
            }
        }

        /// <summary>payload: "deviceId|kind|connected|version", e.g. "usb1|usb|1|v2" or "sim|sim|0|".</summary>
        public void OnDeviceStatus(string payload)
        {
            string[] parts = (payload ?? string.Empty).Split('|');
            if (parts.Length < 3 || parts[0].Length == 0) return;
            string version = parts.Length > 3 && parts[3].Length > 0 ? parts[3] : null;
            SetStatus(parts[0], parts[1], parts[2] == "1", version);
        }

        /// <summary>payload: "1" or "0".</summary>
        public void SetKeyboardEnabled(string payload) => KeyboardEnabled = payload != "0";
    }
}
