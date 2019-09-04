/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/AAWireBox"
{
    SubShader
    {
        Tags { "RenderType" = "Overlay"}

        Pass
        {
            Cull Off
            ZTest Off
            ZWrite On

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            struct Vertex
            {
                float3 position;
                float3 color;
            };

            StructuredBuffer<Vertex> _Vertices;

            struct v2f
            {
                float4 position : SV_POSITION;
                nointerpolation float3 color : COLOR;
            };

            v2f Vert(uint id : SV_VertexID)
            {
                Vertex v = _Vertices[id];

                v2f output;
                output.position = float4(v.position.xy, 0.999f, 1);
                output.color = v.color;
                return output;
            }

            float4 Frag(v2f input) : SV_Target
            {
                return float4(input.color, 1.0f);
            }

            ENDCG
        }
    }
}
