using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Gökyüzü Tapınağı'nın kodla yapılan canlıları ve efekt mesh'leri:
    /// Bulut Ruhu (sürü minyonu, süzülür), Altın Griffon (iri düşman, kanat çırpar),
    /// Yıldırım (Şimşek Süvarisi mermisi), Fırtına Bulutu (Fırtına Lordu'nun üstünde durduğu bulut).
    /// Okunurluk: açık mermer yolda koyu fırtına grisi bulut ruhları (parlayan mavi gözler, sarı şimşek ağız);
    /// griffonda koyu bronz bacak/kanat altları ve parlak altın gövde.
    /// </summary>
    public static class SkyCreatures
    {
        static Color G(Color c) => new Color(c.r, c.g, c.b, 0f);
        static Color Half(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // ─────────────────────────────────────────────────────
        // BULUT RUHU (boy ~1, yüzü +Z)
        // ─────────────────────────────────────────────────────
        public static Mesh CloudSpirit()
        {
            var b = new MeshBuilder();
            var dark = new Color(0.27f, 0.27f, 0.4f);
            var mid = new Color(0.36f, 0.37f, 0.52f);
            var light = new Color(0.5f, 0.52f, 0.68f);
            var eye = G(new Color(0.55f, 0.95f, 1f));
            var bolt = G(new Color(1f, 0.92f, 0.45f));

            // Kabarık bulut gövde (alt koyu, üst açık)
            b.Sphere(new Vector3(0f, 0.6f, 0f), new Vector3(0.36f, 0.3f, 0.32f), mid, 9, 6, default, true);
            b.Sphere(new Vector3(0f, 0.8f, -0.04f), new Vector3(0.26f, 0.2f, 0.24f), light, 8, 5, default, true);
            b.Sphere(new Vector3(-0.27f, 0.62f, -0.02f), new Vector3(0.2f, 0.18f, 0.2f), mid, 7, 5, default, true);
            b.Sphere(new Vector3(0.27f, 0.62f, -0.02f), new Vector3(0.2f, 0.18f, 0.2f), mid, 7, 5, default, true);
            b.Sphere(new Vector3(-0.15f, 0.76f, 0.08f), new Vector3(0.15f, 0.13f, 0.14f), light, 7, 4, default, true);
            b.Sphere(new Vector3(0.17f, 0.75f, 0.07f), new Vector3(0.14f, 0.12f, 0.13f), light, 7, 4, default, true);
            b.Sphere(new Vector3(0f, 0.46f, 0.02f), new Vector3(0.3f, 0.14f, 0.26f), dark, 8, 4, default, true);
            // Aşağı doğru incelen buğu kuyruğu (yere değmeden süzülür)
            b.Cylinder(new Vector3(0f, 0.44f, -0.02f), new Vector3(0.04f, 0.08f, -0.12f), 0.2f, 0f, 7, dark, true, true);
            b.Cylinder(new Vector3(-0.12f, 0.42f, 0f), new Vector3(-0.2f, 0.18f, -0.08f), 0.1f, 0f, 5, dark, true, true);
            b.Cylinder(new Vector3(0.12f, 0.42f, 0f), new Vector3(0.22f, 0.2f, -0.06f), 0.09f, 0f, 5, dark, true, true);
            // Parlayan gözler (çatık) + zikzak şimşek ağız
            for (int s = -1; s <= 1; s += 2)
            {
                b.Box(new Vector3(0.1f * s, 0.67f, 0.31f), new Vector3(0.09f, 0.05f, 0.03f), eye, new Vector3(0f, 0f, -18f * s));
                // Yanlardan çıkan küçük şimşek "kollar"
                var a0 = new Vector3(0.42f * s, 0.6f, 0f);
                var a1 = a0 + new Vector3(0.1f * s, 0.06f, 0.02f);
                var a2 = a1 + new Vector3(0.06f * s, -0.1f, 0.02f);
                b.Cylinder(a0, a1, 0.025f, 0.02f, 4, bolt, true, true);
                b.Cylinder(a1, a2, 0.02f, 0f, 4, bolt, true, true);
            }
            var m0 = new Vector3(-0.1f, 0.54f, 0.29f);
            for (int k = 0; k < 4; k++)
            {
                var m1 = m0 + new Vector3(0.05f, k % 2 == 0 ? -0.04f : 0.04f, 0f);
                b.Cylinder(m0, m1, 0.017f, 0.017f, 4, bolt, true, true);
                m0 = m1;
            }
            return b.ToMesh("CloudSpirit_Mesh");
        }

        // ─────────────────────────────────────────────────────
        // ALTIN GRIFFON (boy ~1, uzunluk ~1.4, yüzü +Z) — gövde + ayrı kanatlar (menteşe orijinde)
        // ─────────────────────────────────────────────────────
        static readonly Color Gold = new Color(0.86f, 0.6f, 0.2f);
        static readonly Color GoldLight = new Color(1f, 0.8f, 0.38f);
        static readonly Color Bronze = new Color(0.46f, 0.3f, 0.12f);
        static readonly Color Feather = new Color(0.95f, 0.92f, 0.82f);

        public static Mesh GriffonBody()
        {
            var b = new MeshBuilder();
            var beak = new Color(1f, 0.68f, 0.12f);
            var talon = new Color(0.16f, 0.12f, 0.1f);
            var eye = G(new Color(1f, 0.95f, 0.55f));

            // Aslan gövdesi
            b.Sphere(new Vector3(0f, 0.5f, -0.1f), new Vector3(0.27f, 0.24f, 0.5f), Gold, 10, 6, default, true);
            b.Sphere(new Vector3(0f, 0.4f, -0.1f), new Vector3(0.22f, 0.14f, 0.42f), Bronze, 9, 4, default, true);   // koyu karın
            // Tüylü göğüs + boyun
            b.Sphere(new Vector3(0f, 0.62f, 0.3f), new Vector3(0.25f, 0.26f, 0.22f), Feather, 9, 6, default, true);
            b.Cylinder(new Vector3(0f, 0.66f, 0.32f), new Vector3(0f, 0.88f, 0.44f), 0.17f, 0.13f, 8, Feather, false, true);
            // Kartal başı + gaga + parlayan gözler + kulak tüyleri
            b.Sphere(new Vector3(0f, 0.94f, 0.48f), new Vector3(0.15f, 0.14f, 0.16f), Feather, 9, 6, default, true);
            b.Cylinder(new Vector3(0f, 0.93f, 0.58f), new Vector3(0f, 0.86f, 0.78f), 0.07f, 0.01f, 6, beak, true, true);
            b.Box(new Vector3(0f, 0.9f, 0.62f), new Vector3(0.1f, 0.05f, 0.1f), beak, new Vector3(20f, 0f, 0f));
            for (int s = -1; s <= 1; s += 2)
            {
                b.Box(new Vector3(0.085f * s, 0.97f, 0.6f), new Vector3(0.05f, 0.035f, 0.03f), eye, new Vector3(0f, 25f * s, -15f * s));
                b.Box(new Vector3(0.09f * s, 1.06f, 0.4f), new Vector3(0.04f, 0.14f, 0.05f), GoldLight, new Vector3(-35f, 0f, 15f * s), 0.2f, 0.4f);
                // Ön bacaklar: kartal pençesi (koyu)
                var fTop = new Vector3(0.13f * s, 0.46f, 0.26f);
                var fFoot = new Vector3(0.15f * s, 0.05f, 0.34f);
                b.Cylinder(fTop, fFoot, 0.065f, 0.045f, 6, Bronze, true, true);
                for (int k = -1; k <= 1; k++)
                    b.Cylinder(fFoot, fFoot + new Vector3(k * 0.04f, -0.03f, 0.09f), 0.022f, 0f, 4, talon, true, true);
                // Arka bacaklar: aslan (kalın üst + ince alt)
                var hip = new Vector3(0.16f * s, 0.48f, -0.42f);
                var knee = new Vector3(0.17f * s, 0.26f, -0.32f);
                var paw = new Vector3(0.16f * s, 0.05f, -0.42f);
                b.Sphere(hip, new Vector3(0.11f, 0.16f, 0.15f), Gold, 7, 5, default, true);
                b.Cylinder(hip, knee, 0.08f, 0.055f, 6, Gold, true, true);
                b.Cylinder(knee, paw, 0.055f, 0.045f, 6, Bronze, true, true);
                b.Sphere(paw, new Vector3(0.06f, 0.035f, 0.08f), Bronze, 5, 3, default, true);
            }
            // Aslan kuyruğu (ucu koyu püskül)
            b.Cylinder(new Vector3(0f, 0.55f, -0.58f), new Vector3(0f, 0.42f, -0.85f), 0.035f, 0.025f, 5, Gold, true, true);
            b.Cylinder(new Vector3(0f, 0.42f, -0.85f), new Vector3(0f, 0.55f, -1f), 0.025f, 0.02f, 5, Gold, true, true);
            b.Sphere(new Vector3(0f, 0.57f, -1.02f), new Vector3(0.05f, 0.07f, 0.05f), Bronze, 5, 4, default, true);
            return b.ToMesh("Griffon_Body");
        }

        /// <summary>Tek kanat (sağ: side=1, sol: side=-1). Menteşe orijinde; kanat yukarı-dışa açılır, tüyler geriye-aşağı sarkar.</summary>
        public static Mesh GriffonWing(int side)
        {
            var b = new MeshBuilder();
            var root = Vector3.zero;
            var tip = new Vector3(0.6f * side, 0.42f, -0.22f);
            var arm = tip - root;
            var armDir = arm.normalized;
            var hang = new Vector3(0.1f * side, -0.55f, -1f).normalized;          // tüylerin sarktığı yön
            var normal = Vector3.Cross(armDir, hang).normalized;
            // Kanat kemiği (kalın altın kenar)
            b.Cylinder(root, tip, 0.065f, 0.03f, 6, GoldLight, true, true);
            // Örtü tüyleri: kemik boyunca yassı, dolu bir yüzey (altta koyu bronz)
            for (int i = 0; i < 4; i++)
            {
                float k = (i + 0.5f) / 4f;
                var top = Vector3.Lerp(root, tip, k);
                float len = Mathf.Lerp(0.22f, 0.16f, k);
                b.Box(top + hang * len * 0.5f, new Vector3(arm.magnitude / 4f + 0.05f, 0.035f, len), Gold, Quaternion.LookRotation(hang, normal).eulerAngles, 1f, 1f);
            }
            // Uçuş tüyleri: üst üste binen, uzun, uçları krem
            for (int i = 0; i < 7; i++)
            {
                float k = i / 6f;
                var top = Vector3.Lerp(root + armDir * 0.08f, tip, k) + hang * 0.12f;
                var d = Vector3.Slerp(hang, armDir, k * 0.45f).normalized;
                float len = Mathf.Lerp(0.34f, 0.5f, Mathf.Sin(k * Mathf.PI * 0.85f + 0.15f));
                var rot = Quaternion.LookRotation(d, normal).eulerAngles;
                b.Box(top + d * len * 0.5f, new Vector3(0.17f, 0.025f, len), i % 2 == 0 ? Gold : GoldLight, rot, 0.75f, 1f);
                b.Box(top + d * (len + 0.03f), new Vector3(0.08f, 0.024f, 0.08f), Feather, rot, 0.5f, 1f);
            }
            // Alt yüz: koyu bronz (yukarıdan bakınca kanadın kalınlığı okunur)
            b.Box(Vector3.Lerp(root, tip, 0.55f) + hang * 0.26f - normal * 0.02f, new Vector3(arm.magnitude * 0.95f, 0.02f, 0.5f), new Color(0.62f, 0.42f, 0.15f), Quaternion.LookRotation(hang, normal).eulerAngles);
            return b.ToMesh(side > 0 ? "Griffon_WingR" : "Griffon_WingL");
        }

        // ─────────────────────────────────────────────────────
        // ŞİMŞEK SÜVARİSİ (kodla, parçalı: gövde + 2 bacak + 2 kol; boy ~1, yüzü +Z)
        // Gövde pivotu kalçada (0, 0.47, 0); bacak pivotları kalça ekleminde; kol pivotları omuzda.
        // ─────────────────────────────────────────────────────
        static readonly Color LNavy = new Color(0.13f, 0.17f, 0.42f);
        static readonly Color LNavyDark = new Color(0.08f, 0.1f, 0.25f);
        static readonly Color LGold = new Color(0.95f, 0.72f, 0.26f);
        static readonly Color LGoldDark = new Color(0.62f, 0.42f, 0.14f);
        static readonly Color LGoldLight = new Color(1f, 0.86f, 0.45f);
        static readonly Color LWhite = new Color(0.93f, 0.94f, 1f);
        static readonly Color LElectric = new Color(0.55f, 0.88f, 1f, 0f);

        public static readonly Vector3 LancerHip = new Vector3(0f, 0.47f, 0f);
        public static readonly Vector3 LancerLegOffset = new Vector3(0.085f, -0.02f, 0f);   // gövdeye göre değil, köke göre: kalça + bu
        public static readonly Vector3 LancerShoulder = new Vector3(0.2f, 0.29f, 0f);      // gövde pivotuna göre

        public static Mesh LancerBody()
        {
            var b = new MeshBuilder();
            // Kemer + tunik eteği (aşağı genişler) + altın etek şeridi
            b.Box(new Vector3(0f, 0.02f, 0f), new Vector3(0.3f, 0.07f, 0.21f), LGold);
            b.Box(new Vector3(0f, -0.07f, 0f), new Vector3(0.34f, 0.15f, 0.24f), LNavy, default, 0.82f, 0.82f);
            b.Box(new Vector3(0f, -0.14f, 0f), new Vector3(0.345f, 0.025f, 0.245f), LGoldDark);
            b.Box(new Vector3(0f, -0.06f, 0.115f), new Vector3(0.1f, 0.14f, 0.02f), LGold, default, 0.6f, 1f);   // ön etek plakası
            // Gövde (omuzlara doğru genişler) + altın göğüs zırhı + karın plakaları
            b.Box(new Vector3(0f, 0.18f, 0f), new Vector3(0.28f, 0.28f, 0.19f), LNavy, default, 1.18f, 1.05f);
            b.Box(new Vector3(0f, 0.2f, 0.07f), new Vector3(0.27f, 0.22f, 0.08f), LGold, new Vector3(-6f, 0f, 0f), 1.12f, 1f);
            b.Box(new Vector3(0f, 0.27f, 0.105f), new Vector3(0.2f, 0.05f, 0.03f), LGoldLight, new Vector3(-6f, 0f, 0f));
            for (int i = 0; i < 2; i++) b.Box(new Vector3(0f, 0.07f + i * 0.045f, 0.09f), new Vector3(0.2f - i * 0.02f, 0.035f, 0.05f), LGoldDark);
            // Göğüste parlayan şimşek amblemi
            var e0 = new Vector3(-0.025f, 0.26f, 0.12f);
            var e1 = e0 + new Vector3(0.04f, -0.05f, 0f);
            var e2 = e1 + new Vector3(-0.03f, -0.005f, 0f);
            var e3 = e2 + new Vector3(0.04f, -0.06f, 0f);
            b.Cylinder(e0, e1, 0.013f, 0.013f, 4, LElectric, true, true);
            b.Cylinder(e1, e2, 0.013f, 0.013f, 4, LElectric, true, true);
            b.Cylinder(e2, e3, 0.013f, 0f, 4, LElectric, true, true);
            // Omuzluklar (katmanlı altın)
            for (int s = -1; s <= 1; s += 2)
            {
                b.Sphere(new Vector3(0.19f * s, 0.3f, 0f), new Vector3(0.1f, 0.07f, 0.1f), LGold, 8, 5, new Vector3(0f, 0f, -18f * s), true);
                b.Sphere(new Vector3(0.21f * s, 0.26f, 0f), new Vector3(0.09f, 0.04f, 0.09f), LGoldDark, 8, 4, new Vector3(0f, 0f, -25f * s), true);
            }
            // Boyun + altın miğfer + koyu yüz + parlayan vizör yarığı
            b.Cylinder(new Vector3(0f, 0.31f, 0f), new Vector3(0f, 0.36f, 0f), 0.05f, 0.05f, 6, LNavyDark, true, true);
            b.Sphere(new Vector3(0f, 0.43f, 0f), new Vector3(0.1f, 0.11f, 0.105f), LGold, 10, 7, default, true);
            b.Box(new Vector3(0f, 0.41f, 0.075f), new Vector3(0.13f, 0.09f, 0.06f), LNavyDark, default, 1f, 1f);
            b.Box(new Vector3(0f, 0.425f, 0.104f), new Vector3(0.11f, 0.018f, 0.01f), LElectric);
            b.Box(new Vector3(0f, 0.39f, 0.1f), new Vector3(0.03f, 0.06f, 0.02f), LGold);   // burun koruması
            // Miğfer tepeliği: öne-arkaya kavisli beyaz-mavi püskül
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.Lerp(-40f, 120f, i / 5f) * Mathf.Deg2Rad;
                var c = new Vector3(0f, 0.43f + Mathf.Cos(a) * 0.14f, Mathf.Sin(a) * -0.13f);
                b.Box(c, new Vector3(0.035f, 0.07f, 0.06f), i % 2 == 0 ? LWhite : new Color(0.7f, 0.82f, 1f), new Vector3(Mathf.Rad2Deg * a, 0f, 0f));
            }
            // Miğfer yanlarında küçük altın kanatlar
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 3; i++)
                    b.Box(new Vector3(0.11f * s, 0.45f + i * 0.025f, -0.02f - i * 0.02f), new Vector3(0.015f, 0.025f, 0.09f - i * 0.02f), LGoldLight, new Vector3(-25f - i * 10f, 0f, 0f));
            // Sırtta beyaz melek kanatları (altın kemikli)
            for (int s = -1; s <= 1; s += 2)
            {
                var root = new Vector3(0.07f * s, 0.24f, -0.1f);
                var tip = root + new Vector3(0.24f * s, 0.2f, -0.12f);
                b.Cylinder(root, tip, 0.02f, 0.012f, 5, LGold, true, true);
                for (int i = 0; i < 6; i++)
                {
                    float k = i / 5f;
                    var top = Vector3.Lerp(root, tip, k);
                    float len = Mathf.Lerp(0.14f, 0.27f, k);
                    var fc = i % 2 == 0 ? LWhite : new Color(0.8f, 0.86f, 1f);
                    b.Box(top + new Vector3(0.012f * s, -len * 0.45f, -0.01f - k * 0.01f), new Vector3(0.075f, len, 0.014f), fc, new Vector3(-8f, 0f, (6f + k * 14f) * s), 0.55f, 1f);
                }
            }
            return b.ToMesh("Lancer_Body");
        }

        public static Mesh LancerLeg()
        {
            var b = new MeshBuilder();
            b.Cylinder(Vector3.zero, new Vector3(0f, -0.2f, 0f), 0.058f, 0.048f, 7, LNavyDark, true, true);       // uyluk
            b.Sphere(new Vector3(0f, -0.21f, 0.015f), new Vector3(0.055f, 0.05f, 0.05f), LGold, 7, 5, default, true); // dizlik
            b.Cylinder(new Vector3(0f, -0.22f, 0f), new Vector3(0f, -0.4f, 0f), 0.05f, 0.042f, 7, LGold, true, true); // altın tozluk
            b.Box(new Vector3(0f, -0.3f, 0.04f), new Vector3(0.05f, 0.12f, 0.02f), LGoldLight);
            b.Box(new Vector3(0f, -0.425f, 0.025f), new Vector3(0.09f, 0.06f, 0.17f), LGoldDark, default, 0.9f, 0.85f); // çizme
            return b.ToMesh("Lancer_Leg");
        }

        /// <summary>Sol kol: kalkanlı (yuvarlak altın kalkan, ortası lacivert, üstünde parlayan şimşek).</summary>
        public static Mesh LancerArmShield()
        {
            var b = new MeshBuilder();
            var elbow = new Vector3(-0.02f, -0.15f, 0f);
            var hand = new Vector3(-0.02f, -0.2f, 0.12f);
            b.Cylinder(Vector3.zero, elbow, 0.045f, 0.04f, 6, LNavy, true, true);
            b.Cylinder(elbow, hand, 0.042f, 0.036f, 6, LGold, true, true);
            b.Sphere(hand, Vector3.one * 0.035f, LGoldDark, 6, 4, default, true);
            var c = hand + new Vector3(-0.04f, 0f, 0.03f);
            b.Cylinder(c, c + new Vector3(-0.03f, 0f, 0f), 0.15f, 0.15f, 12, LGold, true, true);
            b.Cylinder(c + new Vector3(-0.029f, 0f, 0f), c + new Vector3(-0.036f, 0f, 0f), 0.11f, 0.11f, 12, LNavy, true, true);
            var z0 = c + new Vector3(-0.04f, 0.07f, -0.02f);
            var z1 = z0 + new Vector3(0f, -0.06f, 0.04f);
            var z2 = z1 + new Vector3(0f, 0f, -0.035f);
            var z3 = z2 + new Vector3(0f, -0.07f, 0.04f);
            b.Cylinder(z0, z1, 0.014f, 0.014f, 4, LElectric, true, true);
            b.Cylinder(z1, z2, 0.014f, 0.014f, 4, LElectric, true, true);
            b.Cylinder(z2, z3, 0.014f, 0f, 4, LElectric, true, true);
            return b.ToMesh("Lancer_ArmShield");
        }

        /// <summary>Sağ kol: dik tutulan altın mızrak, ucu parlayan elektrik bıçağı + şimşek süsü.</summary>
        public static Mesh LancerArmSpear()
        {
            var b = new MeshBuilder();
            var elbow = new Vector3(0.02f, -0.15f, 0f);
            var hand = new Vector3(0.02f, -0.2f, 0.12f);
            b.Cylinder(Vector3.zero, elbow, 0.045f, 0.04f, 6, LNavy, true, true);
            b.Cylinder(elbow, hand, 0.042f, 0.036f, 6, LGold, true, true);
            b.Sphere(hand, Vector3.one * 0.036f, LGoldDark, 6, 4, default, true);
            var dir = new Vector3(0f, 1f, 0.12f).normalized;
            var bottom = hand - dir * 0.3f;
            var top = hand + dir * 0.82f;
            b.Cylinder(bottom, top, 0.017f, 0.017f, 6, LGoldLight, true, true);
            b.Cylinder(bottom, bottom - dir * 0.04f, 0.022f, 0f, 6, LGoldDark, true, true);
            b.Cylinder(top - dir * 0.03f, top + dir * 0.02f, 0.035f, 0.035f, 6, LGold, true, true);      // bıçak bileziği
            // Parlayan elektrik bıçağı (yassı, sivri) + yanlarda şimşek kanatçıkları
            b.Box(top + dir * 0.12f, new Vector3(0.07f, 0.2f, 0.02f), LElectric, Quaternion.FromToRotation(Vector3.up, dir).eulerAngles, 0.05f, 1f);
            b.Box(top + dir * 0.1f, new Vector3(0.025f, 0.18f, 0.04f), new Color(0.95f, 1f, 1f, 0f), Quaternion.FromToRotation(Vector3.up, dir).eulerAngles, 0.05f, 1f);
            for (int s = -1; s <= 1; s += 2)
            {
                var w0 = top + dir * 0.03f;
                var w1 = w0 + new Vector3(0.06f * s, 0.02f, 0f);
                var w2 = w1 + new Vector3(-0.015f * s, 0.035f, 0f);
                var w3 = w2 + new Vector3(0.04f * s, 0.03f, 0f);
                b.Cylinder(w0, w1, 0.011f, 0.011f, 4, LElectric, true, true);
                b.Cylinder(w1, w2, 0.011f, 0.011f, 4, LElectric, true, true);
                b.Cylinder(w2, w3, 0.011f, 0f, 4, LElectric, true, true);
            }
            return b.ToMesh("Lancer_ArmSpear");
        }

        public static readonly Vector3 WingHingeR = new Vector3(0.16f, 0.66f, 0.05f);

        // ─────────────────────────────────────────────────────
        // YILDIRIM (Şimşek Süvarisi mermisi, uç -Z'de)
        // ─────────────────────────────────────────────────────
        static Mesh boltMesh;
        public static Mesh LightningBolt => boltMesh != null ? boltMesh : (boltMesh = BuildBolt());

        static Mesh BuildBolt()
        {
            // Eklemeli (parlayan) materyalle çizilir: alfa 1, renk = parlaklık
            var b = new MeshBuilder();
            var core = new Color(1f, 0.97f, 0.75f);
            var halo = new Color(0.2f, 0.45f, 0.85f);
            // Keskin zikzak şimşek: arkadan (+Z) öne (-Z)
            var pts = new[]
            {
                new Vector3(0f, 0f, 0.65f), new Vector3(0.16f, 0.02f, 0.4f), new Vector3(-0.12f, -0.02f, 0.18f),
                new Vector3(0.14f, 0.02f, -0.05f), new Vector3(-0.1f, 0f, -0.3f), new Vector3(0.04f, 0f, -0.62f),
            };
            for (int i = 0; i < pts.Length - 1; i++)
            {
                var d = pts[i + 1] - pts[i];
                var c = (pts[i] + pts[i + 1]) * 0.5f;
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                float len = d.magnitude + 0.04f;
                bool last = i == pts.Length - 2;
                b.Box(c, new Vector3(0.2f, 0.2f, len), halo, new Vector3(0f, yaw, 45f), last ? 0.2f : 1f, 1f);
                b.Box(c, new Vector3(0.07f, 0.07f, len), core, new Vector3(0f, yaw, 45f), last ? 0.2f : 1f, 1f);
            }
            b.Sphere(new Vector3(0.04f, 0f, -0.55f), new Vector3(0.12f, 0.12f, 0.16f), core, 6, 4);
            return b.ToMesh("LightningBolt");
        }

        // ─────────────────────────────────────────────────────
        // FIRTINA BULUTU (Fırtına Lordu'nun bindiği koyu bulut) — yarıçap ~1, kalınlık ~0.5, merkez orijinde
        // ─────────────────────────────────────────────────────
        public static Mesh StormCloud()
        {
            var b = new MeshBuilder();
            var under = new Color(0.14f, 0.14f, 0.22f);
            var mid = new Color(0.25f, 0.25f, 0.37f);
            var top = new Color(0.38f, 0.39f, 0.54f);
            var spark = G(new Color(0.55f, 0.85f, 1f));
            var rnd = new System.Random(9);
            b.Sphere(new Vector3(0f, 0f, 0f), new Vector3(0.95f, 0.26f, 0.85f), under, 12, 5, default, true);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.3f;
                float r = 0.55f + (float)rnd.NextDouble() * 0.3f;
                var p = new Vector3(Mathf.Cos(a) * r, 0.08f + (float)rnd.NextDouble() * 0.08f, Mathf.Sin(a) * r * 0.9f);
                float s = 0.28f + (float)rnd.NextDouble() * 0.14f;
                b.Sphere(p, new Vector3(s, s * 0.75f, s), i % 2 == 0 ? mid : top, 8, 5, new Vector3(0f, (float)rnd.NextDouble() * 90f, 0f), true);
            }
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f + 0.4f;
                var p = new Vector3(Mathf.Cos(a) * 0.35f, 0.24f, Mathf.Sin(a) * 0.3f);
                b.Sphere(p, new Vector3(0.3f, 0.2f, 0.3f), top, 8, 5, default, true);
            }
            // Altta, bulutun içinden görünen elektrik damarları
            for (int i = 0; i < 6; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                var p0 = new Vector3(Mathf.Cos(a) * 0.2f, -0.2f, Mathf.Sin(a) * 0.2f);
                var p1 = p0 + new Vector3(Mathf.Cos(a + 0.5f) * 0.35f, -0.05f, Mathf.Sin(a + 0.5f) * 0.3f);
                var p2 = p1 + new Vector3(Mathf.Cos(a - 0.4f) * 0.3f, 0.02f, Mathf.Sin(a - 0.4f) * 0.25f);
                b.Cylinder(p0, p1, 0.025f, 0.02f, 4, spark, true, true);
                b.Cylinder(p1, p2, 0.02f, 0f, 4, spark, true, true);
            }
            return b.ToMesh("StormCloud");
        }

        /// <summary>Uzakta / aşağıda süzülen dekor bulutu: bir kaç yassı küreden oluşan küme. Yarıçap ~1.</summary>
        public static Mesh DriftCloud(int seed)
        {
            var b = new MeshBuilder();
            var rnd = new System.Random(seed);
            var under = new Color(0.22f, 0.22f, 0.33f, 1f);
            var topC = new Color(0.4f, 0.41f, 0.56f, 1f);
            b.Sphere(Vector3.zero, new Vector3(1f, 0.32f, 0.7f), under, 10, 5, default, true);
            int n = 4 + rnd.Next(3);
            for (int i = 0; i < n; i++)
            {
                var p = new Vector3(((float)rnd.NextDouble() - 0.5f) * 1.4f, 0.12f + (float)rnd.NextDouble() * 0.15f, ((float)rnd.NextDouble() - 0.5f) * 0.8f);
                float s = 0.3f + (float)rnd.NextDouble() * 0.25f;
                b.Sphere(p, new Vector3(s, s * 0.7f, s * 0.9f), topC, 8, 5, default, true);
            }
            return b.ToMesh("DriftCloud");
        }

        /// <summary>Boss'un altına bulut ekler (bulut kendi kendine döner, alttan küçük şimşekler çakar).</summary>
        public static GameObject AttachStormCloud(Transform bossRoot, float scale, float height)
        {
            var go = CharacterDesigns.MakeVisual("StormCloud", StormCloud(), bossRoot);
            go.transform.localPosition = new Vector3(0f, height, 0f);
            go.transform.localScale = new Vector3(scale, scale * 0.9f, scale);
            go.AddComponent<StormCloudRide>();
            return go;
        }
    }

    /// <summary>Bulut ruhu: yerden yüksekte süzülür, hafifçe yalpalar.</summary>
    public class FloatMotion : MonoBehaviour
    {
        public float hover = 0.12f;
        Vector3 basePos;
        Quaternion baseRot;
        float phase;

        void Start()
        {
            basePos = transform.localPosition + Vector3.up * hover;
            baseRot = transform.localRotation;
            phase = Random.value * 6.28f;
        }

        void Update()
        {
            float t = Time.time * 3.2f + phase;
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(t) * 0.07f;
            transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t * 0.7f) * 6f + 8f, Mathf.Sin(t * 0.5f) * 10f, Mathf.Sin(t * 0.9f) * 7f);
        }
    }

    /// <summary>Griffon kanatlarını çırpar (prefabdaki WingL/WingR alt nesneleri).</summary>
    public class WingFlap : MonoBehaviour
    {
        public float rate = 7f;
        public float amplitude = 28f;
        Transform left, right;
        Quaternion lBase, rBase;
        float phase;

        void Start()
        {
            left = transform.Find("WingL");
            right = transform.Find("WingR");
            if (left != null) lBase = left.localRotation;
            if (right != null) rBase = right.localRotation;
            phase = Random.value * 6.28f;
        }

        void Update()
        {
            float a = Mathf.Sin(Time.time * rate + phase) * amplitude;
            if (right != null) right.localRotation = rBase * Quaternion.Euler(0f, 0f, a);
            if (left != null) left.localRotation = lBase * Quaternion.Euler(0f, 0f, -a);
        }
    }

    /// <summary>Fırtına Lordu'nun bulutu: yavaşça döner, nefes alır gibi büyüyüp küçülür, alttan ara ara yere şimşek çakar.</summary>
    public class StormCloudRide : MonoBehaviour
    {
        Vector3 baseScale;
        float boltT = 1f, phase;

        void Start()
        {
            baseScale = transform.localScale;
            phase = Random.value * 6f;
        }

        void Update()
        {
            float t = Time.time + phase;
            transform.localRotation = Quaternion.Euler(0f, t * 12f, 0f);
            float k = 1f + Mathf.Sin(t * 1.7f) * 0.03f;
            transform.localScale = new Vector3(baseScale.x * k, baseScale.y * (2f - k), baseScale.z * k);
            boltT -= Time.deltaTime;
            if (boltT <= 0f)
            {
                boltT = Random.Range(0.5f, 1.4f);
                var c = transform.position;
                float r = baseScale.x * 0.6f;
                var from = c + new Vector3(Random.Range(-r, r), -baseScale.y * 0.15f, Random.Range(-r, r) * 0.6f);
                var to = new Vector3(from.x + Random.Range(-0.8f, 0.8f), 0.02f, from.z + Random.Range(-0.8f, 0.4f));
                Effects.Lightning(from, to, 0.18f, new Color(0.55f, 0.85f, 1f), 0.16f);
                Effects.Zap(to, 0.6f);
            }
        }
    }
}

