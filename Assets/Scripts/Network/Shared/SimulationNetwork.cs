/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Shared
{
    using Client;
    using Core.Configs;
    using Core.Messaging;
    using Master;

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
        /// Initialization method for setting up the simulation network 
        /// </summary>
        /// <param name="type">Type of The cluster node type of this simulation</param>
        /// <param name="settings">Settings used in this simulation</param>
        public void Initialize(ClusterNodeType type, NetworkSettings settings)
        {
            Type = type;
            Settings = settings;
        }
    }
}