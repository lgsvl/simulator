/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Controllables
{
    using Effectors;
    using Elements;
    using Managers;
    using ScenarioEditor.Controllables;
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

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (isInitialized)
                return;
            policyEditPanel.PolicyUpdated += PolicyEditPanelOnPolicyUpdated;
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
            policyEditPanel.PolicyUpdated -= PolicyEditPanelOnPolicyUpdated;
            isInitialized = false;
        }

        /// <summary>
        /// Method invoked when the policy was changed
        /// </summary>
        /// <param name="policy">Updated policy</param>
        private void PolicyEditPanelOnPolicyUpdated(string policy)
        {
            if (SelectedControllable!=null)
                SelectedControllable.Policy = policy;
        }

        /// <summary>
        /// Method called when another scenario element has been selected
        /// </summary>
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void OnSelectedOtherElement(ScenarioElement selectedElement)
        {
            if (SelectedControllable != null)
                policyEditPanel.SubmitChangedInputs();
            
            SelectedControllable = selectedElement as ScenarioControllable;
            //Disable edit panel if there are no valid actions or states
            if (SelectedControllable == null)
            {
                policyEditPanel.Setup(null, "");
                gameObject.SetActive(false);
            }
            else
            {
                var controllable = SelectedControllable.Variant.controllable;
                if (controllable.ValidActions.Length == 0 && controllable.ValidStates.Length == 0)
                {
                    policyEditPanel.Setup(null, "");
                    gameObject.SetActive(false);
                    return;
                }

                policyEditPanel.Setup(SelectedControllable.Variant.controllable, SelectedControllable.Policy);
                gameObject.SetActive(true);
            }
        }
    }
}