namespace StratejiGame
{
    /// <summary>
    /// Şimşek Süvarisi yürüyüş animasyonu (kodla, kemiksiz): yürürken bacaklar ve kollar karşılıklı sallanır,
    /// gövde adım ritmiyle iner-kalkar ve hafif öne eğilir; dururken nefes alır.
    /// Saldırıda (Enemy.onAttack) mızraklı kol geriye çekilip öne savrulur (şimşek fırlatma).
    /// Prefab yapısı: kök / Body (ArmL, ArmR çocukları) / LegL / LegR.
    /// </summary>
    public class KnightWalk : MonoBehaviour
    {
        public float stepRate = 7.5f;
        Transform body, legL, legR, armL, armR;
        Quaternion qBody, qLegL, qLegR, qArmL, qArmR;
        Vector3 bodyPos;
        Enemy enemy;
        Vector3 last;
        float phase, moveBlend, throwT = -1f;
        Transform[] flaps = new Transform[0];
        Quaternion[] qFlaps;

        void Start()
        {
            body = transform.Find("Body");
            legL = transform.Find("LegL");
            legR = transform.Find("LegR");
            armL = body != null ? body.Find("ArmL") : null;
            armR = body != null ? body.Find("ArmR") : null;
            if (body != null) { qBody = body.localRotation; bodyPos = body.localPosition; }
            if (legL != null) qLegL = legL.localRotation;
            if (legR != null) qLegR = legR.localRotation;
            if (armL != null) qArmL = armL.localRotation;
            if (armR != null) qArmR = armR.localRotation;
            // Kumaş parçaları (adı "Flap" ile başlayan gövde çocukları): cüppe etekleri, kuşak, şal uçları
            if (body != null)
            {
                var list = new System.Collections.Generic.List<Transform>();
                foreach (Transform c in body) if (c.name.StartsWith("Flap")) list.Add(c);
                flaps = list.ToArray();
                qFlaps = new Quaternion[flaps.Length];
                for (int i = 0; i < flaps.Length; i++) qFlaps[i] = flaps[i].localRotation;
            }
            enemy = GetComponentInParent<Enemy>();
            if (enemy != null) enemy.onAttack += OnAttack;
            last = transform.position;
            phase = Random.value * 6.28f;
        }

