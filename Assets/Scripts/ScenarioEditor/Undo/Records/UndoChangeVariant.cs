/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo.Records
{
    using Agents;
    using Elements;
    using Managers;

    /// <summary>
    /// Record that undoes changing the agent variant
    /// </summary>
    public class UndoChangeVariant : UndoRecord
    {
        /// <summary>
        /// Scenario element which variant was changed
        /// </summary>
        private ScenarioElementWithVariant scenarioElementWithVariant;

        /// <summary>
        /// Previous source variant
        /// </summary>
        private SourceVariant sourceVariant;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioElementWithVariant">Scenario element which variant was changed</param>
        /// <param name="sourceVariant">Previous source variant</param>
        public UndoChangeVariant(ScenarioElementWithVariant scenarioElementWithVariant, SourceVariant sourceVariant)
        {
            this.scenarioElementWithVariant = scenarioElementWithVariant;
            this.sourceVariant = sourceVariant;
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            scenarioElementWithVariant.ChangeVariant(sourceVariant, false);
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback changed agent variant.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
