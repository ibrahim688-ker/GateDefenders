using UnityEngine;

namespace StratejiGame
{
    public enum Snd
    {
        PlayerShot, SoldierShot, Hit, CrabDeath, BruteDeath, BossDeath, SoldierDeath,
        GateSoldiers, GatePower, GateBad, HarpoonThrow, Splash, PlayerHurt,
        BossRoar, TsunamiWarn, TsunamiHit, Summon, SkillAoe, SkillHeal, SkillRage,
        Click, Win, Lose,
        Purchase, Denied,
    }

    /// <summary>
    /// Ses çalar: kodla sentezlenen klipleri havuzlu AudioSource'larla çalar, çok sık tekrarlanan
    /// sesleri (asker atışı, isabet) sınırlar, su altı ortam sesini döngüde tutar.
    /// Oyun açılınca kendiliğinden oluşur, sahneler arasında kalır. Ses açık/kapalı PlayerPrefs'te saklanır.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx I { get; private set; }
        const string PrefKey = "sound_on";
        const int Voices = 14;

        AudioClip[] clips;
        float[] volume, minGap, pitchVar, lastTime;
        AudioSource[] voices;
        int next;
        AudioSource ambience;
        float ambienceTarget;

        /// <summary>Ana ses (eski tek ayar). Açık iken efekt / ortam ayrı ayrı kapatılabilir.</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(PrefKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
                PlayerPrefs.Save();
                AudioListener.volume = value ? 1f : 0f;
            }
        }

        /// <summary>Ses efektleri (atış, patlama, vuruş...).</summary>
        public static bool EffectsOn
        {
            get => PlayerPrefs.GetInt("sfx_on", 1) == 1;
            set { PlayerPrefs.SetInt("sfx_on", value ? 1 : 0); PlayerPrefs.Save(); effectsCache = value; }
        }

        /// <summary>Dünya ortam sesi (dalga, rüzgâr, fırtına...).</summary>
        public static bool AmbienceOn
        {
            get => PlayerPrefs.GetInt("amb_on", 1) == 1;
            set { PlayerPrefs.SetInt("amb_on", value ? 1 : 0); PlayerPrefs.Save(); ambOn = value; }
        }
        static bool ambOn = true;
        static bool effectsCache = true;
        public static void RefreshPrefs() { ambOn = AmbienceOn; effectsCache = EffectsOn; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Spawn()
        {
            var go = new GameObject("Sfx");
            DontDestroyOnLoad(go);
            I = go.AddComponent<Sfx>();
        }

        void Awake()
        {
            // Eski tek "ses kapalı" ayarı: yeni ayrı ayarlara taşınır
            if (!Enabled) { PlayerPrefs.SetInt("sfx_on", 0); PlayerPrefs.SetInt("amb_on", 0); Enabled = true; }
            RefreshPrefs();
            AudioListener.volume = Enabled ? 1f : 0f;
            // Tüm sesler 2B: tek dinleyici burada; sahnelerdeki kamera dinleyicileri kapatılır (menu sahnesinde hiç yoktu)
            gameObject.AddComponent<AudioListener>();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => DisableOtherListeners();
            DisableOtherListeners();
            int n = System.Enum.GetValues(typeof(Snd)).Length;
            clips = new AudioClip[n];
            volume = new float[n];
            minGap = new float[n];
            pitchVar = new float[n];
            lastTime = new float[n];
            for (int i = 0; i < n; i++) lastTime[i] = -10f;

            Def(Snd.PlayerShot, SoundSynth.PlayerShot(), 0.225f, 0.05f, 0.06f);
            Def(Snd.SoldierShot, SoundSynth.SoldierShot(), 0.09f, 0.07f, 0.12f);
            Def(Snd.Hit, SoundSynth.Hit(), 0.11f, 0.05f, 0.15f);
            Def(Snd.CrabDeath, SoundSynth.CrabDeath(1f), 0.45f, 0.06f, 0.12f);
            Def(Snd.BruteDeath, SoundSynth.CrabDeath(1.8f), 0.7f, 0.05f, 0.08f);
            Def(Snd.BossDeath, SoundSynth.CrabDeath(3.2f), 1f, 0.2f, 0f);
            Def(Snd.SoldierDeath, SoundSynth.SoldierDeath(), 0.4f, 0.08f, 0.1f);
            Def(Snd.GateSoldiers, SoundSynth.GateSoldiers(), 0.6f, 0.1f, 0f);
            Def(Snd.GatePower, SoundSynth.GatePower(), 0.6f, 0.1f, 0f);
            Def(Snd.GateBad, SoundSynth.GateBad(), 0.6f, 0.1f, 0f);
            Def(Snd.HarpoonThrow, SoundSynth.HarpoonThrow(), 0.4f, 0.12f, 0.1f);
            Def(Snd.Splash, SoundSynth.Splash(), 0.4f, 0.1f, 0.12f);
            Def(Snd.PlayerHurt, SoundSynth.PlayerHurt(), 0.7f, 0.12f, 0.06f);
            Def(Snd.BossRoar, SoundSynth.BossRoar(), 0.9f, 1f, 0.05f);
            Def(Snd.TsunamiWarn, SoundSynth.TsunamiWarn(), 0.7f, 0.5f, 0f);
            Def(Snd.TsunamiHit, SoundSynth.TsunamiHit(), 0.9f, 0.5f, 0f);
            Def(Snd.Summon, SoundSynth.Summon(), 0.55f, 0.5f, 0.05f);
            Def(Snd.SkillAoe, SoundSynth.SkillAoe(), 0.9f, 0.2f, 0f);
            Def(Snd.SkillHeal, SoundSynth.SkillHeal(), 0.7f, 0.2f, 0f);
            Def(Snd.SkillRage, SoundSynth.SkillRage(), 0.8f, 0.2f, 0f);
            Def(Snd.Click, SoundSynth.Click(), 0.5f, 0.05f, 0.03f);
            Def(Snd.Win, SoundSynth.Win(), 0.8f, 1f, 0f);
            Def(Snd.Lose, SoundSynth.Lose(), 0.8f, 1f, 0f);
            Def(Snd.Purchase, SoundSynth.Purchase(), 0.7f, 0.06f, 0.04f);
            Def(Snd.Denied, SoundSynth.Denied(), 0.55f, 0.15f, 0f);

            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                voices[i] = s;
            }
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = SoundSynth.Ambience();
            ambience.loop = true;
            ambience.playOnAwake = false;
            ambience.volume = 0f;
            ambience.priority = 200;
        }

