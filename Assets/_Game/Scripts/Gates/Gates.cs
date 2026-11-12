using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    public enum GateType { Soldiers, Multiply, FireRate, Damage, ExtraBullet, Pierce, Crit }

    [System.Serializable]
    public class GateDef
    {
        public GateType type;
        public int value;
        [Tooltip("Vurulabilir kapı: her birkaç isabette değeri 1 artar")]
        public bool shootable;
        public int maxValue = 5;

        public GateDef(GateType type, int value = 0, bool shootable = false)
        {
            this.type = type;
            this.value = value;
            this.shootable = shootable;
        }

        public bool IsPositive => type switch
        {
            GateType.Soldiers or GateType.FireRate or GateType.Damage or GateType.Crit => value > 0,
            _ => true,
        };

        /// <summary>Asker kapıları mavi, silah güçlendirmeleri altın/turuncu renkte görünür.</summary>
        public bool IsWeaponBuff => type != GateType.Soldiers && type != GateType.Multiply;

        string Signed => value > 0 ? "+" + value : value.ToString();

        public string Label => type switch
        {
            GateType.Soldiers => Signed,
            GateType.Multiply => "x" + value,
            GateType.FireRate => Signed + Loc.T("%\nATIŞ HIZI", "%\nFIRE RATE"),
            GateType.Damage => Signed + Loc.T("%\nHASAR", "%\nDAMAGE"),
            GateType.ExtraBullet => Loc.T("+1\nMERMİ", "+1\nBULLET"),
            GateType.Pierce => Loc.T("DELİCİ\nMERMİ", "PIERCING\nBULLET"),
            GateType.Crit => Signed + Loc.T("%\nKRİTİK", "%\nCRIT"),
            _ => "?",
        };

        public string ShortLabel => type switch
        {
            GateType.Soldiers => Signed + Loc.T(" ASKER", " SOLDIERS"),
            GateType.Multiply => "x" + value + Loc.T(" ASKER", " SOLDIERS"),
            _ => Label.Replace("\n", " "),
        };

        public GateDef Clone() => new GateDef(type, value, shootable) { maxValue = maxValue };

        public static GateDef Soldiers(int n, bool shootable = false) => new GateDef(GateType.Soldiers, n, shootable);
        public static GateDef Multiply(int n) => new GateDef(GateType.Multiply, n);
        public static GateDef FireRate(int pct) => new GateDef(GateType.FireRate, pct);
        public static GateDef Damage(int pct) => new GateDef(GateType.Damage, pct);
        public static GateDef ExtraBullet() => new GateDef(GateType.ExtraBullet, 1);
        public static GateDef Pierce() => new GateDef(GateType.Pierce, 1);
        public static GateDef Crit(int pct) => new GateDef(GateType.Crit, pct);
    }

    /// <summary>
    /// 3 şeritli kapı sırası. Şeritlerden biri her zaman boştur.
    /// Sıra koridordan aşağı akar; karakter hangi şeritteyse o şeritteki kapıyı alır.
    /// </summary>
    public class GateRow : MonoBehaviour
    {
        public int hitsPerIncrement = 4;

        readonly Lane[] lanes = new Lane[GameManager.LaneCount];
        GameManager gm;
        bool introDone;
        const float IntroDistance = 17f;

        /// <summary>Sıradaki görülmemiş türü anlatır. Kart meşgulse sonraki karede tekrar dener.</summary>
        bool TryIntro()
        {
            foreach (var l in lanes)
            {
                if (l == null || l.def == null) continue;
                BuffInfo.Entry e;
                if (!l.def.IsPositive) e = l.def.IsWeaponBuff ? BuffInfo.WeakGate : BuffInfo.Negative;
                else if (BuffInfo.HasSymbol(l.def.type)) e = BuffInfo.For(l.def.type);
                else continue;
                if (BuffInfo.Seen(e.key)) continue;
                if (!gm.Hud.ShowBuffIntro(e)) return false;
                BuffInfo.MarkSeen(e.key);
                return false;   // aynı sıradaki diğer yeni tür kart kapanınca anlatılır
            }
            return true;
        }

        class Lane
        {
            public GateDef def; // null = boş şerit
            public GateDesigns.Parts parts;
            public TextMesh text;
            public TextMesh[] outline;
            public SpriteRenderer icon, badge;
            public int hits;
        }

        static readonly Color SoldierBlue = new Color(0.15f, 0.45f, 0.95f);
        static readonly Color BuffGold = new Color(0.95f, 0.6f, 0.1f);
        static readonly Color NegativeRed = new Color(0.9f, 0.2f, 0.2f);
        static readonly Color PostColor = new Color(0.9f, 0.92f, 0.95f);

        static Color ColorFor(GateDef d) => !d.IsPositive ? NegativeRed : d.IsWeaponBuff ? BuffGold : SoldierBlue;

        /// <summary>
        /// Panel yazısı: menüdeki net Baloo yazı tipi, koyu dış çizgi (arkada 8 yöne kaydırılmış koyu kopyalar),
        /// kameraya doğru 30° eğik ve taşın biraz önünde (eğilince taşa gömülmesin).
        /// </summary>
        void MakeText(Lane lane, float cx, string label, bool big) => MakeText(lane, cx, label, big ? 112 : 54, 0f, big);

        /// <summary>
        /// Güçlendirme kapısı: solda koyu yuvarlak rozet içinde sembol, sağında büyük değer (+20%).
        /// Yazı yerine sembol kullanılır; anlamı ilk görüşte açıklama kartında ve menüdeki REHBER'de anlatılır.
        /// </summary>
        void MakeSymbol(Lane lane, float cx, GateDef def)
        {
            string val = BuffInfo.Value(def);
            MakeText(lane, cx, val, ValueSize(val), -0.15f, true);
            // değer sembolün sağından başlar, sembolün üstüne binmez
            lane.text.anchor = TextAnchor.MiddleLeft;
            foreach (var o in lane.outline) o.anchor = TextAnchor.MiddleLeft;
            var pivot = lane.text.transform.parent;
            // kısa değerlerde (+1) sembol + sayı grubu panelin ortasına kayar
            pivot.localPosition += new Vector3(val.Length >= 4 ? -0.06f : val.Length == 3 ? 0.04f : 0.14f, 0f, 0f);

            var badgeGo = new GameObject("Badge");
            badgeGo.transform.SetParent(pivot, false);
            badgeGo.transform.localPosition = new Vector3(-0.5f, 0f, 0.015f);
            var badge = badgeGo.AddComponent<SpriteRenderer>();
            badge.sprite = UiKit.Circle;
            badge.color = new Color(0.03f, 0.04f, 0.08f, 0.94f);
            badge.sortingOrder = 1;
            badgeGo.transform.localScale = Vector3.one * (0.62f / 1.28f);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(pivot, false);
            iconGo.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
            var icon = iconGo.AddComponent<SpriteRenderer>();
            icon.sprite = BuffInfo.Icon(def.type);
            icon.color = Color.white;
            icon.sortingOrder = 2;
            iconGo.transform.localScale = Vector3.one * (0.5f / 1.28f);

            lane.badge = badge;
            lane.icon = icon;
        }

        static int ValueSize(string v) => v.Length >= 4 ? 72 : v.Length == 3 ? 86 : 100;

        void SetLabel(Lane lane)
        {
            var d = lane.def;
            string t = BuffInfo.HasSymbol(d.type) ? BuffInfo.Value(d) : d.Label;
            lane.text.text = t;
            foreach (var o in lane.outline) o.text = t;
            if (BuffInfo.HasSymbol(d.type))
            {
                int sz = ValueSize(t);
                lane.text.fontSize = sz;
                foreach (var o in lane.outline) o.fontSize = sz;
            }
        }

        void MakeText(Lane lane, float cx, string label, int size, float offsetX, bool big)
        {
            var font = UiKit.MenuFont != null ? UiKit.MenuFont : Mats.Font;
            const float charSize = 0.062f;
            var pivot = new GameObject("GateText").transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = new Vector3(cx, GateDesigns.PanelY + 0.04f, GateDesigns.FrontZ - 0.2f);
            pivot.localRotation = Quaternion.Euler(30f, 0f, 0f);

            TextMesh Make(string name, Vector3 off, Color c)
            {
                var go = new GameObject(name);
                go.transform.SetParent(pivot, false);
                go.transform.localPosition = off + new Vector3(offsetX, 0f, 0f);
                var tm = go.AddComponent<TextMesh>();
                tm.font = font;
                tm.text = label;
                tm.fontSize = size;
                tm.characterSize = charSize;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.lineSpacing = 0.85f;
                tm.color = c;
                tm.fontStyle = font == Mats.Font ? FontStyle.Bold : FontStyle.Normal;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                return tm;
            }

            // koyu dış çizgi: ana yazının hemen arkasında 8 yöne kaydırılmış kopyalar
            float d = big ? 0.03f : 0.022f;
            var outline = new List<TextMesh>();
            var dark = new Color(0.02f, 0.03f, 0.06f, 0.95f);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI * 0.25f;
                outline.Add(Make("Outline", new Vector3(Mathf.Cos(a) * d, Mathf.Sin(a) * d, 0.02f), dark));
            }
            lane.outline = outline.ToArray();
            lane.text = Make("Label", Vector3.zero, Color.white);
        }

        public void Setup(GateDef[] defs, Vector3 pos)
        {
            gm = GameManager.I;
            transform.position = pos;
            float lw = gm.LaneWidth;
            for (int i = 0; i < lanes.Length; i++)
            {
                var def = i < defs.Length ? defs[i] : null;
                lanes[i] = new Lane { def = def };
                if (def == null) continue;
                float cx = gm.LaneCenter(i);
                float w = lw - 0.35f;
                // Bel hizası taş blok: üstten su akar, önde oyulmuş renkli panel + parlayan rün çerçeve
                var parts = GateDesigns.Build(transform, cx, w);
                GateDesigns.SetColor(parts, ColorFor(def));
                bool big = def.type == GateType.Soldiers || def.type == GateType.Multiply;
                lanes[i].parts = parts;
                if (BuffInfo.HasSymbol(def.type)) MakeSymbol(lanes[i], cx, def);
                else MakeText(lanes[i], cx, def.Label, big);
            }
        }

        void Update()
        {
            if (gm == null || !gm.Playing) return;
            var p = transform.position;
            p.z -= gm.scrollSpeed * Time.deltaTime;
            transform.position = p;

            // Yeni bir güçlendirme türü ilk kez yaklaşınca açıklama kartı (oyun kısa süre yavaşlar)
            if (!introDone && p.z - gm.Player.Z < IntroDistance) introDone = TryIntro();

            if (p.z <= gm.Player.Z + 0.2f)
            {
                var chosen = lanes[gm.LaneOf(gm.Player.X)].def;
                if (chosen != null)
                {
                    Effects.GatePickup(gm.Player.transform.position, ColorFor(chosen), chosen.IsPositive);
                    Sfx.Play(!chosen.IsPositive ? Snd.GateBad : chosen.IsWeaponBuff ? Snd.GatePower : Snd.GateSoldiers);
                    gm.ApplyGate(chosen);
                }
                gm.Gates.Remove(this);
                Destroy(gameObject);
            }
        }

        /// <summary>Mermi bir panoya çarptı mı? Sadece vurulabilir panolar mermiyi durdurur.</summary>
        public bool TryHit(Vector3 bulletPos)
        {
            if (Mathf.Abs(bulletPos.z - transform.position.z) > 0.4f) return false;
            var lane = lanes[gm.LaneOf(bulletPos.x)];
            var d = lane.def;
            if (d == null || !d.shootable || d.value >= d.maxValue) return false;
            lane.hits++;
            if (lane.hits >= hitsPerIncrement)
            {
                lane.hits = 0;
                d.value++;
                if (d.value == 0) d.value = 1;
                SetLabel(lane);
                GateDesigns.SetColor(lane.parts, ColorFor(d));
            }
            return true;
        }
    }

    public class GateManager : MonoBehaviour
    {
        readonly List<GateRow> active = new();

        /// <summary>İki kapıyı rastgele iki şeride koyar, kalan şerit boş kalır.</summary>
        public void SpawnRow(GateDef a, GateDef b)
        {
            int empty = Random.Range(0, GameManager.LaneCount);
            var defs = new GateDef[GameManager.LaneCount];
            var items = Random.value < 0.5f ? new[] { a, b } : new[] { b, a };
            int k = 0;
            for (int i = 0; i < defs.Length; i++)
                defs[i] = i == empty ? null : items[k++];
            SpawnRow(defs);
        }

        /// <summary>Şeritleri tek tek belirterek sıra oluşturur (null = boş şerit).</summary>
        public void SpawnRow(GateDef[] lanes)
        {
            var gm = GameManager.I;
            var go = new GameObject("GateRow");
            go.transform.SetParent(transform, false);
            var row = go.AddComponent<GateRow>();
            var copies = new GateDef[lanes.Length];
            for (int i = 0; i < lanes.Length; i++) copies[i] = lanes[i]?.Clone();
            row.Setup(copies, new Vector3(0f, 0f, gm.spawnZ));
            active.Add(row);
        }

        public void Remove(GateRow r) => active.Remove(r);

        /// <summary>Sahadaki tüm kapı sıralarını kaldırır (Sonsuz Savunma'da diyar değişirken).</summary>
        public void ClearAll()
        {
            foreach (var r in active) if (r != null) Destroy(r.gameObject);
            active.Clear();
        }

        public bool TryBulletHit(Vector3 pos)
        {
            for (int i = 0; i < active.Count; i++)
                if (active[i].TryHit(pos)) return true;
            return false;
        }
    }
}
