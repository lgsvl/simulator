/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Simulator.Utilities
{
    public static class WindowFlasher
    {
        private static IntPtr unityWindowHandle  = IntPtr.Zero;

        private const string UnityWindowClassName = "UnityWndClass";

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumThreadWindows(uint dwThreadId, IntPtr lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hWnd, bool binveted);

        public static void Flash()
        {
            if (UnityEngine.SystemInfo.operatingSystemFamily != UnityEngine.OperatingSystemFamily.Windows)
            {
                return;
            }

            if (unityWindowHandle == IntPtr.Zero)
            {
                EnumWindowsProc cb = (hWnd, lParam) =>
                {
                    var classText = new StringBuilder(UnityWindowClassName.Length + 1);
                    GetClassName(hWnd, classText, classText.Capacity);
                    if (classText.ToString() == UnityWindowClassName)
                    {
                        unityWindowHandle = hWnd;
                        return false;
                    }
                    return true;
                };

                uint threadId = GetCurrentThreadId();
                EnumThreadWindows(threadId, Marshal.GetFunctionPointerForDelegate(cb), IntPtr.Zero);
            }

            FlashWindow(unityWindowHandle, true);
        }
    }
}