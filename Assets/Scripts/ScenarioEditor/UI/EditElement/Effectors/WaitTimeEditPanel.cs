/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Effectors
{
    using System;
    using Elements;
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;
    using Utilities;

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
        public void SetValue(string valueString)
        {
            if (float.TryParse(valueString, out var value))
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