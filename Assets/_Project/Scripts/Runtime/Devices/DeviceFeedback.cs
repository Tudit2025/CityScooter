using CityScooter.Game;
using CityScooter.Protocol;
using UnityEngine;

namespace CityScooter.Devices
{
    /// <summary>
    /// Forwards game events to every micro:bit and the simulator: go / crash / gift right away,
    /// speed only when it changes and at most a few times per second (V1 boards have tiny UART buffers).
    /// WA police: wanted / chase / heli / arrested / busted / lost right away (rare), "cop:N" like speed (latest value,
    /// at most maxCopMessagesPerSecond), "inc:" queued with incomingSpacing between two (5 stars: two rings at once).
    /// </summary>
    [RequireComponent(typeof(DeviceHub))]
    public sealed class DeviceFeedback : MonoBehaviour
    {
        [Tooltip("Upper limit for spd: messages per second.")]
        public float maxSpeedMessagesPerSecond = 5f;
        [Tooltip("Upper limit for cop: messages per second (police car distance while chased).")]
        public float maxCopMessagesPerSecond = 3f;
        [Tooltip("Seconds between two inc: messages (paint pellet rings / red dot).")]
        public float incomingSpacing = 0.15f;

        DeviceHub _hub;
        int _wantedSpeed = -1;
        int _sentSpeed = -1;
        float _nextSpeedTime;
        int _wantedCop = -1;
        int _sentCop = -1;
        float _nextCopTime;
        readonly System.Collections.Generic.Queue<string> _incoming = new System.Collections.Generic.Queue<string>();
        float _nextIncomingTime;

        void Awake() => _hub = GetComponent<DeviceHub>();

        void OnEnable()
        {
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.Crashed += OnCrashed;
            GameEvents.GiftCollected += OnGift;
            GameEvents.SpeedLevelChanged += OnSpeedLevel;
            GameEvents.PunchLanded += OnPunchLanded;
            GameEvents.PersonBounced += OnBoing;
            GameEvents.Tired += OnTired;
            GameEvents.BoardSaid += OnBoardSaid;
            GameEvents.Warning += OnWarning;
            GameEvents.TurnChanged += OnTurn;
            GameEvents.WantedChanged += OnWanted;
            GameEvents.ChaseChanged += OnChase;
            GameEvents.CopDistanceChanged += OnCop;
            GameEvents.HeliChanged += OnHeli;
            GameEvents.Incoming += OnIncoming;
            GameEvents.Arrested += OnArrested;
            GameEvents.Busted += OnBusted;
            GameEvents.LostPolice += OnLost;
        }

        void OnDisable()
        {
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.Crashed -= OnCrashed;
            GameEvents.GiftCollected -= OnGift;
            GameEvents.SpeedLevelChanged -= OnSpeedLevel;
            GameEvents.PunchLanded -= OnPunchLanded;
            GameEvents.PersonBounced -= OnBoing;
            GameEvents.Tired -= OnTired;
            GameEvents.BoardSaid -= OnBoardSaid;
            GameEvents.Warning -= OnWarning;
            GameEvents.TurnChanged -= OnTurn;
            GameEvents.WantedChanged -= OnWanted;
            GameEvents.ChaseChanged -= OnChase;
            GameEvents.CopDistanceChanged -= OnCop;
            GameEvents.HeliChanged -= OnHeli;
            GameEvents.Incoming -= OnIncoming;
            GameEvents.Arrested -= OnArrested;
            GameEvents.Busted -= OnBusted;
            GameEvents.LostPolice -= OnLost;
        }

        void OnRunStarted()
        {
            _sentSpeed = -1;
            _wantedCop = _sentCop = -1;
            _incoming.Clear();
            _hub.Send(Messages.Go);
        }

        void OnCrashed() => _hub.Send(Messages.Crash);

        void OnGift() => _hub.Send(Messages.Gift);

        void OnSpeedLevel(int level) => _wantedSpeed = level;

