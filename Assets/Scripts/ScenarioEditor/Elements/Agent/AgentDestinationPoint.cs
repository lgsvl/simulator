/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Agents
{
    using SimpleJSON;
    using UnityEngine;
    using Waypoints;

    /// <summary>
    /// Scenario agent extension that handles the destination point
    /// </summary>
    public class AgentDestinationPoint : IScenarioElementExtension
    {
        /// <summary>
        /// Scenario agent that this object extends
        /// </summary>
        private ScenarioAgent ParentAgent { get; set; }
        
        /// <summary>
        /// Point that indicates the destination for this agent
        /// </summary>
        public ScenarioDestinationPoint DestinationPoint { get; private set; }

        /// <inheritdoc/>
        public void Initialize(ScenarioElement parentElement)
        {
            ParentAgent = (ScenarioAgent) parentElement;
        }

        /// <inheritdoc/>
        public void Deinitialize()
        {
            SetDestinationPoint(null);
            ParentAgent = null;
        }

        /// <summary>
        /// Sets the destination point
        /// </summary>
        /// <param name="destinationPoint">Destination point to be set</param>
        public void SetDestinationPoint(ScenarioDestinationPoint destinationPoint)
        {
            DestinationPoint = destinationPoint;
        }

        /// <inheritdoc/>
        public void SerializeToJson(JSONNode elementNode)
        {
            if (DestinationPoint == null || !DestinationPoint.IsActive) return;
            var destinationPoint = new JSONObject();
            elementNode.Add("destinationPoint", destinationPoint);
            var destinationPosition =
                new JSONObject().WriteVector3(DestinationPoint.TransformToMove.position);
            destinationPoint.Add("position", destinationPosition);
            var destinationRotation =
                new JSONObject().WriteVector3(DestinationPoint.TransformToRotate.rotation
                    .eulerAngles);
            destinationPoint.Add("rotation", destinationRotation);
            destinationPoint.Add("isPlaybackPathVisible", DestinationPoint.IsPlaybackPathVisible);
            DestinationPoint.PlaybackPath.SerializeToJson(destinationPoint);
        }

        /// <inheritdoc/>
        public void DeserializeFromJson(JSONNode elementNode)
        {
            if (DestinationPoint == null || !elementNode.HasKey("destinationPoint")) return;
            var destinationPoint = elementNode["destinationPoint"];
            DestinationPoint.TransformToMove.position =
                destinationPoint["position"].ReadVector3();
            DestinationPoint.TransformToRotate.rotation =
                Quaternion.Euler(destinationPoint["rotation"].ReadVector3());
            DestinationPoint.SetPlaybackPathVisible(destinationPoint["isPlaybackPathVisible"]);
            DestinationPoint.PlaybackPath.DeserializeFromJson(destinationPoint);
            DestinationPoint.SetActive(true);
            DestinationPoint.SetVisibility(false);
            DestinationPoint.Refresh();
        }

        /// <inheritdoc/>
        public void CopyProperties(ScenarioElement originElement)
        {
            var originAgent = (ScenarioAgent) originElement;
            var origin = originAgent.GetExtension<AgentDestinationPoint>();
            if (origin == null) return;
            var destinationPoint = ParentAgent.GetComponentInChildren<ScenarioDestinationPoint>(true);
            if (destinationPoint != null)
                destinationPoint.AttachToAgent(ParentAgent, false);
        }
    }
}
