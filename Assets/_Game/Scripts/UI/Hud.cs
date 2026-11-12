using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace StratejiGame
{
    /// <summary>
    /// Oyun içi arayüz (çalışma anında kurulur).
    /// Üst bar: kalp ikonlu oval can barı (hasarda geriden eriyen iz), para ikonlu altın kutusu, yuvarlak duraklatma düğmesi,
    /// seviye + dünya adı, dünya renginde ilerleme çubuğu (sonunda bayrak / boss kurukafası, üzerinde kayan işaret).
    /// Altta yuvarlak ikonlu yetenek düğmeleri: saat gibi dolan bekleme, hazır olunca parlayıp nabız atar.
    /// Hasar alınca ekran kenarı kızarır, altın artınca sayı zıplar, bildirimler büyüyerek gelir.
    /// Boss barı dünya süsüyle (alev tacı, buz tacı...) yukarıdan kayarak gelir. Duraklatma menüsü yuvarlak kart.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        GameManager gm;
        RectTransform root;
        MenuThemeView.Palette pal;

        // üst bar
        RectTransform hpFill, hpTrail, heart, progressFill, progressDot, goldBox, goldCoin;
        Text hpText, goldText, levelText, worldText, toastText;
        Image hpFillImg;
        float hpTrailK = 1f, hpTrailHold, lastHp = -1f, heartPopT;
        int lastGold, goldShown, goldTextValue = -1, shownHp = -1, shownMax = -1;
        float goldPopT;

        // efektler
        Image vignette;
        float vignetteA;
        Color vignetteC = new Color(1f, 0.1f, 0.08f);
        float toastT, toastPopT;
        float progShown;
        const float ToastTime = 1.3f;

        // boss
        RectTransform bossBox, bossFill, bossTrail;
        Text bossText;
        GameObject bossBar;
        float bossTrailK = 1f, bossTrailHold, bossInT, lastBossK = 1f;

        // duraklatma / bitiş
        GameObject pausePanel, pauseButton;
        RectTransform pauseCard;
        Text pauseInfo;
        EndScreen endScreen;
        float pauseInT;

        SkillButton aoeBtn, healBtn, rageBtn;

        // yeni güçlendirme açıklama kartı
        RectTransform introLayer, introCard, introBadge;
        Image introIcon, introRim, introBadgeImg;
        Text introTitle, introDesc;
        float introT, introInT;
        const float IntroTime = 1.8f, IntroSlow = 0.3f;

        class SkillButton
        {
            public Button button;
            public RectTransform rt, glow;
            public Image bg, icon, cooldown, glowImg;
            public Text sub, name;
            public Color color;
            public float prevK = -1f, popT, iconA = -1f, glowA = -1f, scale = -1f;
            public int subKey = int.MinValue;
            public bool empty;
        }

        public void Init()
        {
            gm = GameManager.I;
            pal = MenuThemeView.Get(gm.Theme);
            EnsureEventSystem();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            root = (RectTransform)canvasGo.transform;

            // Hasar / iyileşme kenar parlaması (en altta; görünmezken kapalı)
            vignette = Panel(Layer("FxLayer", false), "Vignette", Vector2.zero, Vector2.one, new Color(1f, 0.1f, 0.08f, 0f)).GetComponent<Image>();
            vignette.sprite = UiIcons.Vignette;
            vignette.raycastTarget = false;
            vignette.gameObject.SetActive(false);

            BuildTopBar();
            BuildBossBar();

            // Kapı / olay bildirimi
            toastText = Label(Layer("ToastLayer", false), "", 64, TextAnchor.MiddleCenter);
            Stretch(toastText.rectTransform, new Vector2(0f, 0.7f), new Vector2(1f, 0.78f));
            var to = toastText.gameObject.AddComponent<Outline>();
            to.effectColor = new Color(0f, 0f, 0f, 0.75f);
            to.effectDistance = new Vector2(3f, -3f);

            // Yetenek düğmeleri
            aoeBtn = MakeSkill(Loc.T("ALAN HASARI", "AREA BLAST"), UiIcons.Blast, new Color(0.95f, 0.5f, 0.12f), 0.19f, gm.Skills.UseAoe);
            healBtn = MakeSkill(Loc.T("CAN YENİLE", "HEAL"), UiIcons.Potion, new Color(0.25f, 0.78f, 0.35f), 0.5f, gm.Skills.UseHeal);
            rageBtn = MakeSkill(Loc.T("ÖFKE", "RAGE"), UiIcons.Flame, new Color(0.9f, 0.2f, 0.18f), 0.81f, gm.Skills.UseRage);

            BuildIntroCard();
            BuildPausePanel();
            endScreen = gameObject.AddComponent<EndScreen>();
            endScreen.Build(root, gm, pal);
        }

        // ------------------------------------------------------------------ üst bar
        void BuildTopBar()
        {
            // Can barı: koyu hap zemin, geriden eriyen açık iz, renkli dolgu, ortada sayı; solunda kalp rozeti
            var hpLayer = Layer("HpLayer", false);
            var hpBg = Pill(Panel(hpLayer, "HpBar", new Vector2(0.1f, 0.938f), new Vector2(0.56f, 0.964f), new Color(0f, 0f, 0f, 0.62f))).rectTransform;
            hpTrail = Panel(hpBg, "Trail", Vector2.zero, Vector2.one, new Color(1f, 0.92f, 0.75f, 0.85f));
            Pill(hpTrail);
            hpFill = Panel(hpBg, "Fill", Vector2.zero, Vector2.one, new Color(0.3f, 0.85f, 0.35f));
            hpFillImg = Pill(hpFill);
            var shine = Pill(Panel(hpFill, "Shine", new Vector2(0.02f, 0.55f), new Vector2(0.98f, 0.88f), new Color(1f, 1f, 1f, 0.25f)));
            shine.raycastTarget = false;
            hpText = Label(hpBg, "", 32, TextAnchor.MiddleCenter);
            Stretch(hpText.rectTransform, new Vector2(0.08f, 0f), new Vector2(1f, 1f));
            TextShadow(hpText, 3f);
            heart = Badge(hpLayer, "Heart", new Vector2(0.095f, 0.951f), 96f, new Color(0.55f, 0.08f, 0.14f), UiIcons.Heart, new Color(1f, 0.38f, 0.45f));

            // Altın kutusu: para rozeti + sayı
            var goldLayer = Layer("GoldLayer", false);
            goldBox = Panel(goldLayer, "Gold", new Vector2(0.62f, 0.938f), new Vector2(0.84f, 0.964f), new Color(0f, 0f, 0f, 0.62f));
            Pill(goldBox);
            goldText = Label(goldBox, "0", 36, TextAnchor.MiddleCenter);
            Stretch(goldText.rectTransform, new Vector2(0.28f, 0f), new Vector2(0.96f, 1f));
            goldText.color = new Color(1f, 0.86f, 0.32f);
            TextShadow(goldText, 3f);
            goldCoin = Coin(goldLayer, new Vector2(0.625f, 0.951f), 76f);

            // Duraklatma düğmesi: yuvarlak, iki yuvarlak uçlu çubuk
            var pb = Fixed(root, "PauseButton", Vector2.zero, new Vector2(104f, 104f), new Color(0f, 0f, 0f, 0.6f), UiKit.Circle);
            pb.anchorMin = pb.anchorMax = new Vector2(0.92f, 0.951f);
            var pbtn = pb.gameObject.AddComponent<Button>();
            pbtn.targetGraphic = pb.GetComponent<Image>();
            pbtn.onClick.AddListener(Sfx.Click);
            pbtn.onClick.AddListener(gm.Pause);
            for (int i = -1; i <= 1; i += 2)
            {
                var bar = Fixed(pb, "Bar", new Vector2(i * 15f, 0f), new Vector2(18f, 46f), Color.white, null);
                Pill(bar).raycastTarget = false;
            }
            pauseButton = pb.gameObject;

            // Seviye + dünya adı
            levelText = Label(root, gm.Endless ? Loc.T("DALGA 1", "WAVE 1") : Loc.T("SEVİYE ", "LEVEL ") + gm.Level + (gm.IsBossLevel ? "  ·  BOSS" : ""), 38, TextAnchor.MiddleLeft);
            Stretch(levelText.rectTransform, new Vector2(0.05f, 0.903f), new Vector2(0.55f, 0.93f));
            TextShadow(levelText, 3f);
            worldText = Label(root, gm.CurrentWorld != null ? Loc.Upper(Loc.Name(gm.CurrentWorld.displayName)) : "", 26, TextAnchor.MiddleRight);
            Stretch(worldText.rectTransform, new Vector2(0.45f, 0.903f), new Vector2(0.95f, 0.93f));
            worldText.color = pal.label;
            TextShadow(worldText, 3f);

            // İlerleme çubuğu (dünya renginde) + sonunda bayrak / boss kurukafası + kayan işaret
            var progBg = Pill(Panel(Layer("ProgressLayer", false), "Progress", new Vector2(0.05f, 0.888f), new Vector2(0.88f, 0.899f), new Color(0f, 0f, 0f, 0.5f))).rectTransform;
            progressFill = Panel(progBg, "Fill", Vector2.zero, new Vector2(0f, 1f), pal.label);
            Pill(progressFill);
            progressDot = Fixed(progBg, "Dot", Vector2.zero, new Vector2(34f, 34f), Color.white, UiKit.Circle);
            progressDot.anchorMin = progressDot.anchorMax = new Vector2(0f, 0.5f);
            var dotIn = Fixed(progressDot, "In", Vector2.zero, new Vector2(22f, 22f), pal.emblem, UiKit.Circle);
            dotIn.GetComponent<Image>().raycastTarget = false;
            progressDot.GetComponent<Image>().raycastTarget = false;
            bool boss = gm.IsBossLevel || gm.Endless;   // Sonsuz Savunma: çubuk sıradaki boss'a kadar olan yolu gösterir
            Badge(root, "Goal", new Vector2(0.915f, 0.8935f), 58f, boss ? new Color(0.75f, 0.12f, 0.16f) : Color.Lerp(pal.emblem, Color.black, 0.2f),
                boss ? UiIcons.Skull : UiIcons.Flag, Color.white);
        }

        // ------------------------------------------------------------------ boss barı
        void BuildBossBar()
        {
            bossBox = Panel(Layer("BossLayer", false), "BossBar", new Vector2(0.05f, 0.815f), new Vector2(0.95f, 0.872f), Color.clear);
            bossBox.GetComponent<Image>().raycastTarget = false;
            bossBar = bossBox.gameObject;

            // Sol rozet: dünya süsü (alev tacı, buz tacı...) + kırmızı daire + kurukafa
            var badgeHost = Fixed(bossBox, "BadgeHost", Vector2.zero, new Vector2(118f, 118f), Color.clear, null);
            badgeHost.anchorMin = badgeHost.anchorMax = new Vector2(0.065f, 0.5f);
            badgeHost.GetComponent<Image>().raycastTarget = false;
            var deco = Fixed(badgeHost, "Deco", Vector2.zero, new Vector2(176f, 176f), new Color(pal.ring.r, pal.ring.g, pal.ring.b, 0.95f), MenuArt.ThemeBadge(gm.Theme));
            deco.GetComponent<Image>().raycastTarget = false;
            deco.gameObject.AddComponent<SlowSpin>();
            var face = Fixed(badgeHost, "Face", Vector2.zero, new Vector2(104f, 104f), new Color(0.6f, 0.08f, 0.12f), UiKit.Circle);
            face.GetComponent<Image>().raycastTarget = false;
            var sk = Fixed(face, "Skull", Vector2.zero, new Vector2(66f, 66f), new Color(1f, 0.92f, 0.85f), UiIcons.Skull);
            sk.GetComponent<Image>().raycastTarget = false;

            bossText = Label(bossBox, "", 32, TextAnchor.MiddleLeft);
            Stretch(bossText.rectTransform, new Vector2(0.15f, 0.55f), new Vector2(1f, 1.1f));
            bossText.color = new Color(1f, 0.82f, 0.78f);
            TextShadow(bossText, 3f);

            var bg = Pill(Panel(bossBox, "Bg", new Vector2(0.14f, 0.08f), new Vector2(1f, 0.52f), new Color(0.05f, 0.02f, 0.03f, 0.8f))).rectTransform;
            var frame = bg.gameObject.AddComponent<Outline>();
            frame.effectColor = new Color(pal.ring.r, pal.ring.g, pal.ring.b, 0.8f);
            frame.effectDistance = new Vector2(3f, -3f);
            bossTrail = Panel(bg, "Trail", Vector2.zero, Vector2.one, new Color(1f, 0.85f, 0.6f, 0.9f));
            Pill(bossTrail);
            bossFill = Panel(bg, "Fill", Vector2.zero, Vector2.one, new Color(0.88f, 0.14f, 0.18f));
            Pill(bossFill);
            var shine = Pill(Panel(bossFill, "Shine", new Vector2(0.01f, 0.55f), new Vector2(0.99f, 0.88f), new Color(1f, 1f, 1f, 0.22f)));
            shine.raycastTarget = false;
            badgeHost.SetAsLastSibling();
            bossBar.SetActive(false);
        }

        // ------------------------------------------------------------------ duraklatma ve bitiş
        void BuildPausePanel()
        {
            var pp = Panel(root, "PausePanel", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.62f));
            pausePanel = pp.gameObject;
            pauseCard = Panel(pp, "Card", new Vector2(0.09f, 0.33f), new Vector2(0.91f, 0.67f), new Color(0.06f, 0.11f, 0.2f, 0.97f));
            UiIcons.Round(pauseCard, 44f);
            var sh = pauseCard.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.5f);
            sh.effectDistance = new Vector2(0f, -12f);
            Pill(Panel(pauseCard, "Accent", new Vector2(0.3f, 0.93f), new Vector2(0.7f, 0.96f), pal.label)).raycastTarget = false;

            var title = Label(pauseCard, Loc.T("DURAKLATILDI", "PAUSED"), 76, TextAnchor.MiddleCenter);
            Stretch(title.rectTransform, new Vector2(0f, 0.74f), new Vector2(1f, 0.9f));
            TextShadow(title, 5f);
            pauseInfo = Label(pauseCard, "", 32, TextAnchor.MiddleCenter);
            Stretch(pauseInfo.rectTransform, new Vector2(0f, 0.64f), new Vector2(1f, 0.74f));
            pauseInfo.color = pal.label;

            MakeButton(pauseCard, "ResumeButton", new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f), pal.button, Loc.T("DEVAM ET", "RESUME"), 56, gm.Resume);
            MakeButton(pauseCard, "QuitButton", new Vector2(0.18f, 0.17f), new Vector2(0.82f, 0.33f), new Color(0.22f, 0.32f, 0.48f), Loc.T("ANA MENÜ", "MAIN MENU"), 44, gm.ReturnToMenu);
            var warn = Label(pauseCard, Loc.T("Ana menüye dönersen bu bölümde toplanan altın kaybolur.", "If you return to the menu, the gold collected in this level is lost."), 26, TextAnchor.MiddleCenter);
            warn.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(warn.rectTransform, new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.14f));
            warn.color = new Color(1f, 1f, 1f, 0.7f);
            pausePanel.SetActive(false);
        }

        // ------------------------------------------------------------------ yeni güçlendirme kartı
        void BuildIntroCard()
        {
            introLayer = Layer("IntroLayer", false);
            var rim = Panel(introLayer, "IntroCard", new Vector2(0.15f, 0.785f), new Vector2(0.85f, 0.876f), BuffInfo.Gold);   // ilerleme çubuğunun hemen altında, küçük
            introCard = rim;
            introRim = UiIcons.Round(rim.GetComponent<Image>(), 26f);
            var sh = rim.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.45f);
            sh.effectDistance = new Vector2(0f, -6f);
            var inner = Panel(rim, "Inner", Vector2.zero, Vector2.one, new Color(0.05f, 0.09f, 0.16f, 0.96f));
            inner.offsetMin = new Vector2(5f, 5f);
            inner.offsetMax = new Vector2(-5f, -5f);
            UiIcons.Round(inner.GetComponent<Image>(), 22f);

            introBadge = Fixed(inner, "Badge", Vector2.zero, new Vector2(124f, 124f), BuffInfo.Gold, UiKit.Circle);
            introBadge.anchorMin = introBadge.anchorMax = new Vector2(0.115f, 0.5f);
            introBadgeImg = introBadge.GetComponent<Image>();
            var bin = Fixed(introBadge, "In", Vector2.zero, new Vector2(107f, 107f), new Color(0.03f, 0.05f, 0.1f), UiKit.Circle);
            introIcon = Fixed(bin, "Icon", Vector2.zero, new Vector2(72f, 72f), Color.white, UiIcons.Bolt).GetComponent<Image>();

            var tag = Label(inner, Loc.T("YENİ GÜÇLENDİRME", "NEW POWER-UP"), 22, TextAnchor.MiddleLeft);
            tag.color = new Color(1f, 0.85f, 0.35f);
            Stretch(tag.rectTransform, new Vector2(0.235f, 0.75f), new Vector2(0.98f, 0.96f));
            introTitle = Label(inner, "", 40, TextAnchor.MiddleLeft);
            Stretch(introTitle.rectTransform, new Vector2(0.235f, 0.46f), new Vector2(0.98f, 0.78f));
            TextShadow(introTitle, 3f);
            introDesc = Label(inner, "", 25, TextAnchor.UpperLeft);
            introDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
            introDesc.color = new Color(0.85f, 0.9f, 0.97f);
            Stretch(introDesc.rectTransform, new Vector2(0.235f, 0.02f), new Vector2(0.97f, 0.47f));
            introDescTag = tag;
            introLayer.gameObject.SetActive(false);
        }

        Text introDescTag;

        /// <summary>Yeni bir kapı türünü kısaca anlatır, oyun kısa süre yavaşlar. Kart zaten açıksa false.</summary>
        public bool ShowBuffIntro(BuffInfo.Entry e)
        {
            if (introT > 0f || gm == null || !gm.Playing) return false;
            introIcon.sprite = e.icon();
            introIcon.color = (e.key == BuffInfo.Negative.key || e.key == BuffInfo.WeakGate.key) ? new Color(1f, 0.5f, 0.48f) : Color.white;
            introBadgeImg.color = e.color;
            introRim.color = e.color;
            introTitle.text = e.title;
            introTitle.color = Color.Lerp(e.color, Color.white, 0.35f);
            introDesc.text = e.desc;
            introDescTag.text = (e.key == BuffInfo.Negative.key || e.key == BuffInfo.WeakGate.key) ? Loc.T("DİKKAT!", "WARNING!") : Loc.T("YENİ GÜÇLENDİRME", "NEW POWER-UP");
            introDescTag.color = (e.key == BuffInfo.Negative.key || e.key == BuffInfo.WeakGate.key) ? new Color(1f, 0.55f, 0.5f) : new Color(1f, 0.85f, 0.35f);
            introT = IntroTime;
            introInT = 0f;
            introLayer.gameObject.SetActive(true);
            Sfx.Click();
            return true;
        }

        void HideIntro()
        {
            if (introT > 0f && Mathf.Abs(Time.timeScale - 1f) > 0.001f && Time.timeScale > 0f) Time.timeScale = 1f;
            introT = 0f;
            if (introLayer != null) introLayer.gameObject.SetActive(false);
        }

        void UpdateIntro(float dt)
        {
            if (introT <= 0f || !gm.Playing) return;   // duraklatılmışken bekler
            introT -= dt;
            if (introT <= 0f) { Time.timeScale = 1f; introLayer.gameObject.SetActive(false); return; }
            // ilk ve son anlarda yumuşak geçiş: hızlı → yavaş → hızlı
            float elapsed = IntroTime - introT;
            float k = Mathf.Min(Mathf.Clamp01(elapsed / 0.25f), Mathf.Clamp01(introT / 0.45f));
            Time.timeScale = Mathf.Lerp(1f, IntroSlow, k);
            introInT = Mathf.Min(1f, introInT + dt / 0.25f);
            float t = introInT - 1f;
            float sc = 1f + 1.7f * t * t * t + 0.7f * t * t;
            float outK = Mathf.Clamp01(introT / 0.3f);
            introCard.localScale = Vector3.one * (sc * (0.9f + 0.1f * outK));
            introBadge.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(elapsed * 8f));
        }

        public void SetVisible(bool visible)
        {
            if (root != null) root.gameObject.SetActive(visible);
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        // ------------------------------------------------------------------ yetenek düğmeleri
        SkillButton MakeSkill(string title, Sprite iconSprite, Color color, float x, UnityEngine.Events.UnityAction onClick)
        {
            var sb = new SkillButton { color = color };
            var layer = Layer("SkillLayer_" + title, true);
            // hazır olunca parlayan dış halka
            sb.glow = Fixed(layer, "Glow_" + title, Vector2.zero, new Vector2(236f, 236f), new Color(color.r, color.g, color.b, 0f), UiIcons.Spark);
            sb.glow.anchorMin = sb.glow.anchorMax = new Vector2(x, 0.088f);
            sb.glowImg = sb.glow.GetComponent<Image>();
            sb.glowImg.raycastTarget = false;

            sb.rt = Fixed(layer, "Skill_" + title, Vector2.zero, new Vector2(184f, 184f), Color.Lerp(color, Color.white, 0.25f), UiKit.Circle);
            sb.rt.anchorMin = sb.rt.anchorMax = new Vector2(x, 0.088f);
            var ring = sb.rt.GetComponent<Image>();
            var sh = sb.rt.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.45f);
            sh.effectDistance = new Vector2(0f, -8f);
            sb.bg = Fixed(sb.rt, "Bg", Vector2.zero, new Vector2(160f, 160f), color, UiKit.Circle).GetComponent<Image>();
            sb.bg.raycastTarget = false;
            var gloss = Fixed(sb.rt, "Gloss", new Vector2(0f, 34f), new Vector2(118f, 70f), new Color(1f, 1f, 1f, 0.16f), UiKit.Circle);
            gloss.GetComponent<Image>().raycastTarget = false;
            sb.icon = Fixed(sb.rt, "Icon", new Vector2(0f, 8f), new Vector2(96f, 96f), Color.white, iconSprite).GetComponent<Image>();
            sb.icon.raycastTarget = false;
            // saat gibi dolan bekleme katmanı
            sb.cooldown = Fixed(sb.rt, "Cooldown", Vector2.zero, new Vector2(184f, 184f), new Color(0f, 0f, 0f, 0.6f), UiKit.Circle).GetComponent<Image>();
            sb.cooldown.type = Image.Type.Filled;
            sb.cooldown.fillMethod = Image.FillMethod.Radial360;
            sb.cooldown.fillOrigin = (int)Image.Origin360.Top;
            sb.cooldown.fillClockwise = false;
            sb.cooldown.raycastTarget = false;
            sb.sub = Label(sb.rt, "", 30, TextAnchor.MiddleCenter);
            Stretch(sb.sub.rectTransform, new Vector2(0f, 0.08f), new Vector2(1f, 0.34f));
            var so = sb.sub.gameObject.AddComponent<Outline>();
            so.effectColor = new Color(0f, 0f, 0f, 0.7f);
            so.effectDistance = new Vector2(2f, -2f);

            sb.button = sb.rt.gameObject.AddComponent<Button>();
            sb.button.targetGraphic = ring;
            sb.button.transition = Selectable.Transition.None;
            sb.button.onClick.AddListener(onClick);
            sb.button.onClick.AddListener(() => sb.popT = 0.22f);

            var name = Label(layer, title, 26, TextAnchor.MiddleCenter);
            name.rectTransform.anchorMin = new Vector2(x - 0.15f, 0.008f);
            name.rectTransform.anchorMax = new Vector2(x + 0.15f, 0.034f);
            name.rectTransform.offsetMin = name.rectTransform.offsetMax = Vector2.zero;
            TextShadow(name, 3f);
            sb.name = name;
            return sb;
        }

        /// <summary>mode: 0 boş yazı, 1 "n sn", 2 "n hak", 3 "AKTİF". Yazı yalnızca değişince kurulur (her karede metin + çöp üretmesin).</summary>
        void UpdateSkill(SkillButton b, float k, int mode, int value, bool empty, bool active, float now, float dt)
        {
            k = Mathf.Clamp01(k);
            if (Mathf.Abs(b.cooldown.fillAmount - k) > 0.004f || (k == 0f && b.cooldown.fillAmount != 0f)) b.cooldown.fillAmount = k;
            int key = mode * 100000 + value;
            if (key != b.subKey)
            {
                b.subKey = key;
                b.sub.text = mode == 1 ? value + Loc.T(" sn", " s") : mode == 2 ? value + Loc.T(" hak", " left") : mode == 3 ? Loc.T("AKTİF", "ACTIVE") : "";
            }
            bool ready = k <= 0.001f && !empty;
            // hazır olduğu an: zıplama
            if (ready && b.prevK > 0.001f) b.popT = 0.35f;
            b.prevK = k;
            if (empty != b.empty)
            {
                b.empty = empty;
                b.bg.color = empty ? new Color(0.3f, 0.32f, 0.36f) : b.color;
            }
            float ia = ready || active ? 1f : 0.55f;
            if (ia != b.iconA) { b.iconA = ia; b.icon.color = new Color(1f, 1f, 1f, ia); }

            float scale = 1f;
            if (b.popT > 0f)
            {
                b.popT -= dt;
                scale += 0.14f * Mathf.Sin(Mathf.Clamp01(1f - b.popT / 0.35f) * Mathf.PI);
            }
            else if (ready) scale += 0.035f * Mathf.Sin(now * 5f);
            if (Mathf.Abs(scale - b.scale) > 0.002f) { b.scale = scale; b.rt.localScale = Vector3.one * scale; }

            float glowA = active ? 0.55f + 0.25f * Mathf.Sin(now * 9f) : ready ? 0.3f + 0.15f * Mathf.Sin(now * 4f) : 0f;
            if (Mathf.Abs(glowA - b.glowA) > 0.01f)
            {
                b.glowA = glowA;
                var gc = b.glowImg.color;
                gc.a = glowA;
                b.glowImg.color = gc;
                b.glow.gameObject.SetActive(glowA > 0.005f);
                b.glow.localScale = Vector3.one * (active ? 1.1f + 0.06f * Mathf.Sin(now * 9f) : scale);
            }
        }

        // ------------------------------------------------------------------ durum değişiklikleri
        /// <summary>Duraklatma panelini açar/kapatır.</summary>
        public void ShowPause(bool show)
        {
            if (pausePanel != null) pausePanel.SetActive(show);
            if (pauseButton != null) pauseButton.SetActive(!show);
            if (show)
            {
                pauseInT = 0f;
                pauseInfo.text = (gm.Endless ? Loc.T("DALGA ", "WAVE ") + gm.Director.Wave : Loc.T("SEVİYE ", "LEVEL ") + gm.Level) + Loc.T("  ·  ALTIN ", "  ·  GOLD ") + gm.RunGold;
            }
        }

        public void Toast(string msg, bool positive)
        {
            toastText.text = msg;
            toastText.color = positive ? new Color(0.55f, 0.9f, 1f) : new Color(1f, 0.42f, 0.4f);
            toastT = ToastTime;
            toastPopT = 0.2f;
        }

        /// <summary>Uzun / iki satırlık duyuru (ör. Komutan desteği): daha küçük yazı, daha uzun süre ekranda.</summary>
        public void ToastLong(string msg, Color color, int fontSize, float time)
        {
            Toast(msg, true);
            toastText.color = color;
            toastText.fontSize = fontSize;
            toastT = time;
        }

        /// <summary>Sonsuz Savunma: üst çubuktaki dalga ve diyar adı.</summary>
        /// <summary>Sonsuz Savunma üst çubuğu: "DALGA 2" ya da boss aşamasında "DALGA 3 · BOSS".</summary>
        public void SetEndlessInfo(int wave, int sub, int subCount, bool boss, string worldName)
        {
            levelText.text = Loc.T("DALGA ", "WAVE ") + wave + (boss ? "  ·  BOSS" : "");
            worldText.text = worldName;
        }

        public void HideBoss() => bossBar.SetActive(false);

        Image flash;
        /// <summary>Diyar geçişinde tam ekran beyaz parlama (0 = kapalı).</summary>
        public void SetFlash(float a)
        {
            if (flash == null)
            {
                if (a <= 0f) return;
                flash = Panel(Layer("FlashLayer", false), "Flash", Vector2.zero, Vector2.one, new Color(1f, 0.97f, 0.9f, 0f)).GetComponent<Image>();
                flash.raycastTarget = false;
            }
            flash.color = new Color(1f, 0.97f, 0.9f, a);
            flash.gameObject.SetActive(a > 0.001f);
        }

        public void ShowBoss(string name)
        {
            bossText.text = Loc.Upper(name);
            bossBar.SetActive(true);
            bossInT = 0f;
            bossTrailK = 1f;
            lastBossK = 1f;
            Toast(Loc.T("BOSS GELİYOR!", "BOSS INCOMING!"), false);
            toastText.fontSize = 84;
        }

        public void ShowEnd(bool won, int reward, bool replay)
        {
            bossBar.SetActive(false);
            if (pauseButton != null) pauseButton.SetActive(false);
            if (toastT > 0f) { toastT = 0f; toastText.text = ""; }
            HideIntro();
            // yetenek düğmeleri bitiş ekranının altında görünmesin
            foreach (var b in new[] { aoeBtn, healBtn, rageBtn })
            {
                b.rt.gameObject.SetActive(false);
                b.glow.gameObject.SetActive(false);
                b.name.gameObject.SetActive(false);
            }
            endScreen.Show(won, reward, replay);
        }

        // ------------------------------------------------------------------ her kare
        void Update()
        {
            if (gm == null) return;
            // Android geri tuşu (klavyede Esc): oyunu duraklat / devam ettir
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && (gm.Playing || gm.Paused)) gm.TogglePause();

            float dt = Time.unscaledDeltaTime;
            float gdt = Time.deltaTime;
            float now = Time.unscaledTime;

            // --- can ---
            var p = gm.Player;
            float hpK = p.MaxHp > 0 ? Mathf.Clamp01(p.Hp / p.MaxHp) : 0f;
            if (lastHp >= 0f && p.Hp < lastHp - 0.01f)
            {
                // hasar: kenar kızarır (darbe büyüdükçe daha çok), kalp zıplar, iz bir süre bekleyip erir
                float frac = (lastHp - p.Hp) / Mathf.Max(1f, p.MaxHp);
                vignetteA = Mathf.Max(vignetteA, Mathf.Clamp(0.25f + frac * 4f, 0.25f, 0.7f));
                vignetteC = new Color(1f, 0.1f, 0.08f);
                heartPopT = 0.25f;
                hpTrailHold = 0.35f;
            }
            else if (lastHp >= 0f && p.Hp > lastHp + 0.5f)
            {
                vignetteA = Mathf.Max(vignetteA, 0.3f);
                vignetteC = new Color(0.3f, 1f, 0.45f);
                heartPopT = 0.3f;
            }
            lastHp = p.Hp;
            if (Mathf.Abs(hpFill.anchorMax.x - hpK) > 0.0005f) hpFill.anchorMax = new Vector2(hpK, 1f);
            hpFillImg.color = Color.Lerp(new Color(0.92f, 0.22f, 0.2f), new Color(0.32f, 0.86f, 0.36f), Mathf.Clamp01(hpK * 1.3f - 0.1f));
            if (hpTrailK < hpK) hpTrailK = hpK;
            else if (hpTrailHold > 0f) hpTrailHold -= gdt;
            else hpTrailK = Mathf.MoveTowards(hpTrailK, hpK, gdt * 0.7f);
            if (Mathf.Abs(hpTrail.anchorMax.x - hpTrailK) > 0.0005f) hpTrail.anchorMax = new Vector2(hpTrailK, 1f);
            int hpI = Mathf.CeilToInt(p.Hp), maxI = Mathf.CeilToInt(p.MaxHp);
            if (hpI != shownHp || maxI != shownMax) { shownHp = hpI; shownMax = maxI; hpText.text = hpI + " / " + maxI; }
            float hs = 1f;
            if (heartPopT > 0f) { heartPopT -= dt; hs += 0.25f * Mathf.Sin(Mathf.Clamp01(1f - heartPopT / 0.25f) * Mathf.PI); }
            else if (hpK < 0.3f) hs += 0.08f * Mathf.Abs(Mathf.Sin(now * 6f));       // can azken kalp atar
            if (Mathf.Abs(heart.localScale.x - hs) > 0.002f) heart.localScale = Vector3.one * hs;

            if (vignetteA > 0.001f)
            {
                if (!vignette.gameObject.activeSelf) vignette.gameObject.SetActive(true);
                vignetteA = Mathf.MoveTowards(vignetteA, 0f, dt * 1.4f);
                vignette.color = new Color(vignetteC.r, vignetteC.g, vignetteC.b, vignetteA);
            }
            else if (vignette.gameObject.activeSelf) vignette.gameObject.SetActive(false);

            // --- altın ---
            int gold = gm.RunGold;
            if (gold > lastGold) goldPopT = 0.25f;
            lastGold = gold;
            if (goldShown != gold) goldShown = gold > goldShown ? Mathf.Min(gold, goldShown + Mathf.Max(1, Mathf.CeilToInt((gold - goldShown) * 0.25f))) : gold;
            if (goldShown != goldTextValue) { goldTextValue = goldShown; goldText.text = goldShown.ToString(); }
            float gs = 1f;
            if (goldPopT > 0f) { goldPopT -= dt; gs += 0.22f * Mathf.Sin(Mathf.Clamp01(1f - goldPopT / 0.25f) * Mathf.PI); }
            if (Mathf.Abs(goldCoin.localScale.x - gs) > 0.002f)
            {
                goldCoin.localScale = Vector3.one * gs;
                goldText.rectTransform.localScale = Vector3.one * (1f + (gs - 1f) * 0.6f);
            }

            // --- seviye / ilerleme ---
            // düşman vuruldukça ilerler; küçük sıçramalar yumuşak dolsun
            float target = Mathf.Clamp01(gm.Director.Progress);
            progShown = target < progShown - 0.2f ? target : Mathf.MoveTowards(progShown, target, Mathf.Max(0.02f, Mathf.Abs(target - progShown) * 6f) * dt);
            float prog = progShown;
            if (Mathf.Abs(progressFill.anchorMax.x - prog) > 0.001f)
            {
                progressFill.anchorMax = new Vector2(prog, 1f);
                progressDot.anchorMin = progressDot.anchorMax = new Vector2(prog, 0.5f);
            }

            // --- boss ---
            var boss = gm.Director.Boss;
            if (bossBar.activeSelf)
            {
                if (bossInT < 1f)
                {
                    // yukarıdan hafif sekerek kayar
                    bossInT = Mathf.Min(1f, bossInT + gdt / 0.5f);
                    float t = bossInT - 1f;
                    float e = 1f + 2.2f * t * t * t + 1.2f * t * t;
                    bossBox.anchoredPosition = new Vector2(0f, (1f - e) * 260f);
                }
                if (boss != null && boss.Enemy != null)
                {
                    float bk = Mathf.Clamp01(boss.Enemy.hp / boss.Enemy.maxHp);
                    if (bk < lastBossK - 0.0001f) bossTrailHold = 0.3f;
                    lastBossK = bk;
                    bossFill.anchorMax = new Vector2(bk, 1f);
                    if (bossTrailHold > 0f) bossTrailHold -= gdt;
                    else bossTrailK = Mathf.MoveTowards(bossTrailK, bk, gdt * 0.5f);
                    if (bossTrailK < bk) bossTrailK = bk;
                    bossTrail.anchorMax = new Vector2(bossTrailK, 1f);
                }
            }

            // --- yetenekler ---
            var s = gm.Skills;
            UpdateSkill(aoeBtn, s.AoeT / s.aoeCooldown, s.AoeT > 0 ? 1 : 0, Mathf.CeilToInt(s.AoeT), false, false, now, dt);
            bool healEmpty = s.HealLeft <= 0;
            float healK = healEmpty ? 0f : s.HealT / s.healGap;
            UpdateSkill(healBtn, healK, 2, s.HealLeft, healEmpty, false, now, dt);
            bool rageActive = s.RageActiveT > 0;
            UpdateSkill(rageBtn, rageActive ? 0f : s.RageT / s.rageCooldown, rageActive ? 3 : (s.RageT > 0 ? 1 : 0), Mathf.CeilToInt(s.RageT), false, rageActive, now, dt);

            UpdateIntro(dt);

            // --- bildirim: büyüyerek gelir, sonda söner ---
            if (toastT > 0f)
            {
                toastT -= gdt;
                float sc = 1f;
                if (toastPopT > 0f) { toastPopT -= gdt; float k = Mathf.Clamp01(toastPopT / 0.2f); sc = 1f + 0.45f * k * k; }
                toastText.rectTransform.localScale = Vector3.one * sc;
                var c = toastText.color;
                c.a = Mathf.Clamp01(toastT / 0.35f);
                toastText.color = c;
                if (toastT <= 0f) toastText.fontSize = 64;
            }

            // --- duraklatma kartı (zaman durduğu için gerçek zamanla) ---
            if (pausePanel.activeSelf && pauseInT < 1f)
            {
                pauseInT = Mathf.Min(1f, pauseInT + dt / 0.22f);
                float t = pauseInT - 1f;
                pauseCard.localScale = Vector3.one * (1f + 1.7f * t * t * t + 0.7f * t * t);
            }
        }

        // ------------------------------------------------------------------ yardımcılar
        internal static void MakeButton(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Color color, string text, int size, UnityEngine.Events.UnityAction onClick)
            => MakeButton(parent, name, aMin, aMax, color, text, size, onClick, out _);

        internal static void MakeButton(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Color color, string text, int size, UnityEngine.Events.UnityAction onClick, out Text label)
        {
            var rt = Panel(parent, name, aMin, aMax, color);
            var img = UiIcons.MakePill(rt.GetComponent<Image>());
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(Sfx.Click);
            btn.onClick.AddListener(onClick);
            var sh = rt.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.4f);
            sh.effectDistance = new Vector2(0f, -8f);
            var gloss = Pill(Panel(rt, "Gloss", new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.9f), new Color(1f, 1f, 1f, 0.14f)));
            gloss.raycastTarget = false;
            label = Label(rt, text, size, TextAnchor.MiddleCenter);
            TextShadow(label, 4f);
        }

        internal static RectTransform Badge(RectTransform parent, string name, Vector2 anchor, float size, Color bg, Sprite icon, Color iconColor)
        {
            var b = Fixed(parent, name, Vector2.zero, new Vector2(size, size), bg, UiKit.Circle);
            b.anchorMin = b.anchorMax = anchor;
            b.GetComponent<Image>().raycastTarget = false;
            var sh = b.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.4f);
            sh.effectDistance = new Vector2(0f, -4f);
            var rim = Fixed(b, "Rim", Vector2.zero, new Vector2(size * 0.86f, size * 0.86f), Color.Lerp(bg, Color.white, 0.12f), UiKit.Circle);
            rim.GetComponent<Image>().raycastTarget = false;
            var ic = Fixed(b, "Icon", Vector2.zero, new Vector2(size * 0.58f, size * 0.58f), iconColor, icon);
            ic.GetComponent<Image>().raycastTarget = false;
            return b;
        }

        internal static RectTransform Coin(RectTransform parent, Vector2 anchor, float size)
        {
            var c = Fixed(parent, "Coin", Vector2.zero, new Vector2(size, size), new Color(0.78f, 0.5f, 0.08f), UiIcons.Coin);
            c.anchorMin = c.anchorMax = anchor;
            c.GetComponent<Image>().raycastTarget = false;
            var face = Fixed(c, "Face", Vector2.zero, new Vector2(size, size), new Color(1f, 0.86f, 0.32f), UiIcons.CoinFace);
            face.GetComponent<Image>().raycastTarget = false;
            return c;
        }

        /// <summary>
        /// Alt katman: kendi Canvas'ı olan tam ekran kap. İçindeki öğe her karede değişse (nabız, kayan işaret, sayı)
        /// yalnızca bu küçük katman yeniden hesaplanır; tüm arayüz (gölgeli/dış çizgili yazılar) her karede baştan kurulmaz.
        /// </summary>
        RectTransform Layer(string name, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(root, false);
            Stretch(rt, Vector2.zero, Vector2.one);
            go.AddComponent<Canvas>();
            if (raycast) go.AddComponent<GraphicRaycaster>();
            return rt;
        }

        internal static Image Pill(RectTransform rt) => UiIcons.MakePill(rt.GetComponent<Image>());

        /// <summary>Yazının çevresine koyu çizgi (karlı / parlak 3B zeminde de okunsun).</summary>
        internal static void TextShadow(Text t, float dist)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.65f);
            o.effectDistance = new Vector2(Mathf.Min(3f, dist * 0.6f), -Mathf.Min(3f, dist * 0.6f));
        }

        internal static RectTransform Panel(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt, aMin, aMax);
            go.GetComponent<Image>().color = color;
            return rt;
        }

        internal static RectTransform Fixed(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite)
        {
            var rt = Panel(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), color);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            if (sprite != null) rt.GetComponent<Image>().sprite = sprite;
            return rt;
        }

        internal static Text Label(RectTransform parent, string text, int size, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt, Vector2.zero, Vector2.one);
            var t = go.GetComponent<Text>();
            var f = UiKit.MenuFont;
            t.font = f != null ? f : Mats.Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = f != null ? FontStyle.Normal : FontStyle.Bold;
            t.alignment = anchor;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        internal static void Stretch(RectTransform rt, Vector2 aMin, Vector2 aMax)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Boss rozetindeki dünya süsünü yavaşça döndürür (oyun zamanıyla: duraklatınca durur).</summary>
    public class SlowSpin : MonoBehaviour
    {
        public float speed = 12f;
        void Update() => transform.Rotate(0f, 0f, speed * Time.deltaTime);
    }
}
