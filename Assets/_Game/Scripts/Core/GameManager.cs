using UnityEngine;

namespace StratejiGame
{
    public enum GameState { Loading, Playing, Won, Lost, Paused }

    /// <summary>Ana karakter ve askerlerin paylaştığı savaş değerleri. Kapılar ve Öfke yeteneği bunları değiştirir.</summary>
    [System.Serializable]
    public class CombatStats
    {
        [Header("Taban değerler (kalıcı yükseltmelerden gelir)")]
        public float playerDamage = 10f;
        public float soldierDamage = 4f;
        public float playerFireInterval = 0.25f;
        public float soldierFireInterval = 0.6f;

        [Header("Bölüm içi güçlendirmeler (kapılardan)")]
        public float fireRateMult = 1f;
        public float damageMult = 1f;
        public int extraBullets = 0;
        public int pierce = 0;
        public float critChance = 0f;

        [Header("Geçici")]
        public float rageMult = 1f;

        public float Interval(float baseInterval) => baseInterval / (fireRateMult * rageMult);

        public float Damage(float baseDamage)
        {
            float d = baseDamage * damageMult * rageMult;
            if (critChance > 0f && Random.value < critChance) d *= 2f;
            return d;
        }
    }

    /// <summary>
    /// Savaş sahnesinin merkezi. Ana menüde seçilen seviyeyi oynatır:
    /// sahneyi kurar, kayıtlı yükseltmeleri uygular, sonuçta ödülü ve ilerlemeyi kaydeder.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        [Header("Veri (boşsa Resources/GameDatabase kullanılır)")]
        public GameDatabase database;

        [Header("Koridor")]
        public float corridorHalfWidth = 4f;
        public float spawnZ = 42f;
        [Tooltip("Kapı sıralarının ana karaktere doğru ilerleme hızı (mekan sabittir)")]
        public float scrollSpeed = 4f;

        [Header("Kamera")]
        public Vector3 cameraPosition = new Vector3(0f, 15f, -9f);
        public float cameraPitch = 45f;
        [Tooltip("Yatay görüş açısının yarısı; koridorun ekrana sığmasını belirler")]
        public float cameraHorizontalHalfFov = 26f;

        [Header("Savaş (çalışma anında yükseltmelerden hesaplanır)")]
        public CombatStats stats = new CombatStats();
        public float playerMaxHp = 100f;
        public int startSoldiers = 0;

        public GameState State { get; private set; } = GameState.Loading;
        public bool Playing => State == GameState.Playing;
        public bool Paused => State == GameState.Paused;
        public int RunGold { get; private set; }
        public int KillCount { get; set; }
        /// <summary>Bu bölümde ulaşılan en büyük ordu (istatistik).</summary>
        public int RunMaxArmy { get; private set; }
        float runTime;
        bool statsRecorded;
        /// <summary>Bu bölümde geçen oyun süresi (sn).</summary>
        public float RunTime => runTime;
        /// <summary>Kazanınca kalan cana göre 1-3 yıldız; NewBestStars: bu seviyenin rekoru mu.</summary>
        public int LastStars { get; private set; }
        public bool NewBestStars { get; private set; }

        /// <summary>Kalan can oranına göre yıldız: %70+ → 3, %35+ → 2, aksi 1.</summary>
        public static int StarsFor(float hpFraction) => hpFraction >= 0.7f ? 3 : hpFraction >= 0.35f ? 2 : 1;

        public SaveData Save => SaveSystem.Data;
        public int TotalGold => Save.gold;

        /// <summary>Şu an oynanan seviye (genel numara).</summary>
        public int Level { get; private set; }
        /// <summary>Sonsuz Savunma: Level, o an bulunulan diyarın ilk seviyesini gösterir (tema, düşman modelleri, boss buradan gelir).</summary>
        public bool Endless { get; private set; }
        public bool IsReplay => !Endless && GameSession.IsCompleted(Level);
        public LevelData CurrentLevel => database != null ? database.GetLevel(Level) : null;
        public WorldData CurrentWorld => database != null ? database.GetWorld(Level) : null;
        public WorldTheme Theme => CurrentWorld != null ? CurrentWorld.theme : WorldTheme.Underwater;
        public UpgradeConfig UpgradeCfg { get; private set; }
        public float DifficultyMult => (Endless && Director != null ? Director.EndlessHpMult : (CurrentLevel != null ? CurrentLevel.enemyHpMultiplier : Mathf.Pow(1.1f, Level - 1)) * SupportHpMult) * WorldHpBalance;
        public float BossHpMult => (Endless && Director != null ? Director.EndlessBossMult : (CurrentLevel != null ? CurrentLevel.bossHpMultiplier : 1f) * SupportHpMult) * WorldHpBalance;

