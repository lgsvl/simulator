#define NEG_THR -0.01

inline float4 permute(const float4 x)
{
    const float4 y = (x * 34.0 + 1.0) * x;
    return y - floor(y / 289.0) * 289.0;
}

void PerlinNoise_float(const float3 pos, out float output)
{
    float3 fl = floor(pos);
    float3 fr = frac(pos);
    float3 a = fl + 1.0;
    float3 b = fr - 1.0;
    
    fl = fl - 289.0 * floor(fl / 289.0);
    a = a - 289.0 * floor(a / 289.0);

    const float4 bx = float4(fl.x, a.x, fl.x, a.x);
    const float4 by = float4(fl.y, fl.y, a.y, a.y);

    const float4 p = permute(permute(bx) + by);
    const float4 pa = permute(p + fl.z);
    const float4 pc = permute(p + a.z);

    float4 ux = lerp(-1.0, 1.0, frac(floor(pa / 7.0) / 7.0));
    float4 uy = lerp(-1.0, 1.0, frac(floor(pa % 7.0) / 7.0));
    float4 uz = 1 - abs(ux) - abs(uy);

    const float4 sgn_u = uz < NEG_THR ? 1.0 : -1.0;
    ux += (ux < NEG_THR ? 1.0 : -1.0) * sgn_u;
    uy += (uy < NEG_THR ? 1.0 : -1.0) * sgn_u;

    float4 vx = lerp(-1.0, 1.0, frac(floor(pc / 7.0) / 7.0));
    float4 vy = lerp(-1.0, 1.0, frac(floor(pc % 7.0) / 7.0));
    float4 vz = 1.0 - abs(vx) - abs(vy);

    const float4 sgn_v = vz < NEG_THR ? 1.0 : -1.0;
    vx += (vx < NEG_THR ? 1.0 : -1.0) * sgn_v;
    vy += (vy < NEG_THR ? 1.0 : -1.0) * sgn_v;

    const float3 s0 = normalize(float3(ux.x, uy.x, uz.x));
    const float3 s1 = normalize(float3(ux.y, uy.y, uz.y));
    const float3 s2 = normalize(float3(ux.z, uy.z, uz.z));
    const float3 s3 = normalize(float3(ux.w, uy.w, uz.w));
    const float3 s4 = normalize(float3(vx.x, vy.x, vz.x));
    const float3 s5 = normalize(float3(vx.y, vy.y, vz.y));
    const float3 s6 = normalize(float3(vx.z, vy.z, vz.z));
    const float3 s7 = normalize(float3(vx.w, vy.w, vz.w));

    float d0 = dot(s0, fr);
    float d1 = dot(s1, float3(b.x, fr.y, fr.z));
    float d2 = dot(s2, float3(fr.x, b.y, fr.z));
    float d3 = dot(s3, float3(b.x, b.y, fr.z));
    float d4 = dot(s4, float3(fr.x, fr.y, b.z));
    float d5 = dot(s5, float3(b.x, fr.y, b.z));
    float d6 = dot(s6, float3(fr.x, b.y, b.z));
    float d7 = dot(s7, b);

    const float3 frp = (fr * (fr * 6.0 - 15.0) + 10.0) * fr * fr * fr;
    const float4 wz = lerp(float4(d0, d1, d2, d3), float4(d4, d5, d6, d7), frp.z);
    const float2 wyz = lerp(wz.xy, wz.zw, frp.y);
    output = lerp(wyz.x, wyz.y, frp.x) * 1.46;
}