/* Copyright (c) 2018, Dassault Systemes All rights reserved.
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
 * Neither the name of the copyright holder nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING,
 * BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
 * SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using UnityEngine;
using System;
using System.Security;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

namespace Simulator.FMU
{
    [Serializable]
    public class FMUData
    {
        public string Name;
        public string Version;
        public string GUID;
        public string modelName;
        public FMIType type;
        public List<ScalarVariable> modelVariables;
        public string Path;
    }

    [Serializable]
    public enum FMIType
    {
        ModelExchange,
        CoSimulation
    };

    public enum VariableType
    {
        Real,
        Integer,
        Enumeration,
        Boolean,
        String
    };

    [Serializable]
    public class ScalarVariable
    {
        public string name;
        public uint valueReference;
        public string description;
        public string causality;
        public string variability;
        public string initial;
        public VariableType type;
        public string start;
    }

    public class FMU : IDisposable
    {
        public enum ModelType
        {
            ModelExchange,
            ModelCoSimulation
        };

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void fmi2CallbackLogger(IntPtr componentEnvironment, IntPtr instanceName, int status, IntPtr category, IntPtr message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate IntPtr fmi2CallbackAllocateMemory(ulong a, ulong b);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void fmi2CallbackFreeMemory(IntPtr mem);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public delegate void fmi2StepFinished(IntPtr mem, int status);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate IntPtr fmi2InstantiateDelegate(string instanceName, int fmuType, string fmuGUID, string resourceLocation, IntPtr callbacks, bool visible, bool loggingOn);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate void fmi2FreeInstanceDelegate(IntPtr c);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2SetupExperimentDelegate(IntPtr c, bool toleranceDefined, double tolerance, double startTime, bool stopTimeDefined, double stopTime);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2EnterInitializationModeDelegate(IntPtr c);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2ExitInitializationModeDelegate(IntPtr c);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2TerminateDelegate(IntPtr c);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2ResetDelegate(IntPtr c);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2DoStepDelegate(IntPtr c, double currentCommunicationPoint, double communicationStepSize, bool noSetFMUStatePriorToCurrentPoint);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private unsafe delegate int fmi2GetRealDelegate(IntPtr c, int[] vr, ulong nvr, double[] value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2SetRealDelegate(IntPtr c, int[] vr, ulong nvr, double[] value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2GetIntegerDelegate(IntPtr c, int[] vr, ulong nvr, int[] value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2SetIntegerDelegate(IntPtr c, int[] vr, ulong nvr, int[] value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2GetBooleanDelegate(IntPtr c, int[] vr, ulong nvr, bool[] value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2SetBooleanDelegate(IntPtr c, int[] vr, ulong nvr, bool[] value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2GetStringDelegate(IntPtr c, int[] vr, ulong nvr, IntPtr[] value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private delegate int fmi2SetStringDelegate(IntPtr c, int[] vr, ulong nvr, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] value);

        [StructLayout(LayoutKind.Sequential)]
        public struct fmi2CallbackFunctions
        {
            public fmi2CallbackLogger logger;
            public fmi2CallbackAllocateMemory allocateMemory;
            public fmi2CallbackFreeMemory freeMemory;
            public fmi2StepFinished stepFinished;
            public IntPtr componentEnvironment;
        }

        private IntPtr dll;
        private IntPtr Component;
        private IntPtr Callbacks;
        private string DLLGUID;

        private fmi2InstantiateDelegate FMI2Instantiate;
        private fmi2FreeInstanceDelegate FMI2FreeInstance;
        private fmi2SetupExperimentDelegate FMI2SetupExperiment;
        private fmi2EnterInitializationModeDelegate FMI2EnterInitializationMode;
        private fmi2ExitInitializationModeDelegate FMI2ExitInitializationMode;
        private fmi2TerminateDelegate FMI2Terminate;
        private fmi2ResetDelegate FMI2Reset;
        private fmi2DoStepDelegate FMI2DoStep;
        private fmi2GetRealDelegate FMI2GetReal;
        private fmi2SetRealDelegate FMI2SetReal;
        private fmi2GetIntegerDelegate FMI2GetInteger;
        private fmi2SetIntegerDelegate FMI2SetInteger;
        private fmi2GetBooleanDelegate FMI2GetBoolean;
        private fmi2SetBooleanDelegate FMI2SetBoolean;
        private fmi2GetStringDelegate FMI2GetString;
        private fmi2SetStringDelegate FMI2SetString;

        private Dictionary<string, uint> ValueReferences;

        public FMU(string fmuName, string instanceName, string GUID = null, Dictionary<string, uint> vars = null, string path = "", bool loggingOn = false)
        {
            if (GUID == null || vars == null)
                return;

            ValueReferences = vars;

            var dllPath = path; // TODO test if linux checks are even needed

            var modelIdentifier = fmuName;
            DLLGUID = GUID;

            Web.Config.FMUs.TryGetValue(GUID, out dll);
            if (dll == default)
            {
                // load the DLL
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                //dllPath += "/win" + IntPtr.Size * 8 + "/" + modelIdentifier + ".dll";
                dll = LoadLibrary(dllPath);
#else
                //dllPath += "linux64/" + modelIdentifier + ".so"; //.dylib
                dll = dlopen(dllPath, 4);
#endif
                Web.Config.FMUs.Add(GUID, dll);
            }

            fmi2CallbackFunctions functions;
            functions.logger = new fmi2CallbackLogger(LogMessage); // no log message
            functions.allocateMemory = new fmi2CallbackAllocateMemory(AllocateMemory);
            functions.freeMemory = new fmi2CallbackFreeMemory(FreeMemory);
            functions.stepFinished = new fmi2StepFinished(StepFinished);
            functions.componentEnvironment = IntPtr.Zero;

            //unsafe
            //{
            this.Callbacks = Marshal.AllocHGlobal(Marshal.SizeOf(functions));
            //Callbacks = (IntPtr) UnsafeUtility.Malloc(UnsafeUtility.SizeOf<fmi2CallbackFunctions>(), 8, Unity.Collections.Allocator.Persistent);
            Marshal.StructureToPtr(functions, this.Callbacks, false);
            //UnsafeUtility.CopyStructureToPtr(ref functions, Callbacks.ToPointer()); // Copy the struct to unmanaged memory.
            //}


            FMI2Instantiate = GetFunc<fmi2InstantiateDelegate>(dll, "fmi2Instantiate");
            FMI2FreeInstance = GetFunc<fmi2FreeInstanceDelegate>(dll, "fmi2FreeInstance");
            FMI2SetupExperiment = GetFunc<fmi2SetupExperimentDelegate>(dll, "fmi2SetupExperiment");
            FMI2EnterInitializationMode = GetFunc<fmi2EnterInitializationModeDelegate>(dll, "fmi2EnterInitializationMode");
            FMI2ExitInitializationMode = GetFunc<fmi2ExitInitializationModeDelegate>(dll, "fmi2ExitInitializationMode");
            FMI2Terminate = GetFunc<fmi2TerminateDelegate>(dll, "fmi2Terminate");
            FMI2Reset = GetFunc<fmi2ResetDelegate>(dll, "fmi2Reset");
            FMI2DoStep = GetFunc<fmi2DoStepDelegate>(dll, "fmi2DoStep");
            FMI2GetReal = GetFunc<fmi2GetRealDelegate>(dll, "fmi2GetReal");
            FMI2SetReal = GetFunc<fmi2SetRealDelegate>(dll, "fmi2SetReal");
            FMI2GetInteger = GetFunc<fmi2GetIntegerDelegate>(dll, "fmi2GetInteger");
            FMI2SetInteger = GetFunc<fmi2SetIntegerDelegate>(dll, "fmi2SetInteger");
            FMI2GetBoolean = GetFunc<fmi2GetBooleanDelegate>(dll, "fmi2GetBoolean");
            FMI2SetBoolean = GetFunc<fmi2SetBooleanDelegate>(dll, "fmi2SetBoolean");
            FMI2GetString = GetFunc<fmi2GetStringDelegate>(dll, "fmi2GetString");
            FMI2SetString = GetFunc<fmi2SetStringDelegate>(dll, "fmi2SetString");

            //var resourceLocation = new Uri(unzipdir).AbsoluteUri;

            Component = FMI2Instantiate(instanceName, (int)ModelType.ModelCoSimulation, DLLGUID, null, this.Callbacks, false, loggingOn ? true : false);
        }

        public uint GetValueReference(string variable)
        {
            return ValueReferences[variable];
        }

        public void Dispose()
        {
            FMI2FreeInstance(Component);
            //unsafe
            //{
            Marshal.FreeHGlobal(Callbacks);
            //UnsafeUtility.Free(Callbacks.ToPointer(), Unity.Collections.Allocator.Persistent);
            //}

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            FreeLibrary(dll);
#else
		    dlclose(dll);
#endif
            if (Web.Config.FMUs.TryGetValue(DLLGUID, out dll))
            {
                Web.Config.FMUs.Remove(DLLGUID);
            }
        }

        public void SetupExperiment(double startTime, double? tolerance = null, double? stopTime = null)
        {
            var status = FMI2SetupExperiment(Component,
                         tolerance.HasValue ? true : false,
                         tolerance.HasValue ? tolerance.Value : 0.0,
                         startTime,
                         stopTime.HasValue ? true : false,
                         stopTime.HasValue ? stopTime.Value : 0.0);
        }

        public void EnterInitializationMode()
        {
            var status = FMI2EnterInitializationMode(Component);
        }

        public void ExitInitializationMode()
        {
            var status = FMI2ExitInitializationMode(Component);
        }

        public void Terminate()
        {
            var status = FMI2Terminate(Component);
        }

        public void Reset()
        {
            var status = FMI2Reset(Component);
        }

        public void FreeInstance()
        {
            FMI2FreeInstance(Component);
        }

        public void DoStep(double currentCommunicationPoint, double communicationStepSize, bool noSetFMUStatePriorToCurrentPoint = true)
        {
            var status = FMI2DoStep(Component, currentCommunicationPoint, communicationStepSize, noSetFMUStatePriorToCurrentPoint ? true : false);
        }

        public double GetReal(uint vr)
        {
            int[] vrs = { (int)vr };
            double[] value = { 0.0 };
            var status = FMI2GetReal(Component, vrs, 1, value);
            return value[0];
            //unsafe
            //{
            //    double value;
            //    var status = FMI2GetReal(Component, (int*)&vr, 1, &value);
            //    return value;
            //}
        }

        public double GetReal(string name)
        {
            var vr = ValueReferences[name];
            return GetReal(vr);
        }

        //public void GetRealValues(uint[] ids, double[] values) // length
        //{
        //    unsafe
        //    {
        //        fixed (uint* iptr = ids)
        //        fixed (double* optr = values)
        //        {
        //            var status = FMI2GetReal(Component, (int*)iptr, (ulong)ids.Length, optr);
        //            // TODO: log status when error?
        //        }
        //    }
        //}

        public void SetReal(uint vr, double value)
        {
            int[] vrs = { (int)vr };
            double[] value_ = { value };
            var status = FMI2SetReal(Component, vrs, 1, value_);
        }

        public void SetReal(string name, double value)
        {
            var vr = ValueReferences[name];
            SetReal(vr, value);
        }

        public int GetInteger(uint vr)
        {
            int[] vrs = { (int)vr };
            int[] value = { 0 };
            var status = FMI2GetInteger(Component, vrs, 1, value);
            return value[0];
        }

        public int GetInteger(string name)
        {
            var vr = ValueReferences[name];
            return GetInteger(vr);
        }

        public void SetInteger(uint vr, int value)
        {
            int[] vrs = { (int)vr };
            int[] value_ = { value };
            var status = FMI2SetInteger(Component, vrs, 1, value_);
        }

        public void SetInteger(string name, int value)
        {
            var vr = ValueReferences[name];
            SetInteger(vr, value);
        }

        public bool GetBoolean(uint vr)
        {
            int[] vrs = { (int)vr };
            bool[] value = { false };
            var status = FMI2GetBoolean(Component, vrs, 1, value);
            return value[0] != false;
        }

        public bool GetBoolean(string name)
        {
            var vr = ValueReferences[name];
            return GetBoolean(vr);
        }

        public void SetBoolean(uint vr, bool value)
        {
            int[] vrs = { (int)vr };
            bool[] value_ = { value ? true : false };
            var status = FMI2SetBoolean(Component, vrs, 1, value_);
        }

        public void SetBoolean(string name, bool value)
        {
            var vr = ValueReferences[name];
            SetBoolean(vr, value);
        }

        public string GetString(uint vr)
        {
            int[] vrs = { (int)vr };
            IntPtr[] value = { IntPtr.Zero };
            var status = FMI2GetString(Component, vrs, 1, value);
            var str = Marshal.PtrToStringAnsi(value[0]);
            return str;
        }

        public string GetString(string name)
        {
            var vr = ValueReferences[name];
            return GetString(vr);
        }

        public void SetString(uint vr, string value)
        {
            int[] vrs = { (int)vr };
            string[] value_ = { value };
            var status = FMI2SetString(Component, vrs, 1, value_);
        }

        public void SetString(string name, string value)
        {
            var vr = ValueReferences[name];
            SetString(vr, value);
        }

        private static IntPtr AllocateMemory(ulong nobj, ulong size)
        {
            //unsafe
            //{
            //    long count = (long)(nobj * size);
            //    var mem = UnsafeUtility.Malloc(count, 16, Unity.Collections.Allocator.Persistent);
            //    UnsafeUtility.MemClear(mem, count);
            //    return (IntPtr) mem;
            //}

            var nbytes = (int)(nobj * size);
            // allocate the memory
            var mem = Marshal.AllocHGlobal(nbytes);

            var zero = new byte[nbytes];
            // set all bytes to 0
            Marshal.Copy(zero, 0, mem, nbytes);

            return mem;
        }

        private static void FreeMemory(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);
#else
        [DllImport("libdl")]
        protected static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl")]
        protected static extern int dlclose(IntPtr handle);

        [DllImport("libdl")]
        private static extern IntPtr dlsym(IntPtr handle, String symbol);
#endif

        private static void StepFinished(IntPtr mem, int status)
        {
            //
        }

        private static void LogMessage(IntPtr env, IntPtr instanceName, int status, IntPtr category, IntPtr message)
        {
            Debug.Log(Marshal.PtrToStringAnsi(message) + ": " + Marshal.PtrToStringAnsi(message));
        }

        private TDelegate GetFunc<TDelegate>(IntPtr dll, string fname) where TDelegate : class
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            IntPtr p = GetProcAddress(dll, fname);
#else
            IntPtr p = dlsym(dll, fname);
#endif

            if (p == IntPtr.Zero)
            {
                return null;
            }

            return Marshal.GetDelegateForFunctionPointer<TDelegate>(p);
        }
    }
}
