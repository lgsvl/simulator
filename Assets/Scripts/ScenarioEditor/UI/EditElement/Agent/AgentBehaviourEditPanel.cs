/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Agent
{
    using System.Collections.Generic;
    using Effectors;
    using Elements;
    using Elements.Agents;
    using Managers;
    using ScenarioEditor.Utilities;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Edit panel for the agent's behaviour
    /// </summary>
    public class AgentBehaviourEditPanel : ParameterEditPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Dropdown for the agent behaviour selection
        /// </summary>
        [SerializeField]
        private Dropdown behaviourDropdown;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;
        
        /// <summary>
        /// Behaviour extension that is edited by this panel
        /// </summary>
        private AgentBehaviour behaviourExtension;

        /// <summary>
        /// Currently edited scenario agent reference
        /// </summary>
        private ScenarioAgent selectedAgent;

        /// <summary>
        /// Behaviours that can be selected for current agent
        /// </summary>
        private List<string> availableBehaviours;

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
            if (behaviourExtension != null)
                behaviourExtension.BehaviourChanged -= BehaviourExtensionOnBehaviourChanged;

            selectedAgent = selectedElement as ScenarioAgent;
            //Attach to selected agent events
            if (selectedAgent != null)
            {
                behaviourExtension = selectedAgent.GetExtension<AgentBehaviour>();
                if (behaviourExtension == null)
                    Hide();
                else
                {
                    behaviourExtension.BehaviourChanged += BehaviourExtensionOnBehaviourChanged;
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
            availableBehaviours = selectedAgent.Source.Behaviours;
            //Setup behaviour
            if (availableBehaviours != null && availableBehaviours.Count > 0)
            {
                behaviourDropdown.options.Clear();
                behaviourDropdown.AddOptions(availableBehaviours);
                behaviourDropdown.gameObject.SetActive(true);
            }
            else
            {
                behaviourDropdown.gameObject.SetActive(false);
            }
            if (availableBehaviours != null)
            {
                var behaviourId = string.IsNullOrEmpty(behaviourExtension.Behaviour)
                    ? 0
                    : availableBehaviours.IndexOf(behaviourExtension.Behaviour);
                behaviourDropdown.SetValueWithoutNotify(behaviourId);
                behaviourDropdown.RefreshShownValue();
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
        /// Method invoked when the behaviour is extension changes
        /// </summary>
        /// <param name="behaviour">New behaviour applied to the extension</param>
        private void BehaviourExtensionOnBehaviourChanged(string behaviour)
        {
            var behaviourId = availableBehaviours.IndexOf(behaviour);
            behaviourDropdown.SetValueWithoutNotify(behaviourId);
        }

        /// <summary>
        /// Method changing the behaviour of the currently selected scenario agent
        /// </summary>
        /// <param name="behaviourId">Behaviour identifier in the source</param>
        public void BehaviourDropdownChanged(int behaviourId)
        {
            behaviourExtension.ChangeBehaviour(availableBehaviours[behaviourId]);
        }
    }
}