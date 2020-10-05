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
    /// Record that undoes setting a value to the toggle
    /// </summary>
    public class UndoToggle : UndoRecord
    {
        /// <summary>
        /// Toggle that has been changed
        /// </summary>
        private readonly Toggle toggle;
        
        /// <summary>
        /// Previous value in the changed toggle
        /// </summary>
        private readonly bool previousValue;
        
        /// <summary>
        /// Event invoked when the undo is applied
        /// </summary>
        private readonly Action<bool> valueApplyCallback;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="toggle">Toggle that has been changed</param>
        /// <param name="previousValue">Previous value in the changed toggle</param>
        /// <param name="valueApplyCallback">Event invoked when the undo is applied</param>
        public UndoToggle(Toggle toggle, bool previousValue, Action<bool> valueApplyCallback)
        {
            this.toggle = toggle;
            this.previousValue = previousValue;
            this.valueApplyCallback = valueApplyCallback;
        }

        /// <inheritdoc/>
        public override void Undo()
        {
            toggle.SetIsOnWithoutNotify(previousValue);
            valueApplyCallback?.Invoke(previousValue);
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback change in a toggle.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
