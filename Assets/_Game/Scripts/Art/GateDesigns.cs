using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Buff kapıları için bel hizası taş blok: üstündeki havuzdan su akar, ön kenarın iki yanından ve
    /// yan yüzlerden dökülür. Ön yüzde oyulmuş renkli panel (yazı burada) ve türüne göre parlayan rün çerçeve.
    /// Taş/su/panel/rün mesh'leri tüm bloklarda ortak (önbellekli); renk MaterialPropertyBlock ile verilir.
    /// </summary>
    public static class GateDesigns
    {
        public const float TopY = 1.1f;
        public const float FrontZ = -0.3f;
        public const float PanelY = 0.57f;

        static float cachedW = -1f;
        static WorldTheme cachedTheme;
        static Mesh stone, water, panel, rune;

        static readonly Color Stone = new Color(0.5f, 0.56f, 0.55f);
        static readonly Color StoneDark = new Color(0.3f, 0.36f, 0.38f);
        static readonly Color StoneLight = new Color(0.62f, 0.67f, 0.64f);
        static readonly Color Moss = new Color(0.25f, 0.45f, 0.24f);

        public class Parts
        {
            public Renderer panel;
            public Renderer rune;
        }

        public static Parts Build(Transform parent, float cx, float w)
        {
            Ensure(w);
            var root = new GameObject("GateBlock").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(cx, 0f, 0f);
            WaterFx.MakeEnv("Stone", stone, root, true);
            WaterFx.MakeWater("Water", water, root);
            var p = new Parts
            {
                panel = CharacterDesigns.MakeVisual("Panel", panel, root).GetComponent<Renderer>(),
                rune = FxDesigns.MakeFx("Rune", rune, root).GetComponent<Renderer>(),
            };
            p.panel.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return p;
        }

        static readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        /// <summary>Panel (koyu, türün rengi) ve rün çerçevesi (parlak, türün rengi) rengini ayarlar.</summary>
        public static void SetColor(Parts p, Color c)
        {
            mpb.Clear();
            mpb.SetColor("_BaseColor", Color.Lerp(c, new Color(0.25f, 0.3f, 0.32f), 0.25f));
            p.panel.SetPropertyBlock(mpb);
            mpb.Clear();
            mpb.SetColor("_BaseColor", c * 1.6f);
            p.rune.SetPropertyBlock(mpb);
        }

        static void Ensure(float w)
        {
            if (stone != null && water != null && panel != null && rune != null && Mathf.Approximately(w, cachedW) && cachedTheme == WaterFx.Theme) return;
            cachedW = w;
            cachedTheme = WaterFx.Theme;
            bool volcano = WaterFx.Theme == WorldTheme.Volcano;
            bool swampT = WaterFx.Theme == WorldTheme.Swamp;
            bool frostT = WaterFx.Theme == WorldTheme.Frost;
            bool skyT = WaterFx.Theme == WorldTheme.Sky;
            bool desertT = WaterFx.Theme == WorldTheme.Desert;   // Çöl: kumtaşı, altın süs; üstten kum akar   // Gökyüzü: beyaz mermer, altın süsler; üstten elektrikli mavi ışık akar   // Kar: buzlu mavi-beyaz taş, üstü karlı; üstten buz suyu akar   // Bataklık: yosunlu, koyu yeşil-gri taş; üstten bataklık suyu akar
            // Yanardağ: bazalt blok, köşelerde kor, üstten lav akar (sıvı materyali temaya göre)
            var Stone = desertT ? new Color(0.8f, 0.66f, 0.45f) : skyT ? new Color(0.74f, 0.74f, 0.8f) : volcano ? new Color(0.26f, 0.21f, 0.2f) : swampT ? new Color(0.38f, 0.4f, 0.34f) : frostT ? new Color(0.55f, 0.66f, 0.8f) : GateDesigns.Stone;
            var StoneDark = desertT ? new Color(0.5f, 0.36f, 0.22f) : skyT ? new Color(0.42f, 0.42f, 0.54f) : volcano ? new Color(0.13f, 0.1f, 0.1f) : swampT ? new Color(0.2f, 0.22f, 0.18f) : frostT ? new Color(0.3f, 0.38f, 0.5f) : GateDesigns.StoneDark;
            var StoneLight = desertT ? new Color(0.9f, 0.8f, 0.6f) : skyT ? new Color(0.9f, 0.9f, 0.94f) : volcano ? new Color(0.36f, 0.3f, 0.28f) : swampT ? new Color(0.48f, 0.5f, 0.42f) : frostT ? new Color(0.8f, 0.86f, 0.95f) : GateDesigns.StoneLight;
            var Moss = desertT ? new Color(1f, 0.75f, 0.3f, 0.6f) : skyT ? new Color(1f, 0.75f, 0.28f, 0.5f) : volcano ? new Color(0.5f, 0.16f, 0.03f, 0f) : swampT ? new Color(0.36f, 0.62f, 0.18f) : frostT ? new Color(0.9f, 0.94f, 1f) : GateDesigns.Moss;
            float hw = w * 0.5f;
            float pw = w * 0.7f, ph = 0.7f;

            // ── Taş blok ──
            var b = new MeshBuilder();
            b.Box(new Vector3(0f, 0.08f, 0f), new Vector3(w, 0.16f, 0.78f), StoneDark);
            b.Box(new Vector3(0f, 0.57f, 0f), new Vector3(w - 0.14f, 0.82f, 0.6f), Stone, default, 1.02f, 1.02f);
            b.Box(new Vector3(0f, 1.05f, 0f), new Vector3(w, 0.14f, 0.74f), StoneLight);
            for (int s = -1; s <= 1; s += 2)
            {
                b.Box(new Vector3(s * (hw - 0.07f), 1.14f, 0f), new Vector3(0.14f, 0.1f, 0.74f), StoneLight);
                // Ön yüzün dış kısımlarında oyma süs (suyun arkasında)
                b.Box(new Vector3(s * (hw - 0.17f), 0.57f, FrontZ - 0.005f), new Vector3(0.1f, 0.5f, 0.02f), StoneDark);
                // Köşelerde yosun
                b.Sphere(new Vector3(s * (hw - 0.12f), 1.13f, 0.3f), new Vector3(0.16f, 0.07f, 0.12f), Moss, 6, 3, default, true);
            }
            // Panel yuvası (çerçevenin arkası koyu)
            b.Box(new Vector3(0f, PanelY, FrontZ - 0.005f), new Vector3(pw + 0.12f, ph + 0.12f, 0.02f), StoneDark);
            if (volcano)
                for (int s = -1; s <= 1; s += 2)
                {
                    // Yan yüzlerde lav çatlakları
                    b.Box(new Vector3(s * (hw - 0.065f), 0.45f, 0.05f), new Vector3(0.02f, 0.4f, 0.04f), new Color(0.5f, 0.16f, 0.03f, 0f), new Vector3(20f * s, 0f, 0f));
                    b.Box(new Vector3(s * (hw - 0.065f), 0.78f, 0.12f), new Vector3(0.02f, 0.3f, 0.04f), new Color(0.5f, 0.16f, 0.03f, 0f), new Vector3(-25f * s, 0f, 0f));
                }
            stone = b.ToMesh("Gate_Stone");

            // ── Renkli panel (beyaz; renk MaterialPropertyBlock ile) ──
            var pb = new MeshBuilder();
            pb.Box(new Vector3(0f, PanelY, FrontZ - 0.02f), new Vector3(pw, ph, 0.02f), Color.white);
            panel = pb.ToMesh("Gate_Panel");

            // ── Parlayan rün çerçevesi ──
            var rb = new MeshBuilder();
            float fz = FrontZ - 0.035f, t = 0.035f;
            var white = Color.white;
            rb.Box(new Vector3(0f, PanelY + ph * 0.5f + 0.04f, fz), new Vector3(pw + 0.1f, t, 0.01f), white);
            rb.Box(new Vector3(0f, PanelY - ph * 0.5f - 0.04f, fz), new Vector3(pw + 0.1f, t, 0.01f), white);
            for (int s = -1; s <= 1; s += 2)
            {
                rb.Box(new Vector3(s * (pw * 0.5f + 0.04f), PanelY, fz), new Vector3(t, ph + 0.1f, 0.01f), white);
                rb.Box(new Vector3(s * (pw * 0.5f + 0.04f), PanelY + ph * 0.5f + 0.04f, fz - 0.003f), new Vector3(0.1f, 0.1f, 0.01f), white, new Vector3(0f, 0f, 45f));
                rb.Box(new Vector3(s * (pw * 0.5f + 0.04f), PanelY - ph * 0.5f - 0.04f, fz - 0.003f), new Vector3(0.1f, 0.1f, 0.01f), white, new Vector3(0f, 0f, 45f));
            }
            // Üst taştaki küçük rün (uzaktan da renk okunur)
            rb.Box(new Vector3(0f, 1.06f, -0.375f), new Vector3(0.12f, 0.07f, 0.01f), white, new Vector3(0f, 0f, 45f));
            rune = rb.ToMesh("Gate_Rune");

            // ── Su: üstteki havuzdan öne akar, ön kenarın iki dış köşesinden ince şeritler halinde dökülür ──
            float fallX = hw - 0.17f;
            var top = WaterFx.Sheet(new[] { new Vector3(0f, TopY, 0.3f), new Vector3(0f, TopY, 0f), new Vector3(0f, TopY, -0.34f) },
                Vector3.right, w - 0.3f, 6, 0f, 0.1f);
            var parts = new System.Collections.Generic.List<Mesh> { top };
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * fallX;
                parts.Add(WaterFx.Sheet(new[]
                {
                    new Vector3(x, TopY, -0.34f), new Vector3(x, TopY - 0.05f, -0.41f), new Vector3(x, 0.6f, -0.44f), new Vector3(x, 0.03f, -0.46f),
                }, Vector3.right, 0.2f, 1, 0.1f, 0.8f));
            }
            // Önde köpüklü birikinti
            parts.Add(WaterFx.Sheet(new[] { new Vector3(0f, 0.025f, -0.42f), new Vector3(0f, 0.02f, -0.7f) }, Vector3.right, w, 6, 0.6f, 0f));
            water = WaterFx.Combine("Gate_Water", parts.ToArray());
        }
    }
}
