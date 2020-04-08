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
        /// Simulation configuration
        /// </summary>
        public SimulationConfig Simulation { get; set; }

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
            PacketsProcessor.RegisterNestedType(SerializationHelpers.SerializeLoadAgent,
                SerializationHelpers.DeserializeLoadAgent);
            PacketsProcessor.SubscribeReusable<Commands.Info, IPeerManager>(OnInfoCommand);
            PacketsProcessor.SubscribeReusable<Commands.LoadResult, IPeerManager>(OnLoadResultCommand);
            PacketsProcessor.SubscribeReusable<Commands.Pong, IPeerManager>(OnPongCommand);
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
        public void InitializeSimulation(GameObject rootGameObject)
        {
            if (Loader.Instance.LoaderUI != null)
                Loader.Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.PROGRESS);
            if (ObjectsRoot != null)
                Log.Warning("Setting new master objects root, but previous one is still available on the scene.");
            objectsRoot = rootGameObject.AddComponent<MasterObjectsRoot>();
            ObjectsRoot.SetMessagesManager(MessagesManager);
            ObjectsRoot.SetSettings(settings);
            if (clients.Count == 0)
            {
                if (!timescaleLockCalled)
                {
                    SimulatorManager.Instance.TimeManager.TimeScaleSemaphore.Lock();
                    timescaleLockCalled = true;
                }
                ConnectToClients();
            }
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
        public void StartConnection()
        {
            if (settings == null)
                throw new NullReferenceException("Set network settings before starting the connection.");
            MessagesManager.RegisterObject(this);
            ConnectionManager.Start(settings.ConnectionPort);
            ConnectionManager.PeerConnected += OnClientConnected;
            ConnectionManager.PeerDisconnected += OnClientDisconnected;
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
            MessagesManager.UnregisterObject(this);
        }

        /// <summary>
        /// Tries to connect to the clients defined in the simulation config clusters
        /// </summary>
        public void ConnectToClients()
        {
            Debug.Assert(State == SimulationState.Initial);
            foreach (var address in Simulation.Clusters)
            {
                Log.Info($"Trying to connect to the address: {address}");
                var endPoint = new IPEndPoint(IPAddress.Parse(address), settings.ConnectionPort);
                var peer = ConnectionManager.Connect(endPoint);
                Clients.Add(new ClientConnection() {Peer = peer, State = SimulationState.Initial});
            }

            State = SimulationState.Connecting;
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
        }

        /// <summary>
        /// Method invoked when connection with a client is established
        /// </summary>
        /// <param name="clientPeerManager">Connected client peer manager</param>
        private void OnClientConnected(IPeerManager clientPeerManager)
        {
            Log.Info($"Client connected: {clientPeerManager.PeerEndPoint.Address}");
            Debug.Assert(State == SimulationState.Connecting);
            var client = Clients.Find(c => c.Peer == clientPeerManager);
            Debug.Assert(client != null);
            Debug.Assert(client.State == SimulationState.Initial);

            client.State = SimulationState.Connecting;
        }

        /// <summary>
        /// Method invoked when connection with a client is established
        /// </summary>
        /// <param name="clientPeerManager">Connected client peer manager</param>
        private void OnClientDisconnected(IPeerManager clientPeerManager)
        {
            var client = Clients.Find(c => c.Peer == clientPeerManager);
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
        /// Method invoked when manager receives info command
        /// </summary>
        /// <param name="info">Received info command</param>
        /// <param name="peer">Peer which has sent the command</param>
        public void OnInfoCommand(Commands.Info info, IPeerManager peer)
        {
            Debug.Assert(State == SimulationState.Connecting);
            var client = Clients.Find(c => c.Peer == peer);
            Debug.Assert(client != null);
            Debug.Assert(client.State == SimulationState.Connecting);

            Log.Info($"NET: Client connected from {peer.PeerEndPoint}");

            Log.Info($"NET: Client version = {info.Version}");
            Log.Info($"NET: Client Unity version = {info.UnityVersion}");
            Log.Info($"NET: Client OS = {info.OperatingSystem}");

            client.State = SimulationState.Connected;

            var sim = Loader.Instance.SimConfig;

            if (Clients.All(c => c.State == SimulationState.Connected))
            {
                var load = new Commands.Load()
                {
                    UseSeed = sim.Seed != null,
                    Seed = sim.Seed ?? 0,
                    Name = sim.Name,
                    MapName = sim.MapName,
                    MapUrl = sim.MapUrl,
                    ApiOnly = sim.ApiOnly,
                    Headless = sim.Headless,
                    Interactive = false,
                    TimeOfDay = sim.TimeOfDay.ToString("o", CultureInfo.InvariantCulture),
                    Rain = sim.Rain,
                    Fog = sim.Fog,
                    Wetness = sim.Wetness,
                    Cloudiness = sim.Cloudiness,
                    Agents = Simulation.Agents.Select(a => new Commands.LoadAgent()
                    {
                        Name = a.Name,
                        Url = a.Url,
                        Bridge = a.Bridge == null ? string.Empty : a.Bridge.Name,
                        Connection = a.Connection,
                        Sensors = a.Sensors,
                    }).ToArray(),
                    UseTraffic = false,
                    UsePedestrians = false
//                        UseTraffic = Simulation.UseTraffic,
//                        UsePedestrians = Simulation.UsePedestrians,
                };

                foreach (var c in Clients)
                {
                    var loadData = PacketsProcessor.Write(load);
                    var message = MessagesPool.Instance.GetMessage(loadData.Length);
                    message.AddressKey = Key;
                    message.Content.PushBytes(loadData);
                    message.Type = DistributedMessageType.ReliableOrdered;
                    UnicastMessage(c.Peer.PeerEndPoint, message);
                    c.State = SimulationState.Loading;
                }

                State = SimulationState.Loading;
            }
        }

        /// <summary>
        /// Method invoked when manager receives load result command
        /// </summary>
        /// <param name="res">Received load result command</param>
        /// <param name="peer">Peer which has sent the command</param>
        public void OnLoadResultCommand(Commands.LoadResult res, IPeerManager peer)
        {
            Debug.Assert(State == SimulationState.Loading);
            var client = Clients.Find(c => c.Peer == peer);
            Debug.Assert(client != null);
            Debug.Assert(client.State == SimulationState.Loading);

            if (res.Success)
            {
                Log.Info("Client loaded");
            }
            else
            {
                // TODO: stop simulation / cancel loading for other clients
                Log.Error($"Failed to start '{Simulation.Name}' simulation. Client failed to load: {res.ErrorMessage}");

                // TODO: reset all other clients

                // TODO: update simulation status in DB
                // simulation.Status = "Invalid";
                // db.Update(simulation);

                // NotificationManager.SendNotification("simulation", SimulationResponse.Create(simulation), simulation.Owner);

                Loader.ResetLoaderScene();

                DisconnectFromClients();
                Clients.Clear();
                return;
            }

            client.State = SimulationState.Ready;

            if (Clients.All(c => c.State == SimulationState.Ready))
            {
                Log.Info("All clients are ready. Resuming time.");

                var run = new Commands.Run();
                foreach (var c in Clients)
                {
                    var runData = PacketsProcessor.Write(run);
                    var message = MessagesPool.Instance.GetMessage(runData.Length);
                    message.AddressKey = Key;
                    message.Content.PushBytes(runData);
                    message.Type = DistributedMessageType.ReliableOrdered;
                    UnicastMessage(c.Peer.PeerEndPoint, message);
                    c.State = SimulationState.Running;
                }

                State = SimulationState.Running;

                // Notify WebUI simulation is running
                Loader.Instance.CurrentSimulation.Status = "Running";

                // Flash main window to let user know simulation is ready
                WindowFlasher.Flash();

                if (Loader.Instance.LoaderUI != null)
                {
                    Loader.Instance.LoaderUI.SetLoaderUIState(LoaderUI.LoaderUIStateType.READY);
                    Loader.Instance.LoaderUI.DisableUI();
                }

                SceneManager.UnloadSceneAsync(Loader.Instance.LoaderScene);
                if (timescaleLockCalled)
                {
                    SimulatorManager.Instance.TimeManager.TimeScaleSemaphore.Unlock();
                    timescaleLockCalled = false;
                }
            }
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
        /// Broadcast the stop command to all clients' simulations
        /// </summary>
        public void BroadcastSimulationStop()
        {
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
            DisconnectFromClients();
            if (timescaleLockCalled)
            {
                SimulatorManager.Instance.TimeManager.TimeScaleSemaphore.Unlock();
                timescaleLockCalled = false;
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