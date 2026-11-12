using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StratejiGame
{
    /// <summary>
    /// Ana sayfa (Savaş sekmesi) arka planı: oyuncunun bulunduğu dünyaya göre değişir.
    /// Su altı: kabarcıklar, yosunlar, tepeden süzülen ışık · Yanardağ: kenarlardan aşağı akan lav, kızıl kaya,
    /// yükselen kıvılcımlar · Bataklık: sis, ateş böcekleri, sarkan sarmaşıklar · Kar: yağan kar, buzlanmış kenarlar,
    /// buz sarkıtları, kar yığını · Gökyüzü: kayan bulutlar, şimşek çakması · Çöl: güneş, piramitli kum tepeleri, savrulan kum.
    /// Ayrıca seviye amblemine temaya özel süs ekler (kabarcık halkası, alev tacı, yaprak çelengi, buz tacı, kanatlar, güneş ışınları).
    /// Tüm hareket dokuların uvRect kaydırmasıyla yapılır (parçacık sistemi yok, ucuz).
    /// </summary>
    public class MenuThemeView : MonoBehaviour
    {
        public struct Palette
        {
            public Color top, mid, bottom;   // arka plan geçişi
            public Color emblem, ring;        // seviye dairesi ve halkası
            public Color label;               // dünya adı
            public Color button;              // SAVAŞ düğmesi
        }

        public static Palette Get(WorldTheme t)
        {
            switch (t)
            {
                case WorldTheme.Volcano:
                    return new Palette { top = C(0.07f, 0.02f, 0.03f), mid = C(0.28f, 0.05f, 0.03f), bottom = C(0.7f, 0.2f, 0.04f),
                        emblem = C(0.86f, 0.3f, 0.06f), ring = new Color(1f, 0.62f, 0.2f, 0.35f), label = C(1f, 0.72f, 0.38f), button = C(0.85f, 0.2f, 0.08f) };
                case WorldTheme.Swamp:
                    return new Palette { top = C(0.02f, 0.06f, 0.04f), mid = C(0.06f, 0.14f, 0.07f), bottom = C(0.15f, 0.24f, 0.09f),
                        emblem = C(0.3f, 0.55f, 0.16f), ring = new Color(0.72f, 1f, 0.42f, 0.28f), label = C(0.76f, 1f, 0.5f), button = C(0.36f, 0.62f, 0.12f) };
                case WorldTheme.Frost:
                    return new Palette { top = C(0.05f, 0.14f, 0.3f), mid = C(0.14f, 0.34f, 0.55f), bottom = C(0.5f, 0.72f, 0.9f),
                        emblem = C(0.28f, 0.62f, 0.92f), ring = new Color(0.86f, 0.97f, 1f, 0.4f), label = C(0.82f, 0.96f, 1f), button = C(0.2f, 0.52f, 0.9f) };
                case WorldTheme.Sky:
                    return new Palette { top = C(0.07f, 0.05f, 0.2f), mid = C(0.2f, 0.18f, 0.42f), bottom = C(0.42f, 0.45f, 0.7f),
                        emblem = C(0.93f, 0.68f, 0.2f), ring = new Color(1f, 0.92f, 0.55f, 0.35f), label = C(1f, 0.88f, 0.5f), button = C(0.52f, 0.34f, 0.88f) };
                case WorldTheme.Desert:
                    return new Palette { top = C(0.3f, 0.1f, 0.12f), mid = C(0.76f, 0.36f, 0.18f), bottom = C(0.98f, 0.7f, 0.4f),
                        emblem = C(0.84f, 0.52f, 0.18f), ring = new Color(1f, 0.92f, 0.62f, 0.4f), label = C(1f, 0.9f, 0.62f), button = C(0.82f, 0.32f, 0.1f) };
                default:
                    return new Palette { top = C(0.01f, 0.07f, 0.17f), mid = C(0.02f, 0.18f, 0.32f), bottom = C(0.03f, 0.32f, 0.42f),
                        emblem = C(0.08f, 0.55f, 0.72f), ring = new Color(0.55f, 0.95f, 1f, 0.3f), label = C(0.6f, 0.95f, 1f), button = C(0.95f, 0.5f, 0.12f) };
            }
        }

        static Color C(float r, float g, float b) => new Color(r, g, b);

        // ------------------------------------------------------------------ hareketli öğeler
        class Scroll { public RawImage img; public float tiles; public Vector2 speed; public Vector2 off; public bool flip; public float wobble; public bool fixedY; }
        class Sway { public RectTransform rt; public float amp, freq, phase, baseRot; }
        class Spin { public RectTransform rt; public float speed; }
        class Pulse { public Graphic g; public float baseA, amp, freq, phase; public RectTransform scaleRt; public float scaleAmp; }
        class Fly { public RectTransform rt; public Image img; public Vector2 pos, vel; public float phase, speed; }

        readonly List<Scroll> scrolls = new List<Scroll>();
        readonly List<Sway> sways = new List<Sway>();
        readonly List<Spin> spins = new List<Spin>();
        readonly List<Pulse> pulses = new List<Pulse>();
        readonly List<Fly> flies = new List<Fly>();

        RectTransform layer, deco;
        public WorldTheme Theme { get; private set; } = (WorldTheme)(-1);

        // şimşek
        Image flash, bolt;
        float boltTimer, flashT;

        public static MenuThemeView Create(RectTransform parent)
        {
            var go = new GameObject("MenuTheme", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            UiKit.Stretch(rt, Vector2.zero, Vector2.one);
            var v = go.AddComponent<MenuThemeView>();
            v.layer = rt;
            return v;
        }

        RectTransform decoRoot;

        /// <summary>
        /// Temayı kurar. decoHost: süsün ekleneceği kap (Savaş sekmesinde seviye amblemi, Seviyeler haritasında boss düğümü);
        /// süsler 400 birimlik amblem için tasarlandı, kabın boyutuna göre ölçeklenir. null: süs yok.
        /// Arka plan yalnızca tema değişince, süs yalnızca tema veya kap değişince yeniden kurulur.
        /// </summary>
        public void Apply(WorldTheme t, RectTransform decoHost)
        {
            bool layerChanged = t != Theme;
            bool decoChanged = layerChanged || decoHost != deco || (decoHost != null && decoRoot == null);
            if (!layerChanged && !decoChanged) return;
            Theme = t;
            deco = decoHost;

            if (layerChanged)
            {
                for (int i = layer.childCount - 1; i >= 0; i--) Destroy(layer.GetChild(i).gameObject);
                scrolls.Clear(); sways.Clear(); spins.Clear(); pulses.Clear(); flies.Clear();
                flash = bolt = null;
                var p = Get(t);
                Raw("Sky", Vector2.zero, Vector2.one, MenuArt.Gradient(t.ToString(), p.top, p.mid, p.bottom), Color.white);
                switch (t)
                {
                    case WorldTheme.Volcano: BuildVolcano(); break;
                    case WorldTheme.Swamp: BuildSwamp(); break;
                    case WorldTheme.Frost: BuildFrost(); break;
                    case WorldTheme.Sky: BuildSky(); break;
                    case WorldTheme.Desert: BuildDesert(); break;
                    default: BuildUnderwater(); break;
                }
            }

            if (decoChanged)
            {
                if (decoRoot != null) Destroy(decoRoot.gameObject);
                decoRoot = null;
                if (decoHost != null)
                {
                    var go = new GameObject("ThemeDeco", typeof(RectTransform));
                    decoRoot = (RectTransform)go.transform;
                    decoRoot.SetParent(decoHost, false);
                    decoRoot.anchorMin = decoRoot.anchorMax = new Vector2(0.5f, 0.5f);
                    decoRoot.sizeDelta = new Vector2(400f, 400f);
                    decoRoot.anchoredPosition = Vector2.zero;
                    float w = decoHost.rect.width > 1f ? decoHost.rect.width : decoHost.sizeDelta.x;
                    decoRoot.localScale = Vector3.one * Mathf.Max(0.1f, w / 400f);
                    BuildDeco(t);
                }
            }
        }

        // ------------------------------------------------------------------ temalar
        void BuildUnderwater()
        {
            // Tepeden süzülen ışık
            var light = Img("Light", new Vector2(-0.3f, 0.55f), new Vector2(1.3f, 1.15f), new Color(0.45f, 0.9f, 1f, 0.16f), UiIcons.Spark);
            AddPulse(light, 0.16f, 0.05f, 0.5f, 0f);
            // Kabarcıklar (iki derinlik)
            AddScroll(Raw("BubblesFar", Vector2.zero, Vector2.one, MenuArt.Dots("bubbleS", 256, 26, 3f, 8f, true, 61), new Color(0.7f, 0.95f, 1f, 0.35f)), 3.2f, new Vector2(0f, -0.035f), 0.02f);
            AddScroll(Raw("BubblesNear", Vector2.zero, Vector2.one, MenuArt.Dots("bubbleB", 256, 9, 9f, 18f, true, 62), new Color(0.75f, 0.97f, 1f, 0.55f)), 1.6f, new Vector2(0f, -0.07f), 0.03f);
            // Köşelerde sallanan yosunlar
            var weed = MenuArt.Seaweed();
            Weed(weed, new Vector2(0.06f, 0.085f), new Vector2(230f, 860f), new Color(0.06f, 0.42f, 0.36f, 0.9f), 0f);
            Weed(weed, new Vector2(0.16f, 0.085f), new Vector2(170f, 600f), new Color(0.1f, 0.55f, 0.42f, 0.8f), 1.3f);
            Weed(weed, new Vector2(0.94f, 0.085f), new Vector2(230f, 820f), new Color(0.06f, 0.42f, 0.36f, 0.9f), 2.1f);
            Weed(weed, new Vector2(0.84f, 0.085f), new Vector2(160f, 540f), new Color(0.1f, 0.55f, 0.42f, 0.8f), 0.7f);

        }

        void Weed(Sprite s, Vector2 anchor, Vector2 size, Color c, float phase)
        {
            var rt = Img("Seaweed", anchor, anchor, c, s);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            sways.Add(new Sway { rt = rt, amp = 4f, freq = 0.7f, phase = phase });
        }

        void BuildVolcano()
        {
            // Alttan yükselen lav parıltısı
            var glow = Raw("LavaGlow", Vector2.zero, new Vector2(1f, 0.38f), MenuArt.Gradient("lavaGlow", new Color(1f, 0.5f, 0.1f, 0f), new Color(1f, 0.45f, 0.08f, 0.15f), new Color(1f, 0.55f, 0.1f, 0.6f)), Color.white);
            AddPulse(glow, 1f, 0.18f, 0.8f, 0f);
            // Yükselen kıvılcımlar
            AddScroll(Raw("Embers", Vector2.zero, Vector2.one, MenuArt.Dots("ember", 256, 45, 1.2f, 3.5f, false, 71), new Color(1f, 0.62f, 0.22f, 0.9f)), 2.6f, new Vector2(0.008f, -0.06f), 0.04f);
            AddScroll(Raw("EmbersNear", Vector2.zero, new Vector2(1f, 0.6f), MenuArt.Dots("emberB", 256, 14, 2.5f, 6f, false, 72), new Color(1f, 0.8f, 0.35f, 0.85f)), 1.5f, new Vector2(-0.01f, -0.11f), 0.05f);
            // Kenarlardan aşağı akan lav + tırtıklı kaya kenarı
            var lava = MenuArt.Lava();
            var rock = MenuArt.RockEdge();
            AddScroll(Raw("LavaL", new Vector2(0f, 0f), new Vector2(0.1f, 1f), lava, Color.white), 1f, new Vector2(0f, 0.32f), 0f);
            AddScroll(Raw("LavaR", new Vector2(0.9f, 0f), new Vector2(1f, 1f), lava, Color.white), 1f, new Vector2(0f, 0.27f), 0f, true);
            AddScroll(Raw("RockL", new Vector2(0.065f, 0f), new Vector2(0.2f, 1f), rock, Color.white), 1f, Vector2.zero, 0f);
            AddScroll(Raw("RockR", new Vector2(0.8f, 0f), new Vector2(0.935f, 1f), rock, Color.white), 1f, Vector2.zero, 0f, true);

        }

        void BuildSwamp()
        {
            // Yerde sürünen sis (iki katman, ters yönlerde)
            AddScroll(Raw("Fog", new Vector2(0f, 0.02f), new Vector2(1f, 0.6f), MenuArt.Clouds("fog", 256, 128, 0.42f, 0.25f, 81), new Color(0.62f, 0.82f, 0.52f, 0.3f)), 1.1f, new Vector2(0.012f, 0f), 0f, false, true);
            AddScroll(Raw("Fog2", new Vector2(0f, 0.1f), new Vector2(1f, 0.38f), MenuArt.Clouds("fog2", 256, 128, 0.48f, 0.2f, 82), new Color(0.7f, 0.9f, 0.55f, 0.24f)), 1.6f, new Vector2(-0.018f, 0f), 0f, false, true);
            // Sarkan sarmaşıklar
            var vine = MenuArt.Vine();
            float[] xs = { 0.03f, 0.1f, 0.18f, 0.82f, 0.9f, 0.97f };
            float[] hs = { 760f, 520f, 380f, 420f, 600f, 820f };
            for (int i = 0; i < xs.Length; i++)
            {
                var rt = Img("Vine", new Vector2(xs[i], 0.935f), new Vector2(xs[i], 0.935f), i % 2 == 0 ? new Color(0.1f, 0.28f, 0.09f, 0.95f) : new Color(0.16f, 0.36f, 0.12f, 0.9f), vine);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(hs[i] * 0.19f, hs[i]);
                rt.anchoredPosition = Vector2.zero;
                sways.Add(new Sway { rt = rt, amp = 3.5f, freq = 0.5f + 0.1f * i, phase = i * 1.1f });
            }
            // Ateş böcekleri
            var rnd = new System.Random(83);
            for (int i = 0; i < 16; i++)
            {
                var rt = Img("Firefly", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.82f, 1f, 0.42f, 0.9f), UiIcons.Spark);
                float s = 26f + 22f * (float)rnd.NextDouble();
                rt.sizeDelta = new Vector2(s, s);
                flies.Add(new Fly
                {
                    rt = rt, img = rt.GetComponent<Image>(),
                    pos = new Vector2((float)rnd.NextDouble(), 0.1f + 0.75f * (float)rnd.NextDouble()),
                    vel = Random.insideUnitCircle.normalized * 0.02f,
                    phase = (float)rnd.NextDouble() * 10f, speed = 0.6f + (float)rnd.NextDouble()
                });
            }

        }

        void BuildFrost()
        {
            // Yağan kar (iki derinlik)
            AddScroll(Raw("SnowFar", Vector2.zero, Vector2.one, MenuArt.Dots("snowS", 256, 80, 1.2f, 3f, false, 91), new Color(1f, 1f, 1f, 0.7f)), 3f, new Vector2(0.006f, 0.05f), 0.03f);
            AddScroll(Raw("SnowNear", Vector2.zero, Vector2.one, MenuArt.Dots("snowB", 256, 22, 3.5f, 7f, false, 92), new Color(1f, 1f, 1f, 0.9f)), 1.5f, new Vector2(-0.01f, 0.11f), 0.05f);
            // Altta kar yığını
            var drift = Raw("Drift", new Vector2(0f, 0.075f), new Vector2(1f, 0.17f), MenuArt.Dunes("snow", 3, false), new Color(0.9f, 0.96f, 1f, 1f));
            AddScroll(drift, 1.1f, Vector2.zero, 0f);
            // Buzlanmış ekran kenarları
            var frost = MenuArt.FrostSide();
            var l = Raw("FrostL", new Vector2(0f, 0f), new Vector2(0.27f, 1f), frost, Color.white);
            var r = Raw("FrostR", new Vector2(0.73f, 0f), new Vector2(1f, 1f), frost, Color.white);
            r.uvRect = new Rect(1f, 0f, -1f, 1f);
            AddPulse(l, 1f, 0.06f, 0.4f, 0f);
            AddPulse(r, 1f, 0.06f, 0.4f, 1.5f);
            // Üst barın altından sarkan buz sarkıtları
            var ice = Raw("Icicles", new Vector2(0f, 0.855f), new Vector2(1f, 0.935f), MenuArt.Icicles(), Color.white);
            AddScroll(ice, 2.2f, Vector2.zero, 0f);

        }

        void BuildSky()
        {
            AddScroll(Raw("CloudsBack", new Vector2(0f, 0.45f), new Vector2(1f, 1f), MenuArt.Clouds("cloudA", 256, 128, 0.5f, 0.2f, 101), new Color(0.72f, 0.72f, 0.95f, 0.4f)), 1.3f, new Vector2(0.008f, 0f), 0f, false, true);
            AddScroll(Raw("CloudsFront", new Vector2(0f, 0.04f), new Vector2(1f, 0.46f), MenuArt.Clouds("cloudB", 256, 128, 0.45f, 0.22f, 102), new Color(0.88f, 0.88f, 1f, 0.65f)), 1.1f, new Vector2(0.018f, 0f), 0f, false, true);
            // Şimşek: arada bir ekran parlar, bir şimşek çakar
            bolt = Img("Bolt", new Vector2(0.5f, 0.93f), new Vector2(0.5f, 0.93f), new Color(1f, 0.95f, 0.6f, 0f), MenuArt.Bolt(7)).GetComponent<Image>();
            bolt.rectTransform.pivot = new Vector2(0.5f, 1f);
            bolt.rectTransform.sizeDelta = new Vector2(150f, 620f);
            bolt.rectTransform.anchoredPosition = Vector2.zero;
            flash = Img("Flash", Vector2.zero, Vector2.one, new Color(0.85f, 0.85f, 1f, 0f), null).GetComponent<Image>();
            flash.gameObject.SetActive(false);
            boltTimer = 2.5f;

        }

        void BuildDesert()
        {
            // Uzakta piramitli kum tepeleri, önde kum tepeleri
            Raw("FarDunes", new Vector2(0f, 0.12f), new Vector2(1f, 0.38f), MenuArt.Dunes("far", 1, true), new Color(0.7f, 0.38f, 0.24f, 1f));
            var near = Raw("NearDunes", new Vector2(0f, 0.075f), new Vector2(1f, 0.24f), MenuArt.Dunes("near", 2, false), new Color(0.93f, 0.62f, 0.32f, 1f));
            AddScroll(near, 1.2f, Vector2.zero, 0f);
            // Savrulan kum
            AddScroll(Raw("Sand", new Vector2(0f, 0.08f), new Vector2(1f, 0.62f), MenuArt.Streaks("sand", 256, 45, 111), new Color(1f, 0.92f, 0.72f, 0.4f)), 2f, new Vector2(-0.22f, 0f), 0.02f);
        }

        // ------------------------------------------------------------------ amblem süsleri
        void BuildDeco(WorldTheme t)
        {
            switch (t)
            {
                case WorldTheme.Volcano:
                {
            var outer = DecoImg("Flames", 700f, new Color(1f, 0.42f, 0.08f, 0.9f), MenuArt.FlameCrown());
            var inner = DecoImg("FlamesIn", 610f, new Color(1f, 0.82f, 0.3f, 0.9f), MenuArt.FlameCrown());
            pulses.Add(new Pulse { g = outer.GetComponent<Image>(), baseA = 0.9f, amp = 0.08f, freq = 5f, scaleRt = outer, scaleAmp = 0.025f });
            pulses.Add(new Pulse { g = inner.GetComponent<Image>(), baseA = 0.9f, amp = 0.08f, freq = 6.3f, phase = 1f, scaleRt = inner, scaleAmp = 0.03f });
                    break;
                }
                case WorldTheme.Swamp:
                {
            var w = DecoImg("Wreath", 640f, new Color(0.34f, 0.66f, 0.2f, 0.95f), MenuArt.LeafWreath());
            spins.Add(new Spin { rt = w, speed = 3f });
                    break;
                }
                case WorldTheme.Frost:
                {
            var crown = DecoImg("IceCrown", 650f, new Color(0.84f, 0.96f, 1f, 0.9f), MenuArt.IceCrown());
            spins.Add(new Spin { rt = crown, speed = 4f });
            pulses.Add(new Pulse { g = crown.GetComponent<Image>(), baseA = 0.85f, amp = 0.12f, freq = 1.2f });
                    break;
                }
                case WorldTheme.Sky:
                {
            var halo = DecoImg("Halo", 760f, new Color(1f, 0.85f, 0.4f, 0.35f), UiIcons.Spark);
            pulses.Add(new Pulse { g = halo.GetComponent<Image>(), baseA = 0.35f, amp = 0.1f, freq = 1.4f });
            var wing = MenuArt.Wing();
            foreach (int side in new[] { -1, 1 })
            {
                var w = UiKit.Fixed(decoRoot, "Wing", new Vector2(side * 175f, 30f), new Vector2(330f, 248f), new Color(1f, 0.9f, 0.55f, 1f), wing);
                w.GetComponent<Image>().raycastTarget = false;
                w.pivot = new Vector2(0f, 0.62f);
                w.anchoredPosition = new Vector2(side * 175f, 30f);
                w.localScale = new Vector3(side, 1f, 1f);
                sways.Add(new Sway { rt = w, amp = 7f * side, freq = 0.9f, phase = 0f });
            }
                    break;
                }
                case WorldTheme.Desert:
                {
            // Güneş: amblemin arkasında büyük sıcak parıltı + dönen ışınlar
            var sun = DecoImg("Sun", 980f, new Color(1f, 0.85f, 0.45f, 0.55f), UiIcons.Spark);
            pulses.Add(new Pulse { g = sun.GetComponent<Image>(), baseA = 0.55f, amp = 0.1f, freq = 0.8f, scaleRt = sun, scaleAmp = 0.04f });
            var rays = DecoImg("Rays", 680f, new Color(1f, 0.88f, 0.5f, 0.85f), MenuArt.SunRays());
            spins.Add(new Spin { rt = rays, speed = 5f });
                    break;
                }
                default:
                {
            var ring = DecoImg("Bubbles", 640f, new Color(0.65f, 0.95f, 1f, 0.85f), MenuArt.BubbleRing());
            spins.Add(new Spin { rt = ring, speed = 6f });
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ yapı yardımcıları
        RawImage Raw(string name, Vector2 aMin, Vector2 aMax, Texture tex, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            var rt = (RectTransform)go.transform;
            rt.SetParent(layer, false);
            UiKit.Stretch(rt, aMin, aMax);
            var img = go.GetComponent<RawImage>();
            img.texture = tex;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        RectTransform Img(string name, Vector2 aMin, Vector2 aMax, Color c, Sprite s)
        {
            var rt = UiKit.Panel(layer, name, aMin, aMax, c, s);
            rt.GetComponent<Image>().raycastTarget = false;
            return rt;
        }

        RectTransform DecoImg(string name, float size, Color c, Sprite s)
        {
            var rt = UiKit.Fixed(decoRoot, name, Vector2.zero, new Vector2(size, size), c, s);
            rt.GetComponent<Image>().raycastTarget = false;
            return rt;
        }

        void AddScroll(RawImage img, float tiles, Vector2 speed, float wobble, bool flip = false, bool fixedY = false)
        {
            scrolls.Add(new Scroll { img = img, tiles = tiles, speed = speed, wobble = wobble, flip = flip, fixedY = fixedY });
        }

        void AddPulse(Graphic g, float baseA, float amp, float freq, float phase)
        {
            pulses.Add(new Pulse { g = g, baseA = baseA, amp = amp, freq = freq, phase = phase });
        }

        void AddPulse(RectTransform rt, float baseA, float amp, float freq, float phase) => AddPulse(rt.GetComponent<Graphic>(), baseA, amp, freq, phase);

        // ------------------------------------------------------------------ animasyon
        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            // kabı yok edilen süslerin kayıtlarını temizle
            sways.RemoveAll(x => x.rt == null);
            spins.RemoveAll(x => x.rt == null);
            pulses.RemoveAll(x => x.g == null);
            float now = Time.unscaledTime;

            foreach (var s in scrolls)
            {
                if (s.img == null) continue;
                var r = s.img.rectTransform.rect;
                var tex = s.img.texture;
                if (r.width < 1f || tex == null) continue;
                // döşeme: genişlikte 'tiles' kez, yükseklikte dokunun en-boy oranını koruyacak kadar
                // fixedY: dikeyde tek kopya (üst/alt kenarı sönen sis ve bulut katmanları)
                float tilesY = s.fixedY ? 1f : s.tiles * (r.height / r.width) * ((float)tex.width / tex.height);
                s.off += s.speed * dt;
                s.off.x = Mathf.Repeat(s.off.x, 1f);
                s.off.y = Mathf.Repeat(s.off.y, 1f);
                float wob = s.wobble > 0f ? Mathf.Sin(now * 0.7f) * s.wobble : 0f;
                s.img.uvRect = s.flip
                    ? new Rect(s.off.x + wob + s.tiles, s.off.y, -s.tiles, tilesY)
                    : new Rect(s.off.x + wob, s.off.y, s.tiles, tilesY);
            }

            foreach (var w in sways)
                if (w.rt != null) w.rt.localRotation = Quaternion.Euler(0f, 0f, w.baseRot + Mathf.Sin(now * w.freq * Mathf.PI * 2f * 0.5f + w.phase) * w.amp);

            foreach (var s in spins)
                if (s.rt != null) s.rt.localRotation = Quaternion.Euler(0f, 0f, now * s.speed);

            foreach (var p in pulses)
            {
                if (p.g == null) continue;
                float k = Mathf.Sin(now * p.freq * Mathf.PI * 2f * 0.5f + p.phase);
                var c = p.g.color;
                c.a = Mathf.Clamp01(p.baseA + k * p.amp);
                p.g.color = c;
                if (p.scaleRt != null)
                {
                    float sx = Mathf.Sign(p.scaleRt.localScale.x);
                    float sc = 1f + k * p.scaleAmp;
                    p.scaleRt.localScale = new Vector3(sx * sc, sc, 1f);
                }
            }

            if (flies.Count > 0)
            {
                var lr = layer.rect;
                foreach (var f in flies)
                {
                    // yavaşça yön değiştirerek dolaşır, kenarlardan döner, yanıp söner
                    f.vel = (Quaternion.Euler(0f, 0f, Mathf.Sin(now * 0.8f + f.phase) * 90f * dt) * f.vel);
                    f.pos += f.vel * f.speed * dt;
                    if (f.pos.x < 0.03f || f.pos.x > 0.97f) f.vel.x = -f.vel.x;
                    if (f.pos.y < 0.1f || f.pos.y > 0.88f) f.vel.y = -f.vel.y;
                    f.pos = new Vector2(Mathf.Clamp(f.pos.x, 0.03f, 0.97f), Mathf.Clamp(f.pos.y, 0.1f, 0.88f));
                    f.rt.anchoredPosition = new Vector2((f.pos.x - 0.5f) * lr.width, (f.pos.y - 0.5f) * lr.height);
                    float blink = 0.5f + 0.5f * Mathf.Sin(now * 2.2f * f.speed + f.phase);
                    var c = f.img.color;
                    c.a = 0.15f + 0.85f * blink * blink;
                    f.img.color = c;
                }
            }

            if (bolt != null)
            {
                boltTimer -= dt;
                if (boltTimer <= 0f)
                {
                    boltTimer = Random.Range(3.5f, 7f);
                    flashT = 0.45f;
                    var lr = layer.rect;
                    float x = Random.value < 0.5f ? Random.Range(0.08f, 0.3f) : Random.Range(0.7f, 0.92f);
                    bolt.rectTransform.anchorMin = bolt.rectTransform.anchorMax = new Vector2(x, 0.93f);
                    bolt.rectTransform.localScale = new Vector3(Random.value < 0.5f ? -1f : 1f, 1f, 1f);
                    flash.gameObject.SetActive(true);
                }
                if (flashT > 0f)
                {
                    flashT -= dt;
                    float k = Mathf.Clamp01(flashT / 0.45f);
                    // iki kez çakan parlama
                    float f = k > 0.75f ? (k - 0.75f) * 4f : k > 0.45f ? 0f : k > 0.25f ? (k - 0.25f) * 5f * 0.6f : 0f;
                    flash.color = new Color(0.85f, 0.85f, 1f, f * 0.22f);
                    bolt.color = new Color(1f, 0.95f, 0.6f, k > 0.3f ? 1f : k / 0.3f);
                    if (flashT <= 0f) { flash.gameObject.SetActive(false); bolt.color = new Color(1f, 0.95f, 0.6f, 0f); }
                }
            }
        }
    }
}
