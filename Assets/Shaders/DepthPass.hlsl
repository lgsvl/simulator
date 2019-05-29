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
};

v2f Vert(vertex i)
{
    float3 world = TransformObjectToWorld(i.pos.xyz);

    v2f o;
    o.pos = TransformWorldToHClip(world);
    o.uv = TRANSFORM_TEX(i.uv, _BaseColorMap);
    return o;
}

// Z buffer to linear 0..1 depth
float Linear01Depth(float z)
{
    return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
}

float4 Frag(v2f i) : SV_Target
{
    float4 color = _BaseColorMap.Sample(sampler_BaseColorMap, i.uv) * _BaseColor;

#if defined(_ALPHATEST_ON)
    clip(color.a - _AlphaCutoff);
#elif defined(_SURFACE_TYPE_TRANSPARENT)
    clip(color.a - 0.5);
#endif

    float d = Linear01Depth(i.pos.z);
    d = pow(d, 0.4);

    return float4(d, d, d, 1);
}
