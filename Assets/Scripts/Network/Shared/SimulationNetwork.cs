/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Client;
    using Core;
    using Core.Configs;
    using Core.Messaging;
    using Core.Threading;
    using Master;
    using UnityEngine;
    using Web;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Network objects used in a single simulation
    /// </summary>
    public class SimulationNetwork
    {
        /// <summary>
        /// The cluster node types of the simulation
        /// </summary>
        public enum ClusterNodeType
        {
            NotClusterNode,
            Master,
            Client
        }

        /// <summary>
        /// The cluster node type of this simulation
        /// </summary>
        public ClusterNodeType Type { get; private set; }

        /// <summary>
        /// Is this a cluster simulation
        /// </summary>
        public bool IsClusterSimulation => Type != ClusterNodeType.NotClusterNode;

        /// <summary>
        /// Is this a master node for the cluster simulation
        /// </summary>
        public bool IsMaster => Type == ClusterNodeType.Master;

        /// <summary>
        /// Is this a client node for the cluster simulation
        /// </summary>
        public bool IsClient => Type == ClusterNodeType.Client;

        /// <summary>
        /// Network settings used in this simulation
        /// </summary>
        public NetworkSettings Settings { get; private set; }

        /// <summary>
        /// Master manager instance, if this is a master node of the simulation
        /// </summary>
        public MasterManager Master { get; set; }

        /// <summary>
        /// Client manager instance, if this is a client node of the simulation
        /// </summary>
        public ClientManager Client { get; set; }

        /// <summary>
        /// Raw cluster configuration data from the cloud
        /// </summary>
        public ClusterData ClusterData { get; private set; }

        /// <summary>
        /// Count of clients in this distributed simulation
        /// </summary>
        public int ClientsCount => ClusterData.Instances.Length - 1;

        /// <summary>
        /// Identifier of this machine in the simulation
        /// </summary>
        public string LocalIdentifier { get; private set; }

        /// <summary>
        /// IP end point addresses for this machine in the simulation
        /// </summary>
        public List<IPEndPoint> LocalAddresses { get; } = new List<IPEndPoint>();

        /// <summary>
        /// Identifier of the simulation master
        /// </summary>
        public string MasterIdentifier { get; private set; }

        /// <summary>
        /// IP end point addresses for the simulation master
        /// </summary>
        public List<IPEndPoint> MasterAddresses { get; } = new List<IPEndPoint>();

        /// <summary>
        /// Messages manager used either in master node or in client node
        /// </summary>
        public MessagesManager MessagesManager
        {
            get
            {
                switch (Type)
                {
                    case ClusterNodeType.Master:
                        return Master.MessagesManager;
                    case ClusterNodeType.Client:
                        return Client.MessagesManager;
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Cached current simulation model
        /// </summary>
        public SimulationData CurrentSimulation { get; set; }

        /// <summary>
        /// Checks if simulation is ready to be started
        /// </summary>
        public bool IsSimulationReady => CurrentSimulation != null;

        /// <summary>
        /// Initialization method for setting up the simulation network 
        /// </summary>
        /// <param name="simulationId">This machine simulation id</param>
        /// <param name="clusterData">Cluster data for this simulation</param>
        /// <param name="settings">Settings used in this simulation</param>
        public void Initialize(string simulationId, ClusterData clusterData, NetworkSettings settings)
        {
            ClusterData = clusterData;
            Settings = settings;
            
            //Check what cluster node type is this machine in the simulation
            if (ClusterData.Instances.Length <= 1)
            {
                Type = ClusterNodeType.NotClusterNode;
            }
            else
            {
                //Set this machine type
                var instancesData = ClusterData.Instances.Where(instance => instance.SimId == simulationId).ToArray();
                var resultsCount = instancesData.Length;
                if (resultsCount == 0)
                    throw new ArgumentException(
                        $"Invalid cluster settings. Could not find instance data for the simulation id '{simulationId}'.");
                if (resultsCount > 1)
                    throw new ArgumentException(
                        $"Invalid cluster settings. Found multiple instance data for the simulation id '{simulationId}'.");
                var thisInstanceData = instancesData[0];
                Type = thisInstanceData.IsMaster ? ClusterNodeType.Master : ClusterNodeType.Client;
                LocalIdentifier = thisInstanceData.SimId;
                foreach (var ip in thisInstanceData.Ip)
                    LocalAddresses.Add(new IPEndPoint(IPAddress.Parse(ip), Settings.ConnectionPort));

                //Setup master addresses 
                var masterInstanceData = ClusterData.Instances.Where(instance => instance.IsMaster).ToArray();
                resultsCount = masterInstanceData.Length;
                if (resultsCount == 0)
                    throw new ArgumentException($"Invalid cluster settings. Could not find master instance data.");
                if (resultsCount > 1)
                    throw new ArgumentException($"Invalid cluster settings. Found multiple master instance data.");
                MasterIdentifier = masterInstanceData[0].SimId;
                foreach (var ip in masterInstanceData[0].Ip)
                    MasterAddresses.Add(new IPEndPoint(IPAddress.Parse(ip), Settings.ConnectionPort));
            }

            //Initialize network objects
            var mainThreadDispatcher = Object.FindObjectOfType<MainThreadDispatcher>();
            if (mainThreadDispatcher == null)
            {
                var dispatcher = new GameObject("MainThreadDispatcher");
                Object.DontDestroyOnLoad(dispatcher);
                dispatcher.AddComponent<MainThreadDispatcher>();
            }
            if (Type == ClusterNodeType.Master)
            {
                var masterGameObject = new GameObject("MasterManager");
                Object.DontDestroyOnLoad(masterGameObject);
                Master = masterGameObject.AddComponent<MasterManager>();
                Master.SetSettings(Settings);
                var clientsIdentifiers = ClusterData.Instances.Where(instanceData => !instanceData.IsMaster)
                    .Select(instanceData => instanceData.SimId).ToList();
                Master.StartConnection(clientsIdentifiers);
            }
            else if (Type == ClusterNodeType.Client)
            {
                var clientGameObject = new GameObject("ClientManager");
                Object.DontDestroyOnLoad(clientGameObject);
                Client = clientGameObject.AddComponent<ClientManager>();
                clientGameObject.AddComponent<MainThreadDispatcher>();
                Client.SetSettings(Settings);
                Client.StartConnection();
                Client.TryConnectToMaster();
            }
            Log.Info("Initialized the Simulation Network data.");
        }

        /// <summary>
        /// Deinitialization method cleaning all the created objects
        /// </summary>
        public async Task Deinitialize()
        {
            //Client should wait for master to break the connection
            if (IsClient)
            {
                while (Client.Connection.ConnectedPeersCount > 0)
                    await Task.Delay(20);
            }
            StopConnection();

            var type = Type;
            var clearObjectsAction = new Action(() =>
            {
                ClearNetworkObjects(type);
            });
            ThreadingUtilities.DispatchToMainThread(clearObjectsAction);

            LocalAddresses.Clear();
            MasterAddresses.Clear();
            Type = ClusterNodeType.NotClusterNode;
            CurrentSimulation = null;
            Log.Info("Deinitialized the Simulation Network data.");
        }

        /// <summary>
        /// Removes instantiated objects by the network system
        /// </summary>
        private void ClearNetworkObjects(ClusterNodeType type)
        {
            switch (type)
            {
                case ClusterNodeType.Master:
                    if (Master != null)
                    {
                        Object.Destroy(Master.gameObject);
                        Master = null;
                    }
                    break;
                case ClusterNodeType.Client:
                    if (Client != null)
                    {
                        Object.Destroy(Client.gameObject);
                        Client = null;
                    }
                    break;
            }
        }

        /// <summary>
        /// Master tries to start the simulation, client reports about being ready
        /// </summary>
        /// <param name="simulationData">Simulation model which will be used to run the simulation</param>
        public void SetSimulationData(SimulationData simulationData)
        {
            CurrentSimulation = simulationData;
            Log.Info($"Simulation with id '{simulationData.Id}' set for the network system.");
            switch (Type)
            {
                case ClusterNodeType.NotClusterNode:
                    break;
                case ClusterNodeType.Master:
                    Master.TryLoadSimulation();
                    break;
                case ClusterNodeType.Client:
                    simulationData.Interactive = false;
                    Client.SendReadyCommand();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Initializes network objects on the prepared simulation scene
        /// </summary>
        /// <param name="simulationRoot">The root game object of the simulation</param>
        public void InitializeSimulationScene(GameObject simulationRoot)
        {
            switch (Type)
            {
                case ClusterNodeType.NotClusterNode:
                    break;
                case ClusterNodeType.Master:
                    Master.InitializeSimulationScene(simulationRoot);
                    break;
                case ClusterNodeType.Client:
                    Client.InitializeSimulationScene(simulationRoot);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Log.Info("Initialized the Simulation scene for the network manager");
        }

        /// <summary>
        /// Stops the connection as the simulation has ended 
        /// </summary>
        public void StopConnection()
        {
            switch (Type)
            {
                case ClusterNodeType.NotClusterNode:
                    break;
                case ClusterNodeType.Master:
                    Master.StopConnection();
                    break;
                case ClusterNodeType.Client:
                    Client.StopConnection();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Broadcasts the stop command to the connected peers
        /// </summary>
        public void BroadcastStopCommand()
        {
            //Do not send stop command is simulation is already stopping
            if (Loader.Instance.Status != SimulatorStatus.Running && Loader.Instance.Status != SimulatorStatus.Error)
                return;
            switch (Type)
            {
                case ClusterNodeType.NotClusterNode:
                    break;
                case ClusterNodeType.Master:
                    Master.BroadcastStopCommand();
                    break;
                case ClusterNodeType.Client:
                    Client.BroadcastStopCommand();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}