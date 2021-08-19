/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Effectors.Effectors
{
    using System;
    using Agents.Triggers;
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
    public class WaitingPointEditPanel : EffectorEditPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Game object that visualizes the activation point and the radius
        /// </summary>
        public GameObject zoneVisualization;

        /// <summary>
        /// UI InputField for the effector radius
        /// </summary>
        [SerializeField]
        private InputField radiusInputField;
#pragma warning restore 0649

        /// <summary>
        /// Parent trigger panel
        /// </summary>
        private TriggerEditPanel parentPanel;

        /// <summary>
        /// Trigger being edited
        /// </summary>
        private ScenarioTrigger editedTrigger;

        /// <summary>
        /// Trigger effector being edited
        /// </summary>
        private WaitingPointEffector editedEffector;

        /// <inheritdoc/>
        public override Type EditedEffectorType => typeof(WaitingPointEffector);

        /// <summary>
        /// Unity OnDisable method
        /// </summary>
        private void OnDisable()
        {
            radiusInputField.OnDeselect(new BaseEventData(EventSystem.current));
        }

        /// <inheritdoc/>
        public override void StartEditing(TriggerEditPanel triggerPanel, ScenarioTrigger trigger,
            TriggerEffector effector)
        {
            parentPanel = triggerPanel;
            editedTrigger = trigger;
            editedEffector = (WaitingPointEffector) effector;
            radiusInputField.text = editedEffector.PointRadius.ToString("F");
        }

        /// <inheritdoc/>
        public override void FinishEditing()
        {
            if (this == null)
                return;
            var selected = EventSystem.current.currentSelectedGameObject;
            if (radiusInputField.gameObject == selected)
                OnRadiusInputChange(radiusInputField.text);
            if (zoneVisualization == null)
                return;
            zoneVisualization.SetActive(false);
            zoneVisualization.transform.SetParent(transform);
        }

        /// <inheritdoc/>
        public override void InitializeEffector(ScenarioTrigger trigger, TriggerEffector effector)
        {
            if (!(effector is WaitingPointEffector waitingPointEffector))
                throw new ArgumentException(
                    $"{GetType().Name} received effector of invalid type {effector.GetType().Name}.");
            //Put the activator point nearby the trigger
            waitingPointEffector.ActivatorPoint =
                trigger.transform.position + zoneVisualization.transform.localPosition;
            waitingPointEffector.PointRadius = 2.0f;
        }

        /// <inheritdoc/>
        public override void EffectorAddedToTrigger(ScenarioTrigger trigger, TriggerEffector effector)
        {
            if (!(effector is WaitingPointEffector waitingPointEffector))
                throw new ArgumentException(
                    $"{GetType().Name} received effector of invalid type {effector.GetType().Name}.");
            var zone = trigger.GetEffectorObject(effector, zoneVisualization.name);
            if (zone != null) return;
            zone = trigger.AddEffectorObject(effector, zoneVisualization.name, zoneVisualization);
            zone.transform.position = waitingPointEffector.ActivatorPoint;
            zone.transform.localScale = Vector3.one * waitingPointEffector.PointRadius;
            zone.gameObject.SetActive(true);
            var zoneComponent = zone.GetComponent<WaitingPointZone>();
            zoneComponent.Refresh();
        }

        /// <inheritdoc/>
        public override void EffectorRemovedFromTrigger(ScenarioTrigger trigger, TriggerEffector effector)
        {
            trigger.RemoveEffectorObject(effector, zoneVisualization.name);
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
        /// <param name="radiusString">Radius that should be set to the effector</param>
        public void OnRadiusInputChange(string radiusString)
        {
            if (!float.TryParse(radiusString, out var radius)) return;
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoInputField(
                radiusInputField, editedEffector.PointRadius.ToString("F"), SetRadius));
            SetRadius(radius);
        }

        /// <summary>
        /// Sets the trigger effector max distance
        /// </summary>
        /// <param name="radiusString">Radius that should be set to the effector</param>
        private void SetRadius(string radiusString)
        {
            if (!float.TryParse(radiusString, out var radius)) return;
            SetRadius(radius);
        }

        /// <summary>
        /// Sets the trigger effector max distance
        /// </summary>
        /// <param name="radius">Radius that should be set to the effector</param>
        public void SetRadius(float radius)
        {
            ScenarioManager.Instance.IsScenarioDirty = true;
            editedEffector.PointRadius = radius;
            var zoneGameObject = editedTrigger.GetEffectorObject(editedEffector, zoneVisualization.name);
            zoneGameObject.transform.localScale = Vector3.one * editedEffector.PointRadius;
            var zone = zoneGameObject.GetComponent<WaitingPointZone>();
            if (zone != null)
                zone.Refresh();
        }
    }
}