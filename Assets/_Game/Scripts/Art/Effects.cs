using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Oyun içi efektler: isabet kıvılcımı, düşman ölüm patlaması, asker düşmesi, buff alma, su sıçraması.
    /// Mobil dostu: her efekt türü için tek bir ParticleSystem vardır; efektler Emit ile "basılır"
    /// (nesne oluşturma / yok etme yok). Sahne başına bir kez GameManager tarafından kurulur.
    /// </summary>
    public class Effects : MonoBehaviour
    {
        public static Effects I { get; private set; }

        ParticleSystem sparks, flashes, bubbles, chunks, rings, foam, glitter, beams, mist, dust;
        RootSpike[] spikes;
        int spikeNext;
        SkyBolt[] bolts;
        SandWall[] walls;
        int wallNext;
        int boltNext;

        // Düşman türüne göre parça rengi
        static readonly Color CrabRed = new Color(0.95f, 0.38f, 0.25f);
        static readonly Color BrutePurple = new Color(0.62f, 0.4f, 0.7f);
        static readonly Color HarpoonTeal = new Color(0.52f, 0.25f, 0.7f);   // su altı zıpkıncısı artık mor
        static readonly Color BossGold = new Color(1f, 0.75f, 0.35f);
        static readonly Color SoldierBlue = new Color(0.45f, 0.65f, 1f);

        WorldTheme theme;
        bool volcano => theme == WorldTheme.Volcano;
        bool swamp => theme == WorldTheme.Swamp;
        bool frost => theme == WorldTheme.Frost;
        bool sky => theme == WorldTheme.Sky;
        bool desert => theme == WorldTheme.Desert;
        // Temaya göre renkler: Su Altı'nda kabarcık/mavi, Yanardağ'da kor/turuncu, Bataklık'ta zehir yeşili
        Color BubbleC => desert ? new Color(0.95f, 0.78f, 0.45f) : sky ? new Color(0.55f, 0.8f, 1f) : volcano ? new Color(1f, 0.5f, 0.15f) : frost ? new Color(0.75f, 0.92f, 1f) : swamp ? new Color(0.5f, 0.95f, 0.3f) : new Color(0.55f, 0.85f, 1f);
        Color RingC => desert ? new Color(1f, 0.75f, 0.4f) : sky ? new Color(0.5f, 0.75f, 1f) : volcano ? new Color(1f, 0.45f, 0.1f) : frost ? new Color(0.6f, 0.85f, 1f) : swamp ? new Color(0.45f, 0.9f, 0.25f) : new Color(0.4f, 0.8f, 1f);
        Color FoamC => desert ? new Color(0.88f, 0.72f, 0.48f) : sky ? new Color(0.8f, 0.88f, 1f) : volcano ? new Color(1f, 0.6f, 0.2f) : frost ? new Color(0.9f, 0.95f, 1f) : swamp ? new Color(0.45f, 0.62f, 0.3f) : new Color(0.7f, 0.9f, 1f);

        public static void Create(Transform parent)
        {
            var go = new GameObject("Effects");
            go.transform.SetParent(parent, false);
            I = go.AddComponent<Effects>();
            I.theme = GameManager.I != null ? GameManager.I.Theme : WorldTheme.Underwater;
            I.Build();
        }

        void OnDestroy() { if (I == this) I = null; }

        // ─────────────────────────────────────────────────────
        // KULLANIM
        // ─────────────────────────────────────────────────────

        /// <summary>Mermi isabeti: kısa kıvılcım saçılması + küçük parlama.</summary>
        public static void Hit(Vector3 pos, bool fromPlayer, bool bigTarget = false)
        {
            if (I == null) return;
            var c = fromPlayer ? new Color(1f, 0.7f, 0.3f) : new Color(0.45f, 0.85f, 1f);
            Emit(I.sparks, pos, c, fromPlayer ? 5 : 3, bigTarget ? 1.3f : 1f);
            Emit(I.flashes, pos, c, 1, fromPlayer ? 1f : 0.65f);
        }

        /// <summary>Düşman ölümü: kabuk parçaları, kabarcık patlaması, zeminde halka, parlama.</summary>
        public static void EnemyDeath(Vector3 pos, EnemyKind kind)
        {
            if (I == null) return;
            Color c; float s; int n;
            switch (kind)
            {
                case EnemyKind.Brute: c = I.desert ? new Color(0.62f, 0.36f, 0.22f) : I.sky ? new Color(0.9f, 0.64f, 0.22f) : I.volcano ? new Color(0.2f, 0.17f, 0.16f) : I.swamp ? new Color(0.3f, 0.22f, 0.14f) : I.frost ? new Color(0.85f, 0.9f, 0.97f) : BrutePurple; s = 1.5f; n = 10; break;
                case EnemyKind.Harpooner: c = I.desert ? new Color(0.5f, 0.37f, 0.23f) : I.sky ? new Color(0.2f, 0.26f, 0.6f) : I.volcano ? new Color(0.16f, 0.13f, 0.12f) : I.swamp ? new Color(0.22f, 0.36f, 0.18f) : I.frost ? new Color(0.5f, 0.6f, 0.72f) : HarpoonTeal; s = 1.25f; n = 8; break;
                case EnemyKind.Boss: c = BossGold; s = 3.2f; n = 30; break;
                default: c = I.desert ? new Color(0.42f, 0.13f, 0.07f) : I.sky ? new Color(0.32f, 0.33f, 0.48f) : I.volcano ? new Color(0.18f, 0.14f, 0.12f) : I.swamp ? new Color(0.08f, 0.35f, 1f) : I.frost ? new Color(0.2f, 0.2f, 0.24f) : CrabRed; s = 1f; n = 5; break;
            }
            var center = pos + Vector3.up * 0.45f * s;
            Emit(I.chunks, center, c, n, s);
            Emit(I.bubbles, center, I.BubbleC, n + 3, s);
            Emit(I.flashes, center, new Color(1f, 0.8f, 0.55f), 1, 1.5f * s);
            Emit(I.rings, pos + Vector3.up * 0.06f, I.RingC, 1, s);
            if (I.volcano) Emit(I.sparks, center, new Color(1f, 0.55f, 0.15f), 4 + n / 2, s);
            if (I.swamp) Emit(I.mist, center, new Color(0.3f, 0.7f, 0.18f), 1 + n / 4, 0.6f * s);
            if (I.frost) Emit(I.sparks, center, new Color(0.7f, 0.9f, 1f), 3 + n / 2, s);
            if (I.desert && I.dust != null) Emit(I.dust, center, new Color(0.66f, 0.5f, 0.3f), 2 + n / 3, 0.8f * s);   // kum tozu
            if (I.sky)
            {
                // Bulut dağılır + elektrik boşalır
                Emit(I.mist, center, new Color(0.22f, 0.23f, 0.34f), 1 + n / 4, 0.7f * s);
                Emit(I.sparks, center, new Color(0.55f, 0.85f, 1f), 3 + n / 2, s);
            }
            Emit(I.glitter, center, new Color(1f, 0.82f, 0.3f), kind == EnemyKind.Minion ? 1 : 3, 1f);
        }

        /// <summary>Asker düştü: mavi parlama + kabarcık + birkaç zırh parçası.</summary>
        public static void SoldierDeath(Vector3 pos)
        {
            if (I == null) return;
            var center = pos + Vector3.up * 0.5f;
            Emit(I.chunks, center, SoldierBlue, 3, 0.7f);
            Emit(I.bubbles, center, I.BubbleC, 4, 0.8f);
            Emit(I.flashes, center, new Color(0.5f, 0.7f, 1f), 1, 0.9f);
        }

        /// <summary>Yeni asker katıldı: küçük mavi pırıltı.</summary>
        public static void SoldierSpawn(Vector3 pos)
        {
            if (I == null) return;
            Emit(I.glitter, pos + Vector3.up * 0.5f, new Color(0.5f, 0.8f, 1f), 2, 0.8f);
        }

        /// <summary>Buff bloğundan geçildi: türün renginde ışık sütunu, halka, yükselen pırıltılar ve su sıçraması.</summary>
        public static void GatePickup(Vector3 pos, Color c, bool positive)
        {
            if (I == null) return;
            Emit(I.beams, pos, c, 1, 1f);
            Emit(I.rings, pos + Vector3.up * 0.06f, c, 1, 1.8f);
            Emit(I.glitter, pos + Vector3.up * 0.8f, c, positive ? 18 : 8, 1.2f);
            Emit(I.foam, pos + Vector3.up * 0.2f, I.FoamC, 10, 1f);
        }

        /// <summary>Su sıçraması (mızrak isabeti, oyuncu hasarı).</summary>
        public static void Splash(Vector3 pos, float scale = 1f)
        {
            if (I == null) return;
            Emit(I.foam, pos, I.FoamC, Mathf.RoundToInt(8 * scale), scale);
            Emit(I.bubbles, pos, I.BubbleC, 4, scale);
        }

        /// <summary>Tek kor kıvılcımı (alevli balta izi, lav uyarısı).</summary>
        public static void Ember(Vector3 pos)
        {
            if (I == null) return;
            Emit(I.glitter, pos, new Color(1f, Random.Range(0.35f, 0.6f), 0.1f), 1, Random.Range(0.6f, 1.1f));
        }

        /// <summary>Lav fışkırması (Magma Golem saldırısı): şerit boyunca ateş sütunları, kıvılcım, kaya parçası, halka.</summary>
        public static void LavaEruption(Vector3 center, float width)
        {
            if (I == null) return;
            var fire = new Color(1f, 0.45f, 0.08f);
            for (int i = 0; i < 4; i++)
            {
                var p = center + new Vector3(Random.Range(-width * 0.25f, width * 0.25f), 0f, -3f + i * 3f);
                Emit(I.beams, p, fire, 1, 1.3f);
                Emit(I.rings, p + Vector3.up * 0.06f, fire, 1, 2.2f);
                Emit(I.flashes, p + Vector3.up * 0.6f, new Color(1f, 0.7f, 0.3f), 1, 3f);
                Emit(I.glitter, p + Vector3.up * 0.5f, new Color(1f, 0.55f, 0.12f), 10, 1.4f);
                Emit(I.sparks, p + Vector3.up * 0.3f, new Color(1f, 0.6f, 0.2f), 6, 1.6f);
                Emit(I.chunks, p + Vector3.up * 0.3f, new Color(0.15f, 0.12f, 0.11f), 3, 1.4f);
            }
        }

        /// <summary>Tek zehir damlası/pırıltısı (zehirli ok izi, kök uyarısı).</summary>
        public static void PoisonDrip(Vector3 pos)
        {
            if (I == null) return;
            Emit(I.glitter, pos, new Color(0.45f, Random.Range(0.85f, 1f), 0.2f), 1, Random.Range(0.6f, 1f));
        }

        /// <summary>Buz pırıltısı (buz kristali izi, gürz uyarısı).</summary>
        public static void FrostSparkle(Vector3 pos)
        {
            if (I == null) return;
            Emit(I.glitter, pos, new Color(0.7f, Random.Range(0.88f, 1f), 1f), 1, Random.Range(0.6f, 1f));
        }

        /// <summary>Zırhlı düşmanın kalkanı kırıldı: çelik kıvılcımları ve parlama.</summary>
        public static void ShieldBreak(Vector3 pos)
        {
            if (I == null) return;
            Emit(I.sparks, pos, new Color(0.85f, 0.9f, 1f), 9, 1.1f);
            Emit(I.sparks, pos, new Color(1f, 0.8f, 0.3f), 4, 0.9f);
            Emit(I.flashes, pos, new Color(0.9f, 0.95f, 1f), 1, 1.3f);
        }

        /// <summary>Buz kristali kırıldı / asker dondu: buz parçaları, pırıltı, halka.</summary>
        public static void IceShatter(Vector3 pos, float scale = 1f)
        {
            if (I == null) return;
            Emit(I.sparks, pos, new Color(0.75f, 0.95f, 1f), 8, scale);
            Emit(I.glitter, pos, new Color(0.7f, 0.92f, 1f), 6, scale);
            Emit(I.flashes, pos, new Color(0.6f, 0.85f, 1f), 1, 1.2f * scale);
            Emit(I.rings, new Vector3(pos.x, 0.06f, pos.z), new Color(0.55f, 0.85f, 1f), 1, 1.2f * scale);
        }

        /// <summary>Buzul Gürzü (Yeti): şerit boyunca yerden sivri buz dikenleri fırlar, kar ve buz saçılır.</summary>
        public static void IceEruption(Vector3 center, float width)
        {
            if (I == null || I.spikes == null) return;
            for (int i = 0; i < 4; i++)
            {
                var p = center + new Vector3(Random.Range(-width * 0.2f, width * 0.2f), 0f, -3f + i * 3f);
                var sp = I.spikes[I.spikeNext++ % I.spikes.Length];
                sp.Play(p, Random.Range(0f, 360f), width * Random.Range(0.42f, 0.5f), i * 0.05f);
                Emit(I.foam, p + Vector3.up * 0.1f, new Color(0.9f, 0.95f, 1f), 10, 1.5f);
                Emit(I.sparks, p + Vector3.up * 0.4f, new Color(0.7f, 0.92f, 1f), 6, 1.6f);
                Emit(I.rings, p + Vector3.up * 0.06f, new Color(0.55f, 0.85f, 1f), 1, 2.2f);
                Emit(I.flashes, p + Vector3.up * 0.6f, new Color(0.6f, 0.85f, 1f), 1, 2.5f);
            }
        }

        /// <summary>Mor mantar sporu damlası (zehirli ok izi).</summary>
        public static void SporeDrip(Vector3 pos)
        {
            if (I == null) return;
            Emit(I.glitter, pos, new Color(Random.Range(0.75f, 0.95f), 0.3f, 1f), 1, Random.Range(0.7f, 1.1f));
        }

        /// <summary>Gözlerden yükselen zehirli yeşil sis (Çürümüş Ent).</summary>
        public static void ToxicMist(Vector3 pos)
        {
            if (I == null) return;
            Emit(I.mist, pos, new Color(0.3f, 0.75f, 0.2f), 1, 1f);
        }

        /// <summary>Bataklıkta süzülen boss'un önündeki çamur sıçraması ve halka.</summary>
        public static void MudWake(Vector3 pos, float scale)
        {
            if (I == null) return;
            Emit(I.foam, pos, new Color(0.3f, 0.36f, 0.18f), 3, scale);
            if (Random.value < 0.5f) Emit(I.rings, pos, new Color(0.35f, 0.55f, 0.2f), 1, scale * 1.6f);
            if (Random.value < 0.4f) Emit(I.bubbles, pos, new Color(0.45f, 0.85f, 0.25f), 2, scale);
        }

        /// <summary>Kök fışkırması (Kök Kralı saldırısı): şerit boyunca yerden sivri kökler fırlar; çamur ve zehir saçılır.</summary>
        public static void RootEruption(Vector3 center, float width)
        {
            if (I == null || I.spikes == null) return;
            for (int i = 0; i < 4; i++)
            {
                var p = center + new Vector3(Random.Range(-width * 0.2f, width * 0.2f), 0f, -3f + i * 3f);
                var sp = I.spikes[I.spikeNext++ % I.spikes.Length];
                sp.Play(p, Random.Range(0f, 360f), width * Random.Range(0.42f, 0.5f), i * 0.05f);
                Emit(I.chunks, p + Vector3.up * 0.3f, new Color(0.22f, 0.16f, 0.1f), 4, 1.3f);
                Emit(I.foam, p + Vector3.up * 0.1f, new Color(0.35f, 0.45f, 0.2f), 8, 1.4f);
                Emit(I.rings, p + Vector3.up * 0.06f, new Color(0.45f, 0.9f, 0.25f), 1, 2.2f);
                Emit(I.mist, p + Vector3.up * 0.5f, new Color(0.3f, 0.7f, 0.18f), 2, 1.3f);
            }
        }

        /// <summary>Kum tozu bulutu (kum girdabı, kum duvarı izi, çakram isabeti).</summary>
        public static void SandPuff(Vector3 pos, float scale = 1f)
        {
            if (I == null) return;
            // Opak (ışık alan) kum tozu: eklemeli parçacık parlak kumda görünmüyor
            Emit(I.dust != null ? I.dust : I.mist, pos, new Color(Random.Range(0.62f, 0.74f), 0.5f, 0.3f), 1, scale);
        }

        /// <summary>Çakram izi: altın kıvılcım.</summary>
        public static void GoldSpark(Vector3 pos)
        {
            if (I == null) return;
            Emit(I.glitter, pos, new Color(1f, Random.Range(0.7f, 0.85f), 0.35f), 1, Random.Range(0.6f, 1f));
        }

        /// <summary>Kum fırtınası dalgası (Kumların Hakimi): şerit boyunca boss'tan oyuncuya kum duvarı akar.</summary>
        public static void SandWave(Vector3 center, float width, float fromZ)
        {
            if (I == null) return;
            if (I.walls != null)
            {
                var w = I.walls[I.wallNext++ % I.walls.Length];
                w.Play(center.x, width * 0.95f, fromZ, -6f);
            }
            if (I.dust != null)
                for (int i = 0; i < 10; i++)
                    Emit(I.dust, new Vector3(center.x + Random.Range(-width * 0.5f, width * 0.5f), Random.Range(0.2f, 2f), fromZ + Random.Range(-0.5f, 1.5f)), new Color(0.55f, 0.4f, 0.24f), 1, 2.4f);

        }

        /// <summary>İki nokta arasında zikzak şimşek (havuzlu çizgi). width: kalınlık, life: süre (sn).</summary>
        public static void Lightning(Vector3 from, Vector3 to, float width, Color c, float life, float jag = -1f)
        {
            if (I == null || I.bolts == null) return;
            var b = I.bolts[I.boltNext++ % I.bolts.Length];
            b.Play(from, to, width, c, life, jag < 0f ? Mathf.Clamp(Vector3.Distance(from, to) * 0.06f, 0.08f, 1.2f) : jag);
        }

        /// <summary>Küçük elektrik boşalması: mavi kıvılcım + parlama.</summary>
        public static void Zap(Vector3 pos, float scale = 1f)
        {
            if (I == null) return;
            Emit(I.sparks, pos, new Color(0.6f, 0.88f, 1f), Mathf.Max(2, Mathf.RoundToInt(5 * scale)), scale);
            Emit(I.flashes, pos, new Color(0.55f, 0.8f, 1f), 1, 1.1f * scale);
        }

        /// <summary>Tek elektrik pırıltısı (yıldırım izi, uyarı şeridi). Ara ara altın renkli.</summary>
        public static void SkySpark(Vector3 pos)
        {
            if (I == null) return;
            var c = Random.value < 0.25f ? new Color(1f, 0.85f, 0.4f) : new Color(0.55f, Random.Range(0.8f, 0.95f), 1f);
            Emit(I.glitter, pos, c, 1, Random.Range(0.6f, 1f));
        }

        /// <summary>Fırtına Lordu saldırısı: şerit boyunca gökten yere peş peşe kalın yıldırımlar düşer.</summary>
        public static void LightningStrike(Vector3 center, float width)
        {
            if (I == null) return;
            var blue = new Color(0.45f, 0.72f, 1f);
            for (int i = 0; i < 3; i++)
            {
                var p = center + new Vector3(Random.Range(-width * 0.2f, width * 0.2f), 0f, -2f + i * 3.5f);
                var top = p + new Vector3(Random.Range(-1f, 1f), 13f, Random.Range(2f, 4f));
                Lightning(top, p, 0.26f, new Color(0.6f, 0.82f, 1f), 0.32f, 0.7f);
                // Yere değdiği yerden yana sıçrayan kısa kol
                Lightning(p + Vector3.up * 0.1f, p + new Vector3(Random.Range(-1.2f, 1.2f), 0.05f, Random.Range(-1.2f, 1.2f)), 0.08f, blue, 0.2f, 0.25f);
                Emit(I.flashes, p + Vector3.up * 0.5f, new Color(0.6f, 0.8f, 1f), 1, 2.6f);
                Emit(I.rings, p + Vector3.up * 0.06f, blue, 1, 2.2f);
                Emit(I.sparks, p + Vector3.up * 0.3f, new Color(0.7f, 0.92f, 1f), 7, 1.5f);
                Emit(I.chunks, p + Vector3.up * 0.2f, new Color(0.75f, 0.76f, 0.82f), 2, 1f);
            }
            Emit(I.beams, center + Vector3.forward * 1.5f, new Color(0.35f, 0.55f, 1f), 1, 1.3f);
        }

        static void Emit(ParticleSystem ps, Vector3 pos, Color c, int count, float scale)
        {
            if (ps == null || count <= 0) return;
            var ep = new ParticleSystem.EmitParams
            {
                position = pos,
                applyShapeToPosition = true,
                startColor = c,
            };
            var main = ps.main;
            float baseSize = main.startSizeMultiplier;
            if (Mathf.Abs(scale - 1f) > 0.01f)
            {
                // Boyut ve hızı ölçekle (parçacık başına tek tek)
                for (int i = 0; i < count; i++)
                {
                    ep.startSize = Random.Range(main.startSize.constantMin, main.startSize.constantMax) * scale;
                    ps.Emit(ep, 1);
                }
                return;
            }
            ps.Emit(ep, count);
        }

        // ─────────────────────────────────────────────────────
        // KURULUM
        // ─────────────────────────────────────────────────────
        void Build()
        {
            var fx = FxDesigns.FxMaterial;
            var toon = CharacterDesigns.Material;

            sparks = System("Sparks", SparkMesh(), fx, 200);
            Setup(sparks, 0.12f, 0.28f, 4f, 8f, 0.1f, 0.18f, 0f);
            Shape(sparks, ParticleSystemShapeType.Sphere, 0.05f);
            Drag(sparks, 6f);
            FadeShrink(sparks, true);
            Renderer(sparks).alignment = ParticleSystemRenderSpace.Velocity;

            flashes = System("Flashes", SphereMesh(), fx, 60);
            Setup(flashes, 0.08f, 0.12f, 0f, 0f, 0.35f, 0.45f, 0f);
            Shape(flashes, ParticleSystemShapeType.Sphere, 0.01f);
            GrowFade(flashes, 0.5f, 1.3f);

            bubbles = System("DeathBubbles", SphereMesh(), fx, 250);
            Setup(bubbles, 0.6f, 1.3f, 0.5f, 1.8f, 0.07f, 0.17f, -0.6f);
            Shape(bubbles, ParticleSystemShapeType.Sphere, 0.35f);
            Drag(bubbles, 2f);
            FadeShrink(bubbles, false);
            Noise(bubbles, 0.4f);

            chunks = System("Chunks", ChunkMesh(), toon, 200);
            Setup(chunks, 0.55f, 0.9f, 2.5f, 5f, 0.14f, 0.26f, 1.2f);
            Shape(chunks, ParticleSystemShapeType.Hemisphere, 0.25f);
            Drag(chunks, 1.5f);
            FadeShrink(chunks, false);
            Rotation(chunks);
            Renderer(chunks).shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            rings = System("Rings", RingMesh(), fx, 30);
            Setup(rings, 0.35f, 0.4f, 0f, 0f, 0.6f, 0.6f, 0f);
            Shape(rings, ParticleSystemShapeType.Sphere, 0.001f);
            GrowFade(rings, 0.4f, 2.4f);
            Renderer(rings).alignment = ParticleSystemRenderSpace.World;

            foam = System("Foam", SphereMesh(), fx, 150);
            Setup(foam, 0.35f, 0.6f, 2f, 4.5f, 0.07f, 0.16f, 2.2f);
            Shape(foam, ParticleSystemShapeType.Cone, 0.2f);
            FadeShrink(foam, false);

            glitter = System("Glitter", SparkMesh(), fx, 150);
            Setup(glitter, 0.5f, 0.9f, 0.8f, 2.2f, 0.08f, 0.14f, -0.8f);
            Shape(glitter, ParticleSystemShapeType.Sphere, 0.6f);
            Drag(glitter, 2.5f);
            FadeShrink(glitter, true);
            Rotation(glitter);

            beams = System("Beams", BeamMesh(), fx, 10);
            Setup(beams, 0.4f, 0.4f, 0f, 0f, 1f, 1f, 0f);
            Shape(beams, ParticleSystemShapeType.Sphere, 0.001f);
            GrowFade(beams, 1.2f, 0.4f);
            Renderer(beams).alignment = ParticleSystemRenderSpace.World;

            // Zehirli sis bulutu: yavaşça yükselip büyüyerek söner
            mist = System("ToxicMist", SphereMesh(), fx, 120);
            Setup(mist, 1.1f, 1.7f, 0.15f, 0.45f, 0.22f, 0.4f, -0.12f);
            Shape(mist, ParticleSystemShapeType.Sphere, 0.12f);
            GrowFade(mist, 0.6f, 2.2f);
            Noise(mist, 0.3f);

            if (desert)
            {
                dust = System("SandDust", SphereMesh(), toon, 500);
                Setup(dust, 0.8f, 1.6f, 0.3f, 1.4f, 0.25f, 0.5f, -0.06f);
                Shape(dust, ParticleSystemShapeType.Sphere, 0.2f);
                Drag(dust, 2f);
                FadeShrink(dust, false);
                Renderer(dust).shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var wm = DesertCreatures.SandWallMesh();
                var rm = DesertCreatures.SandRollMesh();
                walls = new SandWall[4];
                for (int i = 0; i < walls.Length; i++) walls[i] = SandWall.Create(transform, wm, rm, toon);
            }
            bolts = new SkyBolt[40];
            for (int i = 0; i < bolts.Length; i++) bolts[i] = SkyBolt.Create(transform);

            if (swamp || frost)
            {
                var mesh = frost ? FrostCreatures.IceSpikeMesh() : RootSpike.BuildMesh();
                spikes = new RootSpike[12];
                for (int i = 0; i < spikes.Length; i++)
                {
                    var go = new GameObject("RootSpike");
                    go.transform.SetParent(transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = toon;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    spikes[i] = go.AddComponent<RootSpike>();
                    go.SetActive(false);
                }
            }
        }

        ParticleSystem System(string name, Mesh mesh, Material mat, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = mesh;
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        static ParticleSystemRenderer Renderer(ParticleSystem ps) => ps.GetComponent<ParticleSystemRenderer>();

        static void Setup(ParticleSystem ps, float lifeMin, float lifeMax, float speedMin, float speedMax, float sizeMin, float sizeMax, float gravity)
        {
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            m.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            m.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            m.gravityModifier = gravity;
        }

        static void Shape(ParticleSystem ps, ParticleSystemShapeType type, float radius)
        {
            var s = ps.shape;
            s.enabled = true;
            s.shapeType = type;
            s.radius = radius;
            if (type == ParticleSystemShapeType.Cone) { s.angle = 30f; s.rotation = new Vector3(-90f, 0f, 0f); }
            if (type == ParticleSystemShapeType.Hemisphere) s.rotation = new Vector3(-90f, 0f, 0f);
        }

        static void Drag(ParticleSystem ps, float drag)
        {
            var l = ps.limitVelocityOverLifetime;
            l.enabled = true;
            l.drag = drag;
            l.multiplyDragByParticleSize = false;
            l.multiplyDragByParticleVelocity = false;
        }

        static void Noise(ParticleSystem ps, float strength)
        {
            var n = ps.noise;
            n.enabled = true;
            n.strength = strength;
            n.frequency = 1.2f;
            n.quality = ParticleSystemNoiseQuality.Low;
        }

        static void Rotation(ParticleSystem ps)
        {
            var m = ps.main;
            m.startRotation3D = true;
            m.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            m.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            m.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var r = ps.rotationOverLifetime;
            r.enabled = true;
            r.separateAxes = true;
            r.x = new ParticleSystem.MinMaxCurve(-8f, 8f);
            r.y = new ParticleSystem.MinMaxCurve(-8f, 8f);
            r.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
        }

        /// <summary>Ömrünün sonunda küçülür; eklemeli olanlar ayrıca renkçe söner.</summary>
        static void FadeShrink(ParticleSystem ps, bool fadeColor)
        {
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.6f, 0.9f), new Keyframe(1f, 0f)));
            if (!fadeColor) return;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        /// <summary>Başlangıç → bitiş ölçeğine büyür/küçülür ve söner (parlama, halka, ışık sütunu).</summary>
        static void GrowFade(ParticleSystem ps, float from, float to)
        {
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, from), new Keyframe(1f, to)));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        // ─────────────────────────────────────────────────────
        // EFEKT MESH'LERİ (beyaz; renk parçacıktan gelir)
        // ─────────────────────────────────────────────────────
        static Mesh sparkMesh, sphereMesh, chunkMesh, ringMesh, beamMesh;

        static Mesh SparkMesh()
        {
            if (sparkMesh != null) return sparkMesh;
            // İki eksende çapraz, uzun elmas: hız yönüne hizalanınca çizgi gibi görünür
            var b = new MeshBuilder();
            b.Box(Vector3.zero, new Vector3(0.35f, 0.35f, 1.6f), Color.white, new Vector3(0f, 0f, 45f), 0.2f, 0.2f);
            sparkMesh = b.ToMesh("Fx_Spark");
            return sparkMesh;
        }

        static Mesh SphereMesh()
        {
            if (sphereMesh != null) return sphereMesh;
            var b = new MeshBuilder();
            b.Sphere(Vector3.zero, Vector3.one * 0.5f, Color.white, 7, 5);
            sphereMesh = b.ToMesh("Fx_Sphere");
            return sphereMesh;
        }

        static Mesh ChunkMesh()
        {
            if (chunkMesh != null) return chunkMesh;
            // Düzensiz kabuk parçası
            var b = new MeshBuilder();
            b.Box(Vector3.zero, new Vector3(1f, 0.45f, 0.8f), Color.white, new Vector3(0f, 20f, 0f), 0.6f, 0.7f, 0.15f);
            chunkMesh = b.ToMesh("Fx_Chunk");
            return chunkMesh;
        }

        static Mesh RingMesh()
        {
            if (ringMesh != null) return ringMesh;
            // Zemine yatık ince halka (XZ düzlemi)
            const int seg = 24;
            var v = new System.Collections.Generic.List<Vector3>();
            var c = new System.Collections.Generic.List<Color>();
            var t = new System.Collections.Generic.List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(d * 0.4f); c.Add(new Color(0f, 0f, 0f, 1f));
                v.Add(d * 0.5f); c.Add(Color.white);
                v.Add(d * 0.56f); c.Add(new Color(0f, 0f, 0f, 1f));
                if (i < seg)
                {
                    int k = i * 3;
                    t.Add(k); t.Add(k + 4); t.Add(k + 1);
                    t.Add(k); t.Add(k + 3); t.Add(k + 4);
                    t.Add(k + 1); t.Add(k + 5); t.Add(k + 2);
                    t.Add(k + 1); t.Add(k + 4); t.Add(k + 5);
                }
            }
            ringMesh = new Mesh { name = "Fx_Ring" };
            ringMesh.SetVertices(v);
            ringMesh.SetColors(c.ConvertAll(x => (Color32)x));
            ringMesh.SetTriangles(t, 0);
            ringMesh.RecalculateBounds();
            return ringMesh;
        }

        static Mesh BeamMesh()
        {
            if (beamMesh != null) return beamMesh;
            // Yerden yükselen, yukarı doğru sönen ışık sütunu (açık silindir, köşe rengiyle degrade)
            const int seg = 12;
            const float h = 3f, r = 0.7f;
            var v = new System.Collections.Generic.List<Vector3>();
            var c = new System.Collections.Generic.List<Color>();
            var t = new System.Collections.Generic.List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(d * r); c.Add(new Color(0.45f, 0.45f, 0.45f, 1f));
                v.Add(d * r * 0.7f + Vector3.up * h); c.Add(new Color(0f, 0f, 0f, 1f));
                if (i < seg)
                {
                    int k = i * 2;
                    t.Add(k); t.Add(k + 1); t.Add(k + 2);
                    t.Add(k + 2); t.Add(k + 1); t.Add(k + 3);
                }
            }
            beamMesh = new Mesh { name = "Fx_Beam" };
            beamMesh.SetVertices(v);
            beamMesh.SetColors(c.ConvertAll(x => (Color32)x));
            beamMesh.SetTriangles(t, 0);
            beamMesh.RecalculateBounds();
            return beamMesh;
        }
    }

    /// <summary>Yerden fırlayıp geri çekilen sivri kök kümesi (havuzlu).</summary>
    public class RootSpike : MonoBehaviour
    {
        const float Rise = 0.12f, Hold = 0.55f, Sink = 0.45f;
        float t, delay, size;

        public void Play(Vector3 pos, float yaw, float s, float wait)
        {
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            size = s;
            t = -wait;
            transform.localScale = new Vector3(s, 0.001f, s);
            gameObject.SetActive(true);
        }

        void Update()
        {
            t += Time.deltaTime;
            float h;
            if (t < 0f) h = 0f;
            else if (t < Rise) h = t / Rise;
            else if (t < Rise + Hold) h = 1f;
            else if (t < Rise + Hold + Sink) h = 1f - (t - Rise - Hold) / Sink;
            else { gameObject.SetActive(false); return; }
            h = 1f - (1f - h) * (1f - h);
            transform.localScale = new Vector3(size, size * Mathf.Max(0.001f, h), size);
        }

        /// <summary>Çamur tümseği + dışa doğru eğik 5 sivri kök (mor-gri kabuk, yeşil sarmaşık, dikenler). Taban yarıçapı ~1.</summary>
        public static Mesh BuildMesh()
        {
            var b = new MeshBuilder();
            var bark = new Color(0.34f, 0.28f, 0.38f);
            var barkDark = new Color(0.18f, 0.14f, 0.2f);
            var vine = new Color(0.3f, 0.6f, 0.15f);
            var mud = new Color(0.14f, 0.1f, 0.06f);
            b.Sphere(new Vector3(0f, 0.02f, 0f), new Vector3(1.05f, 0.18f, 1.05f), mud, 10, 4, default, true);
            var rnd = new System.Random(5);
            for (int i = 0; i < 5; i++)
            {
                float a = (i * 72f + (float)rnd.NextDouble() * 30f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float h = i == 0 ? 2.4f : 1.3f + (float)rnd.NextDouble() * 0.8f;
                var b0 = dir * (i == 0 ? 0.05f : 0.4f);
                var mid = b0 + dir * 0.25f + Vector3.up * h * 0.55f;
                var tip = b0 + dir * (i == 0 ? 0.2f : 0.55f) + Vector3.up * h;
                float r = i == 0 ? 0.28f : 0.18f;
                b.Cylinder(b0 - Vector3.up * 0.1f, mid, r, r * 0.6f, 6, bark, true, true);
                b.Cylinder(mid, tip, r * 0.6f, 0f, 6, barkDark, true, true);
                // Sarmaşık halkası ve diken
                b.Cylinder(Vector3.Lerp(b0, mid, 0.6f), Vector3.Lerp(b0, mid, 0.6f) + Vector3.up * 0.08f, r * 0.85f, r * 0.8f, 6, vine, false, true);
                var side = Vector3.Cross(dir, Vector3.up);
                var th = Vector3.Lerp(mid, tip, 0.3f);
                b.Cylinder(th, th + side * 0.25f + Vector3.up * 0.1f, 0.05f, 0f, 4, barkDark, true, true);
            }
            return b.ToMesh("Fx_RootSpike");
        }
    }
}

