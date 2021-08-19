/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Data.Deserializer
{
    using System;
    using System.Threading.Tasks;
    using Agents;
    using Controllable;
    using Controllables;
    using Input;
    using Managers;
    using SimpleJSON;
    using Simulator.Utilities;
    using UnityEngine;

    /// <summary>
    /// Class deserializing json data and loading a scenario from it
    /// </summary>
    public static class JsonScenarioDeserializer
    {
        /// <summary>
        /// Deserializes and loads scenario from the given json data
        /// </summary>
        /// <param name="json">Json data with the scenario</param>
        /// <param name="callback">Callback invoked after the scenario is loaded</param>
        public static async Task DeserializeScenario(JSONNode json, Action callback = null)
        {
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            try
            {
                var mapDeserialized = await DeserializeMap(json, callback);
                if (!mapDeserialized)
                {
                    ScenarioManager.Instance.logPanel.EnqueueError("Could not deserialize the scenario, failed deserializing map.");
                    return;
                }

                await DeserializeAgents(json);
                DeserializeControllables(json);
                DeserializeMetadata(json);
                callback?.Invoke();
            }
            catch (Exception ex)
            {
                ScenarioManager.Instance.logPanel.EnqueueError($"Could not deserialize the scenario. Exception: {ex.Message}.");
                callback?.Invoke();
            }
        }

        /// <summary>
        /// Deserializes scenario meta data from the json data
        /// </summary>
        /// <param name="data">Json object with the metadata</param>
        private static void DeserializeMetadata(JSONNode data)
        {
            var vseMetadata = data["vseMetadata"];
            if (vseMetadata == null)
                vseMetadata = data["vse_metadata"];
            if (vseMetadata == null)
                return;
            var cameraSettings = vseMetadata["cameraSettings"];
            if (cameraSettings == null)
                cameraSettings = vseMetadata["camera_settings"];
            if (cameraSettings == null)
                return;
            var position = cameraSettings["position"];
            var camera = ScenarioManager.Instance.ScenarioCamera;
            var rotation = cameraSettings.HasKey("rotation")
                ? cameraSettings["rotation"].ReadVector3()
                : camera.transform.rotation.eulerAngles;
            ScenarioManager.Instance.GetExtension<InputManager>().ForceCameraReposition(position, rotation);
        }

        /// <summary>
        /// Deserializes scenario map from the json data
        /// </summary>
        /// <param name="data">Json data with the map</param>
        /// <param name="callback">Callback invoked after the scenario is loaded</param>
        /// <returns>True if map could have loaded, false if maps requires reloading or error occured</returns>
        private static async Task<bool> DeserializeMap(JSONNode data, Action callback)
        {
            var map = data["map"];
            if (map == null)
                return false;
            var mapName = map["name"];
            if (mapName == null)
                return false;
            var mapManager = ScenarioManager.Instance.GetExtension<ScenarioMapManager>();
            if (mapManager.CurrentMapName != mapName)
            {
                if (mapManager.MapExists(mapName))
                {
                    await mapManager.LoadMapAsync(mapName);
                    return mapManager.CurrentMapName == mapName;
                }

                ScenarioManager.Instance.logPanel.EnqueueError(
                    $"Loaded scenario requires map {mapName} which is not available in the database.");
                return false;
            }

            await mapManager.LoadMapAsync(mapName);
            return true;
        }

        /// <summary>
        /// Deserializes scenario agents from the json data
        /// </summary>
        /// <param name="data">Json data with scenario agents</param>
        private static async Task DeserializeAgents(JSONNode data)
        {
            var agents = data["agents"] as JSONArray;
            if (agents == null)
                return;
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
                var agentInstance = agentSource.GetAgentInstance(agentVariant);
                //Disable gameobject to delay OnEnable methods
                agentInstance.gameObject.SetActive(false);
                agentInstance.Uid = agentNode["uid"];
                var transformNode = agentNode["transform"];
                agentInstance.transform.position = transformNode["position"].ReadVector3();
                agentInstance.TransformToRotate.rotation = Quaternion.Euler(transformNode["rotation"].ReadVector3());

                foreach (var extension in agentInstance.Extensions) extension.Value.DeserializeFromJson(agentNode);
                agentInstance.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Deserializes scenario controllables from the json data
        /// </summary>
        /// <param name="data">Json data with scenario controllables</param>
        private static void DeserializeControllables(JSONNode data)
        {
            var controllablesNode = data["controllables"] as JSONArray;
            if (controllablesNode == null)
                return;
            foreach (var controllableNode in controllablesNode.Children)
            {
                var controllablesManager = ScenarioManager.Instance.GetExtension<ScenarioControllablesManager>();
                IControllable iControllable;
                ScenarioControllable scenarioControllable;

                var uid = controllableNode["uid"];
                bool spawned;
                if (controllableNode.HasKey("spawned"))
                    spawned = controllableNode["spawned"].AsBool;
                else
                    spawned = controllablesManager.FindControllable(uid) == null;
                //Check if this controllable is already on the map, if yes just apply the policy
                if (!spawned)
                {
                    scenarioControllable = controllablesManager.FindControllable(uid);
                    if (scenarioControllable == null)
                    {
                        ScenarioManager.Instance.logPanel.EnqueueWarning(
                            $"Could not load controllable with uid: {uid}.");
                        continue;
                    }

                    iControllable = scenarioControllable.Variant.controllable;
                    scenarioControllable.Policy = iControllable.ParseControlPolicy(controllableNode["policy"], out _);
                    continue;
                }

                var controllableName = controllableNode["name"];
                var variant = controllablesManager.Source.Variants.Find(v => v.Name == controllableName);
                if (variant == null)
                {
                    ScenarioManager.Instance.logPanel.EnqueueError(
                        $"Error while deserializing Scenario. Controllable variant '{controllableName}' could not be found in Simulator.");
                    continue;
                }

                if (!(variant is ControllableVariant controllableVariant))
                {
                    ScenarioManager.Instance.logPanel.EnqueueError(
                        $"Could not properly deserialize variant '{controllableName}' as {nameof(ControllableVariant)} class.");
                    continue;
                }

                var policy = Utility.ParseControlPolicy(null, controllableNode["policy"], out _);
                scenarioControllable = controllablesManager.Source.GetControllableInstance(controllableVariant, policy);
                scenarioControllable.Uid = uid;
                if (scenarioControllable.IsEditableOnMap)
                {
                    var transformNode = controllableNode["transform"];
                    scenarioControllable.transform.position = transformNode["position"].ReadVector3();
                    scenarioControllable.TransformToRotate.rotation =
                        Quaternion.Euler(transformNode["rotation"].ReadVector3());
                }
            }
        }
    }
}