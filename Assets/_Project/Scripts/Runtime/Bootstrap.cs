using CityScooter.Devices;
using CityScooter.DevTools;
using UnityEngine;

namespace CityScooter
{
    /// <summary>
    /// Creates the persistent "DeviceBridge" object before the first scene loads, so no scene has to
    /// contain it. Scenes stay pure art/gameplay (tech artist owns them) and never conflict with this plumbing.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateDeviceBridge()
        {
            if (DeviceHub.Instance != null) return;
            var go = new GameObject(DeviceHub.GameObjectName);
            Object.DontDestroyOnLoad(go);
            go.AddComponent<DeviceHub>();
            go.AddComponent<WebShellBridge>();
#if UNITY_EDITOR || UNITY_STANDALONE
            go.AddComponent<UdpDeviceTransport>();
#endif
            go.AddComponent<DeviceFeedback>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            go.AddComponent<DeviceDebugOverlay>();
#endif
        }
    }
}
