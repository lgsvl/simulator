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
                float4 pix = _ColorTex.Load(int3(int2(Input.TexCoord * _ColorTex_TexelSize.zw), _DebugLevel));
                if (pix.a <= 0)
                {
                    discard;
                }

                // TODO: figure out how to remap from "distance to camera" to "distance to projection plane" here
                // See fragment shader in PointCloudSolidRender
                float z = pix.a;

                float4 clip = UnityViewToClipPos(float3(0, 0, -z));

                FragOutput Output;
                Output.Color = float4(pix.rgb, 1);
                Output.Depth = clip.z / clip.w;
                return Output;
            }

            ENDHLSL
        }
    }
}
