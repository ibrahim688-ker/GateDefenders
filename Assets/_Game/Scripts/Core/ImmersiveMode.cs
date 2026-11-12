using UnityEngine;
using UnityEngine.SceneManagement;

namespace StratejiGame
{
    /// <summary>
    /// Android tam ekran (yapışkan sürükleyici mod): üstteki durum çubuğu ve alttaki gezinme (geri) tuşları gizli kalır.
    /// Kullanıcı kenardan kaydırırsa çubuklar kısa süre görünüp kendiliğinden kaybolur; dokunmak onları kalıcı açmaz.
    /// Çizim çözünürlüğü düşürülünce (Screen.SetResolution) bazı telefonlarda bu mod bozulduğu için
    /// açılışta, her sahne yüklenişinde ve uygulamaya geri dönülünce yeniden uygulanır.
    /// </summary>
    public class ImmersiveMode : MonoBehaviour
    {
        static ImmersiveMode I;

        public static void Ensure()
        {
            if (I != null) return;
            var go = new GameObject("ImmersiveMode");
            DontDestroyOnLoad(go);
            I = go.AddComponent<ImmersiveMode>();
        }

        void Start()
        {
            Apply();
            Invoke(nameof(ApplyLater), 1f);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene s, LoadSceneMode m) => Apply();

        void ApplyLater() => Apply();

        void OnApplicationFocus(bool focus)
        {
            if (!focus) return;
            Apply();
            Invoke(nameof(ApplyLater), 0.6f);
        }

        public static void Apply()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.fullScreen = true;
            try
            {
                var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity == null) return;
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        var window = activity.Call<AndroidJavaObject>("getWindow");
                        int sdk;
                        using (var v = new AndroidJavaClass("android.os.Build$VERSION")) sdk = v.GetStatic<int>("SDK_INT");
                        if (sdk >= 30)
                        {
                            var ctrl = window.Call<AndroidJavaObject>("getInsetsController");
                            if (ctrl != null)
                            {
                                int bars;
                                using (var t = new AndroidJavaClass("android.view.WindowInsets$Type")) bars = t.CallStatic<int>("systemBars");
                                ctrl.Call("setSystemBarsBehavior", 2);   // BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
                                ctrl.Call("hide", bars);
                            }
                        }
                        else
                        {
                            // IMMERSIVE_STICKY | FULLSCREEN | HIDE_NAVIGATION | LAYOUT_FULLSCREEN | LAYOUT_HIDE_NAVIGATION | LAYOUT_STABLE
                            var decor = window.Call<AndroidJavaObject>("getDecorView");
                            decor.Call("setSystemUiVisibility", 0x1706);
                        }
                    }
                    catch (System.Exception e) { Debug.LogWarning("Tam ekran uygulanamadı: " + e.Message); }
                }));
            }
            catch (System.Exception e) { Debug.LogWarning("Tam ekran uygulanamadı: " + e.Message); }
#endif
        }
    }
}
