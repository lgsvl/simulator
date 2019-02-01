//
// TODO: support deferred rendering path

//
Shader "SegmentColorer"
{
    CGINCLUDE
        float4 vert(float4 vertex : POSITION) : SV_POSITION
        {
            return UnityObjectToClipPos(vertex);
        }
    ENDCG

    SubShader
    {
        Tags { "SegmentColor" = "Car" }
        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                fixed4 frag(float4 vertex : SV_POSITION) : SV_Target
                {
                    return  fixed4(0x12/255.0, 0x0E / 255.0, 0x97 / 255.0, 1.0);
                }
            ENDCG
        }
    }

    SubShader
    {
        Tags { "SegmentColor" = "Road" }
        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                fixed4 frag(float4 vertex : SV_POSITION) : SV_Target
                {
                    return  fixed4(0x7A / 255.0, 0x3F / 255.0, 0x83 / 255.0, 1.0);
                }
            ENDCG
        }
    }

    SubShader
    {
        Tags { "SegmentColor" = "Vegetation" }
        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                fixed4 frag(float4 vertex : SV_POSITION) : SV_Target
                {
                    return  fixed4(0x71 / 255.0, 0xC0 / 255.0, 0x2F / 255.0, 1.0);
                }
            ENDCG
        }
    }

    SubShader
    {
        Tags { "SegmentColor" = "Sidewalk" }
        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                fixed4 frag(float4 vertex : SV_POSITION) : SV_Target
                {
                    return  fixed4(0xBA / 255.0, 0x83 / 255.0, 0x50 / 255.0, 1.0);
                }
            ENDCG
        }
    }

    SubShader
    {
        Tags { "SegmentColor" = "Building" }
        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                fixed4 frag(float4 vertex : SV_POSITION) : SV_Target
                {
                    return  fixed4(0x23 / 255.0, 0x86 / 255.0, 0x88 / 255.0, 1.0);
                }
            ENDCG
        }
    }

    SubShader
    {
        Tags { "SegmentColor" = "Sign" }
        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                fixed4 frag(float4 vertex : SV_POSITION) : SV_Target
                {
                    return  fixed4(0xC0 / 255.0, 0xC0 / 255.0, 0x00 / 255.0, 1.0);
                }
            ENDCG
        }
    }

    SubShader
    {
        Tags { "SegmentColor" = "TrafficLight" }
        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                fixed4 frag(float4 vertex : SV_POSITION) : SV_Target
                {
                    return  fixed4(0xFF / 255.0, 0xFF / 255.0, 0x00 / 255.0, 1.0);
                }
            ENDCG
        }
    }

    SubShader
    {
        Tags { "SegmentColor" = "Obstacle" }
        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                fixed4 frag(float4 vertex : SV_POSITION) : SV_Target
                {
                    return  fixed4(0xFF / 255.0, 0xFF / 255.0, 0xFF / 255.0, 1.0);
                }
            ENDCG
        }
    }

    SubShader
    {
        Tags { "SegmentColor" = "Shoulder" }
        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                fixed4 frag(float4 vertex : SV_POSITION) : SV_Target
                {
                    return  fixed4(0xFF / 255.0, 0x00 / 255.0, 0xFF / 255.0, 1.0);
                }
            ENDCG
        }
    }

	SubShader
    {
        Tags { "SegmentColor" = "Pedestrian" }
        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                fixed4 frag(float4 vertex : SV_POSITION) : SV_Target
                {
                    return  fixed4(0xFF / 255.0, 0x00 / 255.0, 0x00 / 255.0, 1.0);
                }
            ENDCG
        }
    }
}
