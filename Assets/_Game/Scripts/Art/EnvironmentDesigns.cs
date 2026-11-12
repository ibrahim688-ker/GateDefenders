using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Su Altı dünyasının ortamı: taş döşeme yol, dalgalı kum zemin, kırık / yosunlu sütunlar,
    /// sol tarafta mercan resifi (mercan, kaya, yosun, deniz çimi, deniz yıldızı) ve şelaleli düşman kapısı.
    /// Yerleşim sabit tohumla üretilir (her oyunda aynı). Statik parçalar birkaç büyük mesh'te birleşir.
    /// Köşe renginin alfası bitkilerin su akıntısında ne kadar sallanacağını belirler (1 = sabit).
    /// </summary>
    public static class EnvironmentDesigns
    {
        // ── Palet ─────────────────────────────────────────────
        static readonly Color StoneA = new Color(0.46f, 0.53f, 0.53f);
        static readonly Color StoneB = new Color(0.35f, 0.43f, 0.45f);
        static readonly Color StoneDark = new Color(0.25f, 0.31f, 0.33f);
        static readonly Color Grout = new Color(0.1f, 0.14f, 0.16f);
        static readonly Color Moss = new Color(0.3f, 0.52f, 0.26f);
        static readonly Color MossDark = new Color(0.2f, 0.38f, 0.22f);
        static readonly Color MossLight = new Color(0.45f, 0.65f, 0.3f);
        static readonly Color Sand = new Color(0.6f, 0.48f, 0.33f);
        static readonly Color SandDark = new Color(0.36f, 0.28f, 0.2f);
        static readonly Color PillarStone = new Color(0.62f, 0.66f, 0.62f);
        static readonly Color PillarDark = new Color(0.46f, 0.51f, 0.49f);
        static readonly Color Crack = new Color(0.12f, 0.15f, 0.16f);
        static readonly Color Rock = new Color(0.42f, 0.47f, 0.52f);
        static readonly Color RockDark = new Color(0.3f, 0.34f, 0.4f);

        static readonly Color[] CoralWarm =
        {
            new Color(0.98f, 0.4f, 0.62f), new Color(1f, 0.58f, 0.32f), new Color(0.68f, 0.38f, 0.9f), new Color(0.95f, 0.3f, 0.35f),
        };
        static readonly Color[] CoralCool =
        {
            new Color(0.2f, 0.62f, 0.64f), new Color(0.3f, 0.46f, 0.8f), new Color(0.58f, 0.66f, 0.3f), new Color(0.72f, 0.55f, 0.35f),
        };

        static System.Random rnd;
        static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        static bool Chance(float p) => rnd.NextDouble() < p;
        static Color Pick(Color[] arr) => arr[rnd.Next(arr.Length)];
        static Color Vary(Color c, float amt) { float k = R(1f - amt, 1f + amt); return new Color(c.r * k, c.g * k, c.b * k, c.a); }
        static Color Sway(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);

        /// <summary>Kum zemininin yüksekliği (dekorlar bu yüksekliğe oturur).</summary>
        public static float SandHeight(float x, float z)
        {
            float edge = Mathf.Clamp01((Mathf.Abs(x) - 4.5f) / 6f);
            return -0.28f + 0.16f * Mathf.Sin(x * 0.35f + z * 0.21f) + 0.1f * Mathf.Sin(z * 0.52f - x * 0.17f)
                   + 0.05f * Mathf.Sin(x * 1.3f + z * 1.1f) - edge * 0.25f;
        }

        /// <summary>Kum yüksekliği; yolun altında derzlerin arasından görünmesin diye bastırılır.</summary>
        static float SandHeightMesh(float x, float z)
        {
            float h = SandHeight(x, z);
            return Mathf.Abs(x) < 5.3f ? Mathf.Min(h, -0.4f) : h;
        }

        public static void Build(Transform env, float hw, float nearZ, float farZ, float gateZ)
        {
            WaterFx.Init();

            var road = new MeshBuilder();
            rnd = new System.Random(11);
            BuildRoad(road, hw, nearZ - 6f, gateZ + 2.5f);
            var roadMesh = road.ToMesh("Env_Road");
            SandstoneTint(roadMesh);   // açık kumtaşı: koyu düşmanlar (zıpkıncı, yengeç) zeminde kaybolmasın
            WaterFx.MakeEnv("Road", roadMesh, env, false);

            var sand = new MeshBuilder();
            BuildSand(sand, -32f, 32f, nearZ - 14f, farZ + 30f, 1.6f);
            WaterFx.MakeEnv("Seabed", sand.ToMesh("Env_Seabed"), env, false);

            var ruins = new MeshBuilder();
            rnd = new System.Random(23);
            BuildPillars(ruins, hw, nearZ, gateZ);
            BuildEnemyGate(ruins, hw, gateZ);
            WaterFx.MakeEnv("Ruins", ruins.ToMesh("Env_Ruins"), env, true);

            var reef = new MeshBuilder();
            rnd = new System.Random(37);
            BuildReef(reef, -15.5f, -hw - 0.9f, nearZ - 6f, gateZ + 3f, true);
            rnd = new System.Random(53);
            BuildReef(reef, hw + 2.6f, 15f, nearZ - 6f, gateZ + 3f, false);
            WaterFx.MakeEnv("Reef", reef.ToMesh("Env_Reef"), env, false);

            var backdrop = new MeshBuilder();
            BuildBackdrop(backdrop, gateZ);
            WaterFx.MakeEnv("Backdrop", backdrop.ToMesh("Env_Backdrop"), env, false);
            BuildSeaCurtain(env, gateZ + 42f);

            BuildGlowLines(env, hw, nearZ - 6f, gateZ);
            BuildWaterfall(env, hw, gateZ);
        }

        // ─────────────────────────────────────────────────────
        // TAŞ YOL
        // ─────────────────────────────────────────────────────
        /// <summary>
        /// Yol taşlarını açık, sıcak kumtaşı tonuna çevirir (oyuncu geri bildirimi: düşmanlar mavi-gri zeminle karışıyordu).
        /// Her köşenin parlaklığı korunur (taş, derz, çatlak deseni aynı kalır); yosun tonları olduğu gibi bırakılır.
        /// </summary>
        static void SandstoneTint(Mesh mesh)
        {
            var cols = mesh.colors;
            if (cols == null || cols.Length == 0) return;
            float mn = 9f, mx = -9f;
            for (int i = 0; i < cols.Length; i++)
            {
                float l = cols[i].r * 0.3f + cols[i].g * 0.59f + cols[i].b * 0.11f;
                if (l < mn) mn = l;
                if (l > mx) mx = l;
            }
            var dark = new Color(0.42f, 0.31f, 0.18f);
            var light = new Color(0.9f, 0.75f, 0.52f);
            for (int i = 0; i < cols.Length; i++)
            {
                Color.RGBToHSV(cols[i], out float h, out float sat, out float v);
                if (h > 0.18f && h < 0.45f && sat > 0.3f) continue;   // yosun
                float l = cols[i].r * 0.3f + cols[i].g * 0.59f + cols[i].b * 0.11f;
                var c = Color.Lerp(dark, light, Mathf.InverseLerp(mn, mx, l));
                c.a = cols[i].a;
                cols[i] = c;
            }
            mesh.colors = cols;
        }

        static void BuildRoad(MeshBuilder b, float hw, float z0, float z1)
        {
            // Derz: taşların arasında görünen koyu taban
            b.Box(new Vector3(0f, -0.2f, (z0 + z1) * 0.5f), new Vector3(hw * 2f + 0.1f, 0.3f, z1 - z0), Grout);

            float lw = hw * 2f / GameManager.LaneCount;
            const float gap = 0.07f;
            float z = z0;
            while (z < z1)
            {
                float rowD = Mathf.Min(R(0.85f, 1.35f), z1 - z);
                if (rowD < 0.4f) break;
                for (int lane = 0; lane < GameManager.LaneCount; lane++)
                {
                    float xStart = -hw + lane * lw + 0.07f;
                    float xEnd = -hw + (lane + 1) * lw - 0.07f;
                    float x = xStart;
                    while (x < xEnd - 0.3f)
                    {
                        float w = R(0.65f, 1.45f);
                        if (xEnd - (x + w) < 0.55f) w = xEnd - x;
                        Tile(b, new Vector3(x + w * 0.5f, 0f, z + rowD * 0.5f), w - gap, rowD - gap);
                        x += w;
                    }
                }
                z += rowD;
            }

            // Kenar taşları (bordür)
            for (int side = -1; side <= 1; side += 2)
            {
                float cz = z0;
                while (cz < z1)
                {
                    float len = R(1.1f, 2.3f);
                    float h = R(0.32f, 0.55f);
                    bool chipped = Chance(0.25f);
                    var c = Vary(StoneDark, 0.12f);
                    var center = new Vector3(side * (hw + 0.32f), h * 0.5f - 0.18f, cz + len * 0.5f);
                    var euler = chipped ? new Vector3(R(-4f, 4f), R(-5f, 5f), R(-6f, 6f)) : new Vector3(0f, R(-1.5f, 1.5f), 0f);
                    b.Box(center, new Vector3(0.62f, chipped ? h * 0.75f : h, len - 0.08f), c, euler, 0.9f, 0.96f);
                    if (Chance(0.35f))
                        MossBlob(b, center + new Vector3(R(-0.1f, 0.1f), (chipped ? h * 0.75f : h) * 0.5f, R(-len * 0.25f, len * 0.25f)), R(0.12f, 0.2f), Vary(MossDark, 0.15f));
                    cz += len;
                }
            }
        }

        static void Tile(MeshBuilder b, Vector3 c, float w, float d)
        {
            float y = -0.08f + R(-0.022f, 0.018f);
            var col = Color.Lerp(StoneA, StoneB, (float)rnd.NextDouble());
            col = Vary(col, 0.07f);
            var euler = Chance(0.2f) ? new Vector3(R(-1.6f, 1.6f), 0f, R(-1.6f, 1.6f)) : Vector3.zero;
            b.Box(new Vector3(c.x, y, c.z), new Vector3(w, 0.2f, d), col, euler, 0.95f, 0.95f);
            float top = y + 0.1f;

            // Yosun lekesi (referanstaki gibi taşların arasına yayılan yeşillik): düzensiz, basık
            if (Chance(0.14f))
            {
                var mc = Vary(Chance(0.7f) ? MossDark : Moss, 0.12f);
                var mp = new Vector3(c.x + R(-w * 0.3f, w * 0.3f), top, c.z + R(-d * 0.3f, d * 0.3f));
                MossBlob(b, mp, R(0.12f, 0.24f), mc);
            }
            // Çatlak
            if (Chance(0.08f))
            {
                float a = R(-60f, 60f);
                b.Box(new Vector3(c.x + R(-w * 0.2f, w * 0.2f), top + 0.004f, c.z), new Vector3(0.035f, 0.01f, d * R(0.4f, 0.8f)), Crack, new Vector3(0f, a, 0f));
            }
        }

/// <summary>Taşın üstüne yayılmış düzensiz, yassı yosun lekesi (üst üste binen çokgen diskler).</summary>
        static void MossBlob(MeshBuilder b, Vector3 p, float r, Color col)
        {
            int n = 2 + rnd.Next(2);
            for (int i = 0; i < n; i++)
            {
                var o = new Vector3(R(-r, r), 0f, R(-r, r)) * 0.8f;
                float rr = r * R(0.6f, 1f);
                b.Cylinder(p + o + Vector3.up * (0.001f + i * 0.002f), p + o + Vector3.up * (0.012f + i * 0.002f), rr, rr * 0.9f, 6, Vary(col, 0.06f), true, true);
            }
        }


        // ─────────────────────────────────────────────────────
        // KUM ZEMİN (düşük poligonlu, fasetli dalgalar)
        // ─────────────────────────────────────────────────────
        static void BuildSand(MeshBuilder b, float x0, float x1, float z0, float z1, float cell)
        {
            int nx = Mathf.CeilToInt((x1 - x0) / cell), nz = Mathf.CeilToInt((z1 - z0) / cell);
            var h = new float[nx + 1, nz + 1];
            var jit = new System.Random(5);
            for (int i = 0; i <= nx; i++)
                for (int j = 0; j <= nz; j++)
                    h[i, j] = SandHeightMesh(x0 + i * cell, z0 + j * cell) + (float)jit.NextDouble() * 0.05f;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float xa = x0 + i * cell, za = z0 + j * cell;
                    var p00 = new Vector3(xa, h[i, j], za);
                    var p10 = new Vector3(xa + cell, h[i + 1, j], za);
                    var p11 = new Vector3(xa + cell, h[i + 1, j + 1], za + cell);
                    var p01 = new Vector3(xa, h[i, j + 1], za + cell);
                    float avg = (p00.y + p11.y) * 0.5f;
                    var col = Color.Lerp(SandDark, Sand, Mathf.InverseLerp(-0.6f, 0f, avg));
                    col = Vary(col, 0.04f);
                    var mid = (p00 + p11) * 0.5f + Vector3.down;
                    b.Face(p00, p10, p11, p01, mid, col);
                }
        }

        // ─────────────────────────────────────────────────────
        // SÜTUNLAR: kırık / sağlam, bazıları yosun sarılı, bazıları çatlak
        // ─────────────────────────────────────────────────────
        struct PillarStyle
        {
            public float broken;   // 0 = sağlam, >0 kırık yükseklik
            public float moss;     // 0..1 yosun yoğunluğu
            public bool cracks;
            public bool fallen;
        }

        static void BuildPillars(MeshBuilder b, float hw, float nearZ, float gateZ)
        {
            // Denge: 7 sütunun 3'ü kırık, 4'ünde yosun; bazıları hem kırık hem yosunlu, biri tertemiz.
            var styles = new[]
            {
                new PillarStyle { broken = 0f, moss = 0.8f, cracks = false },
                new PillarStyle { broken = 2.6f, moss = 0f, cracks = true, fallen = true },
                new PillarStyle { broken = 0f, moss = 0f, cracks = true },
                new PillarStyle { broken = 1.6f, moss = 0.9f, cracks = false, fallen = true },
                new PillarStyle { broken = 0f, moss = 1f, cracks = false },
                new PillarStyle { broken = 2.1f, moss = 0f, cracks = true },
                new PillarStyle { broken = 0f, moss = 0.45f, cracks = true },
            };
            int k = 0;
            for (float z = nearZ + 2f; z < gateZ - 3f; z += 7f, k++)
            {
                var p = new Vector3(hw + 1.45f, 0f, z);
                p.y = Mathf.Min(0f, SandHeight(p.x, p.z) + 0.05f);
                Pillar(b, p, styles[k % styles.Length]);
            }

            // Arka sıra: yıkık sütun kütükleri ve alçak duvar kalıntıları (sağ taraf boş kalmasın)
            for (float z = nearZ + 5.5f; z < gateZ - 2f; z += 9f)
            {
                var p = new Vector3(hw + 7.2f + R(-0.6f, 0.6f), 0f, z + R(-1f, 1f));
                p.y = SandHeight(p.x, p.z) - 0.1f;
                Pillar(b, p, new PillarStyle { broken = R(0.9f, 2f), moss = Chance(0.5f) ? R(0.4f, 1f) : 0f, cracks = Chance(0.5f), fallen = Chance(0.4f) });
                // Alçak duvar parçası
                var wz = z + 4.5f;
                float wl = R(1.5f, 3f);
                var wc = new Vector3(hw + 5.2f, 0f, wz);
                wc.y = SandHeight(wc.x, wc.z);
                for (float t = 0; t < wl; t += 0.75f)
                {
                    float bh = R(0.3f, 0.75f);
                    b.Box(wc + new Vector3(R(-0.05f, 0.05f), bh * 0.5f - 0.1f, t), new Vector3(0.6f, bh, 0.7f), Vary(Color.Lerp(PillarStone, PillarDark, 0.5f), 0.07f),
                        new Vector3(R(-3f, 3f), R(-6f, 6f), R(-3f, 3f)), 0.95f, 0.95f);
                }
            }
        }

        static void Pillar(MeshBuilder b, Vector3 p, PillarStyle s)
        {
            const float r = 0.5f;
            // Kaide
            b.Box(p + new Vector3(0f, 0.12f, 0f), new Vector3(1.55f, 0.5f, 1.55f), Vary(PillarDark, 0.05f));
            b.Box(p + new Vector3(0f, 0.47f, 0f), new Vector3(1.25f, 0.2f, 1.25f), Vary(PillarStone, 0.05f));

            float top = s.broken > 0f ? s.broken : 3.9f;
            b.Cylinder(p + new Vector3(0f, 0.57f, 0f), p + new Vector3(0f, top, 0f), r, r * 0.93f, 10, Vary(PillarStone, 0.04f), true, true);
            // Tambur halkaları
            for (float y = 1.6f; y < top - 0.2f; y += 1.05f)
                b.Cylinder(p + new Vector3(0f, y, 0f), p + new Vector3(0f, y + 0.05f, 0f), r + 0.015f, r + 0.015f, 10, PillarDark, false, true);

            if (s.broken <= 0f)
            {
                // Başlık
                b.Cylinder(p + new Vector3(0f, top, 0f), p + new Vector3(0f, top + 0.18f, 0f), r * 0.95f, 0.68f, 10, PillarStone, true, true);
                b.Box(p + new Vector3(0f, top + 0.31f, 0f), new Vector3(1.4f, 0.26f, 1.4f), Vary(PillarStone, 0.05f));
            }
            else
            {
                // Kırık tepe: kenarda sivri parçalar
                int n = 4;
                float a0 = R(0f, 360f);
                for (int i = 0; i < n; i++)
                {
                    float a = (a0 + i * 360f / n + R(-20f, 20f)) * Mathf.Deg2Rad;
                    float hh = R(0.2f, 0.55f);
                    var c = p + new Vector3(Mathf.Cos(a) * r * 0.62f, top + hh * 0.5f - 0.05f, Mathf.Sin(a) * r * 0.62f);
                    b.Box(c, new Vector3(R(0.3f, 0.45f), hh, 0.3f), PillarStone, new Vector3(R(-12f, 12f), -a * Mathf.Rad2Deg + 90f, R(-15f, 15f)), 0.3f, 0.6f);
                }
                // İçerisi daha koyu (kırık yüzey)
                b.Cylinder(p + new Vector3(0f, top - 0.02f, 0f), p + new Vector3(0f, top + 0.01f, 0f), r * 0.8f, r * 0.8f, 8, PillarDark, true, true);
            }

            if (s.fallen)
            {
                // Yere düşmüş tambur + moloz
                var dir = Quaternion.Euler(0f, R(-40f, 40f), 0f) * Vector3.right;
                var c = p + new Vector3(1.4f, 0f, R(-1.2f, 1.2f));
                c.y = EnvironmentDesigns.SandHeight(c.x, c.z) + 0.38f;
                b.Cylinder(c - dir * 0.5f, c + dir * 0.5f, 0.44f, 0.42f, 9, Vary(PillarStone, 0.06f), true, true);
                for (int i = 0; i < 3; i++)
                {
                    var q = p + new Vector3(R(0.8f, 2.2f), 0f, R(-1.5f, 1.5f));
                    q.y = SandHeight(q.x, q.z) + 0.08f;
                    b.Box(q, new Vector3(R(0.2f, 0.4f), R(0.15f, 0.3f), R(0.2f, 0.4f)), PillarDark, new Vector3(R(0f, 30f), R(0f, 90f), R(0f, 30f)));
                }
            }

            if (s.cracks)
            {
                // Yola bakan yüzde (−x) zikzak çatlaklar
                int cn = 2;
                for (int i = 0; i < cn; i++)
                {
                    float ang = (180f + R(-40f, 40f)) * Mathf.Deg2Rad;
                    float y = R(0.9f, top - 0.6f);
                    for (int sgm = 0; sgm < 3; sgm++)
                    {
                        float a = ang + sgm * R(-0.12f, 0.12f);
                        var c = p + new Vector3(Mathf.Cos(a) * (r + 0.005f), y + sgm * 0.28f, Mathf.Sin(a) * (r + 0.005f));
                        b.Box(c, new Vector3(0.04f, 0.32f, 0.02f), Crack, new Vector3(0f, -a * Mathf.Rad2Deg + 90f, (sgm % 2 == 0 ? 1f : -1f) * R(15f, 30f)));
                    }
                }
            }

            if (s.moss > 0f)
            {
                // Sütuna sarılan yosun sarmalı
                float hMax = (s.broken > 0f ? top : top + 0.2f) * Mathf.Lerp(0.55f, 1f, s.moss);
                float a = R(0f, 6.28f);
                for (float y = 0.6f; y < hMax; y += 0.09f)
                {
                    a += 0.42f;
                    var c = p + new Vector3(Mathf.Cos(a) * (r + 0.03f), y, Mathf.Sin(a) * (r + 0.03f));
                    var col = Vary(Chance(0.5f) ? Moss : MossLight, 0.12f);
                    b.Box(c, new Vector3(0.24f, 0.13f, 0.05f), col, new Vector3(R(-10f, 10f), -a * Mathf.Rad2Deg + 90f, R(-25f, 25f)));
                }
                // Dipte yosun öbekleri
                int clumps = 2 + Mathf.RoundToInt(s.moss * 3f);
                for (int i = 0; i < clumps; i++)
                {
                    float ca = R(0f, 6.28f);
                    b.Sphere(p + new Vector3(Mathf.Cos(ca) * 0.7f, 0.38f, Mathf.Sin(ca) * 0.7f), new Vector3(R(0.2f, 0.35f), R(0.12f, 0.2f), R(0.2f, 0.35f)),
                        Vary(MossDark, 0.1f), 6, 4, default, true);
                }
                // Tepeden sarkan, akıntıda sallanan yosun
                int strands = Mathf.RoundToInt(3 + s.moss * 4f);
                float yTop = s.broken > 0f ? top : top + 0.18f;
                for (int i = 0; i < strands; i++)
                {
                    float sa = R(0f, 6.28f);
                    float rr = s.broken > 0f ? r + 0.02f : 0.72f;
                    var start = p + new Vector3(Mathf.Cos(sa) * rr, yTop, Mathf.Sin(sa) * rr);
                    Strand(b, start, R(0.5f, 1.2f), 0.07f, Vary(Moss, 0.15f), 0.85f);
                }
            }
        }

        /// <summary>Aşağı sarkan, alt ucu daha çok sallanan yosun şeridi.</summary>
        static void Strand(MeshBuilder b, Vector3 start, float len, float width, Color col, float alphaTop)
        {
            const int segs = 3;
            float sl = len / segs;
            for (int i = 0; i < segs; i++)
            {
                float a = Mathf.Lerp(alphaTop, 0.15f, (i + 0.5f) / segs);
                b.Box(start + new Vector3(0f, -sl * (i + 0.5f), 0f), new Vector3(width * (1f - i * 0.2f), sl + 0.02f, 0.02f), Sway(col, a),
                    new Vector3(0f, R(0f, 180f), 0f), 0.85f, 1f);
            }
        }

        // ─────────────────────────────────────────────────────
        // RESİF: mercan, kaya, yosun (kelp), deniz çimi, deniz yıldızı
        // ─────────────────────────────────────────────────────
