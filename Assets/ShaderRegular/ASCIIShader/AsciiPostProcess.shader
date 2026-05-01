Shader "Custom/URP/AsciiObject_Modular_Final"
{
    Properties
    {
        // ascii atlas texture
        _AsciiTex ("ASCII Atlas", 2D) = "white" {}

        // atlas layout + density
        _Columns ("Columns", Float) = 10
        _Rows ("Rows", Float) = 10
        _Zoom ("Density", Range(0.1, 100)) = 10


        // random character selection
        _Seed ("Random Seed", Float) = 1
        _RandomStrength ("Random Strength", Range(0,1)) = 1


        // reshuffles the characters over time
        _ShuffleSpeed ("Shuffle Speed (seconds)", Range(0,10)) = 0


        // screen space wave distortion
        _WaveStrength ("Wave Strength", Range(0,2)) = 0.3
        _WaveSpeed ("Wave Speed", Range(0,10)) = 2


        // base tint settings
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ColorVariation ("Per-Character Variation", Range(0,1)) = 0.5


        // optional gradient coloring
        _UseGradient ("Use Gradient Mode", Float) = 0
        _GradientTex ("Gradient Texture", 2D) = "white" {}
        _GradientStrength ("Gradient Strength", Range(0,1)) = 1
    }


    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }


        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back


            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            // textures
            TEXTURE2D(_AsciiTex);
            SAMPLER(sampler_AsciiTex);

            TEXTURE2D(_GradientTex);
            SAMPLER(sampler_GradientTex);


            // atlas settings
            float _Columns;
            float _Rows;
            float _Zoom;


            // randomness
            float _Seed;
            float _RandomStrength;


            // animation
            float _ShuffleSpeed;

            float _WaveStrength;
            float _WaveSpeed;


            // colors
            float4 _BaseColor;
            float _ColorVariation;

            float _UseGradient;
            float _GradientStrength;


            struct Attributes
            {
                float4 positionOS : POSITION;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };


            Varyings vert(Attributes v)
            {
                Varyings o;

                float4 clip = TransformObjectToHClip(v.positionOS.xyz);

                o.positionHCS = clip;
                o.screenPos = ComputeScreenPos(clip);

                return o;
            }


            // simple random hash based on cell position
            float hash21(float2 p, float seed)
            {
                p += seed;

                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);

                return frac(p.x * p.y);
            }


            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;


                // reshuffles the ascii pattern every few seconds
                float timeStep = (_ShuffleSpeed > 0.001)
                    ? floor(_Time.y / _ShuffleSpeed)
                    : 0;

                float seed = _Seed + timeStep * 100.0;


                // layered sine waves for distortion
                float wave =
                    sin(uv.x * 6.2831 * 2.0 + _Time.y * _WaveSpeed) * 0.5 +
                    sin(uv.y * 6.2831 * 1.5 + _Time.y * _WaveSpeed * 1.2) * 0.5;

                uv.y += wave * _WaveStrength;


                // split the screen into ascii cells
                float2 gridSize = float2(_Columns, _Rows) * _Zoom;

                float2 uvGrid = uv * gridSize;

                float2 cell = floor(uvGrid);
                float2 f = frac(uvGrid);


                // random character index per cell
                float rnd = hash21(cell, seed);

                rnd = lerp(0.5, rnd, _RandomStrength);

                float charIndex = floor(rnd * (_Columns * _Rows));



                // convert index into atlas uv coords
                float2 atlasUV;

                atlasUV.x = (fmod(charIndex, _Columns) + f.x) / _Columns;
                atlasUV.y = (floor(charIndex / _Columns) + f.y) / _Rows;


                float4 ascii = SAMPLE_TEXTURE2D(_AsciiTex, sampler_AsciiTex, atlasUV);

                float alpha = ascii.a;


                // base color
                float3 color = _BaseColor.rgb;


                // slight color variation per character
                float3 randomColorOffset = (
                    float3(
                        hash21(cell + 1.1, seed),
                        hash21(cell + 2.2, seed),
                        hash21(cell + 3.3, seed)
                    ) - 0.5
                ) * _ColorVariation;

                color += randomColorOffset;



                // optional gradient coloring mode
                if (_UseGradient > 0.5)
                {
                    float gradientPos = saturate(uv.y * _GradientStrength + rnd * 0.1);

                    color = SAMPLE_TEXTURE2D(
                        _GradientTex,
                        sampler_GradientTex,
                        float2(gradientPos, 0.5)
                    ).rgb;
                }

                return float4(color, alpha);
            }

            
            ENDHLSL
        }
    }
}