/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud
{
    using System;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;
    using UnityEngine.Serialization;

    [ExecuteInEditMode]
    public abstract class PointCloudRenderer : MonoBehaviour
    {
#region member_types
        public enum RenderType
        {
            Points,
            Solid,
            Cones
        }

        public enum LightingMode
        {
            Unlit,
            ShadowReceiver,
            FullDeferred
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
            Shadowcaster = 1 << 1,
            Lidar = 1 << 2,
            Default = Camera | Lidar
        }
#endregion

        public ColorizeType Colorize = ColorizeType.RainbowIntensity;

        [FormerlySerializedAs("Render")]
        public RenderType RenderMode = RenderType.Points;

        public RenderMask Mask = RenderMask.Default;

        public LightingMode Lighting = LightingMode.Unlit;

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

        [Range(1f, 1.5f)]
        public float ReprojectionRatio = 1.1f;

        public int DebugSolidBlitLevel;
        public bool DebugForceFill = true;

        [Range(-0.4f, 0.2f)]
        public float DebugFillThreshold = -0.2f;

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

#if UNITY_EDITOR
        protected ComputeBuffer sceneViewBuffer;
#endif
        
        public abstract Bounds Bounds { get; }

        public virtual int PointCount => Buffer?.count ?? 0;
        
#if UNITY_EDITOR
        public virtual int SceneViewPointCount => sceneViewBuffer?.count ?? 0;
#endif

        private PointCloudResources Resources => PointCloudManager.Resources;

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
                        return CalculateNormals && Lighting == LightingMode.FullDeferred;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void GetMipData(
            Vector2Int resolution,
            int mipLevel,
            out Vector2Int mipResolution,
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

        private void Start()
        {
            PointCloudManager.HandleRendererAdded(this);
        }

        protected virtual void OnDisable()
        {
            Buffer?.Dispose();
            Buffer = null;
            
#if UNITY_EDITOR
            sceneViewBuffer?.Dispose();
            sceneViewBuffer = null;
#endif
        }

        private void OnDestroy()
        {
            PointCloudManager.HandleRendererRemoved(this);
        }

        private ComputeBuffer GetBufferForCamera(HDCamera hdCamera)
        {
#if UNITY_EDITOR
            return hdCamera.camera.cameraType == CameraType.SceneView ? sceneViewBuffer : Buffer;
#else
            return Buffer;
#endif
        }

        private int GetPointCountForCamera(HDCamera hdCamera)
        {
#if UNITY_EDITOR
            return hdCamera.camera.cameraType == CameraType.SceneView ? SceneViewPointCount : PointCount;
#else
            return PointCount;
#endif
        }

        public void Render(CommandBuffer cmd, HDCamera targetCamera, RenderTargetIdentifier[] rtIds, RTHandle depthBuffer, RTHandle cameraColorBuffer)
        {
            var pointCount = GetPointCountForCamera(targetCamera);
            var buffer = GetBufferForCamera(targetCamera);
            
            if ((Mask & RenderMask.Camera) == 0 || buffer == null || pointCount == 0 || !isActiveAndEnabled)
                return;

            // Temporarily disabled due to incompatibility with HDRP (camera relative rendering)
            if (TemporalSmoothing)
                TemporalSmoothing = false;

            switch (RenderMode)
            {
                case RenderType.Solid:
                    RenderAsSolid(cmd, targetCamera, rtIds, depthBuffer, cameraColorBuffer);
                    break;
                case RenderType.Points:
                case RenderType.Cones:
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
#if UNITY_EDITOR
            if (targetCamera.camera.cameraType == CameraType.SceneView)
                return;
#endif
            
            if ((Mask & RenderMask.Lidar) == 0 || Buffer == null || PointCount == 0 || !isActiveAndEnabled)
                return;

            if (solid)
                RenderLidarSolid(cmd, targetCamera, colorBuffer, depthBuffer);
            else
                RenderLidarPoints(cmd, targetCamera, colorBuffer, depthBuffer);
        }
        
        public void RenderDepth(CommandBuffer cmd, HDCamera targetCamera, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            var solid = RenderMode == RenderType.Solid;
            RenderDepth(cmd, targetCamera, colorBuffer, depthBuffer, solid);
        }

        public void RenderDepth(CommandBuffer cmd, HDCamera targetCamera, RTHandle colorBuffer, RTHandle depthBuffer, bool solid)
        {
            if ((Mask & RenderMask.Camera) == 0 || Buffer == null || PointCount == 0 || !isActiveAndEnabled)
                return;

            if (solid)
                RenderDepthSolid(cmd, targetCamera, colorBuffer, depthBuffer);
            else
                RenderDepthPoints(cmd, targetCamera, colorBuffer, depthBuffer);
        }

        private void RenderLidarSolid(CommandBuffer cmd, HDCamera targetCamera, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            // This value changes multiple times per frame, so it has to be set through command buffer, hence global
            CoreUtils.SetKeyword(cmd, PointCloudShaderIDs.SolidCompose.TargetGBufferKeyword, false);

            RenderSolidCore(cmd, targetCamera, null, false, false);

            // One texture can't be set as target and read from at the same time - copy needed
            var rtLidarCopy = Resources.GetCustomSizedDepthRT(depthBuffer.referenceSize);
            cmd.CopyTexture(depthBuffer, rtLidarCopy);

            cmd.SetGlobalTexture(PointCloudShaderIDs.SolidCompose.ColorTexture, Resources.GetRTHandle(RTUsage.ColorBuffer));
            cmd.SetGlobalTexture(PointCloudShaderIDs.SolidCompose.OriginalDepth, rtLidarCopy);
            cmd.SetGlobalVector(PointCloudShaderIDs.SolidCompose.ReprojectionVector, GetFovReprojectionVector(targetCamera.camera));
            var lidarComposePass = Resources.Passes.lidarCompose;

            CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer);
            CoreUtils.DrawFullScreen(cmd, Resources.SolidComposeMaterial, shaderPassId: lidarComposePass);
        }
        
        private void RenderDepthSolid(CommandBuffer cmd, HDCamera targetCamera, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            // This value changes multiple times per frame, so it has to be set through command buffer, hence global
            CoreUtils.SetKeyword(cmd, PointCloudShaderIDs.SolidCompose.TargetGBufferKeyword, false);

            RenderSolidCore(cmd, targetCamera, null, false, false);

            // One texture can't be set as target and read from at the same time - copy needed
            var rtDepthCopy = Resources.GetCustomSizedDepthRT(depthBuffer.referenceSize);
            cmd.CopyTexture(depthBuffer, rtDepthCopy);

            cmd.SetGlobalTexture(PointCloudShaderIDs.SolidCompose.ColorTexture, Resources.GetRTHandle(RTUsage.ColorBuffer));
            cmd.SetGlobalTexture(PointCloudShaderIDs.SolidCompose.OriginalDepth, rtDepthCopy);
            cmd.SetGlobalVector(PointCloudShaderIDs.SolidCompose.ReprojectionVector, GetFovReprojectionVector(targetCamera.camera));
            var depthComposePass = Resources.Passes.depthCompose;

            CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer);
            CoreUtils.DrawFullScreen(cmd, Resources.SolidComposeMaterial, shaderPassId: depthComposePass);
        }

        private void RenderAsSolid(CommandBuffer cmd, HDCamera targetCamera, RenderTargetIdentifier[] rtIds, RTHandle depthBuffer, RTHandle cameraColorBuffer)
        {
            // More than 1 RT currently means that render is targeting GBuffer
            var targetGBuffer = rtIds.Length > 1;
            var calculateNormals = CalculateNormals;
            var smoothNormals = SmoothNormals && calculateNormals;
            var unlitShadows = CalculateNormals && Lighting == LightingMode.ShadowReceiver;
            
            CoreUtils.SetKeyword(cmd, PointCloudShaderIDs.SolidCompose.TargetGBufferKeyword, targetGBuffer);
            CoreUtils.SetKeyword(cmd, PointCloudShaderIDs.SolidCompose.UnlitShadowsKeyword, unlitShadows);
            
            RenderSolidCore(cmd, targetCamera, cameraColorBuffer, calculateNormals, smoothNormals);

            // One texture can't be set as target and read from at the same time - copy needed depth data
            var depthCopy = Resources.GetRTHandle(RTUsage.DepthCopy);
            cmd.CopyTexture(depthBuffer, depthCopy);

            cmd.SetGlobalTexture(PointCloudShaderIDs.SolidCompose.ColorTexture, Resources.GetRTHandle(RTUsage.ColorBuffer));
            cmd.SetGlobalTexture(PointCloudShaderIDs.SolidCompose.NormalTexture, Resources.GetRTHandle(RTUsage.Generic0));
            cmd.SetGlobalTexture(PointCloudShaderIDs.SolidCompose.OriginalDepth, depthCopy);
            cmd.SetGlobalVector(PointCloudShaderIDs.SolidCompose.ReprojectionVector, GetFovReprojectionVector(targetCamera.camera));
            var composePass = Resources.Passes.solidCompose;

            CoreUtils.SetRenderTarget(cmd, rtIds, depthBuffer);
            CoreUtils.DrawFullScreen(cmd, Resources.SolidComposeMaterial, shaderPassId: composePass);

            // cmd.CopyTexture(depthBuffer, rtDepthCopy);
            // SolidBlitMaterial.SetTexture(PointCloudShaderIDs.SolidCompose.OriginalDepth, rtDepthCopy);
            // SolidBlitMaterial.SetTexture(PointCloudShaderIDs.SolidCompose.ColorTexture, rtColor);
            // SolidBlitMaterial.SetVector(PointCloudShaderIDs.SolidCompose.ReprojectionVector, GetFovReprojectionVector(targetCamera.camera));
            // var composePass = SolidBlitMaterial.FindPass("Point Cloud Debug Compose");
            //
            // CoreUtils.SetRenderTarget(cmd, rtIds, depthBuffer);
            // CoreUtils.DrawFullScreen(cmd, SolidBlitMaterial, shaderPassId: composePass);
        }

        private void RenderSolidCore(CommandBuffer cmd, HDCamera targetCamera, RTHandle cameraColorBuffer, bool calculateNormals, bool smoothNormals)
        {
            CoreUtils.SetKeyword(cmd, PointCloudShaderIDs.SolidCompose.LinearDepthKeyword, DebugUseLinearDepth);

            var rt = Resources.GetRTHandle(RTUsage.PointRender);
            var rt1 = Resources.GetRTHandle(RTUsage.Generic0);
            var rt2 = Resources.GetRTHandle(RTUsage.Generic1);
            var rtColor = Resources.GetRTHandle(RTUsage.ColorBuffer);
            var rtDepth = Resources.GetRTHandle(RTUsage.DepthBuffer);

            var width = targetCamera.actualWidth;
            var height = targetCamera.actualHeight;
            var refSize = rt.referenceSize;

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

            cmd.SetGlobalBuffer(PointCloudShaderIDs.Shared.Buffer, GetBufferForCamera(targetCamera));
            cmd.SetGlobalInt(PointCloudShaderIDs.Shared.Colorize, (int)Colorize);
            cmd.SetGlobalFloat(PointCloudShaderIDs.Shared.MinHeight, Bounds.min.y);
            cmd.SetGlobalFloat(PointCloudShaderIDs.Shared.MaxHeight, Bounds.max.y);
            cmd.SetGlobalMatrix(PointCloudShaderIDs.SolidRender.MVPMatrix, solidRenderMvp);
            cmd.DrawProcedural(Matrix4x4.identity, Resources.SolidRenderMaterial, 0, MeshTopology.Points, GetPointCountForCamera(targetCamera));

            var cs = Resources.SolidComputeShader;

            cmd.SetComputeVectorParam(cs, PointCloudShaderIDs.SolidCompute.TextureSize, new Vector4(width - 1, height - 1, 1f / width, 1f / height));
            cmd.SetComputeVectorParam(cs, PointCloudShaderIDs.SolidCompute.FullRTSize, new Vector4(refSize.x - 1, refSize.y - 1, 1f / refSize.x, 1f / refSize.y));
            cmd.SetComputeFloatParam(cs, PointCloudShaderIDs.SolidCompute.FarPlane, targetCamera.camera.farClipPlane);
            cmd.SetComputeMatrixParam(cs, PointCloudShaderIDs.SolidCompute.ProjectionMatrix, projMatrix);
            cmd.SetComputeMatrixParam(cs, PointCloudShaderIDs.SolidCompute.InverseProjectionMatrix, invProjMatrix);
            cmd.SetComputeMatrixParam(cs, PointCloudShaderIDs.SolidCompute.InverseVPMatrix, invViewProjMatrix);
            cmd.SetComputeVectorParam(cs, PointCloudShaderIDs.SolidCompute.InverseReprojectionVector, GetInverseUvFovReprojectionVector(targetCamera.camera));

            var blendSky = DebugBlendSky && cameraColorBuffer != null;
            var setupCopy = Resources.Kernels.GetSetupKernel(DebugUseLinearDepth, DebugForceFill, blendSky);
            cmd.SetComputeTextureParam(cs, setupCopy, PointCloudShaderIDs.SolidCompute.SetupCopy.InputColor, rt, 0);
            cmd.SetComputeTextureParam(cs, setupCopy, PointCloudShaderIDs.SolidCompute.SetupCopy.InputPosition, rtDepth, 0);
            cmd.SetComputeTextureParam(cs, setupCopy, PointCloudShaderIDs.SolidCompute.SetupCopy.OutputPosition, rt1, 0);
            cmd.SetComputeTextureParam(cs, setupCopy, PointCloudShaderIDs.SolidCompute.SetupCopy.OutputColor, rtColor, 0);
            if (blendSky)
                cmd.SetComputeTextureParam(cs, setupCopy, PointCloudShaderIDs.SolidCompute.SetupCopy.PostSkyPreRenderTexture, cameraColorBuffer, 0);
            cmd.SetComputeFloatParam(cs, PointCloudShaderIDs.SolidCompute.SetupCopy.HorizonThreshold, DebugFillThreshold);
            cmd.DispatchCompute(cs, setupCopy, GetGroupSize(width, 8), GetGroupSize(height, 8), 1);

            if (SolidRemoveHidden)
            {
                var downsample = Resources.Kernels.Downsample;
                for (var i = 1; i <= maxLevel + 3; i++)
                {
                    GetMipData(resolution, i - 1, out var mipRes, out var mipVec);
                    cmd.SetComputeVectorParam(cs, PointCloudShaderIDs.SolidCompute.MipTextureSize, mipVec);
                    cmd.SetComputeTextureParam(cs, downsample, PointCloudShaderIDs.SolidCompute.Downsample.InputPosition, rt1, i - 1);
                    cmd.SetComputeTextureParam(cs, downsample, PointCloudShaderIDs.SolidCompute.Downsample.OutputPosition, rt1, i);
                    cmd.DispatchCompute(cs, downsample, GetGroupSize(mipRes.x, 16), GetGroupSize(mipRes.y, 16), 1);
                }

                DebugSolidFixedLevel = Math.Min(Math.Max(DebugSolidFixedLevel, 0), maxLevel);

                var removeHidden = Resources.Kernels.GetRemoveHiddenKernel(DebugShowRemoveHiddenCascades);
                var removeHiddenMagic = RemoveHiddenCascadeOffset * height * 0.5f / Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);

                cmd.SetComputeIntParam(cs, PointCloudShaderIDs.SolidCompute.RemoveHidden.LevelCount, maxLevel);
                cmd.SetComputeTextureParam(cs, removeHidden, PointCloudShaderIDs.SolidCompute.RemoveHidden.Position, rt1);
                cmd.SetComputeTextureParam(cs, removeHidden, PointCloudShaderIDs.SolidCompute.RemoveHidden.Color, rtColor, 0);
                cmd.SetComputeFloatParam(cs, PointCloudShaderIDs.SolidCompute.RemoveHidden.CascadesOffset, removeHiddenMagic);
                cmd.SetComputeFloatParam(cs, PointCloudShaderIDs.SolidCompute.RemoveHidden.CascadesSize, RemoveHiddenCascadeSize);
                cmd.SetComputeIntParam(cs, PointCloudShaderIDs.SolidCompute.RemoveHidden.FixedLevel, DebugSolidFixedLevel);
                cmd.DispatchCompute(cs, removeHidden, GetGroupSize(width, 8), GetGroupSize(height, 8), 1);
            }

            if (DebugSolidPullPush)
            {
                var pullKernel = Resources.Kernels.Pull;
                cmd.SetComputeFloatParam(cs, PointCloudShaderIDs.SolidCompute.PullKernel.FilterExponent, DebugSolidPullParam);

                for (var i = 1; i <= maxLevel; i++)
                {
                    GetMipData(resolution, i, out var mipRes, out var higherMipVec);
                    GetMipData(resolution, i - 1, out _, out var mipVec);
                    cmd.SetComputeVectorParam(cs, PointCloudShaderIDs.SolidCompute.MipTextureSize, mipVec);
                    cmd.SetComputeVectorParam(cs, PointCloudShaderIDs.SolidCompute.HigherMipTextureSize, higherMipVec);
                    cmd.SetComputeIntParam(cs, PointCloudShaderIDs.SolidCompute.PullKernel.SkipWeightMul, i == maxLevel ? 1 : 0);
                    cmd.SetComputeIntParam(cs, PointCloudShaderIDs.SolidCompute.PullKernel.InputLevel, i - 1);
                    cmd.SetComputeTextureParam(cs, pullKernel, PointCloudShaderIDs.SolidCompute.PullKernel.InputColor, rtColor, i - 1);
                    cmd.SetComputeTextureParam(cs, pullKernel, PointCloudShaderIDs.SolidCompute.PullKernel.OutputColor, rtColor, i);
                    cmd.DispatchCompute(cs, pullKernel, GetGroupSize(mipRes.x, 8), GetGroupSize(mipRes.y, 8), 1);
                }

                var pushKernel = Resources.Kernels.Push;

                for (var i = maxLevel; i > 0; i--)
                {
                    GetMipData(resolution, i - 1, out var mipRes, out var mipVec);
                    GetMipData(resolution, i, out _, out var lowerMipVec);
                    cmd.SetComputeVectorParam(cs, PointCloudShaderIDs.SolidCompute.MipTextureSize, mipVec);
                    cmd.SetComputeVectorParam(cs, PointCloudShaderIDs.SolidCompute.HigherMipTextureSize, lowerMipVec);
                    cmd.SetComputeIntParam(cs, PointCloudShaderIDs.SolidCompute.PushKernel.InputLevel, i);
                    cmd.SetComputeTextureParam(cs, pushKernel, PointCloudShaderIDs.SolidCompute.PushKernel.InputColor, rtColor, i);
                    cmd.SetComputeTextureParam(cs, pushKernel, PointCloudShaderIDs.SolidCompute.PushKernel.OutputColor, rtColor, i - 1);
                    cmd.DispatchCompute(cs, pushKernel, GetGroupSize(mipRes.x, 8), GetGroupSize(mipRes.y, 8), 1);
                }

                if (calculateNormals)
                {
                    var calculateNormalsKernel = Resources.Kernels.GetCalculateNormalsKernel(DebugUseLinearDepth);
                    var normalsTarget = smoothNormals ? rt2 : rt1;

                    for (var i = 0; i < maxLevel; ++i)
                    {
                        GetMipData(resolution, i, out var mipRes, out var mipVec);
                        cmd.SetComputeVectorParam(cs, PointCloudShaderIDs.SolidCompute.MipTextureSize, mipVec);
                        cmd.SetComputeIntParam(cs, PointCloudShaderIDs.SolidCompute.CalculateNormals.InputLevel, i);
                        cmd.SetComputeTextureParam(cs, calculateNormalsKernel, PointCloudShaderIDs.SolidCompute.CalculateNormals.Input, rtColor);
                        cmd.SetComputeTextureParam(cs, calculateNormalsKernel, PointCloudShaderIDs.SolidCompute.CalculateNormals.Output, normalsTarget, i);
                        cmd.DispatchCompute(cs, calculateNormalsKernel, GetGroupSize(mipRes.x, 8), GetGroupSize(mipRes.x, 8), 1);
                    }

                    if (smoothNormals)
                    {
                        var smoothNormalsKernel = Resources.Kernels.GetSmoothNormalsKernel(DebugUseLinearDepth, DebugShowSmoothNormalsCascades);
                        var smoothNormalsMagic = SmoothNormalsCascadeOffset * height * 0.5f / Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);

                        cmd.SetComputeTextureParam(cs, smoothNormalsKernel, PointCloudShaderIDs.SolidCompute.SmoothNormals.Input, rt2);
                        cmd.SetComputeTextureParam(cs, smoothNormalsKernel, PointCloudShaderIDs.SolidCompute.SmoothNormals.Output, rt1, 0);
                        cmd.SetComputeFloatParam(cs, PointCloudShaderIDs.SolidCompute.SmoothNormals.CascadesOffset, smoothNormalsMagic);
                        cmd.SetComputeFloatParam(cs, PointCloudShaderIDs.SolidCompute.SmoothNormals.CascadesSize, SmoothNormalsCascadeSize);
                        if (DebugShowSmoothNormalsCascades)
                            cmd.SetComputeTextureParam(cs, smoothNormalsKernel, PointCloudShaderIDs.SolidCompute.SmoothNormals.ColorDebug, rtColor, 0);
                        cmd.DispatchCompute(cs, smoothNormalsKernel, GetGroupSize(width, 8), GetGroupSize(height, 8), 1);
                    }
                }
            }
        }

        private void RenderAsPoints(CommandBuffer cmd, HDCamera targetCamera, RenderTargetIdentifier[] rtIds, RTHandle depthBuffer)
        {
            // More that 1 RT currently means that render is targeting GBuffer
            var targetGBuffer = rtIds.Length > 1;
            CoreUtils.SetKeyword(cmd, PointCloudShaderIDs.PointsRender.ConesKeyword, RenderMode == RenderType.Cones);

            if (ConstantSize && Mathf.Approximately(PixelSize, 1f))
            {
                cmd.SetGlobalBuffer(PointCloudShaderIDs.Shared.Buffer, GetBufferForCamera(targetCamera));
                cmd.SetGlobalMatrix(PointCloudShaderIDs.PointsRender.ModelMatrix, transform.localToWorldMatrix);
                cmd.SetGlobalMatrix(PointCloudShaderIDs.PointsRender.VPMatrix, targetCamera.mainViewConstants.viewProjMatrix);
                cmd.SetGlobalInt(PointCloudShaderIDs.Shared.Colorize, (int)Colorize);
                cmd.SetGlobalFloat(PointCloudShaderIDs.PointsRender.MinHeight, Bounds.min.y);
                cmd.SetGlobalFloat(PointCloudShaderIDs.PointsRender.MaxHeight, Bounds.max.y);

                // This value changes multiple times per frame, so it has to be set through command buffer, hence global
                CoreUtils.SetKeyword(cmd, PointCloudShaderIDs.SolidCompose.TargetGBufferKeyword, targetGBuffer);
                CoreUtils.SetRenderTarget(cmd, rtIds, depthBuffer);

                cmd.DrawProcedural(Matrix4x4.identity, Resources.PointsMaterial, 0, MeshTopology.Points, GetPointCountForCamera(targetCamera));
            }
            else
            {
                SetCirclesMaterialProperties(cmd, targetCamera);

                // This value changes multiple times per frame, so it has to be set through command buffer, hence global
                CoreUtils.SetKeyword(cmd, PointCloudShaderIDs.SolidCompose.TargetGBufferKeyword, targetGBuffer);
                CoreUtils.SetRenderTarget(cmd, rtIds, depthBuffer);

                var pass = Resources.Passes.circlesGBuffer;
                cmd.DrawProcedural(Matrix4x4.identity, Resources.CirclesMaterial, pass, MeshTopology.Points, GetPointCountForCamera(targetCamera));
            }
        }

        private void RenderLidarPoints(CommandBuffer cmd, HDCamera targetCamera, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            SetCirclesMaterialProperties(cmd, targetCamera);

            CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer);
            var pass = Resources.Passes.lidarCircles;
            cmd.DrawProcedural(Matrix4x4.identity, Resources.CirclesMaterial, pass, MeshTopology.Points, PointCount);
        }
        
        private void RenderDepthPoints(CommandBuffer cmd, HDCamera targetCamera, RTHandle colorBuffer, RTHandle depthBuffer)
        {
            SetCirclesMaterialProperties(cmd, targetCamera);

            CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer);
            var pass = Resources.Passes.depthCircles;
            cmd.DrawProcedural(Matrix4x4.identity, Resources.CirclesMaterial, pass, MeshTopology.Points, PointCount);
        }

        private void SetCirclesMaterialProperties(CommandBuffer cmd, HDCamera targetCamera)
        {
            CoreUtils.SetKeyword(cmd, PointCloudShaderIDs.PointsRender.ConesKeyword, RenderMode == RenderType.Cones);
            cmd.SetGlobalBuffer(PointCloudShaderIDs.Shared.Buffer, GetBufferForCamera(targetCamera));
            cmd.SetGlobalMatrix(PointCloudShaderIDs.PointsRender.ModelMatrix, transform.localToWorldMatrix);
            cmd.SetGlobalMatrix(PointCloudShaderIDs.PointsRender.VPMatrix, targetCamera.mainViewConstants.viewProjMatrix);
            cmd.SetGlobalInt(PointCloudShaderIDs.Shared.Colorize, (int)Colorize);
            cmd.SetGlobalFloat(PointCloudShaderIDs.PointsRender.MinHeight, Bounds.min.y);
            cmd.SetGlobalFloat(PointCloudShaderIDs.PointsRender.MaxHeight, Bounds.max.y);

            if (ConstantSize)
            {
                CoreUtils.SetKeyword(cmd, PointCloudShaderIDs.PointsRender.SizeInPixelsKeyword, true);
                cmd.SetGlobalFloat(PointCloudShaderIDs.PointsRender.Size, PixelSize);
            }
            else
            {
                CoreUtils.SetKeyword(cmd, PointCloudShaderIDs.PointsRender.SizeInPixelsKeyword, false);
                cmd.SetGlobalFloat(PointCloudShaderIDs.PointsRender.Size, AbsoluteSize);
                cmd.SetGlobalFloat(PointCloudShaderIDs.PointsRender.MinSize, MinPixelSize);
            }
        }

        public void RenderShadows(CommandBuffer cmd, float worldTexelSize)
        {
            if ((Mask & RenderMask.Shadowcaster) == 0 || Buffer == null || PointCount == 0 || !isActiveAndEnabled)
                return;

            var scale = ShadowPointSize * 0.001f / worldTexelSize;
            var biasPush = ShadowBias * scale;
            var shadowVector = new Vector4(scale, biasPush, 0, 0);

            // Only use the game view buffer, skip shadows for scene view culled points to save performance
            cmd.SetGlobalBuffer(PointCloudShaderIDs.Shared.Buffer, Buffer);
            cmd.SetGlobalMatrix(PointCloudShaderIDs.PointsRender.ModelMatrix, transform.localToWorldMatrix);
            cmd.SetGlobalVector(PointCloudShaderIDs.PointsRender.ShadowVector, shadowVector);

            var pass = Resources.Passes.circlesShadowcaster;
            cmd.DrawProcedural(Matrix4x4.identity, Resources.CirclesMaterial, pass, MeshTopology.Points, PointCount);
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

            var revMult = 1f / mult;
            var border = 0.5f * (1 - revMult);

            return new Vector4(revMult, width * border, height * border, 0f);
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

        private void CalculateMatrices(
            HDCamera targetCamera,
            out Matrix4x4 proj,
            out Matrix4x4 invProj,
            out Matrix4x4 invViewProj,
            out Matrix4x4 solidRenderMvp)
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
