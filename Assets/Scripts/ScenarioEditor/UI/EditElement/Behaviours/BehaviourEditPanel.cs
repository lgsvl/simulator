/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Behaviours
{
    using Effectors;
    using Elements;
    using Elements.Agents;
    using Managers;
    using ScenarioEditor.Utilities;
    using UnityEngine;

    /// <summary>
    /// UI panel which allows editing a scenario agent behaviour
    /// </summary>
    public abstract class BehaviourEditPanel : ParameterEditPanel
    {
        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;
        
        /// <summary>
        /// Is this panel shown
        /// </summary>
        private bool isShown = true;

        /// <summary>
        /// Currently edited scenario agent reference
        /// </summary>
        protected ScenarioAgent selectedAgent;

        /// <summary>
        /// Currently edited behaviours extensions
        /// </summary>
        protected AgentBehaviour behaviourExtension;
        
        /// <summary>
        /// Behaviour name that can be edited with this panel
        /// </summary>
        protected abstract string EditedBehaviour { get; }

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (isInitialized)
                return;
            Hide();
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
            {
                behaviourExtension.BehaviourChanged -= SelectedAgentOnBehaviourChanged;
                Hide();
            }

            selectedAgent = selectedElement as ScenarioAgent;
            behaviourExtension = selectedAgent == null ? null : selectedAgent.GetExtension<AgentBehaviour>();
            //Attach to selected agent events
            if (behaviourExtension != null)
            {
                behaviourExtension.BehaviourChanged += SelectedAgentOnBehaviourChanged;
                Show();
            }
            SelectedAgentOnBehaviourChanged(behaviourExtension == null ? "" : behaviourExtension.Behaviour);
        }

        /// <summary>
        /// Method invoked when selected agent changes the behaviour
        /// </summary>
        /// <param name="newBehaviour">Agent new behaviour</param>
        private void SelectedAgentOnBehaviourChanged(string newBehaviour)
        {
            if (selectedAgent == null || newBehaviour != EditedBehaviour)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// Shows this panel with prepared UI elements for currently selected agent
        /// </summary>
        public void Show()
        {
            if (isShown)
                return;
            gameObject.SetActive(true);
            UnityUtilities.LayoutRebuild(transform as RectTransform);
            OnShown();
            isShown = true;
        }

        /// <summary>
        /// Hides the panel and clears current agent
        /// </summary>
        public void Hide()
        {
            if (!isShown)
                return;
            gameObject.SetActive(false);
            OnHidden();
            isShown = false;
        }

        /// <summary>
        /// Method invoked when this panel is being shown
        /// </summary>
        protected virtual void OnShown() { }

        /// <summary>
        /// Method invoked when this panel is being hidden
        /// </summary>
        protected virtual void OnHidden() { }
    }
}