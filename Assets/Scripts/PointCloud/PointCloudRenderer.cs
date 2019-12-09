/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using UnityEngine;
using Simulator.Utilities;

namespace Simulator.PointCloud
{
    [ExecuteInEditMode]
    public class PointCloudRenderer : MonoBehaviour
    {
        public enum RenderType
        {
            Points,
            Solid,
        }

        public enum ColorizeType
        {
            Colors = 0,
            Intensity = 1,
            RainbowIntensity = 2,
            RainbowHeight = 3,
        }

        public PointCloudData Data;

        public ColorizeType Colorize = ColorizeType.RainbowIntensity;

        public RenderType Render = RenderType.Points;

        public bool ConstantSize = false;

        [Range(1, 32)]
        public float PixelSize = 6.0f;

        [Range(0.001f, 0.3f)]
        public float AbsoluteSize = 0.05f;

        [Range(1, 8)]
        public float MinPixelSize = 3.0f;

        public int DebugSolidBlitLevel = 0;
        public bool SolidRemoveHidden = true;
        public bool DebugSolidPullPush = true;
        public int DebugSolidFixedLevel = 0;

        [Range(0f, 100.0f)]
        public float DebugSolidAlwaysFillDistance = 10.0f;

        [Range(0.01f, 100.0f)]
        public float DebugSolidMetric = 7.2f;
        
        [Range(0.01f, 5.0f)]
        public float DebugSolidMetric2 = 1.3f;
        
        [Range(0.01f, 20f)]
        public float DebugSolidPullParam = 4f;

        protected ComputeBuffer Buffer;

        Material PointsMaterial;
        Material CirclesMaterial;

        ComputeShader SolidComputeShader;
        Material SolidRenderMaterial;
        Material SolitBlitMaterial;

        RenderTexture rt;
        RenderTexture rtPos;
        RenderTexture rtMask;
        RenderTexture rtPosition;
        RenderTexture rtColor;
        RenderTexture rtDepth;
        RenderTexture rtDepthBuffer;
        RenderTexture rtNormals;

        protected virtual Bounds Bounds => Data == null ? default : Data.Bounds;

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

        void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (PointsMaterial != null) Destroy(PointsMaterial);
                if (CirclesMaterial != null) Destroy(CirclesMaterial);
                if (SolidRenderMaterial != null) Destroy(SolidRenderMaterial);
                if (SolitBlitMaterial != null) Destroy(SolitBlitMaterial);
                if (SolidComputeShader != null) Destroy(SolidComputeShader);
            }
            else
            {
                if (PointsMaterial != null) DestroyImmediate(PointsMaterial);
                if (CirclesMaterial != null) DestroyImmediate(CirclesMaterial);
                if (SolidRenderMaterial != null) DestroyImmediate(SolidRenderMaterial);
                if (SolitBlitMaterial != null) DestroyImmediate(SolitBlitMaterial);
                if (SolidComputeShader != null) DestroyImmediate(SolidComputeShader);
            }

