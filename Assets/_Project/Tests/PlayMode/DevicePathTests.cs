using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CityScooter.Devices;
using CityScooter.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CityScooter.Tests
{
    /// <summary>
    /// The no-hardware path end to end, as tools/fake_microbit.py uses it:
    /// Bootstrap creates DeviceBridge → UDP lines reach DeviceHub → game event goes back over UDP.
    /// </summary>
    public class DevicePathTests
    {
        [UnityTest]
        public IEnumerator FakeBoardOverUdp_Steers_Jumps_AndGetsCrash()
        {
            yield return null; // Bootstrap ran before the test scene loaded
            DeviceHub hub = DeviceHub.Instance;
            Assert.IsNotNull(hub, "Bootstrap should create the DeviceBridge object");
            Assert.AreEqual(DeviceHub.GameObjectName, hub.gameObject.name);

            using (var client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            {
                client.Client.ReceiveTimeout = 3000;
                var game = new IPEndPoint(IPAddress.Loopback, UdpDeviceTransport.DefaultPort);
                byte[] lines = Encoding.UTF8.GetBytes("t1|hi:v2\nt1|st:-50\nt1|jp\nt1|print from student\n");
                client.Send(lines, lines.Length, game);

                float waited = 0f;
                while (waited < 2f && !(hub.Devices.ContainsKey("t1") && hub.Devices["t1"].Lines >= 4))
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.IsTrue(hub.Devices.ContainsKey("t1"), "device t1 should be registered");
                DeviceInfo dev = hub.Devices["t1"];
                Assert.AreEqual("udp", dev.Kind);
                Assert.AreEqual("v2", dev.Version);
                Assert.AreEqual(1, dev.NonProtocolLines, "student print counts as non-protocol, not an error");
                Assert.AreEqual(-0.5f, hub.Mixer.Steer(DeviceHub.Now), 1e-3f);
                Assert.IsTrue(hub.Mixer.ConsumeJump());

                GameEvents.RaiseCrashed();
                var from = new IPEndPoint(IPAddress.Any, 0);
                byte[] reply = client.Receive(ref from);
                Assert.AreEqual("crash\n", Encoding.UTF8.GetString(reply));
            }
        }
    }
}
