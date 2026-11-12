using System;
using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    public enum QuestType { Kill, Win, Play, Gold, Gates, Army, Stars, Upgrade, Skills, Endless, Boss, WeaponGates }   // yeni türler sona eklenir (kayıtta sayı olarak tutulur)

    /// <summary>
    /// Günlük giriş ödülü (7 günlük seri) ve günlük 3 görev. İnternet gerekmez: cihazın yerel tarihi kullanılır.
    /// - Seri: her gün bir kez alınır; bir gün kaçırılırsa 1. güne döner; 7. günden sonra baştan başlar.
    /// - Saat geri alınırsa (bugün &lt; son alınan gün) yeni ödül / yeni görev verilmez.
    /// - Görevler her gün yenilenir: 12 görevlik havuzdan oyuncuya özel rastgele 3 tanesi seçilir, hepsi bitince ek bonus sandığı alınır.
    /// Ödüller oyuncunun ilerlemesine göre ölçeklenir (bir seviyeden kazanılan altın ≈ 60 + 20·seviye).
    /// </summary>
    public static class DailyQuests
    {
        public const int DayCount = 7;
        public const int QuestCount = 3;
        static readonly float[] DayMult = { 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f, 4f };
        const float QuestMult = 0.6f, BonusMult = 1.5f;

        static SaveData S => SaveSystem.Data;

        /// <summary>Yerel takvime göre gün numarası.</summary>
        public static int Today => (int)(DateTime.Now.Date - new DateTime(2020, 1, 1)).TotalDays;

        /// <summary>Bir sonraki yeni güne kalan süre.</summary>
        public static TimeSpan UntilTomorrow => DateTime.Now.Date.AddDays(1) - DateTime.Now;

        /// <summary>Ödüllerin ölçeği: o anki seviyeden kazanılan yaklaşık altın.</summary>
        static int LevelGold => 60 + 20 * Mathf.Clamp(S.level, 1, 61);

        static int Round(float v, int step) => Mathf.Max(step, Mathf.RoundToInt(v / step) * step);

        // ------------------------------------------------------------------ günlük seri
        public static bool DailyAvailable => Today > S.dailyLastDay;

        /// <summary>Bugün alınabilecek (alındıysa: bugün alınan) serinin gün sırası (0..6).</summary>
        public static int DailyIndex
        {
            get
            {
                int today = Today;
                if (today <= S.dailyLastDay) return Mathf.Clamp(S.dailyIndex, 0, DayCount - 1);
                if (today == S.dailyLastDay + 1 && S.dailyIndex >= 0) return (S.dailyIndex + 1) % DayCount;
                return 0;   // ilk kez ya da gün kaçırıldı
            }
        }

        /// <summary>Serideki gün kutusu alınmış mı (ekrandaki işaret için).</summary>
        public static bool DayClaimed(int i)
        {
            int cur = DailyIndex;
            return DailyAvailable ? i < cur : i <= cur;
        }

        public static int DayReward(int i) => Round(LevelGold * DayMult[Mathf.Clamp(i, 0, DayCount - 1)], 10);

        /// <summary>Günlük ödülü alır, altını kayda yazar. Alınamazsa 0.</summary>
        public static int ClaimDaily()
        {
            if (!DailyAvailable) return 0;
            int idx = DailyIndex;
            int g = DayReward(idx);
            S.dailyIndex = idx;
            S.dailyLastDay = Today;
            GiveGold(g);
            return g;
        }

        // ------------------------------------------------------------------ görevler
        /// <summary>Gün değiştiyse yeni görevleri oluşturur.</summary>
        public static void EnsureToday()
        {
            var s = S;
            if (s == null) return;
            s.EnsureQuests();
            int today = Today;
            if (s.questDay == today || (s.questDay > today && s.questType.Count == QuestCount)) return;   // saat geri alındıysa eski görevler kalır
            s.questDay = today;
            s.questType.Clear(); s.questTarget.Clear(); s.questProgress.Clear(); s.questReward.Clear();
            s.questClaimed = 0;
            s.questBonusClaimed = false;

            var pool = new List<QuestType> { QuestType.Kill, QuestType.Win, QuestType.Play, QuestType.Gold, QuestType.Gates, QuestType.Army, QuestType.Skills, QuestType.WeaponGates };
            if (s.level > 3) pool.Add(QuestType.Stars);
            if (s.level > 1) pool.Add(QuestType.Upgrade);
            if (s.level > GameDatabase.LevelsPerWorld) pool.Add(QuestType.Boss);   // ilk boss yenildikten sonra (tekrar oynanabilir)
            if (GameSession.EndlessUnlocked) pool.Add(QuestType.Endless);
            // oyuncuya özel rastgelelik: her kurulumun kendi tohumu var, aynı gün herkese aynı görevler çıkmaz
            if (s.questSeed == 0) s.questSeed = UnityEngine.Random.Range(1, int.MaxValue);
            var rng = new System.Random(unchecked(today * 7919 + s.questSeed));
            for (int i = 0; i < QuestCount && pool.Count > 0; i++)
            {
                int k = rng.Next(pool.Count);
                var t = pool[k];
                pool.RemoveAt(k);
                s.questType.Add((int)t);
                s.questTarget.Add(Target(t));
                s.questProgress.Add(0);
                s.questReward.Add(Round(LevelGold * QuestMult, 10));
            }
            SaveSystem.Save();
        }

        static int Target(QuestType t)
        {
            int L = Mathf.Clamp(S.level, 1, 60);
            switch (t)
            {
                case QuestType.Kill: return Round(2f * (80 + 14 * L), 50);
                case QuestType.Win: return 2;
                case QuestType.Play: return 3;
                case QuestType.Gold: return Round(2f * LevelGold, 50);
                case QuestType.Gates: return 15;
                case QuestType.Army: return Mathf.Clamp(Round(10 + L * 1.2f, 5), 15, 80);
                case QuestType.Stars: return 1;
                case QuestType.Upgrade: return 2;
                case QuestType.Skills: return 4;
                case QuestType.Endless: return Mathf.Clamp(S.endlessBestWave / 2 + 1, 2, 6);   // dalga = diyar (3 aşama)
                case QuestType.Boss: return 1;
                case QuestType.WeaponGates: return 6;
            }
            return 1;
        }

        public static int Count { get { EnsureToday(); return S.questType.Count; } }
        public static QuestType TypeOf(int i) => (QuestType)S.questType[i];
        public static int TargetOf(int i) => S.questTarget[i];
        public static int ProgressOf(int i) => Mathf.Min(S.questProgress[i], S.questTarget[i]);
        public static int RewardOf(int i) => S.questReward[i];
        public static bool Done(int i) => S.questProgress[i] >= S.questTarget[i];
        public static bool Claimed(int i) => (S.questClaimed & (1 << i)) != 0;
        public static int BonusReward => Round(LevelGold * BonusMult, 10);

        public static bool AllClaimed
        {
            get
            {
                for (int i = 0; i < Count; i++) if (!Claimed(i)) return false;
                return Count > 0;
            }
        }

        public static bool BonusAvailable => AllClaimed && !S.questBonusClaimed;
        public static bool BonusClaimed => S.questBonusClaimed;

        public static string Describe(int i)
        {
            int n = TargetOf(i);
            switch (TypeOf(i))
            {
                case QuestType.Kill: return Loc.T(n + " düşman yok et", "Defeat " + n + " enemies");
                case QuestType.Win: return Loc.T(n + " seviye kazan", "Win " + n + " levels");
                case QuestType.Play: return Loc.T(n + " oyun oyna", "Play " + n + " games");
                case QuestType.Gold: return Loc.T(n + " altın kazan", "Earn " + n + " gold");
                case QuestType.Gates: return Loc.T(n + " olumlu kapıdan geç", "Pass " + n + " bonus gates");
                case QuestType.Army: return Loc.T("Tek oyunda " + n + " askere ulaş", "Reach " + n + " soldiers in one run");
                case QuestType.Stars: return Loc.T("Bir seviyeyi 3 yıldızla kazan", "Win a level with 3 stars");
                case QuestType.Upgrade: return Loc.T(n + " yükseltme satın al", "Buy " + n + " upgrades");
                case QuestType.Skills: return Loc.T(n + " kez yetenek kullan", "Use skills " + n + " times");
                case QuestType.Endless: return Loc.T("Sonsuz Savunma'da " + n + ". dalgaya ulaş", "Reach wave " + n + " in Endless Defense");
                case QuestType.Boss: return Loc.T("Bir boss yen", "Defeat a boss");
                case QuestType.WeaponGates: return Loc.T(n + " silah güçlendirme kapısından geç", "Pass " + n + " weapon upgrade gates");
            }
            return "";
        }

        /// <summary>Tamamlanmış görevin ödülünü alır.</summary>
        public static int ClaimQuest(int i)
        {
            EnsureToday();
            if (i < 0 || i >= S.questType.Count || !Done(i) || Claimed(i)) return 0;
            S.questClaimed |= 1 << i;
            int g = RewardOf(i);
            GiveGold(g);
            return g;
        }

        public static int ClaimBonus()
        {
            if (!BonusAvailable) return 0;
            S.questBonusClaimed = true;
            int g = BonusReward;
            GiveGold(g);
            return g;
        }

        /// <summary>Alınmayı bekleyen bir şey var mı (menüdeki kırmızı nokta).</summary>
        public static bool AnyClaimable
        {
            get
            {
                if (S == null) return false;
                if (DailyAvailable) return true;
                int n = Count;
                for (int i = 0; i < n; i++) if (Done(i) && !Claimed(i)) return true;
                return BonusAvailable;
            }
        }

        // ------------------------------------------------------------------ ilerleme (oyundan çağrılır; kayıt, çağıranın Save'inde yazılır)
        public static void Add(QuestType t, int amount)
        {
            if (amount <= 0 || S == null) return;
            EnsureToday();
            for (int i = 0; i < S.questType.Count; i++)
                if (S.questType[i] == (int)t && S.questProgress[i] < S.questTarget[i])
                    S.questProgress[i] = Mathf.Min(S.questTarget[i], S.questProgress[i] + amount);
        }

        /// <summary>Tek oyunluk rekor türü görevler (ordu büyüklüğü, dalga): en yüksek değer tutulur.</summary>
        public static void Max(QuestType t, int value)
        {
            if (S == null) return;
            EnsureToday();
            for (int i = 0; i < S.questType.Count; i++)
                if (S.questType[i] == (int)t && value > S.questProgress[i])
                    S.questProgress[i] = Mathf.Min(S.questTarget[i], value);
        }

        static void GiveGold(int g)
        {
            S.gold += g;
            S.totalGoldEarned += g;
            SaveSystem.Save();
        }
    }
}
