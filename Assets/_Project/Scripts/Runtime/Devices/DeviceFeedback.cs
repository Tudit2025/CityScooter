using CityScooter.Game;
using CityScooter.Protocol;
using UnityEngine;

namespace CityScooter.Devices
{
    /// <summary>
    /// Forwards game events to every micro:bit and the simulator: go / crash / gift right away,
    /// speed only when it changes and at most a few times per second (V1 boards have tiny UART buffers).
    /// </summary>
    [RequireComponent(typeof(DeviceHub))]
    public sealed class DeviceFeedback : MonoBehaviour
    {
        [Tooltip("Upper limit for spd: messages per second.")]
        public float maxSpeedMessagesPerSecond = 5f;

        DeviceHub _hub;
        int _wantedSpeed = -1;
        int _sentSpeed = -1;
        float _nextSpeedTime;

        void Awake() => _hub = GetComponent<DeviceHub>();

        void OnEnable()
        {
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.Crashed += OnCrashed;
            GameEvents.GiftCollected += OnGift;
            GameEvents.SpeedLevelChanged += OnSpeedLevel;
            GameEvents.PunchLanded += OnPunchLanded;
            GameEvents.PersonBounced += OnBoing;
            GameEvents.Warning += OnWarning;
            GameEvents.TurnChanged += OnTurn;
        }

        void OnDisable()
        {
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.Crashed -= OnCrashed;
            GameEvents.GiftCollected -= OnGift;
            GameEvents.SpeedLevelChanged -= OnSpeedLevel;
            GameEvents.PunchLanded -= OnPunchLanded;
            GameEvents.PersonBounced -= OnBoing;
            GameEvents.Warning -= OnWarning;
            GameEvents.TurnChanged -= OnTurn;
        }

        void OnRunStarted()
        {
            _sentSpeed = -1;
            _hub.Send(Messages.Go);
        }

        void OnCrashed() => _hub.Send(Messages.Crash);

        void OnGift() => _hub.Send(Messages.Gift);

        void OnSpeedLevel(int level) => _wantedSpeed = level;

        void OnPunchLanded() => _hub.Send(Messages.Ko);

        void OnBoing() => _hub.Send(Messages.Boing);

        // V1 boards have tiny UART buffers: at most a few warnings per second (HazardWarnings already sends each thing once).
        float _nextWarnTime;
        void OnWarning(string message)
        {
            if (Time.unscaledTime < _nextWarnTime) return;
            _nextWarnTime = Time.unscaledTime + 0.25f;
            _hub.Send(message);
        }

        void OnTurn(int direction) => _hub.Send(Messages.Turn(direction));

        void Update()
        {
            if (_wantedSpeed < 0 || _wantedSpeed == _sentSpeed || Time.unscaledTime < _nextSpeedTime) return;
            _hub.Send(Messages.Speed(_wantedSpeed));
            _sentSpeed = _wantedSpeed;
            _nextSpeedTime = Time.unscaledTime + 1f / Mathf.Max(0.5f, maxSpeedMessagesPerSecond);
        }
    }
}
