/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo.Records
{
    using Undo;
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
        /// Constructor
        /// </summary>
        /// <param name="input">Input field that has been changed</param>
        /// <param name="previousValue">Previous value in the changed input field</param>
        public UndoInputField(InputField input, string previousValue)
        {
            inputField = input;
            this.previousValue = previousValue;
        }

        /// <inheritdoc/>
        public override void Undo()
        {
            inputField.text = previousValue;
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
