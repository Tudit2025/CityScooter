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
        // "do:chase", "do:stars:3", "do:turn:L" ...: event name and its number (stars 0..5, turn -1/1).
        static Command ParseDo(string arg, string raw)
        {
            string a = arg.Trim().ToLowerInvariant();
            int colon = a.IndexOf(':');
            string name = (colon < 0 ? a : a.Substring(0, colon)).Trim(), param = colon < 0 ? "" : a.Substring(colon + 1).Trim();
            switch (name)
            {
                case Messages.DoChase: case Messages.DoHeli: case Messages.DoCalm: case Messages.DoCar: case Messages.DoSlowCar:
                case Messages.DoRoadworks: case Messages.DoPolice: case Messages.DoCrossing: case Messages.DoManhole:
                case Messages.DoRamp: // ---- Ramps and air (v0.3)
                    return param.Length == 0 ? new Command(CommandKind.Trigger, 0f, name) : new Command(CommandKind.Malformed, 0f, raw);
                case Messages.DoStars:
                    return int.TryParse(param, NumberStyles.Integer, CultureInfo.InvariantCulture, out int stars) && stars >= 0 && stars <= 5
                        ? new Command(CommandKind.Trigger, stars, name) : new Command(CommandKind.Malformed, 0f, raw);
                case Messages.DoTurn:
                    if (param == "l" || param == "left" || param == "-1") return new Command(CommandKind.Trigger, -1f, name);
                    if (param == "r" || param == "right" || param == "1") return new Command(CommandKind.Trigger, 1f, name);
                    return new Command(CommandKind.Malformed, 0f, raw);
                default:
                    return new Command(CommandKind.Malformed, 0f, raw);
            }
        }

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
            if (lower == Messages.Boost) return new Command(CommandKind.Boost);
            if (lower == Messages.Surrender) return new Command(CommandKind.Surrender);
            if (lower == Messages.Trick) return new Command(CommandKind.Trick); // ---- Ramps and air (v0.3)

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
                case Messages.Boost:
                    return new Command(CommandKind.Boost);
                case Messages.Do:
                    return ParseDo(arg, s);
                case Messages.Say:
                    return Messages.IsSayWord(arg) ? new Command(CommandKind.Say, 0f, arg) : new Command(CommandKind.Malformed, 0f, s);
                case Messages.Surrender:
                    return new Command(CommandKind.Surrender);
                case Messages.Trick: // ---- Ramps and air (v0.3): "trick:1" is forgiven like "jp:1"
                    return new Command(CommandKind.Trick);
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
