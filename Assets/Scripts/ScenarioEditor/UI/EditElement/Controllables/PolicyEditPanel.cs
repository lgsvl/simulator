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
    using System.Linq;
    using System.Text;
    using Controllable;
    using Managers;
    using MapSelecting;
    using ScenarioEditor.Controllables;
    using ScenarioEditor.Utilities;
    using Simulator.Utilities;
    using Undo;
    using Undo.Records;
    using UnityEngine;

    /// <summary>
    /// Panel for editing a controllable policy
    /// </summary>
    public class PolicyEditPanel : MonoBehaviour
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Single policy entry element in the controllable policy
        /// </summary>
        [SerializeField]
        private PolicyEntry policyEntryPrefab;
#pragma warning restore 0649

        /// <summary>
        /// Is this panel initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Valid actions list for currently selected controllable
        /// </summary>
        private List<string> validActions;

        /// <summary>
        /// Valid states list for currently selected controllable
        /// </summary>
        private List<string> validStates;

        /// <summary>
        /// Currently viewed policy entries
        /// </summary>
        private readonly List<PolicyEntry> entries = new List<PolicyEntry>();

        /// <summary>
        /// Reference to currently bound controllable
        /// </summary>
        public IControllable BoundControllable { get; private set; }

        /// <summary>
        /// Current policy
        /// </summary>
        public List<ControlAction> Policy { get; private set; } = new List<ControlAction>();

        /// <summary>
        /// Event called when the policy is updated
        /// </summary>
        public event Action<List<ControlAction>> PolicyUpdated;

        /// <summary>
        /// Submits changed input field value
        /// </summary>
        public void SubmitChangedInputs()
        {
            foreach (var policyEntry in entries)
                policyEntry.SubmitChangedInputs();
        }

        /// <summary>
        /// Setups the policy edit panel according to the passed controllable and policy
        /// </summary>
        /// <param name="controllable">Scenario controllable which policy will be edited</param>
        /// <param name="initialPolicy">Policy that will be applied during the setup</param>
        public void Setup(IControllable controllable, List<ControlAction> initialPolicy)
        {
            if (BoundControllable != null)
                SubmitChangedInputs();
            BoundControllable = controllable;
            if (BoundControllable == null)
            {
                gameObject.SetActive(false);
                UnityUtilities.LayoutRebuild(transform as RectTransform);
                return;
            }

            //List valid actions for this controllable
            validActions = controllable.ValidActions.ToList();
            validStates = controllable.ValidStates.ToList();
            if (validStates.Count > 0)
                validActions.Add("state");

            //Check if this controllable can use policy
            if (validActions.Count == 0)
            {
                gameObject.SetActive(false);
                UnityUtilities.LayoutRebuild(transform as RectTransform);
                return;
            }

            SetPolicy(initialPolicy);
            gameObject.SetActive(true);
            UnityUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Sets the policy to this panel and initialize editable entries
        /// </summary>
        /// <param name="policy">Policy that will be set</param>
        private void SetPolicy(List<ControlAction> policy)
        {
            Policy = policy;
            if (policy != null)
            {
                while (entries.Count < policy.Count) AddPolicyEntry(updatePolicy: false);
                while (entries.Count > policy.Count) RemovePolicyEntry(entries[0], updatePolicy: false);

                for (var i = 0; i < entries.Count; i++)
                {
                    entries[i].Deinitialize();
                    entries[i].Initialize(this, validActions, validStates, policy[i].Action,
                        policy[i].Value);
                }
            }
            else
                while (entries.Count > 0)
                    RemovePolicyEntry(entries[0]);

            PolicyUpdated?.Invoke(Policy);
            ScenarioManager.Instance.IsScenarioDirty = true;
        }

        /// <summary>
        /// Updates the controllable policy basing on the inherited policy entries
        /// </summary>
        public void UpdatePolicy()
        {
            if (BoundControllable == null)
                return;
            Policy.Clear();
            for (var i = 0; i < entries.Count; i++)
            {
                if (!entries[i].IsValid)
                    continue;
                Policy.Add(entries[i].Policy);
            }

            PolicyUpdated?.Invoke(Policy);
            ScenarioManager.Instance.IsScenarioDirty = true;
        }

        /// <summary>
        /// Add a single policy entry to this edit panel, registers the undo record
        /// </summary>
        public void AddPolicyEntryWithUndo()
        {
            var newEntry = ScenarioManager.Instance.prefabsPools.GetInstance(policyEntryPrefab.gameObject)
                .GetComponent<PolicyEntry>();
            var undoRecord = new GenericUndo<PolicyEntry>(newEntry, "Undo adding a policy entry",
                entry => RemovePolicyEntry(entry));
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(undoRecord);
            AddPolicyEntry(newEntry);
        }

        /// <summary>
        /// Add a single policy entry to this edit panel
        /// </summary>
        /// <param name="newEntry">Policy entry to be added</param>
        /// <param name="updatePolicy">Should the policy be updated</param>
        private void AddPolicyEntry(PolicyEntry newEntry = null, bool updatePolicy = true)
        {
            if (newEntry == null)
                newEntry = ScenarioManager.Instance.prefabsPools.GetInstance(policyEntryPrefab.gameObject)
                    .GetComponent<PolicyEntry>();
            entries.Add(newEntry);
            newEntry.Initialize(this, validActions, validStates, validActions[0], "");
            newEntry.transform.SetParent(transform);
            newEntry.gameObject.SetActive(true);
            if (updatePolicy)
                UpdatePolicy();
            UnityUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Remove the policy entry from this edit panel, registers the undo record
        /// </summary>
        /// <param name="entry">Policy entry to be removed</param>
        public void RemovePolicyEntryWithUndo(PolicyEntry entry)
        {
            var undo = new Action<PolicyEntry>(revertedEntry =>
            {
                entry.gameObject.SetActive(true);
                entries.Add(entry);
                UpdatePolicy();
                UnityUtilities.LayoutRebuild(transform as RectTransform);
            });
            var dispose = new Action<PolicyEntry>(disposedEntry =>
            {
                ScenarioManager.Instance.prefabsPools.ReturnInstance(disposedEntry.gameObject);
            });
            var undoRecord = new GenericUndo<PolicyEntry>(entry, "Undo removing a policy entry", undo, dispose);
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(undoRecord);
            RemovePolicyEntry(entry);
        }

        /// <summary>
        /// Remove the policy entry from this edit panel
        /// </summary>
        /// <param name="entry">Policy entry to be removed</param>
        /// <param name="updatePolicy">Should the policy be updated</param>
        public void RemovePolicyEntry(PolicyEntry entry, bool updatePolicy = true)
        {
            entry.gameObject.SetActive(false);
            entries.Remove(entry);
            entry.Deinitialize();
            if (updatePolicy)
                UpdatePolicy();
            UnityUtilities.LayoutRebuild(transform as RectTransform);
        }

        /// <summary>
        /// Copies policy from this panel
        /// </summary>
        public void CopyPolicy()
        {
            if (BoundControllable == null)
            {
                ScenarioManager.Instance.logPanel.EnqueueWarning(
                    "Cannot copy policy that is not bound to a controllable.");
                return;
            }

            ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>().CopyPolicy(BoundControllable, Policy);
            ScenarioManager.Instance.logPanel.EnqueueInfo(
                $"Copied policy for the controllable {BoundControllable.GetType().Name}.");
        }

        /// <summary>
        /// Pastes policy to this panel
        /// </summary>
        public void PastePolicy()
        {
            if (BoundControllable == null)
            {
                ScenarioManager.Instance.logPanel.EnqueueWarning(
                    "Cannot copy policy that is not bound to a controllable.");
                return;
            }

            if (ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>()
                .GetCopiedPolicy(BoundControllable, out var policy))
            {
                var pasteAction = new Action(() =>
                {
                    ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                        .RegisterRecord(new GenericUndo<List<ControlAction>>(Policy, "Undo pasting policy", SetPolicy));
                    SetPolicy(policy);
                });
                //Do not show confirmation popup if policy is empty
                if (Policy == null || Policy.Count == 0)
                {
                    pasteAction.Invoke();
                    return;
                }

                //Ask for replacing current policy
                var popupData = new ConfirmationPopup.PopupData
                {
                    Text = "Replace current policy with the copied one?"
                };
                popupData.ConfirmCallback += pasteAction;
                ScenarioManager.Instance.confirmationPopup.Show(popupData);
            }
            else
                ScenarioManager.Instance.logPanel.EnqueueWarning(
                    "Cannot paste policy copied from other controllable type.");
        }
    }
}