        void OnPunchLanded() => _hub.Send(Messages.Ko);

        void OnBoing() => _hub.Send(Messages.Boing);

        void OnTired() => _hub.Send(Messages.Tired);

        // "say:word" from one board: the game says the word to every board (at most 2 a second per board).
        readonly System.Collections.Generic.Dictionary<string, float> _nextSay = new System.Collections.Generic.Dictionary<string, float>();
        public int SaysRelayed { get; private set; }
        void OnBoardSaid(string deviceId, string word)
        {
            string key = deviceId ?? "?";
            if (_nextSay.TryGetValue(key, out float next) && Time.unscaledTime < next) return;
            _nextSay[key] = Time.unscaledTime + 0.5f;
            SaysRelayed++;
            _hub.Send(word);
        }

        // V1 boards have tiny UART buffers: at most 4 warnings a second. Warnings that arrive together (truck, worker and
        // manhole entering the look-ahead at once) wait their turn instead of being lost; one of each kind is enough.
        float _nextWarnTime;
        readonly System.Collections.Generic.List<string> _warnQueue = new System.Collections.Generic.List<string>(3);
        void OnWarning(string message)
        {
            if (_warnQueue.Contains(message)) return;
            if (_warnQueue.Count >= 3) _warnQueue.RemoveAt(0);
            _warnQueue.Add(message);
            SendWarnings();
        }

        void SendWarnings()
        {
            if (_warnQueue.Count == 0 || Time.unscaledTime < _nextWarnTime) return;
            _nextWarnTime = Time.unscaledTime + 0.25f;
            _hub.Send(_warnQueue[0]);
            _warnQueue.RemoveAt(0);
        }

        void OnTurn(int direction) => _hub.Send(Messages.Turn(direction));

        // ---- WA police
        void OnWanted(int stars) => _hub.Send(Messages.Wanted(stars));

        void OnChase(bool on)
        {
            // No stale cop: or inc: after a chase is over (they would land after "busted" / "lost").
            _wantedCop = _sentCop = -1;
            if (!on) _incoming.Clear();
            _hub.Send(Messages.Chase(on));
        }

        void OnCop(int level) => _wantedCop = level;

        void OnHeli(bool on)
        {
            if (!on) _incoming.Clear();
            _hub.Send(Messages.Heli(on));
        }

        void OnIncoming(int side)
        {
            if (_incoming.Count < 3) _incoming.Enqueue(Messages.Incoming(side));
            SendIncoming();
        }

        // The ride is over, like any crash: "crash" first (Lesson 1 programs), then the reason.
        void OnArrested()
        {
            _hub.Send(Messages.Crash);
            _hub.Send(Messages.Arrested);
        }

        void OnBusted() => _hub.Send(Messages.Busted);

        void OnLost() => _hub.Send(Messages.Lost);

        void SendIncoming()
        {
            if (_incoming.Count == 0 || Time.unscaledTime < _nextIncomingTime) return;
            _hub.Send(_incoming.Dequeue());
            _nextIncomingTime = Time.unscaledTime + incomingSpacing;
        }

        void Update()
        {
            SendIncoming();
            SendWarnings();
            if (_wantedCop >= 0 && _wantedCop != _sentCop && Time.unscaledTime >= _nextCopTime)
            {
                _hub.Send(Messages.Cop(_wantedCop));
                _sentCop = _wantedCop;
                _nextCopTime = Time.unscaledTime + 1f / Mathf.Max(0.5f, maxCopMessagesPerSecond);
            }
            if (_wantedSpeed < 0 || _wantedSpeed == _sentSpeed || Time.unscaledTime < _nextSpeedTime) return;
            _hub.Send(Messages.Speed(_wantedSpeed));
            _sentSpeed = _wantedSpeed;
            _nextSpeedTime = Time.unscaledTime + 1f / Mathf.Max(0.5f, maxSpeedMessagesPerSecond);
        }
    }
}
