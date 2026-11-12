using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Dondurucu Zirveler'in kodla yapılan canlıları ve efekt mesh'leri:
    /// Kurt Yavrusu (sürü minyonu, dörtnala koşar), Kardan Dev (iri düşman), Buz Kristali (Ayaz Muhafızı mermisi),
    /// Buz Kalıbı (donan asker), Buz Dikeni (Yeti'nin Buzul Gürzü saldırısı).
    /// Okunurluk: beyaz karda koyu kömür grisi kurtlar; kardan devde kırmızı atkı, koyu kova şapka, kömür gözler, koyu dal kollar.
    /// </summary>
    public static class FrostCreatures
    {
        static Color G(Color c) => new Color(c.r, c.g, c.b, 0f);
        static Color Half(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // ─────────────────────────────────────────────────────
        // KURT YAVRUSU (uzunluk ~1.1, boy ~0.85)
        // ─────────────────────────────────────────────────────
        public static Mesh WolfPup()
        {
            var b = new MeshBuilder();
            var fur = new Color(0.17f, 0.17f, 0.21f);
            var furDark = new Color(0.08f, 0.08f, 0.1f);
            var chest = new Color(0.55f, 0.57f, 0.63f);
            var eye = G(new Color(0.45f, 0.9f, 1f));

            b.Sphere(new Vector3(0f, 0.42f, -0.06f), new Vector3(0.24f, 0.21f, 0.42f), fur, 9, 6, new Vector3(-6f, 0f, 0f), true);
            b.Box(new Vector3(0f, 0.62f, -0.08f), new Vector3(0.1f, 0.05f, 0.6f), furDark, new Vector3(-6f, 0f, 0f), 0.6f, 0.9f);
            b.Sphere(new Vector3(0f, 0.38f, 0.22f), new Vector3(0.19f, 0.2f, 0.17f), chest, 8, 5, default, true);
            // Kafa + burun + kulaklar
            b.Sphere(new Vector3(0f, 0.62f, 0.4f), new Vector3(0.19f, 0.17f, 0.19f), fur, 8, 6, default, true);
            b.Box(new Vector3(0f, 0.56f, 0.6f), new Vector3(0.14f, 0.11f, 0.24f), fur, new Vector3(8f, 0f, 0f), 0.7f, 0.8f);
            b.Box(new Vector3(0f, 0.52f, 0.62f), new Vector3(0.12f, 0.05f, 0.2f), chest, new Vector3(8f, 0f, 0f), 0.8f, 0.8f);
            b.Sphere(new Vector3(0f, 0.59f, 0.73f), new Vector3(0.04f, 0.035f, 0.03f), furDark, 5, 3);
            for (int s = -1; s <= 1; s += 2)
            {
                b.Box(new Vector3(0.1f * s, 0.8f, 0.36f), new Vector3(0.09f, 0.15f, 0.05f), fur, new Vector3(-8f, 0f, 12f * s), 0.1f, 0.3f);
                b.Box(new Vector3(0.1f * s, 0.79f, 0.375f), new Vector3(0.05f, 0.1f, 0.02f), new Color(0.55f, 0.35f, 0.38f), new Vector3(-8f, 0f, 12f * s), 0.1f, 0.3f);
                b.Sphere(new Vector3(0.085f * s, 0.66f, 0.56f), new Vector3(0.032f, 0.028f, 0.02f), eye, 5, 3);
                // Bacaklar + açık renk patiler
                for (int f = -1; f <= 1; f += 2)
                {
                    var top = new Vector3(0.13f * s, 0.36f, 0.26f * f - 0.04f);
                    var foot = new Vector3(0.14f * s, 0.04f, 0.28f * f - 0.02f);
                    b.Cylinder(top, foot, 0.065f, 0.045f, 5, fur, true, true);
                    b.Sphere(foot, new Vector3(0.055f, 0.03f, 0.07f), chest, 5, 3, default, true);
                }
            }
            // Gür kuyruk (ucu beyaz)
            b.Cylinder(new Vector3(0f, 0.5f, -0.42f), new Vector3(0f, 0.6f, -0.72f), 0.08f, 0.05f, 6, fur, true, true);
            b.Sphere(new Vector3(0f, 0.61f, -0.76f), new Vector3(0.06f, 0.06f, 0.08f), chest, 5, 3, default, true);
            return b.ToMesh("WolfPup_Mesh");
        }

        // ─────────────────────────────────────────────────────
        // KARDAN DEV (boy ~1.45)
        // ─────────────────────────────────────────────────────
        public static Mesh SnowGiant()
        {
            var b = new MeshBuilder();
            var snow = new Color(0.8f, 0.86f, 0.96f);
            var snowShade = new Color(0.62f, 0.7f, 0.86f);
            var coal = new Color(0.04f, 0.04f, 0.05f);
            var carrot = new Color(1f, 0.42f, 0.08f);
            var scarf = new Color(0.8f, 0.12f, 0.1f);
            var scarfDark = new Color(0.5f, 0.06f, 0.06f);
            var branch = new Color(0.22f, 0.14f, 0.08f);
            var bucket = new Color(0.3f, 0.32f, 0.36f);
            var ice = G(new Color(0.5f, 0.85f, 1f));

            b.Sphere(new Vector3(0f, 0.34f, 0f), new Vector3(0.52f, 0.37f, 0.48f), snow, 10, 7, default, true);
            b.Sphere(new Vector3(0f, 0.12f, 0f), new Vector3(0.56f, 0.14f, 0.52f), snowShade, 10, 4, default, true);
            b.Sphere(new Vector3(0f, 0.8f, 0.02f), new Vector3(0.41f, 0.33f, 0.37f), snow, 10, 7, default, true);
            b.Sphere(new Vector3(0f, 1.15f, 0.05f), new Vector3(0.28f, 0.25f, 0.26f), snow, 9, 6, default, true);
            // Kömür gözler + çatık kaşlar + asık ağız
            for (int s = -1; s <= 1; s += 2)
            {
                b.Sphere(new Vector3(0.1f * s, 1.2f, 0.28f), Vector3.one * 0.05f, coal, 5, 4);
                b.Box(new Vector3(0.1f * s, 1.28f, 0.27f), new Vector3(0.14f, 0.035f, 0.04f), coal, new Vector3(0f, 0f, -22f * s));
            }
            for (int k = -2; k <= 2; k++)
                b.Sphere(new Vector3(k * 0.06f, 1.04f + Mathf.Abs(k) * -0.015f + 0.02f, 0.28f - Mathf.Abs(k) * 0.02f), Vector3.one * 0.025f, coal, 4, 3);
            // Havuç burun
            b.Cylinder(new Vector3(0f, 1.14f, 0.28f), new Vector3(0f, 1.1f, 0.52f), 0.05f, 0f, 6, carrot, true, true);
            // Kömür düğmeler
            for (int k = 0; k < 3; k++) b.Sphere(new Vector3(0f, 0.68f + k * 0.11f, 0.38f - Mathf.Abs(k - 1) * 0.02f), Vector3.one * 0.045f, coal, 5, 4);
            // Kırmızı atkı + sarkan ucu
            b.Cylinder(new Vector3(0f, 0.95f, 0.03f), new Vector3(0f, 1.03f, 0.04f), 0.33f, 0.3f, 10, scarf, true, true);
            b.Box(new Vector3(0.16f, 0.82f, 0.33f), new Vector3(0.12f, 0.28f, 0.04f), scarf, new Vector3(-10f, 0f, 8f), 1f, 1f);
            b.Box(new Vector3(0.17f, 0.7f, 0.34f), new Vector3(0.12f, 0.04f, 0.045f), scarfDark, new Vector3(-10f, 0f, 8f));
            // Eğik kova şapka
            b.Cylinder(new Vector3(0f, 1.33f, 0.03f), new Vector3(0.03f, 1.55f, 0f), 0.22f, 0.19f, 9, bucket, true, true);
            b.Cylinder(new Vector3(0f, 1.33f, 0.03f), new Vector3(0f, 1.36f, 0.03f), 0.25f, 0.25f, 9, new Color(0.2f, 0.21f, 0.24f), true, true);
            // Kalın dal kollar + parmak dalları
            for (int s = -1; s <= 1; s += 2)
            {
                var sh = new Vector3(0.36f * s, 0.86f, 0f);
                var el = new Vector3(0.62f * s, 0.7f, 0.12f);
                var hand = new Vector3(0.74f * s, 0.42f, 0.28f);
                b.Cylinder(sh, el, 0.07f, 0.06f, 5, branch, true, true);
                b.Cylinder(el, hand, 0.06f, 0.045f, 5, branch, true, true);
                for (int k = 0; k < 3; k++)
                {
                    var tip = hand + new Vector3((0.08f + k * 0.04f) * s, -0.1f + k * 0.08f, 0.1f);
                    b.Cylinder(hand, tip, 0.03f, 0.01f, 4, branch, true, true);
                }
            }
            // Sırtta birkaç parlayan buz kristali
            b.Cylinder(new Vector3(0.15f, 0.95f, -0.3f), new Vector3(0.25f, 1.2f, -0.4f), 0.06f, 0f, 5, ice, true, true);
            b.Cylinder(new Vector3(-0.12f, 0.9f, -0.32f), new Vector3(-0.2f, 1.1f, -0.44f), 0.05f, 0f, 5, ice, true, true);
            b.Cylinder(new Vector3(0.02f, 0.62f, -0.4f), new Vector3(0.05f, 0.82f, -0.55f), 0.05f, 0f, 5, ice, true, true);
            return b.ToMesh("SnowGiant_Mesh");
        }

        // ─────────────────────────────────────────────────────
        // BUZ KRİSTALİ (uç -Z'de)
        // ─────────────────────────────────────────────────────
        static Mesh shard;
        public static Mesh IceShard => shard != null ? shard : (shard = BuildShard());

        static Mesh BuildShard()
        {
            var b = new MeshBuilder();
            var ice = G(new Color(0.5f, 0.85f, 1f));
            var core = G(new Color(0.9f, 1f, 1f));
            b.Cylinder(new Vector3(0f, 0f, 0.3f), new Vector3(0f, 0f, -0.55f), 0.12f, 0f, 5, ice, true, true);
            b.Cylinder(new Vector3(0f, 0f, 0.3f), new Vector3(0f, 0f, 0.42f), 0.12f, 0.03f, 5, ice, true, true);
            b.Sphere(new Vector3(0f, 0f, 0f), new Vector3(0.07f, 0.07f, 0.16f), core, 6, 4);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                b.Cylinder(d * 0.06f + Vector3.forward * 0.15f, d * 0.2f - Vector3.forward * 0.12f, 0.05f, 0f, 4, ice, true, true);
            }
            return b.ToMesh("IceShard");
        }

        // ─────────────────────────────────────────────────────
        // BUZ KALIBI (donan askerin etrafında, eklemeli materyal)
        // ─────────────────────────────────────────────────────
        static Mesh block;
        public static Mesh IceBlock => block != null ? block : (block = BuildBlock());

        static Mesh BuildBlock()
        {
            var b = new MeshBuilder();
            var c = new Color(0.18f, 0.38f, 0.55f);
            var edge = new Color(0.4f, 0.7f, 0.9f);
            b.Box(new Vector3(0f, 0.55f, 0f), new Vector3(0.62f, 1.1f, 0.62f), c, new Vector3(0f, 12f, 0f), 0.9f, 0.9f);
            b.Cylinder(new Vector3(0.2f, 0.9f, -0.2f), new Vector3(0.32f, 1.35f, -0.3f), 0.1f, 0f, 5, edge, true, true);
            b.Cylinder(new Vector3(-0.2f, 0.7f, 0.15f), new Vector3(-0.36f, 1.15f, 0.25f), 0.09f, 0f, 5, edge, true, true);
            b.Cylinder(new Vector3(0f, 1.05f, 0f), new Vector3(0.05f, 1.4f, 0.02f), 0.08f, 0f, 5, edge, true, true);
            return b.ToMesh("Fx_IceBlock");
        }

        // ─────────────────────────────────────────────────────
        // BUZ DİKENİ KÜMESİ (Buzul Gürzü) — taban yarıçapı ~1
        // ─────────────────────────────────────────────────────
        public static Mesh IceSpikeMesh()
        {
            var b = new MeshBuilder();
            var snow = new Color(0.78f, 0.84f, 0.95f);
            var ice = new Color(0.45f, 0.8f, 1f, 0.4f);
            var iceBright = new Color(0.75f, 0.95f, 1f, 0.2f);
            b.Sphere(new Vector3(0f, 0.02f, 0f), new Vector3(1.05f, 0.2f, 1.05f), snow, 10, 4, default, true);
            var rnd = new System.Random(7);
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + (float)rnd.NextDouble() * 25f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float h = i == 0 ? 2.6f : 1.2f + (float)rnd.NextDouble() * 0.9f;
                var b0 = dir * (i == 0 ? 0.05f : 0.42f);
                var tip = b0 + dir * (i == 0 ? 0.15f : 0.5f) + Vector3.up * h;
                float r = i == 0 ? 0.32f : 0.2f;
                b.Cylinder(b0 - Vector3.up * 0.1f, tip, r, 0f, 6, i % 2 == 0 ? iceBright : ice, true, true);
            }
            return b.ToMesh("Fx_IceSpike");
        }
    }

    /// <summary>Dörtnala koşma: ritmik sıçrama + öne arkaya yunuslama (kurt yavruları).</summary>
    public class GallopMotion : MonoBehaviour
    {
        public float rate = 7f;
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
            float t = Time.time * rate + phase;
            transform.localPosition = basePos + Vector3.up * Mathf.Abs(Mathf.Sin(t)) * 0.12f;
            transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t * 2f) * 7f, 0f, 0f);
        }
    }
}
