using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Dondurucu Zirveler ortamı: karla kaplı, arası parlayan buz levhalardan yol; solda dipsiz dağ uçurumu
    /// (karlı kornişler, sarkan buzlar, karşıda sisli dağlar, uçurumda kar fırtınası);
    /// sağda sütun yerine parlayan buz kristali kuleleri + dev buz sarkıtları sarkan karlı kaya duvarı, karlı çam ve kayalar;
    /// buz kütlelerinden kapı + yarı saydam kar fırtınası perdesi; arkada karlı sıradağlar.
    /// Ortam materyalinde köşe alfası = parıltı (buz), sallanma yok.
    /// </summary>
    public static class FrostDesigns
    {
        // ── Palet ─────────────────────────────────────────────
        static readonly Color Snow = new Color(0.68f, 0.74f, 0.86f);
        static readonly Color SnowShade = new Color(0.5f, 0.58f, 0.74f);
        static readonly Color TileA = new Color(0.32f, 0.45f, 0.64f);
        static readonly Color TileB = new Color(0.27f, 0.39f, 0.57f);
        static readonly Color Grout = new Color(0.12f, 0.16f, 0.24f);
        static readonly Color Rock = new Color(0.3f, 0.33f, 0.4f);
        static readonly Color RockDark = new Color(0.18f, 0.2f, 0.26f);
        static readonly Color Pine = new Color(0.12f, 0.24f, 0.22f);
        static readonly Color Bark = new Color(0.22f, 0.16f, 0.12f);
        // Parlayan buz (alfa = parıltı)
        static readonly Color Ice = new Color(0.45f, 0.75f, 1f, 0.5f);
        static readonly Color IceBright = new Color(0.7f, 0.92f, 1f, 0.3f);
        static readonly Color IceDeep = new Color(0.25f, 0.5f, 0.8f, 0.55f);

        public static readonly Color Fog = new Color(0.62f, 0.7f, 0.8f);

        const float GateH = 8.2f;
        public const float CliffX = -6.4f;

        static System.Random rnd;
        static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        static bool Chance(float p) => rnd.NextDouble() < p;
        static Color Vary(Color c, float amt) { float k = R(1f - amt, 1f + amt); return new Color(c.r * k, c.g * k, c.b * k, c.a); }

        /// <summary>Karlı zemin; CliffX'in solunda dik uçurum.</summary>
        public static float GroundHeight(float x, float z)
        {
            float h = EnvironmentDesigns.SandHeight(x, z) * 0.6f + 0.02f;
            if (x < CliffX)
            {
                float k = Mathf.InverseLerp(CliffX, CliffX - 1.1f, x);
                h = Mathf.Lerp(h, -45f, Mathf.SmoothStep(0f, 1f, k));
            }
            return h;
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
            WaterFx.MakeEnv("Road", road.ToMesh("Fr_Road"), env, false);

            var ground = new MeshBuilder();
            rnd = new System.Random(5);
            BuildGround(ground, CliffX - 1.6f, 34f, nearZ - 14f, farZ + 30f, 1.1f);
            WaterFx.MakeEnv("Ground", ground.ToMesh("Fr_Ground"), env, false);

            var right = new MeshBuilder();
            rnd = new System.Random(23);
            BuildSpires(right, hw, nearZ, gateZ);
            BuildRockWall(right, hw, nearZ - 6f, gateZ + 4f);
            BuildEnemyGate(right, hw, gateZ);
            WaterFx.MakeEnv("IceAndRock", right.ToMesh("Fr_Right"), env, true);

            var wild = new MeshBuilder();
            rnd = new System.Random(37);
            BuildCliffEdge(wild, nearZ - 8f, gateZ + 4f);
            rnd = new System.Random(53);
            BuildRightDecor(wild, hw, nearZ - 6f, gateZ + 3f);
            WaterFx.MakeEnv("Wilds", wild.ToMesh("Fr_Wilds"), env, false);

            var back = new MeshBuilder();
            BuildBackdrop(back, gateZ);
            WaterFx.MakeEnv("Backdrop", back.ToMesh("Fr_Backdrop"), env, false);
            BuildSky(env, gateZ + 42f);

            BuildGlowLines(env, hw, nearZ - 6f, gateZ);
            BuildCurtain(env, hw, gateZ);
        }

        // ─────────────────────────────────────────────────────
        // YOL: buz levhalar + üstünde kar lekeleri
        // ─────────────────────────────────────────────────────
        static void BuildRoad(MeshBuilder b, float hw, float z0, float z1)
        {
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
            // Bordür: karla örtülü kaya blokları + kar yığıntıları
            for (int side = -1; side <= 1; side += 2)
            {
                float cz = z0;
                while (cz < z1)
                {
                    float len = R(1.1f, 2.3f);
                    float h = R(0.3f, 0.5f);
                    var center = new Vector3(side * (hw + 0.32f), h * 0.5f - 0.18f, cz + len * 0.5f);
                    b.Box(center, new Vector3(0.62f, h, len - 0.08f), Vary(RockDark, 0.12f), new Vector3(R(-3f, 3f), R(-3f, 3f), R(-4f, 4f)), 0.9f, 0.95f);
                    b.Sphere(center + Vector3.up * h * 0.5f, new Vector3(0.36f, 0.09f, len * 0.5f), Vary(Snow, 0.04f), 7, 3, default, true);
                    cz += len;
                }
                // Yol kenarında kar birikintisi
                for (float dz = z0; dz < z1; dz += R(2.5f, 4.5f))
                    b.Sphere(new Vector3(side * (hw + 0.95f), 0.02f, dz), new Vector3(R(0.4f, 0.7f), R(0.14f, 0.25f), R(0.8f, 1.4f)), Vary(Snow, 0.04f), 7, 3, default, true);
            }
        }

        static void Tile(MeshBuilder b, Vector3 c, float w, float d)
        {
            float y = -0.08f + R(-0.02f, 0.015f);
            var col = Vary(Color.Lerp(TileA, TileB, (float)rnd.NextDouble()), 0.06f);
            b.Box(new Vector3(c.x, y, c.z), new Vector3(w, 0.2f, d), col, Chance(0.2f) ? new Vector3(R(-1.5f, 1.5f), 0f, R(-1.5f, 1.5f)) : Vector3.zero, 0.95f, 0.95f);
            float top = y + 0.1f;
            if (Chance(0.3f))
            {
                // Savrulmuş kar lekesi
                var p = new Vector3(c.x + R(-w * 0.3f, w * 0.3f), top, c.z + R(-d * 0.3f, d * 0.3f));
                int n = 2 + rnd.Next(2);
                float r = R(0.15f, 0.3f);
                for (int i = 0; i < n; i++)
                {
                    var o = new Vector3(R(-r, r), 0f, R(-r, r)) * 0.7f;
                    b.Cylinder(p + o, p + o + Vector3.up * (0.015f + i * 0.004f), r * R(0.6f, 1f), r * 0.8f, 7, Vary(Snow, 0.04f), true, true);
                }
            }
            if (Chance(0.12f))
            {
                // Buz çatlağı (beyaz, zikzak)
                var p = new Vector3(c.x + R(-w * 0.2f, w * 0.2f), top + 0.004f, c.z - d * 0.3f);
                float a = R(-40f, 40f);
                for (int i = 0; i < 3; i++)
                {
                    float seg = d * 0.22f;
                    float ang = a + (i % 2 == 0 ? 1f : -1f) * R(20f, 35f);
                    var dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
                    b.Box(p + dir * seg * 0.5f, new Vector3(0.03f, 0.01f, seg + 0.03f), new Color(0.85f, 0.93f, 1f, 0.6f), new Vector3(0f, ang, 0f));
                    p += dir * seg;
                }
            }
        }

        // ─────────────────────────────────────────────────────
        // KARLI ZEMİN + UÇURUM YÜZÜ
        // ─────────────────────────────────────────────────────
        static void BuildGround(MeshBuilder b, float x0, float x1, float z0, float z1, float cell)
        {
            int nx = Mathf.CeilToInt((x1 - x0) / cell), nz = Mathf.CeilToInt((z1 - z0) / cell);
            var h = new float[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++)
                for (int j = 0; j <= nz; j++)
                    h[i, j] = GroundMesh(x0 + i * cell, z0 + j * cell) + (float)rnd.NextDouble() * 0.04f;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float xa = x0 + i * cell, za = z0 + j * cell;
                    var p00 = new Vector3(xa, h[i, j], za);
                    var p10 = new Vector3(xa + cell, h[i + 1, j], za);
                    var p11 = new Vector3(xa + cell, h[i + 1, j + 1], za + cell);
                    var p01 = new Vector3(xa, h[i, j + 1], za + cell);
                    float drop = Mathf.Max(Mathf.Abs(p10.y - p00.y), Mathf.Abs(p11.y - p01.y));
                    Color col = drop > 2f ? Vary(RockDark, 0.12f)                                   // uçurum yüzü: çıplak kaya
                        : Vary(Color.Lerp(SnowShade, Snow, Mathf.InverseLerp(-0.4f, 0.12f, (p00.y + p11.y) * 0.5f)), 0.03f);
                    b.Face(p00, p10, p11, p01, (p00 + p11) * 0.5f + Vector3.down, col);
                }
        }

        static void BuildCliffEdge(MeshBuilder b, float z0, float z1)
        {
            // Uçurum kenarında karlı kaya kornişleri + aşağı sarkan buzlar
            for (float z = z0; z < z1; z += R(1.2f, 2.2f))
            {
                var p = new Vector3(CliffX - R(0.1f, 0.5f), 0f, z);
                p.y = GroundHeight(CliffX + 0.3f, z) - R(0.2f, 0.6f);
                var radii = new Vector3(R(0.5f, 0.9f), R(0.4f, 0.8f), R(0.6f, 1.1f));
                b.Sphere(p, radii, Vary(Rock, 0.12f), 6, 4, new Vector3(R(-10f, 10f), R(0f, 360f), R(-10f, 10f)), true);
                b.Sphere(p + Vector3.up * radii.y * 0.75f, new Vector3(radii.x * 0.95f, radii.y * 0.35f, radii.z * 0.95f), Vary(Snow, 0.04f), 7, 3, default, true);
                if (Chance(0.55f))
                {
                    int n = 1 + rnd.Next(3);
                    for (int i = 0; i < n; i++)
                    {
                        var top = p + new Vector3(-radii.x * R(0.5f, 0.9f), -radii.y * 0.3f, R(-radii.z, radii.z) * 0.7f);
                        b.Cylinder(top, top + Vector3.down * R(0.6f, 1.6f), R(0.08f, 0.16f), 0f, 5, Chance(0.5f) ? Ice : IceBright, true, true);
                    }
                }
            }
            // Uçurumun aşağısında birkaç çıkıntı kaya (derinlik hissi)
            for (float z = z0; z < z1; z += R(4f, 7f))
            {
                var p = new Vector3(CliffX - R(1.6f, 3f), -R(3f, 9f), z);
                b.Sphere(p, new Vector3(R(0.8f, 1.6f), R(0.6f, 1.2f), R(1f, 2f)), Vary(RockDark, 0.12f), 6, 4, new Vector3(0f, R(0f, 90f), 0f), true);
                b.Sphere(p + Vector3.up * 0.6f, new Vector3(0.9f, 0.25f, 1.1f), Vary(SnowShade, 0.05f), 6, 3, default, true);
            }
        }

        // ─────────────────────────────────────────────────────
        // SAĞ: buz kristali kuleleri (sütunların yerine) + buz sarkıtlı kaya duvarı
        // ─────────────────────────────────────────────────────
        static void BuildSpires(MeshBuilder b, float hw, float nearZ, float gateZ)
        {
            int k = 0;
            for (float z = nearZ + 2f; z < gateZ - 3f; z += 7f, k++)
            {
                var p = OnGround(new Vector3(hw + 1.5f, 0f, z));
                // Karlı kaya taban
                b.Sphere(p + Vector3.up * 0.15f, new Vector3(0.85f, 0.45f, 0.85f), Vary(Rock, 0.1f), 7, 4, new Vector3(0f, R(0f, 90f), 0f), true);
                b.Sphere(p + Vector3.up * 0.5f, new Vector3(0.8f, 0.18f, 0.8f), Vary(Snow, 0.04f), 7, 3, default, true);
                // Buz kristali kümesi: bir uzun ana kristal + eğik yan kristaller
                float mainH = k % 3 == 1 ? R(2.2f, 2.8f) : R(3.2f, 4.2f);
                var basePos = p + Vector3.up * 0.4f;
                Crystal(b, basePos, Vector3.up, mainH, 0.36f, IceBright);
                int n = 3 + rnd.Next(3);
                for (int i = 0; i < n; i++)
                {
                    float a = R(0f, 6.28f);
                    var dir = new Vector3(Mathf.Cos(a) * R(0.3f, 0.6f), 1f, Mathf.Sin(a) * R(0.3f, 0.6f)).normalized;
                    Crystal(b, basePos + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.25f, dir, mainH * R(0.35f, 0.65f), R(0.16f, 0.24f), Chance(0.5f) ? Ice : IceDeep);
                }
            }
        }

        /// <summary>Altıgen, sivri uçlu buz kristali.</summary>
        static void Crystal(MeshBuilder b, Vector3 p, Vector3 dir, float h, float r, Color c)
        {
            var mid = p + dir * h * 0.75f;
            b.Cylinder(p - dir * 0.1f, mid, r, r * 0.92f, 6, c, false, true);
            b.Cylinder(mid, p + dir * h, r * 0.92f, 0f, 6, new Color(Mathf.Min(1f, c.r * 1.2f), Mathf.Min(1f, c.g * 1.1f), c.b, c.a * 0.6f), true, true);
        }

        static void BuildRockWall(MeshBuilder b, float hw, float z0, float z1)
        {
            float x0 = hw + 5.5f;
            for (float z = z0; z < z1; z += R(2.2f, 3.4f))
            {
                float h = R(5f, 8.5f);
                var c = new Vector3(x0 + R(0f, 1.5f), 0f, z);
                c.y = GroundHeight(c.x, c.z);
                // Kaya kütlesi: alt geniş, üst dar
                b.Box(c + Vector3.up * h * 0.5f, new Vector3(R(2.2f, 3.2f), h, R(2.6f, 3.6f)), Vary(Rock, 0.1f), new Vector3(0f, R(-8f, 8f), R(-4f, 4f)), 0.75f, 0.85f);
                // Üstte kar örtüsü
                b.Sphere(c + Vector3.up * h, new Vector3(R(1.3f, 1.8f), R(0.35f, 0.6f), R(1.5f, 2f)), Vary(Snow, 0.04f), 8, 4, default, true);
                // Yola doğru çıkıntı + dev buz sarkıtları
                if (Chance(0.65f))
                {
                    float ly = h * R(0.55f, 0.8f);
                    var ledge = c + new Vector3(-1.5f, ly, R(-0.5f, 0.5f));
                    b.Box(ledge, new Vector3(1.6f, 0.55f, R(1.8f, 2.6f)), Vary(RockDark, 0.1f), new Vector3(0f, 0f, R(-6f, 6f)));
                    b.Sphere(ledge + Vector3.up * 0.3f, new Vector3(0.85f, 0.2f, 1.2f), Vary(Snow, 0.04f), 7, 3, default, true);
                    int n = 3 + rnd.Next(4);
                    for (int i = 0; i < n; i++)
                    {
                        var top = ledge + new Vector3(R(-0.7f, 0.3f), -0.25f, R(-0.9f, 0.9f));
                        float len = i == 0 ? R(2f, 3.2f) : R(0.7f, 2f);
                        b.Cylinder(top, top + Vector3.down * len, R(0.14f, 0.3f), 0f, 6, Chance(0.5f) ? Ice : IceBright, true, true);
                    }
                }
                // Kaya yüzünde koyu çatlaklar
                b.Box(c + new Vector3(-1.1f, h * R(0.3f, 0.6f), R(-0.8f, 0.8f)), new Vector3(0.05f, R(1f, 2f), 0.06f), RockDark, new Vector3(0f, 0f, R(-10f, 10f)));
            }
        }

        static void BuildRightDecor(MeshBuilder b, float hw, float z0, float z1)
        {
            for (float z = z0; z < z1; z += 3f)
                for (float x = hw + 2.8f; x < hw + 5f; x += 2.2f)
                {
                    if (!Chance(0.5f)) continue;
                    var p = OnGround(new Vector3(x + R(0f, 1.5f), 0f, z + R(0f, 2.5f)));
                    float roll = (float)rnd.NextDouble();
                    if (roll < 0.4f) SnowPine(b, p, R(1.6f, 2.8f));
                    else if (roll < 0.75f) SnowRock(b, p, R(0.5f, 1f));
                    else b.Sphere(p, new Vector3(R(0.6f, 1f), R(0.2f, 0.35f), R(0.6f, 1.1f)), Vary(Snow, 0.04f), 7, 3, default, true);
                }
        }

        static void SnowPine(MeshBuilder b, Vector3 p, float h)
        {
            b.Cylinder(p - Vector3.up * 0.1f, p + Vector3.up * h * 0.3f, 0.1f * h / 2f, 0.07f, 5, Bark, false, true);
            for (int i = 0; i < 3; i++)
            {
                float y0 = h * (0.2f + i * 0.25f);
                float r = h * (0.36f - i * 0.09f);
                b.Cylinder(p + Vector3.up * y0, p + Vector3.up * (y0 + h * 0.38f), r, 0f, 7, Vary(Pine, 0.1f), true, true);
                // Dalların üstünde kar
                b.Cylinder(p + Vector3.up * (y0 + h * 0.12f), p + Vector3.up * (y0 + h * 0.3f), r * 0.7f, 0f, 7, Vary(Snow, 0.03f), false, true);
            }
        }

        static void SnowRock(MeshBuilder b, Vector3 p, float s)
        {
            var radii = new Vector3(R(0.5f, 0.8f), R(0.35f, 0.55f), R(0.5f, 0.8f)) * s;
            b.Sphere(p + Vector3.up * radii.y * 0.3f, radii, Vary(Rock, 0.1f), 6, 4, new Vector3(R(-10f, 10f), R(0f, 360f), 0f), true);
            b.Sphere(p + Vector3.up * radii.y * 0.95f, new Vector3(radii.x * 0.85f, radii.y * 0.3f, radii.z * 0.85f), Vary(Snow, 0.03f), 6, 3, default, true);
        }

        // ─────────────────────────────────────────────────────
        // DÜŞMAN KAPISI: yarı saydam buz kütleleri + buz sarkıtlı lento
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
                    float h = i == 0 ? 1.8f : Mathf.Min(R(1.3f, 1.7f), GateH - y);
                    float w = i == 0 ? 2.2f : R(1.8f, 2f);
                    b.Box(new Vector3(x + R(-0.08f, 0.08f), y + h * 0.5f, gz), new Vector3(w, h - 0.04f, w), i % 2 == 0 ? Ice : IceDeep,
                        new Vector3(R(-2f, 2f), R(-6f, 6f), R(-2f, 2f)), 0.95f, 0.95f);
                    // Buzun içindeki koyu çatlak / hava kabarcığı izleri
                    b.Box(new Vector3(x + R(-0.5f, 0.5f), y + h * 0.5f, gz - w * 0.5f - 0.01f), new Vector3(0.04f, h * 0.6f, 0.02f), new Color(0.85f, 0.95f, 1f, 0.4f), new Vector3(0f, 0f, R(-25f, 25f)));
                    y += h;
                    i++;
                }
                // Tabanda kar yığını
                b.Sphere(new Vector3(x, 0.1f, gz - 0.6f), new Vector3(1.5f, 0.5f, 1.2f), Vary(Snow, 0.03f), 8, 4, default, true);
            }
            // Lento: kalın buz blok + üstte kar + önünde buz sarkıtı saçağı
            b.Box(new Vector3(0f, GateH + 0.65f, gz), new Vector3(hw * 2f + 4.4f, 1.3f, 2.2f), Ice);
            b.Sphere(new Vector3(0f, GateH + 1.35f, gz), new Vector3(hw + 2.4f, 0.45f, 1.2f), Vary(Snow, 0.03f), 10, 4, default, true);
            for (int k = 0; k < 16; k++)
            {
                var top = new Vector3(R(-hw - 1.8f, hw + 1.8f), GateH + 0.02f, gz - 1.02f);
                b.Cylinder(top, top + Vector3.down * R(0.3f, 1.2f), R(0.08f, 0.18f), 0f, 5, Chance(0.5f) ? IceBright : Ice, true, true);
            }
            // Lentoda parlayan buz rünü
            b.Box(new Vector3(0f, GateH + 0.65f, gz - 1.11f), new Vector3(0.5f, 0.5f, 0.02f), new Color(0.7f, 0.95f, 1f, 0f), new Vector3(0f, 0f, 45f));
            b.Box(new Vector3(0f, GateH + 0.65f, gz - 1.12f), new Vector3(0.22f, 0.22f, 0.02f), new Color(0.2f, 0.4f, 0.6f), new Vector3(0f, 0f, 45f));
            // Tünel
            var dark = new Color(0.08f, 0.12f, 0.2f);
            b.Box(new Vector3(0f, GateH * 0.5f, gz + 3.2f), new Vector3(hw * 2f + 1f, GateH, 0.3f), dark);
            for (int side = -1; side <= 1; side += 2)
                b.Box(new Vector3(side * (hw + 0.4f), GateH * 0.5f, gz + 2.1f), new Vector3(0.3f, GateH, 2.4f), dark);
            b.Box(new Vector3(0f, GateH + 0.1f, gz + 2.1f), new Vector3(hw * 2f + 1f, 0.2f, 2.4f), dark);
        }

        static void BuildCurtain(Transform env, float hw, float gz)
        {
            // Kapıdaki yarı saydam kar fırtınası perdesi (akan beyaz tabaka) + yola savrulan kar
            float z = gz - 0.7f;
            float t = GateH - 0.05f;
            var fall = WaterFx.Sheet(new[]
            {
                new Vector3(0f, t, z + 0.1f), new Vector3(0f, t - 0.3f, z - 0.2f), new Vector3(0f, t * 0.72f, z - 0.36f),
                new Vector3(0f, t * 0.4f, z - 0.43f), new Vector3(0f, 0.9f, z - 0.47f), new Vector3(0f, 0.05f, z - 0.49f),
            }, Vector3.right, hw * 2f + 0.5f, 10, 0.3f, 0.95f, "BlizzardFall");
            var pool = WaterFx.Sheet(new[]
            {
                new Vector3(0f, 0.05f, z - 0.49f), new Vector3(0f, 0.045f, z - 1.3f), new Vector3(0f, 0.04f, z - 2.5f),
            }, Vector3.right, hw * 2f + 0.5f, 10, 0.95f, 0f, "SnowDrift");
            WaterFx.MakeWater("Blizzard", WaterFx.Combine("Fr_Curtain", fall, pool), env);
        }

        // ─────────────────────────────────────────────────────
        // UZAK PLAN: karlı sıradağlar (uçurumun karşısı dahil)
        // ─────────────────────────────────────────────────────
        static void BuildBackdrop(MeshBuilder b, float gz)
        {
            rnd = new System.Random(71);
            // Arkadaki zirveler
            for (float x = -80f; x <= 80f; x += R(12f, 18f))
            {
                float side = Mathf.InverseLerp(0f, 80f, Mathf.Abs(x));
                float z = gz + R(22f, 34f) - side * 4f;
                float h = R(14f, 24f) * Mathf.Lerp(1.2f, 0.9f, side);
                Peak(b, new Vector3(x, -8f, z), h, R(8f, 13f));
            }
            // Kapının hemen arkasında yüksek, sivri ana zirve
            Peak(b, new Vector3(2f, -6f, gz + 20f), 26f, 12f);
            // Uçurumun karşısındaki dağ duvarı (solda, aşağısı siste kaybolur)
            for (float z = gz - 60f; z < gz + 20f; z += R(9f, 14f))
                Peak(b, new Vector3(R(-48f, -36f), -30f, z), R(34f, 44f), R(9f, 14f));
        }

        static void Peak(MeshBuilder b, Vector3 p, float h, float r)
        {
            var rock = Vary(new Color(0.34f, 0.38f, 0.48f), 0.08f);
            float snowLine = R(0.45f, 0.6f);
            b.Cylinder(p, p + Vector3.up * h * snowLine, r, r * (1f - snowLine) + 0.2f, 7, rock, false, true);
            b.Cylinder(p + Vector3.up * h * snowLine, p + Vector3.up * h, r * (1f - snowLine) + 0.25f, 0f, 7, Vary(new Color(0.82f, 0.87f, 0.95f), 0.03f), true, true);
            // Karın aşağıya akan dilleri
            for (int i = 0; i < 3; i++)
            {
                float a = (200f + i * 50f + R(-15f, 15f)) * Mathf.Deg2Rad;
                float rr = r * (1f - snowLine) + 0.3f;
                var top = p + new Vector3(Mathf.Cos(a) * rr, h * snowLine, Mathf.Sin(a) * rr);
                var bot = p + new Vector3(Mathf.Cos(a) * r * 0.75f, h * snowLine * R(0.4f, 0.6f), Mathf.Sin(a) * r * 0.75f);
                b.Cylinder(top, bot, r * 0.12f, r * 0.05f, 4, new Color(0.8f, 0.85f, 0.94f), true, true);
            }
        }

        static void BuildSky(Transform env, float z)
        {
            const float halfW = 140f, y0 = -40f, y1 = 42f;
            var m = new Mesh { name = "Fr_Sky" };
            m.SetVertices(new List<Vector3> { new Vector3(-halfW, y0, z), new Vector3(halfW, y0, z), new Vector3(halfW, y1, z), new Vector3(-halfW, y1, z) });
            m.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.RecalculateBounds();
            var go = new GameObject("Sky");
            go.transform.SetParent(env, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>();
            var db = GameDatabase.Load();
            var mat = db != null ? db.frostSkyMaterial : null;
            if (mat == null)
            {
                mat = new Material(Shader.Find("StratejiGame/SeaBackdrop"));
                mat.SetColor("_Bottom", Fog);
                mat.SetColor("_Top", new Color(0.36f, 0.47f, 0.62f));
                mat.SetColor("_Shimmer", new Color(0.8f, 0.9f, 1f));
                mat.SetFloat("_ShimmerStrength", 0.1f);
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
                b.Box(new Vector3(x, -0.044f, (z0 + gz) * 0.5f), new Vector3(0.1f, 0.01f, gz - z0), new Color(0.06f, 0.12f, 0.2f));
            }
            FxDesigns.MakeFx("GlowLines", b.ToMesh("Fr_GlowLines"), env);
        }
    }
}
