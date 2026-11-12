using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    public class Soldier : MonoBehaviour
    {
        public float fireT;
        public Transform visual;
        public float recoil;
        public float phase;
        public float frozenT;
        public GameObject iceBlock;
    }

    /// <summary>
    /// Bölüm içi yardımcı askerler. Ana karakterin etrafında altıgen halkalar halinde dizilir,
    /// karakterle birlikte kayar ve otomatik ateş eder. Görünür sınırı aşan askerler hasar bonusuna dönüşür.
    /// </summary>
    public class Army : MonoBehaviour
    {
        /// <summary>Ekranda çizilen en fazla asker (fazlası "taşma" olarak hasar bonusu verir). Zayıf cihazda 24.</summary>
        public static int VisibleCap => AppBoot.LowEnd ? 24 : 40;
        const float SoldierScale = 1.15f;
        public float spacing = 0.72f;
        public float followSharpness = 12f;
        [Tooltip("Görünür sınırı aşan her asker için hasar bonusu")]
        public float overflowBonusPerSoldier = 0.02f;
        [Tooltip("Bir askerin temasta düşmana verdiği hasar")]
        public float soldierBlockDamage = 20f;

        readonly List<Soldier> soldiers = new();
        int tier;
        int overflow;
        Pool<Soldier> pool;
        Vector3[] slots;
        GameManager gm;

        public int Count => soldiers.Count + overflow;
        public float FrontZ { get; private set; }
        public float OverflowBonus => overflow * overflowBonusPerSoldier;

        static readonly Color SoldierColor = new Color(0.30f, 0.55f, 1f);

        public void Init()
        {
            gm = GameManager.I;
            pool = new Pool<Soldier>(CreateSoldier, transform, 20);
            slots = BuildHexSlots(VisibleCap, spacing);
        }

/// <summary>Asker evrim aşamasını ayarlar; değişirse havuzdaki boştaki askerler yeni görünümle yeniden oluşturulur.</summary>
        public void SetTier(int newTier)
        {
            if (newTier == tier) return;
            tier = newTier;
            if (pool == null || soldiers.Count > 0) return;
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            pool = new Pool<Soldier>(CreateSoldier, transform, 0);
        }


Soldier CreateSoldier()
        {
            // Kodla üretilen düşük poligonlu model (Çaylak → Kıdemli → Elit → Kraliyet), ~700 üçgen
            var go = new GameObject("Soldier");
            var s = go.AddComponent<Soldier>();
            s.visual = CharacterDesigns.MakeVisual("Model", CharacterDesigns.Soldier(tier), go.transform).transform;
            s.visual.localScale = Vector3.one * SoldierScale;
            s.phase = Random.value * 10f;
            return s;
        }

        /// <summary>Merkez hariç altıgen halkalar: 6 + 12 + 18 + 24 = 60 yuva.</summary>
/// <summary>Askerler karakterin arkasında sıralar halinde dizilir (her sırada en fazla 8 asker).</summary>
        static Vector3[] BuildHexSlots(int count, float spacing)
        {
            const int perRow = 8;
            var slots = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                int row = i / perRow;
                int inRow = i % perRow;
                int rowSize = Mathf.Min(perRow, count - row * perRow);
                // Sırayı ortadan dışa doğru doldur: 0, -1, +1, -2, +2 ...
                float sign = inRow % 2 == 0 ? 1f : -1f;
                float x = rowSize % 2 == 0
                    ? (inRow / 2 + 0.5f) * sign * spacing
                    : ((inRow + 1) / 2) * sign * spacing;
                if (row % 2 == 1) x += spacing * 0.25f;
                slots[i] = new Vector3(x, 0f, -1.1f - row * spacing);
            }
            return slots;
        }

        public void Add(int n)
        {
            var center = gm.Player.transform.position;
            for (int i = 0; i < n; i++)
            {
                if (soldiers.Count < VisibleCap)
                {
                    var s = pool.Get();
                    s.frozenT = 0f;
                    if (s.iceBlock != null) s.iceBlock.SetActive(false);
                    s.transform.position = center + slots[soldiers.Count] + Vector3.back * 0.5f;
                    Effects.SoldierSpawn(s.transform.position);
                    s.fireT = Random.Range(0f, gm.stats.soldierFireInterval);
                    soldiers.Add(s);
                }
                else overflow++;
            }
        }

        public void Remove(int n)
        {
            for (int i = 0; i < n && Count > 0; i++)
            {
#if UNITY_EDITOR
                DbgGate++;
#endif
                if (overflow > 0) overflow--;
                else Kill(soldiers[soldiers.Count - 1]);
            }
        }

        public void Multiply(int factor)
        {
            if (factor > 1) Add(Count * (factor - 1));
        }

        public void Kill(Soldier s)
        {
            if (!soldiers.Remove(s)) return;
            Effects.SoldierDeath(s.transform.position);
            Sfx.Play(Snd.SoldierDeath);
            pool.Release(s);
            if (overflow > 0)
            {
                overflow--;
                Add(1);
            }
        }

        void Update()
        {
            if (gm == null || !gm.Playing) return;
            float dt = Time.deltaTime;
            var center = gm.Player.transform.position;
            float limit = gm.corridorHalfWidth - 0.25f;
            var st = gm.stats;
            float front = center.z + 0.6f;
            float t = 1f - Mathf.Exp(-followSharpness * dt);

            for (int i = 0; i < soldiers.Count; i++)
            {
                var s = soldiers[i];
                var target = center + slots[i];
                target.x = Mathf.Clamp(target.x, -limit, limit);
                var p = Vector3.Lerp(s.transform.position, target, t);
                s.transform.position = p;
                if (p.z > front) front = p.z;

                // Geri tepme + hafif nefes alma
                if (s.visual != null)
                {
                    s.recoil = Mathf.Max(0f, s.recoil - dt * 8f);
                    float bob = Mathf.Abs(Mathf.Sin(Time.time * 4f + s.phase)) * 0.025f;
                    s.visual.localPosition = new Vector3(0f, bob, -0.07f * s.recoil);
                    s.visual.localRotation = Quaternion.Euler(-6f * s.recoil, 0f, 0f);
                }


                // Donmuş asker: buz kalıbında, ateş edemez
                if (s.frozenT > 0f)
                {
                    s.frozenT -= dt;
                    if (s.frozenT <= 0f)
                    {
                        if (s.iceBlock != null) s.iceBlock.SetActive(false);
                        Effects.IceShatter(p + Vector3.up * 0.5f, 0.6f);
                    }
                    continue;
                }
                s.fireT -= dt;
                if (s.fireT <= 0f)
                {
                    s.fireT = st.Interval(st.soldierFireInterval) * Random.Range(0.9f, 1.1f);
                    s.recoil = 1f;
                    Sfx.Play(Snd.SoldierShot);
                    gm.Enemies.FireBullet(p + CharacterDesigns.SoldierMuzzle * SoldierScale, st.Damage(st.soldierDamage * (1f + OverflowBonus)), st.pierce, false);
                }
            }
            FrontZ = front;
        }

