using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Çöl Sıcaklığı'nın kodla yapılan canlıları. Şimdilik: Çakram Ustası (Dune Dancer) — kullanıcının FBX'ine göre
    /// parçalı yeniden modellendi: sarıklı baş + yüz örtüsü, uçuşan uzun cüppe (arka etekler ayrı, dalgalanır),
    /// deri kuşak + sallanan kuşak uçları, bol şalvar, sargılı baldır, kıvrık burunlu çizme, iki elde altın çakram.
    /// Pivotlar Şimşek Süvarisi ile aynı (KnightWalk ile oynar): gövde kalçada (0, 0.47, 0), bacaklar kalça ekleminde,
    /// kollar omuzda (gövdeye göre ±0.19, 0.29). Adı "Flap" ile başlayan gövde çocukları kumaş gibi dalgalanır.
    /// Okunurluk: kum zeminde kaybolmasın diye bej yerine koyu toprak tonlu cüppe, bordo sarık/şal, koyu deri kuşak.
    /// </summary>
    public static class DesertCreatures
    {
        static Color G(Color c, float a = 0f) => new Color(c.r, c.g, c.b, a);

        static readonly Color Robe = new Color(0.5f, 0.37f, 0.23f);
        static readonly Color RobeDark = new Color(0.36f, 0.25f, 0.15f);
        static readonly Color RobeLight = new Color(0.66f, 0.52f, 0.34f);
        static readonly Color Pants = new Color(0.4f, 0.3f, 0.2f);
        static readonly Color Wrap = new Color(0.78f, 0.66f, 0.46f);
        static readonly Color Leather = new Color(0.22f, 0.13f, 0.08f);
        static readonly Color Scarf = new Color(0.55f, 0.14f, 0.11f);
        static readonly Color ScarfDark = new Color(0.36f, 0.08f, 0.07f);
        static readonly Color Skin = new Color(0.6f, 0.42f, 0.3f);
        static readonly Color Gold = new Color(0.98f, 0.76f, 0.3f);
        static readonly Color GoldDark = new Color(0.68f, 0.48f, 0.16f);

        public static readonly Vector3 Hip = new Vector3(0f, 0.47f, 0f);
        public static readonly Vector3 LegOffset = new Vector3(0.09f, -0.02f, 0f);
        public static readonly Vector3 Shoulder = new Vector3(0.17f, 0.29f, 0f);

        // ─────────────────────────────────────────────────────
        // GÖVDE (kalça pivotlu)
        // ─────────────────────────────────────────────────────
        public static Mesh DancerBody()
        {
            var b = new MeshBuilder();
            // Şalvarın üstü + deri kuşak (altın tokalı)
            b.Box(new Vector3(0f, -0.03f, 0f), new Vector3(0.3f, 0.12f, 0.21f), Pants, default, 1.05f, 1.05f);
            b.Cylinder(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.075f, 0f), 0.165f, 0.155f, 10, Leather, true, true);
            b.Cylinder(new Vector3(0f, 0.075f, 0f), new Vector3(0f, 0.1f, 0f), 0.15f, 0.15f, 10, Scarf, true, true);
            b.Box(new Vector3(0f, 0.04f, 0.16f), new Vector3(0.06f, 0.05f, 0.02f), G(Gold, 0.7f), new Vector3(0f, 0f, 45f));
            // Cüppe gövdesi (omuzlara genişler) + çapraz yaka + içteki koyu gömlek
            b.Box(new Vector3(0f, 0.2f, 0f), new Vector3(0.23f, 0.26f, 0.17f), Robe, default, 1.2f, 1.05f);
            b.Box(new Vector3(0f, 0.25f, 0.087f), new Vector3(0.05f, 0.12f, 0.01f), RobeDark, default, 1.8f, 1f);
            for (int s = -1; s <= 1; s += 2)
                b.Box(new Vector3(0.035f * s, 0.24f, 0.09f), new Vector3(0.025f, 0.16f, 0.015f), RobeLight, new Vector3(0f, 0f, 18f * s));
            // Cüppenin önden açık etek parçaları (kısa, sabit)
            for (int s = -1; s <= 1; s += 2)
                b.Box(new Vector3(0.1f * s, -0.12f, 0.07f), new Vector3(0.11f, 0.22f, 0.03f), Robe, new Vector3(-6f, 0f, 8f * s), 1f, 1f);
            // Omuzları saran bordo şal
            b.Sphere(new Vector3(0f, 0.32f, -0.01f), new Vector3(0.17f, 0.055f, 0.12f), Scarf, 10, 4, default, true);
            b.Sphere(new Vector3(0.1f, 0.3f, 0.07f), new Vector3(0.04f, 0.035f, 0.025f), ScarfDark, 6, 4, new Vector3(0f, 0f, 30f), true);
            // Boyun + baş (ten) + yüz örtüsü + göz yarığı
            b.Cylinder(new Vector3(0f, 0.31f, 0f), new Vector3(0f, 0.37f, 0f), 0.05f, 0.05f, 6, Scarf, true, true);
            b.Sphere(new Vector3(0f, 0.43f, 0f), new Vector3(0.078f, 0.09f, 0.082f), Skin, 9, 6, default, true);
            // Peçe: yalnız burun ve ağzı örter (gözler açıkta)
            b.Sphere(new Vector3(0f, 0.385f, 0.024f), new Vector3(0.08f, 0.042f, 0.072f), Scarf, 9, 5, default, true);
            b.Box(new Vector3(0f, 0.415f, 0.077f), new Vector3(0.11f, 0.012f, 0.02f), ScarfDark);
            for (int s = -1; s <= 1; s += 2)
                b.Box(new Vector3(0.03f * s, 0.445f, 0.078f), new Vector3(0.024f, 0.012f, 0.01f), G(new Color(1f, 0.75f, 0.3f), 0.2f));
            // Sarık: yuvarlak, başı saran iki şişkin katman + çapraz bant + yanda düğüm
            b.Sphere(new Vector3(0f, 0.49f, -0.005f), new Vector3(0.1f, 0.06f, 0.1f), Scarf, 10, 5, default, true);
            b.Sphere(new Vector3(0f, 0.525f, -0.012f), new Vector3(0.085f, 0.05f, 0.085f), ScarfDark, 9, 5, default, true);
            b.Box(new Vector3(0f, 0.5f, 0.06f), new Vector3(0.03f, 0.1f, 0.03f), Scarf, new Vector3(-20f, 0f, 35f));
            b.Box(new Vector3(0f, 0.51f, 0.055f), new Vector3(0.03f, 0.1f, 0.03f), ScarfDark, new Vector3(-20f, 0f, -35f));
            b.Sphere(new Vector3(0.085f, 0.5f, -0.03f), new Vector3(0.03f, 0.035f, 0.03f), Scarf, 6, 4, default, true);
            return b.ToMesh("Dancer_Body");
        }

        /// <summary>Arkadan sarkan uzun cüppe eteği (pivot belde, aşağı sarkar). side: -1 sol, 1 sağ.</summary>
        public static Mesh DancerRobeTail(int side)
        {
            var b = new MeshBuilder();
            // Belden diz altına inen, aşağı doğru incelip sivrilen ince kumaş; iki katman (dış koyu, iç açık kenarlı)
            b.Box(new Vector3(0.01f * side, -0.2f, 0f), new Vector3(0.12f, 0.4f, 0.018f), Robe, new Vector3(0f, 0f, 180f + 3f * side), 0.45f, 1f);
            b.Box(new Vector3(0.04f * side, -0.44f, -0.004f), new Vector3(0.06f, 0.12f, 0.016f), RobeDark, new Vector3(0f, 0f, 180f + 12f * side), 0.1f, 1f);
            b.Box(new Vector3(0.012f * side, -0.18f, 0.009f), new Vector3(0.11f, 0.015f, 0.01f), RobeLight);
            return b.ToMesh(side < 0 ? "Dancer_TailL" : "Dancer_TailR");
        }

        /// <summary>Kuşaktan sarkan dalgalı iki şerit (pivot kuşakta).</summary>
        public static Mesh DancerSashTail()
        {
            var b = new MeshBuilder();
            var p = Vector3.zero;
            for (int i = 0; i < 5; i++)
            {
                var n = p + new Vector3((i % 2 == 0 ? 0.035f : -0.025f), -0.07f, -0.015f);
                b.Box((p + n) * 0.5f, new Vector3(0.055f, 0.08f, 0.012f), i % 2 == 0 ? Scarf : ScarfDark, new Vector3(0f, 0f, i % 2 == 0 ? -25f : 20f));
                p = n;
            }
            p = new Vector3(0.04f, -0.01f, 0.005f);
            for (int i = 0; i < 4; i++)
            {
                var n = p + new Vector3((i % 2 == 0 ? -0.03f : 0.03f), -0.065f, -0.01f);
                b.Box((p + n) * 0.5f, new Vector3(0.045f, 0.075f, 0.012f), ScarfDark, new Vector3(0f, 0f, i % 2 == 0 ? 22f : -22f));
                p = n;
            }
            return b.ToMesh("Dancer_Sash");
        }

        /// <summary>Sarığın arkasından omuza sarkan şal ucu (pivot başın arkasında).</summary>
        public static Mesh DancerScarfTail()
        {
            var b = new MeshBuilder();
            b.Box(new Vector3(0f, -0.07f, -0.01f), new Vector3(0.07f, 0.16f, 0.015f), Scarf, new Vector3(12f, 0f, 0f), 0.8f, 1f);
            b.Box(new Vector3(0.02f, -0.17f, -0.03f), new Vector3(0.06f, 0.08f, 0.014f), ScarfDark, new Vector3(20f, 0f, -10f), 0.6f, 1f);
            return b.ToMesh("Dancer_Scarf");
        }

        // ─────────────────────────────────────────────────────
        // BACAK (kalça ekleminde pivot): bol şalvar, sargılı baldır, kıvrık burunlu çizme
        // ─────────────────────────────────────────────────────
        public static Mesh DancerLeg()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, -0.1f, 0f), new Vector3(0.085f, 0.13f, 0.09f), Pants, 8, 6, default, true);
            b.Sphere(new Vector3(0f, -0.22f, 0.01f), new Vector3(0.07f, 0.05f, 0.075f), Pants, 7, 4, default, true);
            b.Cylinder(new Vector3(0f, -0.24f, 0f), new Vector3(0f, -0.4f, 0f), 0.048f, 0.04f, 7, Wrap, true, true);
            for (int i = 0; i < 4; i++)
                b.Cylinder(new Vector3(0f, -0.26f - i * 0.035f, 0f), new Vector3(0f, -0.272f - i * 0.035f, 0.004f), 0.051f - i * 0.002f, 0.05f - i * 0.002f, 7, RobeLight, false, true);
            b.Box(new Vector3(0f, -0.425f, 0.03f), new Vector3(0.085f, 0.06f, 0.16f), Leather, default, 0.85f, 0.85f);
            b.Cylinder(new Vector3(0f, -0.43f, 0.1f), new Vector3(0f, -0.405f, 0.15f), 0.025f, 0.008f, 5, Leather, true, true);   // kıvrık burun
            return b.ToMesh("Dancer_Leg");
        }

        // ─────────────────────────────────────────────────────
        // KOL (omuzda pivot) + elde altın çakram
        // ─────────────────────────────────────────────────────
        public static Mesh DancerArm(int side)
        {
            var b = new MeshBuilder();
            var elbow = new Vector3(0.03f * side, -0.15f, 0.01f);
            var hand = new Vector3(0.04f * side, -0.24f, 0.08f);
            b.Cylinder(Vector3.zero, elbow, 0.055f, 0.06f, 7, Robe, true, true);                   // bol cüppe kolu
            b.Cylinder(elbow + new Vector3(0f, 0.02f, 0f), elbow - new Vector3(0f, 0.01f, 0f), 0.064f, 0.062f, 7, RobeDark, true, true);
            b.Cylinder(elbow, hand, 0.04f, 0.033f, 6, Wrap, true, true);                            // sargılı ön kol
            b.Sphere(hand, Vector3.one * 0.034f, Skin, 6, 4, default, true);
            // Çakram: elde dikey tutulur (halka düzlemi gövdeye paralel), hafif dışa açık
            var c = hand + new Vector3(0.05f * side, -0.02f, 0.05f);
            AddChakram(b, c, Quaternion.Euler(0f, 90f + 20f * side, 0f), 0.12f);
            return b.ToMesh(side < 0 ? "Dancer_ArmL" : "Dancer_ArmR");
        }

        /// <summary>Altın çakram: kalın halka + dış kenarda keskin dişler + iç halka. Yerel düzlem: XY, eksen Z.</summary>
        public static void AddChakram(MeshBuilder b, Vector3 c, Quaternion rot, float r)
        {
            const int seg = 14;
            var gold = G(Gold, 0.65f);
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                var p0 = c + rot * new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * r;
                var p1 = c + rot * new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * r;
                b.Cylinder(p0, p1, r * 0.13f, r * 0.13f, 5, i % 2 == 0 ? gold : G(GoldDark, 0.8f), true, true);
                if (i % 2 == 0)
                {
                    // Dış bıçak dişi (dönme yönünde eğik)
                    float am = (a0 + a1) * 0.5f;
                    var root = c + rot * new Vector3(Mathf.Cos(am), Mathf.Sin(am), 0f) * r * 1.05f;
                    var tip = c + rot * new Vector3(Mathf.Cos(am + 0.25f), Mathf.Sin(am + 0.25f), 0f) * r * 1.35f;
                    b.Cylinder(root, tip, r * 0.09f, 0f, 4, G(new Color(1f, 0.9f, 0.6f), 0.5f), true, true);
                }
            }
            // Ortadaki deri kavrama
            b.Cylinder(c + rot * new Vector3(-r * 0.85f, 0f, 0f), c + rot * new Vector3(-r * 0.55f, 0f, 0f), r * 0.11f, r * 0.11f, 5, Leather, true, true);
        }

        // ─────────────────────────────────────────────────────
        // AKREP YAVRUSU (sürü minyonu; uzunluk ~1.1, boy ~0.6, yüzü +Z)
        // Kum yolda seçilsin diye koyu kızıl-kahve kabuk, parlayan turuncu gözler, mor zehirli iğne
        // ─────────────────────────────────────────────────────
        public static Mesh ScorpionPup()
        {
            var b = new MeshBuilder();
            var shell = new Color(0.42f, 0.13f, 0.07f);
            var shellDark = new Color(0.22f, 0.06f, 0.04f);
            var shellLight = new Color(0.6f, 0.22f, 0.1f);
            var eye = G(new Color(1f, 0.6f, 0.15f));
            var venom = G(new Color(0.8f, 0.3f, 1f), 0.3f);
            // Gövde segmentleri
            b.Sphere(new Vector3(0f, 0.2f, 0.12f), new Vector3(0.2f, 0.1f, 0.18f), shell, 9, 5, default, true);
            b.Sphere(new Vector3(0f, 0.2f, -0.08f), new Vector3(0.18f, 0.09f, 0.14f), shellLight, 9, 5, default, true);
            b.Sphere(new Vector3(0f, 0.19f, -0.22f), new Vector3(0.14f, 0.08f, 0.1f), shell, 8, 5, default, true);
            for (int i = 0; i < 3; i++) b.Box(new Vector3(0f, 0.29f, 0.14f - i * 0.12f), new Vector3(0.22f - i * 0.03f, 0.015f, 0.03f), shellDark);
            // Gözler
            for (int s = -1; s <= 1; s += 2) b.Sphere(new Vector3(0.05f * s, 0.27f, 0.27f), Vector3.one * 0.028f, eye, 5, 3);
            // Kıskaçlar
            for (int s = -1; s <= 1; s += 2)
            {
                var sh = new Vector3(0.14f * s, 0.2f, 0.22f);
                var el = new Vector3(0.26f * s, 0.18f, 0.36f);
                var cl = new Vector3(0.24f * s, 0.18f, 0.5f);
                b.Cylinder(sh, el, 0.035f, 0.03f, 5, shell, true, true);
                b.Cylinder(el, cl, 0.03f, 0.04f, 5, shell, true, true);
                b.Sphere(cl + new Vector3(0f, 0f, 0.04f), new Vector3(0.06f, 0.045f, 0.08f), shellLight, 7, 4, default, true);
                b.Cylinder(cl + new Vector3(0.02f * s, 0f, 0.09f), cl + new Vector3(-0.02f * s, 0f, 0.17f), 0.025f, 0f, 4, shellDark, true, true);
                b.Cylinder(cl + new Vector3(-0.03f * s, 0f, 0.08f), cl + new Vector3(-0.05f * s, 0f, 0.14f), 0.02f, 0f, 4, shellDark, true, true);
                // 4 çift bacak
                for (int k = 0; k < 4; k++)
                {
                    float z = 0.12f - k * 0.11f;
                    var hip = new Vector3(0.15f * s, 0.18f, z);
                    var knee = new Vector3(0.27f * s, 0.24f, z - 0.02f);
                    var foot = new Vector3(0.33f * s, 0.01f, z - 0.05f);
                    b.Cylinder(hip, knee, 0.02f, 0.018f, 4, shellDark, true, true);
                    b.Cylinder(knee, foot, 0.018f, 0.008f, 4, shellDark, true, true);
                }
            }
            // Yukarı kıvrık kuyruk + parlayan mor iğne
            var p = new Vector3(0f, 0.2f, -0.3f);
            var pts = new[] { p, p + new Vector3(0f, 0.08f, -0.12f), p + new Vector3(0f, 0.24f, -0.16f), p + new Vector3(0f, 0.38f, -0.1f), p + new Vector3(0f, 0.44f, 0.02f) };
            for (int i = 0; i < pts.Length - 1; i++)
            {
                b.Sphere(pts[i], Vector3.one * (0.065f - i * 0.008f), i % 2 == 0 ? shell : shellLight, 6, 4, default, true);
                b.Cylinder(pts[i], pts[i + 1], 0.05f - i * 0.007f, 0.045f - i * 0.007f, 6, shell, false, true);
            }
            b.Sphere(pts[4], new Vector3(0.05f, 0.05f, 0.06f), shellDark, 6, 4, default, true);
            b.Cylinder(pts[4], pts[4] + new Vector3(0f, -0.06f, 0.1f), 0.03f, 0f, 5, venom, true, true);
            return b.ToMesh("ScorpionPup_Mesh");
        }

        // ─────────────────────────────────────────────────────
        // KUM GOLEMİ (iri düşman; boy ~1, yüzü +Z): kızıl kumtaşı bloklar, parlayan turkuaz hiyeroglifler ve gözler
        // ─────────────────────────────────────────────────────
        public static Mesh SandGolem()
        {
            var b = new MeshBuilder();
            var rockA = new Color(0.62f, 0.36f, 0.22f);
            var rockB = new Color(0.5f, 0.28f, 0.17f);
            var rockD = new Color(0.3f, 0.16f, 0.1f);
            var sand = new Color(0.86f, 0.7f, 0.45f);
            var glyph = G(new Color(0.25f, 0.95f, 0.9f));
            // Bacaklar (kalın taş sütunlar)
            for (int s = -1; s <= 1; s += 2)
            {
                b.Box(new Vector3(0.16f * s, 0.14f, 0f), new Vector3(0.2f, 0.28f, 0.22f), rockB, new Vector3(0f, 6f * s, 0f), 0.9f, 0.9f);
                b.Box(new Vector3(0.16f * s, 0.03f, 0.03f), new Vector3(0.24f, 0.07f, 0.28f), rockD);
            }
            // Gövde: büyük blok + göğüste hiyeroglif paneli + kum akan çatlaklar
            b.Box(new Vector3(0f, 0.45f, 0f), new Vector3(0.52f, 0.36f, 0.34f), rockA, new Vector3(0f, 0f, 2f), 1.15f, 1.05f);
            b.Box(new Vector3(0f, 0.68f, -0.01f), new Vector3(0.62f, 0.14f, 0.36f), rockB, new Vector3(0f, 0f, -3f));
            b.Box(new Vector3(0f, 0.47f, 0.175f), new Vector3(0.26f, 0.24f, 0.02f), rockD);
            b.Box(new Vector3(0f, 0.51f, 0.188f), new Vector3(0.03f, 0.14f, 0.01f), glyph);                  // ankh gövdesi
            b.Box(new Vector3(0f, 0.6f, 0.188f), new Vector3(0.08f, 0.06f, 0.01f), glyph);
            b.Box(new Vector3(0f, 0.47f, 0.188f), new Vector3(0.12f, 0.025f, 0.01f), glyph);
            for (int i = -1; i <= 1; i += 2) b.Box(new Vector3(0.09f * i, 0.4f, 0.188f), new Vector3(0.02f, 0.1f, 0.01f), glyph, new Vector3(0f, 0f, 20f * i));
            b.Box(new Vector3(0.2f, 0.42f, 0.172f), new Vector3(0.02f, 0.2f, 0.01f), sand, new Vector3(0f, 0f, 25f));
            // Baş: küçük blok, parlayan turkuaz gözler, nemes benzeri omuza inen taş başlık
            b.Box(new Vector3(0f, 0.86f, 0.03f), new Vector3(0.22f, 0.2f, 0.2f), rockA, default, 0.9f, 0.9f);
            b.Box(new Vector3(0f, 0.86f, -0.02f), new Vector3(0.3f, 0.24f, 0.18f), rockB, default, 0.7f, 1f);
            for (int s = -1; s <= 1; s += 2) b.Box(new Vector3(0.05f * s, 0.87f, 0.135f), new Vector3(0.05f, 0.025f, 0.01f), glyph);
            // Kollar: omuz bloğu + iri yumruk (kayalar)
            for (int s = -1; s <= 1; s += 2)
            {
                b.Box(new Vector3(0.38f * s, 0.66f, 0f), new Vector3(0.2f, 0.18f, 0.22f), rockB, new Vector3(0f, 0f, -15f * s));
                b.Box(new Vector3(0.44f * s, 0.45f, 0.04f), new Vector3(0.15f, 0.26f, 0.16f), rockA, new Vector3(10f, 0f, 8f * s), 1.1f, 1.1f);
                b.Box(new Vector3(0.46f * s, 0.24f, 0.08f), new Vector3(0.22f, 0.2f, 0.22f), rockD, new Vector3(0f, 15f * s, 0f));
                b.Box(new Vector3(0.44f * s, 0.55f, 0.125f), new Vector3(0.02f, 0.12f, 0.01f), glyph);
            }
            // Omuzlarda biriken kum
            for (int s = -1; s <= 1; s += 2) b.Sphere(new Vector3(0.36f * s, 0.76f, 0f), new Vector3(0.1f, 0.04f, 0.1f), sand, 6, 3, default, true);
            return b.ToMesh("SandGolem_Mesh");
        }

        // ─────────────────────────────────────────────────────
        // KUM FIRTINASI DUVARI (boss saldırısı): şerit genişliğinde dönen kum bulutu yığını, genişlik ~1, boy ~1
        // ─────────────────────────────────────────────────────
        public static Mesh SandWallMesh()
        {
            // Yükselen kum dalgası: altta koyu, geniş gövde; üstte öne (−Z) kıvrılan açık renkli tepe. Genişlik ~1, boy ~1.
            var b = new MeshBuilder();
            var rnd = new System.Random(4);
            var dark = new Color(0.42f, 0.28f, 0.16f);
            var mid = new Color(0.58f, 0.41f, 0.24f);
            var light = new Color(0.78f, 0.6f, 0.37f);
            for (int row = 0; row < 5; row++)
            {
                float y = 0.08f + row * 0.2f;
                float lean = -row * row * 0.035f;                 // tepeye doğru öne kıvrılır
                int n = 6 - row / 2;
                for (int i = 0; i < n; i++)
                {
                    float x = (i + 0.5f) / n - 0.5f + ((float)rnd.NextDouble() - 0.5f) * 0.08f;
                    float r = 0.2f - row * 0.015f + (float)rnd.NextDouble() * 0.06f;
                    var c = row < 2 ? dark : row < 4 ? mid : light;
                    b.Sphere(new Vector3(x * 1.05f, y, lean + ((float)rnd.NextDouble() - 0.5f) * 0.15f), new Vector3(r * 1.1f, r * 0.85f, r), c, 7, 4, new Vector3(0f, (float)rnd.NextDouble() * 90f, 0f), true);
                }
            }
            // Zeminde koyu kum yığını (duvarın tabanı)
            b.Sphere(new Vector3(0f, 0.02f, 0.1f), new Vector3(0.62f, 0.08f, 0.45f), dark, 9, 3, default, true);
            return b.ToMesh("Fx_SandWall");
        }

        /// <summary>Duvarın önünde yuvarlanan kum/toz silindiri (X ekseni etrafında döner). Yarıçap ~0.5.</summary>
        public static Mesh SandRollMesh()
        {
            var b = new MeshBuilder();
            var rnd = new System.Random(6);
            var a = new Color(0.66f, 0.48f, 0.29f);
            var d = new Color(0.5f, 0.34f, 0.2f);
            for (int i = 0; i < 16; i++)
            {
                float ang = i / 16f * Mathf.PI * 2f;
                float x = ((float)rnd.NextDouble() - 0.5f) * 1f;
                var p = new Vector3(x, Mathf.Sin(ang) * 0.42f, Mathf.Cos(ang) * 0.42f);
                float r = 0.13f + (float)rnd.NextDouble() * 0.08f;
                b.Sphere(p, Vector3.one * r, i % 3 == 0 ? d : a, 6, 4, default, true);
            }
            return b.ToMesh("Fx_SandRoll");
        }

        static Mesh chakram;
        /// <summary>Fırlatılan çakram mermisi (yatay, eksen Y; ileride Çöl dünyasında kullanılacak).</summary>
        public static Mesh Chakram
        {
            get
            {
                if (chakram != null) return chakram;
                var b = new MeshBuilder();
                AddChakram(b, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), 0.2f);
                chakram = b.ToMesh("Chakram");
                return chakram;
            }
        }
    }
}

