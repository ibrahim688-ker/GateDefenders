using UnityEngine;

namespace StratejiGame.EditorTools
{
    /// <summary>
    /// Gökyüzü Tapınağı modellerini boyar (SwampPainter.Load/Save altyapısıyla).
    /// Fırtına Lordu: soluk mermer-mavi ten, gövdede parlayan altın rün çizgileri, parlayan elektrik saç ve gözler,
    /// koyu fırtına bulutu hortumu (sarmal bantlar + içinde yer yer şimşek), altın / elektrik mavisi haleler.
    /// Şimşek Süvarisi: koyu lacivert tunik, altın zırh (göğüs, omuz, miğfer, dizlik), parlayan mavi vizör,
    /// açık altın kanatlar, altın mızrak sapı ve parlayan elektrik mızrak ucu.
    /// </summary>
    public static class SkyPainter
    {
        public static string PaintStormLord(string fbxPath, string outPath)
        {
            var m = SwampPainter.Load(fbxPath);
            var rnd = new System.Random(5);
            var col = new Color[m.T];
            var glow = new float[m.T];
            var skin = new Color(0.8f, 0.84f, 0.96f);
            var skinShade = new Color(0.5f, 0.54f, 0.74f);
            var rune = new Color(1f, 0.78f, 0.3f);
            var hair = new Color(0.75f, 0.92f, 1f);
            var eye = new Color(0.85f, 1f, 1f);
            var gold = new Color(0.95f, 0.72f, 0.26f);
            var cloudDark = new Color(0.2f, 0.2f, 0.3f);
            var cloudLight = new Color(0.46f, 0.47f, 0.62f);
            var electric = new Color(0.5f, 0.85f, 1f);
            var bolt = new Color(1f, 0.95f, 0.55f);

            float runeTh = SwampPainter.ConcThreshold(m, f => m.comp[f] == 0 && m.fc[f].y > 0.45f && m.fc[f].y < 0.8f, 0.13f);
            System.Func<int, bool> inFace = f => m.comp[f] == 0 && Mathf.Abs(m.fc[f].x) < 0.07f && m.fc[f].y > 0.8f && m.fc[f].y < 0.885f && m.fn[f].z > 0.3f && m.fc[f].z > 0f;
            float eyeTh = SwampPainter.ConcThreshold(m, f => inFace(f) && Mathf.Abs(m.fc[f].x) > 0.012f, 0.2f);
            int eyes = 0, runes = 0, hairN = 0;
            for (int f = 0; f < m.T; f++)
            {
                int ci = m.comp[f];
                var p = m.fc[f];
                Color c;
                float g = 0f;
                switch (ci)
                {
                    case 0:
                        if (p.y > 0.885f) { c = Color.Lerp(hair, Color.white, (float)rnd.NextDouble() * 0.4f); g = 0.55f; hairN++; }
                        else if (inFace(f) && Mathf.Abs(p.x) > 0.012f && m.conc[f] >= eyeTh) { c = eye; g = 1f; eyes++; }
                        else if (p.y < 0.45f) c = SwampPainter.Vary(rnd, gold, 0.08f);                        // altın kuşak
                        else if (p.y < 0.8f && m.conc[f] >= runeTh) { c = rune; g = 0.75f; runes++; }      // kas çizgilerinde altın rünler
                        else
                        {
                            c = SwampPainter.Vary(rnd, skin, 0.05f);
                            c = Color.Lerp(c, skinShade, Mathf.Clamp01(-m.fn[f].y) * 0.45f);
                        }
                        break;
                    case 1:
                    {
                        // Fırtına hortumu: sarmal açık/koyu bantlar, içinde yer yer şimşek
                        float a = Mathf.Atan2(p.z, p.x);
                        float s = 0.5f + 0.5f * Mathf.Sin(a * 2f + p.y * 26f);
                        c = Color.Lerp(cloudDark, cloudLight, s * s);
                        if (s > 0.9f && rnd.NextDouble() < 0.18f) { c = electric; g = 0.8f; }
                        break;
                    }
                    case 6: case 7: case 8: c = gold; g = 0.7f; break;                                       // baş halesi, bilek halkaları
                    case 2: case 5: case 9: case 10: c = electric; g = 0.8f; break;                          // elektrik haleleri
                    case 3: case 4: c = Color.Lerp(skin, electric, 0.35f); g = 0.35f; break;                 // elektrik yüklü eller
                    default: c = bolt; g = 1f; break;                                                        // küçük şimşek parçaları
                }
                col[f] = c;
                glow[f] = g;
            }
            SwampPainter.Save(m, col, glow, outPath);
            return $"{outPath}: {m.T} üçgen | göz {eyes} | rün {runes} | saç {hairN}";
        }

        public static string PaintLancer(string fbxPath, string outPath)
        {
            var m = SwampPainter.Load(fbxPath);
            // Mızrak yüzünden model yana kaymış: pivotu gövdeye (göğüs zırhı) al
            var chest = m.compBounds[5].center;
            var shift = new Vector3(chest.x, 0f, chest.z);
            for (int i = 0; i < m.v.Length; i++) m.v[i] -= shift;
            for (int f = 0; f < m.T; f++) m.fc[f] -= shift;

            var rnd = new System.Random(6);
            var col = new Color[m.T];
            var glow = new float[m.T];
            var navy = new Color(0.13f, 0.18f, 0.45f);
            var navyDark = new Color(0.08f, 0.1f, 0.26f);
            var gold = new Color(0.95f, 0.72f, 0.26f);
            var goldDark = new Color(0.6f, 0.4f, 0.14f);
            var goldLight = new Color(1f, 0.88f, 0.5f);
            var visor = new Color(0.5f, 0.9f, 1f);
            var wing = new Color(1f, 0.9f, 0.62f);
            var tip = new Color(0.6f, 0.9f, 1f);
            int vis = 0;
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
                        if (Mathf.Abs(p.x) > 0.3f && p.y > 0.55f) c = gold;                             // altın eldivenler
                        else if (p.y > 0.53f && p.y < 0.575f) c = gold;                                   // kemer
                        else if (p.y < 0.42f) c = goldDark;                                                // tunik eteği altın şerit
                        else c = SwampPainter.Vary(rnd, navy, 0.06f);
                        break;
                    case 2:
                        // Miğfer: altın; ön yüzde parlayan vizör yarığı, altında koyu yüz
                        if (n.z > 0.45f && p.y > 0.895f && p.y < 0.935f) { c = visor; g = 0.9f; vis++; }
                        else if (n.z > 0.4f && p.y <= 0.895f) c = navyDark;
                        else c = n.y > 0.5f ? goldLight : gold;
                        break;
                    case 5: c = n.y > 0.4f ? goldLight : gold; break;                                     // göğüs zırhı
                    case 6: case 7: c = n.y > 0.4f ? goldLight : gold; break;                             // omuzluklar
                    case 1: c = SwampPainter.Vary(rnd, navyDark, 0.06f); break;                           // bacaklar
                    case 8: case 9: c = gold; break;                                                      // dizlikler
                    case 3: case 4: c = goldDark; break;                                                  // çizmeler
                    case 10: case 11: c = wing; g = 0.3f; break;                                          // küçük altın kanatlar
                    case 13: c = gold; break;                                                             // mızrak sapı
                    default: c = tip; g = 0.85f; break;                                                   // elektrikli mızrak ucu
                }
                col[f] = SwampPainter.Vary(rnd, c, 0.04f);
                glow[f] = g;
            }
            SwampPainter.Save(m, col, glow, outPath);
            return $"{outPath}: {m.T} üçgen | vizör {vis} | kayma {shift}";
        }
    }
}
