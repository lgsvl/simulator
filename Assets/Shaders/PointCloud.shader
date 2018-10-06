Shader "Unlit/PointCloud"
{
	Properties
	{
        _Color("Color", Color) = (1,0,0,0)
		_Size ("Size", Float) = 0.04
        [Toggle(ConstantSize)]
        _ConstSize("Constant Size", Float) = 0
	}
	SubShader
	{
		Tags { "Queue"="AlphaTest" "RenderType"="Transparent" "IgnoreProjector"="True" }
		Blend One OneMinusSrcAlpha
		AlphaToMask On
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			
			#include "UnityCG.cginc"

            fixed4 _Color;
			float _Size;
            float _ConstSize;

			struct v2g
			{
				float4 vertex : POSITION;
				float3 normal	: NORMAL;
				float4 color	: COLOR;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
			};

			struct g2f {
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

            v2g vert (appdata_full v)
			{
                v2g o = (v2g)0;
				o.vertex = v.vertex;
				o.normal = v.normal;
				o.color = v.color;
				return o;
			}


			[maxvertexcount(3)]
			void geom (point v2g pt[1], inout TriangleStream<g2f> triStream)
			{
                g2f pIn = (g2f)0;
                pIn.normal = pt[0].normal;
				pIn.color = pt[0].color;

				float4 vertex = mul(unity_ObjectToWorld, pt[0].vertex);
                float3 vertToCam_World = _WorldSpaceCameraPos - vertex;
                float distance = length(vertToCam_World);
                float3 tangent = normalize(cross(float3(0, 1, 0), vertToCam_World));
				float3 up = normalize(cross(vertToCam_World, tangent));

                float4 vertex_proj = mul(UNITY_MATRIX_VP, vertex + float4(pIn.normal * 0.005, 0));               

                pIn.vertex = _ConstSize * (vertex_proj + float4(float3(1, 0, 0) * -_Size / 5.33 * vertex_proj.a / 1.5, 0)) +
                            (1 - _ConstSize) * (vertex_proj + mul(UNITY_MATRIX_VP, float4(tangent * -_Size / 1.5, 0)));		
				pIn.texcoord = float2(-0.5,0);
				triStream.Append(pIn);

                pIn.vertex = _ConstSize * (pIn.vertex = vertex_proj + float4(float3(0, -1, 0) * _Size / 5.33 * vertex_proj.a, 0)) +
                    (1 - _ConstSize) * (pIn.vertex = vertex_proj + mul(UNITY_MATRIX_VP, float4(up * _Size, 0)));
				pIn.texcoord = float2(0.5,1.5);
				triStream.Append(pIn);

                pIn.vertex = _ConstSize * (pIn.vertex = vertex_proj + float4(float3(1, 0, 0) * _Size / 5.33 * vertex_proj.a / 1.5, 0)) +
                    (1 - _ConstSize) * (pIn.vertex = vertex_proj + mul(UNITY_MATRIX_VP, float4(tangent * _Size / 1.5, 0)));
				pIn.texcoord = float2(1.5,0);
				triStream.Append(pIn);
			}

			float4 frag (g2f i) : COLOR
			{
				return _Color;
			}
			ENDCG
		}
	}
}
