using CityScooter.Devices;
using UnityEngine;
using UnityEngine.UI;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// v0 HUD built in code (uGUI + the built-in LegacyRuntime font) so it works before the UI art exists:
    /// score, distance and gifts, a 10-step speed bar (the same 0..9 the micro:bit gets), what is
    /// controlling the scooter, and Ready / Crash messages. Student-facing text is English.
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
            _hint.text = "Tilt your micro:bit or press Left / Right to steer  ·  A or Space to jump  ·  B or S to brake";

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

        public void ShowReady()
        {
            if (_centre == null) return;
            _centre.text = "CITY SCOOTER\n<size=34>Press A on your micro:bit or Space to start</size>";
            _centre.supportRichText = true;
        }

        public void ShowRunning()
        {
            if (_centre != null) _centre.text = string.Empty;
        }

        public void ShowCrashed(int score, int best)
        {
            if (_centre == null) return;
            _centre.supportRichText = true;
            _centre.text = "CRASH!\n<size=36>Score " + score + "    Best " + best + "</size>\n<size=30>Press A or Space to ride again</size>";
        }

        void Update()
        {
            GameController g = game != null ? game : GameController.Instance;
            if (g == null || g.rider == null) return;
            _score.text = g.Score.ToString();
            _stats.text = Mathf.FloorToInt(Mathf.Max(0f, g.rider.Distance)) + " m    Gifts " + g.Gifts + (g.rider.Braking ? "    BRAKE" : "");
            int level = Mathf.Max(0, g.rider.SpeedLevel);
            for (int i = 0; i < _speedSegments.Length; i++) _speedSegments[i].color = i <= level && g.State == RunState.Running ? SegmentOn : SegmentOff;
            _device.text = DeviceLabel();
        }

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
            return label ?? "Keyboard";
        }
    }
}
