using System;
using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>Prototip için çalışma anında renkli materyal ve basit şekiller üretir.</summary>
    public static class Mats
    {
        static readonly Dictionary<Color, Material> opaque = new();
        static readonly Dictionary<Color, Material> transparent = new();
        static Shader litShader;
        static Font font;

        static Shader Lit
        {
            get
            {
                if (litShader == null)
                {
                    litShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (litShader == null) litShader = Shader.Find("Standard");
                }
                return litShader;
            }
        }

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Material Get(Color c)
        {
            if (opaque.TryGetValue(c, out var m) && m != null) return m;
            m = new Material(Lit) { enableInstancing = true };
            m.SetColor("_BaseColor", c);
            m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
            opaque[c] = m;
            return m;
        }

        public static Material GetTransparent(Color c)
        {
            if (transparent.TryGetValue(c, out var m) && m != null) return m;
            m = new Material(Lit);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetColor("_BaseColor", c);
            m.color = c;
            transparent[c] = m;
            return m;
        }

        public static GameObject Prim(PrimitiveType type, Color color, Transform parent, Vector3 localPos, Vector3 scale, bool transparentMat = false)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = transparentMat ? GetTransparent(color) : Get(color);
            r.shadowCastingMode = transparentMat ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        public static TextMesh Label(Transform parent, Vector3 localPos, string text, int fontSize, Color color, float charSize = 0.1f)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            tm.text = text;
            tm.fontSize = fontSize;
            tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;
            go.GetComponent<MeshRenderer>().sharedMaterial = Font.material;
            return tm;
        }
    }

    /// <summary>Basit obje havuzu: sürekli Instantiate/Destroy yerine objeleri yeniden kullanır.</summary>
    public class Pool<T> where T : Component
    {
        readonly Stack<T> free = new();
        readonly Func<T> factory;
        readonly Transform root;

        public Pool(Func<T> factory, Transform root, int prewarm = 0)
        {
            this.factory = factory;
            this.root = root;
            for (int i = 0; i < prewarm; i++)
            {
                var t = Create();
                t.gameObject.SetActive(false);
                free.Push(t);
            }
        }

        T Create()
        {
            var t = factory();
            t.transform.SetParent(root, false);
            return t;
        }

        public T Get()
        {
            var t = free.Count > 0 ? free.Pop() : Create();
            t.gameObject.SetActive(true);
            return t;
        }

        public void Release(T t)
        {
            t.gameObject.SetActive(false);
            free.Push(t);
        }
    }
}
