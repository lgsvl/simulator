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
    using UnityEngine;

    /// <summary>
    /// Record that undoes copying a scenario trigger
    /// </summary>
    public class UndoTriggerCopy : UndoRecord
    {
        /// <summary>
        /// Scenario trigger that was changed
        /// </summary>
        private readonly ScenarioTrigger targetTrigger;

        /// <summary>
        /// Trigger copy that holds previous properties for undo action
        /// </summary>
        private readonly ScenarioTrigger backupTrigger;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="targetTrigger">Scenario trigger that was changed</param>
        public UndoTriggerCopy(ScenarioTrigger targetTrigger)
        {
            this.targetTrigger = targetTrigger;
            var gameObject =
                Object.Instantiate(targetTrigger.gameObject, ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().transform);
            backupTrigger = gameObject.GetComponent<ScenarioTrigger>();
            backupTrigger.CopyProperties(targetTrigger);
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            targetTrigger.CopyProperties(backupTrigger);
            Object.Destroy(backupTrigger);
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback copying a trigger.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Object.Destroy(backupTrigger);
        }
    }
}
