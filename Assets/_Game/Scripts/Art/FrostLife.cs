using UnityEngine;
using UnityEngine.UI;

namespace StratejiGame
{
    /// <summary>
    /// Kar atmosferi: tüm alana yağan kar, uçurumdan yükselip savrulan kar fırtınası, buz kristallerinde pırıltılar.
    /// </summary>
    public static class FrostLife
    {
        static Mesh dotMesh;
        static Mesh DotMesh
        {
            get
            {
                if (dotMesh != null) return dotMesh;
                var b = new MeshBuilder();
                b.Sphere(Vector3.zero, Vector3.one * 0.5f, Color.white, 5, 3, default, true);
                dotMesh = b.ToMesh("SnowDot");
                return dotMesh;
            }
        }

        public static void Build(Transform env, float hw, float nearZ, float gateZ)
        {
            var root = new GameObject("FrostLife").transform;
            root.SetParent(env, false);
            float midZ = (nearZ + gateZ) * 0.5f, len = gateZ - nearZ;

            // Yağan kar (rüzgârla hafif sağa savrulur)
            {
                var ps = MakeSystem(root, "Snowfall", new Vector3(0f, 10f, midZ + 4f), new Vector3(30f, 1f, len + 14f));
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 7f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
                main.startColor = new Color(0.75f, 0.8f, 0.9f);
                main.maxParticles = 320;
                var em = ps.emission;
                em.rateOverTime = 40f;
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
                vel.y = new ParticleSystem.MinMaxCurve(-2.2f, -1.4f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.2f);
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 0.4f;
                noise.frequency = 0.5f;
                noise.quality = ParticleSystemNoiseQuality.Low;
                ps.Play();
            }
            // Uçurumdan savrulan kar fırtınası (hızlı, yoğun, soldan)
            {
                var ps = MakeSystem(root, "Blizzard", new Vector3(-12f, 1.5f, midZ), new Vector3(6f, 7f, len + 10f));
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
                main.startColor = new Color(0.8f, 0.85f, 0.95f);
                main.maxParticles = 260;
                var em = ps.emission;
                em.rateOverTime = 60f;
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(3f, 6f);
                vel.y = new ParticleSystem.MinMaxCurve(0.4f, 1.6f);
                vel.z = new ParticleSystem.MinMaxCurve(-1.2f, 0.4f);
                var col = ps.colorOverLifetime;
                col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
                col.color = g;
                ps.Play();
            }
            // Buz kristallerinde pırıltılar (sağ taraf)
            {
                var ps = MakeSystem(root, "IceTwinkles", new Vector3(hw + 3f, 2.2f, midZ), new Vector3(5f, 4f, len));
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
                main.startColor = new Color(0.6f, 0.9f, 1f);
                main.maxParticles = 40;
                var em = ps.emission;
                em.rateOverTime = 14f;
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
                ps.Play();
            }
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

    /// <summary>
    /// Ekran kenarlarında buzlanma: kodla üretilen, köşelerde yoğunlaşan, kristal çizgili yarı saydam doku.
    /// HUD'un arkasında (düşük sıralama) tam ekran, tıklamaları engellemez.
    /// </summary>
    public class FrostVignette : MonoBehaviour
    {
        static Texture2D tex;

        public static void Create(Transform parent)
        {
            var go = new GameObject("FrostVignette");
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -5;
            var imgGo = new GameObject("Frost");
            imgGo.transform.SetParent(go.transform, false);
            var img = imgGo.AddComponent<RawImage>();
            img.texture = Texture;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.AddComponent<FrostVignette>();
        }

        static Texture2D Texture
        {
            get
            {
                if (tex != null) return tex;
                const int W = 128, H = 256;
                var px = new Color32[W * H];
                var rnd = new System.Random(3);
                var edge = new float[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float u = (x + 0.5f) / W, v = (y + 0.5f) / H;
                        float dx = Mathf.Min(u, 1f - u), dy = Mathf.Min(v, 1f - v) * 0.5f;
                        float d = Mathf.Min(dx, dy);
                        float corner = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 5f);
                        float n = Mathf.PerlinNoise(u * 9f, v * 18f) * 0.06f;
                        float k = Mathf.Clamp01((d + n) / (0.07f + corner * 0.06f));
                        k = k * k * (3f - 2f * k);   // gerçek smoothstep (Mathf.SmoothStep bir ara değerleme fonksiyonu)
                        float a = (1f - k) * 0.42f + corner * 0.12f;
                        edge[y * W + x] = Mathf.Clamp01(a);
                    }
                // Kenarlardan içeri uzanan kristal çizgiler
                for (int k = 0; k < 70; k++)
                {
                    int side = rnd.Next(4);
                    float px0 = side == 0 ? 0f : side == 1 ? W - 1 : (float)rnd.NextDouble() * W;
                    float py0 = side == 2 ? 0f : side == 3 ? H - 1 : (float)rnd.NextDouble() * H;
                    float ang = side == 0 ? 0f : side == 1 ? Mathf.PI : side == 2 ? Mathf.PI * 0.5f : -Mathf.PI * 0.5f;
                    ang += ((float)rnd.NextDouble() - 0.5f) * 1.2f;
                    int steps = 6 + rnd.Next(14);
                    for (int s = 0; s < steps; s++)
                    {
                        int ix = Mathf.RoundToInt(px0), iy = Mathf.RoundToInt(py0);
                        if (ix >= 0 && ix < W && iy >= 0 && iy < H) edge[iy * W + ix] = Mathf.Clamp01(edge[iy * W + ix] + 0.25f * (1f - s / (float)steps));
                        px0 += Mathf.Cos(ang); py0 += Mathf.Sin(ang);
                        if (s % 4 == 3) ang += ((float)rnd.NextDouble() - 0.5f) * 0.9f;
                    }
                }
                for (int i = 0; i < px.Length; i++)
                    px[i] = new Color32(225, 240, 255, (byte)(edge[i] * 255f));
                tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "FrostVignette" };
                tex.SetPixels32(px);
                tex.Apply(false, true);
                return tex;
            }
        }
    }
}
