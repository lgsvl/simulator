/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Agent
{
    using Managers;
    using UnityEngine;
    using Utilities;

    /// <summary>
    /// Editable destination point for an agent
    /// </summary>
    public class ScenarioDestinationPoint : ScenarioElement
    {
        /// <summary>
        /// Position offset from agent that will be applied while initializing
        /// </summary>
        private const float InitialOffset = 10.0f;
        
        /// <summary>
        /// The position offset that will be applied to the line renderer
        /// </summary>
        private static Vector3 lineRendererPositionOffset = new Vector3(0.0f, 0.2f, 0.0f);

        /// <summary>
        /// Line renderer for displaying the connection between agent and destination point
        /// </summary>
        private LineRenderer pathRenderer;

        /// <summary>
        /// Parent agent which includes this destination point
        /// </summary>
        public ScenarioAgent ParentAgent { get; private set; }

        /// <inheritdoc/>
        public override bool CanBeRemoved { get; } = false;
        
        /// <inheritdoc/>
        public override bool CanBeCopied { get; } = false;
        
        /// <inheritdoc/>
        public override bool CanBeResized { get; } = false;
        
        /// <inheritdoc/>
        public override bool CanBeRotated { get; } = false;

        /// <summary>
        /// Line renderer for displaying the connection between agent and destination point
        /// </summary>
        public LineRenderer PathRenderer
        {
            get
            {
                if (pathRenderer != null)
                    return pathRenderer;
                pathRenderer = gameObject.GetComponent<LineRenderer>();
                if (pathRenderer != null)
                    return pathRenderer;
                pathRenderer = gameObject.AddComponent<LineRenderer>();
                pathRenderer.material = ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>().destinationPathMaterial;
                pathRenderer.useWorldSpace = false;
                pathRenderer.positionCount = 2;
                pathRenderer.textureMode = LineTextureMode.Tile;
                pathRenderer.sortingLayerName = "Ignore Raycast";
                pathRenderer.widthMultiplier = 0.4f;
                pathRenderer.SetPosition(0, lineRendererPositionOffset);
                return pathRenderer;
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            ScenarioManager.Instance.GetExtension<PrefabsPools>().ReturnInstance(gameObject);
        }

        /// <summary>
        /// Attach this destination point to the agent
        /// </summary>
        /// <param name="agent">Scenario agent to which destination point will be attached</param>
        public void AttachToAgent(ScenarioAgent agent)
        {
            ParentAgent = agent;
            agent.DestinationPoint = this;
            var thisTransform = transform;
            thisTransform.SetParent(agent.transform);
            thisTransform.localPosition = agent.transform.forward * InitialOffset;
            thisTransform.localRotation = Quaternion.Euler(Vector3.zero);
            Refresh();
        }
        
        /// <inheritdoc/>
        protected override void OnMoved()
        {
            base.OnMoved();
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            switch (ParentAgent.Type)
            {
                case AgentType.Ego:
                case AgentType.Npc:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic, transform);
                    break;
                case AgentType.Pedestrian:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian, transform);
                    break;
            }

            Refresh();
        }

        /// <summary>
        /// Refresh the 
        /// </summary>
        public void Refresh()
        {
            var thisTransform = transform;
            var localPos = thisTransform.localPosition;
            var localScale = thisTransform.localScale;
            var position = localPos;
            position.x /= -localScale.x;
            position.y = lineRendererPositionOffset.y / localScale.y;
            position.z /= -localScale.z;
            PathRenderer.SetPosition(1, position);
        }
    }
}