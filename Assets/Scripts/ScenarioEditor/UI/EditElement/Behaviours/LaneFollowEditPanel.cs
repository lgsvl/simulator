/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.EditElement.Behaviours
{
    using Data.Serializer;
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
            currentIsLaneChange = selectedAgent.BehaviourParameters.HasKey("isLaneChange") &&
                               (bool) selectedAgent.BehaviourParameters["isLaneChange"];
            isLaneChangeToggle.SetIsOnWithoutNotify(currentIsLaneChange);
            maxSpeedInput.Initialize(ScenarioPersistenceKeys.SpeedUnitKey, MaxSpeedApply);
            var maxSpeed = selectedAgent.BehaviourParameters.HasKey("maxSpeed")
                ? (float) selectedAgent.BehaviourParameters["maxSpeed"]
                : 0.0f;
            maxSpeedInput.ExternalValueChange(maxSpeed, false);
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
            ScenarioManager.Instance.GetExtension<ScenarioUndoManager>().RegisterRecord(new UndoToggle(
                isLaneChangeToggle, currentIsLaneChange, IsLaneChangeApply));
            IsLaneChangeApply(isLaneChange);
        }

        /// <summary>
        /// Method invoked when the is lane change variable is changed by the toggle
        /// </summary>
        /// <param name="isLaneChange">Is lane change</param>
        private void IsLaneChangeApply(bool isLaneChange)
        {
            if (currentIsLaneChange == isLaneChange)
                return;
            if (selectedAgent.BehaviourParameters.HasKey("isLaneChange"))
                selectedAgent.BehaviourParameters["isLaneChange"] = isLaneChange;
            else
                selectedAgent.BehaviourParameters.Add("isLaneChange", isLaneChange);
            currentIsLaneChange = isLaneChange;
        }

        /// <summary>
        /// Method invoked when the max speed is changed by the input field
        /// </summary>
        /// <param name="maxSpeed">New max speed applied</param>
        private void MaxSpeedApply(float maxSpeed)
        {
            if (selectedAgent == null)
                return;
            if (selectedAgent.BehaviourParameters.HasKey("maxSpeed"))
                selectedAgent.BehaviourParameters["maxSpeed"] = maxSpeed;
            else
                selectedAgent.BehaviourParameters.Add("maxSpeed", maxSpeed);
        }
    }
}