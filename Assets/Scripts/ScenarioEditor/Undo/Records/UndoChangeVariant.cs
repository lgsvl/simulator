/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo.Records
{
    using Agents;
    using Managers;

    /// <summary>
    /// Record that undoes changing the agent variant
    /// </summary>
    public class UndoChangeVariant : UndoRecord
    {
        /// <summary>
        /// Scenario agent which variant was changed
        /// </summary>
        private ScenarioAgent scenarioAgent;

        /// <summary>
        /// Previous agent variant
        /// </summary>
        private AgentVariant agentVariant;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioAgent">Scenario agent which variant was changed</param>
        /// <param name="agentVariant">Previous agent variant</param>
        public UndoChangeVariant(ScenarioAgent scenarioAgent, AgentVariant agentVariant)
        {
            this.scenarioAgent = scenarioAgent;
            this.agentVariant = agentVariant;
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            scenarioAgent.ChangeVariant(agentVariant, false);
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback changed agent variant.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
