/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents.Triggers
{
    using System;
    using Elements;
    using Elements.Triggers;
    using Managers;
    using UnityEngine;
    using UnityEngine.Rendering;

    /// <summary>
    /// Object that represents the activation zone of the waiting point effector in the scenario
    /// </summary>
    public class WaitingPointZone : ScenarioEffectorObject
    {
        /// <summary>
        /// The position offset that will be applied to the line renderer
        /// </summary>
        private static Vector3 lineRendererPositionOffset = new Vector3(0.0f, 0.1f, 0.0f);

        /// <summary>
        /// Line renderer for displaying the connection between waypoint and activation zone
        /// </summary>
        private LineRenderer pathRenderer;

        /// <summary>
        /// Cached reference to the linked effector
        /// </summary>
        private WaitingPointEffector waitingPointEffector;

        /// <inheritdoc/>
        public override string ElementType { get; } = "Waiting Point Zone";

        /// <inheritdoc/>
        public override bool CanBeRotated => false;

        /// <inheritdoc/>
        public override bool CanBeResized => true;

        /// <summary>
        /// Line renderer for displaying the connection between waypoint and activation zone
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
                pathRenderer.material = ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>().TriggerPathMaterial;
                pathRenderer.useWorldSpace = false;
                pathRenderer.positionCount = 2;
                pathRenderer.textureMode = LineTextureMode.Tile;
                pathRenderer.shadowCastingMode = ShadowCastingMode.Off;
                pathRenderer.sortingLayerName = "Ignore Raycast";
                pathRenderer.widthMultiplier = 0.1f;
                pathRenderer.SetPosition(0, lineRendererPositionOffset);
                trigger.OnModelChanged();
                return pathRenderer;
            }
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioElement origin)
        {
            transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            Refresh();
        }

        /// <inheritdoc/>
        public override void Setup(ScenarioTrigger trigger, TriggerEffector effector)
        {
            base.Setup(trigger, effector);
            waitingPointEffector = effector as WaitingPointEffector;
            if (waitingPointEffector == null)
                throw new ArgumentException(
                    $"{GetType().Name} received effector of invalid type {effector.GetType().Name}.");
            Refresh();
        }

        /// <inheritdoc/>
        protected override void OnMoved(bool notifyOthers = true)
        {
            base.OnMoved(notifyOthers);
            Refresh();
            // Update serialized data as it can be used in the VSE
            OnBeforeSerialize();
        }

        protected override void OnResized(bool notifyOthers = true)
        {
            base.OnResized(notifyOthers);
            if (waitingPointEffector!=null)
                waitingPointEffector.PointRadius = transform.localScale.x;
            Refresh();
            // Update serialized data as it can be used in the VSE
            OnBeforeSerialize();
        }

        /// <inheritdoc/>
        public override void Refresh()
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

        /// <inheritdoc/>
        public override void OnBeforeSerialize()
        {
            waitingPointEffector.ActivatorPoint = transform.position;
            waitingPointEffector.PointRadius = transform.localScale.x;
        }
    }
}