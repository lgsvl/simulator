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
    public class UndoChangeBehaviour : UndoRecord
    {
        /// <summary>
        /// Scenario agent which variant was changed
        /// </summary>
        private ScenarioAgent scenarioAgent;

        /// <summary>
        /// Previous agent behaviour name
        /// </summary>
        private string behaviourName;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioAgent">Scenario agent which variant was changed</param>
        /// <param name="behaviourName">Previous agent behaviour name</param>
        public UndoChangeBehaviour(ScenarioAgent scenarioAgent, string behaviourName)
        {
            this.scenarioAgent = scenarioAgent;
            this.behaviourName = behaviourName;
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            scenarioAgent.ChangeBehaviour(behaviourName, false);
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback changed agent behaviour.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
