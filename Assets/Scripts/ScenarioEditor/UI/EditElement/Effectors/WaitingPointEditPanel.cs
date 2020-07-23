/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Effectors
{
    using System;
    using Agents.Triggers;
    using Elements;
    using Managers;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Default panel that edits trigger effectors
    /// </summary>
    public class WaitingPointEditPanel : EffectorEditPanel
    {
        /// <summary>
        /// Object name for the activation zone objects
        /// </summary>
        private static string ZoneObjectName = "WaitingPoint.ZoneVisualization";
        
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

        /// <inheritdoc/>
        public override void StartEditing(TriggerEditPanel triggerPanel, ScenarioTrigger trigger, TriggerEffector effector)
        {
            parentPanel = triggerPanel;
            editedTrigger = trigger;
            editedEffector = (WaitingPointEffector) effector;
            radiusInputField.text = editedEffector.PointRadius.ToString("F");
        }

        /// <inheritdoc/>
        public override void FinishEditing()
        {
            zoneVisualization.SetActive(false);
            zoneVisualization.transform.SetParent(transform);
        }

        /// <inheritdoc/>
        public override void EffectorAddedToTrigger(ScenarioTrigger trigger, TriggerEffector effector, bool initializeData)
        {
            var zone = trigger.GetOrAddEffectorObject(ZoneObjectName, zoneVisualization);
            if (!(effector is WaitingPointEffector waitingPointEffector))
                throw new ArgumentException($"{GetType().Name} received effector of invalid type {effector.GetType().Name}.");
            //If this effector should initialize data, put the activator point nearby the trigger
            if (initializeData)
                waitingPointEffector.ActivatorPoint = trigger.transform.position+zoneVisualization.transform.localPosition;
            zone.transform.position = waitingPointEffector.ActivatorPoint;
            zone.transform.localScale = Vector3.one * waitingPointEffector.PointRadius;
            zone.SetActive(true);
            var zoneComponent = zone.GetComponent<WaitingPointZone>();
            if (zoneComponent != null)
                zoneComponent.Setup(effector as WaitingPointEffector);
            else
                ScenarioManager.Instance.logPanel.EnqueueError(
                    $"Activation zone object used in the {GetType().Name} requires a {nameof(WaitingPointZone)} component.");
        }

        /// <inheritdoc/>
        public override void EffectorRemovedFromTrigger(ScenarioTrigger trigger, TriggerEffector effector)
        {
            trigger.RemoveEffectorObject(ZoneObjectName);
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
        /// <param name="radiusString">Radius that should be set to the effector</param>
        public void SetRadius(string radiusString)
        {
            if (float.TryParse(radiusString, out var radius))
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
            var zoneGameObject = editedTrigger.GetOrAddEffectorObject(ZoneObjectName, zoneVisualization);
            zoneGameObject.transform.localScale = Vector3.one * editedEffector.PointRadius;
            var zone = zoneGameObject.GetComponent<WaitingPointZone>();
            if (zone!=null)
                zone.Refresh();
        }
    }
}