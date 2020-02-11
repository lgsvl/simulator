/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using System;
    using System.Collections.Generic;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Class used to build buffers used in point cloud rendering.
    /// </summary>
    public class BufferBuilder : IDisposable
    {
        private readonly NodeLoader nodeLoader;

        private readonly List<string> queuedNodes = new List<string>();

        private readonly int maxBufferElements;
        
        private readonly int rebuildSteps;
        
        private ComputeBuffer bufferA;
        private ComputeBuffer bufferB;

        private int lastProcessedNodeIndex;
        private int currentStepCount;
        private int constructedBufferItemsCount;
        private int readyBufferItemsCount;
        
        private bool bufferSwapFlag;
        
        private bool busy;

        private ComputeBuffer ReadyBuffer => bufferSwapFlag ? bufferB : bufferA;
        
        private ComputeBuffer ConstructedBuffer => bufferSwapFlag ? bufferA : bufferB;

        /// <summary>
        /// Creates a new instance of this class with given parameters.
        /// </summary>
        /// <param name="nodeLoader">Reference to <see cref="NodeLoader"/> that will be used to load required nodes.</param>
        /// <param name="maxBufferElements">Maximum amount of points that can be stored in buffer.</param>
        /// <param name="rebuildSteps">Amount of steps that buffer building process is split into.</param>
        public BufferBuilder(NodeLoader nodeLoader, int maxBufferElements, int rebuildSteps)
        {
            this.nodeLoader = nodeLoader;
            this.maxBufferElements = maxBufferElements;
            this.rebuildSteps = rebuildSteps;
            
            // DX11 for some reason doesn't work with SubUpdate mode in this case
            var bufferMode = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan
                ? ComputeBufferMode.SubUpdates
                : ComputeBufferMode.Immutable; 
            
            bufferA = new ComputeBuffer(maxBufferElements, UnsafeUtility.SizeOf<PointCloudPoint>(),
                ComputeBufferType.Default, bufferMode);
            
            bufferB = new ComputeBuffer(maxBufferElements, UnsafeUtility.SizeOf<PointCloudPoint>(),
                ComputeBufferType.Default, bufferMode);
        }

        /// <summary>
        /// <para>Performs as single step in buffer building process. Buffer will be ready after up to [<see cref="rebuildSteps"/>] steps.</para>
        /// <para>Note that this method is not bound to update loop and has to be called externally.</para>
        /// </summary>
        private void PerformBuildStep()
        {
            // Calculate amount of points that should be reached during this step
            currentStepCount++;
            var targetPointCount = (currentStepCount == rebuildSteps)
                ? maxBufferElements
                : (int) ((float) currentStepCount / rebuildSteps * maxBufferElements);

            var constructedBuffer = ConstructedBuffer;

            // Copy data from nodes until either all nodes are processed or quota for this step is reached
            while (lastProcessedNodeIndex < queuedNodes.Count && constructedBufferItemsCount < targetPointCount)
            {
                if (!nodeLoader.TryGetNode(queuedNodes[lastProcessedNodeIndex++], out var node))
                    continue;

                var count = node.Points.Length;
                var afterAppendCount = constructedBufferItemsCount + count;
                
                if (afterAppendCount > maxBufferElements)
                {
                    Debug.LogWarning($"Total amount of points in requested nodes exceeds buffer size ({maxBufferElements.ToString()}). Truncating.");
                    lastProcessedNodeIndex = queuedNodes.Count;
                    break;
                }

                constructedBuffer.SetData(node.Points, 0, constructedBufferItemsCount, count);

                constructedBufferItemsCount += count;
            }

            // This was the last step - reset progress, swap ready and under-construction buffers
            if (lastProcessedNodeIndex == queuedNodes.Count)
            {
                lastProcessedNodeIndex = 0;
                readyBufferItemsCount = constructedBufferItemsCount;
                constructedBufferItemsCount = 0;
                currentStepCount = 0;
                
                bufferSwapFlag = !bufferSwapFlag;
                busy = false;
            }
        }

        /// <summary>
        /// <para>Requests new buffer built from given nodes and returns latest completed version of it.</para>
        /// <para>Buffer returned by this method is not the immediate result of merging passed nodes, but last fully
        /// completed version of request from up to [<see cref="rebuildSteps"/>] calls ago.</para>
        /// </summary>
        /// <param name="requiredNodes">Nodes that should be visible. This might be ignored if buffer is currently under construction.</param>
        /// <param name="validPointCount">Amount of valid points in returned buffer.</param>
        public ComputeBuffer GetPopulatedBuffer(List<string> requiredNodes, out int validPointCount)
        {
            if (!busy)
            {
                queuedNodes.Clear();
                queuedNodes.AddRange(requiredNodes);
                busy = true;
            }
            
            PerformBuildStep();
            
            validPointCount = readyBufferItemsCount;
            return ReadyBuffer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            bufferA.Release();
            bufferB.Release();

            bufferA = null;
            bufferB = null;
        }
    }
}