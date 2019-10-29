/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Plugins;
using Simulator.Utilities;
using Simulator.Sensors.UI;

namespace Simulator.Sensors
{
    [SensorType("Semantic Camera", new[] {typeof(ImageData)})]
    [RequireComponent(typeof(Camera))]
    public class SemanticCameraSensor : SensorBase
    {
        [SensorParameter]
        [Range(1, 1920)]
        public int Width = 1920;

        [SensorParameter]
        [Range(1, 1080)]
        public int Height = 1080;

        [SensorParameter]
        [Range(1, 100)]
        public int Frequency = 5;

        [SensorParameter]
        [Range(1.0f, 90.0f)]
        public float FieldOfView = 60.0f;

        [SensorParameter]
        [Range(0.01f, 1000.0f)]
        public float MinDistance = 0.1f;

        [SensorParameter]
        [Range(0.01f, 2000.0f)]
        public float MaxDistance = 1000.0f;

        IWriter<ImageData> ImageWriter;
        ImageData Data;

        IBridge Bridge;

        const int MaxJpegSize = 4 * 1024 * 1024; // 4MB

        private Camera Camera;

        private float nextCaptureTime = 0.0f;

        private bool capturePending = false;

        private struct CameraCapture
        {
            public AsyncGPUReadbackRequest readbackRequest;

            public double captureTime;

            public CameraCapture(AsyncGPUReadbackRequest request)
            {
                readbackRequest = request;
                captureTime = SimulatorManager.Instance.CurrentTime;
            }
        }

        private Queue<CameraCapture> captureQueue = new Queue<CameraCapture>();

        public void Start()
        {
            Camera = GetComponent<Camera>();
            Camera.enabled = false;

            Camera.GetComponent<HDAdditionalCameraData>().customRender += CustomRender;

            Data = new ImageData()
            {
                Name = Name,
                Frame = Frame,
                Width = Width,
                Height = Height,
                Bytes = new byte[MaxJpegSize],
            };
        }

        void CustomRender(ScriptableRenderContext context, HDCamera hd)
        {
            var camera = hd.camera;

            ScriptableCullingParameters culling;
            if (camera.TryGetCullingParameters(out culling))
            {
                var cull = context.Cull(ref culling);

                context.SetupCameraProperties(camera);

                var cmd = CommandBufferPool.Get();
                hd.SetupGlobalParams(cmd, 0, 0, 0);
                cmd.ClearRenderTarget(true, true, SimulatorManager.Instance.SemanticSkyColor);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                var sorting = new SortingSettings(camera);
                var drawing = new DrawingSettings(new ShaderTagId("SimulatorSemanticPass"), sorting);
                var filter = new FilteringSettings(RenderQueueRange.all);

                context.DrawRenderers(cull, ref drawing, ref filter);
            }
        }

        public void OnDestroy()
        {
            Camera.targetTexture?.Release();
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            ImageWriter = bridge.AddWriter<ImageData>(Topic);
        }

        public void Update()
        {
            Camera.fieldOfView = FieldOfView;
            Camera.nearClipPlane = MinDistance;
            Camera.farClipPlane = MaxDistance;
            
            CheckTexture();
            CheckCapture();
            ProcessReadbackRequests();
        }

        void CheckTexture()
        {
            // if this is not first time
            if (Camera.targetTexture != null)
            {
                if (Width != Camera.targetTexture.width || Height != Camera.targetTexture.height)
                {
                    // if camera capture size has changed
                    Data.Width = Width;
                    Data.Height = Height;

                    Camera.targetTexture.Release();
                    Camera.targetTexture = null;
                }
                else if (!Camera.targetTexture.IsCreated())
                {
                    // if we have lost rendertexture due to Unity window resizing or otherwise
                    Camera.targetTexture.Release();
                    Camera.targetTexture = null;
                }
            }

            if (Camera.targetTexture == null)
            {
                Camera.targetTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                {
                    dimension = TextureDimension.Tex2D,
                    antiAliasing = 1,
                    useMipMap = false,
                    useDynamicScale = false,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
            }
        }

		void CheckCapture()
        {
            if(capturePending)
            {
                TriggerCapture();
            }

            if(Time.time >= nextCaptureTime)
            {
                nextCaptureTime = Time.time + (1.0f / Frequency);
                capturePending = true;
                Camera.enabled = true;
            }
        }

        void TriggerCapture()
        {
            var readbackRequest = AsyncGPUReadback.Request(Camera.targetTexture, 0, TextureFormat.RGBA32);
            var capture = new CameraCapture(readbackRequest);
            captureQueue.Enqueue(capture);
            Camera.enabled = false;
            capturePending = false;
        }

        void ProcessReadbackRequests()
        {
            while(captureQueue.Count > 0)
            {
                var capture = captureQueue.Peek();
                if(capture.readbackRequest.hasError)
                {
                    Debug.Log("Failed to read GPU texture");
                    captureQueue.Dequeue();
                }
                else if(capture.readbackRequest.done)
                {
                    var data = capture.readbackRequest.GetData<byte>();
                    captureQueue.Dequeue();
                    var imageData = new ImageData()
                    {
                        Name = Name,
                        Frame = Frame,
                        Width = Width,
                        Height = Height,
                        Bytes = new byte[MaxJpegSize],
                        Sequence = Data.Sequence,
                    };
                    
                    if (Bridge != null && Bridge.Status == Status.Connected)
                    {
                        Task.Run(() =>
                        {
                            imageData.Length = JpegEncoder.Encode(data, Width, Height, 4, 100, imageData.Bytes);
                            if (imageData.Length > 0)
                            {
                                imageData.Time = capture.captureTime;
                                ImageWriter.Write(imageData);
                            }
                            else
                            {
                                Debug.Log("Compressed image is empty, length = 0");
                            }
                        });
                    }

                    Data.Sequence++;
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
            Camera.Render();
            var readback = AsyncGPUReadback.Request(Camera.targetTexture, 0, TextureFormat.RGBA32);
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
            visualizer.UpdateRenderTexture(Camera.activeTexture, Camera.aspect);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
