Shader "CityScooter/Comic Panorama Sky"
{
    Properties
    {
        [NoScaleOffset] _ComicSkyTexture("Perth comic panorama", 2D) = "grey" {}
        _ComicSkyMapping("Yaw, elevation, scale, enabled",Vector)=(0,.279253,.45,1)
        _ComicSkyScale("Apparent skyline size",Range(.4,1))=.7
        _ComicSkyHaze("Atmospheric tint / strength",Color)=(.60,.74,.83,.12)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ComicPanorama.hlsl"
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            V Vert(A i) { V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.direction=i.positionOS.xyz;return o; }
            half4 Frag(V i):SV_Target { return half4(ComicPanorama(i.direction),1); }
            ENDHLSL
        }
    }
    Fallback Off
}
