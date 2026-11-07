using CityScooter.Track;
using UnityEngine;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// What one scene/skin is built from. Leave lists empty and the generator uses grey-box placeholders,
    /// so gameplay works before art exists.
    /// </summary>
    [CreateAssetMenu(menuName = "CityScooter/Track Theme", fileName = "Theme_")]
    public sealed class TrackTheme : ScriptableObject
    {
        [Tooltip("Road chunk prefabs (root has RoadChunk).")]
        public RoadChunk[] chunks;

        [Tooltip("Cycle chunks in array order to preserve authored near / open / near vista rhythm.")]
        public bool orderedChunks;

        [Tooltip("Single-lane obstacle prefabs (root has Obstacle).")]
        public Obstacle[] obstacles;

        [Tooltip("Road-wide obstacles you must jump (Obstacle.spansRoad), e.g. railings.")]
        public Obstacle[] jumpObstacles;

        [Tooltip("Pickup prefabs (root has Gift).")]
        public Gift[] gifts;
        [Tooltip("Hand-made stretches (roadworks, police stop, traffic) placed instead of random obstacles.")]
        public HazardPattern[] hazards;
        [Header("Comic pop-ups (optional)")]
        public GameObject punchPopup;
        public GameObject bellPopup;
        public GameObject startlePopup;
        public GameObject sorryPopup;
        public GameObject boingPopup;
        public GameObject crashPopup;
        public GameObject bonkPopup;
        public GameObject boostPopup;
        [Tooltip("Shown when the bell has been rung too often and nobody listens.")]
        public GameObject ignorePopup;
        [Tooltip("Comic motion lines when a punch swings, and the ink burst where it lands.")]
        public GameObject swishPopup;
        public GameObject impactPopup;
        [Tooltip("The rider's punching arm and the riding model without that arm (PunchArm).")]
        public GameObject punchArm;
        [Header("Street life and crashes (optional)")]
        [Tooltip("People the generator puts on the sidewalks of every chunk (root has Pedestrian).")]
        public GameObject[] pedestrians;
        [Tooltip("Thrown off the scooter in a crash (CrashTumble).")]
        public GameObject crashRider;
        public GameObject crashScooter;

        [Header("Boulevard sections (optional; when they appear is in GameTuning)")]
        [Tooltip("Widens the street into the 4-lane boulevard (RoadChunk.endHalfWidth).")]
        public RoadChunk boulevardIn;
        [Tooltip("Full-width 4-lane chunks, used in turn between the In and Out chunks.")]
        public RoadChunk[] boulevardChunks;
        [Tooltip("Narrows the boulevard back to the normal street.")]
        public RoadChunk boulevardOut;
        [Tooltip("Hazard patterns allowed on the full-width boulevard chunks. Street patterns (roadHalfWidth 4) move their " +
                 "sidewalk entries out with the kerb. Empty: the normal hazards list is used.")]
        public HazardPattern[] boulevardHazards;
        [Header("WA police: wanted level, chase, helicopter (optional; PoliceChase)")]
        [Tooltip("Drives after the rider while the wanted level is 1 or more (the parked police car prefab is fine).")]
        public GameObject policeCar;
        [Tooltip("A police officer (Obstacle.police, kind Person) for scripted moments and tests; patterns place their own.")]
        public GameObject policeOfficer;
        [Tooltip("The cap that flies off when an officer is punched with cartoon mode off.")]
        public GameObject policeCap;
        [Tooltip("The police helicopter (decoration only: no colliders).")]
        public GameObject policeHeli;
        [Tooltip("What the helicopter drops: one prefab, swap it to change the look (comic paint pellet).")]
        public GameObject paintPellet;
        [Tooltip("The paint left on the road (and on the rider when it hits).")]
        public GameObject paintSplat;
        [Tooltip("The ring on the road where a pellet will land; its Art/Pulse child closes in until it lands.")]
        public GameObject targetRing;
        [Tooltip("The sniper's red dot on the road; its Art/Blink child blinks when it locks.")]
        public GameObject sniperDot;
        public GameObject oiPopup;
        public GameObject splatPopup;
        public GameObject screechPopup;

        public bool HasChunks => chunks != null && chunks.Length > 0;
        public bool HasBoulevard => boulevardIn != null && boulevardOut != null && boulevardChunks != null
                                    && boulevardChunks.Length > 0 && System.Array.TrueForAll(boulevardChunks, c => c != null);
    }
}
