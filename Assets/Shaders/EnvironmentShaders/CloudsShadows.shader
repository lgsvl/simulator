/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

Shader "Simulator/CloudsShadowsRenderTexture"
{
	Properties
	{
		_cloudDensity("CloudDensity", Range(0, 2)) = 1
		_cloudSize("CloudSize", Range(0.01, 2)) = 0.5
		_cloudSpeed("CloudSpeed", Range(0, 1)) = 0
		_cloudCover("CloudCover", Range(0, 1)) = 0
		_cloudShadow("CloudShadowIntensity", Range(0, 1)) = 0.5
	}

		SubShader
	{
		Lighting Off
		Blend One Zero

		Pass
		{
			CGPROGRAM
			#include "UnityCustomRenderTexture.cginc"

			#pragma vertex InitCustomRenderTextureVertexShader
			#pragma fragment frag
			#pragma target 3.0

					CBUFFER_START(UnityPerMaterial)
					float _cloudDensity;
					float _cloudSize;
					float _cloudSpeed;
					float _cloudCover;
					float _cloudShadow;
					CBUFFER_END


						// Pixel Graph Inputs
							struct SurfaceDescriptionInputs {
								float4 uv0; // optional
							};
					// Pixel Graph Outputs
						struct SurfaceDescription
						{
							float3 Color;
						};

						// Shared Graph Node Functions

							void Unity_Multiply_float(float A, float B, out float Out)
							{
								Out = A * B;
							}

							void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
							{
								Out = UV * Tiling + Offset;
							}


			inline float2 unity_voronoi_noise_randomVector(float2 UV, float offset)
			{
				float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
				UV = frac(sin(mul(UV, m)) * 46839.32);
				return float2(sin(UV.y*+offset)*0.5 + 0.5, cos(UV.x*offset)*0.5 + 0.5);
			}

							void Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
							{
								float2 g = floor(UV * CellDensity);
								float2 f = frac(UV * CellDensity);
								float t = 8.0;
								float3 res = float3(8.0, 0.0, 0.0);

								for (int y = -1; y <= 1; y++)
								{
									for (int x = -1; x <= 1; x++)
									{
										float2 lattice = float2(x,y);
										float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset);
										float d = distance(lattice + offset, f);

										if (d < res.x)
										{

											res = float3(d, offset.x, offset.y);
											Out = res.x;
											Cells = res.y;

										}
									}

								}

							}

							void Unity_OneMinus_float(float In, out float Out)
							{
								Out = 1 - In;
							}

							void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
							{
								Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
							}


			inline float unity_noise_randomValue(float2 uv)
			{
				return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);
			}

			inline float unity_noise_interpolate(float a, float b, float t)
			{
				return (1.0 - t)*a + (t*b);
			}


			inline float unity_valueNoise(float2 uv)
			{
				float2 i = floor(uv);
				float2 f = frac(uv);
				f = f * f * (3.0 - 2.0 * f);

				uv = abs(frac(uv) - 0.5);
				float2 c0 = i + float2(0.0, 0.0);
				float2 c1 = i + float2(1.0, 0.0);
				float2 c2 = i + float2(0.0, 1.0);
				float2 c3 = i + float2(1.0, 1.0);
				float r0 = unity_noise_randomValue(c0);
				float r1 = unity_noise_randomValue(c1);
				float r2 = unity_noise_randomValue(c2);
				float r3 = unity_noise_randomValue(c3);

				float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
				float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
				float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
				return t;
			}
							void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
							{
								float t = 0.0;

								float freq = pow(2.0, float(0));
								float amp = pow(0.5, float(3 - 0));
								t += unity_valueNoise(float2(UV.x*Scale / freq, UV.y*Scale / freq))*amp;

								freq = pow(2.0, float(1));
								amp = pow(0.5, float(3 - 1));
								t += unity_valueNoise(float2(UV.x*Scale / freq, UV.y*Scale / freq))*amp;

								freq = pow(2.0, float(2));
								amp = pow(0.5, float(3 - 2));
								t += unity_valueNoise(float2(UV.x*Scale / freq, UV.y*Scale / freq))*amp;

								Out = t;
							}

							void Unity_Power_float(float A, float B, out float Out)
							{
								Out = pow(A, B);
							}

							void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
							{
								Out = smoothstep(Edge1, Edge2, In);
							}

							void Unity_Add_float(float A, float B, out float Out)
							{
								Out = A + B;
							}

							void Unity_Lerp_float(float A, float B, float T, out float Out)
							{
								Out = lerp(A, B, T);
							}

							void Unity_Round_float(float In, out float Out)
							{
								Out = round(In);
							}

							// Pixel Graph Evaluation
								SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
								{
									SurfaceDescription surface = (SurfaceDescription)0;
									float _Property_EC042A9A_Out = _cloudSize;
									float _Multiply_ABC9EC28_Out;
									Unity_Multiply_float(_Property_EC042A9A_Out, 0.1, _Multiply_ABC9EC28_Out);

									float _Property_BACBE5EF_Out = _cloudSpeed;
									float _Multiply_2033AE0C_Out;
									Unity_Multiply_float(_Property_BACBE5EF_Out, _Time.y, _Multiply_2033AE0C_Out);

									float2 _TilingAndOffset_EF7CE224_Out;
									Unity_TilingAndOffset_float(IN.uv0.xy, (_Multiply_ABC9EC28_Out.xx), (_Multiply_2033AE0C_Out.xx), _TilingAndOffset_EF7CE224_Out);
									float _Voronoi_76DF9F96_Out;
									float _Voronoi_76DF9F96_Cells;
									Unity_Voronoi_float(_TilingAndOffset_EF7CE224_Out, 5, 5, _Voronoi_76DF9F96_Out, _Voronoi_76DF9F96_Cells);
									float _OneMinus_5830923F_Out;
									Unity_OneMinus_float(_Voronoi_76DF9F96_Out, _OneMinus_5830923F_Out);
									float _Remap_D5B5F2EF_Out;
									Unity_Remap_float(_OneMinus_5830923F_Out, float2 (0,1), float2 (0.25,1), _Remap_D5B5F2EF_Out);
									float2 _TilingAndOffset_83EFB652_Out;
									Unity_TilingAndOffset_float(IN.uv0.xy, float2 (1,1), (_Multiply_2033AE0C_Out.xx), _TilingAndOffset_83EFB652_Out);
									float _SimpleNoise_9E256899_Out;
									Unity_SimpleNoise_float(_TilingAndOffset_83EFB652_Out, 20, _SimpleNoise_9E256899_Out);
									float _Power_E84AFA7A_Out;
									Unity_Power_float(_SimpleNoise_9E256899_Out, 1.5, _Power_E84AFA7A_Out);
									float _Multiply_EF047494_Out;
									Unity_Multiply_float(_Remap_D5B5F2EF_Out, _Power_E84AFA7A_Out, _Multiply_EF047494_Out);

									float _Smoothstep_F53ED8EF_Out;
									Unity_Smoothstep_float(0.25, 0.75, _Voronoi_76DF9F96_Out, _Smoothstep_F53ED8EF_Out);
									float _Smoothstep_BCDECAA1_Out;
									Unity_Smoothstep_float(0.25, 0.75, _SimpleNoise_9E256899_Out, _Smoothstep_BCDECAA1_Out);
									float _Multiply_2E3DEA3C_Out;
									Unity_Multiply_float(_Smoothstep_F53ED8EF_Out, _Smoothstep_BCDECAA1_Out, _Multiply_2E3DEA3C_Out);

									float _Multiply_5C120E2E_Out;
									Unity_Multiply_float(0.25, _Multiply_2E3DEA3C_Out, _Multiply_5C120E2E_Out);

									float _Add_F0B81ADD_Out;
									Unity_Add_float(_Multiply_EF047494_Out, _Multiply_5C120E2E_Out, _Add_F0B81ADD_Out);
									float _Property_86D526FD_Out = _cloudDensity;
									float _Multiply_C0B9D314_Out;
									Unity_Multiply_float(_Add_F0B81ADD_Out, _Property_86D526FD_Out, _Multiply_C0B9D314_Out);

									float _Property_52767D38_Out = _cloudSize;
									float _Multiply_EF97AE4B_Out;
									Unity_Multiply_float(_Property_52767D38_Out, 0.3, _Multiply_EF97AE4B_Out);

									float _Property_7E8AF415_Out = _cloudSpeed;
									float _Multiply_98DDA047_Out;
									Unity_Multiply_float(_Property_7E8AF415_Out, _Time.y, _Multiply_98DDA047_Out);

									float _Multiply_D47639F_Out;
									Unity_Multiply_float(_Multiply_98DDA047_Out, 0.9, _Multiply_D47639F_Out);

									float2 _TilingAndOffset_C8D790FE_Out;
									Unity_TilingAndOffset_float(IN.uv0.xy, (_Multiply_EF97AE4B_Out.xx), (_Multiply_D47639F_Out.xx), _TilingAndOffset_C8D790FE_Out);
									float _Voronoi_CDE8636E_Out;
									float _Voronoi_CDE8636E_Cells;
									Unity_Voronoi_float(_TilingAndOffset_C8D790FE_Out, 8, 8, _Voronoi_CDE8636E_Out, _Voronoi_CDE8636E_Cells);
									float _OneMinus_CCCDDA42_Out;
									Unity_OneMinus_float(_Voronoi_CDE8636E_Out, _OneMinus_CCCDDA42_Out);
									float _Remap_688D34D5_Out;
									Unity_Remap_float(_OneMinus_CCCDDA42_Out, float2 (0,1), float2 (0.25,1), _Remap_688D34D5_Out);
									float _Multiply_6C1C020B_Out;
									Unity_Multiply_float(_Multiply_D47639F_Out, 0.8, _Multiply_6C1C020B_Out);

									float2 _TilingAndOffset_3E133F35_Out;
									Unity_TilingAndOffset_float(IN.uv0.xy, (_Multiply_EF97AE4B_Out.xx), (_Multiply_6C1C020B_Out.xx), _TilingAndOffset_3E133F35_Out);
									float _SimpleNoise_887E0B1F_Out;
									Unity_SimpleNoise_float(_TilingAndOffset_3E133F35_Out, 16, _SimpleNoise_887E0B1F_Out);
									float _Power_52D1B9F7_Out;
									Unity_Power_float(_SimpleNoise_887E0B1F_Out, 1.5, _Power_52D1B9F7_Out);
									float _Multiply_B13FDE2A_Out;
									Unity_Multiply_float(_Remap_688D34D5_Out, _Power_52D1B9F7_Out, _Multiply_B13FDE2A_Out);

									float _Smoothstep_AA759182_Out;
									Unity_Smoothstep_float(0.25, 0.75, _Voronoi_CDE8636E_Out, _Smoothstep_AA759182_Out);
									float _Smoothstep_DE837DEA_Out;
									Unity_Smoothstep_float(0.25, 0.75, _SimpleNoise_887E0B1F_Out, _Smoothstep_DE837DEA_Out);
									float _Multiply_E58514F9_Out;
									Unity_Multiply_float(_Smoothstep_AA759182_Out, _Smoothstep_DE837DEA_Out, _Multiply_E58514F9_Out);

									float _Multiply_E320FDA_Out;
									Unity_Multiply_float(0.25, _Multiply_E58514F9_Out, _Multiply_E320FDA_Out);

									float _Add_37D3F77C_Out;
									Unity_Add_float(_Multiply_B13FDE2A_Out, _Multiply_E320FDA_Out, _Add_37D3F77C_Out);
									float _Property_65EC89B3_Out = _cloudDensity;
									float _Multiply_74862887_Out;
									Unity_Multiply_float(_Add_37D3F77C_Out, _Property_65EC89B3_Out, _Multiply_74862887_Out);

									float _Multiply_86058AA2_Out;
									Unity_Multiply_float(_Multiply_C0B9D314_Out, _Multiply_74862887_Out, _Multiply_86058AA2_Out);

									float _Property_3F1015BF_Out = _cloudCover;
									float _Lerp_A0734998_Out;
									Unity_Lerp_float(_Multiply_86058AA2_Out, 0.9, _Property_3F1015BF_Out, _Lerp_A0734998_Out);
									float _Round_56B1F71D_Out;
									Unity_Round_float(_Lerp_A0734998_Out, _Round_56B1F71D_Out);
									surface.Color = (_Round_56B1F71D_Out.xxx);
									return surface;
								}

								float4 frag(v2f_init_customrendertexture IN) : COLOR
								{
									SurfaceDescriptionInputs i;
									i.uv0 = IN.texcoord.xyzz;

									SurfaceDescription o = SurfaceDescriptionFunction(i);
									return 1 - min(_cloudShadow, o.Color.xxxx);
								}

			ENDCG
		}
	}
}
