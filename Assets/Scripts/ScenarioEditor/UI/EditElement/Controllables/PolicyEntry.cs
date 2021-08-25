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
        /// Parent policy edit panel, which includes this entry
        /// </summary>
        private PolicyEditPanel parentPanel;

        /// <summary>
        /// Current policy value for this entry
        /// </summary>
        public ControlAction Policy { get; private set; }

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
        /// Is this policy entry initialized
        /// </summary>
        public bool IsInitialized { get; private set; }

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
                    if (!validStates.Contains(currentValue))
                        currentValue = validStates[0];
                    var valueId = validStates.IndexOf(currentValue);
                    Value = currentValue;
                    valueDropdown.SetValueWithoutNotify(valueId);
                    break;
                default:
                    Value = currentValue;
                    valueInput.SetTextWithoutNotify(currentValue);
                    break;
            }

            UpdatePolicy(Value);
            IsInitialized = true;
        }

        /// <summary>
        /// Deinitalizes the policy entry
        /// </summary>
        public void Deinitialize()
        {
            parentPanel = null;
            IsInitialized = false;
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
            Policy = new ControlAction
            {
                Action = Action,
                Value = value
            };
            IsValid = Validate();
            if (IsInitialized)
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
            Text placeholder;
            switch (action)
            {
                case "state":
                    valueDropdown.gameObject.SetActive(true);
                    valueInput.gameObject.SetActive(false);
                    Value = valueDropdown.options[valueDropdown.value].text;
                    break;
                case "wait":
                case "trigger":
                    valueDropdown.gameObject.SetActive(false);
                    placeholder = valueInput.placeholder as Text;
                    if (placeholder != null)
                        placeholder.text = 0.0f.ToString("F2");
                    valueInput.contentType = InputField.ContentType.DecimalNumber;
                    valueInput.gameObject.SetActive(true);
                    break;
                case "loop":
                    valueDropdown.gameObject.SetActive(false);
                    valueInput.gameObject.SetActive(false);
                    valueInput.SetTextWithoutNotify("");
                    break;
                default:
                    valueDropdown.gameObject.SetActive(false);
                    placeholder = valueInput.placeholder as Text;
                    if (placeholder != null)
                        placeholder.text = "Enter text...";
                    valueInput.contentType = InputField.ContentType.Alphanumeric;
                    valueInput.gameObject.SetActive(true);
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
        /// <returns>Is policy valid for the given controllable</returns>
        private bool Validate()
        {
            return !string.IsNullOrEmpty(Policy.Action);
        }
    }
}