            if (rt != null) rt.Release();
            if (rtPos != null) rtPos.Release();
            if (rtMask != null) rtMask.Release();
            if (rtPosition != null) rtPosition.Release();
            if (rtColor != null) rtColor.Release();
            if (rtDepthBuffer != null) rtDepthBuffer.Release();
            if (rtDepth != null) rtDepth.Release();
            if (rtNormals != null) rtNormals.Release();
        }

        void CreatePointsMaterial()
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

        void CreateSolidMaterial()
        {
            if (SolidComputeShader == null)
            {
                SolidComputeShader = Instantiate(RuntimeSettings.Instance.PointCloudSolid);

                SolidRenderMaterial = new Material(RuntimeSettings.Instance.PointCloudSolidRender);
                SolidRenderMaterial.hideFlags = HideFlags.DontSave;
                SolidRenderMaterial.SetBuffer("_Buffer", Buffer);

                SolitBlitMaterial = new Material(RuntimeSettings.Instance.PointCloudSolidBlit);
                SolitBlitMaterial.hideFlags = HideFlags.DontSave;
            }
        }

        void LateUpdate()
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

        void RenderAsSolid()
        {
            CreateSolidMaterial();

            // TODO: camera
            var camera = Camera.main;

            int width = camera.pixelWidth;
            int height = camera.pixelHeight;
            int size = Math.Max(Mathf.NextPowerOfTwo(width), Mathf.NextPowerOfTwo(height));

            if (rt == null || rt.width != camera.pixelWidth || rt.height != camera.pixelHeight)
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
                
                if (rtDepthBuffer != null)
                    rtDepthBuffer.Release();
                rtDepthBuffer = new RenderTexture(size, size, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                rtDepthBuffer.enableRandomWrite = true;
                rtDepthBuffer.autoGenerateMips = false;
                rtDepthBuffer.useMipMap = true;
                rtDepthBuffer.Create();

                if (rtDepth != null)
                    rtDepth.Release();
                rtDepth = new RenderTexture(size, size, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                rtDepth.enableRandomWrite = true;
                rtDepth.autoGenerateMips = false;
                rtDepth.useMipMap = true;
                rtDepth.Create();
                
                if (rtNormals != null)
                    rtNormals.Release();
                rtNormals = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                rtNormals.enableRandomWrite = true;
                rtNormals.autoGenerateMips = false;
                rtNormals.useMipMap = true;
                rtNormals.Create();
            }

            int maxLevel = 0;
            while ((size >> maxLevel) > 8)
            {
                maxLevel++;
            }

            Graphics.SetRenderTarget(new[] { rt.colorBuffer, rtPos.colorBuffer }, rt.depthBuffer);
            GL.Clear(true, true, Color.clear);

            SolidRenderMaterial.SetInt("_Colorize", (int)Colorize);
            SolidRenderMaterial.SetFloat("_MinHeight", Bounds.min.y);
            SolidRenderMaterial.SetFloat("_MaxHeight", Bounds.max.y);
            SolidRenderMaterial.SetMatrix("_Transform", camera.worldToCameraMatrix * transform.localToWorldMatrix);
            SolidRenderMaterial.SetMatrix("_ViewToClip", GL.GetGPUProjectionMatrix(camera.projectionMatrix, false));
            SolidRenderMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, Buffer.count);

            SolidComputeShader.SetFloat("_FarPlane", camera.farClipPlane);
            
            var setupClear = SolidComputeShader.FindKernel("SetupClear");
            SolidComputeShader.SetTexture(setupClear, "_SetupClearPosition", rtPosition, 0);
            SolidComputeShader.SetTexture(setupClear, "_SetupClearColor", rtColor, 0);
            SolidComputeShader.SetTexture(setupClear, "_SetupClearDepthBuffer", rtDepthBuffer, 0);
            SolidComputeShader.SetTexture(setupClear, "_SetupClearDepthRaw", rtDepth, 0);
            SolidComputeShader.Dispatch(setupClear, size / 8, size / 8, 1);

            var setupCopy = SolidComputeShader.FindKernel("SetupCopy");
            SolidComputeShader.SetTexture(setupCopy, "_SetupCopyInput", rt, 0);
            SolidComputeShader.SetTexture(setupCopy, "_SetupCopyInputPos", rtPos, 0);
            SolidComputeShader.SetTexture(setupCopy, "_SetupCopyPosition", rtPosition, 0);
            SolidComputeShader.SetTexture(setupCopy, "_SetupCopyColor", rtColor, 0);
            SolidComputeShader.SetTexture(setupCopy, "_SetupCopyDepthBuffer", rtDepthBuffer, 0);
            SolidComputeShader.SetTexture(setupCopy, "_SetupCopyDepthRaw", rtDepth, 0);
            SolidComputeShader.SetMatrix("_SetupCopyProj", GL.GetGPUProjectionMatrix(camera.projectionMatrix, false));
            SolidComputeShader.SetMatrix("_SetupCopyInverseProj", GL.GetGPUProjectionMatrix(camera.projectionMatrix, false).inverse);
            SolidComputeShader.Dispatch(setupCopy, size / 8, size / 8, 1);
            
            SolidComputeShader.SetInt("_RemoveHiddenLevelCount", maxLevel);    
            
            if (SolidRemoveHidden)
            {
                var posMax = new[] { width-1, height-1 };
                var downsample = SolidComputeShader.FindKernel("Downsample");
                for (int i = 1; i <= maxLevel + 3; i++)
                {
                    SolidComputeShader.SetInts("_DownsamplePosMax", posMax);
                    posMax[0] = (posMax[0] + 1) / 2 - 1;
                    posMax[1] = (posMax[1] + 1) / 2 - 1;
                    SolidComputeShader.SetTexture(downsample, "_DownsampleInput", rtPosition, i - 1);
                    SolidComputeShader.SetTexture(downsample, "_DownsampleOutput", rtPosition, i);
                    SolidComputeShader.SetTexture(downsample, "_DownsampleDepthRawInput", rtDepth, i - 1);
                    SolidComputeShader.SetTexture(downsample, "_DownsampleDepthRawOutput", rtDepth, i);
                    SolidComputeShader.Dispatch(downsample, Math.Max(1, (size >> i) / 8), Math.Max(1, (size >> i) / 8), 1);
                }

                float metric = DebugSolidMetric / 100f;
                float removeHiddenMagic = 10 * metric * camera.pixelHeight * 0.5f / Mathf.Tan(0.5f * camera.fieldOfView * Mathf.Deg2Rad);

                DebugSolidFixedLevel = Math.Min(Math.Max(DebugSolidFixedLevel, 0), maxLevel);

                var removeHidden = SolidComputeShader.FindKernel("RemoveHidden");
                SolidComputeShader.SetTexture(removeHidden, "_RemoveHiddenMask", rtMask);
                SolidComputeShader.SetTexture(removeHidden, "_RemoveHiddenPosition", rtPosition);
                SolidComputeShader.SetTexture(removeHidden, "_RemoveHiddenColor", rtColor, 0);
                SolidComputeShader.SetTexture(removeHidden, "_RemoveHiddenDepthBuffer", rtDepthBuffer, 0);
                SolidComputeShader.SetTexture(removeHidden, "_RemoveHiddenDepthRaw", rtDepth);
                SolidComputeShader.SetFloat("_RemoveHiddenMagic", removeHiddenMagic);
                SolidComputeShader.SetFloat("_RemoveHiddenMagic2", DebugSolidMetric2);
                SolidComputeShader.SetInt("_RemoveHiddenLevel", DebugSolidFixedLevel);
                SolidComputeShader.Dispatch(removeHidden, size / 8, size / 8, 1);
            }

            if (DebugSolidPullPush)
            {
                var pullKernel = SolidComputeShader.FindKernel("PullKernel");
                SolidComputeShader.SetFloat("_PullFilterParam", DebugSolidPullParam);

                for (int i = 1; i <= maxLevel; i++)
                {
                    SolidComputeShader.SetBool("_PullSkipWeightMul", i == maxLevel);
                    SolidComputeShader.SetTexture(pullKernel, "_PullColorInput", rtColor, i - 1);
                    SolidComputeShader.SetTexture(pullKernel, "_PullColorOutput", rtColor, i);
                    SolidComputeShader.SetTexture(pullKernel, "_PullDepthBufferInput", rtDepthBuffer, i - 1);
                    SolidComputeShader.SetTexture(pullKernel, "_PullDepthBufferOutput", rtDepthBuffer, i);
                    SolidComputeShader.Dispatch(pullKernel, Math.Max(1, (size >> i) / 8), Math.Max(1, (size >> i) / 8), 1);
                }

                var pushKernel = SolidComputeShader.FindKernel("PushKernel");

                for (int i = maxLevel; i > 0; i--)
                {
                    SolidComputeShader.SetTexture(pushKernel, "_PushColorInput", rtColor, i);
                    SolidComputeShader.SetTexture(pushKernel, "_PushColorOutput", rtColor, i - 1);
                    SolidComputeShader.SetTexture(pushKernel, "_PushDepthBufferInput", rtDepthBuffer, i);
                    SolidComputeShader.SetTexture(pushKernel, "_PushDepthBufferOutput", rtDepthBuffer, i - 1);
                    SolidComputeShader.Dispatch(pushKernel, Math.Max(1, (size >> (i - 1)) / 8), Math.Max(1, (size >> (i - 1)) / 8), 1);
                }

                var calculateNormalsKernel = SolidComputeShader.FindKernel("CalculateNormalsKernel");

                for (var i = 0; i < maxLevel; ++i)
                {
                    SolidComputeShader.SetTexture(calculateNormalsKernel, "_NormalsDepthInput", rtDepthBuffer, i);
                    SolidComputeShader.SetTexture(calculateNormalsKernel, "_NormalsOutput", rtNormals, i);
                    SolidComputeShader.Dispatch(calculateNormalsKernel, Math.Max(1, (size >> i) / 8), Math.Max(1, (size >> i) / 8), 1);
                }
            }

            DebugSolidBlitLevel = Math.Min(Math.Max(DebugSolidBlitLevel, 0), maxLevel);
            
            SolitBlitMaterial.SetTexture("_ColorTex", rtColor);
            SolitBlitMaterial.SetTexture("_DepthTex", rtDepthBuffer);
            SolitBlitMaterial.SetTexture("_NormalTex", rtNormals);
            SolitBlitMaterial.SetTexture("_MaskTex", rtMask);
            SolitBlitMaterial.SetFloat("_FarPlane", camera.farClipPlane);
//            SolitBlitMaterial.SetFloat("_NearPlane", camera.nearClipPlane);
            SolitBlitMaterial.SetInt("_DebugLevel", DebugSolidBlitLevel);

            Graphics.DrawProcedural(SolitBlitMaterial, GetWorldBounds(), MeshTopology.Triangles, 3, camera: camera, layer: 1);
        }

        void RenderAsPoints(bool constantSize, float pixelSize)
        {
            CreatePointsMaterial();

            if (constantSize && pixelSize == 1.0f)
            {
                PointsMaterial.SetBuffer("_Buffer", Buffer);
                PointsMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
                PointsMaterial.SetInt("_Colorize", (int)Colorize);
                PointsMaterial.SetFloat("_MinHeight", Bounds.min.y);
                PointsMaterial.SetFloat("_MaxHeight", Bounds.max.y);

                Graphics.DrawProcedural(PointsMaterial, GetWorldBounds(), MeshTopology.Points, Buffer.count, layer: 1,
                    camera: TargetCamera);
            }
            else
            {
                CirclesMaterial.SetBuffer("_Buffer", Buffer);
                CirclesMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
                CirclesMaterial.SetInt("_Colorize", (int)Colorize);
                CirclesMaterial.SetFloat("_MinHeight", Bounds.min.y);
                CirclesMaterial.SetFloat("_MaxHeight", Bounds.max.y);

                if (constantSize)
                {
                    CirclesMaterial.EnableKeyword("_SIZE_IN_PIXELS");
                    CirclesMaterial.SetFloat("_Size", pixelSize);
                }
                else
                {
                    CirclesMaterial.DisableKeyword("_SIZE_IN_PIXELS");
                    CirclesMaterial.SetFloat("_Size", AbsoluteSize);
                    CirclesMaterial.SetFloat("_MinSize", MinPixelSize);
                }

                Graphics.DrawProcedural(CirclesMaterial, GetWorldBounds(), MeshTopology.Points, Buffer.count, layer: 1,
                    camera: TargetCamera);
            }
        }

        Bounds GetWorldBounds()
        {
            var center = transform.TransformPoint(Bounds.center);

            var extents = Bounds.extents;
            var x = transform.TransformVector(extents.x, 0, 0);
            var y = transform.TransformVector(0, extents.y, 0);
            var z = transform.TransformVector(0, 0, extents.z);

            extents.x = Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x);
            extents.y = Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y);
            extents.z = Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z);

            return new Bounds { center = center, extents = extents };
        }
    }
}
