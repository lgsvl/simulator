/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
    using System;
    using System.Collections.Generic;
    using Input;
    using Managers;
    using Undo;
    using Undo.Records;
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
        /// Scenario elements in all the parent game objects
        /// </summary>
        private readonly List<ScenarioElement> parentElements = new List<ScenarioElement>();
        
        /// <summary>
        /// Scenario elements in the same game object
        /// </summary>
        private readonly List<ScenarioElement> siblingElements = new List<ScenarioElement>();
        
        /// <summary>
        /// Scenario elements in all the child game objects
        /// </summary>
        private readonly List<ScenarioElement> childElements = new List<ScenarioElement>();

        /// <summary>
        /// Uid of this scenario element
        /// </summary>
        public virtual string Uid
        {
            get => uid ??= Guid.NewGuid().ToString();
            set => uid = value;
        }
        
        /// <summary>
        /// Checks if this scenario element can be edited on the map
        /// </summary>
        public virtual bool IsEditableOnMap => true;

        /// <summary>
        /// Name of this scenario element type
        /// </summary>
        public abstract string ElementType { get; }

        /// <summary>
        /// Can this scenario element be copied
        /// </summary>
        public virtual bool CanBeCopied => false;

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
        /// Transform that will be animated in the playback
        /// </summary>
        public virtual Transform TransformForPlayback => transform;

        /// <summary>
        /// Event invoked when the scenario element is moved
        /// </summary>
        public event Action<ScenarioElement> Moved;

        /// <summary>
        /// Event invoked when the scenario element is rotated
        /// </summary>
        public event Action<ScenarioElement> Rotated;

        /// <summary>
        /// Event invoked when the scenario element is resized
        /// </summary>
        public event Action<ScenarioElement> Resized;

        /// <summary>
        /// Event invoked when the scenario element's model changes
        /// </summary>
        public event Action<ScenarioElement> ModelChanged;
        

        /// <summary>
        /// Unity Start method
        /// </summary>
        protected virtual void Awake()
        {
            if (inputManager == null)
                inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
        }

        /// <summary>
        /// Unity OnEnable method
        /// </summary>
        protected virtual void OnEnable()
        {
            var siblings = GetComponents<ScenarioElement>();
            foreach (var sibling in siblings)
            {
                if (sibling==this || siblingElements.Contains(sibling))
                    continue;
                sibling.siblingElements.Add(this);
                this.siblingElements.Add(sibling);
            }
            var parents = GetComponentsInParent<ScenarioElement>();
            foreach (var parent in parents)
            {
                if (parent==this || siblingElements.Contains(parent))
                    continue;
                parent.childElements.Add(this);
                this.parentElements.Add(parent);
            }
            var children = GetComponentsInChildren<ScenarioElement>();
            foreach (var child in children)
            {
                if (child == this || siblingElements.Contains(child))
                    continue;
                child.parentElements.Add(this);
                this.childElements.Add(child);
            }
            ScenarioManager.Instance.ReportActivatedElement(this);
            ModelChanged?.Invoke(this);
        }

        /// <summary>
        /// Unity OnDisable method
        /// </summary>
        protected virtual void OnDisable()
        {
            foreach (var sibling in siblingElements) 
                sibling.siblingElements.Remove(this);
            siblingElements.Clear();
            foreach (var parent in parentElements)
                parent.childElements.Remove(this);
            parentElements.Clear();
            foreach (var child in childElements)
                child.parentElements.Remove(this);
            childElements.Clear();
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.ReportDeactivatedElement(this);
        }

        /// <summary>
        /// Method called when the element is selected by the user
        /// </summary>
        public virtual void Selected()
        {
        }

        /// <summary>
        /// Method called when the element was deselected by another element
        /// </summary>
        public virtual void Deselected()
        {
        }

        /// <summary>
        /// Removes element from the map, but holds it for the undo
        /// </summary>
        public virtual void RemoveFromMap()
        {
            if (ScenarioManager.Instance.SelectedElement == this)
                ScenarioManager.Instance.SelectedElement = null;
        }

        /// <summary>
        /// Method called to entirely remove element from the scenario
        /// </summary>
        public virtual void UndoRemove()
        {
        }

        /// <summary>
        /// Entirely removes element from the scene
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Method called after this element is instantiated using copied element
        /// </summary>
        /// <param name="origin">Origin element from which copy was created</param>
        public abstract void CopyProperties(ScenarioElement origin);

        /// <summary>
        /// Moves this element along Y-axis so it stays on the ground
        /// </summary>
        public void RepositionOnGround()
        {
            if (inputManager == null)
                inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            var pos = TransformToMove.position;
            pos.y += 100.0f;
            var ray = new Ray(pos, Vector3.down);
            var hits = inputManager.RaycastAll(ray);
            var furthestHit = inputManager.GetClosestHit(hits, hits.Length, true, true);
            if (!furthestHit.HasValue)
                return;
            ForceMove(furthestHit.Value.point);
        }

        /// <summary>
        /// Repositions the scenario element on the map including the element's restrictions
        /// </summary>
        /// <param name="requestedPosition">Position that will be applied</param>
        public virtual void ForceMove(Vector3 requestedPosition)
        {
            TransformToMove.position = requestedPosition;
            OnMoved();
        }

        /// <summary>
        /// Rotates the scenario element on the map
        /// </summary>
        /// <param name="requestedRotation">Rotation that will be applied</param>
        public virtual void ForceRotate(Quaternion requestedRotation)
        {
            TransformToRotate.rotation = requestedRotation;
            OnRotated();
        }

        /// <summary>
        /// Resized the scenario element on the map
        /// </summary>
        /// <param name="requestedScale">Scale that will be applied</param>
        public virtual void ForceResize(Vector3 requestedScale)
        {
            TransformToResize.localScale = requestedScale;
            OnResized();
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
            inputManager.StartDraggingElement(this);
        }

        /// <summary>
        /// Starts rotating the element with drag motion
        /// </summary>
        /// <exception cref="ArgumentException">Drag rotation called for ScenarioElement which does not support rotating.</exception>
        public void StartDragRotation()
        {
            if (!CanBeMoved)
                throw new ArgumentException(
                    "Drag rotation called for ScenarioElement which does not support rotating.");
            currentDragType = DragType.Rotation;
            inputManager.StartDraggingElement(this, true);
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
            inputManager.StartDraggingElement(this, true);
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
                    rotationBeforeRotating = TransformToRotate.rotation;
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
                    ForceMove(inputManager.MouseRaycastPosition);
                    break;
                case DragType.Rotation:
                    var rotationValue = (inputManager.MouseViewportPosition.x - startViewportPosition.x) *
                                        ScreenWidthRotation;
                    TransformToRotate.localRotation =
                        rotationBeforeRotating * Quaternion.Euler(0.0f, rotationValue, 0.0f);
                    OnRotated();
                    break;
                case DragType.Resize:
                    float scaleValue;
                    if (inputManager.MouseViewportPosition.x >= startViewportPosition.x)
                        scaleValue = 1.0f + (inputManager.MouseViewportPosition.x - startViewportPosition.x) *
                            ScreenWidthResizeMultiplier;
                    else
                        scaleValue = (1.0f - (startViewportPosition.x - inputManager.MouseViewportPosition.x) /
                            startViewportPosition.x);
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
            var undoManager = ScenarioManager.Instance.GetExtension<ScenarioUndoManager>();
            switch (currentDragType)
            {
                case DragType.None:
                    break;
                case DragType.Movement:
                    var records = new List<UndoRecord>();
                    records.Add(new UndoMoveElement(this, positionBeforeDrag));
                    records.Add(new UndoRotateElement(this, rotationBeforeRotating));
                    undoManager.RegisterRecord(new ComplexUndo(records));
                    break;
                case DragType.Rotation:
                    undoManager.RegisterRecord(new UndoRotateElement(this, rotationBeforeRotating));
                    break;
                case DragType.Resize:
                    undoManager.RegisterRecord(new UndoResizeElement(this, scaleBeforeResizing));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

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
        /// Method called every time when the position is updated while dragging
        /// </summary>
        /// <param name="notifyOthers">Should this call notify siblings and children</param>
        protected virtual void OnMoved(bool notifyOthers = true)
        {
            Moved?.Invoke(this);
            if (!notifyOthers)
                return;
            foreach (var siblingElement in siblingElements)
                siblingElement.OnMoved(false);
            foreach (var childElement in childElements)
                childElement.OnMoved(false);
        }

        /// <summary>
        /// Method called every time when the rotation is updated while rotating
        /// </summary>
        /// <param name="notifyOthers">Should this call notify siblings and children</param>
        protected virtual void OnRotated(bool notifyOthers = true)
        {
            Rotated?.Invoke(this);
            if (!notifyOthers)
                return;
            foreach (var siblingElement in siblingElements)
                siblingElement.OnRotated(false);
            foreach (var childElement in childElements)
                childElement.OnRotated(false);
        }

        /// <summary>
        /// Method called every time when the scale is updated while resizing
        /// </summary>
        /// <param name="notifyOthers">Should this call notify siblings and children</param>
        protected virtual void OnResized(bool notifyOthers = true)
        {
            Resized?.Invoke(this);
            if (!notifyOthers)
                return;
            foreach (var siblingElement in siblingElements)
                siblingElement.OnResized(false);
            foreach (var childElement in childElements)
                childElement.OnResized(false);
        }

        /// <summary>
        /// Method called every time when the model changes
        /// </summary>
        public virtual void OnModelChanged()
        {
            ModelChanged?.Invoke(this);
        }
    }
}