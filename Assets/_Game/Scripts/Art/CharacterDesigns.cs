using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Kodla üretilen, mobil dostu düşük poligonlu karakter modelleri.
    /// Yardımcı asker (4 evrim aşaması) ve ana karakter (şövalye komutan).
    /// Modeller +Z'ye bakar, pivot ayak tabanındadır. Mesh'ler bir kez üretilip önbelleğe alınır.
    /// </summary>
    public static class CharacterDesigns
    {
        // ── Palet ─────────────────────────────────────────────
        static readonly Color Skin = new Color(0.96f, 0.76f, 0.6f);
        static readonly Color Eye = new Color(0.12f, 0.1f, 0.12f);
        static readonly Color Leather = new Color(0.45f, 0.28f, 0.15f);
        static readonly Color LeatherDark = new Color(0.3f, 0.18f, 0.1f);
        static readonly Color Wood = new Color(0.58f, 0.38f, 0.2f);
        static readonly Color WoodLight = new Color(0.72f, 0.5f, 0.28f);
        static readonly Color Brass = new Color(0.85f, 0.65f, 0.28f);
        static readonly Color Iron = new Color(0.42f, 0.45f, 0.5f);
        static readonly Color IronDark = new Color(0.22f, 0.24f, 0.28f);
        static readonly Color Steel = new Color(0.58f, 0.62f, 0.7f);
        static readonly Color SteelBright = new Color(0.76f, 0.8f, 0.88f);
        static readonly Color Navy = new Color(0.18f, 0.26f, 0.52f);
        static readonly Color NavyDark = new Color(0.12f, 0.16f, 0.32f);
        static readonly Color Blue = new Color(0.2f, 0.42f, 0.95f);
        static readonly Color RoyalBlue = new Color(0.14f, 0.26f, 0.78f);
        static readonly Color Glow = new Color(0.45f, 0.9f, 1f);
        static readonly Color Gold = new Color(1f, 0.78f, 0.28f);
        static readonly Color GoldDark = new Color(0.78f, 0.55f, 0.16f);
        static readonly Color White = new Color(0.95f, 0.95f, 0.97f);

        public const int SoldierTiers = 4;
        /// <summary>Asker silahının namlu ucu (yerel koordinat) — mermiler buradan çıkar.</summary>
        public static readonly Vector3 SoldierMuzzle = new Vector3(0.12f, 0.47f, 0.6f);

        static Mesh[] soldierCache = new Mesh[SoldierTiers];
        static Mesh heroBody, heroArm;

        // Kahraman ölçeği (asker ölçüsünde tasarlanıp büyütülür)
        public const float HeroScale = 1.9f;
        /// <summary>Silah namlusu (omuz pivotuna göre, ölçeksiz).</summary>
        static readonly Vector3 HeroGunMuzzle = new Vector3(0f, -0.1f, 0.8f);
        /// <summary>Kahramanın namlu ucu (kahramanın yerel koordinatında) — mermiler buradan çıkar.</summary>
        public static Vector3 HeroMuzzle => HeroShoulder + HeroGunMuzzle * HeroScale;
        /// <summary>Silah kolunun omuz eklemi (kahramanın yerel koordinatında, ölçekli).</summary>
        public static readonly Vector3 HeroShoulder = new Vector3(0.23f, 0.62f, 0f) * HeroScale;

        static Material material;
        /// <summary>Tüm kod-modelleri için paylaşılan tek materyal (SRP Batcher / instancing dostu).</summary>
        public static Material Material
        {
            get
            {
                if (material != null) return material;
                var db = GameDatabase.Load();
                material = db != null ? db.characterMaterial : null;
                if (material == null)
                {
                    var sh = Shader.Find("StratejiGame/VertexColorToon");
                    material = new Material(sh != null ? sh : Shader.Find("Universal Render Pipeline/Lit")) { enableInstancing = true };
                }
                return material;
            }
        }

        /// <summary>Verilen mesh ile bir görsel alt nesne oluşturur.</summary>
        public static GameObject MakeVisual(string name, Mesh mesh, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = false;
            return go;
        }

        
public static Mesh Soldier(int tier)
        {
            tier = Mathf.Clamp(tier, 0, SoldierTiers - 1);
            if (soldierCache[tier] == null) soldierCache[tier] = BuildSoldier(tier);
            return soldierCache[tier];
        }

        public static Mesh HeroBody { get { if (heroBody == null) BuildHero(); return heroBody; } }
        public static Mesh HeroArm { get { if (heroArm == null) BuildHero(); return heroArm; } }

        // ─────────────────────────────────────────────────────
        // YARDIMCI ASKER
        // ─────────────────────────────────────────────────────
        static Mesh BuildSoldier(int tier)
        {
            var b = new MeshBuilder();

            Color cloth, pants, armArmor, boots;
            switch (tier)
            {
                case 1: cloth = new Color(0.3f, 0.36f, 0.55f); pants = NavyDark; armArmor = Steel; boots = LeatherDark; break;
                case 2: cloth = SteelBright; pants = Steel; armArmor = SteelBright; boots = Iron; break;
                case 3: cloth = Gold; pants = RoyalBlue; armArmor = Gold; boots = GoldDark; break;
                default: cloth = Navy; pants = NavyDark; armArmor = Navy; boots = Leather; break;
            }

            // Bacaklar + botlar
            for (int s = -1; s <= 1; s += 2)
            {
                b.Box(new Vector3(0.09f * s, 0.19f, 0f), new Vector3(0.12f, 0.26f, 0.13f), pants);
                b.Box(new Vector3(0.09f * s, 0.05f, 0.02f), new Vector3(0.14f, 0.1f, 0.2f), boots, default, 1f, 0.8f);
            }

            // Gövde (omuzlara doğru genişler)
            b.Box(new Vector3(0f, 0.47f, 0f), new Vector3(0.32f, 0.34f, 0.2f), cloth, default, 1.15f, 1.05f);
            // Kemer + toka
            b.Box(new Vector3(0f, 0.32f, 0f), new Vector3(0.34f, 0.06f, 0.22f), tier == 3 ? RoyalBlue : Leather);
            b.Box(new Vector3(0f, 0.32f, 0.115f), new Vector3(0.07f, 0.06f, 0.02f), tier >= 2 ? Gold : Brass);

            switch (tier)
            {
                case 0:
                    // Çapraz kayış (ön + arka)
                    b.Box(new Vector3(0f, 0.48f, 0.105f), new Vector3(0.05f, 0.4f, 0.012f), Leather, new Vector3(0f, 0f, 38f));
                    b.Box(new Vector3(0f, 0.48f, -0.105f), new Vector3(0.05f, 0.4f, 0.012f), Leather, new Vector3(0f, 0f, 38f));
                    break;
                case 1:
                    // Çelik göğüslük + omuzluklar
                    b.Box(new Vector3(0f, 0.5f, 0.004f), new Vector3(0.35f, 0.26f, 0.23f), Steel, default, 1.1f, 1f);
                    for (int s = -1; s <= 1; s += 2)
                        b.Sphere(new Vector3(0.2f * s, 0.62f, 0f), new Vector3(0.09f, 0.06f, 0.1f), Steel, 7, 4);
                    break;
                case 2:
                    // Tam plaka: göğüs plakası çizgisi + mavi tabard
                    b.Box(new Vector3(0f, 0.5f, 0.004f), new Vector3(0.36f, 0.26f, 0.23f), SteelBright, default, 1.1f, 1f);
                    b.Box(new Vector3(0f, 0.2f, 0.112f), new Vector3(0.14f, 0.2f, 0.015f), Blue, default, 1.2f, 1f);
                    b.Box(new Vector3(0f, 0.2f, -0.112f), new Vector3(0.14f, 0.2f, 0.015f), Blue, default, 1.2f, 1f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        b.Sphere(new Vector3(0.21f * s, 0.62f, 0f), new Vector3(0.1f, 0.07f, 0.11f), SteelBright, 7, 4);
                        b.Box(new Vector3(0.22f * s, 0.6f, 0f), new Vector3(0.04f, 0.03f, 0.2f), Blue);
                    }
                    break;
                case 3:
                    // Altın plaka + mavi kenarlar + omuz pelerini
                    b.Box(new Vector3(0f, 0.5f, 0.004f), new Vector3(0.36f, 0.26f, 0.23f), Gold, default, 1.1f, 1f);
                    b.Box(new Vector3(0f, 0.5f, 0.118f), new Vector3(0.05f, 0.2f, 0.01f), RoyalBlue);
                    b.Box(new Vector3(0f, 0.2f, 0.112f), new Vector3(0.14f, 0.2f, 0.015f), RoyalBlue, default, 1.2f, 1f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        b.Sphere(new Vector3(0.21f * s, 0.62f, 0f), new Vector3(0.1f, 0.07f, 0.11f), Gold, 7, 4);
                        b.Box(new Vector3(0.23f * s, 0.595f, 0f), new Vector3(0.035f, 0.025f, 0.21f), RoyalBlue);
                    }
                    break;
            }

            // Kollar: sağ kol tetikte, sol kol namluyu tutar
            b.Box(new Vector3(0.21f, 0.5f, 0.02f), new Vector3(0.09f, 0.2f, 0.1f), armArmor);
            b.Box(new Vector3(0.18f, 0.42f, 0.12f), new Vector3(0.085f, 0.085f, 0.2f), armArmor, new Vector3(0f, -12f, 0f));
            b.Sphere(new Vector3(0.14f, 0.43f, 0.22f), new Vector3(0.045f, 0.045f, 0.045f), tier >= 2 ? armArmor : Skin, 6, 4);
            b.Box(new Vector3(-0.21f, 0.5f, 0.02f), new Vector3(0.09f, 0.2f, 0.1f), armArmor);
            b.Box(new Vector3(-0.05f, 0.44f, 0.22f), new Vector3(0.085f, 0.085f, 0.36f), armArmor, new Vector3(0f, 44f, 0f));
            b.Sphere(new Vector3(0.1f, 0.46f, 0.36f), new Vector3(0.045f, 0.045f, 0.045f), tier >= 2 ? armArmor : Skin, 6, 4);

            // Baş
            b.Sphere(new Vector3(0f, 0.8f, 0.01f), new Vector3(0.16f, 0.16f, 0.16f), Skin, 10, 6);
            if (tier == 0)
                for (int s = -1; s <= 1; s += 2)
                    b.Box(new Vector3(0.055f * s, 0.8f, 0.155f), new Vector3(0.035f, 0.045f, 0.02f), Eye);

            BuildSoldierHelmet(b, tier);
            BuildSoldierBack(b, tier);
            BuildSoldierGun(b, tier);

            return b.ToMesh("Soldier_T" + tier);
        }

        static void BuildSoldierHelmet(MeshBuilder b, int tier)
        {
            switch (tier)
            {
                case 0:
                    // Açık demir miğfer + kulaklıklar + siperlik
                    b.Sphere(new Vector3(0f, 0.85f, 0f), new Vector3(0.18f, 0.13f, 0.18f), Iron, 10, 5);
                    b.Cylinder(new Vector3(0f, 0.83f, 0f), new Vector3(0f, 0.86f, 0f), 0.2f, 0.2f, 10, IronDark);
                    for (int s = -1; s <= 1; s += 2)
                        b.Box(new Vector3(0.165f * s, 0.75f, -0.01f), new Vector3(0.035f, 0.12f, 0.12f), Iron);
                    break;
                case 1:
                    // Kapalı miğfer + vizör yarığı + tepe sırtı
                    b.Sphere(new Vector3(0f, 0.82f, 0f), new Vector3(0.18f, 0.19f, 0.18f), Steel, 10, 6);
                    b.Box(new Vector3(0f, 0.81f, 0.165f), new Vector3(0.2f, 0.03f, 0.03f), IronDark);
                    b.Box(new Vector3(0f, 0.99f, -0.01f), new Vector3(0.035f, 0.05f, 0.28f), Steel);
                    break;
                case 2:
                    // Parlak miğfer + mavi sorguç
                    b.Sphere(new Vector3(0f, 0.82f, 0f), new Vector3(0.185f, 0.19f, 0.185f), SteelBright, 10, 6);
                    b.Box(new Vector3(0f, 0.81f, 0.17f), new Vector3(0.2f, 0.03f, 0.03f), IronDark);
                    b.Box(new Vector3(0f, 0.81f, 0.17f), new Vector3(0.03f, 0.12f, 0.035f), SteelBright);
                    b.Sphere(new Vector3(0f, 1.03f, -0.06f), new Vector3(0.045f, 0.09f, 0.2f), Blue, 6, 5, new Vector3(-15f, 0f, 0f));
                    break;
                default:
                    // Altın miğfer + taç dişleri + mavi tüy
                    b.Sphere(new Vector3(0f, 0.82f, 0f), new Vector3(0.185f, 0.19f, 0.185f), Gold, 10, 6);
                    b.Box(new Vector3(0f, 0.81f, 0.17f), new Vector3(0.2f, 0.03f, 0.03f), NavyDark);
                    b.Cylinder(new Vector3(0f, 0.9f, 0f), new Vector3(0f, 0.93f, 0f), 0.19f, 0.185f, 10, RoyalBlue, false);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = (-60f + i * 30f) * Mathf.Deg2Rad;
                        b.Box(new Vector3(Mathf.Sin(a) * 0.15f, 1.02f, Mathf.Cos(a) * 0.15f - 0.02f), new Vector3(0.04f, 0.07f, 0.03f),
                            Gold, new Vector3(0f, a * Mathf.Rad2Deg, 0f), 0.3f, 0.3f);
                    }
                    b.Sphere(new Vector3(0f, 1.06f, -0.08f), new Vector3(0.04f, 0.08f, 0.18f), Blue, 6, 5, new Vector3(-20f, 0f, 0f));
                    break;
            }
        }

        static void BuildSoldierBack(MeshBuilder b, int tier)
        {
            switch (tier)
            {
                case 0:
                    // Sırtta yuvarlak tahta kalkan
                    b.Cylinder(new Vector3(0f, 0.5f, -0.11f), new Vector3(0f, 0.5f, -0.15f), 0.18f, 0.18f, 10, Wood);
                    b.Box(new Vector3(0f, 0.5f, -0.152f), new Vector3(0.34f, 0.035f, 0.01f), WoodLight);
                    b.Sphere(new Vector3(0f, 0.5f, -0.155f), new Vector3(0.05f, 0.05f, 0.03f), Iron, 6, 4);
                    break;
                case 1:
                    // Çelik takviyeli kalkan
                    b.Cylinder(new Vector3(0f, 0.5f, -0.11f), new Vector3(0f, 0.5f, -0.15f), 0.2f, 0.2f, 12, Steel);
                    b.Cylinder(new Vector3(0f, 0.5f, -0.145f), new Vector3(0f, 0.5f, -0.16f), 0.16f, 0.16f, 12, Wood);
                    b.Box(new Vector3(0f, 0.5f, -0.163f), new Vector3(0.3f, 0.04f, 0.01f), Steel);
                    b.Box(new Vector3(0f, 0.5f, -0.163f), new Vector3(0.04f, 0.3f, 0.01f), Steel);
                    b.Sphere(new Vector3(0f, 0.5f, -0.168f), new Vector3(0.05f, 0.05f, 0.035f), SteelBright, 6, 4);
                    break;
                case 2:
                    // Enerji sırt çantası (parlayan çekirdek)
                    b.Box(new Vector3(0f, 0.5f, -0.15f), new Vector3(0.24f, 0.26f, 0.1f), Iron, default, 0.9f, 0.9f);
                    b.Box(new Vector3(0f, 0.5f, -0.2f), new Vector3(0.1f, 0.16f, 0.02f), Glow);
                    b.Cylinder(new Vector3(-0.08f, 0.64f, -0.17f), new Vector3(-0.08f, 0.72f, -0.17f), 0.025f, 0.02f, 6, SteelBright);
                    b.Cylinder(new Vector3(0.08f, 0.64f, -0.17f), new Vector3(0.08f, 0.72f, -0.17f), 0.025f, 0.02f, 6, SteelBright);
                    break;
                default:
                    // Mavi pelerin (arkadan en çok görünen parça) + altın kenar
                    b.Box(new Vector3(0f, 0.38f, -0.15f), new Vector3(0.36f, 0.52f, 0.03f), RoyalBlue, new Vector3(-10f, 0f, 0f), 0.78f);
                    b.Box(new Vector3(0f, 0.125f, -0.197f), new Vector3(0.37f, 0.035f, 0.035f), Gold, new Vector3(-10f, 0f, 0f));
                    b.Box(new Vector3(0f, 0.43f, -0.172f), new Vector3(0.07f, 0.07f, 0.01f), Gold, new Vector3(-10f, 0f, 45f));
                    break;
            }
        }

        static void BuildSoldierGun(MeshBuilder b, int tier)
        {
            float x = SoldierMuzzle.x, y = SoldierMuzzle.y;
            switch (tier)
            {
                case 0:
                    // Tromblon: ahşap dipçik + pirinç, ağzı genişleyen namlu
                    b.Box(new Vector3(x, y - 0.03f, 0.02f), new Vector3(0.055f, 0.09f, 0.2f), Wood, new Vector3(-10f, 0f, 0f));
                    b.Cylinder(new Vector3(x, y, 0.1f), new Vector3(x, y, 0.5f), 0.028f, 0.034f, 6, Brass);
                    b.Cylinder(new Vector3(x, y, 0.49f), new Vector3(x, y, 0.58f), 0.036f, 0.06f, 7, Brass);
                    break;
                case 1:
                    // Kollu tüfek: ahşap gövde, koyu namlu, tetik kolu
                    b.Box(new Vector3(x, y - 0.03f, 0.02f), new Vector3(0.05f, 0.08f, 0.22f), Wood, new Vector3(-8f, 0f, 0f));
                    b.Box(new Vector3(x, y, 0.17f), new Vector3(0.06f, 0.07f, 0.14f), IronDark);
                    b.Box(new Vector3(x, y - 0.06f, 0.17f), new Vector3(0.02f, 0.05f, 0.08f), IronDark);
                    b.Cylinder(new Vector3(x, y + 0.01f, 0.23f), new Vector3(x, y + 0.01f, 0.6f), 0.02f, 0.02f, 6, IronDark);
                    b.Box(new Vector3(x, y - 0.02f, 0.36f), new Vector3(0.045f, 0.035f, 0.2f), Wood);
                    break;
                case 2:
                    // Süslü tüfek: beyaz-çelik gövde, mavi enerji halkaları
                    b.Box(new Vector3(x, y - 0.02f, 0.05f), new Vector3(0.055f, 0.09f, 0.28f), SteelBright, new Vector3(-6f, 0f, 0f));
                    b.Box(new Vector3(x, y + 0.055f, 0.14f), new Vector3(0.03f, 0.03f, 0.12f), IronDark);
                    b.Cylinder(new Vector3(x, y, 0.19f), new Vector3(x, y, 0.62f), 0.026f, 0.022f, 6, SteelBright);
                    for (int i = 0; i < 3; i++)
                    {
                        float z = 0.3f + i * 0.1f;
                        b.Cylinder(new Vector3(x, y, z), new Vector3(x, y, z + 0.03f), 0.038f, 0.038f, 6, Glow);
                    }
                    break;
                default:
                    // Enerji tüfeği: altın gövde, parlayan namlu
                    b.Box(new Vector3(x, y - 0.02f, 0.05f), new Vector3(0.06f, 0.1f, 0.3f), White, new Vector3(-6f, 0f, 0f));
                    b.Box(new Vector3(x, y + 0.02f, 0.2f), new Vector3(0.07f, 0.08f, 0.12f), Gold);
                    b.Cylinder(new Vector3(x, y, 0.24f), new Vector3(x, y, 0.64f), 0.03f, 0.024f, 6, Glow);
                    b.Cylinder(new Vector3(x, y, 0.26f), new Vector3(x, y, 0.3f), 0.045f, 0.045f, 6, Gold);
                    b.Cylinder(new Vector3(x, y, 0.56f), new Vector3(x, y, 0.6f), 0.042f, 0.042f, 6, Gold);
                    break;
            }
        }

        // ─────────────────────────────────────────────────────
        // ANA KARAKTER: ŞÖVALYE KOMUTAN
        // ─────────────────────────────────────────────────────
        static void BuildHero()
        {
            var b = new MeshBuilder();
            b.M = Matrix4x4.Scale(Vector3.one * HeroScale);

            // Bacaklar: çelik plaka + koyu çizme + altın diz kapağı
            for (int s = -1; s <= 1; s += 2)
            {
                b.Box(new Vector3(0.095f * s, 0.2f, 0f), new Vector3(0.13f, 0.28f, 0.14f), Steel);
                b.Box(new Vector3(0.095f * s, 0.05f, 0.025f), new Vector3(0.15f, 0.1f, 0.22f), IronDark, default, 1f, 0.8f);
                b.Sphere(new Vector3(0.095f * s, 0.22f, 0.07f), new Vector3(0.05f, 0.045f, 0.03f), Gold, 6, 4);
            }

            // Etek (mavi tabard) ön/arka
            b.Box(new Vector3(0f, 0.24f, 0.09f), new Vector3(0.2f, 0.22f, 0.02f), RoyalBlue, new Vector3(8f, 0f, 0f), 1.25f);
            b.Box(new Vector3(0f, 0.24f, -0.09f), new Vector3(0.2f, 0.22f, 0.02f), RoyalBlue, new Vector3(-8f, 0f, 0f), 1.25f);

            // Gövde + göğüs plakası
            b.Box(new Vector3(0f, 0.47f, 0f), new Vector3(0.33f, 0.34f, 0.21f), Steel, default, 1.18f, 1.05f);
            b.Box(new Vector3(0f, 0.5f, 0.012f), new Vector3(0.36f, 0.24f, 0.22f), SteelBright, default, 1.1f, 1f);
            // Altın göğüs amblemi + kemer
            b.Box(new Vector3(0f, 0.52f, 0.124f), new Vector3(0.07f, 0.07f, 0.01f), Gold, new Vector3(0f, 0f, 45f));
            b.Box(new Vector3(0f, 0.33f, 0f), new Vector3(0.35f, 0.06f, 0.23f), LeatherDark);
            b.Box(new Vector3(0f, 0.33f, 0.117f), new Vector3(0.08f, 0.065f, 0.02f), Gold);

            // Omuzluklar (katmanlı, altın kenarlı)
            for (int s = -1; s <= 1; s += 2)
            {
                b.Sphere(new Vector3(0.22f * s, 0.63f, 0f), new Vector3(0.12f, 0.085f, 0.13f), SteelBright, 8, 5);
                b.Box(new Vector3(0.25f * s, 0.585f, 0f), new Vector3(0.05f, 0.03f, 0.23f), Gold, new Vector3(0f, 0f, -25f * s));
            }

            // Pelerin (arkadan görünen ana parça)
            b.Box(new Vector3(0f, 0.4f, -0.155f), new Vector3(0.42f, 0.6f, 0.035f), RoyalBlue, new Vector3(-12f, 0f, 0f), 0.72f);
            b.Box(new Vector3(0f, 0.115f, -0.21f), new Vector3(0.43f, 0.035f, 0.04f), Gold, new Vector3(-12f, 0f, 0f));
            b.Box(new Vector3(0f, 0.4f, -0.182f), new Vector3(0.09f, 0.09f, 0.01f), Gold, new Vector3(-12f, 0f, 45f));
            for (int s = -1; s <= 1; s += 2)
                b.Sphere(new Vector3(0.13f * s, 0.66f, -0.12f), new Vector3(0.035f, 0.035f, 0.035f), Gold, 6, 4);

            // Sol kol + kalkan (sol yanda)
            b.Box(new Vector3(-0.23f, 0.49f, 0.01f), new Vector3(0.1f, 0.22f, 0.11f), Steel);
            b.Box(new Vector3(-0.24f, 0.36f, 0.05f), new Vector3(0.1f, 0.12f, 0.12f), SteelBright);
            b.Box(new Vector3(-0.3f, 0.42f, 0.06f), new Vector3(0.035f, 0.36f, 0.26f), Gold, default, 1f, 1.15f);
            b.Box(new Vector3(-0.31f, 0.42f, 0.06f), new Vector3(0.03f, 0.31f, 0.21f), RoyalBlue, default, 1f, 1.15f);
            b.Box(new Vector3(-0.328f, 0.44f, 0.06f), new Vector3(0.01f, 0.08f, 0.08f), Gold, new Vector3(45f, 0f, 0f));

            // Baş: büyük miğfer (kapalı), vizör, altın haç ve tepelik
            b.Sphere(new Vector3(0f, 0.8f, 0.01f), new Vector3(0.15f, 0.15f, 0.15f), Skin, 8, 5);
            b.Cylinder(new Vector3(0f, 0.68f, 0f), new Vector3(0f, 0.9f, 0f), 0.175f, 0.17f, 12, SteelBright);
            b.Sphere(new Vector3(0f, 0.9f, 0f), new Vector3(0.17f, 0.1f, 0.17f), SteelBright, 12, 6);
            b.Box(new Vector3(0f, 0.8f, 0.168f), new Vector3(0.22f, 0.035f, 0.02f), IronDark);
            b.Box(new Vector3(0f, 0.76f, 0.172f), new Vector3(0.035f, 0.12f, 0.02f), Gold);
            b.Box(new Vector3(0f, 0.86f, 0.172f), new Vector3(0.22f, 0.02f, 0.02f), Gold);
            b.Box(new Vector3(0f, 1.02f, -0.01f), new Vector3(0.035f, 0.1f, 0.3f), Gold, default, 1f, 0.8f);
            b.Sphere(new Vector3(0f, 1.1f, -0.12f), new Vector3(0.05f, 0.1f, 0.2f), Blue, 6, 5, new Vector3(-25f, 0f, 0f));

            heroBody = b.ToMesh("Hero_Body");

            // ── Silah kolu: omuz ekleminde pivot; model yerel koordinatı omuza göre ──
            var a = new MeshBuilder();
            a.M = Matrix4x4.Scale(Vector3.one * HeroScale);
            a.Box(new Vector3(0.005f, -0.085f, 0.035f), new Vector3(0.1f, 0.2f, 0.11f), Steel, new Vector3(-22f, 0f, 0f));
            a.Box(new Vector3(0f, -0.17f, 0.17f), new Vector3(0.1f, 0.1f, 0.2f), SteelBright);
            a.Box(new Vector3(0f, -0.17f, 0.27f), new Vector3(0.115f, 0.115f, 0.05f), Gold);
            a.Sphere(new Vector3(0f, -0.17f, 0.31f), new Vector3(0.05f, 0.05f, 0.05f), SteelBright, 6, 4);
            // Komutan tüfeği: ahşap kabza/dipçik, koyu gövde, altın plakalar, parlayan çekirdek, çelik namlu
            float gy = HeroGunMuzzle.y;
            a.Box(new Vector3(0f, -0.16f, 0.31f), new Vector3(0.045f, 0.1f, 0.06f), Wood, new Vector3(15f, 0f, 0f));
            a.Box(new Vector3(0f, gy - 0.03f, 0.19f), new Vector3(0.05f, 0.07f, 0.2f), Wood, new Vector3(8f, 0f, 0f));
            a.Box(new Vector3(0f, gy, 0.36f), new Vector3(0.075f, 0.085f, 0.18f), IronDark);
            a.Box(new Vector3(0f, gy, 0.36f), new Vector3(0.085f, 0.04f, 0.12f), Gold);
            a.Box(new Vector3(0f, gy + 0.05f, 0.36f), new Vector3(0.035f, 0.02f, 0.1f), Glow);
            a.Cylinder(new Vector3(0f, gy, 0.44f), new Vector3(0f, gy, HeroGunMuzzle.z - 0.06f), 0.03f, 0.028f, 8, Steel);
            a.Cylinder(new Vector3(0f, gy - 0.035f, 0.44f), new Vector3(0f, gy - 0.035f, 0.62f), 0.02f, 0.02f, 6, IronDark);
            a.Cylinder(new Vector3(0f, gy, 0.51f), new Vector3(0f, gy, 0.535f), 0.04f, 0.04f, 8, Gold);
            a.Cylinder(new Vector3(0f, gy, 0.64f), new Vector3(0f, gy, 0.665f), 0.04f, 0.04f, 8, Gold);
            a.Cylinder(new Vector3(0f, gy, HeroGunMuzzle.z - 0.07f), new Vector3(0f, gy, HeroGunMuzzle.z), 0.045f, 0.042f, 8, IronDark);
            heroArm = a.ToMesh("Hero_GunArm");
        }
    }

    /// <summary>Kahramanın ateş etme (geri tepme) ve nefes alma animasyonu (kemik gerektirmez).</summary>
    public class HeroAnimator : MonoBehaviour
    {
        public Transform body;
        public Transform arm;
        float recoilT = 1f;
        Vector3 armBase;
        bool hasBase;
        const float RecoilTime = 0.18f;

        /// <summary>Her atışta çağrılır: namlu yukarı kalkar, kol geri itilir, sonra yerine döner.</summary>
        public void Fire() => recoilT = 0f;

        void Update()
        {
            float time = Time.time;
            if (body != null) body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(time * 3f)) * 0.03f, 0f);

            if (arm == null) return;
            if (!hasBase) { armBase = arm.localPosition; hasBase = true; }
            recoilT = Mathf.Min(1f, recoilT + Time.deltaTime / RecoilTime);
            // 0 → 0.15: sert tepme, 0.15 → 1: yumuşak dönüş
            float k = recoilT < 0.15f ? recoilT / 0.15f : 1f - Mathf.SmoothStep(0f, 1f, (recoilT - 0.15f) / 0.85f);
            arm.localRotation = Quaternion.Euler(-16f * k, 0f, 0f);
            arm.localPosition = armBase + new Vector3(0f, 0f, -0.12f * k);
        }
    }
}
