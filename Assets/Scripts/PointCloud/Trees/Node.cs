/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using System;
    using Unity.Collections;
    using UnityEngine;

    /// <summary>
    /// Class representing a non-editable version of a tree node.
    /// </summary>
    public class Node : IDisposable
    {
        /// <summary>
        /// Unique identifier of this node.
        /// </summary>
        public readonly string Identifier;

        /// <summary>
        /// Current state of unmanaged data stored in this node.
        /// </summary>
        public NodeDataState DataState;
        
        /// <summary>
        /// Returns a list of points currently stored in this node.
        /// </summary>
        public NativeArray<PointCloudPoint> Points;

        /// <summary>
        /// Creates a new instance of a node and populates it with passed data.
        /// </summary>
        /// <param name="record">Record with meta data of the node.</param>
        public Node(NodeRecord record)
        {
            Identifier = record.Identifier;
            if (record.PointCount > 0)
            {
                Points = new NativeArray<PointCloudPoint>(record.PointCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);

                DataState = NodeDataState.Loading;
            }
            else
            {
                Debug.LogWarning($"Node with ID {Identifier} has no data. This shouldn't happen.");
                DataState = NodeDataState.Empty;
            }
        }

        /// <summary>
        /// Marks this node's data as loaded and ready to use.
        /// </summary>
        public void MarkAsLoaded()
        {
            DataState = NodeDataState.InMemory;
        }

        ~Node()
        {
            // No need to suppress finalizer, Points array is checked for prior dispose
            Dispose();
        }

        public void Dispose()
        {
            DataState = NodeDataState.Disposed;
            
            if (Points == default)
                return;
            
            Points.Dispose();
            Points = default;
        }
    }
}