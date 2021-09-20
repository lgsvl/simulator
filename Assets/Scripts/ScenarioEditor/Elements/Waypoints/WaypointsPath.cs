/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Waypoints
{
    using System;
    using System.Collections.Generic;
    using Agents;
    using Managers;
    using SimpleJSON;
    using Simulator.Utilities;
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Scenario agent extension that handles the waypoints
    /// </summary>
    public class WaypointsPath : IScenarioElementExtension
    {
        /// <summary>
        /// Minimal waypoint implementation for Bezier calculations
        /// </summary>
        public class Waypoint : IWaypoint
        {
            /// <inheritdoc/>
            public Vector3 Position { get; set; }

            /// <inheritdoc/>
            public Vector3 Angle { get; set; }

            /// <summary>
            /// Maximum speed that agent will try to achieve when reaching this waypoint
            /// Applied immediately if acceleration is not given
            /// </summary>
            public float MaxSpeed { get; set; }

            /// <summary>
            /// Acceleration used to reach max speed while traveling to this waypoint
            /// </summary>
            public float Acceleration { get; set; }

            /// <inheritdoc/>
            public virtual IWaypoint Clone()
            {
                return new Waypoint()
                {
                    Position = Position,
                    Angle = Angle,
                    MaxSpeed = MaxSpeed,
                    Acceleration = Acceleration
                };
            }

            /// <inheritdoc/>
            public virtual IWaypoint GetControlPoint()
            {
                return new Waypoint()
                {
                    Position = Position,
                    Angle = Angle,
                    MaxSpeed = MaxSpeed,
                    Acceleration = Acceleration
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
        protected virtual string WaypointsObjectName { get; } = "Waypoints";

        /// <summary>
        /// Name for the JSON node object
        /// </summary>
        protected virtual string JsonNodeName { get; } = "waypoints";

        /// <summary>
        /// Scenario element that this object extends
        /// </summary>
        protected ScenarioElement ParentElement { get; private set; }

        /// <summary>
        /// Scenario element that starts the waypoints path
        /// </summary>
        public ScenarioElement StartElement { get; private set; }

        /// <summary>
        /// Scenario element that ends the waypoints path
        /// </summary>
        public ScenarioElement EndElement { get; private set; }

        /// <summary>
        /// Line renderer for displaying the connection between waypoints
        /// </summary>
        protected LineRenderer pathRenderer;

        /// <summary>
        /// Waypoints parent where inherited waypoints objects will be added
        /// </summary>
        protected Transform waypointsParent;

        /// <summary>
        /// Waypoints path type
        /// </summary>
        protected WaypointsPathType pathType;

        /// <summary>
        /// Should this waypoints path be looped
        /// </summary>
        protected bool loop;

        /// <summary>
        /// Precalculated bezier spline
        /// </summary>
        protected BezierSpline<Waypoint> bezierSpline;

        /// <summary>
        /// Included waypoints that this agent will follow
        /// </summary>
        protected readonly List<ScenarioWaypoint> waypoints = new List<ScenarioWaypoint>();

        /// <summary>
        /// Included waypoints that this agent will follow
        /// </summary>
        public List<ScenarioWaypoint> Waypoints => waypoints;

        /// <summary>
        /// Index of the first waypoint index in the path array
        /// </summary>
        private int WaypointsStartIndex => StartElement == null ? 0 : 1;

        /// <summary>
        /// Waypoints parent where inherited waypoints objects will be added
        /// </summary>
        public Transform WaypointsParent
        {
            get
            {
                if (waypointsParent == null)
                    waypointsParent = ParentElement.transform.Find(WaypointsObjectName);
                if (waypointsParent == null)
                {
                    var newGameObject = new GameObject(WaypointsObjectName);
                    waypointsParent = newGameObject.transform;
                    waypointsParent.SetParent(ParentElement.transform);
                    waypointsParent.localPosition = Vector3.zero;
                    SetActive(true);
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
                    var waypointsMaterial = GetWaypointsMaterial();
                    if (waypointsMaterial != null)
                        pathRenderer.material = waypointsMaterial;
                    pathRenderer.useWorldSpace = false;
                    pathRenderer.positionCount = 0;
                    pathRenderer.sortingLayerName = "Ignore Raycast";
                    pathRenderer.widthMultiplier = 0.1f;
                    pathRenderer.generateLightingData = false;
                    pathRenderer.textureMode = LineTextureMode.Tile;
                    pathRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    ParentElement.OnModelChanged();
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
        /// Returns true if the path is active, false otherwise
        /// </summary>
        public bool IsActive => WaypointsParent.gameObject.activeSelf;

        /// <summary>
        /// Precalculated bezier spline
        /// </summary>
        public BezierSpline<Waypoint> CachedBezierSpline => bezierSpline;

        /// <summary>
        /// Event invoked when the is active state of waypoints has changed.
        /// </summary>
        public event Action<bool> IsActiveChanged;

        /// <inheritdoc/>
        public virtual void Initialize(ScenarioElement parentElement)
        {
            ParentElement = parentElement;
        }

        /// <inheritdoc/>
        public virtual void Deinitialize()
        {
            for (var i = waypoints.Count - 1; i >= 0; i--)
            {
                var waypoint = waypoints[i];
                waypoint.RemoveFromMap();
                waypoint.Dispose();
            }

            SetStartEndElements(null, null);
        }

        /// <summary>
        /// Sets the start and end scenario elements for this path
        /// </summary>
        /// <param name="start">Start scenario element</param>
        /// <param name="end">End scenario element</param>
        public void SetStartEndElements(ScenarioElement start, ScenarioElement end)
        {
            // Setup start element
            if (StartElement != null)
            {
                StartElement.Moved -= StartElementOnMoved;
            }

            StartElement = start;
            if (StartElement != null)
            {
                StartElement.Moved += StartElementOnMoved;
            }

            // Setup end element
            if (EndElement != null)
            {
                EndElement.Moved -= EndElementOnMoved;
            }

            EndElement = end;
            if (EndElement != null)
            {
                EndElement.Moved += EndElementOnMoved;
            }

            // Update waypoints path
            switch (PathType)
            {
                case WaypointsPathType.Linear:
                    PathRenderer.positionCount = (start == null ? 0 : 1) + (end == null ? 0 : 1);
                    if (start != null)
                        PathRenderer.SetPosition(0,
                            start.TransformToMove.position - waypointsParent.transform.position +
                            LineRendererPositionOffset);
                    if (end != null)
                        PathRenderer.SetPosition(PathRenderer.positionCount - 1,
                            end.TransformToMove.position - waypointsParent.transform.position +
                            LineRendererPositionOffset);
                    break;
                case WaypointsPathType.BezierSpline:
                    RecalculateBezierSpline();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Method invoked when the start element moves
        /// </summary>
        /// <param name="element">Moved start element</param>
        private void StartElementOnMoved(ScenarioElement element)
        {
            switch (PathType)
            {
                case WaypointsPathType.Linear:
                    if (PathRenderer.positionCount <= 0)
                        return;
                    PathRenderer.SetPosition(0,
                        element.TransformToMove.position - waypointsParent.transform.position +
                        LineRendererPositionOffset);
                    break;
                case WaypointsPathType.BezierSpline:
                    RecalculateBezierSpline();
                    if (PathRenderer.positionCount <= 0)
                        return;
                    PathRenderer.SetPosition(0,
                        element.TransformToMove.position - waypointsParent.transform.position +
                        LineRendererPositionOffset);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Method invoked when the end element moves
        /// </summary>
        /// <param name="element">Moved end element</param>
        private void EndElementOnMoved(ScenarioElement element)
        {
            switch (PathType)
            {
                case WaypointsPathType.Linear:
                    PathRenderer.SetPosition(PathRenderer.positionCount - 1,
                        element.TransformToMove.position - waypointsParent.transform.position +
                        LineRendererPositionOffset);
                    break;
                case WaypointsPathType.BezierSpline:
                    RecalculateBezierSpline();
                    PathRenderer.SetPosition(PathRenderer.positionCount - 1,
                        element.TransformToMove.position - waypointsParent.transform.position +
                        LineRendererPositionOffset);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        /// <summary>
        /// Sets the waypoints path parent as active or inactive
        /// </summary>
        /// <param name="isActive">Is waypoints path active</param>
        public void SetActive(bool isActive)
        {
            WaypointsParent.gameObject.SetActive(isActive);
            IsActiveChanged?.Invoke(isActive);
        }

        /// <summary>
        /// Get a waypoint instance proper for this path
        /// </summary>
        /// <returns>Waypoint instance</returns>
        public virtual ScenarioWaypoint GetWaypointInstance()
        {
            return ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>()
                .GetWaypointInstance<ScenarioWaypoint>();
        }

        /// <summary>
        /// Returns waypoints material for current settings
        /// </summary>
        /// <returns>Material set to the waypoints</returns>
        protected virtual Material GetWaypointsMaterial()
        {
            return null;
        }

        /// <summary>
        /// Returns an <see cref="Waypoint"/> data for waypoint of given index
        /// </summary>
        /// <param name="index"><see cref="Waypoint"/> data for waypoint of given index</param>
        /// <returns></returns>
        protected virtual Waypoint GetBezierWaypoint(int index)
        {
            return new Waypoint
            {
                Position = waypoints[index].TransformToMove.localPosition,
                Angle = waypoints[index].TransformToRotate.localRotation.eulerAngles,
                MaxSpeed = waypoints[index].DestinationSpeed,
                Acceleration = waypoints[index].Acceleration
            };
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
                    var positions = new Vector3[waypoints.Count + WaypointsStartIndex + (EndElement == null ? 0 : 1)];
                    positions[0] = LineRendererPositionOffset;
                    for (var i = 0; i < waypoints.Count; i++)
                    {
                        positions[i + WaypointsStartIndex] =
                            LineRendererPositionOffset + waypoints[i].transform.localPosition;
                    }

                    // Reset all the path renderer positions
                    PathRenderer.positionCount = waypoints.Count + WaypointsStartIndex + (EndElement == null ? 0 : 1);
                    if (StartElement != null)
                        PathRenderer.SetPosition(0,
                            StartElement.TransformToMove.position - waypointsParent.transform.position +
                            LineRendererPositionOffset);
                    if (EndElement != null)
                        PathRenderer.SetPosition(PathRenderer.positionCount - 1,
                            EndElement.TransformToMove.position - waypointsParent.transform.position +
                            LineRendererPositionOffset);
                    for (var i = 0; i < waypoints.Count; i++)
                    {
                        var position = LineRendererPositionOffset + waypoints[i].transform.localPosition;
                        PathRenderer.SetPosition(i + WaypointsStartIndex, position);
                    }

                    UpdateDirectionTransforms();

                    // If the last waypoint is moved, rotate transform to it
                    if (waypoints.Count > 0)
                    {
                        if (EndElement != null)
                            EndElement.TransformToRotate.LookAt(2 * EndElement.TransformToMove.position -
                                                                waypoints[waypoints.Count - 1].TransformToMove
                                                                    .position);

                        if (StartElement != null)
                            StartElement.TransformToRotate.LookAt(waypoints[0].TransformToMove);
                    }

                    break;

                case WaypointsPathType.BezierSpline:
                    RecalculateBezierSpline();

                    // Rotate agent to the second bezier point (first is start position)
                    var bezierPoints = bezierSpline.GetBezierWaypoints();
                    if (bezierPoints != null)
                    {
                        if (EndElement != null && bezierPoints.Count > 2)
                            EndElement.TransformToRotate.LookAt(2 * EndElement.TransformToMove.position -
                                                                (bezierPoints[bezierPoints.Count - 2].Position +
                                                                 waypointsParent.position));

                        if (StartElement != null && bezierPoints.Count > 1)
                            StartElement.TransformToRotate.LookAt(bezierPoints[1].Position +
                                                                  waypointsParent.position);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Fake first and last waypoint reposition to 
        }

        /// <inheritdoc/>
        public void SerializeToJson(JSONNode elementNode)
        {
            var waypointsNode = elementNode.GetValueOrDefault(JsonNodeName, new JSONArray());
            if (!elementNode.HasKey(JsonNodeName))
                elementNode.Add(JsonNodeName, waypointsNode);
            elementNode.Add("waypointsPathType", new JSONString(pathType.ToString()));
            elementNode.Add("waypointsLoop", new JSONBool(loop));

            for (var i = 0; i < Waypoints.Count; i++)
            {
                var scenarioWaypoint = Waypoints[i];
                var waypointNode = new JSONObject();
                SerializeWaypoint(scenarioWaypoint, i, waypointNode);
                waypointsNode.Add(waypointNode);
            }
        }

        /// <summary>
        /// Serializes the waypoint with given index to the waypoint node
        /// </summary>
        /// <param name="waypoint">Scenario waypoint to serialize</param>
        /// <param name="waypointIndex">Index of the given scenario waypoint</param>
        /// <param name="waypointNode">Json node where the waypoint will be serialized</param>
        protected virtual void SerializeWaypoint(ScenarioWaypoint waypoint, int waypointIndex, JSONObject waypointNode)
        {
            var position = new JSONObject().WriteVector3(waypoint.TransformToMove.position);
            var previousPosition = waypointIndex > 0 ? Waypoints[waypointIndex - 1].TransformToMove.position : StartElement.TransformToMove.position;
            var angle = Quaternion.LookRotation((position - previousPosition).normalized).eulerAngles;
            waypointNode.Add("ordinalNumber", new JSONNumber(waypointIndex));
            waypointNode.Add("position", position);
            waypointNode.Add("angle", angle);
            waypointNode.Add("speed", new JSONNumber(waypoint.DestinationSpeed));
            waypointNode.Add("acceleration", new JSONNumber(waypoint.Acceleration));
        }

        /// <inheritdoc/>
        public void DeserializeFromJson(JSONNode elementNode)
        {
            var waypointsNode = elementNode[JsonNodeName] as JSONArray;
            if (waypointsNode == null)
                return;
            // Try parse the path type, set linear if parsing fails
            var pathTypeNode = elementNode["waypointsPathType"];
            if (pathTypeNode == null)
            {
                pathTypeNode = elementNode["waypoints_path_type"];
            }
            if (pathTypeNode == null || !Enum.TryParse(pathTypeNode, true, out pathType))
            {
                pathType = WaypointsPathType.Linear;
            }

            var loopNode = elementNode["waypointsLoop"] as JSONBool;
            if (loopNode == null)
            {
                loopNode = elementNode["waypoints_loop"] as JSONBool;
            }
            if (loopNode != null)
            {
                loop = loopNode;
            }

            foreach (var waypointNode in waypointsNode.Children)
            {
                DeserializeWaypoint(GetWaypointInstance(), waypointNode);
            }

            SetActive(true);
        }

        /// <summary>
        /// Deserializes a single waypoint node
        /// </summary>
        /// <param name="waypoint">Scenario waypoint instance instantiated for this node</param>
        /// <param name="waypointNode">JSON Node data for the waypoint</param>
        protected virtual void DeserializeWaypoint(ScenarioWaypoint waypoint, JSONNode waypointNode)
        {
            waypoint.transform.position = waypointNode["position"].ReadVector3();
            var ordinalNumber = waypointNode["ordinalNumber"];
            if (ordinalNumber == null)
                ordinalNumber = waypointNode["ordinal_number"];
            int index = ordinalNumber;
            waypoint.DestinationSpeed = waypointNode["speed"];
            waypoint.Acceleration = waypointNode["acceleration"];
            //TODO sort waypoints
            waypoint.IndexInAgent = index;
            AddWaypoint(waypoint);
        }

        /// <inheritdoc/>
        public virtual void CopyProperties(ScenarioElement originElement)
        {
            var originAgent = (ScenarioAgent) originElement;
            var origin = originAgent.GetWaypointsPath();
            if (origin == null)
            {
                ScenarioManager.Instance.logPanel.EnqueueError(
                    $"Cannot copy waypoints path from {originElement.GetType()} - currently unimplemented.");
                return;
            }

            CopyProperties(origin);
        }

        /// <summary>
        /// Method called after this element is instantiated using copied waypoints path
        /// </summary>
        /// <param name="origin">Origin waypoints path from which copy was created</param>
        public void CopyProperties(WaypointsPath origin)
        {
            PathRenderer.positionCount = 0;
            for (var i = 0; i < ParentElement.transform.childCount; i++)
            {
                var child = ParentElement.transform.GetChild(i);
                if (child.name == WaypointsObjectName)
                {
                    waypointsParent = child;
                    for (var j = 0; j < waypointsParent.childCount; j++)
                    {
                        var waypoint = waypointsParent.GetChild(j).GetComponent<ScenarioWaypoint>();
                        AddWaypoint(waypoint);
                    }
                }
            }
            
            ChangePathType(origin.pathType);
        }

        /// <summary>
        /// Adds waypoint to this agent right after previous waypoint
        /// </summary>
        /// <param name="waypoint">Waypoint that will be added to this agent</param>
        /// <param name="previousWaypoint">New waypoint will be added after previous waypoint, if null new is added as last</param>
        public virtual void AddWaypoint(ScenarioWaypoint waypoint, ScenarioWaypoint previousWaypoint)
        {
            var index = previousWaypoint == null ? waypoints.Count : waypoints.IndexOf(previousWaypoint) + 1;
            if (index > 0)
                waypoint.CopyProperties(waypoints[index - 1]);

            waypoint.IndexInAgent = index;
            AddWaypoint(waypoint);
        }

        /// <summary>
        /// Adds waypoint to this agent right after previous waypoint
        /// </summary>
        /// <param name="waypoint">Waypoint that will be added to this agent</param>
        private void AddWaypoint(ScenarioWaypoint waypoint)
        {
            var index = waypoint.IndexInAgent;
            if (index > waypoints.Count)
                index = waypoints.Count;
            if (index < 0)
                index = 0;
            waypoint.Moved += WaypointPositionChanged;
            waypoint.Removed += RemoveWaypoint;
            waypoint.Reverted += AddWaypoint;
            waypoint.DestinationSpeedChanged += WaypointOnDestinationSpeedChanged;
            waypoint.AccelerationChanged += WaypointOnAccelerationChanged;
            waypoints.Insert(index, waypoint);
            waypoint.ParentElement = ParentElement;
            var waypointsMaterial = GetWaypointsMaterial();
            if (waypointsMaterial != null)
                waypoint.waypointRenderer.material = waypointsMaterial;
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
                    PathRenderer.positionCount = waypoints.Count + WaypointsStartIndex + (EndElement == null ? 0 : 1);
                    for (var i = index; i < waypoints.Count; i++)
                    {
                        var position = LineRendererPositionOffset + waypoints[i].transform.localPosition;
                        PathRenderer.SetPosition(i + WaypointsStartIndex, position);
                    }

                    if (EndElement != null)
                        EndElementOnMoved(EndElement);
                    break;

                case WaypointsPathType.BezierSpline:
                    RecalculateBezierSpline();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            WaypointPositionChanged(waypoint);
        }

        /// <summary>
        /// Removes the waypoint from this agent
        /// </summary>
        /// <param name="waypoint">Waypoint that will be removed from this agent</param>
        public virtual void RemoveWaypoint(ScenarioWaypoint waypoint)
        {
            waypoint.Moved -= WaypointPositionChanged;
            waypoint.Removed -= RemoveWaypoint;
            waypoint.Reverted -= AddWaypoint;
            waypoint.DestinationSpeedChanged -= WaypointOnDestinationSpeedChanged;
            waypoint.AccelerationChanged -= WaypointOnAccelerationChanged;
            var index = waypoints.IndexOf(waypoint);
            waypoints.Remove(waypoint);
            for (var i = index; i < waypoints.Count; i++)
            {
                waypoints[i].IndexInAgent = i;
            }

            switch (pathType)
            {
                case WaypointsPathType.Linear:
                    PathRenderer.positionCount = waypoints.Count + WaypointsStartIndex + (EndElement == null ? 0 : 1);
                    for (var i = index; i < waypoints.Count; i++)
                    {
                        var position = LineRendererPositionOffset + waypoints[i].transform.transform.localPosition;
                        PathRenderer.SetPosition(i + 1, position);
                    }

                    if (EndElement != null)
                        EndElementOnMoved(EndElement);
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
            waypoint.IndexInAgent = index;
        }

        /// <summary>
        /// Method that updates line renderer, has to be called every time when waypoint changes the position
        /// </summary>
        /// <param name="changedElement">Waypoint that changed it's position</param>
        public void WaypointPositionChanged(ScenarioElement changedElement)
        {
            if (changedElement is ScenarioWaypoint waypoint)
                WaypointPositionChanged(waypoint);
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
                    PathRenderer.SetPosition(index + WaypointsStartIndex, position);
                    UpdateDirectionTransforms(index, index + 1);

                    // If the last waypoint is moved, rotate transform to it
                    if (index == waypoints.Count - 1 && EndElement != null)
                    {
                        EndElement.TransformToRotate.LookAt(2 * EndElement.TransformToMove.position -
                                                            waypoint.TransformToMove.position);
                    }

                    // If the first waypoint is moved, rotate transform to it
                    if (index == 0 && StartElement != null)
                    {
                        StartElement.TransformToRotate.LookAt(waypoint.TransformToMove);
                    }

                    break;

                case WaypointsPathType.BezierSpline:
                    RecalculateBezierSpline();

                    // Rotate agent to the second bezier point (first is start position)
                    var bezierPoints = bezierSpline.GetBezierWaypoints();
                    if (bezierPoints != null)
                    {
                        if (EndElement != null && bezierPoints.Count > 2)
                        {
                            EndElement.TransformToRotate.LookAt(2 * EndElement.TransformToMove.position -
                                                                (bezierPoints[bezierPoints.Count - 2].Position +
                                                                 waypointsParent.position));
                        }

                        if (StartElement != null && bezierPoints.Count > 1)
                        {
                            StartElement.TransformToRotate.LookAt(bezierPoints[1].Position +
                                                                  waypointsParent.position);
                        }
                    }

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
                        var index = i + WaypointsStartIndex;
                        var waypoint = waypoints[i];
                        if (index < 1 || PathRenderer.positionCount < 2)
                        {
                            waypoint.directionTransform.gameObject.SetActive(false);
                            continue;
                        }

                        var position = PathRenderer.GetPosition(index);
                        var previousPosition = PathRenderer.GetPosition(index - 1);
                        var directionVector = previousPosition - position;
                        waypoint.directionTransform.localPosition = directionVector / 2.0f;
                        waypoint.directionTransform.localRotation = directionVector.sqrMagnitude > 0.0f
                            ? Quaternion.LookRotation(-directionVector)
                            : Quaternion.Euler(0.0f, 0.0f, 0.0f);
                        waypoint.directionTransform.gameObject.SetActive(true);
                    }

                    break;
                case WaypointsPathType.BezierSpline:
                    var bezierKnots = bezierSpline.Knots;
                    for (var i = startIndex; i <= endIndex; i++)
                    {
                        var waypoint = waypoints[i];
                        var previousBezierPointIndex = i + WaypointsStartIndex - 1;
                        if (bezierKnots.Length < 1 || previousBezierPointIndex < 0)
                        {
                            waypoint.directionTransform.gameObject.SetActive(false);
                            continue;
                        }

                        var position = waypoint.TransformToMove.position;
                        var previousPosition =
                            bezierKnots[previousBezierPointIndex].Position + waypointsParent.position;

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
        protected void RecalculateBezierSpline()
        {
            if (waypoints.Count < 1)
            {
                bezierSpline = new BezierSpline<Waypoint>(new Waypoint[0], 0.01f);
                PathRenderer.positionCount = 0;
                return;
            }

            var waypointsStartIndex = WaypointsStartIndex;
            var knotsCount = waypoints.Count + waypointsStartIndex + (EndElement == null ? 0 : 1);
            var inputWaypoints = new Waypoint[knotsCount];

            // Set waypoints knots
            for (var i = 0; i < waypoints.Count; i++)
            {
                inputWaypoints[i + waypointsStartIndex] = GetBezierWaypoint(i);
            }

            // Set initial knots
            if (StartElement != null)
            {
                var waypoint = GetBezierWaypoint(0).GetControlPoint() as Waypoint;
                waypoint.Position = StartElement.TransformToMove.position - waypointsParent.transform.position;
                inputWaypoints[0] = waypoint;
            }

            if (EndElement != null)
            {
                var waypoint = GetBezierWaypoint(waypoints.Count - 1).GetControlPoint() as Waypoint;
                waypoint.Position = EndElement.TransformToMove.position - waypointsParent.transform.position;
                inputWaypoints[knotsCount - 1] = waypoint;
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
        /// Method invoked when a waypoint changes its speed
        /// </summary>
        /// <param name="waypoint">Scenario waypoint that has changed</param>
        /// <exception cref="ArgumentOutOfRangeException">Unknown path type</exception>
        private void WaypointOnDestinationSpeedChanged(ScenarioWaypoint waypoint)
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
        /// Method invoked when a waypoint changes its acceleration
        /// </summary>
        /// <param name="waypoint">Scenario waypoint that has changed</param>
        /// <exception cref="ArgumentOutOfRangeException">Unknown path type</exception>
        private void WaypointOnAccelerationChanged(ScenarioWaypoint waypoint)
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
    }
}