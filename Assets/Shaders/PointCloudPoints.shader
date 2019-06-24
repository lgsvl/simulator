/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/PointCloud/Points"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"
            #include "PointCloudCommon.hlsl"

            struct v2f
            {
                float4 Position : SV_POSITION;
                nointerpolation float4 Color: COLOR;
                nointerpolation float Height : HEIGHT;
            };

            v2f Vert(uint id : SV_VertexID)
            {
                PointCloudPoint v = _Buffer[id];

                v2f Output;
                Output.Position = UnityWorldToClipPos(PointCloudWorldPosition(v.Position));
                Output.Color = PointCloudUnpack(v.Color);
                Output.Height = v.Position.y;
                return Output;
            }

            float4 Frag(v2f Input) : SV_Target
            {
                return PointCloudColor(Input.Color, Input.Height);
            }

            ENDHLSL
        }
    }
}
