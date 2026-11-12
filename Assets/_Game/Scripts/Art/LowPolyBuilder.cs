using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Kutu / silindir / küre parçalarından tek bir düşük poligonlu mesh üretir.
    /// Renkler köşe renklerine (vertex color) yazılır, doku gerekmez → tek materyal, tek draw call.
    /// Yüz yönü (winding) her üçgende iç noktaya göre otomatik düzeltilir.
    /// </summary>
    public class MeshBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Color> c = new List<Color>();
        readonly List<int> t = new List<int>();

        /// <summary>Eklenen tüm parçalara uygulanan dönüşüm (parça grupları için).</summary>
        public Matrix4x4 M = Matrix4x4.identity;
        public int TriangleCount => t.Count / 3;

        Vector3 P(Vector3 p) => M.MultiplyPoint3x4(p);

        int Add(Vector3 worldPos, Vector3 worldNormal, Color col)
        {
            v.Add(worldPos);
            n.Add(worldNormal.normalized);
            c.Add(col.linear);
            return v.Count - 1;
        }

        void Tri(int a, int b, int d, Vector3 outward)
        {
            var cr = Vector3.Cross(v[b] - v[a], v[d] - v[a]);
            if (cr.sqrMagnitude < 1e-14f) return; // dejenere
            if (Vector3.Dot(cr, outward) < 0f) { t.Add(a); t.Add(d); t.Add(b); }
            else { t.Add(a); t.Add(b); t.Add(d); }
        }

        /// <summary>Düz gölgeli dörtgen yüz (yerel koordinatlar). interior: parçanın iç noktası.</summary>
        public void Face(Vector3 a, Vector3 b, Vector3 d, Vector3 e, Vector3 interior, Color col)
        {
            Vector3 wa = P(a), wb = P(b), wd = P(d), we = P(e), wi = P(interior);
            var nn = Vector3.Cross(wb - wa, wd - wa);
            if (nn.sqrMagnitude < 1e-12f) nn = Vector3.Cross(wd - wa, we - wa);
            var fc = (wa + wb + wd + we) * 0.25f;
            if (Vector3.Dot(nn, fc - wi) < 0f) nn = -nn;
            int i0 = Add(wa, nn, col), i1 = Add(wb, nn, col), i2 = Add(wd, nn, col), i3 = Add(we, nn, col);
            Tri(i0, i1, i2, nn);
            Tri(i0, i2, i3, nn);
        }

        /// <summary>
        /// Kutu. taperX/taperZ üst yüzü daraltır/genişletir, shearZ üst yüzü ileri/geri kaydırır.
        /// </summary>
        public void Box(Vector3 center, Vector3 size, Color col, Vector3 euler = default,
            float taperX = 1f, float taperZ = 1f, float shearZ = 0f)
        {
            var q = Quaternion.Euler(euler);
            var h = size * 0.5f;
            var p = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                float sx = (i & 1) != 0 ? 1f : -1f;
                float sy = (i & 2) != 0 ? 1f : -1f;
                float sz = (i & 4) != 0 ? 1f : -1f;
                bool top = sy > 0f;
                p[i] = center + q * new Vector3(sx * h.x * (top ? taperX : 1f), sy * h.y,
                    sz * h.z * (top ? taperZ : 1f) + (top ? shearZ : 0f));
            }
            Face(p[0], p[2], p[6], p[4], center, col);
            Face(p[1], p[3], p[7], p[5], center, col);
            Face(p[0], p[1], p[5], p[4], center, col);
            Face(p[2], p[3], p[7], p[6], center, col);
            Face(p[0], p[1], p[3], p[2], center, col);
            Face(p[4], p[5], p[7], p[6], center, col);
        }

        /// <summary>a noktasından b noktasına uzanan (konik olabilen) silindir. Yanlar yumuşak gölgeli.</summary>
        public void Cylinder(Vector3 a, Vector3 b, float ra, float rb, int seg, Color col, bool caps = true, bool flatSides = false)
        {
            var axis = b - a;
            var q = Quaternion.FromToRotation(Vector3.up, axis.normalized);
            var wAxis = M.MultiplyVector(axis).normalized;
            int baseA = v.Count;
            var ringA = new int[seg];
            var ringB = new int[seg];
            var dirs = new Vector3[seg];
            for (int i = 0; i < seg; i++)
            {
                float ang = i * Mathf.PI * 2f / seg;
                dirs[i] = q * new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            }
            if (!flatSides)
            {
                for (int i = 0; i < seg; i++)
                {
                    var wn = M.MultiplyVector(dirs[i]);
                    ringA[i] = Add(P(a + dirs[i] * ra), wn, col);
                    ringB[i] = Add(P(b + dirs[i] * rb), wn, col);
                }
                for (int i = 0; i < seg; i++)
                {
                    int j = (i + 1) % seg;
                    var outw = M.MultiplyVector(dirs[i] + dirs[j]);
                    Tri(ringA[i], ringA[j], ringB[j], outw);
                    Tri(ringA[i], ringB[j], ringB[i], outw);
                }
            }
            else
            {
                var mid = (a + b) * 0.5f;
                for (int i = 0; i < seg; i++)
                {
                    int j = (i + 1) % seg;
                    Face(a + dirs[i] * ra, a + dirs[j] * ra, b + dirs[j] * rb, b + dirs[i] * rb, mid, col);
                }
            }
            if (!caps) return;
            for (int side = 0; side < 2; side++)
            {
                var ctr = side == 0 ? a : b;
                float r = side == 0 ? ra : rb;
                if (r <= 0.0001f) continue;
                var wn = side == 0 ? -wAxis : wAxis;
                int ci = Add(P(ctr), wn, col);
                var ring = new int[seg];
                for (int i = 0; i < seg; i++) ring[i] = Add(P(ctr + dirs[i] * r), wn, col);
                for (int i = 0; i < seg; i++) Tri(ci, ring[i], ring[(i + 1) % seg], wn);
            }
        }

        /// <summary>Elipsoid (düşük çözünürlüklü küre). flat=true ise düz (fasetli) gölgeli.</summary>
        public void Sphere(Vector3 center, Vector3 radii, Color col, int lon = 8, int lat = 6, Vector3 euler = default, bool flat = false)
        {
            var q = Quaternion.Euler(euler);
            var wc = P(center);
            var pts = new Vector3[lat + 1, lon];
            var nrm = new Vector3[lat + 1, lon];
            for (int y = 0; y <= lat; y++)
            {
                float th = Mathf.PI * y / lat; // 0 = üst
                float sy = Mathf.Cos(th), sr = Mathf.Sin(th);
                for (int x = 0; x < lon; x++)
                {
                    float ph = Mathf.PI * 2f * x / lon;
                    var u = new Vector3(Mathf.Cos(ph) * sr, sy, Mathf.Sin(ph) * sr);
                    pts[y, x] = P(center + q * Vector3.Scale(u, radii));
                    nrm[y, x] = M.MultiplyVector(q * new Vector3(u.x / radii.x, u.y / radii.y, u.z / radii.z));
                }
            }
            if (flat)
            {
                for (int y = 0; y < lat; y++)
                    for (int x = 0; x < lon; x++)
                    {
                        int x1 = (x + 1) % lon;
                        Vector3 p0 = pts[y, x], p1 = pts[y, x1], p2 = pts[y + 1, x1], p3 = pts[y + 1, x];
                        var fc = (p0 + p1 + p2 + p3) * 0.25f;
                        var nn = fc - wc;
                        int i0 = Add(p0, nn, col), i1 = Add(p1, nn, col), i2 = Add(p2, nn, col), i3 = Add(p3, nn, col);
                        if (y > 0) Tri(i0, i1, i2, nn);
                        if (y < lat - 1) Tri(i0, i2, i3, nn);
                    }
                return;
            }
            var idx = new int[lat + 1, lon];
            for (int y = 0; y <= lat; y++)
                for (int x = 0; x < lon; x++)
                    idx[y, x] = Add(pts[y, x], nrm[y, x], col);
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int x1 = (x + 1) % lon;
                    var outw = (pts[y, x] + pts[y + 1, x1]) * 0.5f - wc;
                    if (y > 0) Tri(idx[y, x], idx[y, x1], idx[y + 1, x1], outw);
                    if (y < lat - 1) Tri(idx[y, x], idx[y + 1, x1], idx[y + 1, x], outw);
                }
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetNormals(n);
            // 8 bit (Color32) renk: ParticleSystem mesh parçacıkları float renk formatını yanlış okuyor
            var c32 = new List<Color32>(c.Count);
            for (int i = 0; i < c.Count; i++) c32.Add(c[i]);
            m.SetColors(c32);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
