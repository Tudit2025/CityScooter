using System.Globalization;

namespace CityScooter.Protocol
{
    /// <summary>What a radio payload is (the part after "rx:").</summary>
    public enum RadioKind
    {
        /// <summary>Not a race message (or a broken one): the game ignores it, the web monitor still shows it.</summary>
        None = 0,
        /// <summary>"p:AMY:1234:-2": AMY is 1234 m down the road, in the lane at x = -2 m.</summary>
        Position,
        /// <summary>"f:AMY:72.35": AMY crossed the finish line after 72.35 s.</summary>
        Finish,
        /// <summary>"go:4821:uwa:2000": a race starts now, seed 4821, from UWA, 2000 m long.</summary>
        Go,
    }

    /// <summary>
    /// v0.4 radio race (11/9, docs/radio-race.md): one radio message a board heard from another board and passed on to
    /// the game as "rx:&lt;payload&gt;". Every message carries a whole state, never a change, so a lost one is fixed by
    /// the next. Keys are case-insensitive and spaces around ':' are allowed, like the USB protocol.
    ///   p:NAME:DIST:LANE    NAME letters and digits (upper case, at most 5), DIST metres (0 or more), LANE metres
    ///   f:NAME:SECONDS      finish time in seconds (more than 0)
    ///   go:SEED:START:METRES  SEED 1..99999, START a district id (Messages.ZoneIds; unknown = uwa), METRES 300..5000
    /// Out-of-range seeds and lengths are clamped. Anything else is <see cref="RadioKind.None"/> (never throws).
    /// </summary>
    public readonly struct RadioMessage
    {
        public const int MinSeed = 1, MaxSeed = 99999;
        public const int MinMetres = 300, MaxMetres = 5000;
        public const string DefaultStart = "uwa";
        /// <summary>Lanes further out than this are clamped (no road is that wide).</summary>
        public const float MaxLane = 20f;

        public readonly RadioKind Kind;
        /// <summary>Position, Finish: the rider's name (upper case letters and digits, 1..5).</summary>
        public readonly string Name;
        /// <summary>Position: metres from the start (0 or more).</summary>
        public readonly float Distance;
        /// <summary>Position: x in metres (-20..20).</summary>
        public readonly float Lane;
        /// <summary>Finish: seconds from the start to the finish line.</summary>
        public readonly float Seconds;
        /// <summary>Go: 1..99999.</summary>
        public readonly int Seed;
        /// <summary>Go: a district id from Messages.ZoneIds.</summary>
        public readonly string Start;
        /// <summary>Go: race length in metres (300..5000).</summary>
        public readonly int Metres;

        RadioMessage(RadioKind kind, string name = null, float distance = 0f, float lane = 0f, float seconds = 0f,
            int seed = 0, string start = null, int metres = 0)
        {
            Kind = kind; Name = name; Distance = distance; Lane = lane; Seconds = seconds; Seed = seed; Start = start; Metres = metres;
        }

        public bool IsRace => Kind != RadioKind.None;

        public static RadioMessage Position(string name, float distance, float lane) => new RadioMessage(RadioKind.Position, name, distance, lane);
        public static RadioMessage Finish(string name, float seconds) => new RadioMessage(RadioKind.Finish, name, seconds: seconds);
        public static RadioMessage Go(int seed, string start, int metres) => new RadioMessage(RadioKind.Go, seed: seed, start: start, metres: metres);

        static readonly RadioMessage NoneMessage = new RadioMessage(RadioKind.None);

        public static RadioMessage Parse(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return NoneMessage;
            string[] parts = payload.Split(':');
            for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
            switch (parts[0].ToLowerInvariant())
            {
                case "p":
                {
                    if (parts.Length != 4) return NoneMessage;
                    string name = Messages.RaceName(parts[1]);
                    if (name.Length == 0 || !Number(parts[2], out float distance) || !Number(parts[3], out float lane)) return NoneMessage;
                    if (distance < 0f) distance = 0f;
                    if (lane > MaxLane) lane = MaxLane;
                    if (lane < -MaxLane) lane = -MaxLane;
                    return Position(name, distance, lane);
                }
                case "f":
                {
                    if (parts.Length != 3) return NoneMessage;
                    string name = Messages.RaceName(parts[1]);
                    if (name.Length == 0 || !Number(parts[2], out float seconds) || seconds <= 0f || seconds > 100000f) return NoneMessage;
                    return Finish(name, seconds);
                }
                case "go":
                {
                    if (parts.Length != 4) return NoneMessage;
                    if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long seed)) return NoneMessage;
                    if (!Number(parts[3], out float metres)) return NoneMessage;
                    string start = Messages.DistrictId(parts[2]);
                    if (System.Array.IndexOf(Messages.ZoneIds, start) < 0) start = DefaultStart;
                    int m = metres >= MaxMetres ? MaxMetres : metres <= MinMetres ? MinMetres : (int)System.Math.Round(metres);
                    int s = seed >= MaxSeed ? MaxSeed : seed <= MinSeed ? MinSeed : (int)seed;
                    return Go(s, start, m);
                }
                default:
                    return NoneMessage;
            }
        }

        static bool Number(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);

        public override string ToString()
        {
            switch (Kind)
            {
                case RadioKind.Position: return "p:" + Name + ":" + Distance.ToString(CultureInfo.InvariantCulture) + ":" + Lane.ToString(CultureInfo.InvariantCulture);
                case RadioKind.Finish: return "f:" + Name + ":" + Seconds.ToString("0.00", CultureInfo.InvariantCulture);
                case RadioKind.Go: return "go:" + Seed + ":" + Start + ":" + Metres;
                default: return "None";
            }
        }
    }
}
