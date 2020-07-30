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
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Default panel that edits trigger effectors
    /// </summary>
    public class WaitForDistanceEditPanel : EffectorEditPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// UI InputField for the effector wait time value
        /// </summary>
        [SerializeField]
        private InputField maxDistanceInputField;
#pragma warning restore 0649
        
        /// <summary>
        /// Parent trigger panel
        /// </summary>
        private TriggerEditPanel parentPanel;
        
        /// <summary>
        /// Trigger effector type linked to this panel
        /// </summary>
        private WaitForDistanceEffector editedEffector;

        /// <inheritdoc/>
        public override Type EditedEffectorType => typeof(WaitForDistanceEffector);
        
        /// <inheritdoc/>
        public override void StartEditing(TriggerEditPanel triggerPanel, ScenarioTrigger trigger, TriggerEffector effector)
        {
            parentPanel = triggerPanel;
            editedEffector = (WaitForDistanceEffector) effector;
            maxDistanceInputField.text = editedEffector.MaxDistance.ToString("F");
        }

        /// <summary>
        /// Removes linked effector from the trigger and returns it to the pool
        /// </summary>
        public void Remove()
        {
            parentPanel.RemoveEffector(editedEffector);
        }
        
        /// <summary>
        /// Sets the trigger effector max distance
        /// </summary>
        /// <param name="maxDistanceString">Max distance that should be set to the effector</param>
        public void SetMaxDistance(string maxDistanceString)
        {
            if (float.TryParse(maxDistanceString, out var maxDistance))
                SetMaxDistance(maxDistance);
        }


        /// <summary>
        /// Sets the trigger effector max distance
        /// </summary>
        /// <param name="maxDistance">Max distance that should be set to the effector</param>
        public void SetMaxDistance(float maxDistance)
        {
            ScenarioManager.Instance.IsScenarioDirty = true;
            editedEffector.MaxDistance = maxDistance;
        }
    }
}