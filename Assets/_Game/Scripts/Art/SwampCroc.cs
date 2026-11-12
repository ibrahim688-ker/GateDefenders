using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Bataklığın sol tarafındaki timsahlar (dekor): kodla yapılmış düşük poligonlu timsah.
    /// Yüzenler suya gömülü (sırt, gözler, burun dışarıda) yavaşça elips yolda süzülür, kuyruğu kıvrılır, ara ara durur.
    /// Adacıkta güneşlenen timsah ağzını yavaşça açıp kapar. Gövde / çene / kuyruk (2 parça) ayrı parçalardır.
    /// </summary>
    public class SwampCroc : MonoBehaviour
    {
        enum Mode { Swim, Bask }

        static Mesh bodyMesh, jawMesh, tail1Mesh, tail2Mesh;
        static Material crocMat;
        /// <summary>Karakter materyalinin kopyası: mavi kenar ışığı yerine hafif yeşil (suyun içinde mavimsi parlamasın).</summary>
        static Material CrocMaterial
        {
            get
            {
                if (crocMat != null) return crocMat;
                crocMat = new Material(CharacterDesigns.Material) { name = "Croc_Mat" };
                crocMat.SetColor("_RimColor", new Color(0.55f, 0.75f, 0.35f));
                crocMat.SetFloat("_RimStrength", 0.12f);
                return crocMat;
            }
        }

        Mode mode;
        Vector3 center;
        Vector2 radius;
        float speed, angle, phase, y, wakeT;
        Transform jaw, tail1, tail2;

        static readonly Color Back = new Color(0.46f, 0.52f, 0.22f);
        static readonly Color BackDark = new Color(0.17f, 0.21f, 0.08f);
        static readonly Color Belly = new Color(0.58f, 0.52f, 0.28f);
        static readonly Color Tooth = new Color(0.85f, 0.82f, 0.7f);
        static readonly Color Mouth = new Color(0.45f, 0.15f, 0.12f);
        static readonly Color EyeGlow = new Color(1f, 0.8f, 0.15f, 0f);   // karanlıkta parlayan sarı göz

        /// <summary>Suda elips yol boyunca yüzen timsah.</summary>
        public static SwampCroc Swimmer(Transform parent, Vector3 center, Vector2 radius, float speed, float size, int seed)
        {
            var c = Build(parent, size);
            c.mode = Mode.Swim;
            c.center = center;
            c.radius = radius;
            c.speed = speed;
            var r = new System.Random(seed);
            c.angle = (float)r.NextDouble() * 6.28f;
            c.phase = (float)r.NextDouble() * 10f;
            c.y = SwampDesigns.WaterY + 0.06f * size;   // sırtın üst yarısı sudan çıkar
            return c;
        }

        /// <summary>Adacıkta / kıyıda güneşlenen, ağzını açıp kapayan timsah.</summary>
        public static SwampCroc Basker(Transform parent, Vector3 pos, float yaw, float size, int seed)
        {
            var c = Build(parent, size);
            c.mode = Mode.Bask;
            c.transform.position = pos;
            c.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            c.phase = new System.Random(seed).Next(0, 100) * 0.1f;
            return c;
        }

        static SwampCroc Build(Transform parent, float size)
        {
            Ensure();
            var root = new GameObject("Crocodile");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * size;
            var c = root.AddComponent<SwampCroc>();
            Part("Body", bodyMesh, root.transform, Vector3.zero);
            c.jaw = Part("Jaw", jawMesh, root.transform, new Vector3(0f, -0.03f, 0.82f)).transform;
            c.tail1 = Part("Tail1", tail1Mesh, root.transform, new Vector3(0f, 0f, -0.72f)).transform;
            c.tail2 = Part("Tail2", tail2Mesh, c.tail1, new Vector3(0f, 0f, -0.82f)).transform;
            return c;
        }

        static GameObject Part(string name, Mesh m, Transform parent, Vector3 pos)
        {
            var go = CharacterDesigns.MakeVisual(name, m, parent);
            go.transform.localPosition = pos;
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.sharedMaterial = CrocMaterial;
            return go;
        }

        static void Ensure()
        {
            if (bodyMesh != null) return;

            // ── Gövde + kafa + bacaklar (kafa +Z'de) ──
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0f, 0f), new Vector3(0.36f, 0.17f, 0.78f), Back, 10, 6, default, true);
            b.Sphere(new Vector3(0f, -0.06f, 0f), new Vector3(0.33f, 0.1f, 0.72f), Belly, 9, 4, default, true);
            // Sırt pulları (iki sıra)
            for (int i = 0; i < 7; i++)
            {
                float z = -0.6f + i * 0.2f;
                for (int s = -1; s <= 1; s += 2)
                    b.Box(new Vector3(0.1f * s, 0.16f - Mathf.Abs(z) * 0.06f, z), new Vector3(0.07f, 0.06f, 0.12f), BackDark, default, 0.4f, 0.6f);
            }
            // Bacaklar
            for (int s = -1; s <= 1; s += 2)
                for (int f = -1; f <= 1; f += 2)
                {
                    var hip = new Vector3(0.28f * s, -0.04f, 0.45f * f);
                    var foot = new Vector3(0.52f * s, -0.16f, 0.5f * f + 0.12f);
                    b.Cylinder(hip, foot, 0.08f, 0.06f, 5, Back, true, true);
                    b.Sphere(foot, new Vector3(0.09f, 0.03f, 0.1f), BackDark, 5, 3, default, true);
                }
            // Kafa (üst çene) + uzun burun
            b.Sphere(new Vector3(0f, 0.02f, 0.88f), new Vector3(0.25f, 0.12f, 0.24f), Back, 8, 5, default, true);
            b.Box(new Vector3(0f, 0.0f, 1.32f), new Vector3(0.28f, 0.1f, 0.66f), Back, default, 0.65f, 0.8f);
            b.Box(new Vector3(0f, 0.055f, 1.62f), new Vector3(0.12f, 0.05f, 0.08f), BackDark);          // burun delikleri tümseği
            // Göz tümsekleri + parlayan sarı gözler
            for (int s = -1; s <= 1; s += 2)
            {
                b.Sphere(new Vector3(0.11f * s, 0.12f, 0.92f), new Vector3(0.07f, 0.06f, 0.08f), Back, 6, 4, default, true);
                b.Sphere(new Vector3(0.12f * s, 0.15f, 0.97f), new Vector3(0.035f, 0.03f, 0.03f), EyeGlow, 5, 3);
                // Üst çene dişleri
                for (int k = 0; k < 4; k++)
                    b.Box(new Vector3(0.13f * s, -0.06f, 1.08f + k * 0.13f), new Vector3(0.025f, 0.05f, 0.025f), Tooth, default, 0.2f, 0.2f);
            }
            bodyMesh = b.ToMesh("Croc_Body");

            // ── Alt çene (pivot kafanın arkasında; X ekseninde açılır) ──
            b = new MeshBuilder();
            b.Box(new Vector3(0f, -0.04f, 0.42f), new Vector3(0.26f, 0.06f, 0.78f), Belly, default, 0.7f, 0.8f);
            b.Box(new Vector3(0f, -0.005f, 0.4f), new Vector3(0.2f, 0.01f, 0.66f), Mouth);
            for (int s = -1; s <= 1; s += 2)
                for (int k = 0; k < 4; k++)
                    b.Box(new Vector3(0.11f * s, 0.02f, 0.3f + k * 0.13f), new Vector3(0.022f, 0.045f, 0.022f), Tooth, default, 0.2f, 0.2f);
            jawMesh = b.ToMesh("Croc_Jaw");

            // ── Kuyruk (iki parça, uç -Z'de) ──
            b = new MeshBuilder();
            b.Box(new Vector3(0f, -0.01f, -0.42f), new Vector3(0.36f, 0.16f, 0.9f), Back, default, 0.6f, 0.7f);
            for (int i = 0; i < 4; i++)
                b.Box(new Vector3(0f, 0.08f - i * 0.01f, -0.12f - i * 0.2f), new Vector3(0.05f, 0.07f, 0.12f), BackDark, default, 0.3f, 0.6f);
            tail1Mesh = b.ToMesh("Croc_Tail1");
            b = new MeshBuilder();
            b.Box(new Vector3(0f, -0.01f, -0.4f), new Vector3(0.22f, 0.1f, 0.84f), Back, default, 0.2f, 0.4f);
            for (int i = 0; i < 4; i++)
                b.Box(new Vector3(0f, 0.05f - i * 0.008f, -0.1f - i * 0.18f), new Vector3(0.04f, 0.06f, 0.1f), BackDark, default, 0.3f, 0.6f);
            tail2Mesh = b.ToMesh("Croc_Tail2");
        }

        void Update()
        {
            float t = Time.time + phase;
            if (mode == Mode.Swim)
            {
                // Hız zamanla dalgalanır: ara ara neredeyse durur
                float pace = Mathf.Clamp01(0.55f + 0.6f * Mathf.Sin(t * 0.35f));
                angle += speed * pace * Time.deltaTime;
                var p = center + new Vector3(Mathf.Cos(angle) * radius.x, 0f, Mathf.Sin(angle) * radius.y);
                p.y = y + Mathf.Sin(t * 1.1f) * 0.025f;
                var tangent = new Vector3(-Mathf.Sin(angle) * radius.x, 0f, Mathf.Cos(angle) * radius.y) * Mathf.Sign(speed);
                transform.position = p;
                if (tangent.sqrMagnitude > 1e-4f)
                    transform.rotation = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0f, Mathf.Sin(t * 2.2f) * 6f * pace, 0f);
                float wag = 0.3f + pace;
                tail1.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 2.2f) * 18f * wag, 0f);
                tail2.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 2.2f - 0.9f) * 28f * wag, 0f);
                jaw.localRotation = Quaternion.identity;
                // Yüzerken burnunun önünde küçük su halkaları (hareket uzaktan fark edilsin)
                wakeT -= Time.deltaTime;
                if (wakeT <= 0f && pace > 0.3f)
                {
                    wakeT = 0.45f;
                    Effects.MudWake(transform.TransformPoint(new Vector3(0f, 0f, 1.5f)) + Vector3.up * 0.05f, 0.35f * transform.localScale.x);
                }
            }
            else
            {
                // Güneşlenen: ağız yavaşça açılır, bekler, kapanır; kuyruk ağır ağır kıpırdar
                float cyc = Mathf.Repeat(t / 7f, 1f);
                float open = cyc < 0.2f ? cyc / 0.2f : cyc < 0.55f ? 1f : cyc < 0.7f ? 1f - (cyc - 0.55f) / 0.15f : 0f;
                jaw.localRotation = Quaternion.Euler(Mathf.SmoothStep(0f, 1f, open) * 28f, 0f, 0f);
                tail1.localRotation = Quaternion.Euler(0f, 12f + Mathf.Sin(t * 0.6f) * 6f, 0f);
                tail2.localRotation = Quaternion.Euler(0f, 18f + Mathf.Sin(t * 0.6f - 0.7f) * 10f, 0f);
            }
        }
    }
}
