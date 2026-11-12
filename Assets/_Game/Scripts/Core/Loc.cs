using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    public enum Lang { TR = 0, EN = 1 }

    /// <summary>
    /// Basit iki dilli metin desteği (Türkçe / İngilizce).
    /// İlk açılışta telefonun diline göre seçilir (Türkçe değilse İngilizce); Ayarlar'dan değiştirilir (PlayerPrefs "lang").
    /// Arayüz metinleri yerinde Loc.T("türkçe", "english") ile yazılır; veri dosyalarındaki adlar (dünya, boss, yükseltme) Loc.Name ile çevrilir.
    /// Dil değişince menü baştan kurulur.
    /// </summary>
    public static class Loc
    {
        const string PrefKey = "lang";
        static bool loaded;
        static Lang current;

        public static Lang Current
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    int v = PlayerPrefs.GetInt(PrefKey, -1);
                    current = v >= 0 ? (Lang)v : (Application.systemLanguage == SystemLanguage.Turkish ? Lang.TR : Lang.EN);
                }
                return current;
            }
        }

        public static bool EN => Current == Lang.EN;

        public static void Set(Lang lang)
        {
            loaded = true;
            current = lang;
            PlayerPrefs.SetInt(PrefKey, (int)lang);
            PlayerPrefs.Save();
        }

        /// <summary>Seçili dile göre metin.</summary>
        public static string T(string tr, string en) => EN ? en : tr;

        /// <summary>Büyük harf: Türkçede i → İ, ı → I doğru çevrilir (telefonun bölge ayarından bağımsız).</summary>
        public static string Upper(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (!EN) s = s.Replace('i', 'İ').Replace('ı', 'I');
            return s.ToUpperInvariant();
        }

        /// <summary>Veri dosyalarından gelen ad (dünya, boss, yükseltme, asker rütbesi). Tabloda yoksa olduğu gibi döner.</summary>
        public static string Name(string tr)
        {
            if (!EN || string.IsNullOrEmpty(tr)) return tr;
            return Names.TryGetValue(tr.Trim(), out var en) ? en : tr;
        }

        static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            // dünyalar
            { "Su Altı Kalıntıları", "Sunken Ruins" },
            { "Volkanik Dağ Etekleri", "Volcanic Foothills" },
            { "Çürümüş Orman", "Rotten Forest" },
            { "Dondurucu Zirveler", "Frozen Peaks" },
            { "Gökyüzü Tapınağı", "Sky Temple" },
            { "Çöl Sıcaklığı", "Desert Heat" },
            // bosslar
            { "Abisal Yengeç Kral", "Abyssal Crab King" },
            { "Magma Golemi", "Magma Golem" },
            { "Kök Kralı", "Root King" },
            { "Zırhlı Yeti Savaş Şefi", "Armored Yeti Warchief" },
            { "Fırtına Lordu", "Storm Lord" },
            { "Kumların Hakimi", "Lord of the Sands" },
            // yükseltmeler
            { "Maksimum Can", "Max Health" },
            { "Vuruş Hasarı", "Attack Damage" },
            { "Asker Seviyesi", "Soldier Level" },
            { "Başlangıç Birliği", "Starting Squad" },
            { "Alan Hasarı", "Area Damage" },
            { "Can Yenileme", "Healing" },
            { "Öfke", "Rage" },
            // asker rütbeleri
            { "Çaylak Asker", "Recruit" },
            { "Kıdemli Piyade", "Veteran Infantry" },
            { "Elit Muhafız", "Elite Guard" },
            { "Kraliyet Şövalyesi", "Royal Knight" },
        };
    }
}
