using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Oyunun tüm içerik listesi: dünyalar (her biri 10 seviye), yükseltme ayarları ve
    /// henüz yapılmamış dünyaların adları. Resources/GameDatabase.asset olarak iki sahneden de okunur.
    /// Seviye numaraları geneldir: 1-10 = 1. dünya, 11-20 = 2. dünya ...
    /// </summary>
    [CreateAssetMenu(menuName = "StratejiGame/Oyun Veritabanı", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public const int LevelsPerWorld = 10;

        public List<WorldData> worlds = new List<WorldData>();
        public UpgradeConfig upgrades;
        [Tooltip("Kodla üretilen karakter modelleri (asker, kahraman) için köşe renkli toon materyal")]
        public Material characterMaterial;
        [Tooltip("Parlayan efektler (namlu alevi, mermi izi) için eklemeli materyal")]
        public Material fxMaterial;
        [Tooltip("Ortam (yol, kum, sütun, mercan) için köşe renkli toon materyal — güçlü kostik + bitki sallanması")]
        public Material envMaterial;
        [Tooltip("Akan su (şelale, buff bloklarından dökülen su)")]
        public Material waterMaterial;
        [Tooltip("Uzak arka plan: açık deniz degrade + ışıltı (SeaBackdrop)")]
        public Material backdropMaterial;
        [Header("Yanardağ teması")]
        public Material volcanoEnvMaterial;
        public Material lavaMaterial;
        public Material volcanoSkyMaterial;

        [Header("Bataklık teması")]
        public Material swampEnvMaterial;
        public Material swampGlowMaterial;
        public Material swampWaterMaterial;
        public Material swampSkyMaterial;

        [Header("Kar teması")]
        public Material frostEnvMaterial;
        public Material frostWaterMaterial;
        public Material frostSkyMaterial;

        [Header("Gökyüzü teması")]
        public Material skyEnvMaterial;
        public Material skyWaterMaterial;
        public Material skySkyMaterial;

        [Header("Çöl teması")]
        public Material desertEnvMaterial;
        public Material desertSandMaterial;
        public Material desertSkyMaterial;




        [Tooltip("Henüz yapılmamış dünyalar (seviye haritasında kilitli ve 'Yakında' olarak görünür)")]
        public List<string> upcomingWorlds = new List<string>
        {
            "Volkanik Dağ Etekleri", "Çürümüş Orman", "Dondurucu Zirveler", "Gökyüzü Tapınağı", "Çöl Sıcaklığı",
        };

        public int WorldSlotCount => worlds.Count + upcomingWorlds.Count;

        public static int WorldIndexOf(int level) => (level - 1) / LevelsPerWorld;
        public static int IndexInWorld(int level) => (level - 1) % LevelsPerWorld;
        public static int FirstLevelOf(int worldIndex) => worldIndex * LevelsPerWorld + 1;

        public WorldData GetWorld(int level)
        {
            int w = WorldIndexOf(level);
            return level >= 1 && w < worlds.Count ? worlds[w] : null;
        }

        public LevelData GetLevel(int level)
        {
            var w = GetWorld(level);
            int i = IndexInWorld(level);
            return w != null && i < w.levels.Count ? w.levels[i] : null;
        }

        public bool HasLevel(int level) => GetLevel(level) != null;
        public bool IsWorldAvailable(int worldIndex) => worldIndex >= 0 && worldIndex < worlds.Count;

        public string WorldName(int worldIndex)
        {
            if (worldIndex < worlds.Count) return Loc.Name(worlds[worldIndex].displayName);
            int u = worldIndex - worlds.Count;
            return u < upcomingWorlds.Count ? Loc.Name(upcomingWorlds[u]) : "?";
        }

        static GameDatabase cached;

        public static GameDatabase Load()
        {
            if (cached == null) cached = Resources.Load<GameDatabase>("GameDatabase");
            if (cached == null) Debug.LogError("Resources/GameDatabase.asset bulunamadı. StratejiGame → Veri Dosyalarını Oluştur komutunu çalıştırın.");
            return cached;
        }
    }
}
