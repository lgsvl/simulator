/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Plugins;
using Simulator.Utilities;
using Simulator.Sensors.UI;

namespace Simulator.Sensors
{
    [RequireComponent(typeof(Camera))]
    public abstract class CameraSensorBase: SensorBase
    {
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
        public float MaxDistance = 1000.0f;

        [SensorParameter]
        public bool Distorted;
        [SensorParameter]
        public List<float> DistortionParameters;

        IBridge Bridge;
        IWriter<ImageData> ImageWriter;
        uint Sequence;

        const int MaxJpegSize = 4 * 1024 * 1024; // 4MB

        private float NextCaptureTime;
        protected Camera sensorCamera;

        protected Camera SensorCamera
        {
            get
            {
                if (sensorCamera == null)
                    sensorCamera = GetComponent<Camera>();

                return sensorCamera;
            }
        }
        protected RenderTextureReadWrite CameraTargetTextureReadWriteType = RenderTextureReadWrite.sRGB;

        public override SensorDistributionType DistributionType => SensorDistributionType.HighLoad;
        private int RenderTextureWidth, RenderTextureHeight;
        private int CurrentWidth, CurrentHeight;
        private float CurrentFieldOfView;
        private bool CurrentDistorted;
        private List<float> CurrentDistortionParameters;

        private LensDistortion LensDistortion;
        private RenderTexture DistortedTexture;
        float FrustumWidth, FrustumHeight;

        private struct CameraCapture
        {
            public AsyncGPUReadbackRequest Request;
            public double CaptureTime;
        }

        private Queue<CameraCapture> CaptureQueue = new Queue<CameraCapture>();
        private ConcurrentBag<byte[]> JpegOutput = new ConcurrentBag<byte[]>();

        public void Start()
        {
            SensorCamera.enabled = false;

            CurrentWidth = Width;
            CurrentHeight = Height;
            CurrentFieldOfView = FieldOfView;
            CurrentDistorted = Distorted;
            CurrentDistortionParameters = new List<float>(DistortionParameters);
        }

        public void OnDestroy()
        {
            if (SensorCamera != null && SensorCamera.targetTexture != null)
            {
                SensorCamera.targetTexture.Release();
            }
            DistortedTexture?.Release();
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            ImageWriter = bridge.AddWriter<ImageData>(Topic);
        }

        public void Update()
        {
            SensorCamera.fieldOfView = FieldOfView;
            SensorCamera.nearClipPlane = MinDistance;
            SensorCamera.farClipPlane = MaxDistance;

            if (Distorted)
            {
                CheckDistortion();
            }
            else
            {
                ResetDistortion();
            }
            CheckTexture();
            CheckCapture();
            ProcessReadbackRequests();
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

            if (LensDistortion == null || CurrentWidth != Width || CurrentHeight != Height || 
                CurrentFieldOfView != FieldOfView || CurrentDistorted != Distorted || 
                !Enumerable.SequenceEqual(DistortionParameters, CurrentDistortionParameters))
            {
                // View frustum size at the focal plane.
                FrustumHeight = 2 * Mathf.Tan(FieldOfView / 2 * Mathf.Deg2Rad);
                FrustumWidth = FrustumHeight * Width / Height;
                if (LensDistortion == null)
                {
                    LensDistortion = new LensDistortion();
                }
                LensDistortion.InitDistortion(DistortionParameters, FrustumWidth, FrustumHeight);
                LensDistortion.CalculateRenderTextureSize(Width, Height, out RenderTextureWidth, out RenderTextureHeight);
                if (RenderTextureWidth <= 0 || RenderTextureHeight <= 0)
                {
                    throw new Exception("Distortion parameters cause texture size invalid (<= 0).");
                }

                CurrentWidth = Width;
                CurrentHeight = Height;
                CurrentFieldOfView = FieldOfView;
                CurrentDistorted = Distorted;
                CurrentDistortionParameters = new List<float>(DistortionParameters);
            }
        }

        void ResetDistortion()
        {
            RenderTextureWidth = Width;
            RenderTextureHeight = Height;
        }

