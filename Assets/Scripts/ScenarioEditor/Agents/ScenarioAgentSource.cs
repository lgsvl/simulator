/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Agents
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Input;
    using UnityEngine;

    /// <summary>
    /// Scenario agent type source used to instantiate and handle new agents in the scenario
    /// </summary>
    public abstract class ScenarioAgentSource : IDragHandler
    {
        /// <summary>
        /// Name of the agent type this source handles
        /// </summary>
        public abstract string AgentTypeName { get; }

        /// <summary>
        /// Id of the agent type this source handles
        /// </summary>
        public abstract int AgentTypeId { get; }

        /// <summary>
        /// Agent type this source handles
        /// </summary>
        public AgentType AgentType => (AgentType) AgentTypeId;

        /// <summary>
        /// List of available agent variants in this agent type
        /// </summary>
        public abstract List<AgentVariant> AgentVariants { get; }
        
        /// <summary>
        /// Variant that will be used as initial one
        /// </summary>
        public abstract AgentVariant DefaultVariant { get; set; }

        /// <summary>
        /// Initialization method
        /// </summary>
        public abstract Task Initialize();

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public abstract void Deinitialize();

        /// <summary>
        /// Method that instantiates and initializes a prefab of the selected variant
        /// </summary>
        /// <param name="variant">Agent variant which model should be instantiated</param>
        /// <returns>Agent variant model</returns>
        public abstract GameObject GetModelInstance(AgentVariant variant);

        /// <summary>
        /// Method that instantiates new <see cref="ScenarioAgent"/> and initializes it with selected variant
        /// </summary>
        /// <param name="variant">Agent variant which model should be instantiated</param>
        /// <returns><see cref="ScenarioAgent"/> initializes with selected variant</returns>
        public abstract ScenarioAgent GetAgentInstance(AgentVariant variant);

        /// <summary>
        /// Returns the model instance to the models pool
        /// </summary>
        /// <param name="instance">Agent variant instance which should be returned to the pool</param>
        public abstract void ReturnModelInstance(GameObject instance);

        /// <summary>
        /// Method that instantiates new agent and starts dragging it
        /// </summary>
        public abstract void DragNewAgent();

        /// <inheritdoc/>
        public abstract void DragStarted();

        /// <inheritdoc/>
        public abstract void DragMoved();

        /// <inheritdoc/>
        public abstract void DragFinished();

        /// <inheritdoc/>
        public abstract void DragCancelled();
    }
}