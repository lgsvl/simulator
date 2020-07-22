/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents.Triggers
{
    using Elements;
    using Managers;
    using UnityEngine;

    /// <summary>
    /// Object that represents the activation zone of the waiting point effector in the scenario
    /// </summary>
    public class WaitingPointZone : ScenarioElement
    {
        /// <summary>
        /// The position offset that will be applied to the line renderer of waypoints
        /// </summary>
        private static Vector3 lineRendererPositionOffset = new Vector3(0.0f, 0.5f, 0.0f);

        /// <summary>
        /// Line renderer for displaying the connection between waypoint and activation zone
        /// </summary>
        private LineRenderer lineRenderer;
        
        /// <summary>
        /// Parent effector of this zone
        /// </summary>
        private WaitingPointEffector parentEffector;

        /// <summary>
        /// Setups the zone object with the data
        /// </summary>
        /// <param name="parentEffector">Parent effector of this zone</param>
        public void Setup(WaitingPointEffector parentEffector)
        {
            this.parentEffector = parentEffector;
            lineRenderer = gameObject.GetComponent<LineRenderer>();
            if (lineRenderer==null)
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = ScenarioManager.Instance.waypointsManager.triggerPathMaterial;
            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = 2;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.SetPosition(0, Vector3.zero);
            var thisTransform = transform;
            var localPos = thisTransform.localPosition;
            var localScale = thisTransform.localScale;
            var position = new Vector3(-localPos.x/localScale.x, -localPos.y/localScale.y, -localPos.z/localScale.z);
            lineRenderer.SetPosition(1, position);
            lineRenderer.sortingLayerName = "Ignore Raycast";
            lineRenderer.widthMultiplier = 0.4f;
        }

        /// <inheritdoc/>
        protected override void OnDragged()
        {
            base.OnDragged();
            var thisTransform = transform;
            parentEffector.ActivatorPoint = thisTransform.position;
            var localPos = thisTransform.localPosition;
            var localScale = thisTransform.localScale;
            var position = new Vector3(-localPos.x/localScale.x, -localPos.y/localScale.y, -localPos.z/localScale.z);
            lineRenderer.SetPosition(1, position);
        }
    }
}