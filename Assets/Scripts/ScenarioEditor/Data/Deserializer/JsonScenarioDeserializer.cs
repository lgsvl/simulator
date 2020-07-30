/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Data.Deserializer
{
    using System;
    using System.Threading.Tasks;
    using Agents;
    using Elements;
    using Managers;
    using SimpleJSON;
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
            var mapDeserialized = await DeserializeMap(json, callback);
            if (!mapDeserialized)
                return;
            await DeserializeAgents(json);
            DeserializeMetadata(json);
            callback?.Invoke();
        }
        
        /// <summary>
        /// Deserializes scenario meta data from the json data
        /// </summary>
        /// <param name="data">Json object with the metadata</param>
        private static void DeserializeMetadata(JSONNode data)
        {
            var vseMetadata = data["vse_metadata"];
            var cameraSettings = vseMetadata["camera_settings"];
            if (cameraSettings == null)
                return;
            var position = cameraSettings["position"];
            ScenarioManager.Instance.inputManager.ForceCameraReposition(position);
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
            if (ScenarioManager.Instance.MapManager.CurrentMapName != mapName)
            {
                var mapManager = ScenarioManager.Instance.MapManager;
                if (mapManager.MapExists(mapName))
                {
                    await mapManager.LoadMapAsync(mapName);
                    return true;
                }

                ScenarioManager.Instance.logPanel.EnqueueError(
                    $"Loaded scenario requires map {mapName} which is not available in the database.");
                return false;
            }

            await ScenarioManager.Instance.MapManager.LoadMapAsync(mapName);
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
                var agentSource =
                    ScenarioManager.Instance.agentsManager.Sources.Find(source => source.AgentTypeId == agentType);
                if (agentSource == null)
                {
                    ScenarioManager.Instance.logPanel.EnqueueError(
                        $"Error while deserializing Scenario. Agent type '{agentType}' could not be found in Simulator.");
                    continue;
                }

                var variantName = agentNode["variant"];
                var variant = agentSource.AgentVariants.Find(sourceVariant => sourceVariant.name == variantName);
                if (variant == null)
                {
                    ScenarioManager.Instance.logPanel.EnqueueError(
                        $"Error while deserializing Scenario. Agent variant '{variantName}' could not be found in Simulator.");
                    continue;
                }

                await variant.Prepare();
                var agentInstance = agentSource.GetAgentInstance(variant);
                agentInstance.Uid = agentNode["uid"];
                var transformNode = agentNode["transform"];
                agentInstance.transform.position = transformNode["position"].ReadVector3();
                agentInstance.transform.rotation = Quaternion.Euler(transformNode["rotation"].ReadVector3());

                DeserializeWaypoints(agentNode, agentInstance);
            }
        }

        /// <summary>
        /// Deserializes waypoints for the scenario agent from the json data
        /// </summary>
        /// <param name="data">Json data with agent's waypoints</param>
        /// <param name="scenarioAgent">Scenario agent which includes those waypoints</param>
        private static void DeserializeWaypoints(JSONNode data, ScenarioAgent scenarioAgent)
        {
            var waypointsNode = data["waypoints"] as JSONArray;
            if (waypointsNode == null)
                return;

            foreach (var waypointNode in waypointsNode.Children)
            {
                var mapWaypointPrefab = ScenarioManager.Instance.waypointsManager.waypointPrefab;
                var waypointInstance = ScenarioManager.Instance.prefabsPools.GetInstance(mapWaypointPrefab)
                    .GetComponent<ScenarioWaypoint>();
                waypointInstance.transform.position = waypointNode["position"].ReadVector3();
                waypointInstance.WaitTime = waypointNode["wait_time"];
                waypointInstance.Speed = waypointNode["speed"];
                int index = waypointNode["ordinal_number"];
                var trigger = DeserializeTrigger(waypointNode["trigger"]);
                waypointInstance.LinkedTrigger.Trigger = trigger;
                //TODO sort waypoints
                scenarioAgent.AddWaypoint(waypointInstance, index);
            }
        }

        /// <summary>
        /// Deserializes a trigger from the json data
        /// </summary>
        /// <param name="triggerNode">Json data with a trigger</param>
        private static WaypointTrigger DeserializeTrigger(JSONNode triggerNode)
        {
            if (triggerNode == null)
                return null;
            var effectorsNode = triggerNode["effectors"];

            var trigger = new WaypointTrigger();
            if (effectorsNode == null)
                return trigger;

            foreach (var effectorNode in effectorsNode.Children)
            {
                var typeName = effectorNode["typeName"];
                var effector = TriggersManager.GetEffectorOfType(typeName);
                if (effector == null)
                {
                    ScenarioManager.Instance.logPanel.EnqueueError(
                        $"Could not deserialize trigger effector as the type '{typeName}' does not implement '{nameof(TriggerEffector)}' interface.");
                    continue;
                }

                effector.DeserializeProperties(effectorNode["parameters"]);
                trigger.Effectors.Add(effector);
            }

            return trigger;
        }
    }
}