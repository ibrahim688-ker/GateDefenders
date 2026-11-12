using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StratejiGame.EditorTools
{
    /// <summary>
    /// Renksiz gelen düşük poligonlu modelleri "yanardağ kayası" gibi boyar:
    /// koyu bazalt plakalar, plakaların arasındaki çukur dikişlerde parlayan lav,
    /// isteğe bağlı parlak bölgeler (göğüs çekirdeği, gözler, balta ağızları).
    /// Köşe renginin alfası parlamadır (0 = tam parlak) → VertexColorToon'un _Glow özelliği.
    /// Üçgenler ayrıştırılır (düz gölgeli, üçgen başına tek renk).
    /// </summary>
    public static class MagmaPainter
    {
        public class Region
        {
            public Vector3 center;     // model uzayında (boy 1'e normalize)
            public Vector3 radius;     // elipsoid yarıçapları
            public Color color;
            public float glow = 1f;    // 0..1
            public bool onlyFront;     // yalnızca öne (+Z) bakan yüzler
        }

        public class Settings
        {
            public float lavaFraction = 0.18f;   // en çukur üçgenlerin bu oranı lav olur
            public float emberFraction = 0.1f;   // sonraki oran kor (sönük) olur
            public Color rockA = new Color(0.1f, 0.09f, 0.09f);
            public Color rockB = new Color(0.2f, 0.17f, 0.16f);
            public Color lava = new Color(1f, 0.42f, 0.08f);
            public Color ember = new Color(0.55f, 0.16f, 0.05f);
            public List<Region> regions = new List<Region>();
            public int seed = 1;
        }

        public static string Paint(Mesh src, string outPath, Settings s)
        {
            var v = src.vertices;
            var t = src.triangles;
            var rnd = new System.Random(s.seed);

            // 1) Aynı konumdaki köşeleri birleştir → komşuluk
            var weld = new Dictionary<Vector3Int, int>();
            var wid = new int[v.Length];
            var wpos = new List<Vector3>();
            for (int i = 0; i < v.Length; i++)
            {
                var key = new Vector3Int(Mathf.RoundToInt(v[i].x * 10000f), Mathf.RoundToInt(v[i].y * 10000f), Mathf.RoundToInt(v[i].z * 10000f));
                int id;
                if (!weld.TryGetValue(key, out id)) { id = wpos.Count; weld[key] = id; wpos.Add(v[i]); }
                wid[i] = id;
            }
            int W = wpos.Count;
            var wn = new Vector3[W];
            var nb = new HashSet<int>[W];
            for (int i = 0; i < W; i++) nb[i] = new HashSet<int>();
            int T = t.Length / 3;
            var fn = new Vector3[T];
            for (int f = 0; f < T; f++)
            {
                int a = wid[t[f * 3]], b = wid[t[f * 3 + 1]], c = wid[t[f * 3 + 2]];
                var n = Vector3.Cross(wpos[b] - wpos[a], wpos[c] - wpos[a]);
                fn[f] = n.normalized;
                wn[a] += n; wn[b] += n; wn[c] += n;
                nb[a].Add(b); nb[a].Add(c); nb[b].Add(a); nb[b].Add(c); nb[c].Add(a); nb[c].Add(b);
            }
            // 2) Köşe çukurluğu: komşular teğet düzlemin üstündeyse (normal yönünde) çukur
            var conc = new float[W];
            for (int i = 0; i < W; i++)
            {
                var n = wn[i].normalized;
                float sum = 0f; int k = 0;
                foreach (var j in nb[i]) { var d = wpos[j] - wpos[i]; float len = d.magnitude; if (len < 1e-6f) continue; sum += Vector3.Dot(n, d / len); k++; }
                conc[i] = k > 0 ? sum / k : 0f;
            }
            var fc = new float[T];
            for (int f = 0; f < T; f++) fc[f] = (conc[wid[t[f * 3]]] + conc[wid[t[f * 3 + 1]]] + conc[wid[t[f * 3 + 2]]]) / 3f;
            var sorted = (float[])fc.Clone();
            System.Array.Sort(sorted);
            float lavaTh = sorted[Mathf.Clamp(Mathf.RoundToInt(T * (1f - s.lavaFraction)), 0, T - 1)];
            float emberTh = sorted[Mathf.Clamp(Mathf.RoundToInt(T * (1f - s.lavaFraction - s.emberFraction)), 0, T - 1)];

            // 3) Üçgen başına renk
            var ov = new List<Vector3>(T * 3);
            var on = new List<Vector3>(T * 3);
            var oc = new List<Color32>(T * 3);
            var ot = new List<int>(T * 3);
            int lavaCount = 0;
            for (int f = 0; f < T; f++)
            {
                var p0 = v[t[f * 3]]; var p1 = v[t[f * 3 + 1]]; var p2 = v[t[f * 3 + 2]];
                var ctr = (p0 + p1 + p2) / 3f;
                Color col; float glow = 0f;
                Region hit = null;
                foreach (var r in s.regions)
                {
                    var d = ctr - r.center;
                    float e = (d.x * d.x) / (r.radius.x * r.radius.x) + (d.y * d.y) / (r.radius.y * r.radius.y) + (d.z * d.z) / (r.radius.z * r.radius.z);
                    if (e <= 1f && (!r.onlyFront || fn[f].z > 0.1f)) { hit = r; break; }
                }
                if (hit != null) { col = hit.color; glow = hit.glow; }
                else if (fc[f] >= lavaTh) { col = s.lava; glow = 1f; lavaCount++; }
                else if (fc[f] >= emberTh) { col = s.ember; glow = 0.5f; }
                else
                {
                    col = Color.Lerp(s.rockA, s.rockB, (float)rnd.NextDouble());
                    // Üste bakan yüzler biraz açık (kül tozu)
                    col = Color.Lerp(col, new Color(0.3f, 0.27f, 0.26f), Mathf.Clamp01(fn[f].y) * 0.25f);
                }
                var lin = col.linear;
                var c32 = (Color32)new Color(lin.r, lin.g, lin.b, 1f - glow);
                for (int k = 0; k < 3; k++)
                {
                    ov.Add(v[t[f * 3 + k]]);
                    on.Add(fn[f]);
                    oc.Add(c32);
                    ot.Add(f * 3 + k);
                }
            }
            var m = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(outPath) };
            m.SetVertices(ov);
            m.SetNormals(on);
            m.SetColors(oc);
            m.SetTriangles(ot, 0);
            m.RecalculateBounds();
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(outPath);
            if (existing != null) { EditorUtility.CopySerialized(m, existing); EditorUtility.SetDirty(existing); }
            else AssetDatabase.CreateAsset(m, outPath);
            AssetDatabase.SaveAssets();
            return outPath + ": " + T + " üçgen, lav " + lavaCount;
        }
    }
}