void DisableOtherListeners()
        {
            foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (l.gameObject != gameObject) l.enabled = false;
        }


        void Def(Snd s, AudioClip c, float vol, float gap, float pitch)
        {
            int i = (int)s;
            clips[i] = c;
            volume[i] = vol;
            minGap[i] = gap;
            pitchVar[i] = pitch;
        }

        /// <summary>Ses çalar. Aynı ses çok sık istenirse (minGap içinde) atlanır.</summary>
        public static void Play(Snd s, float volumeScale = 1f)
        {
            if (I == null) return;
            I.PlayInternal(s, volumeScale);
        }

        public static void Click() => Play(Snd.Click);

        void PlayInternal(Snd s, float volumeScale)
        {
            if (!effectsCache) return;
            int i = (int)s;
            float now = Time.unscaledTime;
            if (now - lastTime[i] < minGap[i]) return;
            lastTime[i] = now;

            // Boştaki sesi bul; yoksa en eskisini kullan
            AudioSource src = null;
            for (int k = 0; k < voices.Length; k++)
            {
                var v = voices[(next + k) % voices.Length];
                if (!v.isPlaying) { src = v; next = (next + k + 1) % voices.Length; break; }
            }
            if (src == null) { src = voices[next]; next = (next + 1) % voices.Length; }

            src.clip = clips[i];
            src.volume = volume[i] * volumeScale;
            src.pitch = 1f + Random.Range(-pitchVar[i], pitchVar[i]);
            src.Play();
        }

        // Temaya göre değişen klipler (ilk ihtiyaçta üretilir)
        AudioClip[] underwaterSet, volcanoSet, swampSet, frostSet, skySet, desertSet;
        AudioClip underwaterAmb, volcanoAmb, swampAmb, frostAmb, skyAmb, desertAmb;
        static readonly Snd[] ThemedSounds = { Snd.CrabDeath, Snd.BruteDeath, Snd.BossDeath, Snd.Splash, Snd.TsunamiHit, Snd.HarpoonThrow };
        WorldTheme currentTheme = WorldTheme.Underwater;

        /// <summary>Dünya temasına göre ölüm/sıçrama/saldırı seslerini ve ortam sesini değiştirir.</summary>
        public static void SetTheme(WorldTheme theme)
        {
            if (I == null || I.currentTheme == theme) return;
            I.ApplyTheme(theme);
        }

        void ApplyTheme(WorldTheme theme)
        {
            if (underwaterSet == null)
            {
                underwaterSet = new AudioClip[ThemedSounds.Length];
                for (int i = 0; i < ThemedSounds.Length; i++) underwaterSet[i] = clips[(int)ThemedSounds[i]];
                underwaterAmb = ambience.clip;
            }
            if (theme == WorldTheme.Volcano && volcanoSet == null)
            {
                volcanoSet = new[] { SoundSynth.RockDeath(1f), SoundSynth.RockDeath(1.8f), SoundSynth.RockDeath(3.2f), SoundSynth.LavaSplash(), SoundSynth.Eruption(), underwaterSet[5] };
                volcanoAmb = SoundSynth.VolcanoAmbience();
            }
            if (theme == WorldTheme.Swamp && swampSet == null)
            {
                swampSet = new[] { SoundSynth.SquishDeath(1f), SoundSynth.SquishDeath(1.8f), SoundSynth.SquishDeath(3.2f), SoundSynth.MudSplash(), SoundSynth.RootBurst(), SoundSynth.BowShot() };
                swampAmb = SoundSynth.SwampAmbience();
            }
            if (theme == WorldTheme.Frost && frostSet == null)
            {
                frostSet = new[] { SoundSynth.SnowDeath(1f), SoundSynth.SnowDeath(1.8f), SoundSynth.IceShatterBig(), SoundSynth.IceHit(), SoundSynth.IceBurst(), SoundSynth.IceCast() };
                frostAmb = SoundSynth.BlizzardAmbience();
            }
            if (theme == WorldTheme.Sky && skySet == null)
            {
                skySet = new[] { SoundSynth.PuffDeath(1f), SoundSynth.PuffDeath(1.8f), SoundSynth.ThunderBig(), SoundSynth.Zap(), SoundSynth.ThunderStrike(), SoundSynth.ZapThrow() };
                skyAmb = SoundSynth.StormAmbience();
            }
            if (theme == WorldTheme.Desert && desertSet == null)
            {
                desertSet = new[] { SoundSynth.SandDeath(1f), SoundSynth.SandDeath(1.8f), SoundSynth.SandCrumble(), SoundSynth.SandHit(), SoundSynth.SandStorm(), SoundSynth.ChakramThrow() };
                desertAmb = SoundSynth.DesertAmbience();
            }
            var set = theme == WorldTheme.Desert ? desertSet : theme == WorldTheme.Sky ? skySet : theme == WorldTheme.Volcano ? volcanoSet : theme == WorldTheme.Swamp ? swampSet : theme == WorldTheme.Frost ? frostSet : underwaterSet;
            for (int i = 0; i < ThemedSounds.Length; i++) clips[(int)ThemedSounds[i]] = set[i];
            bool wasPlaying = ambience.isPlaying;
            ambience.clip = theme == WorldTheme.Desert ? desertAmb : theme == WorldTheme.Sky ? skyAmb : theme == WorldTheme.Volcano ? volcanoAmb : theme == WorldTheme.Swamp ? swampAmb : theme == WorldTheme.Frost ? frostAmb : underwaterAmb;
            if (wasPlaying) ambience.Play();
            currentTheme = theme;
        }

        /// <summary>Su altı ortam sesini yumuşakça aç/kapat.</summary>
        public static void SetAmbience(bool on, float level = 0.35f)
        {
            if (I == null) return;
            I.ambienceTarget = on ? level : 0f;
            if (on && !I.ambience.isPlaying) I.ambience.Play();
        }

        void Update()
        {
            if (ambience == null) return;
            float target = ambOn ? ambienceTarget : 0f;
            ambience.volume = Mathf.MoveTowards(ambience.volume, target, Time.unscaledDeltaTime * (ambOn ? 0.4f : 2f));
            if (ambience.volume <= 0f && target <= 0f && ambience.isPlaying) ambience.Stop();
            else if (target > 0f && !ambience.isPlaying) ambience.Play();
        }
    }
}