        // dengeleme: 4. diyarın (Buz) uzaktan vuranları askerleri öldürmez, yalnız dondurur; ordu büyük kaldığı için o diyar
        // diğerlerinden kolaydı. Bu yüzden orada düşman ve boss canı %12 fazla.
        static readonly float[] WorldHpByWorld = { 1f, 1f, 1f, 1.12f, 1f, 1f };
        public float WorldHpBalance => WorldHpByWorld[Mathf.Clamp(GameDatabase.WorldIndexOf(Level), 0, WorldHpByWorld.Length - 1)];

        // ------------------------------------------------------------------ Komutan desteği
        // Aynı seviyede art arda kaybeden oyuncuya yardım (zayıf oyuncuda asıl sorun küçük ordu, bu yüzden asker ağırlıklı):
        // 3 yenilgi: düşmanlar %12 zayıf + 6 asker, 5 yenilgi: %22 zayıf + 8 asker, 7 yenilgi: %35 zayıf + 10 asker.
        // Seviye kazanılınca sayaç sıfırlanır. Sonsuz Savunma'da yok.
        public const int SupportAfter = 3, SupportAfter2 = 5, SupportAfter3 = 7;
        /// <summary>0: destek yok, 1: hafif, 2: güçlü, 3: en güçlü.</summary>
        public int SupportTier { get; private set; }
        public float SupportHpMult => SupportTier == 3 ? 0.65f : SupportTier == 2 ? 0.78f : SupportTier == 1 ? 0.88f : 1f;
        public int SupportSoldiers => SupportTier == 3 ? 10 : SupportTier == 2 ? 8 : SupportTier == 1 ? 6 : 0;

        int ComputeSupportTier()
        {
            if (Endless || Save == null || Save.failLevel != Level) return 0;
            return Save.failStreak >= SupportAfter3 ? 3 : Save.failStreak >= SupportAfter2 ? 2 : Save.failStreak >= SupportAfter ? 1 : 0;
        }
        public bool IsBossLevel => !Endless && (CurrentLevel != null ? CurrentLevel.hasBoss : GameSession.IsBoss(Level));
        public bool HasNextLevel => database != null && database.HasLevel(Level + 1);

        // --- 3 şeritli yol ---
        public const int LaneCount = 3;
        public float LaneWidth => corridorHalfWidth * 2f / LaneCount;
        public float LaneCenter(int lane) => -corridorHalfWidth + LaneWidth * (lane + 0.5f);
        public int LaneOf(float x) => Mathf.Clamp(Mathf.FloorToInt((x + corridorHalfWidth) / LaneWidth), 0, LaneCount - 1);

        public PlayerController Player { get; private set; }
        public Army Army { get; private set; }
        public EnemyManager Enemies { get; private set; }
        public GateManager Gates { get; private set; }
        public LevelDirector Director { get; private set; }
        public SkillSystem Skills { get; private set; }
        public Hud Hud { get; private set; }

        Camera cam;
        Vector3 camBasePos;
        Quaternion camBaseRot = Quaternion.identity;
        float shakeT, shakeAmp;
        // geçici kamera odağı (boss ölüm sahnesi): 0 = normal kamera, 1 = tamamen odak konumu
        Vector3 camFocusPos;
        Quaternion camFocusRot = Quaternion.identity;
        float camFocusK;
        bool camDirty;

