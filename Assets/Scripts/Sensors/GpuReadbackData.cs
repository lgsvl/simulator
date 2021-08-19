/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors
{
    using System;
    using Unity.Collections;
    using UnityEngine.Rendering;

    public class GpuReadbackData<T> : IDisposable where T : struct
    {
        public NativeArray<T> gpuData;
        public AsyncGPUReadbackRequest request;
        public double captureTime;

        public void Init(int nativeArraySize)
        {
            gpuData = new NativeArray<T>(nativeArraySize, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (gpuData.IsCreated)
                gpuData.Dispose();
        }
    }
}