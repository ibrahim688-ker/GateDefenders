using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Bataklık atmosferi: ateş böcekleri, bataklık yüzeyinden yükselen mor/yeşil zehirli sis,
    /// suda patlayan kabarcıklar, kapının zehirli akıntısından yükselen sis. Tek mesh'li hafif parçacıklar.
    /// </summary>
    public static class SwampLife
    {
        static Mesh dotMesh;
        static Mesh DotMesh
        {
            get
            {
                if (dotMesh != null) return dotMesh;
                var b = new MeshBuilder();
                b.Sphere(Vector3.zero, Vector3.one * 0.5f, Color.white, 6, 4);
                dotMesh = b.ToMesh("SwampDot");
                return dotMesh;
            }
        }

        public static void Build(Transform env, float hw, float nearZ, float gateZ)
        {
            var root = new GameObject("SwampLife").transform;
            root.SetParent(env, false);
            float midZ = (nearZ + gateZ) * 0.5f, len = gateZ - nearZ;

            // Ateş böcekleri: iki yanda süzülür, yanıp söner
            Fireflies(root, "Fireflies_L", new Vector3(-10f, 1.6f, midZ), new Vector3(9f, 2.4f, len + 6f), 7f);
            Fireflies(root, "Fireflies_R", new Vector3(hw + 5f, 1.8f, midZ), new Vector3(8f, 2.6f, len), 5f);

            // Zehirli sis: bataklık yüzeyinden yavaşça yükselen büyük, soluk puflar (mor ve yeşil)
            Mist(root, "Mist_Green", new Vector3(-12f, -0.2f, midZ + 3f), new Vector3(12f, 0.1f, len + 10f), new Color(0.07f, 0.14f, 0.05f), 3.5f);
            Mist(root, "Mist_Purple", new Vector3(-11f, -0.2f, midZ), new Vector3(11f, 0.1f, len + 6f), new Color(0.08f, 0.03f, 0.11f), 2.5f);
            Mist(root, "Mist_Gate", new Vector3(0f, 0.2f, gateZ - 1.6f), new Vector3(hw * 2f, 0.1f, 0.8f), new Color(0.07f, 0.15f, 0.05f), 3f);

            Crocodiles(root, nearZ, gateZ);

            // Bataklık kabarcıkları
            {
                var ps = MakeSystem(root, "SwampBubbles", new Vector3(-11f, SwampDesigns.WaterY, midZ), new Vector3(11f, 0.01f, len + 6f));
                var main = ps.main;
                main.prewarm = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
                main.startColor = new Color(0.4f, 0.8f, 0.25f);
                main.maxParticles = 50;
                var em = ps.emission;
                em.rateOverTime = 14f;
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
                vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.8f, 1f), new Keyframe(1f, 1.4f)));
                var col = ps.colorOverLifetime;
                col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
                col.color = g;
                ps.Play();
            }
        }

        /// <summary>Sol bataklıkta 3 yüzen + 1 güneşlenen timsah. Yollar adacıklara çarpmasın diye suda doğrulanır.</summary>
        static void Crocodiles(Transform root, float nearZ, float gateZ)
        {
            var group = new GameObject("Crocodiles").transform;
            group.SetParent(root, false);
            // Kamerada görünsün diye yola yakın (yakında x ≈ -9, uzakta biraz daha dışarıda)
            var centers = new[] { new Vector3(-8.4f, 0f, nearZ + 16f), new Vector3(-9.2f, 0f, nearZ + 27f), new Vector3(-10.2f, 0f, nearZ + 41f) };
            var radii = new[] { new Vector2(0.7f, 3.4f), new Vector2(0.9f, 3.8f), new Vector2(1.2f, 3.6f) };
            float[] speeds = { 0.22f, -0.17f, 0.2f };
            float[] sizes = { 1.6f, 1.75f, 1.65f };
            int made = 0;
            for (int i = 0; i < centers.Length; i++)
            {
                // Yol tamamen suda değilse biraz kaydır / küçült
                var c = centers[i];
                var r = radii[i];
                bool ok = false;
                for (int tries = 0; tries < 12 && !ok; tries++)
                {
                    ok = PathInWater(c, r + Vector2.one * 0.6f * sizes[i]);
                    if (!ok) { c += new Vector3(tries % 4 == 3 ? -0.5f : 0f, 0f, (tries % 2 == 0 ? 1.1f + tries * 0.4f : -(1.1f + tries * 0.4f))); r *= 0.95f; }
                }
                if (!ok) continue;
                SwampCroc.Swimmer(group, c, r, speeds[i], sizes[i], i + 3);
                made++;
            }
            // Dinlenen: suda kıpırdamadan durur, ağzını yavaşça açıp kapar (yüzenlerin arasında, yola yakın)
            for (float z = nearZ + 33f; z < nearZ + 38f; z += 0.5f)
            {
                if (!SwampDesigns.IsWater(-8.2f, z) || !SwampDesigns.IsWater(-8.2f, z + 1.6f) || !SwampDesigns.IsWater(-8.2f, z - 1.6f)) continue;
                SwampCroc.Basker(group, new Vector3(-8.2f, SwampDesigns.WaterY + 0.08f, z), 205f, 1.7f, 9);
                break;
            }
        }

        static bool PathInWater(Vector3 c, Vector2 r)
        {
            for (int k = 0; k < 20; k++)
            {
                float a = k * Mathf.PI * 2f / 20f;
                if (!SwampDesigns.IsWater(c.x + Mathf.Cos(a) * r.x, c.z + Mathf.Sin(a) * r.y)) return false;
            }
            return SwampDesigns.IsWater(c.x, c.z);
        }

        static void Fireflies(Transform root, string name, Vector3 center, Vector3 box, float rate)
        {
            var ps = MakeSystem(root, name, center, box);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.11f);
            main.startColor = new Color(0.8f, 1f, 0.35f);
            main.maxParticles = 60;
            var em = ps.emission;
            em.rateOverTime = rate;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.4f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            // Yanıp sönme: alfa birkaç kez iner çıkar
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.15f, 0.3f), new GradientAlphaKey(1f, 0.45f),
                        new GradientAlphaKey(0.2f, 0.62f), new GradientAlphaKey(1f, 0.78f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();
        }

        static void Mist(Transform root, string name, Vector3 center, Vector3 box, Color c, float rate)
        {
            var ps = MakeSystem(root, name, center, box);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startColor = c;
            main.maxParticles = 45;
            var em = ps.emission;
            em.rateOverTime = rate;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            vel.y = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.6f)));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0f, 1f) });
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
    }
}
