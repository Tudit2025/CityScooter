Shader "CityScooter/Comic Vertex"
{
    Properties
    {
        _BaseColor("Tint", Color) = (1,1,1,1)
        _ShadowTint("Ink shadow tint", Color) = (0.48,0.57,0.73,1)
        _Halftone("Shadow halftone", Range(0,0.3)) = 0.06
        _FogOverride("Distant silhouette visibility", Range(0,1)) = 0
        _InkColor("Black crease ink", Color) = (0.012,0.014,0.022,1)
        _CreasePixels("Crease width at 900p", Range(0,2)) = 0.65
        _WorldBend("Follow city curvature", Float) = 1
        _HorizonExtension("Distant road strip", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "ComicWorldBend.hlsl"
        #include "ComicPanorama.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _ShadowTint;
            half _Halftone;
            half _FogOverride;
            half4 _InkColor;
            float _CreasePixels;
            float _WorldBend;
            float _HorizonExtension;
        CBUFFER_END
        ENDHLSL
        Pass
        {
            Name "ComicForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; float3 inkUV:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half4 color:COLOR; half fog:TEXCOORD2; float3 inkUV:TEXCOORD3; };
            V Vert(A i)
            {
                V o;
                float3 originalWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionWS = ComicBendPosition(originalWS, _WorldBend);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = ComicBendNormal(originalWS, TransformObjectToWorldNormal(i.normalOS), _WorldBend);
                o.color = i.color * _BaseColor; o.fog = ComputeFogFactor(o.positionCS.z);
                o.inkUV = i.inkUV;
                return o;
            }
            half4 Frag(V i):SV_Target
            {
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(normalize(i.normalWS), sun.direction));
                half band = ndl > 0.62 ? 1 : (ndl > 0.18 ? 0.62 : 0.18);
                band *= lerp(0.28, 1, step(0.5, sun.shadowAttenuation));
                half3 lighting = lerp(_ShadowTint.rgb, half3(1.10,1.06,0.98), band);
                float2 dots = frac(i.positionCS.xy / (4.0 * _ScreenParams.y / 900.0)) - 0.5;
                half ink = (1-smoothstep(0.14,0.23,length(dots))) * (1 - band) * _Halftone;
                half3 col = i.color.rgb * lighting * max(half3(0.8,0.8,0.8), sun.color) * (1 - ink);
                float2 uvWidth=max(fwidth(i.inkUV.xy),float2(0.00001,0.00001));
                float2 edgePixels=min(i.inkUV.xy,1-i.inkUV.xy)/uvWidth;
                float width=_CreasePixels*_ScreenParams.y/900.0;
                half creaseMask=(1-smoothstep(max(0,width-0.35),width+0.35,min(edgePixels.x,edgePixels.y)))*i.inkUV.z;
                // Thin distant details merge into the color blocks instead of turning black.
                creaseMask*=1-smoothstep(30,140,distance(_WorldSpaceCameraPos,i.positionWS));
                creaseMask*=step(0.01,_CreasePixels);
                col=lerp(col,_InkColor.rgb,creaseMask);
                half3 fogged=lerp(MixFog(col,i.fog),col,_FogOverride);
                if(_ComicSkyMapping.w>.5)
                {
                    float distanceZ=i.positionWS.z-_ComicWorldBend.x;
                    half3 sky=ComicPanorama(i.positionWS-_WorldSpaceCameraPos);
                    // Preserve the fine road taper while merging distant facades into the actual image sky.
                    float farFade=smoothstep(220,360,distanceZ);
                    if(_HorizonExtension>.5)farFade=smoothstep(310,_ComicHorizon.y+30,distanceZ);
                    fogged=lerp(fogged,sky,farFade);
                }
                return half4(fogged, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct ShadowInput { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            float4 ShadowVert(ShadowInput i):SV_POSITION
            {
                float3 originalWS=TransformObjectToWorld(i.positionOS.xyz);
                float3 positionWS=ComicBendPosition(originalWS,_WorldBend);
                float3 normalWS=ComicBendNormal(originalWS,TransformObjectToWorldNormal(i.normalOS),_WorldBend);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS=normalize(_LightPosition-positionWS);
                #else
                    float3 lightDirectionWS=_LightDirection;
                #endif
                return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS,normalWS,lightDirectionWS)));
            }
            half4 ShadowFrag():SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            float4 DepthVert(float4 positionOS:POSITION):SV_POSITION { return TransformWorldToHClip(ComicBendPosition(TransformObjectToWorld(positionOS.xyz),_WorldBend)); }
            half DepthFrag():SV_Target { return 0; }
            ENDHLSL
        }
    }
}
