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
    using SimpleJSON;
    using Undo;
    using Undo.Records;

    /// <summary>
    /// Scenario agent extension that handles the behaviour
    /// </summary>
    public class AgentBehaviour : ScenarioAgentExtension
    {
        /// <summary>
        /// Behaviour that will control this agent in the simulation
        /// </summary>
        public string Behaviour { get; private set; }

        /// <summary>
        /// Parameters used by the set behaviour
        /// </summary>
        public JSONObject BehaviourParameters { get; set; } = new JSONObject();

        /// <summary>
        /// Event invoked when this agent changes the behaviour
        /// </summary>
        public event Action<string> BehaviourChanged;

        /// <inheritdoc/>
        public override void Initialize(ScenarioAgent parentAgent)
        {
            base.Initialize(parentAgent);
            ChangeBehaviour(nameof(NPCWaypointBehaviour), false);
        }

        /// <inheritdoc/>
        public override void SerializeToJson(JSONNode agentNode)
        {
            var behaviour = new JSONObject();
            behaviour.Add("name", new JSONString(Behaviour));
            agentNode.Add("behaviour", behaviour);
            if (BehaviourParameters.Count > 0)
                behaviour.Add("parameters", BehaviourParameters);
        }

        /// <inheritdoc/>
        public override void DeserializeFromJson(JSONNode agentNode)
        {
            if (agentNode.HasKey("behaviour"))
            {
                var behaviourNode = agentNode["behaviour"];
                if (behaviourNode.HasKey("parameters"))
                    BehaviourParameters = behaviourNode["parameters"] as JSONObject;
                ChangeBehaviour(behaviourNode["name"], false);
            }
        }

        /// <inheritdoc/>
        public override void CopyProperties(ScenarioAgent agent)
        {
            var origin = agent.GetExtension<AgentBehaviour>();
            if (origin == null) return;
            Behaviour = origin.Behaviour;
            BehaviourParameters = origin.BehaviourParameters;
        }

        /// <summary>
        /// Changes the current agent behaviour
        /// </summary>
        /// <param name="newBehaviour">New agent behaviour</param>
        /// <param name="registerUndo">If true, this action can be undone</param>
        public void ChangeBehaviour(string newBehaviour, bool registerUndo = true)
        {
            if (Behaviour == newBehaviour)
                return;
            if (registerUndo)
                ScenarioManager.Instance.GetExtension<ScenarioUndoManager>()
                    .RegisterRecord(new UndoChangeBehaviour(this));
            Behaviour = newBehaviour;
            BehaviourChanged?.Invoke(Behaviour);
        }
    }
}