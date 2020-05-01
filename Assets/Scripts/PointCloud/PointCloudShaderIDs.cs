/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud
{
    using UnityEngine;

    public static class PointCloudShaderIDs
    {
        public static class PointsRender
        {
            public const string SizeInPixelsKeyword = "_SIZE_IN_PIXELS";
            public const string ConesKeyword = "_CONES";
            public static readonly int StencilRef = Shader.PropertyToID("_StencilRefGBuffer");
            public static readonly int StencilMask = Shader.PropertyToID("_StencilWriteMaskGBuffer");
            public static readonly int ModelMatrix = Shader.PropertyToID("_Transform");
            public static readonly int VPMatrix = Shader.PropertyToID("_ViewProj");
            public static readonly int MinHeight = Shader.PropertyToID("_MinHeight");
            public static readonly int MaxHeight = Shader.PropertyToID("_MaxHeight");
            public static readonly int Size = Shader.PropertyToID("_Size");
            public static readonly int MinSize = Shader.PropertyToID("_MinSize");
            public static readonly int ShadowVector = Shader.PropertyToID("_PCShadowVector");
        }

        public static class Shared
        {
            public static readonly int Buffer = Shader.PropertyToID("_Buffer");
            public static readonly int Colorize = Shader.PropertyToID("_Colorize");
            public static readonly int MinHeight = Shader.PropertyToID("_MinHeight");
            public static readonly int MaxHeight = Shader.PropertyToID("_MaxHeight");
        }

        public static class SolidRender
        {
            public static readonly int MVPMatrix = Shader.PropertyToID("_PointCloudMVP");
        }

        public static class SolidCompose
        {
            public const string TargetGBufferKeyword = "_PC_TARGET_GBUFFER";
            public const string UnlitShadowsKeyword = "_PC_UNLIT_SHADOWS";
            public const string LinearDepthKeyword = "_PC_LINEAR_DEPTH";
            public static readonly int ColorTexture = Shader.PropertyToID("_ColorTex");
            public static readonly int NormalTexture = Shader.PropertyToID("_NormalDepthTex");
            public static readonly int OriginalDepth = Shader.PropertyToID("_OriginalDepth");
            public static readonly int ReprojectionVector = Shader.PropertyToID("_SRMulVec");
            public static readonly int UnlitShadowsFilter = Shader.PropertyToID("_ShadowsFilter");
        }

        public static class SolidCompute
        {
            public static readonly int TextureSize = Shader.PropertyToID("_TexSize");
            public static readonly int FullRTSize = Shader.PropertyToID("_FullRTSize");
            public static readonly int MipTextureSize = Shader.PropertyToID("_MipTexSize");
            public static readonly int HigherMipTextureSize = Shader.PropertyToID("_HigherMipTexSize");
            public static readonly int FarPlane = Shader.PropertyToID("_FarPlane");
            public static readonly int ProjectionMatrix = Shader.PropertyToID("_Proj");
            public static readonly int InverseProjectionMatrix = Shader.PropertyToID("_InverseProj");
            public static readonly int InverseVPMatrix = Shader.PropertyToID("_InverseVP");
            public static readonly int InverseReprojectionVector = Shader.PropertyToID("_InverseSRMulVec");

            public static class SetupCopy
            {
                public const string KernelName = "SetupCopy";
                public const string KernelNameFF = "SetupCopyFF";
                public const string KernelNameLinearDepth = "SetupCopyLinearDepth";
                public const string KernelNameLinearDepthFF = "SetupCopyLinearDepthFF";
                public const string KernelNameSky = "SetupCopySky";
                public const string KernelNameFFSky = "SetupCopyFFSky";
                public const string KernelNameLinearDepthSky = "SetupCopyLinearDepthSky";
                public const string KernelNameLinearDepthFFSky = "SetupCopyLinearDepthFFSky";
                public static readonly int InputColor = Shader.PropertyToID("_SetupCopyInput");
                public static readonly int InputPosition = Shader.PropertyToID("_SetupCopyInputPos");
                public static readonly int OutputColor = Shader.PropertyToID("_SetupCopyColor");
                public static readonly int OutputPosition = Shader.PropertyToID("_SetupCopyPosition");
                public static readonly int PostSkyPreRenderTexture = Shader.PropertyToID("_PostSkyPreRenderColorTexture");
                public static readonly int HorizonThreshold = Shader.PropertyToID("_HorizonThreshold");
            }

            public static class Downsample
            {
                public const string KernelName = "Downsample";
                public static readonly int InputPosition = Shader.PropertyToID("_DownsampleInput");
                public static readonly int OutputPosition = Shader.PropertyToID("_DownsampleOutput");
            }

            public static class RemoveHidden
            {
                public const string KernelName = "RemoveHidden";
                public const string DebugKernelName = "RemoveHiddenDebug";
                public static readonly int LevelCount = Shader.PropertyToID("_RemoveHiddenLevelCount");
                public static readonly int Position = Shader.PropertyToID("_RemoveHiddenPosition");
                public static readonly int Color = Shader.PropertyToID("_RemoveHiddenColor");
                public static readonly int DepthBuffer = Shader.PropertyToID("_RemoveHiddenDepthBuffer");
                public static readonly int CascadesOffset = Shader.PropertyToID("_RemoveHiddenCascadesOffset");
                public static readonly int CascadesSize = Shader.PropertyToID("_RemoveHiddenCascadesSize");
                public static readonly int FixedLevel = Shader.PropertyToID("_RemoveHiddenLevel");
            }

            public static class PullKernel
            {
                public const string KernelName = "PullKernel";
                public static readonly int InputLevel = Shader.PropertyToID("_PullInputLevel");
                public static readonly int FilterExponent = Shader.PropertyToID("_PullFilterParam");
                public static readonly int SkipWeightMul = Shader.PropertyToID("_PullSkipWeightMul");
                public static readonly int InputColor = Shader.PropertyToID("_PullColorInput");
                public static readonly int OutputColor = Shader.PropertyToID("_PullColorOutput");
            }

            public static class PushKernel
            {
                public const string KernelName = "PushKernel";
                public static readonly int InputLevel = Shader.PropertyToID("_PushInputLevel");
                public static readonly int InputColor = Shader.PropertyToID("_PushColorInput");
                public static readonly int OutputColor = Shader.PropertyToID("_PushColorOutput");
            }

            public static class CalculateNormals
            {
                public const string KernelName = "CalculateNormals";
                public const string KernelNameLinearDepth = "CalculateNormalsLinearDepth";
                public static readonly int InputLevel = Shader.PropertyToID("_CalcNormalsInputLevel");
                public static readonly int Input = Shader.PropertyToID("_NormalsIn");
                public static readonly int Output = Shader.PropertyToID("_NormalsOut");
            }

            public static class SmoothNormals
            {
                public const string KernelName = "SmoothNormals";
                public const string KernelNameLinearDepth = "SmoothNormalsLinearDepth";
                public const string DebugKernelName = "SmoothNormalsDebug";
                public const string DebugKernelNameLinearDepth = "SmoothNormalsLinearDepthDebug";
                public static readonly int Input = Shader.PropertyToID("_SmoothNormalsIn");
                public static readonly int Output = Shader.PropertyToID("_SmoothNormalsOut");
                public static readonly int CascadesOffset = Shader.PropertyToID("_SmoothNormalsCascadesOffset");
                public static readonly int CascadesSize = Shader.PropertyToID("_SmoothNormalsCascadesSize");
                public static readonly int ColorDebug = Shader.PropertyToID("_SmoothNormalsColorDebug");
            }
        }

        public static class SHCoefficients
        {
            public static readonly int SHAr = Shader.PropertyToID("PC_SHAr");
            public static readonly int SHAg = Shader.PropertyToID("PC_SHAg");
            public static readonly int SHAb = Shader.PropertyToID("PC_SHAb");
            public static readonly int SHBr = Shader.PropertyToID("PC_SHBr");
            public static readonly int SHBg = Shader.PropertyToID("PC_SHBg");
            public static readonly int SHBb = Shader.PropertyToID("PC_SHBb");
            public static readonly int SHC = Shader.PropertyToID("PC_SHC");

            public static readonly int[] SHA = {SHAr, SHAg, SHAb};
            public static readonly int[] SHB = {SHBr, SHBg, SHBb};
        }
    }
}
