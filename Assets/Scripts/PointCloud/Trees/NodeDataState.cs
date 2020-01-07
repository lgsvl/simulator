/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    /// <summary>
    /// Describes state of node's unmanaged data.
    /// </summary>
    public enum NodeDataState
    {
        /// Node's data is currently being loaded and is not available yet.
        Loading,
        /// Node's data is loaded in memory and usable.
        InMemory,
        /// Node's data is has been already disposed of.
        Disposed,
        /// Node has no data available. This state can only be achieved through unexpected behaviour.
        Empty
    }
}