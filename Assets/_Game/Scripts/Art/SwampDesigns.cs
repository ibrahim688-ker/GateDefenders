using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Bataklık dünyasının (Çürümüş Orman) ortamı: çamura gömülmüş yosunlu taş yol (üstünden geçen kökler,
    /// zehirli yeşil su birikintileri), çamur zemin, solda zehirli bataklık (nilüferler, sazlar, kütükler, adacıklar),
    /// sağda sütunlar yerine KURUMUŞ AĞAÇLAR (sarkan yosun, parlayan mor raf mantarları), dev mantar kümeleri,
    /// iki dev kuru gövdeden kapı + zehirli yeşil akıntı perdesi, arkada sisli ölü orman siluetleri.
    /// Ortam materyalinde köşe alfası = sallanma (saz, yosun); parlayan parçalar ayrı mesh'te (GlowMaterial).
    /// </summary>
    public static class SwampDesigns
    {
        // ── Palet ─────────────────────────────────────────────
        static readonly Color SlabA = new Color(0.31f, 0.33f, 0.28f);
        static readonly Color SlabB = new Color(0.24f, 0.26f, 0.22f);
        static readonly Color SlabDark = new Color(0.16f, 0.17f, 0.14f);
        static readonly Color MudC = new Color(0.12f, 0.095f, 0.065f);
        static readonly Color Soil = new Color(0.27f, 0.22f, 0.15f);
        static readonly Color SoilDark = new Color(0.16f, 0.13f, 0.09f);
        static readonly Color Moss = new Color(0.26f, 0.4f, 0.14f);
        static readonly Color MossLight = new Color(0.42f, 0.58f, 0.2f);
        static readonly Color Bark = new Color(0.3f, 0.26f, 0.3f);
        static readonly Color BarkDark = new Color(0.17f, 0.14f, 0.17f);
        static readonly Color BarkLight = new Color(0.44f, 0.4f, 0.42f);
        static readonly Color HangMoss = new Color(0.44f, 0.5f, 0.34f);
        static readonly Color Reed = new Color(0.36f, 0.46f, 0.18f);
        static readonly Color Lily = new Color(0.2f, 0.42f, 0.12f);
        static readonly Color Rock = new Color(0.3f, 0.31f, 0.3f);
        // Parlayanlar (GlowMaterial; alfa 0 = tam parlak)
        static readonly Color ShroomPurple = new Color(0.62f, 0.2f, 0.95f, 0f);
        static readonly Color ShroomPink = new Color(0.9f, 0.3f, 0.8f, 0f);
        static readonly Color ShroomStem = new Color(0.7f, 0.66f, 0.76f, 0.7f);
        static readonly Color Toxic = new Color(0.24f, 0.55f, 0.1f, 0.65f);
        static readonly Color ToxicHot = new Color(0.45f, 0.95f, 0.2f, 0f);

        public static readonly Color Fog = new Color(0.07f, 0.1f, 0.075f);

        const float GateH = 8.2f;
        public const float WaterY = -0.3f;

        static System.Random rnd;
        static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        static bool Chance(float p) => rnd.NextDouble() < p;
        static Color Vary(Color c, float amt) { float k = R(1f - amt, 1f + amt); return new Color(c.r * k, c.g * k, c.b * k, c.a); }
        static Color Sway(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>Solda bataklık havzası (su altında), içinde adacıklar; sağda çamurlu kara.</summary>
        public static float GroundHeight(float x, float z)
        {
            float h = EnvironmentDesigns.SandHeight(x, z) * 0.8f - 0.02f;
            if (x < -5.6f)
            {
                float basin = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-5.6f, -7.6f, x));
                float island = Mathf.Sin(x * 0.33f + z * 0.21f) * Mathf.Sin(z * 0.29f - x * 0.15f + 1.3f);
                float isl = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.55f, island));
                h = Mathf.Lerp(h, -0.8f, basin * (1f - isl * 0.9f));
            }
            return h;
        }

        static float GroundMesh(float x, float z)
        {
            float h = GroundHeight(x, z);
            return Mathf.Abs(x) < 5.3f ? Mathf.Min(h, -0.4f) : h;
        }

        static Vector3 OnGround(Vector3 p) { p.y = GroundHeight(p.x, p.z); return p; }
        public static bool IsWater(float x, float z) => GroundHeight(x, z) < WaterY - 0.05f;

        public static void Build(Transform env, float hw, float nearZ, float farZ, float gateZ)
        {
            WaterFx.Init();
            var glow = new MeshBuilder();

            var road = new MeshBuilder();
            rnd = new System.Random(11);
            BuildRoad(road, glow, hw, nearZ - 6f, gateZ + 2.5f);
            WaterFx.MakeEnv("Road", road.ToMesh("Sw_Road"), env, false);

            var ground = new MeshBuilder();
            rnd = new System.Random(5);
            BuildGround(ground, -34f, 34f, nearZ - 14f, farZ + 30f, 1.3f);
            WaterFx.MakeEnv("Ground", ground.ToMesh("Sw_Ground"), env, false);

            var trees = new MeshBuilder();
            rnd = new System.Random(23);
            BuildTrees(trees, glow, hw, nearZ, gateZ);
            BuildEnemyGate(trees, glow, hw, gateZ);
            WaterFx.MakeEnv("DeadTrees", trees.ToMesh("Sw_Trees"), env, true);

            var wild = new MeshBuilder();
            rnd = new System.Random(37);
            BuildLeft(wild, glow, hw, nearZ - 8f, gateZ + 3f);
            rnd = new System.Random(53);
            BuildRight(wild, glow, hw, nearZ - 6f, gateZ + 3f);
            WaterFx.MakeEnv("Wilds", wild.ToMesh("Sw_Wilds"), env, false);

            var back = new MeshBuilder();
            BuildBackdrop(back, glow, gateZ);
            WaterFx.MakeEnv("Backdrop", back.ToMesh("Sw_Backdrop"), env, false);
            BuildSky(env, gateZ + 42f);

            WaterFx.MakeGlow("Glowing", glow.ToMesh("Sw_Glow"), env);
            BuildGlowLines(env, hw, nearZ - 6f, gateZ);
            BuildWater(env, hw, nearZ, gateZ);
        }

        // ─────────────────────────────────────────────────────
        // YOL: çamura gömülmüş yosunlu taş levhalar
        // ─────────────────────────────────────────────────────
        static void BuildRoad(MeshBuilder b, MeshBuilder glow, float hw, float z0, float z1)
        {
            b.Box(new Vector3(0f, -0.2f, (z0 + z1) * 0.5f), new Vector3(hw * 2f + 0.1f, 0.3f, z1 - z0), MudC);
            float lw = hw * 2f / GameManager.LaneCount;
            const float gap = 0.08f;
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
                        Tile(b, glow, new Vector3(x + w * 0.5f, 0f, z + rowD * 0.5f), w - gap, rowD - gap);
                        x += w;
                    }
                }
                z += rowD;
            }

            // Bordür: yosunlu, yarı batık taşlar
            for (int side = -1; side <= 1; side += 2)
            {
                float cz = z0;
                while (cz < z1)
                {
                    float len = R(1.1f, 2.3f);
                    float h = R(0.28f, 0.5f);
                    var center = new Vector3(side * (hw + 0.32f), h * 0.5f - 0.2f, cz + len * 0.5f);
                    b.Box(center, new Vector3(0.62f, h, len - 0.1f), Vary(SlabDark, 0.15f), new Vector3(R(-3f, 3f), R(-4f, 4f), R(-5f, 5f)), 0.9f, 0.95f);
                    if (Chance(0.55f))
                        MossBlob(b, center + new Vector3(R(-0.1f, 0.1f), h * 0.5f, R(-len * 0.25f, len * 0.25f)), R(0.14f, 0.24f), Vary(Moss, 0.15f));
                    cz += len;
                }
            }

            // Yolun üstünden geçen ölü ağaç kökleri (alçak; sağdaki ağaçlardan uzanır)
            for (float rz = z0 + 5f; rz < z1 - 4f; rz += R(7f, 11f))
            {
                float x0 = hw + 0.9f, x1 = hw - R(1.2f, 2.6f);
                var a = new Vector3(x0, 0.02f, rz);
                var m = new Vector3((x0 + x1) * 0.5f, 0.11f, rz + R(-0.4f, 0.4f));
                var c = new Vector3(x1, 0.0f, rz + R(-0.6f, 0.6f));
                b.Cylinder(a, m, 0.13f, 0.1f, 6, Vary(Bark, 0.1f), true, true);
                b.Cylinder(m, c, 0.1f, 0.03f, 6, Vary(BarkDark, 0.1f), true, true);
            }
        }

        static void Tile(MeshBuilder b, MeshBuilder glow, Vector3 c, float w, float d)
        {
            float y = -0.09f + R(-0.03f, 0.02f);
            var col = Vary(Color.Lerp(SlabA, SlabB, (float)rnd.NextDouble()), 0.08f);
            var euler = Chance(0.3f) ? new Vector3(R(-2.2f, 2.2f), 0f, R(-2.2f, 2.2f)) : Vector3.zero;
            b.Box(new Vector3(c.x, y, c.z), new Vector3(w, 0.2f, d), col, euler, 0.94f, 0.94f);
            float top = y + 0.1f;
            if (Chance(0.2f))
                MossBlob(b, new Vector3(c.x + R(-w * 0.3f, w * 0.3f), top, c.z + R(-d * 0.3f, d * 0.3f)), R(0.12f, 0.25f), Vary(Chance(0.6f) ? Moss : MossLight, 0.12f));
            if (Chance(0.028f))
            {
                // Zehirli yeşil su birikintisi (hafif parlar)
                var p = new Vector3(c.x + R(-w * 0.2f, w * 0.2f), top + 0.004f, c.z + R(-d * 0.2f, d * 0.2f));
                float r = R(0.18f, 0.32f);
                glow.Cylinder(p, p + Vector3.up * 0.008f, r, r * 0.95f, 8, Toxic, true, true);
                glow.Cylinder(p + new Vector3(r * 0.3f, 0f, r * 0.2f), p + new Vector3(r * 0.3f, 0.01f, r * 0.2f), r * 0.6f, r * 0.55f, 7, Toxic, true, true);
            }
            else if (Chance(0.06f))
                b.Box(new Vector3(c.x + R(-w * 0.2f, w * 0.2f), top + 0.004f, c.z), new Vector3(0.035f, 0.01f, d * R(0.4f, 0.8f)), SlabDark, new Vector3(0f, R(-60f, 60f), 0f));
        }

        static void MossBlob(MeshBuilder b, Vector3 p, float r, Color col)
        {
            int n = 2 + rnd.Next(2);
            for (int i = 0; i < n; i++)
            {
                var o = new Vector3(R(-r, r), 0f, R(-r, r)) * 0.8f;
                float rr = r * R(0.6f, 1f);
                b.Cylinder(p + o + Vector3.up * (0.001f + i * 0.002f), p + o + Vector3.up * (0.014f + i * 0.002f), rr, rr * 0.9f, 6, Vary(col, 0.06f), true, true);
            }
        }

        // ─────────────────────────────────────────────────────
        // ÇAMUR ZEMİN (solda bataklık havzası)
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
                    Color col;
                    if (avg < WaterY - 0.1f) col = Vary(new Color(0.08f, 0.09f, 0.05f), 0.1f);            // su altındaki koyu dip
                    else
                    {
                        col = Vary(Color.Lerp(SoilDark, Soil, Mathf.InverseLerp(-0.5f, 0.05f, avg)), 0.07f);
                        if (Mathf.Sin(xa * 0.7f + za * 0.4f) * Mathf.Sin(za * 0.5f - xa * 0.3f) > 0.35f) col = Color.Lerp(col, Moss, 0.3f);
                    }
                    b.Face(p00, p10, p11, p01, (p00 + p11) * 0.5f + Vector3.down, col);
                }
        }

        // ─────────────────────────────────────────────────────
        // KURUMUŞ AĞAÇLAR (sağ; sütunların yerine)
        // ─────────────────────────────────────────────────────
        static void BuildTrees(MeshBuilder b, MeshBuilder glow, float hw, float nearZ, float gateZ)
        {
            int k = 0;
            for (float z = nearZ + 2f; z < gateZ - 3f; z += 7f, k++)
            {
                var p = OnGround(new Vector3(hw + 1.55f, 0f, z));
                DeadTree(b, glow, p, R(4.2f, 5.6f), k % 3 == 1, k % 2 == 0, R(0f, 360f));
            }
            // Arka sıra: daha kısa / kırık ağaçlar ve kütükler
            for (float z = nearZ + 5.5f; z < gateZ - 2f; z += 9f)
            {
                var p = OnGround(new Vector3(hw + 7f + R(-0.8f, 0.8f), 0f, z + R(-1f, 1f)));
                if (Chance(0.6f)) DeadTree(b, glow, p, R(3f, 4.4f), Chance(0.5f), Chance(0.5f), R(0f, 360f));
                else Stump(b, glow, p, R(0.5f, 0.75f));
            }
        }

        /// <summary>Budaksız, çürümüş ölü ağaç: kıvrık gövde, yayılan kökler, çıplak dallar, sarkan yosun, mor raf mantarları.</summary>
        static void DeadTree(MeshBuilder b, MeshBuilder glow, Vector3 p, float h, bool broken, bool mushrooms, float yaw)
        {
            float r = R(0.42f, 0.55f);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            // Gövde: 3 parçalı hafif kıvrık
            float top = broken ? h * R(0.45f, 0.6f) : h;
            var pts = new Vector3[4];
            pts[0] = p - Vector3.up * 0.2f;
            for (int i = 1; i < 4; i++)
                pts[i] = p + Vector3.up * (top * i / 3f) + rot * new Vector3(R(-0.25f, 0.25f) * i * 0.6f, 0f, R(-0.2f, 0.2f) * i * 0.6f);
            for (int i = 0; i < 3; i++)
            {
                float ra = Mathf.Lerp(r, r * 0.45f, i / 3f), rb = Mathf.Lerp(r, r * 0.45f, (i + 1) / 3f);
                b.Cylinder(pts[i], pts[i + 1], ra, rb, 7, Vary(Bark, 0.06f), i == 2, true);
            }
            // Yayılan kökler
            int roots = 5;
            for (int i = 0; i < roots; i++)
            {
                float a = (i * 360f / roots + R(-20f, 20f)) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var s = p + dir * r * 0.5f + Vector3.up * R(0.4f, 0.7f);
                var e = OnGround(p + dir * R(1f, 1.6f)) - Vector3.up * 0.05f;
                b.Cylinder(s, e, r * 0.42f, 0.05f, 5, Vary(BarkDark, 0.1f), true, true);
            }
            // Kabuk yarıkları (koyu dikey şeritler)
            for (int i = 0; i < 4; i++)
            {
                float a = R(0f, 6.28f);
                float y = R(0.6f, top * 0.7f);
                b.Box(p + new Vector3(Mathf.Cos(a) * r * 0.9f, y, Mathf.Sin(a) * r * 0.9f), new Vector3(0.05f, R(0.5f, 1.1f), 0.05f), BarkDark,
                    new Vector3(0f, -a * Mathf.Rad2Deg, R(-6f, 6f)));
            }
            if (broken)
            {
                // Kırık tepe: sivri kıymıklar + içi açık renkli çürük odun
                for (int i = 0; i < 4; i++)
                {
                    float a = (i * 90f + R(-25f, 25f)) * Mathf.Deg2Rad;
                    float hh = R(0.4f, 1f);
                    var c = pts[3] + new Vector3(Mathf.Cos(a) * r * 0.3f, hh * 0.45f, Mathf.Sin(a) * r * 0.3f);
                    b.Box(c, new Vector3(R(0.12f, 0.2f), hh, 0.12f), BarkLight, new Vector3(R(-12f, 12f), -a * Mathf.Rad2Deg, R(-15f, 15f)), 0.15f, 0.4f);
                }
            }
            // Dallar (kırık olsa da bir iki dal kalır)
            int branches = broken ? 2 : 4 + rnd.Next(2);
            for (int i = 0; i < branches; i++)
            {
                float t = broken ? R(0.5f, 0.9f) : R(0.45f, 0.95f);
                var from = Vector3.Lerp(pts[1], pts[3], t);
                float a = R(0f, 6.28f);
                var dir = new Vector3(Mathf.Cos(a), R(0.5f, 1.1f), Mathf.Sin(a)).normalized;
                float len = R(1.2f, 2.2f);
                var mid = from + dir * len * 0.55f;
                var end = mid + (dir + new Vector3(R(-0.4f, 0.4f), R(-0.3f, 0.2f), R(-0.4f, 0.4f))).normalized * len * 0.5f;
                b.Cylinder(from, mid, r * 0.35f, r * 0.2f, 5, Vary(Bark, 0.08f), false, true);
                b.Cylinder(mid, end, r * 0.2f, 0.02f, 5, Vary(BarkDark, 0.08f), true, true);
                // İnce çatallar
                var tw = mid + new Vector3(R(-0.5f, 0.5f), R(0.3f, 0.7f), R(-0.5f, 0.5f));
                b.Cylinder(mid, tw, r * 0.1f, 0.01f, 4, BarkDark, true, true);
                // Sarkan yosun (sallanır)
                if (Chance(0.7f))
                {
                    int ns = 2 + rnd.Next(3);
                    for (int s = 0; s < ns; s++)
                        Strand(b, Vector3.Lerp(from, end, R(0.3f, 0.95f)), R(0.6f, 1.5f), 0.09f, Vary(HangMoss, 0.12f));
                }
            }
            // Mor raf mantarları (parlar)
            if (mushrooms)
            {
                int n = 3 + rnd.Next(3);
                float a0 = R(0f, 6.28f);
                for (int i = 0; i < n; i++)
                {
                    float a = a0 + R(-0.8f, 0.8f);
                    float y = R(0.7f, top * 0.75f);
                    float rr = Mathf.Lerp(r, r * 0.45f, y / top);
                    var c = p + new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr);
                    float s = R(0.18f, 0.32f);
                    glow.Sphere(c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * s * 0.4f, new Vector3(s, s * 0.28f, s), Chance(0.7f) ? ShroomPurple : ShroomPink, 7, 3,
                        new Vector3(0f, -a * Mathf.Rad2Deg, 0f), true);
                }
            }
        }

        static void Strand(MeshBuilder b, Vector3 start, float len, float width, Color col)
        {
            const int segs = 3;
            float sl = len / segs;
            for (int i = 0; i < segs; i++)
            {
                float a = Mathf.Lerp(0.8f, 0.1f, (i + 0.5f) / segs);
                b.Box(start + new Vector3(0f, -sl * (i + 0.5f), 0f), new Vector3(width * (1f - i * 0.22f), sl + 0.02f, 0.02f), Sway(col, a),
                    new Vector3(0f, R(0f, 180f), 0f), 0.8f, 1f);
            }
        }

        static void Stump(MeshBuilder b, MeshBuilder glow, Vector3 p, float r)
        {
            float h = R(0.5f, 1.1f);
            b.Cylinder(p - Vector3.up * 0.2f, p + Vector3.up * h, r, r * 0.85f, 8, Vary(Bark, 0.08f), false, true);
            b.Cylinder(p + Vector3.up * h, p + Vector3.up * (h + 0.02f), r * 0.85f, r * 0.8f, 8, BarkLight, true, true);
            b.Cylinder(p + Vector3.up * (h + 0.02f), p + Vector3.up * (h + 0.03f), r * 0.45f, r * 0.4f, 8, Vary(BarkDark, 0.1f), true, true);
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + R(-20f, 20f)) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                b.Cylinder(p + dir * r * 0.6f + Vector3.up * 0.3f, OnGround(p + dir * r * 1.9f), r * 0.35f, 0.04f, 5, Vary(BarkDark, 0.1f), true, true);
            }
            if (Chance(0.6f)) ShroomCluster(glow, b, p + new Vector3(R(-r, r), h + 0.02f, R(-r, r)) * 1f, 0.6f);
        }

        // ─────────────────────────────────────────────────────
        // YAN ALANLAR
        // ─────────────────────────────────────────────────────
        static void BuildLeft(MeshBuilder b, MeshBuilder glow, float hw, float z0, float z1)
        {
            const float cell = 2.6f;
            for (float z = z0; z < z1; z += cell)
                for (float x = -17f; x < -5.4f; x += cell)
                {
                    var c = new Vector3(x + R(0.3f, cell - 0.3f), 0f, z + R(0.3f, cell - 0.3f));
                    if (c.x > -5.6f) continue;
                    if (IsWater(c.x, c.z))
                    {
                        float roll = (float)rnd.NextDouble();
                        if (roll < 0.35f) LilyPads(b, glow, c);
                        else if (roll < 0.55f) Reeds(b, new Vector3(c.x, WaterY - 0.3f, c.z), R(1f, 1.6f));
                        else if (roll < 0.65f) FloatingLog(b, new Vector3(c.x, WaterY, c.z));
                        else if (roll < 0.72f) Stump(b, glow, new Vector3(c.x, WaterY - 0.1f, c.z), R(0.4f, 0.65f));
                    }
                    else
                    {
                        if (!Chance(0.75f)) continue;
                        var g = OnGround(c);
                        float roll = (float)rnd.NextDouble();
                        if (roll < 0.3f) ShroomCluster(glow, b, g, R(0.9f, 1.4f));
                        else if (roll < 0.55f) Reeds(b, g, R(1f, 1.5f));
                        else if (roll < 0.75f) RockPile(b, g, R(0.6f, 1.1f));
                        else if (roll < 0.88f && c.x < -9f) DeadTree(b, glow, g, R(2.5f, 4f), Chance(0.5f), Chance(0.5f), R(0f, 360f));
                        else Stump(b, glow, g, R(0.4f, 0.6f));
                    }
                }
            // Yol ile bataklık arasındaki kıyı: sazlar ve küçük mantarlar
            for (float z = z0; z < z1; z += R(1.4f, 2.4f))
            {
                var g = OnGround(new Vector3(R(-6.4f, -5.5f), 0f, z));
                if (Chance(0.6f)) Reeds(b, g, R(0.7f, 1.1f));
                else if (Chance(0.5f)) ShroomCluster(glow, b, g, R(0.5f, 0.8f));
            }
        }

        static void BuildRight(MeshBuilder b, MeshBuilder glow, float hw, float z0, float z1)
        {
            const float cell = 3.3f;
            for (float z = z0; z < z1; z += cell)
                for (float x = hw + 2.8f; x < 16f; x += cell)
                {
                    float nearRoad = Mathf.InverseLerp(16f, hw + 2.8f, x);
                    if (!Chance(Mathf.Lerp(0.35f, 0.6f, nearRoad))) continue;
                    var c = OnGround(new Vector3(x + R(0.3f, cell - 0.3f), 0f, z + R(0.3f, cell - 0.3f)));
                    float roll = (float)rnd.NextDouble();
                    if (roll < 0.28f) GiantShrooms(glow, b, c);
                    else if (roll < 0.45f) FallenLog(b, glow, c);
                    else if (roll < 0.62f) RockPile(b, c, R(0.7f, 1.2f));
                    else if (roll < 0.78f) ToxicPool(b, glow, c, R(0.8f, 1.3f));
                    else if (roll < 0.9f) Reeds(b, c, R(0.9f, 1.3f));
                    else ShroomCluster(glow, b, c, R(0.8f, 1.2f));
                }
        }

        static void LilyPads(MeshBuilder b, MeshBuilder glow, Vector3 c)
        {
            int n = 2 + rnd.Next(4);
            for (int i = 0; i < n; i++)
            {
                var p = new Vector3(c.x + R(-0.9f, 0.9f), WaterY + 0.015f, c.z + R(-0.9f, 0.9f));
                float r = R(0.22f, 0.42f);
                b.Cylinder(p, p + Vector3.up * 0.015f, r, r, 9, Vary(Lily, 0.12f), true, true);
                // Çentik (koyu)
                b.Box(p + new Vector3(0f, 0.017f, r * 0.5f), new Vector3(0.04f, 0.005f, r), new Color(0.1f, 0.16f, 0.06f), new Vector3(0f, R(0f, 360f), 0f));
                if (Chance(0.18f)) glow.Sphere(p + Vector3.up * 0.06f, new Vector3(0.07f, 0.05f, 0.07f), new Color(0.95f, 0.5f, 0.85f, 0.5f), 6, 3, default, true);
            }
        }

        static void Reeds(MeshBuilder b, Vector3 p, float s)
        {
            int n = 6 + rnd.Next(6);
            for (int i = 0; i < n; i++)
            {
                float h = R(0.7f, 1.4f) * s;
                var c = p + new Vector3(R(-0.3f, 0.3f), h * 0.5f, R(-0.3f, 0.3f));
                var e = new Vector3(R(-8f, 8f), R(0f, 180f), R(-10f, 10f));
                b.Box(c, new Vector3(0.05f, h, 0.015f), Sway(Vary(Reed, 0.12f), 0.35f), e, 0.2f, 1f);
                // Kedikuyruğu (kahverengi başak)
                if (Chance(0.35f))
                {
                    var top = c + Quaternion.Euler(e) * Vector3.up * h * 0.42f;
                    b.Cylinder(top, top + Vector3.up * 0.2f * s, 0.045f, 0.04f, 5, Sway(new Color(0.28f, 0.17f, 0.08f), 0.15f), true, true);
                }
            }
        }

        static void FloatingLog(MeshBuilder b, Vector3 p)
        {
            var dir = Quaternion.Euler(0f, R(0f, 180f), 0f) * Vector3.forward;
            float len = R(1.6f, 2.8f), r = R(0.22f, 0.32f);
            b.Cylinder(p - dir * len * 0.5f, p + dir * len * 0.5f, r, r * 0.85f, 7, Vary(Bark, 0.1f), true, true);
            b.Cylinder(p + dir * len * 0.5f, p + dir * (len * 0.5f + 0.02f), r * 0.85f, r * 0.8f, 7, BarkLight, true, true);
            MossBlob(b, p + Vector3.up * r * 0.9f, r * 0.8f, Vary(Moss, 0.1f));
        }

        static void FallenLog(MeshBuilder b, MeshBuilder glow, Vector3 p)
        {
            var dir = Quaternion.Euler(0f, R(0f, 180f), 0f) * Vector3.forward;
            float len = R(2.2f, 3.4f), r = R(0.3f, 0.42f);
            var c = p + Vector3.up * r * 0.6f;
            b.Cylinder(c - dir * len * 0.5f, c + dir * len * 0.5f, r, r * 0.9f, 8, Vary(Bark, 0.08f), true, true);
            b.Cylinder(c + dir * len * 0.5f, c + dir * (len * 0.5f + 0.02f), r * 0.9f, r * 0.85f, 8, BarkLight, true, true);
            b.Cylinder(c + dir * (len * 0.5f + 0.02f), c + dir * (len * 0.5f + 0.03f), r * 0.5f, r * 0.45f, 8, BarkDark, true, true);
            MossBlob(b, c + Vector3.up * r * 0.9f - dir * len * 0.2f, r, Vary(Moss, 0.1f));
            for (int i = 0; i < 3; i++)
            {
                var q = c + dir * R(-len * 0.4f, len * 0.4f) + Vector3.up * r * 0.85f;
                glow.Sphere(q, new Vector3(0.12f, 0.035f, 0.12f), ShroomPurple, 6, 3, default, true);
            }
        }

        static void RockPile(MeshBuilder b, Vector3 p, float s)
        {
            int n = Chance(0.5f) ? 1 : 2 + rnd.Next(2);
            for (int i = 0; i < n; i++)
            {
                float rs = s * (i == 0 ? 1f : R(0.35f, 0.6f));
                var c = p + (i == 0 ? Vector3.zero : new Vector3(R(-0.6f, 0.6f), 0f, R(-0.6f, 0.6f)) * s);
                var radii = new Vector3(R(0.35f, 0.6f), R(0.25f, 0.4f), R(0.35f, 0.6f)) * rs;
                b.Sphere(c + Vector3.up * radii.y * 0.3f, radii, Vary(Rock, 0.1f), 6, 4, new Vector3(R(-10f, 10f), R(0f, 360f), R(-10f, 10f)), true);
                if (i == 0) b.Sphere(c + Vector3.up * radii.y * 0.9f, new Vector3(radii.x * 0.75f, radii.y * 0.3f, radii.z * 0.75f), Vary(Moss, 0.12f), 6, 3, default, true);
            }
        }

        /// <summary>Küçük parlayan mantar kümesi.</summary>
        static void ShroomCluster(MeshBuilder glow, MeshBuilder b, Vector3 p, float s)
        {
            int n = 3 + rnd.Next(4);
            for (int i = 0; i < n; i++)
            {
                var c = p + new Vector3(R(-0.35f, 0.35f), 0f, R(-0.35f, 0.35f)) * s;
                float h = R(0.12f, 0.35f) * s, r = R(0.07f, 0.15f) * s;
                glow.Cylinder(c - Vector3.up * 0.05f, c + Vector3.up * h, r * 0.3f, r * 0.25f, 5, ShroomStem, false, true);
                glow.Sphere(c + Vector3.up * h, new Vector3(r, r * 0.6f, r), Chance(0.75f) ? ShroomPurple : ShroomPink, 7, 4, default, true);
            }
        }

        /// <summary>Dev zehirli mantarlar (insan boyu), altta küçükler.</summary>
        static void GiantShrooms(MeshBuilder glow, MeshBuilder b, Vector3 p)
        {
            int n = 1 + rnd.Next(3);
            for (int i = 0; i < n; i++)
            {
                var c = p + (i == 0 ? Vector3.zero : new Vector3(R(-0.9f, 0.9f), 0f, R(-0.9f, 0.9f)));
                float h = i == 0 ? R(1.6f, 2.4f) : R(0.8f, 1.4f);
                float r = h * R(0.35f, 0.45f);
                var top = c + new Vector3(R(-0.2f, 0.2f), h, R(-0.2f, 0.2f));
                b.Cylinder(c - Vector3.up * 0.1f, top, r * 0.28f, r * 0.2f, 7, new Color(0.55f, 0.5f, 0.6f), false, true);
                glow.Sphere(top, new Vector3(r, r * 0.45f, r), Chance(0.7f) ? ShroomPurple : ShroomPink, 9, 5, default, true);
                glow.Cylinder(top - Vector3.up * 0.02f, top, r * 0.95f, r * 0.9f, 9, new Color(0.35f, 0.12f, 0.5f, 0.5f), true, true);
                // Şapkada açık benekler
                for (int k = 0; k < 4; k++)
                {
                    float a = R(0f, 6.28f), d = R(0.2f, 0.6f) * r;
                    glow.Sphere(top + new Vector3(Mathf.Cos(a) * d, r * 0.38f, Mathf.Sin(a) * d), new Vector3(0.07f, 0.03f, 0.07f) * (r + 0.4f), new Color(0.95f, 0.85f, 1f, 0.3f), 5, 3, default, true);
                }
            }
            ShroomCluster(glow, b, p + new Vector3(0.6f, 0f, -0.5f), 0.8f);
        }

        static void ToxicPool(MeshBuilder b, MeshBuilder glow, Vector3 p, float r)
        {
            float y = p.y + 0.04f;
            glow.Cylinder(new Vector3(p.x, y - 0.08f, p.z), new Vector3(p.x, y, p.z), r, r * 0.97f, 9, Toxic, true, true);
            glow.Cylinder(new Vector3(p.x, y, p.z), new Vector3(p.x, y + 0.01f, p.z), r * 0.4f, r * 0.35f, 7, ToxicHot, true, true);
            for (int i = 0; i < 7; i++)
            {
                float a = (i * 360f / 7f + R(-15f, 15f)) * Mathf.Deg2Rad;
                var c = new Vector3(p.x + Mathf.Cos(a) * r, y, p.z + Mathf.Sin(a) * r);
                b.Sphere(c, new Vector3(R(0.2f, 0.32f), R(0.1f, 0.18f), R(0.2f, 0.32f)), Vary(Rock, 0.15f), 5, 3, new Vector3(0f, R(0f, 90f), 0f), true);
            }
        }

        // ─────────────────────────────────────────────────────
        // DÜŞMAN KAPISI: iki dev kuru gövde + dolanan dikenli sarmaşık lento + zehirli yeşil akıntı perdesi
        // ─────────────────────────────────────────────────────
        static void BuildEnemyGate(MeshBuilder b, MeshBuilder glow, float hw, float gz)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (hw + 1.1f);
                var basePos = new Vector3(x, 0f, gz);
                // Kalın, hafif kıvrık gövde
                var a = basePos - Vector3.up * 0.3f;
                var m = basePos + new Vector3(side * 0.25f, GateH * 0.5f, 0.1f);
                var t = basePos + new Vector3(-side * 0.35f, GateH + 1.2f, 0f);
                b.Cylinder(a, m, 1.25f, 0.95f, 8, Vary(Bark, 0.05f), false, true);
                b.Cylinder(m, t, 0.95f, 0.6f, 8, Vary(Bark, 0.05f), true, true);
                // Kökler
                for (int i = 0; i < 5; i++)
                {
                    float ang = (i * 72f + R(-15f, 15f)) * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    if (dir.x * -side > 0.6f) continue; // yola taşmasın
                    b.Cylinder(basePos + dir * 0.7f + Vector3.up * 1f, basePos + dir * 2.2f - Vector3.up * 0.1f, 0.5f, 0.06f, 6, Vary(BarkDark, 0.1f), true, true);
                }
                // Yarıklar + parlayan mor mantarlar
                for (int i = 0; i < 5; i++)
                    b.Box(basePos + new Vector3(R(-0.6f, 0.6f), R(1f, GateH - 1f), -1.02f), new Vector3(0.07f, R(0.8f, 1.6f), 0.05f), BarkDark, new Vector3(0f, 0f, R(-8f, 8f)));
                for (int i = 0; i < 4; i++)
                {
                    var c = basePos + new Vector3(R(-0.7f, 0.7f), R(1f, GateH - 1.5f), -1.02f);
                    float s = R(0.25f, 0.4f);
                    glow.Sphere(c, new Vector3(s, s * 0.28f, s * 0.7f), ShroomPurple, 7, 3, default, true);
                }
                // Tepede çıplak dallar
                for (int i = 0; i < 3; i++)
                {
                    var dir = new Vector3(side * R(0.2f, 1f), R(0.6f, 1f), R(-0.4f, 0.4f)).normalized;
                    b.Cylinder(t, t + dir * R(1.5f, 2.6f), 0.28f, 0.03f, 5, BarkDark, true, true);
                }
            }
            // Lento: iç içe geçmiş kalın sarmaşık/kök demeti
            for (int k = 0; k < 4; k++)
            {
                float y = GateH + 0.3f + k * 0.28f;
                var pa = new Vector3(-hw - 1.3f, y + R(-0.2f, 0.2f), gz + R(-0.3f, 0.3f));
                var pb = new Vector3(0f, y + R(0.1f, 0.5f), gz + R(-0.4f, 0.4f));
                var pc = new Vector3(hw + 1.3f, y + R(-0.2f, 0.2f), gz + R(-0.3f, 0.3f));
                float r = R(0.22f, 0.34f);
                var col = k % 2 == 0 ? Vary(Bark, 0.08f) : Vary(new Color(0.24f, 0.36f, 0.14f), 0.1f);
                b.Cylinder(pa, pb, r, r * 0.9f, 6, col, true, true);
                b.Cylinder(pb, pc, r * 0.9f, r, 6, col, true, true);
                // Dikenler
                for (int d = 0; d < 5; d++)
                {
                    var q = Vector3.Lerp(pa, pc, R(0.1f, 0.9f));
                    b.Cylinder(q, q + new Vector3(R(-0.2f, 0.2f), R(-0.4f, 0.4f), -R(0.2f, 0.4f)), 0.06f, 0f, 4, BarkDark, true, true);
                }
            }
            // Lentodan sarkan yosun
            for (int k = 0; k < 12; k++)
                Strand(b, new Vector3(R(-hw, hw), GateH + 0.2f, gz - 0.5f), R(0.5f, 1.6f), 0.1f, Vary(HangMoss, 0.12f));
            // Lentonun ortasında parlayan yeşil göz rünü
            glow.Sphere(new Vector3(0f, GateH + 0.9f, gz - 0.55f), new Vector3(0.55f, 0.3f, 0.12f), new Color(0.45f, 0.95f, 0.2f, 0f), 8, 5, default, true);
            glow.Sphere(new Vector3(0f, GateH + 0.9f, gz - 0.62f), new Vector3(0.14f, 0.26f, 0.06f), new Color(0.03f, 0.06f, 0.02f, 1f), 6, 4, default, true);
            // Tünel: akıntının arkası karanlık
            var dark = new Color(0.015f, 0.025f, 0.015f);
            b.Box(new Vector3(0f, GateH * 0.5f, gz + 3.2f), new Vector3(hw * 2f + 1f, GateH, 0.3f), dark);
            for (int side = -1; side <= 1; side += 2)
                b.Box(new Vector3(side * (hw + 0.4f), GateH * 0.5f, gz + 2.1f), new Vector3(0.3f, GateH, 2.4f), dark);
            b.Box(new Vector3(0f, GateH + 0.1f, gz + 2.1f), new Vector3(hw * 2f + 1f, 0.2f, 2.4f), dark);
        }

        static void BuildWater(Transform env, float hw, float nearZ, float gz)
        {
            // Kapıdan dökülen zehirli yeşil akıntı perdesi + yola yayılan birikinti
            float z = gz - 0.7f;
            float t = GateH - 0.05f;
            var fall = WaterFx.Sheet(new[]
            {
                new Vector3(0f, t, z + 0.1f), new Vector3(0f, t - 0.3f, z - 0.2f), new Vector3(0f, t * 0.72f, z - 0.36f),
                new Vector3(0f, t * 0.4f, z - 0.43f), new Vector3(0f, 0.9f, z - 0.47f), new Vector3(0f, 0.05f, z - 0.49f),
            }, Vector3.right, hw * 2f + 0.5f, 10, 0.05f, 0.95f, "SlimeFall");
            var pool = WaterFx.Sheet(new[]
            {
                new Vector3(0f, 0.05f, z - 0.49f), new Vector3(0f, 0.045f, z - 1.3f), new Vector3(0f, 0.04f, z - 2.5f),
            }, Vector3.right, hw * 2f + 0.5f, 10, 0.95f, 0f, "SlimePool");

            // Sol bataklık yüzeyi: tek geniş, yavaş akan tabaka (oyuncuya doğru)
            var path = new List<Vector3>();
            for (float rz = gz + 30f; rz > nearZ - 16f; rz -= 4f) path.Add(new Vector3(-20f, WaterY, rz));
            var swamp = WaterFx.Sheet(path, Vector3.right, 28.6f, 8, 0.12f, 0.12f, "SwampWater");
            // Sağda da uzakta bir gölet
            var path2 = new List<Vector3>();
            for (float rz = gz + 20f; rz > gz - 6f; rz -= 4f) path2.Add(new Vector3(22f, WaterY, rz));
            var pond = WaterFx.Sheet(path2, Vector3.right, 16f, 4, 0.12f, 0.12f, "SwampPond");
            WaterFx.MakeWater("SwampWater", WaterFx.Combine("Sw_Water", fall, pool, swamp, pond), env);
        }

        // ─────────────────────────────────────────────────────
        // UZAK PLAN: sisli ölü orman, dev çürük ağaç, yıkık kulübe
        // ─────────────────────────────────────────────────────
        static void BuildBackdrop(MeshBuilder b, MeshBuilder glow, float gz)
        {
            rnd = new System.Random(71);
            var hill = new Color(0.12f, 0.14f, 0.11f);
            // Alçak tepeler
            for (float x = -75f; x <= 75f; x += R(10f, 15f))
            {
                float side = Mathf.InverseLerp(10f, 75f, Mathf.Abs(x));
                float z = gz + R(16f, 26f) - side * 5f;
                float h = R(3f, 6f) * Mathf.Lerp(0.8f, 1.4f, side);
                b.Sphere(new Vector3(x, h * 0.2f - 2f, z), new Vector3(R(7f, 11f), h * 0.6f, R(3f, 5f)), Vary(hill, 0.1f), 7, 5,
                    new Vector3(R(-5f, 5f), 0f, R(-6f, 6f)), true);
            }
            // Uzak ölü ağaç siluetleri
            for (int i = 0; i < 26; i++)
            {
                float x = R(-60f, 60f);
                if (Mathf.Abs(x) < 7f) x += Mathf.Sign(x == 0f ? 1f : x) * 7f;
                var p = new Vector3(x, -0.5f, gz + R(6f, 28f));
                float h = R(8f, 17f);
                BackTree(b, glow, p, h, R(0.5f, 0.9f));
            }
            // Kapının arkasında devasa çürük ağaç (Kök Kralı'nın yuvası): içi parlayan mor
            var c = new Vector3(0f, -1f, gz + 16f);
            b.Cylinder(c, c + new Vector3(0.8f, 16f, 0f), 4.2f, 2.4f, 9, new Color(0.2f, 0.17f, 0.21f), false, true);
            glow.Sphere(c + new Vector3(0f, 5.5f, -3.6f), new Vector3(1.8f, 3f, 0.6f), new Color(0.35f, 0.1f, 0.5f, 0.3f), 8, 6, default, true);
            for (int i = 0; i < 6; i++)
            {
                var dir = new Vector3(R(-1f, 1f), R(0.5f, 1f), R(-0.3f, 0.5f)).normalized;
                var from = c + new Vector3(0.8f, R(11f, 15.5f), 0f);
                b.Cylinder(from, from + dir * R(5f, 9f), 1f, 0.1f, 6, new Color(0.18f, 0.15f, 0.19f), true, true);
            }
            // Yıkık kazık-kulübe (sağda)
            var hut = new Vector3(15f, 0f, gz + 8f);
            for (int i = 0; i < 4; i++)
            {
                var q = hut + new Vector3((i % 2) * 3f - 1.5f, 0f, (i / 2) * 2.4f - 1.2f);
                b.Cylinder(q - Vector3.up * 0.5f, q + Vector3.up * 2.2f, 0.14f, 0.12f, 5, BarkDark, true, true);
            }
            b.Box(hut + new Vector3(0f, 2.3f, 0f), new Vector3(3.6f, 0.2f, 3f), Bark, new Vector3(0f, 0f, 3f));
            b.Box(hut + new Vector3(0f, 3.3f, 0.2f), new Vector3(3f, 1.9f, 2.4f), new Color(0.24f, 0.2f, 0.16f), new Vector3(0f, 0f, 4f));
            b.Box(hut + new Vector3(0f, 4.6f, 0.2f), new Vector3(3.8f, 0.9f, 3.1f), new Color(0.3f, 0.3f, 0.16f), new Vector3(0f, 0f, 9f), 0.2f, 1f);
            glow.Box(hut + new Vector3(0.4f, 3.3f, -1.0f), new Vector3(0.6f, 0.6f, 0.05f), new Color(0.8f, 0.7f, 0.3f, 0.2f));
        }

        static void BackTree(MeshBuilder b, MeshBuilder glow, Vector3 p, float h, float r)
        {
            var col = new Color(0.14f, 0.13f, 0.15f);
            var top = p + new Vector3(R(-0.8f, 0.8f), h, R(-0.5f, 0.5f));
            b.Cylinder(p, top, r, r * 0.3f, 6, col, false, true);
            int n = 3 + rnd.Next(3);
            for (int i = 0; i < n; i++)
            {
                var from = Vector3.Lerp(p, top, R(0.45f, 0.9f));
                var dir = new Vector3(R(-1f, 1f), R(0.3f, 1f), R(-0.4f, 0.4f)).normalized;
                b.Cylinder(from, from + dir * R(2f, 4.5f), r * 0.3f, 0.03f, 4, col, true, true);
            }
            if (Chance(0.35f))
                glow.Sphere(Vector3.Lerp(p, top, R(0.1f, 0.4f)) + Vector3.back * r, new Vector3(0.5f, 0.15f, 0.4f), ShroomPurple, 6, 3, default, true);
        }

        static void BuildSky(Transform env, float z)
        {
            const float halfW = 140f, y0 = -4f, y1 = 42f;
            var m = new Mesh { name = "Sw_Sky" };
            m.SetVertices(new List<Vector3> { new Vector3(-halfW, y0, z), new Vector3(halfW, y0, z), new Vector3(halfW, y1, z), new Vector3(-halfW, y1, z) });
            m.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.RecalculateBounds();
            var go = new GameObject("Sky");
            go.transform.SetParent(env, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>();
            var db = GameDatabase.Load();
            var mat = db != null ? db.swampSkyMaterial : null;
            if (mat == null)
            {
                mat = new Material(Shader.Find("StratejiGame/SeaBackdrop"));
                mat.SetColor("_Bottom", Fog);
                mat.SetColor("_Top", new Color(0.13f, 0.19f, 0.14f));
                mat.SetColor("_Shimmer", new Color(0.4f, 0.6f, 0.3f));
                mat.SetFloat("_ShimmerStrength", 0.12f);
            }
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static void BuildGlowLines(Transform env, float hw, float z0, float gz)
        {
            var b = new MeshBuilder();
            b.Box(new Vector3(0f, 0.03f, 0.9f), new Vector3(hw * 2f, 0.01f, 0.1f), new Color(0.35f, 0.7f, 1f));
            b.Box(new Vector3(0f, 0.025f, 0.9f), new Vector3(hw * 2f, 0.01f, 0.45f), new Color(0.06f, 0.16f, 0.3f));
            float lw = hw * 2f / GameManager.LaneCount;
            for (int i = 1; i < GameManager.LaneCount; i++)
            {
                float x = -hw + lw * i;
                b.Box(new Vector3(x, -0.044f, (z0 + gz) * 0.5f), new Vector3(0.1f, 0.01f, gz - z0), new Color(0.05f, 0.12f, 0.03f));
            }
            FxDesigns.MakeFx("GlowLines", b.ToMesh("Sw_GlowLines"), env);
        }
    }
}
