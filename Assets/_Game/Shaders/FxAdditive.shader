Shader "StratejiGame/FxAdditive"
{
    // Namlu alevi, mermi izi gibi parlayan efektler: köşe rengi x _BaseColor, eklemeli (additive) karışım.
    Properties
    {
        _BaseColor ("Renk", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "FxAdd"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
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
            CBUFFER_END
            struct A { float4 pos : POSITION; float4 col : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos : SV_POSITION; float4 col : COLOR; float fog : TEXCOORD0; };
            V vert (A i)
            {
                UNITY_SETUP_INSTANCE_ID(i);
                V o;
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.col = i.col * _BaseColor;
                o.fog = ComputeFogFactor(o.pos.z);
                return o;
            }
            half4 frag (V i) : SV_Target
            {
                // Eklemeli parlamayı sisle birlikte söndür (uzaktaki ışıklar kaybolsun)
                float3 c = MixFogColor(i.col.rgb * i.col.a, float3(0, 0, 0), i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}