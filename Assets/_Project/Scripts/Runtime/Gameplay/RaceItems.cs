using System.Collections.Generic;
using CityScooter.Controls;
using CityScooter.Devices;
using CityScooter.Game;
using CityScooter.Protocol;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// v0.5 item race (11/10, docs/item-race.md): Mario-Kart-style items in the radio race.
    /// Rows of three question-mark boxes stand on the race road, the first 150 m after the start and then every 250 m,
    /// one box in each of three lanes. Where they are depends on the distance alone, so every game of a race has them
    /// in the same places; they are laid over the road and use none of its random numbers, so the road itself stays as it
    /// was. Riding through a box with empty hands gives one item ("item:rocket" to the board); which one depends on the
    /// rider's place right now (local randomness). Items never use themselves: the program says "use" (keyboard: E).
    ///   boost   3 s of strong boost                      shield  8 s: stops the next item or the next crash, once
    ///   oil     a slick 6 m behind, in my lane           rocket  the nearest rider ahead: 1 s warning, then a 1.5 s stop
    ///   bolt    everyone else at half speed for 3 s      police  the leader is wanted (3 stars) for 15 s
    /// Attacks go out over the boards' radio: the game asks its board with "tx:i:AMY:rocket:BEN:3", three times, 0.15 s
    /// apart, and the board sends the part after "tx:". The other boards pass it on as "rx:i:..."; each game works out
    /// what it means for its own rider and drops copies it has seen (same sender and item number within 20 s). A board
    /// never hears its own radio, so what my own item does on my screen happens here straight away.
    /// The board hears "hit:rocket" / "hit:bolt" / "hit:police" / "hit:oil" when an item gets its rider,
    /// "blocked:rocket" (or "blocked:crash") when the shield stopped it, and "item:none" when the hands are empty again.
    /// Works in the race and on practice rides in the lobby; nothing here runs outside race mode.
    /// GameController adds it at runtime (no scene changes); the pictures are built by <see cref="RaceItemArt"/>.
    /// </summary>
    [DefaultExecutionOrder(61)] // after RaceDirector decided the state this frame
    public sealed class RaceItems : MonoBehaviour
    {
        // ---- The numbers (docs/item-race.md).
        public const float FirstBoxMetres = 150f, BoxSpacingMetres = 250f;
        /// <summary>Lane x of the three boxes of a row (metres from the middle of the road).</summary>
        public static readonly float[] BoxLanes = { -2.6f, 0f, 2.6f };
        /// <summary>Height of a box's middle above the road (or the ramp) under it.</summary>
        public const float BoxHeight = 1.25f;
        /// <summary>A box is taken when the rider passes its row this close to it sideways. Height does not matter: a
        /// rider jumping or flying off a ramp over the row takes the box under them too (rows can be where ramps are).</summary>
        public const float BoxGrabHalfWidth = 1.1f;
        /// <summary>No rows this close to the finish line (or after it).</summary>
        public const float BoxesEndBeforeFinish = 40f;
        public const float BoostSeconds = 3f, BoostFactor = 1.6f;
        public const float ShieldSeconds = 8f;
        public const float OilBehindMetres = 6f, OilSlowFactor = 0.4f, OilSlowSeconds = 1.5f, OilLifeSeconds = 60f, OilRadius = 1.3f;
        /// <summary>Oil heard over the radio is put on the road when it is ahead of my rider, or at most this far behind.</summary>
        public const float OilKeepBehindMetres = 5f;
        public const int MaxSlicks = 24;
        public const float RocketWarnSeconds = 1f, RocketStopSeconds = 1.5f;
        public const float BoltSlowFactor = 0.5f, BoltSlowSeconds = 3f;
        public const int PoliceStars = 3;
        public const float PoliceSeconds = 15f;
        /// <summary>Every item message goes to the board this many times, this far apart (radio packets get lost).</summary>
        public const int SendRepeats = 3;
        public const float SendSpacing = 0.15f;
        /// <summary>The same sender and item number again within this many seconds is a copy.</summary>
        public const float DedupSeconds = 20f;

        static readonly Color RocketRed = new Color(1f, 0.3f, 0.22f);
        static readonly Color BoltYellow = new Color(1f, 0.9f, 0.25f);
        static readonly Color ShieldCyan = new Color(0.45f, 0.9f, 1f);
        static readonly Color PoliceBlue = new Color(0.5f, 0.7f, 1f);
        static readonly Color OilGrey = new Color(0.82f, 0.8f, 0.9f);
        static readonly Color BoostOrange = new Color(1f, 0.62f, 0.15f);
        static readonly Color Plain = new Color(1f, 1f, 1f);

        public static RaceItems Instance { get; private set; }

        public RaceDirector race;
        public RiderController rider;
        public TrackGenerator track;
        public HudView hud;
        /// <summary>Off: no item boxes on the road (tests that hand items out themselves; items still work).</summary>
        [System.NonSerialized] public bool boxesOn = true;

        // ---------------------------------------------------------------- which item

        static readonly (string item, int percent)[] FirstTable = { (Messages.ItemOil, 50), (Messages.ItemShield, 30), (Messages.ItemBoost, 20) };
        static readonly (string item, int percent)[] MiddleTable = { (Messages.ItemRocket, 35), (Messages.ItemBoost, 25), (Messages.ItemOil, 20), (Messages.ItemShield, 20) };
        static readonly (string item, int percent)[] LastTable = { (Messages.ItemRocket, 30), (Messages.ItemPolice, 25), (Messages.ItemBoost, 25), (Messages.ItemBolt, 20) };

        static (string item, int percent)[] TableFor(int place, int count) =>
            count <= 1 ? MiddleTable : place <= 1 ? FirstTable : place >= count ? LastTable : MiddleTable;

        /// <summary>
        /// The item a box gives the rider in this place (1 = first) of <paramref name="count"/> riders: the leader gets
        /// oil, shields and boosts; the last rider gets rockets, the police, boosts and bolts; everyone between (and a
        /// rider alone, e.g. practising) gets rockets, boosts, oil and shields. With two riders the second is "last".
        /// </summary>
        public static string PickItem(int place, int count, System.Random rng)
        {
            (string item, int percent)[] table = TableFor(place, count);
            int roll = rng.Next(100);
            foreach ((string item, int percent) in table)
            {
                if (roll < percent) return item;
                roll -= percent;
            }
            return table[table.Length - 1].item;
        }

        /// <summary>Chance in percent that a box gives <paramref name="item"/> to the rider in this place (tests, docs).</summary>
        public static int Chance(int place, int count, string item)
        {
            foreach ((string name, int percent) in TableFor(place, count)) if (name == item) return percent;
            return 0;
        }

        // ---------------------------------------------------------------- state

        /// <summary>The item in the rider's hands (one of Messages.ItemNames), or null.</summary>
        public string Held { get; private set; }
        /// <summary>Seconds of shield left (0 = no shield).</summary>
        public float ShieldLeft { get; private set; }
        public bool ShieldOn => ShieldLeft > 0f;
        /// <summary>The boost item is pushing right now, and for how long.</summary>
        public bool Boosting => rider != null && rider.ItemBoosting;
        public float BoostLeft => rider != null && rider.ItemBoosting ? rider.ItemBoostLeft : 0f;
        /// <summary>Seconds of slow-down left, and what caused it ("oil" or "bolt"; null when not slowed).</summary>
        public float SlowLeft => rider != null ? rider.ItemSlowLeft : 0f;
        public string SlowedBy => SlowLeft > 0f ? _slowBy : null;
        /// <summary>A rocket is on its way to my rider: seconds until it lands (0 = none), and who fired it.</summary>
        public float RocketIn => _rockets.Count > 0 ? Mathf.Max(0.001f, _rockets[0].HitAt - Time.time) : 0f;
        public string RocketFrom => _rockets.Count > 0 ? _rockets[0].From : null;
        /// <summary>Seconds left of the police another rider sent (0 = none).</summary>
        public float PoliceLeft => _policeUntil > 0f ? Mathf.Max(0f, _policeUntil - Time.time) : 0f;

        /// <summary>One oil slick on my road.</summary>
        public sealed class Slick
        {
            public string From;
            /// <summary>Metres from the start line, and lane x.</summary>
            public float Distance, Lane;
            /// <summary>I dropped it myself.</summary>
            public bool Mine;
            internal double Expires;
            internal GameObject View;
        }

        public IReadOnlyList<Slick> Slicks => _slicks;

        // Counters (tests, and a line for the teacher's debugging).
        public int Picked { get; private set; }
        public int Used { get; private set; }
        /// <summary>"use" with empty hands.</summary>
        public int IgnoredUses { get; private set; }
        /// <summary>Item messages from the radio that were new; copies that were dropped.</summary>
        public int Heard { get; private set; }
        public int Duplicates { get; private set; }
        public int HitsTaken { get; private set; }
        public int Blocks { get; private set; }
        /// <summary>The last item used, and who it went to (null: nobody, or it has no target).</summary>
        public string LastUsed { get; private set; }
        public string LastTarget { get; private set; }
        /// <summary>The item number the next message from this game will carry (0..9, going round).</summary>
        public int NextSequence => _sequence;

        struct Rocket { public string From; public float HitAt; }
        struct Flying { public GameObject View; public float Until, TargetX, Speed; }

        readonly List<Rocket> _rockets = new List<Rocket>();
        readonly List<Slick> _slicks = new List<Slick>();
        readonly HashSet<int> _taken = new HashSet<int>();
        readonly Dictionary<string, double> _seen = new Dictionary<string, double>();
        readonly List<(double at, string line)> _outbox = new List<(double, string)>();
        readonly List<string> _forget = new List<string>();
        System.Random _random = new System.Random();
        RaceState _state = RaceState.Off;
        int _sequence;
        float _lastDistance, _policeUntil, _nextWobble, _wobbleSide = 1f;
        int _policeRestore;
        string _slowBy;
        double _nextForget;
        RiderActions _actions;
        RiderController _guarded;

        static double Now => Time.unscaledTimeAsDouble;

        GameController Game => GameController.Instance;
        RiderActions Actions { get { if (_actions == null) _actions = GetComponent<RiderActions>(); return _actions; } }
        TrackTheme Theme => track != null ? track.theme : null;

        /// <summary>My rider is riding right now, in a practice ride or in the race (not stopped, not over the line).</summary>
        public bool Riding
        {
            get
            {
                GameController g = Game;
                return race != null && rider != null && g != null && g.State == RunState.Running && rider.Running
                    && (race.State == RaceState.Lobby || race.State == RaceState.Racing);
            }
        }

        /// <summary>The same items in the same order (tests).</summary>
        public void SeedRandom(int seed) => _random = new System.Random(seed);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake() => Instance = this;

        void OnEnable()
        {
            Instance = this;
            GameEvents.RadioHeard += OnRadioHeard;
            GameEvents.RunStarted += OnRunStarted;
            RiderInputSource.ItemInHand = HoldingForKey;
        }

        void OnDisable()
        {
            GameEvents.RadioHeard -= OnRadioHeard;
            GameEvents.RunStarted -= OnRunStarted;
            if (RiderInputSource.ItemInHand == HoldingForKey) RiderInputSource.ItemInHand = null;
            if (_guarded != null && _guarded.CrashGuard == GuardCrash) _guarded.CrashGuard = null;
            _guarded = null;
            ClearRide();
            UpdateViews(false); // nothing of the items stays on the road
            _viewsOn = false;
            _state = RaceState.Off;
            if (hud != null) hud.UpdateItemHud(null);
            if (Instance == this) Instance = null;
        }

        void OnDestroy()
        {
            _art?.Dispose();
            _art = null;
        }

        bool HoldingForKey() => Held != null && race != null && race.ItemsOn;

        // ---------------------------------------------------------------- frame

        void Update()
        {
            RaceState state = race != null ? race.State : RaceState.Off;
            if (state != _state)
            {
                // Lobby, countdown, race, finish: each is a new start for the items (the race start itself also comes as
                // RunStarted). A held item is dropped: the board hears item:none.
                bool keep = _state == RaceState.Countdown && state == RaceState.Racing;
                _state = state;
                if (!keep) ClearRide();
            }
            double now = Now;
            FlushOutbox(now);
            if (state == RaceState.Off) return;
            if (now >= _nextForget) ForgetOldMessages(now);
            if (rider != null && _guarded != rider)
            {
                if (_guarded != null && _guarded.CrashGuard == GuardCrash) _guarded.CrashGuard = null;
                _guarded = rider;
                rider.CrashGuard = GuardCrash;
            }
            RiderInput input = RiderInputSource.Read();
            if (!race.ItemsOn || rider == null) return;
            if (input.Use) Use();
            float dt = Time.deltaTime;
            if (ShieldLeft > 0f)
            {
                ShieldLeft = Mathf.Max(0f, ShieldLeft - dt);
                if (ShieldLeft <= 0f) Flash("SHIELD GONE", null, 1f, ShieldCyan);
            }
            UpdateRockets();
            UpdatePolice();
            UpdateOil(now);
            UpdateBoxes();
            UpdateWobble();
            _lastDistance = race.MyDistance;
        }

        void LateUpdate()
        {
            bool on = race != null && race.State != RaceState.Off;
            if (!on && !_viewsOn) return;
            _viewsOn = on;
            UpdateViews(on && race.ItemsOn && rider != null && track != null);
            if (hud != null) hud.UpdateItemHud(on ? this : null);
        }

        void OnRunStarted()
        {
            if (race == null || race.State == RaceState.Off) return;
            ClearRide();
        }

        /// <summary>Empty hands, no shield, no rockets on the way, no oil, all boxes back: a new ride (or race mode is over).</summary>
        void ClearRide()
        {
            if (Held != null)
            {
                Held = null;
                Send(Messages.ItemNone);
            }
            ShieldLeft = 0f;
            _rockets.Clear();
            _policeUntil = 0f;
            _policeRestore = 0;
            _slowBy = null;
            _taken.Clear();
            _lastDistance = 0f;
            foreach (Slick s in _slicks) Release(s);
            _slicks.Clear();
            foreach (Flying f in _flying) if (f.View != null) f.View.SetActive(false);
            _flying.Clear();
            if (rider != null) rider.ClearItemEffects();
        }

        // ---------------------------------------------------------------- boxes

        /// <summary>Metres from the start line to row <paramref name="row"/> (0 = the first row).</summary>
        public static float RowDistance(int row) => FirstBoxMetres + row * BoxSpacingMetres;

        /// <summary>This row is on the road of the current ride: every row on a practice ride, the ones well before the
        /// finish line in a race.</summary>
        public bool RowExists(int row)
        {
            if (row < 0 || race == null || !race.ItemsOn || !boxesOn) return false;
            bool raceRoad = race.Seeded && race.State != RaceState.Lobby;
            return !raceRoad || RowDistance(row) <= race.Metres - BoxesEndBeforeFinish;
        }

        /// <summary>I already took a box of this row (its other boxes are gone too).</summary>
        public bool RowTaken(int row) => _taken.Contains(row);

        /// <summary>Where the box of this row and lane (0..2) is in the world (its middle).</summary>
        public Vector3 BoxPosition(int row, int lane)
        {
            float x = BoxLanes[Mathf.Clamp(lane, 0, BoxLanes.Length - 1)], z = (race != null ? race.StartZ : 0f) + RowDistance(row);
            float road = track != null ? track.SurfaceHeightAt(x, z) : 0f;
            float lift = Ramp.HighestAt(x, z, road, out _, out _);
            return new Vector3(x, road + Mathf.Max(0f, lift) + BoxHeight, z);
        }

        void UpdateBoxes()
        {
            float d = race.MyDistance;
            if (!Riding || d <= _lastDistance) return;
            // Every row passed this frame (one, unless the frame was very long).
            int from = Mathf.Max(0, Mathf.CeilToInt((_lastDistance - FirstBoxMetres) / BoxSpacingMetres));
            if (RowDistance(from) <= _lastDistance) from++;
            for (int row = from; RowDistance(row) <= d; row++)
            {
                if (Held != null || !RowExists(row) || _taken.Contains(row)) continue;
                Vector3 p = rider.transform.position;
                for (int lane = 0; lane < BoxLanes.Length; lane++)
                {
                    if (Mathf.Abs(p.x - BoxLanes[lane]) > BoxGrabHalfWidth) continue;
                    Take(row, new Vector3(p.x, p.y + 3.1f, p.z));
                    break;
                }
            }
        }

        void Take(int row, Vector3 at)
        {
            _taken.Add(row);
            int place = race.MyPlace(out int count);
            if (race.State != RaceState.Racing) { place = 1; count = 1; } // a practice ride: nobody to race against
            Held = PickItem(place, count, _random);
            Picked++;
            Send(Messages.Item(Held));
            Popup(RaceItemArt.Burst.Item, at, rider.transform);
            Actions?.Cue("UI_Pickup_01");
        }

        /// <summary>Put an item in the rider's hands (tests, scripted moments): what riding through a box does. False when
        /// the hands are full, the name is not an item, or items are off.</summary>
        public bool Give(string item)
        {
            if (race == null || !race.ItemsOn || Held != null || !Messages.IsItem(item)) return false;
            Held = item;
            Picked++;
            Send(Messages.Item(Held));
            return true;
        }

        // ---------------------------------------------------------------- using an item

        /// <summary>"use" (or E): use the item in hand. False with empty hands.</summary>
        public bool Use()
        {
            if (race == null || !race.ItemsOn || rider == null || Held == null)
            {
                IgnoredUses++;
                return false;
            }
            string item = Held;
            Held = null;
            Used++;
            LastUsed = item;
            LastTarget = null;
            Send(Messages.ItemNone);
            Vector3 p = rider.transform.position;
            switch (item)
            {
                case Messages.ItemBoost:
                    rider.ItemBoost(BoostSeconds, BoostFactor);
                    Actions?.Popup(Theme != null ? Theme.boostPopup : null, p + new Vector3(-1.3f, 2.3f, 0f), rider.transform);
                    Actions?.Cue("SCO_Jump");
                    RiderActions.ShakeCamera(0.2f);
                    Flash("BOOST!", null, 1f, BoostOrange);
                    break;
                case Messages.ItemShield:
                    ShieldLeft = ShieldSeconds;
                    Actions?.Cue("UI_Pickup_01");
                    Flash("SHIELD!", "stops the next hit or crash", 1.4f, ShieldCyan);
                    break;
                case Messages.ItemOil:
                {
                    int metres = Mathf.Max(0, Mathf.FloorToInt(race.MyDistance - OilBehindMetres)), lane = Mathf.RoundToInt(p.x);
                    AddSlick(race.MyName, metres, lane, true);
                    Transmit(RadioMessage.ItemOil(race.MyName, metres, lane, TakeSequence()));
                    Actions?.Cue("SCO_Land_01");
                    Flash("OIL!", "a slick behind you", 1.2f, OilGrey);
                    break;
                }
                case Messages.ItemRocket:
                {
                    RaceDirector.Rival target = RivalAhead();
                    Launch(target);
                    Actions?.Cue("SCO_Jump");
                    if (target == null) { Flash("ROCKET", "no one ahead", 1.4f, RocketRed); break; }
                    LastTarget = target.Name;
                    Transmit(RadioMessage.ItemOn(race.MyName, Messages.ItemRocket, target.Name, TakeSequence()));
                    Flash("ROCKET!", "→ " + target.Name, 1.4f, RocketRed);
                    break;
                }
                case Messages.ItemBolt:
                    Transmit(RadioMessage.ItemBolt(race.MyName, TakeSequence()));
                    if (hud != null) hud.ItemScreenFlash(BoltYellow, 0.45f, 0.3f);
                    Actions?.Cue("SCO_Land_02");
                    Flash("BOLT!", "everyone else slows down", 1.4f, BoltYellow);
                    break;
                case Messages.ItemPolice:
                {
                    string target = Leader();
                    Actions?.Cue("VEH_Horn_02");
                    if (target == null) { Flash("POLICE", "no one ahead", 1.4f, PoliceBlue); break; }
                    LastTarget = target;
                    Transmit(RadioMessage.ItemOn(race.MyName, Messages.ItemPolice, target, TakeSequence()));
                    Flash("POLICE!", "→ " + target, 1.4f, PoliceBlue);
                    break;
                }
            }
            return true;
        }

        /// <summary>The rider just ahead of me in this race (the rocket's target); null when I lead or ride alone.</summary>
        public RaceDirector.Rival RivalAhead()
        {
            if (race == null || race.State != RaceState.Racing) return null;
            float mine = race.MyDistance, bestGap = float.MaxValue;
            RaceDirector.Rival best = null;
            foreach (RaceDirector.Rival r in race.Rivals)
            {
                if (!r.InRace || r.Finished || !r.HasPosition) continue;
                float gap = r.ShownDistance - mine;
                if (gap <= 0f || gap >= bestGap) continue;
                bestGap = gap;
                best = r;
            }
            return best;
        }

        /// <summary>Who the police item goes after: the leader of the race, or the second when I lead; null when alone.</summary>
        public string Leader()
        {
            if (race == null || race.State != RaceState.Racing) return null;
            foreach (RaceDirector.Standing s in race.Standings())
                if (!s.Me && !s.Finished) return s.Name;
            return null;
        }

        int TakeSequence()
        {
            int s = _sequence;
            _sequence = (_sequence + 1) % (RadioMessage.MaxSequence + 1);
            return s;
        }

        // ---------------------------------------------------------------- messages

        static void Send(string line)
        {
            DeviceHub hub = DeviceHub.Instance;
            if (hub != null) hub.Send(line);
        }

        // Ask the board to radio this: now, and again 0.15 and 0.3 s later.
        void Transmit(string payload)
        {
            string line = Messages.Transmit(payload);
            Send(line);
            double now = Now;
            for (int i = 1; i < SendRepeats; i++) _outbox.Add((now + i * SendSpacing, line));
        }

        void FlushOutbox(double now)
        {
            for (int i = 0; i < _outbox.Count; i++)
            {
                if (_outbox[i].at > now) continue;
                Send(_outbox[i].line);
                _outbox.RemoveAt(i--);
            }
        }

        void ForgetOldMessages(double now)
        {
            _nextForget = now + 5d;
            _forget.Clear();
            foreach (KeyValuePair<string, double> s in _seen) if (now - s.Value > DedupSeconds) _forget.Add(s.Key);
            foreach (string key in _forget) _seen.Remove(key);
        }

        void OnRadioHeard(string deviceId, string payload)
        {
            if (race == null || race.State == RaceState.Off) return;
            RadioMessage m = RadioMessage.Parse(payload);
            if (m.Kind != RadioKind.Item) return;
            if (m.Name == race.MyName) return; // my own board never hears itself; someone using my name gets nowhere
            double now = Now;
            string key = m.Name + ":" + m.Sequence;
            if (_seen.TryGetValue(key, out double seen) && now - seen <= DedupSeconds) { Duplicates++; return; }
            _seen[key] = now;
            Heard++;
            bool forMe = m.Target == race.MyName;
            switch (m.Item)
            {
                case Messages.ItemRocket:
                    if (!forMe) { Flash(m.Name + " → " + m.Target + ": ROCKET!", null, 1.6f, Plain); break; }
                    if (!Riding) break; // on the ground already (or not riding): it flies past
                    _rockets.Add(new Rocket { From = m.Name, HitAt = Time.time + RocketWarnSeconds });
                    Actions?.Popup(Theme != null ? Theme.startlePopup : null, rider.transform.position + new Vector3(0f, 2.7f, 0f), rider.transform);
                    Actions?.Cue("VEH_Horn_01");
                    Flash("ROCKET!", "from " + m.Name + "  ·  use your shield!", RocketWarnSeconds, RocketRed);
                    break;
                case Messages.ItemPolice:
                    if (!forMe) { Flash(m.Name + " → " + m.Target + ": POLICE!", null, 1.6f, Plain); break; }
                    if (!Riding || !race.PoliceAllowed) break;
                    if (Shielded(Messages.ItemPolice)) break;
                    TakePolice(m.Name);
                    break;
                case Messages.ItemBolt:
                    if (!Riding) break;
                    if (Shielded(Messages.ItemBolt)) break;
                    TakeBolt(m.Name);
                    break;
                case Messages.ItemOil:
                    if (!race.ItemsOn || m.Distance < race.MyDistance - OilKeepBehindMetres) break; // behind me: it can never matter here
                    AddSlick(m.Name, m.Distance, m.Lane, false);
                    break;
            }
        }

        // ---------------------------------------------------------------- being hit

        // The shield takes it (once): true = nothing else happens.
        bool Shielded(string what)
        {
            if (ShieldLeft <= 0f) return false;
            ShieldLeft = 0f;
            Blocks++;
            Send(Messages.Blocked(what));
            if (rider != null) Popup(RaceItemArt.Burst.Block, rider.transform.position + new Vector3(1.7f, 2.5f, 0.4f), rider.transform);
            Actions?.Cue("SCO_Land_01");
            RiderActions.ShakeCamera(0.2f);
            Flash("BLOCKED!", what == Messages.BlockedCrash ? "your shield took the crash" : "your shield stopped the " + what, 1.6f, ShieldCyan);
            return true;
        }

        // RiderController asks just before a crash into something: the shield takes that too.
        bool GuardCrash(Obstacle obstacle) => race != null && race.ItemsOn && Shielded(Messages.BlockedCrash);

        void Hit(string item)
        {
            HitsTaken++;
            Send(Messages.Hit(item));
        }

        void UpdateRockets()
        {
            while (_rockets.Count > 0 && Time.time >= _rockets[0].HitAt)
            {
                Rocket r = _rockets[0];
                _rockets.RemoveAt(0);
                if (!Riding) continue; // crashed in the meantime: nothing left to knock over
                if (Shielded(Messages.ItemRocket)) continue;
                Vector3 p = rider.transform.position;
                if (!race.TakeItemStop("BOOM!", RocketStopSeconds)) continue;
                Hit(Messages.ItemRocket);
                Actions?.Shot(p);
                Popup(RaceItemArt.Burst.Boom, p + new Vector3(0f, 2.1f, 0.5f), null);
                RiderActions.ShakeCamera(0.8f);
                Flash("BOOM!", "rocket from " + r.From + "  ·  +" + RocketStopSeconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " s", 1.4f, RocketRed);
            }
        }

        void TakeBolt(string from)
        {
            Hit(Messages.ItemBolt);
            rider.ItemSlow(BoltSlowSeconds, BoltSlowFactor);
            _slowBy = Messages.ItemBolt;
            if (hud != null) hud.ItemScreenFlash(BoltYellow, 0.85f, 0.4f);
            Popup(RaceItemArt.Burst.Zap, rider.transform.position + new Vector3(1.7f, 2.5f, 0.4f), rider.transform);
            Actions?.Cue("SCO_Land_02");
            RiderActions.ShakeCamera(0.4f);
            Flash("ZAP!", "bolt from " + from + "  ·  slow for " + BoltSlowSeconds.ToString("0") + " s", 1.8f, BoltYellow);
        }

        void TakePolice(string from)
        {
            PoliceChase police = PoliceChase.Instance;
            if (police == null) return;
            Hit(Messages.ItemPolice);
            _policeRestore = _policeUntil > 0f ? _policeRestore : police.Wanted;
            police.SetWanted(Mathf.Max(police.Wanted, PoliceStars));
            _policeUntil = Time.time + PoliceSeconds;
            // Said again for a moment: the helicopter arriving next frame has a title of its own, but who sent it matters more.
            _policeFrom = from;
            _policeSayUntil = Time.unscaledTime + 0.4f;
            SayPolice();
        }

        string _policeFrom;
        float _policeSayUntil;

        void SayPolice() => Flash("POLICE!", _policeFrom + " sent the police  \u00B7  dodge them for " + PoliceSeconds.ToString("0") + " s", 2.4f, PoliceBlue);

        // The police another rider sent stay 15 s (unless they catch the rider first).
        void UpdatePolice()
        {
            if (_policeUntil <= 0f) return;
            if (Time.unscaledTime < _policeSayUntil && hud != null && hud.FlashText != "POLICE!") SayPolice();
            PoliceChase police = PoliceChase.Instance;
            if (police == null || police.Wanted == 0) { _policeUntil = 0f; return; } // caught, crashed or shaken off already
            if (Time.time < _policeUntil) return;
            _policeUntil = 0f;
            if (police.Wanted <= _policeRestore) return;
            police.SetWanted(_policeRestore);
            if (_policeRestore == 0) Flash("POLICE GONE", null, 1.2f, PoliceBlue);
        }

        // ---------------------------------------------------------------- oil

        void AddSlick(string from, float distance, float lane, bool mine)
        {
            if (_slicks.Count >= MaxSlicks) { Release(_slicks[0]); _slicks.RemoveAt(0); }
            _slicks.Add(new Slick { From = from, Distance = Mathf.Max(0f, distance), Lane = lane, Mine = mine, Expires = Now + OilLifeSeconds });
        }

        void UpdateOil(double now)
        {
            float d = race.MyDistance, x = rider.transform.position.x;
            bool onGround = Riding && !rider.Airborne && !rider.Falling;
            for (int i = _slicks.Count - 1; i >= 0; i--)
            {
                Slick s = _slicks[i];
                if (now >= s.Expires) { Release(s); _slicks.RemoveAt(i); continue; }
                if (s.Mine || !onGround) continue;
                // Rode onto it this frame: it slips once, then it is used up on this screen.
                if (d < s.Distance - OilRadius || _lastDistance > s.Distance + OilRadius || Mathf.Abs(x - s.Lane) > OilRadius) continue;
                Release(s);
                _slicks.RemoveAt(i);
                if (Shielded(Messages.ItemOil)) continue;
                Hit(Messages.ItemOil);
                rider.ItemSlow(OilSlowSeconds, OilSlowFactor);
                _slowBy = Messages.ItemOil;
                _nextWobble = 0f;
                Popup(RaceItemArt.Burst.Slip, rider.transform.position + new Vector3(-1.7f, 2.5f, 0.4f), rider.transform);
                Actions?.Cue("SCO_Scrape_01");
                RiderActions.ShakeCamera(0.3f);
                Flash("SLIP!", s.From + "'s oil  ·  slow for " + OilSlowSeconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " s", 1.5f, OilGrey);
            }
        }

        // Slipping on oil: the rider twists left and right.
        void UpdateWobble()
        {
            if (_slowBy != Messages.ItemOil || rider == null || !rider.ItemSlowed || !rider.Running) return;
            if (Time.time < _nextWobble) return;
            _nextWobble = Time.time + 0.22f;
            _wobbleSide = -_wobbleSide;
            rider.PunchTwist(_wobbleSide);
        }

        // ---------------------------------------------------------------- what is drawn

        RaceItemArt _art;
        Transform _root;
        bool _viewsOn;
        readonly ItemBox[] _boxes = new ItemBox[6];
        readonly Stack<GameObject> _slickPool = new Stack<GameObject>();
        readonly Dictionary<RaceItemArt.Burst, GameObject> _bursts = new Dictionary<RaceItemArt.Burst, GameObject>();
        readonly List<Flying> _flying = new List<Flying>();
        readonly Stack<GameObject> _rocketPool = new Stack<GameObject>();
        GameObject _shield, _flame, _bolt, _incoming, _target;
        Renderer _shieldBubble;
        Transform _flicker;

        /// <summary>Boxes standing on the road right now (tests, screenshots).</summary>
        public IEnumerable<ItemBox> BoxesShown()
        {
            foreach (ItemBox b in _boxes) if (b != null && b.gameObject.activeSelf) yield return b;
        }
        /// <summary>The shield bubble, the boost flame and the bolt over the rider are drawn (tests).</summary>
        public bool ShieldShown => _shield != null && _shield.activeSelf;
        public bool FlameShown => _flame != null && _flame.activeSelf;
        public bool BoltShown => _bolt != null && _bolt.activeSelf;
        /// <summary>Oil slicks drawn on the road right now.</summary>
        public int SlicksShown { get { int n = 0; foreach (Slick s in _slicks) if (s.View != null && s.View.activeSelf) n++; return n; } }

        RaceItemArt Art
        {
            get
            {
                if (_art != null) return _art;
                _root = new GameObject("RaceItems").transform;
                _root.SetParent(transform, false);
                _art = new RaceItemArt(rider != null ? rider.model : null);
                return _art;
            }
        }

        void Popup(RaceItemArt.Burst kind, Vector3 at, Transform follow)
        {
            RiderActions actions = Actions;
            if (actions == null) return;
            if (!_bursts.TryGetValue(kind, out GameObject template) || template == null)
            {
                RaceItemArt art = Art;
                _bursts[kind] = template = art.BurstTemplate(_root, kind);
            }
            actions.Popup(template, at, follow);
        }

        void Flash(string title, string sub, float seconds, Color colour)
        {
            if (hud != null) hud.Flash(title, sub, seconds, colour);
        }

        void Release(Slick s)
        {
            if (s.View == null) return;
            s.View.SetActive(false);
            _slickPool.Push(s.View);
            s.View = null;
        }

        void UpdateViews(bool on)
        {
            if (!on && _art == null) return;
            RaceItemArt art = Art;
            float riderZ = rider != null ? rider.transform.position.z : 0f;
            float startZ = race != null ? race.StartZ : 0f;
            float ahead = track != null ? track.buildAhead - 6f : 200f;

            // Boxes: the row coming up and the one after it, once the road under them is built.
            int first = on ? Mathf.Max(0, Mathf.CeilToInt((race.MyDistance - 8f - FirstBoxMetres) / BoxSpacingMetres)) : 0;
            for (int k = 0; k < 2; k++)
            {
                int row = first + k;
                float z = startZ + RowDistance(row);
                bool show = on && RowExists(row) && !_taken.Contains(row) && z - riderZ < ahead;
                for (int lane = 0; lane < 3; lane++)
                {
                    int slot = (row & 1) * 3 + lane;
                    if (!show) { if (_boxes[slot] != null && _boxes[slot].gameObject.activeSelf) _boxes[slot].gameObject.SetActive(false); continue; }
                    if (_boxes[slot] == null) _boxes[slot] = art.Box(_root, lane);
                    ItemBox box = _boxes[slot];
                    box.Row = row;
                    box.transform.position = BoxPosition(row, lane);
                    if (!box.gameObject.activeSelf) box.gameObject.SetActive(true);
                }
            }

            // Oil: on the road where it was dropped, while that part of the road is there.
            foreach (Slick s in _slicks)
            {
                float z = startZ + s.Distance;
                bool show = on && z - riderZ < ahead && z - riderZ > -40f;
                if (!show) { if (s.View != null && s.View.activeSelf) s.View.SetActive(false); continue; }
                if (s.View == null) s.View = _slickPool.Count > 0 ? _slickPool.Pop() : art.Oil(_root);
                float limit = Mathf.Max(0.5f, track.RideableHalfWidthAt(z) - 0.6f), x = Mathf.Clamp(s.Lane, -limit, limit);
                s.View.transform.SetPositionAndRotation(new Vector3(x, track.SurfaceHeightAt(x, z), z), Quaternion.Euler(track.SurfacePitchAt(z), 0f, 0f));
                if (!s.View.activeSelf) s.View.SetActive(true);
            }

            bool riderShown = on && rider.model != null && rider.model.gameObject.activeInHierarchy;
            Vector3 feet = rider != null ? rider.transform.position : Vector3.zero;

            // Shield: a bubble round the rider; it blinks in its last two seconds.
            bool shield = riderShown && ShieldLeft > 0f && (ShieldLeft > 2f || Mathf.Repeat(Time.time * 6f, 1f) < 0.65f);
            if (shield && _shield == null) _shield = art.Shield(_root, out _shieldBubble);
            if (_shield != null)
            {
                if (_shield.activeSelf != shield) _shield.SetActive(shield);
                if (shield)
                {
                    float pulse = 1.32f * (1f + 0.03f * Mathf.Sin(Time.time * 7f));
                    _shield.transform.SetPositionAndRotation(feet + Vector3.up * 0.95f, Quaternion.identity);
                    _shield.transform.localScale = Vector3.one * pulse;
                }
            }

            // Boost: a flame behind the scooter.
            bool flame = riderShown && rider.Running && rider.ItemBoosting;
            if (flame && _flame == null)
            {
                _flame = art.Flame(_root);
                _flicker = _flame.transform.Find("Flicker");
            }
            if (_flame != null)
            {
                if (_flame.activeSelf != flame) _flame.SetActive(flame);
                if (flame)
                {
                    float pitch = track.SurfacePitchAt(feet.z) - rider.RampPitch;
                    _flame.transform.SetPositionAndRotation(feet + Quaternion.Euler(pitch, 0f, 0f) * new Vector3(0f, 0.42f, -0.7f), Quaternion.Euler(pitch, 0f, 0f));
                    float t = Time.time * 23f;
                    if (_flicker != null) _flicker.localScale = new Vector3(1f + 0.12f * Mathf.Sin(t * 1.7f), 1f + 0.12f * Mathf.Sin(t * 2.3f + 1f), 0.85f + 0.25f * Mathf.Abs(Mathf.Sin(t)));
                }
            }

            // Bolt: a lightning bolt over the slowed rider.
            bool bolt = riderShown && rider.ItemSlowed && _slowBy == Messages.ItemBolt;
            if (bolt && _bolt == null) _bolt = art.BoltMark(_root);
            if (_bolt != null)
            {
                if (_bolt.activeSelf != bolt) _bolt.SetActive(bolt);
                if (bolt)
                {
                    Camera cam = Camera.main;
                    _bolt.transform.SetPositionAndRotation(feet + Vector3.up * (2.75f + 0.08f * Mathf.Sin(Time.time * 14f)), cam != null ? cam.transform.rotation : Quaternion.identity);
                    _bolt.transform.localScale = Vector3.one * (Mathf.Repeat(Time.time * 5f, 1f) < 0.5f ? 1f : 0.86f);
                }
            }

            // A rocket coming for me: a target ring round the rider that closes in, and the rocket diving in from above
            // and behind (straight from behind it would stay below the chase camera's picture until it hit).
            bool incoming = on && _rockets.Count > 0 && rider != null;
            if (incoming && _incoming == null) { _incoming = art.Rocket(_root); _target = art.Target(_root); }
            if (_incoming != null)
            {
                if (_incoming.activeSelf != incoming) { _incoming.SetActive(incoming); _target.SetActive(incoming); }
                if (incoming)
                {
                    float left = Mathf.Clamp01((_rockets[0].HitAt - Time.time) / RocketWarnSeconds);
                    Vector3 aim = feet + Vector3.up * 1.1f;
                    var at = new Vector3(feet.x - 1.2f * left, aim.y + 9f * left, feet.z - 0.3f - 5.4f * left);
                    _incoming.transform.SetPositionAndRotation(at, Quaternion.LookRotation((aim - at).normalized + Vector3.forward * 0.001f));
                    _incoming.transform.localScale = Vector3.one * 1.5f;
                    _target.transform.SetPositionAndRotation(new Vector3(feet.x, track.SurfaceHeightAt(feet.x, feet.z), feet.z), Quaternion.Euler(track.SurfacePitchAt(feet.z), Time.time * 140f, 0f));
                    _target.transform.localScale = Vector3.one * (0.85f + 0.9f * left);
                }
            }

            // Rockets I fired: off down the road, towards the rider they are for.
            for (int i = _flying.Count - 1; i >= 0; i--)
            {
                Flying f = _flying[i];
                if (!on || Time.time >= f.Until || f.View == null)
                {
                    if (f.View != null)
                    {
                        if (on) Actions?.Popup(Theme != null ? Theme.impactPopup : null, f.View.transform.position);
                        f.View.SetActive(false);
                        _rocketPool.Push(f.View);
                    }
                    _flying.RemoveAt(i);
                    continue;
                }
                Vector3 p = f.View.transform.position;
                p.z += f.Speed * Time.deltaTime;
                p.x = Mathf.MoveTowards(p.x, f.TargetX, 6f * Time.deltaTime);
                p.y = track.SurfaceHeightAt(p.x, p.z) + 1.15f;
                f.View.transform.SetPositionAndRotation(p, Quaternion.Euler(track.SurfacePitchAt(p.z), 0f, Time.time * 360f));
            }
        }

        void Launch(RaceDirector.Rival target)
        {
            if (rider == null || track == null) return;
            RaceItemArt art = Art;
            GameObject view = _rocketPool.Count > 0 ? _rocketPool.Pop() : art.Rocket(_root);
            Vector3 p = rider.transform.position;
            view.transform.SetPositionAndRotation(p + new Vector3(0f, 1.15f, 1.6f), Quaternion.identity);
            view.transform.localScale = Vector3.one * 1.8f;
            view.SetActive(true);
            float targetX = p.x;
            if (target != null && RaceDirector.GhostPosition(target, out Vector3 ghost)) targetX = ghost.x;
            else if (target != null) targetX = target.ShownLane;
            _flying.Add(new Flying { View = view, Until = Time.time + 1.2f, TargetX = targetX, Speed = rider.Speed + 34f });
        }
    }
}
