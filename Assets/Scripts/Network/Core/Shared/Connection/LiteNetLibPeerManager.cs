/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Shared.Connection
{
    using System.Net;
    using LiteNetLib;
    using Messaging;
    using Messaging.Data;

    /// <summary>
    /// Peer to peer connection manager using LiteNetLib
    /// </summary>
    public class LiteNetLibPeerManager : IPeerManager
    {
        /// <summary>
        /// Managed NetPeer
        /// </summary>
        public NetPeer Peer { get; }

        /// <inheritdoc/>
        public long RemoteTimeTicksDifference => Peer.RemoteTimeDelta;

        /// <inheritdoc/>
        public bool Connected => Peer.ConnectionState == ConnectionState.Connected;
        
        /// <inheritdoc/>
        public IPEndPoint PeerEndPoint => Peer.EndPoint;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="peer">Managed NetPeer</param>
        public LiteNetLibPeerManager(NetPeer peer)
        {
            Peer = peer;
        }
        
        /// <inheritdoc/>
        public void Send(Message message)
        {
            var dataToSent = message.Content.GetDataCopy();
            try
            {
                NetworkStatistics.ReportSentPackage(dataToSent);
                Peer.Send(dataToSent, GetDeliveryMethod(message.Type));
            }
            catch (TooBigPacketException)
            {
                Log.Error($"Too large message to be sent: {dataToSent.Length}.");
            }
        }

        /// <summary>
        /// Gets LiteNetLib DeliveryMethod corresponding to the given MessageType
        /// </summary>
        /// <param name="messageType">Message type</param>
        /// <returns>Corresponding LiteNetLib DeliveryMethod to the given MessageType</returns>
        public static DeliveryMethod GetDeliveryMethod(MessageType messageType)
        {
            return (DeliveryMethod) messageType;
        }

        /// <summary>
        /// Gets MessageType corresponding to the given LiteNetLib DeliveryMethod
        /// </summary>
        /// <param name="deliveryMethod">Delivery method</param>
        /// <returns>Corresponding MessageType to the given LiteNetLib DeliveryMethod</returns>
        public static MessageType GetDeliveryMethod(DeliveryMethod deliveryMethod)
        {
            return (MessageType) deliveryMethod;
        }
    }
}