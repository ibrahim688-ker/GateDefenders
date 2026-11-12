using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Bataklık dünyasının kodla yapılan canlıları ve mermisi:
    /// Zehir Kurbağası (sürü minyonu, zıplar), Çamur Canavarı (iri düşman), Zehirli Ok (okçu mermisi).
    /// Hepsi +Z'ye bakar; köşe alfası = parıltı (karakter materyalinin _Glow değeriyle).
    /// Okunurluk: koyu, yeşil-kahve bataklık zemininde parlak mavi kurbağa ve açık yosun yeşili sırtlı canavar.
    /// </summary>
    public static class SwampCreatures
    {
        static Color G(Color c) => new Color(c.r, c.g, c.b, 0f);     // tam parlak
        static Color Half(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // ─────────────────────────────────────────────────────
        // ZEHİR KURBAĞASI (boy ~0.8, uzunluk ~1)
        // ─────────────────────────────────────────────────────
        public static Mesh Frog()
        {
            var b = new MeshBuilder();
            var blue = new Color(0.05f, 0.32f, 1f);
            var blueDark = new Color(0.02f, 0.1f, 0.45f);
            var belly = new Color(0.02f, 0.05f, 0.18f);
            var spot = new Color(0.01f, 0.01f, 0.03f);
            var eye = new Color(0.02f, 0.02f, 0.02f);
            var toxic = new Color(0.55f, 1f, 0.15f);

            // Gövde (öne doğru kalkık) + kafa + karın
            b.Sphere(new Vector3(0f, 0.38f, -0.05f), new Vector3(0.4f, 0.28f, 0.46f), blue, 9, 6, new Vector3(-14f, 0f, 0f), true);
            b.Sphere(new Vector3(0f, 0.5f, 0.34f), new Vector3(0.34f, 0.23f, 0.28f), blue, 9, 6, new Vector3(-8f, 0f, 0f), true);
            b.Sphere(new Vector3(0f, 0.27f, 0.08f), new Vector3(0.34f, 0.17f, 0.4f), belly, 8, 5, default, true);
            // Ağız çizgisi + gırtlak kesesi
            b.Box(new Vector3(0f, 0.43f, 0.6f), new Vector3(0.38f, 0.025f, 0.03f), blueDark);
            b.Sphere(new Vector3(0f, 0.34f, 0.52f), new Vector3(0.16f, 0.1f, 0.1f), blueDark, 6, 4, default, true);
            // Göz tümsekleri ve iri siyah gözler (üstte küçük parlak nokta)
            for (int s = -1; s <= 1; s += 2)
            {
                b.Sphere(new Vector3(0.19f * s, 0.66f, 0.36f), new Vector3(0.12f, 0.1f, 0.12f), blue, 7, 5, default, true);
                b.Sphere(new Vector3(0.21f * s, 0.7f, 0.4f), new Vector3(0.09f, 0.09f, 0.09f), eye, 7, 5);
                b.Sphere(new Vector3(0.23f * s, 0.75f, 0.45f), Vector3.one * 0.025f, G(new Color(0.8f, 0.9f, 1f)), 4, 3);
            }
            // Sırtta siyah benekler + zehirli yeşil çizgi
            var spots = new[] { new Vector3(0.12f, 0.64f, 0.02f), new Vector3(-0.15f, 0.61f, -0.1f), new Vector3(0.05f, 0.6f, -0.26f), new Vector3(-0.04f, 0.66f, 0.14f), new Vector3(0.24f, 0.5f, -0.2f), new Vector3(-0.27f, 0.52f, 0.05f), new Vector3(0.17f, 0.56f, 0.24f), new Vector3(-0.2f, 0.58f, 0.26f) };
            foreach (var p in spots) b.Sphere(p, new Vector3(0.07f, 0.03f, 0.07f), spot, 6, 3, default, true);
            b.Box(new Vector3(0f, 0.665f, -0.02f), new Vector3(0.05f, 0.02f, 0.5f), Half(toxic, 0.3f), new Vector3(-12f, 0f, 0f), 0.6f, 1f);

            for (int s = -1; s <= 1; s += 2)
            {
                // Ön bacaklar
                var sh = new Vector3(0.26f * s, 0.34f, 0.32f);
                var hand = new Vector3(0.4f * s, 0.04f, 0.5f);
                b.Cylinder(sh, hand, 0.075f, 0.055f, 6, blue, true, true);
                b.Sphere(hand, new Vector3(0.1f, 0.03f, 0.1f), blueDark, 6, 3, default, true);
                // Arka bacaklar: katlanmış iri uyluk + incik + uzun ayak
                var hip = new Vector3(0.32f * s, 0.3f, -0.3f);
                var knee = new Vector3(0.5f * s, 0.2f, -0.02f);
                var ankle = new Vector3(0.48f * s, 0.05f, -0.34f);
                b.Sphere(Vector3.Lerp(hip, knee, 0.45f), new Vector3(0.14f, 0.13f, 0.24f), blue, 7, 5, new Vector3(0f, 30f * s, 0f), true);
                b.Cylinder(knee, ankle, 0.07f, 0.05f, 6, blue, true, true);
                b.Sphere(ankle + new Vector3(0.02f * s, -0.02f, 0.2f), new Vector3(0.1f, 0.03f, 0.2f), blueDark, 6, 3, default, true);
            }
            return b.ToMesh("PoisonFrog_Mesh");
        }

        // ─────────────────────────────────────────────────────
        // ÇAMUR CANAVARI (boy ~1.05)
        // ─────────────────────────────────────────────────────
        public static Mesh MudBrute()
        {
            var b = new MeshBuilder();
            var mud = new Color(0.3f, 0.2f, 0.12f);
            var mudDark = new Color(0.16f, 0.1f, 0.06f);
            var moss = new Color(0.36f, 0.62f, 0.12f);
            var mossLight = new Color(0.55f, 0.8f, 0.2f);
            var toxic = new Color(0.5f, 1f, 0.2f);
            var cap = new Color(0.75f, 0.25f, 1f);

            // Alt: yere akan çamur yığını, üstte kambur gövde, gömük kafa
            b.Sphere(new Vector3(0f, 0.14f, 0f), new Vector3(0.56f, 0.2f, 0.5f), mudDark, 10, 5, default, true);
            b.Sphere(new Vector3(0f, 0.5f, -0.02f), new Vector3(0.48f, 0.42f, 0.4f), mud, 10, 7, default, true);
            b.Sphere(new Vector3(0f, 0.86f, 0.16f), new Vector3(0.25f, 0.2f, 0.23f), mud, 8, 6, new Vector3(-10f, 0f, 0f), true);
            // Parlayan zehir yeşili gözler + ağız yarığı ve salya
            for (int s = -1; s <= 1; s += 2)
                b.Sphere(new Vector3(0.1f * s, 0.9f, 0.36f), new Vector3(0.055f, 0.045f, 0.04f), G(toxic), 6, 4);
            b.Box(new Vector3(0f, 0.79f, 0.37f), new Vector3(0.22f, 0.045f, 0.04f), new Color(0.04f, 0.03f, 0.02f));
            b.Box(new Vector3(0.04f, 0.68f, 0.38f), new Vector3(0.035f, 0.18f, 0.03f), Half(toxic, 0.2f), default, 0.4f, 1f);
            b.Box(new Vector3(-0.06f, 0.71f, 0.37f), new Vector3(0.03f, 0.12f, 0.03f), Half(toxic, 0.2f), default, 0.4f, 1f);
            // Kalın kollar ve yumruklar
            for (int s = -1; s <= 1; s += 2)
            {
                var sh = new Vector3(0.46f * s, 0.66f, 0.04f);
                var fist = new Vector3(0.6f * s, 0.2f, 0.24f);
                b.Sphere(sh, new Vector3(0.22f, 0.2f, 0.22f), mud, 8, 5, default, true);
                b.Cylinder(sh, fist, 0.16f, 0.19f, 7, mud, true, true);
                b.Sphere(fist, new Vector3(0.21f, 0.19f, 0.21f), mudDark, 8, 5, default, true);
                // Omuzda yosun örtüsü
                b.Sphere(sh + new Vector3(0f, 0.12f, 0f), new Vector3(0.24f, 0.1f, 0.24f), moss, 8, 4, default, true);
            }
            // Sırtta açık yeşil yosun örtüsü (uzaktan okunur siluet)
            b.Sphere(new Vector3(0f, 0.82f, -0.12f), new Vector3(0.4f, 0.14f, 0.34f), moss, 10, 5, new Vector3(12f, 0f, 0f), true);
            b.Sphere(new Vector3(0.1f, 0.94f, -0.05f), new Vector3(0.22f, 0.08f, 0.2f), mossLight, 8, 4, default, true);
            b.Sphere(new Vector3(-0.18f, 0.88f, -0.2f), new Vector3(0.16f, 0.07f, 0.16f), mossLight, 7, 4, default, true);
            // Sırttan çıkan parlak mor mantarlar
            var caps = new[] { new Vector3(-0.2f, 0.95f, -0.18f), new Vector3(0.2f, 0.92f, -0.22f), new Vector3(0.02f, 1.02f, -0.12f) };
            for (int i = 0; i < caps.Length; i++)
            {
                var p = caps[i];
                float r = i == 2 ? 0.1f : 0.075f;
                b.Cylinder(p - Vector3.up * 0.06f, p, 0.02f, 0.018f, 5, new Color(0.85f, 0.8f, 0.9f));
                b.Sphere(p + Vector3.up * 0.02f, new Vector3(r, r * 0.55f, r), G(cap), 7, 4, default, true);
            }
            // Gövdeden akan zehirli çamur damlaları
            b.Box(new Vector3(0.22f, 0.38f, 0.36f), new Vector3(0.05f, 0.2f, 0.03f), Half(toxic, 0.35f), new Vector3(0f, 0f, 6f), 0.4f, 1f);
            b.Box(new Vector3(-0.28f, 0.44f, 0.32f), new Vector3(0.04f, 0.16f, 0.03f), Half(toxic, 0.35f), new Vector3(0f, 0f, -8f), 0.4f, 1f);
            return b.ToMesh("MudBrute_Mesh");
        }

        // ─────────────────────────────────────────────────────
        // ZEHİRLİ OK (uç -Z'de; oyuncuya doğru uçar)
        // ─────────────────────────────────────────────────────
        static Mesh arrow;
        public static Mesh PoisonArrow => arrow != null ? arrow : (arrow = BuildArrow());

        static Mesh BuildArrow()
        {
            var b = new MeshBuilder();
            var shaft = new Color(0.5f, 0.38f, 0.22f);
            var fletch = new Color(0.55f, 0.18f, 0.8f);
            // Sağdaki zehirli mantarların parlak morundan yapılmış uç (koyu bataklıkta ve yeşil sisin içinde net seçilir)
            var toxic = new Color(1f, 0.42f, 1f);
            var hot = new Color(1f, 0.7f, 1f);
            b.Cylinder(new Vector3(0f, 0f, -0.42f), new Vector3(0f, 0f, 0.5f), 0.026f, 0.026f, 5, shaft);
            // Mantar-zehirli uç (parlar) + çevresinde mantar sporu kabuğu + damlayan zehir
            b.Cylinder(new Vector3(0f, 0f, -0.38f), new Vector3(0f, 0f, -0.68f), 0.1f, 0f, 5, G(toxic), true, true);
            b.Sphere(new Vector3(0f, 0f, -0.4f), new Vector3(0.09f, 0.09f, 0.07f), G(hot), 6, 4);
            b.Sphere(new Vector3(0f, -0.07f, -0.48f), new Vector3(0.035f, 0.06f, 0.035f), G(toxic), 5, 3);
            // Tüyler
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f;
                var dir = Quaternion.Euler(0f, 0f, a) * Vector3.up;
                b.Box(new Vector3(0f, 0f, 0.42f) + dir * 0.045f, new Vector3(0.01f, 0.07f, 0.18f), fletch, new Vector3(0f, 0f, a), 1f, 0.5f);
            }
            return b.ToMesh("PoisonArrow");
        }
    }

    /// <summary>Kurbağa zıplaması: periyodik sıçrama + iniş anında basılma (ModelBob yerine).</summary>
    public class HopMotion : MonoBehaviour
    {
        public float height = 0.35f;
        public float rate = 2.6f;
        Vector3 basePos, baseScale;
        float phase;

        void Start()
        {
            basePos = transform.localPosition;
            baseScale = transform.localScale;
            phase = Random.value;
        }

        void Update()
        {
            float t = Mathf.Repeat(Time.time * rate + phase, 1f);
            // 0-0.65 havada (yay), 0.65-1 yerde (basılıp gerilme)
            float y = 0f, sq = 0f;
            if (t < 0.65f) { float k = t / 0.65f; y = 4f * k * (1f - k); sq = -0.08f * Mathf.Sin(k * Mathf.PI); }
            else { float k = (t - 0.65f) / 0.35f; sq = 0.14f * Mathf.Sin(k * Mathf.PI); }
            transform.localPosition = basePos + Vector3.up * y * height;
            transform.localScale = new Vector3(baseScale.x * (1f + sq), baseScale.y * (1f - sq), baseScale.z * (1f + sq * 0.5f));
        }
    }

    /// <summary>
    /// Ayaksız boss (Çürümüş Ent) için süzülme: gövde bataklığa hafifçe gömülü, yavaşça öne yatarak kayar,
    /// hareket ederken önünde çamur/zehir sıçramaları ve halkalar oluşur, gözlerinden yeşil sis yükselir.
    /// Kökler shader'da (_RootWave) kıvranır.
    /// </summary>
    public class SwampGlide : MonoBehaviour
    {
        public float sink = 0.35f;          // dünya biriminde gömülme
        public Vector3 eyeLocal = new Vector3(0f, 0.61f, 0.05f);
        Vector3 basePos;
        Quaternion baseRot;
        Vector3 lastWorld;
        float wakeT, mistT, phase;

        void Start()
        {
            basePos = transform.localPosition - Vector3.up * sink;
            baseRot = transform.localRotation;
            lastWorld = transform.position;
            phase = Random.value * 10f;
        }

        void Update()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float t = Time.time + phase;
            var world = transform.parent != null ? transform.parent.position : transform.position;
            float speed = (lastWorld - world).magnitude / dt;
            lastWorld = world;
            bool moving = speed > 0.2f;

            // Hafif yükselip alçalma + ağır sallanma; ilerlerken öne eğilir
            transform.localPosition = basePos + Vector3.up * (Mathf.Sin(t * 1.3f) * 0.06f);
            float lean = moving ? 5f : 0f;
            transform.localRotation = baseRot * Quaternion.Euler(lean + Mathf.Sin(t * 0.9f) * 1.5f, Mathf.Sin(t * 0.5f) * 3f, Mathf.Sin(t * 0.7f) * 2f);

            float scale = transform.lossyScale.y;
            if (moving)
            {
                wakeT -= dt;
                if (wakeT <= 0f)
                {
                    wakeT = 0.12f;
                    // Önde (oyuncu tarafı: dünya -Z) çamur sıçraması
                    var front = world + new Vector3(Random.Range(-0.35f, 0.35f) * scale, 0.05f, -0.3f * scale);
                    Effects.MudWake(front, scale * 0.4f);
                }
            }
            mistT -= dt;
            if (mistT <= 0f)
            {
                mistT = 0.09f;
                var eye = transform.TransformPoint(eyeLocal + new Vector3(Random.Range(-0.06f, 0.06f), 0f, 0f));
                Effects.ToxicMist(eye);
            }
        }
    }
}
