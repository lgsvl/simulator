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
    using System.Linq;
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
        public const string ApplicationKey = "LGSVL";

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
        public List<string> AcceptableIdentifiers { get; } = new List<string>();

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
            var result = netServer.Start(port);
            if (result)
                Log.Info($"{GetType().Name} started using the port '{port}'.");
            else 
                Log.Error($"{GetType().Name} failed to start using the port '{port}'.");
            return result;
        }

        /// <inheritdoc/>
        public void Stop()
        {
            NetDebug.Logger = null;
            netServer?.Stop();
            Log.Info($"{GetType().Name} was stopped.");
        }

        /// <inheritdoc/>
        public IPeerManager Connect(IPEndPoint endPoint, string identifier)
        {
            var peerManager = new LiteNetLibPeerManager(netServer.Connect(endPoint, identifier));
            peers.Add(peerManager.PeerEndPoint, peerManager);
            Log.Info($"{GetType().Name} starts the connection to a peer with address '{endPoint.ToString()}'.");
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
            {
                peerManager = new LiteNetLibPeerManager(peer);
                peers.Add(peer.EndPoint, peerManager);
            }

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
            var key = request.Data.GetString();
            if (ApplicationKey != key)
                return;
            var identifier = request.Data.GetString();
            var peerConnected = peers.Any(peer => peer.Value.Identifier == identifier);
            if (peerConnected)
                return;
            var acceptIdentifier = AcceptableIdentifiers.Contains(identifier);
            if (!acceptIdentifier)
                return;
            Log.Info($"{GetType().Name} received and accepted an connection request from address '{request.RemoteEndPoint.Address}'.");
            request.Accept();
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