        void OnDestroy() { if (enemy != null) enemy.onAttack -= OnAttack; }

        void OnAttack() { throwT = 0f; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var p = transform.position;
            float speed = new Vector2(p.x - last.x, p.z - last.z).magnitude / dt;
            last = p;
            moveBlend = Mathf.MoveTowards(moveBlend, speed > 0.3f ? 1f : 0f, dt * 5f);
            phase += dt * stepRate * Mathf.Max(moveBlend, 0.15f);
            float s = Mathf.Sin(phase), m = moveBlend;

            // Fırlatma: geriye çek (0.2 sn) → öne savur (0.12 sn) → yavaşça bırak
            float throwA = 0f;
            if (throwT >= 0f)
            {
                throwT += dt;
                if (throwT < 0.2f) throwA = Mathf.Lerp(0f, 80f, throwT / 0.2f);
                else if (throwT < 0.32f) throwA = Mathf.Lerp(80f, -55f, (throwT - 0.2f) / 0.12f);
                else if (throwT < 0.7f) throwA = Mathf.Lerp(-55f, 0f, (throwT - 0.32f) / 0.38f);
                else throwT = -1f;
            }

            if (legL != null) legL.localRotation = qLegL * Quaternion.Euler(-32f * s * m, 0f, 0f);
            if (legR != null) legR.localRotation = qLegR * Quaternion.Euler(32f * s * m, 0f, 0f);
            if (armL != null) armL.localRotation = qArmL * Quaternion.Euler(24f * s * m, 0f, 0f);
            if (armR != null) armR.localRotation = qArmR * Quaternion.Euler(-8f * s * m + throwA, 0f, 0f);
            if (body != null)
            {
                float breathe = (1f - m) * Mathf.Sin(Time.time * 2f) * 0.006f;
                body.localPosition = bodyPos + Vector3.up * (Mathf.Abs(Mathf.Cos(phase)) * 0.025f * m + breathe);
                body.localRotation = qBody * Quaternion.Euler(6f * m + throwA * 0.08f, 5f * s * m, 0f);
            }
            // Kumaş: yürürken rüzgârla geriye savrulur ve dalgalanır, dururken hafifçe sallanır
            for (int i = 0; i < flaps.Length; i++)
            {
                float w = Mathf.Sin(Time.time * 6f + i * 1.3f) * (4f + 8f * m) + Mathf.Sin(phase * 2f + i) * 6f * m;
                flaps[i].localRotation = qFlaps[i] * Quaternion.Euler(18f * m + w, 0f, Mathf.Sin(Time.time * 4f + i * 2f) * 5f);
            }
        }
    }
}
