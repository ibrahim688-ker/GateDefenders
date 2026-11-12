using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    public enum UpgradeId { MaxHp, Attack, SoldierLevel, StartSoldiers, AreaDamage, Heal, Rage }

    [System.Serializable]
    public class UpgradeDef
    {
        public UpgradeId id;
        public string displayName;
        public int maxLevel = 20;
        public int baseCost = 50;
        [Tooltip("Her seviyede maliyet bu oranla katlanır")]
        public float costGrowth = 1.35f;

        public int Cost(int currentLevel) => Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, currentLevel));
    }

    /// <summary>Kalıcı yükseltmelerin listesi, maliyetleri ve seviye başına etkileri.</summary>
    [CreateAssetMenu(menuName = "StratejiGame/Yükseltme Ayarları", fileName = "UpgradeConfig")]
    public class UpgradeConfig : ScriptableObject
    {
        public List<UpgradeDef> upgrades = new List<UpgradeDef>();

        [Header("Ana karakter")]
        public float baseMaxHp = 100f;
        public float hpPerLevel = 20f;
        public float baseAttack = 10f;
        [Tooltip("Seviye başına saldırı artışı (0.15 = %15)")]
        public float attackPerLevel = 0.15f;

        [Header("Yardımcı asker")]
        public float baseSoldierDamage = 4f;
        public float soldierDamagePerLevel = 0.12f;
        [Tooltip("Asker evrim seviyeleri: Kıdemli, Elit, Kraliyet")]
        public int[] evolutionLevels = { 5, 10, 15 };
        [Tooltip("Kıdemli Piyade ve sonrası için ek hasar")]
        public float veteranDamageBonus = 0.25f;

        [Header("Alan Hasarı")]
        public float aoeBaseCooldown = 12f;
        public float aoeCooldownPerLevel = 0.5f;
        public float aoeMinCooldown = 6f;
        public float aoeBaseRadius = 5f;
        public float aoeRadiusPerLevel = 0.3f;
        public float aoeBaseMult = 9f;
        public float aoeMultPerLevel = 0.15f;

        [Header("Can Yenileme")]
        public int healBaseCharges = 1;
        [Tooltip("Kaç yükseltmede bir ek kullanım hakkı")]
        public int healLevelsPerCharge = 2;
        public float healBaseFraction = 0.25f;
        public float healFractionPerLevel = 0.05f;

        [Header("Öfke")]
        public float rageBaseDuration = 5f;
        public float rageDurationPerLevel = 0.3f;
        public float rageBaseCooldown = 25f;
        public float rageCooldownPerLevel = 1f;
        public float rageMinCooldown = 12f;

        public UpgradeDef Get(UpgradeId id) => upgrades.Find(u => u.id == id);

        // --- seviyeye göre değerler ---
        public float MaxHp(int l) => baseMaxHp + hpPerLevel * l;
        public float Attack(int l) => baseAttack * (1f + attackPerLevel * l);
        public int SoldierLevel(int l) => l + 1;
        public int EvolutionTier(int soldierLevel)
        {
            int tier = 0;
            for (int i = 0; i < evolutionLevels.Length; i++) if (soldierLevel >= evolutionLevels[i]) tier = i + 1;
            return tier;
        }
        public float SoldierDamage(int l)
        {
            float d = baseSoldierDamage * (1f + soldierDamagePerLevel * l);
            if (EvolutionTier(SoldierLevel(l)) >= 1) d *= 1f + veteranDamageBonus;
            return d;
        }
        public int StartSoldiers(int l) => l;
        public float AoeCooldown(int l) => Mathf.Max(aoeMinCooldown, aoeBaseCooldown - aoeCooldownPerLevel * l);
        public float AoeRadius(int l) => aoeBaseRadius + aoeRadiusPerLevel * l;
        public float AoeMult(int l) => aoeBaseMult * (1f + aoeMultPerLevel * l);
        public int HealCharges(int l) => healBaseCharges + l / Mathf.Max(1, healLevelsPerCharge);
        public float HealFraction(int l) => Mathf.Min(0.6f, healBaseFraction + healFractionPerLevel * l);
        public float RageDuration(int l) => rageBaseDuration + rageDurationPerLevel * l;
        public float RageCooldown(int l) => Mathf.Max(rageMinCooldown, rageBaseCooldown - rageCooldownPerLevel * l);

        public static readonly string[] TierNames = { "Çaylak Asker", "Kıdemli Piyade", "Elit Muhafız", "Kraliyet Şövalyesi" };

        /// <summary>Menüde gösterilecek "şimdiki → sonraki" açıklaması.</summary>
        public string Describe(UpgradeId id, int l)
        {
            switch (id)
            {
                case UpgradeId.MaxHp: return $"{Loc.T("Can", "Health")}: {MaxHp(l):0} → {MaxHp(l + 1):0}";
                case UpgradeId.Attack: return $"{Loc.T("Hasar", "Damage")}: {Attack(l):0.#} → {Attack(l + 1):0.#}";
                case UpgradeId.SoldierLevel:
                    {
                        int sl = SoldierLevel(l);
                        string tier = Loc.Name(TierNames[Mathf.Min(EvolutionTier(sl), TierNames.Length - 1)]);
                        return Loc.EN ? $"Lv {sl} {tier} · Damage {SoldierDamage(l):0.#} → {SoldierDamage(l + 1):0.#}"
                                      : $"Sv {sl} {tier} · Hasar {SoldierDamage(l):0.#} → {SoldierDamage(l + 1):0.#}";
                    }
                case UpgradeId.StartSoldiers: return $"{Loc.T("Başlangıç askeri", "Starting soldiers")}: {StartSoldiers(l)} → {StartSoldiers(l + 1)}";
                case UpgradeId.AreaDamage: return Loc.EN ? $"Cooldown {AoeCooldown(l):0.#} s · Radius {AoeRadius(l):0.#} → {AoeRadius(l + 1):0.#}"
                                                       : $"Bekleme {AoeCooldown(l):0.#} sn · Alan {AoeRadius(l):0.#} → {AoeRadius(l + 1):0.#}";
                case UpgradeId.Heal: return Loc.EN ? $"{HealCharges(l)} uses, {HealFraction(l) * 100:0}% → {HealCharges(l + 1)} uses, {HealFraction(l + 1) * 100:0}%"
                                                 : $"{HealCharges(l)} hak, %{HealFraction(l) * 100:0} → {HealCharges(l + 1)} hak, %{HealFraction(l + 1) * 100:0}";
                case UpgradeId.Rage: return Loc.EN ? $"Duration {RageDuration(l):0.#} s → {RageDuration(l + 1):0.#} s · Cooldown {RageCooldown(l + 1):0} s"
                                                 : $"Süre {RageDuration(l):0.#} sn → {RageDuration(l + 1):0.#} sn · Bekleme {RageCooldown(l + 1):0} sn";
            }
            return "";
        }

        /// <summary>Varsayılan yükseltme listesini doldurur.</summary>
        public void ResetToDefaults()
        {
            upgrades = new List<UpgradeDef>
            {
                new UpgradeDef { id = UpgradeId.MaxHp, displayName = "Maksimum Can", maxLevel = 30, baseCost = 60, costGrowth = 1.25f },
                new UpgradeDef { id = UpgradeId.Attack, displayName = "Vuruş Hasarı", maxLevel = 30, baseCost = 75, costGrowth = 1.27f },
                new UpgradeDef { id = UpgradeId.SoldierLevel, displayName = "Asker Seviyesi", maxLevel = 19, baseCost = 90, costGrowth = 1.28f },
                new UpgradeDef { id = UpgradeId.StartSoldiers, displayName = "Başlangıç Birliği", maxLevel = 5, baseCost = 200, costGrowth = 1.6f },
                new UpgradeDef { id = UpgradeId.AreaDamage, displayName = "Alan Hasarı", maxLevel = 12, baseCost = 120, costGrowth = 1.3f },
                new UpgradeDef { id = UpgradeId.Heal, displayName = "Can Yenileme", maxLevel = 6, baseCost = 150, costGrowth = 1.45f },
                new UpgradeDef { id = UpgradeId.Rage, displayName = "Öfke", maxLevel = 10, baseCost = 140, costGrowth = 1.32f },
            };
        }
    }
}
