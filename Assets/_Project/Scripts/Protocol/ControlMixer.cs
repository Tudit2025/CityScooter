namespace CityScooter.Protocol
{
    /// <summary>
    /// Merges commands from any number of devices into what the rider should do.
    /// v0 rule (makes 2-board co-op free): any device may steer, jump or brake; the latest steer wins.
    /// Times are seconds on any monotonic clock supplied by the caller (keeps this testable).
    /// </summary>
    public sealed class ControlMixer
    {
        /// <summary>No "st:" for this long → steer falls back to 0 (board unplugged, program stopped, lost packets).</summary>
        public double SteerTimeout = 0.5;

        /// <summary>Each "br" keeps the brake on this long; students send it every loop while B is held.</summary>
        public double BrakeHold = 0.3;

        float _steer;
        double _steerTime = double.NegativeInfinity;
        double _brakeUntil = double.NegativeInfinity;
        int _pendingJumps;
        int _pendingPunches;
        int _pendingBells;

        /// <summary>v1 cartoon mode: the last "toon:" any device sent (off until a program turns it on).</summary>
        public bool Toon { get; private set; }

        public void Apply(in Command command, double now)
        {
            switch (command.Kind)
            {
                case CommandKind.Steer:
                    _steer = command.Value / CommandParser.SteerLimit;
                    _steerTime = now;
                    break;
                case CommandKind.Jump:
                    _pendingJumps++;
                    break;
                case CommandKind.Brake:
                    _brakeUntil = now + BrakeHold;
                    break;
                case CommandKind.Punch:
                    _pendingPunches++;
                    break;
                case CommandKind.Bell:
                    _pendingBells++;
                    break;
                case CommandKind.Toon:
                    Toon = command.Value > 0.5f;
                    break;
            }
        }

        /// <summary>-1 (left) .. 1 (right); 0 when no fresh steer.</summary>
        public float Steer(double now) => HasRecentSteer(now) ? _steer : 0f;

        public bool HasRecentSteer(double now) => now - _steerTime <= SteerTimeout;

        public bool Braking(double now) => now < _brakeUntil;

        /// <summary>How many "jp" arrived since the last ConsumeJump (useful for the polling-vs-event lesson).</summary>
        public int PendingJumps => _pendingJumps;

        /// <summary>True if at least one jump was requested since the last call; clears the request.</summary>
        public bool ConsumeJump()
        {
            if (_pendingJumps <= 0) return false;
            _pendingJumps = 0;
            return true;
        }

        public int PendingPunches => _pendingPunches;
        /// <summary>True if a punch was requested since the last call; clears the request.</summary>
        public bool ConsumePunch()
        {
            if (_pendingPunches <= 0) return false;
            _pendingPunches = 0;
            return true;
        }
        /// <summary>True if the bell was rung since the last call; clears the request.</summary>
        public bool ConsumeBell()
        {
            if (_pendingBells <= 0) return false;
            _pendingBells = 0;
            return true;
        }

        public void Reset()
        {
            Toon = false;
            _pendingPunches = 0;
            _pendingBells = 0;
            _steer = 0f;
            _steerTime = double.NegativeInfinity;
            _brakeUntil = double.NegativeInfinity;
            _pendingJumps = 0;
        }
    }
}
