using System.Collections.Generic;
using CityScooter.Game;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// The WA police helicopter (heliMinStars and up). It hovers ahead of and above the rider, side on so "WA POLICE"
    /// faces the camera.
    ///   Nets: a target ring appears on the road where the rider will be netWarnSeconds later (never less than 0.8 s);
    ///   its outer ring closes in while a comic net drops on a rope and spreads out. Caught inside = ARRESTED ("NETTED!",
    ///   the net lies over the rider); a sidestep or braking = the net lands on the road (decoration, no collider).
    ///   Sniper (sniperMinStars and up): a red dot slides after the rider (slower than the rider can move sideways),
    ///   locks and blinks for sniperLockSeconds, then BANG!: a bright streak from the helicopter to the dot. Hit =
    ///   BUSTED (a cartoon tumble off the scooter); a miss = a puff of dust on the road.
    /// Fair by design: it never aims at a spot the rider cannot leave (both sides blocked), and waits while the police
    /// car is close (one danger at a time). Created by PoliceChase. Numbers: GameTuning "WA police helicopter".
    /// </summary>
    [DefaultExecutionOrder(10)] // after the rider has moved this frame: the red dot and the helicopter keep up with it
    public sealed class PoliceHelicopter : MonoBehaviour
    {
        public PoliceChase chase;

        /// <summary>True from arriving ("heli:1") until standing down ("heli:0").</summary>
        public bool InAir { get; private set; }
        public Transform Body => _heli != null && _heli.activeSelf ? _heli.transform : null;
        public int NetsDropped { get; private set; }
        public int SniperShots { get; private set; }
        /// <summary>Nets that landed on the road and shots that hit the road instead of the rider.</summary>
        public int Misses { get; private set; }
        public int PendingNets => _shots.Count;
        /// <summary>Missed nets lying on the road right now (decoration).</summary>
        public int NetsOnRoad { get { int n = 0; foreach (GameObject g in _netsDown) if (g != null && g.activeSelf) n++; return n; } }
        public bool SniperTracking => _sniper == SniperState.Tracking;
        public bool SniperLocked => _sniper == SniperState.Locked;
        /// <summary>World x of the red dot (tests).</summary>
        public float SniperX => _dotX;
        /// <summary>The shot's streak, while it shows (tests, previews).</summary>
        public bool TracerShowing => _tracer != null && _tracer.activeSelf;

        sealed class Shot
        {
            public GameObject Ring, Net, Rope;
            public Transform Pulse;
            public Vector3 At, From;
            public float Born, Warn;
        }

        enum SniperState { Off, Tracking, Locked }

        const float NetFlight = 0.55f, NetTop = 1.94f, TracerSeconds = 0.16f; // NetTop: where the rope ties on (HazardKitBuilder.PoliceNet)
        readonly List<Shot> _shots = new List<Shot>();
        readonly Stack<GameObject> _rings = new Stack<GameObject>(), _nets = new Stack<GameObject>(), _ropes = new Stack<GameObject>();
        readonly List<GameObject> _netsDown = new List<GameObject>();
        readonly Collider[] _hits = new Collider[16];
        GameObject _heli, _dot, _dotBlink, _tracer;
        Transform _rotor, _tailRotor, _muzzle;
        Vector3 _offset;
        bool _leaving;
        float _nextNet, _nextSniper, _sniperTime, _dotX, _tracerUntil;
        SniperState _sniper;
        RiderActions _actions;

        RiderController Rider => chase != null ? chase.rider : null;
        TrackTheme Theme => chase != null ? chase.theme : null;
        GameTuning Tune => Rider != null && Rider.tuning != null ? Rider.tuning : GameTuning.Default;
        TrackGenerator Track => Rider != null ? Rider.track : null;
        RiderActions Actions { get { if (_actions == null) _actions = GetComponent<RiderActions>(); return _actions; } }
        static bool Running => GameController.Instance != null && GameController.Instance.State == RunState.Running;
        float Ground(float x, float z) => Track != null ? Track.SurfaceHeightAt(x, z) : 0f;
        float Pitch(float z) => Track != null ? Track.SurfacePitchAt(z) : 0f;
        Vector3 Muzzle => _muzzle != null && _muzzle.gameObject.activeInHierarchy ? _muzzle.position : Body != null ? Body.position : Vector3.zero;

        void OnEnable() => GameEvents.RunStarted += OnRunStarted;
        void OnDisable() => GameEvents.RunStarted -= OnRunStarted;

        void OnRunStarted()
        {
            InAir = _leaving = false;
            if (_heli != null) _heli.SetActive(false);
            ClearShots();
            StopSniper(false);
            foreach (GameObject g in _netsDown) if (g != null) g.SetActive(false);
            if (_tracer != null) _tracer.SetActive(false);
            NetsDropped = SniperShots = Misses = 0;
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
            UpdateDecoration(running);
            if (!InAir || !running || dt <= 0f) { if (_sniper != SniperState.Off) StopSniper(false); return; }
            int stars = chase.Wanted;
            if (!ThreatClear()) _nextNet = Mathf.Max(_nextNet, Time.time + 0.5f);
            if (Time.time >= _nextNet && _sniper != SniperState.Locked)
            {
                float interval = Mathf.Max(1.6f, Tune.netInterval - Tune.netIntervalPerStar * (stars - Tune.heliMinStars));
                _nextNet = Time.time + (DropNets(stars >= 5 ? Tune.netsAtFiveStars : 1) ? interval : 0.4f);
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
                _muzzle = art != null ? art.Find("Muzzle") : null;
            }
            InAir = true;
            _leaving = false;
            if (!_heli.activeSelf) _offset = new Vector3(18f, 22f, 70f); // flies in from ahead, high on the right
            _heli.SetActive(true);
            _nextNet = Time.time + Tune.heliFirstShotSeconds;
            _nextSniper = Time.time + Tune.heliFirstShotSeconds + 1.5f;
            GameEvents.RaiseHeliChanged(true);
            GameController game = GameController.Instance;
            if (game != null && game.hud != null) game.hud.Flash("POLICE HELICOPTER!", "Dodge the nets and the red dot");
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

        // ---------------------------------------------------------------- nets

        /// <summary>
        /// Drop nets now (tests and the normal cadence): one ring where the rider will be netWarnSeconds later, a second
        /// one beside it if count is 2 (the other side stays open). False when the rider would have nowhere to go.
        /// </summary>
        public bool DropNets(int count = 1)
        {
            RiderController rider = Rider;
            if (!InAir || rider == null || !Running) return false;
            float warn = Mathf.Max(0.8f, Tune.netWarnSeconds), r = Tune.netRadius;
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
            NetsDropped++;
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
                if (s.Net == null && age >= s.Warn - NetFlight)
                {
                    s.Net = Take(_nets, Theme != null ? Theme.policeNet : null);
                    s.Rope = Take(_ropes, Theme != null ? Theme.netRope : null);
                    s.From = Body != null ? Body.position + Vector3.down * 1.2f : s.At + new Vector3(0f, 9f, 6f);
                }
                if (s.Net != null)
                {
                    // Bunched up under the helicopter, it spreads out as it falls and lands as a dome over the ring.
                    float p = Mathf.Clamp01((age - (s.Warn - NetFlight)) / NetFlight);
                    Vector3 pos = Vector3.Lerp(s.From, s.At, p * p) + Vector3.up * (Mathf.Sin(p * Mathf.PI) * 0.8f);
                    float spread = Mathf.Lerp(0.3f, 1f, Mathf.Sqrt(p)), tall = Mathf.Lerp(0.35f, 1f, p); // a bundle that opens up
                    s.Net.transform.SetPositionAndRotation(pos, Quaternion.Euler(Pitch(s.At.z), age * 140f, 0f));
                    s.Net.transform.localScale = new Vector3(spread, tall, spread);
                    if (s.Rope != null)
                    {
                        if (Body != null) Stretch(s.Rope, Body.position + Vector3.down * 0.4f, pos + Vector3.up * (NetTop * tall));
                        else s.Rope.SetActive(false);
                    }
                }
                if (age < s.Warn) continue;
                _shots.RemoveAt(i);
                _landed.Add(s);
            }
            // Resolved after the loop: a catch ends the chase, and standing down clears _shots (5 stars: two nets can
            // land in the same frame). Once the ride is over, the other nets just land on the road.
            for (int i = 0; i < _landed.Count; i++) Land(_landed[i], running && Running && Rider.Running);
            _landed.Clear();
        }

        readonly List<Shot> _landed = new List<Shot>();

        void Land(Shot s, bool running)
        {
            Give(_rings, s.Ring);
            Give(_nets, s.Net);
            Give(_ropes, s.Rope);
            RiderController rider = Rider;
            bool hit = false;
            if (running && rider != null)
            {
                Vector3 rp = rider.transform.position;
                float r = Tune.netRadius;
                // Generous along the road (the rider covers metres per frame), strict across it: sidestepping is the dodge.
                hit = Mathf.Abs(rp.x - s.At.x) < r + rider.radius * 0.5f && Mathf.Abs(rp.z - s.At.z) < r + 1.5f;
            }
            if (hit) { chase.NetArrest(s.At); return; } // RiderActions lays the net over the rider
            if (running) Misses++;
            LayNet(s.At);
        }

        // A net that missed stays on the road as decoration (no collider) until the rider has left it behind.
        void LayNet(Vector3 at)
        {
            GameObject net = null;
            foreach (GameObject g in _netsDown) if (g != null && !g.activeSelf) { net = g; break; }
            if (net == null)
            {
                if (Theme == null || Theme.netOnRoad == null) return;
                net = Instantiate(Theme.netOnRoad, transform);
                net.name = Theme.netOnRoad.name;
                _netsDown.Add(net);
            }
            net.transform.SetPositionAndRotation(new Vector3(at.x, Ground(at.x, at.z) + 0.02f, at.z), Quaternion.Euler(Pitch(at.z), Random.Range(0f, 360f), 0f));
            net.SetActive(true);
        }

        void UpdateDecoration(bool running)
        {
            if (_tracer != null && _tracer.activeSelf && Time.time > _tracerUntil) _tracer.SetActive(false);
            if (!running || Rider == null) return;
            float behind = Rider.transform.position.z - 30f;
            foreach (GameObject g in _netsDown) if (g != null && g.activeSelf && g.transform.position.z < behind) g.SetActive(false);
        }

        void ClearShots()
        {
            foreach (Shot s in _shots) { Give(_rings, s.Ring); Give(_nets, s.Net); Give(_ropes, s.Rope); }
            _shots.Clear();
        }

        // A one-metre piece along +Z (rope, tracer) stretched from a to b.
        static void Stretch(GameObject piece, Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            if (d.sqrMagnitude < 1e-4f) { piece.SetActive(false); return; }
            piece.SetActive(true);
            piece.transform.SetPositionAndRotation(a, Quaternion.LookRotation(d));
            piece.transform.localScale = new Vector3(1f, 1f, d.magnitude);
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
            go.transform.localScale = Vector3.one;
            pool.Push(go);
        }

        // ---------------------------------------------------------------- the sniper's red dot

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

        // BANG! at the helicopter, a bright streak to the dot for a moment; the rider is hit only if still on the dot.
        void Fire(Vector3 rp)
        {
            SniperShots++;
            RiderController rider = Rider;
            float x = _dotX;
            bool hit = Mathf.Abs(rp.x - x) < Tune.sniperRadius + rider.radius;
            Vector3 spot = new Vector3(x, Ground(x, rp.z + 0.2f) + 0.05f, rp.z + 0.2f);
            Vector3 muzzle = Muzzle;
            Actions?.Popup(Theme != null ? Theme.bangPopup : null, muzzle + new Vector3(0f, 0.4f, -1.5f)); // a little towards the camera, in front of the cabin
            Actions?.Cue("SCO_Land_02", muzzle);
            if (_tracer == null && Theme != null && Theme.sniperTracer != null)
            {
                _tracer = Instantiate(Theme.sniperTracer, transform);
                _tracer.name = Theme.sniperTracer.name;
            }
            if (_tracer != null)
            {
                Stretch(_tracer, muzzle, hit ? rp + Vector3.up * 1.2f : spot);
                _tracerUntil = Time.time + TracerSeconds;
            }
            StopSniper(true);
            if (hit) { chase.SniperBust(rp); return; }
            Misses++;
            Actions?.Popup(Theme != null ? Theme.dustPopup : null, spot + Vector3.up * 0.35f);
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
