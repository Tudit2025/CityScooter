using CityScooter.Controls;
using CityScooter.Game;
using CityScooter.Protocol;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// WA police: the wanted level (0..5 stars), the police car that chases the rider, and how a chase ends.
    /// Attacking police (punching an officer or a police car, knocking an officer flying in cartoon mode) adds a star;
    /// the first star starts a chase. The police car takes turns with two tactics:
    ///   Alongside: it pulls up in the next lane; boxed in for arrestSeconds = ARRESTED (brake, boost or swerve away).
    ///   Ram: it closes in on the rider's lane and commits copLungeWarning seconds before impact; a hit = BUSTED, a
    ///        sidestep after it commits makes it overshoot and spin out ("SCREECH!").
    /// Pulling over while chased (or "sur") = ARRESTED. The police car being gone or far behind for escapeSeconds, or
    /// no new attack for wantedDecaySeconds, drops a star; at 0 stars "LOST THEM!" and bonus points. At 3+ stars the
    /// PoliceHelicopter joins in: it drops nets (caught = ARRESTED) and, from 4 stars, a sniper fires (hit = BUSTED).
    /// Created by GameController, so no scene changes.
    /// Fair by design: only a committed ram (after its warning) drives at the rider. Otherwise the police car never
    /// closes in on the rider's lane from right behind and never cuts across the rider; it slows down instead.
    /// Numbers: GameTuning "WA police".
    /// </summary>
    [DefaultExecutionOrder(-42)] // the police car moves before TrackGenerator syncs physics and before the rider sweeps
    public sealed class PoliceChase : MonoBehaviour
    {
        public enum Tactic { Alongside, Ram }
        public enum CopMode { None, Approach, Lunge, SpinOut, DropBack, Escort, Leaving }

        public static PoliceChase Instance { get; private set; }

        public RiderController rider;
        public TrackTheme theme;

        public int Wanted { get; private set; }
        /// <summary>Highest wanted level of this ride (end-of-ride note, "LOST THEM!" bonus).</summary>
        public int PeakWanted { get; private set; }
        public bool Chasing { get; private set; }
        public CopMode Mode { get; private set; }
        public Tactic CurrentTactic { get; private set; }
        /// <summary>The chasing police car (null before the first chase).</summary>
        public Obstacle Cop => _copObstacle;
        public bool CopOnRoad => _cop != null && _cop.activeSelf && Mode != CopMode.None;
        /// <summary>Metres from the police car's front bumper to the rider (negative once it is level or ahead).</summary>
        public float CopGap { get; private set; } = float.PositiveInfinity;
        /// <summary>Police car x minus rider x (HUD arrow).</summary>
        public float CopOffsetX => rider != null ? _copX - rider.transform.position.x : 0f;
        /// <summary>0 (right behind you) .. 9 (far, or no police car): what "cop:N" carries.</summary>
        public int CopLevel { get; private set; } = Messages.MaxCopLevel;
        /// <summary>The police car is about to ram, or has the rider boxed in.</summary>
        public bool CopDanger => CopOnRoad && (Mode == CopMode.Lunge || _boxedIn > 0.05f);
        public int SpinOuts { get; private set; }
        public int LastBonus { get; private set; }
        /// <summary>Stars of this chase earned by attacking the police (ReportAttack). Only these pay the LOST THEM!
        /// bonus: stars a program sets ("do:stars:5", via SetWanted) earn nothing, so the bonus cannot be farmed.</summary>
        public int EarnedStars { get; private set; }
        public PoliceHelicopter Helicopter => _heli;

        GameObject _cop;
        Obstacle _copObstacle;
        Collider[] _copColliders;
        Blinker _copBlinker;
        float _copHalfLength = 2.15f, _copHalfWidth = 0.95f;
        float _copZ, _copX, _copSpeed, _copYaw, _spinRate, _modeTime, _lungeSpeed, _side = 1f;
        int _attempt;
        Tactic? _forced;
        float _respawnAt, _lastAttack = -99f, _lastStarChange, _escape, _boxedIn, _pulledOver, _freeFor;
        bool _surrender, _boxedOnce;
        readonly Collider[] _hits = new Collider[24];
        PoliceHelicopter _heli;
        RiderActions _actions;
        RiderController _subscribed;

        GameTuning Tune => rider != null && rider.tuning != null ? rider.tuning : GameTuning.Default;
        TrackGenerator Track => rider != null ? rider.track : null;
        RiderActions Actions { get { if (_actions == null) _actions = GetComponent<RiderActions>(); return _actions; } }
        static bool Running => GameController.Instance != null && GameController.Instance.State == RunState.Running;
        int MaxStars => Mathf.Clamp(Tune.wantedMax, 1, Messages.MaxWanted);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake() => Instance = this;

        void OnEnable()
        {
            Instance = this;
            GameEvents.RunStarted += OnRunStarted;
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            GameEvents.RunStarted -= OnRunStarted;
            if (_subscribed != null) _subscribed.HitObstacle -= OnRiderHit;
            _subscribed = null;
        }

        void Start()
        {
            _heli = GetComponent<PoliceHelicopter>();
            if (_heli == null) _heli = gameObject.AddComponent<PoliceHelicopter>();
            _heli.chase = this;
        }

        void Subscribe()
        {
            if (_subscribed == rider) return;
            if (_subscribed != null) _subscribed.HitObstacle -= OnRiderHit;
            _subscribed = rider;
            if (rider != null) rider.HitObstacle += OnRiderHit;
        }

        void OnRunStarted()
        {
            _surrender = false;
            _boxedIn = _pulledOver = _escape = 0f;
            _attempt = 0;
            _forced = null;
            _lastAttack = -99f;
            LastBonus = SpinOuts = PeakWanted = EarnedStars = 0;
            HideCop();
            if (Chasing) { Chasing = false; GameEvents.RaiseChaseChanged(false); } // normally already sent when the ride ended
            if (Wanted != 0) { Wanted = 0; GameEvents.RaiseWantedChanged(0); }
            CopLevel = Messages.MaxCopLevel;
            CopGap = float.PositiveInfinity;
        }

        // ---------------------------------------------------------------- wanted level

        /// <summary>Someone attacked the police (RiderActions): one more star, at most once per policeAttackCooldown.</summary>
        public bool ReportAttack(Obstacle target)
        {
            if (!Running || target == null || !target.police) return false;
            if (RaceDirector.On) return false; // ---- Radio race (v0.4): no police in race mode
            if (Time.time - _lastAttack < Tune.policeAttackCooldown) return false;
            _lastAttack = Time.time;
            int before = Wanted;
            SetWanted(Wanted + 1);
            if (Wanted > before) EarnedStars++; // after SetWanted: starting a chase resets the count
            return true;
        }

        /// <summary>
        /// Set the wanted level directly (programs' "do:chase" / "do:stars:N" / "do:heli" / "do:calm", tests, scripted
        /// moments). The first star starts a chase; 0 ends it quietly. Stars set this way pay no LOST THEM! bonus.
        /// </summary>
        public void SetWanted(int stars)
        {
            if (!Running) return;
            if (RaceDirector.On && stars > 0) return; // ---- Radio race (v0.4): no police in race mode
            stars = Mathf.Clamp(stars, 0, MaxStars);
            if (stars == Wanted)
            {
                _lastStarChange = Time.time; // attacking again at 5 stars keeps them from wearing off
                return;
            }
            bool start = Wanted == 0;
            ChangeWanted(stars);
            if (stars == 0) EndChase();
            else if (start) StartChase();
        }

        void ChangeWanted(int stars)
        {
            Wanted = stars;
            PeakWanted = Mathf.Max(PeakWanted, stars);
            _lastStarChange = Time.time;
            _escape = 0f;
            GameEvents.RaiseWantedChanged(stars);
        }

        void LoseStar()
        {
            if (Wanted <= 1) { LostThem(); return; }
            ChangeWanted(Wanted - 1);
        }

        void LostThem()
        {
            // Only stars earned by attacking the police pay (never more than the highest level of this chase).
            LastBonus = Tune.lostBonusPerStar * Mathf.Min(EarnedStars, PeakWanted);
            GameController game = GameController.Instance;
            if (LastBonus > 0) game?.AddPoints(LastBonus);
            ChangeWanted(0);
            EndChase();
            if (game != null && game.hud != null) game.hud.Flash("LOST THEM!", LastBonus > 0 ? "+" + LastBonus + " points" : "no bonus: those stars came from a program");
            Actions?.Cue("UI_Pickup_01");
            GameEvents.RaiseLostPolice();
        }

        void StartChase()
        {
            Chasing = true;
            EarnedStars = 0;
            _attempt = 0;
            _escape = _boxedIn = _pulledOver = 0f;
            _surrender = false;
            _respawnAt = Time.time + Tune.copFirstDelay;
            if (rider != null) rider.BrakeFloor = 0f; // pulling over is allowed while chased
            GameEvents.RaiseChaseChanged(true);
            GameController game = GameController.Instance;
            if (game != null && game.hud != null) game.hud.Flash("WANTED!", "The WA police are after you");
        }

        void EndChase()
        {
            if (!Chasing) return;
            Chasing = false;
            _surrender = false;
            _boxedIn = _pulledOver = 0f;
            if (rider != null) { rider.BrakeFloor = -1f; rider.HoldBrake = false; }
            if (CopOnRoad && Mode != CopMode.SpinOut) SetMode(CopMode.Leaving);
            if (_heli != null) _heli.StandDown();
            GameEvents.RaiseChaseChanged(false);
            CopLevel = Messages.MaxCopLevel;
        }

        void UpdateStars(float dt)
        {
            bool far = !CopOnRoad || CopGap > Tune.escapeDistance;
            _escape = far ? _escape + dt : Mathf.Max(0f, _escape - 0.5f * dt);
            if (_escape >= Tune.escapeSeconds) { _escape = 0f; LoseStar(); return; }
            if (Time.time - _lastStarChange >= Tune.wantedDecaySeconds) LoseStar();
        }

        // ---------------------------------------------------------------- endings

        public bool IsChaseCar(Obstacle o) => o != null && o == _copObstacle;

        void OnRiderHit(Obstacle o)
        {
            // GameController has already ended the ride ("BUSTED!" comes from RiderActions.EndTitle).
            if (!Chasing) return;
            bool busted = IsChaseCar(o);
            EndChase();
            if (busted) GameEvents.RaiseBusted();
        }

        void Arrest()
        {
            GameController game = GameController.Instance;
            if (game == null || game.State != RunState.Running) return;
            if (CopOnRoad && Mode == CopMode.Escort) _copSpeed = Mathf.Min(_copSpeed, rider.Speed);
            game.EndRun("ARRESTED!", false);
            Actions?.Cue("VEH_Horn_02", rider.transform.position);
            EndChase();
            GameEvents.RaiseArrested(); // DeviceFeedback sends "crash" first, then "arrested"
        }

        /// <summary>The helicopter's net landed on the rider (PoliceHelicopter): "NETTED!", ARRESTED.</summary>
        internal void NetArrest(Vector3 at)
        {
            GameController game = GameController.Instance;
            if (game == null || game.State != RunState.Running) return;
            game.EndRun("ARRESTED!", false);
            Actions?.Netted(at);
            EndChase();
            GameEvents.RaiseArrested(); // DeviceFeedback sends "crash" first, then "arrested"
        }

        /// <summary>The sniper's shot hit the rider (PoliceHelicopter): BUSTED, the rider is knocked off the scooter.</summary>
        internal void SniperBust(Vector3 at)
        {
            GameController game = GameController.Instance;
            if (game == null || game.State != RunState.Running) return;
            game.EndRun("BUSTED!", true);
            Actions?.Shot(at);
            EndChase();
            GameEvents.RaiseBusted();
        }

        /// <summary>For the end-of-ride screen (null when the police never came).</summary>
        public string EndNote()
        {
            if (PeakWanted == 0) return null;
            return "Wanted " + PeakWanted + (PeakWanted == 1 ? " star" : " stars") + (SpinOuts > 0 ? "  ·  police spin-outs " + SpinOuts : "");
        }

        // ---------------------------------------------------------------- frame

        void Update()
        {
            Subscribe();
            if (rider == null) return;
            float dt = Time.deltaTime;
            RiderInput input = RiderInputSource.Read();
            if (!Running || !rider.Running)
            {
                if (Chasing) EndChase();
                Coast(dt);
                return;
            }
            if (dt <= 0f) return;
            if (Chasing)
            {
                if (input.Surrender && !_surrender) _surrender = true;
                rider.HoldBrake = _surrender;
                UpdateStars(dt);
            }
            if (Chasing || CopOnRoad) UpdateCop(dt);
            if (Chasing) UpdateArrest(dt);
        }

        void UpdateArrest(float dt)
        {
            Vector3 rp = rider.transform.position;
            bool boxed = CopOnRoad && Mode == CopMode.Approach && CurrentTactic == Tactic.Alongside && AlongsideNow(rp);
            _boxedIn = boxed ? _boxedIn + dt : Mathf.Max(0f, _boxedIn - 2f * dt);
            _pulledOver = rider.Speed < Tune.pullOverSpeed ? _pulledOver + dt : 0f;
            if (_boxedIn >= Tune.arrestSeconds || _pulledOver >= Tune.arrestSeconds) Arrest();
        }

        bool AlongsideNow(Vector3 rp)
        {
            float dz = Mathf.Abs(_copZ - rp.z), dx = Mathf.Abs(_copX - rp.x);
            return dz < Tune.alongsideZ && dx >= Tune.alongsideX.x && dx <= Tune.alongsideX.y;
        }

        // ---------------------------------------------------------------- the police car

        /// <summary>Scripted moments and tests: a police car turns up this far behind the rider and tries this tactic.</summary>
        public bool SendCop(float behind, Tactic tactic)
        {
            if (!Chasing) return false;
            return SpawnCop(behind, tactic);
        }

        /// <summary>The tactic the next police car will use (tests).</summary>
        public void ForceNextTactic(Tactic tactic) => _forced = tactic;

        Tactic NextTactic()
        {
            if (_forced.HasValue) { Tactic t = _forced.Value; _forced = null; return t; }
            // 1 star: pull alongside first ("pull over!"); from 2 stars it rams first. Then they take turns.
            return ((_attempt + (Wanted >= 2 ? 1 : 0)) & 1) == 0 ? Tactic.Alongside : Tactic.Ram;
        }

        float RoadX(float z) => Mathf.Max(0.5f, (Track != null ? Track.HalfWidthAt(z) : PlaceholderTrack.HalfWidth) - _copHalfWidth - 0.1f);

        bool SpawnCop(float behind, Tactic tactic)
        {
            if (theme == null || theme.policeCar == null || rider == null) return false;
            if (_cop == null)
            {
                _cop = Instantiate(theme.policeCar, transform);
                _cop.name = "PoliceChaseCar";
                _copObstacle = _cop.GetComponent<Obstacle>();
                _copColliders = _cop.GetComponentsInChildren<Collider>(true);
                _copBlinker = _cop.GetComponent<Blinker>();
                var box = _cop.GetComponent<BoxCollider>();
                if (box != null) { _copHalfLength = box.size.z * 0.5f; _copHalfWidth = box.size.x * 0.5f; }
            }
            Vector3 rp = rider.transform.position;
            _copZ = rp.z - behind;
            float roadX = RoadX(_copZ);
            CurrentTactic = tactic;
            _side = rp.x < 0.6f ? 1f : -1f;
            _copX = Mathf.Clamp(tactic == Tactic.Ram ? rp.x : rp.x + _side * Tune.copAlongsideOffset, -roadX, roadX);
            _copSpeed = rider.Speed + Closing();
            _copYaw = _spinRate = 0f;
            _boxedOnce = false;
            _freeFor = 0f;
            SetColliders(true);
            _cop.SetActive(true);
            if (_copBlinker != null) _copBlinker.flashesPerSecond = 5f;
            SetMode(CopMode.Approach);
            PlaceCop(_copX, 0f);
            Actions?.Cue("VEH_Horn_02", _cop.transform.position);
            return true;
        }

        void HideCop()
        {
            if (_cop != null) _cop.SetActive(false);
            Mode = CopMode.None;
        }

        void Despawn(bool comeBack)
        {
            HideCop();
            CopGap = float.PositiveInfinity;
            if (!comeBack) return;
            _respawnAt = Time.time + Mathf.Max(4f, Tune.copRespawnSeconds - Tune.copRespawnPerStar * (Wanted - 1));
            _attempt++;
        }

        void SetMode(CopMode mode)
        {
            Mode = mode;
            _modeTime = 0f;
        }

        void SetColliders(bool on)
        {
            if (_copColliders == null) return;
            foreach (Collider c in _copColliders) if (c != null) c.enabled = on;
        }

        float Closing() => Tune.copClosingSpeed + Tune.copClosingPerStar * (Mathf.Max(1, Wanted) - 1);

        void UpdateCop(float dt)
        {
            Vector3 rp = rider.transform.position;
            if (!CopOnRoad)
            {
                CopGap = float.PositiveInfinity;
                SetCopLevel(Messages.MaxCopLevel);
                if (!Chasing || Time.time < _respawnAt || !SpawnCop(Tune.copSpawnBehind, NextTactic())) return;
            }
            float rs = rider.Speed, closing = Closing();
            float lateral = Mathf.Min(Tune.copLateralSpeed + Tune.copLateralPerStar * (Mathf.Max(1, Wanted) - 1), Tune.lateralMaxSpeed * 0.75f);
            float roadX = RoadX(_copZ);
            float gap = rp.z - (_copZ + _copHalfLength) - rider.radius;
            float edge = (_copX >= rp.x ? 1f : -1f) * roadX; // its own side of the road (never across the rider)
            float targetSpeed = rs + closing, targetX = _copX, accel = 7f, decel = 10f;
            bool pullingOver = _surrender || (rider.Braking && rs < 8f);
            _modeTime += dt;
            switch (Mode)
            {
                case CopMode.Approach:
                    if (pullingOver) { SetMode(CopMode.Escort); break; }
                    targetSpeed += Mathf.Clamp((gap - 30f) * 0.25f, 0f, 14f); // catch up from far behind
                    if (CurrentTactic == Tactic.Ram)
                    {
                        targetX = rp.x;
                        float commit = Mathf.Max(6f, (closing + Tune.copLungeBoost) * Tune.copLungeWarning);
                        bool lined = Mathf.Abs(_copX - rp.x) < _copHalfWidth + rider.radius + 0.3f;
                        if (gap > 0f && gap < commit && lined)
                        {
                            // Committed: no more steering from here, so a sidestep now makes it miss.
                            _lungeSpeed = rs + closing + Tune.copLungeBoost;
                            SetMode(CopMode.Lunge);
                        }
                        else if (gap < commit) SetMode(CopMode.DropBack); // close but not lined up: no side-swipes, try again
                    }
                    else
                    {
                        targetX = rp.x + _side * Tune.copAlongsideOffset;
                        if (Mathf.Abs(Mathf.Clamp(targetX, -roadX, roadX) - rp.x) < Tune.alongsideX.x + 0.3f)
                        {
                            _side = -_side;
                            targetX = rp.x + _side * Tune.copAlongsideOffset;
                        }
                        float dz = rp.z - _copZ;
                        if (dz < 10f) targetSpeed = rs + Mathf.Clamp(dz * 1.6f, -6f, closing);
                        if (AlongsideNow(rp)) { _freeFor = 0f; if (_boxedIn > 0.25f) _boxedOnce = true; }
                        else _freeFor += dt;
                        // Shaken off after being boxed in (braked, boosted, swerved away), or overshot: let the rider go.
                        if ((_boxedOnce && _freeFor > 1.2f) || dz < -8f || _modeTime > 14f) SetMode(CopMode.DropBack);
                    }
                    break;
                case CopMode.Lunge:
                    targetSpeed = _lungeSpeed;
                    accel = 12f;
                    targetX = _copX;
                    if (_copZ - _copHalfLength > rp.z + rider.radius + 0.3f) { SpinOut(); break; } // missed!
                    if (_modeTime > 3f) SetMode(CopMode.DropBack);
                    break;
                case CopMode.SpinOut:
                    targetSpeed = 0f;
                    decel = 14f;
                    targetX = edge;
                    _copYaw += _spinRate * dt;
                    _spinRate = Mathf.MoveTowards(_spinRate, 0f, 420f * dt);
                    if ((_modeTime > 1.2f && gap > 40f) || _modeTime > 6f) { Despawn(Chasing); return; }
                    break;
                case CopMode.DropBack:
                    if (pullingOver) { SetMode(CopMode.Escort); break; }
                    targetSpeed = rs - 6f;
                    targetX = edge;
                    if (gap > 25f)
                    {
                        _attempt++;
                        CurrentTactic = NextTactic();
                        _boxedOnce = false;
                        _freeFor = 0f;
                        _side = rp.x < 0.6f ? 1f : -1f;
                        SetMode(CopMode.Approach);
                    }
                    break;
                case CopMode.Escort:
                {
                    // The rider is pulling over: the police car pulls in beside and a little behind, and waits.
                    targetX = rp.x + _side * Tune.copAlongsideOffset;
                    if (Mathf.Abs(Mathf.Clamp(targetX, -roadX, roadX) - rp.x) < Tune.alongsideX.x + 0.3f) { _side = -_side; targetX = rp.x + _side * Tune.copAlongsideOffset; }
                    float ez = rp.z - 3.5f - _copZ;
                    targetSpeed = Mathf.Max(0f, rs + Mathf.Clamp(ez * 1.5f, -6f, 6f));
                    if (!pullingOver && rs > 9f) SetMode(CopMode.Approach);
                    break;
                }
                case CopMode.Leaving:
                    targetSpeed = rs * 0.5f;
                    targetX = edge;
                    if (gap > 40f || _modeTime > 8f) { Despawn(false); return; }
                    break;
            }
            if (Mode != CopMode.SpinOut && Mode != CopMode.Lunge)
            {
                AvoidTraffic(ref targetSpeed, ref targetX, roadX, rs, rp);
                // Never cut across the rider: changing sides only happens well behind them.
                bool crossing = (_copX >= rp.x) != (targetX >= rp.x);
                if (crossing && rp.z - _copZ < 9f) targetX = _copX;
                // Never run into the rider from behind unless it is a committed ram (with its warning).
                bool lined = Mathf.Abs(_copX - rp.x) < _copHalfWidth + rider.radius + 0.35f;
                if (lined && gap < 5f && gap > -_copHalfLength * 2f) targetSpeed = Mathf.Min(targetSpeed, rs - 2f);
            }
            targetX = Mathf.Clamp(targetX, -roadX, roadX);
            _copSpeed = Mathf.Max(0f, Mathf.MoveTowards(_copSpeed, targetSpeed, (targetSpeed > _copSpeed ? accel : decel) * dt));
            float oldX = _copX;
            _copX = Mathf.MoveTowards(_copX, targetX, lateral * dt);
            _copZ += _copSpeed * dt;
            PlaceCop(oldX, dt);
            if (Mode != CopMode.SpinOut && Mode != CopMode.Leaving) Plough();
            CopGap = rp.z - (_copZ + _copHalfLength) - rider.radius;
            SetCopLevel(Chasing ? Level(CopGap) : Messages.MaxCopLevel);
        }

        static int Level(float gap) => gap <= 0f ? 0 : Mathf.Clamp(Mathf.FloorToInt(gap / 10f), 0, Messages.MaxCopLevel);

        void SetCopLevel(int level)
        {
            if (level == CopLevel) return;
            CopLevel = level;
            if (Chasing) GameEvents.RaiseCopDistance(level);
        }

        void PlaceCop(float oldX, float dt)
        {
            TrackGenerator track = Track;
            float y = track != null ? track.SurfaceHeightAt(_copX, _copZ) : 0f;
            float pitch = track != null ? track.SurfacePitchAt(_copZ) : 0f;
            float steer = dt > 0f ? Mathf.Atan2((_copX - oldX) / dt, Mathf.Max(4f, _copSpeed)) * Mathf.Rad2Deg : 0f;
            _cop.transform.SetPositionAndRotation(new Vector3(_copX, y, _copZ), Quaternion.Euler(pitch, Mode == CopMode.SpinOut ? _copYaw : steer, 0f));
            Mover.MovedThisFrame = true; // TrackGenerator (runs next) syncs physics before the rider sweeps
        }

        void SpinOut()
        {
            SetMode(CopMode.SpinOut);
            SpinOuts++;
            SetColliders(false);
            _spinRate = (_copX >= rider.transform.position.x ? 1f : -1f) * 760f;
            Vector3 at = _cop.transform.position;
            Actions?.Popup(theme != null ? theme.screechPopup : null, at + Vector3.up * 2.4f);
            Actions?.Cue("SCO_Scrape_01", at);
            RiderActions.ShakeCamera(0.15f);
        }

        // Brakes and changes lane for cars, people, barriers and holes ahead; cones and bins it just ploughs through.
        void AvoidTraffic(ref float targetSpeed, ref float targetX, float roadX, float riderSpeed, Vector3 rp)
        {
            float look = Mathf.Max(10f, _copSpeed * 0.9f);
            if (!Blocked(_copX, look)) return;
            for (int i = 0; i < 2; i++)
            {
                float s = (i == 0) == (_copX <= 0f) ? 1f : -1f; // towards the middle of the road first
                float x = _copX + s * 2.6f;
                if (Mathf.Abs(x) > roadX || Blocked(x, look)) continue;
                // Not into the rider's lane while close: it brakes instead.
                if (rp.z - _copZ < 25f && Mathf.Abs(x - rp.x) < _copHalfWidth + rider.radius + 0.5f) continue;
                targetX = x;
                targetSpeed = Mathf.Min(targetSpeed, riderSpeed * 0.9f);
                return;
            }
            targetSpeed = 0f; // nowhere to go: stop and fall behind (that is how the rider loses them in traffic)
        }

        bool Blocked(float x, float look)
        {
            float y = Track != null ? Track.SurfaceHeightAt(x, _copZ) : 0f, drop = look * 0.1f;
            var centre = new Vector3(x, y + 0.9f - drop * 0.5f, _copZ + _copHalfLength + 0.5f + look * 0.5f);
            int n = Physics.OverlapBoxNonAlloc(centre, new Vector3(_copHalfWidth, 0.8f + drop * 0.5f, look * 0.5f), _hits, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                Obstacle o = Solid(_hits[i]);
                if (o != null && !Ploughable(o)) return true;
            }
            return false;
        }

        // Anything the police car's body touches: cones and bins fly, people jump aside, anything else spins it out.
        void Plough()
        {
            Vector3 c = _cop.transform.position + _cop.transform.up * 0.85f;
            int n = Physics.OverlapBoxNonAlloc(c, new Vector3(_copHalfWidth, 0.8f, _copHalfLength), _hits, _cop.transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                Obstacle o = Solid(_hits[i]);
                if (o == null) continue;
                if (Ploughable(o))
                {
                    Knockout k = o.GetComponent<Knockout>();
                    if (k == null) k = o.gameObject.AddComponent<Knockout>();
                    float side = o.transform.position.x >= _copX ? 1f : -1f;
                    k.Launch(new Vector3(side * 6f, 7f, Mathf.Max(8f, _copSpeed * 0.8f)));
                    Actions?.Cue("SCO_Land_01", o.transform.position);
                    continue;
                }
                if (o.kind == ObstacleKind.Person) o.GetComponent<Pedestrian>()?.Startle(_copX);
                SpinOut();
                return;
            }
        }

        Obstacle Solid(Collider c)
        {
            if (c == null) return null;
            Obstacle o = c.GetComponentInParent<Obstacle>();
            if (o == null || o == _copObstacle || !o.gameObject.activeInHierarchy) return null;
            if (rider != null && o.transform.IsChildOf(rider.transform)) return null;
            Knockout k = o.GetComponent<Knockout>();
            return k != null && k.KnockedOut ? null : o;
        }

        static bool Ploughable(Obstacle o) => o.kind == ObstacleKind.Thing && (o.punchable || o.canJumpOver);

        // The ride is over: the police car rolls to a stop where it is.
        void Coast(float dt)
        {
            if (!CopOnRoad || dt <= 0f) return;
            _copSpeed = Mathf.MoveTowards(_copSpeed, 0f, 22f * dt); // brakes hard: the ride is over
            if (Mode == CopMode.SpinOut) { _copYaw += _spinRate * dt; _spinRate = Mathf.MoveTowards(_spinRate, 0f, 420f * dt); }
            _copZ += _copSpeed * dt;
            PlaceCop(_copX, dt);
        }
    }
}
