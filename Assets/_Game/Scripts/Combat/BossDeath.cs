using System;
using System.Collections.Generic;
using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Boss ölüm sahnesi (her diyarda farklı). Boss ölünce: kısa ağır çekim + kamera sarsıntısı, sahadaki düşmanlar ve
    /// düşman mermileri temizlenir, ardından diyara özel sahne oynar; bitince onDone (zafer ekranı / Sonsuz Savunma devamı).
    ///  - Su Altı (Yengeç Kral): titrer, sırtüstü döner, köpükler içinde suya batar.
    ///  - Yanardağ (Magma Golemi): lavı söner, griye döner; taş parçalarına ayrılıp yıkılır (parçalar Blender'da bölündü).
    ///  - Bataklık (Kök Kralı): kuruyup kararır, kıvranarak toprağa çekilir.
    ///  - Kar (Yeti): sendeler, sırtüstü devrilir; yere çarpınca kar / buz patlaması.
    ///  - Gökyüzü (Fırtına Lordu): şimşeklerle aşırı yüklenip yükselir, ışık patlamasıyla dağılır; fırtına bulutu söner.
    ///  - Çöl (Kumların Hakimi): kuma dönüşür, ufalanıp çöker (parçalar Blender'da bölündü).
    /// Tüm zamanlamalar gerçek zamanla çalışır (ağır çekimden etkilenmez).
    /// </summary>
    public class BossDeath : MonoBehaviour
    {
        public const float Duration = 2.8f;
#if UNITY_EDITOR
        /// <summary>Yalnız editör (test): doluysa sahne boyunca bu dosya önekiyle 6 ekran görüntüsü alınır.</summary>
        public static string DebugShotPrefix;
        static readonly float[] DebugShotTimes = { 0.05f, 0.7f, 1.5f, 2.5f, 3.05f, 3.3f };
        int debugShot;
#endif
        const float SlowMoTime = 0.6f, SlowMoScale = 0.3f;
        // kamera yaklaşması: sahne başında boss'a doğru yaklaşır, sahne bitince eski haline döner, sonra zafer ekranı
        const float CamInTime = 0.6f, CamOutTime = 0.55f;
        const float CamCloser = 0.55f;   // normal kameradan boss'a olan mesafenin bu oranı kadar yaklaş
        const float CamLookAt = 0.85f;   // bakışı boss'a bu oranda çevir
        Vector3 camTarget;

        GameManager gm;
        Transform model, cloud;
        WorldTheme theme;
        Action onDone;
        float t, tAll;   // t: sahne zamanı (Duration'da durur), tAll: kamera dönüşü dahil toplam
        bool sceneEnded;
        bool done, swapped, impact;
        Vector3 modelPos0, modelScale0, rootPos0;
        Quaternion modelRot0;
        Renderer[] rends;
        MaterialPropertyBlock mpb;
        float fxT;
        float height = 3f;

        // parçalanma (Golem, Firavun)
        class Piece { public Transform tr; public Vector3 vel, spin; public bool resting; }
        readonly List<Piece> pieces = new List<Piece>();
        Transform broken;

        public static void Play(Boss boss, Action onDone)
        {
            var d = boss.gameObject.AddComponent<BossDeath>();
            d.onDone = onDone;
            d.Begin();
        }

        void Begin()
        {
            gm = GameManager.I;
            theme = gm != null ? gm.Theme : WorldTheme.Underwater;
            model = transform.Find("Model");
            cloud = transform.Find("StormCloud");
            if (cloud != null) foreach (var mb in cloud.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
            rootPos0 = transform.position;
            if (model != null)
            {
                modelPos0 = model.localPosition;
                modelRot0 = model.localRotation;
                modelScale0 = model.localScale;
                // boşta hareket bileşenleri dursun (nefes alma, süzülme)
                foreach (var mb in model.GetComponentsInChildren<MonoBehaviour>(true)) if (mb != this) mb.enabled = false;
                rends = model.GetComponentsInChildren<Renderer>(true);
                var b = new Bounds(transform.position, Vector3.zero);
                foreach (var r in rends) b.Encapsulate(r.bounds);
                height = Mathf.Max(1f, b.size.y);
            }
            mpb = new MaterialPropertyBlock();
            camTarget = transform.position + Vector3.up * height * 0.45f;

            // sahayı temizle: kalan düşmanlar ölür, düşman mermileri kaybolur
            if (gm != null && gm.Enemies != null)
            {
                foreach (var e in gm.Enemies.Active.ToArray())
                    if (e != null && !e.dead && e.kind != EnemyKind.Boss) e.Kill(false);
                foreach (var p in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                    if (p.gameObject.activeSelf) gm.Enemies.ReleaseProjectile(p);
                Shake(0.7f, 0.6f);
            }
            Sfx.Play(Snd.BossDeath);
            Haptics.Vibrate(120);
            if (gm != null && gm.Hud != null) gm.Hud.Toast(Loc.T("BOSS YENİLDİ!", "BOSS DEFEATED!"), true);   // "BOSS GELİYOR!" yazısı kalmasın
            Time.timeScale = SlowMoScale;
        }

        void OnDestroy()
        {
            if (!done) Time.timeScale = 1f;
            if (gm != null) gm.SetCameraFocus(camTarget, CamCloser, CamLookAt, 0f);   // yarıda kesilirse kamera normale dönsün
        }

        void Update()
        {
            if (done) return;
            // gerçek zaman (ağır çekimden etkilenmez); Time.deltaTime/timeScale: editörde sabit kare adımıyla (captureDeltaTime) da doğru akar
            float dt = Mathf.Min(Time.timeScale > 0.001f ? Time.deltaTime / Time.timeScale : Time.unscaledDeltaTime, 0.05f);
            tAll += dt;
            t = Mathf.Min(tAll, Duration);
            // ağır çekim: ilk anlar yavaş, sonra normale döner
            Time.timeScale = t < SlowMoTime ? SlowMoScale : Mathf.MoveTowards(Time.timeScale, 1f, dt * 2.5f);
            fxT -= dt;

            float camK = tAll < Duration ? Ease(K(tAll, 0f, CamInTime)) : 1f - Ease(K(tAll, Duration, Duration + CamOutTime));
            if (gm != null) gm.SetCameraFocus(camTarget, CamCloser, CamLookAt, camK);

            // sahne son karesini (t = Duration) bir kez oynar, kamera dönerken son haliyle bekler
            if (model != null && !sceneEnded)
            {
                switch (theme)
                {
                    case WorldTheme.Underwater: Crab(dt); break;
                    case WorldTheme.Volcano: Golem(dt); break;
                    case WorldTheme.Swamp: RootKing(dt); break;
                    case WorldTheme.Frost: Yeti(dt); break;
                    case WorldTheme.Sky: StormLord(dt); break;
                    case WorldTheme.Desert: Pharaoh(dt); break;
                }
            }
            if (t >= Duration) sceneEnded = true;
            UpdatePieces(dt);
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(DebugShotPrefix) && debugShot < DebugShotTimes.Length && tAll >= DebugShotTimes[debugShot])
                ScreenCapture.CaptureScreenshot(DebugShotPrefix + "_" + (debugShot++) + ".png");
#endif

            if (tAll >= Duration + CamOutTime)
            {
                done = true;
                if (gm != null) gm.SetCameraFocus(camTarget, CamCloser, CamLookAt, 0f);
                Time.timeScale = 1f;
                var cb = onDone;
                onDone = null;
                cb?.Invoke();
            }
        }

        // ------------------------------------------------------------------ yardımcılar
        /// <summary>Kamera boss'a yaklaştığı için sarsıntı daha büyük görünür: sahnedeki tüm sarsıntılar bu oranda hafifletilir.</summary>
        const float ShakeScale = 0.4f;
        void Shake(float amplitude, float duration) { if (gm != null) gm.Shake(amplitude * ShakeScale, duration); }

        static float Ease(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
        static float K(float t, float a, float b) => Mathf.Clamp01((t - a) / (b - a));

        Vector3 Feet => transform.position;

        void Tint(Color c, float k)
        {
            if (rends == null) return;
            foreach (var r in rends)
            {
                if (r == null) continue;
                var baseC = r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.white;
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", Color.Lerp(baseC, c, k));
                r.SetPropertyBlock(mpb);
            }
        }

        void Shudder(float amp)
        {
            model.localPosition = modelPos0 + new Vector3(Mathf.Sin(t * 61f) * amp, 0f, Mathf.Cos(t * 47f) * amp * 0.6f);
        }

        Vector3 Around(float r, float y = 0.3f) => Feet + new Vector3(UnityEngine.Random.Range(-r, r), y, UnityEngine.Random.Range(-r * 0.6f, r * 0.6f));

        // ------------------------------------------------------------------ Su Altı: Yengeç Kral
        void Crab(float dt)
        {
            if (t < 0.5f) Shudder(0.12f);
            else if (t < 1.3f)
            {
                float k = Ease(K(t, 0.5f, 1.3f));
                float jump = Mathf.Sin(k * Mathf.PI) * 1.4f;
                // ayak hizasından ters döner: dönerken gövde yere gömülmesin diye boyu kadar yükselir
                model.localPosition = modelPos0 + Vector3.up * (jump + height * 0.55f * k);
                model.localRotation = Quaternion.AngleAxis(180f * k, Vector3.forward) * modelRot0;
            }
            else
            {
                float k = Ease(K(t, 1.3f, Duration));
                model.localPosition = modelPos0 + Vector3.up * (height * 0.55f - (height * 1.4f) * k);
                model.localRotation = Quaternion.AngleAxis(180f + Mathf.Sin(t * 6f) * 6f * (1f - k), Vector3.forward) * modelRot0;
                if (fxT <= 0f) { fxT = 0.06f; Effects.Splash(Around(2.2f, 0.1f), UnityEngine.Random.Range(1f, 1.8f)); }
            }
            if (!impact && t > 1.3f) { impact = true; Shake(0.4f, 0.3f); Effects.Splash(Feet + Vector3.up * 0.2f, 3f); }
        }

        // ------------------------------------------------------------------ Yanardağ: Magma Golemi
        void Golem(float dt)
        {
            if (!swapped)
            {
                // lav söner: renk griye döner, titrer, kıvılcımlar
                Tint(new Color(0.35f, 0.33f, 0.32f), Ease(K(t, 0f, 0.8f)));
                Shudder(0.1f * K(t, 0f, 0.8f));
                if (fxT <= 0f) { fxT = 0.05f; Effects.Ember(Around(1.8f, UnityEngine.Random.Range(0.5f, height))); }
                if (t >= 0.85f) Shatter("BossDeath/MagmaGolem_Broken", 7f, 6f, 0.35f, new Color(0.35f, 0.33f, 0.32f));
            }
            else if (fxT <= 0f) { fxT = 0.12f; Effects.Ember(Around(2.5f, 0.2f)); }
        }

        // ------------------------------------------------------------------ Bataklık: Kök Kralı
        void RootKing(float dt)
        {
            float k = Ease(K(t, 0.15f, Duration - 0.2f));
            Tint(new Color(0.12f, 0.09f, 0.06f), Ease(K(t, 0f, 1.2f)));
            // kıvranarak çekilir: boyu kısalır, yana yayılır, toprağa gömülür
            model.localScale = new Vector3(modelScale0.x * (1f + 0.25f * k), modelScale0.y * (1f - 0.75f * k), modelScale0.z * (1f + 0.25f * k));
            model.localPosition = modelPos0 + Vector3.down * (height * 0.6f * k);
            model.localRotation = modelRot0 * Quaternion.Euler(Mathf.Sin(t * 9f) * 6f * (1f - k), Mathf.Sin(t * 3.3f) * 25f * k, Mathf.Cos(t * 7f) * 5f * (1f - k));
            if (!impact) { impact = true; Effects.RootEruption(new Vector3(Feet.x, 0f, Feet.z), gm.LaneWidth * 1.5f); }
            if (fxT <= 0f)
            {
                fxT = 0.08f;
                Effects.SporeDrip(Around(1.8f, UnityEngine.Random.Range(0.3f, height * (1f - k) + 0.3f)));
                if (UnityEngine.Random.value < 0.3f) Effects.ToxicMist(Around(2f, 0.2f));
            }
        }

        // ------------------------------------------------------------------ Kar: Yeti Savaş Şefi
        void Yeti(float dt)
        {
            float back;
            if (t < 0.35f) { back = -6f * Ease(K(t, 0f, 0.35f)); Shudder(0.08f); }   // öne sendeler
            else
            {
                // geriye (oyuncudan uzağa) hızlanarak devrilir, yere çarpınca hafif seker
                float k = K(t, 0.35f, 1.15f);
                back = -6f + 92f * k * k;
                if (k >= 1f)
                {
                    float b = K(t, 1.15f, 1.5f);
                    back = 86f + Mathf.Sin(b * Mathf.PI) * 5f;
                }
            }
            model.localRotation = Quaternion.AngleAxis(back, Vector3.right) * modelRot0;
            if (!impact && t >= 1.15f)
            {
                impact = true;
                Shake(0.8f, 0.45f);
                Haptics.Vibrate(90);
                var hit = Feet + new Vector3(0f, 0.2f, height * 0.6f);
                Effects.IceShatter(hit, 3f);
                Effects.IceEruption(new Vector3(Feet.x, 0f, Feet.z + height * 0.6f), gm.LaneWidth * 1.5f);
                for (int i = 0; i < 14; i++) Effects.FrostSparkle(hit + new Vector3(UnityEngine.Random.Range(-2.5f, 2.5f), UnityEngine.Random.Range(0f, 1.5f), UnityEngine.Random.Range(-1.5f, 1.5f)));
            }
            if (t > 1.9f)
            {
                // yattığı yerde buz altında kaybolur
                float k = Ease(K(t, 1.9f, Duration));
                model.localPosition = modelPos0 + Vector3.down * (1.5f * k);
                if (fxT <= 0f) { fxT = 0.07f; Effects.FrostSparkle(Around(2.5f, 0.3f) + Vector3.forward * height * 0.5f); }
            }
        }

        // ------------------------------------------------------------------ Gökyüzü: Fırtına Lordu
        void StormLord(float dt)
        {
            const float burst = 1.5f;
            if (t < burst)
            {
                float k = K(t, 0f, burst);
                model.localPosition = modelPos0 + Vector3.up * (3f * Ease(k)) + new Vector3(Mathf.Sin(t * 70f) * 0.06f, 0f, 0f);
                model.localScale = modelScale0 * (1f + 0.15f * k);
                // aşırı yüklenme: hızla yanıp söner
                Tint(Color.white, (Mathf.Sin(t * (20f + 40f * k)) > 0f ? 0.85f : 0.1f) * k);
                if (fxT <= 0f)
                {
                    fxT = Mathf.Lerp(0.15f, 0.04f, k);
                    var c = model.position + Vector3.up * height * 0.5f;
                    var sky = c + new Vector3(UnityEngine.Random.Range(-4f, 4f), 14f, UnityEngine.Random.Range(-2f, 4f));
                    Effects.Lightning(sky, c, 0.12f, new Color(0.55f, 0.8f, 1f), 0.15f, 0.8f);
                    Effects.Zap(c + UnityEngine.Random.insideUnitSphere * 1.2f, 1.2f);
                }
            }
            else
            {
                if (!impact)
                {
                    impact = true;
                    Shake(0.9f, 0.5f);
                    Haptics.Vibrate(120);
                    for (int i = 0; i < GameManager.LaneCount; i++) Effects.LightningStrike(new Vector3(gm.LaneCenter(i), 0f, Feet.z), gm.LaneWidth);
                    var c = model.position + Vector3.up * height * 0.5f;
                    for (int i = 0; i < 10; i++)
                    {
                        var dir = UnityEngine.Random.onUnitSphere;
                        Effects.Lightning(c, c + dir * UnityEngine.Random.Range(3f, 6f), 0.14f, new Color(0.7f, 0.9f, 1f), 0.25f, 0.7f);
                    }
                }
                float k = Ease(K(t, burst, burst + 0.35f));
                model.localScale = modelScale0 * (1.15f * (1f - k));
                Tint(Color.white, 1f);
                if (cloud != null) cloud.localScale = Vector3.Lerp(cloud.localScale, Vector3.zero, dt * 3f);
                if (fxT <= 0f) { fxT = 0.05f; Effects.SkySpark(Around(3f, UnityEngine.Random.Range(0.5f, 4f))); }
            }
        }

        // ------------------------------------------------------------------ Çöl: Kumların Hakimi
        void Pharaoh(float dt)
        {
            if (!swapped)
            {
                Tint(new Color(0.86f, 0.7f, 0.44f), Ease(K(t, 0f, 0.9f)));
                Shudder(0.06f);
                if (fxT <= 0f) { fxT = 0.05f; Effects.SandPuff(Around(2f, UnityEngine.Random.Range(0.3f, height)), 0.8f); }
                if (t >= 0.95f) Shatter("BossDeath/ScorpionPharaoh_Broken", 2.2f, 1.2f, 0.1f, new Color(0.86f, 0.7f, 0.44f));
            }
            else if (fxT <= 0f) { fxT = 0.06f; Effects.SandPuff(Around(2.5f, 0.2f), 1.2f); }
        }

        // ------------------------------------------------------------------ parçalanma
        /// <summary>Modeli Blender'da bölünmüş parçalarıyla değiştirir ve dağıtır.</summary>
        void Shatter(string resource, float outSpeed, float upSpeed, float bounce, Color tint)
        {
            swapped = true;
            var prefab = Resources.Load<GameObject>(resource);
            if (prefab == null) { model.gameObject.SetActive(false); return; }
            broken = Instantiate(prefab, transform).transform;
            broken.localPosition = model.localPosition;
            broken.localRotation = model.localRotation;
            broken.localScale = model.localScale;
            model.gameObject.SetActive(false);
            var center = model.position + Vector3.up * height * 0.45f;
            var block = new MaterialPropertyBlock();
            foreach (Transform p in broken)
            {
                var r = p.GetComponent<Renderer>();
                if (r != null)
                {
                    var baseC = r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.white;
                    block.SetColor("_BaseColor", Color.Lerp(baseC, tint, 0.85f));
                    r.SetPropertyBlock(block);
                }
                var dir = p.position - center;
                dir.y = Mathf.Max(0f, dir.y);
                dir = dir.sqrMagnitude > 0.001f ? dir.normalized : UnityEngine.Random.onUnitSphere;
                pieces.Add(new Piece
                {
                    tr = p,
                    vel = dir * outSpeed * UnityEngine.Random.Range(0.6f, 1.2f) + Vector3.up * upSpeed * UnityEngine.Random.Range(0.5f, 1.1f),
                    spin = UnityEngine.Random.insideUnitSphere * 400f,
                });
            }
            pieceBounce = bounce;
            Shake(0.7f, 0.4f);
            Haptics.Vibrate(90);
        }

        float pieceBounce;

        void UpdatePieces(float dt)
        {
            if (pieces.Count == 0) return;
            float sinkStart = Duration - 0.8f;
            foreach (var p in pieces)
            {
                if (p.tr == null) continue;
                var pos = p.tr.position;
                if (!p.resting)
                {
                    p.vel.y -= 22f * dt;
                    pos += p.vel * dt;
                    p.tr.rotation = Quaternion.Euler(p.spin * dt) * p.tr.rotation;
                    if (pos.y < 0.15f && p.vel.y < 0f)
                    {
                        pos.y = 0.15f;
                        p.vel = new Vector3(p.vel.x * 0.5f, -p.vel.y * pieceBounce, p.vel.z * 0.5f);
                        p.spin *= 0.5f;
                        if (p.vel.y < 0.6f) p.resting = true;
                    }
                }
                // son anda parçalar yere gömülür
                if (t > sinkStart) pos.y -= dt * 1.6f;
                p.tr.position = pos;
            }
        }
    }
}
