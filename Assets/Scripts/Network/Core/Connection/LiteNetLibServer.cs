/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Connection
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using LiteNetLib;
    using Messaging.Data;

    using Simulator.Network.Core.Messaging;

    /// <summary>
    /// The server's connection manager using LiteNetLib
    /// </summary>
    public class LiteNetLibServer : IConnectionManager, INetEventListener, INetLogger
    {
        /// <summary>
        /// Connection application key
        /// </summary>
        public const string ApplicationKey = "SimulatorServer"; // TODO: this can be unique per run

        /// <summary>
        /// The LiteNetLib net manager
        /// </summary>
        private NetManager netServer;

        /// <summary>
        /// Peers dictionary, which are connected or trying to connect
        /// </summary>
        private readonly Dictionary<EndPoint, LiteNetLibPeerManager> peers =
            new Dictionary<EndPoint, LiteNetLibPeerManager>();

        /// <inheritdoc/>
        public bool IsServer => true;
        
        /// <inheritdoc/>
        public int Port { get; private set; }

        /// <inheritdoc/>
        public int Timeout => 30000;

        /// <inheritdoc/>
        public int ConnectedPeersCount => peers.Count;

        /// <inheritdoc/>
        public event Action<IPeerManager> PeerConnected;

        /// <inheritdoc/>
        public event Action<IPeerManager> PeerDisconnected;

        /// <inheritdoc/>
        public event Action<DistributedMessage> MessageReceived;

        /// <inheritdoc/>
        public bool Start(int port)
        {
            Port = port;
            NetDebug.Logger = this;
            netServer = new NetManager(this)
                {BroadcastReceiveEnabled = false, UpdateTime = 5, DisconnectTimeout = Timeout};
            return netServer.Start(port);
        }

        /// <inheritdoc/>
        public void Stop()
        {
            NetDebug.Logger = null;
            netServer?.Stop();
        }

        /// <inheritdoc/>
        public IPeerManager Connect(IPEndPoint endPoint)
        {
            var peerManager = new LiteNetLibPeerManager(netServer.Connect(endPoint, ApplicationKey));
            peers.Add(peerManager.PeerEndPoint, peerManager);
            return peerManager;
        }

        /// <inheritdoc/>
        public void PoolEvents()
        {
            netServer?.PollEvents();
        }

        /// <inheritdoc/>
        public void Unicast(IPEndPoint endPoint, DistributedMessage distributedMessage)
        {
            peers[endPoint].Send(distributedMessage);
            distributedMessage.Release();
        }

        /// <inheritdoc/>
        public void Broadcast(DistributedMessage distributedMessage)
        {
            foreach (var peer in peers)
                peer.Value.Send(distributedMessage);
            distributedMessage.Release();
        }

        /// <inheritdoc/>
        public IPeerManager GetConnectedPeerManager(IPEndPoint endPoint)
        {
            return peers.TryGetValue(endPoint, out var manager) ? manager : null;
        }

        /// <inheritdoc/>
        public void OnPeerConnected(NetPeer peer)
        {
            if (!peers.TryGetValue(peer.EndPoint, out var peerManager))
                peerManager = new LiteNetLibPeerManager(peer);
            PeerConnected?.Invoke(peerManager);
        }

        /// <inheritdoc/>
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            var disconnectedPeer = peers[peer.EndPoint];
            peers.Remove(peer.EndPoint);
            PeerDisconnected?.Invoke(disconnectedPeer);
        }

        /// <inheritdoc/>
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Log.Error($"[SERVER] Network error {socketError}");
        }

        /// <inheritdoc/>
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (MessageReceived == null)
                return;

            var data = reader.GetRemainingBytes();
            var message = MessagesPool.Instance.GetMessage(data.Length);
            message.Content.PushBytes(data);
            message.Sender = peers[peer.EndPoint];
            message.Type = GetDeliveryMethod(deliveryMethod);
            MessageReceived?.Invoke(message);
        }

        /// <inheritdoc/>
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
            UnconnectedMessageType messageType)
        {
            //TODO broadcast messages support
        }

        /// <inheritdoc/>
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        /// <inheritdoc/>
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey(LiteNetLibClient.ApplicationKey);
        }

        /// <inheritdoc/>
        public void WriteNet(NetLogLevel level, string str, params object[] args)
        {
            Log.Info(string.Format(str, args));
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
