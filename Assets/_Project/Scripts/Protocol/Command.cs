namespace CityScooter.Protocol
{
    public enum CommandKind
    {
        /// <summary>Not a protocol line: student print output, a traceback, an empty line.</summary>
        Unknown = 0,
        /// <summary>Looks like a protocol command but the value is wrong, e.g. "st:abc". Worth a hint in the monitor.</summary>
        Malformed,
        Steer,
        Jump,
        Brake,
        Hello,
        Echo,
        /// <summary>v1: "pu" punch (one punch per line; the game has a cooldown).</summary>
        Punch,
        /// <summary>v1: "bl" ring the bell (pedestrians ahead step aside).</summary>
        Bell,
        /// <summary>v1: "toon:1" / "toon:0" cartoon mode on/off (Value 1 or 0): bumping into people sends them flying.</summary>
        Toon,
        /// <summary>v1: "bo" boost for a moment (has a cooldown).</summary>
        Boost,
        /// <summary>v1: "say:party" makes the game say "party" to every board (Text holds the word).</summary>
        Say,
        /// <summary>v1: "do:chase", "do:stars:3", "do:turn:L", "do:car" ... trigger one of the game's own events
        /// (Text = event name, Value = stars 0..5 or turn -1/1).</summary>
        Trigger,
        /// <summary>v1 police: "sur" surrender, pull over now (ignored unless the police are chasing).</summary>
        Surrender,
        /// <summary>v0.3 ramps: "trick" do a spin or flip while flying off a ramp.</summary>
        Trick,
        /// <summary>v0.3 districts: "start:kingspark" rides start in this district (Text = district id, lower case letters).</summary>
        Start,
        /// <summary>v0.4 radio race: "race:AMY:12" race as AMY in radio group 12 (Text = name, upper case letters and
        /// digits, 1..5; Value = group 0..255).</summary>
        Race,
        /// <summary>v0.4 radio race: "rx:p:BEN:120:-2" a radio message the board passed on (Text = the payload as it
        /// came; <see cref="RadioMessage.Parse"/> reads it, anything else in it is ignored).</summary>
        Radio,
        /// <summary>11/9: "open:stars:5", "open:chase" ... start every ride with one of the game's own events (the same
        /// events as "do:"; Text = event name, Value = stars 0..5 or turn -1/1). Programs say it again about once a
        /// second, like "start:".</summary>
        Opening,
    }

    /// <summary>One parsed line from a micro:bit (or the simulator, or a dev tool).</summary>
    public readonly struct Command
    {
        public readonly CommandKind Kind;
        /// <summary>Steer: -100..100, already clamped.</summary>
        public readonly float Value;
        /// <summary>Hello: board version ("v1"/"v2"). Echo: echoed text. Unknown/Malformed: the trimmed line.</summary>
        public readonly string Text;

        public Command(CommandKind kind, float value = 0f, string text = null)
        {
            Kind = kind;
            Value = value;
            Text = text;
        }

        public bool IsProtocol => Kind != CommandKind.Unknown && Kind != CommandKind.Malformed;

        public override string ToString()
        {
            switch (Kind)
            {
                case CommandKind.Steer: return "Steer(" + Value + ")";
                case CommandKind.Hello: return "Hello(" + Text + ")";
                case CommandKind.Echo: return "Echo(" + Text + ")";
                case CommandKind.Unknown: return "Unknown(" + Text + ")";
                case CommandKind.Malformed: return "Malformed(" + Text + ")";
                default: return Kind.ToString();
            }
        }
    }
}
