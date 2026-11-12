using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Düşman mermisi. Düz aşağı (oyuncuya doğru) gider; şerit değiştirerek kaçılabilir.
    /// Zıpkın: hızlı ve delici (2 askere kadar). Nişancı mermisi: yavaş, parlak, tek askere isabet eder.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        public enum Kind { Harpoon, Bolt }

        public Kind kind;
        public float speed = 13f;
        public int maxSoldierHits = 2;
        public float playerDamage = 8f;

        int hitsLeft;
        EnemyManager owner;
        GameManager gm;
        Transform glow;
        Transform spin;
        bool tumble, drip, freezer, chain, boomerang, returning, hitPlayer;
        float originZ;
        static readonly System.Collections.Generic.List<Vector3> chainHits = new System.Collections.Generic.List<Vector3>();
        float trailT;
        float pulse;

        public static EnemyProjectile CreateHarpoon()
        {
            var go = new GameObject("Harpoon");
            var h = go.AddComponent<EnemyProjectile>();
            h.kind = Kind.Harpoon;
            h.speed = 13f;
            h.maxSoldierHits = 2;
            h.playerDamage = 12f;
            // Zıpkıncı'nın elindeki mızrağa benzeyen model (uç oyuncuya doğru), hafifçe kendi etrafında döner
            var theme = GameManager.I != null ? GameManager.I.Theme : WorldTheme.Underwater;
            bool volcano = theme == WorldTheme.Volcano, swamp = theme == WorldTheme.Swamp, frost = theme == WorldTheme.Frost, sky = theme == WorldTheme.Sky, desert = theme == WorldTheme.Desert;
            var vis = desert ? CharacterDesigns.MakeVisual("Chakram", DesertCreatures.Chakram, go.transform)
                : sky ? FxDesigns.MakeFx("Bolt", SkyCreatures.LightningBolt, go.transform)
                : volcano ? CharacterDesigns.MakeVisual("Axe", FxDesigns.FlamingAxe, go.transform)
                : frost ? CharacterDesigns.MakeVisual("IceShard", FrostCreatures.IceShard, go.transform)
                : swamp ? CharacterDesigns.MakeVisual("Arrow", SwampCreatures.PoisonArrow, go.transform)
                : CharacterDesigns.MakeVisual("Spear", FxDesigns.Harpoon, go.transform);
            vis.transform.localScale = desert ? Vector3.one * 2.5f : sky ? new Vector3(1.8f, 1.8f, 2.2f) : volcano ? Vector3.one * 1.6f : swamp ? Vector3.one * 2f : frost ? Vector3.one * 2.1f : new Vector3(2.5f, 2.5f, 2.1f);
            if (!desert && !sky && !volcano && !frost && !swamp)
            {
                // Su altı: mızrak büyük ve arkasında parlak kırmızı-beyaz iz bırakır (yengeç kalabalığında da seçilsin)
                FxDesigns.MakeFx("Trail", FxDesigns.HarpoonTrail, go.transform);
            }
            if (desert)
            {
                // Çöl: altın çakram sarı kumda kayboluyordu. Daha büyük + altında parlak turkuaz hale + kırmızı-beyaz iz
                var halo = Mats.Prim(PrimitiveType.Cylinder, new Color(0.15f, 0.95f, 1f, 0.55f), go.transform, Vector3.zero, new Vector3(1.5f, 0.015f, 1.5f), true);
                halo.name = "Halo";
                FxDesigns.MakeFx("Trail", FxDesigns.HarpoonTrail, go.transform);
            }
            h.spin = vis.transform;
            h.tumble = volcano;
            h.drip = swamp;
            h.freezer = frost;
            h.chain = sky;
            if (sky) h.speed = 14f;
            h.boomerang = desert;
            if (desert) { h.speed = 11f; h.maxSoldierHits = 1; }   // her geçişte 1 asker: gidiş + dönüş en fazla 2 (diğer dünyalardaki mızrakla aynı; bot testi 25 Ekim)
            return h;
        }

        public static EnemyProjectile CreateBolt()
        {
            var go = new GameObject("Bolt");
            var b = go.AddComponent<EnemyProjectile>();
            b.kind = Kind.Bolt;
            b.speed = 7.5f;          // yavaş: görüp kaçmak mümkün
            b.maxSoldierHits = 1;
            b.playerDamage = 12f;
            // Parlak kırmızı-turuncu, nabız gibi büyüyüp küçülen mermi (uzaktan kolay fark edilsin)
            var core = Mats.Prim(PrimitiveType.Sphere, new Color(1f, 0.3f, 0.05f), go.transform, Vector3.zero, new Vector3(0.45f, 0.45f, 0.6f));
            core.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            b.glow = core.transform;
            return b;
        }

        public void Fire(Vector3 pos, EnemyManager mgr)
        {
            owner = mgr;
            gm = GameManager.I;
            transform.position = pos;
            hitsLeft = maxSoldierHits;
            originZ = pos.z;
            returning = false;
            hitPlayer = false;
        }

        void Update()
        {
            if (gm == null || !gm.Playing) return;
            var p = transform.position;
            if (boomerang)
            {
                // Çakram: oyuncunun arkasına kadar gider, döner ve atıldığı yere geri uçar
                p.z += (returning ? 1f : -1f) * speed * Time.deltaTime;
                if (!returning && p.z <= gm.Player.Z - 2.5f) { returning = true; hitsLeft = 0; }   // dönüşte askerlere vurmaz (yalnız kahramana)
                transform.position = p;
                spin.localRotation = Quaternion.Euler(0f, Time.time * 1100f, 0f);
                trailT -= Time.deltaTime;
                if (trailT <= 0f) { trailT = 0.03f; Effects.GoldSpark(p + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f))); }
                if (hitsLeft > 0 && gm.Army.HitAt(p, 0.5f))
                {
                    hitsLeft--;
                    Effects.SandPuff(p, 0.8f);
                    Sfx.Play(Snd.Splash, 0.6f);
                }
                if (!hitPlayer && Mathf.Abs(p.z - gm.Player.Z) < 0.5f && Mathf.Abs(p.x - gm.Player.X) < 0.7f)
                {
                    hitPlayer = true;
                    gm.Player.TakeDamage(playerDamage);
                    Effects.Splash(p, 1f);
                }
                if (returning && p.z >= originZ) owner.ReleaseProjectile(this);
                return;
            }
            p.z -= speed * Time.deltaTime;
            transform.position = p;

            if (spin != null)
            {
                if (tumble)
                {
                    // Balta: öne doğru takla atarak döner, arkasında kıvılcım izi bırakır
                    spin.localRotation = Quaternion.Euler(-Time.time * 900f, 0f, 0f);
                    trailT -= Time.deltaTime;
                    if (trailT <= 0f) { trailT = 0.035f; Effects.Ember(p); }
                }
                else spin.localRotation = Quaternion.Euler(0f, 0f, Time.time * 540f);
                if (freezer)
                {
                    // Buz kristali: arkasında buz pırıltısı
                    trailT -= Time.deltaTime;
                    if (trailT <= 0f) { trailT = 0.03f; Effects.FrostSparkle(p + new Vector3(Random.Range(-0.1f, 0.1f), 0f, 0.35f)); }
                }
                if (chain)
                {
                    // Yıldırım: arkasında elektrik pırıltısı
                    trailT -= Time.deltaTime;
                    if (trailT <= 0f) { trailT = 0.03f; Effects.SkySpark(p + new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(-0.1f, 0.1f), 0.4f)); }
                }
                if (drip)
                {
                    // Zehirli ok: arkasında yeşil zehir damlaları
                    trailT -= Time.deltaTime;
                    if (trailT <= 0f) { trailT = 0.035f; Effects.SporeDrip(p + new Vector3(0f, -0.05f, 0.4f)); }
                }
            }

            if (glow != null)
            {
                pulse += Time.deltaTime * 10f;
                float s = 0.45f + 0.1f * Mathf.Sin(pulse);
                glow.localScale = new Vector3(s, s, s * 1.35f);
            }

            if (freezer && gm.Army.FreezeAt(p, 0.45f, 1.05f, 2f) > 0)
            {
                // Ayaz Muhafızı: askeri öldürmez, çevresiyle birlikte dondurur (1.5 sn ateş edemezler)
                Effects.IceShatter(p);
                Sfx.Play(Snd.Splash, 0.8f);
                owner.ReleaseProjectile(this);
                return;
            }
            if (chain && gm.Army.ChainAt(p, 0.45f, 1, 1.6f, chainHits) > 0)   // 1 sekme: atış başına 2 asker (diğer dünyalardaki mızrakla aynı)
            {
                // Şimşek Süvarisi: vurduğu askerden en yakın 1 askere seker (zincir yıldırım)
                var prev = p;
                foreach (var h in chainHits)
                {
                    var hp = h + Vector3.up * 0.6f;
                    Effects.Lightning(prev, hp, 0.12f, new Color(0.55f, 0.85f, 1f), 0.25f, 0.2f);
                    Effects.Zap(hp, 0.8f);
                    prev = hp;
                }
                Sfx.Play(Snd.Splash, 0.8f);
                owner.ReleaseProjectile(this);
                return;
            }
            if (!freezer && !chain && hitsLeft > 0 && gm.Army.HitAt(p, 0.45f))
            {
                hitsLeft--;
                Effects.Splash(p, 0.7f);
                Sfx.Play(Snd.Splash, 0.7f);
                if (hitsLeft <= 0 && kind == Kind.Bolt) { owner.ReleaseProjectile(this); return; }
            }

            if (p.z <= gm.Player.Z + 0.5f && p.z > gm.Player.Z - 0.5f && Mathf.Abs(p.x - gm.Player.X) < 0.7f)
            {
                gm.Player.TakeDamage(playerDamage);
                Effects.Splash(p, 1.2f);
                Sfx.Play(Snd.Splash);
                owner.ReleaseProjectile(this);
                return;
            }
            if (p.z < -6f) owner.ReleaseProjectile(this);
        }
    }
}
