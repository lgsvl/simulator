/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Agent
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Agents;
    using Elements;
    using Managers;
    using SimpleJSON;
    using Undo;
    using Undo.Records;
    using UnityEngine;

    /// <inheritdoc cref="Simulator.ScenarioEditor.Elements.ScenarioElement" />
    /// <remarks>
    /// Scenario agent representation
    /// </remarks>
    public class ScenarioAgent : ScenarioElementWithVariant
    {
        /// <summary>
        /// The position offset that will be applied to the line renderer of waypoints
        /// </summary>
        private static Vector3 lineRendererPositionOffset = new Vector3(0.0f, 1.0f, 0.0f);

        /// <summary>
        /// Name for the gameobject containing waypoints
        /// </summary>
        private static string waypointsObjectName = "Waypoints";
        
        /// <summary>
        /// Line renderer for displaying the connection between waypoints
        /// </summary>
        private LineRenderer pathRenderer;

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
                    waypointsParent = transform.Find(waypointsObjectName);
                if (waypointsParent == null)
                {
                    var newGameObject = new GameObject(waypointsObjectName);
                    waypointsParent = newGameObject.transform;
                    waypointsParent.SetParent(transform);
                    waypointsParent.localPosition = Vector3.zero;
                    PathRenderer.material = ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>()
                        .waypointPathMaterial;
                    PathRenderer.useWorldSpace = false;
                    PathRenderer.positionCount = 1;
                    PathRenderer.SetPosition(0, lineRendererPositionOffset);
                    PathRenderer.sortingLayerName = "Ignore Raycast";
                    PathRenderer.widthMultiplier = 1.0f;
                    PathRenderer.generateLightingData = false;
                    PathRenderer.textureMode = LineTextureMode.Tile;
                }

                return waypointsParent;
            }
        }

        /// <summary>
        /// Line renderer for displaying the connection between waypoints
        /// </summary>
        public LineRenderer PathRenderer
        {
            get
            {
                if (pathRenderer != null) return pathRenderer;

                pathRenderer = WaypointsParent.gameObject.GetComponent<LineRenderer>();
                if (pathRenderer == null)
                    pathRenderer = WaypointsParent.gameObject.AddComponent<LineRenderer>();
                return pathRenderer;
            }
        }

        /// <inheritdoc/>
        public override bool CanBeCopied => true;

        /// <inheritdoc/>
        public override Transform TransformForPlayback => modelInstance.transform;

        /// <summary>
        /// Behaviour that will control this agent in the simulation
        /// </summary>
        public string Behaviour { get; private set; }
        
        /// <summary>
        /// Parameters used by the set behaviour
        /// </summary>
        public JSONObject BehaviourParameters { get; set; } = new JSONObject();
        
        /// <summary>
        /// 
        /// </summary>
        public ScenarioDestinationPoint DestinationPoint { get; set; }

        /// <summary>
        /// Parent source of this scenario agent
        /// </summary>
        public ScenarioAgentSource Source => source as ScenarioAgentSource;

        /// <summary>
        /// This agent variant
        /// </summary>
        public AgentVariant Variant => variant as AgentVariant;

        /// <summary>
        /// Included waypoints that this agent will follow
        /// </summary>
        public List<ScenarioWaypoint> Waypoints => waypoints;

        /// <summary>
        /// Type of this agent
        /// </summary>
        public AgentType Type => Source.AgentType;

        /// <summary>
        /// Event invoked when this agent changes the behaviour
        /// </summary>
        public event Action<string> BehaviourChanged;

        /// <inheritdoc/>
        public override void Setup(ScenarioElementSource source, SourceVariant variant)
        {
            base.Setup(source, variant);
            ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>().RegisterAgent(this);
        }

        /// <summary>
        /// Changes the current agent behaviour
        /// </summary>
        /// <param name="newBehaviour">New agent behaviour</param>
        /// <param name="registerUndo">If true, this action can be undone</param>
        public void ChangeBehaviour(string newBehaviour, bool registerUndo = true)
        {
            if (registerUndo)
                ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                    .RegisterRecord(new UndoChangeBehaviour(this));
            Behaviour = newBehaviour;
            WaypointsParent.gameObject.SetActive(Source.AgentSupportWaypoints(this));
            BehaviourChanged?.Invoke(Behaviour);
        }

        /// <inheritdoc/>
        public override void RemoveFromMap()
        {
            base.RemoveFromMap();
            ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>().UnregisterAgent(this);
        }

        /// <inheritdoc/>
        public override void UndoRemove()
        {
            base.UndoRemove();
            ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>().RegisterAgent(this);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            for (var i = waypoints.Count - 1; i >= 0; i--)
            {
                var waypoint = waypoints[i];
                waypoint.RemoveFromMap();
                waypoint.Dispose();
            }

            base.Dispose();
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioElement origin)
        {
            var originAgent = origin as ScenarioAgent;
            if (originAgent == null)
                throw new ArgumentException(
                    $"Invalid origin scenario element type ({origin.GetType().Name}) when cloning {GetType().Name}.");
            base.CopyProperties(origin);
            Behaviour = originAgent.Behaviour;
            PathRenderer.positionCount = 0;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == waypointsObjectName)
                {
                    waypointsParent = child;
                    for (var j = 0; j < waypointsParent.childCount; j++)
                    {
                        var waypoint = waypointsParent.GetChild(j).GetComponent<ScenarioWaypoint>();
                        AddWaypoint(waypoint, waypoint.IndexInAgent);
                    }
                }
            }

            ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>().RegisterAgent(this);
        }

        /// <inheritdoc/>
        public override void ForceMove(Vector3 requestedPosition)
        {
            base.ForceMove(requestedPosition);
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            switch (Type)
            {
                case AgentType.Ego:
                case AgentType.Npc:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic,
                        TransformToMove, TransformToRotate);
                    break;
                case AgentType.Pedestrian:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian,
                        TransformToMove, TransformToRotate);
                    break;
            }
        }

        /// <summary>
        /// Adds waypoint to this agent right after previous waypoint
        /// </summary>
        /// <param name="waypoint">Waypoint that will be added to this agent</param>
        /// <param name="copyProperties">If true, waypoint will copy properties from the previous waypoint</param>
        /// <param name="previousWaypoint">New waypoint will be added after previous waypoint, if null new is added as last</param>
        public void AddWaypoint(ScenarioWaypoint waypoint, bool copyProperties = true,
            ScenarioWaypoint previousWaypoint = null)
        {
            var index = previousWaypoint == null ? waypoints.Count : waypoints.IndexOf(previousWaypoint) + 1;
            if (copyProperties && index > 0)
                waypoint.CopyProperties(waypoints[index - 1]);

            AddWaypoint(waypoint, index);
        }

        /// <summary>
        /// Adds waypoint to this agent at the index position
        /// </summary>
        /// <param name="waypoint">Waypoint that will be added to this agent</param>
        /// <param name="index">Index position where waypoint will be added</param>
        public int AddWaypoint(ScenarioWaypoint waypoint, int index)
        {
            if (index > waypoints.Count)
                index = waypoints.Count;
            if (index < 0)
                index = 0;
            AddTrigger(waypoint.LinkedTrigger);
            waypoints.Insert(index, waypoint);
            waypoint.ParentAgent = this;
            waypoint.IndexInAgent = index;
            var waypointTransform = waypoint.transform;
            waypointTransform.SetParent(WaypointsParent);
            PathRenderer.positionCount = waypoints.Count + 1;
            for (var i = index; i < waypoints.Count; i++)
            {
                var position = lineRendererPositionOffset + waypoints[i].transform.localPosition;
                PathRenderer.SetPosition(i + 1, position);
                waypoints[i].IndexInAgent = i;
            }

            return index;
        }

        /// <summary>
        /// Removes the waypoint from this agent
        /// </summary>
        /// <param name="waypoint">Waypoint that will be removed from this agent</param>
        public int RemoveWaypoint(ScenarioWaypoint waypoint)
        {
            var index = waypoints.IndexOf(waypoint);
            waypoints.Remove(waypoint);
            waypoint.LinkedTrigger.LinkedWaypoint = null;
            RemoveTrigger(waypoint.LinkedTrigger);
            for (var i = index; i < waypoints.Count; i++)
            {
                var position = lineRendererPositionOffset + waypoints[i].transform.transform.localPosition;
                PathRenderer.SetPosition(i + 1, position);
                waypoints[i].IndexInAgent = i;
            }

            PathRenderer.positionCount = waypoints.Count + 1;
            return index;
        }

        /// <summary>
        /// Method that updates line rendered, has to be called every time when waypoint changes the position
        /// </summary>
        /// <param name="waypoint">Waypoint that changed it's position</param>
        public void WaypointPositionChanged(ScenarioWaypoint waypoint)
        {
            var index = waypoints.IndexOf(waypoint);
            var position = lineRendererPositionOffset + waypoint.transform.transform.localPosition;
            PathRenderer.SetPosition(index + 1, position);
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
