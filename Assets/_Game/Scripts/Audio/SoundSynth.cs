using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Oyunun tüm sesleri kodla sentezlenir (ses dosyası yok): gürültü, sinüs/kare/testere dalgaları,
    /// frekans kaydırma, basit filtreler ve zarf eğrileri. Klipler bir kez üretilip önbelleğe alınır.
    /// Örnekleme hızı 22050 Hz (mobil bellek için yeterli).
    /// </summary>
    public static class SoundSynth
    {
        public const int SR = 22050;
        const float TwoPi = Mathf.PI * 2f;

        // ─────────────────────────────────────────────────────
        // YARDIMCILAR
        // ─────────────────────────────────────────────────────
        static System.Random rnd = new System.Random(1);
        static float Noise() => (float)rnd.NextDouble() * 2f - 1f;
        static float[] Buf(float seconds) => new float[Mathf.CeilToInt(seconds * SR)];
        static float LpA(float cutoff) => 1f - Mathf.Exp(-TwoPi * cutoff / SR);

        /// <summary>Üstel sönüm zarfı (attack saniye, decay: saniyedeki sönüm katsayısı).</summary>
        static float Env(float t, float attack, float decay)
        {
            float a = attack > 0f ? Mathf.Clamp01(t / attack) : 1f;
            return a * Mathf.Exp(-decay * Mathf.Max(0f, t - attack));
        }

        /// <summary>Kaydırmalı sinüs ekler: frekans f0'dan f1'e üstel geçer.</summary>
        static void AddSweep(float[] b, float start, float dur, float f0, float f1, float amp, float attack, float decay, float harmonics = 0f)
        {
            int s = (int)(start * SR), n = Mathf.Min(b.Length - s, (int)(dur * SR));
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, k = t / dur;
                float f = f0 * Mathf.Pow(f1 / f0, k);
                ph += TwoPi * f / SR;
                float v = Mathf.Sin(ph) + harmonics * 0.5f * Mathf.Sin(ph * 2f) + harmonics * 0.25f * Mathf.Sin(ph * 3f);
                b[s + i] += v * amp * Env(t, attack, decay);
            }
        }

        /// <summary>Filtrelenmiş gürültü ekler (lpCut: alçak geçiren; hpCut &gt; 0 ise yüksek geçiren de).</summary>
        static void AddNoise(float[] b, float start, float dur, float amp, float attack, float decay, float lpCut, float hpCut = 0f, float lpCutEnd = -1f)
        {
            int s = (int)(start * SR), n = Mathf.Min(b.Length - s, (int)(dur * SR));
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float cut = lpCutEnd > 0f ? Mathf.Lerp(lpCut, lpCutEnd, t / dur) : lpCut;
                float a = LpA(cut);
                lp += a * (Noise() - lp);
                float v = lp;
                if (hpCut > 0f) { lp2 += LpA(hpCut) * (lp - lp2); v = lp - lp2; }
                b[s + i] += v * amp * Env(t, attack, decay);
            }
        }

        /// <summary>Kare dalga (sert, "oyunsu" tını).</summary>
        static void AddSquare(float[] b, float start, float dur, float f0, float f1, float amp, float attack, float decay)
        {
            int s = (int)(start * SR), n = Mathf.Min(b.Length - s, (int)(dur * SR));
            float ph = 0f, lp = 0f, a = LpA(2500f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float f = f0 * Mathf.Pow(f1 / f0, t / dur);
                ph += f / SR;
                float v = (ph % 1f) < 0.5f ? 1f : -1f;
                lp += a * (v - lp);
                b[s + i] += lp * amp * Env(t, attack, decay);
            }
        }

        /// <summary>Su altı kabarcık "blup" sesi: yukarı kayan kısa sinüs.</summary>
        static void AddBubble(float[] b, float start, float size, float amp)
        {
            float f0 = 260f / size, dur = 0.05f + 0.07f * size;
            AddSweep(b, start, dur, f0, f0 * 2.4f, amp, 0.004f, 30f / (0.5f + size));
        }

        /// <summary>Çan / zil tınısı (akor ve arpej için).</summary>
        static void AddBell(float[] b, float start, float freq, float amp, float decay)
        {
            int s = (int)(start * SR), n = Mathf.Min(b.Length - s, (int)(1.2f * SR));
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float v = Mathf.Sin(TwoPi * freq * t) + 0.35f * Mathf.Sin(TwoPi * freq * 2.01f * t) + 0.12f * Mathf.Sin(TwoPi * freq * 3.98f * t);
                b[s + i] += v * amp * Env(t, 0.003f, decay);
            }
        }

        static float[] Distort(float[] b, float drive)
        {
            for (int i = 0; i < b.Length; i++) b[i] = (float)System.Math.Tanh(b[i] * drive) / (float)System.Math.Tanh(drive);
            return b;
        }

        static AudioClip Make(string name, float[] data, float peak = 0.9f)
        {
            float max = 0.0001f;
            for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
            float g = peak / max;
            // Sonda tıklama olmasın diye kısa kapanış
            int fade = Mathf.Min(data.Length, SR / 100);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= g;
                int left = data.Length - 1 - i;
                if (left < fade) data[i] *= left / (float)fade;
            }
            var clip = AudioClip.Create(name, data.Length, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ─────────────────────────────────────────────────────
        // SESLER
        // ─────────────────────────────────────────────────────

        /// <summary>Komutan tüfeği: dolgun "pew" + gövde darbesi.</summary>
        public static AudioClip PlayerShot()
        {
            rnd = new System.Random(11);
            var b = Buf(0.22f);
            AddSweep(b, 0f, 0.16f, 1400f, 240f, 0.55f, 0.001f, 26f, 0.4f);
            AddNoise(b, 0f, 0.1f, 0.9f, 0.0005f, 45f, 5000f, 400f);
            AddSweep(b, 0f, 0.14f, 150f, 55f, 0.8f, 0.002f, 22f);
            return Make("sfx_player_shot", Distort(b, 1.6f), 0.8f);
        }

        /// <summary>Asker tüfeği: kısa, hafif "pıt".</summary>
        public static AudioClip SoldierShot()
        {
            rnd = new System.Random(12);
            var b = Buf(0.09f);
            AddSweep(b, 0f, 0.07f, 1900f, 600f, 0.45f, 0.0005f, 55f);
            AddNoise(b, 0f, 0.05f, 0.7f, 0.0005f, 80f, 7000f, 1200f);
            return Make("sfx_soldier_shot", b, 0.7f);
        }

        /// <summary>Mermi isabeti: kabuğa çarpan kuru "tık".</summary>
        public static AudioClip Hit()
        {
            rnd = new System.Random(13);
            var b = Buf(0.07f);
            AddNoise(b, 0f, 0.06f, 1f, 0.0003f, 90f, 6000f, 1500f);
            AddSweep(b, 0f, 0.05f, 900f, 500f, 0.4f, 0.0005f, 70f);
            return Make("sfx_hit", b, 0.7f);
        }

        /// <summary>Yengeç ölümü: kabuk çatırtısı + kabarcık patlaması.</summary>
        public static AudioClip CrabDeath(float size)
        {
            rnd = new System.Random(14 + (int)(size * 10f));
            var b = Buf(0.45f + 0.2f * size);
            AddNoise(b, 0f, 0.12f, 1f, 0.001f, 30f / size, 4500f / size, 300f);
            AddNoise(b, 0.02f, 0.08f, 0.6f, 0.001f, 50f, 7000f, 2000f);
            AddSweep(b, 0f, 0.18f * size, 220f / size, 70f / size, 0.7f, 0.002f, 16f / size);
            for (int i = 0; i < 4 + (int)(size * 3f); i++)
                AddBubble(b, 0.05f + (float)rnd.NextDouble() * 0.3f * size, 0.5f + (float)rnd.NextDouble() * size * 0.6f, 0.35f);
            return Make("sfx_crab_death_" + size.ToString("0.0"), Distort(b, 1.3f), 0.85f);
        }

        /// <summary>Asker düştü: tok zırh darbesi + kabarcık.</summary>
        public static AudioClip SoldierDeath()
        {
            rnd = new System.Random(15);
            var b = Buf(0.35f);
            AddSweep(b, 0f, 0.2f, 180f, 80f, 0.8f, 0.002f, 18f, 0.3f);
            AddNoise(b, 0f, 0.1f, 0.6f, 0.001f, 35f, 2500f, 200f);
            AddSweep(b, 0f, 0.12f, 1300f, 1100f, 0.2f, 0.001f, 30f, 0.8f);
            AddBubble(b, 0.08f, 0.8f, 0.3f);
            AddBubble(b, 0.16f, 0.6f, 0.25f);
            return Make("sfx_soldier_death", b, 0.75f);
        }

        /// <summary>Asker katıldı (buff): yükselen üç notalı parlak arpej.</summary>
        public static AudioClip GateSoldiers()
        {
            var b = Buf(0.9f);
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < notes.Length; i++) AddBell(b, i * 0.07f, notes[i], 0.5f, 5f);
            rnd = new System.Random(16);
            for (int i = 0; i < 5; i++) AddBubble(b, 0.05f + i * 0.08f, 0.35f + (float)rnd.NextDouble() * 0.2f, 0.12f);
            return Make("sfx_gate_soldiers", b, 0.75f);
        }

        /// <summary>Silah güçlendirmesi: yükselen enerji süpürmesi + parlak vuruş.</summary>
        public static AudioClip GatePower()
        {
            rnd = new System.Random(17);
            var b = Buf(0.8f);
            AddSquare(b, 0f, 0.35f, 220f, 880f, 0.35f, 0.01f, 3f);
            AddSweep(b, 0f, 0.35f, 330f, 1320f, 0.35f, 0.01f, 3f, 0.5f);
            AddBell(b, 0.33f, 1318.5f, 0.45f, 6f);
            AddBell(b, 0.33f, 1975.5f, 0.2f, 7f);
            AddNoise(b, 0.3f, 0.3f, 0.25f, 0.002f, 10f, 9000f, 3000f);
            return Make("sfx_gate_power", b, 0.75f);
        }

        /// <summary>Zararlı kapı: inen, kırık iki nota.</summary>
        public static AudioClip GateBad()
        {
            var b = Buf(0.7f);
            AddSquare(b, 0f, 0.22f, 330f, 300f, 0.5f, 0.005f, 6f);
            AddSquare(b, 0.2f, 0.45f, 247f, 175f, 0.5f, 0.005f, 5f);
            AddSweep(b, 0.2f, 0.45f, 123f, 88f, 0.4f, 0.005f, 5f);
            return Make("sfx_gate_bad", b, 0.7f);
        }

        /// <summary>Zıpkın fırlatma: hava/su yaran "vuuş".</summary>
        public static AudioClip HarpoonThrow()
        {
            rnd = new System.Random(18);
            var b = Buf(0.45f);
            AddNoise(b, 0f, 0.45f, 1f, 0.15f, 9f, 900f, 250f, 2500f);
            AddSweep(b, 0f, 0.3f, 300f, 160f, 0.25f, 0.05f, 10f);
            return Make("sfx_harpoon_throw", b, 0.6f);
        }

        /// <summary>Su sıçraması: köpüklü gürültü + kabarcıklar.</summary>
        public static AudioClip Splash()
        {
            rnd = new System.Random(19);
            var b = Buf(0.6f);
            AddNoise(b, 0f, 0.5f, 1f, 0.005f, 8f, 3500f, 300f, 900f);
            for (int i = 0; i < 7; i++) AddBubble(b, 0.03f + (float)rnd.NextDouble() * 0.35f, 0.4f + (float)rnd.NextDouble() * 0.7f, 0.3f);
            return Make("sfx_splash", b, 0.7f);
        }

        /// <summary>Komutan hasar aldı: tok darbe + kısa uyarı tonu.</summary>
        public static AudioClip PlayerHurt()
        {
            rnd = new System.Random(20);
            var b = Buf(0.35f);
            AddSweep(b, 0f, 0.25f, 140f, 60f, 1f, 0.002f, 12f, 0.5f);
            AddNoise(b, 0f, 0.12f, 0.6f, 0.001f, 30f, 2000f, 150f);
            AddSquare(b, 0.02f, 0.18f, 220f, 180f, 0.25f, 0.005f, 12f);
            return Make("sfx_player_hurt", Distort(b, 1.8f), 0.85f);
        }

        /// <summary>Boss kükremesi: alçak, titreşimli testere + gürültü.</summary>
        public static AudioClip BossRoar()
        {
            rnd = new System.Random(21);
            var b = Buf(1.8f);
            float ph = 0f, lp = 0f, a = LpA(700f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float f = 70f + 25f * Mathf.Sin(t * 1.7f) + 6f * Mathf.Sin(t * 37f) + Noise() * 4f;
                ph += f / SR;
                float saw = (ph % 1f) * 2f - 1f;
                lp += a * (saw - lp);
                b[i] += lp * Env(t, 0.25f, 1.2f);
            }
            AddNoise(b, 0f, 1.6f, 0.6f, 0.3f, 1.4f, 600f, 80f);
            for (int i = 0; i < 10; i++) AddBubble(b, 0.3f + i * 0.12f, 1.2f + (float)rnd.NextDouble(), 0.2f);
            return Make("sfx_boss_roar", Distort(b, 2.2f), 0.9f);
        }

        /// <summary>Tsunami uyarısı: yükselen uğultu.</summary>
        public static AudioClip TsunamiWarn()
        {
            rnd = new System.Random(22);
            var b = Buf(1.4f);
            AddNoise(b, 0f, 1.4f, 1f, 1.1f, 0.5f, 250f, 40f, 1200f);
            AddSweep(b, 0f, 1.4f, 55f, 110f, 0.5f, 1f, 0.5f, 0.6f);
            return Make("sfx_tsunami_warn", b, 0.7f);
        }

        /// <summary>Tsunami çarpması: büyük dalga gürlemesi.</summary>
        public static AudioClip TsunamiHit()
        {
            rnd = new System.Random(23);
            var b = Buf(1.5f);
            AddNoise(b, 0f, 1.5f, 1f, 0.02f, 2.2f, 1800f, 60f, 400f);
            AddSweep(b, 0f, 0.6f, 90f, 40f, 0.8f, 0.01f, 5f);
            for (int i = 0; i < 12; i++) AddBubble(b, 0.2f + (float)rnd.NextDouble(), 0.5f + (float)rnd.NextDouble(), 0.2f);
            return Make("sfx_tsunami_hit", Distort(b, 1.5f), 0.9f);
        }

        /// <summary>Boss çağırma: kabarcıklı gurgul.</summary>
        public static AudioClip Summon()
        {
            rnd = new System.Random(24);
            var b = Buf(0.8f);
            AddNoise(b, 0f, 0.7f, 0.5f, 0.1f, 4f, 600f, 80f);
            for (int i = 0; i < 14; i++) AddBubble(b, (float)rnd.NextDouble() * 0.6f, 0.6f + (float)rnd.NextDouble() * 1.2f, 0.35f);
            return Make("sfx_summon", b, 0.7f);
        }

        /// <summary>Alan hasarı: derin su altı patlaması.</summary>
        public static AudioClip SkillAoe()
        {
            rnd = new System.Random(25);
            var b = Buf(1.4f);
            AddSweep(b, 0f, 0.8f, 120f, 30f, 1f, 0.005f, 4f, 0.6f);
            AddNoise(b, 0f, 1.2f, 1f, 0.005f, 3f, 2500f, 40f, 300f);
            for (int i = 0; i < 16; i++) AddBubble(b, 0.15f + (float)rnd.NextDouble() * 0.9f, 0.4f + (float)rnd.NextDouble() * 1.2f, 0.25f);
            return Make("sfx_skill_aoe", Distort(b, 2f), 0.95f);
        }

        /// <summary>Can yenileme: parıldayan yükselen ışıltı.</summary>
        public static AudioClip SkillHeal()
        {
            rnd = new System.Random(26);
            var b = Buf(1.2f);
            float[] notes = { 587.33f, 739.99f, 880f, 1174.66f, 1479.98f };
            for (int i = 0; i < notes.Length; i++) AddBell(b, i * 0.09f, notes[i], 0.4f, 3.5f);
            AddNoise(b, 0f, 0.9f, 0.15f, 0.2f, 3f, 10000f, 5000f);
            return Make("sfx_skill_heal", b, 0.7f);
        }

        /// <summary>Öfke: hırıltılı yükselen güç sesi.</summary>
        public static AudioClip SkillRage()
        {
            rnd = new System.Random(27);
            var b = Buf(1.1f);
            AddSquare(b, 0f, 0.8f, 90f, 260f, 0.6f, 0.05f, 2.5f);
            AddSweep(b, 0f, 0.8f, 180f, 520f, 0.4f, 0.05f, 2.5f, 0.7f);
            AddNoise(b, 0f, 0.9f, 0.5f, 0.05f, 3f, 1500f, 200f);
            AddBell(b, 0.7f, 1046.5f, 0.3f, 6f);
            return Make("sfx_skill_rage", Distort(b, 2.5f), 0.85f);
        }

        /// <summary>Arayüz dokunuşu: yumuşak "pop".</summary>
        public static AudioClip Click()
        {
            var b = Buf(0.08f);
            AddSweep(b, 0f, 0.07f, 700f, 1100f, 0.8f, 0.001f, 60f);
            return Make("sfx_click", b, 0.55f);
        }

        /// <summary>Yükseltme satın alma: altın şıngırtısı + yükselen parlak süpürme + kısa majör çan.</summary>
        public static AudioClip Purchase()
        {
            var b = Buf(0.7f);
            AddTinkle(b, 0f, 2093f, 0.35f);
            AddTinkle(b, 0.045f, 2637f, 0.3f);
            AddSweep(b, 0.02f, 0.28f, 420f, 1250f, 0.35f, 0.01f, 9f, 0.4f);
            AddBell(b, 0.16f, 1046.5f, 0.32f, 6f);
            AddBell(b, 0.16f, 1318.5f, 0.24f, 6f);
            AddBell(b, 0.16f, 1567.98f, 0.22f, 6f);
            return Make("sfx_purchase", b, 0.75f);
        }

        /// <summary>Yetersiz altın: iki kısa, alçak vızıltı.</summary>
        public static AudioClip Denied()
        {
            var b = Buf(0.3f);
            AddSquare(b, 0f, 0.09f, 180f, 160f, 0.5f, 0.004f, 12f);
            AddSquare(b, 0.12f, 0.12f, 150f, 130f, 0.5f, 0.004f, 10f);
            return Make("sfx_denied", b, 0.55f);
        }

        /// <summary>Zafer: majör arpej + çan.</summary>
        public static AudioClip Win()
        {
            var b = Buf(1.8f);
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
            for (int i = 0; i < notes.Length; i++) AddBell(b, i * 0.11f, notes[i], 0.45f, 3f);
            AddBell(b, 0.62f, 1046.5f, 0.3f, 1.8f);
            AddBell(b, 0.62f, 1318.5f, 0.25f, 1.8f);
            AddBell(b, 0.62f, 1567.98f, 0.25f, 1.8f);
            return Make("sfx_win", b, 0.8f);
        }

        /// <summary>Yenilgi: inen minör notalar.</summary>
        public static AudioClip Lose()
        {
            var b = Buf(1.8f);
            float[] notes = { 440f, 392f, 349.23f, 293.66f };
            for (int i = 0; i < notes.Length; i++) AddBell(b, i * 0.22f, notes[i], 0.5f, 2.5f);
            AddSweep(b, 0.66f, 1.1f, 146.83f, 110f, 0.4f, 0.02f, 2f, 0.5f);
            return Make("sfx_lose", b, 0.75f);
        }

        // ─────────────────────────────────────────────────────
        // YANARDAĞ SESLERİ
        // ─────────────────────────────────────────────────────

        /// <summary>Cızırtı: lav / sıcak taş üstüne düşen parça (yüksek frekanslı titrek gürültü).</summary>
        static void AddSizzle(float[] b, float start, float dur, float amp)
        {
            int s0 = (int)(start * SR), n = Mathf.Min(b.Length - s0, (int)(dur * SR));
            float lp = 0f, hp = 0f, a1 = LpA(7000f), a2 = LpA(2500f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                lp += a1 * (Noise() - lp);
                hp += a2 * (lp - hp);
                float crackle = rnd.NextDouble() < 0.004 ? Noise() * 3f : 0f;
                b[s0 + i] += ((lp - hp) * (0.6f + 0.4f * Mathf.Sin(t * 90f)) + crackle) * amp * Env(t, 0.01f, 3f / dur);
            }
        }

        /// <summary>Kaya / magma yaratığı ölümü: taş kırılması + cızırtı + alçak gümbürtü (kabarcık yok).</summary>
        public static AudioClip RockDeath(float size)
        {
            rnd = new System.Random(31 + (int)(size * 10f));
            var b = Buf(0.5f + 0.25f * size);
            AddNoise(b, 0f, 0.14f, 1f, 0.001f, 28f / size, 3500f / size, 200f);
            for (int i = 0; i < 3 + (int)(size * 2f); i++)
                AddNoise(b, (float)rnd.NextDouble() * 0.15f * size, 0.05f, 0.5f, 0.001f, 60f, 6000f, 1500f);
            AddSweep(b, 0f, 0.25f * size, 160f / size, 45f / size, 0.8f, 0.002f, 12f / size);
            AddSizzle(b, 0.05f, 0.35f * size, 0.35f);
            return Make("sfx_rock_death_" + size.ToString("0.0"), Distort(b, 1.4f), 0.85f);
        }

        /// <summary>Lav sıçraması / cızırtı (balta isabeti).</summary>
        public static AudioClip LavaSplash()
        {
            rnd = new System.Random(32);
            var b = Buf(0.6f);
            AddNoise(b, 0f, 0.15f, 0.8f, 0.002f, 20f, 2500f, 150f);
            AddSizzle(b, 0.02f, 0.55f, 0.7f);
            return Make("sfx_lava_splash", b, 0.7f);
        }

        /// <summary>Lav fışkırması: derin patlama + püskürme + cızırtı.</summary>
        public static AudioClip Eruption()
        {
            rnd = new System.Random(33);
            var b = Buf(1.6f);
            AddSweep(b, 0f, 0.7f, 110f, 32f, 1f, 0.004f, 4f, 0.6f);
            AddNoise(b, 0f, 1.3f, 1f, 0.01f, 2.5f, 2200f, 50f, 500f);
            AddSizzle(b, 0.2f, 1.2f, 0.5f);
            for (int i = 0; i < 8; i++) AddNoise(b, 0.1f + (float)rnd.NextDouble() * 0.8f, 0.05f, 0.4f, 0.001f, 50f, 5000f, 1200f);
            return Make("sfx_eruption", Distort(b, 1.8f), 0.9f);
        }

        /// <summary>Yanardağ ortam sesi (10 sn döngü): derin yer uğultusu + ara ara çatırdayan kor + uzak gümbürtü.</summary>
        public static AudioClip VolcanoAmbience()
        {
            rnd = new System.Random(34);
            const float len = 10f, xf = 1f;
            var b = Buf(len + xf);
            float lp1 = 0f, lp2 = 0f, a1 = LpA(120f), a2 = LpA(45f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                lp1 += a1 * (Noise() - lp1);
                lp2 += a2 * (lp1 - lp2);
                float swell = 0.65f + 0.35f * Mathf.Sin(TwoPi * t / len * 2f + 0.5f);
                b[i] = lp2 * 7f * swell + Mathf.Sin(TwoPi * 36f * t) * 0.06f * swell;
            }
            // Çatırdayan korlar
            for (int i = 0; i < 70; i++)
                AddNoise(b, (float)rnd.NextDouble() * (len - 0.1f), 0.03f, 0.05f + (float)rnd.NextDouble() * 0.08f, 0.0005f, 90f, 6000f, 1800f);
            // Uzak gümbürtüler
            AddSweep(b, 2.5f, 1.2f, 60f, 35f, 0.25f, 0.2f, 2f);
            AddSweep(b, 7.2f, 1.2f, 55f, 32f, 0.2f, 0.2f, 2f);
            int L = (int)(len * SR), X = (int)(xf * SR);
            var o = new float[L];
            for (int i = 0; i < L; i++)
            {
                o[i] = b[i];
                if (i < X) { float k = i / (float)X; o[i] = b[i] * k + b[L + i] * (1f - k); }
            }
            float max = 0.0001f;
            for (int i = 0; i < L; i++) max = Mathf.Max(max, Mathf.Abs(o[i]));
            for (int i = 0; i < L; i++) o[i] *= 0.8f / max;
            var clip = AudioClip.Create("amb_volcano", L, 1, SR, false);
            clip.SetData(o, 0);
            return clip;
        }

        // ─────────────────────────────────────────────────────
        // BATAKLIK
        // ─────────────────────────────────────────────────────

        /// <summary>Kısa kurbağa vıraklaması (kare dalga darbeleri).</summary>
        static void AddCroak(float[] b, float start, float pitch, float amp)
        {
            int pulses = 2 + rnd.Next(2);
            for (int i = 0; i < pulses; i++)
                AddSquare(b, start + i * 0.055f, 0.06f, pitch * 1.15f, pitch, amp, 0.004f, 30f);
        }

        /// <summary>Bataklık yaratığı ölümü: ıslak şapırtı + kabarcık patlamaları (küçükte vıraklama).</summary>
        public static AudioClip SquishDeath(float size)
        {
            rnd = new System.Random(41 + (int)(size * 10f));
            var b = Buf(0.45f + 0.25f * size);
            AddNoise(b, 0f, 0.2f * size, 1f, 0.002f, 16f / size, 1100f / size, 90f);
            AddSweep(b, 0f, 0.18f * size, 200f / size, 70f / size, 0.6f, 0.003f, 14f / size);
            for (int i = 0; i < 3 + (int)size; i++)
                AddBubble(b, 0.03f + (float)rnd.NextDouble() * 0.25f * size, 0.6f + (float)rnd.NextDouble() * size, 0.25f);
            if (size < 1.5f) AddCroak(b, 0f, 170f, 0.25f);
            return Make("sfx_squish_death_" + size.ToString("0.0"), Distort(b, 1.2f), 0.85f);
        }

        /// <summary>Çamur / zehir sıçraması (ok isabeti).</summary>
        public static AudioClip MudSplash()
        {
            rnd = new System.Random(42);
            var b = Buf(0.5f);
            AddNoise(b, 0f, 0.3f, 0.9f, 0.002f, 12f, 1500f, 120f);
            for (int i = 0; i < 4; i++) AddBubble(b, 0.02f + (float)rnd.NextDouble() * 0.3f, 0.5f + (float)rnd.NextDouble() * 0.8f, 0.2f);
            return Make("sfx_mud_splash", b, 0.7f);
        }

        /// <summary>Kök fışkırması: odun çatırtısı + yer gümbürtüsü + toprak saçılması.</summary>
        public static AudioClip RootBurst()
        {
            rnd = new System.Random(43);
            var b = Buf(1.3f);
            for (int i = 0; i < 9; i++)
                AddNoise(b, (float)rnd.NextDouble() * 0.25f, 0.06f, 0.7f, 0.0005f, 70f, 7000f, 1500f);
            AddSweep(b, 0f, 0.8f, 95f, 34f, 1f, 0.005f, 4.5f, 0.5f);
            AddNoise(b, 0.02f, 1.1f, 0.8f, 0.01f, 3.5f, 800f, 40f);
            return Make("sfx_root_burst", Distort(b, 1.6f), 0.9f);
        }

        /// <summary>Yay atışı: gerilen kirişin tınlaması + ok vınlaması.</summary>
        public static AudioClip BowShot()
        {
            rnd = new System.Random(44);
            var b = Buf(0.45f);
            AddSweep(b, 0f, 0.25f, 230f, 170f, 0.6f, 0.002f, 22f, 0.8f);
            AddNoise(b, 0.01f, 0.35f, 0.6f, 0.03f, 10f, 3200f, 700f, 1500f);
            return Make("sfx_bow_shot", b, 0.75f);
        }

        /// <summary>Bataklık ortam sesi (10 sn döngü): alçak uğultu, cırcır böceği vızıltısı, uzak kurbağalar, kabarcıklar.</summary>
        public static AudioClip SwampAmbience()
        {
            rnd = new System.Random(45);
            const float len = 10f, xf = 1f;
            var b = Buf(len + xf);
            float lp1 = 0f, lp2 = 0f, a1 = LpA(220f), a2 = LpA(80f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                lp1 += a1 * (Noise() - lp1);
                lp2 += a2 * (lp1 - lp2);
                float swell = 0.6f + 0.4f * Mathf.Sin(TwoPi * t / len * 2f + 1f);
                // Böcek vızıltısı: yüksek ton, hızlı titreşimle açılıp kapanır
                float buzz = Mathf.Sin(TwoPi * 4100f * t) * Mathf.Max(0f, Mathf.Sin(TwoPi * 28f * t)) * 0.035f * (0.5f + 0.5f * Mathf.Sin(TwoPi * t / len * 3f));
                b[i] = lp2 * 4f * swell + buzz;
            }
            for (int i = 0; i < 9; i++)
                AddCroak(b, (float)rnd.NextDouble() * (len - 0.4f), 90f + (float)rnd.NextDouble() * 60f, 0.08f + (float)rnd.NextDouble() * 0.07f);
            for (int i = 0; i < 14; i++)
                AddBubble(b, (float)rnd.NextDouble() * (len - 0.3f), 1f + (float)rnd.NextDouble() * 1.4f, 0.05f + (float)rnd.NextDouble() * 0.06f);
            int L = (int)(len * SR), X = (int)(xf * SR);
            var o = new float[L];
            for (int i = 0; i < L; i++)
            {
                o[i] = b[i];
                if (i < X) { float k = i / (float)X; o[i] = b[i] * k + b[L + i] * (1f - k); }
            }
            float max = 0.0001f;
            for (int i = 0; i < L; i++) max = Mathf.Max(max, Mathf.Abs(o[i]));
            for (int i = 0; i < L; i++) o[i] *= 0.8f / max;
            var clip = AudioClip.Create("amb_swamp", L, 1, SR, false);
            clip.SetData(o, 0);
            return clip;
        }

        // ─────────────────────────────────────────────────────
        // KAR / BUZ
        // ─────────────────────────────────────────────────────

        /// <summary>Buz tınısı: yüksek, hızla sönen çan benzeri kısa tonlar.</summary>
        static void AddTinkle(float[] b, float start, float f, float amp)
        {
            AddSweep(b, start, 0.25f, f, f * 0.98f, amp, 0.001f, 22f, 0.3f);
            AddSweep(b, start, 0.18f, f * 2.76f, f * 2.7f, amp * 0.4f, 0.001f, 30f);
        }

        /// <summary>Kar yaratığı ölümü: kar çıtırtısı + yumuşak gümleme (küçükte kısa bir inilti).</summary>
        public static AudioClip SnowDeath(float size)
        {
            rnd = new System.Random(51 + (int)(size * 10f));
            var b = Buf(0.45f + 0.2f * size);
            AddNoise(b, 0f, 0.25f * size, 0.9f, 0.002f, 14f / size, 2600f / size, 300f);
            for (int i = 0; i < 6; i++) AddNoise(b, (float)rnd.NextDouble() * 0.2f * size, 0.03f, 0.35f, 0.0005f, 90f, 7000f, 2500f);
            AddSweep(b, 0f, 0.2f * size, 140f / size, 60f / size, 0.6f, 0.003f, 12f / size);
            if (size < 1.5f) AddSweep(b, 0.02f, 0.22f, 820f, 480f, 0.18f, 0.01f, 10f, 0.4f);
            return Make("sfx_snow_death_" + size.ToString("0.0"), b, 0.85f);
        }

        /// <summary>Boss ölümü: büyük buz kırılması + tınılar + gümbürtü.</summary>
        public static AudioClip IceShatterBig()
        {
            rnd = new System.Random(52);
            var b = Buf(1.6f);
            AddNoise(b, 0f, 0.5f, 1f, 0.001f, 7f, 6000f, 800f);
            AddSweep(b, 0f, 0.9f, 90f, 30f, 1f, 0.005f, 3.5f, 0.6f);
            for (int i = 0; i < 12; i++) AddTinkle(b, (float)rnd.NextDouble() * 0.9f, 1800f + (float)rnd.NextDouble() * 2400f, 0.12f);
            return Make("sfx_ice_shatter_big", Distort(b, 1.4f), 0.9f);
        }

        /// <summary>Buz kristali isabeti: çınlayan kırılma.</summary>
        public static AudioClip IceHit()
        {
            rnd = new System.Random(53);
            var b = Buf(0.5f);
            AddNoise(b, 0f, 0.12f, 0.8f, 0.0005f, 35f, 8000f, 1500f);
            for (int i = 0; i < 4; i++) AddTinkle(b, 0.01f + (float)rnd.NextDouble() * 0.12f, 2000f + (float)rnd.NextDouble() * 2000f, 0.18f);
            return Make("sfx_ice_hit", b, 0.7f);
        }

        /// <summary>Buzul Gürzü: yer sarsıntısı + buz dikenlerinin çatırdayarak fırlaması.</summary>
        public static AudioClip IceBurst()
        {
            rnd = new System.Random(54);
            var b = Buf(1.4f);
            AddSweep(b, 0f, 0.8f, 100f, 30f, 1f, 0.004f, 4.5f, 0.6f);
            AddNoise(b, 0f, 0.9f, 0.7f, 0.005f, 4f, 1200f, 60f);
            for (int i = 0; i < 10; i++) AddNoise(b, 0.05f + (float)rnd.NextDouble() * 0.3f, 0.05f, 0.5f, 0.0005f, 60f, 7500f, 2000f);
            for (int i = 0; i < 6; i++) AddTinkle(b, 0.1f + (float)rnd.NextDouble() * 0.4f, 1500f + (float)rnd.NextDouble() * 1500f, 0.1f);
            return Make("sfx_ice_burst", Distort(b, 1.6f), 0.9f);
        }

        /// <summary>Asadan buz fırlatma: parıltılı vınlama.</summary>
        public static AudioClip IceCast()
        {
            rnd = new System.Random(55);
            var b = Buf(0.5f);
            AddNoise(b, 0f, 0.35f, 0.5f, 0.03f, 9f, 5000f, 1500f, 2000f);
            AddSweep(b, 0f, 0.3f, 1400f, 2600f, 0.2f, 0.01f, 9f);
            AddTinkle(b, 0.02f, 3200f, 0.12f);
            return Make("sfx_ice_cast", b, 0.7f);
        }

        /// <summary>Kar fırtınası ortam sesi (10 sn döngü): uğuldayan rüzgâr, dalgalanan esintiler, ara ara ıslık.</summary>
        public static AudioClip BlizzardAmbience()
        {
            rnd = new System.Random(56);
            const float len = 10f, xf = 1f;
            var b = Buf(len + xf);
            float lp1 = 0f, lp2 = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float gust = 0.55f + 0.45f * Mathf.Sin(TwoPi * t / len * 2f) * Mathf.Sin(TwoPi * t / len * 3f + 0.7f);
                float cut = 300f + 900f * gust;
                lp1 += LpA(cut) * (Noise() - lp1);
                lp2 += LpA(cut * 0.6f) * (lp1 - lp2);
                float howl = Mathf.Sin(TwoPi * (320f + 90f * Mathf.Sin(TwoPi * t / len * 2f)) * t) * 0.03f * gust * gust;
                b[i] = (lp1 - lp2 * 0.5f) * 3.5f * gust + howl;
            }
            int L = (int)(len * SR), X = (int)(xf * SR);
            var o = new float[L];
            for (int i = 0; i < L; i++)
            {
                o[i] = b[i];
                if (i < X) { float k = i / (float)X; o[i] = b[i] * k + b[L + i] * (1f - k); }
            }
            float max = 0.0001f;
            for (int i = 0; i < L; i++) max = Mathf.Max(max, Mathf.Abs(o[i]));
            for (int i = 0; i < L; i++) o[i] *= 0.8f / max;
            var clip = AudioClip.Create("amb_blizzard", L, 1, SR, false);
            clip.SetData(o, 0);
            return clip;
        }

        // ─────────────────────────────────────────────────────
        // GÖKYÜZÜ / FIRTINA
        // ─────────────────────────────────────────────────────

        /// <summary>Elektrik cızırtısı: kısa, rastgele aralıklı yüksek frekanslı çıtırtılar.</summary>
        static void AddCrackle(float[] b, float start, float dur, int count, float amp)
        {
            for (int i = 0; i < count; i++)
                AddNoise(b, start + (float)rnd.NextDouble() * dur, 0.012f + (float)rnd.NextDouble() * 0.02f, amp, 0.0005f, 120f, 9000f, 3000f);
        }

        /// <summary>Gök gürültüsü yuvarlanması: rastgele patlamalı, uzun, alçak gürültü.</summary>
        static void AddRumble(float[] b, float start, float dur, float amp)
        {
            AddNoise(b, start, dur, amp, 0.05f, 2.2f / dur, 220f, 25f);
            for (int i = 0; i < 6; i++)
                AddNoise(b, start + (float)rnd.NextDouble() * dur * 0.6f, dur * 0.4f, amp * 0.6f, 0.02f, 5f / dur, 160f + (float)rnd.NextDouble() * 120f, 20f);
        }

        /// <summary>Bulut ruhu / griffon ölümü: yumuşak "puf" + elektrik boşalması (iride kartal çığlığı).</summary>
        public static AudioClip PuffDeath(float size)
        {
            rnd = new System.Random(61 + (int)(size * 10f));
            var b = Buf(0.45f + 0.25f * size);
            AddNoise(b, 0f, 0.3f * size, 0.8f, 0.01f, 10f / size, 1400f / size, 120f);
            AddCrackle(b, 0f, 0.2f * size, 6 + (int)(size * 4f), 0.35f);
            AddSweep(b, 0f, 0.18f * size, 220f / size, 90f / size, 0.4f, 0.003f, 14f / size);
            if (size > 1.5f) AddSweep(b, 0.02f, 0.4f, 1900f, 900f, 0.22f, 0.02f, 5f, 0.5f);
            return Make("sfx_puff_death_" + size.ToString("0.0"), b, 0.85f);
        }

        /// <summary>Boss ölümü: uzun gök gürültüsü + büyük elektrik boşalması.</summary>
        public static AudioClip ThunderBig()
        {
            rnd = new System.Random(62);
            var b = Buf(2.6f);
            AddNoise(b, 0f, 0.15f, 1f, 0.0005f, 25f, 9000f, 1200f);
            AddCrackle(b, 0f, 0.5f, 30, 0.4f);
            AddRumble(b, 0.05f, 2.4f, 1.1f);
            AddSweep(b, 0f, 1.2f, 80f, 28f, 0.8f, 0.01f, 2.5f, 0.5f);
            return Make("sfx_thunder_big", Distort(b, 1.5f), 0.95f);
        }

        /// <summary>Yıldırım isabeti (zincir yıldırım): keskin elektrik şaklaması.</summary>
        public static AudioClip Zap()
        {
            rnd = new System.Random(63);
            var b = Buf(0.35f);
            AddNoise(b, 0f, 0.06f, 0.9f, 0.0005f, 40f, 9000f, 2000f);
            AddCrackle(b, 0f, 0.2f, 14, 0.5f);
            AddSquare(b, 0f, 0.15f, 180f, 120f, 0.25f, 0.002f, 20f);
            return Make("sfx_zap", b, 0.75f);
        }

        /// <summary>Fırtına Lordu yıldırımı: kulak tırmalayan şak + derin gümbürtü.</summary>
        public static AudioClip ThunderStrike()
        {
            rnd = new System.Random(64);
            var b = Buf(1.8f);
            AddNoise(b, 0f, 0.08f, 1f, 0.0003f, 30f, 10000f, 1500f);
            AddCrackle(b, 0f, 0.3f, 24, 0.5f);
            AddSweep(b, 0f, 0.9f, 110f, 32f, 1f, 0.004f, 4f, 0.6f);
            AddRumble(b, 0.08f, 1.6f, 0.9f);
            return Make("sfx_thunder_strike", Distort(b, 1.8f), 0.95f);
        }

        /// <summary>Şimşek fırlatma: yükselen elektrik vızıltısı.</summary>
        public static AudioClip ZapThrow()
        {
            rnd = new System.Random(65);
            var b = Buf(0.45f);
            AddSquare(b, 0f, 0.35f, 90f, 260f, 0.3f, 0.02f, 7f);
            AddNoise(b, 0f, 0.3f, 0.4f, 0.02f, 8f, 6000f, 1800f);
            AddCrackle(b, 0.05f, 0.25f, 8, 0.35f);
            return Make("sfx_zap_throw", b, 0.7f);
        }

        /// <summary>Fırtına ortam sesi (12 sn döngü): yüksekte esen rüzgâr + uzaktan yuvarlanan gök gürültüleri.</summary>
        public static AudioClip StormAmbience()
        {
            rnd = new System.Random(66);
            const float len = 12f, xf = 1f;
            var b = Buf(len + xf);
            float lp1 = 0f, lp2 = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float gust = 0.6f + 0.4f * Mathf.Sin(TwoPi * t / len * 2f) * Mathf.Sin(TwoPi * t / len * 3f + 1.3f);
                float cut = 250f + 600f * gust;
                lp1 += LpA(cut) * (Noise() - lp1);
                lp2 += LpA(cut * 0.5f) * (lp1 - lp2);
                b[i] = (lp1 - lp2 * 0.4f) * 2.6f * gust;
            }
            AddRumble(b, 1.2f, 3.5f, 0.5f);
            AddRumble(b, 6.5f, 4f, 0.35f);
            int L = (int)(len * SR), X = (int)(xf * SR);
            var o = new float[L];
            for (int i = 0; i < L; i++)
            {
                o[i] = b[i];
                if (i < X) { float k = i / (float)X; o[i] = b[i] * k + b[L + i] * (1f - k); }
            }
            float max = 0.0001f;
            for (int i = 0; i < L; i++) max = Mathf.Max(max, Mathf.Abs(o[i]));
            for (int i = 0; i < L; i++) o[i] *= 0.8f / max;
            var clip = AudioClip.Create("amb_storm", L, 1, SR, false);
            clip.SetData(o, 0);
            return clip;
        }

        // ─────────────────────────────────────────────────────
        // ÇÖL
        // ─────────────────────────────────────────────────────

        /// <summary>Çöl yaratığı ölümü: kabuk çıtırtısı + kum hışırtısı (iride taş çatlaması + gümbürtü).</summary>
        public static AudioClip SandDeath(float size)
        {
            rnd = new System.Random(71 + (int)(size * 10f));
            var b = Buf(0.45f + 0.25f * size);
            AddNoise(b, 0.02f, 0.35f * size, 0.6f, 0.02f, 7f / size, 5000f, 1200f);
            for (int i = 0; i < 5 + (int)(size * 3f); i++) AddNoise(b, (float)rnd.NextDouble() * 0.12f * size, 0.02f, 0.5f, 0.0005f, 100f, 6000f / size, 900f);
            AddSweep(b, 0f, 0.16f * size, 180f / size, 70f / size, 0.5f, 0.003f, 16f / size);
            return Make("sfx_sand_death_" + size.ToString("0.0"), b, 0.85f);
        }

        /// <summary>Boss ölümü: dev kabuğun çöküşü, uzun kum akışı.</summary>
        public static AudioClip SandCrumble()
        {
            rnd = new System.Random(72);
            var b = Buf(2.2f);
            AddSweep(b, 0f, 1f, 90f, 30f, 1f, 0.005f, 3f, 0.6f);
            for (int i = 0; i < 18; i++) AddNoise(b, (float)rnd.NextDouble() * 0.9f, 0.06f, 0.6f, 0.001f, 40f, 3000f, 300f);
            AddNoise(b, 0.2f, 2f, 0.6f, 0.3f, 1.6f, 4000f, 900f);
            return Make("sfx_sand_crumble", Distort(b, 1.4f), 0.9f);
        }

        /// <summary>Çakram / kum isabeti: kuru şaklama + kum serpintisi.</summary>
        public static AudioClip SandHit()
        {
            rnd = new System.Random(73);
            var b = Buf(0.35f);
            AddNoise(b, 0f, 0.05f, 0.9f, 0.0005f, 50f, 7000f, 1500f);
            AddNoise(b, 0.01f, 0.3f, 0.4f, 0.01f, 12f, 5000f, 1500f);
            AddSweep(b, 0f, 0.08f, 1400f, 900f, 0.2f, 0.001f, 40f, 0.3f);
            return Make("sfx_sand_hit", b, 0.7f);
        }

        /// <summary>Kum fırtınası dalgası: yükselen uğultu + savrulan kum kükremesi.</summary>
        public static AudioClip SandStorm()
        {
            rnd = new System.Random(74);
            var b = Buf(1.6f);
            AddNoise(b, 0f, 1.5f, 1f, 0.15f, 2.4f, 900f, 60f, 3500f);
            AddNoise(b, 0.05f, 1.2f, 0.5f, 0.1f, 3f, 6000f, 2000f);
            AddSweep(b, 0f, 0.9f, 70f, 40f, 0.7f, 0.05f, 3f, 0.5f);
            return Make("sfx_sand_storm", Distort(b, 1.3f), 0.95f);
        }

        /// <summary>Çakram fırlatma: metalik vınlayan dönme sesi.</summary>
        public static AudioClip ChakramThrow()
        {
            rnd = new System.Random(75);
            var b = Buf(0.6f);
            int n = b.Length;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float wob = 0.5f + 0.5f * Mathf.Sin(TwoPi * 22f * t);
                b[i] += Mathf.Sin(TwoPi * 1250f * t) * 0.15f * wob * Env(t, 0.01f, 5f) + Mathf.Sin(TwoPi * 2510f * t) * 0.06f * Env(t, 0.01f, 7f);
            }
            AddNoise(b, 0f, 0.5f, 0.6f, 0.02f, 6f, 3000f, 600f);
            return Make("sfx_chakram_throw", b, 0.7f);
        }

        /// <summary>Çöl ortam sesi (12 sn döngü): sıcak, kuru rüzgâr + ara ara kum hışırtısı.</summary>
        public static AudioClip DesertAmbience()
        {
            rnd = new System.Random(76);
            const float len = 12f, xf = 1f;
            var b = Buf(len + xf);
            float lp1 = 0f, lp2 = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float gust = 0.55f + 0.45f * Mathf.Sin(TwoPi * t / len * 2f + 0.4f) * Mathf.Sin(TwoPi * t / len * 3f);
                float cut = 200f + 500f * gust;
                lp1 += LpA(cut) * (Noise() - lp1);
                lp2 += LpA(cut * 0.5f) * (lp1 - lp2);
                b[i] = (lp1 - lp2 * 0.5f) * 2.4f * gust;
            }
            for (int i = 0; i < 6; i++) AddNoise(b, (float)rnd.NextDouble() * (len - 1.5f), 1.2f, 0.06f, 0.4f, 2f, 7000f, 2500f);
            int L = (int)(len * SR), X = (int)(xf * SR);
            var o = new float[L];
            for (int i = 0; i < L; i++)
            {
                o[i] = b[i];
                if (i < X) { float k = i / (float)X; o[i] = b[i] * k + b[L + i] * (1f - k); }
            }
            float max = 0.0001f;
            for (int i = 0; i < L; i++) max = Mathf.Max(max, Mathf.Abs(o[i]));
            for (int i = 0; i < L; i++) o[i] *= 0.8f / max;
            var clip = AudioClip.Create("amb_desert", L, 1, SR, false);
            clip.SetData(o, 0);
            return clip;
        }

        /// <summary>
        /// Su altı ortam sesi (kesintisiz döngü, 10 sn): derin uğultu, yavaş dalgalanma ve ara ara kabarcıklar.
        /// Başı ve sonu çapraz geçişle birleştirilir → döngüde atlama duyulmaz.
        /// </summary>
        public static AudioClip Ambience()
        {
            rnd = new System.Random(28);
            const float len = 10f, xf = 1f;
            var b = Buf(len + xf);
            float lp1 = 0f, lp2 = 0f, a1 = LpA(180f), a2 = LpA(60f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float n = Noise();
                lp1 += a1 * (n - lp1);
                lp2 += a2 * (lp1 - lp2);
                float swell = 0.6f + 0.4f * Mathf.Sin(TwoPi * t / len * 2f) * Mathf.Sin(TwoPi * t / len * 3f + 1f);
                b[i] = lp2 * 6f * swell + Mathf.Sin(TwoPi * 43f * t) * 0.05f * swell;
            }
            for (int i = 0; i < 26; i++)
                AddBubble(b, (float)rnd.NextDouble() * (len - 0.3f), 0.4f + (float)rnd.NextDouble() * 1.4f, 0.05f + (float)rnd.NextDouble() * 0.08f);
            // Çapraz geçişli döngü
            int L = (int)(len * SR), X = (int)(xf * SR);
            var o = new float[L];
            for (int i = 0; i < L; i++)
            {
                o[i] = b[i];
                if (i < X)
                {
                    float k = i / (float)X;
                    o[i] = b[i] * k + b[L + i] * (1f - k);
                }
            }
            float max = 0.0001f;
            for (int i = 0; i < L; i++) max = Mathf.Max(max, Mathf.Abs(o[i]));
            for (int i = 0; i < L; i++) o[i] *= 0.8f / max;
            var clip = AudioClip.Create("amb_underwater", L, 1, SR, false);
            clip.SetData(o, 0);
            return clip;
        }
    }
}
