/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System.Collections.Generic;
    using Elements;
    using UnityEngine;

    /// <summary>
    /// Manager for caching and handling all the waypoints
    /// </summary>
    public class ScenarioWaypointsManager : MonoBehaviour
    {
        /// <summary>
        /// Shared material that will be used for all the waypoints line renderers
        /// </summary>
        public Material waypointPathMaterial;
        
        /// <summary>
        /// Shared material that will be used for all the triggers line renderers
        /// </summary>
        public Material triggerPathMaterial;

        /// <summary>
        /// Prefab for the waypoint graphic representation on the map
        /// </summary>
        public GameObject waypointPrefab;

        /// <summary>
        /// Cached all the waypoints available in the scenario
        /// </summary>
        public List<ScenarioWaypoint> Waypoints { get; } = new List<ScenarioWaypoint>();

        /// <summary>
        /// Initialization method
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            Waypoints.Clear();
        }

        /// <summary>
        /// Registers the waypoint in the manager
        /// </summary>
        /// <param name="waypoint">Waypoint to register</param>
        public void RegisterWaypoint(ScenarioWaypoint waypoint)
        {
            Waypoints.Add(waypoint);
        }

        /// <summary>
        /// Unregisters the waypoint in the manager
        /// </summary>
        /// <param name="waypoint">Waypoint to unregister</param>
        public void UnregisterWaypoint(ScenarioWaypoint waypoint)
        {
            Waypoints.Remove(waypoint);
        }
    }
}