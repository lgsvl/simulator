/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/PointCloud/SolidRender"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "PointCloudCommon.hlsl"

            float4x4 _PointCloudMVP;

            struct v2f
            {
                float4 Position : SV_POSITION;
                // float2 Depth : DEPTH;
                nointerpolation float4 Color : COLOR;
                nointerpolation float Height : HEIGHT;
            };

            v2f Vert(uint id : SV_VertexID)
            {
                PointCloudPoint pt = _Buffer[id];

                v2f Output;
                Output.Color = PointCloudUnpack(pt.Color);
                Output.Position = mul(_PointCloudMVP, float4(pt.Position, 1));
                // Output.Depth = Output.Position.zw;
                Output.Height = pt.Position.y;
                return Output;
            }

            void Frag(v2f Input, out float4 color: SV_Target0 /*, out float depth : SV_Target1*/)
            {
                color = float4(PointCloudColor(Input.Color, Input.Height).rgb, 1);
                // depth = Input.Depth.x / Input.Depth.y;
            }

            ENDHLSL
        }
    }
}
