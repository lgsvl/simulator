/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using System.Collections.Generic;
    using Elements;
    using Managers;
    using UnityEngine;

    /// <inheritdoc cref="Simulator.ScenarioEditor.Elements.ScenarioElement" />
    /// <remarks>
    /// Scenario agent representation
    /// </remarks>
    public class ScenarioAgent : ScenarioElement
    {
        /// <summary>
        /// The position offset that will be applied to the line renderer of waypoints
        /// </summary>
        private static Vector3 lineRendererPositionOffset = new Vector3(0.0f, 0.5f, 0.0f);

        /// <summary>
        /// Parent source of this scenario agent
        /// </summary>
        private ScenarioAgentSource source;

        /// <summary>
        /// This agent variant
        /// </summary>
        private AgentVariant variant;

        /// <summary>
        /// Cached model instance object
        /// </summary>
        private GameObject modelInstance;

        /// <summary>
        /// Line renderer for displaying the connection between waypoints
        /// </summary>
        private LineRenderer lineRenderer;

        /// <summary>
        /// Waypoints parent where inherited waypoints objects will be added
        /// </summary>
        private Transform waypointsParent;

        /// <summary>
        /// Included waypoints that this agent will follow
        /// </summary>
        private List<ScenarioWaypoint> waypoints = new List<ScenarioWaypoint>();

        /// <summary>
        /// Included triggers that will influence this agent
        /// </summary>
        private List<ScenarioTrigger> triggers = new List<ScenarioTrigger>();

        /// <summary>
        /// Waypoints parent where inherited waypoints objects will be added
        /// </summary>
        public Transform WaypointsParent
        {
            get
            {
                if (waypointsParent == null)
                {
                    var newGameObject = new GameObject("Waypoints");
                    waypointsParent = newGameObject.transform;
                    waypointsParent.SetParent(transform);
                    waypointsParent.localPosition = Vector3.zero;
                    lineRenderer = newGameObject.AddComponent<LineRenderer>();
                    lineRenderer.material = ScenarioManager.Instance.waypointsManager.waypointPathMaterial;
                    lineRenderer.useWorldSpace = false;
                    lineRenderer.positionCount = 1;
                    lineRenderer.SetPosition(0, lineRendererPositionOffset);
                    lineRenderer.sortingLayerName = "Ignore Raycast";
                    lineRenderer.widthMultiplier = 0.2f;
                }

                return waypointsParent;
            }
        }

        /// <inheritdoc/>
        public override Transform TransformToRotate => modelInstance.transform;

        /// <summary>
        /// Parent source of this scenario agent
        /// </summary>
        public ScenarioAgentSource Source => source;

        /// <summary>
        /// This agent variant
        /// </summary>
        public AgentVariant Variant => variant;

        /// <summary>
        /// Included waypoints that this agent will follow
        /// </summary>
        public List<ScenarioWaypoint> Waypoints => waypoints;

        /// <summary>
        /// Included triggers that will influence this agent
        /// </summary>
        public List<ScenarioTrigger> Triggers => triggers;

        /// <summary>
        /// Type of this agent
        /// </summary>
        public AgentType Type => source.AgentType;

        /// <summary>
        /// Setup method for initializing the required agent data
        /// </summary>
        /// <param name="agentSource">Source of this agent</param>
        /// <param name="agentVariant">This agent variant</param>
        public void Setup(ScenarioAgentSource agentSource, AgentVariant agentVariant)
        {
            source = agentSource;
            ChangeVariant(agentVariant);
            ScenarioManager.Instance.agentsManager.RegisterAgent(this);
        }

        /// <summary>
        /// Changes the current agent variant
        /// </summary>
        /// <param name="newVariant">New agent variant</param>
        public void ChangeVariant(AgentVariant newVariant)
        {
            var position = Vector3.zero;
            var rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            if (modelInstance != null)
            {
                position = modelInstance.transform.localPosition;
                rotation = modelInstance.transform.localRotation;
                source.ReturnModelInstance(modelInstance);
            }

            variant = newVariant;
            modelInstance = source.GetModelInstance(variant);
            modelInstance.transform.SetParent(transform);
            modelInstance.transform.localPosition = position;
            modelInstance.transform.localRotation = rotation;
        }

        /// <inheritdoc/>
        public override void Remove()
        {
            if (modelInstance != null)
                source.ReturnModelInstance(modelInstance);
            for (var i = waypoints.Count - 1; i >= 0; i--) waypoints[i].Remove();

            ScenarioManager.Instance.agentsManager.UnregisterAgent(this);
            Destroy(gameObject);
        }

        /// <inheritdoc/>
        public override void Reposition(Vector3 requestedPosition)
        {
            base.Reposition(requestedPosition);
            switch (Type)
            {
                case AgentType.Ego:
                case AgentType.Npc:
                    ScenarioManager.Instance.MapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic, TransformToMove, TransformToRotate);
                    break;
                case AgentType.Pedestrian:
                    ScenarioManager.Instance.MapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian, TransformToMove, TransformToRotate);
                    break;
            }
        }

        /// <summary>
        /// Adds waypoint to this agent right after previous waypoint
        /// </summary>
        /// <param name="waypoint">Waypoint that will be added to this agent</param>
        /// <param name="previousWaypoint">New waypoint will be added after previous waypoint, if null new is added as last</param>
        public void AddWaypoint(ScenarioWaypoint waypoint, ScenarioWaypoint previousWaypoint = null)
        {
            var index = previousWaypoint == null ? waypoints.Count : waypoints.IndexOf(previousWaypoint) + 1;
            AddWaypoint(waypoint, index);
        }

        /// <summary>
        /// Adds waypoint to this agent at the index position
        /// </summary>
        /// <param name="waypoint">Waypoint that will be added to this agent</param>
        /// <param name="index">Index position where waypoint will be added</param>
        public void AddWaypoint(ScenarioWaypoint waypoint, int index)
        {
            if (index > waypoints.Count)
                index = waypoints.Count;
            if (index < 0)
                index = 0;
            AddTrigger(waypoint.LinkedTrigger);
            waypoints.Insert(index, waypoint);
            waypoint.ParentAgent = this;
            var waypointTransform = waypoint.transform;
            waypointTransform.SetParent(WaypointsParent);
            lineRenderer.positionCount = waypoints.Count + 1;
            for (var i = index; i < waypoints.Count; i++)
            {
                var position = lineRendererPositionOffset + waypoints[i].transform.localPosition;
                lineRenderer.SetPosition(i + 1, position);
            }
        }

        /// <summary>
        /// Removes the waypoint from this agent
        /// </summary>
        /// <param name="waypoint">Waypoint that will be removed from this agent</param>
        public void RemoveWaypoint(ScenarioWaypoint waypoint)
        {
            var index = waypoints.IndexOf(waypoint);
            waypoints.Remove(waypoint);
            waypoint.LinkedTrigger.LinkedWaypoint = null;
            RemoveTrigger(waypoint.LinkedTrigger);
            for (var i = index; i < waypoints.Count; i++)
            {
                var position = lineRendererPositionOffset + waypoints[i].transform.transform.localPosition;
                lineRenderer.SetPosition(i + 1, position);
            }

            lineRenderer.positionCount = waypoints.Count + 1;
        }

        /// <summary>
        /// Method that updates line rendered, has to be called every time when waypoint changes the position
        /// </summary>
        /// <param name="waypoint">Waypoint that changed it's position</param>
        public void WaypointPositionChanged(ScenarioWaypoint waypoint)
        {
            var index = waypoints.IndexOf(waypoint);
            var position = lineRendererPositionOffset + waypoint.transform.transform.localPosition;
            lineRenderer.SetPosition(index + 1, position);
        }

        /// <summary>
        /// Adds trigger to this agent
        /// </summary>
        /// <param name="trigger">Trigger that will be added to this agent</param>
        public void AddTrigger(ScenarioTrigger trigger)
        {
            triggers.Add(trigger);
            trigger.ParentAgent = this;
        }

        /// <summary>
        /// Removes trigger from this agent
        /// </summary>
        /// <param name="trigger">Trigger that will be removed from this agent</param>
        public void RemoveTrigger(ScenarioTrigger trigger)
        {
            triggers.Remove(trigger);
        }
    }
}