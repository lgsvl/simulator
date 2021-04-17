/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    using UnityEditor.ShaderGraph;

    static class SVLShaderPasses
    {
        public static PassDescriptor GenerateLidar()
        {
            return new PassDescriptor
            {
                displayName = "SimulatorLidarPass",
                referenceName = "SHADERPASS_SIMULATOR_LIDAR",
                lightMode = "SimulatorLidarPass",
                validPixelBlocks = new[]
                {
                    BlockFields.SurfaceDescription.BaseColor,
                    BlockFields.SurfaceDescription.Metallic,
                    BlockFields.SurfaceDescription.Alpha,
                    BlockFields.SurfaceDescription.AlphaClipThreshold,
                    HDBlockFields.SurfaceDescription.DepthOffset
                },
                validVertexBlocks = new[]
                {
                    BlockFields.VertexDescription.Position
                },
                requiredFields = CoreRequiredFields.PositionRWS,
                pragmas = CorePragmas.Basic,
                defines = new DefineCollection()
                {
                    { new KeywordDescriptor()
                    {
                        displayName = "Mask Map",
                        referenceName = "_MASKMAP",
                        type = KeywordType.Boolean,
                        definition = KeywordDefinition.Predefined,
                        scope = KeywordScope.Local,
                    }, 0 }
                },
                keywords = new KeywordCollection(),
                includes = new IncludeCollection
                {
                    { CoreIncludes.CorePregraph },
                    { CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph },
                    { CoreIncludes.CoreUtility },
                    { CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph },
                    { "Assets/Shaders/LidarPass.hlsl", IncludeLocation.Postgraph }
                }
            };
        }

        public static PassDescriptor GenerateSegmentation()
        {
            return new PassDescriptor
            {
                displayName = "SimulatorSegmentationPass",
                referenceName = "SHADERPASS_SIMULATOR_SEGMENTATION",
                lightMode = "SimulatorSegmentationPass",
                validPixelBlocks = new[]
                {
                    BlockFields.SurfaceDescription.BaseColor,
                    BlockFields.SurfaceDescription.Alpha,
                    BlockFields.SurfaceDescription.AlphaClipThreshold,
                    HDBlockFields.SurfaceDescription.DepthOffset
                },
                validVertexBlocks = new[]
                {
                    BlockFields.VertexDescription.Position
                },
                requiredFields = CoreRequiredFields.PositionRWS,
                pragmas = CorePragmas.Basic,
                defines = new DefineCollection(),
                keywords = new KeywordCollection(),
                includes = new IncludeCollection
                {
                    { CoreIncludes.CorePregraph },
                    { CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph },
                    { CoreIncludes.CoreUtility },
                    { CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph },
                    { "Assets/Shaders/SegmentationPass.hlsl", IncludeLocation.Postgraph }
                }
            };
        }

        public static PassDescriptor GenerateDepth()
        {
            return new PassDescriptor
            {
                displayName = "SimulatorDepthPass",
                referenceName = "SHADERPASS_SIMULATOR_DEPTH",
                lightMode = "SimulatorDepthPass",
                validPixelBlocks = new[]
                {
                    BlockFields.SurfaceDescription.BaseColor,
                    BlockFields.SurfaceDescription.Alpha,
                    BlockFields.SurfaceDescription.AlphaClipThreshold,
                    HDBlockFields.SurfaceDescription.DepthOffset
                },
                validVertexBlocks = new[]
                {
                    BlockFields.VertexDescription.Position
                },
                requiredFields = CoreRequiredFields.PositionRWS,
                pragmas = CorePragmas.Basic,
                defines = new DefineCollection(),
                keywords = new KeywordCollection(),
                includes = new IncludeCollection
                {
                    { CoreIncludes.CorePregraph },
                    { CoreIncludes.kPassPlaceholder, IncludeLocation.Pregraph },
                    { CoreIncludes.CoreUtility },
                    { CoreIncludes.kPostDecalsPlaceholder, IncludeLocation.Pregraph },
                    { "Assets/Shaders/DepthPass.hlsl", IncludeLocation.Postgraph }
                }
            };
        }
    }
}