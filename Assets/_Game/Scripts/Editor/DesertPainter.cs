using UnityEngine;

namespace StratejiGame.EditorTools
{
    /// <summary>
    /// Çöl Sıcaklığı boss'u Kumların Hakimi (Zırhlı Akrep Firavun) boyaması (SwampPainter.Load/Save altyapısıyla).
    /// İnsan gövdesi bronz ten + altın/lacivert göğüslük yakası, nemes başlık (altın-lacivert çizgili), parlayan kehribar gözler,
    /// akrep gövdesi altın zırh plakaları (oluklar koyu), koyu bronz bacaklar ve kıskaç uçları, kuyrukta parlayan mor zehir iğnesi,
    /// lacivert-altın kuşak, beyaz keten etek, altın orak (sapı abanoz).
    /// </summary>
    public static class DesertPainter
    {
        public static string PaintPharaoh(string fbxPath, string outPath)
        {
            var m = SwampPainter.Load(fbxPath);
            var rnd = new System.Random(8);
            var col = new Color[m.T];
            var glow = new float[m.T];
            var skin = new Color(0.62f, 0.42f, 0.28f);
            var gold = new Color(0.86f, 0.62f, 0.22f);
            var goldLight = new Color(1f, 0.82f, 0.4f);
            var groove = new Color(0.36f, 0.22f, 0.09f);
            var bronze = new Color(0.34f, 0.21f, 0.1f);
            var lapis = new Color(0.14f, 0.28f, 0.68f);
            var linen = new Color(0.92f, 0.88f, 0.76f);
            var ebony = new Color(0.18f, 0.11f, 0.06f);
            var venom = new Color(0.78f, 0.3f, 1f);
            var eye = new Color(1f, 0.78f, 0.3f);
            var turq = new Color(0.25f, 0.8f, 0.75f);

            float grooveTh = SwampPainter.ConcThreshold(m, f => m.comp[f] <= 3 && IsArmor(m, f), 0.2f);
            System.Func<int, bool> inFace = f => m.comp[f] == 1 && m.fn[f].z > 0.45f && Mathf.Abs(m.fc[f].x) < 0.07f && m.fc[f].y < 0.9f && m.fc[f].z > 0.27f;
            float eyeTh = SwampPainter.ConcThreshold(m, f => inFace(f) && m.fc[f].y > 0.84f && Mathf.Abs(m.fc[f].x) > 0.012f, 0.25f);
            int eyes = 0, sting = 0, faceN = 0;
            for (int f = 0; f < m.T; f++)
            {
                int ci = m.comp[f];
                var p = m.fc[f];
                var n = m.fn[f];
                Color c;
                float g = 0f;
                switch (ci)
                {
                    case 0:
                        if (p.z > -0.42f && p.z < -0.15f && p.y > 0.58f && p.y < 0.74f) { c = venom; g = 0.85f; sting++; }      // zehir iğnesi
                        else if (!IsArmor(m, f))
                        {
                            // İnsan gövdesi: boyunda geniş altın-lacivert yaka, altı bronz ten
                            if (p.y > 0.74f && p.z > 0.1f) c = Mathf.Repeat(p.y * 60f, 1f) < 0.5f ? gold : lapis;
                            else c = SwampPainter.Vary(rnd, skin, 0.05f);
                        }
                        else if (n.y < -0.35f) c = bronze;                                                                       // karın altı
                        else c = m.conc[f] >= grooveTh ? groove : Color.Lerp(gold, goldLight, Mathf.Clamp01(n.y) * 0.5f);     // altın zırh plakaları
                        break;
                    case 1:
                        if (inFace(f))
                        {
                            if (p.y > 0.84f && Mathf.Abs(p.x) > 0.012f && m.conc[f] >= eyeTh) { c = eye; g = 1f; eyes++; }
                            else { c = skin; faceN++; }
                        }
                        else c = Mathf.Repeat(p.y * 45f, 1f) < 0.5f ? gold : lapis;                                          // nemes başlık
                        break;
                    case 2: case 3:
                        c = m.conc[f] >= grooveTh ? groove : gold; break;                                                         // kıskaçlar
                    case 13: case 14: c = bronze; break;                                                                         // kıskaç uçları
                    case 10:
                        if (p.y > 0.74f) { c = goldLight; g = 0.45f; } else c = ebony; break;                                    // orak
                    case 11: c = Mathf.Repeat(p.y * 50f, 1f) < 0.5f ? lapis : gold; break;                                       // kuşak
                    case 12: c = SwampPainter.Vary(rnd, linen, 0.04f); break;                                                    // keten etek
                    case 15: case 17: c = gold; g = 0.3f; break;
                    case 16: c = turq; g = 0.4f; break;
                    default: c = SwampPainter.Vary(rnd, bronze, 0.08f); break;                                                   // bacaklar
                }
                col[f] = c;
                glow[f] = g;
            }
            SwampPainter.Save(m, col, glow, outPath);
            return $"{outPath}: {m.T} üçgen | göz {eyes} | yüz {faceN} | iğne {sting}";
        }

        /// <summary>Gövde parçası akrep zırhı mı (insan gövdesi değil)?</summary>
        static bool IsArmor(SwampPainter.Model m, int f)
        {
            var p = m.fc[f];
            if (m.comp[f] != 0) return true;
            return p.z < -0.05f || p.y < 0.48f;
        }
    }
}
