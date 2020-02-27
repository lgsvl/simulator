/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging
{
    using Connection;
    using Data;
    using Identification;

    /// <summary>
    /// Interface for the message receiver
    /// </summary>
    public interface IMessageReceiver : IIdentifiedObject
    {
        /// <summary>
        /// Method handling incoming message to this receiver
        /// </summary>
        /// <param name="sender">The peer from which message has been received</param>
        /// <param name="distributedMessage">Received message</param>
        void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage);
    }
}