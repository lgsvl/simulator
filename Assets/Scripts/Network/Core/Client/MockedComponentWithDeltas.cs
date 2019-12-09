/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Client
{
    using Shared.Messaging.Data;

    /// <summary>
    /// Mocked component that support received snapshot and delta messages
    /// </summary>
    public abstract class MockedComponentWithDeltas : MockedComponent
    {
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
