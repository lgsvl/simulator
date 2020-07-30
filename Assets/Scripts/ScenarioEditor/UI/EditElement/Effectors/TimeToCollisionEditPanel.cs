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

    /// <summary>
    /// Component indicating that this object will edit selected effector type
    /// </summary>
    public class TimeToCollisionEditPanel : EffectorEditPanel
    {
        /// <summary>
        /// Parent trigger panel
        /// </summary>
        private TriggerEditPanel parentPanel;
        
        /// <summary>
        /// Trigger effector type linked to this panel
        /// </summary>
        private TriggerEffector editedTrigger;
        
        /// <summary>
        /// Type of the effector that this object edits
        /// </summary>
        public override Type EditedEffectorType => typeof(TimeToCollisionEffector);

        /// <inheritdoc/>
        public override void StartEditing(TriggerEditPanel triggerPanel, ScenarioTrigger trigger, TriggerEffector effector)
        {
            parentPanel = triggerPanel;
            editedTrigger = effector;
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
            parentPanel.RemoveEffector(editedTrigger);
        }
    }
}