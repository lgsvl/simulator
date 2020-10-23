Shader "Simulator/ColorPicker/BlueSlider" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Red ("Red", Range (0.0, 1.0)) = 0.0
        _Green ("Green", Range (0.0, 1.0)) = 0.0
    }
    SubShader {
        Pass {

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            uniform float _Red;
            uniform float _Green;

            struct v2f {
                float4 pos : SV_POSITION;
                fixed2 uv: TEXCOORD0;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return float4(_Red, _Green, i.uv.x, 1);
            }
            ENDCG

        }
    }
}