namespace CityScooter.Protocol
{
    /// <summary>
    /// Wire format v1 (see docs/protocol.md). One ASCII message per line, '\n' terminated.
    /// micro:bit → game: "st:-35" steer (-100..100), "jp" jump, "br" brake (repeat while held), "hi:v2" hello.
    /// game → micro:bit: "go" new run, "crash", "gift", "spd:0".."spd:9".
    /// v1 adds micro:bit → game "pu" punch, "bl" bell; game → micro:bit "ko", "warn:car|ped|hole", "turn:-1|0|1".
    /// v1 WA police (11/8) adds micro:bit → game "sur" surrender; game → micro:bit "wanted:N", "chase:1|0", "cop:N",
    /// "heli:1|0", "inc:L|C|R", "arrested", "busted", "lost".
    /// v0 boards keep working: v1 only adds messages (student code skips lines it does not know).
    /// Any other line (student prints, Python tracebacks) is shown in the serial monitor, never an error.
    /// </summary>
    public static class Messages
    {
        public const string ProtocolVersion = "1";

        // micro:bit → game
        public const string Steer = "st";
        public const string Jump = "jp";
        public const string Brake = "br";
        public const string Hello = "hi";
        public const string Echo = "echo";
        public const string Punch = "pu";
        public const string Bell = "bl";
        public const string Toon = "toon";
        public const string Boost = "bo";
        /// <summary>"say:word": the game repeats the word to every connected board (students' own events).</summary>
        public const string Say = "say";
        public const int MaxSayLength = 16;

        /// <summary>"do:event": a program triggers one of the game's own events.</summary>
        public const string Do = "do";
        public const string DoChase = "chase", DoStars = "stars", DoHeli = "heli", DoCalm = "calm", DoTurn = "turn",
            DoCar = "car", DoSlowCar = "slowcar", DoRoadworks = "roadworks", DoPolice = "police", DoCrossing = "crossing", DoManhole = "manhole";

        /// <summary>A word the game itself sends (crash, gift, ...): a board may not make the game say these.</summary>
        public static bool IsGameMessage(string word)
        {
            string w = (word ?? "").Trim().ToLowerInvariant();
            switch (w)
            {
                case Go: case Crash: case Gift: case Ko: case Boing: case Tired:
                case "arrested": case "busted": case "lost": case "echo":
                // board commands too: a relayed "sur" or "jp" must not look like a command to an echoing program
                case Jump: case Brake: case Punch: case Bell: case Boost: case Toon: case Say: case Do: case "sur":
                case Trick: // ---- Ramps and air (v0.3)
                    return true;
            }
            return w.IndexOf(':') >= 0;
        }

        /// <summary>Letters, digits, _ - ! ? only, 1..16 characters, and not a game message.</summary>
        public static bool IsSayWord(string word)
        {
            if (string.IsNullOrEmpty(word) || word.Length > MaxSayLength || IsGameMessage(word)) return false;
            foreach (char c in word)
                if (!(char.IsLetterOrDigit(c) && c < 128) && c != '_' && c != '-' && c != '!' && c != '?') return false;
            return true;
        }

        // game → micro:bit
        public const string Go = "go";
        public const string Crash = "crash";
        public const string Gift = "gift";
        public const string Ko = "ko";
        public const string Boing = "boing";
        public const string Tired = "tired";
        public const string WarnPrefix = "warn:";
        public const string WarnCar = "warn:car";
        public const string WarnPerson = "warn:ped";
        public const string WarnHole = "warn:hole";
        public const string TurnPrefix = "turn:";
        public static string Turn(int direction) => TurnPrefix + (direction < 0 ? -1 : direction > 0 ? 1 : 0);
        // ---- Districts (v0.3): "zone:uwa" at the start of a ride and when the rider enters the next district
        public const string ZonePrefix = "zone:";
        /// <summary>District ids on the UWA -> Perth CBD route, in route order (blocks and docs list the same).</summary>
        public static readonly string[] ZoneIds = { "uwa", "matilda", "kingspark", "westperth", "cbd", "quay", "mountsbay" };
        /// <summary>"zone:&lt;id&gt;": lower case letters only, at most 16 (anything else is dropped from the id).</summary>
        public static string Zone(string districtId)
        {
            var sb = new System.Text.StringBuilder(ZonePrefix);
            foreach (char c in (districtId ?? "").ToLowerInvariant())
                if (c >= 'a' && c <= 'z' && sb.Length < ZonePrefix.Length + 16) sb.Append(c);
            return sb.ToString();
        }
        // ---- end Districts (v0.3)
        public const string SpeedPrefix = "spd:";
        public const int MaxSpeedLevel = 9;

        public static string Speed(int level)
        {
            if (level < 0) level = 0;
            if (level > MaxSpeedLevel) level = MaxSpeedLevel;
            return SpeedPrefix + level;
        }

        // ---- v1 WA police (11/8): wanted level, chase, helicopter with nets and a sniper
        // micro:bit → game
        /// <summary>Surrender: pull over now (only while the police chase you).</summary>
        public const string Surrender = "sur";
        // game → micro:bit
        public const string WantedPrefix = "wanted:";
        public const int MaxWanted = 5;
        public static string Wanted(int stars) => WantedPrefix + Clamp(stars, 0, MaxWanted);
        public const string ChaseOn = "chase:1";
        public const string ChaseOff = "chase:0";
        public static string Chase(bool on) => on ? ChaseOn : ChaseOff;
        /// <summary>"cop:0" (right behind you) .. "cop:9" (far away, or no police car right now), about 10 m a step.</summary>
        public const string CopPrefix = "cop:";
        public const int MaxCopLevel = 9;
        public static string Cop(int level) => CopPrefix + Clamp(level, 0, MaxCopLevel);
        public const string HeliOn = "heli:1";
        public const string HeliOff = "heli:0";
        public static string Heli(bool on) => on ? HeliOn : HeliOff;
        /// <summary>A net's target ring or the sniper's red dot: on your left, on you, on your right.</summary>
        public const string IncomingPrefix = "inc:";
        public const string IncomingLeft = "inc:L";
        public const string IncomingCentre = "inc:C";
        public const string IncomingRight = "inc:R";
        public static string Incoming(int side) => side < 0 ? IncomingLeft : side > 0 ? IncomingRight : IncomingCentre;
        public const string Arrested = "arrested";
        public const string Busted = "busted";
        public const string Lost = "lost";

        // ---- Ramps and air (v0.3): ramps you fly off, tricks in the air
        // micro:bit -> game
        /// <summary>Do a trick (spin, then flip, ...) while flying off a ramp; on the ground it does nothing.</summary>
        public const string Trick = "trick";
        /// <summary>"do:ramp": a program puts a ramp on the road ahead.</summary>
        public const string DoRamp = "ramp";
        // game -> micro:bit
        /// <summary>Took off from a ramp (or any flight that will last airMessageMinSeconds or more).</summary>
        public const string AirOn = "air:1";
        /// <summary>Back on the ground (always after air:1; also when the ride ends in the air).</summary>
        public const string AirOff = "air:0";
        public static string Air(bool on) => on ? AirOn : AirOff;
        /// <summary>Landed safely, right after air:0 (land:trick when at least one trick was finished in that flight).</summary>
        public const string LandOk = "land:ok";
        public const string LandTrick = "land:trick";

        static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
