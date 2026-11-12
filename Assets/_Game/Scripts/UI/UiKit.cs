using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace StratejiGame
{
    /// <summary>Çalışma anında uGUI kurmak için ortak yardımcılar ve koddan üretilen basit ikonlar (daire, tik, kilit).</summary>
    public static class UiKit
    {
        public static RectTransform CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            return (RectTransform)go.transform;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || Object.FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static RectTransform Panel(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt, aMin, aMax);
            var img = go.GetComponent<Image>();
            img.color = color;
            if (sprite != null) img.sprite = sprite;
            return rt;
        }

        /// <summary>Merkeze göre sabit boyutlu (piksel) öğe.</summary>
        public static RectTransform Fixed(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = Panel(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), color, sprite);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        /// <summary>
        /// Ana menü yazı tipi (Baloo 2 ExtraBold, Resources/Fonts). Menü açıkken FontOverride buna eşitlenir;
        /// o sırada oluşturulan tüm yazılar bu fontu (sahte kalınlık olmadan) kullanır. Oyun içi HUD etkilenmez.
        /// </summary>
        public static Font FontOverride;
        static Font menuFont;
        public static Font MenuFont => menuFont != null ? menuFont : (menuFont = Resources.Load<Font>("Fonts/Baloo2-ExtraBold"));

        public static Text Label(RectTransform parent, string text, int size, TextAnchor anchor, Color? color = null)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt, Vector2.zero, Vector2.one);
            var t = go.GetComponent<Text>();
            t.font = FontOverride != null ? FontOverride : Mats.Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontOverride != null ? FontStyle.Normal : FontStyle.Bold;
            t.alignment = anchor;
            t.color = color ?? Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button Button(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Color color, string label, int fontSize,
            UnityEngine.Events.UnityAction onClick, out Text labelText)
        {
            var rt = Panel(parent, name, aMin, aMax, color);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = rt.GetComponent<Image>();
            b.onClick.AddListener(Sfx.Click);
            if (onClick != null) b.onClick.AddListener(onClick);
            labelText = Label(rt, label, fontSize, TextAnchor.MiddleCenter);
            return b;
        }

        public static void Stretch(RectTransform rt, Vector2 aMin, Vector2 aMax)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // --- koddan üretilen ikonlar ---
        static Sprite circle, check, lockIcon;

        public static Sprite Circle => circle != null ? circle : (circle = MakeSprite(128, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f));
            return Mathf.Clamp01(62f - d);
        }));

        public static Sprite Check => check != null ? check : (check = MakeSprite(128, (x, y) =>
        {
            var p = new Vector2(x, y);
            float d = Mathf.Min(SegDist(p, new Vector2(24, 64), new Vector2(52, 34)), SegDist(p, new Vector2(52, 34), new Vector2(106, 96)));
            return Mathf.Clamp01(11f - d);
        }));

        public static Sprite Lock => lockIcon != null ? lockIcon : (lockIcon = MakeSprite(128, (x, y) =>
        {
            var p = new Vector2(x, y);
            // gövde
            bool body = x >= 26 && x <= 102 && y >= 10 && y <= 70;
            // anahtar deliği
            bool hole = Vector2.Distance(p, new Vector2(64, 44)) < 9f || (x >= 60 && x <= 68 && y >= 22 && y <= 44);
            // kilit halkası (üst yarım halka)
            float r = Vector2.Distance(p, new Vector2(64, 70));
            bool shackle = y >= 66 && r >= 22f && r <= 34f;
            return (body && !hole) || shackle ? 1f : 0f;
        }));

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        static Sprite MakeSprite(int size, System.Func<int, int, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(x, y)) * 255));
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
