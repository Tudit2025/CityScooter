using System.Runtime.InteropServices;
using CityScooter.Protocol;
using UnityEngine;

namespace CityScooter.Devices
{
    /// <summary>
    /// Outbound half of the web shell contract (docs/bridge.md). Inbound lines arrive through
    /// DeviceHub.OnDeviceLines via SendMessage; outbound game messages leave through the jslib
    /// functions in Plugins/WebGL/CityScooterBridge.jslib. Does nothing outside WebGL builds.
    /// </summary>
    [RequireComponent(typeof(DeviceHub))]
    public sealed class WebShellBridge : MonoBehaviour, IDeviceOutput
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CS_SendToDevices(string target, string line);
        [DllImport("__Internal")] static extern void CS_UnityReady(string protocolVersion);
#endif

        DeviceHub _hub;

        void Awake() => _hub = GetComponent<DeviceHub>();

        void OnEnable() => _hub.RegisterOutput(this);

        void OnDisable() => _hub.UnregisterOutput(this);

        void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Unity must not swallow every key on the page: the shell has text boxes (code editor, notes).
            // The canvas still gets keys when it has focus.
            WebGLInput.captureAllKeyboardInput = false;
            CS_UnityReady(Messages.ProtocolVersion);
#endif
        }

        public void Send(string target, string line)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CS_SendToDevices(target, line);
#endif
        }
    }
}
