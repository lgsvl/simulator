/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Sensors
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class GpuReadbackPool<TData, T> : IDisposable where TData : GpuReadbackData<T>, new() where T : struct
    {
        private enum ProcessingState
        {
            Processing,
            Processed,
            Failed
        }

        private class QueueItem
        {
            public TData data;
            public ProcessingState state;
        }

        private readonly Stack<TData> pool = new Stack<TData>();
        private readonly LinkedList<QueueItem> processingQueue = new LinkedList<QueueItem>();

        private int arraySize;
        private Action<TData> onCompleteDelegate;

        public void Initialize(int nativeArraySize, Action<TData> onComplete)
        {
            arraySize = nativeArraySize;
            onCompleteDelegate = onComplete;
            pool.Push(CreateNewElement());
        }

        public void Resize(int nativeArraySize)
        {
            if (nativeArraySize == arraySize)
                return;

            DisposeAll();
            Initialize(nativeArraySize, onCompleteDelegate);
        }

        private TData GetFromPool()
        {
            return pool.Count > 0 ? pool.Pop() : CreateNewElement();
        }

        private TData CreateNewElement()
        {
            var item = new TData();
            item.Init(arraySize);
            return item;
        }

        public TData StartReadback(Texture src)
        {
            var data = GetFromPool();
            var req = AsyncGPUReadback.RequestIntoNativeArray(ref data.gpuData, src);
            data.request = req;
            data.captureTime = SimulatorManager.Instance.CurrentTime;
            processingQueue.AddLast(new QueueItem {data = data});
            return data;
        }

        public TData StartReadback(Texture src, int mipIndex, TextureFormat dstFormat)
        {
            var data = GetFromPool();
            var req = AsyncGPUReadback.RequestIntoNativeArray(ref data.gpuData, src, mipIndex, dstFormat);
            data.request = req;
            data.captureTime = SimulatorManager.Instance.CurrentTime;
            processingQueue.AddLast(new QueueItem {data = data});
            return data;
        }

        public TData StartReadback(ComputeBuffer src)
        {
            var data = GetFromPool();
            var req = AsyncGPUReadback.RequestIntoNativeArray(ref data.gpuData, src);
            data.request = req;
            data.captureTime = SimulatorManager.Instance.CurrentTime;
            processingQueue.AddLast(new QueueItem {data = data});
            return data;
        }

        public TData StartReadback(ComputeBuffer src, int size, int offset)
        {
            var data = GetFromPool();
            var req = AsyncGPUReadback.RequestIntoNativeArray(ref data.gpuData, src, size, offset);
            data.request = req;
            data.captureTime = SimulatorManager.Instance.CurrentTime;
            processingQueue.AddLast(new QueueItem {data = data});
            return data;
        }

        public void Process()
        {
            foreach (var item in processingQueue)
            {
                if (item.state != ProcessingState.Processing)
                    continue;

                if (!item.data.request.done)
                    continue;

                if (item.data.request.hasError)
                {
                    Debug.LogError("GPU Readback request failed.");
                    item.state = ProcessingState.Failed;
                    continue;
                }

                item.state = ProcessingState.Processed;
            }

            while (processingQueue.Count > 0 && processingQueue.First.Value.state != ProcessingState.Processing)
            {
                var item = processingQueue.First.Value;
                if (item.state == ProcessingState.Processed)
                    onCompleteDelegate(item.data);

                pool.Push(item.data);
                processingQueue.RemoveFirst();
            }
        }

        public void Dispose()
        {
            DisposeAll();
        }

        private void DisposeAll()
        {
            foreach (var item in pool)
                item.Dispose();

            foreach (var item in processingQueue)
            {
                item.data.request.WaitForCompletion();
                item.data.Dispose();
            }

            pool.Clear();
            processingQueue.Clear();
        }
    }
}