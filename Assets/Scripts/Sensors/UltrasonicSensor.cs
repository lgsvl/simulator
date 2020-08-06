/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Simulator.Bridge;
using Simulator.PointCloud;
using Simulator.Utilities;
using Simulator.Sensors.UI;
using UnityEngine.Rendering.HighDefinition;
using Unity.Collections;

using UltrasonicData = Simulator.Bridge.Data.UltrasonicData;

namespace Simulator.Sensors
{
    [RequireComponent(typeof(Camera))]
    [SensorType("Ultrasonic", new[] { typeof(UltrasonicData) })]
    public class UltrasonicSensor : SensorBase
    {
        [SensorParameter]
        [Range(1, 1920)]
        public int Width = 400;

        [SensorParameter]
        [Range(1, 1080)]
        public int Height = 160;

        [SensorParameter]
        [Range(1, 100)]
        public int Frequency = 15;

        [SensorParameter]
        [Range(0, 100)]
        public int JpegQuality = 75;

        [SensorParameter]
        [Range(1.0f, 90.0f)]
        public float FieldOfView = 40.0f;

        [SensorParameter]
        [Range(0.01f, 1000.0f)]
        public float MinDistance = 0.3f;

        [SensorParameter]
        [Range(0.01f, 2000.0f)]
        public float MaxDistance = 2.0f;

        BridgeInstance Bridge;
        Publisher<UltrasonicData> Publish;
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

        public override SensorDistributionType DistributionType => SensorDistributionType.LowLoad;
        private int CurrentWidth, CurrentHeight;
        private float CurrentFieldOfView;

        protected SensorRenderTarget renderTarget;
        private RenderTexture visualizationTexture;

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

        UltrasonicData UltrasonicResult;

        private ShaderTagId passId;

        public void Start()
        {
            SensorCamera.enabled = false;

            CurrentWidth = Width;
            CurrentHeight = Height;
            CurrentFieldOfView = FieldOfView;
            SizeChanged = false;

            var hd = SensorCamera.GetComponent<HDAdditionalCameraData>();
            hd.hasPersistentHistory = true;

            passId = new ShaderTagId("SimulatorLidarPass");
            SensorCamera.GetComponent<HDAdditionalCameraData>().customRender += CustomRender;
            // Create a RenderTexture without alpha channel, so that we can only visualize RGB channels.
            visualizationTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.RGB565);
        }

        void CustomRender(ScriptableRenderContext context, HDCamera hd)
        {
            var cmd = CommandBufferPool.Get();
            SensorPassRenderer.Render(context, cmd, hd, renderTarget, passId, Color.white);
            PointCloudManager.RenderDepth(context, cmd, hd, renderTarget.ColorHandle, renderTarget.DepthHandle);
            CommandBufferPool.Release(cmd);
        }

        public void OnDestroy()
        {
            renderTarget?.Release();
            
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
            Publish = bridge.AddPublisher<UltrasonicData>(Topic);
        }

        public void Update()
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

            while (Tasks.Count > 0 && Tasks.Peek().IsCompleted)
            {
                Tasks.Dequeue();
            }

            CheckTexture();
            CheckCapture();
            ProcessReadbackRequests();

            SizeChanged = false;
        }

        void CheckTexture()
        {
            // if this is not first time
            if (renderTarget != null)
            {
                if (!renderTarget.IsValid(Width, Height))
                {
                    // if camera capture size has changed or we have lost rendertexture due to Unity window resizing or otherwise
                    renderTarget.Release();
                    renderTarget = null;
                }
            }

            if (renderTarget == null)
            {
                renderTarget = SensorRenderTarget.Create2D(Width, Height);
                SensorCamera.targetTexture = renderTarget;
            }
        }

        void RenderCamera()
        {
            SensorCamera.Render();
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
                capture.Request = AsyncGPUReadback.Request(renderTarget.ColorTexture, 0, TextureFormat.RGBA32);
                CaptureList.Add(capture);

                NextCaptureTime = Time.time + (1.0f / Frequency);
            }
        }

        private static float DecodeFloatRGB(byte r, byte g, byte b, byte a)
        {
            return (r / 255.0f) + (g / 255.0f) / 255.0f + (b / 255.0f) / 65025.0f;
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
                    // TODO: Remove the following two lines of extra memory copy, when we can use 
                    // AsyncGPUReadback.RequestIntoNativeArray.
                    var data = capture.Request.GetData<byte>();
                    NativeArray<byte>.Copy(data, capture.GpuData, data.Length);

                    float min_d = 100f;
                    float distance = 0f;
                    int min_x = -1;
                    int min_y = -1;
                    // TODO: Test GPU version of finding min_d among all pixels
                    // CPU version may be better if the ulstrasonic sensor returns more than 1 point
                    for (int y = 0; y < Height; y++)
                    {
                        int yOffset = y * Width * 4;
                        for (int x = 0; x < Width; x++)
                        {
                            byte r = data[yOffset + x * 4 + 0];
                            byte g = data[yOffset + x * 4 + 1];
                            byte b = data[yOffset + x * 4 + 2];
                            byte a = data[yOffset + x * 4 + 3];
                            distance = 2.0f * DecodeFloatRGB(r, g, b, a) * MaxDistance;
                            if (min_d > distance)
                            {
                                min_d = distance;
                                min_x = x;
                                min_y = y;
                            }
                        }
                    }

                    UltrasonicResult = new UltrasonicData()
                    {
                        Name = Name,
                        Frame = Frame,
                        Time = SimulatorManager.Instance.CurrentTime,
                        Sequence = Sequence,
                        MinimumDistance = min_d > MaxDistance ? -1 : min_d,
                    };

                    if (Bridge != null && Bridge.Status == Status.Connected)
                    {
                        Tasks.Enqueue(Task.Run(() =>
                        {
                            Publish(UltrasonicResult);
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

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);

            if (UltrasonicResult == null)
            {
                return;
            }

            var graphData = new Dictionary<string, object>()
            {
                {"Time", UltrasonicResult.Time},
                {"Minimum distance", UltrasonicResult.MinimumDistance}
            };
            visualizer.UpdateGraphValues(graphData);
            // TODO: Add visualization of the closest point (with color changes according to distance).
            Graphics.Blit(renderTarget.ColorTexture, visualizationTexture);
            visualizer.UpdateRenderTexture(visualizationTexture, SensorCamera.aspect);
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
