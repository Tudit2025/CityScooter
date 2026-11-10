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
        /// <summary>v0.5 item race: "i:AMY:rocket:BEN:3", "i:AMY:police:BEN:4", "i:AMY:bolt:5", "i:AMY:oil:1234:-2:6":
        /// AMY used an item (docs/item-race.md).</summary>
        Item,
    }

    /// <summary>
    /// v0.4 radio race (11/9, docs/radio-race.md): one radio message a board heard from another board and passed on to
    /// the game as "rx:&lt;payload&gt;". Every message carries a whole state, never a change, so a lost one is fixed by
    /// the next. Keys are case-insensitive and spaces around ':' are allowed, like the USB protocol.
    ///   p:NAME:DIST:LANE    NAME letters and digits (upper case, at most 5), DIST metres (0 or more), LANE metres
    ///   f:NAME:SECONDS      finish time in seconds (more than 0)
    ///   go:SEED:START:METRES  SEED 1..99999, START a district id (Messages.ZoneIds; unknown = uwa), METRES 300..5000
    /// Out-of-range seeds and lengths are clamped. Anything else is <see cref="RadioKind.None"/> (never throws).
    /// v0.5 item race (11/10, docs/item-race.md): an item used on the others. The last part is the sender's item number
    /// (one digit, 0..9, going round); each message is sent 3 times and receivers drop the copies.
    ///   i:NAME:rocket:TARGET:N    NAME's rocket hits TARGET          i:NAME:police:TARGET:N   the police go after TARGET
    ///   i:NAME:bolt:N             everyone but NAME slows down      i:NAME:oil:DIST:LANE:N   an oil slick at DIST m, lane LANE m
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
        /// <summary>Item: which item (Messages.ItemRocket, ItemPolice, ItemBolt or ItemOil). Name is who used it; Distance
        /// and Lane are where the oil is.</summary>
        public readonly string Item;
        /// <summary>Item (rocket, police): who it is for (upper case letters and digits, 1..5).</summary>
        public readonly string Target;
        /// <summary>Item: the sender's item number, 0..9.</summary>
        public readonly int Sequence;

        RadioMessage(RadioKind kind, string name = null, float distance = 0f, float lane = 0f, float seconds = 0f,
            int seed = 0, string start = null, int metres = 0, string item = null, string target = null, int sequence = 0)
        {
            Kind = kind; Name = name; Distance = distance; Lane = lane; Seconds = seconds; Seed = seed; Start = start; Metres = metres;
            Item = item; Target = target; Sequence = sequence;
        }

        public bool IsRace => Kind != RadioKind.None;

        public static RadioMessage Position(string name, float distance, float lane) => new RadioMessage(RadioKind.Position, name, distance, lane);
        public static RadioMessage Finish(string name, float seconds) => new RadioMessage(RadioKind.Finish, name, seconds: seconds);
        public static RadioMessage Go(int seed, string start, int metres) => new RadioMessage(RadioKind.Go, seed: seed, start: start, metres: metres);

        // ---- Item race (v0.5): what the game asks its board to send ("tx:" + one of these)
        /// <summary>Highest item number; after it the count starts at 0 again.</summary>
        public const int MaxSequence = 9;
        /// <summary>Oil further down the road than this is clamped (the longest race is 5000 m).</summary>
        public const int MaxOilMetres = 99999;

        /// <summary>"i:AMY:rocket:BEN:3" (also for police): <paramref name="item"/> from <paramref name="name"/> on <paramref name="target"/>.</summary>
        public static string ItemOn(string name, string item, string target, int sequence) =>
            "i:" + Messages.RaceName(name) + ":" + item + ":" + Messages.RaceName(target) + ":" + Digit(sequence);
        /// <summary>"i:AMY:bolt:5": everyone but AMY slows down.</summary>
        public static string ItemBolt(string name, int sequence) => "i:" + Messages.RaceName(name) + ":" + Messages.ItemBolt + ":" + Digit(sequence);
        /// <summary>"i:AMY:oil:1234:-2:6": AMY left oil 1234 m down the road in the lane at x = -2 m (whole numbers).</summary>
        public static string ItemOil(string name, int metres, int lane, int sequence) =>
            "i:" + Messages.RaceName(name) + ":" + Messages.ItemOil + ":" + (metres < 0 ? 0 : metres > MaxOilMetres ? MaxOilMetres : metres)
            + ":" + (lane < -(int)MaxLane ? -(int)MaxLane : lane > (int)MaxLane ? (int)MaxLane : lane) + ":" + Digit(sequence);

        static int Digit(int sequence) => ((sequence % (MaxSequence + 1)) + MaxSequence + 1) % (MaxSequence + 1);

        static readonly RadioMessage NoneMessage = new RadioMessage(RadioKind.None);

        // "i:NAME:rocket:TARGET:N", "i:NAME:police:TARGET:N", "i:NAME:bolt:N", "i:NAME:oil:DIST:LANE:N"
        static RadioMessage ParseItem(string[] parts)
        {
            if (parts.Length < 4) return NoneMessage;
            string name = Messages.RaceName(parts[1]);
            string last = parts[parts.Length - 1];
            if (name.Length == 0 || last.Length != 1 || last[0] < '0' || last[0] > '9') return NoneMessage;
            int sequence = last[0] - '0';
            string item = parts[2].ToLowerInvariant();
            switch (item)
            {
                case Messages.ItemRocket:
                case Messages.ItemPolice:
                {
                    if (parts.Length != 5) return NoneMessage;
                    string target = Messages.RaceName(parts[3]);
                    if (target.Length == 0) return NoneMessage;
                    return new RadioMessage(RadioKind.Item, name, item: item, target: target, sequence: sequence);
                }
                case Messages.ItemBolt:
                    return parts.Length == 4 ? new RadioMessage(RadioKind.Item, name, item: item, sequence: sequence) : NoneMessage;
                case Messages.ItemOil:
                {
                    if (parts.Length != 6 || !Number(parts[3], out float distance) || !Number(parts[4], out float lane)) return NoneMessage;
                    if (distance < 0f) distance = 0f;
                    if (distance > MaxOilMetres) distance = MaxOilMetres;
                    if (lane > MaxLane) lane = MaxLane;
                    if (lane < -MaxLane) lane = -MaxLane;
                    return new RadioMessage(RadioKind.Item, name, distance, lane, item: item, sequence: sequence);
                }
                default:
                    return NoneMessage; // boost and shield are never sent; anything else is not an item
            }
        }

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
                case "i": // ---- Item race (v0.5)
                    return ParseItem(parts);
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
                case RadioKind.Item:
                    return Item == Messages.ItemBolt ? ItemBolt(Name, Sequence)
                        : Item == Messages.ItemOil ? ItemOil(Name, (int)System.Math.Round(Distance), (int)System.Math.Round(Lane), Sequence)
                        : ItemOn(Name, Item, Target, Sequence);
                default: return "None";
            }
        }
    }
}
