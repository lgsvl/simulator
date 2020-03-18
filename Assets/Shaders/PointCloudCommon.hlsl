/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

#ifndef POINT_CLOUD_COMMON_INCLUDED
#define POINT_CLOUD_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

struct PointCloudPoint
{
    float3 Position;
    uint Color;
};

StructuredBuffer<PointCloudPoint> _Buffer;

float4x4 _Transform;
int _Colorize;

float _MinHeight;
float _MaxHeight;

float4 PointCloudWorldPosition(float3 position)
{
    return mul(_Transform, float4(position, 1));
}

float3 PointCloudWorldPositionHDRP(float3 position)
{
    float3 positionWS = PointCloudWorldPosition(position).xyz;
    return GetCameraRelativePositionWS(positionWS);
}

float4 PointCloudUnpack(uint color)
{
    uint r = color & 0xff;
    uint g = (color >> 8) & 0xff;
    uint b = (color >> 16) & 0xff;
    uint a = color >> 24;
    return float4(float(r), float(g), float(b), float(a)) / 255.0f;
}

float3 PointCloudRainbow(float value)
{
    float h = value * 5.0f + 1.0f;
    int i = floor(h);
    float f = h - i;
    if (!(i & 1)) f = 1 - f;
    float n = 1 - f;

    if (i <= 1) return float3(n, 0, 1);
    else if (i == 2) return float3(0, n, 1);
    else if (i == 3) return float3(0, 1, n);
    else if (i == 4) return float3(n, 1, 0);
    else return float3(1, n, 0);
}

float4 PointCloudColor(float4 color, float height)
{
    if (_Colorize == 0) return float4(color.rgb, 1);
    if (_Colorize == 1) return float4(color.w, color.w, color.w, 1);
    if (_Colorize == 2) return float4(PointCloudRainbow(color.w), 1);

    float h = (height - _MinHeight) / (_MaxHeight - _MinHeight);
    return float4(PointCloudRainbow(h), 1);
}

#endif