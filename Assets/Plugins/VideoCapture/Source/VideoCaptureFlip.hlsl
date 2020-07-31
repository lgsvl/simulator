/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Texture2D<float4> Input : register(t0);

RWTexture2D<float4> Output : register(u0);

[numthreads(8,8,1)]
void FlipKernel(uint3 id: SV_DispatchThreadID)
{
    uint width, height;
    Output.GetDimensions(width, height);

    uint2 flipped = uint2(id.x, height - id.y - 1);

    Output[flipped] = Input[id.xy];
}
