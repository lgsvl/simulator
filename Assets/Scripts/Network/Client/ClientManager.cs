/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Client
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Net;
    using System.Text;
    using Core;
    using Core.Configs;
    using Core.Connection;
    using Core.Messaging;
    using Core.Messaging.Data;
    using LiteNetLib.Utils;
    using Shared;
    using UnityEngine;

    /// <summary>
    /// Simulation network client manager
    /// </summary>
    public class ClientManager : MonoBehaviour, IMessageSender, IMessageReceiver
    {
        /// <summary>
        /// Network settings for this simulation
        /// </summary>
        private NetworkSettings settings;

        /// <summary>
        /// Root of the mocked objects
        /// </summary>
        private ClientObjectsRoot objectsRoot;

        /// <summary>
        /// Timeout coroutine, that stops the simulator if connecting fails
        /// </summary>
        private IEnumerator timeoutCoroutine;

        /// <summary>
        /// Coroutine to iterate master ip adresses to try
        /// </summary>
        private Coroutine masterEndpointsCouroutine;

        /// <summary>
        /// Count of current connection requests to the main ip address
        /// </summary>
        private int connectionRequests;

        /// <summary>
        /// Current state of the simulation
        /// </summary>
        private SimulationState State { get; set; } = SimulationState.Initial;

        /// <inheritdoc />
        public string Key { get; } = "SimulationManager";

        /// <summary>
        /// Packets processor used for objects deserialization
        /// </summary>
        private NetPacketProcessor PacketsProcessor { get; } = new NetPacketProcessor();

        /// <summary>
        /// Messages manager for incoming and outgoing messages via connection manager
        /// </summary>
        public MessagesManager MessagesManager { get; }

        /// <summary>
        /// Connection manager for this server simulation
        /// </summary>
        public LiteNetLibClient Connection { get; } = new LiteNetLibClient();

        /// <summary>
        /// Cached connection manager to the master peer
        /// </summary>
        private IPeerManager MasterPeer { get; set; }

        /// <summary>
        /// Root of the mocked objects
        /// </summary>
        public ClientObjectsRoot ObjectsRoot => objectsRoot;

        /// <summary>
        /// Checks if this client is currently connected to the master peer
        /// </summary>
        public bool IsConnected => MasterPeer != null && MasterPeer.Connected;

        /// <summary>
        /// Constructor
        /// </summary>
        public ClientManager()
        {
            MessagesManager = new MessagesManager(Connection);
        }

        /// <summary>
        /// Unity Awake method
        /// </summary>
        private void Awake()
        {
            PacketsProcessor.SubscribeReusable<Commands.Run>(OnRunCommand);
            PacketsProcessor.SubscribeReusable<Commands.Stop>(OnStopCommand);
            PacketsProcessor.SubscribeReusable<Commands.EnvironmentState>(OnEnvironmentStateCommand);
            PacketsProcessor.SubscribeReusable<Commands.Ping>(OnPingCommand);

            SetCollisionBetweenSimulationObjects(false);
        }

        /// <summary>
        /// Unity LateUpdate method
        /// </summary>
        private void LateUpdate()
        {
            Connection.PoolEvents();
        }

        /// <summary>
        /// Unity OnApplicationQuit method
        /// </summary>
        private void OnApplicationQuit()
        {
            SetCollisionBetweenSimulationObjects(true);
            StopConnection();
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            SetCollisionBetweenSimulationObjects(true);
            StopConnection();
            PacketsProcessor.RemoveSubscription<Commands.Run>();
            PacketsProcessor.RemoveSubscription<Commands.Stop>();
            PacketsProcessor.RemoveSubscription<Commands.EnvironmentState>();
            PacketsProcessor.RemoveSubscription<Commands.Ping>();
        }

        /// <summary>
        /// Sets collisions between simulation objects
        /// </summary>
        /// <param name="collision">Should the collision be enabled</param>
        private void SetCollisionBetweenSimulationObjects(bool collision)
        {
            Log.Info($"{GetType().Name} overrides the collision matrix between layers.");
            var agentLayer = LayerMask.NameToLayer("Agent");
            var npcLayer = LayerMask.NameToLayer("NPC");
            var pedestrianLayer = LayerMask.NameToLayer("Pedestrian");
            Physics.IgnoreLayerCollision(agentLayer, agentLayer, !collision);
            Physics.IgnoreLayerCollision(agentLayer, npcLayer, !collision);
            Physics.IgnoreLayerCollision(agentLayer, pedestrianLayer, !collision);
            Physics.IgnoreLayerCollision(npcLayer, npcLayer, !collision);
            Physics.IgnoreLayerCollision(npcLayer, pedestrianLayer, !collision);
            Physics.IgnoreLayerCollision(pedestrianLayer, pedestrianLayer, !collision);
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
        /// Initializes the simulation, adds <see cref="ClientObjectsRoot"/> component to the root game object
        /// </summary>
        /// <param name="rootGameObject">Root game object where new component will be added</param>
        public void InitializeSimulationScene(GameObject rootGameObject)
        {
            objectsRoot = rootGameObject.AddComponent<ClientObjectsRoot>();
            ObjectsRoot.SetMessagesManager(MessagesManager);
            ObjectsRoot.SetSettings(settings);
            Log.Info($"{GetType().Name} initialized the simulation.");
            SendLoadedCommand();
            State = SimulationState.Running;
        }

        /// <summary>
        /// Start the connection listening for incoming packets
        /// </summary>
        public void StartConnection()
        {
            if (settings == null)
                throw new NullReferenceException("Set network settings before starting the connection.");
            MessagesManager.RegisterObject(this);
            Connection.Start(settings.Timeout);
            Connection.PeerConnected += OnPeerConnected;
            Connection.PeerDisconnected += OnPeerDisconnected;
            Log.Info($"{GetType().Name} started the connection manager.");
        }

        /// <summary>
        /// Stop the connection
        /// </summary>
        public void StopConnection()
        {
            if (State == SimulationState.Initial)
                return;
            State = SimulationState.Initial;
            Connection.PeerConnected -= OnPeerConnected;
            Connection.PeerDisconnected -= OnPeerDisconnected;
            Connection.Stop();
            MessagesManager.UnregisterObject(this);
            if (timeoutCoroutine != null)
            {
                StopCoroutine(timeoutCoroutine);
                timeoutCoroutine = null;
            }

            Log.Info($"{GetType().Name} stopped the connection manager.");
        }

        /// <summary>
        /// Method invoked when new peer connects
        /// </summary>
        /// <param name="peer">Peer that has connected</param>
        public void OnPeerConnected(IPeerManager peer)
        {
            connectionRequests--;
            Debug.Assert(State == SimulationState.Connecting);
            StopCoroutine(masterEndpointsCouroutine);
            MasterPeer = peer;

            Log.Info($"Peer {peer.PeerEndPoint} connected.");

            State = SimulationState.Connected;
            if (Loader.Instance.Network.IsSimulationReady)
                SendReadyCommand();
        }

        /// <summary>
        /// Method invoked when peer disconnects
        /// </summary>
        /// <param name="peer">Peer that has disconnected</param>
        public void OnPeerDisconnected(IPeerManager peer)
        {
            if (peer != MasterPeer)
            {
                Log.Info($"Could not connect to the {peer.PeerEndPoint} endpoint.");
                connectionRequests--;
                return;
            }

            MasterPeer = null;
            Log.Info($"Peer {peer.PeerEndPoint} disconnected.");
        }

        /// <inheritdoc />
        public void UnicastMessage(IPEndPoint endPoint, DistributedMessage distributedMessage)
        {
            MessagesManager.UnicastMessage(endPoint, distributedMessage);
        }

        /// <inheritdoc />
        public void BroadcastMessage(DistributedMessage distributedMessage)
        {
            MessagesManager.BroadcastMessage(distributedMessage);
        }

        /// <inheritdoc />
        void IMessageSender.UnicastInitialMessages(IPEndPoint endPoint)
        {
        }

        /// <inheritdoc />
        public void ReceiveMessage(IPeerManager sender, DistributedMessage distributedMessage)
        {
            Debug.Assert(MasterPeer == null || MasterPeer == sender);
            PacketsProcessor.ReadAllPackets(new NetDataReader(distributedMessage.Content.GetDataCopy()), sender);
        }

        /// <summary>
        /// Tries to connect to any master ip address
        /// </summary>
        public void TryConnectToMaster()
        {
            if (State != SimulationState.Initial)
                return;

            State = SimulationState.Connecting;
            Log.Info("Client tries to connect to the master.");
            TryConnectToMasterEndPoints();
            if (timeoutCoroutine != null)
                StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = CheckInitialTimeout();
            StartCoroutine(timeoutCoroutine);
        }

        /// <summary>
        /// Tries to connect to any master end point
        /// </summary>
        private void TryConnectToMasterEndPoints()
        {
            masterEndpointsCouroutine = StartCoroutine(IterateMasterEndpoints());
        }

        /// <summary>
        ///  Iterates through master endpoint adresses
        /// </summary>
        private IEnumerator IterateMasterEndpoints()
        {
            //Check if this simulation was not deinitialized
            var network = Loader.Instance.Network;
            if (!network.IsClient)
                yield break;
            var masterEndPoints = network.MasterAddresses;
            var identifier = network.LocalIdentifier;
            
            //Try connecting to every endpoint multiple times if it is needed
            for (var i = 0; i < settings.MaximumConnectionRetries; i++)
            {
                foreach (var masterEndPoint in masterEndPoints)
                {
                    if (State == SimulationState.Connected)
                        yield break;
                    //Check if client is already connected
                    if (MasterPeer != null)
                        yield break;
                    if (!IPAddress.IsLoopback(masterEndPoint.Address))
                    {
                        connectionRequests++;
                        Connection.Connect(masterEndPoint, identifier);
                        while (connectionRequests > 0)
                            yield return null;
                    }
                }
            }
        }

        /// <summary>
        /// Coroutine checking if this client connected to a master after timeout time
        /// </summary>
        /// <returns></returns>
        private IEnumerator CheckInitialTimeout()
        {
            yield return new WaitForSecondsRealtime(settings.Timeout / 1000.0f);
            if (MasterPeer != null) yield break;
            var network = Loader.Instance.Network;
            //Check if this simulation was not deinitialized
            if (!network.IsClient) yield break;

            //Could not connect to any master address, log error and stop the simulation
            var localAddressesSb = new StringBuilder();
            for (var i = 0; i < network.LocalAddresses.Count; i++)
            {
                var ipEndPoint = network.LocalAddresses[i];
                localAddressesSb.Append(ipEndPoint.Address);
                if (i + 1 < network.LocalAddresses.Count)
                    localAddressesSb.Append(", ");
            }

            var masterAddressesSb = new StringBuilder();
            for (var i = 0; i < network.MasterAddresses.Count; i++)
            {
                var ipEndPoint = network.MasterAddresses[i];
                masterAddressesSb.Append(ipEndPoint);
                if (i + 1 < network.MasterAddresses.Count)
                    masterAddressesSb.Append(", ");
            }

            Log.Error(
                $"{GetType().Name} could not establish the connection to the master. This client ip addresses: '{localAddressesSb}', master ip addresses: '{masterAddressesSb}', current UTC time: {DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}.");

            //Stop the simulation
            Loader.Instance.StopAsync();
        }

        /// <summary>
        /// Sends ready command to the master
        /// </summary>
        public void SendReadyCommand()
        {
            if (State != SimulationState.Connected) return;
            State = SimulationState.Ready;
            var dataWriter = new NetDataWriter();
            PacketsProcessor.Write(dataWriter, new Commands.Ready());
            var message = MessagesPool.Instance.GetMessage(dataWriter.Length);
            message.AddressKey = Key;
            message.Content.PushBytes(dataWriter.CopyData());
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
            Log.Info($"{GetType().Name} is ready and has sent ready command to the master.");
        }

        /// <summary>
        /// Sends loaded command to the master
        /// </summary>
        public void SendLoadedCommand()
        {
            if (State != SimulationState.Connected) return;
            State = SimulationState.Ready;
            var dataWriter = new NetDataWriter();
            PacketsProcessor.Write(dataWriter, new Commands.Loaded());
            var message = MessagesPool.Instance.GetMessage(dataWriter.Length);
            message.AddressKey = Key;
            message.Content.PushBytes(dataWriter.CopyData());
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
            Log.Info($"{GetType().Name} loaded the simulation and has sent loaded command to the master.");
        }

        /// <summary>
        /// Broadcast the stop command to the master
        /// </summary>
        public void BroadcastStopCommand()
        {
            if (State == SimulationState.Stopping || Loader.Instance.Network.CurrentSimulation == null)
                return;
            Log.Info($"{GetType().Name} broadcasts the simulation stop command.");

            var dataWriter = new NetDataWriter();
            PacketsProcessor.Write(dataWriter, new Commands.Stop
            {
                SimulationId = Loader.Instance.Network.CurrentSimulation.Id
            });
            var message = MessagesPool.Instance.GetMessage(dataWriter.Length);
            message.AddressKey = Key;
            message.Content.PushBytes(dataWriter.CopyData());
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);

            State = SimulationState.Stopping;
        }

        /// <summary>
        /// Method invoked when manager receives run command
        /// </summary>
        /// <param name="run">Received run command</param>
        private void OnRunCommand(Commands.Run run)
        {
            //Check if the simulation is already loading or running
            if (State == SimulationState.Loading || State == SimulationState.Running)
                return;
            Debug.Assert(State == SimulationState.Ready);
            Loader.Instance.StartAsync(Loader.Instance.Network.CurrentSimulation);
            State = SimulationState.Loading;
            Log.Info(
                $"{GetType().Name} received run command and started the simulation. Local UTC time: {DateTime.UtcNow}, remote UTC time: {Connection.MasterPeer.RemoteUtcTime}, time difference {Connection.MasterPeer.RemoteTimeTicksDifference}.");
        }

        /// <summary>
        /// Method invoked when manager receives stop command
        /// </summary>
        /// <param name="stop">Received stop command</param>
        private void OnStopCommand(Commands.Stop stop)
        {
            var simulation = Loader.Instance.Network.CurrentSimulation;
            if (State == SimulationState.Initial || State == SimulationState.Stopping ||
                simulation == null || simulation.Id != stop.SimulationId) return;

            Log.Info($"{GetType().Name} received stop command and stops the simulation.");
            State = SimulationState.Stopping;
            Loader.Instance.StopAsync();
        }

        /// <summary>
        /// Method invoked when manager receives command updating environment state
        /// </summary>
        /// <param name="state">Environment state update command</param>
        private void OnEnvironmentStateCommand(Commands.EnvironmentState state)
        {
            // TODO: this seems backwards to update UI to update actual values

            var ui = SimulatorManager.Instance.UIManager;
            ui.FogSlider.value = state.Fog;
            ui.RainSlider.value = state.Rain;
            ui.WetSlider.value = state.Wet;
            ui.CloudSlider.value = state.Cloud;
            ui.DamageSlider.value = state.Damage;
            ui.TimeOfDaySlider.value = state.TimeOfDay;
            Log.Info($"{GetType().Name} received environment state update.");
        }

        /// <summary>
        /// Method invoked when manager receives ping command
        /// </summary>
        /// <param name="ping">Ping command</param>
        private void OnPingCommand(Commands.Ping ping)
        {
            var dataWriter = new NetDataWriter();
            PacketsProcessor.Write(dataWriter, new Commands.Pong() {Id = ping.Id});
            var message = MessagesPool.Instance.GetMessage(dataWriter.Length);
            message.AddressKey = Key;
            message.Content.PushBytes(dataWriter.CopyData());
            message.Type = DistributedMessageType.Unreliable;
            BroadcastMessage(message);
        }
    }
}