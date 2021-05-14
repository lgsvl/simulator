/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Hidden/Shader/SunFlare"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    #define HSQRT2 0.70710678118 // sqrt(2)/2
    #define DIV4PI 1.27323954474 // 4/pi

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

	static float2 sample_offset[8] =
	{
		float2(1, 0),
		float2(HSQRT2, HSQRT2),
		float2(0, 1),
		float2(-HSQRT2, HSQRT2),
		float2(-1, 0),
		float2(-HSQRT2, -HSQRT2),
		float2(0, -1),
		float2(HSQRT2, -HSQRT2)
	};

    static const float3 ghost_sizes0 = float3(2.4, 2.4, 2.4);
    static const float3 ghost_sizes1 = float3(5.5, 5.5, 5.5);
    static const float3 ghost_sizes2 = float3(1.6, 1.6, 1.6);
    
    static const float3 ghost_offsets0 = float3(0.4, 0.45, 0.5);
    static const float3 ghost_offsets1 = float3(0.2, 0.4, 0.6);
    static const float3 ghost_offsets2 = float3(0.3, 0.325, 0.35);

    static const float3 ghost_intensities0 = float3(6, 5, 3);
    static const float3 ghost_intensities1 = float3(2, 2, 2);
    static const float3 ghost_intensities2 = float3(6, 3, 5);
    
    float4 _SunSettings; // x: sun disk intensity, y: halo intensity, z: ghosting intensity, w: angle intensity
    float4 _SunViewPos;
    TEXTURE2D_X(_InputTexture);
    sampler2D _OcclusionTexOut;
    sampler2D _OcclusionTexIn;
    StructuredBuffer<float> _AngleOcclusion;

    inline float3 CalcGhost(float2 luv, float2 lightPos, float3 flipOffset, float3 size, float3 intensity)
    {
		float rl = length(luv + flipOffset.r * lightPos);
    	float gl = length(luv + flipOffset.g * lightPos);
    	float bl = length(luv + flipOffset.b * lightPos);

    	return max(0.01 - pow(float3(rl, gl, bl), size), 0) * intensity;
    }

    float SampleRayIntensity(float2 ouv)
    {
	    const float2 couv = ouv - 0.5;
	    const float ang = atan2(couv.y, couv.x);
        float index = ang * 64 / 3.141569;
        index = index < 0 ? index + 128 : index;
    	int iLow = floor(index);
    	if (iLow < 0)
    		iLow = 127;
    	int iHigh = ceil(index);
    	if (iHigh > 127)
    		iHigh = 0;
    	float lVal = frac(index);
        return lerp(_AngleOcclusion[iLow], _AngleOcclusion[iHigh], lVal);
    }

    float3 CalcSunFlare(float2 ouv, float2 cuv, float2 pos, uint2 posSS, uint2 sunPosSS)
	{
		const float2 fuv = cuv * length(cuv);

		const float globalOccl = tex2D(_OcclusionTexIn, float2(0, 0)).r;
		const float rayOccl = SampleRayIntensity(ouv);
		const float depth = Linear01Depth(LoadCameraDepth(posSS), _ZBufferParams);

		const float texOccl = depth;
		const float occl = rayOccl * texOccl;
		const float fOccl = lerp(globalOccl, 1, occl);

		float sunDisk = 1 / (length(cuv - pos) * 25 + 1);
		sunDisk = sunDisk + max(sunDisk, 0.2);
    	sunDisk = clamp(sunDisk, 0, 0.1);
    	sunDisk *= fOccl;
    	sunDisk *= _SunSettings.x;

		float halo_r = max(1 / (1 + 32 * pow(length(2 * fuv + 1.1 * pos), 2)), 0) * 0.25;
		float halo_g = max(1 / (1 + 32 * pow(length(2 * fuv + 1.2 * pos), 2)), 0) * 0.23;
		float halo_b = max(1 / (1 + 32 * pow(length(2 * fuv + 1.3 * pos), 2)), 0) * 0.21;
    	float3 halo = float3(halo_r, halo_g, halo_b);
    	halo *= _SunSettings.y;

		float2 luv = lerp(cuv, fuv, -0.5);
		const float3 g0 = CalcGhost(luv, pos, ghost_offsets0, ghost_sizes0, ghost_intensities0);
		luv = lerp(cuv,fuv,-0.4);
		const float3 g1 = CalcGhost(luv, pos, ghost_offsets1, ghost_sizes1, ghost_intensities1);
		luv = lerp(cuv,fuv,-0.5);
		const float3 g2 = CalcGhost(luv, pos, ghost_offsets2, ghost_sizes2, ghost_intensities2);

    	float3 g = g0 + g1 + g2;
    	g *= _SunSettings.z;
		float3 c = halo + g;
    	float gOccl = SampleRayIntensity(-pos);
    	c *= saturate(2 * gOccl);
    	float ln = length(fuv) * 0.1;
		c = saturate(c * 1.3 - ln + sunDisk);
    	c *= pow(globalOccl, 0.5);
    	return c;
	}

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        uint2 positionSS = input.texcoord * _ScreenSize.xy;
        float3 inColor = LOAD_TEXTURE2D_X(_InputTexture, positionSS).xyz;
    	float aspectRatio = _ScreenSize.x * _ScreenSize.w;
    	uint2 sunPosSS = _SunViewPos.xy * _ScreenSize.xy;
    	float2 uv = input.texcoord - 0.5;
    	float2 pos = _SunViewPos.xy - 0.5;
    	uv.x *= aspectRatio;
    	pos.x *= aspectRatio;
    	float2 ouv = (0.05 + (uv - pos)) * 10;
    	// TODO: grab tint from skybox
    	float3 tint = float3(1.4,1.2,1.0);
		float3 color = tint * CalcSunFlare(ouv, uv, pos, positionSS, sunPosSS);
    	color *= _SunSettings.w;
		float4 result = float4(inColor + color ,1.0);
        return result;
    }

    ENDHLSL
    SubShader
    {
        Pass
        {
            Name "SunFlare"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off
            HLSLPROGRAM
            #pragma fragment CustomPostProcess
            #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
