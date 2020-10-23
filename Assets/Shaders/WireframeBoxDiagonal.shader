/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/WireframeBoxDiagonal"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" }

        Pass
        {
            Cull Off

            CGPROGRAM
            #pragma vertex Vert
            #pragma geometry Geom
            #pragma fragment Frag

            #include "UnityCG.cginc"

            struct Box
            {
                float4x4 transform;
                float3 position;
                float3 size;
                float3 color;
            };

            StructuredBuffer<Box> _Boxes;

            float _LineWidth;

            struct v2g
            {
                float4x4 transform : TRANSFORM;
                float4 position : SV_POSITION;
                float3 size : SIZE;
                float3 color : COLOR;
            };

            struct g2f
            {
                float4 position : SV_POSITION;
                float3 color : COLOR;
            };

            v2g Vert(uint id : SV_VertexID)
            {
                Box box = _Boxes[id];

                v2g output;
                output.transform = box.transform;
                output.position = float4(box.position, 1.0f);
                output.size = box.size * 0.5f;
                output.color = box.color;
                return output;
            }

            void Vertex(float4 vertex, float2 normal, float3 color, inout TriangleStream<g2f> output)
            {
                g2f o;

                o.position = vertex + float4(normal, 0, 0);
                o.color = color;
                output.Append(o);

                o.position = vertex - float4(normal, 0, 0);
                o.color = color;
                output.Append(o);
            }

            void Line(float4x4 transform, float3 p0, float3 p1, float3 color, inout TriangleStream<g2f> output)
            {
                float4 v0 = UnityWorldToClipPos(mul(transform, float4(p0, 1)));
                float4 v1 = UnityWorldToClipPos(mul(transform, float4(p1, 1)));

                float2 dir = normalize((v1.xy / v1.w - v0.xy / v0.w).xy);
                float2 normal = float2(-dir.y, dir.x);

                float2 size = _LineWidth / _ScreenParams.xy;

                dir *= size * 0.5f;
                normal *= size;

                Vertex(v0 - float4(dir * v0.w, 0, 0), normal * v0.w, color, output);
                Vertex(v1 + float4(dir * v1.w, 0, 0), normal * v1.w, color, output);

                output.RestartStrip();
            }

            [maxvertexcount(4)]
            void Geom(point v2g input[1], inout TriangleStream<g2f> output)
            {
                g2f o;

                float4x4 transform = input[0].transform;
                float3 center = input[0].position.xyz;
                float3 size = input[0].size;
                float3 color = input[0].color;

                float3 edge[2] = { float3(-1, -1, -1), float3(+1, +1, +1) };

                float3 p0 = center + size * edge[0];
                float3 p1 = center + size * edge[1];
                Line(transform, p0, p1, color, output);
            }

            float4 Frag(g2f input) : SV_Target
            {
                return float4(input.color, 1.0f);
            }

            ENDCG
        }
    }
}
