using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DeviceState
{
    public int lX;              /* x-axis position              */
    public int lY;              /* y-axis position              */
    public int lZ;              /* z-axis position              */
    public int lRx;             /* x-axis rotation              */
    public int lRy;             /* y-axis rotation              */
    public int lRz;             /* z-axis rotation              */
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public int[] rglSlider;     /* extra axes positions         */
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public uint[] rgdwPOV;      /* POV directions               */
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public byte[] rgbButtons;   /* 32 buttons                   */
  
};

public class DirectInputWrapper {

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern long Init();

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern int DevicesCount();

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetProductName(int device);

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool HasForceFeedback(int device);

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Close();

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Update();

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool GetState(int device, IntPtr state);

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetNumEffects(int device);

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetEffectName(int device, int index);

    //params in range 0 - 10000
    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern long PlaySpringForce(int device, int offset, int saturation, int coefficient);

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool StopSpringForce(int device);

    //force -10000 - 10000
    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern long PlayDamperForce(int device, int damperAmount);

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool StopDamperForce(int device);

    //force -10000 - 10000
    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern long PlayConstantForce(int device, int force);

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern long UpdateConstantForce(int device, int force);

    [DllImport("DirectInputPlugin", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool StopConstantForce(int device);

    public static bool GetStateManaged(int device, out DeviceState state)
    {
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DeviceState)));
        if (!GetState(device, ptr))
        {
            //TODO: do something better here
            Debug.Log("GetState failed");
            state = new DeviceState();
            return false;
        }
        state = (DeviceState)Marshal.PtrToStructure(ptr, typeof(DeviceState));
        Marshal.FreeHGlobal(ptr);
        return true;
    }

    public static string GetProductNameManaged(int device)
    {
        var pName = DirectInputWrapper.GetProductName(device);
        return Marshal.PtrToStringUni(pName);
    }

    public static string GetEffectNameManaged(int device, int index)
    {
        var pName = DirectInputWrapper.GetEffectName(device, index);
        return Marshal.PtrToStringUni(pName);
    }

}
