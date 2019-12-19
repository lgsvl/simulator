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
    using UnityEngine.Serialization;
    using Utilities;

    [ExecuteInEditMode]
    public class PointCloudRenderer : MonoBehaviour
    {
        #region member_types

        private static class ShaderVariables
        {
            public static class PointsRender
            {
                public const string SizeInPixelsKeyword = "_SIZE_IN_PIXELS";
                public static readonly int Buffer = Shader.PropertyToID("_Buffer");
                public static readonly int ModelMatrix = Shader.PropertyToID("_Transform");
                public static readonly int Colorize = Shader.PropertyToID("_Colorize");
                public static readonly int MinHeight = Shader.PropertyToID("_MinHeight");
                public static readonly int MaxHeight = Shader.PropertyToID("_MaxHeight");
                public static readonly int Size = Shader.PropertyToID("_Size");
                public static readonly int MinSize = Shader.PropertyToID("_MinSize");
            }

            public static class SolidRender
            {
                public static readonly int Buffer = Shader.PropertyToID("_Buffer");
                public static readonly int Colorize = Shader.PropertyToID("_Colorize");
                public static readonly int MinHeight = Shader.PropertyToID("_MinHeight");
                public static readonly int MaxHeight = Shader.PropertyToID("_MaxHeight");
                public static readonly int ViewMatrix = Shader.PropertyToID("_Transform");
                public static readonly int ProjectionMatrix = Shader.PropertyToID("_ViewToClip");
            }
            
            public static class SolidBlit
            {
                public const string ColorOnlyKeyword = "COLOR_ONLY";
                public const string NormalsOnlyKeyword = "NORMALS_ONLY";
                public const string DepthOnlyKeyword = "DEPTH_ONLY";
                public static readonly int ColorTex = Shader.PropertyToID("_ColorTex");
                public static readonly int NormalDepthTex = Shader.PropertyToID("_NormalDepthTex");
                public static readonly int FarPlane = Shader.PropertyToID("_FarPlane");
                public static readonly int UvMultiplier = Shader.PropertyToID("_SRMul");
                public static readonly int DebugLevel = Shader.PropertyToID("_DebugLevel");
                public static readonly int ReprojectionMatrix = Shader.PropertyToID("_ReprojectionMatrix");
                public static readonly int InverseProjectionMatrix = Shader.PropertyToID("_InvProjMatrix");
            }

            public static class SolidCompute
            {
                public static class SetupClear
                {
                    public const string KernelName = "SetupClear";
                    public static readonly int Position = Shader.PropertyToID("_SetupClearPosition");
                    public static readonly int Color = Shader.PropertyToID("_SetupClearColor");
                    public static readonly int DepthBuffer = Shader.PropertyToID("_SetupClearDepthBuffer");
                    public static readonly int DepthRaw = Shader.PropertyToID("_SetupClearDepthRaw");
                }

                public static class SetupCopy
                {
                    public const string KernelName = "SetupCopy";
                    public static readonly int FarPlane = Shader.PropertyToID("_FarPlane");
                    public static readonly int InputColor = Shader.PropertyToID("_SetupCopyInput");
                    public static readonly int InputPosition = Shader.PropertyToID("_SetupCopyInputPos");
                    public static readonly int OutputColor = Shader.PropertyToID("_SetupCopyColor");
                    public static readonly int OutputPosition = Shader.PropertyToID("_SetupCopyPosition");
                    public static readonly int DepthBuffer = Shader.PropertyToID("_SetupCopyDepthBuffer");
                    public static readonly int DepthRaw = Shader.PropertyToID("_SetupCopyDepthRaw");
                    public static readonly int InverseProjectionMatrix = Shader.PropertyToID("_SetupCopyInverseProj");
                }

                public static class Downsample
                {
                    public const string KernelName = "Downsample";
                    public static readonly int PosMax = Shader.PropertyToID("_DownsamplePosMax");
                    public static readonly int InputPosition = Shader.PropertyToID("_DownsampleInput");
                    public static readonly int OutputPosition = Shader.PropertyToID("_DownsampleOutput");
                    public static readonly int InputDepth = Shader.PropertyToID("_DownsampleDepthRawInput");
                    public static readonly int OutputDepth = Shader.PropertyToID("_DownsampleDepthRawOutput");
                }

                public static class RemoveHidden
                {
                    public const string KernelName = "RemoveHidden";
                    public const string DebugKernelName = "RemoveHiddenDebug";
                    public static readonly int LevelCount = Shader.PropertyToID("_RemoveHiddenLevelCount");
                    public static readonly int Position = Shader.PropertyToID("_RemoveHiddenPosition");
                    public static readonly int Color = Shader.PropertyToID("_RemoveHiddenColor");
                    public static readonly int DepthBuffer = Shader.PropertyToID("_RemoveHiddenDepthBuffer");
                    public static readonly int DepthRaw = Shader.PropertyToID("_RemoveHiddenDepthRaw");
                    public static readonly int CascadesOffset = Shader.PropertyToID("_RemoveHiddenCascadesOffset");
                    public static readonly int CascadesSize = Shader.PropertyToID("_RemoveHiddenCascadesSize");
                    public static readonly int FixedLevel = Shader.PropertyToID("_RemoveHiddenLevel");
                }

                public static class ApplyPreviousFrame
                {
                    public const string KernelName = "ApplyPreviousFrame";
                    public static readonly int InputColor = Shader.PropertyToID("_PrevColorInput");
                    public static readonly int OutputColor = Shader.PropertyToID("_PrevColorOutput");
                    public static readonly int InputDepth = Shader.PropertyToID("_PrevDepthInput");
                    public static readonly int OutputDepth = Shader.PropertyToID("_PrevDepthOutput");
                    public static readonly int PrevToCurrentMatrix = Shader.PropertyToID("_PrevToCurrentMatrix");
                }
                
                public static class CopyFrame
                {
                    public const string KernelName = "CopyFrame";
                    public static readonly int InputColor = Shader.PropertyToID("_CopyFrameInputColor");
                    public static readonly int OutputColor = Shader.PropertyToID("_CopyFrameOutputColor");
                    public static readonly int InputDepth = Shader.PropertyToID("_CopyFrameInputDepth");
                    public static readonly int OutputDepth = Shader.PropertyToID("_CopyFrameOutputDepth");
                }

                public static class PullKernel
                {
                    public const string KernelName = "PullKernel";
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
                    public static readonly int InputColor = Shader.PropertyToID("_PushColorInput");
                    public static readonly int OutputColor = Shader.PropertyToID("_PushColorOutput");
                    public static readonly int InputDepth = Shader.PropertyToID("_PushDepthBufferInput");
                    public static readonly int OutputDepth = Shader.PropertyToID("_PushDepthBufferOutput");
                }

                public static class CalculateNormals
                {
                    public const string KernelName = "CalculateNormals";
                    public static readonly int InputOutput = Shader.PropertyToID("_NormalsInOut");
                }

                public static class SmoothNormals
                {
                    public const string KernelName = "SmoothNormals";
                    public const string DebugKernelName = "SmoothNormalsDebug";
                    public static readonly int Input = Shader.PropertyToID("_SmoothNormalsIn");
                    public static readonly int Output = Shader.PropertyToID("_SmoothNormalsOut");
                    public static readonly int CascadesOffset = Shader.PropertyToID("_SmoothNormalsCascadesOffset");
                    public static readonly int CascadesSize = Shader.PropertyToID("_SmoothNormalsCascadesSize");
                    public static readonly int ColorDebug = Shader.PropertyToID("_SmoothNormalsColorDebug");
                }
            }
        }
        
        public enum RenderType
        {
            Points,
            Solid,
        }

        public enum SolidRenderType
        {
            Composed,
            Color,
            Normals,
            Depth
        }

        public enum ColorizeType
        {
            Colors = 0,
            Intensity = 1,
            RainbowIntensity = 2,
            RainbowHeight = 3,
        }

        #endregion

        public PointCloudData Data;

        public ColorizeType Colorize = ColorizeType.RainbowIntensity;

        public RenderType Render = RenderType.Points;

        public SolidRenderType SolidRender = SolidRenderType.Color;

        public bool ConstantSize;

        [Range(1, 32)]
        public float PixelSize = 6.0f;

        [Range(0.001f, 0.3f)]
        public float AbsoluteSize = 0.05f;

        [Range(1, 8)]
        public float MinPixelSize = 3.0f;

        [FormerlySerializedAs("DebugSolidMetric")]
        [Range(0.01f, 10.0f)]
        public float RemoveHiddenCascadeOffset = 1f;

        [FormerlySerializedAs("DebugSolidMetric2")]
        [Range(0.01f, 5.0f)]
        public float RemoveHiddenCascadeSize = 1f;

        [Range(0.01f, 20f)]
        public float DebugSolidPullParam = 4f;
        
        [Range(0.01f, 10.0f)]
        public float SmoothNormalsCascadeOffset = 1.3f;
        
        [Range(0.01f, 5.0f)]
        public float SmoothNormalsCascadeSize = 4f;

        public bool SolidFovReprojection;

        public bool TemporalSmoothing = true;

        [Range(0f, 1f)]
        public float FramePersistence = 0.1f;

        public bool PreserveTexelSize;

        [Range(1f, 1.5f)]
        public float ReprojectionRatio = 1.1f;
        
        public int DebugSolidBlitLevel;
        public bool SolidRemoveHidden = true;
        public bool DebugSolidPullPush = true;
        public int DebugSolidFixedLevel;
        public bool DebugShowRemoveHiddenCascades;
        public bool DebugShowSmoothNormalsCascades;

        private Matrix4x4 previousView;
        private Matrix4x4 previousProj;
        private bool previousFrameDataAvailable;

        protected ComputeBuffer Buffer;

        private Material PointsMaterial;
        private Material CirclesMaterial;

        private ComputeShader SolidComputeShader;
        private Material SolidRenderMaterial;
        private Material SolidBlitMaterial;

        private RenderTexture rt;
        private RenderTexture rtPos;
        private RenderTexture rtMask;
        private RenderTexture rtPosition;
        private RenderTexture rtColor;
        private RenderTexture rtDepth;
        private RenderTexture rtPreviousColor;
        private RenderTexture rtPreviousDepth;
        private RenderTexture rtNormalDepth;
        private RenderTexture rtDebug;

        protected virtual Bounds Bounds => Data == null ? default : Data.Bounds;

        protected virtual int PointCount => Buffer.count;

        protected virtual Camera TargetCamera => null;

        protected virtual void OnEnable()
        {
            if (Data != null)
            {
                Buffer = new ComputeBuffer(Data.Count, Data.Stride, ComputeBufferType.Default, ComputeBufferMode.Immutable);
                Buffer.SetData(Data.Points);
            }
        }

        protected virtual void OnDisable()
        {
            Buffer?.Release();
            Buffer = null;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (PointsMaterial != null) Destroy(PointsMaterial);
                if (CirclesMaterial != null) Destroy(CirclesMaterial);
                if (SolidRenderMaterial != null) Destroy(SolidRenderMaterial);
                if (SolidBlitMaterial != null) Destroy(SolidBlitMaterial);
                if (SolidComputeShader != null) Destroy(SolidComputeShader);
            }
            else
            {
                if (PointsMaterial != null) DestroyImmediate(PointsMaterial);
                if (CirclesMaterial != null) DestroyImmediate(CirclesMaterial);
                if (SolidRenderMaterial != null) DestroyImmediate(SolidRenderMaterial);
                if (SolidBlitMaterial != null) DestroyImmediate(SolidBlitMaterial);
                if (SolidComputeShader != null) DestroyImmediate(SolidComputeShader);
            }

            if (rt != null) rt.Release();
            if (rtPos != null) rtPos.Release();
            if (rtMask != null) rtMask.Release();
            if (rtPosition != null) rtPosition.Release();
            if (rtColor != null) rtColor.Release();
            if (rtDepth != null) rtDepth.Release();
            if (rtNormalDepth != null) rtNormalDepth.Release();
            if (rtDebug != null) rtDebug.Release();
        }

        private void CreatePointsMaterial()
        {
            if (PointsMaterial == null)
            {
                PointsMaterial = new Material(RuntimeSettings.Instance.PointCloudPoints);
                PointsMaterial.hideFlags = HideFlags.DontSave;
            }

            if (CirclesMaterial == null)
            {
                CirclesMaterial = new Material(RuntimeSettings.Instance.PointCloudCircles);
                CirclesMaterial.hideFlags = HideFlags.DontSave;
            }
        }

        private void CreateSolidMaterial()
        {
            if (SolidComputeShader == null)
            {
                SolidComputeShader = Instantiate(RuntimeSettings.Instance.PointCloudSolid);

                SolidRenderMaterial = new Material(RuntimeSettings.Instance.PointCloudSolidRender);
                SolidRenderMaterial.hideFlags = HideFlags.DontSave;
                SolidRenderMaterial.SetBuffer(ShaderVariables.SolidRender.Buffer, Buffer);

                SolidBlitMaterial = new Material(RuntimeSettings.Instance.PointCloudSolidBlit);
                SolidBlitMaterial.hideFlags = HideFlags.DontSave;
            }
        }

        private void LateUpdate()
        {
            if (Buffer == null)
            {
                return;
            }

            if (Render == RenderType.Points)
            {
                RenderAsPoints(ConstantSize, PixelSize);
            }
            else if (Render == RenderType.Solid)
            {
                if (Application.isPlaying)
                {
                    // DebugRender();
                    RenderAsSolid();
                }
                else
                {
                    RenderAsPoints(true, 1.0f);
                }
            }
        }

        private void DebugRender()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            if (Input.GetKeyDown(KeyCode.K))
            {
                previousFrameDataAvailable = false;
                RenderAsSolid();
                mainCamera.transform.position += Vector3.right * 2;
                // mainCamera.transform.Rotate(0, 10, 0);
                RenderAsSolid();
            }
            else
            {
                if (rt != null)
                {
                    SolidBlitMaterial.SetTexture(ShaderVariables.SolidBlit.ColorTex, rtColor);
                    SolidBlitMaterial.SetTexture(ShaderVariables.SolidBlit.NormalDepthTex, rtDebug);
                    SolidBlitMaterial.SetFloat(ShaderVariables.SolidBlit.FarPlane, mainCamera.farClipPlane);
                    SolidBlitMaterial.SetFloat(ShaderVariables.SolidBlit.UvMultiplier, 1f);
                    SolidBlitMaterial.SetInt(ShaderVariables.SolidBlit.DebugLevel, 0);
                    SolidBlitMaterial.SetMatrix(ShaderVariables.SolidBlit.ReprojectionMatrix,
                        GetReprojectionMatrix(mainCamera));
                    SolidBlitMaterial.SetMatrix(ShaderVariables.SolidBlit.InverseProjectionMatrix,
                        GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false).inverse);
                    SetSolidBlitType();

                    Graphics.DrawProcedural(SolidBlitMaterial, GetWorldBounds(), MeshTopology.Triangles, 3,
                        camera: mainCamera, layer: 1);
                }
            }
        }

        private void RenderAsSolid()
        {
            CreateSolidMaterial();

            // TODO: camera
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;
            
            var width = mainCamera.pixelWidth;
            var height = mainCamera.pixelHeight;

            var screenRescaleMultiplier = 1f;
            if (SolidFovReprojection && PreserveTexelSize)
            {
                screenRescaleMultiplier = GetFovReprojectionMultiplier(mainCamera);
                width = (int) (width * screenRescaleMultiplier);
                height = (int) (height * screenRescaleMultiplier);
            }

            var fov = mainCamera.fieldOfView;
            if (SolidFovReprojection)
                fov *= ReprojectionRatio;

            var size = Math.Max(Mathf.NextPowerOfTwo(width), Mathf.NextPowerOfTwo(height));

            if (rt == null || rt.width != width || rt.height != height)
            {
                if (rt != null)
                    rt.Release();
                rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                rt.enableRandomWrite = true;
                rt.autoGenerateMips = false;
                rt.useMipMap = false;
                rt.Create();

                if (rtPos != null)
                    rtPos.Release();
                rtPos = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                rtPos.enableRandomWrite = true;
                rtPos.autoGenerateMips = false;
                rtPos.useMipMap = false;
                rtPos.Create();

                if (rtMask != null)
                    rtMask.Release();
                rtMask = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                rtMask.enableRandomWrite = true;
                rtMask.autoGenerateMips = false;
                rtMask.useMipMap = false;
                rtMask.Create();

                if (rtPosition != null)
                    rtPosition.Release();
                rtPosition = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                rtPosition.enableRandomWrite = true;
                rtPosition.autoGenerateMips = false;
                rtPosition.useMipMap = true;
                rtPosition.Create();

                if (rtColor != null)
                    rtColor.Release();
                rtColor = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                rtColor.enableRandomWrite = true;
                rtColor.autoGenerateMips = false;
                rtColor.useMipMap = true;
                rtColor.Create();

                if (rtDepth != null)
                    rtDepth.Release();
                rtDepth = new RenderTexture(size, size, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                rtDepth.enableRandomWrite = true;
                rtDepth.autoGenerateMips = false;
                rtDepth.useMipMap = true;
                rtDepth.Create();

                if (rtNormalDepth != null)
                    rtNormalDepth.Release();
                rtNormalDepth = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                rtNormalDepth.enableRandomWrite = true;
                rtNormalDepth.autoGenerateMips = false;
                rtNormalDepth.useMipMap = true;
                rtNormalDepth.Create();

                if (rtDebug != null)
                    rtDebug.Release();
                rtDebug = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                rtDebug.enableRandomWrite = true;
                rtDebug.autoGenerateMips = false;
                rtDebug.useMipMap = false;
                rtDebug.Create();
            }

            if (TemporalSmoothing && (rtPreviousColor == null || rtPreviousColor.width != width || rtPreviousColor.height != height))
            {
                previousFrameDataAvailable = false;
                
                if (rtPreviousColor != null)
                    rtPreviousColor.Release();
                rtPreviousColor = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                rtPreviousColor.enableRandomWrite = true;
                rtPreviousColor.autoGenerateMips = false;
                rtPreviousColor.useMipMap = false;
                rtPreviousColor.Create();
                
                if (rtPreviousDepth != null)
                    rtPreviousDepth.Release();
                rtPreviousDepth = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                rtPreviousDepth.enableRandomWrite = true;
                rtPreviousDepth.autoGenerateMips = false;
                rtPreviousDepth.useMipMap = false;
                rtPreviousDepth.Create();
            }

            var maxLevel = 0;
            while ((size >> maxLevel) > 8)
            {
                maxLevel++;
            }

            Graphics.SetRenderTarget(new[] {rt.colorBuffer, rtPos.colorBuffer}, rt.depthBuffer);
            GL.Clear(true, true, Color.clear);
            var proj = GetProjectionMatrix(mainCamera);
            var invProj = proj.inverse;

            SolidRenderMaterial.SetInt(ShaderVariables.SolidRender.Colorize, (int) Colorize);
            SolidRenderMaterial.SetFloat(ShaderVariables.SolidRender.MinHeight, Bounds.min.y);
            SolidRenderMaterial.SetFloat(ShaderVariables.SolidRender.MaxHeight, Bounds.max.y);
            SolidRenderMaterial.SetMatrix(ShaderVariables.SolidRender.ViewMatrix, mainCamera.worldToCameraMatrix * transform.localToWorldMatrix);
            SolidRenderMaterial.SetMatrix(ShaderVariables.SolidRender.ProjectionMatrix, proj);
            SolidRenderMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, PointCount);
            
            var setupClear = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.SetupClear.KernelName);
            SolidComputeShader.SetTexture(setupClear, ShaderVariables.SolidCompute.SetupClear.Position, rtPosition, 0);
            SolidComputeShader.SetTexture(setupClear, ShaderVariables.SolidCompute.SetupClear.Color, rtColor, 0);
            SolidComputeShader.SetTexture(setupClear, ShaderVariables.SolidCompute.SetupClear.DepthBuffer, rtNormalDepth, 0);
            SolidComputeShader.SetTexture(setupClear, ShaderVariables.SolidCompute.SetupClear.DepthRaw, rtDepth, 0);
            SolidComputeShader.Dispatch(setupClear, size / 8, size / 8, 1);

            var setupCopy = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.SetupCopy.KernelName);
            SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.SetupCopy.FarPlane, mainCamera.farClipPlane);
            SolidComputeShader.SetTexture(setupCopy, ShaderVariables.SolidCompute.SetupCopy.InputColor, rt, 0);
            SolidComputeShader.SetTexture(setupCopy, ShaderVariables.SolidCompute.SetupCopy.InputPosition, rtPos, 0);
            SolidComputeShader.SetTexture(setupCopy, ShaderVariables.SolidCompute.SetupCopy.OutputPosition, rtPosition, 0);
            SolidComputeShader.SetTexture(setupCopy, ShaderVariables.SolidCompute.SetupCopy.OutputColor, rtColor, 0);
            SolidComputeShader.SetTexture(setupCopy, ShaderVariables.SolidCompute.SetupCopy.DepthBuffer, rtNormalDepth, 0);
            SolidComputeShader.SetTexture(setupCopy, ShaderVariables.SolidCompute.SetupCopy.DepthRaw, rtDepth, 0);
            SolidComputeShader.SetMatrix(ShaderVariables.SolidCompute.SetupCopy.InverseProjectionMatrix, invProj);
            SolidComputeShader.Dispatch(setupCopy, size / 8, size / 8, 1);

            if (SolidRemoveHidden)
            {
                var posMax = new[] {width - 1, height - 1};
                var downsample = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.Downsample.KernelName);
                for (var i = 1; i <= maxLevel + 3; i++)
                {
                    SolidComputeShader.SetInts(ShaderVariables.SolidCompute.Downsample.PosMax, posMax);
                    posMax[0] = (posMax[0] + 1) / 2 - 1;
                    posMax[1] = (posMax[1] + 1) / 2 - 1;
                    SolidComputeShader.SetTexture(downsample, ShaderVariables.SolidCompute.Downsample.InputPosition, rtPosition, i - 1);
                    SolidComputeShader.SetTexture(downsample, ShaderVariables.SolidCompute.Downsample.OutputPosition, rtPosition, i);
                    SolidComputeShader.SetTexture(downsample, ShaderVariables.SolidCompute.Downsample.InputDepth, rtDepth, i - 1);
                    SolidComputeShader.SetTexture(downsample, ShaderVariables.SolidCompute.Downsample.OutputDepth, rtDepth, i);
                    SolidComputeShader.Dispatch(downsample, Math.Max(1, (size >> i) / 8), Math.Max(1, (size >> i) / 8), 1);
                }

                DebugSolidFixedLevel = Math.Min(Math.Max(DebugSolidFixedLevel, 0), maxLevel);

                var removeHidden = DebugShowRemoveHiddenCascades
                    ? SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.RemoveHidden.DebugKernelName)
                    : SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.RemoveHidden.KernelName);
                var removeHiddenMagic = RemoveHiddenCascadeOffset * height * 0.5f / Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);

                SolidComputeShader.SetInt(ShaderVariables.SolidCompute.RemoveHidden.LevelCount, maxLevel);
                SolidComputeShader.SetTexture(removeHidden, ShaderVariables.SolidCompute.RemoveHidden.Position, rtPosition);
                SolidComputeShader.SetTexture(removeHidden, ShaderVariables.SolidCompute.RemoveHidden.Color, rtColor, 0);
                SolidComputeShader.SetTexture(removeHidden, ShaderVariables.SolidCompute.RemoveHidden.DepthBuffer, rtNormalDepth, 0);
                SolidComputeShader.SetTexture(removeHidden, ShaderVariables.SolidCompute.RemoveHidden.DepthRaw, rtDepth);
                SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.RemoveHidden.CascadesOffset, removeHiddenMagic);
                SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.RemoveHidden.CascadesSize, RemoveHiddenCascadeSize);
                SolidComputeShader.SetInt(ShaderVariables.SolidCompute.RemoveHidden.FixedLevel, DebugSolidFixedLevel);
                SolidComputeShader.Dispatch(removeHidden, size / 8, size / 8, 1);

                if (TemporalSmoothing)
                {
                    var curProj = GetProjectionMatrix(mainCamera);
                    var curView = mainCamera.worldToCameraMatrix;
                    
                    var prevToCurrent = curProj * curView * previousView.inverse * previousProj.inverse;
                    
                    previousProj = curProj;
                    previousView = curView;
                    
                    if (previousFrameDataAvailable)
                    {
                        var applyPrevious =
                            SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.ApplyPreviousFrame.KernelName);
                        SolidComputeShader.SetTexture(applyPrevious, ShaderVariables.SolidCompute.ApplyPreviousFrame.InputColor, rtPreviousColor, 0);
                        SolidComputeShader.SetTexture(applyPrevious, ShaderVariables.SolidCompute.ApplyPreviousFrame.InputDepth, rtPreviousDepth, 0);
                        SolidComputeShader.SetTexture(applyPrevious, ShaderVariables.SolidCompute.ApplyPreviousFrame.OutputColor, rtColor, 0);
                        SolidComputeShader.SetTexture(applyPrevious, ShaderVariables.SolidCompute.ApplyPreviousFrame.OutputDepth, rtNormalDepth, 0);
                        SolidComputeShader.SetMatrix(ShaderVariables.SolidCompute.ApplyPreviousFrame.PrevToCurrentMatrix, prevToCurrent);
                        SolidComputeShader.SetFloat("_FramePersistence", FramePersistence);
                        SolidComputeShader.Dispatch(applyPrevious, size / 8, size / 8, 1);
                    }
                    else
                    {
                        previousFrameDataAvailable = true;
                    }

                    var copyFrame = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.CopyFrame.KernelName);
                    SolidComputeShader.SetTexture(copyFrame, ShaderVariables.SolidCompute.CopyFrame.InputColor, rtColor, 0);
                    SolidComputeShader.SetTexture(copyFrame, ShaderVariables.SolidCompute.CopyFrame.InputDepth, rtNormalDepth, 0);
                    SolidComputeShader.SetTexture(copyFrame, ShaderVariables.SolidCompute.CopyFrame.OutputColor, rtPreviousColor, 0);
                    SolidComputeShader.SetTexture(copyFrame, ShaderVariables.SolidCompute.CopyFrame.OutputDepth, rtPreviousDepth, 0);
                    SolidComputeShader.Dispatch(copyFrame, size / 8, size / 8, 1);
                }
            }

            if (DebugSolidPullPush)
            {
                var pullKernel = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.PullKernel.KernelName);
                SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.PullKernel.FilterExponent, DebugSolidPullParam);

                for (var i = 1; i <= maxLevel; i++)
                {
                    SolidComputeShader.SetBool(ShaderVariables.SolidCompute.PullKernel.SkipWeightMul, i == maxLevel);
                    SolidComputeShader.SetTexture(pullKernel, ShaderVariables.SolidCompute.PullKernel.InputColor, rtColor, i - 1);
                    SolidComputeShader.SetTexture(pullKernel, ShaderVariables.SolidCompute.PullKernel.OutputColor, rtColor, i);
                    SolidComputeShader.SetTexture(pullKernel, ShaderVariables.SolidCompute.PullKernel.InputDepth, rtNormalDepth, i - 1);
                    SolidComputeShader.SetTexture(pullKernel, ShaderVariables.SolidCompute.PullKernel.OutputDepth, rtNormalDepth, i);
                    SolidComputeShader.Dispatch(pullKernel, Math.Max(1, (size >> i) / 8), Math.Max(1, (size >> i) / 8),
                        1);
                }

                var pushKernel = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.PushKernel.KernelName);

                for (var i = maxLevel; i > 0; i--)
                {
                    SolidComputeShader.SetTexture(pushKernel, ShaderVariables.SolidCompute.PushKernel.InputColor, rtColor, i);
                    SolidComputeShader.SetTexture(pushKernel, ShaderVariables.SolidCompute.PushKernel.OutputColor, rtColor, i - 1);
                    SolidComputeShader.SetTexture(pushKernel, ShaderVariables.SolidCompute.PushKernel.InputDepth, rtNormalDepth, i);
                    SolidComputeShader.SetTexture(pushKernel, ShaderVariables.SolidCompute.PushKernel.OutputDepth, rtNormalDepth, i - 1);
                    SolidComputeShader.Dispatch(pushKernel, Math.Max(1, (size >> (i - 1)) / 8),
                        Math.Max(1, (size >> (i - 1)) / 8), 1);
                }

                var calculateNormalsKernel = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.CalculateNormals.KernelName);

                for (var i = 0; i < maxLevel; ++i)
                {
                    SolidComputeShader.SetTexture(calculateNormalsKernel, ShaderVariables.SolidCompute.CalculateNormals.InputOutput, rtNormalDepth, i);
                    SolidComputeShader.Dispatch(calculateNormalsKernel, Math.Max(1, (size >> i) / 8),
                        Math.Max(1, (size >> i) / 8), 1);
                }

                var smoothNormalsKernel = DebugShowSmoothNormalsCascades
                    ? SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.SmoothNormals.DebugKernelName)
                    : SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.SmoothNormals.KernelName);
                var smoothNormalsMagic = SmoothNormalsCascadeOffset * height * 0.5f / Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);

                SolidComputeShader.SetTexture(smoothNormalsKernel, ShaderVariables.SolidCompute.SmoothNormals.Input, rtNormalDepth);
                SolidComputeShader.SetTexture(smoothNormalsKernel, ShaderVariables.SolidCompute.SmoothNormals.Output, rtDebug, 0);
                SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.SmoothNormals.CascadesOffset, smoothNormalsMagic);
                SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.SmoothNormals.CascadesSize, SmoothNormalsCascadeSize);
                if (DebugShowSmoothNormalsCascades)
                    SolidComputeShader.SetTexture(smoothNormalsKernel, ShaderVariables.SolidCompute.SmoothNormals.ColorDebug, rtColor, 0);
                SolidComputeShader.Dispatch(smoothNormalsKernel, size / 8, size / 8, 1);
            }

            DebugSolidBlitLevel = Math.Min(Math.Max(DebugSolidBlitLevel, 0), maxLevel);

            SolidBlitMaterial.SetTexture(ShaderVariables.SolidBlit.ColorTex, rtColor);
            SolidBlitMaterial.SetTexture(ShaderVariables.SolidBlit.NormalDepthTex, rtDebug);
            SolidBlitMaterial.SetFloat(ShaderVariables.SolidBlit.FarPlane, mainCamera.farClipPlane);
            SolidBlitMaterial.SetFloat(ShaderVariables.SolidBlit.UvMultiplier, screenRescaleMultiplier);
            SolidBlitMaterial.SetInt(ShaderVariables.SolidBlit.DebugLevel, DebugSolidBlitLevel);
            SolidBlitMaterial.SetMatrix(ShaderVariables.SolidBlit.ReprojectionMatrix, GetReprojectionMatrix(mainCamera));
            SolidBlitMaterial.SetMatrix(ShaderVariables.SolidBlit.InverseProjectionMatrix, GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false).inverse);
            SetSolidBlitType();

            Graphics.DrawProcedural(SolidBlitMaterial, GetWorldBounds(), MeshTopology.Triangles, 3, camera: mainCamera, layer: 1);
        }

        private void RenderAsPoints(bool constantSize, float pixelSize)
        {
            CreatePointsMaterial();

            if (constantSize && Mathf.Approximately(pixelSize, 1f))
            {
                PointsMaterial.SetBuffer(ShaderVariables.PointsRender.Buffer, Buffer);
                PointsMaterial.SetMatrix(ShaderVariables.PointsRender.ModelMatrix, transform.localToWorldMatrix);
                PointsMaterial.SetInt(ShaderVariables.PointsRender.Colorize, (int) Colorize);
                PointsMaterial.SetFloat(ShaderVariables.PointsRender.MinHeight, Bounds.min.y);
                PointsMaterial.SetFloat(ShaderVariables.PointsRender.MaxHeight, Bounds.max.y);

                Graphics.DrawProcedural(PointsMaterial, GetWorldBounds(), MeshTopology.Points, PointCount, layer: 1,
                    camera: TargetCamera);
            }
            else
            {
                CirclesMaterial.SetBuffer(ShaderVariables.PointsRender.Buffer, Buffer);
                CirclesMaterial.SetMatrix(ShaderVariables.PointsRender.ModelMatrix, transform.localToWorldMatrix);
                CirclesMaterial.SetInt(ShaderVariables.PointsRender.Colorize, (int) Colorize);
                CirclesMaterial.SetFloat(ShaderVariables.PointsRender.MinHeight, Bounds.min.y);
                CirclesMaterial.SetFloat(ShaderVariables.PointsRender.MaxHeight, Bounds.max.y);

                if (constantSize)
                {
                    CirclesMaterial.EnableKeyword(ShaderVariables.PointsRender.SizeInPixelsKeyword);
                    CirclesMaterial.SetFloat(ShaderVariables.PointsRender.Size, pixelSize);
                }
                else
                {
                    CirclesMaterial.DisableKeyword(ShaderVariables.PointsRender.SizeInPixelsKeyword);
                    CirclesMaterial.SetFloat(ShaderVariables.PointsRender.Size, AbsoluteSize);
                    CirclesMaterial.SetFloat(ShaderVariables.PointsRender.MinSize, MinPixelSize);
                }

                Graphics.DrawProcedural(CirclesMaterial, GetWorldBounds(), MeshTopology.Points, PointCount, layer: 1, camera: TargetCamera);
            }
        }

        private Bounds GetWorldBounds()
        {
            var center = transform.TransformPoint(Bounds.center);

            var extents = Bounds.extents;
            var x = transform.TransformVector(extents.x, 0, 0);
            var y = transform.TransformVector(0, extents.y, 0);
            var z = transform.TransformVector(0, 0, extents.z);

            extents.x = Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x);
            extents.y = Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y);
            extents.z = Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z);

            return new Bounds {center = center, extents = extents};
        }

        private float GetFovReprojectionMultiplier(Camera usedCamera)
        {
            var originalFov = usedCamera.fieldOfView;
            var extendedFov = originalFov * ReprojectionRatio;

            return Mathf.Tan(0.5f * extendedFov * Mathf.Deg2Rad) / Mathf.Tan(0.5f * originalFov * Mathf.Deg2Rad);
        }

        private Matrix4x4 GetReprojectionMatrix(Camera usedCamera)
        {
            var m = Matrix4x4.identity;

            if (!SolidFovReprojection)
                return m;

            m[0, 0] = m[1, 1] = GetFovReprojectionMultiplier(usedCamera);

            return m;
        }

        private Matrix4x4 GetProjectionMatrix(Camera usedCamera)
        {
            var proj = usedCamera.projectionMatrix;

            if (SolidFovReprojection)
            {
                var mul = 1 / GetFovReprojectionMultiplier(usedCamera);

                proj[0, 0] *= mul;
                proj[1, 1] *= mul;
            }

            return GL.GetGPUProjectionMatrix(proj, false);
        }

        private void SetSolidBlitType()
        {
            SolidBlitMaterial.SetKeyword(ShaderVariables.SolidBlit.ColorOnlyKeyword, SolidRender == SolidRenderType.Color);
            SolidBlitMaterial.SetKeyword(ShaderVariables.SolidBlit.NormalsOnlyKeyword, SolidRender == SolidRenderType.Normals);
            SolidBlitMaterial.SetKeyword(ShaderVariables.SolidBlit.DepthOnlyKeyword, SolidRender == SolidRenderType.Depth);
        }
    }
}