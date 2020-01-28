/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging.Data
{
    /// <summary>
    /// Distributed object command type
    /// </summary>
    public enum DistributedObjectCommandType
    {
        /// <summary>
        /// Enable game object of the distributed object
        /// </summary>
        Enable = 0,
        
        /// <summary>
        /// Disable game object of the distributed object
        /// </summary>
        Disable = 1,
    }
}
