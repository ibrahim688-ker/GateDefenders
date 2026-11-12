using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StratejiGame.EditorTools
{
    /// <summary>
    /// Bataklık modellerini (renksiz, düşük poligonlu FBX) köşe rengiyle boyar.
    /// Model boy 1'e normalize edilir (taban y = 0, XZ merkezde), üçgenler ayrıştırılır (düz gölgeli).
    /// Köşe alfası = parıltı (0 = tam parlak) → VertexColorToon _Glow.
    /// Parçalar bağlı bileşenlere ayrılır: mantar şapkaları / sapları / sarmaşıklar / ok vb.
    /// </summary>
    public static class SwampPainter
    {
        public class Model
        {
            public Vector3[] v;
            public int[] t;
            public int T;
            public Vector3[] fn;      // yüz normali
            public Vector3[] fc;      // yüz merkezi
            public float[] conc;      // yüz çukurluğu
            public int[] comp;        // yüzün bileşen sırası (0 = en büyük)
            public List<Bounds> compBounds = new List<Bounds>();
            public List<int> compTris = new List<int>();
        }

        public static Model Load(string fbxPath)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var mf = go.GetComponentInChildren<MeshFilter>();
            var src = mf.sharedMesh;
            var M = Matrix4x4.TRS(Vector3.zero, mf.transform.rotation, mf.transform.lossyScale);
            var v = src.vertices;
            for (int i = 0; i < v.Length; i++) v[i] = M.MultiplyPoint3x4(v[i]);
            Vector3 mn = v[0], mx = v[0];
            foreach (var p in v) { mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); }
            float h = mx.y - mn.y;
            var c = new Vector3((mn.x + mx.x) * 0.5f, mn.y, (mn.z + mx.z) * 0.5f);
            for (int i = 0; i < v.Length; i++) v[i] = (v[i] - c) / h;

            var m = new Model { v = v, t = src.triangles };
            m.T = m.t.Length / 3;
            var t = m.t;

            // Kaynak: aynı konumdaki köşeleri birleştir
            var weld = new Dictionary<Vector3Int, int>();
            var wid = new int[v.Length];
            var wpos = new List<Vector3>();
            for (int i = 0; i < v.Length; i++)
            {
                var key = new Vector3Int(Mathf.RoundToInt(v[i].x * 20000f), Mathf.RoundToInt(v[i].y * 20000f), Mathf.RoundToInt(v[i].z * 20000f));
                int id;
                if (!weld.TryGetValue(key, out id)) { id = wpos.Count; weld[key] = id; wpos.Add(v[i]); }
                wid[i] = id;
            }
            int W = wpos.Count;
            var par = new int[W];
            for (int i = 0; i < W; i++) par[i] = i;
            System.Func<int, int> find = null;
            find = x => { while (par[x] != x) { par[x] = par[par[x]]; x = par[x]; } return x; };

            var wn = new Vector3[W];
            var nb = new HashSet<int>[W];
            for (int i = 0; i < W; i++) nb[i] = new HashSet<int>();
            m.fn = new Vector3[m.T];
            m.fc = new Vector3[m.T];
            for (int f = 0; f < m.T; f++)
            {
                int a = wid[t[f * 3]], b = wid[t[f * 3 + 1]], cc = wid[t[f * 3 + 2]];
                var n = Vector3.Cross(wpos[b] - wpos[a], wpos[cc] - wpos[a]);
                m.fn[f] = n.normalized;
                m.fc[f] = (wpos[a] + wpos[b] + wpos[cc]) / 3f;
                wn[a] += n; wn[b] += n; wn[cc] += n;
                nb[a].Add(b); nb[a].Add(cc); nb[b].Add(a); nb[b].Add(cc); nb[cc].Add(a); nb[cc].Add(b);
                int ra = find(a), rb = find(b); par[rb] = ra;
                int rc = find(cc); par[rc] = find(ra);
            }
            var vc = new float[W];
            for (int i = 0; i < W; i++)
            {
                var n = wn[i].normalized;
                float sum = 0f; int k = 0;
                foreach (var j in nb[i]) { var d = wpos[j] - wpos[i]; float len = d.magnitude; if (len < 1e-6f) continue; sum += Vector3.Dot(n, d / len); k++; }
                vc[i] = k > 0 ? sum / k : 0f;
            }
            m.conc = new float[m.T];
            for (int f = 0; f < m.T; f++) m.conc[f] = (vc[wid[t[f * 3]]] + vc[wid[t[f * 3 + 1]]] + vc[wid[t[f * 3 + 2]]]) / 3f;

            // Bileşenler (büyükten küçüğe sıralı)
            var groups = new Dictionary<int, List<int>>();
            for (int f = 0; f < m.T; f++)
            {
                int r = find(wid[t[f * 3]]);
                List<int> l;
                if (!groups.TryGetValue(r, out l)) { l = new List<int>(); groups[r] = l; }
                l.Add(f);
            }
            var list = new List<List<int>>(groups.Values);
            list.Sort((x, y) => y.Count.CompareTo(x.Count));
            m.comp = new int[m.T];
            for (int ci = 0; ci < list.Count; ci++)
            {
                var b = new Bounds(m.fc[list[ci][0]], Vector3.zero);
                foreach (var f in list[ci])
                {
                    m.comp[f] = ci;
                    b.Encapsulate(v[t[f * 3]]); b.Encapsulate(v[t[f * 3 + 1]]); b.Encapsulate(v[t[f * 3 + 2]]);
                }
                m.compBounds.Add(b);
                m.compTris.Add(list[ci].Count);
            }
            return m;
        }

        /// <summary>Çukurluk eşiği: bileşen içindeki yüzlerin en çukur 'fraction' kadarı.</summary>
        public static float ConcThreshold(Model m, System.Func<int, bool> filter, float fraction)
        {
            var vals = new List<float>();
            for (int f = 0; f < m.T; f++) if (filter(f)) vals.Add(m.conc[f]);
            if (vals.Count == 0) return 999f;
            vals.Sort();
            return vals[Mathf.Clamp(Mathf.RoundToInt(vals.Count * (1f - fraction)), 0, vals.Count - 1)];
        }

        public static void Save(Model m, Color[] faceCol, float[] faceGlow, string outPath)
        {
            var ov = new List<Vector3>(m.T * 3);
            var on = new List<Vector3>(m.T * 3);
            var oc = new List<Color32>(m.T * 3);
            var ot = new List<int>(m.T * 3);
            for (int f = 0; f < m.T; f++)
            {
                var lin = faceCol[f].linear;
                var c32 = (Color32)new Color(lin.r, lin.g, lin.b, 1f - faceGlow[f]);
                for (int k = 0; k < 3; k++)
                {
                    ov.Add(m.v[m.t[f * 3 + k]]);
                    on.Add(m.fn[f]);
                    oc.Add(c32);
                    ot.Add(f * 3 + k);
                }
            }
            var mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(outPath) };
            mesh.SetVertices(ov);
            mesh.SetNormals(on);
            mesh.SetColors(oc);
            mesh.SetTriangles(ot, 0);
            mesh.RecalculateBounds();
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(outPath);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); EditorUtility.SetDirty(existing); }
            else AssetDatabase.CreateAsset(mesh, outPath);
            AssetDatabase.SaveAssets();
        }

        public static Color Vary(System.Random r, Color c, float amt)
        {
            float k = 1f + ((float)r.NextDouble() * 2f - 1f) * amt;
            return new Color(c.r * k, c.g * k, c.b * k);
        }

        // ─────────────────────────────────────────────────────
        // ÇÜRÜMÜŞ ENT (boss)
        // ─────────────────────────────────────────────────────
        public static readonly Color BarkA = new Color(0.5f, 0.45f, 0.56f);
        public static readonly Color BarkB = new Color(0.38f, 0.34f, 0.44f);
        public static readonly Color Crevice = new Color(0.16f, 0.12f, 0.2f);
        public static readonly Color Mud = new Color(0.22f, 0.2f, 0.16f);
        public static readonly Color EyeGreen = new Color(0.55f, 1f, 0.3f);
        public static readonly Color CavityPurple = new Color(0.3f, 0.1f, 0.4f);
        public static readonly Color CapPurple = new Color(0.62f, 0.28f, 1f);
        public static readonly Color StemPale = new Color(0.85f, 0.8f, 0.92f);
        public static readonly Color Vine = new Color(0.38f, 0.66f, 0.22f);

        /// <summary>Eşikler: gözler ve ağız oyuğu bölgeleri (model uzayında, boy = 1).</summary>
        public class TreantSettings
        {
            public Vector2 eyeX = new Vector2(-0.16f, 0.16f);
            public Vector2 eyeY = new Vector2(0.55f, 0.76f);
            public float eyeFraction = 0.22f;
            public float eyeGap = 0.03f;   // iki gözün arası (ortadaki yüzler göz sayılmaz)
            public Vector2 mouthX = new Vector2(-0.17f, 0.17f);
            public Vector2 mouthY = new Vector2(0.07f, 0.45f);
            public float mouthFraction = 0.45f;
            public float frontZ = 0.0f;
        }

        public static string PaintTreant(string fbxPath, string outPath, TreantSettings s)
        {
            var m = Load(fbxPath);
            var rnd = new System.Random(7);
            var col = new Color[m.T];
            var glow = new float[m.T];

            System.Func<int, bool> inEye = f => m.comp[f] == 0 && Mathf.Abs(m.fc[f].x) > s.eyeGap && m.fc[f].x > s.eyeX.x && m.fc[f].x < s.eyeX.y && m.fc[f].y > s.eyeY.x && m.fc[f].y < s.eyeY.y && m.fc[f].z > s.frontZ && m.fn[f].z > -0.2f;
            System.Func<int, bool> inMouth = f => m.comp[f] == 0 && m.fc[f].x > s.mouthX.x && m.fc[f].x < s.mouthX.y && m.fc[f].y > s.mouthY.x && m.fc[f].y < s.mouthY.y && m.fc[f].z > s.frontZ - 0.08f;
            float eyeTh = ConcThreshold(m, inEye, s.eyeFraction);
            float mouthTh = ConcThreshold(m, inMouth, s.mouthFraction);
            float bodyTh = ConcThreshold(m, f => m.comp[f] == 0, 0.14f);

            int eyes = 0, mouth = 0, caps = 0, stems = 0, vines = 0;
            var eyeCenter = Vector3.zero;
            for (int f = 0; f < m.T; f++)
            {
                int ci = m.comp[f];
                if (ci > 0)
                {
                    var b = m.compBounds[ci];
                    var sz = b.size;
                    float maxD = Mathf.Max(sz.x, Mathf.Max(sz.y, sz.z));
                    bool stem = sz.y > 3f * Mathf.Max(sz.x, sz.z);
                    bool vine = (m.compTris[ci] >= 25 && maxD >= 0.14f) || (maxD < 0.05f && b.center.y > 0.5f);
                    if (stem) { col[f] = StemPale; glow[f] = 0.3f; stems++; }
                    else if (vine) { col[f] = Vary(rnd, Vine, 0.12f); glow[f] = 0.12f; vines++; }
                    else
                    {
                        // Mantar şapkası: üst yüz parlak mor, alt yüz daha koyu
                        col[f] = m.fn[f].y > -0.2f ? CapPurple : new Color(0.45f, 0.18f, 0.6f);
                        glow[f] = m.fn[f].y > -0.2f ? 1f : 0.5f;
                        caps++;
                    }
                    continue;
                }
                if (inEye(f) && m.conc[f] >= eyeTh) { col[f] = EyeGreen; glow[f] = 1f; eyes++; eyeCenter += m.fc[f]; continue; }
                if (inMouth(f) && m.conc[f] >= mouthTh) { col[f] = Vary(rnd, CavityPurple, 0.1f); glow[f] = 0.35f; mouth++; continue; }

                var c = Color.Lerp(BarkA, BarkB, (float)rnd.NextDouble());
                c = Color.Lerp(c, new Color(0.62f, 0.58f, 0.66f), Mathf.Clamp01(m.fn[f].y) * 0.25f);
                if (m.conc[f] >= bodyTh) c = Color.Lerp(c, Crevice, 0.75f);
                // Kökler: çamur ve ıslaklık
                float wet = Mathf.InverseLerp(0.12f, 0.02f, m.fc[f].y);
                c = Color.Lerp(c, Mud, wet * 0.7f);
                col[f] = c;
            }
            Save(m, col, glow, outPath);
            if (eyes > 0) eyeCenter /= eyes;
            return $"{outPath}: {m.T} üçgen | göz {eyes} (merkez {eyeCenter.ToString("0.000")}) | ağız {mouth} | şapka {caps} | sap {stems} | sarmaşık {vines}";
        }

        // ─────────────────────────────────────────────────────
        // ZEHİRLİ OKÇU (Venom Archer)
        // ─────────────────────────────────────────────────────
        public static string PaintArcher(string fbxPath, string outPath)
        {
            var m = Load(fbxPath);
            var rnd = new System.Random(9);
            var col = new Color[m.T];
            var glow = new float[m.T];
            var cloak = new Color(0.22f, 0.36f, 0.18f);
            var robe = new Color(0.26f, 0.3f, 0.17f);
            var tunic = new Color(0.25f, 0.4f, 0.2f);
            var hood = new Color(0.32f, 0.48f, 0.24f);
            var wood = new Color(0.33f, 0.23f, 0.15f);
            var leather = new Color(0.42f, 0.27f, 0.14f);
            var boots = new Color(0.2f, 0.14f, 0.1f);
            var bone = new Color(0.88f, 0.82f, 0.68f);
            var toxic = new Color(0.5f, 1f, 0.25f);
            var fletch = new Color(0.45f, 0.62f, 0.25f);
            int tips = 0;
            for (int f = 0; f < m.T; f++)
            {
                int ci = m.comp[f];
                var b = m.compBounds[ci];
                var p = m.fc[f];
                Color c;
                switch (ci)
                {
                    case 0: c = Color.Lerp(cloak, new Color(0.14f, 0.2f, 0.12f), Mathf.InverseLerp(0.25f, 0.02f, p.y) * 0.6f); break;
                    case 1: c = Color.Lerp(robe, new Color(0.18f, 0.18f, 0.12f), Mathf.InverseLerp(0.25f, 0.02f, p.y) * 0.5f); break;
                    case 2: c = Mathf.Abs(p.y - 0.47f) < 0.025f ? leather : tunic; break;
                    case 3: c = hood; break;
                    case 4: c = wood; break;
                    case 5: c = leather; break;
                    case 6: case 8: c = boots; break;
                    case 7: c = new Color(0.27f, 0.19f, 0.12f); break;
                    case 9: c = leather; break;
                    case 10: c = bone; break;
                    default:
                        if (b.min.x < -0.37f && b.size.y < 0.05f && b.size.x > 0.05f) { c = new Color(0.85f, 0.3f, 1f); glow[f] = 1f; tips++; }          // ok ucu (zehirli)
                        else if (b.size.x > 0.35f && b.size.y < 0.1f) c = new Color(0.58f, 0.48f, 0.32f);      // ok gövdesi
                        else if (b.size.y > 0.3f && Mathf.Min(b.size.x, b.size.z) < 0.06f) c = new Color(0.75f, 0.72f, 0.6f); // yay kirişi
                        else if (b.center.x > 0f && b.center.z < 0.05f) c = fletch;   // sadaktaki oklar
                        else c = wood;                                               // yayın dal uçları
                        break;
                }
                // Maske gözleri: gagalı maskenin kök kısmında zehir yeşili ışıltı
                if (ci == 3 && Vector3.Distance(p, new Vector3(0.12f, 0.79f, 0.06f)) < 0.05f && m.fn[f].z > 0.2f) { c = toxic; glow[f] = 0.8f; }
                col[f] = Vary(rnd, c, 0.06f);
            }
            // Model yana dönük nişan alıyor (ok -X yönünde): oku oyuncuya (+Z) çevirmek için Y ekseninde 90° döndür
            // Ok gövdesinin gerçek yönünü ölç (uç -X tarafında), tam +Z'ye çevir
            Vector3 tipP = Vector3.zero, tailP = Vector3.zero; float tipX = 99f, tailX = -99f;
            for (int f = 0; f < m.T; f++)
            {
                var bb = m.compBounds[m.comp[f]];
                if (!(bb.size.x > 0.35f && bb.size.y < 0.1f)) continue;
                for (int k = 0; k < 3; k++)
                {
                    var q = m.v[m.t[f * 3 + k]];
                    if (q.x < tipX) { tipX = q.x; tipP = q; }
                    if (q.x > tailX) { tailX = q.x; tailP = q; }
                }
            }
            var adir = tipP - tailP;
            float yawDeg = tipX < 90f ? -Mathf.Atan2(adir.x, adir.z) * Mathf.Rad2Deg : 90f;
            var rot = Quaternion.Euler(0f, yawDeg, 0f);
            Vector3 mn = Vector3.one * 99f, mx = -Vector3.one * 99f;
            for (int i = 0; i < m.v.Length; i++) { m.v[i] = rot * m.v[i]; mn = Vector3.Min(mn, m.v[i]); mx = Vector3.Max(mx, m.v[i]); }
            var ctr = new Vector3((mn.x + mx.x) * 0.5f, 0f, (mn.z + mx.z) * 0.5f);
            for (int i = 0; i < m.v.Length; i++) m.v[i] -= ctr;
            for (int f = 0; f < m.T; f++) m.fn[f] = rot * m.fn[f];
            Save(m, col, glow, outPath);
            return $"{outPath}: {m.T} üçgen, ok ucu {tips}, dönüş {yawDeg:0.0}°";
        }
    }
}
