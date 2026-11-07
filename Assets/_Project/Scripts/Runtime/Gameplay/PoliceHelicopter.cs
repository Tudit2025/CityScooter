using System.Collections.Generic;
using CityScooter.Game;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// The WA police helicopter (heliMinStars and up). It hovers ahead of and above the rider, side on so "WA POLICE"
    /// faces the camera, and drops comic paint pellets: a target ring appears on the road where the rider will be
    /// pelletWarnSeconds later (never less than 0.8 s); its outer ring closes in and the pellet lands. Inside = BUSTED,
    /// a sidestep or braking = a harmless splat on the road. From sniperMinStars a red dot slides after the rider
    /// (slower than the rider can move sideways), locks and blinks for sniperLockSeconds, then fires.
    /// Fair by design: it never fires at a spot the rider cannot leave (both sides blocked), and waits while the
    /// police car is close (one danger at a time). Created by PoliceChase. Numbers: GameTuning "WA police helicopter".
    /// </summary>
    [DefaultExecutionOrder(10)] // after the rider has moved this frame: the red dot and the helicopter keep up with it
    public sealed class PoliceHelicopter : MonoBehaviour
    {
        public PoliceChase chase;

        /// <summary>True from arriving ("heli:1") until standing down ("heli:0").</summary>
        public bool InAir { get; private set; }
        public Transform Body => _heli != null && _heli.activeSelf ? _heli.transform : null;
        public int PelletsDropped { get; private set; }
        public int SniperShots { get; private set; }
        public int Misses { get; private set; }
        public int PendingPellets => _shots.Count;
        public bool SniperTracking => _sniper == SniperState.Tracking;
        public bool SniperLocked => _sniper == SniperState.Locked;
        /// <summary>World x of the red dot (tests).</summary>
        public float SniperX => _dotX;

        sealed class Shot
        {
            public GameObject Ring, Pellet;
            public Transform Pulse;
            public Vector3 At, From;
            public float Born, Warn;
        }

        enum SniperState { Off, Tracking, Locked }

        const float PelletFlight = 0.45f;
        readonly List<Shot> _shots = new List<Shot>();
        readonly Stack<GameObject> _rings = new Stack<GameObject>(), _pellets = new Stack<GameObject>();
        readonly List<GameObject> _splats = new List<GameObject>();
        readonly List<float> _splatUntil = new List<float>();
        readonly Collider[] _hits = new Collider[16];
        GameObject _heli, _dot, _dotBlink;
        Transform _rotor, _tailRotor;
        Vector3 _offset;
        bool _leaving;
        float _nextPellet, _nextSniper, _sniperTime, _dotX, _lockZ;
        SniperState _sniper;

        RiderController Rider => chase != null ? chase.rider : null;
        TrackTheme Theme => chase != null ? chase.theme : null;
        GameTuning Tune => Rider != null && Rider.tuning != null ? Rider.tuning : GameTuning.Default;
        TrackGenerator Track => Rider != null ? Rider.track : null;
        static bool Running => GameController.Instance != null && GameController.Instance.State == RunState.Running;
        float Ground(float x, float z) => Track != null ? Track.SurfaceHeightAt(x, z) : 0f;
        float Pitch(float z) => Track != null ? Track.SurfacePitchAt(z) : 0f;

        void OnEnable() => GameEvents.RunStarted += OnRunStarted;
        void OnDisable() => GameEvents.RunStarted -= OnRunStarted;

        void OnRunStarted()
        {
            InAir = _leaving = false;
            if (_heli != null) _heli.SetActive(false);
            ClearShots();
            StopSniper(false);
            for (int i = 0; i < _splats.Count; i++) if (_splats[i] != null) _splats[i].SetActive(false);
            PelletsDropped = SniperShots = Misses = 0;
        }

        void Update()
        {
            RiderController rider = Rider;
            if (rider == null) return;
            float dt = Time.deltaTime;
            bool running = Running && rider.Running;
            bool want = running && chase.Chasing && chase.Wanted >= Tune.heliMinStars;
            if (want && !InAir) Arrive();
            else if (!want && InAir) StandDown();
            Fly(dt);
            UpdateShots(running);
            UpdateSplats();
            if (!InAir || !running || dt <= 0f) { if (_sniper != SniperState.Off) StopSniper(false); return; }
            int stars = chase.Wanted;
            if (!ThreatClear()) _nextPellet = Mathf.Max(_nextPellet, Time.time + 0.5f);
            if (Time.time >= _nextPellet && _sniper != SniperState.Locked)
            {
                float interval = Mathf.Max(1.6f, Tune.pelletInterval - Tune.pelletIntervalPerStar * (stars - Tune.heliMinStars));
                _nextPellet = Time.time + (DropPellets(stars >= 5 ? Tune.pelletsAtFiveStars : 1) ? interval : 0.4f);
            }
            if (stars >= Tune.sniperMinStars) UpdateSniper(dt, stars);
            else if (_sniper != SniperState.Off) StopSniper(true);
        }

        // One danger at a time: no new shots while the police car is right behind or beside the rider.
        bool ThreatClear() => !(chase.CopOnRoad && chase.CopGap < 15f && chase.Mode != PoliceChase.CopMode.SpinOut && chase.Mode != PoliceChase.CopMode.Leaving);

        void Arrive()
        {
            if (Theme == null || Theme.policeHeli == null) return;
            if (_heli == null)
            {
                _heli = Instantiate(Theme.policeHeli, transform);
                _heli.name = "PoliceHelicopter";
                Transform art = _heli.transform.Find("Art");
                _rotor = art != null ? art.Find("Rotor") : null;
                _tailRotor = art != null ? art.Find("TailRotor") : null;
            }
            InAir = true;
            _leaving = false;
            if (!_heli.activeSelf) _offset = new Vector3(18f, 22f, 70f); // flies in from ahead, high on the right
            _heli.SetActive(true);
            _nextPellet = Time.time + Tune.heliFirstShotSeconds;
            _nextSniper = Time.time + Tune.heliFirstShotSeconds + 1.5f;
            GameEvents.RaiseHeliChanged(true);
            GameController game = GameController.Instance;
            if (game != null && game.hud != null) game.hud.Flash("POLICE HELICOPTER!", "Watch the road for paint targets");
        }

        /// <summary>Fly away (fewer than heliMinStars, chase over, ride over); pending rings vanish. Sends "heli:0".</summary>
        public void StandDown()
        {
            if (!InAir) return;
            InAir = false;
            _leaving = true;
            ClearShots();
            StopSniper(false);
            GameEvents.RaiseHeliChanged(false);
        }

        void Fly(float dt)
        {
            if (_heli == null || !_heli.activeSelf) return;
            Vector3 rp = Rider.transform.position;
            float t = Time.time;
            float z = rp.z + Tune.heliAhead, x = Mathf.Sin(t * 0.45f) * 2.5f + rp.x * 0.3f;
            float y = Ground(0f, z) + Tune.heliHeight + Mathf.Sin(t * 1.3f) * 0.3f;
            Vector3 target = _leaving ? new Vector3(25f, 30f, 90f) : Vector3.zero;
            _offset = Vector3.Lerp(_offset, target, 1f - Mathf.Exp(-(_leaving ? 0.9f : 1.4f) * Mathf.Max(0f, dt)));
            _heli.transform.SetPositionAndRotation(new Vector3(x, y, z) + _offset, Quaternion.Euler(5f, 62f, Mathf.Cos(t * 0.45f) * 6f));
            if (_rotor != null) _rotor.localRotation = Quaternion.Euler(0f, t * 1100f % 360f, 0f);
            if (_tailRotor != null) _tailRotor.localRotation = Quaternion.Euler(t * 1700f % 360f, 0f, 0f);
            if (_leaving && (_offset - target).sqrMagnitude < 25f) { _heli.SetActive(false); _leaving = false; }
        }

        // ---------------------------------------------------------------- paint pellets

        /// <summary>
        /// Drop pellets now (tests and the normal cadence): one ring where the rider will be pelletWarnSeconds later,
        /// a second one beside it if count is 2 (the other side stays open). False when the rider would have nowhere to go.
        /// </summary>
        public bool DropPellets(int count = 1)
        {
            RiderController rider = Rider;
            if (!InAir || rider == null || !Running) return false;
            float warn = Mathf.Max(0.8f, Tune.pelletWarnSeconds), r = Tune.pelletRadius;
            Vector3 rp = rider.transform.position;
            float x = rp.x, z = rp.z + rider.Speed * warn;
            int open = OpenSides(x, z, r);
            if (open == 0) return false;
            Spawn(x, z, warn);
            GameEvents.RaiseIncoming(0);
            if (count >= 2)
            {
                // The second ring goes on a side whose other side stays open: there is always a way out.
                float s = open == 3 ? (x >= 0f ? 1f : -1f) : open == 1 ? 1f : -1f;
                float x2 = x + s * (2f * r + 0.5f);
                float half = Track != null ? Track.RideableHalfWidthAt(z) - 0.3f : 4f;
                if (Mathf.Abs(x2) <= half)
                {
                    Spawn(x2, z, warn);
                    GameEvents.RaiseIncoming(s > 0f ? 1 : -1);
                }
            }
            PelletsDropped++;
            return true;
        }

        // Bit 1: the rider can get out of a circle of radius r at (x, z) to the left; bit 2: to the right.
        int OpenSides(float x, float z, float r)
        {
            RiderController rider = Rider;
            float half = (Track != null ? Track.RideableHalfWidthAt(z) : PlaceholderTrack.HalfWidth) - rider.radius - 0.1f;
            int open = 0;
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1f : 1f, ex = x + s * (r + rider.radius + 0.6f);
                if (Mathf.Abs(ex) <= half && Free(ex, z)) open |= i == 0 ? 1 : 2;
            }
            return open;
        }

        // Nothing solid beside the target in the last metres before it (parked cars, barriers, the police car).
        bool Free(float x, float z)
        {
            var centre = new Vector3(x, Ground(x, z) + 0.9f, z - 1f);
            int n = Physics.OverlapBoxNonAlloc(centre, new Vector3(0.6f, 0.9f, 4f), _hits, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                Collider c = _hits[i];
                if (c == null || c.transform.IsChildOf(Rider.transform)) continue;
                Obstacle o = c.GetComponentInParent<Obstacle>();
                if (o == null || !o.gameObject.activeInHierarchy || o.canJumpOver) continue;
                Knockout k = o.GetComponent<Knockout>();
                if (k != null && k.KnockedOut) continue;
                return false;
            }
            return true;
        }

        void Spawn(float x, float z, float warn)
        {
            var shot = new Shot { At = new Vector3(x, Ground(x, z) + 0.03f, z), Born = Time.time, Warn = warn };
            shot.Ring = Take(_rings, Theme != null ? Theme.targetRing : null);
            if (shot.Ring != null)
            {
                shot.Ring.transform.SetPositionAndRotation(shot.At, Quaternion.Euler(Pitch(z), 0f, 0f));
                Transform art = shot.Ring.transform.Find("Art");
                shot.Pulse = art != null ? art.Find("Pulse") : null;
                if (shot.Pulse != null) shot.Pulse.localScale = Vector3.one * 2.4f;
            }
            _shots.Add(shot);
        }

        void UpdateShots(bool running)
        {
            for (int i = _shots.Count - 1; i >= 0; i--)
            {
                Shot s = _shots[i];
                float age = Time.time - s.Born, k = Mathf.Clamp01(age / s.Warn);
                if (s.Pulse != null) s.Pulse.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, k);
                if (s.Pellet == null && age >= s.Warn - PelletFlight)
                {
                    s.Pellet = Take(_pellets, Theme != null ? Theme.paintPellet : null);
                    s.From = Body != null ? Body.position + Vector3.down * 0.6f : s.At + new Vector3(0f, 9f, 6f);
                }
                if (s.Pellet != null)
                {
                    float p = Mathf.Clamp01((age - (s.Warn - PelletFlight)) / PelletFlight);
                    s.Pellet.transform.position = Vector3.Lerp(s.From, s.At + Vector3.up * 0.25f, p * p) + Vector3.up * (Mathf.Sin(p * Mathf.PI) * 1.2f);
                    s.Pellet.transform.rotation = Quaternion.Euler(age * 500f, age * 300f, 0f);
                }
                if (age < s.Warn) continue;
                _shots.RemoveAt(i);
                Land(s, running);
            }
        }

        void Land(Shot s, bool running)
        {
            Give(_rings, s.Ring);
            Give(_pellets, s.Pellet);
            Splat(s.At);
            RiderController rider = Rider;
            if (!running || rider == null) return;
            Vector3 rp = rider.transform.position;
            float r = Tune.pelletRadius;
            // Generous along the road (the rider covers metres per frame), strict across it: sidestepping is the dodge.
            bool hit = Mathf.Abs(rp.x - s.At.x) < r + rider.radius * 0.5f && Mathf.Abs(rp.z - s.At.z) < r + 1.5f;
            if (hit) chase.PaintBust(s.At);
            else Misses++;
        }

        void ClearShots()
        {
            foreach (Shot s in _shots) { Give(_rings, s.Ring); Give(_pellets, s.Pellet); }
            _shots.Clear();
        }

        void Splat(Vector3 at)
        {
            GameObject splat = null;
            for (int i = 0; i < _splats.Count; i++)
                if (_splats[i] != null && !_splats[i].activeSelf) { splat = _splats[i]; _splatUntil[i] = Time.time + 2.5f; break; }
            if (splat == null)
            {
                if (Theme == null || Theme.paintSplat == null) return;
                splat = Instantiate(Theme.paintSplat, transform);
                splat.name = Theme.paintSplat.name;
                _splats.Add(splat);
                _splatUntil.Add(Time.time + 2.5f);
            }
            splat.transform.SetPositionAndRotation(new Vector3(at.x, Ground(at.x, at.z) + 0.035f, at.z), Quaternion.Euler(Pitch(at.z), Random.Range(0f, 360f), 0f));
            splat.SetActive(true);
        }

        void UpdateSplats()
        {
            for (int i = 0; i < _splats.Count; i++)
                if (_splats[i] != null && _splats[i].activeSelf && Time.time > _splatUntil[i] && Running) _splats[i].SetActive(false);
        }

        GameObject Take(Stack<GameObject> pool, GameObject prefab)
        {
            while (pool.Count > 0)
            {
                GameObject go = pool.Pop();
                if (go == null) continue;
                go.SetActive(true);
                return go;
            }
            if (prefab == null) return null;
            GameObject made = Instantiate(prefab, transform);
            made.name = prefab.name;
            return made;
        }

        static void Give(Stack<GameObject> pool, GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            pool.Push(go);
        }

        // ---------------------------------------------------------------- the red dot (sniper)

        /// <summary>Start the red dot now (tests; normally every sniperInterval seconds from sniperMinStars).</summary>
        public bool StartSniper()
        {
            RiderController rider = Rider;
            if (!InAir || rider == null || _sniper != SniperState.Off) return false;
            if (_dot == null)
            {
                if (Theme == null || Theme.sniperDot == null) return false;
                _dot = Instantiate(Theme.sniperDot, transform);
                _dot.name = Theme.sniperDot.name;
                Transform art = _dot.transform.Find("Art");
                Transform blink = art != null ? art.Find("Blink") : null;
                _dotBlink = blink != null ? blink.gameObject : null;
            }
            Vector3 rp = rider.transform.position;
            float half = (Track != null ? Track.RideableHalfWidthAt(rp.z) : PlaceholderTrack.HalfWidth) - 0.5f;
            // It starts off to one side and slides in, so you see it coming.
            _dotX = Mathf.Clamp(rp.x + (rp.x > 0f ? -3f : 3f), -half, half);
            _sniper = SniperState.Tracking;
            _sniperTime = 0f;
            _dot.SetActive(true);
            if (_dotBlink != null) _dotBlink.SetActive(true);
            PlaceDot(rp.z);
            return true;
        }

        void UpdateSniper(float dt, int stars)
        {
            RiderController rider = Rider;
            Vector3 rp = rider.transform.position;
            switch (_sniper)
            {
                case SniperState.Off:
                    if (Time.time >= _nextSniper && _shots.Count == 0 && ThreatClear()) StartSniper();
                    break;
                case SniperState.Tracking:
                {
                    _dotX = Mathf.MoveTowards(_dotX, rp.x, Mathf.Min(Tune.sniperFollowSpeed, Tune.lateralMaxSpeed * 0.75f) * dt);
                    _sniperTime += dt;
                    PlaceDot(rp.z);
                    float track = Tune.sniperTrackSeconds * (stars >= 5 ? 0.75f : 1f);
                    if (_sniperTime < track) break;
                    float lockSeconds = LockSeconds(stars);
                    bool onTarget = Mathf.Abs(_dotX - rp.x) < 1f;
                    if (onTarget && _shots.Count == 0 && ThreatClear() && OpenSides(_dotX, rp.z + rider.Speed * lockSeconds * 0.5f, Tune.sniperRadius) != 0) Lock(rp);
                    else if (_sniperTime > track + 2f) StopSniper(true);
                    break;
                }
                case SniperState.Locked:
                    _sniperTime += dt;
                    PlaceDot(rp.z);
                    if (_dotBlink != null) _dotBlink.SetActive(((int)(Time.time * 12f) & 1) == 0);
                    if (_sniperTime >= LockSeconds(stars)) Fire(rp);
                    break;
            }
        }

        float LockSeconds(int stars) => Mathf.Max(0.4f, stars >= 5 ? Tune.sniperLockAtFiveStars : Tune.sniperLockSeconds);

        /// <summary>Lock the red dot where it is now (tests); it fires LockSeconds later.</summary>
        public bool LockSniper()
        {
            if (_sniper != SniperState.Tracking || Rider == null) return false;
            Lock(Rider.transform.position);
            return true;
        }

        void Lock(Vector3 rp)
        {
            _sniper = SniperState.Locked;
            _sniperTime = 0f;
            float dx = _dotX - rp.x;
            GameEvents.RaiseIncoming(Mathf.Abs(dx) < Tune.sniperRadius + Rider.radius ? 0 : dx < 0f ? -1 : 1);
        }

        void Fire(Vector3 rp)
        {
            SniperShots++;
            _lockZ = rp.z;
            Splat(new Vector3(_dotX, 0f, _lockZ));
            bool hit = Mathf.Abs(rp.x - _dotX) < Tune.sniperRadius + Rider.radius;
            float x = _dotX;
            StopSniper(true);
            if (hit) chase.PaintBust(new Vector3(x, rp.y, rp.z));
            else Misses++;
        }

        void StopSniper(bool cooldown)
        {
            _sniper = SniperState.Off;
            if (_dot != null) _dot.SetActive(false);
            if (cooldown) _nextSniper = Time.time + Mathf.Max(2.5f, Tune.sniperInterval - 1.5f * (chase.Wanted - Tune.sniperMinStars));
        }

        // Rides along with the rider (the sniper leads the target): only a sidestep gets you out.
        void PlaceDot(float z)
        {
            if (_dot == null) return;
            z += 0.2f;
            _dot.transform.SetPositionAndRotation(new Vector3(_dotX, Ground(_dotX, z) + 0.04f, z), Quaternion.Euler(Pitch(z), 0f, 0f));
        }
    }
}
