using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StratejiGame
{
    /// <summary>
    /// Terfi sandığı: bir diyarın boss'u ilk kez yenilip terfi alınınca (Komutan Atilla'nın konuşmasından sonra) açılır.
    /// Sandık yukarıdan düşer ve sallanır; dokununca kapağı fırlar, paralar saçılır, ödül akarak sayılır, TOPLA ile kapanır.
    /// Altın, sandık gösterilmeden önce kayda yazılır (uygulama kapansa bile ödül kaybolmaz). Her diyar için yalnızca bir kez.
    /// Kendi tuvalinde (StoryDialog'un da üstünde) kurulur; zafer ekranında ve ana menüde aynı şekilde kullanılır.
    /// </summary>
    public class PromotionChest : MonoBehaviour
    {
        // ------------------------------------------------------------------ ödül kuralları
        static readonly int[] GoldByWorld = { 500, 1000, 1500, 2000, 2500, 3000 };

        public static int Gold(int worldIndex) => GoldByWorld[Mathf.Clamp(worldIndex, 0, GoldByWorld.Length - 1)];

        public static bool Claimed(int worldIndex) => (SaveSystem.Data.chestsClaimed & (1 << worldIndex)) != 0;

        /// <summary>Diyarın sandığını alır (altını kayda ekler). Daha önce alındıysa 0 döner.</summary>
        public static int Claim(int worldIndex)
        {
            if (worldIndex < 0 || Claimed(worldIndex)) return 0;
            var s = SaveSystem.Data;
            int g = Gold(worldIndex);
            s.chestsClaimed |= 1 << worldIndex;
            s.gold += g;
            s.totalGoldEarned += g;
            SaveSystem.Save();
            return g;
        }

        /// <summary>
        /// Temizlenmiş ama sandığı alınmamış tüm diyarların ödülünü birlikte alır
        /// (sandık sisteminden önce boss yenmiş oyuncular ya da zafer ekranından hemen çıkanlar için). highest: en son diyar.
        /// </summary>
        public static int ClaimMissed(out int highest)
        {
            highest = -1;
            int total = 0;
            for (int w = 0; w < StoryData.WorldCount; w++)
            {
                if (!StoryData.WorldCleared(w) || Claimed(w)) continue;
                total += Claim(w);
                highest = w;
            }
            return total;
        }

        public static string RankLine(int worldIndex) => Loc.T("TERFİ: ", "PROMOTED: ") + StoryData.Rank(worldIndex);

        public static bool IsOpen => current != null;
        static PromotionChest current;

        /// <summary>Sandığı gösterir (altın önceden Claim ile verilmiş olmalı). title boşsa "TERFİ SANDIĞI" (günlük ödüller de kullanır).</summary>
        public static void Show(int gold, string subtitle, Action onDone, string title = null)
        {
            if (current != null) Destroy(current.gameObject);
            var go = new GameObject("PromotionChest");
            current = go.AddComponent<PromotionChest>();
            current.gold = gold;
            current.onDone = onDone;
            current.Build(subtitle, title);
        }

        // ------------------------------------------------------------------ görünüm
        static readonly Color Gold1 = new Color(1f, 0.82f, 0.26f);
        static readonly Color Gold2 = new Color(0.86f, 0.58f, 0.12f);
        static readonly Color Wood = new Color(0.5f, 0.27f, 0.11f);
        static readonly Color WoodLight = new Color(0.6f, 0.34f, 0.14f);
        static readonly Color WoodDark = new Color(0.36f, 0.18f, 0.07f);

        RectTransform root, rays, glow, chest, body, lid, innerGlow, titleRt, subRt, promptRt, amountRt, collectBtn;
        Image dimImg, innerGlowImg, glowImg;
        Text amountText, promptText;
        CanvasGroup group, collectGroup, amountGroup;
        int gold;
        Action onDone;

        enum Phase { Intro, Opening, Closing }
        Phase phase;
        float t, phaseT;
        bool soundDone, collectShown;

        class Coin { public RectTransform rt; public Vector2 vel; public float spin, life; }
        readonly List<Coin> coins = new();

        static Sprite softRays;
        /// <summary>Yumuşak kenarlı 14 ışın (merkezden dışa doğru sönümlenir).</summary>
        static Sprite SoftRays
        {
            get
            {
                if (softRays != null) return softRays;
                const int n = 256;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a = Mathf.Atan2(v, u);
                    float ray = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(a * 7f)), 6f);
                    float fall = Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 6f);
                    byte al = (byte)Mathf.RoundToInt(255f * ray * fall * fall);
                    px[y * n + x] = new Color32(255, 255, 255, al);
                }
                tex.SetPixels32(px);
                tex.Apply();
                softRays = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
                return softRays;
            }
        }

        Text Label(RectTransform parent, string text, int size, Color color)
        {
            var l = UiKit.Label(parent, text, size, TextAnchor.MiddleCenter, color);
            if (UiKit.MenuFont != null) { l.font = UiKit.MenuFont; l.fontStyle = FontStyle.Normal; }
            return l;
        }

        static void AddOutline(Graphic g, float d, float a = 0.75f)
        {
            var o = g.gameObject.AddComponent<UnityEngine.UI.Outline>();
            o.effectColor = new Color(0.08f, 0.04f, 0f, a);
            o.effectDistance = new Vector2(d, -d);
        }

        static void AddShadow(Graphic g, float d, float a = 0.45f)
        {
            var s = g.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, a);
            s.effectDistance = new Vector2(0f, -d);
        }

        RectTransform Box(RectTransform parent, string name, Vector2 pos, Vector2 size, Color c, float radius)
        {
            var r = UiKit.Fixed(parent, name, pos, size, c);
            var img = r.GetComponent<Image>();
            if (radius > 0f) UiIcons.Round(img, radius);
            img.raycastTarget = false;
            return r;
        }

        void Build(string subtitle, string titleText)
        {
            root = UiKit.CreateCanvas(transform, "ChestCanvas", 310);
            group = root.gameObject.AddComponent<CanvasGroup>();

            // karartma: aynı zamanda dokunma alanı (sandığı açar)
            var dim = UiKit.Panel(root, "Dim", Vector2.zero, Vector2.one, new Color(0f, 0.02f, 0.06f, 0f));
            dimImg = dim.GetComponent<Image>();
            var tap = dim.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(Open);

            // arkada dönen ışınlar + parıltı
            glow = UiKit.Fixed(root, "Glow", new Vector2(0f, -40f), new Vector2(1100f, 1100f), new Color(1f, 0.78f, 0.3f, 0.18f), UiIcons.Spark);
            glowImg = glow.GetComponent<Image>();
            glowImg.raycastTarget = false;
            rays = UiKit.Fixed(root, "Rays", new Vector2(0f, -40f), new Vector2(1600f, 1600f), new Color(1f, 0.85f, 0.4f, 0.35f), SoftRays);
            rays.GetComponent<Image>().raycastTarget = false;

            // başlık
            var title = Label(root, string.IsNullOrEmpty(titleText) ? Loc.T("TERFİ SANDIĞI", "PROMOTION CHEST") : titleText, 92, Gold1);
            titleRt = title.rectTransform;
            UiKit.Stretch(titleRt, new Vector2(0f, 0.795f), new Vector2(1f, 0.875f));
            AddOutline(title, 5f);
            AddShadow(title, 8f);
            var sub = Label(root, subtitle, 44, Color.white);
            subRt = sub.rectTransform;
            UiKit.Stretch(subRt, new Vector2(0f, 0.745f), new Vector2(1f, 0.795f));
            AddOutline(sub, 3f);

            // ------------------------------------------------ sandık
            chest = UiKit.Fixed(root, "Chest", new Vector2(0f, -40f), new Vector2(520f, 420f), Color.clear);
            chest.GetComponent<Image>().raycastTarget = false;

            // gölge
            var shadow = Box(chest, "Shadow", new Vector2(0f, -205f), new Vector2(470f, 50f), new Color(0f, 0f, 0f, 0.35f), 25f);
            shadow.GetComponent<Image>().sprite = UiKit.Circle;
            shadow.GetComponent<Image>().type = Image.Type.Simple;

            // içten gelen ışık (kapak açılınca)
            innerGlow = UiKit.Fixed(chest, "InnerGlow", new Vector2(0f, 60f), new Vector2(600f, 460f), new Color(1f, 0.9f, 0.45f, 0f), UiIcons.Spark);
            innerGlowImg = innerGlow.GetComponent<Image>();
            innerGlowImg.raycastTarget = false;

            // gövde
            body = Box(chest, "Body", new Vector2(0f, -70f), new Vector2(460f, 250f), Wood, 34f);
            AddShadow(body.GetComponent<Image>(), 10f, 0.4f);
            Box(body, "Plank1", new Vector2(0f, 35f), new Vector2(430f, 10f), WoodDark, 5f);
            Box(body, "Plank2", new Vector2(0f, -40f), new Vector2(430f, 10f), WoodDark, 5f);
            Box(body, "Rim", new Vector2(0f, 112f), new Vector2(476f, 30f), Gold2, 12f);
            Box(body, "Base", new Vector2(0f, -112f), new Vector2(476f, 30f), Gold2, 12f);
            foreach (float x in new[] { -160f, 160f })
            {
                Box(body, "Band", new Vector2(x, 0f), new Vector2(46f, 250f), Gold1, 10f);
                Box(body, "BandShade", new Vector2(x + 14f, 0f), new Vector2(12f, 246f), Gold2, 5f);
            }

            // kapak (alt kenarından menteşeli gibi döner)
            lid = UiKit.Fixed(chest, "Lid", new Vector2(0f, 55f), new Vector2(490f, 150f), Color.clear);
            lid.pivot = new Vector2(0.5f, 0f);
            lid.anchoredPosition = new Vector2(0f, 55f);
            lid.GetComponent<Image>().raycastTarget = false;
            var lidTop = Box(lid, "Top", new Vector2(0f, 0f), new Vector2(490f, 150f), WoodLight, 60f);
            AddShadow(lidTop.GetComponent<Image>(), 6f, 0.35f);
            Box(lidTop, "Plank", new Vector2(0f, 10f), new Vector2(450f, 10f), WoodDark, 5f);
            Box(lidTop, "Gloss", new Vector2(0f, 48f), new Vector2(380f, 26f), new Color(1f, 1f, 1f, 0.12f), 13f);
            Box(lidTop, "Rim", new Vector2(0f, -60f), new Vector2(496f, 30f), Gold2, 12f);
            foreach (float x in new[] { -160f, 160f })
                Box(lidTop, "Band", new Vector2(x, 0f), new Vector2(46f, 150f), Gold1, 12f);
            // kilit
            var lockRt = UiKit.Fixed(lidTop, "Lock", new Vector2(0f, -62f), new Vector2(104f, 104f), Gold1, UiKit.Circle);
            lockRt.GetComponent<Image>().raycastTarget = false;
            AddShadow(lockRt.GetComponent<Image>(), 5f);
            var lockIn = UiKit.Fixed(lockRt, "In", Vector2.zero, new Vector2(78f, 78f), Gold2, UiKit.Circle);
            lockIn.GetComponent<Image>().raycastTarget = false;
            Box(lockRt, "Key", new Vector2(0f, -6f), new Vector2(16f, 34f), WoodDark, 8f);
            var keyTop = UiKit.Fixed(lockRt, "KeyTop", new Vector2(0f, 12f), new Vector2(28f, 28f), WoodDark, UiKit.Circle);
            keyTop.GetComponent<Image>().raycastTarget = false;

            // ------------------------------------------------ alt yazılar
            promptText = Label(root, Loc.T("AÇMAK İÇİN DOKUN", "TAP TO OPEN"), 46, Color.white);
            promptRt = promptText.rectTransform;
            UiKit.Stretch(promptRt, new Vector2(0f, 0.25f), new Vector2(1f, 0.31f));
            AddOutline(promptText, 3f);

            amountRt = UiKit.Panel(root, "Amount", new Vector2(0.15f, 0.235f), new Vector2(0.85f, 0.325f), Color.clear);
            amountRt.GetComponent<Image>().raycastTarget = false;
            amountGroup = amountRt.gameObject.AddComponent<CanvasGroup>();
            amountGroup.alpha = 0f;
            amountGroup.blocksRaycasts = false;
            Hud.Coin(amountRt, new Vector2(0.18f, 0.5f), 120f);
            amountText = Label(amountRt, "+0", 110, Gold1);
            UiKit.Stretch(amountText.rectTransform, new Vector2(0.3f, 0f), new Vector2(1f, 1f));
            amountText.alignment = TextAnchor.MiddleLeft;
            AddOutline(amountText, 5f);
            AddShadow(amountText, 8f);

            Text cl;
            var btn = UiKit.Button(root, "Collect", new Vector2(0.25f, 0.11f), new Vector2(0.75f, 0.185f), new Color(0.2f, 0.66f, 0.3f), Loc.T("TOPLA", "COLLECT"), 52, Close, out cl);
            UiIcons.MakePill(btn.GetComponent<Image>());
            AddShadow(btn.GetComponent<Image>(), 8f, 0.45f);
            if (UiKit.MenuFont != null) { cl.font = UiKit.MenuFont; cl.fontStyle = FontStyle.Normal; }
            AddOutline(cl, 3f, 0.5f);
            collectBtn = (RectTransform)btn.transform;
            collectGroup = btn.gameObject.AddComponent<CanvasGroup>();
            collectGroup.alpha = 0f;
            collectGroup.interactable = false;
            collectGroup.blocksRaycasts = false;

            phase = Phase.Intro;
            t = phaseT = 0f;
            Apply();
            Sfx.Play(Snd.GatePower);
        }

        // ------------------------------------------------------------------ akış
        void Open()
        {
            if (phase != Phase.Intro || t < 0.55f) return;
            phase = Phase.Opening;
            phaseT = 0f;
            Haptics.Vibrate(40);
        }

        void Close()
        {
            if (phase != Phase.Opening || !collectShown) return;
            phase = Phase.Closing;
            phaseT = 0f;
            collectGroup.interactable = false;
            Sfx.Play(Snd.Purchase);
        }

        void Finish()
        {
            if (current == this) current = null;
            var cb = onDone;
            Destroy(gameObject);
            cb?.Invoke();
        }

        static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            float c1 = 1.70158f, c3 = c1 + 1f, k = x - 1f;
            return 1f + c3 * k * k * k + c1 * k * k;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            t += dt;
            phaseT += dt;
            // dokunulmazsa birkaç saniye sonra kendiliğinden açılır
            if (phase == Phase.Intro && t > 5f) Open();
            Apply();
            UpdateCoins(dt);
            if (phase == Phase.Closing && phaseT >= 0.3f) Finish();
        }

        void Apply()
        {
            float fade = phase == Phase.Closing ? 1f - Mathf.Clamp01(phaseT / 0.3f) : 1f;
            group.alpha = fade;
            // tam opak: doğrusal renk uzayında yarı saydam karartma alttaki zafer kartını belirgin gösteriyordu
            dimImg.color = new Color(0.02f, 0.03f, 0.07f, Mathf.Clamp01(t / 0.25f));

            float open = phase == Phase.Intro ? 0f : Mathf.Clamp01(phaseT / 0.35f);
            rays.localRotation = Quaternion.Euler(0f, 0f, t * 14f);
            rays.localScale = Vector3.one * (0.55f + 0.25f * Mathf.Clamp01(t / 0.6f) + 0.35f * open);
            glow.localScale = Vector3.one * (0.85f + 0.06f * Mathf.Sin(t * 1.8f) + 0.25f * open);
            glowImg.color = new Color(1f, 0.78f, 0.3f, 0.18f + 0.12f * open);

            // başlık yukarıdan iner
            float tk = Mathf.Clamp01((t - 0.1f) / 0.35f);
            titleRt.localScale = Vector3.one * Mathf.LerpUnclamped(1.4f, 1f, EaseOutBack(tk));
            titleRt.GetComponent<Text>().color = new Color(Gold1.r, Gold1.g, Gold1.b, tk);
            float sk = Mathf.Clamp01((t - 0.3f) / 0.3f);
            subRt.GetComponent<Text>().color = new Color(1f, 1f, 1f, sk);

            if (phase == Phase.Intro)
            {
                // düşüş + sallanma (her 1.3 sn'de bir titrer: "aç beni")
                float dk = Mathf.Clamp01(t / 0.5f);
                chest.anchoredPosition = new Vector2(0f, -40f + (1f - EaseOutBack(dk)) * 700f);
                float w = (t - 0.6f) % 1.3f;
                float wob = t > 0.6f && w < 0.4f ? Mathf.Sin(w / 0.4f * Mathf.PI * 4f) * 6f * (1f - w / 0.4f) : 0f;
                chest.localRotation = Quaternion.Euler(0f, 0f, wob);
                float land = t > 0.5f && t < 0.7f ? Mathf.Sin((t - 0.5f) / 0.2f * Mathf.PI) : 0f;
                chest.localScale = new Vector3(1f + 0.08f * land, 1f - 0.08f * land, 1f);
                lid.localRotation = Quaternion.identity;
                lid.anchoredPosition = new Vector2(0f, 55f + (wob != 0f ? Mathf.Abs(wob) * 1.2f : 0f));
                promptText.color = new Color(1f, 1f, 1f, t > 0.6f ? 0.65f + 0.35f * Mathf.Sin(t * 5f) : 0f);
                promptRt.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(t * 5f));
                innerGlowImg.color = new Color(1f, 0.9f, 0.45f, 0f);
                return;
            }

            // açılış
            chest.localRotation = Quaternion.identity;
            chest.anchoredPosition = new Vector2(0f, -40f);
            float sq = Mathf.Clamp01(phaseT / 0.12f);
            float rel = Mathf.Clamp01((phaseT - 0.12f) / 0.25f);
            float squash = phaseT < 0.12f ? sq : 1f - EaseOutBack(rel);
            chest.localScale = new Vector3(1f + 0.12f * squash, 1f - 0.12f * squash, 1f);

            if (phaseT >= 0.12f && !soundDone)
            {
                soundDone = true;
                Sfx.Play(Snd.Win);
                Haptics.Vibrate(80);
                SpawnCoins(26);
            }
            float lk = Mathf.Clamp01((phaseT - 0.12f) / 0.35f);
            float le = EaseOutBack(lk);
            lid.anchoredPosition = new Vector2(-70f * le, 55f + 95f * le);
            lid.localRotation = Quaternion.Euler(0f, 0f, 30f * le);
            innerGlowImg.color = new Color(1f, 0.9f, 0.45f, 0.6f * Mathf.Clamp01((phaseT - 0.1f) / 0.2f) * (0.85f + 0.15f * Mathf.Sin(t * 6f)));
            innerGlow.localScale = Vector3.one * (0.7f + 0.3f * lk);

            promptText.color = new Color(1f, 1f, 1f, 0f);
            // ödül akarak sayılır
            float ak = Mathf.Clamp01((phaseT - 0.3f) / 1.0f);
            float ae = 1f - (1f - ak) * (1f - ak);
            amountGroup.alpha = Mathf.Clamp01((phaseT - 0.25f) / 0.2f);
            amountText.text = "+" + Mathf.RoundToInt(gold * ae);
            amountRt.localScale = Vector3.one * (ak > 0f && ak < 1f ? 1.06f : Mathf.LerpUnclamped(1.25f, 1f, EaseOutBack(Mathf.Clamp01((phaseT - 1.3f) / 0.3f))));

            if (phaseT >= 1.35f && !collectShown)
            {
                collectShown = true;
                collectGroup.interactable = true;
                collectGroup.blocksRaycasts = true;
                Sfx.Play(Snd.Purchase);
            }
            if (collectShown)
            {
                float bk = Mathf.Clamp01((phaseT - 1.35f) / 0.3f);
                collectGroup.alpha = bk;
                collectBtn.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, EaseOutBack(bk)) * (1f + 0.03f * Mathf.Sin(t * 4f));
            }
        }

        // ------------------------------------------------------------------ saçılan paralar
        void SpawnCoins(int n)
        {
            for (int i = 0; i < n; i++)
            {
                var c = Hud.Coin(root, new Vector2(0.5f, 0.5f), UnityEngine.Random.Range(56f, 84f));
                c.SetSiblingIndex(chest.GetSiblingIndex() + 1);
                c.anchoredPosition = new Vector2(UnityEngine.Random.Range(-120f, 120f), 120f);
                float ang = UnityEngine.Random.Range(-60f, 60f) * Mathf.Deg2Rad;
                float sp = UnityEngine.Random.Range(900f, 1500f);
                coins.Add(new Coin
                {
                    rt = c,
                    vel = new Vector2(Mathf.Sin(ang) * sp, Mathf.Cos(ang) * sp),
                    spin = UnityEngine.Random.Range(-540f, 540f),
                });
            }
        }

        void UpdateCoins(float dt)
        {
            for (int i = coins.Count - 1; i >= 0; i--)
            {
                var c = coins[i];
                c.life += dt;
                c.vel.y -= 2600f * dt;
                c.rt.anchoredPosition += c.vel * dt;
                c.rt.localRotation = Quaternion.Euler(0f, 0f, c.life * c.spin);
                // para "dönerken" yatay ölçek değişir
                c.rt.localScale = new Vector3(Mathf.Abs(Mathf.Cos(c.life * 7f + i)) * 0.8f + 0.2f, 1f, 1f);
                if (c.life > 1.6f)
                {
                    Destroy(c.rt.gameObject);
                    coins.RemoveAt(i);
                }
            }
        }

        void OnDestroy()
        {
            if (current == this) current = null;
        }
    }
}
