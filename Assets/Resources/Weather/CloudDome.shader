Shader "Weather/CloudDome"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { 
			"RenderType"="Transparent"
			"Queue"="Overlay"
	}
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100

		Pass
		{
			Cull Off
			ZWrite On

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
//			float4 unity_FogColor;
			
			// density / sqrt(ln(2)),
			// density / ln(2),
			//–1/(end-start),
			// end/(end-start)). x is useful for Exp2 fog mode, y for Exp mode, z and w for Linear mode.
//			float4 unity_FogParams;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float4 col = float4(0,0,0,0);
				UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,0,0,0));
				float ff = col.r;
				col.rgb = unity_FogColor.rgb;
				col.a = ff;
				return col;
			}
			ENDCG
		}
	}
}
