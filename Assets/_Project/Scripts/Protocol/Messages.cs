namespace CityScooter.Protocol
{
    /// <summary>
    /// Wire format v1 (see docs/protocol.md). One ASCII message per line, '\n' terminated.
    /// micro:bit → game: "st:-35" steer (-100..100), "jp" jump, "br" brake (repeat while held), "hi:v2" hello.
    /// game → micro:bit: "go" new run, "crash", "gift", "spd:0".."spd:9".
    /// v1 adds micro:bit → game "pu" punch, "bl" bell; game → micro:bit "ko", "warn:car|ped|hole", "turn:-1|0|1".
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

        // game → micro:bit
        public const string Go = "go";
        public const string Crash = "crash";
        public const string Gift = "gift";
        public const string Ko = "ko";
        public const string Boing = "boing";
        public const string WarnPrefix = "warn:";
        public const string WarnCar = "warn:car";
        public const string WarnPerson = "warn:ped";
        public const string WarnHole = "warn:hole";
        public const string TurnPrefix = "turn:";
        public static string Turn(int direction) => TurnPrefix + (direction < 0 ? -1 : direction > 0 ? 1 : 0);
        public const string SpeedPrefix = "spd:";
        public const int MaxSpeedLevel = 9;

        public static string Speed(int level)
        {
            if (level < 0) level = 0;
            if (level > MaxSpeedLevel) level = MaxSpeedLevel;
            return SpeedPrefix + level;
        }
    }
}
