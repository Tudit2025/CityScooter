using System.Collections.Generic;
using CityScooter.Audio;
using CityScooter.Devices;
using CityScooter.Game;
using CityScooter.Rendering;
using CityScooter.Track;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Districts along the UWA -> Perth CBD route (v0.3), everything except laying the road (TrackGenerator does that):
    /// - the look: fog colour, ambient sky / equator / ground, sun colour and intensity and the sky tint blend from one
    ///   district to the next over the new district's blendMeters, centred on the boundary (smoothstep on the rider's z;
    ///   exactly half way at the boundary). The scene's own look comes back when this is disabled or districts are off.
    /// - "zone:&lt;id&gt;" (GameEvents.ZoneEntered) at the start of a ride and once per boundary crossing, and the HUD
    ///   caption ENTERING &lt;NAME&gt;.
    /// - far scenery (District.farScenery) kept at the horizon around the rider while in its district; at a boundary
    ///   the old one sinks below the horizon and the new one rises, so nothing pops.
    /// - ambience (District.ambience): two AudioSources cross-fade with the look.
    /// - the start screen: "Start: UWA (keys 1-7)"; number keys 1..N choose the start district (teacher demos).
    /// GameController adds it at runtime (no scene changes). Without districts it does nothing at all.
    /// </summary>
    [DefaultExecutionOrder(50)] // after the rider and the track moved this frame
    public sealed class DistrictDirector : MonoBehaviour
    {
        public static DistrictDirector Instance { get; private set; }
        static readonly int SkyHazeId = Shader.PropertyToID("_ComicSkyHaze");
        static readonly int SkyMappingId = Shader.PropertyToID("_ComicSkyMapping");
        static readonly int SkyScaleId = Shader.PropertyToID("_ComicSkyScale");

        public TrackGenerator track;
        public RiderController rider;
        public HudView hud;
        [Tooltip("How far (m) far scenery sinks below the horizon while it fades out at a boundary.")]
        public float scenerySink = 60f;
        [Tooltip("Seconds the ENTERING caption stays on screen.")]
        public float captionSeconds = 2.5f;

        /// <summary>The district the rider is in (null when districts are off).</summary>
        public District Current { get; private set; }
        /// <summary>The look shown now: LookFrom blended towards LookTo by LookBlend (0..1).</summary>
        public District LookFrom { get; private set; }
        public District LookTo { get; private set; }
        public float LookBlend { get; private set; }
        /// <summary>zone: events raised since this component started (tests).</summary>
        public int ZonesRaised { get; private set; }
        /// <summary>True while it has changed RenderSettings / the sun (and will put them back).</summary>
        public bool LookApplied => _applied;

        struct Look
        {
            public Color Fog, FogGlobal, Sky, Equator, Ground, Sun;
            public float SunIntensity;
        }

        Look _baseline;
        bool _applied;
        Light _sun;
        District _lastFrom, _lastTo;
        float _lastBlend = -1f;
        int _zoneSerial = -1;
        GameController _game;
        CityAudioDirector _audio;
        bool _audioSearched;

        // Sky tint: a runtime copy of the panorama sky material (only once some district tints the sky).
        Material _skyOriginal, _skyInstance;
        Color _skyHazeBase;
        Color _tintTarget = Color.white;
        float _tintWeight;

        void OnEnable()
        {
            Instance = this;
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.StartRequested += OnStartRequested;
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
        }

        void OnDisable()
        {
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.StartRequested -= OnStartRequested;
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            if (Instance == this) Instance = null;
            Restore();
            foreach (GameObject go in _scenery.Values) if (go != null) Destroy(go);
            _scenery.Clear();
            foreach (AmbienceVoice v in _voices) if (v.Source != null) v.Source.Stop();
        }

        bool Active => track != null && track.DistrictsOn;
        float RiderZ => rider != null ? rider.transform.position.z : 0f;

        void OnRunStarted()
        {
            if (!Active) return;
            if (!track.TryGetDistrictSpan(RiderZ, out TrackGenerator.DistrictSpan span) || span.District == null) return;
            _zoneSerial = span.Serial;
            Current = span.District;
            ZonesRaised++;
            GameEvents.RaiseZoneEntered(span.District.id);
        }

        void LateUpdate()
        {
            if (!Active)
            {
                if (_applied) Restore();
                Current = null;
                if (hud != null && _shownState != 0) { hud.SetStartLine(null); _shownState = 0; _shownStart = null; }
                FadeAmbience(null, null, 0f);
                return;
            }
            if (_game == null) _game = GetComponent<GameController>();
            float z = RiderZ;
            if (track.TryGetDistrictSpan(z, out TrackGenerator.DistrictSpan span) && span.District != null)
            {
                Current = span.District;
                bool running = rider != null && rider.Running;
                if (running && span.Serial != _zoneSerial)
                {
                    // Crossed a boundary: once per crossing (a reset starts counting again at 0).
                    bool forward = span.Serial > _zoneSerial;
                    _zoneSerial = span.Serial;
                    if (forward)
                    {
                        ZonesRaised++;
                        GameEvents.RaiseZoneEntered(span.District.id);
                        if (hud != null) hud.Caption("ENTERING " + span.District.displayName, captionSeconds);
                    }
                }
                else if (!running) _zoneSerial = span.Serial;
            }

            // Someone put a sky material back (ApplySceneLook re-applies the scene look, e.g. one editor tick after
            // loading): take it as the new original and apply the district's sky to a fresh copy of it.
            if (_skyInstance != null && RenderSettings.skybox != _skyInstance)
            {
                Destroy(_skyInstance);
                _skyInstance = null;
                _lastBlend = -1f;
            }
            Blend(z, out District from, out District to, out float t);
            LookFrom = from;
            LookTo = to;
            LookBlend = t;
            if (from != null)
            {
                if (from != _lastFrom || to != _lastTo || Mathf.Abs(t - _lastBlend) > 0.0001f || !_applied) ApplyLook(from, to, t);
                UpdateScenery(from, to, t, z);
                FadeAmbience(from, to, t);
            }
            HandleStartKeys();
            ApplyBoardStart();
            UpdateStartLine();
        }

        // ---------------- look

        /// <summary>The two districts whose looks meet at world z, and how far from the first to the second (0..1).</summary>
        public void Blend(float z, out District from, out District to, out float t)
        {
            from = to = null;
            t = 0f;
            IReadOnlyList<TrackGenerator.DistrictSpan> spans = track != null ? track.DistrictSpans : null;
            if (spans == null || spans.Count == 0) return;
            int i = spans.Count - 1;
            while (i > 0 && z < spans[i].StartZ) i--;
            from = to = spans[i].District;
            // Near the boundary behind (this district's start) or ahead (the next one's start)?
            if (i > 0)
            {
                float half = Mathf.Max(10f, spans[i].District.blendMeters) * 0.5f;
                if (z < spans[i].StartZ + half)
                {
                    from = spans[i - 1].District;
                    to = spans[i].District;
                    t = Smooth((z - (spans[i].StartZ - half)) / (2f * half));
                    return;
                }
            }
            if (i + 1 < spans.Count)
            {
                float half = Mathf.Max(10f, spans[i + 1].District.blendMeters) * 0.5f;
                if (z > spans[i + 1].StartZ - half)
                {
                    from = spans[i].District;
                    to = spans[i + 1].District;
                    t = Smooth((z - (spans[i + 1].StartZ - half)) / (2f * half));
                }
            }
        }

        static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        Light Sun()
        {
            if (_sun != null) return _sun;
            if (RenderSettings.sun != null && RenderSettings.sun.type == LightType.Directional) return _sun = RenderSettings.sun;
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.isActiveAndEnabled) return _sun = l;
            return null;
        }

        void Capture()
        {
            Light sun = Sun();
            _baseline = new Look
            {
                Fog = RenderSettings.fogColor, FogGlobal = Shader.GetGlobalColor(ApplySceneLook.FogColorId),
                Sky = RenderSettings.ambientSkyColor, Equator = RenderSettings.ambientEquatorColor, Ground = RenderSettings.ambientGroundColor,
                Sun = sun != null ? sun.color : Color.white, SunIntensity = sun != null ? sun.intensity : 1f,
            };
        }

        void ApplyLook(District a, District b, float t)
        {
            if (!_applied) Capture();
            _applied = true;
            _lastFrom = a;
            _lastTo = b;
            _lastBlend = t;
            Color fog = Color.Lerp(a.fogColor, b.fogColor, t);
            fog.a = 1f;
            RenderSettings.fogColor = fog;
            Shader.SetGlobalColor(ApplySceneLook.FogColorId, fog); // alpha 1: valid (sky gradient horizon)
            RenderSettings.ambientSkyColor = Color.Lerp(a.ambientSky, b.ambientSky, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(a.ambientEquator, b.ambientEquator, t);
            RenderSettings.ambientGroundColor = Color.Lerp(a.ambientGround, b.ambientGround, t);
            Light sun = Sun();
            if (sun != null)
            {
                sun.color = Color.Lerp(a.sunColor, b.sunColor, t);
                sun.intensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t);
            }
            ApplySkyTint(Color.Lerp(a.skyTint, b.skyTint, t));
            ApplySkyView(Mathf.LerpAngle(a.skyYaw, b.skyYaw, t), Mathf.Lerp(a.skySize, b.skySize, t), Mathf.Lerp(a.skyHeight, b.skyHeight, t),
                Mathf.Lerp(a.skyline, b.skyline, t));
        }

        // ---------------- sky view: the one Perth panorama turned (which way the road runs) and sized (how far the city is)
        // Yaw goes through FakeTurnDirector.SkyOffset (the sky heading); size and height adjust the panorama globals
        // that ComicCityBackdrop writes every frame, just after it wrote them (see BeforeCamera).
        float _skySize = 1f, _skyHeight, _skyline = 1f;
        float _writtenScale = float.NaN, _writtenElevation = float.NaN;

        /// <summary>District.skyline = 0 puts this row of the panorama on the horizon (0 = bottom, 1 = top): in
        /// Perth_Comic_Panorama.png the tallest tower ends at about 0.61, then there is pale sky with clouds. Sinking further
        /// would bring the deep blue of the top edge down to the horizon.</summary>
        public const float HiddenSkylineRow = 0.64f;

        /// <summary>Sky now (tests): yaw in degrees, size multiplier, height in degrees.</summary>
        public Vector3 SkyView { get; private set; } = new Vector3(0f, 1f, 0f);
        /// <summary>How much of the Perth skyline shows now (tests): 1 all, 0 sunk below the horizon.</summary>
        public float SkylineShown => _skyline;

        void ApplySkyView(float yawDegrees, float size, float heightDegrees, float skyline = 1f)
        {
            SkyView = new Vector3(yawDegrees, size, heightDegrees);
            _skySize = size;
            _skyHeight = heightDegrees;
            _skyline = Mathf.Clamp01(skyline);
            FakeTurnDirector turns = FakeTurnDirector.Instance;
            if (turns != null) turns.SkyOffset = yawDegrees * Mathf.Deg2Rad;
            // The sky material keeps its own _ComicSkyMapping and _ComicSkyScale (Sky_ComicCity.mat), and a material's own
            // value wins over a global: so size, height and skyline go onto the runtime copy of the sky material. (Until
            // 11/9 they only went to the globals, which the sky never read: UWA kept the full-size city.)
            bool neutral = Mathf.Abs(size - 1f) < 0.001f && Mathf.Abs(heightDegrees) < 0.001f && _skyline > 0.999f;
            if (neutral && _skyInstance == null) return;
            if (!EnsureSkyInstance()) return;
            if (_skyInstance.HasProperty(SkyMappingId))
            {
                Vector4 mapping = _skyMappingBase;
                mapping.y = SkyElevation(mapping.y, VerticalSpan(_skyMappingBase, _skyScaleBase), size, heightDegrees, _skyline);
                _skyInstance.SetVector(SkyMappingId, mapping);
            }
            if (_skyInstance.HasProperty(SkyScaleId)) _skyInstance.SetFloat(SkyScaleId, _skyScaleBase * size);
        }

        Vector4 _skyMappingBase;
        float _skyScaleBase = 1f;

        /// <summary>A runtime copy of the panorama sky material, for the district tint and sky view (once one needs it).</summary>
        bool EnsureSkyInstance()
        {
            if (_skyInstance != null) return true;
            Material sky = RenderSettings.skybox;
            if (sky == null || !sky.HasProperty(SkyHazeId)) return false;
            _skyOriginal = sky;
            _skyHazeBase = sky.GetColor(SkyHazeId);
            _skyMappingBase = sky.HasProperty(SkyMappingId) ? sky.GetVector(SkyMappingId) : Vector4.zero;
            _skyScaleBase = sky.HasProperty(SkyScaleId) ? sky.GetFloat(SkyScaleId) : 1f;
            _skyInstance = new Material(sky) { name = sky.name + " (district)" };
            RenderSettings.skybox = _skyInstance;
            return true;
        }

        /// <summary>
        /// The panorama elevation for a district: size scales the skyline about the horizon (elevation 0), not about the
        /// panorama's middle row (_ComicSkyMapping.y, 12 degrees up), so a smaller city still stands on the horizon.
        /// Then height moves it. skyline below 1 sinks it: at 0 the row HiddenSkylineRow (just above the tallest tower) is
        /// on the horizon, whatever the size. verticalSpan: radians the whole panorama spans at size 1 (VerticalSpan).
        /// </summary>
        public static float SkyElevation(float elevation, float verticalSpan, float size, float heightDegrees, float skyline)
        {
            float shown = elevation * size + heightDegrees * Mathf.Deg2Rad;
            float hidden = -(HiddenSkylineRow - 0.5f) * verticalSpan * size;
            return Mathf.Lerp(hidden, shown, Mathf.Clamp01(skyline));
        }

        /// <summary>Radians the panorama spans top to bottom (ComicPanorama.hlsl: PI * mapping.z * apparent scale).</summary>
        public static float VerticalSpan(Vector4 mapping, float scale) => Mathf.PI * Mathf.Max(.1f, mapping.z) * Mathf.Max(.1f, scale);

        void ApplySkySizeAndHeight()
        {
            if (Mathf.Abs(_skySize - 1f) < 0.001f && Mathf.Abs(_skyHeight) < 0.001f && _skyline > 0.999f) return;
            // Only on top of a fresh value from ComicCityBackdrop: if what is there is what we wrote last, it was not
            // rewritten since (no backdrop in this scene), so do not apply it twice.
            float scale = Shader.GetGlobalFloat(SkyScaleId);
            if (scale > 0f && !Mathf.Approximately(scale, _writtenScale))
            {
                _writtenScale = scale * _skySize;
                Shader.SetGlobalFloat(SkyScaleId, _writtenScale);
            }
            Vector4 mapping = Shader.GetGlobalVector(SkyMappingId);
            if (mapping.w > 0.5f && !Mathf.Approximately(mapping.y, _writtenElevation))
            {
                mapping.y = SkyElevation(mapping.y, VerticalSpan(mapping, scale > 0f ? scale : 1f), _skySize, _skyHeight, _skyline);
                // Far facades fade into the panorama pixel behind them (ComicVertex). With the city sunk that pixel is sky
                // and the far ground would turn into blue patches: let them fade into the distance fog instead.
                if (_skyline < 0.5f) mapping.w = 0f;
                _writtenElevation = mapping.y;
                Shader.SetGlobalVector(SkyMappingId, mapping);
            }
        }

        /// <summary>Put the scene's own look back (RenderSettings, sun, sky material).</summary>
        public void Restore()
        {
            if (_skyInstance != null)
            {
                if (RenderSettings.skybox == _skyInstance) RenderSettings.skybox = _skyOriginal;
                Destroy(_skyInstance);
                _skyInstance = null;
                _skyOriginal = null;
            }
            _tintWeight = 0f;
            _tintTarget = Color.white;
            ApplySkyView(0f, 1f, 0f);
            _writtenScale = _writtenElevation = float.NaN;
            if (!_applied) return;
            _applied = false;
            _lastFrom = _lastTo = null;
            _lastBlend = -1f;
            RenderSettings.fogColor = _baseline.Fog;
            Shader.SetGlobalColor(ApplySceneLook.FogColorId, _baseline.FogGlobal);
            RenderSettings.ambientSkyColor = _baseline.Sky;
            RenderSettings.ambientEquatorColor = _baseline.Equator;
            RenderSettings.ambientGroundColor = _baseline.Ground;
            Light sun = Sun();
            if (sun != null)
            {
                sun.color = _baseline.Sun;
                sun.intensity = _baseline.SunIntensity;
            }
        }

        // The comic panorama sky has no colour multiply, only a haze colour it fades towards (_ComicSkyHaze, colour and
        // strength). A tint therefore washes the sky towards the tint colour: white = exactly the authored sky.
        void ApplySkyTint(Color tint)
        {
            float w = Mathf.Clamp01((1f - Mathf.Min(tint.r, Mathf.Min(tint.g, tint.b))) * 1.5f);
            _tintTarget = tint;
            _tintWeight = w;
            if (w <= 0.001f && _skyInstance == null) return;
            if (!EnsureSkyInstance()) return;
            _skyInstance.SetColor(SkyHazeId, TintHaze(_skyHazeBase, tint, w));
        }

        static Color TintHaze(Color baseHaze, Color tint, float w) =>
            Color.Lerp(baseHaze, new Color(tint.r * 0.85f, tint.g * 0.85f, tint.b * 0.85f, 0.4f), w);

        // Far facades fade into the sky with the global haze (ComicCityBackdrop sets it every frame): tint it too.
        void BeforeCamera(ScriptableRenderContext context, Camera cam)
        {
            if (!Active) return;
            if (_applied) ApplySkySizeAndHeight();
            if (_tintWeight <= 0.001f) return;
            Shader.SetGlobalColor(SkyHazeId, TintHaze(Shader.GetGlobalColor(SkyHazeId), _tintTarget, _tintWeight));
        }

        // ---------------- far scenery: at the horizon around the rider, sinks away / rises at a boundary

        readonly Dictionary<District, GameObject> _scenery = new Dictionary<District, GameObject>();
        readonly List<District> _sceneryScratch = new List<District>();

        void UpdateScenery(District from, District to, float t, float z)
        {
            float y = track.SurfaceHeightAt(z);
            _sceneryScratch.Clear();
            foreach (KeyValuePair<District, GameObject> kv in _scenery) _sceneryScratch.Add(kv.Key);
            foreach (District d in _sceneryScratch)
                if (d != from && d != to && _scenery[d] != null && _scenery[d].activeSelf) _scenery[d].SetActive(false);
            PlaceScenery(from, from == to ? 1f : 1f - t, y, z);
            if (to != from) PlaceScenery(to, t, y, z);
        }

        void PlaceScenery(District d, float weight, float y, float z)
        {
            if (d == null || d.farScenery == null) return;
            if (!_scenery.TryGetValue(d, out GameObject go) || go == null)
            {
                go = Instantiate(d.farScenery, transform);
                go.name = "FarScenery_" + d.id;
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false; // decoration only
                _scenery[d] = go;
            }
            bool show = weight > 0.001f;
            if (go.activeSelf != show) go.SetActive(show);
            if (show) go.transform.SetPositionAndRotation(new Vector3(0f, y - (1f - weight) * scenerySink, z), Quaternion.identity);
        }

        /// <summary>The far scenery instance of a district, if one was made (tests).</summary>
        public GameObject SceneryOf(District d) => d != null && _scenery.TryGetValue(d, out GameObject go) ? go : null;

        // ---------------- ambience: two sources cross-fade with the look

        sealed class AmbienceVoice
        {
            public AudioSource Source;
            public float Level;
        }

        readonly List<AmbienceVoice> _voices = new List<AmbienceVoice>();

        void FadeAmbience(District from, District to, float t)
        {
            AudioClip a = from != null ? from.ambience : null, b = to != null ? to.ambience : null;
            if (a == null && b == null && _voices.Count == 0) return;
            bool running = rider != null && rider.Running;
            float gain = running ? AudioGain() : 0f;
            float wantA = a != null ? (from == to ? 1f : 1f - t) * from.ambienceVolume : 0f;
            float wantB = b != null && to != from ? t * to.ambienceVolume : 0f;
            if (a != null && a == b) { wantA += wantB; wantB = 0f; b = null; }
            while (_voices.Count < 2 && (a != null || b != null))
            {
                var go = new GameObject("DistrictAmbience" + _voices.Count);
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.loop = true;
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _voices.Add(new AmbienceVoice { Source = src });
            }
            float k = 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime);
            foreach (AmbienceVoice v in _voices)
            {
                float want = v.Source.clip != null && v.Source.clip == a ? wantA : v.Source.clip != null && v.Source.clip == b ? wantB : 0f;
                v.Level = Mathf.Lerp(v.Level, want, k);
                if (v.Level < 0.002f && want <= 0f) { v.Level = 0f; if (v.Source.isPlaying) v.Source.Stop(); }
            }
            Assign(a, wantA);
            Assign(b, wantB);
            foreach (AmbienceVoice v in _voices)
            {
                v.Source.volume = v.Level * gain;
                if (v.Level > 0.002f && gain > 0f && !v.Source.isPlaying && v.Source.clip != null) v.Source.Play();
            }
        }

        void Assign(AudioClip clip, float want)
        {
            if (clip == null || want <= 0f) return;
            foreach (AmbienceVoice v in _voices) if (v.Source.clip == clip) return;
            foreach (AmbienceVoice v in _voices)
                if (v.Level <= 0f) { v.Source.Stop(); v.Source.clip = clip; return; }
        }

        float AudioGain()
        {
            if (!_audioSearched) { _audioSearched = true; _audio = FindAnyObjectByType<CityAudioDirector>(); }
            if (_audio == null) return 1f;
            return _audio.Muted ? 0f : _audio.Master * _audio.Ambience;
        }

        // ---------------- start screen: "Start: UWA (keys 1-7)"

        District _shownStart;
        int _shownState = -1;

        // Game keys allowed? Tomorrow's class build turns the keyboard off (micro:bit only), and the web shell turns it
        // off while students type: then the number keys do nothing and the line does not mention them.
        static bool KeysAllowed
        {
            get
            {
                DeviceHub hub = DeviceHub.Instance;
                return hub == null || hub.KeyboardEnabled;
            }
        }

        void UpdateStartLine()
        {
            if (hud == null) return;
            District start = track.StartDistrict;
            int count = track.Route != null ? track.Route.Count : 0;
            bool ready = _game == null || _game.State == RunState.Ready;
            // One district alone: nothing to choose, the start screen stays as it was.
            bool show = ready && start != null && count > 1;
            int state = !show ? 0 : KeysAllowed ? 1000 + count : 1;
            if (start == _shownStart && state == _shownState) return;
            _shownStart = start;
            _shownState = state;
            hud.SetStartLine(!show ? null : "Start: " + start.displayName + (KeysAllowed ? "  (keys 1-" + Mathf.Min(9, count) + ")" : ""));
        }

        void HandleStartKeys()
        {
            if (_game == null || _game.State != RunState.Ready || track.Route == null || track.Route.Count < 2) return;
            if (!KeysAllowed) return;
            Keyboard k = Keyboard.current;
            if (k == null) return;
            int n = Mathf.Min(9, track.Route.Count);
            for (int i = 0; i < n; i++)
                if (k[Key.Digit1 + i].wasPressedThisFrame || k[Key.Numpad1 + i].wasPressedThisFrame)
                {
                    SelectStartDistrict(i);
                    return;
                }
        }

        // ---------------- "start:kingspark" from a board (block "start rides in [Kings Park]")

        /// <summary>The start screen is rebuilt at most this often for boards' start: messages.</summary>
        public const float BoardStartRebuildSeconds = 0.3f;
        bool _boardStartDirty;
        float _boardStartRebuiltAt = -10f;

        void OnStartRequested(string deviceId, string districtId) => RequestStart(districtId);

        /// <summary>
        /// A board's "start:&lt;id&gt;": rides start in this district from now on. On the start screen it shows there
        /// within a frame (rebuilt at most every BoardStartRebuildSeconds); during a ride or on the crash screen the ride
        /// carries on and the next one starts there. The blocks send it again every second, so the district already
        /// chosen does nothing. False for an id that is not on the route (or a route of one district).
        /// </summary>
        public bool RequestStart(string districtId)
        {
            if (track == null || track.Route == null || track.Route.Count < 2) return false;
            int index = DistrictRoute.IndexOf(track.Route, districtId);
            if (index < 0) return false;
            string id = track.Route[index].id;
            track.startDistrictOverride = id; // StartRun builds the next ride from it
            if (_game == null) _game = GetComponent<GameController>();
            bool ready = _game == null || _game.State == RunState.Ready;
            if (ready && track.StartDistrict != track.Route[index]) _boardStartDirty = true;
            return true;
        }

        void ApplyBoardStart()
        {
            if (!_boardStartDirty) return;
            if (_game != null && _game.State != RunState.Ready) { _boardStartDirty = false; return; } // StartRun rebuilt it
            if (Time.unscaledTime - _boardStartRebuiltAt < BoardStartRebuildSeconds) return;
            _boardStartDirty = false;
            _boardStartRebuiltAt = Time.unscaledTime;
            int want = DistrictRoute.IndexOf(track.Route, track.startDistrictOverride);
            if (want < 0 || track.StartDistrict == track.Route[want]) return; // e.g. the number keys got there first
            if (_game != null) _game.EnterReady();
            else track.ResetTrack(rider != null ? rider.transform.position.z - 2f : 0f);
        }

        /// <summary>Start screen: the next ride starts in this district (index in route order). False when there is none.</summary>
        public bool SelectStartDistrict(int routeIndex)
        {
            if (track == null || track.Route == null || routeIndex < 0 || routeIndex >= track.Route.Count) return false;
            track.startDistrictOverride = track.Route[routeIndex].id;
            if (_game == null) _game = GetComponent<GameController>();
            if (_game != null && _game.State == RunState.Ready) _game.EnterReady();
            else if (_game == null) track.ResetTrack(rider != null ? rider.transform.position.z - 2f : 0f);
            UpdateStartLine();
            return true;
        }
    }
}
