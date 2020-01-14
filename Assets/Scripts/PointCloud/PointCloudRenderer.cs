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
    using UnityEngine.Rendering;
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
                public static readonly int MVMatrix = Shader.PropertyToID("_Transform");
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
                }

                public static class SetupCopy
                {
                    public const string KernelName = "SetupCopy";
                    public static readonly int FarPlane = Shader.PropertyToID("_FarPlane");
                    public static readonly int InputColor = Shader.PropertyToID("_SetupCopyInput");
                    public static readonly int InputPosition = Shader.PropertyToID("_SetupCopyInputPos");
                    public static readonly int OutputColor = Shader.PropertyToID("_SetupCopyColor");
                    public static readonly int OutputPosition = Shader.PropertyToID("_SetupCopyPosition");
                    public static readonly int InverseProjectionMatrix = Shader.PropertyToID("_SetupCopyInverseProj");
                }

                public static class Downsample
                {
                    public const string KernelName = "Downsample";
                    public static readonly int PosMax = Shader.PropertyToID("_DownsamplePosMax");
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
                    public static readonly int ProjectionMatrix = Shader.PropertyToID("_ProjMatrix");
                    public static readonly int FramePersistence = Shader.PropertyToID("_FramePersistence");
                }

                public static class CopyFrame
                {
                    public const string KernelName = "CopyFrame";
                    public static readonly int InputColor = Shader.PropertyToID("_CopyFrameInputColor");
                    public static readonly int OutputColor = Shader.PropertyToID("_CopyFrameOutputColor");
                    public static readonly int InputPos = Shader.PropertyToID("_CopyFrameInputPos");
                    public static readonly int OutputPos = Shader.PropertyToID("_CopyFrameOutputPos");
                }

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
                    public static readonly int InputLevel = Shader.PropertyToID("_CalcNormalsInputLevel");
                    public static readonly int InputOutput = Shader.PropertyToID("_NormalsInOut");
                    public static readonly int Input = Shader.PropertyToID("_NormalsIn");
                    public static readonly int Output = Shader.PropertyToID("_NormalsOut");
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

        private static bool UsingVulkan => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan;

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

        [Range(1, 120)]
        public int InterpolatedFrames = 60;

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
        private bool previousFrameDataAvailable;

        protected ComputeBuffer Buffer;

        private Material PointsMaterial;
        private Material CirclesMaterial;

        private ComputeShader SolidComputeShader;
        private Material SolidRenderMaterial;
        private Material SolidBlitMaterial;

        private RenderTexture rt;
        private RenderTexture rt1;
        private RenderTexture rt2;
        private RenderTexture rtPos;
        private RenderTexture rtColor;
        private RenderTexture rtPreviousColor;
        private RenderTexture rtPreviousPos;

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
            if (rt1 != null) rt1.Release();
            if (rtColor != null) rtColor.Release();
            if (rt2 != null) rt2.Release();
            if (rtPreviousColor != null) rtPreviousColor.Release();
            if (rtPreviousPos != null) rtPreviousPos.Release();
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

        private bool firstFrameDone;

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
                    RenderAsSolid();
                }
                else
                {
                    RenderAsPoints(true, 1.0f);
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

                if (rt1 != null)
                    rt1.Release();
                rt1 = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                rt1.enableRandomWrite = true;
                rt1.autoGenerateMips = false;
                rt1.useMipMap = true;
                rt1.Create();

                if (rtPos != null)
                    rtPos.Release();
                rtPos = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                rtPos.enableRandomWrite = true;
                rtPos.autoGenerateMips = false;
                rtPos.useMipMap = false;
                rtPos.Create();

                if (rtColor != null)
                    rtColor.Release();
                rtColor = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                rtColor.enableRandomWrite = true;
                rtColor.autoGenerateMips = false;
                rtColor.useMipMap = true;
                rtColor.Create();

                if (rt2 != null)
                    rt2.Release();
                rt2 = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                rt2.enableRandomWrite = true;
                rt2.autoGenerateMips = false;
                rt2.useMipMap = true;
                rt2.Create();
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

                if (rtPreviousPos != null)
                    rtPreviousPos.Release();
                rtPreviousPos = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                rtPreviousPos.enableRandomWrite = true;
                rtPreviousPos.autoGenerateMips = false;
                rtPreviousPos.useMipMap = false;
                rtPreviousPos.Create();
            }

            var maxLevel = 0;
            while ((size >> maxLevel) > 8)
            {
                maxLevel++;
            }

            Graphics.SetRenderTarget(new[] {rt.colorBuffer, rtPos.colorBuffer}, rt.depthBuffer);
            GL.Clear(true, true, Color.clear);
            var proj = GetProjectionMatrix(mainCamera, true);
            var invProj = proj.inverse;

            SolidRenderMaterial.SetInt(ShaderVariables.SolidRender.Colorize, (int) Colorize);
            SolidRenderMaterial.SetFloat(ShaderVariables.SolidRender.MinHeight, Bounds.min.y);
            SolidRenderMaterial.SetFloat(ShaderVariables.SolidRender.MaxHeight, Bounds.max.y);
            SolidRenderMaterial.SetMatrix(ShaderVariables.SolidRender.MVMatrix, mainCamera.worldToCameraMatrix * transform.localToWorldMatrix);
            SolidRenderMaterial.SetMatrix(ShaderVariables.SolidRender.ProjectionMatrix, GetProjectionMatrix(mainCamera));
            SolidRenderMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, PointCount);

            var setupClear = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.SetupClear.KernelName);
            SolidComputeShader.SetTexture(setupClear, ShaderVariables.SolidCompute.SetupClear.Position, rt1, 0);
            SolidComputeShader.SetTexture(setupClear, ShaderVariables.SolidCompute.SetupClear.Color, rtColor, 0);
            SolidComputeShader.Dispatch(setupClear, size / 8, size / 8, 1);

            var setupCopy = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.SetupCopy.KernelName);
            SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.SetupCopy.FarPlane, mainCamera.farClipPlane);
            SolidComputeShader.SetTexture(setupCopy, ShaderVariables.SolidCompute.SetupCopy.InputColor, rt, 0);
            SolidComputeShader.SetTexture(setupCopy, ShaderVariables.SolidCompute.SetupCopy.InputPosition, rtPos, 0);
            SolidComputeShader.SetTexture(setupCopy, ShaderVariables.SolidCompute.SetupCopy.OutputPosition, rt1, 0);
            SolidComputeShader.SetTexture(setupCopy, ShaderVariables.SolidCompute.SetupCopy.OutputColor, rtColor, 0);
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
                    SolidComputeShader.SetTexture(downsample, ShaderVariables.SolidCompute.Downsample.InputPosition, rt1, i - 1);
                    SolidComputeShader.SetTexture(downsample, ShaderVariables.SolidCompute.Downsample.OutputPosition, rt1, i);
                    SolidComputeShader.Dispatch(downsample, Math.Max(1, (size >> i) / 8), Math.Max(1, (size >> i) / 8), 1);
                }

                DebugSolidFixedLevel = Math.Min(Math.Max(DebugSolidFixedLevel, 0), maxLevel);

                var removeHidden = DebugShowRemoveHiddenCascades
                    ? SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.RemoveHidden.DebugKernelName)
                    : SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.RemoveHidden.KernelName);
                var removeHiddenMagic = RemoveHiddenCascadeOffset * height * 0.5f / Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);

                SolidComputeShader.SetInt(ShaderVariables.SolidCompute.RemoveHidden.LevelCount, maxLevel);
                SolidComputeShader.SetTexture(removeHidden, ShaderVariables.SolidCompute.RemoveHidden.Position, rt1);
                SolidComputeShader.SetTexture(removeHidden, ShaderVariables.SolidCompute.RemoveHidden.Color, rtColor, 0);
                SolidComputeShader.SetTexture(removeHidden, ShaderVariables.SolidCompute.RemoveHidden.DepthBuffer, rt2, 0);
                SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.RemoveHidden.CascadesOffset, removeHiddenMagic);
                SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.RemoveHidden.CascadesSize, RemoveHiddenCascadeSize);
                SolidComputeShader.SetInt(ShaderVariables.SolidCompute.RemoveHidden.FixedLevel, DebugSolidFixedLevel);
                SolidComputeShader.Dispatch(removeHidden, size / 8, size / 8, 1);

                if (TemporalSmoothing)
                {
                    var curProj = GetProjectionMatrix(mainCamera);
                    var curView = mainCamera.worldToCameraMatrix;

                    var prevToCurrent = curView * previousView.inverse;

                    previousView = curView;

                    if (previousFrameDataAvailable)
                    {
                        var applyPrevious = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.ApplyPreviousFrame.KernelName);
                        SolidComputeShader.SetTexture(applyPrevious, ShaderVariables.SolidCompute.ApplyPreviousFrame.SavedColor, rtPreviousColor, 0);
                        SolidComputeShader.SetTexture(applyPrevious, ShaderVariables.SolidCompute.ApplyPreviousFrame.SavedPos, rtPreviousPos, 0);
                        SolidComputeShader.SetTexture(applyPrevious, ShaderVariables.SolidCompute.ApplyPreviousFrame.CurrentColor, rtColor, 0);
                        SolidComputeShader.SetTexture(applyPrevious, ShaderVariables.SolidCompute.ApplyPreviousFrame.CurrentPos, rt2, 0);
                        if (UsingVulkan)
                        {
                            SolidComputeShader.SetTexture(applyPrevious, ShaderVariables.SolidCompute.ApplyPreviousFrame.CurrentColorIn, rtColor, 0);
                            SolidComputeShader.SetTexture(applyPrevious, ShaderVariables.SolidCompute.ApplyPreviousFrame.CurrentPosIn, rt2, 0);
                        }

                        SolidComputeShader.SetMatrix(ShaderVariables.SolidCompute.ApplyPreviousFrame.PrevToCurrentMatrix, prevToCurrent);
                        SolidComputeShader.SetMatrix(ShaderVariables.SolidCompute.ApplyPreviousFrame.ProjectionMatrix, curProj);
                        SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.ApplyPreviousFrame.FramePersistence, 1f / InterpolatedFrames);
                        SolidComputeShader.Dispatch(applyPrevious, size / 8, size / 8, 1);
                    }
                    else
                    {
                        previousFrameDataAvailable = true;
                    }

                    var copyFrame = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.CopyFrame.KernelName);
                    SolidComputeShader.SetTexture(copyFrame, ShaderVariables.SolidCompute.CopyFrame.InputColor, rtColor);
                    SolidComputeShader.SetTexture(copyFrame, ShaderVariables.SolidCompute.CopyFrame.InputPos, rt2);
                    SolidComputeShader.SetTexture(copyFrame, ShaderVariables.SolidCompute.CopyFrame.OutputColor, rtPreviousColor, 0);
                    SolidComputeShader.SetTexture(copyFrame, ShaderVariables.SolidCompute.CopyFrame.OutputPos, rtPreviousPos, 0);
                    SolidComputeShader.Dispatch(copyFrame, size / 8, size / 8, 1);
                }
                else if (previousFrameDataAvailable)
                    previousFrameDataAvailable = false;
            }

            if (DebugSolidPullPush)
            {
                var pullKernel = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.PullKernel.KernelName);
                SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.PullKernel.FilterExponent, DebugSolidPullParam);

                for (var i = 1; i <= maxLevel; i++)
                {
                    SolidComputeShader.SetBool(ShaderVariables.SolidCompute.PullKernel.SkipWeightMul, i == maxLevel);
                    SolidComputeShader.SetInt(ShaderVariables.SolidCompute.PullKernel.InputLevel, i - 1);
                    SolidComputeShader.SetTexture(pullKernel, ShaderVariables.SolidCompute.PullKernel.InputColor, rtColor, i - 1);
                    SolidComputeShader.SetTexture(pullKernel, ShaderVariables.SolidCompute.PullKernel.OutputColor, rtColor, i);
                    SolidComputeShader.SetTexture(pullKernel, ShaderVariables.SolidCompute.PullKernel.InputDepth, rt2, i - 1);
                    SolidComputeShader.SetTexture(pullKernel, ShaderVariables.SolidCompute.PullKernel.OutputDepth, rt2, i);
                    SolidComputeShader.Dispatch(pullKernel, Math.Max(1, (size >> i) / 8), Math.Max(1, (size >> i) / 8), 1);
                }

                var pushKernel = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.PushKernel.KernelName);

                for (var i = maxLevel; i > 0; i--)
                {
                    SolidComputeShader.SetInt(ShaderVariables.SolidCompute.PushKernel.InputLevel, i);
                    SolidComputeShader.SetTexture(pushKernel, ShaderVariables.SolidCompute.PushKernel.InputColor, rtColor, i);
                    SolidComputeShader.SetTexture(pushKernel, ShaderVariables.SolidCompute.PushKernel.OutputColor, rtColor, i - 1);
                    SolidComputeShader.SetTexture(pushKernel, ShaderVariables.SolidCompute.PushKernel.InputDepth, rt2, i);
                    SolidComputeShader.SetTexture(pushKernel, ShaderVariables.SolidCompute.PushKernel.OutputDepth, rt2, i - 1);
                    SolidComputeShader.Dispatch(pushKernel, Math.Max(1, (size >> (i - 1)) / 8), Math.Max(1, (size >> (i - 1)) / 8), 1);
                }

                var calculateNormalsKernel = SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.CalculateNormals.KernelName);

                for (var i = 0; i < maxLevel; ++i)
                {
                    if (UsingVulkan)
                    {
                        SolidComputeShader.SetInt(ShaderVariables.SolidCompute.CalculateNormals.InputLevel, i);
                        SolidComputeShader.SetTexture(calculateNormalsKernel, ShaderVariables.SolidCompute.CalculateNormals.Input, rt2);
                        SolidComputeShader.SetTexture(calculateNormalsKernel, ShaderVariables.SolidCompute.CalculateNormals.Output, rt2, i);
                    }
                    else
                    {
                        SolidComputeShader.SetTexture(calculateNormalsKernel, ShaderVariables.SolidCompute.CalculateNormals.InputOutput, rt2, i);
                    }

                    SolidComputeShader.Dispatch(calculateNormalsKernel, Math.Max(1, (size >> i) / 8), Math.Max(1, (size >> i) / 8), 1);
                }

                var smoothNormalsKernel = DebugShowSmoothNormalsCascades
                    ? SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.SmoothNormals.DebugKernelName)
                    : SolidComputeShader.FindKernel(ShaderVariables.SolidCompute.SmoothNormals.KernelName);
                var smoothNormalsMagic = SmoothNormalsCascadeOffset * height * 0.5f / Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);

                SolidComputeShader.SetTexture(smoothNormalsKernel, ShaderVariables.SolidCompute.SmoothNormals.Input, rt2);
                SolidComputeShader.SetTexture(smoothNormalsKernel, ShaderVariables.SolidCompute.SmoothNormals.Output, rt1, 0);
                SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.SmoothNormals.CascadesOffset, smoothNormalsMagic);
                SolidComputeShader.SetFloat(ShaderVariables.SolidCompute.SmoothNormals.CascadesSize, SmoothNormalsCascadeSize);
                if (DebugShowSmoothNormalsCascades)
                    SolidComputeShader.SetTexture(smoothNormalsKernel, ShaderVariables.SolidCompute.SmoothNormals.ColorDebug, rtColor, 0);
                SolidComputeShader.Dispatch(smoothNormalsKernel, size / 8, size / 8, 1);
            }

            DebugSolidBlitLevel = Math.Min(Math.Max(DebugSolidBlitLevel, 0), maxLevel);

            SolidBlitMaterial.SetTexture(ShaderVariables.SolidBlit.ColorTex, rtColor);
            SolidBlitMaterial.SetTexture(ShaderVariables.SolidBlit.NormalDepthTex, rt1);
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

        private Matrix4x4 GetProjectionMatrix(Camera usedCamera, bool renderIntoTexture = false)
        {
            var proj = usedCamera.projectionMatrix;

            if (SolidFovReprojection)
            {
                var mul = 1 / GetFovReprojectionMultiplier(usedCamera);

                proj[0, 0] *= mul;
                proj[1, 1] *= mul;
            }

            return GL.GetGPUProjectionMatrix(proj, renderIntoTexture);
        }

        private void SetSolidBlitType()
        {
            SolidBlitMaterial.SetKeyword(ShaderVariables.SolidBlit.ColorOnlyKeyword, SolidRender == SolidRenderType.Color);
            SolidBlitMaterial.SetKeyword(ShaderVariables.SolidBlit.NormalsOnlyKeyword, SolidRender == SolidRenderType.Normals);
            SolidBlitMaterial.SetKeyword(ShaderVariables.SolidBlit.DepthOnlyKeyword, SolidRender == SolidRenderType.Depth);
        }
    }
}