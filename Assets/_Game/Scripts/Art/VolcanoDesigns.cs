using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Yanardağ dünyasının ortamı: bazalt döşeme yol (derzlerde sönük lav ışıltısı), kül zemin,
    /// solda kıvrılarak oyuncuya doğru akan lav nehri, sağda lav damarlı kırık bazalt sütunlar,
    /// altıgen bazalt kolon kümeleri, kömürleşmiş ağaçlar, obsidyen kristaller, lav gölcükleri,
    /// lav şelaleli düşman kapısı ve arkada püsküren yanardağ.
    /// Köşe renginin alfası parıltıdır (0 = tam parlar); ortam materyalinin _Glow değeriyle çarpılır.
    /// </summary>
    public static class VolcanoDesigns
    {
        // ── Palet ─────────────────────────────────────────────
        static readonly Color BasaltA = new Color(0.25f, 0.21f, 0.2f);
        static readonly Color BasaltB = new Color(0.19f, 0.16f, 0.16f);
        static readonly Color BasaltDark = new Color(0.12f, 0.1f, 0.1f);
        static readonly Color BasaltLight = new Color(0.34f, 0.29f, 0.27f);
        static readonly Color Ash = new Color(0.33f, 0.3f, 0.28f);
        static readonly Color AshDark = new Color(0.16f, 0.13f, 0.12f);
        static readonly Color Scorch = new Color(0.3f, 0.12f, 0.06f);
        static readonly Color Char = new Color(0.07f, 0.055f, 0.05f);
        static readonly Color Obsidian = new Color(0.09f, 0.07f, 0.12f);
        static readonly Color ObsidianLight = new Color(0.3f, 0.22f, 0.4f);
        static readonly Color Mountain = new Color(0.16f, 0.09f, 0.08f);
        // Parlayanlar (alfa = parıltı)
        static readonly Color Lava = new Color(0.5f, 0.16f, 0.03f, 0f);
        static readonly Color LavaHot = new Color(0.45f, 0.3f, 0.1f, 0f);
        static readonly Color Ember = new Color(0.35f, 0.08f, 0.02f, 0.5f);

        public static readonly Color Fog = new Color(0.1f, 0.035f, 0.025f);

        const float GateH = 8.2f;
        const float RiverHalf = 1.35f;
        const float LavaY = -0.55f;

        static System.Random rnd;
        static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        static bool Chance(float p) => rnd.NextDouble() < p;
        static Color Vary(Color c, float amt) { float k = R(1f - amt, 1f + amt); return new Color(c.r * k, c.g * k, c.b * k, c.a); }

        /// <summary>Lav nehrinin orta çizgisi (solda, yola paralel kıvrılır).</summary>
        public static float RiverX(float z) => -9.2f + 0.9f * Mathf.Sin(z * 0.13f) + 0.4f * Mathf.Sin(z * 0.31f + 1f);

        /// <summary>Kül zeminin yüksekliği; nehir boyunca oyulmuş kanal.</summary>
        public static float GroundHeight(float x, float z)
        {
            float h = EnvironmentDesigns.SandHeight(x, z) * 0.85f - 0.04f;
            float d = Mathf.Abs(x - RiverX(z));
            if (d < RiverHalf + 1f)
            {
                float t = Mathf.InverseLerp(RiverHalf + 1f, RiverHalf - 0.2f, d);
                h = Mathf.Lerp(h, -0.9f, Mathf.SmoothStep(0f, 1f, t));
            }
            return h;
        }

        static float GroundMesh(float x, float z)
        {
            float h = GroundHeight(x, z);
            return Mathf.Abs(x) < 5.3f ? Mathf.Min(h, -0.4f) : h;
        }

        static Vector3 OnGround(Vector3 p) { p.y = GroundHeight(p.x, p.z); return p; }
        static bool InRiver(float x, float z, float margin) => Mathf.Abs(x - RiverX(z)) < RiverHalf + margin;

        public static void Build(Transform env, float hw, float nearZ, float farZ, float gateZ)
        {
            WaterFx.Init();

            var road = new MeshBuilder();
            rnd = new System.Random(11);
            BuildRoad(road, hw, nearZ - 6f, gateZ + 2.5f);
            WaterFx.MakeEnv("Road", road.ToMesh("Vol_Road"), env, false);

            var ground = new MeshBuilder();
            rnd = new System.Random(5);
            BuildGround(ground, -34f, 34f, nearZ - 14f, farZ + 30f, 1.2f);
            WaterFx.MakeEnv("Ground", ground.ToMesh("Vol_Ground"), env, false);

            var ruins = new MeshBuilder();
            rnd = new System.Random(23);
            BuildPillars(ruins, hw, nearZ, gateZ);
            BuildEnemyGate(ruins, hw, gateZ);
            WaterFx.MakeEnv("Ruins", ruins.ToMesh("Vol_Ruins"), env, true);

            var wild = new MeshBuilder();
            rnd = new System.Random(37);
            BuildLeft(wild, hw, nearZ - 8f, gateZ + 3f);
            rnd = new System.Random(53);
            BuildRight(wild, hw, nearZ - 6f, gateZ + 3f);
            WaterFx.MakeEnv("Wilds", wild.ToMesh("Vol_Wilds"), env, false);

            var back = new MeshBuilder();
            BuildBackdrop(back, gateZ);
            WaterFx.MakeEnv("Backdrop", back.ToMesh("Vol_Backdrop"), env, false);
            BuildSky(env, gateZ + 42f);

            BuildGlowLines(env, hw, nearZ - 6f, gateZ);
            BuildLava(env, hw, nearZ, gateZ);
        }

        // ─────────────────────────────────────────────────────
        // BAZALT YOL
        // ─────────────────────────────────────────────────────
        static void BuildRoad(MeshBuilder b, float hw, float z0, float z1)
        {
            // Derz: taşların arasında sönük lav ışıltısı
            b.Box(new Vector3(0f, -0.2f, (z0 + z1) * 0.5f), new Vector3(hw * 2f + 0.1f, 0.3f, z1 - z0), new Color(0.16f, 0.04f, 0.012f, 0.85f));

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

            // Bordür: iri bazalt blokları, bazılarında lav çatlağı
            for (int side = -1; side <= 1; side += 2)
            {
                float cz = z0;
                while (cz < z1)
                {
                    float len = R(1.1f, 2.3f);
                    float h = R(0.32f, 0.55f);
                    bool chipped = Chance(0.25f);
                    float hh = chipped ? h * 0.75f : h;
                    var center = new Vector3(side * (hw + 0.32f), h * 0.5f - 0.18f, cz + len * 0.5f);
                    var euler = chipped ? new Vector3(R(-4f, 4f), R(-5f, 5f), R(-6f, 6f)) : new Vector3(0f, R(-1.5f, 1.5f), 0f);
                    b.Box(center, new Vector3(0.62f, hh, len - 0.08f), Vary(BasaltDark, 0.15f), euler, 0.9f, 0.96f);
                    if (Chance(0.22f))
                        b.Box(center + new Vector3(0f, hh * 0.5f + 0.004f, 0f), new Vector3(0.05f, 0.012f, len * R(0.35f, 0.7f)), Lava, new Vector3(0f, R(-12f, 12f), 0f));
                    else if (Chance(0.3f))
                        AshBlob(b, center + new Vector3(R(-0.1f, 0.1f), hh * 0.5f, R(-len * 0.25f, len * 0.25f)), R(0.12f, 0.2f));
                    cz += len;
                }
            }
        }

        static void Tile(MeshBuilder b, Vector3 c, float w, float d)
        {
            float y = -0.08f + R(-0.022f, 0.018f);
            var col = Vary(Color.Lerp(BasaltA, BasaltB, (float)rnd.NextDouble()), 0.08f);
            var euler = Chance(0.2f) ? new Vector3(R(-1.6f, 1.6f), 0f, R(-1.6f, 1.6f)) : Vector3.zero;
            b.Box(new Vector3(c.x, y, c.z), new Vector3(w, 0.2f, d), col, euler, 0.95f, 0.95f);
            float top = y + 0.1f;

            if (Chance(0.12f))
                AshBlob(b, new Vector3(c.x + R(-w * 0.3f, w * 0.3f), top, c.z + R(-d * 0.3f, d * 0.3f)), R(0.12f, 0.24f));

            if (Chance(0.035f))
            {
                // Parlayan lav çatlağı (zikzak)
                var p = new Vector3(c.x + R(-w * 0.2f, w * 0.2f), top + 0.004f, c.z - d * 0.3f);
                float a = R(-40f, 40f);
                for (int i = 0; i < 3; i++)
                {
                    float seg = d * 0.22f;
                    float ang = a + (i % 2 == 0 ? 1f : -1f) * R(20f, 35f);
                    var dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
                    b.Box(p + dir * seg * 0.5f, new Vector3(0.04f, 0.012f, seg + 0.03f), Lava, new Vector3(0f, ang, 0f));
                    p += dir * seg;
                }
            }
            else if (Chance(0.08f))
                b.Box(new Vector3(c.x + R(-w * 0.2f, w * 0.2f), top + 0.004f, c.z), new Vector3(0.035f, 0.01f, d * R(0.4f, 0.8f)), Char, new Vector3(0f, R(-60f, 60f), 0f));
        }

        /// <summary>Taşın üstüne savrulmuş kül lekesi.</summary>
        static void AshBlob(MeshBuilder b, Vector3 p, float r)
        {
            int n = 2 + rnd.Next(2);
            for (int i = 0; i < n; i++)
            {
                var o = new Vector3(R(-r, r), 0f, R(-r, r)) * 0.8f;
                float rr = r * R(0.6f, 1f);
                b.Cylinder(p + o + Vector3.up * (0.001f + i * 0.002f), p + o + Vector3.up * (0.012f + i * 0.002f), rr, rr * 0.9f, 6, Vary(Ash, 0.08f), true, true);
            }
        }

        // ─────────────────────────────────────────────────────
        // KÜL ZEMİN + NEHİR KANALI
        // ─────────────────────────────────────────────────────
        static void BuildGround(MeshBuilder b, float x0, float x1, float z0, float z1, float cell)
        {
            int nx = Mathf.CeilToInt((x1 - x0) / cell), nz = Mathf.CeilToInt((z1 - z0) / cell);
            var h = new float[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++)
                for (int j = 0; j <= nz; j++)
                    h[i, j] = GroundMesh(x0 + i * cell, z0 + j * cell) + (float)rnd.NextDouble() * 0.05f;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float xa = x0 + i * cell, za = z0 + j * cell;
                    var p00 = new Vector3(xa, h[i, j], za);
                    var p10 = new Vector3(xa + cell, h[i + 1, j], za);
                    var p11 = new Vector3(xa + cell, h[i + 1, j + 1], za + cell);
                    var p01 = new Vector3(xa, h[i, j + 1], za + cell);
                    float avg = (p00.y + p11.y) * 0.5f;
                    var col = Vary(Color.Lerp(AshDark, Ash, Mathf.InverseLerp(-0.6f, 0.05f, avg)), 0.06f);
                    float d = Mathf.Abs(xa + cell * 0.5f - RiverX(za + cell * 0.5f));
                    if (d < RiverHalf + 0.5f) col = new Color(0.36f, 0.1f, 0.03f, 0.55f);   // lavın aydınlattığı kıyı
                    else if (d < RiverHalf + 1.7f) col = Color.Lerp(col, Scorch, 0.55f);
                    var mid = (p00 + p11) * 0.5f + Vector3.down;
                    b.Face(p00, p10, p11, p01, mid, col);
                }
        }

        // ─────────────────────────────────────────────────────
        // SAĞ SÜTUNLAR: altıgen bazalt, bazıları kırık (içi lav), bazılarında mangal
        // ─────────────────────────────────────────────────────
        static void BuildPillars(MeshBuilder b, float hw, float nearZ, float gateZ)
        {
            int k = 0;
            for (float z = nearZ + 2f; z < gateZ - 3f; z += 7f, k++)
            {
                var p = new Vector3(hw + 1.45f, 0f, z);
                p.y = Mathf.Min(0f, GroundHeight(p.x, p.z) + 0.05f);
                BasaltPillar(b, p, k % 7);
            }

            // Arka sıra: altıgen kolon kümeleri + alçak yıkık duvar
            for (float z = nearZ + 5.5f; z < gateZ - 2f; z += 9f)
            {
                var p = OnGround(new Vector3(hw + 7.2f + R(-0.6f, 0.6f), 0f, z + R(-1f, 1f)));
                HexColumns(b, p, R(1f, 1.4f));
                var wc = OnGround(new Vector3(hw + 5.2f, 0f, z + 4.5f));
                float wl = R(1.5f, 3f);
                for (float t = 0; t < wl; t += 0.75f)
                {
                    float bh = R(0.3f, 0.75f);
                    b.Box(wc + new Vector3(R(-0.05f, 0.05f), bh * 0.5f - 0.1f, t), new Vector3(0.6f, bh, 0.7f), Vary(BasaltB, 0.1f),
                        new Vector3(R(-3f, 3f), R(-6f, 6f), R(-3f, 3f)), 0.95f, 0.95f);
                }
            }
        }

        static void BasaltPillar(MeshBuilder b, Vector3 p, int style)
        {
            const float r = 0.55f;
            b.Box(p + new Vector3(0f, 0.12f, 0f), new Vector3(1.55f, 0.5f, 1.55f), Vary(BasaltDark, 0.05f));
            b.Box(p + new Vector3(0f, 0.47f, 0f), new Vector3(1.25f, 0.2f, 1.25f), Vary(BasaltB, 0.05f));

            bool broken = style == 1 || style == 3 || style == 5;
            float top = broken ? (style == 1 ? 2.5f : style == 3 ? 1.6f : 2.1f) : 3.9f;
            b.Cylinder(p + new Vector3(0f, 0.57f, 0f), p + new Vector3(0f, top, 0f), r, r * 0.92f, 6, Vary(BasaltA, 0.05f), true, true);
            for (float y = 1.6f; y < top - 0.2f; y += 1.05f)
                b.Cylinder(p + new Vector3(0f, y, 0f), p + new Vector3(0f, y + 0.06f, 0f), r + 0.03f, r + 0.03f, 6, BasaltDark, false, true);

            if (!broken)
            {
                b.Cylinder(p + new Vector3(0f, top, 0f), p + new Vector3(0f, top + 0.18f, 0f), r * 0.95f, 0.7f, 6, BasaltB, true, true);
                b.Box(p + new Vector3(0f, top + 0.31f, 0f), new Vector3(1.4f, 0.26f, 1.4f), Vary(BasaltLight, 0.05f));
                if (style == 0 || style == 4)
                {
                    // Mangal: içinde kor
                    b.Cylinder(p + new Vector3(0f, top + 0.44f, 0f), p + new Vector3(0f, top + 0.82f, 0f), 0.36f, 0.52f, 6, BasaltDark, true, true);
                    b.Sphere(p + new Vector3(0f, top + 0.8f, 0f), new Vector3(0.44f, 0.14f, 0.44f), Lava, 6, 3, default, true);
                    b.Sphere(p + new Vector3(0f, top + 0.86f, 0f), new Vector3(0.24f, 0.1f, 0.24f), LavaHot, 5, 3, default, true);
                }
            }
            else
            {
                float a0 = R(0f, 360f);
                for (int i = 0; i < 4; i++)
                {
                    float a = (a0 + i * 90f + R(-20f, 20f)) * Mathf.Deg2Rad;
                    float hh = R(0.2f, 0.55f);
                    var c = p + new Vector3(Mathf.Cos(a) * r * 0.62f, top + hh * 0.5f - 0.05f, Mathf.Sin(a) * r * 0.62f);
                    b.Box(c, new Vector3(R(0.3f, 0.45f), hh, 0.3f), BasaltA, new Vector3(R(-12f, 12f), -a * Mathf.Rad2Deg + 90f, R(-15f, 15f)), 0.3f, 0.6f);
                }
                // Kırık yüzey: erimiş çekirdek
                b.Cylinder(p + new Vector3(0f, top - 0.02f, 0f), p + new Vector3(0f, top + 0.015f, 0f), r * 0.78f, r * 0.72f, 6, Lava, true, true);
                b.Cylinder(p + new Vector3(0f, top + 0.015f, 0f), p + new Vector3(0f, top + 0.03f, 0f), r * 0.35f, r * 0.3f, 6, LavaHot, true, true);
            }

            if (style == 1 || style == 2 || style == 5)
            {
                // Yola bakan yüzde (−x) lav damarları
                for (int i = 0; i < 2; i++)
                {
                    float ang = (180f + R(-25f, 25f)) * Mathf.Deg2Rad;
                    float y = R(0.8f, Mathf.Max(0.9f, top - 1.1f));
                    for (int sgm = 0; sgm < 3; sgm++)
                    {
                        float a = ang + sgm * R(-0.1f, 0.1f);
                        var c = p + new Vector3(Mathf.Cos(a) * (r * 0.9f + 0.02f), y + sgm * 0.28f, Mathf.Sin(a) * (r * 0.9f + 0.02f));
                        b.Box(c, new Vector3(0.05f, 0.32f, 0.03f), Lava, new Vector3(0f, -a * Mathf.Rad2Deg + 90f, (sgm % 2 == 0 ? 1f : -1f) * R(15f, 30f)));
                    }
                }
            }

            if (style == 3)
            {
                // Devrilmiş tambur (kırık ucu kızgın) + moloz
                var dir = Quaternion.Euler(0f, R(-40f, 40f), 0f) * Vector3.right;
                var c = p + new Vector3(1.4f, 0f, R(-1.2f, 1.2f));
                c.y = GroundHeight(c.x, c.z) + 0.4f;
                b.Cylinder(c - dir * 0.5f, c + dir * 0.5f, 0.46f, 0.44f, 6, Vary(BasaltA, 0.06f), true, true);
                b.Cylinder(c + dir * 0.5f, c + dir * 0.52f, 0.34f, 0.3f, 6, Lava, true, true);
                for (int i = 0; i < 3; i++)
                {
                    var q = OnGround(p + new Vector3(R(0.8f, 2.2f), 0f, R(-1.5f, 1.5f))) + Vector3.up * 0.08f;
                    b.Box(q, new Vector3(R(0.2f, 0.4f), R(0.15f, 0.3f), R(0.2f, 0.4f)), BasaltDark, new Vector3(R(0f, 30f), R(0f, 90f), R(0f, 30f)));
                }
            }
        }

        // ─────────────────────────────────────────────────────
        // YAN ALANLAR
        // ─────────────────────────────────────────────────────
        static void BuildLeft(MeshBuilder b, float hw, float z0, float z1)
        {
            // Nehir kıyısı kayaları
            for (float z = z0; z < z1; z += R(1.1f, 1.9f))
                for (int s = -1; s <= 1; s += 2)
                {
                    if (!Chance(0.75f)) continue;
                    float x = RiverX(z) + s * (RiverHalf + R(0.05f, 0.4f));
                    var p = new Vector3(x, LavaY + 0.05f, z + R(-0.4f, 0.4f));
                    var radii = new Vector3(R(0.3f, 0.6f), R(0.25f, 0.45f), R(0.3f, 0.6f));
                    b.Sphere(p, radii, Vary(Color.Lerp(BasaltB, BasaltDark, (float)rnd.NextDouble()), 0.08f), 6, 4,
                        new Vector3(R(-10f, 10f), R(0f, 360f), R(-10f, 10f)), true);
                }
            // Nehirde yüzen kabuklar
            for (float z = z0 + 2f; z < z1 - 2f; z += R(3f, 6f))
                b.Sphere(new Vector3(RiverX(z) + R(-0.6f, 0.6f), LavaY + 0.01f, z), new Vector3(R(0.25f, 0.5f), 0.05f, R(0.25f, 0.45f)), Vary(new Color(0.16f, 0.08f, 0.06f), 0.2f), 7, 2,
                    new Vector3(0f, R(0f, 90f), 0f), true);

            const float cell = 3f;
            for (float z = z0; z < z1; z += cell)
                for (float x = -17f; x < -5.2f; x += cell)
                {
                    var c = new Vector3(x + R(0.3f, cell - 0.3f), 0f, z + R(0.3f, cell - 0.3f));
                    if (c.x > -5.4f || InRiver(c.x, c.z, 0.9f)) continue;
                    bool strip = c.x > RiverX(c.z);   // yol ile nehir arası dar şerit
                    if (!Chance(strip ? 0.45f : 0.6f)) continue;
                    if (strip) SmallThing(b, OnGround(c), true);
                    else WildCluster(b, c, -5.4f);
                }
        }

        static void BuildRight(MeshBuilder b, float hw, float z0, float z1)
        {
            const float cell = 3.3f;
            for (float z = z0; z < z1; z += cell)
                for (float x = hw + 2.6f; x < 16f; x += cell)
                {
                    float nearRoad = Mathf.InverseLerp(16f, hw + 2.6f, x);
                    if (!Chance(Mathf.Lerp(0.35f, 0.6f, nearRoad))) continue;
                    WildCluster(b, new Vector3(x + R(0.3f, cell - 0.3f), 0f, z + R(0.3f, cell - 0.3f)), 0f);
                }
        }

        /// <summary>Bir çapa (kolon kümesi / kristal / ağaç / kaya / lav gölcüğü) ve etrafında küçükler.</summary>
        static void WildCluster(MeshBuilder b, Vector3 c, float maxX)
        {
            var anchor = OnGround(c);
            bool left = maxX < 0f;
            float roll = (float)rnd.NextDouble();
            float roadDist = Mathf.Abs(c.x) - 5f;
            if (roll < 0.26f) HexColumns(b, anchor, R(0.8f, 1.4f));
            else if (roll < 0.46f) Crystals(b, anchor, R(0.9f, 1.5f));
            else if (roll < 0.62f) DeadTree(b, anchor, R(2f, 3.2f));
            else if (roll < 0.8f || roadDist < 3f) RockPile(b, anchor, R(0.8f, 1.5f));
            else LavaPool(b, anchor, R(0.8f, 1.4f));

            int n = 1 + rnd.Next(3);
            for (int i = 0; i < n; i++)
            {
                float a = R(0f, 6.28f), d = R(0.9f, 1.6f);
                var p = c + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                if (left && (p.x > maxX || InRiver(p.x, p.z, 0.6f))) continue;
                if (!left && p.x < 6.8f) continue;
                SmallThing(b, OnGround(p), false);
            }
        }

        static void SmallThing(MeshBuilder b, Vector3 p, bool tiny)
        {
            float r2 = (float)rnd.NextDouble();
            float s = tiny ? 0.7f : 1f;
            if (r2 < 0.35f) RockPile(b, p, R(0.35f, 0.65f) * s);
            else if (r2 < 0.6f) Crystals(b, p, R(0.45f, 0.75f) * s);
            else if (r2 < 0.8f) GroundCrack(b, p, R(0.8f, 1.4f) * s);
            else AshMound(b, p, R(0.5f, 0.9f) * s);
        }

        static void HexColumns(MeshBuilder b, Vector3 p, float s)
        {
            int n = 3 + rnd.Next(5);
            for (int i = 0; i < n; i++)
            {
                var c = p + (i == 0 ? Vector3.zero : new Vector3(R(-0.7f, 0.7f), 0f, R(-0.7f, 0.7f)) * s);
                float h = (i == 0 ? R(2.2f, 3.4f) : R(0.6f, 2.4f)) * s;
                float r = R(0.26f, 0.36f) * s;
                var col = Vary(Color.Lerp(BasaltA, BasaltB, (float)rnd.NextDouble()), 0.08f);
                b.Cylinder(c - Vector3.up * 0.3f, c + Vector3.up * h, r, r, 6, col, false, true);
                b.Cylinder(c + Vector3.up * h, c + Vector3.up * (h + 0.05f), r, r * 0.9f, 6, Vary(BasaltLight, 0.06f), true, true);
                if (Chance(0.18f))
                    b.Box(c + new Vector3(-r * 0.87f, h * 0.45f, 0f), new Vector3(0.03f, h * 0.5f, 0.04f), Lava, new Vector3(0f, 0f, R(-8f, 8f)));
            }
        }

        static void Crystals(MeshBuilder b, Vector3 p, float s)
        {
            int n = 2 + rnd.Next(4);
            for (int i = 0; i < n; i++)
            {
                float h = (i == 0 ? R(1f, 1.4f) : R(0.4f, 0.9f)) * s;
                float w = R(0.16f, 0.26f) * s;
                var tilt = new Vector3(R(-25f, 25f), R(0f, 90f), R(-25f, 25f));
                var up = Quaternion.Euler(tilt) * Vector3.up;
                var c = p + new Vector3(R(-0.25f, 0.25f), 0f, R(-0.25f, 0.25f)) * s + up * h * 0.45f;
                b.Box(c, new Vector3(w, h, w), Chance(0.35f) ? ObsidianLight : Vary(Obsidian, 0.15f), tilt, 0.12f, 0.12f);
            }
            // Dipte hafif mor ışıltı
            b.Cylinder(p + Vector3.up * 0.01f, p + Vector3.up * 0.02f, 0.35f * s, 0.3f * s, 6, new Color(0.2f, 0.1f, 0.3f, 0.5f), true, true);
        }

        static void DeadTree(MeshBuilder b, Vector3 p, float h)
        {
            var top = p + new Vector3(R(-0.2f, 0.2f), h, R(-0.2f, 0.2f));
            b.Cylinder(p - Vector3.up * 0.1f, top, 0.2f, 0.07f, 5, Vary(Char, 0.15f), true, true);
            b.Cylinder(p - Vector3.up * 0.05f, p + Vector3.up * 0.25f, 0.34f, 0.2f, 5, Char, false, true);
            int n = 3 + rnd.Next(2);
            for (int i = 0; i < n; i++)
            {
                var from = Vector3.Lerp(p, top, R(0.4f, 0.85f));
                float a = R(0f, 6.28f);
                var to = from + new Vector3(Mathf.Cos(a) * R(0.5f, 1f), R(0.3f, 0.8f), Mathf.Sin(a) * R(0.5f, 1f));
                b.Cylinder(from, to, 0.07f, 0.025f, 4, Vary(Char, 0.15f), false, true);
                if (Chance(0.5f))
                {
                    var tw = to + new Vector3(R(-0.3f, 0.3f), R(0.1f, 0.35f), R(-0.3f, 0.3f));
                    b.Cylinder(to, tw, 0.025f, 0.012f, 4, Char, false, true);
                }
                if (Chance(0.4f)) b.Sphere(to, Vector3.one * 0.045f, Lava, 4, 3, default, true);
            }
            // Gövdede kor çatlağı
            b.Box(Vector3.Lerp(p, top, 0.3f) + new Vector3(-0.17f, 0f, 0f), new Vector3(0.02f, h * 0.25f, 0.05f), Ember);
        }

        static void RockPile(MeshBuilder b, Vector3 p, float s)
        {
            int n = Chance(0.5f) ? 1 : 2 + rnd.Next(2);
            for (int i = 0; i < n; i++)
            {
                float rs = s * (i == 0 ? 1f : R(0.35f, 0.6f));
                var c = p + (i == 0 ? Vector3.zero : new Vector3(R(-0.6f, 0.6f), 0f, R(-0.6f, 0.6f)) * s);
                var radii = new Vector3(R(0.35f, 0.6f), R(0.25f, 0.45f), R(0.35f, 0.6f)) * rs;
                b.Sphere(c + Vector3.up * radii.y * 0.35f, radii, Vary(Color.Lerp(BasaltA, BasaltDark, (float)rnd.NextDouble()), 0.08f),
                    6, 4, new Vector3(R(-10f, 10f), R(0f, 360f), R(-10f, 10f)), true);
                if (i == 0 && Chance(0.3f))
                    b.Sphere(c + Vector3.up * radii.y * 0.95f, new Vector3(radii.x * 0.7f, radii.y * 0.3f, radii.z * 0.7f), Vary(Ash, 0.1f), 6, 3, default, true);
            }
        }

        static void AshMound(MeshBuilder b, Vector3 p, float s)
        {
            b.Sphere(p, new Vector3(R(0.5f, 0.8f), R(0.18f, 0.3f), R(0.5f, 0.8f)) * s, Vary(Ash, 0.08f), 7, 3, new Vector3(0f, R(0f, 90f), 0f), true);
        }

        static void GroundCrack(MeshBuilder b, Vector3 p, float len)
        {
            float a = R(0f, 180f);
            int segs = 3 + rnd.Next(2);
            float sl = len / segs;
            var q = p + Vector3.up * 0.03f;
            for (int i = 0; i < segs; i++)
            {
                float ang = a + (i % 2 == 0 ? 1f : -1f) * R(15f, 35f);
                var dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
                q.y = GroundHeight(q.x, q.z) + 0.03f;
                b.Box(q + dir * sl * 0.5f, new Vector3(0.07f, 0.05f, sl + 0.05f), Lava, new Vector3(0f, ang, 0f));
                q += dir * sl;
            }
        }

        static void LavaPool(MeshBuilder b, Vector3 p, float r)
        {
            float y = p.y + 0.05f;
            b.Cylinder(new Vector3(p.x, y - 0.1f, p.z), new Vector3(p.x, y, p.z), r, r * 0.97f, 9, Lava, true, true);
            b.Cylinder(new Vector3(p.x, y, p.z), new Vector3(p.x, y + 0.012f, p.z), r * 0.45f, r * 0.4f, 7, LavaHot, true, true);
            int n = 7;
            for (int i = 0; i < n; i++)
            {
                float a = (i * 360f / n + R(-15f, 15f)) * Mathf.Deg2Rad;
                var c = new Vector3(p.x + Mathf.Cos(a) * r * 1.02f, y, p.z + Mathf.Sin(a) * r * 1.02f);
                b.Sphere(c, new Vector3(R(0.22f, 0.36f), R(0.14f, 0.24f), R(0.22f, 0.36f)) * Mathf.Max(0.8f, r), Vary(BasaltDark, 0.15f), 5, 3,
                    new Vector3(0f, R(0f, 90f), 0f), true);
            }
            b.Cylinder(new Vector3(p.x, y - 0.12f, p.z), new Vector3(p.x, y - 0.1f, p.z), r * 1.6f, r * 1.5f, 9, Vary(Scorch, 0.1f), true, true);
        }

        // ─────────────────────────────────────────────────────
        // DÜŞMAN KAPISI (bazalt + lav şelalesi)
        // ─────────────────────────────────────────────────────
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
                    b.Box(new Vector3(x + R(-0.05f, 0.05f), y + h * 0.5f, gz), new Vector3(w, h - 0.05f, w), Vary(i % 2 == 0 ? BasaltA : BasaltB, 0.06f),
                        new Vector3(0f, R(-3f, 3f), 0f), 0.97f, 0.97f);
                    y += h;
                    i++;
                }
                // Ön yüzde lav damarları
                for (int k = 0; k < 2; k++)
                {
                    float cx = x + R(-0.6f, 0.6f);
                    float cy = R(1f, GateH - 2f);
                    for (int s = 0; s < 4; s++)
                        b.Box(new Vector3(cx + (s % 2 == 0 ? 0.06f : -0.06f), cy + s * 0.34f, gz - 1.1f), new Vector3(0.06f, 0.38f, 0.02f), Lava,
                            new Vector3(0f, 0f, (s % 2 == 0 ? 1f : -1f) * R(12f, 25f)));
                }
            }
            // Lento + boynuz gibi sivri taçlar
            b.Box(new Vector3(0f, GateH + 0.65f, gz), new Vector3(hw * 2f + 4.4f, 1.3f, 2.2f), Vary(BasaltA, 0.03f));
            b.Box(new Vector3(0f, GateH + 1.45f, gz), new Vector3(hw * 2f + 3.6f, 0.3f, 1.8f), BasaltDark);
            for (int k = -2; k <= 2; k++)
            {
                float hh = k == 0 ? 1.6f : (Mathf.Abs(k) == 1 ? 1.1f : 1.4f);
                b.Box(new Vector3(k * 2.3f, GateH + 1.6f + hh * 0.5f, gz), new Vector3(0.7f, hh, 0.8f), Vary(BasaltB, 0.05f),
                    new Vector3(0f, 0f, k * -6f), 0.15f, 0.3f);
            }
            // Tünel: lav perdesinin arkası karanlık
            var dark = new Color(0.03f, 0.012f, 0.01f);
            b.Box(new Vector3(0f, GateH * 0.5f, gz + 3.2f), new Vector3(hw * 2f + 1f, GateH, 0.3f), dark);
            for (int side = -1; side <= 1; side += 2)
                b.Box(new Vector3(side * (hw + 0.4f), GateH * 0.5f, gz + 2.1f), new Vector3(0.3f, GateH, 2.4f), dark);
            b.Box(new Vector3(0f, GateH + 0.1f, gz + 2.1f), new Vector3(hw * 2f + 1f, 0.2f, 2.4f), dark);
        }

        static void BuildLava(Transform env, float hw, float nearZ, float gz)
        {
            // Kapıdan dökülen lav perdesi + yola yayılan birikinti
            float z = gz - 0.7f;
            float t = GateH - 0.05f;
            var fall = WaterFx.Sheet(new[]
            {
                new Vector3(0f, t, z + 0.1f), new Vector3(0f, t - 0.3f, z - 0.2f), new Vector3(0f, t * 0.72f, z - 0.36f),
                new Vector3(0f, t * 0.4f, z - 0.43f), new Vector3(0f, 0.9f, z - 0.47f), new Vector3(0f, 0.05f, z - 0.49f),
            }, Vector3.right, hw * 2f + 0.5f, 10, 0.05f, 0.95f, "Lavafall");
            var pool = WaterFx.Sheet(new[]
            {
                new Vector3(0f, 0.05f, z - 0.49f), new Vector3(0f, 0.045f, z - 1.3f), new Vector3(0f, 0.04f, z - 2.5f),
            }, Vector3.right, hw * 2f + 0.5f, 10, 0.95f, 0f, "LavaPool");

            // Sol nehir: kapının arkasından oyuncuya doğru akar
            var path = new List<Vector3>();
            for (float rz = gz + 8f; rz > nearZ - 12f; rz -= 1.5f)
                path.Add(new Vector3(RiverX(rz), LavaY, rz));
            var river = WaterFx.Sheet(path, Vector3.right, RiverHalf * 2f + 0.3f, 3, 0.12f, 0.12f, "LavaRiver");

            WaterFx.MakeWater("Lava", WaterFx.Combine("Vol_Lava", fall, pool, river), env);
        }

        // ─────────────────────────────────────────────────────
        // UZAK PLAN: püsküren yanardağ, sırtlar, duman sütunu
        // ─────────────────────────────────────────────────────
        static void BuildBackdrop(MeshBuilder b, float gz)
        {
            rnd = new System.Random(71);
            var ridge = new Color(0.15f, 0.1f, 0.09f);

            for (float x = -75f; x <= 75f; x += R(9f, 14f))
            {
                if (Mathf.Abs(x) < 15f) continue;
                float side = Mathf.InverseLerp(15f, 75f, Mathf.Abs(x));
                float z = gz + R(14f, 24f) - side * 5f;
                float h = R(5f, 10f) * Mathf.Lerp(0.8f, 1.5f, side);
                float w = R(7f, 11f);
                float d = R(3f, 5f);
                var cc = new Vector3(x, h * 0.3f - 2f, z);
                var radii = new Vector3(w * 0.5f, h * 0.62f, d);
                b.Sphere(cc, radii, Vary(ridge, 0.1f), 7, 5, new Vector3(R(-5f, 5f), 0f, R(-8f, 8f)), true);
                if (Chance(0.6f))
                    b.Sphere(new Vector3(x + R(-2f, 2f), h * 0.75f, z + R(-1f, 1f)), new Vector3(w * 0.28f, h * 0.35f, R(2f, 3f)), Vary(ridge, 0.08f), 6, 4,
                        new Vector3(R(-15f, 15f), R(0f, 360f), R(-15f, 15f)), true);
                if (Chance(0.4f))
                {
                    // Sırttan inen lav akıntısı
                    float sx = x + R(-w * 0.2f, w * 0.2f);
                    Vector3 prev = Vector3.zero;
                    for (int s = 0; s <= 6; s++)
                    {
                        float tt = s / 6f;
                        float yy = cc.y + radii.y * Mathf.Lerp(0.8f, -0.1f, tt);
                        float k = (yy - cc.y) / radii.y;
                        float zf = cc.z - d * Mathf.Sqrt(Mathf.Max(0.05f, 1f - k * k)) - 0.35f;
                        var pt = new Vector3(sx + Mathf.Sin(tt * 5f) * 0.5f, yy, zf);
                        if (s > 0) b.Cylinder(prev, pt, 0.22f + tt * 0.15f, 0.22f + tt * 0.15f, 4, Lava, false);
                        prev = pt;
                    }
                }
            }

            // Yanardağ konisi + omuzları
            var vc = new Vector3(0f, -3f, gz + 34f);
            const float baseR = 34f, topR = 5f, H = 14.5f;
            b.Cylinder(vc, vc + Vector3.up * H, baseR, topR, 12, Mountain, false, true);
            b.Cylinder(vc + new Vector3(-22f, 0f, 6f), vc + new Vector3(-21f, 12f, 6f), 22f, 5f, 9, new Color(0.135f, 0.076f, 0.068f), false, true);
            b.Cylinder(vc + new Vector3(25f, 0f, 3f), vc + new Vector3(24f, 9.5f, 3f), 18f, 6f, 9, new Color(0.144f, 0.081f, 0.072f), false, true);
            float ty = vc.y + H;
            var top = new Vector3(0f, ty, vc.z);
            // Krater ağzı: parlayan kenar + kor çanak
            b.Cylinder(top - Vector3.up * 0.2f, top + Vector3.up * 0.5f, topR + 0.2f, topR * 0.92f, 12, new Color(0.35f, 0.1f, 0.04f, 0.35f), false, true);
            b.Cylinder(top + Vector3.up * 0.25f, top + Vector3.up * 0.32f, topR * 0.9f, topR * 0.88f, 12, LavaHot, true, true);

            // Krater ağzından ön yamaca akan lav kolları
            for (int k = 0; k < 3; k++)
            {
                float a0 = 270f + (k - 1) * 22f + R(-4f, 4f);
                float len = R(0.6f, 0.95f);
                Vector3 prev = Vector3.zero;
                for (int s = 0; s <= 10; s++)
                {
                    float tt = s / 10f;
                    float yy = ty - tt * H * len;
                    float a = (a0 + Mathf.Sin(tt * 6f + k) * 3f) * Mathf.Deg2Rad;
                    float rr = Mathf.Lerp(baseR, topR, (yy - vc.y) / H) + 0.5f;
                    var pt = new Vector3(Mathf.Cos(a) * rr, yy + 0.2f, vc.z + Mathf.Sin(a) * rr);
                    if (s > 0) b.Cylinder(prev, pt, 0.22f + tt * 0.3f, 0.22f + tt * 0.3f, 4, s < 2 ? LavaHot : new Color(0.42f, 0.11f, 0.02f, 0.2f), false);
                    prev = pt;
                }
            }

            // Duman sütunu: altı krater ışığıyla kızarmış, yukarı doğru genişleyip rüzgârla sağa kayar
            for (int i = 0; i < 10; i++)
            {
                float t = i / 9f;
                var c = new Vector3(R(-1f, 1f) + t * t * 14f, ty + 1.5f + t * 17f, vc.z + t * 4f);
                float s = Mathf.Lerp(3f, 9f, t);
                var col = i < 2 ? new Color(0.26f, 0.12f, 0.08f, 0.85f) : Vary(new Color(0.2f, 0.16f, 0.15f), 0.1f);
                b.Sphere(c, new Vector3(s, s * 0.7f, s) * R(0.9f, 1.1f), col, 7, 5, new Vector3(R(-10f, 10f), R(0f, 360f), R(-10f, 10f)), true);
                if (i > 2 && Chance(0.7f))
                    b.Sphere(c + new Vector3(R(-s, s) * 0.7f, R(-1f, 1f), R(0f, 2f)), Vector3.one * s * R(0.5f, 0.7f), Vary(new Color(0.18f, 0.14f, 0.13f), 0.1f), 6, 4, default, true);
            }
        }

        /// <summary>En arkada koyu kızıl gökyüzü perdesi; ufuk rengi sisle aynı.</summary>
        static void BuildSky(Transform env, float z)
        {
            const float halfW = 140f, y0 = -4f, y1 = 42f;
            var m = new Mesh { name = "Vol_Sky" };
            m.SetVertices(new List<Vector3>
            {
                new Vector3(-halfW, y0, z), new Vector3(halfW, y0, z), new Vector3(halfW, y1, z), new Vector3(-halfW, y1, z),
            });
            m.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.RecalculateBounds();
            var go = new GameObject("Sky");
            go.transform.SetParent(env, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>();
            var db = GameDatabase.Load();
            var mat = db != null ? db.volcanoSkyMaterial : null;
            if (mat == null)
            {
                mat = new Material(Shader.Find("StratejiGame/SeaBackdrop"));
                mat.SetColor("_Bottom", Fog);
                mat.SetColor("_Top", new Color(0.3f, 0.08f, 0.04f));
                mat.SetColor("_Shimmer", new Color(0.6f, 0.2f, 0.05f));
                mat.SetFloat("_ShimmerStrength", 0.15f);
            }
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // ─────────────────────────────────────────────────────
        // PARLAYAN ÇİZGİLER: savunma hattı (mavi kalır) + şerit kanalları + kapı rünü
        // ─────────────────────────────────────────────────────
        static void BuildGlowLines(Transform env, float hw, float z0, float gz)
        {
            var b = new MeshBuilder();
            b.Box(new Vector3(0f, 0.03f, 0.9f), new Vector3(hw * 2f, 0.01f, 0.1f), new Color(0.35f, 0.7f, 1f));
            b.Box(new Vector3(0f, 0.025f, 0.9f), new Vector3(hw * 2f, 0.01f, 0.45f), new Color(0.06f, 0.16f, 0.3f));
            float lw = hw * 2f / GameManager.LaneCount;
            for (int i = 1; i < GameManager.LaneCount; i++)
            {
                float x = -hw + lw * i;
                b.Box(new Vector3(x, -0.044f, (z0 + gz) * 0.5f), new Vector3(0.1f, 0.01f, gz - z0), new Color(0.08f, 0.022f, 0.006f));
            }
            var rune = new Color(1f, 0.45f, 0.1f);
            b.Box(new Vector3(0f, GateH + 0.65f, gz - 1.12f), new Vector3(2.4f, 0.35f, 0.01f), rune);
            b.Box(new Vector3(0f, GateH + 0.65f, gz - 1.12f), new Vector3(0.35f, 0.9f, 0.01f), rune, new Vector3(0f, 0f, 45f));
            FxDesigns.MakeFx("GlowLines", b.ToMesh("Vol_GlowLines"), env);
        }
    }
}
