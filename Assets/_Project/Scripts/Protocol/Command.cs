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
