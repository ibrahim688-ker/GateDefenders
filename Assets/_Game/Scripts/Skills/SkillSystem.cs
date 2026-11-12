using UnityEngine;
using UnityEngine.InputSystem;

namespace StratejiGame
{
    /// <summary>Ana karakterin 3 aktif yeteneği: Alan Hasarı, Can Yenileme, Öfke. (Klavye: 1, 2, 3)</summary>
    public class SkillSystem : MonoBehaviour
    {
        [Header("Alan Hasarı (Fire Slam)")]
        public float aoeCooldown = 12f;
        public float aoeRadius = 5f;
        public float aoeDamageMult = 9f;
        public float aoeForwardOffset = 6f;

        [Header("Can Yenileme (Vitality)")]
        public int healCharges = 1;
        public float healFraction = 0.25f;
        public float healGap = 10f;

        [Header("Öfke (Rage)")]
        public float rageDuration = 5f;
        public float rageCooldown = 25f;
        public float rageMult = 1.5f;

        public float AoeT { get; private set; }
        public float HealT { get; private set; }
        public float RageT { get; private set; }
        public float RageActiveT { get; private set; }
        public int HealLeft { get; private set; }

        GameManager gm;

        public void Init()
        {
            gm = GameManager.I;
            HealLeft = healCharges;
        }

        void Update()
        {
            if (gm == null || !gm.Playing) return;
            float dt = Time.deltaTime;
            AoeT = Mathf.Max(0f, AoeT - dt);
            HealT = Mathf.Max(0f, HealT - dt);
            RageT = Mathf.Max(0f, RageT - dt);
            if (RageActiveT > 0f)
            {
                RageActiveT -= dt;
                if (RageActiveT <= 0f)
                {
                    gm.stats.rageMult = 1f;
                    gm.Player.SetRageTint(false);
                }
            }

#if UNITY_EDITOR
            // Yalnız editör: 1/2/3 tuşlarıyla yetenek (geliştirici kolaylığı, yayın build'ine girmez)
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) UseAoe();
                if (kb.digit2Key.wasPressedThisFrame) UseHeal();
                if (kb.digit3Key.wasPressedThisFrame) UseRage();
            }
#endif
        }

        public bool AoeReady => AoeT <= 0f;
        public bool HealReady => HealLeft > 0 && HealT <= 0f && gm.Player.Hp < gm.Player.MaxHp;
        public bool RageReady => RageT <= 0f;

        public void UseAoe()
        {
            if (!gm.Playing || !AoeReady) return;
            AoeT = aoeCooldown;
            DailyQuests.Add(QuestType.Skills, 1);
            Sfx.Play(Snd.SkillAoe);
            var center = gm.Player.transform.position + Vector3.forward * aoeForwardOffset;
            float dmg = gm.stats.Damage(gm.stats.playerDamage * aoeDamageMult);
            foreach (var e in gm.Enemies.Active)
            {
                if (e.dead) continue;
                var d = e.transform.position - center;
                d.y = 0f;
                if (d.magnitude <= aoeRadius + e.radius) e.TakeDamage(dmg);
            }
            gm.Shake(0.35f, 0.25f);
            Fx.Ring(center + Vector3.up * 0.1f, aoeRadius, new Color(1f, 0.5f, 0.1f, 0.55f), 0.35f);
        }

        public void UseHeal()
        {
            if (!gm.Playing || HealLeft <= 0 || HealT > 0f) return;
            HealLeft--;
            HealT = healGap;
            DailyQuests.Add(QuestType.Skills, 1);
            Sfx.Play(Snd.SkillHeal);
            gm.Player.Heal(healFraction);
            Fx.Ring(gm.Player.transform.position + Vector3.up * 0.1f, 2.5f, new Color(0.3f, 1f, 0.4f, 0.5f), 0.4f);
        }

        public void UseRage()
        {
            if (!gm.Playing || !RageReady) return;
            RageT = rageCooldown;
            RageActiveT = rageDuration;
            DailyQuests.Add(QuestType.Skills, 1);
            Sfx.Play(Snd.SkillRage);
            gm.stats.rageMult = rageMult;
            gm.Player.SetRageTint(true);
            Fx.Ring(gm.Player.transform.position + Vector3.up * 0.1f, 3.5f, new Color(1f, 0.2f, 0.1f, 0.5f), 0.3f);
        }
    }

    /// <summary>Basit genişleyen halka efekti.</summary>
    public class Fx : MonoBehaviour
    {
        float t, duration, radius;

        public static void Ring(Vector3 pos, float radius, Color color, float duration)
        {
            var go = Mats.Prim(PrimitiveType.Cylinder, color, null, pos, new Vector3(0.1f, 0.02f, 0.1f), true);
            var fx = go.AddComponent<Fx>();
            fx.radius = radius;
            fx.duration = duration;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float d = Mathf.Lerp(0.2f, radius * 2f, 1f - (1f - k) * (1f - k));
            transform.localScale = new Vector3(d, 0.02f, d);
            if (t >= duration + 0.1f) Destroy(gameObject);
        }
    }
}
