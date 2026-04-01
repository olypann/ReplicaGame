Shader "Custom/UI_ImageOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Float) = 1.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        LOD 100

        Pass
        {
            Name "UI_ImageOutline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _Color;
            float4 _OutlineColor;
            float _OutlineThickness;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                
                float4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;

                
                if(texCol.a > 0.01){
                    return texCol;
                }

                
                float2 offsets[8] = {
                    float2(-1,0), float2(1,0),
                    float2(0,-1), float2(0,1),
                    float2(-1,-1), float2(-1,1),
                    float2(1,-1), float2(1,1)
                };

                float2 texelSize = _OutlineThickness / float2(512,512);
                float outlineAlpha = 0.0;

                for(int k=0;k<8;k++)
                {
                    float2 uvOffset = i.uv + offsets[k]*texelSize;
                    float sampleAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvOffset).a;
                    outlineAlpha = max(outlineAlpha, sampleAlpha);
                }

                return float4(_OutlineColor.rgb, outlineAlpha*_OutlineColor.a);
            }

            ENDHLSL
        }
    }
}
