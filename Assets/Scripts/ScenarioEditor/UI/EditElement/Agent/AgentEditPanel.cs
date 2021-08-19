/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Agent
{
    using System.Linq;
    using Agents;
    using Effectors;
    using Elements;
    using Elements.Agents;
    using Managers;
    using ScenarioEditor.Utilities;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// UI panel which allows editing a scenario agent parameters
    /// </summary>
    public class AgentEditPanel : ParameterEditPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Dropdown for the agent variant selection
        /// </summary>
        [SerializeField]
        private Dropdown variantDropdown;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Cached agent source which variants are currently available in the dropdown
        /// </summary>
        private ScenarioAgentSource agentSource;

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
            OnSelectedOtherElement(null, ScenarioManager.Instance.SelectedElement);
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
        /// <param name="previousElement">Scenario element that has been deselected</param>
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void OnSelectedOtherElement(ScenarioElement previousElement, ScenarioElement selectedElement)
        {
            //Detach from current agent events
            if (selectedAgent != null)
            {
                selectedAgent.VariantChanged -= SelectedAgentOnVariantChanged;
            }

            selectedAgent = selectedElement as ScenarioAgent;
            //Attach to selected agent events
            if (selectedAgent != null)
            {
                Show();
                selectedAgent.VariantChanged += SelectedAgentOnVariantChanged;
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// Method invoked when selected agent changes the variant
        /// </summary>
        /// <param name="newVariant">Agent new variant</param>
        private void SelectedAgentOnVariantChanged(SourceVariant newVariant)
        {
            var variantId = variantDropdown.options.FindIndex(o => o.text == selectedAgent.Variant.Name);
            variantDropdown.SetValueWithoutNotify(variantId);
        }

        /// <summary>
        /// Shows this panel with prepared UI elements for currently selected agent
        /// </summary>
        public void Show()
        {
            //Setup variants
            agentSource = selectedAgent.Source;
            variantDropdown.options.Clear();
            variantDropdown.AddOptions(
                agentSource.Variants.Where(variant => variant.IsPrepared).Select(variant => variant.Name).ToList());
            SelectedAgentOnVariantChanged(selectedAgent.Variant);

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
        /// Method changing the variant of the currently selected scenario agent
        /// </summary>
        /// <param name="optionId">Option identifier of the selected variant</param>
        public void VariantDropdownChanged(int optionId)
        {
            var index = agentSource.Variants.FindIndex(v => v.Name == variantDropdown.options[optionId].text);
            ChangeVariant(agentSource.Variants[index]);
        }

        /// <summary>
        /// Changes variant of the selected vehicle
        /// </summary>
        /// <param name="variant">Variant that will be applied to the vehicle</param>
        /// <returns>Task</returns>
        private void ChangeVariant(SourceVariant variant)
        {
            ScenarioManager.Instance.colorPicker.Hide();
            selectedAgent.ChangeVariant(variant);
        }
    }
}