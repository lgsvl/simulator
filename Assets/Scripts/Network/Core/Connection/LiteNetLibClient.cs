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
    using System.Globalization;
    using System.Net;
    using System.Net.Sockets;
    using LiteNetLib;
    using LiteNetLib.Utils;
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
        public const string ApplicationKey = "LGSVL";

        /// <summary>
        /// The net manager for this client
        /// </summary>
        private NetManager netClient;

        /// <summary>
        /// Currently active connections made by this connection manager
        /// </summary>
        private Dictionary<IPEndPoint, LiteNetLibPeerManager> activeConnections =
            new Dictionary<IPEndPoint, LiteNetLibPeerManager>();

        /// <summary>
        /// Peer manager for connection with the server
        /// </summary>
        private LiteNetLibPeerManager masterPeer;

        /// <inheritdoc/>
        public bool IsServer => false;

        /// <inheritdoc/>
        public int Port { get; private set; }

        /// <inheritdoc/>
        public int Timeout { get; private set; }

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
        public List<string> AcceptableIdentifiers { get; } = new List<string>();

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
        public bool Start(int port, int timeout)
        {
            Port = port;
            Timeout = timeout;
            netClient = new NetManager(this)
            {
                UnconnectedMessagesEnabled = false, UpdateTime = 5, DisconnectTimeout = timeout,
                AutoRecycle = true
            };
            var result = NetClient.Start(port);
            if (result)
                Log.Info($"{GetType().Name} started using the port '{port}'.");
            else
                Log.Error($"{GetType().Name} failed to start using the port '{port}'.");
            return result;
        }

        /// <inheritdoc/>
        public void Stop()
        {
            NetClient?.Stop();
            Log.Info($"{GetType().Name} was stopped.");
        }

        /// <inheritdoc/>
        public IPeerManager Connect(IPEndPoint endPoint, string peerIdentifier)
        {
            if (activeConnections.ContainsKey(endPoint))
            {
                Log.Warning($"{GetType().Name} already got a connection active to the endpoint '{endPoint}'.");
                return null;
            }

            var writer = new NetDataWriter();
            writer.Put(ApplicationKey);
            writer.Put(peerIdentifier);
            var peer = new LiteNetLibPeerManager(NetClient.Connect(endPoint, writer));
            activeConnections.Add(endPoint, peer);
            Log.Info(
                $"{GetType().Name} tries to connect with a peer at address '{endPoint}, current UTC time: {DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}.");
            return peer;
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
            var bytesStack = distributedMessage.Content;
            try
            {
                NetworkStatistics.ReportSentPackage(bytesStack.Count);
                NetClient.SendToAll(bytesStack.RawData, 0, bytesStack.Count,
                    LiteNetLibPeerManager.GetDeliveryMethod(distributedMessage.Type));
            }
            catch (TooBigPacketException)
            {
                Log.Error($"Too large message to be sent: {bytesStack.Count}.");
            }

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
            if (masterPeer != null)
                return;

            masterPeer = activeConnections[peer.EndPoint];
            PeerConnected?.Invoke(masterPeer);
        }

        /// <inheritdoc/>
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (masterPeer == null || masterPeer.Peer != peer)
                return;
            var disconnectedPeer = masterPeer;
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
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
            UnconnectedMessageType messageType)
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
            Log.Error(
                $"{GetType().Name} received an connection request from address '{request.RemoteEndPoint.Address}' but client simulation cannot accept requests.");
            request.Reject();
        }

        /// <summary>
        /// Gets MessageType corresponding to the given LiteNetLib DeliveryMethod
        /// </summary>
        /// <param name="deliveryMethod">Delivery method</param>
        /// <returns>Corresponding MessageType to the given LiteNetLib DeliveryMethod</returns>
        public static DistributedMessageType GetDeliveryMethod(DeliveryMethod deliveryMethod)
        {
            return (DistributedMessageType) deliveryMethod;
        }
    }
}