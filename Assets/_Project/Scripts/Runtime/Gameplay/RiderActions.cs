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
    /// Created by GameController, so no scene changes.
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
        static bool _keyToon;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { CartoonMode = false; _keyToon = false; }

        /// <summary>For tests and the HUD.</summary>
        public int Punches { get; private set; }
        public int Bounced { get; private set; }
        public int Hits { get; private set; }
        public int Bells { get; private set; }

        float _nextPunch;
        int _turn;
        readonly Collider[] _hits = new Collider[32];
        readonly Dictionary<int, float> _warned = new Dictionary<int, float>();
        readonly Dictionary<GameObject, Stack<ComicPopup>> _popups = new Dictionary<GameObject, Stack<ComicPopup>>();
        readonly List<ComicPopup> _live = new List<ComicPopup>();
        CityAudioDirector _audio;
        int _horn;

        // Reuses the comic city's sound bank (no new clips yet): thud for a punch, click for the bell, horns for cars.
        void Cue(string id, Vector3? position = null)
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
        }

        void OnRunStarted()
        {
            _warned.Clear();
            _turn = 0;
            _nextPunch = 0f;
            ClearCrash();
        }

        /// <summary>Keyboard T, or a test: flip cartoon mode for this session.</summary>
        public static void ToggleCartoonKey() => _keyToon = !_keyToon;

        void Update()
        {
            Subscribe();
            DeviceHub hub = DeviceHub.Instance;
            if ((hub == null || hub.KeyboardEnabled) && KeyboardControls.ToonTogglePressed()) ToggleCartoonKey();
            CartoonMode = _keyToon || (hub != null && hub.Mixer.Toon);
            if (rider == null || !rider.Running) return;
            RiderInput input = RiderInputSource.Read();
            if (input.Punch) Punch();
            if (input.Bell) Bell();
            Warn();
            FakeTurnDirector turns = FakeTurnDirector.Instance;
            int dir = turns != null ? turns.Direction : 0;
            if (dir != _turn) { _turn = dir; GameEvents.RaiseTurnChanged(dir); }
        }

        public bool Punch()
        {
            if (Time.time < _nextPunch) return false;
            _nextPunch = Time.time + punchCooldown;
            Punches++;
            Vector3 p = rider.transform.position;
            float reach = Mathf.Max(punchReach, rider.Speed * punchReachSeconds);
            float drop = reach * MaxGrade; // the street runs downhill: things ahead sit lower
            var centre = new Vector3(p.x, p.y + 0.9f - drop * 0.5f, p.z + reach * 0.5f);
            int n = Physics.OverlapBoxNonAlloc(centre, new Vector3(punchHalfWidth, 1.2f + drop * 0.5f, reach * 0.5f + 0.5f), _hits, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Obstacle best = null;
            float bestDz = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Obstacle o = _hits[i] != null ? _hits[i].GetComponentInParent<Obstacle>() : null;
                if (o == null || !(o.punchable || (CartoonMode && o.kind == ObstacleKind.Person)) || !o.gameObject.activeInHierarchy) continue;
                Knockout k = o.GetComponent<Knockout>();
                if (k != null && k.KnockedOut) continue;
                float dz = o.transform.position.z - p.z;
                if (dz < -1f || dz >= bestDz) continue;
                bestDz = dz;
                best = o;
            }
            if (best == null) return false;
            Knockout ko = best.GetComponent<Knockout>();
            if (ko == null) ko = best.gameObject.AddComponent<Knockout>();
            float side = best.transform.position.x >= p.x ? 1f : -1f;
            ko.Launch(best.kind == ObstacleKind.Person ? new Vector3(side * 6f, 12f, Mathf.Max(6f, rider.Speed * 0.6f)) : new Vector3(side * 7f, 8f, Mathf.Max(6f, rider.Speed * 0.8f)));
            Hits++;
            Cue("SCO_Land_02", best.transform.position);
            Popup(theme != null ? theme.punchPopup : null, best.transform.position + Vector3.up * 1.4f);
            GameEvents.RaisePunchLanded();
            return true;
        }

        public void Bell()
        {
            Bells++;
            Vector3 p = rider.transform.position;
            Cue("UI_Click");
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
                if (_warned.TryGetValue(id, out float when) && Time.time - when < 8f) continue;
                _warned[id] = Time.time;
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
            rider.Bump(0.65f);
            Bounced++;
            Cue("SCO_Land_01", o.transform.position);
            Popup(theme != null ? theme.boingPopup : null, o.transform.position + Vector3.up * 2.2f);
            ShakeCamera(0.25f);
            GameEvents.RaisePersonBounced();
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
            else Popup(theme != null ? theme.crashPopup : null, rider.transform.position + new Vector3(0f, 2.4f, 1f));
            ThrowRider(o);
        }

        // The rider leaves the scooter in a way that matches what was hit.
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
            Mover mover = o.GetComponent<Mover>();
            Quaternion lying = Quaternion.Euler(90f, 0f, 0f), onSide = Quaternion.Euler(0f, 0f, 90f * side);
            switch (o.kind)
            {
                case ObstacleKind.Vehicle when mover != null && mover.speed < 0f:
                    // Oncoming car: thrown back and high, the scooter skids back too.
                    _crashRider.Throw(at + Vector3.up * 0.4f, facing, new Vector3(side * 3f, 11f, -Mathf.Clamp(v * 0.25f + 12f, 12f, 30f)), new Vector3(-540f, 90f * side, 0f), lying, 0.15f);
                    _crashScooter.Throw(at, facing, new Vector3(side * 2f, 4f, -10f), new Vector3(0f, 200f * side, 300f * side), onSide, 0.2f);
                    ShakeCamera(0.9f);
                    break;
                case ObstacleKind.Vehicle:
                    // Into a parked or slower car: over the bonnet, the scooter stops dead.
                    _crashRider.Throw(at + Vector3.up * 0.6f, facing, new Vector3(side * 1.5f, 9f, Mathf.Clamp(v * 0.35f, 5f, 14f)), new Vector3(600f, 0f, 60f * side), lying, 0.15f);
                    _crashScooter.Throw(at, facing, new Vector3(0f, 2f, -2f), new Vector3(-120f, 0f, 160f * side), onSide, 0.2f);
                    ShakeCamera(0.7f);
                    break;
                case ObstacleKind.Hole:
                    // Front wheel into the open manhole: the scooter noses down and sticks, the rider goes over the bars.
                    _crashScooter.Stick(new Vector3(o.transform.position.x, at.y - 0.25f, o.transform.position.z - 0.35f), facing * Quaternion.Euler(38f, 0f, 0f));
                    _crashRider.Throw(at + Vector3.up * 0.7f, facing, new Vector3(side * 0.8f, 7.5f, Mathf.Clamp(v * 0.45f, 6f, 16f)), new Vector3(480f, 0f, 0f), lying, 0.15f);
                    ShakeCamera(0.5f);
                    break;
                case ObstacleKind.Person:
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
            if (_hiddenModel != null) _hiddenModel.SetActive(true);
            _hiddenModel = null;
        }

        static void ShakeCamera(float strength)
        {
            Camera cam = Camera.main;
            FollowCamera follow = cam != null ? cam.GetComponent<FollowCamera>() : null;
            if (follow != null) follow.Shake(strength);
        }

        void Popup(GameObject prefab, Vector3 position, Transform follow = null)
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
