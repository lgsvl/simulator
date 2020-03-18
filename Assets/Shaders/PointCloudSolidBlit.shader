/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/PointCloud/SolidBlit"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" }

        Pass
        {
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ COLOR_ONLY NORMALS_ONLY DEPTH_ONLY

            #include "UnityCG.cginc"

            Texture2D _ColorTex;
            SamplerState sampler_ColorTex;
            float4 _ColorTex_TexelSize;

            Texture2D _NormalDepthTex;
            SamplerState sampler_NormalDepthTex;

            float4x4 _ReprojectionMatrix;
            float4x4 _InvProjMatrix;

            float _FarPlane;
            float _SRMul;

            Texture2D _MaskTex;

            int _DebugLevel;

            struct v2f
            {
                float4 Position : SV_Position;
                float2 TexCoord : TEXCOORD;
            };

            v2f Vert(uint id : SV_VertexID)
            {
                // https://www.slideshare.net/DevCentralAMD/vertex-shader-tricks-bill-bilodeau

                v2f Output;

                Output.Position.x = (float)(id / 2) * 4 - 1;
                Output.Position.y = (float)(id % 2) * 4 - 1;
                Output.Position.z = 0;
                Output.Position.w = 1;

                Output.Position = mul(_ReprojectionMatrix, Output.Position);

                Output.TexCoord.x = (float)(id / 2) * 2;
                Output.TexCoord.y = (float)(id % 2) * 2;

                Output.TexCoord.xy *= _ColorTex_TexelSize.xy * _SRMul * _ScreenParams.xy / float(1 << _DebugLevel);

                return Output;
            }

            struct FragOutput
            {
                float4 Color : SV_Target;
                float Depth : SV_Depth;
            };

            FragOutput Frag(v2f Input)
            {
                float2 uv = int2(Input.TexCoord * _ColorTex_TexelSize.zw);
                float4 col = _ColorTex.Load(float3(uv, _DebugLevel));
                float4 dnSample = _NormalDepthTex.Load(float3(uv, _DebugLevel));
                float depth = dnSample.w;
                // float originalDepth = _OriginalDepthTex.Load(float3(uv, 0));
                // float4 originalColor = tex2D(_OriginalColorTex, Input.TexCoord);
                float3 normalPacked = dnSample.rgb;
                float3 normal = normalPacked * 2 - 1;

                #if NORMALS_ONLY
                    col.rgb = normalPacked;
                #elif DEPTH_ONLY
                    col.rgb = float3(depth, depth, depth);
                #elif !defined(COLOR_ONLY)
                    // == Debug Lambert lighting
                    float3 worldNormal = mul(_InvProjMatrix, normal);
                    float3 lightDir = _WorldSpaceLightPos0.xyz;
                    fixed diff = max (0, dot (worldNormal, lightDir));

                    fixed4 lighting;
                    lighting.rgb = (col * diff);
                    lighting.a = 1;
                    col.rgb = col.rgb * 0.3 + lighting * 0.8;
                    // ==/
                #endif

                col.a = 1;

                FragOutput Output;
                Output.Color = col;
                Output.Depth = 0.5; // TODO clip.z / clip.w;
                return Output;
            }

            ENDHLSL
        }
    }
}
