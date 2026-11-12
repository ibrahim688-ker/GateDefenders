Shader "StratejiGame/SeaBackdrop"
{
    // Uzak arka plan: açık deniz. Ufukta sis rengi (sahneyle kusursuz birleşir), yukarı doğru
    // aydınlanan turkuaz; üst kısımda yüzeyden süzülen hafif, yavaşça dalgalanan ışıltı. Sis uygulanmaz.
    Properties
    {
        _Bottom ("Ufuk Rengi", Color) = (0.012, 0.05, 0.085, 1)
        _Top ("Üst Renk", Color) = (0.05, 0.2, 0.3, 1)
        _Shimmer ("Işıltı Rengi", Color) = (0.25, 0.5, 0.6, 1)
        _ShimmerStrength ("Işıltı Gücü", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Backdrop"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Bottom;
                float4 _Top;
                float4 _Shimmer;
                float _ShimmerStrength;
            CBUFFER_END
            TEXTURE2D(_CausticTex); SAMPLER(sampler_CausticTex);
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert (A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; return o; }
            half4 frag (V i) : SV_Target
            {
                float t = saturate(i.uv.y);
                float g = smoothstep(0.0, 1.0, pow(t, 0.8));
                float3 col = lerp(_Bottom.rgb, _Top.rgb, g);
                float time = _Time.y;
                // Yüzeyden süzülen ışıltı: iki farklı yönde kayan desenin minimumu, yatayda uzatılmış
                float2 uv = float2(i.uv.x * 7.0, i.uv.y * 2.2);
                float c1 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, uv + float2(time * 0.012, -time * 0.02)).r;
                float c2 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, uv * 1.3 + float2(-time * 0.009, -time * 0.028)).r;
                float s = min(c1, c2) * smoothstep(0.35, 1.0, t);
                col += _Shimmer.rgb * s * _ShimmerStrength;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}