/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo.Records
{
    using System.Collections;
    using Agents;
    using Elements.Agent;
    using Managers;
    using SimpleJSON;

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
        /// Previous agent behaviour parameters
        /// </summary>
        private JSONObject behaviourParameters;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scenarioAgent">Scenario agent which variant was changed</param>
        public UndoChangeBehaviour(ScenarioAgent scenarioAgent)
        {
            this.scenarioAgent = scenarioAgent;
            behaviourName = scenarioAgent.Behaviour;
            behaviourParameters = scenarioAgent.BehaviourParameters;
            scenarioAgent.BehaviourParameters = new JSONObject();
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            scenarioAgent.BehaviourParameters = behaviourParameters;
            behaviourParameters = null;
            scenarioAgent.ChangeBehaviour(behaviourName, false);
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback changed agent behaviour.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
