using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>Dünyanın görsel/işitsel teması (ortam, efekt, ses ve mermi görünümü buna göre seçilir).</summary>
    public enum WorldTheme { Underwater = 0, Volcano = 1, Swamp = 2, Frost = 3, Sky = 4, Desert = 5 }

    /// <summary>Bir tematik dünya: adı, 10 seviyesi ve boss adı.</summary>
    [CreateAssetMenu(menuName = "StratejiGame/Dünya", fileName = "World")]
    public class WorldData : ScriptableObject
    {
        public string displayName = "Su Altı Kalıntıları";
        public string bossName = "Abisal Yengeç Kral";
        public WorldTheme theme = WorldTheme.Underwater;
        public List<LevelData> levels = new List<LevelData>();

        [Header("3D modeller (boşsa basit şekiller kullanılır)")]
        [Tooltip("Boss modeli. Pivot ayak hizasında, yüzü -Z yönüne (oyuncuya) bakmalı.")]
        public GameObject bossPrefab;
        [Tooltip("Boss modelinin oyundaki ölçeği")]
        public float bossModelScale = 2.2f;

        [Tooltip("Düşman türlerinin 3D modelleri (listede olmayan türler basit şekillerle çizilir). Model yüzü +Z yönüne bakmalı.")]
        public List<EnemyModel> enemyModels = new List<EnemyModel>();

        public EnemyModel GetEnemyModel(EnemyKind kind) => enemyModels.Find(m => m != null && m.kind == kind && m.prefab != null);
    }

    [System.Serializable]
    public class EnemyModel
    {
        public EnemyKind kind;
        public GameObject prefab;
        [Tooltip("Oyundaki ölçek (model boyu 1 birim olarak hazırlanır)")]
        public float scale = 1f;
    }
}
