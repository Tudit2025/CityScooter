Shader "CityScooter/Comic Outline"
{
    Properties
    {
        _InkColor("Silhouette ink", Color) = (0.012,0.014,0.022,1)
        _OutlinePixels("Width at 900p", Range(0,4)) = 1.8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "InkSilhouette"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ComicWorldBend.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _InkColor;
                float _OutlinePixels;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 smoothNormalOS:TEXCOORD1; };
            struct V { float4 positionCS:SV_POSITION; half fog:TEXCOORD0; };
            V Vert(A i)
            {
                V o;
                float3 originalWS=TransformObjectToWorld(i.positionOS.xyz);
                float3 positionWS=ComicBendPosition(originalWS,1);
                float4 positionCS=TransformWorldToHClip(positionWS);
                float3 normalWS=ComicBendNormal(originalWS,TransformObjectToWorldNormal(i.smoothNormalOS),1);
                float2 projected=mul(UNITY_MATRIX_VP,float4(normalWS,0)).xy*_ScreenParams.xy;
                float norm=max(length(projected),0.0001);
                float fade=lerp(1,0.22,smoothstep(25,150,distance(_WorldSpaceCameraPos,positionWS)));
                float pixels=_OutlinePixels*_ScreenParams.y/900.0*fade;
                pixels*=1-smoothstep(180,320,originalWS.z-_ComicWorldBend.x);
                o.positionCS=positionCS;
                o.positionCS.xy+=projected/norm*(2*pixels/_ScreenParams.xy)*positionCS.w;
                o.fog=ComputeFogFactor(positionCS.z);
                return o;
            }
            half4 Frag(V i):SV_Target { return half4(MixFog(_InkColor.rgb,i.fog),1); }
            ENDHLSL
        }
    }
}
