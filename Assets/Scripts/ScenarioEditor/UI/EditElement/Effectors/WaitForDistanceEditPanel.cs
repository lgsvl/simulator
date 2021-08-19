/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Effectors.Effectors
{
    using System;
    using Elements;
    using Elements.Triggers;
    using Managers;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.EventSystems;
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

        /// <summary>
        /// Unity OnDisable method
        /// </summary>
        private void OnDisable()
        {
            maxDistanceInputField.OnDeselect(new BaseEventData(EventSystem.current));
        }

        /// <inheritdoc/>
        public override void StartEditing(TriggerEditPanel triggerPanel, ScenarioTrigger trigger,
            TriggerEffector effector)
        {
            parentPanel = triggerPanel;
            editedEffector = (WaitForDistanceEffector) effector;
            maxDistanceInputField.text = editedEffector.MaxDistance.ToString("F");
        }

        /// <inheritdoc/>
        public override void FinishEditing()
        {
            if (this == null)
                return;
            var selected = EventSystem.current.currentSelectedGameObject;
            if (maxDistanceInputField.gameObject == selected)
                OnMaxDistanceInputChange(maxDistanceInputField.text);
        }

        /// <inheritdoc/>
        public override void InitializeEffector(ScenarioTrigger trigger, TriggerEffector effector)
        {
            if (!(effector is WaitForDistanceEffector waitForDistanceEffector))
                throw new ArgumentException(
                    $"{GetType().Name} received effector of invalid type {effector.GetType().Name}.");
            waitForDistanceEffector.MaxDistance = 5.0f;
        }

        /// <summary>
        /// Removes linked effector from the trigger and returns it to the pool
        /// </summary>
        public void Remove()
        {
            parentPanel.RemoveEffector(editedEffector);
        }

        /// <summary>
        /// Sets the trigger effector max distance and registers an undo record
        /// </summary>
        /// <param name="maxDistanceString">Max distance that should be set to the effector</param>
        public void OnMaxDistanceInputChange(string maxDistanceString)
        {
            if (!float.TryParse(maxDistanceString, out var maxDistance)) return;
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoInputField(
                maxDistanceInputField, editedEffector.MaxDistance.ToString("F"), SetMaxDistance));
            SetMaxDistance(maxDistance);
        }

        /// <summary>
        /// Sets the trigger effector max distance
        /// </summary>
        /// <param name="maxDistanceString">Max distance that should be set to the effector</param>
        /// <returns>Was the max distance changed</returns>
        private void SetMaxDistance(string maxDistanceString)
        {
            if (!float.TryParse(maxDistanceString, out var maxDistance)) return;
            SetMaxDistance(maxDistance);
        }


        /// <summary>
        /// Sets the trigger effector max distance
        /// </summary>
        /// <param name="maxDistance">Max distance that should be set to the effector</param>
        public void SetMaxDistance(float maxDistance)
        {
            editedEffector.MaxDistance = maxDistance;
            ScenarioManager.Instance.IsScenarioDirty = true;
        }
    }
}