Shader "StratejiGame/VertexColorToon"
{
    // Dokusuz modeller: renkler köşe renklerinden (vertex color) gelir.
    // Toon gölgelendirme + kenar ışığı + SU ALTI: hareketli kostik ışık desenleri ve
    // bitkiler için sallanma (köşe renginin alfası: 1 = sabit, 0 = tam sallanır). URP, mobil uyumlu.
    Properties
    {
        _BaseColor ("Renk Çarpanı", Color) = (1,1,1,1)
        _ShadeSoftness ("Gölge Yumuşaklığı", Range(0.01, 1)) = 0.35
        _ShadowColor ("Gölge Rengi", Color) = (0.55, 0.6, 0.75, 1)
        _RimColor ("Kenar Işığı", Color) = (0.6, 0.85, 1, 1)
        _RimPower ("Kenar Işığı Keskinliği", Range(0.5, 8)) = 3
        _RimStrength ("Kenar Işığı Gücü", Range(0, 1)) = 0.35
        _CausticStrength ("Kostik Işık Gücü", Range(0, 2)) = 0.4
        _CausticScale ("Kostik Ölçeği", Range(0.05, 2)) = 0.28
        _SwayStrength ("Sallanma Gücü", Range(0, 1)) = 0.25
        _Glow ("Parlama (köşe alfası 0 = tam parlak)", Range(0, 4)) = 0
        _RootWave ("Kök Kıvranması (nesne uzayı)", Range(0, 0.1)) = 0
        _RootHeight ("Kök Yüksekliği (nesne uzayı)", Range(0.01, 1)) = 0.14
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _ShadeSoftness;
                float4 _ShadowColor;
                float4 _RimColor;
                float _RimPower;
                float _RimStrength;
                float _CausticStrength;
                float _CausticScale;
                float _SwayStrength;
                float _Glow;
                float _RootWave;
                float _RootHeight;
            CBUFFER_END
            TEXTURE2D(_CausticTex); SAMPLER(sampler_CausticTex);
            float4 _CausticColor;
            // Genel kostik anahtarı (AppBoot ayarlar): zayıf GPU'da 0 → iki doku okuması tamamen atlanır
            float _CausticGlobal;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fog : TEXCOORD2;
                half3 sh : TEXCOORD3;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                float3 pos = i.positionOS.xyz;
                // Ayaksız boss: gövdenin altındaki kökler kıvranır (nesne uzayında, tabana yakın köşeler)
                if (_RootWave > 0.0)
                {
                    float k = saturate(1.0 - pos.y / _RootHeight);
                    k *= k;
                    float ph = _Time.y * 2.6 + atan2(pos.z, pos.x) * 3.0 + pos.y * 20.0;
                    float2 dir = normalize(pos.xz + 1e-4);
                    pos.xz += dir * sin(ph) * _RootWave * k;
                    pos.y += cos(ph * 0.8) * _RootWave * 0.4 * k;
                }
                float3 ws = TransformObjectToWorld(pos);
                // Su akıntısında sallanma (bitkiler, yosun sarkıntıları)
                float w = (1.0 - i.color.a) * _SwayStrength;
                float t = _Time.y;
                ws.x += sin(t * 1.3 + ws.z * 0.55 + ws.y * 0.8) * w;
                ws.z += cos(t * 1.05 + ws.x * 0.45 + ws.y * 0.6) * w * 0.6;
                o.positionCS = TransformWorldToHClip(ws);
                o.positionWS = ws;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                // Ortam ışığı köşe başına (sahnelerde düz ortam ışığı kullanılıyor, piksel başına gerek yok)
                o.sh = SampleSH(o.normalWS);
                o.color = i.color;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                Light l = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float ndl = dot(n, l.direction) * 0.5 + 0.5;
                float lit = smoothstep(0.5 - _ShadeSoftness * 0.5, 0.5 + _ShadeSoftness * 0.5, ndl) * l.shadowAttenuation;
                float3 baseCol = i.color.rgb * _BaseColor.rgb;
                // Gölge rengi ışık şiddetiyle birlikte kararır (karanlık sahnede gölgeler parlak kalmasın)
                float li = max(l.color.r, max(l.color.g, l.color.b));
                float3 col = baseCol * lerp(_ShadowColor.rgb * li, l.color, lit) + baseCol * i.sh * 0.35;

                // Kostik: iki farklı yöne kayan desenin minimumu → dalgalanan ışık ağı
                float cs = _CausticStrength * _CausticGlobal;
                if (cs > 0.001)
                {
                    float t = _Time.y;
                    float2 uv = i.positionWS.xz * _CausticScale + i.positionWS.y * 0.15;
                    float c1 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, uv + float2(t * 0.031, t * 0.019)).r;
                    float c2 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, uv * 1.37 + float2(-t * 0.023, t * 0.037)).r;
                    float caustic = min(c1, c2);
                    float facing = saturate(n.y * 0.75 + 0.35);
                    col += caustic * cs * facing * (0.35 + 0.65 * lit) * _CausticColor.rgb * baseCol * 2.2;
                }

                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float rim = pow(1.0 - saturate(dot(n, v)), _RimPower);
                col += _RimColor.rgb * rim * _RimStrength;
                // Kendi ışığını yayan bölgeler (lav çatlakları, çekirdek, gözler): alfa düştükçe parlar
                col += baseCol * (1.0 - i.color.a) * _Glow;
                col = MixFog(col, lerp(i.fog, 1.0, (1.0 - i.color.a) * saturate(_Glow) * 0.6));
                return half4(col, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack Off
}