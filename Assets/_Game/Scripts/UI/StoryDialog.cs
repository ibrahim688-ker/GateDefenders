using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StratejiGame
{
    /// <summary>
    /// Komutan Atilla'nın konuşma ekranı ve eğitim (tutorial) sayfaları.
    /// Kendi tuvalinde (her şeyin üstünde) kurulur; menüde ve zafer ekranında aynı şekilde kullanılır.
    /// Konuşma: altta portreli kart, yazı daktilo gibi akar; dokununca önce yazı tamamlanır, sonra sıradaki cümleye geçilir.
    /// Eğitim: resimli sayfalar (GERİ / İLERİ / BAŞLA). İkisinde de sağ üstte ATLA.
    /// </summary>
    public class StoryDialog : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, 0.84f, 0.3f);
        static readonly Color Navy = new Color(0.05f, 0.09f, 0.17f, 1f);
        static readonly Color Rim = new Color(0.95f, 0.66f, 0.18f);
        static readonly Color Orange = new Color(0.92f, 0.45f, 0.1f);

        static Sprite portrait;
        static Sprite Portrait
        {
            get
            {
                if (portrait == null)
                {
                    var tex = Resources.Load<Texture2D>("Story/Commander");
                    if (tex != null) portrait = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
                return portrait;
            }
        }

        public static bool IsOpen => current != null;
        static StoryDialog current;

        // ------------------------------------------------------------------ dış kullanım
        /// <summary>Komutan konuşması. banner: kartın üstündeki kurdele (ör. "TERFİ: ONBAŞI"), boş olabilir.</summary>
        public static void Talk(string[] lines, string banner, Action onDone)
        {
            var d = Create();
            d.lines = lines;
            d.banner = banner;
            d.onDone = onDone;
            d.BuildTalk();
        }

        public static void Tutorial(Action onDone)
        {
            var d = Create();
            d.onDone = onDone;
            d.BuildTutorial();
        }

        /// <summary>Oyunun ilk açılışı: hikaye girişi, ardından eğitim sayfaları.</summary>
        public static void IntroSequence(Action onDone)
        {
            Talk(StoryData.Intro, "", () => Tutorial(onDone));
        }

        static StoryDialog Create()
        {
            if (current != null) Destroy(current.gameObject);
            var go = new GameObject("StoryDialog");
            current = go.AddComponent<StoryDialog>();
            current.root = UiKit.CreateCanvas(go.transform, "StoryCanvas", 300);
            return current;
        }

        // ------------------------------------------------------------------ ortak
        RectTransform root, card, portraitRt, hint;
        CanvasGroup group;
        Action onDone;
        float openT;
        bool closing;
        float closeT;

        string[] lines;
        string banner;
        int lineIndex;
        float typeT;
        Text bodyText, counterText;

        StoryData.Page[] pages;
        int pageIndex;
        Text pageTitle, pageText, pageCounter, nextLabel;
        RectTransform art, backBtn;
        readonly List<RectTransform> dots = new();
        readonly List<RectTransform> bob = new();
        RectTransform moveHero;
        float pageT;

        const float CharsPerSecond = 55f;

        Text Label(RectTransform parent, string text, int size, TextAnchor anchor, Color color)
        {
            var t = UiKit.Label(parent, text, size, anchor, color);
            if (UiKit.MenuFont != null) { t.font = UiKit.MenuFont; t.fontStyle = FontStyle.Normal; }
            return t;
        }

        static void AddShadow(Graphic g, float d, float a = 0.5f)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, a);
            s.effectDistance = new Vector2(0f, -d);
        }

        static void AddOutline(Graphic g, float d, float a = 0.6f)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, a);
            o.effectDistance = new Vector2(d, -d);
        }

        RectTransform Dim()
        {
            var dim = UiKit.Panel(root, "Dim", Vector2.zero, Vector2.one, new Color(0f, 0.02f, 0.06f, 0.86f));
            group = root.gameObject.AddComponent<CanvasGroup>();
            return dim;
        }

        void SkipButton()
        {
            Text l;
            var b = UiKit.Button(root, "Skip", new Vector2(0.72f, 0.925f), new Vector2(0.96f, 0.975f), new Color(0.1f, 0.16f, 0.28f, 0.92f), Loc.T("ATLA  »", "SKIP  »"), 34, Finish, out l);
            UiIcons.MakePill(b.GetComponent<Image>());
            if (UiKit.MenuFont != null) { l.font = UiKit.MenuFont; l.fontStyle = FontStyle.Normal; }
            l.color = new Color(0.85f, 0.9f, 1f);
        }

        /// <summary>Altın çerçeveli yuvarlak portre.</summary>
        RectTransform Medallion(RectTransform parent, float size)
        {
            var ring = UiKit.Fixed(parent, "Portrait", Vector2.zero, new Vector2(size, size), Rim, UiKit.Circle);
            ring.GetComponent<Image>().raycastTarget = false;
            AddShadow(ring.GetComponent<Image>(), size * 0.03f);
            var inner = UiKit.Fixed(ring, "Inner", Vector2.zero, new Vector2(size * 0.92f, size * 0.92f), new Color(0.12f, 0.22f, 0.42f), UiKit.Circle);
            inner.GetComponent<Image>().raycastTarget = false;
            // portre görseli zaten daire şeklinde kırpılı (Mask kullanılmaz)
            var glow = UiKit.Fixed(inner, "Glow", new Vector2(0f, size * 0.06f), new Vector2(size * 0.8f, size * 0.8f), new Color(0.45f, 0.7f, 1f, 0.45f), UiIcons.Spark);
            glow.GetComponent<Image>().raycastTarget = false;
            var face = UiKit.Fixed(inner, "Face", Vector2.zero, new Vector2(size * 0.92f, size * 0.92f), Color.white, Portrait);
            face.GetComponent<Image>().raycastTarget = false;
            if (Portrait == null) face.GetComponent<Image>().color = Color.clear;
            return ring;
        }

        // ------------------------------------------------------------------ konuşma
        void BuildTalk()
        {
            var dim = Dim();
            var tap = dim.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(OnTap);

            card = UiKit.Panel(root, "Card", new Vector2(0.04f, 0.055f), new Vector2(0.96f, 0.35f), Rim);
            UiIcons.Round(card, 40f).raycastTarget = false;
            AddShadow(card.GetComponent<Image>(), 12f);
            var inner = UiKit.Panel(card, "Inner", Vector2.zero, Vector2.one, Navy);
            inner.offsetMin = new Vector2(7f, 7f);
            inner.offsetMax = new Vector2(-7f, -7f);
            UiIcons.Round(inner, 34f).raycastTarget = false;

            // portre kartın sol üstünden taşar
            portraitRt = Medallion(root, 420f);
            portraitRt.anchorMin = portraitRt.anchorMax = new Vector2(0.27f, 0.42f);

            // ad etiketi
            var nameTag = UiKit.Panel(card, "Name", new Vector2(0.44f, 0.9f), new Vector2(0.97f, 1.08f), Orange);
            UiIcons.MakePill(nameTag).raycastTarget = false;
            AddShadow(nameTag.GetComponent<Image>(), 5f);
            var nt = Label(nameTag, StoryData.CommanderName, 36, TextAnchor.MiddleCenter, Color.white);
            AddOutline(nt, 2f, 0.4f);

            if (!string.IsNullOrEmpty(banner))
            {
                var rib = UiKit.Panel(root, "Banner", new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.725f), Gold);
                UiIcons.MakePill(rib).raycastTarget = false;
                AddShadow(rib.GetComponent<Image>(), 8f);
                var bt = Label(rib, banner, 40, TextAnchor.MiddleCenter, new Color(0.35f, 0.16f, 0.02f));
                bob.Add(rib);
            }

            bodyText = Label(inner, "", 40, TextAnchor.UpperLeft, new Color(0.93f, 0.96f, 1f));
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.lineSpacing = 1.05f;
            UiKit.Stretch(bodyText.rectTransform, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.8f));

            counterText = Label(inner, "", 28, TextAnchor.MiddleLeft, new Color(0.6f, 0.68f, 0.8f));
            UiKit.Stretch(counterText.rectTransform, new Vector2(0.05f, 0.03f), new Vector2(0.4f, 0.18f));
            hint = Label(inner, "", 32, TextAnchor.MiddleRight, Gold).rectTransform;
            UiKit.Stretch(hint, new Vector2(0.45f, 0.03f), new Vector2(0.95f, 0.18f));

            SkipButton();
            lineIndex = 0;
            ShowLine();
        }

        void ShowLine()
        {
            typeT = 0f;
            bodyText.text = "";
            counterText.text = (lineIndex + 1) + " / " + lines.Length;
            hint.GetComponent<Text>().text = "";
        }

        bool Typing => lines != null && lineIndex < lines.Length && typeT * CharsPerSecond < lines[lineIndex].Length;

        void OnTap()
        {
            if (closing || lines == null) return;
            if (Typing) { typeT = 999f; return; }
            Sfx.Click();
            lineIndex++;
            if (lineIndex >= lines.Length) { Finish(); return; }
            ShowLine();
        }

        // ------------------------------------------------------------------ eğitim
        void BuildTutorial()
        {
            Dim();
            pages = StoryData.Tutorial;

            card = UiKit.Panel(root, "Card", new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.87f), Rim);
            UiIcons.Round(card, 44f).raycastTarget = false;
            AddShadow(card.GetComponent<Image>(), 12f);
            var inner = UiKit.Panel(card, "Inner", Vector2.zero, Vector2.one, Navy);
            inner.offsetMin = new Vector2(7f, 7f);
            inner.offsetMax = new Vector2(-7f, -7f);
            UiIcons.Round(inner, 38f).raycastTarget = false;

            // üstte küçük portre + ad + sayfa sayacı
            portraitRt = Medallion(inner, 170f);
            portraitRt.anchorMin = portraitRt.anchorMax = new Vector2(0.15f, 0.9f);
            var name = Label(inner, StoryData.CommanderName, 34, TextAnchor.MiddleLeft, Gold);
            UiKit.Stretch(name.rectTransform, new Vector2(0.27f, 0.9f), new Vector2(0.97f, 0.96f));
            pageCounter = Label(inner, "", 28, TextAnchor.MiddleLeft, new Color(0.65f, 0.72f, 0.85f));
            UiKit.Stretch(pageCounter.rectTransform, new Vector2(0.27f, 0.85f), new Vector2(0.97f, 0.9f));

            art = UiKit.Panel(inner, "Art", new Vector2(0.06f, 0.45f), new Vector2(0.94f, 0.81f), new Color(0f, 0f, 0f, 0.25f));
            UiIcons.Round(art, 30f).raycastTarget = false;

            pageTitle = Label(inner, "", 60, TextAnchor.MiddleCenter, Gold);
            UiKit.Stretch(pageTitle.rectTransform, new Vector2(0.03f, 0.36f), new Vector2(0.97f, 0.44f));
            AddOutline(pageTitle, 3f, 0.5f);
            pageText = Label(inner, "", 36, TextAnchor.UpperCenter, new Color(0.9f, 0.94f, 1f));
            pageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiKit.Stretch(pageText.rectTransform, new Vector2(0.07f, 0.15f), new Vector2(0.93f, 0.355f));

            // sayfa noktaları
            for (int i = 0; i < pages.Length; i++)
            {
                var d = UiKit.Fixed(inner, "Dot" + i, Vector2.zero, new Vector2(20f, 20f), Color.white, UiKit.Circle);
                d.anchorMin = d.anchorMax = new Vector2(0.5f + (i - (pages.Length - 1) * 0.5f) * 0.045f, 0.135f);
                d.GetComponent<Image>().raycastTarget = false;
                dots.Add(d);
            }

            Text bl;
            var back = UiKit.Button(inner, "Back", new Vector2(0.05f, 0.025f), new Vector2(0.36f, 0.11f), new Color(0.22f, 0.32f, 0.48f), Loc.T("GERİ", "BACK"), 40, () => GoPage(pageIndex - 1), out bl);
            UiIcons.MakePill(back.GetComponent<Image>());
            if (UiKit.MenuFont != null) { bl.font = UiKit.MenuFont; bl.fontStyle = FontStyle.Normal; }
            backBtn = (RectTransform)back.transform;
            var next = UiKit.Button(inner, "Next", new Vector2(0.4f, 0.025f), new Vector2(0.95f, 0.11f), Orange, "", 44, () => GoPage(pageIndex + 1), out nextLabel);
            UiIcons.MakePill(next.GetComponent<Image>());
            if (UiKit.MenuFont != null) { nextLabel.font = UiKit.MenuFont; nextLabel.fontStyle = FontStyle.Normal; }
            AddOutline(nextLabel, 2f, 0.4f);

            SkipButton();
            GoPage(0);
        }

        void GoPage(int i)
        {
            if (closing) return;
            if (i >= pages.Length) { Finish(); return; }
            if (i < 0) return;
            pageIndex = i;
            pageT = 0f;
            pageTitle.text = pages[i].title;
            pageText.text = pages[i].text;
            pageCounter.text = Loc.T("EĞİTİM ", "TRAINING ") + (i + 1) + " / " + pages.Length;
            nextLabel.text = i == pages.Length - 1 ? Loc.T("SAVAŞA BAŞLA!", "START FIGHTING!") : Loc.T("İLERİ  »", "NEXT  »");
            backBtn.gameObject.SetActive(i > 0);
            for (int k = 0; k < dots.Count; k++)
            {
                dots[k].GetComponent<Image>().color = k == i ? Gold : new Color(1f, 1f, 1f, 0.25f);
                dots[k].sizeDelta = Vector2.one * (k == i ? 26f : 18f);
            }
            BuildArt(i);
        }

        // --- sayfa resimleri (koddan ikonlarla) ---
        RectTransform Badge(RectTransform parent, Vector2 anchor, float size, Color color, Sprite icon, string label = null, Color? iconColor = null)
        {
            var ring = UiKit.Fixed(parent, "Badge", Vector2.zero, new Vector2(size, size), color, UiKit.Circle);
            ring.anchorMin = ring.anchorMax = anchor;
            ring.GetComponent<Image>().raycastTarget = false;
            AddShadow(ring.GetComponent<Image>(), 6f, 0.4f);
            var inner = UiKit.Fixed(ring, "In", Vector2.zero, new Vector2(size * 0.84f, size * 0.84f), new Color(0.03f, 0.05f, 0.1f), UiKit.Circle);
            inner.GetComponent<Image>().raycastTarget = false;
            var ic = UiKit.Fixed(inner, "Icon", Vector2.zero, new Vector2(size * 0.55f, size * 0.55f), iconColor ?? Color.white, icon);
            ic.GetComponent<Image>().raycastTarget = false;
            if (label != null)
            {
                var chip = UiKit.Panel(ring, "Chip", new Vector2(0.08f, -0.2f), new Vector2(0.92f, 0.08f), color);
                UiIcons.MakePill(chip).raycastTarget = false;
                var t = Label(chip, label, Mathf.RoundToInt(size * 0.2f), TextAnchor.MiddleCenter, Color.white);
                AddOutline(t, 2f, 0.5f);
            }
            bob.Add(ring);
            return ring;
        }

        RectTransform GateBox(RectTransform parent, Vector2 aMin, Vector2 aMax, Color color, Sprite icon, string value)
        {
            var frame = UiKit.Panel(parent, "Gate", aMin, aMax, new Color(0.62f, 0.68f, 0.72f));
            UiIcons.Round(frame, 20f).raycastTarget = false;
            AddShadow(frame.GetComponent<Image>(), 6f, 0.4f);
            var panel = UiKit.Panel(frame, "Panel", new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.86f), color);
            UiIcons.Round(panel, 14f).raycastTarget = false;
            if (icon != null)
            {
                var b = UiKit.Fixed(panel, "Badge", Vector2.zero, new Vector2(84f, 84f), new Color(0.03f, 0.04f, 0.08f, 0.9f), UiKit.Circle);
                b.anchorMin = b.anchorMax = new Vector2(0.27f, 0.5f);
                b.GetComponent<Image>().raycastTarget = false;
                UiKit.Fixed(b, "Icon", Vector2.zero, new Vector2(56f, 56f), Color.white, icon).GetComponent<Image>().raycastTarget = false;
            }
            var t = Label(panel, value, 52, TextAnchor.MiddleCenter, Color.white);
            UiKit.Stretch(t.rectTransform, new Vector2(icon != null ? 0.45f : 0f, 0f), Vector2.one);
            AddOutline(t, 3f, 0.7f);
            bob.Add(frame);
            return frame;
        }

        void BuildArt(int page)
        {
            for (int i = art.childCount - 1; i >= 0; i--) { var ch = art.GetChild(i).gameObject; ch.SetActive(false); Destroy(ch); }
            bob.Clear();
            moveHero = null;
            var blue = new Color(0.15f, 0.45f, 0.95f);
            var red = new Color(0.9f, 0.2f, 0.2f);
            switch (page)
            {
                case 0:
                    {
                        // 3 şerit, ortada kahraman, iki yanda kaydırma okları
                        for (int l = 0; l < 3; l++)
                        {
                            var lane = UiKit.Panel(art, "Lane", new Vector2(0.24f + l * 0.18f, 0.08f), new Vector2(0.4f + l * 0.18f, 0.92f), new Color(1f, 1f, 1f, 0.07f));
                            UiIcons.Round(lane, 18f).raycastTarget = false;
                        }
                        moveHero = UiKit.Fixed(art, "Hero", Vector2.zero, new Vector2(150f, 150f), blue, UiKit.Circle);
                        moveHero.anchorMin = moveHero.anchorMax = new Vector2(0.5f, 0.42f);
                        moveHero.GetComponent<Image>().raycastTarget = false;
                        UiKit.Fixed(moveHero, "Icon", Vector2.zero, new Vector2(96f, 96f), Color.white, UiIcons.PersonIcon).GetComponent<Image>().raycastTarget = false;
                        for (int s = -1; s <= 1; s += 2)
                        {
                            var arrow = UiKit.Fixed(art, "Arrow", Vector2.zero, new Vector2(110f, 110f), Gold, UiIcons.Chevrons);
                            arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f + s * 0.38f, 0.42f);
                            arrow.localEulerAngles = new Vector3(0f, 0f, -90f * s);
                            arrow.GetComponent<Image>().raycastTarget = false;
                            bob.Add(arrow);
                        }
                        break;
                    }
                case 1:
                    GateBox(art, new Vector2(0.06f, 0.22f), new Vector2(0.47f, 0.78f), blue, UiIcons.Troops, "+5");
                    GateBox(art, new Vector2(0.53f, 0.22f), new Vector2(0.94f, 0.78f), blue, UiIcons.Troops, "x2");
                    break;
                case 2:
                    {
                        var icons = new[] { UiIcons.Bolt, UiIcons.Sword, UiIcons.Bullet, UiIcons.PierceIcon, UiIcons.Crosshair };
                        var labels = new[] { "+20%", "+25%", "+1", "+1", "+10%" };
                        for (int k = 0; k < icons.Length; k++)
                            Badge(art, new Vector2(0.12f + k * 0.19f, 0.56f), 150f, BuffInfo.Gold, icons[k], labels[k]);
                        break;
                    }
                case 3:
                    {
                        GateBox(art, new Vector2(0.06f, 0.22f), new Vector2(0.47f, 0.78f), red, UiIcons.Skull, "-10");
                        var g2 = GateBox(art, new Vector2(0.53f, 0.22f), new Vector2(0.94f, 0.78f), blue, UiIcons.Crosshair, "+1 » +3");
                        g2.GetComponentInChildren<Text>().fontSize = 40;
                        break;
                    }
                case 4:
                    Badge(art, new Vector2(0.2f, 0.58f), 190f, new Color(0.95f, 0.5f, 0.12f), UiIcons.Blast, Loc.T("ALAN", "BLAST"));
                    Badge(art, new Vector2(0.5f, 0.58f), 190f, new Color(0.25f, 0.78f, 0.35f), UiIcons.Potion, Loc.T("CAN", "HEAL"));
                    Badge(art, new Vector2(0.8f, 0.58f), 190f, new Color(0.9f, 0.2f, 0.18f), UiIcons.Flame, Loc.T("ÖFKE", "RAGE"));
                    break;
                default:
                    Badge(art, new Vector2(0.2f, 0.58f), 180f, new Color(0.95f, 0.72f, 0.15f), UiIcons.Coin, Loc.T("ALTIN", "GOLD"), new Color(1f, 0.86f, 0.32f));
                    Badge(art, new Vector2(0.5f, 0.58f), 180f, new Color(0.3f, 0.75f, 0.45f), UiIcons.UpArrow, Loc.T("YÜKSELT", "UPGRADE"));
                    Badge(art, new Vector2(0.8f, 0.58f), 180f, new Color(0.8f, 0.15f, 0.2f), UiIcons.Skull, "BOSS");
                    break;
            }
        }

        // ------------------------------------------------------------------ kapanış ve animasyon
        void Finish()
        {
            if (closing) return;
            closing = true;
            closeT = 0f;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float now = Time.unscaledTime;

            if (closing)
            {
                closeT += dt;
                if (group != null) group.alpha = 1f - Mathf.Clamp01(closeT / 0.2f);
                if (closeT >= 0.2f)
                {
                    var cb = onDone;
                    if (current == this) current = null;
                    Destroy(gameObject);
                    cb?.Invoke();
                }
                return;
            }

            // açılış: kart aşağıdan kayar, portre büyüyerek gelir
            openT += dt;
            float k = Mathf.Clamp01(openT / 0.35f);
            float e = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);
            if (group != null) group.alpha = Mathf.Clamp01(openT / 0.2f);
            if (card != null) card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, e);
            if (portraitRt != null) portraitRt.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, e) * (1f + 0.012f * Mathf.Sin(now * 2.2f));

            if (lines != null && lineIndex < lines.Length)
            {
                typeT += dt;
                string full = lines[lineIndex];
                int n = Mathf.Min(full.Length, Mathf.FloorToInt(typeT * CharsPerSecond));
                if (bodyText.text.Length != n) bodyText.text = full.Substring(0, n);
                var ht = hint.GetComponent<Text>();
                if (!Typing)
                {
                    ht.text = lineIndex == lines.Length - 1 ? Loc.T("TAMAM", "OK") : Loc.T("DEVAM  »", "NEXT  »");
                    var c = ht.color; c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(now * 3f)); ht.color = c;
                }
            }

            if (pages != null)
            {
                pageT += dt;
                if (moveHero != null) moveHero.anchoredPosition = new Vector2(Mathf.Sin(now * 1.8f) * 190f, 0f);
            }
            for (int i = 0; i < bob.Count; i++)
                if (bob[i] != null) bob[i].localScale = Vector3.one * (1f + 0.035f * Mathf.Sin(now * 3f + i * 0.9f));
        }

        void OnDestroy()
        {
            if (current == this) current = null;
        }
    }
}
