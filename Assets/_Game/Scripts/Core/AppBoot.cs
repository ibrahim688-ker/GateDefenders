using UnityEngine;
using UnityEngine.SceneManagement;

namespace StratejiGame
{
    /// <summary>
    /// Uygulama açılır açılmaz (ilk sahneden önce) performans ayarlarını yapar.
    /// - Kare hızı: ekran yenileme hızının 58 üstündeki en küçük tam böleni (60 Hz → 60, 90 → 90, 120 → 60, 144 → 72).
    /// - Android: çizim yüzeyinin kısa kenarı en fazla 1080 (normal cihaz, 1080p telefonda tam çözünürlük = net görüntü),
    ///   zayıf cihazda 900. 1440p ekranlar 1080'e indirilir. Tablette sınır 1440 (zayıf tablette 1080). (URP renderScale 1.0 → ara doku + tam ekran kopya yok.)
    /// - Normal cihazda 2x MSAA kenar yumuşatma (karo tabanlı mobil GPU'larda ucuz), zayıf cihazda kapalı.
    /// - Zayıf ekran kartlarında (Mali-G52 vb.) gerçek zamanlı gölgeler kapatılır.
    /// </summary>
    public static class AppBoot
    {
        public const int MaxSurfaceWidth = 1080;
        public const int MaxSurfaceWidthLowEnd = 900;
        /// <summary>Tablette ekran büyük olduğu için daha yüksek sınır (1600x2560 tablet → 1440x2304).</summary>
        public const int MaxSurfaceWidthTablet = 1440;
        public const int MaxSurfaceWidthTabletLowEnd = 1080;
        public static int TargetFps { get; private set; } = 60;
        static bool detectedLowEnd;
        /// <summary>Editörde düşük grafik modunu denemek için.</summary>
#if UNITY_EDITOR
        public static bool ForceLowEnd;
        public static bool LowEnd => detectedLowEnd || ForceLowEnd;
#else
        public static bool LowEnd => detectedLowEnd;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            detectedLowEnd = DetectLowEnd();
#if UNITY_ANDROID && !UNITY_EDITOR
            int w = Screen.width, h = Screen.height;
            int shortSide = Mathf.Min(w, h);
            bool tablet = IsTablet(w, h);
            int maxW = tablet ? (detectedLowEnd ? MaxSurfaceWidthTabletLowEnd : MaxSurfaceWidthTablet)
                              : (detectedLowEnd ? MaxSurfaceWidthLowEnd : MaxSurfaceWidth);
            if (shortSide > maxW)
            {
                float k = (float)maxW / shortSide;
                Screen.SetResolution(Mathf.RoundToInt(w * k), Mathf.RoundToInt(h * k), FullScreenMode.FullScreenWindow);
            }
#endif
            // Tam ekran: durum çubuğu ve geri tuşları gizli kalsın (dokununca açılıp kalmasın)
            ImmersiveMode.Ensure();
            ApplyFrameRate();
            ApplyAntiAliasing();
            // Oyunda fizik kullanılmıyor (çarpışmalar elle hesaplanıyor): fizik motorunu her karede çalıştırma
            Physics.simulationMode = SimulationMode.Script;
            Physics2D.simulationMode = SimulationMode2D.Script;
            ApplyShaderQuality();
            SceneManager.sceneLoaded += (s, m) => ApplySceneQuality();
        }

        public static void ApplyFrameRate()
        {
            QualitySettings.vSyncCount = 0;
            int hz = 60;
            try { hz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value); } catch { }
            if (hz < 50) hz = 60;
            int target = hz;
            for (int n = 4; n >= 1; n--)
            {
                if (hz / (float)n >= 58f) { target = Mathf.RoundToInt(hz / (float)n); break; }
            }
            TargetFps = target;
            Application.targetFrameRate = target;
        }

        /// <summary>Tablet mi: kısa kenar 3.4 inçten genişse (telefonlar ~2.6–3.1 inç) ya da ekran oranı 16:10'a yakınsa.</summary>
        static bool IsTablet(int w, int h)
        {
            float shortPx = Mathf.Min(w, h), longPx = Mathf.Max(w, h);
            float dpi = Screen.dpi;
            if (dpi > 50f) return shortPx / dpi >= 3.4f;
            return longPx / shortPx < 1.7f;
        }

        /// <summary>Kenar yumuşatma: normal cihazda 2x MSAA, zayıf cihazda kapalı. (Editörde proje dosyasını değiştirmemek için uygulanmaz.)</summary>
        static void ApplyAntiAliasing()
        {
#if !UNITY_EDITOR
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset rp)
                rp.msaaSampleCount = LowEnd ? 1 : 2;
#endif
        }

        /// <summary>Zemin / taş shader'ındaki kostik ışık: zayıf GPU'da kapalı (piksel başına iki doku okuması).</summary>
        public static void ApplyShaderQuality() => Shader.SetGlobalFloat("_CausticGlobal", LowEnd ? 0f : 1f);

        /// <summary>Zayıf cihazda sahnedeki yönlü ışıkların gölgesini kapatır.</summary>
        static void ApplySceneQuality()
        {
            ApplyShaderQuality();
            if (!LowEnd) return;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.shadows = LightShadows.None;
        }

        /// <summary>Basit ekran kartı / bellek sezgisi.</summary>
        static bool DetectLowEnd()
        {
#if UNITY_EDITOR
            return false;
#else
            string g = (SystemInfo.graphicsDeviceName ?? "").ToLowerInvariant();
            if (SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 3500) return true;
            if (g.Contains("mali-t") || g.Contains("mali-g3") || g.Contains("mali-g5") || g.Contains("mali-g71") || g.Contains("mali-g72")) return true;
            if (g.Contains("powervr")) return true;
            if (g.Contains("adreno"))
            {
                // Adreno 3xx/4xx/5xx ve 60x/61x serisi
                var digits = System.Text.RegularExpressions.Regex.Match(g, @"adreno\D*(\d{3})");
                if (digits.Success && int.TryParse(digits.Groups[1].Value, out int n) && (n < 600 || n < 620)) return true;
            }
            return false;
#endif
        }
    }
}
