using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>Düşmanları, oyuncu mermilerini ve düşman mermilerini obje havuzlarıyla yönetir.</summary>
    public class EnemyManager : MonoBehaviour
    {
        public readonly List<Enemy> Active = new();
        public readonly List<Bullet> ActiveBullets = new();

        readonly Dictionary<EnemyKind, Pool<Enemy>> pools = new();
        Pool<Bullet> playerBullets, soldierBullets;
        Pool<EnemyProjectile> harpoons, bolts;
        Pool<MuzzleFlash> flashes;
        GameManager gm;

        public void Init()
        {
            gm = GameManager.I;
            BuildPools();
        }

        void BuildPools()
        {
            pools.Clear();
            foreach (var k in new[] { EnemyKind.Minion, EnemyKind.Brute, EnemyKind.Harpooner, EnemyKind.Gunner })
            {
                var kind = k;
                pools[kind] = new Pool<Enemy>(() =>
                {
                    var go = new GameObject(kind.ToString());
                    var e = go.AddComponent<Enemy>();
                    var world = gm.CurrentWorld;
                    e.Build(kind, world != null ? world.GetEnemyModel(kind) : null);
                    return e;
                }, transform, kind == EnemyKind.Minion ? 60 : 6);
            }
            if (playerBullets == null)
            {
                playerBullets = new Pool<Bullet>(() => Bullet.Create(true), transform, 30);
                soldierBullets = new Pool<Bullet>(() => Bullet.Create(false), transform, 80);
                flashes = new Pool<MuzzleFlash>(MuzzleFlash.Create, transform, 16);
            }
            harpoons = new Pool<EnemyProjectile>(EnemyProjectile.CreateHarpoon, transform, 6);
            bolts = new Pool<EnemyProjectile>(EnemyProjectile.CreateBolt, transform, 12);
        }

        /// <summary>
        /// Sonsuz Savunma'da diyar değişince: düşman ve düşman mermisi havuzları yeni diyarın modelleriyle baştan kurulur.
        /// Saha boşken çağrılır (ölmüş ama henüz havuza dönmemiş düşmanlar da silinir). Oyuncu mermileri korunur.
        /// </summary>
        public void RebuildPools()
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var e = Active[i];
                if (e != null && e.kind == EnemyKind.Boss) continue;
                Active.RemoveAt(i);
                if (e != null) Destroy(e.gameObject);
            }
            foreach (var e in GetComponentsInChildren<Enemy>(true))
                if (e.kind != EnemyKind.Boss) Destroy(e.gameObject);
            foreach (var p in GetComponentsInChildren<EnemyProjectile>(true))
                Destroy(p.gameObject);
            ClearRangedSlots();
            BuildPools();
        }

        // ------------------------------------------------------------------ menzilli düşman atış yerleri
        /// <summary>Atış yeri sıraları: 0 ön (tam hızla atar), 1 arka (seyrek atar). Her sırada şerit başına bir yer.</summary>
        public const int RangedRows = 2;
        readonly Enemy[,] rangedSlots = new Enemy[RangedRows, GameManager.LaneCount];

        /// <summary>maxRow'a kadar (dahil) ilk boş sıradaki, prefer şeridine en yakın boş yeri ayırır.</summary>
        public bool ClaimRangedSlot(Enemy e, int prefer, int maxRow, out int row, out int lane)
        {
            for (int r = 0; r <= maxRow && r < RangedRows; r++)
            {
                int best = -1, bestD = int.MaxValue;
                for (int l = 0; l < GameManager.LaneCount; l++)
                {
                    var o = rangedSlots[r, l];
                    if (o != null && o != e && !o.dead && o.gameObject.activeInHierarchy) continue;
                    int d = Mathf.Abs(l - prefer);
                    if (d < bestD) { bestD = d; best = l; }
                }
                if (best >= 0)
                {
                    rangedSlots[r, best] = e;
                    row = r;
                    lane = best;
                    return true;
                }
            }
            row = lane = -1;
            return false;
        }

        public void ReleaseRangedSlot(Enemy e, int row, int lane)
        {
            if (row < 0 || row >= RangedRows || lane < 0 || lane >= GameManager.LaneCount) return;
            if (rangedSlots[row, lane] == e) rangedSlots[row, lane] = null;
        }

        void ClearRangedSlots() => System.Array.Clear(rangedSlots, 0, rangedSlots.Length);

        public Enemy Spawn(EnemyKind kind, Vector3 pos)
        {
            var e = pools[kind].Get();
            e.Setup(pos, gm.DifficultyMult);
            Active.Add(e);
            return e;
        }

        /// <summary>Boss gibi havuz dışı düşmanları hedef listesine ekler.</summary>
        public void Register(Enemy e) => Active.Add(e);

        public void FireBullet(Vector3 pos, float dmg, int pierce, bool fromPlayer)
        {
            var b = (fromPlayer ? playerBullets : soldierBullets).Get();
            b.Fire(pos, dmg, pierce, fromPlayer);
            ActiveBullets.Add(b);
            // zayıf cihazda yalnızca ana karakterin namlu parlaması (asker parlamaları üst üste binip pahalı)
            if (fromPlayer || !AppBoot.LowEnd) flashes.Get().Play(pos, fromPlayer ? 2.1f : 1.1f, flashes);
        }

        public void ReleaseBullet(Bullet b)
        {
            if (!b.active) return;
            b.active = false;
            (b.fromPlayer ? playerBullets : soldierBullets).Release(b);
        }

        public void FireHarpoon(Vector3 pos)
        {
            var h = harpoons.Get();
            Sfx.Play(Snd.HarpoonThrow);
            h.Fire(pos, this);
        }

        public void FireBolt(Vector3 pos)
        {
            var b = bolts.Get();
            b.Fire(pos, this);
        }

        public void ReleaseProjectile(EnemyProjectile p)
        {
            if (!p.gameObject.activeSelf) return;
            (p.kind == EnemyProjectile.Kind.Bolt ? bolts : harpoons).Release(p);
        }

        void LateUpdate()
        {
            // Ölen düşmanları havuza geri gönder
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var e = Active[i];
                if (!e.dead) continue;
                Active[i] = Active[Active.Count - 1];
                Active.RemoveAt(Active.Count - 1);
                if (e.kind != EnemyKind.Boss) pools[e.kind].Release(e);
            }
            for (int i = ActiveBullets.Count - 1; i >= 0; i--)
            {
                if (ActiveBullets[i].active) continue;
                ActiveBullets[i] = ActiveBullets[ActiveBullets.Count - 1];
                ActiveBullets.RemoveAt(ActiveBullets.Count - 1);
            }
        }

        public int AliveCount
        {
            get
            {
                int n = 0;
                foreach (var e in Active) if (!e.dead) n++;
                return n;
            }
        }
    }
}
