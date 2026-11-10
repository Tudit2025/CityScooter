using System.Collections.Generic;
using CityScooter.Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace CityScooter.Gameplay
{
    /// <summary>
    /// Item race (v0.5, docs/item-race.md) on the HUD, same style as the rest (uGUI, drawn sprites, no font glyphs):
    /// - the item slot at the bottom right: a comic panel with the item's picture and name, and under it what to do
    ///   ("your program says: use"), because items never use themselves;
    /// - beside it what is on right now: SHIELD 6, BOOST, SLOW 2, POLICE 12, and ROCKET! while one is on its way;
    /// - a flash over the whole screen for a bolt, speed lines round the edge during the boost item.
    /// The big one-line messages (BOOM!, BLOCKED!, "AMY → BEN: ROCKET!") go through <see cref="Flash(string,string,float,Color)"/>.
    /// RaceItems calls UpdateItemHud every frame in race mode; nothing here runs otherwise.
    /// </summary>
    public sealed partial class HudView
    {
        RectTransform _itemRoot, _speedLines;
        Image _itemPanel, _itemShadow, _itemIcon, _itemFlash;
        Text _itemLabel, _itemName, _itemHint, _itemStatus, _itemEmpty;
        Image[] _lines;
        readonly Dictionary<string, Sprite> _itemSprites = new Dictionary<string, Sprite>();
        string _shownItem = "?";
        bool _shownItemKeys;
        int _shownStatus = -1;
        float _itemPopAt = -10f, _nextLines, _itemFlashAt = -10f, _itemFlashSeconds = 0.3f, _itemFlashAlpha;
        Color _itemFlashColour = Color.white;

        static readonly Color PanelFill = new Color(1f, 0.97f, 0.88f);
        static readonly Color InkText = new Color(0.12f, 0.1f, 0.09f);

        /// <summary>The item slot is on the screen (tests).</summary>
        public bool ItemSlotShown => _itemRoot != null && _itemRoot.gameObject.activeInHierarchy;
        /// <summary>The name under the picture in the slot: "ROCKET", or "" with empty hands (tests).</summary>
        public string ItemSlotText => ItemSlotShown && _itemName != null ? _itemName.text : "";
        /// <summary>What is on right now, one per line: "SHIELD 6", "SLOW 2" ... (tests).</summary>
        public string ItemStatusText => ItemSlotShown && _itemStatus != null ? _itemStatus.text : "";
        /// <summary>How strong the whole-screen flash is right now, 0..1 (tests).</summary>
        public float ItemScreenFlashNow => _itemFlash != null && _itemFlash.gameObject.activeSelf ? _itemFlash.color.a : 0f;
        /// <summary>The boost item's speed lines are showing (tests).</summary>
        public bool ItemSpeedLinesShown => _speedLines != null && _speedLines.gameObject.activeInHierarchy;

        void BuildItems()
        {
            RectTransform canvas = CanvasRoot;
            if (canvas == null) return;
            // Under every other HUD element: the whole-screen flash and the speed lines.
            _itemFlash = Rect(canvas, "ItemFlash", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4200f, 3200f), Color.clear);
            _itemFlash.transform.SetAsFirstSibling();
            _itemFlash.gameObject.SetActive(false);
            _speedLines = Panel(canvas, "ItemSpeedLines");
            _speedLines.SetSiblingIndex(1);
            _lines = new Image[16];
            for (int i = 0; i < _lines.Length; i++) _lines[i] = Rect(_speedLines, "Line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 200f), new Color(1f, 1f, 1f, 0.6f));
            _speedLines.gameObject.SetActive(false);

            _itemRoot = Panel(canvas, "Items");
            var corner = new Vector2(1f, 0f);
            var slot = new Vector2(-138f, 238f);
            Sprite panel = PanelSprite();
            _itemShadow = Rect(_itemRoot, "ItemShadow", corner, slot + new Vector2(7f, -7f), new Vector2(184f, 184f), new Color(0f, 0f, 0f, 0.4f));
            _itemShadow.sprite = panel;
            _itemPanel = Rect(_itemRoot, "ItemPanel", corner, slot, new Vector2(184f, 184f), Color.white);
            _itemPanel.sprite = panel;
            _itemIcon = Rect(_itemRoot, "ItemIcon", corner, slot + new Vector2(0f, 14f), new Vector2(124f, 124f), Color.white);
            _itemEmpty = Centred("ItemEmpty", slot + new Vector2(0f, 6f), 110, FontStyle.Bold);
            _itemEmpty.text = "?";
            _itemEmpty.color = new Color(0.12f, 0.1f, 0.09f, 0.22f);
            _itemEmpty.GetComponent<Outline>().enabled = false;
            _itemName = Centred("ItemName", slot + new Vector2(0f, -68f), 27, FontStyle.Bold);
            _itemName.color = InkText;
            _itemName.GetComponent<Outline>().enabled = false;
            _itemLabel = Centred("ItemLabel", slot + new Vector2(0f, 110f), 26, FontStyle.Bold);
            _itemLabel.text = "ITEM";
            // right-aligned under the slot, so a long hint never runs off the screen
            _itemHint = MakeText(_itemRoot, "ItemHint", corner, new Vector2(-40f, slot.y - 112f), 23, TextAnchor.MiddleRight, FontStyle.Bold);
            _itemHint.rectTransform.pivot = new Vector2(1f, 0.5f);
            _itemHint.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            _itemStatus = MakeText(_itemRoot, "ItemStatus", corner, slot + new Vector2(-112f, 0f), 34, TextAnchor.MiddleRight, FontStyle.Bold);
            _itemStatus.rectTransform.pivot = new Vector2(1f, 0.5f);
            _itemStatus.supportRichText = true;
            _itemStatus.lineSpacing = 1.05f;
            _itemStatus.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            _itemStatus.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
        }

        Text Centred(string name, Vector2 pos, int size, FontStyle style)
        {
            Text t = MakeText(_itemRoot, name, new Vector2(1f, 0f), pos, size, TextAnchor.MiddleCenter, style);
            t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            t.rectTransform.sizeDelta = new Vector2(420f, size * 1.6f);
            return t;
        }

        /// <summary>A flash over the whole screen that fades out (the bolt): strong when it hits me, light when I threw it.</summary>
        public void ItemScreenFlash(Color colour, float alpha, float seconds)
        {
            if (_itemRoot == null) BuildItems();
            if (_itemFlash == null) return;
            _itemFlashColour = colour;
            _itemFlashAlpha = Mathf.Clamp01(alpha);
            _itemFlashSeconds = Mathf.Max(0.05f, seconds);
            _itemFlashAt = Time.unscaledTime;
            _itemFlash.color = new Color(colour.r, colour.g, colour.b, _itemFlashAlpha);
            _itemFlash.gameObject.SetActive(true);
        }

        /// <summary>Called by RaceItems every frame in race mode (null: race mode is over).</summary>
        public void UpdateItemHud(RaceItems items)
        {
            bool race = items != null && items.race != null && items.race.State != RaceState.Off;
            if (!race)
            {
                if (_itemRoot != null && _itemRoot.gameObject.activeSelf) _itemRoot.gameObject.SetActive(false);
                if (_speedLines != null && _speedLines.gameObject.activeSelf) _speedLines.gameObject.SetActive(false);
                if (_itemFlash != null && _itemFlash.gameObject.activeSelf) _itemFlash.gameObject.SetActive(false);
                return;
            }
            if (_itemRoot == null) BuildItems();
            if (_itemRoot == null) return;
            bool on = items.race.ItemsOn;
            if (_itemRoot.gameObject.activeSelf != on) _itemRoot.gameObject.SetActive(on);
            UpdateItemFlash();
            bool lines = on && items.Boosting && items.rider != null && items.rider.Running;
            if (_speedLines.gameObject.activeSelf != lines) _speedLines.gameObject.SetActive(lines);
            if (lines) PlaceSpeedLines();
            if (!on) return;

            string held = items.Held ?? "";
            bool keys = KeysAllowed;
            if (held != _shownItem || keys != _shownItemKeys)
            {
                if (held != _shownItem && held.Length > 0) _itemPopAt = Time.unscaledTime;
                _shownItem = held;
                _shownItemKeys = keys;
                bool has = held.Length > 0;
                Show(_itemIcon, has);
                Show(_itemEmpty, !has);
                if (has) _itemIcon.sprite = ItemSprite(held);
                Set(_itemName, has ? held.ToUpperInvariant() : "");
                Set(_itemHint, has ? (keys ? "your program says: use  (or E)" : "your program says: use") : "ride into a ? box");
                _itemHint.color = has ? Yellow : new Color(1f, 1f, 1f, 0.85f);
            }
            // The new item pops in.
            float pop = Mathf.Clamp01((Time.unscaledTime - _itemPopAt) / 0.3f);
            _itemIcon.rectTransform.localScale = Vector3.one * (pop < 1f ? Mathf.Lerp(1.3f, 1f, pop * pop * (3f - 2f * pop)) : 1f);

            int shield = Mathf.CeilToInt(items.ShieldLeft), slow = Mathf.CeilToInt(items.SlowLeft), police = Mathf.CeilToInt(items.PoliceLeft);
            bool rocket = items.RocketIn > 0f, boost = items.Boosting;
            bool blink = rocket && ((int)(Time.unscaledTime * 8f) & 1) == 0;
            int key = shield + slow * 16 + police * 256 + (rocket ? 8192 : 0) + (blink ? 16384 : 0) + (boost ? 32768 : 0) + (items.SlowedBy == Messages.ItemOil ? 65536 : 0);
            if (key == _shownStatus) return;
            _shownStatus = key;
            var sb = new System.Text.StringBuilder();
            if (rocket) sb.Append(blink ? "<color=#ff4a3a>ROCKET!</color>\n" : "<color=#ffffff>ROCKET!</color>\n");
            if (shield > 0) sb.Append("<color=#72e6ff>SHIELD  ").Append(shield).Append("</color>\n");
            if (boost) sb.Append("<color=#ff9e26>BOOST</color>\n");
            if (slow > 0) sb.Append(items.SlowedBy == Messages.ItemOil ? "<color=#d1ccE6>" : "<color=#ffe640>").Append("SLOW  ").Append(slow).Append("</color>\n");
            if (police > 0) sb.Append("<color=#80b3ff>POLICE  ").Append(police).Append("</color>\n");
            Set(_itemStatus, sb.ToString().TrimEnd('\n'));
        }

        void UpdateItemFlash()
        {
            if (_itemFlash == null || !_itemFlash.gameObject.activeSelf) return;
            float t = (Time.unscaledTime - _itemFlashAt) / _itemFlashSeconds;
            if (t >= 1f) { _itemFlash.gameObject.SetActive(false); return; }
            _itemFlash.color = new Color(_itemFlashColour.r, _itemFlashColour.g, _itemFlashColour.b, _itemFlashAlpha * (1f - t) * (1f - t));
        }

        // Comic speed lines round the edge of the screen, pointing at the middle, redrawn a few times a second.
        void PlaceSpeedLines()
        {
            if (Time.unscaledTime < _nextLines) return;
            _nextLines = Time.unscaledTime + 0.05f;
            for (int i = 0; i < _lines.Length; i++)
            {
                float a = (i + Random.value) * Mathf.PI * 2f / _lines.Length, far = Random.Range(0.8f, 1.02f);
                RectTransform rt = _lines[i].rectTransform;
                rt.anchoredPosition = new Vector2(Mathf.Cos(a) * 1010f * far, Mathf.Sin(a) * 590f * far);
                rt.sizeDelta = new Vector2(Random.Range(4f, 9f), Random.Range(140f, 300f));
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(Mathf.Sin(a) * 590f, Mathf.Cos(a) * 1010f) * Mathf.Rad2Deg - 90f);
                _lines[i].color = new Color(1f, 1f, 1f, Random.Range(0.3f, 0.7f));
            }
        }

        // ---------------------------------------------------------------- drawn sprites

        // The slot: a cream comic panel with a dark ink border and round corners (white, so the shadow can tint it).
        static Sprite PanelSprite()
        {
            const int n = 128;
            const float radius = 22f, border = 8f;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "ItemPanel" };
            var px = new Color32[n * n];
            Color32 fill = PanelFill, ink = new Color32(30, 24, 20, 255);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // distance inside a rounded square (positive = inside)
                    float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - n * 0.5f) - (n * 0.5f - radius)), dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - n * 0.5f) - (n * 0.5f - radius));
                    float inside = radius - Mathf.Sqrt(dx * dx + dy * dy);
                    if (inside <= 0f) { px[y * n + x] = new Color32(30, 24, 20, 0); continue; }
                    Color32 c = inside < border ? ink : fill;
                    c.a = (byte)(255f * Mathf.Clamp01(inside));
                    px[y * n + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        sealed class IconShape
        {
            public Vector2[] Points;
            public Color32 Colour;
            public bool Ink = true;
        }

        Sprite ItemSprite(string item)
        {
            if (_itemSprites.TryGetValue(item, out Sprite sprite) && sprite != null) return sprite;
            sprite = DrawIcon("Item_" + item, IconShapes(item));
            _itemSprites[item] = sprite;
            return sprite;
        }

        static Vector2[] Circle(float cx, float cy, float rx, float ry, float fromDegrees = 0f, float toDegrees = 360f, int segments = 20)
        {
            bool whole = Mathf.Abs(toDegrees - fromDegrees) >= 359.9f;
            int count = whole ? segments : segments + 1;
            var points = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(fromDegrees, toDegrees, i / (float)segments) * Mathf.Deg2Rad;
                points[i] = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
            }
            return points;
        }

        static Vector2[] With(Vector2 first, Vector2[] rest)
        {
            var points = new Vector2[rest.Length + 1];
            points[0] = first;
            rest.CopyTo(points, 1);
            return points;
        }

        static Vector2[] P(params float[] xy)
        {
            var points = new Vector2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return points;
        }

        // The pictures, in a 0..1 square (y up), back to front.
        static List<IconShape> IconShapes(string item)
        {
            var s = new List<IconShape>();
            void Add(Vector2[] points, Color colour, bool ink = true) => s.Add(new IconShape { Points = points, Colour = colour, Ink = ink });
            var red = new Color(0.92f, 0.18f, 0.14f);
            var navy = new Color(0.15f, 0.21f, 0.42f);
            var yellow = new Color(1f, 0.84f, 0.12f);
            var orange = new Color(1f, 0.52f, 0.1f);
            var white = new Color(0.98f, 0.98f, 0.96f);
            switch (item)
            {
                case Messages.ItemBoost: // two chevrons pointing up
                    Add(P(0.5f, 0.60f, 0.9f, 0.23f, 0.9f, 0.05f, 0.5f, 0.37f, 0.1f, 0.05f, 0.1f, 0.23f), yellow);
                    Add(P(0.5f, 0.93f, 0.9f, 0.56f, 0.9f, 0.36f, 0.5f, 0.70f, 0.1f, 0.36f, 0.1f, 0.56f), orange);
                    break;
                case Messages.ItemShield:
                    Add(P(0.14f, 0.86f, 0.5f, 0.95f, 0.86f, 0.86f, 0.86f, 0.48f, 0.74f, 0.24f, 0.5f, 0.06f, 0.26f, 0.24f, 0.14f, 0.48f), new Color(0.16f, 0.6f, 1f));
                    Add(P(0.25f, 0.77f, 0.5f, 0.84f, 0.5f, 0.2f, 0.35f, 0.31f, 0.25f, 0.5f), new Color(0.56f, 0.88f, 1f), false);
                    break;
                case Messages.ItemOil: // a black drop with a shine
                    Add(With(new Vector2(0.5f, 0.95f), Circle(0.5f, 0.38f, 0.3f, 0.3f, 40f, -220f, 16)), new Color(0.08f, 0.08f, 0.12f));
                    Add(Circle(0.4f, 0.42f, 0.055f, 0.1f), new Color(0.82f, 0.86f, 1f), false);
                    break;
                case Messages.ItemRocket:
                {
                    // drawn nose up, then turned to point up and to the right
                    Vector2[] Turn(Vector2[] points)
                    {
                        for (int i = 0; i < points.Length; i++)
                        {
                            Vector2 p = points[i] * 1.2f;
                            points[i] = new Vector2(0.5f + (p.x + p.y) * 0.7071f, 0.5f + (p.y - p.x) * 0.7071f);
                        }
                        return points;
                    }
                    Add(Turn(P(-0.075f, -0.26f, 0.075f, -0.26f, 0f, -0.5f)), orange);
                    Add(Turn(P(-0.11f, -0.28f, -0.28f, -0.37f, -0.11f, -0.04f)), navy);
                    Add(Turn(P(0.11f, -0.28f, 0.28f, -0.37f, 0.11f, -0.04f)), navy);
                    Add(Turn(P(-0.11f, -0.28f, 0.11f, -0.28f, 0.11f, 0.14f, -0.11f, 0.14f)), red);
                    Add(Turn(P(-0.11f, 0.14f, 0.11f, 0.14f, 0f, 0.4f)), white);
                    Add(Turn(Circle(0f, -0.04f, 0.055f, 0.055f, 0f, 360f, 12)), new Color(0.6f, 0.86f, 1f));
                    break;
                }
                case Messages.ItemBolt:
                    Add(P(0.58f, 0.95f, 0.2f, 0.47f, 0.44f, 0.47f, 0.32f, 0.05f, 0.8f, 0.57f, 0.55f, 0.57f, 0.74f, 0.95f), yellow);
                    break;
                default: // police: a siren, half blue, half red, flashing
                    foreach (float a in new[] { 35f, 90f, 145f })
                    {
                        float r = a * Mathf.Deg2Rad;
                        Vector2 dir = new Vector2(Mathf.Cos(r), Mathf.Sin(r)), side = new Vector2(-dir.y, dir.x) * 0.032f, c = new Vector2(0.5f, 0.27f);
                        Add(new[] { c + dir * 0.36f - side, c + dir * 0.36f + side, c + dir * 0.49f + side, c + dir * 0.49f - side }, yellow);
                    }
                    Add(With(new Vector2(0.5f, 0.27f), Circle(0.5f, 0.27f, 0.27f, 0.27f, 90f, 180f, 10)), new Color(0.2f, 0.45f, 1f));
                    Add(With(new Vector2(0.5f, 0.27f), Circle(0.5f, 0.27f, 0.27f, 0.27f, 0f, 90f, 10)), red);
                    Add(P(0.16f, 0.08f, 0.84f, 0.08f, 0.84f, 0.27f, 0.16f, 0.27f), navy);
                    break;
            }
            return s;
        }

        // Fills the shapes back to front and draws a dark ink line along the edges of the inked ones.
        static Sprite DrawIcon(string name, List<IconShape> shapes)
        {
            const int n = 128;
            const float inkWidth = 0.034f;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = name };
            var px = new Color32[n * n];
            var ink = new Color32(30, 24, 20, 255);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var p = new Vector2((x + 0.5f) / n, (y + 0.5f) / n);
                    bool inside = false;
                    Color32 fill = default;
                    float edge = float.MaxValue;
                    foreach (IconShape shape in shapes)
                    {
                        if (shape.Ink) edge = Mathf.Min(edge, EdgeDistance(shape.Points, p));
                        if (InPolygon(shape.Points, p)) { inside = true; fill = shape.Colour; }
                    }
                    if (edge < inkWidth)
                    {
                        Color32 c = ink;
                        if (!inside) c.a = (byte)(255f * Mathf.Clamp01((inkWidth - edge) * n));
                        px[y * n + x] = c;
                    }
                    else px[y * n + x] = inside ? fill : new Color32(30, 24, 20, 0);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        static bool InPolygon(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) inside = !inside;
            return inside;
        }

        static float EdgeDistance(Vector2[] poly, Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                Vector2 a = poly[j], ab = poly[i] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                best = Mathf.Min(best, (p - (a + ab * t)).sqrMagnitude);
            }
            return Mathf.Sqrt(best);
        }
    }
}
