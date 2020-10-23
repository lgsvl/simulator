Shader "Simulator/ColorPicker/HueSlider" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader {
        Pass {

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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
                return HSVtoRGB(float3(i.uv.x, 1, 1));
            }
            ENDCG

        }
    }
}