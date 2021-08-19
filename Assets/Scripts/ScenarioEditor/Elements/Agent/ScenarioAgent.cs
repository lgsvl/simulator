/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Agents
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Elements;
    using Managers;
    using ScenarioEditor.Agents;
    using SimpleJSON;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using Waypoints;

    /// <inheritdoc cref="Simulator.ScenarioEditor.Elements.ScenarioElement" />
    /// <remarks>
    /// Scenario agent representation
    /// </remarks>
    public class ScenarioAgent : ScenarioElementWithVariant, ITriggerAgent
    {
        /// <summary>
        /// This agent extensions for additional features
        /// </summary>
        public Dictionary<Type, IScenarioElementExtension> Extensions { get; } =
            new Dictionary<Type, IScenarioElementExtension>();

        /// <inheritdoc/>
        public override string ElementType => Variant == null ? "Agent" : Variant.Name;

        /// <inheritdoc/>
        public override bool CanBeCopied => true;

        /// <inheritdoc/>
        public override Transform TransformToRotate => modelInstance.transform;

        /// <inheritdoc/>
        public override Transform TransformForPlayback => modelInstance.transform;
        
        /// <inheritdoc/>
        public Transform AgentTransform => TransformForPlayback;
        
        /// <inheritdoc/>
        public float MovementSpeed { get; set; }
        
        /// <inheritdoc/>
        public Vector3 Acceleration { get; set; }

        /// <summary>
        /// Parent source of this scenario agent
        /// </summary>
        public ScenarioAgentSource Source => source as ScenarioAgentSource;

        /// <summary>
        /// This agent variant
        /// </summary>
        public AgentVariant Variant => variant as AgentVariant;

        /// <summary>
        /// Type of this agent
        /// </summary>
        public AgentType Type => Source.AgentType;

        /// <summary>
        /// Event invoked when an extension is added to the agent
        /// </summary>
        public event Action<IScenarioElementExtension> ExtensionAdded;

        /// <summary>
        /// Event invoked when an extension is removed from the agent
        /// </summary>
        public event Action<IScenarioElementExtension> ExtensionRemoved;

        /// <inheritdoc/>
        public override void Setup(ScenarioElementSource source, SourceVariant variant)
        {
            base.Setup(source, variant);
            ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>().RegisterAgent(this);
        }

        /// <summary>
        /// Gets an agent's extension
        /// </summary>
        /// <typeparam name="T">Extension type to get</typeparam>
        /// <returns>Agent's extension of the given type, null if extension is not available</returns>
        public T GetExtension<T>() where T : class, IScenarioElementExtension
        {
            var type = typeof(T);
            return GetExtension(type) as T;
        }

        /// <summary>
        /// Gets an agent's extension
        /// </summary>
        /// <param name="type">Extension type to get</param>
        /// <returns>Agent's extension of the given type, null if extension is not available</returns>
        public IScenarioElementExtension GetExtension(Type type)
        {
            if (!Extensions.TryGetValue(type, out var extension))
                return null;
            return extension;
        }

        /// <summary>
        /// Gets an agent's extension or adds a new one and returns it
        /// </summary>
        /// <param name="defaultValue">Value that will be added if there is no extension available</param>
        /// <typeparam name="T">Extension type to get</typeparam>
        /// <returns>Agent's extension of the given type</returns>
        public T GetOrAddExtension<T>(T defaultValue = null) where T : class, IScenarioElementExtension
        {
            var type = typeof(T);
            return GetOrAddExtension(type, defaultValue) as T;
        }
        
        /// <summary>
        /// Gets an agent's extension or adds a new one and returns it
        /// </summary>
        /// <param name="type">Extension type to get</param>
        /// <param name="defaultValue">Value that will be added if there is no extension available</param>
        /// <returns>Agent's extension of the given type</returns>
        public IScenarioElementExtension GetOrAddExtension(Type type, IScenarioElementExtension defaultValue = null)
        {
            if (Extensions.TryGetValue(type, out var extension))
                return extension;
            if (defaultValue == null)
                defaultValue = Activator.CreateInstance(type) as IScenarioElementExtension;
            if (defaultValue == null)
                return null;
            Extensions.Add(type, defaultValue);
            defaultValue.Initialize(this);
            ExtensionAdded?.Invoke(defaultValue);
            return defaultValue;
        }

        /// <summary>
        /// Removes extension of given type if it is available
        /// </summary>
        /// <typeparam name="T">Extension type to remove</typeparam>
        /// <returns>Removed extension</returns>
        public T RemoveExtension<T>() where T : class, IScenarioElementExtension
        {
            var type = typeof(T);
            return RemoveExtension(type) as T;
        }

        /// <summary>
        /// Removes extension of given type if it is available
        /// </summary>
        /// <param name="type">Extension type to remove</param>
        /// <returns>Removed extension</returns>
        public IScenarioElementExtension RemoveExtension(Type type)
        {
            if (!Extensions.TryGetValue(type, out var extension))
                return null;
            extension.Deinitialize();
            Extensions.Remove(type);
            ExtensionRemoved?.Invoke(extension);
            return extension;
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
        protected override void RegisterUndoChangeVariant()
        {
            var undoRecords = new List<UndoRecord>();
            undoRecords.Add(new UndoChangeVariant(this, variant));
            var colorExtension = GetExtension<AgentColorExtension>();
            if (colorExtension!=null)
                undoRecords.Add(new UndoChangeColor(colorExtension, colorExtension.AgentColor));
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new ComplexUndo(undoRecords));
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            var extensionsList = Extensions.Values.ToList();
            foreach (var extension in extensionsList) RemoveExtension(extension.GetType());

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
            foreach (var originExtension in originAgent.Extensions)
            {
                var extension = GetOrAddExtension(originExtension.Value.GetType());
                extension.CopyProperties(originAgent);
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
        /// Gets the waypoints path set to this agent
        /// </summary>
        /// <returns>Waypoints path set to this agent</returns>
        public WaypointsPath GetWaypointsPath()
        {
            switch (Type)
            {
                case AgentType.Unknown:
                    break;
                case AgentType.Ego:
                    return GetExtension<AgentDestinationPoint>().DestinationPoint.PlaybackPath;
                case AgentType.Npc:
                case AgentType.Pedestrian:
                    var waypointsExtension = GetExtension<AgentWaypointsPath>();
                    return waypointsExtension;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return null;
        }
    }
}