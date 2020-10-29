/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Controllables
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Agents;
    using Elements;
    using Input;
    using Managers;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using Web;

    /// <summary>
    /// This scenario source handles adding controllable
    /// </summary>
    public class ScenarioControllableSource : ScenarioElementSource, IDragHandler
    {
        /// <summary>
        /// Cached reference to the scenario editor input manager
        /// </summary>
        private InputManager inputManager;

        /// <summary>
        /// Currently dragged agent instance
        /// </summary>
        private GameObject draggedInstance;

        /// <summary>
        /// Is this source initialized
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <inheritdoc/>
        public override string ElementTypeName { get; } = "Controllable";

        /// <summary>
        /// List of available controllable variants
        /// </summary>
        public override List<SourceVariant> Variants { get; } = new List<SourceVariant>();

        /// <summary>
        /// Controllable variant that is currently selected
        /// </summary>
        private ControllableVariant selectedVariant;

        /// <summary>
        /// Initialization method
        /// </summary>
        /// <returns>Task</returns>
        public Task Initialize()
        {
            if (IsInitialized)
                return Task.CompletedTask;
            inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            var controllables = Config.Controllables;
            foreach (var controllable in controllables)
            {
                var variant = new ControllableVariant();
                variant.Setup(controllable.Key, controllable.Value);
                Variants.Add(variant);
            }

            IsInitialized = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            if (!IsInitialized)
                return;
            IsInitialized = false;
        }

        /// <summary>
        /// Method that instantiates new <see cref="ScenarioControllable"/> and initializes it with selected variant
        /// </summary>
        /// <param name="variant">Controllable variant which model should be instantiated</param>
        /// <returns><see cref="ScenarioControllable"/> initialized with selected variant</returns>
        public ScenarioControllable GetControllableInstance(ControllableVariant variant)
        {
            var newGameObject = new GameObject(ElementTypeName);
            newGameObject.transform.SetParent(transform);
            var scenarioControllable = newGameObject.AddComponent<ScenarioControllable>();
            scenarioControllable.Setup(this, variant);
            return scenarioControllable;
        }

        /// <inheritdoc/>
        public override void OnVariantSelected(SourceVariant variant)
        {
            selectedVariant = variant as ControllableVariant;
            if (selectedVariant!=null)
                ScenarioManager.Instance.GetExtension<InputManager>().StartDraggingElement(this);
        }

        /// <inheritdoc/>
        public void DragStarted()
        {
            draggedInstance = GetModelInstance(selectedVariant);
            draggedInstance.transform.SetParent(ScenarioManager.Instance.transform);
            draggedInstance.transform.SetPositionAndRotation(inputManager.MouseRaycastPosition,
                Quaternion.Euler(0.0f, 0.0f, 0.0f));
        }

        /// <inheritdoc/>
        public void DragMoved()
        {
            draggedInstance.transform.position = inputManager.MouseRaycastPosition;
        }

        /// <inheritdoc/>
        public void DragFinished()
        {
            var controllable = GetControllableInstance(selectedVariant);
            controllable.transform.rotation = draggedInstance.transform.rotation;
            controllable.transform.position = draggedInstance.transform.position;
            ScenarioManager.Instance.prefabsPools.ReturnInstance(draggedInstance);
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new UndoAddElement(controllable));
            draggedInstance = null;
        }

        /// <inheritdoc/>
        public void DragCancelled()
        {
            draggedInstance = null;
        }
    }
}