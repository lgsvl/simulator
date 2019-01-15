/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "LidarShader"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }

    CGINCLUDE
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 viewPos : TEXCOORD1;
        };

        sampler2D _MainTex;
        float4 _MainTex_ST;

        v2f vert(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            o.viewPos = UnityObjectToViewPos(v.vertex);
            return o;
        }

        void AlphaCutoff(float alpha);

        float2 frag(v2f i) : SV_Target
        {
            float4 color = tex2D(_MainTex, i.uv);
            AlphaCutoff(color.a);

            float depth = length(i.viewPos);
            //float depth = LinearEyeDepth(i.vertex.z);

            float intensity = (color.r + color.g + color.b) / 3;

            return float2(depth, intensity);
        }
    ENDCG

    SubShader
    {
        Tags{ "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            void AlphaCutoff(float alpha)
            {
            }
            ENDCG
        }
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" }

        Pass
        {
            CGPROGRAM
            float _Cutoff;
            void AlphaCutoff(float alpha)
            {
                if (alpha < _Cutoff)
                {
                    discard;
                }
            }
            ENDCG
        }
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" }

        Pass
        {
            CGPROGRAM
            float _Cutoff;
            void AlphaCutoff(float alpha)
            {
                if (alpha < _Cutoff)
                {
                    discard;
                }
            }
            ENDCG
        }
    }
}
