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
    using Simulator.Utilities;
    using UnityEngine;

    /// <summary>
    /// Manager for caching and handling all the scenario agents and their sources
    /// </summary>
    public class ScenarioAgentsManager : MonoBehaviour
    {
        /// <summary>
        /// All the available agent sources in this assembly
        /// </summary>
        public List<ScenarioAgentSource> Sources { get; } = new List<ScenarioAgentSource>();

        /// <summary>
        /// All instantiated scenario agents
        /// </summary>
        public List<ScenarioAgent> Agents { get; } = new List<ScenarioAgent>();

        /// <summary>
        /// Initialization method
        /// </summary>
        public async Task Initialize()
        {
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
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        public void Deinitialize()
        {
            Sources.Clear();
            Agents.Clear();
        }

        /// <summary>
        /// Registers the agent in the manager
        /// </summary>
        /// <param name="agent">Agent to register</param>
        public void RegisterAgent(ScenarioAgent agent)
        {
            Agents.Add(agent);
        }

        /// <summary>
        /// Unregisters the agent in the manager
        /// </summary>
        /// <param name="agent">Agent to register</param>
        public void UnregisterAgent(ScenarioAgent agent)
        {
            Agents.Remove(agent);
        }
    }
}