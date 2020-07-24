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
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using Core.Configs;
    using Core.Connection;
    using Core.Messaging;
    using Core.Messaging.Data;
    using LiteNetLib.Utils;
    using Shared;
    using Simulator.Network.Core;
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
        /// Downloads that are currently in progress
        /// </summary>
        private List<string> processedDownloads = new List<string>();

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
        }

        /// <summary>
        /// Start the connection listening for incoming packets
        /// </summary>
        public void StartConnection()
        {
            if (settings == null)
                throw new NullReferenceException("Set network settings before starting the connection.");
            MessagesManager.RegisterObject(this);
            Connection.Start(settings.ConnectionPort, settings.Timeout);
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
            Log.Info($"{GetType().Name} stopped the connection manager.");
        }

        /// <summary>
        /// Method invoked when new peer connects
        /// </summary>
        /// <param name="peer">Peer that has connected</param>
        public void OnPeerConnected(IPeerManager peer)
        {
            Debug.Assert(State == SimulationState.Connecting);
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
                return;
            MasterPeer = null;
            // OnStopCommand(new Commands.Stop());
            // MessagesManager.RevokeIdentifiers();
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
            State = SimulationState.Connecting;
            var masterEndPoints = Loader.Instance.Network.MasterAddresses;
            var localEndPoints = Loader.Instance.Network.LocalAddresses;
            var identifier = Loader.Instance.Network.LocalIdentifier;
            foreach (var masterEndPoint in masterEndPoints)
            {
                //Check if client is already connected
                if (MasterPeer != null)
                    return;
                if (!localEndPoints.Contains(masterEndPoint))
                    Connection.Connect(masterEndPoint, identifier);
            }

            StartCoroutine(CheckInitialTimeout());
        }

        /// <summary>
        /// Coroutine checking if this client connected to a master after timeout time
        /// </summary>
        /// <returns></returns>
        private IEnumerator CheckInitialTimeout()
        {
            yield return new WaitForSecondsRealtime(settings.Timeout / 1000.0f);
            if (MasterPeer != null) yield break;

            //Could not connect to any master address, log error and stop the simulation
            var localAddressesSb = new StringBuilder();
            for (var i = 0; i < Loader.Instance.Network.LocalAddresses.Count; i++)
            {
                var ipEndPoint = Loader.Instance.Network.LocalAddresses[i];
                localAddressesSb.Append(ipEndPoint);
                if (i+1 < Loader.Instance.Network.LocalAddresses.Count)
                    localAddressesSb.Append(", ");
            }
            var masterAddressesSb = new StringBuilder();
            for (var i = 0; i < Loader.Instance.Network.MasterAddresses.Count; i++)
            {
                var ipEndPoint = Loader.Instance.Network.MasterAddresses[i];
                masterAddressesSb.Append(ipEndPoint);
                if (i+1 < Loader.Instance.Network.MasterAddresses.Count)
                    masterAddressesSb.Append(", ");
            }

            Log.Error(
                $"{GetType().Name} could not establish the connection to the master. This client ip addresses: '{localAddressesSb}', master ip addresses: '{masterAddressesSb}'.");

            var simulation = Loader.Instance.Network.CurrentSimulation;
            if (simulation != null)
            {
                ConnectionManager.instance.UpdateStatus("Stopping", simulation.Id);
            }
            else
            {
                Debug.Log("Cannot send stopping status without simulation");
            }
        }

        /// <summary>
        /// Sends ready command to the master
        /// </summary>
        public void SendReadyCommand()
        {
            if (State != SimulationState.Connected) return;
            State = SimulationState.Ready;
            var stopData = PacketsProcessor.Write(new Commands.Ready() { });
            var message = MessagesPool.Instance.GetMessage(stopData.Length);
            message.AddressKey = Key;
            message.Content.PushBytes(stopData);
            message.Type = DistributedMessageType.ReliableOrdered;
            BroadcastMessage(message);
            Log.Info($"{GetType().Name} is ready and has sent ready command to the master..");
        }

        /// <summary>
        /// Method invoked when manager receives run command
        /// </summary>
        /// <param name="run">Received run command</param>
        private void OnRunCommand(Commands.Run run)
        {
            Debug.Assert(State == SimulationState.Ready);
            Loader.StartAsync(Loader.Instance.Network.CurrentSimulation);
            State = SimulationState.Running;
            Log.Info($"{GetType().Name} received run command and started the simulation.");
        }

        /// <summary>
        /// Method invoked when manager receives stop command
        /// </summary>
        /// <param name="stop">Received stop command</param>
        private void OnStopCommand(Commands.Stop stop)
        {
            if (Loader.Instance.CurrentSimulation != null && State != SimulationState.Initial)
                Loader.StopAsync();

            State = SimulationState.Initial;
            Log.Info($"{GetType().Name} received stop command and stops the simulation.");
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
            ui.TimeOfDaySlider.value = state.TimeOfDay;
            Log.Info($"{GetType().Name} received environment state update.");
        }

        /// <summary>
        /// Method invoked when manager receives ping command
        /// </summary>
        /// <param name="ping">Ping command</param>
        private void OnPingCommand(Commands.Ping ping)
        {
            var pongData = PacketsProcessor.Write(new Commands.Pong() {Id = ping.Id});
            var message = MessagesPool.Instance.GetMessage(pongData.Length);
            message.AddressKey = Key;
            message.Content.PushBytes(pongData);
            message.Type = DistributedMessageType.Unreliable;
            BroadcastMessage(message);
        }
    }
}