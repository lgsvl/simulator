/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Controllables
{
    using System;
    using System.Collections.Generic;
    using Controllable;
    using Effectors;
    using Elements;
    using Managers;
    using ScenarioEditor.Controllables;
    using ScenarioEditor.Utilities;
    using UnityEngine;

    /// <summary>
    /// Edit panel for changing the controllable element
    /// </summary>
    public class ControllableEditPanel : ParameterEditPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Panel for editing the selected controllable policy
        /// </summary>
        [SerializeField]
        private PolicyEditPanel policyEditPanel;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Reference to currently selected controllable
        /// </summary>
        public ScenarioControllable SelectedControllable { get; private set; }

        /// <summary>
        /// All custom edit panels
        /// </summary>
        private readonly Dictionary<Type, IControllableEditPanel> customEditPanels =
            new Dictionary<Type, IControllableEditPanel>();

        /// <summary>
        /// Custom panel that is currently enabled
        /// </summary>
        private IControllableEditPanel enabledCustomPanel;

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (isInitialized)
                return;
            policyEditPanel.PolicyUpdated += PolicyEditPanelOnPolicyUpdated;
            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;
            var controllableManager = ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>();
            var customPanelsPrefabs = controllableManager.Source.CustomEditPanels;
            foreach (var prefab in customPanelsPrefabs)
            {
                var panel = Instantiate(prefab.PanelObject, transform);
                var controllablePanel = panel.GetComponent<IControllableEditPanel>();
                customEditPanels.Add(controllablePanel.EditedType, controllablePanel);
                panel.SetActive(false);
                controllablePanel.Initialize();
            }

            foreach (var controllable in controllableManager.Controllables)
            {
                OnControllableRegistered(controllable);
            }
            controllableManager.ControllableRegistered += OnControllableRegistered;
            isInitialized = true;
            OnSelectedOtherElement(null, ScenarioManager.Instance.SelectedElement);
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            if (!isInitialized)
                return;
            var scenarioManager = ScenarioManager.Instance;
            if (scenarioManager != null)
            {
                scenarioManager.SelectedOtherElement -= OnSelectedOtherElement;
                var controllableManager = scenarioManager.GetExtension<ScenarioControllablesManager>();
                controllableManager.ControllableRegistered += OnControllableRegistered;
            }

            policyEditPanel.PolicyUpdated -= PolicyEditPanelOnPolicyUpdated;
            foreach (var customEditPanel in customEditPanels)
            {
                customEditPanel.Value.Deinitialize();
            }
            isInitialized = false;
        }

        /// <summary>
        /// Method invoked when a new controllable is registered
        /// </summary>
        /// <param name="controllable">Controllable that is registered</param>
        private void OnControllableRegistered(ScenarioControllable controllable)
        {
            if (customEditPanels.TryGetValue(controllable.Variant.controllable.GetType(), out var customPanel))
                customPanel.InitializeControllable(controllable);
        }

        /// <summary>
        /// Method invoked when the policy was changed
        /// </summary>
        /// <param name="policy">Updated policy</param>
        private void PolicyEditPanelOnPolicyUpdated(List<ControlAction> policy)
        {
            if (SelectedControllable!=null)
                SelectedControllable.Policy = policy;
        }

        /// <summary>
        /// Method called when another scenario element has been selected
        /// </summary>
        /// <param name="previousElement">Scenario element that has been deselected</param>
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void OnSelectedOtherElement(ScenarioElement previousElement, ScenarioElement selectedElement)
        {
            if (SelectedControllable != null)
                policyEditPanel.SubmitChangedInputs();
            if (enabledCustomPanel != null)
            {
                enabledCustomPanel.Setup(null, null);
                enabledCustomPanel.PanelObject.SetActive(false);
                enabledCustomPanel = null;
            }
            
            SelectedControllable = selectedElement as ScenarioControllable;
            //Disable edit panel if there are no valid actions or states
            if (SelectedControllable == null)
            {
                policyEditPanel.Setup(null, null);
                gameObject.SetActive(false);
            }
            else
            {
                var controllable = SelectedControllable.Variant.controllable;
                if (controllable.ValidActions.Length == 0 && controllable.ValidStates.Length == 0)
                {
                    policyEditPanel.Setup(null, null);
                    gameObject.SetActive(false);
                    return;
                }
                
                if (customEditPanels.TryGetValue(controllable.GetType(), out enabledCustomPanel))
                {
                    enabledCustomPanel.Setup(SelectedControllable, SelectedControllable.Policy);
                    policyEditPanel.gameObject.SetActive(false);
                }
                else
                {
                    policyEditPanel.Setup(SelectedControllable.Variant.controllable, SelectedControllable.Policy);
                    policyEditPanel.gameObject.SetActive(true);
                }

                gameObject.SetActive(true);
                UnityUtilities.LayoutRebuild(transform as RectTransform);
            }
        }
    }
}