        void Awake()
        {
            I = this;
            AppBoot.ApplyFrameRate();
            Application.runInBackground = true; // editör pencere odakta değilken de oyun çalışsın
            SaveSystem.Load();
            if (database == null) database = GameDatabase.Load();

            UpgradeCfg = database != null ? database.upgrades : null;
            if (UpgradeCfg == null)
            {
                UpgradeCfg = ScriptableObject.CreateInstance<UpgradeConfig>();
                UpgradeCfg.ResetToDefaults();
            }

            Level = GameSession.SelectedLevel > 0 ? GameSession.SelectedLevel : Save.level;
            GameSession.SelectedLevel = 0;
            Endless = GameSession.Endless;
            // Sonsuz Savunma rastgele bir diyarda başlar
            if (Endless) Level = GameDatabase.FirstLevelOf(LevelDirector.RandomWorld(-1));
            Build();
        }

        void Start()
        {
            // Seviye verisi yoksa (ör. henüz yapılmamış dünya) ana menüye dön
            if (CurrentLevel == null && database != null && database.worlds.Count > 0)
            {
                GameSession.GoToMenu();
                return;
            }
            StartRun();
            Sfx.SetTheme(Theme);
            Sfx.SetAmbience(true);
        }

        void Build()
        {
            CorridorBuilder.Build(transform, corridorHalfWidth, this);
            if (AppBoot.LowEnd) PerfTuning.ReduceEnvironmentEffects(transform);
            SetupCamera();
            Effects.Create(transform);

            Enemies = Child<EnemyManager>("Enemies");
            Player = Child<PlayerController>("Player");
            Army = Child<Army>("Army");
            Gates = Child<GateManager>("Gates");
            Skills = Child<SkillSystem>("Skills");
            Director = Child<LevelDirector>("Director");
            Hud = Child<Hud>("Hud");

            ApplyUpgrades();
            Enemies.Init();
            Player.Init(playerMaxHp);
            Army.Init();
            Skills.Init();
            Hud.Init();
            Director.Init();
        }

        T Child<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        /// <summary>Kayıtlı yükseltme seviyelerini savaş değerlerine çevirir.</summary>
        public void ApplyUpgrades()
        {
            var c = UpgradeCfg;
            int L(UpgradeId id) => Save.GetUpgrade(id);

            playerMaxHp = c.MaxHp(L(UpgradeId.MaxHp));
            stats.playerDamage = c.Attack(L(UpgradeId.Attack));
            stats.soldierDamage = c.SoldierDamage(L(UpgradeId.SoldierLevel));
            startSoldiers = c.StartSoldiers(L(UpgradeId.StartSoldiers));

            if (Skills != null)
            {
                int a = L(UpgradeId.AreaDamage), h = L(UpgradeId.Heal), r = L(UpgradeId.Rage);
                Skills.aoeCooldown = c.AoeCooldown(a);
                Skills.aoeRadius = c.AoeRadius(a);
                Skills.aoeDamageMult = c.AoeMult(a);
                Skills.healCharges = c.HealCharges(h);
                Skills.healFraction = c.HealFraction(h);
                Skills.rageDuration = c.RageDuration(r);
                Skills.rageCooldown = c.RageCooldown(r);
            }
            if (Army != null) Army.SetTier(c.EvolutionTier(c.SoldierLevel(L(UpgradeId.SoldierLevel))));
        }

        void StartRun()
        {
            ApplyUpgrades();
            Player.SetMaxHp(playerMaxHp);
            Skills.Init();
            Hud.SetVisible(true);
            State = GameState.Playing;
            SupportTier = ComputeSupportTier();
            if (startSoldiers + SupportSoldiers > 0) Army.Add(startSoldiers + SupportSoldiers);
            if (Endless) Director.BeginEndless();
            else Director.Begin(CurrentLevel);
            if (SupportTier > 0)
                Hud.ToastLong(Loc.T("KOMUTAN DESTEĞİ!\nDüşmanlar zayıfladı · +", "COMMANDER SUPPORT!\nEnemies weakened · +") + SupportSoldiers + Loc.T(" asker", " soldiers"),
                    new Color(1f, 0.85f, 0.3f), 56, 2.6f);
        }

