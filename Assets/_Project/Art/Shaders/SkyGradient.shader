// CityScooter cheap gradient skybox. One pass, at most one texture fetch.
//  - Gradient: either a vertical sky strip from the skyline art (Use Sky Strip) or procedural horizon -> zenith colours.
//    The strip is mapped with the same angles as the skyline layers: SkylineParallax publishes _CS_SkyMapping
//    (tan of the horizon angle, tan of the top-row angle), so the sky stays lined up with the city silhouettes.
//    Without a rig the material's own angles are used.
//  - Warm glow low in the sky on the sun's side, plus a soft sun disc and halo that follow the main light.
// The horizon colour should equal the fog colour: the strip is painted that way, and the procedural mode can take
// the fog colour straight from ApplySceneLook (_CS_FogColor).
Shader "CityScooter/Sky Gradient"
{
    Properties
    {
        [Header(Sky strip from the skyline art)]
        [Toggle] _UseGradientTex("Use Sky Strip", Float) = 0
        [NoScaleOffset] _GradientTex("Sky Strip (vertical gradient, top = up)", 2D) = "white" {}
        _GradientHorizon("Strip Horizon (fraction from the top)", Range(0.3, 1)) = 0.845
        _GradientHorizonAngle("Horizon Angle without a rig (deg)", Range(-5, 5)) = -0.4
        _GradientTopAngle("Strip Top Angle without a rig (deg)", Range(5, 60)) = 18

        [Header(Procedural gradient)]
        _ZenithColor("Zenith (also above the strip)", Color) = (0.37, 0.62, 0.84, 1)
        _HorizonColor("Horizon", Color) = (0.957, 0.89, 0.773, 1)
        [Toggle] _HorizonFromFog("Horizon = Scene Look Fog Colour", Float) = 1
        _BelowHorizonColor("Below Horizon", Color) = (0.957, 0.83, 0.655, 1)
        _GradientPower("Gradient Power (lower = blue comes further down)", Range(0.1, 3)) = 0.55
        _HorizonBlend("Below-Horizon Blend Width", Range(0.01, 1)) = 0.15

        [Header(Warm glow on the sun side)]
        _SunGlowColor("Glow Colour", Color) = (1, 0.88, 0.7, 1)
        _SunGlowAmount("Glow Amount", Range(0, 1)) = 0.2
        _SunGlowPower("Glow Tightness", Range(0.5, 16)) = 3
        _SunGlowHeight("Glow Height", Range(0.05, 1)) = 0.35

        [Header(Sun disc)]
        _SunDiscColor("Disc Colour", Color) = (1, 0.97, 0.88, 1)
        _SunDiscIntensity("Disc Intensity", Range(0, 4)) = 1.6
        _SunSize("Disc Radius (radians)", Range(0.005, 0.2)) = 0.035
        _SunSoftness("Disc Edge Softness", Range(0.001, 0.1)) = 0.012
        _SunHaloColor("Halo Colour", Color) = (1, 0.95, 0.82, 1)
        _SunHaloAmount("Halo Amount", Range(0, 1)) = 0.25
        _SunHaloPower("Halo Tightness", Range(1, 256)) = 24

        [Header(Sun direction)]
        [Toggle] _UseMainLight("Follow Main Light", Float) = 1
        _SunDirection("Fallback Direction (towards the sun)", Vector) = (-0.4, 0.7, -0.6, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SkyGradient"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_GradientTex);
            SAMPLER(sampler_GradientTex);

            CBUFFER_START(UnityPerMaterial)
                half _UseGradientTex;
                float _GradientHorizon;
                float _GradientHorizonAngle;
                float _GradientTopAngle;
                half4 _ZenithColor;
                half4 _HorizonColor;
                half _HorizonFromFog;
                half4 _BelowHorizonColor;
                half _GradientPower;
                half _HorizonBlend;
                half4 _SunGlowColor;
                half _SunGlowAmount;
                half _SunGlowPower;
                half _SunGlowHeight;
                half4 _SunDiscColor;
                half _SunDiscIntensity;
                float _SunSize;
                float _SunSoftness;
                half4 _SunHaloColor;
                half _SunHaloAmount;
                half _SunHaloPower;
                half _UseMainLight;
                float4 _SunDirection;
            CBUFFER_END

            // Globals: SkylineParallax (x = tan horizon, y = tan top row, w = 1 when set) and ApplySceneLook (alpha 1 = set).
            float4 _CS_SkyMapping;
            half4 _CS_FogColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 directionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.directionWS = input.positionOS.xyz; // the skybox mesh is centred on the camera
                return output;
            }

            half3 ProceduralSky(float3 dir)
            {
                half3 horizon = (_HorizonFromFog > 0.5 && _CS_FogColor.a > 0.5) ? _CS_FogColor.rgb : _HorizonColor.rgb;
                float h = dir.y;
                half up = (half)pow(saturate(h), _GradientPower);
                half3 sky = lerp(horizon, _ZenithColor.rgb, up);
                return lerp(sky, _BelowHorizonColor.rgb, (half)saturate(-h / _HorizonBlend));
            }

            half3 StripSky(float3 dir)
            {
                // Same vertical mapping as the skyline layers (a cylinder: rows are linear in tan(elevation)).
                float2 mapping = _CS_SkyMapping.w > 0.5
                    ? _CS_SkyMapping.xy
                    : float2(tan(radians(_GradientHorizonAngle)), tan(radians(_GradientTopAngle)));
                float tanElevation = dir.y / max(length(dir.xz), 1e-4);
                float rowFromTop = _GradientHorizon * (mapping.y - tanElevation) / max(mapping.y - mapping.x, 1e-4);
                half3 sky = SAMPLE_TEXTURE2D_LOD(_GradientTex, sampler_GradientTex, float2(0.5, 1.0 - saturate(rowFromTop)), 0).rgb;
                // Above the strip: keep going into the zenith colour (linear, so the gradient does not flatten into a band).
                float sinTop = mapping.y * rsqrt(1.0 + mapping.y * mapping.y);
                half above = (half)saturate((dir.y - sinTop) / max(1.0 - sinTop, 1e-3));
                return lerp(sky, _ZenithColor.rgb, above);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.directionWS);
                float3 sunDir = normalize(_UseMainLight > 0.5 ? _MainLightPosition.xyz : _SunDirection.xyz);

                half3 sky = _UseGradientTex > 0.5 ? StripSky(dir) : ProceduralSky(dir);

                // Warm afternoon glow low in the sky on the sun's side.
                float2 dirXZ = normalize(dir.xz + 1e-5);
                float2 sunXZ = normalize(sunDir.xz + 1e-5);
                half towardSun = (half)saturate(dot(dirXZ, sunXZ) * 0.5 + 0.5);
                half lowSky = (half)(1.0 - saturate(abs(dir.y) / _SunGlowHeight));
                half glow = pow(towardSun, _SunGlowPower) * lowSky * lowSky * _SunGlowAmount;
                sky = lerp(sky, _SunGlowColor.rgb, glow);

                // Sun disc and halo (float: tiny angles need the precision).
                float cosAngle = dot(dir, sunDir);
                float angle = sqrt(saturate(2.0 - 2.0 * cosAngle)); // chord length ~ angle in radians near the sun
                half disc = (half)(1.0 - smoothstep(_SunSize, _SunSize + _SunSoftness, angle));
                half halo = (half)pow(saturate(cosAngle), _SunHaloPower) * _SunHaloAmount;
                sky += _SunHaloColor.rgb * halo;
                sky = lerp(sky, _SunDiscColor.rgb * _SunDiscIntensity, disc);

                return half4(sky, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
