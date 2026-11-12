using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Çöl Sıcaklığı ortamı: hiyeroglif desenli, çatlamış antik sarı taş yol (üstünde savrulmuş kum);
    /// solda uçurum yerine (kullanıcı isteği) kum tepeleri arasında yükselen PİRAMİTLER (bazıları yarı gömülü, basamaklı);
    /// sağda altın işlemeli, şahin/Anubis motifli kumtaşı dikilitaşlar (bazıları çatlak, eğik), harabe duvarlar, hurmalar, küpler;
    /// kapı = mısır pilonu (iki eğimli kule + kanatlı güneş kursu) + dalgalanan kum perdesi; arkada kum tepeleri ve uzak piramitler.
    /// Ortam materyalinde köşe alfası = parıltı (altın, turkuaz hiyeroglifler), sallanma yok (hurma yaprakları hariç değil — sallanma 0).
    /// </summary>
    public static class DesertDesigns
    {
        // ── Palet ─────────────────────────────────────────────
        static readonly Color Sand = new Color(0.88f, 0.72f, 0.46f);
        static readonly Color SandShade = new Color(0.76f, 0.58f, 0.36f);
        static readonly Color StoneA = new Color(0.84f, 0.72f, 0.5f);
        static readonly Color StoneB = new Color(0.77f, 0.64f, 0.43f);
        static readonly Color StoneDark = new Color(0.55f, 0.42f, 0.27f);
        static readonly Color Grout = new Color(0.42f, 0.31f, 0.19f);
        static readonly Color Glyph = new Color(0.5f, 0.38f, 0.22f);
        static readonly Color Limestone = new Color(0.9f, 0.82f, 0.64f);
        static readonly Color PalmLeaf = new Color(0.3f, 0.45f, 0.18f);
        static readonly Color PalmTrunk = new Color(0.45f, 0.32f, 0.18f);
        // Parlayan (alfa = parıltı)
        static readonly Color Gold = new Color(0.98f, 0.76f, 0.3f, 0.7f);
        static readonly Color GoldGlow = new Color(1f, 0.8f, 0.35f, 0.35f);
        static readonly Color Turq = new Color(0.25f, 0.85f, 0.8f, 0.25f);
        static readonly Color Lapis = new Color(0.15f, 0.3f, 0.7f);

        public static readonly Color Fog = new Color(0.9f, 0.77f, 0.56f);

        const float GateH = 8.2f;

        static System.Random rnd;
        static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        static bool Chance(float p) => rnd.NextDouble() < p;
        static Color Vary(Color c, float amt) { float k = R(1f - amt, 1f + amt); return new Color(c.r * k, c.g * k, c.b * k, c.a); }

        /// <summary>Kum zemini: yoldan uzaklaştıkça yükselen yumuşak kum tepeleri.</summary>
        public static float GroundHeight(float x, float z)
        {
            float d = Mathf.Max(0f, Mathf.Abs(x) - 5.5f);
            float dunes = (Mathf.Sin(x * 0.21f + z * 0.13f) * 0.5f + Mathf.Sin(x * 0.11f - z * 0.27f) * 0.5f) * Mathf.Min(1f, d * 0.12f) * 1.6f;
            return EnvironmentDesigns.SandHeight(x, z) * 0.5f + Mathf.Min(d * 0.18f, 2.5f) + dunes;
        }

        static float GroundMesh(float x, float z)
        {
            float h = GroundHeight(x, z);
            return Mathf.Abs(x) < 5.3f ? Mathf.Min(h, -0.4f) : h;
        }

        static Vector3 OnGround(Vector3 p) { p.y = GroundHeight(p.x, p.z); return p; }

        public static void Build(Transform env, float hw, float nearZ, float farZ, float gateZ)
        {
            WaterFx.Init();

            var road = new MeshBuilder();
            rnd = new System.Random(11);
            BuildRoad(road, hw, nearZ - 6f, gateZ + 2.5f);
            WaterFx.MakeEnv("Road", road.ToMesh("De_Road"), env, false);

            var ground = new MeshBuilder();
            rnd = new System.Random(5);
            BuildGround(ground, -60f, 60f, nearZ - 14f, farZ + 40f, 1.4f);
            WaterFx.MakeEnv("Ground", ground.ToMesh("De_Ground"), env, false);

            var right = new MeshBuilder();
            rnd = new System.Random(23);
            BuildObelisks(right, hw, nearZ, gateZ);
            BuildEnemyGate(right, hw, gateZ);
            WaterFx.MakeEnv("Obelisks", right.ToMesh("De_Right"), env, true);

            var decor = new MeshBuilder();
            rnd = new System.Random(53);
            BuildRightDecor(decor, hw, nearZ - 6f, gateZ + 3f);
            WaterFx.MakeEnv("Ruins", decor.ToMesh("De_Ruins"), env, true);

            var pyr = new MeshBuilder();
            rnd = new System.Random(37);
            BuildLeftPyramids(pyr, hw, nearZ, gateZ);
            WaterFx.MakeEnv("Pyramids", pyr.ToMesh("De_Pyramids"), env, false);

            var back = new MeshBuilder();
            rnd = new System.Random(71);
            BuildBackdrop(back, gateZ);
            WaterFx.MakeEnv("Backdrop", back.ToMesh("De_Backdrop"), env, false);
            BuildSky(env, gateZ + 42f);

            BuildGlowLines(env, hw, nearZ - 6f, gateZ);
            BuildCurtain(env, hw, gateZ);
        }

        // ─────────────────────────────────────────────────────
        // YOL: çatlak sarı taş levhalar, hiyeroglif oymaları, üstünde kum birikintileri
        // ─────────────────────────────────────────────────────
        static void BuildRoad(MeshBuilder b, float hw, float z0, float z1)
        {
            b.Box(new Vector3(0f, -0.2f, (z0 + z1) * 0.5f), new Vector3(hw * 2f + 0.1f, 0.3f, z1 - z0), Grout);
            float lw = hw * 2f / GameManager.LaneCount;
            const float gap = 0.07f;
            float z = z0;
            while (z < z1)
            {
                float rowD = Mathf.Min(R(1f, 1.5f), z1 - z);
                if (rowD < 0.4f) break;
                for (int lane = 0; lane < GameManager.LaneCount; lane++)
                {
                    float xStart = -hw + lane * lw + 0.07f;
                    float xEnd = -hw + (lane + 1) * lw - 0.07f;
                    float x = xStart;
                    while (x < xEnd - 0.3f)
                    {
                        float w = R(0.8f, 1.5f);
                        if (xEnd - (x + w) < 0.55f) w = xEnd - x;
                        Slab(b, new Vector3(x + w * 0.5f, 0f, z + rowD * 0.5f), w - gap, rowD - gap);
                        x += w;
                    }
                }
                z += rowD;
            }
            // Bordür: büyük kumtaşı bloklar + üstte kum birikintisi
            for (int side = -1; side <= 1; side += 2)
            {
                float cz = z0;
                while (cz < z1)
                {
                    float len = R(1.2f, 2.2f);
                    float h = R(0.32f, 0.5f);
                    var center = new Vector3(side * (hw + 0.32f), h * 0.5f - 0.18f, cz + len * 0.5f);
                    b.Box(center, new Vector3(0.62f, h, len - 0.07f), Vary(StoneB, 0.06f), new Vector3(R(-2f, 2f), R(-2f, 2f), R(-3f, 3f)), 0.92f, 0.96f);
                    if (Chance(0.5f)) b.Box(center + new Vector3(-side * 0.31f, 0f, 0f), new Vector3(0.01f, h * 0.5f, len * 0.6f), Gold);
                    cz += len;
                }
                for (float dz = z0; dz < z1; dz += R(2f, 3.5f))
                    b.Sphere(new Vector3(side * (hw + R(0.1f, 0.6f)), 0.02f, dz), new Vector3(R(0.4f, 0.8f), R(0.1f, 0.2f), R(0.8f, 1.5f)), Vary(Sand, 0.04f), 7, 3, default, true);
            }
        }

        static void Slab(MeshBuilder b, Vector3 c, float w, float d)
        {
            float y = -0.08f + R(-0.025f, 0.015f);
            var col = Vary(Color.Lerp(StoneA, StoneB, (float)rnd.NextDouble()), 0.05f);
            b.Box(new Vector3(c.x, y, c.z), new Vector3(w, 0.2f, d), col, Chance(0.2f) ? new Vector3(R(-1.5f, 1.5f), 0f, R(-1.5f, 1.5f)) : Vector3.zero, 0.95f, 0.95f);
            float top = y + 0.1f + 0.003f;
            float roll = (float)rnd.NextDouble();
            if (roll < 0.22f)
            {
                // Hiyeroglif sırası: küçük oyma semboller (göz, kuş, ankh, dalga) ve çerçeve çizgileri
                float gx = c.x - w * 0.35f;
                b.Box(new Vector3(c.x, top, c.z + d * 0.32f), new Vector3(w * 0.8f, 0.006f, 0.025f), Glyph);
                b.Box(new Vector3(c.x, top, c.z - d * 0.32f), new Vector3(w * 0.8f, 0.006f, 0.025f), Glyph);
                for (int i = 0; i < 4 && gx < c.x + w * 0.35f; i++, gx += w * 0.2f)
                {
                    int g = rnd.Next(4);
                    var p = new Vector3(gx, top, c.z);
                    if (g == 0) { b.Box(p, new Vector3(0.12f, 0.006f, 0.05f), Glyph); b.Box(p, new Vector3(0.04f, 0.008f, 0.04f), Grout); }
                    else if (g == 1) { b.Box(p, new Vector3(0.03f, 0.006f, 0.16f), Glyph); b.Box(p + new Vector3(0f, 0f, 0.07f), new Vector3(0.08f, 0.006f, 0.03f), Glyph); }
                    else if (g == 2) { b.Cylinder(p + new Vector3(0f, 0f, 0.05f), p + new Vector3(0f, 0.006f, 0.05f), 0.04f, 0.04f, 6, Glyph, true, true); b.Box(p - new Vector3(0f, 0f, 0.03f), new Vector3(0.025f, 0.006f, 0.1f), Glyph); }
                    else for (int k = 0; k < 3; k++) b.Box(p + new Vector3(0f, 0f, -0.05f + k * 0.05f), new Vector3(0.1f, 0.006f, 0.02f), Glyph, new Vector3(0f, k % 2 == 0 ? 15f : -15f, 0f));
                }
            }
            else if (roll < 0.34f)
            {
                // Çatlak
                var p = new Vector3(c.x + R(-w * 0.2f, w * 0.2f), top, c.z - d * 0.35f);
                float a = R(-40f, 40f);
                for (int i = 0; i < 3; i++)
                {
                    float seg = d * 0.24f;
                    float ang = a + (i % 2 == 0 ? 1f : -1f) * R(20f, 35f);
                    var dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
                    b.Box(p + dir * seg * 0.5f, new Vector3(0.03f, 0.008f, seg + 0.03f), Grout, new Vector3(0f, ang, 0f));
                    p += dir * seg;
                }
            }
            if (Chance(0.3f))
            {
                // Savrulmuş kum lekesi
                var p = new Vector3(c.x + R(-w * 0.3f, w * 0.3f), top, c.z + R(-d * 0.3f, d * 0.3f));
                float r = R(0.15f, 0.35f);
                b.Cylinder(p, p + Vector3.up * 0.02f, r, r * 0.7f, 7, Vary(Sand, 0.04f), true, true);
            }
        }

        // ─────────────────────────────────────────────────────
        // KUM ZEMİNİ (her iki yanda kum tepeleri)
        // ─────────────────────────────────────────────────────
        static void BuildGround(MeshBuilder b, float x0, float x1, float z0, float z1, float cell)
        {
            int nx = Mathf.CeilToInt((x1 - x0) / cell), nz = Mathf.CeilToInt((z1 - z0) / cell);
            var h = new float[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++)
                for (int j = 0; j <= nz; j++)
                    h[i, j] = GroundMesh(x0 + i * cell, z0 + j * cell) + (float)rnd.NextDouble() * 0.03f;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float xa = x0 + i * cell, za = z0 + j * cell;
                    var p00 = new Vector3(xa, h[i, j], za);
                    var p10 = new Vector3(xa + cell, h[i + 1, j], za);
                    var p11 = new Vector3(xa + cell, h[i + 1, j + 1], za + cell);
                    var p01 = new Vector3(xa, h[i, j + 1], za + cell);
                    // Rüzgâr yönüne bakan yamaçlar açık, gölge yamaçlar koyu
                    float slope = (p10.y - p00.y) / cell;
                    var col = Vary(Color.Lerp(Sand, SandShade, Mathf.Clamp01(0.5f - slope * 1.5f)), 0.025f);
                    b.Face(p00, p10, p11, p01, (p00 + p11) * 0.5f + Vector3.down, col);
                }
        }

        // ─────────────────────────────────────────────────────
        // SOL: kum tepeleri arasında piramitler (uçurum yerine)
        // ─────────────────────────────────────────────────────
        static void BuildLeftPyramids(MeshBuilder b, float hw, float nearZ, float gateZ)
        {
            // Yakın, orta ve uzak sıralar; yakındakiler yarı gömülü/basamaklı, uzaktakiler dev ve pürüzsüz
            var spots = new[]
            {
                new Vector4(-hw - 6.5f, nearZ + 6f, 4.5f, 0f),
                new Vector4(-hw - 9f, nearZ + 20f, 7f, 1f),
                new Vector4(-hw - 7f, nearZ + 33f, 5f, 0f),
                new Vector4(-hw - 16f, nearZ + 12f, 10f, 1f),
                new Vector4(-hw - 20f, nearZ + 38f, 14f, 1f),
                new Vector4(-hw - 30f, nearZ + 24f, 18f, 1f),
            };
            foreach (var s in spots)
            {
                var p = OnGround(new Vector3(s.x, 0f, s.y)) - Vector3.up * s.z * 0.15f;
                if (s.w < 0.5f) StepPyramid(b, p, s.z);
                else Pyramid(b, p, s.z, true);
            }
            // Piramitlerin arasında yarı gömülü bloklar, kırık sütun başları
            for (float z = nearZ; z < gateZ; z += R(4f, 7f))
            {
                var p = OnGround(new Vector3(-hw - R(2.2f, 5f), 0f, z));
                b.Box(p + Vector3.up * 0.15f, new Vector3(R(0.6f, 1.2f), R(0.4f, 0.8f), R(0.6f, 1.1f)), Vary(StoneB, 0.06f), new Vector3(R(-15f, 15f), R(0f, 90f), R(-15f, 15f)));
                b.Sphere(p, new Vector3(R(0.8f, 1.4f), R(0.15f, 0.3f), R(0.8f, 1.4f)), Vary(Sand, 0.04f), 7, 3, default, true);
            }
        }

        /// <summary>Pürüzsüz piramit (dört kenarlı koni); tepesinde altın kaplama piramidion.</summary>
        static void Pyramid(MeshBuilder b, Vector3 p, float h, bool goldTop)
        {
            float half = h * 0.78f;
            b.Cylinder(p, p + Vector3.up * h * 0.9f, half * 1.414f, half * 1.414f * 0.1f, 4, Vary(Limestone, 0.04f), true, true);
            if (goldTop)
                b.Cylinder(p + Vector3.up * h * 0.9f, p + Vector3.up * h, half * 1.414f * 0.1f, 0f, 4, Gold, true, true);
            // Yüzeyde kum birikintisi ve aşınmış yatay sıra çizgileri
            b.Sphere(p + Vector3.up * h * 0.02f, new Vector3(half * 1.2f, h * 0.08f, half * 1.2f), Vary(Sand, 0.03f), 10, 4, default, true);
        }

        /// <summary>Basamaklı, yer yer kırık piramit (yakın plan için daha detaylı).</summary>
        static void StepPyramid(MeshBuilder b, Vector3 p, float h)
        {
            int steps = 6;
            float half = h * 0.8f;
            for (int i = 0; i < steps; i++)
            {
                float k = i / (float)steps;
                float w = half * 2f * (1f - k * 0.85f);
                float sh = h / steps;
                var c = p + Vector3.up * (sh * (i + 0.5f));
                b.Box(c, new Vector3(w, sh * 1.02f, w), Vary(i % 2 == 0 ? StoneA : StoneB, 0.04f), new Vector3(0f, 8f, 0f), 0.94f, 0.94f);
                // Kırık köşe
                if (Chance(0.35f)) b.Box(c + new Vector3(w * 0.45f, sh * 0.3f, w * 0.45f), new Vector3(w * 0.15f, sh * 0.5f, w * 0.15f), Vary(StoneDark, 0.05f), new Vector3(0f, 8f, 0f));
            }
            // Ön yüzde altın çerçeveli giriş + üstte altın tepe
            b.Box(p + new Vector3(0f, h * 0.18f, -half * 0.98f), new Vector3(h * 0.16f, h * 0.22f, 0.1f), new Color(0.2f, 0.14f, 0.08f), new Vector3(0f, 8f, 0f));
            b.Box(p + new Vector3(0f, h * 0.31f, -half * 0.98f), new Vector3(h * 0.22f, h * 0.035f, 0.12f), Gold, new Vector3(0f, 8f, 0f));
            b.Cylinder(p + Vector3.up * h, p + Vector3.up * (h + h * 0.18f), half * 0.2f, 0f, 4, Gold, true, true);
            b.Sphere(p + Vector3.up * 0.1f, new Vector3(half * 1.3f, h * 0.1f, half * 1.3f), Vary(Sand, 0.03f), 10, 4, default, true);
        }

        // ─────────────────────────────────────────────────────
        // SAĞ: altın işlemeli dikilitaşlar (sütunların yerine)
        // ─────────────────────────────────────────────────────
        static void BuildObelisks(MeshBuilder b, float hw, float nearZ, float gateZ)
        {
            int k = 0;
            for (float z = nearZ + 2f; z < gateZ - 3f; z += 7f, k++)
            {
                var p = OnGround(new Vector3(hw + 1.6f, 0f, z));
                bool cracked = k % 3 == 1;
                Obelisk(b, p, cracked ? R(3.4f, 4f) : R(4.8f, 5.6f), cracked, k % 2 == 0);
            }
        }

        static void Obelisk(MeshBuilder b, Vector3 p, float h, bool cracked, bool falcon)
        {
            // Kaide (iki basamak) + altın bant
            b.Box(p + Vector3.up * 0.15f, new Vector3(1.5f, 0.3f, 1.5f), Vary(StoneB, 0.04f));
            b.Box(p + Vector3.up * 0.4f, new Vector3(1.15f, 0.2f, 1.15f), Vary(StoneA, 0.04f));
            b.Box(p + Vector3.up * 0.51f, new Vector3(1.16f, 0.04f, 1.16f), Gold);
            var baseC = p + Vector3.up * 0.5f;
            var tilt = cracked ? new Vector3(R(-4f, 4f), 0f, R(-6f, -3f)) : Vector3.zero;
            var q = Quaternion.Euler(tilt);
            float w = 0.62f;
            // Gövde: yukarı incelen kare taş
            b.Box(baseC + q * (Vector3.up * h * 0.5f), new Vector3(w, h, w), Vary(StoneA, 0.03f), tilt, 0.68f, 0.68f);
            // Tepede altın piramidion
            var top = baseC + q * (Vector3.up * h);
            b.Box(top + q * (Vector3.up * 0.22f), new Vector3(w * 0.68f, 0.44f, w * 0.68f), Gold, tilt, 0.02f, 0.02f);
            // Yol tarafındaki yüzde (−X) hiyeroglif sütunu: turkuaz parlayan semboller + altın çerçeve
            var face = q * new Vector3(-1f, 0f, 0f);
            for (int i = 0; i < 6; i++)
            {
                float y = 0.6f + i * (h - 1.2f) / 6f;
                float ww = w * (1f - y / h * 0.32f);
                var c = baseC + q * new Vector3(-ww * 0.5f - 0.005f, y, 0f);
                int g = (i + (falcon ? 0 : 2)) % 4;
                if (g == 0) { b.Box(c, new Vector3(0.01f, 0.06f, 0.2f), Turq, tilt); b.Box(c + q * new Vector3(0f, 0.07f, 0.05f), new Vector3(0.01f, 0.08f, 0.06f), Turq, tilt); }  // şahin
                else if (g == 1) { b.Box(c, new Vector3(0.01f, 0.18f, 0.035f), Turq, tilt); b.Box(c + q * new Vector3(0f, 0.08f, 0f), new Vector3(0.01f, 0.05f, 0.12f), Turq, tilt); }   // ankh
                else if (g == 2) { b.Box(c, new Vector3(0.01f, 0.05f, 0.2f), Turq, tilt); b.Box(c, new Vector3(0.012f, 0.03f, 0.05f), Lapis, tilt); }                                  // göz
                else { b.Box(c + q * new Vector3(0f, 0.06f, -0.04f), new Vector3(0.01f, 0.16f, 0.03f), Turq, tilt); b.Box(c + q * new Vector3(0f, 0.12f, 0.04f), new Vector3(0.01f, 0.07f, 0.07f), Turq, tilt); }  // Anubis başı
            }
            b.Box(baseC + q * new Vector3(-w * 0.5f, h * 0.45f, w * 0.36f), new Vector3(0.012f, h * 0.85f, 0.03f), Gold, tilt, 0.7f, 1f);
            b.Box(baseC + q * new Vector3(-w * 0.5f, h * 0.45f, -w * 0.36f), new Vector3(0.012f, h * 0.85f, 0.03f), Gold, tilt, 0.7f, 1f);
            if (cracked)
            {
                // Çatlak: tabana yakın koyu yarık + düşmüş parçalar (çevresel tuzak dikilitaşı)
                b.Box(baseC + q * new Vector3(-w * 0.51f, 0.5f, 0f), new Vector3(0.02f, 0.35f, w * 0.7f), Grout, tilt + new Vector3(0f, 0f, 20f));
                for (int i = 0; i < 3; i++)
                    b.Box(p + new Vector3(R(-0.9f, 0.9f), 0.12f, R(-0.9f, 0.9f)), new Vector3(R(0.15f, 0.3f), R(0.12f, 0.22f), R(0.15f, 0.3f)), Vary(StoneB, 0.05f), new Vector3(R(0f, 30f), R(0f, 90f), R(0f, 30f)));
            }
            b.Sphere(p + Vector3.up * 0.05f, new Vector3(1.2f, 0.18f, 1.2f), Vary(Sand, 0.03f), 8, 3, default, true);
        }

        static void BuildRightDecor(MeshBuilder b, float hw, float z0, float z1)
        {
            // Harabe duvar parçaları (arka planda), hurmalar, küpler, yarı gömülü heykel başları
            for (float z = z0; z < z1; z += R(3.5f, 5.5f))
            {
                var p = OnGround(new Vector3(hw + R(5f, 7f), 0f, z));
                float h = R(1.2f, 3f);
                b.Box(p + Vector3.up * h * 0.5f, new Vector3(R(0.7f, 1f), h, R(2f, 3.5f)), Vary(StoneB, 0.05f), new Vector3(0f, R(-6f, 6f), 0f), 1f, 0.9f);
                b.Box(p + Vector3.up * (h * 0.6f) + new Vector3(-0.5f, 0f, 0f), new Vector3(0.02f, h * 0.15f, 1.6f), Glyph);
            }
            for (float z = z0; z < z1; z += 3f)
            {
                if (!Chance(0.55f)) continue;
                var p = OnGround(new Vector3(hw + R(2.6f, 4.2f), 0f, z + R(0f, 2f)));
                float roll = (float)rnd.NextDouble();
                if (roll < 0.35f) Palm(b, p, R(2.8f, 4f));
                else if (roll < 0.6f)
                {
                    // Toprak küpler
                    for (int i = 0; i < 2; i++)
                    {
                        var c = p + new Vector3(R(-0.4f, 0.4f), 0f, R(-0.4f, 0.4f));
                        b.Sphere(c + Vector3.up * 0.3f, new Vector3(0.24f, 0.3f, 0.24f), new Color(0.62f, 0.36f, 0.2f), 8, 5, default, true);
                        b.Cylinder(c + Vector3.up * 0.55f, c + Vector3.up * 0.68f, 0.1f, 0.13f, 7, new Color(0.55f, 0.32f, 0.18f), true, true);
                        b.Cylinder(c + Vector3.up * 0.3f, c + Vector3.up * 0.34f, 0.25f, 0.25f, 8, Lapis, false, true);
                    }
                }
                else if (roll < 0.8f)
                {
                    // Kuma gömülü dev heykel başı (firavun başlığı)
                    b.Sphere(p + Vector3.up * 0.35f, new Vector3(0.55f, 0.6f, 0.5f), Vary(StoneA, 0.04f), 8, 6, new Vector3(0f, R(-30f, 30f), R(-15f, 15f)), true);
                    b.Box(p + new Vector3(0f, 0.6f, 0f), new Vector3(1.2f, 0.7f, 0.25f), Vary(StoneB, 0.04f), new Vector3(0f, 90f, 0f), 0.6f, 1f);
                    b.Sphere(p, new Vector3(1.1f, 0.25f, 1.1f), Vary(Sand, 0.03f), 8, 3, default, true);
                }
                else b.Sphere(p, new Vector3(R(0.8f, 1.4f), R(0.3f, 0.5f), R(0.8f, 1.6f)), Vary(Sand, 0.04f), 8, 4, default, true);
            }
        }

        static void Palm(MeshBuilder b, Vector3 p, float h)
        {
            var lean = new Vector3(R(-0.5f, 0.3f), h, R(-0.3f, 0.3f));
            int segs = 5;
            for (int i = 0; i < segs; i++)
            {
                var a = p + lean * (i / (float)segs);
                var c = p + lean * ((i + 1) / (float)segs);
                b.Cylinder(a, c, 0.13f - i * 0.012f, 0.12f - i * 0.012f, 6, Vary(PalmTrunk, 0.08f), false, true);
            }
            var top = p + lean;
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f + R(0f, 0.4f);
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var mid = top + dir * 0.7f + Vector3.up * 0.2f;
                var tip = top + dir * 1.4f - Vector3.up * 0.4f;
                b.Box((top + mid) * 0.5f, new Vector3(0.28f, 0.03f, 0.75f), Vary(PalmLeaf, 0.1f), Quaternion.LookRotation(mid - top).eulerAngles, 1f, 1f);
                b.Box((mid + tip) * 0.5f, new Vector3(0.22f, 0.03f, 0.75f), Vary(PalmLeaf, 0.1f), Quaternion.LookRotation(tip - mid).eulerAngles, 0.3f, 1f);
            }
            b.Sphere(top, Vector3.one * 0.14f, new Color(0.4f, 0.25f, 0.1f), 6, 4, default, true);
        }

        // ─────────────────────────────────────────────────────
        // DÜŞMAN KAPISI: Mısır pilonu (iki eğimli kule) + kanatlı güneş kursu
        // ─────────────────────────────────────────────────────
        static void BuildEnemyGate(MeshBuilder b, float hw, float gz)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (hw + 1.4f);
                // Eğimli kule (yukarı daralan)
                b.Box(new Vector3(x, (GateH + 1.2f) * 0.5f, gz), new Vector3(3f, GateH + 1.2f, 2.6f), Vary(StoneA, 0.03f), default, 0.78f, 0.85f);
                // Üst korniş (dışa taşan) + altın şerit
                b.Box(new Vector3(x, GateH + 1.35f, gz), new Vector3(2.6f, 0.35f, 2.4f), StoneB);
                b.Box(new Vector3(x, GateH + 1.1f, gz - 1.11f), new Vector3(2.3f, 0.12f, 0.02f), Gold);
                // Ön yüzde dikey hiyeroglif şeritleri (turkuaz) ve büyük kabartma figür (koyu)
                for (int i = -1; i <= 1; i += 2)
                    b.Box(new Vector3(x + i * 0.7f, GateH * 0.5f, gz - 1.25f), new Vector3(0.12f, GateH * 0.75f, 0.02f), Turq, new Vector3(-4f, 0f, 0f));
                b.Box(new Vector3(x, GateH * 0.45f, gz - 1.24f), new Vector3(0.8f, 2.6f, 0.03f), Glyph, new Vector3(-4f, 0f, 0f), 0.7f, 1f);
                b.Sphere(new Vector3(x, GateH * 0.45f + 1.5f, gz - 1.22f), new Vector3(0.28f, 0.3f, 0.05f), Glyph, 7, 5);
                // Önde bekçi sfenks heykeli yerine iki altın uçlu dikme
                b.Cylinder(new Vector3(x - side * 1.5f, 0f, gz - 1.6f), new Vector3(x - side * 1.5f, 2.2f, gz - 1.6f), 0.18f, 0.14f, 6, StoneB, true, true);
                b.Cylinder(new Vector3(x - side * 1.5f, 2.2f, gz - 1.6f), new Vector3(x - side * 1.5f, 2.6f, gz - 1.6f), 0.14f, 0f, 4, Gold, true, true);
                b.Sphere(new Vector3(x, 0.1f, gz - 1.2f), new Vector3(1.8f, 0.4f, 1f), Vary(Sand, 0.03f), 8, 4, default, true);
            }
            // Lento + kanatlı güneş kursu
            b.Box(new Vector3(0f, GateH + 0.6f, gz), new Vector3(hw * 2f + 1.6f, 1.2f, 2.2f), StoneA);
            b.Box(new Vector3(0f, GateH + 1.25f, gz), new Vector3(hw * 2f + 1.9f, 0.2f, 2.4f), StoneB);
            var c = new Vector3(0f, GateH + 0.6f, gz - 1.12f);
            b.Cylinder(c, c + new Vector3(0f, 0f, -0.06f), 0.38f, 0.38f, 12, new Color(1f, 0.6f, 0.2f, 0.1f), true, true);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 4; i++)
                    b.Box(c + new Vector3(s * (0.75f + i * 0.45f), 0.12f - i * 0.05f, -0.03f), new Vector3(0.5f, 0.16f - i * 0.02f, 0.02f), i % 2 == 0 ? Gold : Lapis, new Vector3(0f, 0f, -s * (4f + i * 3f)), 1f, 1f);
            // Tünel
            var dark = new Color(0.2f, 0.13f, 0.08f);
            b.Box(new Vector3(0f, GateH * 0.5f, gz + 3.2f), new Vector3(hw * 2f + 1f, GateH, 0.3f), dark);
            for (int side = -1; side <= 1; side += 2)
                b.Box(new Vector3(side * (hw + 0.4f), GateH * 0.5f, gz + 2.1f), new Vector3(0.3f, GateH, 2.4f), dark);
            b.Box(new Vector3(0f, GateH + 0.1f, gz + 2.1f), new Vector3(hw * 2f + 1f, 0.2f, 2.4f), dark);
        }

        static void BuildCurtain(Transform env, float hw, float gz)
        {
            // Kapıda dalgalanan kum perdesi (akan sarı tabaka) + yola yayılan kum
            float z = gz - 0.7f;
            float t = GateH - 0.05f;
            var fall = WaterFx.Sheet(new[]
            {
                new Vector3(0f, t, z + 0.1f), new Vector3(0f, t - 0.3f, z - 0.2f), new Vector3(0f, t * 0.72f, z - 0.36f),
                new Vector3(0f, t * 0.4f, z - 0.43f), new Vector3(0f, 0.9f, z - 0.47f), new Vector3(0f, 0.05f, z - 0.49f),
            }, Vector3.right, hw * 2f + 0.5f, 10, 0.3f, 0.95f, "SandFall");
            var pool = WaterFx.Sheet(new[]
            {
                new Vector3(0f, 0.05f, z - 0.49f), new Vector3(0f, 0.045f, z - 1.3f), new Vector3(0f, 0.04f, z - 2.5f),
            }, Vector3.right, hw * 2f + 0.5f, 10, 0.95f, 0f, "SandDrift");
            WaterFx.MakeWater("SandCurtain", WaterFx.Combine("De_Curtain", fall, pool), env);
        }

        // ─────────────────────────────────────────────────────
        // UZAK PLAN: kum tepeleri + dev piramitler
        // ─────────────────────────────────────────────────────
        static void BuildBackdrop(MeshBuilder b, float gz)
        {
            for (float x = -90f; x <= 90f; x += R(14f, 22f))
                b.Sphere(new Vector3(x, -6f, gz + R(20f, 34f)), new Vector3(R(14f, 22f), R(6f, 11f), R(8f, 12f)), Vary(Color.Lerp(Sand, SandShade, 0.4f), 0.04f), 10, 5, default, true);
            Pyramid(b, new Vector3(-14f, -2f, gz + 26f), 22f, true);
            Pyramid(b, new Vector3(16f, -2f, gz + 30f), 17f, true);
            Pyramid(b, new Vector3(36f, -3f, gz + 22f), 12f, false);
            Pyramid(b, new Vector3(-40f, -3f, gz + 16f), 15f, false);
        }

        static void BuildSky(Transform env, float z)
        {
            const float halfW = 140f, y0 = -40f, y1 = 42f;
            var m = new Mesh { name = "De_Sky" };
            m.SetVertices(new List<Vector3> { new Vector3(-halfW, y0, z), new Vector3(halfW, y0, z), new Vector3(halfW, y1, z), new Vector3(-halfW, y1, z) });
            m.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.RecalculateBounds();
            var go = new GameObject("Sky");
            go.transform.SetParent(env, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>();
            var db = GameDatabase.Load();
            var mat = db != null ? db.desertSkyMaterial : null;
            if (mat == null)
            {
                mat = new Material(Shader.Find("StratejiGame/SeaBackdrop"));
                mat.SetColor("_Bottom", Fog);
                mat.SetColor("_Top", new Color(0.55f, 0.68f, 0.85f));
                mat.SetColor("_Shimmer", new Color(1f, 0.9f, 0.7f));
                mat.SetFloat("_ShimmerStrength", 0.06f);
            }
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static void BuildGlowLines(Transform env, float hw, float z0, float gz)
        {
            var b = new MeshBuilder();
            b.Box(new Vector3(0f, 0.03f, 0.9f), new Vector3(hw * 2f, 0.01f, 0.1f), new Color(1f, 0.7f, 0.3f));
            b.Box(new Vector3(0f, 0.025f, 0.9f), new Vector3(hw * 2f, 0.01f, 0.45f), new Color(0.25f, 0.15f, 0.05f));
            float lw = hw * 2f / GameManager.LaneCount;
            for (int i = 1; i < GameManager.LaneCount; i++)
            {
                float x = -hw + lw * i;
                b.Box(new Vector3(x, -0.044f, (z0 + gz) * 0.5f), new Vector3(0.1f, 0.01f, gz - z0), new Color(0.18f, 0.11f, 0.04f));
            }
            FxDesigns.MakeFx("GlowLines", b.ToMesh("De_GlowLines"), env);
        }
    }
}
