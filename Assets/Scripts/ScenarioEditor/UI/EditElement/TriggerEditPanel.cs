/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Effectors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Effectors;
    using Elements;
    using Managers;
    using MapSelecting;
    using ScenarioEditor.Utilities;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// UI panel which allows editing a selected scenario trigger
    /// </summary>
    public class TriggerEditPanel : ParameterEditPanel
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
        private DefaultEffectorEditPanel defaultEffectorEditPanelPanel;

        /// <summary>
        /// Custom effector edit panels that are build within the VSE
        /// </summary>
        [SerializeField]
        private List<EffectorEditPanel> customEffectorEditPanels;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Reference to currently selected trigger
        /// </summary>
        private ScenarioTrigger selectedTrigger;

        /// <summary>
        /// Trigger that is copied and it's effectors can be pasted to other triggers
        /// </summary>
        private ScenarioTrigger copiedTrigger;

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
        private Dictionary<string, EffectorEditPanel> effectorPanels =
            new Dictionary<string, EffectorEditPanel>();

        /// <summary>
        /// Currently visible effector panels
        /// </summary>
        private List<EffectorEditPanel> visiblePanels = new List<EffectorEditPanel>();

        /// <inheritdoc/>
        public override void Initialize()
        {
            if (isInitialized)
                return;

            var customEffectorPanels = new Dictionary<Type, EffectorEditPanel>();
            foreach (var customEffectorEditPanel in customEffectorEditPanels)
                customEffectorPanels.Add(customEffectorEditPanel.EditedEffectorType, customEffectorEditPanel);
            var allEffectorTypes = TriggersManager.GetAllEffectorsTypes();
            for (int i = 0; i < allEffectorTypes.Count; i++)
            {
                var effector = Activator.CreateInstance(allEffectorTypes[i]) as TriggerEffector;
                allEffector.Add(effector);
                AddEffectorPanel(customEffectorPanels, effector);
            }

            defaultEffectorEditPanelPanel.gameObject.SetActive(false);
            isInitialized = true;

            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;
            ScenarioManager.Instance.NewScenarioElement += OnNewElementActivation;
            OnSelectedOtherElement(ScenarioManager.Instance.SelectedElement);
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
                scenarioManager.NewScenarioElement -= OnNewElementActivation;
            }

            if (selectedTrigger != null)
            {
                selectedTrigger.Trigger.EffectorAdded -= TriggerOnEffectorAdded;
                selectedTrigger.Trigger.EffectorRemoved -= TriggerOnEffectorRemoved;
                selectedTrigger = null;
            }

            isInitialized = false;
        }

        /// <summary>
        /// Method called when another scenario element has been selected
        /// </summary>
        /// <param name="selectedElement">Scenario element that has been selected</param>
        private void OnSelectedOtherElement(ScenarioElement selectedElement)
        {
            if (selectedTrigger != null)
            {
                selectedTrigger.Trigger.EffectorAdded -= TriggerOnEffectorAdded;
                selectedTrigger.Trigger.EffectorRemoved -= TriggerOnEffectorRemoved;
                selectedTrigger = null;
            }

            foreach (var effectorPanel in visiblePanels)
            {
                effectorPanel.FinishEditing();
                effectorPanel.gameObject.SetActive(false);
            }

            visiblePanels.Clear();

            var selectedWaypoint = selectedElement as ScenarioWaypoint;
            gameObject.SetActive(selectedWaypoint != null);
            if (selectedWaypoint != null)
            {
                selectedTrigger = selectedWaypoint.LinkedTrigger;
                selectedTrigger.Trigger.EffectorAdded += TriggerOnEffectorAdded;
                selectedTrigger.Trigger.EffectorRemoved += TriggerOnEffectorRemoved;
                var effectors = selectedTrigger.Trigger.Effectors;
                var agentType = selectedTrigger.ParentAgent.Source.AgentType;
                //Get available effectors that supports this agent and their instance is not added to the trigger yet
                availableEffectorTypes =
                    allEffector.Where(newEffector =>
                        effectors.All(addedEffector => addedEffector.GetType() != newEffector.GetType()) &&
                        !newEffector.UnsupportedAgentTypes.Contains(agentType)).ToList();
                triggerSelectDropdown.options.Clear();
                triggerSelectDropdown.AddOptions(
                    availableEffectorTypes.Select(effector => effector.TypeName).ToList());

                for (var i = 0; i < effectors.Count; i++)
                {
                    var effector = effectors[i];
                    var effectorPanel = effectorPanels[effector.TypeName];
                    effectorPanel.StartEditing(this, selectedTrigger, effector);
                    effectorPanel.gameObject.SetActive(true);
                    visiblePanels.Add(effectorPanel);
                }

                UnityUtilities.LayoutRebuild(transform as RectTransform);
            }
        }

        /// <summary>
        /// Method called when new scenario element has been activated
        /// </summary>
        /// <param name="selectedElement">Scenario element that has been activated</param>
        private void OnNewElementActivation(ScenarioElement selectedElement)
        {
            if (!(selectedElement is ScenarioWaypoint waypoint)) return;
            var trigger = waypoint.LinkedTrigger;
            var effectors = trigger.Trigger.Effectors;
            foreach (var effector in effectors)
            {
                var effectorPanel = effectorPanels[effector.TypeName];
                effectorPanel.EffectorAddedToTrigger(trigger, effector);
            }
        }

        /// <summary>
        /// Method called when the effector is added to the selected trigger
        /// </summary>
        /// <param name="effector">Effector that was added to the selected trigger</param>
        private void TriggerOnEffectorAdded(TriggerEffector effector)
        {
            availableEffectorTypes.RemoveAt(triggerSelectDropdown.value);
            triggerSelectDropdown.options.RemoveAt(triggerSelectDropdown.value);
            triggerSelectDropdown.SetValueWithoutNotify(0);
            triggerSelectDropdown.RefreshShownValue();

            var effectorPanel = effectorPanels[effector.TypeName];
            effectorPanel.StartEditing(this, selectedTrigger, effector);
            effectorPanel.EffectorAddedToTrigger(selectedTrigger, effector);
            effectorPanel.gameObject.SetActive(true);
            visiblePanels.Add(effectorPanel);
            UnityUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Method called when the effector is removed from the selected trigger
        /// </summary>
        /// <param name="effector">Effector that was removed from the selected trigger</param>
        private void TriggerOnEffectorRemoved(TriggerEffector effector)
        {
            if (this == null)
                return;
            var panel = effectorPanels[effector.TypeName];
            if (panel != null)
            {
                panel.EffectorRemovedFromTrigger(selectedTrigger, effector);
                panel.FinishEditing();
                if (panel.gameObject != null)
                    panel.gameObject.SetActive(false);
                visiblePanels.Remove(panel);
            }

            UnityUtilities.LayoutRebuild(transform as RectTransform);
            availableEffectorTypes.Add(effector);
            triggerSelectDropdown.options.Add(new Dropdown.OptionData(effector.TypeName));
            triggerSelectDropdown.RefreshShownValue();
        }

        /// <summary>
        /// Adds new effector edit panel
        /// </summary>
        /// <param name="customEffectorPanels">Custom effector panels that will be used instead of default ones</param>
        /// <param name="effector">Effector for which panel will be added</param>
        /// <returns>Effectors panel that will be used for editing the effector</returns>
        private void AddEffectorPanel(Dictionary<Type, EffectorEditPanel> customEffectorPanels,
            TriggerEffector effector)
        {
            var panelPrefab = defaultEffectorEditPanelPanel.gameObject;
            if (customEffectorPanels.TryGetValue(effector.GetType(), out var editPanel))
                panelPrefab = editPanel.gameObject;
            var effectorPanel = Instantiate(panelPrefab).GetComponent<EffectorEditPanel>();
            effectorPanel.transform.SetParent(transform);
            effectorPanel.gameObject.SetActive(false);
            effectorPanels.Add(effector.TypeName, effectorPanel);
        }

        /// <summary>
        /// Adds currently selected effector to the trigger
        /// </summary>
        public void AddSelectedEffector()
        {
            if (triggerSelectDropdown.value < 0 || availableEffectorTypes.Count <= triggerSelectDropdown.value)
                return;

            var selectedEffectorType = availableEffectorTypes[triggerSelectDropdown.value].GetType();
            if (!(Activator.CreateInstance(selectedEffectorType) is TriggerEffector effector))
                throw new ArgumentException(
                    $"Invalid effector type '{availableEffectorTypes[triggerSelectDropdown.value].GetType()}'.");
            var effectorPanel = effectorPanels[effector.TypeName];
            effectorPanel.InitializeEffector(selectedTrigger, effector);
            selectedTrigger.Trigger.AddEffector(effector);
            ScenarioManager.Instance.IsScenarioDirty = true;
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new UndoAddEffector(selectedTrigger, effector));
        }

        /// <summary>
        /// Removes selected effector from the trigger and returns it to the pool
        /// </summary>
        public void RemoveEffector(TriggerEffector effector)
        {
            selectedTrigger.Trigger.RemoveEffector(effector.TypeName);
            ScenarioManager.Instance.IsScenarioDirty = true;
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new UndoRemoveEffector(selectedTrigger, effector));
        }

        /// <summary>
        /// Copies selected trigger
        /// </summary>
        public void CopyEffectors()
        {
            if (copiedTrigger!=null)
                Destroy(copiedTrigger.gameObject);
            var clonedTriggerObject = Instantiate(selectedTrigger.gameObject, ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>().transform);
            clonedTriggerObject.SetActive(false);
            copiedTrigger = clonedTriggerObject.GetComponent<ScenarioTrigger>();
            copiedTrigger.CopyProperties(selectedTrigger);
            ScenarioManager.Instance.logPanel.EnqueueInfo(
                $"Copied {selectedTrigger.Trigger.Effectors.Count} trigger effectors.");
        }

        /// <summary>
        /// Paste all the effectors from the copied trigger to this
        /// </summary>
        public void PasteEffectors()
        {
            if (copiedTrigger == null)
                return;

            var pasteAction = new Action(() =>
            {
                ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                    .RegisterRecord(new UndoTriggerCopy(selectedTrigger));
                selectedTrigger.CopyProperties(copiedTrigger);
            });

            //If there are any effectors added ask for replacing
            if (selectedTrigger.Trigger.Effectors.Count > 0)
            {
                var popupData = new ConfirmationPopup.PopupData
                {
                    Text = "Replace currently added effectors with the copied ones?"
                };
                popupData.ConfirmCallback += pasteAction;
                ScenarioManager.Instance.confirmationPopup.Show(popupData);
            }
            else
                pasteAction.Invoke();
        }
    }
}