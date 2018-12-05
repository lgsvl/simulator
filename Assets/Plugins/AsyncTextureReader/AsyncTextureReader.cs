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
    public static extern int AsyncTextureReaderCreate(System.IntPtr texture, int width, int height);

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

    public TextureFormat ReadFormat { get; private set; }
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
                Type = ReadType.Native;
                if (texture.format == RenderTextureFormat.ARGBFloat)
                {
                    BytesPerPixel = 16;
                    ReadFormat = TextureFormat.RGBAFloat;
                }
                else if (texture.format == RenderTextureFormat.RGFloat)
                {
                    BytesPerPixel = 8;
                    ReadFormat = TextureFormat.RGFloat;
                }
                else if (texture.format == RenderTextureFormat.RFloat)
                {
                    BytesPerPixel = 4;
                    ReadFormat = TextureFormat.RFloat;
                }
                else // if (texture.format == RenderTextureFormat.ARGB32)
                {
                    BytesPerPixel = 3;
                    ReadFormat = TextureFormat.RGB24;
                }
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

                    if (texture.format == RenderTextureFormat.ARGBFloat)
                    {
                        BytesPerPixel = 16;
                        ReadFormat = TextureFormat.RGBAFloat;
                    }
                    else if (texture.format == RenderTextureFormat.RGFloat)
                    {
                        BytesPerPixel = 8;
                        ReadFormat = TextureFormat.RGFloat;
                    }
                    else if (texture.format == RenderTextureFormat.RFloat)
                    {
                        BytesPerPixel = 4;
                        ReadFormat = TextureFormat.RFloat;
                    }
                    else // if (texture.format == RenderTextureFormat.ARGB32)
                    {
                        BytesPerPixel = 4;
                        ReadFormat = TextureFormat.RGBA32;
                    }

                    Data = new NativeArray<T>(Texture.width * Texture.height * BytesPerPixel, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    LinuxId = AsyncTextureReaderImports.AsyncTextureReaderCreate(texture.GetNativeTexturePtr(), texture.width, texture.height);
                    if (LinuxId < 0)
                    {
                        // Debug.Log("Failed to create AsyncTextureReader");
                        Type = ReadType.None;
                        return;
                    }
                    else
                    {
                        LinuxUpdate = AsyncTextureReaderImports.AsyncTextureReaderGetUpdate();
                        GL.IssuePluginEvent(LinuxUpdate, LinuxId);

                        Type = ReadType.LinuxOpenGL;
                        return;
                    }

                }
            }
        }

        if (texture.format != RenderTextureFormat.ARGB32)
        {
            Type = ReadType.None;
            return;
        }

        BytesPerPixel = 3;
        ReadFormat = TextureFormat.RGB24;

        Type = ReadType.Sync;
        Data = new NativeArray<T>(texture.width * texture.height * BytesPerPixel, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        ReadTexture = new Texture2D(texture.width, texture.height, ReadFormat, false);
    }

    public void Destroy()
    {
        if (Type == ReadType.LinuxOpenGL)
        {
            AsyncTextureReaderImports.AsyncTextureReaderDestroy(LinuxId);
            GL.IssuePluginEvent(LinuxUpdate, LinuxId);
            LinuxId = -1;
            if (Data.IsCreated)
            {
                Data.Dispose();
            }
        }
        else if (Type == ReadType.Sync)
        {
            if (Data.IsCreated)
            {
                Data.Dispose();
            }
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
            NativeReadRequest = AsyncGPUReadback.Request(Texture, 0, ReadFormat);
        }
        else if (Type == ReadType.LinuxOpenGL)
        {
            unsafe
            {
                AsyncTextureReaderImports.AsyncTextureReaderStart(LinuxId, new IntPtr(Data.GetUnsafePtr()));
            }
            GL.IssuePluginEvent(LinuxUpdate, LinuxId);
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

    public void Update()
    {
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

                Data = NativeReadRequest.GetData<T>();
                Status = AsyncTextureReaderStatus.Finished;
            }
        }
        else if (Type == ReadType.LinuxOpenGL)
        {
            Status = AsyncTextureReaderImports.AsyncTextureReaderGetStatus(LinuxId);
            if (Status != AsyncTextureReaderStatus.Finished)
            {
                GL.IssuePluginEvent(LinuxUpdate, LinuxId);
            }
        }
        else if (Type == ReadType.Sync)
        {
            RenderTexture current = RenderTexture.active;
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
