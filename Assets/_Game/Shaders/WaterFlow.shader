Shader "StratejiGame/WaterFlow"
{
    // Akan su tabakası (şelale, taş bloktan dökülen su). UV.y akış yönü; kostik dokusu
    // uzatılıp kaydırılarak akış çizgileri üretilir. Alt uçta köpük. Saydam, mobil uyumlu.
    Properties
    {
        _BaseColor ("Su Rengi", Color) = (0.45, 0.8, 1, 0.55)
        _FoamColor ("Köpük Rengi", Color) = (0.9, 0.98, 1, 1)
        _FlowSpeed ("Akış Hızı", Float) = 1.2
        _Stretch ("Çizgi Uzunluğu", Float) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _FoamColor;
                float _FlowSpeed;
                float _Stretch;
            CBUFFER_END
            TEXTURE2D(_CausticTex); SAMPLER(sampler_CausticTex);
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; float4 col : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 col : COLOR; float fog : TEXCOORD1; };
            V vert (A i)
            {
                UNITY_SETUP_INSTANCE_ID(i);
                V o;
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.uv = i.uv;
                o.col = i.col;
                o.fog = ComputeFogFactor(o.pos.z);
                return o;
            }
            half4 frag (V i) : SV_Target
            {
                float t = _Time.y * _FlowSpeed;
                float2 uv = float2(i.uv.x, i.uv.y * _Stretch);
                float s1 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, uv + float2(0, -t * 0.35)).r;
                float s2 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, uv * float2(1.7, 1.3) + float2(0.37, -t * 0.5)).r;
                float streak = saturate(s1 + s2 * 0.7);
                // Köşe rengi: rgb = ton, a = köpük miktarı (şelale dibi, döküldüğü kenar)
                float foam = saturate(i.col.a + streak * i.col.a);
                float3 col = lerp(_BaseColor.rgb * i.col.rgb, _FoamColor.rgb, saturate(streak * 0.4 + foam * 0.45));
                float alpha = saturate(_BaseColor.a * (0.55 + streak * 0.9) + foam * 0.3);
                col = MixFog(col, i.fog);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}