static void BuildReef(MeshBuilder b, float x0, float x1, float z0, float z1, bool lush)
        {
            // Kümeler halinde yerleşim: her kümede bir "çapa" (büyük kaya / büyük mercan) ve etrafında küçükler.
            // Aralarında boş kum kalır → dağınık konfeti görüntüsü yerine doğal resif adacıkları.
            float cell = lush ? 2.7f : 3.4f;
            for (float z = z0; z < z1; z += cell)
                for (float x = x0; x < x1; x += cell)
                {
                    float nearRoad = lush ? Mathf.InverseLerp(x0, x1, x) : Mathf.InverseLerp(x1, x0, x);
                    float density = lush ? Mathf.Lerp(0.35f, 0.85f, nearRoad) : Mathf.Lerp(0.3f, 0.6f, nearRoad);
                    if (!Chance(density)) continue;
                    var c = new Vector3(x + R(0.3f, cell - 0.3f), 0f, z + R(0.3f, cell - 0.3f));
                    if (lush) ReefCluster(b, c, nearRoad);
                    else RuinCluster(b, c);
                }
        }

        static Vector3 OnSand(Vector3 p) { p.y = SandHeight(p.x, p.z); return p; }

        static void ReefCluster(MeshBuilder b, Vector3 c, float nearRoad)
        {
            float roll = (float)rnd.NextDouble();
            var anchor = OnSand(c);
            if (roll < 0.28f) RockPile(b, anchor, R(1f, 1.8f));
            else if (roll < 0.5f) BranchCoral(b, anchor, R(1.5f, 2.3f), Pick(CoralWarm));
            else if (roll < 0.66f) BrainCoral(b, anchor, R(1.1f, 1.7f), Pick(CoralCool));
            else if (roll < 0.8f) Kelp(b, anchor, R(2.8f, 5f));
            else if (roll < 0.92f) FanCoral(b, anchor, R(1.4f, 2.1f), Pick(CoralWarm));
            else TubeCoral(b, anchor, R(1.3f, 1.8f), Pick(CoralCool));

            int n = 2 + rnd.Next(4);
            for (int i = 0; i < n; i++)
            {
                float a = R(0f, 6.28f), d = R(0.8f, 1.6f);
                var p = OnSand(c + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d));
                if (p.x > -4.9f) continue; // yola taşmasın
                float r2 = (float)rnd.NextDouble();
                if (r2 < 0.3f) SeaGrass(b, p, R(1f, 1.5f));
                else if (r2 < 0.48f) TubeCoral(b, p, R(0.8f, 1.2f), Chance(0.5f) ? Pick(CoralCool) : Pick(CoralWarm));
                else if (r2 < 0.63f) BranchCoral(b, p, R(0.9f, 1.3f), Pick(CoralWarm));
                else if (r2 < 0.78f) RockPile(b, p, R(0.4f, 0.7f));
                else if (r2 < 0.88f) Kelp(b, p, R(1.8f, 3.2f));
                else Starfish(b, p, R(0.9f, 1.3f));
            }
        }

        static void RuinCluster(MeshBuilder b, Vector3 c)
        {
            var anchor = OnSand(c);
            float roll = (float)rnd.NextDouble();
            if (roll < 0.45f) RockPile(b, anchor, R(0.9f, 1.6f));
            else if (roll < 0.75f) Rubble(b, anchor);
            else Kelp(b, anchor, R(2.2f, 4f));
            int n = 1 + rnd.Next(3);
            for (int i = 0; i < n; i++)
            {
                float a = R(0f, 6.28f), d = R(0.8f, 1.5f);
                var p = OnSand(c + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d));
                if (p.x < 6.6f) continue;
                float r2 = (float)rnd.NextDouble();
                if (r2 < 0.5f) SeaGrass(b, p, R(0.9f, 1.3f));
                else if (r2 < 0.8f) RockPile(b, p, R(0.4f, 0.7f));
                else if (r2 < 0.92f) BrainCoral(b, p, R(0.6f, 1f), Pick(CoralCool));
                else Starfish(b, p, R(0.9f, 1.2f));
            }
        }

        static void RockPile(MeshBuilder b, Vector3 p, float s)
        {
            int n = Chance(0.5f) ? 1 : 2 + rnd.Next(2);
            for (int i = 0; i < n; i++)
            {
                float rs = s * (i == 0 ? 1f : R(0.35f, 0.6f));
                var c = p + (i == 0 ? Vector3.zero : new Vector3(R(-0.6f, 0.6f), 0f, R(-0.6f, 0.6f)) * s);
                var radii = new Vector3(R(0.35f, 0.6f), R(0.22f, 0.4f), R(0.35f, 0.6f)) * rs;
                b.Sphere(c + Vector3.up * radii.y * 0.35f, radii, Vary(Color.Lerp(Rock, RockDark, (float)rnd.NextDouble()), 0.06f),
                    6, 4, new Vector3(R(-10f, 10f), R(0f, 360f), R(-10f, 10f)), true);
                if (i == 0 && Chance(0.4f))
                    b.Sphere(c + Vector3.up * radii.y * 0.95f, new Vector3(radii.x * 0.7f, radii.y * 0.3f, radii.z * 0.7f), Vary(Moss, 0.1f), 6, 3, default, true);
            }
        }

        static void Kelp(MeshBuilder b, Vector3 p, float h)
        {
            int stalks = 1 + rnd.Next(3);
            for (int k = 0; k < stalks; k++)
            {
                var basePos = p + new Vector3(R(-0.3f, 0.3f), 0f, R(-0.3f, 0.3f));
                float hh = h * R(0.7f, 1f);
                const int segs = 6;
                float sl = hh / segs;
                var col = Color.Lerp(new Color(0.28f, 0.52f, 0.24f), new Color(0.5f, 0.62f, 0.25f), (float)rnd.NextDouble());
                float yaw = R(0f, 180f);
                float xOff = 0f;
                for (int i = 0; i < segs; i++)
                {
                    float t = (i + 0.5f) / segs;
                    xOff += R(-0.06f, 0.06f);
                    float alpha = Mathf.Lerp(0.95f, 0.05f, t);
                    var c = basePos + new Vector3(xOff, sl * (i + 0.5f), 0f);
                    b.Box(c, new Vector3(Mathf.Lerp(0.2f, 0.12f, t), sl + 0.03f, 0.025f), Sway(Vary(col, 0.08f), alpha), new Vector3(0f, yaw + R(-15f, 15f), R(-6f, 6f)), 0.85f, 1f);
                    // Küçük yaprak
                    if (i > 0 && i % 2 == 0)
                        b.Box(c + new Vector3(0.1f, 0f, 0f), new Vector3(0.18f, 0.08f, 0.02f), Sway(Vary(col, 0.1f), alpha), new Vector3(0f, yaw, R(20f, 40f)));
                }
            }
        }

        static void SeaGrass(MeshBuilder b, Vector3 p, float s)
        {
            int n = 5 + rnd.Next(5);
            var col = Color.Lerp(new Color(0.3f, 0.6f, 0.3f), new Color(0.45f, 0.7f, 0.35f), (float)rnd.NextDouble());
            for (int i = 0; i < n; i++)
            {
                float h = R(0.35f, 0.75f) * s;
                var c = p + new Vector3(R(-0.25f, 0.25f), h * 0.5f, R(-0.25f, 0.25f));
                b.Box(c, new Vector3(0.06f, h, 0.012f), Sway(Vary(col, 0.1f), 0.35f), new Vector3(R(-15f, 15f), R(0f, 180f), R(-18f, 18f)), 0.15f, 1f);
            }
        }

        static void BranchCoral(MeshBuilder b, Vector3 p, float s, Color col)
        {
            var tip = Color.Lerp(col, Color.white, 0.35f);
            var trunkTop = p + Vector3.up * 0.45f * s;
            b.Cylinder(p - Vector3.up * 0.05f, trunkTop, 0.1f * s, 0.075f * s, 6, Vary(col, 0.06f));
            int branches = 3 + rnd.Next(3);
            for (int i = 0; i < branches; i++)
            {
                float a = (i * 360f / branches + R(-25f, 25f)) * Mathf.Deg2Rad;
                var from = p + Vector3.up * R(0.2f, 0.45f) * s;
                var dir = new Vector3(Mathf.Cos(a) * R(0.35f, 0.6f), 1f, Mathf.Sin(a) * R(0.35f, 0.6f)).normalized;
                var to = from + dir * R(0.45f, 0.75f) * s;
                b.Cylinder(from, to, 0.065f * s, 0.045f * s, 5, Vary(col, 0.06f), false);
                b.Sphere(to, Vector3.one * 0.065f * s, tip, 5, 3);
                if (Chance(0.6f))
                {
                    var d2 = (dir + new Vector3(R(-0.5f, 0.5f), 0.3f, R(-0.5f, 0.5f))).normalized;
                    var mid = Vector3.Lerp(from, to, 0.55f);
                    var to2 = mid + d2 * R(0.25f, 0.4f) * s;
                    b.Cylinder(mid, to2, 0.045f * s, 0.035f * s, 5, Vary(col, 0.06f), false);
                    b.Sphere(to2, Vector3.one * 0.05f * s, tip, 5, 3);
                }
            }
        }

        static void BrainCoral(MeshBuilder b, Vector3 p, float s, Color col)
        {
            var radii = new Vector3(0.5f, 0.32f, 0.5f) * s;
            b.Sphere(p + Vector3.up * radii.y * 0.45f, radii, Vary(col, 0.05f), 9, 5, new Vector3(0f, R(0f, 90f), 0f), true);
            // Kıvrımlı oluklar
            var dark = col * 0.6f; dark.a = 1f;
            for (int i = 0; i < 3; i++)
            {
                float a = R(0f, 180f);
                b.Box(p + Vector3.up * radii.y * 1.2f, new Vector3(radii.x * 1.3f, 0.04f, 0.04f), dark, new Vector3(0f, a, 0f));
            }
        }

        static void FanCoral(MeshBuilder b, Vector3 p, float s, Color col)
        {
            int n = 1 + rnd.Next(2);
            for (int i = 0; i < n; i++)
            {
                float h = R(0.7f, 1f) * s;
                b.Box(p + new Vector3(R(-0.2f, 0.2f), h * 0.5f, R(-0.2f, 0.2f)), new Vector3(0.35f * s, h, 0.03f), Sway(Vary(col, 0.08f), 0.7f),
                    new Vector3(R(-8f, 8f), R(0f, 180f), R(-10f, 10f)), 2.6f, 1f);
                b.Cylinder(p, p + Vector3.up * h * 0.35f, 0.04f * s, 0.03f * s, 5, col * 0.7f, false);
            }
        }

        static void TubeCoral(MeshBuilder b, Vector3 p, float s, Color col)
        {
            int n = 4 + rnd.Next(4);
            var rim = Color.Lerp(col, Color.white, 0.4f);
            for (int i = 0; i < n; i++)
            {
                var c = p + new Vector3(R(-0.28f, 0.28f), 0f, R(-0.28f, 0.28f)) * s;
                float h = R(0.25f, 0.85f) * s;
                float r = R(0.06f, 0.11f) * s;
                var top = c + new Vector3(R(-0.08f, 0.08f), h, R(-0.08f, 0.08f));
                b.Cylinder(c - Vector3.up * 0.05f, top, r * 0.9f, r, 6, Vary(col, 0.08f));
                b.Cylinder(top, top + Vector3.up * 0.04f, r * 1.15f, r * 1.15f, 6, rim);
            }
        }

        static void Starfish(MeshBuilder b, Vector3 p, float s)
        {
            var col = Chance(0.5f) ? new Color(1f, 0.45f, 0.25f) : new Color(0.95f, 0.3f, 0.45f);
            float yaw = R(0f, 72f);
            var c = p + Vector3.up * 0.06f;
            for (int i = 0; i < 5; i++)
            {
                float a = (yaw + i * 72f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                b.Box(c + dir * 0.13f * s, new Vector3(0.1f * s, 0.05f * s, 0.26f * s), col, new Vector3(0f, 90f - a * Mathf.Rad2Deg, 0f), 0.4f, 0.3f);
            }
            b.Sphere(c, new Vector3(0.09f, 0.05f, 0.09f) * s, col, 6, 3);
        }

        static void Rubble(MeshBuilder b, Vector3 p)
        {
            int n = 2 + rnd.Next(3);
            for (int i = 0; i < n; i++)
            {
                var c = p + new Vector3(R(-0.6f, 0.6f), 0f, R(-0.6f, 0.6f));
                var sz = new Vector3(R(0.3f, 0.8f), R(0.2f, 0.45f), R(0.3f, 0.7f));
                c.y = SandHeight(c.x, c.z) + sz.y * 0.35f;
                b.Box(c, sz, Vary(Color.Lerp(PillarDark, StoneDark, (float)rnd.NextDouble()), 0.06f), new Vector3(R(-12f, 12f), R(0f, 90f), R(-12f, 12f)), 0.9f, 0.9f);
                if (Chance(0.3f))
                    b.Box(c + Vector3.up * sz.y * 0.5f, new Vector3(sz.x * 0.7f, 0.03f, sz.z * 0.6f), Vary(Moss, 0.1f), new Vector3(0f, R(0f, 90f), 0f), 0.8f, 0.8f);
            }
        }

        // ─────────────────────────────────────────────────────
        // DÜŞMAN KAPISI (taş kapı + şelale)
        // ─────────────────────────────────────────────────────
        /// <summary>Kapı açıklığının yüksekliği: boss (~4.7 m + sallanma) rahatça sığsın.</summary>
        const float GateH = 8.2f;

        static void BuildEnemyGate(MeshBuilder b, float hw, float gz)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (hw + 1.0f);
                float y = 0f;
                int i = 0;
                while (y < GateH - 0.1f)
                {
                    float h = i == 0 ? 1.8f : Mathf.Min(R(1.3f, 1.65f), GateH - y);
                    float w = i == 0 ? 2.15f : R(1.75f, 1.9f);
                    b.Box(new Vector3(x + R(-0.05f, 0.05f), y + h * 0.5f, gz), new Vector3(w, h - 0.04f, w), Vary(i % 2 == 0 ? PillarStone : PillarDark, 0.05f),
                        new Vector3(0f, R(-3f, 3f), 0f), 0.97f, 0.97f);
                    y += h;
                    i++;
                }
                // Yosun
                for (int k = 0; k < 6; k++)
                    b.Sphere(new Vector3(x + R(-0.8f, 0.8f), R(0.3f, GateH - 1f), gz - 0.95f), new Vector3(R(0.2f, 0.35f), R(0.15f, 0.3f), 0.12f), Vary(Moss, 0.12f), 6, 4, default, true);
                Strand(b, new Vector3(x - side * 0.5f, GateH + 0.1f, gz - 1.05f), R(1.2f, 2.2f), 0.1f, Moss, 0.9f);
            }
            // Lento + taçlar
            b.Box(new Vector3(0f, GateH + 0.65f, gz), new Vector3(hw * 2f + 4.4f, 1.3f, 2.2f), Vary(PillarStone, 0.03f));
            b.Box(new Vector3(0f, GateH + 1.45f, gz), new Vector3(hw * 2f + 3.6f, 0.3f, 1.8f), PillarDark);
            for (int k = -2; k <= 2; k++)
                b.Box(new Vector3(k * 2.3f, GateH + 1.9f, gz), new Vector3(0.75f, 0.6f, 1f), Vary(PillarStone, 0.05f), default, 0.7f, 0.7f);
            // Tünel: şelalenin arkası karanlık (boss burada bekler, sonra sudan çıkar)
            b.Box(new Vector3(0f, GateH * 0.5f, gz + 3.2f), new Vector3(hw * 2f + 1f, GateH, 0.3f), new Color(0.01f, 0.03f, 0.05f));
            for (int side = -1; side <= 1; side += 2)
                b.Box(new Vector3(side * (hw + 0.4f), GateH * 0.5f, gz + 2.1f), new Vector3(0.3f, GateH, 2.4f), new Color(0.02f, 0.04f, 0.06f));
            b.Box(new Vector3(0f, GateH + 0.1f, gz + 2.1f), new Vector3(hw * 2f + 1f, 0.2f, 2.4f), new Color(0.02f, 0.04f, 0.06f));
            // Lento önünde sarkan yosunlar
            for (int k = 0; k < 10; k++)
                Strand(b, new Vector3(R(-hw, hw), GateH, gz - 1.12f), R(0.4f, 1.1f), 0.08f, Vary(Moss, 0.12f), 0.9f);
        }

        static void BuildWaterfall(Transform env, float hw, float gz)
        {
            float z = gz - 0.7f;
            float t = GateH - 0.05f;
            var fall = WaterFx.Sheet(new[]
            {
                new Vector3(0f, t, z + 0.1f), new Vector3(0f, t - 0.3f, z - 0.2f), new Vector3(0f, t * 0.72f, z - 0.36f),
                new Vector3(0f, t * 0.4f, z - 0.43f), new Vector3(0f, 0.9f, z - 0.47f), new Vector3(0f, 0.05f, z - 0.49f),
            }, Vector3.right, hw * 2f + 0.5f, 10, 0.05f, 0.95f, "Waterfall");
            // Dipte yola yayılan köpüklü su
            var pool = WaterFx.Sheet(new[]
            {
                new Vector3(0f, 0.05f, z - 0.49f), new Vector3(0f, 0.045f, z - 1.3f), new Vector3(0f, 0.04f, z - 2.5f),
            }, Vector3.right, hw * 2f + 0.5f, 10, 0.95f, 0f, "WaterfallPool");
            WaterFx.MakeWater("Waterfall", WaterFx.Combine("Env_Waterfall", fall, pool), env);
        }

