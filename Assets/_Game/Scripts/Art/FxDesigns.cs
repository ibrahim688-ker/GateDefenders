using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Kodla üretilen mermi / efekt modelleri: oyuncu ve asker mermisi (+ parlayan iz),
    /// namlu alevi ve Zıpkıncı'nın fırlattığı mızrak. Hepsi düşük poligonlu ve önbellekli.
    /// </summary>
    public static class FxDesigns
    {
        static readonly Color Gold = new Color(1f, 0.8f, 0.3f);
        static readonly Color Copper = new Color(0.85f, 0.5f, 0.25f);
        static readonly Color SteelTip = new Color(0.75f, 0.85f, 0.95f);
        static readonly Color ShaftWood = new Color(0.5f, 0.34f, 0.18f);
        static readonly Color Bronze = new Color(0.78f, 0.56f, 0.25f);
        static readonly Color Silver = new Color(0.82f, 0.87f, 0.92f);
        static readonly Color SeaTeal = new Color(0.15f, 0.7f, 0.68f);

        static Mesh bulletPlayer, bulletSoldier, tracerPlayer, tracerSoldier, flash, harpoon;
        static Material fxMaterial;

        /// <summary>Eklemeli (parlayan) efekt materyali.</summary>
        public static Material FxMaterial
        {
            get
            {
                if (fxMaterial != null) return fxMaterial;
                var db = GameDatabase.Load();
                fxMaterial = db != null ? db.fxMaterial : null;
                if (fxMaterial == null)
                {
                    var sh = Shader.Find("StratejiGame/FxAdditive");
                    fxMaterial = new Material(sh != null ? sh : Shader.Find("Universal Render Pipeline/Unlit")) { enableInstancing = true };
                }
                return fxMaterial;
            }
        }

        public static GameObject MakeFx(string name, Mesh mesh, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = FxMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        // ── Mermi başı (sivri uçlu fişek), +Z yönünde uçar ──
        public static Mesh BulletHead(bool player)
        {
            if (player) return bulletPlayer != null ? bulletPlayer : (bulletPlayer = BuildBullet(true));
            return bulletSoldier != null ? bulletSoldier : (bulletSoldier = BuildBullet(false));
        }

        static Mesh BuildBullet(bool player)
        {
            var b = new MeshBuilder();
            float s = player ? 1f : 0.62f;
            b.M = Matrix4x4.Scale(Vector3.one * s);
            Color body = player ? Gold : Copper;
            Color tip = player ? new Color(1f, 0.92f, 0.6f) : SteelTip;
            b.Cylinder(new Vector3(0f, 0f, -0.12f), new Vector3(0f, 0f, 0.05f), 0.06f, 0.06f, 6, body);
            b.Cylinder(new Vector3(0f, 0f, 0.05f), new Vector3(0f, 0f, 0.19f), 0.06f, 0f, 6, tip);
            return b.ToMesh(player ? "Bullet_Player" : "Bullet_Soldier");
        }

        // ── Merminin arkasındaki parlayan iz (eklemeli) ──
        public static Mesh Tracer(bool player)
        {
            if (player) return tracerPlayer != null ? tracerPlayer : (tracerPlayer = BuildTracer(true));
            return tracerSoldier != null ? tracerSoldier : (tracerSoldier = BuildTracer(false));
        }

        static Mesh BuildTracer(bool player)
        {
            var b = new MeshBuilder();
            float s = player ? 1f : 0.7f;
            b.M = Matrix4x4.Scale(new Vector3(s, s, player ? 1f : 0.85f));
            Color outer = player ? new Color(1f, 0.45f, 0.08f) : new Color(0.1f, 0.55f, 1f);
            Color inner = player ? new Color(1f, 0.85f, 0.45f) : new Color(0.55f, 0.95f, 1f);
            b.Cylinder(new Vector3(0f, 0f, -0.02f), new Vector3(0f, 0f, -1.0f), 0.1f, 0f, 6, outer, false);
            b.Cylinder(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, -0.6f), 0.055f, 0f, 5, inner, false);
            b.Sphere(Vector3.zero, new Vector3(0.1f, 0.1f, 0.14f), inner, 6, 4);
            return b.ToMesh(player ? "Tracer_Player" : "Tracer_Soldier");
        }

        // ── Namlu alevi: ileri koni + yıldız yapraklar (eklemeli) ──
        public static Mesh MuzzleFlash => flash != null ? flash : (flash = BuildFlash());

        static Mesh BuildFlash()
        {
            var b = new MeshBuilder();
            var orange = new Color(1f, 0.45f, 0.1f);
            var yellow = new Color(1f, 0.85f, 0.4f);
            var core = new Color(1f, 0.95f, 0.8f);
            b.Cylinder(Vector3.zero, new Vector3(0f, 0f, 0.55f), 0.15f, 0f, 6, orange, false);
            b.Cylinder(Vector3.zero, new Vector3(0f, 0f, 0.34f), 0.085f, 0f, 6, yellow, false);
            // Yandan/arkadan görünen yıldız yapraklar
            for (int i = 0; i < 4; i++)
                b.Box(new Vector3(0f, 0f, 0.03f), new Vector3(0.5f, 0.06f, 0.005f), orange, new Vector3(0f, 0f, i * 45f), 1f, 1f);
            // Üstten görünen elmas
            b.Box(new Vector3(0f, 0f, 0.12f), new Vector3(0.2f, 0.004f, 0.2f), yellow, new Vector3(0f, 45f, 0f));
            b.Sphere(Vector3.zero, new Vector3(0.09f, 0.09f, 0.09f), core, 6, 4);
            return b.ToMesh("MuzzleFlash");
        }

        // ── Magma Berserker'in fırlattığı alevli balta: sap yerel Y ekseninde, ağız üstte (X ekseni etrafında döner) ──
        static Mesh axe;
        public static Mesh FlamingAxe => axe != null ? axe : (axe = BuildAxe());

        static Mesh BuildAxe()
        {
            var b = new MeshBuilder();
            var haft = new Color(0.16f, 0.1f, 0.08f, 1f);
            var rock = new Color(0.12f, 0.1f, 0.1f, 1f);
            var lava = new Color(1f, 0.45f, 0.08f, 0f);
            var core = new Color(1f, 0.8f, 0.3f, 0f);
            b.Cylinder(new Vector3(0f, -0.5f, 0f), new Vector3(0f, 0.45f, 0f), 0.045f, 0.04f, 6, haft);
            b.Sphere(new Vector3(0f, -0.52f, 0f), Vector3.one * 0.06f, rock, 5, 3);
            // Balta başı: koyu kaya gövde + iki yana açılan parlayan ağızlar
            b.Box(new Vector3(0f, 0.38f, 0f), new Vector3(0.14f, 0.2f, 0.1f), rock);
            for (int s = -1; s <= 1; s += 2)
            {
                b.Box(new Vector3(0.2f * s, 0.38f, 0f), new Vector3(0.28f, 0.34f, 0.04f), lava, new Vector3(0f, 0f, 0f), 1.35f, 1f);
                b.Box(new Vector3(0.12f * s, 0.38f, 0f), new Vector3(0.12f, 0.22f, 0.05f), core);
            }
            b.Box(new Vector3(0f, 0.55f, 0f), new Vector3(0.05f, 0.14f, 0.05f), lava, default(Vector3), 0.2f, 0.2f);
            return b.ToMesh("FlamingAxe");
        }

        // ── Su altı mızrağının arkasındaki parlak iz (eklemeli; uç -Z'de, iz +Z'ye uzanır) ──
        static Mesh harpoonTrail;
        public static Mesh HarpoonTrail => harpoonTrail != null ? harpoonTrail : (harpoonTrail = BuildHarpoonTrail());

        static Mesh BuildHarpoonTrail()
        {
            var b = new MeshBuilder();
            Color outer = new Color(1f, 0.25f, 0.12f);
            Color inner = new Color(1f, 0.92f, 0.75f);
            b.Cylinder(new Vector3(0f, 0f, -0.2f), new Vector3(0f, 0f, 2.2f), 0.16f, 0f, 6, outer, false);
            b.Cylinder(new Vector3(0f, 0f, -0.3f), new Vector3(0f, 0f, 1.3f), 0.075f, 0f, 5, inner, false);
            b.Sphere(new Vector3(0f, 0f, -0.25f), new Vector3(0.2f, 0.2f, 0.32f), outer, 6, 4);
            return b.ToMesh("HarpoonTrail");
        }

        // ── Zıpkıncı mızrağı: bronz sap, gümüş dikenli uç, deniz yeşili sargı ve kuyruk (uç -Z'de) ──
        public static Mesh Harpoon => harpoon != null ? harpoon : (harpoon = BuildHarpoon());

        static Mesh BuildHarpoon()
        {
            var b = new MeshBuilder();
            // Sap
            b.Cylinder(new Vector3(0f, 0f, -0.46f), new Vector3(0f, 0f, 0.82f), 0.035f, 0.032f, 6, ShaftWood);
            // Bronz bilezikler + deniz yeşili sargı
            b.Cylinder(new Vector3(0f, 0f, -0.36f), new Vector3(0f, 0f, -0.31f), 0.05f, 0.05f, 6, Bronze);
            b.Cylinder(new Vector3(0f, 0f, -0.31f), new Vector3(0f, 0f, -0.16f), 0.044f, 0.044f, 6, SeaTeal);
            b.Cylinder(new Vector3(0f, 0f, 0.22f), new Vector3(0f, 0f, 0.27f), 0.045f, 0.045f, 6, Bronze);
            // Uç: gümüş yuva + elmas kesitli sivri uç + iki geri kanca
            b.Cylinder(new Vector3(0f, 0f, -0.46f), new Vector3(0f, 0f, -0.56f), 0.045f, 0.035f, 6, Silver);
            b.Cylinder(new Vector3(0f, 0f, -0.55f), new Vector3(0f, 0f, -0.92f), 0.085f, 0f, 4, Silver, true, true);
            for (int s = -1; s <= 1; s += 2)
                b.Box(new Vector3(0.07f * s, 0f, -0.6f), new Vector3(0.025f, 0.03f, 0.17f), Silver, new Vector3(0f, 27f * s, 0f), 1f, 1f);
            // Kuyruk yüzgeçleri
            b.Box(new Vector3(0f, 0f, 0.74f), new Vector3(0.2f, 0.012f, 0.16f), SeaTeal, default, 1f, 1f);
            b.Box(new Vector3(0f, 0f, 0.74f), new Vector3(0.012f, 0.2f, 0.16f), SeaTeal, default, 1f, 1f);
            return b.ToMesh("Harpoon");
        }
    }

    /// <summary>Kısa ömürlü namlu alevi: hızla büyüyüp söner, rastgele döner, sonra havuza döner.</summary>
    public class MuzzleFlash : MonoBehaviour
    {
        public const float Life = 0.07f;
        float t;
        float size;
        Pool<MuzzleFlash> pool;

        public static MuzzleFlash Create()
        {
            var go = new GameObject("MuzzleFlash");
            FxDesigns.MakeFx("Mesh", FxDesigns.MuzzleFlash, go.transform);
            return go.AddComponent<MuzzleFlash>();
        }

        public void Play(Vector3 pos, float scale, Pool<MuzzleFlash> owner)
        {
            pool = owner;
            size = scale;
            t = 0f;
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
            transform.localScale = Vector3.one * (0.6f * size);
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = t / Life;
            if (k >= 1f) { pool.Release(this); return; }
            // Önce hızla büyür, sonra incelerek kaybolur
            float grow = k < 0.35f ? Mathf.Lerp(0.6f, 1.15f, k / 0.35f) : Mathf.Lerp(1.15f, 0.3f, (k - 0.35f) / 0.65f);
            transform.localScale = new Vector3(grow, grow, grow * (1f + k * 0.6f)) * size;
        }
    }
}
