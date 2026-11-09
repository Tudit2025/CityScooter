using System.Collections.Generic;
using CityScooter.Game;
using CityScooter.Protocol;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Students' programs trigger the game's own events with "do:..." (protocol v1): start a police chase, set the
    /// wanted stars, call the helicopter, order a turn, send traffic or roadworks. Example rule a student can build:
    /// count "ko" with a variable and after 3 punches send "do:chase". At most one trigger a second per board during a
    /// ride. Triggers sent between rides (in "on start", on the start screen, on the crash screen) happen just after
    /// the next ride starts, and again at the start of every ride after that: a program's "on start" runs once, but it
    /// sets up every ride. They stop when the program sends that kind again (the last one counts) or the board goes
    /// away (unplugged, re-flashed, the simulator runs new code). Hazards are placed out of sight ahead (never on a
    /// boulevard transition), and the usual messages (chase:1, wanted:N, warn:car, turn:1 ...) follow as for any other event.
    /// </summary>
    public sealed class ProgramTriggers : MonoBehaviour
    {
        public RiderController rider;
        public TrackGenerator track;
        public float perBoardCooldown = 1f;
        [Tooltip("Seconds into the next ride before triggers sent between rides happen.")]
        public float queuedDelay = 0.4f;

        /// <summary>For tests: triggers carried out, and ones dropped (too fast, nowhere to put it).</summary>
        public int Done { get; private set; }
        public int Dropped { get; private set; }
        /// <summary>Triggers sent since the last ride started, waiting for the next ride.</summary>
        public int Queued => _queue.Count;
        /// <summary>Triggers that happen at the start of every ride (sent between earlier rides).</summary>
        public int Opening => _opening.Count;

        struct Pending { public string Device, Name; public int Value; }
        const int MaxQueued = 6;
        readonly List<Pending> _queue = new List<Pending>();
        readonly List<Pending> _opening = new List<Pending>();
        readonly Dictionary<string, float> _next = new Dictionary<string, float>();
        float _runQueueAt = -1f;

        // Trigger name -> hazard pattern asset (theme.hazards) it places ahead.
        /// <summary>Hazard pattern asset names the triggers use (a test checks they exist in the theme).</summary>
        public static IEnumerable<string> PatternNames => PatternFor.Values;

        static readonly Dictionary<string, string> PatternFor = new Dictionary<string, string>
        {
            [Messages.DoCar] = "HZ_OncomingCar", [Messages.DoSlowCar] = "HZ_SlowCar", [Messages.DoRoadworks] = "HZ_RoadworksShort",
            [Messages.DoPolice] = "HZ_PoliceStop", [Messages.DoCrossing] = "HZ_Crossing", [Messages.DoManhole] = "HZ_Manhole",
        };

        void OnEnable()
        {
            GameEvents.TriggerRequested += OnTrigger;
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.DeviceLeft += OnDeviceLeft;
        }

        void OnDisable()
        {
            GameEvents.TriggerRequested -= OnTrigger;
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.DeviceLeft -= OnDeviceLeft;
        }

        void OnTrigger(string deviceId, string name, int value)
        {
            if (RaceDirector.On)
            {
                // ---- Radio race (v0.4): everyone gets the same road, so programs cannot change it in race mode.
                Dropped++;
                _queue.Clear();
                _opening.Clear();
                Caption(deviceId ?? "?", Label(name, value) + ": not in race mode");
                return;
            }
            string key = deviceId ?? "?";
            if (rider == null) { Dropped++; return; }
            if (!rider.Running)
            {
                // Between rides: keep it for the start of the next ride, and of every ride after that. One of each
                // kind is enough, so a program that sends it in a loop does not pile them up.
                _queue.RemoveAll(q => q.Name == name);
                if (_queue.Count >= MaxQueued) _queue.RemoveAt(0);
                _queue.Add(new Pending { Device = key, Name = name, Value = value });
                Caption(key, Label(name, value) + " (every ride)");
                return;
            }
            if (_next.TryGetValue(key, out float next) && Time.time < next) { Dropped++; return; }
            if (Run(key, name, value)) _next[key] = Time.time + perBoardCooldown;
        }

        void OnRunStarted()
        {
            _next.Clear();
            if (RaceDirector.On)
            {
                // ---- Radio race (v0.4): events kept from before race mode do not happen in a race
                _queue.Clear();
                _opening.Clear();
                _runQueueAt = -1f;
                return;
            }
            // What was sent since the last ride joins the rides' opening: same kind -> the newest one counts.
            foreach (Pending q in _queue)
            {
                _opening.RemoveAll(o => o.Name == q.Name);
                if (_opening.Count >= MaxQueued) _opening.RemoveAt(0);
                _opening.Add(q);
            }
            _queue.Clear();
            _runQueueAt = _opening.Count > 0 ? Time.time + queuedDelay : -1f;
        }

        // Unplugged, re-flashed, or the simulator runs new code: the old program's set-up goes with it.
        void OnDeviceLeft(string deviceId)
        {
            string key = deviceId ?? "?";
            _queue.RemoveAll(q => q.Device == key);
            _opening.RemoveAll(o => o.Device == key);
        }

        void Update()
        {
            if (_runQueueAt < 0f || Time.time < _runQueueAt || rider == null || !rider.Running) return;
            _runQueueAt = -1f;
            foreach (Pending p in _opening.ToArray()) Run(p.Device, p.Name, p.Value);
        }

        bool Run(string device, string name, int value)
        {
            if (!Do(name, value))
            {
                Dropped++;
                Caption(device, Label(name, value) + ": no room right now");
                return false;
            }
            Done++;
            Caption(device, Label(name, value));
            return true;
        }

        static void Caption(string device, string text)
        {
            GameController game = GameController.Instance;
            if (game != null && game.hud != null) game.hud.Caption((device == "sim" ? "Simulator" : device) + " → " + text);
        }

        // False if it could not be done now.
        bool Do(string name, int value)
        {
            PoliceChase police = PoliceChase.Instance;
            switch (name)
            {
                case Messages.DoChase:
                    if (police == null) return false;
                    police.SetWanted(Mathf.Max(police.Wanted, 1));
                    return true;
                case Messages.DoStars:
                    if (police == null) return false;
                    police.SetWanted(value);
                    return true;
                case Messages.DoHeli:
                    if (police == null) return false;
                    police.SetWanted(Mathf.Max(police.Wanted, 3));
                    return true;
                case Messages.DoCalm:
                    if (police == null) return false;
                    police.SetWanted(0);
                    return true;
                case Messages.DoRamp: // ---- Ramps and air (v0.3): a ramp in the rider's lane, out of sight ahead
                    if (track == null) return false;
                    return track.TryPlaceRampAhead(Mathf.Clamp(Mathf.Max(100f, rider.Speed * 4f), 60f, Mathf.Max(60f, track.buildAhead - 100f)));
                case Messages.DoTurn:
                    FakeTurnDirector turns = FakeTurnDirector.Instance;
                    if (turns == null || value == 0) return false;
                    float speed = Mathf.Max(10f, rider.Speed);
                    return turns.RequestTurn(rider.transform.position.z + Mathf.Max(150f, speed * 4f), value, 0.9f, Mathf.Max(160f, speed * 6f));
                default:
                    if (!PatternFor.TryGetValue(name, out string pattern) || track == null || track.theme == null || track.theme.hazards == null) return false;
                    HazardPattern p = System.Array.Find(track.theme.hazards, h => h != null && h.name == pattern);
                    if (p == null) return false;
                    float ahead = Mathf.Min(Mathf.Max(120f, rider.Speed * 3.5f), track.buildAhead - p.length - 12f);
                    return track.TryPlacePatternAhead(p, ahead);
            }
        }

        static string Label(string name, int value)
        {
            switch (name)
            {
                case Messages.DoChase: return "POLICE CHASE!";
                case Messages.DoStars: return value > 0 ? "WANTED: " + Mathf.Clamp(value, 0, 5) + (value == 1 ? " STAR" : " STARS") : "police give up";
                case Messages.DoHeli: return "POLICE HELICOPTER!";
                case Messages.DoCalm: return "police give up";
                case Messages.DoTurn: return value < 0 ? "turn LEFT ahead" : "turn RIGHT ahead";
                case Messages.DoCar: return "oncoming car!";
                case Messages.DoSlowCar: return "slow car ahead";
                case Messages.DoRoadworks: return "ROADWORKS ahead";
                case Messages.DoPolice: return "police stop ahead";
                case Messages.DoCrossing: return "people crossing ahead";
                case Messages.DoRamp: return "RAMP ahead!"; // ---- Ramps and air (v0.3)
                default: return "open manhole ahead";
            }
        }
    }
}
