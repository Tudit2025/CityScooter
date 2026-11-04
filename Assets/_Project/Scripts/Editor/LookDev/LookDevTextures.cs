using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace CityScooter.EditorTools.LookDev
{
    /// <summary>
    /// Procedural textures: the small VFX sprites (always generated) and three tileable placeholder skyline
    /// silhouettes (2048x512, transparent sky; only on request now that the real art is in Art/Textures/Skyline).
    /// Deterministic (fixed seeds), so re-running gives identical PNGs.
    /// </summary>
    static class LookDevTextures
    {
        public static void GenerateVfxSprites(bool overwrite)
        {
            Write(LookDevPaths.SoftDotTexture, overwrite, 64, 64, () => SoftDot(64));
            Write(LookDevPaths.SparkleTexture, overwrite, 64, 64, () => Sparkle(64));
            Write(LookDevPaths.StarTexture, overwrite, 128, 128, () => Star(128));
            Write(LookDevPaths.BlobShadowTexture, overwrite, 128, 128, () => Blob(128));
        }

        [MenuItem("CityScooter/Look Dev/Generate Placeholder Skyline Textures")]
        static void PlaceholdersMenu() => GeneratePlaceholderSkylines(true);

        /// <summary>Writes the placeholder skylines into Art/Textures/Generated (assign them to a rig layer to use them;
        /// they have no baked haze, so give those layers Haze 0.3-0.55).</summary>
        public static void GeneratePlaceholderSkylines(bool overwrite)
        {
            Write(LookDevPaths.SkylineFarTexture, overwrite, SkylineCanvas.Width, SkylineCanvas.Height, FarSkyline);
            Write(LookDevPaths.SkylineMidTexture, overwrite, SkylineCanvas.Width, SkylineCanvas.Height, MidSkyline);
            Write(LookDevPaths.SkylineNearTexture, overwrite, SkylineCanvas.Width, SkylineCanvas.Height, NearSkyline);
        }

        /// <summary>Writes the placeholder skylines as plain PNG files outside the project (for checking the generator).</summary>
        public static void WritePlaceholderSkylinesTo(string folder)
        {
            Directory.CreateDirectory(folder);
            WriteRaw(Path.Combine(folder, "T_Skyline_Far_Placeholder.png"), SkylineCanvas.Width, SkylineCanvas.Height, FarSkyline());
            WriteRaw(Path.Combine(folder, "T_Skyline_Mid_Placeholder.png"), SkylineCanvas.Width, SkylineCanvas.Height, MidSkyline());
            WriteRaw(Path.Combine(folder, "T_Skyline_Near_Placeholder.png"), SkylineCanvas.Width, SkylineCanvas.Height, NearSkyline());
            LookDevUtil.Log("placeholder skylines written to " + folder);
        }

        static void WriteRaw(string file, int width, int height, Color32[] pixels)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply(false);
            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static void Write(string assetPath, bool overwrite, int width, int height, Func<Color32[]> generate)
        {
            if (!overwrite && File.Exists(LookDevUtil.ToFullPath(assetPath))) return;
            WritePng(assetPath, width, height, generate());
            LookDevUtil.Log("wrote " + assetPath);
        }

        /// <summary>Writes RGBA pixels (row 0 = bottom) as PNG and imports it; LookDevTexturePostprocessor sets import options.</summary>
        public static void WritePng(string assetPath, int width, int height, Color32[] pixels)
        {
            LookDevUtil.EnsureFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply(false);
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(LookDevUtil.ToFullPath(assetPath), png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        // ------------------------------------------------------------------ skyline placeholders

        static Color32[] FarSkyline()
        {
            // Perth CBD towers: hazy blue-grey glass and concrete, two clusters with distinctive crowns.
            var rng = new Random(1101);
            var canvas = new SkylineCanvas { BandHeight = 16f, BandColor = Hex("#8C9FB3") };
            Color[] glass = { Hex("#8FA3BA"), Hex("#A0B1C3"), Hex("#7F95AD"), Hex("#AEBBC9"), Hex("#93A9B9") };
            Color window = Hex("#C8D4DF");
            for (int i = 0; i < 46; i++)
            {
                canvas.Add(new Tower
                {
                    Cx = R(rng, 0f, SkylineCanvas.Width), HalfWidth = R(rng, 16f, 42f), Top = R(rng, 40f, 115f),
                    Body = Pick(rng, glass) * 0.97f, Depth = R(rng, 6f, 9f),
                });
            }
            for (int i = 0; i < 24; i++)
            {
                canvas.Add(new Tower
                {
                    Cx = R(rng, 0f, SkylineCanvas.Width), HalfWidth = R(rng, 11f, 28f), Top = R(rng, 130f, 270f),
                    Body = Pick(rng, glass), Window = window, Windows = true,
                    Crown = rng.Next(0, 4) == 0 ? 2 : 0, CrownSize = R(rng, 12f, 30f), Depth = R(rng, 3f, 6f),
                });
            }
            AddTowerCluster(canvas, rng, 700f, 170f, 7, 280f, 420f, glass, window);
            AddTowerCluster(canvas, rng, 1620f, 110f, 4, 220f, 330f, glass, window);
            return canvas.Rasterize();
        }

        static void AddTowerCluster(SkylineCanvas canvas, Random rng, float centre, float spread, int count,
            float minTop, float maxTop, Color[] palette, Color window)
        {
            for (int i = 0; i < count; i++)
            {
                canvas.Add(new Tower
                {
                    Cx = centre + R(rng, -spread, spread), HalfWidth = R(rng, 15f, 30f), Top = R(rng, minTop, maxTop),
                    Body = Pick(rng, palette), Window = window, Windows = true,
                    Crown = 1 + (i % 4), CrownSize = R(rng, 25f, 60f), Depth = R(rng, 0f, 2.5f),
                });
            }
        }

        static Color32[] MidSkyline()
        {
            // Kings Park style tree line with mid-rise blocks poking through.
            var rng = new Random(2202);
            var canvas = new SkylineCanvas { BandHeight = 18f, BandColor = Hex("#6F8A66") };
            Color[] blocks = { Hex("#B9AF9F"), Hex("#A9A398"), Hex("#C6B9A3"), Hex("#A3ABB1"), Hex("#BFB3A0") };
            Color[] leaves = { Hex("#6D9160"), Hex("#5F8657"), Hex("#7A9C66"), Hex("#668B5B") };
            for (int i = 0; i < 16; i++)
            {
                canvas.Add(new Tower
                {
                    Cx = R(rng, 0f, SkylineCanvas.Width), HalfWidth = R(rng, 24f, 58f), Top = R(rng, 90f, 200f),
                    Body = Pick(rng, blocks), Window = Hex("#D6CEBF"), Windows = true,
                    Crown = rng.Next(0, 5) == 0 ? 3 : 0, CrownSize = R(rng, 8f, 18f), Depth = R(rng, 3f, 7f),
                });
            }
            for (int i = 0; i < 80; i++)
            {
                float radius = R(rng, 18f, 48f);
                canvas.Add(new Tree
                {
                    Cx = R(rng, 0f, SkylineCanvas.Width), HalfWidth = radius, Radius = radius,
                    CenterY = R(rng, 40f, 105f), Leaf = Pick(rng, leaves), Depth = R(rng, 0f, 8f),
                });
            }
            return canvas.Rasterize();
        }

        static Color32[] NearSkyline()
        {
            // Suburb: pitched terracotta / Colorbond roofs, street trees, Norfolk pines and palms.
            var rng = new Random(3303);
            var canvas = new SkylineCanvas { BandHeight = 20f, BandColor = Hex("#788A66") };
            Color[] walls = { Hex("#EBDDC5"), Hex("#F2EBDF"), Hex("#DCC9AE"), Hex("#E6D3B6"), Hex("#D8D2C6") };
            Color[] roofs = { Hex("#B5654A"), Hex("#A85D45"), Hex("#6E7B86"), Hex("#8D5B48"), Hex("#C07A55") };
            Color[] leaves = { Hex("#5E8A4F"), Hex("#6A9558"), Hex("#567F49") };
            float x = 0f;
            while (x < SkylineCanvas.Width)
            {
                float half = R(rng, 32f, 58f);
                const float eave = 5f;
                canvas.Add(new House
                {
                    Cx = x + half, HalfWidth = half + eave, Eave = eave, Wall = R(rng, 34f, 58f), Roof = R(rng, 22f, 40f),
                    WallColor = Pick(rng, walls), RoofColor = Pick(rng, roofs), Depth = R(rng, 3f, 6f),
                });
                x += 2f * half + R(rng, 8f, 40f);
            }
            for (int i = 0; i < 20; i++)
            {
                float radius = R(rng, 22f, 40f);
                canvas.Add(new Tree
                {
                    Cx = R(rng, 0f, SkylineCanvas.Width), HalfWidth = radius, Radius = radius,
                    CenterY = R(rng, 48f, 85f), Leaf = Pick(rng, leaves), Depth = R(rng, 0f, 5f),
                });
            }
            for (int i = 0; i < 5; i++)
            {
                canvas.Add(new Pine
                {
                    Cx = R(rng, 0f, SkylineCanvas.Width), HalfWidth = R(rng, 16f, 22f), Height = R(rng, 170f, 235f),
                    Leaf = Hex("#3F6B46"), Trunk = Hex("#6B5A47"), Depth = R(rng, 1f, 4f),
                });
            }
            for (int i = 0; i < 7; i++)
            {
                float crown = R(rng, 24f, 30f);
                float lean = R(rng, -10f, 10f);
                canvas.Add(new Palm
                {
                    Cx = R(rng, 0f, SkylineCanvas.Width), HalfWidth = crown + Math.Abs(lean) + 3f, Height = R(rng, 120f, 175f),
                    Crown = crown, Lean = lean, TrunkColor = Hex("#8D7C62"), LeafColor = Hex("#4F8248"), Depth = R(rng, 0f, 3f),
                });
            }
            return canvas.Rasterize();
        }

        /// <summary>Silhouette canvas: shapes are height fields sampled with 2x2 supersampling; x wraps so it tiles.</summary>
        sealed class SkylineCanvas
        {
            public const int Width = 2048;
            public const int Height = 512;

            public float BandHeight;
            public Color BandColor;
            readonly List<Shape> _shapes = new List<Shape>();

            public void Add(Shape shape) => _shapes.Add(shape);

            public Color32[] Rasterize()
            {
                _shapes.Sort((a, b) => a.Depth.CompareTo(b.Depth)); // front first
                var columns = new List<Shape>[Width];
                for (int x = 0; x < Width; x++) columns[x] = new List<Shape>(8);
                foreach (Shape s in _shapes)
                {
                    int x0 = Mathf.FloorToInt(s.Cx - s.HalfWidth) - 1;
                    int x1 = Mathf.CeilToInt(s.Cx + s.HalfWidth) + 1;
                    for (int x = x0; x <= x1; x++) columns[Mod(x, Width)].Add(s);
                }

                var pixels = new Color32[Width * Height];
                var empty = new Color(BandColor.r, BandColor.g, BandColor.b, 0f);
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        List<Shape> list = columns[x];
                        float r = 0f, g = 0f, b = 0f;
                        int hits = 0;
                        for (int sy = 0; sy < 2; sy++)
                        {
                            float py = y + 0.25f + 0.5f * sy;
                            for (int sx = 0; sx < 2; sx++)
                            {
                                float px = x + 0.25f + 0.5f * sx;
                                if (!TrySample(list, px, py, out Color c)) continue;
                                r += c.r;
                                g += c.g;
                                b += c.b;
                                hits++;
                            }
                        }
                        pixels[y * Width + x] = hits > 0 ? new Color(r / hits, g / hits, b / hits, hits / 4f) : empty;
                    }
                }
                return pixels;
            }

            bool TrySample(List<Shape> list, float px, float py, out Color c)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Shape s = list[i];
                    float dx = px - s.Cx;
                    if (dx > Width * 0.5f) dx -= Width;
                    else if (dx < -Width * 0.5f) dx += Width;
                    if (s.Sample(dx, py, out c)) return true;
                }
                if (py < BandHeight)
                {
                    c = BandColor;
                    return true;
                }
                c = default;
                return false;
            }
        }

        abstract class Shape
        {
            public float Cx;
            public float HalfWidth;
            public float Depth;

            /// <summary>dx: horizontal offset from Cx in pixels; y: pixels above the bottom edge.</summary>
            public abstract bool Sample(float dx, float y, out Color c);

            protected static Color Opaque(Color c)
            {
                c.a = 1f;
                return c;
            }
        }

        sealed class Tower : Shape
        {
            public float Top;
            public Color Body;
            public Color Window;
            public bool Windows;
            /// <summary>0 flat, 1 mast, 2 stepped, 3 sloped, 4 dome.</summary>
            public int Crown;
            public float CrownSize;

            public override bool Sample(float dx, float y, out Color c)
            {
                c = default;
                float adx = Math.Abs(dx);
                if (adx > HalfWidth) return false;
                float top = Top;
                switch (Crown)
                {
                    case 1:
                        if (adx < 1.6f) top = Top + CrownSize;
                        else if (adx < HalfWidth * 0.35f) top = Top + CrownSize * 0.18f;
                        break;
                    case 2:
                        if (adx < HalfWidth * 0.62f) top = Top + CrownSize;
                        break;
                    case 3:
                        top = Top + CrownSize * (dx + HalfWidth) / (2f * HalfWidth);
                        break;
                    case 4:
                        float r = HalfWidth * 0.85f;
                        if (adx < r) top = Top + (float)Math.Sqrt(r * r - dx * dx) * (CrownSize / r);
                        break;
                }
                if (y > top) return false;
                float side = dx < -HalfWidth * 0.25f ? 1.05f : 0.96f; // sunlit left face
                float gradient = 0.9f + 0.12f * Mathf.Clamp01(y / Math.Max(1f, top));
                c = Body * (side * gradient);
                if (Windows && y > 8f && y < Top - 5f && adx < HalfWidth - 2.5f)
                {
                    float wx = Mathf.Repeat(dx + HalfWidth, 6f);
                    float wy = Mathf.Repeat(y, 8f);
                    if (wx < 3.4f && wy < 4f) c = Color.Lerp(c, Window, 0.55f);
                }
                c = Opaque(c);
                return true;
            }
        }

        sealed class Tree : Shape
        {
            public float CenterY;
            public float Radius;
            public Color Leaf;

            public override bool Sample(float dx, float y, out Color c)
            {
                c = default;
                if (Math.Abs(dx) > Radius) return false;
                float top = CenterY + (float)Math.Sqrt(Radius * Radius - dx * dx);
                if (y > top) return false;
                float ny = Mathf.Clamp01((y - (CenterY - Radius)) / (2f * Radius));
                float shade = 0.86f + 0.18f * ny + (dx < 0f ? 0.04f : -0.02f);
                c = Opaque(Leaf * shade);
                return true;
            }
        }

        sealed class House : Shape
        {
            public float Wall;
            public float Roof;
            public float Eave;
            public Color WallColor;
            public Color RoofColor;

            public override bool Sample(float dx, float y, out Color c)
            {
                c = default;
                float adx = Math.Abs(dx);
                if (adx > HalfWidth) return false;
                float roofTop = Wall + Roof * (1f - adx / HalfWidth);
                if (y > roofTop) return false;
                if (y >= Wall - 1.5f)
                {
                    c = Opaque(RoofColor * (dx < 0f ? 1.04f : 0.9f));
                    return true;
                }
                float wallHalf = HalfWidth - Eave;
                if (adx > wallHalf) return false; // under the eaves
                c = WallColor * (dx < 0f ? 1f : 0.92f);
                float wx = Mathf.Repeat(dx + wallHalf, 18f);
                if (y > Wall * 0.35f && y < Wall * 0.7f && wx > 6f && wx < 13f) c = Color.Lerp(c, new Color(0.35f, 0.42f, 0.5f), 0.6f);
                c = Opaque(c);
                return true;
            }
        }

        sealed class Pine : Shape
        {
            public float Height;
            public Color Leaf;
            public Color Trunk;

            public override bool Sample(float dx, float y, out Color c)
            {
                c = default;
                float adx = Math.Abs(dx);
                float trunkTop = Height * 0.18f;
                if (y < trunkTop)
                {
                    if (adx > 2f) return false;
                    c = Opaque(Trunk);
                    return true;
                }
                if (y > Height) return false;
                float t = (y - trunkTop) / (Height - trunkTop);
                float tier = Mathf.Repeat(y, 16f) / 16f; // Norfolk pine tiers: wider at the bottom of each tier
                float width = HalfWidth * (1f - t) * (0.72f + 0.28f * (1f - tier));
                if (adx > width + 1.2f) return false;
                c = Opaque(Leaf * (dx < 0f ? 1.04f : 0.92f));
                return true;
            }
        }

        sealed class Palm : Shape
        {
            public float Height;
            public float Crown;
            public float Lean;
            public Color TrunkColor;
            public Color LeafColor;

            public override bool Sample(float dx, float y, out Color c)
            {
                c = default;
                float h = y / Height;
                if (y <= Height && Math.Abs(dx - Lean * h * h) < 2.2f)
                {
                    c = Opaque(TrunkColor);
                    return true;
                }
                float ox = dx - Lean;
                float oy = y - Height;
                float r = (float)Math.Sqrt(ox * ox + oy * oy);
                if (r > Crown) return false;
                float angle = Mathf.Atan2(oy, ox);
                float frond = 0.45f + 0.55f * Mathf.Abs(Mathf.Cos(angle * 3.5f));
                float droop = oy < 0f ? 0.75f : 1f;
                if (r > Crown * frond * droop) return false;
                c = Opaque(LeafColor * (ox < 0f ? 1.05f : 0.92f));
                return true;
            }
        }

        // ------------------------------------------------------------------ VFX sprites (white RGB, shape in alpha)

        static Color32[] SoftDot(int size)
        {
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = Uv(x, y, size);
                    float r2 = p.sqrMagnitude;
                    float a = r2 >= 1f ? 0f : (1f - r2) * (1f - r2);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            return px;
        }

        static Color32[] Blob(int size)
        {
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = Uv(x, y, size).magnitude;
                    float a = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.12f, 1f, r));
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Pow(a, 1.25f));
                }
            }
            return px;
        }

        static Color32[] Sparkle(int size)
        {
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = Uv(x, y, size);
                    float ax = Mathf.Abs(p.x), ay = Mathf.Abs(p.y), r = p.magnitude;
                    float core = Mathf.Exp(-r * r * 22f);
                    float rays = Mathf.Exp(-ay * 38f) * Sq(Mathf.Clamp01(1f - ax)) + Mathf.Exp(-ax * 38f) * Sq(Mathf.Clamp01(1f - ay));
                    float diagonal = (Mathf.Exp(-Mathf.Abs(p.x - p.y) * 30f) + Mathf.Exp(-Mathf.Abs(p.x + p.y) * 30f))
                        * Sq(Mathf.Clamp01(1f - r * 1.6f)) * 0.35f;
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(core + rays * 0.9f + diagonal));
                }
            }
            return px;
        }

        static Color32[] Star(int size)
        {
            // Chunky 5-point cartoon star: white fill, orange-ish outline (particle colour tints both).
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float radius = i % 2 == 0 ? 0.86f : 0.42f;
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) - 0.06f) * radius;
            }
            var outline = new Color(1f, 0.55f, 0.12f);
            float pixel = 2f / size;
            const float roundness = 0.05f;
            const float outlineWidth = 0.1f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = Uv(x, y, size);
                    float d = float.MaxValue;
                    for (int i = 0; i < points.Length; i++) d = Mathf.Min(d, DistanceToSegment(p, points[i], points[(i + 1) % points.Length]));
                    float sd = (InsidePolygon(p, points) ? -d : d) - roundness;
                    float alpha = Mathf.Clamp01(0.5f - sd / (1.5f * pixel));
                    float fill = Mathf.Clamp01(0.5f - (sd + outlineWidth) / (1.5f * pixel));
                    Color c = Color.Lerp(outline, Color.white, fill);
                    c.a = alpha;
                    px[y * size + x] = c;
                }
            }
            return px;
        }

        // ------------------------------------------------------------------ helpers

        static Vector2 Uv(int x, int y, int size) => new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);

        static float Sq(float v) => v * v;

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        static bool InsidePolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y)
                    && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        static int Mod(int v, int m) => ((v % m) + m) % m;

        static float R(Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

        static Color Pick(Random rng, Color[] palette) => palette[rng.Next(palette.Length)];

        static Color Hex(string html) => LookDevUtil.Hex(html);
    }

    /// <summary>
    /// First-import settings for generated textures and for skyline art in Art/Textures/Skyline (README of the art):
    /// skyline layers = U Repeat / V Clamp, alpha is transparency, mipmaps, max 2048, DXT5 (no crunch: thin cables
    /// and window bands stay clean); sky strips (*_sky*) = clamp, no mipmaps, 256 px, uncompressed (no banding);
    /// VFX sprites = clamp, mipmaps, 256 px. Only applied on a texture's first import, so later manual changes stick.
    /// </summary>
    sealed class LookDevTexturePostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            bool generated = path.StartsWith(LookDevPaths.GeneratedTextures + "/", StringComparison.Ordinal);
            bool skylineArt = path.StartsWith(LookDevPaths.SkylineTextures + "/", StringComparison.Ordinal);
            if (!generated && !skylineArt) return;
            if (!assetImporter.importSettingsMissing) return;

            string file = Path.GetFileName(path);
            bool skyStrip = skylineArt && file.IndexOf("_sky", StringComparison.OrdinalIgnoreCase) >= 0;
            bool skyline = !skyStrip && (skylineArt || file.StartsWith("T_Skyline_", StringComparison.Ordinal));
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 1;
            importer.isReadable = false;
            if (skyStrip)
            {
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 256;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                return;
            }

            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = false;
            if (skyline)
            {
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
            }
            else
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 256;
            }
        }
    }
}
