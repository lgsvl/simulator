/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Plugins;

namespace Simulator.Sensors
{
    public enum CameraType
    {
        Color,
        Depth,
        Segmentation,
    }

    [RequireComponent(typeof(Camera))]
    public class CameraSensor : SensorBase
    {
        public CameraType Type = CameraType.Color;
        public int Width = 1280;
        public int Height = 720;
        [Range(1, 100)]
        public int SendRate = 15;
        [Range(0, 100)]
        public int JpegQuality = 75;

        IWriter<ImageData> ImageWriter;
        ImageData Data;

        TextureFormat ReadTextureFormat;
        RenderTextureFormat RenderTextureFormat;
        RenderTextureReadWrite RenderColorSpace;

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

            int readSize;
            if (Type == CameraType.Color)
            {
                ReadTextureFormat = TextureFormat.RGB24;
                RenderTextureFormat = RenderTextureFormat.ARGB32;
                RenderColorSpace = RenderTextureReadWrite.sRGB;
                readSize = Width * Height * 3;
            }
            else if (Type == CameraType.Depth)
            {
                ReadTextureFormat = TextureFormat.RGB24; // TODO: change to RFloat
                RenderTextureFormat = RenderTextureFormat.ARGB32; // TODO: change to RFloat
                RenderColorSpace = RenderTextureReadWrite.Linear;
                readSize = Width * Height * 3;
            }
            else if (Type == CameraType.Segmentation)
            {
                // TODO: how to get sahder?
                // Camera.SetReplacementShader(shader, "SegmentColor");
                Camera.backgroundColor = Color.black; //= color;
                Camera.clearFlags = CameraClearFlags.SolidColor;
                Camera.renderingPath = RenderingPath.Forward;

                ReadTextureFormat = TextureFormat.RGB24;
                RenderTextureFormat = RenderTextureFormat.ARGB32;
                RenderColorSpace = RenderTextureReadWrite.Linear;
                readSize = Width * Height * 3;
            }
            else
            {
                throw new Exception("Unknown camera type");
            }

            Data = new ImageData()
            {
                Name = Name,
                Frame = Frame,
                Width = Width,
                Height = Height,
                Bytes = new byte[MaxJpegSize],
            };
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
                Camera.targetTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat, RenderColorSpace)
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
                    ReadBuffer = new NativeArray<byte>(Width * Height * 3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                }
            }

            StartCoroutine(Capture());
        }

        IEnumerator Capture()
        {
            Capturing = true;
            var captureStart = Time.time;

            Camera.Render();

            var readback = AsyncGPUReadback.Request(Camera.targetTexture, 0, ReadTextureFormat);

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
                Data.Length = JpegEncoder.Encode(ReadBuffer, Width, Height, 3, JpegQuality, Data.Bytes);
                if (Data.Length > 0)
                {
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
