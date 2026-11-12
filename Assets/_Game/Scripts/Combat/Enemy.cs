using UnityEngine;

namespace StratejiGame
{
    // Not: yeni türler sona eklenir (seviye dosyaları bu sayısal değerleri saklar)
    public enum EnemyKind { Minion, Brute, Harpooner, Boss, Gunner }

    /// <summary>Düşman birimi. Koridor boyunca aşağı iner; askerlere ve ana karaktere temas hasarı verir.</summary>
    public class Enemy : MonoBehaviour
    {
        public EnemyKind kind;
        public float hp, maxHp, speed, radius, contactDamage;
        public int gold;
        public bool dead;
        public System.Action<Enemy> onDeath;
        /// <summary>Kalkan (Sonsuz Savunma zırhlı düşmanı): sıfırdan büyükken düşman hasarın yarısını alır.</summary>
        public float shield;
        GameObject shieldVis;
        public bool Armored => shield > 0f;
        /// <summary>Menzilli düşman ateş ettiğinde (animasyon için).</summary>
        public System.Action onAttack;

        [Tooltip("Harpooner: bu Z değerinde durup zıpkın atar")]
        public float stopZ = 17f;
        public float attackInterval = 2.8f;

        // --- Menzilli düşman dizilişi (EnemyManager'daki 3 şerit x 2 sıra atış yeri) ---
        /// <summary>Arka sıra ön sıranın bu kadar gerisinde durur.</summary>
        public const float BackRowOffset = 3.5f;
        /// <summary>Yer bulamayan nişancı ön sıranın bu kadar gerisinde bekler (ateş etmez).</summary>
        public const float WaitOffset = 7.5f;
        /// <summary>Arka sıra bu kat daha seyrek ateş eder (1.5: ön sıranın üçte ikisi hızında).</summary>
        public const float BackRowFireMult = 1.5f;
        const float LateralSpeed = 3f;
        int slotRow = -1, slotLane = -1;
        float slotCheckT;
        /// <summary>Bekleyenler üst üste binmesin: sırayla 3 şeride ve 3 derinliğe dağılır.</summary>
        static int waitSeq;
        int waitIdx;
        /// <summary>0 ön sıra, 1 arka sıra, -1 yer yok (bekliyor / henüz gelmedi).</summary>
        public int SlotRow => slotRow;

        GameManager gm;
        Renderer[] rends;
        Material[] baseMats;
        MaterialPropertyBlock mpb;

        // Vurulma parlaması: renderer'a MaterialPropertyBlock vermek SRP Batcher'ı o nesne için bozuyordu (kalabalıkta
        // işlemci yükü). Bunun yerine her malzemenin önceden hazırlanmış parlak kopyasına geçilir, sonra geri dönülür.
        static readonly System.Collections.Generic.Dictionary<Material, Material> flashMats = new System.Collections.Generic.Dictionary<Material, Material>();

        static Material FlashOf(Material m)
        {
            if (m == null || !m.HasProperty("_BaseColor")) return m;
            if (!flashMats.TryGetValue(m, out var f) || f == null)
            {
                f = new Material(m) { name = m.name + "_Flash" };
                f.SetColor("_BaseColor", new Color(2.2f, 2.2f, 2.2f, 1f)); // dokulu modellerde de parlama görünsün
                flashMats[m] = f;
            }
            return f;
        }

        void CaptureMaterials()
        {
            baseMats = new Material[rends.Length];
            for (int i = 0; i < rends.Length; i++) baseMats[i] = rends[i].sharedMaterial;
        }

        void SetFlash(bool on)
        {
            if (rends == null || baseMats == null) return;
            for (int i = 0; i < rends.Length; i++)
                if (rends[i] != null && baseMats[i] != null) rends[i].sharedMaterial = on ? FlashOf(baseMats[i]) : baseMats[i];
        }
        float flashT, flashCooldown, attackT;

