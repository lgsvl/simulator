/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements
{
    using System.Collections.Generic;
    using Input;
    using Managers;
    using ScenarioEditor.Agents;
    using Undo;
    using Undo.Records;
    using UnityEngine;

    /// <summary>
    /// Abstract scenario element source 
    /// </summary>
    public abstract class ScenarioElementSource : MonoBehaviour, IDragHandler
    {
        /// <summary>
        /// Name of the agent type this source handles
        /// </summary>
        public abstract string ElementTypeName { get; }
        
        /// <summary>
        /// List of available variants in this element source
        /// </summary>
        public abstract List<SourceVariant> Variants { get; }

        /// <summary>
        /// Currently dragged element instance
        /// </summary>
        protected ScenarioElement draggedInstance;

        /// <summary>
        /// Controllable variant that is currently selected
        /// </summary>
        protected SourceVariant selectedVariant;

        /// <summary>
        /// Method invokes when this source is selected in the UI
        /// </summary>
        /// <param name="variant">Scenario element variant that is selected</param>
        public virtual void OnVariantSelected(SourceVariant variant)
        {
            selectedVariant = variant;
            if (variant!=null)
                ScenarioManager.Instance.GetExtension<InputManager>().StartDraggingElement(this);
        }
        
        /// <summary>
        /// Method that instantiates and initializes a prefab of the selected variant
        /// </summary>
        /// <param name="variant">Scenario element variant which model should be instantiated</param>
        /// <returns>Scenario element variant model</returns>
        public virtual GameObject GetModelInstance(SourceVariant variant)
        {
            if (variant.Prefab == null)
                return null;
            var instance = ScenarioManager.Instance.prefabsPools.GetInstance(variant.Prefab);
            return instance;
        }

        /// <summary>
        /// Creates an element instance for the given source variant
        /// </summary>
        /// <param name="variant">Source variant for the new instance</param>
        /// <returns>Element instance for the given source variant</returns>
        public abstract ScenarioElement GetElementInstance(SourceVariant variant);

        /// <summary>
        /// Method invoked when dragged instance is moved
        /// </summary>
        protected virtual void OnDraggedInstanceMove()
        {
            
        }
        
        /// <inheritdoc/>
        void IDragHandler.DragStarted()
        {
            var inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            draggedInstance = GetElementInstance(selectedVariant);
            draggedInstance.transform.SetParent(ScenarioManager.Instance.transform);
            draggedInstance.transform.SetPositionAndRotation(inputManager.MouseRaycastPosition,
                Quaternion.Euler(0.0f, 0.0f, 0.0f));
            ScenarioManager.Instance.SelectedElement = draggedInstance;
            OnDraggedInstanceMove();
        }

        /// <inheritdoc/>
        void IDragHandler.DragMoved()
        {
            var inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            if (draggedInstance == null)
            {
                inputManager.CancelDraggingElement(this);
                return;
            }
            draggedInstance.transform.position = inputManager.MouseRaycastPosition;
            OnDraggedInstanceMove();
        }

        /// <inheritdoc/>
        void IDragHandler.DragFinished()
        {
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new UndoAddElement(draggedInstance));
            draggedInstance = null;
        }

        /// <inheritdoc/>
        void IDragHandler.DragCancelled()
        {
            if (draggedInstance == null)
                return;
            draggedInstance.RemoveFromMap();
            draggedInstance.Dispose();
            draggedInstance = null;
        }
    }
}