Shader "Custom/ToonRegular"
{
    Properties
    {
        _BaseMap ("base texture", 2D) = "white" {}
        _PatternTexture ("edge pattern texture", 2D) = "gray" {}

        _ShadowThreshold ("shadow threshold", Range(0,1)) = 0.5
        _ShadowSmoothness ("shadow smoothness", Range(0.001,0.5)) = 0.1

        _PatternScale ("pattern scale", Float) = 5.0
        _PatternStrength ("pattern strength", Range(0,1)) = 0.3
        _PatternContrast ("pattern contrast", Range(0.1,5)) = 1.5

        _EdgeWidth ("edge width", Range(0.01,0.5)) = 0.15
        _EdgeSharpness ("edge sharpness", Range(0.5,10)) = 3.0

        _ScrollSpeed ("pattern scroll speed", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_PatternTexture);
            SAMPLER(sampler_PatternTexture);

            float4 _BaseMap_ST;

            float _ShadowThreshold;
            float _ShadowSmoothness;

            float _PatternScale;
            float _PatternStrength;
            float _PatternContrast;

            float _EdgeWidth;
            float _EdgeSharpness;

            float2 _ScrollSpeed;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings vert (Attributes input)
            {
                Varyings output;

                // convert object space to world space
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // convert world to clip space
                output.positionHCS = TransformWorldToHClip(output.positionWS);

                // world normal for lighting
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                // uv mapping
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            float CalculateToonLighting(
                float3 normalWS,
                float3 lightDirection,
                float attenuation,
                float2 uv,
                float time
            )
            {
                float ndotl = dot(normalWS, lightDirection);

                // basic toon ramp
                float shadowRamp = smoothstep(
                    _ShadowThreshold - _ShadowSmoothness,
                    _ShadowThreshold + _ShadowSmoothness,
                    ndotl
                );

                // edge band where pattern shows up
                float edgeBand = smoothstep(
                    _ShadowThreshold - _EdgeWidth,
                    _ShadowThreshold + _EdgeWidth,
                    ndotl
                );

                // shape it into a soft mask
                edgeBand = pow(edgeBand * (1.0 - edgeBand) * 4.0, _EdgeSharpness);

                // animated pattern uv
                float2 patternUv = uv * _PatternScale + (_ScrollSpeed * time);

                float patternValue = SAMPLE_TEXTURE2D(_PatternTexture, sampler_PatternTexture, patternUv).r;

                // boost pattern contrast
                patternValue = pow(patternValue, _PatternContrast);

                // slightly shift shadow threshold using pattern in edge zone
                float adjustedThreshold =
                    _ShadowThreshold +
                    (patternValue - 0.5) * _PatternStrength * edgeBand;

                float finalLighting = smoothstep(
                    adjustedThreshold - _ShadowSmoothness,
                    adjustedThreshold + _ShadowSmoothness,
                    ndotl
                );

                return finalLighting * attenuation;
            }

            float4 frag (Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);

                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                float3 totalLighting = 0;

                float time = _Time.y;

                // main directional light
                Light mainLight = GetMainLight();
                totalLighting += CalculateToonLighting(
                    normalWS,
                    mainLight.direction,
                    mainLight.distanceAttenuation,
                    input.uv,
                    time
                ) * mainLight.color;

                // extra lights in scene
                #ifdef _ADDITIONAL_LIGHTS

                int additionalLightCount = GetAdditionalLightsCount();

                for (int i = 0; i < additionalLightCount; i++)
                {
                    Light additionalLight = GetAdditionalLight(i, input.positionWS);

                    totalLighting += CalculateToonLighting(
                        normalWS,
                        additionalLight.direction,
                        additionalLight.distanceAttenuation,
                        input.uv,
                        time
                    ) * additionalLight.color;
                }

                #endif

                float3 finalColor = baseColor.rgb * totalLighting;

                return float4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }
}