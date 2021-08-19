/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Effectors.Effectors
{
    using System;
    using Elements;
    using Elements.Triggers;
    using Managers;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// Default panel that edits trigger effectors
    /// </summary>
    public class WaitTimeEditPanel : EffectorEditPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// UI InputField for the effector wait time value
        /// </summary>
        [SerializeField]
        private InputField valueInputField;
#pragma warning restore 0649
        
        /// <summary>
        /// Parent trigger panel
        /// </summary>
        private TriggerEditPanel parentPanel;
        
        /// <summary>
        /// Trigger effector type linked to this panel
        /// </summary>
        private WaitTimeEffector editedEffector;

        /// <inheritdoc/>
        public override Type EditedEffectorType => typeof(WaitTimeEffector);
        
        /// <summary>
        /// Unity OnDisable method
        /// </summary>
        private void OnDisable()
        {
            valueInputField.OnDeselect(new BaseEventData(EventSystem.current));
        }
        
        /// <inheritdoc/>
        public override void StartEditing(TriggerEditPanel triggerPanel, ScenarioTrigger trigger, TriggerEffector effector)
        {
            parentPanel = triggerPanel;
            editedEffector = (WaitTimeEffector) effector;
            valueInputField.text = editedEffector.Value.ToString("F");
        }

        /// <inheritdoc/>
        public override void FinishEditing()
        {
            if (this == null)
                return;
            var selected = EventSystem.current.currentSelectedGameObject;
            if (valueInputField.gameObject == selected)
                OnValueInputChange(valueInputField.text);
        }
        
        /// <inheritdoc/>
        public override void InitializeEffector(ScenarioTrigger trigger, TriggerEffector effector)
        {
            if (!(effector is WaitTimeEffector waitTimeEffector))
                throw new ArgumentException($"{GetType().Name} received effector of invalid type {effector.GetType().Name}.");
            waitTimeEffector.Value = 0.0f;
        }

        /// <summary>
        /// Removes linked effector from the trigger and returns it to the pool
        /// </summary>
        public void Remove()
        {
            parentPanel.RemoveEffector(editedEffector);
        }

        /// <summary>
        /// Sets the trigger effector value
        /// </summary>
        /// <param name="valueString">Value that should be set to the effector</param>
        public void OnValueInputChange(string valueString)
        {
            if (!float.TryParse(valueString, out var value)) return;
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoInputField(valueInputField,
                editedEffector.Value.ToString("F"), SetValue));
            SetValue(value);
        }

        /// <summary>
        /// Sets the trigger effector value
        /// </summary>
        /// <param name="valueString">Value that should be set to the effector</param>
        public void SetValue(string valueString)
        {
            if (!float.TryParse(valueString, out var value)) return;
            SetValue(value);
        }

        /// <summary>
        /// Sets the trigger effector value
        /// </summary>
        /// <param name="value">Value that should be set to the effector</param>
        public void SetValue(float value)
        {
            ScenarioManager.Instance.IsScenarioDirty = true;
            editedEffector.Value = value;
        }
    }
}