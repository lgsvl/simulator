/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Waypoints
{
    using Agents;
    using Managers;
    using Triggers;
    using UnityEngine;

    /// <inheritdoc cref="Simulator.ScenarioEditor.Elements.ScenarioElement" />
    /// <remarks>
    /// Scenario agent waypoint representation that includes parametrization
    /// </remarks>
    public class ScenarioAgentWaypoint : ScenarioWaypoint
    {
        /// <summary>
        /// Name of the gameobject containing trigger
        /// </summary>
        private static string triggerObjectName = "Trigger";
        
        /// <inheritdoc/>
        public override string ElementType { get; } = "AgentWaypoint";

        /// <summary>
        /// Trigger that is linked to this waypoint
        /// </summary>
        private ScenarioTrigger linkedTrigger;

        /// <inheritdoc/>
        public override bool CanBeRotated => false;

        /// <summary>
        /// Trigger that is linked to this waypoint
        /// </summary>
        public ScenarioTrigger LinkedTrigger
        {
            get
            {
                if (linkedTrigger != null) return linkedTrigger;
                linkedTrigger = GetComponentInChildren<ScenarioTrigger>();
                if (linkedTrigger != null) return linkedTrigger;
                var go = new GameObject(triggerObjectName);
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.Euler(Vector3.zero);
                linkedTrigger = go.AddComponent<ScenarioTrigger>();
                linkedTrigger.Initialize();
                return linkedTrigger;
            }
        }

        /// <summary>
        /// Time that agent will wait on this waypoint before continuing the movement
        /// </summary>
        public float WaitTime { get; set; }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioElement origin)
        {
            base.CopyProperties(origin);
            var originWaypoint = origin.GetComponent<ScenarioAgentWaypoint>();
            if (originWaypoint == null) return;
            
            //Clear triggers object
            LinkedTrigger.Deinitalize();
            LinkedTrigger.Initialize();
            WaitTime = originWaypoint.WaitTime;
            LinkedTrigger.CopyProperties(originWaypoint.LinkedTrigger);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (linkedTrigger != null)
                linkedTrigger.Deinitalize();
            WaitTime = 0.0f;
            base.Dispose();
        }

        /// <inheritdoc/>
        public override void ForceMove(Vector3 requestedPosition)
        {
            transform.position = requestedPosition;
            var parentAgent = ParentElement as ScenarioAgent;
            if (parentAgent == null) return;
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            switch (parentAgent.Type)
            {
                case AgentType.Ego:
                case AgentType.Npc:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic, TransformToMove, TransformToRotate);
                    break;
                case AgentType.Pedestrian:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian, TransformToMove, TransformToRotate);
                    break;
            }

            OnMoved(false);
        }
    }
}