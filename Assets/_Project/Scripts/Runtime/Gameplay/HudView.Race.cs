using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Radio race (v0.4) screens, same style as the rest of the HUD (uGUI text with outlines, drawn sprites):
    /// - lobby: "RACE 12 · YOU: AMY", the riders heard in the last 3 s with signal bars, "Waiting for the race to
    ///   start (go)"; during a practice ride a one-line banner instead;
    /// - countdown: 3, 2, 1, GO!;
    /// - race: a bar from START to FINISH at the top with a dot per rider (yours biggest), the live place "2nd / 5",
    ///   the race time and "+3 s" after a crash, a name tag over every ghost;
    /// - finish: FINISH!, your place and time; results: the podium and the table.
    /// RaceDirector calls UpdateRaceHud every frame in race mode (after the camera moved); nothing here runs otherwise.
    /// </summary>
    public sealed partial class HudView
    {
        RectTransform _raceRoot, _barRoot, _lobbyRoot, _podiumRoot;
        Image _raceShade;
        Text _raceTitle, _raceSub, _raceHint, _raceFoot, _raceBig, _racePlace, _raceClock, _racePenalty, _raceBanner, _raceList;
        Image _barLine, _barBack;
        Image _meDot;
        Text _meLabel;
        readonly List<Image> _dots = new List<Image>();
        readonly List<Text> _tags = new List<Text>();
        readonly List<Text> _lobbyNames = new List<Text>();
        readonly List<Image[]> _lobbyBars = new List<Image[]>();
        readonly Image[] _podiumBlocks = new Image[3];
        readonly Text[] _podiumNames = new Text[3], _podiumTimes = new Text[3], _podiumNumbers = new Text[3];
        Sprite _dotSprite;
        float _nextRaceText;
        RaceState _shownRace = (RaceState)(-1);
        bool _shownPractice;

        const float BarWidth = 1000f, BarY = -64f;
        static readonly Color MeColour = new Color(1f, 0.3f, 0.22f);
        static readonly Color Yellow = new Color(1f, 0.85f, 0.2f);
        static readonly Color GreyText = new Color(0.72f, 0.72f, 0.76f);
        static readonly Color BarOn = new Color(0.45f, 1f, 0.45f);
        static readonly Color BarOff = new Color(1f, 1f, 1f, 0.22f);
        static readonly Color Gold = new Color(1f, 0.8f, 0.22f), Silver = new Color(0.78f, 0.8f, 0.86f), Bronze = new Color(0.84f, 0.52f, 0.26f);

        /// <summary>Texts shown by the race screens now (tests): title, sub line, big centre text, place, clock.</summary>
        public string RaceTitleText => _raceTitle != null && _raceTitle.gameObject.activeInHierarchy ? _raceTitle.text : "";
        public string RaceSubText => _raceSub != null && _raceSub.gameObject.activeInHierarchy ? _raceSub.text : "";
        public string RaceBigText => _raceBig != null && _raceBig.gameObject.activeInHierarchy ? _raceBig.text : "";
        public string RacePlaceText => _racePlace != null && _racePlace.gameObject.activeInHierarchy ? _racePlace.text : "";
        public string RacePenaltyText => _racePenalty != null && _racePenalty.gameObject.activeInHierarchy ? _racePenalty.text : "";
        public string RaceBannerText => _raceBanner != null && _raceBanner.gameObject.activeInHierarchy ? _raceBanner.text : "";
        /// <summary>Names in the lobby list now (tests).</summary>
        public List<string> RaceLobbyNames()
        {
            var list = new List<string>();
            if (_lobbyRoot == null || !_lobbyRoot.gameObject.activeInHierarchy) return list;
            foreach (Text t in _lobbyNames) if (t.gameObject.activeSelf) list.Add(t.text);
            return list;
        }
        /// <summary>Name tags shown now (tests).</summary>
        public List<string> RaceTagTexts()
        {
            var list = new List<string>();
            foreach (Text t in _tags) if (t.gameObject.activeSelf) list.Add(t.text);
            return list;
        }

        RectTransform CanvasRoot => _centre != null ? (RectTransform)_centre.rectTransform.parent : null;

        void BuildRace()
        {
            RectTransform canvas = CanvasRoot;
            if (canvas == null) return;
            _dotSprite = DotSprite();
            _raceRoot = Panel(canvas, "Race");

            // Tags first: everything else draws over them.
            _barRoot = Panel(_raceRoot, "RaceBar");
            _barBack = Rect(_barRoot, "BarBack", new Vector2(0.5f, 1f), new Vector2(0f, BarY), new Vector2(BarWidth + 12f, 22f), new Color(0f, 0f, 0f, 0.45f));
            _barLine = Rect(_barRoot, "BarLine", new Vector2(0.5f, 1f), new Vector2(0f, BarY), new Vector2(BarWidth, 10f), new Color(1f, 1f, 1f, 0.9f));
            for (int i = 0; i < 2; i++)
                Rect(_barRoot, i == 0 ? "StartTick" : "FinishTick", new Vector2(0.5f, 1f), new Vector2((i == 0 ? -0.5f : 0.5f) * BarWidth, BarY), new Vector2(8f, 40f), i == 0 ? Color.white : Yellow);
            Text start = MakeText(_barRoot, "BarStart", new Vector2(0.5f, 1f), new Vector2(-BarWidth * 0.5f - 18f, BarY + 12f), 24, TextAnchor.UpperRight, FontStyle.Bold);
            start.rectTransform.pivot = new Vector2(1f, 0.5f);
            start.alignment = TextAnchor.MiddleRight;
            start.text = "START";
            Text finish = MakeText(_barRoot, "BarFinish", new Vector2(0.5f, 1f), new Vector2(BarWidth * 0.5f + 18f, BarY + 12f), 24, TextAnchor.UpperLeft, FontStyle.Bold);
            finish.rectTransform.pivot = new Vector2(0f, 0.5f);
            finish.alignment = TextAnchor.MiddleLeft;
            finish.text = "FINISH";
            finish.color = Yellow;
            _meDot = Rect(_barRoot, "MeDot", new Vector2(0.5f, 1f), new Vector2(-BarWidth * 0.5f, BarY), new Vector2(44f, 44f), MeColour);
            _meDot.sprite = _dotSprite;
            _meLabel = MakeText(_barRoot, "MeLabel", new Vector2(0.5f, 1f), new Vector2(0f, BarY - 26f), 22, TextAnchor.UpperCenter, FontStyle.Bold);
            _meLabel.text = "YOU";
            // Place and time on the right, under the speed bar (the stats line runs along the top left).
            _racePlace = MakeText(_raceRoot, "RacePlace", new Vector2(1f, 1f), new Vector2(-40f, -124f), 72, TextAnchor.UpperRight, FontStyle.Bold);
            _raceClock = MakeText(_raceRoot, "RaceClock", new Vector2(1f, 1f), new Vector2(-40f, -206f), 34, TextAnchor.UpperRight, FontStyle.Bold);
            _racePenalty = MakeText(_raceRoot, "RacePenalty", new Vector2(1f, 1f), new Vector2(-40f, -250f), 50, TextAnchor.UpperRight, FontStyle.Bold);
            _racePenalty.color = new Color(1f, 0.32f, 0.25f);
            _raceBanner = MakeText(_raceRoot, "RaceBanner", new Vector2(0.5f, 1f), new Vector2(0f, -34f), 30, TextAnchor.UpperCenter, FontStyle.Bold);
            _raceBanner.color = Yellow;

            // A dark band behind the centre screens, so white text reads over a sunny street.
            _raceShade = Rect(_raceRoot, "RaceShade", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 100f), new Color(0.04f, 0.05f, 0.09f, 0.6f));
            _raceTitle = MakeText(_raceRoot, "RaceTitle", new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), 72, TextAnchor.MiddleCenter, FontStyle.Bold);
            _raceTitle.color = Yellow;
            _raceBig = MakeText(_raceRoot, "RaceBig", new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), 240, TextAnchor.MiddleCenter, FontStyle.Bold);
            _raceSub = MakeText(_raceRoot, "RaceSub", new Vector2(0.5f, 0.5f), new Vector2(0f, 118f), 34, TextAnchor.MiddleCenter, FontStyle.Bold);
            _raceHint = MakeText(_raceRoot, "RaceHint", new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), 38, TextAnchor.MiddleCenter, FontStyle.Bold);
            _raceFoot = MakeText(_raceRoot, "RaceFoot", new Vector2(0.5f, 0.5f), new Vector2(0f, -178f), 28, TextAnchor.MiddleCenter, FontStyle.Normal);
            _raceList = MakeText(_raceRoot, "RaceList", new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), 32, TextAnchor.UpperCenter, FontStyle.Bold);
            _raceList.rectTransform.pivot = new Vector2(0.5f, 1f);
            _raceList.supportRichText = true;
            _raceTitle.supportRichText = _raceSub.supportRichText = true;
            foreach (Text t in new[] { _raceTitle, _raceBig, _raceSub, _raceHint, _raceFoot, _raceList, _racePlace, _raceClock })
                t.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            _raceBig.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            _raceTitle.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            _racePlace.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);

            _lobbyRoot = Panel(_raceRoot, "RaceLobby");
            _podiumRoot = Panel(_raceRoot, "RacePodium");
            float[] xs = { 0f, -280f, 280f }, hs = { 230f, 160f, 110f };
            Color[] cs = { Gold, Silver, Bronze };
            for (int i = 0; i < 3; i++)
            {
                Image block = Rect(_podiumRoot, "Podium" + (i + 1), new Vector2(0.5f, 0.5f), new Vector2(xs[i], -70f + hs[i] * 0.5f), new Vector2(250f, hs[i]), cs[i]);
                block.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.08f, 0.06f, 1f);
                block.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);
                _podiumBlocks[i] = block;
                _podiumNumbers[i] = MakeText(_podiumRoot, "PodiumNumber" + (i + 1), new Vector2(0.5f, 0.5f), new Vector2(xs[i], -70f + hs[i] * 0.5f), 84, TextAnchor.MiddleCenter, FontStyle.Bold);
                _podiumNumbers[i].text = (i + 1).ToString();
                _podiumNumbers[i].color = new Color(0.16f, 0.12f, 0.08f);
                _podiumNumbers[i].GetComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.35f);
                _podiumNames[i] = MakeText(_podiumRoot, "PodiumName" + (i + 1), new Vector2(0.5f, 0.5f), new Vector2(xs[i], -70f + hs[i] + 84f), 50, TextAnchor.MiddleCenter, FontStyle.Bold);
                _podiumTimes[i] = MakeText(_podiumRoot, "PodiumTime" + (i + 1), new Vector2(0.5f, 0.5f), new Vector2(xs[i], -70f + hs[i] + 34f), 32, TextAnchor.MiddleCenter, FontStyle.Bold);
                _podiumNames[i].GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
                _podiumNames[i].GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            }
        }

        RectTransform Panel(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        Image Rect(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = colour;
            img.raycastTarget = false;
            return img;
        }

        // A comic dot: white fill with a dark ink ring (tinted by the Image colour).
        static Sprite DotSprite()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "RaceDot" };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x + 0.5f - n * 0.5f, y + 0.5f - n * 0.5f).magnitude;
                    byte a = (byte)(255f * Mathf.Clamp01(n * 0.5f - d));
                    px[y * n + x] = d < n * 0.5f - 6f ? new Color32(255, 255, 255, 255) : new Color32(25, 20, 18, a);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>A crash in race mode: the ride goes on after a 3 s stop.</summary>
        public void RaceCrash(string title)
        {
            Flash(string.IsNullOrEmpty(title) ? "CRASH!" : title, "+3 s  ·  back on the scooter in 3 s", 2.6f);
        }

        static void Show(Component c, bool on) { if (c != null && c.gameObject.activeSelf != on) c.gameObject.SetActive(on); }
        static void Set(Text t, string s) { if (t != null && t.text != s) t.text = s; }

        /// <summary>Called by RaceDirector every frame in race mode (and once when it ends).</summary>
        public void UpdateRaceHud(RaceDirector race)
        {
            bool on = race != null && race.State != RaceState.Off;
            if (!on)
            {
                if (_raceRoot != null && _raceRoot.gameObject.activeSelf) _raceRoot.gameObject.SetActive(false);
                _shownRace = (RaceState)(-1);
                return;
            }
            if (_raceRoot == null) BuildRace();
            if (_raceRoot == null) return;
            if (!_raceRoot.gameObject.activeSelf) _raceRoot.gameObject.SetActive(true);
            GameController g = game != null ? game : GameController.Instance;
            RaceState s = race.State;
            bool practice = s == RaceState.Lobby && g != null && g.State != RunState.Ready;
            bool redraw = s != _shownRace || practice != _shownPractice || Time.unscaledTime >= _nextRaceText;
            if (redraw)
            {
                _nextRaceText = Time.unscaledTime + 0.1f;
                _shownRace = s;
                _shownPractice = practice;
                if (_centre != null && _centre.text.Length > 0 && (s != RaceState.Lobby || !practice)) _centre.text = "";
                DrawRaceTexts(race, s, practice);
            }
            bool bar = s == RaceState.Racing || s == RaceState.Finished;
            Show(_barRoot, bar);
            if (bar) PlaceDots(race);
            PlaceTags(race, TagsShown(s));
            // The countdown number changes on its own second; GO! for a moment after the start.
            if (s == RaceState.Countdown) Set(_raceBig, Mathf.CeilToInt(Mathf.Max(0.01f, race.CountdownLeft)).ToString());
        }

        void DrawRaceTexts(RaceDirector race, RaceState s, bool practice)
        {
            bool lobby = s == RaceState.Lobby && !practice;
            Show(_lobbyRoot, lobby);
            Show(_podiumRoot, s == RaceState.Results);
            Show(_raceBanner, s == RaceState.Lobby && practice);
            Show(_racePlace, s == RaceState.Racing || s == RaceState.Finished);
            Show(_raceClock, s == RaceState.Racing || s == RaceState.Finished);
            Show(_racePenalty, race.PenaltyLeft > 0f && s != RaceState.Lobby);
            Show(_raceTitle, lobby || s == RaceState.Results || s == RaceState.Finished);
            Show(_raceBig, s == RaceState.Countdown || (s == RaceState.Racing && race.RaceTime < 0.9f));
            Show(_raceSub, lobby || s == RaceState.Countdown || s == RaceState.Finished);
            Show(_raceHint, lobby || s == RaceState.Finished);
            Show(_raceFoot, lobby || s == RaceState.Results || s == RaceState.Finished);
            Show(_raceList, s == RaceState.Results);
            string group = "RACE " + race.Group, me = string.IsNullOrEmpty(race.MyName) ? "?" : race.MyName;
            string press = KeysAllowed ? "Press A (or Space)" : "Press A on your micro:bit";
            Show(_raceShade, false);
            switch (s)
            {
                case RaceState.Lobby:
                    if (practice)
                    {
                        int n = race.HeardRecently().Count;
                        Set(_raceBanner, "PRACTICE  ·  " + group + "  ·  YOU: " + me + "  ·  " + n + (n == 1 ? " rider" : " riders") + " here  ·  waiting for go");
                        break;
                    }
                    _raceTitle.rectTransform.anchoredPosition = new Vector2(0f, 200f);
                    _raceTitle.fontSize = 72;
                    Set(_raceTitle, group + "  ·  YOU: " + me);
                    List<RaceDirector.Rival> heard = race.HeardRecently();
                    Set(_raceSub, heard.Count == 0 ? "Nobody heard yet" : "Riders I can hear:");
                    _raceSub.rectTransform.anchoredPosition = new Vector2(0f, 118f);
                    DrawLobbyList(heard);
                    Set(_raceHint, "Waiting for the race to start (go)");
                    _raceHint.rectTransform.anchoredPosition = new Vector2(0f, -120f);
                    Set(_raceFoot, heard.Count == 0
                        ? "Check: the same group number?  radio on?  rx: messages passed to the game?"
                        : press + " to practise");
                    int extraRows = Mathf.Max(0, (heard.Count - 1) / 5);
                    _raceFoot.rectTransform.anchoredPosition = new Vector2(0f, -178f - extraRows * 70f);
                    Shade(25f - extraRows * 35f, 520f + extraRows * 70f);
                    break;
                case RaceState.Countdown:
                    _raceBig.color = Color.white;
                    _raceBig.fontSize = 240;
                    _raceBig.rectTransform.anchoredPosition = new Vector2(0f, 70f);
                    Set(_raceSub, group + "  ·  " + race.Metres + " m from " + race.StartName());
                    _raceSub.rectTransform.anchoredPosition = new Vector2(0f, -110f);
                    Shade(-110f, 64f);
                    break;
                case RaceState.Racing:
                case RaceState.Finished:
                {
                    int place = race.MyPlace(out int count);
                    Set(_racePlace, RaceDirector.Ordinal(place) + " / " + count);
                    Set(_raceClock, Seconds(race.RaceTime) + "  ·  " + Mathf.Clamp(Mathf.FloorToInt(race.rider != null ? race.rider.Distance : 0f), 0, race.Metres) + " / " + race.Metres + " m");
                    Set(_racePenalty, race.PenaltyLeft > 0f ? "+3 s  (" + Mathf.CeilToInt(race.PenaltyLeft) + ")" : "");
                    if (s == RaceState.Racing)
                    {
                        _raceBig.color = new Color(0.45f, 1f, 0.45f);
                        _raceBig.fontSize = 200;
                        Set(_raceBig, "GO!");
                        break;
                    }
                    _raceTitle.rectTransform.anchoredPosition = new Vector2(0f, 150f);
                    _raceTitle.fontSize = 120;
                    Set(_raceTitle, "FINISH!");
                    Set(_raceSub, RaceDirector.Ordinal(place) + " place  ·  " + Time2(race.FinishTime));
                    _raceSub.rectTransform.anchoredPosition = new Vector2(0f, 50f);
                    Set(_raceHint, race.SinceFinish < RaceDirector.ResultsAfterSeconds
                        ? "Waiting for the others  ·  results in " + Mathf.CeilToInt(RaceDirector.ResultsAfterSeconds - race.SinceFinish) + " s" : "");
                    _raceHint.rectTransform.anchoredPosition = new Vector2(0f, -10f);
                    Set(_raceFoot, "Your micro:bit is sending fin:" + race.FinishTime.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                    _raceFoot.rectTransform.anchoredPosition = new Vector2(0f, -60f);
                    Shade(65f, 330f);
                    break;
                }
                case RaceState.Results:
                    _raceTitle.rectTransform.anchoredPosition = new Vector2(0f, 360f);
                    _raceTitle.fontSize = 64;
                    Show(_raceTitle, true);
                    Set(_raceTitle, "RESULTS  ·  " + group);
                    int rows = DrawResults(race);
                    Set(_raceFoot, press + " to practise  ·  the next race starts with go");
                    float foot = -120f - rows * 40f - 30f;
                    _raceFoot.rectTransform.anchoredPosition = new Vector2(0f, foot);
                    Shade((410f + foot - 30f) * 0.5f, 410f - foot + 30f);
                    break;
            }
        }

        void Shade(float y, float height)
        {
            Show(_raceShade, true);
            _raceShade.rectTransform.anchoredPosition = new Vector2(0f, y);
            _raceShade.rectTransform.sizeDelta = new Vector2(4000f, height);
        }

        static string Seconds(float s) => s.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s";

        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        static Color RivalColour(int index) => index < 0 ? MeColour : RaceDirector.Palette[index % RaceDirector.Palette.Length];

        void DrawLobbyList(List<RaceDirector.Rival> heard)
        {
            const int perRow = 5;
            const float slot = 290f;
            while (_lobbyNames.Count < heard.Count)
            {
                Text name = MakeText(_lobbyRoot, "LobbyName", new Vector2(0.5f, 0.5f), Vector2.zero, 46, TextAnchor.MiddleRight, FontStyle.Bold);
                name.rectTransform.sizeDelta = new Vector2(200f, 60f);
                name.rectTransform.pivot = new Vector2(1f, 0.5f);
                name.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
                name.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
                var bars = new Image[3];
                for (int b = 0; b < 3; b++)
                {
                    bars[b] = Rect(_lobbyRoot, "LobbyBar", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(11f, 14f + b * 10f), BarOff);
                    bars[b].rectTransform.pivot = new Vector2(0.5f, 0f);
                }
                _lobbyNames.Add(name);
                _lobbyBars.Add(bars);
            }
            int rows = (heard.Count + perRow - 1) / perRow;
            for (int i = 0; i < _lobbyNames.Count; i++)
            {
                bool show = i < heard.Count;
                Show(_lobbyNames[i], show);
                foreach (Image b in _lobbyBars[i]) Show(b, show);
                if (!show) continue;
                RaceDirector.Rival r = heard[i];
                int row = i / perRow, inRow = Mathf.Min(perRow, heard.Count - row * perRow);
                float x = (i % perRow - (inRow - 1) * 0.5f) * slot + 20f;
                float y = 40f - row * 70f - (rows > 1 ? 0f : 0f);
                _lobbyNames[i].rectTransform.anchoredPosition = new Vector2(x, y);
                Set(_lobbyNames[i], r.Name);
                _lobbyNames[i].color = RivalColour(r.Colour);
                int bars = RaceDirector.SignalBars(r.PacketsPerSecond, RaceDirector.LobbyPositionsPerSecond);
                for (int b = 0; b < 3; b++)
                {
                    _lobbyBars[i][b].rectTransform.anchoredPosition = new Vector2(x + 18f + b * 16f, y - 18f);
                    _lobbyBars[i][b].color = b < bars ? BarOn : BarOff;
                }
            }
            _raceHint.rectTransform.anchoredPosition = new Vector2(0f, -120f - Mathf.Max(0, rows - 1) * 70f);
        }

        // The podium (first three) and the rest as a list under it; returns the number of list lines.
        int DrawResults(RaceDirector race)
        {
            List<RaceDirector.Standing> all = race.Standings();
            for (int i = 0; i < 3; i++)
            {
                bool show = i < all.Count;
                Show(_podiumBlocks[i], show);
                Show(_podiumNumbers[i], show);
                Show(_podiumNames[i], show);
                Show(_podiumTimes[i], show);
                if (!show) continue;
                RaceDirector.Standing st = all[i];
                Set(_podiumNames[i], st.Me ? st.Name + " (you)" : st.Name + (st.Suspicious ? " ?" : ""));
                _podiumNames[i].color = RivalColour(st.Me ? -1 : st.Colour);
                Set(_podiumTimes[i], st.Finished ? Time2(st.Time) : Mathf.FloorToInt(st.Distance) + " m");
            }
            var sb = new System.Text.StringBuilder();
            int shown = 0;
            for (int i = 3; i < all.Count; i++)
            {
                if (shown == 6) { sb.Append("+" + (all.Count - i) + " more"); shown++; break; }
                RaceDirector.Standing st = all[i];
                sb.Append("<color=#").Append(Hex(RivalColour(st.Me ? -1 : st.Colour))).Append('>')
                  .Append(RaceDirector.Ordinal(i + 1)).Append("  ").Append(st.Name).Append(st.Me ? " (you)" : "").Append("</color>  ")
                  .Append(st.Finished ? Time2(st.Time) : Mathf.FloorToInt(st.Distance) + " m (still riding)")
                  .Append(st.Suspicious ? "  <color=#ff5a46>? impossible</color>" : "").Append('\n');
                shown++;
            }
            Set(_raceList, sb.ToString().TrimEnd('\n'));
            _raceList.rectTransform.anchoredPosition = new Vector2(0f, -92f);
            return shown;
        }

        static string Time2(float s) => s.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " s";

        void PlaceDots(RaceDirector race)
        {
            IReadOnlyList<RaceDirector.Rival> rivals = race.Rivals;
            int n = 0;
            float metres = Mathf.Max(1f, race.Metres);
            double now = Time.unscaledTimeAsDouble;
            for (int i = 0; i < rivals.Count; i++)
            {
                RaceDirector.Rival r = rivals[i];
                if (!r.InRace) continue;
                if (_dots.Count <= n)
                {
                    Image dot = Rect(_barRoot, "Dot", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(28f, 28f), Color.white);
                    dot.sprite = _dotSprite;
                    dot.transform.SetSiblingIndex(_meDot.transform.GetSiblingIndex()); // under my dot
                    _dots.Add(dot);
                }
                Image d = _dots[n++];
                Show(d, true);
                float f = r.Finished ? 1f : Mathf.Clamp01(r.ShownDistance / metres);
                d.rectTransform.anchoredPosition = new Vector2((f - 0.5f) * BarWidth, BarY);
                d.color = r.SignalLost(now) ? new Color(0.55f, 0.55f, 0.58f) : RivalColour(r.Colour);
            }
            for (int i = n; i < _dots.Count; i++) Show(_dots[i], false);
            float mine = race.FinishTime > 0f ? 1f : Mathf.Clamp01((race.rider != null ? race.rider.Distance : 0f) / metres);
            _meDot.rectTransform.anchoredPosition = new Vector2((mine - 0.5f) * BarWidth, BarY);
            _meLabel.rectTransform.anchoredPosition = new Vector2((mine - 0.5f) * BarWidth, BarY - 26f);
        }

        void PlaceTags(RaceDirector race, bool show)
        {
            Camera cam = Camera.main;
            RectTransform canvas = CanvasRoot;
            int n = 0;
            if (show && cam != null && canvas != null)
            {
                Vector2 size = canvas.rect.size;
                double now = Time.unscaledTimeAsDouble;
                _tagOrder.Clear();
                foreach (RaceDirector.Rival r in race.Rivals)
                {
                    if (!RaceDirector.GhostPosition(r, out Vector3 world)) continue;
                    Vector3 vp = cam.WorldToViewportPoint(Bend(world + Vector3.up * 2.55f));
                    if (vp.z <= 0.5f || vp.z > 260f || vp.x < -0.05f || vp.x > 1.05f || vp.y < -0.05f || vp.y > 1.05f) continue;
                    _tagOrder.Add((r, vp));
                }
                _tagOrder.Sort((a, b) => a.vp.z.CompareTo(b.vp.z)); // the nearest keeps its place, others move up
                _placedTags.Clear();
                foreach ((RaceDirector.Rival r, Vector3 vp) in _tagOrder)
                {
                    float dist = vp.z;
                    if (_tags.Count <= n)
                    {
                        Text tag = MakeText(_raceRoot, "Tag", new Vector2(0.5f, 0.5f), Vector2.zero, 32, TextAnchor.LowerCenter, FontStyle.Bold);
                        tag.rectTransform.sizeDelta = new Vector2(400f, 50f);
                        tag.rectTransform.pivot = new Vector2(0.5f, 0f);
                        tag.supportRichText = true;
                        tag.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
                        tag.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
                        tag.transform.SetAsFirstSibling(); // under the race panels
                        _tags.Add(tag);
                    }
                    Text t = _tags[n++];
                    Show(t, true);
                    bool lost = r.SignalLost(now);
                    Set(t, r.Name + (r.Suspicious ? " <color=#ff4a3a>?</color>" : "") + (lost ? " <size=22>signal lost</size>" : ""));
                    t.color = lost ? GreyText : RivalColour(r.Colour);
                    float scale = Mathf.Lerp(1f, 0.6f, Mathf.InverseLerp(8f, 160f, dist));
                    t.rectTransform.localScale = Vector3.one * scale;
                    // Tags of riders bunched up far ahead would sit on top of each other: stack them.
                    var at = new Vector2((vp.x - 0.5f) * size.x, (vp.y - 0.5f) * size.y);
                    float halfW = t.preferredWidth * 0.5f * scale + 6f, h = 34f * scale;
                    for (int guard = 0; guard < 12; guard++)
                    {
                        bool clash = false;
                        foreach (Rect placed in _placedTags)
                            if (Mathf.Abs(placed.center.x - at.x) < placed.width * 0.5f + halfW && at.y < placed.yMax && at.y + h > placed.yMin)
                            {
                                at.y = placed.yMax + 2f;
                                clash = true;
                            }
                        if (!clash) break;
                    }
                    _placedTags.Add(new Rect(at.x - halfW, at.y, halfW * 2f, h));
                    t.rectTransform.anchoredPosition = at;
                }
            }
            for (int i = n; i < _tags.Count; i++) Show(_tags[i], false);
        }

        readonly List<(RaceDirector.Rival r, Vector3 vp)> _tagOrder = new List<(RaceDirector.Rival, Vector3)>();
        readonly List<Rect> _placedTags = new List<Rect>();

        /// <summary>Re-place the name tags now (screenshots: after switching the canvas to a render texture).</summary>
        public void RelayoutRaceTags()
        {
            RaceDirector race = RaceDirector.Instance;
            if (race != null && race.State != RaceState.Off && _raceRoot != null) PlaceTags(race, TagsShown(race.State));
        }

        // Name tags over the ghosts: not in the lobby (no ghosts) and not on the results screen (the podium has the names).
        static bool TagsShown(RaceState s) => s == RaceState.Countdown || s == RaceState.Racing || s == RaceState.Finished;

        // Where ComicWorldBend.hlsl draws a world point (the road far ahead rises and curves through fake turns), so a
        // tag sits on the drawn ghost. Same formula as ComicBendPosition with amount 1; identity when no globals are set.
        static readonly int WorldBendId = Shader.PropertyToID("_ComicWorldBend"), HorizonId = Shader.PropertyToID("_ComicHorizon"),
            CurveTailId = Shader.PropertyToID("_ComicCurveTail"), TurnId = Shader.PropertyToID("_ComicTurn"), Turn2Id = Shader.PropertyToID("_ComicTurn2");

        static Vector3 Bend(Vector3 p)
        {
            Vector4 wb = Shader.GetGlobalVector(WorldBendId), hz = Shader.GetGlobalVector(HorizonId), tail = Shader.GetGlobalVector(CurveTailId);
            float d = p.z - wb.x;
            float dd = Mathf.Max(0f, d - wb.y), a = Mathf.Max(0f, tail.x - wb.y), rise;
            if (tail.z < 0.01f || dd <= a) rise = dd * dd * wb.z;
            else
            {
                float t = dd - a, finalSlope = tail.y + hz.w, delta = 2f * a * wb.z - finalSlope, decay = Mathf.Exp(-t / tail.z);
                rise = a * a * wb.z + finalSlope * t + delta * tail.z * (1f - decay);
            }
            float span = Mathf.Max(1f, hz.y - hz.x), tt = Mathf.Clamp01((d - hz.x) / span), strength = hz.y > 0.01f ? 1f - hz.z : 0f;
            float scale = 1f - strength * tt * tt * (3f - 2f * tt);
            float ground = wb.w - d * hz.w;
            float side = TurnSegment(p.z, Shader.GetGlobalVector(TurnId), wb) + TurnSegment(p.z, Shader.GetGlobalVector(Turn2Id), wb);
            return new Vector3(p.x * scale + side, ground + rise + (p.y - ground) * scale, p.z);
        }

        static float TurnSegment(float z, Vector4 turn, Vector4 wb)
        {
            float from = Mathf.Max(turn.y, wb.x + wb.y), span = Mathf.Max(0f, turn.z - from), q = Mathf.Clamp(z - from, 0f, span);
            return turn.x * (q * q + 2f * span * Mathf.Max(0f, z - turn.z));
        }
    }
}
