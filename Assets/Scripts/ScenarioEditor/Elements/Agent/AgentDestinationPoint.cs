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

    /// <summary>
    /// Scenario agent extension that handles the destination point
    /// </summary>
    public class AgentDestinationPoint : ScenarioAgentExtension
    {
        /// <summary>
        /// Point that indicates the destination for this agent
        /// </summary>
        public ScenarioDestinationPoint DestinationPoint { get; set; }
        
        /// <inheritdoc/>
        public override void SerializeToJson(JSONNode agentNode)
        {
            if (DestinationPoint == null || !DestinationPoint.IsActive) return;
            var destinationPoint = new JSONObject();
            agentNode.Add("destinationPoint", destinationPoint);
            var destinationPosition =
                new JSONObject().WriteVector3(DestinationPoint.TransformToMove.position);
            destinationPoint.Add("position", destinationPosition);
            var destinationRotation =
                new JSONObject().WriteVector3(DestinationPoint.TransformToRotate.rotation
                    .eulerAngles);
            destinationPoint.Add("rotation", destinationRotation);
        }

        /// <inheritdoc/>
        public override void DeserializeFromJson(JSONNode agentNode)
        {
            if (DestinationPoint == null || !agentNode.HasKey("destinationPoint")) return;
            var destinationPoint = agentNode["destinationPoint"];
            DestinationPoint.TransformToMove.position =
                destinationPoint["position"].ReadVector3();
            DestinationPoint.TransformToRotate.rotation =
                Quaternion.Euler(destinationPoint["rotation"].ReadVector3());
            DestinationPoint.SetActive(true);
            DestinationPoint.SetVisibility(false);
            DestinationPoint.Refresh();
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioAgent agent)
        {
            var origin = agent.GetExtension<AgentDestinationPoint>();
            if (origin == null) return;
            var destinationPoint = ParentAgent.GetComponentInChildren<ScenarioDestinationPoint>(true);
            if (destinationPoint != null)
                destinationPoint.AttachToAgent(ParentAgent, false);
        }
    }
}
