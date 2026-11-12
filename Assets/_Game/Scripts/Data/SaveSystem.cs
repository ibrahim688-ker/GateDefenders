using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>Oyuncunun kalıcı ilerlemesi. JSON olarak cihazda saklanır.</summary>
    [System.Serializable]
    public class SaveData
    {
        /// <summary>Kayıt biçimi sürümü. SaveSystem.CurrentVersion'dan küçükse kayıt baştan başlatılır.</summary>
        public int version = SaveSystem.CurrentVersion;
        public int gold;
        public int level = 1;
        public int bestLevel = 1;
        public List<int> upgrades = new List<int>();

        // --- istatistikler (Profil sekmesi). Eski kayıtlarda yoksa 0 olarak gelir. ---
        public int totalKills;
        public int bossesDefeated;
        public int totalGoldEarned;
        public float playTimeSeconds;
        public int maxArmy;
        public int levelsWon;
        public int runsLost;
        public int gamesPlayed;
        /// <summary>Seviye başına en iyi yıldız (indeks = seviye - 1; 0 = kayıt yok).</summary>
        public List<int> stars = new List<int>();
        /// <summary>Görülen hikaye konuşmaları (bit 0 giriş + eğitim, bit 1..6 dünya sonları). Bkz. StoryData.</summary>
        public int storySeen;
        /// <summary>Alınan terfi sandıkları (bit = dünya indeksi). Bkz. PromotionChest.</summary>
        public int chestsClaimed;
        /// <summary>Sonsuz Savunma: ulaşılan en yüksek dalga ve oynanan koşu sayısı.</summary>
        public int endlessBestWave;
        public int endlessRuns;
        /// <summary>Sonsuz Savunma kişisel rekorları (tek koşuda en iyisi; Profil'de gösterilir).</summary>
        public int endlessBestKills, endlessBestArmy, endlessBestGold, endlessBestBosses;
        public float endlessBestTime;

        // günlük ödül serisi (DailyQuests): son alınan gün numarası ve o günün serideki sırası (0..6)
        public int dailyLastDay = -1;
        public int dailyIndex = -1;
        // günlük görevler
        public int questDay = -1;
        public List<int> questType = new List<int>();
        public List<int> questTarget = new List<int>();
        public List<int> questProgress = new List<int>();
        public List<int> questReward = new List<int>();
        public int questClaimed;
        public bool questBonusClaimed;
        public int questSeed;
        // Komutan desteği: aynı seviyede art arda kaç kez kaybedildi
        public int failLevel;
        public int failStreak;   // görev seçimi için kuruluma özel rastgele tohum

        public void EnsureQuests()
        {
            questType ??= new List<int>();
            questTarget ??= new List<int>();
            questProgress ??= new List<int>();
            questReward ??= new List<int>();
            int n = questType.Count;
            if (questTarget.Count != n || questProgress.Count != n || questReward.Count != n) { questType.Clear(); questTarget.Clear(); questProgress.Clear(); questReward.Clear(); questDay = -1; }
        }

        public int GetStars(int level) => level >= 1 && level - 1 < stars.Count ? stars[level - 1] : 0;

        /// <summary>Daha iyiyse kaydeder; yeni rekorsa true döner.</summary>
        public bool SetStars(int level, int n)
        {
            if (level < 1) return false;
            while (stars.Count < level) stars.Add(0);
            if (n <= stars[level - 1]) return false;
            stars[level - 1] = n;
            return true;
        }

        public void Ensure()
        {
            int n = System.Enum.GetValues(typeof(UpgradeId)).Length;
            while (upgrades.Count < n) upgrades.Add(0);
            if (level < 1) level = 1;
            if (bestLevel < level) bestLevel = level;
        }

        public int GetUpgrade(UpgradeId id) => upgrades[(int)id];
        public void SetUpgrade(UpgradeId id, int value) => upgrades[(int)id] = value;
    }

    public static class SaveSystem
    {
        static SaveData data;

        /// <summary>
        /// 2: 3 Ekim — kullanıcı isteğiyle tüm ilerleme sıfırlandı (test için açılmış seviyeler kapatıldı, oyun baştan).
        /// Bu sayı artırılırsa eski kayıtlar bir kez silinir.
        /// </summary>
        public const int CurrentVersion = 3;

#if UNITY_EDITOR
        /// <summary>Yalnız editör: test botu kendi kayıt dosyasını kullanır (oyuncunun kaydı değişmez).</summary>
        public static string PathOverride;
        public static string FilePath => PathOverride ?? Path.Combine(Application.persistentDataPath, "save.json");
#else
        public static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
#endif

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static void Load()
        {
            data = null;
            try
            {
                if (File.Exists(FilePath)) data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Kayıt okunamadı, yeni kayıt oluşturuluyor: " + e.Message);
            }
            if (data != null && data.version < CurrentVersion)
            {
                Debug.Log("Kayıt sürümü eski (" + data.version + " < " + CurrentVersion + "): ilerleme sıfırlanıyor.");
                data = new SaveData();
                data.Ensure();
                Save();
                return;
            }
            if (data == null) data = new SaveData();
            data.Ensure();
        }

        /// <summary>Yalnızca editör testleri için: true iken kayıt dosyasına yazılmaz (oyuncunun ilerlemesi korunur).</summary>
#if UNITY_EDITOR
        public static bool SuppressWrites;   // yalnız editör testleri (yayın build'ine girmez)
#endif

        public static void Save()
        {
#if UNITY_EDITOR
            if (SuppressWrites) return;
#endif
#if UNITY_EDITOR
            // Editör testleri: yeniden derlemeden sonra da geçerli olan koruma (SessionState editör kapanınca sıfırlanır)
            if (PathOverride == null && UnityEditor.SessionState.GetBool("StratejiGame.TestNoSave", false)) return;
#endif
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Kayıt yazılamadı: " + e.Message);
            }
        }

        public static void ResetAll()
        {
            data = new SaveData();
            data.Ensure();
            Save();
        }
    }
}
