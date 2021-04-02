/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Connection
{
    using System;
    using System.Net;
    using Messaging.Data;

    /// <summary>
    /// Interface managing a connection with a single peer
    /// </summary>
    public interface IPeerManager
    {
        /// <summary>
        /// Unique identifier of the peer
        /// </summary>
        string Identifier { get; }
        
        /// <summary>
        /// Is the peer currently connected
        /// </summary>
        bool Connected { get; }
        
        /// <summary>
        /// Difference between local time and remote time in ticks count
        /// </summary>
        long RemoteTimeTicksDifference { get; }
        
        /// <summary>
        /// Current UTC time of the remote peer
        /// </summary>
        DateTime RemoteUtcTime { get; }
        
        /// <summary>
        /// End point of the connected peer
        /// </summary>
        IPEndPoint PeerEndPoint { get; }

        /// <summary>
        /// Disconnects from the peer
        /// </summary>
        void Disconnect();
        
        /// <summary>
        /// Send message directly to the peer
        /// </summary>
        /// <param name="distributedMessage">Message to be sent</param>
        void Send(DistributedMessage distributedMessage);
    }
}