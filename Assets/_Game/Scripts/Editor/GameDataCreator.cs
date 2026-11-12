using System.IO;
using UnityEditor;
using UnityEngine;

namespace StratejiGame.EditorTools
{
    /// <summary>
    /// Menü: StratejiGame → Veri Dosyalarını Oluştur.
    /// Su Altı dünyası için 10 seviyelik başlangıç verisini ve yükseltme ayarlarını üretir.
    /// Üretilen dosyalar daha sonra Inspector'dan elle düzenlenebilir.
    /// </summary>
    public static class GameDataCreator
    {
        const string DataRoot = "Assets/_Game/Data";
        const string LevelFolder = DataRoot + "/Levels/World1_SuAlti";

        [MenuItem("StratejiGame/Veri Dosyalarını Oluştur (Su Altı)")]
        public static void CreateAll()
        {
            Directory.CreateDirectory(LevelFolder);
            AssetDatabase.Refresh();

            var upgrades = LoadOrCreate<UpgradeConfig>(DataRoot + "/UpgradeConfig.asset");
            upgrades.ResetToDefaults();
            EditorUtility.SetDirty(upgrades);

            var world = LoadOrCreate<WorldData>(DataRoot + "/World1_SuAlti.asset");
            world.displayName = "Su Altı Kalıntıları";
            world.bossName = "Abisal Yengeç Kral";
            world.levels.Clear();
            for (int i = 1; i <= 10; i++)
            {
                var lvl = LoadOrCreate<LevelData>($"{LevelFolder}/Level_{i:00}.asset");
                Generate(lvl, i);
                EditorUtility.SetDirty(lvl);
                world.levels.Add(lvl);
            }
            EditorUtility.SetDirty(world);
            AssetDatabase.SaveAssets();

            // Her iki sahnenin de okuduğu veritabanı (Resources/GameDatabase.asset)
            Directory.CreateDirectory("Assets/_Game/Resources");
            AssetDatabase.Refresh();
            var db = LoadOrCreate<GameDatabase>("Assets/_Game/Resources/GameDatabase.asset");
            db.upgrades = upgrades;
            if (!db.worlds.Contains(world)) db.worlds.Insert(0, world);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("StratejiGame: Su Altı dünyası (10 seviye) ve yükseltme ayarları oluşturuldu.");
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        /// <summary>Seviye numarasına göre artan zorlukta dalga ve kapı dizisi üretir.
        /// n = genel seviye numarası; k = dünya içindeki sıra (1-10); w = dünya (0 = Su Altı, 1 = Yanardağ...).
        /// Dünya 1 için sonuçlar eski üreticiyle birebir aynıdır.</summary>
        static void Generate(LevelData d, int n)
        {
            int w = (n - 1) / 10, k = (n - 1) % 10 + 1;
            var rnd = new System.Random(n * 97 + 13);
            d.waves.Clear();
            d.gateRows.Clear();
            d.enemyHpMultiplier = 1f + 0.08f * (n - 1);
            d.completionReward = 20 + 6 * n;
            d.hasBoss = k == 10;
            d.bossHpMultiplier = 1f + 0.6f * w;

            float length = k == 10 ? 26f + 4f * w : 30f + k * 3f + 4f * w; // saniye
            bool extraBulletUsed = false, pierceUsed = false;
            int step = 0;
            for (float t = 1f; t < length; t += 4f, step++)
            {
                // Temel minyon dalgası: seviye ve zamanla büyür
                int minions = 6 + Mathf.RoundToInt(k * 1.4f + w * 5f + t / 8f);
                d.waves.Add(new WaveEntry { time = t, kind = EnemyKind.Minion, count = minions, duration = 3.5f });

                // Not: Nişancı (Gunner) kullanıcı kararıyla kaldırıldı; menzilli rolü Zıpkıncı (Yanardağ'da Magma Berserker) üstleniyor.

                if ((k >= 3 || w > 0) && step % 3 == 2)
                    d.waves.Add(new WaveEntry { time = t + 1f, kind = EnemyKind.Brute, count = 1 + k / 4 + w, duration = 2f });
                // Menzilli düşmanlar: ilk seviyelerde seyrek, ilerledikçe sık ve kalabalık
                int harpEvery = (k <= 3 && w == 0) ? 3 : 2;
                if (step % harpEvery == 1)
                    d.waves.Add(new WaveEntry { time = t + 0.5f, kind = EnemyKind.Harpooner, count = 1 + (k - 1) / 4 + w, duration = 1.5f });

                // Kapı sırası (üçüncü şerit oyunda otomatik boş kalır)
                var a = SoldierGate(rnd, n);
                GateDef b;
                int roll = rnd.Next(100);
                if ((k >= 4 || w > 0) && !extraBulletUsed && roll < 12) { b = GateDef.ExtraBullet(); extraBulletUsed = true; }
                else if ((k >= 5 || w > 0) && !pierceUsed && roll < 22) { b = GateDef.Pierce(); pierceUsed = true; }
                else if ((k >= 3 || w > 0) && roll < 30) b = GateDef.Multiply(2);
                else if ((k >= 2 || w > 0) && roll < 42) b = GateDef.Soldiers(-(2 + rnd.Next(2 + w)), true);
                else if (roll < 58) b = GateDef.FireRate(15 + 5 * rnd.Next(3));
                else if (roll < 70) b = GateDef.Damage(20 + 5 * rnd.Next(2));
                else if (roll < 78) b = GateDef.Crit(10);
                else b = SoldierGate(rnd, n);
                d.gateRows.Add(new GateRowEntry { time = t + 1f, first = a, second = b });
            }
            d.bossTime = length + 2f;
        }

        const string Level2Folder = DataRoot + "/Levels/World2_Yanardag";

        /// <summary>Yanardağ dünyası (seviye 11-20). Yalnızca bu dünyanın verisini üretir; Su Altı'na dokunmaz.</summary>
        [MenuItem("StratejiGame/Veri Dosyalarını Oluştur (Yanardağ)")]
        public static void CreateVolcano()
        {
            Directory.CreateDirectory(Level2Folder);
            AssetDatabase.Refresh();
            var world = LoadOrCreate<WorldData>(DataRoot + "/World2_Yanardag.asset");
            world.displayName = "Volkanik Dağ Etekleri";
            world.bossName = "Magma Golemi";
            world.theme = WorldTheme.Volcano;
            world.levels.Clear();
            for (int i = 11; i <= 20; i++)
            {
                var lvl = LoadOrCreate<LevelData>($"{Level2Folder}/Level_{i:00}.asset");
                Generate(lvl, i);
                EditorUtility.SetDirty(lvl);
                world.levels.Add(lvl);
            }
            EditorUtility.SetDirty(world);
            var db = LoadOrCreate<GameDatabase>("Assets/_Game/Resources/GameDatabase.asset");
            if (!db.worlds.Contains(world)) db.worlds.Insert(Mathf.Min(1, db.worlds.Count), world);
            db.upcomingWorlds.Remove(world.displayName);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("StratejiGame: Yanardağ dünyası (seviye 11-20) oluşturuldu.");
        }

        const string Level3Folder = DataRoot + "/Levels/World3_Bataklik";

        /// <summary>Bataklık dünyası (seviye 21-30): Çürümüş Orman, boss Kök Kralı. Diğer dünyalara dokunmaz.</summary>
        [MenuItem("StratejiGame/Veri Dosyalarını Oluştur (Bataklık)")]
        public static void CreateSwamp()
        {
            Directory.CreateDirectory(Level3Folder);
            AssetDatabase.Refresh();
            var world = LoadOrCreate<WorldData>(DataRoot + "/World3_Bataklik.asset");
            world.displayName = "Çürümüş Orman";
            world.bossName = "Kök Kralı";
            world.theme = WorldTheme.Swamp;
            world.levels.Clear();
            for (int i = 21; i <= 30; i++)
            {
                var lvl = LoadOrCreate<LevelData>($"{Level3Folder}/Level_{i:00}.asset");
                Generate(lvl, i);
                EditorUtility.SetDirty(lvl);
                world.levels.Add(lvl);
            }
            EditorUtility.SetDirty(world);
            var db = LoadOrCreate<GameDatabase>("Assets/_Game/Resources/GameDatabase.asset");
            if (!db.worlds.Contains(world)) db.worlds.Insert(Mathf.Min(2, db.worlds.Count), world);
            db.upcomingWorlds.Remove(world.displayName);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("StratejiGame: Bataklık dünyası (seviye 21-30) oluşturuldu.");
        }

        const string Level4Folder = DataRoot + "/Levels/World4_Kar";

        /// <summary>Kar dünyası (seviye 31-40): Dondurucu Zirveler, boss Zırhlı Yeti Savaş Şefi. Diğer dünyalara dokunmaz.</summary>
        [MenuItem("StratejiGame/Veri Dosyalarını Oluştur (Kar)")]
        public static void CreateFrost()
        {
            Directory.CreateDirectory(Level4Folder);
            AssetDatabase.Refresh();
            var world = LoadOrCreate<WorldData>(DataRoot + "/World4_Kar.asset");
            world.displayName = "Dondurucu Zirveler";
            world.bossName = "Zırhlı Yeti Savaş Şefi";
            world.theme = WorldTheme.Frost;
            world.levels.Clear();
            for (int i = 31; i <= 40; i++)
            {
                var lvl = LoadOrCreate<LevelData>($"{Level4Folder}/Level_{i:00}.asset");
                Generate(lvl, i);
                EditorUtility.SetDirty(lvl);
                world.levels.Add(lvl);
            }
            EditorUtility.SetDirty(world);
            var db = LoadOrCreate<GameDatabase>("Assets/_Game/Resources/GameDatabase.asset");
            if (!db.worlds.Contains(world)) db.worlds.Insert(Mathf.Min(3, db.worlds.Count), world);
            db.upcomingWorlds.Remove(world.displayName);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("StratejiGame: Kar dünyası (seviye 31-40) oluşturuldu.");
        }

        const string Level5Folder = DataRoot + "/Levels/World5_Gokyuzu";

        /// <summary>Gökyüzü dünyası (seviye 41-50): Gökyüzü Tapınağı, boss Fırtına Lordu. Diğer dünyalara dokunmaz.</summary>
        [MenuItem("StratejiGame/Veri Dosyalarını Oluştur (Gökyüzü)")]
        public static void CreateSky()
        {
            Directory.CreateDirectory(Level5Folder);
            AssetDatabase.Refresh();
            var world = LoadOrCreate<WorldData>(DataRoot + "/World5_Gokyuzu.asset");
            world.displayName = "Gökyüzü Tapınağı";
            world.bossName = "Fırtına Lordu";
            world.theme = WorldTheme.Sky;
            world.levels.Clear();
            for (int i = 41; i <= 50; i++)
            {
                var lvl = LoadOrCreate<LevelData>($"{Level5Folder}/Level_{i:00}.asset");
                Generate(lvl, i);
                EditorUtility.SetDirty(lvl);
                world.levels.Add(lvl);
            }
            EditorUtility.SetDirty(world);
            var db = LoadOrCreate<GameDatabase>("Assets/_Game/Resources/GameDatabase.asset");
            if (!db.worlds.Contains(world)) db.worlds.Insert(Mathf.Min(4, db.worlds.Count), world);
            db.upcomingWorlds.Remove(world.displayName);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("StratejiGame: Gökyüzü dünyası (seviye 41-50) oluşturuldu.");
        }

        const string Level6Folder = DataRoot + "/Levels/World6_Col";

        /// <summary>Çöl dünyası (seviye 51-60): Çöl Sıcaklığı, boss Kumların Hakimi. Diğer dünyalara dokunmaz.</summary>
        [MenuItem("StratejiGame/Veri Dosyalarını Oluştur (Çöl)")]
        public static void CreateDesert()
        {
            Directory.CreateDirectory(Level6Folder);
            AssetDatabase.Refresh();
            var world = LoadOrCreate<WorldData>(DataRoot + "/World6_Col.asset");
            world.displayName = "Çöl Sıcaklığı";
            world.bossName = "Kumların Hakimi";
            world.theme = WorldTheme.Desert;
            world.levels.Clear();
            for (int i = 51; i <= 60; i++)
            {
                var lvl = LoadOrCreate<LevelData>($"{Level6Folder}/Level_{i:00}.asset");
                Generate(lvl, i);
                EditorUtility.SetDirty(lvl);
                world.levels.Add(lvl);
            }
            EditorUtility.SetDirty(world);
            var db = LoadOrCreate<GameDatabase>("Assets/_Game/Resources/GameDatabase.asset");
            if (!db.worlds.Contains(world)) db.worlds.Insert(Mathf.Min(5, db.worlds.Count), world);
            db.upcomingWorlds.Remove(world.displayName);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("StratejiGame: Çöl dünyası (seviye 51-60) oluşturuldu.");
        }

        static GateDef SoldierGate(System.Random rnd, int n) => GateDef.Soldiers(1 + rnd.Next(2 + n / 3));
    }
}
