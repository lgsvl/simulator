/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Agents;
    using Elements;
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;
    using Utilities;

    /// <summary>
    /// UI panel which allows editing a selected scenario trigger
    /// </summary>
    public class TriggerEditPanel : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Dropdown for the agent variant selection
        /// </summary>
        [SerializeField]
        private Dropdown triggerSelectDropdown;

        /// <summary>
        /// Sample of the effector panel
        /// </summary>
        [SerializeField]
        private TriggerEffectorPanel effectorSamplePanel;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Reference to currently selected trigger
        /// </summary>
        private ScenarioTrigger trigger;

        /// <summary>
        /// List of all the effector types
        /// </summary>
        private List<TriggerEffector> allEffector = new List<TriggerEffector>();

        /// <summary>
        /// List of effector types that can be added to the trigger
        /// </summary>
        private List<TriggerEffector> availableEffectorTypes = new List<TriggerEffector>();

        /// <summary>
        /// Dictionary of all the effector panels required by the trigger
        /// </summary>
        private Dictionary<TriggerEffector, TriggerEffectorPanel> effectorPanels =
            new Dictionary<TriggerEffector, TriggerEffectorPanel>();

        /// <summary>
        /// Unity Start method
        /// </summary>
        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            Deinitialize();
        }

        /// <summary>
        /// Unity OnEnable method
        /// </summary>
        private void OnEnable()
        {
            Initialize();
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        private void Initialize()
        {
            if (isInitialized)
                return;
            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;

            var interfaceType = typeof(TriggerEffector);
            var allEffectorTypes = TriggersManager.GetAllEffectorsTypes();
            for (int i = 0; i < allEffectorTypes.Count; i++)
                allEffector.Add(Activator.CreateInstance(allEffectorTypes[i]) as TriggerEffector);
            effectorSamplePanel.gameObject.SetActive(false);
            isInitialized = true;
            OnSelectedOtherElement(ScenarioManager.Instance.SelectedElement);
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        private void Deinitialize()
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
            foreach (var effectorPanel in effectorPanels)
                ScenarioManager.Instance.prefabsPools.ReturnInstance(effectorPanel.Value.gameObject);
            effectorPanels.Clear();

            var selectedWaypoint = selectedElement as ScenarioWaypoint;
            gameObject.SetActive(selectedWaypoint != null);
            if (selectedWaypoint != null)
            {
                trigger = selectedWaypoint.LinkedTrigger;
                var effectors = trigger.Trigger.Effectors;
                var agentType = (AgentType)trigger.ParentAgent.Source.AgentTypeId;
                //Get available effectors that supports this agent and their instance is not added to the trigger yet
                //TODO !newEffector.UnsupportedAgentTypes.Contains(agentType) &&
                availableEffectorTypes = 
                    allEffector.Where(newEffector => effectors.All(addedEffector => addedEffector.GetType() != newEffector.GetType())).ToList();
                triggerSelectDropdown.options.Clear();
                triggerSelectDropdown.AddOptions(
                    availableEffectorTypes.Select(effector => effector.TypeName).ToList());

                for (var i = 0; i < effectors.Count; i++)
                {
                    var effector = effectors[i];
                    var effectorPanel = ScenarioManager.Instance.prefabsPools
                        .GetInstance(effectorSamplePanel.gameObject)
                        .GetComponent<TriggerEffectorPanel>();
                    effectorPanel.transform.SetParent(transform);
                    effectorPanel.gameObject.SetActive(true);
                    effectorPanel.Initialize(this, effector);
                    effectorPanels.Add(effector, effectorPanel);
                }

                UIUtilities.LayoutRebuild(transform as RectTransform);
            }
        }

        /// <summary>
        /// Adds currently selected effector to the trigger
        /// </summary>
        public void AddSelectedEffector()
        {
            if (triggerSelectDropdown.value < 0 || availableEffectorTypes.Count <= triggerSelectDropdown.value)
                return;
            
            ScenarioManager.Instance.IsScenarioDirty = true;
            var selectedEffectorType = availableEffectorTypes[triggerSelectDropdown.value].GetType();
            if (!(Activator.CreateInstance(selectedEffectorType) is TriggerEffector effector))
                throw new ArgumentException(
                    $"Invalid effector type '{availableEffectorTypes[triggerSelectDropdown.value].GetType()}'.");
            trigger.Trigger.Effectors.Add(effector);
            availableEffectorTypes.RemoveAt(triggerSelectDropdown.value);
            triggerSelectDropdown.options.RemoveAt(triggerSelectDropdown.value);
            triggerSelectDropdown.SetValueWithoutNotify(0);
            triggerSelectDropdown.RefreshShownValue();

            var effectorPanel = ScenarioManager.Instance.prefabsPools.GetInstance(effectorSamplePanel.gameObject)
                .GetComponent<TriggerEffectorPanel>();
            effectorPanel.transform.SetParent(transform);
            effectorPanel.gameObject.SetActive(true);
            effectorPanel.Initialize(this, effector);
            effectorPanels.Add(effector, effectorPanel);
            UIUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Removes selected effector from the trigger and returns it to the pool
        /// </summary>
        public void RemoveEffector(TriggerEffector effector)
        {
            var panel = effectorPanels[effector];
            ScenarioManager.Instance.prefabsPools.ReturnInstance(panel.gameObject);
            effectorPanels.Remove(effector);
            UIUtilities.LayoutRebuild(transform as RectTransform);

            ScenarioManager.Instance.IsScenarioDirty = true;
            trigger.Trigger.Effectors.Remove(effector);
            availableEffectorTypes.Add(effector);
            triggerSelectDropdown.options.Add(new Dropdown.OptionData(effector.TypeName));
            triggerSelectDropdown.RefreshShownValue();
        }
    }
}