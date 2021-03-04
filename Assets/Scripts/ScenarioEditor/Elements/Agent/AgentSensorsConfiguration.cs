/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Agents
{
    using System;
    using Managers;
    using ScenarioEditor.Agents;
    using SimpleJSON;
    using Undo;
    using Undo.Records;

    /// <summary>
    /// Scenario agent extension that handles the sensors configuration
    /// </summary>
    public class AgentSensorsConfiguration : ScenarioAgentExtension
    {
        /// <summary>
        /// Sensors configuration that will be applied to this agent
        /// </summary>
        public string SensorsConfigurationId { get; private set; }

        /// <summary>
        /// Event invoked when this agent changes the sensors configuration id
        /// </summary>
        public event Action<string> SensorsConfigurationIdChanged;

        /// <inheritdoc/>
        public override void Initialize(ScenarioAgent parentAgent)
        {
            base.Initialize(parentAgent);
            ParentAgent.VariantChanged += ParentAgentOnVariantChanged;
        }

        /// <inheritdoc/>
        public override void Deinitialize()
        {
            ParentAgent.VariantChanged -= ParentAgentOnVariantChanged;
            base.Deinitialize();
        }

        /// <inheritdoc/>
        public override void SerializeToJson(JSONNode agentNode)
        {
            agentNode.Add("sensorsConfigurationId", new JSONString(SensorsConfigurationId));
        }

        /// <inheritdoc/>
        public override void DeserializeFromJson(JSONNode agentNode)
        {
            ChangeSensorsConfigurationId(agentNode["sensorsConfigurationId"], false); 
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioAgent agent)
        {
            var origin = agent.GetExtension<AgentSensorsConfiguration>();
            if (origin == null) return;
            SensorsConfigurationId = origin.SensorsConfigurationId;
        }

        /// <summary>
        /// Changes the current agent sensors configuration id
        /// </summary>
        /// <param name="newId">New sensors configuration id</param>
        /// <param name="registerUndo">If true, this action can be undone</param>
        public void ChangeSensorsConfigurationId(string newId, bool registerUndo = true)
        {
            if (SensorsConfigurationId == newId)
                return;
            if (registerUndo)
            {
                var undoAction = new Action<string>(id =>
                {
                    ChangeSensorsConfigurationId(id, false);
                });
                ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                    .RegisterRecord(new GenericUndo<string>(SensorsConfigurationId,
                        "Undo change of a sensors configuration id.", undoAction));
            }

            SensorsConfigurationId = newId;
            SensorsConfigurationIdChanged?.Invoke(SensorsConfigurationId);
        }

        /// <summary>
        /// Method called when variant of the parent agent changes
        /// </summary>
        /// <param name="newVariant">New variant of the parent agent</param>
        private void ParentAgentOnVariantChanged(SourceVariant newVariant)
        {
            if (newVariant is EgoAgentVariant egoAgentVariant && egoAgentVariant.SensorsConfigurations.Count>0)
                ChangeSensorsConfigurationId(egoAgentVariant.SensorsConfigurations[0].Id, false);
            else
                ChangeSensorsConfigurationId("", false);
        }
    }
}
