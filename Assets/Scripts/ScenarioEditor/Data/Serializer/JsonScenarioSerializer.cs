/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.Data.Serializer
{
    using Agents;
    using Controllables;
    using Elements.Agents;
    using Managers;
    using SimpleJSON;
    using Simulator.Utilities;

    /// <summary>
    /// Class serializing a scenario into a json data
    /// </summary>
    public static class JsonScenarioSerializer
    {
        /// <summary>
        /// Serializes current scenario state into a json scenario
        /// </summary>
        /// <returns>Json scenario with serialized scenario</returns>
        public static JsonScenario SerializeScenario()
        {
            var scenarioData = new JSONObject();
            var scenarioManager = ScenarioManager.Instance;
            scenarioData.Add("version", new JSONString("0.01"));
            var vseMetadata = new JSONObject();
            scenarioData.Add("vseMetadata", vseMetadata);
            SerializeMetadata(vseMetadata);
            AddMapNode(scenarioData, scenarioManager.GetExtension<ScenarioMapManager>().CurrentMapMetaData);
            //Add agents
            var agents = scenarioManager.GetExtension<ScenarioAgentsManager>().Agents;
            var agentsNode = scenarioData.GetValueOrDefault("agents", new JSONArray());
            if (!scenarioData.HasKey("agents"))
                scenarioData.Add("agents", agentsNode);
            foreach (var agent in agents)
                AddAgentNode(agentsNode, agent);
            //Add controllables
            var controllables = scenarioManager.GetExtension<ScenarioControllablesManager>().Controllables;
            var controllablesNode = scenarioData.GetValueOrDefault("controllables", new JSONArray());
            if (!scenarioData.HasKey("controllables"))
                scenarioData.Add("controllables", controllablesNode);
            foreach (var controllable in controllables)
                AddControllableNode(controllablesNode, controllable);

            return new JsonScenario(scenarioData);
        }

        /// <summary>
        /// Adds visual scenario editor metadata to the json scenario
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        private static void SerializeMetadata(JSONObject data)
        {
            var cameraSettings = new JSONObject();
            data.Add("cameraSettings", cameraSettings);
            var camera = ScenarioManager.Instance.ScenarioCamera;
            var position = new JSONObject().WriteVector3(camera.transform.position);
            cameraSettings.Add("position", position);
            var rotation = new JSONObject().WriteVector3(camera.transform.rotation.eulerAngles);
            cameraSettings.Add("rotation", rotation);
        }

        /// <summary>
        /// Adds map node to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="scenarioMap">Scenario map to serialize</param>
        private static void AddMapNode(JSONObject data, ScenarioMapManager.MapMetaData scenarioMap)
        {
            var mapNode = new JSONObject();
            data.Add("map", mapNode);
            mapNode.Add("id", new JSONString(scenarioMap.guid));
            mapNode.Add("name", new JSONString(scenarioMap.name));
            mapNode.Add("parameterType", new JSONString("map"));
        }

        /// <summary>
        /// Adds an agent node to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="scenarioAgent">Scenario agent to serialize</param>
        private static void AddAgentNode(JSONNode data, ScenarioAgent scenarioAgent)
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

        /// <summary>
        /// Adds an controllable node to the json
        /// </summary>
        /// <param name="data">Json object where data will be added</param>
        /// <param name="controllable">Scenario controllable to serialize</param>
        private static void AddControllableNode(JSONNode data, ScenarioControllable controllable)
        {
            var controllableNode = new JSONObject();
            data.Add(controllableNode);
            controllableNode.Add("uid", new JSONString(controllable.Uid));
            controllableNode.Add("policy", Utility.SerializeControlPolicy(controllable.Policy));
            controllableNode.Add("spawned", controllable.IsEditableOnMap);
            if (!controllable.IsEditableOnMap) return;

            controllableNode.Add("name", new JSONString(controllable.Variant.Name));
            var transform = new JSONObject();
            controllableNode.Add("transform", transform);
            var position = new JSONObject().WriteVector3(controllable.TransformToMove.position);
            transform.Add("position", position);
            var rotation = new JSONObject().WriteVector3(controllable.TransformToRotate.rotation.eulerAngles);
            transform.Add("rotation", rotation);
        }
    }
}