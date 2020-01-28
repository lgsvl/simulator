/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging.Data
{
    /// <summary>
    /// Distributed objects root command type
    /// </summary>
    public enum DistributedRootCommandType
    {
        /// <summary>
        /// Instantiate new distributed object
        /// </summary>
        InstantiateDistributedObject = 0,
        
        /// <summary>
        /// Destroy instantiated distributed object
        /// </summary>
        DestroyDistributedObject = 1
    }
}
