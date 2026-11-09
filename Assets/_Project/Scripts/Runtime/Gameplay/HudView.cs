using CityScooter.Devices;
using UnityEngine;
using UnityEngine.UI;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// v0 HUD built in code (uGUI + the built-in LegacyRuntime font) so it works before the UI art exists:
    /// score, distance and gifts, a 10-step speed bar (the same 0..9 the micro:bit gets), what is
    /// controlling the scooter, and Ready / Crash messages. Student-facing text is English.
    /// WA police: wanted stars at the top (drawn sprites, so no font glyphs are needed in the Web build), a "POLICE"
    /// marker at the bottom that follows the police car left / right and turns red when it is about to ram or has
    /// you boxed in, and short flash titles ("WANTED!", "LOST THEM!").
    /// The tech artist can replace this with a designed prefab later.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        public GameController game;

        Text _score;
        Text _stats;
        Text _device;
        Text _centre;
        Text _hint;
        Image[] _speedSegments;
        Font _font;
        Image[] _stars;
        Text _wantedLabel, _police, _flash, _flashSub;
        Text _say;
        float _sayUntil;
        float _flashUntil, _starPulse;
        int _shownWanted;

        static readonly Color SegmentOff = new Color(1f, 1f, 1f, 0.18f);
        static readonly Color SegmentOn = new Color(1f, 0.82f, 0.2f, 0.95f);

        void Awake() => Build();

        void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform root = canvasGo.GetComponent<RectTransform>();

            _score = MakeText(root, "Score", new Vector2(0f, 1f), new Vector2(40f, -30f), 64, TextAnchor.UpperLeft, FontStyle.Bold);
            _stats = MakeText(root, "Stats", new Vector2(0f, 1f), new Vector2(42f, -110f), 30, TextAnchor.UpperLeft, FontStyle.Normal);
            _device = MakeText(root, "Device", new Vector2(1f, 1f), new Vector2(-40f, -90f), 28, TextAnchor.UpperRight, FontStyle.Normal);
            _centre = MakeText(root, "Centre", new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), 56, TextAnchor.MiddleCenter, FontStyle.Bold);
            _hint = MakeText(root, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, 40f), 28, TextAnchor.LowerCenter, FontStyle.Normal);
            _say = MakeText(root, "Say", new Vector2(0.5f, 0f), new Vector2(0f, 150f), 44, TextAnchor.LowerCenter, FontStyle.Bold);
            _say.color = new Color(1f, 0.85f, 0.2f);
            Game.GameEvents.BoardSaid += OnBoardSaid;
            ShowHint();

            _speedSegments = new Image[10];
            for (int i = 0; i < _speedSegments.Length; i++)
            {
                var seg = new GameObject("Speed" + i, typeof(RectTransform), typeof(Image));
                var rt = seg.GetComponent<RectTransform>();
                rt.SetParent(root, false);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
                rt.sizeDelta = new Vector2(26f, 18f + i * 4f);
                rt.anchoredPosition = new Vector2(-40f - (9 - i) * 32f, -40f - (36f - (18f + i * 4f)));
                _speedSegments[i] = seg.GetComponent<Image>();
                _speedSegments[i].color = SegmentOff;
            }

            // WA police
            Sprite star = StarSprite();
            _stars = new Image[5];
            for (int i = 0; i < _stars.Length; i++)
            {
                var go = new GameObject("WantedStar" + i, typeof(RectTransform), typeof(Image));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(root, false);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(58f, 58f);
                rt.anchoredPosition = new Vector2((i - 2) * 62f, -28f);
                _stars[i] = go.GetComponent<Image>();
                _stars[i].sprite = star;
                _stars[i].raycastTarget = false;
                go.SetActive(false);
            }
            _wantedLabel = MakeText(root, "Wanted", new Vector2(0.5f, 1f), new Vector2(0f, -88f), 26, TextAnchor.UpperCenter, FontStyle.Bold);
            _wantedLabel.text = "WANTED";
            _wantedLabel.color = StarOn;
            _wantedLabel.gameObject.SetActive(false);
            _police = MakeText(root, "Police", new Vector2(0.5f, 0f), new Vector2(0f, 96f), 36, TextAnchor.LowerCenter, FontStyle.Bold);
            _flash = MakeText(root, "Flash", new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), 64, TextAnchor.MiddleCenter, FontStyle.Bold);
            _flash.color = StarOn;
            _flashSub = MakeText(root, "FlashSub", new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), 30, TextAnchor.MiddleCenter, FontStyle.Normal);
        }

        static readonly Color StarOn = new Color(1f, 0.82f, 0.2f, 1f);
        static readonly Color StarOff = new Color(1f, 1f, 1f, 0.28f);
        static readonly Color PoliceBlue = new Color(0.45f, 0.65f, 1f, 1f);
        static readonly Color PoliceRed = new Color(1f, 0.25f, 0.2f, 1f);

        /// <summary>A short title in the upper middle of the screen while the ride goes on ("LOST THEM!").</summary>
        public void Flash(string title, string sub = null, float seconds = 2.2f)
        {
            if (_flash == null) return;
            _flash.text = title;
            _flashSub.text = sub ?? "";
            _flashUntil = Time.unscaledTime + seconds;
        }

        // A comic star: white fill with a dark ink edge (tinted yellow or grey by the Image colour).
        static Sprite StarSprite()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "WantedStar" };
            var px = new Color32[n * n];
            var ink = new Color32(30, 24, 20, 255);
            var fill = new Color32(255, 255, 255, 255);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var p = new Vector2(x + 0.5f - n * 0.5f, y + 0.5f - n * 0.5f + 1.5f);
                    px[y * n + x] = InStar(p, 23f) ? fill : InStar(p, 30f) ? ink : new Color32(0, 0, 0, 0);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        // Inside a five-pointed star (one point up) of outer radius r?
        static bool InStar(Vector2 p, float r)
        {
            float seg = Mathf.PI * 2f / 5f, t = Mathf.Repeat(Mathf.Atan2(p.y, p.x) - Mathf.PI * 0.5f, seg);
            if (t > seg * 0.5f) t = seg - t;
            Vector2 a = new Vector2(r, 0f), b = new Vector2(Mathf.Cos(seg * 0.5f), Mathf.Sin(seg * 0.5f)) * (r * 0.46f), e = b - a;
            Vector2 d = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
            float den = d.x * e.y - d.y * e.x;
            if (Mathf.Abs(den) < 1e-6f) return p.magnitude <= r;
            return p.magnitude <= (a.x * e.y - a.y * e.x) / den;
        }

        // ---- Districts (v0.3): "Start: UWA (keys 1-7)" under the start prompt; DistrictDirector shows it only on the
        // start screen (null hides it). Its own text, so the start / again texts above stay as they are.
        Text _startText;
        public void SetStartLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                if (_startText != null && _startText.text.Length > 0) _startText.text = "";
                return;
            }
            if (_startText == null)
            {
                if (_centre == null) return;
                _startText = MakeText((RectTransform)_centre.rectTransform.parent, "StartDistrict", new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), 30, TextAnchor.MiddleCenter, FontStyle.Bold);
                _startText.color = new Color(1f, 0.85f, 0.2f);
            }
            if (_startText.text != line) _startText.text = line;
        }
        /// <summary>The start district line shown now ("" when hidden; tests).</summary>
        public string StartLineText => _startText != null ? _startText.text : "";
        // ---- end Districts (v0.3)

        void UpdatePolice(GameController g)
        {
            PoliceChase chase = g.GetComponent<PoliceChase>();
            int wanted = chase != null ? chase.Wanted : 0;
            if (wanted != _shownWanted) { _starPulse = Time.unscaledTime; _shownWanted = wanted; }
            float pulse = 1f + 0.35f * Mathf.Clamp01(1f - (Time.unscaledTime - _starPulse) / 0.35f);
            for (int i = 0; i < _stars.Length; i++)
            {
                bool show = wanted > 0;
                if (_stars[i].gameObject.activeSelf != show) _stars[i].gameObject.SetActive(show);
                if (!show) continue;
                bool on = i < wanted;
                _stars[i].color = on ? StarOn : StarOff;
                _stars[i].rectTransform.localScale = Vector3.one * (on && i == wanted - 1 ? pulse : 1f);
            }
            if (_wantedLabel.gameObject.activeSelf != wanted > 0) _wantedLabel.gameObject.SetActive(wanted > 0);
            bool cop = chase != null && chase.Chasing && chase.CopOnRoad && chase.CopGap > -4f && g.State == RunState.Running;
            _police.text = cop ? (chase.CopDanger ? "POLICE!" : "POLICE  " + Mathf.Max(0, Mathf.RoundToInt(chase.CopGap)) + " m") : "";
            if (cop)
            {
                bool blink = ((int)(Time.unscaledTime * 6f) & 1) == 0;
                _police.color = chase.CopDanger ? (blink ? PoliceRed : Color.white) : (blink ? PoliceBlue : PoliceRed);
                _police.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(chase.CopOffsetX * 90f, -520f, 520f), 96f);
            }
            float left = _flashUntil - Time.unscaledTime;
            if (left <= 0f) { if (_flash.text.Length > 0) { _flash.text = ""; _flashSub.text = ""; } return; }
            float a = Mathf.Clamp01(left / 0.4f);
            _flash.color = new Color(StarOn.r, StarOn.g, StarOn.b, a);
            _flashSub.color = new Color(1f, 1f, 1f, a);
        }

        Text MakeText(RectTransform parent, string name, Vector2 anchor, Vector2 pos, int size, TextAnchor align, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1400f, size * 4f);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = align;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            return text;
        }

        // ---- Keyboard off (micro:bit only, e.g. in lessons): the web shell sends SetKeyboardEnabled("0"); then no text
        // mentions keys. The flag can arrive after a screen is shown, so screens are redrawn when it changes.
        static bool KeysAllowed { get { DeviceHub hub = DeviceHub.Instance; return hub == null || hub.KeyboardEnabled; } }
        bool _shownKeys;
        enum CentreScreen { None, Ready, Crashed }
        CentreScreen _screen;
        int _crashScore, _crashBest;
        string _crashTitle, _crashNote;

        void ShowHint()
        {
            _shownKeys = KeysAllowed;
            if (_hint == null) return;
            _hint.text = _shownKeys
                ? "Tilt your micro:bit or press Left / Right to steer  ·  A or Space to jump  ·  B or S to brake  ·  F punch  ·  R bell  ·  E boost  ·  T cartoon"
                : "Tilt your micro:bit to steer  ·  A to jump  ·  B to brake  ·  punch, bell, boost, tricks: program them on your micro:bit!";
        }

        void RedrawForKeys()
        {
            if (KeysAllowed == _shownKeys) return;
            ShowHint();
            if (_screen == CentreScreen.Ready) ShowReady();
            else if (_screen == CentreScreen.Crashed) ShowCrashed(_crashScore, _crashBest, _crashTitle, _crashNote);
        }

        public void ShowReady()
        {
            _screen = CentreScreen.Ready;
            if (_centre == null) return;
            _centre.text = "CITY SCOOTER\n<size=34>" + (KeysAllowed ? "Press A on your micro:bit or Space to start" : "Press A on your micro:bit to start") + "</size>";
            _centre.supportRichText = true;
        }

        public void ShowRunning()
        {
            _screen = CentreScreen.None;
            if (_centre != null) _centre.text = string.Empty;
        }

        public void ShowCrashed(int score, int best, string title = "CRASH!", string note = null)
        {
            _screen = CentreScreen.Crashed;
            _crashScore = score; _crashBest = best; _crashTitle = title; _crashNote = note;
            if (_centre == null) return;
            _centre.supportRichText = true;
            _centre.text = title + "\n<size=36>Score " + score + "    Best " + best + "</size>"
                + (string.IsNullOrEmpty(note) ? "" : "\n<size=26>" + note + "</size>")
                + "\n<size=30>" + (KeysAllowed ? "Press A or Space to ride again" : "Press A on your micro:bit to ride again") + "</size>";
        }

        void OnDestroy() => Game.GameEvents.BoardSaid -= OnBoardSaid;

        // "say:party" from a board: show who said what for a moment (the game also repeats it to every board).
        void OnBoardSaid(string deviceId, string word) => Caption("\u201c" + word + "\u201d  \u2014 " + (deviceId == "sim" ? "Simulator" : deviceId));

        /// <summary>A short line above the hint for a moment (what a board said or triggered).</summary>
        public void Caption(string text, float seconds = 1.8f)
        {
            if (_say == null) return;
            _say.text = text;
            _sayUntil = Time.unscaledTime + seconds;
        }

        void Update()
        {
            RedrawForKeys();
            if (_say != null && _say.text.Length > 0 && Time.unscaledTime > _sayUntil) _say.text = "";
            GameController g = game != null ? game : GameController.Instance;
            if (g == null || g.rider == null) return;
            // Strings only when what they show changed: per-frame garbage makes the web build's GC hitch.
            int score = g.Score;
            if (score != _shownScore) { _shownScore = score; _score.text = score.ToString(); }
            RiderActions actions = _actions != null ? _actions : (_actions = g.GetComponent<RiderActions>());
            int charges = actions != null ? Mathf.FloorToInt(actions.Charges) : -1, max = actions != null ? actions.MaxCharges : 0;
            int boost = g.rider.Boosting ? 2 : g.rider.BoostReady ? 1 : 0;
            long key = Mathf.FloorToInt(Mathf.Max(0f, g.rider.Distance));
            key = key * 997 + g.Gifts;
            key = key * 997 + Mathf.RoundToInt(g.rider.Speed);
            key = key * 997 + Mathf.RoundToInt(g.rider.CurrentMaxSpeed);
            key = key * 31 + (g.rider.Braking ? 1 : 0) * 16 + (RiderActions.CartoonMode ? 1 : 0) * 8 + boost * 2;
            key = key * 31 + (charges + 1) * 8 + max;
            if (key != _shownStats)
            {
                _shownStats = key;
                _stats.text = Mathf.FloorToInt(Mathf.Max(0f, g.rider.Distance)) + " m    Gifts " + g.Gifts
                    + "    Speed " + g.rider.Speed.ToString("0") + " / " + g.rider.CurrentMaxSpeed.ToString("0") + " m/s"
                    + (g.rider.Braking ? "    BRAKE" : "")
                    + (RiderActions.CartoonMode ? "    CARTOON MODE" : "")
                    + (actions != null ? "    Punch " + new string('\u25CF', Mathf.Clamp(charges, 0, max)) + new string('\u25CB', Mathf.Clamp(max - charges, 0, max)) : "")
                    + (boost == 2 ? "    BOOST!" : boost == 1 ? "    Boost ready" : "");
            }
            int level = Mathf.Max(0, g.rider.SpeedLevel);
            for (int i = 0; i < _speedSegments.Length; i++) _speedSegments[i].color = i <= level && g.State == RunState.Running ? SegmentOn : SegmentOff;
            if (Time.unscaledTime >= _nextDeviceLabel)
            {
                _nextDeviceLabel = Time.unscaledTime + 0.5f;
                string label = DeviceLabel();
                if (label != _device.text) _device.text = label;
            }
            UpdatePolice(g);
            UpdateTrickHint(); // ---- Ramps and air (v0.3)
        }

        // ---- Ramps and air (v0.3): "Q trick" at the end of the hint, only while the keyboard is on (the board's
        // "trick" message always works). Whatever the hint says otherwise is left alone.
        const string TrickHint = "  \u00B7  Q trick";
        void UpdateTrickHint()
        {
            if (_hint == null) return;
            DeviceHub hub = DeviceHub.Instance;
            bool keys = hub == null || hub.KeyboardEnabled;
            string text = _hint.text ?? "";
            bool has = text.EndsWith(TrickHint, System.StringComparison.Ordinal);
            if (keys && !has && text.Length > 0) _hint.text = text + TrickHint;
            else if (!keys && has) _hint.text = text.Substring(0, text.Length - TrickHint.Length);
        }

        int _shownScore = int.MinValue;
        long _shownStats = long.MinValue;
        float _nextDeviceLabel;
        RiderActions _actions;

        static string DeviceLabel()
        {
            DeviceHub hub = DeviceHub.Instance;
            if (hub == null) return "Keyboard";
            string label = null;
            foreach (DeviceInfo d in hub.Devices.Values)
            {
                if (!d.Connected) continue;
                string one = d.Id == "sim" ? "Simulator" : "micro:bit " + (d.Version != null ? d.Version.ToUpperInvariant() : "");
                label = label == null ? one : label + " + " + one;
            }
            // The class build has no keyboard (micro:bit only): say what is missing instead.
            return label ?? (hub.KeyboardEnabled ? "Keyboard" : "No micro:bit yet");
        }
    }
}
