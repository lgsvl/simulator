Shader "Simulator/SegmentationLine"
{
    Properties
    {
        _SegmentationColor("Segmentation Color", Color) = (1, 1, 1, 1)
    }

    HLSLINCLUDE

    #pragma target 4.5

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"

    CBUFFER_START(UnityPerMaterial)

    // Following two variables are feeded by the C++ Editor for Scene selection
    int _ObjectId;
    int _PassValue;

    float4 _SegmentationColor;

    CBUFFER_END


    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Opaque" }

        Pass
        {
            Name "SimulatorSegmentationPass"
            Tags { "LightMode" = "SimulatorSegmentationPass" }

            HLSLPROGRAM

            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Assets/Shaders/SegmentationPass.hlsl"

            ENDHLSL
        }
    }
}
