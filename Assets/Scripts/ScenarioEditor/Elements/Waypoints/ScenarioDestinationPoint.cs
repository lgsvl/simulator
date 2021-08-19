/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Waypoints
{
    using System;
    using Agents;
    using Managers;
    using UnityEngine;

    /// <summary>
    /// Editable destination point for an agent
    /// </summary>
    public class ScenarioDestinationPoint : ScenarioElement
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Transform that will be used to display the rotation
        /// </summary>
        [SerializeField]
        private Transform transformToRotate;
#pragma warning restore 0649

        /// <summary>
        /// Position offset from agent that will be applied while initializing
        /// </summary>
        private const float InitialOffset = 10.0f;

        /// <summary>
        /// Parent agent which includes this destination point
        /// </summary>
        public ScenarioAgent ParentAgent { get; private set; }
        
        /// <inheritdoc/>
        public override string ElementType { get; } = "Destination Point";

        /// <inheritdoc/>
        public override bool CanBeRemoved { get; } = false;

        /// <inheritdoc/>
        public override bool CanBeCopied { get; } = false;

        /// <inheritdoc/>
        public override bool CanBeResized { get; } = false;

        /// <inheritdoc/>
        public override Transform TransformToRotate => transformToRotate;

        /// <summary>
        /// True if this destination point is active, false otherwise
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// True if the playback path is visible, false otherwise
        /// </summary>
        public bool IsPlaybackPathVisible { get; private set; }

        /// <summary>
        /// Waypoints path used in the playback mode
        /// </summary>
        public DestinationPointWaypointsPath PlaybackPath { get; } = new DestinationPointWaypointsPath();

        /// <summary>
        /// Event invoked when the IsActive property changes value
        /// </summary>
        public event Action<ScenarioDestinationPoint> IsActiveChanged;

        /// <summary>
        /// Event invoked when the IsPlaybackPathVisible property changes value
        /// </summary>
        public event Action<ScenarioDestinationPoint> IsPlaybackPathVisibilityChanged;
        

        /// <inheritdoc/>
        public override void Dispose()
        {
            ScenarioManager.Instance.prefabsPools.ReturnInstance(gameObject);
            PlaybackPath.Deinitialize();
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioElement origin)
        {
            TransformToMove.localPosition = origin.TransformToMove.localPosition;
            var originPoint = (ScenarioDestinationPoint) origin;
            IsPlaybackPathVisible = originPoint.IsPlaybackPathVisible;
            PlaybackPath.CopyProperties(originPoint.PlaybackPath);
            SetActive(originPoint.IsActive);
            SetVisibility(false);
        }

        /// <inheritdoc/>
        public override void Selected()
        {
            base.Selected();
            SetVisibility(true);
        }

        /// <inheritdoc/>
        public override void Deselected()
        {
            base.Deselected();
            SetVisibility(false);
        }

        /// <summary>
        /// Activates or deactivates this destination point
        /// </summary>
        public void SetActive(bool active)
        {
            IsActive = active;
            IsActiveChanged?.Invoke(this);
        }

        /// <summary>
        /// Sets the playback path visible or invisible
        /// </summary>
        public void SetPlaybackPathVisible(bool visible)
        {
            IsPlaybackPathVisible = visible;
            IsPlaybackPathVisibilityChanged?.Invoke(this);
        }

        /// <summary>
        /// Makes this destination point visible or not
        /// </summary>
        /// <param name="visible">Should this destination point be visible or not</param>
        public void SetVisibility(bool visible)
        {
            gameObject.SetActive(visible);
            if (IsPlaybackPathVisible)
                PlaybackPath.SetActive(visible);
        }

        /// <summary>
        /// Attach this destination point to the agent
        /// </summary>
        /// <param name="agent">Scenario agent to which destination point will be attached</param>
        /// <param name="initializeTransform">Should this attach initialize the transform</param>
        public void AttachToAgent(ScenarioAgent agent, bool initializeTransform)
        {
            ParentAgent = agent;
            PlaybackPath.Initialize(this);
            var extension = agent.GetExtension<AgentDestinationPoint>();
            extension.SetDestinationPoint(this);
            if (!initializeTransform) return;
            transform.SetParent(agent.transform);
            var forward = agent.TransformToMove.forward;
            TransformToMove.localPosition = forward * InitialOffset;
            TransformToRotate.localRotation = Quaternion.LookRotation(forward);
            Refresh();
        }

        /// <inheritdoc/>
        public override void ForceMove(Vector3 requestedPosition)
        {
            TransformToMove.position = requestedPosition;
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
            OnMoved();
        }

        /// <inheritdoc/>
        protected override void OnMoved(bool notifyOthers = true)
        {
            base.OnMoved(notifyOthers);
            Refresh();
        }

        /// <summary>
        /// Refresh the destination point after transform changes
        /// </summary>
        public void Refresh()
        {
            
        }
    }
}