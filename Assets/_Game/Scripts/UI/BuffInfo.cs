using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Güçlendirme kapıları ve kahraman yeteneklerinin sembolü, adı, rengi ve açıklaması.
    /// Kapılardaki sembol, ilk görüşteki açıklama kartı ve ana menüdeki REHBER aynı bilgiyi kullanır.
    /// </summary>
    public static class BuffInfo
    {
        public struct Entry
        {
            public string key, title, desc;
            public System.Func<Sprite> icon;
            public Color color;
        }

        public static readonly Color Gold = new Color(0.95f, 0.6f, 0.1f);
        public static readonly Color Blue = new Color(0.15f, 0.45f, 0.95f);
        public static readonly Color Red = new Color(0.9f, 0.2f, 0.2f);

        /// <summary>Kapıda sembolle gösterilen tür mü (asker kapıları sayıyla kalır).</summary>
        public static bool HasSymbol(GateType t) => t != GateType.Soldiers && t != GateType.Multiply;

        public static Sprite Icon(GateType t) => t switch
        {
            GateType.FireRate => UiIcons.Bolt,
            GateType.Damage => UiIcons.Sword,
            GateType.ExtraBullet => UiIcons.Bullet,
            GateType.Pierce => UiIcons.PierceIcon,
            GateType.Crit => UiIcons.Crosshair,
            _ => UiIcons.Troops,
        };

        /// <summary>Kapıdaki sayı (sembolün yanında).</summary>
        public static string Value(GateDef d)
        {
            string signed = d.value > 0 ? "+" + d.value : d.value.ToString();
            return d.type switch
            {
                GateType.FireRate or GateType.Damage or GateType.Crit => signed + "%",
                GateType.ExtraBullet or GateType.Pierce => "+1",
                _ => signed,
            };
        }

        public static Entry For(GateType t) => t switch
        {
            GateType.FireRate => new Entry { key = "FireRate", title = Loc.T("ATIŞ HIZI", "FIRE RATE"), icon = () => UiIcons.Bolt, color = Gold,
                desc = Loc.T("Kahraman ve askerler daha hızlı ateş eder.", "The hero and soldiers shoot faster.") },
            GateType.Damage => new Entry { key = "Damage", title = Loc.T("HASAR", "DAMAGE"), icon = () => UiIcons.Sword, color = Gold,
                desc = Loc.T("Tüm mermilerin verdiği hasar artar.", "All bullets deal more damage.") },
            GateType.ExtraBullet => new Entry { key = "ExtraBullet", title = Loc.T("+1 MERMİ", "+1 BULLET"), icon = () => UiIcons.Bullet, color = Gold,
                desc = Loc.T("Kahraman her atışta 1 mermi fazla atar (en fazla +2).", "The hero fires 1 extra bullet per shot (max +2).") },
            GateType.Pierce => new Entry { key = "Pierce", title = Loc.T("DELİCİ MERMİ", "PIERCING BULLET"), icon = () => UiIcons.PierceIcon, color = Gold,
                desc = Loc.T("Mermi düşmanı delip arkasındakine de vurur (en fazla 2).", "Bullets pass through and hit the enemy behind (max 2).") },
            GateType.Crit => new Entry { key = "Crit", title = Loc.T("KRİTİK VURUŞ", "CRITICAL HIT"), icon = () => UiIcons.Crosshair, color = Gold,
                desc = Loc.T("Mermilerin bir kısmı çift hasar verir (en fazla %30).", "Some bullets deal double damage (max 30%).") },
            _ => new Entry { key = "Soldiers", title = Loc.T("ASKER KAPISI", "SOLDIER GATE"), icon = () => UiIcons.Troops, color = Blue,
                desc = Loc.T("Mavi kapılar ordunu büyütür: + asker ekler, x asker sayını katlar.", "Blue gates grow your army: + adds soldiers, x multiplies them.") },
        };

        public static Entry Negative => new Entry
        {
            key = "Negative", title = Loc.T("KIRMIZI KAPI", "RED GATE"), icon = () => UiIcons.Skull, color = Red,
            desc = Loc.T("Eksi değerli kapı: asker ya da güç kaybettirir. Uzak dur!", "Negative gate: you lose soldiers or power. Stay away!"),
        };

        // --- Sonsuz Savunma'ya özel kapılar ---
        public static Entry WeakGate => new Entry
        {
            key = "WeakGate", title = Loc.T("ZAYIFLATAN KAPI", "WEAKENING GATE"), icon = () => UiIcons.Bolt, color = Red,
            desc = Loc.T("Ateş hızını ya da hasarı %10-15 düşürür. 2. dalgadan itibaren çıkar.", "Lowers fire rate or damage by 10-15%. Appears from wave 2."),
        };

        public static Entry TrapRow => new Entry
        {
            key = "TrapRow", title = Loc.T("TUZAK SIRA", "TRAP ROW"), icon = () => UiIcons.Skull, color = Red,
            desc = Loc.T("İki kapı da kırmızı! Kurtuluş boş şeritte. 3. dalgadan itibaren çıkar.", "Both gates are red! The empty lane is your way out. From wave 3."),
        };

        public static Entry Armored => new Entry
        {
            key = "Armored", title = Loc.T("ZIRHLI DÜŞMAN", "ARMORED ENEMY"), icon = () => UiIcons.Diamond, color = new Color(0.55f, 0.62f, 0.75f),
            desc = Loc.T("Çelik kalkanlı düşman: kalkan kırılana kadar yarım hasar alır. 4. dalgadan itibaren çıkar.", "Steel-shielded enemy: takes half damage until the shield breaks. From wave 4."),
        };

        public static Entry[] Skills => new[]
        {
            new Entry { key = "Aoe", title = Loc.T("ALAN HASARI", "AREA BLAST"), icon = () => UiIcons.Blast, color = new Color(0.95f, 0.5f, 0.12f),
                desc = Loc.T("Önündeki alanda büyük patlama, kalabalığı temizler. 12 sn bekleme.", "A big blast ahead of you that clears crowds. 12 s cooldown.") },
            new Entry { key = "Heal", title = Loc.T("CAN YENİLE", "HEAL"), icon = () => UiIcons.Potion, color = new Color(0.25f, 0.78f, 0.35f),
                desc = Loc.T("Canının %25'ini geri verir. Bölüm başına sınırlı hak.", "Restores 25% of your health. Limited uses per level.") },
            new Entry { key = "Rage", title = Loc.T("ÖFKE", "RAGE"), icon = () => UiIcons.Flame, color = new Color(0.9f, 0.2f, 0.18f),
                desc = Loc.T("5 sn boyunca ateş hızı ve hasar x1,5. 25 sn bekleme.", "Fire rate and damage x1.5 for 5 s. 25 s cooldown.") },
        };

        // --- ilk görüş: her tür bir kez anlatılır ---
        const string Prefix = "buff_seen_";
        public static bool Seen(string key) => PlayerPrefs.GetInt(Prefix + key, 0) == 1;
        public static void MarkSeen(string key) { PlayerPrefs.SetInt(Prefix + key, 1); PlayerPrefs.Save(); }

        /// <summary>İlerleme sıfırlanınca açıklamalar yeniden gösterilsin.</summary>
        public static void ResetSeen()
        {
            foreach (GateType t in System.Enum.GetValues(typeof(GateType))) PlayerPrefs.DeleteKey(Prefix + For(t).key);
            PlayerPrefs.DeleteKey(Prefix + Negative.key);
            PlayerPrefs.DeleteKey(Prefix + WeakGate.key);
            PlayerPrefs.Save();
        }
    }
}
