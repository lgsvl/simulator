/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
    using Input;
    using Managers;
    using UnityEngine;

    /// <summary>
    /// Scenario element that can be placed on map and edited
    /// </summary>
    public abstract class ScenarioElement : MonoBehaviour, IDragHandler, IRotateHandler
    {
        /// <summary>
        /// Rotation value in degrees that will be applied when swiping whole screen
        /// </summary>
        private const float ScreenWidthRotation = 360 * 2;

        /// <summary>
        /// Cached position before drag started, applied back when drag is cancelled
        /// </summary>
        private Vector3 positionBeforeDrag;

        /// <summary>
        /// Cached rotation before rotating started, applied back when rotation is cancelled
        /// </summary>
        private Quaternion rotationBeforeRotating;

        /// <summary>
        /// Viewport position that was passed in rotation start method
        /// </summary>
        private Vector2 rotationStartViewportPosition;

        /// <summary>
        /// Uid of this scenario element
        /// </summary>
        private string uid;

        /// <summary>
        /// Uid of this scenario element
        /// </summary>
        public virtual string Uid
        {
            get => uid ?? (uid = System.Guid.NewGuid().ToString());
            set => uid = value;
        }

        /// <summary>
        /// Transform that will be dragged
        /// </summary>
        public virtual Transform TransformToDrag => transform;

        /// <summary>
        /// Transform that will be rotated
        /// </summary>
        public virtual Transform TransformToRotate => transform;

        /// <summary>
        /// Unity OnEnable method
        /// </summary>
        protected virtual void OnEnable()
        {
            ScenarioManager.Instance.NewElementActivated(this);
        }

        /// <summary>
        /// Method called when the element is selected by the user
        /// </summary>
        public virtual void Selected()
        {
            
        }

        /// <summary>
        /// Repositions the scenario element on the map including the element's restrictions
        /// </summary>
        /// <param name="requestedPosition"></param>
        public virtual void Reposition(Vector3 requestedPosition)
        {
            transform.position = requestedPosition;
        }

        /// <inheritdoc/>
        void IDragHandler.DragStarted(Vector3 dragPosition)
        {
            positionBeforeDrag = TransformToDrag.position;
        }

        /// <inheritdoc/>
        void IDragHandler.DragMoved(Vector3 dragPosition)
        {
            TransformToDrag.position = dragPosition;
            OnDragged();
        }

        /// <inheritdoc/>
        void IDragHandler.DragFinished(Vector3 dragPosition)
        {
            TransformToDrag.position = dragPosition;
            OnDragged();
        }

        /// <inheritdoc/>
        void IDragHandler.DragCancelled(Vector3 dragPosition)
        {
            TransformToDrag.position = positionBeforeDrag;
            OnDragged();
        }

        /// <inheritdoc/>
        void IRotateHandler.RotationStarted(Vector2 viewportPosition)
        {
            rotationStartViewportPosition = viewportPosition;
            rotationBeforeRotating = TransformToRotate.localRotation;
        }

        /// <inheritdoc/>
        void IRotateHandler.RotationChanged(Vector2 viewportPosition)
        {
            var rotationValue = (viewportPosition.x - rotationStartViewportPosition.x) * ScreenWidthRotation;
            TransformToRotate.localRotation = rotationBeforeRotating * Quaternion.Euler(0.0f, rotationValue, 0.0f);
            OnRotated();
        }

        /// <inheritdoc/>
        void IRotateHandler.RotationFinished(Vector2 viewportPosition)
        {
            var rotationValue = (viewportPosition.x - rotationStartViewportPosition.x) * ScreenWidthRotation;
            TransformToRotate.localRotation = rotationBeforeRotating * Quaternion.Euler(0.0f, rotationValue, 0.0f);
            OnRotated();
        }

        /// <inheritdoc/>
        void IRotateHandler.RotationCancelled(Vector2 viewportPosition)
        {
            TransformToRotate.localRotation = rotationBeforeRotating;
            OnRotated();
        }

        /// <summary>
        /// Method called every time position is updated while dragging
        /// </summary>
        protected virtual void OnDragged()
        {
        }

        /// <summary>
        /// Method called every time rotation is updated while rotating
        /// </summary>
        protected virtual void OnRotated()
        {
        }
    }
}