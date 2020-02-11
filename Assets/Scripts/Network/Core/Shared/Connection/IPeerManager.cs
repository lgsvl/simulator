/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Shared.Connection
{
    using System.Net;
    using Messaging;
    using Messaging.Data;

    /// <summary>
    /// Interface managing a connection with a single peer
    /// </summary>
    public interface IPeerManager
    {
        /// <summary>
        /// Is the peer currently connected
        /// </summary>
        bool Connected { get; }
        
        /// <summary>
        /// Difference between local time and remote time in ticks count
        /// </summary>
        long RemoteTimeTicksDifference { get; }
        
        /// <summary>
        /// End point of the connected peer
        /// </summary>
        IPEndPoint PeerEndPoint { get; }
        
        /// <summary>
        /// Send message directly to the peer
        /// </summary>
        /// <param name="message">Message to be sent</param>
        void Send(Message message);
    }
}