Shader "Unlit/PointCloud"
{
    Properties
    {
        _Color("Color", Color) = (1,0,0,0)
        _Size ("Size", Float) = 0.04
        [Toggle(ConstantSize)]
        _ConstSize("Constant Size", Float) = 1
    }
    SubShader
    {
        Pass
        {
            Cull Off

            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Size;
            float _ConstSize;

            float4x4 _LidarToWorld;

            StructuredBuffer<float4> PointCloud;

            struct g2f
            {
                float4 vertex : SV_POSITION;
            };

            float4 vert(uint id : SV_VertexID) : POSITION
            {
                return mul(_LidarToWorld, float4(PointCloud[id].xyz, 1));
            }

            [maxvertexcount(3)]
            void geom (point float4 pt[1] : POSITION,  inout TriangleStream<g2f> triStream)
            {
                g2f output;

                // lidar ray did not hit anything
                if (dot(pt[0], pt[0]) == 0) return;

                float4 vertex = pt[0];
                float3 vertToCam_World = _WorldSpaceCameraPos - vertex;
                float3 tangent = normalize(cross(float3(0, 1, 0), vertToCam_World));
                float3 up = normalize(cross(vertToCam_World, tangent));

                float4 vertex_view = mul(UNITY_MATRIX_V, vertex) + float4(0, 0, 0.1, 0);
                float4 vertex_proj = mul(UNITY_MATRIX_P, vertex_view);

                output.vertex = _ConstSize * (vertex_proj + float4(float3(1, 0, 0) * -_Size / 5.33 * vertex_proj.a / 1.5, 0)) +
                            (1 - _ConstSize) * (vertex_proj + mul(UNITY_MATRIX_VP, float4(tangent * -_Size / 1.5, 0)));		
                triStream.Append(output);

                output.vertex = _ConstSize * (vertex_proj + float4(float3(0, -1, 0) * _Size / 5.33 * vertex_proj.a, 0)) +
                    (1 - _ConstSize) * (vertex_proj + mul(UNITY_MATRIX_VP, float4(up * _Size, 0)));
                triStream.Append(output);

                output.vertex = _ConstSize * (vertex_proj + float4(float3(1, 0, 0) * _Size / 5.33 * vertex_proj.a / 1.5, 0)) +
                    (1 - _ConstSize) * (vertex_proj + mul(UNITY_MATRIX_VP, float4(tangent * _Size / 1.5, 0)));
                triStream.Append(output);
            }

            float4 frag (g2f i) : COLOR
            {
                return _Color;
            }
            ENDCG
        }
    }
}
