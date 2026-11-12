using System;
using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Seviyeyi LevelData'ya göre oynatır: dalgalar, kapı sıraları ve (varsa) boss.
    /// Boss'suz seviyeler, tüm olaylar bitip sahada düşman kalmayınca kazanılır.
    /// </summary>
    public class LevelDirector : MonoBehaviour
    {
        struct Ev
        {
            public float t;
            public Action a;
        }

        readonly List<Ev> events = new();
        int index;
        float time;
        float endTime;
        bool running;
        bool hasBoss;
        GameManager gm;

        public Boss Boss { get; private set; }
        /// <summary>
        /// İlerleme: seviyedeki düşmanların ne kadarı bertaraf edildi (öldürülen ya da kahramana ulaşıp yok olan).
        /// Örn. 100 düşmandan 20'si vuruldu = %20. Boss'lu seviyede boss son %20'lik dilimi oluşturur (canı azaldıkça dolar).
        /// </summary>
        public float Progress => endless ? EndlessProgress : KillProgress;

        // dengeleme: 5. ve 6. diyarlarda minyon sayısı %10 az (az askerli oyuncu da baş edebilsin)
        static readonly float[] MinionCountByWorld = { 1f, 1f, 1f, 1f, 0.9f, 0.9f };

        int totalEnemies, resolvedEnemies;
        bool bossDefeated;

        float KillProgress
        {
            get
            {
                if (totalEnemies <= 0 && !hasBoss) return endTime > 0f ? Mathf.Clamp01(time / endTime) : 0f;
                float bossW = hasBoss ? Mathf.Max(5f, totalEnemies * 0.25f) : 0f;   // boss ≈ toplamın %20'si
                float bossDone = 0f;
                if (bossDefeated) bossDone = 1f;
                else if (Boss != null && Boss.Enemy != null && Boss.Enemy.maxHp > 0f) bossDone = 1f - Mathf.Clamp01(Boss.Enemy.hp / Boss.Enemy.maxHp);
                return Mathf.Clamp01((resolvedEnemies + bossW * bossDone) / (totalEnemies + bossW));
            }
        }

        public void Init()
        {
            gm = GameManager.I;
        }

        public void Begin(LevelData data)
        {
            events.Clear();
            index = 0;
            time = 0f;
            totalEnemies = resolvedEnemies = 0;
            bossDefeated = false;
            if (data != null) Load(data);
            else BuildFallback();
            events.Sort((a, b) => a.t.CompareTo(b.t));
            running = true;
        }

        void Load(LevelData d)
        {
            int world = Mathf.Clamp(GameDatabase.WorldIndexOf(gm.Level), 0, MinionCountByWorld.Length - 1);
            foreach (var w in d.waves)
            {
                int n = w.kind == EnemyKind.Minion ? Mathf.Max(1, Mathf.RoundToInt(w.count * MinionCountByWorld[world])) : w.count;
                AddWave(w.time, w.kind, n, w.duration);
            }
            foreach (var g in d.gateRows) Gate(g.time, g.first, g.second);
            hasBoss = d.hasBoss;
            endTime = d.EndTime;
            if (hasBoss) events.Add(new Ev { t = d.bossTime, a = SpawnBoss });
        }

        /// <summary>Veri dosyası atanmamışsa kullanılan prototip seviyesi.</summary>
        void BuildFallback()
        {
            float t = 1f;
            for (int i = 0; i < 10; i++)
            {
                AddWave(t, EnemyKind.Minion, 8 + i * 2, 4f);
                if (i % 3 == 2) AddWave(t + 1f, EnemyKind.Brute, 1 + i / 3, 2f);
                Gate(t + 1f, GateDef.Soldiers(1 + i % 3), i % 2 == 0 ? GateDef.FireRate(20) : GateDef.Soldiers(2));
                t += 4f;
            }
            hasBoss = true;
            endTime = t;
            events.Add(new Ev { t = t, a = SpawnBoss });
        }

        void AddWave(float start, EnemyKind kind, int count, float duration)
        {
            totalEnemies += count;
            for (int i = 0; i < count; i++)
            {
                float at = start + (count > 1 ? duration * i / (count - 1) : 0f);
                events.Add(new Ev { t = at, a = () => SpawnEnemy(kind) });
            }
        }

        void Gate(float at, GateDef first, GateDef second)
        {
            events.Add(new Ev { t = at, a = () => gm.Gates.SpawnRow(first, second) });
        }

void SpawnEnemy(EnemyKind kind)
        {
            float lim = gm.corridorHalfWidth - 0.6f;
            float x = UnityEngine.Random.Range(-lim, lim);
            // Menzilli düşmanlar bir şeridin ortasına yerleşir; mermileri o şerit boyunca gelir
            if (kind == EnemyKind.Gunner || kind == EnemyKind.Harpooner)
                x = gm.LaneCenter(UnityEngine.Random.Range(0, GameManager.LaneCount)) + UnityEngine.Random.Range(-0.25f, 0.25f);
            var e = gm.Enemies.Spawn(kind, new Vector3(x, 0f, gm.spawnZ + UnityEngine.Random.Range(0f, 1.5f)));
            if (e != null && !endless) e.onDeath += _ => resolvedEnemies++;   // ilerleme sayacı
            // Sonsuz Savunma: 8. dalgadan sonra bazı minyon ve iri düşmanlar kalkanlı gelir
            if (e != null && endless && Stage > ArmorAfterWave && (kind == EnemyKind.Minion || kind == EnemyKind.Brute)
                && UnityEngine.Random.value < ArmorChance)
                e.SetArmor();
        }

        void SpawnBoss()
        {
            // Şelalenin arkasındaki tünelde doğar, yürüyüp suyun içinden çıkar
            Boss = Boss.Create(gm.transform, new Vector3(0f, 0f, gm.spawnZ + 2f));
            Sfx.Play(Snd.BossRoar);
            var boss = Boss;
            Boss.Enemy.onDeath += _ =>
            {
                // diyara özel ölüm sahnesi (BossDeath), bitince zafer / Sonsuz Savunma devamı
                if (gm.Hud != null) gm.Hud.HideBoss();
                bossDefeated = true;
                BossDeath.Play(boss, () =>
                {
                    if (boss != null) Destroy(boss.gameObject);
                    if (endless) OnEndlessBossDead();
                    else gm.Win();
                });
            };
            gm.Hud.ShowBoss(Boss.displayName);
        }

        void Update()
        {
            if (endless) { UpdateEndless(); return; }
            if (!running || gm == null || !gm.Playing) return;
            time += Time.deltaTime;
            while (index < events.Count && events[index].t <= time)
            {
                events[index].a();
                index++;
            }

            // Boss'suz seviye: her şey çıktı ve sahada düşman kalmadıysa kazan
            if (!hasBoss && index >= events.Count && time > endTime + 1f && gm.Enemies.AliveCount == 0)
                gm.Win();
        }

        // ================================================================== SONSUZ SAVUNMA
        // Dalga dalga, ölene kadar. Her dalga bir "tehdit puanı" bütçesiyle kurulur (Minion 1, Zıpkıncı 4, Brute 8);
        // bütçe bir kalıba göre harcanır (sürü, tank duvarı, nişancı hattı, karışık). Her 3 dalgada bir saha temizlenince
        // ekran beyaza döner ve rastgele başka bir diyara geçilir (o diyarın ortamı, düşmanları, sesi). Her 9. dalga,
        // o an bulunulan diyarın boss'udur. Yeni diyara varınca ödül kapıları gelir (nefes alma).

        public const int WavesPerLand = 3;
        public const int BossEvery = 9;
        const float WaveLength = 14f;
        const float WaveGap = 2.5f;

        enum EPhase { Wave, WaitClear, Transition, Arrive }

        bool endless;
        EPhase ePhase;
        float eT;
        bool bossAlive, switched;
        readonly List<Ev> waveEvents = new();
        int waveIndex;

        /// <summary>Şu anki dalga (1'den başlar).</summary>
        /// <summary>İç aşama sayacı (her diyarda 3 aşama). Zorluk, kapılar ve boss bu sayaca göre kurulur.</summary>
        public int Stage { get; private set; }
        /// <summary>
        /// Oyuncuya gösterilen dalga: her diyar (3 aşama) bir dalga. Rekor, görev ve bitiş ekranı bunu kullanır.
        /// Her 3. dalganın son aşaması boss'tur.
        /// </summary>
        public int Wave => Stage <= 0 ? 0 : (Stage - 1) / WavesPerLand + 1;
        /// <summary>Dalganın içindeki aşama (1-3).</summary>
        public int SubStage => Stage <= 0 ? 0 : (Stage - 1) % WavesPerLand + 1;

        void ShowEndlessInfo(int stage, string name)
        {
            stage = Mathf.Max(1, stage);
            gm.Hud.SetEndlessInfo((stage - 1) / WavesPerLand + 1, (stage - 1) % WavesPerLand + 1, WavesPerLand, stage % BossEvery == 0, name);
        }
        public int EndlessBossesKilled { get; private set; }
        public bool IsBossWave => Stage % BossEvery == 0;
        // 1. dalga ≈ kampanyanın 10. seviyesi (Sonsuz Savunma 10. seviyeden sonra açılır): düşman canı x1,9'dan başlar,
        // her dalgada +0,12 (kampanyada seviye başına +0,10).
        public const float EndlessStartHp = 1.9f, EndlessHpPerWave = 0.12f;
        public float EndlessHpMult => (EndlessStartHp + EndlessHpPerWave * (Stage - 1)) * ArmyHpFactor;
        public float EndlessBossMult => (1f + 0.6f * (Mathf.Max(1, Stage / BossEvery) - 1)) * ArmyBossFactor;

        // --- Orduya göre güçlenme (yalnız Sonsuz Savunma) ---
        // Ordu büyüdükçe ateş gücü de büyüyor; düşmanlar portaldan çıkmadan eriyordu. Ateş gücü ≈ asker sayısı + 6 (kahraman).
        // 12 askerin üstünde düşman canı ateş gücünün 0,8. kuvvetiyle artar (41 askerde x2,15, 80 askerde x3,5; en fazla x5),
        // boss canı bunun karekökü kadar, dalga kalabalığı asker başına +%1,2 (en fazla x1,6).
        // Büyük ordu yine avantajlıdır (ateş gücü canınkinden hızlı artar), ama düşmanlar portalda erimez.
        public const int ArmyScaleFrom = 12;
        int ArmyCount => gm != null && gm.Army != null ? gm.Army.Count : 0;
        int ArmyOver => Mathf.Max(0, ArmyCount - ArmyScaleFrom);
        // Oyuncunun o anki saniyelik hasarı (ordu + kahraman + kapılardan toplanan atış hızı / hasar / kritik / mermi / delme),
        // aynı yükseltmelerle 12 askerli ve kapı güçlendirmesiz haline oranlanır; düşman canı bu oranın 0,85. kuvvetiyle artar.
        // Böylece kapılardan toplanan her güç biraz avantaj verir ama düşmanlar portalda erimez. Yükseltmeler (kalıcı güç) oranı
        // etkilemez, yani yükseltme yapan oyuncu yine daha ileri gider.
        float Dps(int army, bool withBuffs)
        {
            if (gm == null || gm.Army == null) return 1f;
            var st = gm.stats;
            float fr = withBuffs ? st.fireRateMult : 1f, dm = withBuffs ? st.damageMult : 1f, crit = withBuffs ? st.critChance : 0f;
            int xb = withBuffs ? st.extraBullets : 0, pc = withBuffs ? st.pierce : 0;
            int shooters = Mathf.Min(army, Army.VisibleCap), over = Mathf.Max(0, army - Army.VisibleCap);
            float soldiers = shooters * st.soldierDamage * (1f + over * gm.Army.overflowBonusPerSoldier) / Mathf.Max(0.05f, st.soldierFireInterval);
            float hero = st.playerDamage * (1 + xb) / Mathf.Max(0.05f, st.playerFireInterval);
            return Mathf.Max(1f, (soldiers + hero) * fr * dm * (1f + crit) * (1f + 0.5f * pc));
        }
        public float ArmyHpFactor => Mathf.Clamp(Mathf.Pow(Dps(ArmyCount, true) / Dps(ArmyScaleFrom, false), 0.85f), 1f, 80f);
        public float ArmyBossFactor => Mathf.Sqrt(ArmyHpFactor);
        public float CrowdFactor => Mathf.Min(1.6f, 1f + 0.012f * ArmyOver);
        const int MaxWaveBudget = 380;   // telefon performansı: bir dalgada en fazla bu kadar tehdit puanı

        // --- Zırhlı düşmanlar ---
        // 8. dalgadan sonra minyon / iri düşmanların bir kısmı kalkanla gelir: kalkan kırılana kadar hasarın yarısını alırlar.
        public const int ArmorAfterWave = 9;   // aşama: 10. aşamadan (4. dalga) itibaren
        bool armorAnnounced;   // ilk zırhlı (boss olmayan) dalgada bir kez duyurulur
        public float ArmorChance => Stage <= ArmorAfterWave ? 0f : Mathf.Min(0.35f, 0.08f + 0.03f * (Stage - ArmorAfterWave - 1));

        /// <summary>İlerleme çubuğu: sıradaki boss'a kadar olan yol (9 dalgalık döngü).</summary>
        float EndlessProgress
        {
            get
            {
                float inWave = ePhase == EPhase.Wave ? Mathf.Clamp01(eT / WaveLength) : 1f;
                int done = (Stage - 1) % BossEvery;
                return Mathf.Clamp01((done + inWave) / BossEvery);
            }
        }

        /// <summary>Rastgele bir diyar (current hariç). Henüz olmayan dünyalar seçilmez.</summary>
        public static int RandomWorld(int current)
        {
            var db = GameSession.Db;
            int n = db != null ? Mathf.Max(1, db.worlds.Count) : 1;
            if (n == 1) return 0;
            int w = UnityEngine.Random.Range(0, n - (current >= 0 ? 1 : 0));
            if (current >= 0 && w >= current) w++;
            return w;
        }

        public void BeginEndless()
        {
            endless = true;
            running = true;
            hasBoss = false;
            Stage = 0;
            EndlessBossesKilled = 0;
            armorAnnounced = false;
            StartArrive(true);
        }

        int CurrentWorldIndex => GameDatabase.WorldIndexOf(gm.Level);

        void StartArrive(bool first)
        {
            ePhase = EPhase.Arrive;
            eT = 0f;
            var name = Loc.Upper(GameSession.Db != null ? GameSession.Db.WorldName(CurrentWorldIndex) : "");
            ShowEndlessInfo(Stage + 1, name);
            int nextWave = Stage / WavesPerLand + 1;
            gm.Hud.Toast((first ? Loc.T("SONSUZ SAVUNMA\n", "ENDLESS DEFENSE\n") : "") + Loc.T("DALGA ", "WAVE ") + nextWave + "  ·  " + name, true);
            // nefes: yeni diyara varınca ödül kapıları
            if (!first) gm.Gates.SpawnRow(SoldierGate(Stage + 1, true), PowerGate(Stage + 1));
        }

        void StartWave()
        {
            Stage++;
            ePhase = EPhase.Wave;
            eT = 0f;
            waveIndex = 0;
            waveEvents.Clear();
            BuildWave(Stage);
            waveEvents.Sort((a, b) => a.t.CompareTo(b.t));
            var name = Loc.Upper(GameSession.Db != null ? GameSession.Db.WorldName(CurrentWorldIndex) : "");
            ShowEndlessInfo(Stage, name);
            if (!armorAnnounced && Stage > ArmorAfterWave && !IsBossWave) { armorAnnounced = true; gm.Hud.ToastLong(Loc.T("DALGA ", "WAVE ") + Wave + Loc.T("\nZIRHLI DÜŞMANLAR GELİYOR!", "\nARMORED ENEMIES INCOMING!"), new Color(0.75f, 0.85f, 1f), 60, 2.4f); }
            // aşama yazısı yok: dalga numarası yalnız yeni diyara varınca gösterilir
            if (Stage > 1) gm.AddGold(4 + 2 * Stage);   // dalga bonusu
        }

        void UpdateEndless()
        {
            if (!running || gm == null || !gm.Playing) return;
            float dt = Time.deltaTime;
            eT += dt;
            switch (ePhase)
            {
                case EPhase.Arrive:
                    if (eT >= 2.2f) StartWave();
                    break;

                case EPhase.Wave:
                    while (waveIndex < waveEvents.Count && waveEvents[waveIndex].t <= eT)
                    {
                        waveEvents[waveIndex].a();
                        waveIndex++;
                    }
                    bool eventsDone = waveIndex >= waveEvents.Count;
                    if (IsBossWave)
                    {
                        // boss dalgası boss ölünce biter (diyar sonu)
                        if (eventsDone && !bossAlive && eT > 3f) { ePhase = EPhase.WaitClear; eT = 0f; }
                    }
                    else if (eventsDone && eT >= WaveLength + WaveGap)
                    {
                        if (Stage % WavesPerLand == 0) { ePhase = EPhase.WaitClear; eT = 0f; }
                        else StartWave();
                    }
                    break;

                case EPhase.WaitClear:
                    // diyar değişimi saha boşken olur (düşman modelleri ve ortam baştan kurulur)
                    if (gm.Enemies.AliveCount == 0 && eT > 0.6f)
                    {
                        ePhase = EPhase.Transition;
                        eT = 0f;
                        switched = false;
                        Sfx.Play(Snd.Summon);
                    }
                    break;

                case EPhase.Transition:
                {
                    // 0.45 sn beyaza döner, değişim, 0.6 sn'de açılır
                    float a = eT < 0.45f ? eT / 0.45f : 1f - Mathf.Clamp01((eT - 0.55f) / 0.6f);
                    gm.Hud.SetFlash(Mathf.Clamp01(a));
                    if (!switched && eT >= 0.45f)
                    {
                        switched = true;
                        gm.SwitchWorld(RandomWorld(CurrentWorldIndex));
                    }
                    if (eT >= 1.15f)
                    {
                        gm.Hud.SetFlash(0f);
                        StartArrive(false);
                    }
                    break;
                }
            }
        }

        void OnEndlessBossDead()
        {
            bossAlive = false;
            Boss = null;
            EndlessBossesKilled++;
            gm.Save.bossesDefeated++;
            DailyQuests.Add(QuestType.Boss, 1);
            gm.Hud.HideBoss();
            int bonus = 40 + 10 * Stage;
            gm.AddGold(bonus);
            gm.Hud.Toast(Loc.T("BOSS YENİLDİ! +", "BOSS DEFEATED! +") + bonus, true);
            Sfx.Play(Snd.Win);
            Haptics.Vibrate(80);
        }

        // ------------------------------------------------------------------ dalga üretimi
        enum Pattern { Swarm, Wall, Snipers, Mixed }

        void At(float t, Action a) => waveEvents.Add(new Ev { t = t, a = a });

        void Group(float start, EnemyKind kind, int count, float duration)
        {
            for (int i = 0; i < count; i++)
            {
                float at = start + (count > 1 ? duration * i / (count - 1) : 0f);
                At(at, () => SpawnEnemy(kind));
            }
        }

        void BuildWave(int n)
        {
            // tehdit puanı: 1. aşama 105 (kampanya 9-10. seviyenin yoğunluğunun biraz altı; kullanıcı isteğiyle 115'ten 10 düşürüldü), her aşamada +5, en fazla 290
            int budget = Mathf.Min(Mathf.RoundToInt(Mathf.Min(105 + 5 * (n - 1), 290) * CrowdFactor), MaxWaveBudget);

            if (IsBossWave)
            {
                bossAlive = true;
                At(1.5f, () => SpawnBoss());
                Group(0.3f, EnemyKind.Minion, Mathf.RoundToInt(budget * 0.25f), 3f);
                Group(9f, EnemyKind.Minion, Mathf.RoundToInt(budget * 0.25f), 4f);
                At(0.8f, () => gm.Gates.SpawnRow(SoldierGate(n, false), PowerGate(n)));
                return;
            }

            Pattern p;
            if (n <= 2) p = UnityEngine.Random.value < 0.5f ? Pattern.Swarm : Pattern.Mixed;
            else
            {
                float r = UnityEngine.Random.value;
                p = r < 0.3f ? Pattern.Swarm : r < 0.55f ? Pattern.Wall : r < 0.75f ? Pattern.Snipers : Pattern.Mixed;
            }

            // bütçe 3 kümeye bölünür; her küme 3 sn'ye yayılır
            float[] starts = { 0.5f, 5f, 9.5f };
            for (int c = 0; c < starts.Length; c++)
            {
                int b = budget / 3;
                int brutes = 0, harps = 0;
                switch (p)
                {
                    case Pattern.Wall: brutes = Mathf.Max(1, Mathf.RoundToInt(b * 0.45f / 8f)); break;
                    case Pattern.Snipers: harps = Mathf.Max(1, Mathf.RoundToInt(b * 0.35f / 4f)); break;
                    case Pattern.Mixed:
                        brutes = n >= 2 ? Mathf.RoundToInt(b * 0.2f / 8f) : 0;
                        harps = Mathf.RoundToInt(b * 0.2f / 4f);
                        break;
                }
                harps = Mathf.Min(harps, 4);   // menzilli düşman sayısı sınırlı (ekran mızrakla dolmasın)
                int minions = Mathf.Max(0, b - brutes * 8 - harps * 4);
                float s = starts[c];
                if (brutes > 0) Group(s, EnemyKind.Brute, brutes, 1.5f);          // duvar önde
                if (harps > 0) Group(s + 0.8f, EnemyKind.Harpooner, harps, 2f);
                if (minions > 0) Group(s + (brutes > 0 ? 0.8f : 0f), EnemyKind.Minion, minions, 3f);
            }

            // 3 kapı sırası (kampanya 10. seviyedeki gibi ~4,5 sn'de bir)
            foreach (float gt in new[] { 1.5f, 6f, 10.5f })
                At(gt, () =>
                {
                    // 6. dalgadan sonra tuzak sıra: iki kapı da eksi, tek kurtuluş boş şerit (sıklığı dalgayla artar, en fazla %30)
                    bool trap = n >= 7 && UnityEngine.Random.value < Mathf.Min(0.3f, 0.06f + 0.02f * (n - 7));   // 7. aşama = 3. dalga
                    gm.Gates.SpawnRow(trap ? NegativeGate(n) : SoldierGate(n, false), SecondGate(n));
                });
        }

        static GateDef SoldierGate(int n, bool generous)
        {
            // mavi kapı değerleri kampanya 10. seviyesindeki gibi başlar ve yavaş artar (1. dalga 2-3, 5. dalga 3-4, 10. dalga 4-6, 20. dalga 6-9), en fazla 18
            int lo = 2 + n / 5, hi = 3 + n / 3;
            hi = Mathf.Min(hi, 18);
            lo = Mathf.Min(lo, hi);
            int v = generous ? hi : UnityEngine.Random.Range(lo, hi + 1);
            return GateDef.Soldiers(Shrink(v));
        }

        static GateDef PowerGate(int n)
        {
            float r = UnityEngine.Random.value;
            if (r < 0.05f && CurArmy <= 60) return GateDef.Multiply(2);   // x2 nadir; büyük orduda hiç çıkmaz
            if (r < 0.34f) return GateDef.FireRate(new[] { 15, 20, 25 }[UnityEngine.Random.Range(0, 3)]);
            if (r < 0.54f) return GateDef.Damage(new[] { 20, 25 }[UnityEngine.Random.Range(0, 2)]);
            if (r < 0.66f) return GateDef.ExtraBullet();
            if (r < 0.78f) return GateDef.Pierce();
            if (r < 0.9f) return GateDef.Crit(10);
            return GateDef.Soldiers(Shrink(Mathf.Min(18, 3 + n / 3)));
        }

        /// <summary>İkinci kapı: güç, asker ya da (dalga ilerledikçe sıklaşan) eksi kapı.</summary>
        static GateDef SecondGate(int n)
        {
            // kırmızı kapı olasılığı: 1. dalga %24, 5. dalga %40, 10. dalgadan sonra %60
            float neg = Mathf.Min(0.6f, 0.2f + 0.04f * n);
            float r = UnityEngine.Random.value;
            if (r < neg) return NegativeGate(n);
            if (r < neg + 0.4f) return PowerGate(n);
            return SoldierGate(n, false);
        }

        /// <summary>Eksi kapı: çoğunlukla asker kaybettirir; 4. dalgadan sonra bazen ateş hızı / hasar düşürür.</summary>
        static GateDef NegativeGate(int n)
        {
            if (n >= 4 && UnityEngine.Random.value < 0.3f)   // 4. aşama = 2. dalga
                return UnityEngine.Random.value < 0.5f
                    ? GateDef.FireRate(-new[] { 10, 15 }[UnityEngine.Random.Range(0, 2)])
                    : GateDef.Damage(-new[] { 10, 15 }[UnityEngine.Random.Range(0, 2)]);
            int k = UnityEngine.Random.Range(1 + n / 3, 3 + n * 2 / 3);
            // büyük orduda kırmızı kapı da hissedilsin: en az ordunun %15'i gider
            return GateDef.Soldiers(-Mathf.Max(Mathf.Clamp(k, 1, 25), Mathf.RoundToInt(CurArmy * 0.15f)));
        }

        static int CurArmy => GameManager.I != null && GameManager.I.Army != null ? GameManager.I.Army.Count : 0;

        /// <summary>Sonsuz Savunma: ordu 60'ı geçince mavi kapıların verdiği asker azalır (60/ordu oranında, en az %25).</summary>
        static int Shrink(int v)
        {
            int a = CurArmy;
            if (a <= 60) return v;
            return Mathf.Max(1, Mathf.RoundToInt(v * Mathf.Max(0.25f, 60f / a)));
        }
    }
}
