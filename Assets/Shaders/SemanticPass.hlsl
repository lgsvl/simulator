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
    v2f o;
    o.pos = TransformObjectToHClip(i.pos.xyz);
    o.uv = TRANSFORM_TEX(i.uv, _BaseColorMap);
    return o;
}

float4 Frag(v2f i) : SV_Target
{
    float alpha = _BaseColorMap.Sample(sampler_BaseColorMap, i.uv).a * _BaseColor.a;

#if defined(_ALPHATEST_ON)
    clip(alpha - _AlphaCutoff);
#elif defined(_SURFACE_TYPE_TRANSPARENT)
    clip(alpha - 0.5);
#endif

    return _SemanticColor;
}
