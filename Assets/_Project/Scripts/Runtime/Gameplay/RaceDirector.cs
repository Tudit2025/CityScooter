using System.Collections.Generic;
using CityScooter.Controls;
using CityScooter.Devices;
using CityScooter.Game;
using CityScooter.Protocol;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>Where the radio race is (docs/radio-race.md, "实现约定 v1").</summary>
    public enum RaceState
    {
        /// <summary>Normal play: no board has sent "race:" in the last 10 s.</summary>
        Off,
        /// <summary>The race lobby: who can I hear? A rides a practice ride; "go:" starts the countdown.</summary>
        Lobby,
        /// <summary>3, 2, 1 on the race's road (seed and start district from "go:").</summary>
        Countdown,
        Racing,
        /// <summary>Crossed the line: "fin:" goes out; results after 30 s or when everyone has finished.</summary>
        Finished,
        /// <summary>The podium, until the next "go:" (A goes back to the lobby).</summary>
        Results,
    }

    /// <summary>
    /// v0.4 radio race (11/9). No server: the micro:bits' radio is the network. A board that sends "race:NAME:GROUP"
    /// puts the game in race mode (off again 10 s after the last one). The game tells its board where the rider is
    /// ("pos:DIST:LANE", 4 times a second in a race, "pos:0:0" once a second in the lobby; "fin:SECONDS" every 0.5 s for
    /// 10 s after the finish line); the board broadcasts it and passes on what it hears from the others as
    /// "rx:p:BEN:1234:-2", "rx:f:BEN:72.35" and "rx:go:4821:uwa:2000".
    /// Everyone gets the same road from the same seed: TrackGenerator.seed, the start district and FakeTurnDirector all
    /// use the go:'s seed, and speed-dependent planning is fixed (fixedPlanning). Other riders are ghosts: copies of the
    /// rider model in their own colour, placed at their distance and lane, moved on between packets (dead reckoning),
    /// never in the way. In race mode: no cartoon mode, "start:" is ignored, "do:" / "open:" events that change the road
    /// are ignored; a crash is a 3 s stop and the ride goes on from the same spot.
    /// v0.5 item race (11/10, docs/item-race.md): the police work in race mode (a program's own "do:chase" /
    /// "do:stars:N" / "do:heli" / "do:calm", and the police item); ARRESTED and BUSTED are the same 3 s stop, then the
    /// wanted level is 0 again. Items are in <see cref="RaceItems"/>.
    /// GameController adds it at runtime (no scene changes). Without "race:" nothing here runs.
    /// </summary>
    [DefaultExecutionOrder(60)] // after the rider, the track and the districts moved this frame; LateUpdate after the camera
    public sealed class RaceDirector : MonoBehaviour
    {
        // ---- Race numbers (docs/radio-race.md "实现约定 v1"); the race tuning lives here, not in GameTuning.
        public const float ModeTimeoutSeconds = 10f;
        public const float LobbyHeardSeconds = 3f;
        public const float CountdownSeconds = 3f;
        public const float CrashPenaltySeconds = 3f;
        /// <summary>After a crash penalty obstacles are passed through this long (whatever was hit is still there).</summary>
        public const float RespawnPassSeconds = 1.5f;
        public const float RacePositionsPerSecond = 4f;
        public const float LobbyPositionsPerSecond = 1f;
        public const float FinishRepeatSeconds = 0.5f;
        public const float FinishRepeatFor = 10f;
        public const float ResultsAfterSeconds = 30f;
        /// <summary>Even when everyone has finished, the FINISH! moment stays this long before the results.</summary>
        public const float ResultsMinSeconds = 3f;
        public const float GhostGreySeconds = 2f;
        public const float GhostGoneSeconds = 10f;
        public const int MaxGhosts = 8;
        /// <summary>Plausibility: a rider may move at most top speed x seconds x 1.5 + 20 m between two messages.</summary>
        public const float PlausibleFactor = 1.5f, PlausibleSlack = 20f;
        /// <summary>Dead reckoning: never guess further ahead than this many seconds after the last position.</summary>
        public const float ExtrapolateSeconds = 0.75f;

        public static RaceDirector Instance { get; private set; }
        /// <summary>True in race mode (any state but Off): no cartoon mode, no start:, no road-changing do: events.</summary>
        public static bool On => Instance != null && Instance.State != RaceState.Off;

        public RiderController rider;
        public TrackGenerator track;
        public HudView hud;

        /// <summary>One other rider, by name (from p: and f:).</summary>
        public sealed class Rival
        {
            public string Name;
            /// <summary>Index into <see cref="Palette"/>.</summary>
            public int Colour;
            /// <summary>Last position that was accepted (metres from the start, lane x).</summary>
            public float Distance, Lane;
            /// <summary>Estimated forward speed (m/s) from the last positions.</summary>
            public float Speed;
            /// <summary>Last p: or f: from this rider; last accepted position.</summary>
            public double HeardAt, AcceptedAt;
            public bool HasPosition;
            /// <summary>Moved further than anyone could (or finished impossibly fast): shown with "?", position not used.</summary>
            public bool Suspicious;
            public bool Finished;
            public float FinishTime;
            /// <summary>Heard since the last go: (counts in the race standings).</summary>
            public bool InRace;
            /// <summary>Where it is drawn now (dead reckoned, smoothed).</summary>
            public float ShownDistance, ShownLane;
            internal float ErrorDistance;
            internal bool Seen;
            internal readonly Queue<double> Packets = new Queue<double>();
            /// <summary>The ghost drawn for this rider (null when not drawn).</summary>
            public RaceGhost Ghost { get; internal set; }
            /// <summary>Drawn on the road right now (at most MaxGhosts are, the nearest).</summary>
            public bool Drawn => Ghost != null && Ghost.gameObject.activeSelf;

            /// <summary>Messages per second over the last LobbyHeardSeconds.</summary>
            public float PacketsPerSecond => Packets.Count / LobbyHeardSeconds;
            public bool SignalLost(double now) => now - HeardAt > GhostGreySeconds;
        }

        /// <summary>One line of the standings.</summary>
        public struct Standing
        {
            public string Name;
            public bool Me, Finished, Suspicious, Lost;
            public float Time, Distance;
            public int Colour;
        }

        /// <summary>Rival colours (not red: that is your own scarf). Names hash to the same colour on every screen.</summary>
        public static readonly Color[] Palette =
        {
            new Color(0.22f, 0.56f, 1f), new Color(0.18f, 0.82f, 0.32f), new Color(1f, 0.6f, 0.1f), new Color(0.68f, 0.4f, 1f),
            new Color(0.1f, 0.86f, 0.86f), new Color(1f, 0.45f, 0.78f), new Color(1f, 0.9f, 0.2f), new Color(0.62f, 0.92f, 0.18f),
            new Color(0.1f, 0.62f, 0.56f), new Color(1f, 0.7f, 0.45f), new Color(0.94f, 0.94f, 0.94f), new Color(0.92f, 0.25f, 0.92f),
        };

        public RaceState State { get; private set; } = RaceState.Off;
        /// <summary>From "race:": who I am and which radio group (race) I am in.</summary>
        public string MyName { get; private set; } = "";
        public int Group { get; private set; } = -1;
        /// <summary>A go: arrived since race mode came on: the road is the race's (seed, start district).</summary>
        public bool Seeded { get; private set; }
        public int Seed { get; private set; }
        public int TurnSeed => Seed;
        public string StartId { get; private set; } = RadioMessage.DefaultStart;
        public int Metres { get; private set; } = 2000;
        /// <summary>Seconds since GO (frozen at the finish time once finished; 0 before).</summary>
        public float RaceTime { get; private set; }
        /// <summary>My finish time (seconds); 0 until the finish line.</summary>
        public float FinishTime { get; private set; }
        /// <summary>Seconds left in the countdown.</summary>
        public float CountdownLeft => State == RaceState.Countdown ? Mathf.Max(0f, (float)(_raceStartAt - Now)) : 0f;
        /// <summary>Crashes in this race (each one a 3 s stop).</summary>
        public int Crashes { get; private set; }
        /// <summary>Seconds left of the current crash stop (0 when riding).</summary>
        public float PenaltyLeft => _penaltyUntil > 0d ? Mathf.Max(0f, (float)(_penaltyUntil - Now)) : 0f;
        /// <summary>How long the current (or last) stop is: 3 s for a crash, an arrest or a bust, 1.5 s for a rocket.</summary>
        public float PenaltySeconds { get; private set; } = CrashPenaltySeconds;
        /// <summary>v0.5: the police may come now (a practice ride in the lobby, or the race itself; not after the finish line).</summary>
        public bool PoliceAllowed => State == RaceState.Lobby || State == RaceState.Racing;
        /// <summary>v0.5: item boxes, items and what they do are on (the lobby with its practice rides, the countdown, the race).</summary>
        public bool ItemsOn => State == RaceState.Lobby || State == RaceState.Countdown || State == RaceState.Racing;
        /// <summary>Metres from the start line to my rider (0 before the start).</summary>
        public float MyDistance => rider != null ? Mathf.Max(0f, rider.Distance) : 0f;
        /// <summary>Seconds since the finish line (Finished and Results).</summary>
        public float SinceFinish => _finishedAt > 0d ? (float)(Now - _finishedAt) : 0f;
        public IReadOnlyList<Rival> Rivals => _rivals;
        /// <summary>go: messages ignored because a race was on (tests).</summary>
        public int IgnoredGo { get; private set; }
        /// <summary>The FINISH arch (null until a race has a length).</summary>
        public GameObject FinishArch => _arch;
        /// <summary>A press of A may start a ride now: normal play, or a practice ride in the lobby.</summary>
        public bool AllowsStart => State == RaceState.Off || (State == RaceState.Lobby && _lobbyFrame != Time.frameCount);
        /// <summary>World z of the start line (distance 0) on this road.</summary>
        public float StartZ => _game != null ? _game.startPosition.z : 0f;

        static double Now => Time.unscaledTimeAsDouble;

        readonly List<Rival> _rivals = new List<Rival>();
        GameController _game;
        RiderActions _actions;
        FakeTurnDirector _turns;
        double _lastRaceAt = double.NegativeInfinity, _raceStartAt, _goAt, _finishedAt, _penaltyUntil, _nextPos, _nextFin;
        int _savedSeed;
        string _savedStart;
        int _lobbyFrame = -1;
        float _respawnX;
        bool _posFast;
        GameObject _arch;
        Transform _ghostRoot;
        Transform _modelTemplate;

        GameTuning T => rider != null && rider.tuning != null ? rider.tuning : GameTuning.Default;

        void Awake() => Instance = this;

        void OnEnable()
        {
            Instance = this;
            GameEvents.RaceRequested += OnRaceRequested;
            GameEvents.RadioHeard += OnRadioHeard;
            GameEvents.RunStarted += OnRunStarted;
        }

        void OnDisable()
        {
            GameEvents.RaceRequested -= OnRaceRequested;
            GameEvents.RadioHeard -= OnRadioHeard;
            GameEvents.RunStarted -= OnRunStarted;
            if (State != RaceState.Off) Leave(false);
            if (Instance == this) Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        GameController Game { get { if (_game == null) _game = GetComponent<GameController>(); return _game; } }

        // ---------------------------------------------------------------- messages from the boards

        void OnRaceRequested(string deviceId, string name, int group)
        {
            _lastRaceAt = Now;
            if (name != MyName)
            {
                MyName = name;
                _rivals.RemoveAll(r => { if (r.Name != name) return false; DropGhost(r); return true; }); // that is me now
            }
            if (group != Group)
            {
                // Another race (group): the riders heard so far belong to the old one.
                if (Group >= 0) ClearRivals();
                Group = group;
            }
            if (State == RaceState.Off) Enter();
        }

        void OnRadioHeard(string deviceId, string payload)
        {
            if (State == RaceState.Off) return;
            RadioMessage m = RadioMessage.Parse(payload);
            switch (m.Kind)
            {
                case RadioKind.Position: OnPosition(m); break;
                case RadioKind.Finish: OnFinish(m); break;
                case RadioKind.Go: OnGo(m); break;
            }
        }

        bool InRaceStates => State == RaceState.Countdown || State == RaceState.Racing || State == RaceState.Finished || State == RaceState.Results;

        Rival Find(string name, bool add)
        {
            foreach (Rival r in _rivals) if (r.Name == name) return r;
            if (!add) return null;
            var rival = new Rival { Name = name, Colour = PickColour(name) };
            if (Seeded && InRaceStates) { rival.HasPosition = true; rival.AcceptedAt = _goAt; } // a newcomer starts from 0
            _rivals.Add(rival);
            return rival;
        }

        void Heard(Rival r, double now)
        {
            r.HeardAt = now;
            r.Packets.Enqueue(now);
            while (r.Packets.Count > 0 && now - r.Packets.Peek() > LobbyHeardSeconds) r.Packets.Dequeue();
            if (Seeded && InRaceStates) r.InRace = true;
        }

        /// <summary>Fastest anyone can go at this point of the race (m/s), boost included.</summary>
        float TopSpeed => T.MaxSpeedAt(Mathf.Max(0f, RaceTime)) * Mathf.Max(1f, T.boostFactor);

        void OnPosition(RadioMessage m)
        {
            if (m.Name == MyName) return; // my own board never hears itself; a copy of my name is not drawn
            double now = Now;
            Rival r = Find(m.Name, true);
            Heard(r, now);
            if (r.Finished && Seeded && InRaceStates) return; // parked past the finish line until the next go
            if (r.HasPosition)
            {
                float seconds = (float)(now - r.AcceptedAt);
                float allowed = TopSpeed * Mathf.Max(0f, seconds) * PlausibleFactor + PlausibleSlack;
                if (m.Distance - r.Distance > allowed)
                {
                    r.Suspicious = true; // "p:AMY:99999": nobody rides that far that fast. Shown with "?", not used.
                    return;
                }
                if (seconds > 0.05f)
                {
                    float v = Mathf.Clamp((m.Distance - r.Distance) / seconds, 0f, TopSpeed);
                    r.Speed = r.Speed > 0f ? Mathf.Lerp(r.Speed, v, 0.6f) : v;
                }
                else if (m.Distance < r.Distance) r.Speed = 0f;
            }
            float before = r.HasPosition ? r.ShownDistance : m.Distance;
            r.Distance = m.Distance;
            r.Lane = m.Lane;
            r.AcceptedAt = now;
            if (!r.HasPosition) r.Speed = 0f;
            if (!r.Seen) { r.ShownLane = m.Lane; r.Seen = true; }
            r.HasPosition = true;
            r.ErrorDistance = Mathf.Abs(before - m.Distance) > 30f ? 0f : before - m.Distance; // blend out small jumps, snap big ones
            r.ShownDistance = m.Distance + r.ErrorDistance;
        }

        void OnFinish(RadioMessage m)
        {
            if (m.Name == MyName || !Seeded || !(State == RaceState.Racing || State == RaceState.Finished || State == RaceState.Results)) return;
            Rival r = Find(m.Name, true);
            Heard(r, Now);
            if (r.Finished) return; // sent every 0.5 s for 10 s: the first one counts
            r.Finished = true;
            r.FinishTime = m.Seconds;
            // A finish faster than top speed allows: "?" (f:AMY:1.00)
            if (m.Seconds < Metres / Mathf.Max(1f, T.MaxSpeedAt(m.Seconds) * Mathf.Max(1f, T.boostFactor))) r.Suspicious = true;
            r.Speed = 0f;
        }

        void OnGo(RadioMessage m)
        {
            if (State != RaceState.Lobby && State != RaceState.Results) { IgnoredGo++; return; } // nobody restarts a race in progress
            StartCountdown(m.Seed, m.Start, m.Metres);
        }

        // ---------------------------------------------------------------- states

        void Enter()
        {
            if (track != null) { _savedSeed = track.seed; _savedStart = track.startDistrictOverride; }
            State = RaceState.Lobby;
            _lobbyFrame = Time.frameCount;
            Seeded = false;
            SetPlanning(true);
            ClearRace();
            Game?.EnterReady();
            _nextPos = Now;
            _posFast = false;
        }

        /// <summary>Back to normal play (no "race:" for 10 s, or the component is switched off).</summary>
        void Leave(bool resetGame)
        {
            State = RaceState.Off;
            if (track != null) { track.seed = _savedSeed; track.startDistrictOverride = _savedStart; }
            Seeded = false;
            SetPlanning(false);
            ClearRace();
            ClearRivals();
            if (_arch != null) { Destroy(_arch); _arch = null; }
            MyName = "";
            Group = -1;
            if (resetGame) Game?.EnterReady();
            if (hud != null) hud.UpdateRaceHud(this);
        }

        void SetPlanning(bool on)
        {
            if (track != null) track.fixedPlanning = on;
            if (_turns == null) _turns = GetComponent<FakeTurnDirector>();
            if (_turns != null) _turns.fixedPlanning = on;
        }

        void ClearRace()
        {
            _penaltyUntil = 0d;
            _finishedAt = 0d;
            RaceTime = FinishTime = 0f;
            Crashes = 0;
            if (rider != null) rider.PassThroughUntil = -1f;
            // A crash stop cut short (go: during a practice ride's stop): the thrown rider goes, the rider is back on.
            _actions = _actions != null ? _actions : GetComponent<RiderActions>();
            _actions?.ClearCrashShow();
        }

        void ClearRivals()
        {
            foreach (Rival r in _rivals) DropGhost(r);
            _rivals.Clear();
        }

        void StartCountdown(int seed, string start, int metres)
        {
            Seed = seed;
            StartId = start;
            Metres = metres;
            Seeded = true;
            _goAt = Now;
            if (track != null)
            {
                track.seed = seed;
                // The race's start district (when this route has it; otherwise the route's own default).
                track.startDistrictOverride = track.Route == null || DistrictRoute.IndexOf(track.Route, start) >= 0 ? start : _savedStart;
            }
            foreach (Rival r in _rivals)
            {
                r.Finished = r.Suspicious = r.InRace = false;
                r.FinishTime = 0f;
                r.Distance = r.ShownDistance = r.ErrorDistance = r.Speed = 0f;
                r.HasPosition = true;
                r.AcceptedAt = _goAt;
            }
            ClearRace();
            State = RaceState.Countdown;
            _raceStartAt = Now + CountdownSeconds;
            Game?.EnterReady(); // the race's road, the rider on the start line
            _nextPos = Now;
            PlaceArch();
        }

        void OnRunStarted()
        {
            if (State == RaceState.Off) return;
            _penaltyUntil = 0d;
            if (rider != null) rider.PassThroughUntil = -1f;
        }

        void Update()
        {
            if (State == RaceState.Off) return;
            double now = Now;
            if (now - _lastRaceAt > ModeTimeoutSeconds) { Leave(true); return; } // board unplugged or a normal program
            if (_game == null) _game = GetComponent<GameController>();

            switch (State)
            {
                case RaceState.Countdown:
                    if (now >= _raceStartAt)
                    {
                        State = RaceState.Racing;
                        _game?.StartRun();
                        _nextPos = now;
                    }
                    break;
                case RaceState.Racing:
                    RaceTime = (float)(now - _raceStartAt);
                    UpdatePenalty(now);
                    if (rider != null && rider.Distance >= Metres) CrossLine(now);
                    break;
                case RaceState.Finished:
                    UpdatePenalty(now);
                    Coast();
                    if (now - _finishedAt >= ResultsAfterSeconds || (now - _finishedAt >= ResultsMinSeconds && EveryoneFinished())) State = RaceState.Results;
                    break;
                case RaceState.Results:
                    UpdatePenalty(now);
                    Coast();
                    if (RiderInputSource.Read().Jump) BackToLobby();
                    break;
                case RaceState.Lobby:
                    UpdatePenalty(now); // practice rides: a crash is a stop too
                    break;
            }
            UpdateRivals(now);
            SendPosition(now);
        }

        void LateUpdate()
        {
            if (State == RaceState.Off) return;
            UpdateGhosts();
            UpdateArch();
            if (hud != null) hud.UpdateRaceHud(this);
        }

        /// <summary>Results (or any race state) back to the lobby: what A does on the results screen. The next go: starts
        /// a new race.</summary>
        public void BackToLobby()
        {
            if (State == RaceState.Off) return;
            State = RaceState.Lobby;
            _lobbyFrame = Time.frameCount; // the same press does not start a practice ride
            ClearRace();
            Game?.EnterReady();
            _nextPos = Now;
        }

        void CrossLine(double now)
        {
            float over = rider.Distance - Metres;
            FinishTime = Mathf.Max(0.01f, RaceTime - over / Mathf.Max(1f, rider.Speed));
            RaceTime = FinishTime;
            State = RaceState.Finished;
            _finishedAt = now;
            _nextFin = now;
            // v0.5: the police stop at the line (before the brake below: ending a chase lets go of the brake).
            PoliceChase police = PoliceChase.Instance;
            if (police != null && (police.Wanted > 0 || police.Chasing)) police.RaceOver();
            // Past the line: nothing can be hit any more, and the scooter rolls to a stop.
            rider.PassThroughUntil = Time.time + 1e6f;
            rider.BrakeFloor = 0f;
            rider.HoldBrake = true;
            _actions = _actions != null ? _actions : GetComponent<RiderActions>();
            _actions?.Cue("UI_Pickup_01");
        }

        void Coast()
        {
            if (rider != null && rider.Running && rider.HoldBrake && rider.Speed <= 0.3f) rider.Stop();
        }

        bool EveryoneFinished()
        {
            double now = Now;
            foreach (Rival r in _rivals)
                if (r.InRace && !r.Finished && now - r.HeardAt <= GhostGoneSeconds) return false;
            return true;
        }

        // ---------------------------------------------------------------- crashes

        /// <summary>
        /// GameController asks first whenever a ride would end. In race mode a crash is a 3 s stop ("+3 s" on the HUD),
        /// then the ride goes on from the same spot: true = handled, the ride is not over.
        /// </summary>
        public bool TakeCrash(string title, bool crash)
        {
            if (State == RaceState.Off || rider == null) return false;
            rider.Stop();
            GameController g = Game;
            if (crash && g != null && g.crashEffect != null) Destroy(Instantiate(g.crashEffect, rider.transform.position, Quaternion.identity), 4f);
            if (crash) GameEvents.RaiseCrashed();
            Stopped(title, CrashPenaltySeconds);
            if (State == RaceState.Racing) Crashes++;
            return true;
        }

        /// <summary>
        /// v0.5 item race: an item (the rocket) knocks the rider off for <paramref name="seconds"/>, then the ride goes on
        /// from the same spot. Not a crash: no "crash" to the boards (RaceItems sends "hit:rocket") and it does not count
        /// in <see cref="Crashes"/>. False when there is nobody riding to stop.
        /// </summary>
        public bool TakeItemStop(string title, float seconds)
        {
            if (State == RaceState.Off || rider == null || !rider.Running) return false;
            rider.Stop();
            Stopped(title, seconds);
            return true;
        }

        void Stopped(string title, float seconds)
        {
            _respawnX = rider.transform.position.x;
            PenaltySeconds = seconds;
            _penaltyUntil = Now + seconds;
            if (hud != null) hud.RaceCrash(title, seconds);
        }

        void UpdatePenalty(double now)
        {
            if (_penaltyUntil <= 0d || now < _penaltyUntil || rider == null) return;
            _penaltyUntil = 0d;
            _actions = _actions != null ? _actions : GetComponent<RiderActions>();
            _actions?.ClearCrashShow();
            rider.Respawn(_respawnX, RespawnPassSeconds);
            if (State == RaceState.Finished || State == RaceState.Results)
            {
                // Crashed after the line (a gap): back on the scooter, standing still.
                rider.PassThroughUntil = Time.time + 1e6f;
                rider.BrakeFloor = 0f;
                rider.HoldBrake = true;
            }
        }

        // ---------------------------------------------------------------- pos: and fin:

        void SendPosition(double now)
        {
            DeviceHub hub = DeviceHub.Instance;
            if (hub == null) return;
            // fin: instead of p: for 10 s after the finish line
            if (_finishedAt > 0d && now - _finishedAt <= FinishRepeatFor + 0.001)
            {
                if (now >= _nextFin)
                {
                    hub.Send(Messages.Finish(FinishTime));
                    _nextFin = Next(_nextFin, FinishRepeatSeconds, now);
                }
                _nextPos = now + 1d / LobbyPositionsPerSecond;
                return;
            }
            bool fast = State == RaceState.Countdown || State == RaceState.Racing;
            if (fast != _posFast) { _posFast = fast; _nextPos = now; }
            if (now < _nextPos) return;
            bool racing = State == RaceState.Racing && rider != null;
            string line = racing
                ? Messages.Position(Mathf.FloorToInt(Mathf.Max(0f, rider.Distance)), Mathf.RoundToInt(rider.transform.position.x))
                : Messages.Position(0, 0);
            hub.Send(line);
            _nextPos = Next(_nextPos, 1d / (fast ? RacePositionsPerSecond : LobbyPositionsPerSecond), now);
        }

        static double Next(double due, double interval, double now) => due + interval > now ? due + interval : now + interval;

        // ---------------------------------------------------------------- rivals and ghosts

        void UpdateRivals(double now)
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = _rivals.Count - 1; i >= 0; i--)
            {
                Rival r = _rivals[i];
                while (r.Packets.Count > 0 && now - r.Packets.Peek() > LobbyHeardSeconds) r.Packets.Dequeue();
                if (now - r.HeardAt > GhostGoneSeconds)
                {
                    DropGhost(r);
                    // Finished riders stay in the results; anyone else silent for 10 s is gone.
                    if (!(r.Finished && r.InRace)) { _rivals.RemoveAt(i); continue; }
                }
                if (r.Finished && Seeded && InRaceStates)
                {
                    // Parked past the line where riders roll to a stop, in finishing order (seen from behind the line).
                    int order = 0;
                    foreach (Rival o in _rivals) if (o != r && o.Finished && o.FinishTime < r.FinishTime) order++;
                    r.ShownDistance = Metres + 34f + 3f * (order / 3);
                    r.ShownLane = order % 3 == 0 ? -2.4f : order % 3 == 1 ? 2.4f : 0f;
                    r.Speed = 0f;
                    continue;
                }
                // Moved on at its speed since the last position, but never guessed further than ExtrapolateSeconds (then
                // it waits there, and turns grey when nothing comes for 2 s).
                float ahead = Mathf.Min((float)(now - r.AcceptedAt), ExtrapolateSeconds);
                r.ErrorDistance *= Mathf.Exp(-dt / 0.3f);
                r.ShownDistance = r.Distance + r.Speed * ahead + r.ErrorDistance;
                r.ShownLane = Mathf.Lerp(r.ShownLane, r.Lane, 1f - Mathf.Exp(-6f * dt));
            }
        }

        bool GhostsVisible => State == RaceState.Countdown || State == RaceState.Racing || State == RaceState.Finished || State == RaceState.Results;

        readonly List<Rival> _scratch = new List<Rival>();

        void UpdateGhosts()
        {
            if (rider == null || track == null) return;
            double now = Now;
            float myZ = rider.transform.position.z, myX = rider.transform.position.x;
            _scratch.Clear();
            foreach (Rival r in _rivals)
                if (GhostsVisible && r.HasPosition && now - r.HeardAt <= GhostGoneSeconds && (r.InRace || State == RaceState.Countdown)) _scratch.Add(r);
            // At most 8 drawn: the nearest to me.
            float startZ = StartZ;
            _scratch.Sort((a, b) => Mathf.Abs(startZ + a.ShownDistance - myZ).CompareTo(Mathf.Abs(startZ + b.ShownDistance - myZ)));
            for (int i = 0; i < _rivals.Count; i++)
            {
                Rival r = _rivals[i];
                int rank = _scratch.IndexOf(r);
                if (rank < 0 || rank >= MaxGhosts) { if (r.Ghost != null) r.Ghost.gameObject.SetActive(false); continue; }
                if (r.Ghost == null) r.Ghost = MakeGhost(r);
                if (r.Ghost == null) continue;
                float x = r.ShownLane, z = startZ + r.ShownDistance;
                if (State == RaceState.Countdown)
                {
                    // On the start grid: beside me and in rows just ahead (never between me and the camera), so nobody
                    // stands inside anybody else.
                    int k = rank, pair = k / 2;
                    float side = k % 2 == 0 ? -1f : 1f;
                    x = side * (pair % 2 == 0 ? 1.8f : 3.3f);
                    z = startZ + 0.6f * (pair % 2) + 3.2f * (pair / 2);
                }
                float limit = track.RideableHalfWidthAt(z) - 0.3f;
                x = Mathf.Clamp(x, -Mathf.Max(0.3f, limit), Mathf.Max(0.3f, limit));
                // Right on top of me, or between me and the camera: only the name tag shows (it would hide my rider).
                bool overlapsMe = z - myZ < 1.6f && z - myZ > -9f && Mathf.Abs(x - myX) < 1.5f && State != RaceState.Countdown;
                bool behind = z < myZ - 25f;
                r.Ghost.gameObject.SetActive(true);
                float road = track.SurfaceHeightAt(x, z);
                float lift = Ramp.HighestAt(x, z, road, out _, out _);
                float lean = Mathf.Clamp((r.Lane - r.ShownLane) * -6f, -14f, 14f);
                r.Ghost.Place(new Vector3(x, road + Mathf.Max(0f, lift), z), track.SurfacePitchAt(z), lean, !(overlapsMe || behind));
                r.Ghost.SetLook(Palette[r.Colour % Palette.Length], r.SignalLost(now));
            }
        }

        RaceGhost MakeGhost(Rival r)
        {
            if (_ghostRoot == null)
            {
                _ghostRoot = new GameObject("RaceGhosts").transform;
                _ghostRoot.SetParent(transform, false);
            }
            if (_modelTemplate == null && rider != null) _modelTemplate = rider.model;
            return RaceGhost.Create(_ghostRoot, _modelTemplate, r.Name, Palette[r.Colour % Palette.Length]);
        }

        static void DropGhost(Rival r)
        {
            if (r.Ghost != null) Destroy(r.Ghost.gameObject);
            r.Ghost = null;
        }

        int PickColour(string name)
        {
            int h = 7;
            foreach (char c in name) h = unchecked(h * 31 + c);
            int start = (h & 0x7fffffff) % Palette.Length;
            for (int i = 0; i < Palette.Length; i++)
            {
                int c = (start + i) % Palette.Length;
                bool used = false;
                foreach (Rival r in _rivals) if (r.Colour == c) { used = true; break; }
                if (!used) return c;
            }
            return start;
        }

        void PlaceArch()
        {
            if (_arch == null)
            {
                Material palette = RaceFinishArch.FindPalette(rider != null ? rider.model : null);
                _arch = RaceFinishArch.Build(transform, palette);
            }
            float z = StartZ + Metres;
            _arch.transform.position = new Vector3(0f, 0f, z);
            RaceFinishArch.Fit(_arch, track, z);
        }

        void UpdateArch()
        {
            if (_arch == null) return;
            bool show = Seeded && State != RaceState.Lobby;
            if (_arch.activeSelf != show) _arch.SetActive(show);
            if (!show || rider == null) return;
            float z = StartZ + Metres;
            if (Mathf.Abs(z - rider.transform.position.z) < track.buildAhead) RaceFinishArch.Fit(_arch, track, z); // the road there is built now
        }

        // ---------------------------------------------------------------- what the HUD shows

        /// <summary>Riders heard in the last 3 s (the lobby list), nearest name first.</summary>
        public List<Rival> HeardRecently()
        {
            double now = Now;
            var list = new List<Rival>();
            foreach (Rival r in _rivals) if (now - r.HeardAt <= LobbyHeardSeconds) list.Add(r);
            list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return list;
        }

        /// <summary>1..3 signal bars from messages per second (1 a second is expected in the lobby, 4 in a race).</summary>
        public static int SignalBars(float perSecond, float expected)
        {
            float share = perSecond / Mathf.Max(0.1f, expected);
            return share >= 0.75f ? 3 : share >= 0.4f ? 2 : 1;
        }

        /// <summary>Everyone in this race, best first: finished riders by time, then the others by distance; riders marked
        /// "?" (impossible positions or times) after everyone else.</summary>
        public List<Standing> Standings()
        {
            double now = Now;
            var list = new List<Standing>();
            float myDistance = rider != null ? Mathf.Clamp(rider.Distance, 0f, Metres) : 0f;
            list.Add(new Standing
            {
                Name = string.IsNullOrEmpty(MyName) ? "YOU" : MyName, Me = true, Finished = FinishTime > 0f, Time = FinishTime,
                Distance = FinishTime > 0f ? Metres : myDistance, Colour = -1,
            });
            foreach (Rival r in _rivals)
            {
                if (!r.InRace) continue;
                list.Add(new Standing
                {
                    Name = r.Name, Finished = r.Finished, Time = r.FinishTime, Suspicious = r.Suspicious, Colour = r.Colour,
                    Distance = r.Finished ? Metres : Mathf.Clamp(r.ShownDistance, 0f, Metres), Lost = r.SignalLost(now),
                });
            }
            list.Sort((a, b) =>
            {
                if (a.Suspicious != b.Suspicious) return a.Suspicious ? 1 : -1; // "?" riders never win: they go last
                if (a.Finished != b.Finished) return a.Finished ? -1 : 1;
                if (a.Finished) return a.Time.CompareTo(b.Time);
                return b.Distance.CompareTo(a.Distance);
            });
            return list;
        }

        /// <summary>My place (1 = first) and how many are racing.</summary>
        public int MyPlace(out int count)
        {
            List<Standing> s = Standings();
            count = s.Count;
            for (int i = 0; i < s.Count; i++) if (s[i].Me) return i + 1;
            return 1;
        }

        public static string Ordinal(int n)
        {
            int t = n % 100;
            if (t >= 11 && t <= 13) return n + "th";
            switch (n % 10) { case 1: return n + "st"; case 2: return n + "nd"; case 3: return n + "rd"; default: return n + "th"; }
        }

        /// <summary>The name of a district id on this route ("UWA"), else the id in capitals.</summary>
        public string StartName()
        {
            if (track != null && track.Route != null)
            {
                int i = DistrictRoute.IndexOf(track.Route, StartId);
                if (i >= 0) return track.Route[i].displayName;
            }
            return (StartId ?? "").ToUpperInvariant();
        }

        /// <summary>Where a rival is drawn (world position of its ghost); false when it is not drawn.</summary>
        public static bool GhostPosition(Rival r, out Vector3 world)
        {
            world = Vector3.zero;
            if (r.Ghost == null || !r.Ghost.gameObject.activeSelf) return false;
            world = r.Ghost.transform.position;
            return true;
        }
    }
}
