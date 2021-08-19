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
    using Elements.Triggers;
    using UnityEngine;

    /// <summary>
    /// Component indicating that this object will edit selected effector type
    /// </summary>
    public abstract class EffectorEditPanel : MonoBehaviour
    {
        /// <summary>
        /// Type of the effector that this object edits
        /// </summary>
        public abstract Type EditedEffectorType { get; }

        /// <summary>
        /// Should multiple instances of this effector be allowed in one trigger
        /// </summary>
        public virtual bool AllowMany { get; } = false;

        /// <summary>
        /// Initializes panel for editing the selected effector
        /// </summary>
        /// <param name="triggerPanel">Parent trigger panel</param>
        /// <param name="trigger">Trigger being edited</param>
        /// <param name="effector">Trigger effector being edited</param>
        public abstract void StartEditing(TriggerEditPanel triggerPanel, ScenarioTrigger trigger, TriggerEffector effector);

        /// <summary>
        /// Deinitializes panel after editing effector
        /// </summary>
        public virtual void FinishEditing()
        {
            
        }

        /// <summary>
        /// Initializes effector with the default data
        /// </summary>
        /// <param name="trigger">Trigger that gained the new effector</param>
        /// <param name="effector">New effector added to the trigger</param>
        public abstract void InitializeEffector(ScenarioTrigger trigger, TriggerEffector effector);

        /// <summary>
        /// Notifies edit panel that the effector was added to the trigger
        /// </summary>
        /// <param name="trigger">Trigger that gained the new effector</param>
        /// <param name="effector">New effector added to the trigger</param>
        public virtual void EffectorAddedToTrigger(ScenarioTrigger trigger, TriggerEffector effector)
        {
            
        }

        /// <summary>
        /// Notifies edit panel that the effector was removed from the trigger
        /// </summary>
        /// <param name="trigger">Trigger that lost the effector</param>
        /// <param name="effector">Effector removed from the trigger</param>
        public virtual void EffectorRemovedFromTrigger(ScenarioTrigger trigger, TriggerEffector effector)
        {
            
        }
    }
}