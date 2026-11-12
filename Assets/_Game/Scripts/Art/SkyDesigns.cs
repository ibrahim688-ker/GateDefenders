using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Gökyüzü Tapınağı ortamı (karanlık fırtına): bulutların üstünde süzülen, altın derzli beyaz mermer yol;
    /// solda boşluk — altta koyu fırtına bulutları denizi (şimşekler SkyLife'ta), yolun altında yüzen ada temeli;
    /// sağda mermer tapınak zemini üzerinde altın rün bantlı Yunan sütunları (bazıları kırık, tepelerinde şimşek iletkeni),
    /// arka sırada lentolu yüksek sütunlar, devrik sütun tamburları, altın mangallar;
    /// kapı = altın sütunlar + mermer alınlık + elektrik küreleri + elektrikli ışık perdesi; uzakta yüzen tapınak adaları.
    /// Ortam materyalinde köşe alfası = parıltı (altın süsler, rünler), sallanma yok.
    /// </summary>
    public static class SkyDesigns
    {
        // ── Palet ─────────────────────────────────────────────
        static readonly Color MarbleA = new Color(0.82f, 0.82f, 0.86f);
        static readonly Color MarbleB = new Color(0.72f, 0.72f, 0.78f);
        static readonly Color MarbleDark = new Color(0.46f, 0.47f, 0.57f);
        static readonly Color Vein = new Color(0.52f, 0.54f, 0.64f);
        static readonly Color Stone = new Color(0.24f, 0.24f, 0.32f);
        static readonly Color StoneDark = new Color(0.14f, 0.14f, 0.2f);
        static readonly Color CloudDark = new Color(0.22f, 0.22f, 0.34f);
        static readonly Color CloudLight = new Color(0.36f, 0.37f, 0.52f);
        // Parlayan altın / elektrik (alfa = parıltı)
        static readonly Color Gold = new Color(0.95f, 0.7f, 0.25f, 0.75f);
        static readonly Color GoldGlow = new Color(1f, 0.78f, 0.32f, 0.35f);
        static readonly Color GoldSeam = new Color(0.58f, 0.43f, 0.2f, 0.9f);
        static readonly Color Electric = new Color(0.5f, 0.82f, 1f, 0.1f);
        static readonly Color Rune = new Color(0.6f, 0.88f, 1f, 0f);

        public static readonly Color Fog = new Color(0.17f, 0.17f, 0.29f);

        const float GateH = 8.2f;
        /// <summary>Sağdaki tapınak zemininin dış kenarı (hw'ye göre).</summary>
        const float PlatformW = 7.6f;

        /// <summary>Şimşek iletkenlerinin uçları (SkyLife sütunlar arasında elektrik arkı çakar).</summary>
        public static readonly List<Vector3> RodTips = new List<Vector3>();
        /// <summary>Kapının üstündeki iki elektrik küresi.</summary>
        public static Vector3 GateOrbL, GateOrbR;

        static System.Random rnd;
        static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        static bool Chance(float p) => rnd.NextDouble() < p;
        static Color Vary(Color c, float amt) { float k = R(1f - amt, 1f + amt); return new Color(c.r * k, c.g * k, c.b * k, c.a); }

        public static void Build(Transform env, float hw, float nearZ, float farZ, float gateZ)
        {
            WaterFx.Init();
            RodTips.Clear();

            var road = new MeshBuilder();
            rnd = new System.Random(11);
            BuildRoad(road, hw, nearZ - 6f, gateZ + 2.5f);
            WaterFx.MakeEnv("Road", road.ToMesh("Sk_Road"), env, false);

            var plat = new MeshBuilder();
            rnd = new System.Random(5);
            BuildPlatform(plat, hw, nearZ - 8f, gateZ + 3f);
            WaterFx.MakeEnv("Platform", plat.ToMesh("Sk_Platform"), env, false);

            var temple = new MeshBuilder();
            rnd = new System.Random(23);
            BuildColumns(temple, hw, nearZ, gateZ);
            BuildEnemyGate(temple, hw, gateZ);
            WaterFx.MakeEnv("Temple", temple.ToMesh("Sk_Temple"), env, true);

            var ruins = new MeshBuilder();
            rnd = new System.Random(53);
            BuildRightDecor(ruins, hw, nearZ - 6f, gateZ + 2f);
            WaterFx.MakeEnv("Ruins", ruins.ToMesh("Sk_Ruins"), env, true);

            var clouds = new MeshBuilder();
            rnd = new System.Random(37);
            BuildCloudSea(clouds, hw, nearZ - 22f, farZ + 40f);
            WaterFx.MakeEnv("CloudSea", clouds.ToMesh("Sk_CloudSea"), env, false);

            var back = new MeshBuilder();
            rnd = new System.Random(71);
            BuildBackdrop(back, hw, nearZ, gateZ);
            WaterFx.MakeEnv("Backdrop", back.ToMesh("Sk_Backdrop"), env, false);
            BuildSky(env, gateZ + 42f);

            BuildGlowLines(env, hw, nearZ - 6f, gateZ);
            BuildCurtain(env, hw, gateZ);
        }

        // ─────────────────────────────────────────────────────
        // YOL: altın derzli büyük mermer levhalar
        // ─────────────────────────────────────────────────────
        static void BuildRoad(MeshBuilder b, float hw, float z0, float z1)
        {
            b.Box(new Vector3(0f, -0.2f, (z0 + z1) * 0.5f), new Vector3(hw * 2f + 0.1f, 0.3f, z1 - z0), GoldSeam);
            float lw = hw * 2f / GameManager.LaneCount;
            const float gap = 0.06f;
            float z = z0;
            while (z < z1)
            {
                float rowD = Mathf.Min(R(1.1f, 1.7f), z1 - z);
                if (rowD < 0.4f) break;
                for (int lane = 0; lane < GameManager.LaneCount; lane++)
                {
                    float xStart = -hw + lane * lw + 0.06f;
                    float xEnd = -hw + (lane + 1) * lw - 0.06f;
                    float x = xStart;
                    while (x < xEnd - 0.3f)
                    {
                        float w = R(0.9f, 1.6f);
                        if (xEnd - (x + w) < 0.6f) w = xEnd - x;
                        Slab(b, new Vector3(x + w * 0.5f, 0f, z + rowD * 0.5f), w - gap, rowD - gap);
                        x += w;
                    }
                }
                z += rowD;
            }
            // Bordür: altın şeritli mermer bloklar
            for (int side = -1; side <= 1; side += 2)
            {
                float cz = z0;
                while (cz < z1)
                {
                    float len = R(1.6f, 2.6f);
                    var center = new Vector3(side * (hw + 0.32f), 0.04f, cz + len * 0.5f);
                    b.Box(center, new Vector3(0.62f, 0.44f, len - 0.06f), Vary(MarbleA, 0.04f), default, 0.94f, 0.98f);
                    b.Box(center + new Vector3(0f, 0.225f, 0f), new Vector3(0.14f, 0.02f, len - 0.3f), GoldSeam);
                    // Yol tarafında altın meander (köşeli dalga) şeridi
                    b.Box(center + new Vector3(-side * 0.315f, 0.02f, 0f), new Vector3(0.01f, 0.05f, len - 0.2f), GoldSeam);
                    cz += len;
                }
            }
        }

        static void Slab(MeshBuilder b, Vector3 c, float w, float d)
        {
            float y = -0.08f + R(-0.015f, 0.01f);
            var col = Vary(Color.Lerp(MarbleA, MarbleB, (float)rnd.NextDouble()), 0.04f);
            b.Box(new Vector3(c.x, y, c.z), new Vector3(w, 0.2f, d), col, default, 0.97f, 0.97f);
            float top = y + 0.1f + 0.003f;
            if (Chance(0.45f))
            {
                // Mermer damarı: ince, eğik, kırık çizgi
                var p = new Vector3(c.x + R(-w * 0.35f, w * 0.35f), top, c.z - d * 0.4f);
                float a = R(-50f, 50f);
                for (int i = 0; i < 3; i++)
                {
                    float seg = d * R(0.2f, 0.32f);
                    float ang = a + R(-25f, 25f);
                    var dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
                    b.Box(p + dir * seg * 0.5f, new Vector3(0.025f, 0.006f, seg), Vein, new Vector3(0f, ang, 0f));
                    p += dir * seg;
                }
            }
            if (Chance(0.12f))
            {
                // Altın kakma: ortada eşkenar dörtgen
                b.Box(new Vector3(c.x, top, c.z), new Vector3(0.36f, 0.008f, 0.36f), Gold, new Vector3(0f, 45f, 0f));
                b.Box(new Vector3(c.x, top + 0.002f, c.z), new Vector3(0.2f, 0.008f, 0.2f), Vary(MarbleA, 0.02f), new Vector3(0f, 45f, 0f));
            }
            else if (Chance(0.08f))
            {
                // Çatlak / kırık köşe (koyu)
                b.Box(new Vector3(c.x + w * 0.38f, top - 0.004f, c.z + d * 0.38f), new Vector3(w * 0.22f, 0.01f, d * 0.22f), MarbleDark, new Vector3(0f, R(10f, 50f), 0f));
            }
        }

        // ─────────────────────────────────────────────────────
        // ZEMİN: sağda tapınak döşemesi + yolun/zeminin altında yüzen ada temeli
        // ─────────────────────────────────────────────────────
        static void BuildPlatform(MeshBuilder b, float hw, float z0, float z1)
        {
            float x0 = hw + 0.64f, x1 = hw + PlatformW;
            // Döşeme (büyük, daha koyu, yer yer eksik/çatlak levhalar)
            float z = z0;
            while (z < z1)
            {
                float rowD = R(1.6f, 2.4f);
                float x = x0;
                while (x < x1 - 0.4f)
                {
                    float w = Mathf.Min(R(1.4f, 2.4f), x1 - x);
                    if (!Chance(0.06f))
                    {
                        float y = -0.12f + R(-0.04f, 0.02f);
                        var tilt = Chance(0.15f) ? new Vector3(R(-3f, 3f), R(-4f, 4f), R(-3f, 3f)) : Vector3.zero;
                        b.Box(new Vector3(x + w * 0.5f, y, z + rowD * 0.5f), new Vector3(w - 0.08f, 0.24f, rowD - 0.08f), Vary(Color.Lerp(MarbleB, MarbleDark, 0.35f), 0.05f), tilt, 0.97f, 0.97f);
                    }
                    x += w;
                }
                z += rowD;
            }
            b.Box(new Vector3((x0 + x1) * 0.5f, -0.3f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.2f, z1 - z0), StoneDark);

            // Yüzen ada temeli: kenarda altın şeritli korniş, altta basamaklı mermer, en altta sivrilen koyu kaya
            float l = -hw - 0.64f;
            float cx = (l + x1) * 0.5f, w0 = x1 - l;
            b.Box(new Vector3(cx, -0.45f, (z0 + z1) * 0.5f), new Vector3(w0 + 0.3f, 0.3f, z1 - z0), MarbleA);
            b.Box(new Vector3(l - 0.15f, -0.33f, (z0 + z1) * 0.5f), new Vector3(0.04f, 0.06f, z1 - z0), Gold);
            b.Box(new Vector3(cx, -1.05f, (z0 + z1) * 0.5f), new Vector3(w0 - 0.4f, 0.9f, z1 - z0), Vary(MarbleDark, 0.03f));
            // Dişli (dentil) süs sırası
            for (float dz = z0; dz < z1; dz += 0.55f)
                b.Box(new Vector3(l + 0.1f, -0.7f, dz), new Vector3(0.12f, 0.16f, 0.28f), MarbleA);
            for (float dz = z0; dz < z1; dz += R(2.5f, 4f))
            {
                // Aşağı sarkan kaya kütleleri (adanın altı)
                var top = new Vector3(R(l + 0.5f, x1 - 1f), -1.4f, dz);
                float r = R(1.2f, 2.4f);
                b.Cylinder(top, top + new Vector3(R(-0.5f, 0.5f), -R(3f, 6.5f), R(-0.5f, 0.5f)), r, 0f, 6, Vary(Stone, 0.12f), true, true);
            }
        }

        // ─────────────────────────────────────────────────────
        // SAĞ: Yunan sütunları (ön sıra + lentolu arka sıra)
        // ─────────────────────────────────────────────────────
        static void BuildColumns(MeshBuilder b, float hw, float nearZ, float gateZ)
        {
            int k = 0;
            for (float z = nearZ + 2f; z < gateZ - 3f; z += 7f, k++)
            {
                var p = new Vector3(hw + 1.7f, 0f, z);
                bool broken = k % 3 == 1;
                Column(b, p, broken ? R(1.8f, 3f) : R(5.2f, 5.8f), 0.46f, broken, true);
            }
            // Arka sıra: daha yüksek sütunlar; yan yana sağlam olanların üstünde mermer lento
            Vector3 prevTop = Vector3.zero;
            bool prevIntact = false;
            k = 0;
            for (float z = nearZ - 1.5f; z < gateZ + 2f; z += 7f, k++)
            {
                var p = new Vector3(hw + 5.4f, 0f, z);
                bool intact = k % 4 != 2;
                float h = 7.4f;
                Column(b, p, intact ? h : R(2f, 3.5f), 0.55f, !intact, false);
                var top = p + Vector3.up * (h + 0.4f);
                if (intact && prevIntact)
                {
                    float len = top.z - prevTop.z + 1.4f;
                    b.Box(new Vector3(p.x, top.y + 0.35f, (top.z + prevTop.z) * 0.5f), new Vector3(1.5f, 0.7f, len), Vary(MarbleA, 0.03f));
                    b.Box(new Vector3(p.x - 0.76f, top.y + 0.35f, (top.z + prevTop.z) * 0.5f), new Vector3(0.03f, 0.18f, len - 0.2f), Gold);
                    b.Box(new Vector3(p.x, top.y + 0.78f, (top.z + prevTop.z) * 0.5f), new Vector3(1.7f, 0.16f, len + 0.2f), Vary(MarbleB, 0.03f));
                }
                prevTop = top;
                prevIntact = intact;
            }
        }

        /// <summary>Oluklu mermer sütun: kaide, altın rün bandı, başlık, (sağlamsa) tepede şimşek iletkeni.</summary>
        static void Column(MeshBuilder b, Vector3 p, float h, float r, bool broken, bool rod)
        {
            b.Box(p + Vector3.up * 0.12f, new Vector3(r * 2.9f, 0.24f, r * 2.9f), Vary(MarbleB, 0.04f));
            b.Cylinder(p + Vector3.up * 0.24f, p + Vector3.up * 0.42f, r * 1.25f, r * 1.12f, 12, MarbleA, true, true);
            var shaftTop = p + Vector3.up * h;
            b.Cylinder(p + Vector3.up * 0.42f, shaftTop, r, r * 0.9f, 10, Vary(MarbleA, 0.03f), !broken, true);
            // Oluklar
            for (int i = 0; i < 10; i++)
            {
                float a = (i + 0.5f) / 10f * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float hh = h - 0.6f;
                b.Box(p + d * (r * 0.93f) + Vector3.up * (0.45f + hh * 0.5f), new Vector3(0.05f, hh, 0.05f), MarbleDark, new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
            }
            // Altın rün bandı (yol tarafında parlayan rünler)
            float by = Mathf.Min(h * 0.38f, 2f);
            b.Cylinder(p + Vector3.up * by, p + Vector3.up * (by + 0.28f), r * 1.06f, r * 1.06f, 10, Gold, false, true);
            for (int i = -1; i <= 1; i++)
            {
                var rp = p + new Vector3(-r * 1.08f, by + 0.14f, i * 0.2f);
                b.Box(rp, new Vector3(0.02f, 0.14f, 0.05f), Rune, new Vector3(0f, 0f, i * 25f));
            }
            if (broken)
            {
                // Kırık uç: eğik, sivri parçalar + yerde devrik tambur
                for (int i = 0; i < 3; i++)
                {
                    float a = R(0f, 6.28f);
                    var o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r * 0.45f;
                    b.Cylinder(shaftTop + o, shaftTop + o + new Vector3(R(-0.1f, 0.1f), R(0.2f, 0.5f), R(-0.1f, 0.1f)), r * 0.45f, 0f, 5, MarbleA, true, true);
                }
                var drum = p + new Vector3(R(0.6f, 1.3f), r * 0.85f, R(-1.8f, 1.8f));
                var axis = Quaternion.Euler(0f, R(0f, 180f), 0f) * Vector3.forward;
                b.Cylinder(drum - axis * 0.55f, drum + axis * 0.55f, r * 0.85f, r * 0.85f, 10, Vary(MarbleA, 0.04f), true, true);
                return;
            }
            // Başlık (iyon/dor karışımı) + abaküs
            b.Cylinder(shaftTop, shaftTop + Vector3.up * 0.22f, r * 0.9f, r * 1.3f, 12, MarbleA, true, true);
            b.Box(shaftTop + Vector3.up * 0.32f, new Vector3(r * 3f, 0.2f, r * 3f), Vary(MarbleB, 0.03f));
            b.Box(shaftTop + new Vector3(-r * 1.5f, 0.32f, 0f), new Vector3(0.02f, 0.08f, r * 2.6f), Gold);
            if (!rod) return;
            // Şimşek iletkeni: altın çubuk + elektrik küresi
            var rb = shaftTop + Vector3.up * 0.42f;
            var tip = rb + Vector3.up * 1.1f;
            b.Cylinder(rb, tip, 0.07f, 0.045f, 6, Gold, true, true);
            b.Cylinder(rb + Vector3.up * 0.3f, rb + Vector3.up * 0.36f, 0.16f, 0.16f, 8, Gold, true, true);
            b.Sphere(tip, Vector3.one * 0.17f, Electric, 7, 5);
            RodTips.Add(tip);
        }

        static void BuildRightDecor(MeshBuilder b, float hw, float z0, float z1)
        {
            for (float z = z0; z < z1; z += 3.2f)
            {
                if (!Chance(0.6f)) continue;
                var p = new Vector3(hw + R(2.8f, 4.3f), 0f, z + R(0f, 2.5f));
                float roll = (float)rnd.NextDouble();
                if (roll < 0.35f)
                {
                    // Devrik sütun tamburu
                    var axis = Quaternion.Euler(0f, R(0f, 180f), 0f) * Vector3.forward;
                    float rr = R(0.35f, 0.5f);
                    var c = p + Vector3.up * rr;
                    b.Cylinder(c - axis * R(0.4f, 0.7f), c + axis * R(0.4f, 0.7f), rr, rr, 10, Vary(MarbleA, 0.04f), true, true);
                }
                else if (roll < 0.6f)
                {
                    // Altın mangal: içinde elektrik mavisi alev
                    b.Cylinder(p, p + Vector3.up * 0.7f, 0.08f, 0.06f, 6, Gold, true, true);
                    b.Cylinder(p, p + Vector3.up * 0.08f, 0.3f, 0.26f, 8, Gold, true, true);
                    b.Cylinder(p + Vector3.up * 0.7f, p + Vector3.up * 0.95f, 0.18f, 0.36f, 8, Gold, true, true);
                    b.Cylinder(p + Vector3.up * 0.9f, p + Vector3.up * 1.45f, 0.26f, 0f, 6, Electric, true, true);
                    b.Cylinder(p + Vector3.up * 0.9f, p + Vector3.up * 1.2f, 0.16f, 0f, 6, new Color(0.9f, 0.97f, 1f, 0f), true, true);
                }
                else if (roll < 0.8f)
                {
                    // Kırık heykel kaidesi + üstünde kırık parça
                    b.Box(p + Vector3.up * 0.45f, new Vector3(0.9f, 0.9f, 0.9f), Vary(MarbleB, 0.04f), new Vector3(0f, R(0f, 40f), 0f));
                    b.Box(p + Vector3.up * 0.93f, new Vector3(1f, 0.08f, 1f), Gold, new Vector3(0f, R(0f, 40f), 0f));
                    b.Sphere(p + Vector3.up * 1.2f, new Vector3(0.28f, 0.35f, 0.26f), MarbleA, 6, 4, new Vector3(R(-20f, 20f), 0f, R(-20f, 20f)), true);
                }
                else
                {
                    // Mermer moloz
                    for (int i = 0; i < 3; i++)
                        b.Box(p + new Vector3(R(-0.6f, 0.6f), 0.15f, R(-0.6f, 0.6f)), new Vector3(R(0.3f, 0.6f), R(0.2f, 0.35f), R(0.3f, 0.6f)), Vary(MarbleB, 0.06f), new Vector3(R(-10f, 10f), R(0f, 90f), R(-10f, 10f)));
                }
            }
        }

        // ─────────────────────────────────────────────────────
        // BULUT DENİZİ (adanın altında ve çevresinde)
        // ─────────────────────────────────────────────────────
        static void BuildCloudSea(MeshBuilder b, float hw, float z0, float z1)
        {
            for (float z = z0; z < z1; z += R(3f, 5f))
            {
                for (float x = -70f; x < 70f; x += R(5f, 9f))
                {
                    if (x > -hw - 3f && x < hw + PlatformW + 2f) continue;   // adanın altı görünmez
                    float near = Mathf.InverseLerp(30f, 6f, Mathf.Abs(x));
                    var p = new Vector3(x + R(-2f, 2f), R(-7.5f, -5.5f) + near * 1.2f, z + R(-1.5f, 1.5f));
                    float s = R(3f, 5.5f);
                    b.Sphere(p, new Vector3(s, s * R(0.28f, 0.4f), s * R(0.6f, 0.9f)), Vary(Color.Lerp(CloudDark, CloudLight, (float)rnd.NextDouble()), 0.04f), 8, 4, new Vector3(0f, R(0f, 90f), 0f), true);
                    if (Chance(0.5f))
                        b.Sphere(p + new Vector3(R(-1f, 1f), s * 0.25f, R(-1f, 1f)), new Vector3(s * 0.55f, s * 0.3f, s * 0.5f), Vary(CloudLight, 0.05f), 7, 4, default, true);
                }
            }
        }

        // ─────────────────────────────────────────────────────
        // DÜŞMAN KAPISI: altın sütunlar, mermer alınlık, elektrik küreleri
        // ─────────────────────────────────────────────────────
        static void BuildEnemyGate(MeshBuilder b, float hw, float gz)
        {
            var goldSolid = new Color(0.85f, 0.6f, 0.2f, 0.85f);
            for (int side = -1; side <= 1; side += 2)
            {
                var p = new Vector3(side * (hw + 1.0f), 0f, gz);
                b.Box(p + Vector3.up * 0.3f, new Vector3(2.2f, 0.6f, 2.2f), Vary(MarbleB, 0.03f));
                b.Box(p + Vector3.up * 0.66f, new Vector3(1.9f, 0.12f, 1.9f), goldSolid);
                b.Cylinder(p + Vector3.up * 0.7f, p + Vector3.up * GateH, 0.72f, 0.64f, 12, goldSolid, false, true);
                // Elektrik mavisi rün bantları
                for (int i = 0; i < 3; i++)
                {
                    float y = 2f + i * 2.1f;
                    b.Cylinder(p + Vector3.up * y, p + Vector3.up * (y + 0.18f), 0.73f, 0.73f, 12, Electric, false, true);
                }
                b.Cylinder(p + Vector3.up * GateH, p + Vector3.up * (GateH + 0.3f), 0.66f, 0.95f, 12, goldSolid, true, true);
            }
            // Alınlık: mermer saçaklık + altın friz + üçgen alınlık
            float W = hw * 2f + 4.4f;
            b.Box(new Vector3(0f, GateH + 0.65f, gz), new Vector3(W, 0.7f, 2.2f), MarbleA);
            b.Box(new Vector3(0f, GateH + 0.6f, gz - 1.11f), new Vector3(W - 0.4f, 0.22f, 0.02f), Gold);
            b.Box(new Vector3(0f, GateH + 1.06f, gz), new Vector3(W + 0.3f, 0.14f, 2.4f), MarbleB);
            b.Box(new Vector3(0f, GateH + 1.85f, gz), new Vector3(W, 1.45f, 2f), MarbleA, default, 0.02f, 1f);
            b.Box(new Vector3(0f, GateH + 1.8f, gz - 1.01f), new Vector3(W * 0.82f, 1.1f, 0.02f), MarbleDark, default, 0.02f, 1f);
            // Alınlıkta parlayan şimşek amblemi
            var e0 = new Vector3(-0.15f, GateH + 2.3f, gz - 1.04f);
            var pts = new[] { e0, e0 + new Vector3(0.3f, -0.35f, 0f), e0 + new Vector3(0.05f, -0.4f, 0f), e0 + new Vector3(0.3f, -0.85f, 0f) };
            for (int i = 0; i < pts.Length - 1; i++) b.Cylinder(pts[i], pts[i + 1], 0.07f, i == pts.Length - 2 ? 0f : 0.07f, 5, Rune, true, true);
            // Köşelerdeki elektrik küreleri (aralarında şimşek çakar)
            for (int side = -1; side <= 1; side += 2)
            {
                var o = new Vector3(side * (W * 0.5f - 0.4f), GateH + 1.25f, gz - 0.3f);
                b.Cylinder(o, o + Vector3.up * 0.4f, 0.22f, 0.14f, 8, goldSolid, true, true);
                b.Sphere(o + Vector3.up * 0.7f, Vector3.one * 0.36f, Electric, 8, 6);
                if (side < 0) GateOrbL = o + Vector3.up * 0.7f; else GateOrbR = o + Vector3.up * 0.7f;
            }
            // Tünel
            var dark = new Color(0.05f, 0.05f, 0.1f);
            b.Box(new Vector3(0f, GateH * 0.5f, gz + 3.2f), new Vector3(hw * 2f + 1f, GateH, 0.3f), dark);
            for (int side = -1; side <= 1; side += 2)
                b.Box(new Vector3(side * (hw + 0.4f), GateH * 0.5f, gz + 2.1f), new Vector3(0.3f, GateH, 2.4f), dark);
            b.Box(new Vector3(0f, GateH + 0.1f, gz + 2.1f), new Vector3(hw * 2f + 1f, 0.2f, 2.4f), dark);
        }

        static void BuildCurtain(Transform env, float hw, float gz)
        {
            // Kapıdaki elektrikli ışık perdesi (akan mavi-beyaz tabaka) + yola yayılan ışık
            float z = gz - 0.7f;
            float t = GateH - 0.05f;
            var fall = WaterFx.Sheet(new[]
            {
                new Vector3(0f, t, z + 0.1f), new Vector3(0f, t - 0.3f, z - 0.2f), new Vector3(0f, t * 0.72f, z - 0.36f),
                new Vector3(0f, t * 0.4f, z - 0.43f), new Vector3(0f, 0.9f, z - 0.47f), new Vector3(0f, 0.05f, z - 0.49f),
            }, Vector3.right, hw * 2f + 0.5f, 10, 0.3f, 0.95f, "StormVeil");
            var pool = WaterFx.Sheet(new[]
            {
                new Vector3(0f, 0.05f, z - 0.49f), new Vector3(0f, 0.045f, z - 1.3f), new Vector3(0f, 0.04f, z - 2.5f),
            }, Vector3.right, hw * 2f + 0.5f, 10, 0.95f, 0f, "VeilGlow");
            WaterFx.MakeWater("StormVeil", WaterFx.Combine("Sk_Curtain", fall, pool), env);
        }

        // ─────────────────────────────────────────────────────
        // UZAK PLAN: yüzen tapınak adaları, fırtına bulutu kuleleri
        // ─────────────────────────────────────────────────────
        static void BuildBackdrop(MeshBuilder b, float hw, float nearZ, float gz)
        {
            // Kapının arkasında dev fırtına bulutu kuleleri
            for (float x = -70f; x <= 70f; x += R(14f, 20f))
            {
                float z = gz + R(24f, 36f);
                float h = R(14f, 26f);
                var p = new Vector3(x, -6f, z);
                for (int i = 0; i < 4; i++)
                {
                    float s = R(7f, 11f) * (1f - i * 0.15f);
                    b.Sphere(p + new Vector3(R(-3f, 3f), h * i / 3.5f, R(-2f, 2f)), new Vector3(s, s * 0.6f, s * 0.7f), Vary(i < 2 ? CloudDark : CloudLight, 0.05f), 9, 5, default, true);
                }
            }
            // Yüzen adalar (uzakta, iki yanda ve kapının ardında)
            var spots = new List<Vector3>();
            for (int i = 0; i < 7; i++) spots.Add(new Vector3(R(-48f, -20f), R(-2f, 10f), R(nearZ + 8f, gz + 20f)));
            for (int i = 0; i < 5; i++) spots.Add(new Vector3(R(hw + 16f, 50f), R(0f, 12f), R(nearZ + 15f, gz + 22f)));
            for (int i = 0; i < 4; i++) spots.Add(new Vector3(R(-30f, 30f), R(6f, 16f), gz + R(12f, 22f)));
            foreach (var s in spots) Island(b, s, R(2.5f, 5f));
        }

        static void Island(MeshBuilder b, Vector3 p, float r)
        {
            b.Cylinder(p, p + new Vector3(R(-0.5f, 0.5f), -r * R(1.6f, 2.4f), R(-0.5f, 0.5f)), r, 0f, 7, Vary(Stone, 0.1f), true, true);
            b.Cylinder(p, p + Vector3.up * 0.3f, r * 1.02f, r * 0.98f, 7, Vary(MarbleB, 0.05f), true, true);
            // Üstünde minik tapınak: sütunlar + çatı
            int n = 2 + rnd.Next(3);
            float cw = r * 0.55f;
            for (int i = 0; i < n; i++)
            {
                float x = Mathf.Lerp(-cw, cw, n == 1 ? 0.5f : i / (float)(n - 1));
                var c = p + new Vector3(x, 0.3f, 0f);
                float h = Chance(0.3f) ? r * 0.35f : r * 0.75f;
                b.Cylinder(c, c + Vector3.up * h, r * 0.09f, r * 0.08f, 6, MarbleA, true, true);
            }
            if (Chance(0.6f))
            {
                b.Box(p + Vector3.up * (0.3f + r * 0.8f), new Vector3(cw * 2.4f, r * 0.12f, r * 0.5f), MarbleA);
                b.Box(p + Vector3.up * (0.3f + r * 1f), new Vector3(cw * 2.4f, r * 0.3f, r * 0.45f), MarbleB, default, 0.02f, 1f);
            }
        }

        static void BuildSky(Transform env, float z)
        {
            const float halfW = 140f, y0 = -40f, y1 = 42f;
            var m = new Mesh { name = "Sk_Sky" };
            m.SetVertices(new List<Vector3> { new Vector3(-halfW, y0, z), new Vector3(halfW, y0, z), new Vector3(halfW, y1, z), new Vector3(-halfW, y1, z) });
            m.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.RecalculateBounds();
            var go = new GameObject("Sky");
            go.transform.SetParent(env, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>();
            var db = GameDatabase.Load();
            var mat = db != null ? db.skySkyMaterial : null;
            if (mat == null)
            {
                mat = new Material(Shader.Find("StratejiGame/SeaBackdrop"));
                mat.SetColor("_Bottom", Fog);
                mat.SetColor("_Top", new Color(0.05f, 0.05f, 0.12f));
                mat.SetColor("_Shimmer", new Color(0.5f, 0.6f, 1f));
                mat.SetFloat("_ShimmerStrength", 0.08f);
            }
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static void BuildGlowLines(Transform env, float hw, float z0, float gz)
        {
            var b = new MeshBuilder();
            b.Box(new Vector3(0f, 0.03f, 0.9f), new Vector3(hw * 2f, 0.01f, 0.1f), new Color(1f, 0.75f, 0.3f));
            b.Box(new Vector3(0f, 0.025f, 0.9f), new Vector3(hw * 2f, 0.01f, 0.45f), new Color(0.25f, 0.16f, 0.05f));
            float lw = hw * 2f / GameManager.LaneCount;
            for (int i = 1; i < GameManager.LaneCount; i++)
            {
                float x = -hw + lw * i;
                b.Box(new Vector3(x, -0.044f, (z0 + gz) * 0.5f), new Vector3(0.1f, 0.01f, gz - z0), new Color(0.22f, 0.15f, 0.05f));
            }
            FxDesigns.MakeFx("GlowLines", b.ToMesh("Sk_GlowLines"), env);
        }
    }
}
