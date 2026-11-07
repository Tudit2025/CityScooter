using System.Collections.Generic;
using CityScooter.Audio;
using CityScooter.Controls;
using CityScooter.Devices;
using CityScooter.Game;
using CityScooter.Protocol;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Protocol v1 on the rider: punch ("pu" / F) knocks a punchable prop out of the way and sends "ko"; the bell
    /// ("bl" / R) makes people ahead step aside; "warn:car|ped|hole" for things in the rider's lane ahead (each
    /// thing once); "turn:-1|0|1" when the fake road turns. Cartoon mode ("toon:1" / T) makes people bounce away
    /// ("boing") instead of ending the run. Crashes throw the rider off the scooter depending on what was hit.
    /// WA police: punching an officer (BONK, the cap flies, "OI!") or a police car, or knocking an officer flying in
    /// cartoon mode, tells PoliceChase (one more star). Created by GameController, so no scene changes.
    /// </summary>
    public sealed class RiderActions : MonoBehaviour
    {
        public RiderController rider;
        public TrackTheme theme;
        [Tooltip("Seconds between punches: sending pu in a fast loop does not punch faster.")]
        public float punchCooldown = 0.4f;
        public float punchHalfWidth = 2.5f;
        [Tooltip("Punch reach ahead: at least this many metres, or this many seconds at the current speed.")]
        public float punchReach = 4f;
        public float punchReachSeconds = 0.2f;
        public float bellRange = 30f;
        [Tooltip("Warn about things this far ahead: at least warnDistance metres, or warnSeconds at the current speed.")]
        public float warnDistance = 25f;
        public float warnSeconds = 1.5f;
        public float warnHalfWidth = 1.3f;
        /// <summary>Steepest street grade the look-ahead boxes allow for (comic downhill is 0.079).</summary>
        const float MaxGrade = 0.1f;

        /// <summary>v1 cartoon mode: off until a program sends "toon:1" (or T on the keyboard). People bumped into go flying.</summary>
        public static bool CartoonMode { get; private set; }
        static bool _keyToon, _lastBoardToon;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { CartoonMode = false; CanBounce = false; _keyToon = false; _lastBoardToon = false; }

        /// <summary>For tests and the HUD.</summary>
        public int Punches { get; private set; }
        public int Bounced { get; private set; }
        /// <summary>Punches sent while out of breath, rings nobody listened to, punches that hit nothing.</summary>
        public int IgnoredPunches { get; private set; }
        public int IgnoredBells { get; private set; }
        public int Misses { get; private set; }
        /// <summary>Punches left (refills over time); see GameTuning "Punch and bell limits".</summary>
        public float Charges { get; private set; } = 3f;
        public int MaxCharges => Mathf.Max(1, Tune.punchCharges);
        /// <summary>Cartoon mode and a gift to pay with: bumping into someone sends them flying instead of ending the run.</summary>
        public static bool CanBounce { get; private set; }

        GameTuning Tune => rider != null && rider.tuning != null ? rider.tuning : GameTuning.Default;
        float _nextBell, _bellIgnoredUntil, _nextTired;
        readonly Queue<float> _rings = new Queue<float>();
        bool _punchOpen;
        public int Hits { get; private set; }
        public int Bells { get; private set; }

        float _nextPunch;
        int _turn;
        readonly Collider[] _hits = new Collider[32];
        readonly Dictionary<int, Vector2> _warned = new Dictionary<int, Vector2>(); // instance id -> (time, z) warned
        readonly Dictionary<GameObject, Stack<ComicPopup>> _popups = new Dictionary<GameObject, Stack<ComicPopup>>();
        readonly List<ComicPopup> _live = new List<ComicPopup>();
        CityAudioDirector _audio;
        int _horn;

        // Reuses the comic city's sound bank (no new clips yet): thud for a punch, click for the bell, horns for cars.
        internal void Cue(string id, Vector3? position = null)
        {
            if (_audio == null) _audio = FindAnyObjectByType<CityAudioDirector>();
            if (_audio != null) _audio.PlayCue(id, 1f, position);
        }

        void OnEnable()
        {
            Pedestrian.Startled += OnStartled;
            Pedestrian.RiderPosition = () => rider != null ? rider.transform.position : Vector3.zero;
            Pedestrian.RiderSpeed = () => rider != null ? rider.Speed : 0f;
            GameEvents.RunStarted += OnRunStarted;
        }

        RiderController _subscribed;
        void Subscribe()
        {
            if (_subscribed == rider) return;
            if (_subscribed != null) { _subscribed.HitObstacle -= OnHit; _subscribed.BouncedPerson -= OnBounced; }
            _subscribed = rider;
            if (rider != null) { rider.HitObstacle += OnHit; rider.BouncedPerson += OnBounced; }
        }

        void OnDisable()
        {
            Pedestrian.Startled -= OnStartled;
            GameEvents.RunStarted -= OnRunStarted;
            if (_subscribed != null) { _subscribed.HitObstacle -= OnHit; _subscribed.BouncedPerson -= OnBounced; }
            _subscribed = null;
            if (Time.timeScale < 1f) Time.timeScale = 1f; // never leave a hit-stop behind
        }

        void OnRunStarted()
        {
            _warned.Clear();
            _turn = 0;
            _nextPunch = 0f;
            _nextBell = _bellIgnoredUntil = 0f;
            _rings.Clear();
            _punchOpen = false;
            Charges = MaxCharges;
            Punches = Hits = Misses = IgnoredPunches = IgnoredBells = Bells = Bounced = 0;
            ClearCrash();
        }

        /// <summary>Keyboard T, or a test: flip cartoon mode for this session.</summary>
        public static void ToggleCartoonKey() => _keyToon = !_keyToon;

        void Update()
        {
            Subscribe();
            DeviceHub hub = DeviceHub.Instance;
            if ((hub == null || hub.KeyboardEnabled) && KeyboardControls.ToonTogglePressed()) ToggleCartoonKey();
            bool boardToon = hub != null && hub.Mixer.Toon;
            if (_lastBoardToon && !boardToon) _keyToon = false; // a board turning it off wins over the T key
            _lastBoardToon = boardToon;
            CartoonMode = _keyToon || boardToon;
            GameController game = GameController.Instance;
            CanBounce = CartoonMode && (!Tune.cartoonCostsGift || (game != null && game.Gifts > 0));
            Charges = Mathf.Min(MaxCharges, Charges + Time.deltaTime / Mathf.Max(0.1f, Tune.punchRechargeSeconds));
            if (rider != null) rider.SteerFactor = (_arm != null && _arm.Busy) || Punching ? Tune.punchSteerFactor : 1f;
            if (rider == null || !rider.Running) return;
            RiderInput input = RiderInputSource.Read();
            if (input.Punch) Punch();
            UpdatePunch();
            if (input.Bell) Bell();
            if (input.Boost && rider.Boost())
            {
                Popup(theme != null ? theme.boostPopup : null, rider.transform.position + new Vector3(-1.3f, 2.3f, 0f), rider.transform);
                Cue("SCO_Jump");
                ShakeCamera(0.15f);
            }
            Warn();
            FakeTurnDirector turns = FakeTurnDirector.Instance;
            int dir = turns != null ? turns.Direction : 0;
            if (dir != _turn) { _turn = dir; GameEvents.RaiseTurnChanged(dir); }
        }

        // ---------------------------------------------------------------- punch: the rider's arm

        PunchArm _arm;
        float _punchUntil, _punchSide = 1f;
        bool _punchLanded;

        /// <summary>True while a punch is swinging (its hit window).</summary>
        public bool Punching => Time.time < _punchUntil;
        public PunchArm Arm => _arm;

        /// <summary>Metres ahead the fist reaches; something must be (or come) this close while the punch is out.</summary>
        public float fistReach = 2.4f;

        /// <summary>
        /// Throw a punch: the rider's arm swings at once. Anything within the fist's reach during the next 0.3 s gets
        /// hit (riding forward brings things into reach), so the timing does not have to be frame-perfect.
        /// Returns true if something was hit right away.
        /// </summary>
        public bool Punch()
        {
            if (Time.time < _nextPunch) return false;
            if (Charges < 1f)
            {
                // Out of breath: spamming pu in a loop gets ignored (and the board hears "tired").
                IgnoredPunches++;
                if (Time.time >= _nextTired) { _nextTired = Time.time + 1f; GameEvents.RaiseTired(); }
                return false;
            }
            Charges -= 1f;
            _punchOpen = true;
            _nextPunch = Time.time + punchCooldown;
            _punchUntil = Time.time + 0.3f;
            _punchLanded = false;
            Punches++;
            // Aim at what is coming (further ahead), hit only what the fist reaches.
            Obstacle aim = FindPunchTarget(Mathf.Max(punchReach, rider.Speed * punchReachSeconds), out _);
            Vector3 p = rider.transform.position;
            _punchSide = aim != null ? (aim.transform.position.x >= p.x ? 1f : -1f) : (p.x > 0.5f ? -1f : 1f);
            rider.PunchTwist(_punchSide);
            FireArm(aim);
            Popup(theme != null ? theme.swishPopup : null, p + new Vector3(_punchSide * 1.3f, 1.35f, 1.1f), rider.transform);
            Cue("UI_Click");
            Obstacle target = FindPunchTarget(FistZone(), out bool solid);
            return target != null && Land(target, solid);
        }

        // The fist's reach plus this frame's ride, so fast riders cannot skip past a target between frames.
        float FistZone() => fistReach + rider.Speed * Time.deltaTime * 1.5f;

        void UpdatePunch()
        {
            if (!Punching)
            {
                if (_punchOpen && !_punchLanded) { Misses++; GameController.Instance?.AddPoints(-Tune.punchMissPoints); }
                _punchOpen = false;
                return;
            }
            if (_punchLanded) return;
            Obstacle target = FindPunchTarget(FistZone(), out bool solid);
            if (target != null) Land(target, solid);
        }

        Obstacle FindPunchTarget(float reach, out bool solid)
        {
            solid = false;
            Vector3 p = rider.transform.position;
            float drop = reach * MaxGrade; // the street runs downhill: things ahead sit lower
            var centre = new Vector3(p.x, p.y + 0.9f - drop * 0.5f, p.z + reach * 0.5f);
            int n = Physics.OverlapBoxNonAlloc(centre, new Vector3(punchHalfWidth, 1.2f + drop * 0.5f, reach * 0.5f + 0.5f), _hits, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Obstacle best = null;
            float bestDz = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Obstacle o = _hits[i] != null ? _hits[i].GetComponentInParent<Obstacle>() : null;
                if (o == null || !o.gameObject.activeInHierarchy || o.transform.IsChildOf(rider.transform)) continue;
                Knockout k = o.GetComponent<Knockout>();
                if (k != null && k.KnockedOut) continue;
                float dz = o.transform.position.z - p.z;
                if (dz < -1f || dz >= bestDz) continue;
                bestDz = dz;
                best = o;
            }
            if (best != null) solid = !(best.punchable || best.kind == ObstacleKind.Person);
            return best;
        }

        void FireArm(Obstacle target)
        {
            if (_arm == null && theme != null && theme.punchArm != null && rider.model != null)
            {
                GameObject go = Instantiate(theme.punchArm, rider.model);
                go.name = theme.punchArm.name;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                _arm = go.GetComponent<PunchArm>();
            }
            if (_arm == null) return;
            // A hook to the side the target is on, or straight-ish ahead on a miss.
            Vector3 aim = target != null
                ? rider.model.InverseTransformPoint(target.transform.position + Vector3.up * 1.1f)
                : new Vector3(_punchSide * 0.9f, 1.35f, 1.6f);
            _arm.Fire(_punchSide, aim);
        }

        // What happens where the glove lands depends on what it hits.
        bool Land(Obstacle target, bool solid)
        {
            _punchLanded = true;
            Vector3 p = rider.transform.position, at = target.transform.position + Vector3.up * 1f;
            float side = target.transform.position.x >= p.x ? 1f : -1f;
            if (_arm != null && rider.model != null) _arm.Strike(rider.model.InverseTransformPoint(at + Vector3.up * 0.1f));
            if (solid)
            {
                // Cars, barriers, signs, workers: they shake, BONK, the glove springs back.
                Wobble w = target.GetComponent<Wobble>();
                if (w == null) w = target.gameObject.AddComponent<Wobble>();
                w.Hit(side);
                if (_arm != null) _arm.Recoil();
                Popup(theme != null ? theme.bonkPopup : null, at + Vector3.up * 0.8f);
                Cue("SCO_Land_01", at);
                ShakeCamera(0.2f);
                if (target.police) PoliceChase.Instance?.ReportAttack(target); // punching a police car: one more star
                return false;
            }
            if (target.kind == ObstacleKind.Person && target.police && !CartoonMode)
            {
                // A police officer does not go flying (cartoon mode off): BONK, the cap pops off, "OI!". One more star.
                BonkOfficer(target, at, side);
                PoliceChase.Instance?.ReportAttack(target);
                return false;
            }
            if (target.kind == ObstacleKind.Person && !CartoonMode)
            {
                // Not in cartoon mode: the glove only makes people jump out of the way.
                Pedestrian ped = target.GetComponent<Pedestrian>();
                if (ped != null) ped.Startle(p.x);
                if (_arm != null) _arm.Recoil();
                return false;
            }
            Knockout ko = target.GetComponent<Knockout>();
            if (ko == null) ko = target.gameObject.AddComponent<Knockout>();
            ko.Launch(target.kind == ObstacleKind.Person
                ? new Vector3(side * 7f, 13f, Mathf.Max(8f, rider.Speed * 0.7f))
                : new Vector3(side * 10f, 9f, Mathf.Max(9f, rider.Speed * 0.95f)));
            Hits++;
            GameController.Instance?.AddPoints(Tune.punchHitPoints);
            Cue("SCO_Land_02", at);
            Popup(theme != null ? theme.impactPopup : null, at);
            Popup(theme != null ? theme.punchPopup : null, at + Vector3.up * 0.7f);
            ShakeCamera(0.35f);
            StartCoroutine(HitStop());
            GameEvents.RaisePunchLanded();
            if (target.police) PoliceChase.Instance?.ReportAttack(target); // cartoon mode: an officer sent flying
            return true;
        }

        // ---------------------------------------------------------------- WA police

        readonly List<GameObject> _caps = new List<GameObject>();

        void BonkOfficer(Obstacle officer, Vector3 at, float side)
        {
            Wobble w = officer.GetComponent<Wobble>();
            if (w == null) w = officer.gameObject.AddComponent<Wobble>();
            w.Hit(side);
            if (_arm != null) _arm.Recoil();
            Popup(theme != null ? theme.bonkPopup : null, at + Vector3.up * 0.8f);
            Popup(theme != null ? theme.oiPopup : null, officer.transform.position + new Vector3(side * 1f, 3.1f, 0f), officer.transform);
            Cue("SCO_Land_01", at);
            ShakeCamera(0.2f);
            PoliceOfficer po = officer.GetComponent<PoliceOfficer>();
            if (po == null || theme == null || theme.policeCap == null || !po.TakeCapOff(out Vector3 capAt, out Quaternion capRot)) return;
            GameObject cap = null;
            foreach (GameObject c in _caps) if (c != null && !c.activeSelf) { cap = c; break; }
            if (cap == null)
            {
                cap = Instantiate(theme.policeCap, transform);
                cap.name = theme.policeCap.name;
                _caps.Add(cap);
            }
            cap.transform.SetPositionAndRotation(capAt, capRot);
            cap.SetActive(true);
            Knockout k = cap.GetComponent<Knockout>();
            if (k == null) k = cap.AddComponent<Knockout>();
            k.Launch(new Vector3(side * 2.5f, 7.5f, Mathf.Max(4f, rider.Speed * 0.6f)));
        }

        GameObject _paintBlob;

        /// <summary>A paint pellet hit the rider (PoliceChase.PaintBust): "SPLAT!", a pink blob, a gentle topple.</summary>
        internal void PaintSplat(Vector3 at)
        {
            if (rider == null) return;
            Popup(theme != null ? theme.splatPopup : null, rider.transform.position + new Vector3(0f, 2.4f, 0.6f));
            Cue("SCO_Land_02", at);
            ThrowRider(null);
            if (_crashRider == null || theme == null || theme.paintSplat == null) return;
            if (_paintBlob == null) { _paintBlob = Instantiate(theme.paintSplat, _crashRider.transform); _paintBlob.name = "PaintOnTheRider"; }
            _paintBlob.transform.SetParent(_crashRider.transform, false);
            _paintBlob.transform.localPosition = new Vector3(0f, 1.35f, -0.19f);
            _paintBlob.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // the ground decal turned to face backwards
            _paintBlob.transform.localScale = Vector3.one * 0.45f;
            _paintBlob.SetActive(true);
        }

        // A tiny freeze on impact makes a hit feel solid (comic "hit stop").
        System.Collections.IEnumerator HitStop()
        {
            if (Time.timeScale < 0.99f) yield break;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(0.06f);
            Time.timeScale = 1f;
        }

        public void Bell()
        {
            if (Time.time < _nextBell) return;
            _nextBell = Time.time + Tune.bellCooldown;
            Bells++;
            Vector3 p = rider.transform.position;
            Cue("UI_Click");
            while (_rings.Count > 0 && Time.time - _rings.Peek() > Tune.bellSpamWindow) _rings.Dequeue();
            _rings.Enqueue(Time.time);
            if (_rings.Count > Tune.bellSpamCount) _bellIgnoredUntil = Time.time + Tune.bellIgnoreSeconds;
            if (Time.time < _bellIgnoredUntil)
            {
                // Rung too often: people stop listening for a while ("...").
                IgnoredBells++;
                Popup(theme != null ? theme.ignorePopup : null, p + new Vector3(1.3f, 2.4f, 0f), rider.transform);
                return;
            }
            Popup(theme != null ? theme.bellPopup : null, p + new Vector3(1.3f, 2.4f, 0f), rider.transform);
            for (int i = Pedestrian.Active.Count - 1; i >= 0; i--)
            {
                Pedestrian ped = Pedestrian.Active[i];
                if (ped == null) continue;
                float dz = ped.transform.position.z - p.z;
                if (dz > -1f && dz < bellRange) ped.Startle(p.x);
            }
        }

        void Warn()
        {
            Vector3 p = rider.transform.position;
            float range = Mathf.Max(warnDistance, rider.Speed * warnSeconds);
            float drop = (range + 2f) * MaxGrade;
            var centre = new Vector3(p.x, p.y + 1f - drop * 0.5f, p.z + 2f + range * 0.5f);
            int n = Physics.OverlapBoxNonAlloc(centre, new Vector3(warnHalfWidth, 1.5f + drop * 0.5f, range * 0.5f), _hits, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                Obstacle o = _hits[i] != null ? _hits[i].GetComponentInParent<Obstacle>() : null;
                if (o == null || o.kind == ObstacleKind.Thing) continue;
                int id = o.GetInstanceID();
                float z = o.transform.position.z;
                // A pooled object reused further up the road is a new thing: warn again if it moved more than 20 m.
                if (_warned.TryGetValue(id, out Vector2 seen) && Time.time - seen.x < 8f && Mathf.Abs(z - seen.y) < 20f) continue;
                _warned[id] = new Vector2(Time.time, z);
                if (o.kind == ObstacleKind.Vehicle && o.GetComponent<Mover>() != null) Cue((_horn++ & 1) == 0 ? "VEH_Horn_01" : "VEH_Horn_02", o.transform.position);
                GameEvents.RaiseWarning(o.kind == ObstacleKind.Vehicle ? Messages.WarnCar : o.kind == ObstacleKind.Person ? Messages.WarnPerson : Messages.WarnHole);
            }
        }

        void OnStartled(Pedestrian ped) => Popup(theme != null ? theme.startlePopup : null, ped.transform.position + Vector3.up * 2.2f, ped.transform);

        // Cartoon mode: the person goes flying (no injuries in comics), the rider wobbles and loses speed.
        void OnBounced(Obstacle o)
        {
            if (o == null) return;
            Knockout k = o.GetComponent<Knockout>();
            if (k == null) k = o.gameObject.AddComponent<Knockout>();
            if (k.KnockedOut) return;
            Vector3 p = rider.transform.position;
            float side = o.transform.position.x >= p.x ? 1f : -1f;
            k.Launch(new Vector3(side * 5f, 13f, Mathf.Clamp(rider.Speed * 0.7f, 6f, 25f)));
            if (o.police) PoliceChase.Instance?.ReportAttack(o); // cartoon mode: rode an officer flying
            rider.Bump(0.65f);
            if (Tune.cartoonCostsGift) GameController.Instance?.SpendGift();
            Bounced++;
            Cue("SCO_Land_01", o.transform.position);
            Popup(theme != null ? theme.boingPopup : null, o.transform.position + Vector3.up * 2.2f);
            ShakeCamera(0.25f);
            GameEvents.RaisePersonBounced();
        }

        /// <summary>Title for the end-of-ride screen.</summary>
        public string EndTitle(Obstacle hit)
        {
            if (hit != null && PoliceChase.Instance != null && PoliceChase.Instance.IsChaseCar(hit)) return "BUSTED!";
            return hit != null && hit.kind == ObstacleKind.Person ? "SORRY!" : "CRASH!";
        }

        /// <summary>Punch accuracy for the end-of-ride screen (null when no punches were thrown).</summary>
        public string EndNote()
        {
            if (Punches == 0) return null;
            int pct = Mathf.RoundToInt(100f * Hits / Mathf.Max(1, Punches));
            return "Punches " + Punches + "  ·  hits " + Hits + " (" + pct + "%)" + (IgnoredPunches > 0 ? "  ·  too tired " + IgnoredPunches + "x" : "");
        }

        // ---------------------------------------------------------------- crash show

        CrashTumble _crashRider, _crashScooter;
        GameObject _hiddenModel;

        /// <summary>For tests: the thrown rider (null before the first crash).</summary>
        public CrashTumble CrashRider => _crashRider;

        void OnHit(Obstacle o)
        {
            if (o == null || rider == null) return;
            if (o.kind == ObstacleKind.Person) Popup(theme != null ? theme.sorryPopup : null, o.transform.position + Vector3.up * 2.3f);
            else if (PoliceChase.Instance != null && PoliceChase.Instance.IsChaseCar(o)) Popup(theme != null ? theme.bonkPopup : null, rider.transform.position + new Vector3(0f, 2.2f, 0f));
            else Popup(theme != null ? theme.crashPopup : null, rider.transform.position + new Vector3(0f, 2.4f, 1f));
            ThrowRider(o);
        }

        enum Throw { ChaseCar, Oncoming, Car, Hole, Person, Paint, Thing }

        static Throw StyleOf(Obstacle o)
        {
            if (o == null) return Throw.Paint;
            if (PoliceChase.Instance != null && PoliceChase.Instance.IsChaseCar(o)) return Throw.ChaseCar;
            Mover mover = o.GetComponent<Mover>();
            switch (o.kind)
            {
                case ObstacleKind.Vehicle: return mover != null && mover.speed < 0f ? Throw.Oncoming : Throw.Car;
                case ObstacleKind.Hole: return Throw.Hole;
                case ObstacleKind.Person: return Throw.Person;
                default: return Throw.Thing;
            }
        }

        // The rider leaves the scooter in a way that matches what was hit (null: a paint pellet).
        void ThrowRider(Obstacle o)
        {
            if (theme == null || theme.crashRider == null || theme.crashScooter == null || rider.model == null) return;
            _crashRider = Spawn(_crashRider, theme.crashRider);
            _crashScooter = Spawn(_crashScooter, theme.crashScooter);
            Transform model = rider.model;
            Vector3 at = model.position;
            float yaw = model.eulerAngles.y;
            Quaternion facing = Quaternion.Euler(0f, yaw, 0f);
            float v = Mathf.Max(rider.Speed, 6f), side = Random.value < 0.5f ? -1f : 1f;
            Quaternion lying = Quaternion.Euler(90f, 0f, 0f), onSide = Quaternion.Euler(0f, 0f, 90f * side);
            switch (StyleOf(o))
            {
                case Throw.ChaseCar:
                    // The police car from behind: thrown forward and high (BUSTED), the scooter skids on ahead.
                    _crashRider.Throw(at + Vector3.up * 0.5f, facing, new Vector3(side * 2f, 10f, Mathf.Clamp(v * 0.4f + 10f, 12f, 26f)), new Vector3(560f, 0f, 90f * side), lying, 0.15f);
                    _crashScooter.Throw(at, facing, new Vector3(side * 2.5f, 3f, Mathf.Clamp(v * 0.3f + 6f, 8f, 18f)), new Vector3(200f, 160f * side, 240f * side), onSide, 0.2f);
                    ShakeCamera(0.85f);
                    break;
                case Throw.Paint:
                    // Splat: a surprised wobble and a gentle topple, the scooter rolls on and falls over.
                    _crashRider.Throw(at + Vector3.up * 0.3f, facing, new Vector3(side * 1.5f, 4.5f, Mathf.Clamp(v * 0.2f, 3f, 6f)), new Vector3(200f, 0f, 120f * side), lying, 0.15f);
                    _crashScooter.Throw(at, facing, new Vector3(side * 1f, 1f, Mathf.Clamp(v * 0.25f, 2f, 8f)), new Vector3(0f, 0f, 220f * side), onSide, 0.2f);
                    ShakeCamera(0.4f);
                    break;
                case Throw.Oncoming:
                    // Oncoming car: thrown back and high, the scooter skids back too.
                    _crashRider.Throw(at + Vector3.up * 0.4f, facing, new Vector3(side * 3f, 11f, -Mathf.Clamp(v * 0.25f + 12f, 12f, 30f)), new Vector3(-540f, 90f * side, 0f), lying, 0.15f);
                    _crashScooter.Throw(at, facing, new Vector3(side * 2f, 4f, -10f), new Vector3(0f, 200f * side, 300f * side), onSide, 0.2f);
                    ShakeCamera(0.9f);
                    break;
                case Throw.Car:
                    // Into a parked or slower car: over the bonnet, the scooter stops dead.
                    _crashRider.Throw(at + Vector3.up * 0.6f, facing, new Vector3(side * 1.5f, 9f, Mathf.Clamp(v * 0.35f, 5f, 14f)), new Vector3(600f, 0f, 60f * side), lying, 0.15f);
                    _crashScooter.Throw(at, facing, new Vector3(0f, 2f, -2f), new Vector3(-120f, 0f, 160f * side), onSide, 0.2f);
                    ShakeCamera(0.7f);
                    break;
                case Throw.Hole:
                    // Front wheel into the open manhole: the scooter noses down and sticks, the rider goes over the bars.
                    _crashScooter.Stick(new Vector3(o.transform.position.x, at.y - 0.25f, o.transform.position.z - 0.35f), facing * Quaternion.Euler(38f, 0f, 0f));
                    _crashRider.Throw(at + Vector3.up * 0.7f, facing, new Vector3(side * 0.8f, 7.5f, Mathf.Clamp(v * 0.45f, 6f, 16f)), new Vector3(480f, 0f, 0f), lying, 0.15f);
                    ShakeCamera(0.5f);
                    break;
                case Throw.Person:
                    // A gentle topple: nobody gets hurt.
                    _crashRider.Throw(at + Vector3.up * 0.3f, facing, new Vector3(side * 1.2f, 3.5f, 3f), new Vector3(150f, 0f, 90f * side), lying, 0.15f);
                    _crashScooter.Throw(at, facing, new Vector3(side * 1.5f, 1f, 2f), new Vector3(0f, 0f, 220f * side), onSide, 0.2f);
                    ShakeCamera(0.3f);
                    break;
                default:
                    // Cones, bins, barriers, signs: launched forward over the obstacle, the scooter slides away.
                    _crashRider.Throw(at + Vector3.up * 0.5f, facing, new Vector3(side * 1f, 6.5f, Mathf.Clamp(v * 0.5f, 6f, 18f)), new Vector3(420f, 0f, 40f * side), lying, 0.15f);
                    _crashScooter.Throw(at, facing, new Vector3(side * 3f, 1.5f, Mathf.Clamp(v * 0.3f, 3f, 10f)), new Vector3(0f, 120f * side, 260f * side), onSide, 0.2f);
                    ShakeCamera(0.55f);
                    break;
            }
            _hiddenModel = model.gameObject;
            _hiddenModel.SetActive(false);
        }

        CrashTumble Spawn(CrashTumble existing, GameObject prefab)
        {
            if (existing == null)
            {
                GameObject go = Instantiate(prefab, transform);
                go.name = prefab.name;
                existing = go.GetComponent<CrashTumble>();
                if (existing == null) existing = go.AddComponent<CrashTumble>();
            }
            existing.track = rider.track;
            return existing;
        }

        void ClearCrash()
        {
            if (_crashRider != null) _crashRider.gameObject.SetActive(false);
            if (_crashScooter != null) _crashScooter.gameObject.SetActive(false);
            if (_paintBlob != null) _paintBlob.SetActive(false);
            if (_hiddenModel != null) _hiddenModel.SetActive(true);
            _hiddenModel = null;
        }

        internal static void ShakeCamera(float strength)
        {
            Camera cam = Camera.main;
            FollowCamera follow = cam != null ? cam.GetComponent<FollowCamera>() : null;
            if (follow != null) follow.Shake(strength);
        }

        internal void Popup(GameObject prefab, Vector3 position, Transform follow = null)
        {
            if (prefab == null) return;
            if (!_popups.TryGetValue(prefab, out Stack<ComicPopup> pool)) _popups[prefab] = pool = new Stack<ComicPopup>();
            for (int i = _live.Count - 1; i >= 0; i--)
                if (_live[i] == null || !_live[i].gameObject.activeSelf) { ReturnToPool(_live[i]); _live.RemoveAt(i); }
            ComicPopup popup = pool.Count > 0 ? pool.Pop() : null;
            if (popup == null)
            {
                GameObject go = Instantiate(prefab, transform);
                go.name = prefab.name;
                popup = go.GetComponent<ComicPopup>();
                if (popup == null) popup = go.AddComponent<ComicPopup>();
                _prefabOf[popup] = prefab;
            }
            popup.Show(position, follow);
            _live.Add(popup);
        }

        readonly Dictionary<ComicPopup, GameObject> _prefabOf = new Dictionary<ComicPopup, GameObject>();

        void ReturnToPool(ComicPopup popup)
        {
            if (popup == null || !_prefabOf.TryGetValue(popup, out GameObject prefab)) return;
            _popups[prefab].Push(popup);
        }
    }
}
