/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo.Records
{
    using System;
    using Managers;
    using Undo;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// Record that undoes setting a value to the input field
    /// </summary>
    public class UndoInputField : UndoRecord
    {
        /// <summary>
        /// Input field that has been changed
        /// </summary>
        private readonly InputField inputField;
        
        /// <summary>
        /// Previous value in the changed input field
        /// </summary>
        private readonly string previousValue;

        /// <summary>
        /// Event invoked when the undo is applied
        /// </summary>
        private readonly Action<string> valueApplyCallback;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="input">Input field that has been changed</param>
        /// <param name="previousValue">Previous value in the changed input field</param>
        /// <param name="valueApplyCallback">Event invoked when the undo is applied</param>
        public UndoInputField(InputField input, string previousValue, Action<string> valueApplyCallback)
        {
            inputField = input;
            this.previousValue = previousValue;
            this.valueApplyCallback = valueApplyCallback;
        }

        /// <inheritdoc/>
        public override void Undo()
        {
            inputField.text = previousValue;
            valueApplyCallback?.Invoke(previousValue);
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback change in an input field.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
