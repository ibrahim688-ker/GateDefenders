using UnityEngine;

namespace StratejiGame
{
    /// <summary>Oyuncu/asker mermisi. Fizik yerine basit mesafe kontrolüyle isabet eder (mobil performans için).</summary>
    public class Bullet : MonoBehaviour
    {
        public const float Speed = 28f;
        public bool active;
        public bool fromPlayer;
        float dmg;
        int pierceLeft;
        float life;
        Enemy lastHit;
        GameManager gm;

public static Bullet Create(bool player)
        {
            // Sivri uçlu mermi + arkasında parlayan iz (oyuncu: altın/turuncu, asker: bakır/mavi)
            var go = new GameObject(player ? "Bullet_Player" : "Bullet_Soldier");
            var b = go.AddComponent<Bullet>();
            var head = CharacterDesigns.MakeVisual("Head", FxDesigns.BulletHead(player), go.transform);
            head.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            FxDesigns.MakeFx("Tracer", FxDesigns.Tracer(player), go.transform);
            return b;
        }

        public void Fire(Vector3 pos, float damage, int pierce, bool isPlayer)
        {
            gm = GameManager.I;
            transform.position = pos;
            dmg = damage;
            pierceLeft = pierce;
            fromPlayer = isPlayer;
            life = 1.8f;
            lastHit = null;
            active = true;
        }

        void Update()
        {
            if (!active || gm == null || !gm.Playing) return;
            float dt = Time.deltaTime;
            var p = transform.position;
            p.z += Speed * dt;
            transform.position = p;
            life -= dt;
            if (life <= 0f || p.z > gm.spawnZ + 2f) { gm.Enemies.ReleaseBullet(this); return; }

            if (gm.Gates.TryBulletHit(p)) { Effects.Hit(p, fromPlayer); gm.Enemies.ReleaseBullet(this); return; }

            var list = gm.Enemies.Active;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.dead || e == lastHit) continue;
                var ep = e.transform.position;
                float r = e.radius + 0.15f;
                if (Mathf.Abs(ep.x - p.x) < r && Mathf.Abs(ep.z - p.z) < r + 0.35f)
                {
                    e.TakeDamage(dmg);
                    Sfx.Play(Snd.Hit);
                    Effects.Hit(new Vector3(p.x, p.y, ep.z - e.radius * 0.6f), fromPlayer, e.kind == EnemyKind.Boss || e.kind == EnemyKind.Brute);
                    lastHit = e;
                    if (pierceLeft <= 0) { gm.Enemies.ReleaseBullet(this); return; }
                    pierceLeft--;
                    dmg *= 0.7f; // delici mermi her düşmandan sonra zayıflar
                }
            }
        }
    }
}
