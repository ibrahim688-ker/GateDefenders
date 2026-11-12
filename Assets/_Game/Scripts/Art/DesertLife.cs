using UnityEngine;
using UnityEngine.UI;

namespace StratejiGame
{
    /// <summary>
    /// Çöl atmosferi: yerden savrulan kum (soldan rüzgâr), piramitlerin tepesinden uçuşan kum tülleri,
    /// ara ara yükselen küçük kum girdapları, havada altın toz zerreleri.
    /// </summary>
    public static class DesertLife
    {
        static Mesh dotMesh;
        static Mesh DotMesh
        {
            get
            {
                if (dotMesh != null) return dotMesh;
                var b = new MeshBuilder();
                b.Sphere(Vector3.zero, Vector3.one * 0.5f, Color.white, 5, 3, default, true);
                dotMesh = b.ToMesh("SandDot");
                return dotMesh;
            }
        }

        public static void Build(Transform env, float hw, float nearZ, float gateZ)
        {
            var root = new GameObject("DesertLife").transform;
            root.SetParent(env, false);
            float midZ = (nearZ + gateZ) * 0.5f, len = gateZ - nearZ;

            // Yere yakın savrulan kum (soldan sağa, yolun üstünden geçer)
            {
                var ps = MakeSystem(root, "BlowingSand", new Vector3(-4f, 0.25f, midZ), new Vector3(10f, 0.4f, len + 10f));
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
                main.startColor = new Color(0.55f, 0.42f, 0.25f);
                main.maxParticles = 220;
                var em = ps.emission;
                em.rateOverTime = 70f;
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(4f, 7f);
                vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.3f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.3f);
                FadeInOut(ps);
                ps.Play();
            }
            // Piramitlerden ve kum tepelerinden savrulan kum tülleri (büyük, yumuşak)
            {
                var ps = MakeSystem(root, "DuneVeils", new Vector3(-14f, 3f, midZ), new Vector3(14f, 4f, len + 10f));
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
                main.startColor = new Color(0.32f, 0.24f, 0.14f);
                main.maxParticles = 60;
                var em = ps.emission;
                em.rateOverTime = 14f;
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(2f, 4f);
                vel.y = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
                FadeInOut(ps);
                ps.Play();
            }
            // Altın toz zerreleri (sağda, dikilitaşların çevresinde)
            {
                var ps = MakeSystem(root, "GoldDust", new Vector3(hw + 3f, 2f, midZ), new Vector3(6f, 3f, len));
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
                main.startColor = new Color(1f, 0.75f, 0.35f);
                main.maxParticles = 40;
                var em = ps.emission;
                em.rateOverTime = 12f;
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
                vel.y = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
                ps.Play();
            }
            root.gameObject.AddComponent<DustDevils>().Init(hw, nearZ, gateZ);
        }

        static void FadeInOut(ParticleSystem ps)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        static ParticleSystem MakeSystem(Transform root, string name, Vector3 center, Vector3 box)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = center;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = DotMesh;
            r.sharedMaterial = FxDesigns.FxMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }
    }

    /// <summary>Ara ara yolun iki yanında yükselip dönen küçük kum girdapları.</summary>
    public class DustDevils : MonoBehaviour
    {
        float hw, nearZ, gateZ, t = 2f;
        Vector3 pos;
        float life, angle;

        public void Init(float halfW, float near, float gate) { hw = halfW; nearZ = near; gateZ = gate; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (life > 0f)
            {
                life -= dt;
                angle += dt * 9f;
                pos.x += dt * 0.8f;
                for (int i = 0; i < 2; i++)
                {
                    float h = Random.Range(0f, 2.2f);
                    float r = 0.15f + h * 0.35f;
                    var p = pos + new Vector3(Mathf.Cos(angle + i * 3.14f + h * 2f) * r, h, Mathf.Sin(angle + i * 3.14f + h * 2f) * r);
                    Effects.SandPuff(p, 0.5f);
                }
                return;
            }
            t -= dt;
            if (t <= 0f)
            {
                t = Random.Range(3f, 7f);
                life = Random.Range(1.5f, 2.5f);
                float side = Random.value < 0.5f ? -1f : 1f;
                pos = new Vector3(side * (hw + Random.Range(1.5f, 4f)), 0f, Random.Range(nearZ + 8f, gateZ - 6f));
                pos.y = DesertDesigns.GroundHeight(pos.x, pos.z);
            }
        }
    }

    /// <summary>
    /// Serap / sıcaklık dalgalanması: ekran kenarlarında, yukarı doğru kayan, dalgalı, yarı saydam sıcak ışık bantları.
    /// HUD'un arkasında, tıklamaları engellemez.
    /// </summary>
    public class DesertHeat : MonoBehaviour
    {
        static Texture2D tex;
        RawImage[] imgs;

        public static void Create(Transform parent)
        {
            var go = new GameObject("DesertHeat");
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -5;
            // Performans: tam ekran yerine yalnız sol ve sağ kenarda iki dar şerit (ortadaki boş pikseller çizilmez)
            var heat = go.AddComponent<DesertHeat>();
            heat.imgs = new RawImage[2];
            for (int i = 0; i < 2; i++)
            {
                var imgGo = new GameObject(i == 0 ? "HeatL" : "HeatR");
                imgGo.transform.SetParent(go.transform, false);
                var img = imgGo.AddComponent<RawImage>();
                img.texture = Texture;
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = new Vector2(i == 0 ? 0f : 0.82f, 0f);
                rt.anchorMax = new Vector2(i == 0 ? 0.18f : 1f, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                heat.imgs[i] = img;
            }
        }

        void Update()
        {
            if (imgs == null) return;
            float t = Time.time;
            for (int i = 0; i < imgs.Length; i++)
            {
                // Dokunun yalnız ilgili kenar yarısını göster (sol şerit: u 0–0.18, sağ şerit: u 0.82–1)
                float u0 = i == 0 ? 0f : 0.82f;
                imgs[i].uvRect = new Rect(u0 + Mathf.Sin(t * 0.7f) * 0.004f, -t * 0.05f, 0.18f, 1f);
                imgs[i].color = new Color(1f, 1f, 1f, 0.85f + Mathf.Sin(t * 1.3f) * 0.15f);
            }
        }

        static Texture2D Texture
        {
            get
            {
                if (tex != null) return tex;
                const int W = 128, H = 256;
                var px = new Color32[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float u = (x + 0.5f) / W, v = (y + 0.5f) / H;
                        // Yalnız sol/sağ kenarlar (dikey kaydırma döşemeyi bozmasın)
                        float d = Mathf.Min(u, 1f - u);
                        float k = Mathf.Clamp01(d / 0.18f);
                        k = k * k * (3f - 2f * k);   // gerçek smoothstep
                        // Dalgalı, dikeyde tekrar eden ince sıcak hava bantları (doku dikey olarak döşenebilir)
                        float wave = 0.5f + 0.5f * Mathf.Sin(v * Mathf.PI * 2f * 10f + Mathf.Sin(u * 18f) * 1.4f);
                        float a = (1f - k) * (0.1f + 0.1f * wave);
                        px[y * W + x] = new Color32(255, 214, 150, (byte)(Mathf.Clamp01(a) * 255f));
                    }
                tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "DesertHeat" };
                tex.SetPixels32(px);
                tex.Apply(false, true);
                return tex;
            }
        }
    }
}
