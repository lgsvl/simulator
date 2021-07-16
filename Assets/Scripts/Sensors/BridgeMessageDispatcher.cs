namespace Simulator.Sensors
{
    using System.Threading;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Bridge;
    using Bridge.Data;
    using Network.Core.Threading;
    using UnityEngine;

    public class BridgeMessageDispatcher : IDisposable
    {
        private interface IPoolDebugDataProvider
        {
            string LogData();
        }

        private class ConcurrentPool<T> : IPoolDebugDataProvider where T : class, new()
        {
            private readonly Dictionary<int, ConcurrentStack<T>> subStacks = new Dictionary<int, ConcurrentStack<T>>();

            private int poolSize;

            public T Get(int hash)
            {
                ConcurrentStack<T> stack;

                if (!subStacks.TryGetValue(hash, out stack))
                    stack = subStacks[hash] = new ConcurrentStack<T>();

                if (stack.TryPop(out var element))
                    return element;

                poolSize++;
                return new T();
            }

            public void Release(T element, int hash)
            {
                subStacks[hash].Push(element);
            }

            public string LogData()
            {
                return $"Type: {typeof(T).Name}, pool size: {poolSize}, sub-pools: {subStacks.Count}";
            }
        }

        private class Request
        {
            public Action task;
            public Action<bool> externalCallback;
            public Action internalCallback;
            public object exclusiveToken;
        }

        /// <summary>
        /// Returns currently active instance of thread pool, managed by <see cref="SimulatorManager"/>.
        /// </summary>
        public static BridgeMessageDispatcher Instance => SimulatorManager.Instance.BridgeMessageDispatcher;

        private readonly LinkedList<Thread> workerThreads = new LinkedList<Thread>();
        private readonly LinkedList<Request> requests = new LinkedList<Request>();
        private readonly LinkedList<object> activeTokens = new LinkedList<object>();
        private readonly Dictionary<Type, object> pools = new Dictionary<Type, object>();

        private bool disposePending;
        private bool disposed;
        private bool performanceWarningDisplayed;

        private readonly int maxWorkerCount;
        private readonly int maxQueueSize;
        private int workerCount;
        private int idleCycles;

        public BridgeMessageDispatcher()
        {
            const int initialWorkerCount = 1;

            maxWorkerCount = SystemInfo.processorCount;
            maxQueueSize = Mathf.Max(maxWorkerCount, 32);

            InitCachePools();

            for (var i = 0; i < initialWorkerCount; ++i)
                SpawnNewWorker();
        }

        private void InitCachePools()
        {
            var interfaceType = typeof(IThreadCachedBridgeData<>);
            var poolType = typeof(ConcurrentPool<>);

            var cachedTypes = interfaceType.Assembly.GetTypes()
                .Where(x => x.GetInterfaces()
                    .Any(y => y.IsGenericType && y.GetGenericTypeDefinition() == typeof(IThreadCachedBridgeData<>)))
                .ToArray();

            foreach (var type in cachedTypes)
            {
                var poolGenericType = poolType.MakeGenericType(type);
                var poolInstance = Activator.CreateInstance(poolGenericType);
                pools.Add(type, poolInstance);
            }
        }

        private void SpawnNewWorker()
        {
            var worker = new Thread(Worker) {Name = string.Concat("Bridge Message Worker ", workerThreads.Count)};
            worker.Start();
            workerThreads.AddLast(worker);
            workerCount++;
        }

        private void KillWorker()
        {
            var worker = workerThreads.First.Value;
            workerThreads.RemoveFirst();
            worker.Abort();
            workerCount--;
        }

        private string GetDebugData()
        {
            lock (requests)
            {
                var sb = new StringBuilder();
                sb.Append($"Total workers: {workerCount} idle workers: {workerThreads.Count}, queue size: {requests.Count}\n");
                sb.Append($"Pooled types ({pools.Count}):\n");

                foreach (var pool in pools)
                {
                    if (pool.Value is IPoolDebugDataProvider logProvider)
                    {
                        sb.Append(logProvider.LogData());
                        sb.Append("\n");
                    }
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// <para>Creates and returns subscriber delegate that can be subscribed to bridge instance.</para>
        /// <para>Provided function will be executed synchronously on calling thread.</para> 
        /// </summary>
        /// <param name="func">Function to execute when data is received.</param>
        /// <typeparam name="T">Type of data used by the sensor.</typeparam>
        public Subscriber<T> GetSynchronousSubscriber<T>(Action<T> func)
        {
            return data =>
            {
                if (Mathf.Approximately(ThreadingUtilities.LastTimeScale, 0f))
                    return;

                func?.Invoke(data);
            };
        }

        /// <summary>
        /// <para>Creates and returns subscriber delegate that can be subscribed to bridge instance.</para>
        /// <para>Provided function will be executed asynchronously on one of worker threads.</para> 
        /// </summary>
        /// <param name="func">Function to execute when data is received.</param>
        /// <param name="exclusiveToken">
        /// <para>Optional token - only one task using it can be queued at a time.</para>
        /// <para>If this is provided, all subsequent requests with the same token will be dropped until delegate execution is finished.</para>
        /// </param>
        /// <typeparam name="T">Type of data used by the sensor.</typeparam>
        public Subscriber<T> GetAsynchronousSubscriber<T>(Action<T> func, object exclusiveToken = null)
        {
            return data =>
            {
                if (Mathf.Approximately(ThreadingUtilities.LastTimeScale, 0f))
                    return;

                lock (requests)
                {
                    if (disposePending)
                        throw new InvalidOperationException("This thread pool is being disposed - queueing new tasks is not allowed.");

                    if (disposed)
                        throw new ObjectDisposedException("This thread pool has been disposed - queueing new tasks is not allowed.");

                    if (exclusiveToken != null)
                    {
                        // Request with this token is being processed - drop message
                        if (activeTokens.Contains(exclusiveToken))
                            return;

                        activeTokens.AddLast(exclusiveToken);
                    }

                    QueueTask(() => func?.Invoke(data), null, null, exclusiveToken);
                }
            };
        }

        /// <summary>
        /// Attempts to queue bridge publishing task.
        /// </summary>
        /// <param name="publisher">Publisher delegate to use.</param>
        /// <param name="data">Data to send.</param>
        /// <param name="exclusiveToken">
        /// <para>Optional token - only one task using it can be queued at a time.</para>
        /// <para>If this is provided, all subsequent requests with the same token will be dropped until message is sent.</para>
        /// </param>
        /// <typeparam name="T">Data type used by the publisher.</typeparam>
        /// <returns>True if message was queued, false if it was dropped. Message can be dropped when Simulator is paused or if <see cref="exclusiveToken"/> is in use.</returns>
        public bool TryQueueTask<T>(Publisher<T> publisher, T data, object exclusiveToken) where T : class, new()
        {
            return TryQueueTask(publisher, data, null, exclusiveToken);
        }

        /// <summary>
        /// Attempts to queue bridge publishing task.
        /// </summary>
        /// <param name="publisher">Publisher delegate to use.</param>
        /// <param name="data">Data to send.</param>
        /// <param name="callback">Called when message is either sent or dropped. Argument is set to true if sending was success, false if it was dropped or exception happened.</param>
        /// <param name="exclusiveToken">
        /// <para>Optional token - only one task using it can be queued at a time.</para>
        /// <para>If this is provided, all subsequent requests with the same token will be dropped until message is sent.</para>
        /// </param>
        /// <typeparam name="T">Data type used by the publisher.</typeparam>
        /// <returns>True if message was queued, false if it was dropped. Message can be dropped when Simulator is paused or if <see cref="exclusiveToken"/> is in use.</returns>
        public bool TryQueueTask<T>(Publisher<T> publisher, T data, Action<bool> callback = null, object exclusiveToken = null) where T : class, new()
        {
            // Simulator is paused - drop message
            if (Mathf.Approximately(ThreadingUtilities.LastTimeScale, 0f))
            {
                callback?.Invoke(false);
                return false;
            }

            lock (requests)
            {
                if (disposePending)
                    throw new InvalidOperationException("This thread pool is being disposed - queueing new tasks is not allowed.");

                if (disposed)
                    throw new ObjectDisposedException("This thread pool has been disposed - queueing new tasks is not allowed.");

                if (exclusiveToken != null)
                {
                    // Request with this token is being processed - drop message
                    if (activeTokens.Contains(exclusiveToken))
                    {
                        callback?.Invoke(false);
                        return false;
                    }

                    activeTokens.AddLast(exclusiveToken);
                }

                var msgType = typeof(T);
                Action internalCallback = null;

                if (pools.ContainsKey(msgType))
                {
                    // This type has pool with temporary cache objects - move data to cache and use it instead
                    var pool = (ConcurrentPool<T>) pools[msgType];
                    var itcbd = ((IThreadCachedBridgeData<T>) data);
                    var hash = itcbd.GetHash();
                    var item = pool.Get(hash);

                    itcbd.CopyToCache(item);

                    data = item;
                    internalCallback = () => pool.Release(item, hash);
                }

                QueueTask(() => publisher(data), callback, internalCallback, exclusiveToken);
            }

            return true;
        }

        private void QueueTask(Action task, Action<bool> externalCallback = null, Action internalCallback = null, object exclusiveToken = null)
        {
            if (disposePending)
                throw new InvalidOperationException("This thread pool is being disposed - queueing new tasks is not allowed.");

            if (disposed)
                throw new ObjectDisposedException("This thread pool has been disposed - queueing new tasks is not allowed.");

            // Queue is not expected to unload in a single cycle - spawn another thread if possible
            if (requests.Count >= workerCount)
            {
                if (workerCount < maxWorkerCount)
                {
                    // CPU still has available cores - spawn new worker to utilize them 
                    SpawnNewWorker();
                }
                else if (requests.Count >= maxQueueSize)
                {
                    // All CPU cores are occupied, but queue reached its capacity - too much data to process it in
                    // real time. Block main thread until one of the workers finishes job.
                    // This will kill performance, but otherwise the queue would scale into infinity.
                    if (!performanceWarningDisplayed)
                    {
                        performanceWarningDisplayed = true;
                        var msg = "CPU is not able to process bridge messages in realtime. Consider reducing data throughput.\n";
                        msg += GetDebugData();
                        Debug.LogWarning(msg);
                    }

                    Debug.Log(GetDebugData());

                    Monitor.Wait(requests);
                }
            }

            // Allow up to one semi-idle worker to unload queue spikes, but prolonged idle time over that
            if (workerCount > 1 && workerThreads.Count > 1)
                idleCycles++;
            else
                idleCycles = 0;

            // For last 128 requests, at least one worker was idle - just get rid of it
            if (idleCycles > 128)
            {
                KillWorker();
                idleCycles = 0;
            }

            requests.AddLast(new Request
            {
                task = task,
                internalCallback = internalCallback,
                externalCallback = externalCallback,
                exclusiveToken = exclusiveToken
            });

            // Notify worker threads that tasks queue was updated
            Monitor.PulseAll(requests);
        }

        private void Worker()
        {
            while (true)
            {
                Request request;

                lock (requests)
                {
                    while (true)
                    {
                        if (disposed)
                            return;

                        // Assign first task in queue to first idle thread in queue (if available)
                        if (workerThreads.First != null && ReferenceEquals(Thread.CurrentThread, workerThreads.First.Value) && requests.Count > 0)
                        {
                            request = requests.First.Value;
                            requests.RemoveFirst();
                            workerThreads.RemoveFirst();

                            // Queue changed - notify all threads monitoring it
                            Monitor.PulseAll(requests);
                            break;
                        }

                        // Wait for tasks list to update instead of idly looping
                        Monitor.Wait(requests);
                    }
                }

                var crashed = false;

                try
                {
                    request.task();
                }
                catch (Exception ex)
                {
                    crashed = true;
                    Debug.LogException(ex);
                }

                request.internalCallback?.Invoke();
                request.externalCallback?.Invoke(!crashed);

                lock (requests)
                {
                    if (request.exclusiveToken != null)
                        activeTokens.Remove(request.exclusiveToken);

                    workerThreads.AddLast(Thread.CurrentThread);
                }
            }
        }

        public void Dispose()
        {
            var joinThreads = false;

            lock (requests)
            {
                if (!disposed)
                {
                    disposePending = true;
                    GC.SuppressFinalize(this);

                    requests.Clear();

                    // Trigger all threads to execute next iteration of the main loop and terminate through disposed flag
                    disposed = true;
                    Monitor.PulseAll(requests);
                    joinThreads = true;
                }
            }

            if (joinThreads)
            {
                foreach (var worker in workerThreads)
                    worker.Join();
            }
        }
    }
}