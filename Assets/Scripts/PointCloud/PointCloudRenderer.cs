/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud
{
    using System;
    using UnityEngine;
    using UnityEngine.Experimental.Rendering;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;
    using UnityEngine.Serialization;
    using Utilities;

    [ExecuteInEditMode]
    public abstract class PointCloudRenderer : MonoBehaviour
    {
        #region member_types

        private static class ShaderIDs
        {
            public static class PointsRender
            {
                public const string SizeInPixelsKeyword = "_SIZE_IN_PIXELS";
                public const string ConesKeyword = "_CONES";
                public static readonly int StencilRef = Shader.PropertyToID("_StencilRefGBuffer");
                public static readonly int StencilMask = Shader.PropertyToID("_StencilWriteMaskGBuffer");
                public static readonly int Buffer = Shader.PropertyToID("_Buffer");
                public static readonly int ModelMatrix = Shader.PropertyToID("_Transform");
                public static readonly int VPMatrix = Shader.PropertyToID("_ViewProj");
                public static readonly int Colorize = Shader.PropertyToID("_Colorize");
                public static readonly int MinHeight = Shader.PropertyToID("_MinHeight");
                public static readonly int MaxHeight = Shader.PropertyToID("_MaxHeight");
                public static readonly int Size = Shader.PropertyToID("_Size");
                public static readonly int MinSize = Shader.PropertyToID("_MinSize");
                public static readonly int ShadowVector = Shader.PropertyToID("_PCShadowVector");
            }

            public static class SolidRender
            {
                public static readonly int Buffer = Shader.PropertyToID("_Buffer");
                public static readonly int Colorize = Shader.PropertyToID("_Colorize");
                public static readonly int MinHeight = Shader.PropertyToID("_MinHeight");
                public static readonly int MaxHeight = Shader.PropertyToID("_MaxHeight");
                public static readonly int MVPMatrix = Shader.PropertyToID("_PointCloudMVP");
            }

            public static class SolidCompose
            {
                public const string TargetGBufferKeyword = "_PC_TARGET_GBUFFER";
                public const string LinearDepthKeyword = "_PC_LINEAR_DEPTH";
                public static readonly int ColorTexture = Shader.PropertyToID("_ColorTex");
                public static readonly int NormalDepthTexture = Shader.PropertyToID("_NormalDepthTex");
                public static readonly int OriginalDepth = Shader.PropertyToID("_OriginalDepth");
                public static readonly int OriginalLidarDepth = Shader.PropertyToID("_OriginalLidarDepth");
                public static readonly int ReprojectionVector = Shader.PropertyToID("_SRMulVec");
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

                    public static string GetKernelName(bool linearDepth, bool forceFill, bool blendSky)
                    {
                        if (linearDepth)
                        {
                            if (blendSky)
                                return forceFill ? KernelNameLinearDepthFFSky : KernelNameLinearDepthSky;
                            else
                                return forceFill ? KernelNameLinearDepthFF : KernelNameLinearDepth;
                        }
                        else
                        {
                            if (blendSky) 
                                return forceFill ? KernelNameFFSky : KernelNameSky;
                            else
                                return forceFill ? KernelNameFF : KernelName;
                        }
                    }
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

                /*
                public static class ApplyPreviousFrame
                {
                    public const string KernelName = "ApplyPreviousFrame";
                    public static readonly int SavedColor = Shader.PropertyToID("_PrevColorSaved");
                    public static readonly int CurrentColor = Shader.PropertyToID("_PrevColorCurrent");
                    public static readonly int CurrentColorIn = Shader.PropertyToID("_PrevColorCurrentIn");
                    public static readonly int SavedPos = Shader.PropertyToID("_PrevPosSaved");
                    public static readonly int CurrentPos = Shader.PropertyToID("_PrevPosCurrent");
                    public static readonly int CurrentPosIn = Shader.PropertyToID("_PrevPosCurrentIn");
                    public static readonly int PrevToCurrentMatrix = Shader.PropertyToID("_PrevToCurrentMatrix");
                    public static readonly int FramePersistence = Shader.PropertyToID("_FramePersistence");
                }
                */

                public static class PullKernel
                {
                    public const string KernelName = "PullKernel";
                    public static readonly int InputLevel = Shader.PropertyToID("_PullInputLevel");
                    public static readonly int FilterExponent = Shader.PropertyToID("_PullFilterParam");
                    public static readonly int SkipWeightMul = Shader.PropertyToID("_PullSkipWeightMul");
                    public static readonly int InputColor = Shader.PropertyToID("_PullColorInput");
                    public static readonly int OutputColor = Shader.PropertyToID("_PullColorOutput");
                    public static readonly int InputDepth = Shader.PropertyToID("_PullDepthBufferInput");
                    public static readonly int OutputDepth = Shader.PropertyToID("_PullDepthBufferOutput");
                }

                public static class PushKernel
                {
                    public const string KernelName = "PushKernel";
                    public static readonly int InputLevel = Shader.PropertyToID("_PushInputLevel");
                    public static readonly int InputColor = Shader.PropertyToID("_PushColorInput");
                    public static readonly int OutputColor = Shader.PropertyToID("_PushColorOutput");
                    public static readonly int InputDepth = Shader.PropertyToID("_PushDepthBufferInput");
                    public static readonly int OutputDepth = Shader.PropertyToID("_PushDepthBufferOutput");
                }

                public static class CalculateNormals
                {
                    public const string KernelName = "CalculateNormals";
                    public const string KernelNameLinearDepth = "CalculateNormalsLinearDepth";
                    public static readonly int InputLevel = Shader.PropertyToID("_CalcNormalsInputLevel");
                    public static readonly int InputOutput = Shader.PropertyToID("_NormalsInOut");
                    public static readonly int Input = Shader.PropertyToID("_NormalsIn");
                    public static readonly int Output = Shader.PropertyToID("_NormalsOut");

                    public static string GetKernelName(bool linearDepth)
                    {
                        return linearDepth ? KernelNameLinearDepth : KernelName;
                    }
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

                    public static string GetKernelName(bool linearDepth, bool debug)
                    {
                        if (linearDepth)
                            return debug ? DebugKernelNameLinearDepth : KernelNameLinearDepth;
                        else
                            return debug ? DebugKernelName : KernelName;
                    }
                }
            }
        }

        public enum RenderType
        {
            Points,
            Solid,
            Cones
        }

        public enum ColorizeType
        {
            Colors = 0,
            Intensity = 1,
            RainbowIntensity = 2,
            RainbowHeight = 3,
        }

        [Flags]
        public enum RenderMask
        {
            None = 0,
            Camera = 1,
            Shadows = 1 << 1,
            Lidar = 1 << 2,
            Default = Camera | Lidar
        }

        #endregion

        public ColorizeType Colorize = ColorizeType.RainbowIntensity;

        [FormerlySerializedAs("Render")]
        public RenderType RenderMode = RenderType.Points;

        public RenderMask Mask = RenderMask.Default;

        public bool ConstantSize;

        [Range(1, 32)]
        public float PixelSize = 6.0f;

        [Range(0.001f, 0.3f)]
        public float AbsoluteSize = 0.05f;

        [Range(1, 8)]
        public float MinPixelSize = 3.0f;

        public bool PartialPointLighting = false;

        [Range(0.1f, 3f)]
        public float ShadowPointSize = 1.0f;
        
        [Range(-2f, 2f)]
        public float ShadowBias = -1.0f;

        [FormerlySerializedAs("DebugSolidMetric")]
        [Range(0.01f, 10.0f)]
        public float RemoveHiddenCascadeOffset = 1f;

        [FormerlySerializedAs("DebugSolidMetric2")]
        [Range(0.01f, 5.0f)]
        public float RemoveHiddenCascadeSize = 1f;

        [Range(0.001f, 20f)]
        public float DebugSolidPullParam = 4f;
        
        public bool CalculateNormals = true;
        
        public bool SmoothNormals = true;

        [Range(0.01f, 10.0f)]
        public float SmoothNormalsCascadeOffset = 1.3f;

        [Range(0.01f, 5.0f)]
        public float SmoothNormalsCascadeSize = 4f;

        public bool SolidFovReprojection;

        public bool TemporalSmoothing = true;

        [Range(1, 120)]
        public int InterpolatedFrames = 60;

        public bool PreserveTexelSize;

        [Range(1f, 1.5f)]
        public float ReprojectionRatio = 1.1f;

        public int DebugSolidBlitLevel;
        public bool DebugForceFill = true;
        
        [Range(-0.2f, 0.2f)]
        public float DebugFillThreshold = -0.04f;
        public bool SolidRemoveHidden = true;
        public bool DebugSolidPullPush = true;
        public int DebugSolidFixedLevel;
        public bool DebugShowRemoveHiddenCascades;
        public bool DebugShowSmoothNormalsCascades;
        public bool DebugUseLinearDepth;
        public bool DebugBlendSky = true;

        private Matrix4x4 previousViewInv;
        private bool previousFrameDataAvailable;

        protected ComputeBuffer Buffer;

        private Material PointsMaterial;
        private Material CirclesMaterial;

        private ComputeShader SolidComputeShader;
        private Material SolidRenderMaterial;
        private Material SolidBlitMaterial;

        private RTHandle rt;
        private RTHandle rtDepth;
        private RTHandle rt1;
        private RTHandle rt2;
        private RTHandle rtColor;
        private RTHandle rtDepthCopy;
        private RTHandle rtPreviousColor;
        private RTHandle rtPreviousPos;
        private RTHandle rtLidarCopy;

        private bool corrupted;

        public abstract Bounds Bounds { get; }

        public virtual int PointCount => Buffer?.count ?? 0;
        
        private static bool UsingVulkan => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan;

        public bool SupportsLighting
        {
            get
            {
                switch (RenderMode)
                {
                    case RenderType.Points:
                    case RenderType.Cones:
                        // Only partial, not entirely correct lighting supported - has to be explicitly toggled on 
                        // return PartialPointLighting;
                        // Disable lighting for non-solid rendering until ambient SH data is available
                        return false;
                    case RenderType.Solid:
                        // Lighting requires normals - if they are skipped, use unlit variant 
                        return CalculateNormals;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void GetMipData(Vector2Int resolution, int mipLevel, out Vector2Int mipResolution,
            out Vector4 gpuMipTexSize)
        {
            if (mipLevel == 0)
                mipResolution = resolution;
            else
            {
                var x = Math.Max(1, (resolution.x + 1) >> mipLevel);
                var y = Math.Max(1, (resolution.y + 1) >> mipLevel);

                // NOTE: This is workaround for invalid rendering of some odd mip resolutions
                // TODO: Find and fix underlying issue
                if (x * (1 << mipLevel) != resolution.x)
                    x--;
                
                if (y * (1 << mipLevel) != resolution.y)
                    y--;

                mipResolution = new Vector2Int(x, y);
            }

            gpuMipTexSize = new Vector4(
                mipResolution.x - 1,
                mipResolution.y - 1,
                1f / mipResolution.x,
                1f / mipResolution.y);
        }

        private static int GetGroupSize(int threads, int blockCount)
        {
            return Math.Max(1, (threads + blockCount - 1) / blockCount);
        }

        private void VerifyRTHandles(float fovScaleFactor)
        {
            if (!Application.isPlaying)
                return;

            if (rt == null)
            {
                AllocRTHandles(fovScaleFactor);
                return;
            }

            if (!Mathf.Approximately(fovScaleFactor, rt.scaleFactor.x))
            {
                ReleaseRTHandles();
                AllocRTHandles(fovScaleFactor);
            }
            
            if (rtPreviousPos == null && TemporalSmoothing)
            {
                var scaleVec = fovScaleFactor * Vector2.one;
                
                rtPreviousPos = RTHandles.Alloc(scaleVec, colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
                    name: "PointCloud_PrevPos", enableRandomWrite: true, useMipMap: true, autoGenerateMips: false);
            
                rtPreviousColor = RTHandles.Alloc(scaleVec, colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
                    name: "PointCloud_PrevColor", enableRandomWrite: true, useMipMap: true, autoGenerateMips: false);
            }
        }

        private void AllocRTHandles(float fovScaleFactor)
        {
            // TODO: check if 32bit precision per channel is needed everywhere, reduce if possible

            if (rt != null || rtDepth != null || rt1 != null || rt2 != null || rtColor != null || rtDepthCopy != null)
            {
                corrupted = true;
                throw new Exception("Attempting to alloc new RTHandles for point cloud rendering before" +
                                    "releasing old ones. This will cause memory leak.");
            }
            
            var scaleVec = fovScaleFactor * Vector2.one;
            
            rt = RTHandles.Alloc(scaleVec, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "PointCloud_RT",
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false, wrapMode: TextureWrapMode.Clamp);

            rtDepth = RTHandles.Alloc(scaleVec, colorFormat: GraphicsFormat.R32_UInt,
                depthBufferBits: DepthBits.Depth32, name: "PointCloud_Depth", wrapMode: TextureWrapMode.Clamp);
            
            rt1 = RTHandles.Alloc(scaleVec, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, name: "PointCloud_RT1",
                enableRandomWrite: true, useMipMap: true, autoGenerateMips: false, wrapMode: TextureWrapMode.Clamp);
            
            rt2 = RTHandles.Alloc(scaleVec, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, name: "PointCloud_RT2",
                enableRandomWrite: true, useMipMap: true, autoGenerateMips: false, wrapMode: TextureWrapMode.Clamp);

            rtColor = RTHandles.Alloc(scaleVec, colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
                name: "PointCloud_Color", enableRandomWrite: true, useMipMap: true, autoGenerateMips: false, wrapMode: TextureWrapMode.Clamp);
            
            rtDepthCopy = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.Depth32, GraphicsFormat.R32_UInt,
                dimension: TextureXR.dimension, useDynamicScale: true, name: "Depth Stencil Copy", wrapMode: TextureWrapMode.Clamp);

            if (!TemporalSmoothing)
                return;
            
            if (rtPreviousPos != null || rtPreviousColor != null)
            {
                corrupted = true;
                throw new Exception("Attempting to alloc new RTHandles for point cloud temporal smoothing before" +
                                    "releasing old ones. This will cause memory leak.");
            }

            rtPreviousPos = RTHandles.Alloc(scaleVec, colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
                name: "PointCloud_PrevPos", enableRandomWrite: true, useMipMap: true, autoGenerateMips: false);
            
            rtPreviousColor = RTHandles.Alloc(scaleVec, colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
                name: "PointCloud_PrevColor", enableRandomWrite: true, useMipMap: true, autoGenerateMips: false);
        }

        private void ReleaseRTHandles()
        {
            if (rt != null)
            {
                RTHandles.Release(rt);
                rt = null;
            }

            if (rtDepth != null)
            {
                RTHandles.Release(rtDepth);
                rtDepth = null;
            }

            if (rt1 != null)
            {
                RTHandles.Release(rt1);
                rt1 = null;
            }

            if (rtColor != null)
            {
                RTHandles.Release(rtColor);
                rtColor = null;
            }

            if (rt2 != null)
            {
                RTHandles.Release(rt2);
                rt2 = null;
            }

            if (rtPreviousColor != null)
            {
                RTHandles.Release(rtPreviousColor);
                rtPreviousColor = null;
            }

            if (rtPreviousPos != null)
            {
                RTHandles.Release(rtPreviousPos);
                rtPreviousPos = null;
            }

            if (rtDepthCopy != null)
            {
                RTHandles.Release(rtDepthCopy);
                rtDepthCopy = null;
            }
        }

        public virtual void Cleanup()
        {
            CoreUtils.Destroy(PointsMaterial);
            CoreUtils.Destroy(CirclesMaterial);
            CoreUtils.Destroy(SolidRenderMaterial);
            CoreUtils.Destroy(SolidBlitMaterial);
            CoreUtils.Destroy(SolidComputeShader);

            ReleaseRTHandles();
        }

        private void Start()
        {
            PointCloudManager.HandleRendererAdded(this);
        }

        protected virtual void OnDisable()
        {
            Buffer?.Dispose();
            Buffer = null;
        }

        private void OnDestroy()
        {
            PointCloudManager.HandleRendererRemoved(this);
            Cleanup();
        }

        private void VerifyPointsMaterial()
        {
            if (PointsMaterial == null)
            {
                PointsMaterial = new Material(RuntimeSettings.Instance.PointCloudPoints);
                PointsMaterial.hideFlags = HideFlags.DontSave;
                
                PointsMaterial.SetInt(ShaderIDs.PointsRender.StencilRef, HDRenderPipeline.StencilRefGBuffer);
                PointsMaterial.SetInt(ShaderIDs.PointsRender.StencilMask, HDRenderPipeline.StencilWriteMaskGBuffer);
            }

            if (CirclesMaterial == null)
            {
                CirclesMaterial = new Material(RuntimeSettings.Instance.PointCloudCircles);
                CirclesMaterial.hideFlags = HideFlags.DontSave;
                
                CirclesMaterial.SetInt(ShaderIDs.PointsRender.StencilRef, HDRenderPipeline.StencilRefGBuffer);
                CirclesMaterial.SetInt(ShaderIDs.PointsRender.StencilMask, HDRenderPipeline.StencilWriteMaskGBuffer);
            }
        }

        private void VerifySolidMaterial()
        {
            if (SolidComputeShader == null)
            {
                SolidComputeShader = Instantiate(RuntimeSettings.Instance.PointCloudSolid);

                SolidRenderMaterial = new Material(RuntimeSettings.Instance.PointCloudSolidRender);
                SolidRenderMaterial.hideFlags = HideFlags.DontSave;

                SolidBlitMaterial = new Material(RuntimeSettings.Instance.PointCloudSolidBlit);
                SolidBlitMaterial.hideFlags = HideFlags.DontSave;
                
                SolidBlitMaterial.SetInt(ShaderIDs.PointsRender.StencilRef, HDRenderPipeline.StencilRefGBuffer);
                SolidBlitMaterial.SetInt(ShaderIDs.PointsRender.StencilMask, HDRenderPipeline.StencilWriteMaskGBuffer);
            }
            
            SolidBlitMaterial.SetKeyword(ShaderIDs.SolidCompose.LinearDepthKeyword, DebugUseLinearDepth);
        }

        private bool firstFrameDone;

        public void Render(CommandBuffer cmd, HDCamera targetCamera, RenderTargetIdentifier[] rtIds, RTHandle depthBuffer, RTHandle cameraColorBuffer)
        {
            if ((Mask & RenderMask.Camera) == 0 || Buffer == null || PointCount == 0 || !isActiveAndEnabled)
                return;

            // Temporarily disabled due to incompatibility with HDRP (camera relative rendering)
            if (TemporalSmoothing)
                TemporalSmoothing = false;
        
            switch (RenderMode)
            {
                case RenderType.Solid when Application.isPlaying && targetCamera.camera.cameraType != CameraType.SceneView:
                    RenderAsSolid(cmd, targetCamera, rtIds, depthBuffer, cameraColorBuffer);
                    break;
                case RenderType.Points:
                case RenderType.Cones:
                case RenderType.Solid:
                    RenderAsPoints(cmd, targetCamera, rtIds, depthBuffer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void RenderLidar(CommandBuffer cmd, HDCamera targetCamera, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            var solid = RenderMode == RenderType.Solid;
            RenderLidar(cmd, targetCamera, colorBuffer, depthBuffer, solid);
        }
        
        public void RenderLidar(CommandBuffer cmd, HDCamera targetCamera, RTHandle colorBuffer, RTHandle depthBuffer, bool solid)
        {
            if ((Mask & RenderMask.Lidar) == 0 || Buffer == null || PointCount == 0 || !isActiveAndEnabled)
                return;
            
            if (solid)
                RenderLidarSolid(cmd, targetCamera, colorBuffer, depthBuffer);
            else
                RenderLidarPoints(cmd, targetCamera, colorBuffer, depthBuffer);
        }

        private void RenderLidarSolid(CommandBuffer cmd, HDCamera targetCamera, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            // This value changes multiple times per frame, so it has to be set through command buffer, hence global
            CoreUtils.SetKeyword(cmd, ShaderIDs.SolidCompose.TargetGBufferKeyword, false);
            
            RenderSolidCore(cmd, targetCamera, null, false, false);

            // One texture can't be set as target and read from at the same time - copy needed
            // NOTE: Unfortunately, CopyTexture will not allow to copy depth texture without filling the whole target,
            //       so rtDepthCopy can't be reused here (lidar texture is smaller).
            if (rtLidarCopy == null || depthBuffer.referenceSize != rtLidarCopy.referenceSize)
            {
                rtLidarCopy = RTHandles.Alloc(
                    depthBuffer.referenceSize.x,
                    depthBuffer.referenceSize.y,
                    TextureXR.slices,
                    DepthBits.Depth32,
                    GraphicsFormat.R32_UInt,
                    dimension: TextureXR.dimension,
                    useDynamicScale: true,
                    name: "Lidar Depth Copy",
                    wrapMode: TextureWrapMode.Clamp);
            }
            cmd.CopyTexture(depthBuffer, rtLidarCopy);

            SolidBlitMaterial.SetTexture(ShaderIDs.SolidCompose.ColorTexture, rtColor);
            SolidBlitMaterial.SetTexture(ShaderIDs.SolidCompose.NormalDepthTexture, rt1);
            SolidBlitMaterial.SetTexture(ShaderIDs.SolidCompose.OriginalDepth, rtLidarCopy);
            SolidBlitMaterial.SetVector(ShaderIDs.SolidCompose.ReprojectionVector, GetFovReprojectionVector(targetCamera.camera));
            var lidarComposePass = SolidBlitMaterial.FindPass("Point Cloud Lidar Compose");

            CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer);
            CoreUtils.DrawFullScreen(cmd, SolidBlitMaterial, shaderPassId: lidarComposePass);
        }

        private void RenderAsSolid(CommandBuffer cmd, HDCamera targetCamera, RenderTargetIdentifier[] rtIds, RTHandle depthBuffer, RTHandle cameraColorBuffer)
        {
            // More than 1 RT currently means that render is targeting GBuffer
            var targetGBuffer = rtIds.Length > 1;
            // This value changes multiple times per frame, so it has to be set through command buffer, hence global
            CoreUtils.SetKeyword(cmd, ShaderIDs.SolidCompose.TargetGBufferKeyword, targetGBuffer);
            
            RenderSolidCore(cmd, targetCamera, cameraColorBuffer, CalculateNormals, SmoothNormals);

            // One texture can't be set as target and read from at the same time - copy needed depth data
            cmd.CopyTexture(depthBuffer, rtDepthCopy);
            
            SolidBlitMaterial.SetTexture(ShaderIDs.SolidCompose.ColorTexture, rtColor);
            SolidBlitMaterial.SetTexture(ShaderIDs.SolidCompose.NormalDepthTexture, rt1);
            SolidBlitMaterial.SetTexture(ShaderIDs.SolidCompose.OriginalDepth, rtDepthCopy);
            SolidBlitMaterial.SetVector(ShaderIDs.SolidCompose.ReprojectionVector, GetFovReprojectionVector(targetCamera.camera));
            var composePass = SolidBlitMaterial.FindPass("Point Cloud Default Compose");
            
            CoreUtils.SetRenderTarget(cmd, rtIds, depthBuffer);
            CoreUtils.DrawFullScreen(cmd, SolidBlitMaterial, shaderPassId: composePass);
            
            // cmd.CopyTexture(depthBuffer, rtDepthCopy);
            // SolidBlitMaterial.SetTexture(ShaderIDs.SolidCompose.OriginalDepth, rtDepthCopy);
            // SolidBlitMaterial.SetTexture(ShaderIDs.SolidCompose.ColorTexture, rtColor);
            // SolidBlitMaterial.SetVector(ShaderIDs.SolidCompose.ReprojectionVector, GetFovReprojectionVector(targetCamera.camera));
            // var composePass = SolidBlitMaterial.FindPass("Point Cloud Debug Compose");
            //
            // CoreUtils.SetRenderTarget(cmd, rtIds, depthBuffer);
            // CoreUtils.DrawFullScreen(cmd, SolidBlitMaterial, shaderPassId: composePass);
        }

        private void RenderSolidCore(CommandBuffer cmd, HDCamera targetCamera, RTHandle cameraColorBuffer, bool calculateNormals, bool smoothNormals)
        {
            if (!Application.isPlaying || corrupted)
                return;

            var screenRescaleMultiplier = GetFovReprojectionMultiplier(targetCamera.camera);
            
            VerifySolidMaterial();

            VerifyRTHandles(PreserveTexelSize ? screenRescaleMultiplier : 1f);
            
            if (corrupted)
                return;

            var width = targetCamera.actualWidth;
            var height = targetCamera.actualHeight;
            var refSize = rt.referenceSize;

            if (SolidFovReprojection && PreserveTexelSize)
            {
                width = (int) (width * screenRescaleMultiplier);
                height = (int) (height * screenRescaleMultiplier);
            }
            
            var resolution = new Vector2Int(width, height);

            var fov = targetCamera.camera.fieldOfView;
            if (SolidFovReprojection)
                fov *= ReprojectionRatio;

            var size = Math.Max(width, height);

            var maxLevel = 0;
            while (size >> maxLevel >= 16)
            {
                maxLevel++;
            }

            CoreUtils.SetRenderTarget(cmd, rt, rtDepth);
            CoreUtils.ClearRenderTarget(cmd, ClearFlag.All, Color.clear);

            CalculateMatrices(targetCamera, out var projMatrix, out var invProjMatrix, out var invViewProjMatrix, out var solidRenderMvp);

            SolidRenderMaterial.SetBuffer(ShaderIDs.SolidRender.Buffer, Buffer);
            SolidRenderMaterial.SetInt(ShaderIDs.SolidRender.Colorize, (int) Colorize);
            SolidRenderMaterial.SetFloat(ShaderIDs.SolidRender.MinHeight, Bounds.min.y);
            SolidRenderMaterial.SetFloat(ShaderIDs.SolidRender.MaxHeight, Bounds.max.y);
            SolidRenderMaterial.SetMatrix(ShaderIDs.SolidRender.MVPMatrix, solidRenderMvp);
            SolidRenderMaterial.SetPass(0);
            cmd.DrawProcedural(Matrix4x4.identity, SolidRenderMaterial, 0, MeshTopology.Points, PointCount);
            
            var cs = SolidComputeShader;

            cmd.SetComputeVectorParam(cs, ShaderIDs.SolidCompute.TextureSize,
                new Vector4(width - 1, height - 1, 1f / width, 1f / height));
            cmd.SetComputeVectorParam(cs, ShaderIDs.SolidCompute.FullRTSize,
                new Vector4(refSize.x - 1, refSize.y - 1, 1f / refSize.x, 1f / refSize.y));
            cmd.SetComputeFloatParam(cs, ShaderIDs.SolidCompute.FarPlane, targetCamera.camera.farClipPlane);
            cmd.SetComputeMatrixParam(cs, ShaderIDs.SolidCompute.ProjectionMatrix, projMatrix);
            cmd.SetComputeMatrixParam(cs, ShaderIDs.SolidCompute.InverseProjectionMatrix, invProjMatrix);
            cmd.SetComputeMatrixParam(cs, ShaderIDs.SolidCompute.InverseVPMatrix, invViewProjMatrix);
            cmd.SetComputeVectorParam(cs, ShaderIDs.SolidCompute.InverseReprojectionVector, GetInverseUvFovReprojectionVector(targetCamera.camera));

            var blendSky = DebugBlendSky && cameraColorBuffer != null;
            var setupCopy = SolidComputeShader.FindKernel(ShaderIDs.SolidCompute.SetupCopy.GetKernelName(DebugUseLinearDepth, DebugForceFill, blendSky));
            cmd.SetComputeTextureParam(cs, setupCopy, ShaderIDs.SolidCompute.SetupCopy.InputColor, rt, 0);
            cmd.SetComputeTextureParam(cs, setupCopy, ShaderIDs.SolidCompute.SetupCopy.InputPosition, rtDepth, 0);
            cmd.SetComputeTextureParam(cs, setupCopy, ShaderIDs.SolidCompute.SetupCopy.OutputPosition, rt1, 0);
            cmd.SetComputeTextureParam(cs, setupCopy, ShaderIDs.SolidCompute.SetupCopy.OutputColor, rtColor, 0);
            if (blendSky)
                cmd.SetComputeTextureParam(cs, setupCopy, ShaderIDs.SolidCompute.SetupCopy.PostSkyPreRenderTexture, cameraColorBuffer, 0);
            cmd.SetComputeFloatParam(cs, ShaderIDs.SolidCompute.SetupCopy.HorizonThreshold, DebugFillThreshold);
            cmd.DispatchCompute(cs, setupCopy, GetGroupSize(width, 8), GetGroupSize(height, 8), 1);

            if (SolidRemoveHidden)
            {
                var downsample = SolidComputeShader.FindKernel(ShaderIDs.SolidCompute.Downsample.KernelName);
                for (var i = 1; i <= maxLevel + 3; i++)
                {
                    GetMipData(resolution, i - 1, out var mipRes, out var mipVec);
                    cmd.SetComputeVectorParam(cs, ShaderIDs.SolidCompute.MipTextureSize, mipVec);
                    cmd.SetComputeTextureParam(cs, downsample, ShaderIDs.SolidCompute.Downsample.InputPosition, rt1, i - 1);
                    cmd.SetComputeTextureParam(cs, downsample, ShaderIDs.SolidCompute.Downsample.OutputPosition, rt1, i);
                    cmd.DispatchCompute(cs, downsample, GetGroupSize(mipRes.x, 16), GetGroupSize(mipRes.y, 16), 1);
                }

                DebugSolidFixedLevel = Math.Min(Math.Max(DebugSolidFixedLevel, 0), maxLevel);
                
                var removeHidden = DebugShowRemoveHiddenCascades
                    ? SolidComputeShader.FindKernel(ShaderIDs.SolidCompute.RemoveHidden.DebugKernelName)
                    : SolidComputeShader.FindKernel(ShaderIDs.SolidCompute.RemoveHidden.KernelName);
                var removeHiddenMagic = RemoveHiddenCascadeOffset * height * 0.5f / Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);
                
                cmd.SetComputeIntParam(cs, ShaderIDs.SolidCompute.RemoveHidden.LevelCount, maxLevel);
                cmd.SetComputeTextureParam(cs, removeHidden, ShaderIDs.SolidCompute.RemoveHidden.Position, rt1);
                cmd.SetComputeTextureParam(cs, removeHidden, ShaderIDs.SolidCompute.RemoveHidden.Color, rtColor, 0);
                cmd.SetComputeFloatParam(cs, ShaderIDs.SolidCompute.RemoveHidden.CascadesOffset, removeHiddenMagic);
                cmd.SetComputeFloatParam(cs, ShaderIDs.SolidCompute.RemoveHidden.CascadesSize, RemoveHiddenCascadeSize);
                cmd.SetComputeIntParam(cs, ShaderIDs.SolidCompute.RemoveHidden.FixedLevel, DebugSolidFixedLevel);
                cmd.DispatchCompute(cs, removeHidden, GetGroupSize(width, 8), GetGroupSize(height, 8), 1);
                
                /*
                // TODO: fix temporal smoothing for camera-relative rendering
                // View matrix in this case has only rotation, which means that camera movement will be ignored
                if (TemporalSmoothing)
                {
                    var curView = targetCamera.mainViewConstants.viewMatrix;
                    var prevToCurrent = curView * previousViewInv;

                    previousViewInv = targetCamera.mainViewConstants.invViewMatrix;

                    if (previousFrameDataAvailable)
                    {
                        var applyPrevious = SolidComputeShader.FindKernel(ShaderIDs.SolidCompute.ApplyPreviousFrame.KernelName);
                        cmd.SetComputeTextureParam(cs, applyPrevious, ShaderIDs.SolidCompute.ApplyPreviousFrame.SavedColor, rtPreviousColor, 0);
                        cmd.SetComputeTextureParam(cs, applyPrevious, ShaderIDs.SolidCompute.ApplyPreviousFrame.SavedPos, rtPreviousPos, 0);
                        cmd.SetComputeTextureParam(cs, applyPrevious, ShaderIDs.SolidCompute.ApplyPreviousFrame.CurrentColor, rtColor, 0);
                        cmd.SetComputeTextureParam(cs, applyPrevious, ShaderIDs.SolidCompute.ApplyPreviousFrame.CurrentPos, rt2, 0);
                        if (UsingVulkan)
                        {
                            cmd.SetComputeTextureParam(cs, applyPrevious, ShaderIDs.SolidCompute.ApplyPreviousFrame.CurrentColorIn, rtColor, 0);
                            cmd.SetComputeTextureParam(cs, applyPrevious, ShaderIDs.SolidCompute.ApplyPreviousFrame.CurrentPosIn, rt2, 0);
                        }

                        cmd.SetComputeMatrixParam(cs, ShaderIDs.SolidCompute.ApplyPreviousFrame.PrevToCurrentMatrix, prevToCurrent);
                        cmd.SetComputeFloatParam(cs, ShaderIDs.SolidCompute.ApplyPreviousFrame.FramePersistence, 1f / InterpolatedFrames);
                        cmd.DispatchCompute(cs, applyPrevious, HDUtils.DivRoundUp(size, 8), HDUtils.DivRoundUp(size, 8), 1);
                    }
                    else
                    {
                        previousFrameDataAvailable = true;
                    }

                    cmd.CopyTexture(rtColor, rtPreviousColor);
                    cmd.CopyTexture(rt2, rtPreviousPos);
                }
                else if (previousFrameDataAvailable)
                    previousFrameDataAvailable = false;
                    
                */
            }
            
            if (DebugSolidPullPush)
            {
                var pullKernel = SolidComputeShader.FindKernel(ShaderIDs.SolidCompute.PullKernel.KernelName);
                cmd.SetComputeFloatParam(cs, ShaderIDs.SolidCompute.PullKernel.FilterExponent, DebugSolidPullParam);
                
                for (var i = 1; i <= maxLevel; i++)
                {
                    GetMipData(resolution, i, out var mipRes, out var higherMipVec);
                    GetMipData(resolution, i - 1, out _, out var mipVec);
                    cmd.SetComputeVectorParam(cs, ShaderIDs.SolidCompute.MipTextureSize, mipVec);
                    cmd.SetComputeVectorParam(cs, ShaderIDs.SolidCompute.HigherMipTextureSize, higherMipVec);
                    cmd.SetComputeIntParam(cs, ShaderIDs.SolidCompute.PullKernel.SkipWeightMul, i == maxLevel ? 1 : 0);
                    cmd.SetComputeIntParam(cs, ShaderIDs.SolidCompute.PullKernel.InputLevel, i - 1);
                    cmd.SetComputeTextureParam(cs, pullKernel, ShaderIDs.SolidCompute.PullKernel.InputColor, rtColor, i - 1);
                    cmd.SetComputeTextureParam(cs, pullKernel, ShaderIDs.SolidCompute.PullKernel.OutputColor, rtColor, i);
                    cmd.DispatchCompute(cs, pullKernel, GetGroupSize(mipRes.x, 8), GetGroupSize(mipRes.y, 8), 1);
                }
                
                var pushKernel = SolidComputeShader.FindKernel(ShaderIDs.SolidCompute.PushKernel.KernelName);
                
                for (var i = maxLevel; i > 0; i--)
                {
                    GetMipData(resolution, i - 1, out var mipRes, out var mipVec);
                    GetMipData(resolution, i, out _, out var lowerMipVec);
                    cmd.SetComputeVectorParam(cs, ShaderIDs.SolidCompute.MipTextureSize, mipVec);
                    cmd.SetComputeVectorParam(cs, ShaderIDs.SolidCompute.HigherMipTextureSize, lowerMipVec);
                    cmd.SetComputeIntParam(cs, ShaderIDs.SolidCompute.PushKernel.InputLevel, i);
                    cmd.SetComputeTextureParam(cs, pushKernel, ShaderIDs.SolidCompute.PushKernel.InputColor, rtColor, i);
                    cmd.SetComputeTextureParam(cs, pushKernel, ShaderIDs.SolidCompute.PushKernel.OutputColor, rtColor, i - 1);
                    cmd.DispatchCompute(cs, pushKernel, GetGroupSize(mipRes.x, 8), GetGroupSize(mipRes.y, 8), 1);
                }

                if (calculateNormals)
                {
                    var calculateNormalsKernel = SolidComputeShader.FindKernel(ShaderIDs.SolidCompute.CalculateNormals.GetKernelName(DebugUseLinearDepth));
                    var normalsTarget = smoothNormals ? rt2 : rt1;

                    for (var i = 0; i < maxLevel; ++i)
                    {
                        GetMipData(resolution, i, out var mipRes, out var mipVec);
                        cmd.SetComputeVectorParam(cs, ShaderIDs.SolidCompute.MipTextureSize, mipVec);
                        cmd.SetComputeIntParam(cs, ShaderIDs.SolidCompute.CalculateNormals.InputLevel, i);
                        cmd.SetComputeTextureParam(cs, calculateNormalsKernel, ShaderIDs.SolidCompute.CalculateNormals.Input, rtColor);
                        cmd.SetComputeTextureParam(cs, calculateNormalsKernel, ShaderIDs.SolidCompute.CalculateNormals.Output, normalsTarget, i);

                        cmd.DispatchCompute(cs, calculateNormalsKernel, GetGroupSize(mipRes.x, 8), GetGroupSize(mipRes.x, 8), 1);
                    }

                    if (smoothNormals)
                    {
                        var smoothNormalsKernel = SolidComputeShader.FindKernel(ShaderIDs.SolidCompute.SmoothNormals.GetKernelName(DebugUseLinearDepth, DebugShowSmoothNormalsCascades));
                        var smoothNormalsMagic = SmoothNormalsCascadeOffset * height * 0.5f / Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);

                        cmd.SetComputeTextureParam(cs, smoothNormalsKernel, ShaderIDs.SolidCompute.SmoothNormals.Input, rt2);
                        cmd.SetComputeTextureParam(cs, smoothNormalsKernel, ShaderIDs.SolidCompute.SmoothNormals.Output, rt1, 0);

                        cmd.SetComputeFloatParam(cs, ShaderIDs.SolidCompute.SmoothNormals.CascadesOffset, smoothNormalsMagic);
                        cmd.SetComputeFloatParam(cs, ShaderIDs.SolidCompute.SmoothNormals.CascadesSize, SmoothNormalsCascadeSize);
                        if (DebugShowSmoothNormalsCascades)
                            cmd.SetComputeTextureParam(cs, smoothNormalsKernel, ShaderIDs.SolidCompute.SmoothNormals.ColorDebug, rtColor, 0);
                        cmd.DispatchCompute(cs, smoothNormalsKernel, GetGroupSize(width, 8), GetGroupSize(height, 8), 1);
                    }
                }
            }
        }

        private void RenderAsPoints(CommandBuffer cmd, HDCamera targetCamera, RenderTargetIdentifier[] rtIds, RTHandle depthBuffer)
        {
            // More that 1 RT currently means that render is targeting GBuffer
            var targetGBuffer = rtIds.Length > 1;
            VerifyPointsMaterial();

            if (ConstantSize && Mathf.Approximately(PixelSize, 1f))
            {
                PointsMaterial.SetBuffer(ShaderIDs.PointsRender.Buffer, Buffer);
                PointsMaterial.SetMatrix(ShaderIDs.PointsRender.ModelMatrix, transform.localToWorldMatrix);
                PointsMaterial.SetMatrix(ShaderIDs.PointsRender.VPMatrix, targetCamera.mainViewConstants.viewProjMatrix);
                PointsMaterial.SetInt(ShaderIDs.PointsRender.Colorize, (int) Colorize);
                PointsMaterial.SetFloat(ShaderIDs.PointsRender.MinHeight, Bounds.min.y);
                PointsMaterial.SetFloat(ShaderIDs.PointsRender.MaxHeight, Bounds.max.y);
                
                // This value changes multiple times per frame, so it has to be set through command buffer, hence global
                CoreUtils.SetKeyword(cmd, ShaderIDs.SolidCompose.TargetGBufferKeyword, targetGBuffer);
                CoreUtils.SetRenderTarget(cmd, rtIds, depthBuffer);

                cmd.DrawProcedural(Matrix4x4.identity, PointsMaterial, 0, MeshTopology.Points, PointCount);
            }
            else
            {
                SetCirclesMaterialProperties(targetCamera);
                
                // This value changes multiple times per frame, so it has to be set through command buffer, hence global
                CoreUtils.SetKeyword(cmd, ShaderIDs.SolidCompose.TargetGBufferKeyword, targetGBuffer);
                CoreUtils.SetRenderTarget(cmd, rtIds, depthBuffer);
                
                var pass = CirclesMaterial.FindPass("Point Cloud Circles GBuffer");
                cmd.DrawProcedural(Matrix4x4.identity, CirclesMaterial, pass, MeshTopology.Points, PointCount);
            }
        }

        private void RenderLidarPoints(CommandBuffer cmd, HDCamera targetCamera, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            VerifyPointsMaterial();
            SetCirclesMaterialProperties(targetCamera);
            
            CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer);
            var pass = CirclesMaterial.FindPass("Point Cloud Circles Lidar");
            cmd.DrawProcedural(Matrix4x4.identity, CirclesMaterial, pass, MeshTopology.Points, PointCount);
        }

        private void SetCirclesMaterialProperties(HDCamera targetCamera)
        {
            CirclesMaterial.SetKeyword(ShaderIDs.PointsRender.ConesKeyword, RenderMode == RenderType.Cones);
            CirclesMaterial.SetBuffer(ShaderIDs.PointsRender.Buffer, Buffer);
            CirclesMaterial.SetMatrix(ShaderIDs.PointsRender.ModelMatrix, transform.localToWorldMatrix);
            CirclesMaterial.SetMatrix(ShaderIDs.PointsRender.VPMatrix, targetCamera.mainViewConstants.viewProjMatrix);
            CirclesMaterial.SetInt(ShaderIDs.PointsRender.Colorize, (int) Colorize);
            CirclesMaterial.SetFloat(ShaderIDs.PointsRender.MinHeight, Bounds.min.y);
            CirclesMaterial.SetFloat(ShaderIDs.PointsRender.MaxHeight, Bounds.max.y);

            if (ConstantSize)
            {
                CirclesMaterial.EnableKeyword(ShaderIDs.PointsRender.SizeInPixelsKeyword);
                CirclesMaterial.SetFloat(ShaderIDs.PointsRender.Size, PixelSize);
            }
            else
            {
                CirclesMaterial.DisableKeyword(ShaderIDs.PointsRender.SizeInPixelsKeyword);
                CirclesMaterial.SetFloat(ShaderIDs.PointsRender.Size, AbsoluteSize);
                CirclesMaterial.SetFloat(ShaderIDs.PointsRender.MinSize, MinPixelSize);
            }
        }

        public void RenderShadows(CommandBuffer cmd, float worldTexelSize)
        {
            if ((Mask & RenderMask.Shadows) == 0 || Buffer == null || PointCount == 0 || !isActiveAndEnabled)
                return;
            
            VerifyPointsMaterial();
            
            CirclesMaterial.SetBuffer(ShaderIDs.PointsRender.Buffer, Buffer);
            CirclesMaterial.SetMatrix(ShaderIDs.PointsRender.ModelMatrix, transform.localToWorldMatrix);
            
            var scale = ShadowPointSize * 0.001f / worldTexelSize;
            var biasPush = ShadowBias * scale;
            var shadowVector = new Vector4(scale, biasPush, 0, 0);
            // This value changes multiple times per frame, so it has to be set through command buffer, hence global
            cmd.SetGlobalVector(ShaderIDs.PointsRender.ShadowVector, shadowVector);

            var pass = CirclesMaterial.FindPass("Point Cloud Circles ShadowCaster");
            cmd.DrawProcedural(Matrix4x4.identity, CirclesMaterial, pass, MeshTopology.Points, PointCount);
        }

        private float GetFovReprojectionMultiplier(Camera usedCamera)
        {
            if (!SolidFovReprojection)
                return 1f;
            
            var originalFov = usedCamera.fieldOfView;
            var extendedFov = originalFov * ReprojectionRatio;

            return Mathf.Tan(0.5f * extendedFov * Mathf.Deg2Rad) / Mathf.Tan(0.5f * originalFov * Mathf.Deg2Rad);
        }

        private Vector4 GetFovReprojectionVector(Camera usedCamera)
        {
            if (!SolidFovReprojection)
                return new Vector4(1f, 0f, 0f, 0f);
            
            var mult = GetFovReprojectionMultiplier(usedCamera);
            var width = usedCamera.pixelWidth;
            var height = usedCamera.pixelHeight;
            
            if (PreserveTexelSize)
            {
                var border = 0.5f * (mult - 1f);
                return new Vector4(1f, width * border, height * border);
            }
            else
            {
                var revMult = 1f / mult;
                var border = 0.5f * (1 - revMult);
            
                return new Vector4(revMult, width * border, height * border, 0f);
            }
        }

        private Vector4 GetInverseUvFovReprojectionVector(Camera usedCamera)
        {
            if (!SolidFovReprojection)
                return new Vector4(1f, 0f, 0f, 0f);
            
            var vec = GetFovReprojectionVector(usedCamera);
            var width = usedCamera.pixelWidth;
            var height = usedCamera.pixelHeight;
            return new Vector4(1.0f / vec.x, -vec.y / vec.x / width, -vec.z / vec.x / height);
        }

        private void CalculateMatrices(HDCamera targetCamera, out Matrix4x4 proj, out Matrix4x4 invProj,
            out Matrix4x4 invViewProj, out Matrix4x4 solidRenderMvp)
        {
            proj = targetCamera.mainViewConstants.projMatrix;
            var cameraView = targetCamera.mainViewConstants.viewMatrix;

            if (SolidFovReprojection)
            {
                var mul = 1 / GetFovReprojectionMultiplier(targetCamera.camera);

                proj[0, 0] *= mul;
                proj[1, 1] *= mul;
            }

            invProj = proj.inverse;
            invViewProj = (proj * cameraView).inverse;
            
            var m = transform.localToWorldMatrix;
            var v = targetCamera.camera.worldToCameraMatrix;
            solidRenderMvp = proj * v * m;
        }
    }
}