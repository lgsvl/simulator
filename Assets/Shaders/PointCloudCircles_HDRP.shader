/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/PointCloud/HDRP/Circles"
{
    Properties
    {
        [HideInInspector] _StencilRefGBuffer("_StencilRefGBuffer", Int) = 2
        [HideInInspector] _StencilWriteMaskGBuffer("_StencilWriteMaskGBuffer", Int) = 3
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    #pragma require geometry

    #include "PointCloudCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

    float _Size;
    float _MinSize;
    float4 _PCShadowVector;
    float4x4 _ViewProj;
    float4x4 _ViewProjShadow;

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "Point Cloud Circles GBuffer"

            Stencil
            {
                WriteMask [_StencilWriteMaskGBuffer]
                Ref [_StencilRefGBuffer]
                Comp Always
                Pass Replace
            }


            HLSLPROGRAM

            #pragma vertex Vert
            #pragma geometry Geom
            #pragma fragment Frag
            #pragma multi_compile_local _ _SIZE_IN_PIXELS
            #pragma multi_compile _CIRCLES _CONES
            #pragma multi_compile _ _PC_TARGET_GBUFFER

            struct g2f
            {
                float4 Position : SV_POSITION;
                nointerpolation float4 Color : COLOR;
                nointerpolation float Height : HEIGHT;
                float2 TexCoord : TEXCOORD0;
                float3 ViewPos : TEXCOORD1;
            };

            PointCloudPoint Vert(uint id : SV_VertexID) : POINT
            {
                return _Buffer[id];
            }

            [maxvertexcount(4)]
            void Geom(point PointCloudPoint pt[1]: POINT, inout TriangleStream<g2f> stream)
            {
                float3 pos = pt[0].Position;
                float4 color = PointCloudUnpack(pt[0].Color);

                float3 worldPos = PointCloudWorldPositionHDRP(pos);
                float4 clip = TransformWorldToHClip(worldPos);
                float3 view = TransformWorldToView(worldPos);
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
                    o.ViewPos = view;
                    stream.Append(o);
                }
            }

            void Frag(g2f Input,
            #ifdef _PC_TARGET_GBUFFER
                out float4 outGBuffer0 : SV_Target0, 
                out float4 outGBuffer1 : SV_Target1,
                out float4 outGBuffer2 : SV_Target2,
                out float4 outGBuffer3 : SV_Target3
            #else
                out float4 outColor : SV_Target0
            #endif
            #ifdef _CONES
                ,
                out float outDepth : SV_Depth
            #endif
                )
            {
                if (dot(Input.TexCoord, Input.TexCoord) > 1)
                {
                    discard;
                }

                #ifdef _CONES
                    float uvlen = Input.TexCoord.x * Input.TexCoord.x + Input.TexCoord.y * Input.TexCoord.y;
                    float3 view = Input.ViewPos;
                    view.z += (1 - sqrt(uvlen)) * _Size;

                    float4 pos = mul(UNITY_MATRIX_P, float4(view, 1));
                    pos /= pos.w;
                    float depth = pos.z;
                    outDepth = depth;
                #else
                    float4 pos = mul(UNITY_MATRIX_P, float4(Input.ViewPos, 1));
                    pos /= pos.w;
                    float depth = pos.z;
                #endif

                float3 color = PointCloudColor(Input.Color, Input.Height).rgb;
                #ifdef _PC_TARGET_GBUFFER
                    outGBuffer0 = float4(color, 1);
                    outGBuffer1 = float4(0, 0, 0, 1);
                    outGBuffer2 = float4(0, 0, 0, 0);
                    outGBuffer3 = float4(color * 0.1, 0);
                #else
                    PositionInputs posInput = GetPositionInput(Input.Position.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
                    float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
                    float3 fogColor;
                    float3 fogAlpha;
                    EvaluateAtmosphericScattering(posInput, V, fogColor, fogAlpha);
                    outColor.rgb = lerp(color, fogColor, fogAlpha.r);
                    outColor.a = 1;
                #endif
            }

            ENDHLSL
        }

        Pass
        {
            Name "Point Cloud Circles Lidar"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma geometry Geom
            #pragma fragment Frag
            #pragma multi_compile_local _ _SIZE_IN_PIXELS

            struct g2f
            {
                float4 Position : SV_POSITION;
                nointerpolation float4 Color : COLOR;
                float3 WorldPos : TEXCOORD1;
            };

            PointCloudPoint Vert(uint id : SV_VertexID) : POINT
            {
                return _Buffer[id];
            }

            float3 EncodeFloatRGB(float v)
            {
                float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 16581375.0);
                float kEncodeBit = 1.0/255.0;
                float4 enc = kEncodeMul * v * 0.5;
                enc = frac (enc);
                enc -= enc.yzww * kEncodeBit;
                return enc.xyz;
            }


            [maxvertexcount(4)]
            void Geom(point PointCloudPoint pt[1]: POINT, inout TriangleStream<g2f> stream)
            {
                float3 pos = pt[0].Position;
                float4 color = PointCloudUnpack(pt[0].Color);
                float3 worldPos = PointCloudWorldPositionHDRP(pos);
                float4 clip = TransformWorldToHClip(worldPos);
                float2 scale = float2(_Size, _Size);

                #ifdef _SIZE_IN_PIXELS
                    scale *= clip.w / _ScreenSize.xy;
                #else
                    float minSize = _MinSize * clip.w / _ScreenSize.x;
                    if (_Size < minSize)
                    {
                        scale = float2(minSize, minSize);
                    }
                    scale.y *= _ScreenSize.x / _ScreenSize.y;
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
                    o.WorldPos = worldPos;
                    stream.Append(o);
                }
            }

            void Frag(g2f Input, out float4 outColor : SV_Target0)
            {
                // if (dot(Input.TexCoord, Input.TexCoord) > 1)
                // {
                //     discard;
                // }

                float depth = 1.0 / (_ZBufferParams.x * Input.Position.z + _ZBufferParams.y);

                float3 color = PointCloudColor(Input.Color, 1).rgb;
                float intensity = (color.r + color.g + color.b) / 3;
                outColor = float4(EncodeFloatRGB(depth), intensity);
            }

            ENDHLSL
        }

        Pass
        {
            Name "Point Cloud Circles Depth"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma geometry Geom
            #pragma fragment Frag
            #pragma multi_compile_local _ _SIZE_IN_PIXELS

            struct g2f
            {
                float4 Position : SV_POSITION;
                nointerpolation float4 Color : COLOR;
                float2 TexCoord : TEXCOORD0;
                float3 WorldPos : TEXCOORD1;
            };

            PointCloudPoint Vert(uint id : SV_VertexID) : POINT
            {
                return _Buffer[id];
            }

            float3 EncodeFloatRGB(float v)
            {
                float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 16581375.0);
                float kEncodeBit = 1.0/255.0;
                float4 enc = kEncodeMul * v * 0.5;
                enc = frac (enc);
                enc -= enc.yzww * kEncodeBit;
                return enc.xyz;
            }

            [maxvertexcount(4)]
            void Geom(point PointCloudPoint pt[1]: POINT, inout TriangleStream<g2f> stream)
            {
                float3 pos = pt[0].Position;
                float4 color = PointCloudUnpack(pt[0].Color);
                float3 worldPos = PointCloudWorldPositionHDRP(pos);
                float4 clip = TransformWorldToHClip(worldPos);
                float2 scale = float2(_Size, _Size);

                #ifdef _SIZE_IN_PIXELS
                    scale *= clip.w / _ScreenSize.xy;
                #else
                    float minSize = _MinSize * clip.w / _ScreenSize.x;
                    if (_Size < minSize)
                    {
                        scale = float2(minSize, minSize);
                    }
                    scale.y *= _ScreenSize.x / _ScreenSize.y;
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
                    o.WorldPos = worldPos;
                    stream.Append(o);
                }
            }

            void Frag(g2f Input, out float4 outColor : SV_Target0)
            {
                // TODO: this discard changes color of discarded pixels the output texture - fix
                // if (dot(Input.TexCoord, Input.TexCoord) > 1)
                // {
                //     discard;
                // }

                float depth = length(GetPrimaryCameraPosition() - Input.WorldPos);

                float d = Linear01Depth(depth, _ZBufferParams);
                d = pow(abs(d), 0.1);

                outColor = float4(d, d, d, 1);
            }

            ENDHLSL
        }

        Pass
        {
            Name "Point Cloud Circles ShadowCaster"
        
            Cull [_CullMode]

            ZClip [_ZClip]
            ZWrite On
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma geometry Geom
            #pragma fragment Frag

            struct g2f
            {
                float4 Position : SV_POSITION;
                float2 TexCoord : TEXCOORD0;
            };

            PointCloudPoint Vert(uint id : SV_VertexID) : POINT
            {
                return _Buffer[id];
            }

            [maxvertexcount(4)]
            void Geom(point PointCloudPoint pt[1]: POINT, inout TriangleStream<g2f> stream)
            {
                float3 pos = pt[0].Position;
                float4 color = PointCloudUnpack(pt[0].Color);
                float3 positionWS = PointCloudWorldPositionHDRP(pos);
                float4 clip = TransformWorldToHClip(positionWS);
                float scale = _PCShadowVector.x;
                clip.z += _PCShadowVector.y;

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
                    o.TexCoord = offsets[i];
                    stream.Append(o);
                }
            }

            void Frag(g2f Input)
            {
                // if (dot(Input.TexCoord, Input.TexCoord) > 1)
                // {
                //     discard;
                // }
            }

            ENDHLSL
        }
    }
}
