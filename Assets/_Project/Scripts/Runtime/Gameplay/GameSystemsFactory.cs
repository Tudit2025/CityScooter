using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Builds the gameplay hierarchy (GameController + HUD, Track, Rider) with references wired.
    /// The editor uses it to create the GameSystems prefab; tests use it to spin up a game.
    /// Safe in edit mode: nothing visual is created until play mode (Awake builds placeholders and HUD).
    /// </summary>
    public static class GameSystemsFactory
    {
        public static GameObject Create(GameTuning tuning = null, TrackTheme theme = null)
        {
            var root = new GameObject("GameSystems");
            root.SetActive(false); // wire everything before any Awake/OnEnable runs

            var trackGo = new GameObject("Track");
            trackGo.transform.SetParent(root.transform, false);
            var track = trackGo.AddComponent<TrackGenerator>();

            var riderGo = new GameObject("Rider");
            riderGo.transform.SetParent(root.transform, false);
            var rider = riderGo.AddComponent<RiderController>();

            var game = root.AddComponent<GameController>();
            var hud = root.AddComponent<HudView>();

            track.tuning = tuning;
            track.theme = theme;
            track.rider = riderGo.transform;
            rider.tuning = tuning;
            rider.track = track;
            game.tuning = tuning;
            game.rider = rider;
            game.track = track;
            game.hud = hud;
            hud.game = game;

            root.SetActive(true);
            return root;
        }
    }
}
