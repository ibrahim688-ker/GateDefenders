using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Ana sayfa (Savaş sekmesi) dünya temaları için koddan üretilen dokular ve sprite'lar:
    /// renk geçişi, döşenebilir kar / kabarcık / kıvılcım / kum dokuları, sis-bulut gürültüsü, akan lav,
    /// kaya kenarı, buzlu kenar, buz sarkıtları, yosun, sarmaşık, kum tepeleri, şimşek ve amblem süsleri.
    /// Her doku bir anahtarla önbelleğe alınır; Clear() tema değişince eskileri bırakır.
    /// </summary>
    public static class MenuArt
    {
        static readonly Dictionary<string, Object> cache = new Dictionary<string, Object>();

        public static void Clear()
        {
            foreach (var kv in cache)
            {
                if (kv.Value is Sprite sp && sp != null) Object.Destroy(sp.texture);
                if (kv.Value != null) Object.Destroy(kv.Value);
            }
            cache.Clear();
        }

        static T Cached<T>(string key, System.Func<T> make) where T : Object
        {
            if (cache.TryGetValue(key, out var o) && o != null) return (T)o;
            var v = make();
            cache[key] = v;
            return v;
        }

        // ------------------------------------------------------------------ yardımcılar
        static Texture2D NewTex(int w, int h, bool repeat)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        static Texture2D Finish(Texture2D t, Color32[] px)
        {
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Döşenebilir değer gürültüsü (periyot p hücre).</summary>
        class Noise
        {
            readonly float[][] lat;
            readonly int[] per;

            public Noise(int seed, int basePeriod, int octaves)
            {
                var r = new System.Random(seed);
                lat = new float[octaves][];
                per = new int[octaves];
                for (int o = 0; o < octaves; o++)
                {
                    int p = basePeriod << o;
                    per[o] = p;
                    lat[o] = new float[p * p];
                    for (int i = 0; i < lat[o].Length; i++) lat[o][i] = (float)r.NextDouble();
                }
            }

            float V(int o, float u, float v)
            {
                int p = per[o];
                float x = u * p, y = v * p;
                int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
                float fx = x - x0, fy = y - y0;
                fx = fx * fx * (3f - 2f * fx);
                fy = fy * fy * (3f - 2f * fy);
                int xa = ((x0 % p) + p) % p, xb = (xa + 1) % p, ya = ((y0 % p) + p) % p, yb = (ya + 1) % p;
                var l = lat[o];
                float a = Mathf.Lerp(l[ya * p + xa], l[ya * p + xb], fx);
                float b = Mathf.Lerp(l[yb * p + xa], l[yb * p + xb], fx);
                return Mathf.Lerp(a, b, fy);
            }

            /// <summary>u, v: [0,1) aralığında döşenebilir fbm, sonuç ~[0,1].</summary>
            public float Fbm(float u, float v)
            {
                float sum = 0f, amp = 0.5f, norm = 0f;
                for (int o = 0; o < per.Length; o++)
                {
                    sum += V(o, u, v) * amp;
                    norm += amp;
                    amp *= 0.5f;
                }
                return sum / norm;
            }
        }

        // ------------------------------------------------------------------ zemin
        /// <summary>Dikey renk geçişi (üst → orta → alt).</summary>
        public static Texture2D Gradient(string key, Color top, Color mid, Color bottom) => Cached("grad_" + key, () =>
        {
            var t = NewTex(4, 256, false);
            var px = new Color32[4 * 256];
            for (int y = 0; y < 256; y++)
            {
                float v = y / 255f; // 0 alt, 1 üst
                Color c = v > 0.5f ? Color.Lerp(mid, top, (v - 0.5f) * 2f) : Color.Lerp(bottom, mid, v * 2f);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
            }
            return Finish(t, px);
        });

        // ------------------------------------------------------------------ döşenebilir parçacık dokuları
        /// <summary>Rastgele yumuşak noktalar (kar, kıvılcım). ring=true: içi boş halkalar (kabarcık).</summary>
        public static Texture2D Dots(string key, int size, int count, float rMin, float rMax, bool ring, int seed) => Cached("dots_" + key, () =>
        {
            var t = NewTex(size, size, true);
            var a = new float[size * size];
            var rnd = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float cx = (float)rnd.NextDouble() * size, cy = (float)rnd.NextDouble() * size;
                float r = Mathf.Lerp(rMin, rMax, (float)(rnd.NextDouble() * rnd.NextDouble()));
                float bright = 0.55f + 0.45f * (float)rnd.NextDouble();
                int ext = Mathf.CeilToInt(r + 2f);
                for (int dy = -ext; dy <= ext; dy++)
                    for (int dx = -ext; dx <= ext; dx++)
                    {
                        int px = Mathf.FloorToInt(cx) + dx, py = Mathf.FloorToInt(cy) + dy;
                        float d = Mathf.Sqrt((px + 0.5f - cx) * (px + 0.5f - cx) + (py + 0.5f - cy) * (py + 0.5f - cy));
                        float v;
                        if (ring)
                        {
                            // ince halka + küçük parlak nokta (kabarcık)
                            float w = Mathf.Max(1f, r * 0.16f);
                            v = Mathf.Clamp01(1f - Mathf.Abs(d - r) / w) * 0.9f;
                            float hx = px + 0.5f - (cx - r * 0.4f), hy = py + 0.5f - (cy + r * 0.4f);
                            v = Mathf.Max(v, Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx + hy * hy) / (r * 0.22f)));
                            v = Mathf.Max(v, Mathf.Clamp01(1f - d / r) * 0.12f);
                        }
                        else v = Mathf.Clamp01(1f - d / r) * Mathf.Clamp01(1f - d / r) * 1.4f;
                        v *= bright;
                        int ix = ((px % size) + size) % size, iy = ((py % size) + size) % size;
                        int k = iy * size + ix;
                        if (v > a[k]) a[k] = v;
                    }
            }
            var pxs = new Color32[size * size];
            for (int i = 0; i < pxs.Length; i++) pxs[i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a[i]) * 255));
            return Finish(t, pxs);
        });

        /// <summary>Yatay, uçları incelen kum çizgileri (rüzgâr).</summary>
        public static Texture2D Streaks(string key, int size, int count, int seed) => Cached("streak_" + key, () =>
        {
            var t = NewTex(size, size, true);
            var a = new float[size * size];
            var rnd = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float cx = (float)rnd.NextDouble() * size, cy = (float)rnd.NextDouble() * size;
                float len = size * (0.12f + 0.25f * (float)rnd.NextDouble());
                float th = 0.8f + 1.4f * (float)rnd.NextDouble();
                float bright = 0.4f + 0.6f * (float)rnd.NextDouble();
                for (int dx = Mathf.FloorToInt(-len); dx <= len; dx++)
                {
                    float k = 1f - Mathf.Abs(dx) / len;
                    for (int dy = -3; dy <= 3; dy++)
                    {
                        float v = Mathf.Clamp01(1f - Mathf.Abs(dy) / th) * k * bright;
                        int ix = ((Mathf.FloorToInt(cx) + dx) % size + size) % size;
                        int iy = ((Mathf.FloorToInt(cy) + dy) % size + size) % size;
                        if (v > a[iy * size + ix]) a[iy * size + ix] = v;
                    }
                }
            }
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, (byte)(a[i] * 255));
            return Finish(t, px);
        });

        /// <summary>Yumuşak bulut / sis lekeleri (döşenebilir).</summary>
        public static Texture2D Clouds(string key, int w, int h, float threshold, float soft, int seed) => Cached("cloud_" + key, () =>
        {
            var t = NewTex(w, h, true);
            var n = new Noise(seed, 3, 4);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float f = n.Fbm((float)x / w, (float)y / h);
                    float vv = (float)y / h;
                    // üst ve alt kenarda yumuşakça sönsün (katman kenarı çizgi gibi görünmesin)
                    float fade = Smooth(0f, 0.35f, vv) * (1f - Smooth(0.6f, 1f, vv));
                    float a = Smooth(threshold, threshold + soft, f) * fade;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            return Finish(t, px);
        });

        // ------------------------------------------------------------------ yanardağ
        /// <summary>Akan lav (dikeyde döşenir): dikey uzamış gürültüden koyu kabuk → kırmızı → turuncu → sarı.</summary>
        public static Texture2D Lava() => Cached("lava", () =>
        {
            const int w = 64, h = 256;
            var t = NewTex(w, h, true);
            var n = new Noise(11, 2, 5);
            var crust = new Color(0.22f, 0.04f, 0.02f);
            var red = new Color(0.8f, 0.12f, 0.02f);
            var orange = new Color(1f, 0.48f, 0.05f);
            var yellow = new Color(1f, 0.9f, 0.45f);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // x yönünde sık, y yönünde seyrek örnekleme → aşağı akan çizgiler
                    float u = (float)x / w, v = (float)y / h;
                    // doku 64x256: aynı periyotlu gürültü dikeyde 4 kat uzar → aşağı akan çizgiler (dikeyde kusursuz döşenir)
                    float f = n.Fbm(u, Mathf.Repeat(v + n.Fbm(u * 0.5f, v) * 0.15f, 1f));
                    Color c = f < 0.42f ? Color.Lerp(crust, red, Smooth(0.3f, 0.42f, f))
                        : f < 0.55f ? Color.Lerp(red, orange, Smooth(0.42f, 0.55f, f))
                        : Color.Lerp(orange, yellow, Smooth(0.55f, 0.7f, f));
                    px[y * w + x] = c;
                }
            return Finish(t, px);
        });

        /// <summary>Lav akıntısını çevreleyen tırtıklı kaya şeridi (dikeyde döşenir); lav tarafında kızıl kenar.</summary>
        public static Texture2D RockEdge() => Cached("rock", () =>
        {
            const int w = 64, h = 256;
            var t = NewTex(w, h, true);
            var n = new Noise(21, 4, 4);
            var rock = new Color(0.13f, 0.05f, 0.04f);
            var rockLight = new Color(0.24f, 0.1f, 0.07f);
            var rim = new Color(1f, 0.45f, 0.08f);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = (float)y / h;
                float e1 = 0.12f + 0.22f * n.Fbm(0.13f, v);
                float e2 = 0.55f + 0.38f * n.Fbm(0.71f, v);
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float a = Smooth(e1 - 0.03f, e1 + 0.01f, u) * (1f - Smooth(e2 - 0.01f, e2 + 0.03f, u));
                    var c = Color.Lerp(rock, rockLight, n.Fbm(u * 2f % 1f, v * 2f % 1f));
                    float rimK = 1f - Smooth(0f, 0.1f, u - e1);
                    c = Color.Lerp(c, rim, rimK * 0.85f);
                    c.a = a;
                    px[y * w + x] = c;
                }
            }
            return Finish(t, px);
        });

        // ------------------------------------------------------------------ kar
        /// <summary>Ekran kenarı buzlanması: kenarda yoğun, içe doğru dallanan kristal çizgilerle sönen buz.</summary>
        public static Texture2D FrostSide() => Cached("frost", () =>
        {
            const int w = 160, h = 512;
            var t = NewTex(w, h, false);
            var n = new Noise(31, 3, 5);
            var a = new float[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w, v = (float)y / h;
                    float f = n.Fbm(u * 0.5f, v);
                    float edge = 1f - Smooth(0.05f, 0.62f, u + (f - 0.5f) * 0.45f);
                    // ince "buz çiçeği" dokusu
                    float grain = n.Fbm((u * 3f) % 1f, (v * 3f) % 1f);
                    a[y * w + x] = Mathf.Clamp01(edge * (0.55f + 0.6f * grain));
                }
            // dallanan kristal çizgileri
            var rnd = new System.Random(32);
            for (int i = 0; i < 70; i++)
                Branch(a, w, h, rnd, 0f + (float)rnd.NextDouble() * w * 0.12f, (float)rnd.NextDouble() * h,
                    (float)(rnd.NextDouble() - 0.5) * 1.6f, w * (0.18f + 0.35f * (float)rnd.NextDouble()), 3);
            var px = new Color32[w * h];
            var blue = new Color(0.62f, 0.85f, 1f);
            for (int i = 0; i < px.Length; i++)
            {
                float k = Mathf.Clamp01(a[i]);
                var c = Color.Lerp(blue, Color.white, k);
                px[i] = new Color(c.r, c.g, c.b, k * 0.92f);
            }
            return Finish(t, px);
        });

        static void Branch(float[] a, int w, int h, System.Random rnd, float x, float y, float ang, float len, int depth)
        {
            float dx = Mathf.Cos(ang), dy = Mathf.Sin(ang);
            int steps = Mathf.CeilToInt(len);
            for (int s = 0; s < steps; s++)
            {
                float k = 1f - (float)s / steps;
                int ix = Mathf.FloorToInt(x + dx * s), iy = Mathf.FloorToInt(y + dy * s);
                if (ix < 0 || ix >= w || iy < 0 || iy >= h) break;
                float v = 0.65f * k + 0.25f;
                a[iy * w + ix] = Mathf.Max(a[iy * w + ix], v);
                if (ix + 1 < w) a[iy * w + ix + 1] = Mathf.Max(a[iy * w + ix + 1], v * 0.5f);
                if (depth > 0 && s > 4 && rnd.NextDouble() < 0.06)
                    Branch(a, w, h, rnd, x + dx * s, y + dy * s, ang + (rnd.NextDouble() < 0.5 ? -1f : 1f) * (0.6f + 0.4f * (float)rnd.NextDouble()), len * k * 0.5f, depth - 1);
            }
        }

        /// <summary>Üstten sarkan buz sarkıtları + kar şeridi (yatayda döşenir).</summary>
        public static Texture2D Icicles() => Cached("icicles", () =>
        {
            const int w = 256, h = 96;
            var t = NewTex(w, h, true);
            var rnd = new System.Random(41);
            int count = 11;
            var cx = new float[count]; var cw = new float[count]; var cl = new float[count];
            for (int i = 0; i < count; i++)
            {
                cx[i] = (i + 0.2f + 0.6f * (float)rnd.NextDouble()) / count;
                cw[i] = 0.018f + 0.022f * (float)rnd.NextDouble();
                cl[i] = 0.35f + 0.6f * (float)rnd.NextDouble();
            }
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, d = 1f - (y + 0.5f) / h; // d: üstten aşağı 0..1
                    float cap = 0.16f + 0.05f * Mathf.Sin(u * Mathf.PI * 2f * 5f);
                    float a = d < cap ? 1f : 0f;
                    float shade = 0f;
                    for (int i = 0; i < count; i++)
                    {
                        float du = Mathf.Abs(u - cx[i]);
                        du = Mathf.Min(du, 1f - du);
                        float halfW = cw[i] * (1f - (d - cap) / Mathf.Max(0.01f, cl[i] - cap));
                        if (d >= cap && d <= cl[i] && du <= halfW)
                        {
                            a = 1f;
                            shade = Mathf.Max(shade, du / Mathf.Max(0.001f, halfW));
                        }
                    }
                    var c = Color.Lerp(Color.white, new Color(0.6f, 0.85f, 1f), d < cap ? 0.05f : 0.35f + 0.4f * shade);
                    px[y * w + x] = new Color(c.r, c.g, c.b, a * (d < cap ? 1f : 0.88f));
                }
            return Finish(t, px);
        });

        // ------------------------------------------------------------------ silüet sprite'lar
        static Sprite SpriteFrom(Texture2D t, Vector2 pivot) => Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, 100f);

        /// <summary>w x h boyutlu, 2x2 örneklemeli şekil. inside(u, v): u,v ∈ [0,1], v=0 alt.</summary>
        static Texture2D ShapeTex(int w, int h, System.Func<float, float, float> alpha)
        {
            var t = NewTex(w, h, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float s = 0f;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                            s += alpha((x + 0.25f + sx * 0.5f) / w, (y + 0.25f + sy * 0.5f) / h);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(s * 0.25f) * 255));
                }
            return Finish(t, px);
        }

        /// <summary>Dipten yükselen dalgalı yosun demeti (pivot altta).</summary>
        public static Sprite Seaweed() => Cached("seaweed", () => SpriteFrom(ShapeTex(128, 512, (u, v) =>
        {
            for (int k = 0; k < 4; k++)
            {
                float baseX = 0.22f + k * 0.19f;
                float topV = 0.55f + 0.42f * ((k * 37) % 10) / 10f;
                if (v > topV) continue;
                float cx = baseX + 0.07f * Mathf.Sin(v * 11f + k * 1.7f) * v;
                float hw = 0.055f * Mathf.Pow(1f - v / topV, 0.6f) + 0.008f;
                if (Mathf.Abs(u - cx) <= hw) return 1f;
            }
            return 0f;
        }), new Vector2(0.5f, 0f)));

        /// <summary>Üstten sarkan yapraklı sarmaşık (pivot üstte).</summary>
        public static Sprite Vine() => Cached("vine", () =>
        {
            const int n = 12;
            var lx = new float[n]; var ld = new float[n]; var sd = new float[n];
            for (int i = 1; i < n; i++)
            {
                ld[i] = i * 0.075f;
                sd[i] = (i % 2 == 0) ? 1f : -1f;
                lx[i] = 0.5f + 0.16f * Mathf.Sin(ld[i] * 9f) + sd[i] * 0.16f;
            }
            return SpriteFrom(ShapeTex(96, 512, (u, v) =>
            {
                float d = 1f - v; // üstten
                float cx = 0.5f + 0.16f * Mathf.Sin(d * 9f);
                if (d < 0.93f && Mathf.Abs(u - cx) <= 0.03f + 0.02f * (1f - d)) return 1f;
                // yapraklar: dalın iki yanında sırayla
                for (int i = 1; i < n; i++)
                {
                    if (ld[i] > 0.92f) break;
                    float ey = (d - ld[i] - 0.012f) / 0.035f;
                    if (ey < -1.6f || ey > 1.6f) continue;
                    float ex = (u - lx[i]) / 0.15f;
                    float rx = ex * 0.85f - ey * 0.5f * sd[i], ry = ey * 0.85f + ex * 0.5f * sd[i];
                    if (rx * rx + ry * ry <= 1f) return 1f;
                }
                return 0f;
            }), new Vector2(0.5f, 1f));
        });

        /// <summary>Kum tepeleri silueti (yatayda döşenir); pyramids=true ise üstünde piramitler.</summary>
        public static Texture2D Dunes(string key, int seed, bool pyramids) => Cached("dunes_" + key, () =>
        {
            const int w = 512, h = 160;
            var t = NewTex(w, h, !pyramids);
            float ph = seed * 1.3f;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    float top = 0.42f + 0.16f * Mathf.Sin((u + ph) * Mathf.PI * 2f) + 0.09f * Mathf.Sin((u * 3f + ph * 2f) * Mathf.PI * 2f) + 0.035f * Mathf.Sin(u * 7f * Mathf.PI * 2f);
                    float a = Mathf.Clamp01((top - v) * h * 0.7f + 0.5f);
                    if (pyramids)
                    {
                        a = Mathf.Max(a, Pyr(u, v, 0.22f, 0.5f, 0.1f, 0.42f));
                        a = Mathf.Max(a, Pyr(u, v, 0.33f, 0.47f, 0.065f, 0.27f));
                        a = Mathf.Max(a, Pyr(u, v, 0.78f, 0.5f, 0.05f, 0.2f));
                    }
                    // tepelerin üst kenarı biraz daha açık
                    float lit = Mathf.Clamp01(1f - (top - v) * 14f);
                    byte b = (byte)(200 + 55 * lit);
                    px[y * w + x] = new Color32(b, b, b, (byte)(a * 255));
                }
            return Finish(t, px);
        });

        static float Pyr(float u, float v, float cx, float baseV, float halfW, float height)
        {
            if (v < baseV - 0.2f || v > baseV + height) return 0f;
            float hw = halfW * (1f - (v - baseV) / height);
            return Mathf.Abs(u - cx) <= hw ? 1f : 0f;
        }

        /// <summary>Kırık çizgi şimşek (pivot üstte), kenarları parlayan.</summary>
        public static Sprite Bolt(int seed) => Cached("bolt" + seed, () =>
        {
            var rnd = new System.Random(seed);
            var pts = new List<Vector2> { new Vector2(0.5f, 1f) };
            float x = 0.5f;
            for (int i = 1; i <= 9; i++)
            {
                x = Mathf.Clamp(x + (float)(rnd.NextDouble() - 0.5) * 0.45f, 0.2f, 0.8f);
                pts.Add(new Vector2(x, 1f - i / 9f));
            }
            return SpriteFrom(ShapeTex(64, 256, (u, v) =>
            {
                var p = new Vector2(u * 0.25f, v); // en-boy düzeltmesi (64x256)
                float d = 9f;
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    var a = new Vector2(pts[i].x * 0.25f, pts[i].y);
                    var b = new Vector2(pts[i + 1].x * 0.25f, pts[i + 1].y);
                    var ab = b - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    d = Mathf.Min(d, Vector2.Distance(p, a + ab * t));
                }
                return d < 0.009f ? 1f : Mathf.Clamp01(1f - (d - 0.009f) / 0.03f) * 0.45f;
            }), new Vector2(0.5f, 1f));
        });

        // ------------------------------------------------------------------ amblem süsleri (256, -1..1)
        static Sprite Deco(string key, System.Func<float, float, float> alpha) => Cached("deco_" + key, () =>
            SpriteFrom(ShapeTex(256, 256, (u, v) => alpha(u * 2f - 1f, v * 2f - 1f)), new Vector2(0.5f, 0.5f)));

        /// <summary>Dünyanın küçük rozet süsü (boss barı vb.): kabarcık, alev, yaprak, buz, ışın.</summary>
        public static Sprite ThemeBadge(WorldTheme t)
        {
            switch (t)
            {
                case WorldTheme.Volcano: return FlameCrown();
                case WorldTheme.Swamp: return LeafWreath();
                case WorldTheme.Frost: return IceCrown();
                case WorldTheme.Sky:
                case WorldTheme.Desert: return SunRays();
                default: return BubbleRing();
            }
        }

        /// <summary>Su altı: amblemin çevresinde farklı boyda kabarcık halkaları.</summary>
        public static Sprite BubbleRing()
        {
            var rnd = new System.Random(51);
            const int n = 16;
            var bx = new float[n]; var by = new float[n]; var br = new float[n];
            for (int i = 0; i < n; i++)
            {
                float ang = i / (float)n * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.25f;
                float rr = 0.82f + 0.1f * (float)rnd.NextDouble();
                br[i] = 0.045f + 0.06f * (float)rnd.NextDouble();
                bx[i] = Mathf.Cos(ang) * rr;
                by[i] = Mathf.Sin(ang) * rr;
            }
            return Deco("bubbles", (x, y) =>
            {
                float a = 0f;
                for (int i = 0; i < n; i++)
                {
                    float d = Mathf.Sqrt((x - bx[i]) * (x - bx[i]) + (y - by[i]) * (y - by[i]));
                    if (d > br[i] + 0.03f) continue;
                    a = Mathf.Max(a, Mathf.Clamp01(1f - Mathf.Abs(d - br[i]) / 0.014f));
                    float hx = x - (bx[i] - br[i] * 0.35f), hy = y - (by[i] + br[i] * 0.35f);
                    if (hx * hx + hy * hy < br[i] * br[i] * 0.06f) a = 1f;
                }
                return a;
            });
        }

        /// <summary>Yanardağ: amblemin çevresinde alev dilleri.</summary>
        public static Sprite FlameCrown()
        {
            // Üst yayda yukarı doğru yükselen ayrı alev dilleri + amblemi saran tırtıklı kaya halkası
            const int n = 9;
            var fx = new float[n]; var fy = new float[n]; var fh = new float[n]; var fw = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);                       // 0..1 soldan sağa
                float ang = Mathf.Lerp(200f, -20f, t) * Mathf.Deg2Rad;
                fx[i] = Mathf.Cos(ang) * 0.68f;
                fy[i] = Mathf.Sin(ang) * 0.68f;
                float mid = 1f - Mathf.Abs(t - 0.5f) * 2f;          // ortadakiler daha uzun
                fh[i] = 0.05f + 0.1f * mid + 0.02f * ((i * 7) % 3);
                fw[i] = 0.075f + 0.03f * mid;
            }
            return Deco("flames", (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                float ang = Mathf.Atan2(y, x);
                // kaya halkası: dış kenarı tırtıklı
                float rockOuter = 0.78f + 0.025f * Mathf.Sin(ang * 11f) + 0.015f * Mathf.Sin(ang * 23f + 1f);
                if (r >= 0.64f && r <= rockOuter) return 1f;
                for (int i = 0; i < n; i++)
                {
                    float dx = x - fx[i], dy = y - fy[i];
                    if (dy < -fw[i] || dy > fh[i] + 0.12f) continue;
                    // damla tabanı + yukarı incelen, hafif kıvrılan dil
                    if (dx * dx + dy * dy <= fw[i] * fw[i]) return 1f;
                    if (dy < 0f) continue;
                    float t = dy / (fh[i] + 0.12f);
                    float cx = 0.03f * Mathf.Sin(t * 5f + i);
                    if (Mathf.Abs(dx - cx) <= fw[i] * Mathf.Pow(1f - t, 1.2f)) return 1f;
                }
                return 0f;
            });
        }

        /// <summary>Bataklık: yaprak çelengi.</summary>
        public static Sprite LeafWreath()
        {
            const int n = 18;
            var lc = new float[n]; var ls = new float[n]; var lx = new float[n]; var ly = new float[n];
            for (int i = 0; i < n; i++)
            {
                float ang = i / (float)n * Mathf.PI * 2f;
                float rr = 0.75f + (i % 2 == 0 ? 0.1f : -0.1f);
                lc[i] = Mathf.Cos(ang); ls[i] = Mathf.Sin(ang);
                lx[i] = lc[i] * rr; ly[i] = ls[i] * rr;
            }
            return Deco("leaves", (x, y) =>
            {
                float r2 = x * x + y * y;
                if (r2 > 0.72f * 0.72f && r2 < 0.78f * 0.78f) return 1f;   // sarmaşık halkası
                for (int i = 0; i < n; i++)
                {
                    float dx = x - lx[i], dy = y - ly[i];
                    if (dx * dx + dy * dy > 0.0256f) continue;               // 0.16² dışında yaprak yok
                    // yaprak: halkaya teğet uzun elips
                    float along = -dx * ls[i] + dy * lc[i], across = dx * lc[i] + dy * ls[i];
                    if ((along / 0.15f) * (along / 0.15f) + (across / 0.06f) * (across / 0.06f) <= 1f) return 1f;
                }
                return 0f;
            });
        }

        /// <summary>Kar: buz kristali taç (uzun ve kısa sivri uçlar).</summary>
        public static Sprite IceCrown() => Deco("ice", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float ang = Mathf.Atan2(y, x);
            float big = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(ang * 6f)), 14f);
            float small = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(ang * 6f + Mathf.PI)), 18f);
            float edge = 0.7f + 0.29f * big + 0.14f * small;
            return r <= edge ? 1f : 0f;
        });

        /// <summary>Gökyüzü: tek kanat (sağ kanat; sol için yatay çevrilir). Pivot sol orta.</summary>
        public static Sprite Wing() => Cached("wing", () => SpriteFrom(ShapeTex(256, 192, (u, v) =>
        {
            // kanadın kökü solda (u=0, v=0.6); 5 tüy katmanı yelpaze gibi açılır
            for (int i = 0; i < 6; i++)
            {
                float ang = Mathf.Lerp(0.55f, -0.55f, i / 5f);
                float len = 0.95f - i * 0.11f;
                float dx = Mathf.Cos(ang), dy = Mathf.Sin(ang) * 0.75f;
                float px = u, py = v - 0.62f;
                float along = px * dx + py * dy;
                float across = -px * dy + py * dx;
                if (along < 0f || along > len) continue;
                float hw = 0.075f * Mathf.Sin(Mathf.Clamp01(along / len) * Mathf.PI * 0.9f + 0.15f) + 0.02f;
                if (Mathf.Abs(across) <= hw) return 1f;
            }
            return 0f;
        }), new Vector2(0f, 0.62f)));

        /// <summary>Çöl: güneş ışınları.</summary>
        public static Sprite SunRays() => Deco("sun", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r < 0.66f || r > 0.99f) return 0f;
            float ang = Mathf.Atan2(y, x);
            int n = 16;
            float seg = Mathf.PI * 2f / n;
            float local = Mathf.Abs(Mathf.Repeat(ang + seg * 0.5f, seg) - seg * 0.5f);
            bool longRay = Mathf.RoundToInt(Mathf.Repeat(ang, Mathf.PI * 2f) / seg) % 2 == 0;
            float maxR = longRay ? 0.99f : 0.86f;
            if (r > maxR) return 0f;
            float half = seg * 0.32f * (1f - (r - 0.66f) / (maxR - 0.66f));
            return local <= half ? 1f : 0f;
        });
    }
}
