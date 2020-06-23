/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Master
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using Core;
    using Core.Configs;
    using Core.Connection;
    using Core.Messaging;
    using Core.Messaging.Data;
    using Core.Threading;
    using LiteNetLib.Utils;
    using Shared;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Utilities;

    /// <summary>
    /// Simulation network master manager
    /// </summary>
    public class MasterManager : MonoBehaviour, IMessageSender, IMessageReceiver
    {
        /// <summary>
        /// Data about connection with a single client
        /// </summary>
        public class ClientConnection
        {
            public IPeerManager Peer { get; set; }
            public SimulationState State { get; set; }
        }

        /// <summary>
        /// Network settings for this simulation
        /// </summary>
        private NetworkSettings settings;

        /// <summary>
        /// Current state of the simulation on the server
        /// </summary>
        private SimulationState state;

        /// <summary>
        /// Root of the distributed objects
        /// </summary>
        private MasterObjectsRoot objectsRoot;

        /// <summary>
        /// Determines if time scale was locked from this script
        /// </summary>
        private bool timescaleLockCalled = false;
        
        /// <summary>
        /// Identifier of the last sent ping
        /// </summary>
        private int pingId = 0;

        /// <summary>
        /// Count of the received pongs for the last ping
        /// </summary>
        private int receivedPongs = 0;

        /// <summary>
        /// All current clients connected or trying to connect to the master
        /// </summary>
        private readonly List<ClientConnection> clients = new List<ClientConnection>();

        /// <summary>
        /// Packets processor used for objects deserialization
        /// </summary>
        public NetPacketProcessor PacketsProcessor { get; } = new NetPacketProcessor();

        /// <summary>
        /// Messages manager for incoming and outgoing messages via connection manager
        /// </summary>
        public MessagesManager MessagesManager { get; }

        /// <summary>
        /// Current state of the simulation on the server
        /// </summary>
        public SimulationState State
        {
            get => state;
            private set
            {
                if (state == value)
                    return;
                state = value;
                StateChanged?.Invoke(state);
            }
        }

        /// <summary>
        /// Connection manager for this server simulation
        /// </summary>
        private LiteNetLibServer ConnectionManager { get; } = new LiteNetLibServer();

        /// <inheritdoc />
        public string Key { get; } = "SimulationManager";

        /// <summary>
        /// All current clients connected or trying to connect to the master
        /// </summary>
        public List<ClientConnection> Clients
        {
            get => clients;
        }

        /// <summary>
        /// Root of the distributed objects
        /// </summary>
        public MasterObjectsRoot ObjectsRoot => objectsRoot;

        /// <summary>
        /// Event invoked when the simulation state changes
        /// </summary>
        public event Action<SimulationState> StateChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public MasterManager()
        {
            MessagesManager = new MessagesManager(ConnectionManager);
        }

        /// <summary>
        /// Unity Awake method
        /// </summary>
        private void Awake()
        {
            PacketsProcessor.SubscribeReusable<Commands.Pong, IPeerManager>(OnPongCommand);
            PacketsProcessor.SubscribeReusable<Commands.Ready, IPeerManager>(OnReadyCommand);
        }

        /// <summary>
        /// Unity LateUpdate method
        /// </summary>
        private void LateUpdate()
        {
            ConnectionManager.PoolEvents();
        }

        /// <summary>
        /// Unity OnApplicationQuit method
        /// </summary>
        private void OnApplicationQuit()
        {
            StopConnection();
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            StopConnection();
        }

        /// <summary>
        /// Initializes the simulation, adds <see cref="MasterObjectsRoot"/> component to the root game object
        /// </summary>
        /// <param name="rootGameObject">Root game object where new component will be added</param>
        public void InitializeSimulationScene(GameObject rootGameObject)
        {
            if (Loader.Instance.LoaderUI != null)
                Loader.Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.PROGRESS);
            if (ObjectsRoot != null)
                Log.Warning("Setting new master objects root, but previous one is still available on the scene.");
            objectsRoot = rootGameObject.AddComponent<MasterObjectsRoot>();
            ObjectsRoot.SetMessagesManager(MessagesManager);
            ObjectsRoot.SetSettings(settings);
            Log.Info($"{GetType().Name} was initialized and waits for all the clients.");
        }

        /// <summary>
        /// Sets network settings for this simulation
        /// </summary>
        /// <param name="networkSettings">Network settings to set</param>
        public void SetSettings(NetworkSettings networkSettings)
        {
            settings = networkSettings;
            if (ObjectsRoot != null)
                ObjectsRoot.SetSettings(settings);
        }

        /// <summary>
        /// Start the connection listening for incoming packets
        /// </summary>
        /// <param name="acceptableIdentifiers">Client identifiers in the cluster from which connection can be accepted</param>
        public void StartConnection(List<string> acceptableIdentifiers)
        {
            if (settings == null)
                throw new NullReferenceException("Set network settings before starting the connection.");
            MessagesManager.RegisterObject(this);
            ConnectionManager.AcceptableIdentifiers.AddRange(acceptableIdentifiers);
            ConnectionManager.Start(settings.ConnectionPort, settings.Timeout);
            ConnectionManager.PeerConnected += OnClientConnected;
            ConnectionManager.PeerDisconnected += OnClientDisconnected;
            State = SimulationState.Connecting;
            Log.Info($"{GetType().Name} started the connection manager.");
        }

        /// <summary>
        /// Stop the connection
        /// </summary>
        public void StopConnection()
        {
            DisconnectFromClients();
            State = SimulationState.Initial;
            ConnectionManager.PeerConnected -= OnClientConnected;
            ConnectionManager.PeerDisconnected -= OnClientDisconnected;
            ConnectionManager.Stop();
            ConnectionManager.AcceptableIdentifiers.Clear();
            MessagesManager.UnregisterObject(this);
            Log.Info($"{GetType().Name} stopped the connection manager.");
        }

        /// <summary>
        /// Disconnects from all the clients
        /// </summary>
        public void DisconnectFromClients()
        {
            if (Clients == null || Clients.Count == 0)
                return;
            foreach (var client in Clients.Where(client => client.Peer.Connected))
                client.Peer.Disconnect();
            State = SimulationState.Initial;
            Log.Info($"{GetType().Name} disconnected from all the clients.");
        }

        /// <summary>
        /// Tries to start the simulation
        /// </summary>
        public void TryStartSimulation()
        {
            if (!Loader.Instance.Network.IsSimulationReady || State != SimulationState.Connected ||
                clients.Any(c => c.State != SimulationState.Ready)) return;
            State = SimulationState.Ready;
            RunSimulation();
        }

        /// <summary>
        /// Method invoked when connection with a client is established
        /// </summary>
        /// <param name="clientPeerManager">Connected client peer manager</param>
        private void OnClientConnected(IPeerManager clientPeerManager)
        {
            Log.Info($"Client connected: {clientPeerManager.PeerEndPoint.ToString()}");
            var client = new ClientConnection
            {
                Peer = clientPeerManager,
                State = SimulationState.Connected
            };
            Clients.Add(client);

            if (State == SimulationState.Connecting)
            {
                var requiredClients = Loader.Instance.Network.ClusterData.Instances.Length - 1;
                if (Clients.Count == requiredClients)
                    State = SimulationState.Connected;
            }
        }

        /// <summary>
        /// Method invoked when connection with a client is established
        /// </summary>
        /// <param name="clientPeerManager">Connected client peer manager</param>
        private void OnClientDisconnected(IPeerManager clientPeerManager)
        {
            var client = Clients.Find(c => c.Peer == clientPeerManager);
            Log.Info($"{GetType().Name} disconnected from the client with address '{client.Peer.PeerEndPoint.ToString()}'.");
            Debug.Assert(client != null);
            Clients.Remove(client);

            if (Loader.Instance.CurrentSimulation != null && State != SimulationState.Initial)
            {
                Log.Warning("Stopping current cluster simulation as one connection with client has been lost.");
                Loader.StopAsync();
            }

            State = SimulationState.Initial;
        }

        /// <summary>
        /// Checks if the master is connected to the client
        /// </summary>
        /// <param name="endPoint">Endpoint of the checked client</param>
        /// <returns>True if the master is connected to the client, false otherwise</returns>
        public bool IsConnectedToClient(IPEndPoint endPoint)
        {
            return endPoint != null && ConnectionManager.GetConnectedPeerManager(endPoint) != null;
        }

        /// <inheritdoc/>
        public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
        {
            MessagesManager.UnicastMessage(endPoint, distributedMessage);
        }

        /// <inheritdoc/>
        public void BroadcastMessage(DistributedMessage distributedMessage)
        {
            MessagesManager.BroadcastMessage(distributedMessage);
        }

        /// <inheritdoc/>
        void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
        {
        }

        /// <inheritdoc/>
        public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
        {
            PacketsProcessor.ReadAllPackets(new NetDataReader(distributedMessage.Content.GetDataCopy()), sender);
        }

        /// <summary>
        /// Method invoked when manager receives ready result command
        /// </summary>
        /// <param name="ready">Received ready result command</param>
        /// <param name="peer">Peer which has sent the command</param>
        public void OnReadyCommand(Commands.Ready ready, IPeerManager peer)
        {
            var client = clients.First(c => c.Peer == peer);
            if (client == null)
            {
                Log.Warning("Received ready command from a unconnected client.");
                return;
            }

            client.State = SimulationState.Ready;
            TryStartSimulation();

        }

        /// <summary>
        /// Method invoked when manager receives pong result command
        /// </summary>
        /// <param name="res">Received pong result command</param>
        /// <param name="peer">Peer which has sent the command</param>
        public void OnPongCommand(Commands.Pong res, IPeerManager peer)
        {
            if (res.Id == pingId) receivedPongs++;
        }

        /// <summary>
        /// Broadcast the run command to all clients' simulations and runs the simulation
        /// </summary>
        public void RunSimulation()
        {
            Log.Info($"{GetType().Name} runs the prepared simulation and broadcasts run command.");

            var stopData = PacketsProcessor.Write(new Commands.Run());
            var message = MessagesPool.Instance.GetMessage(stopData.Length);
            message.AddressKey = Key;
            message.Content.PushBytes(stopData);
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
            
            Loader.StartAsync(Loader.Instance.Network.CurrentSimulation);
            State = SimulationState.Running;
        }

        /// <summary>
        /// Broadcast the stop command to all clients' simulations and reverse changes in the Simulation
        /// </summary>
        public void SimulationStopped()
        {
            Log.Info($"{GetType().Name} broadcasts the simulation stop command.");

            var stopData = PacketsProcessor.Write(new Commands.Stop());
            var message = MessagesPool.Instance.GetMessage(stopData.Length);
            message.AddressKey = Key;
            message.Content.PushBytes(stopData);
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
            ThreadingUtilities.DispatchToMainThread(RevertChangesInSimulator);
        }

        /// <summary>
        /// Revers changes made in the Simulator manager after the distribution simulation is over
        /// </summary>
        private void RevertChangesInSimulator()
        {
            Log.Info($"{GetType().Name} reverts the changes done in the simulator.");
            DisconnectFromClients();
            if (timescaleLockCalled)
            {
                SimulatorManager.Instance.TimeManager.TimeScaleSemaphore.Unlock();
                timescaleLockCalled = false;
                Log.Info($"{GetType().Name} unlocks the simulator timescale.");
            }
        }

        /// <summary>
        /// Sends a ping command to all the connected clients
        /// </summary>
        public void SendPing()
        {
            receivedPongs = 0;
            var stopData = PacketsProcessor.Write(new Commands.Ping() { Id = ++pingId});
            var message = MessagesPool.Instance.GetMessage(stopData.Length);
            message.AddressKey = Key;
            message.Content.PushBytes(stopData);
            message.Type = DistributedMessageType.Unreliable;
            BroadcastMessage(message);
        }

        /// <summary>
        /// Checks if manager received all pongs for the last ping command
        /// </summary>
        /// <returns>True if master received all pongs for the last ping command</returns>
        public bool ReceivedAllPongs()
        {
            return receivedPongs == clients.Count;
        }
    }
}