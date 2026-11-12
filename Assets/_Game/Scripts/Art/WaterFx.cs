using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Su altı atmosferinin ortak parçaları: kodla üretilen döşenebilir kostik dokusu (global),
    /// ortam / su materyalleri ve UV'li akan su tabakası mesh'i.
    /// </summary>
    public static class WaterFx
    {
        static Texture2D caustic;
        static Material envMat, waterMat, volEnvMat, lavaMat, swEnvMat, swGlowMat, swWaterMat, frEnvMat, frWaterMat, skEnvMat, skWaterMat, deEnvMat, deSandMat;

        /// <summary>Şu an kurulan dünyanın teması (ortam ve sıvı materyali buna göre seçilir).</summary>
        public static WorldTheme Theme = WorldTheme.Underwater;

        /// <summary>Kostik dokusunu üretir ve tüm shader'lara global olarak verir. Birden fazla çağrılabilir.</summary>
        public static void Init()
        {
            if (caustic == null) caustic = BuildCausticTexture(256, 14, 1234);
            Shader.SetGlobalTexture("_CausticTex", caustic);
            Shader.SetGlobalColor("_CausticColor", Theme == WorldTheme.Volcano ? new Color(0.9f, 0.35f, 0.1f) : Theme == WorldTheme.Swamp ? new Color(0.45f, 0.75f, 0.35f) : Theme == WorldTheme.Frost ? new Color(0.85f, 0.95f, 1f) : Theme == WorldTheme.Sky ? new Color(0.65f, 0.75f, 1f) : Theme == WorldTheme.Desert ? new Color(1f, 0.85f, 0.6f) : new Color(0.5f, 0.8f, 1f));
        }

        public static Material EnvMaterial
        {
            get
            {
                if (Theme == WorldTheme.Desert)
                {
                    if (deEnvMat != null) return deEnvMat;
                    var ddb = GameDatabase.Load();
                    deEnvMat = ddb != null ? ddb.desertEnvMaterial : null;
                    if (deEnvMat == null) { deEnvMat = new Material(Shader.Find("StratejiGame/VertexColorToon")) { enableInstancing = true }; deEnvMat.SetFloat("_Glow", 1.5f); deEnvMat.SetFloat("_SwayStrength", 0f); }
                    return deEnvMat;
                }
                if (Theme == WorldTheme.Sky)
                {
                    if (skEnvMat != null) return skEnvMat;
                    var kdb = GameDatabase.Load();
                    skEnvMat = kdb != null ? kdb.skyEnvMaterial : null;
                    if (skEnvMat == null) { skEnvMat = new Material(Shader.Find("StratejiGame/VertexColorToon")) { enableInstancing = true }; skEnvMat.SetFloat("_Glow", 1.6f); skEnvMat.SetFloat("_SwayStrength", 0f); }
                    return skEnvMat;
                }
                if (Theme == WorldTheme.Frost)
                {
                    if (frEnvMat != null) return frEnvMat;
                    var fdb = GameDatabase.Load();
                    frEnvMat = fdb != null ? fdb.frostEnvMaterial : null;
                    if (frEnvMat == null) { frEnvMat = new Material(Shader.Find("StratejiGame/VertexColorToon")) { enableInstancing = true }; frEnvMat.SetFloat("_Glow", 1.5f); frEnvMat.SetFloat("_SwayStrength", 0f); }
                    return frEnvMat;
                }
                if (Theme == WorldTheme.Swamp)
                {
                    if (swEnvMat != null) return swEnvMat;
                    var sdb = GameDatabase.Load();
                    swEnvMat = sdb != null ? sdb.swampEnvMaterial : null;
                    if (swEnvMat == null) { swEnvMat = new Material(Shader.Find("StratejiGame/VertexColorToon")) { enableInstancing = true }; swEnvMat.SetFloat("_SwayStrength", 0.15f); }
                    return swEnvMat;
                }
                if (Theme == WorldTheme.Volcano)
                {
                    if (volEnvMat != null) return volEnvMat;
                    var vdb = GameDatabase.Load();
                    volEnvMat = vdb != null ? vdb.volcanoEnvMaterial : null;
                    if (volEnvMat == null) { volEnvMat = new Material(Shader.Find("StratejiGame/VertexColorToon")) { enableInstancing = true }; volEnvMat.SetFloat("_Glow", 2f); volEnvMat.SetFloat("_SwayStrength", 0f); }
                    return volEnvMat;
                }
                if (envMat != null) return envMat;
                var db = GameDatabase.Load();
                envMat = db != null ? db.envMaterial : null;
                if (envMat == null)
                {
                    envMat = new Material(Shader.Find("StratejiGame/VertexColorToon")) { enableInstancing = true };
                    envMat.SetFloat("_CausticStrength", 1f);
                    envMat.SetFloat("_RimStrength", 0.12f);
                }
                return envMat;
            }
        }

        public static Material WaterMaterial
        {
            get
            {
                if (Theme == WorldTheme.Desert)
                {
                    if (deSandMat != null) return deSandMat;
                    var ddb = GameDatabase.Load();
                    deSandMat = ddb != null ? ddb.desertSandMaterial : null;
                    if (deSandMat == null) deSandMat = new Material(Shader.Find("StratejiGame/WaterFlow"));
                    return deSandMat;
                }
                if (Theme == WorldTheme.Sky)
                {
                    if (skWaterMat != null) return skWaterMat;
                    var kdb = GameDatabase.Load();
                    skWaterMat = kdb != null ? kdb.skyWaterMaterial : null;
                    if (skWaterMat == null) skWaterMat = new Material(Shader.Find("StratejiGame/WaterFlow"));
                    return skWaterMat;
                }
                if (Theme == WorldTheme.Frost)
                {
                    if (frWaterMat != null) return frWaterMat;
                    var fdb = GameDatabase.Load();
                    frWaterMat = fdb != null ? fdb.frostWaterMaterial : null;
                    if (frWaterMat == null) frWaterMat = new Material(Shader.Find("StratejiGame/WaterFlow"));
                    return frWaterMat;
                }
                if (Theme == WorldTheme.Swamp)
                {
                    if (swWaterMat != null) return swWaterMat;
                    var sdb = GameDatabase.Load();
                    swWaterMat = sdb != null ? sdb.swampWaterMaterial : null;
                    if (swWaterMat == null) swWaterMat = new Material(Shader.Find("StratejiGame/WaterFlow"));
                    return swWaterMat;
                }
                if (Theme == WorldTheme.Volcano)
                {
                    if (lavaMat != null) return lavaMat;
                    var vdb = GameDatabase.Load();
                    lavaMat = vdb != null ? vdb.lavaMaterial : null;
                    if (lavaMat == null) lavaMat = new Material(Shader.Find("StratejiGame/WaterFlow"));
                    return lavaMat;
                }
                if (waterMat != null) return waterMat;
                var db = GameDatabase.Load();
                waterMat = db != null ? db.waterMaterial : null;
                if (waterMat == null) waterMat = new Material(Shader.Find("StratejiGame/WaterFlow"));
                return waterMat;
            }
        }

        public static GameObject MakeEnv(string name, Mesh mesh, Transform parent, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = EnvMaterial;
            r.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

/// <summary>Kendi ışığını yayan ortam parçaları için (sallanmasız, _Glow açık) materyal: bataklık mantarları, fenerler.</summary>
        public static Material GlowMaterial
        {
            get
            {
                if (swGlowMat != null) return swGlowMat;
                var db = GameDatabase.Load();
                swGlowMat = db != null ? db.swampGlowMaterial : null;
                if (swGlowMat == null) { swGlowMat = new Material(Shader.Find("StratejiGame/VertexColorToon")) { enableInstancing = true }; swGlowMat.SetFloat("_Glow", 2f); swGlowMat.SetFloat("_SwayStrength", 0f); }
                return swGlowMat;
            }
        }

        public static GameObject MakeGlow(string name, Mesh mesh, Transform parent, bool castShadows = false)
        {
            var go = MakeEnv(name, mesh, parent, castShadows);
            go.GetComponent<MeshRenderer>().sharedMaterial = GlowMaterial;
            return go;
        }

        public static GameObject MakeWater(string name, Mesh mesh, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = WaterMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        /// <summary>
        /// Döşenebilir kostik dokusu: Voronoi hücre kenarlarına yakın yerler parlak (F2 - F1 küçük).
        /// </summary>
        static Texture2D BuildCausticTexture(int size, int cells, int seed)
        {
            var rnd = new System.Random(seed);
            var pts = new Vector2[cells];
            for (int i = 0; i < cells; i++) pts[i] = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    float f1 = 9f, f2 = 9f;
                    for (int i = 0; i < cells; i++)
                        for (int ox = -1; ox <= 1; ox++)
                            for (int oy = -1; oy <= 1; oy++)
                            {
                                float dx = pts[i].x + ox - p.x, dy = pts[i].y + oy - p.y;
                                float d = dx * dx + dy * dy;
                                if (d < f1) { f2 = f1; f1 = d; }
                                else if (d < f2) f2 = d;
                            }
                    float e = Mathf.Sqrt(f2) - Mathf.Sqrt(f1);
                    float v = Mathf.Pow(Mathf.Clamp01(1f - e * 9f), 3.2f);
                    byte b = (byte)(v * 255f);
                    px[y * size + x] = new Color32(b, b, b, 255);
                }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, true)
            {
                name = "CausticTex",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        /// <summary>
        /// Bir yol boyunca uzanan su tabakası. path: suyun aktığı merkez çizgi (başlangıçtan bitişe),
        /// across: tabakanın genişlik yönü (birim), width: genişlik. UV.y akış boyunca metre cinsinden artar.
        /// foamStart/foamEnd: köpük (köşe alfası) başlangıç ve bitişte.
        /// </summary>
        public static Mesh Sheet(IList<Vector3> path, Vector3 across, float width, int widthSegs = 2,
            float foamStart = 0f, float foamEnd = 0.6f, string name = "WaterSheet")
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var c = new List<Color>();
            var t = new List<int>();
            float dist = 0f;
            int cols = widthSegs + 1;
            for (int i = 0; i < path.Count; i++)
            {
                if (i > 0) dist += Vector3.Distance(path[i - 1], path[i]);
                float k = path.Count > 1 ? i / (float)(path.Count - 1) : 0f;
                float foam = Mathf.Lerp(foamStart, foamEnd, k * k);
                for (int j = 0; j < cols; j++)
                {
                    float s = j / (float)widthSegs - 0.5f;
                    v.Add(path[i] + across * (s * width));
                    uv.Add(new Vector2((s + 0.5f) * width * 0.6f, dist));
                    c.Add(new Color(1f, 1f, 1f, foam));
                }
            }
            for (int i = 0; i < path.Count - 1; i++)
                for (int j = 0; j < widthSegs; j++)
                {
                    int a = i * cols + j, b = a + 1, d = a + cols, e = d + 1;
                    t.Add(a); t.Add(d); t.Add(b);
                    t.Add(b); t.Add(d); t.Add(e);
                }
            var m = new Mesh { name = name };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetColors(c);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Birden çok su tabakasını tek mesh'te birleştirir.</summary>
        public static Mesh Combine(string name, params Mesh[] parts)
        {
            var ci = new CombineInstance[parts.Length];
            for (int i = 0; i < parts.Length; i++) ci[i] = new CombineInstance { mesh = parts[i], transform = Matrix4x4.identity };
            var m = new Mesh { name = name };
            m.CombineMeshes(ci, true, true);
            return m;
        }
    }
}
