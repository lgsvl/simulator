/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.Rendering;

public enum AsyncTextureReaderStatus : int
{
    Idle = 0,
    Reading = 1,
    Finished = 2,
}

static class AsyncTextureReaderImports
{
    [DllImport("AsyncTextureReader", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AsyncTextureReaderSetDebug(System.IntPtr ptr);

    [DllImport("AsyncTextureReader", CallingConvention = CallingConvention.Cdecl)]
    public static extern int AsyncTextureReaderCreate(System.IntPtr texture, int size);

    [DllImport("AsyncTextureReader", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AsyncTextureReaderDestroy(int id);

    [DllImport("AsyncTextureReader", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AsyncTextureReaderStart(int id, System.IntPtr data);

    [DllImport("AsyncTextureReader", CallingConvention = CallingConvention.Cdecl)]
    public static extern AsyncTextureReaderStatus AsyncTextureReaderGetStatus(int id);

    [DllImport("AsyncTextureReader", CallingConvention = CallingConvention.Cdecl)]
    public static extern System.IntPtr AsyncTextureReaderGetUpdate();
}

public class AsyncTextureReader<T> where T : struct
{
    public enum ReadType
    {
        None,
        Sync,
        Native,
        LinuxOpenGL,
    }

    public AsyncTextureReaderStatus Status { get; private set; }
    NativeArray<T> Data;

    ReadType Type = ReadType.None;
    AsyncGPUReadbackRequest NativeReadRequest;
    public TextureFormat NativeReadFormat { get; private set; }

    public int BytesPerPixel { get; private set; }

    public RenderTexture Texture { get; private set; }
    Texture2D ReadTexture;

    int LinuxId;
    System.IntPtr LinuxUpdate;

    public AsyncTextureReader(RenderTexture texture)
    {
        Status = AsyncTextureReaderStatus.Idle;
        Texture = texture;

        // WARNING - if you change this, you'll need to update code below
        Debug.Assert(
            texture.format == RenderTextureFormat.ARGBFloat ||
            texture.format == RenderTextureFormat.RGFloat ||
            texture.format == RenderTextureFormat.RFloat ||
            texture.format == RenderTextureFormat.ARGB32);

        Debug.Assert(texture.dimension == TextureDimension.Tex2D);

        var sync = System.Environment.GetEnvironmentVariable("FORCE_SYNC_GPU_READBACK");
        if (sync == null)
        {
            if (SystemInfo.supportsAsyncGPUReadback)
            {
                int length;
                Type = ReadType.Native;
                if (texture.format == RenderTextureFormat.ARGBFloat)
                {
                    BytesPerPixel = 16;
                    NativeReadFormat = TextureFormat.RGBAFloat;
                    length = Texture.width * Texture.height;
                }
                else if (texture.format == RenderTextureFormat.RGFloat)
                {
                    BytesPerPixel = 8;
                    NativeReadFormat = TextureFormat.RGFloat;
                    length = Texture.width * Texture.height;
                }
                else if (texture.format == RenderTextureFormat.RFloat)
                {
                    BytesPerPixel = 4;
                    NativeReadFormat = TextureFormat.RFloat;
                    length = Texture.width * Texture.height;
                }
                else // if (texture.format == RenderTextureFormat.ARGB32)
                {
                    BytesPerPixel = 3;
                    NativeReadFormat = TextureFormat.RGB24;
                    length = Texture.width * Texture.height * BytesPerPixel;
                }
                Data = new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                return;
            }

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore &&
                SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
            {
                var version = SystemInfo.graphicsDeviceVersion;
                version = version.Split(new char[] { ' ' }, 3)[1];
                var parts = version.Split(new char[] { '.' });
                int major = int.Parse(parts[0]);
                int minor = int.Parse(parts[1]);
                //Debug.Log($"OpenGL version = {major}.{minor}");

                if (major > 3 || major == 3 && minor >= 2) // GL_ARB_sync
                {
                    debug = new DebugDelegate(DebugCallback);
                    AsyncTextureReaderImports.AsyncTextureReaderSetDebug(Marshal.GetFunctionPointerForDelegate(debug));

                    int length;
                    if (texture.format == RenderTextureFormat.ARGBFloat)
                    {
                        BytesPerPixel = 16;
                        length = Texture.width * Texture.height;
                    }
                    else if (texture.format == RenderTextureFormat.RGFloat)
                    {
                        BytesPerPixel = 8;
                        length = Texture.width * Texture.height;
                    }
                    else if (texture.format == RenderTextureFormat.RFloat)
                    {
                        BytesPerPixel = 4;
                        length = Texture.width * Texture.height;
                    }
                    else // if (texture.format == RenderTextureFormat.ARGB32)
                    {
                        BytesPerPixel = 4;
                        length = Texture.width * Texture.height * BytesPerPixel;
                    }

                    texture.Create();
                    Data = new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    LinuxId = AsyncTextureReaderImports.AsyncTextureReaderCreate(texture.GetNativeTexturePtr(), Data.Length * BytesPerPixel);
                    if (LinuxId >= 0)
                    {
                        LinuxUpdate = AsyncTextureReaderImports.AsyncTextureReaderGetUpdate();
                        GL.IssuePluginEvent(LinuxUpdate, LinuxId);

                        Type = ReadType.LinuxOpenGL;
                    }
                    else
                    {
                        Debug.Log("ERROR: failed to create native AsyncTextureReader");
                    }
                    return;
                }
            }
        }

        if (texture.format != RenderTextureFormat.ARGB32)
        {
            Debug.Log("ERROR: fallback AsyncTextureReader supports only ARGB32 texture format");
            Type = ReadType.None;
            return;
        }

        Type = ReadType.Sync;
        BytesPerPixel = 3;
        Data = new NativeArray<T>(texture.width * texture.height * BytesPerPixel, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        ReadTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
    }

    public void Destroy()
    {
        if (Type == ReadType.LinuxOpenGL)
        {
            AsyncTextureReaderImports.AsyncTextureReaderDestroy(LinuxId);
            GL.IssuePluginEvent(LinuxUpdate, LinuxId);
            LinuxId = -1;
        }

        if (Data.IsCreated)
        {
            Data.Dispose();
        }
    }

    public void Start()
    {
        Debug.Assert(Status != AsyncTextureReaderStatus.Reading);

        if (Type == ReadType.None)
        {
            return;
        }
        else if (Type == ReadType.Native)
        {
            NativeReadRequest = AsyncGPUReadback.Request(Texture, 0, NativeReadFormat);
        }
        else if (Type == ReadType.LinuxOpenGL)
        {
            if (LinuxId >= 0)
            {
                unsafe
                {
                    AsyncTextureReaderImports.AsyncTextureReaderStart(LinuxId, new IntPtr(Data.GetUnsafePtr()));
                }
                GL.IssuePluginEvent(LinuxUpdate, LinuxId);
            }
        }

        Status = AsyncTextureReaderStatus.Reading;
    }

    public NativeArray<T> GetData()
    {
        if (Type != ReadType.None)
        {
            Debug.Assert(Status == AsyncTextureReaderStatus.Finished);
            Status = AsyncTextureReaderStatus.Idle;
        }
        return Data;
    }

    public void Reset()
    {
        if (Status == AsyncTextureReaderStatus.Reading)
        {
            if (Type == ReadType.Native)
            {
                NativeReadRequest.WaitForCompletion();
            }

            Status = AsyncTextureReaderStatus.Idle;
        }
    }

    public void Update()
    {
        if (Texture.IsCreated() == false)
        {
            // need to recreate native RenderTexture handle, because it is lost
            // this happens, for example, when you resize Unity Editor window on Linux
            if (Type == ReadType.Native)
            {
                NativeReadRequest.WaitForCompletion();
            }
            else if (Type == ReadType.LinuxOpenGL)
            {
                AsyncTextureReaderImports.AsyncTextureReaderDestroy(LinuxId);
                GL.IssuePluginEvent(LinuxUpdate, LinuxId);

                Texture.Create();
                LinuxId = AsyncTextureReaderImports.AsyncTextureReaderCreate(Texture.GetNativeTexturePtr(), Data.Length);
                GL.IssuePluginEvent(LinuxUpdate, LinuxId);
            }

            Status = AsyncTextureReaderStatus.Idle;
            return;
        }

        if (Status != AsyncTextureReaderStatus.Reading)
        {
            return;
        }

        if (Type == ReadType.Native)
        {
            if (NativeReadRequest.done)
            {
                if (NativeReadRequest.layerCount == 0)
                {
                    // start reading request was not issued yet
                    return;
                }
                // this will happen only if AsyncGPUReadback.Request was issued
                if (NativeReadRequest.hasError)
                {
                    return;
                }
                    
                Data.CopyFrom(NativeReadRequest.GetData<T>());
                Status = AsyncTextureReaderStatus.Finished;
            }
        }
        else if (Type == ReadType.LinuxOpenGL)
        {
            if (LinuxId >= 0)
            {
                Status = AsyncTextureReaderImports.AsyncTextureReaderGetStatus(LinuxId);
                if (Status != AsyncTextureReaderStatus.Finished)
                {
                    GL.IssuePluginEvent(LinuxUpdate, LinuxId);
                    Status = AsyncTextureReaderImports.AsyncTextureReaderGetStatus(LinuxId);
                }
            }
            else
            {
                Status = AsyncTextureReaderStatus.Finished;
            }
        }
        else if (Type == ReadType.Sync)
        {
            var current = RenderTexture.active;
            RenderTexture.active = Texture;
            ReadTexture.ReadPixels(new Rect(0, 0, Texture.width, Texture.height), 0, 0);
            ReadTexture.Apply();
            RenderTexture.active = current;

            int size = ReadTexture.width * ReadTexture.height * BytesPerPixel;
            byte[] bytes = ReadTexture.GetRawTextureData();
            unsafe
            {
                fixed (void* ptr = bytes)
                {
                    Buffer.MemoryCopy(ptr, Data.GetUnsafePtr(), size, size);
                }
            }
            Status = AsyncTextureReaderStatus.Finished;
        }
    }
    
    static void DebugCallback(string message)
    {
        UnityEngine.Debug.Log($"AsyncTextureReader: {message}");
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    delegate void DebugDelegate(string message);

    static DebugDelegate debug;
}
