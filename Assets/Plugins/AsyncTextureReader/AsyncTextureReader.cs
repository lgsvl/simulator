/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.Rendering;
#if !UNITY_2018_2_OR_NEWER
using UnityEngine.Experimental.Rendering;
#endif

public class AsyncTextureReader
{
    public enum ReadType
    {
        Sync,
        Native,
        LinuxOpenGL,
    }

    public enum ReadStatus
    {
        Idle = 0,
        Reading = 1,
        Finished = 2,
    }

    public ReadStatus Status { get; private set; }
    NativeArray<byte> Data;

    ReadType Type;
    AsyncGPUReadbackRequest NativeReadRequest;

    public TextureFormat ReadFormat { get; private set; }
    public int BytesPerPixel { get; private set; }

    RenderTexture Texture;
    Texture2D ReadTexture;

    int LinuxId;
    System.IntPtr LinuxUpdate;

    public AsyncTextureReader(RenderTexture texture)
    {
        Status = ReadStatus.Idle;
        Texture = texture;

        var sync = System.Environment.GetEnvironmentVariable("FORCE_SYNC_GPU_READBACK");
        if (sync == null)
        {
            if (SystemInfo.supportsAsyncGPUReadback)
            {
                Type = ReadType.Native;
                ReadFormat = TextureFormat.RGB24;
                BytesPerPixel = 3;
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
                Debug.Log($"OpenGL version = {major}.{minor}");

                if (major > 3 || major == 3 && minor >= 2) // GL_ARB_sync
                {
                    debug = new DebugDelegate(DebugCallback);
                    AsyncTextureReaderSetDebug(Marshal.GetFunctionPointerForDelegate(debug));

                    Debug.Assert(texture.dimension == TextureDimension.Tex2D);
                    Debug.Assert(texture.format == RenderTextureFormat.ARGB32);
                    LinuxId = AsyncTextureReaderCreate(texture.GetNativeTexturePtr(), texture.width, texture.height);
                    if (LinuxId >= 0)
                    {
                        LinuxUpdate = AsyncTextureReaderGetUpdate();
                        GL.IssuePluginEvent(LinuxUpdate, LinuxId);

                        Type = ReadType.LinuxOpenGL;
                        ReadFormat = TextureFormat.RGBA32;
                        BytesPerPixel = 4;
                        return;
                    }
                }
            }
        }

        Type = ReadType.Sync;
        ReadFormat = TextureFormat.RGB24;
        BytesPerPixel = 3;
        ReadTexture = new Texture2D(texture.width, texture.height, ReadFormat, false);
    }

    public void Destroy()
    {
        if (Type == ReadType.LinuxOpenGL)
        {
            AsyncTextureReaderDestroy(LinuxId);
            GL.IssuePluginEvent(LinuxUpdate, LinuxId);
            LinuxId = -1;
            if (Data.IsCreated)
            {
                Data.Dispose();
            }
        }
    }

    public void Start()
    {
        Debug.Assert(Status != ReadStatus.Reading);
        Status = ReadStatus.Reading;

        if (Type == ReadType.Native)
        {
            NativeReadRequest = AsyncGPUReadback.Request(Texture, 0, ReadFormat);
        }
        else if (Type == ReadType.LinuxOpenGL)
        {
            Data = new NativeArray<byte>(Texture.width * Texture.height * BytesPerPixel, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            unsafe
            {
                var ptr = new System.IntPtr(NativeArrayUnsafeUtility.GetUnsafePtr(Data));
                AsyncTextureReaderStart(LinuxId, ptr);
                GL.IssuePluginEvent(LinuxUpdate, LinuxId);
            }
        }
    }

    public NativeArray<byte> GetData()
    {
        Debug.Assert(Status == ReadStatus.Finished);
        Status = ReadStatus.Idle;
        return Data;
    }

    public void Update()
    {
        if (Status != ReadStatus.Reading)
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

                Data = new NativeArray<byte>(NativeReadRequest.GetData<byte>(), Allocator.Persistent);
                Status = ReadStatus.Finished;
            }
        }
        else if (Type == ReadType.LinuxOpenGL)
        {
            Status = (ReadStatus)AsyncTextureReaderGetStatus(LinuxId);
            if (Status != ReadStatus.Finished)
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

#if UNITY_2018_2_OR_NEWER
            Data = new NativeArray<byte>(ReadTexture.GetRawTextureData<byte>(), Allocator.Persistent);
#else
            Data = new NativeArray<byte>(ReadTexture.GetRawTextureData(), Allocator.Persistent);
#endif
            Status = ReadStatus.Finished;
        }
    }
    
    static void DebugCallback(string message)
    {
        UnityEngine.Debug.Log($"AsyncTextureReader: {message}");
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    delegate void DebugDelegate(string message);

    static DebugDelegate debug;

    [DllImport("AsyncTextureReader", CallingConvention=CallingConvention.Cdecl)]
    static extern void AsyncTextureReaderSetDebug(System.IntPtr ptr);

    [DllImport("AsyncTextureReader", CallingConvention = CallingConvention.Cdecl)]
    static extern int AsyncTextureReaderCreate(System.IntPtr texture, int width, int height);

    [DllImport("AsyncTextureReader", CallingConvention = CallingConvention.Cdecl)]
    static extern void AsyncTextureReaderDestroy(int id);

    [DllImport("AsyncTextureReader", CallingConvention = CallingConvention.Cdecl)]
    static extern void AsyncTextureReaderStart(int id, System.IntPtr data);

    [DllImport("AsyncTextureReader", CallingConvention = CallingConvention.Cdecl)]
    static extern int AsyncTextureReaderGetStatus(int id);

    [DllImport("AsyncTextureReader", CallingConvention = CallingConvention.Cdecl)]
    static extern System.IntPtr AsyncTextureReaderGetUpdate();
}
