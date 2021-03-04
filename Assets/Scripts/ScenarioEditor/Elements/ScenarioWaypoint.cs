/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
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
        /// Name of the gameobject containing trigger
        /// </summary>
        private static string triggerObjectName = "Trigger";

        /// <summary>
        /// Trigger that is linked to this waypoint
        /// </summary>
        private ScenarioTrigger linkedTrigger;
        
        /// <inheritdoc/>
        public override string ElementType { get; } = "Waypoint";

        /// <inheritdoc/>
        public override Transform TransformToRotate => transformToRotate;

        /// <inheritdoc/>
        public override bool CanBeRotated => false;

        /// <summary>
        /// Parent agent which includes this waypoint
        /// </summary>
        public ScenarioAgent ParentAgent { get; set; }

        /// <summary>
        /// Parent agent extension which modifies this waypoint
        /// </summary>
        public AgentWaypoints ParentAgentWaypoints { get; set; }

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
        /// Speed that will be applied when agent reach this waypoint
        /// </summary>
        public float Speed { get; set; } = 6.0f;

        /// <summary>
        /// Time that agent will wait on this waypoint before continuing the movement
        /// </summary>
        public float WaitTime { get; set; }

        /// <summary>
        /// Index which this waypoint had in parent agent before being removed from map
        /// </summary>
        public int IndexInAgent { get; set; } = -1;

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
            //Clear triggers object
            LinkedTrigger.Deinitalize();
            LinkedTrigger.Initialize();
            CopyProperties(originWaypoint, true);
        }

        /// <summary>
        /// Copies property values from the origin
        /// </summary>
        /// <param name="originWaypoint">Origin waypoint, properties will be copied from it to this waypoint</param>
        /// <param name="copyTrigger">Should triggers be copied with the waypoint</param>
        public void CopyProperties(ScenarioWaypoint originWaypoint, bool copyTrigger = false)
        {
            Speed = originWaypoint.Speed;
            WaitTime = originWaypoint.WaitTime;
            IndexInAgent = originWaypoint.IndexInAgent;
            if (copyTrigger)
                LinkedTrigger.CopyProperties(originWaypoint.LinkedTrigger);
        }

        /// <inheritdoc/>
        public override void ForceMove(Vector3 requestedPosition)
        {
            transform.position = requestedPosition;
            if (ParentAgent == null) return;
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            switch (ParentAgent.Type)
            {
                case AgentType.Ego:
                case AgentType.Npc:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic, TransformToMove, TransformToRotate);
                    break;
                case AgentType.Pedestrian:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian, TransformToMove, TransformToRotate);
                    break;
            }

            ParentAgentWaypoints.WaypointPositionChanged(this);
        }

        /// <inheritdoc/>
        public override void RemoveFromMap()
        {
            base.RemoveFromMap();
            if (ParentAgentWaypoints != null)
                IndexInAgent = ParentAgentWaypoints.RemoveWaypoint(this);
        }

        /// <inheritdoc/>
        public override void UndoRemove()
        {
            base.UndoRemove();
            ParentAgentWaypoints?.AddWaypoint(this, IndexInAgent);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            ParentAgent = null;
            ParentAgentWaypoints = null;
            if (linkedTrigger != null)
                linkedTrigger.Deinitalize();
            Speed = 6.0f;
            WaitTime = 0.0f;
            ScenarioManager.Instance.prefabsPools.ReturnInstance(gameObject);
        }

        /// <inheritdoc/>
        protected override void OnMoved(bool notifyOthers = true)
        {
            base.OnMoved(notifyOthers);
            ParentAgentWaypoints?.WaypointPositionChanged(this);
        }
    }
}