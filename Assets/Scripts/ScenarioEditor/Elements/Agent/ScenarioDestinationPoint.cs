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
        /// The position offset that will be applied to the line renderer
        /// </summary>
        private static Vector3 lineRendererPositionOffset = new Vector3(0.0f, 0.1f, 0.0f);

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
        public override Transform TransformToRotate => transformToRotate;

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
                pathRenderer.material = ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>()
                    .destinationPathMaterial;
                pathRenderer.useWorldSpace = false;
                pathRenderer.positionCount = 2;
                pathRenderer.textureMode = LineTextureMode.Tile;
                pathRenderer.sortingLayerName = "Ignore Raycast";
                PathRenderer.widthMultiplier = 0.1f;
                PathRenderer.generateLightingData = false;
                pathRenderer.SetPosition(0, lineRendererPositionOffset);
                return pathRenderer;
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            ScenarioManager.Instance.prefabsPools.ReturnInstance(gameObject);
        }

        /// <summary>
        /// Attach this destination point to the agent
        /// </summary>
        /// <param name="agent">Scenario agent to which destination point will be attached</param>
        public void AttachToAgent(ScenarioAgent agent)
        {
            ParentAgent = agent;
            agent.DestinationPoint = this;
            transform.SetParent(agent.transform);
            var forward = agent.TransformToMove.forward;
            TransformToMove.localPosition = forward * InitialOffset;
            TransformToRotate.localRotation = Quaternion.LookRotation(forward);
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
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Traffic, TransformToMove);
                    break;
                case AgentType.Pedestrian:
                    mapManager.LaneSnapping.SnapToLane(LaneSnappingHandler.LaneType.Pedestrian, TransformToMove);
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