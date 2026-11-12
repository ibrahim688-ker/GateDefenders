using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Koddan çizilen beyaz ikonlar (Image.color ile renklendirilir). Koordinatlar -1..1, y yukarı;
    /// kenarlar 4x4 alt örneklemeyle yumuşatılır. Her ikon ilk istendiğinde bir kez üretilir.
    /// </summary>
    public static class UiIcons
    {
        static Sprite pill, fadeLine, diamond, rounded, skull, flag, vignette;
        static Sprite heart, sword, chevrons, troops, blast, potion, flame, coin, coinFace, star, spark;

        public static Sprite ForUpgrade(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.MaxHp: return Heart;
                case UpgradeId.Attack: return Sword;
                case UpgradeId.SoldierLevel: return Chevrons;
                case UpgradeId.StartSoldiers: return Troops;
                case UpgradeId.AreaDamage: return Blast;
                case UpgradeId.Heal: return Potion;
                case UpgradeId.Rage: return Flame;
            }
            return UiKit.Circle;
        }

        public static Color ColorFor(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.MaxHp: return new Color(0.93f, 0.3f, 0.38f);
                case UpgradeId.Attack: return new Color(1f, 0.58f, 0.2f);
                case UpgradeId.SoldierLevel: return new Color(0.36f, 0.6f, 1f);
                case UpgradeId.StartSoldiers: return new Color(0.28f, 0.8f, 0.88f);
                case UpgradeId.AreaDamage: return new Color(0.72f, 0.46f, 1f);
                case UpgradeId.Heal: return new Color(0.36f, 0.86f, 0.46f);
                case UpgradeId.Rage: return new Color(1f, 0.36f, 0.22f);
            }
            return Color.white;
        }

        // ------------------------------------------------------------------ şekiller
        /// <summary>
        /// Yuvarlak uçlu (hap / oval) düğme zemini: 9-dilimli, kenar 64 px. Tam yarım daire uçlar için
        /// Image.pixelsPerUnitMultiplier = 128 / yükseklik verilir (bkz. FitPill).
        /// </summary>
        public static Sprite Pill
        {
            get
            {
                if (pill != null) return pill;
                var tex = ShapeTex(128, (x, y) => x * x + y * y <= 1f);
                pill = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(63, 63, 63, 63));
                return pill;
            }
        }

        /// <summary>
        /// Yuvarlak köşeli dikdörtgen (9-dilimli, köşe yarıçapı 24 px). Round(img, r) ile istenen yarıçapa ayarlanır.
        /// </summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int n = 64;
                const float r = 24f;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        // yuvarlak dikdörtgen işaretli uzaklık alanı (kenar yumuşatmalı)
                        float qx = Mathf.Max(Mathf.Abs(x + 0.5f - n * 0.5f) - (n * 0.5f - r), 0f);
                        float qy = Mathf.Max(Mathf.Abs(y + 0.5f - n * 0.5f) - (n * 0.5f - r), 0f);
                        float d = Mathf.Sqrt(qx * qx + qy * qy) - r;
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - d) * 255));
                    }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                rounded = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
                return rounded;
            }
        }

        /// <summary>Görüntüyü yuvarlak köşeli yapar (radius: canvas biriminde köşe yarıçapı).</summary>
        public static UnityEngine.UI.Image Round(UnityEngine.UI.Image img, float radius = 24f)
        {
            img.sprite = Rounded;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 24f / Mathf.Max(1f, radius);
            return img;
        }

        public static UnityEngine.UI.Image Round(RectTransform rt, float radius = 24f) => Round(rt.GetComponent<UnityEngine.UI.Image>(), radius);

        /// <summary>Görüntüyü hap (uçları tam yarım daire) yapar; boyutu değişince kendini yeniden ayarlar.</summary>
        public static UnityEngine.UI.Image MakePill(UnityEngine.UI.Image img)
        {
            img.sprite = Pill;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            if (img.GetComponent<PillFit>() == null) img.gameObject.AddComponent<PillFit>();
            else FitPill(img);
            return img;
        }

        public static UnityEngine.UI.Image MakePill(RectTransform rt) => MakePill(rt.GetComponent<UnityEngine.UI.Image>());

        /// <summary>Hap zeminini yüksekliğe göre ayarlar: uçlar tam yarım daire olur.</summary>
        public static void FitPill(UnityEngine.UI.Image img)
        {
            // Kenar (63 px) ekranda yüksekliğin yarısı olsun: çizilen kenar = 63 * (canvas.referencePixelsPerUnit / sprite ppu) / çarpan
            var rt = (RectTransform)img.transform;
            float h = rt.rect.height;
            if (h <= 1f) return;
            float refPpu = img.canvas != null ? img.canvas.referencePixelsPerUnit : 100f;
            float m = 63f * (refPpu / 100f) / (h * 0.5f);
            if (Mathf.Abs(img.pixelsPerUnitMultiplier - m) > 0.001f) img.pixelsPerUnitMultiplier = m;
        }

        /// <summary>Yatay süs çizgisi: ortada dolu, iki uca doğru saydamlaşır.</summary>
        public static Sprite FadeLine
        {
            get
            {
                if (fadeLine != null) return fadeLine;
                var tex = new Texture2D(256, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[256 * 4];
                for (int x = 0; x < 256; x++)
                {
                    float d = Mathf.Abs((x + 0.5f) / 256f * 2f - 1f);
                    byte a = (byte)(Mathf.Clamp01(1f - d * d) * 255);
                    for (int y = 0; y < 4; y++) px[y * 256 + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                return fadeLine = Sprite.Create(tex, new Rect(0, 0, 256, 4), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        /// <summary>Küçük elmas (süs çizgisinin ortası).</summary>
        public static Sprite Diamond => diamond != null ? diamond : (diamond = Shape(64, (x, y) => Mathf.Abs(x) + Mathf.Abs(y) <= 0.95f));

        /// <summary>Kurukafa (boss işareti): kafatası + çene, göz/burun boşlukları, diş aralıkları.</summary>
        public static Sprite Skull => skull != null ? skull : (skull = Shape(128, (x, y) =>
        {
            bool cranium = x * x + (y - 0.15f) * (y - 0.15f) <= 0.64f * 0.64f;
            bool jaw = Mathf.Abs(x) <= 0.36f && y >= -0.68f && y <= -0.1f;
            if (!(cranium || jaw)) return false;
            float ax = Mathf.Abs(x) - 0.25f;
            if (ax * ax + (y - 0.05f) * (y - 0.05f) <= 0.17f * 0.17f) return false;               // gözler
            if (y <= -0.12f && y >= -0.3f && Mathf.Abs(x) <= 0.09f * (1f - (-0.12f - y) / 0.18f) + 0.02f) return false; // burun
            if (y <= -0.42f && (Mathf.Abs(x - 0.12f) < 0.035f || Mathf.Abs(x + 0.12f) < 0.035f)) return false;       // dişler
            return true;
        }));

        static Sprite clock;

        /// <summary>Saat (süre).</summary>
        public static Sprite Clock => clock != null ? clock : (clock = Shape(128, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > 0.92f) return false;
            if (r > 0.74f) return true;                                                    // kasa
            // akrep (yukarı) ve yelkovan (sağa)
            if (Mathf.Abs(x) <= 0.07f && y >= -0.05f && y <= 0.5f) return true;
            if (Mathf.Abs(y) <= 0.07f && x >= -0.05f && x <= 0.4f) return true;
            return r <= 0.1f;
        }));

        /// <summary>Bayrak (bölüm sonu işareti).</summary>
        public static Sprite Flag => flag != null ? flag : (flag = Shape(128, (x, y) =>
        {
            if (x >= -0.62f && x <= -0.48f && y >= -0.92f && y <= 0.9f) return true;            // direk
            if (x > -0.48f && x <= 0.72f)
            {
                float wave = 0.06f * Mathf.Sin((x + 0.48f) * 5f);
                return y >= 0.1f + wave && y <= 0.86f + wave;                                     // dalgalanan bez
            }
            return false;
        }));

        /// <summary>Ekran kenarı kararması / kızarması: ortası saydam, kenarlara doğru koyulaşan elips.</summary>
        public static Sprite Vignette
        {
            get
            {
                if (vignette != null) return vignette;
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                        float r = Mathf.Sqrt(u * u + v * v);
                        float a = Mathf.Clamp01((r - 0.55f) / 0.6f);
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                    }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                return vignette = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        // ------------------------------------------------------------------ ikonlar
        public static Sprite Heart => heart != null ? heart : (heart = Shape(128, (x, y) =>
        {
            // klasik kalp eğrisi: (x²+y²-1)³ - x²y³ <= 0
            float px = x * 1.45f, py = y * 1.45f + 0.22f;
            float a = px * px + py * py - 1f;
            return a * a * a - px * px * py * py * py <= 0f;
        }));

        public static Sprite Sword => sword != null ? sword : (sword = Shape(128, SwordShape));

        static bool SwordShape(float x, float y)
        {
            // 45° döndürülmüş kılıç: u = bıçak ekseni, v = yan eksen
            const float k = 0.70710678f;
            x *= 0.78f; y *= 0.78f;
            float u = (x + y) * k, v = (y - x) * k;
            float av = Mathf.Abs(v);
            if (u >= -0.3f && u <= 0.72f && av <= 0.13f) return true;                                     // bıçak
            if (u > 0.72f && u <= 0.98f && av <= 0.13f * (1f - (u - 0.72f) / 0.26f)) return true;          // uç
            if (u >= -0.44f && u < -0.3f && av <= 0.36f) return true;                                      // balçak
            if (u >= -0.76f && u < -0.44f && av <= 0.08f) return true;                                      // kabza
            float du = u + 0.84f;
            return du * du + v * v <= 0.13f * 0.13f;                                                        // topuz
        }

        static Sprite swords, upArrow, person, gear;

        /// <summary>Çapraz iki kılıç (Savaş sekmesi).</summary>
        public static Sprite Swords => swords != null ? swords : (swords = Shape(128, (x, y) => SwordShape(x * 0.95f, y * 0.95f) || SwordShape(-x * 0.95f, y * 0.95f)));

        /// <summary>Kalın yukarı ok (Yükseltme sekmesi).</summary>
        public static Sprite UpArrow => upArrow != null ? upArrow : (upArrow = Shape(128, (x, y) =>
        {
            if (y >= 0.02f && y <= 0.9f && Mathf.Abs(x) <= 0.78f * (0.9f - y) / 0.88f) return true;     // ok ucu
            return Mathf.Abs(x) <= 0.27f && y >= -0.88f && y < 0.05f;                                     // gövde
        }));

        /// <summary>Kişi silueti (Profil sekmesi).</summary>
        public static Sprite PersonIcon => person != null ? person : (person = Shape(128, (x, y) =>
        {
            if (x * x + (y - 0.4f) * (y - 0.4f) <= 0.3f * 0.3f) return true;                            // baş
            float ex = x / 0.66f, ey = (y + 0.62f) / 0.58f;
            return y <= 0.0f && y >= -0.9f && ex * ex + ey * ey <= 1f;                                     // omuzlar
        }));

        /// <summary>Dişli çark (Ayarlar sekmesi).</summary>
        public static Sprite Gear => gear != null ? gear : (gear = Shape(128, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r < 0.3f) return false;                                                                    // ortadaki delik
            float ang = Mathf.Atan2(y, x);
            float tooth = Mathf.Cos(ang * 8f);
            return r <= (tooth > 0.25f ? 0.92f : 0.68f);
        }));

        public static Sprite Chevrons => chevrons != null ? chevrons : (chevrons = Shape(128, (x, y) =>
        {
            // üst üste 3 rütbe şeridi (^)
            for (int i = 0; i < 3; i++)
            {
                float cy = 0.42f - i * 0.5f;
                var p = new Vector2(x, y);
                float d = Mathf.Min(SegDist(p, new Vector2(-0.72f, cy - 0.32f), new Vector2(0f, cy + 0.2f)),
                                    SegDist(p, new Vector2(0f, cy + 0.2f), new Vector2(0.72f, cy - 0.32f)));
                if (d <= 0.12f) return true;
            }
            return false;
        }));

        public static Sprite Troops => troops != null ? troops : (troops = Shape(128, (x, y) =>
        {
            // önde büyük, arkada iki küçük asker silueti (baş + omuz)
            if (Person(x, y, 0f, -0.1f, 1f)) return true;
            bool back = Person(x, y, -0.55f, 0.05f, 0.72f) || Person(x, y, 0.55f, 0.05f, 0.72f);
            // arka askerler öndekinin hemen çevresinde boşlukla ayrılsın
            return back && !Person(x, y, 0f, -0.1f, 1.18f);
        }));

        static bool Person(float x, float y, float cx, float cy, float s)
        {
            float hx = (x - cx) / s, hy = (y - cy) / s;
            if (hx * hx + (hy - 0.38f) * (hy - 0.38f) <= 0.26f * 0.26f) return true;                       // baş
            // omuzlar: yarım elips
            float ex = hx / 0.5f, ey = (hy + 0.25f) / 0.45f;
            return hy <= -0.02f && hy >= -0.7f && ex * ex + ey * ey <= 1f;
        }

        public static Sprite Blast => blast != null ? blast : (blast = Shape(128, (x, y) =>
        {
            // 8 köşeli patlama yıldızı, ortası delik halka
            float r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Atan2(y, x);
            float edge = 0.55f + 0.38f * Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 4f)), 4f) + 0.08f * Mathf.Abs(Mathf.Cos(a * 8f + 0.4f));
            if (r > edge) return false;
            return r > 0.3f || r < 0.17f;
        }));

        public static Sprite Potion => potion != null ? potion : (potion = Shape(128, (x, y) =>
        {
            bool body = x * x + (y + 0.3f) * (y + 0.3f) <= 0.6f * 0.6f;
            bool neck = Mathf.Abs(x) <= 0.17f && y >= 0.15f && y <= 0.62f;
            bool cork = Mathf.Abs(x) <= 0.26f && y > 0.62f && y <= 0.86f;
            if (!(body || neck || cork)) return false;
            // gövdede artı işareti (boşluk)
            bool cross = (Mathf.Abs(x) <= 0.08f && Mathf.Abs(y + 0.3f) <= 0.3f) || (Mathf.Abs(y + 0.3f) <= 0.08f && Mathf.Abs(x) <= 0.3f);
            return !cross;
        }));

        public static Sprite Flame => flame != null ? flame : (flame = Shape(128, (x, y) => FlameShape(x, y) && !FlameShape(x * 2.2f, (y + 0.55f) * 2.2f - 0.45f)));

        // Alev: taban dairesi + ortada uzun, yanlarda iki kısa dil (içte boşluk = iki tonlu görünüm)
        static bool FlameShape(float x, float y)
        {
            if (x * x + (y + 0.4f) * (y + 0.4f) <= 0.52f * 0.52f) return true;
            return Tongue(x, y, -0.4f, 0.06f, 0.98f, 0.52f) || Tongue(x, y, -0.5f, -0.5f, 0.38f, 0.32f) || Tongue(x, y, -0.5f, 0.52f, 0.5f, 0.3f);
        }

        static bool Tongue(float x, float y, float baseY, float tipX, float tipY, float w0)
        {
            if (y < baseY || y > tipY) return false;
            float t = (y - baseY) / (tipY - baseY);
            float cx = tipX * Mathf.Pow(t, 1.6f);
            return Mathf.Abs(x - cx) <= w0 * Mathf.Pow(1f - t, 0.9f);
        }

        /// <summary>Altın para: dış disk (koyu altın ile boyanır).</summary>
        public static Sprite Coin => coin != null ? coin : (coin = Shape(64, (x, y) => x * x + y * y <= 0.96f));

        /// <summary>Altın paranın üst yüzü: iç halka + ortada yıldız (açık altın ile boyanır).</summary>
        public static Sprite CoinFace => coinFace != null ? coinFace : (coinFace = Shape(64, (x, y) =>
        {
            float r2 = x * x + y * y;
            if (r2 > 0.72f * 0.72f) return false;
            if (r2 > 0.58f * 0.58f) return true;                      // ince iç halka
            return StarShape(x / 0.45f, y / 0.45f);
        }));

        /// <summary>Beş köşeli yıldız (kıvılcımlar için).</summary>
        public static Sprite Star => star != null ? star : (star = Shape(64, (x, y) => StarShape(x, y)));

        /// <summary>Yumuşak parıltı noktası (alfa merkezden dışa azalır).</summary>
        public static Sprite Spark => spark != null ? spark : (spark = Soft(64));

        static bool StarShape(float x, float y)
        {
            // keskin uçlu 5 köşeli yıldız: açı bir kola katlanır, dış uç → iç köşe doğrusunun içinde mi bakılır
            const float R = 0.97f, Rin = 0.44f;
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > R) return false;
            if (r <= Rin * 0.8f) return true;
            float seg = Mathf.PI * 2f / 5f;
            float a = Mathf.Atan2(x, y);                              // tepe yukarı
            a = Mathf.Abs(Mathf.Repeat(a + seg * 0.5f, seg) - seg * 0.5f);
            float px = r * Mathf.Cos(a), py = r * Mathf.Sin(a);
            float ix = Rin * Mathf.Cos(seg * 0.5f), iy = Rin * Mathf.Sin(seg * 0.5f);
            // O=(R,0) → I; nokta merkezle aynı taraftaysa içeride
            float ex = ix - R, ey = iy;
            float sideP = ex * py - ey * (px - R);
            float sideO = ex * 0f - ey * (0f - R);
            return sideP * sideO >= 0f;
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        // ------------------------------------------------------------------ güçlendirme sembolleri
        static Sprite bolt, bullet, pierceIcon, crosshair;

        static readonly Vector2[] BoltPts =
        {
            new Vector2(0.15f, 0.95f), new Vector2(-0.5f, -0.05f), new Vector2(-0.02f, -0.05f), new Vector2(-0.2f, -0.95f),
            new Vector2(0.55f, 0.15f), new Vector2(0.06f, 0.15f), new Vector2(0.35f, 0.95f),
        };

        /// <summary>Şimşek: atış hızı.</summary>
        public static Sprite Bolt => bolt != null ? bolt : (bolt = Shape(128, (x, y) => InPoly(BoltPts, x, y)));

        /// <summary>Mermi: +1 mermi.</summary>
        public static Sprite Bullet => bullet != null ? bullet : (bullet = Shape(128, (x, y) =>
        {
            if (Mathf.Abs(x) <= 0.3f && y >= -0.75f && y <= 0.25f) return true;                 // gövde
            if (y > 0.25f) { float ex = x / 0.3f, ey = (y - 0.25f) / 0.62f; return ex * ex + ey * ey <= 1f; }   // sivri uç
            return Mathf.Abs(x) <= 0.36f && y >= -0.9f && y <= -0.72f;                          // kovan dibi
        }));

        /// <summary>Delici: duvarı delip geçen ok.</summary>
        public static Sprite PierceIcon => pierceIcon != null ? pierceIcon : (pierceIcon = Shape(128, (x, y) =>
        {
            if (Mathf.Abs(y) <= 0.09f && x >= -0.9f && x <= 0.46f) return true;                 // gövde
            if (x >= 0.45f && x <= 0.95f && Mathf.Abs(y) <= 0.32f * (0.95f - x) / 0.5f) return true;   // uç
            if (x >= -0.25f && x <= -0.05f && Mathf.Abs(y) <= 0.75f && Mathf.Abs(y) > 0.2f) return true;   // delinen duvar
            // kuyruk tüyleri
            for (int s = -1; s <= 1; s += 2)
                if (SegDist(new Vector2(x, y), new Vector2(-0.62f, 0f), new Vector2(-0.88f, 0.3f * s)) <= 0.07f) return true;
            return false;
        }));

        /// <summary>Nişangah: kritik vuruş.</summary>
        public static Sprite Crosshair => crosshair != null ? crosshair : (crosshair = Shape(128, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r >= 0.56f && r <= 0.72f) return true;
            if (r <= 0.14f) return true;
            if (Mathf.Abs(x) <= 0.07f && Mathf.Abs(y) >= 0.34f && Mathf.Abs(y) <= 0.95f) return true;
            if (Mathf.Abs(y) <= 0.07f && Mathf.Abs(x) >= 0.34f && Mathf.Abs(x) <= 0.95f) return true;
            return false;
        }));

        static bool InPoly(Vector2[] pts, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
            {
                var a = pts[i]; var b = pts[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        // ------------------------------------------------------------------ üretim
        static Sprite Shape(int size, System.Func<float, float, bool> inside)
        {
            var tex = ShapeTex(size, inside);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Texture2D ShapeTex(int size, System.Func<float, float, bool> inside)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            const int ss = 4;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float fx = ((x + (sx + 0.5f) / ss) / size) * 2f - 1f;
                            float fy = ((y + (sy + 0.5f) / ss) / size) * 2f - 1f;
                            if (inside(fx, fy)) hits++;
                        }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / (ss * ss)));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        static Sprite Soft(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fx = (x + 0.5f) / size * 2f - 1f, fy = (y + 0.5f) / size * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(fx * fx + fy * fy));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    /// <summary>Hap şeklindeki görüntünün uçlarını boyut değiştikçe tam yarım daire tutar.</summary>
    public class PillFit : MonoBehaviour
    {
        UnityEngine.UI.Image img;

        void OnEnable() => Fit();
        void OnRectTransformDimensionsChange() => Fit();

        void Fit()
        {
            if (img == null) img = GetComponent<UnityEngine.UI.Image>();
            if (img != null) UiIcons.FitPill(img);
        }
    }
}
