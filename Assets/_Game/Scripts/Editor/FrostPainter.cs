using UnityEngine;

namespace StratejiGame.EditorTools
{
    /// <summary>
    /// Dondurucu Zirveler modellerini boyar (SwampPainter.Load/Save altyapısıyla).
    /// Yeti: mavi-beyaz kürk, mavi deri (yüz/göğüs), parlayan buz gözler, koyu demir zırh (üstü karlı), deri kayışlar, parlayan buz gürz.
    /// Ayaz Muhafızı: çelik miğfer + beyaz sakal, buz-çelik göğüslük, koyu arduvaz pelerin, beyaz kürk yaka, parlayan buz asa başı.
    /// </summary>
    public static class FrostPainter
    {
        public class YetiSettings
        {
            public Vector2 faceX = new Vector2(-0.14f, 0.14f);
            public Vector2 faceY = new Vector2(0.7f, 0.92f);
            public Vector2 chestX = new Vector2(-0.2f, 0.2f);
            public Vector2 chestY = new Vector2(0.48f, 0.7f);
            public float eyeFraction = 0.14f;
            public float eyeGap = 0.025f;
            public float faceMinZ = -0.23f;
            public float chestMinZ = -0.3f;
        }

        public static string PaintYeti(string fbxPath, string outPath, YetiSettings s)
        {
            var m = SwampPainter.Load(fbxPath);
            var rnd = new System.Random(3);
            var col = new Color[m.T];
            var glow = new float[m.T];
            var furA = new Color(0.72f, 0.82f, 0.95f);
            var furB = new Color(0.6f, 0.7f, 0.88f);
            var furLight = new Color(0.9f, 0.94f, 1f);
            var furShadow = new Color(0.36f, 0.46f, 0.68f);
            var skin = new Color(0.3f, 0.46f, 0.8f);
            var eye = new Color(0.6f, 0.95f, 1f);
            var iron = new Color(0.22f, 0.24f, 0.28f);
            var snow = new Color(0.9f, 0.93f, 1f);
            var steel = new Color(0.68f, 0.7f, 0.75f);
            var leather = new Color(0.34f, 0.21f, 0.13f);
            var cloth = new Color(0.4f, 0.28f, 0.18f);
            var ice = new Color(0.5f, 0.82f, 1f);
            var iceLight = new Color(0.8f, 0.95f, 1f);

            float frontZ = s.faceMinZ;
            System.Func<int, bool> inFace = f => m.comp[f] == 0 && m.fc[f].x > s.faceX.x && m.fc[f].x < s.faceX.y && m.fc[f].y > s.faceY.x && m.fc[f].y < s.faceY.y && m.fc[f].z > frontZ && m.fn[f].z > 0.2f;
            System.Func<int, bool> inChest = f => m.comp[f] == 0 && m.fc[f].x > s.chestX.x && m.fc[f].x < s.chestX.y && m.fc[f].y > s.chestY.x && m.fc[f].y < s.chestY.y && m.fc[f].z > s.chestMinZ && m.fn[f].z > 0.35f;
            float eyeTh = SwampPainter.ConcThreshold(m, f => inFace(f) && Mathf.Abs(m.fc[f].x) > s.eyeGap, s.eyeFraction);
            float bodyTh = SwampPainter.ConcThreshold(m, f => m.comp[f] == 0, 0.16f);
            int eyes = 0, faces = 0, chest = 0;
            for (int f = 0; f < m.T; f++)
            {
                int ci = m.comp[f];
                var b = m.compBounds[ci];
                float maxD = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                Color c;
                if (ci == 0)
                {
                    if (inFace(f) && Mathf.Abs(m.fc[f].x) > s.eyeGap && m.conc[f] >= eyeTh) { c = eye; glow[f] = 1f; eyes++; }
                    else if (inFace(f)) { c = SwampPainter.Vary(rnd, skin, 0.08f); faces++; }
                    else if (inChest(f)) { c = SwampPainter.Vary(rnd, skin, 0.08f); chest++; }
                    else if (m.fc[f].y < 0.06f) c = new Color(0.28f, 0.38f, 0.6f);   // ayak tabanı / parmaklar
                    else
                    {
                        c = Color.Lerp(furA, furB, (float)rnd.NextDouble());
                        c = Color.Lerp(c, furLight, Mathf.Clamp01(m.fn[f].y) * 0.5f);
                        if (m.conc[f] >= bodyTh) c = Color.Lerp(c, furShadow, 0.7f);
                    }
                }
                else if (ci == 3)
                {
                    // Buz gürz: parlayan, üst yüzleri daha açık
                    c = m.fn[f].y > 0.4f ? iceLight : SwampPainter.Vary(rnd, ice, 0.1f);
                    glow[f] = m.fn[f].y > 0.4f ? 0.6f : 0.4f;
                }
                else if (ci == 4) c = SwampPainter.Vary(rnd, leather, 0.08f);
                else if (b.size.z < 0.045f && b.size.y > 0.15f) c = SwampPainter.Vary(rnd, cloth, 0.08f);   // bel örtüsü
                else if (maxD <= 0.14f && m.compTris[ci] <= 10) c = steel;                                   // dikenler, perçinler
                else
                {
                    c = SwampPainter.Vary(rnd, iron, 0.1f);
                    if (m.fn[f].y > 0.55f) c = snow;                                                          // zırhın üstünde kar
                }
                col[f] = c;
            }
            SwampPainter.Save(m, col, glow, outPath);
            return $"{outPath}: {m.T} üçgen | göz {eyes} | yüz {faces} | göğüs {chest} | frontZ {frontZ:0.00}";
        }

