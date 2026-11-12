using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StratejiGame
{
    /// <summary>
    /// Ana menüdeki "GÖREVLER" penceresi: üstte 7 günlük giriş ödülü serisi, altta günün 3 görevi ve
    /// hepsi bitince açılan bonus sandığı. Kendi tuvalinde kurulur (terfi sandığı bunun da üstünde açılır).
    /// Altın, ödül alındığı anda kayda yazılır; onGold(eski, yeni) menüdeki altın sayacını canlandırır.
    /// </summary>
    public class DailyPanel : MonoBehaviour
    {
        static readonly Color Dim = new Color(0.01f, 0.02f, 0.05f, 0.92f);
        static readonly Color CardC = new Color(0.05f, 0.1f, 0.18f, 1f);
        static readonly Color RowC = new Color(0.09f, 0.15f, 0.25f, 1f);
        static readonly Color RowDoneC = new Color(0.1f, 0.22f, 0.17f, 1f);
        static readonly Color GoldC = new Color(1f, 0.82f, 0.26f);
        static readonly Color GoldDark = new Color(0.55f, 0.36f, 0.06f);
        static readonly Color GreenC = new Color(0.2f, 0.66f, 0.3f);
        static readonly Color GreyC = new Color(0.24f, 0.27f, 0.32f);
        static readonly Color OrangeC = new Color(1f, 0.6f, 0.1f);
        static readonly Color SubC = new Color(0.65f, 0.75f, 0.85f);
        static readonly Color BarBg = new Color(0.03f, 0.05f, 0.09f, 1f);

        public static bool IsOpen => current != null;
        static DailyPanel current;

        Action<int, int> onGold;
        Action onClose;
        RectTransform root, card;
        float t, tick;
        int builtDay;

        class DayTile { public RectTransform rt; public Image bg, check, ring; public Text label, amount; }
        readonly DayTile[] days = new DayTile[DailyQuests.DayCount];
        Button dailyBtn;
        Image dailyBtnImg;
        Text dailyBtnText, resetText;

        class QuestRow { public Image bg, fill, check; public Text desc, prog, btnText; public Button btn; public Image btnImg; public RectTransform btnRt, fillRt; }
        readonly List<QuestRow> quests = new List<QuestRow>();
        Image bonusBg, bonusBtnImg;
        Button bonusBtn;
        Text bonusText, bonusBtnText;

        class Coin { public RectTransform rt; public Vector2 vel; public float life, spin; }
        readonly List<Coin> coins = new List<Coin>();

        /// <summary>Pencereyi açar.</summary>
        public static void Open(Action<int, int> onGold, Action onClose)
        {
            if (current != null) Destroy(current.gameObject);
            DailyQuests.EnsureToday();
            var go = new GameObject("DailyPanel");
            current = go.AddComponent<DailyPanel>();
            current.onGold = onGold;
            current.onClose = onClose;
            current.Build();
        }

        // ------------------------------------------------------------------ yardımcılar
        Text Label(RectTransform parent, string text, int size, TextAnchor a, Color c, Vector2 aMin, Vector2 aMax)
        {
            var l = UiKit.Label(parent, text, size, a, c);
            if (UiKit.MenuFont != null) { l.font = UiKit.MenuFont; l.fontStyle = FontStyle.Normal; }
            UiKit.Stretch(l.rectTransform, aMin, aMax);
            return l;
        }

        static void Shadow(Graphic g, float d, float a = 0.5f)
        {
            var s = g.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, a);
            s.effectDistance = new Vector2(0f, -d);
        }

        static Image NoRay(RectTransform rt) { var i = rt.GetComponent<Image>(); i.raycastTarget = false; return i; }

        Button PillButton(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Color c, string text, int size, UnityEngine.Events.UnityAction a, out Text label)
        {
            var b = UiKit.Button(parent, name, aMin, aMax, c, text, size, a, out label);
            UiIcons.MakePill(b.GetComponent<Image>());
            if (UiKit.MenuFont != null) { label.font = UiKit.MenuFont; label.fontStyle = FontStyle.Normal; }
            Shadow(label, 3f);
            return b;
        }

        static string Clock(TimeSpan s) => string.Format("{0:00}:{1:00}:{2:00}", (int)s.TotalHours, s.Minutes, s.Seconds);

        // ------------------------------------------------------------------ kurulum
        void Build()
        {
            builtDay = DailyQuests.Today;
            root = UiKit.CreateCanvas(transform, "DailyCanvas", 300);
            var dim = UiKit.Panel(root, "Dim", Vector2.zero, Vector2.one, Dim);
            var db = dim.gameObject.AddComponent<Button>();
            db.transition = Selectable.Transition.None;
            db.onClick.AddListener(Close);

            card = UiKit.Panel(root, "Card", new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), CardC);
            card.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // karta dokunmak kapatmasın
            UiIcons.Round(card, 44f);
            Shadow(card.GetComponent<Image>(), 12f);

            // ---------------- günlük ödül
            var title = Label(card, Loc.T("GÜNLÜK ÖDÜL", "DAILY REWARD"), 66, TextAnchor.MiddleCenter, GoldC, new Vector2(0f, 0.935f), new Vector2(1f, 0.99f));
            Shadow(title, 5f);
            Label(card, Loc.T("Her gün gel, seriyi bozma! 7. gün büyük sandık.", "Come back daily! Big chest on day 7."), 28, TextAnchor.MiddleCenter, SubC, new Vector2(0f, 0.905f), new Vector2(1f, 0.935f));

            // 7 kutu: üstte 4, altta 3 (7. gün geniş)
            for (int i = 0; i < DailyQuests.DayCount; i++)
            {
                float x0, x1, y0, y1;
                if (i < 4) { x0 = 0.04f + i * 0.235f; x1 = x0 + 0.215f; y0 = 0.775f; y1 = 0.895f; }
                else if (i < 6) { x0 = 0.04f + (i - 4) * 0.235f; x1 = x0 + 0.215f; y0 = 0.645f; y1 = 0.765f; }
                else { x0 = 0.51f; x1 = 0.96f; y0 = 0.645f; y1 = 0.765f; }
                days[i] = MakeDay(i, new Vector2(x0, y0), new Vector2(x1, y1));
            }

            dailyBtn = PillButton(card, "DailyClaim", new Vector2(0.2f, 0.57f), new Vector2(0.8f, 0.63f), GreenC, "", 44, ClaimDaily, out dailyBtnText);
            dailyBtnImg = dailyBtn.GetComponent<Image>();

            // ---------------- görevler
            NoRay(UiKit.Panel(card, "Line", new Vector2(0.06f, 0.553f), new Vector2(0.94f, 0.556f), new Color(GoldC.r, GoldC.g, GoldC.b, 1f), UiIcons.FadeLine));
            var qt = Label(card, Loc.T("GÜNLÜK GÖREVLER", "DAILY QUESTS"), 50, TextAnchor.MiddleLeft, GoldC, new Vector2(0.05f, 0.5f), new Vector2(0.7f, 0.548f));
            Shadow(qt, 4f);
            resetText = Label(card, "", 26, TextAnchor.MiddleRight, SubC, new Vector2(0.5f, 0.5f), new Vector2(0.95f, 0.548f));

            for (int i = 0; i < DailyQuests.Count; i++)
            {
                float yMax = 0.49f - i * 0.11f;
                quests.Add(MakeQuest(i, new Vector2(0.04f, yMax - 0.1f), new Vector2(0.96f, yMax)));
            }

            // bonus sandığı
            var brt = UiKit.Panel(card, "Bonus", new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.16f), RowC);
            bonusBg = NoRay(brt);
            UiIcons.Round(bonusBg, 26f);
            var chest = UiKit.Fixed(brt, "ChestIcon", Vector2.zero, new Vector2(70f, 56f), new Color(0.5f, 0.27f, 0.11f));
            chest.anchorMin = chest.anchorMax = new Vector2(0.07f, 0.5f);
            UiIcons.Round(NoRay(chest), 12f);
            var band = UiKit.Fixed(chest, "Band", Vector2.zero, new Vector2(74f, 10f), GoldC);
            NoRay(band);
            var lk = UiKit.Fixed(chest, "Lock", new Vector2(0f, 0f), new Vector2(18f, 22f), GoldC);
            NoRay(lk);
            bonusText = Label(brt, "", 30, TextAnchor.MiddleLeft, Color.white, new Vector2(0.14f, 0f), new Vector2(0.7f, 1f));
            bonusBtn = PillButton(brt, "BonusClaim", new Vector2(0.72f, 0.14f), new Vector2(0.98f, 0.86f), GreenC, "", 32, ClaimBonus, out bonusBtnText);
            bonusBtnImg = bonusBtn.GetComponent<Image>();

            Text cl;
            PillButton(card, "Close", new Vector2(0.28f, 0.02f), new Vector2(0.72f, 0.08f), new Color(0.12f, 0.45f, 0.85f), Loc.T("KAPAT", "CLOSE"), 44, Close, out cl);

            Refresh();
        }

        DayTile MakeDay(int i, Vector2 aMin, Vector2 aMax)
        {
            var d = new DayTile();
            bool big = i == DailyQuests.DayCount - 1;
            // seçili gün çerçevesi (kutunun arkasında, kenarlardan taşar)
            var ring = UiKit.Panel(card, "Ring" + (i + 1), aMin, aMax, OrangeC);
            ring.offsetMin = new Vector2(-8f, -8f);
            ring.offsetMax = new Vector2(8f, 8f);
            d.ring = NoRay(ring);
            UiIcons.Round(d.ring, 30f);
            d.rt = UiKit.Panel(card, "Day" + (i + 1), aMin, aMax, RowC);
            d.bg = NoRay(d.rt);
            UiIcons.Round(d.bg, 24f);
            d.label = Label(d.rt, Loc.T((i + 1) + ". GÜN", "DAY " + (i + 1)), 28, TextAnchor.MiddleCenter, SubC, new Vector2(0f, 0.72f), new Vector2(1f, 0.97f));
            if (big)
            {
                // büyük sandık
                var ch = UiKit.Fixed(d.rt, "Chest", Vector2.zero, new Vector2(96f, 74f), new Color(0.5f, 0.27f, 0.11f));
                ch.anchorMin = ch.anchorMax = new Vector2(0.24f, 0.42f);
                UiIcons.Round(NoRay(ch), 14f);
                NoRay(UiKit.Fixed(ch, "Band", new Vector2(0f, 6f), new Vector2(100f, 12f), GoldC));
                NoRay(UiKit.Fixed(ch, "Lock", new Vector2(0f, 0f), new Vector2(22f, 28f), GoldC));
                Hud.Coin(d.rt, new Vector2(0.55f, 0.4f), 44f);
                d.amount = Label(d.rt, "", 40, TextAnchor.MiddleLeft, GoldC, new Vector2(0.62f, 0.1f), new Vector2(1f, 0.7f));
            }
            else
            {
                Hud.Coin(d.rt, new Vector2(0.5f, 0.48f), 50f);
                d.amount = Label(d.rt, "", 30, TextAnchor.MiddleCenter, GoldC, new Vector2(0f, 0.04f), new Vector2(1f, 0.3f));
            }
            Shadow(d.amount, 3f);
            var chk = UiKit.Fixed(d.rt, "Check", Vector2.zero, new Vector2(64f, 64f), new Color(0.35f, 0.95f, 0.45f), UiKit.Check);
            chk.anchorMin = chk.anchorMax = new Vector2(big ? 0.9f : 0.8f, 0.5f);
            d.check = NoRay(chk);
            d.check.preserveAspect = true;
            return d;
        }

        QuestRow MakeQuest(int i, Vector2 aMin, Vector2 aMax)
        {
            var q = new QuestRow();
            var rt = UiKit.Panel(card, "Quest" + i, aMin, aMax, RowC);
            q.bg = NoRay(rt);
            UiIcons.Round(q.bg, 26f);
            q.desc = Label(rt, DailyQuests.Describe(i), 32, TextAnchor.MiddleLeft, Color.white, new Vector2(0.04f, 0.5f), new Vector2(0.7f, 0.95f));
            var bar = UiKit.Panel(rt, "Bar", new Vector2(0.04f, 0.17f), new Vector2(0.52f, 0.4f), BarBg, UiIcons.Pill);
            NoRay(bar).type = Image.Type.Sliced;
            UiIcons.MakePill(bar.GetComponent<Image>());
            q.fillRt = UiKit.Panel(bar, "Fill", Vector2.zero, new Vector2(0f, 1f), OrangeC, UiIcons.Pill);
            q.fill = NoRay(q.fillRt);
            UiIcons.MakePill(q.fill);
            q.prog = Label(rt, "", 26, TextAnchor.MiddleLeft, SubC, new Vector2(0.54f, 0.1f), new Vector2(0.72f, 0.47f));
            q.btn = PillButton(rt, "Claim", new Vector2(0.72f, 0.16f), new Vector2(0.98f, 0.84f), GreenC, "", 32, () => ClaimQuest(i), out q.btnText);
            q.btnImg = q.btn.GetComponent<Image>();
            q.btnRt = (RectTransform)q.btn.transform;
            var chk = UiKit.Fixed(rt, "Check", Vector2.zero, new Vector2(64f, 64f), new Color(0.35f, 0.95f, 0.45f), UiKit.Check);
            chk.anchorMin = chk.anchorMax = new Vector2(0.85f, 0.5f);
            q.check = NoRay(chk);
            q.check.preserveAspect = true;
            return q;
        }

        // ------------------------------------------------------------------ durum
        void Refresh()
        {
            bool avail = DailyQuests.DailyAvailable;
            int cur = DailyQuests.DailyIndex;
            for (int i = 0; i < days.Length; i++)
            {
                var d = days[i];
                bool claimed = DailyQuests.DayClaimed(i);
                bool isCur = avail && i == cur;
                d.amount.text = "+" + DailyQuests.DayReward(i);
                d.check.enabled = claimed;
                d.ring.enabled = isCur;
                d.bg.color = claimed ? RowDoneC : isCur ? new Color(0.2f, 0.17f, 0.1f) : RowC;
                d.label.color = isCur ? GoldC : SubC;
                d.amount.color = claimed ? new Color(0.6f, 0.75f, 0.65f) : GoldC;
            }
            dailyBtn.interactable = avail;
            dailyBtnImg.color = avail ? GreenC : GreyC;

            for (int i = 0; i < quests.Count; i++)
            {
                var q = quests[i];
                bool done = DailyQuests.Done(i), got = DailyQuests.Claimed(i);
                int p = DailyQuests.ProgressOf(i), n = DailyQuests.TargetOf(i);
                q.prog.text = p + " / " + n;
                q.fillRt.anchorMax = new Vector2(Mathf.Clamp01(n > 0 ? (float)p / n : 0f), 1f);
                q.fill.enabled = p > 0;
                q.fill.color = done ? GreenC : OrangeC;
                q.bg.color = got ? RowDoneC : RowC;
                q.desc.color = got ? new Color(0.7f, 0.82f, 0.74f) : Color.white;
                q.btn.gameObject.SetActive(!got);
                q.check.enabled = got;
                q.btn.interactable = done;
                q.btnImg.color = done ? GreenC : GreyC;
                q.btnText.text = (done ? Loc.T("AL  +", "GET  +") : "+") + DailyQuests.RewardOf(i);
            }

            bool bonusGot = DailyQuests.BonusClaimed, bonusAvail = DailyQuests.BonusAvailable;
            int claimedCount = 0;
            for (int i = 0; i < quests.Count; i++) if (DailyQuests.Claimed(i)) claimedCount++;
            bonusText.text = bonusGot ? Loc.T("Bonus sandığı alındı!", "Bonus chest collected!")
                : Loc.T("Tüm görevleri bitir (" + claimedCount + "/" + quests.Count + ")", "Finish all quests (" + claimedCount + "/" + quests.Count + ")");
            bonusBtn.gameObject.SetActive(!bonusGot);
            bonusBtn.interactable = bonusAvail;
            bonusBtnImg.color = bonusAvail ? GreenC : GreyC;
            bonusBtnText.text = (bonusAvail ? Loc.T("AÇ  +", "OPEN  +") : "+") + DailyQuests.BonusReward;
            bonusBg.color = bonusGot ? RowDoneC : bonusAvail ? new Color(0.2f, 0.17f, 0.1f) : RowC;
            UpdateClock();
        }

        void UpdateClock()
        {
            string c = Clock(DailyQuests.UntilTomorrow);
            resetText.text = Loc.T("Yenilenme: ", "Resets in: ") + c;
            if (DailyQuests.DailyAvailable)
                dailyBtnText.text = Loc.T("ÖDÜLÜ AL  +", "CLAIM  +") + DailyQuests.DayReward(DailyQuests.DailyIndex);
            else
                dailyBtnText.text = Loc.T("Yeni ödül: ", "Next reward: ") + c;
        }

        // ------------------------------------------------------------------ ödül alma
        void ClaimDaily()
        {
            if (!DailyQuests.DailyAvailable) return;
            int idx = DailyQuests.DailyIndex;
            int before = SaveSystem.Data.gold;
            int g = DailyQuests.ClaimDaily();
            if (g <= 0) return;
            if (idx == DailyQuests.DayCount - 1)
                PromotionChest.Show(g, Loc.T("7 günlük seri tamamlandı!", "7-day streak complete!"), () => Gave(before), Loc.T("BÜYÜK SANDIK", "BIG CHEST"));
            else
            {
                Burst(days[idx].rt, 14);
                Gave(before);
            }
            Refresh();
        }

        void ClaimQuest(int i)
        {
            int before = SaveSystem.Data.gold;
            int g = DailyQuests.ClaimQuest(i);
            if (g <= 0) return;
            Burst(quests[i].btnRt, 12);
            Gave(before);
            Refresh();
        }

        void ClaimBonus()
        {
            int before = SaveSystem.Data.gold;
            int g = DailyQuests.ClaimBonus();
            if (g <= 0) return;
            PromotionChest.Show(g, Loc.T("Günün tüm görevleri tamamlandı!", "All daily quests complete!"), () => Gave(before), Loc.T("BONUS SANDIĞI", "BONUS CHEST"));
            Refresh();
        }

        void Gave(int before)
        {
            Sfx.Play(Snd.Purchase);
            Haptics.Vibrate(30);
            onGold?.Invoke(before, SaveSystem.Data.gold);
        }

        void Close()
        {
            if (PromotionChest.IsOpen) return;
            if (current == this) current = null;
            var cb = onClose;
            Destroy(gameObject);
            cb?.Invoke();
        }

        void OnDestroy()
        {
            if (current == this) current = null;
        }

        // ------------------------------------------------------------------ küçük para patlaması
        void Burst(RectTransform from, int n)
        {
            var corners = new Vector3[4];
            from.GetWorldCorners(corners);
            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                var c = Hud.Coin(root, new Vector2(0.5f, 0.5f), UnityEngine.Random.Range(36f, 54f));
                c.position = center;
                float ang = UnityEngine.Random.Range(-70f, 70f) * Mathf.Deg2Rad;
                float sp = UnityEngine.Random.Range(600f, 1000f);
                coins.Add(new Coin { rt = c, vel = new Vector2(Mathf.Sin(ang) * sp, Mathf.Cos(ang) * sp), spin = UnityEngine.Random.Range(-500f, 500f) });
            }
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            t += dt;
            // açılış: kart hafifçe büyüyerek gelir
            float k = Mathf.Clamp01(t / 0.22f);
            card.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, 1f - (1f - k) * (1f - k));
            // seçili günün çerçevesi nabız gibi atar
            foreach (var d in days) if (d.ring.enabled) d.ring.color = new Color(OrangeC.r, OrangeC.g, OrangeC.b, 0.55f + 0.45f * Mathf.Sin(t * 5f));

            tick -= dt;
            if (tick <= 0f)
            {
                tick = 0.5f;
                if (DailyQuests.Today != builtDay)
                {
                    // gece yarısı geçti: görevler yenilendi, pencere baştan kurulsun
                    var g = onGold; var c = onClose;
                    Destroy(gameObject);
                    Open(g, c);
                    return;
                }
                UpdateClock();
            }

            for (int i = coins.Count - 1; i >= 0; i--)
            {
                var c = coins[i];
                c.life += dt;
                c.vel.y -= 2400f * dt;
                c.rt.anchoredPosition += c.vel * dt;
                c.rt.localRotation = Quaternion.Euler(0f, 0f, c.life * c.spin);
                if (c.life > 1.1f) { Destroy(c.rt.gameObject); coins.RemoveAt(i); }
            }
        }
    }
}
