Shader "Hidden/Shader/CameraRainFX"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    #pragma multi_compile LOW MED HGH
	#define S(a, b, t) smoothstep(a, b, t)
    
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

    // List of properties to control your post process effect
	float _Intensity;
	float _Size;
    TEXTURE2D_X(_InputTexture);

	//Random Number Generator
	float N21(float2 p)
	{
		p = frac(p * float2(123.34, 234.45));
		p += dot(p, p + 34.345);
		return frac(p.x * p.y);
	}

	float3 Layer(float2 UV, float t)
	{
		float2 aspect = float2(2, 1);
		float clamped = clamp(_Size, 0, 1);
		float scale = lerp(12, 3, clamped);
		float2 texcoord = UV * scale * aspect;
		texcoord.y += t * .25;
		float2 gv = frac(texcoord) - .5;
		float2 id = floor(texcoord);

		// Drop Animation
		float n = N21(id);
		t += n * 6.2831;
		float w = UV.y * 10;
		float x = (n - .5) * .8;
		x += (.4 - abs(x)) * sin(3 * w) * pow(sin(w), 6) * .45;
		float y = -sin(t + sin(t + sin(t) * .5)) * .45;

		//Shape of drop
		y -= (gv.x - x)*(gv.x - x);

		//Drop
		float2 dropPos = (gv - float2(x, y)) / aspect;
		float drop = S(.05, .03, length(dropPos));

		//Trail
		float2 trailPos = (gv - float2(x, t * .25)) / aspect;
		trailPos.y = (frac(trailPos.y * 8) - .5) / 8;
		float trail = S(.03, .01, length(trailPos));
		float fogTrail = S(-.05, .05, dropPos.y);
		fogTrail *= S(.5, y, gv.y);
		trail *= fogTrail;
		fogTrail *= S(.05, .04, abs(dropPos.x));

		float2 offset = drop * dropPos + trail * trailPos;
		return float3(offset, fogTrail);
	}

	float4 CustomPostProcess(Varyings input) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
		uint2 positionSS = input.texcoord * _ScreenSize.xy;

		//Create Multiple Rain Drops
		float t = fmod(_Time.y, 7200);
		float3 drops = Layer(input.texcoord, t);

#ifdef LOW
        drops += Layer(input.texcoord * 1.23 + 7.54, t);
#endif

#ifdef MED
        drops += Layer(input.texcoord * 1.23 + 7.54, t);
        drops += Layer(input.texcoord * 1.35 + 1.54, t);
#endif

#ifdef HGH
        drops += Layer(input.texcoord * 1.23 + 7.54, t);
        drops += Layer(input.texcoord * 1.35 + 1.54, t);
        drops += Layer(input.texcoord * 1.57 - 7.54, t);
#endif

		//Lerp the Rain Drops into the post effect
		float4 outColor = LOAD_TEXTURE2D_X(_InputTexture, positionSS + (drops.xy * -1) * _ScreenSize.xy);
		return outColor;
	}

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "CameraRainFX"

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
