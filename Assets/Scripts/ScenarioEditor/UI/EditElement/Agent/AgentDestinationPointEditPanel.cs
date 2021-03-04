/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Agent
{
    using System;
    using Effectors;
    using Elements;
    using Elements.Agents;
    using Input;
    using Managers;
    using ScenarioEditor.Utilities;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Edit panel for the agent's destination point
    /// </summary>
    public class AgentDestinationPointEditPanel : ParameterEditPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Panel for editing an active destination point
        /// </summary>
        [SerializeField]
        private GameObject activeDestinationPointPanel;

        /// <summary>
        /// Toggle for the agent's destination point
        /// </summary>
        [SerializeField]
        private Toggle destinationPointToggle;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;
        
        /// <summary>
        /// Destination point extension that is edited by this panel
        /// </summary>
        private AgentDestinationPoint destinationPointExtension;

        /// <summary>
        /// Currently edited scenario agent reference
        /// </summary>
        private ScenarioAgent selectedAgent;

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (isInitialized)
                return;
            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;
            isInitialized = true;
            OnSelectedOtherElement(ScenarioManager.Instance.SelectedElement);
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            if (!isInitialized)
                return;
            var scenarioManager = ScenarioManager.Instance;
            if (scenarioManager != null)
                scenarioManager.SelectedOtherElement -= OnSelectedOtherElement;
            isInitialized = false;
        }

        /// <summary>
        /// Method called when another scenario element has been selected
        /// </summary>
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void OnSelectedOtherElement(ScenarioElement selectedElement)
        {
            //Detach from current agent events
            if (selectedAgent != null)
            {
                //Hide destination point if agent is deselected and something else is selected
                if (destinationPointExtension != null && selectedElement != destinationPointExtension.DestinationPoint)
                    destinationPointExtension.DestinationPoint.SetVisibility(false);
            }

            selectedAgent = selectedElement as ScenarioAgent;
            //Attach to selected agent events
            if (selectedAgent != null)
            {
                destinationPointExtension = selectedAgent.GetExtension<AgentDestinationPoint>();
                if (destinationPointExtension == null)
                    Hide();
                else
                {
                    Show();
                }
            }
            else
            {
                Hide();
            }
        }


        /// <summary>
        /// Shows this panel with prepared UI elements for currently selected agent
        /// </summary>
        public void Show()
        {
            var supportsDestinationPoint = destinationPointExtension.DestinationPoint != null;
            if (supportsDestinationPoint)
            {
                var active = destinationPointExtension.DestinationPoint.IsActive;
                destinationPointToggle.SetIsOnWithoutNotify(active);
                activeDestinationPointPanel.SetActive(active);
                destinationPointExtension.DestinationPoint.SetVisibility(active);
            }
            gameObject.SetActive(true);
            UnityUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Hides the panel and clears current agent
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets destination point as active or inactive if it is supported by selected agent
        /// </summary>
        /// <param name="active">Should the destination point be active</param>
        public void ToggleSetDestinationPoint(bool active)
        {
            if (destinationPointExtension == null) return;
            var agent = selectedAgent;
            var undoCallback = new Action<bool>((undoValue) => { SetDestinationPoint(agent, undoValue); });
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new UndoToggle(destinationPointToggle, destinationPointExtension.DestinationPoint.IsActive,
                    undoCallback));
            SetDestinationPoint(selectedAgent, active);
        }

        /// <summary>
        /// Sets destination point as active or inactive if it is supported by selected agent
        /// </summary>
        /// <param name="agent">Agent owning changed destination point</param>
        /// <param name="active">Should the destination point be active</param>
        private void SetDestinationPoint(ScenarioAgent agent, bool active)
        {
            destinationPointExtension.DestinationPoint.SetActive(active);
            var isSelected = agent == selectedAgent;
            destinationPointExtension.DestinationPoint.SetVisibility(isSelected && active);
            activeDestinationPointPanel.SetActive(isSelected && active);
        }

        /// <summary>
        /// Moves the scenario camera to the destination point
        /// </summary>
        public void MoveCameraToDestinationPoint()
        {
            var inputManager = ScenarioManager.Instance.GetExtension<InputManager>();
            var raycastHitsInCenter =
                inputManager.RaycastAll(inputManager.ScenarioCamera.ViewportPointToRay(new Vector3(0.35f, 0.5f, 0.5f)));
            if (raycastHitsInCenter.Length == 0)
                return;
            var furthestHit = inputManager.GetFurthestHit(raycastHitsInCenter, raycastHitsInCenter.Length, true);
            if (!furthestHit.HasValue)
                return;
            var cameraTransform = inputManager.ScenarioCamera.transform;
            var destinationPointPosition = destinationPointExtension.DestinationPoint.transform.position;
            var offset = furthestHit.Value.point - cameraTransform.position;
            inputManager.ForceCameraReposition(destinationPointPosition - offset, cameraTransform.rotation.eulerAngles);
            ScenarioManager.Instance.SelectedElement = destinationPointExtension.DestinationPoint;
        }

    }
}