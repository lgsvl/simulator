/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Plugins;
using Simulator.Utilities;

namespace Simulator.Sensors
{
    [SensorType("Depth Camera", new[] {typeof(ImageData)})]
    [RequireComponent(typeof(Camera))]
    public class DepthCameraSensor : SensorBase
    {
        [SensorParameter]
        [Range(1, 1920)]
        public int Width = 1920;

        [SensorParameter]
        [Range(1, 1080)]
        public int Height = 1080;

        [SensorParameter]
        [Range(1, 100)]
        public int SendRate = 5;

        [SensorParameter]
        [Range(0, 100)]
        public int JpegQuality = 100;

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

        AsyncGPUReadbackRequest Readback;
        NativeArray<byte> ReadBuffer;

        IBridge Bridge;
        Camera Camera;
        bool Capturing;

        const int MaxJpegSize = 4 * 1024 * 1024; // 4MB

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
                cmd.ClearRenderTarget(true, true, Color.white);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                var sorting = new SortingSettings(camera);
                var drawing = new DrawingSettings(new ShaderTagId("SimulatorDepthPass"), sorting);
                var filter = new FilteringSettings(RenderQueueRange.all);

                context.DrawRenderers(cull, ref drawing, ref filter);
            }
        }

        public void OnDestroy()
        {
            StopAllCoroutines();

            if (ReadBuffer.IsCreated)
            {
                ReadBuffer.Dispose();
            }

            Camera.targetTexture?.Release();
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            ImageWriter = bridge.AddWriter<ImageData>(Topic);
        }

        public void Update()
        {
            if (Capturing)
            {
                return;
            }

            Camera.fieldOfView = FieldOfView;
            Camera.nearClipPlane = MinDistance;
            Camera.farClipPlane = MaxDistance;

            if (Bridge == null || Bridge.Status != Status.Connected)
            {
                return;
            }

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
                    ReadBuffer.Dispose();
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

                if (!ReadBuffer.IsCreated)
                {
                    ReadBuffer = new NativeArray<byte>(Width * Height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                }
            }

            StartCoroutine(Capture());
        }

        IEnumerator Capture()
        {
            Capturing = true;
            var captureStart = Time.time;

            Camera.Render();

            var readback = AsyncGPUReadback.Request(Camera.targetTexture, 0, TextureFormat.RGBA32);

            yield return new WaitUntil(() => readback.done);

            if (readback.hasError)
            {
                Debug.Log("Failed to read GPU texture");
                Capturing = false;
                yield break;
            }

            Debug.Assert(readback.done);
            var data = readback.GetData<byte>();
            ReadBuffer.CopyFrom(data);

            bool sending = true;
            Task.Run(() =>
            {
                Data.Length = JpegEncoder.Encode(ReadBuffer, Width, Height, 4, JpegQuality, Data.Bytes);
                if (Data.Length > 0)
                {
                    Data.Time = SimulatorManager.Instance.CurrentTime;
                    ImageWriter.Write(Data, () => sending = false);
                }
                else
                {
                    Debug.Log("Compressed image is empty, length = 0");
                    sending = false;
                }
            });

            yield return new WaitWhile(() => sending);
            Data.Sequence++;

            var captureEnd = Time.time;
            var captureDelta = captureEnd - captureStart;
            var delay = 1.0f / SendRate - captureDelta;

            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            Capturing = false;
        }
    }
}
