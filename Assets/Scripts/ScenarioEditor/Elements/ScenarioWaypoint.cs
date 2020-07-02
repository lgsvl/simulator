/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
    using Agents;
    using Managers;
    using UnityEngine;

    /// <inheritdoc/>
    /// <remarks>
    /// Scenario waypoint representation
    /// </remarks>
    public class ScenarioWaypoint : ScenarioElement
    {
        /// <summary>
        /// Parent agent which includes this waypoint
        /// </summary>
        public ScenarioAgent ParentAgent { get; set; }
        
        /// <summary>
        /// Trigger that is linked to this waypoint
        /// </summary>
        public ScenarioTrigger LinkedTrigger { get; set; }

        /// <summary>
        /// Speed that will be applied when agent reach this waypoint
        /// </summary>
        public float Speed { get; set; } = 6.0f;

        /// <summary>
        /// Time that agent will wait on this waypoint before continuing the movement
        /// </summary>
        public float WaitTime { get; set; }

        /// <summary>
        /// Unity OnEnable method
        /// </summary>
        private void OnEnable()
        {
            ScenarioManager.Instance.waypointsManager.RegisterWaypoint(this);
        }

        /// <summary>
        /// Unity OnDisable method
        /// </summary>
        private void OnDisable()
        {
            ScenarioManager.Instance.waypointsManager.UnregisterWaypoint(this);
        }

        /// <inheritdoc/>
        public override void Reposition(Vector3 requestedPosition)
        {
            transform.position = requestedPosition;
            if (ParentAgent!=null)
                ParentAgent.WaypointPositionChanged(this);
        }

        /// <inheritdoc/>
        public override void Selected()
        {
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            if (ParentAgent!=null)
                ParentAgent.RemoveWaypoint(this);
            ParentAgent = null;
            ScenarioManager.Instance.prefabsPools.ReturnInstance(gameObject);
        }

        /// <inheritdoc/>
        protected override void OnDragged()
        {
            base.OnDragged();
            ParentAgent.WaypointPositionChanged(this);
        }
    }
}