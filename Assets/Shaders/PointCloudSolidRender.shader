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

            #include "UnityCG.cginc"
            #include "PointCloudCommon.hlsl"

            struct v2f
            {
                float4 Position : SV_POSITION;
                float3 View : VIEW;
                nointerpolation float4 Color : COLOR;
                nointerpolation float Height : HEIGHT;
            };

            float4x4 _ViewToClip;

            v2f Vert(uint id : SV_VertexID)
            {
                PointCloudPoint pt = _Buffer[id];

                v2f Output;
                Output.Color = PointCloudUnpack(pt.Color);
                Output.View = PointCloudWorldPosition(pt.Position);
                Output.Position = mul(_ViewToClip, float4(Output.View, 1));
                Output.Height = pt.Position.y;
                return Output;
            }

            float4 Frag(v2f Input) : SV_Target
            {
                // this produces distance to camera (better for hidden point removal)
                float w = length(Input.View);

                // this produces distance to projection plane (for correct depth output)
                // float w = LinearEyeDepth(Input.Position.z);

                return float4(PointCloudColor(Input.Color, Input.Height).rgb, w);
            }

            ENDHLSL
        }
    }
}
