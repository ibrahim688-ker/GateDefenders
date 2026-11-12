using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StratejiGame
{
    /// <summary>
    /// Yükleniyor sahnesi: oyun açılırken ve menü ↔ oyun geçişlerinde gösterilir.
    /// Ortada oyun simgesi ve adı, altında dolan oval çubuk + "YÜKLENİYOR..." yazısı, en altta değişen ipuçları.
    /// Hedef sahne arka planda yüklenir; çubuk yumuşakça dolar, en az kısa bir süre ekranda kalır.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public const string SceneName = "Loading";

        /// <summary>Yüklenecek sahne (boşsa ana menü).</summary>
        public static string Target;
        static bool firstLaunchDone;

        /// <summary>Yükleniyor ekranı üzerinden sahne değiştirir.</summary>
        public static void Load(string scene)
        {
            Target = scene;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneName);
        }

        RectTransform barFill, iconRt, glowRt;
        Text loadingText, tipText;
        CanvasGroup tipGroup;
        AsyncOperation op;
        float shown, startT, minTime, tipT;
        int tipIndex, dots = -1;
        bool activating, tipSwapped;

        static readonly Color BarBg = new Color(0f, 0f, 0f, 0.45f);
        static readonly Color BarFill = new Color(1f, 0.62f, 0.15f);
        static readonly Color Gold = new Color(1f, 0.85f, 0.3f);

        static string[] Tips => Loc.EN
            ? new[]
            {
                "Stay away from red gates!",
                "Blue gates grow your army.",
                "Find what each gate symbol means in the GUIDE on the main menu.",
                "Spend gold on upgrades to get stronger.",
                "Rage boosts fire rate and damage for a short time.",
                "Shoot some gates to raise their value.",
                "Replay cleared levels to earn more gold.",
            }
            : new[]
            {
                "Kırmızı kapılardan uzak dur!",
                "Mavi kapılar ordunu büyütür.",
                "Kapı sembollerinin anlamını ana menüdeki REHBER'de bulabilirsin.",
                "Altınla yükseltme alarak güçlen.",
                "Öfke yeteneği kısa süre ateş hızını ve hasarı artırır.",
                "Bazı kapıları vurarak değerini artırabilirsin.",
                "Geçtiğin seviyeleri tekrar oynayıp daha çok altın kazanabilirsin.",
            };

        void Start()
        {
            AppBoot.ApplyFrameRate();
            string target = string.IsNullOrEmpty(Target) ? GameSession.MenuScene : Target;
            Target = null;
            // ilk açılışta ekran biraz daha uzun kalsın (oyun adı görünsün), sahne geçişlerinde kısa
            minTime = firstLaunchDone ? 0.6f : 1.6f;
            firstLaunchDone = true;
            tipIndex = Random.Range(0, Tips.Length);
            Build();
            startT = Time.unscaledTime;
            op = SceneManager.LoadSceneAsync(target);
            op.allowSceneActivation = false;
        }

        void Build()
        {
            var root = UiKit.CreateCanvas(transform, "LoadingCanvas", 100);
            var font = UiKit.MenuFont;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(RawImage));
            var bgRt = (RectTransform)bg.transform;
            bgRt.SetParent(root, false);
            UiKit.Stretch(bgRt, Vector2.zero, Vector2.one);
            var raw = bg.GetComponent<RawImage>();
            var bgTex = Resources.Load<Texture2D>("Loading/LoadingBg");
            if (bgTex != null) { raw.texture = bgTex; raw.uvRect = new Rect(0.5f - 0.5f * AspectX(), 0f, AspectX(), 1f); }
            else raw.texture = MenuArt.Gradient("loading", new Color(0.16f, 0.35f, 0.78f), new Color(0.06f, 0.14f, 0.36f), new Color(0.03f, 0.07f, 0.2f));
            raw.raycastTarget = false;

            // simge: arkasında yumuşak parıltı, hafif nabız atar
            glowRt = UiKit.Fixed(root, "Glow", new Vector2(0f, 190f), new Vector2(760f, 760f), new Color(1f, 0.85f, 0.45f, 0.35f), UiIcons.Spark);
            glowRt.GetComponent<Image>().raycastTarget = false;
            var iconTex = Resources.Load<Texture2D>("Loading/LoadingIcon");
            iconRt = UiKit.Fixed(root, "Icon", new Vector2(0f, 190f), new Vector2(480f, 480f), Color.white);
            var iconImg = iconRt.GetComponent<Image>();
            iconImg.raycastTarget = false;
            if (iconTex != null) iconImg.sprite = Sprite.Create(iconTex, new Rect(0, 0, iconTex.width, iconTex.height), new Vector2(0.5f, 0.5f));
            else iconImg.color = Color.clear;
            var ish = iconRt.gameObject.AddComponent<Shadow>();
            ish.effectColor = new Color(0f, 0f, 0f, 0.45f);
            ish.effectDistance = new Vector2(0f, -14f);

            // oyun adı
            var title = UiKit.Label(root, "GATE DEFENDERS", 104, TextAnchor.MiddleCenter, Gold);
            if (font != null) title.font = font;
            UiKit.Stretch(title.rectTransform, new Vector2(0f, 0.33f), new Vector2(1f, 0.43f));
            var to = title.gameObject.AddComponent<Outline>();
            to.effectColor = new Color(0.25f, 0.1f, 0f, 0.9f);
            to.effectDistance = new Vector2(4f, -4f);
            var ts = title.gameObject.AddComponent<Shadow>();
            ts.effectColor = new Color(0f, 0f, 0f, 0.5f);
            ts.effectDistance = new Vector2(0f, -10f);

            // dolan oval çubuk
            var barBg = UiKit.Panel(root, "BarBg", new Vector2(0.14f, 0.215f), new Vector2(0.86f, 0.24f), BarBg);
            UiIcons.MakePill(barBg).raycastTarget = false;
            barFill = UiKit.Panel(barBg, "Fill", Vector2.zero, new Vector2(0.02f, 1f), BarFill);
            barFill.offsetMin = new Vector2(5f, 5f);
            barFill.offsetMax = new Vector2(0f, -5f);
            UiIcons.MakePill(barFill).raycastTarget = false;
            var shine = UiKit.Panel(barFill, "Shine", new Vector2(0.02f, 0.55f), new Vector2(0.98f, 0.9f), new Color(1f, 1f, 1f, 0.3f));
            UiIcons.MakePill(shine).raycastTarget = false;

            loadingText = UiKit.Label(root, "", 44, TextAnchor.MiddleCenter);
            if (font != null) loadingText.font = font;
            UiKit.Stretch(loadingText.rectTransform, new Vector2(0f, 0.25f), new Vector2(1f, 0.29f));
            var lo = loadingText.gameObject.AddComponent<Outline>();
            lo.effectColor = new Color(0f, 0f, 0f, 0.6f);
            lo.effectDistance = new Vector2(2f, -2f);

            // ipucu kartı
            var tipCard = UiKit.Panel(root, "Tip", new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.165f), new Color(0.02f, 0.05f, 0.14f, 0.6f));
            UiIcons.Round(tipCard, 34f).raycastTarget = false;
            tipGroup = tipCard.gameObject.AddComponent<CanvasGroup>();
            var tipTitle = UiKit.Label(tipCard, Loc.T("İPUCU", "TIP"), 34, TextAnchor.MiddleCenter, Gold);
            if (font != null) tipTitle.font = font;
            UiKit.Stretch(tipTitle.rectTransform, new Vector2(0f, 0.64f), new Vector2(1f, 0.94f));
            tipText = UiKit.Label(tipCard, "", 38, TextAnchor.MiddleCenter, new Color(0.9f, 0.94f, 1f));
            if (font != null) tipText.font = font;
            tipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiKit.Stretch(tipText.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.64f));
            tipText.text = Tips[tipIndex];
        }

        /// <summary>Kare arka plan dokusunu ekran oranına göre ortadan kırpar (uzamasın).</summary>
        static float AspectX()
        {
            float a = (float)Screen.width / Mathf.Max(1, Screen.height);
            return Mathf.Clamp01(a);
        }

        void Update()
        {
            float now = Time.unscaledTime;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float elapsed = now - startT;

            // yükleme 0..0.9 arası raporlanır; süre sınırıyla birlikte çubuğun hedefi
            float loadK = op != null ? Mathf.Clamp01(op.progress / 0.9f) : 1f;
            float timeK = Mathf.Clamp01(elapsed / minTime);
            float target = Mathf.Min(loadK, timeK);
            shown = Mathf.MoveTowards(shown, target, dt * 1.6f);
            barFill.anchorMax = new Vector2(Mathf.Max(0.04f, shown), 1f);

            int d = (int)(now * 2.5f) % 4;
            if (d != dots)
            {
                dots = d;
                loadingText.text = Loc.T("YÜKLENİYOR", "LOADING") + new string('.', d);
            }

            float pulse = 1f + 0.025f * Mathf.Sin(now * 3f);
            iconRt.localScale = Vector3.one * pulse;
            glowRt.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(now * 2.2f));
            glowRt.localEulerAngles = new Vector3(0f, 0f, now * 10f);

            // ipucu her 3 saniyede yumuşak geçişle değişir
            tipT += dt;
            if (tipT > 3f)
            {
                float k = (tipT - 3f) / 0.25f;
                if (k < 1f) tipGroup.alpha = 1f - k;
                else
                {
                    if (!tipSwapped) { tipSwapped = true; tipIndex = (tipIndex + 1) % Tips.Length; tipText.text = Tips[tipIndex]; }
                    tipGroup.alpha = Mathf.Min(1f, k - 1f);
                    if (k >= 2f) { tipT = 0f; tipSwapped = false; tipGroup.alpha = 1f; }
                }
            }

            if (!activating && op != null && shown >= 0.999f && loadK >= 1f)
            {
                activating = true;
                op.allowSceneActivation = true;
            }
        }
    }
}
