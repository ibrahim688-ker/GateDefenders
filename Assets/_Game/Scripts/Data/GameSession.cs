using UnityEngine.SceneManagement;

namespace StratejiGame
{
    /// <summary>Sahneler arası ortak durum ve ilerleme kuralları.</summary>
    public static class GameSession
    {
        public const string MenuScene = "MainMenu";
        public const string GameScene = "Game";

        /// <summary>Oynanacak seviye (0 = sıradaki açık seviye).</summary>
        public static int SelectedLevel;

        /// <summary>Sonsuz Savunma modu mu oynanacak (GameManager açılışta okur).</summary>
        public static bool Endless;

        /// <summary>Sonsuz Savunma, ilk diyarın boss'u yenilince açılır.</summary>
#if UNITY_EDITOR
        public static bool EndlessUnlocked => TestUnlockAll || Save.level > GameDatabase.LevelsPerWorld;
#else
        public static bool EndlessUnlocked => Save.level > GameDatabase.LevelsPerWorld;
#endif

        /// <summary>Tekrar oynanan (daha önce geçilmiş) seviyelerde ödül çarpanı.</summary>
        public const float ReplayRewardMultiplier = 0.5f;

        public static GameDatabase Db => GameDatabase.Load();
        public static SaveData Save => SaveSystem.Data;

        /// <summary>Sıradaki oynanacak seviye (kayıttaki en yüksek açık seviye).</summary>
        public static int NextLevel => Save.level;

        public static bool IsCompleted(int level) => level < Save.level;
        /// <summary>
        /// TEST: true iken tüm seviyeler açık (kullanıcı telefonda test için istedi, 2 Ekim). İlerleme/kayıt değişmez;
        /// geçilmemiş seviyeler tam ödüllü oynanır. Yayından önce false yap.
        /// </summary>
#if UNITY_EDITOR
        public static bool TestUnlockAll = false;   // yalnız editör testleri (yayın build'ine girmez)
#endif

#if UNITY_EDITOR
        public static bool IsUnlocked(int level) => (TestUnlockAll || level <= Save.level) && Db != null && Db.HasLevel(level);
#else
        public static bool IsUnlocked(int level) => level <= Save.level && Db != null && Db.HasLevel(level);
#endif
        public static bool IsBoss(int level) => GameDatabase.IndexInWorld(level) == GameDatabase.LevelsPerWorld - 1;

        /// <summary>
        /// Kayıttaki seviye, mevcut son seviyenin bir fazlasını geçemez
        /// (eski döngü sisteminden kalan 11+ seviyeler bir sonraki dünyanın ilk seviyesine çekilir).
        /// </summary>
        public static void NormalizeProgress()
        {
            if (Db == null || Db.worlds.Count == 0) return;
            int last = 0;
            for (int w = 0; w < Db.worlds.Count; w++) last += Db.worlds[w].levels.Count;
            int cap = GameDatabase.FirstLevelOf(Db.worlds.Count); // sonraki (henüz olmayan) dünyanın ilk seviyesi
            if (last < Db.worlds.Count * GameDatabase.LevelsPerWorld) cap = last + 1;
            if (Save.level > cap)
            {
                Save.level = cap;
                SaveSystem.Save();
            }
        }

        public static void Play(int level)
        {
            Endless = false;
            SelectedLevel = level;
            SceneManager.LoadScene(GameScene);   // Yükleniyor ekranı yalnızca oyun açılırken gösterilir
        }

        public static void PlayEndless()
        {
            Endless = true;
            SelectedLevel = 0;
            SceneManager.LoadScene(GameScene);
        }

        public static void GoToMenu()
        {
            Endless = false;
            SceneManager.LoadScene(MenuScene);
        }

        public static bool TryBuyUpgrade(UpgradeId id)
        {
            var cfg = Db != null ? Db.upgrades : null;
            var def = cfg != null ? cfg.Get(id) : null;
            if (def == null) return false;
            int lvl = Save.GetUpgrade(id);
            if (lvl >= def.maxLevel) return false;
            int cost = def.Cost(lvl);
            if (Save.gold < cost) return false;
            Save.gold -= cost;
            Save.SetUpgrade(id, lvl + 1);
            DailyQuests.Add(QuestType.Upgrade, 1);
            SaveSystem.Save();
            return true;
        }
    }
}
