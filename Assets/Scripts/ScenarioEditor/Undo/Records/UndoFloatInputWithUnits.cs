/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo.Records
{
    using Elements;
    using Managers;
    using UI.Utilities;
    using Undo;

    /// <summary>
    /// Record that undoes setting a value to the float input with units
    /// </summary>
    public class UndoFloatInputWithUnits : UndoRecord
    {
        /// <summary>
        /// Float input with units that has been changed
        /// </summary>
        private readonly FloatInputWithUnits floatInputWithUnits;

        /// <summary>
        /// Previous value in the changed input field
        /// </summary>
        private readonly float previousValue;

        /// <summary>
        /// Scenario element which indicates the edit input context
        /// </summary>
        private readonly ScenarioElement undoContext;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="floatInput">Float input with units that has been changed</param>
        /// <param name="previousValue">Previous value in the changed input field</param>
        /// <param name="undoContext">Scenario element which indicates the edit input context</param>
        public UndoFloatInputWithUnits(FloatInputWithUnits floatInput, float previousValue, ScenarioElement undoContext)
        {
            floatInputWithUnits = floatInput;
            this.previousValue = previousValue;
            this.undoContext = undoContext;
        }

        /// <inheritdoc/>
        public override void Undo()
        {
            floatInputWithUnits.ExternalValueChange(previousValue, undoContext, true);
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback change in an input field.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
        }
    }
}