/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/Line"
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

            struct Line
            {
                float4x4 transform;
                float3 start;
                float3 end;
                float3 color;
            };

            StructuredBuffer<Line> _Lines;

            float _LineWidth;

            struct v2g
            {
                float4x4 transform : TRANSFORM;
                float4 start : SV_POSITION;
                float4 end : END;
                float3 color : COLOR;
            };

            struct g2f
            {
                float4 position : SV_POSITION;
                float3 color : COLOR;
            };

            v2g Vert(uint id : SV_VertexID)
            {
                Line ln = _Lines[id];

                v2g output;
                output.transform = ln.transform;
                output.start = float4(ln.start, 1.0f);
                output.end = float4(ln.end, 1.0f);
                output.color = ln.color;
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

            void MakeLine(float4x4 transform, float4 p0, float4 p1, float3 color, inout TriangleStream<g2f> output)
            {
                float4 v0 = UnityWorldToClipPos(mul(transform, p0));
                float4 v1 = UnityWorldToClipPos(mul(transform, p1));

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
                float4 start = input[0].start;
                float4 end = input[0].end;
                float3 color = input[0].color;

                MakeLine(transform, start, end, color, output);
            }

            float4 Frag(g2f input) : SV_Target
            {
                return float4(input.color, 1.0f);
            }

            ENDCG
        }
    }
}
