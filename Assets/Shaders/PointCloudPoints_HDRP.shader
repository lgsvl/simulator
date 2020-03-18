/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/PointCloud/HDRP/Points"
{
    Properties
    {
        [HideInInspector] _StencilRefGBuffer("_StencilRefGBuffer", Int) = 2
        [HideInInspector] _StencilWriteMaskGBuffer("_StencilWriteMaskGBuffer", Int) = 3
    }

    SubShader
    {
        Name "Point Cloud Points GBuffer"
        Tags { "Queue" = "Transparent" }

        Pass
        {
            Stencil
            {
                WriteMask [_StencilWriteMaskGBuffer]
                Ref [_StencilRefGBuffer]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _PC_TARGET_GBUFFER

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
                Output.Position = TransformWorldToHClip(PointCloudWorldPositionHDRP(v.Position));
                Output.Color = PointCloudUnpack(v.Color);
                Output.Height = v.Position.y;
                return Output;
            }

            void Frag(v2f Input, 
                #ifdef _PC_TARGET_GBUFFER
                out float4 outGBuffer0 : SV_Target0, 
                out float4 outGBuffer1 : SV_Target1,
                out float4 outGBuffer2 : SV_Target2,
                out float4 outGBuffer3 : SV_Target3
            #else
                out float4 outColor : SV_Target0
            #endif
                )
            {
                float3 color = PointCloudColor(Input.Color, Input.Height).rgb;
                #ifdef _PC_TARGET_GBUFFER
                    outGBuffer0 = float4(color, 1);
                    outGBuffer1 = float4(0, 0, 0, 1);
                    outGBuffer2 = float4(0, 0, 0, 0);
                    outGBuffer3 = float4(color * 0.1, 0);
                #else
                    outColor = float4(color, 1);
                #endif
            }

            ENDHLSL
        }
    }
}
