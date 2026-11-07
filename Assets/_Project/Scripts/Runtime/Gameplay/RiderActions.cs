using System.Collections.Generic;
using CityScooter.Audio;
using CityScooter.Controls;
using CityScooter.Game;
using CityScooter.Protocol;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Protocol v1 on the rider: punch ("pu" / F) knocks a punchable prop out of the way and sends "ko"; the bell
    /// ("bl" / R) makes people ahead step aside; "warn:car|ped|hole" for things in the rider's lane ahead (each
    /// thing once); "turn:-1|0|1" when the fake road turns. Created by GameController, so no scene changes.
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

        /// <summary>For tests and the HUD.</summary>
        public int Punches { get; private set; }
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
            if (_subscribed != null) _subscribed.HitObstacle -= OnHit;
            _subscribed = rider;
            if (rider != null) rider.HitObstacle += OnHit;
        }

        void OnDisable()
        {
            Pedestrian.Startled -= OnStartled;
            GameEvents.RunStarted -= OnRunStarted;
            if (_subscribed != null) _subscribed.HitObstacle -= OnHit;
            _subscribed = null;
        }

        void OnRunStarted()
        {
            _warned.Clear();
            _turn = 0;
            _nextPunch = 0f;
        }

        void Update()
        {
            Subscribe();
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
                if (o == null || !o.punchable || !o.gameObject.activeInHierarchy) continue;
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
            ko.Launch(new Vector3(side * 7f, 8f, Mathf.Max(6f, rider.Speed * 0.8f)));
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

        void OnHit(Obstacle o)
        {
            if (o != null && o.kind == ObstacleKind.Person) Popup(theme != null ? theme.sorryPopup : null, o.transform.position + Vector3.up * 2.3f);
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
