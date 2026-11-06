Shader "CityScooter/Comic City Sky"
{
    Properties { _ShadeStrength("Graphic face shading", Range(0,1)) = 0.25 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half _ShadeStrength;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; half3 color:TEXCOORD0; };
            V Vert(A i)
            {
                V o;
                o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                float n=dot(TransformObjectToWorldNormal(i.normalOS),normalize(float3(-.4,.8,-.5)));
                o.color=i.color.rgb*(1-_ShadeStrength*(n>.4?0:n>-.2?.4:1));
                return o;
            }
            half4 Frag(V i):SV_Target { return half4(i.color,1); }
            ENDHLSL
        }
    }
}
