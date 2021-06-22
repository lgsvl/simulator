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
    using Messaging;
    using Messaging.Data;

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
        private readonly Dictionary<IPEndPoint, IPeerManager> peers =
            new Dictionary<IPEndPoint, IPeerManager>();

        /// <summary>
        /// Peer manager for connection with the server
        /// </summary>
        private IPeerManager masterPeer;

        /// <inheritdoc/>
        public bool IsServer => false;

        /// <inheritdoc/>
        public int Port { get; set; } = 0;

        /// <inheritdoc/>
        public int Timeout { get; private set; }

        /// <inheritdoc/>
        public int ConnectedPeersCount => MasterPeer == null ? 0 : 1;

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
        public Dictionary<IPEndPoint, IPeerManager> ConnectedPeers => peers;
        
        /// <summary>
        /// Peer manager for connection with the server
        /// </summary>
        public IPeerManager MasterPeer => masterPeer;

        /// <inheritdoc/>
        public event Action<IPeerManager> PeerConnected;

        /// <inheritdoc/>
        public event Action<IPeerManager> PeerDisconnected;

        /// <summary>
        /// Event invoked when all active connections have been dropped and none was accepted
        /// </summary>
        public event Action DroppedAllConnections;

        /// <inheritdoc/>
        public event Action<DistributedMessage> MessageReceived;

        /// <summary>
        /// Event invoked when the latency changes
        /// </summary>
        public event Action<int> LatencyUpdated;

        /// <inheritdoc/>
        public bool Start(int timeout)
        {
            Timeout = timeout;
            netClient = new NetManager(this)
            {
                UnconnectedMessagesEnabled = false, UpdateTime = 5, DisconnectTimeout = timeout,
                AutoRecycle = true
            };
            var result = Port == 0 ? NetClient.Start() : NetClient.Start(Port);
            if (result)
            {
                var portLog = Port == 0 ? $" Using local port '{NetClient.LocalPort}'" : "";
                Log.Info($"{GetType().Name} started.{portLog}");
            }
            else
                Log.Error($"{GetType().Name} failed to start using local port '{NetClient.LocalPort}'.");
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
            if (peers.ContainsKey(endPoint))
            {
                Log.Warning($"{GetType().Name} already got a connection active to the endpoint '{endPoint}'.");
                return null;
            }

            var writer = new NetDataWriter();
            writer.Put(ApplicationKey);
            writer.Put(peerIdentifier);
            var peer = new LiteNetLibPeerManager(NetClient.Connect(endPoint, writer));
            peers.Add(endPoint, peer);
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
            if (Equals(MasterPeer.PeerEndPoint, endPoint))
                MasterPeer.Send(distributedMessage);
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
            return (MasterPeer!=null && Equals(MasterPeer.PeerEndPoint, endPoint)) ? MasterPeer : null;
        }

        /// <inheritdoc/>
        public void OnPeerConnected(NetPeer peer)
        {
            if (MasterPeer != null)
            {
                peers.Remove(peer.EndPoint);
                return;
            }
            if (!peers.TryGetValue(peer.EndPoint, out masterPeer))
            {
                masterPeer = new LiteNetLibPeerManager(peer);
                peers.Add(peer.EndPoint, MasterPeer);
            }

            Log.Info($"{GetType().Name} has connected to the master peer '{MasterPeer.PeerEndPoint}'.");
            PeerConnected?.Invoke(MasterPeer);
        }

        /// <inheritdoc/>
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (MasterPeer == null || !Equals(MasterPeer.PeerEndPoint, peer.EndPoint))
            {
                if (peers.TryGetValue(peer.EndPoint, out var peerManager))
                    PeerDisconnected?.Invoke(peerManager);
                peers.Remove(peer.EndPoint);
                if (peers.Count==0)
                    DroppedAllConnections?.Invoke();
                return;
            }
            var disconnectedPeer = MasterPeer;
            masterPeer = null;
            peers.Remove(peer.EndPoint);
            PeerDisconnected?.Invoke(disconnectedPeer);
        }

        /// <inheritdoc/>
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            if (MasterPeer!=null && Equals(endPoint.Address, MasterPeer.PeerEndPoint.Address))
                Log.Error("[Client] Network error: " + socketError);
            else
                peers.Remove(endPoint);
        }

        /// <inheritdoc/>
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (MessageReceived == null)
                return;

            var availableBytes = reader.AvailableBytes;
            var message = MessagesPool.Instance.GetMessage(availableBytes);
            message.Content.PushBytes(reader.RawData, reader.Position, availableBytes);
            message.Sender = MasterPeer;
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
            var key = request.Data.GetString();
            if (ApplicationKey != key)
            {
                request.Reject();
                Log.Warning(
                    $"{GetType().Name} received and rejected a connection request from address '{request.RemoteEndPoint.Address}', invalid key was passed: {key}, current UTC time: {DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}.");
                return;
            }

            var identifier = request.Data.GetString();
            var peerConnected = peers.ContainsKey(request.RemoteEndPoint);
            if (peerConnected)
            {
                //Connection to same peer is already established, probably request send from other sub-network
                request.Reject();
                return;
            }

            var acceptIdentifier = AcceptableIdentifiers.Contains(identifier);
            if (!acceptIdentifier)
            {
                request.Reject();
                Log.Warning(
                    $"{GetType().Name} received and rejected a connection request from address '{request.RemoteEndPoint.Address}', unacceptable identifier was passed: {identifier}, current UTC time: {DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}.");
                return;
            }

            Log.Info(
                $"{GetType().Name} received and accepted a connection request from address '{request.RemoteEndPoint.Address}', current UTC time: {DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}.");
            request.Accept();
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