        public void Build(EnemyKind k, EnemyModel model = null)
        {
            kind = k;
            if (model != null && model.prefab != null)
            {
                // 3D model: yüzü +Z'ye bakar; düşman kökü 180° döndürüldüğü için oyuncuya bakar
                var m = Instantiate(model.prefab, transform);
                m.name = "Model";
                m.transform.localPosition = Vector3.zero;
                m.transform.localRotation = Quaternion.identity;
                bool underwater = GameManager.I == null || GameManager.I.Theme == WorldTheme.Underwater;
                // Su altı zıpkıncısı yengeç kalabalığının arkasında kaybolmasın: %25 daha büyük (vurulma alanı aynı)
                bool frostWolf = GameManager.I != null && GameManager.I.Theme == WorldTheme.Frost && k == EnemyKind.Minion;
                bool desertPup = GameManager.I != null && GameManager.I.Theme == WorldTheme.Desert && k == EnemyKind.Minion;
                // Kar diyarının kurt yavruları beyaz karda küçük görünüyordu: %30 daha büyük (vurulma alanı aynı)
                m.transform.localScale = Vector3.one * model.scale * (underwater && k == EnemyKind.Harpooner ? 1.25f : frostWolf ? 1.3f : desertPup ? 1.4f : 1f);   // çöl akrep yavruları da kumda seçilmiyordu: %40 büyük
                if (underwater && k == EnemyKind.Harpooner) Recolor(m, 285f / 360f);   // mor zırh: taş zeminden ve kırmızı yengeçlerden ayrılsın
                bool swamp = GameManager.I != null && GameManager.I.Theme == WorldTheme.Swamp;
                bool frost = GameManager.I != null && GameManager.I.Theme == WorldTheme.Frost;
                bool sky = GameManager.I != null && GameManager.I.Theme == WorldTheme.Sky;
                bool desert = GameManager.I != null && GameManager.I.Theme == WorldTheme.Desert;
                if (sky && k == EnemyKind.Brute) m.AddComponent<WingFlap>();               // griffon kanat çırpar (+ aşağıda yürüyüş sallanması)
                if (swamp && k == EnemyKind.Minion) m.AddComponent<HopMotion>();   // zehir kurbağası zıplar
                else if (frost && k == EnemyKind.Minion) m.AddComponent<GallopMotion>();   // kurt yavruları dörtnala koşar
                else if (desert && k == EnemyKind.Minion) m.AddComponent<ScuttleMotion>();   // akrep yavruları tıkır tıkır koşar
                else if (sky && k == EnemyKind.Minion) m.AddComponent<FloatMotion>();     // bulut ruhları süzülür
                else if ((sky || desert) && k == EnemyKind.Harpooner) m.AddComponent<KnightWalk>();   // Şimşek Süvarisi: parçalı yürüyüş + fırlatma animasyonu
                else
                {
                    var bob = m.AddComponent<ModelBob>();
                    bob.speed = (swamp || frost || sky || desert) && k == EnemyKind.Brute ? (frost || sky || desert ? 4f : 5f) : 9f; bob.bobHeight = 0.035f; bob.swayDegrees = 5f;
                }
                rends = GetComponentsInChildren<Renderer>();
                mpb = new MaterialPropertyBlock();
                CaptureMaterials();
                return;
            }
            switch (k)
            {
                case EnemyKind.Minion:
                    Mats.Prim(PrimitiveType.Capsule, new Color(0.85f, 0.15f, 0.15f), transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.45f, 0.55f));
                    break;
                case EnemyKind.Brute:
                    Mats.Prim(PrimitiveType.Capsule, new Color(0.55f, 0.06f, 0.08f), transform, new Vector3(0f, 0.9f, 0f), new Vector3(1.2f, 0.9f, 1.2f));
                    Mats.Prim(PrimitiveType.Cube, new Color(0.3f, 0.3f, 0.32f), transform, new Vector3(0f, 1.2f, -0.45f), new Vector3(1.1f, 0.6f, 0.3f));
                    break;
                case EnemyKind.Harpooner:
                    // Zıpkıncı: pirinç dalgıç başlığı, yosunlu yeşil zırh
                    Mats.Prim(PrimitiveType.Capsule, new Color(0.25f, 0.55f, 0.45f), transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.7f, 0.7f, 0.7f));
                    Mats.Prim(PrimitiveType.Sphere, new Color(0.8f, 0.6f, 0.25f), transform, new Vector3(0f, 1.55f, 0f), new Vector3(0.6f, 0.6f, 0.6f));
                    Mats.Prim(PrimitiveType.Cube, new Color(0.75f, 0.75f, 0.8f), transform, new Vector3(0.4f, 0.9f, -0.3f), new Vector3(0.07f, 0.07f, 1.6f));
                    break;
                case EnemyKind.Gunner:
                    // Nişancı: koyu kırmızı gövde, turuncu vizor, uzun namlu
                    Mats.Prim(PrimitiveType.Capsule, new Color(0.6f, 0.1f, 0.12f), transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.55f, 0.5f, 0.55f));
                    Mats.Prim(PrimitiveType.Cube, new Color(1f, 0.55f, 0.1f), transform, new Vector3(0f, 0.8f, -0.22f), new Vector3(0.35f, 0.12f, 0.1f));
                    Mats.Prim(PrimitiveType.Cube, new Color(0.2f, 0.2f, 0.22f), transform, new Vector3(0.22f, 0.6f, -0.45f), new Vector3(0.09f, 0.09f, 0.9f));
                    break;
            }
            rends = GetComponentsInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
            CaptureMaterials();
        }

        static readonly System.Collections.Generic.Dictionary<Mesh, Mesh> recolored = new();

        /// <summary>Modelin yeşil/turkuaz köşe renklerini verilen tona çevirir (deri, pirinç, gri tonlar aynı kalır). Mesh bir kez kopyalanır.</summary>
        static void Recolor(GameObject root, float hue)
        {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                var src = mf.sharedMesh;
                if (src == null) continue;
                if (!recolored.TryGetValue(src, out var mesh))
                {
                    mesh = Instantiate(src);
                    var cols = mesh.colors;
                    for (int i = 0; i < cols.Length; i++)
                    {
                        Color.RGBToHSV(cols[i], out float h, out float sat, out float v);
                        if (sat < 0.2f || h <= 0.2f || h >= 0.58f) continue;
                        var c = Color.HSVToRGB(hue, Mathf.Min(1f, sat * 1.25f + 0.1f), Mathf.Min(1f, v * 1.2f));
                        c.a = cols[i].a;
                        cols[i] = c;
                    }
                    mesh.colors = cols;
                    recolored[src] = mesh;
                }
                mf.sharedMesh = mesh;
            }
        }

        public bool IsRanged => kind == EnemyKind.Harpooner || kind == EnemyKind.Gunner;

        public void Setup(Vector3 pos, float hpMult)
        {
            gm = GameManager.I;
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            dead = false;
            flashT = 0f;
            onDeath = null;
            shield = 0f;
            if (shieldVis != null) shieldVis.SetActive(false);
            switch (kind)
            {
                case EnemyKind.Minion: maxHp = 14f; speed = 3.2f; radius = 0.4f; contactDamage = 5f; gold = 1; break;
                case EnemyKind.Brute: maxHp = 90f; speed = 2.2f; radius = 0.75f; contactDamage = 15f; gold = 5; break;
                case EnemyKind.Harpooner: maxHp = 60f; speed = 2.6f; radius = 0.5f; contactDamage = 10f; gold = 4; stopZ = 17f; attackInterval = 2.8f; break;
                case EnemyKind.Gunner: maxHp = 22f; speed = 3f; radius = 0.45f; contactDamage = 5f; gold = 2; stopZ = Random.Range(20f, 26f); attackInterval = 2.4f; break;
            }
            attackT = Random.Range(0.4f, attackInterval);
            slotRow = slotLane = -1;
            slotCheckT = 0f;
            waitIdx = waitSeq++;
            maxHp *= hpMult;
            hp = maxHp;
            SetFlash(false);
        }

        /// <summary>Boss gibi özel düşmanlar için.</summary>
        public void SetupCustom(float hpValue, float radiusValue)
        {
            gm = GameManager.I;
            kind = EnemyKind.Boss;
            maxHp = hp = hpValue;
            radius = radiusValue;
            gold = 100;
            rends = GetComponentsInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
            CaptureMaterials();
        }

        void Update()
        {
            if (dead || gm == null || !gm.Playing) return;
            float dt = Time.deltaTime;

            if (flashCooldown > 0f) flashCooldown -= dt;
            if (flashT > 0f)
            {
                flashT -= dt;
                if (flashT <= 0f) SetFlash(false);
            }
            if (kind == EnemyKind.Boss) return; // Boss hareketini Boss bileşeni yönetir

            var p = transform.position;
            if (IsRanged && p.z <= stopZ + WaitOffset + 3.5f) UpdateRanged(dt, ref p);
            else
            {
                p.z -= speed * dt;
                if (p.z < 10f) p.x = Mathf.MoveTowards(p.x, gm.Player.X, 1.5f * dt);
                transform.position = p;
            }

            if (p.z - radius < gm.Army.FrontZ + 0.3f)
            {
                gm.Army.TryContact(this);
                if (dead) return;
            }
            if (p.z - radius <= gm.Player.Z + 0.5f)
            {
                gm.Player.TakeDamage(contactDamage);
                Kill(false);
            }
        }

        /// <summary>
        /// Menzilli düşman: önce ön sırada boş bir şeride geçer; ön sıra doluysa arka sırada boş şeride, o da doluysa
        /// daha geride bekler (ateş etmez). Öndeki ölünce arkadaki öne çıkar. Ön sıra tam hızla, arka sıra seyrek ateş eder.
        /// Böylece nişancılar üst üste binmez, sahada aynı anda en fazla 3 tam hızlı nişancı olur.
        /// </summary>
        void UpdateRanged(float dt, ref Vector3 p)
        {
            slotCheckT -= dt;
            if (slotCheckT <= 0f && slotRow != 0)
            {
                slotCheckT = 0.25f;
                int prefer = slotRow >= 0 ? slotLane : gm.LaneOf(p.x);
                int maxRow = slotRow < 0 ? EnemyManager.RangedRows - 1 : slotRow - 1;
                if (gm.Enemies.ClaimRangedSlot(this, prefer, maxRow, out int r, out int l))
                {
                    if (slotRow >= 0) gm.Enemies.ReleaseRangedSlot(this, slotRow, slotLane);
                    slotRow = r;
                    slotLane = l;
                }
            }
            float targetZ = slotRow == 0 ? stopZ : slotRow == 1 ? stopZ + BackRowOffset : stopZ + WaitOffset + (waitIdx / GameManager.LaneCount % 3) * 1.4f;
            float targetX = gm.LaneCenter(slotRow >= 0 ? slotLane : waitIdx % GameManager.LaneCount);
            p.x = Mathf.MoveTowards(p.x, targetX, LateralSpeed * dt);
            if (p.z > targetZ) p.z = Mathf.Max(targetZ, p.z - speed * dt);
            transform.position = p;

            bool settled = slotRow >= 0 && p.z <= targetZ + 0.05f && Mathf.Abs(p.x - targetX) < 0.3f;
            if (!settled) return;
            attackT -= dt;
            if (attackT > 0f) return;
            attackT = attackInterval * (slotRow == 1 ? BackRowFireMult : 1f);
            onAttack?.Invoke();
            if (kind == EnemyKind.Harpooner)
            {
                // su altında mızrak yengeçlerin üstünden, daha yüksekten uçar
                float y = gm.Theme == WorldTheme.Underwater ? 1.55f : 0.9f;
                gm.Enemies.FireHarpoon(p + new Vector3(0f, y, -0.7f));
            }
            else gm.Enemies.FireBolt(p + new Vector3(0f, 0.6f, -0.9f));
        }

        public void TakeDamage(float dmg)
        {
            if (dead) return;
            if (shield > 0f)
            {
                // kalkan önce kırılmalı: o sürede düşman hasarın yarısını alır
                shield -= dmg;
                hp -= dmg * 0.5f;
                if (shield <= 0f) BreakShield();
            }
            else hp -= dmg;
            // Sürekli vurulan düşman (örn. boss) beyaz kalmasın diye yanıp sönme aralıklı yapılır
            if (flashCooldown <= 0f && rends != null)
            {
                SetFlash(true);
                flashT = 0.05f;
                flashCooldown = kind == EnemyKind.Boss ? 0.25f : 0.12f;
            }
            if (hp <= 0f) Kill(true);
        }

        /// <summary>
        /// Zırhlı yapar: kalkan, düşmanın can değeri kadar hasar emer (kırılana kadar düşman yarım hasar alır,
        /// toplamda ~1,5 kat dayanıklı). Önünde oyuncuya bakan çelik kalkan görünür.
        /// </summary>
        public void SetArmor()
        {
            if (kind == EnemyKind.Boss || dead) return;
            shield = maxHp;
            if (shieldVis == null) shieldVis = BuildShield();
            shieldVis.SetActive(true);
        }

        GameObject BuildShield()
        {
            // kök 180° döndürülmüş: yerel +Z oyuncuya bakar
            var root = new GameObject("Shield");
            root.transform.SetParent(transform, false);
            float d = radius * 1.6f;
            root.transform.localPosition = new Vector3(0f, radius * 1.0f, radius + 0.1f);
            root.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);   // üstü geriye yatık: yukarıdaki kameraya bakar
            var rim = Mats.Prim(PrimitiveType.Cylinder, new Color(0.85f, 0.65f, 0.2f), root.transform, Vector3.zero, new Vector3(d * 1.08f, 0.05f, d * 1.08f));
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var face = Mats.Prim(PrimitiveType.Cylinder, new Color(0.78f, 0.86f, 1f), root.transform, new Vector3(0f, 0f, 0.03f), new Vector3(d, 0.05f, d));
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Mats.Prim(PrimitiveType.Sphere, new Color(1f, 0.78f, 0.25f), root.transform, new Vector3(0f, 0f, 0.07f), Vector3.one * d * 0.24f);
            foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return root;
        }

        void BreakShield()
        {
            shield = 0f;
            if (shieldVis != null)
            {
                Effects.ShieldBreak(shieldVis.transform.position);
                shieldVis.SetActive(false);
            }
            Sfx.Play(Snd.Hit, 0.8f);
        }

        public void Kill(bool byPlayer)
        {
            if (dead) return;
            dead = true;
            if (slotRow >= 0 && gm != null && gm.Enemies != null) gm.Enemies.ReleaseRangedSlot(this, slotRow, slotLane);
            slotRow = slotLane = -1;
            Effects.EnemyDeath(transform.position, kind);
            Sfx.Play(kind == EnemyKind.Boss ? Snd.BossDeath : kind == EnemyKind.Brute || kind == EnemyKind.Harpooner ? Snd.BruteDeath : Snd.CrabDeath);
            if (byPlayer)
            {
                gm.AddGold(gold);
                gm.KillCount++;
            }
            onDeath?.Invoke(this);
        }
    }
}
