/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Network.Core.Connection;

namespace Simulator.Network.Core.Messaging.Data
{
    using System;

    /// <summary>
    /// Message distributed between peers
    /// </summary>
    public class DistributedMessage
    {
        /// <summary>
        /// Pool which released this message
        /// </summary>
        public MessagesPool OriginPool { get; internal set; }
        
        /// <summary>
        /// Peer which sent this message, filled only in incoming messages
        /// </summary>
        public IPeerManager Sender { get; internal set; }
        
        /// <summary>
        /// Address key defining where message should be passed. 
        /// May be not set for received messages until <see cref="MessagesManager"/> identifies address
        /// </summary>
        public string AddressKey { get; set; }
        
        /// <summary>
        /// The content of message
        /// </summary>
        public BytesStack Content { get; }

        /// <summary>
        /// Type defining how message should be delivered
        /// </summary>
        public DistributedMessageType Type { get; set; } = DistributedMessageType.ReliableOrdered;
        
        /// <summary>
        /// Utc time stamp of the message, may be become invalid when new authoritative correction arrives. 
        /// For messages being sent, value is not being set
        /// </summary>
        public DateTime ServerTimestamp { get; set; } = DateTime.MinValue;
        
        /// <summary>
        /// Utc time stamp of the message, may be become invalid as the authoritative correction varies. 
        /// For messages being sent, value is set just before sending by <see cref="MessagesManager"/>
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Time difference in ticks between epoch time and the message send time
        /// For messages being sent, value is set just before sending by <see cref="MessagesManager"/>
        /// </summary>
        public long TimeTicksDifference { get; set; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="content">The content of message</param>
        public DistributedMessage(BytesStack content)
        {
            Content = content;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="addressKey">Address key defining where message should be passed</param>
        /// <param name="content">The content of message</param>
        /// <param name="distributedMessageType">Type defining how message should be delivered</param>
        public DistributedMessage(string addressKey, BytesStack content, DistributedMessageType distributedMessageType)
        {
            AddressKey = addressKey;
            Content = content;
            Type = distributedMessageType;
        }

        /// <summary>
        /// Releases this message to the pool
        /// </summary>
        public void Release()
        {
            OriginPool?.ReleaseMessage(this);
        }
    }
}
