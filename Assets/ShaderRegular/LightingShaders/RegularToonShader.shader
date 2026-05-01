Shader "Custom/ToonRegular"
{
    Properties
    {
        // base textures
        _BaseMap ("base texture", 2D) = "white" {}
        _PatternTexture ("edge pattern texture", 2D) = "gray" {}


        // toon shadow control
        _ShadowThreshold ("shadow threshold", Range(0,1)) = 0.5
        _ShadowSmoothness ("shadow smoothness", Range(0.001,0.5)) = 0.1

        // edge pattern stuff
        _PatternScale ("pattern scale", Float) = 5.0
        _PatternStrength ("pattern strength", Range(0,1)) = 0.3
        _PatternContrast ("pattern contrast", Range(0.1,5)) = 1.5


        
        // outline-ish band shaping
        _EdgeWidth ("edge width", Range(0.01,0.5)) = 0.15
        _EdgeSharpness ("edge sharpness", Range(0.5,10)) = 3.0


        // scrolling pattern offset
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

                // object -> world
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // world -> clip
                output.positionHCS = TransformWorldToHClip(output.positionWS);



                // normal in world space for lighting
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                // uv mapping
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }


            // shared lighting function for main + additional lights
            float CalculateToonLighting(
                float3 normalWS,
                float3 lightDirection,
                float attenuation,
                float2 uv,
                float time
            )
            {
                float ndotl = dot(normalWS, lightDirection);


                // basic toon shadow ramp
                float shadowRamp = smoothstep(
                    _ShadowThreshold - _ShadowSmoothness,
                    _ShadowThreshold + _ShadowSmoothness,
                    ndotl
                );


                // band where pattern shows up near shadow edge
                float edgeBand = smoothstep(
                    _ShadowThreshold - _EdgeWidth,
                    _ShadowThreshold + _EdgeWidth,
                    ndotl
                );

                edgeBand = pow(edgeBand * (1.0 - edgeBand) * 4.0, _EdgeSharpness);


                // animated pattern lookup
                float2 patternUv = uv * _PatternScale + (_ScrollSpeed * time);

                float patternValue = SAMPLE_TEXTURE2D(
                    _PatternTexture,
                    sampler_PatternTexture,
                    patternUv
                ).r;

                patternValue = pow(patternValue, _PatternContrast);


                // pattern slightly shifts shadow threshold in edge zones
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



                // main light
                Light mainLight = GetMainLight();

                totalLighting += CalculateToonLighting(
                    normalWS,
                    mainLight.direction,
                    mainLight.distanceAttenuation,
                    input.uv,
                    time
                ) * mainLight.color;



                // extra lights
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