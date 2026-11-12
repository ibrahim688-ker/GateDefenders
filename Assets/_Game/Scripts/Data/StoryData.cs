namespace StratejiGame
{
    /// <summary>
    /// Hikaye metinleri (Komutan Atilla) ve hangilerinin görüldüğü.
    /// Görülenler kayıtta (SaveData.storySeen bit maskesi) tutulur: bit 0 giriş + eğitim, bit 1..6 dünya sonu konuşmaları.
    /// İlerleme sıfırlanınca hikaye de baştan oynar.
    /// </summary>
    public static class StoryData
    {
        public const int WorldCount = 6;

        public static string CommanderName => Loc.T("KOMUTAN ATİLLA", "COMMANDER ATILLA");

        // ------------------------------------------------------------------ görüldü bilgisi
        public static bool Seen(int bit) => (SaveSystem.Data.storySeen & (1 << bit)) != 0;

        public static void MarkSeen(int bit)
        {
            SaveSystem.Data.storySeen |= 1 << bit;
            SaveSystem.Save();
        }

        public const int IntroBit = 0;
        public static int WorldBit(int worldIndex) => worldIndex + 1;

        /// <summary>Dünya tamamen temizlendi mi (boss dahil 10 seviye geçildi).</summary>
        public static bool WorldCleared(int worldIndex) => SaveSystem.Data.level > (worldIndex + 1) * GameDatabase.LevelsPerWorld;

        // ------------------------------------------------------------------ giriş
        public static string[] Intro => Loc.EN
            ? new[]
            {
                "Soldier! Glad you made it. I'm Commander Atilla. Let me brief you right away.",
                "The Shadow Invasion has crossed our borders. The shadow army is seizing our lands one by one.",
                "The ancient ruins under the sea, the volcanic foothills, the forests, the frozen peaks, the sky temple and the deserts… all under shadow control.",
                "Each land is ruled by a terrible leader. Until that leader falls, those lands will never be free.",
                "I'm giving this cleanup mission to you. Gather your army, choose your gates wisely and take back our lands!",
                "First, let me show you how to fight.",
            }
            : new[]
            {
                "Asker! İyi ki geldin. Ben Komutan Atilla. Vakit kaybetmeden durumu anlatayım.",
                "Gölge İstilası sınırlarımızı aştı. Gölge ordusu diyarlarımızı tek tek ele geçiriyor.",
                "Su altındaki eski kalıntılar, yanardağ etekleri, ormanlar, karlı zirveler, gökyüzü tapınağı ve çöller… Hepsi gölgelerin kontrolünde.",
                "Her diyarın başında korkunç bir lider var. Onu yenmeden o topraklar kurtulmaz.",
                "Bu temizlik görevini sana veriyorum. Ordunu topla, kapıları akıllıca seç ve diyarlarımızı geri al!",
                "Önce sana nasıl savaşacağını göstereyim.",
            };

        // ------------------------------------------------------------------ eğitim sayfaları
        public struct Page { public string title, text; }

        public static Page[] Tutorial => Loc.EN
            ? new[]
            {
                new Page { title = "MOVE", text = "Swipe left and right to change lanes. Your hero fires on his own, you just get to the right spot." },
                new Page { title = "BLUE GATES", text = "Blue gates grow your army. +5 adds soldiers, x2 multiplies them. Your soldiers fight alongside you." },
                new Page { title = "POWER GATES", text = "Gold gates power up your weapon: fire rate, damage, extra bullet, piercing bullet, critical hit. Symbols are explained in the GUIDE on the main menu." },
                new Page { title = "RED GATES", text = "Red gates are negative, they cost you soldiers or power. Stay away! Shoot some gates to raise their value." },
                new Page { title = "SKILLS", text = "Use your skills when things get tough: Area Blast clears crowds, Heal restores you, Rage boosts your power for a short time." },
                new Page { title = "GROW STRONGER", text = "Spend the gold you earn on upgrades. A boss awaits on the 10th level of every land. Good luck, soldier!" },
            }
            : new[]
            {
                new Page { title = "HAREKET", text = "Parmağını sağa sola kaydırarak şerit değiştir. Kahramanın kendiliğinden ateş eder, sen sadece doğru yere git." },
                new Page { title = "MAVİ KAPILAR", text = "Mavi kapılar ordunu büyütür. +5 asker ekler, x2 asker sayını katlar. Askerlerin de seninle birlikte savaşır." },
                new Page { title = "GÜÇ KAPILARI", text = "Altın kapılar silahını güçlendirir: ateş hızı, hasar, ekstra mermi, delici mermi, kritik vuruş. Sembollerin anlamı ana menüdeki REHBER'de." },
                new Page { title = "KIRMIZI KAPILAR", text = "Kırmızı kapılar eksi değerlidir, asker ya da güç kaybettirir. Uzak dur! Bazı kapılara ateş ederek değerlerini artırabilirsin." },
                new Page { title = "YETENEKLER", text = "Zor anlarda yeteneklerini kullan: Alan Hasarı kalabalığı temizler, Can Yenile seni iyileştirir, Öfke kısa süre gücünü artırır." },
                new Page { title = "GELİŞİM", text = "Kazandığın altınla yükseltme al. Her diyarın 10. seviyesinde boss seni bekliyor. Bol şans asker!" },
            };

        // ------------------------------------------------------------------ dünya sonları
        /// <summary>Terfi edilen rütbe (Profil'deki rütbe listesiyle aynı).</summary>
        public static string Rank(int worldIndex)
        {
            string[] tr = { "ONBAŞI", "ÇAVUŞ", "TEĞMEN", "YÜZBAŞI", "BİNBAŞI", "ALBAY" };
            string[] en = { "CORPORAL", "SERGEANT", "LIEUTENANT", "CAPTAIN", "MAJOR", "COLONEL" };
            int i = UnityEngine.Mathf.Clamp(worldIndex, 0, tr.Length - 1);
            return Loc.EN ? en[i] : tr[i];
        }

        public static string[] WorldEnd(int worldIndex)
        {
            bool en = Loc.EN;
            switch (worldIndex)
            {
                case 0: return en
                    ? new[] { "You buried the Abyssal Crab King at the bottom of the sea! Outstanding work.",
                              "For this achievement, I'm promoting you to Corporal.",
                              "But there's no time to rest. The shadow army has set the Volcanic Foothills on fire. Watch out for the lava!" }
                    : new[] { "Abisal Yengeç Kral'ı denizin dibine gömdün! Harika iş çıkardın.",
                              "Bu başarının karşılığında seni Onbaşı rütbesine terfi ettiriyorum.",
                              "Ama dinlenme vakti değil. Gölge ordusu Volkanik Dağ Etekleri'ni ateşe verdi. Lavlara dikkat et!" };
                case 1: return en
                    ? new[] { "The Magma Golem is nothing but a pile of cooled rock now. Congratulations, Sergeant!",
                              "Every victory gives our army hope. Your soldiers are talking about you.",
                              "Next stop: the Rotten Forest. It's dark and treacherous there, don't even trust the trees." }
                    : new[] { "Magma Golemi artık sadece soğumuş bir taş yığını. Tebrikler, Çavuş!",
                              "Her zafer ordumuza umut veriyor. Askerlerin senin adını konuşuyor.",
                              "Sıradaki durak Çürümüş Orman. Orası karanlık ve sinsi, ağaçlara bile güvenme." };
                case 2: return en
                    ? new[] { "The Root King's roots have withered! The forest breathes again.",
                              "You're a Lieutenant now. Lead the soldiers who trust you.",
                              "Now on to the Frozen Peaks. The cold is merciless, but you're tougher." }
                    : new[] { "Kök Kralı'nın kökleri kurudu! Orman yeniden nefes alıyor.",
                              "Artık bir Teğmensin. Sana güvenen askerlerine liderlik et.",
                              "Şimdi Dondurucu Zirveler'e. Soğuk acımasız ama sen ondan da sertsin." };
                case 3: return en
                    ? new[] { "You buried the Armored Yeti Warchief in his own snow! Incredible.",
                              "You've more than earned the rank of Captain.",
                              "The shadows have retreated to the skies. Take back the Sky Temple, and beware of the lightning!" }
                    : new[] { "Zırhlı Yeti Savaş Şefi'ni kendi karında gömdün! İnanılmaz.",
                              "Yüzbaşı rütbesini sonuna kadar hak ettin.",
                              "Gölgeler artık gökyüzüne çekildi. Gökyüzü Tapınağı'nı geri al, yıldırımlardan sakın!" };
                case 4: return en
                    ? new[] { "The Storm Lord has fallen, the sky is ours again!",
                              "Major, one land remains. The last stronghold of the Shadow Invasion: Desert Heat.",
                              "The Lord of the Sands is the strongest enemy of this war. Go with everything you've got!" }
                    : new[] { "Fırtına Lordu düştü, gökyüzü yeniden bizim!",
                              "Binbaşı, son bir diyar kaldı. Gölge İstilası'nın son kalesi: Çöl Sıcaklığı.",
                              "Kumların Hakimi bu savaşın en güçlü düşmanı. Tüm gücünle git!" };
                default: return en
                    ? new[] { "You did it! The Lord of the Sands is defeated and the Shadow Invasion has been wiped from our lands!",
                              "Today the whole nation is shouting your name. I'm promoting you to Colonel. This victory is yours!",
                              "Every land you freed is free for good. We will never forget it.",
                              "But the scouts brought bad news… Shadows are gathering again in distant lands.",
                              "Rest, soldier. Very soon I'll call on you again for new lands and new enemies!" }
                    : new[] { "Başardın! Kumların Hakimi yenildi ve Gölge İstilası diyarlarımızdan tamamen silindi!",
                              "Bugün tüm halk senin adını haykırıyor. Seni Albay rütbesine terfi ettiriyorum. Bu zafer senin!",
                              "Kurtardığın her diyar artık özgür. Bunu asla unutmayacağız.",
                              "Ama izciler kötü haberler getirdi… Uzak diyarlarda gölgeler yeniden toplanıyor.",
                              "Dinlen asker. Çok yakında yeni diyarlar ve yeni düşmanlar için seni tekrar çağıracağım!" };
            }
        }

        /// <summary>Zafer ekranındaki satır: "X gölgelerden temizlendi! Sırada: Y".</summary>
        public static string ClearedLine(int worldIndex, GameDatabase db)
        {
            string cur = db != null ? db.WorldName(worldIndex) : "";
            bool last = worldIndex >= WorldCount - 1 || db == null || !db.IsWorldAvailable(worldIndex + 1);
            if (last) return Loc.T("TÜM DİYARLAR GÖLGELERDEN TEMİZLENDİ!", "ALL LANDS ARE CLEARED OF SHADOWS!");
            string next = db.WorldName(worldIndex + 1);
            return Loc.EN
                ? Loc.Upper(cur) + " IS CLEARED OF SHADOWS!\n<size=26>Next: " + next + "</size>"
                : Loc.Upper(cur) + " GÖLGELERDEN TEMİZLENDİ!\n<size=26>Sırada: " + next + "</size>";
        }

        /// <summary>Arşivdeki başlık.</summary>
        public static string ArchiveTitle(int worldIndex)
        {
            if (worldIndex >= WorldCount - 1) return Loc.T("Final: Zafer", "Finale: Victory");
            var db = GameSession.Db;
            return (worldIndex + 1) + ". " + (db != null ? db.WorldName(worldIndex) : "") + Loc.T(" temizlendi", " cleared");
        }
    }
}
