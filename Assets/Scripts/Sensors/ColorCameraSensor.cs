/**
 * Copyright (c) 2019 LG Electronics, Inc.
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
using Simulator.Bridge.Data;
using Simulator.Plugins;
using Simulator.Utilities;
using Simulator.Sensors.UI;

namespace Simulator.Sensors
{
    [SensorType("Color Camera", new[] { typeof(ImageData) })]
    [RequireComponent(typeof(Camera))]
    public class ColorCameraSensor : SensorBase
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

        IBridge Bridge;
        IWriter<ImageData> ImageWriter;
        uint Sequence;

        const int MaxJpegSize = 4 * 1024 * 1024; // 4MB

        private Camera Camera;
        private float NextCaptureTime;

        private struct CameraCapture
        {
            public AsyncGPUReadbackRequest Request;
            public double CaptureTime;
        }

        private Queue<CameraCapture> CaptureQueue = new Queue<CameraCapture>();
        private ConcurrentBag<byte[]> JpegOutput = new ConcurrentBag<byte[]>();

        public void Start()
        {
            Camera = GetComponent<Camera>();
            Camera.enabled = false;
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
                Camera.targetTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
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
            if (Time.time >= NextCaptureTime)
            {
                Camera.Render();

                var capture = new CameraCapture()
                {
                    CaptureTime = SimulatorManager.Instance.CurrentTime,
                    Request = AsyncGPUReadback.Request(Camera.targetTexture, 0, TextureFormat.RGBA32),
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
