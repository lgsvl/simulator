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
    using System.Linq;
    using System.Threading.Tasks;
    using Agents;
    using Elements.Agents;
    using Input;
    using Simulator.Utilities;
    using UnityEngine;
    using Utilities;

    /// <summary>
    /// Manager for caching and handling all the scenario agents and their sources
    /// </summary>
    public class ScenarioAgentsManager : MonoBehaviour, IScenarioEditorExtension
    {
        /// <summary>
        /// Available agent sources
        /// </summary>
        public List<ScenarioAgentSource> sources;
        
        /// <summary>
        /// Prefab for the destination point graphic representation on the map
        /// </summary>
        public GameObject destinationPoint;
        
        /// <inheritdoc/>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Available agent sources
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
            var loadingProcess = ScenarioManager.Instance.loadingPanel.AddProgress();
            loadingProcess.Update("Initializing agents.");
            await ScenarioManager.Instance.WaitForExtension<InputManager>();
            var tasks = new Task[sources.Count];
            var sourceProgresses = new Progress<float>[sources.Count];
            var sourceProgressesValue = new float[sources.Count];
            var progressUpdate = new Action(() =>
            {
                var progressSum = sourceProgressesValue.Sum();
                progressSum /= sources.Count;
                loadingProcess.Update($"Loading agents {progressSum:P}.");
            });
            for (var i = 0; i < sources.Count; i++)
            {
                var newSource = Instantiate(sources[i], transform);
                Sources.Add(newSource);
                var sourceId = i;
                sourceProgresses[i] = new Progress<float>(f =>
                {
                    sourceProgressesValue[sourceId] = f;
                    progressUpdate();
                });
                tasks[i] = newSource.Initialize(sourceProgresses[i]);
            }

            await Task.WhenAll(tasks);
            
            ScenarioManager.Instance.ScenarioReset += InstanceOnScenarioReset;
            IsInitialized = true;
            loadingProcess.NotifyCompletion();
            Debug.Log($"{GetType().Name} scenario editor extension has been initialized.");
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
            {
                source.Deinitialize();
                Destroy(source);
            }
            Sources.Clear();
            Agents.Clear();
            IsInitialized = false;
            Debug.Log($"{GetType().Name} scenario editor extension has been deinitialized.");
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