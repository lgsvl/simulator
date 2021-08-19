/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Effectors.Effectors
{
    using System;
    using System.Collections.Generic;
    using Agents;
    using Controllable;
    using Controllables;
    using Elements;
    using Elements.Triggers;
    using Input;
    using Managers;
    using ScenarioEditor.Controllables;
    using UnityEngine;

    /// <summary>
    /// Panel that edits control trigger effectors
    /// </summary>
    public class ControlTriggerEditPanel : EffectorEditPanel, IMarkElementsHandler
    {
        /// <summary>
        /// The position offset that will be applied to the line renderer of waypoints
        /// </summary>
        private static readonly Vector3 LineRendererPositionOffset = new Vector3(0.0f, 0.2f, 0.0f);

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Panel for editing the selected controllable policy
        /// </summary>
        [SerializeField]
        private PolicyEditPanel policyEditPanel;

        /// <summary>
        /// Line renderer showing connected controllables
        /// </summary>
        [SerializeField]
        private LineRenderer lineRenderer;
#pragma warning restore 0649

        /// <summary>
        /// Parent trigger panel
        /// </summary>
        private TriggerEditPanel parentPanel;

        /// <summary>
        /// Scenario trigger that is currently edited
        /// </summary>
        private ScenarioTrigger editedTrigger;

        /// <summary>
        /// Trigger effector type linked to this panel
        /// </summary>
        private ControlTriggerEffector editedEffector;

        /// <inheritdoc/>
        public override Type EditedEffectorType => typeof(ControlTriggerEffector);

        /// <inheritdoc/>
        public override bool AllowMany { get; } = true;

        /// <summary>
        /// Controllables linked to this control trigger
        /// </summary>
        private readonly List<ScenarioControllable> linkedControllables = new List<ScenarioControllable>();

        /// <inheritdoc/>
        public override void StartEditing(TriggerEditPanel triggerPanel, ScenarioTrigger trigger,
            TriggerEffector effector)
        {
            parentPanel = triggerPanel;
            editedEffector = (ControlTriggerEffector) effector;
            policyEditPanel.PolicyUpdated += PolicyEditPanelOnPolicyUpdated;
            editedTrigger = trigger;
            editedTrigger.Moved += OnTriggerMoved;
            var manager = ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>();

            //Link to scenario controllables
            for (var i = 0; i < editedEffector.ControllablesUIDs.Count; i++)
            {
                var controllableUID = editedEffector.ControllablesUIDs[i];
                var controllableAdded = false;
                for (var index = 0; index < manager.Controllables.Count; index++)
                {
                    var controllable = manager.Controllables[index];
                    if (controllable.Uid != controllableUID) continue;
                    linkedControllables.Add(controllable);
                    controllableAdded = true;
                    break;
                }

                if (!controllableAdded)
                    ScenarioManager.Instance.logPanel.EnqueueWarning(
                        $"Could not find controllable with uid: {controllableUID} in the scenario, which edited {nameof(ControlTriggerEffector)} was using.");
            }

            //Setup LineRenderer
            lineRenderer.positionCount = linkedControllables.Count + 1;
            lineRenderer.SetPosition(0, trigger.transform.position + LineRendererPositionOffset);
            for (var i = 0; i < linkedControllables.Count; i++)
            {
                var controllable = linkedControllables[i];
                lineRenderer.SetPosition(i + 1, controllable.transform.position + LineRendererPositionOffset);
            }

            policyEditPanel.Setup(linkedControllables.Count > 0 ? linkedControllables[0].Variant.controllable : null,
                editedEffector.ControlPolicy);
        }

        /// <inheritdoc/>
        public override void FinishEditing()
        {
            if (this == null)
                return;
            editedTrigger.Moved -= OnTriggerMoved;
            editedTrigger = null;
            policyEditPanel.PolicyUpdated -= PolicyEditPanelOnPolicyUpdated;
            linkedControllables.Clear();
            policyEditPanel.Setup(null, null);
        }

        /// <summary>
        /// Method invoked when the edited trigger is moved
        /// </summary>
        /// <param name="changedElement">Changed scenario element</param>
        private void OnTriggerMoved(ScenarioElement changedElement)
        {
            lineRenderer.SetPosition(0, editedTrigger.transform.position + LineRendererPositionOffset);
        }

        /// <summary>
        /// Method invoked when the policy was changed
        /// </summary>
        /// <param name="policy">Updated policy</param>
        private void PolicyEditPanelOnPolicyUpdated(List<ControlAction> policy)
        {
            editedEffector.ControlPolicy = policy;
        }

        /// <inheritdoc/>
        public override void InitializeEffector(ScenarioTrigger trigger, TriggerEffector effector)
        {
            if (!(effector is ControlTriggerEffector triggerEffector))
                throw new ArgumentException(
                    $"{GetType().Name} received effector of invalid type {effector.GetType().Name}.");
            triggerEffector.ControllablesUIDs.Clear();
            triggerEffector.ControlPolicy = new List<ControlAction>();
            policyEditPanel.Setup(null, triggerEffector.ControlPolicy);
        }

        /// <summary>
        /// Removes linked effector from the trigger and returns it to the pool
        /// </summary>
        public void Remove()
        {
            parentPanel.RemoveEffector(editedEffector);
        }

        /// <summary>
        /// Starts marking elements to link controlled controllables
        /// </summary>
        public void StartMarkingElements()
        {
            ScenarioManager.Instance.GetExtension<InputManager>().StartMarkingElements(this);
        }

        /// <inheritdoc/>
        void IMarkElementsHandler.MarkingStarted()
        {
        }

        /// <inheritdoc/>
        void IMarkElementsHandler.MarkElement(ScenarioElement element)
        {
            if (!(element is ScenarioControllable scenarioControllable)) return;
            if (linkedControllables.Contains(scenarioControllable))
            {
                var index = linkedControllables.IndexOf(scenarioControllable);
                linkedControllables.RemoveAt(index);
                lineRenderer.positionCount -= 1;
                for (var i = index + 1; i < lineRenderer.positionCount; i++)
                    lineRenderer.SetPosition(i,
                        linkedControllables[i - 1].transform.position + LineRendererPositionOffset);
                editedEffector.ControllablesUIDs.Remove(scenarioControllable.Uid);
                if (linkedControllables.Count == 0)
                    policyEditPanel.Setup(null, null);
            }
            else
            {
                var controllable = scenarioControllable.Variant.controllable;
                if (linkedControllables.Count > 0 &&
                    controllable.GetType() != linkedControllables[0].Variant.controllable.GetType())
                {
                    ScenarioManager.Instance.logPanel.EnqueueWarning(
                        "Cannot link two different controllable variants to one control trigger.");
                    return;
                }

                linkedControllables.Add(scenarioControllable);
                var positionCount = lineRenderer.positionCount;
                lineRenderer.positionCount = positionCount + 1;
                lineRenderer.SetPosition(positionCount, scenarioControllable.transform.position);
                editedEffector.ControllablesUIDs.Add(scenarioControllable.Uid);
                if (linkedControllables.Count != 1) return;
                var policy = new List<ControlAction>(controllable.DefaultControlPolicy);
                policyEditPanel.Setup(controllable, policy);
            }
        }

        /// <inheritdoc/>
        void IMarkElementsHandler.MarkingCancelled()
        {
        }
    }
}