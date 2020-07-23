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
        private static Vector3 lineRendererPositionOffset = new Vector3(0.0f, 0.2f, 0.0f);

        /// <summary>
        /// Line renderer for displaying the connection between waypoint and activation zone
        /// </summary>
        private LineRenderer lineRenderer;
        
        /// <summary>
        /// Parent effector of this zone
        /// </summary>
        private WaitingPointEffector parentEffector;

        /// <inheritdoc/>
        public override bool CanBeRemoved => false;

        /// <inheritdoc/>
        public override bool CanBeRotated => false;

        /// <inheritdoc/>
        public override bool CanBeResized => true;

        /// <inheritdoc/>
        public override void Remove()
        {
            throw new System.NotImplementedException();
        }

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
            lineRenderer.SetPosition(0, lineRendererPositionOffset);
            var thisTransform = transform;
            var localPos = thisTransform.localPosition;
            var localScale = thisTransform.localScale;
            var position = localPos;
            position.x /= -localScale.x;
            position.y = lineRendererPositionOffset.y/localScale.y;
            position.z /= -localScale.z;
            lineRenderer.SetPosition(1, position);
            lineRenderer.sortingLayerName = "Ignore Raycast";
            lineRenderer.widthMultiplier = 0.4f;
        }

        /// <inheritdoc/>
        protected override void OnMoved()
        {
            base.OnMoved();
            parentEffector.ActivatorPoint = transform.position;
            Refresh();
        }

        protected override void OnResized()
        {
            base.OnResized();
            parentEffector.PointRadius = transform.localScale.x;
            Refresh();
        }

        /// <summary>
        /// Refresh the zone visualization with current state
        /// </summary>
        public void Refresh()
        {
            var thisTransform = transform;
            var localPos = thisTransform.localPosition;
            var localScale = thisTransform.localScale;
            var position = localPos;
            position.x /= -localScale.x;
            position.y = lineRendererPositionOffset.y/localScale.y;
            position.z /= -localScale.z;
            lineRenderer.SetPosition(1, position);
        }
    }
}