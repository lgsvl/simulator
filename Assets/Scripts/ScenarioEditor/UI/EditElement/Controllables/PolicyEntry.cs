/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Controllables
{
    using System;
    using System.Collections.Generic;
    using Controllable;
    using Managers;
    using Simulator.Utilities;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// Single policy entry element in the controllable policy
    /// </summary>
    public class PolicyEntry : MonoBehaviour
    {
        /// <summary>
        /// Type of this entry, this type varies entry behaviour
        /// </summary>
        private enum EntryType
        {
            /// <summary>
            /// Basic entry type with manual input value
            /// </summary>
            Action = 0,

            /// <summary>
            /// Entry type with dropdown value
            /// </summary>
            State = 1,

            /// <summary>
            /// Entry type with manual input value for decimals only
            /// </summary>
            ActionWithDecimalValue = 2,
        }

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Dropdown element for selecting available action
        /// </summary>
        [SerializeField]
        private Dropdown actionDropdown;

        /// <summary>
        /// Input field for entering the value
        /// </summary>
        [SerializeField]
        private InputField valueInput;

        /// <summary>
        /// Dropdown element for selecting value from a dropdown (for example available state)
        /// </summary>
        [SerializeField]
        private Dropdown valueDropdown;
#pragma warning restore 0649

        /// <summary>
        /// Current entry type, changes with selected action
        /// </summary>
        private EntryType entryType;

        /// <summary>
        /// Parent policy edit panel, which includes this entry
        /// </summary>
        private PolicyEditPanel parentPanel;

        /// <summary>
        /// Current policy value for this entry
        /// </summary>
        public string Policy { get; private set; }

        /// <summary>
        /// Currently selected action for this entry
        /// </summary>
        public string Action { get; private set; }

        /// <summary>
        /// Current value for this entry
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Is this policy entry valid as an control action
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Unity OnDisable method
        /// </summary>
        private void OnDisable()
        {
            valueInput.OnDeselect(new BaseEventData(EventSystem.current));
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        /// <param name="parent">Parent policy edit panel, which includes this entry</param>
        /// <param name="validActions">Actions that are valid for this entry</param>
        /// <param name="validStates">States that are valid for this entry</param>
        /// <param name="currentAction">Currently selected action of this entry</param>
        /// <param name="currentValue">Current value of this entry</param>
        public void Initialize(PolicyEditPanel parent, List<string> validActions, List<string> validStates,
            string currentAction, string currentValue)
        {
            parentPanel = parent;
            actionDropdown.ClearOptions();
            actionDropdown.AddOptions(validActions);
            if (!validActions.Contains(currentAction))
                currentAction = validActions[0];
            var actionId = validActions.IndexOf(currentAction);
            actionDropdown.SetValueWithoutNotify(actionId);
            ActionChanged(currentAction);
            valueDropdown.ClearOptions();
            valueDropdown.AddOptions(validStates);
            switch (currentAction)
            {
                case "state":
                    entryType = EntryType.State;
                    if (!validStates.Contains(currentValue))
                        currentValue = validStates[0];
                    var valueId = validStates.IndexOf(currentValue);
                    Value = currentValue;
                    valueDropdown.SetValueWithoutNotify(valueId);
                    break;
                default:
                    entryType = EntryType.Action;
                    Value = currentValue;
                    valueInput.SetTextWithoutNotify(currentValue);
                    break;
            }

            UpdatePolicy(Value);
        }

        /// <summary>
        /// Submits changed input field value
        /// </summary>
        public void SubmitChangedInputs()
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            if (valueInput.gameObject == selected)
                OnValueChange(valueInput.text);
        }

        /// <summary>
        /// Updates the policy value according to the passed value and currently selected action in the dropdown
        /// </summary>
        /// <param name="value">Current value that will be applied</param>
        private void UpdatePolicy(string value)
        {
            if (parentPanel.BoundControllable == null)
                return;
            Value = value;
            Policy = entryType != EntryType.State
                ? $"{Action}={Value}"
                : Value;
            IsValid = Validate(parentPanel.BoundControllable);
            parentPanel.UpdatePolicy();
        }

        /// <summary>
        /// Method called when the action dropdown changes selected option
        /// </summary>
        /// <param name="actionId">Selected action id</param>
        public void OnActionChange(int actionId)
        {
            var undo = new Action<Tuple<string, string>>(previous =>
            {
                var previousActionId = actionDropdown.options.FindIndex(option => option.text == previous.Item1);
                actionDropdown.SetValueWithoutNotify(previousActionId);
                ActionChanged(previous.Item1);
                switch (previous.Item1)
                {
                    case "state":
                        var value = valueDropdown.options.FindIndex(option => option.text == previous.Item2);
                        valueDropdown.SetValueWithoutNotify(value);
                        break;
                    default:
                        UpdatePolicy(previous.Item2);
                        break;
                }
            });
            var revertActionDropdown = new GenericUndo<Tuple<string, string>>(new Tuple<string, string>(Action, Value),
                "Undo policy action selection", undo);
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(revertActionDropdown);

            ActionChanged(actionDropdown.options[actionId].text);
        }

        /// <summary>
        /// Method that updates the entry according to selected action
        /// </summary>
        /// <param name="action">Selected action name</param>
        private void ActionChanged(string action)
        {
            Action = action;
            switch (action)
            {
                case "state":
                    entryType = EntryType.State;
                    valueDropdown.gameObject.SetActive(true);
                    valueInput.gameObject.SetActive(false);
                    Value = valueDropdown.options[valueDropdown.value].text;
                    break;
                case "wait":
                case "trigger":
                    entryType = EntryType.ActionWithDecimalValue;
                    valueDropdown.gameObject.SetActive(false);
                    valueInput.gameObject.SetActive(true);
                    valueInput.contentType = InputField.ContentType.DecimalNumber;
                    break;
                default:
                    entryType = EntryType.Action;
                    valueDropdown.gameObject.SetActive(false);
                    valueInput.gameObject.SetActive(true);
                    valueInput.contentType = InputField.ContentType.Alphanumeric;
                    break;
            }

            UpdatePolicy(Value);
        }

        /// <summary>
        /// Method called when the value input field changes
        /// </summary>
        /// <param name="value">Current input field value</param>
        public void OnValueChange(string value)
        {
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoInputField(
                valueInput, Value, UpdatePolicy));
            UpdatePolicy(valueInput.text);
        }

        /// <summary>
        /// Method called when the value dropdown changes selected option
        /// </summary>
        /// <param name="valueId">Selected value option id</param>
        public void OnValueDropdownChange(int valueId)
        {
            var undo = new Action<string>(previousValue =>
            {
                var id = valueDropdown.options.FindIndex(option => option.text == previousValue);
                valueDropdown.SetValueWithoutNotify(id);
                UpdatePolicy(previousValue);
            });
            var revertValueDropdown = new GenericUndo<string>(Value, "Undo policy value selection", undo);
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(revertValueDropdown);
            UpdatePolicy(valueDropdown.options[valueId].text);
        }

        /// <summary>
        /// Remove this entry from the parent panel
        /// </summary>
        public void Remove()
        {
            parentPanel.RemovePolicyEntryWithUndo(this);
        }

        /// <summary>
        /// Validate if current policy is valid control action for the given controllable
        /// </summary>
        /// <param name="controllable">Controllable that validates the policy</param>
        /// <returns>Is policy valid for the given controllable</returns>
        private bool Validate(IControllable controllable)
        {
            if (string.IsNullOrEmpty(Policy))
                return false;
            var controlActions = controllable.ParseControlPolicy(Policy, out _);
            return controlActions != null && controlActions.Count == 1;
        }
    }
}