namespace StratejiGame
{
    /// <summary>
    /// Zikzak şimşek çizgisi (havuzlu): geniş mavi hale + ince beyaz çekirdek iki LineRenderer.
    /// Ömrü boyunca birkaç kez şekil değiştirir ve titreyerek söner.
    /// </summary>
    public class SkyBolt : MonoBehaviour
    {
        LineRenderer halo, core;
        Vector3 a, b;
        float t, life, width, jag, reshapeT;
        Color col;
        const int Segs = 9;
        readonly Vector3[] pts = new Vector3[Segs + 1];

        public static SkyBolt Create(Transform parent)
        {
            var go = new GameObject("SkyBolt");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<SkyBolt>();
            s.halo = Line(go.transform, "Halo");
            s.core = Line(go.transform, "Core");
            go.SetActive(false);
            return s;
        }

        static LineRenderer Line(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = FxDesigns.FxMaterial;
            lr.useWorldSpace = true;
            lr.positionCount = Segs + 1;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        public void Play(Vector3 from, Vector3 to, float w, Color c, float duration, float jaggedness)
        {
            a = from; b = to; width = w; col = c; life = Mathf.Max(0.05f, duration); jag = jaggedness;
            t = 0f; reshapeT = 0f;
            gameObject.SetActive(true);
            Reshape();
            Apply(1f);
        }

        void Reshape()
        {
            var d = b - a;
            var side = Vector3.Cross(d, Vector3.forward);
            if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(d, Vector3.right);
            side.Normalize();
            var side2 = Vector3.Cross(d.normalized, side);
            for (int i = 0; i <= Segs; i++)
            {
                float k = i / (float)Segs;
                var p = Vector3.Lerp(a, b, k);
                if (i > 0 && i < Segs)
                {
                    float env = Mathf.Sin(k * Mathf.PI);
                    p += (side * Random.Range(-1f, 1f) + side2 * Random.Range(-0.5f, 0.5f)) * jag * env;
                }
                pts[i] = p;
            }
            halo.SetPositions(pts);
            core.SetPositions(pts);
        }

        void Apply(float k)
        {
            var hc = new Color(col.r, col.g, col.b, 0.32f * k);
            var cc = new Color(Mathf.Lerp(col.r, 1f, 0.7f), Mathf.Lerp(col.g, 1f, 0.7f), Mathf.Lerp(col.b, 1f, 0.7f), k);
            halo.startColor = halo.endColor = hc;
            core.startColor = core.endColor = cc;
            halo.widthMultiplier = width * 2.2f;
            core.widthMultiplier = width * 0.7f;
        }

        void Update()
        {
            t += Time.deltaTime;
            if (t >= life) { gameObject.SetActive(false); return; }
            reshapeT -= Time.deltaTime;
            if (reshapeT <= 0f) { reshapeT = 0.05f; Reshape(); }
            float k = 1f - t / life;
            float flicker = Random.value < 0.25f ? 0.4f : 1f;
            Apply(k * flicker);
        }
    }
}
