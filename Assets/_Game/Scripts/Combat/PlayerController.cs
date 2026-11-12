using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace StratejiGame
{
    /// <summary>Ana karakter: yalnızca sağa-sola hareket eder ve otomatik ateş eder.</summary>
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Ekranın tamamı boyunca kaydırınca karakterin gideceği mesafe")]
        public float dragSensitivity = 14f;
        [Tooltip("Bırakılınca en yakın şeride kayma hızı")]
        public float laneSnapSpeed = 14f;
        int targetLane = 1;

        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public float X => transform.position.x;
        public float Z => transform.position.z;

        GameManager gm;
        float fireT;
        bool dragging;
        float lastPointerX;
        TextMesh countLabel;
        Renderer[] rends;
        float flashT;
        MaterialPropertyBlock mpb;
        HeroAnimator anim;
        static readonly List<RaycastResult> uiHits = new();
        static readonly Color BodyColor = new Color(0.25f, 0.45f, 0.95f);

        public void Init(float maxHp)
        {
            gm = GameManager.I;
            MaxHp = maxHp;
            Hp = maxHp;
            transform.position = Vector3.zero;

            // Şövalye komutan: kodla üretilen düşük poligonlu model (gövde + omuzdan dönen silah kolu)
            var model = new GameObject("Model").transform;
            model.SetParent(transform, false);
            var bodyT = CharacterDesigns.MakeVisual("Body", CharacterDesigns.HeroBody, model).transform;
            var armPivot = new GameObject("GunArmPivot").transform;
            armPivot.SetParent(bodyT, false);
            armPivot.localPosition = CharacterDesigns.HeroShoulder;
            CharacterDesigns.MakeVisual("GunArm", CharacterDesigns.HeroArm, armPivot);
            anim = model.gameObject.AddComponent<HeroAnimator>();
            anim.body = bodyT;
            anim.arm = armPivot;
            rends = GetComponentsInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();

            countLabel = Mats.Label(transform, new Vector3(0f, 3.4f, 0f), "0", 64, Color.white, 0.09f);
            countLabel.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        }

public void SetMaxHp(float maxHp)
        {
            MaxHp = maxHp;
            Hp = maxHp;
        }


        void Update()
        {
            if (gm == null || !gm.Playing) return;
            HandleInput();
            HandleFire();

            countLabel.text = gm.Army.Count.ToString();

            if (flashT > 0f)
            {
                flashT -= Time.deltaTime;
                if (flashT <= 0f) SetTint(null);
            }
        }

void HandleInput()
        {
            // Karakter ileri-geri gitmez; sadece 3 şerit arasında sağa-sola kayar.
            // Sürüklerken serbest hareket eder, bırakınca en yakın şeride oturur.
            float x = transform.position.x;
            var p = Pointer.current;
            if (p != null)
            {
                Vector2 pos = p.position.ReadValue();
                if (p.press.wasPressedThisFrame)
                {
                    dragging = !IsOverUi(pos);
                    lastPointerX = pos.x;
                }
                if (dragging && p.press.isPressed)
                {
                    x += (pos.x - lastPointerX) / Screen.width * dragSensitivity;
                    lastPointerX = pos.x;
                    targetLane = gm.LaneOf(x);
                }
                if (p.press.wasReleasedThisFrame) dragging = false;
            }

#if UNITY_EDITOR
            // Yalnız editör: klavyeyle şerit değiştirme (geliştirici kolaylığı, yayın build'ine girmez)
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) targetLane = Mathf.Max(0, targetLane - 1);
                if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) targetLane = Mathf.Min(GameManager.LaneCount - 1, targetLane + 1);
            }
#endif

            if (!dragging) x = Mathf.MoveTowards(x, gm.LaneCenter(targetLane), laneSnapSpeed * Time.deltaTime);

            float limit = gm.corridorHalfWidth - 0.6f;
            transform.position = new Vector3(Mathf.Clamp(x, -limit, limit), 0f, 0f);
        }

        static bool IsOverUi(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            uiHits.Clear();
            es.RaycastAll(data, uiHits);
            return uiHits.Count > 0;
        }

        void HandleFire()
        {
            fireT -= Time.deltaTime;
            if (fireT > 0f) return;
            var s = gm.stats;
            fireT = s.Interval(s.playerFireInterval);
            if (anim != null) anim.Fire();
            Sfx.Play(Snd.PlayerShot);
            var muzzle = transform.position + CharacterDesigns.HeroMuzzle;
            int n = 1 + s.extraBullets;
            for (int i = 0; i < n; i++)
            {
                float off = (i - (n - 1) * 0.5f) * 0.35f;
                gm.Enemies.FireBullet(muzzle + new Vector3(off, 0f, 0f), s.Damage(s.playerDamage), s.pierce, true);
            }
        }

        public void TakeDamage(float dmg)
        {
            if (!gm.Playing) return;
            Hp = Mathf.Max(0f, Hp - dmg);
            flashT = 0.12f;
            SetTint(Color.red);
            Sfx.Play(Snd.PlayerHurt);
            gm.Shake(0.25f, 0.15f);
            Haptics.Vibrate(dmg >= MaxHp * 0.15f ? 70 : 30);
            if (Hp <= 0f) gm.Lose();
        }

        public void Heal(float fraction)
        {
            Hp = Mathf.Min(MaxHp, Hp + MaxHp * fraction);
            flashT = 0.3f;
            SetTint(Color.green);
        }

        public void SetRageTint(bool on) => SetTint(on ? new Color(1f, 0.35f, 0.2f) : (Color?)null);

        void SetTint(Color? c)
        {
            foreach (var r in rends)
            {
                if (r == null || r.GetComponent<TextMesh>() != null) continue;
                if (c.HasValue)
                {
                    mpb.SetColor("_BaseColor", c.Value);
                    r.SetPropertyBlock(mpb);
                }
                else r.SetPropertyBlock(null);
            }
        }
    }
}
