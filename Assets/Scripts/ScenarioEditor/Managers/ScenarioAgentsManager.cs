/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Agents;
    using Input;
    using Simulator.Utilities;
    using Utilities;

    /// <summary>
    /// Manager for caching and handling all the scenario agents and their sources
    /// </summary>
    public class ScenarioAgentsManager : IScenarioEditorExtension
    {
        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// All the available agent sources in this assembly
        /// </summary>
        public List<ScenarioAgentSource> Sources { get; } = new List<ScenarioAgentSource>();

        /// <summary>
        /// All instantiated scenario agents
        /// </summary>
        public List<ScenarioAgent> Agents { get; } = new List<ScenarioAgent>();

        /// <summary>
        /// Event invoked when a new agent is registered
        /// </summary>
        public event Action<ScenarioAgent> AgentRegistered;
        
        /// <summary>
        /// Event invoked when agent is unregistered
        /// </summary>
        public event Action<ScenarioAgent> AgentUnregistered;

        /// <summary>
        /// Initialization method
        /// </summary>
        public async Task Initialize()
        {
            if (IsInitialized)
                return;
            await ScenarioManager.Instance.WaitForExtension<PrefabsPools>();
            await ScenarioManager.Instance.WaitForExtension<InputManager>();
            var interfaceType = typeof(ScenarioAgentSource);
            var types = ReflectionCache.FindTypes((type) => !type.IsAbstract && interfaceType.IsAssignableFrom(type));
            var tasks = new Task[types.Count];
            for (var i = 0; i < types.Count; i++)
            {
                var agentSource = Activator.CreateInstance(types[i]) as ScenarioAgentSource;
                if (agentSource == null) continue;
                tasks[i] = agentSource.Initialize();

                Sources.Add(agentSource);
            }
            await Task.WhenAll(tasks);
            ScenarioManager.Instance.ScenarioReset += InstanceOnScenarioReset;
            IsInitialized = true;
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            if (!IsInitialized)
                return;
            InstanceOnScenarioReset();
            foreach (var source in Sources)
                source.Deinitialize();
            Sources.Clear();
            Agents.Clear();
            IsInitialized = false;
        }

        /// <summary>
        /// Method invoked when current scenario is being reset
        /// </summary>
        private void InstanceOnScenarioReset()
        {
            for (var i = Agents.Count - 1; i >= 0; i--)
            {
                var agent = Agents[i];
                agent.RemoveFromMap();
                agent.Dispose();
            }
        }

        /// <summary>
        /// Registers the agent in the manager
        /// </summary>
        /// <param name="agent">Agent to register</param>
        public void RegisterAgent(ScenarioAgent agent)
        {
            Agents.Add(agent);
            AgentRegistered?.Invoke(agent);
        }

        /// <summary>
        /// Unregisters the agent in the manager
        /// </summary>
        /// <param name="agent">Agent to register</param>
        public void UnregisterAgent(ScenarioAgent agent)
        {
            Agents.Remove(agent);
            AgentUnregistered?.Invoke(agent);
        }
    }
}