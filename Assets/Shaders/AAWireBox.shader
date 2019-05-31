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
        Tags { "RenderType" = "Overlay" }

        Pass
        {
            Cull Off
            ZTest Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex Vert
            #pragma geometry Geom
            #pragma fragment Frag

            #include "UnityCG.cginc"

            struct Box
            {
                float2 min;
                float2 max;
                float3 color;
            };

            StructuredBuffer<Box> _Boxes;

            float _LineWidth;

            struct v2g
            {
                float2 min : MIN;
                float2 max : MAX;
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
                output.min = float4(box.min, 0, 1);
                output.max = float4(box.max, 0, 1);
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

            void Line(float4 v0, float4 v1, float3 color, inout TriangleStream<g2f> output)
            {
                float2 dir = normalize((v1.xy / v1.w - v0.xy / v0.w).xy);
                float2 normal = float2(-dir.y, dir.x);

                float2 size = _LineWidth / _ScreenParams.xy;

                dir *= size;
                normal *= size;

                Vertex(v0 - float4(dir * v0.w, 0, 0), normal * v0.w, color, output);
                Vertex(v1 + float4(dir * v1.w, 0, 0), normal * v1.w, color, output);

                output.RestartStrip();
            }

            [maxvertexcount(16)]
            void Geom(point v2g input[1], inout TriangleStream<g2f> output)
            {
                float3 color = input[0].color;
                
                float4 min = float4(input[0].min, 0, 1);
                min.y *= -1;
                float4 max = float4(input[0].max, 0, 1);
                max.y *= -1;
                float4 p0 = float4(min.x, max.y, 0, 1);
                float4 p1 =  float4(max.x, min.y, 0, 1);

                Line(min, p0, color, output);
                Line(p0, max, color, output);
                Line(max, p1, color, output);
                Line(p1, min, color, output);
            }

            float4 Frag(g2f input) : SV_Target
            {
                return float4(input.color, 1.0f);
            }

            ENDCG
        }
    }
}
