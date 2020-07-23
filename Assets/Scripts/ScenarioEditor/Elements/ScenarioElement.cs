/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
    using System;
    using Input;
    using Managers;
    using UnityEngine;

    /// <summary>
    /// Scenario element that can be placed on map and edited
    /// </summary>
    public abstract class ScenarioElement : MonoBehaviour, IDragHandler
    {
        /// <summary>
        /// Type of current drag effect
        /// </summary>
        protected enum DragType
        {
            /// <summary>
            /// Element is not dragged
            /// </summary>
            None = 0,
            
            /// <summary>
            /// Element is dragged to be moved
            /// </summary>
            Movement = 1,
            
            /// <summary>
            /// Element is dragged to be rotated
            /// </summary>
            Rotation = 2,
            
            /// <summary>
            /// Element is dragged to be resized
            /// </summary>
            Resize = 3
        }
        
        /// <summary>
        /// Rotation value in degrees that will be applied when swiping whole screen
        /// </summary>
        private const float ScreenWidthRotation = 360 * 2;
        
        /// <summary>
        /// How many times element will be scaled when swiping whole screen
        /// </summary>
        private const float ScreenWidthResizeMultiplier = 10;

        /// <summary>
        /// Cached reference to the scenario editor input manager
        /// </summary>
        protected InputManager inputManager;

        /// <summary>
        /// Cached position before drag started, applied back when drag is cancelled
        /// </summary>
        private Vector3 positionBeforeDrag;

        /// <summary>
        /// Cached rotation before rotating started, applied back when rotation is cancelled
        /// </summary>
        private Quaternion rotationBeforeRotating;

        /// <summary>
        /// Cached local scale before resizing started, applied back when resizing is cancelled
        /// </summary>
        private Vector3 scaleBeforeResizing;

        /// <summary>
        /// Viewport position value when the drag start method was called
        /// </summary>
        private Vector2 startViewportPosition;

        /// <summary>
        /// Currently handled drag type
        /// </summary>
        protected DragType currentDragType;

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
        /// Can this scenario element be removed
        /// </summary>
        public virtual bool CanBeRemoved => true;

        /// <summary>
        /// Can this scenario element be moved
        /// </summary>
        public virtual bool CanBeMoved => true;

        /// <summary>
        /// Can this scenario element be rotated
        /// </summary>
        public virtual bool CanBeRotated => true;

        /// <summary>
        /// Can this scenario element be resized
        /// </summary>
        public virtual bool CanBeResized => false;

        /// <summary>
        /// Transform that will be moved
        /// </summary>
        public virtual Transform TransformToMove => transform;

        /// <summary>
        /// Transform that will be rotated
        /// </summary>
        public virtual Transform TransformToRotate => transform;

        /// <summary>
        /// Transform that will be resized
        /// </summary>
        public virtual Transform TransformToResize => transform;

        /// <summary>
        /// Unity Start method
        /// </summary>
        protected virtual void Start()
        {
            inputManager = ScenarioManager.Instance.inputManager;
        }

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
        /// Method called to entirely remove element from the scenario
        /// </summary>
        public abstract void Remove();

        /// <summary>
        /// Repositions the scenario element on the map including the element's restrictions
        /// </summary>
        /// <param name="requestedPosition"></param>
        public virtual void Reposition(Vector3 requestedPosition)
        {
            transform.position = requestedPosition;
        }

        /// <summary>
        /// Starts moving the element with drag motion
        /// </summary>
        /// <exception cref="ArgumentException">Drag movement called for ScenarioElement which does not support moving.</exception>
        public void StartDragMovement()
        {
            if (!CanBeMoved)
                throw new ArgumentException("Drag movement called for ScenarioElement which does not support moving.");
            currentDragType = DragType.Movement;
            ScenarioManager.Instance.inputManager.StartDraggingElement(this, true);
        }

        /// <summary>
        /// Starts rotating the element with drag motion
        /// </summary>
        /// <exception cref="ArgumentException">Drag rotation called for ScenarioElement which does not support rotating.</exception>
        public void StartDragRotation()
        {
            if (!CanBeMoved)
                throw new ArgumentException("Drag rotation called for ScenarioElement which does not support rotating.");
            currentDragType = DragType.Rotation;
            ScenarioManager.Instance.inputManager.StartDraggingElement(this, true);
        }

        /// <summary>
        /// Starts resizing the element with drag motion
        /// </summary>
        /// <exception cref="ArgumentException">Drag resize called for ScenarioElement which does not support resizing.</exception>
        public void StartDragResize()
        {
            if (!CanBeMoved)
                throw new ArgumentException("Drag resize called for ScenarioElement which does not support resizing.");
            currentDragType = DragType.Resize;
            ScenarioManager.Instance.inputManager.StartDraggingElement(this, true);
        }

        /// <inheritdoc/>
        void IDragHandler.DragStarted()
        {
            switch (currentDragType)
            {
                case DragType.None:
                    break;
                case DragType.Movement:
                    positionBeforeDrag = TransformToMove.position;
                    break;
                case DragType.Rotation:
                    startViewportPosition = inputManager.MouseViewportPosition;
                    rotationBeforeRotating = TransformToRotate.localRotation;
                    break;
                case DragType.Resize:
                    startViewportPosition = inputManager.MouseViewportPosition;
                    scaleBeforeResizing = TransformToResize.localScale;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc/>
        void IDragHandler.DragMoved()
        {
            switch (currentDragType)
            {
                case DragType.None:
                    break;
                case DragType.Movement:
                    Reposition(inputManager.MouseRaycastPosition);
                    OnMoved();
                    break;
                case DragType.Rotation:
                    var rotationValue = (inputManager.MouseViewportPosition.x - startViewportPosition.x) * ScreenWidthRotation;
                    TransformToRotate.localRotation = rotationBeforeRotating * Quaternion.Euler(0.0f, rotationValue, 0.0f);
                    OnRotated();
                    break;
                case DragType.Resize:
                    float scaleValue;
                    if (inputManager.MouseViewportPosition.x >= startViewportPosition.x)
                        scaleValue = 1.0f + (inputManager.MouseViewportPosition.x - startViewportPosition.x) *
                            ScreenWidthResizeMultiplier;
                    else
                        scaleValue = (1.0f - (startViewportPosition.x - inputManager.MouseViewportPosition.x)/startViewportPosition.x);
                    var minimalScaleFraction = 0.01f;
                    if (scaleValue >= 0.0f && scaleValue < minimalScaleFraction)
                        scaleValue = minimalScaleFraction;
                    TransformToResize.localScale = scaleBeforeResizing * scaleValue;
                    OnResized();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc/>
        void IDragHandler.DragFinished()
        {
            //Apply current mouse position
            (this as IDragHandler).DragMoved();

            currentDragType = DragType.None;
        }

        /// <inheritdoc/>
        void IDragHandler.DragCancelled()
        {
            switch (currentDragType)
            {
                case DragType.None:
                    break;
                case DragType.Movement:
                    TransformToMove.position = positionBeforeDrag;
                    OnMoved();
                    break;
                case DragType.Rotation:
                    TransformToRotate.localRotation = rotationBeforeRotating;
                    OnRotated();
                    break;
                case DragType.Resize:
                    TransformToResize.localScale = scaleBeforeResizing;
                    OnResized();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            currentDragType = DragType.None;
        }

        /// <summary>
        /// Method called every time position is updated while dragging
        /// </summary>
        protected virtual void OnMoved()
        {
        }

        /// <summary>
        /// Method called every time rotation is updated while rotating
        /// </summary>
        protected virtual void OnRotated()
        {
        }

        /// <summary>
        /// Method called every time scale is updated while resizing
        /// </summary>
        protected virtual void OnResized()
        {
        }
    }
}