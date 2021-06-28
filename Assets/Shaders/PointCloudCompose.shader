/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/PointCloud/HDRP/Compose"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinGIUtilities.hlsl"

    #pragma multi_compile _ _PC_TARGET_GBUFFER _PC_UNLIT_SHADOWS
    #pragma multi_compile _ LIGHT_LAYERS

    #ifdef _PC_UNLIT_SHADOWS
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP 
        #define SHADOW_OPTIMIZE_REGISTER_USAGE 1
        #define SHADOW_LOW

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowContext.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadow.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/PunctualLightCommon.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadowLoop.hlsl"
    #endif

    Texture2D _ColorTex;
    SamplerState sampler_ColorTex;
    
    Texture2D _NormalDepthTex;
    SamplerState sampler_NormalDepthTex;

    float _ShadowsFilter;

    float4 PC_SHAr;
    float4 PC_SHAg;
    float4 PC_SHAb;
    float4 PC_SHBr;
    float4 PC_SHBg;
    float4 PC_SHBb;
    float4 PC_SHC;

    TEXTURE2D_X(_OriginalDepth);

    float4 _SRMulVec;

    float3 EncodeFloatRGB(float v)
    {
        float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 16581375.0);
        float kEncodeBit = 1.0/255.0;
        float4 enc = kEncodeMul * v * 0.5;
        enc = frac (enc);
        enc -= enc.yzww * kEncodeBit;
        return enc.xyz;
    }

    float4 UnpackRGBA(float2 packed)
    {
        uint r = asuint(packed.r);
        uint b = asuint(packed.g);
        float4 rgba = float4(f16tof32(r), f16tof32(r >> 16), f16tof32(b), f16tof32(b >> 16));
        return rgba;
    }

    float3 SampleSH9(float3 normal)
    {
        real4 SHCoefficients[7];
        SHCoefficients[0] = PC_SHAr;
        SHCoefficients[1] = PC_SHAg;
        SHCoefficients[2] = PC_SHAb;
        SHCoefficients[3] = PC_SHBr;
        SHCoefficients[4] = PC_SHBg;
        SHCoefficients[5] = PC_SHBb;
        SHCoefficients[6] = PC_SHC;

        return SampleSH9(SHCoefficients, normal);
    }

    void DefaultComposePass(Varyings varyings,
    #ifdef _PC_TARGET_GBUFFER
        out float4 outGBuffer0 : SV_Target0, 
        out float4 outGBuffer1 : SV_Target1,
        out float4 outGBuffer2 : SV_Target2,
        out float4 outGBuffer3 : SV_Target3,
    #ifdef LIGHT_LAYERS
        out float4 outGBuffer4 : SV_Target4,
    #endif
    #else
        out float4 outColor : SV_Target0,
    #endif
        out float depth : SV_Depth)
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float camDepth = LOAD_TEXTURE2D_X_LOD(_OriginalDepth, varyings.positionCS.xy, 0).r;
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, camDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float2 insetSS = posInput.positionSS;
        insetSS.x = insetSS.x * _SRMulVec.x + _SRMulVec.y;
        insetSS.y = insetSS.y * _SRMulVec.x + _SRMulVec.z;

        float4 pcPacked = _ColorTex.Load(int3(insetSS, 0));
        float4 pcNormalDepth = _NormalDepthTex.Load(int3(insetSS, 0));

        float eyeDepth = pcPacked.w;

        if (eyeDepth <= camDepth)
            discard;

        depth = eyeDepth;
        float4 unpacked = UnpackRGBA(pcPacked.rg);
        float3 color = unpacked.rgb;

        #ifndef _PC_TARGET_GBUFFER

            posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, eyeDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

            #ifdef _PC_UNLIT_SHADOWS
                HDShadowContext shadowContext = InitShadowContext();
                float shadow;
                float3 shadow3;
                float3 normalWS = pcNormalDepth.rgb;
                ShadowLoopMin(shadowContext, posInput, normalWS, asuint(_ShadowsFilter), DEFAULT_LIGHT_LAYERS, shadow3);
                shadow = dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0));

                float4 shadowTint = float4(0,0,0,0.9);
                float4 shadowColor = (1 - shadow) * shadowTint.rgba;
                color = lerp(lerp(shadowColor.rgb, color, 1 - shadowTint.a), color, shadow);
            #endif

            float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
            float3 fogColor;
            float3 fogAlpha;
            EvaluateAtmosphericScattering(posInput, V, fogColor, fogAlpha);
            outColor.rgb = lerp(color, fogColor, fogAlpha.r * unpacked.w);
            outColor.a = 1;
        #else
            NormalData nData;
            nData.normalWS = pcNormalDepth.rgb;
            nData.perceptualRoughness = 1;

            float4 normalGBuffer;
            EncodeIntoNormalBuffer(nData, insetSS, /* out */ normalGBuffer);

            float multiplier = GetIndirectDiffuseMultiplier(DEFAULT_LIGHT_LAYERS);
            float3 ambient = SampleSH9(pcNormalDepth.rgb) * color * multiplier * GetCurrentExposureMultiplier();

            outGBuffer0 = float4(color, 1);
            outGBuffer1 = normalGBuffer;
            // outGBuffer2 = float4(0, 0, 0, PackFloatInt8bit(/* coat mask */ 0.0, /* GBUFFER_LIT_STANDARD */ 0, 8));
            outGBuffer2 = float4(0, 0, 0, 0);
            outGBuffer3 = float4(ambient, 0);
            #ifdef LIGHT_LAYERS
            outGBuffer4 = DEFAULT_LIGHT_LAYERS;
            #endif
        #endif
    }

    void LidarComposePass(Varyings varyings, out float4 outColor : SV_Target0)
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float camDepth = LOAD_TEXTURE2D_X_LOD(_OriginalDepth, varyings.positionCS.xy, 0).r;
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, camDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float2 insetSS = posInput.positionSS;
        insetSS.x = insetSS.x * _SRMulVec.x + _SRMulVec.y;
        insetSS.y = insetSS.y * _SRMulVec.x + _SRMulVec.z;

        float4 pcPacked = _ColorTex.Load(int3(insetSS, 0));

        float eyeDepth = pcPacked.w;
        float linearDepth = Linear01Depth(eyeDepth, _ZBufferParams);

        // Solid render has depth data on the whole texture, which lidar will detect - discard far plane
        if (eyeDepth < camDepth || linearDepth > 0.999)
            discard;

        // Sensors use unusual depth format - just calculate it here
        float2 positionNDC = varyings.positionCS.xy * _ScreenSize.zw;
        float3 positionWS = ComputeWorldSpacePosition(positionNDC, eyeDepth, UNITY_MATRIX_I_VP);
        float lidarDepth = length(GetPrimaryCameraPosition() - positionWS);

        float4 pcColor = UnpackRGBA(pcPacked.rg);
        float intensity = (pcColor.r + pcColor.g + pcColor.b) / 3;

        outColor = float4(EncodeFloatRGB(lidarDepth * _ProjectionParams.w), intensity);
    }

    void DepthComposePass(Varyings varyings, out float4 outColor : SV_Target0)
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float camDepth = LOAD_TEXTURE2D_X_LOD(_OriginalDepth, varyings.positionCS.xy, 0).r;
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, camDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float2 insetSS = posInput.positionSS;
        insetSS.x = insetSS.x * _SRMulVec.x + _SRMulVec.y;
        insetSS.y = insetSS.y * _SRMulVec.x + _SRMulVec.z;

        float4 pcPacked = _ColorTex.Load(int3(insetSS, 0));

        #ifdef _PC_LINEAR_DEPTH
            float linearDepth = 1.0 - pcPacked.w;
            float eyeDepth = (1 / (linearDepth) - _ZBufferParams.y) / _ZBufferParams.x;
        #else
            float linearDepth = Linear01Depth(pcPacked.w, _ZBufferParams);
            float eyeDepth = pcPacked.w;
        #endif

        if (eyeDepth < camDepth || linearDepth > 0.999)
            discard;

        // Sensors use unusual depth format - just calculate it here
        float2 positionNDC = varyings.positionCS.xy * _ScreenSize.zw;
        float3 positionWS = ComputeWorldSpacePosition(positionNDC, eyeDepth, UNITY_MATRIX_I_VP);

        float d = length(GetPrimaryCameraPosition() - positionWS);
        d = 1.0 / (_ZBufferParams.x * d + _ZBufferParams.y);
        d = pow(abs(d), 0.1);

        outColor = float4(d, d, d, 1);
    }

    void DebugComposePass(Varyings varyings, out float4 outColor : SV_Target0)
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, 0, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float2 insetSS = posInput.positionSS;
        // insetSS.x = insetSS.x * _SRMulVec.x + _SRMulVec.y;
        // insetSS.y = insetSS.y * _SRMulVec.x + _SRMulVec.z;
        float4 pcColor = _ColorTex.Load(int3((uint2)insetSS, 0));
        // pcColor.rgb = float3(pcColor.w, pcColor.w, pcColor.w);
        // outColor = pcColor;
        float4 rgbs = UnpackRGBA(pcColor.rg);
        // float3 rgb = float3(pcColor.rg, 0);
        outColor = float4(rgbs.rgb, 1);
    }

    ENDHLSL

    Properties
    {
        [HideInInspector] _StencilRefGBuffer("_StencilRefGBuffer", Int) = 2
        [HideInInspector] _StencilWriteMaskGBuffer("_StencilWriteMaskGBuffer", Int) = 3
    }

    SubShader
    {
        Pass
        {
            Stencil
            {
                WriteMask [_StencilWriteMaskGBuffer]
                Ref [_StencilRefGBuffer]
                Comp Always
                Pass Replace
            }

            Name "Point Cloud Default Compose"

            ZWrite On
            ZTest Always
            Blend One Zero
            Cull Off

            HLSLPROGRAM
                #pragma fragment DefaultComposePass
            ENDHLSL
        }

        Pass
        {
            Name "Point Cloud Lidar Compose"

            ZWrite On
            ZTest Always
            Blend One Zero
            Cull Off

            HLSLPROGRAM
                #pragma fragment LidarComposePass
            ENDHLSL
        }

        Pass
        {
            Name "Point Cloud Depth Compose"

            ZWrite On
            ZTest Always
            Blend One Zero
            Cull Off

            HLSLPROGRAM
                #pragma fragment DepthComposePass
            ENDHLSL
        }

        Pass
        {
            Name "Point Cloud Debug Compose"

            ZWrite Off
            ZTest Always
            Blend One Zero
            Cull Off

            HLSLPROGRAM
                #pragma fragment DebugComposePass
            ENDHLSL
        }
    }
    Fallback Off
}
