using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Zayıf ekran kartlarında (AppBoot.LowEnd) ortam efektlerini hafifletir.
    /// Telefon ölçümleri: üçgen sayısı değil, ekranı kaplayan yarı saydam parçacıklar ve tam ekran katmanlar FPS'i düşürüyordu
    /// (Kar 60k üçgenle 41 FPS, Su altı 183k üçgenle 60 FPS). Ortam parçacıkları yarıya, iri olanlar (sis, toz, bulut) üçte bire iner.
    /// </summary>
    public static class PerfTuning
    {
        public static void ReduceEnvironmentEffects(Transform gameRoot)
        {
            var env = gameRoot.Find("Environment");
            if (env == null) return;
            foreach (var ps in env.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                float size = main.startSize.mode == ParticleSystemCurveMode.TwoConstants ? main.startSize.constantMax : main.startSizeMultiplier;
                float k = size >= 0.3f ? 0.33f : 0.5f;
                main.maxParticles = Mathf.Max(4, Mathf.RoundToInt(main.maxParticles * k));
                var em = ps.emission;
                em.rateOverTimeMultiplier *= k;
            }
        }
    }
}
