/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Agents
{
    using System;
    using System.Collections.Generic;
    using Managers;
    using SimpleJSON;
    using UnityEngine;

    /// <summary>
    /// Scenario agent extension that handles the waypoints
    /// </summary>
    public class AgentWaypoints : ScenarioAgentExtension
    {
        /// <summary>
        /// The position offset that will be applied to the line renderer of waypoints
        /// </summary>
        private static readonly Vector3 LineRendererPositionOffset = new Vector3(0.0f, 0.1f, 0.0f);

        /// <summary>
        /// Name for the gameobject containing waypoints
        /// </summary>
        private const string WaypointsObjectName = "Waypoints";

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
        private readonly List<ScenarioWaypoint> waypoints = new List<ScenarioWaypoint>();

        /// <summary>
        /// Included triggers that will influence this agent
        /// </summary>
        private readonly List<ScenarioTrigger> triggers = new List<ScenarioTrigger>();

        /// <summary>
        /// Included waypoints that this agent will follow
        /// </summary>
        public List<ScenarioWaypoint> Waypoints => waypoints;


        /// <summary>
        /// Waypoints parent where inherited waypoints objects will be added
        /// </summary>
        public Transform WaypointsParent
        {
            get
            {
                if (waypointsParent == null)
                    waypointsParent = ParentAgent.transform.Find(WaypointsObjectName);
                if (waypointsParent == null)
                {
                    var newGameObject = new GameObject(WaypointsObjectName);
                    waypointsParent = newGameObject.transform;
                    waypointsParent.SetParent(ParentAgent.transform);
                    waypointsParent.localPosition = Vector3.zero;
                    IsActiveChanged?.Invoke(true);
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
                {
                    pathRenderer = WaypointsParent.gameObject.AddComponent<LineRenderer>();
                    pathRenderer.material = ParentAgent.Source.WaypointsMaterial;
                    pathRenderer.useWorldSpace = false;
                    pathRenderer.positionCount = 1;
                    pathRenderer.SetPosition(0, LineRendererPositionOffset);
                    pathRenderer.sortingLayerName = "Ignore Raycast";
                    pathRenderer.widthMultiplier = 0.1f;
                    pathRenderer.generateLightingData = false;
                    pathRenderer.textureMode = LineTextureMode.Tile;
                }

                return pathRenderer;
            }
        }

        /// <summary>
        /// Event invoked when the is active state of waypoints has changed.
        /// </summary>
        public event Action<bool> IsActiveChanged;

        /// <inheritdoc/>
        public override void Initialize(ScenarioAgent parentAgent)
        {
            base.Initialize(parentAgent);
            ParentAgent.ExtensionAdded += ParentAgentOnExtensionAdded;
            ParentAgent.ExtensionRemoved += ParentAgentOnExtensionRemoved;
            var behaviourExtension = ParentAgent.GetExtension<AgentBehaviour>();
            if (behaviourExtension!=null)
                behaviourExtension.BehaviourChanged += ParentAgentOnBehaviourChanged;
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            ParentAgent.ExtensionAdded -= ParentAgentOnExtensionAdded;
            ParentAgent.ExtensionRemoved -= ParentAgentOnExtensionRemoved;
            var behaviourExtension = ParentAgent.GetExtension<AgentBehaviour>();
            if (behaviourExtension!=null)
                behaviourExtension.BehaviourChanged -= ParentAgentOnBehaviourChanged;
            for (var i = waypoints.Count - 1; i >= 0; i--)
            {
                var waypoint = waypoints[i];
                waypoint.RemoveFromMap();
                waypoint.Dispose();
            }

            base.Deinitialize();
        }

        /// <summary>
        /// Method invoked when an extension is added to the parent agent
        /// </summary>
        /// <param name="extension">Added extension</param>
        private void ParentAgentOnExtensionAdded(ScenarioAgentExtension extension)
        {
            if (extension is AgentBehaviour behaviourExtension)
                behaviourExtension.BehaviourChanged += ParentAgentOnBehaviourChanged;
        }

        /// <summary>
        /// Method invoked when an extension is removed from the parent agent
        /// </summary>
        /// <param name="extension">Removed extension</param>
        private void ParentAgentOnExtensionRemoved(ScenarioAgentExtension extension)
        {
            if (extension is AgentBehaviour behaviourExtension)
                behaviourExtension.BehaviourChanged -= ParentAgentOnBehaviourChanged;
        }


        /// <inheritdoc/>
        public override void SerializeToJson(JSONNode agentNode)
        {
            var waypointsNode = agentNode.GetValueOrDefault("waypoints", new JSONArray());
            if (!agentNode.HasKey("waypoints"))
                agentNode.Add("waypoints", waypointsNode);

            var angle = Vector3.zero;
            for (var i = 0; i < Waypoints.Count; i++)
            {
                var scenarioWaypoint = Waypoints[i];
                var waypointNode = new JSONObject();
                var position = new JSONObject().WriteVector3(scenarioWaypoint.transform.position);
                var hasNextWaypoint = i + 1 < Waypoints.Count;
                var hasPreviousWaypoint = i > 0;
                angle = hasNextWaypoint
                    ? Quaternion.LookRotation(Waypoints[i + 1].transform.position - position).eulerAngles
                    : (hasPreviousWaypoint ? 
                        Quaternion.LookRotation(position - Waypoints[i - 1].transform.position).eulerAngles
                        : ParentAgent.transform.eulerAngles);
                waypointNode.Add("ordinalNumber", new JSONNumber(i));
                waypointNode.Add("position", position);
                waypointNode.Add("angle", angle);
                waypointNode.Add("waitTime", new JSONNumber(scenarioWaypoint.WaitTime));
                waypointNode.Add("speed", new JSONNumber(scenarioWaypoint.Speed));
                AddTriggerNode(waypointNode, scenarioWaypoint.LinkedTrigger);
                waypointsNode.Add(waypointNode);
            }
        }

        /// <summary>
        /// Adds triggers nodes to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="scenarioTrigger">Scenario trigger to serialize</param>
        private static void AddTriggerNode(JSONObject data, ScenarioTrigger scenarioTrigger)
        {
            scenarioTrigger.OnBeforeSerialize();
            var triggerNode = scenarioTrigger.Trigger.SerializeTrigger();
            data.Add("trigger", triggerNode);
        }

        /// <inheritdoc/>
        public override void DeserializeFromJson(JSONNode agentNode)
        {
            var waypointsNode = agentNode["waypoints"] as JSONArray;
            if (waypointsNode == null)
                return;

            foreach (var waypointNode in waypointsNode.Children)
            {
                var mapWaypointPrefab =
                    ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>().waypointPrefab;
                var waypointInstance = ScenarioManager.Instance.prefabsPools
                    .GetInstance(mapWaypointPrefab).GetComponent<ScenarioWaypoint>();
                waypointInstance.transform.position = waypointNode["position"].ReadVector3();
                var waitTime = waypointNode["waitTime"];
                if (waitTime == null)
                    waitTime = waypointNode["wait_time"];
                waypointInstance.WaitTime = waitTime;
                waypointInstance.Speed = waypointNode["speed"];
                var ordinalNumber = waypointNode["ordinalNumber"];
                if (ordinalNumber == null)
                    ordinalNumber = waypointNode["ordinal_number"];
                int index = ordinalNumber;
                //TODO sort waypoints
                AddWaypoint(waypointInstance, index);
                DeserializeTrigger(waypointInstance.LinkedTrigger, waypointNode["trigger"]);
            }

            WaypointsParent.gameObject.SetActive(true);
            IsActiveChanged?.Invoke(true);
        }

        /// <summary>
        /// Deserializes a trigger from the json data
        /// </summary>
        /// <param name="trigger">Trigger object to fill with effectors</param>
        /// <param name="triggerNode">Json data with a trigger</param>
        private static void DeserializeTrigger(ScenarioTrigger trigger, JSONNode triggerNode)
        {
            trigger.Trigger = WaypointTrigger.DeserializeTrigger(triggerNode);
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioAgent agent)
        {
            var origin = agent.GetExtension<AgentWaypoints>();
            if (origin == null) return;

            PathRenderer.positionCount = 0;
            for (var i = 0; i < ParentAgent.transform.childCount; i++)
            {
                var child = ParentAgent.transform.GetChild(i);
                if (child.name == WaypointsObjectName)
                {
                    waypointsParent = child;
                    for (var j = 0; j < waypointsParent.childCount; j++)
                    {
                        var waypoint = waypointsParent.GetChild(j).GetComponent<ScenarioWaypoint>();
                        AddWaypoint(waypoint, waypoint.IndexInAgent);
                    }
                }
            }
        }

        /// <summary>
        /// Method invoked when the parent agent's behaviour changes
        /// </summary>
        /// <param name="newBehaviour">New behaviour of the parent agent</param>
        private void ParentAgentOnBehaviourChanged(string newBehaviour)
        {
            var isActive = ParentAgent.Source.AgentSupportWaypoints(ParentAgent);
            WaypointsParent.gameObject.SetActive(isActive);
            IsActiveChanged?.Invoke(isActive);
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
            waypoint.ParentAgent = ParentAgent;
            waypoint.ParentAgentWaypoints = this;
            waypoint.waypointRenderer.material = ParentAgent.Source.WaypointsMaterial;
            waypoint.IndexInAgent = index;
            var waypointTransform = waypoint.transform;
            waypointTransform.SetParent(WaypointsParent);
            waypointTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            PathRenderer.positionCount = waypoints.Count + 1;
            for (var i = index; i < waypoints.Count; i++)
            {
                var position = LineRendererPositionOffset + waypoints[i].transform.localPosition;
                PathRenderer.SetPosition(i + 1, position);
                waypoints[i].IndexInAgent = i;
            }

            WaypointPositionChanged(waypoint);

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
            RemoveTrigger(waypoint.LinkedTrigger);
            for (var i = index; i < waypoints.Count; i++)
            {
                var position = LineRendererPositionOffset + waypoints[i].transform.transform.localPosition;
                PathRenderer.SetPosition(i + 1, position);
                waypoints[i].IndexInAgent = i;
            }

            PathRenderer.positionCount = waypoints.Count + 1;

            //Update position after removing an element
            if (index < waypoints.Count)
                WaypointPositionChanged(waypoints[index]);
            return index;
        }

        /// <summary>
        /// Method that updates line rendered, has to be called every time when waypoint changes the position
        /// </summary>
        /// <param name="waypoint">Waypoint that changed it's position</param>
        public void WaypointPositionChanged(ScenarioWaypoint waypoint)
        {
            var index = waypoints.IndexOf(waypoint);
            var position = LineRendererPositionOffset + waypoint.transform.transform.localPosition;
            PathRenderer.SetPosition(index + 1, position);

            //Update waypoint direction indicator
            var previousPosition = PathRenderer.GetPosition(index) - position;
            waypoint.directionTransform.localPosition = previousPosition / 2.0f;
            waypoint.directionTransform.localRotation = previousPosition.sqrMagnitude > 0.0f
                ? Quaternion.LookRotation(-previousPosition)
                : Quaternion.Euler(0.0f, 0.0f, 0.0f);

            if (index + 1 < waypoints.Count)
            {
                var nextPosition = position - PathRenderer.GetPosition(index + 2);
                var nextWaypoint = waypoints[index + 1];
                nextWaypoint.directionTransform.localPosition = nextPosition / 2.0f;
                nextWaypoint.directionTransform.localRotation = nextPosition.sqrMagnitude > 0.0f
                    ? Quaternion.LookRotation(-nextPosition)
                    : Quaternion.Euler(0.0f, 0.0f, 0.0f);
            }
        }

        /// <summary>
        /// Adds trigger to this agent
        /// </summary>
        /// <param name="trigger">Trigger that will be added to this agent</param>
        public void AddTrigger(ScenarioTrigger trigger)
        {
            triggers.Add(trigger);
            trigger.TargetAgentType = ParentAgent.Source.AgentType;
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