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
    using Messaging.Data;

    /// <summary>
    /// Connection manager responsible for all connected peers
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Is this server connection manager
        /// </summary>
        bool IsServer { get; }
        
        /// <summary>
        /// Port used for the connections
        /// </summary>
        int Port { get; }
        
        /// <summary>
        /// Timeout value in milliseconds
        /// </summary>
        int Timeout { get; }
        
        /// <summary>
        /// Count of currently connected peers
        /// </summary>
        int ConnectedPeersCount { get; }

        /// <summary>
        /// List of the identifiers which connection can be accepted
        /// </summary>
        List<string> AcceptableIdentifiers { get; }

        /// <summary>
        /// Event invoked when new peer has been connected to the manager
        /// </summary>
        event Action<IPeerManager> PeerConnected;

        /// <summary>
        /// Event invoked when peer has been disconnected from the manager
        /// </summary>
        event Action<IPeerManager> PeerDisconnected;

        /// <summary>
        /// Event invoked when any peer had received message
        /// </summary>
        event Action<DistributedMessage> MessageReceived;

        /// <summary>
        /// Starts the manager, begins listening for events on given port
        /// </summary>
        /// <param name="port">Listening port for the connection</param>
        /// <param name="timeout">Timeout value in milliseconds</param>
        /// <returns>True if manager was started, false if start failed</returns>
        bool Start(int port, int timeout);

        /// <summary>
        /// Stop the connection manager and all established connections
        /// </summary>
        void Stop();

        /// <summary>
        /// Asynchronously connects to the peer at given address and port
        /// </summary>
        /// <param name="endPoint">End point of target peer</param>
        /// <param name="identifier">Identifier of the peer requesting the connection</param>
        IPeerManager Connect(IPEndPoint endPoint, string identifier);

        /// <summary>
        /// Receive all pending events. Call this in application main loop update.
        /// </summary>
        void PoolEvents();

        /// <summary>
        /// Sends the message to peer at the given address
        /// </summary>
        /// <param name="endPoint">End point of target peer</param>
        /// <param name="distributedMessage">Message to be sent</param>
        void Unicast(IPEndPoint endPoint, DistributedMessage distributedMessage);

        /// <summary>
        /// Sends the message to all connected peers
        /// </summary>
        /// <param name="distributedMessage">Message to be sent</param>
        void Broadcast(DistributedMessage distributedMessage);

        /// <summary>
        /// Gets the peer manager for the given address
        /// </summary>
        /// <param name="endPoint">End point of target peer</param>
        /// <returns>ConnectedPeerManager if connected with peer with the address, null otherwise</returns>
        IPeerManager GetConnectedPeerManager(IPEndPoint endPoint);
    }
}