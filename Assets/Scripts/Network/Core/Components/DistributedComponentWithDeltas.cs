/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Components
{
    using System.Net;
    using Messaging;
    using Messaging.Data;

    /// <summary>
    /// Distributed component that support received snapshot and delta messages
    /// </summary>
    public abstract class DistributedComponentWithDeltas : DistributedComponent
    {
        /// <inheritdoc/>
        public override void BroadcastSnapshot(bool reliableSnapshot = false)
        {
            var snapshot = GetSnapshot();
            snapshot.PushEnum<ComponentMessageType>((int) ComponentMessageType.Snapshot);
            BroadcastMessage(new DistributedMessage(Key, snapshot,
                reliableSnapshot ? DistributedMessageType.ReliableUnordered : DistributedMessageType.Unreliable));
        }

        /// <inheritdoc/>
        protected override void UnicastSnapshot(IPEndPoint endPoint, bool reliableSnapshot = false)
        {
            var snapshot = GetSnapshot();
            snapshot.PushEnum<ComponentMessageType>((int) ComponentMessageType.Snapshot);
            UnicastMessage(endPoint, new DistributedMessage(Key, snapshot,
                reliableSnapshot ? DistributedMessageType.ReliableUnordered : DistributedMessageType.Unreliable));
        }

        /// <summary>
        /// Method broadcasting the delta message
        /// </summary>
        /// <param name="deltaMessage">Delta message to send</param>
        /// <param name="deltaType">Delta message type</param>
        protected void SendDelta(BytesStack deltaMessage, DistributedMessageType deltaType = DistributedMessageType.ReliableOrdered)
        {
            deltaMessage.PushEnum<ComponentMessageType>((int) ComponentMessageType.Delta);
            BroadcastMessage(new DistributedMessage(Key, deltaMessage, deltaType));
        }
        
        /// <inheritdoc/>
        protected override void ParseMessage(DistributedMessage distributedMessage)
        {
            var messageType = distributedMessage.Content.PopEnum<ComponentMessageType>();
            if (messageType == ComponentMessageType.Snapshot)
                ApplySnapshot(distributedMessage);
            else if (messageType == ComponentMessageType.Delta)
                ApplyDelta(distributedMessage);
                
        }
        
        /// <summary>
        /// Parsing received delta
        /// </summary>
        /// <param name="distributedMessage">Received delta in a message</param>
        protected abstract void ApplyDelta(DistributedMessage distributedMessage);
    }
}