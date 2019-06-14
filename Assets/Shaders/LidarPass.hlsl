/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

struct vertex
{
    float4 pos : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 view : TEXCOORD1;
};

float3 EncodeFloatRGB(float v)
{
	float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 16581375.0);
    float kEncodeBit = 1.0/255.0;
    float4 enc = kEncodeMul * v * 0.5;
    enc = frac (enc);
    enc -= enc.yzww * kEncodeBit;
    return enc.xyz;
}

v2f Vert(vertex i)
{
    float3 world = TransformObjectToWorld(i.pos.xyz);

    v2f o;
    o.pos = TransformWorldToHClip(world);
    o.uv = TRANSFORM_TEX(i.uv, _BaseColorMap);
    o.view = TransformWorldToView(world);
    return o;
}

float4 Frag(v2f i) : SV_Target
{
    float4 color = _BaseColorMap.Sample(sampler_BaseColorMap, i.uv) * _BaseColor;

#if defined(_ALPHATEST_ON)
    clip(color.a - _AlphaCutoff);
#elif defined(_SURFACE_TYPE_TRANSPARENT)
    clip(color.a - 0.5);
#endif

    float depth = length(i.view) * _ProjectionParams.w;
    float intensity = (color.r + color.g + color.b) / 3;

    return float4(EncodeFloatRGB(depth), intensity);
}