        void CheckTexture()
        {
            // if this is not first time
            if (SensorCamera.targetTexture != null)
            {
                if (RenderTextureWidth != SensorCamera.targetTexture.width || RenderTextureHeight != SensorCamera.targetTexture.height)
                {
                    // if camera capture size has changed
                    SensorCamera.targetTexture.Release();
                    SensorCamera.targetTexture = null;
                }
                else if (!SensorCamera.targetTexture.IsCreated())
                {
                    // if we have lost rendertexture due to Unity window resizing or otherwise
                    SensorCamera.targetTexture.Release();
                    SensorCamera.targetTexture = null;
                }
            }

            if (SensorCamera.targetTexture == null)
            {
                SensorCamera.targetTexture = new RenderTexture(RenderTextureWidth, RenderTextureHeight, 24, 
                    RenderTextureFormat.ARGB32, CameraTargetTextureReadWriteType)
                {
                    dimension = TextureDimension.Tex2D,
                    antiAliasing = 1,
                    useMipMap = false,
                    useDynamicScale = false,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
            }

            if (Distorted && DistortedTexture == null)
            {
                DistortedTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, CameraTargetTextureReadWriteType)
                {
                    dimension = TextureDimension.Tex2D,
                    antiAliasing = 1,
                    useMipMap = false,
                    useDynamicScale = false,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    enableRandomWrite = true,
                };
                DistortedTexture.Create();
            }
        }

        void CheckCapture()
        {
            if (Time.time >= NextCaptureTime)
            {
                SensorCamera.Render();
                if (Distorted)
                {
                    LensDistortion.Distort(SensorCamera.targetTexture, DistortedTexture);
                }

                var capture = new CameraCapture()
                {
                    CaptureTime = SimulatorManager.Instance.CurrentTime,
                    Request = AsyncGPUReadback.Request(Distorted ? DistortedTexture : SensorCamera.targetTexture, 0, TextureFormat.RGBA32),
                };
                CaptureQueue.Enqueue(capture);

                NextCaptureTime = Time.time + (1.0f / Frequency);
            }
        }

        void ProcessReadbackRequests()
        {
            while (CaptureQueue.Count > 0)
            {
                var capture = CaptureQueue.Peek();
                if (capture.Request.hasError)
                {
                    CaptureQueue.Dequeue();
                    Debug.Log("Failed to read GPU texture");
                }
                else if (capture.Request.done)
                {
                    CaptureQueue.Dequeue();
                    var data = capture.Request.GetData<byte>();

                    var imageData = new ImageData()
                    {
                        Name = Name,
                        Frame = Frame,
                        Width = Width,
                        Height = Height,
                        Sequence = Sequence,
                    };

                    if (!JpegOutput.TryTake(out imageData.Bytes))
                    {
                        imageData.Bytes = new byte[MaxJpegSize];
                    }

                    if (Bridge != null && Bridge.Status == Status.Connected)
                    {
                        Task.Run(() =>
                        {
                            imageData.Length = JpegEncoder.Encode(data, Width, Height, 4, JpegQuality, imageData.Bytes);
                            if (imageData.Length > 0)
                            {
                                imageData.Time = capture.CaptureTime;
                                ImageWriter.Write(imageData);
                                JpegOutput.Add(imageData.Bytes);
                            }
                            else
                            {
                                Debug.Log("Compressed image is empty, length = 0");
                            }
                        });
                    }

                    Sequence++;
                }
                else
                {
                    break;
                }
            }
        }

        public bool Save(string path, int quality, int compression)
        {
            CheckTexture();
            SensorCamera.Render();
            if (Distorted)
            {
                LensDistortion.Distort(SensorCamera.targetTexture, DistortedTexture);
            }
            var readback = AsyncGPUReadback.Request(Distorted ? DistortedTexture : SensorCamera.targetTexture, 0, TextureFormat.RGBA32);
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
            visualizer.UpdateRenderTexture(Distorted ? DistortedTexture : SensorCamera.activeTexture, SensorCamera.aspect);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }

        public override bool CheckVisible(Bounds bounds)
        {
            var activeCameraPlanes = GeometryUtility.CalculateFrustumPlanes(SensorCamera);
            return GeometryUtility.TestPlanesAABB(activeCameraPlanes, bounds);
        }
    }
}
