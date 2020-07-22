/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
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
using UnityEngine.Rendering.HighDefinition;
using Unity.Collections;

using LensDistortion = Simulator.Utilities.LensDistortion;
using UnityEngine.Experimental.Rendering;

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
        public float MaxDistance = 2000.0f;

        [SensorParameter]
        public bool Distorted = false;

        [SensorParameter]
        public List<float> DistortionParameters;

        [SensorParameter]
        public bool Fisheye = false;

        [SensorParameter]
        public float Xi = 0.0f;

        BridgeInstance Bridge;
        Publisher<ImageData> Publish;
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

        public override SensorDistributionType DistributionType => SensorDistributionType.UltraHighLoad;
        protected int RenderTextureWidth, RenderTextureHeight;
        private int CurrentWidth, CurrentHeight;
        private float CurrentFieldOfView;
        private bool CurrentDistorted;
        private List<float> CurrentDistortionParameters;
        private float CurrentXi;
        private int CurrentCubemapSize;

        protected SensorRenderTarget renderTarget;

        private LensDistortion LensDistortion;
        private RenderTexture DistortedTexture;
        float FrustumWidth, FrustumHeight;

        [SensorParameter]
        public int CubemapSize = 1024;
        private RenderTexture CubemapTexture;
        protected int faceMask;

        bool SizeChanged;
        ConcurrentBag<NativeArray<byte>> AvailableGpuDataArrays = new ConcurrentBag<NativeArray<byte>>();

        private struct CameraCapture
        {
            public NativeArray<byte> GpuData;
            public AsyncGPUReadbackRequest Request;
            public double CaptureTime;
        }

        private List<CameraCapture> CaptureList = new List<CameraCapture>();
        private ConcurrentBag<byte[]> JpegOutput = new ConcurrentBag<byte[]>();
        private Queue<Task> Tasks = new Queue<Task>();

        public virtual void Start()
        {
            SensorCamera.enabled = false;

            CurrentWidth = Width;
            CurrentHeight = Height;
            CurrentFieldOfView = FieldOfView;
            CurrentDistorted = Distorted;
            CurrentDistortionParameters = new List<float>(DistortionParameters);
            CurrentXi = Xi;
            CurrentCubemapSize = CubemapSize;
            SizeChanged = false;

            var hd = SensorCamera.GetComponent<HDAdditionalCameraData>();
            hd.hasPersistentHistory = true;
        }

        public void OnDestroy()
        {
            renderTarget?.Release();
            
            if (DistortedTexture != null)
            {
                DistortedTexture.Release();
            }

            foreach (var capture in CaptureList)
            {
                capture.GpuData.Dispose();
            }
            CaptureList.Clear();

            // Wait all tasks finished to gurantee all native arrays are in AvailableGpuDataArrays.
            Task.WaitAll(Tasks.ToArray());
            while (AvailableGpuDataArrays.TryTake(out var gpuData))
            {
                gpuData.Dispose();
            }
        }

        public override void OnBridgeSetup(BridgeInstance bridge)
        {
            Bridge = bridge;
            Publish = bridge.AddPublisher<ImageData>(Topic);
        }

        protected virtual void Update()
        {
            SensorCamera.fieldOfView = FieldOfView;
            SensorCamera.nearClipPlane = MinDistance;
            SensorCamera.farClipPlane = MaxDistance;

            if (CurrentWidth != Width || CurrentHeight != Height)
            {
                SizeChanged = true;
                CurrentWidth = Width;
                CurrentHeight = Height;
            }

            if (Distorted)
            {
                CheckDistortion();
            }
            else
            {
                ResetDistortion();
            }

            while (Tasks.Count > 0 && Tasks.Peek().IsCompleted)
            {
                Tasks.Dequeue();
            }

            CheckTexture();
            CheckCapture();
            ProcessReadbackRequests();

            SizeChanged = false;
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

            if (LensDistortion == null || SizeChanged || 
                CurrentFieldOfView != FieldOfView || CurrentDistorted != Distorted || 
                !Enumerable.SequenceEqual(DistortionParameters, CurrentDistortionParameters) ||
                (Fisheye && CurrentXi != Xi))
            {
                // View frustum size at the focal plane.
                FrustumHeight = 2 * Mathf.Tan(FieldOfView / 2 * Mathf.Deg2Rad);
                FrustumWidth = FrustumHeight * Width / Height;
                if (LensDistortion == null)
                {
                    LensDistortion = new LensDistortion();
                }

                LensDistortion.InitDistortion(DistortionParameters, FrustumWidth, FrustumHeight, Fisheye ? Xi : 0);
                LensDistortion.CalculateRenderTextureSize(Width, Height, out RenderTextureWidth, out RenderTextureHeight);
                if (RenderTextureWidth <= 0 || RenderTextureHeight <= 0)
                {
                    throw new Exception("Distortion parameters cause texture size invalid (<= 0).");
                }
                faceMask = 0;
                faceMask |= 1 << (int)(CubemapFace.PositiveX); // right face
                faceMask |= 1 << (int)(CubemapFace.NegativeX); // left face
                faceMask |= 1 << (int)(CubemapFace.PositiveY); // top face
                faceMask |= 1 << (int)(CubemapFace.NegativeY); // bottom face
                faceMask |= 1 << (int)(CubemapFace.PositiveZ); // front face

                CurrentFieldOfView = FieldOfView;
                CurrentDistorted = Distorted;
                CurrentDistortionParameters = new List<float>(DistortionParameters);
                CurrentXi = Xi;
            }
        }

        void ResetDistortion()
        {
            RenderTextureWidth = Width;
            RenderTextureHeight = Height;
        }

        void CheckTexture()
        {
            if (!Distorted || !Fisheye) // No-distortion and non-fisheye distortion use Camera.Render().
            {
                // if this is not first time
                if (renderTarget != null)
                {
                    if (!renderTarget.IsValid(RenderTextureWidth, RenderTextureHeight))
                    {
                        // if camera capture size has changed or we have lost rendertexture due to Unity window resizing or otherwise
                        renderTarget.Release();
                        renderTarget = null;
                    }
                }

                if (renderTarget == null)
                {
                    renderTarget = SensorRenderTarget.Create2D(RenderTextureWidth, RenderTextureHeight, GraphicsFormat.R8G8B8A8_SRGB);
                    SensorCamera.targetTexture = renderTarget;
                }
            }
            else
            {
                CheckCubemapTexture();
            }

            if (Distorted)
            {
                // if this is not first time
                if (DistortedTexture != null)
                {
                    if (SizeChanged)
                    {
                        // if camera capture size has changed
                        DistortedTexture.Release();
                        DistortedTexture = null;
                    }
                    else if (!DistortedTexture.IsCreated())
                    {
                        // if we have lost DistortedTexture due to Unity window resizing or otherwise
                        DistortedTexture.Release();
                        DistortedTexture = null;
                    }
                }
                if (DistortedTexture == null)
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
        }

        protected void RenderCamera()
        {
            if (!Distorted || !Fisheye)
            {
                SensorCamera.Render();
                if (Distorted)
                {
                    LensDistortion.PlumbBobDistort(renderTarget, DistortedTexture);
                }
            }
            else
            {
                RenderToCubemap();
                LensDistortion.UnifiedProjectionDistort(renderTarget ?? CubemapTexture, DistortedTexture);
            }
        }

        protected virtual void CheckCubemapTexture()
        {
            // Fisheye camera uses Camera.RenderToCubemap(...), and thus do not need normal RenderTexture.
            // Although Camera.RenderToCubemap has its own target texture as parameter,
            // setting Camera.targetTexture will still affect its result.
            // So we set Camera.targetTexture to null to make sure its result is correct.
            SensorCamera.targetTexture = null;
            renderTarget?.Release();
            renderTarget = null;

            if (CurrentCubemapSize != CubemapSize)
            {
                CubemapTexture.Release();
                CubemapTexture = null;
            }
            if (CubemapTexture == null)
            {
                CubemapTexture = new RenderTexture(CubemapSize, CubemapSize, 24, RenderTextureFormat.ARGB32, CameraTargetTextureReadWriteType)
                {
                    dimension = TextureDimension.Cube,
                    antiAliasing = 1,
                    useMipMap = false,
                    useDynamicScale = false,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
                CubemapTexture.Create();
            }
        }
        
        protected virtual void RenderToCubemap()
        {
            // Monoscopic mode of RenderToCubemap generates the result ALWAYS aligned with world coordinate system.
            // Stereoscopi mode of RenderToCubemap can generate the result rotate with camera correctly.
            // Setting Camera.stereoSeparation to 0 makes the result from Left/Right eye same as monoscopic mode.
            SensorCamera.stereoSeparation = 0f;
            SensorCamera.RenderToCubemap(CubemapTexture, faceMask, Camera.MonoOrStereoscopicEye.Left);
        }

        void CheckCapture()
        {
            if (Time.time >= NextCaptureTime)
            {
                RenderCamera();

                NativeArray<byte> gpuData;
                while (AvailableGpuDataArrays.TryTake(out gpuData) && gpuData.Length != Width * Height * 4)
                {
                    gpuData.Dispose();
                }
                if (!gpuData.IsCreated)
                {
                    gpuData = new NativeArray<byte>(Width * Height * 4, Allocator.Persistent);
                }

                var capture = new CameraCapture()
                {
                    GpuData = gpuData,
                    CaptureTime = SimulatorManager.Instance.CurrentTime,
                };
                capture.Request = AsyncGPUReadback.Request(Distorted ? DistortedTexture : renderTarget.ColorTexture, 0, TextureFormat.RGBA32);
                // TODO: Replace above AsyncGPUReadback.Request with following AsyncGPUReadback.RequestIntoNativeArray when we upgrade to Unity 2020.1
                // See https://issuetracker.unity3d.com/issues/asyncgpureadback-dot-requestintonativearray-crashes-unity-when-trying-to-request-a-copy-to-the-same-nativearray-multiple-times
                // for the detaisl of the bug in Unity.
                //capture.Request = AsyncGPUReadback.RequestIntoNativeArray(ref capture.GpuData, Distorted ? DistortedTexture : SensorCamera.targetTexture, 0, TextureFormat.RGBA32);
                CaptureList.Add(capture);

                NextCaptureTime = Time.time + (1.0f / Frequency);
            }
        }

        void ProcessReadbackRequests()
        {
            foreach (var capture in CaptureList)
            {
                if (capture.Request.hasError)
                {
                    AvailableGpuDataArrays.Add(capture.GpuData);
                    Debug.Log("Failed to read GPU texture");
                }
                else if (capture.Request.done)
                {
                    if (Bridge != null && Bridge.Status == Status.Connected)
                    {
                        // TODO: Remove the following two lines of extra memory copy, when we can use 
                        // AsyncGPUReadback.RequestIntoNativeArray.
                        var data = capture.Request.GetData<byte>();
                        NativeArray<byte>.Copy(data, capture.GpuData, data.Length);

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

                        Tasks.Enqueue(Task.Run(() =>
                        {
                            imageData.Length = JpegEncoder.Encode(capture.GpuData, Width, Height, 4, JpegQuality, imageData.Bytes);
                            if (imageData.Length > 0)
                            {
                                imageData.Time = capture.CaptureTime;
                                Publish(imageData);
                            }
                            else
                            {
                                Debug.Log("Compressed image is empty, length = 0");
                            }
                            JpegOutput.Add(imageData.Bytes);
                            AvailableGpuDataArrays.Add(capture.GpuData);
                        }));

                        Sequence++;
                    }
                    else
                    {
                        AvailableGpuDataArrays.Add(capture.GpuData);
                    }
                }
            }
            CaptureList.RemoveAll(capture => capture.Request.done == true);
        }

        public virtual bool Save(string path, int quality, int compression)
        {
            CheckTexture();
            RenderCamera();
            var readback = AsyncGPUReadback.Request(Distorted ? DistortedTexture : renderTarget.ColorTexture, 0, TextureFormat.RGBA32);
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
            visualizer.UpdateRenderTexture(Distorted ? DistortedTexture : renderTarget.ColorTexture, SensorCamera.aspect);
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
