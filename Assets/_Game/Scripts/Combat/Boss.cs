using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Prototip boss: Abisal Yengeç Kral (graybox). Yavaş ilerler, belli mesafede durur,
    /// kalabalık minyon dalgaları çağırır ve 3 şeritten 2'sini kaplayan Tsunami saldırısı yapar.
    /// Tsunami'den korunmak için oyuncu işaretlenmeyen güvenli şeride geçmelidir.
    /// </summary>
    public class Boss : MonoBehaviour
    {
        public string displayName = "Abisal Yengeç Kral";
        public float baseHp = 7000f;
        public float walkSpeed = 1.3f;
        public float stopZ = 13f;

        [Header("Tsunami (alan saldırısı)")]
        public float tsunamiInterval = 5f;
        public float tsunamiWarning = 1.5f;
        public int tsunamiMaxKillsPerLane = 4;
        public float tsunamiPlayerDamage = 30f;
        [Tooltip("Güvenli şeridin oyuncunun şu anki şeridi OLMAMA olasılığı (oyuncuyu hareket ettirir)")]
        [Range(0f, 1f)] public float forceMoveChance = 0.7f;

        [Header("Minyon çağırma")]
        public float summonInterval = 5f;
        public int summonCount = 10;
        [Tooltip("Her kaç çağırmada bir iri düşman eklensin")]
        public int bruteEvery = 2;
        [Tooltip("Her kaç çağırmada bir Zıpkıncı eklensin")]
        public int harpoonerEvery = 3;

        public Enemy Enemy { get; private set; }

        GameManager gm;
        float attackT = 3f, summonT = 3f, warnT;
        int summonIndex;
        bool telegraphing;
        int safeLane;
        GameObject[] warns;

        const float SkyRideHeight = 1.7f;
        float zapT;

        static readonly Color WarnA = new Color(1f, 0.1f, 0.1f, 0.35f);
        static readonly Color WarnB = new Color(1f, 0.5f, 0.1f, 0.6f);

        public static Boss Create(Transform parent, Vector3 pos)
        {
            var go = new GameObject("Boss_YengecKral");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var t = go.transform;

            // 3D model varsa onu kullan (Meshy'den gelen Yengeç Kral + sırtındaki gemi enkazı)
            var world = GameManager.I != null ? GameManager.I.CurrentWorld : null;
            if (world != null && world.bossPrefab != null)
            {
                var model = Instantiate(world.bossPrefab, t);
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * world.bossModelScale;
                // Bataklık: ayaksız Kök Kralı bataklıkta süzülür (kökler shader'da kıvranır)
                // Boyanmış modellerin yüzü +Z'de; boss kökü dönmediği için oyuncuya çevir (Yanardağ, Bataklık, Kar)
                var th = GameManager.I.Theme;
                if (th == WorldTheme.Volcano || th == WorldTheme.Swamp || th == WorldTheme.Frost || th == WorldTheme.Sky || th == WorldTheme.Desert)
                    model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                if (th == WorldTheme.Swamp) model.AddComponent<SwampGlide>();
                else if (th == WorldTheme.Sky)
                {
                    // Gökyüzü: Fırtına Lordu koyu bir fırtına bulutunun üstünde süzülür
                    model.transform.localPosition = Vector3.up * SkyRideHeight;
                    var bob = model.AddComponent<ModelBob>();
                    bob.bobHeight = 0.03f; bob.speed = 1.6f; bob.swayDegrees = 2f;
                    SkyCreatures.AttachStormCloud(t, 2.9f, SkyRideHeight + 0.1f);
                }
                else model.AddComponent<ModelBob>();
                var bossWithModel = go.AddComponent<Boss>();
                bossWithModel.Init();
                return bossWithModel;
            }

            // Yedek: basit şekillerle gövde ve sırtındaki gemi enkazı
            Mats.Prim(PrimitiveType.Cube, new Color(0.15f, 0.2f, 0.45f), t, new Vector3(0f, 1.1f, 0f), new Vector3(3.4f, 1.4f, 2.4f));
            Mats.Prim(PrimitiveType.Cube, new Color(0.40f, 0.28f, 0.15f), t, new Vector3(0f, 2.4f, 0.2f), new Vector3(2.4f, 1.1f, 1.2f));
            Mats.Prim(PrimitiveType.Cylinder, new Color(0.35f, 0.25f, 0.15f), t, new Vector3(-0.5f, 3.8f, 0.2f), new Vector3(0.12f, 1.2f, 0.12f));
            Mats.Prim(PrimitiveType.Cylinder, new Color(0.35f, 0.25f, 0.15f), t, new Vector3(0.6f, 3.6f, 0.2f), new Vector3(0.12f, 1f, 0.12f));
            // Kalkan kıskaç (sol) ve keskin kıskaç (sağ)
            Mats.Prim(PrimitiveType.Cube, new Color(0.3f, 0.4f, 0.3f), t, new Vector3(-2.2f, 1.2f, -1.3f), new Vector3(1.4f, 1.8f, 0.4f));
            Mats.Prim(PrimitiveType.Cube, new Color(0.2f, 0.25f, 0.5f), t, new Vector3(2.1f, 0.8f, -1.5f), new Vector3(0.45f, 0.5f, 2f));
            // Gözler
            Mats.Prim(PrimitiveType.Sphere, new Color(1f, 0.85f, 0.2f), t, new Vector3(-0.5f, 1.9f, -1.2f), Vector3.one * 0.3f);
            Mats.Prim(PrimitiveType.Sphere, new Color(1f, 0.85f, 0.2f), t, new Vector3(0.5f, 1.9f, -1.2f), Vector3.one * 0.3f);

            var boss = go.AddComponent<Boss>();
            boss.Init();
            return boss;
        }

        void Init()
        {
            gm = GameManager.I;
            Enemy = gameObject.AddComponent<Enemy>();
            Enemy.SetupCustom(baseHp * gm.BossHpMult, 1.9f);
            var world = gm.CurrentWorld;
            if (world != null && !string.IsNullOrEmpty(world.bossName)) displayName = Loc.Name(world.bossName);
            gm.Enemies.Register(Enemy);

            // Her şerit için bir uyarı alanı
            warns = new GameObject[GameManager.LaneCount];
            for (int i = 0; i < warns.Length; i++)
            {
                warns[i] = Mats.Prim(PrimitiveType.Cube, WarnA, gm.transform,
                    new Vector3(gm.LaneCenter(i), 0.03f, 3f), new Vector3(gm.LaneWidth - 0.1f, 0.05f, 12f), true);
                warns[i].name = "TsunamiWarning_" + i;
                warns[i].SetActive(false);
            }
        }

        void Update()
        {
            if (Enemy == null || Enemy.dead || !gm.Playing)
            {
                if (Enemy == null || Enemy.dead) HideWarnings();
                return;
            }
            float dt = Time.deltaTime;
            var p = transform.position;

            // Yürürken de minyon çağırır
            summonT -= dt;
            if (summonT <= 0f)
            {
                summonT = summonInterval;
                Summon(p);
            }

            if (sandHitT >= 0f)
            {
                sandHitT -= dt;
                if (sandHitT < 0f) { HitLanes(sandSafeLane); gm.Shake(0.45f, 0.35f); }
            }

            if (p.z > stopZ)
            {
                p.z -= walkSpeed * dt;
                transform.position = p;
                return;
            }

            if (!telegraphing)
            {
                attackT -= dt;
                if (attackT <= 0f) StartTsunami();
            }
            else
            {
                warnT -= dt;
                var mat = Mats.GetTransparent(Mathf.Repeat(warnT * 6f, 1f) < 0.5f ? WarnA : WarnB);
                for (int i = 0; i < warns.Length; i++)
                    if (warns[i].activeSelf)
                    {
                        warns[i].GetComponent<Renderer>().sharedMaterial = mat;
                        // Yanardağ: uyarı şeridinde çatlaklardan kıvılcım ve duman yükselir
                        if (gm.Theme == WorldTheme.Volcano && Random.value < 0.5f)
                            Effects.Ember(new Vector3(warns[i].transform.position.x + Random.Range(-1f, 1f), 0.1f, Random.Range(-2f, 8f)));
                        // Bataklık: köklerin çıkacağı şeritte zehir kabarcıkları
                        if (gm.Theme == WorldTheme.Frost && Random.value < 0.6f)
                            Effects.FrostSparkle(new Vector3(warns[i].transform.position.x + Random.Range(-1f, 1f), 0.1f, Random.Range(-2f, 8f)));
                        if (gm.Theme == WorldTheme.Sky)
                        {
                            // Gökyüzü: yıldırımın düşeceği şeritte elektrik pırıltıları ve gökten titreyen ince öncü şimşekler
                            if (Random.value < 0.6f) Effects.SkySpark(new Vector3(warns[i].transform.position.x + Random.Range(-1f, 1f), 0.15f, Random.Range(-2f, 8f)));
                            if (zapT <= 0f && Random.value < 0.5f)
                            {
                                var g = new Vector3(warns[i].transform.position.x + Random.Range(-0.8f, 0.8f), 0.05f, Random.Range(-2f, 8f));
                                Effects.Lightning(g + new Vector3(Random.Range(-1f, 1f), 16f, 2f), g, 0.07f, new Color(0.45f, 0.7f, 1f), 0.12f, 0.9f);
                            }
                        }
                        if (gm.Theme == WorldTheme.Desert && Random.value < 0.6f)
                            Effects.SandPuff(new Vector3(warns[i].transform.position.x + Random.Range(-1.2f, 1.2f), Random.Range(0.1f, 0.8f), Random.Range(-2f, 8f)), 0.8f);
                        if (gm.Theme == WorldTheme.Swamp && Random.value < 0.5f)
                            Effects.PoisonDrip(new Vector3(warns[i].transform.position.x + Random.Range(-1f, 1f), 0.1f, Random.Range(-2f, 8f)));
                    }
                zapT -= dt;
                if (zapT <= 0f) zapT = 0.18f;
                if (warnT <= 0f)
                {
                    telegraphing = false;
                    attackT = tsunamiInterval;
                    HideWarnings();
                    Tsunami();
                }
            }
        }

        void Summon(Vector3 bossPos)
        {
            summonIndex++;
            Sfx.Play(Snd.Summon);
            float lim = gm.corridorHalfWidth - 0.6f;
            for (int i = 0; i < summonCount; i++)
            {
                var pos = new Vector3(Random.Range(-lim, lim), 0f, bossPos.z - 2.2f - Random.Range(0f, 2f));
                gm.Enemies.Spawn(EnemyKind.Minion, pos);
            }
            if (bruteEvery > 0 && summonIndex % bruteEvery == 0)
                gm.Enemies.Spawn(EnemyKind.Brute, new Vector3(gm.LaneCenter(Random.Range(0, GameManager.LaneCount)), 0f, bossPos.z - 2.5f));
            if (harpoonerEvery > 0 && summonIndex % harpoonerEvery == 0)
                gm.Enemies.Spawn(EnemyKind.Harpooner, new Vector3(gm.LaneCenter(Random.Range(0, GameManager.LaneCount)), 0f, bossPos.z - 1.5f));
        }

        void StartTsunami()
        {
            telegraphing = true;
            warnT = tsunamiWarning;
            Sfx.Play(Snd.TsunamiWarn);
            int playerLane = gm.LaneOf(gm.Player.X);
            if (Random.value < forceMoveChance)
            {
                // Oyuncunun şeridi vurulacak; güvenli şerit başka bir şerit
                safeLane = (playerLane + Random.Range(1, GameManager.LaneCount)) % GameManager.LaneCount;
            }
            else safeLane = playerLane;

            for (int i = 0; i < warns.Length; i++) warns[i].SetActive(i != safeLane);
        }

        /// <summary>Çöl: kum duvarı orduya ulaşınca (gecikmeli) vurur.</summary>
        float sandHitT = -1f;
        int sandSafeLane;
        const float SandTravelDelay = 0.62f;

        void HitLanes(int safe)
        {
            for (int i = 0; i < GameManager.LaneCount; i++)
            {
                if (i == safe) continue;
                float half = gm.LaneWidth * 0.5f;
                float cx = gm.LaneCenter(i);
                gm.Army.KillInArea(cx - half, cx + half, -6f, 9f, tsunamiMaxKillsPerLane);
            }
            if (gm.LaneOf(gm.Player.X) != safe)
            {
                gm.Player.TakeDamage(tsunamiPlayerDamage);
            }
        }

        void Tsunami()
        {
            bool desert = gm.Theme == WorldTheme.Desert;
            for (int i = 0; i < GameManager.LaneCount; i++)
            {
                if (i == safeLane) continue;
                float cx = gm.LaneCenter(i);
                if (gm.Theme == WorldTheme.Volcano) Effects.LavaEruption(new Vector3(cx, 0f, 3f), gm.LaneWidth);
                else if (gm.Theme == WorldTheme.Swamp) Effects.RootEruption(new Vector3(cx, 0f, 3f), gm.LaneWidth);
                else if (gm.Theme == WorldTheme.Frost) Effects.IceEruption(new Vector3(cx, 0f, 3f), gm.LaneWidth);
                else if (gm.Theme == WorldTheme.Sky) Effects.LightningStrike(new Vector3(cx, 0f, 3f), gm.LaneWidth);
                else if (gm.Theme == WorldTheme.Desert) Effects.SandWave(new Vector3(cx, 0f, 3f), gm.LaneWidth, transform.position.z - 2f);
                else Fx.Ring(new Vector3(cx, 0.1f, 3f), 3.5f, new Color(0.3f, 0.7f, 1f, 0.5f), 0.4f);
            }
            if (desert) { sandHitT = SandTravelDelay; sandSafeLane = safeLane; gm.Shake(0.2f, 0.3f); }   // kum duvarı yolda; vuruş gecikmeli
            else { HitLanes(safeLane); gm.Shake(0.45f, 0.35f); }
            Sfx.Play(Snd.TsunamiHit);
            // Yeti gürzünü yere vurur (öne eğilip kalkar)
            var bob = GetComponentInChildren<ModelBob>();
            if (bob != null && (gm.Theme == WorldTheme.Frost || gm.Theme == WorldTheme.Desert)) bob.Slam();   // Yeti gürzü / Firavun orağı
        }

        void HideWarnings()
        {
            if (warns == null) return;
            foreach (var w in warns) if (w != null && w.activeSelf) w.SetActive(false);
        }

        void OnDestroy()
        {
            if (warns == null) return;
            foreach (var w in warns) if (w != null) Destroy(w);
        }
    }

    /// <summary>Animasyonsuz modeller için hafif nefes alma / sallanma hareketi.</summary>
    public class ModelBob : MonoBehaviour
    {
        public float bobHeight = 0.04f;
        public float swayDegrees = 2.5f;
        public float speed = 2.2f;
        Vector3 basePos;
        Quaternion baseRot;
        float phase, slamT = -1f;

        /// <summary>Kısa, sert öne eğilme (gürz vuruşu).</summary>
        public void Slam() { slamT = 0f; }

        void Start()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            phase = Random.value * 10f;
        }

        void Update()
        {
            float t = Time.time * speed + phase;
            float s = transform.localScale.y;
            transform.localPosition = basePos + Vector3.up * (Mathf.Sin(t) * bobHeight * s);
            float slam = 0f;
            if (slamT >= 0f)
            {
                slamT += Time.deltaTime;
                slam = slamT < 0.12f ? slamT / 0.12f : Mathf.Max(0f, 1f - (slamT - 0.12f) / 0.5f);
                if (slamT > 0.7f) slamT = -1f;
            }
            transform.localRotation = baseRot * Quaternion.Euler(slam * 14f, 0f, Mathf.Sin(t * 0.5f) * swayDegrees);
        }
    }
}
