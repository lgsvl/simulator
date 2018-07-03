/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class RfbClient : MonoBehaviour
{
    public string Address = "127.0.0.1";
    public ushort Port = 5901;

    public Action<Texture2D> OnTextureCreated;

    public bool IsConnected { get; private set; }

    int id = -1;
    bool IsCreated { get { return id >= 0; } }
    Texture2D Texture;

    void Start()
    {
        IsConnected = false;

        if (!IsValidPlatform)
        {
            throw new Exception("RfbClient is supported only on Windows D3D11 and Linux OpenGLCore");
        }

        if (debug == null)
        {
            debug = new DebugDelegate(Debug);
            RfbClientSetDebug(Marshal.GetFunctionPointerForDelegate(debug));

            onUpdate = RfbClientGetUpdateFunc();
        }

        id = RfbClientStart(Address, Port);
        if (id < 0)
        {
            throw new Exception("Failed to create VNC client");
        }
    }

    void Stop()
    {
        if (IsCreated)
        {
            RfbClientStop(id);
            GL.IssuePluginEvent(onUpdate, id);
            if (Texture != null)
            {
                Destroy(Texture);
                Texture = null;
            }

            OnTextureCreated(null);
            id = -1;
        }
    }

    void Update()
    {
        if (!IsCreated)
        {
            return;
        }

        GL.IssuePluginEvent(onUpdate, id);

        int width, height;
        IntPtr texture;
        IsConnected = RfbClientDoUpdate(id, out width, out height, out texture);

        if (texture != IntPtr.Zero)
        {
            if (Texture == null || width != Texture.width || height != Texture.height)
            {
                if (Texture != null)
                {
                    Destroy(Texture);
                }
                Texture = Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBA32, true, true, texture);
                OnTextureCreated?.Invoke(Texture);
            }
        }
    }
    
    void OnDestroy()
    {
        Stop();
    }

    void OnApplicationQuit()
    {
        Stop();
    }

    static bool IsValidPlatform
    {
        get
        {
            if (IntPtr.Size != 8)
            {
                return false;
            }

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows &&
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11)
            {
                return true;
            }

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux &&
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore)
            {
                return true;
            }

            return false;
        }
    }

    static void Debug(string message)
    {
        UnityEngine.Debug.Log("RfbClient: " + message);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    delegate void DebugDelegate(string message);

    static DebugDelegate debug;
    static IntPtr onUpdate;

    [DllImport("RfbClient")]
    static extern void RfbClientSetDebug(IntPtr ptr);

    [DllImport("RfbClient")]
    static extern int RfbClientStart([MarshalAs(UnmanagedType.LPStr)] string address, ushort port);

    [DllImport("RfbClient")]
    static extern void RfbClientStop(int id);

    [DllImport("RfbClient")]
    static extern IntPtr RfbClientGetUpdateFunc();

    [DllImport("RfbClient")]
    static extern bool RfbClientDoUpdate(int id, out int width, out int height, out IntPtr texture);
}
