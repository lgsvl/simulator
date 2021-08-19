/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Elements.Waypoints;
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
        [SerializeField]
        private Material waypointPathMaterial;

        /// <summary>
        /// Shared material that will be used for all the triggers line renderers
        /// </summary>
        [SerializeField]
        private Material triggerPathMaterial;

        /// <summary>
        /// Prefab for the waypoint graphic representation on the map
        /// </summary>
        [SerializeField]
        private List<ScenarioWaypoint> waypointPrefabs;

        /// <summary>
        /// Cached all the waypoints available in the scenario
        /// </summary>
        public List<ScenarioWaypoint> Waypoints { get; } = new List<ScenarioWaypoint>();

        /// <summary>
        /// Shared material that will be used for all the triggers line renderers
        /// </summary>
        public Material TriggerPathMaterial => triggerPathMaterial;

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

        /// <summary>
        /// Instantiates a scenario waypoint for the given waypoint type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetWaypointInstance<T>() where T : ScenarioWaypoint
        {
            ScenarioWaypoint prefab =
                waypointPrefabs.FirstOrDefault(waypointPrefab => waypointPrefab.GetType() == typeof(T));

            if (prefab == null)
            {
                foreach (var waypointPrefab in waypointPrefabs)
                {
                    if (!(waypointPrefab is T))
                        continue;

                    prefab = waypointPrefab;
                    break;
                }
            }

            if (prefab == null)
            {
                ScenarioManager.Instance.logPanel.EnqueueError(
                    $"Could not instantiate a waypoints prefab for type {typeof(T).Name}. Add a proper waypoint prefab to the {nameof(ScenarioWaypointsManager)}.");
                return null;
            }

            return ScenarioManager.Instance.prefabsPools
                .GetInstance(prefab.gameObject).GetComponent<T>();
        }
    }
}