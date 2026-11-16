using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DataChampions.EditorTools
{
    /// <summary>Icons and shapes painted from signed distance functions (anti-aliased, no art files needed).</summary>
    public static partial class DataChampionsBuilder
    {
        /// <summary>Canvas in "icon units": x runs 0..1 across the width, y runs 0..height/width.</summary>
        class Painter
        {
            public readonly int w, h;
            public readonly Color[] px;

            public Painter(int w, int h = -1)
            {
                this.w = w;
                this.h = h < 0 ? w : h;
                px = new Color[this.w * this.h];
            }

            public void Fill(Func<Vector2, float> sdf, Color c) => FillWith(sdf, _ => c);

            public void FillWith(Func<Vector2, float> sdf, Func<Vector2, Color> color, bool erase = false)
            {
                float aa = 1.2f / w;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        var p = new Vector2((x + 0.5f) / w, (y + 0.5f) / w);
                        float a = Mathf.Clamp01(0.5f - sdf(p) / aa);
                        if (a <= 0f) continue;
                        int i = y * w + x;
                        if (erase) { px[i].a *= 1f - a; continue; }
                        var c = color(p);
                        float sa = c.a * a;
                        float da = px[i].a * (1f - sa);
                        float oa = sa + da;
                        if (oa <= 0f) continue;
                        var rgb = (new Vector3(c.r, c.g, c.b) * sa + new Vector3(px[i].r, px[i].g, px[i].b) * da) / oa;
                        px[i] = new Color(rgb.x, rgb.y, rgb.z, oa);
                    }
            }
        }

        // ------------------------------------------------------------------ distance functions

        static Vector2 V(float x, float y) => new Vector2(x, y);
        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        static float Ellipse(Vector2 p, Vector2 c, Vector2 r)
        {
            var q = new Vector2((p.x - c.x) / r.x, (p.y - c.y) / r.y);
            return (q.magnitude - 1f) * Mathf.Min(r.x, r.y);
        }

        static float Box(Vector2 p, Vector2 c, Vector2 half, float r)
        {
            var d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + Vector2.one * r;
            return new Vector2(Mathf.Max(d.x, 0), Mathf.Max(d.y, 0)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0) - r;
        }

        static float Segment(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a; var ba = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * t).magnitude - r;
        }

        static float Poly(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i];
                var wv = p - v[i];
                var b = wv - e * Mathf.Clamp01(Vector2.Dot(wv, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * wv.y > e.y * wv.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        /// <summary>Ring arc between angles a0..a1 (degrees, counter-clockwise from +x).</summary>
        static float Arc(Vector2 p, Vector2 c, float r, float thick, float a0, float a1)
        {
            var d = p - c;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float mid = (a0 + a1) * 0.5f, half = (a1 - a0) * 0.5f;
            if (Mathf.Abs(Mathf.DeltaAngle(ang, mid)) <= half) return Mathf.Abs(d.magnitude - r) - thick;
            var e1 = c + new Vector2(Mathf.Cos(a0 * Mathf.Deg2Rad), Mathf.Sin(a0 * Mathf.Deg2Rad)) * r;
            var e2 = c + new Vector2(Mathf.Cos(a1 * Mathf.Deg2Rad), Mathf.Sin(a1 * Mathf.Deg2Rad)) * r;
            return Mathf.Min((p - e1).magnitude, (p - e2).magnitude) - thick;
        }

        static Vector2[] StarPts(Vector2 c, float outer, float inner, int points = 5, float rot = 90f)
        {
            var v = new Vector2[points * 2];
            for (int i = 0; i < points * 2; i++)
            {
                float a = (rot + 180f / points * i) * Mathf.Deg2Rad;
                v[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (i % 2 == 0 ? outer : inner);
            }
            return v;
        }

        static Vector2[] Regular(Vector2 c, float r, int n, float rot = 90f)
        {
            var v = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = (rot + 360f / n * i) * Mathf.Deg2Rad;
                v[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return v;
        }

        static Color Hx(uint rgb, float a = 1f) => Palette.Hex(rgb, a);
        static Color Darker(Color c, float k = 0.72f) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        // ------------------------------------------------------------------ saving

        static Sprite Save(string name, Painter pt, bool mips = true, FilterMode filter = FilterMode.Bilinear)
        {
            string path = TexDir + "/" + name + ".png";
            var tex = new Texture2D(pt.w, pt.h, TextureFormat.RGBA32, false);
            tex.SetPixels(pt.px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = mips;
            ti.filterMode = mips ? FilterMode.Trilinear : filter;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = Mathf.Max(pt.w, pt.h) <= 64 ? 64 : Mathf.Max(pt.w, pt.h) <= 128 ? 128 : Mathf.Max(pt.w, pt.h) <= 256 ? 256 : 512;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite Paint(string name, int w, Action<Painter> paint, int h = -1, bool mips = true)
        {
            var pt = new Painter(w, h);
            paint(pt);
            return Save(name, pt, mips);
        }

        static void Add(GameConfig cfg, string name, Sprite s)
        {
            if (s == null) { Debug.LogWarning("[DataChampions] sprite failed: " + name); return; }
            cfg.sprites.Add(new GameConfig.NamedSprite { name = name, sprite = s });
        }

        // ------------------------------------------------------------------ the icon set

        static void BuildIcons(GameConfig cfg)
        {
            cfg.sprites.Clear();
            var white = Color.white;

            // soft circle (radial falloff)
            {
                var pt = new Painter(128);
                for (int y = 0; y < 128; y++)
                    for (int x = 0; x < 128; x++)
                    {
                        float d = Vector2.Distance(new Vector2((x + 0.5f) / 128f, (y + 0.5f) / 128f), V(0.5f, 0.5f)) * 2f;
                        pt.px[y * 128 + x] = new Color(1, 1, 1, Mathf.Clamp01(1f - Mathf.SmoothStep(0.2f, 1f, d)));
                    }
                Add(cfg, "soft", Save("Soft", pt));
            }
            // spotlight beam: narrow at the top, wide and faded at the bottom (128 x 512)
            {
                var pt = new Painter(128, 512);
                for (int y = 0; y < 512; y++)
                    for (int x = 0; x < 128; x++)
                    {
                        float v = y / 511f;                 // 0 bottom .. 1 top
                        float u = (x + 0.5f) / 128f - 0.5f;  // -0.5 .. 0.5
                        float halfW = Mathf.Lerp(0.5f, 0.06f, v);
                        float edge = Mathf.Clamp01(1f - Mathf.Abs(u) / halfW);
                        edge = Mathf.SmoothStep(0f, 1f, edge);
                        float fade = Mathf.SmoothStep(0f, 1f, v) * 0.9f + 0.1f * v;
                        pt.px[y * 128 + x] = new Color(1, 1, 1, edge * fade);
                    }
                Add(cfg, "beam", Save("Beam", pt));
            }
            // studio gradient (8 x 512): violet floor glow to deep indigo top
            {
                var pt = new Painter(8, 512);
                for (int y = 0; y < 512; y++)
                {
                    float v = y / 511f;
                    var c = v < 0.5f ? Color.Lerp(Palette.StudioBottom, Hx(0x351580), v / 0.5f) : Color.Lerp(Hx(0x351580), Palette.StudioTop, (v - 0.5f) / 0.5f);
                    for (int x = 0; x < 8; x++) pt.px[y * 8 + x] = c;
                }
                Add(cfg, "studio_grad", Save("StudioGradient", pt, false));
            }

            Add(cfg, "star", Paint("Star", 256, pt => pt.Fill(p => Poly(p, StarPts(V(0.5f, 0.47f), 0.47f, 0.21f)) - 0.025f, white)));
            Add(cfg, "sparkle", Paint("Sparkle", 128, pt => pt.Fill(p => Poly(p, StarPts(V(0.5f, 0.5f), 0.48f, 0.1f, 4)) - 0.01f, white)));
            Add(cfg, "ring", Paint("Ring", 256, pt => pt.Fill(p => Mathf.Abs(Circle(p, V(0.5f, 0.5f), 0.44f)) - 0.035f, white)));
            Add(cfg, "bulb", Paint("Bulb", 64, pt =>
            {
                pt.FillWith(p => Circle(p, V(0.5f, 0.5f), 0.48f), p => new Color(1, 1, 1, 0.35f * Mathf.Clamp01(1f - (p - V(0.5f, 0.5f)).magnitude / 0.48f)));
                pt.Fill(p => Circle(p, V(0.5f, 0.5f), 0.27f), white);
            }, -1, false));
            Add(cfg, "tri", Paint("Tri", 128, pt => pt.Fill(p => Poly(p, new[] { V(0.08f, 0.12f), V(0.5f, 0.9f), V(0.92f, 0.12f) }) - 0.04f, white)));

            // owl parts (white, tinted in the scene)
            Add(cfg, "wing", Paint("Wing", 128, pt =>
            {
                pt.Fill(p => Mathf.Min(Ellipse(p, V(0.5f, 0.86f), V(0.36f, 0.5f)), Poly(p, new[] { V(0.2f, 0.62f), V(0.5f, 0.06f), V(0.8f, 0.62f) })), white);
                // feather notches
                for (int i = 0; i < 3; i++)
                {
                    float y = 0.42f + i * 0.2f;
                    pt.FillWith(p => Arc(p, V(0.5f, y + 0.1f), 0.18f, 0.018f, 220f, 320f), _ => new Color(1, 1, 1, 0.0f), true);
                }
            }, 192));
            Add(cfg, "tuft", Paint("Tuft", 128, pt => pt.Fill(p => Mathf.Max(Circle(p, V(0.15f, 0.2f), 0.75f), Circle(p, V(0.85f, 0.2f), 0.75f), -p.y + 0.06f), white)));
            Add(cfg, "beak", Paint("Beak", 128, pt => pt.Fill(p => Poly(p, new[] { V(0.1f, 0.9f), V(0.9f, 0.9f), V(0.5f, 0.1f) }) - 0.07f, white)));
            Add(cfg, "happy_eye", Paint("HappyEye", 128, pt => pt.Fill(p => Arc(p, V(0.5f, 0.2f), 0.3f, 0.07f, 20f, 160f), white), 96));
            Add(cfg, "scallop", Paint("Scallop", 128, pt => pt.Fill(p => Arc(p, V(0.5f, 0.4f), 0.3f, 0.06f, 200f, 340f), white), 64));

            // UI glyphs
            Add(cfg, "play", Paint("Play", 128, pt => pt.Fill(p => Poly(p, new[] { V(0.3f, 0.2f), V(0.3f, 0.8f), V(0.82f, 0.5f) }) - 0.05f, white)));
            Add(cfg, "pause", Paint("Pause", 128, pt =>
            {
                pt.Fill(p => Box(p, V(0.35f, 0.5f), V(0.09f, 0.28f), 0.05f), white);
                pt.Fill(p => Box(p, V(0.65f, 0.5f), V(0.09f, 0.28f), 0.05f), white);
            }));
            Add(cfg, "home", Paint("Home", 128, pt =>
            {
                pt.Fill(p => Poly(p, new[] { V(0.12f, 0.52f), V(0.5f, 0.86f), V(0.88f, 0.52f) }) - 0.03f, white);
                pt.Fill(p => Box(p, V(0.5f, 0.36f), V(0.25f, 0.2f), 0.04f), white);
                pt.FillWith(p => Box(p, V(0.5f, 0.27f), V(0.07f, 0.12f), 0.03f), _ => Color.clear, true);
            }));
            Add(cfg, "replay", Paint("Replay", 128, pt =>
            {
                pt.Fill(p => Arc(p, V(0.5f, 0.48f), 0.27f, 0.065f, 60f, 330f), white);
                pt.Fill(p => Poly(p, new[] { V(0.56f, 0.88f), V(0.8f, 0.66f), V(0.5f, 0.6f) }) - 0.02f, white);
            }));
            Add(cfg, "next", Paint("Next", 128, pt =>
            {
                pt.Fill(p => Segment(p, V(0.2f, 0.5f), V(0.62f, 0.5f), 0.08f), white);
                pt.Fill(p => Poly(p, new[] { V(0.5f, 0.22f), V(0.5f, 0.78f), V(0.86f, 0.5f) }) - 0.04f, white);
            }));
            Action<Painter> speaker = pt =>
            {
                pt.Fill(p => Box(p, V(0.24f, 0.5f), V(0.09f, 0.12f), 0.03f), white);
                pt.Fill(p => Poly(p, new[] { V(0.24f, 0.4f), V(0.5f, 0.2f), V(0.5f, 0.8f), V(0.24f, 0.6f) }) - 0.02f, white);
            };
            Add(cfg, "sound_on", Paint("SoundOn", 128, pt =>
            {
                speaker(pt);
                pt.Fill(p => Arc(p, V(0.5f, 0.5f), 0.16f, 0.04f, -50f, 50f), white);
                pt.Fill(p => Arc(p, V(0.5f, 0.5f), 0.3f, 0.04f, -50f, 50f), white);
            }));
            Add(cfg, "speaker", cfg.S("sound_on"));
            Add(cfg, "sound_off", Paint("SoundOff", 128, pt =>
            {
                speaker(pt);
                pt.Fill(p => Segment(p, V(0.62f, 0.36f), V(0.86f, 0.64f), 0.045f), white);
                pt.Fill(p => Segment(p, V(0.62f, 0.64f), V(0.86f, 0.36f), 0.045f), white);
            }));
            Add(cfg, "close", Paint("Close", 128, pt =>
            {
                pt.Fill(p => Segment(p, V(0.28f, 0.28f), V(0.72f, 0.72f), 0.08f), white);
                pt.Fill(p => Segment(p, V(0.28f, 0.72f), V(0.72f, 0.28f), 0.08f), white);
            }));
            Add(cfg, "check", Paint("Check", 128, pt =>
            {
                pt.Fill(p => Segment(p, V(0.2f, 0.52f), V(0.42f, 0.3f), 0.085f), white);
                pt.Fill(p => Segment(p, V(0.42f, 0.3f), V(0.82f, 0.74f), 0.085f), white);
            }));
            Add(cfg, "cross", cfg.S("close"));
            Add(cfg, "question", Paint("Question", 128, pt =>
            {
                pt.Fill(p => Arc(p, V(0.5f, 0.64f), 0.17f, 0.07f, -70f, 190f), white);
                pt.Fill(p => Segment(p, V(0.555f, 0.48f), V(0.5f, 0.39f), 0.07f), white);
                pt.Fill(p => Circle(p, V(0.5f, 0.17f), 0.08f), white);
            }));

            // diagram type minis
            Add(cfg, "mini_picto", Paint("MiniPicto", 128, pt =>
            {
                for (int r = 0; r < 2; r++)
                    for (int c = 0; c < 3 - r; c++)
                    {
                        var cc = V(0.22f + c * 0.28f, 0.66f - r * 0.32f);
                        pt.Fill(p => Circle(p, cc, 0.11f), white);
                    }
            }));
            Add(cfg, "mini_bar", Paint("MiniBar", 128, pt =>
            {
                pt.Fill(p => Box(p, V(0.24f, 0.36f), V(0.1f, 0.22f), 0.03f), white);
                pt.Fill(p => Box(p, V(0.5f, 0.5f), V(0.1f, 0.36f), 0.03f), white);
                pt.Fill(p => Box(p, V(0.76f, 0.28f), V(0.1f, 0.14f), 0.03f), white);
            }));
            Add(cfg, "mini_venn", Paint("MiniVenn", 128, pt =>
            {
                pt.Fill(p => Mathf.Abs(Circle(p, V(0.36f, 0.5f), 0.25f)) - 0.045f, white);
                pt.Fill(p => Mathf.Abs(Circle(p, V(0.64f, 0.5f), 0.25f)) - 0.045f, white);
            }));
            Add(cfg, "mini_carroll", Paint("MiniCarroll", 128, pt =>
            {
                for (int i = 0; i < 4; i++)
                {
                    var cc = V(0.3f + (i % 2) * 0.4f, 0.7f - (i / 2) * 0.4f);
                    pt.Fill(p => Box(p, cc, V(0.16f, 0.16f), 0.04f), white);
                }
            }));

            // beach ball: coloured wedges + 3D-ish highlight and shade
            Add(cfg, "beach_ball", Paint("BeachBall", 256, pt =>
            {
                var c = V(0.5f, 0.5f);
                Color[] wedge = { Hx(0xFF4D5E), Color.white, Hx(0x2FB8FF), Hx(0xFFD43B), Color.white, Hx(0x34D17C) };
                pt.FillWith(p => Circle(p, c, 0.44f), p =>
                {
                    var d = p - V(0.5f, 0.66f);
                    float a = Mathf.Repeat(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 90f, 360f);
                    var col = wedge[Mathf.FloorToInt(a / 60f) % 6];
                    float shade = Mathf.Clamp01(Vector2.Dot((p - c) / 0.44f, V(0.6f, -0.8f)));
                    return Color.Lerp(col, Darker(col, 0.62f), shade * 0.7f);
                });
                pt.Fill(p => Circle(p, V(0.5f, 0.66f), 0.08f), Color.white);
                pt.FillWith(p => Ellipse(p, V(0.36f, 0.66f), V(0.1f, 0.06f)), _ => new Color(1, 1, 1, 0.55f));
                pt.Fill(p => Mathf.Abs(Circle(p, c, 0.44f)) - 0.008f, new Color(0, 0, 0, 0.15f));
            }));

            // starfish with a friendly face
            Add(cfg, "starfish", Paint("Starfish", 256, pt =>
            {
                var c = V(0.5f, 0.48f);
                var outer = StarPts(c, 0.47f, 0.22f);
                pt.Fill(p => Poly(p, outer) - 0.045f, Hx(0xE0701E));
                pt.FillWith(p => Poly(p, outer) - 0.025f, p => Color.Lerp(Hx(0xFFC15A), Hx(0xFF8A2E), Mathf.Clamp01(1f - p.y)));
                var rnd = new System.Random(3);
                for (int i = 0; i < 5; i++)
                {
                    float a = (90f + 72f * i) * Mathf.Deg2Rad;
                    for (int k = 1; k <= 2; k++)
                    {
                        var dc = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (0.13f + k * 0.1f);
                        pt.Fill(p => Circle(p, dc, 0.022f), new Color(1f, 0.95f, 0.8f, 0.9f));
                    }
                }
                pt.Fill(p => Circle(p, V(0.43f, 0.52f), 0.045f), Hx(0x2B2257));
                pt.Fill(p => Circle(p, V(0.57f, 0.52f), 0.045f), Hx(0x2B2257));
                pt.Fill(p => Circle(p, V(0.445f, 0.535f), 0.015f), Color.white);
                pt.Fill(p => Circle(p, V(0.585f, 0.535f), 0.015f), Color.white);
                pt.Fill(p => Arc(p, V(0.5f, 0.46f), 0.05f, 0.012f, 200f, 340f), Hx(0x2B2257));
                pt.FillWith(p => Circle(p, V(0.38f, 0.45f), 0.03f), _ => new Color(1f, 0.4f, 0.5f, 0.5f));
                pt.FillWith(p => Circle(p, V(0.62f, 0.45f), 0.03f), _ => new Color(1f, 0.4f, 0.5f, 0.5f));
            }));

            // Carroll diagram shapes (exact symmetry matters: the regular ones are regular, the others are not)
            void Shape(string name, Func<Vector2, float> sdf, uint rgb)
            {
                var col = Hx(rgb);
                Add(cfg, name, Paint("Shape_" + name, 160, pt =>
                {
                    pt.Fill(p => sdf(p) - 0.012f, Darker(col, 0.7f));
                    pt.FillWith(p => sdf(p) + 0.018f, p => Color.Lerp(col, Color.Lerp(col, Color.white, 0.35f), Mathf.Clamp01(p.y)));
                }));
            }
            Shape("shp_ellipse", p => Ellipse(p, V(0.5f, 0.5f), V(0.44f, 0.28f)), 0xFFE34D);
            Shape("shp_triangle", p => Poly(p, new[] { V(0.5f, 0.92f), V(0.12f, 0.12f), V(0.88f, 0.12f) }), 0xFF4D4D);
            Shape("shp_kite", p => Poly(p, new[] { V(0.5f, 0.94f), V(0.22f, 0.62f), V(0.5f, 0.06f), V(0.78f, 0.62f) }), 0xFF3FA4);
            Shape("shp_rect", p => Box(p, V(0.5f, 0.5f), V(0.42f, 0.22f), 0.01f), 0x7FD34A);
            Shape("shp_star", p => Poly(p, StarPts(V(0.5f, 0.47f), 0.47f, 0.2f)), 0xFF9B26);
            Shape("shp_pentagon", p => Poly(p, Regular(V(0.5f, 0.47f), 0.45f, 5)), 0xFF4FA0);
            Shape("shp_sliver", p => Poly(p, new[] { V(0.04f, 0.6f), V(0.96f, 0.8f), V(0.46f, 0.5f) }), 0xFFD43B);
            Shape("shp_scalene1", p => Poly(p, new[] { V(0.08f, 0.1f), V(0.94f, 0.3f), V(0.3f, 0.92f) }), 0x21B5A8);
            Shape("shp_quad", p => Poly(p, new[] { V(0.14f, 0.32f), V(0.64f, 0.08f), V(0.92f, 0.56f), V(0.34f, 0.86f) }), 0x3C3CC8);
            Shape("shp_scalene2", p => Poly(p, new[] { V(0.04f, 0.56f), V(0.96f, 0.16f), V(0.62f, 0.86f) }), 0xFF8A3D);
            Shape("shp_scalene3", p => Poly(p, new[] { V(0.12f, 0.12f), V(0.92f, 0.22f), V(0.22f, 0.88f) }), 0x55C7F7);
            Shape("shp_hexa", p => Poly(p, new[] { V(0.06f, 0.5f), V(0.32f, 0.84f), V(0.68f, 0.76f), V(0.95f, 0.56f), V(0.74f, 0.14f), V(0.2f, 0.24f) }), 0xFF8A3D);
            Shape("shp_penta_irr", p => Poly(p, new[] { V(0.08f, 0.18f), V(0.54f, 0.36f), V(0.92f, 0.08f), V(0.76f, 0.88f), V(0.24f, 0.7f) }), 0xFF4F8B);
            Shape("shp_hexb", p => Poly(p, new[] { V(0.05f, 0.42f), V(0.3f, 0.86f), V(0.74f, 0.92f), V(0.96f, 0.6f), V(0.8f, 0.1f), V(0.36f, 0.18f) }), 0x8B5CF6);

            // Fluent emoji (MIT) imported from Art/Icons
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Art/Icons" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                bool dirty = ti.textureType != TextureImporterType.Sprite || !ti.mipmapEnabled || ti.maxTextureSize != 256;
                if (dirty)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.alphaIsTransparency = true;
                    ti.mipmapEnabled = true;
                    ti.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                    ti.filterMode = FilterMode.Trilinear;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.maxTextureSize = 256;
                    ti.textureCompression = TextureImporterCompression.Compressed;
                    ti.SaveAndReimport();
                }
                Add(cfg, Path.GetFileNameWithoutExtension(path), AssetDatabase.LoadAssetAtPath<Sprite>(path));
            }
        }
    }
}
