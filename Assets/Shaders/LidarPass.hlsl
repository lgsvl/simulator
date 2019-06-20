/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

float3 EncodeFloatRGB(float v)
{
    float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 16581375.0);
    float kEncodeBit = 1.0/255.0;
    float4 enc = kEncodeMul * v * 0.5;
    enc = frac (enc);
    enc -= enc.yzww * kEncodeBit;
    return enc.xyz;
}

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}

void Frag(PackedVaryingsToPS packedInput,
        out float4 outColor : SV_Target0
        #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : SV_Depth
        #endif
          )
{
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz);

    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

#if defined(_SURFACE_TYPE_TRANSPARENT)
    clip(builtinData.opacity - 0.5);
#endif

    float depth = length(GetPrimaryCameraPosition() - posInput.positionWS);
#if defined(_MASKMAP)
    float intensity = surfaceData.metallic;
#else
    float intensity = (surfaceData.baseColor.r + surfaceData.baseColor.g + surfaceData.baseColor.b) / 3;
#endif

    outColor = float4(EncodeFloatRGB(depth * _ProjectionParams.w), intensity);

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif
}
