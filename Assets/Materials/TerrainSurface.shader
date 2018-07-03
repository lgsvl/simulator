Shader "TinyTerrain/TerrainSurface" {
	Properties {
		_FractalTex("Fractal Tex", 2D) = "white" {}
		_PlantTex("Plant Tex", 2D) = "white" {}
		plantScale("Plant Tex Scale", float) = 1
		_SlopeTex("Slope Tex", 2D) = "white" {}
		slopeScale("Slope Tex Scale", float) = 1
		_FlatTex("Flat Tex", 2D) = "white" {}
		flatScale("Flat Tex Scale", float) = 1
		_BumpTex("Bump Tex", 2D) = "white" {}
		bumpScale("Bump Tex Scale", float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Lambert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _FractalTex, _PlantTex, _SlopeTex, _FlatTex, _BumpTex;
		float plantScale, slopeScale, flatScale, bumpScale;
		struct Input {
			float2 uv_FractalTex;
			float3 worldPos;
			float3 worldNormal;
		}; 

		float4 GetColor(float3 position, float3 normal, float terrainType)
		{
			// Calculate blend weights
			float3 blend_weights = normal;
			blend_weights.y = max(0, blend_weights.y);
			blend_weights = abs(blend_weights);
			blend_weights = pow(blend_weights, 3);
			blend_weights = normalize(blend_weights);

			// Get sand color, mix of sand texture and bump map
			float4 bump = tex2D(_BumpTex, position.zx * bumpScale);
			float4 sand = tex2D(_FlatTex, position.zx * flatScale);
			sand = lerp(sand, bump, .5);

			// Get grass color
			float4 grass = tex2D(_PlantTex, position.zx * plantScale);

			// Lerp ground color between sand and grass depending on terrainType
			float4 ground = lerp(sand, grass, terrainType);

			return tex2D(_SlopeTex, position.yz * slopeScale).xyzw * blend_weights.xxxx +
					ground * blend_weights.yyyy +
					tex2D(_SlopeTex, position.xy * slopeScale).xyzw * blend_weights.zzzz;
		}

		void surf (Input IN, inout SurfaceOutput o) {
			float4 terrain = tex2D(_FractalTex, IN.uv_FractalTex);
			float terrainType = terrain.a;
			float3 densityNormal = terrain.xyz;
			fixed4 c = GetColor(IN.worldPos, IN.worldNormal, terrainType);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
