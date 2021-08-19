/**
 * Copyright (c) 2019-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Analysis;
    using Bridge;
    using Bridge.Data;
    using Plugins;
    using Postprocessing;
    using UI;
    using UnityEngine;
    using UnityEngine.Experimental.Rendering;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;
    using Utilities;
    using LensDistortion = Utilities.LensDistortion;

    [RequireComponent(typeof(Camera))]
    public abstract class CameraSensorBase: SensorBase
    {
        private static readonly int ScreenSizeProperty = Shader.PropertyToID("_ScreenSize");

        [SensorParameter]
        [Range(1, 1920)]
        public int Width = 1920;

        [SensorParameter]
        [Range(1, 1080)]
        public int Height = 1080;

        [SensorParameter]
        [Range(1, 100)]
        public int Frequency = 15;

        [SensorParameter]
        [Range(0, 100)]
        public int JpegQuality = 75;

        [SensorParameter]
        [Range(1.0f, 90.0f)]
        public float FieldOfView = 60.0f;

        [SensorParameter]
        [Range(0.01f, 1000.0f)]
        public float MinDistance = 0.1f;

        [SensorParameter]
        [Range(0.01f, 2000.0f)]
        public float MaxDistance = 2000.0f;

        [SensorParameter]
        public bool Distorted = false;

        [SensorParameter]
        public List<float> DistortionParameters;

        [SensorParameter]
        public bool Fisheye = false;

        [SensorParameter]
        public float Xi = 0.0f;

        [SensorParameter]
        public string CameraInfoTopic;

        public List<PostProcessData> Postprocessing;

        public List<PostProcessData> LatePostprocessing;

        BridgeInstance Bridge;
        Publisher<ImageData> Publish;
        Publisher<CameraInfoData> CameraInfoPublish;
        uint Sequence;

        CameraInfoData CameraInfoData;

        const int MaxJpegSize = 4 * 1024 * 1024; // 4MB

        private float NextCaptureTime;
        private float PreviousCaptureTime = -1f;
        protected Camera sensorCamera;
        protected HDAdditionalCameraData hdAdditionalCameraData;

        protected Camera SensorCamera
        {
            get
            {
                if (sensorCamera == null)
                    sensorCamera = GetComponent<Camera>();

                return sensorCamera;
            }
        }

        protected HDAdditionalCameraData HDAdditionalCameraData
        {
            get
            {
                if (hdAdditionalCameraData == null)
                    hdAdditionalCameraData = GetComponent<HDAdditionalCameraData>();

                return hdAdditionalCameraData;
            }
        }

        protected SensorRenderTarget FinalRenderTarget => Distorted ? DistortedHandle : renderTarget;

        protected int ByteBufferSize => Width * Height * 4;

        protected RenderTextureReadWrite CameraTargetTextureReadWriteType = RenderTextureReadWrite.sRGB;

        public override SensorDistributionType DistributionType => SensorDistributionType.ClientOnly;
        public override float PerformanceLoad { get; } = 1.0f;

        protected SensorRenderTarget renderTarget;

        private LensDistortion LensDistortion;
        private SensorRenderTarget DistortedHandle;
        float FrustumWidth, FrustumHeight;

        [SensorParameter]
        public int CubemapSize = 1024;

        protected readonly int faceMask = 1 << (int) CubemapFace.PositiveX | 1 << (int) CubemapFace.NegativeX |
                                          1 << (int) CubemapFace.PositiveY | 1 << (int) CubemapFace.NegativeY |
                                          1 << (int) CubemapFace.PositiveZ;

        private ConcurrentBag<byte[]> JpegOutput = new ConcurrentBag<byte[]>();
        private Queue<Task> Tasks = new Queue<Task>();

        private GpuReadbackPool<GpuReadbackData<byte>, byte> ReadbackPool;
        private int CurrentByteBufferSize;

        #region FPSCalculation
        [SensorParameter]
        [AnalysisMeasurement(MeasurementType.Fps)]
        public float TargetFPS = 0f;
        [SensorParameter]
        public float TargetFPSTime = 5f;
        private float LowFPSCalculatedTime = 0f;
        private int TotalFrames;
        private float AveDelta;

        [AnalysisMeasurement(MeasurementType.Fps)]
        public float AveFPS = 0f;

        [AnalysisMeasurement(MeasurementType.Fps)]
        public float LowestFPS = float.MaxValue;
        private bool LowFPS = false;
        #endregion

        protected override void Initialize()
        {
            SensorCamera.enabled = false;
            HDAdditionalCameraData.hasPersistentHistory = true;
            ReadbackPool = new GpuReadbackPool<GpuReadbackData<byte>, byte>();
            ReadbackPool.Initialize(ByteBufferSize, OnReadbackComplete);
            CurrentByteBufferSize = ByteBufferSize;
        }

        protected override void Deinitialize()
        {
            renderTarget?.Release();
            DistortedHandle?.Release();

            Task.WaitAll(Tasks.ToArray());

            ReadbackPool?.Dispose();
        }

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            Bridge = bridge;
            Publish = bridge.AddPublisher<ImageData>(Topic);
            if (!string.IsNullOrEmpty(CameraInfoTopic))
            {
                CameraInfoPublish = bridge.AddPublisher<CameraInfoData>(CameraInfoTopic);
            }
        }

        protected virtual void Update()
        {
            SensorCamera.fieldOfView = FieldOfView;
            SensorCamera.nearClipPlane = MinDistance;
            SensorCamera.farClipPlane = MaxDistance;

            if (Distorted)
            {
                CheckDistortion();
            }

            while (Tasks.Count > 0 && Tasks.Peek().IsCompleted)
            {
                Tasks.Dequeue();
            }

            CheckTexture();
            CheckCapture();

            if (CameraInfoPublish != null)
            {
                InitCameraInfoData();
            }

            ReadbackPool.Process();
        }

        void CheckDistortion()
        {
            if (DistortionParameters.Count == 0)
            {
                DistortionParameters = new List<float>(new float[4]);
            }

            else if (DistortionParameters.Count != 4)
            {
                throw new Exception("Length of DistortionParameters is not 4.");
            }

            if (Fisheye && !Distorted)
            {
                throw new Exception("Distorted must be true for fisheye lens.");
            }

            if (LensDistortion == null)
            {
                LensDistortion = new LensDistortion();
                LensDistortion.InitDistortion(DistortionParameters, FieldOfView, Xi, Width, Height);
            }
            else if (!LensDistortion.IsValid(DistortionParameters, FieldOfView, Xi, Width, Height))
            {
                LensDistortion.InitDistortion(DistortionParameters, FieldOfView, Xi, Width, Height);
            }
        }

        void CheckTexture()
        {
            var targetWidth = -1;
            var targetHeight = -1;

            if (Distorted)
            {
                // Distorted + fisheye - render to cubemap, then distort to sensor-sized texture
                if (Fisheye)
                {
                    if (renderTarget != null && (!renderTarget.IsCube || !renderTarget.IsValid(CubemapSize, CubemapSize)))
                    {
                        renderTarget.Release();
                        renderTarget = null;
                    }
                    if (renderTarget == null)
                    {
                        renderTarget = SensorRenderTarget.CreateCube(CubemapSize, CubemapSize, faceMask);
                        SensorCamera.targetTexture = null;
                    }
                }
                // Distorted - render to larger texture, then distort to sensor-sized one
                else
                {
                    targetWidth = LensDistortion.ActualWidth;
                    targetHeight = LensDistortion.ActualHeight;
                }
                
                if (DistortedHandle != null && !DistortedHandle.IsValid(Width, Height))
                {
                    DistortedHandle.Release();
                    DistortedHandle = null;
                }

                if (DistortedHandle == null)
                {
                    DistortedHandle = SensorRenderTarget.Create2D(Width, Height, GraphicsFormat.R8G8B8A8_SRGB, true);
                }
            }
            else
            {
                // Undistorted - use only sensor-sized texture
                if (DistortedHandle != null)
                {
                    DistortedHandle.Release();
                    DistortedHandle = null;
                }

                targetWidth = Width;
                targetHeight = Height;
            }

            if (targetWidth > 0)
            {
                if (renderTarget != null && !renderTarget.IsValid(targetWidth, targetHeight))
                {
                    renderTarget.Release();
                    renderTarget = null;
                }
                if (renderTarget == null)
                {
                    renderTarget = SensorRenderTarget.Create2D(targetWidth, targetHeight, GraphicsFormat.R8G8B8A8_SRGB, !Distorted);
                    SensorCamera.targetTexture = renderTarget;
                }
            }

            if (CurrentByteBufferSize != ByteBufferSize)
            {
               ReadbackPool.Resize(ByteBufferSize);
               CurrentByteBufferSize = ByteBufferSize;
            }
        }

        protected void RenderCamera()
        {
            var cmd = CommandBufferPool.Get();
            var hd = HDCamera.GetOrCreate(SensorCamera);

            if (renderTarget.IsCube && !HDAdditionalCameraData.hasCustomRender)
            {
                // HDRP renders cubemap as multiple separate images, each with different exposure.
                // Locking exposure will force it to use the same value for all faces, removing inconsistencies.
                hd.LockExposure();
                SensorCamera.stereoSeparation = 0f;
                SensorCamera.RenderToCubemap(renderTarget, faceMask, Camera.MonoOrStereoscopicEye.Left);
                hd.UnlockExposure();
            }
            else
            {
                SensorCamera.Render();
            }

            if (Distorted)
            {
                if (Fisheye)
                {
                    LensDistortion.UnifiedProjectionDistort(cmd, renderTarget, DistortedHandle);
                }
                else
                {
                    LensDistortion.PlumbBobDistort(cmd, renderTarget, DistortedHandle);
                }

                cmd.SetGlobalVector(ScreenSizeProperty, new Vector4(Width, Height, 1.0f / Width, 1.0f / Height));
                var ctx = new PostProcessPassContext(cmd, hd, DistortedHandle);
                SimulatorManager.Instance.Sensors.PostProcessSystem.RenderLateForSensor(ctx, this);
            }

            FinalRenderTarget.BlitTo2D(cmd, hd);
            HDRPUtilities.ExecuteAndClearCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void CheckCapture()
        {
            if (Time.time >= NextCaptureTime)
            {
                CalculateFPS();
                RenderCamera();
                ReadbackPool.StartReadback(FinalRenderTarget.UiTexture, 0, TextureFormat.RGBA32);

                TotalFrames++;
                PreviousCaptureTime = Time.time;

                if (NextCaptureTime < Time.time - Time.deltaTime)
                {
                    NextCaptureTime = Time.time + 1.0f / Frequency;
                }
                else
                {
                    NextCaptureTime += 1.0f / Frequency;
                }
            }
        }

        private void OnReadbackComplete(GpuReadbackData<byte> data)
        {
            if (Bridge is {Status: Status.Connected})
            {
                var imageData = new ImageData()
                {
                    Name = Name,
                    Frame = Frame,
                    Width = Width,
                    Height = Height,
                    Sequence = Sequence,
                };

                if (!JpegOutput.TryTake(out imageData.Bytes))
                    imageData.Bytes = new byte[MaxJpegSize];

                Tasks.Enqueue(Task.Run(() =>
                {
                    imageData.Length = JpegEncoder.Encode(data.gpuData, Width, Height, 4, JpegQuality, imageData.Bytes);
                    if (imageData.Length > 0)
                    {
                        var time = data.captureTime;
                        imageData.Time = time;
                        Publish(imageData);

                        if (CameraInfoData != null)
                        {
                            CameraInfoData.Name = Name;
                            CameraInfoData.Frame = Frame;
                            CameraInfoData.Time = time;
                            CameraInfoData.Sequence = Sequence;

                            CameraInfoPublish?.Invoke(CameraInfoData);
                        }
                    }
                    else
                        Debug.Log("Compressed image is empty, length = 0");

                    JpegOutput.Add(imageData.Bytes);
                }));

                Sequence++;
            }
        }

        public virtual bool Save(string path, int quality, int compression)
        {
            CheckTexture();
            RenderCamera();
            var readback = AsyncGPUReadback.Request(FinalRenderTarget.UiTexture, 0, TextureFormat.RGBA32);
            readback.WaitForCompletion();

            if (readback.hasError)
            {
                Debug.Log("Failed to read GPU texture");
                return false;
            }

            Debug.Assert(readback.done);
            var data = readback.GetData<byte>();

            var bytes = new byte[16 * 1024 * 1024];
            int length;

            var ext = System.IO.Path.GetExtension(path).ToLower();

            if (ext == ".png")
            {
                length = PngEncoder.Encode(data, Width, Height, 4, compression, bytes);
            }
            else if (ext == ".jpeg" || ext == ".jpg")
            {
                length = JpegEncoder.Encode(data, Width, Height, 4, quality, bytes);
            }
            else
            {
                return false;
            }

            if (length > 0)
            {
                try
                {
                    using (var file = System.IO.File.Create(path))
                    {
                        file.Write(bytes, 0, length);
                    }
                    return true;
                }
                catch
                {
                }
            }

            return false;
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);
            visualizer.UpdateRenderTexture(FinalRenderTarget.UiTexture, SensorCamera.aspect);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }

        private void CalculateFPS()
        {
            if (LowFPS)
                return;

            if (PreviousCaptureTime < 0f)
                return;

            var delta = (Time.time - PreviousCaptureTime);
            var fps = 1.0f / delta;
            LowestFPS = Mathf.Min(fps, LowestFPS);
            AveDelta = (AveDelta * (TotalFrames - 1) + delta) / (TotalFrames);
            AveFPS = 1f / AveDelta;
            if (fps < TargetFPS)
            {
                LowFPSCalculatedTime += delta;
                if (LowFPSCalculatedTime >= TargetFPSTime)
                {
                    LowFPSEvent(GetComponentInParent<IAgentController>().GTID, delta, fps);
                    LowFPSCalculatedTime = 0f;
                    LowFPS = true;
                }
            }
            else
            {
                LowFPSCalculatedTime = 0f;
            }
        }

        private void LowFPSEvent(uint id, float ms, float fps)
        {
            Hashtable data = new Hashtable
            {
                { "Id", id },
                { "Type", "LowFPS" },
                { "Time", SimulatorManager.Instance.GetSessionElapsedTimeSpan().ToString() },
                { "MS", ms },
                { "FPS", fps },
                { "Average FPS", AveFPS },
                { "Status", AnalysisManager.AnalysisStatusType.Failed },
            };
            SimulatorManager.Instance.AnalysisManager.AddEvent(data);
        }

        private void InitCameraInfoData()
        {
            if (CameraInfoData != null)
            {
                return;
            }

            var vFOV = SensorCamera.fieldOfView * Mathf.Deg2Rad;
            var hFOV = 2 * Mathf.Atan(Mathf.Tan(SensorCamera.fieldOfView * Mathf.Deg2Rad / 2) * SensorCamera.aspect);

            double fx = (double)(SensorCamera.pixelWidth / (2.0f * Mathf.Tan(0.5f * hFOV)));
            double fy = (double)(SensorCamera.pixelHeight / (2.0f * Mathf.Tan(0.5f * vFOV)));
            double cx = SensorCamera.pixelWidth / 2.0f;
            double cy = SensorCamera.pixelHeight / 2.0f;

            CameraInfoData = new CameraInfoData();
            CameraInfoData.Width = SensorCamera.pixelWidth;
            CameraInfoData.Height = SensorCamera.pixelHeight;
            CameraInfoData.FocalLengthX = fx;
            CameraInfoData.FocalLengthY = fx;
            CameraInfoData.PrincipalPointX = cx;
            CameraInfoData.PrincipalPointY = cy;

            if (Distorted)
            {
                CameraInfoData.DistortionParameters = DistortionParameters.ToArray();
            }
            else
            {
                CameraInfoData.DistortionParameters = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f };
            }
        }
    }
}
