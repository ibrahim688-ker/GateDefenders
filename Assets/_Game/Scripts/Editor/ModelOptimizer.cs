using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace StratejiGame.EditorTools
{
    /// <summary>
    /// Meshy gibi araçlardan gelen çok yüksek poligonlu modelleri mobil için hafifletir.
    /// Unity'nin yerleşik Mesh LOD üreticisiyle sadeleştirir, hedef üçgen sayısına en yakın seviyeyi
    /// ayrı bir Mesh dosyası olarak kaydeder. Model yönü/ölçeği köke göre düzeltilir, pivot ayak hizasına alınır.
    /// </summary>
    public static class ModelOptimizer
    {
        public static string Bake(string fbxPath, int targetTris, string outMeshPath, float targetHeight)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var mf = prefab.GetComponentInChildren<MeshFilter>(true);
            var src = mf.sharedMesh;
            Matrix4x4 toRoot = prefab.transform.localToWorldMatrix * Matrix4x4.identity;
            // Kök dahil tüm hiyerarşi dönüşümü (rot 270/ölçek 100 gibi FBX düzeltmeleri)
            toRoot = mf.transform.localToWorldMatrix;
            // prefab kökünün kendi konumu sıfırdır; localToWorld zaten kök rot/ölçeğini içerir

            var work = Object.Instantiate(src);
            MeshLodUtility.GenerateMeshLods(work, 24);

            int best = 0;
            for (int i = 0; i < work.lodCount; i++)
            {
                int tris = (int)work.GetLod(0, i).indexCount / 3;
                if (tris >= targetTris) best = i; else break;
            }
            var range = work.GetLod(0, best);
            // Tüm LOD'ları içeren ham indeks tamponunu oku
            var allIdx = new int[range.indexCount];
            using (var md = Mesh.AcquireReadOnlyMeshData(work))
            {
                var d = md[0];
                if (d.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32)
                {
                    var ib = d.GetIndexData<uint>();
                    for (int k = 0; k < allIdx.Length; k++) allIdx[k] = (int)ib[(int)range.indexStart + k];
                }
                else
                {
                    var ib = d.GetIndexData<ushort>();
                    for (int k = 0; k < allIdx.Length; k++) allIdx[k] = ib[(int)range.indexStart + k];
                }
            }
            var srcV = work.vertices;
            var srcN = work.normals;
            var srcUv = work.uv;

            var map = new Dictionary<int, int>();
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>();
            var idx = new int[range.indexCount];
            var normalM = toRoot.inverse.transpose;
            for (int k = 0; k < range.indexCount; k++)
            {
                int oi = allIdx[k];
                if (!map.TryGetValue(oi, out int ni))
                {
                    ni = v.Count;
                    map[oi] = ni;
                    v.Add(toRoot.MultiplyPoint3x4(srcV[oi]));
                    n.Add(srcN.Length > 0 ? normalM.MultiplyVector(srcN[oi]).normalized : Vector3.up);
                    uv.Add(srcUv.Length > 0 ? srcUv[oi] : Vector2.zero);
                }
                idx[k] = ni;
            }
            Object.DestroyImmediate(work);

            // Pivot: ayak hizası (min y) ve yatay merkez; hedef boya ölçekle
            var b = new Bounds(v[0], Vector3.zero);
            foreach (var p in v) b.Encapsulate(p);
            float s = targetHeight > 0f ? targetHeight / Mathf.Max(0.0001f, b.size.y) : 1f;
            var offset = new Vector3(b.center.x, b.min.y, b.center.z);
            for (int i = 0; i < v.Count; i++) v[i] = (v[i] - offset) * s;

            var mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(outMeshPath) };
            mesh.indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(idx, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            mesh.UploadMeshData(false);

            AssetDatabase.DeleteAsset(outMeshPath);
            AssetDatabase.CreateAsset(mesh, outMeshPath);
            AssetDatabase.SaveAssets();
            return $"{outMeshPath}: {idx.Length / 3} üçgen, {v.Count} köşe, boyut {mesh.bounds.size}";
        }
    }
}