namespace StratejiGame
{
    /// <summary>Akrep yavrusu: hızlı, tıkır tıkır ilerleme — küçük sıçrama + iki yana yalpalama.</summary>
    public class ScuttleMotion : MonoBehaviour
    {
        Vector3 basePos;
        Quaternion baseRot;
        float phase;

        void Start()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            phase = Random.value * 6.28f;
        }

        void Update()
        {
            float t = Time.time * 16f + phase;
            transform.localPosition = basePos + Vector3.up * Mathf.Abs(Mathf.Sin(t)) * 0.03f;
            transform.localRotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(t * 0.25f) * 10f, Mathf.Sin(t) * 4f);
        }
    }

    /// <summary>
    /// Kum fırtınası dalgası (Kumların Hakimi): boss'un önünden yükselip şerit boyunca oyuncuya akan, öne kıvrılmış
    /// koyu kum duvarı + önünde yuvarlanan toz silindiri; arkasında kalın toz izi bırakır, sonunda çöker. Havuzlu (Effects içinde).
    /// </summary>
    public class SandWall : MonoBehaviour
    {
        public const float Duration = 1f;
        float t = -1f, width, startZ, endZ, puffT;
        Transform wall, roll;

        public static SandWall Create(Transform parent, Mesh wallMesh, Mesh rollMesh, Material mat)
        {
            var go = new GameObject("SandWall");
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<SandWall>();
            w.wall = Part("Wall", wallMesh, go.transform, mat);
            w.roll = Part("Roll", rollMesh, go.transform, mat);
            go.SetActive(false);
            return w;
        }

        static Transform Part(string name, Mesh mesh, Transform parent, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        public void Play(float x, float w, float fromZ, float toZ)
        {
            width = w; startZ = fromZ; endZ = toZ; t = 0f;
            transform.position = new Vector3(x, 0f, fromZ);
            gameObject.SetActive(true);
            Update();
        }

        void Update()
        {
            if (t < 0f) return;
            t += Time.deltaTime;
            float k = t / Duration;
            if (k >= 1.2f) { t = -1f; gameObject.SetActive(false); return; }
            float z = Mathf.Lerp(startZ, endZ, Mathf.Clamp01(k));
            var p = transform.position;
            p.z = z;
            transform.position = p;
            // Yükselme (ilk %20), tam boy, sonda çökme
            float h = k < 0.2f ? Mathf.SmoothStep(0f, 1f, k / 0.2f) : k > 1f ? Mathf.Max(0f, 1f - (k - 1f) / 0.2f) : 1f;
            float boil = 1f + Mathf.Sin(t * 26f) * 0.05f;
            wall.localScale = new Vector3(width, 3.6f * h * boil + 0.01f, 2.2f);
            wall.localRotation = Quaternion.Euler(Mathf.Sin(t * 17f) * 4f, 0f, Mathf.Sin(t * 21f) * 3f);
            roll.localScale = new Vector3(width, 1.3f * h + 0.01f, 1.3f * h + 0.01f);
            roll.localPosition = new Vector3(0f, 0.6f * h, -0.9f);
            roll.localRotation = Quaternion.Euler(-t * 720f, 0f, 0f);   // ileri doğru yuvarlanır
            // Arkasında kalın toz izi, tepesinden savrulan kum
            puffT -= Time.deltaTime;
            if (puffT <= 0f || Time.deltaTime <= 0f)
            {
                puffT = 0.025f;
                for (int i = 0; i < 2; i++)
                {
                    Effects.SandPuff(new Vector3(p.x + Random.Range(-width * 0.5f, width * 0.5f), Random.Range(0.1f, 1.2f), z + Random.Range(0.6f, 2f)), 2.2f);
                    Effects.SandPuff(new Vector3(p.x + Random.Range(-width * 0.45f, width * 0.45f), 3.4f * h + Random.Range(-0.3f, 0.4f), z - Random.Range(0f, 0.4f)), 1.4f);
                }
            }
        }
    }
}
