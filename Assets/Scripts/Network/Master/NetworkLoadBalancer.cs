/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Master
{
    using System.Collections.Generic;
    using Core;
    using Core.Connection;

    /// <summary>
    /// Load balancer for distributing tasks between clients
    /// </summary>
    public class NetworkLoadBalancer
    {
        /// <summary>
        /// Initial master machine load
        /// </summary>
        private const float InitialMasterLoad = 0.15f;

        /// <summary>
        /// Parent master manager that uses this load balancer
        /// </summary>
        private MasterManager parentManager;

        /// <summary>
        /// Current load of the master machine
        /// </summary>
        private float masterLoad;
        
        /// <summary>
        /// Client machines loads
        /// </summary>
        private readonly Dictionary<IPeerManager, float> clientLoads = new Dictionary<IPeerManager, float>();

        /// <summary>
        /// Initialization method
        /// </summary>
        /// <param name="masterManager">Parent master manager that uses this load balancer</param>
        public void Initialize(MasterManager masterManager)
        {
            if (parentManager != null)
            {
                Log.Error($"{nameof(NetworkLoadBalancer)} is already initialized.");
                return;
            }

            masterLoad = InitialMasterLoad;
            clientLoads.Clear();
            masterManager.ClientConnected += MasterManagerOnClientConnected;
            masterManager.ClientDisconnected += MasterManagerOnClientDisconnected;
            foreach (var client in masterManager.Clients)
            {
                MasterManagerOnClientConnected(client.Peer);
            }

            parentManager = masterManager;
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            if (parentManager == null)
                return;
            parentManager.ClientConnected -= MasterManagerOnClientConnected;
            parentManager.ClientDisconnected -= MasterManagerOnClientDisconnected;
            parentManager = null;
        }

        /// <summary>
        /// Method invoked when master manager connects to a new client
        /// </summary>
        /// <param name="peer">Peer manager of the connected client</param>
        private void MasterManagerOnClientConnected(IPeerManager peer)
        {
            if (clientLoads.ContainsKey(peer))
            {
                Log.Error($"Client {peer.PeerEndPoint.Address} is already registered in the load balanced.");
                return;
            }

            clientLoads.Add(peer, 0.0f);
        }

        /// <summary>
        /// Method invoked when master manager disconnects from a client
        /// </summary>
        /// <param name="peer">Peer manager of the disconnected client</param>
        private void MasterManagerOnClientDisconnected(IPeerManager peer)
        {
            clientLoads.Remove(peer);
        }

        /// <summary>
        /// Appends the given load to the machine with the lowest load
        /// </summary>
        /// <param name="load">Load to append</param>
        /// <param name="includeMaster">Can master receive this load</param>
        /// <returns>The peer manager that received the load, null if master machine received it</returns>
        public IPeerManager AppendLoad(float load, bool includeMaster)
        {
            IPeerManager lowestPeer = null;
            foreach (var clientLoad in clientLoads)
            {
                if (lowestPeer == null || clientLoad.Value < clientLoads[lowestPeer])
                {
                    lowestPeer = clientLoad.Key;
                }
            }

            if (includeMaster && (lowestPeer == null || masterLoad < clientLoads[lowestPeer]))
            {
                masterLoad += load;
                return null;
            }

            if (lowestPeer == null)
            {
                Log.Error($"{nameof(NetworkLoadBalancer)} does not have any client registered. Load appended to the master machine.");
                masterLoad += load;
                return null;
            }

            clientLoads[lowestPeer] = clientLoads[lowestPeer] + load;
            return lowestPeer;
        }

        /// <summary>
        /// Appends the load directly to the master machine
        /// </summary>
        /// <param name="load">Load to append</param>
        public void AppendMasterLoad(float load)
        {
            masterLoad += load;
        }

        /// <summary>
        /// Release the load from given peer
        /// </summary>
        /// <param name="peer">Peer which should lower its load</param>
        /// <param name="load">Load value</param>
        public void ReleaseLoad(IPeerManager peer, float load)
        {
            if (peer == null)
            {
                masterLoad -= load;
                return;
            }

            clientLoads[peer] = clientLoads[peer] - load;
        }
    }
}