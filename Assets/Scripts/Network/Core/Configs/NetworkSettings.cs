/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core.Configs
{
    using UnityEngine;

    /// <summary>
    /// Configuration required for the networking system
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkSettings", menuName = "Simulator/Network/Network Settings")]
    public class NetworkSettings : ScriptableObject
    {
        /// <summary>
        /// Connection port used for establishing connection with a peer
        /// </summary>
        [SerializeField]
        private int connectionPort = 9999;
        
        /// <summary>
        /// Timeout value in milliseconds
        /// </summary>
        [SerializeField]
        private int timeout = 30000;

        /// <summary>
        /// Defines how many times client can retry connecting to the main, can be limited by the timeout value 
        /// </summary>
        [SerializeField]
        private int maximumConnectionRetries = 3;

//Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Prefabs of distributed objects which can be instantiated in a distributed objects root
        /// </summary>
        [SerializeField]
        private GameObject[] distributedObjectPrefabs;
#pragma warning restore 0649

        /// <summary>
        /// Connection port used for establishing connection with a peer
        /// </summary>
        public int ConnectionPort => connectionPort;

        /// <summary>
        /// Timeout value in milliseconds
        /// </summary>
        public int Timeout => timeout;

        /// <summary>
        /// Prefabs of distributed objects which can be instantiated in a distributed objects root
        /// </summary>
        public GameObject[] DistributedObjectPrefabs => distributedObjectPrefabs;

        /// <summary>
        /// Maxi
        /// </summary>
        public int MaximumConnectionRetries => maximumConnectionRetries;
    }
}