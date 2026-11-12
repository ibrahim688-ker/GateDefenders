#if UNITY_EDITOR
// Geliştirici aracı (FPS / CPU-GPU / pil sıcaklığı göstergesi): yalnız editörde derlenir, yayın build'ine girmez.
using UnityEngine;
using UnityEngine.UI;

namespace StratejiGame
{
    /// <summary>
    /// Ekranda FPS göstergesi (sağ üstte, altın kutusunun altında). Oyun açılınca kendiliğinden oluşur, sahneler arasında kalır.
    /// Yarım saniyelik ortalama: 55+ yeşil, 40+ sarı, altı kırmızı. Kapatmak için Enabled = false.
    /// </summary>
    public class FpsCounter : MonoBehaviour
    {
        /// <summary>Göstergeyi göster / gizle (Profil > Ayarlar). PlayerPrefs "show_fps", varsayılan açık.</summary>
        public static bool Enabled
        {
            get => Application.isEditor && PlayerPrefs.GetInt("show_fps", 0) == 1;   // yayında gizli; yalnızca editörde, elle açılırsa
            set
            {
                PlayerPrefs.SetInt("show_fps", value ? 1 : 0);
                PlayerPrefs.Save();
                if (I != null) I.panel.SetActive(value);
            }
        }

        static FpsCounter I;
        GameObject panel;
        bool compact;
        Text label;
        float acc;
        int frames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            if (!Enabled) return;   // kapalıyken hiç oluşturulmaz (her karede ölçüm maliyeti olmasın)
            var go = new GameObject("FpsCounter");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = new GameObject("Bg");
            bg.transform.SetParent(go.transform, false);
            var img = bg.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);
            UiIcons.Round(img, 18f);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            // Sol altta, yetenek düğmelerinin hemen üstünde (sağ üstte duraklatma düğmesi var)
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(24f, 250f);
            rt.sizeDelta = new Vector2(500f, 120f);

            var tgo = new GameObject("Text");
            tgo.transform.SetParent(bg.transform, false);
            var txt = tgo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 30;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.raycastTarget = false;
            txt.text = "FPS --";
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(14f, 4f);
            trt.offsetMax = new Vector2(-8f, -4f);

            I = go.AddComponent<FpsCounter>();
            I.label = txt;
            I.panel = bg;
            bg.SetActive(Enabled);
            Place(rt);
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (sc, m) => { if (I != null) Place((RectTransform)I.panel.transform); };
        }

        /// <summary>Oyunda sol altta (yetenek düğmelerinin üstü); menüde sol üstte, üst barın hemen altında (ayarları kapatmasın).</summary>
        static void Place(RectTransform rt)
        {
            bool menu = FindFirstObjectByType<MainMenuUI>() != null;
            if (I != null)
            {
                I.compact = menu;
                I.label.alignment = menu ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            }
            if (menu)
            {
                // Menüde tek satır, üst barın ortasında (seviye ile altın arasında)
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -30f);
                rt.sizeDelta = new Vector2(220f, 64f);
            }
            else
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(24f, 335f);   // yuvarlak yetenek düğmelerinin üstünde
                rt.sizeDelta = new Vector2(500f, 120f);
            }
        }

        readonly FrameTiming[] timings = new FrameTiming[1];
        float tempT, batteryTemp = -1f;

        /// <summary>Android pil sıcaklığı (°C); ısınmayı izlemek için. Okunamazsa -1.</summary>
        static float ReadBatteryTemp()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var filter = new AndroidJavaObject("android.content.IntentFilter", "android.intent.action.BATTERY_CHANGED"))
                using (var intent = activity.Call<AndroidJavaObject>("registerReceiver", null, filter))
                    return intent != null ? intent.Call<int>("getIntExtra", "temperature", -10) / 10f : -1f;
            }
            catch { return -1f; }
#else
            return -1f;
#endif
        }

        void Update()
        {
            if (!panel.activeSelf) return;
            acc += Time.unscaledDeltaTime;
            frames++;
            if (acc < 0.5f) return;
            float fps = frames / acc;
            acc = 0f;
            frames = 0;
            // Teşhis: işlemci / ekran kartı kare süresi (hangisi darboğaz?), pil sıcaklığı (ısınma), sahnedeki yük
            var res = Screen.currentResolution;
            string timing = "";
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                timing = "CPU " + timings[0].cpuFrameTime.ToString("0.0") + " ms  GPU " + (timings[0].gpuFrameTime > 0 ? timings[0].gpuFrameTime.ToString("0.0") + " ms" : "-");
            tempT -= 0.5f;
            if (tempT <= 0f) { tempT = 3f; batteryTemp = ReadBatteryTemp(); }
            string load = "";
            var gm = GameManager.I;
            if (gm != null && gm.Enemies != null && gm.Army != null)
                load = Loc.T("düşman ", "enemies ") + gm.Enemies.Active.Count + Loc.T("  mermi ", "  bullets ") + gm.Enemies.ActiveBullets.Count + Loc.T("  asker ", "  soldiers ") + gm.Army.Count;
            if (compact) label.text = "FPS " + Mathf.RoundToInt(fps);
            else label.text = "FPS " + Mathf.RoundToInt(fps) + Loc.T("  (hedef ", "  (target ") + Application.targetFrameRate + ")" + (batteryTemp > 0f ? Loc.T("  pil ", "  battery ") + batteryTemp.ToString("0.0") + "°C" : "") + "\n"
                + (timing.Length > 0 ? timing : Mathf.RoundToInt((float)res.refreshRateRatio.value) + " Hz  " + Screen.width + "x" + Screen.height) + (AppBoot.LowEnd ? Loc.T("  düşük", "  low") : "") + "\n"
                + (load.Length > 0 ? load : SystemInfo.graphicsDeviceName);
            label.color = fps >= 55f ? new Color(0.4f, 1f, 0.4f) : fps >= 40f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.35f, 0.3f);
        }
    }
}
#endif
