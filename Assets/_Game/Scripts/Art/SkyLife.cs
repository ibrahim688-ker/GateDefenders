using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Gökyüzü atmosferi: aşağıda ve uzakta süzülen bulutlar, bulut denizinde çakan mavi şimşekler,
    /// uzak gökyüzünde dev yıldırımlar (sahne bir an aydınlanır + gök gürültüsü), kapı küreleri arasında sürekli ark,
    /// sütunlardaki şimşek iletkenleri arasında ara ara elektrik akımı, yükselen elektrik/altın zerreleri.
    /// </summary>
    public static class SkyLife
    {
        static Mesh dotMesh;
        static Mesh DotMesh
        {
            get
            {
                if (dotMesh != null) return dotMesh;
                var b = new MeshBuilder();
                b.Sphere(Vector3.zero, Vector3.one * 0.5f, Color.white, 5, 3, default, true);
                dotMesh = b.ToMesh("SkyDot");
                return dotMesh;
            }
        }

        public static void Build(Transform env, float hw, float nearZ, float gateZ)
        {
            var root = new GameObject("SkyLife").transform;
            root.SetParent(env, false);
            float midZ = (nearZ + gateZ) * 0.5f, len = gateZ - nearZ;

            // Süzülen bulutlar (oyuncuya doğru yavaşça akar, döngüde başa sarar)
            var rnd = new System.Random(17);
            var meshes = new Mesh[4];
            for (int i = 0; i < meshes.Length; i++) meshes[i] = SkyCreatures.DriftCloud(30 + i);
            var drifts = new List<Transform>();
            for (int i = 0; i < 16; i++)
            {
                bool left = i < 11;
                float x = left ? -8f - (float)rnd.NextDouble() * 26f : hw + 11f + (float)rnd.NextDouble() * 20f;
                float y = left ? -2.2f - (float)rnd.NextDouble() * 3.5f : -1.5f - (float)rnd.NextDouble() * 3f;
                float z = nearZ - 10f + (float)rnd.NextDouble() * (len + 40f);
                var go = WaterFx.MakeEnv("Cloud", meshes[i % meshes.Length], root, false);
                go.transform.position = new Vector3(x, y, z);
                float s = 3f + (float)rnd.NextDouble() * 4f;
                go.transform.localScale = new Vector3(s, s * 0.9f, s);
                go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                drifts.Add(go.transform);
            }

            // Yükselen elektrik ve altın zerreleri (yolun sağı ve sütunlar)
            {
                var ps = MakeSystem(root, "Motes", new Vector3(hw * 0.5f, 1.5f, midZ), new Vector3(hw * 2f + 6f, 3f, len));
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 0.8f, 1f), new Color(1f, 0.8f, 0.4f));
                main.maxParticles = 80;
                var em = ps.emission;
                em.rateOverTime = 22f;
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                vel.y = new ParticleSystem.MinMaxCurve(0.4f, 1f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
                ps.Play();
            }
            // Soldan esen ince bulut/rüzgâr tozu (boşluktan yükselir)
            {
                var ps = MakeSystem(root, "Wind", new Vector3(-10f, -1f, midZ), new Vector3(6f, 3f, len + 10f));
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
                main.startColor = new Color(0.16f, 0.17f, 0.26f);
                main.maxParticles = 60;
                var em = ps.emission;
                em.rateOverTime = 16f;
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(1.5f, 3f);
                vel.y = new ParticleSystem.MinMaxCurve(0.3f, 1f);
                vel.z = new ParticleSystem.MinMaxCurve(-1f, 0f);
                var col = ps.colorOverLifetime;
                col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
                col.color = g;
                ps.Play();
            }

            var storm = root.gameObject.AddComponent<SkyStorm>();
            storm.Init(drifts, hw, nearZ, gateZ);
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

    /// <summary>Fırtına zamanlayıcıları: bulut akışı, şimşekler, ark ve gök gürültüsü aydınlanması.</summary>
    public class SkyStorm : MonoBehaviour
    {
        List<Transform> drifts;
        float[] driftSpeed;
        float hw, nearZ, gateZ;
        float belowT = 1f, skyT = 3f, gateT, rodT = 2f, flashT = -1f, thunderT = -1f;
        Color baseAmbient;
        Light sun;
        float baseSun;

        static readonly Color Blue = new Color(0.5f, 0.8f, 1f);

        public void Init(List<Transform> d, float halfW, float near, float gate)
        {
            drifts = d;
            hw = halfW; nearZ = near; gateZ = gate;
            driftSpeed = new float[d.Count];
            for (int i = 0; i < d.Count; i++) driftSpeed[i] = Random.Range(0.5f, 1.2f);
        }

        void Start()
        {
            baseAmbient = RenderSettings.ambientLight;
            sun = RenderSettings.sun;
            if (sun != null) baseSun = sun.intensity;
        }

        void OnDestroy()
        {
            // Sahne kapanırken ışık geri dönsün
            if (flashT >= 0f) RenderSettings.ambientLight = baseAmbient;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Bulutlar oyuncuya doğru akar
            for (int i = 0; i < drifts.Count; i++)
            {
                var t = drifts[i];
                var p = t.position;
                p.z -= driftSpeed[i] * dt;
                if (p.z < nearZ - 20f) p.z = gateZ + 30f;
                t.position = p;
            }

            // Aşağıdaki bulut denizinde mavi şimşekler
            belowT -= dt;
            if (belowT <= 0f)
            {
                belowT = Random.Range(0.7f, 2.2f);
                bool left = Random.value < 0.75f;
                var a = new Vector3(left ? Random.Range(-30f, -9f) : Random.Range(hw + 10f, hw + 26f), Random.Range(-3.5f, -5f), Random.Range(nearZ + 4f, gateZ));
                var b = a + new Vector3(Random.Range(-5f, 5f), Random.Range(-3f, -1f), Random.Range(-4f, 4f));
                Effects.Lightning(a, b, 0.22f, Blue, 0.3f);
                if (Random.value < 0.5f) Effects.Lightning(a, a + new Vector3(Random.Range(-3f, 3f), -1.5f, Random.Range(-3f, 3f)), 0.12f, Blue, 0.22f);
                Effects.Zap(Vector3.Lerp(a, b, 0.5f), 3f);
            }

            // Uzak gökyüzünde dev yıldırım + sahne aydınlanması + gecikmeli gök gürültüsü
            skyT -= dt;
            if (skyT <= 0f)
            {
                skyT = Random.Range(4f, 8f);
                var top = new Vector3(Random.Range(-40f, 40f), 40f, gateZ + Random.Range(14f, 32f));
                var bot = new Vector3(top.x + Random.Range(-8f, 8f), Random.Range(4f, 10f), top.z - Random.Range(0f, 6f));
                Effects.Lightning(top, bot, 1.1f, new Color(0.7f, 0.85f, 1f), 0.45f, 3.5f);
                Effects.Lightning(Vector3.Lerp(top, bot, 0.4f), Vector3.Lerp(top, bot, 0.4f) + new Vector3(Random.Range(-10f, 10f), -12f, 0f), 0.5f, Blue, 0.35f, 2.5f);
                flashT = 0f;
                thunderT = Random.Range(0.25f, 0.7f);
            }
            if (flashT >= 0f)
            {
                flashT += dt;
                // İki kısa çakma
                bool on = flashT < 0.08f || (flashT > 0.16f && flashT < 0.24f);
                RenderSettings.ambientLight = on ? baseAmbient * 2.1f : baseAmbient;
                if (sun != null) sun.intensity = on ? baseSun * 1.5f : baseSun;
                if (flashT > 0.3f) { flashT = -1f; RenderSettings.ambientLight = baseAmbient; if (sun != null) sun.intensity = baseSun; }
            }
            if (thunderT >= 0f)
            {
                thunderT -= dt;
                if (thunderT < 0f) Sfx.Play(Snd.TsunamiHit, 0.22f);
            }

            // Kapının iki küresi arasında sürekli titreyen ark
            gateT -= dt;
            if (gateT <= 0f)
            {
                gateT = Random.Range(0.08f, 0.2f);
                var a = SkyDesigns.GateOrbL;
                var b = SkyDesigns.GateOrbR;
                Effects.Lightning(a, b, 0.14f, Blue, 0.13f, 0.9f);
                if (Random.value < 0.25f)
                {
                    // Küreden perdeye inen kol
                    var from = Random.value < 0.5f ? a : b;
                    Effects.Lightning(from, new Vector3(from.x * 0.7f, Random.Range(2f, 6f), from.z - 0.6f), 0.08f, Blue, 0.12f, 0.6f);
                }
            }

            // Sütunlardaki şimşek iletkenleri arasında ara ara elektrik akımı
            rodT -= dt;
            if (rodT <= 0f && SkyDesigns.RodTips.Count > 1)
            {
                rodT = Random.Range(1.2f, 3f);
                int i = Random.Range(0, SkyDesigns.RodTips.Count - 1);
                var a = SkyDesigns.RodTips[i];
                var b = SkyDesigns.RodTips[i + 1];
                Effects.Lightning(a, b, 0.12f, Blue, 0.35f, 1f);
                Effects.Zap(a, 0.7f);
                Effects.Zap(b, 0.7f);
            }
        }
    }
}
