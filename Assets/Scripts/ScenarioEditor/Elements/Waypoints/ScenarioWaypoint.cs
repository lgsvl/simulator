/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Waypoints
{
    using System;
    using Agents;
    using Managers;
    using UnityEngine;

    /// <inheritdoc cref="Simulator.ScenarioEditor.Elements.ScenarioElement" />
    /// <remarks>
    /// Scenario waypoint representation
    /// </remarks>
    public class ScenarioWaypoint : ScenarioElement
    {
        /// <summary>
        /// Transform of the object that shows the direction of this waypoint
        /// </summary>
        public Transform directionTransform;

        /// <summary>
        /// Transform that will be rotated
        /// </summary>
        public Transform transformToRotate;

        /// <summary>
        /// Mesh renderer of the waypoint model
        /// </summary>
        public MeshRenderer waypointRenderer;

        /// <summary>
        /// Speed that will be applied when agent reach this waypoint
        /// </summary>
        private float destinationSpeed = 6.0f;

        /// <summary>
        /// Acceleration used to reach max speed while traveling to this waypoint
        /// </summary>
        private float acceleration = 0.0f;
        
        /// <inheritdoc/>
        public override string ElementType { get; } = "Waypoint";

        /// <inheritdoc/>
        public override Transform TransformToRotate => transformToRotate;

        /// <inheritdoc/>
        public override bool CanBeRotated => false;

        /// <summary>
        /// Parent scenario element which includes this waypoint
        /// </summary>
        public ScenarioElement ParentElement { get; set; }

        /// <summary>
        /// Index which this waypoint had in parent agent before being removed from map
        /// </summary>
        public int IndexInAgent { get; set; } = -1;

        /// <summary>
        /// Destination speed that agent will try to achieve when reaching this waypoint
        /// Applied immediately if acceleration is not given
        /// </summary>
        public float DestinationSpeed
        {
            get => destinationSpeed;
            set
            {
                if (Mathf.Approximately(destinationSpeed, value))
                    return;
                destinationSpeed = value;
                DestinationSpeedChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Acceleration used to reach max speed while traveling to this waypoint
        /// </summary>
        public float Acceleration
        {
            get => acceleration;
            set
            {
                if (Mathf.Approximately(acceleration, value))
                    return;
                acceleration = value;
                AccelerationChanged?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Event invoked when the waypoint is removed from the scenario
        /// </summary>
        public event Action<ScenarioWaypoint> Removed;
        
        /// <summary>
        /// Event invoked when the waypoint is reverted back into the scenario
        /// </summary>
        public event Action<ScenarioWaypoint> Reverted;

        /// <summary>
        /// Event invoked when the waypoint changes its destination speed
        /// </summary>
        public event Action<ScenarioWaypoint> DestinationSpeedChanged;

        /// <summary>
        /// Event invoked when the waypoint changes its acceleration
        /// </summary>
        public event Action<ScenarioWaypoint> AccelerationChanged;

        /// <summary>
        /// Unity OnEnable method
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>().RegisterWaypoint(this);
        }

        /// <summary>
        /// Unity OnDisable method
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>().UnregisterWaypoint(this);
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioElement origin)
        {
            var originWaypoint = origin.GetComponent<ScenarioWaypoint>();
            if (originWaypoint == null) return;
            IndexInAgent = originWaypoint.IndexInAgent;
            DestinationSpeed = originWaypoint.DestinationSpeed;
            Acceleration = originWaypoint.Acceleration;
        }
        
        /// <inheritdoc/>
        public override void RemoveFromMap()
        {
            base.RemoveFromMap();
            Removed?.Invoke(this);
        }

        /// <inheritdoc/>
        public override void UndoRemove()
        {
            base.UndoRemove();
            Reverted?.Invoke(this);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            ParentElement = null;
            DestinationSpeed = 6.0f;
            ScenarioManager.Instance.prefabsPools.ReturnInstance(gameObject);
        }
    }
}