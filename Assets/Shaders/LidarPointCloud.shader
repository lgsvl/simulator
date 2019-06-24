/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/LidarPointCloud"
{
    Properties
    {
        _Color("Color", Color) = (1, 0, 0, 1)
        _Size("Size (px)", Float) = 2
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" }

        Pass
        {
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma geometry Geom
            #pragma fragment Frag

            #include "UnityCG.cginc"

            CBUFFER_START(UnityPerMaterial)
                float4x4 _LocalToWorld;
                float4 _Color;
                float _Size;
            CBUFFER_END

            StructuredBuffer<float4> _PointCloud;

            struct g2f
            {
                float4 position : POSITION;
            };

            float4 Vert(uint id : SV_VertexID) : POINT
            {
                return _PointCloud[id];
            }

            [maxvertexcount(4)]
            void Geom(point float4 vertex[1]: POINT, inout TriangleStream<g2f> stream)
            {
                float3 pos = vertex[0].xyz;
                if (dot(pos, pos) == 0) return;

                float4 world = mul(_LocalToWorld, float4(pos, 1.0));
                float4 clip = UnityWorldToClipPos(world.xyz);

                float2 scale = _Size * clip.w / _ScreenParams.xy;

                float2 offsets[] = {
                    float2(-1, -1),
                    float2(1, -1),
                    float2(-1, 1),
                    float2(1, 1),
                };

                UNITY_UNROLL
                for (int i = 0; i < 4; i++)
                {
                    g2f vertex;
                    vertex.position.xy = clip.xy + scale * offsets[i];
                    vertex.position.z = 1.1f * clip.z;
                    vertex.position.w = clip.w;
                    stream.Append(vertex);
                }
            }

            float4 Frag(g2f input) : SV_Target
            {
                return float4(_Color.rgb, 1.0f);
            }

            ENDHLSL
        }
    }
}
