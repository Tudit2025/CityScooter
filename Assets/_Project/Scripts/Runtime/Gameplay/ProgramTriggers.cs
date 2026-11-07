using System.Collections.Generic;
using CityScooter.Game;
using CityScooter.Protocol;
using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Students' programs trigger the game's own events with "do:..." (protocol v1): start a police chase, set the
    /// wanted stars, call the helicopter, order a turn, send traffic or roadworks. Example rule a student can build:
    /// count "ko" with a variable and after 3 punches send "do:chase". At most one trigger a second per board; the
    /// game only does it during a ride, places hazards out of sight ahead (never on a boulevard transition), and the
    /// usual messages (chase:1, wanted:N, warn:car, turn:1 ...) follow as for any other event.
    /// </summary>
    public sealed class ProgramTriggers : MonoBehaviour
    {
        public RiderController rider;
        public TrackGenerator track;
        public float perBoardCooldown = 1f;

        /// <summary>For tests: triggers carried out, and ones dropped (too fast, not riding, nowhere to put it).</summary>
        public int Done { get; private set; }
        public int Dropped { get; private set; }

        readonly Dictionary<string, float> _next = new Dictionary<string, float>();

        // Trigger name -> hazard pattern asset (theme.hazards) it places ahead.
        static readonly Dictionary<string, string> PatternFor = new Dictionary<string, string>
        {
            [Messages.DoCar] = "HZ_OncomingCar", [Messages.DoSlowCar] = "HZ_SlowCar", [Messages.DoRoadworks] = "HZ_RoadworksShort",
            [Messages.DoPolice] = "HZ_PoliceStop", [Messages.DoCrossing] = "HZ_Crossing", [Messages.DoManhole] = "HZ_Manhole",
        };

        void OnEnable() => GameEvents.TriggerRequested += OnTrigger;
        void OnDisable() => GameEvents.TriggerRequested -= OnTrigger;

        void OnTrigger(string deviceId, string name, int value)
        {
            string key = deviceId ?? "?";
            if (rider == null || !rider.Running || (_next.TryGetValue(key, out float next) && Time.time < next)) { Dropped++; return; }
            _next[key] = Time.time + perBoardCooldown;
            string label = Do(name, value);
            if (label == null) { Dropped++; return; }
            Done++;
            GameController game = GameController.Instance;
            if (game != null && game.hud != null) game.hud.Caption((deviceId == "sim" ? "Simulator" : deviceId) + " → " + label);
        }

        // Returns what happened, for the HUD, or null if it could not be done now.
        string Do(string name, int value)
        {
            PoliceChase police = PoliceChase.Instance;
            switch (name)
            {
                case Messages.DoChase:
                    if (police == null) return null;
                    police.SetWanted(Mathf.Max(police.Wanted, 1));
                    return "POLICE CHASE!";
                case Messages.DoStars:
                    if (police == null) return null;
                    police.SetWanted(value);
                    return value > 0 ? "WANTED " + new string('★', Mathf.Clamp(value, 0, 5)) : "police give up";
                case Messages.DoHeli:
                    if (police == null) return null;
                    police.SetWanted(Mathf.Max(police.Wanted, 3));
                    return "POLICE HELICOPTER!";
                case Messages.DoCalm:
                    if (police == null) return null;
                    police.SetWanted(0);
                    return "police give up";
                case Messages.DoTurn:
                    FakeTurnDirector turns = FakeTurnDirector.Instance;
                    if (turns == null || value == 0) return null;
                    float speed = Mathf.Max(10f, rider.Speed);
                    bool ok = turns.RequestTurn(rider.transform.position.z + Mathf.Max(150f, speed * 4f), value, 0.9f, Mathf.Max(160f, speed * 6f));
                    return ok ? (value < 0 ? "turn LEFT ahead" : "turn RIGHT ahead") : null;
                default:
                    if (!PatternFor.TryGetValue(name, out string pattern) || track == null || track.theme == null || track.theme.hazards == null) return null;
                    HazardPattern p = System.Array.Find(track.theme.hazards, h => h != null && h.name == pattern);
                    if (p == null) return null;
                    float ahead = Mathf.Min(Mathf.Max(120f, rider.Speed * 3.5f), track.buildAhead - p.length - 12f);
                    return track.TryPlacePatternAhead(p, ahead) ? Label(name) : null;
            }
        }

        static string Label(string name)
        {
            switch (name)
            {
                case Messages.DoCar: return "oncoming car!";
                case Messages.DoSlowCar: return "slow car ahead";
                case Messages.DoRoadworks: return "ROADWORKS ahead";
                case Messages.DoPolice: return "police stop ahead";
                case Messages.DoCrossing: return "people crossing ahead";
                default: return "open manhole ahead";
            }
        }
    }
}
