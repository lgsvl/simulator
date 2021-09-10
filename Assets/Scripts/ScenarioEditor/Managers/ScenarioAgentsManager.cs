/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
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
    using Data;
    using Elements.Agents;
    using Input;
    using SimpleJSON;
    using Simulator.Utilities;
    using UnityEngine;
    using Utilities;

    /// <summary>
    /// Manager for caching and handling all the scenario agents and their sources
    /// </summary>
    public class ScenarioAgentsManager : MonoBehaviour, IScenarioEditorExtension, ISerializedExtension
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

        /// <inheritdoc/>
        public bool Serialize(JSONNode data)
        {
            var agentsNode = data.GetValueOrDefault("agents", new JSONArray());
            if (!data.HasKey("agents"))
                data.Add("agents", agentsNode);
            foreach (var agent in Agents)
                SerializeAgentNode(agentsNode, agent);
            return true;
        }

        /// <summary>
        /// Adds an agent node to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="scenarioAgent">Scenario agent to serialize</param>
        private static void SerializeAgentNode(JSONNode data, ScenarioAgent scenarioAgent)
        {
            var agentNode = new JSONObject();
            data.Add(agentNode);
            if (scenarioAgent.Variant is CloudAgentVariant cloudVariant)
                agentNode.Add("id", new JSONString(cloudVariant.guid));

            agentNode.Add("uid", new JSONString(scenarioAgent.Uid));
            agentNode.Add("variant", new JSONString(scenarioAgent.Variant.Name));
            agentNode.Add("type", new JSONNumber(scenarioAgent.Source.AgentTypeId));
            agentNode.Add("parameterType", new JSONString(scenarioAgent.Source.ParameterType));
            var transform = new JSONObject();
            agentNode.Add("transform", transform);
            var position = new JSONObject().WriteVector3(scenarioAgent.TransformToMove.position);
            transform.Add("position", position);
            var rotation = new JSONObject().WriteVector3(scenarioAgent.TransformToRotate.rotation.eulerAngles);
            transform.Add("rotation", rotation);

            foreach (var extension in scenarioAgent.Extensions) extension.Value.SerializeToJson(agentNode);
        }

        /// <inheritdoc/>
        public async Task<bool> Deserialize(JSONNode data)
        {
            var agents = data["agents"] as JSONArray;
            if (agents == null)
                return false;
            foreach (var agentNode in agents.Children)
            {
                var agentType = agentNode["type"];
                var agentsManager = ScenarioManager.Instance.GetExtension<ScenarioAgentsManager>();
                var agentSource = agentsManager.Sources.Find(source => source.AgentTypeId == agentType);
                if (agentSource == null)
                {
                    ScenarioManager.Instance.logPanel.EnqueueError(
                        $"Error while deserializing Scenario. Agent type '{agentType}' could not be found in Simulator.");
                    continue;
                }

                var variantName = agentNode["variant"];
                var variant = agentSource.Variants.Find(sourceVariant => sourceVariant.Name == variantName);
                if (variant == null)
                {
                    ScenarioManager.Instance.logPanel.EnqueueError(
                        $"Error while deserializing Scenario. Agent variant '{variantName}' could not be found in Simulator.");
                    continue;
                }

                if (!(variant is AgentVariant agentVariant))
                {
                    ScenarioManager.Instance.logPanel.EnqueueError(
                        $"Could not properly deserialize variant '{variantName}' as {nameof(AgentVariant)} class.");
                    continue;
                }

                await agentVariant.Prepare();
                var agentInstance = agentSource.GetElementInstance(agentVariant) as ScenarioAgent;
                //Disable gameobject to delay OnEnable methods
                agentInstance.gameObject.SetActive(false);
                agentInstance.Uid = agentNode["uid"];
                var transformNode = agentNode["transform"];
                agentInstance.TransformToMove.position = transformNode["position"].ReadVector3();
                agentInstance.TransformToRotate.rotation = Quaternion.Euler(transformNode["rotation"].ReadVector3());

                foreach (var extension in agentInstance.Extensions) extension.Value.DeserializeFromJson(agentNode);
                agentInstance.gameObject.SetActive(true);
            }

            return true;
        }
    }
}