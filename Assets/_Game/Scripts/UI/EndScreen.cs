using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StratejiGame
{
    /// <summary>
    /// Bölüm sonu ekranı (Hud kurar, Hud.ShowEnd açar).
    /// Kazanınca: dönen altın ışınlar + konfeti, "ZAFER!" kurdelesi, kalan cana göre 1-3 yıldız (tek tek patlayarak dolar),
    /// bölüm istatistikleri (öldürülen düşman, en büyük ordu, süre, boss), akarak sayılan ödül ve toplam altına uçan paralar,
    /// boss yenilip yeni dünya açıldıysa kutlama satırı. Kaybedince: kırmızı "YENİLDİN" kurdelesi, bölümün yüzde kaçına
    /// ulaşıldığı, istatistikler, yarım ödül. Tüm animasyonlar gerçek zamanla (Time.unscaledDeltaTime) oynar.
    /// </summary>
    public class EndScreen : MonoBehaviour
    {
        GameManager gm;
        MenuThemeView.Palette pal;

        RectTransform panel, card, ribbon, rays, glow, starRow, progressRow, statsBox, rewardBox, coinFrom, coinTo, buttons;
        Image dim, ribbonImg, cardImg, progressFill;
        CanvasGroup cardGroup, buttonsGroup, statsGroup;
        Text title, sub, rewardText, rewardNote, totalText, newWorldText, primaryText, progressText;
        readonly Image[] starFill = new Image[3];
        readonly Image[] starBurst = new Image[3];
        readonly List<Text> statLabels = new List<Text>();
        readonly List<Text> statValues = new List<Text>();
        readonly List<Image> statIcons = new List<Image>();
        readonly List<RectTransform> statRows = new List<RectTransform>();

        bool won, hasNext, open;
        int pendingStory = -1;
        int pendingChest = -1;
        RectTransform upBadge;
        Text upBadgeText;

        /// <summary>Altının yettiği yükseltme sayısı (en yüksek seviyedekiler hariç).</summary>
        static int AffordableUpgrades(int gold)
        {
            var cfg = GameSession.Db != null ? GameSession.Db.upgrades : null;
            if (cfg == null) return 0;
            int n = 0;
            foreach (var def in cfg.upgrades)
            {
                int lvl = SaveSystem.Data.GetUpgrade(def.id);
                if (lvl < def.maxLevel && gold >= def.Cost(lvl)) n++;
            }
            return n;
        }
        int reward, totalBefore, totalAfter, stars;
        float t;
        float rewardStart, coinStart = -1f;
        int coinsLanded;
        readonly bool[] starDone = new bool[3];

        class Bit { public RectTransform rt; public Image img; public Vector2 vel; public float spin, sway, phase, life, t; public Vector2 from, to; }
        readonly List<Bit> confetti = new List<Bit>();
        readonly List<Bit> coins = new List<Bit>();

        static readonly Color CardWin = new Color(0.06f, 0.1f, 0.19f, 0.97f);
        static readonly Color CardLose = new Color(0.13f, 0.07f, 0.1f, 0.97f);
        static readonly Color Gold = new Color(1f, 0.84f, 0.28f);

        // ------------------------------------------------------------------ kurulum
        public void Build(RectTransform root, GameManager g, MenuThemeView.Palette p)
        {
            gm = g;
            pal = p;
            panel = Hud.Panel(root, "EndPanel", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
            dim = panel.GetComponent<Image>();

            // Kazanınca kartın arkasında dönen ışınlar + parıltı
            glow = Hud.Fixed(panel, "Glow", Vector2.zero, new Vector2(1300f, 1300f), new Color(1f, 0.8f, 0.35f, 0.35f), UiIcons.Spark);
            glow.anchorMin = glow.anchorMax = new Vector2(0.5f, 0.72f);
            glow.GetComponent<Image>().raycastTarget = false;
            rays = Hud.Fixed(panel, "Rays", Vector2.zero, new Vector2(1250f, 1250f), new Color(1f, 0.85f, 0.4f, 0.28f), MenuArt.SunRays());
            rays.anchorMin = rays.anchorMax = new Vector2(0.5f, 0.72f);
            rays.GetComponent<Image>().raycastTarget = false;

            // Kart
            card = Hud.Panel(panel, "Card", new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.72f), CardWin);
            cardImg = UiIcons.Round(card, 50f);
            var csh = card.gameObject.AddComponent<Shadow>();
            csh.effectColor = new Color(0f, 0f, 0f, 0.5f);
            csh.effectDistance = new Vector2(0f, -14f);
            cardGroup = card.gameObject.AddComponent<CanvasGroup>();

            // Kurdele başlık (kartın üst kenarından taşar)
            ribbon = Hud.Panel(card, "Ribbon", new Vector2(0.1f, 0.905f), new Vector2(0.9f, 1.035f), Gold);
            ribbonImg = Hud.Pill(ribbon);
            var rsh = ribbon.gameObject.AddComponent<Shadow>();
            rsh.effectColor = new Color(0f, 0f, 0f, 0.45f);
            rsh.effectDistance = new Vector2(0f, -8f);
            Hud.Pill(Hud.Panel(ribbon, "Gloss", new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.9f), new Color(1f, 1f, 1f, 0.2f))).raycastTarget = false;
            title = Hud.Label(ribbon, "", 82, TextAnchor.MiddleCenter);
            Hud.TextShadow(title, 5f);

            // Yıldızlar (kazanınca) — ortadaki daha büyük ve yüksekte
            starRow = Hud.Panel(card, "Stars", new Vector2(0f, 0.7f), new Vector2(1f, 0.89f), Color.clear);
            starRow.GetComponent<Image>().raycastTarget = false;
            float[] sx = { -230f, 0f, 230f }, sy = { -10f, 22f, -10f }, ss = { 170f, 215f, 170f };
            for (int i = 0; i < 3; i++)
            {
                var back = Hud.Fixed(starRow, "StarBack" + i, new Vector2(sx[i], sy[i]), new Vector2(ss[i], ss[i]), new Color(0.03f, 0.05f, 0.1f, 0.9f), UiIcons.Star);
                back.GetComponent<Image>().raycastTarget = false;
                back.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 12f : i == 2 ? -12f : 0f);
                var inner = Hud.Fixed(back, "Slot", Vector2.zero, new Vector2(ss[i] * 0.8f, ss[i] * 0.8f), new Color(0.2f, 0.24f, 0.32f), UiIcons.Star);
                inner.GetComponent<Image>().raycastTarget = false;
                var burst = Hud.Fixed(back, "Burst", Vector2.zero, new Vector2(ss[i] * 2f, ss[i] * 2f), new Color(1f, 0.9f, 0.5f, 0f), UiIcons.Spark);
                starBurst[i] = burst.GetComponent<Image>();
                starBurst[i].raycastTarget = false;
                var fill = Hud.Fixed(back, "Fill", Vector2.zero, new Vector2(ss[i] * 0.86f, ss[i] * 0.86f), Gold, UiIcons.Star);
                starFill[i] = fill.GetComponent<Image>();
                starFill[i].raycastTarget = false;
                var shine = Hud.Fixed(fill, "Shine", new Vector2(-ss[i] * 0.08f, ss[i] * 0.1f), new Vector2(ss[i] * 0.34f, ss[i] * 0.34f), new Color(1f, 1f, 0.9f, 0.55f), UiIcons.Star);
                shine.GetComponent<Image>().raycastTarget = false;
            }

            // Kaybedince: bölümün yüzde kaçına ulaşıldı
            progressRow = Hud.Panel(card, "Progress", new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.87f), Color.clear);
            progressRow.GetComponent<Image>().raycastTarget = false;
            progressText = Hud.Label(progressRow, "", 40, TextAnchor.MiddleCenter);
            Hud.Stretch(progressText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f));
            var pbg = Hud.Pill(Hud.Panel(progressRow, "Bar", new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.36f), new Color(0f, 0f, 0f, 0.5f))).rectTransform;
            progressFill = Hud.Pill(Hud.Panel(pbg, "Fill", Vector2.zero, new Vector2(0.5f, 1f), new Color(0.9f, 0.3f, 0.3f)));

            // Alt başlık: seviye · dünya
            sub = Hud.Label(card, "", 34, TextAnchor.MiddleCenter);
            Hud.Stretch(sub.rectTransform, new Vector2(0f, 0.635f), new Vector2(1f, 0.69f));
            Hud.TextShadow(sub, 3f);

            // İstatistikler
            statsBox = Hud.Panel(card, "Stats", new Vector2(0.07f, 0.385f), new Vector2(0.93f, 0.625f), new Color(0f, 0f, 0f, 0.28f));
            UiIcons.Round(statsBox, 30f).raycastTarget = false;
            statsGroup = statsBox.gameObject.AddComponent<CanvasGroup>();

            // Ödül kutusu: solda +ödül, sağda toplam
            rewardBox = Hud.Panel(card, "Reward", new Vector2(0.07f, 0.235f), new Vector2(0.93f, 0.365f), new Color(0f, 0f, 0f, 0.35f));
            UiIcons.Round(rewardBox, 30f).raycastTarget = false;
            coinFrom = Hud.Coin(rewardBox, new Vector2(0.11f, 0.5f), 92f);
            rewardText = Hud.Label(rewardBox, "", 66, TextAnchor.MiddleLeft);
            // sayı, soldaki para ikonuyla aynı yatay eksende (kutunun tam ortasında)
            Hud.Stretch(rewardText.rectTransform, new Vector2(0.2f, 0f), new Vector2(0.62f, 1f));
            rewardText.color = Gold;
            Hud.TextShadow(rewardText, 4f);
            rewardNote = Hud.Label(rewardBox, "", 24, TextAnchor.MiddleLeft);
            Hud.Stretch(rewardNote.rectTransform, new Vector2(0.2f, 0.02f), new Vector2(0.7f, 0.3f));
            rewardNote.color = new Color(1f, 1f, 1f, 0.65f);
            var totLbl = Hud.Label(rewardBox, Loc.T("TOPLAM", "TOTAL"), 26, TextAnchor.MiddleCenter);
            Hud.Stretch(totLbl.rectTransform, new Vector2(0.64f, 0.55f), new Vector2(0.98f, 0.92f));
            totLbl.color = new Color(1f, 1f, 1f, 0.7f);
            totalText = Hud.Label(rewardBox, "", 44, TextAnchor.MiddleCenter);
            Hud.Stretch(totalText.rectTransform, new Vector2(0.64f, 0.08f), new Vector2(0.98f, 0.58f));
            totalText.color = Gold;
            Hud.TextShadow(totalText, 3f);
            coinTo = totalText.rectTransform;

            // Yeni dünya kutlaması
            newWorldText = Hud.Label(card, "", 31, TextAnchor.MiddleCenter);
            newWorldText.lineSpacing = 0.9f;
            Hud.Stretch(newWorldText.rectTransform, new Vector2(0f, 0.17f), new Vector2(1f, 0.225f));
            newWorldText.color = Gold;
            Hud.TextShadow(newWorldText, 3f);

            // Düğmeler
            buttons = Hud.Panel(card, "Buttons", new Vector2(0.06f, 0.035f), new Vector2(0.94f, 0.16f), Color.clear);
            buttons.GetComponent<Image>().raycastTarget = false;
            buttonsGroup = buttons.gameObject.AddComponent<CanvasGroup>();
            Hud.MakeButton(buttons, "PrimaryButton", new Vector2(0f, 0f), new Vector2(0.45f, 1f), pal.button, "", 38, OnPrimary, out primaryText);
            // Yükseltmeler: ana menüye döner ve doğrudan Yükseltmeler sekmesini açar
            Hud.MakeButton(buttons, "UpgradeButton", new Vector2(0.475f, 0.06f), new Vector2(0.745f, 0.94f), new Color(0.2f, 0.62f, 0.3f), Loc.T("YÜKSELT", "UPGRADE"), 30, () =>
            {
                MainMenuUI.OpenUpgradesNext();
                gm.ReturnToMenu();
            }, out var upLabel);
            Hud.Stretch(upLabel.rectTransform, new Vector2(0.3f, 0f), new Vector2(0.97f, 1f));
            var upIcon = Hud.Fixed((RectTransform)upLabel.transform.parent, "Icon", Vector2.zero, new Vector2(40f, 40f), Color.white, UiIcons.UpArrow);
            upIcon.anchorMin = upIcon.anchorMax = new Vector2(0.19f, 0.5f);
            upIcon.GetComponent<Image>().raycastTarget = false;
            // kaç yükseltmeye altının yettiğini gösteren kırmızı rozet (menüdeki sekme rozetiyle aynı hesap)
            upBadge = Hud.Fixed((RectTransform)upLabel.transform.parent, "Badge", Vector2.zero, new Vector2(52f, 52f), new Color(0.9f, 0.16f, 0.16f), UiKit.Circle);
            upBadge.anchorMin = upBadge.anchorMax = new Vector2(0.93f, 0.9f);
            upBadge.GetComponent<Image>().raycastTarget = false;
            var bsh = upBadge.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            bsh.effectColor = new Color(0f, 0f, 0f, 0.4f);
            bsh.effectDistance = new Vector2(0f, -3f);
            upBadgeText = Hud.Label(upBadge, "", 30, TextAnchor.MiddleCenter);
            upBadge.gameObject.SetActive(false);
            Hud.MakeButton(buttons, "MenuButton", new Vector2(0.77f, 0.06f), new Vector2(1f, 0.94f), new Color(0.22f, 0.32f, 0.48f), Loc.T("MENÜ", "MENU"), 34, gm.ReturnToMenu);

            panel.gameObject.SetActive(false);
        }

        void OnPrimary()
        {
            if (won && hasNext) gm.PlayNextLevel();
            else gm.RetryLevel();
        }

        void AddStat(Sprite icon, Color iconColor, string label, string value)
        {
            int i = statRows.Count;
            var row = Hud.Panel(statsBox, "Row" + i, Vector2.zero, Vector2.one, Color.clear);
            row.GetComponent<Image>().raycastTarget = false;
            var badge = Hud.Fixed(row, "Icon", Vector2.zero, new Vector2(58f, 58f), iconColor, UiKit.Circle);
            badge.anchorMin = badge.anchorMax = new Vector2(0.08f, 0.5f);
            badge.GetComponent<Image>().raycastTarget = false;
            var ic = Hud.Fixed(badge, "I", Vector2.zero, new Vector2(36f, 36f), Color.white, icon);
            ic.GetComponent<Image>().raycastTarget = false;
            var l = Hud.Label(row, label, 32, TextAnchor.MiddleLeft);
            Hud.Stretch(l.rectTransform, new Vector2(0.16f, 0f), new Vector2(0.7f, 1f));
            l.color = new Color(0.82f, 0.88f, 0.95f);
            var v = Hud.Label(row, value, 38, TextAnchor.MiddleRight);
            Hud.Stretch(v.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.95f, 1f));
            Hud.TextShadow(v, 3f);
            statRows.Add(row);
            statLabels.Add(l);
            statValues.Add(v);
            statIcons.Add(badge.GetComponent<Image>());
        }

        void LayoutStats()
        {
            int n = statRows.Count;
            for (int i = 0; i < n; i++)
            {
                float h = 1f / n;
                Hud.Stretch(statRows[i], new Vector2(0f, 1f - (i + 1) * h), new Vector2(1f, 1f - i * h));
            }
        }

        // ------------------------------------------------------------------ açılış
        public void Show(bool didWin, int rewardGold, bool replay)
        {
            won = didWin;
            reward = rewardGold;
            totalAfter = gm.TotalGold;
            totalBefore = totalAfter - reward;
            hasNext = gm.HasNextLevel && GameSession.IsUnlocked(gm.Level + 1);
            stars = won ? Mathf.Clamp(gm.LastStars, 1, 3) : 0;

            // istatistik satırları
            foreach (var r in statRows) Destroy(r.gameObject);
            statRows.Clear(); statLabels.Clear(); statValues.Clear(); statIcons.Clear();
            AddStat(UiIcons.Skull, new Color(0.75f, 0.25f, 0.25f), Loc.T("Öldürülen düşman", "Enemies killed"), gm.KillCount.ToString());
            AddStat(UiIcons.Troops, new Color(0.3f, 0.55f, 0.95f), Loc.T("En büyük ordu", "Largest army"), gm.RunMaxArmy.ToString());
            int sec = Mathf.FloorToInt(gm.RunTime);
            AddStat(UiIcons.Clock, new Color(0.55f, 0.45f, 0.9f), Loc.T("Süre", "Time"), (sec / 60) + ":" + (sec % 60).ToString("00"));
            if (gm.Endless && gm.Director.EndlessBossesKilled > 0)
                AddStat(UiIcons.Skull, new Color(0.85f, 0.6f, 0.15f), Loc.T("Yenilen boss", "Bosses defeated"), gm.Director.EndlessBossesKilled.ToString());
            if (gm.IsBossLevel && won && gm.CurrentWorld != null)
                AddStat(UiIcons.Skull, new Color(0.85f, 0.6f, 0.15f), Loc.T("Yenilen boss", "Boss defeated"), Loc.Name(gm.CurrentWorld.bossName));
            LayoutStats();

            title.text = won ? Loc.T("ZAFER!", "VICTORY!") : Loc.T("YENİLDİN", "DEFEATED");
            ribbonImg.color = won ? new Color(0.95f, 0.62f, 0.14f) : new Color(0.62f, 0.14f, 0.18f);
            cardImg.color = won ? CardWin : CardLose;
            string world = gm.CurrentWorld != null ? Loc.Upper(Loc.Name(gm.CurrentWorld.displayName)) : "";
            sub.text = gm.Endless ? Loc.T("SONSUZ SAVUNMA  ·  ", "ENDLESS DEFENSE  ·  ") + world : Loc.T("SEVİYE ", "LEVEL ") + gm.Level + "  ·  " + world;
            sub.color = won ? pal.label : new Color(1f, 0.7f, 0.7f);

            starRow.gameObject.SetActive(won);
            progressRow.gameObject.SetActive(!won);
            if (gm.Endless)
            {
                // Sonsuz Savunma: ulaşılan dalga, rekor çubuğu (önceki rekora göre)
                int wave = gm.Director.Wave;
                title.text = Loc.T("DALGA ", "WAVE ") + wave;
                ribbonImg.color = gm.EndlessNewRecord ? new Color(0.95f, 0.62f, 0.14f) : new Color(0.42f, 0.24f, 0.7f);
                progressText.text = gm.EndlessNewRecord
                    ? (gm.EndlessPrevBest > 0 ? Loc.T("Önceki rekor: Dalga ", "Previous best: Wave ") + gm.EndlessPrevBest : Loc.T("İlk rekorun!", "Your first record!"))
                    : Loc.T("Rekor: Dalga ", "Best: Wave ") + gm.Save.endlessBestWave;
                progressFill.rectTransform.anchorMax = new Vector2(0f, 1f);
                progressFill.color = gm.EndlessNewRecord ? Gold : new Color(0.62f, 0.45f, 0.95f);
            }
            else if (!won)
            {
                progressFill.color = new Color(0.9f, 0.3f, 0.3f);
                float prog = Mathf.Clamp01(gm.Director.Progress);
                progressText.text = Loc.EN ? "You reached " + Mathf.RoundToInt(prog * 100f) + "% of the level" : "Bölümün %" + Mathf.RoundToInt(prog * 100f) + "'ine ulaştın";
                progressFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            }
            for (int i = 0; i < 3; i++)
            {
                starFill[i].rectTransform.localScale = Vector3.zero;
                starDone[i] = false;
                var bc = starBurst[i].color; bc.a = 0f; starBurst[i].color = bc;
            }

            rewardText.text = "+0";
            rewardNote.text = replay ? Loc.T("tekrar oynama: yarım ödül", "replay: half reward") : "";
            totalText.text = totalBefore.ToString();
            int canBuy = AffordableUpgrades(totalAfter);
            upBadge.gameObject.SetActive(canBuy > 0);
            upBadgeText.text = canBuy.ToString();
            primaryText.text = won ? (hasNext ? Loc.T("SONRAKİ SEVİYE", "NEXT LEVEL") : Loc.T("TEKRAR OYNA", "PLAY AGAIN")) : Loc.T("TEKRAR DENE", "TRY AGAIN");

            // boss yenildi ve yeni dünya açıldı mı?
            newWorldText.text = "";
            pendingStory = -1;
            pendingChest = -1;
            if (won && gm.IsBossLevel && !replay)
            {
                // diyar temizlendi: zafer satırı + birazdan Komutan Atilla'nın konuşması (ilk seferde)
                int wi = GameDatabase.WorldIndexOf(gm.Level);
                newWorldText.text = StoryData.ClearedLine(wi, GameSession.Db);
                if (!StoryData.Seen(StoryData.WorldBit(wi))) pendingStory = wi;
                if (!PromotionChest.Claimed(wi)) pendingChest = wi;
            }
            else if (won && replay && gm.NewBestStars) newWorldText.text = Loc.T("YENİ YILDIZ REKORU!", "NEW STAR RECORD!");
            if (gm.Endless && gm.EndlessNewRecord) newWorldText.text = Loc.T("YENİ REKOR!", "NEW RECORD!");

            rays.gameObject.SetActive(won);
            glow.gameObject.SetActive(won);
            ClearBits(confetti);
            ClearBits(coins);
            coinStart = -1f;
            coinsLanded = 0;
            rewardStart = won ? 1.35f : 0.9f;

            t = 0f;
            open = true;
            panel.gameObject.SetActive(true);
            Apply(0f);
            if (won || (gm.Endless && gm.EndlessNewRecord)) SpawnConfetti(60);
        }

        void ClearBits(List<Bit> list)
        {
            foreach (var b in list) if (b.rt != null) Destroy(b.rt.gameObject);
            list.Clear();
        }

        // ------------------------------------------------------------------ animasyon
        void Update()
        {
            if (!open) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            t += dt;
            Apply(dt);
            if (pendingStory >= 0 && t > 2.8f)
            {
                int wi = pendingStory;
                int chest = pendingChest;
                pendingStory = -1;
                pendingChest = -1;
                StoryData.MarkSeen(StoryData.WorldBit(wi));
                // konuşma bitince terfi sandığı açılır
                StoryDialog.Talk(StoryData.WorldEnd(wi), Loc.T("TERFİ: ", "PROMOTED: ") + StoryData.Rank(wi), () => OpenChest(chest));
            }
            else if (pendingStory < 0 && pendingChest >= 0 && t > 2.8f && !StoryDialog.IsOpen)
            {
                int chest = pendingChest;
                pendingChest = -1;
                OpenChest(chest);
            }
        }

        /// <summary>Terfi sandığı: altını kayda ekler, sandığı gösterir; kapanınca toplam altın ve YÜKSELT rozeti güncellenir.</summary>
        void OpenChest(int worldIndex)
        {
            if (worldIndex < 0) return;
            int g = PromotionChest.Claim(worldIndex);
            if (g <= 0) return;
            PromotionChest.Show(g, PromotionChest.RankLine(worldIndex), () =>
            {
                if (this == null) return;
                totalBefore = totalAfter = gm.TotalGold;
                totalText.text = totalAfter.ToString();
                int canBuy = AffordableUpgrades(totalAfter);
                upBadge.gameObject.SetActive(canBuy > 0);
                upBadgeText.text = canBuy.ToString();
            });
        }

        static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            float c1 = 1.70158f, c3 = c1 + 1f;
            float k = x - 1f;
            return 1f + c3 * k * k * k + c1 * k * k;
        }

        void Apply(float dt)
        {
            // karartma + kart
            dim.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, won ? 0.62f : 0.72f, Mathf.Clamp01(t / 0.25f)));
            float cp = Mathf.Clamp01((t - 0.05f) / 0.35f);
            card.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, EaseOutBack(cp));
            cardGroup.alpha = Mathf.Clamp01(cp * 2.5f);

            // kurdele yukarıdan düşer
            float rp = Mathf.Clamp01((t - 0.3f) / 0.3f);
            ribbon.anchoredPosition = new Vector2(0f, (1f - EaseOutBack(rp)) * 160f);
            ribbon.localScale = Vector3.one * Mathf.LerpUnclamped(1.3f, 1f, EaseOutBack(rp));

            if (won)
            {
                rays.localRotation = Quaternion.Euler(0f, 0f, t * 12f);
                float ra = Mathf.Clamp01(t / 0.6f);
                rays.localScale = Vector3.one * (0.6f + 0.4f * ra + 0.03f * Mathf.Sin(t * 2f));
                glow.localScale = Vector3.one * (0.9f + 0.06f * Mathf.Sin(t * 1.7f));
                // yıldızlar sırayla
                for (int i = 0; i < 3; i++)
                {
                    float st = 0.65f + i * 0.28f;
                    var fr = starFill[i].rectTransform;
                    if (i < stars && t >= st)
                    {
                        if (!starDone[i]) { starDone[i] = true; Sfx.Play(Snd.GatePower); Haptics.Vibrate(20); }
                        float k = Mathf.Clamp01((t - st) / 0.32f);
                        fr.localScale = Vector3.one * (k < 0.6f ? Mathf.Lerp(0f, 1.35f, k / 0.6f) : Mathf.Lerp(1.35f, 1f, (k - 0.6f) / 0.4f));
                        fr.localRotation = Quaternion.Euler(0f, 0f, (1f - k) * -40f);
                        float bk = Mathf.Clamp01((t - st) / 0.5f);
                        starBurst[i].color = new Color(1f, 0.9f, 0.5f, (1f - bk) * 0.9f);
                        starBurst[i].rectTransform.localScale = Vector3.one * (0.4f + bk * 1.1f);
                    }
                    else if (i >= stars) fr.localScale = Vector3.zero;
                }
            }
            else
            {
                float pk = Mathf.Clamp01((t - 0.5f) / 0.7f);
                float prog = gm.Endless ? Mathf.Clamp01(gm.Director.Wave / (float)Mathf.Max(1, gm.EndlessNewRecord ? gm.Director.Wave : gm.Save.endlessBestWave)) : Mathf.Clamp01(gm.Director.Progress);
                progressFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.04f, prog * (1f - (1f - pk) * (1f - pk))), 1f);
            }

            // istatistikler sırayla belirir
            statsGroup.alpha = Mathf.Clamp01((t - 0.45f) / 0.3f);
            for (int i = 0; i < statRows.Count; i++)
            {
                float k = Mathf.Clamp01((t - 0.5f - i * 0.1f) / 0.25f);
                statRows[i].localScale = new Vector3(1f, 1f, 1f);
                statRows[i].anchoredPosition = new Vector2((1f - EaseOutBack(k)) * 120f, 0f);
                var c = statValues[i].color; c.a = k; statValues[i].color = c;
            }

            // ödül akarak sayılır, sonra paralar toplama uçar
            float rk = Mathf.Clamp01((t - rewardStart) / 0.8f);
            float re = 1f - (1f - rk) * (1f - rk);
            rewardText.text = "+" + Mathf.RoundToInt(reward * re);
            rewardText.rectTransform.localScale = Vector3.one * (rk > 0f && rk < 1f ? 1.08f : 1f);
            if (rk >= 1f && coinStart < 0f)
            {
                coinStart = t;
                if (reward > 0) { SpawnCoins(Mathf.Clamp(reward / 20, 4, 9)); Sfx.Play(Snd.Purchase); }
                else coinsLanded = 1;
            }
            int target = coins.Count > 0 ? Mathf.RoundToInt(Mathf.Lerp(totalBefore, totalAfter, (float)coinsLanded / coins.Count)) : (coinStart >= 0f ? totalAfter : totalBefore);
            totalText.text = target.ToString();

            // düğmeler
            float bp = Mathf.Clamp01((t - 0.55f) / 0.3f);
            buttonsGroup.alpha = bp;
            buttons.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(bp));

            // yeni dünya satırı nabız atar
            if (newWorldText.text.Length > 0)
            {
                float nk = Mathf.Clamp01((t - 1.2f) / 0.3f);
                newWorldText.rectTransform.localScale = Vector3.one * (nk * (1f + 0.05f * Mathf.Sin(t * 5f)));
            }

            UpdateConfetti(dt);
            UpdateCoins(dt);
        }

        // ------------------------------------------------------------------ konfeti ve paralar
        static readonly Color[] ConfettiColors =
        {
            new Color(1f, 0.82f, 0.25f), new Color(0.35f, 0.75f, 1f), new Color(1f, 0.4f, 0.45f),
            new Color(0.45f, 0.9f, 0.45f), new Color(0.8f, 0.5f, 1f), Color.white,
        };

        void SpawnConfetti(int n)
        {
            var r = panel.rect;
            for (int i = 0; i < n; i++)
            {
                var rt = Hud.Fixed(panel, "Confetti", Vector2.zero, new Vector2(Random.Range(14f, 22f), Random.Range(24f, 38f)), ConfettiColors[i % ConfettiColors.Length], null);
                rt.SetSiblingIndex(card.GetSiblingIndex()); // kartın arkasında
                var img = UiIcons.Round(rt.GetComponent<Image>(), 6f);
                img.raycastTarget = false;
                rt.anchoredPosition = new Vector2(Random.Range(-0.5f, 0.5f) * r.width, r.height * 0.5f + Random.Range(20f, r.height * 0.6f));
                confetti.Add(new Bit
                {
                    rt = rt, img = img,
                    vel = new Vector2(Random.Range(-40f, 40f), -Random.Range(260f, 520f)),
                    spin = Random.Range(-360f, 360f), sway = Random.Range(30f, 90f), phase = Random.value * 10f
                });
            }
        }

        void UpdateConfetti(float dt)
        {
            if (confetti.Count == 0) return;
            var r = panel.rect;
            foreach (var b in confetti)
            {
                b.t += dt;
                var p = b.rt.anchoredPosition + b.vel * dt;
                p.x += Mathf.Sin(b.t * 3f + b.phase) * b.sway * dt;
                // ilk 3 saniye yağmaya devam eder
                if (p.y < -r.height * 0.55f && t < 3f) p.y = r.height * 0.55f + Random.Range(0f, 80f);
                b.rt.anchoredPosition = p;
                b.rt.localRotation = Quaternion.Euler(0f, 0f, b.t * b.spin);
                // dönerken incelip kalınlaşır (3B kâğıt etkisi)
                b.rt.localScale = new Vector3(Mathf.Abs(Mathf.Cos(b.t * 4f + b.phase)) * 0.8f + 0.2f, 1f, 1f);
            }
        }

        Vector2 PanelPos(RectTransform target) => (Vector2)panel.InverseTransformPoint(target.TransformPoint(target.rect.center));

        void SpawnCoins(int n)
        {
            var from = PanelPos(coinFrom);
            var to = PanelPos(coinTo);
            for (int i = 0; i < n; i++)
            {
                var c = Hud.Coin(panel, new Vector2(0.5f, 0.5f), 54f);
                c.anchoredPosition = from;
                c.localScale = Vector3.zero;
                coins.Add(new Bit { rt = c, from = from + Random.insideUnitCircle * 20f, to = to, t = -i * 0.07f, life = 0.5f, phase = Random.Range(-1f, 1f) });
            }
        }

        void UpdateCoins(float dt)
        {
            foreach (var b in coins)
            {
                if (b.life <= 0f) continue;
                b.t += dt;
                if (b.t < 0f) continue;
                float k = Mathf.Clamp01(b.t / b.life);
                float e = k * k * (3f - 2f * k);
                var mid = (b.from + b.to) * 0.5f + new Vector2(b.phase * 80f, 140f);
                b.rt.anchoredPosition = Vector2.Lerp(Vector2.Lerp(b.from, mid, e), Vector2.Lerp(mid, b.to, e), e);
                b.rt.localScale = Vector3.one * (k < 0.15f ? k / 0.15f : Mathf.Lerp(1f, 0.6f, k));
                if (k >= 1f)
                {
                    b.life = 0f;
                    b.rt.gameObject.SetActive(false);
                    coinsLanded++;
                    totalText.rectTransform.localScale = Vector3.one * 1.18f;
                }
            }
            var ts = totalText.rectTransform.localScale.x;
            if (ts > 1f) totalText.rectTransform.localScale = Vector3.one * Mathf.MoveTowards(ts, 1f, dt * 1.5f);
        }
    }
}
