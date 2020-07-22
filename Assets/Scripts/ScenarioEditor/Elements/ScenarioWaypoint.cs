/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
    using Agents;
    using Input;
    using Managers;
    using UnityEngine;

    /// <inheritdoc cref="Simulator.ScenarioEditor.Elements.ScenarioElement" />
    /// <remarks>
    /// Scenario waypoint representation
    /// </remarks>
    public class ScenarioWaypoint : ScenarioElement, IRemoveHandler
    {
        /// <summary>
        /// Trigger that is linked to this waypoint
        /// </summary>
        private ScenarioTrigger linkedTrigger;
        
        /// <summary>
        /// Parent agent which includes this waypoint
        /// </summary>
        public ScenarioAgent ParentAgent { get; set; }

        /// <summary>
        /// Trigger that is linked to this waypoint
        /// </summary>
        public ScenarioTrigger LinkedTrigger
        {
            get
            {
                if (linkedTrigger != null) return linkedTrigger;
                var go = new GameObject("Trigger");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.Euler(Vector3.zero);
                linkedTrigger = go.AddComponent<ScenarioTrigger>();
                linkedTrigger.LinkedWaypoint = this;
                linkedTrigger.Initialize();
                return linkedTrigger;
            }
        }

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
        protected override void OnEnable()
        {
            base.OnEnable();
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
        public void Remove()
        {
            if (ParentAgent!=null)
                ParentAgent.RemoveWaypoint(this);
            ParentAgent = null;
            if (linkedTrigger != null)
                linkedTrigger.Deinitalize();

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