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
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using LiteNetLib;
    using LiteNetLib.Utils;
    using Messaging;
    using Messaging.Data;

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
        private readonly Dictionary<IPEndPoint, IPeerManager> peers =
            new Dictionary<IPEndPoint, IPeerManager>();

        /// <inheritdoc/>
        public bool IsServer => true;

        /// <inheritdoc/>
        public int Port { get; set; }

        /// <inheritdoc/>
        public int Timeout { get; private set; }

        /// <inheritdoc/>
        public int ConnectedPeersCount => peers.Count;

        /// <inheritdoc/>
        public List<string> AcceptableIdentifiers { get; } = new List<string>();

        /// <inheritdoc/>
        public Dictionary<IPEndPoint, IPeerManager> ConnectedPeers => peers;

        /// <inheritdoc/>
        public event Action<IPeerManager> PeerConnected;

        /// <inheritdoc/>
        public event Action<IPeerManager> PeerDisconnected;

        /// <inheritdoc/>
        public event Action<DistributedMessage> MessageReceived;

        /// <inheritdoc/>
        public bool Start(int timeout)
        {
            Timeout = timeout;
            NetDebug.Logger = this;
            netServer = new NetManager(this)
                {BroadcastReceiveEnabled = false, UpdateTime = 5, DisconnectTimeout = timeout, AutoRecycle = true};
            var result = netServer.Start(Port);
            if (result)
                Log.Info($"{GetType().Name} started using the port '{Port}'.");
            else
                Log.Error($"{GetType().Name} failed to start using the port '{Port}'.");
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
            var writer = new NetDataWriter();
            writer.Put(ApplicationKey);
            writer.Put(identifier);
            var peerManager = new LiteNetLibPeerManager(netServer.Connect(endPoint, writer));
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
            if (netServer != null)
            {
                var bytesStack = distributedMessage.Content;
                try
                {
                    NetworkStatistics.ReportSentPackage(bytesStack.Count);
                    netServer.SendToAll(bytesStack.RawData, 0, bytesStack.Count,
                        LiteNetLibPeerManager.GetDeliveryMethod(distributedMessage.Type));
                }
                catch (TooBigPacketException)
                {
                    Log.Error($"Too large message to be sent: {bytesStack.Count}.");
                }
            }

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
            //Check if the peer was accepted and initialized
            if (!peers.TryGetValue(peer.EndPoint, out var peerManager))
                return;

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
            {
                request.Reject();
                Log.Warning(
                    $"{GetType().Name} received and rejected a connection request from address '{request.RemoteEndPoint.Address}', invalid key was passed: {key}, current UTC time: {DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}.");
                return;
            }

            var identifier = request.Data.GetString();
            var peerConnected = peers.Any(peer => peer.Value.Identifier == identifier);
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
            peers.Add(request.RemoteEndPoint, new LiteNetLibPeerManager(request.Accept(), identifier));
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
            return (DistributedMessageType) deliveryMethod;
        }
    }
}