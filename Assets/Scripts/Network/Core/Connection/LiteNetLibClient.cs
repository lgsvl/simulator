/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Connection
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using LiteNetLib;
    using Messaging.Data;

    using Simulator.Network.Core.Messaging;

    /// <summary>
    /// The client's connection manager using LiteNetLib
    /// </summary>
    public class LiteNetLibClient : IConnectionManager, INetEventListener
    {
        /// <summary>
        /// Connection application key
        /// </summary>
        public const string ApplicationKey = "SimulatorClient"; // TODO: this can be unique per run
        
        /// <summary>
        /// The net manager for this client
        /// </summary>
        private NetManager netClient;
        
        /// <summary>
        /// Peer manager for connection with the server
        /// </summary>
        private LiteNetLibPeerManager masterPeer;

        /// <inheritdoc/>
        public bool IsServer => false;
        
        /// <inheritdoc/>
        public int Port { get; private set; }

        /// <inheritdoc/>
        public int Timeout => 30000;

        /// <inheritdoc/>
        public int ConnectedPeersCount => masterPeer == null ? 0 : 1;

        /// <summary>
        /// The net manager for this client
        /// </summary>
        public NetManager NetClient => netClient;
        
        /// <summary>
        /// Current latency for this connection with the server
        /// </summary>
        public int Latency { get; private set; }

        /// <inheritdoc/>
        public event Action<IPeerManager> PeerConnected;

        /// <inheritdoc/>
        public event Action<IPeerManager> PeerDisconnected;
        
        /// <inheritdoc/>
        public event Action<DistributedMessage> MessageReceived;
        
        /// <summary>
        /// Event invoked when the latency changes
        /// </summary>
        public event Action<int> LatencyUpdated;
        
        /// <inheritdoc/>
        public bool Start(int port)
        {
            Port = port;
            netClient = new NetManager(this) {UnconnectedMessagesEnabled = false, UpdateTime = 5, DisconnectTimeout = Timeout,
                AutoRecycle = true};
            return NetClient.Start(port);
        }

        /// <inheritdoc/>
        public void Stop()
        {
            NetClient?.Stop();
        }

        /// <inheritdoc/>
        public IPeerManager Connect(IPEndPoint endPoint)
        {
            if (masterPeer != null)
                throw new ArgumentException("Client can be connected only to a single peer.");
            masterPeer = new LiteNetLibPeerManager(NetClient.Connect(endPoint, ApplicationKey));
            return masterPeer;
        }

        /// <inheritdoc/>
        public void PoolEvents()
        {
            NetClient?.PollEvents();
        }

        /// <inheritdoc/>
        public void Unicast(IPEndPoint endPoint, DistributedMessage distributedMessage)
        {
            if (Equals(masterPeer.PeerEndPoint, endPoint))
                masterPeer.Send(distributedMessage);
            distributedMessage.Release();
        }

        /// <inheritdoc/>
        public void Broadcast(DistributedMessage distributedMessage)
        {
            masterPeer?.Send(distributedMessage);
            distributedMessage.Release();
        }

        /// <inheritdoc/>
        public IPeerManager GetConnectedPeerManager(IPEndPoint endPoint)
        {
            return Equals(masterPeer.PeerEndPoint, endPoint) ? masterPeer : null;
        }

        /// <inheritdoc/>
        public void OnPeerConnected(NetPeer peer)
        {
            if (masterPeer == null)
                masterPeer = new LiteNetLibPeerManager(peer);
            else if (!Equals(masterPeer.PeerEndPoint, peer.EndPoint))
                throw new ArgumentException("Client can be connected only to a single peer.");
            PeerConnected?.Invoke(masterPeer);
        }

        /// <inheritdoc/>
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            var disconnectedPeer = masterPeer;
            if (masterPeer.Peer == peer)
                masterPeer = null;
            PeerDisconnected?.Invoke(disconnectedPeer);
        }

        /// <inheritdoc/>
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Log.Error("[Client] error " + socketError);
        }

        /// <inheritdoc/>
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (MessageReceived == null)
                return;

            var availableBytes = reader.AvailableBytes;
            var message = MessagesPool.Instance.GetMessage(availableBytes);
            message.Content.PushBytes(reader.RawData, reader.Position, availableBytes);
            message.Sender = masterPeer;
            message.Type = GetDeliveryMethod(deliveryMethod);
            MessageReceived?.Invoke(message);
        }

        /// <inheritdoc/>
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            //TODO broadcast messages support
        }

        /// <inheritdoc/>
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            Latency = latency;
            LatencyUpdated?.Invoke(latency);
        }

        /// <inheritdoc/>
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey(LiteNetLibServer.ApplicationKey);
        }

        /// <summary>
        /// Gets MessageType corresponding to the given LiteNetLib DeliveryMethod
        /// </summary>
        /// <param name="deliveryMethod">Delivery method</param>
        /// <returns>Corresponding MessageType to the given LiteNetLib DeliveryMethod</returns>
        public static DistributedMessageType GetDeliveryMethod(DeliveryMethod deliveryMethod)
        {
            return (DistributedMessageType)deliveryMethod;
        }
    }
}
