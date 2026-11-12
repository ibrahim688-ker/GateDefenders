using UnityEngine;

namespace StratejiGame
{
    /// <summary>
    /// Sabit uzunlukta ve genişlikte savunma alanını (graybox) kurar. Mekan hareket etmez;
    /// düşmanlar ve kapılar uzaktaki giriş kapısından çıkıp ana karaktere doğru yürür.
    /// </summary>
    public static class CorridorBuilder
    {
        public static Color FogColor { get; private set; } = new Color(0.012f, 0.05f, 0.085f);

public static void Build(Transform root, float hw, GameManager gm)
        {
            // Görsel parçalar: EnvironmentDesigns (yol, kum, sütunlar, resif, şelaleli kapı) + SeaLife (canlılar, atmosfer)
            var env = new GameObject("Environment").transform;
            env.SetParent(root, false);

            const float nearZ = -8f;
            float farZ = gm.spawnZ + 3f;
            // Kapının şelalesi doğma noktasının hemen önünde: düşmanlar suyun içinden çıkar
            float gateZ = gm.spawnZ + 0.8f;

            var sun = RenderSettings.sun != null ? RenderSettings.sun : Object.FindFirstObjectByType<Light>();
            WaterFx.Theme = gm.Theme;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            if (gm.Theme == WorldTheme.Volcano)
            {
                // Yanardağ: sıcak turuncu güneş, kızıl sis; lav, korlar ve kristaller karanlıkta parlar
                FogColor = VolcanoDesigns.Fog;
                VolcanoDesigns.Build(env, hw, nearZ, farZ, gateZ);
                VolcanoLife.Build(env, hw, nearZ, gateZ);
                if (sun != null) { sun.intensity = 0.75f; sun.color = new Color(1f, 0.62f, 0.42f); }
                RenderSettings.fogColor = FogColor;
                RenderSettings.fogStartDistance = 28f;
                RenderSettings.fogEndDistance = 105f;
                RenderSettings.ambientLight = new Color(0.26f, 0.14f, 0.1f);
                return;
            }

            if (gm.Theme == WorldTheme.Desert)
            {
                // Çöl: kavurucu, sıcak güneş; sarı-altın kum pusu; ekran kenarlarında serap
                FogColor = DesertDesigns.Fog;
                DesertDesigns.Build(env, hw, nearZ, farZ, gateZ);
                DesertLife.Build(env, hw, nearZ, gateZ);
                if (!AppBoot.LowEnd) DesertHeat.Create(env);   // tam ekran ısı katmanı zayıf GPU'da pahalı
                if (sun != null) { sun.intensity = 1.05f; sun.color = new Color(1f, 0.9f, 0.72f); }
                RenderSettings.fogColor = FogColor;
                RenderSettings.fogStartDistance = 36f;
                RenderSettings.fogEndDistance = 125f;
                RenderSettings.ambientLight = new Color(0.46f, 0.38f, 0.3f);
                return;
            }

            if (gm.Theme == WorldTheme.Sky)
            {
                // Gökyüzü: bulutların üstünde, koyu mor-lacivert fırtına; soğuk ay ışığı gibi güneş, şimşekler aydınlatır
                FogColor = SkyDesigns.Fog;
                SkyDesigns.Build(env, hw, nearZ, farZ, gateZ);
                SkyLife.Build(env, hw, nearZ, gateZ);
                if (sun != null) { sun.intensity = 0.8f; sun.color = new Color(0.78f, 0.82f, 1f); }
                RenderSettings.fogColor = FogColor;
                RenderSettings.fogStartDistance = 30f;
                RenderSettings.fogEndDistance = 110f;
                RenderSettings.ambientLight = new Color(0.27f, 0.27f, 0.38f);
                return;
            }

            if (gm.Theme == WorldTheme.Frost)
            {
                // Kar: parlak, soğuk güneş; açık mavi-gri kar fırtınası sisi; kenarlarda buzlanma
                FogColor = FrostDesigns.Fog;
                FrostDesigns.Build(env, hw, nearZ, farZ, gateZ);
                FrostLife.Build(env, hw, nearZ, gateZ);
                if (!AppBoot.LowEnd) FrostVignette.Create(env); // tam ekran buz kenarlığı zayıf GPU'da pahalı
                if (sun != null) { sun.intensity = 0.85f; sun.color = new Color(0.95f, 0.97f, 1f); }
                RenderSettings.fogColor = FogColor;
                RenderSettings.fogStartDistance = 34f;
                RenderSettings.fogEndDistance = 118f;
                RenderSettings.ambientLight = new Color(0.3f, 0.35f, 0.45f);
                return;
            }

            if (gm.Theme == WorldTheme.Swamp)
            {
                // Bataklık: soluk yeşilimsi güneş, koyu yeşil sis; mantarlar, zehirli sular ve ateş böcekleri parlar
                FogColor = SwampDesigns.Fog;
                SwampDesigns.Build(env, hw, nearZ, farZ, gateZ);
                SwampLife.Build(env, hw, nearZ, gateZ);
                if (sun != null) { sun.intensity = 0.6f; sun.color = new Color(0.78f, 0.92f, 0.72f); }
                RenderSettings.fogColor = FogColor;
                RenderSettings.fogStartDistance = 22f;
                RenderSettings.fogEndDistance = 90f;
                RenderSettings.ambientLight = new Color(0.18f, 0.22f, 0.17f);
                return;
            }

            // Derin deniz havası: zayıf, mavimsi güneş ışığı; koyu ortam ışığı; yakın başlayan koyu sis.
            FogColor = new Color(0.012f, 0.05f, 0.085f);
            EnvironmentDesigns.Build(env, hw, nearZ, farZ, gateZ);
            SeaLife.Build(env, hw, nearZ, gateZ);
            if (sun != null) { sun.intensity = 0.6f; sun.color = new Color(0.62f, 0.8f, 1f); }
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 78f;
            RenderSettings.ambientLight = new Color(0.13f, 0.21f, 0.3f);
        }
    }
}
