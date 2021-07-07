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
    using Simulator.Utilities;
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Scenario agent extension that handles the waypoints
    /// </summary>
    public class AgentWaypoints : ScenarioAgentExtension
    {
        /// <summary>
        /// Minimal waypoint implementation for Bezier calculations
        /// </summary>
        public struct Waypoint : IWaypoint
        {
            /// <inheritdoc/>
            public Vector3 Position { get; set; }

            /// <inheritdoc/>
            public Vector3 Angle { get; set; }

            public float Speed { get; set; }

            /// <inheritdoc/>
            public IWaypoint Clone()
            {
                return new Waypoint()
                {
                    Position = Position,
                    Angle = Angle,
                    Speed = Speed
                };
            }

            /// <inheritdoc/>
            public IWaypoint GetControlPoint()
            {
                return new Waypoint()
                {
                    Position = Position,
                    Angle = Angle,
                    Speed = Speed
                };
            }
        }

        /// <summary>
        /// The position offset that will be applied to the line renderer of waypoints
        /// </summary>
        public static readonly Vector3 LineRendererPositionOffset = new Vector3(0.0f, 0.1f, 0.0f);

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
        /// Waypoints path type
        /// </summary>
        private WaypointsPathType pathType;

        /// <summary>
        /// Should this waypoints path be looped
        /// </summary>
        private bool loop;

        /// <summary>
        /// Precalculated bezier spline
        /// </summary>
        private BezierSpline<Waypoint> bezierSpline;

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
                    pathRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    ParentAgent.OnModelChanged();
                }

                return pathRenderer;
            }
        }

        /// <summary>
        /// Waypoints path type
        /// </summary>
        public WaypointsPathType PathType => pathType;

        /// <summary>
        /// Should this waypoints path be looped
        /// </summary>
        public bool Loop
        {
            get => loop;
            set => loop = value;
        }

        /// <summary>
        /// Precalculated bezier spline
        /// </summary>
        public BezierSpline<Waypoint> CachedBezierSpline => bezierSpline;

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
            if (behaviourExtension != null)
                behaviourExtension.BehaviourChanged += ParentAgentOnBehaviourChanged;
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            ParentAgent.ExtensionAdded -= ParentAgentOnExtensionAdded;
            ParentAgent.ExtensionRemoved -= ParentAgentOnExtensionRemoved;
            var behaviourExtension = ParentAgent.GetExtension<AgentBehaviour>();
            if (behaviourExtension != null)
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

        /// <summary>
        /// Changes the waypoints path type
        /// </summary>
        /// <param name="newPathType">New waypoints path type</param>
        public void ChangePathType(WaypointsPathType newPathType)
        {
            if (Equals(pathType, newPathType))
                return;
            pathType = newPathType;
            switch (pathType)
            {
                case WaypointsPathType.Linear:
                    var positions = new Vector3[waypoints.Count + 1];
                    positions[0] = LineRendererPositionOffset;
                    for (var i = 0; i < waypoints.Count; i++)
                    {
                        positions[i + 1] = LineRendererPositionOffset + waypoints[i].transform.localPosition;
                    }

                    PathRenderer.positionCount = waypoints.Count + 1;
                    PathRenderer.SetPositions(positions);
                    UpdateDirectionTransforms();
                    break;
                case WaypointsPathType.BezierSpline:
                    RecalculateBezierSpline();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc/>
        public override void SerializeToJson(JSONNode agentNode)
        {
            var waypointsNode = agentNode.GetValueOrDefault("waypoints", new JSONArray());
            if (!agentNode.HasKey("waypoints"))
                agentNode.Add("waypoints", waypointsNode);
            agentNode.Add("waypoints_path_type", new JSONString(pathType.ToString()));
            agentNode.Add("waypoints_loop", new JSONBool(loop));

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
                    : (hasPreviousWaypoint
                        ? Quaternion.LookRotation(position - Waypoints[i - 1].transform.position).eulerAngles
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
            // Try parse the path type, set linear if parsing fails
            var pathTypeNode = agentNode["waypoints_path_type"];
            if (pathTypeNode == null || !Enum.TryParse(pathTypeNode, true, out pathType))
            {
                pathType = WaypointsPathType.Linear;
            }

            var loopNode = agentNode["waypoints_loop"] as JSONBool;
            if (loopNode != null)
            {
                loop = loopNode;
            }

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
            pathType = origin.pathType;
            bezierSpline = origin.CachedBezierSpline;
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
            for (var i = index; i < waypoints.Count; i++)
            {
                waypoints[i].IndexInAgent = i;
            }

            switch (pathType)
            {
                case WaypointsPathType.Linear:
                    PathRenderer.positionCount = waypoints.Count + 1;
                    for (var i = index; i < waypoints.Count; i++)
                    {
                        var position = LineRendererPositionOffset + waypoints[i].transform.localPosition;
                        PathRenderer.SetPosition(i + 1, position);
                    }

                    break;
                case WaypointsPathType.BezierSpline:
                    RecalculateBezierSpline();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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
                waypoints[i].IndexInAgent = i;
            }

            switch (pathType)
            {
                case WaypointsPathType.Linear:
                    for (var i = index; i < waypoints.Count; i++)
                    {
                        var position = LineRendererPositionOffset + waypoints[i].transform.transform.localPosition;
                        PathRenderer.SetPosition(i + 1, position);
                    }

                    PathRenderer.positionCount = waypoints.Count + 1;
                    break;
                case WaypointsPathType.BezierSpline:
                    RecalculateBezierSpline();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //Update position after removing an element
            if (index < waypoints.Count)
                WaypointPositionChanged(waypoints[index]);
            return index;
        }

        /// <summary>
        /// Method that updates line renderer, has to be called every time when waypoint changes the position
        /// </summary>
        /// <param name="waypoint">Waypoint that changed it's position</param>
        public void WaypointPositionChanged(ScenarioWaypoint waypoint)
        {
            var index = waypoints.IndexOf(waypoint);

            switch (pathType)
            {
                case WaypointsPathType.Linear:
                    var position = LineRendererPositionOffset + waypoint.transform.transform.localPosition;
                    PathRenderer.SetPosition(index + 1, position);
                    UpdateDirectionTransforms(index, index + 1);
                    break;
                case WaypointsPathType.BezierSpline:
                    RecalculateBezierSpline();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Method that updates cached data, which uses speed from waypoints
        /// </summary>
        /// <param name="waypoint">Waypoint that changed it's speed</param>
        public void WaypointSpeedChanged(ScenarioWaypoint waypoint)
        {
            switch (pathType)
            {
                case WaypointsPathType.Linear:
                    break;
                case WaypointsPathType.BezierSpline:
                    RecalculateBezierSpline();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Updates direction arrows transforms according to the current state
        /// </summary>
        /// <param name="startIndex">Start index of waypoints range that will be updated</param>
        /// <param name="endIndex">End index of waypoints range that will be updated</param>
        private void UpdateDirectionTransforms(int startIndex = 0, int endIndex = -1)
        {
            if (startIndex < 0 || startIndex >= waypoints.Count)
                return;
            if (endIndex < 0)
                endIndex = waypoints.Count - 1;
            endIndex = Mathf.Clamp(endIndex, startIndex, waypoints.Count - 1);
            //Update waypoint direction indicator
            switch (pathType)
            {
                case WaypointsPathType.Linear:
                    for (var i = startIndex; i <= endIndex; i++)
                    {
                        var position = PathRenderer.GetPosition(i + 1);
                        var previousPosition = PathRenderer.GetPosition(i);
                        var directionVector = previousPosition - position;
                        var waypoint = waypoints[i];
                        waypoint.directionTransform.localPosition = directionVector / 2.0f;
                        waypoint.directionTransform.localRotation = directionVector.sqrMagnitude > 0.0f
                            ? Quaternion.LookRotation(-directionVector)
                            : Quaternion.Euler(0.0f, 0.0f, 0.0f);
                        waypoint.directionTransform.gameObject.SetActive(true);
                    }

                    break;
                case WaypointsPathType.BezierSpline:
                    for (var i = startIndex; i <= endIndex; i++)
                    {
                        var waypoint = waypoints[i];

                        var position = waypoint.TransformToMove.position;
                        var previousPosition =
                            i == 0 ? ParentAgent.transform.position : waypoints[i - 1].TransformToMove.position;
                        var directionVector = previousPosition - position;
                        waypoint.directionTransform.localPosition =
                            waypoint.transform.InverseTransformPoint(position + directionVector / 2.0f);
                        waypoint.directionTransform.localRotation = directionVector.sqrMagnitude > 0.0f
                            ? Quaternion.LookRotation(-directionVector)
                            : Quaternion.Euler(0.0f, 0.0f, 0.0f);
                        waypoint.directionTransform.gameObject.SetActive(true);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Recalculates Bezier spline
        /// </summary>
        private void RecalculateBezierSpline()
        {
            if (waypoints.Count < 1)
            {
                bezierSpline = new BezierSpline<Waypoint>(new Waypoint[0], 0.01f);
                return;
            }

            var inputWaypoints = new Waypoint[waypoints.Count + 1];
            inputWaypoints[0] = new Waypoint
            {
                Position = Vector3.zero,
                Speed = waypoints[0].Speed
            };
            for (var i = 0; i < waypoints.Count; i++)
            {
                inputWaypoints[i + 1] = new Waypoint
                {
                    Position = waypoints[i].transform.localPosition,
                    Speed = waypoints[i].Speed
                };
            }

            bezierSpline = new BezierSpline<Waypoint>(inputWaypoints, 0.01f);

            var bezierWaypoints = CachedBezierSpline.GetBezierWaypoints();
            if (bezierWaypoints != null)
            {
                PathRenderer.positionCount = bezierWaypoints.Count;
                for (var i = 0; i < bezierWaypoints.Count; i++)
                    PathRenderer.SetPosition(i, bezierWaypoints[i].Position + LineRendererPositionOffset);
            }

            UpdateDirectionTransforms();
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
