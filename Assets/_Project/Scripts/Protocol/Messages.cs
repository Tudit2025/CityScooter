namespace CityScooter.Protocol
{
    /// <summary>
    /// Wire format v0 (see docs/protocol.md). One ASCII message per line, '\n' terminated.
    /// micro:bit → game: "st:-35" steer (-100..100), "jp" jump, "br" brake (repeat while held), "hi:v2" hello.
    /// game → micro:bit: "go" new run, "crash", "gift", "spd:0".."spd:9".
    /// Any other line (student prints, Python tracebacks) is shown in the serial monitor, never an error.
    /// </summary>
    public static class Messages
    {
        public const string ProtocolVersion = "0";

        // micro:bit → game
        public const string Steer = "st";
        public const string Jump = "jp";
        public const string Brake = "br";
        public const string Hello = "hi";
        public const string Echo = "echo";

        // game → micro:bit
        public const string Go = "go";
        public const string Crash = "crash";
        public const string Gift = "gift";
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
