Shader "Custom/UI_UnscaledAnimatedOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineGapColor ("Outline Gap Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness", Float) = 2.0
        _OutlineDot ("Dot Spacing", Float) = 10.0
        _OutlineDot2 ("Dot Gap", Float) = 0.5
        _OutlineSpeed ("Dot Scroll Speed", Float) = 1.0

        _UnscaledTime ("Unscaled Time", Float) = 0
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
            Name "UI_AnimatedOutline"
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
            float4 _OutlineGapColor;
            float _OutlineThickness;
            float _OutlineDot;
            float _OutlineDot2;
            float _OutlineSpeed;
            float _UnscaledTime;

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
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;

                if (col.a > 0.01)
                    return col;

                float2 uv = i.uv;

                float2 offsets[8] = {
                    float2(-1,0), float2(1,0),
                    float2(0,-1), float2(0,1),
                    float2(-1,-1), float2(-1,1),
                    float2(1,-1), float2(1,1)
                };

                float2 texelSize = _OutlineThickness / float2(512,512); 
                float maxNeighborAlpha = 0.0;

                for(int k=0;k<8;k++)
                {
                    float2 uvOff = uv + offsets[k]*texelSize;
                    float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvOff).a;
                    maxNeighborAlpha = max(maxNeighborAlpha, a);
                }

                if (maxNeighborAlpha < 0.01)
                    discard;

                float2 pos = uv * _OutlineDot + _UnscaledTime * _OutlineSpeed;

                float pattern = sin(_OutlineDot * (pos.x + pos.y)) + _OutlineDot2;

                if (pattern >= 0.5)
                    return float4(_OutlineColor.rgb, _OutlineColor.a);
                else
                    return float4(_OutlineGapColor.rgb, _OutlineGapColor.a);
            }

            ENDHLSL
        }
    }
}
