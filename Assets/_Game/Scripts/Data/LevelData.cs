using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    [System.Serializable]
    public class WaveEntry
    {
        [Tooltip("Seviye başından itibaren saniye")]
        public float time;
        public EnemyKind kind = EnemyKind.Minion;
        public int count = 8;
        [Tooltip("Düşmanların kaç saniyeye yayılarak çıkacağı")]
        public float duration = 3f;
    }

    [System.Serializable]
    public class GateRowEntry
    {
        public float time;
        [Tooltip("İlk kapı (şeridi rastgele seçilir)")]
        public GateDef first = GateDef.Soldiers(1);
        [Tooltip("İkinci kapı. Üçüncü şerit her zaman boş kalır.")]
        public GateDef second = GateDef.Soldiers(2);
    }

    /// <summary>Tek bir seviyenin tüm içeriği: düşman dalgaları, kapı sıraları, boss.</summary>
    [CreateAssetMenu(menuName = "StratejiGame/Seviye", fileName = "Level")]
    public class LevelData : ScriptableObject
    {
        [Tooltip("Düşman canı çarpanı")]
        public float enemyHpMultiplier = 1f;
        [Tooltip("Seviye tamamlanınca verilen ek altın")]
        public int completionReward = 30;

        public List<WaveEntry> waves = new List<WaveEntry>();
        public List<GateRowEntry> gateRows = new List<GateRowEntry>();

        [Header("Boss")]
        public bool hasBoss;
        public float bossTime = 40f;
        public float bossHpMultiplier = 1f;

        /// <summary>Son olayın zamanı (boss yoksa seviye bu zamandan sonra, düşmanlar bitince kazanılır).</summary>
        public float EndTime
        {
            get
            {
                float t = hasBoss ? bossTime : 0f;
                foreach (var w in waves) t = Mathf.Max(t, w.time + w.duration);
                foreach (var g in gateRows) t = Mathf.Max(t, g.time);
                return t;
            }
        }
    }
}
