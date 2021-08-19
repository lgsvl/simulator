/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo.Records
{
    using Elements;
    using Elements.Triggers;
    using Managers;

    /// <summary>
    /// Record that undoes adding an effector to the trigger
    /// </summary>
    public class UndoAddEffector : UndoRecord
    {
        /// <summary>
        /// Scenario trigger that contains added effector
        /// </summary>
        private readonly ScenarioTrigger trigger;

        /// <summary>
        /// Effector that was added to the trigger
        /// </summary>
        private readonly TriggerEffector effector;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="trigger">Scenario trigger that contains added effector</param>
        /// <param name="effector">Effector that was added to the trigger</param>
        public UndoAddEffector(ScenarioTrigger trigger, TriggerEffector effector)
        {
            this.trigger = trigger;
            this.effector = effector;
            trigger.TryGetEffector(effector)?.Show();
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            trigger.Trigger.RemoveEffector(effector);
            trigger.TryGetEffector(effector)?.Hide();
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback adding an effector.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {

        }
    }
}