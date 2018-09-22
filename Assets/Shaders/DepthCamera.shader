Shader "DepthCamera"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 depth : DEPTH;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.depth = o.vertex.zw;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float d = Linear01Depth(i.depth.x / i.depth.y);
                // float4 color = tex2D(_MainTex, i.uv);
                // float intensity = (color.r + color.g + color.b) / 3;
                // intensity = intensity * pow(1.0 - depth, 8));
                d = pow(d, 0.4);
                return float4(d, d, d, 1);
            }
            ENDCG
        }
    }
}
