/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud
{
    using System.Collections.Generic;
    using Simulator.Utilities;
    using UnityEngine;
    using UnityEngine.Experimental.Rendering;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    public class PointCloudResources
    {
        private const float AutoReleaseTime = 10f;

        #region member_types

        public class PointCloudPasses
        {
            public readonly int circlesGBuffer;
            public readonly int lidarCircles;
            public readonly int lidarCompose;
            public readonly int depthCompose;
            public readonly int depthCircles;
            public readonly int solidCompose;
            public readonly int circlesShadowcaster;

            public PointCloudPasses(Material pointsMat, Material circlesMat, Material solidComposeMat)
            {
                circlesGBuffer = circlesMat.FindPass("Point Cloud Circles GBuffer");
                lidarCompose = solidComposeMat.FindPass("Point Cloud Lidar Compose");
                depthCompose = solidComposeMat.FindPass("Point Cloud Depth Compose");
                depthCircles = solidComposeMat.FindPass("Point Cloud Depth Circles");
                solidCompose = solidComposeMat.FindPass("Point Cloud Default Compose");
                circlesShadowcaster = circlesMat.FindPass("Point Cloud Circles ShadowCaster");
                lidarCircles = circlesMat.FindPass("Point Cloud Circles Lidar");
            }
        }

        public class PointCloudKernels
        {
            public readonly int Setup;
            public readonly int SetupFF;
            public readonly int SetupLinearDepth;
            public readonly int SetupLinearDepthFF;
            public readonly int SetupSky;
            public readonly int SetupFFSky;
            public readonly int SetupLinearDepthSky;
            public readonly int SetupLinearDepthFFSky;

            public readonly int Downsample;

            public readonly int RemoveHidden;
            public readonly int RemoveHiddenDebug;

            public readonly int Pull;
            public readonly int Push;

            public readonly int CalculateNormals;
            public readonly int CalculateNormalsLinearDepth;

            public readonly int SmoothNormals;
            public readonly int SmoothNormalsLinearDepth;
            public readonly int SmoothNormalsDebug;
            public readonly int SmoothNormalsLinearDepthDebug;

            public PointCloudKernels(ComputeShader cs)
            {
                Setup = cs.FindKernel(PointCloudShaderIDs.SolidCompute.SetupCopy.KernelName);
                SetupFF = cs.FindKernel(PointCloudShaderIDs.SolidCompute.SetupCopy.KernelNameFF);
                SetupLinearDepth = cs.FindKernel(PointCloudShaderIDs.SolidCompute.SetupCopy.KernelNameLinearDepth);
                SetupLinearDepthFF = cs.FindKernel(PointCloudShaderIDs.SolidCompute.SetupCopy.KernelNameLinearDepthFF);
                SetupSky = cs.FindKernel(PointCloudShaderIDs.SolidCompute.SetupCopy.KernelNameSky);
                SetupFFSky = cs.FindKernel(PointCloudShaderIDs.SolidCompute.SetupCopy.KernelNameFFSky);
                SetupLinearDepthSky = cs.FindKernel(PointCloudShaderIDs.SolidCompute.SetupCopy.KernelNameLinearDepthSky);
                SetupLinearDepthFFSky = cs.FindKernel(PointCloudShaderIDs.SolidCompute.SetupCopy.KernelNameLinearDepthFFSky);

                Downsample = cs.FindKernel(PointCloudShaderIDs.SolidCompute.Downsample.KernelName);

                RemoveHidden = cs.FindKernel(PointCloudShaderIDs.SolidCompute.RemoveHidden.KernelName);
                RemoveHiddenDebug = cs.FindKernel(PointCloudShaderIDs.SolidCompute.RemoveHidden.DebugKernelName);

                Pull = cs.FindKernel(PointCloudShaderIDs.SolidCompute.PullKernel.KernelName);
                Push = cs.FindKernel(PointCloudShaderIDs.SolidCompute.PushKernel.KernelName);

                CalculateNormals = cs.FindKernel(PointCloudShaderIDs.SolidCompute.CalculateNormals.KernelName);
                CalculateNormalsLinearDepth = cs.FindKernel(PointCloudShaderIDs.SolidCompute.CalculateNormals.KernelNameLinearDepth);

                SmoothNormals = cs.FindKernel(PointCloudShaderIDs.SolidCompute.SmoothNormals.KernelName);
                SmoothNormalsDebug = cs.FindKernel(PointCloudShaderIDs.SolidCompute.SmoothNormals.DebugKernelName);
                SmoothNormalsLinearDepth = cs.FindKernel(PointCloudShaderIDs.SolidCompute.SmoothNormals.KernelNameLinearDepth);
                SmoothNormalsLinearDepthDebug = cs.FindKernel(PointCloudShaderIDs.SolidCompute.SmoothNormals.DebugKernelNameLinearDepth);
            }

            public int GetSetupKernel(bool linearDepth, bool forceFill, bool blendSky)
            {
                if (linearDepth)
                {
                    if (blendSky)
                        return forceFill ? SetupLinearDepthFFSky : SetupLinearDepthSky;
                    else
                        return forceFill ? SetupLinearDepthFF : SetupLinearDepth;
                }
                else
                {
                    if (blendSky)
                        return forceFill ? SetupFFSky : SetupSky;
                    else
                        return forceFill ? SetupFF : Setup;
                }
            }

            public int GetRemoveHiddenKernel(bool debug)
            {
                return debug ? RemoveHiddenDebug : RemoveHidden;
            }

            public int GetCalculateNormalsKernel(bool linearDepth)
            {
                return linearDepth ? CalculateNormalsLinearDepth : CalculateNormals;
            }

            public int GetSmoothNormalsKernel(bool linearDepth, bool debug)
            {
                if (linearDepth)
                    return debug ? SmoothNormalsLinearDepthDebug : SmoothNormalsLinearDepth;
                else
                    return debug ? SmoothNormalsDebug : SmoothNormals;
            }
        }

        private class CustomSizeDepthRT
        {
            public readonly Vector2Int size;

            private readonly RTHandle handle;
            private float lastUseTime;

            public RTHandle Handle
            {
                get
                {
                    lastUseTime = Time.unscaledTime;
                    return handle;
                }
            }

            public bool UsedRecently => Time.unscaledTime - lastUseTime >= AutoReleaseTime;

            public CustomSizeDepthRT(Vector2Int size)
            {
                this.size = size;

                handle = RTHandles.Alloc(
                    size.x,
                    size.y,
                    TextureXR.slices,
                    DepthBits.Depth32,
                    GraphicsFormat.R32_UInt,
                    dimension: TextureXR.dimension,
                    useDynamicScale: true,
                    name: "PC_CustomSizeDepth",
                    wrapMode: TextureWrapMode.Clamp);
            }

            public void Release()
            {
                RTHandles.Release(handle);
            }
        }

        #endregion

        private RTHandle[] handles;

        private List<CustomSizeDepthRT> customSizeDepthRTs;

        private float lastValidationTime;

        public PointCloudPasses Passes { get; private set; }

        public PointCloudKernels Kernels { get; private set; }

        public Material PointsMaterial { get; private set; }

        public Material CirclesMaterial { get; private set; }

        public Material SolidRenderMaterial { get; private set; }

        public Material SolidComposeMaterial { get; private set; }

        public ComputeShader SolidComputeShader { get; private set; }

        public PointCloudResources()
        {
            AllocRTHandles();
            CreateMaterials();
        }

        public void ReleaseAll()
        {
            ReleaseRTHandles();
            DestroyMaterials();
        }

        private void AllocRTHandles()
        {
            customSizeDepthRTs = new List<CustomSizeDepthRT>();

            handles = new RTHandle[(int) RTUsage.Count];

            handles[(int) RTUsage.PointRender] = RTHandles.Alloc(
                Vector2.one,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                name: "PC_PointRender",
                enableRandomWrite: true,
                useMipMap: false,
                autoGenerateMips: false,
                wrapMode: TextureWrapMode.Clamp);

            handles[(int) RTUsage.DepthBuffer] = RTHandles.Alloc(
                Vector2.one,
                colorFormat: GraphicsFormat.R32_UInt,
                depthBufferBits: DepthBits.Depth32,
                name: "PC_Depth",
                wrapMode: TextureWrapMode.Clamp);

            handles[(int) RTUsage.Generic0] = RTHandles.Alloc(
                Vector2.one,
                colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
                name: "PC_Generic0",
                enableRandomWrite: true,
                useMipMap: true,
                autoGenerateMips: false,
                wrapMode: TextureWrapMode.Clamp);

            handles[(int) RTUsage.Generic1] = RTHandles.Alloc(
                Vector2.one,
                colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
                name: "PC_Generic1",
                enableRandomWrite: true,
                useMipMap: true,
                autoGenerateMips: false,
                wrapMode: TextureWrapMode.Clamp);

            handles[(int) RTUsage.ColorBuffer] = RTHandles.Alloc(
                Vector2.one,
                colorFormat: GraphicsFormat.R32G32B32A32_SFloat,
                name: "PC_Color",
                enableRandomWrite: true,
                useMipMap: true,
                autoGenerateMips: false,
                wrapMode: TextureWrapMode.Clamp);

            handles[(int) RTUsage.DepthCopy] = RTHandles.Alloc(
                Vector2.one,
                TextureXR.slices,
                DepthBits.Depth32,
                GraphicsFormat.R32_UInt,
                dimension: TextureXR.dimension,
                useDynamicScale: true,
                name: "PC_DepthCopy",
                wrapMode: TextureWrapMode.Clamp);
        }

        public void UpdateSHCoefficients(CommandBuffer cmd, Vector3 position)
        {
            LightProbes.GetInterpolatedProbe(position, null, out var shl2);

            for (var i = 0; i < 3; ++i)
            {
                cmd.SetGlobalVector(PointCloudShaderIDs.SHCoefficients.SHA[i],
                    new Vector4(shl2[i, 3], shl2[i, 1], shl2[i, 2], shl2[i, 0] - shl2[i, 6]));

                cmd.SetGlobalVector(PointCloudShaderIDs.SHCoefficients.SHB[i],
                    new Vector4(shl2[i, 4], shl2[i, 6], shl2[i, 5] * 3, shl2[i, 7]));
            }

            cmd.SetGlobalVector(PointCloudShaderIDs.SHCoefficients.SHC,
                new Vector4(shl2[0, 8], shl2[2, 8], shl2[1, 8], 1));
        }

        private void ReleaseRTHandles()
        {
            foreach (var handle in handles)
                RTHandles.Release(handle);

            foreach (var handle in customSizeDepthRTs)
                handle.Release();

            customSizeDepthRTs.Clear();
            customSizeDepthRTs = null;

            handles = null;
        }

        private void CreateMaterials()
        {
            // Points
            PointsMaterial = new Material(RuntimeSettings.Instance.PointCloudPoints);
            PointsMaterial.hideFlags = HideFlags.DontSave;
            PointsMaterial.SetInt(PointCloudShaderIDs.PointsRender.StencilRef, HDRenderPipeline.StencilRefGBuffer);
            PointsMaterial.SetInt(PointCloudShaderIDs.PointsRender.StencilMask, HDRenderPipeline.StencilWriteMaskGBuffer);

            // Circles
            CirclesMaterial = new Material(RuntimeSettings.Instance.PointCloudCircles);
            CirclesMaterial.hideFlags = HideFlags.DontSave;
            CirclesMaterial.SetInt(PointCloudShaderIDs.PointsRender.StencilRef, HDRenderPipeline.StencilRefGBuffer);
            CirclesMaterial.SetInt(PointCloudShaderIDs.PointsRender.StencilMask, HDRenderPipeline.StencilWriteMaskGBuffer);

            // Solid
            SolidComputeShader = Object.Instantiate(RuntimeSettings.Instance.PointCloudSolid);

            SolidRenderMaterial = new Material(RuntimeSettings.Instance.PointCloudSolidRender);
            SolidRenderMaterial.hideFlags = HideFlags.DontSave;

            SolidComposeMaterial = new Material(RuntimeSettings.Instance.PointCloudSolidBlit);
            SolidComposeMaterial.hideFlags = HideFlags.DontSave;
            SolidComposeMaterial.SetInt(PointCloudShaderIDs.PointsRender.StencilRef, HDRenderPipeline.StencilRefGBuffer);
            SolidComposeMaterial.SetInt(PointCloudShaderIDs.PointsRender.StencilMask, HDRenderPipeline.StencilWriteMaskGBuffer);
            SolidComposeMaterial.SetFloat(PointCloudShaderIDs.SolidCompose.UnlitShadowsFilter, HDRenderPipeline.UnlitShadowsFilter);

            Passes = new PointCloudPasses(PointsMaterial, CirclesMaterial, SolidComposeMaterial);
            Kernels = new PointCloudKernels(SolidComputeShader);
        }

        private void DestroyMaterials()
        {
            CoreUtils.Destroy(PointsMaterial);
            CoreUtils.Destroy(CirclesMaterial);
            CoreUtils.Destroy(SolidComputeShader);
            CoreUtils.Destroy(SolidRenderMaterial);
            CoreUtils.Destroy(SolidComposeMaterial);

            Passes = null;
            Kernels = null;
        }

        public RTHandle GetRTHandle(RTUsage usage)
        {
            return handles[(int) usage];
        }

        public RTHandle GetCustomSizedDepthRT(Vector2Int size)
        {
            ValidateCustomSizedDepthRTs();

            for (var i = 0; i < customSizeDepthRTs.Count; ++i)
            {
                if (customSizeDepthRTs[i].size == size)
                    return customSizeDepthRTs[i].Handle;
            }

            var rt = new CustomSizeDepthRT(size);
            customSizeDepthRTs.Add(rt);

            return rt.Handle;
        }

        private void ValidateCustomSizedDepthRTs()
        {
            // Check old handles periodically and release the ones that were not used during that period
            if (!(Time.unscaledTime - lastValidationTime >= AutoReleaseTime))
                return;

            lastValidationTime = Time.unscaledTime;

            for (var i = 0; i < customSizeDepthRTs.Count; ++i)
            {
                if (customSizeDepthRTs[i].UsedRecently)
                    continue;

                customSizeDepthRTs[i].Release();
                customSizeDepthRTs.RemoveAt(i--);
            }
        }
    }
}