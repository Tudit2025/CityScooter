// CityScooter distant skyline layer: unlit, alpha blended, horizontally tiling/scrolling texture that fades into
// the scene haze colour (global _CS_HazeColor from ApplySceneLook, else _HazeColor). No fog, no lighting, no depth
// write. SkylineParallax drives _BaseMap, _BaseMap_ST (repeats + scroll), _BaseColor, _Haze and _BottomHaze through
// a MaterialPropertyBlock, so one material serves every layer.
Shader "CityScooter/Skyline Layer"
{
    Properties
    {
        [MainTexture] _BaseMap("Skyline (RGBA, tiles in X)", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _Haze("Haze Amount", Range(0, 1)) = 0.3
        _BottomHaze("Bottom Haze Height (0..1 of the layer)", Range(0, 1)) = 0.3
        _HazeColor("Haze Colour (when no Scene Look is active)", Color) = (0.81, 0.90, 0.96, 1)
        _ScrollX("Extra Scroll X", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-100" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "SkylineUnlit"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Haze;
                half _BottomHaze;
                half4 _HazeColor;
                float _ScrollX;
            CBUFFER_END

            // Set globally by ApplySceneLook; alpha 1 means "valid".
            half4 _CS_HazeColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half height01 : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw + float2(_ScrollX, 0.0);
                output.height01 = (half)input.uv.y;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 hazeColor = _CS_HazeColor.a > 0.5h ? _CS_HazeColor.rgb : _HazeColor.rgb;
                half bottom = 1.0h - smoothstep(0.0h, max(_BottomHaze, 0.001h), input.height01);
                half haze = saturate(lerp(_Haze, 1.0h, bottom));
                half3 color = lerp(tex.rgb * _BaseColor.rgb, hazeColor, haze);
                return half4(color, tex.a * _BaseColor.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
