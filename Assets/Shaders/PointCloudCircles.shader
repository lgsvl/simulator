/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/PointCloud/Circles"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma geometry Geom
            #pragma fragment Frag
            #pragma multi_compile_local _ _SIZE_IN_PIXELS

            #include "UnityCG.cginc"
            #include "PointCloudCommon.hlsl"

            struct g2f
            {
                float4 Position : SV_POSITION;
                nointerpolation float4 Color : COLOR;
                nointerpolation float Height : HEIGHT;
                float2 TexCoord : TEXCOORD0;
            };

            float _Size;
            float _MinSize;

            PointCloudPoint Vert(uint id : SV_VertexID) : POINT
            {
                return _Buffer[id];
            }

            [maxvertexcount(4)]
            void Geom(point PointCloudPoint pt[1]: POINT, inout TriangleStream<g2f> stream)
            {
                float3 pos = pt[0].Position;
                float4 color = PointCloudUnpack(pt[0].Color);

                float4 clip = UnityWorldToClipPos(PointCloudWorldPosition(pos));
                float2 scale = float2(_Size, _Size);

#ifdef _SIZE_IN_PIXELS
                scale *= clip.w / _ScreenParams.xy;
#else
                float minSize = _MinSize * clip.w / _ScreenParams.x;
                if (_Size < minSize)
                {
                    scale = float2(minSize, minSize);
                }
                scale.y *= _ScreenParams.x / _ScreenParams.y;
#endif
                float2 offsets[] =
                {
                    float2(-1, -1),
                    float2(1, -1),
                    float2(-1, 1),
                    float2(1, 1),
                };

                UNITY_UNROLL
                for (int i = 0; i < 4; i++)
                {
                    g2f o;
                    o.Position.xy = clip.xy + scale * offsets[i];
                    o.Position.zw = clip.zw;
                    o.Color = color;
                    o.TexCoord = offsets[i];
                    o.Height = pos.y;
                    stream.Append(o);
                }
            }

            float4 Frag(g2f Input) : SV_Target
            {
                if (dot(Input.TexCoord, Input.TexCoord) > 1)
                {
                    discard;
                }

                return PointCloudColor(Input.Color, Input.Height);
            }

            ENDHLSL
        }
    }
}
