Shader "Custom/ToonVertex"
{
    Properties
    {
        // textures
        _BaseMap ("base texture", 2D) = "white" {}
        _PatternTexture ("edge pattern texture", 2D) = "gray" {}



        // toon shading controls
        _ShadowThreshold ("shadow threshold", Range(0,1)) = 0.5
        _ShadowSmoothness ("shadow smoothness", Range(0.001,0.5)) = 0.1

        // pattern stuff for edges
        _PatternScale ("pattern scale", Float) = 5.0
        _PatternStrength ("pattern strength", Range(0,1)) = 0.3
        _PatternContrast ("pattern contrast", Range(0.1,5)) = 1.5



        // edge shaping
        _EdgeWidth ("edge width", Range(0.01,0.5)) = 0.15
        _EdgeSharpness ("edge sharpness", Range(0.5,10)) = 3.0



        
        // uv scroll
        _ScrollSpeed ("pattern scroll speed", Vector) = (0,0,0,0)



        // vertex wobble / snap stuff
        _VertexSnapStrength ("vertex snap strength", Range(0,1)) = 0.2
        _VertexSnapScale ("vertex snap noise scale", Float) = 3.0
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



            float _VertexSnapStrength;
            float _VertexSnapScale;



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



            // simple stable hash for vertex noise
            float Hash33(float3 position)
            {
                position = frac(position * 0.1031);
                position += dot(position, position.yzx + 33.33);
                return frac((position.x + position.y) * position.z);
            }



            Varyings vert (Attributes input)
            {
                Varyings output;

                float3 worldPosition = TransformObjectToWorld(input.positionOS.xyz);
                float3 worldNormal = TransformObjectToWorldNormal(input.normalOS);

                float4 clipPosition = TransformWorldToHClip(worldPosition);



                // screen space coords (used for snapping)
                float2 ndc = clipPosition.xy / clipPosition.w;



                // how tight the vertex snapping feels
                float snapAmount = lerp(1.0, 80.0, _VertexSnapStrength);

                ndc = floor(ndc * snapAmount) / snapAmount;



                // tiny stable per-vertex noise so it doesnt look too grid-like
                float noise = Hash33(worldPosition * _VertexSnapScale);
                ndc += (noise - 0.5) * (1.0 / snapAmount);



                clipPosition.xy = ndc * clipPosition.w;



                output.positionHCS = clipPosition;
                output.positionWS = worldPosition;
                output.normalWS = worldNormal;

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }



            float CalculateLighting(
                float3 normalWS,
                float3 lightDirection,
                float attenuation,
                float2 uv,
                float time
            )
            {
                float ndotl = dot(normalWS, lightDirection);



                // toon ramp
                float shadowRamp = smoothstep(
                    _ShadowThreshold - _ShadowSmoothness,
                    _ShadowThreshold + _ShadowSmoothness,
                    ndotl
                );



                // edge band where pattern kicks in
                float edgeMask = smoothstep(
                    _ShadowThreshold - _EdgeWidth,
                    _ShadowThreshold + _EdgeWidth,
                    ndotl
                );

                edgeMask = pow(edgeMask * (1.0 - edgeMask) * 4.0, _EdgeSharpness);



                float2 patternUv = uv * _PatternScale + (_ScrollSpeed * time);

                float patternValue = SAMPLE_TEXTURE2D(
                    _PatternTexture,
                    sampler_PatternTexture,
                    patternUv
                ).r;

                patternValue = pow(patternValue, _PatternContrast);



                // pattern slightly shifts shadow cutoff in edge zone
                float adjustedThreshold =
                    _ShadowThreshold +
                    (patternValue - 0.5) * _PatternStrength * edgeMask;



                float finalRamp = smoothstep(
                    adjustedThreshold - _ShadowSmoothness,
                    adjustedThreshold + _ShadowSmoothness,
                    ndotl
                );

                return finalRamp * attenuation;
            }



            float4 frag (Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);

                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                float3 lightingResult = 0;

                float time = _Time.y;



                // main light
                Light mainLight = GetMainLight();

                lightingResult += CalculateLighting(
                    normalWS,
                    mainLight.direction,
                    mainLight.distanceAttenuation,
                    input.uv,
                    time
                ) * mainLight.color;



                // additional lights
                #ifdef _ADDITIONAL_LIGHTS

                int additionalLightCount = GetAdditionalLightsCount();

                for (int i = 0; i < additionalLightCount; i++)
                {
                    Light additionalLight = GetAdditionalLight(i, input.positionWS);

                    lightingResult += CalculateLighting(
                        normalWS,
                        additionalLight.direction,
                        additionalLight.distanceAttenuation,
                        input.uv,
                        time
                    ) * additionalLight.color;
                }

                #endif



                float3 finalColor = baseColor.rgb * lightingResult;

                return float4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }
}