using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StratejiGame
{
    /// <summary>
    /// Ana menü sahnesi. Altta sekme çubuğu: Yükseltmeler · Savaş · Seviyeler · Profil (istatistikler + ayarlar).
    /// Seviyeler sekmesinde her dünya için 10 seviyelik yol haritası vardır:
    /// geçilen seviyeler buğulu + tikli, sıradaki parlayan, oynanmamışlar kilitli.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        enum Tab { Upgrades = 0, Battle = 1, Levels = 2, Profile = 3, Settings = 4 }
        const int TabCount = 5;

        GameDatabase db;
        UpgradeConfig cfg;
        RectTransform root, content;
        readonly RectTransform[] pages = new RectTransform[TabCount];

        // Sekme çubuğu: 4 küçük sekme (ikon + yazı) + ortada çubuktan taşan büyük SAVAŞ düğmesi
        class TabVisual { public Tab tab; public float x; public Image icon; public Text label; public RectTransform iconRt; public float pop; }
        readonly List<TabVisual> tabVisuals = new();
        RectTransform tabHighlight, battleBtn, battleGlow, upgradeBadge;
        Image battleBtnImg, tabHighlightImg;
        Text upgradeBadgeText;
        float highlightX = -1f, highlightA, battlePop;
        static readonly Color TabIconOff = new Color(0.52f, 0.6f, 0.72f);
        Tab current = Tab.Battle;
        Text goldText, topLevelText, toastText;
        float toastT;

        // Savaş sayfası
        Text bWorld, bLevelNum, bLevel, bTag, bInfo, bBtnText;
        GameObject endlessBtn;
        Image endlessImg;
        RectTransform endlessLock;
        Text endlessSub;

        void OnEndless()
        {
            if (GameSession.EndlessUnlocked) GameSession.PlayEndless();
            else Toast(Loc.T("İlk diyarın boss'unu yenince açılır!", "Defeat the first land's boss to unlock!"), false);
        }
        Button bBtn;
        Image bEmblem, bRing, bBtnGloss;
        RectTransform bDeco;
        MenuThemeView themeView;
        RawImage menuBgRaw;

        // Yükseltmeler
        readonly List<Row> rows = new();

        class Row
        {
            public UpgradeId id;
            public Color color;
            public RectTransform rt, btnRt, barFill, coinRt;
            public Text name, desc, level, cost;
            public Button button;
            public Image buttonImage, flash, badge, gloss;
            public Image barBgImg, barFillImg, barShineImg;
            public bool affordable, maxed;
            public float barCur = -1f, barTarget;
            public float punchT, shakeT, popT, flashT, enterDelay;
        }

        // Yükseltme efektleri
        RectTransform fxLayer, goldCoin;
        float goldShown = -1f;
        int goldTarget;
        float goldAnimFrom, goldAnimT = 1f;
        float upgradesEnterT;
        class Particle
        {
            public RectTransform rt;
            public Image img;
            public Vector2 vel, from, to;
            public float t, life, spin, size;
            public bool coin;
        }
        readonly List<Particle> particles = new();
        readonly List<Particle> particlePool = new();

        // Seviyeler
        int worldPage;
        Text wTitle, wSub;
        RectTransform map;
        RectTransform pulseNode;
        Vector2 swipeStart;
        bool swipeTracking;

        static readonly Color Bg = new Color(0.04f, 0.1f, 0.18f);
        static readonly Color TabOff = new Color(0.07f, 0.13f, 0.22f);
        static readonly Color TabOn = new Color(0.9f, 0.45f, 0.1f);
        static readonly Color GoldC = new Color(1f, 0.85f, 0.3f);
        static readonly Color RowBg = new Color(0.07f, 0.14f, 0.24f, 0.95f);
        static readonly Color BuyOn = new Color(0.2f, 0.7f, 0.3f);
        static readonly Color BuyOff = new Color(0.3f, 0.3f, 0.33f);
        static readonly Color NodeDone = new Color(0.3f, 0.55f, 0.8f);
        static readonly Color NodeNext = new Color(1f, 0.6f, 0.1f);
        static readonly Color NodeLocked = new Color(0.25f, 0.28f, 0.33f);
        static readonly Color NodeBoss = new Color(0.85f, 0.2f, 0.25f);
        static readonly Color MaxedC = new Color(0.62f, 0.45f, 0.12f);

        void Awake()
        {
            AppBoot.ApplyFrameRate();
            SaveSystem.Load();
            db = GameDatabase.Load();
            GameSession.NormalizeProgress();
            cfg = db != null ? db.upgrades : null;
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<UpgradeConfig>();
                cfg.ResetToDefaults();
            }
            int slots = db != null ? db.WorldSlotCount : 1;
            worldPage = CurrentWorldPage();
            UiKit.FontOverride = UiKit.MenuFont;
            Build();
            ShowTab(PendingTab ?? Tab.Battle);
            PendingTab = null;
            // Oyunun ilk açılışı: Komutan Atilla'nın hikaye girişi + eğitim sayfaları
            if (!StoryData.Seen(StoryData.IntroBit))
                StoryDialog.IntroSequence(() => StoryData.MarkSeen(StoryData.IntroBit));
            else
            {
                // zafer ekranında kaçırılan dünya sonu konuşması varsa (ör. hemen sonraki seviyeye geçildiyse) şimdi göster
                int pending = -1;
                for (int w = 0; w < StoryData.WorldCount; w++)
                    if (StoryData.WorldCleared(w) && !StoryData.Seen(StoryData.WorldBit(w))) pending = w;
                if (pending >= 0)
                {
                    for (int w = 0; w <= pending; w++) SaveSystem.Data.storySeen |= 1 << StoryData.WorldBit(w);
                    SaveSystem.Save();
                    StoryDialog.Talk(StoryData.WorldEnd(pending), Loc.T("TERFİ: ", "PROMOTED: ") + StoryData.Rank(pending), ShowMissedChests);
                }
                else ShowMissedChests();
            }
            Sfx.SetTheme(WorldTheme.Underwater);
            Sfx.SetAmbience(true, 0.2f);
        }

        /// <summary>
        /// Temizlenmiş ama terfi sandığı alınmamış diyarlar (sandık sisteminden önce boss yenenler, zafer ekranından
        /// hemen çıkanlar): hepsinin ödülü tek sandıkta verilir.
        /// </summary>
        void ShowMissedChests()
        {
            if (this == null) return;
            int before = SaveSystem.Data.gold;
            int total = PromotionChest.ClaimMissed(out int highest);
            if (total <= 0) { ShowDailyIfReady(); return; }
            PromotionChest.Show(total, PromotionChest.RankLine(highest), () =>
            {
                if (this == null) return;
                StartGoldAnim(before, SaveSystem.Data.gold);
                Refresh();
                ShowDailyIfReady();
            });
        }

        // ------------------------------------------------------------------ günlük ödül / görevler
        GameObject questDot;
        RectTransform questBtnRt;
        float questDotCheckT;

        /// <summary>Bugünün giriş ödülü alınmadıysa Görevler penceresi kendiliğinden açılır.</summary>
        void ShowDailyIfReady()
        {
            if (this == null || !DailyQuests.DailyAvailable) return;
            OpenQuests();
        }

        void OpenQuests()
        {
            if (DailyPanel.IsOpen) return;
            DailyPanel.Open((from, to) =>
            {
                if (this == null) return;
                StartGoldAnim(from, to);
            }, () =>
            {
                if (this == null) return;
                Refresh();
                UpdateQuestDot();
            });
        }

        void BuildQuestButton(RectTransform page)
        {
            Text ql;
            var btn = UiKit.Button(page, "QuestButton", new Vector2(0.04f, 0.945f), new Vector2(0.38f, 0.998f), new Color(0.1f, 0.2f, 0.34f, 0.92f), Loc.T("GÖREVLER", "QUESTS"), 34, OpenQuests, out ql);
            UiIcons.MakePill(btn.GetComponent<Image>());
            UiKit.Stretch(ql.rectTransform, new Vector2(0.3f, 0f), new Vector2(0.95f, 1f));
            AddTextShadow(ql, 3f);
            questBtnRt = (RectTransform)btn.transform;
            var ic = UiKit.Panel(questBtnRt, "Icon", new Vector2(0.05f, 0.14f), new Vector2(0.27f, 0.86f), GoldC, UiKit.Circle);
            ic.GetComponent<Image>().preserveAspect = true;
            ic.GetComponent<Image>().raycastTarget = false;
            var st = UiKit.Panel(ic, "Star", new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), Color.white, UiIcons.Star);
            st.GetComponent<Image>().preserveAspect = true;
            st.GetComponent<Image>().raycastTarget = false;
            // alınmayı bekleyen ödül varsa kırmızı nokta
            var dot = UiKit.Fixed(questBtnRt, "Dot", Vector2.zero, new Vector2(34f, 34f), new Color(0.95f, 0.2f, 0.2f), UiKit.Circle);
            dot.anchorMin = dot.anchorMax = new Vector2(0.97f, 0.88f);
            dot.GetComponent<Image>().raycastTarget = false;
            questDot = dot.gameObject;
            UpdateQuestDot();
        }

        void UpdateQuestDot()
        {
            if (questDot != null) questDot.SetActive(DailyQuests.AnyClaimable);
        }

        // ------------------------------------------------------------------ kurulum
        void Build()
        {
            root = UiKit.CreateCanvas(transform, "MenuCanvas", 0);
            // Zemin: yumuşak dikey renk geçişi (tek katman)
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(RawImage));
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.SetParent(root, false);
            UiKit.Stretch(bgRt, Vector2.zero, Vector2.one);
            var bgImg = bgGo.GetComponent<RawImage>();
            bgImg.texture = MenuArt.Gradient("menu", new Color(0.08f, 0.2f, 0.32f), Bg, new Color(0.02f, 0.06f, 0.12f));
            bgImg.raycastTarget = false;
            menuBgRaw = bgImg;

            // Savaş sekmesinin dünya temalı arka planı (yalnızca o sekmede görünür)
            themeView = MenuThemeView.Create(root);

            // Üst bar
            var top = UiKit.Panel(root, "TopBar", new Vector2(0f, 0.935f), Vector2.one, new Color(0f, 0f, 0f, 0.35f));
            topLevelText = UiKit.Label(top, "", 40, TextAnchor.MiddleLeft);
            UiKit.Stretch(topLevelText.rectTransform, new Vector2(0.05f, 0f), new Vector2(0.5f, 1f));
            goldText = UiKit.Label(top, "", 48, TextAnchor.MiddleRight, GoldC);
            UiKit.Stretch(goldText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.95f, 1f));
            goldCoin = MakeCoin(top, 62f);
            goldCoin.anchorMin = goldCoin.anchorMax = new Vector2(0.95f, 0.5f);

            content = UiKit.Panel(root, "Content", new Vector2(0f, 0.115f), new Vector2(1f, 0.935f), Color.clear);
            content.GetComponent<Image>().raycastTarget = false;

            BuildTabBar();

            for (int i = 0; i < TabCount; i++)
            {
                pages[i] = UiKit.Panel(content, "Page_" + (Tab)i, Vector2.zero, Vector2.one, Color.clear);
                pages[i].GetComponent<Image>().raycastTarget = false;
            }
            BuildBattlePage(pages[(int)Tab.Battle]);
            BuildUpgradesPage(pages[(int)Tab.Upgrades]);
            BuildLevelsPage(pages[(int)Tab.Levels]);
            BuildProfilePage(pages[(int)Tab.Profile]);
            BuildSettingsPage(pages[(int)Tab.Settings]);

            toastText = UiKit.Label(root, "", 44, TextAnchor.MiddleCenter);
            UiKit.Stretch(toastText.rectTransform, new Vector2(0f, 0.1f), new Vector2(1f, 0.14f));
            toastText.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);

            // Kıvılcım / uçan altın katmanı (her şeyin üstünde, dokunmaları engellemez)
            fxLayer = UiKit.Panel(root, "FxLayer", Vector2.zero, Vector2.one, Color.clear);
            fxLayer.GetComponent<Image>().raycastTarget = false;

            BuildGuide();
        }

        // ------------------------------------------------------------------ hikaye arşivi
        GameObject archive;

        void OpenArchive()
        {
            if (archive != null) Destroy(archive);
            var layer = UiKit.Panel(root, "Archive", Vector2.zero, Vector2.one, new Color(0f, 0.02f, 0.05f, 0.82f));
            archive = layer.gameObject;
            var bg = layer.gameObject.AddComponent<Button>();
            bg.transition = Selectable.Transition.None;
            bg.onClick.AddListener(CloseArchive);
            var card = UiKit.Panel(layer, "Card", new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.88f), new Color(0.05f, 0.1f, 0.18f, 1f));
            card.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            UiIcons.Round(card, 44f);
            var title = UiKit.Label(card, Loc.T("HİKAYE ARŞİVİ", "STORY ARCHIVE"), 60, TextAnchor.MiddleCenter, GoldC);
            UiKit.Stretch(title.rectTransform, new Vector2(0f, 0.9f), new Vector2(1f, 0.98f));
            AddTextShadow(title, 5f);
            var sub = UiKit.Label(card, StoryData.CommanderName, 28, TextAnchor.MiddleCenter, new Color(0.65f, 0.75f, 0.85f));
            UiKit.Stretch(sub.rectTransform, new Vector2(0f, 0.86f), new Vector2(1f, 0.9f));

            var items = new List<(string title, bool open, System.Action play)>();
            items.Add((Loc.T("Giriş: Gölge İstilası", "Intro: The Shadow Invasion"), true, () => StoryDialog.Talk(StoryData.Intro, "", null)));
            items.Add((Loc.T("Eğitim", "Training"), true, () => StoryDialog.Tutorial(null)));
            for (int w = 0; w < StoryData.WorldCount; w++)
            {
                int wi = w;
                bool open = StoryData.WorldCleared(wi);
                items.Add((open ? StoryData.ArchiveTitle(wi) : Loc.T("Kilitli: ", "Locked: ") + (wi + 1) + Loc.T(". diyarı temizle", ". land not cleared"), open,
                    () => StoryDialog.Talk(StoryData.WorldEnd(wi), Loc.T("TERFİ: ", "PROMOTED: ") + StoryData.Rank(wi), null)));
            }
            float top = 0.84f, h = 0.084f;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                float yMax = top - i * h, yMin = yMax - h + 0.012f;
                var row = UiKit.Panel(card, "Item" + i, new Vector2(0.05f, yMin), new Vector2(0.95f, yMax), it.open ? RowBg : new Color(0.06f, 0.09f, 0.14f, 0.9f));
                UiIcons.Round(row, 24f);
                var icon = UiKit.Panel(row, "Icon", new Vector2(0.025f, 0.15f), new Vector2(0.13f, 0.85f), it.open ? NodeNext : NodeLocked, UiKit.Circle);
                icon.GetComponent<Image>().preserveAspect = true;
                icon.GetComponent<Image>().raycastTarget = false;
                var ii = UiKit.Panel(icon, "I", new Vector2(0.22f, 0.22f), new Vector2(0.78f, 0.78f), Color.white, it.open ? UiIcons.Star : UiKit.Lock);
                ii.GetComponent<Image>().preserveAspect = true;
                ii.GetComponent<Image>().raycastTarget = false;
                var t = UiKit.Label(row, it.title, 34, TextAnchor.MiddleLeft, it.open ? Color.white : new Color(0.55f, 0.6f, 0.68f));
                UiKit.Stretch(t.rectTransform, new Vector2(0.17f, 0f), new Vector2(0.97f, 1f));
                if (it.open)
                {
                    var b = row.gameObject.AddComponent<Button>();
                    b.targetGraphic = row.GetComponent<Image>();
                    var play = it.play;
                    b.onClick.AddListener(Sfx.Click);
                    b.onClick.AddListener(() => play());
                }
            }
            Text cl;
            var close = UiKit.Button(card, "Close", new Vector2(0.25f, 0.02f), new Vector2(0.75f, 0.09f), TabOn, Loc.T("KAPAT", "CLOSE"), 44, CloseArchive, out cl);
            UiIcons.MakePill(close.GetComponent<Image>());
            AddTextShadow(cl, 4f);
        }

        void CloseArchive()
        {
            if (archive != null) Destroy(archive);
            archive = null;
        }

        // ------------------------------------------------------------------ rehber
        GameObject guide;
        RectTransform guideCard;
        ScrollRect guideScroll;
        float guideInT = 1f;

        void BuildGuideButton(RectTransform page)
        {
            Text gl;
            var btn = UiKit.Button(page, "GuideButton", new Vector2(0.62f, 0.945f), new Vector2(0.96f, 0.998f), new Color(0.1f, 0.2f, 0.34f, 0.92f), Loc.T("REHBER", "GUIDE"), 34, OpenGuide, out gl);
            UiIcons.MakePill(btn.GetComponent<Image>());
            UiKit.Stretch(gl.rectTransform, new Vector2(0.3f, 0f), new Vector2(0.95f, 1f));
            AddTextShadow(gl, 3f);
            var q = UiKit.Panel((RectTransform)btn.transform, "Q", new Vector2(0.05f, 0.14f), new Vector2(0.27f, 0.86f), NodeNext, UiKit.Circle);
            q.GetComponent<Image>().preserveAspect = true;
            q.GetComponent<Image>().raycastTarget = false;
            UiKit.Label(q, "?", 36, TextAnchor.MiddleCenter);
        }

        void BuildGuide()
        {
            var layer = UiKit.Panel(root, "Guide", Vector2.zero, Vector2.one, new Color(0f, 0.02f, 0.05f, 0.82f));
            guide = layer.gameObject;
            layer.gameObject.AddComponent<Button>().onClick.AddListener(CloseGuide);   // karta dışından dokunmak kapatır
            layer.GetComponent<Button>().transition = Selectable.Transition.None;

            var card = UiKit.Panel(layer, "Card", new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), new Color(0.05f, 0.1f, 0.18f, 1f));
            guideCard = card;
            card.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // karta dokunmak kapatmasın
            UiIcons.Round(card, 44f);
            var cardSh = card.gameObject.AddComponent<Shadow>();
            cardSh.effectColor = new Color(0f, 0f, 0f, 0.5f);
            cardSh.effectDistance = new Vector2(0f, -12f);

            var title = UiKit.Label(card, Loc.T("REHBER", "GUIDE"), 66, TextAnchor.MiddleCenter, GoldC);
            UiKit.Stretch(title.rectTransform, new Vector2(0f, 0.925f), new Vector2(1f, 0.99f));
            AddTextShadow(title, 5f);
            var sub = UiKit.Label(card, Loc.T("Kapılardaki semboller ve kahramanın yetenekleri", "Gate symbols and the hero's skills"), 28, TextAnchor.MiddleCenter, new Color(0.65f, 0.75f, 0.85f));
            UiKit.Stretch(sub.rectTransform, new Vector2(0f, 0.895f), new Vector2(1f, 0.93f));

            // kaydırılabilir liste
            var view = UiKit.Panel(card, "View", new Vector2(0.03f, 0.115f), new Vector2(0.97f, 0.885f), new Color(1f, 1f, 1f, 0f));
            view.gameObject.AddComponent<RectMask2D>();
            var list = UiKit.Panel(view, "List", new Vector2(0f, 1f), new Vector2(1f, 1f), Color.clear);
            list.GetComponent<Image>().raycastTarget = false;
            list.pivot = new Vector2(0.5f, 1f);
            guideScroll = view.gameObject.AddComponent<ScrollRect>();
            guideScroll.content = list;
            guideScroll.viewport = view;
            guideScroll.horizontal = false;
            guideScroll.movementType = ScrollRect.MovementType.Elastic;
            guideScroll.scrollSensitivity = 30f;

            float y = 0f;
            void Header(string text)
            {
                y += 14f;
                var h = UiKit.Label(list, text, 36, TextAnchor.MiddleLeft, new Color(0.6f, 0.9f, 1f));
                h.rectTransform.anchorMin = new Vector2(0f, 1f);
                h.rectTransform.anchorMax = new Vector2(1f, 1f);
                h.rectTransform.pivot = new Vector2(0.5f, 1f);
                h.rectTransform.offsetMin = new Vector2(16f, 0f);
                h.rectTransform.offsetMax = new Vector2(-16f, 0f);
                h.rectTransform.sizeDelta = new Vector2(-32f, 60f);
                h.rectTransform.anchoredPosition = new Vector2(0f, -y);
                y += 64f;
            }
            void Row(BuffInfo.Entry e, string chip)
            {
                const float H = 164f;
                var row = UiKit.Panel(list, "Row_" + e.key, new Vector2(0f, 1f), new Vector2(1f, 1f), RowBg);
                row.pivot = new Vector2(0.5f, 1f);
                row.sizeDelta = new Vector2(0f, H);
                row.anchoredPosition = new Vector2(0f, -y);
                UiIcons.Round(row, 30f);
                row.GetComponent<Image>().raycastTarget = false;
                // sembol: kapıdaki gibi renkli halka içinde koyu rozet
                var badge = UiKit.Panel(row, "Badge", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), e.color, UiKit.Circle);
                badge.sizeDelta = new Vector2(124f, 124f);
                badge.anchoredPosition = new Vector2(86f, chip != null ? 10f : 0f);
                badge.GetComponent<Image>().raycastTarget = false;
                var inner = UiKit.Fixed(badge, "In", Vector2.zero, new Vector2(106f, 106f), new Color(0.03f, 0.05f, 0.1f), UiKit.Circle);
                inner.GetComponent<Image>().raycastTarget = false;
                var ic = UiKit.Fixed(inner, "Icon", Vector2.zero, new Vector2(72f, 72f), Color.white, e.icon());
                ic.GetComponent<Image>().raycastTarget = false;
                if (chip != null)
                {
                    // kapıda nasıl göründüğü: örnek değer
                    var c = UiKit.Panel(row, "Chip", new Vector2(0f, 0f), new Vector2(0f, 0f), e.color);
                    c.sizeDelta = new Vector2(108f, 40f);
                    c.anchoredPosition = new Vector2(86f, 26f);
                    UiIcons.MakePill(c).raycastTarget = false;
                    var ct = UiKit.Label(c, chip, 28, TextAnchor.MiddleCenter);
                    AddTextShadow(ct, 2f);
                }
                var t = UiKit.Label(row, e.title, 42, TextAnchor.MiddleLeft, Color.Lerp(e.color, Color.white, 0.4f));
                UiKit.Stretch(t.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 0.95f));
                t.rectTransform.offsetMin = new Vector2(176f, 0f);
                t.rectTransform.offsetMax = new Vector2(-16f, 0f);
                AddTextShadow(t, 3f);
                var d = UiKit.Label(row, e.desc, 29, TextAnchor.UpperLeft, new Color(0.8f, 0.86f, 0.93f));
                d.fontStyle = FontStyle.Normal;
                d.horizontalOverflow = HorizontalWrapMode.Wrap;
                UiKit.Stretch(d.rectTransform, new Vector2(0f, 0.04f), new Vector2(1f, 0.56f));
                d.rectTransform.offsetMin = new Vector2(176f, 0f);
                d.rectTransform.offsetMax = new Vector2(-18f, 0f);
                y += H + 14f;
            }

            Header(Loc.T("GÜÇLENDİRME KAPILARI", "POWER-UP GATES"));
            Row(BuffInfo.For(GateType.FireRate), "+20%");
            Row(BuffInfo.For(GateType.Damage), "+25%");
            Row(BuffInfo.For(GateType.ExtraBullet), "+1");
            Row(BuffInfo.For(GateType.Pierce), "+1");
            Row(BuffInfo.For(GateType.Crit), "+10%");
            Header(Loc.T("DİĞER KAPILAR", "OTHER GATES"));
            Row(BuffInfo.For(GateType.Soldiers), "+5");
            Row(BuffInfo.Negative, "-10%");
            Header(Loc.T("SONSUZ SAVUNMA", "ENDLESS DEFENSE"));
            Row(BuffInfo.WeakGate, "-15%");
            Row(BuffInfo.TrapRow, null);
            Row(BuffInfo.Armored, null);
            Header(Loc.T("KAHRAMAN YETENEKLERİ", "HERO SKILLS"));
            foreach (var sk in BuffInfo.Skills) Row(sk, null);
            y += 10f;
            list.sizeDelta = new Vector2(0f, y);

            UiKit.Button(card, "Close", new Vector2(0.25f, 0.02f), new Vector2(0.75f, 0.095f), TabOn, Loc.T("KAPAT", "CLOSE"), 46, CloseGuide, out var cl);
            UiIcons.MakePill(card.Find("Close").GetComponent<Image>());
            AddTextShadow(cl, 4f);
            guide.SetActive(false);
        }

        void OpenGuide()
        {
            guide.SetActive(true);
            guide.transform.SetAsLastSibling();
            guideScroll.verticalNormalizedPosition = 1f;
            guideInT = 0f;
        }

        void CloseGuide()
        {
            if (guide != null) guide.SetActive(false);
        }

        // ------------------------------------------------------------------ sekme çubuğu
        void BuildTabBar()
        {
            var bar = UiKit.Panel(root, "TabBar", Vector2.zero, new Vector2(1f, 0.095f), new Color(0.03f, 0.06f, 0.12f));
            UiKit.Panel(bar, "TopLine", new Vector2(0f, 0.97f), Vector2.one, new Color(1f, 1f, 1f, 0.07f)).GetComponent<Image>().raycastTarget = false;
            // ortadaki düğmenin arkasında çubukla birleşen çentik
            var notch = UiKit.Fixed(bar, "Notch", Vector2.zero, new Vector2(236f, 236f), new Color(0.03f, 0.06f, 0.12f), UiKit.Circle);
            notch.anchorMin = notch.anchorMax = new Vector2(0.5f, 0.62f);
            notch.GetComponent<Image>().raycastTarget = false;

            // kayan vurgu (seçili küçük sekmenin arkasında)
            tabHighlight = UiKit.Panel(bar, "Highlight", new Vector2(0f, 0.1f), new Vector2(0.18f, 0.9f), new Color(TabOn.r, TabOn.g, TabOn.b, 0.22f));
            tabHighlightImg = UiIcons.Round(tabHighlight, 30f);
            tabHighlightImg.raycastTarget = false;

            AddTab(bar, Tab.Upgrades, Loc.T("YÜKSELTME", "UPGRADES"), UiIcons.UpArrow, 0.1f);
            AddTab(bar, Tab.Levels, Loc.T("SEVİYELER", "LEVELS"), UiIcons.Flag, 0.29f);
            AddTab(bar, Tab.Profile, Loc.T("PROFİL", "PROFILE"), UiIcons.PersonIcon, 0.71f);
            AddTab(bar, Tab.Settings, Loc.T("AYARLAR", "SETTINGS"), UiIcons.Gear, 0.9f);

            // Yükseltme rozeti: alınabilecek yükseltme sayısı
            var up = tabVisuals[0];
            upgradeBadge = UiKit.Fixed(up.iconRt, "Badge", new Vector2(34f, 26f), new Vector2(44f, 44f), new Color(0.92f, 0.18f, 0.2f), UiKit.Circle);
            upgradeBadge.GetComponent<Image>().raycastTarget = false;
            var bsh = upgradeBadge.gameObject.AddComponent<Shadow>();
            bsh.effectColor = new Color(0f, 0f, 0f, 0.4f);
            bsh.effectDistance = new Vector2(0f, -3f);
            upgradeBadgeText = UiKit.Label(upgradeBadge, "", 28, TextAnchor.MiddleCenter);
            upgradeBadge.gameObject.SetActive(false);

            // Ortada büyük SAVAŞ düğmesi (çubuktan yukarı taşar, dünya renginde)
            battleGlow = UiKit.Fixed(root, "BattleGlow", Vector2.zero, new Vector2(330f, 330f), new Color(1f, 0.75f, 0.35f, 0.35f), UiIcons.Spark);
            battleGlow.anchorMin = battleGlow.anchorMax = new Vector2(0.5f, 0.072f);
            battleGlow.GetComponent<Image>().raycastTarget = false;
            battleBtn = UiKit.Fixed(root, "BattleTab", Vector2.zero, new Vector2(196f, 196f), new Color(1f, 1f, 1f, 0.9f), UiKit.Circle);
            battleBtn.anchorMin = battleBtn.anchorMax = new Vector2(0.5f, 0.072f);
            var ring = battleBtn.GetComponent<Image>();
            var sh = battleBtn.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.45f);
            sh.effectDistance = new Vector2(0f, -8f);
            battleBtnImg = UiKit.Fixed(battleBtn, "Face", Vector2.zero, new Vector2(176f, 176f), TabOn, UiKit.Circle).GetComponent<Image>();
            battleBtnImg.raycastTarget = false;
            var gloss = UiKit.Fixed(battleBtn, "Gloss", new Vector2(0f, 40f), new Vector2(124f, 68f), new Color(1f, 1f, 1f, 0.18f), UiKit.Circle);
            gloss.GetComponent<Image>().raycastTarget = false;
            var sw = UiKit.Fixed(battleBtn, "Swords", new Vector2(0f, 16f), new Vector2(92f, 92f), Color.white, UiIcons.Swords);
            sw.GetComponent<Image>().raycastTarget = false;
            var bl = UiKit.Label(battleBtn, Loc.T("SAVAŞ", "BATTLE"), 30, TextAnchor.MiddleCenter);
            UiKit.Stretch(bl.rectTransform, new Vector2(0f, 0.1f), new Vector2(1f, 0.34f));
            AddTextShadow(bl, 3f);
            var btn = battleBtn.gameObject.AddComponent<Button>();
            btn.targetGraphic = ring;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(Sfx.Click);
            btn.onClick.AddListener(() => ShowTab(Tab.Battle));
        }

        void AddTab(RectTransform bar, Tab tab, string name, Sprite icon, float x)
        {
            var b = UiKit.Button(bar, "Tab_" + tab, new Vector2(x - 0.09f, 0.04f), new Vector2(x + 0.09f, 0.96f), Color.clear, "", 26, () => ShowTab(tab), out var label);
            b.transition = Selectable.Transition.None;
            label.text = name;
            UiKit.Stretch(label.rectTransform, new Vector2(0f, 0.06f), new Vector2(1f, 0.36f));
            var ic = UiKit.Fixed((RectTransform)b.transform, "Icon", new Vector2(0f, 22f), new Vector2(66f, 66f), TabIconOff, icon);
            ic.GetComponent<Image>().raycastTarget = false;
            tabVisuals.Add(new TabVisual { tab = tab, x = x, icon = ic.GetComponent<Image>(), label = label, iconRt = ic });
        }

        void UpdateTabBar(float dt, float now)
        {
            // vurgu seçili sekmeye kayar; Savaş seçiliyken söner
            TabVisual sel = null;
            foreach (var tv in tabVisuals) if (tv.tab == current) sel = tv;
            if (sel != null)
            {
                if (highlightX < 0f) highlightX = sel.x;
                highlightX = Mathf.Lerp(highlightX, sel.x, 1f - Mathf.Exp(-14f * dt));
            }
            highlightA = Mathf.MoveTowards(highlightA, sel != null ? 1f : 0f, dt * 5f);
            tabHighlight.anchorMin = new Vector2(highlightX - 0.085f, 0.1f);
            tabHighlight.anchorMax = new Vector2(highlightX + 0.085f, 0.9f);
            tabHighlightImg.color = new Color(TabOn.r, TabOn.g, TabOn.b, 0.22f * highlightA);

            foreach (var tv in tabVisuals)
            {
                float sc = tv.tab == current ? 1.12f : 1f;
                if (tv.pop > 0f) { tv.pop -= dt; sc += 0.25f * Mathf.Sin(Mathf.Clamp01(1f - tv.pop / 0.3f) * Mathf.PI); }
                if (Mathf.Abs(tv.iconRt.localScale.x - sc) > 0.002f) tv.iconRt.localScale = Vector3.one * sc;
            }

            // ortadaki SAVAŞ: seçiliyken büyük + parlar, değilken hafif nabız (davet)
            bool on = current == Tab.Battle;
            float bs = on ? 1.06f : 1f + 0.025f * Mathf.Sin(now * 3f);
            if (battlePop > 0f) { battlePop -= dt; bs += 0.12f * Mathf.Sin(Mathf.Clamp01(1f - battlePop / 0.3f) * Mathf.PI); }
            battleBtn.localScale = Vector3.one * bs;
            var gc = battleGlow.GetComponent<Image>();
            float ga = on ? 0.45f + 0.12f * Mathf.Sin(now * 4f) : 0.18f;
            gc.color = new Color(gc.color.r, gc.color.g, gc.color.b, ga);
            if (upgradeBadge.gameObject.activeSelf)
                upgradeBadge.localScale = Vector3.one * (1f + 0.08f * Mathf.Max(0f, Mathf.Sin(now * 5f)));
        }

        void BuildBattlePage(RectTransform page)
        {
            bWorld = UiKit.Label(page, "", 56, TextAnchor.MiddleCenter, new Color(0.6f, 0.9f, 1f));
            UiKit.Stretch(bWorld.rectTransform, new Vector2(0f, 0.86f), new Vector2(1f, 0.93f));

            // Temaya özel amblem süsü (kabarcık, alev, yaprak, buz, kanat, güneş) halkanın arkasında
            bDeco = UiKit.Fixed(page, "EmblemDeco", new Vector2(0f, 170f), new Vector2(400f, 400f), Color.clear);
            bDeco.GetComponent<Image>().raycastTarget = false;
            var ring = UiKit.Fixed(page, "EmblemRing", new Vector2(0f, 170f), new Vector2(470f, 470f), new Color(1f, 1f, 1f, 0.12f), UiKit.Circle);
            bRing = ring.GetComponent<Image>();
            bRing.raycastTarget = false;
            var em = UiKit.Fixed(page, "Emblem", new Vector2(0f, 170f), new Vector2(400f, 400f), NodeNext, UiKit.Circle);
            bEmblem = em.GetComponent<Image>();
            bEmblem.raycastTarget = false;
            bLevelNum = UiKit.Label(em, "", 170, TextAnchor.MiddleCenter);
            AddTextShadow(bLevelNum, 6f);
            AddTextShadow(bWorld, 4f);

            bLevel = UiKit.Label(page, "", 64, TextAnchor.MiddleCenter);
            UiKit.Stretch(bLevel.rectTransform, new Vector2(0f, 0.33f), new Vector2(1f, 0.39f));
            bTag = UiKit.Label(page, "", 40, TextAnchor.MiddleCenter, NodeBoss);
            UiKit.Stretch(bTag.rectTransform, new Vector2(0f, 0.285f), new Vector2(1f, 0.33f));
            AddTextShadow(bLevel, 4f);
            var to = bTag.gameObject.AddComponent<Outline>();
            to.effectColor = new Color(0.2f, 0f, 0f, 0.7f);
            to.effectDistance = new Vector2(2f, -2f);

            bBtn = UiKit.Button(page, "BattleButton", new Vector2(0.15f, 0.13f), new Vector2(0.85f, 0.25f), TabOn, Loc.T("SAVAŞ", "BATTLE"), 80, OnBattle, out bBtnText);
            UiIcons.Round(bBtn.GetComponent<Image>(), 56f);
            var bsh = bBtn.gameObject.AddComponent<Shadow>();
            bsh.effectColor = new Color(0f, 0f, 0f, 0.4f);
            bsh.effectDistance = new Vector2(0f, -10f);
            bBtnGloss = UiIcons.Round(UiKit.Panel((RectTransform)bBtn.transform, "Gloss", new Vector2(0.04f, 0.55f), new Vector2(0.96f, 0.92f), new Color(1f, 1f, 1f, 0.14f)), 40f);
            bBtnGloss.raycastTarget = false;
            bBtnText.transform.SetAsLastSibling();
            AddTextShadow(bBtnText, 5f);

            bInfo = UiKit.Label(page, "", 30, TextAnchor.MiddleCenter, new Color(0.7f, 0.78f, 0.85f));
            bInfo.horizontalOverflow = HorizontalWrapMode.Wrap;
            bInfo.color = new Color(0.93f, 0.95f, 1f);
            var io = bInfo.gameObject.AddComponent<Outline>();
            io.effectColor = new Color(0f, 0f, 0f, 0.55f);
            io.effectDistance = new Vector2(2f, -2f);
            UiKit.Stretch(bInfo.rectTransform, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.11f));

            // Sonsuz Savunma (ilk boss yenilince bilgi yazısının yerine çıkar)
            Text el;
            var eb = UiKit.Button(page, "EndlessButton", new Vector2(0.2f, 0.025f), new Vector2(0.8f, 0.11f), new Color(0.42f, 0.24f, 0.7f),
                Loc.T("SONSUZ SAVUNMA", "ENDLESS DEFENSE"), 40, OnEndless, out el);
            UiIcons.MakePill(eb.GetComponent<Image>());
            var esh = eb.gameObject.AddComponent<Shadow>();
            esh.effectColor = new Color(0f, 0f, 0f, 0.4f);
            esh.effectDistance = new Vector2(0f, -7f);
            UiIcons.MakePill(UiKit.Panel((RectTransform)eb.transform, "Gloss", new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.9f), new Color(1f, 1f, 1f, 0.14f))).raycastTarget = false;
            el.transform.SetAsLastSibling();
            UiKit.Stretch(el.rectTransform, new Vector2(0f, 0.36f), new Vector2(1f, 1f));
            AddTextShadow(el, 3f);
            endlessSub = UiKit.Label((RectTransform)eb.transform, "", 24, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.4f));
            UiKit.Stretch(endlessSub.rectTransform, new Vector2(0f, 0.06f), new Vector2(1f, 0.42f));
            endlessBtn = eb.gameObject;
            endlessImg = eb.GetComponent<Image>();
            endlessLock = UiKit.Fixed((RectTransform)eb.transform, "Lock", Vector2.zero, new Vector2(44f, 44f), Color.white, UiKit.Lock);
            endlessLock.anchorMin = endlessLock.anchorMax = new Vector2(0.09f, 0.5f);
            endlessLock.GetComponent<Image>().raycastTarget = false;

            BuildGuideButton(page);
            BuildQuestButton(page);
        }

        static void AddTextShadow(Text t, float dist)
        {
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.5f);
            sh.effectDistance = new Vector2(0f, -dist);
        }

        void BuildUpgradesPage(RectTransform page)
        {
            var title = UiKit.Label(page, Loc.T("YÜKSELTMELER", "UPGRADES"), 64, TextAnchor.MiddleCenter);
            UiKit.Stretch(title.rectTransform, new Vector2(0f, 0.925f), new Vector2(1f, 0.995f));
            var tsh = title.gameObject.AddComponent<Shadow>();
            tsh.effectColor = new Color(0f, 0f, 0f, 0.55f);
            tsh.effectDistance = new Vector2(0f, -5f);

            // Başlığın altında süs çizgisi: iki yanda saydamlaşan altın çizgi + ortada elmas
            var orn = UiKit.Panel(page, "Ornament", new Vector2(0.12f, 0.905f), new Vector2(0.88f, 0.915f), Color.clear);
            orn.GetComponent<Image>().raycastTarget = false;
            var line = UiKit.Panel(orn, "Line", new Vector2(0f, 0.35f), new Vector2(1f, 0.65f), new Color(GoldC.r, GoldC.g, GoldC.b, 0.75f), UiIcons.FadeLine);
            line.GetComponent<Image>().raycastTarget = false;
            var dia = UiKit.Fixed(orn, "Diamond", Vector2.zero, new Vector2(34f, 34f), GoldC, UiIcons.Diamond);
            dia.GetComponent<Image>().raycastTarget = false;
            foreach (float sx in new[] { -52f, 52f })
            {
                var d2 = UiKit.Fixed(orn, "Dot", new Vector2(sx, 0f), new Vector2(14f, 14f), new Color(GoldC.r, GoldC.g, GoldC.b, 0.8f), UiIcons.Diamond);
                d2.GetComponent<Image>().raycastTarget = false;
            }

            var list = cfg.upgrades;
            float topY = 0.885f, bottomY = 0.01f;
            float h = (topY - bottomY) / Mathf.Max(1, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                float yMax = topY - i * h, yMin = yMax - h + 0.01f;
                var r = MakeRow(page, list[i].id, Loc.Name(list[i].displayName), yMin, yMax);
                r.enterDelay = i * 0.045f;
                rows.Add(r);
            }
        }

        /// <summary>Altın para ikonu: koyu altın disk + açık altın yüz (halka ve yıldız).</summary>
        RectTransform MakeCoin(RectTransform parent, float size)
        {
            var c = UiKit.Fixed(parent, "Coin", Vector2.zero, new Vector2(size, size), new Color(0.78f, 0.5f, 0.08f), UiIcons.Coin);
            c.GetComponent<Image>().raycastTarget = false;
            var face = UiKit.Panel(c, "Face", Vector2.zero, Vector2.one, new Color(1f, 0.86f, 0.32f), UiIcons.CoinFace);
            face.GetComponent<Image>().raycastTarget = false;
            return c;
        }

        Row MakeRow(RectTransform parent, UpgradeId id, string title, float yMin, float yMax)
        {
            var col = UiIcons.ColorFor(id);
            var rt = UiKit.Panel(parent, "Upgrade_" + id, new Vector2(0.03f, yMin), new Vector2(0.97f, yMax), RowBg);
            UiIcons.Round(rt, 28f);
            var r = new Row { id = id, color = col, rt = rt };
            // Solda yuvarlak uçlu renk şeridi
            UiIcons.MakePill(UiKit.Panel(rt, "Accent", new Vector2(0.013f, 0.2f), new Vector2(0.022f, 0.8f), col)).raycastTarget = false;

            // İkon rozeti
            var ring = UiKit.Panel(rt, "BadgeRing", new Vector2(0.03f, 0.1f), new Vector2(0.2f, 0.9f), new Color(col.r, col.g, col.b, 0.35f), UiKit.Circle);
            var ringImg = ring.GetComponent<Image>();
            ringImg.preserveAspect = true;
            ringImg.raycastTarget = false;
            var badge = UiKit.Panel(ring, "Badge", new Vector2(0.07f, 0.07f), new Vector2(0.93f, 0.93f), Color.Lerp(col, Color.black, 0.45f), UiKit.Circle);
            r.badge = badge.GetComponent<Image>();
            r.badge.preserveAspect = true;
            r.badge.raycastTarget = false;
            var icon = UiKit.Panel(badge, "Icon", new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), Color.Lerp(col, Color.white, 0.55f), UiIcons.ForUpgrade(id));
            icon.GetComponent<Image>().preserveAspect = true;
            icon.GetComponent<Image>().raycastTarget = false;

            r.name = UiKit.Label(rt, title, 40, TextAnchor.MiddleLeft);
            UiKit.Stretch(r.name.rectTransform, new Vector2(0.23f, 0.56f), new Vector2(0.6f, 0.94f));
            r.level = UiKit.Label(rt, "", 32, TextAnchor.MiddleRight, Color.Lerp(col, Color.white, 0.45f));
            UiKit.Stretch(r.level.rectTransform, new Vector2(0.52f, 0.56f), new Vector2(0.72f, 0.94f));
            r.desc = UiKit.Label(rt, "", 26, TextAnchor.MiddleLeft, new Color(0.78f, 0.84f, 0.9f));
            UiKit.Stretch(r.desc.rectTransform, new Vector2(0.23f, 0.3f), new Vector2(0.72f, 0.56f));
            r.desc.fontStyle = FontStyle.Normal;

            // Seviye ilerleme çubuğu
            // Oval uçlu ilerleme çubuğu (zemin, dolgu ve parlama hap şeklinde)
            var barBg = UiKit.Panel(rt, "Bar", new Vector2(0.23f, 0.11f), new Vector2(0.72f, 0.23f), new Color(0f, 0f, 0f, 0.45f), UiIcons.Pill);
            r.barBgImg = barBg.GetComponent<Image>();
            r.barBgImg.type = Image.Type.Sliced;
            r.barBgImg.raycastTarget = false;
            r.barFill = UiKit.Panel(barBg, "Fill", Vector2.zero, new Vector2(0f, 1f), col, UiIcons.Pill);
            r.barFill.pivot = new Vector2(0f, 0.5f);
            r.barFillImg = r.barFill.GetComponent<Image>();
            r.barFillImg.type = Image.Type.Sliced;
            r.barFillImg.raycastTarget = false;
            r.barShineImg = UiKit.Panel(r.barFill, "Shine", new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.86f), new Color(1f, 1f, 1f, 0.25f), UiIcons.Pill).GetComponent<Image>();
            r.barShineImg.type = Image.Type.Sliced;
            r.barShineImg.raycastTarget = false;
            var def = cfg.Get(id);
            if (id == UpgradeId.SoldierLevel && def != null)
            {
                // Evrim eşikleri (Kıdemli, Elit, Kraliyet): çubukta küçük altın çentikler
                foreach (int ev in cfg.evolutionLevels)
                {
                    float f = (ev - 1f) / Mathf.Max(1, def.maxLevel);
                    if (f <= 0f || f >= 1f) continue;
                    var tick = UiKit.Panel(barBg, "Evo", new Vector2(f, -0.35f), new Vector2(f, 1.35f), GoldC);
                    tick.sizeDelta = new Vector2(5f, 0f);
                    tick.GetComponent<Image>().raycastTarget = false;
                }
            }

            // Satın alma düğmesi: para ikonu + fiyat, alt gölge ve üst parlama şeridi (kabartma görünümü)
            r.button = UiKit.Button(rt, "Buy", new Vector2(0.745f, 0.13f), new Vector2(0.975f, 0.87f), BuyOn, "", 40, () => OnBuy(r), out r.cost);
            r.btnRt = (RectTransform)r.button.transform;
            r.buttonImage = r.button.GetComponent<Image>();
            var nav = r.button.navigation;
            nav.mode = Navigation.Mode.None;
            r.button.navigation = nav;
            // Oval (hap) düğme: altta koyu gölge, üst yarıda yumuşak parlama
            r.buttonImage.sprite = UiIcons.Pill;
            r.buttonImage.type = Image.Type.Sliced;
            var bsh = r.button.gameObject.AddComponent<Shadow>();
            bsh.effectColor = new Color(0f, 0f, 0f, 0.45f);
            bsh.effectDistance = new Vector2(0f, -7f);
            r.gloss = UiKit.Panel(r.btnRt, "Gloss", new Vector2(0.12f, 0.6f), new Vector2(0.88f, 0.88f), new Color(1f, 1f, 1f, 0.13f), UiIcons.Pill).GetComponent<Image>();
            r.gloss.type = Image.Type.Sliced;
            r.gloss.raycastTarget = false;
            r.coinRt = MakeCoin(r.btnRt, 46f);
            r.coinRt.anchorMin = r.coinRt.anchorMax = new Vector2(0.24f, 0.5f);
            r.cost.transform.SetAsLastSibling();
            UiKit.Stretch(r.cost.rectTransform, new Vector2(0.38f, 0f), new Vector2(0.92f, 1f));
            r.cost.alignment = TextAnchor.MiddleCenter;
            r.cost.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.45f);

            // Satın alınca parlayan beyaz katman (en üstte)
            var fl = UiKit.Panel(rt, "Flash", Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f));
            r.flash = UiIcons.Round(fl, 28f);
            r.flash.raycastTarget = false;
            return r;
        }

        void OnBuy(Row r)
        {
            var save = SaveSystem.Data;
            var def = cfg.Get(r.id);
            if (def == null) return;
            int lvl = save.GetUpgrade(r.id);
            if (lvl >= def.maxLevel) return;
            int cost = def.Cost(lvl);
            int before = save.gold;
            if (GameSession.TryBuyUpgrade(r.id))
            {
                StartGoldAnim(before, save.gold);
                r.punchT = 0.32f;
                r.popT = 0.4f;
                r.flashT = 0.4f;
                Sfx.Play(Snd.Purchase);
                Haptics.Vibrate(25);
                SpawnPurchaseFx(r);
                Refresh();
            }
            else
            {
                r.shakeT = 0.4f;
                Sfx.Play(Snd.Denied);
                Haptics.Vibrate(40);
                Toast((cost - save.gold) + Loc.T(" altın eksik", " gold short"), false);
            }
        }

        void StartGoldAnim(int from, int to)
        {
            goldAnimFrom = goldShown >= 0f ? goldShown : from;
            goldTarget = to;
            goldAnimT = 0f;
        }

        void SetGoldText(int value)
        {
            goldText.text = value.ToString();
            // Para ikonu, sağa yaslı sayının hemen soluna
            goldCoin.anchoredPosition = new Vector2(-goldText.preferredWidth - 50f, 0f);
        }

        // ------------------------------------------------------------------ yükseltme efektleri
        Particle GetParticle(Sprite sprite, Color color)
        {
            Particle p;
            if (particlePool.Count > 0) { p = particlePool[particlePool.Count - 1]; particlePool.RemoveAt(particlePool.Count - 1); }
            else
            {
                p = new Particle();
                p.rt = UiKit.Fixed(fxLayer, "P", Vector2.zero, new Vector2(20f, 20f), Color.white);
                p.img = p.rt.GetComponent<Image>();
                p.img.raycastTarget = false;
            }
            p.img.sprite = sprite;
            p.img.color = color;
            p.rt.gameObject.SetActive(true);
            p.rt.localRotation = Quaternion.identity;
            p.t = 0f;
            particles.Add(p);
            return p;
        }

        Vector2 FxPos(RectTransform target) => (Vector2)fxLayer.InverseTransformPoint(target.TransformPoint(target.rect.center));

        void SpawnPurchaseFx(Row r)
        {
            Vector2 btn = FxPos(r.btnRt);
            Vector2 gold = FxPos(goldCoin);
            // Üst bardaki altından düğmeye uçan paralar
            for (int i = 0; i < 5; i++)
            {
                var p = GetParticle(UiIcons.Coin, new Color(1f, 0.8f, 0.25f));
                p.coin = true;
                p.from = gold + Random.insideUnitCircle * 12f;
                p.to = btn + Random.insideUnitCircle * 20f;
                p.t = -i * 0.05f;
                p.life = 0.42f;
                p.size = 40f;
                p.spin = 0f;
                p.rt.sizeDelta = new Vector2(p.size, p.size);
                p.rt.anchoredPosition = p.from;
                p.img.color = new Color(1f, 0.8f, 0.25f, 0f);
            }
            // Düğmeden saçılan yıldız ve parıltılar (yükseltmenin renginde ve altın)
            for (int i = 0; i < 16; i++)
            {
                bool starP = i % 2 == 0;
                var c = i % 3 == 0 ? GoldC : Color.Lerp(r.color, Color.white, 0.3f);
                var p = GetParticle(starP ? UiIcons.Star : UiIcons.Spark, c);
                p.coin = false;
                float a = Random.Range(0f, Mathf.PI * 2f);
                float sp = Random.Range(380f, 820f);
                p.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f + 0.5f) * sp;
                p.from = btn;
                p.rt.anchoredPosition = btn;
                p.t = 0f;
                p.life = Random.Range(0.45f, 0.75f);
                p.size = starP ? Random.Range(26f, 40f) : Random.Range(34f, 56f);
                p.spin = Random.Range(-540f, 540f);
                p.rt.sizeDelta = new Vector2(p.size, p.size);
            }
        }

        void UpdateUpgradeFx(float dt)
        {
            // Üst bardaki altın sayacı akarak değişir
            if (goldShown < 0f) goldShown = SaveSystem.Data.gold;
            if (goldAnimT < 1f)
            {
                goldAnimT = Mathf.Min(1f, goldAnimT + dt / 0.5f);
                float e = 1f - (1f - goldAnimT) * (1f - goldAnimT);
                goldShown = Mathf.Lerp(goldAnimFrom, goldTarget, e);
                SetGoldText(Mathf.RoundToInt(goldShown));
                goldCoin.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(goldAnimT * Mathf.PI));
            }

            float now = Time.unscaledTime;
            if (current == Tab.Upgrades) upgradesEnterT += dt;
            foreach (var r in rows)
            {
                // Hap düğmenin uçları tam yarım daire olsun (yerleşim bitince bir kez)
                if (current == Tab.Upgrades)
                {
                    UiIcons.FitPill(r.buttonImage);
                    UiIcons.FitPill(r.gloss);
                    UiIcons.FitPill(r.barBgImg);
                    UiIcons.FitPill(r.barFillImg);
                    UiIcons.FitPill(r.barShineImg);
                }
                // Sekme açılınca satırlar sırayla sağdan kayarak gelir
                float e = Mathf.Clamp01((upgradesEnterT - r.enterDelay) / 0.28f);
                float ease = 1f - Mathf.Pow(1f - e, 3f);
                var cg = r.rt.anchoredPosition;
                r.rt.anchoredPosition = new Vector2((1f - ease) * 260f, cg.y);

                // İlerleme çubuğu yumuşakça dolar
                if (r.barCur < 0f) r.barCur = r.barTarget;
                r.barCur = Mathf.MoveTowards(r.barCur, r.barTarget, dt * 0.8f);
                r.barFill.anchorMax = new Vector2(r.barCur, 1f);
                r.barFill.gameObject.SetActive(r.barCur > 0.001f);
                // Dolgu çok kısayken bile yuvarlak uç görünsün: en az çubuk yüksekliği kadar genişlik
                float bh = r.barFill.rect.height;
                r.barFill.sizeDelta = new Vector2(Mathf.Max(0f, bh - r.barFill.rect.width + r.barFill.sizeDelta.x), r.barFill.sizeDelta.y);

                // Düğme: alınabiliyorsa hafif nabız, satın alınca zıplama, yetmiyorsa titreme
                float scale = r.affordable ? 1f + 0.035f * Mathf.Sin(now * 5f + r.enterDelay * 20f) : 1f;
                if (r.punchT > 0f)
                {
                    r.punchT -= dt;
                    float t = 1f - r.punchT / 0.32f;
                    scale *= t < 0.25f ? Mathf.Lerp(1f, 0.86f, t / 0.25f) : 1f + 0.16f * Mathf.Sin((t - 0.25f) / 0.75f * Mathf.PI) * (1f - t);
                }
                r.btnRt.localScale = Vector3.one * scale;
                float sx = 0f;
                if (r.shakeT > 0f)
                {
                    r.shakeT -= dt;
                    sx = Mathf.Sin(r.shakeT * 70f) * 16f * (r.shakeT / 0.4f);
                    r.buttonImage.color = Color.Lerp(r.affordable ? BuyOn : BuyOff, new Color(0.85f, 0.2f, 0.2f), r.shakeT / 0.4f);
                    if (r.shakeT <= 0f) r.buttonImage.color = r.maxed ? MaxedC : r.affordable ? BuyOn : BuyOff;
                }
                r.btnRt.anchoredPosition = new Vector2(sx, 0f);

                if (r.popT > 0f)
                {
                    r.popT -= dt;
                    float k = Mathf.Clamp01(r.popT / 0.4f);
                    r.level.rectTransform.localScale = Vector3.one * (1f + 0.45f * k * k);
                    r.badge.transform.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin((1f - k) * Mathf.PI));
                }
                if (r.flashT > 0f)
                {
                    r.flashT -= dt;
                    r.flash.color = new Color(r.color.r, r.color.g, r.color.b, 0.35f * Mathf.Clamp01(r.flashT / 0.4f));
                }
            }

            // Parçacıklar
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.t += dt;
                if (p.t < 0f) continue;
                float k = p.t / p.life;
                if (k >= 1f)
                {
                    p.rt.gameObject.SetActive(false);
                    particles.RemoveAt(i);
                    particlePool.Add(p);
                    continue;
                }
                if (p.coin)
                {
                    // Yay çizerek uçan para
                    float e = k * k * (3f - 2f * k);
                    var mid = (p.from + p.to) * 0.5f + new Vector2(-160f, 60f);
                    var a = Vector2.Lerp(p.from, mid, e);
                    var b = Vector2.Lerp(mid, p.to, e);
                    p.rt.anchoredPosition = Vector2.Lerp(a, b, e);
                    p.rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, e);
                    var c = p.img.color;
                    c.a = Mathf.Clamp01(k * 6f) * Mathf.Clamp01((1f - k) * 5f);
                    p.img.color = c;
                }
                else
                {
                    p.vel += new Vector2(0f, -1500f) * dt;
                    p.vel *= 1f - 1.8f * dt;
                    p.rt.anchoredPosition += p.vel * dt;
                    p.rt.localRotation = Quaternion.Euler(0f, 0f, p.spin * p.t);
                    p.rt.localScale = Vector3.one * (1f - k * 0.6f);
                    var c = p.img.color;
                    c.a = 1f - k * k;
                    p.img.color = c;
                }
            }
        }


        void BuildLevelsPage(RectTransform page)
        {
            UiIcons.Round(UiKit.Button(page, "Prev", new Vector2(0.03f, 0.905f), new Vector2(0.15f, 0.985f), TabOff, "<", 64, () => ChangeWorld(-1), out _).GetComponent<Image>(), 26f);
            UiIcons.Round(UiKit.Button(page, "Next", new Vector2(0.85f, 0.905f), new Vector2(0.97f, 0.985f), TabOff, ">", 64, () => ChangeWorld(1), out _).GetComponent<Image>(), 26f);
            // Başlık plakası: temalı arka planlarda (buz sarkıtı, sarmaşık...) başlık okunaklı kalsın
            var plate = UiKit.Panel(page, "TitlePlate", new Vector2(0.17f, 0.895f), new Vector2(0.83f, 0.993f), new Color(0.02f, 0.04f, 0.08f, 0.62f));
            UiIcons.Round(plate, 30f).raycastTarget = false;
            wTitle = UiKit.Label(page, "", 48, TextAnchor.MiddleCenter);
            UiKit.Stretch(wTitle.rectTransform, new Vector2(0.15f, 0.94f), new Vector2(0.85f, 0.99f));
            wSub = UiKit.Label(page, "", 30, TextAnchor.MiddleCenter, new Color(0.7f, 0.8f, 0.9f));
            UiKit.Stretch(wSub.rectTransform, new Vector2(0.15f, 0.9f), new Vector2(0.85f, 0.94f));
            AddTextShadow(wTitle, 4f);
            AddTextShadow(wSub, 3f);

            map = UiKit.Fixed(page, "Map", new Vector2(0f, -70f), new Vector2(920f, 1350f), Color.clear);
            map.GetComponent<Image>().raycastTarget = false;
        }

        void BuildProfilePage(RectTransform page)
        {
            UiKit.Stretch(UiKit.Label(page, Loc.T("PROFİL", "PROFILE"), 56, TextAnchor.MiddleCenter).rectTransform, new Vector2(0f, 0.93f), new Vector2(1f, 0.99f));
            // Hikaye arşivi: açılmış komutan konuşmaları tekrar okunur
            Text sl;
            var storyBtn = UiKit.Button(page, "StoryButton", new Vector2(0.64f, 0.94f), new Vector2(0.97f, 0.993f), new Color(0.1f, 0.2f, 0.34f, 0.92f), Loc.T("HİKAYE", "STORY"), 32, OpenArchive, out sl);
            UiIcons.MakePill(storyBtn.GetComponent<Image>());
            UiKit.Stretch(sl.rectTransform, new Vector2(0.3f, 0f), new Vector2(0.95f, 1f));
            AddTextShadow(sl, 3f);
            var bk = UiKit.Panel((RectTransform)storyBtn.transform, "Icon", new Vector2(0.05f, 0.14f), new Vector2(0.27f, 0.86f), NodeNext, UiKit.Circle);
            bk.GetComponent<Image>().preserveAspect = true;
            bk.GetComponent<Image>().raycastTarget = false;
            var bki = UiKit.Panel(bk, "Star", new Vector2(0.22f, 0.22f), new Vector2(0.78f, 0.78f), Color.white, UiIcons.Star);
            bki.GetComponent<Image>().preserveAspect = true;
            bki.GetComponent<Image>().raycastTarget = false;

            // --- Komutan kartı: rütbe + ilerleme çubuğu ---
            var card = UiKit.Panel(page, "RankCard", new Vector2(0.04f, 0.795f), new Vector2(0.96f, 0.92f), RowBg);
            UiIcons.Round(card, 30f);
            var badge = UiKit.Panel(card, "Badge", new Vector2(0.03f, 0.1f), new Vector2(0.21f, 0.9f), NodeNext, UiKit.Circle);
            badge.GetComponent<Image>().preserveAspect = true;
            pRankNum = UiKit.Label(badge, "", 64, TextAnchor.MiddleCenter);
            pRank = UiKit.Label(card, "", 46, TextAnchor.MiddleLeft, GoldC);
            UiKit.Stretch(pRank.rectTransform, new Vector2(0.25f, 0.52f), new Vector2(0.97f, 0.95f));
            var barBg = UiKit.Panel(card, "ProgBg", new Vector2(0.25f, 0.2f), new Vector2(0.95f, 0.4f), new Color(0f, 0f, 0f, 0.45f));
            UiIcons.MakePill(barBg);
            pProgFill = UiKit.Panel(barBg, "Fill", Vector2.zero, new Vector2(0.5f, 1f), TabOn);
            UiIcons.MakePill(pProgFill);
            pProg = UiKit.Label(barBg, "", 28, TextAnchor.MiddleCenter);

            // --- İstatistikler: 2 x 4 karo ---
            UiKit.Stretch(UiKit.Label(page, Loc.T("İSTATİSTİKLER", "STATISTICS"), 40, TextAnchor.MiddleLeft, new Color(0.6f, 0.9f, 1f)).rectTransform,
                new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.785f));
            string[] names = Loc.EN
                ? new[] { "LEVELS CLEARED", "WORLDS CLEARED", "BOSSES DEFEATED", "ENEMIES KILLED", "GOLD EARNED", "LARGEST ARMY", "PLAY TIME", "WIN RATE" }
                : new[] { "GEÇİLEN SEVİYE", "TEMİZLENEN DÜNYA", "YENİLEN BOSS", "ÖLDÜRÜLEN DÜŞMAN", "KAZANILAN ALTIN", "EN BÜYÜK ORDU", "OYUN SÜRESİ", "ZAFER ORANI" };
            Color[] accents = { NodeDone, new Color(0.35f, 0.75f, 0.55f), NodeBoss, new Color(0.9f, 0.5f, 0.3f), GoldC, new Color(0.45f, 0.75f, 1f), new Color(0.75f, 0.6f, 1f), new Color(0.4f, 0.9f, 0.5f) };
            pStats = new Text[names.Length];
            float top = 0.735f, bottom = 0.375f, gap = 0.012f;
            float h = (top - bottom) / 4f;
            for (int i = 0; i < names.Length; i++)
            {
                int col = i % 2, row = i / 2;
                float x0 = col == 0 ? 0.04f : 0.51f, x1 = col == 0 ? 0.49f : 0.96f;
                float yMax = top - row * h, yMin = yMax - h + gap;
                var tile = UiKit.Panel(page, "Stat" + i, new Vector2(x0, yMin), new Vector2(x1, yMax), RowBg);
                UiIcons.Round(tile, 24f);
                UiIcons.MakePill(UiKit.Panel(tile, "Accent", new Vector2(0.02f, 0.18f), new Vector2(0.038f, 0.82f), accents[i])).raycastTarget = false;
                pStats[i] = UiKit.Label(tile, "", 50, TextAnchor.MiddleLeft, accents[i]);
                UiKit.Stretch(pStats[i].rectTransform, new Vector2(0.09f, 0.4f), new Vector2(0.98f, 0.95f));
                var l = UiKit.Label(tile, names[i], 25, TextAnchor.MiddleLeft, new Color(0.7f, 0.78f, 0.85f));
                UiKit.Stretch(l.rectTransform, new Vector2(0.09f, 0.06f), new Vector2(0.98f, 0.4f));
            }

            BuildEndlessStats(page);

        }

        // --- Profil > Sonsuz Savunma: tek koşuda yapılan en iyi değerler (her rekorda güncellenir) ---
        static readonly Color EndlessC = new Color(0.62f, 0.45f, 0.95f);
        Text[] eStats;
        Text eRuns, eLocked;
        GameObject eGrid;

        void BuildEndlessStats(RectTransform page)
        {
            var head = UiKit.Label(page, Loc.T("SONSUZ SAVUNMA REKORLARI", "ENDLESS DEFENSE RECORDS"), 36, TextAnchor.MiddleLeft, EndlessC);
            UiKit.Stretch(head.rectTransform, new Vector2(0.05f, 0.318f), new Vector2(0.75f, 0.36f));
            eRuns = UiKit.Label(page, "", 26, TextAnchor.MiddleRight, new Color(0.7f, 0.78f, 0.85f));
            UiKit.Stretch(eRuns.rectTransform, new Vector2(0.6f, 0.318f), new Vector2(0.95f, 0.36f));

            var box = UiKit.Panel(page, "EndlessBox", new Vector2(0.04f, 0.025f), new Vector2(0.96f, 0.312f), new Color(0.13f, 0.09f, 0.24f, 1f));
            UiIcons.Round(box, 30f);
            UiIcons.MakePill(UiKit.Panel(box, "Accent", new Vector2(0.015f, 0.08f), new Vector2(0.026f, 0.92f), EndlessC)).raycastTarget = false;

            string[] names = Loc.EN
                ? new[] { "BEST WAVE", "LONGEST RUN", "MOST KILLS", "LARGEST ARMY", "MOST GOLD", "MOST BOSSES" }
                : new[] { "EN İYİ DALGA", "EN UZUN SÜRE", "EN ÇOK DÜŞMAN", "EN BÜYÜK ORDU", "EN ÇOK ALTIN", "EN ÇOK BOSS" };
            Color[] acc = { GoldC, new Color(0.75f, 0.6f, 1f), new Color(0.9f, 0.5f, 0.3f), new Color(0.45f, 0.75f, 1f), GoldC, NodeBoss };
            var grid = UiKit.Panel(box, "Grid", Vector2.zero, Vector2.one, Color.clear);
            grid.GetComponent<Image>().raycastTarget = false;
            eGrid = grid.gameObject;
            eStats = new Text[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                int col = i % 3, row = i / 3;
                float x0 = 0.04f + col * 0.32f, x1 = x0 + 0.3f;
                float y1 = row == 0 ? 0.95f : 0.48f, y0 = y1 - 0.43f;
                var tile = UiKit.Panel(grid, "E" + i, new Vector2(x0, y0), new Vector2(x1, y1), new Color(0.2f, 0.15f, 0.34f, 1f));   // opak (yarı saydam beyaz doğrusal renkte griye kaçıyordu)
                UiIcons.Round(tile, 20f).raycastTarget = false;
                eStats[i] = UiKit.Label(tile, "", i == 0 ? 54 : 44, TextAnchor.MiddleCenter, acc[i]);
                UiKit.Stretch(eStats[i].rectTransform, new Vector2(0f, 0.38f), new Vector2(1f, 0.98f));
                var l = UiKit.Label(tile, names[i], 21, TextAnchor.MiddleCenter, new Color(0.75f, 0.78f, 0.9f));
                UiKit.Stretch(l.rectTransform, new Vector2(0f, 0.06f), new Vector2(1f, 0.38f));
            }
            eLocked = UiKit.Label(box, Loc.T("İlk diyarın boss'unu yenince açılır", "Unlocks after the first land's boss"), 34, TextAnchor.MiddleCenter, new Color(0.8f, 0.82f, 0.9f));
        }

        void RefreshEndlessStats(SaveData s)
        {
            if (eStats == null) return;
            bool on = GameSession.EndlessUnlocked;
            eGrid.SetActive(on);
            eLocked.gameObject.SetActive(!on);
            eRuns.text = on ? Loc.T("Oynanan: ", "Runs: ") + s.endlessRuns : "";
            string dash = "-";
            eStats[0].text = s.endlessBestWave > 0 ? s.endlessBestWave.ToString() : dash;
            eStats[1].text = s.endlessBestTime > 0f ? Duration(s.endlessBestTime) : dash;
            eStats[2].text = s.endlessRuns > 0 ? Group(s.endlessBestKills) : dash;
            eStats[3].text = s.endlessRuns > 0 ? Group(s.endlessBestArmy) : dash;
            eStats[4].text = s.endlessRuns > 0 ? Group(s.endlessBestGold) : dash;
            eStats[5].text = s.endlessRuns > 0 ? s.endlessBestBosses.ToString() : dash;
        }

        void BuildSettingsPage(RectTransform page)
        {
            var title = UiKit.Label(page, Loc.T("AYARLAR", "SETTINGS"), 56, TextAnchor.MiddleCenter);
            UiKit.Stretch(title.rectTransform, new Vector2(0f, 0.93f), new Vector2(1f, 0.99f));
            float sTop = 0.9f, sh = 0.115f;
            AddToggle(page, Loc.T("SES EFEKTLERİ", "SOUND EFFECTS"), Loc.T("Atış, patlama ve vuruş sesleri", "Shots, explosions and hits"), sTop - 0 * sh, sh, () => Sfx.EffectsOn, v => Sfx.EffectsOn = v);
            AddToggle(page, Loc.T("ORTAM SESİ", "AMBIENCE"), Loc.T("Dünyaların arka plan sesi", "Background sound of the worlds"), sTop - 1 * sh, sh, () => Sfx.AmbienceOn, v => { Sfx.AmbienceOn = v; if (v) Sfx.SetAmbience(true, 0.2f); });
            AddToggle(page, Loc.T("TİTREŞİM", "VIBRATION"), Loc.T("Hasar, zafer ve yenilgide titreşim", "Vibrate on damage, victory and defeat"), sTop - 2 * sh, sh, () => Haptics.Enabled, v => { Haptics.Enabled = v; if (v) Haptics.Vibrate(60); });
            AddLanguageRow(page, sTop - 3 * sh, sh);
            AddPrivacyRow(page, sTop - 4 * sh, sh);

            var ver = UiKit.Label(page, "Gate Defenders · " + Loc.T("sürüm ", "version ") + Application.version, 26, TextAnchor.MiddleCenter, new Color(0.55f, 0.62f, 0.72f));
            UiKit.Stretch(ver.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.07f));
        }

        /// <summary>Dil seçimi: Türkçe / English. Seçince menü yeni dille baştan kurulur ve Ayarlar sekmesi açık kalır.</summary>
        void AddLanguageRow(RectTransform page, float yMax, float h)
        {
            var row = UiKit.Panel(page, "Set_Language", new Vector2(0.04f, yMax - h + 0.014f), new Vector2(0.96f, yMax), RowBg);
            UiIcons.Round(row, 28f);
            var l = UiKit.Label(row, Loc.T("DİL", "LANGUAGE"), 38, TextAnchor.MiddleLeft);
            UiKit.Stretch(l.rectTransform, new Vector2(0.05f, 0.45f), new Vector2(0.45f, 0.92f));
            var d = UiKit.Label(row, Loc.T("Oyunun dili", "Game language"), 24, TextAnchor.MiddleLeft, new Color(0.65f, 0.72f, 0.8f));
            d.fontStyle = FontStyle.Normal;
            UiKit.Stretch(d.rectTransform, new Vector2(0.05f, 0.1f), new Vector2(0.45f, 0.45f));
            void Option(Lang lang, string text, float x0, float x1)
            {
                bool on = Loc.Current == lang;
                var b = UiKit.Button(row, "Lang_" + lang, new Vector2(x0, 0.22f), new Vector2(x1, 0.78f), on ? TabOn : BuyOff, text, 30, () =>
                {
                    if (Loc.Current == lang) return;
                    Loc.Set(lang);
                    PendingTab = Tab.Settings;
                    GameSession.GoToMenu();   // tüm yazılar yeni dille yeniden kurulsun
                }, out var t);
                UiIcons.MakePill(b.GetComponent<Image>());
                if (!on) t.color = new Color(0.8f, 0.84f, 0.9f);
                AddTextShadow(t, 3f);
            }
            Option(Lang.TR, "TÜRKÇE", 0.42f, 0.69f);
            Option(Lang.EN, "ENGLISH", 0.705f, 0.96f);
        }

        /// <summary>Gizlilik politikası adresi. Türkçede sayfanın Türkçe bölümü (#tr) açılır.</summary>
        public const string PrivacyPolicyUrl = "https://yemre-games.github.io/gate-defenders/privacy/";
        public static string PrivacyUrlForLang() => Loc.EN ? PrivacyPolicyUrl : PrivacyPolicyUrl + "#tr";

        /// <summary>Gizlilik politikası satırı: AÇ düğmesi politikayı telefonun tarayıcısında açar (oyun internete bağlanmaz).</summary>
        void AddPrivacyRow(RectTransform page, float yMax, float h)
        {
            var row = UiKit.Panel(page, "Set_Privacy", new Vector2(0.04f, yMax - h + 0.014f), new Vector2(0.96f, yMax), RowBg);
            UiIcons.Round(row, 28f);
            var l = UiKit.Label(row, Loc.T("GİZLİLİK POLİTİKASI", "PRIVACY POLICY"), 38, TextAnchor.MiddleLeft);
            UiKit.Stretch(l.rectTransform, new Vector2(0.05f, 0.45f), new Vector2(0.68f, 0.92f));
            var d = UiKit.Label(row, Loc.T("Verilerinin nasıl ele alındığı", "How your data is handled"), 24, TextAnchor.MiddleLeft, new Color(0.65f, 0.72f, 0.8f));
            d.fontStyle = FontStyle.Normal;
            UiKit.Stretch(d.rectTransform, new Vector2(0.05f, 0.1f), new Vector2(0.68f, 0.45f));
            var b = UiKit.Button(row, "Open", new Vector2(0.7f, 0.22f), new Vector2(0.96f, 0.78f), BuyOff, Loc.T("AÇ", "OPEN"), 34, () => Application.OpenURL(PrivacyUrlForLang()), out var t);
            UiIcons.MakePill(b.GetComponent<Image>());
            AddTextShadow(t, 3f);
        }

        /// <summary>Menü yeniden kurulunca açılacak sekme (dil değişince Ayarlar'da kalmak için).</summary>
        static Tab? PendingTab;

        /// <summary>Menü bir sonraki açılışında Yükseltmeler sekmesiyle açılsın (bitiş ekranındaki YÜKSELT düğmesi).</summary>
        public static void OpenUpgradesNext() => PendingTab = Tab.Upgrades;

        float resetArmedAt = -10f;
        Text resetLabelRef;

        // Profil
        Text pRankNum, pRank, pProg;
        RectTransform pProgFill;
        Text[] pStats;

        void AddToggle(RectTransform page, string title, string desc, float yMax, float h, System.Func<bool> get, System.Action<bool> set)
        {
            var row = UiKit.Panel(page, "Set_" + title, new Vector2(0.04f, yMax - h + 0.014f), new Vector2(0.96f, yMax), RowBg);
            UiIcons.Round(row, 28f);
            var l = UiKit.Label(row, title, 38, TextAnchor.MiddleLeft);
            UiKit.Stretch(l.rectTransform, new Vector2(0.05f, 0.45f), new Vector2(0.65f, 0.92f));
            var d = UiKit.Label(row, desc, 24, TextAnchor.MiddleLeft, new Color(0.65f, 0.72f, 0.8f));
            d.fontStyle = FontStyle.Normal;
            UiKit.Stretch(d.rectTransform, new Vector2(0.05f, 0.1f), new Vector2(0.68f, 0.45f));
            Text label = null;
            Button btn = null;
            System.Action paint = () =>
            {
                bool on = get();
                label.text = on ? Loc.T("AÇIK", "ON") : Loc.T("KAPALI", "OFF");
                btn.GetComponent<Image>().color = on ? BuyOn : BuyOff;
            };
            btn = UiKit.Button(row, "Toggle", new Vector2(0.7f, 0.22f), new Vector2(0.96f, 0.78f), BuyOn, "", 34, () =>
            {
                set(!get());
                paint();
            }, out label);
            UiIcons.MakePill(btn.GetComponent<Image>());
            paint();
        }

        static string Group(int n)
        {
            // 12345 -> 12.345 (Türkçe binlik ayırıcı)
            var s = Mathf.Abs(n).ToString();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (i > 0 && (s.Length - i) % 3 == 0) sb.Append('.');
                sb.Append(s[i]);
            }
            return (n < 0 ? "-" : "") + sb;
        }

        static string Duration(float sec)
        {
            int m = Mathf.FloorToInt(sec / 60f);
            string mn = Loc.T(" dk", " min"), hr = Loc.T(" sa ", " h ");
            if (m < 60) return m + mn;
            return (m / 60) + hr + (m % 60) + mn;
        }

        void RefreshProfile()
        {
            if (pStats == null) return;
            var s = SaveSystem.Data;
            int total = db != null ? db.worlds.Count * GameDatabase.LevelsPerWorld : 10;
            int passed = Mathf.Clamp(s.level - 1, 0, total);
            int worlds = passed / GameDatabase.LevelsPerWorld;
            string[] ranks = Loc.EN
                ? new[] { "PRIVATE", "CORPORAL", "SERGEANT", "LIEUTENANT", "CAPTAIN", "MAJOR", "COLONEL", "GENERAL" }
                : new[] { "ER", "ONBAŞI", "ÇAVUŞ", "TEĞMEN", "YÜZBAŞI", "BİNBAŞI", "ALBAY", "GENERAL" };
            pRankNum.text = (worlds + 1).ToString();
            pRank.text = Loc.T("KOMUTAN · ", "COMMANDER · ") + ranks[Mathf.Min(worlds, ranks.Length - 1)];
            float frac = total > 0 ? (float)passed / total : 0f;
            pProgFill.anchorMax = new Vector2(Mathf.Max(0.02f, frac), 1f);
            pProg.text = passed + " / " + total + Loc.T(" seviye", " levels");

            int bosses = Mathf.Max(s.bossesDefeated, worlds);
            int wins = s.levelsWon, games = s.levelsWon + s.runsLost;
            pStats[0].text = passed.ToString();
            pStats[1].text = worlds + " / " + (db != null ? db.worlds.Count : 1);
            pStats[2].text = bosses.ToString();
            pStats[3].text = Group(s.totalKills);
            pStats[4].text = Group(s.totalGoldEarned);
            pStats[5].text = Group(s.maxArmy);
            pStats[6].text = Duration(s.playTimeSeconds);
            pStats[7].text = games > 0 ? (Loc.EN ? Mathf.RoundToInt(100f * wins / games) + "%" : "%" + Mathf.RoundToInt(100f * wins / games)) : "-";
            RefreshEndlessStats(s);
        }

        // ------------------------------------------------------------------ davranış
        /// <summary>Oyuncunun bulunduğu (sıradaki seviyenin) dünyası; henüz olmayan dünyadaysa son açık dünya.</summary>
        int CurrentWorldPage()
        {
            int slots = db != null ? db.WorldSlotCount : 1;
            int w = Mathf.Clamp(GameDatabase.WorldIndexOf(GameSession.NextLevel), 0, Mathf.Max(0, slots - 1));
            if (db != null && !db.IsWorldAvailable(w)) w = Mathf.Max(0, db.worlds.Count - 1);
            return w;
        }

        void ShowTab(Tab t)
        {
            // Seviyeler sekmesi her açılışta oyuncunun bulunduğu dünyadan başlar
            if (t == Tab.Levels && current != Tab.Levels) worldPage = CurrentWorldPage();
            // Sekme değişince yarım kalmış kaydırma unutulur (eski dokunuş, sekme düğmesine basışla birleşip dünya atlatıyordu)
            swipeTracking = false;
            current = t;
            for (int i = 0; i < TabCount; i++) pages[i].gameObject.SetActive(i == (int)t);
            foreach (var tv in tabVisuals)
            {
                bool on = tv.tab == t;
                tv.icon.color = on ? TabOn : TabIconOff;
                tv.label.color = on ? Color.white : TabIconOff;
                if (on) tv.pop = 0.3f;
            }
            if (t == Tab.Battle) battlePop = 0.3f;
            // Temalı arka plan yalnızca Savaş sekmesinde; diğer sekmelerde sade zemin (üst üste çizim olmasın)
            themeView.gameObject.SetActive(t == Tab.Battle);
            menuBgRaw.enabled = t != Tab.Battle;
            Refresh();
            if (t == Tab.Profile) RefreshProfile();
            if (t == Tab.Upgrades) upgradesEnterT = 0f;
        }

        void OnBattle()
        {
            int next = GameSession.NextLevel;
            if (GameSession.IsUnlocked(next)) GameSession.Play(next);
            else Toast(Loc.T("Yeni dünya yakında!", "New world coming soon!"), false);
        }

        void ChangeWorld(int dir)
        {
            int slots = db != null ? db.WorldSlotCount : 1;
            int w = Mathf.Clamp(worldPage + dir, 0, slots - 1);
            if (w == worldPage) return;
            worldPage = w;
            RefreshLevels();
        }

        /// <summary>Yalnızca test için: 0-5 arası verilirse ana sayfa o dünyanın temasıyla gösterilir.</summary>
        public static int PreviewTheme = -1;

        WorldTheme CurrentTheme(int worldIndex)
        {
            if (PreviewTheme >= 0) return (WorldTheme)PreviewTheme;
            if (db == null || db.worlds.Count == 0) return WorldTheme.Underwater;
            int i = Mathf.Clamp(worldIndex, 0, db.worlds.Count - 1);
            return db.worlds[i] != null ? db.worlds[i].theme : WorldTheme.Underwater;
        }

        void Refresh()
        {
            var save = SaveSystem.Data;
            if (goldAnimT >= 1f || goldShown < 0f) { goldShown = save.gold; SetGoldText(save.gold); }
            int next = GameSession.NextLevel;
            topLevelText.text = Loc.T("SEVİYE ", "LEVEL ") + next;

            // Savaş sayfası
            bool available = GameSession.IsUnlocked(next);
            int wi = GameDatabase.WorldIndexOf(next);
            bWorld.text = db != null ? Loc.Upper(db.WorldName(wi)) : "";
            bLevelNum.text = next.ToString();
            bLevel.text = Loc.T("SEVİYE ", "LEVEL ") + next;
            bool boss = GameSession.IsBoss(next);
            bTag.text = available && boss ? Loc.T("BOSS SEVİYESİ", "BOSS LEVEL") : "";

            // Dünya teması: arka plan, amblem süsü ve renkler bulunulan dünyaya göre
            var theme = CurrentTheme(wi);
            if (current == Tab.Battle) themeView.Apply(theme, bDeco);
            var pal = MenuThemeView.Get(theme);
            bWorld.color = pal.label;
            bEmblem.color = !available ? NodeLocked : pal.emblem;
            bRing.color = pal.ring;
            bTag.color = new Color(1f, 0.4f, 0.4f);
            bBtn.interactable = available;
            bBtn.GetComponent<Image>().color = available ? pal.button : BuyOff;
            battleBtnImg.color = pal.button;
            var glowImg = battleGlow.GetComponent<Image>();
            glowImg.color = new Color(pal.label.r, pal.label.g, pal.label.b, glowImg.color.a);
            bBtnText.text = available ? Loc.T("SAVAŞ", "BATTLE") : Loc.T("YAKINDA", "SOON");
            // Sonsuz Savunma düğmesi hep görünür; ilk boss yenilene kadar kilitli (gri + kilit)
            bool endlessOn = GameSession.EndlessUnlocked;
            bInfo.gameObject.SetActive(false);
            endlessImg.color = endlessOn ? new Color(0.42f, 0.24f, 0.7f) : new Color(0.3f, 0.3f, 0.36f);
            endlessLock.gameObject.SetActive(!endlessOn);
            endlessSub.text = !endlessOn ? Loc.T("İLK BOSS'U YENİNCE AÇILIR", "UNLOCKS AFTER THE FIRST BOSS")
                : save.endlessBestWave > 0 ? Loc.T("REKOR: DALGA ", "BEST: WAVE ") + save.endlessBestWave : Loc.T("Ne kadar dayanabilirsin?", "How long can you last?");
            endlessSub.color = endlessOn ? new Color(1f, 0.86f, 0.4f) : new Color(0.8f, 0.82f, 0.86f);
            bInfo.text = available
                ? Loc.T("Geçtiğin seviyeleri Seviyeler sekmesinden yarım ödülle tekrar oynayabilirsin.", "Replay cleared levels from the Levels tab for half the reward.")
                : Loc.T("Tüm seviyeleri geçtin! Sıradaki dünya: ", "You cleared every level! Next world: ") + (db != null ? db.WorldName(wi) : "") + Loc.T("\nO zamana kadar eski seviyeleri tekrar oynayıp güçlenebilirsin.", "\nUntil then, replay old levels to get stronger.");

            // Yükseltmeler
            int affordableCount = 0;
            foreach (var r in rows)
            {
                var def = cfg.Get(r.id);
                int lvl = save.GetUpgrade(r.id);
                bool maxed = lvl >= def.maxLevel;
                int cost = def.Cost(lvl);
                bool affordable = !maxed && save.gold >= cost;
                r.level.text = r.id == UpgradeId.SoldierLevel ? Loc.T("Sv ", "Lv ") + cfg.SoldierLevel(lvl) : lvl + " / " + def.maxLevel;
                r.desc.text = maxed ? Loc.T("En yüksek seviye", "Max level") : cfg.Describe(r.id, lvl);
                r.cost.text = maxed ? Loc.T("MAKS", "MAX") : cost.ToString();
                r.cost.fontSize = maxed ? 40 : cost >= 10000 ? 32 : cost >= 1000 ? 36 : 40;
                r.cost.color = maxed || affordable ? Color.white : new Color(1f, 0.62f, 0.55f);
                r.coinRt.gameObject.SetActive(!maxed);
                UiKit.Stretch(r.cost.rectTransform, new Vector2(maxed ? 0.08f : 0.38f, 0f), new Vector2(0.92f, 1f));
                r.button.interactable = !maxed;
                r.affordable = affordable;
                if (affordable) affordableCount++;
                r.maxed = maxed;
                if (r.shakeT <= 0f) r.buttonImage.color = maxed ? MaxedC : affordable ? BuyOn : BuyOff;
                r.barTarget = def.maxLevel > 0 ? Mathf.Clamp01((float)lvl / def.maxLevel) : 0f;
            }

            upgradeBadge.gameObject.SetActive(affordableCount > 0);
            upgradeBadgeText.text = affordableCount.ToString();

            RefreshLevels();
        }

        WorldTheme levelsTheme;
        MenuThemeView.Palette levelsPal;
        RectTransform levelsBossHost;

        void ApplyLevelsTheme(bool worldAvailable)
        {
            themeView.gameObject.SetActive(worldAvailable);
            menuBgRaw.enabled = !worldAvailable;
            if (worldAvailable) themeView.Apply(levelsTheme, levelsBossHost);
        }

        void RefreshLevels()
        {
            for (int i = map.childCount - 1; i >= 0; i--) Destroy(map.GetChild(i).gameObject);
            pulseNode = null;

            bool worldAvailable = db != null && db.IsWorldAvailable(worldPage);
            int first = GameDatabase.FirstLevelOf(worldPage);
            int done = 0;
            for (int i = 0; i < GameDatabase.LevelsPerWorld; i++) if (GameSession.IsCompleted(first + i) && worldAvailable) done++;
            wTitle.text = (worldPage + 1) + ". " + (db != null ? Loc.Upper(db.WorldName(worldPage)) : "");
            wSub.text = !worldAvailable ? Loc.T("YAKINDA", "COMING SOON") : done + " / " + GameDatabase.LevelsPerWorld + Loc.T(" seviye geçildi", " levels cleared");

            // Haritanın teması: görüntülenen dünyaya göre (kaydırınca değişir); henüz olmayan dünyada sade zemin
            levelsTheme = CurrentTheme(worldPage);
            var pal = MenuThemeView.Get(levelsTheme);
            levelsPal = pal;
            wTitle.color = worldAvailable ? pal.label : Color.white;
            wSub.color = worldAvailable ? Color.Lerp(pal.label, Color.white, 0.5f) : new Color(0.7f, 0.8f, 0.9f);
            levelsBossHost = null;

            // Yol üzerindeki düğüm konumları (aşağıdan yukarı, kıvrımlı)
            var pos = new Vector2[GameDatabase.LevelsPerWorld];
            float[] xs = { -250f, 0f, 250f, 0f };
            for (int i = 0; i < pos.Length; i++)
                pos[i] = new Vector2(i == pos.Length - 1 ? 0f : xs[i % xs.Length], -590f + i * 128f);

            // Önce yol parçaları (düğümlerin altında kalsın)
            for (int i = 0; i < pos.Length - 1; i++)
            {
                bool lit = worldAvailable && GameSession.IsCompleted(first + i);
                var a = pos[i]; var b = pos[i + 1];
                var litC = new Color(pal.label.r, pal.label.g, pal.label.b, 0.9f);
                var seg = UiKit.Fixed(map, "Path", (a + b) * 0.5f, new Vector2(Vector2.Distance(a, b), 18f),
                    lit ? litC : new Color(1f, 1f, 1f, 0.18f));
                seg.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                UiIcons.MakePill(seg).raycastTarget = false;
            }

            for (int i = 0; i < pos.Length; i++)
                MakeNode(first + i, pos[i], worldAvailable);

            if (current == Tab.Levels) ApplyLevelsTheme(worldAvailable);
        }

        void MakeNode(int level, Vector2 p, bool worldAvailable)
        {
            bool boss = GameSession.IsBoss(level);
            bool completed = worldAvailable && GameSession.IsCompleted(level);
            bool isNext = worldAvailable && !completed && GameSession.IsUnlocked(level);
            bool locked = !completed && !isNext;
            float size = boss ? 180f : 140f;

            var pal = levelsPal;
            Color c = locked ? NodeLocked
                : completed ? (boss ? Color.Lerp(NodeBoss, pal.emblem, 0.5f) : pal.emblem)
                : (boss ? NodeBoss : pal.emblem);

            // Boss düğümünün arkasında dünyanın süsü (alev tacı, buz tacı, kanatlar...)
            if (boss && worldAvailable)
            {
                levelsBossHost = UiKit.Fixed(map, "BossDeco", p, new Vector2(size, size), Color.clear);
                levelsBossHost.GetComponent<Image>().raycastTarget = false;
            }
            // Açık düğümlerin çevresinde temanın halkası
            if (!locked)
            {
                var halo = UiKit.Fixed(map, "Ring_" + level, p, new Vector2(size + 22f, size + 22f), pal.ring, UiKit.Circle);
                halo.GetComponent<Image>().raycastTarget = false;
            }
            var node = UiKit.Fixed(map, "Level_" + level, p, new Vector2(size, size), c, UiKit.Circle);
            var btn = node.gameObject.AddComponent<Button>();
            btn.targetGraphic = node.GetComponent<Image>();
            btn.onClick.AddListener(Sfx.Click);
            btn.onClick.AddListener(() => OnNode(level, locked, worldAvailable));

            if (completed)
            {
                // Buğulu görünüm + tik
                var fog = UiKit.Fixed(node, "Fog", Vector2.zero, new Vector2(size, size), new Color(0.9f, 0.95f, 1f, 0.28f), UiKit.Circle);
                fog.GetComponent<Image>().raycastTarget = false;
                var chk = UiKit.Fixed(node, "Check", Vector2.zero, new Vector2(size * 0.62f, size * 0.62f), new Color(0.15f, 0.75f, 0.3f), UiKit.Check);
                chk.GetComponent<Image>().raycastTarget = false;
            }
            else if (locked)
            {
                var lk = UiKit.Fixed(node, "Lock", new Vector2(0f, 4f), new Vector2(size * 0.5f, size * 0.5f), new Color(0.75f, 0.78f, 0.82f), UiKit.Lock);
                lk.GetComponent<Image>().raycastTarget = false;
            }
            else
            {
                UiKit.Label(node, level.ToString(), boss ? 72 : 64, TextAnchor.MiddleCenter);
                pulseNode = node;
            }

            // Kazanılan yıldızlar düğümün alt kenarında (kaydı olan seviyelerde)
            int st = completed ? SaveSystem.Data.GetStars(level) : 0;
            if (st > 0)
            {
                for (int k = 0; k < 3; k++)
                {
                    float sx = (k - 1) * 38f, sy = -size * 0.5f + (k == 1 ? -6f : 4f);
                    var back = UiKit.Fixed(map, "StarB", p + new Vector2(sx, sy), new Vector2(44f, 44f), new Color(0.03f, 0.05f, 0.1f, 0.85f), UiIcons.Star);
                    back.GetComponent<Image>().raycastTarget = false;
                    var fill = UiKit.Fixed(back, "Star", Vector2.zero, new Vector2(34f, 34f), k < st ? new Color(1f, 0.84f, 0.28f) : new Color(0.3f, 0.33f, 0.4f), UiIcons.Star);
                    fill.GetComponent<Image>().raycastTarget = false;
                }
            }

            // Seviye numarası / etiketi düğümün altında
            var lbl = UiKit.Fixed(map, "Label_" + level, p + new Vector2(0f, -size * 0.5f - (st > 0 ? 40f : 24f)), new Vector2(260f, 44f), Color.clear);
            lbl.GetComponent<Image>().raycastTarget = false;
            string txt = boss ? "BOSS · " + level : level.ToString();
            var lt = UiKit.Label(lbl, isNext ? Loc.T("OYNA · ", "PLAY · ") + txt : txt, 30, TextAnchor.MiddleCenter, locked ? new Color(0.72f, 0.76f, 0.8f) : Color.white);
            var lo = lt.gameObject.AddComponent<Outline>();
            lo.effectColor = new Color(0f, 0f, 0f, 0.6f);
            lo.effectDistance = new Vector2(2f, -2f);
        }

        void OnNode(int level, bool locked, bool worldAvailable)
        {
            if (!worldAvailable) { Toast(Loc.T("Bu dünya yakında geliyor", "This world is coming soon"), false); return; }
            if (locked) { Toast(Loc.T("Önce önceki seviyeleri geçmelisin", "Clear the previous levels first"), false); return; }
            GameSession.Play(level);
        }

        void Toast(string msg, bool positive)
        {
            toastText.text = msg;
            toastText.color = positive ? new Color(0.5f, 0.9f, 1f) : new Color(1f, 0.55f, 0.5f);
            toastT = 1.6f;
        }

        void OnDestroy()
        {
            // Oyun içi HUD eski fontuyla kalsın
            UiKit.FontOverride = null;
        }

        void Update()
        {
            questDotCheckT -= Time.unscaledDeltaTime;
            if (questDotCheckT <= 0f)
            {
                questDotCheckT = 1f;
                UpdateQuestDot();
            }
            if (questDot != null && questDot.activeSelf)
                questDot.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f));
            UpdateUpgradeFx(Mathf.Min(Time.unscaledDeltaTime, 0.05f));
            UpdateTabBar(Mathf.Min(Time.unscaledDeltaTime, 0.05f), Time.unscaledTime);
            if (archive != null && !StoryDialog.IsOpen)
            {
                var kb2 = Keyboard.current;
                if (kb2 != null && kb2.escapeKey.wasPressedThisFrame) CloseArchive();
            }
            if (guide != null && guide.activeSelf)
            {
                var kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame) CloseGuide();
                if (guideInT < 1f)
                {
                    guideInT = Mathf.Min(1f, guideInT + Mathf.Min(Time.unscaledDeltaTime, 0.05f) / 0.22f);
                    float t = guideInT - 1f;
                    guideCard.localScale = Vector3.one * (1f + 1.7f * t * t * t + 0.7f * t * t);
                }
            }
            if (resetLabelRef != null && resetArmedAt > 0f && Time.unscaledTime - resetArmedAt > 3f) { resetArmedAt = -10f; resetLabelRef.text = Loc.T("SIFIRLA", "RESET"); }
            if (toastT > 0f)
            {
                toastT -= Time.deltaTime;
                var c = toastText.color;
                c.a = Mathf.Clamp01(toastT / 0.4f);
                toastText.color = c;
            }

            if (pulseNode != null && current == Tab.Levels)
                pulseNode.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(Time.time * 4f));

            // Seviyeler sayfasında sağa-sola kaydırarak dünya değiştir
            if (current == Tab.Levels)
            {
                var p = Pointer.current;
                if (p != null)
                {
                    if (p.press.wasPressedThisFrame)
                    {
                        // yalnızca harita alanında başlayan dokunuş kaydırma sayılır (sekme çubuğu / üst bar değil)
                        swipeStart = p.position.ReadValue();
                        float yk = swipeStart.y / Mathf.Max(1f, Screen.height);
                        swipeTracking = yk > 0.11f && yk < 0.92f;
                    }
                    if (swipeTracking && p.press.wasReleasedThisFrame)
                    {
                        swipeTracking = false;
                        float dx = p.position.ReadValue().x - swipeStart.x;
                        if (Mathf.Abs(dx) > Screen.width * 0.18f) ChangeWorld(dx < 0f ? 1 : -1);
                    }
                }
            }
        }
    }
}
