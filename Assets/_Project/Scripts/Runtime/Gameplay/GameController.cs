using CityScooter.Controls;
using CityScooter.Game;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    public enum RunState
    {
        Ready,
        Running,
        Crashed,
    }

    /// <summary>
    /// Run flow: Ready (press A / Space) → Running → Crashed (press again after a short delay) → Running.
    /// Owns the score and raises the GameEvents that DeviceFeedback forwards to the micro:bits.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        const string BestKey = "cityscooter.best";

        public static GameController Instance { get; private set; }

        public GameTuning tuning;
        public RiderController rider;
        public TrackGenerator track;
        public HudView hud;
        [Tooltip("Optional effect spawned where the rider crashes.")]
        public GameObject crashEffect;
        [Tooltip("Optional effect for gifts that have no Gift.pickupEffect.")]
        public GameObject giftEffect;
        [Tooltip("Where each run starts.")]
        public Vector3 startPosition = new Vector3(0f, 0f, 2f);

        public RunState State { get; private set; } = RunState.Ready;
        public int Gifts { get; private set; }
        public int Best { get; private set; }
        public int Score => rider == null ? 0 : Mathf.Max(0, Mathf.Max(0, Mathf.FloorToInt(rider.Distance * T.pointsPerMetre)) + _giftPoints);

        /// <summary>Bonus or penalty points (punch hits and misses).</summary>
        public void AddPoints(int points) => _giftPoints += points;

        /// <summary>Use up one gift (cartoon mode). False when there is none.</summary>
        public bool SpendGift()
        {
            if (Gifts <= 0) return false;
            Gifts--;
            return true;
        }

        /// <summary>Tests and scripted moments.</summary>
        public void GrantGifts(int count) => Gifts += Mathf.Max(0, count);
        public float StateTime => _stateTime;

        float _stateTime;
        int _giftPoints;

        GameTuning T => tuning != null ? tuning : GameTuning.Default;

        void Awake()
        {
            Instance = this;
            try { Best = PlayerPrefs.GetInt(BestKey, 0); } catch (System.Exception) { Best = 0; }
            if (track != null && track.rider == null && rider != null) track.rider = rider.transform;
            if (rider != null && rider.track == null) rider.track = track;
            if (rider != null && rider.tuning == null) rider.tuning = tuning;
            if (track != null && track.tuning == null) track.tuning = tuning;
        }

        void OnEnable()
        {
            if (rider == null) return;
            rider.HitObstacle += OnHitObstacle;
            rider.GotGift += OnGotGift;
        }

        void OnDisable()
        {
            if (rider == null) return;
            rider.HitObstacle -= OnHitObstacle;
            rider.GotGift -= OnGotGift;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        FakeTurnDirector _turns;

        void Start()
        {
            // Fake turns need no scene setup: created here so Main.unity stays the tech artist's.
            _turns = GetComponent<FakeTurnDirector>();
            if (_turns == null && rider != null) _turns = gameObject.AddComponent<FakeTurnDirector>();
            if (_turns != null) { if (_turns.rider == null) _turns.rider = rider; if (_turns.tuning == null) _turns.tuning = tuning; }
            // v1 punch / bell / warnings, also without scene changes.
            var actions = GetComponent<RiderActions>();
            if (actions == null && rider != null) actions = gameObject.AddComponent<RiderActions>();
            if (actions != null) { if (actions.rider == null) actions.rider = rider; if (actions.theme == null && track != null) actions.theme = track.theme; }
            EnterReady();
            Camera cam = Camera.main;
            if (cam != null && rider != null)
            {
                FollowCamera follow = cam.GetComponent<FollowCamera>();
                if (follow == null) follow = cam.gameObject.AddComponent<FollowCamera>();
                if (follow.target == null) follow.target = rider.transform;
                follow.SnapToTarget();
            }
        }

        void Update()
        {
            _stateTime += Time.unscaledDeltaTime;
            switch (State)
            {
                case RunState.Ready:
                    if (RiderInputSource.Read().Jump) StartRun();
                    break;
                case RunState.Crashed:
                    bool pressed = RiderInputSource.Read().Jump; // read every frame so stale presses are consumed
                    if (pressed && _stateTime >= T.restartDelay) StartRun();
                    break;
            }
        }

        public void EnterReady()
        {
            track?.ResetTrack(startPosition.z - 2f);
            rider?.ResetRider(startPosition);
            Gifts = 0;
            _giftPoints = 0;
            SetState(RunState.Ready);
            hud?.ShowReady();
        }

        public void StartRun()
        {
            _turns?.Restart(System.Environment.TickCount); // before the track: a roadworks pattern may ask for a turn
            track?.ResetTrack(startPosition.z - 2f);
            rider?.ResetRider(startPosition);
            Gifts = 0;
            _giftPoints = 0;
            rider?.Begin();
            Camera.main?.GetComponent<FollowCamera>()?.SnapToTarget();
            SetState(RunState.Running);
            GameEvents.RaiseRunStarted();
            hud?.ShowRunning();
        }

        void OnHitObstacle(Obstacle obstacle)
        {
            if (State != RunState.Running) return;
            rider.Stop();
            SetState(RunState.Crashed);
            if (crashEffect != null) Destroy(Instantiate(crashEffect, rider.transform.position, Quaternion.identity), 4f);
            int score = Score;
            if (score > Best)
            {
                Best = score;
                try { PlayerPrefs.SetInt(BestKey, Best); PlayerPrefs.Save(); } catch (System.Exception) { /* storage blocked */ }
            }
            GameEvents.RaiseCrashed();
            RiderActions actions = GetComponent<RiderActions>();
            hud?.ShowCrashed(score, Best, actions != null ? actions.EndTitle(obstacle) : "CRASH!", actions != null ? actions.EndNote() : null);
        }

        void OnGotGift(Gift gift)
        {
            if (State != RunState.Running) return;
            Gifts++;
            _giftPoints += gift.points;
            GameObject fx = gift.pickupEffect != null ? gift.pickupEffect : giftEffect;
            if (fx != null) Destroy(Instantiate(fx, gift.transform.position + Vector3.up, Quaternion.identity), 3f);
            gift.gameObject.SetActive(false); // back with its chunk when the chunk is recycled
            GameEvents.RaiseGiftCollected();
        }

        void SetState(RunState s)
        {
            State = s;
            _stateTime = 0f;
        }
    }
}
