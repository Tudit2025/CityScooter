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
        /// <summary>The big title of the last end-of-ride screen ("CRASH!", "SORRY!", "ARRESTED!", "BUSTED!").</summary>
        public string EndTitle { get; private set; }

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
            // WA police (wanted level, chase, helicopter), also without scene changes.
            var police = GetComponent<PoliceChase>();
            if (police == null && rider != null) police = gameObject.AddComponent<PoliceChase>();
            if (police != null) { if (police.rider == null) police.rider = rider; if (police.theme == null && track != null) police.theme = track.theme; }
            // Programs triggering the game's own events ("do:chase", "do:car", ...).
            var triggers = GetComponent<ProgramTriggers>();
            if (triggers == null && rider != null) triggers = gameObject.AddComponent<ProgramTriggers>();
            if (triggers != null) { if (triggers.rider == null) triggers.rider = rider; if (triggers.track == null) triggers.track = track; }
            // ---- Districts (v0.3): look blending, zone:<id>, captions, far scenery, ambience, start keys; no scene changes.
            var districts = GetComponent<DistrictDirector>();
            if (districts == null && rider != null && track != null) districts = gameObject.AddComponent<DistrictDirector>();
            if (districts != null) { if (districts.rider == null) districts.rider = rider; if (districts.track == null) districts.track = track; if (districts.hud == null) districts.hud = hud; }
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
            RiderActions actions = GetComponent<RiderActions>();
            Finish(actions != null ? actions.EndTitle(obstacle) : "CRASH!", true);
        }

        /// <summary>
        /// End the ride without hitting anything (WA police: "ARRESTED!", the sniper: "BUSTED!"). With crash the
        /// usual crash happens too (crash sound, "crash" to the boards); without it the ride just stops.
        /// </summary>
        public void EndRun(string title, bool crash = true)
        {
            if (State != RunState.Running) return;
            Finish(title, crash);
        }

        void Finish(string title, bool crash)
        {
            rider.Stop();
            SetState(RunState.Crashed);
            if (crash && crashEffect != null) Destroy(Instantiate(crashEffect, rider.transform.position, Quaternion.identity), 4f);
            int score = Score;
            if (score > Best)
            {
                Best = score;
                try { PlayerPrefs.SetInt(BestKey, Best); PlayerPrefs.Save(); } catch (System.Exception) { /* storage blocked */ }
            }
            EndTitle = title;
            if (crash) GameEvents.RaiseCrashed();
            hud?.ShowCrashed(score, Best, title, EndNote());
        }

        string EndNote()
        {
            RiderActions actions = GetComponent<RiderActions>();
            PoliceChase police = GetComponent<PoliceChase>();
            string a = actions != null ? actions.EndNote() : null, p = police != null ? police.EndNote() : null;
            return string.IsNullOrEmpty(a) ? p : string.IsNullOrEmpty(p) ? a : a + "  ·  " + p;
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
