using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Kısa titreşim (Android). Ayar Profil sekmesinden açılıp kapatılır (PlayerPrefs "vibrate_on").
    /// Çok sık titreşmemesi için bekleme süresi vardır; editörde ve desteklenmeyen cihazlarda hiçbir şey yapmaz.
    /// </summary>
    public static class Haptics
    {
        const string PrefKey = "vibrate_on";
        static bool? enabledCache;
        static float lastTime = -10f;

        public static bool Enabled
        {
            get
            {
                if (!enabledCache.HasValue) enabledCache = PlayerPrefs.GetInt(PrefKey, 1) == 1;
                return enabledCache.Value;
            }
            set
            {
                enabledCache = value;
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static bool initTried;
        static int sdk;
#endif

        /// <summary>ms milisaniye titreşir (uzun titreşimler kısa olanları bastırmaz: bekleme 0.12 sn).</summary>
        public static void Vibrate(long ms)
        {
            if (!Enabled) return;
            float now = Time.unscaledTime;
            if (now - lastTime < 0.12f) return;
            lastTime = now;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (!initTried)
                {
                    initTried = true;
                    using (var ver = new AndroidJavaClass("android.os.Build$VERSION")) sdk = ver.GetStatic<int>("SDK_INT");
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                if (vibrator == null) { Handheld.Vibrate(); return; }
                if (sdk >= 26)
                {
                    using (var eff = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = eff.CallStatic<AndroidJavaObject>("createOneShot", ms, -1))
                        vibrator.Call("vibrate", effect);
                }
                else vibrator.Call("vibrate", ms);
            }
            catch (System.Exception)
            {
                vibrator = null;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (ms >= 150) Handheld.Vibrate();
#endif
        }

        // Handheld.Vibrate'e bir başvuru: Unity Android manifestine VIBRATE iznini bununla ekler
        internal static void ManifestPermissionHint() => Handheld.Vibrate();
    }
}
