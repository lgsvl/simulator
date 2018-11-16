Shader "Custom/Skybox2CubeBlend" {
	Properties {
    _Blend ("Blend", Range (0, 1) ) = 0.0
    _Rotation ("Rotation", Range(0, 360)) = 0
    _Tex ("Cubemap   (HDR)", Cube) = "grey" {}
    _OverlayTex ("CubemapOverlay (HDR)", Cube) = "grey" {}
}

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off

    Pass {

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0

        #include "UnityCG.cginc"

        samplerCUBE _Tex;
        samplerCUBE _OverlayTex;
        half4 _Tex_HDR;
        half4 _Tint;
        half _Exposure;
        float _Rotation;
        float _Blend;

        float3 RotateAroundYInDegrees (float3 vertex, float degrees)
        {
            float alpha = degrees * UNITY_PI / 180.0;
            float sina, cosa;
            sincos(alpha, sina, cosa);
            float2x2 m = float2x2(cosa, -sina, sina, cosa);
            return float3(mul(m, vertex.xz), vertex.y).xzy;
        }

        struct appdata_t {
            float4 vertex : POSITION;
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
        };

        v2f vert (appdata_t v)
        {
            v2f o;
            float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
            o.vertex = UnityObjectToClipPos(rotated);
            o.texcoord = v.vertex.xyz;
            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
            half4 tex = texCUBE (_Tex, i.texcoord);
            half4 tex2 = texCUBE (_OverlayTex, i.texcoord);
            float4 env = lerp( tex, tex2, _Blend );

            half3 c = DecodeHDR (env, _Tex_HDR);

            return half4(c, 1);
        }
        ENDCG 
    }
}   
Fallback Off

}