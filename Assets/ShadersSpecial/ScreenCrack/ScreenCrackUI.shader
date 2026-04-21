Shader "UI/ScreenCrack_Overlay"
{
    Properties
    {
        _CrackTex ("Crack Texture", 2D) = "white" {}
        _Damage ("Damage", Range(0,10)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _CrackTex;
            float _Damage;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 crack = tex2D(_CrackTex, i.uv);

                float dmg = saturate(_Damage / 10.0);

                float2 grid = i.uv * 6.0;

                float cell = hash(floor(grid));

                float reveal = step(cell, max(dmg, 0.000001));

                //cracks appear progressively
                crack.a *= reveal;

                return crack * i.color;
            }
            ENDCG
        }
    }
}