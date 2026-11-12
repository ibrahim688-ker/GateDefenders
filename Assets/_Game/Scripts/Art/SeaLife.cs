using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Su altının canlı atmosferi: balık sürüleri, parlayan denizanaları, yukarıdan inen ışık huzmeleri,
    /// yükselen kabarcıklar ve suda süzülen parçacıklar. Hepsi hafif ve sayıca az (mobil).
    /// </summary>
    public static class SeaLife
    {
        public static void Build(Transform env, float hw, float nearZ, float gateZ)
        {
            var root = new GameObject("SeaLife").transform;
            root.SetParent(env, false);

            // Balık sürüleri (sol resifin üstünde; biri sağda)
            FishSchool.Create(root, new Vector3(-9f, 1.6f, 6f), new Vector2(3.2f, 5f), 0.32f, 6, 0, 1);
            FishSchool.Create(root, new Vector3(-8f, 2.4f, 22f), new Vector2(3.8f, 6f), -0.25f, 7, 1, 2);
            FishSchool.Create(root, new Vector3(9.5f, 2f, 16f), new Vector2(2.6f, 4.5f), 0.28f, 5, 2, 3);

            // Arkadaki açık denizde yavaşça süzülen büyük balıklar + uzakta küçük bir sürü
            BackgroundFish.Create(root, new Vector3(-30f, 12.5f, gateZ + 16f), 2.4f, 8f, 2, 0f);
            BackgroundFish.Create(root, new Vector3(35f, 16f, gateZ + 22f), -1.8f, 10f, 0, 11f);
            for (int i = 0; i < 5; i++)
                BackgroundFish.Create(root, new Vector3(10f + i * 2.6f, 16.5f + (i % 2) * 1.4f, gateZ + 20f + (i % 3)), -2.8f, 4f, 1, 5f + i * 0.7f);

            // Denizanaları (sol tarafta, yavaşça süzülür)
            Jellyfish.Create(root, new Vector3(-7.2f, 3.2f, 12f), 1.0f, 0);
            Jellyfish.Create(root, new Vector3(-10.5f, 4.2f, 24f), 1.25f, 1);
            Jellyfish.Create(root, new Vector3(-8.6f, 2.6f, 32f), 0.85f, 2);

            // Işık huzmeleri kullanıcı isteğiyle kaldırıldı (LightRay sınıfı ileride lazım olursa duruyor)

            // Kabarcıklar + süzülen parçacıklar
            Bubbles(root, hw, nearZ, gateZ);
            Motes(root, nearZ, gateZ);
        }

        static Mesh bubbleMesh;
        static Mesh BubbleMesh
        {
            get
            {
                if (bubbleMesh != null) return bubbleMesh;
                var b = new MeshBuilder();
                b.Sphere(Vector3.zero, Vector3.one * 0.5f, Color.white, 6, 4);
                bubbleMesh = b.ToMesh("Bubble");
                return bubbleMesh;
            }
        }

        static ParticleSystem MakeSystem(Transform root, string name, Vector3 center, Vector3 box)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = center;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = BubbleMesh;
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

        static void Bubbles(Transform root, float hw, float nearZ, float gateZ)
        {
            // İki kenar boyunca yükselen kabarcıklar (yolun ortasını kalabalıklaştırmaz)
            for (int side = -1; side <= 1; side += 2)
            {
                var ps = MakeSystem(root, side < 0 ? "Bubbles_L" : "Bubbles_R",
                    new Vector3(side * (hw + 4f), -0.2f, (nearZ + gateZ) * 0.5f), new Vector3(6f, 0.1f, gateZ - nearZ));
                var main = ps.main;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
                main.startColor = new Color(0.55f, 0.85f, 1f, 1f);
                main.maxParticles = 90;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.prewarm = true;
                var em = ps.emission;
                em.rateOverTime = 12f;
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
                vel.y = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 0.25f;
                noise.frequency = 0.6f;
                noise.quality = ParticleSystemNoiseQuality.Low;
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = Fade(new Color(0.35f, 0.6f, 0.75f));
                ps.Play();
            }
        }

        static void Motes(Transform root, float nearZ, float gateZ)
        {
            var ps = MakeSystem(root, "Motes", new Vector3(0f, 3f, (nearZ + gateZ) * 0.5f + 4f), new Vector3(22f, 5f, gateZ - nearZ));
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.startColor = Color.white;
            main.maxParticles = 160;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var em = ps.emission;
            em.rateOverTime = 18f;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.12f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Fade(new Color(0.25f, 0.4f, 0.45f));
            ps.Play();
        }
    }

    /// <summary>Elips yörüngede dolaşan küçük balık sürüsü; kuyruk sallama ile.</summary>
    public class FishSchool : MonoBehaviour
    {
        static readonly Mesh[] fishMeshes = new Mesh[3];

        Transform[] fish;
        Vector3[] offsets;
        float[] phases;
        Vector3 center;
        Vector2 radius;
        float speed, angle;

        public static FishSchool Create(Transform parent, Vector3 center, Vector2 radius, float speed, int count, int variant, int seed)
        {
            var go = new GameObject("FishSchool");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<FishSchool>();
            s.center = center;
            s.radius = radius;
            s.speed = speed;
            var rnd = new System.Random(seed);
            s.fish = new Transform[count];
            s.offsets = new Vector3[count];
            s.phases = new float[count];
            s.angle = (float)rnd.NextDouble() * 6.28f;
            for (int i = 0; i < count; i++)
            {
                var f = CharacterDesigns.MakeVisual("Fish", FishMesh(variant), go.transform);
                f.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                float sc = 0.85f + (float)rnd.NextDouble() * 0.35f;
                f.transform.localScale = Vector3.one * sc;
                s.fish[i] = f.transform;
                s.offsets[i] = new Vector3((float)rnd.NextDouble() - 0.5f, ((float)rnd.NextDouble() - 0.5f) * 0.6f, (float)rnd.NextDouble() - 0.5f) * 1.3f;
                s.phases[i] = (float)rnd.NextDouble() * 6.28f;
            }
            return s;
        }

        void Update()
        {
            if (fish == null) return;
            float dt = Time.deltaTime, t = Time.time;
            angle += speed * dt;
            for (int i = 0; i < fish.Length; i++)
            {
                float a = angle + offsets[i].z * 0.25f;
                var p = center + new Vector3(Mathf.Cos(a) * (radius.x + offsets[i].x), offsets[i].y + Mathf.Sin(t * 1.3f + phases[i]) * 0.12f, Mathf.Sin(a) * (radius.y + offsets[i].x));
                var tangent = new Vector3(-Mathf.Sin(a) * radius.x, 0f, Mathf.Cos(a) * radius.y) * Mathf.Sign(speed);
                fish[i].position = p;
                float wiggle = Mathf.Sin(t * 11f + phases[i]) * 9f;
                fish[i].rotation = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0f, wiggle, 0f);
            }
        }

        internal static Mesh FishMesh(int v)
        {
            v = Mathf.Clamp(v, 0, 2);
            if (fishMeshes[v] != null) return fishMeshes[v];
            Color body, stripe, fin;
            switch (v)
            {
                case 1: body = new Color(1f, 0.5f, 0.15f); stripe = Color.white; fin = new Color(1f, 0.5f, 0.15f); break;   // palyaço
                case 2: body = new Color(0.2f, 0.4f, 0.95f); stripe = new Color(0.1f, 0.15f, 0.35f); fin = new Color(1f, 0.85f, 0.2f); break; // mavi cerrah
                default: body = new Color(1f, 0.88f, 0.25f); stripe = new Color(0.25f, 0.5f, 1f); fin = new Color(1f, 0.88f, 0.25f); break;
            }
            var b = new MeshBuilder();
            b.Sphere(Vector3.zero, new Vector3(0.06f, 0.1f, 0.19f), body, 7, 5, default, true);
            b.Box(new Vector3(0f, 0f, 0.03f), new Vector3(0.125f, 0.17f, 0.035f), stripe);
            if (v == 1) b.Box(new Vector3(0f, 0f, -0.08f), new Vector3(0.115f, 0.14f, 0.03f), stripe);
            b.Box(new Vector3(0f, 0f, -0.22f), new Vector3(0.015f, 0.17f, 0.1f), fin, default, 1f, 1f, -0.03f);
            b.Box(new Vector3(0f, 0.1f, -0.02f), new Vector3(0.012f, 0.07f, 0.14f), fin, default, 1f, 0.5f);
            for (int s = -1; s <= 1; s += 2)
                b.Sphere(new Vector3(0.05f * s, 0.025f, 0.12f), Vector3.one * 0.02f, new Color(0.05f, 0.05f, 0.08f), 4, 3);
            fishMeshes[v] = b.ToMesh("Fish_" + v);
            return fishMeshes[v];
        }
    }

    /// <summary>Parlayan, nabız gibi atan ve yavaşça yükselip alçalan denizanası.</summary>
    public class Jellyfish : MonoBehaviour
    {
        static readonly Mesh[] meshes = new Mesh[3];
        Vector3 basePos;
        float phase, scale;
        Transform vis;

        public static Jellyfish Create(Transform parent, Vector3 pos, float scale, int variant)
        {
            var go = new GameObject("Jellyfish");
            go.transform.SetParent(parent, false);
            var j = go.AddComponent<Jellyfish>();
            j.basePos = pos;
            j.scale = scale;
            j.phase = variant * 2.1f;
            j.vis = FxDesigns.MakeFx("Glow", BuildMesh(variant), go.transform).transform;
            go.transform.position = pos;
            return j;
        }

        void Update()
        {
            float t = Time.time + phase;
            if (vis == null) return;
            float pulse = Mathf.Sin(t * 2.2f);
            transform.position = basePos + new Vector3(Mathf.Sin(t * 0.35f) * 0.8f, Mathf.Sin(t * 0.5f) * 0.6f + Mathf.Max(0f, pulse) * 0.15f, Mathf.Cos(t * 0.3f) * 0.6f);
            vis.localScale = new Vector3(1f + pulse * 0.08f, 1f - pulse * 0.1f, 1f + pulse * 0.08f) * scale;
            vis.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 8f, t * 10f, Mathf.Cos(t * 0.6f) * 8f);
        }

        static Mesh BuildMesh(int v)
        {
            if (meshes[v] != null) return meshes[v];
            Color bell, core, tent;
            switch (v)
            {
                case 1: bell = new Color(0.12f, 0.3f, 0.5f); core = new Color(0.3f, 0.7f, 0.9f); tent = new Color(0.06f, 0.18f, 0.3f); break;
                case 2: bell = new Color(0.4f, 0.16f, 0.4f); core = new Color(0.9f, 0.4f, 0.8f); tent = new Color(0.22f, 0.08f, 0.24f); break;
                default: bell = new Color(0.3f, 0.16f, 0.5f); core = new Color(0.7f, 0.5f, 1f); tent = new Color(0.16f, 0.1f, 0.3f); break;
            }
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0f, 0f), new Vector3(0.38f, 0.3f, 0.38f), bell, 10, 6);
            b.Sphere(new Vector3(0f, 0.02f, 0f), new Vector3(0.18f, 0.14f, 0.18f), core, 8, 5);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                float len = 0.6f + (i % 3) * 0.25f;
                b.Box(new Vector3(Mathf.Cos(a) * 0.2f, -0.1f - len * 0.5f, Mathf.Sin(a) * 0.2f), new Vector3(0.03f, len, 0.03f), tent,
                    new Vector3(Mathf.Sin(a) * 8f, 0f, Mathf.Cos(a) * 8f), 1.4f, 1.4f);
            }
            meshes[v] = b.ToMesh("Jellyfish_" + v);
            return meshes[v];
        }
    }

    /// <summary>Yüzeyden süzülen, yavaşça yanıp sönen ve sallanan ışık huzmesi (eklemeli).</summary>
    public class LightRay : MonoBehaviour
    {
        static Mesh rayMesh;
        MaterialPropertyBlock mpb;
        Renderer rend;
        float phase;
        Quaternion baseRot;

        public static LightRay Create(Transform parent, Vector3 foot, float width, int index)
        {
            var go = new GameObject("LightRay");
            go.transform.SetParent(parent, false);
            go.transform.position = foot;
            go.transform.localScale = new Vector3(width, 1f, 1f);
            go.transform.rotation = Quaternion.Euler(-12f, 0f, (index % 2 == 0 ? 1f : -1f) * (10f + index * 3f));
            var l = go.AddComponent<LightRay>();
            l.baseRot = go.transform.rotation;
            l.phase = index * 1.7f;
            l.rend = FxDesigns.MakeFx("Ray", RayMesh, go.transform).GetComponent<Renderer>();
            l.mpb = new MaterialPropertyBlock();
            return l;
        }

        void Update()
        {
            float t = Time.time + phase;
            if (rend == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            float k = 0.65f + 0.35f * Mathf.Sin(t * 0.7f) * Mathf.Sin(t * 0.43f + 1f);
            mpb.SetColor("_BaseColor", new Color(k, k, k, 1f));
            rend.SetPropertyBlock(mpb);
            transform.rotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.3f) * 3f);
        }

        static Mesh RayMesh
        {
            get
            {
                if (rayMesh != null) return rayMesh;
                // Tepede parlak, tabana doğru sönen dikey dörtgen (iki katman: dar parlak + geniş soluk)
                var v = new List<Vector3>();
                var c = new List<Color>();
                var t = new List<int>();
                AddQuad(v, c, t, 0.5f, 9f, new Color(0.045f, 0.08f, 0.09f), new Color(0.004f, 0.012f, 0.016f));
                AddQuad(v, c, t, 1.1f, 9f, new Color(0.02f, 0.035f, 0.04f), new Color(0f, 0f, 0f));
                rayMesh = new Mesh { name = "LightRay" };
                rayMesh.SetVertices(v);
                rayMesh.SetColors(c);
                rayMesh.SetTriangles(t, 0);
                rayMesh.RecalculateBounds();
                return rayMesh;
            }
        }

        static void AddQuad(List<Vector3> v, List<Color> c, List<int> t, float halfW, float h, Color top, Color bottom)
        {
            int i = v.Count;
            v.Add(new Vector3(-halfW * 0.6f, 0f, 0f)); c.Add(bottom);
            v.Add(new Vector3(halfW * 0.6f, 0f, 0f)); c.Add(bottom);
            v.Add(new Vector3(halfW, h, 0f)); c.Add(top);
            v.Add(new Vector3(-halfW, h, 0f)); c.Add(top);
            t.Add(i); t.Add(i + 2); t.Add(i + 1);
            t.Add(i); t.Add(i + 3); t.Add(i + 2);
        }
    }

    /// <summary>Arka plandaki açık denizde yatay olarak süzülen büyük balık; ekranın dışına çıkınca diğer uçtan döner.</summary>
    public class BackgroundFish : MonoBehaviour
    {
        const float Range = 42f;
        float speed, phase, baseY;
        Transform vis;

        public static BackgroundFish Create(Transform parent, Vector3 pos, float speed, float scale, int variant, float phase)
        {
            var go = new GameObject("BackgroundFish");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var f = go.AddComponent<BackgroundFish>();
            f.speed = speed;
            f.phase = phase;
            f.baseY = pos.y;
            var v = CharacterDesigns.MakeVisual("Fish", FishSchool.FishMesh(variant), go.transform);
            v.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            v.transform.localScale = Vector3.one * scale;
            f.vis = v.transform;
            return f;
        }

        void Update()
        {
            if (vis == null) return;
            float t = Time.time + phase;
            var p = transform.position;
            p.x += speed * Time.deltaTime;
            if (p.x > Range) p.x = -Range;
            else if (p.x < -Range) p.x = Range;
            p.y = baseY + Mathf.Sin(t * 0.4f) * 1.2f;
            transform.position = p;
            float yaw = speed > 0f ? 90f : -90f;
            transform.rotation = Quaternion.Euler(Mathf.Cos(t * 0.4f) * 6f, yaw + Mathf.Sin(t * 5f) * 7f, 0f);
        }
    }
}