// ─────────────────────────────────────────────────────
        // UZAK SİLUETLER: sise gömülü batık tapınak, kuleler, kayalıklar, dev yosun ormanı
        // (uzun telefon ekranlarında kapının üstünde kalan boş alanı doldurur)
        // ─────────────────────────────────────────────────────
        static void BuildBackdrop(MeshBuilder b, float gz)
        {
            rnd = new System.Random(71);
            var cliff = new Color(0.36f, 0.4f, 0.44f);
            var stone = new Color(0.52f, 0.56f, 0.56f);
            var stoneDark = new Color(0.38f, 0.42f, 0.44f);

            // Kayalık sırtı: ortada tapınağa yer bırakır, kenarlara doğru yükselir
            for (float x = -70f; x <= 70f; x += R(9f, 14f))
            {
                if (Mathf.Abs(x) < 11f) continue;
                float side = Mathf.InverseLerp(11f, 70f, Mathf.Abs(x));
                float z = gz + R(16f, 26f) - side * 6f;
                float h = R(5f, 9f) * Mathf.Lerp(0.8f, 1.4f, side);
                float w = R(7f, 11f);
                var col = Vary(cliff, 0.08f);
                b.Sphere(new Vector3(x, h * 0.3f - 2f, z), new Vector3(w * 0.5f, h * 0.62f, R(3f, 5f)), col, 7, 5,
                    new Vector3(R(-8f, 8f), R(0f, 360f), R(-10f, 10f)), true);
                if (Chance(0.65f))
                    b.Sphere(new Vector3(x + R(-2f, 2f), h * 0.75f, z + R(-1f, 1f)), new Vector3(w * 0.28f, h * 0.35f, R(2f, 3f)), Vary(col, 0.06f), 6, 4,
                        new Vector3(R(-15f, 15f), R(0f, 360f), R(-15f, 15f)), true);
            }

            // Batık tapınak (kapının tam arkasında)
            float tz = gz + 13f;
            b.Box(new Vector3(0f, 0.5f, tz), new Vector3(24f, 3f, 11f), stoneDark);
            b.Box(new Vector3(0f, 2.8f, tz + 0.8f), new Vector3(19f, 1.8f, 9f), stone);
            b.Box(new Vector3(0f, 4.4f, tz + 1.4f), new Vector3(15f, 1.6f, 7f), stoneDark);
            // Revak: sütunlar (bazıları kırık)
            for (int i = 0; i < 7; i++)
            {
                float x = -6.6f + i * 2.2f;
                bool broken = i == 1 || i == 5;
                float top = broken ? R(7f, 9.5f) : 12.2f;
                b.Cylinder(new Vector3(x, 5.2f, tz - 0.8f), new Vector3(x, top, tz - 0.8f), 0.55f, 0.5f, 8, Vary(stone, 0.05f), true, true);
            }
            // Sağ yarısı duran, sol yarısı çökmüş çatı ve alınlık
            b.Box(new Vector3(2.4f, 12.7f, tz), new Vector3(10.5f, 1f, 5.5f), stone, new Vector3(0f, 0f, -2f));
            b.Box(new Vector3(3.2f, 14.6f, tz - 0.9f), new Vector3(9f, 2.8f, 0.8f), stoneDark, new Vector3(0f, 0f, -2f), 0.05f, 1f);
            b.Box(new Vector3(-5.5f, 11.2f, tz - 0.2f), new Vector3(4.5f, 0.9f, 4.5f), stone, new Vector3(4f, 10f, 24f));
            // İçerideki karanlık
            b.Box(new Vector3(0f, 8.6f, tz + 1.6f), new Vector3(13f, 7f, 0.5f), new Color(0.08f, 0.1f, 0.13f));

            // Kuleler
            Tower(b, new Vector3(-17f, 0f, tz + 6f), 26f, 2.1f, false, stone, stoneDark);
            Tower(b, new Vector3(16f, 0f, tz + 3f), 19f, 1.8f, true, stone, stoneDark);
            // Yıkılmış kemer
            b.Box(new Vector3(-24f, 4f, tz + 2f), new Vector3(1.6f, 8f, 1.6f), stoneDark);
            b.Box(new Vector3(-19.5f, 3.5f, tz + 2f), new Vector3(1.6f, 7f, 1.6f), stoneDark);
            b.Box(new Vector3(-21.8f, 8.3f, tz + 2f), new Vector3(6.5f, 1.2f, 1.8f), stone, new Vector3(0f, 0f, 9f));

            // Dev yosun ormanı (kenarlarda, akıntıda yavaşça sallanır)
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 5; i++)
                {
                    var p = new Vector3(side * R(13f, 40f), 0f, gz + R(2f, 16f));
                    p.y = SandHeight(p.x, p.z) - 0.2f;
                    TallKelp(b, p, R(9f, 16f));
                }
        }


        /// <summary>
        /// En arkada açık deniz: ufukta sis rengi, yukarı doğru aydınlanan turkuaz ve yüzeyden süzülen hafif ışıltı.
        /// Sisten etkilenmez; ufuk rengi sisle aynı olduğu için sahneyle kesintisiz birleşir.
        /// </summary>
        static void BuildSeaCurtain(Transform env, float z)
        {
            const float halfW = 140f, y0 = -4f, y1 = 42f;
            var m = new Mesh { name = "Env_SeaCurtain" };
            m.SetVertices(new System.Collections.Generic.List<Vector3>
            {
                new Vector3(-halfW, y0, z), new Vector3(halfW, y0, z), new Vector3(halfW, y1, z), new Vector3(-halfW, y1, z),
            });
            m.SetUVs(0, new System.Collections.Generic.List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.RecalculateBounds();
            var go = new GameObject("SeaCurtain");
            go.transform.SetParent(env, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>();
            var db = GameDatabase.Load();
            var mat = db != null ? db.backdropMaterial : null;
            if (mat == null) mat = new Material(Shader.Find("StratejiGame/SeaBackdrop"));
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static void Tower(MeshBuilder b, Vector3 p, float h, float r, bool broken, Color stone, Color dark)
        {
            b.Box(p + new Vector3(0f, 1f, 0f), new Vector3(r * 2.6f, 2f, r * 2.6f), dark);
            float top = broken ? h * R(0.65f, 0.8f) : h;
            b.Cylinder(p + new Vector3(0f, 2f, 0f), p + new Vector3(0f, top, 0f), r, r * 0.8f, 8, Vary(stone, 0.05f), true, true);
            // Kat bantları ve karanlık pencereler
            for (float y = 6f; y < top - 2f; y += 5.5f)
            {
                float rr = Mathf.Lerp(r, r * 0.8f, (y - 2f) / (top - 2f));
                b.Cylinder(p + new Vector3(0f, y, 0f), p + new Vector3(0f, y + 0.5f, 0f), rr + 0.15f, rr + 0.15f, 8, dark, true, true);
                b.Box(p + new Vector3(0f, y + 2.2f, -rr), new Vector3(0.6f, 1.6f, 0.2f), new Color(0.06f, 0.08f, 0.1f));
            }
            if (broken)
            {
                for (int i = 0; i < 3; i++)
                {
                    float a = (i * 120f + R(-20f, 20f)) * Mathf.Deg2Rad;
                    float hh = R(1.5f, 3.5f);
                    b.Box(p + new Vector3(Mathf.Cos(a) * r * 0.6f, top + hh * 0.5f - 0.3f, Mathf.Sin(a) * r * 0.6f), new Vector3(r * 0.8f, hh, 0.8f), stone,
                        new Vector3(R(-10f, 10f), -a * Mathf.Rad2Deg + 90f, R(-12f, 12f)), 0.3f, 0.6f);
                }
            }
            else
            {
                b.Cylinder(p + new Vector3(0f, top, 0f), p + new Vector3(0f, top + 0.6f, 0f), r * 1.15f, r * 1.15f, 8, dark, true, true);
                b.Cylinder(p + new Vector3(0f, top + 0.6f, 0f), p + new Vector3(0f, top + r * 2.2f, 0f), r * 1.05f, 0f, 8, stone, true, true);
            }
        }

        /// <summary>Uzak plan için kalın ve uzun yosun sapı (üste doğru daha çok sallanır).</summary>
        static void TallKelp(MeshBuilder b, Vector3 p, float h)
        {
            const int segs = 8;
            float sl = h / segs;
            var col = Color.Lerp(new Color(0.22f, 0.36f, 0.22f), new Color(0.32f, 0.42f, 0.24f), (float)rnd.NextDouble());
            float yaw = R(0f, 180f), xOff = 0f;
            for (int i = 0; i < segs; i++)
            {
                float t = (i + 0.5f) / segs;
                xOff += R(-0.25f, 0.25f);
                b.Box(p + new Vector3(xOff, sl * (i + 0.5f), 0f), new Vector3(Mathf.Lerp(0.9f, 0.45f, t), sl + 0.1f, 0.1f),
                    Sway(col, Mathf.Lerp(0.95f, 0.1f, t)), new Vector3(0f, yaw + R(-10f, 10f), R(-4f, 4f)), 0.85f, 1f);
                if (i > 0 && i % 2 == 1)
                    b.Box(p + new Vector3(xOff + 0.5f, sl * (i + 0.5f), 0f), new Vector3(0.8f, 0.3f, 0.06f), Sway(col, Mathf.Lerp(0.95f, 0.1f, t)),
                        new Vector3(0f, yaw, R(20f, 40f)));
            }
        }


        // ─────────────────────────────────────────────────────
        // PARLAYAN ÇİZGİLER: savunma hattı + şerit kanalları
        // ─────────────────────────────────────────────────────
        static void BuildGlowLines(Transform env, float hw, float z0, float gz)
        {
            var b = new MeshBuilder();
            // Savunma hattı: parlak mavi rün çizgisi + yumuşak hale
            b.Box(new Vector3(0f, 0.03f, 0.9f), new Vector3(hw * 2f, 0.01f, 0.1f), new Color(0.35f, 0.7f, 1f));
            b.Box(new Vector3(0f, 0.025f, 0.9f), new Vector3(hw * 2f, 0.01f, 0.45f), new Color(0.06f, 0.16f, 0.3f));
            // Şerit kanalları: taşlar arasındaki derzde çok hafif turkuaz parıltı
            float lw = hw * 2f / GameManager.LaneCount;
            for (int i = 1; i < GameManager.LaneCount; i++)
            {
                float x = -hw + lw * i;
                b.Box(new Vector3(x, -0.044f, (z0 + gz) * 0.5f), new Vector3(0.1f, 0.01f, gz - z0), new Color(0.05f, 0.2f, 0.24f));
            }
            // Kapı lentosundaki rün
            b.Box(new Vector3(0f, GateH + 0.65f, gz - 1.12f), new Vector3(2.4f, 0.35f, 0.01f), new Color(0.2f, 0.75f, 0.9f));
            b.Box(new Vector3(0f, GateH + 0.65f, gz - 1.12f), new Vector3(0.35f, 0.9f, 0.01f), new Color(0.2f, 0.75f, 0.9f), new Vector3(0f, 0f, 45f));
            FxDesigns.MakeFx("GlowLines", b.ToMesh("Env_GlowLines"), env);
        }
    }
}
