/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Agent
{
    using Agents;
    using Effectors;
    using Elements;
    using Elements.Agents;
    using Managers;
    using ScenarioEditor.Utilities;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Edit panel for the agent's sensors configuration
    /// </summary>
    public class AgentSensorsConfigurationEditPanel : ParameterEditPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Dropdown for the agent sensors configuration selection
        /// </summary>
        [SerializeField]
        private Dropdown sensorsConfigurationDropdown;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;
        
        /// <summary>
        /// Sensors configuration that is edited by this panel
        /// </summary>
        private AgentSensorsConfiguration sensorsConfiguration;

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
                if (sensorsConfiguration != null)
                    sensorsConfiguration.SensorsConfigurationIdChanged -= SelectedAgentOnSensorsConfigurationIdChanged;
            }

            selectedAgent = selectedElement as ScenarioAgent;
            //Attach to selected agent events
            if (selectedAgent != null)
            {
                sensorsConfiguration = selectedAgent.GetExtension<AgentSensorsConfiguration>();
                if (sensorsConfiguration == null)
                    Hide();
                else
                {
                    selectedAgent.VariantChanged += SelectedAgentOnVariantChanged;
                    sensorsConfiguration.SensorsConfigurationIdChanged += SelectedAgentOnSensorsConfigurationIdChanged;
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
            SetupSensorsConfigurationDropdown();
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
        /// Method invoked when selected agent changes the sensors configuration id
        /// </summary>
        /// <param name="newId">Agent new sensors configuration id</param>
        private void SelectedAgentOnSensorsConfigurationIdChanged(string newId)
        {
            if (!(selectedAgent.Variant is EgoAgentVariant egoAgentVariant))
                return;
            var configNo = egoAgentVariant.SensorsConfigurations.FindIndex(c => c.Id == newId);
            sensorsConfigurationDropdown.SetValueWithoutNotify(configNo);
        }

        /// <summary>
        /// Method invoked when selected agent changes the variant
        /// </summary>
        /// <param name="newVariant">Agent new variant</param>
        private void SelectedAgentOnVariantChanged(SourceVariant newVariant)
        {
            SetupSensorsConfigurationDropdown();
        }

        /// <summary>
        /// Setups the sensors configuration dropdown basing on the currently selected agent
        /// </summary>
        private void SetupSensorsConfigurationDropdown()
        {
            if (selectedAgent.Variant is EgoAgentVariant egoAgentVariant &&
                egoAgentVariant.SensorsConfigurations.Count > 0)
            {
                sensorsConfigurationDropdown.options.Clear();
                var selectedNo = 0;
                for (var i = 0; i < egoAgentVariant.SensorsConfigurations.Count; i++)
                {
                    var configuration = egoAgentVariant.SensorsConfigurations[i];
                    sensorsConfigurationDropdown.options.Add(new Dropdown.OptionData(configuration.Name));
                    if (configuration.Id == sensorsConfiguration.SensorsConfigurationId)
                        selectedNo = i;
                }

                sensorsConfigurationDropdown.SetValueWithoutNotify(selectedNo);
                sensorsConfigurationDropdown.RefreshShownValue();
                sensorsConfigurationDropdown.gameObject.SetActive(true);
            }
            else
            {
                sensorsConfigurationDropdown.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Method changing the sensors configuration of the currently selected scenario agent
        /// </summary>
        /// <param name="configNo">Order number of the selected sensors configuration</param>
        public void SensorsConfigurationDropdownChanged(int configNo)
        {
            if (selectedAgent.Variant is EgoAgentVariant egoAgentVariant)
                sensorsConfiguration.ChangeSensorsConfigurationId(egoAgentVariant.SensorsConfigurations[configNo].Id);
        }
    }
}