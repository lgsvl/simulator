Shader "Hidden/Shader/VideoArtifacts"
{
	HLSLINCLUDE
	#pragma target 4.5
	#pragma only_renderers d3d11 vulkan metal

	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

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

	//Block effect properties
	uint _Seed;

	float _BlockStrength;
	uint _BlockStride;
	uint _BlockSeed1;
	uint _BlockSeed2;
	uint _BlockSize;

	TEXTURE2D_X(_InputTexture);

	float FRandom(uint seed)
	{
		return GenerateHashedRandomFloat(seed);
	}

	float4 CustomPostProcess(Varyings input) : SV_Target
	{
		float2 uv = input.texcoord;

		// Block index
		uint columns = _ScreenSize.x / _BlockSize;
		uint2 block_xy = input.texcoord * _ScreenSize.xy / _BlockSize;
		uint block = block_xy.y * columns + block_xy.x;

		// Segment index
		uint segment = block / _BlockStride;

		// Per-block random number
		float r1 = FRandom(block + _BlockSeed1);
		float r3 = FRandom(block / 3 + _BlockSeed2);
		uint seed = (r1 + r3) < 1 ? _BlockSeed1 : _BlockSeed2;
		float rand = FRandom(segment + seed);

		// Block damage (offsetting)
		block += rand * 20000 * (rand < _BlockStrength);

		// Screen space position reconstruction (ssp)
		uint2 ssp = uint2(block % columns, block / columns) * _BlockSize;
		ssp += (uint2)(input.texcoord * _ScreenSize.xy) % _BlockSize;

		// UV recalculation
		uv = frac((ssp + 0.5) / _ScreenSize.xy);

		float4 c = LOAD_TEXTURE2D_X(_InputTexture, uv * _ScreenSize.xy);

		// Block damage (color mixing)
		if (frac(rand * 1234) < _BlockStrength * 0.1)
		{
			float3 hsv = RgbToHsv(c.rgb);
			hsv = hsv * float3(-1, 1, 0) + float3(0.5, 0, 0.9);
			c.rgb = HsvToRgb(hsv);
		}
		return c;
	}
		ENDHLSL
	SubShader
	{
		Pass
			{
				Name "VideoArtifacts"
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