/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Elements.Agents
{
    using SimpleJSON;

    /// <summary>
    /// Base class for extending the scenario agent parameters
    /// </summary>
    public abstract class ScenarioAgentExtension
    {
        /// <summary>
        /// Scenario agent that this object extends
        /// </summary>
        protected ScenarioAgent ParentAgent { get; set; }
        
        /// <summary>
        /// Initialization method
        /// </summary>
        /// <param name="parentAgent">Scenario agent that this object extends</param>
        public virtual void Initialize(ScenarioAgent parentAgent)
        {
            ParentAgent = parentAgent;
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public virtual void Deinitialize()
        {
            ParentAgent = null;
        }

        /// <summary>
        /// Method that serializes current extension to the agent node
        /// </summary>
        /// <param name="agentNode">Agent node where object should be serialized</param>
        public abstract void SerializeToJson(JSONNode agentNode);

        /// <summary>
        /// Method that deserializes extension from the agent node
        /// </summary>
        /// <param name="agentNode">Agent node where object is serialized</param>
        public abstract void DeserializeFromJson(JSONNode agentNode);

        /// <summary>
        /// Method called after this element is instantiated using copied agent
        /// </summary>
        /// <param name="agent">Origin agent from which copy was created</param>
        public abstract void CopyProperties(ScenarioAgent agent);
    }
}
