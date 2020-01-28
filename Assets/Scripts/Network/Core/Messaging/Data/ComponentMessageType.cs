/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging.Data
{
    /// <summary>
    /// Distributed component message type
    /// </summary>
    public enum ComponentMessageType
    {
        /// <summary>
        /// Snapshot of the distributed component included in the message
        /// </summary>
        Snapshot = 0,
        
        /// <summary>
        /// Only some changes of the distributed component included in the message
        /// </summary>
        Delta = 1,
    }
}
