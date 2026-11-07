using System.Globalization;

namespace CityScooter.Protocol
{
    /// <summary>
    /// Parses one line of student output. Never throws: anything that is not a command
    /// comes back as Unknown so the game can show it instead of failing.
    /// Keys are case-insensitive and spaces around ':' are allowed ("ST : 20" works),
    /// because Year 9 code will be imperfect and the monitor is where we teach precision.
    /// </summary>
    public static class CommandParser
    {
        public const float SteerLimit = 100f;

        public static Command Parse(string line)
        {
            if (line == null) return new Command(CommandKind.Unknown, 0f, string.Empty);
            string s = line.Trim();
            if (s.Length == 0) return new Command(CommandKind.Unknown, 0f, s);

            string lower = s.ToLowerInvariant();
            if (lower == Messages.Jump) return new Command(CommandKind.Jump);
            if (lower == Messages.Brake) return new Command(CommandKind.Brake);
            if (lower == Messages.Punch) return new Command(CommandKind.Punch);
            if (lower == Messages.Bell) return new Command(CommandKind.Bell);
            if (lower == Messages.Toon) return new Command(CommandKind.Toon, 1f);

            int colon = lower.IndexOf(':');
            if (colon <= 0) return new Command(CommandKind.Unknown, 0f, s);

            string key = lower.Substring(0, colon).Trim();
            string arg = s.Substring(colon + 1).Trim();
            switch (key)
            {
                case Messages.Steer:
                    if (float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
                        && !float.IsNaN(v) && !float.IsInfinity(v))
                    {
                        if (v > SteerLimit) v = SteerLimit;
                        if (v < -SteerLimit) v = -SteerLimit;
                        return new Command(CommandKind.Steer, v);
                    }
                    return new Command(CommandKind.Malformed, 0f, s);
                case Messages.Hello:
                    return new Command(CommandKind.Hello, 0f, arg.ToLowerInvariant());
                case Messages.Echo:
                    return new Command(CommandKind.Echo, 0f, arg);
                case Messages.Jump:
                case Messages.Brake:
                    // "jp:1" style: be forgiving, the intent is clear.
                    return new Command(key == Messages.Jump ? CommandKind.Jump : CommandKind.Brake);
                case Messages.Punch:
                    return new Command(CommandKind.Punch);
                case Messages.Bell:
                    return new Command(CommandKind.Bell);
                case Messages.Toon:
                    switch (arg.ToLowerInvariant())
                    {
                        case "1": case "on": case "true": return new Command(CommandKind.Toon, 1f);
                        case "0": case "off": case "false": return new Command(CommandKind.Toon, 0f);
                        default: return new Command(CommandKind.Malformed, 0f, s);
                    }
                default:
                    return new Command(CommandKind.Unknown, 0f, s);
            }
        }
    }
}
