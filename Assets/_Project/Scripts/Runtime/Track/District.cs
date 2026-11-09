using UnityEngine;

namespace CityScooter.Track
{
    /// <summary>
    /// One part of the ride along the real UWA -> Perth CBD route (v0.3), e.g. UWA campus, Kings Park, Elizabeth Quay.
    /// The theme lists districts in route order (<see cref="order"/>); the ride starts in the first one, goes through
    /// them in order and loops. Each district lasts <see cref="length"/> metres of road, then the next one starts.
    ///
    /// Road contract (so any district can follow any other): every chunk in <see cref="chunks"/>, <see cref="entry"/>
    /// and <see cref="exit"/> STARTS and ENDS with the standard street cross-section: road halfWidth 4, sidewalkWidth 4,
    /// sidewalkHeight 0.2, curbTransitionWidth 0.3. In between, a chunk may change width (endHalfWidth) as long as the
    /// district's own chunk order brings it back. Lengths are multiples of 10 m (40 m is standard). Slope: endHeight
    /// about -3.1471 per 40 m (the standard 4.5 degree downhill); steeper (Kings Park descent) or flatter (riverside)
    /// is fine, keep it between -8 and 0 per 40 m.
    ///
    /// Transitions: the generator lays the old district's <see cref="exit"/> chunk, then the new district's
    /// <see cref="entry"/> chunk, and blends the look (fog, light, sky tint) over <see cref="blendMeters"/> around the
    /// boundary. Entry/exit chunks should calm down towards their outer end (lower buildings, trees, hedges, low walls)
    /// so the meeting point of two districts never shows a hard cut. Empty lists fall back to the theme's own.
    /// </summary>
    [CreateAssetMenu(menuName = "CityScooter/District", fileName = "District_")]
    public sealed class District : ScriptableObject
    {
        [Tooltip("Short id sent to the micro:bit as zone:<id> (lower case letters only), e.g. uwa, matilda, kingspark.")]
        public string id = "street";
        [Tooltip("Shown on the HUD and on the gateway sign, e.g. UWA, KINGS PARK, PERTH CBD (capitals, A-Z and spaces).")]
        public string displayName = "STREET";
        [Tooltip("Position on the route loop (lower first). UWA 10, Matilda Bay 20, Kings Park 30, West Perth 40, " +
                 "Perth CBD 50, Elizabeth Quay 60, Mounts Bay Road 70.")]
        public int order;
        [Tooltip("Metres of road this district lasts (random between x and y each time it comes round).")]
        public Vector2 length = new Vector2(450f, 650f);

        [Header("Road")]
        [Tooltip("The district's chunks (root has RoadChunk). Laid in array order when orderedChunks, else weighted random.")]
        public RoadChunk[] chunks;
        public bool orderedChunks = true;
        [Tooltip("First chunk of the district (after the previous district's exit chunk). Optional.")]
        public RoadChunk entry;
        [Tooltip("Last chunk of the district (before the next district's entry chunk). Optional.")]
        public RoadChunk exit;
        [Tooltip("Whether the 4-lane boulevard sections may appear here (only city streets).")]
        public bool allowBoulevard;

        [Header("What appears on the road (empty = the theme's own)")]
        public Obstacle[] obstacles;
        public Obstacle[] jumpObstacles;
        public Gift[] gifts;
        public HazardPattern[] hazards;
        [Tooltip("People and animals on the sidewalks (root has Pedestrian), e.g. students, joggers, peacocks.")]
        public GameObject[] pedestrians;
        [Tooltip("Ramps that may be placed on the road here (root has Ramp). Empty = the theme's generic ramps.")]
        public Ramp[] ramps;
        [Tooltip("Relative amount of ramps here (0 = none, 1 = normal, 2 = twice as many).")]
        [Min(0f)] public float rampFrequency = 1f;

        [Header("Look (blended over blendMeters at each boundary)")]
        [Min(10f)] public float blendMeters = 150f;
        public Color fogColor = new Color(0.957f, 0.890f, 0.773f);
        [Tooltip("Multiplies the comic panorama sky colour (white = unchanged).")]
        public Color skyTint = Color.white;
        public Color ambientSky = new Color(0.66f, 0.80f, 0.92f);
        public Color ambientEquator = new Color(0.86f, 0.83f, 0.75f);
        public Color ambientGround = new Color(0.61f, 0.55f, 0.45f);
        public Color sunColor = new Color(1f, 0.95f, 0.86f);
        [Range(0f, 3f)] public float sunIntensity = 1.35f;

        [Header("Sky panorama (one 360-degree Perth picture, turned and sized per district; blended like the look)")]
        [Tooltip("Turns the panorama so it matches which way this road really runs (degrees, + = right). 0 = the CBD " +
                 "skyline ahead-left with the river on the right, as seen from Kings Park; 180 = the city behind you " +
                 "(Mounts Bay Rd, riding west along the river).")]
        [Range(-180f, 180f)] public float skyYaw;
        [Tooltip("Apparent size of the skyline: below 1 = further from the city (UWA), above 1 = closer (CBD). It grows " +
                 "or shrinks from the horizon, so the city stays standing on it.")]
        [Range(0.5f, 1.6f)] public float skySize = 1f;
        [Tooltip("Moves the skyline up (+) or down (-), in degrees: down when you look over the city from a hill.")]
        [Range(-10f, 10f)] public float skyHeight;
        [Tooltip("How much of the Perth CBD skyline shows: 1 = all of it, 0 = none (it sinks below the horizon). 0 for " +
                 "districts far from the city (the south line from Canning Bridge): their farScenery is the horizon. " +
                 "Blends across a boundary, so the city sets behind you as you ride away.")]
        [Range(0f, 1f)] public float skyline = 1f;

        [Header("Scenery and sound (optional)")]
        [Tooltip("Far scenery that follows the rider at the horizon while in this district (no colliders), e.g. the river " +
                 "and the far shore, the Kings Park bush line. Pivot on the road centre line at the rider's height.")]
        public GameObject farScenery;
        [Tooltip("Placed across the road where the district starts (decoration, no colliders). Empty = a generic sign " +
                 "with the display name.")]
        public GameObject gateway;
        [Tooltip("Looping background sound (birds, river, traffic), cross-faded at the boundary.")]
        public AudioClip ambience;
        [Range(0f, 1f)] public float ambienceVolume = 0.35f;

        public bool HasChunks => chunks != null && chunks.Length > 0 && System.Array.TrueForAll(chunks, c => c != null);
    }
}
