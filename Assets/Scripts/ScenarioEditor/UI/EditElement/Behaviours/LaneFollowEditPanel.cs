/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Behaviours
{
    using System;
    using Data.Serializer;
    using Elements;
    using Elements.Agents;
    using Managers;
    using Undo;
    using Undo.Records;
    using UnityEngine;
    using UnityEngine.UI;
    using Utilities;

    /// <inheritdoc/>
    public class LaneFollowEditPanel : BehaviourEditPanel
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Toggle for is lane change variable
        /// </summary>
        [SerializeField]
        private Toggle isLaneChangeToggle;

        /// <summary>
        /// Input field with units for max speed
        /// </summary>
        [SerializeField]
        private FloatInputWithUnits maxSpeedInput;
#pragma warning restore 0649

        /// <inheritdoc/>
        protected override string EditedBehaviour { get; } = nameof(NPCLaneFollowBehaviour);

        /// <summary>
        /// Is lane change current value
        /// </summary>
        private bool currentIsLaneChange;

        /// <inheritdoc/>
        protected override void OnShown()
        {
            base.OnShown();

            //Setup the isLaneChange value
            if (behaviourExtension.BehaviourParameters.HasKey("isLaneChange"))
            {
                currentIsLaneChange = (bool) behaviourExtension.BehaviourParameters["isLaneChange"];
            }
            else
            {
                currentIsLaneChange = false;
                behaviourExtension.BehaviourParameters["isLaneChange"] = currentIsLaneChange;
            }

            isLaneChangeToggle.SetIsOnWithoutNotify(currentIsLaneChange);
            //Setup the maxSpeed value and input
            maxSpeedInput.Initialize(ScenarioPersistenceKeys.SpeedUnitKey, MaxSpeedApply, selectedAgent);
            float maxSpeed;
            if (behaviourExtension.BehaviourParameters.HasKey("maxSpeed"))
            {
                maxSpeed = (float) behaviourExtension.BehaviourParameters["maxSpeed"];
            }
            else
            {
                maxSpeed = 0.0f;
                behaviourExtension.BehaviourParameters["maxSpeed"] = maxSpeed;
            }

            maxSpeedInput.ExternalValueChange(maxSpeed, selectedAgent, false);
        }

        /// <inheritdoc/>
        protected override void OnHidden()
        {
            base.OnHidden();
            maxSpeedInput.Deinitialize();
        }

        /// <summary>
        /// Method invoked when the is lane change variable is changed by the toggle
        /// </summary>
        /// <param name="isLaneChange">Is lane change</param>
        public void OnIsLaneChangeToggleChange(bool isLaneChange)
        {
            if (currentIsLaneChange == isLaneChange)
                return;
            var extension = behaviourExtension;
            var undoCallback = new Action<bool>((undoValue) =>
            {
                IsLaneChangeApply(extension, undoValue);
            });
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new GenericUndo<bool>(
                currentIsLaneChange, "Undo toggling is lane change value", undoCallback));
            IsLaneChangeApply(behaviourExtension, isLaneChange);
        }

        /// <summary>
        /// Method invoked when the is lane change variable is changed by the toggle
        /// </summary>
        /// <param name="extension">Agent behaviour which is lane change changes</param>
        /// <param name="isLaneChange">Is lane change</param>
        private void IsLaneChangeApply(AgentBehaviour extension, bool isLaneChange)
        {
            if (currentIsLaneChange == isLaneChange)
                return;
            if (extension.BehaviourParameters.HasKey("isLaneChange"))
                extension.BehaviourParameters["isLaneChange"] = isLaneChange;
            else
                extension.BehaviourParameters.Add("isLaneChange", isLaneChange);
            currentIsLaneChange = isLaneChange;
            var isSelected = extension == behaviourExtension;
            if (isSelected)
                isLaneChangeToggle.SetIsOnWithoutNotify(isLaneChange);
        }

        /// <summary>
        /// Method invoked when the max speed is changed by the input field
        /// </summary>
        /// <param name="changedElement">Scenario element which speed has been changed</param>
        /// <param name="maxSpeed">New max speed applied</param>
        private void MaxSpeedApply(ScenarioElement changedElement, float maxSpeed)
        {
            if (!(changedElement is ScenarioAgent changedAgent))
                return;
            var behaviour = changedAgent.GetExtension<AgentBehaviour>();
            if (behaviour == null)
                return;
            if (behaviour.BehaviourParameters.HasKey("maxSpeed"))
                behaviour.BehaviourParameters["maxSpeed"] = maxSpeed;
            else
                behaviour.BehaviourParameters.Add("maxSpeed", maxSpeed);
        }
    }
}