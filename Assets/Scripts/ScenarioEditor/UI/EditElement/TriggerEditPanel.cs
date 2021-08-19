/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
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
    using Elements.Triggers;
    using Managers;
    using MapSelecting;
    using ScenarioEditor.Utilities;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.Serialization;
    using UnityEngine.UI;

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
        [FormerlySerializedAs("triggerSelectDropdown")]
        [SerializeField]
        private Dropdown effectorSelectDropdown;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Cached prefabs pools
        /// </summary>
        private PrefabsPools prefabsPools;

        /// <summary>
        /// Default effector edit panel
        /// </summary>
        private DefaultEffectorEditPanel defaultEffectorEditPanel;

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
        private List<TriggerEffector> allEffectors = new List<TriggerEffector>();

        /// <summary>
        /// List of effector types that can be added to the trigger
        /// </summary>
        private List<TriggerEffector> availableEffectorTypes = new List<TriggerEffector>();

        /// <summary>
        /// Dictionary of all the effector panels required by the trigger
        /// </summary>
        private readonly Dictionary<string, EffectorEditPanel> effectorPanelsPrefabs =
            new Dictionary<string, EffectorEditPanel>();

        /// <summary>
        /// Currently visible effector panels
        /// </summary>
        private readonly Dictionary<TriggerEffector, EffectorEditPanel> visiblePanels = new Dictionary<TriggerEffector, EffectorEditPanel>();

        /// <summary>
        /// Initialization
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            prefabsPools = ScenarioManager.Instance.prefabsPools;
            var manager = ScenarioManager.Instance.GetExtension<ScenarioTriggersManager>();
            defaultEffectorEditPanel = Instantiate(manager.defaultEffectorEditPanel, transform);
            defaultEffectorEditPanel.gameObject.SetActive(false);
            var customEffectorPanels = new Dictionary<Type, EffectorEditPanel>();
            foreach (var customEffectorEditPanel in manager.customEffectorEditPanels)
                customEffectorPanels.Add(customEffectorEditPanel.EditedEffectorType, customEffectorEditPanel);
            var allEffectorTypes = TriggersManager.GetAllEffectorsTypes();
            for (int i = 0; i < allEffectorTypes.Count; i++)
            {
                var effector = Activator.CreateInstance(allEffectorTypes[i]) as TriggerEffector;
                allEffectors.Add(effector);
                InitializeEffectorPanel(customEffectorPanels, effector);
            }

            isInitialized = true;
        }

        /// <summary>
        /// Deinitialization
        /// </summary>
        public void Deinitialize()
        {
            if (!isInitialized)
                return;
            if (selectedTrigger != null)
            {
                selectedTrigger.Trigger.EffectorAdded -= TriggerOnEffectorAdded;
                selectedTrigger.Trigger.EffectorRemoved -= TriggerOnEffectorRemoved;
                selectedTrigger = null;
            }

            isInitialized = false;
        }

        /// <summary>
        /// Method called when another scenario trigger has been selected
        /// </summary>
        /// <param name="newTrigger">Scenario trigger that has been selected</param>
        public void OnSelectedNewTrigger(ScenarioTrigger newTrigger)
        {
            if (selectedTrigger != null)
            {
                selectedTrigger.Trigger.EffectorAdded -= TriggerOnEffectorAdded;
                selectedTrigger.Trigger.EffectorRemoved -= TriggerOnEffectorRemoved;
                selectedTrigger = null;
            }

            foreach (var effectorPanel in visiblePanels)
            {
                effectorPanel.Value.FinishEditing();
                prefabsPools.ReturnInstance(effectorPanel.Value.gameObject);
            }

            visiblePanels.Clear();

            gameObject.SetActive(newTrigger != null);
            if (newTrigger == null) return;
            
            selectedTrigger = newTrigger;
            selectedTrigger.Trigger.EffectorAdded += TriggerOnEffectorAdded;
            selectedTrigger.Trigger.EffectorRemoved += TriggerOnEffectorRemoved;
            var effectors = selectedTrigger.Trigger.Effectors;
            //Get available effectors that supports this agent and their instance is not added to the trigger yet
            availableEffectorTypes =
                allEffectors.Where(newEffector =>
                    //Check if multiple effectors can be added, or there is no effector of this type
                    (effectorPanelsPrefabs[newEffector.TypeName].AllowMany || effectors.All(addedEffector =>
                        addedEffector.GetType() != newEffector.GetType())) &&
                    //Check if effector is supported for selected agent type
                    !newEffector.UnsupportedAgentTypes.Contains(selectedTrigger.TargetAgentType)).ToList();
            effectorSelectDropdown.options.Clear();
            effectorSelectDropdown.AddOptions(
                availableEffectorTypes.Select(effector => effector.TypeName).ToList());

            for (var i = 0; i < effectors.Count; i++)
            {
                var effector = effectors[i];
                var effectorPanel = prefabsPools.GetInstance(effectorPanelsPrefabs[effector.TypeName].gameObject)
                    .GetComponent<EffectorEditPanel>();
                effectorPanel.StartEditing(this, selectedTrigger, effector);
                effectorPanel.transform.SetParent(transform);
                effectorPanel.gameObject.SetActive(true);
                visiblePanels.Add(effector, effectorPanel);
            }

            UnityUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Method called when the effector is added to the selected trigger
        /// </summary>
        /// <param name="effector">Effector that was added to the selected trigger</param>
        private void TriggerOnEffectorAdded(TriggerEffector effector)
        {
            var effectorPanel = prefabsPools.GetInstance(effectorPanelsPrefabs[effector.TypeName].gameObject)
                .GetComponent<EffectorEditPanel>();
            if (!effectorPanel.AllowMany)
            {
                availableEffectorTypes.RemoveAt(effectorSelectDropdown.value);
                effectorSelectDropdown.options.RemoveAt(effectorSelectDropdown.value);
                effectorSelectDropdown.SetValueWithoutNotify(0);
                effectorSelectDropdown.RefreshShownValue();
            }

            effectorPanel.StartEditing(this, selectedTrigger, effector);
            effectorPanel.EffectorAddedToTrigger(selectedTrigger, effector);
            effectorPanel.transform.SetParent(transform);
            effectorPanel.gameObject.SetActive(true);
            visiblePanels.Add(effector, effectorPanel);
            UnityUtilities.LayoutRebuild(effectorPanel.transform as RectTransform);
        }

        /// <summary>
        /// Method called when the effector is removed from the selected trigger
        /// </summary>
        /// <param name="effector">Effector that was removed from the selected trigger</param>
        private void TriggerOnEffectorRemoved(TriggerEffector effector)
        {
            if (this == null)
                return;
            if (visiblePanels.TryGetValue(effector, out var panel))
            {
                panel.EffectorRemovedFromTrigger(selectedTrigger, effector);
                panel.FinishEditing();
                if (panel.gameObject != null)
                    prefabsPools.ReturnInstance(panel.gameObject);
                visiblePanels.Remove(effector);
            }

            UnityUtilities.LayoutRebuild(transform as RectTransform);
            if (!availableEffectorTypes.Contains(effector))
            {
                availableEffectorTypes.Add(effector);
                effectorSelectDropdown.options.Add(new Dropdown.OptionData(effector.TypeName));
                effectorSelectDropdown.RefreshShownValue();
            }
        }

        /// <summary>
        /// Initializes new prefab for the effector edit panel
        /// </summary>
        /// <param name="customEffectorPanels">Custom effector panels that will be used instead of default ones</param>
        /// <param name="effector">Effector for which panel will be added</param>
        /// <returns>Effectors panel that will be used for editing the effector</returns>
        private void InitializeEffectorPanel(Dictionary<Type, EffectorEditPanel> customEffectorPanels,
            TriggerEffector effector)
        {
            var panelPrefab = defaultEffectorEditPanel.GetComponent<EffectorEditPanel>();
            if (customEffectorPanels.TryGetValue(effector.GetType(), out var editPanel))
                panelPrefab = editPanel.GetComponent<EffectorEditPanel>();
            effectorPanelsPrefabs.Add(effector.TypeName, panelPrefab);
        }

        /// <summary>
        /// Adds currently selected effector to the trigger
        /// </summary>
        public void AddSelectedEffector()
        {
            if (effectorSelectDropdown.value < 0 || availableEffectorTypes.Count <= effectorSelectDropdown.value)
                return;

            var selectedEffectorType = availableEffectorTypes[effectorSelectDropdown.value].GetType();
            if (!(Activator.CreateInstance(selectedEffectorType) is TriggerEffector effector))
                throw new ArgumentException(
                    $"Invalid effector type '{availableEffectorTypes[effectorSelectDropdown.value].GetType()}'.");
            var effectorPanel = effectorPanelsPrefabs[effector.TypeName];
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
            selectedTrigger.Trigger.RemoveEffector(effector);
            ScenarioManager.Instance.IsScenarioDirty = true;
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                .RegisterRecord(new UndoRemoveEffector(selectedTrigger, effector));
        }

        /// <summary>
        /// Copies selected trigger
        /// </summary>
        public void CopyEffectors()
        {
            if (copiedTrigger != null)
                Destroy(copiedTrigger.gameObject);
            var clonedTriggerObject = Instantiate(selectedTrigger.gameObject,
                ScenarioManager.Instance.GetExtension<ScenarioWaypointsManager>().transform);
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
            if (copiedTrigger.TargetAgentType != selectedTrigger.TargetAgentType)
            {
                ScenarioManager.Instance.logPanel.EnqueueWarning("Cannot paste trigger effectors. Copied trigger targets different agent type.");
                return;
            }

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