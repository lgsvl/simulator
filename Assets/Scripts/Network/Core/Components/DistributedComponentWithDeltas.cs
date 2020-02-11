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
            BroadcastMessage(new Message(Key, snapshot,
                reliableSnapshot ? MessageType.ReliableUnordered : MessageType.Unreliable));
        }

        /// <inheritdoc/>
        protected override void UnicastSnapshot(IPEndPoint endPoint, bool reliableSnapshot = false)
        {
            var snapshot = GetSnapshot();
            snapshot.PushEnum<ComponentMessageType>((int) ComponentMessageType.Snapshot);
            UnicastMessage(endPoint, new Message(Key, snapshot,
                reliableSnapshot ? MessageType.ReliableUnordered : MessageType.Unreliable));
        }

        /// <summary>
        /// Method broadcasting the delta message
        /// </summary>
        /// <param name="deltaMessage">Delta message to send</param>
        /// <param name="deltaType">Delta message type</param>
        protected void SendDelta(BytesStack deltaMessage, MessageType deltaType = MessageType.ReliableOrdered)
        {
            deltaMessage.PushEnum<ComponentMessageType>((int) ComponentMessageType.Delta);
            BroadcastMessage(new Message(Key, deltaMessage, deltaType));
        }
        
        /// <inheritdoc/>
        protected override void ParseMessage(Message message)
        {
            var messageType = message.Content.PopEnum<ComponentMessageType>();
            if (messageType == ComponentMessageType.Snapshot)
                ApplySnapshot(message);
            else if (messageType == ComponentMessageType.Delta)
                ApplyDelta(message);
                
        }
        
        /// <summary>
        /// Parsing received delta
        /// </summary>
        /// <param name="message">Received delta in a message</param>
        protected abstract void ApplyDelta(Message message);
    }
}