/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Elements;
    using UnityEngine;

    /// <summary>
    /// Manager for caching and handling all the waypoints
    /// </summary>
    public class ScenarioWaypointsManager : MonoBehaviour, IScenarioEditorExtension
    {
        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }

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

        /// <inheritdoc/>
        public Task Initialize()
        {
            if (IsInitialized)
                return Task.CompletedTask;
            ScenarioManager.Instance.ScenarioReset += OnScenarioReset;
            IsInitialized = true;
            Debug.Log($"{GetType().Name} scenario editor extension has been initialized.");
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Deinitialize()
        {
            if (!IsInitialized)
                return;
            ScenarioManager.Instance.ScenarioReset -= OnScenarioReset;
            OnScenarioReset();
            Waypoints.Clear();
            IsInitialized = false;
            Debug.Log($"{GetType().Name} scenario editor extension has been deinitialized.");
        }

        /// <summary>
        /// Method invoked when current scenario is being reset
        /// </summary>
        private void OnScenarioReset()
        {
            for (var i = Waypoints.Count - 1; i >= 0; i--)
            {
                var waypoint = Waypoints[i];
                waypoint.RemoveFromMap();
                waypoint.Dispose();
            }
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