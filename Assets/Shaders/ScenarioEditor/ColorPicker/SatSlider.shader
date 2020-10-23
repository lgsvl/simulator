Shader "Simulator/ColorPicker/SatSlider" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Hue ("Hue", Range (0.0, 1.0)) = 0.0
        _Val ("Val", Range (0.0, 1.0)) = 0.0
    }
    SubShader {
        Pass {

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            uniform float _Hue;
            uniform float _Val;

            struct v2f {
                float4 pos : SV_POSITION;
                fixed2 uv: TEXCOORD0;
            };
            
            float3 Hue(float H)
            {
                float R = abs(H * 6 - 3) - 1;
                float G = 2 - abs(H * 6 - 2);
                float B = 2 - abs(H * 6 - 4);
                return saturate(float3(R,G,B));
            }
 
            float4 HSVtoRGB(in float3 HSV)
            {
                return float4(((Hue(HSV.x) - 1) * HSV.y + 1) * HSV.z,1);
            }

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return HSVtoRGB(float3(_Hue, i.uv.x, _Val*2/3+0.33));
            }
            ENDCG

        }
    }
}