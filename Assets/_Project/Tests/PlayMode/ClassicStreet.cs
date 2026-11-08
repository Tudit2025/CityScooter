using CityScooter.Gameplay;
using UnityEngine;

namespace CityScooter.Tests
{
    /// <summary>
    /// v0.3: tests written for the comic street ride it as it was before districts. The real theme lists the districts
    /// (UWA first), so these tests give the game a copy of its tuning with districts off and rebuild the track.
    /// </summary>
    public static class ClassicStreet
    {
        public static GameTuning Use(GameController game)
        {
            GameTuning street = Object.Instantiate(game.tuning != null ? game.tuning : GameTuning.Default);
            street.name = "Tuning (classic street)";
            street.districts = false;
            game.tuning = street;
            if (game.rider != null) game.rider.tuning = street;
            if (game.track != null) game.track.tuning = street;
            FakeTurnDirector turns = game.GetComponent<FakeTurnDirector>();
            if (turns != null) turns.tuning = street;
            game.EnterReady();
            return street;
        }
    }
}