        public static string PaintWarden(string fbxPath, string outPath)
        {
            var m = SwampPainter.Load(fbxPath);
            var rnd = new System.Random(4);
            var col = new Color[m.T];
            var glow = new float[m.T];
            var fur = new Color(0.86f, 0.89f, 0.94f);
            var furGrey = new Color(0.42f, 0.45f, 0.52f);
            var steel = new Color(0.36f, 0.45f, 0.58f);
            var steelDark = new Color(0.24f, 0.29f, 0.38f);
            var cape = new Color(0.1f, 0.14f, 0.26f);
            var boots = new Color(0.24f, 0.26f, 0.31f);
            var beard = new Color(0.9f, 0.92f, 0.96f);
            var ice = new Color(0.55f, 0.9f, 1f);
            var shaft = new Color(0.62f, 0.8f, 0.92f);
            int crystals = 0;
            for (int f = 0; f < m.T; f++)
            {
                int ci = m.comp[f];
                var b = m.compBounds[ci];
                var p = m.fc[f];
                Color c;
                switch (ci)
                {
                    case 0: c = furGrey; break;
                    case 1: c = (m.fn[f].z > 0.35f && p.y < 0.84f && p.y > 0.66f) ? beard : steel; break;
                    case 2: c = p.y > 0.82f ? fur : Color.Lerp(cape, new Color(0.14f, 0.18f, 0.27f), Mathf.InverseLerp(0.4f, 0.05f, p.y) * 0.5f); break;
                    case 3: c = m.fn[f].y > 0.5f ? Color.Lerp(steel, Color.white, 0.3f) : steel; break;
                    case 4: case 5: c = steelDark; break;
                    case 6: c = shaft; glow[f] = 0.25f; break;
                    case 7: case 10: case 11: c = fur; break;
                    case 8: case 9: c = boots; break;
                    default:
                        if (b.center.x > 0.08f && b.center.y > 0.44f && b.center.z > 0.1f) { c = ice; glow[f] = 0.85f; crystals++; }   // asa başındaki buz kristalleri
                        else c = steel;
                        break;
                }
                col[f] = SwampPainter.Vary(rnd, c, 0.05f);
            }
            SwampPainter.Save(m, col, glow, outPath);
            return $"{outPath}: {m.T} üçgen, kristal yüzü {crystals}";
        }
    }
}
