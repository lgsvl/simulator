/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging
{
    using System.Net;
    using Data;
    using Identification;

    /// <summary>
    /// Interface for the message sender
    /// </summary>
    public interface IMessageSender : IIdentifiedObject
    {
        /// <summary>
        /// Unicast message to connected peer within given address
        /// </summary>
        /// <param name="endPoint">End point of the target peer</param>
        /// <param name="distributedMessage">Message to be sent</param>
        void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage);

        /// <summary>
        /// Broadcast message to all connected peers
        /// </summary>
        /// <param name="distributedMessage">Message to be sent</param>
        void BroadcastMessage(DistributedMessage distributedMessage);

        /// <summary>
        /// Get initial messages that have to be sent to new peers
        /// </summary>
        /// <param name="endPoint">End point of target peer where messages will be sent</param>
        void UnicastInitialMessages(IPEndPoint endPoint);
    }
}