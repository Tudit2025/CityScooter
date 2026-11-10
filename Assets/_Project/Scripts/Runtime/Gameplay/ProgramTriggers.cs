using System.Collections.Generic;
using CityScooter.Devices;
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
    /// ride; triggers sent between rides (start screen, crash screen) wait and happen just after the next ride starts.
    /// "open:..." (11/9) asks for the same events at the start of EVERY ride: "on start" runs once but sets up every ride.
    /// Programs say it again about once a second (the blocks do), so it survives the game not listening yet, a program
    /// started in the middle of a ride, and a USB hiccup; a new program that no longer says it loses it after a few seconds.
    /// Hazards are placed out of sight ahead (never on a boulevard transition), and the usual messages (chase:1,
    /// wanted:N, warn:car, turn:1 ...) follow as for any other event.
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
        /// <summary>Triggers waiting for the next ride.</summary>
        public int Queued => _queue.Count;
        /// <summary>Events boards asked for at the start of every ride ("open:..."), still being said.</summary>
        public int Opening => _opening.Count;
        [Tooltip("An every-ride event (open:) is forgotten when its board has talked this many seconds without saying it again (a new program).")]
        public float openingForgetSeconds = 3f;

        struct Pending { public string Device, Name; public int Value; }
        const int MaxQueued = 6;
        readonly List<Pending> _queue = new List<Pending>();
        readonly Dictionary<string, float> _next = new Dictionary<string, float>();
        float _runQueueAt = -1f;

        sealed class Open { public string Device, Name; public int Value; public double Heard; }
        readonly List<Open> _opening = new List<Open>();

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
            GameEvents.OpeningRequested += OnOpening;
            GameEvents.RunStarted += OnRunStarted;
        }

        void OnDisable()
        {
            GameEvents.TriggerRequested -= OnTrigger;
            GameEvents.OpeningRequested -= OnOpening;
            GameEvents.RunStarted -= OnRunStarted;
        }

        // "open:stars:5": said about once a second. Only a new one (or a changed number) does something now: during a
        // ride it happens at once (the program just started), between rides the caption says it is for every ride.
        void OnOpening(string deviceId, string name, int value)
        {
            string key = deviceId ?? "?";
            if (RaceDirector.On && !RaceAllows(name))
            {
                // ---- Radio race: the same road for everyone. It comes every second, so the caption shows now and then.
                if (!_raceCaptionAt.TryGetValue(name, out float last) || Time.unscaledTime - last > 8f)
                {
                    _raceCaptionAt[name] = Time.unscaledTime;
                    Caption(key, Label(name, value) + ": not in race mode");
                }
                return;
            }
            Open o = _opening.Find(x => x.Device == key && x.Name == name);
            if (o != null && o.Value == value) { o.Heard = DeviceHub.Now; return; }
            if (o == null)
            {
                o = new Open { Device = key, Name = name };
                _opening.Add(o);
            }
            o.Value = value;
            o.Heard = DeviceHub.Now;
            if (rider != null && rider.Running) Run(key, name, value);
            else Caption(key, Label(name, value) + " (every ride)");
        }

        // A program that stopped saying it (a new program without it) loses it; an unplugged board keeps it for when it
        // comes back (its program may still be running), it just does not count while it is away.
        void ForgetOldOpenings()
        {
            DeviceHub hub = DeviceHub.Instance;
            if (hub == null) return;
            _opening.RemoveAll(o => hub.Devices.TryGetValue(o.Device, out DeviceInfo d) && d.LastSeen - o.Heard > openingForgetSeconds);
        }

        static bool Present(string device)
        {
            DeviceHub hub = DeviceHub.Instance;
            return hub == null || !hub.Devices.TryGetValue(device, out DeviceInfo d) || d.Connected;
        }

        // ---- Item race (v0.5, docs/item-race.md): in race mode (the lobby too) the police events still work, they only
        // change things for the rider who asked: chase, stars:N, heli, calm. Events that change the road (turn, car,
        // slowcar, roadworks, police stop, crossing, manhole, ramp) stay ignored: everyone must get the same road.
        public static bool RaceAllows(string name) =>
            name == Messages.DoChase || name == Messages.DoStars || name == Messages.DoHeli || name == Messages.DoCalm;

        readonly Dictionary<string, float> _raceCaptionAt = new Dictionary<string, float>();

        void OnTrigger(string deviceId, string name, int value)
        {
            if (RaceDirector.On && !RaceAllows(name))
            {
                // ---- Radio race (v0.4): everyone gets the same road, so programs cannot change it in race mode.
                Dropped++;
                _queue.RemoveAll(q => !RaceAllows(q.Name));
                Caption(deviceId ?? "?", Label(name, value) + ": not in race mode");
                return;
            }
            string key = deviceId ?? "?";
            if (rider == null) { Dropped++; return; }
            if (!rider.Running)
            {
                // Between rides: keep it for the start of the next ride. One of each kind is enough, so a program
                // that sends it in a loop does not pile them up.
                _queue.RemoveAll(q => q.Name == name);
                if (_queue.Count >= MaxQueued) _queue.RemoveAt(0);
                _queue.Add(new Pending { Device = key, Name = name, Value = value });
                Caption(key, Label(name, value) + " (next ride)");
                return;
            }
            if (_next.TryGetValue(key, out float next) && Time.time < next) { Dropped++; return; }
            if (Run(key, name, value)) _next[key] = Time.time + perBoardCooldown;
        }

        void OnRunStarted()
        {
            _next.Clear();
            // ---- Radio race: road events kept from before race mode do not happen in a race (police events do, v0.5)
            if (RaceDirector.On) _queue.RemoveAll(q => !RaceAllows(q.Name));
            ForgetOldOpenings();
            bool opening = _opening.Exists(o => !RaceDirector.On || RaceAllows(o.Name));
            _runQueueAt = _queue.Count > 0 || opening ? Time.time + queuedDelay : -1f;
        }

        void Update()
        {
            if (_runQueueAt < 0f || Time.time < _runQueueAt || rider == null || !rider.Running) return;
            _runQueueAt = -1f;
            // Every ride's events first, then the ones sent since the last ride (so a "do:" sent between rides wins this ride).
            bool race = RaceDirector.On;
            foreach (Open o in _opening.ToArray())
                if (Present(o.Device) && (!race || RaceAllows(o.Name))) Run(o.Device, o.Name, o.Value);
            var pending = new List<Pending>(_queue);
            _queue.Clear();
            foreach (Pending p in pending)
                if (!race || RaceAllows(p.Name)) Run(p.Device, p.Name, p.Value);
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
