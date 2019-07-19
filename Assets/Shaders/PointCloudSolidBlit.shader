/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/PointCloud/SolidBlit"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" }

        Pass
        {
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            Texture2D _ColorTex;
            SamplerState sampler_ColorTex;
            float4 _ColorTex_TexelSize;

            Texture2D _MaskTex;

            int _DebugLevel;

            struct v2f
            {
                float4 Position : SV_Position;
                float2 TexCoord : TEXCOORD;
            };

            v2f Vert(uint id : SV_VertexID)
            {
                // https://www.slideshare.net/DevCentralAMD/vertex-shader-tricks-bill-bilodeau

                v2f Output;

                Output.Position.x = (float)(id / 2) * 4 - 1;
                Output.Position.y = (float)(id % 2) * 4 - 1;
                Output.Position.z = 0;
                Output.Position.w = 1;

                Output.TexCoord.x = (float)(id / 2) * 2;
                Output.TexCoord.y = (float)(id % 2) * 2;

                Output.TexCoord.xy *= _ColorTex_TexelSize.xy * _ScreenParams.xy / float(1 << _DebugLevel);

                return Output;
            }

            struct FragOutput
            {
                float4 Color : SV_Target;
                float Depth : SV_Depth;
            };

            FragOutput Frag(v2f Input)
            {
                int2 uv = int2(Input.TexCoord * _ColorTex_TexelSize.zw);
                float4 pix = _ColorTex.Load(int3(uv, _DebugLevel));
                if (pix.a <= 0)
                {
                    discard;
                }

                // float z = pix.a;
                // float4 clip = UnityViewToClipPos(float3(0, 0, -z));

                 //pix.r = _MaskTex.Load(int3(uv, 0)).r == 1 ? 0 : 1;
                 //pix.g = 0; // pix.a == 0 ? 0 : 1;
                 //pix.b = 0;
                 pix.a = 1;

                FragOutput Output;
                Output.Color = pix;
                Output.Depth = 0.5; // TODO clip.z / clip.w;
                return Output;
            }

            ENDHLSL
        }
    }
}
