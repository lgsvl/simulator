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
            var snapshotMessage = GetSnapshotMessage(reliableSnapshot);
            snapshotMessage.Content.PushEnum<ComponentMessageType>((int) ComponentMessageType.Snapshot);
            BroadcastMessage(snapshotMessage);
        }

        /// <inheritdoc/>
        protected override void UnicastSnapshot(IPEndPoint endPoint, bool reliableSnapshot = false)
        {
            var snapshotMessage = GetSnapshotMessage(reliableSnapshot);
            snapshotMessage.Content.PushEnum<ComponentMessageType>((int) ComponentMessageType.Snapshot);
            UnicastMessage(endPoint, snapshotMessage);
        }

        /// <summary>
        /// Gets distributed message without content prepared to be sent as delta message
        /// </summary>
        /// <param name="reliableMessage">Should this message be reliable</param>
        /// <param name="expectedMessageSize">Expected message size in bytes</param>
        public DistributedMessage GetEmptyDeltaMessage(bool reliableMessage = false, int expectedMessageSize = 0)
        {
            var message = MessagesPool.Instance.GetMessage(expectedMessageSize <= 0 ? 0 : expectedMessageSize + 4);
            message.Type = reliableMessage ? DistributedMessageType.ReliableOrdered : DistributedMessageType.Unreliable;
            message.AddressKey = Key;
            return message;
        }

        /// <summary>
        /// Method broadcasting the delta message
        /// </summary>
        /// <param name="deltaMessage">Delta message to send</param>
        public virtual void BroadcastDelta(DistributedMessage deltaMessage)
        {
            deltaMessage.Content.PushEnum<ComponentMessageType>((int) ComponentMessageType.Delta);
            BroadcastMessage(deltaMessage);
        }

        /// <summary>
        /// Method unicasting the delta message
        /// </summary>
        /// <param name="endPoint">Endpoint of the target client</param>
        /// <param name="deltaMessage">Delta message to send</param>
        public virtual void UnicastDelta(IPEndPoint endPoint, DistributedMessage deltaMessage)
        {
            deltaMessage.Content.PushEnum<ComponentMessageType>((int) ComponentMessageType.Delta);
            UnicastMessage(endPoint, deltaMessage);
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