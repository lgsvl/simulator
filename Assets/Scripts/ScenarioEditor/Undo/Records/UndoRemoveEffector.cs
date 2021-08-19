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
    /// Record that undoes removing an effector to the trigger
    /// </summary>
    public class UndoRemoveEffector : UndoRecord
    {
        /// <summary>
        /// Scenario trigger from which effector was removed
        /// </summary>
        private readonly ScenarioTrigger trigger;

        /// <summary>
        /// Effector that was removed from the trigger
        /// </summary>
        private readonly TriggerEffector effector;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="trigger">Scenario trigger from which effector was removed</param>
        /// <param name="effector">Effector that was removed from the trigger</param>
        public UndoRemoveEffector(ScenarioTrigger trigger, TriggerEffector effector)
        {
            this.trigger = trigger;
            this.effector = effector;
            trigger.TryGetEffector(effector)?.Hide();
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            trigger.Trigger.AddEffector(effector);
            trigger.TryGetEffector(effector)?.Show();
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback removing an effector.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            trigger.TryGetEffector(effector)?.Deinitialize();
        }
    }
}