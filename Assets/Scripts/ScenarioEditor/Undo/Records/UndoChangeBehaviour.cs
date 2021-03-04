/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Undo.Records
{
    using Elements.Agents;
    using Managers;
    using SimpleJSON;

    /// <summary>
    /// Record that undoes changing the agent behaviour
    /// </summary>
    public class UndoChangeBehaviour : UndoRecord
    {
        /// <summary>
        /// Scenario agent behaviour extension which behaviour was changed
        /// </summary>
        private AgentBehaviour agentBehaviour;

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
        /// <param name="agentBehaviour">Scenario agent behaviour extension which behaviour was changed</param>
        public UndoChangeBehaviour(AgentBehaviour agentBehaviour)
        {
            this.agentBehaviour = agentBehaviour;
            behaviourName = agentBehaviour.Behaviour;
            behaviourParameters = agentBehaviour.BehaviourParameters;
            agentBehaviour.BehaviourParameters = new JSONObject();
        }
        
        /// <inheritdoc/>
        public override void Undo()
        {
            agentBehaviour.BehaviourParameters = behaviourParameters;
            behaviourParameters = null;
            agentBehaviour.ChangeBehaviour(behaviourName, false);
            ScenarioManager.Instance.logPanel.EnqueueInfo("Undo applied to rollback changed agent behaviour.");
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            
        }
    }
}
