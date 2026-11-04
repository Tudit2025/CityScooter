#if UNITY_EDITOR || UNITY_STANDALONE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace CityScooter.Devices
{
    /// <summary>
    /// Desktop-only transport so the game can be driven without a browser or hardware:
    /// tools/fake_microbit.py (no hardware) and future desktop bridges send UDP to 127.0.0.1:47800.
    /// A datagram holds one or more lines; a line may be prefixed "deviceId|", otherwise the id is "udp:&lt;port&gt;".
    /// Game messages are sent back to every peer heard from in the last few seconds.
    /// </summary>
    [RequireComponent(typeof(DeviceHub))]
    public sealed class UdpDeviceTransport : MonoBehaviour, IDeviceOutput
    {
        public const int DefaultPort = 47800;

        public int listenPort = DefaultPort;
        public float peerTimeoutSeconds = 5f;

        struct Packet
        {
            public string Id;
            public string Line;
            public IPEndPoint From;
        }

        readonly ConcurrentQueue<Packet> _inbox = new ConcurrentQueue<Packet>();
        readonly Dictionary<string, IPEndPoint> _peers = new Dictionary<string, IPEndPoint>();
        readonly Dictionary<string, double> _peerSeen = new Dictionary<string, double>();
        readonly List<string> _expired = new List<string>();
        readonly HashSet<IPEndPoint> _sentTo = new HashSet<IPEndPoint>();

        UdpClient _udp;
        Thread _thread;
        volatile bool _running;
        DeviceHub _hub;

        void OnEnable()
        {
            _hub = GetComponent<DeviceHub>();
            try
            {
                _udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, listenPort));
            }
            catch (SocketException e)
            {
                Debug.LogWarning("[CityScooter] UDP port " + listenPort + " unavailable (" + e.Message + "); dev tools disabled.");
                _udp = null;
                return;
            }
            _running = true;
            _thread = new Thread(ReceiveLoop) { IsBackground = true, Name = "CityScooter UDP" };
            _thread.Start();
            _hub.RegisterOutput(this);
        }

        void OnDisable()
        {
            _running = false;
            if (_hub != null) _hub.UnregisterOutput(this);
            try { _udp?.Close(); } catch (Exception) { /* closing anyway */ }
            _udp = null;
            _thread = null;
        }

        void ReceiveLoop()
        {
            while (_running)
            {
                try
                {
                    var from = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _udp.Receive(ref from);
                    string text = Encoding.UTF8.GetString(data);
                    foreach (string raw in text.Split('\n'))
                    {
                        string line = raw.TrimEnd('\r');
                        if (line.Length == 0) continue;
                        string id;
                        int bar = line.IndexOf('|');
                        if (bar > 0)
                        {
                            id = line.Substring(0, bar);
                            line = line.Substring(bar + 1);
                        }
                        else
                        {
                            id = "udp:" + from.Port;
                        }
                        _inbox.Enqueue(new Packet { Id = id, Line = line, From = from });
                    }
                }
                catch (SocketException)
                {
                    // Windows reports ICMP "port unreachable" from an earlier send as a receive error; keep going.
                    if (!_running) break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        void Update()
        {
            if (_udp == null) return;
            double now = DeviceHub.Now;
            while (_inbox.TryDequeue(out Packet p))
            {
                if (!_peers.ContainsKey(p.Id)) _hub.SetStatus(p.Id, "udp", true);
                _peers[p.Id] = p.From;
                _peerSeen[p.Id] = now;
                _hub.Receive(p.Id, p.Line);
            }
            if (_peerSeen.Count == 0) return;
            _expired.Clear();
            foreach (KeyValuePair<string, double> kv in _peerSeen)
            {
                if (now - kv.Value > peerTimeoutSeconds) _expired.Add(kv.Key);
            }
            foreach (string id in _expired)
            {
                _peers.Remove(id);
                _peerSeen.Remove(id);
                _hub.SetStatus(id, "udp", false);
            }
        }

        public void Send(string target, string line)
        {
            if (_udp == null || _peers.Count == 0) return;
            byte[] bytes = Encoding.UTF8.GetBytes(line + "\n");
            _sentTo.Clear();
            foreach (KeyValuePair<string, IPEndPoint> kv in _peers)
            {
                if (target != "*" && target != kv.Key) continue;
                if (!_sentTo.Add(kv.Value)) continue; // several fake boards can share one socket
                try { _udp.Send(bytes, bytes.Length, kv.Value); } catch (Exception) { /* peer gone */ }
            }
        }
    }
}
#endif