        /// <summary>
        /// Sonsuz Savunma'da diyar değişimi (saha boşken, ekran beyaza dönmüşken çağrılır): ortam, efektler,
        /// düşman havuzları, sis/kamera rengi, ortam sesi ve HUD'daki diyar adı yeni diyara göre yeniden kurulur.
        /// </summary>
        public void SwitchWorld(int worldIndex)
        {
            Level = GameDatabase.FirstLevelOf(worldIndex);
            var old = transform.Find("Environment");
            if (old != null)
            {
                old.name = "Environment_Old";
                old.gameObject.SetActive(false);
                Destroy(old.gameObject);
            }
            CorridorBuilder.Build(transform, corridorHalfWidth, this);
            if (AppBoot.LowEnd) PerfTuning.ReduceEnvironmentEffects(transform);
            if (Effects.I != null) Destroy(Effects.I.gameObject);
            Effects.Create(transform);
            Enemies.RebuildPools();
            Gates.ClearAll();
            if (cam != null) cam.backgroundColor = CorridorBuilder.FogColor;
            Sfx.SetTheme(Theme);
            Sfx.SetAmbience(true);
        }

        const float CameraZoomIn = 0.12f;

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.transform.rotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            // Kamera bakış yönünde zemine olan mesafenin %12'si kadar yaklaştırılır (kullanıcı isteği, 11 Ekim)
            var fwd = cam.transform.forward;
            float groundDist = fwd.y < -0.01f ? cameraPosition.y / -fwd.y : 0f;
            cam.transform.position = cameraPosition + fwd * groundDist * CameraZoomIn;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CorridorBuilder.FogColor;
            cam.farClipPlane = 120f;
            camBasePos = cam.transform.position;
            camBaseRot = cam.transform.rotation;
            camFocusK = 0f;
            FitCamera();
        }

        /// <summary>Koridorun tamamı ekrana sığsın diye dikey FOV'u ekran oranına göre ayarlar.</summary>
        void FitCamera()
        {
            float aspect = Mathf.Max(0.3f, cam.aspect);
            float hHalf = cameraHorizontalHalfFov * Mathf.Deg2Rad;
            float vfov = 2f * Mathf.Atan(Mathf.Tan(hHalf) / aspect) * Mathf.Rad2Deg;
            // Tablet gibi geniş ekranlarda dikey açı çok daralıp uzaktan gelen düşmanlar / kapılar geç görünmesin:
            // en az 16:9 telefondaki kadar (82°) ileriyi göster; yanlarda biraz daha fazla dekor görünür.
            cam.fieldOfView = Mathf.Clamp(vfov, 82f, 95f);
        }

        void Update()
        {
            if (Playing)
            {
                runTime += Time.unscaledDeltaTime;
                if (Army != null && Army.Count > RunMaxArmy) RunMaxArmy = Army.Count;
            }
            if (cam == null) return;
            FitCamera();
            bool focus = camFocusK > 0f;
            if (shakeT > 0f || focus || camDirty)
            {
                Vector3 p = focus ? Vector3.Lerp(camBasePos, camFocusPos, camFocusK) : camBasePos;
                Quaternion r = focus ? Quaternion.Slerp(camBaseRot, camFocusRot, camFocusK) : camBaseRot;
                if (shakeT > 0f)
                {
                    shakeT -= Time.deltaTime;
                    if (shakeT > 0f) p += (Vector3)(Random.insideUnitCircle * shakeAmp);
                }
                cam.transform.SetPositionAndRotation(p, r);
                camDirty = focus;   // odak bitince bir kare daha yazılıp tam eski konuma oturur
            }
        }

        /// <summary>
        /// Kamerayı geçici olarak bir noktaya yaklaştırır (boss ölüm sahnesi). k: 0 = normal kamera, 1 = odak konumu.
        /// Hedefe, normal kameradan hedefe olan mesafenin 'closer' oranı kadar yaklaşır ve bakışını kısmen hedefe çevirir.
        /// k = 0 verilince kamera tam eski konumuna / açısına döner. Sarsıntı odak konumunun üstüne uygulanır.
        /// </summary>
        public void SetCameraFocus(Vector3 target, float closer, float lookAt, float k)
        {
            camFocusK = Mathf.Clamp01(k);
            if (camFocusK <= 0f) return;
            camFocusPos = Vector3.Lerp(camBasePos, target, Mathf.Clamp01(closer));
            var toTarget = target - camFocusPos;
            if (toTarget.sqrMagnitude < 0.01f) { camFocusRot = camBaseRot; return; }
            camFocusRot = Quaternion.Slerp(camBaseRot, Quaternion.LookRotation(toTarget.normalized, Vector3.up), Mathf.Clamp01(lookAt));
        }

        public void Shake(float amplitude, float duration)
        {
            shakeAmp = Mathf.Max(shakeAmp * (shakeT > 0 ? 1 : 0), amplitude);
            shakeT = Mathf.Max(shakeT, duration);
        }

        public void AddGold(int amount) => RunGold += amount;

        public void ApplyGate(GateDef g)
        {
            switch (g.type)
            {
                case GateType.Soldiers:
                    if (g.value > 0) Army.Add(g.value); else Army.Remove(-g.value);
                    break;
                case GateType.Multiply:
                    Army.Multiply(g.value);
                    break;
                case GateType.FireRate:
                    stats.fireRateMult = Mathf.Clamp(stats.fireRateMult * (1f + g.value / 100f), 0.5f, 3f);
                    break;
                case GateType.Damage:
                    stats.damageMult = Mathf.Clamp(stats.damageMult * (1f + g.value / 100f), 0.5f, 3f);
                    break;
                case GateType.ExtraBullet:
                    stats.extraBullets = Mathf.Min(stats.extraBullets + 1, 2);
                    break;
                case GateType.Pierce:
                    stats.pierce = Mathf.Min(stats.pierce + 1, 2);
                    break;
                case GateType.Crit:
                    stats.critChance = Mathf.Min(stats.critChance + g.value / 100f, 0.3f);
                    break;
            }
            Hud.Toast(g.ShortLabel, g.IsPositive);
            if (g.IsPositive) DailyQuests.Add(QuestType.Gates, 1);
            if (g.IsPositive && g.IsWeaponBuff) DailyQuests.Add(QuestType.WeaponGates, 1);
        }

        public void Win()
        {
            if (!Playing || Endless) return;
            bool replay = IsReplay;
            State = GameState.Won;
            int reward = RunGold + (CurrentLevel != null ? CurrentLevel.completionReward : 50);
            if (replay) reward = Mathf.RoundToInt(reward * GameSession.ReplayRewardMultiplier);
            Save.gold += reward;
            RecordStats(true, reward);
            LastStars = StarsFor(Player != null && Player.MaxHp > 0 ? Player.Hp / Player.MaxHp : 1f);
            NewBestStars = Save.SetStars(Level, LastStars);
            if (LastStars >= 3) DailyQuests.Add(QuestType.Stars, 1);
            if (Save.failLevel == Level) Save.failStreak = 0;   // Komutan desteği sayacı
            if (!replay && Level == Save.level)
            {
                Save.level++;
                Save.bestLevel = Mathf.Max(Save.bestLevel, Save.level);
            }
            SaveSystem.Save();
            Sfx.Play(Snd.Win);
            Haptics.Vibrate(60);
            Sfx.SetAmbience(true, 0.15f);
            Hud.ShowEnd(true, reward, replay);
        }

        public void Lose()
        {
            if (!Playing) return;
            if (Endless) { LoseEndless(); return; }
            bool replay = IsReplay;
            State = GameState.Lost;
            int reward = RunGold / 2;
            if (replay) reward = Mathf.RoundToInt(reward * GameSession.ReplayRewardMultiplier);
            Save.gold += reward;
            RecordStats(false, reward);
            // Komutan desteği: aynı seviyede art arda yenilgi sayısı
            if (Save.failLevel == Level) Save.failStreak++;
            else { Save.failLevel = Level; Save.failStreak = 1; }
            SaveSystem.Save();
            Haptics.Vibrate(220);
            Sfx.Play(Snd.Lose);
            Sfx.SetAmbience(true, 0.15f);
            Hud.ShowEnd(false, reward, replay);
        }

        /// <summary>Sonsuz Savunma bitti: toplanan altının tamamı verilir, dalga rekoru güncellenir.</summary>
        void LoseEndless()
        {
            State = GameState.Lost;
            int reward = RunGold;
            Save.gold += reward;
            RecordStats(null, reward);
            int wave = Director.Wave;
            EndlessNewRecord = wave > Save.endlessBestWave;
            EndlessPrevBest = Save.endlessBestWave;
            if (EndlessNewRecord) Save.endlessBestWave = wave;
            Save.endlessRuns++;
            // tek koşuluk kişisel rekorlar (Profil > Sonsuz Savunma)
            Save.endlessBestKills = Mathf.Max(Save.endlessBestKills, KillCount);
            Save.endlessBestArmy = Mathf.Max(Save.endlessBestArmy, RunMaxArmy);
            Save.endlessBestGold = Mathf.Max(Save.endlessBestGold, reward);
            Save.endlessBestBosses = Mathf.Max(Save.endlessBestBosses, Director.EndlessBossesKilled);
            Save.endlessBestTime = Mathf.Max(Save.endlessBestTime, runTime);
            SaveSystem.Save();
            Haptics.Vibrate(220);
            Sfx.Play(EndlessNewRecord ? Snd.Win : Snd.Lose);
            Sfx.SetAmbience(true, 0.15f);
            Hud.ShowEnd(false, reward, false);
        }

        /// <summary>Son Sonsuz Savunma koşusu rekor kırdı mı / önceki rekor (bitiş ekranı için).</summary>
        public bool EndlessNewRecord { get; private set; }
        public int EndlessPrevBest { get; private set; }

        /// <summary>Bölüm sonu istatistiklerini kayda işler (her bölümde bir kez). won=null: yarıda bırakıldı.</summary>
        void RecordStats(bool? won, int goldEarned)
        {
            if (statsRecorded) return;
            statsRecorded = true;
            var s = Save;
            s.gamesPlayed++;
            s.totalKills += KillCount;
            s.totalGoldEarned += goldEarned;
            s.playTimeSeconds += runTime;
            s.maxArmy = Mathf.Max(s.maxArmy, RunMaxArmy);
            if (won == true)
            {
                s.levelsWon++;
                if (IsBossLevel) s.bossesDefeated++;
            }
            else if (won == false) s.runsLost++;
            // günlük görevler
            DailyQuests.Add(QuestType.Kill, KillCount);
            DailyQuests.Add(QuestType.Gold, goldEarned);
            DailyQuests.Max(QuestType.Army, RunMaxArmy);
            if (won.HasValue || Endless) DailyQuests.Add(QuestType.Play, 1);
            if (won == true) DailyQuests.Add(QuestType.Win, 1);
            if (won == true && IsBossLevel) DailyQuests.Add(QuestType.Boss, 1);
            if (Endless && Director != null) DailyQuests.Max(QuestType.Endless, Director.Wave);
        }

        // --- duraklatma ---
        /// <summary>Oyunu durdurur: tüm sistemler (Playing kontrolü) ve zaman (timeScale 0) durur, sesler susar.</summary>
        public void Pause()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Hud.ShowPause(true);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Hud.ShowPause(false);
        }

        public void TogglePause()
        {
            if (Paused) Resume(); else Pause();
        }

        /// <summary>Telefon arka plana atılınca (ana ekran, arama) oyun kendiliğinden duraklar.</summary>
        void OnApplicationPause(bool paused)
        {
            if (paused) Pause();
        }

        void OnDestroy()
        {
            // Sahne değişirken zaman ve ses mutlaka normale dönsün
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        // --- sonuç ekranı butonları ---
        public void PlayNextLevel()
        {
            if (HasNextLevel && GameSession.IsUnlocked(Level + 1)) GameSession.Play(Level + 1);
            else GameSession.GoToMenu();
        }

        public void RetryLevel()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (State == GameState.Paused || State == GameState.Playing) { RecordStats(null, 0); SaveSystem.Save(); }
            if (Endless) GameSession.PlayEndless();
            else GameSession.Play(Level);
        }
        /// <summary>Ana menüye döner (duraklatma menüsünden çıkılırsa bu bölümde toplanan altın kaydedilmez).</summary>
        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            // Yarıda bırakılan bölüm: altın kaydedilmez ama süre ve öldürülen düşmanlar istatistiğe işlenir
            if (State == GameState.Paused || State == GameState.Playing)
            {
                RecordStats(null, 0);
                SaveSystem.Save();
            }
            GameSession.GoToMenu();
        }
    }
}
