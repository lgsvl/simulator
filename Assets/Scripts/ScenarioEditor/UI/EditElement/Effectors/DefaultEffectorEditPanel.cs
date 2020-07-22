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
    using UnityEngine;
    using UnityEngine.UI;
    using Utilities;

    /// <summary>
    /// Default panel that edits trigger effectors
    /// </summary>
    public class DefaultEffectorEditPanel : EffectorEditPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// UI Text with the effector name
        /// </summary>
        [SerializeField]
        private Text title;
#pragma warning restore 0649
        
        /// <summary>
        /// Parent trigger panel
        /// </summary>
        private TriggerEditPanel parentPanel;
        
        /// <summary>
        /// Trigger effector type linked to this panel
        /// </summary>
        private TriggerEffector editedEffector;

        /// <inheritdoc/>
        public override Type EditedEffectorType => typeof(TriggerEffector);
        
        /// <inheritdoc/>
        public override void StartEditing(TriggerEditPanel triggerPanel, ScenarioTrigger trigger, TriggerEffector effector)
        {
            parentPanel = triggerPanel;
            editedEffector = effector;
            title.text = editedEffector.TypeName;
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
            parentPanel.RemoveEffector(editedEffector);
        }
    }
}