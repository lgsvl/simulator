Shader "Hidden/FbxCompatibilityShader"
{
    Properties
    {
        _MainTex("_MainTex", 2D) = "white" {}
        _BumpMap("_BumpMap", 2D) = "bump" {}
        _EmissionMap("_EmissionMap", 2D) = "black" {}
        _Color("_Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _BumpScale("_BumpScale", Float) = 1
        _EmissionColor("_EmissionColor", Color) = (0, 0, 0, 0)
    }
    SubShader {
      Pass {
         CGPROGRAM

         #pragma vertex vert 
         #pragma fragment frag

         float4 vert(float4 vertexPos : POSITION) : SV_POSITION 
         {
            return UnityObjectToClipPos(vertexPos);
         }

         float4 frag(void) : COLOR
         {
            return float4(1.0, 0.0, 0.0, 1.0); 
         }

         ENDCG
      }
   }
}
