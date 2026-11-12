using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Yanardağ atmosferi: kenarlardan ve lav nehrinden yükselen korlar, havada süzülen kül,
    /// nehirde patlayan lav kabarcıkları. Hepsi hafif, tek mesh'li parçacık sistemleri.
    /// </summary>
    public static class VolcanoLife
    {
        static Mesh dotMesh;
        static Mesh DotMesh
        {
            get
            {
                if (dotMesh != null) return dotMesh;
                var b = new MeshBuilder();
                b.Sphere(Vector3.zero, Vector3.one * 0.5f, Color.white, 5, 3, default, true);
                dotMesh = b.ToMesh("VolcanoDot");
                return dotMesh;
            }
        }

        public static void Build(Transform env, float hw, float nearZ, float gateZ)
        {
            var root = new GameObject("VolcanoLife").transform;
            root.SetParent(env, false);
            float midZ = (nearZ + gateZ) * 0.5f, len = gateZ - nearZ;

            // Korlar: nehirden ve sağ taraftan yükselir
            Embers(root, "Embers_River", new Vector3(-9.2f, -0.4f, midZ), new Vector3(3.5f, 0.1f, len + 6f), 14f);
            Embers(root, "Embers_Right", new Vector3(hw + 5f, -0.2f, midZ), new Vector3(8f, 0.1f, len), 9f);
            Embers(root, "Embers_Gate", new Vector3(0f, 0.2f, gateZ - 1.4f), new Vector3(hw * 2f, 0.1f, 0.6f), 6f);

            // Kül: tüm alanda yavaşça düşer
            {
                var ps = MakeSystem(root, "Ash", new Vector3(0f, 9f, midZ + 4f), new Vector3(26f, 1f, len + 10f));
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 14f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
                main.startColor = Color.white;
                main.maxParticles = 180;
                var em = ps.emission;
                em.rateOverTime = 16f;
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
                vel.y = new ParticleSystem.MinMaxCurve(-0.45f, -0.25f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.05f);
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = Fade(new Color(0.3f, 0.27f, 0.25f));
                ps.Play();
            }

            // Lav kabarcıkları: nehirde küçük sıçramalar
            {
                var ps = MakeSystem(root, "LavaPops", new Vector3(-9.2f, -0.5f, midZ), new Vector3(1.8f, 0.01f, len + 6f));
                var main = ps.main;
                main.prewarm = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.85f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
                main.startColor = new Color(1f, 0.55f, 0.15f);
                main.gravityModifier = 0.6f;
                main.maxParticles = 40;
                var em = ps.emission;
                em.rateOverTime = 10f;
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
                vel.y = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
                ps.Play();
            }
        }

        static void Embers(Transform root, string name, Vector3 center, Vector3 box, float rate)
        {
            var ps = MakeSystem(root, name, center, box);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = Color.white;
            main.maxParticles = 90;
            var em = ps.emission;
            em.rateOverTime = rate;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.25f);
            vel.y = new ParticleSystem.MinMaxCurve(0.6f, 1.5f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.7f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.75f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.08f), 0.5f), new GradientColorKey(new Color(0.6f, 0.1f, 0.02f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();
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

        static Gradient Fade(Color c)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            return g;
        }
    }
}
