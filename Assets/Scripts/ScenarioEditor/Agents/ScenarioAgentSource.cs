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
    using Elements;
    using Elements.Agent;
    using Input;
    using Managers;

    /// <summary>
    /// Scenario agent type source used to instantiate and handle new agents in the scenario
    /// </summary>
    public abstract class ScenarioAgentSource : ScenarioElementSource, IDragHandler
    {
        /// <summary>
        /// Agent variant that is currently selected
        /// </summary>
        protected AgentVariant selectedVariant;
        
        /// <summary>
        /// Id of the agent type this source handles
        /// </summary>
        public abstract int AgentTypeId { get; }

        /// <summary>
        /// Agent type this source handles
        /// </summary>
        public AgentType AgentType => (AgentType) AgentTypeId;

        /// <summary>
        /// Available behaviour for this agent
        /// </summary>
        public List<string> Behaviours { get; protected set; }

        /// <summary>
        /// Initialization method
        /// </summary>
        public abstract Task Initialize();

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public abstract void Deinitialize();

        /// <summary>
        /// Method that instantiates new <see cref="ScenarioAgent"/> and initializes it with selected variant
        /// </summary>
        /// <param name="variant">Agent variant which model should be instantiated</param>
        /// <returns><see cref="ScenarioAgent"/> initialized with selected variant</returns>
        public abstract ScenarioAgent GetAgentInstance(AgentVariant variant);

        /// <summary>
        /// Checks if the given agent supports waypoints
        /// </summary>
        /// <param name="agent">Agent to check</param>
        /// <returns>True if agent supports waypoints, false otherwise</returns>
        public abstract bool AgentSupportWaypoints(ScenarioAgent agent);

        /// <inheritdoc/>
        public override void OnVariantSelected(SourceVariant variant)
        {
            selectedVariant = variant as AgentVariant;
            if (selectedVariant!=null)
                DragNewAgent();
        }

        /// <summary>
        /// Method that instantiates new agent and starts dragging it
        /// </summary>
        public void DragNewAgent()
        {
            ScenarioManager.Instance.GetExtension<InputManager>().StartDraggingElement(this);
        }

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