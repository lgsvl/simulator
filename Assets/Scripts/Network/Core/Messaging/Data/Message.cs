/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging.Data
{
    using System;

    /// <summary>
    /// Message distributed between peers
    /// </summary>
    public class Message
    {
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
        public MessageType Type { get; set; } = MessageType.ReliableOrdered;
        
        /// <summary>
        /// Utc time stamp of the message, may be become invalid when new authoritative correction arrives. 
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
        public Message(BytesStack content)
        {
            Content = content;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="addressKey">Address key defining where message should be passed</param>
        /// <param name="content">The content of message</param>
        /// <param name="messageType">Type defining how message should be delivered</param>
        public Message(string addressKey, BytesStack content, MessageType messageType)
        {
            AddressKey = addressKey;
            Content = content;
            Type = messageType;
        }
    }
}
