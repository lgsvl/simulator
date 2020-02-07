/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Connection;
    using Data;
    using Identification;

    /// <summary>
    /// Messages manager for incoming  and outgoing messages via connection manager
    /// </summary>
    public class MessagesManager
    {
        /// <summary>
        /// Bytes stack that is addressed to the target end point
        /// </summary>
        private class AwaitingMessage
        {
            /// <summary>
            /// EndPoint address of the message
            /// </summary>
            public IPEndPoint EndPoint { get; set; }

            /// <summary>
            /// Message awaiting in the queue
            /// </summary>
            public Message Message { get; set; }
        }

        /// <summary>
        /// Manager for coding and decoding the timestamps of messages
        /// </summary>
        private readonly TimeManager timeManager = new TimeManager();

        /// <summary>
        /// Register of identified objects
        /// </summary>
        private readonly IdsRegister idsRegister;

        /// <summary>
        /// Connection manager used for sending and receiving messages
        /// </summary>
        private readonly IConnectionManager connectionManager;

        /// <summary>
        /// Registered messages senders
        /// </summary>
        private readonly List<IMessageSender> senders = new List<IMessageSender>();

        /// <summary>
        /// Awaiting messages received before proper receiver is registered
        /// </summary>
        private readonly Dictionary<int, List<AwaitingMessage>> awaitingIncomingMessages =
            new Dictionary<int, List<AwaitingMessage>>();

        /// <summary>
        /// Awaiting messages sent before proper sender is registered
        /// </summary>
        private readonly Dictionary<string, List<AwaitingMessage>> awaitingOutgoingMessages =
            new Dictionary<string, List<AwaitingMessage>>();

        /// <summary>
        /// Manager for coding and decoding the timestamps of messages
        /// </summary>
        public TimeManager TimeManager => timeManager;

        /// <summary>
        /// Timeout of incoming message in queue in milliseconds, after this time message is dropped
        /// </summary>
        private float IncomingMessagesTimeout => connectionManager.Timeout;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionManager">Connection manager used for sending and receiving messages</param>
        public MessagesManager(IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
            idsRegister = new IdsRegister(this, connectionManager.IsServer);
            senders.Add(idsRegister);
            if (idsRegister.AssignIds)
            {
                idsRegister.SelfRegister();
            }

            idsRegister.ObjectBoundToId += IdsRegisterOnObjectBoundToId;
            connectionManager.PeerConnected += ConnectionManagerOnPeerConnected;
            connectionManager.MessageReceived += ConnectionManagerOnMessageReceived;
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~MessagesManager()
        {
            connectionManager.MessageReceived -= ConnectionManagerOnMessageReceived;
            connectionManager.PeerConnected -= ConnectionManagerOnPeerConnected;
            idsRegister.ObjectBoundToId -= IdsRegisterOnObjectBoundToId;
            senders.Remove(idsRegister);
        }

        /// <summary>
        /// Resets time manager start time and revoke identifiers bound by server
        /// </summary>
        public void RevokeIdentifiers()
        {
            if (idsRegister.AssignIds) return;
            awaitingIncomingMessages.Clear();
            awaitingOutgoingMessages.Clear();
            idsRegister.RevokeIds();
        }

        /// <summary>
        /// Method called when new identified object is bound with id
        /// </summary>
        /// <param name="identifiedObject">Bound identified object</param>
        /// <param name="id">Bound identifier</param>
        private void IdsRegisterOnObjectBoundToId(IIdentifiedObject identifiedObject, int id)
        {
            //Send to the object all awaiting incoming messages addressed to this identifier
            if (identifiedObject is IMessageReceiver receiver)
                if (awaitingIncomingMessages.TryGetValue(id, out var awaitingMessages))
                    try
                    {
                        //Clear messages that wait too long
                        var timeTicksNow = DateTime.UtcNow.Ticks;
                        var timeoutTicks = IncomingMessagesTimeout * 10000;
                        var numberOfMessagesToRemove = 0;
                        for (var i=0; i<awaitingMessages.Count; i++)
                            if (timeTicksNow - awaitingMessages[i].Message.TimeTicksDifference > timeoutTicks)
                                numberOfMessagesToRemove = i + 1;
                            else break;
                        awaitingMessages.RemoveRange(0, numberOfMessagesToRemove);
                        
                        //Pass still valid messages
                        foreach (var awaitingMessage in awaitingMessages)
                        {
                            //Ignore messages with outdated assigned identifiers
                            if (awaitingMessage.Message.Timestamp < idsRegister.InternalIdBindUtcTime)
                                continue;
                            awaitingMessage.Message.AddressKey = identifiedObject.Key;
                            receiver.ReceiveMessage(connectionManager.GetConnectedPeerManager(awaitingMessage.EndPoint),
                                awaitingMessage.Message);
                        }
                    }
                    finally
                    {
                        awaitingIncomingMessages.Remove(id);
                    }

            //Send all awaiting outgoing messages tried to be send with the same address key
            if (identifiedObject is IMessageSender sender)
                if (awaitingOutgoingMessages.TryGetValue(sender.Key, out var awaitingMessages))
                    try
                    {
                        foreach (var awaitingMessage in awaitingMessages)
                        {
                            if (Equals(awaitingMessage.EndPoint.Address, IPAddress.Broadcast))
                                BroadcastMessage(awaitingMessage.Message);
                            else
                                UnicastMessage(awaitingMessage.EndPoint, awaitingMessage.Message);
                        }
                    }
                    finally
                    {
                        awaitingOutgoingMessages.Remove(sender.Key);
                    }
        }

        /// <summary>
        /// Method that sends initial messages to new peer after connecting to the connection manager
        /// </summary>
        /// <param name="peer">New connected peer</param>
        private void ConnectionManagerOnPeerConnected(IPeerManager peer)
        {
            for (var i = 0; i < senders.Count; i++)
                senders[i].UnicastInitialMessages(peer.PeerEndPoint);
        }

        /// <summary>
        /// Method handling received message from the connection manager
        /// </summary>
        /// <param name="sender">The peer from which message has been received</param>
        /// <param name="message">Received message</param>
        private void ConnectionManagerOnMessageReceived(IPeerManager sender, Message message)
        {
            MessageReceived(sender, message);
        }

        /// <summary>
        /// Method handling incoming message to this receiver
        /// </summary>
        /// <param name="sender">The peer from which message has been received</param>
        /// <param name="message">Received message</param>
        private void MessageReceived(IPeerManager sender, Message message)
        {
            //Check if peer is still connected
            if (sender == null)
                return;
            TimeManager.PopTimeDifference(message, sender.RemoteTimeTicksDifference);
            var id = idsRegister.PopId(message.Content);
            var identifiedObject = idsRegister.ResolveObject(id);
            if (identifiedObject is IMessageReceiver receiver)
            {
                //Forward message to proper receiver
                message.AddressKey = identifiedObject.Key;
                receiver.ReceiveMessage(sender, message);
            }
            else
            {
                //Check if it is initialization message for IdsRegister - first sent message
                if (idsRegister.IsInitializationMessage(sender, message))
                    return;

                //Ignore messages with outdated assigned identifiers
                if (message.Timestamp < idsRegister.InternalIdBindUtcTime)
                    return;

                //Hold message until proper receiver registers
                if (!awaitingIncomingMessages.TryGetValue(id, out var messages))
                {
                    messages = new List<AwaitingMessage>();
                    awaitingIncomingMessages.Add(id, messages);
                }
                else
                {
                    //Clear messages that wait too long
                    var timeTicksNow = DateTime.UtcNow.Ticks;
                    var timeoutTicks = IncomingMessagesTimeout * 10000;
                    var numberOfMessagesToRemove = 0;
                    for (var i=0; i<messages.Count; i++)
                        if (timeTicksNow - messages[i].Message.TimeTicksDifference > timeoutTicks)
                            numberOfMessagesToRemove = i + 1;
                        else break;
                    messages.RemoveRange(0, numberOfMessagesToRemove);
                }

                var awaitingMessage = new AwaitingMessage()
                {
                    EndPoint = sender.PeerEndPoint,
                    Message = message
                };
                messages.Add(awaitingMessage);
            }
        }

        /// <summary>
        /// Unicast message to connected peer within given address
        /// </summary>
        /// <param name="endPoint">End point of the target peer</param>
        /// <param name="message">Message to be sent</param>
        public void UnicastMessage(IPEndPoint endPoint, Message message)
        {
            var id = idsRegister.ResolveId(message.AddressKey);
            if (id != null)
            {
                idsRegister.PushId(message);
                TimeManager.PushTimeDifference(message);
                connectionManager.Unicast(endPoint, message);
                return;
            }

            //Hold message until proper sender registers
            var key = message.AddressKey;
            if (!awaitingOutgoingMessages.TryGetValue(key, out var messages))
            {
                messages = new List<AwaitingMessage>();
                awaitingOutgoingMessages.Add(key, messages);
            }

            var awaitingMessage = new AwaitingMessage()
            {
                EndPoint = endPoint,
                Message = message
            };
            messages.Add(awaitingMessage);
        }

        /// <summary>
        /// Broadcast message to all connected peers
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public void BroadcastMessage(Message message)
        {
            var id = idsRegister.ResolveId(message.AddressKey);
            if (id != null)
            {
                idsRegister.PushId(message);
                TimeManager.PushTimeDifference(message);
                connectionManager.Broadcast(message);
                return;
            }

            //Hold message until proper sender registers
            var key = message.AddressKey;
            if (!awaitingOutgoingMessages.TryGetValue(key, out var messages))
            {
                messages = new List<AwaitingMessage>();
                awaitingOutgoingMessages.Add(key, messages);
            }

            var awaitingMessage = new AwaitingMessage()
            {
                EndPoint = new IPEndPoint(IPAddress.Broadcast, connectionManager.Port),
                Message = message
            };
            messages.Add(awaitingMessage);
        }

        /// <summary>
        /// Register identified object in the manager
        /// </summary>
        /// <param name="identifiedObject">Identified object to be registered</param>
        public void RegisterObject(IIdentifiedObject identifiedObject)
        {
            if (identifiedObject is IMessageSender sender)
                senders.Add(sender);
            idsRegister.RegisterObject(identifiedObject);
        }

        /// <summary>
        /// Unregister identified object from the manager
        /// </summary>
        /// <param name="identifiedObject">Identified object to be unregistered</param>
        public void UnregisterObject(IIdentifiedObject identifiedObject)
        {
            if (identifiedObject is IMessageSender sender)
                senders.Remove(sender);
            idsRegister.UnregisterObject(identifiedObject);
        }
    }
}