#if UNITY_EDITOR
        /// <summary>Yalnız editör (test botu): askerlerin neden öldüğü sayaçları.</summary>
        public static int DbgContact, DbgHit, DbgChain, DbgArea, DbgGate;
        public static void DbgReset() { DbgContact = DbgHit = DbgChain = DbgArea = DbgGate = 0; }
#endif

        /// <summary>Düşman askerlere değerse: asker ölür, düşman hasar alır.</summary>
        public void TryContact(Enemy e)
        {
            var ep = e.transform.position;
            float r = e.radius + 0.3f;
            float r2 = r * r;
            for (int i = soldiers.Count - 1; i >= 0 && !e.dead; i--)
            {
                var sp = soldiers[i].transform.position;
                float dx = sp.x - ep.x, dz = sp.z - ep.z;
                if (dx * dx + dz * dz < r2)
                {
#if UNITY_EDITOR
                    DbgContact++;
#endif
                    Kill(soldiers[i]);
                    e.TakeDamage(soldierBlockDamage);
                }
            }
        }

        /// <summary>Verilen noktadaki bir askeri öldürür (düşman mermileri için).</summary>
        public bool HitAt(Vector3 pos, float radius)
        {
            float r2 = radius * radius;
            for (int i = soldiers.Count - 1; i >= 0; i--)
            {
                var sp = soldiers[i].transform.position;
                float dx = sp.x - pos.x, dz = sp.z - pos.z;
                if (dx * dx + dz * dz < r2)
                {
#if UNITY_EDITOR
                    DbgHit++;
#endif
                    Kill(soldiers[i]);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Buz kristali isabeti: hitRadius içinde asker varsa, freezeRadius içindeki tüm askerler 'duration' saniye donar.
        /// Donan asker ölmez ama ateş edemez. Dönen değer donan asker sayısı (0 = isabet yok).
        /// </summary>
        public int FreezeAt(Vector3 pos, float hitRadius, float freezeRadius, float duration)
        {
            bool hit = false;
            float h2 = hitRadius * hitRadius;
            for (int i = 0; i < soldiers.Count && !hit; i++)
            {
                var sp = soldiers[i].transform.position;
                float dx = sp.x - pos.x, dz = sp.z - pos.z;
                if (dx * dx + dz * dz < h2) hit = true;
            }
            if (!hit) return 0;
            int n = 0;
            float f2 = freezeRadius * freezeRadius;
            foreach (var s in soldiers)
            {
                var sp = s.transform.position;
                float dx = sp.x - pos.x, dz = sp.z - pos.z;
                if (dx * dx + dz * dz >= f2) continue;
                s.frozenT = duration;
                if (s.iceBlock == null)
                {
                    s.iceBlock = FxDesigns.MakeFx("IceBlock", FrostCreatures.IceBlock, s.transform);
                    s.iceBlock.transform.localScale = Vector3.one * SoldierScale;
                }
                s.iceBlock.SetActive(true);
                n++;
            }
            return n;
        }

        /// <summary>
        /// Zincir yıldırım (Şimşek Süvarisi): hitRadius içinde bir asker varsa onu ve ondan sekerek en yakın 'jumps'
        /// askeri (her sıçrama en fazla jumpRadius) öldürür. Vurulanların konumları 'hits' listesine sırayla eklenir.
        /// </summary>
        public int ChainAt(Vector3 pos, float hitRadius, int jumps, float jumpRadius, List<Vector3> hits)
        {
            hits.Clear();
            Soldier first = null;
            float best = hitRadius * hitRadius;
            foreach (var s in soldiers)
            {
                var sp = s.transform.position;
                float dx = sp.x - pos.x, dz = sp.z - pos.z, d = dx * dx + dz * dz;
                if (d < best) { best = d; first = s; }
            }
            if (first == null) return 0;
            var chain = new List<Soldier> { first };
            var last = first;
            float j2 = jumpRadius * jumpRadius;
            for (int k = 0; k < jumps; k++)
            {
                Soldier next = null;
                float nb = j2;
                var lp = last.transform.position;
                foreach (var s in soldiers)
                {
                    if (chain.Contains(s)) continue;
                    var sp = s.transform.position;
                    float dx = sp.x - lp.x, dz = sp.z - lp.z, d = dx * dx + dz * dz;
                    if (d < nb) { nb = d; next = s; }
                }
                if (next == null) break;
                chain.Add(next);
                last = next;
            }
            foreach (var s in chain) hits.Add(s.transform.position);
#if UNITY_EDITOR
            DbgChain += chain.Count;
#endif
            foreach (var s in chain) Kill(s);
            return chain.Count;
        }

        /// <summary>Alan içindeki askerlerden en fazla maxKill tanesini öldürür (boss saldırıları için).</summary>
        public int KillInArea(float xMin, float xMax, float zMin, float zMax, int maxKill)
        {
            int killed = 0;
            for (int i = soldiers.Count - 1; i >= 0 && killed < maxKill; i--)
            {
                var sp = soldiers[i].transform.position;
                if (sp.x >= xMin && sp.x <= xMax && sp.z >= zMin && sp.z <= zMax)
                {
#if UNITY_EDITOR
                    DbgArea++;
#endif
                    Kill(soldiers[i]);
                    killed++;
                }
            }
            return killed;
        }
    }
}
